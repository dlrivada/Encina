namespace Encina.Sharding.Migrations;

/// <summary>
/// The result of applying a migration script across all shards in the topology.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="IShardedMigrationCoordinator.ApplyToAllShardsAsync"/> inside an
/// <c>Either&lt;EncinaError, MigrationResult&gt;</c>. The <see cref="PerShardStatus"/>
/// dictionary provides granular per-shard outcomes, while <see cref="AllSucceeded"/>
/// offers a quick overall success check.
/// </para>
/// <para>
/// This type follows the same patterns as <see cref="ShardedQueryResult{T}"/>, providing
/// both aggregate and per-shard views of the operation result.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await coordinator.ApplyToAllShardsAsync(script, options, ct);
///
/// result.Match(
///     Right: r =>
///     {
///         if (r.AllSucceeded)
///             logger.LogInformation("Migration applied to all {Count} shards in {Duration}",
///                 r.PerShardStatus.Count, r.TotalDuration);
///         else
///             foreach (var (shardId, status) in r.PerShardStatus.Where(
///                 kvp => kvp.Value.Outcome == MigrationOutcome.Failed))
///                 logger.LogError("Shard {ShardId} failed: {Error}", shardId, status.Error?.Message);
///     },
///     Left: error => logger.LogError("Migration coordination failed: {Error}", error.Message));
/// </code>
/// </example>
/// <param name="Id">Unique identifier for this migration execution.</param>
/// <param name="PerShardStatus">Per-shard migration outcomes keyed by shard ID.</param>
/// <param name="TotalDuration">Wall-clock duration of the entire migration operation.</param>
/// <param name="AppliedAtUtc">UTC timestamp when the migration execution started.</param>
public sealed record MigrationResult(
    Guid Id,
    IReadOnlyDictionary<string, ShardMigrationStatus> PerShardStatus,
    TimeSpan TotalDuration,
    DateTimeOffset AppliedAtUtc)
{
    /// <summary>
    /// Gets whether every shard completed the migration successfully.
    /// </summary>
    public bool AllSucceeded => PerShardStatus.Values.All(
        s => s.Outcome == MigrationOutcome.Succeeded);

    /// <summary>
    /// Gets the number of shards that were successfully migrated.
    /// </summary>
    public int SucceededCount => PerShardStatus.Values.Count(
        s => s.Outcome == MigrationOutcome.Succeeded);

    /// <summary>
    /// Gets the number of shards where the migration failed.
    /// </summary>
    public int FailedCount => PerShardStatus.Values.Count(
        s => s.Outcome == MigrationOutcome.Failed);
}
