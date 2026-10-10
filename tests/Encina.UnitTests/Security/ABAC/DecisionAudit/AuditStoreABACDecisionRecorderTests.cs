using System.Transactions;

using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.Audit;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.ABAC.DecisionAudit;

/// <summary>
/// Unit tests for <see cref="AuditStoreABACDecisionRecorder"/> (#751 Phase 3): isolated scope,
/// suppressed ambient transaction, a write never linked to the caller's token, the
/// <see cref="ABACDecisionAuditOptions.WriteTimeout"/> race against a store that ignores its token,
/// and the idempotent re-check of an ambiguous failure.
/// </summary>
public sealed class AuditStoreABACDecisionRecorderTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly TimerSignallingTimeProvider _time = new(new DateTimeOffset(2026, 10, 9, 8, 0, 0, TimeSpan.Zero));
    private readonly FakeLogCollector _logs = new();

    /// <summary>
    /// A fake clock that signals every timer created on it, so a test advances time only once the
    /// timers it means to fire exist (deterministic, no polling).
    /// </summary>
    private sealed class TimerSignallingTimeProvider(DateTimeOffset start) : FakeTimeProvider(start)
    {
        private readonly global::System.Threading.Channels.Channel<bool> _created = global::System.Threading.Channels.Channel.CreateUnbounded<bool>();

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = base.CreateTimer(callback, state, dueTime, period);
            _created.Writer.TryWrite(true);
            return timer;
        }

        public async Task WaitForTimersAsync(int count)
        {
            for (var i = 0; i < count; i++)
            {
                await _created.Reader.ReadAsync(TestContext.Current.CancellationToken);
            }
        }
    }

    /// <summary>A scriptable store: each call runs the delegate the test sets.</summary>
    private sealed class ScriptedStore : IOperationAuditStore
    {
        public Func<OperationAuditEntry, CancellationToken, ValueTask<Either<EncinaError, Unit>>> Record { get; set; } =
            (_, _) => ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));

        public Func<string, CancellationToken, ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>>> ByCorrelation { get; set; } =
            (_, _) => ValueTask.FromResult(Right<EncinaError, IReadOnlyList<OperationAuditEntry>>([]));

        public List<OperationAuditEntry> Written { get; } = [];

        public Transaction? AmbientDuringWrite { get; private set; }

        public CancellationToken WriteToken { get; private set; }

        public ValueTask<Either<EncinaError, Unit>> RecordAsync(OperationAuditEntry entry, CancellationToken cancellationToken = default)
        {
            Written.Add(entry);
            AmbientDuringWrite = Transaction.Current;
            WriteToken = cancellationToken;
            return Record(entry, cancellationToken);
        }

        public ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default) =>
            ByCorrelation(correlationId, cancellationToken);

        public ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByEntityAsync(string entityType, string? entityId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByUserAsync(string userId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask<Either<EncinaError, PagedResult<OperationAuditEntry>>> QueryAsync(OperationAuditQuery query, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(DateTime olderThanUtc, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private AuditStoreABACDecisionRecorder Recorder(ScriptedStore? store, List<IServiceProvider>? scopes = null)
    {
        var services = new ServiceCollection();
        if (store is not null)
        {
            services.AddScoped<IOperationAuditStore>(sp =>
            {
                scopes?.Add(sp);
                return store;
            });
        }

        var options = new ABACOptions();
        options.DecisionAudit.WriteTimeout = Timeout;

        return new AuditStoreABACDecisionRecorder(
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            _time,
            new FakeLogger<AuditStoreABACDecisionRecorder>(_logs));
    }

    private static ABACDecisionRecord Record() => new()
    {
        DecisionId = Guid.CreateVersion7(),
        UserId = "user-1",
        IdentityKind = IdentityKind.User,
        CorrelationId = "corr-1",
        RequestType = "GetOrderQuery",
        EnforcedOutcome = ABACEnforcedOutcome.Granted,
        ReasonCode = ABACDecisionAuditSchema.PermitReasonCode,
        EnforcementMode = ABACEnforcementMode.Block,
        StartedAtUtc = new DateTimeOffset(2026, 10, 9, 8, 0, 0, TimeSpan.Zero),
        CompletedAtUtc = new DateTimeOffset(2026, 10, 9, 8, 0, 1, TimeSpan.Zero)
    };

    private static ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> Found(OperationAuditEntry entry) =>
        ValueTask.FromResult(Right<EncinaError, IReadOnlyList<OperationAuditEntry>>([entry]));

    private static string Code(Either<EncinaError, Unit> result) =>
        result.Match(Right: _ => "<right>", Left: error => error.GetCode().IfNone("<none>"));

    [Fact]
    public async Task RecordAsync_WritesTheMappedEntryThroughAStoreOfItsOwnScope()
    {
        var store = new ScriptedStore();
        var scopes = new List<IServiceProvider>();
        var recorder = Recorder(store, scopes);
        var record = Record();

        var first = await recorder.RecordAsync(record);
        var second = await recorder.RecordAsync(record with { DecisionId = Guid.CreateVersion7() });

        first.IsRight.ShouldBeTrue();
        second.IsRight.ShouldBeTrue();
        var entry = store.Written[0];
        entry.Id.ShouldBe(record.DecisionId);
        entry.Action.ShouldBe(ABACDecisionAuditSchema.Action);
        entry.EntityType.ShouldBe("GetOrderQuery");
        scopes.Count.ShouldBe(2);
        scopes[0].ShouldNotBeSameAs(scopes[1]);
    }

    [Fact]
    public async Task RecordAsync_InsideAnAmbientTransaction_WritesWithTheTransactionSuppressed()
    {
        var store = new ScriptedStore();
        var recorder = Recorder(store);

        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            Transaction.Current.ShouldNotBeNull();
            (await recorder.RecordAsync(Record())).IsRight.ShouldBeTrue();
        }

        store.Written.ShouldHaveSingleItem();
        store.AmbientDuringWrite.ShouldBeNull();
    }

    [Fact]
    public async Task RecordAsync_WithoutAStore_ReturnsStoreUnavailable()
    {
        var result = await Recorder(store: null).RecordAsync(Record());

        Code(result).ShouldBe(ABACErrors.DecisionAuditStoreUnavailableCode);
    }

    [Fact]
    public async Task RecordAsync_NullRecord_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () => await Recorder(new ScriptedStore()).RecordAsync(null!));
    }

    [Fact]
    public async Task RecordAsync_CallerAlreadyCancelled_WritesNothing()
    {
        var store = new ScriptedStore();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await Recorder(store).RecordAsync(Record(), new CancellationToken(canceled: true)));

        store.Written.ShouldBeEmpty();
    }

    [Fact]
    public async Task RecordAsync_CallerCancelsDuringTheWrite_TheWriteIsNotAborted()
    {
        var release = new TaskCompletionSource<Either<EncinaError, Unit>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new ScriptedStore { Record = (_, _) => new ValueTask<Either<EncinaError, Unit>>(release.Task) };
        using var caller = new CancellationTokenSource();

        var pending = Recorder(store).RecordAsync(Record(), caller.Token).AsTask();
        await caller.CancelAsync();

        store.WriteToken.ShouldNotBe(caller.Token);
        store.WriteToken.IsCancellationRequested.ShouldBeFalse();
        release.SetResult(Unit.Default);
        (await pending).IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RecordAsync_StoreFailsAndTheEntryIsAbsent_ReturnsTheStoreError()
    {
        var store = new ScriptedStore
        {
            Record = (_, _) => ValueTask.FromResult(Left<EncinaError, Unit>(EncinaErrors.Create("store.down", "Secret detail")))
        };

        var result = await Recorder(store).RecordAsync(Record());

        Code(result).ShouldBe("store.down");
        result.IfLeft(error =>
        {
            error.Message.ShouldNotContain("Secret detail");
            error.Exception.Map(exception => exception.ToString()).IfNone(string.Empty).ShouldNotContain("Secret detail");
        });
    }

    [Fact]
    public async Task RecordAsync_StoreFailsButTheEntryWasCommitted_CountsAsWritten()
    {
        var store = new ScriptedStore
        {
            Record = (_, _) => ValueTask.FromResult(Left<EncinaError, Unit>(EncinaErrors.Create("store.timeout", "late"))),
        };
        store.ByCorrelation = (_, _) => Found(store.Written[0]);

        var result = await Recorder(store).RecordAsync(Record());

        result.IsRight.ShouldBeTrue();
        _logs.GetSnapshot().ShouldContain(log => log.Id.Id == 9089 && log.Level == LogLevel.Debug);
    }

    [Fact]
    public async Task RecordAsync_StoreFailsAndTheLookupFindsAnotherEntry_ReturnsTheStoreError()
    {
        var store = new ScriptedStore
        {
            Record = (_, _) => ValueTask.FromResult(Left<EncinaError, Unit>(EncinaErrors.Create("store.down", "x")))
        };
        store.ByCorrelation = (_, _) => Found(store.Written[0] with { Id = Guid.NewGuid() });

        Code(await Recorder(store).RecordAsync(Record())).ShouldBe("store.down");
    }

    [Fact]
    public async Task RecordAsync_StoreFailsAndTheLookupFails_ReturnsTheStoreError()
    {
        var store = new ScriptedStore
        {
            Record = (_, _) => ValueTask.FromResult(Left<EncinaError, Unit>(EncinaErrors.Create("store.down", "x"))),
            ByCorrelation = (_, _) => ValueTask.FromResult(Left<EncinaError, IReadOnlyList<OperationAuditEntry>>(EncinaErrors.Create("store.read", "Secret read detail")))
        };

        Code(await Recorder(store).RecordAsync(Record())).ShouldBe("store.down");
        var log = _logs.GetSnapshot().ShouldHaveSingleItem();
        log.Id.Id.ShouldBe(9099);
        log.Message.ShouldContain("store.down");
        log.Message.ShouldContain("store.read");
        log.Message.ShouldNotContain("Secret read detail");
    }

    [Fact]
    public async Task RecordAsync_StoreThrowsAndTheEntryIsAbsent_RethrowsTheException()
    {
        var store = new ScriptedStore
        {
            Record = (_, _) => throw new InvalidOperationException("store exploded"),
            ByCorrelation = (_, _) => throw new InvalidOperationException("lookup exploded")
        };

        await Should.ThrowAsync<InvalidOperationException>(async () => await Recorder(store).RecordAsync(Record()));
        var log = _logs.GetSnapshot().ShouldHaveSingleItem();
        log.Id.Id.ShouldBe(9099);
        log.Message.ShouldContain(nameof(InvalidOperationException));
        log.Message.ShouldNotContain("exploded");
    }

    [Fact]
    public async Task RecordAsync_StoreThrowsButTheEntryWasCommitted_CountsAsWritten()
    {
        var store = new ScriptedStore { Record = (_, _) => throw new InvalidOperationException("connection reset after commit") };
        store.ByCorrelation = (_, _) => Found(store.Written[0]);

        (await Recorder(store).RecordAsync(Record())).IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RecordAsync_StoreIgnoresItsToken_TheWriteTimeoutStillBoundsTheCall()
    {
        // The store never completes and never observes its token: only the WaitAsync race ends the call.
        var hung = new TaskCompletionSource<Either<EncinaError, Unit>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var lookupHung = new TaskCompletionSource<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new ScriptedStore
        {
            Record = (_, _) => new ValueTask<Either<EncinaError, Unit>>(hung.Task),
            ByCorrelation = (_, _) => new ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>>(lookupHung.Task)
        };

        var pending = Recorder(store).RecordAsync(Record()).AsTask();
        pending.IsCompleted.ShouldBeFalse();

        // Each bounded call creates two timers on the clock: its token source and its WaitAsync race.
        await _time.WaitForTimersAsync(2);
        _time.Advance(Timeout);
        await _time.WaitForTimersAsync(2);
        _time.Advance(Timeout);

        await Should.ThrowAsync<TimeoutException>(pending);
        _logs.GetSnapshot().ShouldContain(log => log.Id.Id == 9099);
        hung.SetException(new InvalidOperationException("late fault is observed, not unobserved"));
    }

    [Fact]
    public async Task RecordAsync_StoreObservesItsToken_TheCancellationIsReportedAsATimeout()
    {
        var store = new ScriptedStore
        {
            Record = async (_, token) =>
            {
                await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, token);
                return Unit.Default;
            }
        };

        var pending = Recorder(store).RecordAsync(Record()).AsTask();
        _time.Advance(Timeout);

        await Should.ThrowAsync<TimeoutException>(pending);
    }

    [Fact]
    public async Task RecordAsync_StoreObservesItsTokenAndReturnsLeft_IsClassifiedAsATimeout()
    {
        // A store like EF Core reports a cancelled save as a Left ("Operation was cancelled", no
        // code) instead of throwing: once the bound fired it is the same timeout, not encina.unknown.
        var store = new ScriptedStore
        {
            Record = async (_, token) =>
            {
                try
                {
                    await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, token);
                    return Unit.Default;
                }
                catch (OperationCanceledException)
                {
                    return EncinaError.New("Operation was cancelled");
                }
            }
        };

        var pending = Recorder(store).RecordAsync(Record()).AsTask();
        _time.Advance(Timeout);

        await Should.ThrowAsync<TimeoutException>(pending);
    }

    [Fact]
    public async Task RecordAsync_TimedOutWriteThatWasCommitted_CountsAsWritten()
    {
        var hung = new TaskCompletionSource<Either<EncinaError, Unit>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new ScriptedStore { Record = (_, _) => new ValueTask<Either<EncinaError, Unit>>(hung.Task) };
        store.ByCorrelation = (_, _) => Found(store.Written[0]);

        var pending = Recorder(store).RecordAsync(Record()).AsTask();
        _time.Advance(Timeout);

        (await pending).IsRight.ShouldBeTrue();
    }
}
