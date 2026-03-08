using Encina.Security.ABAC;
using Encina.Security.ABAC.CombiningAlgorithms;
using FluentAssertions;

namespace Encina.UnitTests.Security.ABAC.CombiningAlgorithms;

/// <summary>
/// Unit tests for <see cref="FirstApplicableAlgorithm"/>.
/// Verifies XACML 3.0 §C.3: first non-NotApplicable result wins.
/// </summary>
public sealed class FirstApplicableAlgorithmTests
{
    private readonly FirstApplicableAlgorithm _sut = new();

    [Fact]
    public void AlgorithmId_ReturnsFirstApplicable()
    {
        _sut.AlgorithmId.Should().Be(CombiningAlgorithmId.FirstApplicable);
    }

    [Fact]
    public void CombineRuleResults_FirstPermit_ReturnsPermit()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.Permit),
            MakeRuleResult(Effect.Deny)
        };

        _sut.CombineRuleResults(results).Should().Be(Effect.Permit);
    }

    [Fact]
    public void CombineRuleResults_FirstDeny_ReturnsDeny()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.Deny),
            MakeRuleResult(Effect.Permit)
        };

        _sut.CombineRuleResults(results).Should().Be(Effect.Deny);
    }

    [Fact]
    public void CombineRuleResults_SkipsNotApplicable_ReturnsFirstApplicable()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.NotApplicable),
            MakeRuleResult(Effect.NotApplicable),
            MakeRuleResult(Effect.Permit)
        };

        _sut.CombineRuleResults(results).Should().Be(Effect.Permit);
    }

    [Fact]
    public void CombineRuleResults_AllNotApplicable_ReturnsNotApplicable()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.NotApplicable),
            MakeRuleResult(Effect.NotApplicable)
        };

        _sut.CombineRuleResults(results).Should().Be(Effect.NotApplicable);
    }

    [Fact]
    public void CombineRuleResults_FirstIndeterminate_ReturnsIndeterminate()
    {
        var results = new[]
        {
            MakeRuleResult(Effect.Indeterminate),
            MakeRuleResult(Effect.Permit)
        };

        _sut.CombineRuleResults(results).Should().Be(Effect.Indeterminate);
    }

    [Fact]
    public void CombineRuleResults_Empty_ReturnsNotApplicable()
    {
        _sut.CombineRuleResults(Array.Empty<RuleEvaluationResult>())
            .Should().Be(Effect.NotApplicable);
    }

    [Fact]
    public void CombinePolicyResults_FirstApplicableReturned()
    {
        var results = new[]
        {
            MakePolicyResult(Effect.NotApplicable, "p1"),
            MakePolicyResult(Effect.Permit, "p2"),
            MakePolicyResult(Effect.Deny, "p3")
        };

        var combined = _sut.CombinePolicyResults(results);
        combined.Effect.Should().Be(Effect.Permit);
        combined.PolicyId.Should().Be("p2");
    }

    [Fact]
    public void CombinePolicyResults_Empty_ReturnsNotApplicable()
    {
        var combined = _sut.CombinePolicyResults(Array.Empty<PolicyEvaluationResult>());
        combined.Effect.Should().Be(Effect.NotApplicable);
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

    private static PolicyEvaluationResult MakePolicyResult(Effect effect, string policyId) =>
        new() { Effect = effect, PolicyId = policyId, Obligations = [], Advice = [] };

    #endregion
}
