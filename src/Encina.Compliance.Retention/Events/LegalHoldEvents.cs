namespace Encina.Compliance.Retention.Events;

// Event-sourced events implement INotification so they are automatically published
// by Encina.Marten's EventPublishingPipelineBehavior after successful command execution.
// This allows handlers to react to aggregate state changes without a separate notification layer.

/// <summary>
/// Raised when a legal hold is placed on a data entity, suspending all deletion activity.
/// </summary>
/// <remarks>
/// <para>
/// Initiates the legal hold lifecycle. A legal hold prevents the deletion of data associated
/// with the specified <paramref name="EntityId"/> regardless of whether the data's retention
/// period has expired. This supports GDPR Article 17(3)(e): processing is necessary "for the
/// establishment, exercise or defence of legal claims."
/// </para>
/// <para>
/// Multiple legal holds may be active simultaneously for the same entity (e.g., multiple
/// ongoing litigation matters). The entity remains protected until ALL active holds are lifted.
/// </para>
/// <para>
/// When this event is raised, the <c>ILegalHoldService</c> also propagates the hold to
/// all <c>RetentionRecordAggregate</c> instances tracking the entity, raising
/// <c>RetentionRecordHeld</c> events for cross-aggregate coordination.
/// </para>
/// </remarks>
/// <param name="HoldId">Unique identifier for this legal hold aggregate.</param>
/// <param name="EntityId">The identifier of the data entity being held (e.g., customer ID, case ID).</param>
/// <param name="Reason">The legal reason for the hold (e.g., "Ongoing litigation - Case #12345").</param>
/// <param name="AppliedByUserId">Identifier of the user who placed the hold (typically legal counsel).</param>
/// <param name="AppliedAtUtc">Timestamp when the hold was placed (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record LegalHoldPlaced(
    Guid HoldId,
    string EntityId,
    string Reason,
    string AppliedByUserId,
    DateTimeOffset AppliedAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when a legal hold is lifted, allowing normal retention enforcement to resume.
/// </summary>
/// <remarks>
/// <para>
/// Terminates the legal hold lifecycle. Once lifted, the hold no longer prevents deletion
/// of the associated entity. However, if other active holds exist for the same entity,
/// deletion remains suspended until all holds are lifted.
/// </para>
/// <para>
/// Per GDPR Article 5(2) accountability, this event records who released the hold and when,
/// providing a complete audit trail of the hold lifecycle for compliance review.
/// </para>
/// <para>
/// When this event is raised, the <c>ILegalHoldService</c> checks for remaining active holds
/// on the entity. If none remain, it raises <c>RetentionRecordReleased</c> events on the
/// affected retention records to resume normal lifecycle processing.
/// </para>
/// </remarks>
/// <param name="HoldId">The legal hold aggregate identifier.</param>
/// <param name="EntityId">The identifier of the data entity being released.</param>
/// <param name="ReleasedByUserId">Identifier of the user who lifted the hold.</param>
/// <param name="ReleasedAtUtc">Timestamp when the hold was lifted (UTC).</param>
public sealed record LegalHoldLifted(
    Guid HoldId,
    string EntityId,
    string ReleasedByUserId,
    DateTimeOffset ReleasedAtUtc) : INotification;
