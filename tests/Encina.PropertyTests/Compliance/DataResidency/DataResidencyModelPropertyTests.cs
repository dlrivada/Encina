using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.DataResidency;

public class DataResidencyModelPropertyTests
{
    [Property(MaxTest = 50)]
    public bool DataLocation_Create_AlwaysGeneratesNonEmptyId(NonEmptyString entityId, NonEmptyString category)
    {
        var location = DataLocation.Create(
            entityId: entityId.Get,
            dataCategory: category.Get,
            region: RegionRegistry.DE);

        return !string.IsNullOrEmpty(location.Id) && location.Id.Length == 32;
    }

    [Property(MaxTest = 50)]
    public bool DataLocation_Create_AlwaysGeneratesUniqueIds(NonEmptyString entityId)
    {
        var loc1 = DataLocation.Create(entityId.Get, "data", RegionRegistry.DE);
        var loc2 = DataLocation.Create(entityId.Get, "data", RegionRegistry.DE);

        return loc1.Id != loc2.Id;
    }

    [Property(MaxTest = 50)]
    public bool DataLocation_Create_PreservesEntityId(NonEmptyString entityId)
    {
        var location = DataLocation.Create(entityId.Get, "data", RegionRegistry.DE);
        return location.EntityId == entityId.Get;
    }

    [Property(MaxTest = 50)]
    public bool DataLocation_Create_PreservesDataCategory(NonEmptyString category)
    {
        var location = DataLocation.Create("entity-1", category.Get, RegionRegistry.DE);
        return location.DataCategory == category.Get;
    }

    [Property(MaxTest = 50)]
    public bool ResidencyAuditEntry_Create_AlwaysGeneratesNonEmptyId(NonEmptyString category)
    {
        var entry = ResidencyAuditEntry.Create(
            dataCategory: category.Get,
            sourceRegion: "DE",
            action: ResidencyAction.PolicyCheck,
            outcome: ResidencyOutcome.Allowed);

        return !string.IsNullOrEmpty(entry.Id) && entry.Id.Length == 32;
    }

    [Property(MaxTest = 50)]
    public bool ResidencyAuditEntry_Create_AlwaysGeneratesUniqueIds(NonEmptyString category)
    {
        var e1 = ResidencyAuditEntry.Create(category.Get, "DE", ResidencyAction.PolicyCheck, ResidencyOutcome.Allowed);
        var e2 = ResidencyAuditEntry.Create(category.Get, "DE", ResidencyAction.PolicyCheck, ResidencyOutcome.Allowed);

        return e1.Id != e2.Id;
    }

    [Property(MaxTest = 50)]
    public bool TransferValidationResult_Allow_AlwaysHasIsAllowedTrue()
    {
        return TransferValidationResult.Allow(TransferLegalBasis.AdequacyDecision).IsAllowed;
    }

    [Property(MaxTest = 50)]
    public bool TransferValidationResult_Deny_AlwaysHasIsAllowedFalse(NonEmptyString reason)
    {
        return !TransferValidationResult.Deny(reason.Get).IsAllowed;
    }

    [Property(MaxTest = 50)]
    public bool TransferValidationResult_Deny_PreservesDenialReason(NonEmptyString reason)
    {
        var result = TransferValidationResult.Deny(reason.Get);
        return result.DenialReason == reason.Get;
    }

    [Property(MaxTest = 50)]
    public bool ResidencyPolicyDescriptor_Create_PreservesDataCategory(NonEmptyString category)
    {
        var policy = ResidencyPolicyDescriptor.Create(category.Get, [RegionRegistry.DE]);
        return policy.DataCategory == category.Get;
    }
}
