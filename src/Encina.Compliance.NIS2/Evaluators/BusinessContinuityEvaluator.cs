using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Model;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.NIS2.Evaluators;

/// <summary>
/// Evaluates Art. 21(2)(c): Business continuity, backup management, disaster recovery, and crisis management.
/// </summary>
internal sealed class BusinessContinuityEvaluator : INIS2MeasureEvaluator
{
    public NIS2Measure Measure => NIS2Measure.BusinessContinuity;

    public ValueTask<Either<EncinaError, NIS2MeasureResult>> EvaluateAsync(
        NIS2MeasureContext context,
        CancellationToken cancellationToken = default)
    {
        var result = context.Options.HasBusinessContinuityPlan
            ? NIS2MeasureResult.Satisfied(Measure, "Business continuity and disaster recovery plans are in place.")
            : NIS2MeasureResult.NotSatisfied(Measure,
                "No business continuity plan configured.",
                ["Establish business continuity plan", "Implement backup management strategy", "Define disaster recovery procedures", "Test crisis management processes"]);

        return ValueTask.FromResult(Right<EncinaError, NIS2MeasureResult>(result));
    }
}
