namespace Encina.Security.ABAC.CombiningAlgorithms;

/// <summary>
/// Implements the XACML 3.0 Permit-Overrides combining algorithm (§C.2).
/// </summary>
/// <remarks>
/// <para>
/// Mirror of <see cref="DenyOverridesAlgorithm"/> with Permit and Deny swapped.
/// Any Permit result overrides all other results.
/// </para>
/// <list type="number">
/// <item><description>Any Permit → <see cref="Effect.Permit"/></description></item>
/// <item><description>Any Indeterminate{P} → <see cref="Effect.Indeterminate"/></description></item>
/// <item><description>Any Deny + Indeterminate{D} → <see cref="Effect.Indeterminate"/></description></item>
/// <item><description>Any Deny → <see cref="Effect.Deny"/></description></item>
/// <item><description>Any Indeterminate{D} → <see cref="Effect.Indeterminate"/></description></item>
/// <item><description>Otherwise → <see cref="Effect.NotApplicable"/></description></item>
/// </list>
/// </remarks>
public sealed class PermitOverridesAlgorithm : ICombiningAlgorithm
{
    /// <inheritdoc />
    public CombiningAlgorithmId AlgorithmId => CombiningAlgorithmId.PermitOverrides;

    /// <inheritdoc />
    public Effect CombineRuleResults(IReadOnlyList<RuleEvaluationResult> results)
    {
        var atLeastOneDeny = false;
        var atLeastOnePermit = false;
        var atLeastOneIndeterminateD = false;
        var atLeastOneIndeterminateP = false;

        foreach (var result in results)
        {
            switch (result.Effect)
            {
                case Effect.Deny:
                    atLeastOneDeny = true;
                    break;

                case Effect.Permit:
                    atLeastOnePermit = true;
                    break;

                case Effect.Indeterminate:
                    if (result.Rule.Effect == Effect.Deny)
                    {
                        atLeastOneIndeterminateD = true;
                    }
                    else
                    {
                        atLeastOneIndeterminateP = true;
                    }

                    break;
            }
        }

        return CombineEffects(atLeastOneDeny, atLeastOnePermit, atLeastOneIndeterminateD, atLeastOneIndeterminateP);
    }

    /// <inheritdoc />
    public PolicyEvaluationResult CombinePolicyResults(IReadOnlyList<PolicyEvaluationResult> results)
    {
        var atLeastOneDeny = false;
        var atLeastOnePermit = false;
        var atLeastOneIndeterminateD = false;
        var atLeastOneIndeterminateP = false;

        foreach (var result in results)
        {
            switch (result.Effect)
            {
                case Effect.Deny:
                    atLeastOneDeny = true;
                    break;

                case Effect.Permit:
                    atLeastOnePermit = true;
                    break;

                case Effect.Indeterminate:
                    atLeastOneIndeterminateD = true;
                    atLeastOneIndeterminateP = true;
                    break;
            }
        }

        var combinedEffect = CombineEffects(atLeastOneDeny, atLeastOnePermit, atLeastOneIndeterminateD, atLeastOneIndeterminateP);
        return DenyOverridesAlgorithm.BuildCombinedResult(combinedEffect, results);
    }

    /// <summary>
    /// Applies the Permit-Overrides decision table (mirror of Deny-Overrides).
    /// </summary>
    internal static Effect CombineEffects(
        bool atLeastOneDeny,
        bool atLeastOnePermit,
        bool atLeastOneIndeterminateD,
        bool atLeastOneIndeterminateP)
    {
        if (atLeastOnePermit)
        {
            return Effect.Permit;
        }

        if (atLeastOneIndeterminateP)
        {
            return Effect.Indeterminate;
        }

        if (atLeastOneDeny && atLeastOneIndeterminateD)
        {
            return Effect.Indeterminate;
        }

        if (atLeastOneDeny)
        {
            return Effect.Deny;
        }

        if (atLeastOneIndeterminateD)
        {
            return Effect.Indeterminate;
        }

        return Effect.NotApplicable;
    }
}
