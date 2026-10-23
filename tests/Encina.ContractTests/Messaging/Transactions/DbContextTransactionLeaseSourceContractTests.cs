using System.Data;
using Encina.EntityFrameworkCore.Transactions;
using Encina.Messaging.Transactions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Messaging.Transactions;

/// <summary>
/// The <see cref="ITransactionLeaseSource"/> contract (#718) run against the real
/// <see cref="DbContextTransactionAccessor"/> over a SQLite in-memory database: the first caller owns the
/// transaction, later callers join, a joiner's rollback makes the owner's commit fail, and rows written through
/// the context commit or roll back with the owner.
/// </summary>
[Trait("Category", "Contract")]
public sealed class DbContextTransactionLeaseSourceContractTests : IDisposable
{
    private sealed class Row
    {
        public int Id { get; set; }
    }

    private sealed class Db(DbContextOptions<Db> options) : DbContext(options)
    {
        public DbSet<Row> Rows => Set<Row>();
    }

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly Db _db;
    private readonly DbContextTransactionAccessor _source;

    public DbContextTransactionLeaseSourceContractTests()
    {
        _connection.Open();
        _db = new Db(new DbContextOptionsBuilder<Db>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        _source = new DbContextTransactionAccessor(_db, NullLogger<DbContextTransactionAccessor>.Instance);
    }

    public void Dispose()
    {
        _source.Dispose();
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<ITransactionLease> BeginAsync() => (await ((ITransactionLeaseSource)_source).BeginOrJoinAsync()).ShouldBeRight();

    private async Task WriteRowAsync()
    {
        _db.Rows.Add(new Row());
        await _db.SaveChangesAsync();
    }

    private async Task<int> CountRowsAsync()
    {
        await using var verify = new Db(new DbContextOptionsBuilder<Db>().UseSqlite(_connection).Options);
        return await verify.Rows.CountAsync();
    }

    [Fact]
    public async Task HasActiveTransaction_IsFalseUntilAnOwnerBegins_AndAfterItEnds()
    {
        ((ITransactionLeaseSource)_source).HasActiveTransaction.ShouldBeFalse();

        await using (var owner = await BeginAsync())
        {
            ((ITransactionLeaseSource)_source).HasActiveTransaction.ShouldBeTrue();
            (await owner.CommitAsync()).ShouldBeRight();
        }

        ((ITransactionLeaseSource)_source).HasActiveTransaction.ShouldBeFalse();
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
    public async Task JoinerCommit_IsANoOp_AndOwnerCommitPersistsTheRows()
    {
        await using var owner = await BeginAsync();
        await using var joiner = await BeginAsync();
        await WriteRowAsync();

        (await joiner.CommitAsync()).ShouldBeRight();
        ((ITransactionLeaseSource)_source).HasActiveTransaction.ShouldBeTrue();
        (await owner.CommitAsync()).ShouldBeRight();

        (await CountRowsAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task JoinerRollback_MakesTheOwnerCommitFailWithRollbackOnly_AndNoRowRemains()
    {
        await using var owner = await BeginAsync();
        await using var joiner = await BeginAsync();
        await WriteRowAsync();

        await joiner.RollbackAsync();
        var committed = await owner.CommitAsync();

        committed.ShouldBeErrorWithCode(TransactionErrors.RollbackOnlyCode);
        (await CountRowsAsync()).ShouldBe(0);
        ((ITransactionLeaseSource)_source).HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task OwnerRollback_RollsBack_AndALaterCommitIsRefused()
    {
        var owner = await BeginAsync();
        await WriteRowAsync();

        await owner.RollbackAsync();
        var committed = await owner.CommitAsync();

        committed.ShouldBeErrorWithCode(TransactionErrors.LeaseDisposedCode);
        (await CountRowsAsync()).ShouldBe(0);
        await owner.DisposeAsync();
    }

    [Fact]
    public async Task DisposingAnUnfinishedOwner_RollsBack()
    {
        var owner = await BeginAsync();
        await WriteRowAsync();

        await owner.DisposeAsync();

        (await CountRowsAsync()).ShouldBe(0);
        ((ITransactionLeaseSource)_source).HasActiveTransaction.ShouldBeFalse();
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

    [Fact]
    public async Task JoiningWithADifferentIsolationLevel_IsRefused()
    {
        await using var owner = await BeginAsync();

        var joined = await _source.BeginOrJoinAsync(IsolationLevel.ReadUncommitted);

        joined.ShouldBeErrorWithCode(TransactionErrors.IsolationLevelMismatchCode);
    }

    [Fact]
    public async Task ApplicationBegunTransaction_IsJoinedNeverCommitted_AndAJoinerRollbackReallyRollsItBack()
    {
        await using var applicationTransaction = await _db.Database.BeginTransactionAsync();
        await using var lease = await BeginAsync();
        await WriteRowAsync();
        lease.IsOwner.ShouldBeFalse();

        (await lease.CommitAsync()).ShouldBeRight();
        _db.Database.CurrentTransaction.ShouldNotBeNull();

        await lease.RollbackAsync();

        _db.Database.CurrentTransaction.ShouldBeNull();
        (await lease.CommitAsync()).ShouldBeErrorWithCode(TransactionErrors.RollbackOnlyCode);
        (await CountRowsAsync()).ShouldBe(0);
    }
}
