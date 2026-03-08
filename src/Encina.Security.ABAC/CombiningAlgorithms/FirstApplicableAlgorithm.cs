namespace Encina.Security.ABAC.CombiningAlgorithms;

/// <summary>
/// Implements the XACML 3.0 First-Applicable combining algorithm (§C.3).
/// </summary>
/// <remarks>
/// <para>
/// Returns the effect of the first applicable component. Components are evaluated
/// in declaration order, and the first non-<see cref="Effect.NotApplicable"/> result
/// is returned immediately.
/// </para>
/// <para>
/// If all components return <see cref="Effect.NotApplicable"/>, the combined result
/// is <see cref="Effect.NotApplicable"/>.
/// </para>
/// </remarks>
public sealed class FirstApplicableAlgorithm : ICombiningAlgorithm
{
    /// <inheritdoc />
    public CombiningAlgorithmId AlgorithmId => CombiningAlgorithmId.FirstApplicable;

    /// <inheritdoc />
    public Effect CombineRuleResults(IReadOnlyList<RuleEvaluationResult> results)
    {
        foreach (var result in results)
        {
            if (result.Effect != Effect.NotApplicable)
            {
                return result.Effect;
            }
        }

        return Effect.NotApplicable;
    }

    /// <inheritdoc />
    public PolicyEvaluationResult CombinePolicyResults(IReadOnlyList<PolicyEvaluationResult> results)
    {
        foreach (var result in results)
        {
            if (result.Effect != Effect.NotApplicable)
            {
                return result;
            }
        }

        return new PolicyEvaluationResult
        {
            Effect = Effect.NotApplicable,
            PolicyId = string.Empty,
            Obligations = [],
            Advice = []
        };
    }
}
