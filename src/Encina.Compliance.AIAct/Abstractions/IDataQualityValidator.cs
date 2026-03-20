using Encina.Compliance.AIAct.Model;

using LanguageExt;

namespace Encina.Compliance.AIAct.Abstractions;

/// <summary>
/// Validates data quality and detects bias in training, validation, and testing
/// datasets as required by Article 10 of the EU AI Act.
/// </summary>
/// <remarks>
/// <para>
/// Article 10(2)-(5) establishes data governance requirements for high-risk AI systems.
/// Training, validation, and testing data sets must be subject to data governance and
/// management practices that address:
/// </para>
/// <list type="bullet">
/// <item>Relevance, representativeness, and completeness (Art. 10(3))</item>
/// <item>Freedom from errors (Art. 10(3))</item>
/// <item>Appropriate statistical properties (Art. 10(4))</item>
/// <item>Examination for possible biases (Art. 10(2)(f))</item>
/// <item>Identification of data gaps or shortcomings (Art. 10(2)(g))</item>
/// </list>
/// <para>
/// The default implementation (<c>DefaultDataQualityValidator</c>) provides threshold-based
/// checks. Users implementing ML-specific bias detection (e.g., via ML.NET, FairLearn) should
/// provide a custom implementation.
/// </para>
/// </remarks>
public interface IDataQualityValidator
{
    /// <summary>
    /// Validates the quality of a training, validation, or testing dataset against
    /// the data governance requirements of Article 10.
    /// </summary>
    /// <param name="datasetId">The identifier of the dataset to validate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="DataQualityReport"/> containing completeness, accuracy, and consistency
    /// scores, identified data gaps, bias indicators, and an overall compliance assessment;
    /// or an <see cref="EncinaError"/> if validation could not be performed.
    /// </returns>
    /// <remarks>
    /// The report's <see cref="DataQualityReport.MeetsAIActRequirements"/> flag indicates
    /// whether the dataset satisfies the configured quality thresholds. When <c>false</c>,
    /// the identified gaps should be addressed before using the dataset.
    /// </remarks>
    ValueTask<Either<EncinaError, DataQualityReport>> ValidateTrainingDataAsync(
        string datasetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects potential bias in a dataset for the specified protected attributes.
    /// </summary>
    /// <param name="datasetId">The identifier of the dataset to evaluate for bias.</param>
    /// <param name="protectedAttributes">
    /// The protected attributes to evaluate (e.g., "gender", "ethnicity", "age_group").
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="BiasReport"/> containing per-attribute <see cref="BiasIndicator"/> results
    /// and an overall fairness assessment; or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Article 10(2)(f) requires examination of training data for biases that may affect
    /// health, safety, fundamental rights, or lead to discrimination prohibited under Union law.
    /// The default implementation uses the four-fifths (80%) rule for disparate impact analysis.
    /// </remarks>
    ValueTask<Either<EncinaError, BiasReport>> DetectBiasAsync(
        string datasetId,
        IReadOnlyList<string> protectedAttributes,
        CancellationToken cancellationToken = default);
}
