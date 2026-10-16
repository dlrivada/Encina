using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Encina.AmazonSQS;
using SnsPublishRequest = Amazon.SimpleNotificationService.Model.PublishRequest;
using SqsSendMessageRequest = Amazon.SQS.Model.SendMessageRequest;

namespace Encina.UnitTests.AmazonSQS.Publishing;

/// <summary>
/// Regression tests for #1792: the caller-supplied message id (for example the outbox id) is sent as the
/// <c>MessageId</c> message attribute, and FIFO sends use it as the deduplication id when none is given.
/// </summary>
public sealed class AmazonSQSPublisherMessageIdTests
{
    private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
    private readonly IAmazonSimpleNotificationService _sns = Substitute.For<IAmazonSimpleNotificationService>();
    private readonly AmazonSQSMessagePublisher _publisher;

    public AmazonSQSPublisherMessageIdTests()
    {
        _publisher = new AmazonSQSMessagePublisher(
            _sqs,
            _sns,
            Substitute.For<ILogger<AmazonSQSMessagePublisher>>(),
            Options.Create(new EncinaAmazonSQSOptions
            {
                DefaultQueueUrl = "https://sqs.local/1/q",
                DefaultTopicArn = "arn:aws:sns:us-east-1:1:t"
            }));
    }

    private SqsSendMessageRequest SentSqsRequest() =>
        _sqs.ReceivedCalls().Single(c => c.GetMethodInfo().Name == nameof(IAmazonSQS.SendMessageAsync))
            .GetArguments().OfType<SqsSendMessageRequest>().Single();

    [Fact]
    public async Task SendToQueueAsync_WithMessageId_SendsItAsAttribute()
    {
        await _publisher.SendToQueueAsync(new Payload("a"), messageId: "outbox-1");

        SentSqsRequest().MessageAttributes["MessageId"].StringValue.ShouldBe("outbox-1");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SendToQueueAsync_WithoutMessageId_GeneratesANewGuid(string? messageId)
    {
        await _publisher.SendToQueueAsync(new Payload("a"), messageId: messageId);

        Guid.TryParse(SentSqsRequest().MessageAttributes["MessageId"].StringValue, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task PublishToTopicAsync_WithMessageId_SendsItAsAttribute()
    {
        await _publisher.PublishToTopicAsync(new Payload("a"), messageId: "outbox-2");

        var request = _sns.ReceivedCalls()
            .Single(c => c.GetMethodInfo().Name == nameof(IAmazonSimpleNotificationService.PublishAsync)
                && c.GetArguments().OfType<SnsPublishRequest>().Any())
            .GetArguments().OfType<SnsPublishRequest>().Single();
        request.MessageAttributes["MessageId"].StringValue.ShouldBe("outbox-2");
    }

    [Fact]
    public async Task SendToFifoQueueAsync_WithMessageIdOnly_UsesItAsDeduplicationId()
    {
        await _publisher.SendToFifoQueueAsync(new Payload("a"), "group", messageId: "outbox-3");

        var request = SentSqsRequest();
        request.MessageDeduplicationId.ShouldBe("outbox-3");
        request.MessageAttributes["MessageId"].StringValue.ShouldBe("outbox-3");
    }

    [Fact]
    public async Task SendToFifoQueueAsync_WithExplicitDeduplicationId_KeepsItAndStillSendsTheAttribute()
    {
        await _publisher.SendToFifoQueueAsync(new Payload("a"), "group", deduplicationId: "dedup", messageId: "outbox-4");

        var request = SentSqsRequest();
        request.MessageDeduplicationId.ShouldBe("dedup");
        request.MessageAttributes["MessageId"].StringValue.ShouldBe("outbox-4");
    }

    [Fact]
    public async Task SendToFifoQueueAsync_WithoutAnyId_LeavesDeduplicationToTheQueue()
    {
        await _publisher.SendToFifoQueueAsync(new Payload("a"), "group");

        SentSqsRequest().MessageDeduplicationId.ShouldBeNull();
    }

    private sealed record Payload(string Value);
}
