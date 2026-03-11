#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="AutomatedDecisionMakingCriterion"/>.
/// </summary>
public class AutomatedDecisionMakingCriterionTests
{
    private readonly AutomatedDecisionMakingCriterion _sut = new();

    private static DPIAContext CreateContext(params string[] triggers) => new()
    {
        RequestType = typeof(object),
        DataCategories = [],
        HighRiskTriggers = triggers,
    };

    #region Name Tests

    [Fact]
    public void Name_ShouldReturnExpectedValue()
    {
        _sut.Name.Should().Be("Automated Decision-Making (Art. 22)");
    }

    #endregion

    #region EvaluateAsync Tests

    [Fact]
    public async Task EvaluateAsync_NullContext_ThrowsArgumentNullException()
    {
        var act = async () => await _sut.EvaluateAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task EvaluateAsync_NoAutomatedDecisionMakingTrigger_ReturnsNull()
    {
        var context = CreateContext(HighRiskTriggers.LargeScaleProcessing);

        var result = await _sut.EvaluateAsync(context);

        result.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateAsync_EmptyTriggers_ReturnsNull()
    {
        var context = CreateContext();

        var result = await _sut.EvaluateAsync(context);

        result.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateAsync_AutomatedDecisionMakingOnly_ReturnsHigh()
    {
        var context = CreateContext(HighRiskTriggers.AutomatedDecisionMaking);

        var result = await _sut.EvaluateAsync(context);

        result.Should().NotBeNull();
        result!.Level.Should().Be(RiskLevel.High);
        result.Category.Should().Be("Automated Decision-Making");
        result.MitigationSuggestion.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task EvaluateAsync_AutomatedDecisionMakingWithProfiling_ReturnsVeryHigh()
    {
        var context = CreateContext(
            HighRiskTriggers.AutomatedDecisionMaking,
            HighRiskTriggers.SystematicProfiling);

        var result = await _sut.EvaluateAsync(context);

        result.Should().NotBeNull();
        result!.Level.Should().Be(RiskLevel.VeryHigh);
        result.Category.Should().Be("Automated Decision-Making");
    }

    [Fact]
    public async Task EvaluateAsync_AutomatedDecisionMakingWithOtherTrigger_ReturnsHigh()
    {
        var context = CreateContext(
            HighRiskTriggers.AutomatedDecisionMaking,
            HighRiskTriggers.LargeScaleProcessing);

        var result = await _sut.EvaluateAsync(context);

        result.Should().NotBeNull();
        result!.Level.Should().Be(RiskLevel.High);
    }

    #endregion
}
