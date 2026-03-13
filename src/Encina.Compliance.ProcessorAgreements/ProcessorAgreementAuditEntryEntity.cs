namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// Persistence entity for <see cref="Model.ProcessorAgreementAuditEntry"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a processor agreement audit entry,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// Unlike the DPIA audit entity, all identifiers in processor agreements are already strings,
/// so no type transformations are required.
/// </para>
/// <para>
/// Use <see cref="ProcessorAgreementAuditEntryMapper"/> to convert between this entity and
/// <see cref="Model.ProcessorAgreementAuditEntry"/>.
/// </para>
/// </remarks>
public sealed class ProcessorAgreementAuditEntryEntity
{
    /// <summary>
    /// Unique identifier for this audit entry.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The identifier of the processor this audit entry relates to.
    /// </summary>
    /// <remarks>
    /// An INDEX should be created on this column. References <see cref="ProcessorEntity.Id"/>.
    /// </remarks>
    public required string ProcessorId { get; set; }

    /// <summary>
    /// The identifier of the DPA this audit entry relates to, or <see langword="null"/>
    /// for processor-only operations (e.g., registration, removal).
    /// </summary>
    public string? DPAId { get; set; }

    /// <summary>
    /// The action that was performed (e.g., "Registered", "DPASigned", "SubProcessorAdded", "Blocked").
    /// </summary>
    public required string Action { get; set; }

    /// <summary>
    /// Additional detail about the action, if any.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// The identifier of the user who performed the action, or <see langword="null"/>
    /// for system-initiated actions (e.g., expiration monitoring).
    /// </summary>
    public string? PerformedByUserId { get; set; }

    /// <summary>
    /// Timestamp when the action occurred (UTC).
    /// </summary>
    public DateTimeOffset OccurredAtUtc { get; set; }

    /// <summary>
    /// The tenant identifier for multi-tenancy support, or <see langword="null"/>
    /// when multi-tenancy is not configured.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// The module identifier for module isolation support, or <see langword="null"/>
    /// when module isolation is not configured.
    /// </summary>
    public string? ModuleId { get; set; }
}
