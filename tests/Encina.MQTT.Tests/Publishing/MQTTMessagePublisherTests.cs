using Encina.Testing;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using NSubstitute;

namespace Encina.MQTT.Tests.Publishing;

/// <summary>
/// Unit tests for <see cref="MQTTMessagePublisher"/> using EitherAssertions.
/// </summary>
public sealed class MQTTMessagePublisherTests
{
    private readonly IMqttClient _client;
    private readonly ILogger<MQTTMessagePublisher> _logger;
    private readonly IOptions<EncinaMQTTOptions> _options;

    public MQTTMessagePublisherTests()
    {
        _client = Substitute.For<IMqttClient>();
        _logger = Substitute.For<ILogger<MQTTMessagePublisher>>();
        // Enable logging for coverage of LoggerMessage generated code
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _options = Options.Create(new EncinaMQTTOptions
        {
            TopicPrefix = "test",
            QualityOfService = MqttQualityOfService.AtLeastOnce
        });

        // Default: client is connected
        _client.IsConnected.Returns(true);
    }

    #region PublishAsync Tests

    [Fact]
    public async Task PublishAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.PublishAsync<TestMessage>(null!).AsTask());
    }

    [Fact]
    public async Task PublishAsync_WithValidMessage_ShouldBeSuccess()
    {
        // Arrange
        ConfigureClientSuccess();
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishAsync(message);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task PublishAsync_WithValidMessage_ShouldReturnUnit()
    {
        // Arrange
        ConfigureClientSuccess();
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishAsync(message);

        // Assert
        var value = result.ShouldBeSuccess();
        value.ShouldBe(Unit.Default);
    }

    [Fact]
    public async Task PublishAsync_WithCustomTopic_ShouldBeSuccess()
    {
        // Arrange
        ConfigureClientSuccess();
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var customTopic = "custom/topic";

        // Act
        var result = await publisher.PublishAsync(message, customTopic);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task PublishAsync_WithQoSAtMostOnce_ShouldBeSuccess()
    {
        // Arrange
        ConfigureClientSuccess();
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishAsync(message, qos: MqttQualityOfService.AtMostOnce);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task PublishAsync_WithQoSExactlyOnce_ShouldBeSuccess()
    {
        // Arrange
        ConfigureClientSuccess();
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishAsync(message, qos: MqttQualityOfService.ExactlyOnce);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task PublishAsync_WithRetainFlag_ShouldBeSuccess()
    {
        // Arrange
        ConfigureClientSuccess();
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishAsync(message, retain: true);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task PublishAsync_WhenClientThrowsException_ShouldBeError()
    {
        // Arrange
        ConfigureClientToThrow(new InvalidOperationException("Connection lost"));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishAsync(message);

        // Assert
        result.ShouldBeError();
    }

    [Fact]
    public async Task PublishAsync_WhenClientThrowsException_ShouldReturnErrorWithCode()
    {
        // Arrange
        ConfigureClientToThrow(new InvalidOperationException("Connection lost"));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishAsync(message);

        // Assert
        result.ShouldBeErrorWithCode("MQTT_PUBLISH_FAILED");
    }

    [Fact]
    public async Task PublishAsync_WhenClientThrowsException_ShouldContainErrorMessage()
    {
        // Arrange
        ConfigureClientToThrow(new InvalidOperationException("Connection closed"));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishAsync(message);

        // Assert
        result.ShouldBeErrorContaining("Failed to publish message");
    }

    [Fact]
    public async Task PublishAsync_WhenClientThrowsException_ShouldContainTopicInError()
    {
        // Arrange
        ConfigureClientToThrow(new InvalidOperationException("Connection closed"));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var customTopic = "sensor/temperature";

        // Act
        var result = await publisher.PublishAsync(message, customTopic);

        // Assert
        result.ShouldBeErrorContaining(customTopic);
    }

    #endregion

    #region IsConnected Tests

    [Fact]
    public void IsConnected_WhenClientConnected_ShouldReturnTrue()
    {
        // Arrange
        _client.IsConnected.Returns(true);
        var publisher = CreatePublisher();

        // Act
        var isConnected = publisher.IsConnected;

        // Assert
        isConnected.ShouldBeTrue();
    }

    [Fact]
    public void IsConnected_WhenClientDisconnected_ShouldReturnFalse()
    {
        // Arrange
        _client.IsConnected.Returns(false);
        var publisher = CreatePublisher();

        // Act
        var isConnected = publisher.IsConnected;

        // Assert
        isConnected.ShouldBeFalse();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullClient_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new MQTTMessagePublisher(null!, _logger, _options));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new MQTTMessagePublisher(_client, null!, _options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new MQTTMessagePublisher(_client, _logger, null!));
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_WhenConnected_ShouldDisconnectAndDispose()
    {
        // Arrange
        _client.IsConnected.Returns(true);
        var publisher = CreatePublisher();

        // Act
        await publisher.DisposeAsync();

        // Assert
        await _client.Received(1).DisconnectAsync(
            Arg.Any<MqttClientDisconnectOptions>(),
            Arg.Any<CancellationToken>());
        _client.Received(1).Dispose();
    }

    [Fact]
    public async Task DisposeAsync_WhenNotConnected_ShouldOnlyDispose()
    {
        // Arrange
        _client.IsConnected.Returns(false);
        var publisher = CreatePublisher();

        // Act
        await publisher.DisposeAsync();

        // Assert
        await _client.DidNotReceive().DisconnectAsync(
            Arg.Any<MqttClientDisconnectOptions>(),
            Arg.Any<CancellationToken>());
        _client.Received(1).Dispose();
    }

    #endregion

    #region SubscribeAsync Tests

    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        ConfigureClientSuccess();
        _client.SubscribeAsync(Arg.Any<MqttClientSubscribeOptions>(), Arg.Any<CancellationToken>())
            .Returns(new MqttClientSubscribeResult(0, [], "test", []));

        var publisher = CreatePublisher();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.SubscribeAsync<TestMessage>(null!, "test/topic").AsTask());
    }

    [Fact]
    public async Task SubscribeAsync_WithNullTopic_ThrowsArgumentNullException()
    {
        // Arrange
        ConfigureClientSuccess();
        var publisher = CreatePublisher();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.SubscribeAsync<TestMessage>(_ => ValueTask.CompletedTask, null!).AsTask());
    }

    #endregion

    #region SubscribePatternAsync Tests

    [Fact]
    public async Task SubscribePatternAsync_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        ConfigureClientSuccess();
        var publisher = CreatePublisher();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.SubscribePatternAsync<TestMessage>(null!, "test/#").AsTask());
    }

    [Fact]
    public async Task SubscribePatternAsync_WithNullTopicFilter_ThrowsArgumentNullException()
    {
        // Arrange
        ConfigureClientSuccess();
        var publisher = CreatePublisher();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.SubscribePatternAsync<TestMessage>((_, _) => ValueTask.CompletedTask, null!).AsTask());
    }

    #endregion

    #region Helper Methods

    private MQTTMessagePublisher CreatePublisher()
    {
        return new MQTTMessagePublisher(_client, _logger, _options);
    }

    private void ConfigureClientSuccess()
    {
        _client
            .PublishAsync(Arg.Any<MqttApplicationMessage>(), Arg.Any<CancellationToken>())
            .Returns(new MqttClientPublishResult(0, MqttClientPublishReasonCode.Success, string.Empty, []));
    }

    private void ConfigureClientToThrow(Exception exception)
    {
        _client
            .PublishAsync(Arg.Any<MqttApplicationMessage>(), Arg.Any<CancellationToken>())
            .Returns<MqttClientPublishResult>(_ => throw exception);
    }

    #endregion

    #region Test Types

    private sealed record TestMessage
    {
        public int Id { get; init; }
        public string Content { get; init; } = string.Empty;
    }

    #endregion
}
