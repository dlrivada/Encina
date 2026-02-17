namespace Encina.Sharding.Migrations;

/// <summary>
/// Fluent builder for configuring shard migration coordination options.
/// </summary>
/// <remarks>
/// <para>
/// Used via the <c>WithMigrationCoordination</c> extension method on the sharding
/// configuration to provide a discoverable, chainable API for migration settings.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaSharding&lt;Order&gt;(options =>
/// {
///     options.UseHashRouting()
///         .AddShard("shard-0", "Server=shard0;...")
///         .AddShard("shard-1", "Server=shard1;...");
/// })
/// .WithMigrationCoordination(migration =>
/// {
///     migration
///         .UseStrategy(MigrationStrategy.RollingUpdate)
///         .WithMaxParallelism(8)
///         .StopOnFirstFailure()
///         .WithPerShardTimeout(TimeSpan.FromMinutes(10))
///         .ValidateBeforeApply()
///         .OnShardMigrated((shardId, outcome) =>
///             Console.WriteLine($"Shard {shardId}: {outcome}"))
///         .WithDriftDetection(drift =>
///         {
///             drift.ComparisonDepth = SchemaComparisonDepth.Full;
///             drift.CriticalTables = ["orders", "payments"];
///         });
/// });
/// </code>
/// </example>
public sealed class MigrationCoordinationBuilder
{
    private readonly MigrationCoordinationOptions _options = new();

    /// <summary>
    /// Sets the default migration strategy.
    /// </summary>
    /// <param name="strategy">The strategy to use for applying migrations across shards.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public MigrationCoordinationBuilder UseStrategy(MigrationStrategy strategy)
    {
        _options.DefaultStrategy = strategy;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of shards that can be migrated concurrently.
    /// </summary>
    /// <param name="maxParallelism">The maximum parallelism level. Must be at least 1.</param>
    /// <returns>This builder for fluent chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxParallelism"/> is less than 1.</exception>
    public MigrationCoordinationBuilder WithMaxParallelism(int maxParallelism)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxParallelism, 1);
        _options.MaxParallelism = maxParallelism;
        return this;
    }

    /// <summary>
    /// Configures the coordinator to stop migrating remaining shards when the first failure is detected.
    /// </summary>
    /// <param name="stop">Whether to stop on first failure. Defaults to <see langword="true"/>.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public MigrationCoordinationBuilder StopOnFirstFailure(bool stop = true)
    {
        _options.StopOnFirstFailure = stop;
        return this;
    }

    /// <summary>
    /// Sets the maximum time allowed for a single shard's migration.
    /// </summary>
    /// <param name="timeout">The per-shard timeout duration.</param>
    /// <returns>This builder for fluent chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout"/> is zero or negative.</exception>
    public MigrationCoordinationBuilder WithPerShardTimeout(TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        _options.PerShardTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Configures the coordinator to validate migration scripts before applying them.
    /// </summary>
    /// <param name="validate">Whether to validate before apply. Defaults to <see langword="true"/>.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public MigrationCoordinationBuilder ValidateBeforeApply(bool validate = true)
    {
        _options.ValidateBeforeApply = validate;
        return this;
    }

    /// <summary>
    /// Registers a callback invoked after each shard completes migration.
    /// </summary>
    /// <param name="callback">
    /// The callback receiving the shard identifier and the migration outcome.
    /// </param>
    /// <returns>This builder for fluent chaining.</returns>
    public MigrationCoordinationBuilder OnShardMigrated(Action<string, MigrationOutcome> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _options.OnShardMigrated = callback;
        return this;
    }

    /// <summary>
    /// Configures drift detection options.
    /// </summary>
    /// <param name="configure">An action to configure the <see cref="DriftDetectionOptions"/>.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public MigrationCoordinationBuilder WithDriftDetection(Action<DriftDetectionOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_options.DriftDetection);
        return this;
    }

    /// <summary>
    /// Builds the <see cref="MigrationCoordinationOptions"/> from this builder's configuration.
    /// </summary>
    /// <returns>The configured <see cref="MigrationCoordinationOptions"/>.</returns>
    internal MigrationCoordinationOptions Build() => _options;
}
