namespace Encina.Security.ABAC.CombiningAlgorithms;

/// <summary>
/// Implements the XACML 3.0 Ordered-Deny-Overrides combining algorithm (§C.7).
/// </summary>
/// <remarks>
/// <para>
/// Same semantics as <see cref="DenyOverridesAlgorithm"/> but guarantees evaluation
/// in declaration order. Since our implementation already iterates lists in order,
/// the behavior is identical — this class exists to provide the distinct
/// <see cref="CombiningAlgorithmId.OrderedDenyOverrides"/> identifier for explicit
/// policy configuration.
/// </para>
/// </remarks>
public sealed class OrderedDenyOverridesAlgorithm : ICombiningAlgorithm
{
    private readonly DenyOverridesAlgorithm _inner = new();

    /// <inheritdoc />
    public CombiningAlgorithmId AlgorithmId => CombiningAlgorithmId.OrderedDenyOverrides;

    /// <inheritdoc />
    public Effect CombineRuleResults(IReadOnlyList<RuleEvaluationResult> results) =>
        _inner.CombineRuleResults(results);

    /// <inheritdoc />
    public PolicyEvaluationResult CombinePolicyResults(IReadOnlyList<PolicyEvaluationResult> results) =>
        _inner.CombinePolicyResults(results);
}
