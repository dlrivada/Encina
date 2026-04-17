#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using Shouldly;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="LargeScaleProcessingCriterion"/>.
/// </summary>
public class LargeScaleProcessingCriterionTests
{
    private readonly LargeScaleProcessingCriterion _sut = new();

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
        _sut.Name.ShouldBe("Large-Scale Processing");
    }

    #endregion

    #region EvaluateAsync Tests

    [Fact]
    public async Task EvaluateAsync_NullContext_ThrowsArgumentNullException()
    {
        var act = async () => await _sut.EvaluateAsync(null!);
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task EvaluateAsync_NoLargeScaleTrigger_ReturnsNull()
    {
        var context = CreateContext(HighRiskTriggers.BiometricData);

        var result = await _sut.EvaluateAsync(context);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task EvaluateAsync_LargeScaleOnly_ReturnsMedium()
    {
        var context = CreateContext(HighRiskTriggers.LargeScaleProcessing);

        var result = await _sut.EvaluateAsync(context);

        result.ShouldNotBeNull();
        result!.Level.ShouldBe(RiskLevel.Medium);
        result.Category.ShouldBe("Large-Scale Processing");
    }

    [Fact]
    public async Task EvaluateAsync_LargeScaleWithOtherFactors_ReturnsHigh()
    {
        var context = CreateContext(
            HighRiskTriggers.LargeScaleProcessing,
            HighRiskTriggers.BiometricData);

        var result = await _sut.EvaluateAsync(context);

        result.ShouldNotBeNull();
        result!.Level.ShouldBe(RiskLevel.High);
    }

    #endregion
}
