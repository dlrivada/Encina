namespace Encina.Compliance.BreachNotification.Model;

/// <summary>
/// Controls how the breach detection pipeline behavior responds when a potential
/// breach is detected during request processing.
/// </summary>
/// <remarks>
/// <para>
/// The enforcement mode determines whether detected breaches block the request,
/// emit warnings, or are ignored entirely. This supports gradual adoption of
/// breach detection in existing applications.
/// </para>
/// <para>
/// This follows the same pattern as <c>RetentionEnforcementMode</c> (Retention),
/// <c>AnonymizationEnforcementMode</c> (Anonymization), <c>DSREnforcementMode</c>
/// (DataSubjectRights), <c>LawfulBasisEnforcementMode</c> (GDPR), and
/// <c>ConsentEnforcementMode</c> (Consent) to maintain consistency across compliance modules.
/// </para>
/// <para>
/// Per GDPR Article 33(1), the controller must notify the supervisory authority
/// "without undue delay and, where feasible, not later than 72 hours after having
/// become aware of it." Prompt detection via the pipeline behavior helps meet
/// this obligation by identifying potential breaches as they occur.
/// </para>
/// </remarks>
public enum BreachDetectionEnforcementMode
{
    /// <summary>
    /// Detected breaches cause the request to be blocked and an error is returned.
    /// </summary>
    /// <remarks>
    /// This is the strictest mode. When the <c>BreachDetectionPipelineBehavior</c>
    /// detects a potential breach via registered <c>IBreachDetectionRule</c> instances,
    /// the response is withheld and a <c>BreachNotificationErrors</c> error is returned
    /// to the caller.
    /// </remarks>
    Block = 0,

    /// <summary>
    /// Detected breaches log a warning but allow the response to proceed.
    /// </summary>
    /// <remarks>
    /// Useful during migration or testing phases when breach detection is being
    /// gradually introduced. All detected breaches are logged at Warning level
    /// with full event and rule details.
    /// </remarks>
    Warn = 1,

    /// <summary>
    /// Breach detection pipeline behavior is completely disabled.
    /// </summary>
    /// <remarks>
    /// No security events are evaluated and no breach detection rules are executed.
    /// Useful for development environments or scenarios where breach detection is
    /// managed externally.
    /// </remarks>
    Disabled = 2
}
