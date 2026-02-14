namespace Encina.Sharding.ReplicaSelection;

/// <summary>
/// Selects replicas randomly, weighted by configured capacity weights.
/// </summary>
/// <remarks>
/// <para>
/// Each replica is assigned a weight (higher = more traffic). This strategy uses cumulative
/// weight distribution for O(log n) selection via binary search.
/// </para>
/// <para>
/// When no weights are configured, all replicas are treated equally (weight = 1).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Replica 0 gets ~60% traffic, replica 1 gets ~40%
/// var selector = new WeightedRandomShardReplicaSelector(new[] { 3, 2 });
/// var selected = selector.SelectReplica(replicas);
/// </code>
/// </example>
public sealed class WeightedRandomShardReplicaSelector : IShardReplicaSelector
{
    private readonly IReadOnlyList<int> _weights;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeightedRandomShardReplicaSelector"/> class.
    /// </summary>
    /// <param name="weights">
    /// Weights per replica index. Each weight must be greater than zero.
    /// If the list is shorter than the replica count, remaining replicas get weight 1.
    /// If <see langword="null"/> or empty, all replicas are treated equally.
    /// </param>
    public WeightedRandomShardReplicaSelector(IReadOnlyList<int>? weights = null)
    {
        if (weights is not null)
        {
            for (var i = 0; i < weights.Count; i++)
            {
                if (weights[i] <= 0)
                {
                    throw new ArgumentException(
                        $"Weight at index {i} must be greater than zero, but was {weights[i]}.",
                        nameof(weights));
                }
            }
        }

        _weights = weights ?? [];
    }

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

        // Build cumulative weights
        var totalWeight = 0;
        Span<int> cumulativeWeights = availableReplicas.Count <= 64
            ? stackalloc int[availableReplicas.Count]
            : new int[availableReplicas.Count];

        for (var i = 0; i < availableReplicas.Count; i++)
        {
            var weight = i < _weights.Count ? _weights[i] : 1;
            totalWeight += weight;
            cumulativeWeights[i] = totalWeight;
        }

        // Select a random value in [0, totalWeight) and find the corresponding replica
        var randomValue = Random.Shared.Next(totalWeight);

        for (var i = 0; i < cumulativeWeights.Length; i++)
        {
            if (randomValue < cumulativeWeights[i])
            {
                return availableReplicas[i];
            }
        }

        // Should not be reached, but return last replica as safety fallback
        return availableReplicas[^1];
    }
}
