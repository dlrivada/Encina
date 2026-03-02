namespace Encina.Compliance.Retention.Model;

/// <summary>
/// Lifecycle status of a data retention record.
/// </summary>
/// <remarks>
/// <para>
/// Each retention record progresses through a defined lifecycle based on
/// the expiration of its associated retention policy. Records begin as
/// <c>Active</c> and transition to <c>Expired</c> when the retention period
/// elapses. If automatic deletion is enabled, records move to <c>Deleted</c>
/// after successful enforcement.
/// </para>
/// <para>
/// The <c>UnderLegalHold</c> status suspends all deletion activity regardless
/// of whether the retention period has expired, as required for litigation holds
/// and regulatory investigations (GDPR Article 17(3)(e) — legal claims exemption).
/// </para>
/// </remarks>
public enum RetentionStatus
{
    /// <summary>
    /// The data is within its retention period and must not be deleted.
    /// </summary>
    /// <remarks>
    /// The data is still necessary for its original purpose as defined by
    /// the retention policy (Article 5(1)(e) — storage limitation).
    /// </remarks>
    Active = 0,

    /// <summary>
    /// The retention period has elapsed and the data is eligible for deletion.
    /// </summary>
    /// <remarks>
    /// Once expired, the data should be deleted unless a legal hold
    /// prevents it. If <c>AutoDelete</c> is enabled on the policy,
    /// the enforcement service will delete the data automatically.
    /// </remarks>
    Expired = 1,

    /// <summary>
    /// The data has been successfully deleted by the retention enforcement process.
    /// </summary>
    Deleted = 2,

    /// <summary>
    /// A legal hold has been applied, suspending deletion regardless of expiration.
    /// </summary>
    /// <remarks>
    /// Legal holds take precedence over retention policies. Data under a legal hold
    /// must not be deleted until the hold is released. This supports GDPR Article 17(3)(e)
    /// exemption for the establishment, exercise, or defence of legal claims.
    /// </remarks>
    UnderLegalHold = 3
}
