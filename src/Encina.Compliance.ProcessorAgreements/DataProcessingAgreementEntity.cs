namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// Persistence entity for <see cref="Model.DataProcessingAgreement"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a Data Processing Agreement,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// Key type transformations:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="Model.DataProcessingAgreement.Status"/> (<see cref="Model.DPAStatus"/>) is stored
/// as <see cref="StatusValue"/> (<see cref="int"/>) for cross-provider compatibility.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Model.DataProcessingAgreement.MandatoryTerms"/> (<see cref="Model.DPAMandatoryTerms"/>)
/// is flattened into 8 individual boolean columns (<see cref="ProcessOnDocumentedInstructions"/>
/// through <see cref="AuditRights"/>) for queryable persistence.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Model.DataProcessingAgreement.ProcessingPurposes"/>
/// (<see cref="System.Collections.Generic.IReadOnlyList{T}"/>) is stored as
/// <see cref="ProcessingPurposesJson"/> (<see cref="string"/>) in JSON format.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Use <see cref="DataProcessingAgreementMapper"/> to convert between this entity and
/// <see cref="Model.DataProcessingAgreement"/>.
/// </para>
/// </remarks>
public sealed class DataProcessingAgreementEntity
{
    /// <summary>
    /// Unique identifier for this agreement.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The identifier of the processor this agreement covers.
    /// </summary>
    public required string ProcessorId { get; set; }

    /// <summary>
    /// Integer value of the <see cref="Model.DPAStatus"/> enum.
    /// </summary>
    /// <remarks>
    /// Values: Active=0, Expired=1, PendingRenewal=2, Terminated=3.
    /// An INDEX should be created on this column for efficient status-based queries.
    /// </remarks>
    public required int StatusValue { get; set; }

    /// <summary>
    /// Timestamp when this agreement was signed (UTC).
    /// </summary>
    public DateTimeOffset SignedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when this agreement expires (UTC), or <see langword="null"/> for indefinite agreements.
    /// </summary>
    /// <remarks>
    /// An INDEX should be created on this column for efficient expiration queries.
    /// </remarks>
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    /// <summary>
    /// Whether Standard Contractual Clauses are included in this agreement.
    /// </summary>
    public bool HasSCCs { get; set; }

    /// <summary>
    /// JSON representation of the processing purposes list.
    /// </summary>
    /// <remarks>
    /// Stored as JSONB in PostgreSQL, JSON in MySQL, NVARCHAR(MAX) in SQL Server, and TEXT in SQLite.
    /// </remarks>
    public required string ProcessingPurposesJson { get; set; }

    // ── Mandatory Terms (Article 28(3)(a)-(h)) — individual columns ──

    /// <summary>
    /// Article 28(3)(a): Process only on documented instructions.
    /// </summary>
    public bool ProcessOnDocumentedInstructions { get; set; }

    /// <summary>
    /// Article 28(3)(b): Confidentiality obligations.
    /// </summary>
    public bool ConfidentialityObligations { get; set; }

    /// <summary>
    /// Article 28(3)(c): Security measures per Article 32.
    /// </summary>
    public bool SecurityMeasures { get; set; }

    /// <summary>
    /// Article 28(3)(d): Sub-processor requirements.
    /// </summary>
    public bool SubProcessorRequirements { get; set; }

    /// <summary>
    /// Article 28(3)(e): Data subject rights assistance.
    /// </summary>
    public bool DataSubjectRightsAssistance { get; set; }

    /// <summary>
    /// Article 28(3)(f): Compliance assistance (Articles 32-36).
    /// </summary>
    public bool ComplianceAssistance { get; set; }

    /// <summary>
    /// Article 28(3)(g): Data deletion or return.
    /// </summary>
    public bool DataDeletionOrReturn { get; set; }

    /// <summary>
    /// Article 28(3)(h): Audit rights.
    /// </summary>
    public bool AuditRights { get; set; }

    /// <summary>
    /// The tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// The module identifier for modular monolith isolation.
    /// </summary>
    public string? ModuleId { get; set; }

    /// <summary>
    /// Timestamp when this agreement record was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when this agreement was last updated (UTC).
    /// </summary>
    public DateTimeOffset LastUpdatedAtUtc { get; set; }
}
