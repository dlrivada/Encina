using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.Model;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.Retention;

/// <summary>
/// Property-based tests for <see cref="RetentionPolicyAggregate"/> verifying lifecycle
/// invariants across randomized inputs using FsCheck.
/// </summary>
[Trait("Category", "Property")]
public class RetentionPolicyAggregatePropertyTests
{
    #region Factory Invariants

    /// <summary>
    /// Invariant: A newly created retention policy is always active regardless of the
    /// data category, retention period, or policy type used.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Create_AlwaysActive()
    {
        var catGen = Arb.From(Gen.Elements(
            "customer-data",
            "financial-records",
            "session-logs",
            "marketing-consent"));

        var periodGen = Arb.From(Gen.Choose(1, 3650).Select(d => TimeSpan.FromDays(d)));

        var policyTypeGen = Arb.From(Gen.Elements(
            RetentionPolicyType.TimeBased,
            RetentionPolicyType.EventBased,
            RetentionPolicyType.ConsentBased));

        return Prop.ForAll(catGen, periodGen, policyTypeGen, (category, period, policyType) =>
        {
            var aggregate = RetentionPolicyAggregate.Create(
                Guid.NewGuid(),
                category,
                period,
                autoDelete: false,
                policyType,
                reason: null,
                legalBasis: null,
                occurredAtUtc: DateTimeOffset.UtcNow);

            return aggregate.IsActive;
        });
    }

    /// <summary>
    /// Invariant: A newly created retention policy always stores the exact data category
    /// provided, regardless of which category string is used.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Create_AlwaysSetsDataCategory()
    {
        var catGen = Arb.From(Gen.Elements(
            "customer-data",
            "financial-records",
            "session-logs",
            "marketing-consent"));

        var policyTypeGen = Arb.From(Gen.Elements(
            RetentionPolicyType.TimeBased,
            RetentionPolicyType.EventBased,
            RetentionPolicyType.ConsentBased));

        return Prop.ForAll(catGen, policyTypeGen, (category, policyType) =>
        {
            var aggregate = RetentionPolicyAggregate.Create(
                Guid.NewGuid(),
                category,
                TimeSpan.FromDays(365),
                autoDelete: false,
                policyType,
                reason: null,
                legalBasis: null,
                occurredAtUtc: DateTimeOffset.UtcNow);

            return aggregate.DataCategory == category;
        });
    }

    /// <summary>
    /// Invariant: A newly created retention policy always has a positive retention period —
    /// the stored period is never zero or negative.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Create_RetentionPeriodAlwaysPositive()
    {
        var periodGen = Arb.From(Gen.Choose(1, 3650).Select(d => TimeSpan.FromDays(d)));

        var policyTypeGen = Arb.From(Gen.Elements(
            RetentionPolicyType.TimeBased,
            RetentionPolicyType.EventBased,
            RetentionPolicyType.ConsentBased));

        return Prop.ForAll(periodGen, policyTypeGen, (period, policyType) =>
        {
            var aggregate = RetentionPolicyAggregate.Create(
                Guid.NewGuid(),
                "customer-data",
                period,
                autoDelete: false,
                policyType,
                reason: null,
                legalBasis: null,
                occurredAtUtc: DateTimeOffset.UtcNow);

            return aggregate.RetentionPeriod > TimeSpan.Zero;
        });
    }

    #endregion

    #region Update Invariants

    /// <summary>
    /// Invariant: After updating an active policy, the stored retention period always
    /// reflects the newly supplied value exactly.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Update_AlwaysUpdatesRetentionPeriod()
    {
        var initialPeriodGen = Arb.From(Gen.Choose(1, 1825).Select(d => TimeSpan.FromDays(d)));
        var newPeriodGen = Arb.From(Gen.Choose(1826, 3650).Select(d => TimeSpan.FromDays(d)));

        return Prop.ForAll(initialPeriodGen, newPeriodGen, (initial, updated) =>
        {
            var now = DateTimeOffset.UtcNow;
            var aggregate = RetentionPolicyAggregate.Create(
                Guid.NewGuid(),
                "financial-records",
                initial,
                autoDelete: false,
                RetentionPolicyType.TimeBased,
                reason: null,
                legalBasis: null,
                occurredAtUtc: now);

            aggregate.Update(updated, autoDelete: true, reason: "Extended for compliance", legalBasis: null, occurredAtUtc: now.AddHours(1));

            return aggregate.RetentionPeriod == updated;
        });
    }

    #endregion

    #region Deactivation Invariants

    /// <summary>
    /// Invariant: After deactivating a policy, IsActive is always false regardless of
    /// the data category or retention period the policy was created with.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Deactivate_AlwaysSetsInactive()
    {
        var catGen = Arb.From(Gen.Elements(
            "customer-data",
            "financial-records",
            "session-logs",
            "marketing-consent"));

        var periodGen = Arb.From(Gen.Choose(1, 3650).Select(d => TimeSpan.FromDays(d)));

        return Prop.ForAll(catGen, periodGen, (category, period) =>
        {
            var now = DateTimeOffset.UtcNow;
            var aggregate = RetentionPolicyAggregate.Create(
                Guid.NewGuid(),
                category,
                period,
                autoDelete: false,
                RetentionPolicyType.TimeBased,
                reason: null,
                legalBasis: null,
                occurredAtUtc: now);

            aggregate.Deactivate("Policy superseded", occurredAtUtc: now.AddHours(1));

            return !aggregate.IsActive;
        });
    }

    #endregion
}
