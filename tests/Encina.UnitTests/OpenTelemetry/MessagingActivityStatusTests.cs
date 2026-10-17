using System.Diagnostics;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.OpenTelemetry.Enrichers;
using Encina.OpenTelemetry.MessagingStores;
using Encina.Sharding;
using Encina.Sharding.Diagnostics;
using LanguageExt;
using NSubstitute;
using Shouldly;
using OtelTagNames = Encina.OpenTelemetry.ActivityTagNames;

namespace Encina.UnitTests.OpenTelemetry;

/// <summary>
/// Proves that the messaging decorators, the messaging enricher and the shard router never write an
/// error message to an activity status description or tag (AGENTS.md section 3, #2134).
/// </summary>
[Collection("ActivityListenerIsolation")]
public sealed class MessagingActivityStatusTests
{
    private const string Code = "test.sensitive_failure";

    private static (EncinaError Error, string Secret) NewError()
    {
        var secret = "SECRET-" + Guid.NewGuid().ToString("N");
        return (EncinaErrors.Create(Code, "value " + secret), secret);
    }

    [Fact]
    public async Task InboxStore_WhenInnerReturnsError_StatusIsErrorCode()
    {
        using var capture = new ActivityStatusCapture();
        var (error, secret) = NewError();
        var inner = Substitute.For<IInboxStore>();
        inner.RemoveExpiredMessagesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Left(error));

        await new InstrumentedInboxStore(inner).RemoveExpiredMessagesAsync(["m-1"]);

        capture.ShouldHaveErrorStatus(Code, secret);
    }

    [Fact]
    public async Task InboxStore_MarkAsFailedWithSecretMessage_NeverReachesTheActivity()
    {
        using var capture = new ActivityStatusCapture();
        var (error, secret) = NewError();
        var inner = Substitute.For<IInboxStore>();
        inner.MarkAsFailedAsync("m-1", Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Left(error));

        await new InstrumentedInboxStore(inner).MarkAsFailedAsync("m-1", secret, null);

        capture.ShouldHaveErrorStatus(Code, secret);
    }

    [Fact]
    public async Task DeadLetterStore_WhenInnerReturnsError_ErrorCodeTagIsTheCode()
    {
        using var capture = new ActivityStatusCapture();
        var (error, secret) = NewError();
        var inner = Substitute.For<IDeadLetterStore>();
        inner.DeleteExpiredAsync(Arg.Any<CancellationToken>()).Returns(Either<EncinaError, int>.Left(error));

        await new InstrumentedDeadLetterStore(inner).DeleteExpiredAsync();

        capture.ShouldHaveErrorTag("encina.error_code", Code, secret);
    }

    [Fact]
    public async Task DeadLetterStore_WhenErrorHasNoCode_ErrorCodeTagIsTheFallback()
    {
        using var capture = new ActivityStatusCapture();
        var secret = "SECRET-" + Guid.NewGuid().ToString("N");
        var inner = Substitute.For<IDeadLetterStore>();
        inner.GetCountAsync(Arg.Any<DeadLetterFilter?>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, int>.Left(EncinaError.New("raw " + secret)));

        await new InstrumentedDeadLetterStore(inner).GetCountAsync();

        capture.ShouldHaveErrorTag("encina.error_code", "Encina.unknown", secret);
    }

    [Fact]
    public void ShardRouter_WhenInnerReturnsError_StatusIsErrorCode()
    {
        using var capture = new ActivityStatusCapture();
        var (error, secret) = NewError();
        var inner = Substitute.For<IShardRouter>();
        inner.GetShardId("key").Returns(Either<EncinaError, string>.Left(error));
        var router = new InstrumentedShardRouter(inner, new ShardRoutingMetrics(Topology()), "hash");

        router.GetShardId("key");

        capture.ShouldHaveErrorStatus(Code, secret);
    }

    [Fact]
    public void ShardRouter_WhenErrorHasNoCode_StatusIsTheFallbackNotTheMessage()
    {
        using var capture = new ActivityStatusCapture();
        var secret = "SECRET-" + Guid.NewGuid().ToString("N");
        var inner = Substitute.For<IShardRouter>();
        inner.GetShardId("key").Returns(Either<EncinaError, string>.Left(EncinaError.New("raw " + secret)));
        var router = new InstrumentedShardRouter(inner, new ShardRoutingMetrics(Topology()), "hash");

        router.GetShardId("key");

        capture.ShouldHaveErrorStatus("Encina.unknown", secret);
    }

    [Fact]
    public void ShardRouter_CompoundKeyFailure_StatusIsErrorCode()
    {
        using var capture = new ActivityStatusCapture();
        var (error, secret) = NewError();
        var key = new CompoundShardKey("a", "b");
        var router = new InstrumentedShardRouter(
            new FakeRouter { CompoundResult = Either<EncinaError, string>.Left(error) },
            new ShardRoutingMetrics(Topology()),
            "hash");

        router.GetShardId(key);

        capture.ShouldHaveErrorStatus(Code, secret);
    }

    [Fact]
    public void ShardRouter_PartialKeyFailure_StatusIsErrorCode()
    {
        using var capture = new ActivityStatusCapture();
        var (error, secret) = NewError();
        var key = new CompoundShardKey("a");
        var router = new InstrumentedShardRouter(
            new FakeRouter { PartialResult = Either<EncinaError, IReadOnlyList<string>>.Left(error) },
            new ShardRoutingMetrics(Topology()),
            "hash");

        router.GetShardIds(key);

        capture.ShouldHaveErrorStatus(Code, secret);
    }

    [Fact]
    public void ShardRouter_PartialKeySuccess_CompletesOkWithShardCount()
    {
        using var capture = new ActivityStatusCapture();
        var key = new CompoundShardKey("a");
        var router = new InstrumentedShardRouter(
            new FakeRouter { PartialResult = Either<EncinaError, IReadOnlyList<string>>.Right(["shard-1"]) },
            new ShardRoutingMetrics(Topology()),
            "hash");

        var result = router.GetShardIds(key);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void Enricher_OutboxMessageWithStoredError_TagsOnlyTheFailedMarker()
    {
        var secret = "SECRET-" + Guid.NewGuid().ToString("N");
        var message = Substitute.For<IOutboxMessage>();
        message.Id.Returns(Guid.NewGuid());
        message.ErrorMessage.Returns("insert failed for " + secret);
        using var activity = new Activity("enrich");

        MessagingActivityEnricher.EnrichWithOutboxMessage(activity, message);

        activity.GetTagItem(OtelTagNames.Messaging.Error).ShouldBe(true);
        AssertNoTagCarries(activity, secret);
    }

    [Fact]
    public void Enricher_InboxMessageWithStoredError_TagsOnlyTheFailedMarker()
    {
        var secret = "SECRET-" + Guid.NewGuid().ToString("N");
        var message = Substitute.For<IInboxMessage>();
        message.MessageId.Returns("m-1");
        message.ErrorMessage.Returns("handler failed for " + secret);
        using var activity = new Activity("enrich");

        MessagingActivityEnricher.EnrichWithInboxMessage(activity, message);

        activity.GetTagItem(OtelTagNames.Messaging.Error).ShouldBe(true);
        AssertNoTagCarries(activity, secret);
    }

    [Fact]
    public void Enricher_SagaStateWithStoredError_TagsOnlyTheFailedMarker()
    {
        var secret = "SECRET-" + Guid.NewGuid().ToString("N");
        var saga = Substitute.For<ISagaState>();
        saga.SagaId.Returns(Guid.NewGuid());
        saga.ErrorMessage.Returns("step failed for " + secret);
        using var activity = new Activity("enrich");

        MessagingActivityEnricher.EnrichWithSagaState(activity, saga);

        activity.GetTagItem(OtelTagNames.Saga.Error).ShouldBe(true);
        AssertNoTagCarries(activity, secret);
    }

    [Fact]
    public void Enricher_ScheduledMessageWithStoredError_TagsOnlyTheFailedMarker()
    {
        var secret = "SECRET-" + Guid.NewGuid().ToString("N");
        var message = Substitute.For<IScheduledMessage>();
        message.Id.Returns(Guid.NewGuid());
        message.ErrorMessage.Returns("dispatch failed for " + secret);
        using var activity = new Activity("enrich");

        MessagingActivityEnricher.EnrichWithScheduledMessage(activity, message);

        activity.GetTagItem(OtelTagNames.Messaging.Error).ShouldBe(true);
        AssertNoTagCarries(activity, secret);
    }

    [Fact]
    public void Enricher_MessageWithoutStoredError_SetsNoErrorTag()
    {
        var message = Substitute.For<IOutboxMessage>();
        message.Id.Returns(Guid.NewGuid());
        message.ErrorMessage.Returns((string?)null);
        using var activity = new Activity("enrich");

        MessagingActivityEnricher.EnrichWithOutboxMessage(activity, message);

        activity.GetTagItem(OtelTagNames.Messaging.Error).ShouldBeNull();
    }

    // NSubstitute cannot stub the default interface members of IShardRouter, so the compound-key paths use a fake.
    private sealed class FakeRouter : IShardRouter
    {
        public Either<EncinaError, string> CompoundResult { get; init; } = Either<EncinaError, string>.Right("shard-1");

        public Either<EncinaError, IReadOnlyList<string>> PartialResult { get; init; } =
            Either<EncinaError, IReadOnlyList<string>>.Right(["shard-1"]);

        public Either<EncinaError, string> GetShardId(string shardKey) => Either<EncinaError, string>.Right("shard-1");

        public Either<EncinaError, string> GetShardId(CompoundShardKey key) => CompoundResult;

        public Either<EncinaError, IReadOnlyList<string>> GetShardIds(CompoundShardKey partialKey) => PartialResult;

        public IReadOnlyList<string> GetAllShardIds() => ["shard-1"];

        public Either<EncinaError, string> GetShardConnectionString(string shardId) =>
            Either<EncinaError, string>.Right("conn-shard-1");
    }

    private static ShardTopology Topology() => new([new ShardInfo("shard-1", "conn-shard-1")]);

    private static void AssertNoTagCarries(Activity activity, string secret)
    {
        foreach (var tag in activity.TagObjects)
        {
            (tag.Value?.ToString() ?? string.Empty).ShouldNotContain(secret);
        }
    }
}
