using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Interfaces;

namespace Encina.Testing.TUnit;

/// <summary>
/// TUnit-compatible test fixture for Encina with async lifecycle support.
/// Provides NativeAOT-compatible testing through TUnit's source-generated test discovery.
/// </summary>
/// <remarks>
/// <para>
/// This fixture implements TUnit's <see cref="IAsyncInitializer"/> and <see cref="IAsyncDisposable"/>
/// interfaces for proper async lifecycle management in tests. Unlike xUnit's IClassFixture,
/// TUnit fixtures support fully async initialization and cleanup.
/// </para>
/// <para>
/// The fixture is designed for NativeAOT compatibility by avoiding reflection-based patterns.
/// TUnit's source generator discovers and registers tests at compile time, enabling
/// 10-200x faster test execution compared to traditional reflection-based frameworks.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderHandlerTests
/// {
///     private readonly EncinaTUnitFixture _fixture = new();
///
///     [Before(Test)]
///     public async Task Setup() => await _fixture.InitializeAsync();
///
///     [After(Test)]
///     public async Task Cleanup() => await _fixture.DisposeAsync();
///
///     [Test]
///     public async Task CreateOrder_ShouldSucceed()
///     {
///         var result = await _fixture.Encina.Send(new CreateOrderCommand { CustomerId = "123" });
///
///         await result.ShouldBeSuccessAsync();
///     }
/// }
/// </code>
/// </example>
public class EncinaTUnitFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly object _initLock = new();
    private ServiceProvider? _serviceProvider;
    private IEncina? _encina;
    private readonly ServiceCollection _services = new();
    private readonly List<Action<EncinaConfiguration>> _configureActions = [];
    private bool _disposed;

    /// <summary>
    /// Gets the configured <see cref="IEncina"/> instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="InitializeAsync"/> has not been called.
    /// </exception>
    public IEncina Encina => _encina
        ?? throw new InvalidOperationException("Fixture not initialized. Call InitializeAsync() first.");

    /// <summary>
    /// Gets the service provider for resolving additional services.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="InitializeAsync"/> has not been called.
    /// </exception>
    public IServiceProvider ServiceProvider => _serviceProvider
        ?? throw new InvalidOperationException("Fixture not initialized. Call InitializeAsync() first.");

    /// <summary>
    /// Initializes the fixture asynchronously.
    /// Called automatically by TUnit before tests run.
    /// </summary>
    /// <returns>A task representing the initialization operation.</returns>
    public Task InitializeAsync()
    {
        if (_serviceProvider is not null)
        {
            return Task.CompletedTask;
        }

        lock (_initLock)
        {
            if (_serviceProvider is not null)
            {
                return Task.CompletedTask;
            }

            ConfigureServices(_services);

            _services.AddEncina(config =>
            {
                ConfigureEncina(config);
                foreach (var action in _configureActions)
                {
                    action(config);
                }
            });

            _serviceProvider = _services.BuildServiceProvider();
            _encina = _serviceProvider.GetRequiredService<IEncina>();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Override this method to configure additional services in the DI container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <remarks>
    /// This method is called during <see cref="InitializeAsync"/> before building the service provider.
    /// Register any mock services, repositories, or other dependencies here.
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void ConfigureServices(IServiceCollection services)
    /// {
    ///     services.AddSingleton&lt;IInventoryService&gt;(new MockInventoryService());
    ///     services.AddScoped&lt;IOrderRepository, InMemoryOrderRepository&gt;();
    /// }
    /// </code>
    /// </example>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Override in derived classes to add services
    }

    /// <summary>
    /// Override this method to configure Encina handlers, behaviors, and assemblies.
    /// </summary>
    /// <param name="config">The Encina configuration to customize.</param>
    /// <remarks>
    /// This method is called during <see cref="InitializeAsync"/> when setting up Encina.
    /// Register handlers, validators, and behaviors here.
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void ConfigureEncina(EncinaConfiguration config)
    /// {
    ///     config.RegisterServicesFromAssemblyContaining&lt;CreateOrderHandler&gt;();
    ///     config.AddPipelineBehavior&lt;ValidationBehavior&lt;,&gt;&gt;();
    /// }
    /// </code>
    /// </example>
    protected virtual void ConfigureEncina(EncinaConfiguration config)
    {
        // Override in derived classes to configure Encina
    }

    /// <summary>
    /// Adds a configuration action to be applied during initialization.
    /// Must be called before <see cref="InitializeAsync"/>.
    /// </summary>
    /// <param name="configure">The configuration action.</param>
    /// <returns>This fixture for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the fixture has already been initialized.
    /// </exception>
    public EncinaTUnitFixture WithConfiguration(Action<EncinaConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        if (_serviceProvider is not null)
        {
            throw new InvalidOperationException("Cannot configure after InitializeAsync has been called.");
        }

        _configureActions.Add(configure);
        return this;
    }

    /// <summary>
    /// Adds a service registration action to be applied during initialization.
    /// Must be called before <see cref="InitializeAsync"/>.
    /// </summary>
    /// <param name="configureServices">The service registration action.</param>
    /// <returns>This fixture for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the fixture has already been initialized.
    /// </exception>
    public EncinaTUnitFixture WithServices(Action<IServiceCollection> configureServices)
    {
        ArgumentNullException.ThrowIfNull(configureServices);

        if (_serviceProvider is not null)
        {
            throw new InvalidOperationException("Cannot configure services after InitializeAsync has been called.");
        }

        configureServices(_services);
        return this;
    }

    /// <summary>
    /// Registers handlers from the assembly containing the specified type.
    /// Must be called before <see cref="InitializeAsync"/>.
    /// </summary>
    /// <typeparam name="T">A type in the assembly to scan.</typeparam>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaTUnitFixture WithHandlersFromAssemblyContaining<T>()
    {
        return WithConfiguration(config =>
            config.RegisterServicesFromAssemblyContaining<T>());
    }

    /// <summary>
    /// Registers handlers from the specified assemblies.
    /// Must be called before <see cref="InitializeAsync"/>.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for handlers.</param>
    /// <returns>This fixture for method chaining.</returns>
    public EncinaTUnitFixture WithHandlersFromAssemblies(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        return WithConfiguration(config =>
            config.RegisterServicesFromAssemblies(assemblies));
    }

    /// <summary>
    /// Gets a required service from the service provider.
    /// </summary>
    /// <typeparam name="T">The service type to retrieve.</typeparam>
    /// <returns>The requested service instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="InitializeAsync"/> has not been called or the service is not registered.
    /// </exception>
    public T GetRequiredService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets a service from the service provider, or null if not registered.
    /// </summary>
    /// <typeparam name="T">The service type to retrieve.</typeparam>
    /// <returns>The requested service instance, or null if not registered.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="InitializeAsync"/> has not been called.
    /// </exception>
    public T? GetService<T>() where T : class
    {
        return ServiceProvider.GetService<T>();
    }

    /// <summary>
    /// Creates a new service scope for scoped service resolution.
    /// </summary>
    /// <returns>A new service scope.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="InitializeAsync"/> has not been called.
    /// </exception>
    public IServiceScope CreateScope()
    {
        return ServiceProvider.CreateScope();
    }

    /// <summary>
    /// Disposes the fixture asynchronously.
    /// Called automatically by TUnit after tests complete.
    /// </summary>
    /// <returns>A task representing the disposal operation.</returns>
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

        GC.SuppressFinalize(this);
    }
}
