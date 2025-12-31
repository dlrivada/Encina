using System.Text.Json.Serialization;
using Amazon.Lambda.SQSEvents;
using Shouldly;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Encina.AwsLambda.Tests;

public class SqsMessageHandlerTests
{
    private readonly ILogger _logger;

    public SqsMessageHandlerTests()
    {
        _logger = Substitute.For<ILogger>();
    }

    [Fact]
    public async Task ProcessBatchAsync_WithAllSuccess_ReturnsEmptyFailures()
    {
        // Arrange
        var sqsEvent = CreateSqsEvent(
            CreateMessage("msg-1", "{\"id\":1}"),
            CreateMessage("msg-2", "{\"id\":2}"));

        // Act
        var result = await SqsMessageHandler.ProcessBatchAsync(
            sqsEvent,
            _ => Task.FromResult(Either<EncinaError, int>.Right(1)),
            _logger);

        // Assert
        result.BatchItemFailures.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProcessBatchAsync_WithSomeFailures_ReturnsFailedMessageIds()
    {
        // Arrange
        var sqsEvent = CreateSqsEvent(
            CreateMessage("msg-1", "{\"id\":1}"),
            CreateMessage("msg-2", "{\"id\":2}"),
            CreateMessage("msg-3", "{\"id\":3}"));

        var callCount = 0;

        // Act
        var result = await SqsMessageHandler.ProcessBatchAsync(
            sqsEvent,
            _ =>
            {
                callCount++;
                return callCount == 2
                    ? Task.FromResult(Either<EncinaError, int>.Left(EncinaErrors.Create("error", "Failed")))
                    : Task.FromResult(Either<EncinaError, int>.Right(1));
            },
            _logger);

        // Assert
        var failure = result.BatchItemFailures.ShouldHaveSingleItem();
        failure.ItemIdentifier.ShouldBe("msg-2");
    }

    [Fact]
    public async Task ProcessBatchAsync_WithException_ReturnsFailedMessageId()
    {
        // Arrange
        var sqsEvent = CreateSqsEvent(CreateMessage("msg-1", "{\"id\":1}"));

        // Act
        var result = await SqsMessageHandler.ProcessBatchAsync<int>(
            sqsEvent,
            _ => throw new InvalidOperationException("Test exception"),
            _logger);

        // Assert
        var failure = result.BatchItemFailures.ShouldHaveSingleItem();
        failure.ItemIdentifier.ShouldBe("msg-1");
    }

    [Fact]
    public async Task ProcessBatchAsync_WithDeserialization_DeserializesAndProcesses()
    {
        // Arrange
        var sqsEvent = CreateSqsEvent(
            CreateMessage("msg-1", "{\"value\":42}"));

        // Act
        var result = await SqsMessageHandler.ProcessBatchAsync<TestMessage, int>(
            sqsEvent,
            msg => Task.FromResult(Either<EncinaError, int>.Right(msg.Value)),
            _logger);

        // Assert
        result.BatchItemFailures.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProcessBatchAsync_WithInvalidJson_ReturnsFailure()
    {
        // Arrange
        var sqsEvent = CreateSqsEvent(CreateMessage("msg-1", "invalid json"));

        // Act
        var result = await SqsMessageHandler.ProcessBatchAsync<TestMessage, int>(
            sqsEvent,
            msg => Task.FromResult(Either<EncinaError, int>.Right(msg.Value)),
            _logger);

        // Assert
        var failure = result.BatchItemFailures.ShouldHaveSingleItem();
        failure.ItemIdentifier.ShouldBe("msg-1");
    }

    [Fact]
    public async Task ProcessAllAsync_WithAllSuccess_ReturnsUnit()
    {
        // Arrange
        var sqsEvent = CreateSqsEvent(
            CreateMessage("msg-1", "{\"id\":1}"),
            CreateMessage("msg-2", "{\"id\":2}"));

        // Act
        var result = await SqsMessageHandler.ProcessAllAsync(
            sqsEvent,
            _ => Task.FromResult(Either<EncinaError, int>.Right(1)));

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessAllAsync_WithError_StopsAndReturnsError()
    {
        // Arrange
        var sqsEvent = CreateSqsEvent(
            CreateMessage("msg-1", "{\"id\":1}"),
            CreateMessage("msg-2", "{\"id\":2}"));

        var processedCount = 0;

        // Act
        var result = await SqsMessageHandler.ProcessAllAsync(
            sqsEvent,
            _ =>
            {
                processedCount++;
                return Task.FromResult(Either<EncinaError, int>.Left(EncinaErrors.Create("error", "Failed")));
            });

        // Assert
        result.IsLeft.ShouldBeTrue();
        processedCount.ShouldBe(1); // Should stop after first failure
    }

    [Fact]
    public void DeserializeMessage_WithValidJson_ReturnsDeserializedMessage()
    {
        // Arrange
        var record = CreateMessage("msg-1", "{\"value\":42}");

        // Act
        var result = SqsMessageHandler.DeserializeMessage<TestMessage>(record);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(msg => msg.Value.ShouldBe(42));
    }

    [Fact]
    public void DeserializeMessage_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var record = CreateMessage("msg-1", "invalid json");

        // Act
        var result = SqsMessageHandler.DeserializeMessage<TestMessage>(record);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfSome(code => code.ShouldBe("sqs.deserialization_failed")));
    }

    [Fact]
    public void DeserializeMessage_WithNullRecord_ThrowsArgumentNullException()
    {
        // Act
        var action = () => SqsMessageHandler.DeserializeMessage<TestMessage>(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("record");
    }

    [Fact]
    public async Task ProcessBatchAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> action = () => SqsMessageHandler.ProcessBatchAsync<int>(
            null!,
            _ => Task.FromResult(Either<EncinaError, int>.Right(1)));

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("sqsEvent");
    }

    [Fact]
    public async Task ProcessBatchAsync_WithNullProcessor_ThrowsArgumentNullException()
    {
        // Arrange
        var sqsEvent = CreateSqsEvent();

        // Act
        Func<Task> action = () => SqsMessageHandler.ProcessBatchAsync<int>(
            sqsEvent,
            null!);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("processMessage");
    }

    private static SQSEvent CreateSqsEvent(params SQSEvent.SQSMessage[] messages)
    {
        return new SQSEvent { Records = [.. messages] };
    }

    private static SQSEvent.SQSMessage CreateMessage(string messageId, string body)
    {
        return new SQSEvent.SQSMessage
        {
            MessageId = messageId,
            Body = body
        };
    }

    private sealed class TestMessage
    {
        [JsonPropertyName("value")]
        public int Value { get; set; }
    }
}
