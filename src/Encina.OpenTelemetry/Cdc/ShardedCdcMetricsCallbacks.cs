namespace Encina.OpenTelemetry.Cdc;

/// <summary>
/// Provides callback delegates for sharded CDC observable gauge metrics.
/// </summary>
/// <remarks>
/// <para>
/// This class bridges the <c>Encina.Cdc</c> package with <c>Encina.OpenTelemetry</c>
/// without creating a direct project reference. The CDC infrastructure registers an instance
/// of this class with the appropriate callbacks, and the <see cref="ShardedCdcMetricsInitializer"/>
/// uses it to create the <see cref="ShardedCdcMetrics"/> on startup.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registered by Encina.Cdc when sharded capture is enabled:
/// services.AddSingleton(new ShardedCdcMetricsCallbacks(
///     activeConnectorsCallback: () => connector.ActiveShardIds.Count,
///     lagCallback: () => shardLags));
/// </code>
/// </example>
public sealed class ShardedCdcMetricsCallbacks
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShardedCdcMetricsCallbacks"/> class.
    /// </summary>
    /// <param name="activeConnectorsCallback">
    /// Callback that returns the current number of active shard connectors.
    /// </param>
    /// <param name="lagCallback">
    /// Callback that returns per-shard lag measurements as tuples of (shardId, lagMs).
    /// </param>
    public ShardedCdcMetricsCallbacks(
        Func<int> activeConnectorsCallback,
        Func<IEnumerable<(string ShardId, double LagMs)>> lagCallback)
    {
        ArgumentNullException.ThrowIfNull(activeConnectorsCallback);
        ArgumentNullException.ThrowIfNull(lagCallback);

        ActiveConnectorsCallback = activeConnectorsCallback;
        LagCallback = lagCallback;
    }

    /// <summary>
    /// Gets the callback that returns the current number of active shard connectors.
    /// </summary>
    internal Func<int> ActiveConnectorsCallback { get; }

    /// <summary>
    /// Gets the callback that returns per-shard lag measurements.
    /// </summary>
    internal Func<IEnumerable<(string ShardId, double LagMs)>> LagCallback { get; }
}
