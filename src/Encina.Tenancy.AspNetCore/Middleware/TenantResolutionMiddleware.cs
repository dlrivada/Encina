using Encina.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Encina.Tenancy.AspNetCore;

/// <summary>
/// Middleware that resolves the tenant identifier from HTTP requests using a chain of resolvers.
/// </summary>
/// <remarks>
/// <para>
/// This middleware uses the configured <see cref="ITenantResolver"/> chain to determine
/// the tenant ID from the incoming request. The resolved tenant ID is then set on the
/// <see cref="IRequestContext"/> via <see cref="IRequestContextAccessor"/>.
/// </para>
/// <para>
/// <b>Important:</b> This middleware should be placed after authentication middleware
/// and after <see cref="EncinaContextMiddleware"/> in the pipeline.
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

        // Update the request context with the resolved tenant ID
        if (!string.IsNullOrWhiteSpace(tenantId) && contextAccessor.RequestContext is not null)
        {
            contextAccessor.RequestContext = contextAccessor.RequestContext.WithTenantId(tenantId);
        }

        await _next(context);
    }
}
