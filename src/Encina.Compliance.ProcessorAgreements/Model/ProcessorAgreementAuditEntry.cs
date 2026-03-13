namespace Encina.Compliance.ProcessorAgreements.Model;

/// <summary>
/// An audit trail entry recording an action performed on a processor or its Data Processing Agreement.
/// </summary>
/// <remarks>
/// <para>
/// Supports the accountability principle (GDPR Article 5(2)) by providing a complete record
/// of all operations on processors and their agreements. Each entry captures both the
/// <see cref="ProcessorId"/> and an optional <see cref="DPAId"/> to support traceability
/// across the entire processor relationship lifecycle.
/// </para>
/// <para>
/// Audit entries are recorded for:
/// </para>
/// <list type="bullet">
/// <item><description>Processor registration, update, and removal.</description></item>
/// <item><description>Sub-processor addition and removal (with depth tracking per Article 28(2)).</description></item>
/// <item><description>DPA creation, status changes, termination, and expiration.</description></item>
/// <item><description>DPA validation results (Article 28(3) compliance checks).</description></item>
/// <item><description>Pipeline behavior enforcement actions (block, warn).</description></item>
/// </list>
/// </remarks>
public sealed record ProcessorAgreementAuditEntry
{
    /// <summary>
    /// Unique identifier for this audit entry.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The identifier of the processor this audit entry relates to.
    /// </summary>
    public required string ProcessorId { get; init; }

    /// <summary>
    /// The identifier of the DPA this audit entry relates to, or <see langword="null"/>
    /// for processor-only operations (e.g., registration, removal).
    /// </summary>
    public string? DPAId { get; init; }

    /// <summary>
    /// The action that was performed (e.g., "Registered", "DPASigned", "SubProcessorAdded", "Blocked").
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Additional detail about the action, or <see langword="null"/> if not applicable.
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>
    /// The identifier of the user who performed the action, or <see langword="null"/>
    /// for system-initiated actions (e.g., expiration monitoring).
    /// </summary>
    public string? PerformedByUserId { get; init; }

    /// <summary>
    /// The UTC timestamp when this action occurred.
    /// </summary>
    public required DateTimeOffset OccurredAtUtc { get; init; }

    /// <summary>
    /// The tenant identifier for multi-tenancy support, or <see langword="null"/>
    /// when multi-tenancy is not configured.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// The module identifier for module isolation support, or <see langword="null"/>
    /// when module isolation is not configured.
    /// </summary>
    public string? ModuleId { get; init; }
}
