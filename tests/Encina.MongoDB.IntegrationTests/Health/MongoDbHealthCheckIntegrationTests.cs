using Encina.Messaging.Health;
using Encina.MongoDB.Health;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Encina.MongoDB.IntegrationTests.Health;

/// <summary>
/// Integration tests for MongoDbHealthCheck using a real MongoDB container.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class MongoDbHealthCheckIntegrationTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;

    public MongoDbHealthCheckIntegrationTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WhenMongoDbIsRunning_ReturnsHealthy()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new MongoDbHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("reachable");
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB container not available");

        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "my-custom-mongodb" };
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new MongoDbHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.Should().Be("my-custom-mongodb");
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [SkippableFact]
    public void Tags_ContainsExpectedValues()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new MongoDbHealthCheck(serviceProvider, null);

        // Assert
        healthCheck.Tags.Should().Contain("encina");
        healthCheck.Tags.Should().Contain("database");
        healthCheck.Tags.Should().Contain("mongodb");
        healthCheck.Tags.Should().Contain("ready");
    }

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMongoClient>(_fixture.Client!);
        return services.BuildServiceProvider();
    }
}
