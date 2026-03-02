using Encina.Compliance.Retention.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.Retention;

/// <summary>
/// MongoDB document representation of a <see cref="RetentionAuditEntry"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the retention_audit_entries collection. Provides an immutable audit trail
/// for retention-related actions as required by GDPR Article 5(2) (accountability).
/// </para>
/// <para>
/// Uses snake_case naming convention for MongoDB field names. Audit entries should
/// never be modified or deleted once persisted.
/// </para>
/// </remarks>
public sealed class RetentionAuditEntryDocument
{
    /// <summary>
    /// Gets or sets the unique identifier for this audit entry.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action that was performed.
    /// </summary>
    [BsonElement("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the data entity affected by this action.
    /// </summary>
    [BsonElement("entity_id")]
    public string? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the data category affected by this action.
    /// </summary>
    [BsonElement("data_category")]
    public string? DataCategory { get; set; }

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
    /// Creates a <see cref="RetentionAuditEntryDocument"/> from a <see cref="RetentionAuditEntry"/>.
    /// </summary>
    /// <param name="entry">The audit entry to convert.</param>
    /// <returns>A new document representation of the retention audit entry.</returns>
    public static RetentionAuditEntryDocument FromEntry(RetentionAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new RetentionAuditEntryDocument
        {
            Id = entry.Id,
            Action = entry.Action,
            EntityId = entry.EntityId,
            DataCategory = entry.DataCategory,
            Detail = entry.Detail,
            PerformedByUserId = entry.PerformedByUserId,
            OccurredAtUtc = entry.OccurredAtUtc.UtcDateTime
        };
    }

    /// <summary>
    /// Converts this document to a <see cref="RetentionAuditEntry"/>.
    /// </summary>
    /// <returns>A retention audit entry record.</returns>
    public RetentionAuditEntry ToEntry() => new()
    {
        Id = Id,
        Action = Action,
        EntityId = EntityId,
        DataCategory = DataCategory,
        Detail = Detail,
        PerformedByUserId = PerformedByUserId,
        OccurredAtUtc = new DateTimeOffset(OccurredAtUtc, TimeSpan.Zero)
    };
}
