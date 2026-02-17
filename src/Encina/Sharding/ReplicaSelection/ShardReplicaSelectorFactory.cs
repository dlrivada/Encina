namespace Encina.Sharding.ReplicaSelection;

/// <summary>
/// Factory that creates <see cref="IShardReplicaSelector"/> instances based on the
/// requested <see cref="ReplicaSelectionStrategy"/>.
/// </summary>
/// <remarks>
/// <para>
/// Each call creates a new selector instance. Callers that need per-shard selectors
/// should cache the returned instance for the lifetime of the shard topology.
/// </para>
/// <para>
/// For the <see cref="ReplicaSelectionStrategy.WeightedRandom"/> strategy, weights
/// can be supplied via the <c>weights</c> parameter of
/// <see cref="Create(ReplicaSelectionStrategy, IReadOnlyList{int}?)"/>.
/// </para>
/// </remarks>
public static class ShardReplicaSelectorFactory
{
    /// <summary>
    /// Creates a new <see cref="IShardReplicaSelector"/> for the specified strategy.
    /// </summary>
    /// <param name="strategy">The replica selection strategy.</param>
    /// <param name="weights">
    /// Optional weights per replica index. Only used with <see cref="ReplicaSelectionStrategy.WeightedRandom"/>.
    /// </param>
    /// <returns>A new <see cref="IShardReplicaSelector"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="strategy"/> is not a defined <see cref="ReplicaSelectionStrategy"/> value.
    /// </exception>
    public static IShardReplicaSelector Create(
        ReplicaSelectionStrategy strategy,
        IReadOnlyList<int>? weights = null) => strategy switch
        {
            ReplicaSelectionStrategy.RoundRobin => new RoundRobinShardReplicaSelector(),
            ReplicaSelectionStrategy.Random => new RandomShardReplicaSelector(),
            ReplicaSelectionStrategy.LeastLatency => new LeastLatencyShardReplicaSelector(),
            ReplicaSelectionStrategy.LeastConnections => new LeastConnectionsShardReplicaSelector(),
            ReplicaSelectionStrategy.WeightedRandom => new WeightedRandomShardReplicaSelector(weights),
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, $"Unknown replica selection strategy: {strategy}.")
        };
}
