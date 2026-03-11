using Encina.Compliance.DPIA.Model;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Risk criterion for large-scale processing of personal data.
/// </summary>
/// <remarks>
/// <para>
/// Evaluates whether the processing involves personal data on a large scale, considering
/// factors such as the number of data subjects, volume of data, duration of processing,
/// and geographical extent as outlined by the EDPB in WP 248 rev.01.
/// </para>
/// <para>
/// Risk levels assigned:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="RiskLevel.Medium"/>: Large-scale processing detected alone.</description></item>
/// <item><description><see cref="RiskLevel.High"/>: Large-scale processing combined with other risk factors.</description></item>
/// </list>
/// </remarks>
public sealed class LargeScaleProcessingCriterion : IRiskCriterion
{
    /// <inheritdoc />
    public string Name => "Large-Scale Processing";

    /// <inheritdoc />
    public ValueTask<RiskItem?> EvaluateAsync(
        DPIAContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.HighRiskTriggers.Contains(HighRiskTriggers.LargeScaleProcessing))
        {
            return ValueTask.FromResult<RiskItem?>(null);
        }

        var hasOtherFactors = context.HighRiskTriggers.Count > 1;
        var level = hasOtherFactors ? RiskLevel.High : RiskLevel.Medium;

        var description = hasOtherFactors
            ? "Large-scale processing combined with additional risk factors increases overall risk to data subjects."
            : "Large-scale processing of personal data involves significant numbers of data subjects.";

        return ValueTask.FromResult<RiskItem?>(new RiskItem(
            Category: "Large-Scale Processing",
            Level: level,
            Description: description,
            MitigationSuggestion: "Implement proportionate safeguards including data minimization, access controls, and regular audits."));
    }
}
