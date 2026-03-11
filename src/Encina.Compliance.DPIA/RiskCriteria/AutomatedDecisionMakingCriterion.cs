using Encina.Compliance.DPIA.Model;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Risk criterion for automated individual decision-making per GDPR Article 22.
/// </summary>
/// <remarks>
/// <para>
/// Evaluates whether the processing involves automated decision-making, including profiling,
/// which produces legal effects or similarly significant effects on data subjects.
/// </para>
/// <para>
/// Risk levels assigned:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="RiskLevel.High"/>: Automated decision-making detected.</description></item>
/// <item><description><see cref="RiskLevel.VeryHigh"/>: Automated decision-making combined with systematic profiling (legal effects more likely).</description></item>
/// </list>
/// </remarks>
public sealed class AutomatedDecisionMakingCriterion : IRiskCriterion
{
    /// <inheritdoc />
    public string Name => "Automated Decision-Making (Art. 22)";

    /// <inheritdoc />
    public ValueTask<RiskItem?> EvaluateAsync(
        DPIAContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.HighRiskTriggers.Contains(HighRiskTriggers.AutomatedDecisionMaking))
        {
            return ValueTask.FromResult<RiskItem?>(null);
        }

        var hasProfiling = context.HighRiskTriggers.Contains(HighRiskTriggers.SystematicProfiling);
        var level = hasProfiling ? RiskLevel.VeryHigh : RiskLevel.High;

        var description = hasProfiling
            ? "Automated decision-making combined with systematic profiling that produces legal effects or similarly significantly affects data subjects."
            : "Processing involves automated decision-making that may significantly affect data subjects.";

        return ValueTask.FromResult<RiskItem?>(new RiskItem(
            Category: "Automated Decision-Making",
            Level: level,
            Description: description,
            MitigationSuggestion: "Provide mechanisms for human review of automated decisions and data subject rights under Article 22."));
    }
}
