namespace Encina.Caching.Sharding;

/// <summary>
/// Provides push-based notification when the shard topology changes externally.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to trigger immediate topology refreshes from external sources
/// (e.g., configuration change watchers, service discovery callbacks, admin API webhooks).
/// </para>
/// <para>
/// When the <see cref="TopologyChanged"/> event fires, the <see cref="CachedShardTopologyProvider"/>
/// immediately initiates a topology refresh, rather than waiting for the next periodic refresh cycle.
/// </para>
/// </remarks>
public interface IShardTopologyChangeNotifier
{
    /// <summary>
    /// Raised when the topology is known to have changed and should be refreshed.
    /// </summary>
    event EventHandler? TopologyChanged;
}
