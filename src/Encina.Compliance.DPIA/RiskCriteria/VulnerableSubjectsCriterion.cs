using Encina.Compliance.DPIA.Model;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Risk criterion for processing data of vulnerable subjects.
/// </summary>
/// <remarks>
/// <para>
/// Evaluates whether the processing involves data of vulnerable individuals who may be unable
/// to freely consent to or oppose the processing. Vulnerable subjects include children,
/// employees, patients, the elderly, and other groups identified in Recital 75 and by
/// the EDPB in WP 248 rev.01.
/// </para>
/// <para>
/// Always assigns <see cref="RiskLevel.High"/> when the <see cref="HighRiskTriggers.VulnerableSubjects"/>
/// trigger is present, reflecting the inherent power imbalance and potential for exploitation.
/// </para>
/// </remarks>
public sealed class VulnerableSubjectsCriterion : IRiskCriterion
{
    /// <inheritdoc />
    public string Name => "Vulnerable Subjects";

    /// <inheritdoc />
    public ValueTask<RiskItem?> EvaluateAsync(
        DPIAContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.HighRiskTriggers.Contains(HighRiskTriggers.VulnerableSubjects))
        {
            return ValueTask.FromResult<RiskItem?>(null);
        }

        return ValueTask.FromResult<RiskItem?>(new RiskItem(
            Category: "Vulnerable Subjects",
            Level: RiskLevel.High,
            Description: "Processing involves data of vulnerable subjects (e.g., children, employees, patients) who may be unable to freely consent or oppose the processing.",
            MitigationSuggestion: "Implement additional safeguards including explicit consent mechanisms and enhanced transparency measures."));
    }
}
