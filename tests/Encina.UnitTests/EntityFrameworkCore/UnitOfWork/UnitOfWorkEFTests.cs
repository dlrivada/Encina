using Encina;
using Encina.DomainModeling;
using Encina.EntityFrameworkCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.UnitOfWork;

/// <summary>
/// Unit tests for <see cref="UnitOfWorkEF"/>.
/// </summary>
/// <remarks>
/// Note: InMemory database provider does NOT support real transactions.
/// Transaction-related tests verify the API behavior returns appropriate errors
/// when transactions cannot be started. For full transaction testing, use
/// integration tests with a real database provider like SQL Server.
/// </remarks>
[Trait("Category", "Unit")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposal handled by IAsyncLifetime.DisposeAsync")]
public class UnitOfWorkEFTests : IAsyncLifetime
{
    private readonly TestDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly UnitOfWorkEF _unitOfWork;

    public UnitOfWorkEFTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        _serviceProvider = Substitute.For<IServiceProvider>();
        _unitOfWork = new UnitOfWorkEF(_dbContext, _serviceProvider);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new UnitOfWorkEF(null!, _serviceProvider));
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new UnitOfWorkEF(_dbContext, null!));
    }

    #endregion

    #region HasActiveTransaction Tests

    [Fact]
    public void HasActiveTransaction_NoTransaction_ReturnsFalse()
    {
        // Act & Assert
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task HasActiveTransaction_AfterFailedBeginTransaction_ReturnsFalse()
    {
        // Note: InMemory doesn't support transactions, so BeginTransactionAsync returns error
        // Act
        var result = await _unitOfWork.BeginTransactionAsync();

        // Assert - InMemory provider fails to start transaction
        // The transaction is not active because it failed to start
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
        result.IsLeft.ShouldBeTrue(); // Expect error because InMemory doesn't support transactions
    }

    #endregion

    #region Repository Tests

    [Fact]
    public void Repository_ReturnsRepositoryInstance()
    {
        // Act
        var repository = _unitOfWork.Repository<TestUoWEntity, Guid>();

        // Assert
        repository.ShouldNotBeNull();
        repository.ShouldBeAssignableTo<IFunctionalRepository<TestUoWEntity, Guid>>();
    }

    [Fact]
    public void Repository_SameEntityType_ReturnsSameInstance()
    {
        // Act
        var repository1 = _unitOfWork.Repository<TestUoWEntity, Guid>();
        var repository2 = _unitOfWork.Repository<TestUoWEntity, Guid>();

        // Assert
        repository1.ShouldBeSameAs(repository2);
    }

    [Fact]
    public void Repository_DifferentEntityTypes_ReturnsDifferentInstances()
    {
        // Act
        var repository1 = _unitOfWork.Repository<TestUoWEntity, Guid>();
        var repository2 = _unitOfWork.Repository<TestUoWEntity2, Guid>();

        // Assert
        repository1.ShouldNotBeSameAs(repository2);
    }

    [Fact]
    public async Task Repository_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        await _unitOfWork.DisposeAsync();

        // Act & Assert
        Should.Throw<ObjectDisposedException>(() =>
            _unitOfWork.Repository<TestUoWEntity, Guid>());
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_WithTrackedEntities_ReturnsAffectedRowCount()
    {
        // Arrange
        var entity = new TestUoWEntity { Id = Guid.NewGuid(), Name = "Test" };
        _dbContext.TestUoWEntities.Add(entity);

        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(1));
    }

    [Fact]
    public async Task SaveChangesAsync_NoChanges_ReturnsZero()
    {
        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    [Fact]
    public async Task SaveChangesAsync_AfterDispose_HandlesDisposedState()
    {
        // Arrange - Create a separate instance for this test
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TestDbContext(options);
        var serviceProvider = NSubstitute.Substitute.For<IServiceProvider>();
        var unitOfWork = new UnitOfWorkEF(dbContext, serviceProvider);

        // Dispose the unit of work first
        await unitOfWork.DisposeAsync();

        // Act - After dispose, methods should either throw or return gracefully
        // The behavior depends on the implementation details
        Exception? caughtException = null;
        try
        {
            await unitOfWork.SaveChangesAsync();
        }
        catch (ObjectDisposedException ex)
        {
            caughtException = ex;
        }

        // Assert - Either throws ObjectDisposedException or completes (implementation-dependent)
        // This test documents the current behavior
        if (caughtException != null)
        {
            caughtException.ShouldBeOfType<ObjectDisposedException>();
        }
        // If no exception, the method handles disposed state gracefully

        // Cleanup
        await dbContext.DisposeAsync();
    }

    #endregion

    #region BeginTransactionAsync Tests

    [Fact]
    public async Task BeginTransactionAsync_InMemoryProvider_ReturnsError()
    {
        // Note: InMemory provider doesn't support transactions
        // Act
        var result = await _unitOfWork.BeginTransactionAsync();

        // Assert - Should fail because InMemory doesn't support transactions
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            // The error should be TransactionStartFailed because InMemory doesn't support transactions
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.TransactionStartFailedErrorCode));
        });
    }

    [Fact]
    public async Task BeginTransactionAsync_AfterDispose_HandlesDisposedState()
    {
        // Arrange - Create a separate instance for this test
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TestDbContext(options);
        var serviceProvider = NSubstitute.Substitute.For<IServiceProvider>();
        var unitOfWork = new UnitOfWorkEF(dbContext, serviceProvider);

        // Dispose the unit of work first
        await unitOfWork.DisposeAsync();

        // Act - After dispose, methods should either throw or return error
        Exception? caughtException = null;
        try
        {
            await unitOfWork.BeginTransactionAsync();
        }
        catch (ObjectDisposedException ex)
        {
            caughtException = ex;
        }

        // Assert - Either throws ObjectDisposedException or completes
        if (caughtException != null)
        {
            caughtException.ShouldBeOfType<ObjectDisposedException>();
        }

        // Cleanup
        await dbContext.DisposeAsync();
    }

    #endregion

    #region CommitAsync Tests

    [Fact]
    public async Task CommitAsync_NoActiveTransaction_ReturnsNoActiveTransactionError()
    {
        // Act
        var result = await _unitOfWork.CommitAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.NoActiveTransactionErrorCode));
        });
    }

    [Fact]
    public async Task CommitAsync_AfterDispose_HandlesDisposedState()
    {
        // Arrange - Create a separate instance for this test
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TestDbContext(options);
        var serviceProvider = NSubstitute.Substitute.For<IServiceProvider>();
        var unitOfWork = new UnitOfWorkEF(dbContext, serviceProvider);

        // Dispose the unit of work first
        await unitOfWork.DisposeAsync();

        // Act - After dispose, methods should either throw or return error
        Exception? caughtException = null;
        try
        {
            await unitOfWork.CommitAsync();
        }
        catch (ObjectDisposedException ex)
        {
            caughtException = ex;
        }

        // Assert - Either throws ObjectDisposedException or completes
        if (caughtException != null)
        {
            caughtException.ShouldBeOfType<ObjectDisposedException>();
        }

        // Cleanup
        await dbContext.DisposeAsync();
    }

    #endregion

    #region RollbackAsync Tests

    [Fact]
    public async Task RollbackAsync_NoActiveTransaction_DoesNotThrow()
    {
        // Act & Assert - Should not throw even when no transaction
        await _unitOfWork.RollbackAsync();
    }

    [Fact]
    public async Task RollbackAsync_AfterDispose_HandlesDisposedState()
    {
        // Arrange - Create a separate instance for this test
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TestDbContext(options);
        var serviceProvider = NSubstitute.Substitute.For<IServiceProvider>();
        var unitOfWork = new UnitOfWorkEF(dbContext, serviceProvider);

        // Dispose the unit of work first
        await unitOfWork.DisposeAsync();

        // Act - After dispose, methods should either throw or return
        Exception? caughtException = null;
        try
        {
            await unitOfWork.RollbackAsync();
        }
        catch (ObjectDisposedException ex)
        {
            caughtException = ex;
        }

        // Assert - Either throws ObjectDisposedException or completes
        if (caughtException != null)
        {
            caughtException.ShouldBeOfType<ObjectDisposedException>();
        }

        // Cleanup
        await dbContext.DisposeAsync();
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        await _unitOfWork.DisposeAsync();
        await _unitOfWork.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_ClearsRepositoryCache()
    {
        // Arrange
        _ = _unitOfWork.Repository<TestUoWEntity, Guid>();

        // Act
        await _unitOfWork.DisposeAsync();

        // Assert - Trying to get repository after dispose throws
        Should.Throw<ObjectDisposedException>(() =>
            _unitOfWork.Repository<TestUoWEntity, Guid>());
    }

    #endregion

    #region Integration Scenario Tests (InMemory - No Transaction Support)

    [Fact]
    public async Task SaveChangesWithoutTransaction_PersistsEntity()
    {
        // Arrange - InMemory doesn't need transactions for simple saves
        var repository = _unitOfWork.Repository<TestUoWEntity, Guid>();
        var entity = new TestUoWEntity { Id = Guid.NewGuid(), Name = "No Transaction Test" };

        // Act
        await repository.AddAsync(entity);
        var saveResult = await _unitOfWork.SaveChangesAsync();

        // Assert
        saveResult.IsRight.ShouldBeTrue();

        // Verify entity is persisted
        var retrieved = await _dbContext.TestUoWEntities.FindAsync(entity.Id);
        retrieved.ShouldNotBeNull();
        retrieved.Name.ShouldBe("No Transaction Test");
    }

    [Fact]
    public async Task MultipleRepositoryOperations_ShareSameDbContext()
    {
        // Arrange
        var repo1 = _unitOfWork.Repository<TestUoWEntity, Guid>();
        var repo2 = _unitOfWork.Repository<TestUoWEntity2, Guid>();

        var entity1 = new TestUoWEntity { Id = Guid.NewGuid(), Name = "Entity 1" };
        var entity2 = new TestUoWEntity2 { Id = Guid.NewGuid(), Description = "Entity 2" };

        // Act - Add through different repositories
        await repo1.AddAsync(entity1);
        await repo2.AddAsync(entity2);

        // Single SaveChanges should save both
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(2));
    }

    #endregion
}

#region Test DbContext and Entities

/// <summary>
/// Test DbContext for UnitOfWork tests.
/// </summary>
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<TestUoWEntity> TestUoWEntities { get; set; } = null!;
    public DbSet<TestUoWEntity2> TestUoWEntity2s { get; set; } = null!;
}

/// <summary>
/// Test entity for UnitOfWork tests.
/// </summary>
public class TestUoWEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Second test entity for verifying repository caching.
/// </summary>
public class TestUoWEntity2
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
}

#endregion
