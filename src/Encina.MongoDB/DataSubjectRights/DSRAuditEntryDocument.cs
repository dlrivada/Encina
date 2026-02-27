using Encina.Compliance.DataSubjectRights;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.DataSubjectRights;

/// <summary>
/// MongoDB document representation of a DSR audit entry.
/// </summary>
public sealed class DSRAuditEntryDocument
{
    /// <summary>
    /// Gets or sets the unique identifier of the audit entry.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the DSR request this audit entry belongs to.
    /// </summary>
    [BsonElement("dsr_request_id")]
    public string DSRRequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action that was performed.
    /// </summary>
    [BsonElement("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional detail about the action, if applicable.
    /// </summary>
    [BsonElement("detail")]
    [BsonIgnoreIfNull]
    public string? Detail { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who performed the action, if applicable.
    /// </summary>
    [BsonElement("performed_by_user_id")]
    [BsonIgnoreIfNull]
    public string? PerformedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the action occurred.
    /// </summary>
    [BsonElement("occurred_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>
    /// Creates a document from a domain audit entry.
    /// </summary>
    public static DSRAuditEntryDocument FromDomain(DSRAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return new DSRAuditEntryDocument
        {
            Id = entry.Id ?? Guid.NewGuid().ToString("D"),
            DSRRequestId = entry.DSRRequestId,
            Action = entry.Action,
            Detail = entry.Detail,
            PerformedByUserId = entry.PerformedByUserId,
            OccurredAtUtc = entry.OccurredAtUtc.UtcDateTime
        };
    }

    /// <summary>
    /// Converts this document to a domain audit entry.
    /// </summary>
    public DSRAuditEntry ToDomain()
    {
        return new DSRAuditEntry
        {
            Id = Id,
            DSRRequestId = DSRRequestId,
            Action = Action,
            Detail = Detail,
            PerformedByUserId = PerformedByUserId,
            OccurredAtUtc = new DateTimeOffset(OccurredAtUtc, TimeSpan.Zero)
        };
    }
}
