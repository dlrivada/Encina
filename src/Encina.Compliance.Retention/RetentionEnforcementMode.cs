namespace Encina.Compliance.Retention;

/// <summary>
/// Controls how the retention pipeline behavior responds when a retention-decorated response
/// lacks a corresponding retention policy or when record creation fails.
/// </summary>
/// <remarks>
/// <para>
/// The enforcement mode determines whether policy resolution failures block the response,
/// emit warnings, or are ignored entirely. This supports gradual adoption of retention
/// tracking in existing applications.
/// </para>
/// <para>
/// This follows the same pattern as <c>AnonymizationEnforcementMode</c> (Anonymization),
/// <c>DSREnforcementMode</c> (DataSubjectRights), <c>LawfulBasisEnforcementMode</c> (GDPR),
/// and <c>ConsentEnforcementMode</c> (Consent) to maintain consistency across compliance modules.
/// </para>
/// <para>
/// Per GDPR Article 5(1)(e) (storage limitation), the <see cref="Block"/> mode ensures
/// that no data is returned without proper retention tracking in place.
/// </para>
/// </remarks>
public enum RetentionEnforcementMode
{
    /// <summary>
    /// Retention tracking failures cause the request to be blocked and an error is returned.
    /// </summary>
    /// <remarks>
    /// This is the recommended mode for production systems where retention compliance is mandatory.
    /// If a response is decorated with <c>[RetentionPeriod]</c> but a retention record cannot be
    /// created (e.g., no matching policy, store failure), the entire response is withheld and a
    /// <see cref="RetentionErrors"/> error is returned to the caller.
    /// </remarks>
    Block = 0,

    /// <summary>
    /// Retention tracking failures log a warning but allow the response to proceed.
    /// </summary>
    /// <remarks>
    /// Useful during migration or testing phases when retention tracking is being gradually introduced.
    /// All failures are logged at Warning level with full field and policy details.
    /// </remarks>
    Warn = 1,

    /// <summary>
    /// Retention pipeline behavior is completely disabled. No retention records are created.
    /// </summary>
    /// <remarks>
    /// Useful for development environments or scenarios where retention is managed externally.
    /// No validation, logging, or metrics are emitted.
    /// </remarks>
    Disabled = 2
}
