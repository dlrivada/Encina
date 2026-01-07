using System.Net;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Encina.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SqsSendMessageRequest = Amazon.SQS.Model.SendMessageRequest;
using SqsSendMessageResponse = Amazon.SQS.Model.SendMessageResponse;
using SnsSendMessageResponse = Amazon.SimpleNotificationService.Model.PublishResponse;

using static System.Globalization.CultureInfo;

namespace Encina.AmazonSQS.Tests.Publishing;

/// <summary>
/// Unit tests for <see cref="AmazonSQSMessagePublisher"/> using EitherAssertions.
/// </summary>
public sealed class AmazonSQSMessagePublisherTests
{
    private static readonly string[] BatchMessageIds = ["msg-1", "msg-2", "msg-3"];

    private readonly IAmazonSQS _sqsClient;
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly ILogger<AmazonSQSMessagePublisher> _logger;
    private readonly IOptions<EncinaAmazonSQSOptions> _options;

    public AmazonSQSMessagePublisherTests()
    {
        _sqsClient = Substitute.For<IAmazonSQS>();
        _snsClient = Substitute.For<IAmazonSimpleNotificationService>();
        _logger = Substitute.For<ILogger<AmazonSQSMessagePublisher>>();
        // Enable logging for coverage of LoggerMessage generated code
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _options = Options.Create(new EncinaAmazonSQSOptions
        {
            DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue",
            DefaultTopicArn = "arn:aws:sns:us-east-1:123456789012:test-topic"
        });
    }

    #region Constructor Guard Clause Tests

    [Fact]
    public void Constructor_WithNullSqsClient_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new AmazonSQSMessagePublisher(null!, _snsClient, _logger, _options));
        ex.ParamName.ShouldBe("sqsClient");
    }

    [Fact]
    public void Constructor_WithNullSnsClient_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new AmazonSQSMessagePublisher(_sqsClient, null!, _logger, _options));
        ex.ParamName.ShouldBe("snsClient");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new AmazonSQSMessagePublisher(_sqsClient, _snsClient, null!, _options));
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new AmazonSQSMessagePublisher(_sqsClient, _snsClient, _logger, null!));
        ex.ParamName.ShouldBe("options");
    }

    #endregion

    #region SendToQueueAsync Tests

    [Fact]
    public async Task SendToQueueAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.SendToQueueAsync<TestMessage>(null!).AsTask());
    }

    [Fact]
    public async Task SendToQueueAsync_WithValidMessage_ShouldBeSuccess()
    {
        // Arrange
        ConfigureSqsClientSuccess("msg-12345");
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.SendToQueueAsync(message);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task SendToQueueAsync_WithValidMessage_ShouldReturnMessageId()
    {
        // Arrange
        var expectedMessageId = "msg-12345";
        ConfigureSqsClientSuccess(expectedMessageId);
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.SendToQueueAsync(message);

        // Assert
        result.ShouldBeSuccessAnd()
            .ShouldSatisfy(messageId => messageId.ShouldBe(expectedMessageId));
    }

    [Fact]
    public async Task SendToQueueAsync_WithCustomQueueUrl_ShouldUseCustomQueue()
    {
        // Arrange
        ConfigureSqsClientSuccess("msg-12345");
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var customQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/custom-queue";

        // Act
        var result = await publisher.SendToQueueAsync(message, customQueueUrl);

        // Assert
        result.ShouldBeSuccess();
        await _sqsClient.Received(1).SendMessageAsync(
            Arg.Is<SqsSendMessageRequest>(r => r.QueueUrl == customQueueUrl),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendToQueueAsync_WhenQueueUrlNotConfigured_ShouldReturnError()
    {
        // Arrange
        var optionsWithNoQueue = CreateOptionsWithoutQueue();
        var publisher = new AmazonSQSMessagePublisher(_sqsClient, _snsClient, _logger, optionsWithNoQueue);
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.SendToQueueAsync(message);

        // Assert
        result.ShouldBeErrorWithCode("SQS_QUEUE_NOT_CONFIGURED");
    }

    [Fact]
    public async Task SendToQueueAsync_WhenSqsClientThrowsException_ShouldReturnErrorWithDetails()
    {
        // Arrange
        ConfigureSqsClientToThrow(new InvalidOperationException("Connection lost"));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.SendToQueueAsync(message);

        // Assert
        result.ShouldBeError();
        result.ShouldBeErrorWithCode("SQS_SEND_FAILED");
        result.ShouldBeErrorContaining("Failed to send message");
    }

    #endregion

    #region PublishToTopicAsync Tests

    [Fact]
    public async Task PublishToTopicAsync_WithValidMessage_ShouldBeSuccess()
    {
        // Arrange
        ConfigureSnsClientSuccess("msg-67890");
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishToTopicAsync(message);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task PublishToTopicAsync_WithValidMessage_ShouldReturnMessageId()
    {
        // Arrange
        var expectedMessageId = "msg-67890";
        ConfigureSnsClientSuccess(expectedMessageId);
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishToTopicAsync(message);

        // Assert
        result.ShouldBeSuccessAnd()
            .ShouldSatisfy(messageId => messageId.ShouldBe(expectedMessageId));
    }

    [Fact]
    public async Task PublishToTopicAsync_WithCustomTopicArn_ShouldUseCustomTopic()
    {
        // Arrange
        ConfigureSnsClientSuccess("msg-67890");
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var customTopicArn = "arn:aws:sns:us-east-1:123456789012:custom-topic";

        // Act
        var result = await publisher.PublishToTopicAsync(message, customTopicArn);

        // Assert
        result.ShouldBeSuccess();
        await _snsClient.Received(1).PublishAsync(
            Arg.Is<PublishRequest>(r => r.TopicArn == customTopicArn),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishToTopicAsync_WhenTopicArnNotConfigured_ShouldReturnError()
    {
        // Arrange
        var optionsWithNoTopic = CreateOptionsWithoutTopic();
        var publisher = new AmazonSQSMessagePublisher(_sqsClient, _snsClient, _logger, optionsWithNoTopic);
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishToTopicAsync(message);

        // Assert
        result.ShouldBeErrorWithCode("SNS_TOPIC_NOT_CONFIGURED");
    }

    [Fact]
    public async Task PublishToTopicAsync_WhenSnsClientThrowsException_ShouldReturnErrorWithCode()
    {
        // Arrange
        ConfigureSnsClientToThrow(new AmazonSimpleNotificationServiceException("Topic not found"));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishToTopicAsync(message);

        // Assert
        result.ShouldBeErrorWithCode("SNS_PUBLISH_FAILED");
    }

    #endregion

    #region SendBatchAsync Tests

    [Fact]
    public async Task SendBatchAsync_WithValidMessages_ShouldBeSuccess()
    {
        // Arrange
        ConfigureSqsBatchClientSuccess(BatchMessageIds);
        var publisher = CreatePublisher();
        var messages = new TestMessage[]
        {
            new() { Id = 1, Content = "Test1" },
            new() { Id = 2, Content = "Test2" },
            new() { Id = 3, Content = "Test3" }
        };

        // Act
        var result = await publisher.SendBatchAsync(messages);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task SendBatchAsync_WithValidMessages_ShouldReturnAllMessageIds()
    {
        // Arrange
        ConfigureSqsBatchClientSuccess(BatchMessageIds);
        var publisher = CreatePublisher();
        var messages = new TestMessage[]
        {
            new() { Id = 1, Content = "Test1" },
            new() { Id = 2, Content = "Test2" },
            new() { Id = 3, Content = "Test3" }
        };

        // Act
        var result = await publisher.SendBatchAsync(messages);

        // Assert
        result.ShouldBeSuccessAnd()
            .ShouldSatisfy(ids => ids.Count.ShouldBe(3));
    }

    [Fact]
    public async Task SendBatchAsync_WhenQueueUrlNotConfigured_ShouldReturnError()
    {
        // Arrange
        var optionsWithNoQueue = CreateOptionsWithoutQueue();
        var publisher = new AmazonSQSMessagePublisher(_sqsClient, _snsClient, _logger, optionsWithNoQueue);
        TestMessage[] messages = [new() { Id = 1, Content = "Test" }];

        // Act
        var result = await publisher.SendBatchAsync(messages);

        // Assert
        result.ShouldBeErrorWithCode("SQS_QUEUE_NOT_CONFIGURED");
    }

    [Fact]
    public async Task SendBatchAsync_WhenSqsClientThrowsException_ShouldReturnErrorWithCode()
    {
        // Arrange
        ConfigureSqsBatchClientToThrow(new AmazonSQSException("Batch failed"));
        var publisher = CreatePublisher();
        TestMessage[] messages = [new() { Id = 1, Content = "Test" }];

        // Act
        var result = await publisher.SendBatchAsync(messages);

        // Assert
        result.ShouldBeErrorWithCode("SQS_BATCH_SEND_FAILED");
    }

    #endregion

    #region SendToFifoQueueAsync Tests

    [Fact]
    public async Task SendToFifoQueueAsync_WithValidMessage_ShouldBeSuccess()
    {
        // Arrange
        ConfigureSqsClientSuccess("msg-fifo-12345");
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var messageGroupId = "group-1";

        // Act
        var result = await publisher.SendToFifoQueueAsync(message, messageGroupId);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task SendToFifoQueueAsync_WithValidMessage_ShouldReturnMessageId()
    {
        // Arrange
        var expectedMessageId = "msg-fifo-12345";
        ConfigureSqsClientSuccess(expectedMessageId);
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var messageGroupId = "group-1";

        // Act
        var result = await publisher.SendToFifoQueueAsync(message, messageGroupId);

        // Assert
        result.ShouldBeSuccessAnd()
            .ShouldSatisfy(messageId => messageId.ShouldBe(expectedMessageId));
    }

    [Fact]
    public async Task SendToFifoQueueAsync_WithDeduplicationId_ShouldSendWithDeduplication()
    {
        // Arrange
        ConfigureSqsClientSuccess("msg-fifo-12345");
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var messageGroupId = "group-1";
        var deduplicationId = "dedup-123";

        // Act
        var result = await publisher.SendToFifoQueueAsync(message, messageGroupId, deduplicationId);

        // Assert
        result.ShouldBeSuccess();
        await _sqsClient.Received(1).SendMessageAsync(
            Arg.Is<SqsSendMessageRequest>(r =>
                r.MessageGroupId == messageGroupId &&
                r.MessageDeduplicationId == deduplicationId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendToFifoQueueAsync_WhenQueueUrlNotConfigured_ShouldReturnError()
    {
        // Arrange
        var optionsWithNoQueue = CreateOptionsWithoutQueue();
        var publisher = new AmazonSQSMessagePublisher(_sqsClient, _snsClient, _logger, optionsWithNoQueue);
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.SendToFifoQueueAsync(message, "group-1");

        // Assert
        result.ShouldBeErrorWithCode("SQS_QUEUE_NOT_CONFIGURED");
    }

    [Fact]
    public async Task SendToFifoQueueAsync_WhenSqsClientThrowsException_ShouldReturnErrorWithCode()
    {
        // Arrange
        ConfigureSqsClientToThrow(new AmazonSQSException("FIFO queue error"));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.SendToFifoQueueAsync(message, "group-1");

        // Assert
        result.ShouldBeErrorWithCode("SQS_FIFO_SEND_FAILED");
    }

    #endregion

    #region Helper Methods

    private AmazonSQSMessagePublisher CreatePublisher()
    {
        return new AmazonSQSMessagePublisher(_sqsClient, _snsClient, _logger, _options);
    }

    private static IOptions<EncinaAmazonSQSOptions> CreateOptionsWithoutQueue()
    {
        return Options.Create(new EncinaAmazonSQSOptions
        {
            DefaultQueueUrl = null,
            DefaultTopicArn = "arn:aws:sns:us-east-1:123456789012:test-topic"
        });
    }

    private static IOptions<EncinaAmazonSQSOptions> CreateOptionsWithoutTopic()
    {
        return Options.Create(new EncinaAmazonSQSOptions
        {
            DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue",
            DefaultTopicArn = null
        });
    }

    private void ConfigureSqsClientSuccess(string messageId)
    {
        _sqsClient
            .SendMessageAsync(Arg.Any<SqsSendMessageRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SqsSendMessageResponse
            {
                MessageId = messageId,
                HttpStatusCode = HttpStatusCode.OK
            });
    }

    private void ConfigureSqsClientToThrow(Exception exception)
    {
        _sqsClient
            .SendMessageAsync(Arg.Any<SqsSendMessageRequest>(), Arg.Any<CancellationToken>())
            .Returns<SqsSendMessageResponse>(_ => throw exception);
    }

    private void ConfigureSnsClientSuccess(string messageId)
    {
        _snsClient
            .PublishAsync(Arg.Any<PublishRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SnsSendMessageResponse
            {
                MessageId = messageId,
                HttpStatusCode = HttpStatusCode.OK
            });
    }

    private void ConfigureSnsClientToThrow(Exception exception)
    {
        _snsClient
            .PublishAsync(Arg.Any<PublishRequest>(), Arg.Any<CancellationToken>())
            .Returns<SnsSendMessageResponse>(_ => throw exception);
    }

    private void ConfigureSqsBatchClientSuccess(string[] messageIds)
    {
        _sqsClient
            .SendMessageBatchAsync(Arg.Any<SendMessageBatchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SendMessageBatchResponse
            {
                Successful = messageIds.Select((id, i) => new SendMessageBatchResultEntry
                {
                    Id = i.ToString(InvariantCulture),
                    MessageId = id
                }).ToList(),
                Failed = [],
                HttpStatusCode = HttpStatusCode.OK
            });
    }

    private void ConfigureSqsBatchClientToThrow(Exception exception)
    {
        _sqsClient
            .SendMessageBatchAsync(Arg.Any<SendMessageBatchRequest>(), Arg.Any<CancellationToken>())
            .Returns<SendMessageBatchResponse>(_ => throw exception);
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
