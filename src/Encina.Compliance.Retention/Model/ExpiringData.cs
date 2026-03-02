namespace Encina.Compliance.Retention.Model;

/// <summary>
/// Represents a data record that is approaching its retention expiration date.
/// </summary>
/// <remarks>
/// <para>
/// Expiring data records are surfaced by the retention enforcement service when
/// data is within the configured alert window (e.g., 7 days before expiration).
/// This enables controllers to review and prepare for upcoming deletions.
/// </para>
/// <para>
/// Per GDPR Recital 39, appropriate measures should be taken to ensure that personal
/// data are not kept longer than necessary. Expiration alerts support proactive
/// compliance management.
/// </para>
/// </remarks>
public sealed record ExpiringData
{
    /// <summary>
    /// Identifier of the data entity approaching expiration.
    /// </summary>
    public required string EntityId { get; init; }

    /// <summary>
    /// The data category of the expiring record.
    /// </summary>
    public required string DataCategory { get; init; }

    /// <summary>
    /// Timestamp when the data entity's retention period expires (UTC).
    /// </summary>
    public required DateTimeOffset ExpiresAtUtc { get; init; }

    /// <summary>
    /// Identifier of the retention policy governing this record.
    /// </summary>
    /// <remarks>
    /// <c>null</c> if no policy is associated with this record.
    /// </remarks>
    public string? PolicyId { get; init; }

    /// <summary>
    /// Number of days until the data expires.
    /// </summary>
    /// <remarks>
    /// Negative values indicate the data has already expired. Zero indicates
    /// the data expires today.
    /// </remarks>
    public required int DaysUntilExpiration { get; init; }
}
