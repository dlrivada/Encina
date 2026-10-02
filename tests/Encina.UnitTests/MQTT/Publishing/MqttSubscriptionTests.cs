using System.Text.Json;
using Encina.Diagnostics;
using Encina.MQTT;
using Encina.Testing;
using Microsoft.Extensions.Logging.Testing;
using MQTTnet;
using MQTTnet.Packets;

namespace Encina.UnitTests.MQTT.Publishing;

/// <summary>
/// Unit tests for the message callbacks of <see cref="MqttSubscription{TMessage}"/> and
/// <see cref="MqttPatternSubscription{TMessage}"/>: topic matching, delivery, null payloads and
/// handler failures, including that the logged exception is redacted.
/// </summary>
public sealed class MqttSubscriptionTests : IDisposable
{
    private const string SentinelMessage = "sentinel-secret-message";

    private readonly MqttClient _client = (MqttClient)new MqttClientFactory().CreateMqttClient();
    private readonly FakeLogger _logger = new();

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task OnMessageReceived_ForTheSubscribedTopic_DeliversTheDeserializedMessage()
    {
        // Arrange
        Reading? received = null;
        var subscription = new MqttSubscription<Reading>(_client, "sensors/a", m => { received = m; return ValueTask.CompletedTask; }, _logger);

        // Act
        await subscription.OnMessageReceived(Args("sensors/a", JsonSerializer.SerializeToUtf8Bytes(new Reading(21))));

        // Assert
        received.ShouldNotBeNull();
        received!.Value.ShouldBe(21);
    }

    [Fact]
    public async Task OnMessageReceived_ForAnotherTopic_DoesNotInvokeTheHandler()
    {
        // Arrange
        var calls = 0;
        var subscription = new MqttSubscription<Reading>(_client, "sensors/a", _ => { calls++; return ValueTask.CompletedTask; }, _logger);

        // Act
        await subscription.OnMessageReceived(Args("sensors/b", JsonSerializer.SerializeToUtf8Bytes(new Reading(1))));

        // Assert
        calls.ShouldBe(0);
    }

    [Fact]
    public async Task OnMessageReceived_WithANullPayload_DoesNotInvokeTheHandler()
    {
        // Arrange
        var calls = 0;
        var subscription = new MqttSubscription<Reading>(_client, "sensors/a", _ => { calls++; return ValueTask.CompletedTask; }, _logger);

        // Act
        await subscription.OnMessageReceived(Args("sensors/a", "null"u8.ToArray()));

        // Assert
        calls.ShouldBe(0);
    }

    [Fact]
    public async Task OnMessageReceived_WhenTheHandlerThrows_LogsARedactedExceptionAndDoesNotThrow()
    {
        // Arrange
        var subscription = new MqttSubscription<Reading>(
            _client, "sensors/a", _ => throw new InvalidOperationException(SentinelMessage), _logger);

        // Act
        await subscription.OnMessageReceived(Args("sensors/a", JsonSerializer.SerializeToUtf8Bytes(new Reading(1))));

        // Assert
        AssertSingleRedactedLog();
    }

    [Theory]
    [InlineData("sensors/#", "sensors/a/b")]
    [InlineData("sensors/+/temp", "sensors/a/temp")]
    [InlineData("sensors/a", "sensors/a")]
    public async Task PatternOnMessageReceived_ForAMatchingTopic_DeliversTopicAndMessage(string filter, string topic)
    {
        // Arrange
        (string Topic, Reading Message)? received = null;
        var subscription = new MqttPatternSubscription<Reading>(
            _client, filter, (t, m) => { received = (t, m); return ValueTask.CompletedTask; }, _logger);

        // Act
        await subscription.OnMessageReceived(Args(topic, JsonSerializer.SerializeToUtf8Bytes(new Reading(7))));

        // Assert
        received.ShouldNotBeNull();
        received!.Value.Topic.ShouldBe(topic);
        received.Value.Message.Value.ShouldBe(7);
    }

    [Theory]
    [InlineData("sensors/+/temp", "sensors/a/humidity")]
    [InlineData("sensors/+/temp", "sensors/a")]
    [InlineData("sensors/a", "sensors/b")]
    public async Task PatternOnMessageReceived_ForANonMatchingTopic_DoesNotInvokeTheHandler(string filter, string topic)
    {
        // Arrange
        var calls = 0;
        var subscription = new MqttPatternSubscription<Reading>(
            _client, filter, (_, _) => { calls++; return ValueTask.CompletedTask; }, _logger);

        // Act
        await subscription.OnMessageReceived(Args(topic, JsonSerializer.SerializeToUtf8Bytes(new Reading(7))));

        // Assert
        calls.ShouldBe(0);
    }

    [Fact]
    public async Task PatternOnMessageReceived_WithANullPayload_DoesNotInvokeTheHandler()
    {
        // Arrange
        var calls = 0;
        var subscription = new MqttPatternSubscription<Reading>(
            _client, "sensors/#", (_, _) => { calls++; return ValueTask.CompletedTask; }, _logger);

        // Act
        await subscription.OnMessageReceived(Args("sensors/a", "null"u8.ToArray()));

        // Assert
        calls.ShouldBe(0);
    }

    [Fact]
    public async Task PatternOnMessageReceived_WhenTheHandlerThrows_LogsARedactedExceptionAndDoesNotThrow()
    {
        // Arrange
        var subscription = new MqttPatternSubscription<Reading>(
            _client, "sensors/#", (_, _) => throw new InvalidOperationException(SentinelMessage), _logger);

        // Act
        await subscription.OnMessageReceived(Args("sensors/a", JsonSerializer.SerializeToUtf8Bytes(new Reading(1))));

        // Assert
        AssertSingleRedactedLog();
    }

    private void AssertSingleRedactedLog()
    {
        var entry = _logger.Collector.GetSnapshot().Single(r => r.Exception is not null);
        entry.Exception.ShouldBeOfType<RedactedException>();
        entry.Exception!.ToString().ShouldNotContain(SentinelMessage);
        entry.Message.ShouldNotContain(SentinelMessage);
    }

    private static MqttApplicationMessageReceivedEventArgs Args(string topic, byte[] payload) =>
        new(
            "client",
            new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(payload).Build(),
            new MqttPublishPacket(),
            (_, _) => Task.CompletedTask);

    private sealed record Reading(int Value);
}
