using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.OpenTelemetry.Sharding;

/// <summary>
/// Exposes time-based sharding tier metrics via the <c>Encina</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
///   <item><description><c>encina.sharding.tiered.shards_per_tier</c> (ObservableGauge) —
///   Current number of shards in each storage tier, tagged with <c>shard.tier</c>.</description></item>
///   <item><description><c>encina.sharding.tiered.oldest_hot_shard_age_days</c> (ObservableGauge) —
///   Age in days of the oldest Hot-tier shard (based on period end date).</description></item>
///   <item><description><c>encina.sharding.tiered.tier_transitions_total</c> (Counter) —
///   Number of tier transitions executed, tagged with <c>tier.from</c> and <c>tier.to</c>.</description></item>
///   <item><description><c>encina.sharding.tiered.auto_created_shards_total</c> (Counter) —
///   Number of shards auto-created by the scheduler.</description></item>
///   <item><description><c>encina.sharding.tiered.queries_per_tier</c> (Counter) —
///   Number of queries routed to each tier, tagged with <c>shard.tier</c>.</description></item>
///   <item><description><c>encina.sharding.tiered.archival_duration_ms</c> (Histogram) —
///   Duration of archival operations in milliseconds, tagged with <c>db.shard.id</c>.</description></item>
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
/// var metrics = new TimeBasedShardingMetrics(callbacks);
/// metrics.RecordTierTransition("Hot", "Warm");
/// metrics.RecordAutoCreatedShard();
/// metrics.RecordQueryPerTier("Hot");
/// metrics.RecordArchivalDuration("orders-2025-01", 1234.5);
/// </code>
/// </example>
public sealed class TimeBasedShardingMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private readonly Counter<long> _tierTransitions;
    private readonly Counter<long> _autoCreatedShards;
    private readonly Counter<long> _queriesPerTier;
    private readonly Histogram<double> _archivalDuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeBasedShardingMetrics"/> class,
    /// registering all time-based sharding metric instruments.
    /// </summary>
    /// <param name="callbacks">The callbacks for observable gauge metric values.</param>
    public TimeBasedShardingMetrics(TimeBasedShardingMetricsCallbacks callbacks)
    {
        ArgumentNullException.ThrowIfNull(callbacks);

        Meter.CreateObservableGauge(
            "encina.sharding.tiered.shards_per_tier",
            () =>
            {
                var measurements = new List<Measurement<int>>();
                foreach (var (tier, count) in callbacks.ShardsPerTierCallback())
                {
                    measurements.Add(new Measurement<int>(
                        count,
                        new KeyValuePair<string, object?>(ActivityTagNames.Tiering.ShardTier, tier)));
                }

                return measurements;
            },
            unit: "{shards}",
            description: "Current number of shards in each storage tier.");

        Meter.CreateObservableGauge<double>(
            "encina.sharding.tiered.oldest_hot_shard_age_days",
            () =>
            {
                var ageDays = callbacks.OldestHotShardAgeDaysCallback();
                return ageDays.HasValue
                    ? [new Measurement<double>(ageDays.Value)]
                    : [];
            },
            unit: "d",
            description: "Age in days of the oldest Hot-tier shard.");

        _tierTransitions = Meter.CreateCounter<long>(
            "encina.sharding.tiered.tier_transitions_total",
            description: "Number of tier transitions executed.");

        _autoCreatedShards = Meter.CreateCounter<long>(
            "encina.sharding.tiered.auto_created_shards_total",
            description: "Number of shards auto-created by the scheduler.");

        _queriesPerTier = Meter.CreateCounter<long>(
            "encina.sharding.tiered.queries_per_tier",
            description: "Number of queries routed to each tier.");

        _archivalDuration = Meter.CreateHistogram<double>(
            "encina.sharding.tiered.archival_duration_ms",
            unit: "ms",
            description: "Duration of archival operations in milliseconds.");
    }

    /// <summary>
    /// Records a tier transition event.
    /// </summary>
    /// <param name="fromTier">The source tier name (e.g., "Hot").</param>
    /// <param name="toTier">The target tier name (e.g., "Warm").</param>
    public void RecordTierTransition(string fromTier, string toTier)
    {
        var tags = new TagList
        {
            { ActivityTagNames.Tiering.TierFrom, fromTier },
            { ActivityTagNames.Tiering.TierTo, toTier }
        };

        _tierTransitions.Add(1, tags);
    }

    /// <summary>
    /// Records an auto-created shard event.
    /// </summary>
    public void RecordAutoCreatedShard()
    {
        _autoCreatedShards.Add(1);
    }

    /// <summary>
    /// Records a query routed to a specific tier.
    /// </summary>
    /// <param name="tier">The tier name (e.g., "Hot", "Warm").</param>
    public void RecordQueryPerTier(string tier)
    {
        _queriesPerTier.Add(1,
            new KeyValuePair<string, object?>(ActivityTagNames.Tiering.ShardTier, tier));
    }

    /// <summary>
    /// Records the duration of an archival operation.
    /// </summary>
    /// <param name="shardId">The shard identifier being archived.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    public void RecordArchivalDuration(string shardId, double durationMs)
    {
        _archivalDuration.Record(durationMs,
            new KeyValuePair<string, object?>("db.shard.id", shardId));
    }
}
