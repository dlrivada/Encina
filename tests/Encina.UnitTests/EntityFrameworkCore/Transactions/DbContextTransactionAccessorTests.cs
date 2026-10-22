using System.Data;
using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Transactions;
using Encina.Messaging.Transactions;
using Encina.Testing;
using LanguageExt;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Transactions;

/// <summary>
/// Tests of <see cref="DbContextTransactionAccessor"/> against a real SQLite in-memory connection.
/// </summary>
[Trait("Category", "Unit")]
public sealed class DbContextTransactionAccessorTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _provider;
    private readonly TestDbContext _db;
    private readonly DbContextTransactionAccessor _accessor;

    public DbContextTransactionAccessorTests()
    {
        _connection.Open();
        _provider = new ServiceCollection()
            .AddDbContext<TestDbContext>(o => o.UseSqlite(_connection))
            .BuildServiceProvider();
        _db = _provider.GetRequiredService<TestDbContext>();
        _db.Database.EnsureCreated();
        _accessor = new DbContextTransactionAccessor(_db, NullLogger<DbContextTransactionAccessor>.Instance);
    }

    public void Dispose()
    {
        _accessor.Dispose();
        _provider.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public void Constructor_NullArguments_Throw()
    {
        Should.Throw<ArgumentNullException>(() => new DbContextTransactionAccessor(null!, NullLogger<DbContextTransactionAccessor>.Instance));
        Should.Throw<ArgumentNullException>(() => new DbContextTransactionAccessor(_db, null!));
    }

    [Fact]
    public async Task BeginOrJoin_FirstCall_OwnsTransactionAndCommits()
    {
        _accessor.HasActiveTransaction.ShouldBeFalse();
        _accessor.Current.ShouldBeNull();
        _accessor.Connection.ShouldNotBeNull();

        var lease = (await _accessor.BeginOrJoinAsync(CancellationToken.None)).ShouldBeRight();

        lease.IsOwner.ShouldBeTrue();
        _accessor.HasActiveTransaction.ShouldBeTrue();
        _accessor.Current.ShouldNotBeNull();

        (await lease.CommitAsync()).IsRight.ShouldBeTrue();
        await lease.DisposeAsync();
        _accessor.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task BeginOrJoin_SecondCall_JoinsAndCommitIsNoOp()
    {
        await using var owner = (await _accessor.BeginOrJoinAsync(CancellationToken.None)).ShouldBeRight();
        await using var joiner = (await _accessor.BeginOrJoinAsync(CancellationToken.None)).ShouldBeRight();

        joiner.IsOwner.ShouldBeFalse();
        (await joiner.CommitAsync()).IsRight.ShouldBeTrue();
        _accessor.HasActiveTransaction.ShouldBeTrue();
        (await owner.CommitAsync()).IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task JoinerRollback_MakesOwnerCommitFailWithRollbackOnly()
    {
        await using var owner = (await _accessor.BeginOrJoinAsync(CancellationToken.None)).ShouldBeRight();
        await using var joiner = (await _accessor.BeginOrJoinAsync(CancellationToken.None)).ShouldBeRight();

        await joiner.RollbackAsync();
        var committed = await owner.CommitAsync();

        committed.IsLeft.ShouldBeTrue();
        committed.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe(TransactionErrors.RollbackOnlyCode);
        _accessor.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task OwnerDisposedWithoutCommit_RollsBack()
    {
        var owner = (await _accessor.BeginOrJoinAsync(CancellationToken.None)).ShouldBeRight();
        await owner.DisposeAsync();

        _accessor.HasActiveTransaction.ShouldBeFalse();
        (await owner.CommitAsync()).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Join_DifferentIsolationLevel_ReturnsMismatch()
    {
        await using var owner = (await _accessor.BeginOrJoinAsync(IsolationLevel.Serializable, CancellationToken.None)).ShouldBeRight();

        var joined = await _accessor.BeginOrJoinAsync(IsolationLevel.ReadCommitted, CancellationToken.None);

        joined.IsLeft.ShouldBeTrue();
        joined.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe(TransactionErrors.IsolationLevelMismatchCode);
    }

    [Fact]
    public async Task BeginOrJoin_Canceled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await _accessor.BeginOrJoinAsync(cts.Token));
    }

    [Fact]
    public async Task ExternalTransaction_IsJoinedNeverCommitted_AndJoinerRollbackIsReal()
    {
        await using var external = await _db.Database.BeginTransactionAsync();

        var lease = (await _accessor.BeginOrJoinAsync(CancellationToken.None)).ShouldBeRight();
        lease.IsOwner.ShouldBeFalse();
        _accessor.HasActiveTransaction.ShouldBeTrue();
        (await lease.CommitAsync()).IsRight.ShouldBeTrue();
        _db.Database.CurrentTransaction.ShouldNotBeNull();

        await lease.RollbackAsync();

        _db.Database.CurrentTransaction.ShouldBeNull();
        (await lease.CommitAsync()).IsLeft.ShouldBeTrue();
        await lease.DisposeAsync();
        (await lease.CommitAsync()).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ExternalTransaction_IsolationMismatch_ReturnsLeft()
    {
        await using var external = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var joined = await _accessor.BeginOrJoinAsync(IsolationLevel.ReadCommitted, CancellationToken.None);

        joined.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task RetryingExecutionStrategy_RefusesToBeginOwnTransaction()
    {
        var services = new ServiceCollection()
            .AddDbContext<TestDbContext>(o => o.UseSqlite(_connection).ReplaceService<IExecutionStrategyFactory, RetryingFactory>())
            .BuildServiceProvider();
        await using (services)
        {
            var db = services.GetRequiredService<TestDbContext>();
            var accessor = new DbContextTransactionAccessor(db, NullLogger<DbContextTransactionAccessor>.Instance);

            var result = await accessor.BeginOrJoinAsync(CancellationToken.None);

            result.IsLeft.ShouldBeTrue();
            result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe(TransactionErrors.NotSupportedCode);
            db.Database.CurrentTransaction.ShouldBeNull();

            // Joining a transaction the application began stays allowed.
            await using var external = await db.Database.BeginTransactionAsync();
            (await accessor.BeginOrJoinAsync(CancellationToken.None)).IsRight.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task DisposeAsync_RollsBackOpenOwnedTransaction()
    {
        _ = (await _accessor.BeginOrJoinAsync(CancellationToken.None)).ShouldBeRight();

        await _accessor.DisposeAsync();

        _accessor.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task Dispose_Synchronously_RollsBackOpenOwnedTransaction()
    {
        _ = (await _accessor.BeginOrJoinAsync(CancellationToken.None)).ShouldBeRight();

        _accessor.Dispose();

        _db.Database.CurrentTransaction.ShouldBeNull();
    }

    private sealed class RetryingFactory : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new RetryingStrategy();
    }

    private sealed class RetryingStrategy : IExecutionStrategy
    {
        public bool RetriesOnFailure => true;

        public TResult Execute<TState, TResult>(TState state, Func<DbContext, TState, TResult> operation, Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded)
            => throw new NotSupportedException();

        public Task<TResult> ExecuteAsync<TState, TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}

internal static class LeaseAssertions
{
    internal static T ShouldBeRight<T>(this Either<EncinaError, T> either)
    {
        either.IsRight.ShouldBeTrue();
        return either.Match(Right: r => r, Left: _ => throw new InvalidOperationException());
    }
}
