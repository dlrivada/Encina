namespace Encina.Compliance.Attestation.Attributes;

/// <summary>
/// Controls the behavior of <see cref="AttestDecisionAttribute"/> when attestation fails.
/// </summary>
public enum AttestationFailureMode
{
    /// <summary>
    /// Attestation failure blocks the request pipeline. The handler result is discarded
    /// and an error is returned to the caller. This is the default for compliance scenarios.
    /// </summary>
    Enforce,

    /// <summary>
    /// Attestation failure is logged as a warning but the request proceeds normally.
    /// Use in environments where attestation is best-effort rather than mandatory.
    /// </summary>
    LogOnly
}
