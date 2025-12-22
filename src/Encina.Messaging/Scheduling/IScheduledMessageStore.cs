namespace Encina.Messaging.Scheduling;

/// <summary>
/// Abstraction for storing and retrieving scheduled messages.
/// </summary>
/// <remarks>
/// <para>
/// This interface allows different persistence implementations for message scheduling:
/// <list type="bullet">
/// <item><description><b>Entity Framework Core</b>: Full ORM with change tracking</description></item>
/// <item><description><b>Dapper</b>: Lightweight micro-ORM with SQL control</description></item>
/// <item><description><b>ADO.NET</b>: Maximum performance, full control</description></item>
/// <item><description><b>Custom</b>: Redis, distributed schedulers, etc.</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IScheduledMessageStore
{
    /// <summary>
    /// Adds a new scheduled message.
    /// </summary>
    /// <param name="message">The scheduled message to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(IScheduledMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets messages that are due for execution.
    /// </summary>
    /// <param name="batchSize">Maximum number of messages to retrieve.</param>
    /// <param name="maxRetries">Maximum number of retries before dead lettering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of due messages.</returns>
    Task<IEnumerable<IScheduledMessage>> GetDueMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as processed.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as failed and schedules retry.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="nextRetryAt">When to retry next.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkAsFailedAsync(
        Guid messageId,
        string errorMessage,
        DateTime? nextRetryAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reschedules a recurring message for its next execution.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="nextScheduledAt">When to execute next.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RescheduleRecurringMessageAsync(
        Guid messageId,
        DateTime nextScheduledAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a scheduled message.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CancelAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes (for stores that support it like EF Core).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
