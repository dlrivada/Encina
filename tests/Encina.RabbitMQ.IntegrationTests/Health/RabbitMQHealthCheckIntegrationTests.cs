using Encina.Messaging.Health;
using Encina.RabbitMQ.Health;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Encina.RabbitMQ.IntegrationTests.Health;

/// <summary>
/// Integration tests for RabbitMQHealthCheck using a real RabbitMQ container.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Messaging", "RabbitMQ")]
public sealed class RabbitMQHealthCheckIntegrationTests : IClassFixture<RabbitMqFixture>
{
    private readonly RabbitMqFixture _fixture;

    public RabbitMQHealthCheckIntegrationTests(RabbitMqFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WhenRabbitMQIsRunning_ReturnsHealthy()
    {
        Skip.IfNot(_fixture.IsAvailable, "RabbitMQ container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new RabbitMQHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("connected");
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {
        Skip.IfNot(_fixture.IsAvailable, "RabbitMQ container not available");

        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "my-custom-rabbitmq" };
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new RabbitMQHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.Should().Be("my-custom-rabbitmq");
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [SkippableFact]
    public void Tags_ContainsExpectedValues()
    {
        Skip.IfNot(_fixture.IsAvailable, "RabbitMQ container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new RabbitMQHealthCheck(serviceProvider, null);

        // Assert
        healthCheck.Tags.Should().Contain("encina");
        healthCheck.Tags.Should().Contain("messaging");
        healthCheck.Tags.Should().Contain("rabbitmq");
        healthCheck.Tags.Should().Contain("ready");
    }

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        var connection = _fixture.ConnectionFactory!.CreateConnectionAsync().GetAwaiter().GetResult();
        services.AddSingleton<IConnection>(connection);
        return services.BuildServiceProvider();
    }
}
