namespace Encina.Compliance.DataResidency;

/// <summary>
/// Controls how the data residency pipeline behavior responds when a residency policy check
/// fails or when data would be stored in a non-compliant region.
/// </summary>
/// <remarks>
/// <para>
/// The enforcement mode determines whether policy violations block the response,
/// emit warnings, or are ignored entirely. This supports gradual adoption of data
/// residency enforcement in existing applications.
/// </para>
/// <para>
/// This follows the same pattern as <c>RetentionEnforcementMode</c> (Retention),
/// <c>AnonymizationEnforcementMode</c> (Anonymization), <c>DSREnforcementMode</c>
/// (DataSubjectRights), <c>LawfulBasisEnforcementMode</c> (GDPR), and
/// <c>ConsentEnforcementMode</c> (Consent) to maintain consistency across compliance modules.
/// </para>
/// <para>
/// Per GDPR Article 44 (general principle for transfers), the <see cref="Block"/> mode
/// ensures that no data is stored or transferred to non-compliant regions.
/// </para>
/// </remarks>
public enum DataResidencyEnforcementMode
{
    /// <summary>
    /// Residency policy violations cause the request to be blocked and an error is returned.
    /// </summary>
    /// <remarks>
    /// This is the recommended mode for production systems where data residency compliance
    /// is mandatory. If a request targets a non-compliant region, the entire operation is
    /// blocked and a <see cref="DataResidencyErrors"/> error is returned to the caller.
    /// </remarks>
    Block = 0,

    /// <summary>
    /// Residency policy violations log a warning but allow the request to proceed.
    /// </summary>
    /// <remarks>
    /// Useful during migration or testing phases when data residency enforcement is being
    /// gradually introduced. All violations are logged at Warning level with full region
    /// and policy details.
    /// </remarks>
    Warn = 1,

    /// <summary>
    /// Data residency pipeline behavior is completely disabled. No policy checks are performed.
    /// </summary>
    /// <remarks>
    /// Useful for development environments or scenarios where data residency is managed externally.
    /// No validation, logging, or metrics are emitted.
    /// </remarks>
    Disabled = 2
}
