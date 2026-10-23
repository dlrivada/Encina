using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Recoverability;

/// <summary>
/// Counters of the retry layers on the <c>Encina</c> meter: <c>encina.retry.attempts_total</c> and
/// <c>encina.retry.skipped_total</c>.
/// </summary>
/// <remarks>
/// Dimensions are bounded and carry no personal data: the request name, an error code, a reason, and the tenant
/// id (<c>encina.tenant_id</c>, omitted when there is none). The text of an error or an exception, a response
/// body and a URL are never a dimension (SPEC-002 REQ-062).
/// </remarks>
public static class RetryMetrics
{
    /// <summary>Reason tag of a retry skipped because the error is permanent.</summary>
    public const string ReasonPermanent = "permanent";

    /// <summary>Reason tag of a retry skipped because the failure is ambiguous on a non-idempotent send and no opt-in holds.</summary>
    public const string ReasonNonIdempotent = "non_idempotent";

    /// <summary>Reason tag of a retry skipped in process because <c>Retry-After</c> exceeds the maximum delay.</summary>
    public const string ReasonRetryAfterExceedsMax = "retry_after_exceeds_max";

    /// <summary>Reason tag of a retry skipped in a messaging layer because <c>Retry-After</c> exceeds the configured ceiling and the message is dead-lettered.</summary>
    public const string ReasonRetryAfterExceedsCeiling = "retry_after_exceeds_ceiling";

    private static readonly Meter Meter = new("Encina", "1.0");

    private static readonly Counter<long> Attempts = Meter.CreateCounter<long>(
        "encina.retry.attempts_total",
        unit: "{attempts}",
        description: "Total number of retries performed after a failed attempt.");

    private static readonly Counter<long> Skipped = Meter.CreateCounter<long>(
        "encina.retry.skipped_total",
        unit: "{retries}",
        description: "Total number of retries that were not performed, by reason.");

    /// <summary>
    /// Counts one retry that is about to be performed.
    /// </summary>
    /// <param name="requestName">The request type name.</param>
    /// <param name="errorCode">The code of the error that caused the retry.</param>
    /// <param name="tenantId">The tenant of the call, or <see langword="null"/>.</param>
    public static void RecordAttempt(string requestName, string errorCode, string? tenantId)
    {
        ArgumentNullException.ThrowIfNull(requestName);
        ArgumentNullException.ThrowIfNull(errorCode);

        Attempts.Add(1, Tags(requestName, errorCode, tenantId, reason: null));
    }

    /// <summary>
    /// Counts one retry that was not performed.
    /// </summary>
    /// <param name="reason">
    /// One of <see cref="ReasonPermanent"/>, <see cref="ReasonNonIdempotent"/>, <see cref="ReasonRetryAfterExceedsMax"/>
    /// and <see cref="ReasonRetryAfterExceedsCeiling"/>.
    /// </param>
    /// <param name="requestName">The request type name.</param>
    /// <param name="errorCode">The code of the error that was not retried.</param>
    /// <param name="tenantId">The tenant of the call, or <see langword="null"/>.</param>
    public static void RecordSkipped(string reason, string requestName, string errorCode, string? tenantId)
    {
        ArgumentNullException.ThrowIfNull(reason);
        ArgumentNullException.ThrowIfNull(requestName);
        ArgumentNullException.ThrowIfNull(errorCode);

        Skipped.Add(1, Tags(requestName, errorCode, tenantId, reason));
    }

    private static TagList Tags(string requestName, string errorCode, string? tenantId, string? reason)
    {
        var tags = new TagList
        {
            { "request_name", requestName },
            { "error_code", errorCode }
        };

        if (reason is not null)
        {
            tags.Add("reason", reason);
        }

        if (tenantId is not null)
        {
            tags.Add("encina.tenant_id", tenantId);
        }

        return tags;
    }
}
