using Encina.Compliance.GDPR;
using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Diagnostics;
using Encina.Compliance.NIS2.Model;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.NIS2.Evaluators;

/// <summary>
/// Evaluates Art. 21(2)(a): Policies on risk analysis and information system security.
/// </summary>
/// <remarks>
/// <para>
/// Beyond the configuration flag, this evaluator checks whether <c>Encina.Compliance.GDPR</c>'s
/// <see cref="IGDPRComplianceValidator"/> and <see cref="IProcessingActivityRegistry"/> are
/// registered AND configured. For the processing registry, it verifies that actual processing
/// activities are registered (not just that the interface is in DI), providing meaningful
/// NIS2 Art. 35 GDPR alignment verification.
/// </para>
/// </remarks>
internal sealed class RiskAnalysisEvaluator : INIS2MeasureEvaluator
{
    private readonly ILogger<RiskAnalysisEvaluator> _logger;

    public RiskAnalysisEvaluator(ILogger<RiskAnalysisEvaluator> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public NIS2Measure Measure => NIS2Measure.RiskAnalysisAndSecurityPolicies;

    public async ValueTask<Either<EncinaError, NIS2MeasureResult>> EvaluateAsync(
        NIS2MeasureContext context,
        CancellationToken cancellationToken = default)
    {
        var hasPolicy = context.Options.HasRiskAnalysisPolicy;

        // Check if GDPR compliance infrastructure is available for risk analysis alignment
        var hasGDPRValidator = context.ServiceProvider
            .GetService<IGDPRComplianceValidator>() is not null;
        var processingRegistry = context.ServiceProvider
            .GetService<IProcessingActivityRegistry>();

        // Go deeper than presence: verify actual processing activities are registered
        var activityCount = 0;
        if (processingRegistry is not null)
        {
            activityCount = await GetProcessingActivityCountAsync(
                processingRegistry, context, cancellationToken).ConfigureAwait(false);
        }

        var hasActiveProcessingActivities = activityCount > 0;

        _logger.GDPRAlignmentChecked(hasGDPRValidator, hasActiveProcessingActivities);

        if (hasPolicy)
        {
            var details = "Risk analysis and security policies are in place.";
            if (hasGDPRValidator && hasActiveProcessingActivities)
            {
                details += $" GDPR compliance validator and processing activity registry ({activityCount} activities) "
                    + "are integrated for unified NIS2/GDPR risk analysis (Art. 35 alignment).";
            }
            else if (hasGDPRValidator && processingRegistry is not null)
            {
                details += " GDPR compliance validator is available but processing activity registry is empty. "
                    + "Register processing activities for full Art. 35 alignment.";
            }
            else if (hasGDPRValidator)
            {
                details += " GDPR compliance validator is available for NIS2/GDPR risk alignment.";
            }

            return Right<EncinaError, NIS2MeasureResult>(
                NIS2MeasureResult.Satisfied(Measure, details));
        }

        var recommendations = new List<string>
        {
            "Establish a risk analysis policy",
            "Define risk assessment methodology",
            "Schedule regular policy reviews"
        };

        if (!hasGDPRValidator)
        {
            recommendations.Add("Register Encina.Compliance.GDPR for integrated data protection risk analysis (NIS2 Art. 35 alignment)");
        }

        if (processingRegistry is null)
        {
            recommendations.Add("Register IProcessingActivityRegistry with processing activities for GDPR Art. 30 / NIS2 Art. 35 compliance");
        }
        else if (activityCount == 0)
        {
            recommendations.Add("Register processing activities in IProcessingActivityRegistry — registry is available but empty");
        }

        return Right<EncinaError, NIS2MeasureResult>(
            NIS2MeasureResult.NotSatisfied(Measure,
                "No risk analysis policy configured.",
                recommendations));
    }

    /// <summary>
    /// Queries the processing activity registry for the count of registered activities.
    /// Uses resilience protection for the external call.
    /// </summary>
    private static async ValueTask<int> GetProcessingActivityCountAsync(
        IProcessingActivityRegistry registry,
        NIS2MeasureContext context,
        CancellationToken cancellationToken)
    {
        var timeout = context.Options.ExternalCallTimeout;

        return await NIS2ResilienceHelper.ExecuteAsync(
            context.ServiceProvider,
            async ct =>
            {
                var result = await registry.GetAllActivitiesAsync(ct).ConfigureAwait(false);
                return result.Match(
                    Right: activities => activities.Count,
                    Left: _ => 0);
            },
            fallback: 0,
            timeout,
            cancellationToken).ConfigureAwait(false);
    }
}
