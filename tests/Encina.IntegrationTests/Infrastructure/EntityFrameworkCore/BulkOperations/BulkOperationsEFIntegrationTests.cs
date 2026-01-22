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
/// The EF Core bulk operations implementation requires SQL Server for SqlBulkCopy and TVP operations.
/// When used with SQLite, the operations correctly return <c>Left</c> with appropriate error messages.
/// </para>
/// <para>
/// These tests verify:
/// <list type="bullet">
/// <item><description>Empty collections return <c>Right(0)</c> for all operations</description></item>
/// <item><description>Non-SQL Server connections return <c>Left</c> with descriptive errors</description></item>
/// <item><description>Error messages include meaningful context (operation type, reason)</description></item>
/// </list>
/// </para>
/// <para>
/// For full SQL Server integration tests, see the tests using <c>SqlServerFixture</c>.
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

    public Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<BulkTestDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _dbContext = new BulkTestDbContext(options);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();

        _bulkOps = new BulkOperationsEF<BulkTestEntity>(_dbContext);

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _dbContext?.Dispose();
        return Task.CompletedTask;
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

    #region BulkInsertAsync Tests - SQLite Not Supported

    [Fact]
    public async Task BulkInsertAsync_NonSqlServerConnection_ReturnsLeftWithError()
    {
        // Arrange
        var entities = CreateEntities(10);

        // Act
        var result = await _bulkOps.BulkInsertAsync(entities);

        // Assert - SQLite doesn't support SqlBulkCopy, should return Left
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("SQL Server");
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(RepositoryErrors.BulkInsertFailedErrorCode));
        });
    }

    [Fact]
    public async Task BulkInsertAsync_WithConfig_NonSqlServer_ReturnsLeftWithError()
    {
        // Arrange
        var entities = CreateEntities(100);
        var config = BulkConfig.Default with { BatchSize = 50 };

        // Act
        var result = await _bulkOps.BulkInsertAsync(entities, config);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("SQL Server"));
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

    #region BulkUpdateAsync Tests - SQLite Not Supported

    [Fact]
    public async Task BulkUpdateAsync_NonSqlServerConnection_ReturnsLeftWithError()
    {
        // Arrange
        var entities = CreateEntities(5);

        // Act
        var result = await _bulkOps.BulkUpdateAsync(entities);

        // Assert - SQLite doesn't support TVP operations
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("SQL Server");
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(RepositoryErrors.BulkUpdateFailedErrorCode));
        });
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

    #region BulkDeleteAsync Tests - SQLite Not Supported

    [Fact]
    public async Task BulkDeleteAsync_NonSqlServerConnection_ReturnsLeftWithError()
    {
        // Arrange
        var entities = CreateEntities(5);

        // Act
        var result = await _bulkOps.BulkDeleteAsync(entities);

        // Assert - SQLite doesn't support TVP operations
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("SQL Server");
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(RepositoryErrors.BulkDeleteFailedErrorCode));
        });
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

    #region BulkMergeAsync Tests - SQLite Not Supported

    [Fact]
    public async Task BulkMergeAsync_NonSqlServerConnection_ReturnsLeftWithError()
    {
        // Arrange
        var entities = CreateEntities(10);

        // Act
        var result = await _bulkOps.BulkMergeAsync(entities);

        // Assert - SQLite doesn't support TVP operations
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("SQL Server");
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(RepositoryErrors.BulkMergeFailedErrorCode));
        });
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

    #region BulkReadAsync Tests - SQLite Not Supported

    [Fact]
    public async Task BulkReadAsync_NonSqlServerConnection_ReturnsLeftWithError()
    {
        // Arrange
        var ids = new List<object> { Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var result = await _bulkOps.BulkReadAsync(ids);

        // Assert - SQLite doesn't support TVP operations
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("SQL Server");
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(RepositoryErrors.BulkReadFailedErrorCode));
        });
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
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
        });
    }
}

#endregion
