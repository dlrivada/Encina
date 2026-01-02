using System.Reflection;
using Encina.Modules;
using Encina.Testing.Fakes;
using Encina.Testing.Fakes.Factories;
using Encina.Testing.Fakes.Stores;
using Encina.Testing.Time;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Testing.Modules;

internal interface IModuleTestFixtureContext
{
    FakeOutboxStore? Outbox { get; }
    IntegrationEventCollector IntegrationEvents { get; }
}

/// <summary>
/// A fluent test fixture for testing modules in isolation.
/// </summary>
/// <typeparam name="TModule">The module type to test.</typeparam>
/// <remarks>
/// <para>
/// This fixture provides isolated module testing with:
/// <list type="bullet">
/// <item><description>Automatic module registration and handler discovery</description></item>
/// <item><description>Mock/fake dependent modules</description></item>
/// <item><description>Integration event capture and assertions</description></item>
/// <item><description>Fake messaging stores (Outbox, Inbox, Saga, etc.)</description></item>
/// <item><description>Time control via <see cref="FakeTimeProvider"/></description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Fact]
/// public async Task PlaceOrder_ValidOrder_Succeeds()
/// {
///     // Arrange
///     var fixture = new ModuleTestFixture&lt;OrdersModule&gt;()
///         .WithMockedModule&lt;IInventoryModuleApi&gt;(mock =&gt;
///             mock.ReserveStock = _ =&gt; Task.FromResult(Right&lt;EncinaError, ReservationId&gt;(...)))
///         .WithMockedOutbox();
///
///     // Act
///     var result = await fixture.SendAsync(new PlaceOrderCommand(...));
///
///     // Assert
///     result.ShouldSucceed();
///     fixture.IntegrationEvents.ShouldContain&lt;OrderPlacedEvent&gt;();
/// }
/// </code>
/// </example>
public sealed class ModuleTestFixture<TModule> : IDisposable, IAsyncDisposable, IModuleTestFixtureContext
    where TModule : IModule, new()
{
    private readonly ServiceCollection _services = new();
    private readonly List<Action<EncinaConfiguration>> _configureActions = new List<Action<EncinaConfiguration>>();
    private readonly List<Action<IServiceCollection>> _serviceConfigurations = new List<Action<IServiceCollection>>();
    private readonly TModule _module;
    private readonly IntegrationEventCollector _integrationEvents = new();
    private ServiceProvider? _serviceProvider;
    private IEncina? _encina;
    private bool _disposed;

    private FakeOutboxStore? _outboxStore;
    private FakeInboxStore? _inboxStore;
    private FakeSagaStore? _sagaStore;
    private FakeScheduledMessageStore? _scheduledMessageStore;
    private FakeDeadLetterStore? _deadLetterStore;
    private FakeTimeProvider? _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleTestFixture{TModule}"/> class.
    /// </summary>
    public ModuleTestFixture()
    {
        _module = new TModule();
    }

    /// <summary>
    /// Gets the module instance being tested.
    /// </summary>
    public TModule Module => _module;

    /// <summary>
    /// Gets the collected integration events for assertion.
    /// </summary>
    public IntegrationEventCollector IntegrationEvents => _integrationEvents;

    /// <summary>
    /// Gets the fake outbox store if <see cref="WithMockedOutbox"/> was called.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithMockedOutbox"/> was not called.
    /// </exception>
    public FakeOutboxStore Outbox =>
        _outboxStore ?? throw new InvalidOperationException(
            "Outbox store not configured. Call WithMockedOutbox() first.");

    /// <summary>
    /// Gets the fake inbox store if <see cref="WithMockedInbox"/> was called.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithMockedInbox"/> was not called.
    /// </exception>
    public FakeInboxStore Inbox =>
        _inboxStore ?? throw new InvalidOperationException(
            "Inbox store not configured. Call WithMockedInbox() first.");

    /// <summary>
    /// Gets the fake saga store if <see cref="WithMockedSaga"/> was called.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithMockedSaga"/> was not called.
    /// </exception>
    public FakeSagaStore SagaStore =>
        _sagaStore ?? throw new InvalidOperationException(
            "Saga store not configured. Call WithMockedSaga() first.");

    /// <summary>
    /// Gets the fake scheduled message store if <see cref="WithMockedScheduling"/> was called.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithMockedScheduling"/> was not called.
    /// </exception>
    public FakeScheduledMessageStore ScheduledMessageStore =>
        _scheduledMessageStore ?? throw new InvalidOperationException(
            "Scheduled message store not configured. Call WithMockedScheduling() first.");

    /// <summary>
    /// Gets the fake dead letter store if <see cref="WithMockedDeadLetter"/> was called.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithMockedDeadLetter"/> was not called.
    /// </exception>
    public FakeDeadLetterStore DeadLetterStore =>
        _deadLetterStore ?? throw new InvalidOperationException(
            "Dead letter store not configured. Call WithMockedDeadLetter() first.");

    /// <summary>
    /// Gets the fake time provider if <see cref="WithFakeTimeProvider()"/> was called.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithFakeTimeProvider()"/> was not called.
    /// </exception>
    public FakeTimeProvider TimeProvider =>
        _timeProvider ?? throw new InvalidOperationException(
            "Time provider not configured. Call WithFakeTimeProvider() first.");

    /// <summary>
    /// Gets the service provider after the fixture has been built.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the fixture has not been built yet.
    /// </exception>
    public IServiceProvider ServiceProvider =>
        _serviceProvider ?? throw new InvalidOperationException(
            "Fixture not built. Call SendAsync, PublishAsync, or Build() first.");

    #region Module Mocking

    /// <summary>
    /// Registers a mocked module API with a configuration action.
    /// </summary>
    /// <typeparam name="TModuleApi">The module API interface type.</typeparam>
    /// <param name="configure">An action to configure the mock.</param>
    /// <returns>This fixture for method chaining.</returns>
    /// <remarks>
    /// Use this method to mock dependent module APIs when testing a module in isolation.
    /// </remarks>
    /// <example>
    /// <code>
    /// fixture.WithMockedModule&lt;IInventoryModuleApi&gt;(mock =&gt;
    /// {
    ///     mock.ReserveStockAsync = request =&gt;
    ///         Task.FromResult(Right&lt;EncinaError, ReservationId&gt;(new ReservationId("res-123")));
    /// });
    /// </code>
    /// </example>
    public ModuleTestFixture<TModule> WithMockedModule<TModuleApi>(Action<MockModuleApi<TModuleApi>> configure)
        where TModuleApi : class
    {
        ArgumentNullException.ThrowIfNull(configure);

        var mock = new MockModuleApi<TModuleApi>();
        configure(mock);

        _services.AddSingleton(mock.Build());
        return this;
    }

    /// <summary>
    /// Registers a mocked module API instance directly.
    /// </summary>
    /// <typeparam name="TModuleApi">The module API interface type.</typeparam>
    /// <param name="implementation">The mock implementation.</param>
    /// <returns>This fixture for method chaining.</returns>
    public ModuleTestFixture<TModule> WithMockedModule<TModuleApi>(TModuleApi implementation)
        where TModuleApi : class
    {
        ArgumentNullException.ThrowIfNull(implementation);
        _services.AddSingleton(implementation);
        return this;
    }

    /// <summary>
    /// Registers a fake module implementation.
    /// </summary>
    /// <typeparam name="TModuleApi">The module API interface type.</typeparam>
    /// <typeparam name="TFakeModule">The fake implementation type.</typeparam>
    /// <returns>This fixture for method chaining.</returns>
    /// <remarks>
    /// Use this when you have a complete fake implementation of a module's API.
    /// </remarks>
    /// <example>
    /// <code>
    /// fixture.WithFakeModule&lt;IIdentityModuleApi, FakeIdentityModule&gt;();
    /// </code>
    /// </example>
    public ModuleTestFixture<TModule> WithFakeModule<TModuleApi, TFakeModule>()
        where TModuleApi : class
        where TFakeModule : class, TModuleApi, new()
    {
        _services.AddSingleton<TModuleApi, TFakeModule>();
        return this;
    }

    /// <summary>
    /// Registers a fake module instance.
    /// </summary>
    /// <typeparam name="TModuleApi">The module API interface type.</typeparam>
    /// <typeparam name="TFakeModule">The fake implementation type.</typeparam>
    /// <param name="instance">The fake instance.</param>
    /// <returns>This fixture for method chaining.</returns>
    public ModuleTestFixture<TModule> WithFakeModule<TModuleApi, TFakeModule>(TFakeModule instance)
        where TModuleApi : class
        where TFakeModule : class, TModuleApi
    {
        ArgumentNullException.ThrowIfNull(instance);
        _services.AddSingleton<TModuleApi>(instance);
        return this;
    }

    #endregion

    #region Service Registration

    /// <summary>
    /// Registers a service with a specific implementation instance.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <param name="implementation">The implementation instance.</param>
    /// <returns>This fixture for method chaining.</returns>
    public ModuleTestFixture<TModule> WithService<TService>(TService implementation)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(implementation);
        _services.AddSingleton(implementation);
        return this;
    }

    /// <summary>
    /// Registers a service with a specific implementation type.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>This fixture for method chaining.</returns>
    public ModuleTestFixture<TModule> WithService<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _services.AddTransient<TService, TImplementation>();
        return this;
    }

    /// <summary>
    /// Configures additional services.
    /// </summary>
    /// <param name="configureServices">The service configuration action.</param>
    /// <returns>This fixture for method chaining.</returns>
    public ModuleTestFixture<TModule> ConfigureServices(Action<IServiceCollection> configureServices)
    {
        ArgumentNullException.ThrowIfNull(configureServices);
        _serviceConfigurations.Add(configureServices);
        return this;
    }

    #endregion

    #region Messaging Stores

    /// <summary>
    /// Registers the fake outbox store for testing outbox message publishing.
    /// </summary>
    /// <returns>This fixture for method chaining.</returns>
    public ModuleTestFixture<TModule> WithMockedOutbox()
    {
        _outboxStore = new FakeOutboxStore();
        // Register the outbox under its interface to avoid exposing the concrete type
        // as a production service. Provide a factory mapping so resolving FakeOutboxStore
        // returns the same instance when the concrete type is requested (test-only convenience).
        _services.AddSingleton<IOutboxStore>(_outboxStore);
        _services.AddSingleton<FakeOutboxStore>(sp => (FakeOutboxStore)sp.GetRequiredService<IOutboxStore>());
        _services.AddSingleton<IOutboxMessageFactory, FakeOutboxMessageFactory>();
        return this;
    }

    /// <summary>
    /// Registers the fake inbox store for testing inbox message processing.
    /// </summary>
    /// <returns>This fixture for method chaining.</returns>
    public ModuleTestFixture<TModule> WithMockedInbox()
    {
        _inboxStore = new FakeInboxStore();
        _services.AddSingleton(_inboxStore);
        _services.AddSingleton<IInboxStore>(_inboxStore);
        return this;
    }

    /// <summary>
    /// Registers the fake saga store for testing saga orchestration.
    /// </summary>
    /// <returns>This fixture for method chaining.</returns>
    public ModuleTestFixture<TModule> WithMockedSaga()
    {
        _sagaStore = new FakeSagaStore();
        _services.AddSingleton(_sagaStore);
        _services.AddSingleton<ISagaStore>(_sagaStore);
        return this;
    }

    /// <summary>
    /// Registers the fake scheduled message store for testing message scheduling.
    /// </summary>
    /// <returns>This fixture for method chaining.</returns>
    public ModuleTestFixture<TModule> WithMockedScheduling()
    {
        _scheduledMessageStore = new FakeScheduledMessageStore();
        _services.AddSingleton(_scheduledMessageStore);
        _services.AddSingleton<IScheduledMessageStore>(_scheduledMessageStore);
        return this;
    }

    /// <summary>
    /// Registers the fake dead letter store for testing dead letter handling.
    /// </summary>
    /// <returns>This fixture for method chaining.</returns>
    public ModuleTestFixture<TModule> WithMockedDeadLetter()
    {
        _deadLetterStore = new FakeDeadLetterStore();
        _services.AddSingleton(_deadLetterStore);
        _services.AddSingleton<IDeadLetterStore>(_deadLetterStore);
        return this;
    }

    /// <summary>
    /// Registers all fake messaging stores.
    /// </summary>
    /// <returns>This fixture for method chaining.</returns>
    public ModuleTestFixture<TModule> WithAllMockedStores()
    {
        return WithMockedOutbox()
            .WithMockedInbox()
            .WithMockedSaga()
            .WithMockedScheduling()
            .WithMockedDeadLetter();
    }

    #endregion

    #region Time Control

    /// <summary>
    /// Registers a fake time provider for time-travel testing.
    /// </summary>
    /// <returns>This fixture for method chaining.</returns>
    public ModuleTestFixture<TModule> WithFakeTimeProvider()
    {
        return WithFakeTimeProvider(DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Registers a fake time provider starting at the specified time.
    /// </summary>
    /// <param name="startTime">The initial time.</param>
    /// <returns>This fixture for method chaining.</returns>
    public ModuleTestFixture<TModule> WithFakeTimeProvider(DateTimeOffset startTime)
    {
        _timeProvider = new FakeTimeProvider(startTime);
        _services.AddSingleton(_timeProvider);
        _services.AddSingleton<TimeProvider>(_timeProvider);
        return this;
    }

    /// <summary>
    /// Advances the fake time provider by the specified duration.
    /// </summary>
    /// <param name="duration">The duration to advance time.</param>
    /// <returns>This fixture for method chaining.</returns>
    public ModuleTestFixture<TModule> AdvanceTimeBy(TimeSpan duration)
    {
        TimeProvider.Advance(duration);
        return this;
    }

    #endregion

    #region Configuration

    /// <summary>
    /// Configures Encina with a custom action.
    /// </summary>
    /// <param name="configure">The configuration action.</param>
    /// <returns>This fixture for method chaining.</returns>
    public ModuleTestFixture<TModule> Configure(Action<EncinaConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configureActions.Add(configure);
        return this;
    }

    #endregion

    #region Execution

    /// <summary>
    /// Sends a request and returns a test context for fluent assertions.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A test context wrapping the result for fluent assertions.</returns>
    public async Task<ModuleTestContext<TResponse>> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureBuilt();

        var result = await _encina!.Send(request, cancellationToken);
        return new ModuleTestContext<TResponse>(result, this);
    }

    /// <summary>
    /// Publishes a notification and returns a test context for fluent assertions.
    /// </summary>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A test context wrapping the result for fluent assertions.</returns>
    public async Task<ModuleTestContext<Unit>> PublishAsync(
        INotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        EnsureBuilt();

        var result = await _encina!.Publish(notification, cancellationToken);

        // Only record the integration event after a successful publish
        if (result.IsRight)
        {
            _integrationEvents.Add(notification);
        }

        return new ModuleTestContext<Unit>(result, this);
    }

    /// <summary>
    /// Gets a required service from the service provider.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service instance.</returns>
    public T GetRequiredService<T>() where T : notnull
    {
        EnsureBuilt();
        return _serviceProvider!.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets a service from the service provider.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service instance or null if not registered.</returns>
    public T? GetService<T>() where T : class
    {
        EnsureBuilt();
        return _serviceProvider!.GetService<T>();
    }

    /// <summary>
    /// Builds the fixture if not already built.
    /// </summary>
    public void Build()
    {
        EnsureBuilt();
    }

    #endregion

    #region State Management

    /// <summary>
    /// Clears all fake stores and captured integration events.
    /// </summary>
    public void ClearStores()
    {
        _outboxStore?.Clear();
        _inboxStore?.Clear();
        _sagaStore?.Clear();
        _scheduledMessageStore?.Clear();
        _deadLetterStore?.Clear();
        _integrationEvents.Clear();
    }

    #endregion

    private void EnsureBuilt()
    {
        if (_serviceProvider is not null)
        {
            return;
        }

        // Register the module's services first
        _module.ConfigureServices(_services);

        // Apply additional service configurations
        foreach (var configure in _serviceConfigurations)
        {
            configure(_services);
        }

        // Register integration event collector
        _services.AddSingleton(_integrationEvents);

        // Configure Encina with the module's assembly
        _services.AddEncina(config =>
        {
            config.RegisterServicesFromAssembly(_module.GetType().Assembly);

            foreach (var action in _configureActions)
            {
                action(config);
            }
        });

        _serviceProvider = _services.BuildServiceProvider();
        _encina = _serviceProvider.GetRequiredService<IEncina>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _serviceProvider?.Dispose();
        _serviceProvider = null;
        _encina = null;
        _disposed = true;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
            _serviceProvider = null;
        }

        _encina = null;
        _disposed = true;
    }
}
