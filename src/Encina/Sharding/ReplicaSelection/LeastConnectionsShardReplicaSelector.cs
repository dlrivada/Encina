using System.Collections.Concurrent;

namespace Encina.Sharding.ReplicaSelection;

/// <summary>
/// Selects the replica with the fewest active connections.
/// </summary>
/// <remarks>
/// <para>
/// Tracks the number of active connections to each replica using a thread-safe concurrent dictionary
/// with <see cref="Interlocked"/> operations. Routes new requests to the replica with the lowest
/// current load.
/// </para>
/// <para>
/// Callers must call <see cref="IncrementConnections"/> when a connection is opened and
/// <see cref="DecrementConnections"/> when a connection is closed to keep the counts accurate.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var selector = new LeastConnectionsShardReplicaSelector();
/// var replica = selector.SelectReplica(replicas);
///
/// selector.IncrementConnections(replica);
/// try
/// {
///     // Use the connection...
/// }
/// finally
/// {
///     selector.DecrementConnections(replica);
/// }
/// </code>
/// </example>
public sealed class LeastConnectionsShardReplicaSelector : IShardReplicaSelector
{
    private readonly ConcurrentDictionary<string, int> _connectionCounts = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public string SelectReplica(IReadOnlyList<string> availableReplicas)
    {
        ArgumentNullException.ThrowIfNull(availableReplicas);

        if (availableReplicas.Count == 0)
        {
            throw new ArgumentException("Available replicas list must contain at least one element.", nameof(availableReplicas));
        }

        if (availableReplicas.Count == 1)
        {
            return availableReplicas[0];
        }

        var bestReplica = availableReplicas[0];
        var bestCount = GetConnectionCount(bestReplica);

        for (var i = 1; i < availableReplicas.Count; i++)
        {
            var count = GetConnectionCount(availableReplicas[i]);
            if (count < bestCount)
            {
                bestCount = count;
                bestReplica = availableReplicas[i];
            }
        }

        return bestReplica;
    }

    /// <summary>
    /// Increments the active connection count for a replica.
    /// </summary>
    /// <param name="replicaConnectionString">The replica connection string.</param>
    public void IncrementConnections(string replicaConnectionString)
    {
        ArgumentNullException.ThrowIfNull(replicaConnectionString);
        _connectionCounts.AddOrUpdate(replicaConnectionString, 1, (_, count) => Interlocked.Increment(ref count));
    }

    /// <summary>
    /// Decrements the active connection count for a replica.
    /// </summary>
    /// <param name="replicaConnectionString">The replica connection string.</param>
    public void DecrementConnections(string replicaConnectionString)
    {
        ArgumentNullException.ThrowIfNull(replicaConnectionString);
        _connectionCounts.AddOrUpdate(replicaConnectionString, 0, (_, count) => Math.Max(0, Interlocked.Decrement(ref count)));
    }

    /// <summary>
    /// Gets the current active connection count for a replica.
    /// </summary>
    /// <param name="replicaConnectionString">The replica connection string.</param>
    /// <returns>The number of active connections, or 0 if not tracked.</returns>
    public int GetConnectionCount(string replicaConnectionString)
        => _connectionCounts.GetValueOrDefault(replicaConnectionString, 0);

    /// <summary>
    /// Resets all connection counts.
    /// </summary>
    public void Reset() => _connectionCounts.Clear();
}
