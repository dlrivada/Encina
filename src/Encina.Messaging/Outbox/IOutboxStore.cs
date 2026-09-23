using LanguageExt;

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
/// <para>
/// All methods return <c>Either&lt;EncinaError, T&gt;</c> following the Railway Oriented Programming
/// pattern. Infrastructure failures are captured as <c>Left</c> values instead of throwing exceptions.
/// </para>
/// </remarks>
public interface IOutboxStore
{
    /// <summary>
    /// Adds a message to the outbox.
    /// </summary>
    /// <param name="message">The outbox message to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(Unit) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, Unit>> AddAsync(IOutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending messages that are ready to be processed.
    /// </summary>
    /// <param name="batchSize">Maximum number of messages to retrieve.</param>
    /// <param name="maxRetries">Maximum number of retries before dead lettering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(messages) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, IEnumerable<IOutboxMessage>>> GetPendingMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as processed.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(Unit) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, Unit>> MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as failed and schedules retry.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="nextRetryAtUtc">When to retry next (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(Unit) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, Unit>> MarkAsFailedAsync(
        Guid messageId,
        string errorMessage,
        DateTime? nextRetryAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the messages that are waiting to be delivered: not processed and with retries left
    /// (<c>RetryCount &lt; maxRetries</c>), whether they are due now or scheduled for a later retry.
    /// </summary>
    /// <param name="maxRetries">The retry limit (<see cref="OutboxOptions.MaxRetries"/>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(count) on success; Left(error) on infrastructure failure.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxRetries"/> is negative.</exception>
    Task<Either<EncinaError, int>> GetPendingCountAsync(
        int maxRetries,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the messages whose retries are exhausted: not processed and with
    /// <c>RetryCount &gt;= maxRetries</c>, the state <see cref="IOutboxMessage.IsDeadLettered"/> describes.
    /// </summary>
    /// <remarks>
    /// Exhausted messages stay in the outbox table and are never fetched again by
    /// <see cref="GetPendingMessagesAsync"/> until they are requeued with <see cref="RequeueExhaustedAsync"/>.
    /// </remarks>
    /// <param name="maxRetries">The retry limit (<see cref="OutboxOptions.MaxRetries"/>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(count) on success; Left(error) on infrastructure failure.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxRetries"/> is negative.</exception>
    Task<Either<EncinaError, int>> GetExhaustedCountAsync(
        int maxRetries,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns exhausted messages to the pending state so that the processor delivers them again:
    /// <c>RetryCount</c> is reset to 0 and <c>NextRetryAtUtc</c> and <c>ErrorMessage</c> are cleared.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only exhausted messages (not processed, <c>RetryCount &gt;= maxRetries</c>) are affected; an identifier
    /// of a message that is pending, processed or unknown is ignored.
    /// </para>
    /// <para>
    /// Stores with a unit of work (EF Core) record the change on their context, and it is persisted by
    /// <see cref="SaveChangesAsync"/>; the other stores apply it immediately.
    /// </para>
    /// </remarks>
    /// <param name="maxRetries">The retry limit (<see cref="OutboxOptions.MaxRetries"/>).</param>
    /// <param name="messageIds">
    /// The identifiers of the messages to requeue, or <see langword="null"/> to requeue every exhausted message.
    /// An empty collection requeues nothing.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(number of messages requeued) on success; Left(error) on infrastructure failure.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxRetries"/> is negative.</exception>
    Task<Either<EncinaError, int>> RequeueExhaustedAsync(
        int maxRetries,
        IReadOnlyCollection<Guid>? messageIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes (for stores that support it like EF Core).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(Unit) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default);
}
