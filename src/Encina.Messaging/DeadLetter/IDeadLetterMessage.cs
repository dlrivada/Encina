namespace Encina.Messaging.DeadLetter;

/// <summary>
/// Represents a message that has permanently failed and is stored in the Dead Letter Queue.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a provider-agnostic abstraction for dead letter messages.
/// Implementations can use Entity Framework Core, Dapper, ADO.NET, or any custom storage.
/// </para>
/// <para>
/// Dead letter messages contain all the information needed to:
/// <list type="bullet">
/// <item><description>Understand why the message failed</description></item>
/// <item><description>Replay the message when the issue is resolved</description></item>
/// <item><description>Monitor and alert on DLQ accumulation</description></item>
/// </list>
/// </para>
/// <para>
/// The record never carries <c>EncinaError.Message</c> or <c>Exception.Message</c>: both can hold
/// personal data (#1274). Only the error code, the exception type and the stack trace are kept.
/// </para>
/// </remarks>
public interface IDeadLetterMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the dead letter message.
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified type name of the original request.
    /// </summary>
    string RequestType { get; set; }

    /// <summary>
    /// Gets or sets the serialized request content.
    /// </summary>
    string RequestContent { get; set; }

    /// <summary>
    /// Gets or sets the <c>EncinaError</c> code describing the failure (never the error message).
    /// </summary>
    string ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the exception type if an exception was thrown, otherwise null.
    /// </summary>
    string? ExceptionType { get; set; }

    /// <summary>
    /// Gets or sets the exception stack trace if an exception was thrown, otherwise null.
    /// </summary>
    string? ExceptionStackTrace { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID from the original request context.
    /// </summary>
    string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the source pattern that produced this dead letter.
    /// </summary>
    /// <remarks>
    /// Examples: "Outbox", "Inbox", "Recoverability", "Saga", "Scheduling".
    /// </remarks>
    string SourcePattern { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the source message, as a string.
    /// </summary>
    /// <remarks>
    /// Together with <see cref="SourcePattern"/> it is the idempotency key of the queue: a store
    /// holds at most one dead letter per <c>(SourcePattern, SourceMessageId)</c>. When the caller
    /// has no source identifier, the orchestrator uses the dead letter <see cref="Id"/>.
    /// </remarks>
    string SourceMessageId { get; set; }

    /// <summary>
    /// Gets or sets the tenant the failed message belonged to, or null when it had none.
    /// </summary>
    string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the total number of retry attempts made before dead lettering.
    /// </summary>
    int TotalRetryAttempts { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the message first failed.
    /// </summary>
    DateTime FirstFailedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the message was moved to DLQ.
    /// </summary>
    DateTime DeadLetteredAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the message expires and can be cleaned up.
    /// </summary>
    DateTime? ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when a replay claimed the message, or null when it is not claimed.
    /// </summary>
    /// <remarks>
    /// Set by the atomic claim that precedes a replay dispatch. A claim older than
    /// <see cref="DeadLetterOptions.ReplayClaimTimeout"/> no longer excludes other replays.
    /// </remarks>
    DateTime? ReplayClaimedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the message was replayed, if applicable.
    /// </summary>
    DateTime? ReplayedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the outcome code of the replay attempt, if applicable.
    /// </summary>
    /// <remarks>
    /// An outcome code only (<c>success</c>, <c>dlq.replay_failed</c> or an <c>EncinaError</c> code),
    /// never error text. At most <see cref="DeadLetterStoreLimits.ReplayResultMaxLength"/> characters.
    /// </remarks>
    string? ReplayResult { get; set; }

    /// <summary>
    /// Gets a value indicating whether this message has been replayed.
    /// </summary>
    bool IsReplayed { get; }

    /// <summary>
    /// Gets a value indicating whether this message has expired at the given instant.
    /// </summary>
    /// <param name="utcNow">The instant to evaluate, in UTC.</param>
    /// <returns>
    /// <c>true</c> when <see cref="ExpiresAtUtc"/> is set and <c>ExpiresAtUtc &lt;= utcNow</c>: a message
    /// is expired at the very instant of its expiry. This is the rule every store applies in SQL
    /// and in MongoDB filters.
    /// </returns>
    bool IsExpiredAt(DateTime utcNow);
}
