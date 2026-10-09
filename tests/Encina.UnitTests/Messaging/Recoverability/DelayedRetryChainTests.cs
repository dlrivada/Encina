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
    public async Task DelayedRetryRedispatch_DoesNotLeakToOtherRequests()
    {
        // Arrange - a marker bound to one instance must not apply to another request.
        var marked = new ChainCommand(1);
        var other = new ChainCommand(1);

        // Act
        using var scope = DelayedRetryRedispatch.Begin(marked);

        // Assert
        DelayedRetryRedispatch.For(marked).ShouldNotBeNull();
        DelayedRetryRedispatch.For(other).ShouldBeNull();
        await Task.CompletedTask;
    }

    [Fact]
    public void DelayedRetryRedispatch_AfterDispose_IsNoLongerMarked()
    {
        // Arrange
        var request = new ChainCommand(1);

        // Act
        var scope = DelayedRetryRedispatch.Begin(request);
        scope.Marker.Report(ErrorClassification.Permanent);
        scope.Dispose();

        // Assert
        scope.Marker.Classification.ShouldBe(ErrorClassification.Permanent);
        DelayedRetryRedispatch.For(request).ShouldBeNull();
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

    private static ChainFixture CreateFixture(int delayedRetries, string failure, string? delayedFailure = null, bool failNextSchedule = false) =>
        new(delayedRetries, failure, delayedFailure ?? failure, failNextSchedule);

    // Succeeds for the first schedule (attempt 0) and fails every later one.
    private sealed class FailingNextScheduler(IDelayedRetryScheduler inner) : IDelayedRetryScheduler
    {
        public Task<Either<EncinaError, Unit>> ScheduleRetryAsync<TRequest>(
            TRequest request,
            RecoverabilityContext context,
            TimeSpan delay,
            int delayedRetryAttempt,
            CancellationToken cancellationToken = default)
            where TRequest : notnull =>
            delayedRetryAttempt == 0
                ? inner.ScheduleRetryAsync(request, context, delay, delayedRetryAttempt, cancellationToken)
                : Task.FromResult<Either<EncinaError, Unit>>(EncinaError.New("scheduler down"));

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

        public ChainFixture(int delayedRetries, string firstFailure, string delayedFailure, bool failNextSchedule)
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
            Scheduler = failNextSchedule ? new FailingNextScheduler(scheduler) : scheduler;

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
