using Encina.Sharding;

namespace Encina.Caching.Sharding;

/// <summary>
/// Provides a source for loading shard topology data, enabling dynamic topology refreshes.
/// </summary>
/// <remarks>
/// Implement this interface to load shard information from external sources such as
/// databases, configuration services, or service discovery systems. The default
/// implementation (<see cref="StaticShardTopologySource"/>) returns the same shards
/// from the initially configured <see cref="ShardTopology"/>.
/// </remarks>
public interface IShardTopologySource
{
    /// <summary>
    /// Loads the current set of shard definitions.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An enumerable of <see cref="ShardInfo"/> describing all known shards.</returns>
    Task<IEnumerable<ShardInfo>> LoadShardsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of <see cref="IShardTopologySource"/> that returns
/// the shards from a fixed <see cref="ShardTopology"/>.
/// </summary>
/// <remarks>
/// This source is registered automatically when no custom <see cref="IShardTopologySource"/>
/// is provided. It returns the same shards used during initial configuration, meaning
/// the topology never changes unless the application is restarted.
/// </remarks>
public sealed class StaticShardTopologySource : IShardTopologySource
{
    private readonly ShardTopology _topology;

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticShardTopologySource"/> class.
    /// </summary>
    /// <param name="topology">The fixed shard topology.</param>
    public StaticShardTopologySource(ShardTopology topology)
    {
        ArgumentNullException.ThrowIfNull(topology);
        _topology = topology;
    }

    /// <inheritdoc />
    public Task<IEnumerable<ShardInfo>> LoadShardsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ShardInfo>>(_topology.GetAllShards());
    }
}
