namespace Encina.Sharding.Migrations;

/// <summary>
/// Defines the strategy used to apply schema migrations across shards.
/// </summary>
/// <remarks>
/// <para>
/// Each strategy offers a different trade-off between safety and speed. The coordinator
/// selects the execution plan based on this value together with the options in
/// <see cref="MigrationOptions"/>.
/// </para>
/// </remarks>
public enum MigrationStrategy
{
    /// <summary>
    /// Apply the migration to one shard at a time, in order.
    /// Safest strategy — a failure stops the rollout immediately.
    /// </summary>
    Sequential,

    /// <summary>
    /// Apply the migration to all shards simultaneously, up to
    /// <see cref="MigrationOptions.MaxParallelism"/> concurrent operations.
    /// Fastest strategy — best for non-destructive, additive DDL changes.
    /// </summary>
    Parallel,

    /// <summary>
    /// Apply the migration in batches of <see cref="MigrationOptions.MaxParallelism"/> shards.
    /// Each batch must succeed before the next batch starts.
    /// Balanced strategy between safety and speed.
    /// </summary>
    RollingUpdate,

    /// <summary>
    /// Apply the migration to a single canary shard first. If the canary succeeds,
    /// the remaining shards are migrated using the <see cref="Parallel"/> strategy.
    /// Recommended for high-risk schema changes.
    /// </summary>
    CanaryFirst
}
