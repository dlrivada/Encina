namespace Encina.Redis.PubSub;

/// <summary>
/// Configuration options for Encina Redis Pub/Sub integration.
/// </summary>
public sealed class EncinaRedisPubSubOptions
{
    /// <summary>
    /// Gets or sets the Redis connection string.
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Gets or sets the channel prefix for all messages.
    /// </summary>
    public string ChannelPrefix { get; set; } = "simplemediator";

    /// <summary>
    /// Gets or sets the channel name for commands.
    /// </summary>
    public string CommandChannel { get; set; } = "commands";

    /// <summary>
    /// Gets or sets the channel name for events.
    /// </summary>
    public string EventChannel { get; set; } = "events";

    /// <summary>
    /// Gets or sets a value indicating whether to use pattern-based subscriptions.
    /// </summary>
    public bool UsePatternSubscription { get; set; }

    /// <summary>
    /// Gets or sets the connect timeout in milliseconds.
    /// </summary>
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the sync timeout in milliseconds.
    /// </summary>
    public int SyncTimeout { get; set; } = 5000;
}
