using Encina.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.NBomber.Scenarios.Messaging.Providers;

/// <summary>
/// Provider factory for InMemoryMessageBus load testing.
/// Configures the message bus with appropriate settings for high-throughput testing.
/// </summary>
public sealed class InMemoryBusProviderFactory : MessagingProviderFactoryBase
{
    private ServiceProvider? _serviceProvider;
    private IInMemoryMessageBus? _messageBus;

    /// <inheritdoc />
    public override string ProviderName => "inmemory";

    /// <inheritdoc />
    public override MessagingProviderCategory Category => MessagingProviderCategory.InMemory;

    /// <summary>
    /// Initializes a new instance with default options.
    /// </summary>
    public InMemoryBusProviderFactory()
    {
    }

    /// <summary>
    /// Initializes a new instance with custom options.
    /// </summary>
    /// <param name="configure">Configuration action for provider options.</param>
    public InMemoryBusProviderFactory(Action<MessagingProviderOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Options);
    }

    /// <inheritdoc />
    protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();

        // Add logging (required by InMemoryMessageBus)
        services.AddLogging();

        // Configure InMemoryMessageBus for load testing
        services.AddEncinaInMemory(options =>
        {
            options.WorkerCount = Options.WorkerCount;
            options.BoundedCapacity = Options.BoundedCapacity;
            options.UseUnboundedChannel = Options.UseUnboundedChannel;
            options.AllowSynchronousContinuations = true; // Better for load testing
        });

        _serviceProvider = services.BuildServiceProvider();
        _messageBus = _serviceProvider.GetRequiredService<IInMemoryMessageBus>();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override IInMemoryMessageBus? CreateMessageBus()
    {
        EnsureInitialized();
        return _messageBus;
    }

    /// <inheritdoc />
    public override IServiceProvider CreateServiceProvider()
    {
        EnsureInitialized();
        return _serviceProvider!;
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeCoreAsync()
    {
        // Only dispose the service provider - it will handle disposing the message bus
        // as a registered service. Don't dispose _messageBus directly to avoid double-dispose.
        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync().ConfigureAwait(false);
            _serviceProvider = null;
            _messageBus = null;
        }
    }
}
