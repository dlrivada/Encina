using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using Shouldly;

namespace Encina.UnitTests.Compliance.DataResidency;

public class RegionRegistryTests
{
    [Fact]
    public void EUMemberStates_ShouldContain27Countries()
    {
        RegionRegistry.EUMemberStates.Count.ShouldBe(27);
    }

    [Fact]
    public void EEACountries_ShouldContainEUPlusEFTA()
    {
        // EEA = 27 EU + 3 EFTA (IS, LI, NO) = 30
        RegionRegistry.EEACountries.Count.ShouldBe(30);
    }

    [Fact]
    public void EUMemberStates_ShouldAllBeEU()
    {
        foreach (var region in RegionRegistry.EUMemberStates)
        {
            region.IsEU.ShouldBeTrue();
            region.IsEEA.ShouldBeTrue();
        }
    }

    [Fact]
    public void EEAOnlyCountries_ShouldBeEEAButNotEU()
    {
        RegionRegistry.NO.IsEEA.ShouldBeTrue();
        RegionRegistry.NO.IsEU.ShouldBeFalse();

        RegionRegistry.IS.IsEEA.ShouldBeTrue();
        RegionRegistry.IS.IsEU.ShouldBeFalse();

        RegionRegistry.LI.IsEEA.ShouldBeTrue();
        RegionRegistry.LI.IsEU.ShouldBeFalse();
    }

    [Fact]
    public void AdequacyCountries_ShouldHaveAdequacyDecision()
    {
        foreach (var region in RegionRegistry.AdequacyCountries)
        {
            region.HasAdequacyDecision.ShouldBeTrue();
        }
    }

    [Fact]
    public void GetByCode_KnownCode_ShouldReturnRegion()
    {
        var region = RegionRegistry.GetByCode("DE");
        region.ShouldNotBeNull();
        region!.Code.ShouldBe("DE");
    }

    [Fact]
    public void GetByCode_UnknownCode_ShouldReturnNull()
    {
        var region = RegionRegistry.GetByCode("ZZZZZ");
        region.ShouldBeNull();
    }

    [Fact]
    public void GetByCode_CaseInsensitive_ShouldReturnRegion()
    {
        var region = RegionRegistry.GetByCode("de");
        region.ShouldNotBeNull();
        region!.Code.ShouldBe("DE");
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
        region.ShouldNotBeNull();
        region!.IsEU.ShouldBeTrue();
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
        region.ShouldNotBeNull();
        region!.HasAdequacyDecision.ShouldBeTrue();
    }

    [Fact]
    public void DE_ShouldHaveHighProtection()
    {
        RegionRegistry.DE.ProtectionLevel.ShouldBe(DataProtectionLevel.High);
    }

    [Fact]
    public void CN_ShouldNotHaveAdequacy()
    {
        RegionRegistry.CN.HasAdequacyDecision.ShouldBeFalse();
        RegionRegistry.CN.IsEU.ShouldBeFalse();
        RegionRegistry.CN.IsEEA.ShouldBeFalse();
    }
}
