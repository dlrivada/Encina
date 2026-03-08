namespace Encina.Security.ABAC;

/// <summary>
/// Represents an XACML 3.0 Rule — the leaf node of the policy hierarchy that
/// produces an authorization effect based on a target match and an optional condition.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.9 — A Rule is the most granular policy element. It specifies:
/// </para>
/// <list type="bullet">
/// <item><description>An <see cref="Effect"/> (Permit or Deny) to return when the rule applies and the condition is satisfied</description></item>
/// <item><description>An optional <see cref="Target"/> that determines applicability</description></item>
/// <item><description>An optional <see cref="Condition"/> (an <see cref="Apply"/> expression) that must evaluate to <c>true</c> for the effect to apply</description></item>
/// <item><description>Optional <see cref="Obligations"/> and <see cref="Advice"/> that accompany the effect</description></item>
/// </list>
/// <para>
/// When the target does not match, the rule returns <see cref="ABAC.Effect.NotApplicable"/>.
/// When the condition evaluation fails, the rule returns <see cref="ABAC.Effect.Indeterminate"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var rule = new Rule
/// {
///     Id = "allow-finance-read",
///     Description = "Allow Finance department to read financial reports",
///     Effect = Effect.Permit,
///     Target = financeDeptTarget,
///     Condition = readActionCondition,
///     Obligations = [],
///     Advice = []
/// };
/// </code>
/// </example>
public sealed record Rule
{
    /// <summary>
    /// The unique identifier for this rule within its containing policy.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// An optional human-readable description of what this rule authorizes or denies.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The effect to return when this rule applies and its condition is satisfied.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §7.9 — Only <see cref="ABAC.Effect.Permit"/> and <see cref="ABAC.Effect.Deny"/>
    /// are valid rule effects. <see cref="ABAC.Effect.NotApplicable"/> and
    /// <see cref="ABAC.Effect.Indeterminate"/> are computed by the evaluation engine.
    /// </remarks>
    public required Effect Effect { get; init; }

    /// <summary>
    /// The optional target that determines whether this rule applies to the request.
    /// </summary>
    /// <remarks>
    /// A <c>null</c> target means the rule applies to all requests that reach it
    /// (the containing policy's target has already filtered applicability).
    /// </remarks>
    public Target? Target { get; init; }

    /// <summary>
    /// The optional condition that must evaluate to <c>true</c> for the rule's effect to apply.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §7.9 — A <c>null</c> condition means the rule is unconditional:
    /// if the target matches, the effect is returned without further evaluation.
    /// When present, the condition is an <see cref="Apply"/> expression tree that must
    /// evaluate to a boolean value.
    /// </remarks>
    public Apply? Condition { get; init; }

    /// <summary>
    /// Obligations to include in the response when this rule's effect matches the decision.
    /// </summary>
    public required IReadOnlyList<Obligation> Obligations { get; init; }

    /// <summary>
    /// Advice expressions to include in the response when this rule's effect matches the decision.
    /// </summary>
    public required IReadOnlyList<AdviceExpression> Advice { get; init; }
}
