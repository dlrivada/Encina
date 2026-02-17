using Encina.Cdc.Abstractions;
using Encina.Cdc.Health;

namespace Encina.Cdc.Debezium.Kafka;

/// <summary>
/// Health check for Debezium Kafka Consumer CDC infrastructure.
/// Verifies connector accessibility and position store status.
/// </summary>
/// <remarks>
/// Tags: "encina", "cdc", "ready", "debezium", "kafka" for Kubernetes readiness probes.
/// </remarks>
public class DebeziumKafkaHealthCheck : CdcHealthCheck
{
    private static readonly string[] ProviderTags = ["debezium", "kafka"];

    /// <summary>
    /// The default health check name.
    /// </summary>
    public const string DefaultName = "encina-cdc-debezium-kafka";

    /// <summary>
    /// Initializes a new instance of the <see cref="DebeziumKafkaHealthCheck"/> class.
    /// </summary>
    /// <param name="connector">The Debezium Kafka CDC connector to check.</param>
    /// <param name="positionStore">The position store to verify accessibility.</param>
    public DebeziumKafkaHealthCheck(
        ICdcConnector connector,
        ICdcPositionStore positionStore)
        : base(DefaultName, connector, positionStore, ProviderTags)
    {
    }
}
