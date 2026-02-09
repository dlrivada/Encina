using Encina.Cdc.Abstractions;
using Encina.Cdc.Health;

namespace Encina.Cdc.MySql.Health;

/// <summary>
/// Health check for MySQL Binary Log Replication CDC infrastructure.
/// Verifies connector connectivity and position store accessibility.
/// </summary>
/// <remarks>
/// Tags: "encina", "cdc", "ready", "mysql", "binlog" for Kubernetes readiness probes.
/// </remarks>
public class MySqlCdcHealthCheck : CdcHealthCheck
{
    private static readonly string[] ProviderTags = ["mysql", "binlog"];

    /// <summary>
    /// The default health check name.
    /// </summary>
    public const string DefaultName = "encina-cdc-mysql";

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlCdcHealthCheck"/> class.
    /// </summary>
    /// <param name="connector">The MySQL CDC connector to check.</param>
    /// <param name="positionStore">The position store to verify accessibility.</param>
    public MySqlCdcHealthCheck(
        ICdcConnector connector,
        ICdcPositionStore positionStore)
        : base(DefaultName, connector, positionStore, ProviderTags)
    {
    }
}
