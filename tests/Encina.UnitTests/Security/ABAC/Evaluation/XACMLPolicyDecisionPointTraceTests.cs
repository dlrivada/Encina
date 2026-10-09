#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.CombiningAlgorithms;
using Encina.Security.ABAC.Evaluation;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using static LanguageExt.Prelude;

using Target = Encina.Security.ABAC.Target;

namespace Encina.UnitTests.Security.ABAC.Evaluation;

/// <summary>
/// Unit tests for the opt-in evaluation trace of <see cref="XACMLPolicyDecisionPoint"/> (#751 Phase 1):
/// <see cref="PolicyDecision.EvaluatedPolicies"/> and <see cref="PolicyDecision.RuleId"/>.
/// </summary>
public sealed class XACMLPolicyDecisionPointTraceTests
{
    private static XACMLPolicyDecisionPoint CreatePdp(IPolicyAdministrationPoint pap)
    {
        var registry = new DefaultFunctionRegistry();
        return new XACMLPolicyDecisionPoint(
            pap,
            new TargetEvaluator(registry),
            new ConditionEvaluator(registry),
            new CombiningAlgorithmFactory(),
            NullLogger<XACMLPolicyDecisionPoint>.Instance);
    }

    private static PolicyEvaluationContext Context(bool trace = true, int maxEntries = 64) => new()
    {
        SubjectAttributes = new Dictionary<string, AttributeBag>(),
        ResourceAttributes = new Dictionary<string, AttributeBag>(),
        EnvironmentAttributes = new Dictionary<string, AttributeBag>(),
        ActionAttributes = new Dictionary<string, AttributeBag>(),
        RequestType = typeof(object),
        IncludeEvaluationTrace = trace,
        MaxTraceEntries = maxEntries
    };

    private static Rule MakeRule(string id, Effect effect) =>
        new() { Id = id, Effect = effect, Obligations = [], Advice = [] };

    private static Policy MakePolicy(
        string id,
        bool isEnabled = true,
        Target? target = null,
        string? version = null,
        params Rule[] rules) => new()
        {
            Id = id,
            IsEnabled = isEnabled,
            Target = target,
            Version = version,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules = rules,
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        };

    private static PolicySet MakePolicySet(
        string id,
        bool isEnabled = true,
        Target? target = null,
        IReadOnlyList<Policy>? policies = null,
        IReadOnlyList<PolicySet>? policySets = null) => new()
        {
            Id = id,
            IsEnabled = isEnabled,
            Target = target,
            Version = "2",
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Policies = policies ?? [],
            PolicySets = policySets ?? [],
            Obligations = [],
            Advice = []
        };

    private static Target SubjectMustHave(string attributeId, bool mustBePresent = false) => new()
    {
        AnyOfElements =
        [
            new AnyOf
            {
                AllOfElements =
                [
                    new AllOf
                    {
                        Matches =
                        [
                            new Match
                            {
                                FunctionId = XACMLFunctionIds.StringEqual,
                                AttributeDesignator = new AttributeDesignator
                                {
                                    Category = AttributeCategory.Subject,
                                    AttributeId = attributeId,
                                    DataType = XACMLDataTypes.String,
                                    MustBePresent = mustBePresent
                                },
                                AttributeValue = new AttributeValue { DataType = XACMLDataTypes.String, Value = "x" }
                            }
                        ]
                    }
                ]
            }
        ]
    };

    private static IPolicyAdministrationPoint Pap(IReadOnlyList<PolicySet>? sets = null, IReadOnlyList<Policy>? policies = null)
    {
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>(sets ?? []));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<Policy>>(policies ?? []));
        return pap;
    }

    [Fact]
    public async Task EvaluateAsync_TraceNotRequested_DecisionCarriesNoTraceAndNoRuleId()
    {
        var pdp = CreatePdp(Pap(policies: [MakePolicy("p", rules: MakeRule("r", Effect.Deny))]));

        var decision = await pdp.EvaluateAsync(Context(trace: false));

        decision.Effect.ShouldBe(Effect.Deny);
        decision.EvaluatedPolicies.ShouldBeEmpty();
        decision.RuleId.ShouldBeNull();
        decision.EvaluationTraceTruncated.ShouldBeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_TraceNotRequested_BuildsNoTraceNodes()
    {
        var policies = Enumerable.Range(0, 50).Select(i => MakePolicy($"p{i}", rules: MakeRule($"r{i}", Effect.Permit))).ToList();
        var pdp = CreatePdp(Pap(policies: policies));
        var off = Context(trace: false);
        var on = Context(trace: true);

        PolicyTraceNode.CreatedOnThisThread = 0;
        var decisionOff = await pdp.EvaluateAsync(off);
        var createdWithTraceOff = PolicyTraceNode.CreatedOnThisThread;

        PolicyTraceNode.CreatedOnThisThread = 0;
        var decisionOn = await pdp.EvaluateAsync(on);
        var createdWithTraceOn = PolicyTraceNode.CreatedOnThisThread;

        decisionOff.EvaluatedPolicies.ShouldBeEmpty();
        decisionOff.RuleId.ShouldBeNull();
        createdWithTraceOff.ShouldBe(0);
        decisionOn.EvaluatedPolicies.Count.ShouldBe(50);
        createdWithTraceOn.ShouldBe(51); // the root plus one node per policy
    }

    [Fact]
    public async Task EvaluatePolicyAsync_TraceNotRequested_BuildsNoTraceNodes()
    {
        var pdp = CreatePdp(Pap(policies: [MakePolicy("wanted", rules: MakeRule("r", Effect.Permit))]));

        PolicyTraceNode.CreatedOnThisThread = 0;
        await pdp.EvaluatePolicyAsync("wanted", Context(trace: false));

        PolicyTraceNode.CreatedOnThisThread.ShouldBe(0);
    }

    [Fact]
    public async Task EvaluateAsync_TraceRequested_RecordsPolicyEffectDecisiveRulesAndRuleId()
    {
        var policy = MakePolicy("p", version: "7", rules: [MakeRule("r-permit", Effect.Permit), MakeRule("r-deny-1", Effect.Deny), MakeRule("r-deny-2", Effect.Deny)]);
        var pdp = CreatePdp(Pap(policies: [policy]));

        var decision = await pdp.EvaluateAsync(Context());

        decision.Effect.ShouldBe(Effect.Deny);
        decision.RuleId.ShouldBe("r-deny-1");
        var node = decision.EvaluatedPolicies.ShouldHaveSingleItem();
        node.PolicyId.ShouldBe("p");
        node.IsPolicySet.ShouldBeFalse();
        node.Effect.ShouldBe(Effect.Deny);
        node.Reason.ShouldBe(PolicyTraceReason.Evaluated);
        node.Version.ShouldBe("7");
        node.DecisiveRuleIds.ShouldBe(["r-deny-1", "r-deny-2"]);
        node.Children.ShouldBeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_PolicySetWithChildren_NestsChildNodesAndResolvesRuleIdThroughTheSet()
    {
        var inner = MakePolicy("inner", rules: MakeRule("inner-rule", Effect.Permit));
        var nested = MakePolicySet("nested", policies: [MakePolicy("deep", rules: MakeRule("deep-rule", Effect.Permit))]);
        var set = MakePolicySet("set", policies: [inner], policySets: [nested]);
        var pdp = CreatePdp(Pap(sets: [set]));

        var decision = await pdp.EvaluateAsync(Context());

        decision.Effect.ShouldBe(Effect.Permit);
        decision.PolicyId.ShouldBe("set");
        decision.RuleId.ShouldBe("inner-rule");
        var root = decision.EvaluatedPolicies.ShouldHaveSingleItem();
        root.IsPolicySet.ShouldBeTrue();
        root.Version.ShouldBe("2");
        root.DecisiveRuleIds.ShouldBeEmpty();
        root.Children.Select(child => child.PolicyId).ShouldBe(["inner", "nested"]);
        root.Children[1].Children.ShouldHaveSingleItem().PolicyId.ShouldBe("deep");
    }

    [Fact]
    public async Task EvaluateAsync_DisabledPolicy_IsTracedAsDisabledNotApplicable()
    {
        var pdp = CreatePdp(Pap(policies: [MakePolicy("off", isEnabled: false, rules: MakeRule("r", Effect.Deny))]));

        var decision = await pdp.EvaluateAsync(Context());

        decision.Effect.ShouldBe(Effect.NotApplicable);
        decision.RuleId.ShouldBeNull();
        var node = decision.EvaluatedPolicies.ShouldHaveSingleItem();
        node.Reason.ShouldBe(PolicyTraceReason.Disabled);
        node.Effect.ShouldBe(Effect.NotApplicable);
        node.DecisiveRuleIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_TargetNotMatched_IsTracedAsTargetNotMatched()
    {
        var policy = MakePolicy("p", target: SubjectMustHave("department"), rules: MakeRule("r", Effect.Deny));
        var set = MakePolicySet("s", target: SubjectMustHave("department"));
        var pdp = CreatePdp(Pap(sets: [set], policies: [policy]));

        var decision = await pdp.EvaluateAsync(Context());

        decision.EvaluatedPolicies.Select(n => n.Reason).ShouldAllBe(reason => reason == PolicyTraceReason.TargetNotMatched);
        decision.EvaluatedPolicies.Select(n => n.PolicyId).ShouldBe(["s", "p"]);
    }

    [Fact]
    public async Task EvaluateAsync_TargetThatCannotBeEvaluated_IsTracedAsTargetIndeterminate()
    {
        var policy = MakePolicy("p", target: SubjectMustHave("department", mustBePresent: true), rules: MakeRule("r", Effect.Deny));
        var set = MakePolicySet("s", target: SubjectMustHave("department", mustBePresent: true));
        var pdp = CreatePdp(Pap(sets: [set], policies: [policy]));

        var decision = await pdp.EvaluateAsync(Context());

        decision.Effect.ShouldBe(Effect.Indeterminate);
        decision.EvaluatedPolicies.ShouldAllBe(node => node.Reason == PolicyTraceReason.TargetIndeterminate && node.Effect == Effect.Indeterminate);
    }

    [Fact]
    public async Task EvaluateAsync_MoreNodesThanTheLimit_DropsTheExcessAndMarksTruncation()
    {
        var policies = Enumerable.Range(0, 5).Select(i => MakePolicy($"p{i}", rules: MakeRule($"r{i}", Effect.Permit))).ToList();
        var pdp = CreatePdp(Pap(policies: policies));

        var decision = await pdp.EvaluateAsync(Context(maxEntries: 2));

        decision.Effect.ShouldBe(Effect.Permit);
        decision.EvaluatedPolicies.Select(n => n.PolicyId).ShouldBe(["p0", "p1"]);
        decision.EvaluationTraceTruncated.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task EvaluateAsync_LimitBelowOne_RecordsNoNodeButStillDecides(int limit)
    {
        var pdp = CreatePdp(Pap(policies: [MakePolicy("p", rules: MakeRule("r", Effect.Permit))]));

        var decision = await pdp.EvaluateAsync(Context(maxEntries: limit));

        decision.Effect.ShouldBe(Effect.Permit);
        decision.EvaluatedPolicies.ShouldBeEmpty();
        decision.RuleId.ShouldBeNull();
        decision.EvaluationTraceTruncated.ShouldBeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_ExactlyAtTheLimit_IsNotTruncated()
    {
        var pdp = CreatePdp(Pap(policies: [MakePolicy("a", rules: MakeRule("r", Effect.Permit)), MakePolicy("b", rules: MakeRule("r", Effect.Permit))]));

        var decision = await pdp.EvaluateAsync(Context(maxEntries: 2));

        decision.EvaluatedPolicies.Count.ShouldBe(2);
        decision.EvaluationTraceTruncated.ShouldBeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_TruncatedPolicySet_KeepsTheSetAndDropsItsChildren()
    {
        var set = MakePolicySet("set", policies: [MakePolicy("child", rules: MakeRule("child-rule", Effect.Permit))]);
        var pdp = CreatePdp(Pap(sets: [set]));

        var decision = await pdp.EvaluateAsync(Context(maxEntries: 1));

        decision.EvaluatedPolicies.ShouldHaveSingleItem().Children.ShouldBeEmpty();
        decision.RuleId.ShouldBeNull();
        decision.EvaluationTraceTruncated.ShouldBeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_StoreCannotBeRead_IsIndeterminateWithNoTrace()
    {
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IReadOnlyList<PolicySet>>(ABACErrors.StoreOperationFailed("read", "down")));
        var pdp = CreatePdp(pap);

        var decision = await pdp.EvaluateAsync(Context());

        decision.Effect.ShouldBe(Effect.Indeterminate);
        decision.EvaluatedPolicies.ShouldBeEmpty();
        decision.RuleId.ShouldBeNull();
    }

    [Fact]
    public async Task EvaluatePolicyAsync_TraceRequested_TracesOnlyTheNamedPolicy()
    {
        var pdp = CreatePdp(Pap(policies:
        [
            MakePolicy("wanted", rules: MakeRule("wanted-rule", Effect.Deny)),
            MakePolicy("other", rules: MakeRule("other-rule", Effect.Permit))
        ]));

        var result = await pdp.EvaluatePolicyAsync("wanted", Context());

        var decision = result.Match(Right: d => d, Left: e => throw new ShouldAssertException(e.GetCode().IfNone("none")));
        decision.RuleId.ShouldBe("wanted-rule");
        decision.EvaluatedPolicies.ShouldHaveSingleItem().PolicyId.ShouldBe("wanted");
    }

    [Fact]
    public async Task EvaluatePolicyAsync_TraceNotRequested_CarriesNoTrace()
    {
        var pdp = CreatePdp(Pap(policies: [MakePolicy("wanted", rules: MakeRule("wanted-rule", Effect.Deny))]));

        var result = await pdp.EvaluatePolicyAsync("wanted", Context(trace: false));

        var decision = result.Match(Right: d => d, Left: e => throw new ShouldAssertException(e.GetCode().IfNone("none")));
        decision.RuleId.ShouldBeNull();
        decision.EvaluatedPolicies.ShouldBeEmpty();
    }
}
