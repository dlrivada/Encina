#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="SystematicProfilingCriterion"/>.
/// </summary>
public class SystematicProfilingCriterionTests
{
    private readonly SystematicProfilingCriterion _sut = new();

    private static DPIAContext CreateContext(params string[] triggers) => new()
    {
        RequestType = typeof(object),
        DataCategories = [],
        HighRiskTriggers = triggers,
    };

    [Fact]
    public void Name_ShouldReturnExpectedValue()
    {
        _sut.Name.Should().Be("Systematic Profiling (Art. 35(3)(a))");
    }

    [Fact]
    public async Task EvaluateAsync_NullContext_ThrowsArgumentNullException()
    {
        var act = async () => await _sut.EvaluateAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task EvaluateAsync_NoProfilingTrigger_ReturnsNull()
    {
        var context = CreateContext(HighRiskTriggers.LargeScaleProcessing);

        var result = await _sut.EvaluateAsync(context);

        result.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateAsync_ProfilingOnly_ReturnsHigh()
    {
        var context = CreateContext(HighRiskTriggers.SystematicProfiling);

        var result = await _sut.EvaluateAsync(context);

        result.Should().NotBeNull();
        result!.Level.Should().Be(RiskLevel.High);
        result.Category.Should().Be("Systematic Profiling");
    }

    [Fact]
    public async Task EvaluateAsync_ProfilingWithAutomatedDecisionMaking_ReturnsVeryHigh()
    {
        var context = CreateContext(
            HighRiskTriggers.SystematicProfiling,
            HighRiskTriggers.AutomatedDecisionMaking);

        var result = await _sut.EvaluateAsync(context);

        result.Should().NotBeNull();
        result!.Level.Should().Be(RiskLevel.VeryHigh);
    }
}
