using Encina.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Encina.Tests.Modules;

public sealed class ModuleLifecycleHostedServiceTests
{
    private readonly ILogger<ModuleLifecycleHostedService> _logger = NullLogger<ModuleLifecycleHostedService>.Instance;

    [Fact]
    public async Task StartAsync_CallsOnStartAsync_ForAllLifecycleModules()
    {
        // Arrange
        var module1 = new TrackingLifecycleModule("Module1");
        var module2 = new TrackingLifecycleModule("Module2");
        var registry = new ModuleRegistry([module1, module2]);
        var service = new ModuleLifecycleHostedService(registry, _logger);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        module1.StartCalled.ShouldBeTrue();
        module2.StartCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task StartAsync_CallsModulesInRegistrationOrder()
    {
        // Arrange
        var callOrder = new List<string>();
        var module1 = new OrderTrackingModule("First", callOrder);
        var module2 = new OrderTrackingModule("Second", callOrder);
        var module3 = new OrderTrackingModule("Third", callOrder);
        var registry = new ModuleRegistry([module1, module2, module3]);
        var service = new ModuleLifecycleHostedService(registry, _logger);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        callOrder.ShouldBe(["First", "Second", "Third"]);
    }

    [Fact]
    public async Task StartAsync_WithNoLifecycleModules_DoesNothing()
    {
        // Arrange
        var regularModule = new RegularModule();
        var registry = new ModuleRegistry([regularModule]);
        var service = new ModuleLifecycleHostedService(registry, _logger);

        // Act
        var exception = await Record.ExceptionAsync(() => service.StartAsync(CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task StartAsync_WhenModuleFails_PropagatesException()
    {
        // Arrange
        var failingModule = new FailingStartModule();
        var registry = new ModuleRegistry([failingModule]);
        var service = new ModuleLifecycleHostedService(registry, _logger);

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => service.StartAsync(CancellationToken.None));
        ex.Message.ShouldBe("Start failed");
    }

    [Fact]
    public async Task StartAsync_WhenCancelled_PropagatesOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var module = new CancellationAwareModule(cts);
        var registry = new ModuleRegistry([module]);
        var service = new ModuleLifecycleHostedService(registry, _logger);

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => service.StartAsync(cts.Token));
    }

    [Fact]
    public async Task StopAsync_CallsOnStopAsync_ForAllLifecycleModules()
    {
        // Arrange
        var module1 = new TrackingLifecycleModule("Module1");
        var module2 = new TrackingLifecycleModule("Module2");
        var registry = new ModuleRegistry([module1, module2]);
        var service = new ModuleLifecycleHostedService(registry, _logger);

        // Act
        await service.StopAsync(CancellationToken.None);

        // Assert
        module1.StopCalled.ShouldBeTrue();
        module2.StopCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task StopAsync_CallsModulesInReverseOrder()
    {
        // Arrange
        var callOrder = new List<string>();
        var module1 = new OrderTrackingModule("First", callOrder, trackStop: true);
        var module2 = new OrderTrackingModule("Second", callOrder, trackStop: true);
        var module3 = new OrderTrackingModule("Third", callOrder, trackStop: true);
        var registry = new ModuleRegistry([module1, module2, module3]);
        var service = new ModuleLifecycleHostedService(registry, _logger);

        // Act
        await service.StopAsync(CancellationToken.None);

        // Assert
        callOrder.ShouldBe(["Third", "Second", "First"]);
    }

    [Fact]
    public async Task StopAsync_ContinuesStoppingOtherModules_WhenOneFails()
    {
        // Arrange
        var callOrder = new List<string>();
        var module1 = new OrderTrackingModule("First", callOrder, trackStop: true);
        var failingModule = new FailingStopModule("Failing");
        var module3 = new OrderTrackingModule("Third", callOrder, trackStop: true);
        var registry = new ModuleRegistry([module1, failingModule, module3]);
        var service = new ModuleLifecycleHostedService(registry, _logger);

        // Act & Assert
        var ex = await Should.ThrowAsync<AggregateException>(
            () => service.StopAsync(CancellationToken.None));

        // All modules should have been attempted to stop (reverse order: Third, Failing, First)
        callOrder.ShouldContain("Third");
        callOrder.ShouldContain("First");
        ex.InnerExceptions.Count.ShouldBe(1);
    }

    [Fact]
    public async Task StopAsync_WithNoLifecycleModules_DoesNothing()
    {
        // Arrange
        var regularModule = new RegularModule();
        var registry = new ModuleRegistry([regularModule]);
        var service = new ModuleLifecycleHostedService(registry, _logger);

        // Act
        var exception = await Record.ExceptionAsync(() => service.StopAsync(CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task StopAsync_CollectsAllExceptions_WhenMultipleFail()
    {
        // Arrange
        var failing1 = new FailingStopModule("Failing1");
        var failing2 = new FailingStopModule("Failing2");
        var registry = new ModuleRegistry([failing1, failing2]);
        var service = new ModuleLifecycleHostedService(registry, _logger);

        // Act
        var ex = await Should.ThrowAsync<AggregateException>(
            () => service.StopAsync(CancellationToken.None));

        // Assert
        ex.InnerExceptions.Count.ShouldBe(2);
    }

    #region Test Fixtures

    private sealed class RegularModule : IModule
    {
        public string Name => "Regular";
        public void ConfigureServices(IServiceCollection services) { }
    }

    private sealed class TrackingLifecycleModule(string name) : IModuleLifecycle
    {
        public string Name { get; } = name;
        public bool StartCalled { get; private set; }
        public bool StopCalled { get; private set; }

        public void ConfigureServices(IServiceCollection services) { }

        public Task OnStartAsync(CancellationToken cancellationToken)
        {
            StartCalled = true;
            return Task.CompletedTask;
        }

        public Task OnStopAsync(CancellationToken cancellationToken)
        {
            StopCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class OrderTrackingModule(string name, List<string> callOrder, bool trackStop = false) : IModuleLifecycle
    {
        public string Name { get; } = name;

        public void ConfigureServices(IServiceCollection services) { }

        public Task OnStartAsync(CancellationToken cancellationToken)
        {
            if (!trackStop)
            {
                callOrder.Add(Name);
            }
            return Task.CompletedTask;
        }

        public Task OnStopAsync(CancellationToken cancellationToken)
        {
            if (trackStop)
            {
                callOrder.Add(Name);
            }
            return Task.CompletedTask;
        }
    }

    private sealed class FailingStartModule : IModuleLifecycle
    {
        public string Name => "FailingStart";
        public void ConfigureServices(IServiceCollection services) { }

        public Task OnStartAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Start failed");
        }

        public Task OnStopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FailingStopModule(string name) : IModuleLifecycle
    {
        public string Name { get; } = name;
        public void ConfigureServices(IServiceCollection services) { }
        public Task OnStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task OnStopAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException($"Stop failed: {Name}");
        }
    }

    private sealed class CancellationAwareModule(CancellationTokenSource cts) : IModuleLifecycle
    {
        public string Name => "CancellationAware";
        public void ConfigureServices(IServiceCollection services) { }

        public Task OnStartAsync(CancellationToken cancellationToken)
        {
            cts.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task OnStopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    #endregion
}
