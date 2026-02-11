using Encina.DomainModeling;
using Encina.EntityFrameworkCore.SoftDelete;
using Microsoft.EntityFrameworkCore;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.SoftDelete;

/// <summary>
/// Integration tests for <see cref="SoftDeleteRepositoryEF{TEntity, TId}"/> using a real SQL Server database.
/// These tests validate that ExecuteDeleteAsync and IgnoreQueryFilters work correctly with soft delete operations.
/// </summary>
[Collection("EFCore SoftDelete")]
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public sealed class SoftDeleteRepositoryEFIntegrationTests : IAsyncLifetime
{
    private readonly SoftDeleteEFFixture _fixture = new();
    private SoftDeleteTestDbContext _context = null!;
    private SoftDeleteRepositoryEF<SoftDeleteTestEntity, Guid> _repository = null!;

    public async ValueTask InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _context = _fixture.CreateDbContext();
        _repository = new SoftDeleteRepositoryEF<SoftDeleteTestEntity, Guid>(_context);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _fixture.ClearAllDataAsync();
        await _fixture.DisposeAsync();
    }

    #region GetByIdWithDeletedAsync Tests

    [Fact]
    public async Task GetByIdWithDeletedAsync_WhenEntityExists_ShouldReturnEntity()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new SoftDeleteTestEntity
        {
            Id = entityId,
            Name = "Test Entity",
            Amount = 100m
        };
        _context.Set<SoftDeleteTestEntity>().Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdWithDeletedAsync(entityId);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: e =>
            {
                e.Id.ShouldBe(entityId);
                e.Name.ShouldBe("Test Entity");
            },
            Left: _ => Assert.Fail("Expected Right but got Left"));
    }

    [Fact]
    public async Task GetByIdWithDeletedAsync_WhenEntityIsSoftDeleted_ShouldReturnEntity()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new SoftDeleteTestEntity
        {
            Id = entityId,
            Name = "Deleted Entity",
            Amount = 200m,
            IsDeleted = true,
            DeletedAtUtc = DateTime.UtcNow,
            DeletedBy = "user-1"
        };
        _context.Set<SoftDeleteTestEntity>().Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdWithDeletedAsync(entityId);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: e =>
            {
                e.Id.ShouldBe(entityId);
                e.IsDeleted.ShouldBeTrue();
                e.DeletedAtUtc.ShouldNotBeNull();
            },
            Left: _ => Assert.Fail("Expected Right but got Left"));
    }

    [Fact]
    public async Task GetByIdWithDeletedAsync_WhenEntityDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdWithDeletedAsync(nonExistentId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => Assert.Fail("Expected Left but got Right"),
            Left: error =>
            {
                error.ErrorCode.ShouldBe("REPOSITORY_NOT_FOUND");
            });
    }

    #endregion

    #region ListWithDeletedAsync Tests

    [Fact]
    public async Task ListWithDeletedAsync_ShouldIncludeSoftDeletedEntities()
    {
        // Arrange
        _context.Set<SoftDeleteTestEntity>().Add(new SoftDeleteTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Active Entity",
            Amount = 100m,
            IsDeleted = false
        });
        _context.Set<SoftDeleteTestEntity>().Add(new SoftDeleteTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Deleted Entity",
            Amount = 200m,
            IsDeleted = true,
            DeletedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var specification = new AllSoftDeleteTestEntitiesSpecification();

        // Act
        var result = await _repository.ListWithDeletedAsync(specification);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: entities => entities.Count.ShouldBe(2),
            Left: _ => Assert.Fail("Expected Right but got Left"));
    }

    [Fact]
    public async Task ListWithDeletedAsync_QueryFiltersExcludeSoftDeletedByDefault()
    {
        // Arrange
        _context.Set<SoftDeleteTestEntity>().Add(new SoftDeleteTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Active Entity",
            Amount = 100m,
            IsDeleted = false
        });
        _context.Set<SoftDeleteTestEntity>().Add(new SoftDeleteTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Deleted Entity",
            Amount = 200m,
            IsDeleted = true,
            DeletedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act - Regular query without IgnoreQueryFilters
        var activeEntities = await _context.Set<SoftDeleteTestEntity>().ToListAsync();

        // Act - Query with IgnoreQueryFilters
        var allEntities = await _context.Set<SoftDeleteTestEntity>().IgnoreQueryFilters().ToListAsync();

        // Assert
        activeEntities.Count.ShouldBe(1);
        allEntities.Count.ShouldBe(2);
    }

    #endregion

    #region RestoreAsync Tests

    [Fact]
    public async Task RestoreAsync_WhenEntityIsSoftDeleted_ShouldRestoreEntity()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new SoftDeleteTestEntity
        {
            Id = entityId,
            Name = "Deleted Entity",
            Amount = 300m,
            IsDeleted = true,
            DeletedAtUtc = DateTime.UtcNow,
            DeletedBy = "user-1"
        };
        _context.Set<SoftDeleteTestEntity>().Add(entity);
        await _context.SaveChangesAsync();
        _context.Entry(entity).State = EntityState.Detached;

        // Act
        var result = await _repository.RestoreAsync(entityId);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: e =>
            {
                e.IsDeleted.ShouldBeFalse();
                e.DeletedAtUtc.ShouldBeNull();
                e.DeletedBy.ShouldBeNull();
            },
            Left: _ => Assert.Fail("Expected Right but got Left"));

        // Verify the entity is now visible in normal queries
        var restored = await _context.Set<SoftDeleteTestEntity>().FindAsync(entityId);
        restored.ShouldNotBeNull();
        restored.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task RestoreAsync_WhenEntityIsNotSoftDeleted_ShouldReturnInvalidOperationError()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new SoftDeleteTestEntity
        {
            Id = entityId,
            Name = "Active Entity",
            Amount = 400m,
            IsDeleted = false
        };
        _context.Set<SoftDeleteTestEntity>().Add(entity);
        await _context.SaveChangesAsync();
        _context.Entry(entity).State = EntityState.Detached;

        // Act
        var result = await _repository.RestoreAsync(entityId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => Assert.Fail("Expected Left but got Right"),
            Left: error =>
            {
                error.ErrorCode.ShouldBe("REPOSITORY_INVALID_OPERATION");
                error.Message.ShouldContain("is not soft-deleted");
            });
    }

    #endregion

    #region HardDeleteAsync Tests

    [Fact]
    public async Task HardDeleteAsync_WhenEntityExists_ShouldPermanentlyDeleteEntity()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new SoftDeleteTestEntity
        {
            Id = entityId,
            Name = "Entity to Hard Delete",
            Amount = 500m
        };
        _context.Set<SoftDeleteTestEntity>().Add(entity);
        await _context.SaveChangesAsync();
        _context.Entry(entity).State = EntityState.Detached;

        // Act
        var result = await _repository.HardDeleteAsync(entityId);

        // Assert
        result.IsRight.ShouldBeTrue();

        // Verify entity is completely removed even with IgnoreQueryFilters
        var deletedEntity = await _context.Set<SoftDeleteTestEntity>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == entityId);
        deletedEntity.ShouldBeNull();
    }

    [Fact]
    public async Task HardDeleteAsync_WhenEntityIsSoftDeleted_ShouldPermanentlyDeleteEntity()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new SoftDeleteTestEntity
        {
            Id = entityId,
            Name = "Soft Deleted Entity to Hard Delete",
            Amount = 600m,
            IsDeleted = true,
            DeletedAtUtc = DateTime.UtcNow
        };
        _context.Set<SoftDeleteTestEntity>().Add(entity);
        await _context.SaveChangesAsync();
        _context.Entry(entity).State = EntityState.Detached;

        // Act
        var result = await _repository.HardDeleteAsync(entityId);

        // Assert
        result.IsRight.ShouldBeTrue();

        // Verify entity is completely removed even with IgnoreQueryFilters
        var deletedEntity = await _context.Set<SoftDeleteTestEntity>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == entityId);
        deletedEntity.ShouldBeNull();
    }

    [Fact]
    public async Task HardDeleteAsync_WhenEntityDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.HardDeleteAsync(nonExistentId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => Assert.Fail("Expected Left but got Right"),
            Left: error =>
            {
                error.ErrorCode.ShouldBe("REPOSITORY_NOT_FOUND");
            });
    }

    #endregion
}

/// <summary>
/// Collection definition for soft delete EF Core integration tests.
/// </summary>
[CollectionDefinition("EFCore SoftDelete")]
public class SoftDeleteEFCoreTestFixtureDefinition : ICollectionFixture<SoftDeleteEFFixture>
{
}

/// <summary>
/// Specification to retrieve all soft delete test entities.
/// </summary>
public sealed class AllSoftDeleteTestEntitiesSpecification : Specification<SoftDeleteTestEntity>
{
    public override System.Linq.Expressions.Expression<Func<SoftDeleteTestEntity, bool>> ToExpression()
    {
        return e => true;
    }
}
