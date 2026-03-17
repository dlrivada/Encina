namespace Encina.Compliance.Retention.Events;

// Event-sourced events implement INotification so they are automatically published
// by Encina.Marten's EventPublishingPipelineBehavior after successful command execution.
// This allows handlers to react to aggregate state changes without a separate notification layer.

/// <summary>
/// Raised when a data entity begins being tracked for retention under a specific policy.
/// </summary>
/// <remarks>
/// <para>
/// Initiates the retention record lifecycle for an individual data entity. The
/// <paramref name="ExpiresAtUtc"/> is calculated as the current time plus the policy's
/// <paramref name="RetentionPeriod"/>, establishing when the data becomes eligible for
/// deletion per GDPR Article 5(1)(e) storage limitation.
/// </para>
/// <para>
/// This event is typically raised by the <c>RetentionValidationPipelineBehavior</c> when
/// it intercepts a response decorated with <c>[RetentionPeriod]</c>, or by the enforcement
/// service when explicitly tracking a new entity.
/// </para>
/// </remarks>
/// <param name="RecordId">Unique identifier for this retention record aggregate.</param>
/// <param name="EntityId">The identifier of the data entity being tracked (e.g., customer ID, order ID).</param>
/// <param name="DataCategory">The data category this entity belongs to (must match a retention policy).</param>
/// <param name="PolicyId">The retention policy aggregate ID governing this entity's lifecycle.</param>
/// <param name="RetentionPeriod">The retention period copied from the policy at tracking time.</param>
/// <param name="ExpiresAtUtc">The calculated expiration timestamp (UTC) — when the entity becomes eligible for deletion.</param>
/// <param name="OccurredAtUtc">Timestamp when the entity was tracked (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record RetentionRecordTracked(
    Guid RecordId,
    string EntityId,
    string DataCategory,
    Guid PolicyId,
    TimeSpan RetentionPeriod,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset OccurredAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when a retention record's retention period has elapsed and the data is eligible for deletion.
/// </summary>
/// <remarks>
/// <para>
/// Transitions the record from <c>Active</c> to <c>Expired</c> status. The enforcement service
/// detects this condition during its periodic sweep by comparing <c>ExpiresAtUtc</c> with the
/// current time.
/// </para>
/// <para>
/// An expired record may still be protected from deletion by a legal hold
/// (GDPR Article 17(3)(e) — legal claims exemption). The enforcement service must check
/// for active holds before proceeding with deletion.
/// </para>
/// </remarks>
/// <param name="RecordId">The retention record aggregate identifier.</param>
/// <param name="EntityId">The identifier of the expired data entity.</param>
/// <param name="DataCategory">The data category of the expired entity.</param>
/// <param name="OccurredAtUtc">Timestamp when expiration was detected (UTC).</param>
public sealed record RetentionRecordExpired(
    Guid RecordId,
    string EntityId,
    string DataCategory,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when a legal hold is applied to a retention record, suspending deletion.
/// </summary>
/// <remarks>
/// <para>
/// Transitions the record to <c>UnderLegalHold</c> status, preventing deletion regardless
/// of whether the retention period has expired. This supports GDPR Article 17(3)(e):
/// "processing is necessary for the establishment, exercise or defence of legal claims."
/// </para>
/// <para>
/// This event is raised as part of the cross-aggregate coordination when
/// <c>ILegalHoldService.PlaceHoldAsync()</c> creates a <c>LegalHoldAggregate</c> and
/// propagates the hold to affected retention records.
/// </para>
/// </remarks>
/// <param name="RecordId">The retention record aggregate identifier.</param>
/// <param name="EntityId">The identifier of the data entity being held.</param>
/// <param name="LegalHoldId">The identifier of the legal hold aggregate that triggered this hold.</param>
/// <param name="OccurredAtUtc">Timestamp when the hold was applied (UTC).</param>
public sealed record RetentionRecordHeld(
    Guid RecordId,
    string EntityId,
    Guid LegalHoldId,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when a legal hold is released from a retention record, resuming normal retention lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// If no other active legal holds remain on this entity, the record returns to its previous
/// status (<c>Active</c> if not yet expired, <c>Expired</c> if past the deadline). The
/// enforcement service will re-evaluate the record during its next sweep.
/// </para>
/// <para>
/// This event is raised as part of the cross-aggregate coordination when
/// <c>ILegalHoldService.LiftHoldAsync()</c> lifts a <c>LegalHoldAggregate</c> and
/// propagates the release to affected retention records (only if no other active holds remain).
/// </para>
/// </remarks>
/// <param name="RecordId">The retention record aggregate identifier.</param>
/// <param name="EntityId">The identifier of the data entity being released.</param>
/// <param name="LegalHoldId">The identifier of the legal hold aggregate that was lifted.</param>
/// <param name="OccurredAtUtc">Timestamp when the hold was released (UTC).</param>
public sealed record RetentionRecordReleased(
    Guid RecordId,
    string EntityId,
    Guid LegalHoldId,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when the data entity tracked by a retention record has been physically deleted.
/// </summary>
/// <remarks>
/// <para>
/// Transitions the record to <c>Deleted</c> status. This is a terminal state — no further
/// state changes are permitted. The event provides an immutable audit trail proving that
/// data was deleted in compliance with GDPR Article 5(1)(e) storage limitation and, where
/// applicable, Article 17(1) right to erasure.
/// </para>
/// <para>
/// Physical deletion is performed by the <c>IDataErasureExecutor</c> (from
/// <c>Encina.Compliance.DataSubjectRights</c>) before this event is raised.
/// </para>
/// </remarks>
/// <param name="RecordId">The retention record aggregate identifier.</param>
/// <param name="EntityId">The identifier of the deleted data entity.</param>
/// <param name="DataCategory">The data category of the deleted entity.</param>
/// <param name="PolicyId">The retention policy aggregate ID that governed the entity's lifecycle.</param>
/// <param name="DeletedAtUtc">Timestamp when the data was physically deleted (UTC).</param>
public sealed record DataDeleted(
    Guid RecordId,
    string EntityId,
    string DataCategory,
    Guid PolicyId,
    DateTimeOffset DeletedAtUtc) : INotification;

/// <summary>
/// Raised when the data entity tracked by a retention record has been anonymized instead of deleted.
/// </summary>
/// <remarks>
/// <para>
/// Transitions the record to <c>Deleted</c> status (terminal). Anonymization is an alternative
/// to deletion where the data is stripped of personally identifiable information but the
/// underlying record structure is preserved for statistical or archival purposes.
/// </para>
/// <para>
/// Per GDPR Recital 26, the principles of data protection should not apply to anonymous
/// information, namely information which does not relate to an identified or identifiable
/// natural person. Anonymized data falls outside GDPR scope.
/// </para>
/// </remarks>
/// <param name="RecordId">The retention record aggregate identifier.</param>
/// <param name="EntityId">The identifier of the anonymized data entity.</param>
/// <param name="DataCategory">The data category of the anonymized entity.</param>
/// <param name="PolicyId">The retention policy aggregate ID that governed the entity's lifecycle.</param>
/// <param name="AnonymizedAtUtc">Timestamp when the data was anonymized (UTC).</param>
public sealed record DataAnonymized(
    Guid RecordId,
    string EntityId,
    string DataCategory,
    Guid PolicyId,
    DateTimeOffset AnonymizedAtUtc) : INotification;
