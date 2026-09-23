using System.Diagnostics;
using Microsoft.AspNetCore.Http;
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
    public async Task InvokeAsync(HttpContext context, IRequestContextAccessor contextAccessor)
    {
        var cancellationToken = context.RequestAborted;

        // Resolve tenant ID using the resolver chain
        var tenantId = await _resolverChain.ResolveAsync(context, cancellationToken);

        // Validate tenant if required
        if (_tenancyOptions.ValidateTenantOnRequest && !string.IsNullOrWhiteSpace(tenantId))
        {
            var exists = await _tenantStore.ExistsAsync(tenantId, cancellationToken);

            if (!exists)
            {
                // Treat as if no tenant was resolved
                tenantId = null;
            }
        }

        // Check if tenant is required but not resolved
        if (_tenancyOptions.RequireTenant && string.IsNullOrWhiteSpace(tenantId))
        {
            if (_aspNetCoreOptions.Return400WhenTenantRequired)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsync(
                    """{"type":"https://tools.ietf.org/html/rfc9110#name-400-bad-request","title":"Tenant identification required","status":400,"detail":"Unable to determine tenant from the request. Ensure a valid tenant identifier is provided via header, claim, route, or subdomain."}""",
                    cancellationToken);

                return;
            }
        }

        // Put the resolved tenant on the ambient request context, creating one when no earlier
        // middleware established it, so the tenant always reaches the Encina pipeline.
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            var requestContext = contextAccessor.RequestContext ?? CreateRequestContext(context);
            contextAccessor.RequestContext = requestContext.WithTenantId(tenantId);
        }

        await _next(context);
    }

    private static IRequestContext CreateRequestContext(HttpContext context)
    {
        var correlationId = Activity.Current?.Id ?? context.TraceIdentifier;

        return string.IsNullOrWhiteSpace(correlationId)
            ? RequestContext.Create()
            : RequestContext.Create(correlationId);
    }
}
