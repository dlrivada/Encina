namespace Encina.Compliance.DataResidency.Model;

/// <summary>
/// Classification of actions recorded in the data residency audit trail.
/// </summary>
/// <remarks>
/// Each action in the residency audit trail is classified to enable filtering,
/// reporting, and compliance analysis. The action type is recorded alongside
/// the outcome in <c>ResidencyAuditEntry</c>.
/// </remarks>
public enum ResidencyAction
{
    /// <summary>
    /// A residency policy was checked for a data processing request.
    /// </summary>
    PolicyCheck = 0,

    /// <summary>
    /// A cross-border data transfer was validated.
    /// </summary>
    CrossBorderTransfer = 1,

    /// <summary>
    /// A data location was recorded for tracking purposes.
    /// </summary>
    LocationRecord = 2,

    /// <summary>
    /// A residency policy violation was detected.
    /// </summary>
    Violation = 3,

    /// <summary>
    /// A request was routed to a specific region for processing.
    /// </summary>
    RegionRouting = 4
}
