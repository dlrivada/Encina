using Encina.Modules;
using Microsoft.Extensions.Logging;

namespace Encina.UnitTests.Core.Modules;

/// <summary>
/// Unit tests for <see cref="ModuleLifecycleHostedService"/>.
/// </summary>
public sealed class ModuleLifecycleHostedServiceTests
{
    private readonly IModuleRegistry _registry;
    private readonly ILogger<ModuleLifecycleHostedService> _logger;

    public ModuleLifecycleHostedServiceTests()
    {
        _registry = Substitute.For<IModuleRegistry>();
        _logger = Substitute.For<ILogger<ModuleLifecycleHostedService>>();
    }

    [Fact]
    public async Task StartAsync_WithNoLifecycleModules_CompletesImmediately()
    {
        // Arrange
        _registry.GetLifecycleModules().Returns(new List<IModuleLifecycle>());
        var service = new ModuleLifecycleHostedService(_registry, _logger);

        // Act & Assert - should complete without error
        await service.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_CallsOnStartForAllModules()
    {
        // Arrange
        var module1 = CreateLifecycleModule("mod-1");
        var module2 = CreateLifecycleModule("mod-2");
        _registry.GetLifecycleModules().Returns(new List<IModuleLifecycle> { module1, module2 });
        var service = new ModuleLifecycleHostedService(_registry, _logger);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        await module1.Received(1).OnStartAsync(Arg.Any<CancellationToken>());
        await module2.Received(1).OnStartAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_WhenModuleThrows_PropagatesException()
    {
        // Arrange
        var module = CreateLifecycleModule("failing-mod");
        module.OnStartAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Start failed")));
        _registry.GetLifecycleModules().Returns(new List<IModuleLifecycle> { module });
        var service = new ModuleLifecycleHostedService(_registry, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StopAsync_WithNoLifecycleModules_CompletesImmediately()
    {
        // Arrange
        _registry.GetLifecycleModules().Returns(new List<IModuleLifecycle>());
        var service = new ModuleLifecycleHostedService(_registry, _logger);

        // Act & Assert
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_CallsOnStopInReverseOrder()
    {
        // Arrange
        var callOrder = new List<string>();
        var module1 = CreateLifecycleModule("mod-1");
        module1.OnStopAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => { callOrder.Add("mod-1"); return Task.CompletedTask; });
        var module2 = CreateLifecycleModule("mod-2");
        module2.OnStopAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => { callOrder.Add("mod-2"); return Task.CompletedTask; });

        _registry.GetLifecycleModules().Returns(new List<IModuleLifecycle> { module1, module2 });
        var service = new ModuleLifecycleHostedService(_registry, _logger);

        // Act
        await service.StopAsync(CancellationToken.None);

        // Assert - reverse order (LIFO)
        callOrder.Count.ShouldBe(2);
        callOrder[0].ShouldBe("mod-2");
        callOrder[1].ShouldBe("mod-1");
    }

    [Fact]
    public async Task StopAsync_WhenModuleThrows_ContinuesStoppingOthers()
    {
        // Arrange
        var module1 = CreateLifecycleModule("mod-1");
        var module2 = CreateLifecycleModule("failing-mod");
        module2.OnStopAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Stop failed")));

        _registry.GetLifecycleModules().Returns(new List<IModuleLifecycle> { module1, module2 });
        var service = new ModuleLifecycleHostedService(_registry, _logger);

        // Act & Assert - should throw AggregateException
        var ex = await Assert.ThrowsAsync<AggregateException>(() =>
            service.StopAsync(CancellationToken.None));
        ex.InnerExceptions.Count.ShouldBe(1);

        // module1 should still have been stopped (since mod2 stops first in LIFO)
        await module1.Received(1).OnStopAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Constructor_WithNullRegistry_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ModuleLifecycleHostedService(null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ModuleLifecycleHostedService(_registry, null!));
    }

    private static IModuleLifecycle CreateLifecycleModule(string name)
    {
        var module = Substitute.For<IModuleLifecycle>();
        module.Name.Returns(name);
        module.OnStartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        module.OnStopAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        return module;
    }
}
