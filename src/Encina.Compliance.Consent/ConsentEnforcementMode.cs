namespace Encina.Compliance.Consent;

/// <summary>
/// Controls how the consent pipeline behavior responds to missing or invalid consent.
/// </summary>
/// <remarks>
/// The enforcement mode determines whether consent violations block processing,
/// emit warnings, or are ignored entirely. This supports gradual adoption of
/// consent enforcement in existing applications.
/// </remarks>
public enum ConsentEnforcementMode
{
    /// <summary>
    /// Non-compliant requests are blocked and an error is returned.
    /// </summary>
    /// <remarks>
    /// This is the recommended mode for production systems where GDPR compliance
    /// is mandatory. Requests without valid consent will receive a
    /// <c>ConsentErrors.MissingConsent</c> error.
    /// </remarks>
    Block = 0,

    /// <summary>
    /// Non-compliant requests log a warning but are allowed to proceed.
    /// </summary>
    /// <remarks>
    /// Useful during migration or testing phases when consent enforcement is being
    /// gradually introduced. All consent violations are logged at Warning level.
    /// </remarks>
    Warn = 1,

    /// <summary>
    /// Consent validation is completely disabled. The pipeline behavior is a no-op.
    /// </summary>
    /// <remarks>
    /// Useful for development environments or scenarios where consent is managed
    /// externally. No validation, logging, or metrics are emitted.
    /// </remarks>
    Disabled = 2
}
