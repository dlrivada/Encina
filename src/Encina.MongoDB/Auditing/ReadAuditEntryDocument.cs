using System.Text.Json;
using Encina.Security.Audit;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.Auditing;

/// <summary>
/// MongoDB document representation of a <see cref="ReadAuditEntry"/>.
/// </summary>
/// <remarks>
/// <para>
/// This document class maps to the <see cref="ReadAuditEntry"/> record from the security audit library,
/// providing MongoDB-specific serialization attributes for BSON storage.
/// </para>
/// <para>
/// Uses snake_case naming convention for MongoDB field names to follow
/// MongoDB community conventions.
/// </para>
/// <para>
/// <b>DateTime handling</b>: MongoDB stores <see cref="DateTime"/> natively as BSON Date type.
/// <see cref="ReadAuditEntry.AccessedAtUtc"/> is <see cref="DateTimeOffset"/>, so conversion
/// is performed in <see cref="FromEntry"/> and <see cref="ToEntry"/> methods.
/// </para>
/// </remarks>
public sealed class ReadAuditEntryDocument
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Gets or sets the unique identifier for this read audit entry.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the type of entity that was accessed.
    /// </summary>
    [BsonElement("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the specific entity identifier that was accessed.
    /// </summary>
    [BsonElement("entity_id")]
    public string? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the user ID who accessed the data.
    /// </summary>
    [BsonElement("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID for multi-tenant applications.
    /// </summary>
    [BsonElement("tenant_id")]
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the data was accessed.
    /// </summary>
    [BsonElement("accessed_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime AccessedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for distributed tracing.
    /// </summary>
    [BsonElement("correlation_id")]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the declared purpose for accessing this data.
    /// </summary>
    [BsonElement("purpose")]
    public string? Purpose { get; set; }

    /// <summary>
    /// Gets or sets the access method as an integer.
    /// </summary>
    /// <remarks>
    /// Stored as <c>(int)<see cref="ReadAccessMethod"/></c> for forward-compatible storage.
    /// </remarks>
    [BsonElement("access_method")]
    public int AccessMethod { get; set; }

    /// <summary>
    /// Gets or sets the number of entities returned by the read operation.
    /// </summary>
    [BsonElement("entity_count")]
    public int EntityCount { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized metadata dictionary.
    /// </summary>
    [BsonElement("metadata")]
    public string? Metadata { get; set; }

    /// <summary>
    /// Creates a <see cref="ReadAuditEntryDocument"/> from a <see cref="ReadAuditEntry"/>.
    /// </summary>
    /// <param name="entry">The read audit entry to convert.</param>
    /// <returns>A new document representation of the entry.</returns>
    public static ReadAuditEntryDocument FromEntry(ReadAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new ReadAuditEntryDocument
        {
            Id = entry.Id,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            UserId = entry.UserId,
            TenantId = entry.TenantId,
            AccessedAtUtc = entry.AccessedAtUtc.UtcDateTime,
            CorrelationId = entry.CorrelationId,
            Purpose = entry.Purpose,
            AccessMethod = (int)entry.AccessMethod,
            EntityCount = entry.EntityCount,
            Metadata = SerializeMetadata(entry.Metadata)
        };
    }

    /// <summary>
    /// Converts this document to a <see cref="ReadAuditEntry"/> record.
    /// </summary>
    /// <returns>A read audit entry record.</returns>
    public ReadAuditEntry ToEntry() => new()
    {
        Id = Id,
        EntityType = EntityType,
        EntityId = EntityId,
        UserId = UserId,
        TenantId = TenantId,
        AccessedAtUtc = new DateTimeOffset(AccessedAtUtc, TimeSpan.Zero),
        CorrelationId = CorrelationId,
        Purpose = Purpose,
        AccessMethod = (ReadAccessMethod)AccessMethod,
        EntityCount = EntityCount,
        Metadata = DeserializeMetadata(Metadata)
    };

    private static string? SerializeMetadata(IReadOnlyDictionary<string, object?> metadata)
    {
        if (metadata.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(metadata, JsonOptions);
    }

    private static Dictionary<string, object?> DeserializeMetadata(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return new Dictionary<string, object?>();
        }

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions);
            return dict ?? new Dictionary<string, object?>();
        }
        catch
        {
            return new Dictionary<string, object?>();
        }
    }
}
