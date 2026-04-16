using Encina.Security.ABAC;
using Encina.Security.ABAC.CombiningAlgorithms;
using Shouldly;

namespace Encina.UnitTests.Security.ABAC.CombiningAlgorithms;

/// <summary>
/// Unit tests for <see cref="DenyOverridesAlgorithm"/>.
/// Verifies XACML 3.0 §C.1: any Deny overrides all other results.
/// </summary>
public sealed class DenyOverridesAlgorithmTests
{
    private readonly DenyOverridesAlgorithm _sut = new();

    #region AlgorithmId

    [Fact]
    public void AlgorithmId_ReturnsDenyOverrides()
    {
        _sut.AlgorithmId.ShouldBe(CombiningAlgorithmId.DenyOverrides);
    }

    #endregion

    #region CombineRuleResults — Single Effect

    [Fact]
    public void CombineRuleResults_SinglePermit_ReturnsPermit()
    {
        var results = new[] { MakeRuleResult(Effect.Permit) };

        var effect = _sut.CombineRuleResults(results);

        effect.ShouldBe(Effect.Permit);
    }

    [Fact]
    public void CombineRuleResults_SingleDeny_ReturnsDeny()
    {
        var results = new[] { MakeRuleResult(Effect.Deny) };

        var effect = _sut.CombineRuleResults(results);

        effect.ShouldBe(Effect.Deny);
    }

    [Fact]
    public void CombineRuleResults_SingleNotApplicable_ReturnsNotApplicable()
    {
        var results = new[] { MakeRuleResult(Effect.NotApplicable) };

        var effect = _sut.CombineRuleResults(results);

        effect.ShouldBe(Effect.NotApplicable);
    }

    [Fact]
    public void CombineRuleResults_SingleIndeterminate_ReturnsIndeterminate()
    {
        var results = new[] { MakeRuleResult(Effect.Indeterminate, ruleEffect: Effect.Deny) };

        var effect = _sut.CombineRuleResults(results);

        effect.ShouldBe(Effect.Indeterminate);
    }

    #endregion

    #region CombineRuleResults — All Same

    [Fact]
    public void CombineRuleResults_AllPermit_ReturnsPermit()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.Permit),
            MakeRuleResult(Effect.Permit),
            MakeRuleResult(Effect.Permit)
        };

        var effect = _sut.CombineRuleResults(results);

        effect.ShouldBe(Effect.Permit);
    }

    [Fact]
    public void CombineRuleResults_AllDeny_ReturnsDeny()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.Deny),
            MakeRuleResult(Effect.Deny)
        };

        var effect = _sut.CombineRuleResults(results);

        effect.ShouldBe(Effect.Deny);
    }

    [Fact]
    public void CombineRuleResults_AllNotApplicable_ReturnsNotApplicable()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.NotApplicable),
            MakeRuleResult(Effect.NotApplicable)
        };

        var effect = _sut.CombineRuleResults(results);

        effect.ShouldBe(Effect.NotApplicable);
    }

    #endregion

    #region CombineRuleResults — Mixed

    [Fact]
    public void CombineRuleResults_PermitAndDeny_ReturnsDeny()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.Permit),
            MakeRuleResult(Effect.Deny)
        };

        var effect = _sut.CombineRuleResults(results);

        effect.ShouldBe(Effect.Deny, "Deny overrides Permit per XACML §C.1");
    }

    [Fact]
    public void CombineRuleResults_PermitAndNotApplicable_ReturnsPermit()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.Permit),
            MakeRuleResult(Effect.NotApplicable)
        };

        var effect = _sut.CombineRuleResults(results);

        effect.ShouldBe(Effect.Permit);
    }

    [Fact]
    public void CombineRuleResults_PermitAndIndeterminateP_ReturnsIndeterminate()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.Permit),
            MakeRuleResult(Effect.Indeterminate, ruleEffect: Effect.Permit)
        };

        var effect = _sut.CombineRuleResults(results);

        effect.ShouldBe(Effect.Indeterminate,
            "Permit + Indeterminate{{P}} → Indeterminate per XACML §C.1");
    }

    [Fact]
    public void CombineRuleResults_IndeterminateD_ReturnsIndeterminate()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.NotApplicable),
            MakeRuleResult(Effect.Indeterminate, ruleEffect: Effect.Deny)
        };

        var effect = _sut.CombineRuleResults(results);

        effect.ShouldBe(Effect.Indeterminate,
            "Indeterminate{{D}} → Indeterminate per XACML §C.1");
    }

    [Fact]
    public void CombineRuleResults_Empty_ReturnsNotApplicable()
    {
        var results = Array.Empty<RuleEvaluationResult>();

        var effect = _sut.CombineRuleResults(results);

        effect.ShouldBe(Effect.NotApplicable);
    }

    #endregion

    #region CombinePolicyResults

    [Fact]
    public void CombinePolicyResults_AllPermit_ReturnsPermit()
    {
        var results = new[]
        {
            MakePolicyResult(Effect.Permit, "policy-1"),
            MakePolicyResult(Effect.Permit, "policy-2")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Effect.ShouldBe(Effect.Permit);
    }

    [Fact]
    public void CombinePolicyResults_PermitAndDeny_ReturnsDeny()
    {
        var results = new[]
        {
            MakePolicyResult(Effect.Permit, "policy-1"),
            MakePolicyResult(Effect.Deny, "policy-2")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Effect.ShouldBe(Effect.Deny);
    }

    [Fact]
    public void CombinePolicyResults_WithObligations_PreservesMatchingObligations()
    {
        var obligation = new Obligation
        {
            Id = "audit-log",
            FulfillOn = FulfillOn.Deny,
            AttributeAssignments = []
        };

        var results = new[]
        {
            MakePolicyResult(Effect.Deny, "policy-1", obligations: [obligation]),
            MakePolicyResult(Effect.Permit, "policy-2")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Effect.ShouldBe(Effect.Deny);
        combined.Obligations.ShouldHaveSingleItem()
            .Which.Id.ShouldBe("audit-log");
    }

    [Fact]
    public void CombinePolicyResults_WithAdvice_PreservesMatchingAdvice()
    {
        var advice = new AdviceExpression
        {
            Id = "notify-user",
            AppliesTo = FulfillOn.Deny,
            AttributeAssignments = []
        };

        var results = new[]
        {
            MakePolicyResult(Effect.Deny, "policy-1", advice: [advice]),
            MakePolicyResult(Effect.Permit, "policy-2")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Advice.ShouldHaveSingleItem()
            .Which.Id.ShouldBe("notify-user");
    }

    [Fact]
    public void CombinePolicyResults_Indeterminate_ReturnsIndeterminate()
    {
        var results = new[]
        {
            MakePolicyResult(Effect.Indeterminate, "policy-1"),
            MakePolicyResult(Effect.Permit, "policy-2")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Effect.ShouldBe(Effect.Indeterminate);
    }

    #endregion

    #region Helpers

    private static RuleEvaluationResult MakeRuleResult(Effect effect, Effect ruleEffect = Effect.Permit) =>
        new()
        {
            Effect = effect,
            Rule = new Rule
            {
                Id = $"rule-{Guid.NewGuid():N}",
                Effect = ruleEffect,
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
