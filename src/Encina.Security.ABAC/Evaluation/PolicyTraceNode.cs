namespace Encina.Security.ABAC.Evaluation;

/// <summary>
/// Mutable builder of one <see cref="PolicyEvaluationTrace"/> node used while the Policy Decision Point
/// evaluates. The PDP creates a root only when the trace is requested; every call site works with a
/// nullable node, so a request without a trace allocates nothing.
/// </summary>
/// <remarks>
/// All nodes of one evaluation share a budget (<see cref="PolicyEvaluationContext.MaxTraceEntries"/>).
/// When the budget is spent <see cref="AddChild"/> returns <c>null</c>, the descendants of the missing
/// node are not recorded either, and <see cref="Truncated"/> becomes <c>true</c>.
/// </remarks>
internal sealed class PolicyTraceNode
{
    private readonly Budget _budget;
    private readonly List<PolicyTraceNode> _children = [];
    private readonly string _policyId;
    private readonly bool _isPolicySet;
    private readonly string? _version;

    private Effect _effect;
    private PolicyTraceReason _reason;
    private IReadOnlyList<string> _decisiveRuleIds = [];

    /// <summary>
    /// The number of nodes (roots included) created on the current thread. Tests use it to prove that a
    /// request without a trace creates none.
    /// </summary>
    [ThreadStatic]
    internal static int CreatedOnThisThread;

    private PolicyTraceNode(Budget budget, string policyId, bool isPolicySet, string? version)
    {
        CreatedOnThisThread++;
        _budget = budget;
        _policyId = policyId;
        _isPolicySet = isPolicySet;
        _version = version;
    }

    /// <summary>Whether a node was dropped because the budget was spent.</summary>
    public bool Truncated => _budget.Truncated;

    /// <summary>Creates the root that holds the top-level nodes; the root itself is never emitted.</summary>
    /// <param name="maxEntries">The number of nodes the whole evaluation may record.</param>
    public static PolicyTraceNode CreateRoot(int maxEntries) =>
        new(new Budget(Math.Max(0, maxEntries)), string.Empty, isPolicySet: true, version: null);

    /// <summary>Adds a child node, or returns <c>null</c> when the budget is spent.</summary>
    public PolicyTraceNode? AddChild(string policyId, bool isPolicySet, string? version)
    {
        if (!_budget.TryTake())
        {
            return null;
        }

        var child = new PolicyTraceNode(_budget, policyId, isPolicySet, version);
        _children.Add(child);
        return child;
    }

    /// <summary>Sets the outcome of the node once its policy or policy set has been evaluated.</summary>
    public void Complete(Effect effect, PolicyTraceReason reason, IReadOnlyList<string>? decisiveRuleIds = null)
    {
        _effect = effect;
        _reason = reason;
        _decisiveRuleIds = decisiveRuleIds ?? [];
    }

    /// <summary>Freezes the children of this node into immutable trace records.</summary>
    public IReadOnlyList<PolicyEvaluationTrace> ToTraces()
    {
        if (_children.Count == 0)
        {
            return [];
        }

        var traces = new PolicyEvaluationTrace[_children.Count];
        for (var i = 0; i < traces.Length; i++)
        {
            traces[i] = _children[i].ToTrace();
        }

        return traces;
    }

    private PolicyEvaluationTrace ToTrace() => new()
    {
        PolicyId = _policyId,
        IsPolicySet = _isPolicySet,
        Effect = _effect,
        Reason = _reason,
        DecisiveRuleIds = _decisiveRuleIds,
        Children = ToTraces(),
        Version = _version
    };

    private sealed class Budget(int remaining)
    {
        private int _remaining = remaining;

        public bool Truncated { get; private set; }

        public bool TryTake()
        {
            if (_remaining <= 0)
            {
                Truncated = true;
                return false;
            }

            _remaining--;
            return true;
        }
    }
}
