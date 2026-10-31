using Encina.Messaging.Transactions;
using Encina.MongoDB.Transactions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using NSubstitute;

namespace Encina.ContractTests.Messaging.Transactions;

/// <summary>
/// The <see cref="ITransactionLeaseSource"/> contract (#718) run against the real <see cref="MongoSessionAccessor"/>
/// over a mocked client: the first caller owns the transaction, later callers join, a joiner's rollback makes the
/// owner's commit fail, and the session's commit or abort follows the owner.
/// </summary>
[Trait("Category", "Contract")]
public sealed class MongoSessionLeaseSourceContractTests : IDisposable
{
    private readonly IClientSessionHandle _session = Substitute.For<IClientSessionHandle>();
    private readonly MongoSessionAccessor _source;

    public MongoSessionLeaseSourceContractTests()
    {
        var cluster = Substitute.For<ICluster>();
        cluster.Description.Returns(new ClusterDescription(new ClusterId(), false, null, ClusterType.ReplicaSet, []));
        var owner = Substitute.For<IMongoClient>();
        owner.Cluster.Returns(cluster);
        _session.Client.Returns(owner);
        _session.IsInTransaction.Returns(true);
        var client = Substitute.For<IMongoClient>();
        client.StartSessionAsync(Arg.Any<ClientSessionOptions>(), Arg.Any<CancellationToken>()).Returns(_session);
        _source = new MongoSessionAccessor(client, NullLogger<MongoSessionAccessor>.Instance);
    }

    public void Dispose() => _source.Dispose();

    private async Task<ITransactionLease> BeginAsync() => (await _source.BeginOrJoinAsync()).ShouldBeRight();

    [Fact]
    public async Task HasActiveTransaction_IsFalseUntilAnOwnerBegins_AndAfterItEnds()
    {
        _source.HasActiveTransaction.ShouldBeFalse();

        await using (var owner = await BeginAsync())
        {
            _source.HasActiveTransaction.ShouldBeTrue();
            (await owner.CommitAsync()).ShouldBeRight();
        }

        _source.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task BeginOrJoinAsync_FirstCallOwns_LaterCallsJoin()
    {
        await using var owner = await BeginAsync();
        await using var joiner = await BeginAsync();

        owner.IsOwner.ShouldBeTrue();
        joiner.IsOwner.ShouldBeFalse();
    }

    [Fact]
    public async Task JoinerCommit_IsANoOp_AndOwnerCommitCommitsTheSession()
    {
        await using var owner = await BeginAsync();
        await using var joiner = await BeginAsync();

        (await joiner.CommitAsync()).ShouldBeRight();
        _source.HasActiveTransaction.ShouldBeTrue();
        await _session.DidNotReceive().CommitTransactionAsync(Arg.Any<CancellationToken>());
        (await owner.CommitAsync()).ShouldBeRight();

        await _session.Received(1).CommitTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinerRollback_MakesTheOwnerCommitFailWithRollbackOnly_AndTheSessionAborts()
    {
        await using var owner = await BeginAsync();
        await using var joiner = await BeginAsync();

        await joiner.RollbackAsync();
        var committed = await owner.CommitAsync();

        committed.ShouldBeErrorWithCode(TransactionErrors.RollbackOnlyCode);
        await _session.DidNotReceive().CommitTransactionAsync(Arg.Any<CancellationToken>());
        await _session.Received(1).AbortTransactionAsync(Arg.Any<CancellationToken>());
        _source.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task OwnerRollback_Aborts_AndALaterCommitIsRefused()
    {
        var owner = await BeginAsync();

        await owner.RollbackAsync();
        var committed = await owner.CommitAsync();

        committed.ShouldBeErrorWithCode(TransactionErrors.LeaseDisposedCode);
        await _session.Received(1).AbortTransactionAsync(Arg.Any<CancellationToken>());
        await owner.DisposeAsync();
    }

    [Fact]
    public async Task DisposingAnUnfinishedOwner_Aborts()
    {
        var owner = await BeginAsync();

        await owner.DisposeAsync();

        await _session.Received(1).AbortTransactionAsync(Arg.Any<CancellationToken>());
        _source.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task AfterTheOwnerEnds_TheNextCallOwnsANewTransaction()
    {
        await using (var first = await BeginAsync())
        {
            (await first.CommitAsync()).ShouldBeRight();
        }

        await using var second = await BeginAsync();

        second.IsOwner.ShouldBeTrue();
    }
}
