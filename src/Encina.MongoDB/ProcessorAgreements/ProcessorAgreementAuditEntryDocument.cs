using Encina.Compliance.ProcessorAgreements.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.ProcessorAgreements;

/// <summary>
/// MongoDB BSON document for <see cref="ProcessorAgreementAuditEntry"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps <see cref="ProcessorAgreementAuditEntry"/> domain records to a MongoDB-native document format
/// with BSON annotations for proper serialization and indexing.
/// </para>
/// <para>
/// Key type transformations:
/// <list type="bullet">
/// <item><description>DateTimeOffset → DateTime (UTC) for MongoDB native date storage.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ProcessorAgreementAuditEntryDocument
{
    /// <summary>
    /// Unique identifier for this audit entry.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The identifier of the processor this audit entry relates to.
    /// </summary>
    [BsonElement("processor_id")]
    public string ProcessorId { get; set; } = string.Empty;

    /// <summary>
    /// The identifier of the DPA this audit entry relates to, or null for processor-only operations.
    /// </summary>
    [BsonElement("dpa_id")]
    public string? DPAId { get; set; }

    /// <summary>
    /// The action that was performed.
    /// </summary>
    [BsonElement("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Additional detail about the action.
    /// </summary>
    [BsonElement("detail")]
    public string? Detail { get; set; }

    /// <summary>
    /// The identifier of the user who performed the action.
    /// </summary>
    [BsonElement("performed_by_user_id")]
    public string? PerformedByUserId { get; set; }

    /// <summary>
    /// Timestamp when this action occurred (UTC).
    /// </summary>
    [BsonElement("occurred_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>
    /// The tenant identifier for multi-tenancy support.
    /// </summary>
    [BsonElement("tenant_id")]
    public string? TenantId { get; set; }

    /// <summary>
    /// The module identifier for module isolation support.
    /// </summary>
    [BsonElement("module_id")]
    public string? ModuleId { get; set; }

    /// <summary>
    /// Creates a document from a domain <see cref="ProcessorAgreementAuditEntry"/>.
    /// </summary>
    /// <param name="entry">The domain audit entry to convert.</param>
    /// <returns>A <see cref="ProcessorAgreementAuditEntryDocument"/> suitable for MongoDB persistence.</returns>
    public static ProcessorAgreementAuditEntryDocument FromEntry(ProcessorAgreementAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new ProcessorAgreementAuditEntryDocument
        {
            Id = entry.Id,
            ProcessorId = entry.ProcessorId,
            DPAId = entry.DPAId,
            Action = entry.Action,
            Detail = entry.Detail,
            PerformedByUserId = entry.PerformedByUserId,
            OccurredAtUtc = entry.OccurredAtUtc.UtcDateTime,
            TenantId = entry.TenantId,
            ModuleId = entry.ModuleId
        };
    }

    /// <summary>
    /// Converts this document back to a domain <see cref="ProcessorAgreementAuditEntry"/>.
    /// </summary>
    /// <returns>A <see cref="ProcessorAgreementAuditEntry"/>.</returns>
    public ProcessorAgreementAuditEntry ToEntry()
    {
        return new ProcessorAgreementAuditEntry
        {
            Id = Id,
            ProcessorId = ProcessorId,
            DPAId = DPAId,
            Action = Action,
            Detail = Detail,
            PerformedByUserId = PerformedByUserId,
            OccurredAtUtc = new DateTimeOffset(OccurredAtUtc, TimeSpan.Zero),
            TenantId = TenantId,
            ModuleId = ModuleId
        };
    }
}
