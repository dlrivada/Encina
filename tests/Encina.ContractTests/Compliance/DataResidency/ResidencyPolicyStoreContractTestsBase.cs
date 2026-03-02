#pragma warning disable CA1859 // Use concrete types when possible for improved performance
#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

namespace Encina.ContractTests.Compliance.DataResidency;

public abstract class ResidencyPolicyStoreContractTestsBase
{
    protected abstract IResidencyPolicyStore CreateStore();

    protected static ResidencyPolicyDescriptor CreatePolicy(
        string? dataCategory = null,
        IReadOnlyList<Region>? allowedRegions = null,
        bool requireAdequacyDecision = false)
    {
        return ResidencyPolicyDescriptor.Create(
            dataCategory: dataCategory ?? $"category-{Guid.NewGuid():N}",
            allowedRegions: allowedRegions ?? [RegionRegistry.DE],
            requireAdequacyDecision: requireAdequacyDecision);
    }

    [Fact]
    public async Task Contract_CreateAsync_ValidPolicy_ShouldSucceed()
    {
        // Arrange
        var store = CreateStore();
        var policy = CreatePolicy();

        // Act
        var result = await store.CreateAsync(policy);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_CreateAsync_DuplicateCategory_ShouldReturnLeftError()
    {
        // Arrange
        var store = CreateStore();
        var category = $"dup-{Guid.NewGuid():N}";
        var policy1 = CreatePolicy(dataCategory: category);
        var policy2 = CreatePolicy(dataCategory: category);

        await store.CreateAsync(policy1);

        // Act
        var result = await store.CreateAsync(policy2);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_GetByCategoryAsync_ExistingCategory_ShouldReturnSome()
    {
        // Arrange
        var store = CreateStore();
        var policy = CreatePolicy(dataCategory: "test-category");
        await store.CreateAsync(policy);

        // Act
        var result = await store.GetByCategoryAsync("test-category");

        // Assert
        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: opt => opt, Left: _ => default);
        option.IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_GetByCategoryAsync_NonExistingCategory_ShouldReturnNone()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.GetByCategoryAsync("non-existing");

        // Assert
        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: opt => opt, Left: _ => default);
        option.IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_GetByCategoryAsync_ShouldPreserveAllFields()
    {
        // Arrange
        var store = CreateStore();
        var regions = new List<Region> { RegionRegistry.DE, RegionRegistry.FR, RegionRegistry.IT };
        var bases = new List<TransferLegalBasis> { TransferLegalBasis.AdequacyDecision, TransferLegalBasis.StandardContractualClauses };
        var policy = ResidencyPolicyDescriptor.Create("healthcare", regions, true, bases);
        await store.CreateAsync(policy);

        // Act
        var result = await store.GetByCategoryAsync("healthcare");

        // Assert
        var option = result.Match(Right: opt => opt, Left: _ => default);
        var p = option.Match(Some: x => x, None: () => throw new InvalidOperationException("Expected Some"));
        p.DataCategory.ShouldBe("healthcare");
        p.AllowedRegions.Count.ShouldBe(3);
        p.RequireAdequacyDecision.ShouldBeTrue();
        p.AllowedTransferBases!.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Contract_GetAllAsync_MultipleItems_ShouldReturnAll()
    {
        // Arrange
        var store = CreateStore();
        await store.CreateAsync(CreatePolicy(dataCategory: "cat-1"));
        await store.CreateAsync(CreatePolicy(dataCategory: "cat-2"));
        await store.CreateAsync(CreatePolicy(dataCategory: "cat-3"));

        // Act
        var result = await store.GetAllAsync();

        // Assert
        var policies = result.Match(Right: p => p, Left: _ => []);
        policies.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Contract_UpdateAsync_ExistingPolicy_ShouldUpdateFields()
    {
        // Arrange
        var store = CreateStore();
        var category = "update-test";
        var original = CreatePolicy(dataCategory: category, allowedRegions: [RegionRegistry.DE]);
        await store.CreateAsync(original);

        var updated = ResidencyPolicyDescriptor.Create(category, [RegionRegistry.DE, RegionRegistry.FR], true);

        // Act
        var result = await store.UpdateAsync(updated);

        // Assert
        result.IsRight.ShouldBeTrue();

        var retrieved = await store.GetByCategoryAsync(category);
        var option = retrieved.Match(Right: opt => opt, Left: _ => default);
        var p = option.Match(Some: x => x, None: () => throw new InvalidOperationException("Expected Some"));
        p.AllowedRegions.Count.ShouldBe(2);
        p.RequireAdequacyDecision.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_DeleteAsync_ExistingPolicy_ShouldRemoveIt()
    {
        // Arrange
        var store = CreateStore();
        var category = "delete-test";
        await store.CreateAsync(CreatePolicy(dataCategory: category));

        // Act
        var result = await store.DeleteAsync(category);

        // Assert
        result.IsRight.ShouldBeTrue();

        var retrieved = await store.GetByCategoryAsync(category);
        var option = retrieved.Match(Right: opt => opt, Left: _ => default);
        option.IsNone.ShouldBeTrue();
    }
}
