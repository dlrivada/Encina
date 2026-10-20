namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// What the Policy Enforcement Point does with a request that would proceed when the decision
/// record cannot be written or cannot be built.
/// </summary>
/// <remarks>
/// A request that is denied anyway stays denied, with its original error, in both modes; the failure
/// is logged. A record that cannot be built (for example a request whose resource id getter throws)
/// is an audit failure exactly like a failed write.
/// </remarks>
public enum ABACDecisionAuditFailureMode
{
    /// <summary>
    /// The request is denied with <see cref="ABACErrors.DecisionAuditFailedCode"/>: no access without
    /// evidence. This is the default.
    /// </summary>
    FailClosed = 0,

    /// <summary>
    /// The request proceeds and the failure (a failed write or a record that cannot be built) is
    /// logged: an explicit opt-out for applications that prefer availability to a complete trail.
    /// </summary>
    BestEffort = 1
}
