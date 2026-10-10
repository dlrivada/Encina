using Encina.Messaging.DeadLetter;
using Encina.Messaging.Recoverability;
using Encina.Messaging.Serialization;
using Encina.UnitTests.Messaging.DeadLetter;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

#pragma warning disable CA2012 // NSubstitute setup of ValueTask-returning members

namespace Encina.UnitTests.Messaging.Recoverability;

/// <summary>
/// Tests of the delayed-retry chain invariant (#2083): one retry chain per failed request, only the
/// <see cref="DelayedRetryProcessor"/> decides attempt N+1 or permanent failure, and
/// <c>OnPermanentFailure</c> runs once under the logical message's original id.
/// </summary>
public sealed class DelayedRetryChainTests
{
    /// <summary>A request the processor can resolve by name and deserialize.</summary>
    public sealed record ChainCommand(int Value) : IRequest<int>;

    private sealed class FakeRow : IDelayedRetryMessage
    {
        public FakeRow(DelayedRetryMessageData data)
        {
            Id = data.Id;
            RecoverabilityContextId = data.RecoverabilityContextId;
            RequestType = data.RequestType;
            RequestContent = data.RequestContent;
            ContextContent = data.ContextContent;
            DelayedRetryAttempt = data.DelayedRetryAttempt;
            ScheduledAtUtc = data.ScheduledAtUtc;
            ExecuteAtUtc = data.ExecuteAtUtc;
            CorrelationId = data.CorrelationId;
        }

        public Guid Id { get; }
        public Guid RecoverabilityContextId { get; }
        public string RequestType { get; }
        public string RequestContent { get; }
        public string ContextContent { get; }
        public int DelayedRetryAttempt { get; }
        public DateTime ScheduledAtUtc { get; }
        public DateTime ExecuteAtUtc { get; }
        public string? CorrelationId { get; }
        public DateTime? ProcessedAtUtc { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsPending => ProcessedAtUtc is null && ErrorMessage is null;
    }

    private sealed class FakeFactory : IDelayedRetryMessageFactory
    {
        public IDelayedRetryMessage Create(DelayedRetryMessageData data) => new FakeRow(data);
    }

    private sealed class FakeStore : IDelayedRetryStore
    {
        private readonly object _gate = new();
        private readonly List<FakeRow> _rows = [];

        private TaskCompletionSource? _pollWaiter;
        private int _pollTarget;

        // Completes once the processor has polled the store `additional` more times.
        public Task WaitForPollsAsync(int additional)
        {
            lock (_gate)
            {
                _pollTarget = PendingCountPerPoll.Count + additional;
                _pollWaiter = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                return _pollWaiter.Task;
            }
        }

        public List<int> PendingCountPerPoll { get; } = [];
        public int FailedCount { get { lock (_gate) { return _rows.Count(r => r.ErrorMessage is not null); } } }
        public List<int> AttemptsAdded { get; } = [];
        public List<Guid> ContextIdsAdded { get; } = [];

        public Task AddAsync(IDelayedRetryMessage message, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var row = (FakeRow)message;
                _rows.Add(row);
                AttemptsAdded.Add(row.DelayedRetryAttempt);
                ContextIdsAdded.Add(row.RecoverabilityContextId);
            }

            return Task.CompletedTask;
        }

        public Task<IEnumerable<IDelayedRetryMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                // The chain invariant: whenever the processor polls, exactly one row is pending.
                var pending = _rows.Where(r => r.IsPending).Cast<IDelayedRetryMessage>().ToList();
                PendingCountPerPoll.Add(pending.Count);
                if (_pollWaiter is not null && PendingCountPerPoll.Count >= _pollTarget)
                {
                    _pollWaiter.TrySetResult();
                }

                return Task.FromResult<IEnumerable<IDelayedRetryMessage>>(pending);
            }
        }

        public Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _rows.Single(r => r.Id == id).ProcessedAtUtc = DateTime.UtcNow;
            }

            return Task.CompletedTask;
        }

        public Task MarkAsFailedAsync(Guid id, string errorMessage, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _rows.Single(r => r.Id == id).ErrorMessage = errorMessage;
            }

            return Task.CompletedTask;
        }

        public Task<bool> DeleteByContextIdAsync(Guid recoverabilityContextId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class MarkerClassifier : IErrorClassifier
    {
        public ErrorClassification Classify(EncinaError error, Exception? exception) =>
            error.Message.Contains("permanent", StringComparison.OrdinalIgnoreCase)
                ? ErrorClassification.Permanent
                : ErrorClassification.Transient;
    }

    [Fact]
    public async Task TransientFailureThroughEveryDelayedRetry_KeepsOnePendingRowPerStep_AndFailsPermanentlyOnce()
    {
        // Arrange - 3 delayed retries; the handler always fails transiently.
        var fixture = CreateFixture(delayedRetries: 3, failure: "transient failure");

        // Act
        await fixture.FirstFailureAsync();
        await fixture.RunProcessorUntilPermanentFailureAsync();

        // Assert - attempts 0..2 were scheduled one at a time (the third failure is permanent),
        // all under one context id.
        fixture.Store.AttemptsAdded.ShouldBe([0, 1, 2]);
        fixture.Store.ContextIdsAdded.Distinct().Count().ShouldBe(1);
        fixture.Store.PendingCountPerPoll.ShouldAllBe(count => count <= 1);
        fixture.PermanentFailures.Count.ShouldBe(1);
        fixture.PermanentFailures[0].Id.ShouldBe(fixture.Store.ContextIdsAdded[0]);

        // The callback gets the real failure of the last re-dispatch, and the count of retries that ran.
        var failed = fixture.PermanentFailures[0];
        failed.Error.Message.ShouldBe("transient failure");
        failed.DelayedRetryAttempts.ShouldBe(3);
        failed.RetryHistory.ShouldNotBeEmpty();
        failed.RetryHistory.ShouldAllBe(attempt => attempt.Error.Message == "transient failure");
    }

    private static FakeRow CreateRow(string requestType, string requestContent) => new(new DelayedRetryMessageData(
        Guid.NewGuid(),
        Guid.NewGuid(),
        requestType,
        requestContent,
        "{\"id\":\"00000000-0000-0000-0000-000000000000\",\"immediateRetryCount\":0,\"delayedRetryCount\":0}",
        0,
        DateTime.UtcNow,
        DateTime.UtcNow,
        "chain-correlation"));

    // Runs the processor over one row that never reaches a handler and returns what OnPermanentFailure got.
    private static async Task<List<FailedMessage>> RunRowToPermanentFailureAsync(
        FakeRow row,
        IMessageSerializer? serializer,
        DeadLetterSourceCapture? deadLetterCapture = null)
    {
        var store = new FakeStore();
        await store.AddAsync(row);

        var failures = new List<FailedMessage>();
        var failed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var options = new RecoverabilityOptions
        {
            DelayedRetries = [TimeSpan.FromMilliseconds(1)],
            OnPermanentFailure = (message, _) => { failures.Add(message); failed.TrySetResult(); return Task.CompletedTask; }
        };

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IDelayedRetryStore)).Returns(store);
        serviceProvider.GetService(typeof(IEncina)).Returns(Substitute.For<IEncina>());
        serviceProvider.GetService(typeof(IMessageSerializer)).Returns(serializer);
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);
        var processor = new DelayedRetryProcessor(
            scopeFactory, options, NullLogger<DelayedRetryProcessor>.Instance, deadLetterCapture: deadLetterCapture)
        {
            ProcessingInterval = TimeSpan.FromMilliseconds(5)
        };

        using var cts = new CancellationTokenSource();
        await processor.StartAsync(cts.Token);
        try
        {
            await failed.Task.WaitAsync(TimeSpan.FromSeconds(20));
            await store.WaitForPollsAsync(2).WaitAsync(TimeSpan.FromSeconds(20));
        }
        finally
        {
            await cts.CancelAsync();
            await processor.StopAsync(default);
        }

        return failures;
    }

    [Fact]
    public async Task UnknownRequestType_TakesThePermanentFailurePath_Once()
    {
        // Arrange
        var row = CreateRow("No.Such.Type, NoSuchAssembly", "{}");

        // Act
        var failures = await RunRowToPermanentFailureAsync(row, serializer: null);

        // Assert
        failures.Count.ShouldBe(1);
        failures[0].RequestType.ShouldBe("No.Such.Type, NoSuchAssembly");
        row.IsPending.ShouldBeFalse();
    }

    [Fact]
    public async Task RequestThatDeserializesToNull_TakesThePermanentFailurePath_Once()
    {
        // Arrange
        var row = CreateRow(typeof(ChainCommand).AssemblyQualifiedName!, "null");

        // Act
        var failures = await RunRowToPermanentFailureAsync(row, serializer: null);

        // Assert
        failures.Count.ShouldBe(1);
        row.IsPending.ShouldBeFalse();
    }

    [Fact]
    public async Task UnexpectedExceptionBeforeTheDispatch_TakesThePermanentFailurePath_Once()
    {
        // Arrange - the serializer throws while reading the request.
        var serializer = Substitute.For<IMessageSerializer>();
        serializer.Deserialize(Arg.Any<string>(), Arg.Any<Type>()).Returns(_ => throw new InvalidOperationException("patient 12345"));
        var row = CreateRow(typeof(ChainCommand).AssemblyQualifiedName!, "{\"value\":1}");

        // Act
        var failures = await RunRowToPermanentFailureAsync(row, serializer);

        // Assert
        failures.Count.ShouldBe(1);
        row.ErrorMessage.ShouldBe(typeof(InvalidOperationException).FullName);
    }

    [Fact]
    public async Task PermanentErrorDuringADelayedRetry_ReachesThePermanentFailurePathOnce()
    {
        // Arrange - the first failure is transient, the delayed retry fails with a permanent error.
        var fixture = CreateFixture(delayedRetries: 3, failure: "transient failure", delayedFailure: "permanent failure");

        // Act
        await fixture.FirstFailureAsync();
        await fixture.RunProcessorUntilPermanentFailureAsync();

        // Assert - no attempt 1 was scheduled after the permanent error.
        fixture.Store.AttemptsAdded.ShouldBe([0]);
        fixture.PermanentFailures.Count.ShouldBe(1);
        fixture.PermanentFailures[0].Id.ShouldBe(fixture.Store.ContextIdsAdded[0]);
    }

    [Fact]
    public async Task SchedulerFailingForTheNextAttempt_EndsTheChainThroughThePermanentFailurePath_Once()
    {
        // Arrange - attempt 0 is scheduled by the first failure; scheduling attempt 1 returns Left.
        var fixture = CreateFixture(delayedRetries: 3, failure: "transient failure", failNextSchedule: true);

        // Act
        await fixture.FirstFailureAsync();
        await fixture.RunProcessorUntilPermanentFailureAsync();

        // Assert - the failure is not reported as success: the row fails and the callback runs once.
        fixture.PermanentFailures.Count.ShouldBe(1);
        fixture.Store.AttemptsAdded.ShouldBe([0]);
        fixture.Store.FailedCount.ShouldBe(1);
    }

    [Fact]
    public async Task FirstTimeFailure_WithoutMarker_SchedulesAttemptZero()
    {
        // Arrange
        var fixture = CreateFixture(delayedRetries: 2, failure: "transient failure");

        // Act
        var result = await fixture.FirstFailureAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
        fixture.Store.AttemptsAdded.ShouldBe([0]);
        fixture.PermanentFailures.ShouldBeEmpty();
    }

    [Fact]
    public void DelayedRetryRedispatch_MatchesTheReDispatchedClassInstanceOnly_AndEveryTimeItIsAsked()
    {
        // Arrange
        var request = new ChainCommand(1);
        using var scope = DelayedRetryRedispatch.Begin(request);

        // Act & Assert - an outer behavior that re-enters the pipeline sees it again; another instance
        // (even an equal one, a nested send) and another type never do.
        DelayedRetryRedispatch.For(request).ShouldBeSameAs(scope.Marker);
        DelayedRetryRedispatch.For(request).ShouldBeSameAs(scope.Marker);
        DelayedRetryRedispatch.For(new ChainCommand(1)).ShouldBeNull();
        DelayedRetryRedispatch.For(new StructCommand(1)).ShouldBeNull();
    }

    [Fact]
    public void DelayedRetryRedispatch_MatchesAStructRequestByValue()
    {
        // Arrange
        using var scope = DelayedRetryRedispatch.Begin(new StructCommand(1));

        // Act & Assert - a boxed struct has no stable reference, so equal values match.
        DelayedRetryRedispatch.For(new StructCommand(1)).ShouldBeSameAs(scope.Marker);
        DelayedRetryRedispatch.For(new StructCommand(2)).ShouldBeNull();
    }

    [Fact]
    public void DelayedRetryRedispatch_AfterDispose_IsNoLongerMarked_AndKeepsTheReportedAttempt()
    {
        // Arrange
        var request = new ChainCommand(1);
        var scope = DelayedRetryRedispatch.Begin(request);
        var attempt = new RecoverabilityContext();
        attempt.RecordFailedAttempt(EncinaError.New("permanent failure"), null, ErrorClassification.Permanent);

        // Act
        scope.Marker.Report(attempt);
        scope.Dispose();

        // Assert
        scope.Marker.Classification.ShouldBe(ErrorClassification.Permanent);
        scope.Marker.Attempt.ShouldBeSameAs(attempt);
        DelayedRetryRedispatch.For(request).ShouldBeNull();
    }

    [Fact]
    public async Task OuterBehaviorReEnteringThePipeline_KeepsOneChain()
    {
        // Arrange - a retrying outer behavior calls the Recoverability behavior twice for the same request.
        var failures = new List<FailedMessage>();
        var scheduler = Substitute.For<IDelayedRetryScheduler>();
        var behavior = CreateBehavior<ChainCommand>(BehaviorOptions(failures), scheduler);
        var request = new ChainCommand(1);
        using var scope = DelayedRetryRedispatch.Begin(request);

        // Act
        for (var entry = 0; entry < 2; entry++)
        {
            await behavior.Handle(
                request,
                CreateRequestContext(),
                () => ValueTask.FromResult(Either<EncinaError, int>.Left(EncinaError.New("transient failure"))),
                CancellationToken.None);
        }

        // Assert - neither entry scheduled a chain of its own or failed the message.
        _ = scheduler.DidNotReceiveWithAnyArgs().ScheduleRetryAsync<ChainCommand>(default!, default!, default, default, default);
        failures.ShouldBeEmpty();
    }

    [Fact]
    public async Task NestedSendOfAnotherInstanceOfTheSameType_StartsItsOwnChain()
    {
        // Arrange - the handler of the re-dispatched request sends another ChainCommand.
        var failures = new List<FailedMessage>();
        var scheduler = Substitute.For<IDelayedRetryScheduler>();
        scheduler.ScheduleRetryAsync(Arg.Any<ChainCommand>(), Arg.Any<RecoverabilityContext>(), Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, Unit>>(Unit.Default));
        var behavior = CreateBehavior<ChainCommand>(BehaviorOptions(failures), scheduler);
        using var scope = DelayedRetryRedispatch.Begin(new ChainCommand(1));

        // Act
        var result = await behavior.Handle(
            new ChainCommand(1),
            CreateRequestContext(),
            () => ValueTask.FromResult(Either<EncinaError, int>.Left(EncinaError.New("transient failure"))),
            CancellationToken.None);

        // Assert - the nested send is a first-time failure: it scheduled attempt 0.
        result.IsLeft.ShouldBeTrue();
        _ = scheduler.Received(1).ScheduleRetryAsync(Arg.Any<ChainCommand>(), Arg.Any<RecoverabilityContext>(), Arg.Any<TimeSpan>(), 0, Arg.Any<CancellationToken>());
    }

    /// <summary>A value-type request: a boxed value has no stable reference identity.</summary>
    public readonly record struct StructCommand(int Value) : IRequest<int>;

    private static RecoverabilityPipelineBehavior<TRequest, int> CreateBehavior<TRequest>(
        RecoverabilityOptions options,
        IDelayedRetryScheduler? scheduler)
        where TRequest : IRequest<int> =>
        new(options, NullLogger<RecoverabilityPipelineBehavior<TRequest, int>>.Instance, scheduler);

    private static IRequestContext CreateRequestContext()
    {
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("chain-correlation");
        return context;
    }

    private static RecoverabilityOptions BehaviorOptions(List<FailedMessage> failures) => new()
    {
        ImmediateRetries = 0,
        UseJitter = false,
        EnableDelayedRetries = true,
        DelayedRetries = [TimeSpan.FromMilliseconds(1)],
        ErrorClassifier = new MarkerClassifier(),
        OnPermanentFailure = (failed, _) => { failures.Add(failed); return Task.CompletedTask; }
    };

    [Fact]
    public async Task StructRequestReDispatch_IsRecognised_AndSchedulesNoNewChain()
    {
        // Arrange
        var failures = new List<FailedMessage>();
        var scheduler = Substitute.For<IDelayedRetryScheduler>();
        var behavior = CreateBehavior<StructCommand>(BehaviorOptions(failures), scheduler);
        using var scope = DelayedRetryRedispatch.Begin(new StructCommand(1));

        // Act
        var result = await behavior.Handle(
            new StructCommand(1),
            CreateRequestContext(),
            () => ValueTask.FromResult(Either<EncinaError, int>.Left(EncinaError.New("transient failure"))),
            CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        _ = scheduler.DidNotReceiveWithAnyArgs().ScheduleRetryAsync<StructCommand>(default, default!, default, default, default);
        failures.ShouldBeEmpty();
        scope.Marker.Classification.ShouldBe(ErrorClassification.Transient);
    }

    [Fact]
    public async Task FirstScheduleReturningLeft_RunsThePermanentFailurePath_Once()
    {
        // Arrange
        var failures = new List<FailedMessage>();
        var scheduler = Substitute.For<IDelayedRetryScheduler>();
        scheduler.ScheduleRetryAsync(Arg.Any<ChainCommand>(), Arg.Any<RecoverabilityContext>(), Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, Unit>>(EncinaError.New("scheduler down")));
        var behavior = CreateBehavior<ChainCommand>(BehaviorOptions(failures), scheduler);

        // Act
        var result = await behavior.Handle(
            new ChainCommand(1),
            CreateRequestContext(),
            () => ValueTask.FromResult(Either<EncinaError, int>.Left(EncinaError.New("transient failure"))),
            CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        failures.Count.ShouldBe(1);
    }

    [Fact]
    public async Task FirstScheduleThrowing_RunsThePermanentFailurePath_Once()
    {
        // Arrange
        var failures = new List<FailedMessage>();
        var scheduler = Substitute.For<IDelayedRetryScheduler>();
        scheduler.ScheduleRetryAsync(Arg.Any<ChainCommand>(), Arg.Any<RecoverabilityContext>(), Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<Task<Either<EncinaError, Unit>>>(_ => throw new InvalidOperationException("patient 12345"));
        var behavior = CreateBehavior<ChainCommand>(BehaviorOptions(failures), scheduler);

        // Act
        var result = await behavior.Handle(
            new ChainCommand(1),
            CreateRequestContext(),
            () => ValueTask.FromResult(Either<EncinaError, int>.Left(EncinaError.New("transient failure"))),
            CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        failures.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ExceptionFromTheHandler_NeverPutsItsMessageInTheError()
    {
        // Arrange
        var behavior = CreateBehavior<ChainCommand>(
            new RecoverabilityOptions { ImmediateRetries = 0, EnableDelayedRetries = false, ErrorClassifier = new MarkerClassifier() },
            scheduler: null);

        // Act
        var result = await behavior.Handle(
            new ChainCommand(1),
            CreateRequestContext(),
            () => throw new InvalidOperationException("patient 12345 not found"),
            CancellationToken.None);

        // Assert
        var error = result.Match(Right: _ => throw new InvalidOperationException("Expected Left"), Left: e => e);
        error.Message.ShouldNotContain("12345");
    }

    private sealed class OddFailureException(string message) : Exception(message);

    [Theory]
    [InlineData("connection reset by peer")]
    [InlineData("invalid state of the thing")]
    public void ThrownException_IsClassifiedExactlyAsBeforeTheMessageWasDropped(string exceptionMessage)
    {
        // The default classifier ignores message patterns for an error that has a cause (documented
        // contract of DefaultErrorClassifier), so building the error without the exception's message
        // changes no classification: a thrown exception is classified by its type, then by code.
        var ex = new OddFailureException(exceptionMessage);
        var classifier = new DefaultErrorClassifier();

        var before = classifier.Classify(EncinaError.New(ex, $"[{RecoverabilityErrorCodes.ExceptionThrown}] {ex.Message}"), ex);
        var after = classifier.Classify(EncinaErrors.Create(RecoverabilityErrorCodes.ExceptionThrown, ex.GetType().Name, ex), ex);

        after.ShouldBe(before);
        after.ShouldBe(ErrorClassification.Unknown);
    }

    [Fact]
    public async Task SchedulerThrowingForTheNextAttempt_EndsTheChainThroughThePermanentFailurePath_Once()
    {
        // Arrange
        var fixture = CreateFixture(delayedRetries: 3, failure: "transient failure", failNextSchedule: true, throwNextSchedule: true);

        // Act
        await fixture.FirstFailureAsync();
        await fixture.RunProcessorUntilPermanentFailureAsync();

        // Assert
        fixture.PermanentFailures.Count.ShouldBe(1);
        fixture.Store.AttemptsAdded.ShouldBe([0]);
        fixture.Store.FailedCount.ShouldBe(1);
    }

    [Fact]
    public async Task ShutdownDuringTheReDispatch_LeavesTheRowPending()
    {
        // Arrange - the host token is cancelled while the retried request is being dispatched.
        var store = new FakeStore();
        var row = new FakeRow(new DelayedRetryMessageData(
            Guid.NewGuid(),
            Guid.NewGuid(),
            typeof(ChainCommand).AssemblyQualifiedName!,
            "{\"value\":1}",
            "{\"id\":\"00000000-0000-0000-0000-000000000000\",\"immediateRetryCount\":0,\"delayedRetryCount\":0}",
            0,
            DateTime.UtcNow,
            DateTime.UtcNow,
            "chain-correlation"));
        await store.AddAsync(row);

        using var cts = new CancellationTokenSource();
        var dispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<IRequest<int>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                dispatched.TrySetResult();
                return new ValueTask<Either<EncinaError, int>>(
                    Either<EncinaError, int>.Left(EncinaError.New("[recoverability.cancelled] Operation was cancelled")));
            });

        var failures = new List<FailedMessage>();
        var options = new RecoverabilityOptions
        {
            DelayedRetries = [TimeSpan.FromMilliseconds(1)],
            OnPermanentFailure = (failed, _) => { failures.Add(failed); return Task.CompletedTask; }
        };

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IDelayedRetryStore)).Returns(store);
        serviceProvider.GetService(typeof(IEncina)).Returns(encina);
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);
        var processor = new DelayedRetryProcessor(scopeFactory, options, NullLogger<DelayedRetryProcessor>.Instance)
        {
            ProcessingInterval = TimeSpan.FromMilliseconds(5)
        };

        // Act
        await processor.StartAsync(cts.Token);
        await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await processor.StopAsync(default);

        // Assert - neither consumed nor failed nor dead-lettered.
        row.IsPending.ShouldBeTrue();
        failures.ShouldBeEmpty();
    }

    [Fact]
    public void RestoreChain_KeepsTheOriginalIdAndStartTime()
    {
        // Arrange
        var context = new RecoverabilityContext();
        var id = Guid.NewGuid();
        var startedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        context.RestoreChain(id, startedAtUtc);

        // Assert
        context.Id.ShouldBe(id);
        context.StartedAtUtc.ShouldBe(startedAtUtc);
    }

    private static ChainFixture CreateFixture(
        int delayedRetries,
        string failure,
        string? delayedFailure = null,
        bool failNextSchedule = false,
        bool throwNextSchedule = false,
        DeadLetterSourceCapture? deadLetterCapture = null) =>
        new(delayedRetries, failure, delayedFailure ?? failure, failNextSchedule, throwNextSchedule, deadLetterCapture);

    [Fact]
    public async Task DeadLetterQueue_ChainThroughEveryDelayedRetry_CapturesOnceUnderTheChainId()
    {
        // Arrange
        using var host = DeadLetterCaptureHost.Create();
        var fixture = CreateFixture(delayedRetries: 2, failure: "transient failure", deadLetterCapture: host.Capture);

        // Act - the first failure schedules a delayed retry: no dead letter yet.
        await fixture.FirstFailureAsync();
        host.Store.GetMessages().ShouldBeEmpty();
        await fixture.RunProcessorUntilPermanentFailureAsync();

        // Assert
        var deadLetter = host.DeadLettersOf(DeadLetterSourcePatterns.Recoverability).ShouldHaveSingleItem();
        deadLetter.SourceMessageId.ShouldBe(fixture.Store.ContextIdsAdded[0].ToString("D"));
        deadLetter.RequestType.ShouldContain(nameof(ChainCommand));
    }

    [Fact]
    public async Task DeadLetterQueue_FlagOff_ChainThroughEveryDelayedRetry_CapturesNothing()
    {
        // Arrange
        using var host = DeadLetterCaptureHost.Create(o => o.IntegrateWithRecoverability = false);
        var fixture = CreateFixture(delayedRetries: 1, failure: "transient failure", deadLetterCapture: host.Capture);

        // Act
        await fixture.FirstFailureAsync();
        await fixture.RunProcessorUntilPermanentFailureAsync();

        // Assert
        fixture.PermanentFailures.Count.ShouldBe(1);
        host.Store.GetMessages().ShouldBeEmpty();
    }

    [Fact]
    public async Task DeadLetterQueue_CaptureFails_LeavesTheRowPendingAndSkipsOnPermanentFailure()
    {
        // Arrange - the dead letter store is down.
        var deadLetterStore = Substitute.For<IDeadLetterStore>();
        deadLetterStore.AddAsync(Arg.Any<IDeadLetterMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, bool>>(EncinaErrors.Create(DeadLetterErrorCodes.StoreFailed, "down")));
        using var host = DeadLetterCaptureHost.Create(store: deadLetterStore);
        var store = new FakeStore();
        var row = CreateRow("No.Such.Type, NoSuchAssembly", "{}");
        await store.AddAsync(row);
        var failures = new List<FailedMessage>();
        var options = new RecoverabilityOptions
        {
            DelayedRetries = [TimeSpan.FromMilliseconds(1)],
            OnPermanentFailure = (message, _) => { failures.Add(message); return Task.CompletedTask; }
        };
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IDelayedRetryStore)).Returns(store);
        serviceProvider.GetService(typeof(IEncina)).Returns(Substitute.For<IEncina>());
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);
        var processor = new DelayedRetryProcessor(
            scopeFactory, options, NullLogger<DelayedRetryProcessor>.Instance, deadLetterCapture: host.Capture)
        {
            ProcessingInterval = TimeSpan.FromMilliseconds(5)
        };

        // Act - several cycles run while the capture keeps failing.
        using var cts = new CancellationTokenSource();
        await processor.StartAsync(cts.Token);
        try
        {
            await store.WaitForPollsAsync(3).WaitAsync(TimeSpan.FromSeconds(20));
        }
        finally
        {
            await cts.CancelAsync();
            await processor.StopAsync(default);
        }

        // Assert - never failed without its dead letter, and the callback waits for the capture.
        row.IsPending.ShouldBeTrue();
        failures.ShouldBeEmpty();
        await deadLetterStore.Received().AddAsync(Arg.Any<IDeadLetterMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnPermanentFailureThrowing_IsLogged_AndTheRowStaysFailed()
    {
        // Arrange - the callback records the call and then throws.
        var store = new FakeStore();
        var row = CreateRow("No.Such.Type, NoSuchAssembly", "{}");
        await store.AddAsync(row);
        var called = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var options = new RecoverabilityOptions
        {
            DelayedRetries = [TimeSpan.FromMilliseconds(1)],
            OnPermanentFailure = (_, _) =>
            {
                called.TrySetResult();
                throw new InvalidOperationException("callback crashed");
            }
        };
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IDelayedRetryStore)).Returns(store);
        serviceProvider.GetService(typeof(IEncina)).Returns(Substitute.For<IEncina>());
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);
        var processor = new DelayedRetryProcessor(scopeFactory, options, NullLogger<DelayedRetryProcessor>.Instance)
        {
            ProcessingInterval = TimeSpan.FromMilliseconds(5)
        };

        // Act
        using var cts = new CancellationTokenSource();
        await processor.StartAsync(cts.Token);
        try
        {
            await called.Task.WaitAsync(TimeSpan.FromSeconds(20));
            await store.WaitForPollsAsync(2).WaitAsync(TimeSpan.FromSeconds(20));
        }
        finally
        {
            await cts.CancelAsync();
            await processor.StopAsync(default);
        }

        // Assert - the throwing callback does not bring the row back.
        row.IsPending.ShouldBeFalse();
        store.FailedCount.ShouldBe(1);
    }

    [Fact]
    public async Task DeadLetterQueue_CaptureRejected_FailsTheRowAndRunsOnPermanentFailure()
    {
        // Arrange - a stored type name with edge white space: the dead letter queue rejects it.
        using var host = DeadLetterCaptureHost.Create();
        var row = CreateRow("No.Such.Type ", "{}");

        // Act
        var failures = await RunRowToPermanentFailureAsync(row, serializer: null, host.Capture);

        // Assert - no loop: the chain ends once, without a dead letter.
        failures.Count.ShouldBe(1);
        row.IsPending.ShouldBeFalse();
        host.Store.GetMessages().ShouldBeEmpty();
    }

    [Fact]
    public async Task DeadLetterQueue_UnknownRequestType_CapturesTheStoredTypeNameAndContent()
    {
        // Arrange
        using var host = DeadLetterCaptureHost.Create();
        var row = CreateRow("No.Such.Type, NoSuchAssembly", "{\"stored\":true}");

        // Act
        var failures = await RunRowToPermanentFailureAsync(row, serializer: null, host.Capture);

        // Assert
        failures.Count.ShouldBe(1);
        var deadLetter = host.DeadLettersOf(DeadLetterSourcePatterns.Recoverability).ShouldHaveSingleItem();
        deadLetter.RequestType.ShouldBe("No.Such.Type, NoSuchAssembly");
        deadLetter.RequestContent.ShouldBe("{\"stored\":true}");
        deadLetter.SourceMessageId.ShouldBe(failures[0].Id.ToString("D"));
    }

    // Succeeds for the first schedule (attempt 0) and fails (or throws on) every later one.
    private sealed class FailingNextScheduler(IDelayedRetryScheduler inner, bool throws) : IDelayedRetryScheduler
    {
        public Task<Either<EncinaError, Unit>> ScheduleRetryAsync<TRequest>(
            TRequest request,
            RecoverabilityContext context,
            TimeSpan delay,
            int delayedRetryAttempt,
            CancellationToken cancellationToken = default)
            where TRequest : notnull
        {
            if (delayedRetryAttempt == 0)
            {
                return inner.ScheduleRetryAsync(request, context, delay, delayedRetryAttempt, cancellationToken);
            }

            return throws
                ? throw new InvalidOperationException("scheduler down for patient 12345")
                : Task.FromResult<Either<EncinaError, Unit>>(EncinaError.New("scheduler down"));
        }

        public Task<Either<EncinaError, Unit>> CancelScheduledRetryAsync(Guid recoverabilityContextId, CancellationToken cancellationToken = default) =>
            inner.CancelScheduledRetryAsync(recoverabilityContextId, cancellationToken);
    }

    private sealed class ChainFixture
    {
        private readonly RecoverabilityPipelineBehavior<ChainCommand, int> _behavior;
        private readonly RecoverabilityOptions _options;
        private readonly TaskCompletionSource _permanentlyFailed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly string _firstFailure;
        private readonly string _delayedFailure;
        private readonly DeadLetterSourceCapture? _deadLetterCapture;
        private int _calls;

        public ChainFixture(
            int delayedRetries,
            string firstFailure,
            string delayedFailure,
            bool failNextSchedule,
            bool throwNextSchedule,
            DeadLetterSourceCapture? deadLetterCapture)
        {
            _deadLetterCapture = deadLetterCapture;
            _firstFailure = firstFailure;
            _delayedFailure = delayedFailure;
            Store = new FakeStore();

            _options = new RecoverabilityOptions
            {
                ImmediateRetries = 1,
                ImmediateRetryDelay = TimeSpan.FromMilliseconds(1),
                UseJitter = false,
                EnableDelayedRetries = true,
                DelayedRetries = Enumerable.Repeat(TimeSpan.FromMilliseconds(1), delayedRetries).ToArray(),
                ErrorClassifier = new MarkerClassifier(),
                OnPermanentFailure = (failed, _) =>
                {
                    PermanentFailures.Add(failed);
                    _permanentlyFailed.TrySetResult();
                    return Task.CompletedTask;
                }
            };

            IDelayedRetryScheduler scheduler = new DelayedRetryScheduler(
                Store,
                new FakeFactory(),
                NullLogger<DelayedRetryScheduler>.Instance,
                new JsonMessageSerializer());
            Scheduler = failNextSchedule ? new FailingNextScheduler(scheduler, throwNextSchedule) : scheduler;

            _behavior = new RecoverabilityPipelineBehavior<ChainCommand, int>(
                _options,
                NullLogger<RecoverabilityPipelineBehavior<ChainCommand, int>>.Instance,
                Scheduler,
                deadLetterCapture: deadLetterCapture);
        }

        public FakeStore Store { get; }

        public IDelayedRetryScheduler Scheduler { get; }

        public List<FailedMessage> PermanentFailures { get; } = [];

        public async Task<Either<EncinaError, int>> FirstFailureAsync()
        {
            var context = Substitute.For<IRequestContext>();
            context.CorrelationId.Returns("chain-correlation");
            var request = new ChainCommand(7);

            return await _behavior.Handle(request, context, () => HandlerAsync(isFirst: true), CancellationToken.None);
        }

        public async Task RunProcessorUntilPermanentFailureAsync()
        {
            var encina = Substitute.For<IEncina>();
            encina.Send(Arg.Any<IRequest<int>>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var context = Substitute.For<IRequestContext>();
                    context.CorrelationId.Returns("chain-correlation");
                    return _behavior.Handle(
                        (ChainCommand)call.Arg<IRequest<int>>(),
                        context,
                        () => HandlerAsync(isFirst: false),
                        CancellationToken.None);
                });

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IDelayedRetryStore)).Returns(Store);
            serviceProvider.GetService(typeof(IEncina)).Returns(encina);
            serviceProvider.GetService(typeof(IDelayedRetryScheduler)).Returns(Scheduler);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            var processor = new DelayedRetryProcessor(
                scopeFactory, _options, NullLogger<DelayedRetryProcessor>.Instance, deadLetterCapture: _deadLetterCapture)
            {
                ProcessingInterval = TimeSpan.FromMilliseconds(5)
            };

            using var cts = new CancellationTokenSource();
            await processor.StartAsync(cts.Token);
            try
            {
                await _permanentlyFailed.Task.WaitAsync(TimeSpan.FromSeconds(20));
                // Let two further processing cycles run, so a wrongly scheduled row would be polled.
                await Store.WaitForPollsAsync(2).WaitAsync(TimeSpan.FromSeconds(20));
            }
            finally
            {
                await cts.CancelAsync();
                await processor.StopAsync(default);
            }
        }

        private ValueTask<Either<EncinaError, int>> HandlerAsync(bool isFirst)
        {
            Interlocked.Increment(ref _calls);
            var message = isFirst ? _firstFailure : _delayedFailure;
            return ValueTask.FromResult(Either<EncinaError, int>.Left(EncinaError.New(message)));
        }
    }
}
