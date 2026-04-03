using Encina.Security.ABAC;
using Encina.Security.ABAC.CombiningAlgorithms;

using Shouldly;

using Rule = Encina.Security.ABAC.Rule;

namespace Encina.GuardTests.Security.ABAC.CombiningAlgorithms;

/// <summary>
/// Guard clause tests for <see cref="CombiningAlgorithmFactory"/>.
/// </summary>
public class CombiningAlgorithmFactoryGuardTests
{
    private readonly CombiningAlgorithmFactory _factory = new();

    #region GetAlgorithm Guards

    [Fact]
    public void GetAlgorithm_InvalidAlgorithmId_ThrowsArgumentOutOfRangeException()
    {
        var act = () => _factory.GetAlgorithm((CombiningAlgorithmId)999);
        act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe("algorithmId");
    }

    #endregion

    #region GetAlgorithm Valid Algorithms

    [Theory]
    [InlineData(CombiningAlgorithmId.DenyOverrides)]
    [InlineData(CombiningAlgorithmId.PermitOverrides)]
    [InlineData(CombiningAlgorithmId.FirstApplicable)]
    [InlineData(CombiningAlgorithmId.OnlyOneApplicable)]
    [InlineData(CombiningAlgorithmId.DenyUnlessPermit)]
    [InlineData(CombiningAlgorithmId.PermitUnlessDeny)]
    [InlineData(CombiningAlgorithmId.OrderedDenyOverrides)]
    [InlineData(CombiningAlgorithmId.OrderedPermitOverrides)]
    public void GetAlgorithm_ValidId_ReturnsAlgorithm(CombiningAlgorithmId algorithmId)
    {
        var algorithm = _factory.GetAlgorithm(algorithmId);
        algorithm.ShouldNotBeNull();
    }

    #endregion

    #region Algorithm Behavior

    private static RuleEvaluationResult MakeResult(string ruleId, Effect effect) => new()
    {
        Rule = new Rule { Id = ruleId, Effect = effect, Obligations = [], Advice = [] },
        Effect = effect,
        Obligations = [],
        Advice = []
    };

    [Fact]
    public void DenyOverrides_EmptyResults_ReturnsNotApplicable()
    {
        var algorithm = _factory.GetAlgorithm(CombiningAlgorithmId.DenyOverrides);
        var result = algorithm.CombineRuleResults([]);
        result.ShouldBe(Effect.NotApplicable);
    }

    [Fact]
    public void PermitOverrides_EmptyResults_ReturnsNotApplicable()
    {
        var algorithm = _factory.GetAlgorithm(CombiningAlgorithmId.PermitOverrides);
        var result = algorithm.CombineRuleResults([]);
        result.ShouldBe(Effect.NotApplicable);
    }

    [Fact]
    public void DenyUnlessPermit_EmptyResults_ReturnsDeny()
    {
        var algorithm = _factory.GetAlgorithm(CombiningAlgorithmId.DenyUnlessPermit);
        var result = algorithm.CombineRuleResults([]);
        result.ShouldBe(Effect.Deny);
    }

    [Fact]
    public void PermitUnlessDeny_EmptyResults_ReturnsPermit()
    {
        var algorithm = _factory.GetAlgorithm(CombiningAlgorithmId.PermitUnlessDeny);
        var result = algorithm.CombineRuleResults([]);
        result.ShouldBe(Effect.Permit);
    }

    [Fact]
    public void FirstApplicable_EmptyResults_ReturnsNotApplicable()
    {
        var algorithm = _factory.GetAlgorithm(CombiningAlgorithmId.FirstApplicable);
        var result = algorithm.CombineRuleResults([]);
        result.ShouldBe(Effect.NotApplicable);
    }

    [Fact]
    public void DenyOverrides_WithDeny_ReturnsDeny()
    {
        var algorithm = _factory.GetAlgorithm(CombiningAlgorithmId.DenyOverrides);
        var results = new List<RuleEvaluationResult>
        {
            MakeResult("r1", Effect.Permit),
            MakeResult("r2", Effect.Deny)
        };
        var result = algorithm.CombineRuleResults(results);
        result.ShouldBe(Effect.Deny);
    }

    [Fact]
    public void PermitOverrides_WithPermit_ReturnsPermit()
    {
        var algorithm = _factory.GetAlgorithm(CombiningAlgorithmId.PermitOverrides);
        var results = new List<RuleEvaluationResult>
        {
            MakeResult("r1", Effect.Deny),
            MakeResult("r2", Effect.Permit)
        };
        var result = algorithm.CombineRuleResults(results);
        result.ShouldBe(Effect.Permit);
    }

    [Fact]
    public void FirstApplicable_ReturnsFirst()
    {
        var algorithm = _factory.GetAlgorithm(CombiningAlgorithmId.FirstApplicable);
        var results = new List<RuleEvaluationResult>
        {
            MakeResult("r1", Effect.Permit),
            MakeResult("r2", Effect.Deny)
        };
        var result = algorithm.CombineRuleResults(results);
        result.ShouldBe(Effect.Permit);
    }

    #endregion
}
