using LanguageExt;

namespace Encina.Marten.Snapshots;

/// <summary>
/// Storage interface for aggregate snapshots.
/// </summary>
/// <typeparam name="TAggregate">The aggregate type.</typeparam>
public interface ISnapshotStore<TAggregate>
    where TAggregate : class, IAggregate, ISnapshotable<TAggregate>
{
    /// <summary>
    /// Retrieves the latest snapshot for an aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Either an error or the snapshot if found.
    /// Returns Right(null) if no snapshot exists (not an error condition).
    /// </returns>
    Task<Either<EncinaError, Snapshot<TAggregate>?>> GetLatestAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a snapshot at or before a specific version.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="maxVersion">The maximum version to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Either an error or the snapshot if found.
    /// Returns Right(null) if no snapshot exists at or before the specified version.
    /// </returns>
    Task<Either<EncinaError, Snapshot<TAggregate>?>> GetAtVersionAsync(
        Guid aggregateId,
        int maxVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a snapshot for an aggregate.
    /// </summary>
    /// <param name="snapshot">The snapshot to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    Task<Either<EncinaError, Unit>> SaveAsync(
        Snapshot<TAggregate> snapshot,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old snapshots for an aggregate, keeping only the most recent ones.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="keepCount">Number of snapshots to retain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Either an error or the number of snapshots deleted.</returns>
    Task<Either<EncinaError, int>> PruneAsync(
        Guid aggregateId,
        int keepCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all snapshots for an aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Either an error or the number of snapshots deleted.</returns>
    Task<Either<EncinaError, int>> DeleteAllAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a snapshot exists for an aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Either an error or true if a snapshot exists.</returns>
    Task<Either<EncinaError, bool>> ExistsAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of snapshots for an aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Either an error or the snapshot count.</returns>
    Task<Either<EncinaError, int>> CountAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default);
}
