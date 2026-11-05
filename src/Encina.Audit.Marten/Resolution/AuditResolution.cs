namespace Encina.Audit.Marten.Resolution;

/// <summary>
/// Marten document that records one operator decision about an unreadable field (or a whole event) of an
/// audit event, so the audit projections can substitute a placeholder or exclude the event instead of failing.
/// </summary>
/// <remarks>
/// <para>
/// Resolutions are an append-only record: each decision is a new document with its own
/// <see cref="Id"/> (a UUID version 7 created from the <see cref="TimeProvider"/>), and a document is never
/// updated or deleted. The most recent resolution of an event and field wins; <see cref="Sequence"/> orders the
/// resolutions of one event and <see cref="Supersedes"/> lists the ones a new resolution amends.
/// </para>
/// <para>
/// The document carries no reason text: the reason lives only in the encrypted audit entry whose identifier is
/// <see cref="ResolutionAuditEntryId"/>.
/// </para>
/// </remarks>
public sealed class AuditResolution
{
    /// <summary>
    /// Unique identifier of this resolution (UUID version 7: ids created in different milliseconds sort by creation
    /// time; <see cref="Sequence"/> decides inside one millisecond).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>The audit trail of the resolved event.</summary>
    public AuditResolutionTrail Trail { get; set; }

    /// <summary>The Marten event identifier (<c>IEvent.Id</c>) of the resolved event.</summary>
    public Guid EventId { get; set; }

    /// <summary>The field the resolution applies to.</summary>
    public AuditResolutionField Field { get; set; }

    /// <summary>How the field was resolved.</summary>
    public AuditResolutionKind Kind { get; set; }

    /// <summary>The position of this resolution among the resolutions of the same trail and event (the highest wins).</summary>
    public long Sequence { get; set; }

    /// <summary>The identifiers of the earlier resolutions this one amends.</summary>
    public List<Guid> Supersedes { get; set; } = [];

    /// <summary>The audit entry identifier carried by the payload of the resolved event (informational), or <c>null</c> when unknown.</summary>
    public Guid? EventAuditEntryId { get; set; }

    /// <summary>The identifier of the audit entry written for this resolution, or <c>null</c> when none was written yet.</summary>
    public Guid? ResolutionAuditEntryId { get; set; }

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
