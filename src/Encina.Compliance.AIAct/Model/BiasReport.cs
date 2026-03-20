namespace Encina.Compliance.AIAct.Model;

/// <summary>
/// Aggregated bias assessment report for a dataset, summarising bias indicators
/// across all evaluated protected attributes.
/// </summary>
/// <remarks>
/// <para>
/// Article 10(2)(f) requires examination of training, validation, and testing data sets
/// in view of possible biases that are likely to affect the health and safety of persons,
/// have a negative impact on fundamental rights, or lead to discrimination.
/// </para>
/// <para>
/// The <see cref="OverallFairness"/> flag indicates whether all <see cref="Indicators"/>
/// are within acceptable thresholds. When <c>false</c>, the dataset requires bias mitigation
/// before it can be used for high-risk AI system training.
/// </para>
/// </remarks>
public sealed record BiasReport
{
    /// <summary>
    /// Identifier of the dataset that was evaluated for bias.
    /// </summary>
    /// <example>"training-set-2026-Q1"</example>
    public required string DatasetId { get; init; }

    /// <summary>
    /// List of protected attributes that were evaluated.
    /// </summary>
    /// <example>["gender", "ethnicity", "age_group", "disability"]</example>
    public required IReadOnlyList<string> ProtectedAttributes { get; init; }

    /// <summary>
    /// Individual bias indicators for each protected attribute evaluated.
    /// </summary>
    public IReadOnlyList<BiasIndicator> Indicators { get; init; } = [];

    /// <summary>
    /// Whether the dataset meets overall fairness criteria — <c>true</c> when no
    /// <see cref="BiasIndicator"/> exceeds its configured threshold.
    /// </summary>
    public required bool OverallFairness { get; init; }

    /// <summary>
    /// Timestamp when the bias evaluation was performed (UTC).
    /// </summary>
    public required DateTimeOffset EvaluatedAtUtc { get; init; }
}
