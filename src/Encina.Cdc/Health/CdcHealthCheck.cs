using Encina.Cdc.Abstractions;
using Encina.Messaging.Health;

namespace Encina.Cdc.Health;

/// <summary>
/// Base health check for CDC infrastructure.
/// Verifies connector connectivity, position store accessibility, and processing status.
/// </summary>
/// <remarks>
/// <para>
/// This health check provides a base implementation that checks:
/// <list type="bullet">
///   <item><description>Connector connectivity via <see cref="ICdcConnector.GetCurrentPositionAsync"/></description></item>
///   <item><description>Position store accessibility via <see cref="ICdcPositionStore.GetPositionAsync"/></description></item>
/// </list>
/// </para>
/// <para>
/// Provider-specific health checks can extend this class to add database-specific checks.
/// Tags: "encina", "cdc", "ready" for Kubernetes readiness probes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Provider-specific health check
/// public class SqlServerCdcHealthCheck : CdcHealthCheck
/// {
///     public SqlServerCdcHealthCheck(
///         ICdcConnector connector,
///         ICdcPositionStore positionStore)
///         : base("encina-cdc-sqlserver", connector, positionStore, ["sqlserver"])
///     {
///     }
/// }
/// </code>
/// </example>
public class CdcHealthCheck : EncinaHealthCheck
{
    private static readonly string[] DefaultTags = ["encina", "cdc", "ready"];

    private readonly ICdcConnector _connector;
    private readonly ICdcPositionStore _positionStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="CdcHealthCheck"/> class.
    /// </summary>
    /// <param name="name">The name of the health check.</param>
    /// <param name="connector">The CDC connector to check.</param>
    /// <param name="positionStore">The position store to verify accessibility.</param>
    /// <param name="providerTags">Optional provider-specific tags to include.</param>
    protected CdcHealthCheck(
        string name,
        ICdcConnector connector,
        ICdcPositionStore positionStore,
        IReadOnlyCollection<string>? providerTags = null)
        : base(name, CombineWithProviderTags(providerTags))
    {
        ArgumentNullException.ThrowIfNull(connector);
        ArgumentNullException.ThrowIfNull(positionStore);

        _connector = connector;
        _positionStore = positionStore;
    }

    /// <inheritdoc />
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, object>
        {
            ["connector_id"] = _connector.ConnectorId
        };

        // Check connector connectivity by getting current position
        var positionResult = await _connector.GetCurrentPositionAsync(cancellationToken).ConfigureAwait(false);

        var isConnectorHealthy = positionResult.Match(
            position =>
            {
                data["current_position"] = position.ToString();
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
                $"CDC connector '{_connector.ConnectorId}' is not accessible",
                data: data);
        }

        // Check position store accessibility
        var storeResult = await _positionStore.GetPositionAsync(
            _connector.ConnectorId, cancellationToken).ConfigureAwait(false);

        var isStoreHealthy = storeResult.Match(
            position =>
            {
                data["last_saved_position"] = position.Match(
                    p => p.ToString(),
                    () => "none");
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
                $"CDC position store is not accessible for connector '{_connector.ConnectorId}'",
                data: data);
        }

        return HealthCheckResult.Healthy(
            $"CDC connector '{_connector.ConnectorId}' is healthy",
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
