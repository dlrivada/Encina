namespace Encina.Compliance.PrivacyByDesign.Model;

/// <summary>
/// The result of a data minimization analysis for a specific request type.
/// </summary>
/// <remarks>
/// <para>
/// Produced by <c>IDataMinimizationAnalyzer</c>, this report provides a comprehensive
/// breakdown of which fields in a request are necessary and which are not, along with
/// a minimization score and actionable recommendations.
/// </para>
/// <para>
/// Per GDPR Article 25(2), "the controller shall implement appropriate technical and
/// organisational measures for ensuring that, by default, only personal data which are
/// necessary for each specific purpose of the processing are processed." This report
/// supports compliance by making the necessity assessment explicit and auditable.
/// </para>
/// </remarks>
public sealed record MinimizationReport
{
    /// <summary>
    /// The name of the request type that was analyzed.
    /// </summary>
    public required string RequestTypeName { get; init; }

    /// <summary>
    /// The fields that are considered necessary for the processing operation.
    /// </summary>
    /// <remarks>
    /// Fields without a <c>[NotStrictlyNecessary]</c> attribute are classified as necessary.
    /// </remarks>
    public required IReadOnlyList<PrivacyFieldInfo> NecessaryFields { get; init; }

    /// <summary>
    /// The fields that are marked as not strictly necessary via <c>[NotStrictlyNecessary]</c>.
    /// </summary>
    /// <remarks>
    /// Each entry includes whether the field has a non-default value in the current request,
    /// the reason for being unnecessary, and the severity level.
    /// </remarks>
    public required IReadOnlyList<UnnecessaryFieldInfo> UnnecessaryFields { get; init; }

    /// <summary>
    /// A score between 0.0 and 1.0 indicating the degree of data minimization compliance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Calculated as the ratio of necessary fields to total fields:
    /// <c>NecessaryFields.Count / (NecessaryFields.Count + UnnecessaryFields.Count)</c>.
    /// </para>
    /// <para>
    /// A score of 1.0 means all fields are necessary (fully minimized).
    /// A score of 0.0 means no fields are necessary (should never occur in practice).
    /// The configurable threshold in <c>PrivacyByDesignOptions.MinimizationScoreThreshold</c>
    /// determines when violations are flagged.
    /// </para>
    /// </remarks>
    public required double MinimizationScore { get; init; }

    /// <summary>
    /// Actionable recommendations for improving data minimization compliance.
    /// </summary>
    /// <remarks>
    /// Generated based on the analysis of unnecessary fields that have non-default values.
    /// Each recommendation suggests removing or making optional a specific field.
    /// </remarks>
    public required IReadOnlyList<string> Recommendations { get; init; }

    /// <summary>
    /// The UTC timestamp when this analysis was performed.
    /// </summary>
    public required DateTimeOffset AnalyzedAtUtc { get; init; }
}
