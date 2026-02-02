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

    #region QueryAsync Tests

    [Fact]
    public async Task QueryAsync_WithNoFilters_ShouldReturnAllEntriesPaginated()
    {
        // Arrange
        for (int i = 0; i < 25; i++)
        {
            await _store.RecordAsync(CreateTestEntry());
        }

        var query = new AuditQuery { PageSize = 10 };

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: paged =>
            {
                paged.TotalCount.Should().Be(25);
                paged.Items.Should().HaveCount(10);
                paged.PageNumber.Should().Be(1);
                paged.TotalPages.Should().Be(3);
                return true;
            },
            Left: _ => false);
    }

    [Fact]
    public async Task QueryAsync_WithUserIdFilter_ShouldReturnMatchingEntries()
    {
        // Arrange
        await _store.RecordAsync(CreateTestEntry() with { UserId = "user-1" });
        await _store.RecordAsync(CreateTestEntry() with { UserId = "user-1" });
        await _store.RecordAsync(CreateTestEntry() with { UserId = "user-2" });

        var query = new AuditQuery { UserId = "user-1" };

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: paged =>
            {
                paged.TotalCount.Should().Be(2);
                paged.Items.All(e => e.UserId == "user-1").Should().BeTrue();
                return true;
            },
            Left: _ => false);
    }

    [Fact]
    public async Task QueryAsync_WithEntityTypeFilter_ShouldReturnMatchingEntries()
    {
        // Arrange
        await _store.RecordAsync(CreateTestEntry() with { EntityType = "Order" });
        await _store.RecordAsync(CreateTestEntry() with { EntityType = "Order" });
        await _store.RecordAsync(CreateTestEntry() with { EntityType = "Customer" });

        var query = new AuditQuery { EntityType = "Order" };

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: paged =>
            {
                paged.TotalCount.Should().Be(2);
                return true;
            },
            Left: _ => false);
    }

    [Fact]
    public async Task QueryAsync_WithOutcomeFilter_ShouldReturnMatchingEntries()
    {
        // Arrange
        await _store.RecordAsync(CreateTestEntry() with { Outcome = AuditOutcome.Success });
        await _store.RecordAsync(CreateTestEntry() with { Outcome = AuditOutcome.Failure });
        await _store.RecordAsync(CreateTestEntry() with { Outcome = AuditOutcome.Failure });

        var query = new AuditQuery { Outcome = AuditOutcome.Failure };

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: paged =>
            {
                paged.TotalCount.Should().Be(2);
                paged.Items.All(e => e.Outcome == AuditOutcome.Failure).Should().BeTrue();
                return true;
            },
            Left: _ => false);
    }

    [Fact]
    public async Task QueryAsync_WithDateRangeFilter_ShouldReturnMatchingEntries()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        await _store.RecordAsync(CreateTestEntry() with { TimestampUtc = baseTime.AddDays(-5) });
        await _store.RecordAsync(CreateTestEntry() with { TimestampUtc = baseTime });
        await _store.RecordAsync(CreateTestEntry() with { TimestampUtc = baseTime.AddDays(5) });

        var query = new AuditQuery
        {
            FromUtc = baseTime.AddDays(-1),
            ToUtc = baseTime.AddDays(1)
        };

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: paged =>
            {
                paged.TotalCount.Should().Be(1);
                return true;
            },
            Left: _ => false);
    }

    [Fact]
    public async Task QueryAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        for (int i = 0; i < 25; i++)
        {
            await _store.RecordAsync(CreateTestEntry());
        }

        var query = new AuditQuery { PageNumber = 2, PageSize = 10 };

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: paged =>
            {
                paged.TotalCount.Should().Be(25);
                paged.Items.Should().HaveCount(10);
                paged.PageNumber.Should().Be(2);
                paged.HasPreviousPage.Should().BeTrue();
                paged.HasNextPage.Should().BeTrue();
                return true;
            },
            Left: _ => false);
    }

    [Fact]
    public async Task QueryAsync_WithDurationFilter_ShouldReturnMatchingEntries()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _store.RecordAsync(CreateTestEntry() with
        {
            StartedAtUtc = now.AddMilliseconds(-50),
            CompletedAtUtc = now
        }); // 50ms duration
        await _store.RecordAsync(CreateTestEntry() with
        {
            StartedAtUtc = now.AddMilliseconds(-200),
            CompletedAtUtc = now
        }); // 200ms duration
        await _store.RecordAsync(CreateTestEntry() with
        {
            StartedAtUtc = now.AddSeconds(-2),
            CompletedAtUtc = now
        }); // 2s duration

        var query = new AuditQuery
        {
            MinDuration = TimeSpan.FromMilliseconds(100),
            MaxDuration = TimeSpan.FromSeconds(1)
        };

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: paged =>
            {
                paged.TotalCount.Should().Be(1); // Only the 200ms entry
                return true;
            },
            Left: _ => false);
    }

    [Fact]
    public async Task QueryAsync_WithNullQuery_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _store.QueryAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("query");
    }

    [Fact]
    public async Task QueryAsync_WithMultipleFilters_ShouldApplyAll()
    {
        // Arrange
        await _store.RecordAsync(CreateTestEntry() with { UserId = "user-1", EntityType = "Order", Outcome = AuditOutcome.Success });
        await _store.RecordAsync(CreateTestEntry() with { UserId = "user-1", EntityType = "Order", Outcome = AuditOutcome.Failure });
        await _store.RecordAsync(CreateTestEntry() with { UserId = "user-1", EntityType = "Customer", Outcome = AuditOutcome.Success });
        await _store.RecordAsync(CreateTestEntry() with { UserId = "user-2", EntityType = "Order", Outcome = AuditOutcome.Success });

        var query = new AuditQuery
        {
            UserId = "user-1",
            EntityType = "Order",
            Outcome = AuditOutcome.Success
        };

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: paged =>
            {
                paged.TotalCount.Should().Be(1);
                return true;
            },
            Left: _ => false);
    }

    #endregion

    #region PurgeEntriesAsync Tests

    [Fact]
    public async Task PurgeEntriesAsync_ShouldDeleteOldEntries()
    {
        // Arrange
        var cutoffDate = DateTime.UtcNow;
        await _store.RecordAsync(CreateTestEntry() with { TimestampUtc = cutoffDate.AddDays(-10) });
        await _store.RecordAsync(CreateTestEntry() with { TimestampUtc = cutoffDate.AddDays(-5) });
        await _store.RecordAsync(CreateTestEntry() with { TimestampUtc = cutoffDate.AddDays(1) });

        // Act
        var result = await _store.PurgeEntriesAsync(cutoffDate);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: count =>
            {
                count.Should().Be(2); // 2 entries older than cutoff
                _store.Count.Should().Be(1); // 1 entry remains
                return true;
            },
            Left: _ => false);
    }

    [Fact]
    public async Task PurgeEntriesAsync_WhenNoOldEntries_ShouldReturnZero()
    {
        // Arrange
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        await _store.RecordAsync(CreateTestEntry() with { TimestampUtc = DateTime.UtcNow });

        // Act
        var result = await _store.PurgeEntriesAsync(cutoffDate);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: count =>
            {
                count.Should().Be(0);
                _store.Count.Should().Be(1);
                return true;
            },
            Left: _ => false);
    }

    [Fact]
    public async Task PurgeEntriesAsync_WhenAllEntriesAreOld_ShouldDeleteAll()
    {
        // Arrange
        var cutoffDate = DateTime.UtcNow.AddDays(1);
        await _store.RecordAsync(CreateTestEntry() with { TimestampUtc = DateTime.UtcNow.AddDays(-10) });
        await _store.RecordAsync(CreateTestEntry() with { TimestampUtc = DateTime.UtcNow.AddDays(-5) });
        await _store.RecordAsync(CreateTestEntry() with { TimestampUtc = DateTime.UtcNow });

        // Act
        var result = await _store.PurgeEntriesAsync(cutoffDate);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: count =>
            {
                count.Should().Be(3);
                _store.Count.Should().Be(0);
                return true;
            },
            Left: _ => false);
    }

    [Fact]
    public async Task PurgeEntriesAsync_WithEmptyStore_ShouldReturnZero()
    {
        // Act
        var result = await _store.PurgeEntriesAsync(DateTime.UtcNow);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: count =>
            {
                count.Should().Be(0);
                return true;
            },
            Left: _ => false);
    }

    [Fact]
    public async Task PurgeEntriesAsync_ShouldOnlyDeleteEntriesStrictlyBeforeCutoff()
    {
        // Arrange
        var cutoffDate = DateTime.UtcNow;
        await _store.RecordAsync(CreateTestEntry() with { TimestampUtc = cutoffDate.AddMilliseconds(-1) }); // Just before
        await _store.RecordAsync(CreateTestEntry() with { TimestampUtc = cutoffDate }); // Exactly at cutoff - should NOT be deleted
        await _store.RecordAsync(CreateTestEntry() with { TimestampUtc = cutoffDate.AddMilliseconds(1) }); // Just after

        // Act
        var result = await _store.PurgeEntriesAsync(cutoffDate);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: count =>
            {
                count.Should().Be(1); // Only entry before cutoff
                _store.Count.Should().Be(2); // Cutoff and after remain
                return true;
            },
            Left: _ => false);
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
        StartedAtUtc = DateTimeOffset.UtcNow.AddSeconds(-1),
        CompletedAtUtc = DateTimeOffset.UtcNow,
        Metadata = new Dictionary<string, object?>()
    };

    #endregion
}
