using Encina.Compliance.AIAct.Model;

using LanguageExt;

namespace Encina.Compliance.AIAct.Abstractions;

/// <summary>
/// Main orchestration interface for AI Act compliance validation, invoked by the
/// <c>AIActCompliancePipelineBehavior</c> to evaluate requests against applicable
/// AI Act requirements.
/// </summary>
/// <remarks>
/// <para>
/// This is the primary entry point for compliance checks in the pipeline. It coordinates
/// across the <see cref="IAIActClassifier"/>, <see cref="IHumanOversightEnforcer"/>, and
/// transparency obligation checks to produce a comprehensive <see cref="AIActComplianceResult"/>.
/// </para>
/// <para>
/// The validation flow for each request:
/// </para>
/// <list type="number">
/// <item>Classify the AI system's risk level (Art. 6, Annex III)</item>
/// <item>Check for prohibited practices (Art. 5)</item>
/// <item>Verify human oversight for high-risk systems (Art. 14)</item>
/// <item>Assess transparency obligations (Art. 13, Art. 50)</item>
/// </list>
/// <para>
/// The <c>systemId</c> parameter is optional — when <c>null</c>, the validator attempts
/// to resolve the system ID from request attributes (<c>[HighRiskAI]</c>,
/// <c>[RequireHumanOversight]</c>, <c>[AITransparency]</c>).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In pipeline behavior
/// var result = await validator.ValidateAsync(request, systemId: "cv-screener-v2", ct);
/// result.Match(
///     Right: compliance =>
///     {
///         if (compliance.IsProhibited)
///             return Left(AIActErrors.ProhibitedUse(compliance.SystemId));
///         // continue pipeline...
///     },
///     Left: error => Left(error));
/// </code>
/// </example>
public interface IAIActComplianceValidator
{
    /// <summary>
    /// Validates whether the specified request complies with the applicable AI Act requirements.
    /// </summary>
    /// <typeparam name="TRequest">The type of request being validated.</typeparam>
    /// <param name="request">The request to validate for AI Act compliance.</param>
    /// <param name="systemId">
    /// The AI system identifier associated with this request. When <c>null</c>, the validator
    /// attempts to resolve the system ID from the request type's attributes.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="AIActComplianceResult"/> summarising the risk level, applicable obligations,
    /// and any identified violations; or an <see cref="EncinaError"/> if validation could not
    /// be performed.
    /// </returns>
    /// <remarks>
    /// The result's <see cref="AIActComplianceResult.Violations"/> list is empty when the request
    /// is fully compliant. The pipeline behavior uses the enforcement mode
    /// (<see cref="AIActEnforcementMode"/>) to determine the action: block, warn, or skip.
    /// </remarks>
    ValueTask<Either<EncinaError, AIActComplianceResult>> ValidateAsync<TRequest>(
        TRequest request,
        string? systemId,
        CancellationToken cancellationToken = default);
}
