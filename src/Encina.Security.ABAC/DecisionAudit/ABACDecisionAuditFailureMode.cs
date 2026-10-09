namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// What the Policy Enforcement Point does with a request that would proceed when the decision
/// record cannot be written.
/// </summary>
/// <remarks>
/// A request that is denied anyway stays denied in both modes; the failure is logged.
/// </remarks>
public enum ABACDecisionAuditFailureMode
{
    /// <summary>
    /// The request is denied with <see cref="ABACErrors.DecisionAuditFailedCode"/>: no access without
    /// evidence. This is the default.
    /// </summary>
    FailClosed = 0,

    /// <summary>
    /// The request proceeds and the failure is logged: an explicit opt-out for applications that
    /// prefer availability to a complete trail.
    /// </summary>
    BestEffort = 1
}
