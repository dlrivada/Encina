namespace Encina.Security.ABAC;

/// <summary>
/// Determines how the ABAC pipeline behavior enforces authorization decisions.
/// </summary>
/// <remarks>
/// <para>
/// The enforcement mode allows gradual rollout of ABAC policies in production systems.
/// Start with <see cref="Warn"/> to observe decisions without blocking requests,
/// then switch to <see cref="Block"/> once policies are validated.
/// </para>
/// <para>
/// This mode is configured via <c>ABACOptions.EnforcementMode</c> and applies
/// globally to the <c>ABACPipelineBehavior</c>.
/// </para>
/// </remarks>
public enum ABACEnforcementMode
{
    /// <summary>
    /// Deny decisions block request execution and return an error.
    /// </summary>
    /// <remarks>Production mode — the PEP enforces all authorization decisions. If the PDP returns Deny, the request is rejected.</remarks>
    Block,

    /// <summary>
    /// Definite denials are logged as warnings but do not block request execution; evaluation
    /// errors still deny.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Observation mode — useful for validating policies before enforcement. All decisions are logged for analysis.
    /// </para>
    /// <para>
    /// Warn relaxes only definite verdicts: a Deny, a required policy that returns Deny or
    /// NotApplicable or is not found (<c>encina.authorization.abac_policy_not_found</c>), and a required condition that
    /// evaluates to <c>false</c> (<c>encina.authorization.abac_condition_not_met</c>). These are logged and the request
    /// proceeds.
    /// </para>
    /// <para>
    /// Errors deny exactly as in <see cref="Block"/>, because they say nothing about whether the
    /// request should be allowed: an Indeterminate result (<c>abac.indeterminate</c>: a condition
    /// that does not compile or throws, a policy store failure, a PDP error), an exception from the
    /// attribute provider or the PDP (<c>abac.evaluation_failed</c>), and a mandatory obligation
    /// that cannot be fulfilled (<c>encina.authorization.abac_obligation_failed</c>).
    /// </para>
    /// </remarks>
    Warn,

    /// <summary>
    /// ABAC evaluation is completely skipped.
    /// </summary>
    /// <remarks>Bypass mode — no policies are evaluated, no obligations are executed. Useful for development or feature-flagging ABAC.</remarks>
    Disabled
}
