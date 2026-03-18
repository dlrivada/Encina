using Encina.Compliance.DataResidency.Aggregates;
using Encina.Compliance.DataResidency.Model;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.DataResidency;

/// <summary>
/// Property-based tests for <see cref="DataLocationAggregate"/> verifying lifecycle
/// invariants across randomized inputs using FsCheck.
/// </summary>
[Trait("Category", "Property")]
public class DataLocationAggregatePropertyTests
{
    #region Register Invariants

    /// <summary>
    /// Invariant: A newly registered data location always stores the exact region code
    /// provided, regardless of which region is used.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Register_AlwaysPreservesRegionCode()
    {
        var regionGen = Arb.From(Gen.Elements("DE", "FR", "US", "JP"));

        return Prop.ForAll(regionGen, (region) =>
        {
            var aggregate = DataLocationAggregate.Register(
                Guid.NewGuid(),
                "entity-1",
                "personal-data",
                region,
                StorageType.Primary,
                DateTimeOffset.UtcNow);

            return aggregate.RegionCode == region;
        });
    }

    /// <summary>
    /// Invariant: A newly registered data location always stores the exact entity ID
    /// provided, regardless of which entity is used.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Register_AlwaysPreservesEntityId()
    {
        var entityGen = Arb.From(Gen.Elements("entity-1", "entity-2"));

        return Prop.ForAll(entityGen, (entityId) =>
        {
            var aggregate = DataLocationAggregate.Register(
                Guid.NewGuid(),
                entityId,
                "personal-data",
                "DE",
                StorageType.Primary,
                DateTimeOffset.UtcNow);

            return aggregate.EntityId == entityId;
        });
    }

    #endregion

    #region Migrate Invariants

    /// <summary>
    /// Invariant: After migrating a data location, the region code always reflects the
    /// newly supplied value exactly.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Migrate_AlwaysUpdatesRegionCode()
    {
        var regionGen = Arb.From(Gen.Elements("DE", "FR", "US", "JP"));

        return Prop.ForAll(regionGen, (newRegion) =>
        {
            var aggregate = DataLocationAggregate.Register(
                Guid.NewGuid(),
                "entity-1",
                "personal-data",
                "GB",
                StorageType.Primary,
                DateTimeOffset.UtcNow);

            aggregate.Migrate(newRegion, "Compliance requirement");

            return aggregate.RegionCode == newRegion;
        });
    }

    #endregion

    #region Verify Invariants

    /// <summary>
    /// Invariant: After verifying a data location, the LastVerifiedAtUtc timestamp always
    /// matches the supplied verification time.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Verify_AlwaysUpdatesLastVerifiedAtUtc()
    {
        var regionGen = Arb.From(Gen.Elements("DE", "FR", "US", "JP"));

        return Prop.ForAll(regionGen, (region) =>
        {
            var aggregate = DataLocationAggregate.Register(
                Guid.NewGuid(),
                "entity-1",
                "personal-data",
                region,
                StorageType.Primary,
                DateTimeOffset.UtcNow);

            var verifiedAt = DateTimeOffset.UtcNow;
            aggregate.Verify(verifiedAt);

            return aggregate.LastVerifiedAtUtc == verifiedAt;
        });
    }

    #endregion

    #region Remove Invariants

    /// <summary>
    /// Invariant: After removing a data location, IsRemoved is always true regardless of
    /// the region code or entity ID the location was created with.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Remove_AlwaysSetsIsRemovedTrue()
    {
        var regionGen = Arb.From(Gen.Elements("DE", "FR", "US", "JP"));
        var entityGen = Arb.From(Gen.Elements("entity-1", "entity-2"));

        return Prop.ForAll(regionGen, entityGen, (region, entityId) =>
        {
            var aggregate = DataLocationAggregate.Register(
                Guid.NewGuid(),
                entityId,
                "personal-data",
                region,
                StorageType.Primary,
                DateTimeOffset.UtcNow);

            aggregate.Remove("Data deleted per retention policy");

            return aggregate.IsRemoved;
        });
    }

    #endregion

    #region Violation Invariants

    /// <summary>
    /// Invariant: After detecting a sovereignty violation, HasViolation is always true
    /// regardless of the violating region or data category.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property DetectViolation_AlwaysSetsHasViolation()
    {
        var regionGen = Arb.From(Gen.Elements("DE", "FR", "US", "JP"));

        return Prop.ForAll(regionGen, (violatingRegion) =>
        {
            var aggregate = DataLocationAggregate.Register(
                Guid.NewGuid(),
                "entity-1",
                "personal-data",
                "DE",
                StorageType.Primary,
                DateTimeOffset.UtcNow);

            aggregate.DetectViolation("personal-data", violatingRegion, "Non-compliant storage detected");

            return aggregate.HasViolation;
        });
    }

    #endregion
}
