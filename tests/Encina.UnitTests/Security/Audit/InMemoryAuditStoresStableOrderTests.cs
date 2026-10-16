using Encina.DomainModeling.Auditing;
using Encina.Security.Audit;
using Shouldly;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Regression tests for #2135: the in-memory audit stores order entries that share a timestamp by their unique id,
/// so paged reads repeat nothing and drop nothing.
/// </summary>
public class InMemoryAuditStoresStableOrderTests
{
    private const int EntryCount = 7;
    private const int PageSize = 3;
    private static readonly DateTime SharedTimestamp = new(2026, 10, 16, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task OperationStore_QueryAsync_EntriesSharingATimestamp_PagesRepeatAndDropNothing()
    {
        var store = new InMemoryOperationAuditStore();
        var correlationId = Guid.NewGuid().ToString();
        var recorded = new List<Guid>();
        for (var i = 0; i < EntryCount; i++)
        {
            var entry = new OperationAuditEntry
            {
                Id = Guid.NewGuid(),
                CorrelationId = correlationId,
                UserId = "user",
                Action = "Create",
                EntityType = "Order",
                EntityId = $"order-{i}",
                Outcome = AuditOutcome.Success,
                TimestampUtc = SharedTimestamp,
                StartedAtUtc = new DateTimeOffset(SharedTimestamp, TimeSpan.Zero),
                CompletedAtUtc = new DateTimeOffset(SharedTimestamp, TimeSpan.Zero).AddMilliseconds(5)
            };
            (await store.RecordAsync(entry)).IsRight.ShouldBeTrue();
            recorded.Add(entry.Id);
        }

        var unpaged = await OperationIdsAsync(store, 1, 100);
        var walk = new List<Guid>();
        for (var page = 1; page <= 3; page++)
        {
            walk.AddRange(await OperationIdsAsync(store, page, PageSize));
        }

        unpaged.Count.ShouldBe(EntryCount);
        walk.Distinct().Count().ShouldBe(EntryCount);
        walk.ToHashSet().SetEquals(recorded).ShouldBeTrue();
        walk.ShouldBe(unpaged);

        var ascending = (await store.GetByCorrelationIdAsync(correlationId))
            .Match(Right: e => e.Select(x => x.Id).ToList(), Left: _ => throw new ShouldAssertException("query failed"));
        ascending.ShouldBe(unpaged.AsEnumerable().Reverse().ToList());
    }

    [Fact]
    public async Task ReadStore_QueryAsync_EntriesSharingATimestamp_PagesRepeatAndDropNothing()
    {
        var store = new InMemoryReadAuditStore();
        var recorded = new List<Guid>();
        for (var i = 0; i < EntryCount; i++)
        {
            var entry = new ReadAuditEntry
            {
                Id = Guid.NewGuid(),
                EntityType = "Patient",
                EntityId = "PAT-1",
                UserId = "user",
                AccessedAtUtc = new DateTimeOffset(SharedTimestamp, TimeSpan.Zero),
                AccessMethod = ReadAccessMethod.Repository,
                EntityCount = 1
            };
            (await store.LogReadAsync(entry)).IsRight.ShouldBeTrue();
            recorded.Add(entry.Id);
        }

        var unpaged = await ReadIdsAsync(store, 1, 100);
        var walk = new List<Guid>();
        for (var page = 1; page <= 3; page++)
        {
            walk.AddRange(await ReadIdsAsync(store, page, PageSize));
        }

        unpaged.Count.ShouldBe(EntryCount);
        walk.Distinct().Count().ShouldBe(EntryCount);
        walk.ToHashSet().SetEquals(recorded).ShouldBeTrue();
        walk.ShouldBe(unpaged);

        var history = (await store.GetAccessHistoryAsync("Patient", "PAT-1"))
            .Match(Right: e => e.Select(x => x.Id).ToList(), Left: _ => throw new ShouldAssertException("query failed"));
        history.ShouldBe(unpaged);
        unpaged.ShouldBe(recorded.OrderByDescending(id => id).ToList());
    }

    [Fact]
    public async Task EntityChangeStore_GetHistoryAsync_EntriesSharingATimestamp_ReturnAStableOrder()
    {
        var store = new InMemoryAuditLogStore();
        for (var i = 0; i < EntryCount; i++)
        {
            await store.LogAsync(new AuditLogEntry(
                Id: Guid.NewGuid().ToString(),
                EntityType: "Order",
                EntityId: "order-1",
                Action: AuditAction.Updated,
                UserId: "user",
                TimestampUtc: SharedTimestamp,
                OldValues: "{}",
                NewValues: "{}",
                CorrelationId: null));
        }

        var first = (await store.GetHistoryAsync("Order", "order-1")).Select(e => e.Id).ToList();
        var second = (await store.GetHistoryAsync("Order", "order-1")).Select(e => e.Id).ToList();

        first.Count.ShouldBe(EntryCount);
        first.ShouldBe(first.OrderByDescending(id => id, StringComparer.Ordinal).ToList());
        second.ShouldBe(first);
    }

    private static async Task<List<Guid>> OperationIdsAsync(InMemoryOperationAuditStore store, int pageNumber, int pageSize)
    {
        var result = await store.QueryAsync(new OperationAuditQuery { EntityType = "Order", PageNumber = pageNumber, PageSize = pageSize });
        return result.Match(Right: p => p.Items.Select(e => e.Id).ToList(), Left: _ => throw new ShouldAssertException("query failed"));
    }

    private static async Task<List<Guid>> ReadIdsAsync(InMemoryReadAuditStore store, int pageNumber, int pageSize)
    {
        var result = await store.QueryAsync(new ReadAuditQuery { EntityType = "Patient", PageNumber = pageNumber, PageSize = pageSize });
        return result.Match(Right: p => p.Items.Select(e => e.Id).ToList(), Left: _ => throw new ShouldAssertException("query failed"));
    }
}
