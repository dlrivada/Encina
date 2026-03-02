using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DataResidency;

public class DataResidencyOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldHaveSensibleDefaults()
    {
        // Act
        var options = new DataResidencyOptions();

        // Assert
        options.DefaultRegion.Should().BeNull();
        options.EnforcementMode.Should().Be(DataResidencyEnforcementMode.Warn);
        options.TrackDataLocations.Should().BeTrue();
        options.TrackAuditTrail.Should().BeTrue();
        options.BlockNonCompliantTransfers.Should().BeTrue();
        options.AddHealthCheck.Should().BeFalse();
        options.AutoRegisterFromAttributes.Should().BeTrue();
        options.AssembliesToScan.Should().BeEmpty();
        options.AdditionalAdequateRegions.Should().BeEmpty();
    }

    [Fact]
    public void DefaultRegion_WhenSet_ShouldReturnValue()
    {
        // Arrange
        var options = new DataResidencyOptions();

        // Act
        options.DefaultRegion = RegionRegistry.DE;

        // Assert
        options.DefaultRegion.Should().Be(RegionRegistry.DE);
    }

    [Theory]
    [InlineData(DataResidencyEnforcementMode.Disabled)]
    [InlineData(DataResidencyEnforcementMode.Warn)]
    [InlineData(DataResidencyEnforcementMode.Block)]
    public void EnforcementMode_WhenSet_ShouldReturnValue(DataResidencyEnforcementMode mode)
    {
        // Arrange
        var options = new DataResidencyOptions();

        // Act
        options.EnforcementMode = mode;

        // Assert
        options.EnforcementMode.Should().Be(mode);
    }

    [Fact]
    public void AdditionalAdequateRegions_ShouldAcceptCustomRegions()
    {
        // Arrange
        var options = new DataResidencyOptions();
        var customRegion = Region.Create("XX", "XX", isEU: false, isEEA: false, hasAdequacyDecision: false, DataProtectionLevel.Medium);

        // Act
        options.AdditionalAdequateRegions.Add(customRegion);

        // Assert
        options.AdditionalAdequateRegions.Should().ContainSingle();
    }

    [Fact]
    public void AddPolicy_ShouldRegisterFluentPolicy()
    {
        // Arrange
        var options = new DataResidencyOptions();

        // Act
        options.AddPolicy("healthcare-data", p => p.AllowEU().RequireAdequacyDecision());

        // Assert
        options.ConfiguredPolicies.Should().ContainSingle()
            .Which.DataCategory.Should().Be("healthcare-data");
    }
}
