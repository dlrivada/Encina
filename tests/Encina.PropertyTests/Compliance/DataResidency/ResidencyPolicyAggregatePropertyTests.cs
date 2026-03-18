using Encina.Compliance.DataResidency.Aggregates;
using Encina.Compliance.DataResidency.Model;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.DataResidency;

/// <summary>
/// Property-based tests for <see cref="ResidencyPolicyAggregate"/> verifying lifecycle
/// invariants across randomized inputs using FsCheck.
/// </summary>
[Trait("Category", "Property")]
public class ResidencyPolicyAggregatePropertyTests
{
    #region Factory Invariants

    /// <summary>
    /// Invariant: A newly created residency policy is always active regardless of the
    /// data category, allowed regions, or transfer bases used.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Create_AlwaysSetsIsActiveToTrue()
    {
        var catGen = Arb.From(Gen.Elements("cat-a", "cat-b", "cat-c"));
        var regionGen = Arb.From(Gen.Elements("DE", "FR", "US"));

        return Prop.ForAll(catGen, regionGen, (category, region) =>
        {
            var aggregate = ResidencyPolicyAggregate.Create(
                Guid.NewGuid(),
                category,
                [region],
                requireAdequacyDecision: false,
                [TransferLegalBasis.AdequacyDecision]);

            return aggregate.IsActive;
        });
    }

    /// <summary>
    /// Invariant: A newly created residency policy always stores the exact data category
    /// provided, regardless of which category string is used.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Create_AlwaysPreservesDataCategory()
    {
        var catGen = Arb.From(Gen.Elements("cat-a", "cat-b", "cat-c"));

        return Prop.ForAll(catGen, (category) =>
        {
            var aggregate = ResidencyPolicyAggregate.Create(
                Guid.NewGuid(),
                category,
                ["DE"],
                requireAdequacyDecision: false,
                [TransferLegalBasis.StandardContractualClauses]);

            return aggregate.DataCategory == category;
        });
    }

    #endregion

    #region Update Invariants

    /// <summary>
    /// Invariant: After updating an active policy, the stored allowed region codes always
    /// reflect the newly supplied values exactly.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Update_AlwaysSetsNewAllowedRegionCodes()
    {
        var regionGen = Arb.From(Gen.Elements("DE", "FR", "US"));

        return Prop.ForAll(regionGen, (newRegion) =>
        {
            var aggregate = ResidencyPolicyAggregate.Create(
                Guid.NewGuid(),
                "cat-a",
                ["JP"],
                requireAdequacyDecision: false,
                [TransferLegalBasis.AdequacyDecision]);

            IReadOnlyList<string> newRegions = [newRegion];
            aggregate.Update(newRegions, false, [TransferLegalBasis.ExplicitConsent]);

            return aggregate.AllowedRegionCodes.SequenceEqual(newRegions);
        });
    }

    #endregion

    #region Delete Invariants

    /// <summary>
    /// Invariant: After deleting an active policy, IsActive is always false regardless of
    /// the data category or allowed regions the policy was created with.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Delete_AlwaysSetsIsActiveToFalse()
    {
        var catGen = Arb.From(Gen.Elements("cat-a", "cat-b", "cat-c"));
        var regionGen = Arb.From(Gen.Elements("DE", "FR", "US"));

        return Prop.ForAll(catGen, regionGen, (category, region) =>
        {
            var aggregate = ResidencyPolicyAggregate.Create(
                Guid.NewGuid(),
                category,
                [region],
                requireAdequacyDecision: false,
                [TransferLegalBasis.AdequacyDecision]);

            aggregate.Delete("Policy superseded");

            return !aggregate.IsActive;
        });
    }

    #endregion
}
