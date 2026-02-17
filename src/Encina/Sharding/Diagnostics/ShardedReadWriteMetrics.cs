using System.Diagnostics;
using System.Diagnostics.Metrics;
using Encina.Sharding.ReplicaSelection;

namespace Encina.Sharding.Diagnostics;

/// <summary>
/// Exposes read/write separation metrics for sharded replicas via the <c>Encina</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
///   <item><description><c>encina.sharding.readwrite.routing_total</c> (Counter) — Read/write routing decisions</description></item>
///   <item><description><c>encina.sharding.readwrite.replica_selection_duration_ms</c> (Histogram) — Replica selection latency</description></item>
///   <item><description><c>encina.sharding.readwrite.replica_latency_ms</c> (Histogram) — Observed replica connection latency</description></item>
///   <item><description><c>encina.sharding.readwrite.replica_unhealthy_count</c> (ObservableGauge) — Per-shard unhealthy replica count</description></item>
///   <item><description><c>encina.sharding.readwrite.replication_lag_ms</c> (ObservableGauge) — Per-replica replication lag</description></item>
///   <item><description><c>encina.sharding.readwrite.fallback_to_primary_total</c> (Counter) — Fallbacks to primary count</description></item>
/// </list>
/// </para>
/// <para>
/// All instruments use the shared <c>"Encina"</c> meter for consistency with
/// <see cref="ShardRoutingMetrics"/> and <c>ShardedDatabasePoolMetrics</c>.
/// </para>
/// </remarks>
#pragma warning disable CA1848 // Use the LoggerMessage delegates
public sealed class ShardedReadWriteMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private readonly Counter<long> _routingTotal;
    private readonly Histogram<double> _replicaSelectionDuration;
    private readonly Histogram<double> _replicaLatency;
    private readonly Counter<long> _fallbackToPrimary;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardedReadWriteMetrics"/> class,
    /// registering all read/write separation metric instruments.
    /// </summary>
    /// <param name="topology">The shard topology for observable gauge callbacks.</param>
    /// <param name="healthTracker">The replica health tracker for observable gauge callbacks.</param>
    public ShardedReadWriteMetrics(ShardTopology topology, IReplicaHealthTracker healthTracker)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(healthTracker);

        _routingTotal = Meter.CreateCounter<long>(
            "encina.sharding.readwrite.routing_total",
            description: "Number of read/write routing decisions made.");

        _replicaSelectionDuration = Meter.CreateHistogram<double>(
            "encina.sharding.readwrite.replica_selection_duration_ms",
            unit: "ms",
            description: "Duration of replica selection operations in milliseconds.");

        _replicaLatency = Meter.CreateHistogram<double>(
            "encina.sharding.readwrite.replica_latency_ms",
            unit: "ms",
            description: "Observed replica connection latency in milliseconds.");

        _fallbackToPrimary = Meter.CreateCounter<long>(
            "encina.sharding.readwrite.fallback_to_primary_total",
            description: "Number of times routing fell back to the primary due to no available replicas.");

        Meter.CreateObservableGauge(
            "encina.sharding.readwrite.replica_unhealthy_count",
            () => ObserveUnhealthyReplicas(topology, healthTracker),
            unit: "{replicas}",
            description: "Number of unhealthy replicas per shard.");

        Meter.CreateObservableGauge(
            "encina.sharding.readwrite.replication_lag_ms",
            () => ObserveReplicationLag(topology, healthTracker),
            unit: "ms",
            description: "Observed replication lag per replica in milliseconds.");
    }

    /// <summary>
    /// Records a read/write routing decision.
    /// </summary>
    /// <param name="shardId">The resolved shard ID.</param>
    /// <param name="intent">The routing intent (<c>"read"</c> or <c>"write"</c>).</param>
    /// <param name="replicaId">The selected replica identifier, or <c>null</c> for primary.</param>
    public void RecordRoutingDecision(string shardId, string intent, string? replicaId = null)
    {
        var tags = new TagList
        {
            { ActivityTagNames.ShardId, shardId },
            { ActivityTagNames.ReadWriteIntent, intent },
        };

        if (replicaId is not null)
        {
            tags.Add(ActivityTagNames.ReplicaId, replicaId);
        }

        _routingTotal.Add(1, tags);
    }

    /// <summary>
    /// Records the duration of a replica selection operation.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="strategy">The selection strategy used.</param>
    /// <param name="durationMs">The selection duration in milliseconds.</param>
    public void RecordReplicaSelectionDuration(string shardId, string strategy, double durationMs)
    {
        var tags = new TagList
        {
            { ActivityTagNames.ShardId, shardId },
            { ActivityTagNames.ReplicaSelectionStrategy, strategy },
        };

        _replicaSelectionDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Records observed connection latency for a replica.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="replicaId">The replica identifier.</param>
    /// <param name="latencyMs">The connection latency in milliseconds.</param>
    public void RecordReplicaLatency(string shardId, string replicaId, double latencyMs)
    {
        var tags = new TagList
        {
            { ActivityTagNames.ShardId, shardId },
            { ActivityTagNames.ReplicaId, replicaId },
        };

        _replicaLatency.Record(latencyMs, tags);
    }

    /// <summary>
    /// Records a fallback to the primary connection because no healthy replicas are available.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="reason">The reason for the fallback (e.g., "no_replicas", "all_unhealthy", "all_stale").</param>
    public void RecordFallbackToPrimary(string shardId, string reason)
    {
        var tags = new TagList
        {
            { ActivityTagNames.ShardId, shardId },
            { ActivityTagNames.ReplicaFallbackReason, reason },
        };

        _fallbackToPrimary.Add(1, tags);
    }

    private static IEnumerable<Measurement<int>> ObserveUnhealthyReplicas(
        ShardTopology topology, IReplicaHealthTracker healthTracker)
    {
        foreach (var shard in topology.GetAllShards())
        {
            if (!shard.HasReplicas)
            {
                continue;
            }

            var healthStates = healthTracker.GetAllHealthStates(shard.ShardId);
            var unhealthyCount = 0;

            foreach (var (_, state) in healthStates)
            {
                if (!state.IsHealthy)
                {
                    unhealthyCount++;
                }
            }

            yield return new Measurement<int>(
                unhealthyCount,
                new KeyValuePair<string, object?>(ActivityTagNames.ShardId, shard.ShardId));
        }
    }

    private static IEnumerable<Measurement<double>> ObserveReplicationLag(
        ShardTopology topology, IReplicaHealthTracker healthTracker)
    {
        foreach (var shard in topology.GetAllShards())
        {
            if (!shard.HasReplicas)
            {
                continue;
            }

            var healthStates = healthTracker.GetAllHealthStates(shard.ShardId);

            foreach (var (replicaCs, state) in healthStates)
            {
                if (state.ObservedReplicationLag.HasValue)
                {
                    yield return new Measurement<double>(
                        state.ObservedReplicationLag.Value.TotalMilliseconds,
                        new TagList
                        {
                            { ActivityTagNames.ShardId, shard.ShardId },
                            { ActivityTagNames.ReplicaId, replicaCs },
                        });
                }
            }
        }
    }
}
#pragma warning restore CA1848
