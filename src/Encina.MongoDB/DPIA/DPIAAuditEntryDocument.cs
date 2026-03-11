using Encina.Compliance.DPIA.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.DPIA;

/// <summary>
/// MongoDB BSON document for DPIA audit entries.
/// </summary>
/// <remarks>
/// <para>
/// Maps <see cref="DPIAAuditEntry"/> domain records to a MongoDB-native document format
/// with BSON annotations for proper serialization and indexing.
/// </para>
/// <para>
/// Key type transformations:
/// <list type="bullet">
/// <item><description><see cref="DPIAAuditEntry.Id"/> (Guid) → <see cref="Id"/> (string, GUID "D" format).</description></item>
/// <item><description><see cref="DPIAAuditEntry.AssessmentId"/> (Guid) → <see cref="AssessmentId"/> (string, GUID "D" format).</description></item>
/// <item><description>DateTimeOffset → DateTime (UTC) for MongoDB native date storage.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class DPIAAuditEntryDocument
{
    /// <summary>
    /// Unique identifier, stored as a GUID string.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the assessment this entry relates to, stored as a GUID string.
    /// </summary>
    [BsonElement("assessment_id")]
    public string AssessmentId { get; set; } = string.Empty;

    /// <summary>
    /// The action that was performed.
    /// </summary>
    [BsonElement("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// The identity of the person or system that performed the action.
    /// </summary>
    [BsonElement("performed_by")]
    public string? PerformedBy { get; set; }

    /// <summary>
    /// Timestamp when the action occurred (UTC).
    /// </summary>
    [BsonElement("occurred_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>
    /// Additional details or context about the action.
    /// </summary>
    [BsonElement("details")]
    public string? Details { get; set; }

    /// <summary>
    /// Creates a document from a domain <see cref="DPIAAuditEntry"/>.
    /// </summary>
    /// <param name="entry">The domain audit entry to convert.</param>
    /// <returns>A <see cref="DPIAAuditEntryDocument"/> suitable for MongoDB persistence.</returns>
    public static DPIAAuditEntryDocument FromEntry(DPIAAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new DPIAAuditEntryDocument
        {
            Id = entry.Id.ToString("D"),
            AssessmentId = entry.AssessmentId.ToString("D"),
            Action = entry.Action,
            PerformedBy = entry.PerformedBy,
            OccurredAtUtc = entry.OccurredAtUtc.UtcDateTime,
            Details = entry.Details
        };
    }

    /// <summary>
    /// Converts this document back to a domain <see cref="DPIAAuditEntry"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="DPIAAuditEntry"/> if valid, or <c>null</c> if the document contains
    /// invalid GUID values.
    /// </returns>
    public DPIAAuditEntry? ToEntry()
    {
        if (!Guid.TryParse(Id, out var id))
            return null;

        if (!Guid.TryParse(AssessmentId, out var assessmentId))
            return null;

        return new DPIAAuditEntry
        {
            Id = id,
            AssessmentId = assessmentId,
            Action = Action,
            PerformedBy = PerformedBy,
            OccurredAtUtc = new DateTimeOffset(OccurredAtUtc, TimeSpan.Zero),
            Details = Details
        };
    }
}
