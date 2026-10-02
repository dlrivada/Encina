using Confluent.Kafka;
using Encina.Diagnostics;
using Encina.Kafka;
using Encina.Testing;
using Microsoft.Extensions.Logging.Testing;

namespace Encina.UnitTests.Kafka.Publishing;

/// <summary>
/// Unit tests for the failure paths of <see cref="KafkaMessagePublisher.ProduceBatchAsync{TMessage}"/>:
/// a message that fails to produce and a source sequence that fails while it is enumerated.
/// </summary>
public sealed class KafkaMessagePublisherBatchTests : IDisposable
{
    private const string SentinelMessage = "sentinel-secret-message";

    private readonly IProducer<string, byte[]> _producer = Substitute.For<IProducer<string, byte[]>>();
    private readonly FakeLogger<KafkaMessagePublisher> _logger = new();
    private readonly KafkaMessagePublisher _publisher;

    public KafkaMessagePublisherBatchTests()
    {
        _publisher = new KafkaMessagePublisher(
            _producer,
            _logger,
            Options.Create(new EncinaKafkaOptions { DefaultEventTopic = "test-events" }));
    }

    public void Dispose() => _publisher.Dispose();

    [Fact]
    public async Task ProduceBatchAsync_WhenTheSecondMessageFails_ReturnsThatErrorAndStopsProducing()
    {
        // Arrange
        var calls = 0;
        _producer
            .ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, byte[]>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                calls++;
                return calls == 2
                    ? Task.FromException<DeliveryResult<string, byte[]>>(new InvalidOperationException(SentinelMessage))
                    : Task.FromResult(new DeliveryResult<string, byte[]>
                    {
                        Topic = "test-events",
                        Partition = new Partition(0),
                        Offset = new Offset(calls),
                        Message = callInfo.Arg<Message<string, byte[]>>()
                    });
            });

        // Act
        var result = await _publisher.ProduceBatchAsync(new[]
        {
            (new Payload("a"), (string?)null),
            (new Payload("b"), null),
            (new Payload("c"), null)
        });

        // Assert
        result.ShouldBeErrorWithCode("KAFKA_PRODUCE_FAILED");
        calls.ShouldBe(2);
    }

    [Fact]
    public async Task ProduceBatchAsync_WhenTheSourceSequenceThrows_ReturnsBatchFailedAndLogsARedactedException()
    {
        _producer
            .ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, byte[]>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(new DeliveryResult<string, byte[]>
            {
                Topic = "test-events",
                Partition = new Partition(0),
                Offset = new Offset(1),
                Message = callInfo.Arg<Message<string, byte[]>>()
            }));

        // Act
        var result = await _publisher.ProduceBatchAsync(FailingSequence());

        // Assert
        result.ShouldBeErrorWithCode("KAFKA_BATCH_PRODUCE_FAILED");
        var entry = _logger.Collector.GetSnapshot().Single(r => r.Exception is not null);
        entry.Exception.ShouldBeOfType<RedactedException>();
        entry.Exception!.ToString().ShouldNotContain(SentinelMessage);
        entry.Message.ShouldNotContain(SentinelMessage);
    }

    private static IEnumerable<(Payload Message, string? Key)> FailingSequence()
    {
        yield return (new Payload("a"), null);
        throw new InvalidOperationException(SentinelMessage);
    }

    private sealed record Payload(string Text);
}
