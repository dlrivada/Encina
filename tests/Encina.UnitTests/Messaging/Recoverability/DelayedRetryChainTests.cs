using Encina.Messaging.Recoverability;
using Encina.Messaging.Serialization;

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
    public void DelayedRetryRedispatch_IsConsumedOnce_AndOnlyForItsRequestType()
    {
        // Arrange
        using var scope = DelayedRetryRedispatch.Begin(typeof(ChainCommand));

        // Act & Assert - another type never takes it; the first behavior takes it; a nested send does not.
        DelayedRetryRedispatch.Consume(typeof(StructCommand)).ShouldBeNull();
        DelayedRetryRedispatch.Consume(typeof(ChainCommand)).ShouldBeSameAs(scope.Marker);
        DelayedRetryRedispatch.Consume(typeof(ChainCommand)).ShouldBeNull();
    }

    [Fact]
    public void DelayedRetryRedispatch_AfterDispose_IsNoLongerMarked()
    {
        // Arrange
        var scope = DelayedRetryRedispatch.Begin(typeof(ChainCommand));

        // Act
        scope.Marker.Report(ErrorClassification.Permanent);
        scope.Dispose();

        // Assert
        scope.Marker.Classification.ShouldBe(ErrorClassification.Permanent);
        DelayedRetryRedispatch.Consume(typeof(ChainCommand)).ShouldBeNull();
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
        using var scope = DelayedRetryRedispatch.Begin(typeof(StructCommand));

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
        bool throwNextSchedule = false) =>
        new(delayedRetries, failure, delayedFailure ?? failure, failNextSchedule, throwNextSchedule);

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
        private int _calls;

        public ChainFixture(int delayedRetries, string firstFailure, string delayedFailure, bool failNextSchedule, bool throwNextSchedule)
        {
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
                Scheduler);
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

            var processor = new DelayedRetryProcessor(scopeFactory, _options, NullLogger<DelayedRetryProcessor>.Instance)
            {
                ProcessingInterval = TimeSpan.FromMilliseconds(5)
            };

            using var cts = new CancellationTokenSource();
            await processor.StartAsync(cts.Token);
            try
            {
                await _permanentlyFailed.Task.WaitAsync(TimeSpan.FromSeconds(20));
                // Let any further (wrong) processing cycle show up before asserting.
                await Task.Delay(100);
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
