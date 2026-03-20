using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Model;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.NIS2.Evaluators;

/// <summary>
/// Evaluates Art. 21(2)(j): The use of multi-factor authentication or continuous authentication solutions.
/// </summary>
internal sealed class MultiFactorAuthenticationEvaluator : INIS2MeasureEvaluator
{
    public NIS2Measure Measure => NIS2Measure.MultiFactorAuthentication;

    public ValueTask<Either<EncinaError, NIS2MeasureResult>> EvaluateAsync(
        NIS2MeasureContext context,
        CancellationToken cancellationToken = default)
    {
        var mfaEnforced = context.Options.EnforceMFA;

        // Check if a custom IMFAEnforcer is registered (beyond the default pass-through)
        var mfaEnforcer = context.ServiceProvider.GetService(typeof(IMFAEnforcer));
        var hasCustomEnforcer = mfaEnforcer is not null and not DefaultMFAEnforcer;

        if (mfaEnforced && hasCustomEnforcer)
        {
            return ValueTask.FromResult(Right<EncinaError, NIS2MeasureResult>(
                NIS2MeasureResult.Satisfied(Measure,
                    "MFA enforcement is enabled with a custom IMFAEnforcer implementation.")));
        }

        if (mfaEnforced)
        {
            return ValueTask.FromResult(Right<EncinaError, NIS2MeasureResult>(
                NIS2MeasureResult.Satisfied(Measure,
                    "MFA enforcement is enabled (using default pass-through enforcer — consider registering a custom IMFAEnforcer).")));
        }

        return ValueTask.FromResult(Right<EncinaError, NIS2MeasureResult>(
            NIS2MeasureResult.NotSatisfied(Measure,
                "MFA enforcement is disabled.",
                ["Enable MFA via NIS2Options.EnforceMFA = true", "Register a custom IMFAEnforcer integrated with your identity provider"])));
    }
}
