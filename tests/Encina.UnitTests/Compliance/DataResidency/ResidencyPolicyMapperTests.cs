using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DataResidency;

public class ResidencyPolicyMapperTests
{
    [Fact]
    public void ToEntity_ShouldMapAllProperties()
    {
        // Arrange
        var policy = ResidencyPolicyDescriptor.Create(
            dataCategory: "healthcare-data",
            allowedRegions: [RegionRegistry.DE, RegionRegistry.FR],
            requireAdequacyDecision: true,
            allowedTransferBases: [TransferLegalBasis.AdequacyDecision, TransferLegalBasis.StandardContractualClauses]);

        // Act
        var entity = ResidencyPolicyMapper.ToEntity(policy);

        // Assert
        entity.DataCategory.Should().Be("healthcare-data");
        entity.AllowedRegionCodes.Should().Be("DE,FR");
        entity.RequireAdequacyDecision.Should().BeTrue();
        entity.AllowedTransferBasesValue.Should().Be("0,1");
        entity.CreatedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        entity.LastModifiedAtUtc.Should().BeNull();
    }

    [Fact]
    public void ToEntity_EmptyTransferBases_ShouldSetNull()
    {
        // Arrange
        var policy = ResidencyPolicyDescriptor.Create("data", [RegionRegistry.DE]);

        // Act
        var entity = ResidencyPolicyMapper.ToEntity(policy);

        // Assert
        entity.AllowedTransferBasesValue.Should().BeNull();
    }

    [Fact]
    public void ToEntity_NullPolicy_ShouldThrow()
    {
        var act = () => ResidencyPolicyMapper.ToEntity(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDomain_ValidEntity_ShouldMapAllProperties()
    {
        // Arrange
        var entity = new ResidencyPolicyEntity
        {
            DataCategory = "personal-data",
            AllowedRegionCodes = "DE,FR",
            RequireAdequacyDecision = true,
            AllowedTransferBasesValue = "0,1",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        // Act
        var result = ResidencyPolicyMapper.ToDomain(entity);

        // Assert
        result.Should().NotBeNull();
        result!.DataCategory.Should().Be("personal-data");
        result.AllowedRegions.Should().HaveCount(2);
        result.AllowedRegions.Should().Contain(r => r.Code == "DE");
        result.AllowedRegions.Should().Contain(r => r.Code == "FR");
        result.RequireAdequacyDecision.Should().BeTrue();
        result.AllowedTransferBases.Should().HaveCount(2);
    }

    [Fact]
    public void ToDomain_NullTransferBases_ShouldReturnEmptyList()
    {
        // Arrange
        var entity = new ResidencyPolicyEntity
        {
            DataCategory = "data",
            AllowedRegionCodes = "DE",
            RequireAdequacyDecision = false,
            AllowedTransferBasesValue = null,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        // Act
        var result = ResidencyPolicyMapper.ToDomain(entity);

        // Assert
        result.Should().NotBeNull();
        result!.AllowedTransferBases.Should().BeEmpty();
    }

    [Fact]
    public void ToDomain_InvalidRegionCode_ShouldReturnNull()
    {
        // Arrange
        var entity = new ResidencyPolicyEntity
        {
            DataCategory = "data",
            AllowedRegionCodes = "DE,INVALID",
            RequireAdequacyDecision = false,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        // Act
        var result = ResidencyPolicyMapper.ToDomain(entity);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ToDomain_InvalidTransferBasis_ShouldReturnNull()
    {
        // Arrange
        var entity = new ResidencyPolicyEntity
        {
            DataCategory = "data",
            AllowedRegionCodes = "DE",
            RequireAdequacyDecision = false,
            AllowedTransferBasesValue = "0,999",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        // Act
        var result = ResidencyPolicyMapper.ToDomain(entity);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ToDomain_NullEntity_ShouldThrow()
    {
        var act = () => ResidencyPolicyMapper.ToDomain(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RoundTrip_ShouldPreserveAllValues()
    {
        // Arrange
        var original = ResidencyPolicyDescriptor.Create(
            "healthcare-data",
            [RegionRegistry.DE, RegionRegistry.FR, RegionRegistry.IT],
            requireAdequacyDecision: true,
            allowedTransferBases: [TransferLegalBasis.AdequacyDecision]);

        // Act
        var entity = ResidencyPolicyMapper.ToEntity(original);
        var roundTripped = ResidencyPolicyMapper.ToDomain(entity);

        // Assert
        roundTripped.Should().NotBeNull();
        roundTripped!.DataCategory.Should().Be(original.DataCategory);
        roundTripped.AllowedRegions.Should().HaveCount(3);
        roundTripped.RequireAdequacyDecision.Should().Be(original.RequireAdequacyDecision);
        roundTripped.AllowedTransferBases.Should().HaveCount(1);
    }
}
