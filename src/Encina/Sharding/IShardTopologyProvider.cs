namespace Encina.Sharding;

/// <summary>
/// Provides access to the current <see cref="ShardTopology"/>.
/// </summary>
/// <remarks>
/// <para>
/// The default implementation returns a fixed, immutable topology. Cached implementations
/// can swap the topology atomically via background refresh, enabling dynamic shard
/// topology updates without application restarts.
/// </para>
/// </remarks>
public interface IShardTopologyProvider
{
    /// <summary>
    /// Gets the current shard topology.
    /// </summary>
    /// <returns>The current <see cref="ShardTopology"/> snapshot.</returns>
    ShardTopology GetTopology();
}
