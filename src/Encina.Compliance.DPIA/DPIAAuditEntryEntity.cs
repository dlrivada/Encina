namespace Encina.Compliance.DPIA;

/// <summary>
/// Persistence entity for <see cref="Model.DPIAAuditEntry"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a DPIA audit entry,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// Key type transformations:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="Model.DPIAAuditEntry.Id"/> (<see cref="System.Guid"/>) is stored as
/// <see cref="Id"/> (<see cref="string"/>) using <c>Guid.ToString("D")</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Model.DPIAAuditEntry.AssessmentId"/> (<see cref="System.Guid"/>) is stored as
/// <see cref="AssessmentId"/> (<see cref="string"/>) using <c>Guid.ToString("D")</c>.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Use <see cref="DPIAAuditEntryMapper"/> to convert between this entity and
/// <see cref="Model.DPIAAuditEntry"/>.
/// </para>
/// </remarks>
public sealed class DPIAAuditEntryEntity
{
    /// <summary>
    /// Unique identifier for this audit entry, stored as a GUID string (<c>"D"</c> format).
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Identifier of the assessment this entry relates to, stored as a GUID string (<c>"D"</c> format).
    /// </summary>
    /// <remarks>
    /// An INDEX should be created on this column. References <see cref="DPIAAssessmentEntity.Id"/>.
    /// </remarks>
    public required string AssessmentId { get; set; }

    /// <summary>
    /// The action that was performed (e.g., "Created", "SubmittedForReview", "Approved", "Expired").
    /// </summary>
    public required string Action { get; set; }

    /// <summary>
    /// The identity of the person or system that performed the action, if available.
    /// </summary>
    public string? PerformedBy { get; set; }

    /// <summary>
    /// Timestamp when the action occurred (UTC).
    /// </summary>
    public DateTimeOffset OccurredAtUtc { get; set; }

    /// <summary>
    /// Additional details or context about the action, if any.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// The tenant identifier associated with this audit entry, or <see langword="null"/> when tenancy is not used.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// The module identifier associated with this audit entry, or <see langword="null"/> when module isolation is not used.
    /// </summary>
    public string? ModuleId { get; set; }
}
