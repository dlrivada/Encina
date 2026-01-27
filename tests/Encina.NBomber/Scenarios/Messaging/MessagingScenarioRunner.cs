using Encina.NBomber.Scenarios.Messaging.Providers;
using NBomber.Contracts;

namespace Encina.NBomber.Scenarios.Messaging;

/// <summary>
/// Runner for messaging load test scenarios.
/// Creates and executes InMemoryBus and Dispatcher scenarios.
/// </summary>
public sealed class MessagingScenarioRunner : IAsyncDisposable
{
    private readonly MessagingFeature _feature;
    private InMemoryBusProviderFactory? _providerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingScenarioRunner"/> class.
    /// </summary>
    /// <param name="feature">The messaging feature to test.</param>
    public MessagingScenarioRunner(MessagingFeature feature)
    {
        _feature = feature;
    }

    /// <summary>
    /// Creates scenarios for the specified messaging feature.
    /// </summary>
    /// <returns>A collection of scenario props to run.</returns>
    public async Task<ScenarioProps[]> CreateScenariosAsync()
    {
        var scenarios = new List<ScenarioProps>();

        // Initialize provider factory for InMemoryBus scenarios
        _providerFactory = new InMemoryBusProviderFactory(options =>
        {
            options.WorkerCount = Environment.ProcessorCount;
            options.UseUnboundedChannel = true;
            options.BoundedCapacity = 10000;
            options.HandlersPerMessageType = 10;
            options.MaxDegreeOfParallelism = Environment.ProcessorCount;
        });

        await _providerFactory.InitializeAsync().ConfigureAwait(false);

        var context = new MessagingScenarioContext(_providerFactory, "inmemory");

        if (_feature is MessagingFeature.InMemoryBus or MessagingFeature.All)
        {
            var inMemoryFactory = new InMemoryBusScenarioFactory(context);
            scenarios.AddRange(inMemoryFactory.CreateScenarios());
        }

        if (_feature is MessagingFeature.Dispatcher or MessagingFeature.All)
        {
            var dispatcherFactory = new DispatcherScenarioFactory(context);
            scenarios.AddRange(dispatcherFactory.CreateScenarios());
        }

        return scenarios.ToArray();
    }

    /// <summary>
    /// Disposes resources used by the runner.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_providerFactory is not null)
        {
            await _providerFactory.DisposeAsync().ConfigureAwait(false);
        }
    }
}

/// <summary>
/// Feature categories for messaging load testing.
/// </summary>
public enum MessagingFeature
{
    /// <summary>InMemoryMessageBus - concurrent publish/subscribe.</summary>
    InMemoryBus,

    /// <summary>Dispatchers - parallel and sequential dispatch strategies.</summary>
    Dispatcher,

    /// <summary>All messaging features.</summary>
    All
}
