using LanguageExt;

namespace Encina.Messaging.Recoverability;

/// <summary>
/// Represents a message that has permanently failed and is destined for the Dead Letter Queue.
/// </summary>
/// <remarks>
/// Contains all context needed to understand and potentially replay the failed message.
/// </remarks>
public sealed record FailedMessage
{
    /// <summary>
    /// Gets the unique identifier for this failure.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the original request that failed.
    /// </summary>
    public required object Request { get; init; }

    /// <summary>
    /// Gets the fully qualified type name of the request.
    /// </summary>
    public required string RequestType { get; init; }

    /// <summary>
    /// Gets the error that caused the final failure.
    /// </summary>
    public required EncinaError Error { get; init; }

    /// <summary>
    /// Gets the exception that caused the final failure, if any.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets the correlation ID from the original request context.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the idempotency key from the original request, if present.
    /// </summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>
    /// Gets the total number of retry attempts made.
    /// </summary>
    public required int TotalAttempts { get; init; }

    /// <summary>
    /// Gets the number of immediate retry attempts made.
    /// </summary>
    public required int ImmediateRetryAttempts { get; init; }

    /// <summary>
    /// Gets the number of delayed retry attempts made.
    /// </summary>
    public required int DelayedRetryAttempts { get; init; }

    /// <summary>
    /// Gets the timestamp when the message was first processed (UTC).
    /// </summary>
    public required DateTime FirstAttemptAtUtc { get; init; }

    /// <summary>
    /// Gets the timestamp when the message permanently failed (UTC).
    /// </summary>
    public required DateTime FailedAtUtc { get; init; }

    /// <summary>
    /// Gets the history of all errors encountered during retries.
    /// </summary>
    public IReadOnlyList<RetryAttempt> RetryHistory { get; init; } = [];
}

/// <summary>
/// Represents a single retry attempt in the failure history.
/// </summary>
public sealed record RetryAttempt
{
    /// <summary>
    /// Gets the attempt number (1-based).
    /// </summary>
    public required int AttemptNumber { get; init; }

    /// <summary>
    /// Gets whether this was an immediate retry (true) or delayed retry (false).
    /// </summary>
    public required bool IsImmediate { get; init; }

    /// <summary>
    /// Gets the timestamp of this attempt (UTC).
    /// </summary>
    public required DateTime AttemptedAtUtc { get; init; }

    /// <summary>
    /// Gets the error from this attempt.
    /// </summary>
    public required EncinaError Error { get; init; }

    /// <summary>
    /// Gets the exception from this attempt, if any.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets the classification of the error.
    /// </summary>
    public required ErrorClassification Classification { get; init; }

    /// <summary>
    /// Gets how long this attempt took.
    /// </summary>
    public TimeSpan? Duration { get; init; }
}
