#pragma warning disable CA2012 // ValueTask should not be awaited multiple times - used via .AsTask().Result in sync property tests

using Encina.Security.Audit;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Security.Audit;

/// <summary>
/// Property-based tests for <see cref="IAuditStore"/> invariants.
/// Uses <see cref="InMemoryAuditStore"/> as the implementation under test.
/// </summary>
[Trait("Category", "Property")]
[Trait("Feature", "Audit")]
public sealed class AuditStorePropertyTests
{
    #region PurgeAsync Invariants

    [Property(MaxTest = 50)]
    public bool PurgeAsync_NeverDeletes_EntriesNewerThanThreshold(PositiveInt entryCount)
    {
        // Arrange
        var count = Math.Min(entryCount.Get, 50);
        var store = new InMemoryAuditStore();
        var threshold = DateTime.UtcNow;
        var newerEntries = new List<AuditEntry>();

        // Insert entries with timestamps both before and after the threshold
        for (var i = 0; i < count; i++)
        {
            var isNewer = i % 2 == 0;
            var timestamp = isNewer
                ? threshold.AddMinutes(i + 1)
                : threshold.AddMinutes(-(i + 1));

            var entry = CreateEntry(
                entityId: $"entity-{i}",
                timestamp: timestamp);

            _ = store.RecordAsync(entry).AsTask().Result;

            if (isNewer)
            {
                newerEntries.Add(entry);
            }
        }

        // Act
        var purgeResult = store.PurgeEntriesAsync(threshold).AsTask().Result;
        if (!purgeResult.IsRight) return false;

        // Assert: all newer entries must still be present
        var remaining = store.GetAllEntries();
        return newerEntries.All(newer =>
            remaining.Any(r => r.Id == newer.Id));
    }

    #endregion

    #region QueryAsync Pagination Invariants

    [Property(MaxTest = 30)]
    public bool QueryAsync_Pagination_NeverReturnsDuplicates(PositiveInt entryCount)
    {
        // Arrange
        var count = Math.Clamp(entryCount.Get, 1, 100);
        var store = new InMemoryAuditStore();
        const int pageSize = 7; // Use an odd page size to test uneven pagination

        for (var i = 0; i < count; i++)
        {
            var entry = CreateEntry(
                entityType: "Order",
                entityId: $"ord-{i}",
                timestamp: DateTime.UtcNow.AddSeconds(-i));

            _ = store.RecordAsync(entry).AsTask().Result;
        }

        // Act: paginate through all pages
        var allRetrievedIds = new System.Collections.Generic.HashSet<Guid>();
        var totalPages = (int)Math.Ceiling((double)count / pageSize);

        for (var page = 1; page <= totalPages + 1; page++)
        {
            var query = new AuditQuery
            {
                EntityType = "Order",
                PageNumber = page,
                PageSize = pageSize
            };

            var result = store.QueryAsync(query).AsTask().Result;
            if (!result.IsRight) return false;

            var pagedResult = result.Match(
                Right: r => r,
                Left: _ => PagedResult<AuditEntry>.Empty());

            foreach (var item in pagedResult.Items)
            {
                // If Add returns false, we found a duplicate
                if (!allRetrievedIds.Add(item.Id))
                {
                    return false;
                }
            }
        }

        // Assert: we retrieved exactly the expected number of unique entries
        return allRetrievedIds.Count == count;
    }

    #endregion

    #region RecordAsync then GetByEntityAsync Invariants

    [Property(MaxTest = 50)]
    public bool RecordAsync_ThenGetByEntity_AlwaysFindsEntry(NonEmptyString entityId)
    {
        // Arrange
        var store = new InMemoryAuditStore();
        var sanitizedEntityId = entityId.Get.Trim();
        if (string.IsNullOrWhiteSpace(sanitizedEntityId)) return true; // skip degenerate input

        var entry = CreateEntry(
            entityType: "Customer",
            entityId: sanitizedEntityId);

        // Act
        var recordResult = store.RecordAsync(entry).AsTask().Result;
        if (!recordResult.IsRight) return false;

        var getResult = store.GetByEntityAsync("Customer", sanitizedEntityId).AsTask().Result;
        if (!getResult.IsRight) return false;

        var entries = getResult.Match(
            Right: r => r,
            Left: _ => []);

        // Assert: the recorded entry must be found
        return entries.Any(e => e.Id == entry.Id);
    }

    #endregion

    #region Helpers

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
