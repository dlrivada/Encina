using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.DataResidency;

public class DataResidencyMapperPropertyTests
{
    private static readonly Region[] KnownRegions =
    [
        RegionRegistry.DE, RegionRegistry.FR, RegionRegistry.IT,
        RegionRegistry.ES, RegionRegistry.NL, RegionRegistry.PL,
        RegionRegistry.NO, RegionRegistry.JP, RegionRegistry.GB,
        RegionRegistry.CH, RegionRegistry.US
    ];

    [Property(MaxTest = 50)]
    public Property DataLocationMapper_RoundTrip_PreservesStorageType()
    {
        return Prop.ForAll(
            Gen.Elements((StorageType[])Enum.GetValues(typeof(StorageType))).ToArbitrary(),
            storageType =>
            {
                var location = DataLocation.Create("entity-1", "data", RegionRegistry.DE, storageType);
                var entity = DataLocationMapper.ToEntity(location);
                var roundTripped = DataLocationMapper.ToDomain(entity);
                return roundTripped != null && roundTripped.StorageType == storageType;
            });
    }

    [Property(MaxTest = 50)]
    public Property DataLocationMapper_RoundTrip_PreservesRegion()
    {
        return Prop.ForAll(
            Gen.Elements(KnownRegions).ToArbitrary(),
            region =>
            {
                var location = DataLocation.Create("entity-1", "data", region);
                var entity = DataLocationMapper.ToEntity(location);
                var roundTripped = DataLocationMapper.ToDomain(entity);
                return roundTripped != null && roundTripped.Region.Code == region.Code;
            });
    }

    [Property(MaxTest = 50)]
    public Property ResidencyAuditEntryMapper_RoundTrip_PreservesActionAndOutcome()
    {
        var actionGen = Gen.Elements((ResidencyAction[])Enum.GetValues(typeof(ResidencyAction)));
        var outcomeGen = Gen.Elements((ResidencyOutcome[])Enum.GetValues(typeof(ResidencyOutcome)));

        return Prop.ForAll(
            actionGen.ToArbitrary(),
            outcomeGen.ToArbitrary(),
            (action, outcome) =>
            {
                var entry = ResidencyAuditEntry.Create("data", "DE", action, outcome);
                var entity = ResidencyAuditEntryMapper.ToEntity(entry);
                var roundTripped = ResidencyAuditEntryMapper.ToDomain(entity);
                return roundTripped != null
                    && roundTripped.Action == action
                    && roundTripped.Outcome == outcome;
            });
    }

    [Property(MaxTest = 50)]
    public Property ResidencyPolicyMapper_RoundTrip_PreservesRegionCount()
    {
        return Prop.ForAll(
            Gen.Choose(1, 5).ToArbitrary(),
            count =>
            {
                var regions = KnownRegions.Take(count).ToList();
                var policy = ResidencyPolicyDescriptor.Create("data", regions);
                var entity = ResidencyPolicyMapper.ToEntity(policy);
                var roundTripped = ResidencyPolicyMapper.ToDomain(entity);
                return roundTripped != null && roundTripped.AllowedRegions.Count == count;
            });
    }

    [Property(MaxTest = 50)]
    public Property ResidencyPolicyMapper_RoundTrip_PreservesAdequacyFlag()
    {
        return Prop.ForAll(
            Gen.Elements(true, false).ToArbitrary(),
            flag =>
            {
                var policy = ResidencyPolicyDescriptor.Create("data", [RegionRegistry.DE], flag);
                var entity = ResidencyPolicyMapper.ToEntity(policy);
                var roundTripped = ResidencyPolicyMapper.ToDomain(entity);
                return roundTripped != null && roundTripped.RequireAdequacyDecision == flag;
            });
    }
}
