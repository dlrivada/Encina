namespace Encina.Audit.Marten.Resolution;

/// <summary>
/// Immutable snapshot of an <see cref="AuditResolution"/> returned to callers.
/// </summary>
/// <param name="Trail">The audit trail of the resolved event.</param>
/// <param name="EventId">The Marten event identifier (<c>IEvent.Id</c>) of the resolved event.</param>
/// <param name="Field">The field the resolution applies to.</param>
/// <param name="Kind">How the field was resolved.</param>
/// <param name="AuditEntryId">The audit entry identifier carried by the event payload, or <c>null</c> when unknown.</param>
/// <param name="TenantId">The tenant of the resolved event, or <c>null</c> without a tenant.</param>
/// <param name="ErasureRequestId">The data-erasure request identifier of a shredded resolution, otherwise <c>null</c>.</param>
/// <param name="ErasureRequestedAtUtc">When the data-erasure request was made (UTC), otherwise <c>null</c>.</param>
/// <param name="ErasureActor">Who requested or approved the erasure, otherwise <c>null</c>.</param>
/// <param name="RetentionPolicyId">The retention policy identifier of an expired resolution, otherwise <c>null</c>.</param>
/// <param name="RetentionCutoffUtc">The cutoff the retention policy applied (UTC), otherwise <c>null</c>.</param>
/// <param name="RetentionExecutedAtUtc">When the retention execution ran (UTC), otherwise <c>null</c>.</param>
/// <param name="RetentionActor">Who executed the retention, otherwise <c>null</c>.</param>
/// <param name="ResolvedBy">The principal that made the resolution.</param>
/// <param name="ResolvedAtUtc">When the resolution was made (UTC).</param>
public sealed record AuditResolutionRecord(
    AuditResolutionTrail Trail,
    Guid EventId,
    AuditResolutionField Field,
    AuditResolutionKind Kind,
    Guid? AuditEntryId,
    string? TenantId,
    string? ErasureRequestId,
    DateTimeOffset? ErasureRequestedAtUtc,
    string? ErasureActor,
    string? RetentionPolicyId,
    DateTimeOffset? RetentionCutoffUtc,
    DateTimeOffset? RetentionExecutedAtUtc,
    string? RetentionActor,
    string ResolvedBy,
    DateTimeOffset ResolvedAtUtc);
