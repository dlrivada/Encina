using Encina.EntityFrameworkCore.Auditing;
using Encina.Security.Audit;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Encina.GuardTests.EntityFrameworkCore.Auditing;

/// <summary>
/// Guard clause tests for <see cref="ReadAuditStoreEF"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class ReadAuditStoreEFGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext dbContext = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ReadAuditStoreEF(dbContext));
        ex.ParamName.ShouldBe("dbContext");
    }

    #endregion

    #region LogReadAsync Guards

    [Fact]
    public async Task LogReadAsync_NullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestReadAuditDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestReadAuditDbContext(options);
        var store = new ReadAuditStoreEF(dbContext);
        ReadAuditEntry entry = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.LogReadAsync(entry));
        ex.ParamName.ShouldBe("entry");
    }

    #endregion

    #region GetAccessHistoryAsync Guards

    [Fact]
    public async Task GetAccessHistoryAsync_NullEntityType_ThrowsArgumentException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestReadAuditDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestReadAuditDbContext(options);
        var store = new ReadAuditStoreEF(dbContext);
        string entityType = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetAccessHistoryAsync(entityType, "entity-1"));
        ex.ParamName.ShouldBe("entityType");
    }

    [Fact]
    public async Task GetAccessHistoryAsync_WhitespaceEntityType_ThrowsArgumentException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestReadAuditDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestReadAuditDbContext(options);
        var store = new ReadAuditStoreEF(dbContext);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetAccessHistoryAsync("  ", "entity-1"));
        ex.ParamName.ShouldBe("entityType");
    }

    [Fact]
    public async Task GetAccessHistoryAsync_NullEntityId_ThrowsArgumentException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestReadAuditDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestReadAuditDbContext(options);
        var store = new ReadAuditStoreEF(dbContext);
        string entityId = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetAccessHistoryAsync("Order", entityId));
        ex.ParamName.ShouldBe("entityId");
    }

    [Fact]
    public async Task GetAccessHistoryAsync_WhitespaceEntityId_ThrowsArgumentException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestReadAuditDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestReadAuditDbContext(options);
        var store = new ReadAuditStoreEF(dbContext);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetAccessHistoryAsync("Order", "  "));
        ex.ParamName.ShouldBe("entityId");
    }

    #endregion

    #region GetUserAccessHistoryAsync Guards

    [Fact]
    public async Task GetUserAccessHistoryAsync_NullUserId_ThrowsArgumentException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestReadAuditDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestReadAuditDbContext(options);
        var store = new ReadAuditStoreEF(dbContext);
        string userId = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetUserAccessHistoryAsync(userId, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow));
        ex.ParamName.ShouldBe("userId");
    }

    [Fact]
    public async Task GetUserAccessHistoryAsync_WhitespaceUserId_ThrowsArgumentException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestReadAuditDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestReadAuditDbContext(options);
        var store = new ReadAuditStoreEF(dbContext);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetUserAccessHistoryAsync("  ", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow));
        ex.ParamName.ShouldBe("userId");
    }

    #endregion

    #region QueryAsync Guards

    [Fact]
    public async Task QueryAsync_NullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestReadAuditDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestReadAuditDbContext(options);
        var store = new ReadAuditStoreEF(dbContext);
        ReadAuditQuery query = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.QueryAsync(query));
        ex.ParamName.ShouldBe("query");
    }

    #endregion

    #region Test Infrastructure

    private sealed class TestReadAuditDbContext : DbContext
    {
        public TestReadAuditDbContext(DbContextOptions<TestReadAuditDbContext> options) : base(options)
        {
        }

        public DbSet<ReadAuditEntryEntity> ReadAuditEntries => Set<ReadAuditEntryEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReadAuditEntryEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
        }
    }

    #endregion
}
