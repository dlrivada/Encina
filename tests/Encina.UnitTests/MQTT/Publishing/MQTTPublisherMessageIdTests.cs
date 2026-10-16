using Encina.MQTT;
using MQTTnet;

namespace Encina.UnitTests.MQTT.Publishing;

/// <summary>
/// Regression tests for #1792: the caller-supplied message id (for example the outbox id) travels in the
/// <c>MessageId</c> MQTT 5 user property.
/// </summary>
public sealed class MQTTPublisherMessageIdTests
{
    private readonly IMqttClient _client = Substitute.For<IMqttClient>();
    private readonly MQTTMessagePublisher _publisher;

    public MQTTPublisherMessageIdTests()
    {
        _publisher = new MQTTMessagePublisher(
            _client,
            Substitute.For<ILogger<MQTTMessagePublisher>>(),
            Options.Create(new EncinaMQTTOptions { TopicPrefix = "t" }));
    }

    private string SentMessageId() =>
        _client.ReceivedCalls()
            .Single(c => c.GetMethodInfo().Name == nameof(IMqttClient.PublishAsync))
            .GetArguments()
            .OfType<MqttApplicationMessage>()
            .Single()
            .UserProperties!
            .Where(p => p.Name == "MessageId")
            .Select(p => System.Text.Encoding.UTF8.GetString(p.ValueBuffer.Span))
            .Single();

    [Fact]
    public async Task PublishAsync_WithMessageId_SendsItAsUserProperty()
    {
        await _publisher.PublishAsync(new Payload("a"), messageId: "outbox-1");

        SentMessageId().ShouldBe("outbox-1");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task PublishAsync_WithoutMessageId_GeneratesANewGuid(string? messageId)
    {
        await _publisher.PublishAsync(new Payload("a"), messageId: messageId);

        Guid.TryParse(SentMessageId(), out _).ShouldBeTrue();
    }

    private sealed record Payload(string Value);
}
