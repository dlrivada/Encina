namespace Encina.Messaging.ReadWriteSeparation;

/// <summary>
/// Selects read replicas using a round-robin strategy.
/// </summary>
/// <remarks>
/// <para>
/// This selector distributes requests evenly across all configured replicas by
/// cycling through them in order. Each call to <see cref="SelectReplica"/> returns
/// the next replica in the sequence, wrapping back to the first after the last.
/// </para>
/// <para>
/// The implementation uses <see cref="Interlocked.Increment(ref int)"/> for thread-safe
/// counter management, ensuring consistent round-robin behavior under concurrent access.
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
/// var selector = new RoundRobinReplicaSelector(replicas);
///
/// // Requests are distributed: replica1, replica2, replica3, replica1, replica2, ...
/// var first = selector.SelectReplica();  // replica1
/// var second = selector.SelectReplica(); // replica2
/// var third = selector.SelectReplica();  // replica3
/// var fourth = selector.SelectReplica(); // replica1
/// </code>
/// </example>
public sealed class RoundRobinReplicaSelector : IReplicaSelector
{
    private readonly IReadOnlyList<string> _replicas;
    private int _currentIndex = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoundRobinReplicaSelector"/> class.
    /// </summary>
    /// <param name="replicas">The list of read replica connection strings.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="replicas"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="replicas"/> is empty.
    /// </exception>
    public RoundRobinReplicaSelector(IReadOnlyList<string> replicas)
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
    /// Uses <see cref="Interlocked.Increment(ref int)"/> for thread-safe round-robin selection.
    /// The counter automatically wraps using modulo arithmetic to cycle through replicas.
    /// </remarks>
    public string SelectReplica()
    {
        var index = Interlocked.Increment(ref _currentIndex);

        // Handle potential integer overflow by using absolute value and modulo
        // This ensures we always get a valid positive index
        var replicaIndex = Math.Abs(index % _replicas.Count);

        return _replicas[replicaIndex];
    }
}
