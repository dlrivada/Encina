using LanguageExt;

namespace Encina.EventStoreDB;

/// <summary>
/// Repository interface for loading and saving event-sourced aggregates.
/// </summary>
/// <typeparam name="TAggregate">The aggregate type.</typeparam>
public interface IAggregateRepository<TAggregate>
    where TAggregate : class, IAggregate
{
    /// <summary>
    /// Loads an aggregate by its identifier.
    /// </summary>
    /// <param name="id">The aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the loaded aggregate.</returns>
    Task<Either<MediatorError, TAggregate>> LoadAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads an aggregate by its identifier at a specific version.
    /// </summary>
    /// <param name="id">The aggregate identifier.</param>
    /// <param name="version">The version to load up to (inclusive).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the loaded aggregate.</returns>
    Task<Either<MediatorError, TAggregate>> LoadAsync(
        Guid id,
        long version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an aggregate and its uncommitted events to the event store.
    /// </summary>
    /// <param name="aggregate">The aggregate to save.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    Task<Either<MediatorError, Unit>> SaveAsync(
        TAggregate aggregate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new aggregate stream with the initial events.
    /// </summary>
    /// <param name="aggregate">The new aggregate to create.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    Task<Either<MediatorError, Unit>> CreateAsync(
        TAggregate aggregate,
        CancellationToken cancellationToken = default);
}
