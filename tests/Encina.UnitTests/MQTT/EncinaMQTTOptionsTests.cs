using Encina.MQTT;

namespace Encina.UnitTests.MQTT;

/// <summary>
/// Unit tests for <see cref="EncinaMQTTOptions"/>.
/// </summary>
public sealed class EncinaMQTTOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new EncinaMQTTOptions();

        // Assert
        options.Host.ShouldBe("localhost");
        options.Port.ShouldBe(1883);
        options.ClientId.ShouldStartWith("encina-");
        options.TopicPrefix.ShouldBe("encina");
        options.Username.ShouldBeNull();
        options.Password.ShouldBeNull();
        options.QualityOfService.ShouldBe(MqttQualityOfService.AtLeastOnce);
        options.UseTls.ShouldBeFalse();
        options.CleanSession.ShouldBeTrue();
        options.KeepAliveSeconds.ShouldBe(60);
    }

    [Fact]
    public void Host_CanBeSet()
    {
        // Arrange
        var options = new EncinaMQTTOptions();

        // Act
        options.Host = "mqtt.example.com";

        // Assert
        options.Host.ShouldBe("mqtt.example.com");
    }

    [Fact]
    public void Port_CanBeSet()
    {
        // Arrange
        var options = new EncinaMQTTOptions();

        // Act
        options.Port = 8883;

        // Assert
        options.Port.ShouldBe(8883);
    }

    [Fact]
    public void ClientId_CanBeSet()
    {
        // Arrange
        var options = new EncinaMQTTOptions();

        // Act
        options.ClientId = "my-client-id";

        // Assert
        options.ClientId.ShouldBe("my-client-id");
    }

    [Fact]
    public void TopicPrefix_CanBeSet()
    {
        // Arrange
        var options = new EncinaMQTTOptions();

        // Act
        options.TopicPrefix = "myapp";

        // Assert
        options.TopicPrefix.ShouldBe("myapp");
    }

    [Fact]
    public void Username_CanBeSet()
    {
        // Arrange
        var options = new EncinaMQTTOptions();

        // Act
        options.Username = "admin";

        // Assert
        options.Username.ShouldBe("admin");
    }

    [Fact]
    public void Password_CanBeSet()
    {
        // Arrange
        var options = new EncinaMQTTOptions();

        // Act
        options.Password = "secret";

        // Assert
        options.Password.ShouldBe("secret");
    }

    [Theory]
    [InlineData(MqttQualityOfService.AtMostOnce)]
    [InlineData(MqttQualityOfService.AtLeastOnce)]
    [InlineData(MqttQualityOfService.ExactlyOnce)]
    public void QualityOfService_CanBeSet(MqttQualityOfService qos)
    {
        // Arrange
        var options = new EncinaMQTTOptions();

        // Act
        options.QualityOfService = qos;

        // Assert
        options.QualityOfService.ShouldBe(qos);
    }

    [Fact]
    public void UseTls_CanBeSet()
    {
        // Arrange
        var options = new EncinaMQTTOptions();

        // Act
        options.UseTls = true;

        // Assert
        options.UseTls.ShouldBeTrue();
    }

    [Fact]
    public void CleanSession_CanBeSet()
    {
        // Arrange
        var options = new EncinaMQTTOptions();

        // Act
        options.CleanSession = false;

        // Assert
        options.CleanSession.ShouldBeFalse();
    }

    [Fact]
    public void KeepAliveSeconds_CanBeSet()
    {
        // Arrange
        var options = new EncinaMQTTOptions();

        // Act
        options.KeepAliveSeconds = 120;

        // Assert
        options.KeepAliveSeconds.ShouldBe(120);
    }

    [Fact]
    public void ProviderHealthCheck_IsNotNull()
    {
        // Arrange & Act
        var options = new EncinaMQTTOptions();

        // Assert
        options.ProviderHealthCheck.ShouldNotBeNull();
    }

    [Fact]
    public void ProviderHealthCheck_DefaultEnabled_IsTrue()
    {
        // Arrange & Act
        var options = new EncinaMQTTOptions();

        // Assert - opt-out design means health checks are enabled by default
        options.ProviderHealthCheck.Enabled.ShouldBeTrue();
    }
}
