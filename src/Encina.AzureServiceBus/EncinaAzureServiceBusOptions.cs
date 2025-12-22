namespace Encina.AzureServiceBus;

/// <summary>
/// Configuration options for Encina Azure Service Bus integration.
/// </summary>
public sealed class EncinaAzureServiceBusOptions
{
    /// <summary>
    /// Gets or sets the Azure Service Bus connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default queue name for commands.
    /// </summary>
    public string DefaultQueueName { get; set; } = "encina-commands";

    /// <summary>
    /// Gets or sets the default topic name for events.
    /// </summary>
    public string DefaultTopicName { get; set; } = "encina-events";

    /// <summary>
    /// Gets or sets the subscription name for this instance.
    /// </summary>
    public string SubscriptionName { get; set; } = "default";

    /// <summary>
    /// Gets or sets a value indicating whether to use sessions.
    /// </summary>
    public bool UseSessions { get; set; }

    /// <summary>
    /// Gets or sets the maximum concurrent calls for the processor.
    /// </summary>
    public int MaxConcurrentCalls { get; set; } = 10;

    /// <summary>
    /// Gets or sets the prefetch count.
    /// </summary>
    public int PrefetchCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum auto-lock renewal duration.
    /// </summary>
    public TimeSpan MaxAutoLockRenewalDuration { get; set; } = TimeSpan.FromMinutes(5);
}
