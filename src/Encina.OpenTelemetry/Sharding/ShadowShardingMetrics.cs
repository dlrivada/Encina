using System.Diagnostics.Metrics;

namespace Encina.OpenTelemetry.Sharding;

/// <summary>
/// Exposes shadow sharding metrics via the <c>Encina</c> meter for monitoring
/// topology migration testing under production traffic.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
///   <item><description><c>encina.sharding.shadow.routing_total</c> (Counter) —
///   Total shadow routing comparisons, tagged with <c>routing_match</c>.</description></item>
///   <item><description><c>encina.sharding.shadow.routing_mismatches_total</c> (Counter) —
///   Shadow routing mismatches, tagged with <c>shard_key_prefix</c>.</description></item>
///   <item><description><c>encina.sharding.shadow.write_total</c> (Counter) —
///   Shadow write operations, tagged with <c>outcome</c> (success/failure).</description></item>
///   <item><description><c>encina.sharding.shadow.write_latency_diff_ms</c> (Histogram) —
///   Latency difference between production and shadow writes, tagged with <c>shard_id</c>.</description></item>
///   <item><description><c>encina.sharding.shadow.read_comparison_total</c> (Counter) —
///   Shadow read comparisons, tagged with <c>results_match</c>.</description></item>
///   <item><description><c>encina.sharding.shadow.read_latency_diff_ms</c> (Histogram) —
///   Latency difference between production and shadow reads.</description></item>
/// </list>
/// </para>
/// <para>
/// All instruments use the same <c>"Encina"</c> meter for consistency with
/// other Encina metrics classes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var metrics = new ShadowShardingMetrics();
/// metrics.RecordRouting(routingMatch: true);
/// metrics.RecordRoutingMismatch("customer");
/// metrics.RecordShadowWrite("shard-1", success: true, latencyDiffMs: 12.5);
/// metrics.RecordShadowRead(resultsMatch: true, latencyDiffMs: 3.2);
/// </code>
/// </example>
public sealed class ShadowShardingMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private readonly Counter<long> _routingTotal;
    private readonly Counter<long> _routingMismatchesTotal;
    private readonly Counter<long> _writeTotal;
    private readonly Histogram<double> _writeLatencyDiff;
    private readonly Counter<long> _readComparisonTotal;
    private readonly Histogram<double> _readLatencyDiff;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShadowShardingMetrics"/> class,
    /// registering all shadow sharding metric instruments.
    /// </summary>
    public ShadowShardingMetrics()
    {
        _routingTotal = Meter.CreateCounter<long>(
            "encina.sharding.shadow.routing_total",
            description: "Total shadow routing comparisons.");

        _routingMismatchesTotal = Meter.CreateCounter<long>(
            "encina.sharding.shadow.routing_mismatches_total",
            description: "Shadow routing mismatches by shard key prefix.");

        _writeTotal = Meter.CreateCounter<long>(
            "encina.sharding.shadow.write_total",
            description: "Shadow write operations by outcome.");

        _writeLatencyDiff = Meter.CreateHistogram<double>(
            "encina.sharding.shadow.write_latency_diff_ms",
            unit: "ms",
            description: "Latency difference between production and shadow writes in milliseconds.");

        _readComparisonTotal = Meter.CreateCounter<long>(
            "encina.sharding.shadow.read_comparison_total",
            description: "Shadow read comparisons by result match status.");

        _readLatencyDiff = Meter.CreateHistogram<double>(
            "encina.sharding.shadow.read_latency_diff_ms",
            unit: "ms",
            description: "Latency difference between production and shadow reads in milliseconds.");
    }

    /// <summary>
    /// Records a shadow routing comparison.
    /// </summary>
    /// <param name="routingMatch">
    /// <c>true</c> if the production and shadow routers agreed on the shard; <c>false</c> otherwise.
    /// </param>
    public void RecordRouting(bool routingMatch)
    {
        _routingTotal.Add(1,
            new KeyValuePair<string, object?>(ActivityTagNames.Shadow.RoutingMatch, routingMatch));
    }

    /// <summary>
    /// Records a routing mismatch with the shard key prefix for aggregation.
    /// </summary>
    /// <param name="shardKeyPrefix">
    /// A prefix of the shard key for grouping mismatches (e.g., first segment of the key).
    /// </param>
    public void RecordRoutingMismatch(string shardKeyPrefix)
    {
        _routingMismatchesTotal.Add(1,
            new KeyValuePair<string, object?>("shard_key_prefix", shardKeyPrefix));
    }

    /// <summary>
    /// Records a shadow write operation and its latency difference.
    /// </summary>
    /// <param name="shardId">The target shard identifier.</param>
    /// <param name="success"><c>true</c> if the shadow write succeeded; <c>false</c> otherwise.</param>
    /// <param name="latencyDiffMs">
    /// The latency difference in milliseconds between shadow and production writes.
    /// Positive values indicate the shadow was slower.
    /// </param>
    public void RecordShadowWrite(string shardId, bool success, double latencyDiffMs)
    {
        var outcome = success ? "success" : "failure";

        _writeTotal.Add(1,
            new KeyValuePair<string, object?>(ActivityTagNames.Shadow.WriteOutcome, outcome));

        _writeLatencyDiff.Record(latencyDiffMs,
            new KeyValuePair<string, object?>("db.shard.id", shardId));
    }

    /// <summary>
    /// Records a shadow read comparison and its latency difference.
    /// </summary>
    /// <param name="resultsMatch">
    /// <c>true</c> if the production and shadow read results matched; <c>false</c> otherwise.
    /// </param>
    /// <param name="latencyDiffMs">
    /// The latency difference in milliseconds between shadow and production reads.
    /// Positive values indicate the shadow was slower.
    /// </param>
    public void RecordShadowRead(bool resultsMatch, double latencyDiffMs)
    {
        _readComparisonTotal.Add(1,
            new KeyValuePair<string, object?>(ActivityTagNames.Shadow.ReadResultsMatch, resultsMatch));

        _readLatencyDiff.Record(latencyDiffMs);
    }
}
