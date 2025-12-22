using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Encina.AspNetCore;

/// <summary>
/// Middleware that enriches <see cref="IRequestContext"/> from ASP.NET Core <see cref="HttpContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// This middleware extracts ambient context information from the HTTP request and makes it available
/// to Encina handlers through <see cref="IRequestContext"/>.
/// </para>
/// <para>
/// Extracted information:
/// <list type="bullet">
/// <item><description><b>CorrelationId</b>: From X-Correlation-ID header or generates new from Activity.Current</description></item>
/// <item><description><b>UserId</b>: From ClaimsPrincipal (configurable claim type)</description></item>
/// <item><description><b>TenantId</b>: From claims or X-Tenant-ID header</description></item>
/// <item><description><b>IdempotencyKey</b>: From X-Idempotency-Key header</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In Program.cs or Startup.cs
/// app.UseEncinaContext();
///
/// // Now all mediator requests will have enriched context
/// var result = await mediator.Send(new CreateUserCommand(...));
/// </code>
/// </example>
public sealed class EncinaContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly EncinaAspNetCoreOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaContextMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">Configuration options.</param>
    public EncinaContextMiddleware(
        RequestDelegate next,
        IOptions<EncinaAspNetCoreOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="contextAccessor">Accessor for setting the request context.</param>
    public async Task InvokeAsync(HttpContext context, IRequestContextAccessor contextAccessor)
    {
        // Extract correlation ID from header or Activity
        var correlationId = ExtractCorrelationId(context);

        // Extract user ID from claims
        var userId = ExtractUserId(context);

        // Extract tenant ID from claims or header
        var tenantId = ExtractTenantId(context);

        // Extract idempotency key from header
        var idempotencyKey = ExtractIdempotencyKey(context);

        // Create enriched request context
        var requestContext = RequestContext.CreateForTest(
            userId: userId,
            tenantId: tenantId,
            idempotencyKey: idempotencyKey,
            correlationId: correlationId);

        // Set context for this request scope
        contextAccessor.RequestContext = requestContext;

        // Also set correlation ID in response header for traceability
        if (!string.IsNullOrEmpty(correlationId))
        {
            context.Response.Headers[_options.CorrelationIdHeader] = correlationId;
        }

        await _next(context);
    }

    private string ExtractCorrelationId(HttpContext context)
    {
        // 1. Try to get from header
        if (context.Request.Headers.TryGetValue(_options.CorrelationIdHeader, out var headerValue) &&
            !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString();
        }

        // 2. Try to get from Activity.Current (distributed tracing)
        if (Activity.Current?.Id != null)
        {
            return Activity.Current.Id;
        }

        // 3. Generate new
        return Activity.Current?.RootId ?? Guid.NewGuid().ToString();
    }

    private string? ExtractUserId(HttpContext context)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        // Try configured claim type first
        var userIdClaim = user.FindFirst(_options.UserIdClaimType);
        if (userIdClaim != null)
        {
            return userIdClaim.Value;
        }

        // Fallback: try common claim types
        // "sub" is the standard OIDC subject claim
        userIdClaim = user.FindFirst("sub");
        if (userIdClaim != null)
        {
            return userIdClaim.Value;
        }

        // Azure AD object identifier
        userIdClaim = user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier");
        if (userIdClaim != null)
        {
            return userIdClaim.Value;
        }

        return null;
    }

    private string? ExtractTenantId(HttpContext context)
    {
        // 1. Try to get from claims (preferred for authenticated users)
        var user = context.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = user.FindFirst(_options.TenantIdClaimType);
            if (tenantClaim != null)
            {
                return tenantClaim.Value;
            }

            // Azure AD tenant ID
            tenantClaim = user.FindFirst("tid");
            if (tenantClaim != null)
            {
                return tenantClaim.Value;
            }

            tenantClaim = user.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid");
            if (tenantClaim != null)
            {
                return tenantClaim.Value;
            }
        }

        // 2. Fallback to header (for API-to-API calls or custom scenarios)
        if (context.Request.Headers.TryGetValue(_options.TenantIdHeader, out var headerValue) &&
            !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString();
        }

        return null;
    }

    private string? ExtractIdempotencyKey(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(_options.IdempotencyKeyHeader, out var headerValue) &&
            !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString();
        }

        return null;
    }
}
