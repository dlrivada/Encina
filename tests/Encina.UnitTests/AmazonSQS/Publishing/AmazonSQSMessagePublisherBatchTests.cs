using System.Net;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using Encina.AmazonSQS;
using Encina.Testing.Shouldly;
using Microsoft.Extensions.Logging.Testing;

namespace Encina.UnitTests.AmazonSQS.Publishing;

/// <summary>
/// Unit tests for the batch entries and the partial-failure handling of
/// <see cref="AmazonSQSMessagePublisher.SendBatchAsync{TMessage}"/>.
/// </summary>
public sealed class AmazonSQSMessagePublisherBatchTests
{
    private readonly IAmazonSQS _sqsClient = Substitute.For<IAmazonSQS>();
    private readonly FakeLogger<AmazonSQSMessagePublisher> _logger = new();
    private readonly AmazonSQSMessagePublisher _publisher;

    public AmazonSQSMessagePublisherBatchTests()
    {
        _publisher = new AmazonSQSMessagePublisher(
            _sqsClient,
            Substitute.For<IAmazonSimpleNotificationService>(),
            _logger,
            Options.Create(new EncinaAmazonSQSOptions
            {
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            }));
    }

    [Fact]
    public async Task SendBatchAsync_WhenSomeEntriesFail_ReturnsOnlyTheSuccessfulIdsAndLogsAWarning()
    {
        // Arrange
        SendMessageBatchRequest? sent = null;
        _sqsClient
            .SendMessageBatchAsync(Arg.Do<SendMessageBatchRequest>(r => sent = r), Arg.Any<CancellationToken>())
            .Returns(new SendMessageBatchResponse
            {
                Successful = [new SendMessageBatchResultEntry { Id = "0", MessageId = "msg-0" }],
                Failed = [new BatchResultErrorEntry { Id = "1", Code = "InternalError" }],
                HttpStatusCode = HttpStatusCode.OK
            });

        // Act
        var result = await _publisher.SendBatchAsync(new[] { new Payload("a"), new Payload("b") });

        // Assert
        var ids = result.ShouldBeSuccess();
        ids.ShouldBe(["msg-0"]);
        _logger.Collector.GetSnapshot().Any(r => r.Level == Microsoft.Extensions.Logging.LogLevel.Warning).ShouldBeTrue();
        sent.ShouldNotBeNull();
        sent!.Entries.Select(e => e.Id).ShouldBe(["0", "1"]);
        sent.Entries.ShouldAllBe(e => e.MessageAttributes["MessageType"].StringValue == typeof(Payload).FullName);
    }

    private sealed record Payload(string Text);
}
