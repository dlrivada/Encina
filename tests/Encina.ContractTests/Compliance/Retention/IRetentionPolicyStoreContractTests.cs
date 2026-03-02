#pragma warning disable CA1859 // Use concrete types when possible for improved performance

using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;

using LanguageExt;

namespace Encina.ContractTests.Compliance.Retention;

/// <summary>
/// Contract tests verifying that <see cref="IRetentionPolicyStore"/> implementations follow the
/// expected behavioral contract for retention policy lifecycle management.
/// </summary>
public abstract class RetentionPolicyStoreContractTestsBase
{
    /// <summary>
    /// Creates a new instance of the store being tested.
    /// </summary>
    protected abstract IRetentionPolicyStore CreateStore();

    #region Helpers

    /// <summary>
    /// Creates a <see cref="RetentionPolicy"/> with optional overrides for testing.
    /// </summary>
    protected static RetentionPolicy CreatePolicy(
        string? dataCategory = null,
        TimeSpan? retentionPeriod = null,
        bool autoDelete = true,
        string? reason = null,
        string? legalBasis = null)
    {
        return RetentionPolicy.Create(
            dataCategory: dataCategory ?? $"category-{Guid.NewGuid():N}",
            retentionPeriod: retentionPeriod ?? RetentionPolicy.FromYears(7),
            autoDelete: autoDelete,
            reason: reason,
            legalBasis: legalBasis);
    }

    #endregion

    #region CreateAsync Contract

    /// <summary>
    /// Contract: CreateAsync with a valid policy should return Right (success).
    /// </summary>
    [Fact]
    public async Task Contract_CreateAsync_ValidPolicy_ReturnsRight()
    {
        var store = CreateStore();
        var policy = CreatePolicy();

        var result = await store.CreateAsync(policy);

        result.IsRight.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: CreateAsync with a duplicate policy ID should return Left (error).
    /// </summary>
    [Fact]
    public async Task Contract_CreateAsync_DuplicateId_ReturnsLeft()
    {
        var store = CreateStore();
        var policy = CreatePolicy();

        await store.CreateAsync(policy);
        var result = await store.CreateAsync(policy);

        result.IsLeft.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: CreateAsync should preserve all fields when the policy is retrieved.
    /// </summary>
    [Fact]
    public async Task Contract_CreateAsync_PreservesAllFields()
    {
        var store = CreateStore();
        var policy = RetentionPolicy.Create(
            dataCategory: "financial-records",
            retentionPeriod: RetentionPolicy.FromYears(7),
            autoDelete: true,
            reason: "German tax law (AO section 147)",
            legalBasis: "Legal obligation (Art. 6(1)(c))");

        await store.CreateAsync(policy);

        var result = await store.GetByIdAsync(policy.Id);
        result.IsRight.ShouldBeTrue();

        var option = result.Match(Right: r => r, Left: _ => Option<RetentionPolicy>.None);
        option.IsSome.ShouldBeTrue();

        var found = option.Match(Some: p => p, None: () => throw new InvalidOperationException("Expected Some but got None"));
        found.Id.ShouldBe(policy.Id);
        found.DataCategory.ShouldBe("financial-records");
        found.AutoDelete.ShouldBeTrue();
        found.Reason.ShouldBe("German tax law (AO section 147)");
        found.LegalBasis.ShouldBe("Legal obligation (Art. 6(1)(c))");
    }

    #endregion

    #region GetByIdAsync Contract

    /// <summary>
    /// Contract: GetByIdAsync for an existing policy ID should return Some.
    /// </summary>
    [Fact]
    public async Task Contract_GetByIdAsync_ExistingPolicy_ReturnsSome()
    {
        var store = CreateStore();
        var policy = CreatePolicy();
        await store.CreateAsync(policy);

        var result = await store.GetByIdAsync(policy.Id);

        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: r => r, Left: _ => Option<RetentionPolicy>.None);
        option.IsSome.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: GetByIdAsync for a non-existing policy ID should return None.
    /// </summary>
    [Fact]
    public async Task Contract_GetByIdAsync_NonExistingPolicy_ReturnsNone()
    {
        var store = CreateStore();

        var result = await store.GetByIdAsync($"non-existing-{Guid.NewGuid():N}");

        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: r => r, Left: _ => Option<RetentionPolicy>.None);
        option.IsNone.ShouldBeTrue();
    }

    #endregion

    #region GetByCategoryAsync Contract

    /// <summary>
    /// Contract: GetByCategoryAsync for an existing data category should return Some.
    /// </summary>
    [Fact]
    public async Task Contract_GetByCategoryAsync_ExistingCategory_ReturnsSome()
    {
        var store = CreateStore();
        var category = $"category-{Guid.NewGuid():N}";
        var policy = CreatePolicy(dataCategory: category);
        await store.CreateAsync(policy);

        var result = await store.GetByCategoryAsync(category);

        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: r => r, Left: _ => Option<RetentionPolicy>.None);
        option.IsSome.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: GetByCategoryAsync for a non-existing data category should return None.
    /// </summary>
    [Fact]
    public async Task Contract_GetByCategoryAsync_NonExistingCategory_ReturnsNone()
    {
        var store = CreateStore();

        var result = await store.GetByCategoryAsync($"non-existing-category-{Guid.NewGuid():N}");

        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: r => r, Left: _ => Option<RetentionPolicy>.None);
        option.IsNone.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: GetByCategoryAsync should return the correct policy for its category.
    /// </summary>
    [Fact]
    public async Task Contract_GetByCategoryAsync_ReturnsCorrectPolicy()
    {
        var store = CreateStore();
        var category = $"category-{Guid.NewGuid():N}";
        var policy = CreatePolicy(dataCategory: category);
        await store.CreateAsync(policy);

        var result = await store.GetByCategoryAsync(category);
        var option = result.Match(Right: r => r, Left: _ => Option<RetentionPolicy>.None);
        var found = option.Match(Some: p => p, None: () => throw new InvalidOperationException("Expected Some but got None"));

        found.DataCategory.ShouldBe(category);
        found.Id.ShouldBe(policy.Id);
    }

    #endregion

    #region GetAllAsync Contract

    /// <summary>
    /// Contract: GetAllAsync should return all stored policies.
    /// </summary>
    [Fact]
    public async Task Contract_GetAllAsync_MultiplePolicies_ReturnsAll()
    {
        var store = CreateStore();
        var policy1 = CreatePolicy();
        var policy2 = CreatePolicy();
        var policy3 = CreatePolicy();

        await store.CreateAsync(policy1);
        await store.CreateAsync(policy2);
        await store.CreateAsync(policy3);

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

    #region UpdateAsync Contract

    /// <summary>
    /// Contract: UpdateAsync for an existing policy should return Right and persist changes.
    /// </summary>
    [Fact]
    public async Task Contract_UpdateAsync_ExistingPolicy_UpdatesPolicy()
    {
        var store = CreateStore();
        var policy = CreatePolicy(reason: "original reason");
        await store.CreateAsync(policy);

        var updated = policy with { Reason = "updated reason", AutoDelete = false };
        var result = await store.UpdateAsync(updated);

        result.IsRight.ShouldBeTrue();

        var fetchResult = await store.GetByIdAsync(policy.Id);
        var option = fetchResult.Match(Right: r => r, Left: _ => Option<RetentionPolicy>.None);
        option.IsSome.ShouldBeTrue();

        var found = option.Match(Some: p => p, None: () => throw new InvalidOperationException("Expected Some but got None"));
        found.Reason.ShouldBe("updated reason");
        found.AutoDelete.ShouldBeFalse();
    }

    /// <summary>
    /// Contract: UpdateAsync for a non-existing policy should return Left (error).
    /// </summary>
    [Fact]
    public async Task Contract_UpdateAsync_NonExistingPolicy_ReturnsLeft()
    {
        var store = CreateStore();
        var policy = CreatePolicy();

        var result = await store.UpdateAsync(policy);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region DeleteAsync Contract

    /// <summary>
    /// Contract: DeleteAsync for an existing policy should return Right and remove the policy.
    /// </summary>
    [Fact]
    public async Task Contract_DeleteAsync_ExistingPolicy_RemovesPolicy()
    {
        var store = CreateStore();
        var policy = CreatePolicy();
        await store.CreateAsync(policy);

        var result = await store.DeleteAsync(policy.Id);

        result.IsRight.ShouldBeTrue();

        var fetchResult = await store.GetByIdAsync(policy.Id);
        var option = fetchResult.Match(Right: r => r, Left: _ => Option<RetentionPolicy>.None);
        option.IsNone.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: DeleteAsync for a non-existing policy should return Left (error).
    /// </summary>
    [Fact]
    public async Task Contract_DeleteAsync_NonExistingPolicy_ReturnsLeft()
    {
        var store = CreateStore();

        var result = await store.DeleteAsync($"non-existing-{Guid.NewGuid():N}");

        result.IsLeft.ShouldBeTrue();
    }

    #endregion
}
