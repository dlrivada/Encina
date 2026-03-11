using Encina.Compliance.DPIA;

namespace Encina.GuardTests.Compliance.DPIA;

/// <summary>
/// Guard tests for all <see cref="IRiskCriterion"/> implementations to verify null parameter handling.
/// </summary>
public class RiskCriteriaGuardTests
{
    #region AutomatedDecisionMakingCriterion Guards

    /// <summary>
    /// Verifies that EvaluateAsync throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task AutomatedDecisionMaking_EvaluateAsync_NullContext_ThrowsArgumentNullException()
    {
        var sut = new AutomatedDecisionMakingCriterion();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.EvaluateAsync(null!));
        ex.ParamName.ShouldBe("context");
    }

    #endregion

    #region LargeScaleProcessingCriterion Guards

    /// <summary>
    /// Verifies that EvaluateAsync throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task LargeScaleProcessing_EvaluateAsync_NullContext_ThrowsArgumentNullException()
    {
        var sut = new LargeScaleProcessingCriterion();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.EvaluateAsync(null!));
        ex.ParamName.ShouldBe("context");
    }

    #endregion

    #region SpecialCategoryDataCriterion Guards

    /// <summary>
    /// Verifies that EvaluateAsync throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task SpecialCategoryData_EvaluateAsync_NullContext_ThrowsArgumentNullException()
    {
        var sut = new SpecialCategoryDataCriterion();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.EvaluateAsync(null!));
        ex.ParamName.ShouldBe("context");
    }

    #endregion

    #region SystematicMonitoringCriterion Guards

    /// <summary>
    /// Verifies that EvaluateAsync throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task SystematicMonitoring_EvaluateAsync_NullContext_ThrowsArgumentNullException()
    {
        var sut = new SystematicMonitoringCriterion();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.EvaluateAsync(null!));
        ex.ParamName.ShouldBe("context");
    }

    #endregion

    #region SystematicProfilingCriterion Guards

    /// <summary>
    /// Verifies that EvaluateAsync throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task SystematicProfiling_EvaluateAsync_NullContext_ThrowsArgumentNullException()
    {
        var sut = new SystematicProfilingCriterion();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.EvaluateAsync(null!));
        ex.ParamName.ShouldBe("context");
    }

    #endregion

    #region VulnerableSubjectsCriterion Guards

    /// <summary>
    /// Verifies that EvaluateAsync throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task VulnerableSubjects_EvaluateAsync_NullContext_ThrowsArgumentNullException()
    {
        var sut = new VulnerableSubjectsCriterion();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.EvaluateAsync(null!));
        ex.ParamName.ShouldBe("context");
    }

    #endregion
}
