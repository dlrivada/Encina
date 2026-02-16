namespace Encina.Sharding.Migrations;

/// <summary>
/// Global configuration options for shard migration coordination, registered in the
/// dependency injection container.
/// </summary>
/// <remarks>
/// <para>
/// These options provide <em>default</em> values used by
/// <see cref="IShardedMigrationCoordinator"/> when no per-call
/// <see cref="MigrationOptions"/> overrides are supplied. Per-call options always
/// take precedence over these defaults.
/// </para>
/// <para>
/// Unlike <see cref="MigrationOptions"/> (which is created per-call),
/// <see cref="MigrationCoordinationOptions"/> is configured once at startup via
/// dependency injection and controls the coordinator's global behavior.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaShardMigration(options =>
/// {
///     options.Coordination.DefaultStrategy = MigrationStrategy.RollingUpdate;
///     options.Coordination.MaxParallelism = 8;
///     options.Coordination.StopOnFirstFailure = true;
///     options.Coordination.OnShardMigrated = (shardId, outcome) =>
///         logger.LogInformation("Shard {Shard} completed with {Outcome}", shardId, outcome);
/// });
/// </code>
/// </example>
public sealed class MigrationCoordinationOptions
{
    /// <summary>
    /// Gets or sets the default migration strategy used when the per-call
    /// <see cref="MigrationOptions.Strategy"/> is not explicitly set.
    /// </summary>
    /// <value>Defaults to <see cref="MigrationStrategy.Sequential"/>.</value>
    public MigrationStrategy DefaultStrategy { get; set; } = MigrationStrategy.Sequential;

    /// <summary>
    /// Gets or sets the default maximum number of shards that can be migrated
    /// concurrently. Applied to strategies that support parallelism
    /// (<see cref="MigrationStrategy.Parallel"/>, <see cref="MigrationStrategy.RollingUpdate"/>,
    /// and the rollout phase of <see cref="MigrationStrategy.CanaryFirst"/>).
    /// </summary>
    /// <value>Defaults to <c>4</c>.</value>
    public int MaxParallelism { get; set; } = 4;

    /// <summary>
    /// Gets or sets whether the coordinator should stop migrating remaining shards
    /// when the first shard failure is detected, unless overridden per-call.
    /// </summary>
    /// <value>Defaults to <see langword="true"/>.</value>
    public bool StopOnFirstFailure { get; set; } = true;

    /// <summary>
    /// Gets or sets the default timeout for a single shard's migration before
    /// the operation is cancelled.
    /// </summary>
    /// <value>Defaults to 5 minutes.</value>
    public TimeSpan PerShardTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets an optional callback invoked after each shard completes migration
    /// (whether it succeeds or fails). Useful for logging, monitoring, or custom notification logic.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The callback receives the shard identifier and the migration outcome.
    /// It is invoked on the same thread that completed the shard's migration.
    /// The callback should be lightweight and non-blocking; long-running operations
    /// may delay subsequent shard migrations.
    /// </para>
    /// </remarks>
    /// <value>Defaults to <see langword="null"/> (no callback).</value>
    public Action<string, MigrationOutcome>? OnShardMigrated { get; set; }

    /// <summary>
    /// Gets or sets whether the coordinator should validate the migration script
    /// (e.g., checksum verification) before applying it to any shard.
    /// </summary>
    /// <value>Defaults to <see langword="true"/>.</value>
    public bool ValidateBeforeApply { get; set; } = true;

    /// <summary>
    /// Gets the drift detection options used by <see cref="IShardedMigrationCoordinator.DetectDriftAsync"/>.
    /// </summary>
    /// <remarks>
    /// These options can be configured via the <see cref="DriftDetection"/> property or
    /// through the builder's <c>WithDriftDetection</c> method.
    /// </remarks>
    public DriftDetectionOptions DriftDetection { get; } = new();

    /// <summary>
    /// Creates a <see cref="MigrationOptions"/> instance populated from these coordination defaults.
    /// </summary>
    /// <returns>A new <see cref="MigrationOptions"/> with values from this configuration.</returns>
    /// <remarks>
    /// This is used internally by the coordinator when no explicit per-call options are provided.
    /// </remarks>
    internal MigrationOptions ToMigrationOptions() => new()
    {
        Strategy = DefaultStrategy,
        MaxParallelism = MaxParallelism,
        StopOnFirstFailure = StopOnFirstFailure,
        PerShardTimeout = PerShardTimeout,
        ValidateBeforeApply = ValidateBeforeApply
    };
}
