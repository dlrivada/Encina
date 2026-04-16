#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using Shouldly;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="SystematicMonitoringCriterion"/>.
/// </summary>
public class SystematicMonitoringCriterionTests
{
    private readonly SystematicMonitoringCriterion _sut = new();

    private static DPIAContext CreateContext(params string[] triggers) => new()
    {
        RequestType = typeof(object),
        DataCategories = [],
        HighRiskTriggers = triggers,
    };

    [Fact]
    public void Name_ShouldReturnExpectedValue()
    {
        _sut.Name.ShouldBe("Systematic Monitoring (Art. 35(3)(c))");
    }

    [Fact]
    public async Task EvaluateAsync_NullContext_ThrowsArgumentNullException()
    {
        var act = async () => await _sut.EvaluateAsync(null!);
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task EvaluateAsync_NoPublicMonitoringTrigger_ReturnsNull()
    {
        var context = CreateContext(HighRiskTriggers.LargeScaleProcessing);

        var result = await _sut.EvaluateAsync(context);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task EvaluateAsync_PublicMonitoringTrigger_ReturnsHigh()
    {
        var context = CreateContext(HighRiskTriggers.PublicMonitoring);

        var result = await _sut.EvaluateAsync(context);

        result.ShouldNotBeNull();
        result!.Level.ShouldBe(RiskLevel.High);
        result.Category.ShouldBe("Systematic Monitoring");
        result.MitigationSuggestion.ShouldNotBeNullOrWhiteSpace();
    }
}
