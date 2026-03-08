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
    /// Deny decisions are logged as warnings but do not block request execution.
    /// </summary>
    /// <remarks>Observation mode — useful for validating policies before enforcement. All decisions are logged for analysis.</remarks>
    Warn,

    /// <summary>
    /// ABAC evaluation is completely skipped.
    /// </summary>
    /// <remarks>Bypass mode — no policies are evaluated, no obligations are executed. Useful for development or feature-flagging ABAC.</remarks>
    Disabled
}
