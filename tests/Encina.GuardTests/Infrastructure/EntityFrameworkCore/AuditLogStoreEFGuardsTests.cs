using Encina.DomainModeling.Auditing;
using Encina.EntityFrameworkCore.Auditing;
using Microsoft.EntityFrameworkCore;

namespace Encina.GuardTests.Infrastructure.EntityFrameworkCore;

/// <summary>
/// Guard tests for <see cref="AuditLogStoreEF"/> to verify null parameter handling.
/// </summary>
public class AuditLogStoreEFGuardsTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when dbContext is null.
    /// </summary>
    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext dbContext = null!;

        // Act & Assert
        var act = () => new AuditLogStoreEF(dbContext);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("dbContext");
    }

    /// <summary>
    /// Verifies that LogAsync throws ArgumentNullException when entry is null.
    /// </summary>
    [Fact]
    public async Task LogAsync_NullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var store = new AuditLogStoreEF(dbContext);
        AuditLogEntry entry = null!;

        // Act & Assert
        Func<Task> act = () => store.LogAsync(entry);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entry");
    }

    /// <summary>
    /// Verifies that GetHistoryAsync throws ArgumentNullException when entityType is null.
    /// </summary>
    [Fact]
    public async Task GetHistoryAsync_NullEntityType_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var store = new AuditLogStoreEF(dbContext);
        string entityType = null!;
        const string entityId = "123";

        // Act & Assert
        Func<Task> act = () => store.GetHistoryAsync(entityType, entityId);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entityType");
    }

    /// <summary>
    /// Verifies that GetHistoryAsync throws ArgumentNullException when entityId is null.
    /// </summary>
    [Fact]
    public async Task GetHistoryAsync_NullEntityId_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var store = new AuditLogStoreEF(dbContext);
        const string entityType = "Order";
        string entityId = null!;

        // Act & Assert
        Func<Task> act = () => store.GetHistoryAsync(entityType, entityId);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entityId");
    }

    /// <summary>
    /// Test DbContext for in-memory database testing.
    /// </summary>
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<AuditLogEntryEntity> AuditLogEntries => Set<AuditLogEntryEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLogEntryEntity>(entity =>
            {
                entity.ToTable("AuditLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EntityType).HasMaxLength(256).IsRequired();
                entity.Property(e => e.EntityId).HasMaxLength(256).IsRequired();
            });
        }
    }
}
