namespace Encina.Compliance.PrivacyByDesign.Model;

/// <summary>
/// Controls how the Privacy by Design pipeline behavior enforces data minimization,
/// purpose limitation, and default privacy requirements.
/// </summary>
/// <remarks>
/// <para>
/// Configures the runtime enforcement strategy for the <c>DataMinimizationPipelineBehavior</c>.
/// This allows gradual adoption: start with <see cref="Disabled"/> during development,
/// move to <see cref="Warn"/> for visibility, and enable <see cref="Block"/> in production
/// to enforce GDPR Article 25 compliance.
/// </para>
/// <para>
/// Per GDPR Article 25(1), the controller shall implement appropriate technical and
/// organisational measures "both at the time of the determination of the means for processing
/// and at the time of the processing itself."
/// </para>
/// </remarks>
public enum PrivacyByDesignEnforcementMode
{
    /// <summary>
    /// Privacy by Design enforcement is completely disabled; the pipeline behavior is a no-op.
    /// </summary>
    /// <remarks>
    /// Use during development or for subsystems where Privacy by Design enforcement
    /// is not applicable.
    /// </remarks>
    Disabled = 0,

    /// <summary>
    /// Log a warning but allow processing to continue when privacy violations are detected.
    /// </summary>
    /// <remarks>
    /// Useful during migration or adoption phases to identify operations that violate
    /// data minimization, purpose limitation, or default privacy without disrupting
    /// existing functionality.
    /// </remarks>
    Warn = 1,

    /// <summary>
    /// Block processing operations that violate data minimization, purpose limitation,
    /// or default privacy requirements.
    /// </summary>
    /// <remarks>
    /// The pipeline behavior returns an <c>EncinaError</c> (Railway Oriented Programming)
    /// when a privacy violation is detected. Recommended for production environments.
    /// </remarks>
    Block = 2
}
