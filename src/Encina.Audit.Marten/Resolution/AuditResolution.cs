namespace Encina.Audit.Marten.Resolution;

/// <summary>
/// Marten document that records an operator's resolution of one unreadable field (or a whole event) of an
/// audit event, so the audit projections can substitute a placeholder or exclude the event instead of failing.
/// </summary>
/// <remarks>
/// <para>
/// The document ID follows the convention <c>"audit-resolution:{trail}:{eventId}:{field}"</c>
/// (built by the internal <c>AuditResolutionIds.Format</c>). The document carries no reason text: the reason lives only in the
/// encrypted audit entry written before the document.
/// </para>
/// </remarks>
public sealed class AuditResolution
{
    /// <summary>
    /// Unique document identifier following the convention <c>"audit-resolution:{trail}:{eventId}:{field}"</c>.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The audit trail of the resolved event.</summary>
    public AuditResolutionTrail Trail { get; set; }

    /// <summary>The Marten event identifier (<c>IEvent.Id</c>) of the resolved event.</summary>
    public Guid EventId { get; set; }

    /// <summary>The field the resolution applies to.</summary>
    public AuditResolutionField Field { get; set; }

    /// <summary>How the field was resolved.</summary>
    public AuditResolutionKind Kind { get; set; }

    /// <summary>The audit entry identifier carried by the event payload (informational), or <c>null</c> when unknown.</summary>
    public Guid? AuditEntryId { get; set; }

    /// <summary>The tenant of the resolved event, copied from the event, or <c>null</c> without a tenant.</summary>
    public string? TenantId { get; set; }

    /// <summary>The data-erasure request identifier of a <see cref="AuditResolutionKind.Shredded"/> resolution, otherwise <c>null</c>.</summary>
    public string? ErasureRequestId { get; set; }

    /// <summary>When the data-erasure request was made (UTC), otherwise <c>null</c>.</summary>
    public DateTimeOffset? ErasureRequestedAtUtc { get; set; }

    /// <summary>Who requested or approved the erasure, otherwise <c>null</c>.</summary>
    public string? ErasureActor { get; set; }

    /// <summary>The retention policy identifier of an <see cref="AuditResolutionKind.Expired"/> resolution, otherwise <c>null</c>.</summary>
    public string? RetentionPolicyId { get; set; }

    /// <summary>The cutoff the retention policy applied (UTC), otherwise <c>null</c>.</summary>
    public DateTimeOffset? RetentionCutoffUtc { get; set; }

    /// <summary>When the retention execution ran (UTC), otherwise <c>null</c>.</summary>
    public DateTimeOffset? RetentionExecutedAtUtc { get; set; }

    /// <summary>Who executed the retention, otherwise <c>null</c>.</summary>
    public string? RetentionActor { get; set; }

    /// <summary>The principal that made the resolution.</summary>
    public string ResolvedBy { get; set; } = string.Empty;

    /// <summary>When the resolution was made (UTC).</summary>
    public DateTimeOffset ResolvedAtUtc { get; set; }
}
