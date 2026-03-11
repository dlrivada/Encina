using Encina.Compliance.DPIA.Model;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Risk criterion for systematic monitoring of publicly accessible areas per GDPR Article 35(3)(c).
/// </summary>
/// <remarks>
/// <para>
/// Evaluates whether the processing involves systematic monitoring of a publicly accessible area
/// on a large scale. This includes CCTV surveillance, Wi-Fi tracking, location-based analytics,
/// and other forms of public space monitoring.
/// </para>
/// <para>
/// Always assigns <see cref="RiskLevel.High"/> when the <see cref="HighRiskTriggers.PublicMonitoring"/>
/// trigger is present, as systematic monitoring of public areas inherently poses significant
/// privacy risks.
/// </para>
/// </remarks>
public sealed class SystematicMonitoringCriterion : IRiskCriterion
{
    /// <inheritdoc />
    public string Name => "Systematic Monitoring (Art. 35(3)(c))";

    /// <inheritdoc />
    public ValueTask<RiskItem?> EvaluateAsync(
        DPIAContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.HighRiskTriggers.Contains(HighRiskTriggers.PublicMonitoring))
        {
            return ValueTask.FromResult<RiskItem?>(null);
        }

        return ValueTask.FromResult<RiskItem?>(new RiskItem(
            Category: "Systematic Monitoring",
            Level: RiskLevel.High,
            Description: "Systematic monitoring of a publicly accessible area on a large scale.",
            MitigationSuggestion: "Implement data minimization, clear signage, and purpose limitation for monitoring activities."));
    }
}
