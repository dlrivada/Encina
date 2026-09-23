namespace Encina.Messaging.Recoverability;

/// <summary>
/// Classifies errors to determine retry behavior.
/// </summary>
/// <remarks>
/// <para>
/// The error classifier analyzes failures and categorizes them as:
/// <list type="bullet">
/// <item><description><see cref="ErrorClassification.Transient"/>: Temporary failures that should be retried</description></item>
/// <item><description><see cref="ErrorClassification.Permanent"/>: Unrecoverable failures that go directly to DLQ</description></item>
/// <item><description><see cref="ErrorClassification.Unknown"/>: Classification could not be determined (treated as transient)</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IErrorClassifier
{
    /// <summary>
    /// Classifies an error to determine if it should be retried.
    /// </summary>
    /// <param name="encinaError">The error to classify.</param>
    /// <param name="exception">The exception that caused the error, if any.</param>
    /// <returns>The error classification.</returns>
    ErrorClassification Classify(EncinaError encinaError, Exception? exception);
}

/// <summary>
/// Classification of an error for retry purposes.
/// </summary>
public enum ErrorClassification
{
    /// <summary>
    /// Unknown error classification. Treated as transient by default.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Transient error that may succeed on retry (network timeout, temporary unavailability, etc.).
    /// </summary>
    Transient = 1,

    /// <summary>
    /// Permanent error that will not succeed on retry (validation failure, not found, unauthorized, etc.).
    /// </summary>
    Permanent = 2
}

/// <summary>
/// Default error classifier using common exception types and error codes.
/// </summary>
/// <remarks>
/// <para>
/// Classifies as <b>transient</b>:
/// <list type="bullet">
/// <item><description><see cref="TimeoutException"/></description></item>
/// <item><description><see cref="HttpRequestException"/> with 5xx status or network errors</description></item>
/// <item><description><see cref="TaskCanceledException"/> (usually timeout)</description></item>
/// <item><description><see cref="IOException"/> (network issues)</description></item>
/// </list>
/// </para>
/// <para>
/// Classifies as <b>permanent</b>:
/// <list type="bullet">
/// <item><description><see cref="ArgumentException"/> and derivatives</description></item>
/// <item><description><see cref="InvalidOperationException"/></description></item>
/// <item><description><see cref="NotSupportedException"/></description></item>
/// <item><description><see cref="UnauthorizedAccessException"/></description></item>
/// <item><description>Error codes or messages containing "validation", "not_found", "unauthorized", "forbidden", "invalid", "bad_request"</description></item>
/// <item><description>Error codes (not messages) containing "missing", "mismatch", "authorization",
/// "consent.expired", "withdrawn", "reconsent", "restriction_active", "not_verified", "rejected" or
/// "exemption" — for example <c>encina.handler.missing</c>, <c>consent.missing</c> or
/// <c>dsr.restriction_active</c></description></item>
/// </list>
/// </para>
/// <para>
/// Evaluation order: the exception passed in, then <see cref="EncinaError.Exception"/>, then the error
/// code (<see cref="EncinaErrorExtensions.GetCode(EncinaError)"/>), then the error message. Anything
/// not matched is <see cref="ErrorClassification.Unknown"/> (treated as transient). Codes containing
/// "ratelimit" are also transient.
/// </para>
/// </remarks>
public sealed class DefaultErrorClassifier : IErrorClassifier
{
    private static readonly string[] PermanentErrorCodePatterns =
    [
        "validation",
        "not_found",
        "unauthorized",
        "forbidden",
        "invalid",
        "bad_request"
    ];

    private static readonly string[] TransientErrorCodePatterns =
    [
        "timeout",
        "unavailable",
        "connection",
        "network",
        "retry",
        "rate_limit",
        "throttle",
        "busy",
        "overload"
    ];

    // Matched against the error code only (never the free-text message, where these words are too
    // common): outcomes a retry cannot change, such as a missing handler, missing or withdrawn
    // consent, an active processing restriction, or a denied authorization.
    private static readonly string[] PermanentCodeOnlyPatterns =
    [
        "missing",
        "mismatch",
        "authorization",
        "consent.expired",
        "withdrawn",
        "reconsent",
        "restriction_active",
        "not_verified",
        "rejected",
        "exemption"
    ];

    private static readonly string[] TransientCodeOnlyPatterns =
    [
        "ratelimit"
    ];

    /// <inheritdoc />
    public ErrorClassification Classify(EncinaError encinaError, Exception? exception)
    {
        // First, check the exception type
        if (exception is not null)
        {
            var exceptionClassification = ClassifyException(exception);
            if (exceptionClassification != ErrorClassification.Unknown)
            {
                return exceptionClassification;
            }
        }

        // Also check the exception from the error itself
        var errorException = encinaError.Exception.MatchUnsafe(ex => ex, () => null);
        if (errorException is not null)
        {
            var exceptionClassification = ClassifyException(errorException);
            if (exceptionClassification != ErrorClassification.Unknown)
            {
                return exceptionClassification;
            }
        }

        // Then, check the error code for patterns
        var code = encinaError.GetCode().IfNone(string.Empty);
        var codeClassification = ClassifyErrorCode(code);
        if (codeClassification != ErrorClassification.Unknown)
        {
            return codeClassification;
        }

        // Finally, check the error message for patterns
        return ClassifyErrorMessage(encinaError.Message);
    }

    private static ErrorClassification ClassifyErrorCode(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return ErrorClassification.Unknown;
        }

        var lowerCode = code.ToLowerInvariant();

        if (ContainsAny(lowerCode, PermanentErrorCodePatterns) || ContainsAny(lowerCode, PermanentCodeOnlyPatterns))
        {
            return ErrorClassification.Permanent;
        }

        if (ContainsAny(lowerCode, TransientErrorCodePatterns) || ContainsAny(lowerCode, TransientCodeOnlyPatterns))
        {
            return ErrorClassification.Transient;
        }

        return ErrorClassification.Unknown;
    }

    private static bool ContainsAny(string value, string[] patterns)
    {
        foreach (var pattern in patterns)
        {
            if (value.Contains(pattern, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static ErrorClassification ClassifyException(Exception exception)
    {
        return exception switch
        {
            // Transient exceptions
            TimeoutException => ErrorClassification.Transient,
            TaskCanceledException => ErrorClassification.Transient,
            IOException => ErrorClassification.Transient,
            HttpRequestException httpEx => ClassifyHttpRequestException(httpEx),

            // Permanent exceptions
            ArgumentException => ErrorClassification.Permanent,
            InvalidOperationException => ErrorClassification.Permanent,
            NotSupportedException => ErrorClassification.Permanent,
            UnauthorizedAccessException => ErrorClassification.Permanent,
            FormatException => ErrorClassification.Permanent,

            // Check inner exception
            _ when exception.InnerException is not null => ClassifyException(exception.InnerException),

            // Unknown
            _ => ErrorClassification.Unknown
        };
    }

    private static ErrorClassification ClassifyHttpRequestException(HttpRequestException httpEx)
    {
        // Check status code if available
        if (httpEx.StatusCode.HasValue)
        {
            var statusCode = (int)httpEx.StatusCode.Value;

            // 5xx errors are transient
            if (statusCode >= 500 && statusCode <= 599)
            {
                return ErrorClassification.Transient;
            }

            // 429 Too Many Requests is transient
            if (statusCode == 429)
            {
                return ErrorClassification.Transient;
            }

            // 4xx errors are permanent (except 429)
            if (statusCode >= 400 && statusCode <= 499)
            {
                return ErrorClassification.Permanent;
            }
        }

        // Network errors without status code are transient
        return ErrorClassification.Transient;
    }

    private static ErrorClassification ClassifyErrorMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return ErrorClassification.Unknown;
        }

        var lowerMessage = message.ToLowerInvariant();

        // Check for permanent patterns first
        foreach (var pattern in PermanentErrorCodePatterns)
        {
            if (lowerMessage.Contains(pattern, StringComparison.Ordinal))
            {
                return ErrorClassification.Permanent;
            }
        }

        // Check for transient patterns
        foreach (var pattern in TransientErrorCodePatterns)
        {
            if (lowerMessage.Contains(pattern, StringComparison.Ordinal))
            {
                return ErrorClassification.Transient;
            }
        }

        return ErrorClassification.Unknown;
    }
}
