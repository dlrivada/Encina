using Encina.Messaging.Transactions;
using Encina.MongoDB.Transactions;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;

namespace Encina.PropertyTests.Messaging.Transactions;

/// <summary>
/// Property (i) of #718 against the real <see cref="MongoSessionAccessor"/> over a mocked client: for any sequence
/// of joins, commits and rollbacks the session transaction commits if and only if the owner commits and no
/// participant rolled back, observed through the session's commit and abort calls.
/// </summary>
[Trait("Category", "Property")]
public sealed class MongoSessionAtomicityPropertyTests
{
    private enum Step
    {
        Join,
        CommitLastJoiner,
        RollbackLastJoiner,
        DisposeLastJoiner,
        CommitAllJoiners
    }

    [Property(MaxTest = 100)]
    public async Task<bool> Property_TransactionCommitsIffOwnerCommitsAndNoParticipantRolledBack(byte[] steps, bool ownerCommits)
    {
        var session = Substitute.For<IClientSessionHandle>();
        var cluster = Substitute.For<ICluster>();
        cluster.Description.Returns(new ClusterDescription(new ClusterId(), false, null, ClusterType.ReplicaSet, []));
        var sessionClient = Substitute.For<IMongoClient>();
        sessionClient.Cluster.Returns(cluster);
        session.Client.Returns(sessionClient);
        session.IsInTransaction.Returns(true);
        var client = Substitute.For<IMongoClient>();
        client.StartSessionAsync(Arg.Any<ClientSessionOptions>(), Arg.Any<CancellationToken>()).Returns(session);
        using var accessor = new MongoSessionAccessor(client, NullLogger<MongoSessionAccessor>.Instance);

        var owner = (await accessor.BeginOrJoinAsync()).ShouldBeRight();
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
            if ((await owner.CommitAsync()).IsRight != expectedCommit)
            {
                return false;
            }
        }
        else
        {
            await owner.RollbackAsync();
        }

        await owner.DisposeAsync();
        var committedCalls = session.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IClientSession.CommitTransactionAsync));
        return committedCalls == (expectedCommit ? 1 : 0) && !accessor.HasActiveTransaction;
    }
}
