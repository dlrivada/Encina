using Encina.DomainModeling.Auditing;
using Encina.EntityFrameworkCore.Auditing;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Auditing;

/// <summary>
/// Unit tests for <see cref="AuditLogStoreEF"/> using EF Core InMemoryDatabase.
/// </summary>
[Trait("Category", "Unit")]
public sealed class AuditLogStoreEFTests : IDisposable
{
    private readonly AuditTestDbContext _dbContext;
    private readonly AuditLogStoreEF _store;

    public AuditLogStoreEFTests()
    {
        var options = new DbContextOptionsBuilder<AuditTestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AuditTestDbContext(options);
        _store = new AuditLogStoreEF(_dbContext);
    }

    #region LogAsync Tests

    [Fact]
    public async Task LogAsync_ValidEntry_ShouldAddToDatabase()
    {
        // Arrange
        var entry = CreateTestEntry();

        // Act
        await _store.LogAsync(entry);

        // Assert
        var stored = await _dbContext.AuditLogEntries.FindAsync(entry.Id);
        stored.ShouldNotBeNull();
        stored.EntityType.ShouldBe(entry.EntityType);
        stored.EntityId.ShouldBe(entry.EntityId);
        stored.Action.ShouldBe(entry.Action);
        stored.UserId.ShouldBe(entry.UserId);
        stored.TimestampUtc.ShouldBe(entry.TimestampUtc);
        stored.OldValues.ShouldBe(entry.OldValues);
        stored.NewValues.ShouldBe(entry.NewValues);
        stored.CorrelationId.ShouldBe(entry.CorrelationId);
    }

    [Fact]
    public async Task LogAsync_NullEntry_ShouldThrowArgumentNullException()
    {
        // Arrange
        AuditLogEntry entry = null!;

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentNullException>(
            async () => await _store.LogAsync(entry));

        exception.ParamName.ShouldBe("entry");
    }

    [Fact]
    public async Task LogAsync_MultipleEntries_ShouldPersistAll()
    {
        // Arrange
        var entry1 = CreateTestEntry(entityId: "entity-1");
        var entry2 = CreateTestEntry(entityId: "entity-2");
        var entry3 = CreateTestEntry(entityId: "entity-3");

        // Act
        await _store.LogAsync(entry1);
        await _store.LogAsync(entry2);
        await _store.LogAsync(entry3);

        // Assert
        var count = await _dbContext.AuditLogEntries.CountAsync();
        count.ShouldBe(3);
    }

    [Fact]
    public async Task LogAsync_EntryWithNullOptionalFields_ShouldSucceed()
    {
        // Arrange
        var entry = new AuditLogEntry(
            Id: Guid.NewGuid().ToString(),
            EntityType: "Order",
            EntityId: "123",
            Action: AuditAction.Created,
            UserId: null,
            TimestampUtc: DateTime.UtcNow,
            OldValues: null,
            NewValues: null,
            CorrelationId: null);

        // Act
        await _store.LogAsync(entry);

        // Assert
        var stored = await _dbContext.AuditLogEntries.FindAsync(entry.Id);
        stored.ShouldNotBeNull();
        stored.UserId.ShouldBeNull();
        stored.OldValues.ShouldBeNull();
        stored.NewValues.ShouldBeNull();
        stored.CorrelationId.ShouldBeNull();
    }

    #endregion

    #region GetHistoryAsync Tests

    [Fact]
    public async Task GetHistoryAsync_ExistingEntity_ShouldReturnEntries()
    {
        // Arrange
        const string entityType = "Order";
        const string entityId = "order-123";

        var entry1 = CreateTestEntry(entityType: entityType, entityId: entityId, timestampUtc: DateTime.UtcNow.AddMinutes(-10));
        var entry2 = CreateTestEntry(entityType: entityType, entityId: entityId, timestampUtc: DateTime.UtcNow.AddMinutes(-5));
        var entry3 = CreateTestEntry(entityType: entityType, entityId: entityId, timestampUtc: DateTime.UtcNow);

        await _store.LogAsync(entry1);
        await _store.LogAsync(entry2);
        await _store.LogAsync(entry3);

        // Act
        var history = (await _store.GetHistoryAsync(entityType, entityId)).ToList();

        // Assert
        history.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetHistoryAsync_NonExistentEntity_ShouldReturnEmpty()
    {
        // Arrange
        var entry = CreateTestEntry(entityType: "Order", entityId: "order-123");
        await _store.LogAsync(entry);

        // Act
        var history = await _store.GetHistoryAsync("Customer", "customer-456");

        // Assert
        history.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetHistoryAsync_NullEntityType_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = await Should.ThrowAsync<ArgumentNullException>(
            async () => await _store.GetHistoryAsync(null!, "123"));

        exception.ParamName.ShouldBe("entityType");
    }

    [Fact]
    public async Task GetHistoryAsync_NullEntityId_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = await Should.ThrowAsync<ArgumentNullException>(
            async () => await _store.GetHistoryAsync("Order", null!));

        exception.ParamName.ShouldBe("entityId");
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldOrderByTimestampDescending()
    {
        // Arrange
        const string entityType = "Order";
        const string entityId = "order-123";

        var olderEntry = CreateTestEntry(entityType: entityType, entityId: entityId, timestampUtc: DateTime.UtcNow.AddHours(-2));
        var newerEntry = CreateTestEntry(entityType: entityType, entityId: entityId, timestampUtc: DateTime.UtcNow.AddHours(-1));
        var newestEntry = CreateTestEntry(entityType: entityType, entityId: entityId, timestampUtc: DateTime.UtcNow);

        await _store.LogAsync(olderEntry);
        await _store.LogAsync(newerEntry);
        await _store.LogAsync(newestEntry);

        // Act
        var history = (await _store.GetHistoryAsync(entityType, entityId)).ToList();

        // Assert
        history.Count.ShouldBe(3);
        history[0].TimestampUtc.ShouldBeGreaterThanOrEqualTo(history[1].TimestampUtc);
        history[1].TimestampUtc.ShouldBeGreaterThanOrEqualTo(history[2].TimestampUtc);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldFilterByBothEntityTypeAndEntityId()
    {
        // Arrange
        await _store.LogAsync(CreateTestEntry(entityType: "Order", entityId: "123"));
        await _store.LogAsync(CreateTestEntry(entityType: "Order", entityId: "456"));
        await _store.LogAsync(CreateTestEntry(entityType: "Customer", entityId: "123"));

        // Act
        var orderHistory = (await _store.GetHistoryAsync("Order", "123")).ToList();

        // Assert
        orderHistory.Count.ShouldBe(1);
        orderHistory[0].EntityType.ShouldBe("Order");
        orderHistory[0].EntityId.ShouldBe("123");
    }

    #endregion

    #region Helper Methods

    private static AuditLogEntry CreateTestEntry(
        string entityType = "Order",
        string entityId = "order-123",
        DateTime? timestampUtc = null) =>
        new(
            Id: Guid.NewGuid().ToString(),
            EntityType: entityType,
            EntityId: entityId,
            Action: AuditAction.Created,
            UserId: "test-user",
            TimestampUtc: timestampUtc ?? DateTime.UtcNow,
            OldValues: null,
            NewValues: "{\"Name\":\"Test\"}",
            CorrelationId: Guid.NewGuid().ToString());

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
}

/// <summary>
/// Test DbContext for AuditLogStore tests.
/// </summary>
internal sealed class AuditTestDbContext : DbContext
{
    public AuditTestDbContext(DbContextOptions<AuditTestDbContext> options) : base(options) { }

    public DbSet<AuditLogEntryEntity> AuditLogEntries => Set<AuditLogEntryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLogEntryEntity>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityType).HasMaxLength(256).IsRequired();
            entity.Property(e => e.EntityId).HasMaxLength(256).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(256);
            entity.Property(e => e.CorrelationId).HasMaxLength(256);
        });
    }
}
