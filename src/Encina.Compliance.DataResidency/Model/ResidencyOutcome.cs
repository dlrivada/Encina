namespace Encina.Compliance.DataResidency.Model;

/// <summary>
/// Outcome of a data residency check or transfer validation.
/// </summary>
/// <remarks>
/// The outcome is recorded in <c>ResidencyAuditEntry</c> alongside
/// the <see cref="ResidencyAction"/> to provide a complete audit trail
/// of all residency enforcement decisions.
/// </remarks>
public enum ResidencyOutcome
{
    /// <summary>
    /// The action was allowed — the data processing or transfer is compliant.
    /// </summary>
    Allowed = 0,

    /// <summary>
    /// The action was blocked — the data processing or transfer was denied.
    /// </summary>
    Blocked = 1,

    /// <summary>
    /// A warning was issued but the action was allowed to proceed.
    /// </summary>
    /// <remarks>
    /// This occurs when the enforcement mode is set to <c>Warn</c>. The action
    /// is logged for review but not blocked. This is useful during migration
    /// or initial deployment when policies are being tuned.
    /// </remarks>
    Warning = 2,

    /// <summary>
    /// The residency check was skipped entirely.
    /// </summary>
    /// <remarks>
    /// This occurs when the enforcement mode is set to <c>Disabled</c> or
    /// when the request type has no residency attributes.
    /// </remarks>
    Skipped = 3
}
