using Encina.Messaging.Health;
using Encina.RabbitMQ.Health;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Encina.IntegrationTests.MessageBrokers.RabbitMQ;

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

    [Fact]
    public async Task CheckHealthAsync_WhenRabbitMQIsRunning_ReturnsHealthy()
    {

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new RabbitMQHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("connected");
    }

    [Fact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {

        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "my-custom-rabbitmq" };
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new RabbitMQHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.ShouldBe("my-custom-rabbitmq");
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new RabbitMQHealthCheck(serviceProvider, null);

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("messaging");
        healthCheck.Tags.ShouldContain("rabbitmq");
        healthCheck.Tags.ShouldContain("ready");
    }

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        var connection = _fixture.ConnectionFactory!.CreateConnectionAsync().GetAwaiter().GetResult();
        services.AddSingleton<IConnection>(connection);
        return services.BuildServiceProvider();
    }
}
