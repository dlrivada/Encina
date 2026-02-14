namespace Encina.Sharding.ReplicaSelection;

/// <summary>
/// Selects a read replica from a list of available replicas for a given shard.
/// </summary>
/// <remarks>
/// <para>
/// Implementations encapsulate the selection algorithm (round-robin, random, least-latency, etc.)
/// and are instantiated per strategy via <see cref="ShardReplicaSelectorFactory"/>.
/// </para>
/// <para>
/// All implementations must be thread-safe, as the same selector instance may be shared
/// across concurrent requests targeting the same shard.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// IShardReplicaSelector selector = factory.Create(ReplicaSelectionStrategy.RoundRobin);
/// string replicaConnectionString = selector.SelectReplica(shard.ReplicaConnectionStrings);
/// </code>
/// </example>
public interface IShardReplicaSelector
{
    /// <summary>
    /// Selects a replica connection string from the available replicas.
    /// </summary>
    /// <param name="availableReplicas">
    /// The list of available replica connection strings to choose from.
    /// Must contain at least one element.
    /// </param>
    /// <returns>The selected replica connection string.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="availableReplicas"/> is empty.
    /// </exception>
    string SelectReplica(IReadOnlyList<string> availableReplicas);
}
