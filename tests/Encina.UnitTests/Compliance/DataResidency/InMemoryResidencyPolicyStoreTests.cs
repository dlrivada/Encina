using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.InMemory;
using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Compliance.DataResidency;

public class InMemoryResidencyPolicyStoreTests
{
    private readonly InMemoryResidencyPolicyStore _store;

    public InMemoryResidencyPolicyStoreTests()
    {
        _store = new InMemoryResidencyPolicyStore(NullLogger<InMemoryResidencyPolicyStore>.Instance);
    }

    [Fact]
    public async Task CreateAsync_ValidPolicy_ShouldSucceed()
    {
        // Arrange
        var policy = ResidencyPolicyDescriptor.Create(
            dataCategory: "personal-data",
            allowedRegions: [RegionRegistry.DE, RegionRegistry.FR]);

        // Act
        var result = await _store.CreateAsync(policy);

        // Assert
        result.IsRight.Should().BeTrue();
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCategory_ShouldReturnError()
    {
        // Arrange
        var policy1 = ResidencyPolicyDescriptor.Create("personal-data", [RegionRegistry.DE]);
        var policy2 = ResidencyPolicyDescriptor.Create("personal-data", [RegionRegistry.FR]);
        await _store.CreateAsync(policy1);

        // Act
        var result = await _store.CreateAsync(policy2);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.LeftAsEnumerable().First().GetCode()
            .Match(Some: code => code, None: () => "")
            .Should().Be(DataResidencyErrors.PolicyAlreadyExistsCode);
    }

    [Fact]
    public async Task GetByCategoryAsync_ExistingCategory_ShouldReturnPolicy()
    {
        // Arrange
        var policy = ResidencyPolicyDescriptor.Create("healthcare-data", [RegionRegistry.DE]);
        await _store.CreateAsync(policy);

        // Act
        var result = await _store.GetByCategoryAsync("healthcare-data");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: opt => opt.IsSome.Should().BeTrue(),
            Left: _ => { });
    }

    [Fact]
    public async Task GetByCategoryAsync_NonExistingCategory_ShouldReturnNone()
    {
        // Act
        var result = await _store.GetByCategoryAsync("unknown");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: opt => opt.IsNone.Should().BeTrue(),
            Left: _ => { });
    }

    [Fact]
    public async Task GetAllAsync_WithMultiplePolicies_ShouldReturnAll()
    {
        // Arrange
        await _store.CreateAsync(ResidencyPolicyDescriptor.Create("cat1", [RegionRegistry.DE]));
        await _store.CreateAsync(ResidencyPolicyDescriptor.Create("cat2", [RegionRegistry.FR]));
        await _store.CreateAsync(ResidencyPolicyDescriptor.Create("cat3", [RegionRegistry.IT]));

        // Act
        var result = await _store.GetAllAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: policies => policies.Should().HaveCount(3),
            Left: _ => { });
    }

    [Fact]
    public async Task GetAllAsync_Empty_ShouldReturnEmptyList()
    {
        // Act
        var result = await _store.GetAllAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: policies => policies.Should().BeEmpty(),
            Left: _ => { });
    }

    [Fact]
    public async Task UpdateAsync_ExistingPolicy_ShouldSucceed()
    {
        // Arrange
        var policy = ResidencyPolicyDescriptor.Create("personal-data", [RegionRegistry.DE]);
        await _store.CreateAsync(policy);

        var updated = ResidencyPolicyDescriptor.Create("personal-data", [RegionRegistry.DE, RegionRegistry.FR]);

        // Act
        var result = await _store.UpdateAsync(updated);

        // Assert
        result.IsRight.Should().BeTrue();
        var retrieved = await _store.GetByCategoryAsync("personal-data");
        retrieved.Match(
            Right: opt => opt.Match(
                Some: p => p.AllowedRegions.Should().HaveCount(2),
                None: () => { }),
            Left: _ => { });
    }

    [Fact]
    public async Task DeleteAsync_ExistingPolicy_ShouldRemoveIt()
    {
        // Arrange
        await _store.CreateAsync(ResidencyPolicyDescriptor.Create("personal-data", [RegionRegistry.DE]));

        // Act
        var result = await _store.DeleteAsync("personal-data");

        // Assert
        result.IsRight.Should().BeTrue();
        _store.Count.Should().Be(0);
    }

    [Fact]
    public async Task Clear_ShouldRemoveAllPolicies()
    {
        // Arrange
        await _store.CreateAsync(ResidencyPolicyDescriptor.Create("cat1", [RegionRegistry.DE]));
        await _store.CreateAsync(ResidencyPolicyDescriptor.Create("cat2", [RegionRegistry.FR]));

        // Act
        _store.Clear();

        // Assert
        _store.Count.Should().Be(0);
    }
}
