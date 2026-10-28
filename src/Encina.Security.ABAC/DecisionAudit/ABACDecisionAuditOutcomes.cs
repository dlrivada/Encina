namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// Selects which enforced outcomes (see <see cref="ABACEnforcedOutcome"/>) the decision audit records.
/// </summary>
[Flags]
public enum ABACDecisionAuditOutcomes
{
    /// <summary>No regular outcome is recorded; only errors, which are always recorded, remain.</summary>
    None = 0,

    /// <summary>Requests that were permitted (<see cref="ABACEnforcedOutcome.Granted"/>).</summary>
    Granted = 1,

    /// <summary>Requests that were denied (<see cref="ABACEnforcedOutcome.Denied"/>).</summary>
    Denied = 2,

    /// <summary>
    /// Definite denials that were not enforced because the mode is <see cref="ABACEnforcementMode.Warn"/>
    /// (<see cref="ABACEnforcedOutcome.DeniedNotEnforced"/>).
    /// </summary>
    NotEnforced = 4,

    /// <summary>
    /// Definite denials that an emergency elevation overrode (<see cref="ABACEnforcedOutcome.DeniedElevated"/>).
    /// Such a decision is recorded whatever this selection says, like an error: the flag only keeps
    /// <see cref="All"/> complete.
    /// </summary>
    Elevated = 8,

    /// <summary>Every outcome.</summary>
    All = Granted | Denied | NotEnforced | Elevated
}
