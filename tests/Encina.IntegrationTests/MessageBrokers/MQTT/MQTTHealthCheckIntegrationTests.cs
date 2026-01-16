using Encina.Messaging.Health;
using Encina.MQTT.Health;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;

namespace Encina.IntegrationTests.MessageBrokers.MQTT;

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
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("connected");
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
        healthCheck.Name.ShouldBe("my-custom-mqtt");
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [SkippableFact]
    public void Tags_ContainsExpectedValues()
    {
        Skip.IfNot(_fixture.IsAvailable, "MQTT container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new MQTTHealthCheck(serviceProvider, null);

        // Act
        var tags = healthCheck.Tags;

        // Assert
        tags.ShouldContain("encina");
        tags.ShouldContain("messaging");
        tags.ShouldContain("mqtt");
        tags.ShouldContain("ready");
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
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("disconnected");

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
