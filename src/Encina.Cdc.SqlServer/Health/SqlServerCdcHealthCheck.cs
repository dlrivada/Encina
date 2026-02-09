using Encina.Cdc.Abstractions;
using Encina.Cdc.Health;

namespace Encina.Cdc.SqlServer.Health;

/// <summary>
/// Health check for SQL Server Change Tracking CDC infrastructure.
/// Verifies connector connectivity and position store accessibility.
/// </summary>
/// <remarks>
/// Tags: "encina", "cdc", "ready", "sqlserver", "change-tracking" for Kubernetes readiness probes.
/// </remarks>
public class SqlServerCdcHealthCheck : CdcHealthCheck
{
    private static readonly string[] ProviderTags = ["sqlserver", "change-tracking"];

    /// <summary>
    /// The default health check name.
    /// </summary>
    public const string DefaultName = "encina-cdc-sqlserver";

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerCdcHealthCheck"/> class.
    /// </summary>
    /// <param name="connector">The SQL Server CDC connector to check.</param>
    /// <param name="positionStore">The position store to verify accessibility.</param>
    public SqlServerCdcHealthCheck(
        ICdcConnector connector,
        ICdcPositionStore positionStore)
        : base(DefaultName, connector, positionStore, ProviderTags)
    {
    }
}
