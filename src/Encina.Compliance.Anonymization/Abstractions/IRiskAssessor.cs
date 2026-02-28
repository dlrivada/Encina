using Encina.Compliance.Anonymization.Model;

using LanguageExt;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Service for assessing the re-identification risk of anonymized datasets.
/// </summary>
/// <remarks>
/// <para>
/// Risk assessments evaluate the effectiveness of anonymization measures by calculating
/// privacy metrics. These metrics help determine whether the anonymized data provides
/// sufficient protection against re-identification attacks, as required by GDPR.
/// </para>
/// <para>
/// Three complementary metrics are computed:
/// <list type="bullet">
/// <item>
/// <term>K-Anonymity</term>
/// <description>Each record is indistinguishable from at least <c>k-1</c> other records
/// based on quasi-identifiers. Higher <c>k</c> = stronger privacy.
/// Typical targets: <c>k ≥ 5</c> for general use, <c>k ≥ 10</c> for sensitive data.</description>
/// </item>
/// <item>
/// <term>L-Diversity</term>
/// <description>Each equivalence class contains at least <c>l</c> distinct sensitive values,
/// preventing homogeneity attacks. Typical target: <c>l ≥ 3</c>.</description>
/// </item>
/// <item>
/// <term>T-Closeness</term>
/// <description>The distribution of sensitive attributes within each equivalence class is
/// within distance <c>t</c> of the global distribution (Earth Mover's Distance).
/// Typical target: <c>t ≤ 0.15</c>.</description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Per GDPR Article 89, controllers must implement "appropriate safeguards" for research
/// and statistics. Risk assessment provides evidence of those safeguards and enables
/// iterative improvement of anonymization profiles.
/// </para>
/// <para>
/// This is a utility service — it is used on-demand for compliance verification,
/// not as a pipeline behavior executed per-request.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var dataset = await repository.GetAnonymizedCustomersAsync(cancellationToken);
/// var quasiIdentifiers = new[] { "Age", "ZipCode", "Gender" };
///
/// var result = await riskAssessor.AssessAsync(dataset, quasiIdentifiers, cancellationToken);
///
/// result.Match(
///     Right: assessment =>
///     {
///         Console.WriteLine($"K-Anonymity: {assessment.KAnonymityValue}");
///         Console.WriteLine($"Re-identification probability: {assessment.ReIdentificationProbability:P2}");
///         Console.WriteLine($"Acceptable: {assessment.IsAcceptable}");
///
///         foreach (var recommendation in assessment.Recommendations)
///             Console.WriteLine($"  - {recommendation}");
///     },
///     Left: error => Console.WriteLine($"Assessment failed: {error.Message}"));
/// </code>
/// </example>
public interface IRiskAssessor
{
    /// <summary>
    /// Assesses the re-identification risk of an anonymized dataset.
    /// </summary>
    /// <typeparam name="T">The type of records in the dataset.</typeparam>
    /// <param name="dataset">The anonymized dataset to assess.</param>
    /// <param name="quasiIdentifiers">
    /// The names of fields that serve as quasi-identifiers (attributes that, in combination,
    /// could potentially identify an individual — e.g., age, zip code, gender).
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="RiskAssessmentResult"/> containing privacy metrics and recommendations,
    /// or an <see cref="EncinaError"/> if the assessment could not be performed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The assessment requires at least 2 records in the dataset. For meaningful results,
    /// datasets should contain a representative sample of the data (typically 1,000+ records).
    /// </para>
    /// <para>
    /// Quasi-identifiers are fields that are not directly identifying on their own but can
    /// be used in combination with external data to re-identify individuals. Common
    /// quasi-identifiers include age, gender, zip code, date of birth, and occupation.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, RiskAssessmentResult>> AssessAsync<T>(
        IReadOnlyList<T> dataset,
        IReadOnlyList<string> quasiIdentifiers,
        CancellationToken cancellationToken = default);
}
