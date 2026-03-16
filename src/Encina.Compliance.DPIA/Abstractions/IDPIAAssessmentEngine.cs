using Encina.Compliance.DPIA.Model;

using LanguageExt;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Core engine for conducting Data Protection Impact Assessments.
/// </summary>
/// <remarks>
/// <para>
/// The assessment engine orchestrates the DPIA lifecycle: evaluating whether a DPIA is required,
/// performing risk analysis through <see cref="IRiskCriterion"/> evaluators, and coordinating
/// DPO consultation per GDPR Article 35(2).
/// </para>
/// <para>
/// Per GDPR Article 35(1), the controller must carry out an assessment "where a type of processing
/// [...] is likely to result in a high risk to the rights and freedoms of natural persons."
/// This engine automates that determination and guides the assessment process.
/// </para>
/// <para>
/// The engine is a pure risk-evaluation component with no persistence dependencies.
/// DPO consultation lifecycle is managed by <see cref="Abstractions.IDPIAService"/>.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Check if a request type requires a DPIA
/// var requiresDpia = await engine.RequiresDPIAAsync(typeof(ProcessBiometricDataCommand), ct);
///
/// // Perform a full assessment
/// var context = new DPIAContext
/// {
///     RequestType = typeof(ProcessBiometricDataCommand),
///     DataCategories = ["BiometricData"],
///     HighRiskTriggers = [HighRiskTriggers.BiometricData, HighRiskTriggers.LargeScaleProcessing]
/// };
/// var result = await engine.AssessAsync(context, ct);
/// </code>
/// </example>
public interface IDPIAAssessmentEngine
{
    /// <summary>
    /// Performs a full Data Protection Impact Assessment for the given context.
    /// </summary>
    /// <param name="context">
    /// The DPIA context containing the request type, data categories, high-risk triggers,
    /// and optional template to guide the assessment.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="DPIAResult"/> containing the overall risk level, identified risks,
    /// proposed mitigations, and whether prior consultation with the supervisory authority
    /// is required; or an <see cref="EncinaError"/> if the assessment could not be completed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The assessment process:
    /// </para>
    /// <list type="number">
    /// <item><description>Evaluates all registered <see cref="IRiskCriterion"/> against the context.</description></item>
    /// <item><description>Aggregates identified risks into an overall risk level.</description></item>
    /// <item><description>Determines if prior consultation with the supervisory authority is required (Article 36).</description></item>
    /// <item><description>Returns a comprehensive <see cref="DPIAResult"/>.</description></item>
    /// </list>
    /// <para>
    /// Per Article 35(7), the assessment must contain at minimum: a systematic description of
    /// the processing, an assessment of necessity and proportionality, an assessment of risks,
    /// and the measures envisaged to address those risks.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, DPIAResult>> AssessAsync(
        DPIAContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a DPIA is required for the specified request type.
    /// </summary>
    /// <param name="requestType">The CLR type of the request to evaluate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> if a DPIA is required for the request type;
    /// <see langword="false"/> if no DPIA is needed; or an <see cref="EncinaError"/> if
    /// the determination could not be made.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method checks both the <see cref="RequiresDPIAAttribute"/> decoration
    /// on the request type and any configured high-risk criteria to determine if a DPIA is needed.
    /// </para>
    /// <para>
    /// Per EDPB WP 248 rev.01, the presence of two or more high-risk criteria generally
    /// triggers the need for a DPIA. Implementations should follow this guidance.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, bool>> RequiresDPIAAsync(
        Type requestType,
        CancellationToken cancellationToken = default);

}
