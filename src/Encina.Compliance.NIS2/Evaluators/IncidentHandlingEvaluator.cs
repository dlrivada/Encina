using Encina.Compliance.BreachNotification.Abstractions;
using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Model;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;

using static LanguageExt.Prelude;

namespace Encina.Compliance.NIS2.Evaluators;

/// <summary>
/// Evaluates Art. 21(2)(b): Incident handling.
/// </summary>
/// <remarks>
/// <para>
/// Beyond configuration flags, this evaluator checks whether
/// <see cref="IBreachNotificationService"/> from <c>Encina.Compliance.BreachNotification</c>
/// is registered, providing evidence that persistent incident lifecycle tracking is in place.
/// </para>
/// </remarks>
internal sealed class IncidentHandlingEvaluator : INIS2MeasureEvaluator
{
    public NIS2Measure Measure => NIS2Measure.IncidentHandling;

    public ValueTask<Either<EncinaError, NIS2MeasureResult>> EvaluateAsync(
        NIS2MeasureContext context,
        CancellationToken cancellationToken = default)
    {
        var hasIncidentHandling = context.Options.HasIncidentHandlingProcedures;
        var hasAuthority = !string.IsNullOrWhiteSpace(context.Options.CompetentAuthority);

        // Check if persistent incident tracking infrastructure is available
        var hasBreachService = context.ServiceProvider
            .GetService<IBreachNotificationService>() is not null;

        if (hasIncidentHandling && hasAuthority)
        {
            var details = hasBreachService
                ? "Incident handling procedures are in place with CSIRT contact configured "
                  + "and persistent breach lifecycle tracking via IBreachNotificationService."
                : "Incident handling procedures are in place with CSIRT contact configured.";

            return ValueTask.FromResult(Right<EncinaError, NIS2MeasureResult>(
                NIS2MeasureResult.Satisfied(Measure, details)));
        }

        var recommendations = new List<string>();
        if (!hasIncidentHandling) recommendations.Add("Implement incident detection, reporting, and response procedures");
        if (!hasAuthority) recommendations.Add("Configure CompetentAuthority contact for CSIRT/authority notification (Art. 23)");
        if (!hasBreachService) recommendations.Add("Register Encina.Compliance.BreachNotification for persistent event-sourced incident lifecycle tracking");

        return ValueTask.FromResult(Right<EncinaError, NIS2MeasureResult>(
            NIS2MeasureResult.NotSatisfied(Measure,
                "Incident handling is not fully configured.",
                recommendations)));
    }
}
