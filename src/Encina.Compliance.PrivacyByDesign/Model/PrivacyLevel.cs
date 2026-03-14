namespace Encina.Compliance.PrivacyByDesign.Model;

/// <summary>
/// Defines the default privacy posture for request processing.
/// </summary>
/// <remarks>
/// <para>
/// Controls how aggressively the Privacy by Design validator enforces data minimization
/// and default privacy checks. Higher levels impose stricter validation.
/// </para>
/// <para>
/// Per GDPR Article 25(2) and Recital 78, appropriate measures should ensure that
/// "by default, personal data are not made accessible without the individual's intervention."
/// </para>
/// </remarks>
public enum PrivacyLevel
{
    /// <summary>
    /// Minimum privacy enforcement: only fields explicitly marked with
    /// <c>[NotStrictlyNecessary]</c> are flagged.
    /// </summary>
    Minimum = 0,

    /// <summary>
    /// Standard privacy enforcement: fields marked with <c>[NotStrictlyNecessary]</c>
    /// are flagged, and purpose limitation is checked for fields with <c>[PurposeLimitation]</c>.
    /// </summary>
    Standard = 1,

    /// <summary>
    /// Maximum privacy enforcement: all minimization, purpose limitation, and default
    /// privacy checks are applied. Recommended for production environments processing
    /// personal data.
    /// </summary>
    Maximum = 2
}
