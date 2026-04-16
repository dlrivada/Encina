using Encina.Security.ABAC;
using Encina.Security.ABAC.CombiningAlgorithms;
using Shouldly;

namespace Encina.UnitTests.Security.ABAC.CombiningAlgorithms;

/// <summary>
/// Unit tests for <see cref="OrderedDenyOverridesAlgorithm"/>.
/// Verifies XACML 3.0 §C.7: same semantics as DenyOverrides with
/// ordered evaluation guarantee. Tests verify the AlgorithmId distinction
/// and behavioral equivalence with DenyOverrides.
/// </summary>
public sealed class OrderedDenyOverridesAlgorithmTests
{
    private readonly OrderedDenyOverridesAlgorithm _sut = new();

    [Fact]
    public void AlgorithmId_ReturnsOrderedDenyOverrides()
    {
        _sut.AlgorithmId.ShouldBe(CombiningAlgorithmId.OrderedDenyOverrides);
    }

    [Fact]
    public void AlgorithmId_DiffersFromDenyOverrides()
    {
        var denyOverrides = new DenyOverridesAlgorithm();

        _sut.AlgorithmId.ShouldNotBe(denyOverrides.AlgorithmId,
            "OrderedDenyOverrides has a distinct AlgorithmId from DenyOverrides");
    }

    [Fact]
    public void CombineRuleResults_PermitAndDeny_ReturnsDeny()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.Permit),
            MakeRuleResult(Effect.Deny)
        };

        _sut.CombineRuleResults(results).ShouldBe(Effect.Deny,
            "Deny overrides Permit — same as DenyOverrides");
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
    public void CombineRuleResults_IndeterminateD_ReturnsIndeterminate()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.NotApplicable),
            MakeRuleResult(Effect.Indeterminate, ruleEffect: Effect.Deny)
        };

        _sut.CombineRuleResults(results).ShouldBe(Effect.Indeterminate);
    }

    [Fact]
    public void CombinePolicyResults_PermitAndDeny_ReturnsDeny()
    {
        var results = new[]
        {
            MakePolicyResult(Effect.Permit, "p1"),
            MakePolicyResult(Effect.Deny, "p2")
        };

        _sut.CombinePolicyResults(results).Effect.ShouldBe(Effect.Deny);
    }

    [Fact]
    public void CombinePolicyResults_WithObligations_PreservesFromDeny()
    {
        var obligation = new Obligation
        {
            Id = "audit-log",
            FulfillOn = FulfillOn.Deny,
            AttributeAssignments = []
        };

        var results = new[]
        {
            MakePolicyResult(Effect.Deny, "p1", obligations: [obligation]),
            MakePolicyResult(Effect.Permit, "p2")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Effect.ShouldBe(Effect.Deny);
        combined.Obligations.ShouldHaveSingleItem()
            .Which.Id.ShouldBe("audit-log");
    }

    [Fact]
    public void CombineRuleResults_BehavioralEquivalenceWithDenyOverrides()
    {
        var denyOverrides = new DenyOverridesAlgorithm();
        var results = new[]
        {
            MakeRuleResult(Effect.Permit),
            MakeRuleResult(Effect.NotApplicable),
            MakeRuleResult(Effect.Deny),
            MakeRuleResult(Effect.Indeterminate, ruleEffect: Effect.Permit)
        };

        var orderedResult = _sut.CombineRuleResults(results);
        var standardResult = denyOverrides.CombineRuleResults(results);

        orderedResult.ShouldBe(standardResult,
            "Ordered variant must produce identical results to DenyOverrides");
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
