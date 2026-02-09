using Encina.Cdc.Abstractions;
using Encina.Cdc.Health;

namespace Encina.Cdc.PostgreSql.Health;

/// <summary>
/// Health check for PostgreSQL Logical Replication CDC infrastructure.
/// Verifies connector connectivity and position store accessibility.
/// </summary>
/// <remarks>
/// Tags: "encina", "cdc", "ready", "postgresql", "logical-replication" for Kubernetes readiness probes.
/// </remarks>
public class PostgresCdcHealthCheck : CdcHealthCheck
{
    private static readonly string[] ProviderTags = ["postgresql", "logical-replication"];

    /// <summary>
    /// The default health check name.
    /// </summary>
    public const string DefaultName = "encina-cdc-postgres";

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresCdcHealthCheck"/> class.
    /// </summary>
    /// <param name="connector">The PostgreSQL CDC connector to check.</param>
    /// <param name="positionStore">The position store to verify accessibility.</param>
    public PostgresCdcHealthCheck(
        ICdcConnector connector,
        ICdcPositionStore positionStore)
        : base(DefaultName, connector, positionStore, ProviderTags)
    {
    }
}
