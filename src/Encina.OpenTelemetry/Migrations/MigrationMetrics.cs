using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.OpenTelemetry.Migrations;

/// <summary>
/// Exposes shard migration metrics via the <c>Encina</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
///   <item><description><c>encina.migration.shards_migrated_total</c> (Counter) —
///   Total number of shards successfully migrated, tagged with <c>migration.id</c> and
///   <c>migration.strategy</c>.</description></item>
///   <item><description><c>encina.migration.shards_failed_total</c> (Counter) —
///   Total number of shard migrations that failed.</description></item>
///   <item><description><c>encina.migration.rollbacks_total</c> (Counter) —
///   Total number of rollback operations executed.</description></item>
///   <item><description><c>encina.migration.duration_per_shard_ms</c> (Histogram) —
///   Duration of each individual shard migration in milliseconds.</description></item>
///   <item><description><c>encina.migration.total_duration_ms</c> (Histogram) —
///   Total duration of the entire migration coordination in milliseconds.</description></item>
///   <item><description><c>encina.migration.drift_detected_count</c> (ObservableGauge) —
///   Number of shards with detected schema drift, updated via callback.</description></item>
/// </list>
/// </para>
/// <para>
/// All instruments use the same <c>"Encina"</c> meter for consistency with
/// other Encina metrics classes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registered automatically via AddEncinaOpenTelemetry()
/// services.AddEncinaOpenTelemetry();
///
/// // Or manually:
/// var metrics = new MigrationMetrics(callbacks);
/// metrics.RecordShardMigrated("migration-001", "Sequential", "shard-1");
/// metrics.RecordShardFailed("migration-001", "Sequential", "shard-2");
/// metrics.RecordPerShardDuration("migration-001", "shard-1", 1234.5);
/// metrics.RecordTotalDuration("migration-001", "Sequential", 5678.9);
/// metrics.RecordRollback("migration-001");
/// </code>
/// </example>
public sealed class MigrationMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private readonly Counter<long> _shardsMigrated;
    private readonly Counter<long> _shardsFailed;
    private readonly Counter<long> _rollbacks;
    private readonly Histogram<double> _durationPerShard;
    private readonly Histogram<double> _totalDuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationMetrics"/> class,
    /// registering all migration metric instruments.
    /// </summary>
    /// <param name="callbacks">The callbacks for observable gauge metric values.</param>
    public MigrationMetrics(MigrationMetricsCallbacks callbacks)
    {
        ArgumentNullException.ThrowIfNull(callbacks);

        Meter.CreateObservableGauge<int>(
            "encina.migration.drift_detected_count",
            () =>
            {
                var count = callbacks.DriftDetectedCountCallback();
                return [new Measurement<int>(count)];
            },
            unit: "{shards}",
            description: "Number of shards with detected schema drift.");

        _shardsMigrated = Meter.CreateCounter<long>(
            "encina.migration.shards_migrated_total",
            description: "Total number of shards successfully migrated.");

        _shardsFailed = Meter.CreateCounter<long>(
            "encina.migration.shards_failed_total",
            description: "Total number of shard migrations that failed.");

        _rollbacks = Meter.CreateCounter<long>(
            "encina.migration.rollbacks_total",
            description: "Total number of rollback operations executed.");

        _durationPerShard = Meter.CreateHistogram<double>(
            "encina.migration.duration_per_shard_ms",
            unit: "ms",
            description: "Duration of each individual shard migration in milliseconds.");

        _totalDuration = Meter.CreateHistogram<double>(
            "encina.migration.total_duration_ms",
            unit: "ms",
            description: "Total duration of migration coordination in milliseconds.");
    }

    /// <summary>
    /// Records a successful shard migration.
    /// </summary>
    /// <param name="migrationId">The migration execution identifier.</param>
    /// <param name="strategy">The migration strategy name.</param>
    /// <param name="shardId">The shard identifier.</param>
    public void RecordShardMigrated(string migrationId, string strategy, string shardId)
    {
        var tags = new TagList
        {
            { ActivityTagNames.Migration.Id, migrationId },
            { ActivityTagNames.Migration.Strategy, strategy },
            { ActivityTagNames.Migration.ShardId, shardId },
            { ActivityTagNames.Migration.ShardOutcome, "Succeeded" }
        };

        _shardsMigrated.Add(1, tags);
    }

    /// <summary>
    /// Records a failed shard migration.
    /// </summary>
    /// <param name="migrationId">The migration execution identifier.</param>
    /// <param name="strategy">The migration strategy name.</param>
    /// <param name="shardId">The shard identifier.</param>
    public void RecordShardFailed(string migrationId, string strategy, string shardId)
    {
        var tags = new TagList
        {
            { ActivityTagNames.Migration.Id, migrationId },
            { ActivityTagNames.Migration.Strategy, strategy },
            { ActivityTagNames.Migration.ShardId, shardId },
            { ActivityTagNames.Migration.ShardOutcome, "Failed" }
        };

        _shardsFailed.Add(1, tags);
    }

    /// <summary>
    /// Records a rollback operation.
    /// </summary>
    /// <param name="migrationId">The migration execution identifier.</param>
    public void RecordRollback(string migrationId)
    {
        _rollbacks.Add(1,
            new KeyValuePair<string, object?>(ActivityTagNames.Migration.Id, migrationId));
    }

    /// <summary>
    /// Records the duration of a single shard's migration.
    /// </summary>
    /// <param name="migrationId">The migration execution identifier.</param>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    public void RecordPerShardDuration(string migrationId, string shardId, double durationMs)
    {
        var tags = new TagList
        {
            { ActivityTagNames.Migration.Id, migrationId },
            { ActivityTagNames.Migration.ShardId, shardId }
        };

        _durationPerShard.Record(durationMs, tags);
    }

    /// <summary>
    /// Records the total duration of the entire migration coordination.
    /// </summary>
    /// <param name="migrationId">The migration execution identifier.</param>
    /// <param name="strategy">The migration strategy name.</param>
    /// <param name="durationMs">The total duration in milliseconds.</param>
    public void RecordTotalDuration(string migrationId, string strategy, double durationMs)
    {
        var tags = new TagList
        {
            { ActivityTagNames.Migration.Id, migrationId },
            { ActivityTagNames.Migration.Strategy, strategy }
        };

        _totalDuration.Record(durationMs, tags);
    }
}
