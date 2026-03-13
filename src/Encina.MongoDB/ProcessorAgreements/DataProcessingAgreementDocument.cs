using System.Diagnostics.CodeAnalysis;
using Encina.Compliance.ProcessorAgreements.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.ProcessorAgreements;

/// <summary>
/// MongoDB BSON document for <see cref="DataProcessingAgreement"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps <see cref="DataProcessingAgreement"/> domain records to a MongoDB-native document format
/// with BSON annotations for proper serialization and indexing.
/// </para>
/// <para>
/// Key type transformations:
/// <list type="bullet">
/// <item><description><see cref="DataProcessingAgreement.Status"/> (enum) → <see cref="StatusValue"/> (int).</description></item>
/// <item><description><see cref="DataProcessingAgreement.MandatoryTerms"/> → 8 individual boolean fields.</description></item>
/// <item><description><see cref="DataProcessingAgreement.ProcessingPurposes"/> → native <see cref="List{T}"/> (no JSON serialization).</description></item>
/// <item><description>DateTimeOffset → DateTime (UTC) for MongoDB native date storage.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class DataProcessingAgreementDocument
{
    /// <summary>
    /// Unique identifier for this agreement.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The identifier of the processor this agreement covers.
    /// </summary>
    [BsonElement("processor_id")]
    public string ProcessorId { get; set; } = string.Empty;

    /// <summary>
    /// Integer value of the <see cref="DPAStatus"/> enum.
    /// </summary>
    [BsonElement("status")]
    public int StatusValue { get; set; }

    /// <summary>
    /// Timestamp when this agreement was signed (UTC).
    /// </summary>
    [BsonElement("signed_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime SignedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when this agreement expires (UTC), or null for indefinite agreements.
    /// </summary>
    [BsonElement("expires_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ExpiresAtUtc { get; set; }

    /// <summary>
    /// Whether Standard Contractual Clauses are included.
    /// </summary>
    [BsonElement("has_sccs")]
    public bool HasSCCs { get; set; }

    /// <summary>
    /// The documented processing purposes — stored as a native BSON array.
    /// </summary>
    [BsonElement("processing_purposes")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "MongoDB BSON deserialization requires mutable setter")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "MongoDB BSON driver maps List<T> to native BSON arrays")]
    public List<string> ProcessingPurposes { get; set; } = [];

    // --- DPA Mandatory Terms (Article 28(3)(a)-(h)) ---

    /// <summary>
    /// Article 28(3)(a): Process only on documented instructions.
    /// </summary>
    [BsonElement("process_on_documented_instructions")]
    public bool ProcessOnDocumentedInstructions { get; set; }

    /// <summary>
    /// Article 28(3)(b): Confidentiality obligations for authorized personnel.
    /// </summary>
    [BsonElement("confidentiality_obligations")]
    public bool ConfidentialityObligations { get; set; }

    /// <summary>
    /// Article 28(3)(c): Appropriate technical and organisational security measures.
    /// </summary>
    [BsonElement("security_measures")]
    public bool SecurityMeasures { get; set; }

    /// <summary>
    /// Article 28(3)(d): Sub-processor engagement requirements.
    /// </summary>
    [BsonElement("sub_processor_requirements")]
    public bool SubProcessorRequirements { get; set; }

    /// <summary>
    /// Article 28(3)(e): Assistance with data subject rights requests.
    /// </summary>
    [BsonElement("data_subject_rights_assistance")]
    public bool DataSubjectRightsAssistance { get; set; }

    /// <summary>
    /// Article 28(3)(f): Assistance with compliance obligations.
    /// </summary>
    [BsonElement("compliance_assistance")]
    public bool ComplianceAssistance { get; set; }

    /// <summary>
    /// Article 28(3)(g): Data deletion or return upon contract termination.
    /// </summary>
    [BsonElement("data_deletion_or_return")]
    public bool DataDeletionOrReturn { get; set; }

    /// <summary>
    /// Article 28(3)(h): Audit and inspection rights.
    /// </summary>
    [BsonElement("audit_rights")]
    public bool AuditRights { get; set; }

    /// <summary>
    /// The tenant identifier for multi-tenancy support.
    /// </summary>
    [BsonElement("tenant_id")]
    public string? TenantId { get; set; }

    /// <summary>
    /// The module identifier for modular monolith isolation.
    /// </summary>
    [BsonElement("module_id")]
    public string? ModuleId { get; set; }

    /// <summary>
    /// Timestamp when this agreement record was created (UTC).
    /// </summary>
    [BsonElement("created_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when this agreement was last updated (UTC).
    /// </summary>
    [BsonElement("last_updated_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime LastUpdatedAtUtc { get; set; }

    /// <summary>
    /// Creates a document from a domain <see cref="DataProcessingAgreement"/>.
    /// </summary>
    /// <param name="dpa">The domain DPA to convert.</param>
    /// <returns>A <see cref="DataProcessingAgreementDocument"/> suitable for MongoDB persistence.</returns>
    public static DataProcessingAgreementDocument FromDPA(DataProcessingAgreement dpa)
    {
        ArgumentNullException.ThrowIfNull(dpa);

        return new DataProcessingAgreementDocument
        {
            Id = dpa.Id,
            ProcessorId = dpa.ProcessorId,
            StatusValue = (int)dpa.Status,
            SignedAtUtc = dpa.SignedAtUtc.UtcDateTime,
            ExpiresAtUtc = dpa.ExpiresAtUtc?.UtcDateTime,
            HasSCCs = dpa.HasSCCs,
            ProcessingPurposes = dpa.ProcessingPurposes.ToList(),
            ProcessOnDocumentedInstructions = dpa.MandatoryTerms.ProcessOnDocumentedInstructions,
            ConfidentialityObligations = dpa.MandatoryTerms.ConfidentialityObligations,
            SecurityMeasures = dpa.MandatoryTerms.SecurityMeasures,
            SubProcessorRequirements = dpa.MandatoryTerms.SubProcessorRequirements,
            DataSubjectRightsAssistance = dpa.MandatoryTerms.DataSubjectRightsAssistance,
            ComplianceAssistance = dpa.MandatoryTerms.ComplianceAssistance,
            DataDeletionOrReturn = dpa.MandatoryTerms.DataDeletionOrReturn,
            AuditRights = dpa.MandatoryTerms.AuditRights,
            TenantId = dpa.TenantId,
            ModuleId = dpa.ModuleId,
            CreatedAtUtc = dpa.CreatedAtUtc.UtcDateTime,
            LastUpdatedAtUtc = dpa.LastUpdatedAtUtc.UtcDateTime
        };
    }

    /// <summary>
    /// Converts this document back to a domain <see cref="DataProcessingAgreement"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="DataProcessingAgreement"/> if valid, or <c>null</c> if the document contains
    /// invalid values (undefined enum).
    /// </returns>
    public DataProcessingAgreement? ToDPA()
    {
        if (!Enum.IsDefined(typeof(DPAStatus), StatusValue))
            return null;

        return new DataProcessingAgreement
        {
            Id = Id,
            ProcessorId = ProcessorId,
            Status = (DPAStatus)StatusValue,
            SignedAtUtc = new DateTimeOffset(SignedAtUtc, TimeSpan.Zero),
            ExpiresAtUtc = ExpiresAtUtc.HasValue
                ? new DateTimeOffset(ExpiresAtUtc.Value, TimeSpan.Zero)
                : null,
            HasSCCs = HasSCCs,
            ProcessingPurposes = ProcessingPurposes,
            MandatoryTerms = new DPAMandatoryTerms
            {
                ProcessOnDocumentedInstructions = ProcessOnDocumentedInstructions,
                ConfidentialityObligations = ConfidentialityObligations,
                SecurityMeasures = SecurityMeasures,
                SubProcessorRequirements = SubProcessorRequirements,
                DataSubjectRightsAssistance = DataSubjectRightsAssistance,
                ComplianceAssistance = ComplianceAssistance,
                DataDeletionOrReturn = DataDeletionOrReturn,
                AuditRights = AuditRights
            },
            TenantId = TenantId,
            ModuleId = ModuleId,
            CreatedAtUtc = new DateTimeOffset(CreatedAtUtc, TimeSpan.Zero),
            LastUpdatedAtUtc = new DateTimeOffset(LastUpdatedAtUtc, TimeSpan.Zero)
        };
    }
}
