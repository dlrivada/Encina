using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Model;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.NIS2.Evaluators;

/// <summary>
/// Evaluates Art. 21(2)(g): Basic cyber hygiene practices and cybersecurity training.
/// </summary>
internal sealed class CyberHygieneEvaluator : INIS2MeasureEvaluator
{
    public NIS2Measure Measure => NIS2Measure.CyberHygiene;

    public ValueTask<Either<EncinaError, NIS2MeasureResult>> EvaluateAsync(
        NIS2MeasureContext context,
        CancellationToken cancellationToken = default)
    {
        var hasProgram = context.Options.HasCyberHygieneProgram;
        var hasManagement = context.Options.ManagementAccountability is not null;
        var hasTraining = context.Options.ManagementAccountability?.TrainingCompletedAtUtc is not null;

        if (hasProgram && hasManagement && hasTraining)
        {
            return ValueTask.FromResult(Right<EncinaError, NIS2MeasureResult>(
                NIS2MeasureResult.Satisfied(Measure, "Cyber hygiene program is in place with management training completed.")));
        }

        var recommendations = new List<string>();
        if (!hasProgram) recommendations.Add("Implement cyber hygiene practices (patching, password policies, secure configuration)");
        if (!hasManagement) recommendations.Add("Configure ManagementAccountability in NIS2Options (Art. 20)");
        if (!hasTraining) recommendations.Add("Ensure management body members complete cybersecurity training (Art. 20(2))");

        return ValueTask.FromResult(Right<EncinaError, NIS2MeasureResult>(
            NIS2MeasureResult.NotSatisfied(Measure,
                "Cyber hygiene and training requirements are not fully met.",
                recommendations)));
    }
}
