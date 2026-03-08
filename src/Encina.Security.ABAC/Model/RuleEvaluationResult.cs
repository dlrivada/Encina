namespace Encina.Security.ABAC;

/// <summary>
/// Represents the evaluation result of a single <see cref="Rule"/>, used internally
/// by combining algorithms to aggregate rule-level decisions within a <see cref="Policy"/>.
/// </summary>
/// <remarks>
/// <para>
/// This record is an intermediate result used by <c>ICombiningAlgorithm</c> implementations
/// at the rule level. It carries the original <see cref="Rule"/> reference along with
/// the computed effect and any associated obligations and advice.
/// </para>
/// </remarks>
public sealed record RuleEvaluationResult
{
    /// <summary>
    /// The rule that was evaluated.
    /// </summary>
    public required Rule Rule { get; init; }

    /// <summary>
    /// The effect produced by evaluating the rule.
    /// </summary>
    public required Effect Effect { get; init; }

    /// <summary>
    /// Obligations from this rule, conditional on the effect matching the final decision.
    /// </summary>
    public required IReadOnlyList<Obligation> Obligations { get; init; }

    /// <summary>
    /// Advice from this rule, conditional on the effect matching the final decision.
    /// </summary>
    public required IReadOnlyList<AdviceExpression> Advice { get; init; }
}
