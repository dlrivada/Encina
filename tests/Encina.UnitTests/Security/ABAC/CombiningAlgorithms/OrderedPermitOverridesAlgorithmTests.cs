using Encina.Security.ABAC;
using Encina.Security.ABAC.CombiningAlgorithms;
using Shouldly;

namespace Encina.UnitTests.Security.ABAC.CombiningAlgorithms;

/// <summary>
/// Unit tests for <see cref="OrderedPermitOverridesAlgorithm"/>.
/// Verifies XACML 3.0 §C.8: same semantics as PermitOverrides with
/// ordered evaluation guarantee. Tests verify the AlgorithmId distinction
/// and behavioral equivalence with PermitOverrides.
/// </summary>
public sealed class OrderedPermitOverridesAlgorithmTests
{
    private readonly OrderedPermitOverridesAlgorithm _sut = new();

    [Fact]
    public void AlgorithmId_ReturnsOrderedPermitOverrides()
    {
        _sut.AlgorithmId.ShouldBe(CombiningAlgorithmId.OrderedPermitOverrides);
    }

    [Fact]
    public void AlgorithmId_DiffersFromPermitOverrides()
    {
        var permitOverrides = new PermitOverridesAlgorithm();

        _sut.AlgorithmId.ShouldNotBe(permitOverrides.AlgorithmId,
            "OrderedPermitOverrides has a distinct AlgorithmId from PermitOverrides");
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
            "Permit overrides Deny — same as PermitOverrides");
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

    [Fact]
    public void CombineRuleResults_DenyAndIndeterminateD_ReturnsIndeterminate()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.Deny),
            MakeRuleResult(Effect.Indeterminate, ruleEffect: Effect.Deny)
        };

        _sut.CombineRuleResults(results).ShouldBe(Effect.Indeterminate);
    }

    [Fact]
    public void CombinePolicyResults_DenyAndPermit_ReturnsPermit()
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
        var obligation = new Obligation
        {
            Id = "audit",
            FulfillOn = FulfillOn.Permit,
            AttributeAssignments = []
        };

        var results = new[]
        {
            MakePolicyResult(Effect.Permit, "p1", obligations: [obligation]),
            MakePolicyResult(Effect.Deny, "p2")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Effect.ShouldBe(Effect.Permit);
        combined.Obligations.ShouldHaveSingleItem()
            .Which.Id.ShouldBe("audit");
    }

    [Fact]
    public void CombineRuleResults_BehavioralEquivalenceWithPermitOverrides()
    {
        var permitOverrides = new PermitOverridesAlgorithm();
        var results = new[]
        {
            MakeRuleResult(Effect.Deny),
            MakeRuleResult(Effect.NotApplicable),
            MakeRuleResult(Effect.Permit),
            MakeRuleResult(Effect.Indeterminate, ruleEffect: Effect.Deny)
        };

        var orderedResult = _sut.CombineRuleResults(results);
        var standardResult = permitOverrides.CombineRuleResults(results);

        orderedResult.ShouldBe(standardResult,
            "Ordered variant must produce identical results to PermitOverrides");
    }

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
