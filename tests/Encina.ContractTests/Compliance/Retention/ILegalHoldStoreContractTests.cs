#pragma warning disable CA1859 // Use concrete types when possible for improved performance

using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;

using LanguageExt;

namespace Encina.ContractTests.Compliance.Retention;

/// <summary>
/// Contract tests verifying that <see cref="ILegalHoldStore"/> implementations follow the
/// expected behavioral contract for legal hold lifecycle management.
/// </summary>
public abstract class LegalHoldStoreContractTestsBase
{
    /// <summary>
    /// Creates a new instance of the store being tested.
    /// </summary>
    protected abstract ILegalHoldStore CreateStore();

    #region Helpers

    /// <summary>
    /// Creates a <see cref="LegalHold"/> with optional overrides for testing.
    /// </summary>
    protected static LegalHold CreateHold(
        string? entityId = null,
        string? reason = null,
        string? appliedByUserId = null)
    {
        return LegalHold.Create(
            entityId: entityId ?? $"entity-{Guid.NewGuid():N}",
            reason: reason ?? "Pending regulatory audit",
            appliedByUserId: appliedByUserId ?? "legal-counsel@company.com");
    }

    #endregion

    #region CreateAsync Contract

    /// <summary>
    /// Contract: CreateAsync with a valid hold should return Right (success).
    /// </summary>
    [Fact]
    public async Task Contract_CreateAsync_ValidHold_ReturnsRight()
    {
        var store = CreateStore();
        var hold = CreateHold();

        var result = await store.CreateAsync(hold);

        result.IsRight.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: CreateAsync with a duplicate hold ID should return Left (error).
    /// </summary>
    [Fact]
    public async Task Contract_CreateAsync_DuplicateId_ReturnsLeft()
    {
        var store = CreateStore();
        var hold = CreateHold();

        await store.CreateAsync(hold);
        var result = await store.CreateAsync(hold);

        result.IsLeft.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: CreateAsync should preserve all fields of the stored hold.
    /// </summary>
    [Fact]
    public async Task Contract_CreateAsync_PreservesAllFields()
    {
        var store = CreateStore();
        var hold = LegalHold.Create(
            entityId: "invoice-12345",
            reason: "Pending tax audit for fiscal year 2024",
            appliedByUserId: "legal@company.com");

        await store.CreateAsync(hold);

        var result = await store.GetByIdAsync(hold.Id);
        result.IsRight.ShouldBeTrue();

        var option = result.Match(Right: r => r, Left: _ => Option<LegalHold>.None);
        option.IsSome.ShouldBeTrue();

        var found = option.Match(Some: h => h, None: () => throw new InvalidOperationException("Expected Some but got None"));
        found.Id.ShouldBe(hold.Id);
        found.EntityId.ShouldBe("invoice-12345");
        found.Reason.ShouldBe("Pending tax audit for fiscal year 2024");
        found.AppliedByUserId.ShouldBe("legal@company.com");
        found.IsActive.ShouldBeTrue();
        found.ReleasedAtUtc.ShouldBeNull();
    }

    #endregion

    #region GetByIdAsync Contract

    /// <summary>
    /// Contract: GetByIdAsync for an existing hold ID should return Some.
    /// </summary>
    [Fact]
    public async Task Contract_GetByIdAsync_ExistingHold_ReturnsSome()
    {
        var store = CreateStore();
        var hold = CreateHold();
        await store.CreateAsync(hold);

        var result = await store.GetByIdAsync(hold.Id);

        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: r => r, Left: _ => Option<LegalHold>.None);
        option.IsSome.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: GetByIdAsync for a non-existing hold ID should return None.
    /// </summary>
    [Fact]
    public async Task Contract_GetByIdAsync_NonExistingHold_ReturnsNone()
    {
        var store = CreateStore();

        var result = await store.GetByIdAsync($"non-existing-{Guid.NewGuid():N}");

        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: r => r, Left: _ => Option<LegalHold>.None);
        option.IsNone.ShouldBeTrue();
    }

    #endregion

    #region GetByEntityIdAsync Contract

    /// <summary>
    /// Contract: GetByEntityIdAsync for an entity with holds should return all its holds.
    /// </summary>
    [Fact]
    public async Task Contract_GetByEntityIdAsync_ExistingEntity_ReturnsEntityHolds()
    {
        var store = CreateStore();
        var entityId = $"entity-{Guid.NewGuid():N}";
        var hold1 = CreateHold(entityId: entityId, reason: "Audit 2023");
        var hold2 = CreateHold(entityId: entityId, reason: "Litigation 2024");
        var otherHold = CreateHold(entityId: $"other-{Guid.NewGuid():N}");

        await store.CreateAsync(hold1);
        await store.CreateAsync(hold2);
        await store.CreateAsync(otherHold);

        var result = await store.GetByEntityIdAsync(entityId);

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Count.ShouldBe(2);
        list.ShouldAllBe(h => h.EntityId == entityId);
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

    #region IsUnderHoldAsync Contract

    /// <summary>
    /// Contract: IsUnderHoldAsync for an entity with an active hold should return true.
    /// </summary>
    [Fact]
    public async Task Contract_IsUnderHoldAsync_EntityWithActiveHold_ReturnsTrue()
    {
        var store = CreateStore();
        var entityId = $"entity-{Guid.NewGuid():N}";
        var hold = CreateHold(entityId: entityId);
        await store.CreateAsync(hold);

        var result = await store.IsUnderHoldAsync(entityId);

        result.IsRight.ShouldBeTrue();
        var isHeld = result.Match(Right: b => b, Left: _ => false);
        isHeld.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: IsUnderHoldAsync for an entity with a released hold should return false.
    /// </summary>
    [Fact]
    public async Task Contract_IsUnderHoldAsync_EntityWithReleasedHold_ReturnsFalse()
    {
        var store = CreateStore();
        var entityId = $"entity-{Guid.NewGuid():N}";
        var hold = CreateHold(entityId: entityId);
        await store.CreateAsync(hold);
        await store.ReleaseAsync(hold.Id, "legal@company.com", DateTimeOffset.UtcNow);

        var result = await store.IsUnderHoldAsync(entityId);

        result.IsRight.ShouldBeTrue();
        var isHeld = result.Match(Right: b => b, Left: _ => true);
        isHeld.ShouldBeFalse();
    }

    /// <summary>
    /// Contract: IsUnderHoldAsync for an entity with no holds should return false.
    /// </summary>
    [Fact]
    public async Task Contract_IsUnderHoldAsync_EntityWithNoHolds_ReturnsFalse()
    {
        var store = CreateStore();

        var result = await store.IsUnderHoldAsync($"entity-{Guid.NewGuid():N}");

        result.IsRight.ShouldBeTrue();
        var isHeld = result.Match(Right: b => b, Left: _ => true);
        isHeld.ShouldBeFalse();
    }

    #endregion

    #region GetActiveHoldsAsync Contract

    /// <summary>
    /// Contract: GetActiveHoldsAsync should return only holds where ReleasedAtUtc is null.
    /// </summary>
    [Fact]
    public async Task Contract_GetActiveHoldsAsync_MixedHolds_ReturnsOnlyActive()
    {
        var store = CreateStore();
        var activeHold = CreateHold();
        var holdToRelease = CreateHold();

        await store.CreateAsync(activeHold);
        await store.CreateAsync(holdToRelease);
        await store.ReleaseAsync(holdToRelease.Id, "legal@company.com", DateTimeOffset.UtcNow);

        var result = await store.GetActiveHoldsAsync();

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.ShouldContain(h => h.Id == activeHold.Id);
        list.ShouldNotContain(h => h.Id == holdToRelease.Id);
    }

    /// <summary>
    /// Contract: GetActiveHoldsAsync with no active holds should return an empty list.
    /// </summary>
    [Fact]
    public async Task Contract_GetActiveHoldsAsync_NoActiveHolds_ReturnsEmptyList()
    {
        var store = CreateStore();

        var result = await store.GetActiveHoldsAsync();

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Count.ShouldBe(0);
    }

    #endregion

    #region ReleaseAsync Contract

    /// <summary>
    /// Contract: ReleaseAsync for an existing active hold should return Right and mark the hold as released.
    /// </summary>
    [Fact]
    public async Task Contract_ReleaseAsync_ActiveHold_ReleasesHold()
    {
        var store = CreateStore();
        var hold = CreateHold();
        await store.CreateAsync(hold);

        var releasedAt = DateTimeOffset.UtcNow;
        var result = await store.ReleaseAsync(hold.Id, "legal@company.com", releasedAt);

        result.IsRight.ShouldBeTrue();

        var fetchResult = await store.GetByIdAsync(hold.Id);
        var option = fetchResult.Match(Right: r => r, Left: _ => Option<LegalHold>.None);
        option.IsSome.ShouldBeTrue();

        var released = option.Match(Some: h => h, None: () => throw new InvalidOperationException("Expected Some but got None"));
        released.IsActive.ShouldBeFalse();
        released.ReleasedAtUtc.ShouldNotBeNull();
        released.ReleasedByUserId.ShouldBe("legal@company.com");
    }

    /// <summary>
    /// Contract: ReleaseAsync for a non-existing hold ID should return Left (error).
    /// </summary>
    [Fact]
    public async Task Contract_ReleaseAsync_NonExistingHold_ReturnsLeft()
    {
        var store = CreateStore();

        var result = await store.ReleaseAsync(
            $"non-existing-{Guid.NewGuid():N}",
            "legal@company.com",
            DateTimeOffset.UtcNow);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetAllAsync Contract

    /// <summary>
    /// Contract: GetAllAsync should return all stored holds (active and released).
    /// </summary>
    [Fact]
    public async Task Contract_GetAllAsync_MultipleHolds_ReturnsAll()
    {
        var store = CreateStore();
        var hold1 = CreateHold();
        var hold2 = CreateHold();
        var hold3 = CreateHold();

        await store.CreateAsync(hold1);
        await store.CreateAsync(hold2);
        await store.CreateAsync(hold3);
        await store.ReleaseAsync(hold3.Id, null, DateTimeOffset.UtcNow);

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
}
