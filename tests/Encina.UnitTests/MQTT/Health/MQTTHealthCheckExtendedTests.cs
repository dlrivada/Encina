using Encina.Messaging.Health;
using Encina.MQTT.Health;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.MQTT.Health;

/// <summary>
/// Extended unit tests for <see cref="MQTTHealthCheck"/>.
/// </summary>
public sealed class MQTTHealthCheckExtendedTests
{
    [Fact]
    public void DefaultName_ShouldBeExpected()
    {
        MQTTHealthCheck.DefaultName.ShouldBe("encina-mqtt");
    }

    [Fact]
    public void Constructor_WithNullOptions_UsesDefaults()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();

        // Act
        var healthCheck = new MQTTHealthCheck(serviceProvider, null);

        // Assert
        healthCheck.Name.ShouldBe("encina-mqtt");
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("mqtt");
    }

    [Fact]
    public void Constructor_WithCustomOptions_UsesCustomValues()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new ProviderHealthCheckOptions
        {
            Name = "custom-mqtt",
            Tags = ["custom-tag"]
        };

        // Act
        var healthCheck = new MQTTHealthCheck(serviceProvider, options);

        // Assert
        healthCheck.Name.ShouldBe("custom-mqtt");
        healthCheck.Tags.ShouldContain("custom-tag");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenClientConnected_ReturnsHealthy()
    {
        // Arrange
        var client = Substitute.For<IMqttClient>();
        client.IsConnected.Returns(true);

        var services = new ServiceCollection();
        services.AddSingleton(client);
        var sp = services.BuildServiceProvider();

        var healthCheck = new MQTTHealthCheck(sp, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("connected");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenClientDisconnected_ReturnsUnhealthy()
    {
        // Arrange
        var client = Substitute.For<IMqttClient>();
        client.IsConnected.Returns(false);

        var services = new ServiceCollection();
        services.AddSingleton(client);
        var sp = services.BuildServiceProvider();

        var healthCheck = new MQTTHealthCheck(sp, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("disconnected");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenClientNotRegistered_ReturnsUnhealthy()
    {
        // Arrange - no client registered, base class catches exception
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();

        var healthCheck = new MQTTHealthCheck(sp, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }
}
