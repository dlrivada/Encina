using System.Text.Json;
using Encina.Messaging.Outbox;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.Outbox;

/// <summary>
/// Unit tests for <see cref="OutboxBatchProcessor"/>, the processing cycle shared by every outbox
/// processor and by <see cref="OutboxOrchestrator.ProcessPendingMessagesAsync"/> (#1151, #1150).
/// </summary>
public sealed class OutboxBatchProcessorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 10, 0, 0, TimeSpan.Zero);
    private static readonly EncinaError HandlerError = EncinaErrors.Create("handler.rejected", "Handler rejected the notification");
    private static readonly EncinaError StoreError = EncinaErrors.Create("outbox.mark_failed", "Database unavailable");

    private readonly IOutboxStore _store = Substitute.For<IOutboxStore>();
    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly CapturingLogger _logger = new();

    public OutboxBatchProcessorTests()
    {
        _store.MarkAsProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
        _store.MarkAsFailedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
    }

    [Fact]
    public async Task ProcessAsync_PublishReturnsRight_MarksAsProcessed()
    {
        var message = Pending(retryCount: 0);
        var sut = CreateSut(new OutboxOptions());

        var result = await sut.ProcessAsync((_, _, _) => Delivered(), CancellationToken.None);

        Outcome(result).ShouldBe(new OutboxBatchResult(1, 0, 0, 0));
        await _store.Received(1).MarkAsProcessedAsync(message.Id, Arg.Any<CancellationToken>());
        await _store.DidNotReceiveWithAnyArgs().MarkAsFailedAsync(default, default!, default, default);
    }

    [Fact]
    public async Task ProcessAsync_PublishReturnsLeft_MarksAsFailedWithBackoffInsteadOfProcessed()
    {
        var message = Pending(retryCount: 0);
        var sut = CreateSut(Options(maxRetries: 5));

        var result = await sut.ProcessAsync((_, _, _) => Rejected(), CancellationToken.None);

        Outcome(result).ShouldBe(new OutboxBatchResult(0, 1, 0, 0));
        await _store.Received(1).MarkAsFailedAsync(
            message.Id,
            HandlerError.Message,
            Now.UtcDateTime.AddSeconds(5),
            Arg.Any<CancellationToken>());
        await _store.DidNotReceiveWithAnyArgs().MarkAsProcessedAsync(default, default);
        _logger.EventIds.ShouldContain(2832);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(1, 10)]
    [InlineData(2, 20)]
    [InlineData(3, 40)]
    public async Task ProcessAsync_Failure_DelayDoublesWithTheCurrentRetryCount(int retryCount, int expectedSeconds)
    {
        var message = Pending(retryCount);
        var sut = CreateSut(Options(maxRetries: 10));

        await sut.ProcessAsync((_, _, _) => Rejected(), CancellationToken.None);

        await _store.Received(1).MarkAsFailedAsync(
            message.Id,
            Arg.Any<string>(),
            Now.UtcDateTime.AddSeconds(expectedSeconds),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_Failure_DelayIsCappedByMaxRetryDelay()
    {
        var message = Pending(retryCount: 8);
        var options = Options(maxRetries: 20);
        options.MaxRetryDelay = TimeSpan.FromMinutes(1);
        var sut = CreateSut(options);

        await sut.ProcessAsync((_, _, _) => Rejected(), CancellationToken.None);

        // 5 s * 2^8 = 1280 s, capped to 60 s.
        await _store.Received(1).MarkAsFailedAsync(
            message.Id,
            Arg.Any<string>(),
            Now.UtcDateTime.AddMinutes(1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_Failure_JitterReducesTheDelayByTheSampledFraction()
    {
        var message = Pending(retryCount: 1);
        var options = Options(maxRetries: 5);
        options.RetryJitterRatio = 0.5;
        var sut = CreateSut(options, jitterSource: () => 0.5);

        await sut.ProcessAsync((_, _, _) => Rejected(), CancellationToken.None);

        // 10 s * (1 - 0.5 * 0.5) = 7.5 s.
        await _store.Received(1).MarkAsFailedAsync(
            message.Id,
            Arg.Any<string>(),
            Now.UtcDateTime.AddSeconds(7.5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_FailureReachingMaxRetries_IsExhaustedWithoutNextRetry()
    {
        var message = Pending(retryCount: 2);
        var sut = CreateSut(Options(maxRetries: 3));

        var result = await sut.ProcessAsync((_, _, _) => Rejected(), CancellationToken.None);

        Outcome(result).ShouldBe(new OutboxBatchResult(0, 0, 1, 0));
        await _store.Received(1).MarkAsFailedAsync(message.Id, HandlerError.Message, null, Arg.Any<CancellationToken>());
        await _store.DidNotReceiveWithAnyArgs().MarkAsProcessedAsync(default, default);
        var entry = _logger.Entries.Single(e => e.EventId == 2958);
        entry.Level.ShouldBe(LogLevel.Error);
        entry.Message.ShouldContain(OutboxErrorCodes.MaxRetriesExceeded);
        entry.Message.ShouldContain(message.Id.ToString());
    }

    [Fact]
    public async Task ProcessAsync_PublishThrows_FollowsTheSameFailurePath()
    {
        var message = Pending(retryCount: 0);
        var sut = CreateSut(Options(maxRetries: 3));
        var exception = new InvalidOperationException("Broker unavailable");

        var result = await sut.ProcessAsync(
            (_, _, _) => ValueTask.FromException<Either<EncinaError, Unit>>(exception),
            CancellationToken.None);

        Outcome(result).ShouldBe(new OutboxBatchResult(0, 1, 0, 0));
        await _store.Received(1).MarkAsFailedAsync(
            message.Id,
            "Broker unavailable",
            Now.UtcDateTime.AddSeconds(5),
            Arg.Any<CancellationToken>());
        _logger.Entries.Single(e => e.EventId == 2832).Exception.ShouldBeSameAs(exception);
    }

    [Fact]
    public async Task ProcessAsync_LeftCarryingAnException_LogsThatException()
    {
        Pending(retryCount: 0);
        var sut = CreateSut(Options(maxRetries: 3));
        var exception = new TimeoutException("Handler timed out");
        var error = EncinaErrors.FromException("handler.timeout", exception);

        await sut.ProcessAsync((_, _, _) => ValueTask.FromResult(Left<EncinaError, Unit>(error)), CancellationToken.None);

        _logger.Entries.Single(e => e.EventId == 2832).Exception.ShouldBeSameAs(exception);
    }

    [Fact]
    public async Task ProcessAsync_UnknownNotificationType_IsAFailure()
    {
        var message = Pending(retryCount: 0, notificationType: "Unknown.Type, Unknown.Assembly");
        var sut = CreateSut(Options(maxRetries: 3));
        var published = false;

        var result = await sut.ProcessAsync((_, _, _) => { published = true; return Delivered(); }, CancellationToken.None);

        published.ShouldBeFalse();
        Outcome(result).ShouldBe(new OutboxBatchResult(0, 1, 0, 0));
        await _store.Received(1).MarkAsFailedAsync(
            message.Id,
            Arg.Is<string>(s => s.Contains("Unknown notification type")),
            Arg.Any<DateTime?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_PayloadDeserializesToNull_IsAFailure()
    {
        var message = Pending(retryCount: 0, content: "null");
        var sut = CreateSut(Options(maxRetries: 3));

        await sut.ProcessAsync((_, _, _) => Delivered(), CancellationToken.None);

        await _store.Received(1).MarkAsFailedAsync(
            message.Id,
            "Failed to deserialize notification",
            Arg.Any<DateTime?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_StoreFailsToFetch_ReturnsTheStoreError()
    {
        var storeError = EncinaErrors.Create("outbox.get_pending_failed", "Database unavailable");
        _store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IEnumerable<IOutboxMessage>>(storeError));
        var sut = CreateSut(new OutboxOptions());

        var result = await sut.ProcessAsync((_, _, _) => Delivered(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.LeftToArray()[0].Message.ShouldBe("Database unavailable");
    }

    [Fact]
    public async Task ProcessAsync_MixedBatch_CountsEachOutcome()
    {
        var delivered = CreateMessage(0);
        var retried = CreateMessage(0);
        var exhausted = CreateMessage(2);
        ReturnPending(delivered, retried, exhausted);
        var sut = CreateSut(Options(maxRetries: 3));

        var result = await sut.ProcessAsync(
            (message, _, _) => message.Id == delivered.Id ? Delivered() : Rejected(),
            CancellationToken.None);

        var outcome = Outcome(result);
        outcome.ShouldBe(new OutboxBatchResult(1, 1, 1, 0));
        outcome.Total.ShouldBe(3);
    }

    [Fact]
    public async Task ProcessAsync_CancellationRequested_StopsBeforeTheNextMessage()
    {
        ReturnPending(CreateMessage(0), CreateMessage(0));
        var sut = CreateSut(new OutboxOptions());
        using var cts = new CancellationTokenSource();

        var result = await sut.ProcessAsync(
            async (_, _, _) =>
            {
                await cts.CancelAsync();
                return Unit.Default;
            },
            cts.Token);

        Outcome(result).Total.ShouldBe(1);
    }

    [Fact]
    public async Task ProcessAsync_MarkAsProcessedReturnsLeft_CountsAStoreErrorInsteadOfASuccess()
    {
        var message = Pending(retryCount: 0);
        _store.MarkAsProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(StoreError));
        var sut = CreateSut(Options(maxRetries: 3));

        var result = await sut.ProcessAsync((_, _, _) => Delivered(), CancellationToken.None);

        Outcome(result).ShouldBe(new OutboxBatchResult(0, 0, 0, 1));
        _logger.EventIds.ShouldNotContain(2831);
        var entry = _logger.Entries.Single(e => e.EventId == 2961);
        entry.Level.ShouldBe(LogLevel.Error);
        entry.Message.ShouldContain(nameof(IOutboxStore.MarkAsProcessedAsync));
        entry.Message.ShouldContain(message.Id.ToString());
        entry.Message.ShouldContain(StoreError.Message);
    }

    [Fact]
    public async Task ProcessAsync_MarkAsFailedReturnsLeftForARetry_CountsAStoreErrorInsteadOfAFailure()
    {
        var message = Pending(retryCount: 0);
        _store.MarkAsFailedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(StoreError));
        var sut = CreateSut(Options(maxRetries: 3));

        var result = await sut.ProcessAsync((_, _, _) => Rejected(), CancellationToken.None);

        Outcome(result).ShouldBe(new OutboxBatchResult(0, 0, 0, 1));
        _logger.EventIds.ShouldNotContain(2832);
        var entry = _logger.Entries.Single(e => e.EventId == 2961);
        entry.Message.ShouldContain(nameof(IOutboxStore.MarkAsFailedAsync));
        entry.Message.ShouldContain(message.Id.ToString());
    }

    [Fact]
    public async Task ProcessAsync_ExhaustedMarkReturnsLeft_DoesNotReportTheMessageAsExhausted()
    {
        var message = Pending(retryCount: 2);
        _store.MarkAsFailedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(StoreError));
        var sut = CreateSut(Options(maxRetries: 3));

        var result = await sut.ProcessAsync((_, _, _) => Rejected(), CancellationToken.None);

        Outcome(result).ShouldBe(new OutboxBatchResult(0, 0, 0, 1));
        await _store.Received(1).MarkAsFailedAsync(message.Id, HandlerError.Message, null, Arg.Any<CancellationToken>());
        _logger.EventIds.ShouldNotContain(2958);
        _logger.EventIds.ShouldContain(2961);
    }

    [Fact]
    public async Task ProcessAsync_DispatcherReportsNotificationCancelled_StopsWithoutConsumingARetry()
    {
        var first = CreateMessage(0);
        var second = CreateMessage(0);
        ReturnPending(first, second);
        var sut = CreateSut(Options(maxRetries: 3));
        var cancelled = EncinaErrors.Create(EncinaErrorCodes.NotificationCancelled, "Notification handler was cancelled");
        var published = 0;

        var result = await sut.ProcessAsync(
            (_, _, _) =>
            {
                published++;
                return ValueTask.FromResult(Left<EncinaError, Unit>(cancelled));
            },
            CancellationToken.None);

        Outcome(result).Total.ShouldBe(0);
        published.ShouldBe(1);
        await _store.DidNotReceiveWithAnyArgs().MarkAsFailedAsync(default, default!, default, default);
        await _store.DidNotReceiveWithAnyArgs().MarkAsProcessedAsync(default, default);
        _logger.Entries.Single(e => e.EventId == 2962).Message.ShouldContain(first.Id.ToString());
    }

    [Fact]
    public async Task ProcessAsync_TokenCancelledDuringAFailedPublish_DoesNotMarkTheMessageFailed()
    {
        Pending(retryCount: 0);
        var sut = CreateSut(Options(maxRetries: 3));
        using var cts = new CancellationTokenSource();

        var result = await sut.ProcessAsync(
            async (_, _, _) =>
            {
                await cts.CancelAsync();
                return Left<EncinaError, Unit>(HandlerError);
            },
            cts.Token);

        Outcome(result).Total.ShouldBe(0);
        await _store.DidNotReceiveWithAnyArgs().MarkAsFailedAsync(default, default!, default, default);
        _logger.EventIds.ShouldContain(2962);
    }

    [Fact]
    public async Task ProcessAsync_PublishThrowsOperationCanceled_DoesNotMarkTheMessageFailed()
    {
        Pending(retryCount: 0);
        var sut = CreateSut(Options(maxRetries: 3));
        using var cts = new CancellationTokenSource();

        var result = await sut.ProcessAsync(
            async (_, _, _) =>
            {
                await cts.CancelAsync();
                throw new OperationCanceledException(cts.Token);
            },
            cts.Token);

        Outcome(result).Total.ShouldBe(0);
        await _store.DidNotReceiveWithAnyArgs().MarkAsFailedAsync(default, default!, default, default);
        await _store.DidNotReceiveWithAnyArgs().MarkAsProcessedAsync(default, default);
    }

    [Fact]
    public async Task ProcessAsync_OperationCanceledWithoutCancellationRequested_IsAnOrdinaryFailure()
    {
        var message = Pending(retryCount: 0);
        var sut = CreateSut(Options(maxRetries: 3));

        var result = await sut.ProcessAsync(
            (_, _, _) => ValueTask.FromException<Either<EncinaError, Unit>>(new TaskCanceledException("HTTP timeout")),
            CancellationToken.None);

        Outcome(result).ShouldBe(new OutboxBatchResult(0, 1, 0, 0));
        await _store.Received(1).MarkAsFailedAsync(message.Id, "HTTP timeout", Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_RetryTimeBeyondDateTimeMaxValue_SaturatesInsteadOfThrowing()
    {
        var message = Pending(retryCount: 60);
        var options = Options(maxRetries: 100);
        options.MaxRetryDelay = TimeSpan.MaxValue;
        var sut = CreateSut(options);

        var result = await sut.ProcessAsync((_, _, _) => Rejected(), CancellationToken.None);

        Outcome(result).ShouldBe(new OutboxBatchResult(0, 1, 0, 0));
        await _store.Received(1).MarkAsFailedAsync(
            message.Id,
            HandlerError.Message,
            DateTime.MaxValue,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AddSaturating_DelayWithinRange_AddsIt()
    {
        OutboxBatchProcessor.AddSaturating(Now.UtcDateTime, TimeSpan.FromMinutes(5))
            .ShouldBe(Now.UtcDateTime.AddMinutes(5));
    }

    [Fact]
    public void AddSaturating_DelayOverflowing_ReturnsUtcMaxValue()
    {
        var result = OutboxBatchProcessor.AddSaturating(Now.UtcDateTime, TimeSpan.MaxValue);

        result.ShouldBe(DateTime.MaxValue);
        result.Kind.ShouldBe(DateTimeKind.Utc);
    }

    private OutboxBatchProcessor CreateSut(OutboxOptions options, Func<double>? jitterSource = null)
        => new(_store, options, _logger, serializer: null, _timeProvider, jitterSource);

    private static OutboxOptions Options(int maxRetries) => new()
    {
        MaxRetries = maxRetries,
        BaseRetryDelay = TimeSpan.FromSeconds(5),
        RetryJitterRatio = 0
    };

    private TestOutboxMessage Pending(int retryCount, string? notificationType = null, string? content = null)
    {
        var message = CreateMessage(retryCount, notificationType, content);
        ReturnPending(message);
        return message;
    }

    private void ReturnPending(params IOutboxMessage[] messages)
    {
        _store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IOutboxMessage>>(messages.ToList()));
    }

    private static TestOutboxMessage CreateMessage(int retryCount, string? notificationType = null, string? content = null) => new()
    {
        Id = Guid.NewGuid(),
        NotificationType = notificationType ?? typeof(TestNotification).AssemblyQualifiedName!,
        Content = content ?? JsonSerializer.Serialize(new TestNotification { Id = Guid.NewGuid(), Message = "Test" }),
        CreatedAtUtc = Now.UtcDateTime,
        RetryCount = retryCount
    };

    private static ValueTask<Either<EncinaError, Unit>> Delivered()
        => ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));

    private static ValueTask<Either<EncinaError, Unit>> Rejected()
        => ValueTask.FromResult(Left<EncinaError, Unit>(HandlerError));

    private static OutboxBatchResult Outcome(Either<EncinaError, OutboxBatchResult> result)
    {
        result.IsRight.ShouldBeTrue();
        return result.RightToArray()[0];
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<(int EventId, LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public IEnumerable<int> EventIds => Entries.Select(e => e.EventId);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((eventId.Id, logLevel, formatter(state, exception), exception));
    }
}
