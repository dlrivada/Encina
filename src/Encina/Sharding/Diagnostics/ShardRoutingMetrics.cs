using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Sharding.Diagnostics;

/// <summary>
/// Exposes shard routing and scatter-gather metrics via the <c>Encina</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
///   <item><description><c>encina.sharding.route.decisions</c> (Counter) — Routing decisions made</description></item>
///   <item><description><c>encina.sharding.route.duration_ns</c> (Histogram) — Routing decision latency in nanoseconds</description></item>
///   <item><description><c>encina.sharding.topology.shards.active</c> (ObservableGauge) — Active shard count</description></item>
///   <item><description><c>encina.sharding.scatter.duration_ms</c> (Histogram) — Scatter-gather operation time</description></item>
///   <item><description><c>encina.sharding.scatter.shard.duration_ms</c> (Histogram) — Per-shard query time</description></item>
///   <item><description><c>encina.sharding.scatter.partial_failures</c> (Counter) — Partial failure count</description></item>
///   <item><description><c>encina.sharding.scatter.queries.active</c> (UpDownCounter) — Concurrent shard queries in flight</description></item>
/// </list>
/// </para>
/// <para>
/// All instruments use the same <c>"Encina"</c> meter for consistency with
/// <see cref="EncinaMetrics"/> and <c>DatabasePoolMetrics</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registered automatically via AddEncinaShardingMetrics()
/// services.AddEncinaShardingMetrics();
///
/// // Or manually:
/// services.AddSingleton&lt;ShardRoutingMetrics&gt;();
/// </code>
/// </example>
#pragma warning disable CA1848 // Use the LoggerMessage delegates
public sealed class ShardRoutingMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    // Routing metrics
    private readonly Counter<long> _routeDecisions;
    private readonly Histogram<long> _routeDuration;

    // Compound key metrics
    private readonly Counter<long> _compoundKeyExtractions;
    private readonly Counter<long> _partialKeyRoutings;

    // Scatter-gather metrics
    private readonly Histogram<double> _scatterDuration;
    private readonly Histogram<double> _shardQueryDuration;
    private readonly Counter<long> _partialFailures;
    private readonly UpDownCounter<int> _activeQueries;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardRoutingMetrics"/> class,
    /// registering all sharding metric instruments.
    /// </summary>
    /// <param name="topology">The shard topology for observable gauge callbacks.</param>
    public ShardRoutingMetrics(ShardTopology topology)
    {
        ArgumentNullException.ThrowIfNull(topology);

        _routeDecisions = Meter.CreateCounter<long>(
            "encina.sharding.route.decisions",
            description: "Number of shard routing decisions made.");

        _routeDuration = Meter.CreateHistogram<long>(
            "encina.sharding.route.duration_ns",
            unit: "ns",
            description: "Duration of shard routing decisions in nanoseconds.");

        _compoundKeyExtractions = Meter.CreateCounter<long>(
            "encina.sharding.compound_key_extractions_total",
            description: "Number of compound shard key extractions performed.");

        _partialKeyRoutings = Meter.CreateCounter<long>(
            "encina.sharding.partial_key_routing_total",
            description: "Number of partial key routing operations performed.");

        _scatterDuration = Meter.CreateHistogram<double>(
            "encina.sharding.scatter.duration_ms",
            unit: "ms",
            description: "Duration of scatter-gather operations in milliseconds.");

        _shardQueryDuration = Meter.CreateHistogram<double>(
            "encina.sharding.scatter.shard.duration_ms",
            unit: "ms",
            description: "Duration of individual shard queries in milliseconds.");

        _partialFailures = Meter.CreateCounter<long>(
            "encina.sharding.scatter.partial_failures",
            description: "Number of scatter-gather operations with partial failures.");

        _activeQueries = Meter.CreateUpDownCounter<int>(
            "encina.sharding.scatter.queries.active",
            unit: "{queries}",
            description: "Number of concurrent shard queries currently in flight.");

        Meter.CreateObservableGauge(
            "encina.sharding.topology.shards.active",
            () => new Measurement<int>(topology.ActiveShardIds.Count),
            unit: "{shards}",
            description: "Current number of active shards in the topology.");
    }

    /// <summary>
    /// Records a routing decision with the resolved shard ID and latency.
    /// </summary>
    /// <param name="shardId">The resolved shard ID.</param>
    /// <param name="routerType">The routing strategy (e.g., "hash", "range", "directory", "geo").</param>
    /// <param name="durationNs">The routing decision duration in nanoseconds.</param>
    public void RecordRouteDecision(string shardId, string routerType, double durationNs)
    {
        var tags = new TagList
        {
            { ActivityTagNames.ShardId, shardId },
            { ActivityTagNames.RouterType, routerType }
        };

        _routeDecisions.Add(1, tags);
        _routeDuration.Record((long)durationNs, tags);
    }

    /// <summary>
    /// Records a compound shard key extraction.
    /// </summary>
    /// <param name="componentCount">The number of components in the extracted compound key.</param>
    /// <param name="routerType">The routing strategy (e.g., "hash", "range", "compound").</param>
    public void RecordCompoundKeyExtraction(int componentCount, string routerType)
    {
        var tags = new TagList
        {
            { ActivityTagNames.CompoundKeyComponents, componentCount },
            { ActivityTagNames.RouterType, routerType }
        };

        _compoundKeyExtractions.Add(1, tags);
    }

    /// <summary>
    /// Records a partial key routing operation where fewer components than expected were provided.
    /// </summary>
    /// <param name="componentsProvided">The number of key components provided.</param>
    /// <param name="componentsExpected">The total number of expected key components.</param>
    /// <param name="routerType">The routing strategy (e.g., "compound").</param>
    public void RecordPartialKeyRouting(int componentsProvided, int componentsExpected, string routerType)
    {
        var tags = new TagList
        {
            { "components.provided", componentsProvided },
            { "components.expected", componentsExpected },
            { ActivityTagNames.RouterType, routerType }
        };

        _partialKeyRoutings.Add(1, tags);
    }

    /// <summary>
    /// Records the total duration of a scatter-gather operation.
    /// </summary>
    /// <param name="shardCount">The number of shards queried.</param>
    /// <param name="resultCount">The total number of result items.</param>
    /// <param name="durationMs">The operation duration in milliseconds.</param>
    public void RecordScatterGatherDuration(int shardCount, int resultCount, double durationMs)
    {
        var tags = new TagList
        {
            { ActivityTagNames.ShardCount, shardCount },
            { "result.count", resultCount }
        };

        _scatterDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Records the duration of a single shard query within a scatter-gather operation.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="durationMs">The query duration in milliseconds.</param>
    public void RecordShardQueryDuration(string shardId, double durationMs)
    {
        _shardQueryDuration.Record(
            durationMs,
            new KeyValuePair<string, object?>(ActivityTagNames.ShardId, shardId));
    }

    /// <summary>
    /// Records a scatter-gather operation with partial failures.
    /// </summary>
    /// <param name="failedCount">The number of failed shard queries.</param>
    /// <param name="totalCount">The total number of shard queries attempted.</param>
    public void RecordPartialFailure(int failedCount, int totalCount)
    {
        var tags = new TagList
        {
            { "failed.count", failedCount },
            { "total.count", totalCount }
        };

        _partialFailures.Add(1, tags);
    }

    /// <summary>
    /// Increments the active concurrent shard query count.
    /// </summary>
    public void IncrementActiveQueries()
    {
        _activeQueries.Add(1);
    }

    /// <summary>
    /// Decrements the active concurrent shard query count.
    /// </summary>
    public void DecrementActiveQueries()
    {
        _activeQueries.Add(-1);
    }
}
#pragma warning restore CA1848
