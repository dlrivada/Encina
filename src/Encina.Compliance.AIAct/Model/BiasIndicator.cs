namespace Encina.Compliance.AIAct.Model;

/// <summary>
/// Represents a statistical bias indicator for a specific protected attribute,
/// measuring potential discriminatory impact in training data or model outputs.
/// </summary>
/// <remarks>
/// <para>
/// Article 10(2)(f) requires that training, validation, and testing data sets be examined
/// in view of possible biases that are likely to affect the health and safety of persons,
/// have a negative impact on fundamental rights, or lead to discrimination prohibited
/// under Union law.
/// </para>
/// <para>
/// The <see cref="DisparateImpactRatio"/> follows the four-fifths (80%) rule commonly
/// used in anti-discrimination analysis: a ratio below 0.8 indicates potential adverse impact.
/// Users should implement their own detection algorithms (e.g., via ML.NET, FairLearn)
/// and populate these indicators accordingly.
/// </para>
/// </remarks>
public sealed record BiasIndicator
{
    /// <summary>
    /// The protected attribute being evaluated for bias (e.g., "gender", "ethnicity", "age_group").
    /// </summary>
    /// <example>"gender"</example>
    public required string ProtectedAttribute { get; init; }

    /// <summary>
    /// The disparate impact ratio for this attribute.
    /// </summary>
    /// <remarks>
    /// Calculated as the selection rate of the disadvantaged group divided by the selection
    /// rate of the advantaged group. A value below 0.8 (the four-fifths rule) typically
    /// indicates potential adverse impact that requires investigation.
    /// </remarks>
    public required double DisparateImpactRatio { get; init; }

    /// <summary>
    /// The confidence interval for the disparate impact measurement, expressed as a
    /// margin of error (e.g., 0.05 means ±5%).
    /// </summary>
    /// <remarks>
    /// A narrower confidence interval indicates higher statistical reliability of the
    /// bias measurement. Essential for assessing whether the observed bias is statistically
    /// significant or within the bounds of sampling variation.
    /// </remarks>
    public required double ConfidenceInterval { get; init; }

    /// <summary>
    /// Whether the bias indicator exceeds the configured threshold for the protected attribute.
    /// </summary>
    /// <remarks>
    /// Typically <c>true</c> when <see cref="DisparateImpactRatio"/> is below 0.8,
    /// but the exact threshold is configurable per attribute via <c>AIActOptions</c>.
    /// </remarks>
    public required bool ExceedsThreshold { get; init; }

    /// <summary>
    /// Number of samples used to calculate the bias indicator, if available.
    /// </summary>
    /// <remarks>
    /// A larger sample size increases the statistical reliability of the measurement.
    /// <c>null</c> when sample size tracking is not available.
    /// </remarks>
    public int? SampleSize { get; init; }

    /// <summary>
    /// Description of the methodology used to calculate the bias indicator.
    /// </summary>
    /// <remarks>
    /// Examples: "four-fifths rule", "chi-squared test", "Fisher exact test",
    /// "Kolmogorov-Smirnov test". <c>null</c> when methodology is not recorded.
    /// </remarks>
    public string? Methodology { get; init; }
}
