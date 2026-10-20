using System.Data;
using Encina.Messaging.Transactions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Messaging.Transactions;

/// <summary>
/// Unit tests for <see cref="DbTransactionAccessor"/>: one owner per scope, joiners that never commit, rollback-only
/// propagation, isolation mismatch, asynchronous token-aware calls and fail-closed connection handling.
/// </summary>
[Trait("Category", "Unit")]
public sealed class DbTransactionAccessorTests : IAsyncDisposable
{
    private readonly RecordingTransactionalConnection _connection = new();
    private readonly DbTransactionAccessor _accessor;

    public DbTransactionAccessorTests()
    {
        _accessor = new DbTransactionAccessor(_connection, NullLogger<DbTransactionAccessor>.Instance);
    }

    public async ValueTask DisposeAsync()
    {
        await _accessor.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static IDbTransactionLease Lease(Either<EncinaError, IDbTransactionLease> begun)
    {
        begun.IsRight.ShouldBeTrue();
        return begun.IfLeft(static _ => throw new InvalidOperationException());
    }

    [Fact]
    public void Constructor_NullArguments_Throw()
    {
        Should.Throw<ArgumentNullException>(() => new DbTransactionAccessor(null!, NullLogger<DbTransactionAccessor>.Instance));
        Should.Throw<ArgumentNullException>(() => new DbTransactionAccessor(_connection, null!));
    }

    [Fact]
    public void Initially_HasNoTransaction()
    {
        _accessor.HasActiveTransaction.ShouldBeFalse();
        _accessor.Current.ShouldBeNull();
        _accessor.Connection.ShouldBeSameAs(_connection);
    }

    [Fact]
    public async Task BeginOrJoin_First_OpensConnectionAndOwnsTheTransactionWithTheToken()
    {
        using var cts = new CancellationTokenSource();

        await using var lease = Lease(await _accessor.BeginOrJoinAsync(IsolationLevel.ReadCommitted, cts.Token));

        lease.IsOwner.ShouldBeTrue();
        lease.Transaction.ShouldBeSameAs(_connection.Transaction);
        _accessor.Current.ShouldBeSameAs(_connection.Transaction);
        _accessor.HasActiveTransaction.ShouldBeTrue();
        _connection.OpenAsyncCalls.ShouldBe(1);
        _connection.OpenAsyncToken.ShouldBe(cts.Token);
        _connection.BeginAsyncToken.ShouldBe(cts.Token);
        _connection.BeginIsolationLevel.ShouldBe(IsolationLevel.ReadCommitted);
    }

    [Fact]
    public async Task BeginOrJoin_ConnectionAlreadyOpen_DoesNotOpenAgain()
    {
        _connection.SetState(ConnectionState.Open);

        await using var lease = Lease(await _accessor.BeginOrJoinAsync(null));

        _connection.OpenAsyncCalls.ShouldBe(0);
        _connection.BeginIsolationLevel.ShouldBe(IsolationLevel.Unspecified);
    }

    [Fact]
    public async Task BeginOrJoin_NeutralOverload_ReturnsTheSameKindOfLease()
    {
        var begun = await ((ITransactionLeaseSource)_accessor).BeginOrJoinAsync();

        begun.IsRight.ShouldBeTrue();
        var lease = begun.IfLeft(static _ => throw new InvalidOperationException());
        lease.IsOwner.ShouldBeTrue();
        await lease.DisposeAsync();
    }

    [Fact]
    public async Task BeginOrJoin_Second_JoinsWithoutBeginningAgain()
    {
        await using var owner = Lease(await _accessor.BeginOrJoinAsync(null));

        await using var joiner = Lease(await _accessor.BeginOrJoinAsync(null));

        joiner.IsOwner.ShouldBeFalse();
        joiner.Transaction.ShouldBeSameAs(owner.Transaction);
        _connection.BeginAsyncCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Joiner_Commit_IsANoOpThatSucceeds()
    {
        await using var owner = Lease(await _accessor.BeginOrJoinAsync(null));
        await using var joiner = Lease(await _accessor.BeginOrJoinAsync(null));

        var committed = await joiner.CommitAsync();

        committed.IsRight.ShouldBeTrue();
        _connection.Transaction.CommitAsyncCalls.ShouldBe(0);
        _accessor.HasActiveTransaction.ShouldBeTrue();
    }

    [Fact]
    public async Task Owner_Commit_CommitsAndClearsTheTransaction()
    {
        await using var owner = Lease(await _accessor.BeginOrJoinAsync(null));

        var committed = await owner.CommitAsync(CancellationToken.None);

        committed.IsRight.ShouldBeTrue();
        _connection.Transaction.CommitAsyncCalls.ShouldBe(1);
        _connection.Transaction.CommitAsyncToken.ShouldBe(CancellationToken.None);
        _accessor.Current.ShouldBeNull();
        _accessor.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task Owner_CommitTwice_SecondFailsAsLeaseDisposed()
    {
        await using var owner = Lease(await _accessor.BeginOrJoinAsync(null));
        await owner.CommitAsync();

        var second = await owner.CommitAsync();

        ((EncinaError)second).GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.LeaseDisposedCode);
    }

    [Fact]
    public async Task Joiner_Rollback_MakesTheOwnersCommitRollBackAndFail()
    {
        await using var owner = Lease(await _accessor.BeginOrJoinAsync(null));
        await using var joiner = Lease(await _accessor.BeginOrJoinAsync(null));
        await joiner.RollbackAsync();

        var committed = await owner.CommitAsync();

        ((EncinaError)committed).GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.RollbackOnlyCode);
        _connection.Transaction.CommitAsyncCalls.ShouldBe(0);
        _connection.Transaction.RollbackAsyncCalls.ShouldBe(1);
        _accessor.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task Owner_CommitThrows_RollsBackAndReturnsCommitFailed()
    {
        await using var owner = Lease(await _accessor.BeginOrJoinAsync(null));
        _connection.Transaction.ThrowOnCommit = true;

        var committed = await owner.CommitAsync();

        ((EncinaError)committed).GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.CommitFailedCode);
        _connection.Transaction.RollbackAsyncCalls.ShouldBe(1);
        _accessor.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task Owner_Rollback_RollsBackWithTokenNoneAndIgnoresCancellation()
    {
        await using var owner = Lease(await _accessor.BeginOrJoinAsync(null));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await owner.RollbackAsync(cts.Token);

        _connection.Transaction.RollbackAsyncCalls.ShouldBe(1);
        _connection.Transaction.RollbackAsyncToken.ShouldBe(CancellationToken.None);
        _accessor.Current.ShouldBeNull();
    }

    [Fact]
    public async Task Owner_RollbackThrows_IsSwallowedAndTheTransactionEnds()
    {
        await using var owner = Lease(await _accessor.BeginOrJoinAsync(null));
        _connection.Transaction.ThrowOnRollback = true;

        await owner.RollbackAsync();

        _accessor.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task Owner_RollbackAfterEnd_DoesNothing()
    {
        await using var owner = Lease(await _accessor.BeginOrJoinAsync(null));
        await owner.RollbackAsync();

        await owner.RollbackAsync();

        _connection.Transaction.RollbackAsyncCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Owner_DisposedWithoutCommit_RollsBackAndDisposesTheTransaction()
    {
        var owner = Lease(await _accessor.BeginOrJoinAsync(null));

        await owner.DisposeAsync();
        await owner.DisposeAsync();

        _connection.Transaction.RollbackAsyncCalls.ShouldBe(1);
        _connection.Transaction.DisposeAsyncCalls.ShouldBe(1);
        _accessor.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task Owner_DisposedAfterCommit_DoesNotRollBack()
    {
        var owner = Lease(await _accessor.BeginOrJoinAsync(null));
        await owner.CommitAsync();

        await owner.DisposeAsync();

        _connection.Transaction.RollbackAsyncCalls.ShouldBe(0);
        _connection.Transaction.DisposeAsyncCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Accessor_Disposed_RollsBackAnOpenTransaction()
    {
        Lease(await _accessor.BeginOrJoinAsync(null));

        await _accessor.DisposeAsync();

        _connection.Transaction.RollbackAsyncCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Accessor_DisposedWithoutTransaction_DoesNothing()
    {
        await _accessor.DisposeAsync();

        _connection.Transaction.RollbackAsyncCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Joiner_AfterOwnerEnded_CommitFailsAsLeaseDisposed()
    {
        var owner = Lease(await _accessor.BeginOrJoinAsync(null));
        var joiner = Lease(await _accessor.BeginOrJoinAsync(null));
        await owner.CommitAsync();

        var committed = await joiner.CommitAsync();

        ((EncinaError)committed).GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.LeaseDisposedCode);
        await joiner.RollbackAsync();
        await joiner.DisposeAsync();
        await owner.DisposeAsync();
    }

    [Fact]
    public async Task Joiner_DisposedThenUsed_FailsAsLeaseDisposedAndDoesNotMarkRollbackOnly()
    {
        await using var owner = Lease(await _accessor.BeginOrJoinAsync(null));
        var joiner = Lease(await _accessor.BeginOrJoinAsync(null));
        await joiner.DisposeAsync();
        await joiner.DisposeAsync();

        var committed = await joiner.CommitAsync();
        await joiner.RollbackAsync();
        var ownerCommit = await owner.CommitAsync();

        ((EncinaError)committed).GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.LeaseDisposedCode);
        ownerCommit.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Joiner_AskingForAnotherIsolationLevel_IsRefused()
    {
        await using var owner = Lease(await _accessor.BeginOrJoinAsync(IsolationLevel.ReadCommitted));

        var joined = await _accessor.BeginOrJoinAsync(IsolationLevel.Serializable);

        ((EncinaError)joined).GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.IsolationLevelMismatchCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(IsolationLevel.Unspecified)]
    [InlineData(IsolationLevel.ReadCommitted)]
    public async Task Joiner_AskingForCompatibleIsolationLevel_Joins(IsolationLevel? level)
    {
        await using var owner = Lease(await _accessor.BeginOrJoinAsync(IsolationLevel.ReadCommitted));

        await using var joiner = Lease(await _accessor.BeginOrJoinAsync(level));

        joiner.IsOwner.ShouldBeFalse();
    }

    [Fact]
    public async Task BeginOrJoin_ConnectionIsNotADbConnection_FailsClosedWithoutSyncFallback()
    {
        var plain = Substitute.For<IDbConnection>();
        var accessor = new DbTransactionAccessor(plain, NullLogger<DbTransactionAccessor>.Instance);

        var begun = await accessor.BeginOrJoinAsync(null);

        ((EncinaError)begun).GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.NotSupportedCode);
        plain.DidNotReceive().Open();
        plain.DidNotReceive().BeginTransaction();
        plain.DidNotReceive().BeginTransaction(Arg.Any<IsolationLevel>());
    }

    [Fact]
    public async Task BeginOrJoin_OpenFails_ReturnsBeginFailed()
    {
        _connection.ThrowOnOpen = true;

        var begun = await _accessor.BeginOrJoinAsync(null);

        ((EncinaError)begun).GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.BeginFailedCode);
        _accessor.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task BeginOrJoin_BeginFails_ReturnsBeginFailed()
    {
        _connection.ThrowOnBegin = true;

        var begun = await _accessor.BeginOrJoinAsync(null);

        ((EncinaError)begun).GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.BeginFailedCode);
        _accessor.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task BeginOrJoin_Cancelled_Propagates()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await _accessor.BeginOrJoinAsync(null, cts.Token);

        await act.ShouldThrowAsync<OperationCanceledException>();
        _accessor.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task BeginOrJoin_AfterTheFirstTransactionEnded_BeginsANewOne()
    {
        var first = Lease(await _accessor.BeginOrJoinAsync(null));
        await first.CommitAsync();
        await first.DisposeAsync();

        await using var second = Lease(await _accessor.BeginOrJoinAsync(null));

        second.IsOwner.ShouldBeTrue();
        _connection.BeginAsyncCalls.ShouldBe(2);
    }
}
