namespace Encina.Messaging.DeadLetter;

/// <summary>
/// Abstraction for storing and retrieving dead letter messages.
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
public interface IDeadLetterStore
{
    /// <summary>
    /// Adds a message to the dead letter queue.
    /// </summary>
    /// <param name="message">The dead letter message to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(IDeadLetterMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a dead letter message by its ID.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The message if found, otherwise null.</returns>
    Task<IDeadLetterMessage?> GetAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dead letter messages with optional filtering.
    /// </summary>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="skip">Number of records to skip (for pagination).</param>
    /// <param name="take">Maximum number of records to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of dead letter messages.</returns>
    Task<IEnumerable<IDeadLetterMessage>> GetMessagesAsync(
        DeadLetterFilter? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of dead letter messages matching the filter.
    /// </summary>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of matching messages.</returns>
    Task<int> GetCountAsync(
        DeadLetterFilter? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as replayed.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="replayResult">The result of the replay attempt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkAsReplayedAsync(
        Guid messageId,
        string replayResult,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a dead letter message.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the message was found and deleted.</returns>
    Task<bool> DeleteAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of messages deleted.</returns>
    Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes (for stores that support it like EF Core).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
