using Encina.Compliance.DPIA.Model;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Evaluates a specific risk criterion against a DPIA context.
/// </summary>
/// <remarks>
/// <para>
/// Risk criteria are the individual assessment units used by <see cref="IDPIAAssessmentEngine"/>
/// to identify risks in a processing operation. Each criterion evaluates a specific aspect
/// of the processing (e.g., data sensitivity, scale, automation level) and returns a
/// <see cref="RiskItem"/> if the criterion is triggered.
/// </para>
/// <para>
/// The EDPB (WP 248 rev.01) identifies nine criteria for assessing high-risk processing:
/// </para>
/// <list type="number">
/// <item><description>Evaluation or scoring (including profiling and predicting).</description></item>
/// <item><description>Automated decision-making with legal or similar significant effect.</description></item>
/// <item><description>Systematic monitoring.</description></item>
/// <item><description>Sensitive data or data of a highly personal nature.</description></item>
/// <item><description>Data processed on a large scale.</description></item>
/// <item><description>Matching or combining datasets.</description></item>
/// <item><description>Data concerning vulnerable data subjects.</description></item>
/// <item><description>Innovative use or applying new technological or organizational solutions.</description></item>
/// <item><description>Processing that prevents data subjects from exercising a right or using a service or contract.</description></item>
/// </list>
/// <para>
/// Implementations should target one or more of these criteria. Custom criteria can also be
/// created for organization-specific risk factors.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class SensitiveDataCriterion : IRiskCriterion
/// {
///     public string Name => "SensitiveDataProcessing";
///
///     public ValueTask&lt;RiskItem?&gt; EvaluateAsync(DPIAContext context, CancellationToken ct)
///     {
///         var hasSensitiveData = context.HighRiskTriggers
///             .Any(t => t is HighRiskTriggers.HealthData or HighRiskTriggers.BiometricData);
///
///         if (!hasSensitiveData)
///             return ValueTask.FromResult&lt;RiskItem?&gt;(null);
///
///         return ValueTask.FromResult&lt;RiskItem?&gt;(new RiskItem(
///             Category: "Sensitive Data",
///             Level: RiskLevel.High,
///             Description: "Processing involves special category data under Article 9(1).",
///             MitigationSuggestion: "Implement pseudonymization and encryption at rest."));
///     }
/// }
/// </code>
/// </example>
public interface IRiskCriterion
{
    /// <summary>
    /// Gets the unique name of this risk criterion.
    /// </summary>
    /// <remarks>
    /// The name is used for identification in audit trails, logging, and assessment results.
    /// It should be descriptive and unique among all registered criteria
    /// (e.g., "SensitiveDataProcessing", "LargeScaleEvaluation").
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Evaluates this risk criterion against the provided DPIA context.
    /// </summary>
    /// <param name="context">
    /// The DPIA context containing request type, data categories, high-risk triggers,
    /// and additional metadata for evaluation.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="RiskItem"/> describing the identified risk if this criterion is triggered;
    /// <see langword="null"/> if the criterion is not applicable to the given context.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Returning <see langword="null"/> indicates that this criterion does not apply to the
    /// processing described by the context. This is not an error condition — it simply means
    /// this particular risk factor is not present.
    /// </para>
    /// <para>
    /// Implementations should be deterministic: given the same context, the same result
    /// should be returned. Side effects (e.g., logging) are acceptable but should not
    /// affect the evaluation outcome.
    /// </para>
    /// </remarks>
    ValueTask<RiskItem?> EvaluateAsync(
        DPIAContext context,
        CancellationToken cancellationToken = default);
}
