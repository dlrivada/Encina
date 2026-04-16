#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using Shouldly;

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
        _sut.Name.ShouldBe("Systematic Profiling (Art. 35(3)(a))");
    }

    [Fact]
    public async Task EvaluateAsync_NullContext_ThrowsArgumentNullException()
    {
        var act = async () => await _sut.EvaluateAsync(null!);
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task EvaluateAsync_NoProfilingTrigger_ReturnsNull()
    {
        var context = CreateContext(HighRiskTriggers.LargeScaleProcessing);

        var result = await _sut.EvaluateAsync(context);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task EvaluateAsync_ProfilingOnly_ReturnsHigh()
    {
        var context = CreateContext(HighRiskTriggers.SystematicProfiling);

        var result = await _sut.EvaluateAsync(context);

        result.ShouldNotBeNull();
        result!.Level.ShouldBe(RiskLevel.High);
        result.Category.ShouldBe("Systematic Profiling");
    }

    [Fact]
    public async Task EvaluateAsync_ProfilingWithAutomatedDecisionMaking_ReturnsVeryHigh()
    {
        var context = CreateContext(
            HighRiskTriggers.SystematicProfiling,
            HighRiskTriggers.AutomatedDecisionMaking);

        var result = await _sut.EvaluateAsync(context);

        result.ShouldNotBeNull();
        result!.Level.ShouldBe(RiskLevel.VeryHigh);
    }
}
