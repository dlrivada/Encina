namespace Encina.Security.ABAC;

/// <summary>
/// Represents the evaluation result of a single <see cref="Policy"/> or <see cref="PolicySet"/>,
/// used internally by combining algorithms to aggregate decisions.
/// </summary>
/// <remarks>
/// <para>
/// This record is an intermediate result used by <c>ICombiningAlgorithm</c> implementations
/// to combine individual policy or policy set effects into a final <see cref="PolicyDecision"/>.
/// Each result carries the effect along with any obligations and advice that should be
/// propagated if this result's effect matches the final decision.
/// </para>
/// </remarks>
public sealed record PolicyEvaluationResult
{
    /// <summary>
    /// The effect produced by evaluating the policy or policy set.
    /// </summary>
    public required Effect Effect { get; init; }

    /// <summary>
    /// The identifier of the policy or policy set that produced this result.
    /// </summary>
    public required string PolicyId { get; init; }

    /// <summary>
    /// Obligations collected from this policy evaluation, conditional on the effect.
    /// </summary>
    public required IReadOnlyList<Obligation> Obligations { get; init; }

    /// <summary>
    /// Advice collected from this policy evaluation, conditional on the effect.
    /// </summary>
    public required IReadOnlyList<AdviceExpression> Advice { get; init; }
}
