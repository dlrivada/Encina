using Encina.Security.ABAC;
using Encina.Security.ABAC.CombiningAlgorithms;
using Shouldly;

namespace Encina.UnitTests.Security.ABAC.CombiningAlgorithms;

/// <summary>
/// Unit tests for <see cref="PermitOverridesAlgorithm"/>.
/// Verifies XACML 3.0 §C.2: any Permit overrides all other results.
/// </summary>
public sealed class PermitOverridesAlgorithmTests
{
    private readonly PermitOverridesAlgorithm _sut = new();

    [Fact]
    public void AlgorithmId_ReturnsPermitOverrides()
    {
        _sut.AlgorithmId.ShouldBe(CombiningAlgorithmId.PermitOverrides);
    }

    [Fact]
    public void CombineRuleResults_SinglePermit_ReturnsPermit()
    {
        var results = new[] { MakeRuleResult(Effect.Permit) };
        _sut.CombineRuleResults(results).ShouldBe(Effect.Permit);
    }

    [Fact]
    public void CombineRuleResults_SingleDeny_ReturnsDeny()
    {
        var results = new[] { MakeRuleResult(Effect.Deny) };
        _sut.CombineRuleResults(results).ShouldBe(Effect.Deny);
    }

    [Fact]
    public void CombineRuleResults_PermitAndDeny_ReturnsPermit()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.Deny),
            MakeRuleResult(Effect.Permit)
        };

        _sut.CombineRuleResults(results).ShouldBe(Effect.Permit,
            "Permit overrides Deny per XACML §C.2");
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
    public void CombineRuleResults_DenyAndIndeterminateD_ReturnsIndeterminate()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.Deny),
            MakeRuleResult(Effect.Indeterminate, ruleEffect: Effect.Deny)
        };

        _sut.CombineRuleResults(results).ShouldBe(Effect.Indeterminate,
            "Deny + Indeterminate{{D}} → Indeterminate per PermitOverrides");
    }

    [Fact]
    public void CombineRuleResults_IndeterminateP_ReturnsIndeterminate()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.NotApplicable),
            MakeRuleResult(Effect.Indeterminate, ruleEffect: Effect.Permit)
        };

        _sut.CombineRuleResults(results).ShouldBe(Effect.Indeterminate);
    }

    [Fact]
    public void CombineRuleResults_Empty_ReturnsNotApplicable()
    {
        _sut.CombineRuleResults(Array.Empty<RuleEvaluationResult>())
            .ShouldBe(Effect.NotApplicable);
    }

    [Fact]
    public void CombinePolicyResults_PermitAndDeny_ReturnsPermit()
    {
        var results = new[]
        {
            MakePolicyResult(Effect.Deny, "p1"),
            MakePolicyResult(Effect.Permit, "p2")
        };

        _sut.CombinePolicyResults(results).Effect.ShouldBe(Effect.Permit);
    }

    [Fact]
    public void CombinePolicyResults_WithObligations_PreservesFromPermit()
    {
        var obligation = new Obligation { Id = "audit", FulfillOn = FulfillOn.Permit, AttributeAssignments = [] };
        var results = new[]
        {
            MakePolicyResult(Effect.Permit, "p1", obligations: [obligation]),
            MakePolicyResult(Effect.Deny, "p2")
        };

        var combined = _sut.CombinePolicyResults(results);
        combined.Effect.ShouldBe(Effect.Permit);
        combined.Obligations.ShouldHaveSingleItem().Id.ShouldBe("audit");
    }

    #region Helpers

    private static RuleEvaluationResult MakeRuleResult(Effect effect, Effect ruleEffect = Effect.Permit) =>
        new()
        {
            Effect = effect,
            Rule = new Rule { Id = $"rule-{Guid.NewGuid():N}", Effect = ruleEffect, Obligations = [], Advice = [] },
            Obligations = [],
            Advice = []
        };

    private static PolicyEvaluationResult MakePolicyResult(
        Effect effect, string policyId, IReadOnlyList<Obligation>? obligations = null) =>
        new()
        {
            Effect = effect,
            PolicyId = policyId,
            Obligations = obligations ?? [],
            Advice = []
        };

    #endregion
}
