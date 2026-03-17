using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.Model;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.Retention;

/// <summary>
/// Property-based tests for <see cref="RetentionRecordAggregate"/> verifying lifecycle
/// invariants across randomized inputs using FsCheck.
/// </summary>
[Trait("Category", "Property")]
public class RetentionRecordAggregatePropertyTests
{
    #region Factory Invariants

    /// <summary>
    /// Invariant: A newly tracked retention record is always in Active status regardless
    /// of the entity ID, data category, or retention period used.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Track_AlwaysActive()
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
            var aggregate = RetentionRecordAggregate.Track(
                Guid.NewGuid(),
                entityId: "entity-001",
                dataCategory: category,
                policyId: Guid.NewGuid(),
                retentionPeriod: period,
                expiresAtUtc: now.Add(period),
                occurredAtUtc: now);

            return aggregate.Status == RetentionStatus.Active;
        });
    }

    /// <summary>
    /// Invariant: The calculated expiration timestamp is always strictly after the creation
    /// timestamp — a freshly tracked record can never already be expired.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Track_ExpiresAtUtcAlwaysAfterCreation()
    {
        var periodGen = Arb.From(Gen.Choose(1, 3650).Select(d => TimeSpan.FromDays(d)));

        return Prop.ForAll(periodGen, period =>
        {
            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.Add(period);

            var aggregate = RetentionRecordAggregate.Track(
                Guid.NewGuid(),
                entityId: "entity-001",
                dataCategory: "customer-data",
                policyId: Guid.NewGuid(),
                retentionPeriod: period,
                expiresAtUtc: expiresAt,
                occurredAtUtc: now);

            return aggregate.ExpiresAtUtc > aggregate.CreatedAtUtc;
        });
    }

    #endregion

    #region Legal Hold Invariants

    /// <summary>
    /// Invariant: Placing a legal hold on an active record always transitions it to
    /// UnderLegalHold status regardless of the entity or hold ID used.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Hold_AlwaysSetsUnderLegalHold()
    {
        var catGen = Arb.From(Gen.Elements(
            "customer-data",
            "financial-records",
            "session-logs",
            "marketing-consent"));

        return Prop.ForAll(catGen, category =>
        {
            var now = DateTimeOffset.UtcNow;
            var aggregate = RetentionRecordAggregate.Track(
                Guid.NewGuid(),
                entityId: "entity-001",
                dataCategory: category,
                policyId: Guid.NewGuid(),
                retentionPeriod: TimeSpan.FromDays(365),
                expiresAtUtc: now.AddDays(365),
                occurredAtUtc: now);

            aggregate.Hold(Guid.NewGuid(), occurredAtUtc: now.AddHours(1));

            return aggregate.Status == RetentionStatus.UnderLegalHold;
        });
    }

    #endregion

    #region Expiration Invariants

    /// <summary>
    /// Invariant: Marking an active record as expired always transitions it to Expired
    /// status regardless of the data category or original retention period.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property MarkExpired_AlwaysSetsExpired()
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
            var aggregate = RetentionRecordAggregate.Track(
                Guid.NewGuid(),
                entityId: "entity-001",
                dataCategory: category,
                policyId: Guid.NewGuid(),
                retentionPeriod: period,
                expiresAtUtc: now.Add(period),
                occurredAtUtc: now);

            aggregate.MarkExpired(occurredAtUtc: now.AddDays(period.TotalDays + 1));

            return aggregate.Status == RetentionStatus.Expired;
        });
    }

    #endregion

    #region Deletion Invariants

    /// <summary>
    /// Invariant: Marking an expired record as deleted always transitions it to Deleted
    /// status regardless of the data category or policy used.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property MarkDeleted_AlwaysSetsDeleted()
    {
        var catGen = Arb.From(Gen.Elements(
            "customer-data",
            "financial-records",
            "session-logs",
            "marketing-consent"));

        return Prop.ForAll(catGen, category =>
        {
            var now = DateTimeOffset.UtcNow;
            var aggregate = RetentionRecordAggregate.Track(
                Guid.NewGuid(),
                entityId: "entity-001",
                dataCategory: category,
                policyId: Guid.NewGuid(),
                retentionPeriod: TimeSpan.FromDays(365),
                expiresAtUtc: now.AddDays(365),
                occurredAtUtc: now);

            aggregate.MarkExpired(occurredAtUtc: now.AddDays(366));
            aggregate.MarkDeleted(deletedAtUtc: now.AddDays(367));

            return aggregate.Status == RetentionStatus.Deleted;
        });
    }

    #endregion
}
