namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// Contains the result of a reference table replication operation across shards,
/// including per-shard outcomes, timing, and error information.
/// </summary>
/// <param name="RowsSynced">The total number of rows synchronized across all target shards.</param>
/// <param name="Duration">The total duration of the replication operation.</param>
/// <param name="ShardResults">Per-shard replication outcomes.</param>
/// <param name="FailedShards">Shards that failed during replication, with error details.</param>
/// <remarks>
/// <para>
/// This record is returned inside <c>Either&lt;EncinaError, ReplicationResult&gt;</c> from
/// <see cref="IReferenceTableReplicator.ReplicateAsync{TEntity}"/> and
/// <see cref="IReferenceTableReplicator.ReplicateAllAsync"/>.
/// </para>
/// <para>
/// When <see cref="IsPartial"/> is <c>true</c>, some shards failed but the replication
/// succeeded on at least one target shard. The caller can inspect <see cref="FailedShards"/>
/// to determine which shards need retry.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await replicator.ReplicateAsync&lt;Country&gt;(ct);
///
/// result.Match(
///     Right: rep =>
///     {
///         Console.WriteLine($"Synced {rep.RowsSynced} rows in {rep.Duration.TotalMilliseconds}ms");
///         if (rep.IsPartial)
///             Console.WriteLine($"Warning: {rep.FailedShards.Count} shards failed");
///     },
///     Left: error => Console.WriteLine($"Replication failed: {error.Message}"));
/// </code>
/// </example>
public sealed record ReplicationResult(
    int RowsSynced,
    TimeSpan Duration,
    IReadOnlyList<ShardReplicationResult> ShardResults,
    IReadOnlyList<ShardFailure> FailedShards)
{
    /// <summary>
    /// Gets whether the replication completed successfully on all target shards.
    /// </summary>
    public bool IsComplete => FailedShards.Count == 0;

    /// <summary>
    /// Gets whether some target shards failed but replication succeeded on at least one.
    /// </summary>
    public bool IsPartial => FailedShards.Count > 0 && ShardResults.Count > 0;

    /// <summary>
    /// Gets the total number of target shards involved (successful + failed).
    /// </summary>
    public int TotalShardsTargeted => ShardResults.Count + FailedShards.Count;
}

/// <summary>
/// Describes the result of replicating a reference table to a single target shard.
/// </summary>
/// <param name="ShardId">The target shard identifier.</param>
/// <param name="RowsUpserted">The number of rows upserted to this shard.</param>
/// <param name="Duration">The duration of the replication operation for this shard.</param>
public sealed record ShardReplicationResult(
    string ShardId,
    int RowsUpserted,
    TimeSpan Duration);
