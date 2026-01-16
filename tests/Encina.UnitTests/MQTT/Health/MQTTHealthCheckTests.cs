using Encina.Messaging.Health;
using Encina.MQTT.Health;
using MQTTnet;

namespace Encina.UnitTests.MQTT.Health;

public sealed class MQTTHealthCheckTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMqttClient _client;

    public MQTTHealthCheckTests()
    {
        _client = Substitute.For<IMqttClient>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(IMqttClient)).Returns(_client);
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        // Assert
        MQTTHealthCheck.DefaultName.ShouldBe("encina-mqtt");
    }

    [Fact]
    public void Constructor_SetsNameFromOptions()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "custom-mqtt" };

        // Act
        var healthCheck = new MQTTHealthCheck(_serviceProvider, options);

        // Assert
        healthCheck.Name.ShouldBe("custom-mqtt");
    }

    [Fact]
    public void Constructor_SetsDefaultNameWhenOptionsNull()
    {
        // Act
        var healthCheck = new MQTTHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Name.ShouldBe(MQTTHealthCheck.DefaultName);
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        // Arrange
        var healthCheck = new MQTTHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("messaging");
        healthCheck.Tags.ShouldContain("mqtt");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnected_ReturnsHealthy()
    {
        // Arrange
        _client.IsConnected.Returns(true);
        var healthCheck = new MQTTHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("connected");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNotConnected_ReturnsUnhealthy()
    {
        // Arrange
        _client.IsConnected.Returns(false);
        var healthCheck = new MQTTHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("disconnected");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenExceptionThrown_ReturnsUnhealthy()
    {
        // Arrange
        _serviceProvider.GetService(typeof(IMqttClient))
            .Returns(_ => throw new InvalidOperationException("Client not available"));
        var healthCheck = new MQTTHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldNotBeNull();
    }
}
