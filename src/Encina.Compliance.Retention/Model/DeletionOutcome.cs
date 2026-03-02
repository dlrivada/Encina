namespace Encina.Compliance.Retention.Model;

/// <summary>
/// Outcome of a retention enforcement action on a single data record.
/// </summary>
/// <remarks>
/// <para>
/// Each record evaluated during retention enforcement receives one of these
/// outcomes. The outcomes are tracked in <see cref="DeletionDetail"/> records
/// and aggregated in <see cref="DeletionResult"/> for audit and reporting.
/// </para>
/// <para>
/// Per GDPR Article 5(2) (accountability), controllers must be able to demonstrate
/// what actions were taken for each data record during retention enforcement.
/// </para>
/// </remarks>
public enum DeletionOutcome
{
    /// <summary>
    /// The data was successfully deleted.
    /// </summary>
    Deleted = 0,

    /// <summary>
    /// The data was retained because it is still within its retention period.
    /// </summary>
    Retained = 1,

    /// <summary>
    /// The deletion attempt failed due to an error.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// The data was not deleted because it is under an active legal hold.
    /// </summary>
    /// <remarks>
    /// Legal holds suspend deletion per GDPR Article 17(3)(e) — exemption
    /// for the establishment, exercise, or defence of legal claims.
    /// </remarks>
    HeldByLegalHold = 3,

    /// <summary>
    /// The record was skipped during enforcement (e.g., already deleted or no matching policy).
    /// </summary>
    Skipped = 4
}
