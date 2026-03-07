using LanguageExt;

namespace Encina.Messaging.Inbox;

/// <summary>
/// Abstraction for storing and retrieving inbox messages.
/// </summary>
/// <remarks>
/// <para>
/// This interface allows different persistence implementations for the Inbox Pattern:
/// <list type="bullet">
/// <item><description><b>Entity Framework Core</b>: Full ORM with change tracking</description></item>
/// <item><description><b>Dapper</b>: Lightweight micro-ORM with SQL control</description></item>
/// <item><description><b>ADO.NET</b>: Maximum performance, full control</description></item>
/// <item><description><b>Custom</b>: Redis, distributed cache, etc.</description></item>
/// </list>
/// </para>
/// <para>
/// All methods return <c>Either&lt;EncinaError, T&gt;</c> following the Railway Oriented Programming
/// pattern. Infrastructure failures are captured as <c>Left</c> values instead of throwing exceptions.
/// </para>
/// </remarks>
public interface IInboxStore
{
    /// <summary>
    /// Checks if a message has already been processed.
    /// </summary>
    /// <param name="messageId">The message ID (IdempotencyKey).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(Some(message)) if found; Right(None) if not found; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, Option<IInboxMessage>>> GetMessageAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new message to the inbox.
    /// </summary>
    /// <param name="message">The inbox message to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(Unit) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, Unit>> AddAsync(IInboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as processed with a successful response.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="response">The serialized response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(Unit) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, Unit>> MarkAsProcessedAsync(string messageId, string response, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as failed.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="nextRetryAtUtc">When to retry next (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(Unit) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, Unit>> MarkAsFailedAsync(
        string messageId,
        string errorMessage,
        DateTime? nextRetryAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the retry count for a message.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(Unit) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, Unit>> IncrementRetryCountAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expired messages that can be cleaned up.
    /// </summary>
    /// <param name="batchSize">Maximum number of messages to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(messages) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, IEnumerable<IInboxMessage>>> GetExpiredMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes expired messages from the inbox.
    /// </summary>
    /// <param name="messageIds">The message IDs to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(Unit) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, Unit>> RemoveExpiredMessagesAsync(
        IEnumerable<string> messageIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes (for stores that support it like EF Core).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(Unit) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default);
}
