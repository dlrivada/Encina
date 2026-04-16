using Encina.Security.ABAC;
using Encina.Security.ABAC.CombiningAlgorithms;
using Shouldly;

namespace Encina.UnitTests.Security.ABAC.CombiningAlgorithms;

/// <summary>
/// Unit tests for <see cref="OnlyOneApplicableAlgorithm"/>.
/// Verifies XACML 3.0 §C.4: exactly one applicable policy must exist.
/// </summary>
public sealed class OnlyOneApplicableAlgorithmTests
{
    private readonly OnlyOneApplicableAlgorithm _sut = new();

    #region AlgorithmId

    [Fact]
    public void AlgorithmId_ReturnsOnlyOneApplicable()
    {
        _sut.AlgorithmId.ShouldBe(CombiningAlgorithmId.OnlyOneApplicable);
    }

    #endregion

    #region CombineRuleResults — Delegates to FirstApplicable

    [Fact]
    public void CombineRuleResults_FirstPermit_ReturnsPermit()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.Permit),
            MakeRuleResult(Effect.Deny)
        };

        _sut.CombineRuleResults(results).ShouldBe(Effect.Permit,
            "Rule-level delegates to FirstApplicable; first non-NotApplicable wins");
    }

    [Fact]
    public void CombineRuleResults_SkipsNotApplicable_ReturnsFirstApplicable()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.NotApplicable),
            MakeRuleResult(Effect.Deny)
        };

        _sut.CombineRuleResults(results).ShouldBe(Effect.Deny);
    }

    [Fact]
    public void CombineRuleResults_AllNotApplicable_ReturnsNotApplicable()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.NotApplicable),
            MakeRuleResult(Effect.NotApplicable)
        };

        _sut.CombineRuleResults(results).ShouldBe(Effect.NotApplicable);
    }

    [Fact]
    public void CombineRuleResults_Empty_ReturnsNotApplicable()
    {
        _sut.CombineRuleResults(Array.Empty<RuleEvaluationResult>())
            .ShouldBe(Effect.NotApplicable);
    }

    #endregion

    #region CombinePolicyResults — Zero Applicable

    [Fact]
    public void CombinePolicyResults_AllNotApplicable_ReturnsNotApplicable()
    {
        var results = new[]
        {
            MakePolicyResult(Effect.NotApplicable, "p1"),
            MakePolicyResult(Effect.NotApplicable, "p2")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Effect.ShouldBe(Effect.NotApplicable);
    }

    [Fact]
    public void CombinePolicyResults_Empty_ReturnsNotApplicable()
    {
        var combined = _sut.CombinePolicyResults(Array.Empty<PolicyEvaluationResult>());

        combined.Effect.ShouldBe(Effect.NotApplicable);
    }

    #endregion

    #region CombinePolicyResults — Exactly One Applicable

    [Fact]
    public void CombinePolicyResults_SinglePermit_ReturnsPermit()
    {
        var results = new[]
        {
            MakePolicyResult(Effect.NotApplicable, "p1"),
            MakePolicyResult(Effect.Permit, "p2"),
            MakePolicyResult(Effect.NotApplicable, "p3")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Effect.ShouldBe(Effect.Permit);
        combined.PolicyId.ShouldBe("p2");
    }

    [Fact]
    public void CombinePolicyResults_SingleDeny_ReturnsDeny()
    {
        var results = new[]
        {
            MakePolicyResult(Effect.Deny, "p1"),
            MakePolicyResult(Effect.NotApplicable, "p2")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Effect.ShouldBe(Effect.Deny);
        combined.PolicyId.ShouldBe("p1");
    }

    [Fact]
    public void CombinePolicyResults_SinglePermit_PreservesObligations()
    {
        var obligation = new Obligation
        {
            Id = "audit",
            FulfillOn = FulfillOn.Permit,
            AttributeAssignments = []
        };

        var results = new[]
        {
            MakePolicyResult(Effect.Permit, "p1", obligations: [obligation]),
            MakePolicyResult(Effect.NotApplicable, "p2")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Effect.ShouldBe(Effect.Permit);
        combined.Obligations.ShouldHaveSingleItem()
            .Id.ShouldBe("audit");
    }

    [Fact]
    public void CombinePolicyResults_SinglePermit_PreservesAdvice()
    {
        var advice = new AdviceExpression
        {
            Id = "notify",
            AppliesTo = FulfillOn.Permit,
            AttributeAssignments = []
        };

        var results = new[]
        {
            MakePolicyResult(Effect.NotApplicable, "p1"),
            MakePolicyResult(Effect.Permit, "p2", advice: [advice])
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Advice.ShouldHaveSingleItem()
            .Id.ShouldBe("notify");
    }

    #endregion

    #region CombinePolicyResults — More Than One Applicable → Indeterminate

    [Fact]
    public void CombinePolicyResults_TwoPermit_ReturnsIndeterminate()
    {
        var results = new[]
        {
            MakePolicyResult(Effect.Permit, "p1"),
            MakePolicyResult(Effect.Permit, "p2")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Effect.ShouldBe(Effect.Indeterminate,
            "More than one applicable policy → Indeterminate per §C.4");
    }

    [Fact]
    public void CombinePolicyResults_PermitAndDeny_ReturnsIndeterminate()
    {
        var results = new[]
        {
            MakePolicyResult(Effect.Permit, "p1"),
            MakePolicyResult(Effect.Deny, "p2")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Effect.ShouldBe(Effect.Indeterminate,
            "Two applicable policies (even with different effects) → Indeterminate");
    }

    [Fact]
    public void CombinePolicyResults_ThreeApplicable_ReturnsIndeterminate()
    {
        var results = new[]
        {
            MakePolicyResult(Effect.Permit, "p1"),
            MakePolicyResult(Effect.NotApplicable, "p2"),
            MakePolicyResult(Effect.Deny, "p3"),
            MakePolicyResult(Effect.Permit, "p4")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Effect.ShouldBe(Effect.Indeterminate);
    }

    [Fact]
    public void CombinePolicyResults_MultipleApplicable_EmptyPolicyId()
    {
        var results = new[]
        {
            MakePolicyResult(Effect.Permit, "p1"),
            MakePolicyResult(Effect.Deny, "p2")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.PolicyId.ShouldBeEmpty("Multiple applicable → no single policy to attribute");
    }

    [Fact]
    public void CombinePolicyResults_MultipleApplicable_EmptyObligations()
    {
        var obligation = new Obligation
        {
            Id = "should-not-appear",
            FulfillOn = FulfillOn.Permit,
            AttributeAssignments = []
        };

        var results = new[]
        {
            MakePolicyResult(Effect.Permit, "p1", obligations: [obligation]),
            MakePolicyResult(Effect.Permit, "p2")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Effect.ShouldBe(Effect.Indeterminate);
        combined.Obligations.ShouldBeEmpty("Indeterminate from ambiguity should carry no obligations");
    }

    #endregion

    #region CombinePolicyResults — Any Indeterminate → Indeterminate

    [Fact]
    public void CombinePolicyResults_IndeterminateFirst_ReturnsIndeterminate()
    {
        var results = new[]
        {
            MakePolicyResult(Effect.Indeterminate, "p1"),
            MakePolicyResult(Effect.Permit, "p2")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Effect.ShouldBe(Effect.Indeterminate,
            "Any Indeterminate during evaluation → overall Indeterminate");
        combined.PolicyId.ShouldBe("p1",
            "Indeterminate preserves the PolicyId of the offending policy");
    }

    [Fact]
    public void CombinePolicyResults_IndeterminateMiddle_ReturnsIndeterminate()
    {
        var results = new[]
        {
            MakePolicyResult(Effect.NotApplicable, "p1"),
            MakePolicyResult(Effect.Indeterminate, "p2"),
            MakePolicyResult(Effect.Permit, "p3")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Effect.ShouldBe(Effect.Indeterminate);
        combined.PolicyId.ShouldBe("p2");
    }

    [Fact]
    public void CombinePolicyResults_IndeterminateOnly_ReturnsIndeterminate()
    {
        var results = new[]
        {
            MakePolicyResult(Effect.Indeterminate, "p1")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Effect.ShouldBe(Effect.Indeterminate);
    }

    #endregion

    #region Helpers

    private static RuleEvaluationResult MakeRuleResult(Effect effect) =>
        new()
        {
            Effect = effect,
            Rule = new Rule
            {
                Id = $"rule-{Guid.NewGuid():N}",
                Effect = Effect.Permit,
                Obligations = [],
                Advice = []
            },
            Obligations = [],
            Advice = []
        };

    private static PolicyEvaluationResult MakePolicyResult(
        Effect effect,
        string policyId,
        IReadOnlyList<Obligation>? obligations = null,
        IReadOnlyList<AdviceExpression>? advice = null) =>
        new()
        {
            Effect = effect,
            PolicyId = policyId,
            Obligations = obligations ?? [],
            Advice = advice ?? []
        };

    #endregion
}
