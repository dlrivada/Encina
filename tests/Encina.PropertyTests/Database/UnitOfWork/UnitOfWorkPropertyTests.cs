using System.Data;
using Encina.DomainModeling;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.PropertyTests.Database.UnitOfWork;

/// <summary>
/// Property-based tests for the Unit of Work pattern.
/// Verifies invariants that MUST hold for all IUnitOfWork implementations.
/// </summary>
[Trait("Category", "Property")]
public sealed class UnitOfWorkPropertyTests
{
    #region Transaction Lifecycle Invariants

    [Fact]
    public void Property_InitialState_NoActiveTransaction()
    {
        // Property: A newly created UnitOfWork MUST NOT have an active transaction
        var (unitOfWork, _) = CreateMockUnitOfWork();

        unitOfWork.HasActiveTransaction.ShouldBeFalse(
            "Initial state must not have an active transaction");
    }

    [Fact]
    public async Task Property_AfterBeginTransaction_HasActiveTransaction()
    {
        // Property: After successful BeginTransaction, HasActiveTransaction MUST be true
        var (unitOfWork, mockConnection) = CreateMockUnitOfWork();
        SetupSuccessfulTransaction(mockConnection);

        var result = await unitOfWork.BeginTransactionAsync();

        result.IsRight.ShouldBeTrue("BeginTransaction should succeed");
        unitOfWork.HasActiveTransaction.ShouldBeTrue(
            "After BeginTransaction, HasActiveTransaction must be true");
    }

    [Fact]
    public async Task Property_AfterCommit_NoActiveTransaction()
    {
        // Property: After successful Commit, HasActiveTransaction MUST be false
        var (unitOfWork, mockConnection) = CreateMockUnitOfWork();
        SetupSuccessfulTransaction(mockConnection);

        await unitOfWork.BeginTransactionAsync();
        var result = await unitOfWork.CommitAsync();

        result.IsRight.ShouldBeTrue("Commit should succeed");
        unitOfWork.HasActiveTransaction.ShouldBeFalse(
            "After Commit, HasActiveTransaction must be false");
    }

    [Fact]
    public async Task Property_AfterRollback_NoActiveTransaction()
    {
        // Property: After Rollback, HasActiveTransaction MUST be false
        var (unitOfWork, mockConnection) = CreateMockUnitOfWork();
        SetupSuccessfulTransaction(mockConnection);

        await unitOfWork.BeginTransactionAsync();
        await unitOfWork.RollbackAsync();

        unitOfWork.HasActiveTransaction.ShouldBeFalse(
            "After Rollback, HasActiveTransaction must be false");
    }

    #endregion

    #region Transaction Uniqueness Invariants

    [Fact]
    public async Task Property_OnlyOneActiveTransaction()
    {
        // Property: BeginTransaction when a transaction is already active MUST return an error
        var (unitOfWork, mockConnection) = CreateMockUnitOfWork();
        SetupSuccessfulTransaction(mockConnection);

        await unitOfWork.BeginTransactionAsync();
        var secondResult = await unitOfWork.BeginTransactionAsync();

        secondResult.IsLeft.ShouldBeTrue(
            "Starting a second transaction must fail");
        secondResult.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue("Error must have a code");
            code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.TransactionAlreadyActiveErrorCode));
        });
    }

    [Fact]
    public async Task Property_CommitWithoutTransaction_ReturnsError()
    {
        // Property: Commit without an active transaction MUST return an error
        var (unitOfWork, _) = CreateMockUnitOfWork();

        var result = await unitOfWork.CommitAsync();

        result.IsLeft.ShouldBeTrue(
            "Commit without transaction must fail");
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue("Error must have a code");
            code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.NoActiveTransactionErrorCode));
        });
    }

    [Fact]
    public async Task Property_RollbackWithoutTransaction_IsNoOp()
    {
        // Property: Rollback without an active transaction MUST NOT throw
        var (unitOfWork, _) = CreateMockUnitOfWork();

        // Should not throw
        await unitOfWork.RollbackAsync();

        unitOfWork.HasActiveTransaction.ShouldBeFalse();
    }

    #endregion

    #region Transaction Sequence Invariants

    [Fact]
    public async Task Property_BeginCommitBegin_AllowsNewTransaction()
    {
        // Property: After Commit, a new transaction can be started
        var (unitOfWork, mockConnection) = CreateMockUnitOfWork();
        SetupSuccessfulTransaction(mockConnection);

        // First transaction cycle
        await unitOfWork.BeginTransactionAsync();
        await unitOfWork.CommitAsync();

        // Second transaction cycle
        var result = await unitOfWork.BeginTransactionAsync();

        result.IsRight.ShouldBeTrue(
            "New transaction after Commit should succeed");
        unitOfWork.HasActiveTransaction.ShouldBeTrue();
    }

    [Fact]
    public async Task Property_BeginRollbackBegin_AllowsNewTransaction()
    {
        // Property: After Rollback, a new transaction can be started
        var (unitOfWork, mockConnection) = CreateMockUnitOfWork();
        SetupSuccessfulTransaction(mockConnection);

        // First transaction cycle
        await unitOfWork.BeginTransactionAsync();
        await unitOfWork.RollbackAsync();

        // Second transaction cycle
        var result = await unitOfWork.BeginTransactionAsync();

        result.IsRight.ShouldBeTrue(
            "New transaction after Rollback should succeed");
        unitOfWork.HasActiveTransaction.ShouldBeTrue();
    }

    [Property(MaxTest = 20)]
    public bool Property_TransactionCyclesAreRepeatable(PositiveInt cycles)
    {
        // Property: Transaction cycles (begin-commit or begin-rollback) can be repeated N times
        var cycleCount = Math.Min(cycles.Get, 10); // Limit to 10 cycles for test speed
        var (unitOfWork, mockConnection) = CreateMockUnitOfWork();
        SetupSuccessfulTransaction(mockConnection);

        for (var i = 0; i < cycleCount; i++)
        {
            var beginResult = unitOfWork.BeginTransactionAsync().GetAwaiter().GetResult();
            if (beginResult.IsLeft) return false;
            if (!unitOfWork.HasActiveTransaction) return false;

            // Alternate between commit and rollback
            if (i % 2 == 0)
            {
                var commitResult = unitOfWork.CommitAsync().GetAwaiter().GetResult();
                if (commitResult.IsLeft) return false;
            }
            else
            {
                unitOfWork.RollbackAsync().GetAwaiter().GetResult();
            }

            if (unitOfWork.HasActiveTransaction) return false;
        }

        return true;
    }

    #endregion

    #region Disposal Invariants

    [Fact]
    public async Task Property_DisposeWithActiveTransaction_RollsBack()
    {
        // Property: Disposing with an active transaction MUST rollback
        var (unitOfWork, mockConnection) = CreateMockUnitOfWork();
        var mockTransaction = SetupSuccessfulTransaction(mockConnection);

        await unitOfWork.BeginTransactionAsync();

        await unitOfWork.DisposeAsync();

        mockTransaction.Received(1).Rollback();
    }

    [Fact]
    public async Task Property_DisposeTwice_DoesNotThrow()
    {
        // Property: Disposing twice MUST NOT throw
        var (unitOfWork, _) = CreateMockUnitOfWork();

        await unitOfWork.DisposeAsync();
        await unitOfWork.DisposeAsync(); // Should not throw
    }

    [Fact]
    public async Task Property_OperationsAfterDispose_ThrowObjectDisposedException()
    {
        // Property: All operations after dispose MUST throw ObjectDisposedException
        var (unitOfWork, _) = CreateMockUnitOfWork();

        await unitOfWork.DisposeAsync();

        await Should.ThrowAsync<ObjectDisposedException>(
            async () => await unitOfWork.BeginTransactionAsync());
        await Should.ThrowAsync<ObjectDisposedException>(
            async () => await unitOfWork.CommitAsync());
        await Should.ThrowAsync<ObjectDisposedException>(
            async () => await unitOfWork.RollbackAsync());
        await Should.ThrowAsync<ObjectDisposedException>(
            async () => await unitOfWork.SaveChangesAsync());
    }

    #endregion

    #region Repository Caching Invariants

    [Fact]
    public void Property_SameEntityType_ReturnsSameRepositoryInstance()
    {
        // Property: Repository<T>() for the same type MUST return the same instance
        var (unitOfWork, _) = CreateMockUnitOfWorkWithMapping();

        var repo1 = unitOfWork.Repository<TestUoWEntity, Guid>();
        var repo2 = unitOfWork.Repository<TestUoWEntity, Guid>();

        repo1.ShouldBeSameAs(repo2,
            "Same entity type must return same repository instance");
    }

    #endregion

    #region SaveChanges Invariants (ADO/Dapper specific)

    [Fact]
    public async Task Property_SaveChanges_ReturnsRight()
    {
        // Property: SaveChangesAsync MUST return Right (for ADO/Dapper, changes execute immediately)
        var (unitOfWork, _) = CreateMockUnitOfWork();

        var result = await unitOfWork.SaveChangesAsync();

        result.IsRight.ShouldBeTrue(
            "SaveChanges should always succeed for ADO/Dapper implementations");
    }

    #endregion

    #region Error Handling Invariants

    [Fact]
    public async Task Property_TransactionStartFailure_ReturnsError()
    {
        // Property: If BeginTransaction fails, it MUST return a TransactionStartFailed error
        var (unitOfWork, mockConnection) = CreateMockUnitOfWork();
        mockConnection.State.Returns(ConnectionState.Open);
        mockConnection.BeginTransaction().Returns(x => throw new InvalidOperationException("Cannot start transaction"));

        var result = await unitOfWork.BeginTransactionAsync();

        result.IsLeft.ShouldBeTrue("Failed BeginTransaction must return error");
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.TransactionStartFailedErrorCode));
        });
    }

    [Fact]
    public async Task Property_CommitFailure_ReturnsErrorAndRollsBack()
    {
        // Property: If Commit fails, it MUST return a CommitFailed error and rollback
        var (unitOfWork, mockConnection) = CreateMockUnitOfWork();
        var mockTransaction = SetupSuccessfulTransaction(mockConnection);
        mockTransaction.When(t => t.Commit()).Do(_ => throw new InvalidOperationException("Commit failed"));

        await unitOfWork.BeginTransactionAsync();
        var result = await unitOfWork.CommitAsync();

        result.IsLeft.ShouldBeTrue("Failed Commit must return error");
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.CommitFailedErrorCode));
        });
        mockTransaction.Received(1).Rollback();
    }

    #endregion

    #region Helper Methods

    private static (TestableUnitOfWork UnitOfWork, IDbConnection MockConnection) CreateMockUnitOfWork()
    {
        var mockConnection = Substitute.For<IDbConnection>();
        var mockServiceProvider = Substitute.For<IServiceProvider>();

        var unitOfWork = new TestableUnitOfWork(mockConnection, mockServiceProvider);
        return (unitOfWork, mockConnection);
    }

    private static (TestableUnitOfWork UnitOfWork, IDbConnection MockConnection) CreateMockUnitOfWorkWithMapping()
    {
        var mockConnection = Substitute.For<IDbConnection>();
        var mockServiceProvider = Substitute.For<IServiceProvider>();

        // Register a marker object to indicate mapping is available
        mockServiceProvider.GetService(typeof(TestMappingMarker<TestUoWEntity, Guid>))
            .Returns(new TestMappingMarker<TestUoWEntity, Guid>());

        var unitOfWork = new TestableUnitOfWork(mockConnection, mockServiceProvider);
        return (unitOfWork, mockConnection);
    }

    private static IDbTransaction SetupSuccessfulTransaction(IDbConnection mockConnection)
    {
        mockConnection.State.Returns(ConnectionState.Open);
        var mockTransaction = Substitute.For<IDbTransaction>();
        mockConnection.BeginTransaction().Returns(mockTransaction);
        return mockTransaction;
    }

    #endregion
}

#region Test Infrastructure

/// <summary>
/// Test entity for Unit of Work property tests.
/// </summary>
public sealed class TestUoWEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Testable Unit of Work implementation that uses mocks for testing invariants.
/// This mirrors the behavior of UnitOfWorkADO/UnitOfWorkDapper without database dependencies.
/// </summary>
public sealed class TestableUnitOfWork : IUnitOfWork
{
    private readonly IDbConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, object> _repositories = new();
    private IDbTransaction? _transaction;
    private bool _disposed;

    public TestableUnitOfWork(IDbConnection connection, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _connection = connection;
        _serviceProvider = serviceProvider;
    }

    public bool HasActiveTransaction => _transaction is not null;

    public IFunctionalRepository<TEntity, TId> Repository<TEntity, TId>()
        where TEntity : class
        where TId : notnull
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var entityType = typeof(TEntity);

        if (_repositories.TryGetValue(entityType, out var existing))
        {
            return (IFunctionalRepository<TEntity, TId>)existing;
        }

        // Check if mapping marker is registered (simulates real mapping registration)
        var marker = _serviceProvider.GetService(typeof(TestMappingMarker<TEntity, TId>));
        if (marker is null)
        {
            throw new InvalidOperationException($"No mapping for {typeof(TEntity).Name}");
        }

        var mockRepo = Substitute.For<IFunctionalRepository<TEntity, TId>>();
        _repositories[entityType] = mockRepo;
        return mockRepo;
    }

    public Task<LanguageExt.Either<EncinaError, int>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Task.FromResult(LanguageExt.Prelude.Right<EncinaError, int>(0));
    }

    public Task<LanguageExt.Either<EncinaError, LanguageExt.Unit>> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction is not null)
        {
            return Task.FromResult(
                LanguageExt.Prelude.Left<EncinaError, LanguageExt.Unit>(UnitOfWorkErrors.TransactionAlreadyActive()));
        }

        try
        {
            _transaction = _connection.BeginTransaction();
            return Task.FromResult(
                LanguageExt.Prelude.Right<EncinaError, LanguageExt.Unit>(LanguageExt.Unit.Default));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                LanguageExt.Prelude.Left<EncinaError, LanguageExt.Unit>(UnitOfWorkErrors.TransactionStartFailed(ex)));
        }
    }

    public Task<LanguageExt.Either<EncinaError, LanguageExt.Unit>> CommitAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction is null)
        {
            return Task.FromResult(
                LanguageExt.Prelude.Left<EncinaError, LanguageExt.Unit>(UnitOfWorkErrors.NoActiveTransaction()));
        }

        try
        {
            _transaction.Commit();
            DisposeTransaction();
            return Task.FromResult(
                LanguageExt.Prelude.Right<EncinaError, LanguageExt.Unit>(LanguageExt.Unit.Default));
        }
        catch (Exception ex)
        {
            RollbackInternal();
            return Task.FromResult(
                LanguageExt.Prelude.Left<EncinaError, LanguageExt.Unit>(UnitOfWorkErrors.CommitFailed(ex)));
        }
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        RollbackInternal();
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;

        _disposed = true;
        RollbackInternal();
        _repositories.Clear();

        return ValueTask.CompletedTask;
    }

    private void RollbackInternal()
    {
        if (_transaction is null) return;

        try { _transaction.Rollback(); }
        catch { /* Swallow */ }
        finally { DisposeTransaction(); }
    }

    private void DisposeTransaction()
    {
        _transaction?.Dispose();
        _transaction = null;
    }
}

/// <summary>
/// Marker class to simulate entity mapping registration in tests.
/// This avoids depending on provider-specific IEntityMapping interfaces.
/// </summary>
public sealed class TestMappingMarker<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
}

#endregion
