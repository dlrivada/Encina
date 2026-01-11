namespace Encina.Messaging.Outbox;

/// <summary>
/// Abstraction for storing and retrieving outbox messages.
/// </summary>
/// <remarks>
/// <para>
/// This interface allows different persistence implementations:
/// <list type="bullet">
/// <item><description><b>Entity Framework Core</b>: Full ORM with change tracking</description></item>
/// <item><description><b>Dapper</b>: Lightweight micro-ORM with SQL control</description></item>
/// <item><description><b>ADO.NET</b>: Maximum performance, full control</description></item>
/// <item><description><b>Custom</b>: NoSQL, message queues, etc.</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IOutboxStore
{
    /// <summary>
    /// Adds a message to the outbox.
    /// </summary>
    /// <param name="message">The outbox message to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(IOutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending messages that are ready to be processed.
    /// </summary>
    /// <param name="batchSize">Maximum number of messages to retrieve.</param>
    /// <param name="maxRetries">Maximum number of retries before dead lettering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of pending messages.</returns>
    Task<IEnumerable<IOutboxMessage>> GetPendingMessagesAsync(
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
    /// <param name="nextRetryAtUtc">When to retry next (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkAsFailedAsync(
        Guid messageId,
        string errorMessage,
        DateTime? nextRetryAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes (for stores that support it like EF Core).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
