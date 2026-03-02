using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DataResidency;

public class RegionRegistryTests
{
    [Fact]
    public void EUMemberStates_ShouldContain27Countries()
    {
        RegionRegistry.EUMemberStates.Should().HaveCount(27);
    }

    [Fact]
    public void EEACountries_ShouldContainEUPlusEFTA()
    {
        // EEA = 27 EU + 3 EFTA (IS, LI, NO) = 30
        RegionRegistry.EEACountries.Should().HaveCount(30);
    }

    [Fact]
    public void EUMemberStates_ShouldAllBeEU()
    {
        foreach (var region in RegionRegistry.EUMemberStates)
        {
            region.IsEU.Should().BeTrue($"{region.Code} should be EU");
            region.IsEEA.Should().BeTrue($"{region.Code} should be EEA");
        }
    }

    [Fact]
    public void EEAOnlyCountries_ShouldBeEEAButNotEU()
    {
        RegionRegistry.NO.IsEEA.Should().BeTrue();
        RegionRegistry.NO.IsEU.Should().BeFalse();

        RegionRegistry.IS.IsEEA.Should().BeTrue();
        RegionRegistry.IS.IsEU.Should().BeFalse();

        RegionRegistry.LI.IsEEA.Should().BeTrue();
        RegionRegistry.LI.IsEU.Should().BeFalse();
    }

    [Fact]
    public void AdequacyCountries_ShouldHaveAdequacyDecision()
    {
        foreach (var region in RegionRegistry.AdequacyCountries)
        {
            region.HasAdequacyDecision.Should().BeTrue($"{region.Code} should have adequacy decision");
        }
    }

    [Fact]
    public void GetByCode_KnownCode_ShouldReturnRegion()
    {
        var region = RegionRegistry.GetByCode("DE");
        region.Should().NotBeNull();
        region!.Code.Should().Be("DE");
    }

    [Fact]
    public void GetByCode_UnknownCode_ShouldReturnNull()
    {
        var region = RegionRegistry.GetByCode("ZZZZZ");
        region.Should().BeNull();
    }

    [Fact]
    public void GetByCode_CaseInsensitive_ShouldReturnRegion()
    {
        var region = RegionRegistry.GetByCode("de");
        region.Should().NotBeNull();
        region!.Code.Should().Be("DE");
    }

    [Theory]
    [InlineData("DE")]
    [InlineData("FR")]
    [InlineData("IT")]
    [InlineData("ES")]
    [InlineData("NL")]
    [InlineData("PL")]
    public void WellKnownEURegions_ShouldExistInRegistry(string code)
    {
        var region = RegionRegistry.GetByCode(code);
        region.Should().NotBeNull();
        region!.IsEU.Should().BeTrue();
    }

    [Theory]
    [InlineData("JP")]
    [InlineData("GB")]
    [InlineData("CH")]
    [InlineData("KR")]
    [InlineData("NZ")]
    public void WellKnownAdequacyRegions_ShouldExistInRegistry(string code)
    {
        var region = RegionRegistry.GetByCode(code);
        region.Should().NotBeNull();
        region!.HasAdequacyDecision.Should().BeTrue();
    }

    [Fact]
    public void DE_ShouldHaveHighProtection()
    {
        RegionRegistry.DE.ProtectionLevel.Should().Be(DataProtectionLevel.High);
    }

    [Fact]
    public void CN_ShouldNotHaveAdequacy()
    {
        RegionRegistry.CN.HasAdequacyDecision.Should().BeFalse();
        RegionRegistry.CN.IsEU.Should().BeFalse();
        RegionRegistry.CN.IsEEA.Should().BeFalse();
    }
}
