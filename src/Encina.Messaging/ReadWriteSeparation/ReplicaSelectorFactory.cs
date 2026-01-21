namespace Encina.Messaging.ReadWriteSeparation;

/// <summary>
/// Factory for creating <see cref="IReplicaSelector"/> instances based on the configured strategy.
/// </summary>
/// <remarks>
/// <para>
/// This factory encapsulates the creation logic for different replica selection strategies,
/// allowing the DI container to resolve the appropriate implementation based on
/// <see cref="ReadWriteSeparationOptions.ReplicaStrategy"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new ReadWriteSeparationOptions
/// {
///     ReadConnectionStrings = new List&lt;string&gt; { "Server=replica1;...", "Server=replica2;..." },
///     ReplicaStrategy = ReplicaStrategy.LeastConnections
/// };
///
/// var selector = ReplicaSelectorFactory.Create(options);
/// // selector is now a LeastConnectionsReplicaSelector
/// </code>
/// </example>
public static class ReplicaSelectorFactory
{
    /// <summary>
    /// Creates an <see cref="IReplicaSelector"/> based on the configured strategy.
    /// </summary>
    /// <param name="options">The read/write separation configuration options.</param>
    /// <returns>
    /// An <see cref="IReplicaSelector"/> implementation matching the configured
    /// <see cref="ReadWriteSeparationOptions.ReplicaStrategy"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="ReadWriteSeparationOptions.ReadConnectionStrings"/> is empty.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <see cref="ReadWriteSeparationOptions.ReplicaStrategy"/> has an unknown value.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The following strategies are supported:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <see cref="ReplicaStrategy.RoundRobin"/>: Creates a <see cref="RoundRobinReplicaSelector"/>
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <see cref="ReplicaStrategy.Random"/>: Creates a <see cref="RandomReplicaSelector"/>
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <see cref="ReplicaStrategy.LeastConnections"/>: Creates a <see cref="LeastConnectionsReplicaSelector"/>
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    public static IReplicaSelector Create(ReadWriteSeparationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.ReadConnectionStrings is null || options.ReadConnectionStrings.Count == 0)
        {
            throw new ArgumentException(
                "At least one read connection string must be configured to create a replica selector.",
                nameof(options));
        }

        var replicas = options.ReadConnectionStrings.ToList().AsReadOnly();

        return options.ReplicaStrategy switch
        {
            ReplicaStrategy.RoundRobin => new RoundRobinReplicaSelector(replicas),
            ReplicaStrategy.Random => new RandomReplicaSelector(replicas),
            ReplicaStrategy.LeastConnections => new LeastConnectionsReplicaSelector(replicas),
            _ => throw new ArgumentOutOfRangeException(
                nameof(options),
                options.ReplicaStrategy,
                $"Unknown replica strategy: {options.ReplicaStrategy}")
        };
    }

    /// <summary>
    /// Creates an <see cref="IReplicaSelector"/> for the specified replicas and strategy.
    /// </summary>
    /// <param name="replicas">The list of read replica connection strings.</param>
    /// <param name="strategy">The strategy to use for selecting replicas.</param>
    /// <returns>
    /// An <see cref="IReplicaSelector"/> implementation matching the specified strategy.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="replicas"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="replicas"/> is empty.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="strategy"/> has an unknown value.
    /// </exception>
    public static IReplicaSelector Create(IReadOnlyList<string> replicas, ReplicaStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(replicas);

        if (replicas.Count == 0)
        {
            throw new ArgumentException(
                "At least one replica connection string must be provided.",
                nameof(replicas));
        }

        return strategy switch
        {
            ReplicaStrategy.RoundRobin => new RoundRobinReplicaSelector(replicas),
            ReplicaStrategy.Random => new RandomReplicaSelector(replicas),
            ReplicaStrategy.LeastConnections => new LeastConnectionsReplicaSelector(replicas),
            _ => throw new ArgumentOutOfRangeException(
                nameof(strategy),
                strategy,
                $"Unknown replica strategy: {strategy}")
        };
    }
}
