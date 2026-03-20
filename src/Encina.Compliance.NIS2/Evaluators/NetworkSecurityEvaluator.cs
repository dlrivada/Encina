using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Model;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.NIS2.Evaluators;

/// <summary>
/// Evaluates Art. 21(2)(e): Security in network and information systems acquisition, development, and maintenance.
/// </summary>
internal sealed class NetworkSecurityEvaluator : INIS2MeasureEvaluator
{
    public NIS2Measure Measure => NIS2Measure.NetworkAndSystemSecurity;

    public ValueTask<Either<EncinaError, NIS2MeasureResult>> EvaluateAsync(
        NIS2MeasureContext context,
        CancellationToken cancellationToken = default)
    {
        var result = context.Options.HasNetworkSecurityPolicy
            ? NIS2MeasureResult.Satisfied(Measure, "Network and information system security policies are in place.")
            : NIS2MeasureResult.NotSatisfied(Measure,
                "No network/system security policy configured.",
                ["Integrate security into the SDLC", "Implement vulnerability management processes", "Establish coordinated vulnerability disclosure (CVD) procedures"]);

        return ValueTask.FromResult(Right<EncinaError, NIS2MeasureResult>(result));
    }
}
