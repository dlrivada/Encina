namespace Encina.MQTT;

/// <summary>
/// Configuration options for Encina MQTT integration.
/// </summary>
public sealed class EncinaMQTTOptions
{
    /// <summary>
    /// Gets or sets the MQTT broker host.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the MQTT broker port.
    /// </summary>
    public int Port { get; set; } = 1883;

    /// <summary>
    /// Gets or sets the client ID.
    /// </summary>
    public string ClientId { get; set; } = $"simplemediator-{Guid.NewGuid():N}";

    /// <summary>
    /// Gets or sets the topic prefix for all messages.
    /// </summary>
    public string TopicPrefix { get; set; } = "simplemediator";

    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password for authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the default QoS level.
    /// </summary>
    public MqttQualityOfService QualityOfService { get; set; } = MqttQualityOfService.AtLeastOnce;

    /// <summary>
    /// Gets or sets a value indicating whether to use TLS.
    /// </summary>
    public bool UseTls { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use clean session.
    /// </summary>
    public bool CleanSession { get; set; } = true;

    /// <summary>
    /// Gets or sets the keep alive interval in seconds.
    /// </summary>
    public int KeepAliveSeconds { get; set; } = 60;
}

/// <summary>
/// MQTT Quality of Service levels.
/// </summary>
public enum MqttQualityOfService
{
    /// <summary>
    /// At most once delivery (fire and forget).
    /// </summary>
    AtMostOnce = 0,

    /// <summary>
    /// At least once delivery (acknowledged delivery).
    /// </summary>
    AtLeastOnce = 1,

    /// <summary>
    /// Exactly once delivery (assured delivery).
    /// </summary>
    ExactlyOnce = 2
}
