using Encina.Security.ABAC;
using Encina.Security.ABAC.Evaluation;

using Shouldly;

namespace Encina.UnitTests.Security.ABAC.Evaluation;

/// <summary>
/// Unit tests for <see cref="PolicyEvaluationTraceResolver"/> and <see cref="PolicyTraceNode"/> (#751 Phase 1).
/// </summary>
public sealed class PolicyEvaluationTraceResolverTests
{
    private static PolicyEvaluationTrace Policy(string id, Effect effect, params string[] rules) => new()
    {
        PolicyId = id,
        IsPolicySet = false,
        Effect = effect,
        Reason = PolicyTraceReason.Evaluated,
        DecisiveRuleIds = rules
    };

    private static PolicyEvaluationTrace Set(string id, Effect effect, params PolicyEvaluationTrace[] children) => new()
    {
        PolicyId = id,
        IsPolicySet = true,
        Effect = effect,
        Reason = PolicyTraceReason.Evaluated,
        Children = children
    };

    [Fact]
    public void ResolveDecisiveRuleId_NullNodes_Throws() =>
        Should.Throw<ArgumentNullException>(() => PolicyEvaluationTraceResolver.ResolveDecisiveRuleId(null!, Effect.Deny));

    [Fact]
    public void ResolveDecisiveRuleId_NotApplicable_IsNull() =>
        PolicyEvaluationTraceResolver
            .ResolveDecisiveRuleId([Policy("p", Effect.NotApplicable, "r")], Effect.NotApplicable)
            .ShouldBeNull();

    [Fact]
    public void ResolveDecisiveRuleId_NoNodeWithTheEffect_IsNull() =>
        PolicyEvaluationTraceResolver
            .ResolveDecisiveRuleId([Policy("p", Effect.Permit, "r")], Effect.Deny)
            .ShouldBeNull();

    [Fact]
    public void ResolveDecisiveRuleId_TakesTheFirstNodeWithTheEffectAndItsFirstRule() =>
        PolicyEvaluationTraceResolver
            .ResolveDecisiveRuleId(
                [Policy("a", Effect.Permit, "ra"), Policy("b", Effect.Deny, "rb1", "rb2"), Policy("c", Effect.Deny, "rc")],
                Effect.Deny)
            .ShouldBe("rb1");

    [Fact]
    public void ResolveDecisiveRuleId_DescendsIntoThePolicySetThatMatches() =>
        PolicyEvaluationTraceResolver
            .ResolveDecisiveRuleId(
                [Set("s", Effect.Deny, Policy("x", Effect.Permit, "rx"), Policy("y", Effect.Deny, "ry"))],
                Effect.Deny)
            .ShouldBe("ry");

    [Fact]
    public void ResolveDecisiveRuleId_MatchingPolicyWithoutRules_IsNull() =>
        PolicyEvaluationTraceResolver
            .ResolveDecisiveRuleId([Policy("a", Effect.Deny), Policy("b", Effect.Deny, "rb")], Effect.Deny)
            .ShouldBeNull();

    [Fact]
    public void ResolveDecisiveRuleId_Indeterminate_ReturnsTheFailingRule() =>
        PolicyEvaluationTraceResolver
            .ResolveDecisiveRuleId([Policy("a", Effect.Indeterminate, "broken")], Effect.Indeterminate)
            .ShouldBe("broken");

    [Fact]
    public void PolicyTraceNode_ChildrenBeyondTheBudget_AreDroppedAndFlagged()
    {
        var root = PolicyTraceNode.CreateRoot(2);

        var first = root.AddChild("a", isPolicySet: false, version: "1");
        var second = root.AddChild("b", isPolicySet: true, version: null);
        var third = root.AddChild("c", isPolicySet: false, version: null);

        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
        third.ShouldBeNull();
        root.Truncated.ShouldBeTrue();
        first.Complete(Effect.Permit, PolicyTraceReason.Evaluated, ["r"]);
        second.Complete(Effect.NotApplicable, PolicyTraceReason.Disabled);
        var traces = root.ToTraces();
        traces.Count.ShouldBe(2);
        traces[0].DecisiveRuleIds.ShouldBe(["r"]);
        traces[0].Version.ShouldBe("1");
        traces[1].Reason.ShouldBe(PolicyTraceReason.Disabled);
    }

    [Fact]
    public void PolicyTraceNode_EmptyRoot_FreezesToAnEmptyList()
    {
        var root = PolicyTraceNode.CreateRoot(5);

        root.ToTraces().ShouldBeEmpty();
        root.Truncated.ShouldBeFalse();
    }
}
