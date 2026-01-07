using Encina.Testing;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.AmazonSQS.Tests.Integration;

/// <summary>
/// Integration tests for <see cref="AmazonSQSMessagePublisher"/> using LocalStack.
/// </summary>
[Collection(LocalStackCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AmazonSQSMessagePublisherIntegrationTests : IAsyncLifetime
{
    private readonly LocalStackFixture _fixture;
    private readonly ILogger<AmazonSQSMessagePublisher> _logger;
    private string _queueUrl = string.Empty;
    private string _topicArn = string.Empty;

    public AmazonSQSMessagePublisherIntegrationTests(LocalStackFixture fixture)
    {
        _fixture = fixture;
        _logger = Substitute.For<ILogger<AmazonSQSMessagePublisher>>();
    }

    public async Task InitializeAsync()
    {
        if (_fixture.IsAvailable)
        {
            _queueUrl = await _fixture.CreateQueueAsync("test-queue");
            _topicArn = await _fixture.CreateTopicAsync("test-topic");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    #region SendToQueueAsync Integration Tests

    [SkippableFact]
    public async Task SendToQueueAsync_WithRealLocalStack_ShouldSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "LocalStack is not available (Docker may not be running)");

        // Arrange
        var sqsClient = _fixture.CreateSqsClient();
        var snsClient = _fixture.CreateSnsClient();
        var options = Options.Create(new EncinaAmazonSQSOptions
        {
            DefaultQueueUrl = _queueUrl,
            DefaultTopicArn = _topicArn
        });
        var publisher = new AmazonSQSMessagePublisher(sqsClient, snsClient, _logger, options);
        var message = new TestMessage { Id = 1, Content = "Integration Test" };

        // Act
        var result = await publisher.SendToQueueAsync(message);

        // Assert
        result.ShouldBeSuccess();
    }

    [SkippableFact]
    public async Task SendToQueueAsync_WithRealLocalStack_ShouldReturnMessageId()
    {
        Skip.IfNot(_fixture.IsAvailable, "LocalStack is not available (Docker may not be running)");

        // Arrange
        var sqsClient = _fixture.CreateSqsClient();
        var snsClient = _fixture.CreateSnsClient();
        var options = Options.Create(new EncinaAmazonSQSOptions
        {
            DefaultQueueUrl = _queueUrl,
            DefaultTopicArn = _topicArn
        });
        var publisher = new AmazonSQSMessagePublisher(sqsClient, snsClient, _logger, options);
        var message = new TestMessage { Id = 2, Content = "Integration Test with ID" };

        // Act
        var result = await publisher.SendToQueueAsync(message);

        // Assert
        result.ShouldBeSuccessAnd()
            .ShouldSatisfy(messageId =>
            {
                messageId.ShouldNotBeNullOrEmpty();
            });
    }

    [SkippableFact]
    public async Task SendToQueueAsync_MessageShouldBeReceivable()
    {
        Skip.IfNot(_fixture.IsAvailable, "LocalStack is not available (Docker may not be running)");

        // Arrange
        var sqsClient = _fixture.CreateSqsClient();
        var snsClient = _fixture.CreateSnsClient();
        var options = Options.Create(new EncinaAmazonSQSOptions
        {
            DefaultQueueUrl = _queueUrl,
            DefaultTopicArn = _topicArn
        });
        var publisher = new AmazonSQSMessagePublisher(sqsClient, snsClient, _logger, options);
        var message = new TestMessage { Id = 3, Content = "Receivable Message" };

        // Act
        var publishResult = await publisher.SendToQueueAsync(message);

        // Assert - Message was published successfully
        publishResult.ShouldBeSuccess();

        // Verify message can be received
        var receiveResult = await sqsClient.ReceiveMessageAsync(_queueUrl);
        receiveResult.Messages.ShouldNotBeEmpty();
        receiveResult.Messages[0].Body.ShouldContain("Receivable Message");
    }

    #endregion

    #region PublishToTopicAsync Integration Tests

    [SkippableFact]
    public async Task PublishToTopicAsync_WithRealLocalStack_ShouldSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "LocalStack is not available (Docker may not be running)");

        // Arrange
        var sqsClient = _fixture.CreateSqsClient();
        var snsClient = _fixture.CreateSnsClient();
        var options = Options.Create(new EncinaAmazonSQSOptions
        {
            DefaultQueueUrl = _queueUrl,
            DefaultTopicArn = _topicArn
        });
        var publisher = new AmazonSQSMessagePublisher(sqsClient, snsClient, _logger, options);
        var message = new TestMessage { Id = 4, Content = "Topic Message" };

        // Act
        var result = await publisher.PublishToTopicAsync(message);

        // Assert
        result.ShouldBeSuccess();
    }

    [SkippableFact]
    public async Task PublishToTopicAsync_WithRealLocalStack_ShouldReturnMessageId()
    {
        Skip.IfNot(_fixture.IsAvailable, "LocalStack is not available (Docker may not be running)");

        // Arrange
        var sqsClient = _fixture.CreateSqsClient();
        var snsClient = _fixture.CreateSnsClient();
        var options = Options.Create(new EncinaAmazonSQSOptions
        {
            DefaultQueueUrl = _queueUrl,
            DefaultTopicArn = _topicArn
        });
        var publisher = new AmazonSQSMessagePublisher(sqsClient, snsClient, _logger, options);
        var message = new TestMessage { Id = 5, Content = "Topic Message with ID" };

        // Act
        var result = await publisher.PublishToTopicAsync(message);

        // Assert
        result.ShouldBeSuccessAnd()
            .ShouldSatisfy(messageId =>
            {
                messageId.ShouldNotBeNullOrEmpty();
            });
    }

    #endregion

    #region SendBatchAsync Integration Tests

    [SkippableFact]
    public async Task SendBatchAsync_WithRealLocalStack_ShouldSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "LocalStack is not available (Docker may not be running)");

        // Arrange
        var sqsClient = _fixture.CreateSqsClient();
        var snsClient = _fixture.CreateSnsClient();
        var options = Options.Create(new EncinaAmazonSQSOptions
        {
            DefaultQueueUrl = _queueUrl,
            DefaultTopicArn = _topicArn
        });
        var publisher = new AmazonSQSMessagePublisher(sqsClient, snsClient, _logger, options);
        var messages = new[]
        {
            new TestMessage { Id = 10, Content = "Batch 1" },
            new TestMessage { Id = 11, Content = "Batch 2" },
            new TestMessage { Id = 12, Content = "Batch 3" }
        };

        // Act
        var result = await publisher.SendBatchAsync(messages);

        // Assert
        result.ShouldBeSuccess();
    }

    [SkippableFact]
    public async Task SendBatchAsync_WithRealLocalStack_ShouldReturnAllMessageIds()
    {
        Skip.IfNot(_fixture.IsAvailable, "LocalStack is not available (Docker may not be running)");

        // Arrange
        var sqsClient = _fixture.CreateSqsClient();
        var snsClient = _fixture.CreateSnsClient();
        var options = Options.Create(new EncinaAmazonSQSOptions
        {
            DefaultQueueUrl = _queueUrl,
            DefaultTopicArn = _topicArn
        });
        var publisher = new AmazonSQSMessagePublisher(sqsClient, snsClient, _logger, options);
        var messages = new[]
        {
            new TestMessage { Id = 20, Content = "Batch A" },
            new TestMessage { Id = 21, Content = "Batch B" },
            new TestMessage { Id = 22, Content = "Batch C" }
        };

        // Act
        var result = await publisher.SendBatchAsync(messages);

        // Assert
        result.ShouldBeSuccessAnd()
            .ShouldSatisfy(messageIds =>
            {
                messageIds.Count.ShouldBe(3);
            });
    }

    #endregion

    #region FIFO Queue Tests

    [SkippableFact]
    public async Task SendToFifoQueueAsync_WithRealLocalStack_ShouldSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "LocalStack is not available (Docker may not be running)");

        // Arrange
        var fifoQueueUrl = await _fixture.CreateQueueAsync("test-fifo-queue", isFifo: true);

        var sqsClient = _fixture.CreateSqsClient();
        var snsClient = _fixture.CreateSnsClient();
        var options = Options.Create(new EncinaAmazonSQSOptions
        {
            DefaultQueueUrl = fifoQueueUrl,
            DefaultTopicArn = _topicArn
        });
        var publisher = new AmazonSQSMessagePublisher(sqsClient, snsClient, _logger, options);
        var message = new TestMessage { Id = 100, Content = "FIFO Message" };

        // Act
        var result = await publisher.SendToFifoQueueAsync(message, messageGroupId: "group-1");

        // Assert
        result.ShouldBeSuccess();
    }

    [SkippableFact]
    public async Task SendToFifoQueueAsync_WithDeduplicationId_ShouldSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "LocalStack is not available (Docker may not be running)");

        // Arrange
        var fifoQueueUrl = await _fixture.CreateQueueAsync("test-fifo-dedup-queue", isFifo: true);

        var sqsClient = _fixture.CreateSqsClient();
        var snsClient = _fixture.CreateSnsClient();
        var options = Options.Create(new EncinaAmazonSQSOptions
        {
            DefaultQueueUrl = fifoQueueUrl,
            DefaultTopicArn = _topicArn
        });
        var publisher = new AmazonSQSMessagePublisher(sqsClient, snsClient, _logger, options);
        var message = new TestMessage { Id = 101, Content = "FIFO Dedup Message" };

        // Act
        var result = await publisher.SendToFifoQueueAsync(
            message,
            messageGroupId: "group-1",
            deduplicationId: "unique-id-123");

        // Assert
        result.ShouldBeSuccess();
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
