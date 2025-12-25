using Encina.Modules;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests.Modules;

public sealed class ModuleServiceCollectionExtensionsTests
{
    private static void AddNullLogging(IServiceCollection services)
    {
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    [Fact]
    public void AddEncinaModules_RegistersModuleRegistry()
    {
        // Arrange
        var services = new ServiceCollection();
        AddNullLogging(services);

        // Act
        services.AddEncinaModules(config => config.AddModule<TestModule>());

        // Assert
        using var provider = services.BuildServiceProvider();
        var registry = provider.GetService<IModuleRegistry>();
        registry.ShouldNotBeNull();
        registry.Modules.Count.ShouldBe(1);
    }

    [Fact]
    public void AddEncinaModules_RegistersIndividualModulesForInjection()
    {
        // Arrange
        var services = new ServiceCollection();
        AddNullLogging(services);

        // Act
        services.AddEncinaModules(config => config.AddModule<TestModule>());

        // Assert
        using var provider = services.BuildServiceProvider();
        var module = provider.GetService<TestModule>();
        module.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaModules_CallsConfigureServicesOnEachModule()
    {
        // Arrange
        var services = new ServiceCollection();
        AddNullLogging(services);
        var module = new ServiceRegisteringModule();

        // Act
        services.AddEncinaModules(config => config.AddModule(module));

        // Assert
        using var provider = services.BuildServiceProvider();
        var registeredService = provider.GetService<ITestService>();
        registeredService.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaModules_WithLifecycleModules_RegistersHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        AddNullLogging(services);

        // Act
        services.AddEncinaModules(config => config.AddModule<TestLifecycleModule>());

        // Assert
        services.ShouldContain(d => d.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public void AddEncinaModules_WithoutLifecycleModules_DoesNotRegisterHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        AddNullLogging(services);

        // Act
        services.AddEncinaModules(config =>
        {
            config.AddModule<TestModule>();
            config.WithoutHandlerDiscovery(); // Prevent Encina registration
        });

        // Assert
        services.ShouldNotContain(d => d.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public void AddEncinaModules_WithHandlerDiscovery_RegistersEncina()
    {
        // Arrange
        var services = new ServiceCollection();
        AddNullLogging(services);

        // Act
        services.AddEncinaModules(config => config.AddModule<ModuleWithHandler>());

        // Assert
        using var provider = services.BuildServiceProvider();
        var encina = provider.GetService<IEncina>();
        encina.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaModules_WithoutHandlerDiscovery_DoesNotRegisterEncina()
    {
        // Arrange
        var services = new ServiceCollection();
        AddNullLogging(services);

        // Act
        services.AddEncinaModules(config =>
        {
            config.AddModule<TestModule>();
            config.WithoutHandlerDiscovery();
        });

        // Assert
        using var provider = services.BuildServiceProvider();
        var encina = provider.GetService<IEncina>();
        encina.ShouldBeNull();
    }

    [Fact]
    public void AddEncinaModules_ThrowsWhenServicesIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ModuleServiceCollectionExtensions.AddEncinaModules(null!, config => { }));
    }

    [Fact]
    public void AddEncinaModules_ThrowsWhenConfigureIsNull()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaModules((Action<ModuleConfiguration>)null!));
    }

    [Fact]
    public void AddEncinaModules_WithConfiguration_ThrowsWhenServicesIsNull()
    {
        // Arrange
        var config = new ModuleConfiguration();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ModuleServiceCollectionExtensions.AddEncinaModules(null!, config));
    }

    [Fact]
    public void AddEncinaModules_WithConfiguration_ThrowsWhenConfigurationIsNull()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaModules((ModuleConfiguration)null!));
    }

    [Fact]
    public void AddEncinaModules_MultipleModules_AllRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        AddNullLogging(services);

        // Act
        services.AddEncinaModules(config =>
        {
            config.AddModule<TestModule>();
            config.AddModule<TestLifecycleModule>();
            config.AddModule<ServiceRegisteringModule>();
            config.WithoutHandlerDiscovery();
        });

        // Assert
        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IModuleRegistry>();
        registry.Modules.Count.ShouldBe(3);
    }

    [Fact]
    public void AddEncinaModules_ModuleRegistryIsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        AddNullLogging(services);
        services.AddEncinaModules(config =>
        {
            config.AddModule<TestModule>();
            config.WithoutHandlerDiscovery();
        });

        // Act
        using var provider = services.BuildServiceProvider();
        var registry1 = provider.GetRequiredService<IModuleRegistry>();
        var registry2 = provider.GetRequiredService<IModuleRegistry>();

        // Assert
        registry1.ShouldBeSameAs(registry2);
    }

    #region Test Fixtures

    private sealed class TestModule : IModule
    {
        public string Name => "Test";
        public void ConfigureServices(IServiceCollection services) { }
    }

    private sealed class TestLifecycleModule : IModuleLifecycle
    {
        public string Name => "TestLifecycle";
        public void ConfigureServices(IServiceCollection services) { }
        public Task OnStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task OnStopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private interface ITestService { }
    private sealed class TestService : ITestService { }

    private sealed class ServiceRegisteringModule : IModule
    {
        public string Name => "ServiceRegistering";
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ITestService, TestService>();
        }
    }

    // Module with a handler to test handler discovery
    private sealed class ModuleWithHandler : IModule
    {
        public string Name => "ModuleWithHandler";
        public void ConfigureServices(IServiceCollection services) { }
    }

    internal sealed record TestCommand(string Value) : ICommand<string>;

    internal sealed class TestCommandHandler : ICommandHandler<TestCommand, string>
    {
        public Task<Either<EncinaError, string>> Handle(TestCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, string>(request.Value));
    }

    #endregion
}
