#pragma warning disable CA2012

using Encina.Compliance.PrivacyByDesign;

namespace Encina.GuardTests.Compliance.PrivacyByDesign;

/// <summary>
/// Guard tests for <see cref="DefaultDataMinimizationAnalyzer"/> to verify null parameter handling.
/// </summary>
public class DefaultDataMinimizationAnalyzerGuardTests
{
    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when timeProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataMinimizationAnalyzer(
            null!, NullLogger<DefaultDataMinimizationAnalyzer>.Instance);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataMinimizationAnalyzer(
            TimeProvider.System, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region AnalyzeAsync Guards

    /// <summary>
    /// Verifies that AnalyzeAsync throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task AnalyzeAsync_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.AnalyzeAsync<object>(null!));
        ex.ParamName.ShouldBe("request");
    }

    #endregion

    #region InspectDefaultsAsync Guards

    /// <summary>
    /// Verifies that InspectDefaultsAsync throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task InspectDefaultsAsync_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.InspectDefaultsAsync<object>(null!));
        ex.ParamName.ShouldBe("request");
    }

    #endregion

    #region Helpers

    private static DefaultDataMinimizationAnalyzer CreateSut() =>
        new(TimeProvider.System, NullLogger<DefaultDataMinimizationAnalyzer>.Instance);

    #endregion
}
