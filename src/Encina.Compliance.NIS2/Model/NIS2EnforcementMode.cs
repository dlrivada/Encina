namespace Encina.Compliance.NIS2.Model;

/// <summary>
/// Determines how the <c>NIS2CompliancePipelineBehavior</c> handles compliance violations.
/// </summary>
/// <remarks>
/// <para>
/// The enforcement mode controls the behavior when a request fails an NIS2 compliance check
/// (MFA not enabled, high-risk supplier, encryption not validated, etc.).
/// </para>
/// <para>
/// This follows the same enforcement pattern used across Encina compliance modules
/// (e.g., <c>BreachDetectionEnforcementMode</c>, <c>GDPREnforcementMode</c>).
/// </para>
/// </remarks>
public enum NIS2EnforcementMode
{
    /// <summary>
    /// Block the request and return an error when a compliance check fails.
    /// </summary>
    /// <remarks>
    /// The pipeline behavior returns <c>Left&lt;EncinaError&gt;</c> with a descriptive
    /// NIS2 error code. The request handler is not invoked.
    /// Recommended for production environments with mature compliance controls.
    /// </remarks>
    Block = 0,

    /// <summary>
    /// Log a warning but allow the request to proceed when a compliance check fails.
    /// </summary>
    /// <remarks>
    /// The pipeline behavior logs a structured warning via <c>ILogger</c> and records
    /// OpenTelemetry metrics, but the request handler is still invoked.
    /// Recommended during adoption/rollout phases to identify compliance gaps without
    /// disrupting operations.
    /// </remarks>
    Warn = 1,

    /// <summary>
    /// Disable all NIS2 compliance checks in the pipeline behavior.
    /// </summary>
    /// <remarks>
    /// The pipeline behavior is a no-op — no attribute detection, no validation,
    /// no logging, no metrics. Useful for development or testing environments
    /// where compliance enforcement is not needed.
    /// </remarks>
    Disabled = 2
}
