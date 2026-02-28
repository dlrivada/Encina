namespace Encina.Compliance.Anonymization.Model;

/// <summary>
/// Result of a re-identification risk assessment on an anonymized dataset.
/// </summary>
/// <remarks>
/// <para>
/// Risk assessments evaluate the effectiveness of anonymization measures by calculating
/// privacy metrics. These metrics help determine whether the anonymized data provides
/// sufficient protection against re-identification attacks.
/// </para>
/// <para>
/// Three complementary metrics are provided:
/// <list type="bullet">
/// <item>
/// <term>K-Anonymity</term>
/// <description>Each record is indistinguishable from at least <c>k-1</c> other records
/// based on quasi-identifiers. Higher <c>k</c> = stronger privacy.</description>
/// </item>
/// <item>
/// <term>L-Diversity</term>
/// <description>Each equivalence class contains at least <c>l</c> distinct sensitive values,
/// preventing homogeneity attacks. Higher <c>l</c> = stronger privacy.</description>
/// </item>
/// <item>
/// <term>T-Closeness</term>
/// <description>The distribution of sensitive attributes within each equivalence class is
/// within distance <c>t</c> of the global distribution (Earth Mover's Distance).
/// Lower <c>t</c> = stronger privacy.</description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Per GDPR Article 89, controllers must implement "appropriate safeguards" for research
/// and statistics. This risk assessment provides evidence of those safeguards.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await riskAssessor.AssessAsync(dataset, ["age", "zipcode", "gender"], ct);
///
/// if (!result.IsRight)
///     // Handle assessment error
///
/// var assessment = result.Match(r => r, _ => throw new InvalidOperationException());
/// Console.WriteLine($"K-Anonymity: {assessment.KAnonymityValue}");
/// Console.WriteLine($"Re-identification probability: {assessment.ReIdentificationProbability:P2}");
/// Console.WriteLine($"Acceptable: {assessment.IsAcceptable}");
/// </code>
/// </example>
public sealed record RiskAssessmentResult
{
    /// <summary>
    /// The achieved k-anonymity value — the minimum equivalence class size.
    /// </summary>
    /// <remarks>
    /// A value of <c>k</c> means every record is indistinguishable from at least <c>k-1</c>
    /// other records based on the quasi-identifiers. Typical targets: <c>k ≥ 5</c> for
    /// general use, <c>k ≥ 10</c> for sensitive data.
    /// </remarks>
    public required int KAnonymityValue { get; init; }

    /// <summary>
    /// The achieved l-diversity value — the minimum number of distinct sensitive values per class.
    /// </summary>
    /// <remarks>
    /// A value of <c>l</c> means each equivalence class contains at least <c>l</c> distinct
    /// values for the sensitive attribute. Prevents homogeneity attacks where all records
    /// in a class share the same sensitive value. Typical target: <c>l ≥ 3</c>.
    /// </remarks>
    public required int LDiversityValue { get; init; }

    /// <summary>
    /// The achieved t-closeness distance — the maximum Earth Mover's Distance
    /// between any equivalence class distribution and the global distribution.
    /// </summary>
    /// <remarks>
    /// A lower value indicates that each equivalence class has a distribution of sensitive
    /// values similar to the overall dataset, preventing skewness attacks.
    /// Typical target: <c>t ≤ 0.15</c>. A value of <c>0.0</c> indicates perfect closeness.
    /// </remarks>
    public required double TClosenessDistance { get; init; }

    /// <summary>
    /// Estimated probability of successfully re-identifying an individual in the dataset.
    /// </summary>
    /// <remarks>
    /// Calculated as <c>1.0 / k</c> where <c>k</c> is the minimum equivalence class size.
    /// Values closer to <c>0.0</c> indicate stronger privacy protection.
    /// GDPR requires that re-identification is "not reasonably likely" (Recital 26).
    /// </remarks>
    public required double ReIdentificationProbability { get; init; }

    /// <summary>
    /// Whether the assessment results meet the configured acceptability thresholds.
    /// </summary>
    /// <remarks>
    /// Determined by comparing <see cref="KAnonymityValue"/>, <see cref="LDiversityValue"/>,
    /// and <see cref="TClosenessDistance"/> against the configured thresholds in
    /// <c>AnonymizationOptions</c>. If any metric fails its threshold, this is <c>false</c>.
    /// </remarks>
    public required bool IsAcceptable { get; init; }

    /// <summary>
    /// Timestamp when the risk assessment was performed (UTC).
    /// </summary>
    public required DateTimeOffset AssessedAtUtc { get; init; }

    /// <summary>
    /// Actionable recommendations for improving anonymization quality.
    /// </summary>
    /// <remarks>
    /// May include suggestions such as "Increase generalization granularity for field 'Age'
    /// to achieve target k=5" or "Apply l-diversity to sensitive attribute 'Diagnosis'".
    /// Empty when <see cref="IsAcceptable"/> is <c>true</c>.
    /// </remarks>
    public required IReadOnlyList<string> Recommendations { get; init; }
}
