namespace Encina.Security.ABAC.CombiningAlgorithms;

/// <summary>
/// Implements the XACML 3.0 Permit-Unless-Deny combining algorithm (§C.6).
/// </summary>
/// <remarks>
/// <para>
/// Mirror of <see cref="DenyUnlessPermitAlgorithm"/>: if any component returns
/// <see cref="Effect.Deny"/>, the combined result is Deny; otherwise it is
/// <see cref="Effect.Permit"/>. This algorithm never returns
/// <see cref="Effect.NotApplicable"/> or <see cref="Effect.Indeterminate"/>.
/// </para>
/// <para>
/// Suitable for open systems where access is generally allowed and only
/// specific denials are defined.
/// </para>
/// </remarks>
public sealed class PermitUnlessDenyAlgorithm : ICombiningAlgorithm
{
    /// <inheritdoc />
    public CombiningAlgorithmId AlgorithmId => CombiningAlgorithmId.PermitUnlessDeny;

    /// <inheritdoc />
    public Effect CombineRuleResults(IReadOnlyList<RuleEvaluationResult> results)
    {
        foreach (var result in results)
        {
            if (result.Effect == Effect.Deny)
            {
                return Effect.Deny;
            }
        }

        return Effect.Permit;
    }

    /// <inheritdoc />
    public PolicyEvaluationResult CombinePolicyResults(IReadOnlyList<PolicyEvaluationResult> results)
    {
        var hasDeny = false;

        foreach (var result in results)
        {
            if (result.Effect == Effect.Deny)
            {
                hasDeny = true;
                break;
            }
        }

        var combinedEffect = hasDeny ? Effect.Deny : Effect.Permit;
        return DenyOverridesAlgorithm.BuildCombinedResult(combinedEffect, results);
    }
}
