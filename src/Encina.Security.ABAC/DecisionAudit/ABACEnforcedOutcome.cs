namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// What the Policy Enforcement Point actually did with a decision (as opposed to what the Policy
/// Decision Point answered): the outcome a decision audit record describes.
/// </summary>
/// <remarks>
/// The Policy Decision Point can answer Permit and the PEP still deny (a mandatory obligation failed),
/// and in <see cref="ABACEnforcementMode.Warn"/> mode a definite denial lets the request through.
/// The record therefore carries the enforced outcome, not the raw effect.
/// </remarks>
public enum ABACEnforcedOutcome
{
    /// <summary>The request was permitted and proceeds to the handler.</summary>
    Granted = 0,

    /// <summary>The request was denied: it does not reach the handler.</summary>
    Denied = 1,

    /// <summary>
    /// The decision is a definite denial but the enforcement mode is <see cref="ABACEnforcementMode.Warn"/>,
    /// so the request proceeds anyway.
    /// </summary>
    DeniedNotEnforced = 2
}
