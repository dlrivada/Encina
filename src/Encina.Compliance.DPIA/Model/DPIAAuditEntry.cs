namespace Encina.Compliance.DPIA.Model;

/// <summary>
/// Records an auditable event in the lifecycle of a DPIA assessment.
/// </summary>
/// <remarks>
/// <para>
/// GDPR Article 5(2) (accountability principle) requires the controller to be able to
/// demonstrate compliance. The audit trail provides evidence of the assessment process,
/// including who performed actions, when they occurred, and what changed.
/// </para>
/// <para>
/// Audit entries are immutable once created and form a chronological record of the
/// assessment lifecycle, supporting regulatory inquiries and internal compliance reviews.
/// </para>
/// </remarks>
public sealed record DPIAAuditEntry
{
    /// <summary>
    /// Unique identifier for this audit entry.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The identifier of the assessment this entry relates to.
    /// </summary>
    public required Guid AssessmentId { get; init; }

    /// <summary>
    /// The action that was performed (e.g., "Created", "SubmittedForReview", "Approved", "Expired").
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// The identity of the person or system that performed the action, if available.
    /// </summary>
    public string? PerformedBy { get; init; }

    /// <summary>
    /// The UTC timestamp when the action occurred.
    /// </summary>
    public required DateTimeOffset OccurredAtUtc { get; init; }

    /// <summary>
    /// Additional details or context about the action, if any.
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// The tenant identifier associated with this audit entry, or <see langword="null"/> when tenancy is not used.
    /// </summary>
    /// <remarks>
    /// Mirrors the <see cref="DPIAAssessment.TenantId"/> of the assessment at the time the action occurred,
    /// providing a complete tenant-scoped audit trail.
    /// </remarks>
    public string? TenantId { get; init; }

    /// <summary>
    /// The module identifier associated with this audit entry, or <see langword="null"/> when module isolation is not used.
    /// </summary>
    /// <remarks>
    /// Mirrors the <see cref="DPIAAssessment.ModuleId"/> of the assessment at the time the action occurred,
    /// providing a complete module-scoped audit trail.
    /// </remarks>
    public string? ModuleId { get; init; }
}
