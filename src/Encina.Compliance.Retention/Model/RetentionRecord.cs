namespace Encina.Compliance.Retention.Model;

/// <summary>
/// Tracks the retention lifecycle of a specific data entity against its retention policy.
/// </summary>
/// <remarks>
/// <para>
/// Each retention record associates a data entity (identified by <see cref="EntityId"/>)
/// with a data category and an expiration date. The record tracks whether the entity's
/// data is still within the retention period, has expired, has been deleted, or is
/// protected by a legal hold.
/// </para>
/// <para>
/// Retention records are created automatically by the <c>RetentionValidationPipelineBehavior</c>
/// when data decorated with the <c>[RetentionPeriod]</c> attribute is created, or manually
/// via the <c>IRetentionRecordStore</c>.
/// </para>
/// <para>
/// Per GDPR Article 5(1)(e) (storage limitation), this record enables the controller to
/// demonstrate that personal data is not kept longer than necessary by providing an auditable
/// trail of when data was created, when it expires, and when it was deleted.
/// </para>
/// </remarks>
public sealed record RetentionRecord
{
    /// <summary>
    /// Unique identifier for this retention record.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Identifier of the data entity being tracked for retention.
    /// </summary>
    /// <remarks>
    /// This should be a stable, unique identifier for the entity (e.g., order ID,
    /// invoice number, user session ID). Used to correlate with the actual data
    /// for deletion purposes.
    /// </remarks>
    public required string EntityId { get; init; }

    /// <summary>
    /// The data category this record belongs to, linking it to a retention policy.
    /// </summary>
    /// <remarks>
    /// Must correspond to a <see cref="RetentionPolicy.DataCategory"/> value.
    /// Examples: "financial-records", "session-logs", "marketing-consent".
    /// </remarks>
    public required string DataCategory { get; init; }

    /// <summary>
    /// Identifier of the retention policy governing this record.
    /// </summary>
    /// <remarks>
    /// <c>null</c> if the record was created manually without a policy reference,
    /// or if the policy was deleted after the record was created.
    /// </remarks>
    public string? PolicyId { get; init; }

    /// <summary>
    /// Timestamp when the data entity was created or registered for retention tracking (UTC).
    /// </summary>
    /// <remarks>
    /// For time-based policies, the expiration date is calculated as
    /// <c>CreatedAtUtc + RetentionPeriod</c>.
    /// </remarks>
    public required DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// Timestamp when the data entity's retention period expires (UTC).
    /// </summary>
    /// <remarks>
    /// After this timestamp, the data is eligible for deletion (unless a legal hold
    /// prevents it). Calculated from <see cref="CreatedAtUtc"/> plus the retention
    /// policy's <see cref="RetentionPolicy.RetentionPeriod"/>.
    /// </remarks>
    public required DateTimeOffset ExpiresAtUtc { get; init; }

    /// <summary>
    /// The current lifecycle status of this retention record.
    /// </summary>
    public required RetentionStatus Status { get; init; }

    /// <summary>
    /// Timestamp when the data was actually deleted (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> while the data has not been deleted. Set when the retention
    /// enforcement process successfully deletes the data.
    /// </remarks>
    public DateTimeOffset? DeletedAtUtc { get; init; }

    /// <summary>
    /// Identifier of the legal hold protecting this record from deletion.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when no legal hold is active. When set, the record's
    /// <see cref="Status"/> should be <see cref="RetentionStatus.UnderLegalHold"/>.
    /// </remarks>
    public string? LegalHoldId { get; init; }

    /// <summary>
    /// Creates a new retention record with a generated unique identifier.
    /// </summary>
    /// <param name="entityId">Identifier of the data entity.</param>
    /// <param name="dataCategory">The data category for retention policy lookup.</param>
    /// <param name="createdAtUtc">When the data entity was created.</param>
    /// <param name="expiresAtUtc">When the data entity's retention period expires.</param>
    /// <param name="policyId">Identifier of the governing retention policy.</param>
    /// <returns>A new <see cref="RetentionRecord"/> with <see cref="RetentionStatus.Active"/> status.</returns>
    public static RetentionRecord Create(
        string entityId,
        string dataCategory,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        string? policyId = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            EntityId = entityId,
            DataCategory = dataCategory,
            PolicyId = policyId,
            CreatedAtUtc = createdAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            Status = RetentionStatus.Active
        };
}
