namespace Encina.Kafka;

/// <summary>
/// Configuration options for Encina Kafka integration.
/// </summary>
public sealed class EncinaKafkaOptions
{
    /// <summary>
    /// Gets or sets the Kafka bootstrap servers.
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Gets or sets the consumer group ID.
    /// </summary>
    public string GroupId { get; set; } = "simplemediator-consumer";

    /// <summary>
    /// Gets or sets the default topic for commands.
    /// </summary>
    public string DefaultCommandTopic { get; set; } = "simplemediator-commands";

    /// <summary>
    /// Gets or sets the default topic for events.
    /// </summary>
    public string DefaultEventTopic { get; set; } = "simplemediator-events";

    /// <summary>
    /// Gets or sets the auto offset reset behavior.
    /// </summary>
    public string AutoOffsetReset { get; set; } = "earliest";

    /// <summary>
    /// Gets or sets a value indicating whether to enable auto commit.
    /// </summary>
    public bool EnableAutoCommit { get; set; }

    /// <summary>
    /// Gets or sets the acks configuration for producer.
    /// </summary>
    public string Acks { get; set; } = "all";

    /// <summary>
    /// Gets or sets a value indicating whether to enable idempotence.
    /// </summary>
    public bool EnableIdempotence { get; set; } = true;

    /// <summary>
    /// Gets or sets the message timeout in milliseconds.
    /// </summary>
    public int MessageTimeoutMs { get; set; } = 30000;
}
