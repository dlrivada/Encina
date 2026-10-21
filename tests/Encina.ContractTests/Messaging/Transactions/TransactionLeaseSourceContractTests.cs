using System.Data;
using Encina.Messaging.Transactions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Encina.ContractTests.Messaging.Transactions;

/// <summary>
/// Behavioral contract of <see cref="ITransactionLeaseSource"/> and <see cref="ITransactionLease"/> (#718), run
/// against the real <see cref="DbTransactionAccessor"/> over a database-free connection: the first caller owns
/// the transaction, later callers join, a joiner's rollback makes the owner's commit fail, and an owner that
/// is disposed unfinished rolls back.
/// </summary>
[Trait("Category", "Contract")]
public sealed class TransactionLeaseSourceContractTests
{
    private static (ITransactionLeaseSource Source, StubTransactionalConnection Connection) Create()
    {
        var connection = new StubTransactionalConnection();
        return (new DbTransactionAccessor(connection, NullLogger<DbTransactionAccessor>.Instance), connection);
    }

    private static async Task<ITransactionLease> BeginAsync(ITransactionLeaseSource source) =>
        (await source.BeginOrJoinAsync()).ShouldBeRight();

    [Fact]
    public async Task HasActiveTransaction_IsFalseUntilAnOwnerBegins_AndAfterItEnds()
    {
        var (source, _) = Create();
        source.HasActiveTransaction.ShouldBeFalse();

        await using (var owner = await BeginAsync(source))
        {
            source.HasActiveTransaction.ShouldBeTrue();
            (await owner.CommitAsync()).ShouldBeRight();
        }

        source.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task BeginOrJoinAsync_FirstCallOwns_LaterCallsJoin()
    {
        var (source, connection) = Create();

        await using var owner = await BeginAsync(source);
        await using var joiner = await BeginAsync(source);

        owner.IsOwner.ShouldBeTrue();
        joiner.IsOwner.ShouldBeFalse();
        connection.BeginCalls.ShouldBe(1);
    }

    [Fact]
    public async Task JoinerCommit_IsANoOpThatSucceeds()
    {
        var (source, connection) = Create();
        await using var owner = await BeginAsync(source);
        await using var joiner = await BeginAsync(source);

        (await joiner.CommitAsync()).ShouldBeRight();

        connection.CommitCalls.ShouldBe(0);
        source.HasActiveTransaction.ShouldBeTrue();
    }

    [Fact]
    public async Task OwnerCommit_CommitsOnce()
    {
        var (source, connection) = Create();
        await using var owner = await BeginAsync(source);
        await using var joiner = await BeginAsync(source);
        (await joiner.CommitAsync()).ShouldBeRight();

        (await owner.CommitAsync()).ShouldBeRight();

        connection.CommitCalls.ShouldBe(1);
        connection.RollbackCalls.ShouldBe(0);
    }

    [Fact]
    public async Task JoinerRollback_MakesTheOwnerCommitFailWithRollbackOnly_AndRollsBackOnce()
    {
        var (source, connection) = Create();
        await using var owner = await BeginAsync(source);
        await using var joiner = await BeginAsync(source);

        await joiner.RollbackAsync();
        var committed = await owner.CommitAsync();

        committed.ShouldBeErrorWithCode(TransactionErrors.RollbackOnlyCode);
        connection.CommitCalls.ShouldBe(0);
        connection.RollbackCalls.ShouldBe(1);
        source.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task OwnerRollback_RollsBack_AndALaterCommitIsRefused()
    {
        var (source, connection) = Create();
        var owner = await BeginAsync(source);

        await owner.RollbackAsync();
        var committed = await owner.CommitAsync();

        connection.RollbackCalls.ShouldBe(1);
        committed.ShouldBeErrorWithCode(TransactionErrors.LeaseDisposedCode);
        await owner.DisposeAsync();
        connection.RollbackCalls.ShouldBe(1);
    }

    [Fact]
    public async Task DisposingAnUnfinishedOwner_RollsBack()
    {
        var (source, connection) = Create();
        var owner = await BeginAsync(source);

        await owner.DisposeAsync();

        connection.RollbackCalls.ShouldBe(1);
        connection.CommitCalls.ShouldBe(0);
        source.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task CommitAfterDispose_ReturnsLeaseDisposed()
    {
        var (source, _) = Create();
        var owner = await BeginAsync(source);
        await owner.DisposeAsync();

        (await owner.CommitAsync()).ShouldBeErrorWithCode(TransactionErrors.LeaseDisposedCode);
    }

    [Fact]
    public async Task OwnerCommitThatFails_ReturnsCommitFailed_AndRollsBack()
    {
        var (source, connection) = Create();
        connection.ThrowOnCommit = true;
        await using var owner = await BeginAsync(source);

        (await owner.CommitAsync()).ShouldBeErrorWithCode(TransactionErrors.CommitFailedCode);

        connection.RollbackCalls.ShouldBe(1);
        source.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task AfterTheOwnerEnds_TheNextCallOwnsANewTransaction()
    {
        var (source, connection) = Create();
        await using (var first = await BeginAsync(source))
        {
            (await first.CommitAsync()).ShouldBeRight();
        }

        await using var second = await BeginAsync(source);

        second.IsOwner.ShouldBeTrue();
        connection.BeginCalls.ShouldBe(2);
    }

    [Fact]
    public async Task JoiningWithADifferentIsolationLevel_IsRefused()
    {
        var connection = new StubTransactionalConnection();
        var accessor = new DbTransactionAccessor(connection, NullLogger<DbTransactionAccessor>.Instance);
        await using var owner = (await accessor.BeginOrJoinAsync(IsolationLevel.ReadCommitted)).ShouldBeRight();

        var joined = await accessor.BeginOrJoinAsync(IsolationLevel.Serializable);

        joined.ShouldBeErrorWithCode(TransactionErrors.IsolationLevelMismatchCode);
    }

    [Fact]
    public async Task ANonDbConnection_IsNotSupported()
    {
        var accessor = new DbTransactionAccessor(Substitute.For<IDbConnection>(), NullLogger<DbTransactionAccessor>.Instance);

        (await accessor.BeginOrJoinAsync()).ShouldBeErrorWithCode(TransactionErrors.NotSupportedCode);
    }
}
