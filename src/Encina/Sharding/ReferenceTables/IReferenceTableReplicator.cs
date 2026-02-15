using LanguageExt;

namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// Manages replication of reference table data from the primary shard to all
/// target shards in the topology.
/// </summary>
/// <remarks>
/// <para>
/// The replicator is the main entry point for triggering reference table synchronization.
/// It reads all data from the primary shard and upserts it to every other active shard,
/// returning a <see cref="ReplicationResult"/> with per-shard outcomes.
/// </para>
/// <para>
/// For <see cref="RefreshStrategy.Manual"/> tables, callers invoke
/// <see cref="ReplicateAsync{TEntity}"/> directly. For <see cref="RefreshStrategy.CdcDriven"/>
/// and <see cref="RefreshStrategy.Polling"/>, the background service invokes replication
/// automatically.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Replicate a single reference table
/// var result = await replicator.ReplicateAsync&lt;Country&gt;(ct);
/// result.Match(
///     Right: r => logger.LogInformation("Synced {Rows} country rows", r.RowsSynced),
///     Left: e => logger.LogError("Country replication failed: {Error}", e.Message));
///
/// // Replicate all registered reference tables
/// var allResult = await replicator.ReplicateAllAsync(ct);
/// </code>
/// </example>
public interface IReferenceTableReplicator
{
    /// <summary>
    /// Replicates a single reference table from the primary shard to all target shards.
    /// </summary>
    /// <typeparam name="TEntity">The entity type of the reference table to replicate.</typeparam>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with <see cref="ReplicationResult"/> on success (which may include partial failures);
    /// Left with <see cref="EncinaError"/> if the operation failed entirely.
    /// </returns>
    Task<Either<EncinaError, ReplicationResult>> ReplicateAsync<TEntity>(
        CancellationToken cancellationToken = default)
        where TEntity : class;

    /// <summary>
    /// Replicates all registered reference tables from their primary shards to all target shards.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with <see cref="ReplicationResult"/> summarizing all tables on success;
    /// Left with <see cref="EncinaError"/> if the operation failed entirely.
    /// </returns>
    Task<Either<EncinaError, ReplicationResult>> ReplicateAllAsync(
        CancellationToken cancellationToken = default);
}
