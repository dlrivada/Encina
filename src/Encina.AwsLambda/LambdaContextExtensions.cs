using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using LanguageExt;
using Microsoft.Extensions.Logging;

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
    /// The error code of a tenant mismatch: the tenant header disagrees with the verified tenant claim.
    /// </summary>
    public const string TenantMismatchErrorCode = "tenancy.tenant_mismatch";

    /// <summary>
    /// Resolves the tenant ID of an API Gateway request: the verified claim wins, a header is a
    /// trusted fallback only when <see cref="EncinaAwsLambdaOptions.TrustTenantIdHeader"/> is set.
    /// </summary>
    /// <param name="context">The Lambda context.</param>
    /// <param name="request">The API Gateway request; <c>null</c> yields no tenant.</param>
    /// <param name="options">The configuration options.</param>
    /// <param name="logger">The logger used for the mismatch and the trusted-header notices.</param>
    /// <returns>
    /// <c>Right(Some(tenant))</c> when a tenant was found, <c>Right(None)</c> when the request carries none
    /// (or only an untrusted header), and <c>Left</c> with <see cref="TenantMismatchErrorCode"/> when the
    /// header disagrees with the claim.
    /// </returns>
    /// <remarks>
    /// <para>The rules, in order:</para>
    /// <list type="number">
    /// <item><description>A claim (the authorizer <c>claims</c> object, then the authorizer entry) is the verified tenant.</description></item>
    /// <item><description>A claim plus a different header is a mismatch (<c>Left</c>, logged as a Warning with the code only).</description></item>
    /// <item><description>No claim, a header and <see cref="EncinaAwsLambdaOptions.TrustTenantIdHeader"/>: the header is the tenant (the opt-out is logged as a Warning).</description></item>
    /// <item><description>No claim, a header that is not trusted: ignored (logged at Debug).</description></item>
    /// </list>
    /// <para>No log entry carries a tenant value.</para>
    /// </remarks>
    public static Either<EncinaError, Option<string>> ResolveTenantId(
        this ILambdaContext context,
        APIGatewayProxyRequest? request,
        EncinaAwsLambdaOptions options,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        if (request is null)
        {
            return Option<string>.None;
        }

        var claim = ReadAuthorizerValue(request, options.TenantIdClaimType);
        var header = ReadHeader(request, options.TenantIdHeader);

        if (claim is not null)
        {
            return header is not null && !string.Equals(header, claim, StringComparison.Ordinal)
                ? TenantMismatch(logger)
                : Option<string>.Some(claim);
        }

        return header is null ? Option<string>.None : ResolveHeaderOnly(header, options, logger);
    }

    private static Either<EncinaError, Option<string>> TenantMismatch(ILogger logger)
    {
        Log.TenantMismatch(logger, TenantMismatchErrorCode);
        return EncinaErrors.Create(
            TenantMismatchErrorCode,
            "The tenant supplied by the request does not match the authenticated tenant.");
    }

    private static Either<EncinaError, Option<string>> ResolveHeaderOnly(string header, EncinaAwsLambdaOptions options, ILogger logger)
    {
        if (!options.TrustTenantIdHeader)
        {
            Log.TenantHeaderIgnored(logger, options.TenantIdHeader);
            return Option<string>.None;
        }

        Log.TenantHeaderTrusted(logger, options.TenantIdHeader);
        return Option<string>.Some(header);
    }

    private static string? ReadHeader(APIGatewayProxyRequest request, string headerName) =>
        request.Headers?.TryGetValue(headerName, out var value) is true && !string.IsNullOrEmpty(value)
            ? value
            : null;

    private static string? ReadAuthorizerValue(APIGatewayProxyRequest request, string claimType)
    {
        var authorizer = request.RequestContext?.Authorizer;
        if (authorizer is null)
        {
            return null;
        }

        // Authorizer claims object (JWT authorizer), then the authorizer entry itself
        return ReadClaimsObject(authorizer, claimType) ?? ReadAuthorizerEntry(authorizer, claimType);
    }

    private static string? ReadClaimsObject(Amazon.Lambda.APIGatewayEvents.APIGatewayCustomAuthorizerContext authorizer, string claimType)
    {
        return authorizer.TryGetValue("claims", out var claimsObj) &&
               claimsObj is JsonElement { ValueKind: JsonValueKind.Object } claims
            ? ReadStringProperty(claims, claimType)
            : null;
    }

    private static string? ReadStringProperty(JsonElement claims, string name)
    {
        if (!claims.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = property.GetString();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static string? ReadAuthorizerEntry(Amazon.Lambda.APIGatewayEvents.APIGatewayCustomAuthorizerContext authorizer, string claimType) =>
        authorizer.TryGetValue(claimType, out var tenantFromAuth) &&
        tenantFromAuth is string { Length: > 0 } tenantStr
            ? tenantStr
            : null;

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
