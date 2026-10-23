using System.Security.Claims;
using LanguageExt;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Encina.AzureFunctions;

/// <summary>
/// Extension methods for <see cref="FunctionContext"/> to extract context information.
/// </summary>
/// <remarks>
/// <para>
/// These extension methods provide convenient access to common context values that are
/// typically set by the <see cref="EncinaFunctionMiddleware"/>. The middleware enriches
/// the function context with correlation IDs and user IDs. The tenant is never stored on the
/// context: HTTP-triggered functions resolve it with
/// <see cref="ResolveTenantId(FunctionContext, HttpRequestData, EncinaAzureFunctionsOptions, ILogger)"/>.
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
    /// The error code of a tenant mismatch: the tenant header disagrees with the verified tenant claim.
    /// </summary>
    public const string TenantMismatchErrorCode = "tenancy.tenant_mismatch";

    /// <summary>
    /// Resolves the tenant ID of an HTTP request: the verified claim wins, a header is a trusted
    /// fallback only when <see cref="EncinaAzureFunctionsOptions.TrustTenantIdHeader"/> is set.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <param name="httpRequest">The HTTP request data.</param>
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
    /// <item><description>A claim of the request identities is the verified tenant.</description></item>
    /// <item><description>A claim plus a different header is a mismatch (<c>Left</c>, logged as a Warning with the code only).</description></item>
    /// <item><description>No claim, a header and <see cref="EncinaAzureFunctionsOptions.TrustTenantIdHeader"/>: the header is the tenant (the opt-out is logged as a Warning).</description></item>
    /// <item><description>No claim, a header that is not trusted: ignored (logged at Debug).</description></item>
    /// </list>
    /// <para>No log entry carries a tenant value.</para>
    /// </remarks>
    public static Either<EncinaError, Option<string>> ResolveTenantId(
        this FunctionContext context,
        HttpRequestData httpRequest,
        EncinaAzureFunctionsOptions options,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(httpRequest);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var claim = httpRequest.Identities
            .SelectMany(i => i.Claims)
            .FirstOrDefault(c => c.Type == options.TenantIdClaimType && !string.IsNullOrEmpty(c.Value))?.Value;

        string? header = null;
        if (httpRequest.Headers.TryGetValues(options.TenantIdHeader, out var values))
        {
            var first = values.FirstOrDefault();
            header = string.IsNullOrEmpty(first) ? null : first;
        }

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

    private static Either<EncinaError, Option<string>> ResolveHeaderOnly(string header, EncinaAzureFunctionsOptions options, ILogger logger)
    {
        if (!options.TrustTenantIdHeader)
        {
            Log.TenantHeaderIgnored(logger, options.TenantIdHeader);
            return Option<string>.None;
        }

        Log.TenantHeaderTrusted(logger, options.TenantIdHeader);
        return Option<string>.Some(header);
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
