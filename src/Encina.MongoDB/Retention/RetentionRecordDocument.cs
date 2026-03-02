using Encina.Compliance.Retention.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.Retention;

/// <summary>
/// MongoDB document representation of a <see cref="RetentionRecord"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the retention_records collection. Each document tracks the retention lifecycle
/// of a specific data entity against its retention policy, including creation time,
/// expiration time, and current lifecycle status.
/// </para>
/// <para>
/// Uses snake_case naming convention for MongoDB field names. The status
/// enum is stored as an integer (<see cref="StatusValue"/>).
/// </para>
/// </remarks>
public sealed class RetentionRecordDocument
{
    /// <summary>
    /// Gets or sets the unique identifier for this retention record.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the data entity being tracked.
    /// </summary>
    [BsonElement("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data category this record belongs to.
    /// </summary>
    [BsonElement("data_category")]
    public string DataCategory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the retention policy governing this record.
    /// </summary>
    [BsonElement("policy_id")]
    public string? PolicyId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the data entity was created (UTC).
    /// </summary>
    [BsonElement("created_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the data entity's retention period expires (UTC).
    /// </summary>
    [BsonElement("expires_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the lifecycle status as an integer value.
    /// </summary>
    /// <remarks>
    /// Maps to <see cref="RetentionStatus"/> enum values:
    /// 0 = Active, 1 = Expired, 2 = Deleted, 3 = UnderLegalHold.
    /// </remarks>
    [BsonElement("status")]
    public int StatusValue { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the data was actually deleted (UTC).
    /// </summary>
    [BsonElement("deleted_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? DeletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the legal hold protecting this record.
    /// </summary>
    [BsonElement("legal_hold_id")]
    public string? LegalHoldId { get; set; }

    /// <summary>
    /// Creates a <see cref="RetentionRecordDocument"/> from a <see cref="RetentionRecord"/>.
    /// </summary>
    /// <param name="record">The retention record to convert.</param>
    /// <returns>A new document representation of the retention record.</returns>
    public static RetentionRecordDocument FromRecord(RetentionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new RetentionRecordDocument
        {
            Id = record.Id,
            EntityId = record.EntityId,
            DataCategory = record.DataCategory,
            PolicyId = record.PolicyId,
            CreatedAtUtc = record.CreatedAtUtc.UtcDateTime,
            ExpiresAtUtc = record.ExpiresAtUtc.UtcDateTime,
            StatusValue = (int)record.Status,
            DeletedAtUtc = record.DeletedAtUtc?.UtcDateTime,
            LegalHoldId = record.LegalHoldId
        };
    }

    /// <summary>
    /// Converts this document to a <see cref="RetentionRecord"/>.
    /// </summary>
    /// <returns>A retention record.</returns>
    public RetentionRecord ToRecord() => new()
    {
        Id = Id,
        EntityId = EntityId,
        DataCategory = DataCategory,
        PolicyId = PolicyId,
        CreatedAtUtc = new DateTimeOffset(CreatedAtUtc, TimeSpan.Zero),
        ExpiresAtUtc = new DateTimeOffset(ExpiresAtUtc, TimeSpan.Zero),
        Status = (RetentionStatus)StatusValue,
        DeletedAtUtc = DeletedAtUtc.HasValue
            ? new DateTimeOffset(DeletedAtUtc.Value, TimeSpan.Zero)
            : null,
        LegalHoldId = LegalHoldId
    };
}
