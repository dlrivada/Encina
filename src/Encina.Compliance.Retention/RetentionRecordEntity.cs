namespace Encina.Compliance.Retention;

/// <summary>
/// Persistence entity for <see cref="Model.RetentionRecord"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a retention record,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// The <see cref="StatusValue"/> property stores the <see cref="Model.RetentionStatus"/> enum
/// as an integer for cross-provider compatibility. Timestamps use <see cref="DateTimeOffset"/>
/// for UTC precision across all providers.
/// </para>
/// <para>
/// Use <see cref="RetentionRecordMapper"/> to convert between this entity and
/// <see cref="Model.RetentionRecord"/>.
/// </para>
/// </remarks>
public sealed class RetentionRecordEntity
{
    /// <summary>
    /// Unique identifier for this retention record.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Identifier of the data entity being tracked for retention.
    /// </summary>
    /// <remarks>
    /// An INDEX should be created on this column for efficient entity lookups.
    /// </remarks>
    public required string EntityId { get; set; }

    /// <summary>
    /// The data category this record belongs to, linking it to a retention policy.
    /// </summary>
    /// <remarks>
    /// An INDEX should be created on this column for efficient category-based queries.
    /// </remarks>
    public required string DataCategory { get; set; }

    /// <summary>
    /// Identifier of the retention policy governing this record.
    /// </summary>
    public string? PolicyId { get; set; }

    /// <summary>
    /// Timestamp when the data entity was created or registered for retention tracking (UTC).
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when the data entity's retention period expires (UTC).
    /// </summary>
    /// <remarks>
    /// An INDEX should be created on this column for efficient expiration queries.
    /// </remarks>
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>
    /// Integer value of the <see cref="Model.RetentionStatus"/> enum.
    /// </summary>
    /// <remarks>
    /// Values: Active=0, Expired=1, Deleted=2, UnderLegalHold=3.
    /// </remarks>
    public required int StatusValue { get; set; }

    /// <summary>
    /// Timestamp when the data was actually deleted (UTC), if applicable.
    /// </summary>
    public DateTimeOffset? DeletedAtUtc { get; set; }

    /// <summary>
    /// Identifier of the legal hold protecting this record from deletion.
    /// </summary>
    public string? LegalHoldId { get; set; }
}
