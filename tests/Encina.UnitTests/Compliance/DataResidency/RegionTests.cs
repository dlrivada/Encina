using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DataResidency;

public class RegionTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        // Act
        var region = Region.Create("DE", "DE", isEU: true, isEEA: true,
            hasAdequacyDecision: true, DataProtectionLevel.High);

        // Assert
        region.Code.Should().Be("DE");
        region.Country.Should().Be("DE");
        region.IsEU.Should().BeTrue();
        region.IsEEA.Should().BeTrue();
        region.HasAdequacyDecision.Should().BeTrue();
        region.ProtectionLevel.Should().Be(DataProtectionLevel.High);
    }

    [Fact]
    public void Equality_SameCode_ShouldBeEqual()
    {
        // Arrange
        var region1 = Region.Create("DE", "DE", true, true, true, DataProtectionLevel.High);
        var region2 = Region.Create("DE", "DE", true, true, true, DataProtectionLevel.High);

        // Assert
        region1.Should().Be(region2);
        (region1 == region2).Should().BeTrue();
    }

    [Fact]
    public void Equality_CaseInsensitive_ShouldBeEqual()
    {
        // Arrange
        var region1 = Region.Create("de", "DE", true, true, true, DataProtectionLevel.High);
        var region2 = Region.Create("DE", "DE", true, true, true, DataProtectionLevel.High);

        // Assert
        region1.Should().Be(region2);
    }

    [Fact]
    public void Equality_DifferentCode_ShouldNotBeEqual()
    {
        // Arrange
        var region1 = Region.Create("DE", "DE", true, true, true, DataProtectionLevel.High);
        var region2 = Region.Create("FR", "FR", true, true, true, DataProtectionLevel.High);

        // Assert
        region1.Should().NotBe(region2);
    }

    [Fact]
    public void NonEURegion_ShouldHaveCorrectFlags()
    {
        // Act
        var region = Region.Create("US", "US", isEU: false, isEEA: false,
            hasAdequacyDecision: true, DataProtectionLevel.Medium);

        // Assert
        region.IsEU.Should().BeFalse();
        region.IsEEA.Should().BeFalse();
        region.HasAdequacyDecision.Should().BeTrue();
    }

    [Theory]
    [InlineData(DataProtectionLevel.High)]
    [InlineData(DataProtectionLevel.Medium)]
    [InlineData(DataProtectionLevel.Low)]
    [InlineData(DataProtectionLevel.Unknown)]
    public void Create_WithAnyProtectionLevel_ShouldPreserve(DataProtectionLevel level)
    {
        // Act
        var region = Region.Create("XX", "XX", false, false, false, level);

        // Assert
        region.ProtectionLevel.Should().Be(level);
    }
}
