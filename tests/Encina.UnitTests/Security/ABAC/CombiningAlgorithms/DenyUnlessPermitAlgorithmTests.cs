using Encina.Security.ABAC;
using Encina.Security.ABAC.CombiningAlgorithms;
using FluentAssertions;

namespace Encina.UnitTests.Security.ABAC.CombiningAlgorithms;

/// <summary>
/// Unit tests for <see cref="DenyUnlessPermitAlgorithm"/>.
/// Verifies XACML 3.0 §C.5: default Deny unless explicit Permit.
/// CRITICAL: This algorithm NEVER returns NotApplicable or Indeterminate.
/// </summary>
public sealed class DenyUnlessPermitAlgorithmTests
{
    private readonly DenyUnlessPermitAlgorithm _sut = new();

    #region AlgorithmId

    [Fact]
    public void AlgorithmId_ReturnsDenyUnlessPermit()
    {
        _sut.AlgorithmId.Should().Be(CombiningAlgorithmId.DenyUnlessPermit);
    }

    #endregion

    #region CombineRuleResults — Basic

    [Fact]
    public void CombineRuleResults_SinglePermit_ReturnsPermit()
    {
        var results = new[] { MakeRuleResult(Effect.Permit) };

        var effect = _sut.CombineRuleResults(results);

        effect.Should().Be(Effect.Permit);
    }

    [Fact]
    public void CombineRuleResults_SingleDeny_ReturnsDeny()
    {
        var results = new[] { MakeRuleResult(Effect.Deny) };

        var effect = _sut.CombineRuleResults(results);

        effect.Should().Be(Effect.Deny);
    }

    [Fact]
    public void CombineRuleResults_Empty_ReturnsDeny()
    {
        var results = Array.Empty<RuleEvaluationResult>();

        var effect = _sut.CombineRuleResults(results);

        effect.Should().Be(Effect.Deny, "No Permit found, so default is Deny");
    }

    #endregion

    #region CombineRuleResults — Never NotApplicable/Indeterminate

    [Fact]
    public void CombineRuleResults_AllNotApplicable_ReturnsDeny()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.NotApplicable),
            MakeRuleResult(Effect.NotApplicable)
        };

        var effect = _sut.CombineRuleResults(results);

        effect.Should().Be(Effect.Deny,
            "DenyUnlessPermit never returns NotApplicable — defaults to Deny");
    }

    [Fact]
    public void CombineRuleResults_AllIndeterminate_ReturnsDeny()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.Indeterminate),
            MakeRuleResult(Effect.Indeterminate)
        };

        var effect = _sut.CombineRuleResults(results);

        effect.Should().Be(Effect.Deny,
            "DenyUnlessPermit never returns Indeterminate — defaults to Deny");
    }

    [Fact]
    public void CombineRuleResults_NotApplicableAndPermit_ReturnsPermit()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.NotApplicable),
            MakeRuleResult(Effect.Permit)
        };

        var effect = _sut.CombineRuleResults(results);

        effect.Should().Be(Effect.Permit);
    }

    [Fact]
    public void CombineRuleResults_IndeterminateAndPermit_ReturnsPermit()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.Indeterminate),
            MakeRuleResult(Effect.Permit)
        };

        var effect = _sut.CombineRuleResults(results);

        effect.Should().Be(Effect.Permit);
    }

    [Theory]
    [InlineData(Effect.NotApplicable)]
    [InlineData(Effect.Indeterminate)]
    [InlineData(Effect.Deny)]
    public void CombineRuleResults_NoPermitPresent_AlwaysReturnsDeny(Effect inputEffect)
    {
        var results = new[]
        {
            MakeRuleResult(inputEffect),
            MakeRuleResult(inputEffect)
        };

        var effect = _sut.CombineRuleResults(results);

        effect.Should().Be(Effect.Deny);
    }

    [Fact]
    public void CombineRuleResults_MixedWithPermit_AlwaysReturnsPermit()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.Deny),
            MakeRuleResult(Effect.NotApplicable),
            MakeRuleResult(Effect.Indeterminate),
            MakeRuleResult(Effect.Permit)
        };

        var effect = _sut.CombineRuleResults(results);

        effect.Should().Be(Effect.Permit);
    }

    #endregion

    #region CombinePolicyResults

    [Fact]
    public void CombinePolicyResults_AnyPermit_ReturnsPermit()
    {
        var results = new[]
        {
            MakePolicyResult(Effect.Deny, "policy-1"),
            MakePolicyResult(Effect.Permit, "policy-2")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Effect.Should().Be(Effect.Permit);
    }

    [Fact]
    public void CombinePolicyResults_NoPermit_ReturnsDeny()
    {
        var results = new[]
        {
            MakePolicyResult(Effect.Deny, "policy-1"),
            MakePolicyResult(Effect.NotApplicable, "policy-2")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Effect.Should().Be(Effect.Deny);
    }

    [Fact]
    public void CombinePolicyResults_WithObligations_PreservesFromMatchingEffect()
    {
        var obligation = new Obligation
        {
            Id = "audit",
            FulfillOn = FulfillOn.Permit,
            AttributeAssignments = []
        };

        var results = new[]
        {
            MakePolicyResult(Effect.Permit, "policy-1", obligations: [obligation]),
            MakePolicyResult(Effect.Deny, "policy-2")
        };

        var combined = _sut.CombinePolicyResults(results);

        combined.Effect.Should().Be(Effect.Permit);
        combined.Obligations.Should().ContainSingle()
            .Which.Id.Should().Be("audit");
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
        IReadOnlyList<Obligation>? obligations = null) =>
        new()
        {
            Effect = effect,
            PolicyId = policyId,
            Obligations = obligations ?? [],
            Advice = []
        };

    #endregion
}
