namespace SimpleMediator.NATS;

/// <summary>
/// Configuration options for SimpleMediator NATS integration.
/// </summary>
public sealed class SimpleMediatorNATSOptions
{
    /// <summary>
    /// Gets or sets the NATS server URL.
    /// </summary>
    public string Url { get; set; } = "nats://localhost:4222";

    /// <summary>
    /// Gets or sets the subject prefix for all messages.
    /// </summary>
    public string SubjectPrefix { get; set; } = "simplemediator";

    /// <summary>
    /// Gets or sets a value indicating whether to use JetStream.
    /// </summary>
    public bool UseJetStream { get; set; }

    /// <summary>
    /// Gets or sets the stream name for JetStream.
    /// </summary>
    public string StreamName { get; set; } = "SIMPLEMEDIATOR";

    /// <summary>
    /// Gets or sets the consumer name for JetStream.
    /// </summary>
    public string ConsumerName { get; set; } = "simplemediator-consumer";

    /// <summary>
    /// Gets or sets a value indicating whether to use durable consumers.
    /// </summary>
    public bool UseDurableConsumer { get; set; } = true;

    /// <summary>
    /// Gets or sets the ack wait timeout.
    /// </summary>
    public TimeSpan AckWait { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the maximum deliver attempts.
    /// </summary>
    public int MaxDeliver { get; set; } = 5;
}
