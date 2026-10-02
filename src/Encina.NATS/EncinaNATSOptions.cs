using Encina.Messaging.Health;

namespace Encina.NATS;

/// <summary>
/// Configuration options for Encina NATS integration.
/// </summary>
public sealed class EncinaNATSOptions
{
    /// <summary>
    /// Gets or sets the NATS server URL, or a comma-separated list of server URLs.
    /// </summary>
    /// <remarks>
    /// Each URL must be absolute with the <c>nats</c>, <c>tls</c>, <c>ws</c> or <c>wss</c> scheme and
    /// must not target a link-local, cloud metadata or unspecified address. The default targets
    /// <c>localhost</c>, so it is rejected at registration unless <see cref="AllowLocalEndpoints"/> is set.
    /// </remarks>
    public string Url { get; set; } = "nats://localhost:4222";

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Url"/> may target <c>localhost</c> or a
    /// loopback address.
    /// </summary>
    /// <value>
    /// Defaults to <c>false</c>. Set to <c>true</c> for local development or a sidecar on the same
    /// host; a warning is logged at startup when it is set.
    /// </value>
    public bool AllowLocalEndpoints { get; set; }

    /// <summary>
    /// Gets or sets the subject prefix for all messages.
    /// </summary>
    public string SubjectPrefix { get; set; } = "encina";

    /// <summary>
    /// Gets or sets a value indicating whether to use JetStream.
    /// </summary>
    public bool UseJetStream { get; set; }

    /// <summary>
    /// Gets or sets the stream name for JetStream.
    /// </summary>
    public string StreamName { get; set; } = "ENCINA";

    /// <summary>
    /// Gets or sets the consumer name for JetStream.
    /// </summary>
    public string ConsumerName { get; set; } = "encina-consumer";

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

    /// <summary>
    /// Gets the provider health check options.
    /// </summary>
    public ProviderHealthCheckOptions ProviderHealthCheck { get; } = new();
}
