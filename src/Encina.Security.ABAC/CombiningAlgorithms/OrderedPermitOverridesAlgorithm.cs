namespace Encina.Security.ABAC.CombiningAlgorithms;

/// <summary>
/// Implements the XACML 3.0 Ordered-Permit-Overrides combining algorithm (§C.8).
/// </summary>
/// <remarks>
/// <para>
/// Same semantics as <see cref="PermitOverridesAlgorithm"/> but guarantees evaluation
/// in declaration order. Since our implementation already iterates lists in order,
/// the behavior is identical — this class exists to provide the distinct
/// <see cref="CombiningAlgorithmId.OrderedPermitOverrides"/> identifier for explicit
/// policy configuration.
/// </para>
/// </remarks>
public sealed class OrderedPermitOverridesAlgorithm : ICombiningAlgorithm
{
    private readonly PermitOverridesAlgorithm _inner = new();

    /// <inheritdoc />
    public CombiningAlgorithmId AlgorithmId => CombiningAlgorithmId.OrderedPermitOverrides;

    /// <inheritdoc />
    public Effect CombineRuleResults(IReadOnlyList<RuleEvaluationResult> results) =>
        _inner.CombineRuleResults(results);

    /// <inheritdoc />
    public PolicyEvaluationResult CombinePolicyResults(IReadOnlyList<PolicyEvaluationResult> results) =>
        _inner.CombinePolicyResults(results);
}
