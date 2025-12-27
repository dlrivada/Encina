using System.Text.Json.Serialization;
using Amazon.Lambda.SQSEvents;
using FluentAssertions;
using LanguageExt;
using Xunit;

namespace Encina.AwsLambda.ContractTests;

/// <summary>
/// Contract tests to verify SQS message handler behavior contracts.
/// </summary>
public class SqsHandlerContractTests
{
    [Fact]
    public async Task ProcessBatchAsync_AllSuccess_ReturnsEmptyBatchItemFailures()
    {
        // Arrange
        var sqsEvent = CreateSqsEvent(
            CreateMessage("msg-1", "{}"),
            CreateMessage("msg-2", "{}"),
            CreateMessage("msg-3", "{}"));

        // Act
        var result = await SqsMessageHandler.ProcessBatchAsync(
            sqsEvent,
            _ => Task.FromResult(Either<EncinaError, int>.Right(1)));

        // Assert - Contract: All success = empty failures list
        result.BatchItemFailures.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessBatchAsync_SomeFailures_ReturnsOnlyFailedMessageIds()
    {
        // Arrange
        var sqsEvent = CreateSqsEvent(
            CreateMessage("msg-1", "{}"),
            CreateMessage("msg-2", "{}"),
            CreateMessage("msg-3", "{}"));

        var processedIds = new List<string>();

        // Act
        var result = await SqsMessageHandler.ProcessBatchAsync(
            sqsEvent,
            record =>
            {
                processedIds.Add(record.MessageId);
                return record.MessageId == "msg-2"
                    ? Task.FromResult(Either<EncinaError, int>.Left(EncinaErrors.Create("error", "Failed")))
                    : Task.FromResult(Either<EncinaError, int>.Right(1));
            });

        // Assert - Contract: Only failed message IDs are returned
        result.BatchItemFailures.Should().HaveCount(1);
        result.BatchItemFailures.First().ItemIdentifier.Should().Be("msg-2");

        // Contract: All messages are processed (not stopped at first failure)
        processedIds.Should().HaveCount(3);
    }

    [Fact]
    public async Task ProcessBatchAsync_AllFailures_ReturnsAllMessageIds()
    {
        // Arrange
        var sqsEvent = CreateSqsEvent(
            CreateMessage("msg-1", "{}"),
            CreateMessage("msg-2", "{}"));

        // Act
        var result = await SqsMessageHandler.ProcessBatchAsync(
            sqsEvent,
            _ => Task.FromResult(Either<EncinaError, int>.Left(EncinaErrors.Create("error", "Failed"))));

        // Assert - Contract: All failed = all message IDs returned
        result.BatchItemFailures.Should().HaveCount(2);
        result.BatchItemFailures.Select(f => f.ItemIdentifier)
            .Should().BeEquivalentTo(["msg-1", "msg-2"]);
    }

    [Fact]
    public async Task ProcessAllAsync_FirstError_StopsProcessing()
    {
        // Arrange
        var sqsEvent = CreateSqsEvent(
            CreateMessage("msg-1", "{}"),
            CreateMessage("msg-2", "{}"),
            CreateMessage("msg-3", "{}"));

        var processedCount = 0;

        // Act
        var result = await SqsMessageHandler.ProcessAllAsync(
            sqsEvent,
            _ =>
            {
                processedCount++;
                return Task.FromResult(Either<EncinaError, int>.Left(EncinaErrors.Create("error", "Failed")));
            });

        // Assert - Contract: ProcessAllAsync stops at first error
        result.IsLeft.Should().BeTrue();
        processedCount.Should().Be(1);
    }

    [Fact]
    public async Task ProcessAllAsync_AllSuccess_ReturnsUnitAndProcessesAll()
    {
        // Arrange
        var sqsEvent = CreateSqsEvent(
            CreateMessage("msg-1", "{}"),
            CreateMessage("msg-2", "{}"),
            CreateMessage("msg-3", "{}"));

        var processedCount = 0;

        // Act
        var result = await SqsMessageHandler.ProcessAllAsync(
            sqsEvent,
            _ =>
            {
                processedCount++;
                return Task.FromResult(Either<EncinaError, int>.Right(1));
            });

        // Assert - Contract: All success = Unit returned, all processed
        result.IsRight.Should().BeTrue();
        processedCount.Should().Be(3);
    }

    [Fact]
    public async Task ProcessBatchAsync_WithDeserialization_DeserializesEachMessage()
    {
        // Arrange
        var sqsEvent = CreateSqsEvent(
            CreateMessage("msg-1", "{\"value\":10}"),
            CreateMessage("msg-2", "{\"value\":20}"),
            CreateMessage("msg-3", "{\"value\":30}"));

        var receivedValues = new List<int>();

        // Act
        await SqsMessageHandler.ProcessBatchAsync<TestMessage, int>(
            sqsEvent,
            msg =>
            {
                receivedValues.Add(msg.Value);
                return Task.FromResult(Either<EncinaError, int>.Right(msg.Value));
            });

        // Assert - Contract: Each message is deserialized correctly
        receivedValues.Should().BeEquivalentTo([10, 20, 30]);
    }

    [Fact]
    public void DeserializeMessage_ValidJson_ReturnsRight()
    {
        // Arrange
        var record = CreateMessage("msg-1", "{\"value\":42}");

        // Act
        var result = SqsMessageHandler.DeserializeMessage<TestMessage>(record);

        // Assert - Contract: Valid JSON = Right with deserialized object
        result.IsRight.Should().BeTrue();
        result.IfRight(msg => msg.Value.Should().Be(42));
    }

    [Fact]
    public void DeserializeMessage_InvalidJson_ReturnsLeftWithDeserializationError()
    {
        // Arrange
        var record = CreateMessage("msg-1", "not valid json");

        // Act
        var result = SqsMessageHandler.DeserializeMessage<TestMessage>(record);

        // Assert - Contract: Invalid JSON = Left with specific error code
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.GetCode().IfSome(code => code.Should().Be("sqs.deserialization_failed")));
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
