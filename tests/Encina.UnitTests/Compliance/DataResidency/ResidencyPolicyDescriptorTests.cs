using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DataResidency;

public class ResidencyPolicyDescriptorTests
{
    [Fact]
    public void Create_WithRequiredParameters_ShouldSetAllProperties()
    {
        // Arrange
        var regions = new List<Region> { RegionRegistry.DE, RegionRegistry.FR };

        // Act
        var policy = ResidencyPolicyDescriptor.Create(
            dataCategory: "healthcare-data",
            allowedRegions: regions);

        // Assert
        policy.DataCategory.Should().Be("healthcare-data");
        policy.AllowedRegions.Should().HaveCount(2);
        policy.AllowedRegions.Should().Contain(RegionRegistry.DE);
        policy.AllowedRegions.Should().Contain(RegionRegistry.FR);
        policy.RequireAdequacyDecision.Should().BeFalse();
        policy.AllowedTransferBases.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithAdequacyRequired_ShouldSetFlag()
    {
        // Act
        var policy = ResidencyPolicyDescriptor.Create(
            dataCategory: "sensitive-data",
            allowedRegions: [RegionRegistry.DE],
            requireAdequacyDecision: true);

        // Assert
        policy.RequireAdequacyDecision.Should().BeTrue();
    }

    [Fact]
    public void Create_WithTransferBases_ShouldPreserveValues()
    {
        // Arrange
        var bases = new List<TransferLegalBasis>
        {
            TransferLegalBasis.AdequacyDecision,
            TransferLegalBasis.StandardContractualClauses
        };

        // Act
        var policy = ResidencyPolicyDescriptor.Create(
            dataCategory: "data",
            allowedRegions: [RegionRegistry.DE],
            allowedTransferBases: bases);

        // Assert
        policy.AllowedTransferBases.Should().HaveCount(2);
        policy.AllowedTransferBases.Should().Contain(TransferLegalBasis.AdequacyDecision);
        policy.AllowedTransferBases.Should().Contain(TransferLegalBasis.StandardContractualClauses);
    }

    [Fact]
    public void Create_WithEmptyRegions_ShouldAllowEmptyList()
    {
        // Act
        var policy = ResidencyPolicyDescriptor.Create(
            dataCategory: "unrestricted-data",
            allowedRegions: []);

        // Assert
        policy.AllowedRegions.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithNullTransferBases_ShouldDefaultToEmpty()
    {
        // Act
        var policy = ResidencyPolicyDescriptor.Create(
            dataCategory: "data",
            allowedRegions: [RegionRegistry.DE],
            allowedTransferBases: null);

        // Assert
        policy.AllowedTransferBases.Should().BeEmpty();
    }
}
