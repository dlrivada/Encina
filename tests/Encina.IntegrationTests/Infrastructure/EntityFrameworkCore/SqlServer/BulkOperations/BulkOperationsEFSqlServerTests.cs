using System.Globalization;
using Encina.DomainModeling;
using Encina.EntityFrameworkCore.BulkOperations;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.SqlServer.BulkOperations;

/// <summary>
/// SQL Server-specific integration tests for <see cref="BulkOperationsEFSqlServer{TEntity}"/>.
/// Tests verify actual bulk operations against a real SQL Server database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Trait("Feature", "BulkOperations")]
[Collection("EFCore-SqlServer")]
public sealed class BulkOperationsEFSqlServerTests : IAsyncLifetime
{
    private readonly EFCoreSqlServerFixture _fixture;
    private BulkOperationsEF<TestRepositoryEntity> _bulkOps = null!;
    private TestEFDbContext _dbContext = null!;

    public BulkOperationsEFSqlServerTests(EFCoreSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext<TestEFDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();
        _bulkOps = new BulkOperationsEF<TestRepositoryEntity>(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _fixture.ClearAllDataAsync();
    }

    private async Task<int> GetRowCountAsync()
    {
        return await _dbContext.TestRepositoryEntities.CountAsync();
    }

    private static List<TestRepositoryEntity> CreateEntities(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new TestRepositoryEntity
            {
                Id = Guid.NewGuid(),
                Name = $"Entity_{i}",
                Amount = (i + 1) * 10.50m,
                IsActive = i % 2 == 0,
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();
    }

    #region BulkInsertAsync Tests

    [Fact]
    public async Task BulkInsertAsync_EmptyCollection_ReturnsRightWithZero()
    {
        // Arrange
        var entities = new List<TestRepositoryEntity>();

        // Act
        var result = await _bulkOps.BulkInsertAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    [Fact]
    public async Task BulkInsertAsync_SingleEntity_InsertsSuccessfully()
    {
        // Arrange
        var entity = new TestRepositoryEntity
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
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(1));

        var rowCount = await GetRowCountAsync();
        rowCount.ShouldBe(1);
    }

    [Fact]
    public async Task BulkInsertAsync_100Entities_InsertsAllSuccessfully()
    {
        // Arrange
        var entities = CreateEntities(100);

        // Act
        var result = await _bulkOps.BulkInsertAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(100));

        var rowCount = await GetRowCountAsync();
        rowCount.ShouldBe(100);
    }

    [Fact]
    public async Task BulkInsertAsync_WithCustomBatchSize_InsertsSuccessfully()
    {
        // Arrange
        var entities = CreateEntities(250);
        var config = BulkConfig.Default with { BatchSize = 50 };

        // Act
        var result = await _bulkOps.BulkInsertAsync(entities, config);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(250));
    }

    #endregion

    #region BulkReadAsync Tests

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

    [Fact]
    public async Task BulkReadAsync_ExistingIds_ReturnsEntities()
    {
        // Arrange
        var entities = CreateEntities(10);
        await _bulkOps.BulkInsertAsync(entities);

        var idsToRead = entities.Take(5).Select(e => (object)e.Id).ToList();

        // Act
        var result = await _bulkOps.BulkReadAsync(idsToRead);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(readEntities =>
        {
            readEntities.Count.ShouldBe(5);
            readEntities.All(e => entities.Any(o => o.Id == e.Id)).ShouldBeTrue();
        });
    }

    [Fact]
    public async Task BulkReadAsync_MixedExistingAndNonExistingIds_ReturnsOnlyExisting()
    {
        // Arrange
        var entities = CreateEntities(5);
        await _bulkOps.BulkInsertAsync(entities);

        var idsToRead = entities.Take(3).Select(e => (object)e.Id).ToList();
        idsToRead.Add(Guid.NewGuid()); // Non-existing ID
        idsToRead.Add(Guid.NewGuid()); // Another non-existing ID

        // Act
        var result = await _bulkOps.BulkReadAsync(idsToRead);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(readEntities => readEntities.Count.ShouldBe(3));
    }

    #endregion

    #region BulkUpdateAsync Tests

    [Fact]
    public async Task BulkUpdateAsync_EmptyCollection_ReturnsRightWithZero()
    {
        // Arrange
        var entities = new List<TestRepositoryEntity>();

        // Act
        var result = await _bulkOps.BulkUpdateAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    [Fact]
    public async Task BulkUpdateAsync_ExistingEntities_UpdatesSuccessfully()
    {
        // Arrange
        var entities = CreateEntities(10);
        await _bulkOps.BulkInsertAsync(entities);

        // Modify entities
        foreach (var entity in entities)
        {
            entity.Name = $"Updated_{entity.Name}";
            entity.Amount += 100;
        }

        // Act
        var result = await _bulkOps.BulkUpdateAsync(entities);

        // Assert
        var updateCount = result.Match(
            Right: count => count,
            Left: error => throw new InvalidOperationException($"BulkUpdate failed: {error.Message}")
        );
        updateCount.ShouldBe(10);

        // Verify updates persisted
        var readResult = await _bulkOps.BulkReadAsync(entities.Select(e => (object)e.Id).ToList());
        readResult.IfRight(readEntities =>
        {
            readEntities.All(e => e.Name.StartsWith("Updated_", StringComparison.Ordinal)).ShouldBeTrue();
        });
    }

    #endregion

    #region BulkDeleteAsync Tests

    [Fact]
    public async Task BulkDeleteAsync_EmptyCollection_ReturnsRightWithZero()
    {
        // Arrange
        var entities = new List<TestRepositoryEntity>();

        // Act
        var result = await _bulkOps.BulkDeleteAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    [Fact]
    public async Task BulkDeleteAsync_ExistingEntities_DeletesSuccessfully()
    {
        // Arrange
        var entities = CreateEntities(20);
        await _bulkOps.BulkInsertAsync(entities);

        var entitiesToDelete = entities.Take(10).ToList();

        // Act
        var result = await _bulkOps.BulkDeleteAsync(entitiesToDelete);

        // Assert
        var deleteCount = result.Match(
            Right: count => count,
            Left: error => throw new InvalidOperationException($"BulkDelete failed: {error.Message}")
        );
        deleteCount.ShouldBe(10);

        var rowCount = await GetRowCountAsync();
        rowCount.ShouldBe(10);
    }

    #endregion

    #region BulkMergeAsync Tests

    [Fact]
    public async Task BulkMergeAsync_EmptyCollection_ReturnsRightWithZero()
    {
        // Arrange
        var entities = new List<TestRepositoryEntity>();

        // Act
        var result = await _bulkOps.BulkMergeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

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

        var rowCount = await GetRowCountAsync();
        rowCount.ShouldBe(10);
    }

    [Fact]
    public async Task BulkMergeAsync_MixedNewAndExisting_InsertsAndUpdates()
    {
        // Arrange
        var existingEntities = CreateEntities(5);
        await _bulkOps.BulkInsertAsync(existingEntities);

        // Modify existing entities
        foreach (var entity in existingEntities)
        {
            entity.Name = $"Merged_{entity.Name}";
        }

        // Add new entities
        var newEntities = CreateEntities(5);
        var allEntities = existingEntities.Concat(newEntities).ToList();

        // Act
        var result = await _bulkOps.BulkMergeAsync(allEntities);

        // Assert
        var mergeCount = result.Match(
            Right: count => count,
            Left: error => throw new InvalidOperationException($"BulkMerge failed: {error.Message}")
        );
        mergeCount.ShouldBe(10); // 5 updates + 5 inserts

        var rowCount = await GetRowCountAsync();
        rowCount.ShouldBe(10);
    }

    #endregion
}
