using LanguageExt;

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
/// <para>
/// All methods return <c>Either&lt;EncinaError, T&gt;</c> following the Railway Oriented Programming
/// pattern. Infrastructure failures are captured as <c>Left</c> values instead of throwing exceptions.
/// </para>
/// <para>
/// <b>Contract shared by every store.</b> The store is explicit: a query returns every tenant unless
/// the filter names one (<see cref="DeadLetterFilter.TenantId"/>); <see cref="DeadLetterFilter.AllTenants"/>
/// is ignored. "Now" comes from the store's own <see cref="TimeProvider"/>, never from the system clock.
/// Argument rules: <c>skip &gt;= 0</c>, <c>1 &lt;= take &lt;= <see cref="DeadLetterStoreLimits.MaxPageSize"/></c>,
/// <c>messageId != Guid.Empty</c> and a <c>replayResult</c> that is not null or whitespace; a violation
/// throws <see cref="ArgumentException"/> (or its subclasses) before any I/O.
/// </para>
/// </remarks>
public interface IDeadLetterStore
{
    /// <summary>
    /// Adds a message to the dead letter queue.
    /// </summary>
    /// <param name="message">The dead letter message to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Right(true) when stored; Right(false) when a message with the same
    /// <c>(SourcePattern, SourceMessageId)</c> already exists (already captured, not an error);
    /// Left(error) on infrastructure failure.
    /// </returns>
    Task<Either<EncinaError, bool>> AddAsync(IDeadLetterMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a dead letter message by its ID.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(Some(message)) if found; Right(None) if not found; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, Option<IDeadLetterMessage>>> GetAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dead letter messages with optional filtering.
    /// </summary>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="skip">Number of records to skip (for pagination).</param>
    /// <param name="take">Maximum number of records to return.</param>
    /// <param name="newestFirst">
    /// <c>false</c> (default) returns the oldest first; <c>true</c> the newest first.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Right(messages) on success, ordered by <c>DeadLetteredAtUtc</c> then <c>Id</c>, ascending
    /// (descending when <paramref name="newestFirst"/>); this order is part of the contract.
    /// Left(error) on infrastructure failure.
    /// </returns>
    Task<Either<EncinaError, IEnumerable<IDeadLetterMessage>>> GetMessagesAsync(
        DeadLetterFilter? filter = null,
        int skip = 0,
        int take = 100,
        bool newestFirst = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of dead letter messages matching the filter.
    /// </summary>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(count) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, int>> GetCountAsync(
        DeadLetterFilter? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically claims a message for replay, before it is dispatched.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="claimExpiredBeforeUtc">
    /// A claim made at or before this instant no longer excludes other replays (the manager passes
    /// <c>now - DeadLetterOptions.ReplayClaimTimeout</c>).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Right(true) only to the caller that won the claim: one conditional update that sets
    /// <c>ReplayClaimedAtUtc</c> when <c>ReplayedAtUtc IS NULL AND (ReplayClaimedAtUtc IS NULL OR
    /// ReplayClaimedAtUtc &lt;= claimExpiredBeforeUtc)</c>. Right(false) when the message does not exist,
    /// was already replayed or holds a live claim. Left(error) on infrastructure failure.
    /// </returns>
    Task<Either<EncinaError, bool>> TryClaimForReplayAsync(
        Guid messageId,
        DateTime claimExpiredBeforeUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as replayed.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="replayResult">
    /// The outcome code of the replay attempt (at most <see cref="DeadLetterStoreLimits.ReplayResultMaxLength"/>
    /// characters). Never error text.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Right(true) when updated. The update applies only while <c>ReplayedAtUtc IS NULL</c>:
    /// Right(false) when the message does not exist or an outcome was already recorded.
    /// Left(error) on infrastructure failure.
    /// </returns>
    Task<Either<EncinaError, bool>> MarkAsReplayedAsync(
        Guid messageId,
        string replayResult,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a dead letter message.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(true) if deleted; Right(false) if not found; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, bool>> DeleteAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes every message matching the filter in one set-based operation.
    /// </summary>
    /// <param name="filter">
    /// The filter. <see cref="DeadLetterFilter.All"/> deletes the whole queue in one statement, with no
    /// confirmation parameter: the caller owns that decision.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(count) of deleted messages; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, int>> DeleteManyAsync(DeadLetterFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Right(count) of deleted messages, where expired means <c>ExpiresAtUtc IS NOT NULL AND
    /// ExpiresAtUtc &lt;= now</c> with <c>now</c> from the store's <see cref="TimeProvider"/>;
    /// Left(error) on infrastructure failure.
    /// </returns>
    Task<Either<EncinaError, int>> DeleteExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes (for stores that support it like EF Core).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(Unit) on success; Left(error) on infrastructure failure.</returns>
    Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default);
}
