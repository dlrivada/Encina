namespace Encina.Security.ABAC;

/// <summary>
/// One node of the evaluation trace of a decision: the result of evaluating one policy or policy set,
/// with the nodes of the policies and policy sets nested inside it.
/// </summary>
/// <remarks>
/// <para>
/// The trace is opt-in (<see cref="PolicyEvaluationContext.IncludeEvaluationTrace"/>) and describes
/// what the Policy Decision Point evaluated: identifiers, effects and reasons only. It never carries
/// attribute values, exception messages or status messages.
/// </para>
/// </remarks>
public sealed record PolicyEvaluationTrace
{
    /// <summary>The identifier of the evaluated policy or policy set.</summary>
    public required string PolicyId { get; init; }

    /// <summary><c>true</c> when the node is a policy set, <c>false</c> when it is a single policy.</summary>
    public required bool IsPolicySet { get; init; }

    /// <summary>The effect the policy or policy set produced.</summary>
    public required Effect Effect { get; init; }

    /// <summary>Why the node has this effect.</summary>
    public required PolicyTraceReason Reason { get; init; }

    /// <summary>
    /// The identifiers of the rules whose own effect equals <see cref="Effect"/>, in rule order.
    /// Empty for a policy set (its children carry the rules), for a policy that was not evaluated and
    /// for a <see cref="ABAC.Effect.NotApplicable"/> policy.
    /// </summary>
    public IReadOnlyList<string> DecisiveRuleIds { get; init; } = [];

    /// <summary>The nodes of the policies and policy sets nested inside a policy set, in evaluation order.</summary>
    public IReadOnlyList<PolicyEvaluationTrace> Children { get; init; } = [];

    /// <summary>The version of the policy or policy set, when it declares one.</summary>
    public string? Version { get; init; }
}
