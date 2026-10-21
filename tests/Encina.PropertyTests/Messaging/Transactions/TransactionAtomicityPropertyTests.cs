#pragma warning disable CA2012 // NSubstitute configures a ValueTask-returning member by calling it inside Returns(...).

using Encina.Messaging.Outbox;
using Encina.Messaging.Serialization;
using Encina.Messaging.Transactions;
using FsCheck.Xunit;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.PropertyTests.Messaging.Transactions;

/// <summary>
/// Property-based tests for the shared transaction and the transactional outbox (#718), run against the real
/// <see cref="DbTransactionAccessor"/> over a database-free connection:
/// (i) for any sequence of joins, commits and rollbacks the transaction commits if and only if the owner commits and
/// no participant rolled back; (ii) for any number of notifications a committed request leaves exactly that many
/// outbox messages and every other outcome leaves none.
/// </summary>
[Trait("Category", "Property")]
public sealed class TransactionAtomicityPropertyTests
{
    private enum Step
    {
        Join,
        CommitLastJoiner,
        RollbackLastJoiner,
        DisposeLastJoiner,
        CommitAllJoiners
    }

    [Property(MaxTest = 200)]
    public async Task<bool> Property_TransactionCommitsIffOwnerCommitsAndNoParticipantRolledBack(byte[] steps, bool ownerCommits)
    {
        var connection = new StubTransactionalConnection();
        var accessor = new DbTransactionAccessor(connection, NullLogger<DbTransactionAccessor>.Instance);
        await using var owner = (await accessor.BeginOrJoinAsync()).ShouldBeRight();
        var joiners = new List<ITransactionLease>();
        var anyRollback = false;

        foreach (var raw in steps ?? [])
        {
            switch ((Step)(raw % 5))
            {
                case Step.Join:
                    joiners.Add((await accessor.BeginOrJoinAsync()).ShouldBeRight());
                    break;
                case Step.CommitLastJoiner when joiners.Count > 0:
                    (await joiners[^1].CommitAsync()).ShouldBeRight();
                    break;
                case Step.RollbackLastJoiner when joiners.Count > 0:
                    await joiners[^1].RollbackAsync();
                    anyRollback = true;
                    break;
                case Step.DisposeLastJoiner when joiners.Count > 0:
                    await joiners[^1].DisposeAsync();
                    joiners.RemoveAt(joiners.Count - 1);
                    break;
                case Step.CommitAllJoiners:
                    foreach (var joiner in joiners)
                    {
                        (await joiner.CommitAsync()).ShouldBeRight();
                    }

                    break;
            }
        }

        foreach (var joiner in joiners)
        {
            await joiner.DisposeAsync();
        }

        var expectedCommit = ownerCommits && !anyRollback;
        if (ownerCommits)
        {
            var result = await owner.CommitAsync();
            if (result.IsRight != expectedCommit)
            {
                return false;
            }
        }
        else
        {
            await owner.RollbackAsync();
        }

        return connection.BeginCalls == 1
            && connection.CommitCalls == (expectedCommit ? 1 : 0)
            && connection.RollbackCalls == (expectedCommit ? 0 : 1)
            && !accessor.HasActiveTransaction;
    }

    private sealed record Order(int Notifications) : IRequest<string>, IHasNotifications
    {
        public IEnumerable<INotification> GetNotifications() =>
            Enumerable.Range(0, Notifications).Select(i => (INotification)new Placed(i));
    }

    private sealed record Placed(int Sequence) : INotification;

    private enum Outcome
    {
        Commit,
        HandlerLeft,
        HandlerThrows,
        WriteFails
    }

    [Property(MaxTest = 200)]
    public async Task<bool> Property_CommittedRequestLeavesNMessages_AnyOtherOutcomeLeavesNone(byte count, byte outcomeSeed)
    {
        var notifications = count % 9;
        var requested = (Outcome)(outcomeSeed % 4);

        // With no notification nothing is written, so a failing store is never reached and the request commits.
        var outcome = requested == Outcome.WriteFails && notifications == 0 ? Outcome.Commit : requested;
        var connection = new StubTransactionalConnection();
        var accessor = new DbTransactionAccessor(connection, NullLogger<DbTransactionAccessor>.Instance);
        var pending = new List<IOutboxMessage>();
        var committed = new List<IOutboxMessage>();
        connection.TransactionEnded += wasCommitted =>
        {
            if (wasCommitted)
            {
                committed.AddRange(pending);
            }

            pending.Clear();
        };

        var store = Substitute.For<IOutboxStore>();
        store.AddAsync(Arg.Any<IOutboxMessage>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            if (outcome == Outcome.WriteFails)
            {
                return Task.FromResult<Either<EncinaError, Unit>>(EncinaErrors.Create("store.failed", "write failed"));
            }

            pending.Add(call.Arg<IOutboxMessage>());
            return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
        });
        store.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<Either<EncinaError, Unit>>(Unit.Default));
        var factory = Substitute.For<IOutboxMessageFactory>();
        factory.Create(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(_ => Substitute.For<IOutboxMessage>());
        var behavior = new TransactionalOutboxPipelineBehavior<Order, string>(
            accessor, store, factory, new JsonMessageSerializer(), NullLogger<TransactionalOutboxPipelineBehavior<Order, string>>.Instance);

        RequestHandlerCallback<string> next = outcome switch
        {
            Outcome.HandlerLeft => () => ValueTask.FromResult<Either<EncinaError, string>>(EncinaErrors.Create("biz.rule", "no")),
            Outcome.HandlerThrows => () => throw new InvalidOperationException("boom"),
            _ => () => ValueTask.FromResult<Either<EncinaError, string>>("done")
        };

        var threw = false;
        var succeeded = false;
        try
        {
            succeeded = (await behavior.Handle(new Order(notifications), Substitute.For<IRequestContext>(), next, CancellationToken.None)).IsRight;
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        var expectedRows = outcome == Outcome.Commit ? notifications : 0;
        return committed.Count == expectedRows
            && pending.Count == 0
            && succeeded == (outcome == Outcome.Commit)
            && threw == (outcome == Outcome.HandlerThrows)
            && !accessor.HasActiveTransaction;
    }
}
