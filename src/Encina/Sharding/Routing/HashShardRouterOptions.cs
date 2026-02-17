namespace Encina.Sharding.Routing;

/// <summary>
/// Configuration options for <see cref="HashShardRouter"/>.
/// </summary>
public sealed class HashShardRouterOptions
{
    /// <summary>
    /// Gets or sets the number of virtual nodes per physical shard on the hash ring.
    /// </summary>
    /// <value>The default is 150.</value>
    /// <remarks>
    /// Higher values provide more uniform distribution at the cost of slightly more memory.
    /// Values between 100 and 200 are recommended for most workloads.
    /// </remarks>
    public int VirtualNodesPerShard { get; set; } = 150;

    /// <summary>
    /// Gets or sets the delimiter used to join compound key components before hashing.
    /// </summary>
    /// <value>The default is <c>"|"</c>.</value>
    /// <remarks>
    /// When routing a <see cref="CompoundShardKey"/>, the components are joined with
    /// this delimiter into a single string that is then hashed. A single-component
    /// compound key produces the same hash as the equivalent plain string.
    /// </remarks>
    public string ComponentDelimiter { get; set; } = "|";
}
