namespace Encina.Security.ABAC.Enforcement;

/// <summary>
/// Assembles the evaluation trace of one decision record from the several Policy Decision Point calls
/// the requirement evaluator makes, plus one node per condition.
/// </summary>
/// <remarks>
/// The entry limit (<see cref="PolicyEvaluationContext.MaxTraceEntries"/>) covers the whole record:
/// each PDP call receives the budget that remains (<see cref="Remaining"/>), and the truncation flag
/// of every call is combined with the nodes this collector has to drop itself.
/// </remarks>
internal sealed class ABACRecordTrace
{
    private readonly List<PolicyEvaluationTrace> _nodes = [];

    public ABACRecordTrace(int maxEntries)
    {
        Remaining = Math.Max(0, maxEntries);
    }

    /// <summary>The number of nodes the next PDP call or leaf may still add.</summary>
    public int Remaining { get; private set; }

    /// <summary><c>true</c> when any PDP call or this collector dropped a node because the limit was reached.</summary>
    public bool Truncated { get; private set; }

    /// <summary>The collected nodes in declaration order.</summary>
    public IReadOnlyList<PolicyEvaluationTrace> Nodes => _nodes;

    /// <summary>Adds the nodes a PDP call recorded, charging them to the budget.</summary>
    public void AddDecision(PolicyDecision decision)
    {
        _nodes.AddRange(decision.EvaluatedPolicies);
        Remaining = Math.Max(0, Remaining - Count(decision.EvaluatedPolicies));
        Truncated |= decision.EvaluationTraceTruncated;
    }

    /// <summary>
    /// Adds a node for a requirement the Policy Decision Point did not trace: a condition, or a policy
    /// that was not found or could not be read. Dropped, and the trace marked truncated, when the limit is reached.
    /// </summary>
    public void AddLeaf(string id, Effect effect, PolicyTraceReason reason)
    {
        if (Remaining <= 0)
        {
            Truncated = true;
            return;
        }

        Remaining--;
        _nodes.Add(new PolicyEvaluationTrace
        {
            PolicyId = id,
            IsPolicySet = false,
            Effect = effect,
            Reason = reason
        });
    }

    private static int Count(IReadOnlyList<PolicyEvaluationTrace> nodes)
    {
        var total = 0;

        foreach (var node in nodes)
        {
            total += 1 + Count(node.Children);
        }

        return total;
    }
}
