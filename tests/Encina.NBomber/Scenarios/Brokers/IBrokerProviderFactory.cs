namespace Encina.NBomber.Scenarios.Brokers;

/// <summary>
/// Factory interface for creating message broker providers for load testing.
/// Provides lifecycle management and configuration for broker providers.
/// </summary>
public interface IBrokerProviderFactory : IAsyncDisposable
{
    /// <summary>
    /// Gets the name of the broker provider (e.g., "rabbitmq", "kafka", "nats", "mqtt").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets the category of the broker provider.
    /// </summary>
    BrokerProviderCategory Category { get; }

    /// <summary>
    /// Gets the broker provider options.
    /// </summary>
    BrokerProviderOptions Options { get; }

    /// <summary>
    /// Gets a value indicating whether the provider is available and ready.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Initializes the broker provider and any required infrastructure (e.g., containers).
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Categories of broker providers for load testing.
/// </summary>
public enum BrokerProviderCategory
{
    /// <summary>RabbitMQ AMQP broker.</summary>
    RabbitMQ,

    /// <summary>Apache Kafka streaming platform.</summary>
    Kafka,

    /// <summary>NATS messaging system.</summary>
    NATS,

    /// <summary>MQTT lightweight messaging protocol.</summary>
    MQTT
}

/// <summary>
/// Configuration options for broker provider factories.
/// </summary>
public sealed class BrokerProviderOptions
{
    /// <summary>
    /// Gets or sets the topic/queue prefix for test resources.
    /// </summary>
    public string TopicPrefix { get; set; } = "nbomber";

    /// <summary>
    /// Gets or sets the default message size in bytes.
    /// </summary>
    public int MessageSizeBytes { get; set; } = 1024;

    /// <summary>
    /// Gets or sets the batch size for batch operations.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the number of partitions for Kafka topics.
    /// </summary>
    public int PartitionCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the number of concurrent consumers.
    /// </summary>
    public int ConsumerCount { get; set; } = 5;

    /// <summary>
    /// Gets or sets the consumer group ID.
    /// </summary>
    public string ConsumerGroupId { get; set; } = "nbomber-consumer-group";

    /// <summary>
    /// Gets or sets the RabbitMQ connection string (for external instances).
    /// </summary>
    public string? RabbitMQConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the RabbitMQ container image.
    /// </summary>
    public string RabbitMQImage { get; set; } = "rabbitmq:3-management-alpine";

    /// <summary>
    /// Gets or sets the Kafka bootstrap servers (for external instances).
    /// </summary>
    public string? KafkaBootstrapServers { get; set; }

    /// <summary>
    /// Gets or sets the Kafka container image.
    /// </summary>
    public string KafkaImage { get; set; } = "confluentinc/cp-kafka:7.6.0";

    /// <summary>
    /// Gets or sets the NATS connection string (for external instances).
    /// </summary>
    public string? NATSConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the NATS container image.
    /// </summary>
    public string NATSImage { get; set; } = "nats:2-alpine";

    /// <summary>
    /// Gets or sets the MQTT broker host (for external instances).
    /// </summary>
    public string? MQTTHost { get; set; }

    /// <summary>
    /// Gets or sets the MQTT broker port (for external instances).
    /// </summary>
    public int? MQTTPort { get; set; }

    /// <summary>
    /// Gets or sets the MQTT container image.
    /// </summary>
    public string MQTTImage { get; set; } = "eclipse-mosquitto:2";

    /// <summary>
    /// Gets or sets whether to enable publisher confirms for RabbitMQ.
    /// </summary>
    public bool EnablePublisherConfirms { get; set; }

    /// <summary>
    /// Gets or sets the MQTT QoS level (0, 1, or 2).
    /// </summary>
    public int MQTTQoS { get; set; }
}
