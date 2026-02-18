namespace Encina.Compliance.GDPR;

/// <summary>
/// Specifies the enforcement level for GDPR compliance checks in the pipeline.
/// </summary>
public enum GDPREnforcementMode
{
    /// <summary>
    /// Non-compliant requests are blocked and an error is returned.
    /// </summary>
    Enforce = 0,

    /// <summary>
    /// Non-compliant requests log a warning but are allowed to proceed.
    /// </summary>
    WarnOnly = 1
}
