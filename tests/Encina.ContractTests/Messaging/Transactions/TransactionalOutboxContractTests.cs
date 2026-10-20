#pragma warning disable CA2012 // NSubstitute configures a ValueTask-returning member by calling it inside Returns(...).

using Encina.Messaging.Outbox;
using Encina.Messaging.Serialization;
using Encina.Messaging.Transactions;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.ContractTests.Messaging.Transactions;

/// <summary>
/// Behavioral contract of <see cref="TransactionalOutboxPipelineBehavior{TRequest, TResponse}"/> (#718) over
/// the real <see cref="DbTransactionAccessor"/>: the outbox messages become visible only when the request's
/// transaction commits, and a failed handler, a failed write or a rollback-only participant leaves none.
/// </summary>
[Trait("Category", "Contract")]
public sealed class TransactionalOutboxContractTests
{
    private sealed record Order(int Notifications) : IRequest<string>, IHasNotifications
    {
        public IEnumerable<INotification> GetNotifications() =>
            Enumerable.Range(0, Notifications).Select(i => (INotification)new Placed(i));
    }

    private sealed record Plain : IRequest<string>;

    private sealed record Placed(int Sequence) : INotification;

    private sealed class Fixture
    {
        public Fixture(bool failWrites = false)
        {
            Connection = new StubTransactionalConnection();
            Accessor = new DbTransactionAccessor(Connection, NullLogger<DbTransactionAccessor>.Instance);
            Connection.TransactionEnded += committed =>
            {
                if (committed)
                {
                    Committed.AddRange(Pending);
                }

                Pending.Clear();
            };

            var store = Substitute.For<IOutboxStore>();
            store.AddAsync(Arg.Any<IOutboxMessage>(), Arg.Any<CancellationToken>()).Returns(call =>
            {
                if (failWrites)
                {
                    return Task.FromResult<Either<EncinaError, Unit>>(EncinaErrors.Create("store.failed", "write failed"));
                }

                Pending.Add(call.Arg<IOutboxMessage>());
                return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
            });
            store.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<Either<EncinaError, Unit>>(Unit.Default));

            var factory = Substitute.For<IOutboxMessageFactory>();
            factory.Create(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
                .Returns(_ => Substitute.For<IOutboxMessage>());

            Store = store;
            Factory = factory;
        }

        public StubTransactionalConnection Connection { get; }

        public DbTransactionAccessor Accessor { get; }

        public IOutboxStore Store { get; }

        public IOutboxMessageFactory Factory { get; }

        public List<IOutboxMessage> Pending { get; } = [];

        public List<IOutboxMessage> Committed { get; } = [];

        public TransactionalOutboxPipelineBehavior<TRequest, string> Behavior<TRequest>()
            where TRequest : IRequest<string> =>
            new(Accessor, Store, Factory, new JsonMessageSerializer(),
                NullLogger<TransactionalOutboxPipelineBehavior<TRequest, string>>.Instance);
    }

    private static RequestHandlerCallback<string> Ok() =>
        () => ValueTask.FromResult<Either<EncinaError, string>>("done");

    private static readonly IRequestContext Context = Substitute.For<IRequestContext>();

    [Fact]
    public void ImplementsIPipelineBehavior_AndIsSealed()
    {
        typeof(IPipelineBehavior<Order, string>).IsAssignableFrom(typeof(TransactionalOutboxPipelineBehavior<Order, string>)).ShouldBeTrue();
        typeof(TransactionalOutboxPipelineBehavior<Order, string>).IsSealed.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    public async Task Commit_MakesEveryNotificationVisible(int notifications)
    {
        var fixture = new Fixture();

        var result = await fixture.Behavior<Order>().Handle(new Order(notifications), Context, Ok(), CancellationToken.None);

        result.ShouldBeRight();
        fixture.Committed.Count.ShouldBe(notifications);
        fixture.Connection.CommitCalls.ShouldBe(1);
    }

    [Fact]
    public async Task HandlerLeft_LeavesNoMessage()
    {
        var fixture = new Fixture();
        RequestHandlerCallback<string> failing = () => ValueTask.FromResult<Either<EncinaError, string>>(EncinaErrors.Create("biz.rule", "no"));

        var result = await fixture.Behavior<Order>().Handle(new Order(3), Context, failing, CancellationToken.None);

        result.ShouldBeErrorWithCode("biz.rule");
        fixture.Committed.ShouldBeEmpty();
        fixture.Pending.ShouldBeEmpty();
        fixture.Connection.RollbackCalls.ShouldBe(1);
    }

    [Fact]
    public async Task FailedWrite_FailsTheRequestAndLeavesNoMessage()
    {
        var fixture = new Fixture(failWrites: true);

        var result = await fixture.Behavior<Order>().Handle(new Order(2), Context, Ok(), CancellationToken.None);

        result.ShouldBeErrorWithCode("store.failed");
        fixture.Committed.ShouldBeEmpty();
        fixture.Connection.RollbackCalls.ShouldBe(1);
        fixture.Connection.CommitCalls.ShouldBe(0);
    }

    [Fact]
    public async Task ParticipantRollback_FailsTheRequestWithRollbackOnly_AndLeavesNoMessage()
    {
        var fixture = new Fixture();
        RequestHandlerCallback<string> rollingBack = async () =>
        {
            await using var joined = (await fixture.Accessor.BeginOrJoinAsync()).ShouldBeRight();
            await joined.RollbackAsync();
            return "done";
        };

        var result = await fixture.Behavior<Order>().Handle(new Order(2), Context, rollingBack, CancellationToken.None);

        result.ShouldBeErrorWithCode(TransactionErrors.RollbackOnlyCode);
        fixture.Committed.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandlerException_RollsBack_AndReachesTheCaller()
    {
        var fixture = new Fixture();
        RequestHandlerCallback<string> throwing = () => throw new InvalidOperationException("boom");

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await fixture.Behavior<Order>().Handle(new Order(2), Context, throwing, CancellationToken.None));

        fixture.Committed.ShouldBeEmpty();
        fixture.Connection.RollbackCalls.ShouldBe(1);
    }

    [Fact]
    public async Task RequestWithoutNotifications_NeverBeginsATransaction()
    {
        var fixture = new Fixture();

        var result = await fixture.Behavior<Plain>().Handle(new Plain(), Context, Ok(), CancellationToken.None);

        result.ShouldBeRight();
        fixture.Connection.BeginCalls.ShouldBe(0);
    }

    [Fact]
    public async Task InsideAnOpenTransaction_JoinsInsteadOfBeginningAnother()
    {
        var fixture = new Fixture();
        await using var owner = (await fixture.Accessor.BeginOrJoinAsync()).ShouldBeRight();

        var result = await fixture.Behavior<Order>().Handle(new Order(2), Context, Ok(), CancellationToken.None);

        result.ShouldBeRight();
        fixture.Connection.BeginCalls.ShouldBe(1);
        fixture.Committed.ShouldBeEmpty();
        (await owner.CommitAsync()).ShouldBeRight();
        fixture.Committed.Count.ShouldBe(2);
    }
}
