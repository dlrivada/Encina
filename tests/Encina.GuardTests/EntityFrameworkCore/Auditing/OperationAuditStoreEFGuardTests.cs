using Encina.EntityFrameworkCore.Auditing;
using Encina.Security.Audit;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Encina.GuardTests.EntityFrameworkCore.Auditing;

/// <summary>
/// Guard clause tests for <see cref="OperationAuditStoreEF"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class OperationAuditStoreEFGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext dbContext = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new OperationAuditStoreEF(dbContext));
        ex.ParamName.ShouldBe("dbContext");
    }

    #endregion

    #region RecordAsync Guards

    [Fact]
    public async Task RecordAsync_NullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestAuditStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestAuditStoreDbContext(options);
        var store = new OperationAuditStoreEF(dbContext);
        OperationAuditEntry entry = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.RecordAsync(entry));
        ex.ParamName.ShouldBe("entry");
    }

    #endregion

    #region GetByEntityAsync Guards

    [Fact]
    public async Task GetByEntityAsync_NullEntityType_ThrowsArgumentException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestAuditStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestAuditStoreDbContext(options);
        var store = new OperationAuditStoreEF(dbContext);
        string entityType = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetByEntityAsync(entityType, "entity-1"));
        ex.ParamName.ShouldBe("entityType");
    }

    [Fact]
    public async Task GetByEntityAsync_WhitespaceEntityType_ThrowsArgumentException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestAuditStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestAuditStoreDbContext(options);
        var store = new OperationAuditStoreEF(dbContext);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetByEntityAsync("  ", "entity-1"));
        ex.ParamName.ShouldBe("entityType");
    }

    #endregion

    #region GetByUserAsync Guards

    [Fact]
    public async Task GetByUserAsync_NullUserId_ThrowsArgumentException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestAuditStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestAuditStoreDbContext(options);
        var store = new OperationAuditStoreEF(dbContext);
        string userId = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetByUserAsync(userId, null, null));
        ex.ParamName.ShouldBe("userId");
    }

    [Fact]
    public async Task GetByUserAsync_WhitespaceUserId_ThrowsArgumentException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestAuditStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestAuditStoreDbContext(options);
        var store = new OperationAuditStoreEF(dbContext);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetByUserAsync("  ", null, null));
        ex.ParamName.ShouldBe("userId");
    }

    #endregion

    #region GetByCorrelationIdAsync Guards

    [Fact]
    public async Task GetByCorrelationIdAsync_NullCorrelationId_ThrowsArgumentException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestAuditStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestAuditStoreDbContext(options);
        var store = new OperationAuditStoreEF(dbContext);
        string correlationId = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetByCorrelationIdAsync(correlationId));
        ex.ParamName.ShouldBe("correlationId");
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_WhitespaceCorrelationId_ThrowsArgumentException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestAuditStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestAuditStoreDbContext(options);
        var store = new OperationAuditStoreEF(dbContext);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetByCorrelationIdAsync("  "));
        ex.ParamName.ShouldBe("correlationId");
    }

    #endregion

    #region QueryAsync Guards

    [Fact]
    public async Task QueryAsync_NullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestAuditStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestAuditStoreDbContext(options);
        var store = new OperationAuditStoreEF(dbContext);
        OperationAuditQuery query = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.QueryAsync(query));
        ex.ParamName.ShouldBe("query");
    }

    #endregion

    #region Test Infrastructure

    private sealed class TestAuditStoreDbContext : DbContext
    {
        public TestAuditStoreDbContext(DbContextOptions<TestAuditStoreDbContext> options) : base(options)
        {
        }

        public DbSet<OperationAuditEntryEntity> AuditEntries => Set<OperationAuditEntryEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OperationAuditEntryEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
        }
    }

    #endregion
}
