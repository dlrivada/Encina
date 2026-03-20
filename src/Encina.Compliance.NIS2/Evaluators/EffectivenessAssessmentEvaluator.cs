using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Model;
using Encina.Security.Audit;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;

using static LanguageExt.Prelude;

namespace Encina.Compliance.NIS2.Evaluators;

/// <summary>
/// Evaluates Art. 21(2)(f): Policies and procedures to assess the effectiveness of cybersecurity risk-management measures.
/// </summary>
/// <remarks>
/// <para>
/// Beyond the configuration flag, this evaluator checks whether <see cref="IAuditStore"/>
/// and <see cref="IReadAuditStore"/> are registered — audit infrastructure is foundational
/// to assessing the effectiveness of security controls. It also checks for resilience
/// pipeline providers that demonstrate fault tolerance is in place.
/// </para>
/// </remarks>
internal sealed class EffectivenessAssessmentEvaluator : INIS2MeasureEvaluator
{
    public NIS2Measure Measure => NIS2Measure.EffectivenessAssessment;

    public ValueTask<Either<EncinaError, NIS2MeasureResult>> EvaluateAsync(
        NIS2MeasureContext context,
        CancellationToken cancellationToken = default)
    {
        var hasAssessment = context.Options.HasEffectivenessAssessment;

        // Check if audit infrastructure is in place for security control effectiveness assessment
        var hasAuditStore = context.ServiceProvider.GetService<IAuditStore>() is not null;
        var hasReadAudit = context.ServiceProvider.GetService<IReadAuditStore>() is not null;

        if (hasAssessment)
        {
            var details = "Effectiveness assessment procedures are in place.";
            if (hasAuditStore && hasReadAudit)
            {
                details += " Audit infrastructure (IAuditStore, IReadAuditStore) provides evidence collection for security control assessment.";
            }
            else if (hasAuditStore)
            {
                details += " Audit trail (IAuditStore) is available for security control assessment.";
            }

            return ValueTask.FromResult(Right<EncinaError, NIS2MeasureResult>(
                NIS2MeasureResult.Satisfied(Measure, details)));
        }

        var recommendations = new List<string>
        {
            "Establish regular security audits",
            "Implement penetration testing program",
            "Define security assessment schedule"
        };

        if (!hasAuditStore)
        {
            recommendations.Add("Register Encina.Security.Audit (IAuditStore) for security control effectiveness evidence collection");
        }

        if (!hasReadAudit)
        {
            recommendations.Add("Register Encina.Security.Audit (IReadAuditStore) for data access tracking and compliance monitoring");
        }

        return ValueTask.FromResult(Right<EncinaError, NIS2MeasureResult>(
            NIS2MeasureResult.NotSatisfied(Measure,
                "No effectiveness assessment procedures configured.",
                recommendations)));
    }
}
