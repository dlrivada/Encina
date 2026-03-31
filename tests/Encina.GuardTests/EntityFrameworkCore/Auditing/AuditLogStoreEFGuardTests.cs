using Encina.DomainModeling.Auditing;
using Encina.EntityFrameworkCore.Auditing;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.EntityFrameworkCore.Auditing;

/// <summary>
/// Guard clause tests for <see cref="AuditLogStoreEF"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class AuditLogStoreEFGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext dbContext = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new AuditLogStoreEF(dbContext));
        ex.ParamName.ShouldBe("dbContext");
    }

    #endregion

    #region LogAsync Guards

    [Fact]
    public async Task LogAsync_NullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestAuditLogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestAuditLogDbContext(options);
        var store = new AuditLogStoreEF(dbContext);
        AuditLogEntry entry = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.LogAsync(entry));
        ex.ParamName.ShouldBe("entry");
    }

    #endregion

    #region GetHistoryAsync Guards

    [Fact]
    public async Task GetHistoryAsync_NullEntityType_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestAuditLogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestAuditLogDbContext(options);
        var store = new AuditLogStoreEF(dbContext);
        string entityType = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.GetHistoryAsync(entityType, "entity-1"));
        ex.ParamName.ShouldBe("entityType");
    }

    [Fact]
    public async Task GetHistoryAsync_NullEntityId_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestAuditLogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestAuditLogDbContext(options);
        var store = new AuditLogStoreEF(dbContext);
        string entityId = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.GetHistoryAsync("Order", entityId));
        ex.ParamName.ShouldBe("entityId");
    }

    #endregion

    #region Test Infrastructure

    private sealed class TestAuditLogDbContext : DbContext
    {
        public TestAuditLogDbContext(DbContextOptions<TestAuditLogDbContext> options) : base(options)
        {
        }

        public DbSet<AuditLogEntryEntity> AuditLogEntries => Set<AuditLogEntryEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLogEntryEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
        }
    }

    #endregion
}
