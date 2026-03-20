namespace Encina.Compliance.AIAct.Model;

/// <summary>
/// Captures the results of a data quality assessment for a training, validation, or testing dataset,
/// as required by Article 10 of the EU AI Act.
/// </summary>
/// <remarks>
/// <para>
/// Article 10(2)-(5) establishes data governance requirements for high-risk AI systems, including
/// completeness, accuracy, representativeness, and freedom from errors. This report aggregates
/// quality scores, identified data gaps, and bias indicators into a single assessment result.
/// </para>
/// <para>
/// The <see cref="MeetsAIActRequirements"/> flag summarises whether the dataset satisfies
/// the minimum thresholds configured in <c>AIActOptions</c>. When <c>false</c>, the dataset
/// should not be used for training, validation, or testing until the identified gaps are addressed.
/// </para>
/// </remarks>
public sealed record DataQualityReport
{
    /// <summary>
    /// Identifier of the dataset that was evaluated.
    /// </summary>
    /// <example>"training-set-2026-Q1"</example>
    public required string DatasetId { get; init; }

    /// <summary>
    /// Completeness score as a value between 0.0 and 1.0.
    /// </summary>
    /// <remarks>
    /// Measures the proportion of required data fields that are present and non-null
    /// across the dataset. Art. 10(3) requires data to be relevant, sufficiently representative,
    /// and to the best extent possible, free of errors and complete.
    /// </remarks>
    public required double CompletenessScore { get; init; }

    /// <summary>
    /// Accuracy score as a value between 0.0 and 1.0.
    /// </summary>
    /// <remarks>
    /// Measures the degree to which data values correctly represent the real-world entities
    /// or events they describe. Art. 10(3) requires data to be free of errors to the best
    /// extent possible.
    /// </remarks>
    public required double AccuracyScore { get; init; }

    /// <summary>
    /// Consistency score as a value between 0.0 and 1.0.
    /// </summary>
    /// <remarks>
    /// Measures internal consistency of the dataset — the absence of contradictory or
    /// conflicting data points across different fields and records.
    /// </remarks>
    public required double ConsistencyScore { get; init; }

    /// <summary>
    /// Data gaps and shortcomings identified during the assessment, as required by Art. 10(2)(g).
    /// </summary>
    public IReadOnlyList<DataGap> IdentifiedGaps { get; init; } = [];

    /// <summary>
    /// Bias indicators for protected attributes, as required by Art. 10(2)(f).
    /// </summary>
    public IReadOnlyList<BiasIndicator> BiasIndicators { get; init; } = [];

    /// <summary>
    /// Whether the dataset meets the AI Act data governance requirements based on
    /// the configured quality thresholds.
    /// </summary>
    public required bool MeetsAIActRequirements { get; init; }

    /// <summary>
    /// Timestamp when the data quality evaluation was performed (UTC).
    /// </summary>
    public required DateTimeOffset EvaluatedAtUtc { get; init; }
}
