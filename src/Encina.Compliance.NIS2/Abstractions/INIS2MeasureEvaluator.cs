using Encina.Compliance.NIS2.Model;

using LanguageExt;

namespace Encina.Compliance.NIS2.Abstractions;

/// <summary>
/// Evaluates a single NIS2 cybersecurity risk-management measure (Art. 21(2)).
/// </summary>
/// <remarks>
/// <para>
/// Each implementation evaluates one of the 10 mandatory measures defined in Article 21(2)(a)–(j).
/// Evaluators are registered as <c>IEnumerable&lt;INIS2MeasureEvaluator&gt;</c> in the DI container
/// and aggregated by <see cref="INIS2ComplianceValidator"/>.
/// </para>
/// <para>
/// Default evaluators check the <c>NIS2Options</c> configuration and registered services
/// to determine whether each measure is satisfied. Applications can replace or extend
/// evaluators via DI for custom compliance logic tailored to their specific environment.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class CustomRiskAnalysisEvaluator : INIS2MeasureEvaluator
/// {
///     public NIS2Measure Measure => NIS2Measure.RiskAnalysisAndSecurityPolicies;
///
///     public ValueTask&lt;Either&lt;EncinaError, NIS2MeasureResult&gt;&gt; EvaluateAsync(
///         NIS2MeasureContext context,
///         CancellationToken cancellationToken = default)
///     {
///         // Custom evaluation logic against internal risk registry...
///         var result = NIS2MeasureResult.Satisfied(Measure, "Risk analysis policy is current.");
///         return ValueTask.FromResult(Right&lt;EncinaError, NIS2MeasureResult&gt;(result));
///     }
/// }
/// </code>
/// </example>
public interface INIS2MeasureEvaluator
{
    /// <summary>
    /// Gets the specific measure that this evaluator assesses.
    /// </summary>
    NIS2Measure Measure { get; }

    /// <summary>
    /// Evaluates whether the measure is satisfied based on the current configuration and runtime state.
    /// </summary>
    /// <param name="context">
    /// The evaluation context providing access to <c>NIS2Options</c>, <c>TimeProvider</c>,
    /// and <c>IServiceProvider</c> for resolving runtime dependencies.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="NIS2MeasureResult"/> indicating whether the measure is satisfied,
    /// with details and recommendations; or an <see cref="EncinaError"/> if evaluation failed.
    /// </returns>
    ValueTask<Either<EncinaError, NIS2MeasureResult>> EvaluateAsync(
        NIS2MeasureContext context,
        CancellationToken cancellationToken = default);
}
