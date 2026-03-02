using Encina.Compliance.Retention.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.Retention;

/// <summary>
/// MongoDB document representation of a <see cref="RetentionPolicy"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the retention_policies collection. Each document defines a data retention
/// policy for a specific data category, including the retention period, auto-delete
/// behavior, and legal basis.
/// </para>
/// <para>
/// Uses snake_case naming convention for MongoDB field names. The retention period
/// is stored as ticks (<see cref="long"/>) since MongoDB does not have a native <see cref="TimeSpan"/>
/// type. The policy type enum is stored as an integer.
/// </para>
/// </remarks>
public sealed class RetentionPolicyDocument
{
    /// <summary>
    /// Gets or sets the unique identifier for this retention policy.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data category this policy applies to.
    /// </summary>
    [BsonElement("data_category")]
    public string DataCategory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the retention period as ticks.
    /// </summary>
    /// <remarks>
    /// MongoDB does not support <see cref="TimeSpan"/> natively, so the retention period
    /// is stored as <see cref="TimeSpan.Ticks"/>.
    /// </remarks>
    [BsonElement("retention_period_ticks")]
    public long RetentionPeriodTicks { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether expired data should be automatically deleted.
    /// </summary>
    [BsonElement("auto_delete")]
    public bool AutoDelete { get; set; }

    /// <summary>
    /// Gets or sets the human-readable reason for this retention period.
    /// </summary>
    [BsonElement("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the GDPR lawful basis or legal reference requiring this retention period.
    /// </summary>
    [BsonElement("legal_basis")]
    public string? LegalBasis { get; set; }

    /// <summary>
    /// Gets or sets the policy type as an integer value.
    /// </summary>
    /// <remarks>
    /// Maps to <see cref="RetentionPolicyType"/> enum values.
    /// </remarks>
    [BsonElement("policy_type")]
    public int PolicyTypeValue { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this policy was created (UTC).
    /// </summary>
    [BsonElement("created_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this policy was last modified (UTC).
    /// </summary>
    [BsonElement("last_modified_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? LastModifiedAtUtc { get; set; }

    /// <summary>
    /// Creates a <see cref="RetentionPolicyDocument"/> from a <see cref="RetentionPolicy"/>.
    /// </summary>
    /// <param name="policy">The retention policy to convert.</param>
    /// <returns>A new document representation of the retention policy.</returns>
    public static RetentionPolicyDocument FromPolicy(RetentionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return new RetentionPolicyDocument
        {
            Id = policy.Id,
            DataCategory = policy.DataCategory,
            RetentionPeriodTicks = policy.RetentionPeriod.Ticks,
            AutoDelete = policy.AutoDelete,
            Reason = policy.Reason,
            LegalBasis = policy.LegalBasis,
            PolicyTypeValue = (int)policy.PolicyType,
            CreatedAtUtc = policy.CreatedAtUtc.UtcDateTime,
            LastModifiedAtUtc = policy.LastModifiedAtUtc?.UtcDateTime
        };
    }

    /// <summary>
    /// Converts this document to a <see cref="RetentionPolicy"/>.
    /// </summary>
    /// <returns>A retention policy record.</returns>
    public RetentionPolicy ToPolicy() => new()
    {
        Id = Id,
        DataCategory = DataCategory,
        RetentionPeriod = TimeSpan.FromTicks(RetentionPeriodTicks),
        AutoDelete = AutoDelete,
        Reason = Reason,
        LegalBasis = LegalBasis,
        PolicyType = (RetentionPolicyType)PolicyTypeValue,
        CreatedAtUtc = new DateTimeOffset(CreatedAtUtc, TimeSpan.Zero),
        LastModifiedAtUtc = LastModifiedAtUtc.HasValue
            ? new DateTimeOffset(LastModifiedAtUtc.Value, TimeSpan.Zero)
            : null
    };
}
