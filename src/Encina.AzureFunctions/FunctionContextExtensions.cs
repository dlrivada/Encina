using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Encina.AzureFunctions;

/// <summary>
/// Extension methods for <see cref="FunctionContext"/> to extract context information.
/// </summary>
/// <remarks>
/// <para>
/// These extension methods provide convenient access to common context values that are
/// typically set by the <see cref="EncinaFunctionMiddleware"/>. The middleware enriches
/// the function context with correlation IDs, tenant IDs, and user IDs extracted from
/// HTTP headers and claims.
/// </para>
/// <para>
/// For HTTP-triggered functions, you can also pass the <see cref="HttpRequestData"/>
/// directly to extract values from headers and claims.
/// </para>
/// </remarks>
public static class FunctionContextExtensions
{
    /// <summary>
    /// Gets the correlation ID from the function context.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>The correlation ID if found, otherwise null.</returns>
    /// <remarks>
    /// The correlation ID should be set by <see cref="EncinaFunctionMiddleware"/> during
    /// request processing. Use <see cref="GetCorrelationId(FunctionContext, HttpRequestData, EncinaAzureFunctionsOptions?)"/>
    /// for HTTP-triggered functions to extract from headers directly.
    /// </remarks>
    public static string? GetCorrelationId(this FunctionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Items.TryGetValue(EncinaFunctionMiddleware.CorrelationIdKey, out var contextValue) &&
            contextValue is string correlationId)
        {
            return correlationId;
        }

        return null;
    }

    /// <summary>
    /// Gets the correlation ID from the HTTP request headers or function context.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <param name="httpRequest">The HTTP request data.</param>
    /// <param name="options">Optional configuration options.</param>
    /// <returns>The correlation ID if found, otherwise null.</returns>
    /// <remarks>
    /// The correlation ID is extracted in the following order:
    /// <list type="number">
    /// <item><description>From HTTP request headers using the configured header name</description></item>
    /// <item><description>From function context items (if set by middleware)</description></item>
    /// </list>
    /// </remarks>
    public static string? GetCorrelationId(
        this FunctionContext context,
        HttpRequestData httpRequest,
        EncinaAzureFunctionsOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(httpRequest);

        var headerName = options?.CorrelationIdHeader ?? "X-Correlation-ID";

        // Try to get from HTTP request headers
        if (httpRequest.Headers.TryGetValues(headerName, out var values))
        {
            var correlationId = values.FirstOrDefault();
            if (!string.IsNullOrEmpty(correlationId))
            {
                return correlationId;
            }
        }

        // Fall back to context items (set by middleware)
        return context.GetCorrelationId();
    }

    /// <summary>
    /// Gets the tenant ID from the function context.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>The tenant ID if found, otherwise null.</returns>
    /// <remarks>
    /// The tenant ID should be set by <see cref="EncinaFunctionMiddleware"/> during
    /// request processing. Use <see cref="GetTenantId(FunctionContext, HttpRequestData, EncinaAzureFunctionsOptions?)"/>
    /// for HTTP-triggered functions to extract from headers or claims directly.
    /// </remarks>
    public static string? GetTenantId(this FunctionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Items.TryGetValue(EncinaFunctionMiddleware.TenantIdKey, out var contextValue) &&
            contextValue is string tenantId)
        {
            return tenantId;
        }

        return null;
    }

    /// <summary>
    /// Gets the tenant ID from the HTTP request headers, claims, or function context.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <param name="httpRequest">The HTTP request data.</param>
    /// <param name="options">Optional configuration options.</param>
    /// <returns>The tenant ID if found, otherwise null.</returns>
    public static string? GetTenantId(
        this FunctionContext context,
        HttpRequestData httpRequest,
        EncinaAzureFunctionsOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(httpRequest);

        var headerName = options?.TenantIdHeader ?? "X-Tenant-ID";
        var claimType = options?.TenantIdClaimType ?? "tenant_id";

        // Try to get from HTTP request headers
        if (httpRequest.Headers.TryGetValues(headerName, out var values))
        {
            var tenantId = values.FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                return tenantId;
            }
        }

        // Try to get from claims
        var tenantClaim = httpRequest.Identities
            .SelectMany(i => i.Claims)
            .FirstOrDefault(c => c.Type == claimType);

        if (tenantClaim is not null && !string.IsNullOrEmpty(tenantClaim.Value))
        {
            return tenantClaim.Value;
        }

        // Fall back to context items
        return context.GetTenantId();
    }

    /// <summary>
    /// Gets the user ID from the function context.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>The user ID if found, otherwise null.</returns>
    /// <remarks>
    /// The user ID should be set by <see cref="EncinaFunctionMiddleware"/> during
    /// request processing. Use <see cref="GetUserId(FunctionContext, HttpRequestData, EncinaAzureFunctionsOptions?)"/>
    /// for HTTP-triggered functions to extract from claims directly.
    /// </remarks>
    public static string? GetUserId(this FunctionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Items.TryGetValue(EncinaFunctionMiddleware.UserIdKey, out var contextValue) &&
            contextValue is string userId)
        {
            return userId;
        }

        return null;
    }

    /// <summary>
    /// Gets the user ID from the HTTP request claims or function context.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <param name="httpRequest">The HTTP request data.</param>
    /// <param name="options">Optional configuration options.</param>
    /// <returns>The user ID if found, otherwise null.</returns>
    public static string? GetUserId(
        this FunctionContext context,
        HttpRequestData httpRequest,
        EncinaAzureFunctionsOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(httpRequest);

        var claimType = options?.UserIdClaimType ?? ClaimTypes.NameIdentifier;

        // Try to get from claims
        var userClaim = httpRequest.Identities
            .SelectMany(i => i.Claims)
            .FirstOrDefault(c => c.Type == claimType);

        if (userClaim is not null && !string.IsNullOrEmpty(userClaim.Value))
        {
            return userClaim.Value;
        }

        // Fall back to context items
        return context.GetUserId();
    }

    /// <summary>
    /// Gets the function invocation ID.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>The invocation ID.</returns>
    public static string GetInvocationId(this FunctionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.InvocationId;
    }
}
