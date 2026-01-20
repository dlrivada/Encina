using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Repository;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Repository;

/// <summary>
/// Unit tests for <see cref="FunctionalRepositoryEF{TEntity, TId}"/>.
/// Uses EF Core InMemory database for isolation.
/// </summary>
[Trait("Category", "Unit")]
public class FunctionalRepositoryEFTests : IDisposable
{
    private readonly RepositoryTestDbContext _dbContext;
    private readonly FunctionalRepositoryEF<TestEntity, Guid> _repository;

    public FunctionalRepositoryEFTests()
    {
        var options = new DbContextOptionsBuilder<RepositoryTestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new RepositoryTestDbContext(options);
        _repository = new FunctionalRepositoryEF<TestEntity, Guid>(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsRight()
    {
        // Arrange
        var entity = CreateTestEntity();
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(entity.Id);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e =>
        {
            e.Id.ShouldBe(entity.Id);
            e.Name.ShouldBe(entity.Name);
        });
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingEntity_ReturnsLeftWithNotFoundError()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistingId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("not found");
            error.Message.ShouldContain(nonExistingId.ToString());
        });
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_EmptyTable_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.ListAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entities => entities.ShouldBeEmpty());
    }

    [Fact]
    public async Task ListAsync_WithEntities_ReturnsAllEntities()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Entity 1"),
            CreateTestEntity("Entity 2"),
            CreateTestEntity("Entity 3")
        };
        _dbContext.TestEntities.AddRange(entities);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.ListAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Count.ShouldBe(3));
    }

    [Fact]
    public async Task ListAsync_WithSpecification_ReturnsFilteredEntities()
    {
        // Arrange
        var activeEntity = CreateTestEntity("Active", isActive: true);
        var inactiveEntity = CreateTestEntity("Inactive", isActive: false);
        _dbContext.TestEntities.AddRange(activeEntity, inactiveEntity);
        await _dbContext.SaveChangesAsync();

        var spec = new ActiveEntitySpec();

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(1);
            list[0].Name.ShouldBe("Active");
        });
    }

    [Fact]
    public async Task ListAsync_WithCombinedSpecification_ReturnsFilteredEntities()
    {
        // Arrange
        _dbContext.TestEntities.AddRange(
            CreateTestEntity("High Active", isActive: true, amount: 200),
            CreateTestEntity("Low Active", isActive: true, amount: 50),
            CreateTestEntity("High Inactive", isActive: false, amount: 300)
        );
        await _dbContext.SaveChangesAsync();

        var spec = new ActiveEntitySpec().And(new MinAmountSpec(100));

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(1);
            list[0].Name.ShouldBe("High Active");
        });
    }

    #endregion

    #region FirstOrDefaultAsync Tests

    [Fact]
    public async Task FirstOrDefaultAsync_MatchingEntity_ReturnsRight()
    {
        // Arrange
        var entity = CreateTestEntity("Test", isActive: true);
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();

        var spec = new ActiveEntitySpec();

        // Act
        var result = await _repository.FirstOrDefaultAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e => e.Name.ShouldBe("Test"));
    }

    [Fact]
    public async Task FirstOrDefaultAsync_NoMatch_ReturnsLeftWithNotFoundError()
    {
        // Arrange
        var entity = CreateTestEntity("Inactive", isActive: false);
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();

        var spec = new ActiveEntitySpec();

        // Act
        var result = await _repository.FirstOrDefaultAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_EmptyTable_ReturnsZero()
    {
        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    [Fact]
    public async Task CountAsync_WithEntities_ReturnsCorrectCount()
    {
        // Arrange
        _dbContext.TestEntities.AddRange(
            CreateTestEntity("Entity 1"),
            CreateTestEntity("Entity 2"),
            CreateTestEntity("Entity 3")
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(3));
    }

    [Fact]
    public async Task CountAsync_WithSpecification_ReturnsFilteredCount()
    {
        // Arrange
        _dbContext.TestEntities.AddRange(
            CreateTestEntity("Active 1", isActive: true),
            CreateTestEntity("Active 2", isActive: true),
            CreateTestEntity("Inactive", isActive: false)
        );
        await _dbContext.SaveChangesAsync();

        var spec = new ActiveEntitySpec();

        // Act
        var result = await _repository.CountAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(2));
    }

    #endregion

    #region AnyAsync Tests

    [Fact]
    public async Task AnyAsync_EmptyTable_ReturnsFalse()
    {
        // Act
        var result = await _repository.AnyAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeFalse());
    }

    [Fact]
    public async Task AnyAsync_WithEntities_ReturnsTrue()
    {
        // Arrange
        _dbContext.TestEntities.Add(CreateTestEntity());
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.AnyAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeTrue());
    }

    [Fact]
    public async Task AnyAsync_WithSpecification_NoMatch_ReturnsFalse()
    {
        // Arrange
        _dbContext.TestEntities.Add(CreateTestEntity("Inactive", isActive: false));
        await _dbContext.SaveChangesAsync();

        var spec = new ActiveEntitySpec();

        // Act
        var result = await _repository.AnyAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeFalse());
    }

    [Fact]
    public async Task AnyAsync_WithSpecification_HasMatch_ReturnsTrue()
    {
        // Arrange
        _dbContext.TestEntities.Add(CreateTestEntity("Active", isActive: true));
        await _dbContext.SaveChangesAsync();

        var spec = new ActiveEntitySpec();

        // Act
        var result = await _repository.AnyAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeTrue());
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ValidEntity_ReturnsRightAndPersists()
    {
        // Arrange
        var entity = CreateTestEntity("New Entity");

        // Act
        var result = await _repository.AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e => e.Name.ShouldBe("New Entity"));

        var stored = await _dbContext.TestEntities.FindAsync(entity.Id);
        stored.ShouldNotBeNull();
        stored!.Name.ShouldBe("New Entity");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_ReturnsRightAndUpdates()
    {
        // Arrange
        var entity = CreateTestEntity("Original");
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Detach and modify
        var updatedEntity = new TestEntity
        {
            Id = entity.Id,
            Name = "Updated",
            Amount = entity.Amount,
            IsActive = entity.IsActive,
            CreatedAtUtc = entity.CreatedAtUtc
        };

        // Act
        var result = await _repository.UpdateAsync(updatedEntity);
        await _dbContext.SaveChangesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _dbContext.TestEntities.FindAsync(entity.Id);
        stored.ShouldNotBeNull();
        stored!.Name.ShouldBe("Updated");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ById_ExistingEntity_ReturnsRightAndRemoves()
    {
        // Arrange
        var entity = CreateTestEntity();
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteAsync(entity.Id);
        await _dbContext.SaveChangesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _dbContext.TestEntities.FindAsync(entity.Id);
        stored.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ById_NonExistingEntity_ReturnsLeftWithNotFoundError()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.DeleteAsync(nonExistingId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    [Fact]
    public async Task DeleteAsync_ByEntity_ExistingEntity_ReturnsRightAndRemoves()
    {
        // Arrange
        var entity = CreateTestEntity();
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteAsync(entity);
        await _dbContext.SaveChangesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();

        _dbContext.ChangeTracker.Clear();
        var stored = await _dbContext.TestEntities.FindAsync(entity.Id);
        stored.ShouldBeNull();
    }

    #endregion

    #region AddRangeAsync Tests

    [Fact]
    public async Task AddRangeAsync_ValidEntities_ReturnsRightAndPersists()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Entity 1"),
            CreateTestEntity("Entity 2"),
            CreateTestEntity("Entity 3")
        };

        // Act
        var result = await _repository.AddRangeAsync(entities);
        await _dbContext.SaveChangesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Count.ShouldBe(3));

        var storedCount = await _dbContext.TestEntities.CountAsync();
        storedCount.ShouldBe(3);
    }

    #endregion

    #region UpdateRangeAsync Tests

    [Fact]
    public async Task UpdateRangeAsync_ExistingEntities_ReturnsRightAndUpdates()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Entity 1"),
            CreateTestEntity("Entity 2")
        };
        _dbContext.TestEntities.AddRange(entities);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Modify
        foreach (var entity in entities)
        {
            entity.Name = entity.Name + " Updated";
        }

        // Act
        var result = await _repository.UpdateRangeAsync(entities);
        await _dbContext.SaveChangesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _dbContext.TestEntities.ToListAsync();
        stored.ShouldAllBe(e => e.Name.EndsWith(" Updated"));
    }

    #endregion

    #region DeleteRangeAsync Tests

    [Fact]
    public async Task DeleteRangeAsync_WithSpecification_ReturnsLeftOnInMemoryProvider()
    {
        // Note: ExecuteDeleteAsync is not supported by EF Core InMemory provider.
        // In a real database, this would delete matching entities.
        // For InMemory, we expect it to return a Left with a persistence error.

        // Arrange
        _dbContext.TestEntities.AddRange(
            CreateTestEntity("Active 1", isActive: true),
            CreateTestEntity("Active 2", isActive: true),
            CreateTestEntity("Inactive", isActive: false)
        );
        await _dbContext.SaveChangesAsync();

        var spec = new ActiveEntitySpec();

        // Act
        var result = await _repository.DeleteRangeAsync(spec);

        // Assert - InMemory provider doesn't support ExecuteDeleteAsync, returns error
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("DeleteRange"));
    }

    #endregion

    #region Constructor Validation Tests

    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new FunctionalRepositoryEF<TestEntity, Guid>(null!));
    }

    #endregion

    #region ArgumentNullException Tests for Methods

    [Fact]
    public async Task ListAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _repository.ListAsync(null!));
    }

    [Fact]
    public async Task FirstOrDefaultAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _repository.FirstOrDefaultAsync(null!));
    }

    [Fact]
    public async Task CountAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _repository.CountAsync(null!));
    }

    [Fact]
    public async Task AnyAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _repository.AnyAsync(null!));
    }

    [Fact]
    public async Task AddAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _repository.AddAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _repository.UpdateAsync(null!));
    }

    [Fact]
    public async Task DeleteAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _repository.DeleteAsync((TestEntity)null!));
    }

    [Fact]
    public async Task AddRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _repository.AddRangeAsync(null!));
    }

    [Fact]
    public async Task UpdateRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _repository.UpdateRangeAsync(null!));
    }

    [Fact]
    public async Task DeleteRangeAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _repository.DeleteRangeAsync(null!));
    }

    #endregion

    #region IHasId Support Tests

    [Fact]
    public async Task AddAsync_EntityWithIHasId_ReturnsEntityWithId()
    {
        // Arrange - Use a special DbContext with IHasId entity
        var options = new DbContextOptionsBuilder<HasIdTestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new HasIdTestDbContext(options);
        var repository = new FunctionalRepositoryEF<TestEntityWithHasId, Guid>(dbContext);
        var entity = new TestEntityWithHasId
        {
            Id = Guid.NewGuid(),
            Name = "Test with IHasId"
        };

        // Act
        var result = await repository.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e =>
        {
            e.Id.ShouldBe(entity.Id);
            e.Name.ShouldBe("Test with IHasId");
        });
    }

    #endregion

    // Note: Cancellation token tests are not included because EF Core InMemory provider
    // does not respect cancellation tokens. These scenarios are tested in integration tests
    // with a real database provider.

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
