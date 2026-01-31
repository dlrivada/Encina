using Encina.DomainModeling.Auditing;

namespace Encina.UnitTests.DomainModeling.Auditing;

public class InMemoryAuditLogStoreTests
{
    #region LogAsync Tests

    [Fact]
    public async Task LogAsync_WithValidEntry_StoresEntry()
    {
        // Arrange
        var store = new InMemoryAuditLogStore();
        var entry = CreateEntry("Order", "123", AuditAction.Created);

        // Act
        await store.LogAsync(entry);

        // Assert
        var history = await store.GetHistoryAsync("Order", "123");
        history.ShouldContain(entry);
    }

    [Fact]
    public async Task LogAsync_WithMultipleEntries_StoresAllEntries()
    {
        // Arrange
        var store = new InMemoryAuditLogStore();
        var entry1 = CreateEntry("Order", "123", AuditAction.Created, DateTime.UtcNow.AddMinutes(-2));
        var entry2 = CreateEntry("Order", "123", AuditAction.Updated, DateTime.UtcNow.AddMinutes(-1));
        var entry3 = CreateEntry("Order", "123", AuditAction.Updated, DateTime.UtcNow);

        // Act
        await store.LogAsync(entry1);
        await store.LogAsync(entry2);
        await store.LogAsync(entry3);

        // Assert
        var history = await store.GetHistoryAsync("Order", "123");
        var entries = history.ToList();
        entries.Count.ShouldBe(3);
    }

    [Fact]
    public async Task LogAsync_WithDifferentEntities_StoresSeparately()
    {
        // Arrange
        var store = new InMemoryAuditLogStore();
        var orderEntry = CreateEntry("Order", "123", AuditAction.Created);
        var customerEntry = CreateEntry("Customer", "456", AuditAction.Created);

        // Act
        await store.LogAsync(orderEntry);
        await store.LogAsync(customerEntry);

        // Assert
        var orderHistory = await store.GetHistoryAsync("Order", "123");
        var customerHistory = await store.GetHistoryAsync("Customer", "456");

        orderHistory.Count().ShouldBe(1);
        customerHistory.Count().ShouldBe(1);
        orderHistory.First().EntityType.ShouldBe("Order");
        customerHistory.First().EntityType.ShouldBe("Customer");
    }

    [Fact]
    public async Task LogAsync_WithNullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new InMemoryAuditLogStore();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.LogAsync(null!));
    }

    #endregion

    #region GetHistoryAsync Tests

    [Fact]
    public async Task GetHistoryAsync_WithExistingEntity_ReturnsEntriesOrderedByTimestampDescending()
    {
        // Arrange
        var store = new InMemoryAuditLogStore();
        var oldestTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var middleTime = new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc);
        var newestTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        var entry1 = CreateEntry("Order", "123", AuditAction.Created, oldestTime);
        var entry2 = CreateEntry("Order", "123", AuditAction.Updated, middleTime);
        var entry3 = CreateEntry("Order", "123", AuditAction.Updated, newestTime);

        // Add in mixed order
        await store.LogAsync(entry2);
        await store.LogAsync(entry1);
        await store.LogAsync(entry3);

        // Act
        var history = await store.GetHistoryAsync("Order", "123");
        var entries = history.ToList();

        // Assert - should be ordered by timestamp descending (newest first)
        entries.Count.ShouldBe(3);
        entries[0].TimestampUtc.ShouldBe(newestTime);
        entries[1].TimestampUtc.ShouldBe(middleTime);
        entries[2].TimestampUtc.ShouldBe(oldestTime);
    }

    [Fact]
    public async Task GetHistoryAsync_WithNonExistingEntity_ReturnsEmptyCollection()
    {
        // Arrange
        var store = new InMemoryAuditLogStore();
        await store.LogAsync(CreateEntry("Order", "123", AuditAction.Created));

        // Act
        var history = await store.GetHistoryAsync("Order", "999");

        // Assert
        history.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetHistoryAsync_WithNonExistingEntityType_ReturnsEmptyCollection()
    {
        // Arrange
        var store = new InMemoryAuditLogStore();
        await store.LogAsync(CreateEntry("Order", "123", AuditAction.Created));

        // Act
        var history = await store.GetHistoryAsync("Customer", "123");

        // Assert
        history.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetHistoryAsync_WithNullEntityType_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new InMemoryAuditLogStore();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.GetHistoryAsync(null!, "123"));
    }

    [Fact]
    public async Task GetHistoryAsync_WithNullEntityId_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new InMemoryAuditLogStore();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.GetHistoryAsync("Order", null!));
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsIndependentCopy()
    {
        // Arrange
        var store = new InMemoryAuditLogStore();
        var entry = CreateEntry("Order", "123", AuditAction.Created);
        await store.LogAsync(entry);

        // Act
        var history1 = await store.GetHistoryAsync("Order", "123");
        var history2 = await store.GetHistoryAsync("Order", "123");

        // Assert - modifying one result shouldn't affect the other
        history1.ShouldNotBeSameAs(history2);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public async Task Clear_RemovesAllEntries()
    {
        // Arrange
        var store = new InMemoryAuditLogStore();
        await store.LogAsync(CreateEntry("Order", "123", AuditAction.Created));
        await store.LogAsync(CreateEntry("Order", "456", AuditAction.Created));
        await store.LogAsync(CreateEntry("Customer", "789", AuditAction.Created));

        // Act
        store.Clear();

        // Assert
        (await store.GetHistoryAsync("Order", "123")).ShouldBeEmpty();
        (await store.GetHistoryAsync("Order", "456")).ShouldBeEmpty();
        (await store.GetHistoryAsync("Customer", "789")).ShouldBeEmpty();
        store.GetTotalCount().ShouldBe(0);
    }

    #endregion

    #region GetTotalCount Tests

    [Fact]
    public void GetTotalCount_WithEmptyStore_ReturnsZero()
    {
        // Arrange
        var store = new InMemoryAuditLogStore();

        // Act
        var count = store.GetTotalCount();

        // Assert
        count.ShouldBe(0);
    }

    [Fact]
    public async Task GetTotalCount_WithMultipleEntries_ReturnsTotalCount()
    {
        // Arrange
        var store = new InMemoryAuditLogStore();
        await store.LogAsync(CreateEntry("Order", "123", AuditAction.Created));
        await store.LogAsync(CreateEntry("Order", "123", AuditAction.Updated));
        await store.LogAsync(CreateEntry("Order", "456", AuditAction.Created));
        await store.LogAsync(CreateEntry("Customer", "789", AuditAction.Created));

        // Act
        var count = store.GetTotalCount();

        // Assert
        count.ShouldBe(4);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task LogAsync_ConcurrentWrites_AllEntriesAreStored()
    {
        // Arrange
        var store = new InMemoryAuditLogStore();
        const int numberOfEntries = 100;
        var tasks = new List<Task>();

        // Act - Concurrent writes
        for (int i = 0; i < numberOfEntries; i++)
        {
            var entry = CreateEntry("Order", "123", AuditAction.Updated,
                DateTime.UtcNow.AddSeconds(i));
            tasks.Add(store.LogAsync(entry));
        }

        await Task.WhenAll(tasks);

        // Assert
        var history = await store.GetHistoryAsync("Order", "123");
        history.Count().ShouldBe(numberOfEntries);
        store.GetTotalCount().ShouldBe(numberOfEntries);
    }

    [Fact]
    public async Task GetHistoryAsync_ConcurrentReads_AllSucceed()
    {
        // Arrange
        var store = new InMemoryAuditLogStore();
        for (int i = 0; i < 10; i++)
        {
            await store.LogAsync(CreateEntry("Order", "123", AuditAction.Updated));
        }

        // Act - Concurrent reads
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => store.GetHistoryAsync("Order", "123"))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        foreach (var result in results)
        {
            result.Count().ShouldBe(10);
        }
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task LogAsync_WithCancellationToken_DoesNotThrowWhenNotCancelled()
    {
        // Arrange
        var store = new InMemoryAuditLogStore();
        var entry = CreateEntry("Order", "123", AuditAction.Created);
        using var cts = new CancellationTokenSource();

        // Act & Assert - should not throw
        await store.LogAsync(entry, cts.Token);

        var history = await store.GetHistoryAsync("Order", "123");
        history.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetHistoryAsync_WithCancellationToken_DoesNotThrowWhenNotCancelled()
    {
        // Arrange
        var store = new InMemoryAuditLogStore();
        await store.LogAsync(CreateEntry("Order", "123", AuditAction.Created));
        using var cts = new CancellationTokenSource();

        // Act & Assert - should not throw
        var history = await store.GetHistoryAsync("Order", "123", cts.Token);
        history.ShouldNotBeEmpty();
    }

    #endregion

    #region Helper Methods

    private static AuditLogEntry CreateEntry(
        string entityType,
        string entityId,
        AuditAction action,
        DateTime? timestamp = null)
    {
        return new AuditLogEntry(
            Id: Guid.NewGuid().ToString(),
            EntityType: entityType,
            EntityId: entityId,
            Action: action,
            UserId: "test-user",
            TimestampUtc: timestamp ?? DateTime.UtcNow,
            OldValues: action == AuditAction.Created ? null : "{}",
            NewValues: action == AuditAction.Deleted ? null : "{}",
            CorrelationId: null);
    }

    #endregion
}
