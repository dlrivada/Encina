using Encina.Security.Audit;
using Shouldly;

namespace Encina.IntegrationTests.Security.Audit;

/// <summary>
/// Shared regression check for #2135 on <see cref="IReadAuditStore"/>: entries are ordered totally (access time, then
/// the unique entry id), so walking pages over entries that share one <c>AccessedAtUtc</c> returns every entry exactly once.
/// </summary>
internal static class ReadAuditStableOrderCheck
{
    private const int EntryCount = 7;
    private const int PageSize = 3;

    /// <summary>
    /// Records entries that all share one access time and one entity, then proves that paged reads repeat nothing,
    /// drop nothing, equal the unpaged read and the entity access history, and are repeatable.
    /// </summary>
    /// <param name="store">The store under test, backed by a real database.</param>
    public static async Task AssertPagedReadsAreStableAsync(IReadAuditStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        var entityType = $"StableOrder{Guid.NewGuid():N}";
        const string entityId = "stable-order-entity";
        var sharedInstant = new DateTimeOffset(2026, 10, 16, 9, 30, 0, TimeSpan.Zero);
        var recordedIds = new System.Collections.Generic.HashSet<Guid>();

        for (var i = 0; i < EntryCount; i++)
        {
            var entry = new ReadAuditEntry
            {
                Id = Guid.NewGuid(),
                EntityType = entityType,
                EntityId = entityId,
                UserId = "stable-order-user",
                AccessedAtUtc = sharedInstant,
                AccessMethod = ReadAccessMethod.Repository,
                EntityCount = 1
            };

            (await store.LogReadAsync(entry)).IsRight.ShouldBeTrue();
            recordedIds.Add(entry.Id);
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

        var history = (await store.GetAccessHistoryAsync(entityType, entityId))
            .Match(Right: entries => entries.Select(e => e.Id).ToList(), Left: error => throw new ShouldAssertException($"Query failed: {error.GetCode().IfNone("none")}"));
        history.ShouldBe(unpaged, "the entity access history must use the same total order");
    }

    private static async Task<List<Guid>> WalkPagesAsync(IReadAuditStore store, string entityType)
    {
        var ids = new List<Guid>();
        for (var page = 1; page <= (EntryCount + PageSize - 1) / PageSize; page++)
        {
            ids.AddRange(await QueryIdsAsync(store, entityType, page, PageSize));
        }

        return ids;
    }

    private static async Task<List<Guid>> QueryIdsAsync(IReadAuditStore store, string entityType, int pageNumber, int pageSize)
    {
        var result = await store.QueryAsync(new ReadAuditQuery
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
