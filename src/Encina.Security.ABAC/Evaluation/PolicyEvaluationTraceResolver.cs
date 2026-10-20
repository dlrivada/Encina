namespace Encina.Security.ABAC.Evaluation;

/// <summary>
/// Finds the representative decisive rule of a decision in its evaluation trace.
/// </summary>
internal static class PolicyEvaluationTraceResolver
{
    /// <summary>
    /// Follows the first node whose effect equals <paramref name="effect"/> (the rule
    /// <c>DenyOverridesAlgorithm.BuildCombinedResult</c> uses to pick the decision's policy id),
    /// descends into policy sets, and returns the first decisive rule of the policy it reaches.
    /// </summary>
    /// <param name="nodes">The trace nodes of one decision, in evaluation order.</param>
    /// <param name="effect">The effect of the decision.</param>
    /// <returns>
    /// The rule id, or <c>null</c> when the effect is <see cref="Effect.NotApplicable"/>, no node has
    /// that effect, or the matching policy has no decisive rule (for example a truncated trace).
    /// </returns>
    public static string? ResolveDecisiveRuleId(IReadOnlyList<PolicyEvaluationTrace> nodes, Effect effect)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        if (effect == Effect.NotApplicable)
        {
            return null;
        }

        var match = nodes.FirstOrDefault(node => node.Effect == effect);

        return match switch
        {
            null => null,
            { IsPolicySet: true } => ResolveDecisiveRuleId(match.Children, effect),
            _ => match.DecisiveRuleIds.Count > 0 ? match.DecisiveRuleIds[0] : null
        };
    }
}
