using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

namespace Encina.AwsLambda;

/// <summary>
/// Extension methods for <see cref="ILambdaContext"/> to extract context information.
/// </summary>
/// <remarks>
/// <para>
/// These extension methods provide convenient access to common context values
/// from Lambda context and API Gateway requests.
/// </para>
/// </remarks>
public static class LambdaContextExtensions
{
    /// <summary>
    /// Gets the correlation ID from the Lambda context or API Gateway request headers.
    /// </summary>
    /// <param name="context">The Lambda context.</param>
    /// <param name="request">Optional API Gateway request for header extraction.</param>
    /// <param name="options">Optional configuration options.</param>
    /// <returns>The correlation ID if found, otherwise generates a new one based on AWS Request ID.</returns>
    public static string GetCorrelationId(
        this ILambdaContext context,
        APIGatewayProxyRequest? request = null,
        EncinaAwsLambdaOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        var headerName = options?.CorrelationIdHeader ?? "X-Correlation-ID";

        // Try to get from API Gateway request headers
        if (request?.Headers is not null &&
            request.Headers.TryGetValue(headerName, out var correlationId) &&
            !string.IsNullOrEmpty(correlationId))
        {
            return correlationId;
        }

        // Fall back to AWS Request ID
        return context.AwsRequestId;
    }

    /// <summary>
    /// Gets the correlation ID from an HTTP API (V2) request.
    /// </summary>
    /// <param name="context">The Lambda context.</param>
    /// <param name="request">The API Gateway HTTP API request.</param>
    /// <param name="options">Optional configuration options.</param>
    /// <returns>The correlation ID if found, otherwise generates a new one based on AWS Request ID.</returns>
    public static string GetCorrelationId(
        this ILambdaContext context,
        APIGatewayHttpApiV2ProxyRequest? request,
        EncinaAwsLambdaOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        var headerName = options?.CorrelationIdHeader ?? "X-Correlation-ID";

        // Try to get from API Gateway HTTP API request headers
        if (request?.Headers is not null &&
            request.Headers.TryGetValue(headerName, out var correlationId) &&
            !string.IsNullOrEmpty(correlationId))
        {
            return correlationId;
        }

        // Fall back to AWS Request ID
        return context.AwsRequestId;
    }

    /// <summary>
    /// Gets the tenant ID from the API Gateway request headers or JWT claims.
    /// </summary>
    /// <param name="context">The Lambda context.</param>
    /// <param name="request">The API Gateway request.</param>
    /// <param name="options">Optional configuration options.</param>
    /// <returns>The tenant ID if found, otherwise null.</returns>
    public static string? GetTenantId(
        this ILambdaContext context,
        APIGatewayProxyRequest? request = null,
        EncinaAwsLambdaOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (request is null) return null;

        var headerName = options?.TenantIdHeader ?? "X-Tenant-ID";
        var claimType = options?.TenantIdClaimType ?? "tenant_id";

        // Try to get from headers
        if (request.Headers?.TryGetValue(headerName, out var tenantId) is true &&
            !string.IsNullOrEmpty(tenantId))
        {
            return tenantId;
        }

        // Try to get from authorizer claims (API Gateway authorizer)
        if (request.RequestContext?.Authorizer?.TryGetValue("claims", out var claimsObj) is true &&
            claimsObj is JsonElement claims &&
            claims.TryGetProperty(claimType, out var tenantClaim))
        {
            return tenantClaim.GetString();
        }

        // Try to get from authorizer claims as dictionary
        if (request.RequestContext?.Authorizer?.TryGetValue(claimType, out var tenantFromAuth) is true &&
            tenantFromAuth is string tenantStr)
        {
            return tenantStr;
        }

        return null;
    }

    /// <summary>
    /// Gets the user ID from the API Gateway request JWT claims.
    /// </summary>
    /// <param name="context">The Lambda context.</param>
    /// <param name="request">The API Gateway request.</param>
    /// <param name="options">Optional configuration options.</param>
    /// <returns>The user ID if found, otherwise null.</returns>
    public static string? GetUserId(
        this ILambdaContext context,
        APIGatewayProxyRequest? request = null,
        EncinaAwsLambdaOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (request is null) return null;

        var claimType = options?.UserIdClaimType ?? "sub";

        // Try to get from authorizer claims (API Gateway authorizer)
        if (request.RequestContext?.Authorizer?.TryGetValue("claims", out var claimsObj) is true &&
            claimsObj is JsonElement claims &&
            claims.TryGetProperty(claimType, out var userClaim))
        {
            return userClaim.GetString();
        }

        // Try to get from authorizer claims as dictionary
        if (request.RequestContext?.Authorizer?.TryGetValue(claimType, out var userFromAuth) is true &&
            userFromAuth is string userStr)
        {
            return userStr;
        }

        // Try principalId from authorizer
        if (request.RequestContext?.Authorizer?.TryGetValue("principalId", out var principalId) is true &&
            principalId is string principal)
        {
            return principal;
        }

        return null;
    }

    /// <summary>
    /// Gets the AWS Request ID from the Lambda context.
    /// </summary>
    /// <param name="context">The Lambda context.</param>
    /// <returns>The AWS Request ID.</returns>
    public static string GetAwsRequestId(this ILambdaContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.AwsRequestId;
    }

    /// <summary>
    /// Gets the function name from the Lambda context.
    /// </summary>
    /// <param name="context">The Lambda context.</param>
    /// <returns>The Lambda function name.</returns>
    public static string GetFunctionName(this ILambdaContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.FunctionName;
    }

    /// <summary>
    /// Gets the remaining execution time in milliseconds.
    /// </summary>
    /// <param name="context">The Lambda context.</param>
    /// <returns>The remaining time in milliseconds.</returns>
    public static int GetRemainingTimeMs(this ILambdaContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return (int)context.RemainingTime.TotalMilliseconds;
    }
}
