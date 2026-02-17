namespace Encina.Sharding.ReplicaSelection;

/// <summary>
/// Selects replicas randomly using <see cref="Random.Shared"/>.
/// </summary>
/// <remarks>
/// Uses the thread-safe <see cref="Random.Shared"/> instance, providing approximately
/// even distribution over time without the overhead of tracking state.
/// </remarks>
public sealed class RandomShardReplicaSelector : IShardReplicaSelector
{
    /// <inheritdoc />
    public string SelectReplica(IReadOnlyList<string> availableReplicas)
    {
        ArgumentNullException.ThrowIfNull(availableReplicas);

        if (availableReplicas.Count == 0)
        {
            throw new ArgumentException("Available replicas list must contain at least one element.", nameof(availableReplicas));
        }

        var index = Random.Shared.Next(availableReplicas.Count);
        return availableReplicas[index];
    }
}
