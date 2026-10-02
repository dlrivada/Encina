using System.Text.Json;
using Encina.Diagnostics;
using Encina.Redis.PubSub;
using Encina.Testing;
using Microsoft.Extensions.Logging.Testing;

namespace Encina.UnitTests.Messaging.RedisPubSub;

/// <summary>
/// Unit tests for the delivery of Redis Pub/Sub messages to subscriber handlers
/// (<see cref="RedisPubSubMessagePublisher.DeliverAsync{TMessage}"/> and
/// <see cref="RedisPubSubMessagePublisher.DeliverPatternAsync{TMessage}"/>): delivery, empty
/// payloads and handler failures, including that the logged exception is redacted.
/// </summary>
public sealed class RedisPubSubDeliveryTests
{
    private const string SentinelMessage = "sentinel-secret-message";

    private readonly FakeLogger _logger = new();

    [Fact]
    public async Task DeliverAsync_WithAPayload_InvokesTheHandlerWithIt()
    {
        // Arrange
        Alert? received = null;

        // Act
        await RedisPubSubMessagePublisher.DeliverAsync<Alert>(
            Wrap(new Alert("fire")), a => { received = a; return ValueTask.CompletedTask; }, _logger, "events");

        // Assert
        received.ShouldNotBeNull();
        received!.Text.ShouldBe("fire");
    }

    [Fact]
    public async Task DeliverAsync_WithoutAPayload_DoesNotInvokeTheHandler()
    {
        // Arrange
        var calls = 0;

        // Act
        await RedisPubSubMessagePublisher.DeliverAsync<Alert>(
            """{"MessageType":"x","Payload":null}""", _ => { calls++; return ValueTask.CompletedTask; }, _logger, "events");

        // Assert
        calls.ShouldBe(0);
    }

    [Fact]
    public async Task DeliverAsync_WhenTheHandlerThrows_LogsARedactedExceptionAndDoesNotThrow()
    {
        // Act
        await RedisPubSubMessagePublisher.DeliverAsync<Alert>(
            Wrap(new Alert("fire")), _ => throw new InvalidOperationException(SentinelMessage), _logger, "events");

        // Assert
        AssertSingleRedactedLog();
    }

    [Fact]
    public async Task DeliverAsync_WithInvalidJson_LogsARedactedExceptionAndDoesNotThrow()
    {
        // Act
        await RedisPubSubMessagePublisher.DeliverAsync<Alert>(
            "{not json", _ => ValueTask.CompletedTask, _logger, "events");

        // Assert
        _logger.Collector.GetSnapshot().Single(r => r.Exception is not null).Exception.ShouldBeOfType<RedactedException>();
    }

    [Fact]
    public async Task DeliverPatternAsync_WithAPayload_InvokesTheHandlerWithChannelAndPayload()
    {
        // Arrange
        (string Channel, Alert Alert)? received = null;

        // Act
        await RedisPubSubMessagePublisher.DeliverPatternAsync<Alert>(
            Wrap(new Alert("fire")), "prefix:orders", (c, a) => { received = (c, a); return ValueTask.CompletedTask; }, _logger);

        // Assert
        received.ShouldNotBeNull();
        received!.Value.Channel.ShouldBe("prefix:orders");
        received.Value.Alert.Text.ShouldBe("fire");
    }

    [Fact]
    public async Task DeliverPatternAsync_WhenTheHandlerThrows_LogsARedactedExceptionAndDoesNotThrow()
    {
        // Act
        await RedisPubSubMessagePublisher.DeliverPatternAsync<Alert>(
            Wrap(new Alert("fire")), "prefix:orders", (_, _) => throw new InvalidOperationException(SentinelMessage), _logger);

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

    private static string Wrap(Alert alert) =>
        JsonSerializer.Serialize(new { MessageType = nameof(Alert), Payload = alert, TimestampUtc = DateTime.UnixEpoch });

    public sealed record Alert(string Text);
}
