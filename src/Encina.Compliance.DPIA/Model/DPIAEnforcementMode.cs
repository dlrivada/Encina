namespace Encina.Compliance.DPIA.Model;

/// <summary>
/// Controls how the DPIA pipeline behavior enforces assessment requirements.
/// </summary>
/// <remarks>
/// <para>
/// Configures the runtime enforcement strategy for the <c>DPIAPipelineBehavior</c>.
/// This allows gradual adoption: start with <see cref="Disabled"/> during development,
/// move to <see cref="Warn"/> for visibility, and enable <see cref="Block"/> in production
/// to enforce GDPR Article 35 compliance.
/// </para>
/// </remarks>
public enum DPIAEnforcementMode
{
    /// <summary>
    /// Block processing operations that lack a current, approved DPIA assessment.
    /// </summary>
    /// <remarks>
    /// The pipeline behavior returns an <c>EncinaError</c> (Railway Oriented Programming)
    /// when no valid assessment exists. Recommended for production environments.
    /// </remarks>
    Block = 0,

    /// <summary>
    /// Log a warning but allow processing to continue when no valid assessment exists.
    /// </summary>
    /// <remarks>
    /// Useful during migration or adoption phases to identify operations that need assessments
    /// without disrupting existing functionality.
    /// </remarks>
    Warn = 1,

    /// <summary>
    /// DPIA enforcement is completely disabled; the pipeline behavior is a no-op.
    /// </summary>
    /// <remarks>
    /// Use during development or for subsystems where DPIA is not applicable.
    /// </remarks>
    Disabled = 2
}
