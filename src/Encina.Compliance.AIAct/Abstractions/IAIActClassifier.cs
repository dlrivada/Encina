using Encina.Compliance.AIAct.Model;

using LanguageExt;

namespace Encina.Compliance.AIAct.Abstractions;

/// <summary>
/// Classifies AI systems according to the EU AI Act risk framework and evaluates
/// their compliance with applicable requirements.
/// </summary>
/// <remarks>
/// <para>
/// Article 6 establishes the criteria for identifying high-risk AI systems. The classifier
/// uses the <see cref="IAISystemRegistry"/> to retrieve system metadata and applies
/// the risk classification rules from Annex III and Article 5 (prohibited practices).
/// </para>
/// <para>
/// The classifier is used by the <c>AIActCompliancePipelineBehavior</c> to determine
/// the risk level of the AI system associated with a request, which in turn drives
/// enforcement decisions (blocking, oversight, transparency).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var riskLevel = await classifier.ClassifySystemAsync("cv-screener-v2", ct);
/// riskLevel.Match(
///     Right: level => logger.LogInformation("System classified as {Level}", level),
///     Left: error => logger.LogError("Classification failed: {Error}", error));
/// </code>
/// </example>
public interface IAIActClassifier
{
    /// <summary>
    /// Determines the risk level of a registered AI system.
    /// </summary>
    /// <param name="systemId">The unique identifier of the AI system to classify.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The <see cref="AIRiskLevel"/> for the system, or an <see cref="EncinaError"/>
    /// if the system is not registered or classification fails.
    /// </returns>
    /// <remarks>
    /// Classification considers the system's <see cref="AISystemCategory"/> (Annex III),
    /// any applicable <see cref="ProhibitedPractice"/> (Art. 5), and the deployment context.
    /// </remarks>
    ValueTask<Either<EncinaError, AIRiskLevel>> ClassifySystemAsync(
        string systemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a registered AI system involves a prohibited practice under Article 5.
    /// </summary>
    /// <param name="systemId">The unique identifier of the AI system to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if the system involves a prohibited practice, <c>false</c> otherwise,
    /// or an <see cref="EncinaError"/> if the system is not registered.
    /// </returns>
    /// <remarks>
    /// Violations of Art. 5 are subject to administrative fines of up to EUR 35 million
    /// or 7% of total worldwide annual turnover (Art. 99(3)).
    /// </remarks>
    ValueTask<Either<EncinaError, bool>> IsProhibitedAsync(
        string systemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a full compliance evaluation of a registered AI system against all
    /// applicable AI Act requirements.
    /// </summary>
    /// <param name="systemId">The unique identifier of the AI system to evaluate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="AIActComplianceResult"/> summarising risk level, applicable obligations,
    /// and any identified violations; or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// The evaluation checks prohibited use (Art. 5), risk classification (Art. 6),
    /// human oversight requirements (Art. 14), and transparency obligations (Art. 13/50).
    /// </remarks>
    ValueTask<Either<EncinaError, AIActComplianceResult>> EvaluateComplianceAsync(
        string systemId,
        CancellationToken cancellationToken = default);
}
