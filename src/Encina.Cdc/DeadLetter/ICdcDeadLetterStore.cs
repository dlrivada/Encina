using LanguageExt;

namespace Encina.Cdc.DeadLetter;

/// <summary>
/// Provides persistent storage for CDC events that failed processing after
/// exhausting all retry attempts. Enables inspection, replay, and resolution
/// of failed events.
/// </summary>
/// <remarks>
/// <para>
/// This is a CDC-specific dead letter store, separate from
/// <c>Encina.Messaging.DeadLetter.IDeadLetterStore</c>, because CDC events carry
/// unique metadata (position, connector ID, table name) that doesn't map to
/// messaging outbox columns.
/// </para>
/// <para>
/// All methods follow the Railway Oriented Programming (ROP) pattern, returning
/// <see cref="Either{L,R}"/> with <see cref="EncinaError"/> on the left channel.
/// </para>
/// </remarks>
public interface ICdcDeadLetterStore
{
    /// <summary>
    /// Persists a failed CDC event into the dead letter store.
    /// </summary>
    /// <param name="entry">The dead letter entry containing the original event and error context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Either an error or <see cref="Unit"/> on success.</returns>
    Task<Either<EncinaError, Unit>> AddAsync(
        CdcDeadLetterEntry entry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves pending dead letter entries that have not yet been resolved.
    /// </summary>
    /// <param name="maxCount">The maximum number of entries to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Either an error or a read-only list of pending entries.</returns>
    Task<Either<EncinaError, IReadOnlyList<CdcDeadLetterEntry>>> GetPendingAsync(
        int maxCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a dead letter entry by marking it as replayed or discarded.
    /// </summary>
    /// <param name="id">The unique identifier of the dead letter entry to resolve.</param>
    /// <param name="resolution">The resolution to apply (<see cref="CdcDeadLetterResolution.Replay"/> or <see cref="CdcDeadLetterResolution.Discard"/>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Either an error or <see cref="Unit"/> on success.</returns>
    Task<Either<EncinaError, Unit>> ResolveAsync(
        Guid id,
        CdcDeadLetterResolution resolution,
        CancellationToken cancellationToken = default);
}
