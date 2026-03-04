#pragma warning disable CA1859 // Use concrete types when possible for improved performance

using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;

using LanguageExt;

namespace Encina.ContractTests.Compliance.BreachNotification;

/// <summary>
/// Contract tests verifying that <see cref="IBreachAuditStore"/> implementations follow the
/// expected behavioral contract for breach audit trail management.
/// </summary>
public abstract class BreachAuditStoreContractTestsBase
{
    /// <summary>
    /// Creates a new instance of the store being tested.
    /// </summary>
    protected abstract IBreachAuditStore CreateStore();

    #region Helpers

    /// <summary>
    /// Creates a <see cref="BreachAuditEntry"/> with sensible defaults for testing.
    /// </summary>
    protected static BreachAuditEntry CreateTestEntry(
        string? breachId = null,
        string? action = null,
        string? detail = null,
        string? performedByUserId = null)
    {
        return BreachAuditEntry.Create(
            breachId: breachId ?? $"breach-{Guid.NewGuid():N}",
            action: action ?? "BreachDetected",
            detail: detail,
            performedByUserId: performedByUserId);
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
        var entry = CreateTestEntry();

        var result = await store.RecordAsync(entry);

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region GetAuditTrailAsync Contract

    /// <summary>
    /// Contract: GetAuditTrailAsync for a breach with audit entries should return those entries.
    /// </summary>
    [Fact]
    public async Task Contract_GetAuditTrailAsync_ExistingBreachId_ReturnsEntries()
    {
        var store = CreateStore();
        var breachId = $"breach-{Guid.NewGuid():N}";
        var entry = CreateTestEntry(breachId: breachId, action: "BreachDetected", detail: "Detected by rule X");

        await store.RecordAsync(entry);

        var result = await store.GetAuditTrailAsync(breachId);

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Count.ShouldBe(1);
        list[0].BreachId.ShouldBe(breachId);
        list[0].Action.ShouldBe("BreachDetected");
    }

    /// <summary>
    /// Contract: GetAuditTrailAsync for a non-existing breach ID should return an empty list.
    /// </summary>
    [Fact]
    public async Task Contract_GetAuditTrailAsync_NonExistingBreachId_ReturnsEmptyList()
    {
        var store = CreateStore();

        var result = await store.GetAuditTrailAsync($"non-existing-{Guid.NewGuid():N}");

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Count.ShouldBe(0);
    }

    /// <summary>
    /// Contract: GetAuditTrailAsync with multiple entries for the same breach should return all of them.
    /// </summary>
    [Fact]
    public async Task Contract_GetAuditTrailAsync_MultipleEntries_ReturnsAll()
    {
        var store = CreateStore();
        var breachId = $"breach-{Guid.NewGuid():N}";
        var entry1 = CreateTestEntry(breachId: breachId, action: "BreachDetected");
        var entry2 = CreateTestEntry(breachId: breachId, action: "AuthorityNotified");
        var entry3 = CreateTestEntry(breachId: breachId, action: "SubjectsNotified");

        // Also record an entry for a different breach to verify filtering
        var otherEntry = CreateTestEntry(breachId: $"other-{Guid.NewGuid():N}", action: "BreachDetected");

        await store.RecordAsync(entry1);
        await store.RecordAsync(entry2);
        await store.RecordAsync(entry3);
        await store.RecordAsync(otherEntry);

        var result = await store.GetAuditTrailAsync(breachId);

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Count.ShouldBe(3);
        list.ShouldAllBe(e => e.BreachId == breachId);
    }

    /// <summary>
    /// Contract: GetAuditTrailAsync should return entries ordered by time descending (most recent first).
    /// </summary>
    [Fact]
    public async Task Contract_GetAuditTrailAsync_ReturnsEntriesOrderedByTimeDescending()
    {
        var store = CreateStore();
        var breachId = $"breach-{Guid.NewGuid():N}";

        // Create entries with controlled timestamps via the factory (OccurredAtUtc is set to UtcNow)
        var entry1 = CreateTestEntry(breachId: breachId, action: "BreachDetected");
        await Task.Delay(10); // Ensure distinct timestamps
        var entry2 = CreateTestEntry(breachId: breachId, action: "AuthorityNotified");
        await Task.Delay(10);
        var entry3 = CreateTestEntry(breachId: breachId, action: "BreachResolved");

        await store.RecordAsync(entry1);
        await store.RecordAsync(entry2);
        await store.RecordAsync(entry3);

        var result = await store.GetAuditTrailAsync(breachId);

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Count.ShouldBe(3);

        // Verify descending order (most recent first)
        for (var i = 0; i < list.Count - 1; i++)
        {
            list[i].OccurredAtUtc.ShouldBeGreaterThanOrEqualTo(list[i + 1].OccurredAtUtc);
        }
    }

    #endregion
}
