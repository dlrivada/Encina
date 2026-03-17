using Encina.Compliance.Retention.Aggregates;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.Retention;

/// <summary>
/// Property-based tests for <see cref="LegalHoldAggregate"/> verifying lifecycle
/// invariants across randomized inputs using FsCheck.
/// </summary>
[Trait("Category", "Property")]
public class LegalHoldAggregatePropertyTests
{
    #region Factory Invariants

    /// <summary>
    /// Invariant: A newly placed legal hold is always active regardless of the entity ID,
    /// reason, or user who applied it.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Place_AlwaysActive()
    {
        var entityGen = Arb.From(Gen.Elements(
            "customer-001",
            "order-999",
            "employee-42",
            "contract-7"));

        var reasonGen = Arb.From(Gen.Elements(
            "Ongoing litigation - Case #12345",
            "Regulatory investigation",
            "Tax audit hold",
            "HR dispute pending"));

        return Prop.ForAll(entityGen, reasonGen, (entityId, reason) =>
        {
            var aggregate = LegalHoldAggregate.Place(
                Guid.NewGuid(),
                entityId,
                reason,
                appliedByUserId: "legal-counsel-1",
                appliedAtUtc: DateTimeOffset.UtcNow);

            return aggregate.IsActive;
        });
    }

    /// <summary>
    /// Invariant: A newly placed legal hold always stores the exact entity ID provided,
    /// regardless of which entity string is used.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Place_AlwaysSetsEntityId()
    {
        var entityGen = Arb.From(Gen.Elements(
            "customer-001",
            "order-999",
            "employee-42",
            "contract-7"));

        return Prop.ForAll(entityGen, entityId =>
        {
            var aggregate = LegalHoldAggregate.Place(
                Guid.NewGuid(),
                entityId,
                reason: "Ongoing litigation",
                appliedByUserId: "legal-counsel-1",
                appliedAtUtc: DateTimeOffset.UtcNow);

            return aggregate.EntityId == entityId;
        });
    }

    #endregion

    #region Lift Invariants

    /// <summary>
    /// Invariant: After lifting a legal hold, IsActive is always false regardless of the
    /// entity ID, reason, or original user who placed the hold.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Lift_AlwaysSetsInactive()
    {
        var entityGen = Arb.From(Gen.Elements(
            "customer-001",
            "order-999",
            "employee-42",
            "contract-7"));

        var reasonGen = Arb.From(Gen.Elements(
            "Ongoing litigation - Case #12345",
            "Regulatory investigation",
            "Tax audit hold",
            "HR dispute pending"));

        return Prop.ForAll(entityGen, reasonGen, (entityId, reason) =>
        {
            var now = DateTimeOffset.UtcNow;
            var aggregate = LegalHoldAggregate.Place(
                Guid.NewGuid(),
                entityId,
                reason,
                appliedByUserId: "legal-counsel-1",
                appliedAtUtc: now);

            aggregate.Lift(releasedByUserId: "legal-counsel-2", releasedAtUtc: now.AddDays(30));

            return !aggregate.IsActive;
        });
    }

    /// <summary>
    /// Invariant: After lifting a legal hold, ReleasedByUserId always matches the user
    /// who performed the lift, regardless of which entity or reason was originally used.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Lift_AlwaysSetsReleasedBy()
    {
        var entityGen = Arb.From(Gen.Elements(
            "customer-001",
            "order-999",
            "employee-42",
            "contract-7"));

        var userGen = Arb.From(Gen.Elements(
            "legal-counsel-1",
            "legal-counsel-2",
            "compliance-officer",
            "dpo-admin"));

        return Prop.ForAll(entityGen, userGen, (entityId, releasedByUserId) =>
        {
            var now = DateTimeOffset.UtcNow;
            var aggregate = LegalHoldAggregate.Place(
                Guid.NewGuid(),
                entityId,
                reason: "Ongoing litigation",
                appliedByUserId: "legal-counsel-1",
                appliedAtUtc: now);

            aggregate.Lift(releasedByUserId, releasedAtUtc: now.AddDays(30));

            return aggregate.ReleasedByUserId == releasedByUserId;
        });
    }

    #endregion
}
