namespace Encina.Security.ABAC.CombiningAlgorithms;

/// <summary>
/// Implements the XACML 3.0 Deny-Overrides combining algorithm (§C.1).
/// </summary>
/// <remarks>
/// <para>
/// Any Deny result overrides all other results. The algorithm tracks four types
/// of intermediate results to correctly handle Indeterminate effects:
/// </para>
/// <list type="number">
/// <item><description>Any Deny → <see cref="Effect.Deny"/></description></item>
/// <item><description>Any Indeterminate{D} → <see cref="Effect.Indeterminate"/></description></item>
/// <item><description>Any Permit + Indeterminate{P} → <see cref="Effect.Indeterminate"/></description></item>
/// <item><description>Any Permit → <see cref="Effect.Permit"/></description></item>
/// <item><description>Any Indeterminate{P} → <see cref="Effect.Indeterminate"/></description></item>
/// <item><description>Otherwise → <see cref="Effect.NotApplicable"/></description></item>
/// </list>
/// </remarks>
public sealed class DenyOverridesAlgorithm : ICombiningAlgorithm
{
    /// <inheritdoc />
    public CombiningAlgorithmId AlgorithmId => CombiningAlgorithmId.DenyOverrides;

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
                    // Classify based on the rule's intended effect
                    if (result.Rule.Effect == Effect.Deny)
                    {
                        atLeastOneIndeterminateD = true;
                    }
                    else
                    {
                        atLeastOneIndeterminateP = true;
                    }

                    break;

                    // NotApplicable — skip
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
                    // At policy level, Indeterminate could be either D or P
                    // Conservatively treat as both
                    atLeastOneIndeterminateD = true;
                    atLeastOneIndeterminateP = true;
                    break;

                    // NotApplicable — skip
            }
        }

        var combinedEffect = CombineEffects(atLeastOneDeny, atLeastOnePermit, atLeastOneIndeterminateD, atLeastOneIndeterminateP);
        return BuildCombinedResult(combinedEffect, results);
    }

    /// <summary>
    /// Applies the Deny-Overrides decision table.
    /// </summary>
    internal static Effect CombineEffects(
        bool atLeastOneDeny,
        bool atLeastOnePermit,
        bool atLeastOneIndeterminateD,
        bool atLeastOneIndeterminateP)
    {
        if (atLeastOneDeny)
        {
            return Effect.Deny;
        }

        if (atLeastOneIndeterminateD)
        {
            return Effect.Indeterminate;
        }

        if (atLeastOnePermit && atLeastOneIndeterminateP)
        {
            return Effect.Indeterminate;
        }

        if (atLeastOnePermit)
        {
            return Effect.Permit;
        }

        if (atLeastOneIndeterminateP)
        {
            return Effect.Indeterminate;
        }

        return Effect.NotApplicable;
    }

    /// <summary>
    /// Builds a combined <see cref="PolicyEvaluationResult"/> by collecting obligations
    /// and advice from all results whose effect matches the combined decision.
    /// </summary>
    internal static PolicyEvaluationResult BuildCombinedResult(
        Effect combinedEffect,
        IReadOnlyList<PolicyEvaluationResult> results)
    {
        var obligations = new List<Obligation>();
        var advice = new List<AdviceExpression>();

        foreach (var result in results)
        {
            if (result.Effect == combinedEffect)
            {
                obligations.AddRange(result.Obligations);
                advice.AddRange(result.Advice);
            }
        }

        return new PolicyEvaluationResult
        {
            Effect = combinedEffect,
            PolicyId = results.FirstOrDefault(r => r.Effect == combinedEffect)?.PolicyId ?? string.Empty,
            Obligations = obligations,
            Advice = advice
        };
    }
}
