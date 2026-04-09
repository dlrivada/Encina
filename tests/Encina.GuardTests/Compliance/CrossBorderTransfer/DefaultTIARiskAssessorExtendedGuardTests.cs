#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.Services;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

namespace Encina.GuardTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Extended guard tests for <see cref="DefaultTIARiskAssessor"/> verifying risk assessment
/// logic for different country categories and data categories.
/// </summary>
public class DefaultTIARiskAssessorExtendedGuardTests
{
    private readonly IAdequacyDecisionProvider _adequacyProvider = Substitute.For<IAdequacyDecisionProvider>();
    private readonly ILogger<DefaultTIARiskAssessor> _logger = NullLogger<DefaultTIARiskAssessor>.Instance;

    #region Valid Construction

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        var sut = new DefaultTIARiskAssessor(_adequacyProvider, _logger);

        sut.ShouldNotBeNull();
    }

    #endregion

    #region AssessRiskAsync — Adequacy Decision Countries

    [Fact]
    public async Task AssessRiskAsync_AdequateCountry_ReturnsLowRisk()
    {
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(true);
        var sut = CreateSut();

        var result = await sut.AssessRiskAsync("JP", "personal-data");

        result.IsRight.ShouldBeTrue();
        var assessment = (TIARiskAssessment)result;
        assessment.Score.ShouldBe(0.1);
    }

    #endregion

    #region AssessRiskAsync — High Surveillance Countries

    [Fact]
    public async Task AssessRiskAsync_HighSurveillanceCountry_ReturnsHighRisk()
    {
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);
        var sut = CreateSut();

        var result = await sut.AssessRiskAsync("CN", "personal-data");

        result.IsRight.ShouldBeTrue();
        var assessment = (TIARiskAssessment)result;
        assessment.Score.ShouldBeGreaterThanOrEqualTo(0.8);
    }

    [Fact]
    public async Task AssessRiskAsync_RussiaHighSurveillance_ReturnsHighRisk()
    {
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);
        var sut = CreateSut();

        var result = await sut.AssessRiskAsync("RU", "personal-data");

        result.IsRight.ShouldBeTrue();
        var assessment = (TIARiskAssessment)result;
        assessment.Score.ShouldBeGreaterThanOrEqualTo(0.8);
    }

    #endregion

    #region AssessRiskAsync — Five Eyes Countries

    [Fact]
    public async Task AssessRiskAsync_FiveEyesCountry_ReturnsMediumRisk()
    {
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);
        var sut = CreateSut();

        var result = await sut.AssessRiskAsync("US", "personal-data");

        result.IsRight.ShouldBeTrue();
        var assessment = (TIARiskAssessment)result;
        assessment.Score.ShouldBe(0.6, 0.01);
    }

    [Fact]
    public async Task AssessRiskAsync_AustraliaFiveEyes_ReturnsMediumRisk()
    {
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);
        var sut = CreateSut();

        var result = await sut.AssessRiskAsync("AU", "personal-data");

        result.IsRight.ShouldBeTrue();
        var assessment = (TIARiskAssessment)result;
        assessment.Score.ShouldBe(0.6, 0.01);
    }

    #endregion

    #region AssessRiskAsync — Nine Eyes Countries

    [Fact]
    public async Task AssessRiskAsync_NineEyesCountry_ReturnsModerateRisk()
    {
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);
        var sut = CreateSut();

        // DK is Nine Eyes but not Five Eyes
        var result = await sut.AssessRiskAsync("DK", "personal-data");

        result.IsRight.ShouldBeTrue();
        var assessment = (TIARiskAssessment)result;
        assessment.Score.ShouldBe(0.55, 0.01);
    }

    #endregion

    #region AssessRiskAsync — Fourteen Eyes Countries

    [Fact]
    public async Task AssessRiskAsync_FourteenEyesCountry_ReturnsLowerModerateRisk()
    {
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);
        var sut = CreateSut();

        // BE is Fourteen Eyes but not Nine/Five Eyes
        var result = await sut.AssessRiskAsync("BE", "personal-data");

        result.IsRight.ShouldBeTrue();
        var assessment = (TIARiskAssessment)result;
        assessment.Score.ShouldBe(0.5, 0.01);
    }

    #endregion

    #region AssessRiskAsync — Sensitive Data Categories

    [Fact]
    public async Task AssessRiskAsync_HealthData_IncreasesRisk()
    {
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);
        var sut = CreateSut();

        var normalResult = await sut.AssessRiskAsync("US", "personal-data");
        var healthResult = await sut.AssessRiskAsync("US", "health-data");

        var normalAssessment = (TIARiskAssessment)normalResult;
        var healthAssessment = (TIARiskAssessment)healthResult;
        healthAssessment.Score.ShouldBeGreaterThan(normalAssessment.Score);
    }

    [Fact]
    public async Task AssessRiskAsync_GeneticData_IncreasesRisk()
    {
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);
        var sut = CreateSut();

        var result = await sut.AssessRiskAsync("US", "genetic-records");

        var assessment = (TIARiskAssessment)result;
        assessment.Score.ShouldBe(0.7, 0.01);
    }

    [Fact]
    public async Task AssessRiskAsync_BiometricData_IncreasesRisk()
    {
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);
        var sut = CreateSut();

        var result = await sut.AssessRiskAsync("US", "biometric-records");

        var assessment = (TIARiskAssessment)result;
        assessment.Score.ShouldBe(0.7, 0.01);
    }

    [Fact]
    public async Task AssessRiskAsync_HighSurveillancePlusSensitive_CapsAtOne()
    {
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);
        var sut = CreateSut();

        var result = await sut.AssessRiskAsync("CN", "health-data");

        var assessment = (TIARiskAssessment)result;
        assessment.Score.ShouldBeLessThanOrEqualTo(1.0);
    }

    #endregion

    #region AssessRiskAsync — Non-Allied Country

    [Fact]
    public async Task AssessRiskAsync_NonAlliedNonSurveillanceCountry_ReturnsBaseRisk()
    {
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);
        var sut = CreateSut();

        var result = await sut.AssessRiskAsync("BR", "personal-data");

        result.IsRight.ShouldBeTrue();
        var assessment = (TIARiskAssessment)result;
        assessment.Score.ShouldBe(0.4, 0.01);
    }

    #endregion

    #region Helpers

    private DefaultTIARiskAssessor CreateSut() =>
        new(_adequacyProvider, _logger);

    #endregion
}
