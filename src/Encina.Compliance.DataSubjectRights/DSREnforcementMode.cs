namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Controls how the processing restriction pipeline behavior responds to active restrictions.
/// </summary>
/// <remarks>
/// <para>
/// The enforcement mode determines whether active processing restrictions block processing,
/// emit warnings, or are ignored entirely. This supports gradual adoption of
/// restriction enforcement in existing applications.
/// </para>
/// <para>
/// This follows the same pattern as <c>LawfulBasisEnforcementMode</c> (GDPR) and
/// <c>ConsentEnforcementMode</c> (Consent) to maintain consistency across compliance modules.
/// </para>
/// </remarks>
public enum DSREnforcementMode
{
    /// <summary>
    /// Requests targeting restricted data subjects are blocked and an error is returned.
    /// </summary>
    /// <remarks>
    /// This is the recommended mode for production systems where GDPR Article 18 compliance
    /// is mandatory. Requests targeting a data subject with an active processing restriction
    /// will receive a <c>DSRErrors.RestrictionActive</c> error.
    /// </remarks>
    Block = 0,

    /// <summary>
    /// Requests targeting restricted data subjects log a warning but are allowed to proceed.
    /// </summary>
    /// <remarks>
    /// Useful during migration or testing phases when restriction enforcement is being
    /// gradually introduced. All violations are logged at Warning level.
    /// </remarks>
    Warn = 1,

    /// <summary>
    /// Processing restriction validation is completely disabled. The pipeline behavior is a no-op.
    /// </summary>
    /// <remarks>
    /// Useful for development environments or scenarios where processing restrictions are managed
    /// externally. No validation, logging, or metrics are emitted.
    /// </remarks>
    Disabled = 2
}
