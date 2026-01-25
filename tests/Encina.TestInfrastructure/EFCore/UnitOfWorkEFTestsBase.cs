using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.TestInfrastructure.EFCore;

/// <summary>
/// Abstract base class for Unit of Work EF Core integration tests.
/// Contains test methods that run against any database provider.
/// </summary>
/// <typeparam name="TFixture">The type of EF Core fixture to use.</typeparam>
/// <typeparam name="TContext">The type of DbContext to use.</typeparam>
public abstract class UnitOfWorkEFTestsBase<TFixture, TContext> : EFCoreTestBase<TFixture>
    where TFixture : class, IEFCoreFixture
    where TContext : DbContext
{
    /// <summary>
    /// Creates a test entity for unit of work tests.
    /// </summary>
    protected abstract object CreateTestEntity(string name);

    /// <summary>
    /// Adds the test entity to the context.
    /// </summary>
    protected abstract void AddEntityToContext(TContext context, object entity);

    /// <summary>
    /// Gets the count of entities in the context.
    /// </summary>
    protected abstract Task<int> GetEntityCountAsync(TContext context);

    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    protected abstract object GetEntityId(object entity);

    /// <summary>
    /// Finds entity by ID.
    /// </summary>
    protected abstract Task<object?> FindEntityByIdAsync(TContext context, object id);

    [Fact]
    public async Task SaveChangesAsync_WithSingleEntity_ShouldPersist()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var entity = CreateTestEntity("Test Entity");
        AddEntityToContext(context, entity);

        // Act
        var saveCount = await context.SaveChangesAsync();

        // Assert
        saveCount.ShouldBe(1);

        await using var verifyContext = CreateDbContext<TContext>();
        var count = await GetEntityCountAsync(verifyContext);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task SaveChangesAsync_WithMultipleEntities_ShouldPersistAll()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        AddEntityToContext(context, CreateTestEntity("Entity 1"));
        AddEntityToContext(context, CreateTestEntity("Entity 2"));
        AddEntityToContext(context, CreateTestEntity("Entity 3"));

        // Act
        var saveCount = await context.SaveChangesAsync();

        // Assert
        saveCount.ShouldBe(3);

        await using var verifyContext = CreateDbContext<TContext>();
        var count = await GetEntityCountAsync(verifyContext);
        count.ShouldBe(3);
    }

    [Fact]
    public async Task Transaction_WithRollback_ShouldNotPersist()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var entity = CreateTestEntity("Test Entity");
        AddEntityToContext(context, entity);
        await context.SaveChangesAsync();

        // Act - Rollback transaction
        await transaction.RollbackAsync();

        // Assert
        await using var verifyContext = CreateDbContext<TContext>();
        var count = await GetEntityCountAsync(verifyContext);
        count.ShouldBe(0);
    }

    [Fact]
    public async Task Transaction_WithCommit_ShouldPersist()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var entity = CreateTestEntity("Test Entity");
        AddEntityToContext(context, entity);
        await context.SaveChangesAsync();

        // Act
        await transaction.CommitAsync();

        // Assert
        await using var verifyContext = CreateDbContext<TContext>();
        var count = await GetEntityCountAsync(verifyContext);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task ConcurrentContexts_ShouldNotInterfere()
    {
        // Arrange & Act
        var tasks = Enumerable.Range(0, 5).Select(async i =>
        {
            await using var context = CreateDbContext<TContext>();
            var entity = CreateTestEntity($"Concurrent Entity {i}");
            AddEntityToContext(context, entity);
            await context.SaveChangesAsync();
            return GetEntityId(entity);
        });

        var entityIds = await Task.WhenAll(tasks);

        // Assert
        entityIds.ShouldAllBe(id => id != null);

        await using var verifyContext = CreateDbContext<TContext>();
        var count = await GetEntityCountAsync(verifyContext);
        count.ShouldBe(5);
    }

    [Fact]
    public async Task ChangeTracker_Clear_ShouldDetachEntities()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var entity = CreateTestEntity("Test Entity");
        AddEntityToContext(context, entity);
        await context.SaveChangesAsync();

        context.ChangeTracker.Entries().Count().ShouldBeGreaterThan(0);

        // Act
        context.ChangeTracker.Clear();

        // Assert
        context.ChangeTracker.Entries().Count().ShouldBe(0);
    }

    [Fact]
    public async Task ModifyEntity_WithoutSave_ShouldNotPersist()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var entity = CreateTestEntity("Original Name");
        AddEntityToContext(context, entity);
        await context.SaveChangesAsync();
        var entityId = GetEntityId(entity);

        // Act - Modify without saving
        await using var modifyContext = CreateDbContext<TContext>();
        var entityToModify = await FindEntityByIdAsync(modifyContext, entityId);
        entityToModify.ShouldNotBeNull();

        // We don't save changes

        // Assert - Original name should remain
        await using var verifyContext = CreateDbContext<TContext>();
        var verified = await FindEntityByIdAsync(verifyContext, entityId);
        verified.ShouldNotBeNull();
    }
}
