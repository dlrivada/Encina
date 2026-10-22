#pragma warning disable CA2012 // NSubstitute configures ValueTask-returning members with .Returns
using Encina.DomainModeling;
using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Transactions;
using Encina.EntityFrameworkCore.UnitOfWork;
using Encina.Messaging.Outbox;
using Encina.Messaging.Transactions;
using Encina.Testing;
using LanguageExt;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Transactions;

/// <summary>
/// Tests of the transaction shared by the EF Core behavior, unit of work and registration.
/// </summary>
[Trait("Category", "Unit")]
public sealed class SharedTransactionTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _provider;
    private readonly TestDbContext _db;
    private readonly DbContextTransactionAccessor _accessor;
    private readonly IRequestContext _context = Substitute.For<IRequestContext>();

    public SharedTransactionTests()
    {
        _connection.Open();
        _provider = new ServiceCollection().AddDbContext<TestDbContext>(o => o.UseSqlite(_connection)).BuildServiceProvider();
        _db = _provider.GetRequiredService<TestDbContext>();
        _db.Database.EnsureCreated();
        _accessor = new DbContextTransactionAccessor(_db, NullLogger<DbContextTransactionAccessor>.Instance);
        _context.CorrelationId.Returns("c1");
    }

    public void Dispose()
    {
        _accessor.Dispose();
        _provider.Dispose();
        _connection.Dispose();
    }

    private sealed record TestTransactionalCommand : ICommand<string>, ITransactionalCommand;

    private TransactionPipelineBehavior<TestTransactionalCommand, string> Behavior()
        => new(_accessor, NullLogger<TransactionPipelineBehavior<TestTransactionalCommand, string>>.Instance);

    [Fact]
    public async Task Behavior_Joiner_DoesNotCommit_OwnerCommitsAfterwards()
    {
        await using var owner = (await _accessor.BeginOrJoinAsync(CancellationToken.None)).Match(Right: r => r, Left: _ => throw new InvalidOperationException());
        var seenOwner = true;

        var result = await Behavior().Handle(new TestTransactionalCommand(), _context, () =>
        {
            seenOwner = _accessor.HasActiveTransaction;
            return ValueTask.FromResult(Either<EncinaError, string>.Right("ok"));
        }, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        seenOwner.ShouldBeTrue();
        _accessor.HasActiveTransaction.ShouldBeTrue();
        (await owner.CommitAsync()).IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Behavior_JoinerLeft_MakesOwnerCommitRollbackOnly()
    {
        await using var owner = (await _accessor.BeginOrJoinAsync(CancellationToken.None)).Match(Right: r => r, Left: _ => throw new InvalidOperationException());

        var result = await Behavior().Handle(new TestTransactionalCommand(), _context,
            () => ValueTask.FromResult(Either<EncinaError, string>.Left(EncinaErrors.Create("x.fail", "no"))),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        (await owner.CommitAsync()).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Behavior_OwnerHandlerLeft_RollsBackAndReturnsLeft()
    {
        var result = await Behavior().Handle(new TestTransactionalCommand(), _context,
            () => ValueTask.FromResult(Either<EncinaError, string>.Left(EncinaErrors.Create("x.fail", "no"))),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        _accessor.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task Behavior_HandlerThrows_ReturnsTransactionFailed()
    {
        var result = await Behavior().Handle(new TestTransactionalCommand(), _context,
            () => throw new InvalidOperationException("boom"), CancellationToken.None);

        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("transaction.failed");
        _accessor.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task Behavior_HandlerCanceled_RollsBackAndRethrows()
    {
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await Behavior().Handle(new TestTransactionalCommand(), _context,
                () => throw new OperationCanceledException(), CancellationToken.None));

        _accessor.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task Behavior_BeginFails_ReturnsLeaseError()
    {
        var failing = Substitute.For<IDbTransactionAccessor>();
        failing.BeginOrJoinAsync(Arg.Any<System.Data.IsolationLevel?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Either<EncinaError, IDbTransactionLease>.Left(TransactionErrors.LeaseDisposed())));
        var behavior = new TransactionPipelineBehavior<TestTransactionalCommand, string>(
            failing, NullLogger<TransactionPipelineBehavior<TestTransactionalCommand, string>>.Instance);
        var called = false;

        var result = await behavior.Handle(new TestTransactionalCommand(), _context, () =>
        {
            called = true;
            return ValueTask.FromResult(Either<EncinaError, string>.Right("ok"));
        }, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        called.ShouldBeFalse();
    }

    [Fact]
    public async Task Behavior_CommitFails_ReplacesResponse()
    {
        var lease = Substitute.For<IDbTransactionLease>();
        lease.IsOwner.Returns(true);
        lease.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Either<EncinaError, Unit>.Left(TransactionErrors.CommitFailed(new InvalidOperationException()))));
        var accessor = Substitute.For<IDbTransactionAccessor>();
        accessor.BeginOrJoinAsync(Arg.Any<System.Data.IsolationLevel?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Either<EncinaError, IDbTransactionLease>.Right(lease)));
        var behavior = new TransactionPipelineBehavior<TestTransactionalCommand, string>(
            accessor, NullLogger<TransactionPipelineBehavior<TestTransactionalCommand, string>>.Instance);

        var result = await behavior.Handle(new TestTransactionalCommand(), _context,
            () => ValueTask.FromResult(Either<EncinaError, string>.Right("ok")), CancellationToken.None);

        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe(TransactionErrors.CommitFailedCode);
    }

    [Fact]
    public async Task UnitOfWork_InsideActiveTransaction_JoinsAndOnlyItsOwnSecondBeginIsRefused()
    {
        await using var owner = (await _accessor.BeginOrJoinAsync(CancellationToken.None)).Match(Right: r => r, Left: _ => throw new InvalidOperationException());
        await using var uow = new UnitOfWorkEF(_db, _accessor, _provider);

        (await uow.BeginTransactionAsync()).IsRight.ShouldBeTrue();
        uow.HasActiveTransaction.ShouldBeTrue();
        (await uow.BeginTransactionAsync()).IsLeft.ShouldBeTrue();
        (await uow.CommitAsync()).IsRight.ShouldBeTrue();
        uow.HasActiveTransaction.ShouldBeFalse();
        _accessor.HasActiveTransaction.ShouldBeTrue();
    }

    [Fact]
    public async Task UnitOfWork_Rollback_ThenOwnerCommitIsRefused()
    {
        await using var owner = (await _accessor.BeginOrJoinAsync(CancellationToken.None)).Match(Right: r => r, Left: _ => throw new InvalidOperationException());
        await using var uow = new UnitOfWorkEF(_db, _accessor, _provider);
        await uow.BeginTransactionAsync();

        await uow.RollbackAsync();

        (await owner.CommitAsync()).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task UnitOfWork_Alone_OwnsAndCommits()
    {
        await using var uow = new UnitOfWorkEF(_db, _accessor, _provider);

        (await uow.BeginTransactionAsync()).IsRight.ShouldBeTrue();
        _db.Database.CurrentTransaction.ShouldNotBeNull();
        (await uow.CommitAsync()).IsRight.ShouldBeTrue();
        _db.Database.CurrentTransaction.ShouldBeNull();
        (await uow.CommitAsync()).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Registration_ResolvesSharedAccessorAndOutboxBehavior_WithValidation()
    {
        foreach (var (outbox, tx) in new[] { (true, false), (false, true), (true, true) })
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(Substitute.For<IEncina>());
            services.AddDbContext<TestDbContext>(o => o.UseSqlite(_connection));
            services.AddEncinaEntityFrameworkCore<TestDbContext>(c =>
            {
                c.UseOutbox = outbox;
                c.UseTransactions = tx;
            });
            services.AddEncinaUnitOfWork<TestDbContext>();

            await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
            await using var scope = provider.CreateAsyncScope();

            var accessor = scope.ServiceProvider.GetRequiredService<IDbTransactionAccessor>();
            scope.ServiceProvider.GetRequiredService<ITransactionLeaseSource>().ShouldBeSameAs(accessor);
            services.ShouldContain(d => d.ServiceType == typeof(IUnitOfWork) && d.ImplementationType == typeof(UnitOfWorkEF));

            var behaviors = services
                .Where(d => d.ServiceType == typeof(IPipelineBehavior<,>))
                .Select(d => d.ImplementationType)
                .ToList();
            behaviors.Contains(typeof(TransactionPipelineBehavior<,>)).ShouldBe(tx);
            behaviors.Contains(typeof(TransactionalOutboxPipelineBehavior<,>)).ShouldBe(outbox);
            if (outbox && tx)
            {
                behaviors.IndexOf(typeof(TransactionPipelineBehavior<,>)).ShouldBeLessThan(behaviors.IndexOf(typeof(TransactionalOutboxPipelineBehavior<,>)));
            }
        }
    }
}
