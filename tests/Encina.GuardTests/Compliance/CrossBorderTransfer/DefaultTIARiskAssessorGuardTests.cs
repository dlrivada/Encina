#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Services;
using Encina.Compliance.DataResidency;

namespace Encina.GuardTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Guard tests for <see cref="DefaultTIARiskAssessor"/> to verify null and empty parameter handling.
/// </summary>
public class DefaultTIARiskAssessorGuardTests
{
    private readonly IAdequacyDecisionProvider _adequacyProvider = Substitute.For<IAdequacyDecisionProvider>();
    private readonly ILogger<DefaultTIARiskAssessor> _logger = NullLogger<DefaultTIARiskAssessor>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when adequacyProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullAdequacyProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultTIARiskAssessor(null!, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("adequacyProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultTIARiskAssessor(_adequacyProvider, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region AssessRiskAsync Guards

    /// <summary>
    /// Verifies that AssessRiskAsync throws ArgumentException when destinationCountryCode is null.
    /// </summary>
    [Fact]
    public async Task AssessRiskAsync_NullDestinationCountryCode_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentException>(
            async () => await sut.AssessRiskAsync(null!, "personal-data"));
        ex.ParamName.ShouldBe("destinationCountryCode");
    }

    /// <summary>
    /// Verifies that AssessRiskAsync throws ArgumentException when destinationCountryCode is empty.
    /// </summary>
    [Fact]
    public async Task AssessRiskAsync_EmptyDestinationCountryCode_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentException>(
            async () => await sut.AssessRiskAsync("", "personal-data"));
        ex.ParamName.ShouldBe("destinationCountryCode");
    }

    /// <summary>
    /// Verifies that AssessRiskAsync throws ArgumentException when destinationCountryCode is whitespace.
    /// </summary>
    [Fact]
    public async Task AssessRiskAsync_WhitespaceDestinationCountryCode_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentException>(
            async () => await sut.AssessRiskAsync("   ", "personal-data"));
        ex.ParamName.ShouldBe("destinationCountryCode");
    }

    /// <summary>
    /// Verifies that AssessRiskAsync throws ArgumentException when dataCategory is null.
    /// </summary>
    [Fact]
    public async Task AssessRiskAsync_NullDataCategory_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentException>(
            async () => await sut.AssessRiskAsync("US", null!));
        ex.ParamName.ShouldBe("dataCategory");
    }

    /// <summary>
    /// Verifies that AssessRiskAsync throws ArgumentException when dataCategory is empty.
    /// </summary>
    [Fact]
    public async Task AssessRiskAsync_EmptyDataCategory_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentException>(
            async () => await sut.AssessRiskAsync("US", ""));
        ex.ParamName.ShouldBe("dataCategory");
    }

    /// <summary>
    /// Verifies that AssessRiskAsync throws ArgumentException when dataCategory is whitespace.
    /// </summary>
    [Fact]
    public async Task AssessRiskAsync_WhitespaceDataCategory_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentException>(
            async () => await sut.AssessRiskAsync("US", "   "));
        ex.ParamName.ShouldBe("dataCategory");
    }

    #endregion

    #region Helpers

    private DefaultTIARiskAssessor CreateSut() =>
        new(_adequacyProvider, _logger);

    #endregion
}
