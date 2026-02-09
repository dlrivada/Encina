using Encina.Cdc.Abstractions;
using Encina.Cdc.Health;

namespace Encina.Cdc.Debezium.Health;

/// <summary>
/// Health check for Debezium HTTP Consumer CDC infrastructure.
/// Verifies connector accessibility and position store status.
/// </summary>
/// <remarks>
/// Tags: "encina", "cdc", "ready", "debezium", "http" for Kubernetes readiness probes.
/// </remarks>
public class DebeziumCdcHealthCheck : CdcHealthCheck
{
    private static readonly string[] ProviderTags = ["debezium", "http"];

    /// <summary>
    /// The default health check name.
    /// </summary>
    public const string DefaultName = "encina-cdc-debezium";

    /// <summary>
    /// Initializes a new instance of the <see cref="DebeziumCdcHealthCheck"/> class.
    /// </summary>
    /// <param name="connector">The Debezium CDC connector to check.</param>
    /// <param name="positionStore">The position store to verify accessibility.</param>
    public DebeziumCdcHealthCheck(
        ICdcConnector connector,
        ICdcPositionStore positionStore)
        : base(DefaultName, connector, positionStore, ProviderTags)
    {
    }
}
