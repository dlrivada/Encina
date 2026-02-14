namespace Encina.Sharding.ReplicaSelection;

/// <summary>
/// Selects replicas in a circular order using a thread-safe counter.
/// </summary>
/// <remarks>
/// <para>
/// Each call advances an internal counter and returns the next replica in the list.
/// The counter wraps around when it reaches the end of the list.
/// </para>
/// <para>
/// Thread safety is achieved using <see cref="Interlocked.Increment(ref int)"/>,
/// making this implementation safe for concurrent use without locks.
/// </para>
/// </remarks>
public sealed class RoundRobinShardReplicaSelector : IShardReplicaSelector
{
    private int _counter = -1;

    /// <inheritdoc />
    public string SelectReplica(IReadOnlyList<string> availableReplicas)
    {
        ArgumentNullException.ThrowIfNull(availableReplicas);

        if (availableReplicas.Count == 0)
        {
            throw new ArgumentException("Available replicas list must contain at least one element.", nameof(availableReplicas));
        }

        var next = Interlocked.Increment(ref _counter);
        // Use unsigned modulo to handle int overflow gracefully
        var index = (int)((uint)next % (uint)availableReplicas.Count);
        return availableReplicas[index];
    }
}
