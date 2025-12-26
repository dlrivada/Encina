using Encina.Hangfire.Health;
using Encina.Messaging.Health;
using Hangfire;
using Hangfire.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Hangfire.IntegrationTests.Health;

/// <summary>
/// Integration tests for HangfireHealthCheck using an in-memory storage.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Scheduler", "Hangfire")]
public sealed class HangfireHealthCheckIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly BackgroundJobServer _server;

    public HangfireHealthCheckIntegrationTests()
    {
        var services = new ServiceCollection();

        // Configure Hangfire with in-memory storage
        var storage = new InMemoryStorage();
        GlobalConfiguration.Configuration.UseStorage(storage);
        services.AddSingleton<JobStorage>(storage);

        _serviceProvider = services.BuildServiceProvider();

        // Start a background job server for realistic testing
        _server = new BackgroundJobServer(new BackgroundJobServerOptions
        {
            ServerName = "TestServer"
        });
    }

    [Fact]
    public void CheckHealthAsync_WhenHangfireIsConfigured_ReturnsHealthy()
    {
        // Arrange
        var healthCheck = new HangfireHealthCheck(_serviceProvider, null);

        // Act
        var result = healthCheck.CheckHealthAsync().GetAwaiter().GetResult();

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("operational");
    }

    [Fact]
    public void CheckHealthAsync_WithCustomName_UsesCustomName()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "my-custom-hangfire" };
        var healthCheck = new HangfireHealthCheck(_serviceProvider, options);

        // Act
        var result = healthCheck.CheckHealthAsync().GetAwaiter().GetResult();

        // Assert
        healthCheck.Name.Should().Be("my-custom-hangfire");
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void CheckHealthAsync_ReturnsDataWithStatistics()
    {
        // Arrange
        var healthCheck = new HangfireHealthCheck(_serviceProvider, null);

        // Act
        var result = healthCheck.CheckHealthAsync().GetAwaiter().GetResult();

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.Should().ContainKey("servers");
        result.Data.Should().ContainKey("queues");
        result.Data.Should().ContainKey("scheduled");
        result.Data.Should().ContainKey("enqueued");
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        // Arrange
        var healthCheck = new HangfireHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Tags.Should().Contain("encina");
        healthCheck.Tags.Should().Contain("scheduling");
        healthCheck.Tags.Should().Contain("hangfire");
        healthCheck.Tags.Should().Contain("ready");
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaHangfire()
    {
        HangfireHealthCheck.DefaultName.Should().Be("encina-hangfire");
    }

    public void Dispose()
    {
        _server.Dispose();
        _serviceProvider.Dispose();
    }
}
