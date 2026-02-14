namespace Encina.Sharding.ReplicaSelection;

/// <summary>
/// Defines the strategy for selecting read replicas within a shard when distributing read requests.
/// </summary>
/// <remarks>
/// <para>
/// Each shard in a sharded topology can have multiple read replicas. This enum determines
/// how read requests are distributed across those replicas. Per-shard strategies can be
/// configured via <see cref="ShardInfo.ReplicaStrategy"/>, or a global default can be set
/// in <see cref="ShardedReadWriteOptions.DefaultReplicaStrategy"/>.
/// </para>
/// <para>
/// <b>Choosing a Strategy:</b>
/// <list type="bullet">
///   <item><description><see cref="RoundRobin"/>: Best for evenly distributing load across replicas with similar capacity.</description></item>
///   <item><description><see cref="Random"/>: Simple approach that avoids tracking state; works well for unpredictable patterns.</description></item>
///   <item><description><see cref="LeastLatency"/>: Adaptive; routes to the fastest replica based on observed latency.</description></item>
///   <item><description><see cref="LeastConnections"/>: Best when query execution times vary significantly.</description></item>
///   <item><description><see cref="WeightedRandom"/>: Best when replicas have different hardware capacities.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Per-shard strategy override
/// var shard = new ShardInfo("shard-0", "Server=primary;...",
///     ReplicaConnectionStrings: new[] { "Server=replica1;...", "Server=replica2;..." },
///     ReplicaStrategy: ReplicaSelectionStrategy.LeastLatency);
///
/// // Global default strategy
/// options.DefaultReplicaStrategy = ReplicaSelectionStrategy.RoundRobin;
/// </code>
/// </example>
public enum ReplicaSelectionStrategy
{
    /// <summary>
    /// Distributes requests evenly across replicas in a circular order.
    /// </summary>
    /// <remarks>
    /// Uses a thread-safe counter with <see cref="System.Threading.Interlocked"/> operations
    /// to ensure consistent round-robin behavior in concurrent scenarios.
    /// </remarks>
    RoundRobin = 0,

    /// <summary>
    /// Selects replicas randomly for each request.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="System.Random.Shared"/> for thread-safe random selection.
    /// Over time, this provides approximately even distribution without tracking state.
    /// </remarks>
    Random = 1,

    /// <summary>
    /// Selects the replica with the lowest observed latency.
    /// </summary>
    /// <remarks>
    /// Tracks per-replica latency measurements and routes to the fastest replica.
    /// Requires external reporting of latency via <see cref="IShardReplicaSelector"/> to be effective.
    /// Falls back to round-robin when no latency data is available.
    /// </remarks>
    LeastLatency = 2,

    /// <summary>
    /// Selects the replica with the fewest active connections.
    /// </summary>
    /// <remarks>
    /// Tracks the number of active connections to each replica and routes new requests
    /// to the replica with the lowest current load. Adapts to varying query execution times.
    /// </remarks>
    LeastConnections = 3,

    /// <summary>
    /// Selects replicas randomly, weighted by configured capacity weights.
    /// </summary>
    /// <remarks>
    /// Each replica is assigned a weight (higher = more traffic). Useful when replicas
    /// have different hardware capacities. Weights are configured per replica index.
    /// </remarks>
    WeightedRandom = 4
}
