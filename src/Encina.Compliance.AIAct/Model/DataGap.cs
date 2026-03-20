namespace Encina.Compliance.AIAct.Model;

/// <summary>
/// Represents a specific data gap or shortcoming identified during training data quality assessment.
/// </summary>
/// <remarks>
/// <para>
/// Article 10(2)(g) requires providers of high-risk AI systems to identify relevant data gaps
/// or shortcomings that prevent compliance with the data governance requirements, and to
/// document how those gaps and shortcomings can be addressed.
/// </para>
/// <para>
/// Each <see cref="DataGap"/> is part of a <see cref="DataQualityReport"/> and contributes
/// to the overall assessment of whether a dataset meets the AI Act requirements for
/// training, validation, and testing.
/// </para>
/// </remarks>
public sealed record DataGap
{
    /// <summary>
    /// Category of the data gap (e.g., "demographic coverage", "temporal representation", "geographic scope").
    /// </summary>
    /// <example>"demographic coverage"</example>
    public required string Category { get; init; }

    /// <summary>
    /// Detailed description of the data gap and its potential impact on model quality.
    /// </summary>
    /// <example>"Training data under-represents applicants over 55, comprising only 2% of samples vs. 18% of the target population."</example>
    public required string Description { get; init; }

    /// <summary>
    /// Assessed severity of this data gap.
    /// </summary>
    public required DataGapSeverity Severity { get; init; }

    /// <summary>
    /// Approximate number of records affected by this gap, if quantifiable.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when the gap cannot be expressed as a record count
    /// (e.g., a missing demographic category entirely absent from the dataset).
    /// </remarks>
    public int? AffectedRecords { get; init; }
}
