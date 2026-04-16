using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using Shouldly;

namespace Encina.UnitTests.Compliance.DataResidency;

public class DataResidencyOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldHaveSensibleDefaults()
    {
        // Act
        var options = new DataResidencyOptions();

        // Assert
        options.DefaultRegion.ShouldBeNull();
        options.EnforcementMode.ShouldBe(DataResidencyEnforcementMode.Warn);
        options.TrackDataLocations.ShouldBeTrue();
        options.BlockNonCompliantTransfers.ShouldBeTrue();
        options.AddHealthCheck.ShouldBeFalse();
        options.AutoRegisterFromAttributes.ShouldBeTrue();
        options.AssembliesToScan.ShouldBeEmpty();
        options.AdditionalAdequateRegions.ShouldBeEmpty();
    }

    [Fact]
    public void DefaultRegion_WhenSet_ShouldReturnValue()
    {
        // Arrange
        var options = new DataResidencyOptions();

        // Act
        options.DefaultRegion = RegionRegistry.DE;

        // Assert
        options.DefaultRegion.ShouldBe(RegionRegistry.DE);
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
        options.EnforcementMode.ShouldBe(mode);
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
        options.AdditionalAdequateRegions.ShouldHaveSingleItem();
    }

    [Fact]
    public void AddPolicy_ShouldRegisterFluentPolicy()
    {
        // Arrange
        var options = new DataResidencyOptions();

        // Act
        options.AddPolicy("healthcare-data", p => p.AllowEU().RequireAdequacyDecision());

        // Assert
        options.ConfiguredPolicies.ShouldHaveSingleItem()
            .Which.DataCategory.ShouldBe("healthcare-data");
    }
}
