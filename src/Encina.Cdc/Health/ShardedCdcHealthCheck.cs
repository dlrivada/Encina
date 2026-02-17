using Encina.Cdc.Abstractions;
using Encina.Messaging.Health;

namespace Encina.Cdc.Health;

/// <summary>
/// Health check for sharded CDC infrastructure.
/// Verifies per-shard connector connectivity and position store accessibility.
/// </summary>
/// <remarks>
/// <para>
/// This health check iterates over all active shards and checks:
/// <list type="bullet">
///   <item><description>Connector positions via <see cref="IShardedCdcConnector.GetAllPositionsAsync"/></description></item>
///   <item><description>Position store accessibility via <see cref="IShardedCdcPositionStore.GetAllPositionsAsync"/></description></item>
/// </list>
/// </para>
/// <para>
/// The overall status is determined by individual shard health:
/// <list type="bullet">
///   <item><description><b>Healthy</b>: All shards are accessible and positions are trackable.</description></item>
///   <item><description><b>Degraded</b>: Some shards have issues (e.g., position store not accessible)
///   but at least one shard is healthy.</description></item>
///   <item><description><b>Unhealthy</b>: No active shards found or the connector is not accessible.</description></item>
/// </list>
/// </para>
/// <para>
/// Tags: "encina", "cdc", "sharded", "ready" for Kubernetes readiness probes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register the health check:
/// services.AddSingleton&lt;IEncinaHealthCheck&gt;(sp =>
///     new ShardedCdcHealthCheck(
///         sp.GetRequiredService&lt;IShardedCdcConnector&gt;(),
///         sp.GetRequiredService&lt;IShardedCdcPositionStore&gt;()));
/// </code>
/// </example>
public class ShardedCdcHealthCheck : EncinaHealthCheck
{
    private static readonly string[] DefaultTags = ["encina", "cdc", "sharded", "ready"];

    private readonly IShardedCdcConnector _connector;
    private readonly IShardedCdcPositionStore _positionStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardedCdcHealthCheck"/> class.
    /// </summary>
    /// <param name="connector">The sharded CDC connector to check.</param>
    /// <param name="positionStore">The sharded position store to verify accessibility.</param>
    /// <param name="providerTags">Optional provider-specific tags to include.</param>
    public ShardedCdcHealthCheck(
        IShardedCdcConnector connector,
        IShardedCdcPositionStore positionStore,
        IReadOnlyCollection<string>? providerTags = null)
        : base("encina-cdc-sharded", CombineWithProviderTags(providerTags))
    {
        ArgumentNullException.ThrowIfNull(connector);
        ArgumentNullException.ThrowIfNull(positionStore);

        _connector = connector;
        _positionStore = positionStore;
    }

    /// <inheritdoc />
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        var connectorId = _connector.GetConnectorId();
        var data = new Dictionary<string, object>
        {
            ["connector_id"] = connectorId
        };

        var activeShardIds = _connector.ActiveShardIds;
        data["active_shards"] = activeShardIds.Count;

        if (activeShardIds.Count == 0)
        {
            return HealthCheckResult.Unhealthy(
                $"Sharded CDC connector '{connectorId}' has no active shards",
                data: data);
        }

        data["shard_ids"] = string.Join(", ", activeShardIds);

        // Check connector positions across all shards
        var positionsResult = await _connector.GetAllPositionsAsync(cancellationToken).ConfigureAwait(false);

        var isConnectorHealthy = positionsResult.Match(
            positions =>
            {
                data["connector_positions_count"] = positions.Count;
                foreach (var (shardId, position) in positions)
                {
                    data[$"shard.{shardId}.position"] = position.ToString();
                }

                return true;
            },
            error =>
            {
                data["connector_error"] = error.ToString();
                return false;
            });

        if (!isConnectorHealthy)
        {
            return HealthCheckResult.Unhealthy(
                $"Sharded CDC connector '{connectorId}' cannot retrieve positions",
                data: data);
        }

        // Check position store accessibility
        var storeResult = await _positionStore.GetAllPositionsAsync(
            connectorId, cancellationToken).ConfigureAwait(false);

        var isStoreHealthy = storeResult.Match(
            positions =>
            {
                data["store_positions_count"] = positions.Count;
                return true;
            },
            error =>
            {
                data["store_error"] = error.ToString();
                return false;
            });

        if (!isStoreHealthy)
        {
            return HealthCheckResult.Degraded(
                $"Sharded CDC position store is not accessible for connector '{connectorId}'",
                data: data);
        }

        return HealthCheckResult.Healthy(
            $"Sharded CDC connector '{connectorId}' is healthy with {activeShardIds.Count} active shard(s)",
            data: data);
    }

    private static string[] CombineWithProviderTags(IReadOnlyCollection<string>? providerTags)
    {
        if (providerTags is null || providerTags.Count == 0)
        {
            return DefaultTags;
        }

        return [.. DefaultTags, .. providerTags];
    }
}
