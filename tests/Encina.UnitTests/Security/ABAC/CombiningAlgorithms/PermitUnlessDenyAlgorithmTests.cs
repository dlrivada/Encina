using Encina.Security.ABAC;
using Encina.Security.ABAC.CombiningAlgorithms;
using FluentAssertions;

namespace Encina.UnitTests.Security.ABAC.CombiningAlgorithms;

/// <summary>
/// Unit tests for <see cref="PermitUnlessDenyAlgorithm"/>.
/// Verifies XACML 3.0 §C.6: default Permit unless explicit Deny.
/// CRITICAL: This algorithm NEVER returns NotApplicable or Indeterminate.
/// </summary>
public sealed class PermitUnlessDenyAlgorithmTests
{
    private readonly PermitUnlessDenyAlgorithm _sut = new();

    [Fact]
    public void AlgorithmId_ReturnsPermitUnlessDeny()
    {
        _sut.AlgorithmId.Should().Be(CombiningAlgorithmId.PermitUnlessDeny);
    }

    [Fact]
    public void CombineRuleResults_SinglePermit_ReturnsPermit()
    {
        var results = new[] { MakeRuleResult(Effect.Permit) };
        _sut.CombineRuleResults(results).Should().Be(Effect.Permit);
    }

    [Fact]
    public void CombineRuleResults_SingleDeny_ReturnsDeny()
    {
        var results = new[] { MakeRuleResult(Effect.Deny) };
        _sut.CombineRuleResults(results).Should().Be(Effect.Deny);
    }

    [Fact]
    public void CombineRuleResults_Empty_ReturnsPermit()
    {
        _sut.CombineRuleResults(Array.Empty<RuleEvaluationResult>())
            .Should().Be(Effect.Permit, "No Deny found, so default is Permit");
    }

    [Fact]
    public void CombineRuleResults_AllNotApplicable_ReturnsPermit()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.NotApplicable),
            MakeRuleResult(Effect.NotApplicable)
        };

        _sut.CombineRuleResults(results).Should().Be(Effect.Permit,
            "PermitUnlessDeny never returns NotApplicable — defaults to Permit");
    }

    [Fact]
    public void CombineRuleResults_AllIndeterminate_ReturnsPermit()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.Indeterminate),
            MakeRuleResult(Effect.Indeterminate)
        };

        _sut.CombineRuleResults(results).Should().Be(Effect.Permit,
            "PermitUnlessDeny never returns Indeterminate — defaults to Permit");
    }

    [Fact]
    public void CombineRuleResults_MixedWithDeny_ReturnsDeny()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.Permit),
            MakeRuleResult(Effect.NotApplicable),
            MakeRuleResult(Effect.Deny)
        };

        _sut.CombineRuleResults(results).Should().Be(Effect.Deny);
    }

    [Theory]
    [InlineData(Effect.NotApplicable)]
    [InlineData(Effect.Indeterminate)]
    [InlineData(Effect.Permit)]
    public void CombineRuleResults_NoDenyPresent_AlwaysReturnsPermit(Effect inputEffect)
    {
        var results = new[] { MakeRuleResult(inputEffect), MakeRuleResult(inputEffect) };
        _sut.CombineRuleResults(results).Should().Be(Effect.Permit);
    }

    [Fact]
    public void CombinePolicyResults_WithDeny_ReturnsDenyWithObligations()
    {
        var obligation = new Obligation { Id = "log-denial", FulfillOn = FulfillOn.Deny, AttributeAssignments = [] };
        var results = new[]
        {
            MakePolicyResult(Effect.Deny, "p1", obligations: [obligation]),
            MakePolicyResult(Effect.Permit, "p2")
        };

        var combined = _sut.CombinePolicyResults(results);
        combined.Effect.Should().Be(Effect.Deny);
        combined.Obligations.Should().ContainSingle().Which.Id.Should().Be("log-denial");
    }

    #region Helpers

    private static RuleEvaluationResult MakeRuleResult(Effect effect) =>
        new()
        {
            Effect = effect,
            Rule = new Rule { Id = $"rule-{Guid.NewGuid():N}", Effect = Effect.Permit, Obligations = [], Advice = [] },
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
