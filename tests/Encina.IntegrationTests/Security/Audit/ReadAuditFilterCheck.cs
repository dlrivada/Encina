using Encina.Security.Audit;
using Shouldly;

namespace Encina.IntegrationTests.Security.Audit;

/// <summary>
/// Shared check that every <see cref="ReadAuditQuery"/> filter is applied by a real <see cref="IReadAuditStore"/>
/// (#2135 changed the query shape of every read audit store; each filter must keep selecting the right rows).
/// </summary>
internal static class ReadAuditFilterCheck
{
    /// <summary>
    /// Records two entries that differ in every filterable field, then queries by each filter alone and by all of them together.
    /// </summary>
    /// <param name="store">The store under test, backed by a real database.</param>
    public static async Task AssertEveryFilterSelectsTheRightEntryAsync(IReadAuditStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        var entityType = $"FilterCheck{Guid.NewGuid():N}";
        var first = new DateTimeOffset(2026, 10, 16, 8, 0, 0, TimeSpan.Zero);
        var second = first.AddHours(2);
        var a = Entry(entityType, "ent-a", "user-a", "tenant-a", ReadAccessMethod.Repository, "Treatment review", "corr-a", first);
        var b = Entry(entityType, "ent-b", "user-b", "tenant-b", ReadAccessMethod.Export, "Billing export", "corr-b", second);

        (await store.LogReadAsync(a)).IsRight.ShouldBeTrue();
        (await store.LogReadAsync(b)).IsRight.ShouldBeTrue();

        (await IdsAsync(store, new ReadAuditQuery { EntityType = entityType })).ToHashSet().SetEquals([a.Id, b.Id]).ShouldBeTrue();
        (await IdsAsync(store, new ReadAuditQuery { EntityType = entityType, UserId = "user-a" })).ShouldBe([a.Id]);
        (await IdsAsync(store, new ReadAuditQuery { EntityType = entityType, TenantId = "tenant-b" })).ShouldBe([b.Id]);
        (await IdsAsync(store, new ReadAuditQuery { EntityType = entityType, EntityId = "ent-a" })).ShouldBe([a.Id]);
        (await IdsAsync(store, new ReadAuditQuery { EntityType = entityType, CorrelationId = "corr-b" })).ShouldBe([b.Id]);
        (await IdsAsync(store, new ReadAuditQuery { EntityType = entityType, AccessMethod = ReadAccessMethod.Export })).ShouldBe([b.Id]);
        (await IdsAsync(store, new ReadAuditQuery { EntityType = entityType, Purpose = "Treatment" })).ShouldBe([a.Id]);
        (await IdsAsync(store, new ReadAuditQuery { EntityType = entityType, FromUtc = first.AddHours(1) })).ShouldBe([b.Id]);
        (await IdsAsync(store, new ReadAuditQuery { EntityType = entityType, ToUtc = first.AddHours(1) })).ShouldBe([a.Id]);
        (await IdsAsync(store, new ReadAuditQuery
        {
            EntityType = entityType,
            UserId = "user-a",
            TenantId = "tenant-a",
            EntityId = "ent-a",
            CorrelationId = "corr-a",
            AccessMethod = ReadAccessMethod.Repository,
            Purpose = "Treatment",
            FromUtc = first.AddMinutes(-1),
            ToUtc = first.AddMinutes(1)
        })).ShouldBe([a.Id]);
        (await IdsAsync(store, new ReadAuditQuery { EntityType = entityType, UserId = "user-b", AccessMethod = ReadAccessMethod.Repository })).ShouldBeEmpty();
    }

    private static ReadAuditEntry Entry(
        string entityType,
        string entityId,
        string userId,
        string tenantId,
        ReadAccessMethod method,
        string purpose,
        string correlationId,
        DateTimeOffset at) => new()
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            TenantId = tenantId,
            AccessedAtUtc = at,
            AccessMethod = method,
            Purpose = purpose,
            CorrelationId = correlationId,
            EntityCount = 1
        };

    private static async Task<List<Guid>> IdsAsync(IReadAuditStore store, ReadAuditQuery query)
    {
        var result = await store.QueryAsync(query);
        return result.Match(
            Right: page => page.Items.Select(e => e.Id).ToList(),
            Left: error => throw new ShouldAssertException($"Query failed: {error.GetCode().IfNone("none")}"));
    }
}
