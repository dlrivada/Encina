namespace Encina.Sharding.Routing;

/// <summary>
/// Configuration options for <see cref="CompoundShardRouter"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps each compound key component index to a dedicated <see cref="IShardRouter"/>
/// that handles routing for that dimension. Component indices must be contiguous
/// starting from 0.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new CompoundShardRouterOptions
/// {
///     ComponentRouters =
///     {
///         [0] = rangeRouterByRegion,
///         [1] = hashRouterByCustomer
///     }
/// };
/// </code>
/// </example>
public sealed class CompoundShardRouterOptions
{
    /// <summary>
    /// Gets the mapping of component indices to their dedicated routers.
    /// </summary>
    /// <remarks>
    /// Indices must be contiguous starting from 0. For example, if there are 2 components,
    /// indices 0 and 1 must both be configured.
    /// </remarks>
    public Dictionary<int, IShardRouter> ComponentRouters { get; } = [];

    /// <summary>
    /// Gets or sets the function that combines per-component shard IDs into a final shard ID.
    /// </summary>
    /// <value>
    /// The default combiner joins results with a hyphen (e.g., <c>"shard-us-shard-42"</c>).
    /// </value>
    /// <remarks>
    /// The combiner receives the ordered list of shard IDs produced by each component router.
    /// Override this to implement custom shard ID composition.
    /// </remarks>
    public Func<IEnumerable<string>, string> ShardIdCombiner { get; set; } =
        results => string.Join("-", results);
}
