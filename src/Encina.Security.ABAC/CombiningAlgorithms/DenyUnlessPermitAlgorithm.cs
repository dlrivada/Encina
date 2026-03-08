namespace Encina.Security.ABAC.CombiningAlgorithms;

/// <summary>
/// Implements the XACML 3.0 Deny-Unless-Permit combining algorithm (§C.5).
/// </summary>
/// <remarks>
/// <para>
/// The simplest combining algorithm: if any component returns <see cref="Effect.Permit"/>,
/// the combined result is Permit; otherwise it is <see cref="Effect.Deny"/>.
/// This algorithm never returns <see cref="Effect.NotApplicable"/> or
/// <see cref="Effect.Indeterminate"/>.
/// </para>
/// <para>
/// This is the safest algorithm for security-critical systems where access
/// should be denied by default unless explicitly permitted.
/// </para>
/// </remarks>
public sealed class DenyUnlessPermitAlgorithm : ICombiningAlgorithm
{
    /// <inheritdoc />
    public CombiningAlgorithmId AlgorithmId => CombiningAlgorithmId.DenyUnlessPermit;

    /// <inheritdoc />
    public Effect CombineRuleResults(IReadOnlyList<RuleEvaluationResult> results)
    {
        foreach (var result in results)
        {
            if (result.Effect == Effect.Permit)
            {
                return Effect.Permit;
            }
        }

        return Effect.Deny;
    }

    /// <inheritdoc />
    public PolicyEvaluationResult CombinePolicyResults(IReadOnlyList<PolicyEvaluationResult> results)
    {
        var hasPermit = false;

        foreach (var result in results)
        {
            if (result.Effect == Effect.Permit)
            {
                hasPermit = true;
                break;
            }
        }

        var combinedEffect = hasPermit ? Effect.Permit : Effect.Deny;
        return DenyOverridesAlgorithm.BuildCombinedResult(combinedEffect, results);
    }
}
