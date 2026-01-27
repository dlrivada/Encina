using Encina.InMemory;

namespace Encina.NBomber.Scenarios.Messaging;

/// <summary>
/// Factory for creating messaging-specific services for load testing scenarios.
/// Each provider implementation provides access to message bus and dispatcher capabilities.
/// </summary>
public interface IMessagingProviderFactory : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique provider name (e.g., "inmemory").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets the provider category.
    /// </summary>
    MessagingProviderCategory Category { get; }

    /// <summary>
    /// Initializes the messaging provider, creating any required infrastructure.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an in-memory message bus instance configured for load testing.
    /// </summary>
    /// <returns>The configured message bus, or null if not supported by this provider.</returns>
    IInMemoryMessageBus? CreateMessageBus();

    /// <summary>
    /// Creates a service provider configured for dispatcher testing.
    /// </summary>
    /// <returns>A service provider with Encina dispatchers configured.</returns>
    IServiceProvider CreateServiceProvider();

    /// <summary>
    /// Gets the configuration options for the provider.
    /// </summary>
    MessagingProviderOptions Options { get; }
}

/// <summary>
/// Provider category enumeration for messaging.
/// </summary>
public enum MessagingProviderCategory
{
    /// <summary>In-memory message bus implementation.</summary>
    InMemory,

    /// <summary>Request/Notification dispatcher testing.</summary>
    Dispatcher
}

/// <summary>
/// Configuration options for messaging providers.
/// </summary>
public sealed class MessagingProviderOptions
{
    /// <summary>
    /// Gets or sets the number of worker threads for message processing.
    /// </summary>
    public int WorkerCount { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Gets or sets the bounded channel capacity.
    /// </summary>
    public int BoundedCapacity { get; set; } = 10000;

    /// <summary>
    /// Gets or sets whether to use unbounded channels.
    /// </summary>
    public bool UseUnboundedChannel { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum degree of parallelism for dispatcher testing.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Gets or sets the number of handlers to register per message type.
    /// </summary>
    public int HandlersPerMessageType { get; set; } = 5;
}
