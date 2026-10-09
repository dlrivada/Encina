using System.Diagnostics;

using Encina.Diagnostics;
using Encina.Security.ABAC.CombiningAlgorithms;
using Encina.Security.ABAC.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.Logging;

namespace Encina.Security.ABAC.Evaluation;

/// <summary>
/// XACML 3.0 Policy Decision Point (PDP) — evaluates access requests against the full
/// policy hierarchy using recursive evaluation, combining algorithms, and obligation collection.
/// </summary>
/// <remarks>
/// <para>
/// Implements the complete XACML 3.0 evaluation algorithm (§7.12-7.14):
/// </para>
/// <list type="number">
/// <item><description>Retrieve all policy sets and standalone policies from the PAP; if either list
/// cannot be read, the decision is <see cref="Effect.Indeterminate"/> (never one made on part of the store)</description></item>
/// <item><description>Recursively evaluate each policy set (target → child policies/sets → combine)</description></item>
/// <item><description>Evaluate each policy (target → rules → combine → collect obligations/advice)</description></item>
/// <item><description>Evaluate each rule (target → condition → effect)</description></item>
/// <item><description>Combine all results at the root level with DenyOverrides</description></item>
/// <item><description>Filter obligations and advice based on the final decision effect</description></item>
/// </list>
/// <para>
/// The PDP never throws exceptions for policy evaluation failures. Instead, evaluation
/// errors produce <see cref="Effect.Indeterminate"/> with a <see cref="DecisionStatus"/>
/// describing the problem.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var pdp = new XACMLPolicyDecisionPoint(pap, targetEvaluator, conditionEvaluator, algorithmFactory, logger);
/// var decision = await pdp.EvaluateAsync(context);
/// if (decision.Effect == Effect.Permit) { /* allow */ }
/// </code>
/// </example>
public sealed class XACMLPolicyDecisionPoint(
    IPolicyAdministrationPoint pap,
    TargetEvaluator targetEvaluator,
    ConditionEvaluator conditionEvaluator,
    CombiningAlgorithmFactory algorithmFactory,
    ILogger<XACMLPolicyDecisionPoint> logger) : IPolicyDecisionPoint
{
    private readonly IPolicyAdministrationPoint _pap = pap
        ?? throw new ArgumentNullException(nameof(pap));

    private readonly TargetEvaluator _targetEvaluator = targetEvaluator
        ?? throw new ArgumentNullException(nameof(targetEvaluator));

    private readonly ConditionEvaluator _conditionEvaluator = conditionEvaluator
        ?? throw new ArgumentNullException(nameof(conditionEvaluator));

    private readonly CombiningAlgorithmFactory _algorithmFactory = algorithmFactory
        ?? throw new ArgumentNullException(nameof(algorithmFactory));

    private readonly ILogger<XACMLPolicyDecisionPoint> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    private const string PolicySetsSource = "policy sets";
    private const string StandalonePoliciesSource = "standalone policies";

    // Fixed text: neither a store error message nor an exception message reaches the decision.
    private const string StoreFailureStatusMessage =
        "The policy store could not be read in full. No decision is made on part of the policies.";

    // Root combining algorithm for top-level results
    private readonly ICombiningAlgorithm _rootAlgorithm = algorithmFactory.GetAlgorithm(CombiningAlgorithmId.DenyOverrides);

    /// <inheritdoc />
    public async ValueTask<PolicyDecision> EvaluateAsync(
        PolicyEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var stopwatch = Stopwatch.StartNew();
        var trace = CreateTraceRoot(context);
        PolicyEvaluationResult? combinedResult;

        try
        {
            combinedResult = await EvaluateWholeStoreAsync(context, trace, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            ABACLogMessages.StoreEvaluationFailed(_logger, ex.ForLogging());
            combinedResult = null;
        }

        stopwatch.Stop();

        // A store that could not be read in full is Indeterminate: deciding on the policies that
        // did load could miss a Deny among the ones that did not (#1676).
        return combinedResult is null
            ? StoreFailureDecision(stopwatch.Elapsed)
            : BuildDecision(combinedResult, context, stopwatch.Elapsed, trace);
    }

    /// <summary>
    /// Creates the root of the evaluation trace, or <c>null</c> when the context does not ask for one
    /// (a request without a trace allocates nothing for it).
    /// </summary>
    private static PolicyTraceNode? CreateTraceRoot(PolicyEvaluationContext context) =>
        context.IncludeEvaluationTrace ? PolicyTraceNode.CreateRoot(context.MaxTraceEntries) : null;

    /// <summary>
    /// Reads every top-level policy set and every standalone policy, evaluates them and combines
    /// the results at the root with DenyOverrides. Returns <c>null</c> when either list could not
    /// be read: the decision is then Indeterminate, never one made on part of the store.
    /// </summary>
    private async ValueTask<PolicyEvaluationResult?> EvaluateWholeStoreAsync(
        PolicyEvaluationContext context,
        PolicyTraceNode? trace,
        CancellationToken cancellationToken)
    {
        var policySetsResult = await _pap.GetPolicySetsAsync(cancellationToken).ConfigureAwait(false);
        var policySets = policySetsResult.MatchUnsafe(
            Right: sets => sets,
            Left: error => RetrievalFailed<PolicySet>(PolicySetsSource, error));

        if (policySets is null)
        {
            return null;
        }

        var policiesResult = await _pap.GetPoliciesAsync(null, cancellationToken).ConfigureAwait(false);
        var policies = policiesResult.MatchUnsafe(
            Right: standalone => standalone,
            Left: error => RetrievalFailed<Policy>(StandalonePoliciesSource, error));

        if (policies is null)
        {
            return null;
        }

        var allResults = new List<PolicyEvaluationResult>(policySets.Count + policies.Count);
        allResults.AddRange(policySets.Select(policySet => EvaluatePolicySet(policySet, context, trace)));
        allResults.AddRange(policies.Select(policy => EvaluatePolicy(policy, context, trace)));

        return allResults.Count == 0
            ? NotApplicableResult(string.Empty)
            : _rootAlgorithm.CombinePolicyResults(allResults);
    }

    private IReadOnlyList<T>? RetrievalFailed<T>(string source, EncinaError error)
    {
        ABACLogMessages.PolicyRetrievalFailed(_logger, source, error.GetCode().IfNone("encina.unknown"));
        return null;
    }

    private static PolicyDecision StoreFailureDecision(TimeSpan evaluationDuration) => new()
    {
        Effect = Effect.Indeterminate,
        Status = new DecisionStatus
        {
            StatusCode = "processing-error",
            StatusMessage = StoreFailureStatusMessage
        },
        Obligations = [],
        Advice = [],
        EvaluationDuration = evaluationDuration
    };

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, PolicyDecision>> EvaluatePolicyAsync(
        string policyId,
        PolicyEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);
        ArgumentNullException.ThrowIfNull(context);

        var stopwatch = Stopwatch.StartNew();
        var trace = CreateTraceRoot(context);
        Option<PolicyEvaluationResult> result;

        try
        {
            result = await FindAndEvaluateAsync(policyId, context, trace, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            ABACLogMessages.RequiredPolicyEvaluationFailed(_logger, ex.ForLogging(), policyId);
            result = IndeterminateResult(policyId);
        }

        stopwatch.Stop();

        return result.Match<Either<EncinaError, PolicyDecision>>(
            Some: evaluated => BuildDecision(evaluated, context, stopwatch.Elapsed, trace),
            None: () => ABACErrors.PolicyNotFound(policyId));
    }

    /// <summary>
    /// Looks up <paramref name="policyId"/> among the top-level policy sets, then among the
    /// standalone policies (the same two lists <see cref="EvaluateAsync"/> combines at the root),
    /// and evaluates the first match. A policy or policy set nested inside a policy set is never
    /// matched: evaluating it alone would skip its parent's enabled flag, target, combining
    /// algorithm and obligations. A failed store read yields an Indeterminate result;
    /// <c>None</c> means not found.
    /// </summary>
    private async ValueTask<Option<PolicyEvaluationResult>> FindAndEvaluateAsync(
        string policyId,
        PolicyEvaluationContext context,
        PolicyTraceNode? trace,
        CancellationToken cancellationToken)
    {
        var policySets = await _pap.GetPolicySetsAsync(cancellationToken).ConfigureAwait(false);
        var fromPolicySet = policySets.Match(
            Left: error => LookupFailed(policyId, error),
            Right: sets => FindById(sets, policyId, set => set.Id).Map(found => EvaluatePolicySet(found, context, trace)));

        if (fromPolicySet.IsSome)
        {
            return fromPolicySet;
        }

        var standalonePolicies = await _pap.GetPoliciesAsync(null, cancellationToken).ConfigureAwait(false);
        return standalonePolicies.Match(
            Left: error => LookupFailed(policyId, error),
            Right: policies => FindById(policies, policyId, policy => policy.Id).Map(found => EvaluatePolicy(found, context, trace)));
    }

    private static Option<T> FindById<T>(IReadOnlyList<T> items, string id, Func<T, string> idOf)
        where T : class
    {
        var found = items.FirstOrDefault(item => string.Equals(idOf(item), id, StringComparison.Ordinal));
        return found is null ? Option<T>.None : Option<T>.Some(found);
    }

    private Option<PolicyEvaluationResult> LookupFailed(string policyId, EncinaError error)
    {
        ABACLogMessages.RequiredPolicyLookupFailed(_logger, policyId, error.GetCode().IfNone("encina.unknown"));
        return IndeterminateResult(policyId);
    }

    /// <summary>
    /// Evaluates a <see cref="PolicySet"/> recursively against the given context.
    /// </summary>
    private PolicyEvaluationResult EvaluatePolicySet(
        PolicySet policySet,
        PolicyEvaluationContext context,
        PolicyTraceNode? parentTrace)
    {
        var node = parentTrace?.AddChild(policySet.Id, isPolicySet: true, policySet.Version);
        var (result, reason) = EvaluatePolicySetCore(policySet, context, node);
        node?.Complete(result.Effect, reason);
        return result;
    }

    private (PolicyEvaluationResult Result, PolicyTraceReason Reason) EvaluatePolicySetCore(
        PolicySet policySet,
        PolicyEvaluationContext context,
        PolicyTraceNode? node)
    {
        // Disabled policy sets and policy sets whose target does not match are not applicable
        var gate = EvaluateGate(policySet.IsEnabled, policySet.Target, context);
        if (gate != PolicyTraceReason.Evaluated)
        {
            return (GateResult(policySet.Id, gate), gate);
        }

        // Evaluate child policies and policy sets
        var childResults = new List<PolicyEvaluationResult>();

        foreach (var childPolicy in policySet.Policies)
        {
            childResults.Add(EvaluatePolicy(childPolicy, context, node));
        }

        foreach (var childPolicySet in policySet.PolicySets)
        {
            childResults.Add(EvaluatePolicySet(childPolicySet, context, node));
        }

        // Combine child results
        var algorithm = _algorithmFactory.GetAlgorithm(policySet.Algorithm);
        var combinedResult = childResults.Count > 0
            ? algorithm.CombinePolicyResults(childResults)
            : NotApplicableResult(policySet.Id);

        // Add policy-set-level obligations and advice matching the combined effect
        var obligations = new List<Obligation>(combinedResult.Obligations);
        var advice = new List<AdviceExpression>(combinedResult.Advice);

        CollectObligations(policySet.Obligations, combinedResult.Effect, obligations);
        CollectAdvice(policySet.Advice, combinedResult.Effect, advice);

        var evaluated = new PolicyEvaluationResult
        {
            Effect = combinedResult.Effect,
            PolicyId = policySet.Id,
            Obligations = obligations,
            Advice = advice
        };

        return (evaluated, PolicyTraceReason.Evaluated);
    }

    /// <summary>
    /// Decides whether a policy or policy set is evaluated at all: <see cref="PolicyTraceReason.Evaluated"/>
    /// when it is enabled and its target matches, otherwise the reason it is not.
    /// </summary>
    private PolicyTraceReason EvaluateGate(bool isEnabled, Target? target, PolicyEvaluationContext context)
    {
        if (!isEnabled)
        {
            return PolicyTraceReason.Disabled;
        }

        return _targetEvaluator.EvaluateTarget(target, context) switch
        {
            Effect.NotApplicable => PolicyTraceReason.TargetNotMatched,
            Effect.Indeterminate => PolicyTraceReason.TargetIndeterminate,
            _ => PolicyTraceReason.Evaluated
        };
    }

    private static PolicyEvaluationResult GateResult(string policyId, PolicyTraceReason gate) =>
        gate == PolicyTraceReason.TargetIndeterminate
            ? IndeterminateResult(policyId)
            : NotApplicableResult(policyId);

    /// <summary>
    /// Evaluates a <see cref="Policy"/> against the given context by evaluating
    /// its rules and combining them with the policy's algorithm.
    /// </summary>
    private PolicyEvaluationResult EvaluatePolicy(
        Policy policy,
        PolicyEvaluationContext context,
        PolicyTraceNode? parentTrace)
    {
        var node = parentTrace?.AddChild(policy.Id, isPolicySet: false, policy.Version);
        var (result, reason, decisiveRuleIds) = EvaluatePolicyCore(policy, context, node is not null);
        node?.Complete(result.Effect, reason, decisiveRuleIds);
        return result;
    }

    private (PolicyEvaluationResult Result, PolicyTraceReason Reason, IReadOnlyList<string>? DecisiveRuleIds) EvaluatePolicyCore(
        Policy policy,
        PolicyEvaluationContext context,
        bool traced)
    {
        // Disabled policies and policies whose target does not match are not applicable
        var gate = EvaluateGate(policy.IsEnabled, policy.Target, context);
        if (gate != PolicyTraceReason.Evaluated)
        {
            return (GateResult(policy.Id, gate), gate, null);
        }

        // Build variable dictionary for condition evaluation
        var variables = BuildVariableDictionary(policy.VariableDefinitions);

        // Evaluate each rule
        var ruleResults = new List<RuleEvaluationResult>(policy.Rules.Count);
        foreach (var rule in policy.Rules)
        {
            ruleResults.Add(EvaluateRule(rule, context, variables));
        }

        // Combine rule effects
        var algorithm = _algorithmFactory.GetAlgorithm(policy.Algorithm);
        var combinedEffect = ruleResults.Count > 0
            ? algorithm.CombineRuleResults(ruleResults)
            : Effect.NotApplicable;

        // Collect rule-level obligations/advice matching the combined effect
        var obligations = new List<Obligation>();
        var advice = new List<AdviceExpression>();

        foreach (var ruleResult in ruleResults)
        {
            if (ruleResult.Effect == combinedEffect)
            {
                obligations.AddRange(ruleResult.Obligations);
                advice.AddRange(ruleResult.Advice);
            }
        }

        // Add policy-level obligations/advice matching the combined effect
        CollectObligations(policy.Obligations, combinedEffect, obligations);
        CollectAdvice(policy.Advice, combinedEffect, advice);

        var evaluated = new PolicyEvaluationResult
        {
            Effect = combinedEffect,
            PolicyId = policy.Id,
            Obligations = obligations,
            Advice = advice
        };

        return (evaluated, PolicyTraceReason.Evaluated, traced ? DecisiveRuleIds(ruleResults, combinedEffect) : null);
    }

    /// <summary>
    /// The identifiers of the rules whose own effect equals the policy's combined effect, in rule
    /// order; empty when the policy is not applicable.
    /// </summary>
    private static List<string> DecisiveRuleIds(IReadOnlyList<RuleEvaluationResult> ruleResults, Effect combinedEffect)
    {
        if (combinedEffect == Effect.NotApplicable)
        {
            return [];
        }

        return ruleResults
            .Where(ruleResult => ruleResult.Effect == combinedEffect)
            .Select(ruleResult => ruleResult.Rule.Id)
            .ToList();
    }

    /// <summary>
    /// Evaluates a single <see cref="Rule"/> against the given context.
    /// </summary>
    private RuleEvaluationResult EvaluateRule(
        Rule rule,
        PolicyEvaluationContext context,
        IReadOnlyDictionary<string, VariableDefinition>? variables)
    {
        // Evaluate target
        var targetResult = _targetEvaluator.EvaluateTarget(rule.Target, context);
        if (targetResult == Effect.NotApplicable)
        {
            return MakeRuleResult(rule, Effect.NotApplicable);
        }

        if (targetResult == Effect.Indeterminate)
        {
            return MakeRuleResult(rule, Effect.Indeterminate);
        }

        // No condition = unconditional rule
        if (rule.Condition is null)
        {
            return MakeRuleResult(rule, rule.Effect);
        }

        // Evaluate condition
        var conditionResult = _conditionEvaluator.Evaluate(rule.Condition, context, variables);

        return conditionResult.Match(
            Left: _ => MakeRuleResult(rule, Effect.Indeterminate),
            Right: value =>
            {
                if (value is true)
                {
                    return MakeRuleResult(rule, rule.Effect);
                }

                if (value is false)
                {
                    return MakeRuleResult(rule, Effect.NotApplicable);
                }

                // Non-boolean result → indeterminate
                return MakeRuleResult(rule, Effect.Indeterminate);
            });
    }

    /// <summary>
    /// Creates a <see cref="RuleEvaluationResult"/> for the given rule and effect,
    /// including obligations and advice only when the effect matches the rule's declared effect.
    /// </summary>
    private static RuleEvaluationResult MakeRuleResult(Rule rule, Effect effect)
    {
        // Only include obligations/advice when the rule's effect is actually applied
        var includeExtras = effect == rule.Effect;

        return new RuleEvaluationResult
        {
            Rule = rule,
            Effect = effect,
            Obligations = includeExtras ? rule.Obligations : [],
            Advice = includeExtras ? rule.Advice : []
        };
    }

    /// <summary>
    /// Builds the final <see cref="PolicyDecision"/> from the combined evaluation result,
    /// filtering obligations and advice based on the decision effect.
    /// </summary>
    private static PolicyDecision BuildDecision(
        PolicyEvaluationResult combinedResult,
        PolicyEvaluationContext context,
        TimeSpan evaluationDuration,
        PolicyTraceNode? trace)
    {
        // Filter obligations: only those whose FulfillOn matches the final effect
        var obligations = FilterObligations(combinedResult.Obligations, combinedResult.Effect);

        // Filter advice: only those whose AppliesTo matches the final effect (if requested)
        var advice = context.IncludeAdvice
            ? FilterAdvice(combinedResult.Advice, combinedResult.Effect)
            : (IReadOnlyList<AdviceExpression>)[];

        var decision = new PolicyDecision
        {
            Effect = combinedResult.Effect,
            PolicyId = string.IsNullOrEmpty(combinedResult.PolicyId) ? null : combinedResult.PolicyId,
            Obligations = obligations,
            Advice = advice,
            EvaluationDuration = evaluationDuration,
            Status = combinedResult.Effect == Effect.Indeterminate
                ? new DecisionStatus
                {
                    StatusCode = "processing-error",
                    StatusMessage = "Policy evaluation produced an indeterminate result."
                }
                : null
        };

        return trace is null ? decision : WithTrace(decision, trace);
    }

    /// <summary>
    /// Adds the evaluation trace and the representative decisive rule id to a decision.
    /// </summary>
    private static PolicyDecision WithTrace(PolicyDecision decision, PolicyTraceNode trace)
    {
        var evaluatedPolicies = trace.ToTraces();

        return decision with
        {
            EvaluatedPolicies = evaluatedPolicies,
            RuleId = PolicyEvaluationTraceResolver.ResolveDecisiveRuleId(evaluatedPolicies, decision.Effect),
            EvaluationTraceTruncated = trace.Truncated
        };
    }

    /// <summary>
    /// Filters obligations to include only those whose <see cref="Obligation.FulfillOn"/>
    /// matches the decision effect.
    /// </summary>
    private static List<Obligation> FilterObligations(
        IReadOnlyList<Obligation> obligations,
        Effect effect)
    {
        if (obligations.Count == 0)
        {
            return [];
        }

        var fulfillOn = effect switch
        {
            Effect.Permit => FulfillOn.Permit,
            Effect.Deny => FulfillOn.Deny,
            _ => (FulfillOn?)null
        };

        if (fulfillOn is null)
        {
            return [];
        }

        return obligations.Where(o => o.FulfillOn == fulfillOn.Value).ToList();
    }

    /// <summary>
    /// Filters advice to include only those whose <see cref="AdviceExpression.AppliesTo"/>
    /// matches the decision effect.
    /// </summary>
    private static List<AdviceExpression> FilterAdvice(
        IReadOnlyList<AdviceExpression> advice,
        Effect effect)
    {
        if (advice.Count == 0)
        {
            return [];
        }

        var appliesTo = effect switch
        {
            Effect.Permit => FulfillOn.Permit,
            Effect.Deny => FulfillOn.Deny,
            _ => (FulfillOn?)null
        };

        if (appliesTo is null)
        {
            return [];
        }

        return advice.Where(a => a.AppliesTo == appliesTo.Value).ToList();
    }

    /// <summary>
    /// Collects obligations from a source list into a target list, filtering by the effect.
    /// </summary>
    private static void CollectObligations(
        IReadOnlyList<Obligation> source,
        Effect effect,
        List<Obligation> target)
    {
        foreach (var obligation in source)
        {
            var fulfillOn = effect switch
            {
                Effect.Permit => FulfillOn.Permit,
                Effect.Deny => FulfillOn.Deny,
                _ => (FulfillOn?)null
            };

            if (fulfillOn.HasValue && obligation.FulfillOn == fulfillOn.Value)
            {
                target.Add(obligation);
            }
        }
    }

    /// <summary>
    /// Collects advice from a source list into a target list, filtering by the effect.
    /// </summary>
    private static void CollectAdvice(
        IReadOnlyList<AdviceExpression> source,
        Effect effect,
        List<AdviceExpression> target)
    {
        foreach (var adviceExpr in source)
        {
            var appliesTo = effect switch
            {
                Effect.Permit => FulfillOn.Permit,
                Effect.Deny => FulfillOn.Deny,
                _ => (FulfillOn?)null
            };

            if (appliesTo.HasValue && adviceExpr.AppliesTo == appliesTo.Value)
            {
                target.Add(adviceExpr);
            }
        }
    }

    /// <summary>
    /// Builds a variable dictionary from the policy's variable definitions.
    /// </summary>
    private static Dictionary<string, VariableDefinition>? BuildVariableDictionary(
        IReadOnlyList<VariableDefinition> variableDefinitions)
    {
        if (variableDefinitions.Count == 0)
        {
            return null;
        }

        var dict = new Dictionary<string, VariableDefinition>(variableDefinitions.Count);
        foreach (var varDef in variableDefinitions)
        {
            dict[varDef.VariableId] = varDef;
        }

        return dict;
    }

    private static PolicyEvaluationResult NotApplicableResult(string policyId) =>
        new()
        {
            Effect = Effect.NotApplicable,
            PolicyId = policyId,
            Obligations = [],
            Advice = []
        };

    private static PolicyEvaluationResult IndeterminateResult(string policyId) =>
        new()
        {
            Effect = Effect.Indeterminate,
            PolicyId = policyId,
            Obligations = [],
            Advice = []
        };
}
