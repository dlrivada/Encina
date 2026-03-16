using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

namespace Encina.GuardTests.Compliance.DPIA;

/// <summary>
/// Guard tests for <see cref="DefaultDPIAAssessmentEngine"/> to verify null parameter handling.
/// </summary>
public class DefaultDPIAAssessmentEngineGuardTests
{
    private readonly IEnumerable<IRiskCriterion> _criteria = [];
    private readonly IDPIATemplateProvider _templateProvider = Substitute.For<IDPIATemplateProvider>();
    private readonly IOptions<DPIAOptions> _options = Options.Create(new DPIAOptions());
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<DefaultDPIAAssessmentEngine> _logger = NullLogger<DefaultDPIAAssessmentEngine>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when criteria is null.
    /// </summary>
    [Fact]
    public void Constructor_NullCriteria_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPIAAssessmentEngine(
            null!, _templateProvider, _options, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("criteria");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when templateProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTemplateProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPIAAssessmentEngine(
            _criteria, null!, _options, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("templateProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPIAAssessmentEngine(
            _criteria, _templateProvider, null!, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when timeProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPIAAssessmentEngine(
            _criteria, _templateProvider, _options, null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPIAAssessmentEngine(
            _criteria, _templateProvider, _options, _timeProvider, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region AssessAsync Guards

    /// <summary>
    /// Verifies that AssessAsync throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task AssessAsync_NullContext_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.AssessAsync(null!));
        ex.ParamName.ShouldBe("context");
    }

    #endregion

    #region RequiresDPIAAsync Guards

    /// <summary>
    /// Verifies that RequiresDPIAAsync throws ArgumentNullException when requestType is null.
    /// </summary>
    [Fact]
    public async Task RequiresDPIAAsync_NullRequestType_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.RequiresDPIAAsync(null!));
        ex.ParamName.ShouldBe("requestType");
    }

    #endregion

    #region Helpers

    private DefaultDPIAAssessmentEngine CreateSut() =>
        new(_criteria, _templateProvider, _options, _timeProvider, _logger);

    #endregion
}
