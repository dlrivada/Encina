using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Testing;

/// <summary>
/// Test fixture for setting up Encina in xUnit tests.
/// Provides a fluent API for configuring handlers, behaviors, and services.
/// </summary>
/// <remarks>
/// <para>
/// Use this fixture with xUnit's <c>IClassFixture&lt;EncinaFixture&gt;</c> to share
/// configuration across tests, or create instances directly for test-specific setup.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderTests : IClassFixture&lt;EncinaFixture&gt;
/// {
///     private readonly EncinaFixture _fixture;
///
///     public OrderTests(EncinaFixture fixture)
///     {
///         _fixture = fixture;
///     }
///
///     [Fact]
///     public async Task CreateOrder_ShouldSucceed()
///     {
///         var encina = _fixture.CreateEncina(config =&gt;
///         {
///             config.RegisterServicesFromAssemblyContaining&lt;CreateOrderHandler&gt;();
///         });
///
///         var result = await encina.Send(new CreateOrder(Guid.NewGuid(), items));
///
///         result.ShouldBeSuccess();
///     }
/// }
/// </code>
/// </example>
public class EncinaFixture : IDisposable
{
    private ServiceProvider? _serviceProvider;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of <see cref="IEncina"/> with the specified configuration.
    /// </summary>
    /// <param name="configure">Action to configure handlers, behaviors, and services.</param>
    /// <returns>A configured <see cref="IEncina"/> instance ready for testing.</returns>
    /// <example>
    /// <code>
    /// var encina = fixture.CreateEncina(config =&gt;
    /// {
    ///     config.RegisterServicesFromAssemblyContaining&lt;MyHandler&gt;();
    ///     config.AddPipelineBehavior&lt;ValidationBehavior&lt;,&gt;&gt;();
    /// });
    /// </code>
    /// </example>
    public IEncina CreateEncina(Action<EncinaConfiguration>? configure = null)
    {
        return CreateEncina(configure, configureServices: null);
    }

    /// <summary>
    /// Creates a new instance of <see cref="IEncina"/> with the specified configuration
    /// and additional service registrations.
    /// </summary>
    /// <param name="configure">Action to configure handlers, behaviors, and services.</param>
    /// <param name="configureServices">Action to register additional services in the DI container.</param>
    /// <returns>A configured <see cref="IEncina"/> instance ready for testing.</returns>
    /// <example>
    /// <code>
    /// var encina = fixture.CreateEncina(
    ///     config =&gt;
    ///     {
    ///         config.RegisterServicesFromAssemblyContaining&lt;MyHandler&gt;();
    ///     },
    ///     services =&gt;
    ///     {
    ///         services.AddSingleton&lt;IMyService, MockMyService&gt;();
    ///     });
    /// </code>
    /// </example>
    public IEncina CreateEncina(
        Action<EncinaConfiguration>? configure,
        Action<IServiceCollection>? configureServices)
    {
        var services = new ServiceCollection();

        configureServices?.Invoke(services);

        services.AddEncina(configure);

        _serviceProvider?.Dispose();
        _serviceProvider = services.BuildServiceProvider();

        return _serviceProvider.GetRequiredService<IEncina>();
    }

    /// <summary>
    /// Creates a new instance of <see cref="IEncina"/> that scans the specified assemblies.
    /// </summary>
    /// <param name="assemblies">Assemblies to scan for handlers and behaviors.</param>
    /// <returns>A configured <see cref="IEncina"/> instance ready for testing.</returns>
    public IEncina CreateEncina(params Assembly[] assemblies)
    {
        return CreateEncina(config =>
        {
            config.RegisterServicesFromAssemblies(assemblies);
        });
    }

    /// <summary>
    /// Creates a new instance of <see cref="IEncina"/> that scans the assembly containing the specified type.
    /// </summary>
    /// <typeparam name="T">Type whose assembly will be scanned.</typeparam>
    /// <returns>A configured <see cref="IEncina"/> instance ready for testing.</returns>
    public IEncina CreateEncinaFromAssemblyContaining<T>()
    {
        return CreateEncina(config =>
        {
            config.RegisterServicesFromAssemblyContaining<T>();
        });
    }

    /// <summary>
    /// Gets the service provider used by the most recently created <see cref="IEncina"/> instance.
    /// </summary>
    /// <remarks>
    /// This is useful for resolving services registered in the container for test verification.
    /// Returns <c>null</c> if no <see cref="IEncina"/> instance has been created yet.
    /// </remarks>
    public IServiceProvider? ServiceProvider => _serviceProvider;

    /// <summary>
    /// Gets a required service from the current service provider.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve.</typeparam>
    /// <returns>The requested service instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <see cref="IEncina"/> instance has been created yet.
    /// </exception>
    public T GetRequiredService<T>() where T : notnull
    {
        if (_serviceProvider is null)
        {
            throw new InvalidOperationException(
                "No IEncina instance has been created. Call CreateEncina first.");
        }

        return _serviceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets a service from the current service provider.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve.</typeparam>
    /// <returns>The requested service instance, or <c>null</c> if not registered.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <see cref="IEncina"/> instance has been created yet.
    /// </exception>
    public T? GetService<T>() where T : class
    {
        if (_serviceProvider is null)
        {
            throw new InvalidOperationException(
                "No IEncina instance has been created. Call CreateEncina first.");
        }

        return _serviceProvider.GetService<T>();
    }

    /// <summary>
    /// Creates a new scope for resolving scoped services.
    /// </summary>
    /// <returns>A new service scope.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <see cref="IEncina"/> instance has been created yet.
    /// </exception>
    public IServiceScope CreateScope()
    {
        if (_serviceProvider is null)
        {
            throw new InvalidOperationException(
                "No IEncina instance has been created. Call CreateEncina first.");
        }

        return _serviceProvider.CreateScope();
    }

    /// <summary>
    /// Disposes the service provider and releases resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed resources.
    /// </summary>
    /// <param name="disposing">True if called from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _serviceProvider?.Dispose();
                _serviceProvider = null;
            }

            _disposed = true;
        }
    }
}
