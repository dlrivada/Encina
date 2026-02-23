using System.Text.Json;
using Encina.Compliance.Consent;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.Consent;

/// <summary>
/// MongoDB document representation of a <see cref="ConsentAuditEntry"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the consent_audit collection. Provides an immutable audit trail for consent-related
/// actions as required by GDPR Article 7(1).
/// </para>
/// <para>
/// Uses snake_case naming convention for MongoDB field names.
/// Metadata is stored as a native BSON document for efficient querying.
/// </para>
/// </remarks>
public sealed class ConsentAuditEntryDocument
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Gets or sets the unique identifier for this audit entry.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the data subject whose consent was affected.
    /// </summary>
    [BsonElement("subject_id")]
    public string SubjectId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the processing purpose associated with this consent action.
    /// </summary>
    [BsonElement("purpose")]
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of consent action that was performed.
    /// </summary>
    [BsonElement("action")]
    public int Action { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the action occurred (UTC).
    /// </summary>
    [BsonElement("occurred_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the actor who performed the action.
    /// </summary>
    [BsonElement("performed_by")]
    public string PerformedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the IP address of the actor at the time of the action.
    /// </summary>
    [BsonElement("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets additional metadata stored as a native BSON document.
    /// </summary>
    [BsonElement("metadata")]
    public BsonDocument? Metadata { get; set; }

    /// <summary>
    /// Creates a <see cref="ConsentAuditEntryDocument"/> from a <see cref="ConsentAuditEntry"/>.
    /// </summary>
    /// <param name="entry">The audit entry to convert.</param>
    /// <returns>A new document representation of the audit entry.</returns>
    public static ConsentAuditEntryDocument FromEntry(ConsentAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new ConsentAuditEntryDocument
        {
            Id = entry.Id,
            SubjectId = entry.SubjectId,
            Purpose = entry.Purpose,
            Action = (int)entry.Action,
            OccurredAtUtc = entry.OccurredAtUtc.UtcDateTime,
            PerformedBy = entry.PerformedBy,
            IpAddress = entry.IpAddress,
            Metadata = SerializeMetadata(entry.Metadata)
        };
    }

    /// <summary>
    /// Converts this document to a <see cref="ConsentAuditEntry"/>.
    /// </summary>
    /// <returns>A consent audit entry record.</returns>
    public ConsentAuditEntry ToEntry() => new()
    {
        Id = Id,
        SubjectId = SubjectId,
        Purpose = Purpose,
        Action = (ConsentAuditAction)Action,
        OccurredAtUtc = new DateTimeOffset(OccurredAtUtc, TimeSpan.Zero),
        PerformedBy = PerformedBy,
        IpAddress = IpAddress,
        Metadata = DeserializeMetadata(Metadata)
    };

    private static BsonDocument? SerializeMetadata(IReadOnlyDictionary<string, object?> metadata)
    {
        if (metadata.Count == 0)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(metadata, JsonOptions);
        return BsonDocument.Parse(json);
    }

    private static Dictionary<string, object?> DeserializeMetadata(BsonDocument? bsonDocument)
    {
        if (bsonDocument is null || bsonDocument.ElementCount == 0)
        {
            return new Dictionary<string, object?>();
        }

        try
        {
            var json = bsonDocument.ToJson(new global::MongoDB.Bson.IO.JsonWriterSettings
            {
                OutputMode = global::MongoDB.Bson.IO.JsonOutputMode.RelaxedExtendedJson
            });
            var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions);
            return dict ?? new Dictionary<string, object?>();
        }
        catch
        {
            return new Dictionary<string, object?>();
        }
    }
}
