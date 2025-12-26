using Encina.Messaging.Health;
using Encina.Quartz.Health;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;

namespace Encina.Quartz.IntegrationTests.Health;

/// <summary>
/// Integration tests for QuartzHealthCheck using a real scheduler.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Scheduler", "Quartz")]
public sealed class QuartzHealthCheckIntegrationTests : IAsyncLifetime
{
    private IScheduler? _scheduler;
    private ServiceProvider? _serviceProvider;

    public async Task InitializeAsync()
    {
        // Create and start a real Quartz scheduler
        var factory = new StdSchedulerFactory();
        _scheduler = await factory.GetScheduler();
        await _scheduler.Start();

        var services = new ServiceCollection();
        services.AddSingleton<ISchedulerFactory>(factory);
        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        if (_scheduler != null)
        {
            await _scheduler.Shutdown(waitForJobsToComplete: false);
        }

        _serviceProvider?.Dispose();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSchedulerIsRunning_ReturnsHealthy()
    {
        // Arrange
        var healthCheck = new QuartzHealthCheck(_serviceProvider!, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("operational");
    }

    [Fact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "my-custom-quartz" };
        var healthCheck = new QuartzHealthCheck(_serviceProvider!, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.Should().Be("my-custom-quartz");
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDataWithSchedulerInfo()
    {
        // Arrange
        var healthCheck = new QuartzHealthCheck(_serviceProvider!, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.Should().ContainKey("scheduler_name");
        result.Data.Should().ContainKey("is_started");
        result.Data.Should().ContainKey("is_shutdown");
        result.Data.Should().ContainKey("is_standby");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSchedulerInStandby_ReturnsDegraded()
    {
        // Arrange
        await _scheduler!.Standby();
        var healthCheck = new QuartzHealthCheck(_serviceProvider!, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("standby");

        // Cleanup - restart scheduler for other tests
        await _scheduler.Start();
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        // Arrange
        var healthCheck = new QuartzHealthCheck(_serviceProvider!, null);

        // Assert
        healthCheck.Tags.Should().Contain("encina");
        healthCheck.Tags.Should().Contain("scheduling");
        healthCheck.Tags.Should().Contain("quartz");
        healthCheck.Tags.Should().Contain("ready");
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaQuartz()
    {
        QuartzHealthCheck.DefaultName.Should().Be("encina-quartz");
    }
}
