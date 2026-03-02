using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.DataResidency;

public class DataResidencyRegionPropertyTests
{
    [Property(MaxTest = 50)]
    public Property Region_Equality_IsCaseInsensitiveOnCode()
    {
        return Prop.ForAll(
            Gen.Elements("DE", "FR", "IT", "ES", "NL", "PL", "NO", "JP").ToArbitrary(),
            code =>
            {
                var r1 = Region.Create(code.ToUpperInvariant(), code, false, false, false, DataProtectionLevel.Medium);
                var r2 = Region.Create(code.ToLowerInvariant(), code, false, false, false, DataProtectionLevel.Medium);
                return r1.Equals(r2);
            });
    }

    [Property(MaxTest = 50)]
    public bool EUMemberStates_AllAreEUAndEEA()
    {
        return RegionRegistry.EUMemberStates.All(r => r.IsEU && r.IsEEA);
    }

    [Property(MaxTest = 50)]
    public bool EEACountries_IncludeAllEUMemberStates()
    {
        return RegionRegistry.EUMemberStates.All(eu =>
            RegionRegistry.EEACountries.Any(eea => eea.Code == eu.Code));
    }

    [Property(MaxTest = 50)]
    public bool AdequacyCountries_AllHaveAdequacyDecision()
    {
        return RegionRegistry.AdequacyCountries.All(r => r.HasAdequacyDecision);
    }

    [Property(MaxTest = 50)]
    public Property RegionRegistry_GetByCode_RoundTrips()
    {
        var allRegions = RegionRegistry.EUMemberStates
            .Concat(RegionRegistry.EEACountries)
            .Concat(RegionRegistry.AdequacyCountries)
            .DistinctBy(r => r.Code)
            .ToArray();

        return Prop.ForAll(
            Gen.Elements(allRegions).ToArbitrary(),
            region =>
            {
                var looked = RegionRegistry.GetByCode(region.Code);
                return looked != null && looked.Code == region.Code;
            });
    }

    [Property(MaxTest = 50)]
    public Property RegionRegistry_GetByCode_CaseInsensitive()
    {
        return Prop.ForAll(
            Gen.Elements("DE", "FR", "IT", "ES", "NL").ToArbitrary(),
            code =>
            {
                var upper = RegionRegistry.GetByCode(code.ToUpperInvariant());
                var lower = RegionRegistry.GetByCode(code.ToLowerInvariant());
                return upper != null && lower != null && upper.Code == lower.Code;
            });
    }
}
