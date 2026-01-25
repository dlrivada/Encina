using System.Globalization;
using Encina.DomainModeling;
using Encina.EntityFrameworkCore.BulkOperations;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.MySQL.BulkOperations;

/// <summary>
/// MySQL-specific integration tests for <see cref="BulkOperationsEFMySql{TEntity}"/>.
/// Tests verify actual bulk operations against a real MySQL database via Testcontainers.
/// Tests are skipped until Pomelo.EntityFrameworkCore.MySql v10 is released.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Trait("Feature", "BulkOperations")]
[Collection("EFCore-MySQL")]
public sealed class BulkOperationsEFMySqlTests : IAsyncLifetime
{
    private readonly EFCoreMySqlFixture _fixture;
    private BulkOperationsEF<TestRepositoryEntity>? _bulkOps;
    private TestEFDbContext? _dbContext;

    public BulkOperationsEFMySqlTests(EFCoreMySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        // Skip initialization until MySQL EF Core support is available
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_dbContext is not null)
        {
            await _dbContext.DisposeAsync();
        }
        await _fixture.ClearAllDataAsync();
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

    [SkippableFact]
    public async Task BulkInsertAsync_EmptyCollection_ReturnsRightWithZero()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange
        _dbContext = _fixture.CreateDbContext<TestEFDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();
        _bulkOps = new BulkOperationsEF<TestRepositoryEntity>(_dbContext);

        var entities = new List<TestRepositoryEntity>();

        // Act
        var result = await _bulkOps.BulkInsertAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    [SkippableFact]
    public async Task BulkInsertAsync_SingleEntity_InsertsSuccessfully()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange
        _dbContext = _fixture.CreateDbContext<TestEFDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();
        _bulkOps = new BulkOperationsEF<TestRepositoryEntity>(_dbContext);

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
    }

    [SkippableFact]
    public async Task BulkInsertAsync_100Entities_InsertsAllSuccessfully()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange
        _dbContext = _fixture.CreateDbContext<TestEFDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();
        _bulkOps = new BulkOperationsEF<TestRepositoryEntity>(_dbContext);

        var entities = CreateEntities(100);

        // Act
        var result = await _bulkOps.BulkInsertAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(100));
    }

    #endregion

    #region BulkReadAsync Tests

    [SkippableFact]
    public async Task BulkReadAsync_EmptyIds_ReturnsEmptyCollection()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange
        _dbContext = _fixture.CreateDbContext<TestEFDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();
        _bulkOps = new BulkOperationsEF<TestRepositoryEntity>(_dbContext);

        var ids = new List<object>();

        // Act
        var result = await _bulkOps.BulkReadAsync(ids);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entities => entities.ShouldBeEmpty());
    }

    [SkippableFact]
    public async Task BulkReadAsync_ExistingIds_ReturnsEntities()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange
        _dbContext = _fixture.CreateDbContext<TestEFDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();
        _bulkOps = new BulkOperationsEF<TestRepositoryEntity>(_dbContext);

        var entities = CreateEntities(10);
        await _bulkOps.BulkInsertAsync(entities);

        var idsToRead = entities.Take(5).Select(e => (object)e.Id).ToList();

        // Act
        var result = await _bulkOps.BulkReadAsync(idsToRead);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(readEntities => readEntities.Count.ShouldBe(5));
    }

    #endregion

    #region BulkUpdateAsync Tests

    [SkippableFact]
    public async Task BulkUpdateAsync_EmptyCollection_ReturnsRightWithZero()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange
        _dbContext = _fixture.CreateDbContext<TestEFDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();
        _bulkOps = new BulkOperationsEF<TestRepositoryEntity>(_dbContext);

        var entities = new List<TestRepositoryEntity>();

        // Act
        var result = await _bulkOps.BulkUpdateAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    [SkippableFact]
    public async Task BulkUpdateAsync_ExistingEntities_UpdatesSuccessfully()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange
        _dbContext = _fixture.CreateDbContext<TestEFDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();
        _bulkOps = new BulkOperationsEF<TestRepositoryEntity>(_dbContext);

        var entities = CreateEntities(10);
        await _bulkOps.BulkInsertAsync(entities);

        foreach (var entity in entities)
        {
            entity.Name = $"Updated_{entity.Name}";
        }

        // Act
        var result = await _bulkOps.BulkUpdateAsync(entities);

        // Assert
        var updateCount = result.Match(
            Right: count => count,
            Left: error => throw new InvalidOperationException($"BulkUpdate failed: {error.Message}")
        );
        updateCount.ShouldBe(10);
    }

    #endregion

    #region BulkDeleteAsync Tests

    [SkippableFact]
    public async Task BulkDeleteAsync_EmptyCollection_ReturnsRightWithZero()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange
        _dbContext = _fixture.CreateDbContext<TestEFDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();
        _bulkOps = new BulkOperationsEF<TestRepositoryEntity>(_dbContext);

        var entities = new List<TestRepositoryEntity>();

        // Act
        var result = await _bulkOps.BulkDeleteAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    [SkippableFact]
    public async Task BulkDeleteAsync_ExistingEntities_DeletesSuccessfully()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange
        _dbContext = _fixture.CreateDbContext<TestEFDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();
        _bulkOps = new BulkOperationsEF<TestRepositoryEntity>(_dbContext);

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
    }

    #endregion

    #region BulkMergeAsync Tests

    [SkippableFact]
    public async Task BulkMergeAsync_EmptyCollection_ReturnsRightWithZero()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange
        _dbContext = _fixture.CreateDbContext<TestEFDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();
        _bulkOps = new BulkOperationsEF<TestRepositoryEntity>(_dbContext);

        var entities = new List<TestRepositoryEntity>();

        // Act
        var result = await _bulkOps.BulkMergeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    [SkippableFact]
    public async Task BulkMergeAsync_NewEntities_InsertsSuccessfully()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange
        _dbContext = _fixture.CreateDbContext<TestEFDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();
        _bulkOps = new BulkOperationsEF<TestRepositoryEntity>(_dbContext);

        var entities = CreateEntities(10);

        // Act
        var result = await _bulkOps.BulkMergeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(10));
    }

    #endregion
}
