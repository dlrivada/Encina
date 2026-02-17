using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Sharding.Logging;

/// <summary>
/// High-performance structured logging for shard replica selection and health monitoring.
/// </summary>
/// <remarks>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </remarks>
[ExcludeFromCodeCoverage]
internal static partial class ShardReplicaLogEvents
{
    [LoggerMessage(
        EventId = 650,
        Level = LogLevel.Debug,
        Message = "Replica selected for shard '{ShardId}': replica '{ReplicaId}' using strategy '{Strategy}', estimated latency {LatencyEstimateMs}ms")]
    public static partial void ReplicaSelected(
        ILogger logger,
        string shardId,
        string replicaId,
        string strategy,
        double latencyEstimateMs);

    [LoggerMessage(
        EventId = 651,
        Level = LogLevel.Warning,
        Message = "Replica marked unhealthy for shard '{ShardId}': replica '{ReplicaId}', reason '{FailureReason}', remaining healthy replicas: {RemainingHealthyCount}")]
    public static partial void ReplicaMarkedUnhealthy(
        ILogger logger,
        string shardId,
        string replicaId,
        string failureReason,
        int remainingHealthyCount);

    [LoggerMessage(
        EventId = 652,
        Level = LogLevel.Information,
        Message = "Replica recovered for shard '{ShardId}': replica '{ReplicaId}', downtime duration {DowntimeDurationMs}ms")]
    public static partial void ReplicaRecovered(
        ILogger logger,
        string shardId,
        string replicaId,
        double downtimeDurationMs);

    [LoggerMessage(
        EventId = 653,
        Level = LogLevel.Debug,
        Message = "Scatter-gather using replicas: query type '{QueryType}', targeting {ShardCount} shard(s) with {ReplicasPerShard} replica(s) per shard")]
    public static partial void ScatterGatherUsingReplicas(
        ILogger logger,
        string queryType,
        int shardCount,
        int replicasPerShard);

    [LoggerMessage(
        EventId = 654,
        Level = LogLevel.Warning,
        Message = "Replication lag exceeded threshold for shard '{ShardId}': replica '{ReplicaId}', observed lag {ObservedLagMs}ms, threshold {ThresholdMs}ms")]
    public static partial void ReplicationLagExceeded(
        ILogger logger,
        string shardId,
        string replicaId,
        double observedLagMs,
        double thresholdMs);

    [LoggerMessage(
        EventId = 655,
        Level = LogLevel.Warning,
        Message = "All replicas stale for shard '{ShardId}', falling back to primary. Configured threshold: {ThresholdMs}ms")]
    public static partial void AllReplicasStale(
        ILogger logger,
        string shardId,
        double thresholdMs);

    [LoggerMessage(
        EventId = 656,
        Level = LogLevel.Warning,
        Message = "No healthy replicas for shard '{ShardId}', falling back to primary. Total replicas: {TotalReplicaCount}")]
    public static partial void FallbackToPrimaryNoReplicas(
        ILogger logger,
        string shardId,
        int totalReplicaCount);
}
