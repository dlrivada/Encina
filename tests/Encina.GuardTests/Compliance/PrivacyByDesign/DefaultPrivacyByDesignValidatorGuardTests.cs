#pragma warning disable CA2012

using Encina.Compliance.PrivacyByDesign;

namespace Encina.GuardTests.Compliance.PrivacyByDesign;

/// <summary>
/// Guard tests for <see cref="DefaultPrivacyByDesignValidator"/> to verify null parameter handling.
/// </summary>
public class DefaultPrivacyByDesignValidatorGuardTests
{
    private readonly IDataMinimizationAnalyzer _analyzer = Substitute.For<IDataMinimizationAnalyzer>();
    private readonly IPurposeRegistry _registry = Substitute.For<IPurposeRegistry>();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<DefaultPrivacyByDesignValidator> _logger = NullLogger<DefaultPrivacyByDesignValidator>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when analyzer is null.
    /// </summary>
    [Fact]
    public void Constructor_NullAnalyzer_ThrowsArgumentNullException()
    {
        var act = () => new DefaultPrivacyByDesignValidator(
            null!, _registry, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("analyzer");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when purposeRegistry is null.
    /// </summary>
    [Fact]
    public void Constructor_NullPurposeRegistry_ThrowsArgumentNullException()
    {
        var act = () => new DefaultPrivacyByDesignValidator(
            _analyzer, null!, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("purposeRegistry");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when timeProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultPrivacyByDesignValidator(
            _analyzer, _registry, null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultPrivacyByDesignValidator(
            _analyzer, _registry, _timeProvider, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region ValidateAsync Guards

    /// <summary>
    /// Verifies that ValidateAsync throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.ValidateAsync<object>(null!));
        ex.ParamName.ShouldBe("request");
    }

    #endregion

    #region AnalyzeMinimizationAsync Guards

    /// <summary>
    /// Verifies that AnalyzeMinimizationAsync throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task AnalyzeMinimizationAsync_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.AnalyzeMinimizationAsync<object>(null!));
        ex.ParamName.ShouldBe("request");
    }

    #endregion

    #region ValidatePurposeLimitationAsync Guards

    /// <summary>
    /// Verifies that ValidatePurposeLimitationAsync throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task ValidatePurposeLimitationAsync_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.ValidatePurposeLimitationAsync<object>(null!, "purpose"));
        ex.ParamName.ShouldBe("request");
    }

    /// <summary>
    /// Verifies that ValidatePurposeLimitationAsync throws ArgumentNullException when purpose is null.
    /// </summary>
    [Fact]
    public async Task ValidatePurposeLimitationAsync_NullPurpose_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.ValidatePurposeLimitationAsync(new object(), null!));
        ex.ParamName.ShouldBe("purpose");
    }

    #endregion

    #region ValidateDefaultsAsync Guards

    /// <summary>
    /// Verifies that ValidateDefaultsAsync throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task ValidateDefaultsAsync_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.ValidateDefaultsAsync<object>(null!));
        ex.ParamName.ShouldBe("request");
    }

    #endregion

    #region Helpers

    private DefaultPrivacyByDesignValidator CreateSut() =>
        new(_analyzer, _registry, _timeProvider, _logger);

    #endregion
}
