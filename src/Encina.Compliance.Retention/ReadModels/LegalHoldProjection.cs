using Encina.Compliance.Retention.Events;
using Encina.Marten.Projections;

namespace Encina.Compliance.Retention.ReadModels;

/// <summary>
/// Marten inline projection that transforms legal hold aggregate events into <see cref="LegalHoldReadModel"/> state.
/// </summary>
/// <remarks>
/// <para>
/// This projection implements the "Query" side of CQRS for legal hold management.
/// It handles both legal hold event types, creating or updating the
/// <see cref="LegalHoldReadModel"/> as events are applied.
/// </para>
/// <para>
/// <b>Event Handling</b>:
/// <list type="bullet">
///   <item><description><see cref="LegalHoldPlaced"/> — Creates a new read model in active status (first event in stream)</description></item>
///   <item><description><see cref="LegalHoldLifted"/> — Records release details; marks hold as inactive</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Idempotency</b>: The projection is idempotent — applying the same event multiple times
/// produces the same result. This enables safe replay and rebuild of the read model.
/// </para>
/// </remarks>
public sealed class LegalHoldProjection :
    IProjection<LegalHoldReadModel>,
    IProjectionCreator<LegalHoldPlaced, LegalHoldReadModel>,
    IProjectionHandler<LegalHoldLifted, LegalHoldReadModel>
{
    /// <inheritdoc />
    public string ProjectionName => "LegalHoldProjection";

    /// <summary>
    /// Creates a new <see cref="LegalHoldReadModel"/> from a <see cref="LegalHoldPlaced"/> event.
    /// </summary>
    /// <remarks>
    /// This is the first event in a legal hold aggregate stream. It initializes all fields
    /// including the entity identifier, reason, and the user who placed the hold.
    /// Per GDPR Article 17(3)(e), this provides the audit trail for the hold placement.
    /// </remarks>
    /// <param name="domainEvent">The legal hold placed event.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>A new <see cref="LegalHoldReadModel"/> in active status.</returns>
    public LegalHoldReadModel Create(LegalHoldPlaced domainEvent, ProjectionContext context)
    {
        return new LegalHoldReadModel
        {
            Id = domainEvent.HoldId,
            EntityId = domainEvent.EntityId,
            Reason = domainEvent.Reason,
            AppliedByUserId = domainEvent.AppliedByUserId,
            IsActive = true,
            AppliedAtUtc = domainEvent.AppliedAtUtc,
            TenantId = domainEvent.TenantId,
            ModuleId = domainEvent.ModuleId,
            LastModifiedAtUtc = domainEvent.AppliedAtUtc,
            Version = 1
        };
    }

    /// <summary>
    /// Updates the read model when a legal hold is lifted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Once lifted, this hold no longer prevents deletion of the associated entity.
    /// Per GDPR Article 5(2) accountability, this event records who released the hold and when,
    /// providing a complete audit trail of the hold lifecycle.
    /// </para>
    /// <para>
    /// The <c>ILegalHoldService</c> performs cross-aggregate coordination after this event,
    /// checking for remaining active holds on the entity. If none remain, it raises
    /// <c>RetentionRecordReleased</c> events on the affected retention records.
    /// </para>
    /// </remarks>
    /// <param name="domainEvent">The legal hold lifted event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model with <see cref="LegalHoldReadModel.IsActive"/> set to <see langword="false"/>.</returns>
    public LegalHoldReadModel Apply(LegalHoldLifted domainEvent, LegalHoldReadModel current, ProjectionContext context)
    {
        current.IsActive = false;
        current.ReleasedByUserId = domainEvent.ReleasedByUserId;
        current.ReleasedAtUtc = domainEvent.ReleasedAtUtc;
        current.LastModifiedAtUtc = domainEvent.ReleasedAtUtc;
        current.Version++;
        return current;
    }
}
