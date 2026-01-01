using System.Collections.Generic;
using System.Reflection;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Testing.Fakes;
using Encina.Testing.Fakes.Factories;
using Encina.Testing.Fakes.Stores;
using Encina.Testing.Time;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using AssemblySet = System.Collections.Generic.HashSet<System.Reflection.Assembly>;

namespace Encina.Testing;

/// <summary>
/// Fluent test fixture for setting up Encina integration tests.
/// </summary>
/// <remarks>
/// <para>
/// This fixture provides a fluent API for configuring handlers, validators,
/// services, and fake stores. It composes the existing <see cref="EncinaFixture"/>
/// while providing a more ergonomic builder pattern.
/// </para>
/// <para>
/// The fixture supports:
/// <list type="bullet">
/// <item><description>Fluent handler and validator registration</description></item>
/// <item><description>Mock service injection</description></item>
/// <item><description>Fake messaging stores (Outbox, Inbox, Saga, etc.)</description></item>
/// <item><description>Chainable assertions via <see cref="EncinaTestContext{TResponse}"/></description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Fact]
/// public async Task CreateOrder_WithValidData_ShouldSucceed()
/// {
///     // Arrange
///     var fixture = new EncinaTestFixture()
///         .WithHandler&lt;CreateOrderHandler&gt;()
///         .WithValidator&lt;CreateOrderValidator&gt;()
///         .WithMockedOutbox()
///         .WithService&lt;IInventoryService&gt;(new MockInventoryService());
///
///     // Act &amp; Assert
///     var context = await fixture.SendAsync(new CreateOrderCommand("cust-1", items));
///     context.ShouldSucceed();
///     fixture.Outbox.WasMessageAdded&lt;OrderCreatedEvent&gt;().ShouldBeTrue();
/// }
/// </code>
/// </example>
public sealed class EncinaTestFixture : IDisposable, IAsyncDisposable
{
    private readonly ServiceCollection _services = new();
    private readonly List<Action<EncinaConfiguration>> _configureActions = [];
    private readonly AssemblySet _registeredAssemblies = [];
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
    /// Thrown when the fixture has not been built yet (no Send/Publish called).
    /// </exception>
    public IServiceProvider ServiceProvider =>
        _serviceProvider ?? throw new InvalidOperationException(
            "Fixture not built. Call SendAsync or PublishAsync first.");

    /// <summary>
    /// Registers a handler type for the test.
    /// </summary>
    /// <typeparam name="THandler">The handler type to register.</typeparam>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaTestFixture WithHandler<THandler>() where THandler : class
    {
        _services.AddTransient<THandler>();

        var assembly = typeof(THandler).Assembly;
        if (_registeredAssemblies.Add(assembly))
        {
            _configureActions.Add(config => config.RegisterServicesFromAssemblyContaining<THandler>());
        }

        return this;
    }

    /// <summary>
    /// Registers a validator type for the test.
    /// </summary>
    /// <typeparam name="TValidator">The validator type to register.</typeparam>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaTestFixture WithValidator<TValidator>() where TValidator : class
    {
        _services.AddTransient<TValidator>();
        return this;
    }

    /// <summary>
    /// Registers a service with a specific implementation instance.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <param name="implementation">The implementation instance.</param>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaTestFixture WithService<TService>(TService implementation) where TService : class
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
    public EncinaTestFixture WithService<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _services.AddTransient<TService, TImplementation>();
        return this;
    }

    /// <summary>
    /// Registers a scoped service with a specific implementation type.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaTestFixture WithScopedService<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _services.AddScoped<TService, TImplementation>();
        return this;
    }

    /// <summary>
    /// Registers a singleton service with a specific implementation type.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaTestFixture WithSingletonService<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _services.AddSingleton<TService, TImplementation>();
        return this;
    }

    /// <summary>
    /// Registers the fake outbox store for testing outbox message publishing.
    /// </summary>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaTestFixture WithMockedOutbox()
    {
        _outboxStore = new FakeOutboxStore();
        _services.AddSingleton(_outboxStore);
        _services.AddSingleton<IOutboxStore>(_outboxStore);
        _services.AddSingleton<IOutboxMessageFactory, FakeOutboxMessageFactory>();
        return this;
    }

    /// <summary>
    /// Registers the fake inbox store for testing inbox message processing.
    /// </summary>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaTestFixture WithMockedInbox()
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
    public EncinaTestFixture WithMockedSaga()
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
    public EncinaTestFixture WithMockedScheduling()
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
    public EncinaTestFixture WithMockedDeadLetter()
    {
        _deadLetterStore = new FakeDeadLetterStore();
        _services.AddSingleton(_deadLetterStore);
        _services.AddSingleton<IDeadLetterStore>(_deadLetterStore);
        return this;
    }

    /// <summary>
    /// Registers all fake messaging stores (Outbox, Inbox, Saga, Scheduling, DeadLetter).
    /// </summary>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaTestFixture WithAllMockedStores()
    {
        return WithMockedOutbox()
            .WithMockedInbox()
            .WithMockedSaga()
            .WithMockedScheduling()
            .WithMockedDeadLetter();
    }

    /// <summary>
    /// Registers a fake time provider for time-travel testing.
    /// </summary>
    /// <returns>This fixture for method chaining.</returns>
    /// <remarks>
    /// The time provider starts at the current UTC time. Use <see cref="WithFakeTimeProvider(DateTimeOffset)"/>
    /// to specify a custom start time.
    /// </remarks>
    public EncinaTestFixture WithFakeTimeProvider()
    {
        return WithFakeTimeProvider(DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Registers a fake time provider starting at the specified time.
    /// </summary>
    /// <param name="startTime">The initial time for the fake time provider.</param>
    /// <returns>This fixture for method chaining.</returns>
    /// <remarks>
    /// Use <see cref="AdvanceTimeBy"/> to advance time during tests.
    /// </remarks>
    public EncinaTestFixture WithFakeTimeProvider(DateTimeOffset startTime)
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
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithFakeTimeProvider()"/> was not called.
    /// </exception>
    /// <remarks>
    /// This is useful for testing time-dependent behavior such as:
    /// <list type="bullet">
    /// <item><description>Saga timeouts and expiration</description></item>
    /// <item><description>Scheduled message execution</description></item>
    /// <item><description>Cache expiration</description></item>
    /// <item><description>Retry delays</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var fixture = new EncinaTestFixture()
    ///     .WithFakeTimeProvider(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero))
    ///     .WithMockedSaga()
    ///     .WithHandler&lt;OrderSagaHandler&gt;();
    ///
    /// // Start the saga
    /// await fixture.SendAsync(new StartOrderCommand(...));
    ///
    /// // Advance time to trigger timeout
    /// fixture.AdvanceTimeBy(TimeSpan.FromHours(2));
    ///
    /// // Verify saga timed out
    /// fixture.SagaStore.GetSagas().First().Status.ShouldBe("TimedOut");
    /// </code>
    /// </example>
    public EncinaTestFixture AdvanceTimeBy(TimeSpan duration)
    {
        TimeProvider.Advance(duration);
        return this;
    }

    /// <summary>
    /// Advances the fake time provider by the specified number of minutes.
    /// </summary>
    /// <param name="minutes">The number of minutes to advance.</param>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaTestFixture AdvanceTimeByMinutes(int minutes)
    {
        TimeProvider.AdvanceMinutes(minutes);
        return this;
    }

    /// <summary>
    /// Advances the fake time provider by the specified number of hours.
    /// </summary>
    /// <param name="hours">The number of hours to advance.</param>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaTestFixture AdvanceTimeByHours(int hours)
    {
        TimeProvider.Advance(TimeSpan.FromHours(hours));
        return this;
    }

    /// <summary>
    /// Advances the fake time provider by the specified number of days.
    /// </summary>
    /// <param name="days">The number of days to advance.</param>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaTestFixture AdvanceTimeByDays(int days)
    {
        TimeProvider.Advance(TimeSpan.FromDays(days));
        return this;
    }

    /// <summary>
    /// Sets the fake time provider to a specific time.
    /// </summary>
    /// <param name="time">The time to set.</param>
    /// <returns>This fixture for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when attempting to move time backwards.</exception>
    public EncinaTestFixture SetTimeTo(DateTimeOffset time)
    {
        TimeProvider.SetUtcNow(time);
        return this;
    }

    /// <summary>
    /// Gets the current time from the fake time provider.
    /// </summary>
    /// <returns>The current fake time.</returns>
    public DateTimeOffset GetCurrentTime()
    {
        return TimeProvider.GetUtcNow();
    }

    /// <summary>
    /// Configures Encina with a custom action.
    /// </summary>
    /// <param name="configure">The configuration action.</param>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaTestFixture Configure(Action<EncinaConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configureActions.Add(configure);
        return this;
    }

    /// <summary>
    /// Configures additional services.
    /// </summary>
    /// <param name="configureServices">The service configuration action.</param>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaTestFixture ConfigureServices(Action<IServiceCollection> configureServices)
    {
        ArgumentNullException.ThrowIfNull(configureServices);
        configureServices(_services);
        return this;
    }

    /// <summary>
    /// Sends a request and returns a test context for fluent assertions.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A test context wrapping the result for fluent assertions.</returns>
    public async Task<EncinaTestContext<TResponse>> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureBuilt();

        var result = await _encina!.Send(request, cancellationToken);
        return new EncinaTestContext<TResponse>(result, this);
    }

    /// <summary>
    /// Publishes a notification and returns a test context for fluent assertions.
    /// </summary>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A test context wrapping the result for fluent assertions.</returns>
    public async Task<EncinaTestContext<Unit>> PublishAsync(
        INotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        EnsureBuilt();

        var result = await _encina!.Publish(notification, cancellationToken);
        return new EncinaTestContext<Unit>(result, this);
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

    private void EnsureBuilt()
    {
        if (_serviceProvider is not null)
        {
            return;
        }

        _services.AddEncina(config =>
        {
            foreach (var action in _configureActions)
            {
                action(config);
            }
        });

        _serviceProvider = _services.BuildServiceProvider();
        _encina = _serviceProvider.GetRequiredService<IEncina>();
    }

    /// <summary>
    /// Clears all fake stores to reset state between test cases.
    /// </summary>
    public void ClearStores()
    {
        _outboxStore?.Clear();
        _inboxStore?.Clear();
        _sagaStore?.Clear();
        _scheduledMessageStore?.Clear();
        _deadLetterStore?.Clear();
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
