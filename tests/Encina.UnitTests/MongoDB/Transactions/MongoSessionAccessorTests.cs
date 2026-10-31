using Encina.Messaging.Transactions;
using Encina.MongoDB.Transactions;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.MongoDB.Transactions;

[Trait("Category", "Unit")]
public sealed class MongoSessionAccessorTests : IDisposable
{
    private readonly IMongoClient _client = Substitute.For<IMongoClient>();
    private readonly MongoSessionAccessor _accessor;

    public MongoSessionAccessorTests()
    {
        _accessor = new MongoSessionAccessor(_client, NullLogger<MongoSessionAccessor>.Instance);
    }

    public void Dispose() => _accessor.Dispose();

    private IClientSessionHandle Session(ClusterType type = ClusterType.ReplicaSet)
    {
        var session = Substitute.For<IClientSessionHandle>();
        var cluster = Substitute.For<ICluster>();
        cluster.Description.Returns(new ClusterDescription(new ClusterId(), false, null, type, []));
        var owner = Substitute.For<IMongoClient>();
        owner.Cluster.Returns(cluster);
        session.Client.Returns(owner);
        session.IsInTransaction.Returns(true);
        _client.StartSessionAsync(Arg.Any<ClientSessionOptions>(), Arg.Any<CancellationToken>()).Returns(session);
        return session;
    }

    private static ITransactionLease Lease(Either<EncinaError, ITransactionLease> result)
        => result.Match(l => l, e => throw new InvalidOperationException(e.GetCode().IfNone("?")));

    private static string Code(Either<EncinaError, ITransactionLease> result)
        => result.Match(_ => "", e => e.GetCode().IfNone(""));

    [Fact]
    public void DbSystem_IsMongoDb() => _accessor.DbSystem.ShouldBe("mongodb");

    [Fact]
    public async Task BeginOrJoin_FirstCall_OwnsAndStartsPrimaryOnlyTransaction()
    {
        var session = Session();

        await using var lease = Lease(await _accessor.BeginOrJoinAsync());

        lease.IsOwner.ShouldBeTrue();
        _accessor.HasActiveTransaction.ShouldBeTrue();
        _accessor.CurrentSession.ShouldBeSameAs(session);
        session.Received(1).StartTransaction(Arg.Is<TransactionOptions>(o =>
            o.ReadPreference == ReadPreference.Primary && o.ReadConcern == null && o.WriteConcern == null));
    }

    [Fact]
    public async Task BeginOrJoin_Standalone_FailsClosedBeforeStartTransaction()
    {
        var session = Session(ClusterType.Standalone);

        var result = await _accessor.BeginOrJoinAsync();

        Code(result).ShouldBe(TransactionErrors.NotSupportedCode);
        session.DidNotReceive().StartTransaction(Arg.Any<TransactionOptions>());
        session.Received(1).Dispose();
        _accessor.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task BeginOrJoin_ServerRejectsTransactionNumbers_MapsToNotSupported()
    {
        var session = Session();
        session.When(s => s.StartTransaction(Arg.Any<TransactionOptions>()))
            .Do(_ => throw new MongoException("Transaction numbers are only allowed on a replica set member or mongos"));

        var result = await _accessor.BeginOrJoinAsync();

        Code(result).ShouldBe(TransactionErrors.NotSupportedCode);
    }

    [Fact]
    public async Task BeginOrJoin_OtherFailure_ReturnsBeginFailed()
    {
        _client.StartSessionAsync(Arg.Any<ClientSessionOptions>(), Arg.Any<CancellationToken>())
            .Returns<IClientSessionHandle>(_ => throw new MongoException("boom"));

        Code(await _accessor.BeginOrJoinAsync()).ShouldBe(TransactionErrors.BeginFailedCode);
    }

    [Fact]
    public async Task BeginOrJoin_Canceled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _client.StartSessionAsync(Arg.Any<ClientSessionOptions>(), Arg.Any<CancellationToken>())
            .Returns<IClientSessionHandle>(_ => throw new OperationCanceledException(cts.Token));

        await Should.ThrowAsync<OperationCanceledException>(async () => await _accessor.BeginOrJoinAsync(cts.Token));
    }

    [Fact]
    public async Task Joiner_CommitIsNoOp_AndOwnerCommits()
    {
        var session = Session();
        await using var owner = Lease(await _accessor.BeginOrJoinAsync());
        await using var joiner = Lease(await _accessor.BeginOrJoinAsync());

        joiner.IsOwner.ShouldBeFalse();
        (await joiner.CommitAsync()).IsRight.ShouldBeTrue();
        await session.DidNotReceive().CommitTransactionAsync(Arg.Any<CancellationToken>());

        (await owner.CommitAsync()).IsRight.ShouldBeTrue();
        await session.Received(1).CommitTransactionAsync(Arg.Any<CancellationToken>());
        _accessor.HasActiveTransaction.ShouldBeFalse();
        _accessor.CurrentSession.ShouldBeNull();
    }

    [Fact]
    public async Task Joiner_Rollback_MakesOwnerCommitAbortAndReturnRollbackOnly()
    {
        var session = Session();
        await using var owner = Lease(await _accessor.BeginOrJoinAsync());
        await using var joiner = Lease(await _accessor.BeginOrJoinAsync());

        await joiner.RollbackAsync();
        var committed = await owner.CommitAsync();

        committed.Match(_ => "", e => e.GetCode().IfNone("")).ShouldBe(TransactionErrors.RollbackOnlyCode);
        await session.Received(1).AbortTransactionAsync(Arg.Any<CancellationToken>());
        await session.DidNotReceive().CommitTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OwnerCommit_FailsWithLabel_AbortsAndCarriesLabelNotMessage()
    {
        var session = Session();
        var failure = new MongoException("secret server text");
        failure.AddErrorLabel("TransientTransactionError");
        session.CommitTransactionAsync(Arg.Any<CancellationToken>()).Returns<Task>(_ => throw failure);
        await using var owner = Lease(await _accessor.BeginOrJoinAsync());

        var committed = await owner.CommitAsync();

        var error = committed.Match(_ => throw new InvalidOperationException(), e => e);
        error.GetCode().IfNone("").ShouldBe(TransactionErrors.CommitFailedCode);
        error.GetDetails()[TransactionErrors.ErrorLabelDetailKey].ShouldBe("TransientTransactionError");
        await session.Received(1).AbortTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OwnerCommit_UnknownResultLabel_IsReported()
    {
        var session = Session();
        var failure = new MongoException("x");
        failure.AddErrorLabel("UnknownTransactionCommitResult");
        session.CommitTransactionAsync(Arg.Any<CancellationToken>()).Returns<Task>(_ => throw failure);
        await using var owner = Lease(await _accessor.BeginOrJoinAsync());

        var error = (await owner.CommitAsync()).Match(_ => throw new InvalidOperationException(), e => e);

        error.GetDetails()[TransactionErrors.ErrorLabelDetailKey].ShouldBe("UnknownTransactionCommitResult");
    }

    [Fact]
    public async Task OwnerCommit_NonMongoFailure_HasNoLabel()
    {
        var session = Session();
        session.CommitTransactionAsync(Arg.Any<CancellationToken>()).Returns<Task>(_ => throw new InvalidOperationException("x"));
        await using var owner = Lease(await _accessor.BeginOrJoinAsync());

        var error = (await owner.CommitAsync()).Match(_ => throw new InvalidOperationException(), e => e);

        error.GetCode().IfNone("").ShouldBe(TransactionErrors.CommitFailedCode);
        error.GetDetails().ContainsKey(TransactionErrors.ErrorLabelDetailKey).ShouldBeFalse();
    }

    [Fact]
    public async Task OwnerCommit_ServerRejectsStandalone_MapsToNotSupported()
    {
        var session = Session();
        session.CommitTransactionAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new MongoException("Transaction numbers are only allowed on a replica set member"));
        await using var owner = Lease(await _accessor.BeginOrJoinAsync());

        var error = (await owner.CommitAsync()).Match(_ => throw new InvalidOperationException(), e => e);

        error.GetCode().IfNone("").ShouldBe(TransactionErrors.NotSupportedCode);
    }

    [Fact]
    public async Task OwnerCommit_AbortAlsoFails_StillReturnsCommitFailed()
    {
        var session = Session();
        session.CommitTransactionAsync(Arg.Any<CancellationToken>()).Returns<Task>(_ => throw new MongoException("x"));
        session.AbortTransactionAsync(Arg.Any<CancellationToken>()).Returns<Task>(_ => throw new MongoException("y"));
        await using var owner = Lease(await _accessor.BeginOrJoinAsync());

        var error = (await owner.CommitAsync()).Match(_ => throw new InvalidOperationException(), e => e);

        error.GetCode().IfNone("").ShouldBe(TransactionErrors.CommitFailedCode);
    }

    [Fact]
    public async Task OwnerRollback_AbortsWithNoneTokenAndReleases()
    {
        var session = Session();
        await using var owner = Lease(await _accessor.BeginOrJoinAsync());

        await owner.RollbackAsync(new CancellationToken(true));

        await session.Received(1).AbortTransactionAsync(CancellationToken.None);
        _accessor.HasActiveTransaction.ShouldBeFalse();
        (await owner.CommitAsync()).IsLeft.ShouldBeTrue();
        await owner.RollbackAsync();
    }

    [Fact]
    public async Task DisposingOwnerWithoutCommit_Aborts()
    {
        var session = Session();
        var owner = Lease(await _accessor.BeginOrJoinAsync());

        await owner.DisposeAsync();
        await owner.DisposeAsync();

        await session.Received(1).AbortTransactionAsync(Arg.Any<CancellationToken>());
        session.Received().Dispose();
    }

    [Fact]
    public async Task DisposeAsync_Accessor_AbortsOpenTransactionAndDisposesSession()
    {
        var session = Session();
        _ = Lease(await _accessor.BeginOrJoinAsync());

        await _accessor.DisposeAsync();

        await session.Received(1).AbortTransactionAsync(Arg.Any<CancellationToken>());
        session.Received().Dispose();
    }

    [Fact]
    public async Task DisposeAsync_NoTransaction_Completes()
    {
        await _accessor.DisposeAsync();
        _accessor.Dispose();
    }

    [Fact]
    public async Task Dispose_Accessor_DisposesSession()
    {
        var session = Session();
        _ = Lease(await _accessor.BeginOrJoinAsync());

        _accessor.Dispose();
        _accessor.Dispose();

        session.Received().Dispose();
        _accessor.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task JoinerAfterOwnerCompleted_CommitIsLeaseDisposed()
    {
        Session();
        await using var owner = Lease(await _accessor.BeginOrJoinAsync());
        var joiner = Lease(await _accessor.BeginOrJoinAsync());
        await owner.CommitAsync();

        (await joiner.CommitAsync()).IsLeft.ShouldBeTrue();
        await joiner.DisposeAsync();
        await joiner.RollbackAsync();
        (await joiner.CommitAsync()).IsLeft.ShouldBeTrue();
    }
}
