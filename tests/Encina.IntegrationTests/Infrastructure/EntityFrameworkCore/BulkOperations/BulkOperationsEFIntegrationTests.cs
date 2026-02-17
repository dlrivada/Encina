using Encina.DomainModeling;
using Encina.EntityFrameworkCore.BulkOperations;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Integration tests for <see cref="BulkOperationsEF{TEntity}"/> using SQLite in-memory database.
/// Tests verify the Railway Oriented Programming patterns and correct error handling.
/// </summary>
/// <remarks>
/// <para>
/// The EF Core bulk operations implementation auto-detects the database provider and selects
/// the appropriate implementation (SQLite, SQL Server, PostgreSQL, MySQL, Oracle).
/// </para>
/// <para>
/// These tests verify:
/// <list type="bullet">
/// <item><description>Empty collections return <c>Right(0)</c> for all operations</description></item>
/// <item><description>Basic operations work with SQLite implementation</description></item>
/// <item><description>Error messages include meaningful context (operation type, reason)</description></item>
/// </list>
/// </para>
/// <para>
/// For provider-specific integration tests, see the tests in the provider subfolders
/// (Sqlite, SqlServer, PostgreSQL, MySQL, Oracle).
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Provider", "EntityFrameworkCore")]
#pragma warning disable CA1001 // IAsyncLifetime handles disposal via DisposeAsync
public sealed class BulkOperationsEFIntegrationTests : IAsyncLifetime
#pragma warning restore CA1001
{
    private BulkTestDbContext _dbContext = null!;
    private BulkOperationsEF<BulkTestEntity> _bulkOps = null!;

    public ValueTask InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<BulkTestDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _dbContext = new BulkTestDbContext(options);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();

        _bulkOps = new BulkOperationsEF<BulkTestEntity>(_dbContext);

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _dbContext?.Dispose();
        return ValueTask.CompletedTask;
    }

    #region BulkInsertAsync Tests - Empty Collection

    [Fact]
    public async Task BulkInsertAsync_EmptyCollection_ReturnsRightWithZero()
    {
        // Arrange
        var entities = new List<BulkTestEntity>();

        // Act
        var result = await _bulkOps.BulkInsertAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    #endregion

    #region BulkInsertAsync Tests - SQLite Support

    [Fact]
    public async Task BulkInsertAsync_SingleEntity_InsertsSuccessfully()
    {
        // Arrange
        var entity = new BulkTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Entity",
            Amount = 99.99m,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        var result = await _bulkOps.BulkInsertAsync([entity]);

        // Assert
        var count = result.Match(
            Right: c => c,
            Left: error => throw new Xunit.Sdk.XunitException($"BulkInsert failed: {error.Message}")
        );
        count.ShouldBe(1);
    }

    [Fact]
    public async Task BulkInsertAsync_MultipleEntities_InsertsSuccessfully()
    {
        // Arrange
        var entities = CreateEntities(10);

        // Act
        var result = await _bulkOps.BulkInsertAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(10));
    }

    [Fact]
    public async Task BulkInsertAsync_WithConfig_InsertsSuccessfully()
    {
        // Arrange
        var entities = CreateEntities(100);
        var config = BulkConfig.Default with { BatchSize = 50 };

        // Act
        var result = await _bulkOps.BulkInsertAsync(entities, config);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(100));
    }

    #endregion

    #region BulkUpdateAsync Tests - Empty Collection

    [Fact]
    public async Task BulkUpdateAsync_EmptyCollection_ReturnsRightWithZero()
    {
        // Arrange
        var entities = new List<BulkTestEntity>();

        // Act
        var result = await _bulkOps.BulkUpdateAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    #endregion

    #region BulkUpdateAsync Tests - SQLite Support

    [Fact]
    public async Task BulkUpdateAsync_ExistingEntities_UpdatesSuccessfully()
    {
        // Arrange
        var entities = CreateEntities(5);
        await _bulkOps.BulkInsertAsync(entities);

        foreach (var entity in entities)
        {
            entity.Name = $"Updated_{entity.Name}";
        }

        // Act
        var result = await _bulkOps.BulkUpdateAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(5));
    }

    #endregion

    #region BulkDeleteAsync Tests - Empty Collection

    [Fact]
    public async Task BulkDeleteAsync_EmptyCollection_ReturnsRightWithZero()
    {
        // Arrange
        var entities = new List<BulkTestEntity>();

        // Act
        var result = await _bulkOps.BulkDeleteAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    #endregion

    #region BulkDeleteAsync Tests - SQLite Support

    [Fact]
    public async Task BulkDeleteAsync_ExistingEntities_DeletesSuccessfully()
    {
        // Arrange
        var entities = CreateEntities(10);
        await _bulkOps.BulkInsertAsync(entities);

        var entitiesToDelete = entities.Take(5).ToList();

        // Act
        var result = await _bulkOps.BulkDeleteAsync(entitiesToDelete);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(5));
    }

    #endregion

    #region BulkMergeAsync Tests - Empty Collection

    [Fact]
    public async Task BulkMergeAsync_EmptyCollection_ReturnsRightWithZero()
    {
        // Arrange
        var entities = new List<BulkTestEntity>();

        // Act
        var result = await _bulkOps.BulkMergeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    #endregion

    #region BulkMergeAsync Tests - SQLite Support

    [Fact]
    public async Task BulkMergeAsync_NewEntities_InsertsSuccessfully()
    {
        // Arrange
        var entities = CreateEntities(10);

        // Act
        var result = await _bulkOps.BulkMergeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(10));
    }

    #endregion

    #region BulkReadAsync Tests - Empty IDs

    [Fact]
    public async Task BulkReadAsync_EmptyIds_ReturnsEmptyCollection()
    {
        // Arrange
        var ids = new List<object>();

        // Act
        var result = await _bulkOps.BulkReadAsync(ids);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entities => entities.ShouldBeEmpty());
    }

    #endregion

    #region BulkReadAsync Tests - SQLite Support

    [Fact]
    public async Task BulkReadAsync_ExistingIds_ReturnsEntities()
    {
        // Arrange
        var entities = CreateEntities(10);
        await _bulkOps.BulkInsertAsync(entities);

        var ids = entities.Take(5).Select(e => (object)e.Id).ToList();

        // Act
        var result = await _bulkOps.BulkReadAsync(ids);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(readEntities => readEntities.Count.ShouldBe(5));
    }

    #endregion

    #region Helper Methods

    private static List<BulkTestEntity> CreateEntities(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new BulkTestEntity
            {
                Id = Guid.NewGuid(),
                Name = $"Entity_{i}",
                Amount = i * 10.5m,
                IsActive = i % 2 == 0,
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();
    }

    #endregion
}

#region Test Entity and DbContext

/// <summary>
/// Test entity for bulk operations integration tests.
/// </summary>
public class BulkTestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// DbContext for bulk operations integration tests.
/// </summary>
public class BulkTestDbContext(DbContextOptions<BulkTestDbContext> options) : DbContext(options)
{
    public DbSet<BulkTestEntity> BulkTestEntities => Set<BulkTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BulkTestEntity>(entity =>
        {
            entity.ToTable("BulkTestEntities");
            entity.HasKey(e => e.Id);
            // Prevent EF Core from auto-generating the Id - we provide it ourselves
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
        });
    }
}

#endregion
