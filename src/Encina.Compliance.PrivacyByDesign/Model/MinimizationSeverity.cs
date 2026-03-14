namespace Encina.Compliance.PrivacyByDesign.Model;

/// <summary>
/// Indicates the severity of a data minimization finding for a field marked
/// with <c>[NotStrictlyNecessary]</c>.
/// </summary>
/// <remarks>
/// <para>
/// Used by the <c>DataMinimizationPipelineBehavior</c> to determine whether a non-necessary
/// field with a value constitutes an informational note, a warning, or a blocking violation.
/// </para>
/// <para>
/// Per GDPR Article 25(2), only personal data which are necessary for each specific purpose
/// of the processing should be processed by default. The severity allows teams to gradually
/// tighten enforcement.
/// </para>
/// </remarks>
public enum MinimizationSeverity
{
    /// <summary>
    /// Informational: the field is not strictly necessary but its presence is noted
    /// without triggering a warning or violation.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning: the field is not strictly necessary and its presence is logged as a warning.
    /// In <see cref="PrivacyByDesignEnforcementMode.Block"/> mode, this still blocks
    /// unless the minimization score threshold is met.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Violation: the field is not strictly necessary and its presence is treated as a
    /// hard violation regardless of the minimization score threshold.
    /// </summary>
    Violation = 2
}
