using System.Text.Json;
using Encina.Messaging;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.Outbox;
using Encina.Messaging.Recoverability;
using Encina.Recoverability;
using Encina.UnitTests.Messaging.DeadLetter;
using Encina.UnitTests.Recoverability;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.Outbox;

/// <summary>
/// The Retry-After ceiling of the outbox (#2588): at or below <see cref="OutboxOptions.MaxRetryAfter"/> the next
/// attempt is never earlier than now + header; above it the message is exhausted at once and dead-lettered with
/// <see cref="RecoverabilityErrorCodes.RetryAfterExceedsCeiling"/>.
/// </summary>
public sealed class OutboxRetryAfterCeilingTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);
    private const string RateLimited = "encina.outbound.rate_limited";

    private readonly IOutboxStore _store = Substitute.For<IOutboxStore>();
    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly FakeLogger _logger = new();

    public OutboxRetryAfterCeilingTests()
    {
        _store.MarkAsFailedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
    }

    [Fact]
    public void Options_Defaults_AreTwentyFourHours()
    {
        new OutboxOptions().MaxRetryAfter.ShouldBe(TimeSpan.FromHours(24));
        new RecoverabilityOptions().MaxRetryAfter.ShouldBe(TimeSpan.FromHours(24));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Options_ZeroOrNegativeCeiling_IsRejected(int seconds)
    {
        var value = TimeSpan.FromSeconds(seconds);

        Should.Throw<ArgumentOutOfRangeException>(() => new OutboxOptions { MaxRetryAfter = value });
        Should.Throw<ArgumentOutOfRangeException>(() => new RecoverabilityOptions { MaxRetryAfter = value });
    }

    [Fact]
    public void Options_MaxValue_IsTheExplicitNoCeiling()
    {
        new OutboxOptions { MaxRetryAfter = TimeSpan.MaxValue }.MaxRetryAfter.ShouldBe(TimeSpan.MaxValue);
        new RecoverabilityOptions { MaxRetryAfter = TimeSpan.MaxValue }.MaxRetryAfter.ShouldBe(TimeSpan.MaxValue);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(60)]
    public async Task RetryAfterAtOrBelowTheCeiling_SchedulesExactlyNowPlusTheHeader(int minutes)
    {
        var message = Pending(retryCount: 0);
        var sut = CreateSut(Options(ceiling: TimeSpan.FromHours(1)));

        var result = await sut.ProcessAsync((_, _, _) => Limited(TimeSpan.FromMinutes(minutes)), CancellationToken.None);

        Outcome(result).ShouldBe(new OutboxBatchResult(0, 1, 0, 0));
        await _store.Received(1).MarkAsFailedAsync(
            message.Id, RateLimited, Now.UtcDateTime.AddMinutes(minutes), Arg.Any<CancellationToken>());
        _logger.Collector.GetSnapshot().ShouldNotContain(e => e.Id.Id == 5456);
    }

    [Fact]
    public async Task RetryAfterAboveTheCeiling_ExhaustsAtOnceWithRetryCountZeroAndTheCeilingCode()
    {
        using var metrics = new MetricCapture("encina.retry.skipped_total");
        var message = Pending(retryCount: 0);
        var sut = CreateSut(Options(ceiling: TimeSpan.FromHours(1)));

        var result = await sut.ProcessAsync((_, _, _) => Limited(TimeSpan.FromMinutes(61)), CancellationToken.None);

        Outcome(result).ShouldBe(new OutboxBatchResult(0, 0, 1, 0));
        await _store.Received(1).MarkAsFailedAsync(
            message.Id, RecoverabilityErrorCodes.RetryAfterExceedsCeiling, null, Arg.Any<CancellationToken>());

        var entry = _logger.Collector.GetSnapshot().Single(e => e.Id.Id == 5456);
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.Message.ShouldContain(RateLimited);
        entry.Message.ShouldContain(message.Id.ToString());
        entry.Message.ShouldNotContain("Outbound failure");

        var tags = metrics.Tags("encina.retry.skipped_total", nameof(TestNotification)).Single();
        tags["reason"].ShouldBe(RetryMetrics.ReasonRetryAfterExceedsCeiling);
        tags["error_code"].ShouldBe(RateLimited);
        tags.ContainsKey("encina.tenant_id").ShouldBeFalse();
    }

    [Fact]
    public async Task RetryAfterAboveTheCeiling_ExhaustsEvenWhenRetriesRemainAndWhenTheyAreUsedUp()
    {
        var first = CreateMessage(retryCount: 0);
        var last = CreateMessage(retryCount: 9);
        ReturnPending(first, last);
        var sut = CreateSut(Options(ceiling: TimeSpan.FromMinutes(1)));

        var result = await sut.ProcessAsync((_, _, _) => Limited(TimeSpan.FromMinutes(2)), CancellationToken.None);

        Outcome(result).ShouldBe(new OutboxBatchResult(0, 0, 2, 0));
        await _store.Received(1).MarkAsFailedAsync(
            first.Id, RecoverabilityErrorCodes.RetryAfterExceedsCeiling, null, Arg.Any<CancellationToken>());
        await _store.Received(1).MarkAsFailedAsync(
            last.Id, RecoverabilityErrorCodes.RetryAfterExceedsCeiling, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DefaultCeiling_TwoHoursIsScheduledAndTwentyFiveHoursIsExhausted()
    {
        var scheduled = Pending(retryCount: 0);
        var sut = CreateSut(new OutboxOptions { RetryJitterRatio = 0 });
        await sut.ProcessAsync((_, _, _) => Limited(TimeSpan.FromHours(2)), CancellationToken.None);
        await _store.Received(1).MarkAsFailedAsync(
            scheduled.Id, RateLimited, Now.UtcDateTime.AddHours(2), Arg.Any<CancellationToken>());

        var exhausted = Pending(retryCount: 0);
        await sut.ProcessAsync((_, _, _) => Limited(TimeSpan.FromHours(25)), CancellationToken.None);
        await _store.Received(1).MarkAsFailedAsync(
            exhausted.Id, RecoverabilityErrorCodes.RetryAfterExceedsCeiling, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetryAfterAboveTheCeiling_IsDeadLetteredWithTheCeilingCode()
    {
        using var host = DeadLetterCaptureHost.Create();
        var message = Pending(retryCount: 0);
        var sut = CreateSut(Options(ceiling: TimeSpan.FromHours(1)), host.Capture);

        await sut.ProcessAsync((_, _, _) => Limited(TimeSpan.FromHours(2)), CancellationToken.None);

        var deadLetter = host.DeadLettersOf(DeadLetterSourcePatterns.Outbox).Single();
        deadLetter.ErrorCode.ShouldBe(RecoverabilityErrorCodes.RetryAfterExceedsCeiling);
        deadLetter.SourceMessageId.ShouldBe(message.Id.ToString("D"));
    }

    [Fact]
    public async Task RetryAfterAboveTheCeiling_CaptureFailureKeepsTheRetryableSemantics()
    {
        using var metrics = new MetricCapture("encina.retry.skipped_total");
        var deadLetterStore = Substitute.For<IDeadLetterStore>();
        deadLetterStore.AddAsync(Arg.Any<IDeadLetterMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, bool>>(EncinaErrors.Create(DeadLetterErrorCodes.StoreFailed, "down")));
        using var host = DeadLetterCaptureHost.Create(store: deadLetterStore);
        Pending(retryCount: 0);
        var sut = CreateSut(Options(ceiling: TimeSpan.FromHours(1)), host.Capture);

        var result = await sut.ProcessAsync((_, _, _) => Limited(TimeSpan.FromHours(2)), CancellationToken.None);

        // The exhausted state is not recorded, the message is delivered again later, and nothing is counted twice.
        Outcome(result).ShouldBe(new OutboxBatchResult(0, 0, 0, 1));
        await _store.DidNotReceiveWithAnyArgs().MarkAsFailedAsync(default, default!, default, default);
        _logger.Collector.GetSnapshot().ShouldNotContain(e => e.Id.Id == 5456);
        metrics.Tags("encina.retry.skipped_total", nameof(TestNotification)).ShouldBeEmpty();
    }

    private OutboxBatchProcessor CreateSut(OutboxOptions options, DeadLetterSourceCapture? capture = null)
        => new(_store, options, _logger, serializer: null, _timeProvider, jitterSource: null, capture);

    private static OutboxOptions Options(TimeSpan ceiling) => new()
    {
        MaxRetries = 10,
        BaseRetryDelay = TimeSpan.FromSeconds(5),
        RetryJitterRatio = 0,
        MaxRetryAfter = ceiling
    };

    private TestOutboxMessage Pending(int retryCount)
    {
        var message = CreateMessage(retryCount);
        ReturnPending(message);
        return message;
    }

    private void ReturnPending(params IOutboxMessage[] messages)
    {
        _store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IOutboxMessage>>(messages.ToList()));
    }

    private static TestOutboxMessage CreateMessage(int retryCount) => new()
    {
        Id = Guid.NewGuid(),
        NotificationType = typeof(TestNotification).AssemblyQualifiedName!,
        Content = JsonSerializer.Serialize(new TestNotification { Id = Guid.NewGuid(), Message = "Test" }),
        CreatedAtUtc = Now.UtcDateTime,
        RetryCount = retryCount
    };

    private static ValueTask<Either<EncinaError, Unit>> Limited(TimeSpan retryAfter)
        => ValueTask.FromResult(Left<EncinaError, Unit>(OutboundTestSupport.Outbound(
            RateLimited, transient: true, retryAfter: retryAfter)));

    private static OutboxBatchResult Outcome(Either<EncinaError, OutboxBatchResult> result)
    {
        result.IsRight.ShouldBeTrue();
        return result.RightToArray()[0];
    }
}
