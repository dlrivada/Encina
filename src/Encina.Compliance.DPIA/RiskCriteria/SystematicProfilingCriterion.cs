using Encina.Compliance.DPIA.Model;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Risk criterion for systematic profiling per GDPR Article 35(3)(a).
/// </summary>
/// <remarks>
/// <para>
/// Evaluates whether the processing involves systematic and extensive evaluation of personal
/// aspects based on automated processing, including profiling, on which decisions are based
/// that produce legal effects or similarly significantly affect individuals.
/// </para>
/// <para>
/// Risk levels assigned:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="RiskLevel.High"/>: Systematic profiling detected.</description></item>
/// <item><description><see cref="RiskLevel.VeryHigh"/>: Systematic profiling combined with automated decision-making.</description></item>
/// </list>
/// </remarks>
public sealed class SystematicProfilingCriterion : IRiskCriterion
{
    /// <inheritdoc />
    public string Name => "Systematic Profiling (Art. 35(3)(a))";

    /// <inheritdoc />
    public ValueTask<RiskItem?> EvaluateAsync(
        DPIAContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.HighRiskTriggers.Contains(HighRiskTriggers.SystematicProfiling))
        {
            return ValueTask.FromResult<RiskItem?>(null);
        }

        var hasAutomatedDecisionMaking = context.HighRiskTriggers.Contains(
            HighRiskTriggers.AutomatedDecisionMaking);

        var level = hasAutomatedDecisionMaking ? RiskLevel.VeryHigh : RiskLevel.High;

        var description = hasAutomatedDecisionMaking
            ? "Systematic profiling combined with automated decision-making produces legal effects or similarly significant impacts on individuals."
            : "Systematic and extensive evaluation of personal aspects based on automated processing, including profiling.";

        return ValueTask.FromResult<RiskItem?>(new RiskItem(
            Category: "Systematic Profiling",
            Level: level,
            Description: description,
            MitigationSuggestion: "Implement human oversight mechanisms and meaningful information about the logic involved."));
    }
}
