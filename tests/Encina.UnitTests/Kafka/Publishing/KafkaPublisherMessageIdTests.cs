using System.Text;
using Confluent.Kafka;
using Encina.Kafka;

namespace Encina.UnitTests.Kafka.Publishing;

/// <summary>
/// Regression tests for #1792: the caller-supplied message id (for example the outbox id) travels in
/// the <c>MessageId</c> header, and a new id is generated only when none is supplied.
/// </summary>
public sealed class KafkaPublisherMessageIdTests
{
    private readonly IProducer<string, byte[]> _producer = Substitute.For<IProducer<string, byte[]>>();
    private readonly KafkaMessagePublisher _publisher;
    private Message<string, byte[]>? _sent;

    public KafkaPublisherMessageIdTests()
    {
        _producer
            .ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, byte[]>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                _sent = callInfo.Arg<Message<string, byte[]>>();
                return Task.FromResult(new DeliveryResult<string, byte[]>
                {
                    Topic = "t",
                    Partition = new Partition(0),
                    Offset = new Offset(1),
                    Message = _sent
                });
            });
        _publisher = new KafkaMessagePublisher(
            _producer,
            Substitute.For<ILogger<KafkaMessagePublisher>>(),
            Options.Create(new EncinaKafkaOptions { DefaultEventTopic = "t" }));
    }

    private string SentMessageId() =>
        Encoding.UTF8.GetString(_sent!.Headers.GetLastBytes("MessageId"));

    [Fact]
    public async Task ProduceAsync_WithMessageId_PreservesItInHeader()
    {
        await _publisher.ProduceAsync(new Payload("a"), messageId: "outbox-1");

        SentMessageId().ShouldBe("outbox-1");
    }

    [Fact]
    public async Task ProduceWithHeadersAsync_WithMessageId_PreservesItInHeader()
    {
        await _publisher.ProduceWithHeadersAsync(
            new Payload("a"),
            new Dictionary<string, byte[]> { ["x"] = [1] },
            messageId: "outbox-2");

        SentMessageId().ShouldBe("outbox-2");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ProduceAsync_WithoutMessageId_GeneratesANewGuid(string? messageId)
    {
        await _publisher.ProduceAsync(new Payload("a"), messageId: messageId);

        Guid.TryParse(SentMessageId(), out _).ShouldBeTrue();
    }

    [Fact]
    public async Task ProduceWithHeadersAsync_CallerMessageIdHeader_IsReplacedNotDuplicated()
    {
        await _publisher.ProduceWithHeadersAsync(
            new Payload("a"),
            new Dictionary<string, byte[]> { ["MessageId"] = Encoding.UTF8.GetBytes("caller") },
            messageId: "outbox-3");

        _sent!.Headers.Count(h => h.Key == "MessageId").ShouldBe(1);
        SentMessageId().ShouldBe("outbox-3");
    }

    [Fact]
    public async Task ProduceWithHeadersAsync_WithoutMessageId_GeneratesANewGuid()
    {
        await _publisher.ProduceWithHeadersAsync(new Payload("a"), new Dictionary<string, byte[]>());

        Guid.TryParse(SentMessageId(), out _).ShouldBeTrue();
    }

    private sealed record Payload(string Value);
}
