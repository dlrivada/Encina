#pragma warning disable CA1859 // Use concrete types when possible for improved performance

using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;

using LanguageExt;

namespace Encina.ContractTests.Compliance.Retention;

/// <summary>
/// Contract tests verifying that <see cref="IRetentionRecordStore"/> implementations follow the
/// expected behavioral contract for retention record lifecycle management.
/// </summary>
public abstract class RetentionRecordStoreContractTestsBase
{
    /// <summary>
    /// Creates a new instance of the store being tested.
    /// </summary>
    protected abstract IRetentionRecordStore CreateStore();

    #region Helpers

    /// <summary>
    /// Creates a <see cref="RetentionRecord"/> with optional overrides for testing.
    /// </summary>
    protected static RetentionRecord CreateRecord(
        string? entityId = null,
        string? dataCategory = null,
        DateTimeOffset? createdAtUtc = null,
        DateTimeOffset? expiresAtUtc = null,
        string? policyId = null)
    {
        return RetentionRecord.Create(
            entityId: entityId ?? $"entity-{Guid.NewGuid():N}",
            dataCategory: dataCategory ?? "test-category",
            createdAtUtc: createdAtUtc ?? DateTimeOffset.UtcNow,
            expiresAtUtc: expiresAtUtc ?? DateTimeOffset.UtcNow.AddDays(30),
            policyId: policyId);
    }

    #endregion

    #region CreateAsync Contract

    /// <summary>
    /// Contract: CreateAsync with a valid record should return Right (success).
    /// </summary>
    [Fact]
    public async Task Contract_CreateAsync_ValidRecord_ReturnsRight()
    {
        var store = CreateStore();
        var record = CreateRecord();

        var result = await store.CreateAsync(record);

        result.IsRight.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: CreateAsync with a duplicate record ID should return Left (error).
    /// </summary>
    [Fact]
    public async Task Contract_CreateAsync_DuplicateId_ReturnsLeft()
    {
        var store = CreateStore();
        var record = CreateRecord();

        await store.CreateAsync(record);
        var result = await store.CreateAsync(record);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetByIdAsync Contract

    /// <summary>
    /// Contract: GetByIdAsync for an existing record ID should return Some.
    /// </summary>
    [Fact]
    public async Task Contract_GetByIdAsync_ExistingRecord_ReturnsSome()
    {
        var store = CreateStore();
        var record = CreateRecord();
        await store.CreateAsync(record);

        var result = await store.GetByIdAsync(record.Id);

        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: r => r, Left: _ => Option<RetentionRecord>.None);
        option.IsSome.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: GetByIdAsync for a non-existing record ID should return None.
    /// </summary>
    [Fact]
    public async Task Contract_GetByIdAsync_NonExistingRecord_ReturnsNone()
    {
        var store = CreateStore();

        var result = await store.GetByIdAsync($"non-existing-{Guid.NewGuid():N}");

        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: r => r, Left: _ => Option<RetentionRecord>.None);
        option.IsNone.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: GetByIdAsync should preserve all fields of the stored record.
    /// </summary>
    [Fact]
    public async Task Contract_GetByIdAsync_ExistingRecord_PreservesAllFields()
    {
        var store = CreateStore();
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddDays(365);
        var record = RetentionRecord.Create(
            entityId: "entity-preserve-test",
            dataCategory: "financial-records",
            createdAtUtc: now,
            expiresAtUtc: expires,
            policyId: "policy-001");

        await store.CreateAsync(record);

        var result = await store.GetByIdAsync(record.Id);
        result.IsRight.ShouldBeTrue();

        var option = result.Match(Right: r => r, Left: _ => Option<RetentionRecord>.None);
        option.IsSome.ShouldBeTrue();

        var found = option.Match(Some: r => r, None: () => throw new InvalidOperationException("Expected Some but got None"));
        found.Id.ShouldBe(record.Id);
        found.EntityId.ShouldBe("entity-preserve-test");
        found.DataCategory.ShouldBe("financial-records");
        found.PolicyId.ShouldBe("policy-001");
        found.Status.ShouldBe(RetentionStatus.Active);
    }

    #endregion

    #region GetByEntityIdAsync Contract

    /// <summary>
    /// Contract: GetByEntityIdAsync for an existing entity should return matching records.
    /// </summary>
    [Fact]
    public async Task Contract_GetByEntityIdAsync_ExistingEntity_ReturnsMatchingRecords()
    {
        var store = CreateStore();
        var entityId = $"entity-{Guid.NewGuid():N}";
        var record1 = CreateRecord(entityId: entityId, dataCategory: "category-a");
        var record2 = CreateRecord(entityId: entityId, dataCategory: "category-b");
        var otherRecord = CreateRecord(entityId: $"other-{Guid.NewGuid():N}");

        await store.CreateAsync(record1);
        await store.CreateAsync(record2);
        await store.CreateAsync(otherRecord);

        var result = await store.GetByEntityIdAsync(entityId);

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Count.ShouldBe(2);
        list.ShouldAllBe(r => r.EntityId == entityId);
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
    /// Contract: GetAllAsync should return all stored records.
    /// </summary>
    [Fact]
    public async Task Contract_GetAllAsync_MultipleRecords_ReturnsAll()
    {
        var store = CreateStore();
        var record1 = CreateRecord();
        var record2 = CreateRecord();
        var record3 = CreateRecord();

        await store.CreateAsync(record1);
        await store.CreateAsync(record2);
        await store.CreateAsync(record3);

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

    #endregion

    #region UpdateStatusAsync Contract

    /// <summary>
    /// Contract: UpdateStatusAsync for an existing record should update the status.
    /// </summary>
    [Fact]
    public async Task Contract_UpdateStatusAsync_ExistingRecord_UpdatesStatus()
    {
        var store = CreateStore();
        var record = CreateRecord();
        await store.CreateAsync(record);

        var result = await store.UpdateStatusAsync(record.Id, RetentionStatus.Expired);

        result.IsRight.ShouldBeTrue();

        var fetchResult = await store.GetByIdAsync(record.Id);
        var option = fetchResult.Match(Right: r => r, Left: _ => Option<RetentionRecord>.None);
        option.IsSome.ShouldBeTrue();

        var updated = option.Match(Some: r => r, None: () => throw new InvalidOperationException("Expected Some but got None"));
        updated.Status.ShouldBe(RetentionStatus.Expired);
    }

    /// <summary>
    /// Contract: UpdateStatusAsync for a non-existing record should return Left (error).
    /// </summary>
    [Fact]
    public async Task Contract_UpdateStatusAsync_NonExistingRecord_ReturnsLeft()
    {
        var store = CreateStore();

        var result = await store.UpdateStatusAsync($"non-existing-{Guid.NewGuid():N}", RetentionStatus.Expired);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetExpiredRecordsAsync Contract

    /// <summary>
    /// Contract: GetExpiredRecordsAsync should return only active records with past expiration.
    /// </summary>
    [Fact]
    public async Task Contract_GetExpiredRecordsAsync_WithExpiredActiveRecords_ReturnsExpiredOnly()
    {
        var store = CreateStore();
        var pastDate = DateTimeOffset.UtcNow.AddDays(-10);
        var futureDate = DateTimeOffset.UtcNow.AddDays(30);

        var expiredRecord = RetentionRecord.Create(
            entityId: $"entity-{Guid.NewGuid():N}",
            dataCategory: "test-category",
            createdAtUtc: pastDate.AddDays(-20),
            expiresAtUtc: pastDate);

        var activeRecord = RetentionRecord.Create(
            entityId: $"entity-{Guid.NewGuid():N}",
            dataCategory: "test-category",
            createdAtUtc: DateTimeOffset.UtcNow,
            expiresAtUtc: futureDate);

        await store.CreateAsync(expiredRecord);
        await store.CreateAsync(activeRecord);

        var result = await store.GetExpiredRecordsAsync();

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.ShouldContain(r => r.Id == expiredRecord.Id);
        list.ShouldNotContain(r => r.Id == activeRecord.Id);
    }

    #endregion

    #region GetExpiringWithinAsync Contract

    /// <summary>
    /// Contract: GetExpiringWithinAsync should return records expiring within the given window.
    /// </summary>
    [Fact]
    public async Task Contract_GetExpiringWithinAsync_RecordsInWindow_ReturnsMatching()
    {
        var store = CreateStore();
        var soonRecord = RetentionRecord.Create(
            entityId: $"entity-{Guid.NewGuid():N}",
            dataCategory: "test-category",
            createdAtUtc: DateTimeOffset.UtcNow,
            expiresAtUtc: DateTimeOffset.UtcNow.AddDays(5));

        var farRecord = RetentionRecord.Create(
            entityId: $"entity-{Guid.NewGuid():N}",
            dataCategory: "test-category",
            createdAtUtc: DateTimeOffset.UtcNow,
            expiresAtUtc: DateTimeOffset.UtcNow.AddDays(60));

        await store.CreateAsync(soonRecord);
        await store.CreateAsync(farRecord);

        var result = await store.GetExpiringWithinAsync(TimeSpan.FromDays(10));

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.ShouldContain(r => r.Id == soonRecord.Id);
        list.ShouldNotContain(r => r.Id == farRecord.Id);
    }

    #endregion
}
