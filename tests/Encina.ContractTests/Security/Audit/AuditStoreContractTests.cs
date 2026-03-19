using Encina.Security.Audit;
using LanguageExt;

namespace Encina.ContractTests.Security.Audit;

/// <summary>
/// Contract tests for <see cref="IAuditStore"/> to verify consistent behavior
/// across implementations. Uses <see cref="InMemoryAuditStore"/> as the reference implementation.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "Audit")]
public sealed class AuditStoreContractTests
{
    #region RecordAsync Contract

    [Fact]
    public async Task RecordAsync_ValidEntry_ReturnsRightUnit()
    {
        // Arrange
        var store = CreateStore();
        var entry = CreateEntry();

        // Act
        var result = await store.RecordAsync(entry);

        // Assert
        result.IsRight.ShouldBeTrue("RecordAsync must return Right(Unit) for a valid entry");
    }

    [Fact]
    public async Task RecordAsync_DuplicateId_DoesNotFail()
    {
        // Arrange
        var store = CreateStore();
        var entry = CreateEntry();

        await store.RecordAsync(entry);

        // Act - record same entry again (same Id)
        var result = await store.RecordAsync(entry);

        // Assert - should succeed (update or idempotent)
        result.IsRight.ShouldBeTrue(
            "RecordAsync must handle duplicate IDs gracefully (update or reject without error)");
    }

    #endregion

    #region GetByEntityAsync Contract

    [Fact]
    public async Task GetByEntityAsync_NonExistentEntity_ReturnsEmptyCollection()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.GetByEntityAsync("NonExistentType", "non-existent-id");

        // Assert
        result.IsRight.ShouldBeTrue(
            "GetByEntityAsync must return Right for non-existent entities");

        var entries = result.Match(
            Right: r => r,
            Left: _ => []);

        entries.Count.ShouldBe(0,
            "GetByEntityAsync must return an empty collection for non-existent entities, not an error");
    }

    [Fact]
    public async Task GetByEntityAsync_ExistingEntity_ReturnsMatchingEntries()
    {
        // Arrange
        var store = CreateStore();
        var entry = CreateEntry(entityType: "Product", entityId: "prod-123");
        await store.RecordAsync(entry);

        // Act
        var result = await store.GetByEntityAsync("Product", "prod-123");

        // Assert
        result.IsRight.ShouldBeTrue();

        var entries = result.Match(
            Right: r => r,
            Left: _ => []);

        entries.Count.ShouldBe(1);
        entries[0].Id.ShouldBe(entry.Id);
    }

    [Fact]
    public async Task GetByEntityAsync_CaseInsensitiveEntityType()
    {
        // Arrange
        var store = CreateStore();
        var entry = CreateEntry(entityType: "Order", entityId: "ord-1");
        await store.RecordAsync(entry);

        // Act
        var result = await store.GetByEntityAsync("order", "ord-1");

        // Assert
        result.IsRight.ShouldBeTrue();

        var entries = result.Match(
            Right: r => r,
            Left: _ => []);

        entries.Count.ShouldBe(1,
            "GetByEntityAsync must match entity types case-insensitively");
    }

    #endregion

    #region PurgeEntriesAsync Contract

    [Fact]
    public async Task PurgeEntriesAsync_EmptyStore_ReturnsRightZero()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.PurgeEntriesAsync(DateTime.UtcNow);

        // Assert
        result.IsRight.ShouldBeTrue(
            "PurgeEntriesAsync must return Right(0) for an empty store");

        var purgedCount = result.Match(
            Right: r => r,
            Left: _ => -1);

        purgedCount.ShouldBe(0,
            "PurgeEntriesAsync must return 0 when there are no entries to purge");
    }

    [Fact]
    public async Task PurgeEntriesAsync_WithOldEntries_ReturnsCorrectCount()
    {
        // Arrange
        var store = CreateStore();
        var threshold = DateTime.UtcNow;

        // Add 3 old entries and 2 new entries
        for (var i = 0; i < 3; i++)
        {
            await store.RecordAsync(CreateEntry(timestamp: threshold.AddHours(-(i + 1))));
        }

        for (var i = 0; i < 2; i++)
        {
            await store.RecordAsync(CreateEntry(timestamp: threshold.AddHours(i + 1)));
        }

        // Act
        var result = await store.PurgeEntriesAsync(threshold);

        // Assert
        result.IsRight.ShouldBeTrue();

        var purgedCount = result.Match(
            Right: r => r,
            Left: _ => -1);

        purgedCount.ShouldBe(3);
    }

    #endregion

    #region QueryAsync Contract

    [Fact]
    public async Task QueryAsync_EmptyStore_ReturnsEmptyPagedResult()
    {
        // Arrange
        var store = CreateStore();
        var query = new AuditQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await store.QueryAsync(query);

        // Assert
        result.IsRight.ShouldBeTrue(
            "QueryAsync must return Right for an empty store");

        var pagedResult = result.Match(
            Right: r => r,
            Left: _ => PagedResult<AuditEntry>.Empty());

        pagedResult.Items.Count.ShouldBe(0);
        pagedResult.TotalCount.ShouldBe(0);
        pagedResult.PageNumber.ShouldBe(1);
        pagedResult.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public async Task QueryAsync_WithEntries_ReturnsPaginatedResult()
    {
        // Arrange
        var store = CreateStore();
        for (var i = 0; i < 15; i++)
        {
            await store.RecordAsync(CreateEntry(timestamp: DateTime.UtcNow.AddSeconds(-i)));
        }

        var query = new AuditQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await store.QueryAsync(query);

        // Assert
        result.IsRight.ShouldBeTrue();

        var pagedResult = result.Match(
            Right: r => r,
            Left: _ => PagedResult<AuditEntry>.Empty());

        pagedResult.Items.Count.ShouldBe(10);
        pagedResult.TotalCount.ShouldBe(15);
        pagedResult.HasNextPage.ShouldBeTrue();
        pagedResult.TotalPages.ShouldBe(2);
    }

    #endregion

    #region GetByCorrelationIdAsync Contract

    [Fact]
    public async Task GetByCorrelationIdAsync_NonExistent_ReturnsEmptyCollection()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.GetByCorrelationIdAsync("non-existent-correlation-id");

        // Assert
        result.IsRight.ShouldBeTrue();

        var entries = result.Match(
            Right: r => r,
            Left: _ => []);

        entries.Count.ShouldBe(0);
    }

    #endregion

    #region Helpers

    private static InMemoryAuditStore CreateStore() => new();

    private static AuditEntry CreateEntry(
        string entityType = "Order",
        string? entityId = null,
        DateTime? timestamp = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new AuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid().ToString(),
            Action = "Create",
            EntityType = entityType,
            EntityId = entityId ?? Guid.NewGuid().ToString(),
            Outcome = AuditOutcome.Success,
            TimestampUtc = timestamp ?? DateTime.UtcNow,
            StartedAtUtc = now,
            CompletedAtUtc = now
        };
    }

    #endregion
}
