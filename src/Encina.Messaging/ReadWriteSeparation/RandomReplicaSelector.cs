namespace Encina.Messaging.ReadWriteSeparation;

/// <summary>
/// Selects read replicas randomly.
/// </summary>
/// <remarks>
/// <para>
/// This selector randomly chooses a replica for each request using <see cref="Random.Shared"/>,
/// which is thread-safe and provides good performance in concurrent scenarios.
/// </para>
/// <para>
/// Over time, this approach provides approximately even distribution across replicas
/// while avoiding the overhead of tracking state. It's particularly useful when
/// request patterns are unpredictable or when added randomness helps avoid hotspots.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var replicas = new List&lt;string&gt;
/// {
///     "Server=replica1;...",
///     "Server=replica2;...",
///     "Server=replica3;..."
/// };
/// var selector = new RandomReplicaSelector(replicas);
///
/// // Each call returns a randomly selected replica
/// var selected = selector.SelectReplica(); // Could be any replica
/// </code>
/// </example>
public sealed class RandomReplicaSelector : IReplicaSelector
{
    private readonly IReadOnlyList<string> _replicas;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomReplicaSelector"/> class.
    /// </summary>
    /// <param name="replicas">The list of read replica connection strings.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="replicas"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="replicas"/> is empty.
    /// </exception>
    public RandomReplicaSelector(IReadOnlyList<string> replicas)
    {
        ArgumentNullException.ThrowIfNull(replicas);

        if (replicas.Count == 0)
        {
            throw new ArgumentException("At least one replica connection string must be provided.", nameof(replicas));
        }

        _replicas = replicas;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses <see cref="Random.Shared"/> for thread-safe random selection.
    /// This static instance is designed for concurrent use and provides good performance.
    /// </remarks>
    public string SelectReplica()
    {
        var index = Random.Shared.Next(_replicas.Count);
        return _replicas[index];
    }
}
