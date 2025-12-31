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
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldContain("operational");
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
        healthCheck.Name.ShouldBe("my-custom-quartz");
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDataWithSchedulerInfo()
    {
        // Arrange
        var healthCheck = new QuartzHealthCheck(_serviceProvider!, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Data.ShouldNotBeNull();
        result.Data.ShouldContainKey("scheduler_name");
        result.Data.ShouldContainKey("is_started");
        result.Data.ShouldContainKey("is_shutdown");
        result.Data.ShouldContainKey("is_standby");
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
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldContain("standby");
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        // Arrange
        var healthCheck = new QuartzHealthCheck(_serviceProvider!, null);

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("scheduling");
        healthCheck.Tags.ShouldContain("quartz");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaQuartz()
    {
        QuartzHealthCheck.DefaultName.ShouldBe("encina-quartz");
    }
}
