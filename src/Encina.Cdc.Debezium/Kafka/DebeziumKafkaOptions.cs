namespace Encina.Cdc.Debezium.Kafka;

/// <summary>
/// Configuration options for the Debezium Kafka Consumer CDC connector.
/// </summary>
/// <remarks>
/// <para>
/// This connector consumes Debezium change events from Kafka topics, which is the
/// most common deployment topology for Debezium Connect. Events are read from one or
/// more Kafka topics and mapped to <see cref="ChangeEvent"/> instances using the shared
/// <see cref="DebeziumEventMapper"/>.
/// </para>
/// <para>
/// Example Debezium Connect configuration that writes to Kafka:
/// <code>
/// connector.class=io.debezium.connector.sqlserver.SqlServerConnector
/// database.hostname=db-host
/// topic.prefix=dbserver1
/// </code>
/// Each table produces a topic named <c>{topic.prefix}.{schema}.{table}</c>.
/// </para>
/// </remarks>
public sealed class DebeziumKafkaOptions
{
    /// <summary>
    /// Gets or sets the Kafka bootstrap servers connection string.
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Gets or sets the consumer group ID.
    /// All instances sharing the same group ID participate in the same consumer group,
    /// enabling horizontal scaling with automatic partition assignment.
    /// </summary>
    public string GroupId { get; set; } = "encina-cdc-debezium";

    /// <summary>
    /// Gets or sets the list of Kafka topics to subscribe to.
    /// Debezium typically creates one topic per table, named <c>{prefix}.{schema}.{table}</c>.
    /// </summary>
    public string[] Topics { get; set; } = [];

    /// <summary>
    /// Gets or sets the auto offset reset behavior when no committed offset is found.
    /// Valid values: "earliest", "latest". Defaults to "earliest" to process all events.
    /// </summary>
    public string AutoOffsetReset { get; set; } = "earliest";

    /// <summary>
    /// Gets or sets the session timeout in milliseconds.
    /// The consumer will be considered dead if the broker doesn't receive a heartbeat
    /// within this period, triggering a rebalance.
    /// </summary>
    public int SessionTimeoutMs { get; set; } = 45000;

    /// <summary>
    /// Gets or sets the maximum poll interval in milliseconds.
    /// If the consumer doesn't call poll within this period, it's removed from the group.
    /// </summary>
    public int MaxPollIntervalMs { get; set; } = 300000;

    /// <summary>
    /// Gets or sets the expected event format from Debezium via Kafka.
    /// Kafka-based Debezium deployments typically use <see cref="DebeziumEventFormat.Flat"/>.
    /// </summary>
    public DebeziumEventFormat EventFormat { get; set; } = DebeziumEventFormat.Flat;

    /// <summary>
    /// Gets or sets the security protocol for connecting to Kafka brokers.
    /// Examples: "PLAINTEXT", "SSL", "SASL_PLAINTEXT", "SASL_SSL".
    /// </summary>
    public string? SecurityProtocol { get; set; }

    /// <summary>
    /// Gets or sets the SASL authentication mechanism.
    /// Examples: "PLAIN", "SCRAM-SHA-256", "SCRAM-SHA-512", "GSSAPI".
    /// </summary>
    public string? SaslMechanism { get; set; }

    /// <summary>
    /// Gets or sets the SASL username for authentication.
    /// </summary>
    public string? SaslUsername { get; set; }

    /// <summary>
    /// Gets or sets the SASL password for authentication.
    /// </summary>
    public string? SaslPassword { get; set; }

    /// <summary>
    /// Gets or sets the SSL CA certificate file location for secure connections.
    /// </summary>
    public string? SslCaLocation { get; set; }
}
