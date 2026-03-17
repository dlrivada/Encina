using Encina.Compliance.Retention.Events;
using Encina.Compliance.Retention.Model;
using Encina.Marten.Projections;

namespace Encina.Compliance.Retention.ReadModels;

/// <summary>
/// Marten inline projection that transforms retention record aggregate events into <see cref="RetentionRecordReadModel"/> state.
/// </summary>
/// <remarks>
/// <para>
/// This projection implements the "Query" side of CQRS for retention record management.
/// It handles all 6 retention record event types, creating or updating the
/// <see cref="RetentionRecordReadModel"/> as events are applied.
/// </para>
/// <para>
/// <b>Event Handling</b>:
/// <list type="bullet">
///   <item><description><see cref="RetentionRecordTracked"/> — Creates a new read model in <see cref="RetentionStatus.Active"/> status (first event in stream)</description></item>
///   <item><description><see cref="RetentionRecordExpired"/> — Transitions to <see cref="RetentionStatus.Expired"/></description></item>
///   <item><description><see cref="RetentionRecordHeld"/> — Transitions to <see cref="RetentionStatus.UnderLegalHold"/> per Art. 17(3)(e)</description></item>
///   <item><description><see cref="RetentionRecordReleased"/> — Recalculates status based on <see cref="RetentionRecordReadModel.ExpiresAtUtc"/></description></item>
///   <item><description><see cref="DataDeleted"/> — Transitions to <see cref="RetentionStatus.Deleted"/> (terminal) per Art. 5(1)(e)</description></item>
///   <item><description><see cref="DataAnonymized"/> — Transitions to <see cref="RetentionStatus.Deleted"/> (terminal) per Recital 26</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Idempotency</b>: The projection is idempotent — applying the same event multiple times
/// produces the same result. This enables safe replay and rebuild of the read model.
/// </para>
/// </remarks>
public sealed class RetentionRecordProjection :
    IProjection<RetentionRecordReadModel>,
    IProjectionCreator<RetentionRecordTracked, RetentionRecordReadModel>,
    IProjectionHandler<RetentionRecordExpired, RetentionRecordReadModel>,
    IProjectionHandler<RetentionRecordHeld, RetentionRecordReadModel>,
    IProjectionHandler<RetentionRecordReleased, RetentionRecordReadModel>,
    IProjectionHandler<DataDeleted, RetentionRecordReadModel>,
    IProjectionHandler<DataAnonymized, RetentionRecordReadModel>
{
    /// <inheritdoc />
    public string ProjectionName => "RetentionRecordProjection";

    /// <summary>
    /// Creates a new <see cref="RetentionRecordReadModel"/> from a <see cref="RetentionRecordTracked"/> event.
    /// </summary>
    /// <remarks>
    /// This is the first event in a retention record aggregate stream. It initializes all fields
    /// including the entity identifier, data category, retention period, and calculated expiration
    /// timestamp per GDPR Article 5(1)(e) storage limitation.
    /// </remarks>
    /// <param name="domainEvent">The retention record tracked event.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>A new <see cref="RetentionRecordReadModel"/> in <see cref="RetentionStatus.Active"/> status.</returns>
    public RetentionRecordReadModel Create(RetentionRecordTracked domainEvent, ProjectionContext context)
    {
        return new RetentionRecordReadModel
        {
            Id = domainEvent.RecordId,
            EntityId = domainEvent.EntityId,
            DataCategory = domainEvent.DataCategory,
            PolicyId = domainEvent.PolicyId,
            RetentionPeriod = domainEvent.RetentionPeriod,
            Status = RetentionStatus.Active,
            ExpiresAtUtc = domainEvent.ExpiresAtUtc,
            TenantId = domainEvent.TenantId,
            ModuleId = domainEvent.ModuleId,
            CreatedAtUtc = domainEvent.OccurredAtUtc,
            LastModifiedAtUtc = domainEvent.OccurredAtUtc,
            Version = 1
        };
    }

    /// <summary>
    /// Updates the read model when a retention record's retention period has elapsed.
    /// </summary>
    /// <remarks>
    /// Transitions to <see cref="RetentionStatus.Expired"/>. The entity is now eligible for deletion
    /// unless protected by a legal hold (GDPR Article 17(3)(e)).
    /// </remarks>
    /// <param name="domainEvent">The retention record expired event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model in <see cref="RetentionStatus.Expired"/> status.</returns>
    public RetentionRecordReadModel Apply(RetentionRecordExpired domainEvent, RetentionRecordReadModel current, ProjectionContext context)
    {
        current.Status = RetentionStatus.Expired;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when a legal hold is applied to the retention record.
    /// </summary>
    /// <remarks>
    /// Transitions to <see cref="RetentionStatus.UnderLegalHold"/>, preventing deletion regardless
    /// of whether the retention period has expired. This supports GDPR Article 17(3)(e):
    /// "processing is necessary for the establishment, exercise or defence of legal claims."
    /// </remarks>
    /// <param name="domainEvent">The retention record held event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model in <see cref="RetentionStatus.UnderLegalHold"/> status.</returns>
    public RetentionRecordReadModel Apply(RetentionRecordHeld domainEvent, RetentionRecordReadModel current, ProjectionContext context)
    {
        current.Status = RetentionStatus.UnderLegalHold;
        current.LegalHoldId = domainEvent.LegalHoldId;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when a legal hold is released from the retention record.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Recalculates the status based on the current time relative to <see cref="RetentionRecordReadModel.ExpiresAtUtc"/>:
    /// if the expiration has passed, the record transitions to <see cref="RetentionStatus.Expired"/>;
    /// otherwise, it returns to <see cref="RetentionStatus.Active"/>.
    /// </para>
    /// <para>
    /// The enforcement service will re-evaluate the record during its next sweep to determine
    /// whether deletion should proceed.
    /// </para>
    /// </remarks>
    /// <param name="domainEvent">The retention record released event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model with recalculated status.</returns>
    public RetentionRecordReadModel Apply(RetentionRecordReleased domainEvent, RetentionRecordReadModel current, ProjectionContext context)
    {
        current.Status = domainEvent.OccurredAtUtc >= current.ExpiresAtUtc
            ? RetentionStatus.Expired
            : RetentionStatus.Active;
        current.LegalHoldId = null;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when the data entity has been physically deleted.
    /// </summary>
    /// <remarks>
    /// Transitions to <see cref="RetentionStatus.Deleted"/> (terminal). This event provides an
    /// immutable audit trail proving that data was deleted in compliance with GDPR Article 5(1)(e)
    /// storage limitation and, where applicable, Article 17(1) right to erasure.
    /// </remarks>
    /// <param name="domainEvent">The data deleted event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model in <see cref="RetentionStatus.Deleted"/> status.</returns>
    public RetentionRecordReadModel Apply(DataDeleted domainEvent, RetentionRecordReadModel current, ProjectionContext context)
    {
        current.Status = RetentionStatus.Deleted;
        current.DeletedAtUtc = domainEvent.DeletedAtUtc;
        current.LastModifiedAtUtc = domainEvent.DeletedAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when the data entity has been anonymized.
    /// </summary>
    /// <remarks>
    /// Transitions to <see cref="RetentionStatus.Deleted"/> (terminal). Per GDPR Recital 26,
    /// anonymized data falls outside GDPR scope. The anonymization timestamp is recorded
    /// separately from deletion to distinguish the two terminal paths.
    /// </remarks>
    /// <param name="domainEvent">The data anonymized event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model in <see cref="RetentionStatus.Deleted"/> status.</returns>
    public RetentionRecordReadModel Apply(DataAnonymized domainEvent, RetentionRecordReadModel current, ProjectionContext context)
    {
        current.Status = RetentionStatus.Deleted;
        current.AnonymizedAtUtc = domainEvent.AnonymizedAtUtc;
        current.LastModifiedAtUtc = domainEvent.AnonymizedAtUtc;
        current.Version++;
        return current;
    }
}
