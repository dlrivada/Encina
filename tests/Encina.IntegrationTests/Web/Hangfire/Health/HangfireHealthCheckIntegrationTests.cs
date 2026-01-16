using Encina.Hangfire.Health;
using Encina.Messaging.Health;
using Hangfire;
using Hangfire.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.Web.Hangfire.Health;

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
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("operational");
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
        healthCheck.Name.ShouldBe("my-custom-hangfire");
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public void CheckHealthAsync_ReturnsDataWithStatistics()
    {
        // Arrange
        var healthCheck = new HangfireHealthCheck(_serviceProvider, null);

        // Act
        var result = healthCheck.CheckHealthAsync().GetAwaiter().GetResult();

        // Assert
        result.Data.ShouldNotBeNull();
        result.Data.ShouldContainKey("servers");
        result.Data.ShouldContainKey("queues");
        result.Data.ShouldContainKey("scheduled");
        result.Data.ShouldContainKey("enqueued");
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
    public void DefaultName_ShouldBeEncinaHangfire()
    {
        HangfireHealthCheck.DefaultName.ShouldBe("encina-hangfire");
    }

    public void Dispose()
    {
        _server.Dispose();
        _serviceProvider.Dispose();
    }
}
