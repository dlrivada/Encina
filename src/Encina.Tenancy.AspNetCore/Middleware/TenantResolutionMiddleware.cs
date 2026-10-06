using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Encina.Tenancy.AspNetCore;

/// <summary>
/// Middleware that resolves the tenant identifier from HTTP requests using a chain of resolvers.
/// </summary>
/// <remarks>
/// <para>
/// This middleware uses the configured <see cref="ITenantResolver"/> chain to determine
/// the tenant ID from the incoming request. The resolved tenant ID is then set on the ambient
/// <see cref="IRequestContext"/> held by <see cref="IRequestContextAccessor"/>, which
/// <c>IEncina.Send</c>, <c>Publish</c> and <c>Stream</c> use to seed the pipeline.
/// </para>
/// <para>
/// When an earlier middleware already established a context (for example <c>UseEncinaContext()</c>
/// from the <c>Encina.AspNetCore</c> package, which adds the user, the idempotency key and the audit
/// data), the tenant is added to that context. When no context exists yet, the middleware creates
/// one holding the resolved tenant and the request's correlation id
/// (<see cref="Activity.Current"/>, else <see cref="HttpContext.TraceIdentifier"/>), so the tenant is
/// never lost.
/// </para>
/// <para>
/// Place this middleware after authentication (claim-based resolution needs the user) and after
/// <c>UseEncinaContext()</c> when that middleware is used, so the tenant is added to its context
/// instead of being replaced by it.
/// </para>
/// <para>
/// When <see cref="TenancyOptions.RequireTenant"/> is <c>true</c> and no tenant can be
/// resolved, the middleware returns a 400 Bad Request response.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In Program.cs
/// app.UseAuthentication();
/// app.UseEncinaContext();
/// app.UseTenantResolution(); // After UseEncinaContext
/// app.UseAuthorization();
/// </code>
/// </example>
public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TenantResolverChain _resolverChain;
    private readonly TenancyOptions _tenancyOptions;
    private readonly TenancyAspNetCoreOptions _aspNetCoreOptions;
    private readonly ITenantStore _tenantStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantResolutionMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="resolvers">The tenant resolvers.</param>
    /// <param name="tenancyOptions">The core tenancy options.</param>
    /// <param name="aspNetCoreOptions">The ASP.NET Core tenancy options.</param>
    /// <param name="tenantStore">The tenant store for validation.</param>
    public TenantResolutionMiddleware(
        RequestDelegate next,
        IEnumerable<ITenantResolver> resolvers,
        IOptions<TenancyOptions> tenancyOptions,
        IOptions<TenancyAspNetCoreOptions> aspNetCoreOptions,
        ITenantStore tenantStore)
    {
        _next = next;
        _resolverChain = new TenantResolverChain(resolvers);
        _tenancyOptions = tenancyOptions.Value;
        _aspNetCoreOptions = aspNetCoreOptions.Value;
        _tenantStore = tenantStore;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="contextAccessor">Accessor for updating the request context.</param>
    /// <param name="timeProvider">The clock that stamps a context this middleware creates.</param>
    /// <remarks>
    /// On a request that opens a long-lived connection (a WebSocket upgrade, any extended CONNECT, a
    /// GET whose <c>Accept</c> lists <c>text/event-stream</c>, a SignalR hub endpoint) the tenant is
    /// still resolved and validated, and <see cref="TenancyOptions.RequireTenant"/> still answers 400,
    /// but no request context is written: the connection would keep the connect-time tenant for its
    /// whole life.
    /// </remarks>
    public async Task InvokeAsync(HttpContext context, IRequestContextAccessor contextAccessor, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contextAccessor);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var cancellationToken = context.RequestAborted;

        // Resolve and validate the tenant (an unknown tenant counts as no tenant).
        var tenantId = await ResolveValidTenantAsync(context, cancellationToken);

        // Check if tenant is required but not resolved
        if (_tenancyOptions.RequireTenant && string.IsNullOrWhiteSpace(tenantId) && _aspNetCoreOptions.Return400WhenTenantRequired)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsync(
                """{"type":"https://tools.ietf.org/html/rfc9110#name-400-bad-request","title":"Tenant identification required","status":400,"detail":"Unable to determine tenant from the request. Ensure a valid tenant identifier is provided via header, claim, route, or subdomain."}""",
                cancellationToken);

            return;
        }

        WriteTenant(context, contextAccessor, timeProvider, tenantId);
        await _next(context);
    }

    private async Task<string?> ResolveValidTenantAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var tenantId = await _resolverChain.ResolveAsync(context, cancellationToken);
        if (!_tenancyOptions.ValidateTenantOnRequest || string.IsNullOrWhiteSpace(tenantId))
        {
            return tenantId;
        }

        return await _tenantStore.ExistsAsync(tenantId, cancellationToken) ? tenantId : null;
    }

    // Puts the resolved tenant on the ambient request context, creating one when no earlier middleware
    // established it, so the tenant always reaches the Encina pipeline. Never on a connection request:
    // that context would outlive the request with the connect-time tenant.
    private static void WriteTenant(HttpContext context, IRequestContextAccessor contextAccessor, TimeProvider timeProvider, string? tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || ConnectionRequests.IsConnectionRequest(context))
        {
            return;
        }

        var requestContext = contextAccessor.RequestContext ?? CreateRequestContext(context, timeProvider);
        contextAccessor.RequestContext = requestContext.WithTenantId(tenantId);
    }

    private static IRequestContext CreateRequestContext(HttpContext context, TimeProvider timeProvider) =>
        RequestContext.CreateAnonymousAt(timeProvider.GetUtcNow(), ResolveCorrelationId(context));

    private static string ResolveCorrelationId(HttpContext context)
    {
        var correlationId = Activity.Current?.Id ?? context.TraceIdentifier;
        return string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("N") : correlationId;
    }
}
