using System.Diagnostics;

using Encina.Security.ABAC.CombiningAlgorithms;

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
/// <item><description>Retrieve all policy sets and standalone policies from the PAP</description></item>
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

    // Root combining algorithm for top-level results
    private readonly ICombiningAlgorithm _rootAlgorithm = algorithmFactory.GetAlgorithm(CombiningAlgorithmId.DenyOverrides);

    /// <inheritdoc />
    public async ValueTask<PolicyDecision> EvaluateAsync(
        PolicyEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var allResults = new List<PolicyEvaluationResult>();

            // 1. Evaluate all policy sets
            var policySetsResult = await _pap.GetPolicySetsAsync(cancellationToken).ConfigureAwait(false);
            var policySetsEvaluated = policySetsResult.Match(
                Left: error =>
                {
                    _logger.LogWarning("Failed to retrieve policy sets: {ErrorMessage}", error.Message);
                    return false;
                },
                Right: policySets =>
                {
                    foreach (var policySet in policySets)
                    {
                        var result = EvaluatePolicySet(policySet, context);
                        allResults.Add(result);
                    }

                    return true;
                });

            if (!policySetsEvaluated)
            {
                stopwatch.Stop();
                return new PolicyDecision
                {
                    Effect = Effect.Indeterminate,
                    Status = new DecisionStatus
                    {
                        StatusCode = "processing-error",
                        StatusMessage = "Failed to retrieve policy sets from PAP."
                    },
                    Obligations = [],
                    Advice = [],
                    EvaluationDuration = stopwatch.Elapsed
                };
            }

            // 2. Evaluate standalone policies (not in any policy set)
            var policiesResult = await _pap.GetPoliciesAsync(null, cancellationToken).ConfigureAwait(false);
            var policiesEvaluated = policiesResult.Match(
                Left: error =>
                {
                    _logger.LogWarning("Failed to retrieve standalone policies: {ErrorMessage}", error.Message);
                    return false;
                },
                Right: policies =>
                {
                    foreach (var policy in policies)
                    {
                        var result = EvaluatePolicy(policy, context);
                        allResults.Add(result);
                    }

                    return true;
                });

            // Log if standalone policy retrieval failed (non-fatal — we continue with policy sets)
            if (!policiesEvaluated)
            {
                _logger.LogDebug("Proceeding with policy set results only");
            }

            // 3. Combine all results at root level
            PolicyEvaluationResult combinedResult;
            if (allResults.Count == 0)
            {
                combinedResult = new PolicyEvaluationResult
                {
                    Effect = Effect.NotApplicable,
                    PolicyId = string.Empty,
                    Obligations = [],
                    Advice = []
                };
            }
            else
            {
                combinedResult = _rootAlgorithm.CombinePolicyResults(allResults);
            }

            stopwatch.Stop();

            // 4. Build final decision with filtered obligations/advice
            return BuildDecision(combinedResult, context, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error during policy evaluation");

            return new PolicyDecision
            {
                Effect = Effect.Indeterminate,
                Status = new DecisionStatus
                {
                    StatusCode = "processing-error",
                    StatusMessage = $"Unexpected error: {ex.Message}"
                },
                Obligations = [],
                Advice = [],
                EvaluationDuration = stopwatch.Elapsed
            };
        }
    }

    /// <summary>
    /// Evaluates a <see cref="PolicySet"/> recursively against the given context.
    /// </summary>
    private PolicyEvaluationResult EvaluatePolicySet(
        PolicySet policySet,
        PolicyEvaluationContext context)
    {
        // Disabled policy sets are not applicable
        if (!policySet.IsEnabled)
        {
            return NotApplicableResult(policySet.Id);
        }

        // Evaluate target
        var targetResult = _targetEvaluator.EvaluateTarget(policySet.Target, context);
        if (targetResult == Effect.NotApplicable)
        {
            return NotApplicableResult(policySet.Id);
        }

        if (targetResult == Effect.Indeterminate)
        {
            return IndeterminateResult(policySet.Id);
        }

        // Evaluate child policies and policy sets
        var childResults = new List<PolicyEvaluationResult>();

        foreach (var childPolicy in policySet.Policies)
        {
            childResults.Add(EvaluatePolicy(childPolicy, context));
        }

        foreach (var childPolicySet in policySet.PolicySets)
        {
            childResults.Add(EvaluatePolicySet(childPolicySet, context));
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

        return new PolicyEvaluationResult
        {
            Effect = combinedResult.Effect,
            PolicyId = policySet.Id,
            Obligations = obligations,
            Advice = advice
        };
    }

    /// <summary>
    /// Evaluates a <see cref="Policy"/> against the given context by evaluating
    /// its rules and combining them with the policy's algorithm.
    /// </summary>
    private PolicyEvaluationResult EvaluatePolicy(
        Policy policy,
        PolicyEvaluationContext context)
    {
        // Disabled policies are not applicable
        if (!policy.IsEnabled)
        {
            return NotApplicableResult(policy.Id);
        }

        // Evaluate target
        var targetResult = _targetEvaluator.EvaluateTarget(policy.Target, context);
        if (targetResult == Effect.NotApplicable)
        {
            return NotApplicableResult(policy.Id);
        }

        if (targetResult == Effect.Indeterminate)
        {
            return IndeterminateResult(policy.Id);
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

        return new PolicyEvaluationResult
        {
            Effect = combinedEffect,
            PolicyId = policy.Id,
            Obligations = obligations,
            Advice = advice
        };
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
        TimeSpan evaluationDuration)
    {
        // Filter obligations: only those whose FulfillOn matches the final effect
        var obligations = FilterObligations(combinedResult.Obligations, combinedResult.Effect);

        // Filter advice: only those whose AppliesTo matches the final effect (if requested)
        var advice = context.IncludeAdvice
            ? FilterAdvice(combinedResult.Advice, combinedResult.Effect)
            : (IReadOnlyList<AdviceExpression>)[];

        return new PolicyDecision
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
