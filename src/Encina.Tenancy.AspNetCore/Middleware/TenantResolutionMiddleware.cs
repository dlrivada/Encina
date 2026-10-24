using System.Diagnostics;
using Encina.Tenancy.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
/// The verified tenant claim always wins. When a client-supplied value (a header, a route value, the
/// subdomain) disagrees with it, the middleware answers 403 Forbidden with a problem+json body and
/// does not call the next middleware; the tenant store is not consulted. A tenant header is honored
/// only with no claim and from a trusted source (see <see cref="HeaderResolverOptions"/>).
/// </para>
/// <para>
/// When <see cref="TenancyOptions.RequireTenant"/> is <c>true</c> (the default) and no tenant can be
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
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantResolutionMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="resolvers">The tenant resolvers.</param>
    /// <param name="tenancyOptions">The core tenancy options.</param>
    /// <param name="aspNetCoreOptions">The ASP.NET Core tenancy options.</param>
    /// <param name="tenantStore">The tenant store for validation.</param>
    /// <param name="logger">The logger.</param>
    /// <remarks>
    /// Logs a Warning once for each explicit opt-out: the header resolver enabled with
    /// <see cref="HeaderResolverOptions.TrustAnyPeer"/>, and <see cref="TenancyOptions.RequireTenant"/> set to <c>false</c>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public TenantResolutionMiddleware(
        RequestDelegate next,
        IEnumerable<ITenantResolver> resolvers,
        IOptions<TenancyOptions> tenancyOptions,
        IOptions<TenancyAspNetCoreOptions> aspNetCoreOptions,
        ITenantStore tenantStore,
        ILogger<TenantResolutionMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(resolvers);
        ArgumentNullException.ThrowIfNull(tenancyOptions);
        ArgumentNullException.ThrowIfNull(aspNetCoreOptions);
        ArgumentNullException.ThrowIfNull(tenantStore);
        ArgumentNullException.ThrowIfNull(logger);

        _next = next;
        _resolverChain = new TenantResolverChain(resolvers);
        _tenancyOptions = tenancyOptions.Value;
        _aspNetCoreOptions = aspNetCoreOptions.Value;
        _tenantStore = tenantStore;
        _logger = logger;

        LogExplicitOptOuts();
    }

    private void LogExplicitOptOuts()
    {
        var header = _aspNetCoreOptions.HeaderResolver;
        if (header.Enabled && header.TrustAnyPeer)
        {
            TenancyAspNetCoreLogMessages.HeaderTrustAnyPeerEnabled(_logger, header.HeaderName);
        }

        if (!_tenancyOptions.RequireTenant)
        {
            TenancyAspNetCoreLogMessages.TenantNotRequired(_logger);
        }
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

        var resolution = await _resolverChain.ResolveAsync(context, cancellationToken);

        // A client-supplied tenant that disagrees with the verified one is denied before the tenant store is consulted.
        if (resolution.IsMismatch)
        {
            await WriteMismatchAsync(context, resolution, cancellationToken);
            return;
        }

        // Validate the tenant (an unknown tenant counts as no tenant).
        var tenantId = await ValidateTenantAsync(resolution.TenantId, cancellationToken);

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

    private async Task WriteMismatchAsync(HttpContext context, TenantResolution resolution, CancellationToken cancellationToken)
    {
        TenancyAspNetCoreLogMessages.TenantMismatch(_logger, resolution.AuthoritativeResolver!, resolution.ConflictingResolver!);

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(
            """{"type":"https://tools.ietf.org/html/rfc9110#name-403-forbidden","title":"Tenant mismatch","status":403,"detail":"The tenant supplied by the request does not match the authenticated tenant."}""",
            cancellationToken);
    }

    private async Task<string?> ValidateTenantAsync(string? tenantId, CancellationToken cancellationToken)
    {
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
