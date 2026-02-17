namespace Encina.Sharding.Migrations;

/// <summary>
/// A point-in-time snapshot of an in-flight migration execution.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="IShardedMigrationCoordinator.GetProgressAsync"/> to allow callers
/// to monitor long-running migrations. The <see cref="PerShardProgress"/> dictionary provides
/// detailed per-shard status while the aggregate counters give a quick overview.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var progress = await coordinator.GetProgressAsync(migrationId, ct);
///
/// progress.Match(
///     Right: p => logger.LogInformation(
///         "Migration {Phase}: {Completed}/{Total} shards ({Failed} failed)",
///         p.CurrentPhase, p.CompletedShards, p.TotalShards, p.FailedShards),
///     Left: error => logger.LogWarning("Could not retrieve progress: {Error}", error.Message));
/// </code>
/// </example>
/// <param name="MigrationId">The unique identifier of the migration execution being tracked.</param>
/// <param name="TotalShards">Total number of shards targeted by this migration.</param>
/// <param name="CompletedShards">Number of shards that have completed the migration successfully.</param>
/// <param name="FailedShards">Number of shards where the migration has failed.</param>
/// <param name="CurrentPhase">
/// A human-readable label for the current phase (e.g., <c>"Canary"</c>, <c>"RollingBatch2"</c>,
/// <c>"Completed"</c>).
/// </param>
/// <param name="PerShardProgress">Per-shard migration status keyed by shard ID.</param>
public sealed record MigrationProgress(
    Guid MigrationId,
    int TotalShards,
    int CompletedShards,
    int FailedShards,
    string CurrentPhase,
    IReadOnlyDictionary<string, ShardMigrationStatus> PerShardProgress)
{
    /// <summary>
    /// Gets the number of shards that are still pending or in progress.
    /// </summary>
    public int RemainingShards => TotalShards - CompletedShards - FailedShards;

    /// <summary>
    /// Gets whether the migration has finished (all shards either succeeded or failed).
    /// </summary>
    public bool IsFinished => CompletedShards + FailedShards >= TotalShards;
}
