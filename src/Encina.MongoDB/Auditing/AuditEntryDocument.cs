using System.Text.Json;
using Encina.Security.Audit;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.Auditing;

/// <summary>
/// MongoDB document representation of an <see cref="AuditEntry"/>.
/// </summary>
/// <remarks>
/// <para>
/// This document class maps to the <see cref="AuditEntry"/> record from the security audit library,
/// providing MongoDB-specific serialization attributes for BSON storage.
/// </para>
/// <para>
/// Uses snake_case naming convention for MongoDB field names to follow
/// MongoDB community conventions.
/// </para>
/// </remarks>
public sealed class AuditEntryDocument
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
    /// Gets or sets the correlation ID for distributed tracing.
    /// </summary>
    [BsonElement("correlation_id")]
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID who initiated the operation.
    /// </summary>
    [BsonElement("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID for multi-tenant applications.
    /// </summary>
    [BsonElement("tenant_id")]
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the action performed.
    /// </summary>
    [BsonElement("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of entity being operated on.
    /// </summary>
    [BsonElement("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the specific entity identifier.
    /// </summary>
    [BsonElement("entity_id")]
    public string? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the outcome of the operation.
    /// </summary>
    [BsonElement("outcome")]
    public int Outcome { get; set; }

    /// <summary>
    /// Gets or sets the error message when outcome is not success.
    /// </summary>
    [BsonElement("error_message")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the operation was executed.
    /// </summary>
    [BsonElement("timestamp_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime TimestampUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the operation started.
    /// </summary>
    [BsonElement("started_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime StartedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the operation completed.
    /// </summary>
    [BsonElement("completed_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CompletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the client.
    /// </summary>
    [BsonElement("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the User-Agent header from the HTTP request.
    /// </summary>
    [BsonElement("user_agent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the SHA-256 hash of the sanitized request payload.
    /// </summary>
    [BsonElement("request_payload_hash")]
    public string? RequestPayloadHash { get; set; }

    /// <summary>
    /// Gets or sets the full JSON representation of the request payload.
    /// </summary>
    [BsonElement("request_payload")]
    public string? RequestPayload { get; set; }

    /// <summary>
    /// Gets or sets the full JSON representation of the response payload.
    /// </summary>
    [BsonElement("response_payload")]
    public string? ResponsePayload { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized metadata dictionary.
    /// </summary>
    [BsonElement("metadata")]
    public string? Metadata { get; set; }

    /// <summary>
    /// Creates an <see cref="AuditEntryDocument"/> from an <see cref="AuditEntry"/>.
    /// </summary>
    /// <param name="entry">The audit entry to convert.</param>
    /// <returns>A new document representation of the entry.</returns>
    public static AuditEntryDocument FromEntry(AuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new AuditEntryDocument
        {
            Id = entry.Id,
            CorrelationId = entry.CorrelationId,
            UserId = entry.UserId,
            TenantId = entry.TenantId,
            Action = entry.Action,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            Outcome = (int)entry.Outcome,
            ErrorMessage = entry.ErrorMessage,
            TimestampUtc = entry.TimestampUtc,
            StartedAtUtc = entry.StartedAtUtc.UtcDateTime,
            CompletedAtUtc = entry.CompletedAtUtc.UtcDateTime,
            IpAddress = entry.IpAddress,
            UserAgent = entry.UserAgent,
            RequestPayloadHash = entry.RequestPayloadHash,
            RequestPayload = entry.RequestPayload,
            ResponsePayload = entry.ResponsePayload,
            Metadata = SerializeMetadata(entry.Metadata)
        };
    }

    /// <summary>
    /// Converts this document to an <see cref="AuditEntry"/> record.
    /// </summary>
    /// <returns>An audit entry record.</returns>
    public AuditEntry ToEntry() => new()
    {
        Id = Id,
        CorrelationId = CorrelationId,
        UserId = UserId,
        TenantId = TenantId,
        Action = Action,
        EntityType = EntityType,
        EntityId = EntityId,
        Outcome = (AuditOutcome)Outcome,
        ErrorMessage = ErrorMessage,
        TimestampUtc = TimestampUtc,
        StartedAtUtc = new DateTimeOffset(StartedAtUtc, TimeSpan.Zero),
        CompletedAtUtc = new DateTimeOffset(CompletedAtUtc, TimeSpan.Zero),
        IpAddress = IpAddress,
        UserAgent = UserAgent,
        RequestPayloadHash = RequestPayloadHash,
        RequestPayload = RequestPayload,
        ResponsePayload = ResponsePayload,
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
