namespace Encina.Compliance.AIAct.Model;

/// <summary>
/// Determines how the <c>AIActCompliancePipelineBehavior</c> handles compliance violations.
/// </summary>
/// <remarks>
/// <para>
/// The enforcement mode controls the behaviour when a request fails an AI Act compliance check
/// (prohibited use detected, human oversight missing, transparency disclosure absent, etc.).
/// </para>
/// <para>
/// This follows the same enforcement pattern used across Encina compliance modules
/// (e.g., <c>NIS2EnforcementMode</c>, <c>GDPREnforcementMode</c>).
/// </para>
/// </remarks>
public enum AIActEnforcementMode
{
    /// <summary>
    /// Block the request and return an error when a compliance check fails.
    /// </summary>
    /// <remarks>
    /// The pipeline behaviour returns <c>Left&lt;EncinaError&gt;</c> with a descriptive
    /// AI Act error code. The request handler is not invoked.
    /// Recommended for production environments with mature AI governance controls.
    /// </remarks>
    Block = 0,

    /// <summary>
    /// Log a warning but allow the request to proceed when a compliance check fails.
    /// </summary>
    /// <remarks>
    /// The pipeline behaviour logs a structured warning via <c>ILogger</c> and records
    /// OpenTelemetry metrics, but the request handler is still invoked.
    /// Recommended during adoption or rollout phases to identify compliance gaps without
    /// disrupting operations.
    /// </remarks>
    Warn = 1,

    /// <summary>
    /// Disable all AI Act compliance checks in the pipeline behaviour.
    /// </summary>
    /// <remarks>
    /// The pipeline behaviour is a no-op — no attribute detection, no validation,
    /// no logging, no metrics. Useful for development or testing environments
    /// where compliance enforcement is not needed.
    /// </remarks>
    Disabled = 2
}
