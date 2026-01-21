namespace Encina.Messaging.ReadWriteSeparation;

/// <summary>
/// Defines the strategy for selecting read replicas when distributing read requests.
/// </summary>
/// <remarks>
/// <para>
/// The replica selection strategy determines how read requests are distributed across
/// configured read replicas. Each strategy provides different trade-offs between
/// simplicity, performance, and load distribution.
/// </para>
/// <para>
/// <b>Choosing a Strategy:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="RoundRobin"/>: Best for evenly distributing load across replicas with similar capacity.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="Random"/>: Simple approach that works well when request patterns are unpredictable.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="LeastConnections"/>: Best when replicas have different capacities or query execution
///       times vary significantly.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaEntityFrameworkCore(config =>
/// {
///     config.UseReadWriteSeparation = true;
///     config.ReadWriteSeparationOptions.ReplicaStrategy = ReplicaStrategy.RoundRobin;
/// });
/// </code>
/// </example>
public enum ReplicaStrategy
{
    /// <summary>
    /// Distributes requests evenly across replicas in a circular order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each replica is selected in turn, cycling back to the first replica after the last one.
    /// This provides predictable, even distribution of load across all replicas.
    /// </para>
    /// <para>
    /// The implementation uses a thread-safe counter with interlocked operations
    /// to ensure consistent round-robin behavior in concurrent scenarios.
    /// </para>
    /// </remarks>
    RoundRobin = 0,

    /// <summary>
    /// Selects replicas randomly for each request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses <see cref="System.Random.Shared"/> for thread-safe random selection.
    /// Over time, this provides approximately even distribution while avoiding
    /// the overhead of tracking state.
    /// </para>
    /// <para>
    /// Best suited for scenarios where the added randomness helps avoid
    /// hotspots or when request patterns make round-robin less effective.
    /// </para>
    /// </remarks>
    Random = 1,

    /// <summary>
    /// Selects the replica with the fewest active connections.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tracks the number of active connections to each replica and routes
    /// new requests to the replica with the lowest current load. This strategy
    /// adapts to varying query execution times and replica capacities.
    /// </para>
    /// <para>
    /// This strategy requires tracking connection counts, which adds some overhead
    /// but provides better load balancing when replicas have different performance
    /// characteristics or when query execution times vary significantly.
    /// </para>
    /// </remarks>
    LeastConnections = 2
}
