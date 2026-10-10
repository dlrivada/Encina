using System.Diagnostics;
using System.Dynamic;

using Encina.Diagnostics;
using Encina.Security.ABAC.Diagnostics;
using Encina.Security.ABAC.EEL;

using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;

namespace Encina.Security.ABAC.Enforcement;

/// <summary>
/// The attributes the Policy Enforcement Point collected for one request: the raw dictionaries
/// (used to build <see cref="EELGlobals"/>) and the XACML context built from them.
/// </summary>
internal sealed record ABACCollectedAttributes(
    IReadOnlyDictionary<string, object> Subject,
    IReadOnlyDictionary<string, object> Resource,
    IReadOnlyDictionary<string, object> Environment,
    PolicyEvaluationContext Context);

/// <summary>
/// The decision the Policy Enforcement Point enforces for one request, plus the specific error
/// to return when the decision is a denial that is not an ordinary policy Deny.
/// </summary>
internal sealed record ABACRequirementVerdict(PolicyDecision Decision, EncinaError? DenyError)
{
    /// <summary>
    /// The deciding policy: the first policy in declaration order that decided the verdict, the missing
    /// policy name, or <c>condition:&lt;index&gt;</c> for a condition; never the expression text.
    /// </summary>
    public string? DecidingPolicyId { get; init; }

    /// <summary>The representative decisive rule of <see cref="DecidingPolicyId"/>, when traced.</summary>
    public string? DecidingRuleId { get; init; }

    /// <summary>The evaluation trace of the whole verdict, empty when the context did not ask for one.</summary>
    public IReadOnlyList<PolicyEvaluationTrace> Trace { get; init; } = [];

    /// <summary>Whether the trace dropped nodes because the entry limit was reached.</summary>
    public bool TraceTruncated { get; init; }
}

/// <summary>
/// Evaluates the <see cref="RequirePolicyAttribute"/> and <see cref="RequireConditionAttribute"/>
/// requirements of a request and combines them with <see cref="ABACRequirementCombiner"/> (#1634).
/// </summary>
/// <remarks>
/// Each named policy is evaluated on its own through
/// <see cref="IPolicyDecisionPoint.EvaluatePolicyAsync"/>; conditions are evaluated only when the
/// policies permit, in declaration order, with the cached delegate of <see cref="EELCompiler"/>.
/// No error or exception message is logged or returned: codes and exception types only.
/// </remarks>
internal sealed class ABACRequirementEvaluator
{
    private const string IndeterminateReason = "A required policy or condition could not be evaluated.";
    private const string PolicyDeniedReason = "A required policy did not permit the request.";
    private const string NotFoundReason = "A required policy was not found.";
    private const string ConditionReason = "A required condition was not met.";

    private readonly IPolicyDecisionPoint _pdp;
    private readonly EELCompiler _compiler;
    private readonly ILogger _logger;

    public ABACRequirementEvaluator(IPolicyDecisionPoint pdp, EELCompiler compiler, ILogger logger)
    {
        _pdp = pdp;
        _compiler = compiler;
        _logger = logger;
    }

    /// <summary>Evaluates every requirement of <paramref name="info"/> and returns the verdict to enforce.</summary>
    public async ValueTask<ABACRequirementVerdict> EvaluateAsync(
        ABACAttributeInfo info,
        ABACCollectedAttributes attributes,
        Type requestType,
        CancellationToken cancellationToken)
    {
        var startTimestamp = Stopwatch.GetTimestamp();

        // One trace for the whole record: each PDP call gets the budget that remains.
        var trace = attributes.Context.IncludeEvaluationTrace
            ? new ABACRecordTrace(attributes.Context.MaxTraceEntries)
            : null;

        var policies = await EvaluatePoliciesAsync(info.PolicyAttributes, attributes.Context, requestType, trace, cancellationToken)
            .ConfigureAwait(false);

        var policyOutcomes = policies.ConvertAll(policy => policy.Outcome);

        // Conditions run only when the policies pass; otherwise the policies decide on their own.
        List<ConditionOutcome> conditionOutcomes = ABACRequirementCombiner.CombinePolicies(policyOutcomes) == RequirementVerdictKind.Permit
            ? await EvaluateConditionsAsync(info.ConditionAttributes, attributes, requestType, cancellationToken)
                .ConfigureAwait(false)
            : [];

        AddConditionNodes(trace, conditionOutcomes, info.ConditionAttributes.Count);

        // The verdict comes from the same rule the property tests check (#1634).
        var kind = ABACRequirementCombiner.Combine(policyOutcomes, conditionOutcomes);

        // EvaluateConditionsAsync stops at the first condition that is not true, so it is the last one.
        var verdict = BuildVerdict(kind, policies, conditionOutcomes.Count - 1, requestType, Stopwatch.GetElapsedTime(startTimestamp));

        return trace is null
            ? verdict
            : verdict with { Trace = trace.Nodes, TraceTruncated = trace.Truncated };
    }

    // ── Required policies ───────────────────────────────────────────

    private async ValueTask<List<EvaluatedPolicy>> EvaluatePoliciesAsync(
        IReadOnlyList<RequirePolicyAttribute> required,
        PolicyEvaluationContext context,
        Type requestType,
        ABACRecordTrace? trace,
        CancellationToken cancellationToken)
    {
        var results = new List<EvaluatedPolicy>(required.Count);

        foreach (var attribute in required)
        {
            // The attribute constructor does not reject null; a null name is a missing policy.
            var policyName = attribute.PolicyName ?? string.Empty;
            var decision = await EvaluatePolicyAsync(policyName, context, requestType, trace, cancellationToken)
                .ConfigureAwait(false);

            results.Add(new EvaluatedPolicy(
                new RequiredPolicyOutcome(policyName, attribute.AllMustPass, decision?.Effect),
                decision));
        }

        return results;
    }

    private async ValueTask<PolicyDecision?> EvaluatePolicyAsync(
        string policyName,
        PolicyEvaluationContext context,
        Type requestType,
        ABACRecordTrace? trace,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(policyName))
        {
            return NotFound(policyName, requestType, trace);
        }

        // The PDP traces within what is left of the record's budget.
        var callContext = trace is null ? context : context with { MaxTraceEntries = trace.Remaining };
        var result = await _pdp.EvaluatePolicyAsync(policyName, callContext, cancellationToken).ConfigureAwait(false);

        // MatchUnsafe: a missing policy is represented by null.
        return result.MatchUnsafe(
            Right: decision => Traced(decision, trace),
            Left: error => error.GetCode().IfNone(string.Empty) == ABACErrors.PolicyNotFoundCode
                ? NotFound(policyName, requestType, trace)
                : Unreadable(policyName, trace));
    }

    private static PolicyDecision Traced(PolicyDecision decision, ABACRecordTrace? trace)
    {
        trace?.AddDecision(decision);
        return decision;
    }

    private static PolicyDecision Unreadable(string policyName, ABACRecordTrace? trace)
    {
        trace?.AddLeaf(policyName, Effect.Indeterminate, PolicyTraceReason.NotEvaluated);
        return Decision(Effect.Indeterminate, policyName, [], [], IndeterminateReason, TimeSpan.Zero);
    }

    // ── Condition trace nodes ───────────────────────────────────────

    internal static string ConditionTraceId(int index) => $"condition:{index}";

    // One node per declared condition, in declaration order: the evaluated ones with their outcome,
    // then the ones skipped because an earlier requirement already decided (short-circuit).
    private static void AddConditionNodes(ABACRecordTrace? trace, List<ConditionOutcome> evaluated, int declared)
    {
        if (trace is null)
        {
            return;
        }

        for (var index = 0; index < declared; index++)
        {
            if (index < evaluated.Count)
            {
                trace.AddLeaf(ConditionTraceId(index), ConditionEffect(evaluated[index]), PolicyTraceReason.Evaluated);
            }
            else
            {
                trace.AddLeaf(ConditionTraceId(index), Effect.NotApplicable, PolicyTraceReason.NotEvaluated);
            }
        }
    }

    // crap-exempt: single-question switch — the effect each condition outcome stands for in the trace.
    private static Effect ConditionEffect(ConditionOutcome outcome) => outcome switch
    {
        ConditionOutcome.True => Effect.Permit,
        ConditionOutcome.False => Effect.Deny,
        _ => Effect.Indeterminate
    };

    private PolicyDecision? NotFound(string policyName, Type requestType, ABACRecordTrace? trace)
    {
        ABACLogMessages.RequiredPolicyNotFound(_logger, policyName, requestType.Name);
        trace?.AddLeaf(policyName, Effect.NotApplicable, PolicyTraceReason.NotEvaluated);
        return null;
    }

    // ── Required conditions ─────────────────────────────────────────

    private async ValueTask<List<ConditionOutcome>> EvaluateConditionsAsync(
        IReadOnlyList<RequireConditionAttribute> conditions,
        ABACCollectedAttributes attributes,
        Type requestType,
        CancellationToken cancellationToken)
    {
        if (conditions.Count == 0)
        {
            return [];
        }

        var globals = CreateGlobals(attributes, requestType);
        var outcomes = new List<ConditionOutcome>(conditions.Count);

        // AND in declaration order: the first condition that is not true decides, so stop there.
        for (var index = 0; index < conditions.Count; index++)
        {
            var outcome = await EvaluateConditionAsync(
                conditions[index].Expression, globals, index, requestType, cancellationToken).ConfigureAwait(false);
            outcomes.Add(outcome);

            if (outcome != ConditionOutcome.True)
            {
                break;
            }
        }

        return outcomes;
    }

    private async ValueTask<ConditionOutcome> EvaluateConditionAsync(
        string expression,
        EELGlobals globals,
        int index,
        Type requestType,
        CancellationToken cancellationToken)
    {
        try
        {
            var runner = await CompileAsync(expression, index, requestType, cancellationToken).ConfigureAwait(false);
            if (runner is null)
            {
                return ConditionOutcome.Error;
            }

            if (await runner(globals, cancellationToken).ConfigureAwait(false))
            {
                return ConditionOutcome.True;
            }

            ABACLogMessages.ConditionNotMet(_logger, index, requestType.Name);
            return ConditionOutcome.False;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            ABACLogMessages.ConditionEvaluationFailed(_logger, ex.ForLogging(), index, requestType.Name);
            return ConditionOutcome.Error;
        }
    }

    private async ValueTask<ScriptRunner<bool>?> CompileAsync(
        string expression,
        int index,
        Type requestType,
        CancellationToken cancellationToken)
    {
        // A cache hit when EELExpressionPrecompilationService compiled the expression at startup.
        var compiled = await _compiler.CompileAsync(expression, cancellationToken).ConfigureAwait(false);

        // MatchUnsafe: an expression that does not compile is represented by null.
        return compiled.MatchUnsafe<ScriptRunner<bool>?>(
            Right: runner => runner,
            Left: error =>
            {
                ABACLogMessages.ConditionCompilationFailed(
                    _logger, index, requestType.Name, error.GetCode().IfNone("encina.unknown"));
                return null;
            });
    }

    internal static EELGlobals CreateGlobals(ABACCollectedAttributes attributes, Type requestType) => new()
    {
        user = ToExpando(attributes.Subject),
        resource = ToExpando(attributes.Resource),
        environment = ToExpando(attributes.Environment),
        action = ToExpando(new Dictionary<string, object> { [AttributeContextBuilder.ActionNameAttributeId] = requestType.Name })
    };

    private static ExpandoObject ToExpando(IReadOnlyDictionary<string, object> values)
    {
        var expando = new ExpandoObject();
        var members = (IDictionary<string, object?>)expando;

        foreach (var (name, value) in values)
        {
            members[name] = value;
        }

        return expando;
    }

    // ── Verdict ─────────────────────────────────────────────────────

    private static ABACRequirementVerdict BuildVerdict(
        RequirementVerdictKind kind,
        List<EvaluatedPolicy> policies,
        int failedCondition,
        Type requestType,
        TimeSpan elapsed)
    {
        var verdict = VerdictFor(kind, policies, failedCondition, requestType, elapsed);
        var (policyId, ruleId) = DecidingPolicy(kind, policies, failedCondition);

        return verdict with { DecidingPolicyId = policyId, DecidingRuleId = ruleId };
    }

    // The policy a record names as the decider: the first in declaration order (the full list of
    // deciding policies is in the trace). A condition is named by its declaration index, never its text.
    private static (string? PolicyId, string? RuleId) DecidingPolicy(
        RequirementVerdictKind kind,
        List<EvaluatedPolicy> policies,
        int failedCondition)
    {
        if (kind == RequirementVerdictKind.ConditionNotMet)
        {
            return (ConditionTraceId(failedCondition), null);
        }

        var deciding = FirstDeciding(kind, policies);

        return deciding is null
            ? (ConditionFallback(kind, failedCondition), null)
            : (deciding.Outcome.PolicyId, deciding.Decision?.RuleId);
    }

    // An Indeterminate verdict without a policy comes from a condition that failed to run.
    private static string? ConditionFallback(RequirementVerdictKind kind, int failedCondition) =>
        kind == RequirementVerdictKind.Indeterminate && failedCondition >= 0
            ? ConditionTraceId(failedCondition)
            : null;

    // crap-exempt: single-question switch — the first policy, in declaration order, that decided each kind of verdict.
    private static EvaluatedPolicy? FirstDeciding(RequirementVerdictKind kind, List<EvaluatedPolicy> policies) => kind switch
    {
        RequirementVerdictKind.Permit => policies.Find(policy => policy.Outcome.Effect == Effect.Permit),
        RequirementVerdictKind.PolicyDenied =>
            policies.Find(policy => policy.Outcome.AllMustPass && policy.Outcome.Effect is Effect.Deny or Effect.NotApplicable)
            ?? policies.Find(policy => policy.Outcome.Effect is Effect.Deny or Effect.NotApplicable),
        RequirementVerdictKind.PolicyNotFound => policies.Find(policy => policy.Decision is null),
        _ => policies.Find(policy => policy.Outcome.AllMustPass && policy.Outcome.Effect == Effect.Indeterminate)
            ?? policies.Find(policy => policy.Outcome.Effect == Effect.Indeterminate)
    };

    private static ABACRequirementVerdict VerdictFor(
        RequirementVerdictKind kind,
        List<EvaluatedPolicy> policies,
        int failedCondition,
        Type requestType,
        TimeSpan elapsed) => kind switch
        {
            RequirementVerdictKind.Permit => new(
                FromPolicies(Effect.Permit, policies, null, elapsed), null),

            RequirementVerdictKind.PolicyDenied => new(
                FromPolicies(Effect.Deny, policies, PolicyDeniedReason, elapsed), null),

            RequirementVerdictKind.PolicyNotFound => NotFoundVerdict(policies, requestType, elapsed),

            RequirementVerdictKind.ConditionNotMet => new(
                Decision(Effect.Deny, null, [], [], ConditionReason, elapsed),
                ABACErrors.ConditionNotMet(requestType, failedCondition)),

            _ => new(Decision(Effect.Indeterminate, null, [], [], IndeterminateReason, elapsed), null)
        };

    private static ABACRequirementVerdict NotFoundVerdict(
        List<EvaluatedPolicy> policies,
        Type requestType,
        TimeSpan elapsed)
    {
        var missing = policies.First(policy => policy.Decision is null).Outcome.PolicyId;

        return new(
            Decision(Effect.Deny, missing, [], [], NotFoundReason, elapsed),
            ABACErrors.RequiredPolicyNotFound(requestType, missing));
    }

    /// <summary>
    /// Builds the decision of a Permit or Deny verdict from the named policies that decided it
    /// (the permitting ones for Permit; the denying and not-applicable ones for Deny): their
    /// identifiers, obligations and advice are combined. The PDP already kept only the
    /// obligations and advice that match each policy's own effect.
    /// </summary>
    /// <remarks>Only called when every required policy was found.</remarks>
    private static PolicyDecision FromPolicies(
        Effect effect,
        List<EvaluatedPolicy> policies,
        string? reason,
        TimeSpan elapsed)
    {
        var deciding = policies
            .Where(policy => effect == Effect.Permit
                ? policy.Outcome.Effect == Effect.Permit
                : policy.Outcome.Effect is Effect.Deny or Effect.NotApplicable)
            .ToList();

        var policyId = deciding.Count > 0
            ? string.Join(",", deciding.Select(policy => policy.Outcome.PolicyId))
            : null;

        return Decision(
            effect,
            policyId,
            deciding.SelectMany(policy => policy.Decision!.Obligations).ToList(),
            deciding.SelectMany(policy => policy.Decision!.Advice).ToList(),
            reason,
            elapsed);
    }

    private static PolicyDecision Decision(
        Effect effect,
        string? policyId,
        IReadOnlyList<Obligation> obligations,
        IReadOnlyList<AdviceExpression> advice,
        string? reason,
        TimeSpan elapsed) => new()
        {
            Effect = effect,
            PolicyId = policyId,
            Obligations = obligations,
            Advice = advice,
            Reason = reason,
            EvaluationDuration = elapsed
        };

    private sealed record EvaluatedPolicy(RequiredPolicyOutcome Outcome, PolicyDecision? Decision);
}
