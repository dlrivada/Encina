using Encina.DomainModeling.Auditing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.Auditing;

/// <summary>
/// MongoDB document representation of an audit log entry.
/// </summary>
/// <remarks>
/// <para>
/// This document class maps to the <see cref="AuditLogEntry"/> record from the core library,
/// providing MongoDB-specific serialization attributes for BSON storage.
/// </para>
/// <para>
/// Uses snake_case naming convention for MongoDB field names to follow
/// MongoDB community conventions.
/// </para>
/// </remarks>
public sealed class AuditLogDocument
{
    /// <summary>
    /// Gets or sets the unique identifier for this audit log entry.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type name of the entity that was changed.
    /// </summary>
    [BsonElement("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the string representation of the entity's primary key.
    /// </summary>
    [BsonElement("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of action performed on the entity.
    /// </summary>
    [BsonElement("action")]
    public int Action { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who performed the action.
    /// </summary>
    [BsonElement("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the action occurred.
    /// </summary>
    [BsonElement("timestamp_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime TimestampUtc { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized representation of the entity's state before the change.
    /// </summary>
    [BsonElement("old_values")]
    public string? OldValues { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized representation of the entity's state after the change.
    /// </summary>
    [BsonElement("new_values")]
    public string? NewValues { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID to group related audit entries.
    /// </summary>
    [BsonElement("correlation_id")]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Creates an <see cref="AuditLogDocument"/> from an <see cref="AuditLogEntry"/>.
    /// </summary>
    /// <param name="entry">The audit log entry to convert.</param>
    /// <returns>A new document representation of the entry.</returns>
    public static AuditLogDocument FromEntry(AuditLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new AuditLogDocument
        {
            Id = entry.Id,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            Action = (int)entry.Action,
            UserId = entry.UserId,
            TimestampUtc = entry.TimestampUtc,
            OldValues = entry.OldValues,
            NewValues = entry.NewValues,
            CorrelationId = entry.CorrelationId
        };
    }

    /// <summary>
    /// Converts this document to an <see cref="AuditLogEntry"/> record.
    /// </summary>
    /// <returns>An audit log entry record.</returns>
    public AuditLogEntry ToEntry() => new(
        Id: Id,
        EntityType: EntityType,
        EntityId: EntityId,
        Action: (AuditAction)Action,
        UserId: UserId,
        TimestampUtc: TimestampUtc,
        OldValues: OldValues,
        NewValues: NewValues,
        CorrelationId: CorrelationId);
}
