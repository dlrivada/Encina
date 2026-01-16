using Encina.Quartz;
using Encina.Messaging.Health;
using Encina.Quartz.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Quartz;
using Shouldly;

namespace Encina.UnitTests.Quartz.Health;

public sealed class QuartzHealthCheckTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IScheduler _scheduler;

    public QuartzHealthCheckTests()
    {
        _scheduler = Substitute.For<IScheduler>();
        _schedulerFactory = Substitute.For<ISchedulerFactory>();

        _scheduler.SchedulerName.Returns("TestScheduler");
        _scheduler.IsStarted.Returns(true);
        _scheduler.IsShutdown.Returns(false);
        _scheduler.InStandbyMode.Returns(false);

        // GetMetaData is called to retrieve stats, but we skip mocking it here
        // to avoid SchedulerMetaData's complex constructor requirements.
        // Tests that check metadata values will need integration tests.

        _schedulerFactory.GetScheduler(Arg.Any<CancellationToken>()).Returns(_scheduler);

        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(ISchedulerFactory)).Returns(_schedulerFactory);
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        // Assert
        QuartzHealthCheck.DefaultName.ShouldBe("encina-quartz");
    }

    [Fact]
    public void Constructor_SetsNameFromOptions()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "custom-quartz" };

        // Act
        var healthCheck = new QuartzHealthCheck(_serviceProvider, options);

        // Assert
        healthCheck.Name.ShouldBe("custom-quartz");
    }

    [Fact]
    public void Constructor_SetsDefaultNameWhenOptionsNull()
    {
        // Act
        var healthCheck = new QuartzHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Name.ShouldBe(QuartzHealthCheck.DefaultName);
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        // Arrange
        var healthCheck = new QuartzHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("scheduling");
        healthCheck.Tags.ShouldContain("quartz");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenGetMetaDataThrows_ReturnsUnhealthy()
    {
        // Arrange
        // GetMetaData throws because we can't mock SchedulerMetaData (no parameterless constructor)
        // This test verifies the exception is caught properly
        var healthCheck = new QuartzHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert - GetMetaData returns null by default, which will cause exception
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenShutdown_ReturnsUnhealthy()
    {
        // Arrange
        _scheduler.IsShutdown.Returns(true);
        var healthCheck = new QuartzHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("shut down");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenInStandby_ReturnsDegraded()
    {
        // Arrange
        _scheduler.IsShutdown.Returns(false);
        _scheduler.InStandbyMode.Returns(true);
        var healthCheck = new QuartzHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("standby");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNotStarted_ReturnsDegraded()
    {
        // Arrange
        _scheduler.IsShutdown.Returns(false);
        _scheduler.InStandbyMode.Returns(false);
        _scheduler.IsStarted.Returns(false);
        var healthCheck = new QuartzHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("not started");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSchedulerNull_ReturnsUnhealthy()
    {
        // Arrange
        _schedulerFactory.GetScheduler(Arg.Any<CancellationToken>())
            .Returns((IScheduler)null!);
        var healthCheck = new QuartzHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("not available");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenFactoryNotRegistered_ReturnsUnhealthy()
    {
        // Arrange
        _serviceProvider.GetService(typeof(ISchedulerFactory))
            .Returns(_ => throw new InvalidOperationException("Factory not registered"));
        var healthCheck = new QuartzHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSchedulerException_ReturnsUnhealthy()
    {
        // Arrange
        _schedulerFactory.GetScheduler(Arg.Any<CancellationToken>())
            .ThrowsAsync(new SchedulerException("Connection failed"));
        var healthCheck = new QuartzHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Connection failed");
    }

    [Fact]
    public async Task CheckHealthAsync_RespectsTimeout()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Timeout = TimeSpan.FromMilliseconds(100) };

        _schedulerFactory.GetScheduler(Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), callInfo.Arg<CancellationToken>());
                return _scheduler;
            });

        var healthCheck = new QuartzHealthCheck(_serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("timed out");
    }
}
