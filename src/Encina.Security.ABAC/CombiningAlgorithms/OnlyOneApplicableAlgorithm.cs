namespace Encina.Security.ABAC.CombiningAlgorithms;

/// <summary>
/// Implements the XACML 3.0 Only-One-Applicable combining algorithm (§C.4).
/// </summary>
/// <remarks>
/// <para>
/// At the policy level, this algorithm requires exactly one applicable policy.
/// If zero policies are applicable, the result is <see cref="Effect.NotApplicable"/>.
/// If more than one policy is applicable, the result is <see cref="Effect.Indeterminate"/>
/// (ambiguous configuration).
/// </para>
/// <para>
/// At the rule level, this algorithm is not standard per XACML 3.0 and delegates
/// to <see cref="FirstApplicableAlgorithm"/> semantics.
/// </para>
/// </remarks>
public sealed class OnlyOneApplicableAlgorithm : ICombiningAlgorithm
{
    private readonly FirstApplicableAlgorithm _firstApplicable = new();

    /// <inheritdoc />
    public CombiningAlgorithmId AlgorithmId => CombiningAlgorithmId.OnlyOneApplicable;

    /// <inheritdoc />
    public Effect CombineRuleResults(IReadOnlyList<RuleEvaluationResult> results)
    {
        // OnlyOneApplicable is not standard for rules; delegate to FirstApplicable
        return _firstApplicable.CombineRuleResults(results);
    }

    /// <inheritdoc />
    public PolicyEvaluationResult CombinePolicyResults(IReadOnlyList<PolicyEvaluationResult> results)
    {
        PolicyEvaluationResult? applicableResult = null;

        foreach (var result in results)
        {
            if (result.Effect == Effect.Indeterminate)
            {
                // Any indeterminate during evaluation → overall indeterminate
                return new PolicyEvaluationResult
                {
                    Effect = Effect.Indeterminate,
                    PolicyId = result.PolicyId,
                    Obligations = [],
                    Advice = []
                };
            }

            if (result.Effect != Effect.NotApplicable)
            {
                if (applicableResult is not null)
                {
                    // More than one applicable → indeterminate
                    return new PolicyEvaluationResult
                    {
                        Effect = Effect.Indeterminate,
                        PolicyId = string.Empty,
                        Obligations = [],
                        Advice = []
                    };
                }

                applicableResult = result;
            }
        }

        // Return the single applicable result, or NotApplicable if none
        return applicableResult ?? new PolicyEvaluationResult
        {
            Effect = Effect.NotApplicable,
            PolicyId = string.Empty,
            Obligations = [],
            Advice = []
        };
    }
}
