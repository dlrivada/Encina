using Encina.IntegrationTests.Infrastructure.Caching.Fixtures;
using Encina.Caching.Redis.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Encina.IntegrationTests.Infrastructure.Caching.Health;

/// <summary>
/// Integration tests for RedisHealthCheck using a real Redis container.
/// </summary>
[Collection(RedisCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "Redis")]
public class RedisHealthCheckIntegrationTests
{
    private readonly RedisFixture _fixture;

    public RedisHealthCheckIntegrationTests(RedisFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WhenRedisIsRunning_ReturnsHealthy()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new RedisHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        (result.Description ?? string.Empty).ShouldContain("reachable");
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "my-custom-redis" };
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new RedisHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.ShouldBe("my-custom-redis");
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WithCustomTags_UsesCustomTags()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var customTags = new[] { "custom", "tags" };
        var options = new ProviderHealthCheckOptions { Tags = customTags };
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new RedisHealthCheck(serviceProvider, options);

        // Assert - health check adds "encina" tag by default
        healthCheck.Tags.ShouldContain("custom");
        healthCheck.Tags.ShouldContain("tags");
        healthCheck.Tags.ShouldContain("encina");
    }

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_fixture.Connection!);
        return services.BuildServiceProvider();
    }
}
