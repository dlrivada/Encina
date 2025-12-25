using Encina.Hangfire.Health;
using Encina.Messaging.Health;
using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Encina.Hangfire.Tests.Health;

public sealed class HangfireHealthCheckTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly JobStorage _storage;
    private readonly IStorageConnection _connection;
    private readonly IMonitoringApi _monitoringApi;

    public HangfireHealthCheckTests()
    {
        _storage = Substitute.For<JobStorage>();
        _connection = Substitute.For<IStorageConnection>();
        _monitoringApi = Substitute.For<IMonitoringApi>();

        _storage.GetConnection().Returns(_connection);
        _storage.GetMonitoringApi().Returns(_monitoringApi);

        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(JobStorage)).Returns(_storage);
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        // Assert
        HangfireHealthCheck.DefaultName.ShouldBe("encina-hangfire");
    }

    [Fact]
    public void Constructor_SetsNameFromOptions()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "custom-hangfire" };

        // Act
        var healthCheck = new HangfireHealthCheck(_serviceProvider, options);

        // Assert
        healthCheck.Name.ShouldBe("custom-hangfire");
    }

    [Fact]
    public void Constructor_SetsDefaultNameWhenOptionsNull()
    {
        // Act
        var healthCheck = new HangfireHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Name.ShouldBe(HangfireHealthCheck.DefaultName);
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        // Arrange
        var healthCheck = new HangfireHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("scheduling");
        healthCheck.Tags.ShouldContain("hangfire");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenOperational_ReturnsHealthy()
    {
        // Arrange
        var stats = new StatisticsDto
        {
            Servers = 1,
            Queues = 1,
            Scheduled = 0,
            Enqueued = 0,
            Processing = 0,
            Succeeded = 100,
            Failed = 0
        };
        _monitoringApi.GetStatistics().Returns(stats);

        var healthCheck = new HangfireHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("operational");
        result.Data.ShouldContainKey("servers");
        result.Data["servers"].ShouldBe(1L);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNoServers_ReturnsDegraded()
    {
        // Arrange
        var stats = new StatisticsDto
        {
            Servers = 0,
            Queues = 1,
            Scheduled = 0,
            Enqueued = 5,
            Processing = 0,
            Succeeded = 100,
            Failed = 0
        };
        _monitoringApi.GetStatistics().Returns(stats);

        var healthCheck = new HangfireHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("no active servers");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStorageNotConfigured_ReturnsUnhealthy()
    {
        // Arrange
        // When DI returns null, Hangfire falls back to JobStorage.Current which throws
        _serviceProvider.GetService(typeof(JobStorage)).Returns(null);
        var healthCheck = new HangfireHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        // Either "not configured" from DI check or "has not been initialized" from JobStorage.Current
        (result.Description!.Contains("not configured") ||
         result.Description!.Contains("has not been initialized")).ShouldBeTrue();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenExceptionThrown_ReturnsUnhealthy()
    {
        // Arrange
        _storage.GetMonitoringApi()
            .Returns(_ => throw new InvalidOperationException("Connection failed"));

        var healthCheck = new HangfireHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Connection failed");
    }
}
