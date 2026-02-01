using Encina.Security.Audit;
using FluentAssertions;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for <see cref="InMemoryAuditStore"/>.
/// </summary>
public class InMemoryAuditStoreTests
{
    private readonly InMemoryAuditStore _store;

    public InMemoryAuditStoreTests()
    {
        _store = new InMemoryAuditStore();
    }

    #region RecordAsync Tests

    [Fact]
    public async Task RecordAsync_ShouldAddEntryToStore()
    {
        // Arrange
        var entry = CreateTestEntry();

        // Act
        var result = await _store.RecordAsync(entry);

        // Assert
        result.IsRight.Should().BeTrue();
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task RecordAsync_ShouldReturnUnitOnSuccess()
    {
        // Arrange
        var entry = CreateTestEntry();

        // Act
        var result = await _store.RecordAsync(entry);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task RecordAsync_WithNullEntry_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _store.RecordAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("entry");
    }

    [Fact]
    public async Task RecordAsync_MultipleTimes_ShouldStoreAllEntries()
    {
        // Arrange
        var entry1 = CreateTestEntry();
        var entry2 = CreateTestEntry();
        var entry3 = CreateTestEntry();

        // Act
        await _store.RecordAsync(entry1);
        await _store.RecordAsync(entry2);
        await _store.RecordAsync(entry3);

        // Assert
        _store.Count.Should().Be(3);
    }

    #endregion

    #region GetByEntityAsync Tests

    [Fact]
    public async Task GetByEntityAsync_ShouldReturnMatchingEntries()
    {
        // Arrange
        var entry1 = CreateTestEntry() with { EntityType = "Order", EntityId = "order-1" };
        var entry2 = CreateTestEntry() with { EntityType = "Order", EntityId = "order-2" };
        var entry3 = CreateTestEntry() with { EntityType = "Customer", EntityId = "cust-1" };

        await _store.RecordAsync(entry1);
        await _store.RecordAsync(entry2);
        await _store.RecordAsync(entry3);

        // Act
        var result = await _store.GetByEntityAsync("Order", null);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: entries =>
            {
                entries.Should().HaveCount(2);
                entries.All(e => e.EntityType == "Order").Should().BeTrue();
                return true;
            },
            Left: _ => false);
    }

    [Fact]
    public async Task GetByEntityAsync_WithEntityId_ShouldFilterByEntityId()
    {
        // Arrange
        var entry1 = CreateTestEntry() with { EntityType = "Order", EntityId = "order-1" };
        var entry2 = CreateTestEntry() with { EntityType = "Order", EntityId = "order-2" };

        await _store.RecordAsync(entry1);
        await _store.RecordAsync(entry2);

        // Act
        var result = await _store.GetByEntityAsync("Order", "order-1");

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: entries =>
            {
                entries.Should().HaveCount(1);
                entries[0].EntityId.Should().Be("order-1");
                return true;
            },
            Left: _ => false);
    }

    [Fact]
    public async Task GetByEntityAsync_WhenNoMatches_ShouldReturnEmptyCollection()
    {
        // Arrange
        var entry = CreateTestEntry() with { EntityType = "Order" };
        await _store.RecordAsync(entry);

        // Act
        var result = await _store.GetByEntityAsync("Customer", null);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: entries =>
            {
                entries.Should().BeEmpty();
                return true;
            },
            Left: _ => false);
    }

    [Fact]
    public async Task GetByEntityAsync_WithNullEntityType_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _store.GetByEntityAsync(null!, null);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetByUserAsync Tests

    [Fact]
    public async Task GetByUserAsync_ShouldReturnMatchingEntries()
    {
        // Arrange
        var entry1 = CreateTestEntry() with { UserId = "user-1" };
        var entry2 = CreateTestEntry() with { UserId = "user-1" };
        var entry3 = CreateTestEntry() with { UserId = "user-2" };

        await _store.RecordAsync(entry1);
        await _store.RecordAsync(entry2);
        await _store.RecordAsync(entry3);

        // Act
        var result = await _store.GetByUserAsync("user-1", null, null);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: entries =>
            {
                entries.Should().HaveCount(2);
                entries.All(e => e.UserId == "user-1").Should().BeTrue();
                return true;
            },
            Left: _ => false);
    }

    [Fact]
    public async Task GetByUserAsync_WithDateRange_ShouldFilterByTimestamp()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        var entry1 = CreateTestEntry() with { UserId = "user-1", TimestampUtc = baseTime.AddDays(-2) };
        var entry2 = CreateTestEntry() with { UserId = "user-1", TimestampUtc = baseTime };
        var entry3 = CreateTestEntry() with { UserId = "user-1", TimestampUtc = baseTime.AddDays(2) };

        await _store.RecordAsync(entry1);
        await _store.RecordAsync(entry2);
        await _store.RecordAsync(entry3);

        // Act - Get entries from yesterday to tomorrow
        var result = await _store.GetByUserAsync("user-1", baseTime.AddDays(-1), baseTime.AddDays(1));

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: entries =>
            {
                entries.Should().HaveCount(1);
                return true;
            },
            Left: _ => false);
    }

    [Fact]
    public async Task GetByUserAsync_WithOnlyFromDate_ShouldFilterFromDate()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        var entry1 = CreateTestEntry() with { UserId = "user-1", TimestampUtc = baseTime.AddDays(-2) };
        var entry2 = CreateTestEntry() with { UserId = "user-1", TimestampUtc = baseTime };

        await _store.RecordAsync(entry1);
        await _store.RecordAsync(entry2);

        // Act
        var result = await _store.GetByUserAsync("user-1", baseTime.AddDays(-1), null);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: entries =>
            {
                entries.Should().HaveCount(1);
                return true;
            },
            Left: _ => false);
    }

    [Fact]
    public async Task GetByUserAsync_WhenNoMatches_ShouldReturnEmptyCollection()
    {
        // Arrange
        var entry = CreateTestEntry() with { UserId = "user-1" };
        await _store.RecordAsync(entry);

        // Act
        var result = await _store.GetByUserAsync("user-999", null, null);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: entries =>
            {
                entries.Should().BeEmpty();
                return true;
            },
            Left: _ => false);
    }

    [Fact]
    public async Task GetByUserAsync_WithNullUserId_ShouldThrowArgumentException()
    {
        // Act
        var act = async () => await _store.GetByUserAsync(null!, null, null);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetByCorrelationIdAsync Tests

    [Fact]
    public async Task GetByCorrelationIdAsync_ShouldReturnMatchingEntries()
    {
        // Arrange
        var entry1 = CreateTestEntry() with { CorrelationId = "corr-1" };
        var entry2 = CreateTestEntry() with { CorrelationId = "corr-1" };
        var entry3 = CreateTestEntry() with { CorrelationId = "corr-2" };

        await _store.RecordAsync(entry1);
        await _store.RecordAsync(entry2);
        await _store.RecordAsync(entry3);

        // Act
        var result = await _store.GetByCorrelationIdAsync("corr-1");

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: entries =>
            {
                entries.Should().HaveCount(2);
                entries.All(e => e.CorrelationId == "corr-1").Should().BeTrue();
                return true;
            },
            Left: _ => false);
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_WhenNoMatches_ShouldReturnEmptyCollection()
    {
        // Arrange
        var entry = CreateTestEntry() with { CorrelationId = "corr-1" };
        await _store.RecordAsync(entry);

        // Act
        var result = await _store.GetByCorrelationIdAsync("corr-999");

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: entries =>
            {
                entries.Should().BeEmpty();
                return true;
            },
            Left: _ => false);
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_WithNullCorrelationId_ShouldThrowArgumentException()
    {
        // Act
        var act = async () => await _store.GetByCorrelationIdAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task RecordAsync_ConcurrentWrites_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => _store.RecordAsync(CreateTestEntry()).AsTask())
            .ToList();

        // Act
        await Task.WhenAll(tasks);

        // Assert
        _store.Count.Should().Be(100);
    }

    [Fact]
    public async Task GetAndRecord_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var writeTasks = Enumerable.Range(0, 50)
            .Select(_ => _store.RecordAsync(CreateTestEntry() with { UserId = "user-1" }).AsTask());

        var readTasks = Enumerable.Range(0, 50)
            .Select(_ => _store.GetByUserAsync("user-1", null, null).AsTask());

        // Act - Mix reads and writes
        var allTasks = new List<Task>();
        allTasks.AddRange(writeTasks);
        allTasks.AddRange(readTasks.Select(t => (Task)t));
        await Task.WhenAll(allTasks);

        // Assert - Store should contain all written entries
        _store.Count.Should().Be(50);
    }

    #endregion

    #region Helper Methods Tests

    [Fact]
    public void Count_ShouldReturnNumberOfEntries()
    {
        // Arrange & Act - Empty store
        _store.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetAllEntries_ShouldReturnAllStoredEntries()
    {
        // Arrange
        var entry1 = CreateTestEntry();
        var entry2 = CreateTestEntry();
        await _store.RecordAsync(entry1);
        await _store.RecordAsync(entry2);

        // Act
        var entries = _store.GetAllEntries();

        // Assert
        entries.Should().HaveCount(2);
    }

    [Fact]
    public async Task Clear_ShouldRemoveAllEntries()
    {
        // Arrange
        await _store.RecordAsync(CreateTestEntry());
        await _store.RecordAsync(CreateTestEntry());

        // Act
        _store.Clear();

        // Assert
        _store.Count.Should().Be(0);
        _store.GetAllEntries().Should().BeEmpty();
    }

    #endregion

    #region Helpers

    private static AuditEntry CreateTestEntry() => new()
    {
        Id = Guid.NewGuid(),
        CorrelationId = $"corr-{Guid.NewGuid():N}",
        UserId = "test-user",
        TenantId = "test-tenant",
        Action = "Test",
        EntityType = "TestEntity",
        EntityId = Guid.NewGuid().ToString(),
        Outcome = AuditOutcome.Success,
        TimestampUtc = DateTime.UtcNow,
        Metadata = new Dictionary<string, object?>()
    };

    #endregion
}
