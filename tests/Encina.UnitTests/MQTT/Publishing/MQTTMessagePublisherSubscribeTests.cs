using Encina.MQTT;
using MQTTnet;

namespace Encina.UnitTests.MQTT.Publishing;

/// <summary>
/// Unit tests for the subscribe paths of <see cref="MQTTMessagePublisher"/>: the quality-of-service
/// mapping and the call to the client. The substitute client is not an <c>MqttClient</c>, so the
/// subscription wrapper cannot be created and the call ends in an <see cref="InvalidCastException"/>
/// after the broker subscription was requested.
/// </summary>
public sealed class MQTTMessagePublisherSubscribeTests
{
    private readonly IMqttClient _client = Substitute.For<IMqttClient>();

    [Theory]
    [InlineData(MqttQualityOfService.AtMostOnce)]
    [InlineData(MqttQualityOfService.AtLeastOnce)]
    [InlineData(MqttQualityOfService.ExactlyOnce)]
    [InlineData((MqttQualityOfService)99)]
    public async Task SubscribeAsync_RequestsTheSubscriptionFromTheClient(MqttQualityOfService qos)
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act
        await Should.ThrowAsync<InvalidCastException>(() =>
            publisher.SubscribeAsync<Ping>(_ => ValueTask.CompletedTask, "test/topic", qos).AsTask());

        // Assert
        await _client.Received(1).SubscribeAsync(
            Arg.Any<MqttClientSubscribeOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(MqttQualityOfService.AtMostOnce)]
    [InlineData(MqttQualityOfService.AtLeastOnce)]
    [InlineData(MqttQualityOfService.ExactlyOnce)]
    [InlineData((MqttQualityOfService)99)]
    public async Task SubscribePatternAsync_RequestsTheSubscriptionFromTheClient(MqttQualityOfService qos)
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act
        await Should.ThrowAsync<InvalidCastException>(() =>
            publisher.SubscribePatternAsync<Ping>((_, _) => ValueTask.CompletedTask, "test/#", qos).AsTask());

        // Assert
        await _client.Received(1).SubscribeAsync(
            Arg.Any<MqttClientSubscribeOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubscribePatternAsync_WithoutQos_UsesTheConfiguredDefault()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act
        await Should.ThrowAsync<InvalidCastException>(() =>
            publisher.SubscribePatternAsync<Ping>((_, _) => ValueTask.CompletedTask, "test/#").AsTask());

        // Assert
        await _client.Received(1).SubscribeAsync(
            Arg.Is<MqttClientSubscribeOptions>(o => o.TopicFilters.Count == 1 && o.TopicFilters[0].Topic == "test/#"),
            Arg.Any<CancellationToken>());
    }

    private MQTTMessagePublisher CreatePublisher() =>
        new(
            _client,
            Substitute.For<ILogger<MQTTMessagePublisher>>(),
            Options.Create(new EncinaMQTTOptions { QualityOfService = MqttQualityOfService.AtLeastOnce }));

    private sealed record Ping(string Text);
}
