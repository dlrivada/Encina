using Encina.Security.Audit;
using Shouldly;

namespace Encina.UnitTests.Security.Audit.ReadAudit;

/// <summary>
/// Every <see cref="ReadAuditQuery"/> filter of <see cref="InMemoryReadAuditStore"/> selects the right entry
/// (#2135 moved the filter building into helpers).
/// </summary>
public class InMemoryReadAuditStoreFilterTests
{
    private static readonly DateTimeOffset First = new(2026, 10, 16, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task QueryAsync_EveryFilter_SelectsTheMatchingEntry()
    {
        var store = new InMemoryReadAuditStore();
        var a = Entry("ent-a", "user-a", "tenant-a", ReadAccessMethod.Repository, "Treatment review", "corr-a", First);
        var b = Entry("ent-b", "user-b", "tenant-b", ReadAccessMethod.Export, "Billing export", "corr-b", First.AddHours(2));
        await store.LogReadAsync(a);
        await store.LogReadAsync(b);

        (await IdsAsync(store, new ReadAuditQuery { EntityType = "patient" })).ToHashSet().SetEquals([a.Id, b.Id]).ShouldBeTrue();
        (await IdsAsync(store, new ReadAuditQuery { UserId = "user-a" })).ShouldBe([a.Id]);
        (await IdsAsync(store, new ReadAuditQuery { TenantId = "tenant-b" })).ShouldBe([b.Id]);
        (await IdsAsync(store, new ReadAuditQuery { EntityId = "ent-a" })).ShouldBe([a.Id]);
        (await IdsAsync(store, new ReadAuditQuery { CorrelationId = "corr-b" })).ShouldBe([b.Id]);
        (await IdsAsync(store, new ReadAuditQuery { AccessMethod = ReadAccessMethod.Export })).ShouldBe([b.Id]);
        (await IdsAsync(store, new ReadAuditQuery { Purpose = "treatment" })).ShouldBe([a.Id]);
        (await IdsAsync(store, new ReadAuditQuery { FromUtc = First.AddHours(1) })).ShouldBe([b.Id]);
        (await IdsAsync(store, new ReadAuditQuery { ToUtc = First.AddHours(1) })).ShouldBe([a.Id]);
        (await IdsAsync(store, new ReadAuditQuery { UserId = "user-b", AccessMethod = ReadAccessMethod.Repository })).ShouldBeEmpty();
    }

    private static ReadAuditEntry Entry(
        string entityId,
        string userId,
        string tenantId,
        ReadAccessMethod method,
        string purpose,
        string correlationId,
        DateTimeOffset at) => new()
        {
            Id = Guid.NewGuid(),
            EntityType = "Patient",
            EntityId = entityId,
            UserId = userId,
            TenantId = tenantId,
            AccessedAtUtc = at,
            AccessMethod = method,
            Purpose = purpose,
            CorrelationId = correlationId,
            EntityCount = 1
        };

    private static async Task<List<Guid>> IdsAsync(InMemoryReadAuditStore store, ReadAuditQuery query)
    {
        var result = await store.QueryAsync(query);
        return result.Match(Right: p => p.Items.Select(e => e.Id).ToList(), Left: _ => throw new ShouldAssertException("query failed"));
    }
}
