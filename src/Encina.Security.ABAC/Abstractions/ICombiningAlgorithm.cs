namespace Encina.Security.ABAC;

/// <summary>
/// Implements a XACML combining algorithm that aggregates multiple rule-level
/// or policy-level evaluation results into a single decision.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 Appendix C — Combining algorithms determine how individual rule or policy
/// effects are merged into a final decision. The standard defines eight algorithms:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="CombiningAlgorithmId.DenyOverrides"/> — any Deny wins</description></item>
/// <item><description><see cref="CombiningAlgorithmId.PermitOverrides"/> — any Permit wins</description></item>
/// <item><description><see cref="CombiningAlgorithmId.FirstApplicable"/> — first applicable result wins</description></item>
/// <item><description><see cref="CombiningAlgorithmId.OnlyOneApplicable"/> — exactly one policy must be applicable</description></item>
/// <item><description><see cref="CombiningAlgorithmId.DenyUnlessPermit"/> — default Deny unless explicit Permit</description></item>
/// <item><description><see cref="CombiningAlgorithmId.PermitUnlessDeny"/> — default Permit unless explicit Deny</description></item>
/// <item><description><see cref="CombiningAlgorithmId.OrderedDenyOverrides"/> — ordered variant of DenyOverrides</description></item>
/// <item><description><see cref="CombiningAlgorithmId.OrderedPermitOverrides"/> — ordered variant of PermitOverrides</description></item>
/// </list>
/// <para>
/// Implementations handle two levels of combining:
/// <see cref="CombineRuleResults"/> for rules within a single policy, and
/// <see cref="CombinePolicyResults"/> for policies within a policy set.
/// </para>
/// </remarks>
public interface ICombiningAlgorithm
{
    /// <summary>
    /// Gets the identifier of this combining algorithm.
    /// </summary>
    CombiningAlgorithmId AlgorithmId { get; }

    /// <summary>
    /// Combines multiple rule evaluation results within a single policy into a final effect.
    /// </summary>
    /// <param name="results">The individual rule evaluation results to combine.</param>
    /// <returns>The combined effect (Permit, Deny, NotApplicable, or Indeterminate).</returns>
    Effect CombineRuleResults(IReadOnlyList<RuleEvaluationResult> results);

    /// <summary>
    /// Combines multiple policy evaluation results within a policy set into a single
    /// aggregated result, including propagated obligations and advice.
    /// </summary>
    /// <param name="results">The individual policy evaluation results to combine.</param>
    /// <returns>
    /// A <see cref="PolicyEvaluationResult"/> containing the combined effect and any
    /// obligations and advice that should be propagated.
    /// </returns>
    PolicyEvaluationResult CombinePolicyResults(IReadOnlyList<PolicyEvaluationResult> results);
}
