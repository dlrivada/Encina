#pragma warning disable CA1859 // Use concrete types when possible for improved performance
#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

namespace Encina.ContractTests.Compliance.DataResidency;

public abstract class ResidencyAuditStoreContractTestsBase
{
    protected abstract IResidencyAuditStore CreateStore();

    protected static ResidencyAuditEntry CreateEntry(
        string? entityId = null,
        string? dataCategory = null,
        ResidencyAction action = ResidencyAction.PolicyCheck,
        ResidencyOutcome outcome = ResidencyOutcome.Allowed)
    {
        return ResidencyAuditEntry.Create(
            dataCategory: dataCategory ?? "personal-data",
            sourceRegion: "DE",
            action: action,
            outcome: outcome,
            entityId: entityId);
    }

    [Fact]
    public async Task Contract_RecordAsync_ValidEntry_ShouldSucceed()
    {
        var store = CreateStore();
        var entry = CreateEntry();

        var result = await store.RecordAsync(entry);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_GetByEntityAsync_ShouldReturnMatchingEntries()
    {
        var store = CreateStore();
        var entityId = $"entity-{Guid.NewGuid():N}";
        await store.RecordAsync(CreateEntry(entityId: entityId));
        await store.RecordAsync(CreateEntry(entityId: entityId));
        await store.RecordAsync(CreateEntry(entityId: "other"));

        var result = await store.GetByEntityAsync(entityId);

        var entries = result.Match(Right: e => e, Left: _ => []);
        entries.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Contract_GetByDateRangeAsync_ShouldReturnEntriesInRange()
    {
        var store = CreateStore();
        await store.RecordAsync(CreateEntry());
        await store.RecordAsync(CreateEntry());

        var from = DateTimeOffset.UtcNow.AddMinutes(-1);
        var to = DateTimeOffset.UtcNow.AddMinutes(1);

        var result = await store.GetByDateRangeAsync(from, to);

        var entries = result.Match(Right: e => e, Left: _ => []);
        entries.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Contract_GetViolationsAsync_ShouldReturnOnlyBlocked()
    {
        var store = CreateStore();
        await store.RecordAsync(CreateEntry(outcome: ResidencyOutcome.Allowed));
        await store.RecordAsync(CreateEntry(outcome: ResidencyOutcome.Blocked));
        await store.RecordAsync(CreateEntry(outcome: ResidencyOutcome.Warning));
        await store.RecordAsync(CreateEntry(outcome: ResidencyOutcome.Blocked));

        var result = await store.GetViolationsAsync();

        var entries = result.Match(Right: e => e, Left: _ => []);
        entries.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Contract_GetByEntityAsync_NonExisting_ShouldReturnEmptyList()
    {
        var store = CreateStore();

        var result = await store.GetByEntityAsync("non-existing");

        result.IsRight.ShouldBeTrue();
        var entries = result.Match(Right: e => e, Left: _ => []);
        entries.ShouldBeEmpty();
    }
}
