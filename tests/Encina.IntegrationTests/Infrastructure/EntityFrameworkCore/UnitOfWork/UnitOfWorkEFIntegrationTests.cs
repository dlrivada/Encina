using Encina.DomainModeling;
using Encina.EntityFrameworkCore.UnitOfWork;
using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Repository;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.UnitOfWork;

/// <summary>
/// Integration tests for <see cref="UnitOfWorkEF"/> using real SQL Server.
/// </summary>
[Collection("RepositoryTests")]
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposal handled by IAsyncLifetime.DisposeAsync")]
public class UnitOfWorkEFIntegrationTests : IAsyncLifetime
{
    private readonly RepositoryFixture _fixture;
    private RepositoryTestDbContext _dbContext = null!;
    private UnitOfWorkEF _unitOfWork = null!;
    private IServiceProvider _serviceProvider = null!;

    public UnitOfWorkEFIntegrationTests(RepositoryFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearDataAsync();
        _dbContext = _fixture.CreateDbContext();
        _serviceProvider = new ServiceCollection().BuildServiceProvider();
        _unitOfWork = new UnitOfWorkEF(_dbContext, _serviceProvider);
    }

    public async ValueTask DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
        _dbContext.Dispose();
    }

    #region Transaction Commit Tests

    [Fact]
    public async Task Transaction_CommitMultipleEntities_AllPersisted()
    {
        // Arrange
        var repository = _unitOfWork.Repository<TestEntity, Guid>();
        var entity1 = CreateTestEntity("Entity 1");
        var entity2 = CreateTestEntity("Entity 2");
        var entity3 = CreateTestEntity("Entity 3");

        // Act
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        await repository.AddAsync(entity1);
        await repository.AddAsync(entity2);
        await repository.AddAsync(entity3);

        var saveResult = await _unitOfWork.SaveChangesAsync();
        saveResult.IsRight.ShouldBeTrue();

        var commitResult = await _unitOfWork.CommitAsync();
        commitResult.IsRight.ShouldBeTrue();

        // Assert - Verify with a fresh context
        using var verifyContext = _fixture.CreateDbContext();
        var count = verifyContext.TestEntities.Count();
        count.ShouldBe(3);
    }

    [Fact]
    public async Task Transaction_ModifyEntities_ChangesPersisted()
    {
        // Arrange - Create initial entity
        var entity = CreateTestEntity("Original Name");
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act - Modify in transaction
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        var repository = _unitOfWork.Repository<TestEntity, Guid>();
        var getResult = await repository.GetByIdAsync(entity.Id);
        getResult.IsRight.ShouldBeTrue();

        _ = getResult.IfRight(e => e.Name = "Updated Name");
        var saveResult = await _unitOfWork.SaveChangesAsync();
        saveResult.IsRight.ShouldBeTrue();

        var commitResult = await _unitOfWork.CommitAsync();
        commitResult.IsRight.ShouldBeTrue();

        // Assert - Verify with fresh context
        using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.TestEntities.FindAsync(entity.Id);
        updated.ShouldNotBeNull();
        updated!.Name.ShouldBe("Updated Name");
    }

    #endregion

    #region Transaction Rollback Tests

    [Fact]
    public async Task Transaction_Rollback_NoChangesPersisted()
    {
        // Arrange
        var repository = _unitOfWork.Repository<TestEntity, Guid>();
        var entity = CreateTestEntity("Should Not Persist");

        // Act
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        await repository.AddAsync(entity);
        var saveResult = await _unitOfWork.SaveChangesAsync();
        saveResult.IsRight.ShouldBeTrue();

        // Rollback instead of commit
        await _unitOfWork.RollbackAsync();

        // Assert - Verify with fresh context
        using var verifyContext = _fixture.CreateDbContext();
        var count = verifyContext.TestEntities.Count();
        count.ShouldBe(0);
    }

    [Fact]
    public async Task Transaction_RollbackAfterModify_OriginalValuePreserved()
    {
        // Arrange - Create initial entity
        var entity = CreateTestEntity("Original Name");
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act - Modify in transaction then rollback
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        var repository = _unitOfWork.Repository<TestEntity, Guid>();
        var getResult = await repository.GetByIdAsync(entity.Id);
        getResult.IsRight.ShouldBeTrue();

        _ = getResult.IfRight(e => e.Name = "Modified Name");
        var saveResult = await _unitOfWork.SaveChangesAsync();
        saveResult.IsRight.ShouldBeTrue();

        await _unitOfWork.RollbackAsync();

        // Assert - Original value preserved
        using var verifyContext = _fixture.CreateDbContext();
        var persisted = await verifyContext.TestEntities.FindAsync(entity.Id);
        persisted.ShouldNotBeNull();
        persisted!.Name.ShouldBe("Original Name");
    }

    #endregion

    #region Auto-Rollback on Dispose Tests

    [Fact]
    public async Task Dispose_WithUncommittedTransaction_AutoRollback()
    {
        // Arrange
        var repository = _unitOfWork.Repository<TestEntity, Guid>();
        var entity = CreateTestEntity("Uncommitted Entity");

        // Act
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        await repository.AddAsync(entity);
        var saveResult = await _unitOfWork.SaveChangesAsync();
        saveResult.IsRight.ShouldBeTrue();

        // Don't commit - just dispose
        await _unitOfWork.DisposeAsync();

        // Assert - Changes should be rolled back
        using var verifyContext = _fixture.CreateDbContext();
        var count = verifyContext.TestEntities.Count();
        count.ShouldBe(0);
    }

    #endregion

    #region Repository Caching Tests

    [Fact]
    public void Repository_SameEntityType_ReturnsSameInstance()
    {
        // Act
        var repository1 = _unitOfWork.Repository<TestEntity, Guid>();
        var repository2 = _unitOfWork.Repository<TestEntity, Guid>();

        // Assert
        repository1.ShouldBeSameAs(repository2);
    }

    #endregion

    #region SaveChanges Result Tests

    [Fact]
    public async Task SaveChangesAsync_ReturnsAffectedRowCount()
    {
        // Arrange
        var repository = _unitOfWork.Repository<TestEntity, Guid>();
        await repository.AddAsync(CreateTestEntity("Entity 1"));
        await repository.AddAsync(CreateTestEntity("Entity 2"));

        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(2));
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

    #endregion

    #region Transaction State Tests

    [Fact]
    public async Task BeginTransaction_SetsHasActiveTransactionTrue()
    {
        // Arrange
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();

        // Act
        var result = await _unitOfWork.BeginTransactionAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        _unitOfWork.HasActiveTransaction.ShouldBeTrue();
    }

    [Fact]
    public async Task Commit_ClearsHasActiveTransaction()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();
        _unitOfWork.HasActiveTransaction.ShouldBeTrue();

        // Act
        await _unitOfWork.CommitAsync();

        // Assert
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task Rollback_ClearsHasActiveTransaction()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();
        _unitOfWork.HasActiveTransaction.ShouldBeTrue();

        // Act
        await _unitOfWork.RollbackAsync();

        // Assert
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task BeginTransaction_WhenAlreadyActive_ReturnsError()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();

        // Act
        var result = await _unitOfWork.BeginTransactionAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.TransactionAlreadyActiveErrorCode));
        });
    }

    [Fact]
    public async Task Commit_WhenNoTransaction_ReturnsError()
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

    #endregion

    #region Complex Workflow Tests

    [Fact]
    public async Task ComplexWorkflow_MultipleOperations_CommitPreservesAll()
    {
        // Arrange
        var repository = _unitOfWork.Repository<TestEntity, Guid>();

        // Act - Begin transaction
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        // Add entities
        var entity1 = CreateTestEntity("First");
        var entity2 = CreateTestEntity("Second");
        await repository.AddAsync(entity1);
        await repository.AddAsync(entity2);

        // Save first batch
        var save1Result = await _unitOfWork.SaveChangesAsync();
        save1Result.IsRight.ShouldBeTrue();

        // Modify one entity
        entity1.Name = "First Modified";

        // Add another entity
        var entity3 = CreateTestEntity("Third");
        await repository.AddAsync(entity3);

        // Save second batch
        var save2Result = await _unitOfWork.SaveChangesAsync();
        save2Result.IsRight.ShouldBeTrue();

        // Commit
        var commitResult = await _unitOfWork.CommitAsync();
        commitResult.IsRight.ShouldBeTrue();

        // Assert - All changes persisted
        using var verifyContext = _fixture.CreateDbContext();
        var entities = verifyContext.TestEntities.ToList();
        entities.Count.ShouldBe(3);
        entities.ShouldContain(e => e.Name == "First Modified");
        entities.ShouldContain(e => e.Name == "Second");
        entities.ShouldContain(e => e.Name == "Third");
    }

    #endregion

    #region UpdateImmutable Integration Tests

    [Fact]
    public async Task UpdateImmutable_ModifiesTrackedEntity()
    {
        // Arrange - Create initial aggregate
        var id = Guid.NewGuid();
        var original = new TestImmutableAggregate(id) { Name = "Original", Amount = 100m };
        _dbContext.ImmutableAggregates.Add(original);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Create UnitOfWork with fresh context
        using var testContext = _fixture.CreateDbContext();
        await using var testUnitOfWork = new UnitOfWorkEF(testContext, _serviceProvider);

        // Act - Retrieve and modify using with-expression pattern
        var trackedOriginal = await testContext.ImmutableAggregates.FindAsync(id);
        trackedOriginal.ShouldNotBeNull();

        var modified = new TestImmutableAggregate(id) { Name = "Modified", Amount = 200m };

        var result = testUnitOfWork.UpdateImmutable(modified);
        result.IsRight.ShouldBeTrue();

        var saveResult = await testUnitOfWork.SaveChangesAsync();
        saveResult.IsRight.ShouldBeTrue();

        // Assert - Verify with fresh context
        using var verifyContext = _fixture.CreateDbContext();
        var persisted = await verifyContext.ImmutableAggregates.FindAsync(id);
        persisted.ShouldNotBeNull();
        persisted!.Name.ShouldBe("Modified");
        persisted.Amount.ShouldBe(200m);
    }

    [Fact]
    public async Task UpdateImmutableAsync_ModifiesTrackedEntity()
    {
        // Arrange - Create initial aggregate
        var id = Guid.NewGuid();
        var original = new TestImmutableAggregate(id) { Name = "Original", Amount = 100m };
        _dbContext.ImmutableAggregates.Add(original);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Create UnitOfWork with fresh context
        using var testContext = _fixture.CreateDbContext();
        await using var testUnitOfWork = new UnitOfWorkEF(testContext, _serviceProvider);

        // Act - Retrieve and modify using with-expression pattern
        var trackedOriginal = await testContext.ImmutableAggregates.FindAsync(id);
        trackedOriginal.ShouldNotBeNull();

        var modified = new TestImmutableAggregate(id) { Name = "AsyncModified", Amount = 300m };

        var result = await testUnitOfWork.UpdateImmutableAsync(modified);
        result.IsRight.ShouldBeTrue();

        var saveResult = await testUnitOfWork.SaveChangesAsync();
        saveResult.IsRight.ShouldBeTrue();

        // Assert - Verify with fresh context
        using var verifyContext = _fixture.CreateDbContext();
        var persisted = await verifyContext.ImmutableAggregates.FindAsync(id);
        persisted.ShouldNotBeNull();
        persisted!.Name.ShouldBe("AsyncModified");
        persisted.Amount.ShouldBe(300m);
    }

    [Fact]
    public async Task UpdateImmutable_InTransaction_RollbackRevertsChanges()
    {
        // Arrange - Create initial aggregate
        var id = Guid.NewGuid();
        var original = new TestImmutableAggregate(id) { Name = "Original", Amount = 100m };
        _dbContext.ImmutableAggregates.Add(original);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Create UnitOfWork with fresh context
        using var testContext = _fixture.CreateDbContext();
        await using var testUnitOfWork = new UnitOfWorkEF(testContext, _serviceProvider);

        // Act - Start transaction, modify, rollback
        var beginResult = await testUnitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        var trackedOriginal = await testContext.ImmutableAggregates.FindAsync(id);
        trackedOriginal.ShouldNotBeNull();

        var modified = new TestImmutableAggregate(id) { Name = "ShouldNotPersist", Amount = 999m };

        var updateResult = await testUnitOfWork.UpdateImmutableAsync(modified);
        updateResult.IsRight.ShouldBeTrue();

        var saveResult = await testUnitOfWork.SaveChangesAsync();
        saveResult.IsRight.ShouldBeTrue();

        // Rollback instead of commit
        await testUnitOfWork.RollbackAsync();

        // Assert - Original values preserved
        using var verifyContext = _fixture.CreateDbContext();
        var persisted = await verifyContext.ImmutableAggregates.FindAsync(id);
        persisted.ShouldNotBeNull();
        persisted!.Name.ShouldBe("Original");
        persisted.Amount.ShouldBe(100m);
    }

    [Fact]
    public async Task UpdateImmutable_InTransaction_CommitPersistsChanges()
    {
        // Arrange - Create initial aggregate
        var id = Guid.NewGuid();
        var original = new TestImmutableAggregate(id) { Name = "Original", Amount = 100m };
        _dbContext.ImmutableAggregates.Add(original);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Create UnitOfWork with fresh context
        using var testContext = _fixture.CreateDbContext();
        await using var testUnitOfWork = new UnitOfWorkEF(testContext, _serviceProvider);

        // Act - Start transaction, modify, commit
        var beginResult = await testUnitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        var trackedOriginal = await testContext.ImmutableAggregates.FindAsync(id);
        trackedOriginal.ShouldNotBeNull();

        var modified = new TestImmutableAggregate(id) { Name = "Committed", Amount = 500m };

        var updateResult = await testUnitOfWork.UpdateImmutableAsync(modified);
        updateResult.IsRight.ShouldBeTrue();

        var saveResult = await testUnitOfWork.SaveChangesAsync();
        saveResult.IsRight.ShouldBeTrue();

        var commitResult = await testUnitOfWork.CommitAsync();
        commitResult.IsRight.ShouldBeTrue();

        // Assert - Changes persisted
        using var verifyContext = _fixture.CreateDbContext();
        var persisted = await verifyContext.ImmutableAggregates.FindAsync(id);
        persisted.ShouldNotBeNull();
        persisted!.Name.ShouldBe("Committed");
        persisted.Amount.ShouldBe(500m);
    }

    #endregion

    #region Helper Methods

    private static TestEntity CreateTestEntity(
        string name = "Test Entity",
        bool isActive = true,
        decimal amount = 100m)
    {
        return new TestEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Amount = amount,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    #endregion
}
