namespace Encina.Compliance.DPIA;

/// <summary>
/// Persistence entity for <see cref="Model.DPIAAssessment"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a DPIA assessment,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// Key type transformations:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="Model.DPIAAssessment.Id"/> (<see cref="System.Guid"/>) is stored as
/// <see cref="Id"/> (<see cref="string"/>) using <c>Guid.ToString("D")</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Model.DPIAAssessment.Status"/> (<see cref="Model.DPIAAssessmentStatus"/>) is stored
/// as <see cref="StatusValue"/> (<see cref="int"/>) for cross-provider compatibility.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Model.DPIAAssessment.Result"/> (<see cref="Model.DPIAResult"/>) is stored
/// as <see cref="ResultJson"/> (<see cref="string"/>) in JSON format because it contains
/// nested collections (<see cref="Model.RiskItem"/>, <see cref="Model.Mitigation"/>).
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Model.DPIAAssessment.DPOConsultation"/> (<see cref="Model.DPOConsultation"/>) is stored
/// as <see cref="DPOConsultationJson"/> (<see cref="string"/>) in JSON format.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// The <see cref="Model.DPIAAssessment.RequestType"/> and <see cref="Model.DPIAAssessment.AuditTrail"/>
/// properties are NOT mapped by this entity. <c>RequestType</c> is a runtime-only CLR type reference,
/// and audit entries are stored in a separate table mapped by <see cref="DPIAAuditEntryEntity"/>.
/// </para>
/// <para>
/// Use <see cref="DPIAAssessmentMapper"/> to convert between this entity and
/// <see cref="Model.DPIAAssessment"/>.
/// </para>
/// </remarks>
public sealed class DPIAAssessmentEntity
{
    /// <summary>
    /// Unique identifier for this assessment, stored as a GUID string (<c>"D"</c> format).
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The fully-qualified type name of the request this assessment covers.
    /// </summary>
    /// <remarks>
    /// Part of the UNIQUE index together with <c>TenantId</c> and <c>ModuleId</c>
    /// (when multi-tenancy or module isolation is enabled).
    /// </remarks>
    public required string RequestTypeName { get; set; }

    /// <summary>
    /// Integer value of the <see cref="Model.DPIAAssessmentStatus"/> enum.
    /// </summary>
    /// <remarks>
    /// Values: Draft=0, InReview=1, Approved=2, Rejected=3, RequiresRevision=4, Expired=5.
    /// An INDEX should be created on this column for efficient status-based queries.
    /// </remarks>
    public required int StatusValue { get; set; }

    /// <summary>
    /// The type of processing covered by this assessment (e.g., "profiling", "ai-ml").
    /// </summary>
    public string? ProcessingType { get; set; }

    /// <summary>
    /// The reason or justification for conducting this assessment.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// JSON representation of the <see cref="Model.DPIAResult"/>, including
    /// identified risks, proposed mitigations, overall risk level, and prior consultation flag.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> when the assessment is in <see cref="Model.DPIAAssessmentStatus.Draft"/>
    /// status and has not yet been evaluated. Stored as JSONB in PostgreSQL, JSON in MySQL,
    /// NVARCHAR(MAX) in SQL Server, and TEXT in SQLite.
    /// </remarks>
    public string? ResultJson { get; set; }

    /// <summary>
    /// JSON representation of the <see cref="Model.DPOConsultation"/>, including
    /// DPO name, email, decision, conditions, and timestamps.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> when DPO consultation has not yet been initiated.
    /// Stored as JSONB in PostgreSQL, JSON in MySQL, NVARCHAR(MAX) in SQL Server, and TEXT in SQLite.
    /// </remarks>
    public string? DPOConsultationJson { get; set; }

    /// <summary>
    /// Timestamp when this assessment was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when this assessment was approved (UTC), or <see langword="null"/> if not yet approved.
    /// </summary>
    public DateTimeOffset? ApprovedAtUtc { get; set; }

    /// <summary>
    /// Timestamp for the next scheduled review (UTC).
    /// </summary>
    /// <remarks>
    /// An INDEX should be created on this column for efficient expiration queries.
    /// </remarks>
    public DateTimeOffset? NextReviewAtUtc { get; set; }

    /// <summary>
    /// The tenant identifier for multi-tenancy scoping, or <see langword="null"/> when tenancy is not used.
    /// </summary>
    /// <remarks>
    /// Part of the composite unique constraint together with <see cref="RequestTypeName"/> and <see cref="ModuleId"/>
    /// (when multi-tenancy or module isolation is enabled).
    /// An INDEX should be created on this column for efficient tenant-filtered queries.
    /// </remarks>
    public string? TenantId { get; set; }

    /// <summary>
    /// The module identifier for modular monolith isolation, or <see langword="null"/> when module isolation is not used.
    /// </summary>
    /// <remarks>
    /// Part of the composite unique constraint together with <see cref="RequestTypeName"/> and <see cref="TenantId"/>
    /// (when module isolation is enabled).
    /// </remarks>
    public string? ModuleId { get; set; }
}
