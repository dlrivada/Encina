using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using Shouldly;

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
        region.Code.ShouldBe("DE");
        region.Country.ShouldBe("DE");
        region.IsEU.ShouldBeTrue();
        region.IsEEA.ShouldBeTrue();
        region.HasAdequacyDecision.ShouldBeTrue();
        region.ProtectionLevel.ShouldBe(DataProtectionLevel.High);
    }

    [Fact]
    public void Equality_SameCode_ShouldBeEqual()
    {
        // Arrange
        var region1 = Region.Create("DE", "DE", true, true, true, DataProtectionLevel.High);
        var region2 = Region.Create("DE", "DE", true, true, true, DataProtectionLevel.High);

        // Assert
        region1.ShouldBe(region2);
        (region1 == region2).ShouldBeTrue();
    }

    [Fact]
    public void Equality_CaseInsensitive_ShouldBeEqual()
    {
        // Arrange
        var region1 = Region.Create("de", "DE", true, true, true, DataProtectionLevel.High);
        var region2 = Region.Create("DE", "DE", true, true, true, DataProtectionLevel.High);

        // Assert
        region1.ShouldBe(region2);
    }

    [Fact]
    public void Equality_DifferentCode_ShouldNotBeEqual()
    {
        // Arrange
        var region1 = Region.Create("DE", "DE", true, true, true, DataProtectionLevel.High);
        var region2 = Region.Create("FR", "FR", true, true, true, DataProtectionLevel.High);

        // Assert
        region1.ShouldNotBe(region2);
    }

    [Fact]
    public void NonEURegion_ShouldHaveCorrectFlags()
    {
        // Act
        var region = Region.Create("US", "US", isEU: false, isEEA: false,
            hasAdequacyDecision: true, DataProtectionLevel.Medium);

        // Assert
        region.IsEU.ShouldBeFalse();
        region.IsEEA.ShouldBeFalse();
        region.HasAdequacyDecision.ShouldBeTrue();
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
        region.ProtectionLevel.ShouldBe(level);
    }
}
