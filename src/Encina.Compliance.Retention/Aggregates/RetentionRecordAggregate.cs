using Encina.Compliance.Retention.Events;
using Encina.Compliance.Retention.Model;
using Encina.DomainModeling;

namespace Encina.Compliance.Retention.Aggregates;

/// <summary>
/// Event-sourced aggregate tracking the retention lifecycle of an individual data entity.
/// </summary>
/// <remarks>
/// <para>
/// Each aggregate instance represents one data entity (identified by <see cref="EntityId"/>)
/// being tracked for retention under a specific <see cref="RetentionPolicyAggregate"/>.
/// The lifecycle progresses through: <see cref="RetentionStatus.Active"/> →
/// <see cref="RetentionStatus.Expired"/> → <see cref="RetentionStatus.Deleted"/> (terminal),
/// with an optional <see cref="RetentionStatus.UnderLegalHold"/> state that suspends deletion.
/// </para>
/// <para>
/// Per GDPR Article 5(1)(e), personal data shall be "kept in a form which permits identification
/// of data subjects for no longer than is necessary." This aggregate enforces that principle by
/// tracking when each entity's retention period expires and recording the actual deletion or
/// anonymization event for Article 5(2) accountability.
/// </para>
/// <para>
/// Legal holds (GDPR Article 17(3)(e) — legal claims exemption) override the normal lifecycle:
/// when a hold is placed, the record transitions to <see cref="RetentionStatus.UnderLegalHold"/>
/// regardless of expiration status. When the hold is released and no other holds remain,
/// the record returns to its previous lifecycle position.
/// </para>
/// </remarks>
public sealed class RetentionRecordAggregate : AggregateBase
{
    /// <summary>
    /// The identifier of the data entity being tracked (e.g., customer ID, order ID).
    /// </summary>
    public string EntityId { get; private set; } = string.Empty;

    /// <summary>
    /// The data category this entity belongs to (matches a retention policy).
    /// </summary>
    public string DataCategory { get; private set; } = string.Empty;

    /// <summary>
    /// The retention policy aggregate ID governing this entity's lifecycle.
    /// </summary>
    public Guid PolicyId { get; private set; }

    /// <summary>
    /// The retention period copied from the policy at tracking time.
    /// </summary>
    public TimeSpan RetentionPeriod { get; private set; }

    /// <summary>
    /// Current lifecycle status of this retention record.
    /// </summary>
    public RetentionStatus Status { get; private set; }

    /// <summary>
    /// The calculated expiration timestamp (UTC) — when the entity becomes eligible for deletion.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    /// <summary>
    /// The identifier of the legal hold currently applied, or <see langword="null"/> if not under hold.
    /// </summary>
    /// <remarks>
    /// When multiple holds exist for the same entity, this tracks the most recently applied hold.
    /// The full hold history is available via the event stream.
    /// </remarks>
    public Guid? LegalHoldId { get; private set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; private set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; private set; }

    /// <summary>
    /// The UTC timestamp when this record was first tracked.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>
    /// The UTC timestamp when this record was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAtUtc { get; private set; }

    /// <summary>
    /// Begins tracking a data entity for retention under a specific policy.
    /// </summary>
    /// <param name="id">Unique identifier for the new retention record aggregate.</param>
    /// <param name="entityId">The identifier of the data entity being tracked.</param>
    /// <param name="dataCategory">The data category this entity belongs to.</param>
    /// <param name="policyId">The retention policy aggregate ID governing this entity.</param>
    /// <param name="retentionPeriod">The retention period copied from the policy.</param>
    /// <param name="expiresAtUtc">The calculated expiration timestamp (UTC).</param>
    /// <param name="occurredAtUtc">Timestamp when the entity was tracked (UTC).</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <returns>A new <see cref="RetentionRecordAggregate"/> in <see cref="RetentionStatus.Active"/> status.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="entityId"/> or <paramref name="dataCategory"/> is null or whitespace.</exception>
    public static RetentionRecordAggregate Track(
        Guid id,
        string entityId,
        string dataCategory,
        Guid policyId,
        TimeSpan retentionPeriod,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset occurredAtUtc,
        string? tenantId = null,
        string? moduleId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        var aggregate = new RetentionRecordAggregate();
        aggregate.RaiseEvent(new RetentionRecordTracked(
            id, entityId, dataCategory, policyId, retentionPeriod,
            expiresAtUtc, occurredAtUtc, tenantId, moduleId));
        return aggregate;
    }

    /// <summary>
    /// Marks the retention record as expired, indicating the retention period has elapsed.
    /// </summary>
    /// <remarks>
    /// The entity is now eligible for deletion unless a legal hold prevents it.
    /// Only records in <see cref="RetentionStatus.Active"/> status can be marked as expired.
    /// </remarks>
    /// <param name="occurredAtUtc">Timestamp when expiration was detected (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the record is not in <see cref="RetentionStatus.Active"/> status.</exception>
    public void MarkExpired(DateTimeOffset occurredAtUtc)
    {
        if (Status != RetentionStatus.Active)
        {
            throw new InvalidOperationException(
                $"Cannot mark record '{Id}' as expired because it is in '{Status}' status. " +
                "Only active records can be marked as expired.");
        }

        RaiseEvent(new RetentionRecordExpired(Id, EntityId, DataCategory, occurredAtUtc));
    }

    /// <summary>
    /// Places a legal hold on this retention record, suspending deletion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Transitions the record to <see cref="RetentionStatus.UnderLegalHold"/> status,
    /// preventing deletion per GDPR Article 17(3)(e) — legal claims exemption.
    /// </para>
    /// <para>
    /// A record can be held from <see cref="RetentionStatus.Active"/> or
    /// <see cref="RetentionStatus.Expired"/> status. Records that are already deleted
    /// or anonymized cannot be held.
    /// </para>
    /// </remarks>
    /// <param name="legalHoldId">The identifier of the legal hold aggregate that triggered this hold.</param>
    /// <param name="occurredAtUtc">Timestamp when the hold was applied (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the record is in <see cref="RetentionStatus.Deleted"/> status.</exception>
    public void Hold(Guid legalHoldId, DateTimeOffset occurredAtUtc)
    {
        if (Status == RetentionStatus.Deleted)
        {
            throw new InvalidOperationException(
                $"Cannot place a legal hold on record '{Id}' because it has already been deleted.");
        }

        RaiseEvent(new RetentionRecordHeld(Id, EntityId, legalHoldId, occurredAtUtc));
    }

    /// <summary>
    /// Releases a legal hold from this retention record, resuming normal lifecycle processing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If no other active holds remain, the record returns to its previous position in the
    /// lifecycle (<see cref="RetentionStatus.Active"/> or <see cref="RetentionStatus.Expired"/>
    /// depending on <see cref="ExpiresAtUtc"/>). The enforcement service will re-evaluate
    /// the record during its next sweep.
    /// </para>
    /// <para>
    /// Only records in <see cref="RetentionStatus.UnderLegalHold"/> status can be released.
    /// </para>
    /// </remarks>
    /// <param name="legalHoldId">The identifier of the legal hold aggregate that was lifted.</param>
    /// <param name="occurredAtUtc">Timestamp when the hold was released (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the record is not in <see cref="RetentionStatus.UnderLegalHold"/> status.</exception>
    public void Release(Guid legalHoldId, DateTimeOffset occurredAtUtc)
    {
        if (Status != RetentionStatus.UnderLegalHold)
        {
            throw new InvalidOperationException(
                $"Cannot release record '{Id}' because it is not under a legal hold. Current status: '{Status}'.");
        }

        RaiseEvent(new RetentionRecordReleased(Id, EntityId, legalHoldId, occurredAtUtc));
    }

    /// <summary>
    /// Marks the data entity as physically deleted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a terminal state — no further state changes are permitted on this aggregate.
    /// The event provides an immutable audit trail proving that data was deleted in compliance
    /// with GDPR Article 5(1)(e) storage limitation.
    /// </para>
    /// <para>
    /// Only records in <see cref="RetentionStatus.Expired"/> status can be marked as deleted.
    /// Records under legal hold must first have their hold released.
    /// </para>
    /// </remarks>
    /// <param name="deletedAtUtc">Timestamp when the data was physically deleted (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the record is not in <see cref="RetentionStatus.Expired"/> status.</exception>
    public void MarkDeleted(DateTimeOffset deletedAtUtc)
    {
        if (Status != RetentionStatus.Expired)
        {
            throw new InvalidOperationException(
                $"Cannot delete record '{Id}' because it is in '{Status}' status. " +
                "Only expired records can be deleted.");
        }

        RaiseEvent(new DataDeleted(Id, EntityId, DataCategory, PolicyId, deletedAtUtc));
    }

    /// <summary>
    /// Marks the data entity as anonymized instead of deleted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a terminal state — no further state changes are permitted. Anonymization
    /// is an alternative to deletion where personally identifiable information is removed
    /// but the underlying record structure is preserved.
    /// </para>
    /// <para>
    /// Per GDPR Recital 26, anonymized data falls outside GDPR scope.
    /// </para>
    /// <para>
    /// Only records in <see cref="RetentionStatus.Expired"/> status can be anonymized.
    /// Records under legal hold must first have their hold released.
    /// </para>
    /// </remarks>
    /// <param name="anonymizedAtUtc">Timestamp when the data was anonymized (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the record is not in <see cref="RetentionStatus.Expired"/> status.</exception>
    public void MarkAnonymized(DateTimeOffset anonymizedAtUtc)
    {
        if (Status != RetentionStatus.Expired)
        {
            throw new InvalidOperationException(
                $"Cannot anonymize record '{Id}' because it is in '{Status}' status. " +
                "Only expired records can be anonymized.");
        }

        RaiseEvent(new DataAnonymized(Id, EntityId, DataCategory, PolicyId, anonymizedAtUtc));
    }

    /// <inheritdoc />
    protected override void Apply(object domainEvent)
    {
        switch (domainEvent)
        {
            case RetentionRecordTracked e:
                Id = e.RecordId;
                EntityId = e.EntityId;
                DataCategory = e.DataCategory;
                PolicyId = e.PolicyId;
                RetentionPeriod = e.RetentionPeriod;
                Status = RetentionStatus.Active;
                ExpiresAtUtc = e.ExpiresAtUtc;
                TenantId = e.TenantId;
                ModuleId = e.ModuleId;
                CreatedAtUtc = e.OccurredAtUtc;
                LastUpdatedAtUtc = e.OccurredAtUtc;
                break;

            case RetentionRecordExpired e:
                Status = RetentionStatus.Expired;
                LastUpdatedAtUtc = e.OccurredAtUtc;
                break;

            case RetentionRecordHeld e:
                Status = RetentionStatus.UnderLegalHold;
                LegalHoldId = e.LegalHoldId;
                LastUpdatedAtUtc = e.OccurredAtUtc;
                break;

            case RetentionRecordReleased e:
                Status = DateTimeOffset.UtcNow >= ExpiresAtUtc
                    ? RetentionStatus.Expired
                    : RetentionStatus.Active;
                LegalHoldId = null;
                LastUpdatedAtUtc = e.OccurredAtUtc;
                break;

            case DataDeleted e:
                Status = RetentionStatus.Deleted;
                LastUpdatedAtUtc = e.DeletedAtUtc;
                break;

            case DataAnonymized e:
                Status = RetentionStatus.Deleted;
                LastUpdatedAtUtc = e.AnonymizedAtUtc;
                break;
        }
    }
}
