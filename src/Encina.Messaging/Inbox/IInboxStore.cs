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
    /// Adds a new message to the inbox, <b>outside</b> the business transaction.
    /// </summary>
    /// <remarks>
    /// The entry is committed on its own before the handler runs, so the failure records that follow have a row
    /// to update even when the business transaction rolls back (ADR-048).
    /// </remarks>
    /// <param name="message">The inbox message to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(Unit) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, Unit>> AddAsync(IInboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as processed with a successful response, <b>atomically with the business transaction</b>.
    /// </summary>
    /// <remarks>
    /// When the request runs inside a business transaction (the Transaction pattern), the store enlists this write
    /// in it: the message becomes processed if and only if the business effect commits, so a failed commit leaves
    /// the message unprocessed and the redelivery runs the handler again. Without an active transaction the write
    /// is immediate. A handler that returned a business <c>Left</c> uses
    /// <see cref="CacheHandlerErrorAsync"/> instead, because the transaction rolls back on a <c>Left</c>. See ADR-048.
    /// </remarks>
    /// <param name="messageId">The message ID.</param>
    /// <param name="response">The serialized response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(Unit) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, Unit>> MarkAsProcessedAsync(string messageId, string response, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a handler's business <c>Left</c> as the processed response, <b>outside</b> the business transaction.
    /// </summary>
    /// <remarks>
    /// The business transaction rolls back on a <c>Left</c>; the cached response must survive that rollback so the
    /// redelivery returns the same <c>Left</c> without running the handler again. The write is durable on its own
    /// (a separate connection or unit of work while a business transaction is active). See ADR-048.
    /// </remarks>
    /// <param name="messageId">The message ID.</param>
    /// <param name="response">The serialized response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(Unit) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, Unit>> CacheHandlerErrorAsync(string messageId, string response, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as failed and increments its retry count by exactly one, <b>outside</b> the business transaction.
    /// </summary>
    /// <remarks>
    /// This is the only place where <c>RetryCount</c> grows: one call per failed handler attempt. The write is
    /// durable on its own, so it survives the rollback of the business transaction that the failed attempt causes
    /// (ADR-048). <see cref="AddAsync"/> is durable in the same way.
    /// </remarks>
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
