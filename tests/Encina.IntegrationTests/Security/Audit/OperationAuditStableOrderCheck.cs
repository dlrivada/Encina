using Encina.Security.Audit;
using Shouldly;

namespace Encina.IntegrationTests.Security.Audit;

/// <summary>
/// Shared regression check for #2135: an <see cref="IOperationAuditStore"/> orders entries totally (timestamp, then
/// the unique entry id), so walking pages over entries that share one <c>TimestampUtc</c> returns every entry exactly once.
/// Each provider's integration test class calls it against its real database.
/// </summary>
internal static class OperationAuditStableOrderCheck
{
    private const int EntryCount = 7;
    private const int PageSize = 3;

    /// <summary>
    /// Records entries that all share one timestamp and one correlation id, then proves that paged reads repeat nothing,
    /// drop nothing, equal the unpaged read, are repeatable, and that the ascending read is the exact reverse of the descending one.
    /// </summary>
    /// <param name="store">The store under test, backed by a real database.</param>
    /// <param name="afterRecord">Optional step that waits until the recorded entries are queryable (async projections).</param>
    public static async Task AssertPagedReadsAreStableAsync(IOperationAuditStore store, Func<Task>? afterRecord = null)
    {
        ArgumentNullException.ThrowIfNull(store);

        var entityType = $"StableOrder-{Guid.NewGuid():N}";
        var correlationId = Guid.NewGuid().ToString();
        var sharedTimestamp = new DateTime(2026, 10, 16, 9, 30, 0, DateTimeKind.Utc);
        var recordedIds = new System.Collections.Generic.HashSet<Guid>();

        for (var i = 0; i < EntryCount; i++)
        {
            var entry = new OperationAuditEntry
            {
                Id = Guid.NewGuid(),
                CorrelationId = correlationId,
                UserId = "stable-order-user",
                Action = "Create",
                EntityType = entityType,
                EntityId = $"order-{i}",
                Outcome = AuditOutcome.Success,
                TimestampUtc = sharedTimestamp,
                StartedAtUtc = new DateTimeOffset(sharedTimestamp, TimeSpan.Zero),
                CompletedAtUtc = new DateTimeOffset(sharedTimestamp, TimeSpan.Zero).AddMilliseconds(5)
            };

            (await store.RecordAsync(entry)).IsRight.ShouldBeTrue();
            recordedIds.Add(entry.Id);
        }

        if (afterRecord is not null)
        {
            await afterRecord();
        }

        var unpaged = await QueryIdsAsync(store, entityType, pageNumber: 1, pageSize: 100);
        unpaged.Count.ShouldBe(EntryCount);

        var firstWalk = await WalkPagesAsync(store, entityType);
        var secondWalk = await WalkPagesAsync(store, entityType);

        firstWalk.Count.ShouldBe(EntryCount, "no entry may be dropped across pages");
        firstWalk.Distinct().Count().ShouldBe(EntryCount, "no entry may be repeated across pages");
        firstWalk.ToHashSet().SetEquals(recordedIds).ShouldBeTrue();
        firstWalk.ShouldBe(unpaged, "the paged walk must equal the unpaged read");
        secondWalk.ShouldBe(firstWalk, "the order must be repeatable");

        var byCorrelation = (await store.GetByCorrelationIdAsync(correlationId))
            .Match(Right: entries => entries.Select(e => e.Id).ToList(), Left: error => throw new ShouldAssertException($"Query failed: {error.GetCode().IfNone("none")}"));
        byCorrelation.ShouldBe(unpaged.AsEnumerable().Reverse().ToList(), "ascending must be the exact reverse of descending");
    }

    private static async Task<List<Guid>> WalkPagesAsync(IOperationAuditStore store, string entityType)
    {
        var ids = new List<Guid>();
        for (var page = 1; page <= (EntryCount + PageSize - 1) / PageSize; page++)
        {
            ids.AddRange(await QueryIdsAsync(store, entityType, page, PageSize));
        }

        return ids;
    }

    private static async Task<List<Guid>> QueryIdsAsync(IOperationAuditStore store, string entityType, int pageNumber, int pageSize)
    {
        var result = await store.QueryAsync(new OperationAuditQuery
        {
            EntityType = entityType,
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        return result.Match(
            Right: page => page.Items.Select(e => e.Id).ToList(),
            Left: error => throw new ShouldAssertException($"Query failed: {error.GetCode().IfNone("none")}"));
    }
}
