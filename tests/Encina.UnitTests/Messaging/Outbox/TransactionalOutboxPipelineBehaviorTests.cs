#pragma warning disable CA2012 // NSubstitute configures a ValueTask-returning member by calling it inside Returns(...).

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Encina.Messaging.Diagnostics;
using Encina.Messaging.Outbox;
using Encina.Messaging.Serialization;
using Encina.Messaging.Transactions;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Messaging.Outbox;

/// <summary>
/// Unit tests for <see cref="OutboxWriter"/> and <see cref="TransactionalOutboxPipelineBehavior{TRequest, TResponse}"/>:
/// the skip path, the begin-or-join flow, rollback on a handler Left, a write Left, an exception or a commit Left,
/// and the enqueue activity and counter.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TransactionalOutboxPipelineBehaviorTests
{
    private sealed record Plain : IRequest<string>;

    private sealed record WithNotifications(params INotification[] Items) : IRequest<string>, IHasNotifications
    {
        public IEnumerable<INotification> GetNotifications() => Items;
    }

    private sealed record Note(string Text) : INotification;

    private readonly ITransactionLeaseSource _source = Substitute.For<ITransactionLeaseSource>();
    private readonly ITransactionLease _lease = Substitute.For<ITransactionLease>();
    private readonly IOutboxStore _store = Substitute.For<IOutboxStore>();
    private readonly IOutboxMessageFactory _factory = Substitute.For<IOutboxMessageFactory>();
    private readonly IRequestContext _context = Substitute.For<IRequestContext>();

    public TransactionalOutboxPipelineBehaviorTests()
    {
        _lease.IsOwner.Returns(true);
        _source.HasActiveTransaction.Returns(true);
        _lease.CommitAsync(Arg.Any<CancellationToken>()).Returns(new ValueTask<Either<EncinaError, Unit>>(Unit.Default));
        _source.BeginOrJoinAsync(Arg.Any<CancellationToken>()).Returns(
            new ValueTask<Either<EncinaError, ITransactionLease>>(Either<EncinaError, ITransactionLease>.Right(_lease)));
        _store.AddAsync(Arg.Any<IOutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, Unit>>(Unit.Default));
        _store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, Unit>>(Unit.Default));
        _factory.Create(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(_ => Substitute.For<IOutboxMessage>());
    }

    private TransactionalOutboxPipelineBehavior<TRequest, string> Create<TRequest>()
        where TRequest : IRequest<string> =>
        new(_source, _store, _factory, new JsonMessageSerializer(),
            NullLogger<TransactionalOutboxPipelineBehavior<TRequest, string>>.Instance);

    private static RequestHandlerCallback<string> Ok() =>
        () => ValueTask.FromResult<Either<EncinaError, string>>("done");

    private static string Code<T>(Either<EncinaError, T> either) => ((EncinaError)either).GetCode().IfNone(string.Empty);

    [Fact]
    public void Constructor_NullArguments_Throw()
    {
        var logger = NullLogger<TransactionalOutboxPipelineBehavior<WithNotifications, string>>.Instance;
        var serializer = new JsonMessageSerializer();

        Should.Throw<ArgumentNullException>(() => new TransactionalOutboxPipelineBehavior<WithNotifications, string>(null!, _store, _factory, serializer, logger));
        Should.Throw<ArgumentNullException>(() => new TransactionalOutboxPipelineBehavior<WithNotifications, string>(_source, null!, _factory, serializer, logger));
        Should.Throw<ArgumentNullException>(() => new TransactionalOutboxPipelineBehavior<WithNotifications, string>(_source, _store, null!, serializer, logger));
        Should.Throw<ArgumentNullException>(() => new TransactionalOutboxPipelineBehavior<WithNotifications, string>(_source, _store, _factory, null!, logger));
        Should.Throw<ArgumentNullException>(() => new TransactionalOutboxPipelineBehavior<WithNotifications, string>(_source, _store, _factory, serializer, null!));
    }

    [Fact]
    public async Task Handle_NullArguments_Throw()
    {
        var behavior = Create<WithNotifications>();

        await Should.ThrowAsync<ArgumentNullException>(async () => await behavior.Handle(null!, _context, Ok(), CancellationToken.None));
        await Should.ThrowAsync<ArgumentNullException>(async () => await behavior.Handle(new WithNotifications(), null!, Ok(), CancellationToken.None));
        await Should.ThrowAsync<ArgumentNullException>(async () => await behavior.Handle(new WithNotifications(), _context, null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RequestWithoutNotifications_SkipsWithoutTouchingTheTransaction()
    {
        var result = await Create<Plain>().Handle(new Plain(), _context, Ok(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await _source.DidNotReceive().BeginOrJoinAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotificationsPresent_WritesThemAfterTheHandlerAndCommits()
    {
        var order = new List<string>();
        _store.AddAsync(Arg.Any<IOutboxMessage>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            order.Add("add");
            return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
        });
        _lease.CommitAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            order.Add("commit");
            return new ValueTask<Either<EncinaError, Unit>>(Unit.Default);
        });
        RequestHandlerCallback<string> next = () =>
        {
            order.Add("handler");
            return ValueTask.FromResult<Either<EncinaError, string>>("done");
        };

        var result = await Create<WithNotifications>().Handle(
            new WithNotifications(new Note("a"), new Note("b")), _context, next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        order.ShouldBe(["handler", "add", "add", "commit"]);
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _lease.Received(1).CommitAsync(CancellationToken.None);
        await _lease.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        await _lease.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Handle_NoNotificationsAfterTheHandler_CommitsWithoutWriting()
    {
        var result = await Create<WithNotifications>().Handle(new WithNotifications(), _context, Ok(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await _store.DidNotReceive().AddAsync(Arg.Any<IOutboxMessage>(), Arg.Any<CancellationToken>());
        await _lease.Received(1).CommitAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_TransactionEndedDuringTheHandler_FailsWithRollbackOnlyAndWritesNothing()
    {
        RequestHandlerCallback<string> next = () =>
        {
            _source.HasActiveTransaction.Returns(false);
            return ValueTask.FromResult<Either<EncinaError, string>>("done");
        };

        var result = await Create<WithNotifications>().Handle(
            new WithNotifications(new Note("a")), _context, next, CancellationToken.None);

        Code(result).ShouldBe(TransactionErrors.RollbackOnlyCode);
        await _store.DidNotReceive().AddAsync(Arg.Any<IOutboxMessage>(), Arg.Any<CancellationToken>());
        await _lease.Received(1).RollbackAsync(CancellationToken.None);
        await _lease.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HandlerLeft_RollsBackAndWritesNothing()
    {
        RequestHandlerCallback<string> next = () =>
            ValueTask.FromResult<Either<EncinaError, string>>(EncinaErrors.Create("test.fail", "x"));

        var result = await Create<WithNotifications>().Handle(
            new WithNotifications(new Note("a")), _context, next, CancellationToken.None);

        Code(result).ShouldBe("test.fail");
        await _store.DidNotReceive().AddAsync(Arg.Any<IOutboxMessage>(), Arg.Any<CancellationToken>());
        await _lease.Received(1).RollbackAsync(CancellationToken.None);
        await _lease.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AddLeft_RollsBackAndReturnsTheStoreError()
    {
        _store.AddAsync(Arg.Any<IOutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, Unit>>(EncinaErrors.Create("outbox.add_failed", "x")));

        var result = await Create<WithNotifications>().Handle(
            new WithNotifications(new Note("a")), _context, Ok(), CancellationToken.None);

        Code(result).ShouldBe("outbox.add_failed");
        await _lease.Received(1).RollbackAsync(CancellationToken.None);
        await _lease.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SaveChangesLeft_RollsBackAndReturnsTheStoreError()
    {
        _store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, Unit>>(EncinaErrors.Create("outbox.save_failed", "x")));

        var result = await Create<WithNotifications>().Handle(
            new WithNotifications(new Note("a")), _context, Ok(), CancellationToken.None);

        Code(result).ShouldBe("outbox.save_failed");
        await _lease.Received(1).RollbackAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_CommitLeft_ReplacesTheResponse()
    {
        _lease.CommitAsync(Arg.Any<CancellationToken>()).Returns(
            new ValueTask<Either<EncinaError, Unit>>(TransactionErrors.RollbackOnly()));

        var result = await Create<WithNotifications>().Handle(
            new WithNotifications(new Note("a")), _context, Ok(), CancellationToken.None);

        Code(result).ShouldBe(TransactionErrors.RollbackOnlyCode);
    }

    [Fact]
    public async Task Handle_HandlerThrows_RollsBackAndRethrowsUnchanged()
    {
        RequestHandlerCallback<string> next = () => throw new InvalidOperationException("boom");

        var act = async () => await Create<WithNotifications>().Handle(
            new WithNotifications(new Note("a")), _context, next, CancellationToken.None);

        (await act.ShouldThrowAsync<InvalidOperationException>()).Message.ShouldBe("boom");
        await _lease.Received(1).RollbackAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_Cancelled_RollsBackAndRethrows()
    {
        RequestHandlerCallback<string> next = () => throw new OperationCanceledException();

        var act = async () => await Create<WithNotifications>().Handle(
            new WithNotifications(new Note("a")), _context, next, CancellationToken.None);

        await act.ShouldThrowAsync<OperationCanceledException>();
        await _lease.Received(1).RollbackAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_BeginFails_ReturnsTheErrorWithoutRunningTheHandler()
    {
        _source.BeginOrJoinAsync(Arg.Any<CancellationToken>()).Returns(
            new ValueTask<Either<EncinaError, ITransactionLease>>(TransactionErrors.NotSupported(typeof(object))));
        var ran = false;
        RequestHandlerCallback<string> next = () =>
        {
            ran = true;
            return ValueTask.FromResult<Either<EncinaError, string>>("x");
        };

        var result = await Create<WithNotifications>().Handle(
            new WithNotifications(new Note("a")), _context, next, CancellationToken.None);

        Code(result).ShouldBe(TransactionErrors.NotSupportedCode);
        ran.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_JoinsAnExistingLease_AndStillWritesInsideIt()
    {
        _lease.IsOwner.Returns(false);

        var result = await Create<WithNotifications>().Handle(
            new WithNotifications(new Note("a")), _context, Ok(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await _store.Received(1).AddAsync(Arg.Any<IOutboxMessage>(), Arg.Any<CancellationToken>());
        await _lease.Received(1).CommitAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WithAnInterfaceRequestType_StillDetectsNotificationsAtRuntime()
    {
        var behavior = new TransactionalOutboxPipelineBehavior<IRequest<string>, string>(
            _source, _store, _factory, new JsonMessageSerializer(),
            NullLogger<TransactionalOutboxPipelineBehavior<IRequest<string>, string>>.Instance);

        await behavior.Handle(new WithNotifications(new Note("a")), _context, Ok(), CancellationToken.None);
        await behavior.Handle(new Plain(), _context, Ok(), CancellationToken.None);

        await _store.Received(1).AddAsync(Arg.Any<IOutboxMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmitsTheEnqueueActivityAndTheWrittenCounter()
    {
        var stopped = new ConcurrentQueue<Activity>();
        using var activities = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "Encina.Messaging.Outbox",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Enqueue
        };
        ActivitySource.AddActivityListener(activities);

        var counts = new ConcurrentQueue<(long Value, string? Outcome)>();
        using var meter = new MeterListener();
        meter.InstrumentPublished = (i, l) =>
        {
            if (i.Name == "encina.outbox.enqueued")
            {
                l.EnableMeasurementEvents(i);
            }
        };
        meter.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            string? outcome = null;
            string? db = null;
            foreach (var tag in tags)
            {
                if (tag.Key == "outcome") { outcome = tag.Value as string; }
                if (tag.Key == "db.system") { db = tag.Value as string; }
            }

            if (db == DbSystemName.Other)
            {
                counts.Enqueue((value, outcome));
            }
        });
        meter.Start();

        // A unique notification count identifies this test's activity among parallel tests.
        var items = Enumerable.Range(0, 7).Select(i => (INotification)new Note($"n{i}")).ToArray();
        await Create<WithNotifications>().Handle(new WithNotifications(items), _context, Ok(), CancellationToken.None);

        stopped.ToArray().ShouldContain(a => a.OperationName == "encina.outbox.enqueue"
            && (int)a.GetTagItem("outbox.notification_count")! == 7);
        counts.ToArray().ShouldContain((7L, OutboxProcessorMetrics.EnqueueWritten));
    }

    [Fact]
    public async Task Writer_ReturnsTheCountAndSerializesTheRuntimeType()
    {
        string? captured = null;
        _factory.Create(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Do<string>(c => captured = c), Arg.Any<DateTime>())
            .Returns(_ => Substitute.For<IOutboxMessage>());
        var writer = new OutboxWriter(_store, _factory, new JsonMessageSerializer(), TimeProvider.System);

        var written = await writer.WriteAsync([new Note("hello")], CancellationToken.None);

        written.Match(Right: c => c, Left: _ => -1).ShouldBe(1);
        captured.ShouldNotBeNull().ShouldContain("hello");
    }

    [Fact]
    public async Task Writer_StopsAtTheFirstAddLeft()
    {
        _store.AddAsync(Arg.Any<IOutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, Unit>>(EncinaErrors.Create("outbox.add_failed", "x")));
        var writer = new OutboxWriter(_store, _factory, new JsonMessageSerializer(), TimeProvider.System);

        var written = await writer.WriteAsync([new Note("a"), new Note("b")], CancellationToken.None);

        Code(written).ShouldBe("outbox.add_failed");
        await _store.Received(1).AddAsync(Arg.Any<IOutboxMessage>(), Arg.Any<CancellationToken>());
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Writer_UsesTheInjectedTimeProvider()
    {
        var now = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(now);
        DateTime? created = null;
        _factory.Create(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Do<DateTime>(d => created = d))
            .Returns(_ => Substitute.For<IOutboxMessage>());
        var writer = new OutboxWriter(_store, _factory, new JsonMessageSerializer(), clock);

        await writer.WriteAsync([new Note("a")], CancellationToken.None);

        created.ShouldBe(now.UtcDateTime);
    }
}
