#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.CombiningAlgorithms;
using Encina.Security.ABAC.Evaluation;

using FsCheck.Xunit;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.PropertyTests.Security.ABAC;

/// <summary>
/// Property-based tests for the evaluation trace of the PDP (#751 Phase 1): the trace is bounded by
/// its limit, never changes the decision, and its rule id belongs to the decisive rules.
/// </summary>
public sealed class PolicyEvaluationTraceProperties
{
    private static Effect ToEffect(byte code) => (code % 3) switch
    {
        0 => Effect.Permit,
        1 => Effect.Deny,
        _ => Effect.NotApplicable
    };

    private static List<Policy> Policies(byte[] codes) =>
        codes.Select((code, index) => new Policy
        {
            Id = $"p{index}",
            IsEnabled = code % 7 != 0,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules = [new Rule { Id = $"r{index}", Effect = ToEffect(code), Obligations = [], Advice = [] }],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        }).ToList();

    private static XACMLPolicyDecisionPoint CreatePdp(List<Policy> policies)
    {
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>([]));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<Policy>>(policies));
        var registry = new DefaultFunctionRegistry();
        return new XACMLPolicyDecisionPoint(
            pap,
            new TargetEvaluator(registry),
            new ConditionEvaluator(registry),
            new CombiningAlgorithmFactory(),
            NullLogger<XACMLPolicyDecisionPoint>.Instance);
    }

    private static PolicyEvaluationContext Context(bool trace, int limit) => new()
    {
        SubjectAttributes = new Dictionary<string, AttributeBag>(),
        ResourceAttributes = new Dictionary<string, AttributeBag>(),
        EnvironmentAttributes = new Dictionary<string, AttributeBag>(),
        ActionAttributes = new Dictionary<string, AttributeBag>(),
        RequestType = typeof(object),
        IncludeEvaluationTrace = trace,
        MaxTraceEntries = limit
    };

    [Property(MaxTest = 100)]
    public bool Trace_NeverExceedsItsLimit(byte[] codes, byte limitSeed)
    {
        var policies = Policies(codes ?? []);
        var limit = limitSeed % 8;

        var decision = CreatePdp(policies).EvaluateAsync(Context(trace: true, limit)).AsTask().GetAwaiter().GetResult();

        return decision.EvaluatedPolicies.Count <= limit
            && decision.EvaluatedPolicies.Count == Math.Min(limit, policies.Count);
    }

    [Property(MaxTest = 100)]
    public bool Trace_DoesNotChangeTheDecision(byte[] codes)
    {
        var pdp = CreatePdp(Policies(codes ?? []));

        var plain = pdp.EvaluateAsync(Context(trace: false, 64)).AsTask().GetAwaiter().GetResult();
        var traced = pdp.EvaluateAsync(Context(trace: true, 64)).AsTask().GetAwaiter().GetResult();

        return plain.Effect == traced.Effect
            && plain.PolicyId == traced.PolicyId
            && plain.EvaluatedPolicies.Count == 0
            && plain.RuleId is null;
    }

    [Property(MaxTest = 100)]
    public bool RuleId_WhenPresent_BelongsToAPolicyWithTheDecisionEffect(byte[] codes)
    {
        var policies = Policies(codes ?? []);

        var decision = CreatePdp(policies).EvaluateAsync(Context(trace: true, 64)).AsTask().GetAwaiter().GetResult();

        if (decision.RuleId is null)
        {
            return true;
        }

        var node = decision.EvaluatedPolicies.First(n => n.DecisiveRuleIds.Contains(decision.RuleId));
        return node.Effect == decision.Effect && node.PolicyId == decision.PolicyId;
    }

    [Property(MaxTest = 100)]
    public bool Resolver_MatchesTheFirstNodeWithTheEffect(byte[] codes)
    {
        var nodes = (codes ?? []).Select((code, index) => new PolicyEvaluationTrace
        {
            PolicyId = $"p{index}",
            IsPolicySet = false,
            Effect = ToEffect(code),
            Reason = PolicyTraceReason.Evaluated,
            DecisiveRuleIds = [$"r{index}"]
        }).ToList();

        var resolved = PolicyEvaluationTraceResolver.ResolveDecisiveRuleId(nodes, Effect.Deny);
        var first = nodes.FirstOrDefault(n => n.Effect == Effect.Deny);

        return resolved == first?.DecisiveRuleIds[0];
    }
}
