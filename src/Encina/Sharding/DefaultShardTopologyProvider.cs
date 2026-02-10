namespace Encina.Sharding;

/// <summary>
/// Default implementation of <see cref="IShardTopologyProvider"/> that returns
/// a fixed, immutable <see cref="ShardTopology"/>.
/// </summary>
/// <remarks>
/// This provider is registered automatically by <c>AddEncinaSharding</c> and
/// returns the topology built during service configuration. For dynamic topology
/// updates, use the cached provider from <c>Encina.Caching</c>.
/// </remarks>
public sealed class DefaultShardTopologyProvider : IShardTopologyProvider
{
    private readonly ShardTopology _topology;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultShardTopologyProvider"/> class.
    /// </summary>
    /// <param name="topology">The shard topology to serve.</param>
    public DefaultShardTopologyProvider(ShardTopology topology)
    {
        ArgumentNullException.ThrowIfNull(topology);
        _topology = topology;
    }

    /// <inheritdoc />
    public ShardTopology GetTopology() => _topology;
}
