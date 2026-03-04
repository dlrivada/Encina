using Encina.Compliance.BreachNotification.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.BreachNotification;

/// <summary>
/// MongoDB document representation of a <see cref="BreachAuditEntry"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the breach_audit_entries collection. Provides an immutable audit trail
/// for breach notification actions as required by GDPR Article 33(5) (documentation)
/// and Article 5(2) (accountability).
/// </para>
/// <para>
/// Uses snake_case naming convention for MongoDB field names. Audit entries should
/// never be modified or deleted once persisted. They serve as legal evidence of the
/// notification measures applied.
/// </para>
/// </remarks>
public sealed class BreachAuditEntryDocument
{
    /// <summary>
    /// Gets or sets the unique identifier for this audit entry.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the breach this audit entry relates to.
    /// </summary>
    [BsonElement("breach_id")]
    public string BreachId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action that was performed.
    /// </summary>
    [BsonElement("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional details about the action performed.
    /// </summary>
    [BsonElement("detail")]
    public string? Detail { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user or system that performed the action.
    /// </summary>
    [BsonElement("performed_by_user_id")]
    public string? PerformedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the action occurred (UTC).
    /// </summary>
    [BsonElement("occurred_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>
    /// Creates a <see cref="BreachAuditEntryDocument"/> from a <see cref="BreachAuditEntry"/>.
    /// </summary>
    /// <param name="entry">The audit entry to convert.</param>
    /// <returns>A new document representation of the breach audit entry.</returns>
    public static BreachAuditEntryDocument FromEntry(BreachAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new BreachAuditEntryDocument
        {
            Id = entry.Id,
            BreachId = entry.BreachId,
            Action = entry.Action,
            Detail = entry.Detail,
            PerformedByUserId = entry.PerformedByUserId,
            OccurredAtUtc = entry.OccurredAtUtc.UtcDateTime
        };
    }

    /// <summary>
    /// Converts this document to a <see cref="BreachAuditEntry"/>.
    /// </summary>
    /// <returns>A breach audit entry domain record.</returns>
    public BreachAuditEntry ToEntry() => new()
    {
        Id = Id,
        BreachId = BreachId,
        Action = Action,
        Detail = Detail,
        PerformedByUserId = PerformedByUserId,
        OccurredAtUtc = new DateTimeOffset(OccurredAtUtc, TimeSpan.Zero)
    };
}
