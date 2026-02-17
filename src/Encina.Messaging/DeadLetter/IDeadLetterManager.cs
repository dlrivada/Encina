using LanguageExt;

namespace Encina.Messaging.DeadLetter;

/// <summary>
/// Provides management operations for the Dead Letter Queue.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides operations for:
/// <list type="bullet">
/// <item><description>Replaying failed messages</description></item>
/// <item><description>Querying and inspecting DLQ contents</description></item>
/// <item><description>Cleaning up old messages</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IDeadLetterManager
{
    /// <summary>
    /// Replays a single dead letter message.
    /// </summary>
    /// <param name="messageId">The message ID to replay.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right with success result, or Left with error details.</returns>
    Task<Either<EncinaError, ReplayResult>> ReplayAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replays all messages matching the specified filter.
    /// </summary>
    /// <param name="filter">Filter criteria for messages to replay.</param>
    /// <param name="maxMessages">Maximum number of messages to replay. Default: 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right with batch replay result, or Left with error details.</returns>
    Task<Either<EncinaError, BatchReplayResult>> ReplayAllAsync(
        DeadLetterFilter filter,
        int maxMessages = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a dead letter message by ID.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The message if found, otherwise null.</returns>
    Task<IDeadLetterMessage?> GetMessageAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dead letter messages with optional filtering and pagination.
    /// </summary>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Maximum number of records to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of dead letter messages.</returns>
    Task<IEnumerable<IDeadLetterMessage>> GetMessagesAsync(
        DeadLetterFilter? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of messages matching the filter.
    /// </summary>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of matching messages.</returns>
    Task<int> GetCountAsync(
        DeadLetterFilter? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about the Dead Letter Queue.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>DLQ statistics.</returns>
    Task<DeadLetterStatistics> GetStatisticsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a dead letter message.
    /// </summary>
    /// <param name="messageId">The message ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the message was found and deleted.</returns>
    Task<bool> DeleteAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all messages matching the filter.
    /// </summary>
    /// <param name="filter">Filter criteria for messages to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of messages deleted.</returns>
    Task<int> DeleteAllAsync(
        DeadLetterFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of messages cleaned up.</returns>
    Task<int> CleanupExpiredAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of replaying a single dead letter message.
/// </summary>
public sealed record ReplayResult
{
    /// <summary>
    /// Gets the message ID that was replayed.
    /// </summary>
    public required Guid MessageId { get; init; }

    /// <summary>
    /// Gets whether the replay was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the error message if replay failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the timestamp when replay was completed.
    /// </summary>
    public required DateTime ReplayedAtUtc { get; init; }

    /// <summary>
    /// Creates a successful replay result.
    /// </summary>
    public static ReplayResult Succeeded(Guid messageId) => new()
    {
        MessageId = messageId,
        Success = true,
        ReplayedAtUtc = TimeProvider.System.GetUtcNow().UtcDateTime
    };

    /// <summary>
    /// Creates a failed replay result.
    /// </summary>
    public static ReplayResult Failed(Guid messageId, string errorMessage) => new()
    {
        MessageId = messageId,
        Success = false,
        ErrorMessage = errorMessage,
        ReplayedAtUtc = TimeProvider.System.GetUtcNow().UtcDateTime
    };
}

/// <summary>
/// Result of replaying multiple dead letter messages.
/// </summary>
public sealed record BatchReplayResult
{
    /// <summary>
    /// Gets the total number of messages processed.
    /// </summary>
    public required int TotalProcessed { get; init; }

    /// <summary>
    /// Gets the number of messages successfully replayed.
    /// </summary>
    public required int SuccessCount { get; init; }

    /// <summary>
    /// Gets the number of messages that failed replay.
    /// </summary>
    public required int FailureCount { get; init; }

    /// <summary>
    /// Gets the individual replay results.
    /// </summary>
    public required IReadOnlyList<ReplayResult> Results { get; init; }

    /// <summary>
    /// Gets whether all replays were successful.
    /// </summary>
    public bool AllSucceeded => FailureCount == 0 && TotalProcessed > 0;
}

/// <summary>
/// Statistics about the Dead Letter Queue.
/// </summary>
public sealed record DeadLetterStatistics
{
    /// <summary>
    /// Gets the total number of messages in the DLQ.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Gets the number of messages pending replay.
    /// </summary>
    public required int PendingCount { get; init; }

    /// <summary>
    /// Gets the number of messages that have been replayed.
    /// </summary>
    public required int ReplayedCount { get; init; }

    /// <summary>
    /// Gets the number of expired messages.
    /// </summary>
    public required int ExpiredCount { get; init; }

    /// <summary>
    /// Gets the count by source pattern.
    /// </summary>
    public required IReadOnlyDictionary<string, int> CountBySource { get; init; }

    /// <summary>
    /// Gets the oldest pending message timestamp.
    /// </summary>
    public DateTime? OldestPendingAtUtc { get; init; }

    /// <summary>
    /// Gets the newest pending message timestamp.
    /// </summary>
    public DateTime? NewestPendingAtUtc { get; init; }
}
