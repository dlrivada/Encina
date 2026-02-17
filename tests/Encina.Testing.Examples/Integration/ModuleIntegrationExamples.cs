using Encina.Testing.Examples.Domain;
using Encina.Testing.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Testing.Examples.Integration;

/// <summary>
/// Examples demonstrating ModuleTestFixture for integration tests.
/// Reference: docs/plans/testing-dogfooding-plan.md Section 5.4, 9.2
/// </summary>
public sealed class ModuleIntegrationExamples : IAsyncLifetime
{
    private ModuleTestFixture<TestModule>? _fixture;

    public async ValueTask InitializeAsync()
    {
        _fixture = new ModuleTestFixture<TestModule>()
            .ConfigureServices(services =>
            {
                // Register module-specific services
                services.AddTransient<CreateOrderHandler>();
            });

        await _fixture.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_fixture is not null)
        {
            await _fixture.DisposeAsync();
        }
    }

    /// <summary>
    /// Pattern: Test handler through module.
    /// </summary>
    [Fact]
    public async Task ModuleHandler_ShouldExecute()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = "CUST-001",
            Amount = 100m
        };

        // Act
        var result = await _fixture!.SendAsync(command);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    /// <summary>
    /// Pattern: Access module services.
    /// </summary>
    [Fact]
    public void ModuleServices_ShouldBeResolvable()
    {
        // Act
        var handler = _fixture!.GetService<CreateOrderHandler>();

        // Assert
        handler.ShouldNotBeNull();
    }

    /// <summary>
    /// Pattern: Test module isolation.
    /// </summary>
    [Fact]
    public void ModuleInfo_ShouldProvideMetadata()
    {
        // Act
        var moduleInfo = _fixture!.ModuleInfo;

        // Assert
        moduleInfo.ShouldNotBeNull();
        moduleInfo.Name.ShouldNotBeNullOrWhiteSpace();
    }
}

/// <summary>
/// Sample module for testing.
/// </summary>
public sealed class TestModule : IModule
{
    public string Name => "TestModule";
    public string Version => "1.0.0";

    public IServiceCollection ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<CreateOrderHandler>();
        return services;
    }
}

/// <summary>
/// Module interface for testing.
/// </summary>
public interface IModule
{
    string Name { get; }
    string Version { get; }
    IServiceCollection ConfigureServices(IServiceCollection services);
}

/// <summary>
/// Simplified ModuleTestFixture for examples.
/// </summary>
/// <typeparam name="TModule">The module type.</typeparam>
public sealed class ModuleTestFixture<TModule> : IAsyncDisposable
    where TModule : IModule, new()
{
    private readonly ServiceCollection _services = new();
    private ServiceProvider? _provider;
    private readonly TModule _module = new();
    private readonly List<Action<IServiceCollection>> _configureActions = [];

    public ModuleInfo ModuleInfo => new(_module.Name, _module.Version);

    public ModuleTestFixture<TModule> ConfigureServices(Action<IServiceCollection> configure)
    {
        _configureActions.Add(configure);
        return this;
    }

    public Task InitializeAsync()
    {
        _module.ConfigureServices(_services);

        foreach (var action in _configureActions)
        {
            action(_services);
        }

        _services.AddEncina(config =>
        {
            config.RegisterServicesFromAssemblyContaining<TModule>();
        });

        _provider = _services.BuildServiceProvider();
        return Task.CompletedTask;
    }

    public async Task<Either<EncinaError, TResponse>> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        var encina = _provider!.GetRequiredService<IEncina>();
        return await encina.Send(request);
    }

    public TService? GetService<TService>() where TService : class
    {
        return _provider?.GetService<TService>();
    }

    public async ValueTask DisposeAsync()
    {
        if (_provider is not null)
        {
            await _provider.DisposeAsync();
        }
    }
}

/// <summary>
/// Module metadata.
/// </summary>
public sealed record ModuleInfo(string Name, string Version);
