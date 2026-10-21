namespace Encina.Recoverability;

/// <summary>
/// Classifies errors to determine retry behavior.
/// </summary>
/// <remarks>
/// <para>
/// The classifier lives in core so that every retry layer (the Polly retry behavior, the recoverability
/// pipeline, the delayed-retry processor, the outbox processor and the job adapters) decides through the
/// same contract; register a custom implementation to classify your own error codes.
/// </para>
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
/// <item><description>The explicit error codes listed below (exact match, case-insensitive)</description></item>
/// <item><description>Error messages containing "validation", "not_found", "unauthorized", "forbidden", "invalid", "bad_request"</description></item>
/// </list>
/// </para>
/// <para>
/// Evaluation order:
/// <list type="number">
/// <item><description>The <c>transient</c> metadata (<see cref="OutboundErrorMetadataKeys.Transient"/>) that an
/// outbound producer such as <c>Encina.Refit</c> declares: <see langword="true"/> is
/// <see cref="ErrorClassification.Transient"/>, <see langword="false"/> is
/// <see cref="ErrorClassification.Permanent"/>. An explicit declaration wins over inference.</description></item>
/// <item><description>Connection-pool exhaustion anywhere in the exception chain (SqlClient "max pool size was
/// reached", Npgsql "connection pool has been exhausted", MySqlConnector "All pooled connections are in use";
/// the MongoDB driver raises a <see cref="TimeoutException"/>, transient by type):
/// <see cref="ErrorClassification.Transient"/>, because the pool recovers when a connection is returned. The
/// exception message is only matched, never logged or copied. The drivers localize their messages, so the match
/// works for the English text; register a custom <see cref="IErrorClassifier"/> for another culture.</description></item>
/// <item><description>The exception passed in, then <see cref="EncinaError.Exception"/>, by type.</description></item>
/// <item><description>The error code (<see cref="EncinaErrorExtensions.GetCode(EncinaError)"/>), against an explicit
/// list of codes whose outcome a retry cannot change (a missing handler, missing, expired or withdrawn
/// consent, an active processing restriction, a missing data subject id, a denied authorization, a failed
/// validation) and an explicit list of transient codes (<c>encina.timeout</c>, <c>encina.ratelimit.exceeded</c>).
/// Codes are never matched by substring: <c>saga.not_found</c> or <c>marten.aggregate_not_found</c> can be an
/// out-of-order event that succeeds on a later attempt, so they are not permanent by code.</description></item>
/// <item><description>The error message, by pattern, but only when the error has no causing exception: a message
/// built around an exception (for example the dispatcher's <c>encina.notification.exception</c> message, which
/// names the handler type) is not free text about the failure, so words such as "invalid" in a handler name
/// must not make the failure permanent.</description></item>
/// </list>
/// Anything not matched is <see cref="ErrorClassification.Unknown"/> (treated as transient).
/// </para>
/// </remarks>
public sealed class DefaultErrorClassifier : IErrorClassifier
{
    private static readonly string[] PermanentMessagePatterns =
    [
        "validation",
        "not_found",
        "unauthorized",
        "forbidden",
        "invalid",
        "bad_request"
    ];

    private static readonly string[] TransientMessagePatterns =
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

    // Exact error codes (case-insensitive) whose outcome a retry cannot change. Kept as an explicit
    // list on purpose: substring matching on codes turned "*.not_found" and "*.invalid_*" codes that
    // can succeed on a later attempt (out-of-order events, saga state races) into permanent failures.
    private static readonly HashSet<string> PermanentErrorCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Encina core: no handler, wrong handler, authorization denied
        EncinaErrorCodes.HandlerMissing,
        EncinaErrorCodes.RequestHandlerMissing,
        EncinaErrorCodes.RequestHandlerTypeMismatch,
        EncinaErrorCodes.NotificationMissingHandle,
        EncinaErrorCodes.AuthorizationUnauthorized,
        EncinaErrorCodes.AuthorizationUnauthenticated,
        EncinaErrorCodes.AuthorizationForbidden,
        EncinaErrorCodes.AuthorizationPolicyFailed,
        EncinaErrorCodes.AuthorizationResourceDenied,
        EncinaErrorCodes.AuthorizationInsufficientRoles,
        EncinaErrorCodes.AuthorizationPermissionDenied,
        EncinaErrorCodes.AuthorizationClaimMissing,
        EncinaErrorCodes.AuthorizationNotOwner,

        // Validation failures (Encina.GuardClauses, Encina.DomainModeling, compliance modules)
        "encina.guard.validation_failed",
        "repository.validationfailed",
        "processor.validation_failed",
        "gdpr.compliance_validation_failed",
        "aiact.compliance_validation_failed",

        // Encina.Compliance.Consent
        "consent.missing",
        "consent.expired",
        "consent.withdrawn",
        "consent.requires_reconsent",
        "consent.version_mismatch",

        // Encina.Compliance.DataSubjectRights
        "dsr.restriction_active",
        "dsr.subject_id_missing",
        "dsr.identity_not_verified"
    };

    // Exact error codes (case-insensitive) that are transient.
    private static readonly HashSet<string> TransientErrorCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        EncinaErrorCodes.Timeout,
        EncinaErrorCodes.RateLimitExceeded
    };

    // Messages (matched ordinally, ignoring case) that the drivers raise when no pooled connection becomes
    // available in time. Exception messages are only compared against these, never logged or copied.
    // SqlClient 7.0.0, Npgsql 10.0.2 and MySqlConnector 2.5.0 texts (English: the drivers localize their messages).
    // The MongoDB driver raises a TimeoutException, which is transient by type.
    private static readonly string[] PoolExhaustionMarkers =
    [
        "max pool size was reached",
        "connection pool has been exhausted",
        "all pooled connections are in use"
    ];

    /// <inheritdoc />
    public ErrorClassification Classify(EncinaError encinaError, Exception? exception)
    {
        // An explicit declaration of the producer wins over inference.
        var declared = ClassifyDeclaredTransience(encinaError);
        if (declared != ErrorClassification.Unknown)
        {
            return declared;
        }

        // Then, the exceptions and the error code
        var known = ClassifyExceptionsAndCode(encinaError, exception);
        if (known != ErrorClassification.Unknown)
        {
            return known;
        }

        // Finally, check the error message for patterns, unless the error has a causing exception:
        // then the message was built around that exception (for example by the dispatcher, naming
        // the handler type) and its words say nothing about whether a retry can succeed.
        if (HasCause(encinaError, exception))
        {
            return ErrorClassification.Unknown;
        }

        return ClassifyErrorMessage(encinaError.Message);
    }

    private static ErrorClassification ClassifyExceptionsAndCode(EncinaError encinaError, Exception? exception)
    {
        // A pool recovers when a connection is returned: exhaustion is transient although the driver
        // raises an InvalidOperationException, which is otherwise permanent.
        if (IsPoolExhaustion(exception) || IsPoolExhaustion(encinaError.Exception.MatchUnsafe(ex => ex, () => null)))
        {
            return ErrorClassification.Transient;
        }

        var fromExceptions = ClassifyExceptions(encinaError, exception);
        return fromExceptions != ErrorClassification.Unknown
            ? fromExceptions
            : ClassifyErrorCode(encinaError.GetCode().IfNone(string.Empty));
    }

    private static ErrorClassification ClassifyExceptions(EncinaError encinaError, Exception? exception)
    {
        var fromArgument = exception is null ? ErrorClassification.Unknown : ClassifyException(exception);
        if (fromArgument != ErrorClassification.Unknown)
        {
            return fromArgument;
        }

        var errorException = encinaError.Exception.MatchUnsafe(ex => ex, () => null);
        return errorException is null ? ErrorClassification.Unknown : ClassifyException(errorException);
    }

    private static ErrorClassification ClassifyDeclaredTransience(EncinaError encinaError)
    {
        var details = encinaError.GetDetails();
        if (details.Count == 0
            || !details.TryGetValue(OutboundErrorMetadataKeys.Transient, out var value)
            || value is not bool transient)
        {
            return ErrorClassification.Unknown;
        }

        return transient ? ErrorClassification.Transient : ErrorClassification.Permanent;
    }

    private static bool IsPoolExhaustion(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (MatchesPoolExhaustion(current.Message))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesPoolExhaustion(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return false;
        }

        foreach (var marker in PoolExhaustionMarkers)
        {
            if (message.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasCause(EncinaError encinaError, Exception? exception)
    {
        if (encinaError.GetCause().IsSome)
        {
            return true;
        }

        // Without a cause, EncinaError.Exception is the internal carrier of the code and message;
        // callers that pass it back in (for example the recoverability pipeline) do not pass a cause.
        return exception is not null
            && !encinaError.Exception.Exists(carrier => ReferenceEquals(carrier, exception));
    }

    private static ErrorClassification ClassifyErrorCode(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return ErrorClassification.Unknown;
        }

        if (PermanentErrorCodes.Contains(code))
        {
            return ErrorClassification.Permanent;
        }

        if (TransientErrorCodes.Contains(code))
        {
            return ErrorClassification.Transient;
        }

        return ErrorClassification.Unknown;
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
        // Network errors without a status code are transient
        return httpEx.StatusCode is { } status ? ClassifyStatusCode((int)status) : ErrorClassification.Transient;
    }

    // crap-exempt: single-question switch — whether a retry can change the outcome of each HTTP status.
    private static ErrorClassification ClassifyStatusCode(int statusCode) => statusCode switch
    {
        // 5xx, 408 Request Timeout (the client MAY repeat) and 429 Too Many Requests are transient
        >= 500 and <= 599 or 408 or 429 => ErrorClassification.Transient,
        // other 4xx are permanent
        >= 400 and <= 499 => ErrorClassification.Permanent,
        _ => ErrorClassification.Transient
    };

    private static ErrorClassification ClassifyErrorMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return ErrorClassification.Unknown;
        }

        var lowerMessage = message.ToLowerInvariant();

        // Check for permanent patterns first
        foreach (var pattern in PermanentMessagePatterns)
        {
            if (lowerMessage.Contains(pattern, StringComparison.Ordinal))
            {
                return ErrorClassification.Permanent;
            }
        }

        // Check for transient patterns
        foreach (var pattern in TransientMessagePatterns)
        {
            if (lowerMessage.Contains(pattern, StringComparison.Ordinal))
            {
                return ErrorClassification.Transient;
            }
        }

        return ErrorClassification.Unknown;
    }
}
