namespace Encina.NServiceBus;

/// <summary>
/// Configuration options for Encina NServiceBus integration.
/// </summary>
public sealed class EncinaNServiceBusOptions
{
    /// <summary>
    /// Gets or sets the endpoint name for NServiceBus.
    /// </summary>
    public string EndpointName { get; set; } = "Encina.Endpoint";

    /// <summary>
    /// Gets or sets a value indicating whether to use NServiceBus outbox.
    /// </summary>
    public bool UseOutbox { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include exception details
    /// in error responses.
    /// </summary>
    public bool IncludeExceptionDetails { get; set; }

    /// <summary>
    /// Gets or sets the default timeout for message processing.
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the number of immediate retries before delayed retries.
    /// </summary>
    public int ImmediateRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the number of delayed retries.
    /// </summary>
    public int DelayedRetries { get; set; } = 2;
}
