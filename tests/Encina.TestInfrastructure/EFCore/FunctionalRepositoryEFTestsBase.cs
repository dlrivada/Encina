using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Repository;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.TestInfrastructure.EFCore;

/// <summary>
/// Abstract base class for FunctionalRepositoryEF integration tests.
/// Contains test methods that run against any database provider.
/// </summary>
/// <typeparam name="TFixture">The type of EF Core fixture to use.</typeparam>
/// <typeparam name="TContext">The type of DbContext to use.</typeparam>
/// <typeparam name="TEntity">The entity type being tested.</typeparam>
/// <typeparam name="TId">The ID type of the entity.</typeparam>
public abstract class FunctionalRepositoryEFTestsBase<TFixture, TContext, TEntity, TId> : EFCoreTestBase<TFixture>
    where TFixture : class, IEFCoreFixture
    where TContext : DbContext
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    /// <summary>
    /// Gets the DbSet for the entity from the context.
    /// </summary>
    protected abstract DbSet<TEntity> GetEntities(TContext context);

    /// <summary>
    /// Creates a new test entity with the specified name.
    /// </summary>
    protected abstract TEntity CreateTestEntity(string name = "Test Entity");

    /// <summary>
    /// Creates a new test entity with specified name and active status.
    /// </summary>
    protected abstract TEntity CreateTestEntity(string name, bool isActive);

    /// <summary>
    /// Creates a new test entity with specified name, active status, and amount.
    /// </summary>
    protected abstract TEntity CreateTestEntity(string name, bool isActive, decimal amount);

    /// <summary>
    /// Creates a specification that filters for active entities.
    /// </summary>
    protected abstract Specification<TEntity> CreateActiveEntitySpec();

    /// <summary>
    /// Creates a specification that filters for entities with a minimum amount.
    /// </summary>
    protected abstract Specification<TEntity> CreateMinAmountSpec(decimal minAmount);

    /// <summary>
    /// Gets the name property value from the entity.
    /// </summary>
    protected abstract string GetEntityName(TEntity entity);

    /// <summary>
    /// Sets the name property value on the entity.
    /// </summary>
    protected abstract void SetEntityName(TEntity entity, string name);

    /// <summary>
    /// Creates a copy of the entity with a new name.
    /// </summary>
    protected abstract TEntity CreateEntityCopy(TEntity original, string newName);

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsRight()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var repository = new FunctionalRepositoryEF<TEntity, TId>(context);

        var entity = CreateTestEntity();
        GetEntities(context).Add(entity);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act
        var result = await repository.GetByIdAsync(entity.Id);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e =>
        {
            e.Id.ShouldBe(entity.Id);
            GetEntityName(e).ShouldBe(GetEntityName(entity));
        });
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingEntity_ReturnsLeftWithNotFoundError()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var repository = new FunctionalRepositoryEF<TEntity, TId>(context);

        var nonExistingId = CreateNonExistingId();

        // Act
        var result = await repository.GetByIdAsync(nonExistingId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("not found");
        });
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_EmptyTable_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var repository = new FunctionalRepositoryEF<TEntity, TId>(context);

        // Act
        var result = await repository.ListAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entities => entities.ShouldBeEmpty());
    }

    [Fact]
    public async Task ListAsync_WithEntities_ReturnsAllEntities()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var repository = new FunctionalRepositoryEF<TEntity, TId>(context);

        var entities = new[]
        {
            CreateTestEntity("Entity 1"),
            CreateTestEntity("Entity 2"),
            CreateTestEntity("Entity 3")
        };
        GetEntities(context).AddRange(entities);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act
        var result = await repository.ListAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Count.ShouldBe(3));
    }

    [Fact]
    public async Task ListAsync_WithSpecification_ReturnsFilteredEntities()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var repository = new FunctionalRepositoryEF<TEntity, TId>(context);

        var activeEntity = CreateTestEntity("Active", isActive: true);
        var inactiveEntity = CreateTestEntity("Inactive", isActive: false);
        GetEntities(context).AddRange(activeEntity, inactiveEntity);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var spec = CreateActiveEntitySpec();

        // Act
        var result = await repository.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(1);
            GetEntityName(list[0]).ShouldBe("Active");
        });
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_EmptyTable_ReturnsZero()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var repository = new FunctionalRepositoryEF<TEntity, TId>(context);

        // Act
        var result = await repository.CountAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    [Fact]
    public async Task CountAsync_WithEntities_ReturnsCorrectCount()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var repository = new FunctionalRepositoryEF<TEntity, TId>(context);

        GetEntities(context).AddRange(
            CreateTestEntity("Entity 1"),
            CreateTestEntity("Entity 2"),
            CreateTestEntity("Entity 3")
        );
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act
        var result = await repository.CountAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(3));
    }

    #endregion

    #region AnyAsync Tests

    [Fact]
    public async Task AnyAsync_EmptyTable_ReturnsFalse()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var repository = new FunctionalRepositoryEF<TEntity, TId>(context);

        // Act
        var result = await repository.AnyAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeFalse());
    }

    [Fact]
    public async Task AnyAsync_WithEntities_ReturnsTrue()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var repository = new FunctionalRepositoryEF<TEntity, TId>(context);

        GetEntities(context).Add(CreateTestEntity());
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act
        var result = await repository.AnyAsync();

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
        await using var context = CreateDbContext<TContext>();
        var repository = new FunctionalRepositoryEF<TEntity, TId>(context);

        var entity = CreateTestEntity("New Entity");

        // Act
        var result = await repository.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e => GetEntityName(e).ShouldBe("New Entity"));

        // Verify persisted
        context.ChangeTracker.Clear();
        var stored = await GetEntities(context).FindAsync(entity.Id);
        stored.ShouldNotBeNull();
        GetEntityName(stored!).ShouldBe("New Entity");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_ReturnsRightAndUpdates()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var repository = new FunctionalRepositoryEF<TEntity, TId>(context);

        var entity = CreateTestEntity("Original");
        GetEntities(context).Add(entity);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Modify entity
        var updatedEntity = CreateEntityCopy(entity, "Updated");

        // Act
        var result = await repository.UpdateAsync(updatedEntity);

        // Assert
        result.IsRight.ShouldBeTrue();

        context.ChangeTracker.Clear();
        var stored = await GetEntities(context).FindAsync(entity.Id);
        stored.ShouldNotBeNull();
        GetEntityName(stored!).ShouldBe("Updated");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ById_ExistingEntity_ReturnsRightAndRemoves()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var repository = new FunctionalRepositoryEF<TEntity, TId>(context);

        var entity = CreateTestEntity();
        GetEntities(context).Add(entity);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act
        var result = await repository.DeleteAsync(entity.Id);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await GetEntities(context).FindAsync(entity.Id);
        stored.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ById_NonExistingEntity_ReturnsLeftWithNotFoundError()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var repository = new FunctionalRepositoryEF<TEntity, TId>(context);

        var nonExistingId = CreateNonExistingId();

        // Act
        var result = await repository.DeleteAsync(nonExistingId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    #endregion

    #region AddRangeAsync Tests

    [Fact]
    public async Task AddRangeAsync_ValidEntities_ReturnsRightAndPersists()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var repository = new FunctionalRepositoryEF<TEntity, TId>(context);

        var entities = new[]
        {
            CreateTestEntity("Entity 1"),
            CreateTestEntity("Entity 2"),
            CreateTestEntity("Entity 3")
        };

        // Act
        var result = await repository.AddRangeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Count.ShouldBe(3));

        context.ChangeTracker.Clear();
        var storedCount = await GetEntities(context).CountAsync();
        storedCount.ShouldBe(3);
    }

    #endregion

    /// <summary>
    /// Creates a non-existing ID for tests that need to test "not found" scenarios.
    /// </summary>
    protected abstract TId CreateNonExistingId();
}
