namespace Encina.Compliance.Anonymization;

/// <summary>
/// Controls how the anonymization pipeline behavior responds when a transformation fails.
/// </summary>
/// <remarks>
/// <para>
/// The enforcement mode determines whether transformation failures block the response,
/// emit warnings, or are ignored entirely. This supports gradual adoption of
/// anonymization enforcement in existing applications.
/// </para>
/// <para>
/// This follows the same pattern as <c>DSREnforcementMode</c> (DataSubjectRights),
/// <c>LawfulBasisEnforcementMode</c> (GDPR), and <c>ConsentEnforcementMode</c> (Consent)
/// to maintain consistency across compliance modules.
/// </para>
/// </remarks>
public enum AnonymizationEnforcementMode
{
    /// <summary>
    /// Transformation failures cause the request to be blocked and an error is returned.
    /// </summary>
    /// <remarks>
    /// This is the recommended mode for production systems where data protection is mandatory.
    /// If a field decorated with <c>[Anonymize]</c> or <c>[Pseudonymize]</c> cannot be
    /// transformed, the entire response is withheld and an <c>AnonymizationErrors</c> error
    /// is returned to the caller.
    /// </remarks>
    Block = 0,

    /// <summary>
    /// Transformation failures log a warning but allow the response to proceed with untransformed data.
    /// </summary>
    /// <remarks>
    /// Useful during migration or testing phases when anonymization is being gradually introduced.
    /// All failures are logged at Warning level with full field and technique details.
    /// </remarks>
    Warn = 1,

    /// <summary>
    /// Anonymization pipeline behavior is completely disabled. No transformations are applied.
    /// </summary>
    /// <remarks>
    /// Useful for development environments or scenarios where anonymization is managed externally.
    /// No validation, logging, or metrics are emitted.
    /// </remarks>
    Disabled = 2
}
