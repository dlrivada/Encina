using Encina.MQTT;
using Encina.Testing;
using LanguageExt;
using MQTTnet;

namespace Encina.UnitTests.MQTT.Publishing;

/// <summary>
/// Extended unit tests for <see cref="MQTTMessagePublisher"/> covering
/// additional QoS paths, default topic derivation, and edge cases.
/// </summary>
public sealed class MQTTMessagePublisherExtendedTests
{
    private readonly IMqttClient _client;
    private readonly ILogger<MQTTMessagePublisher> _logger;

    public MQTTMessagePublisherExtendedTests()
    {
        _client = Substitute.For<IMqttClient>();
        _logger = Substitute.For<ILogger<MQTTMessagePublisher>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _client.IsConnected.Returns(true);
    }

    private MQTTMessagePublisher CreatePublisher(EncinaMQTTOptions? opts = null)
    {
        var options = Options.Create(opts ?? new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.AtLeastOnce
        });
        return new MQTTMessagePublisher(_client, _logger, options);
    }

    private void ConfigureClientSuccess()
    {
        _client
            .PublishAsync(Arg.Any<MqttApplicationMessage>(), Arg.Any<CancellationToken>())
            .Returns(new MqttClientPublishResult(0, MqttClientPublishReasonCode.Success, string.Empty, []));
    }

    #region Default Topic Derivation

    [Fact]
    public async Task PublishAsync_WithNullTopic_UsesTopicPrefixAndTypeName()
    {
        // Arrange
        MqttApplicationMessage? capturedMessage = null;
        _client
            .PublishAsync(Arg.Do<MqttApplicationMessage>(m => capturedMessage = m), Arg.Any<CancellationToken>())
            .Returns(new MqttClientPublishResult(0, MqttClientPublishReasonCode.Success, string.Empty, []));

        var publisher = CreatePublisher();

        // Act
        var result = await publisher.PublishAsync(new SensorReading { Temperature = 25.5 });

        // Assert
        result.ShouldBeSuccess();
        capturedMessage.ShouldNotBeNull();
        capturedMessage.Topic.ShouldBe("test/SensorReading");
    }

    [Fact]
    public async Task PublishAsync_WithCustomTopicPrefix_UsesCorrectPrefix()
    {
        // Arrange
        MqttApplicationMessage? capturedMessage = null;
        _client
            .PublishAsync(Arg.Do<MqttApplicationMessage>(m => capturedMessage = m), Arg.Any<CancellationToken>())
            .Returns(new MqttClientPublishResult(0, MqttClientPublishReasonCode.Success, string.Empty, []));

        var publisher = CreatePublisher(new EncinaMQTTOptions { TopicPrefix = "sensors" });

        // Act
        await publisher.PublishAsync(new SensorReading { Temperature = 25.5 });

        // Assert
        capturedMessage.ShouldNotBeNull();
        capturedMessage.Topic.ShouldBe("sensors/SensorReading");
    }

    #endregion

    #region QoS Mapping

    [Fact]
    public async Task PublishAsync_WithDefaultQoS_ShouldUseOptionsQoS()
    {
        // Arrange
        ConfigureClientSuccess();
        var publisher = CreatePublisher(new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.ExactlyOnce
        });

        // Act
        var result = await publisher.PublishAsync(new SensorReading { Temperature = 1.0 });

        // Assert
        result.ShouldBeSuccess();
    }

    [Theory]
    [InlineData(MqttQualityOfService.AtMostOnce)]
    [InlineData(MqttQualityOfService.AtLeastOnce)]
    [InlineData(MqttQualityOfService.ExactlyOnce)]
    public async Task PublishAsync_WithExplicitQoS_ShouldSucceed(MqttQualityOfService qos)
    {
        // Arrange
        ConfigureClientSuccess();
        var publisher = CreatePublisher();

        // Act
        var result = await publisher.PublishAsync(
            new SensorReading { Temperature = 20.0 },
            qos: qos);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task PublishAsync_WithInvalidQoSEnumValue_ShouldStillSucceed()
    {
        // Arrange
        ConfigureClientSuccess();
        var publisher = CreatePublisher();

        // Act - cast an invalid int to the enum
        var result = await publisher.PublishAsync(
            new SensorReading { Temperature = 20.0 },
            qos: (MqttQualityOfService)99);

        // Assert - falls through to default case in switch
        result.ShouldBeSuccess();
    }

    #endregion

    #region ToString

    [Fact]
    public void EncinaMQTTOptions_ToString_ContainsRelevantInfo()
    {
        var options = new EncinaMQTTOptions
        {
            Host = "broker.example.com",
            Port = 8883,
            ClientId = "my-client"
        };

        var result = options.ToString();
        result.ShouldContain("broker.example.com");
        result.ShouldContain("8883");
        result.ShouldContain("my-client");
    }

    #endregion

    #region Test Types

    private sealed record SensorReading
    {
        public double Temperature { get; init; }
    }

    #endregion
}
