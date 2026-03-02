#pragma warning disable CA1859 // Use concrete types when possible for improved performance

using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;

using LanguageExt;

namespace Encina.ContractTests.Compliance.Retention;

/// <summary>
/// Contract tests verifying that <see cref="IRetentionAuditStore"/> implementations follow the
/// expected behavioral contract for retention audit trail management.
/// </summary>
public abstract class RetentionAuditStoreContractTestsBase
{
    /// <summary>
    /// Creates a new instance of the store being tested.
    /// </summary>
    protected abstract IRetentionAuditStore CreateStore();

    #region Helpers

    /// <summary>
    /// Creates a <see cref="RetentionAuditEntry"/> with optional overrides for testing.
    /// </summary>
    protected static RetentionAuditEntry CreateEntry(
        string? action = null,
        string? entityId = null,
        string? dataCategory = null,
        string? detail = null,
        string? performedByUserId = null)
    {
        return RetentionAuditEntry.Create(
            action: action ?? "RecordTracked",
            entityId: entityId ?? $"entity-{Guid.NewGuid():N}",
            dataCategory: dataCategory ?? "test-category",
            detail: detail,
            performedByUserId: performedByUserId ?? "system");
    }

    #endregion

    #region RecordAsync Contract

    /// <summary>
    /// Contract: RecordAsync with a valid entry should return Right (success).
    /// </summary>
    [Fact]
    public async Task Contract_RecordAsync_ValidEntry_ReturnsRight()
    {
        var store = CreateStore();
        var entry = CreateEntry();

        var result = await store.RecordAsync(entry);

        result.IsRight.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: RecordAsync should persist all fields of the entry.
    /// </summary>
    [Fact]
    public async Task Contract_RecordAsync_PreservesAllFields()
    {
        var store = CreateStore();
        var entityId = $"entity-{Guid.NewGuid():N}";
        var entry = RetentionAuditEntry.Create(
            action: "DataDeleted",
            entityId: entityId,
            dataCategory: "financial-records",
            detail: "Retention period expired (7 years), auto-deleted by enforcement service",
            performedByUserId: "enforcement-service");

        await store.RecordAsync(entry);

        var result = await store.GetByEntityIdAsync(entityId);
        result.IsRight.ShouldBeTrue();

        var list = result.Match(Right: l => l, Left: _ => []);
        list.Count.ShouldBe(1);

        var found = list[0];
        found.Id.ShouldBe(entry.Id);
        found.Action.ShouldBe("DataDeleted");
        found.EntityId.ShouldBe(entityId);
        found.DataCategory.ShouldBe("financial-records");
        found.Detail.ShouldBe("Retention period expired (7 years), auto-deleted by enforcement service");
        found.PerformedByUserId.ShouldBe("enforcement-service");
    }

    /// <summary>
    /// Contract: RecordAsync should allow multiple entries with the same entity ID.
    /// </summary>
    [Fact]
    public async Task Contract_RecordAsync_MultipleEntriesSameEntity_AllPersisted()
    {
        var store = CreateStore();
        var entityId = $"entity-{Guid.NewGuid():N}";
        var entry1 = CreateEntry(action: "RecordTracked", entityId: entityId);
        var entry2 = CreateEntry(action: "EnforcementExecuted", entityId: entityId);
        var entry3 = CreateEntry(action: "DataDeleted", entityId: entityId);

        await store.RecordAsync(entry1);
        await store.RecordAsync(entry2);
        await store.RecordAsync(entry3);

        var result = await store.GetByEntityIdAsync(entityId);
        result.IsRight.ShouldBeTrue();

        var list = result.Match(Right: l => l, Left: _ => []);
        list.Count.ShouldBe(3);
    }

    #endregion

    #region GetByEntityIdAsync Contract

    /// <summary>
    /// Contract: GetByEntityIdAsync for an entity with audit entries should return only its entries.
    /// </summary>
    [Fact]
    public async Task Contract_GetByEntityIdAsync_EntityWithEntries_ReturnsEntityEntries()
    {
        var store = CreateStore();
        var entityId = $"entity-{Guid.NewGuid():N}";
        var entry1 = CreateEntry(entityId: entityId, action: "RecordTracked");
        var entry2 = CreateEntry(entityId: entityId, action: "DataDeleted");
        var otherEntry = CreateEntry(entityId: $"other-{Guid.NewGuid():N}");

        await store.RecordAsync(entry1);
        await store.RecordAsync(entry2);
        await store.RecordAsync(otherEntry);

        var result = await store.GetByEntityIdAsync(entityId);

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Count.ShouldBe(2);
        list.ShouldAllBe(e => e.EntityId == entityId);
    }

    /// <summary>
    /// Contract: GetByEntityIdAsync for a non-existing entity should return an empty list.
    /// </summary>
    [Fact]
    public async Task Contract_GetByEntityIdAsync_NonExistingEntity_ReturnsEmptyList()
    {
        var store = CreateStore();

        var result = await store.GetByEntityIdAsync($"non-existing-{Guid.NewGuid():N}");

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Count.ShouldBe(0);
    }

    #endregion

    #region GetAllAsync Contract

    /// <summary>
    /// Contract: GetAllAsync should return all stored audit entries.
    /// </summary>
    [Fact]
    public async Task Contract_GetAllAsync_MultipleEntries_ReturnsAll()
    {
        var store = CreateStore();
        var entry1 = CreateEntry(action: "PolicyCreated");
        var entry2 = CreateEntry(action: "RecordTracked");
        var entry3 = CreateEntry(action: "DataDeleted");

        await store.RecordAsync(entry1);
        await store.RecordAsync(entry2);
        await store.RecordAsync(entry3);

        var result = await store.GetAllAsync();

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Count.ShouldBe(3);
    }

    /// <summary>
    /// Contract: GetAllAsync on an empty store should return an empty list.
    /// </summary>
    [Fact]
    public async Task Contract_GetAllAsync_EmptyStore_ReturnsEmptyList()
    {
        var store = CreateStore();

        var result = await store.GetAllAsync();

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Count.ShouldBe(0);
    }

    /// <summary>
    /// Contract: GetAllAsync should return entries across different entities.
    /// </summary>
    [Fact]
    public async Task Contract_GetAllAsync_EntriesAcrossEntities_ReturnsAll()
    {
        var store = CreateStore();
        var entry1 = CreateEntry(entityId: $"entity-{Guid.NewGuid():N}");
        var entry2 = CreateEntry(entityId: $"entity-{Guid.NewGuid():N}");
        var entry3 = CreateEntry(entityId: null);

        await store.RecordAsync(entry1);
        await store.RecordAsync(entry2);
        await store.RecordAsync(entry3);

        var result = await store.GetAllAsync();

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Count.ShouldBe(3);
    }

    #endregion
}
