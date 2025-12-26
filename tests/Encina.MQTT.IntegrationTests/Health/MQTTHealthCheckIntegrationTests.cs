using Encina.Messaging.Health;
using Encina.MQTT.Health;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;

namespace Encina.MQTT.IntegrationTests.Health;

/// <summary>
/// Integration tests for MQTTHealthCheck using a real MQTT broker container.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Messaging", "MQTT")]
public sealed class MQTTHealthCheckIntegrationTests : IClassFixture<MqttFixture>
{
    private readonly MqttFixture _fixture;

    public MQTTHealthCheckIntegrationTests(MqttFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WhenMQTTBrokerIsRunning_ReturnsHealthy()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new MQTTHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("connected");
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT container not available");

        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "my-custom-mqtt" };
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new MQTTHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.Should().Be("my-custom-mqtt");
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [SkippableFact]
    public void Tags_ContainsExpectedValues()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new MQTTHealthCheck(serviceProvider, null);

        // Assert
        healthCheck.Tags.Should().Contain("encina");
        healthCheck.Tags.Should().Contain("messaging");
        healthCheck.Tags.Should().Contain("mqtt");
        healthCheck.Tags.Should().Contain("ready");
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WhenClientDisconnected_ReturnsUnhealthy()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT container not available");

        // Arrange
        // Create a new disconnected client
        var factory = new MqttClientFactory();
        var disconnectedClient = factory.CreateMqttClient();

        var services = new ServiceCollection();
        services.AddSingleton<IMqttClient>(disconnectedClient);
        var serviceProvider = services.BuildServiceProvider();

        var healthCheck = new MQTTHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("disconnected");

        // Cleanup
        disconnectedClient.Dispose();
    }

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_fixture.Client!);
        return services.BuildServiceProvider();
    }
}
