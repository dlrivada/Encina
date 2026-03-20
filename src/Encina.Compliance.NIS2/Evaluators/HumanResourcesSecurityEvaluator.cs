using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Diagnostics;
using Encina.Compliance.NIS2.Model;
using Encina.Security.ABAC;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.NIS2.Evaluators;

/// <summary>
/// Evaluates Art. 21(2)(i): Human resources security, access control policies, and asset management.
/// </summary>
/// <remarks>
/// <para>
/// Beyond the configuration flag, this evaluator checks whether
/// <c>Encina.Security.ABAC</c>'s <c>IPolicyDecisionPoint</c> is registered in the DI container,
/// providing evidence that attribute-based access control is in place — a key requirement
/// for NIS2's access control policies (Art. 21(2)(i)).
/// </para>
/// </remarks>
internal sealed class HumanResourcesSecurityEvaluator : INIS2MeasureEvaluator
{
    public NIS2Measure Measure => NIS2Measure.HumanResourcesSecurity;

    public ValueTask<Either<EncinaError, NIS2MeasureResult>> EvaluateAsync(
        NIS2MeasureContext context,
        CancellationToken cancellationToken = default)
    {
        var hasPolicy = context.Options.HasHumanResourcesSecurity;

        // Check if ABAC (Attribute-Based Access Control) is available for fine-grained access control
        var hasPDP = context.ServiceProvider
            .GetService<IPolicyDecisionPoint>() is not null;

        var logger = context.ServiceProvider.GetService<ILoggerFactory>()?
            .CreateLogger<HumanResourcesSecurityEvaluator>();
        logger?.ABACPolicyChecked(hasPDP, hasPDP ? "available" : "not_registered");

        if (hasPolicy && hasPDP)
        {
            return ValueTask.FromResult(Right<EncinaError, NIS2MeasureResult>(
                NIS2MeasureResult.Satisfied(Measure,
                    "Human resources security and access control policies are in place with "
                    + "attribute-based access control (ABAC) via IPolicyDecisionPoint.")));
        }

        if (hasPolicy)
        {
            return ValueTask.FromResult(Right<EncinaError, NIS2MeasureResult>(
                NIS2MeasureResult.Satisfied(Measure,
                    "Human resources security and access control policies are in place. "
                    + "Consider registering IPolicyDecisionPoint (Encina.Security.ABAC) "
                    + "for fine-grained attribute-based access control.")));
        }

        var recommendations = new List<string>
        {
            "Implement access control policies (least privilege, RBAC)",
            "Establish HR security measures (background checks, clearances)",
            "Define asset management lifecycle"
        };

        if (!hasPDP)
        {
            recommendations.Add("Register Encina.Security.ABAC for attribute-based access control (IPolicyDecisionPoint)");
        }

        return ValueTask.FromResult(Right<EncinaError, NIS2MeasureResult>(
            NIS2MeasureResult.NotSatisfied(Measure,
                "No HR security or access control policies configured.",
                recommendations)));
    }
}
