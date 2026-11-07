using System.Text.Json;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.SQSEvents;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Encina.GuardTests.Cloud.AwsLambda;

/// <summary>
/// Guard tests for <see cref="SqsMessageHandler"/> and <see cref="EventBridgeHandler"/>:
/// invalid events, malformed bodies, throwing delegates and cancelled tokens.
/// </summary>
public sealed class MessageHandlersInvalidInputGuardTests
{
    private sealed record Msg(int Value);

    private static SQSEvent SqsEvent(params string?[] bodies) => new()
    {
        Records = [.. bodies.Select((b, i) => new SQSEvent.SQSMessage { MessageId = $"m{i}", Body = b! })]
    };

    private static Task<Either<EncinaError, int>> Ok<T>(T _) => Task.FromResult<Either<EncinaError, int>>(1);

    private static Task<Either<EncinaError, int>> Fail<T>(T _) =>
        Task.FromResult<Either<EncinaError, int>>(EncinaErrors.Create("x.failed", "m"));

    #region SqsMessageHandler

    [Fact]
    public async Task ProcessBatchAsync_TypedOverload_NullEvent_Throws()
    {
        var action = () => SqsMessageHandler.ProcessBatchAsync<Msg, int>(null!, Ok);

        (await Should.ThrowAsync<ArgumentNullException>(action)).ParamName.ShouldBe("sqsEvent");
    }

    [Fact]
    public async Task ProcessBatchAsync_TypedOverload_NullProcessor_Throws()
    {
        var action = () => SqsMessageHandler.ProcessBatchAsync<Msg, int>(
            SqsEvent("{}"),
            (Func<Msg, Task<Either<EncinaError, int>>>)null!);

        (await Should.ThrowAsync<ArgumentNullException>(action)).ParamName.ShouldBe("processMessage");
    }

    [Fact]
    public async Task ProcessBatchAsync_NullRecords_ThrowsNullReference()
    {
        var action = () => SqsMessageHandler.ProcessBatchAsync<int>(new SQSEvent { Records = null! }, m => Ok(m));

        await Should.ThrowAsync<NullReferenceException>(action);
    }

    [Fact]
    public async Task ProcessBatchAsync_EmptyRecords_ReturnsNoFailures()
    {
        var response = await SqsMessageHandler.ProcessBatchAsync<int>(SqsEvent(), m => Ok(m));

        response.BatchItemFailures.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProcessBatchAsync_CancelledToken_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var action = () => SqsMessageHandler.ProcessBatchAsync<int>(
            SqsEvent("{}"), m => Ok(m), NullLogger.Instance, cts.Token);

        await Should.ThrowAsync<OperationCanceledException>(action);
    }

    [Fact]
    public async Task ProcessBatchAsync_ThrowingProcessor_ReportsItemAsFailure()
    {
        var response = await SqsMessageHandler.ProcessBatchAsync<int>(
            SqsEvent("{}"),
            (SQSEvent.SQSMessage _) => throw new InvalidOperationException("boom"),
            NullLogger.Instance);

        response.BatchItemFailures.Single().ItemIdentifier.ShouldBe("m0");
    }

    [Fact]
    public async Task ProcessBatchAsync_LeftResult_ReportsItemAsFailureAndKeepsGoing()
    {
        var calls = 0;
        var response = await SqsMessageHandler.ProcessBatchAsync<int>(
            SqsEvent("1", "2"),
            (SQSEvent.SQSMessage m) =>
            {
                calls++;
                return m.MessageId == "m0" ? Fail(m) : Ok(m);
            },
            NullLogger.Instance);

        calls.ShouldBe(2);
        response.BatchItemFailures.Single().ItemIdentifier.ShouldBe("m0");
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("")]
    public async Task ProcessBatchAsync_TypedOverload_InvalidBody_ReportsFailureWithoutCallingProcessor(string body)
    {
        var called = false;
        var response = await SqsMessageHandler.ProcessBatchAsync<Msg, int>(
            SqsEvent(body),
            _ =>
            {
                called = true;
                return Ok(_);
            },
            NullLogger.Instance);

        called.ShouldBeFalse();
        response.BatchItemFailures.Single().ItemIdentifier.ShouldBe("m0");
    }

    [Fact]
    public async Task ProcessBatchAsync_TypedOverload_ValidBody_CallsProcessor()
    {
        Msg? seen = null;
        var response = await SqsMessageHandler.ProcessBatchAsync<Msg, int>(
            SqsEvent("{\"Value\":5}"),
            m =>
            {
                seen = m;
                return Ok(m);
            });

        seen.ShouldBe(new Msg(5));
        response.BatchItemFailures.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProcessAllAsync_EmptyRecords_ReturnsRight()
    {
        var result = await SqsMessageHandler.ProcessAllAsync(SqsEvent(), (SQSEvent.SQSMessage _) => Ok(_));

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessAllAsync_FirstLeftStopsProcessing()
    {
        var calls = 0;
        var result = await SqsMessageHandler.ProcessAllAsync(
            SqsEvent("1", "2"),
            (SQSEvent.SQSMessage m) =>
            {
                calls++;
                return Fail(m);
            });

        calls.ShouldBe(1);
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessAllAsync_AllRight_ProcessesEveryRecord()
    {
        var calls = 0;
        var result = await SqsMessageHandler.ProcessAllAsync(
            SqsEvent("1", "2"),
            (SQSEvent.SQSMessage m) =>
            {
                calls++;
                return Ok(m);
            });

        calls.ShouldBe(2);
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessAllAsync_CancelledToken_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var action = () => SqsMessageHandler.ProcessAllAsync(SqsEvent("1"), (SQSEvent.SQSMessage _) => Ok(_), cts.Token);

        await Should.ThrowAsync<OperationCanceledException>(action);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    public void DeserializeMessage_InvalidBody_ReturnsLeftWithDeserializationCode(string body)
    {
        var result = SqsMessageHandler.DeserializeMessage<Msg>(new SQSEvent.SQSMessage { Body = body });

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone(string.Empty).ShouldBe("sqs.deserialization_failed"));
    }

    [Fact]
    public void DeserializeMessage_NullBody_Throws()
    {
        Action action = () => _ = SqsMessageHandler.DeserializeMessage<Msg>(new SQSEvent.SQSMessage { Body = null! });

        Should.Throw<ArgumentNullException>(action);
    }

    [Fact]
    public void DeserializeMessage_ValidBodyWithCustomOptions_ReturnsRight()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var result = SqsMessageHandler.DeserializeMessage<Msg>(new SQSEvent.SQSMessage { Body = "{\"value\":9}" }, options);

        result.IfRight(m => m.ShouldBe(new Msg(9)));
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region EventBridgeHandler

    private static CloudWatchEvent<Msg> Event(Msg? detail) => new() { Id = "e1", Source = "src", Detail = detail! };

    [Fact]
    public async Task ProcessAsync_UnitOverload_NullEvent_Throws()
    {
        var action = () => EventBridgeHandler.ProcessAsync<Msg>(
            null!,
            _ => Task.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        (await Should.ThrowAsync<ArgumentNullException>(action)).ParamName.ShouldBe("eventBridgeEvent");
    }

    [Fact]
    public async Task ProcessAsync_UnitOverload_NullProcessor_Throws()
    {
        var action = () => EventBridgeHandler.ProcessAsync<Msg>(Event(new Msg(1)), null!);

        (await Should.ThrowAsync<ArgumentNullException>(action)).ParamName.ShouldBe("processEvent");
    }

    [Fact]
    public async Task ProcessAsync_UnitOverload_ValidEvent_ReturnsRight()
    {
        var result = await EventBridgeHandler.ProcessAsync<Msg>(
            Event(new Msg(1)),
            _ => Task.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessAsync_NullDetail_ReturnsLeftWithoutCallingProcessor()
    {
        var called = false;
        var result = await EventBridgeHandler.ProcessAsync<Msg, int>(
            Event(null),
            m =>
            {
                called = true;
                return Ok(m);
            },
            NullLogger.Instance);

        called.ShouldBeFalse();
        result.IfLeft(e => e.GetCode().IfNone(string.Empty).ShouldBe("eventbridge.detail_null"));
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessAsync_ThrowingProcessor_ReturnsLeftWithProcessingFailedCode()
    {
        var result = await EventBridgeHandler.ProcessAsync<Msg, int>(
            Event(new Msg(1)),
            _ => throw new InvalidOperationException("boom"),
            NullLogger.Instance);

        result.IfLeft(e => e.GetCode().IfNone(string.Empty).ShouldBe("eventbridge.processing_failed"));
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessAsync_LeftResult_IsReturnedUnchanged()
    {
        var result = await EventBridgeHandler.ProcessAsync<Msg, int>(Event(new Msg(1)), Fail, NullLogger.Instance);

        result.IfLeft(e => e.GetCode().IfNone(string.Empty).ShouldBe("x.failed"));
        result.IsLeft.ShouldBeTrue();
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("")]
    public async Task ProcessRawAsync_InvalidJson_ReturnsLeftWithDeserializationCode(string json)
    {
        var result = await EventBridgeHandler.ProcessRawAsync<Msg, int>(json, Ok, NullLogger.Instance);

        result.IfLeft(e => e.GetCode().IfNone(string.Empty).ShouldBe("eventbridge.deserialization_failed"));
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessRawAsync_ValidJson_CallsProcessor()
    {
        var json = "{\"id\":\"e1\",\"source\":\"src\",\"detail\":{\"Value\":4}}";
        Msg? seen = null;

        var result = await EventBridgeHandler.ProcessRawAsync<Msg, int>(
            json,
            m =>
            {
                seen = m;
                return Ok(m);
            },
            NullLogger.Instance,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        seen.ShouldBe(new Msg(4));
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void GetMetadata_CopiesEnvelopeFields()
    {
        var time = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var evt = new CloudWatchEvent<Msg>
        {
            Id = "e1",
            Source = "src",
            DetailType = "dt",
            Account = "acc",
            Region = "eu-west-1",
            Time = time
        };

        var metadata = EventBridgeHandler.GetMetadata(evt);

        metadata.Id.ShouldBe("e1");
        metadata.Source.ShouldBe("src");
        metadata.DetailType.ShouldBe("dt");
        metadata.Account.ShouldBe("acc");
        metadata.Region.ShouldBe("eu-west-1");
        metadata.Time.ShouldBe(time);
    }

    #endregion
}
