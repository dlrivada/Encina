namespace Encina.Sharding.Migrations;

/// <summary>
/// Configuration options that control how a migration is applied across shards.
/// </summary>
/// <remarks>
/// <para>
/// These options are passed to
/// <see cref="IShardedMigrationCoordinator.ApplyToAllShardsAsync"/> to control the
/// execution strategy, parallelism, failure behavior, and timeouts.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new MigrationOptions
/// {
///     Strategy = MigrationStrategy.CanaryFirst,
///     MaxParallelism = 8,
///     StopOnFirstFailure = true,
///     PerShardTimeout = TimeSpan.FromMinutes(10),
///     ValidateBeforeApply = true
/// };
///
/// var result = await coordinator.ApplyToAllShardsAsync(script, options, ct);
/// </code>
/// </example>
public sealed class MigrationOptions
{
    /// <summary>
    /// Gets or sets the execution strategy for applying the migration across shards.
    /// </summary>
    /// <value>Defaults to <see cref="MigrationStrategy.Sequential"/>.</value>
    public MigrationStrategy Strategy { get; set; } = MigrationStrategy.Sequential;

    /// <summary>
    /// Gets or sets the maximum number of shards that can be migrated concurrently.
    /// </summary>
    /// <remarks>
    /// Applies to <see cref="MigrationStrategy.Parallel"/>,
    /// <see cref="MigrationStrategy.RollingUpdate"/>, and the rollout phase of
    /// <see cref="MigrationStrategy.CanaryFirst"/>.
    /// Ignored when <see cref="Strategy"/> is <see cref="MigrationStrategy.Sequential"/>.
    /// </remarks>
    /// <value>Defaults to <c>4</c>.</value>
    public int MaxParallelism { get; set; } = 4;

    /// <summary>
    /// Gets or sets whether the coordinator should stop migrating remaining shards
    /// when the first shard failure is detected.
    /// </summary>
    /// <value>Defaults to <see langword="true"/>.</value>
    public bool StopOnFirstFailure { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum time allowed for a single shard's migration before
    /// the operation is cancelled.
    /// </summary>
    /// <value>Defaults to 5 minutes.</value>
    public TimeSpan PerShardTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets whether the coordinator should validate the migration script
    /// (e.g., checksum verification, syntax pre-check) before applying it to any shard.
    /// </summary>
    /// <value>Defaults to <see langword="true"/>.</value>
    public bool ValidateBeforeApply { get; set; } = true;
}
