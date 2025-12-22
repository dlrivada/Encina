namespace Encina.MassTransit;

/// <summary>
/// Configuration options for Encina MassTransit integration.
/// </summary>
public sealed class EncinaMassTransitOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to automatically register consumers for all IRequest types.
    /// Default is true.
    /// </summary>
    public bool AutoRegisterRequestConsumers { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to automatically register consumers for all INotification types.
    /// Default is true.
    /// </summary>
    public bool AutoRegisterNotificationConsumers { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to throw exceptions on mediator errors.
    /// When false, errors are logged but not thrown, allowing message to be acknowledged.
    /// Default is true.
    /// </summary>
    public bool ThrowOnMediatorError { get; set; } = true;

    /// <summary>
    /// Gets or sets the prefix for queue names generated from message types.
    /// Default is "simplemediator".
    /// </summary>
    public string QueueNamePrefix { get; set; } = "simplemediator";
}
