#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.Services;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.CrossBorderTransfer.Services;

public class DefaultTIARiskAssessorTests
{
    private readonly IAdequacyDecisionProvider _adequacyProvider;
    private readonly ILogger<DefaultTIARiskAssessor> _logger;
    private readonly DefaultTIARiskAssessor _sut;

    public DefaultTIARiskAssessorTests()
    {
        _adequacyProvider = Substitute.For<IAdequacyDecisionProvider>();
        _logger = NullLogger<DefaultTIARiskAssessor>.Instance;

        _sut = new DefaultTIARiskAssessor(_adequacyProvider, _logger);
    }

    [Fact]
    public async Task AssessRiskAsync_AdequateCountry_ReturnsLowRisk()
    {
        // Arrange
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(true);

        // Act
        var result = await _sut.AssessRiskAsync("JP", "personal-data");

        // Assert
        result.IsRight.Should().BeTrue();
        var assessment = result.Match(Right: a => a, Left: _ => throw new InvalidOperationException("Expected Right"));
        assessment.Score.Should().BeApproximately(0.1, 0.01);
        assessment.Factors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AssessRiskAsync_HighSurveillanceCountry_ReturnsHighRisk()
    {
        // Arrange
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);

        // Act
        var result = await _sut.AssessRiskAsync("CN", "personal-data");

        // Assert
        result.IsRight.Should().BeTrue();
        var assessment = result.Match(Right: a => a, Left: _ => throw new InvalidOperationException("Expected Right"));
        // Base 0.4 + High surveillance 0.4 = 0.8
        assessment.Score.Should().BeApproximately(0.8, 0.01);
        assessment.Factors.Should().Contain(f => f.Contains("surveillance"));
    }

    [Fact]
    public async Task AssessRiskAsync_FiveEyesCountry_ReturnsMediumRisk()
    {
        // Arrange
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);

        // Act
        var result = await _sut.AssessRiskAsync("US", "personal-data");

        // Assert
        result.IsRight.Should().BeTrue();
        var assessment = result.Match(Right: a => a, Left: _ => throw new InvalidOperationException("Expected Right"));
        // Base 0.4 + Five Eyes 0.2 = 0.6
        assessment.Score.Should().BeApproximately(0.6, 0.01);
        assessment.Factors.Should().Contain(f => f.Contains("Five Eyes"));
    }

    [Fact]
    public async Task AssessRiskAsync_NineEyesCountry_ReturnsModerateRisk()
    {
        // Arrange — FR is a Nine Eyes country (not in Five Eyes)
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);

        // Act
        var result = await _sut.AssessRiskAsync("FR", "personal-data");

        // Assert
        result.IsRight.Should().BeTrue();
        var assessment = result.Match(Right: a => a, Left: _ => throw new InvalidOperationException("Expected Right"));
        // Base 0.4 + Nine Eyes 0.15 = 0.55
        assessment.Score.Should().BeApproximately(0.55, 0.01);
        assessment.Factors.Should().Contain(f => f.Contains("Nine Eyes"));
    }

    [Fact]
    public async Task AssessRiskAsync_FourteenEyesCountry_ReturnsLowerModerateRisk()
    {
        // Arrange — DE is a Fourteen Eyes country (not in Nine or Five Eyes)
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);

        // Act
        var result = await _sut.AssessRiskAsync("DE", "personal-data");

        // Assert
        result.IsRight.Should().BeTrue();
        var assessment = result.Match(Right: a => a, Left: _ => throw new InvalidOperationException("Expected Right"));
        // Base 0.4 + Fourteen Eyes 0.1 = 0.5
        assessment.Score.Should().BeApproximately(0.5, 0.01);
        assessment.Factors.Should().Contain(f => f.Contains("Fourteen Eyes"));
    }

    [Fact]
    public async Task AssessRiskAsync_SensitiveDataCategory_IncreasesRisk()
    {
        // Arrange — Non-adequate, non-alliance country with sensitive data
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);

        // Act
        var result = await _sut.AssessRiskAsync("BR", "health-data");

        // Assert
        result.IsRight.Should().BeTrue();
        var assessment = result.Match(Right: a => a, Left: _ => throw new InvalidOperationException("Expected Right"));
        // Base 0.4 + sensitive 0.1 = 0.5
        assessment.Score.Should().BeApproximately(0.5, 0.01);
        assessment.Factors.Should().Contain(f => f.Contains("sensitive"));
    }

    [Fact]
    public async Task AssessRiskAsync_HighSurveillancePlusSensitive_CapsAtOne()
    {
        // Arrange — High surveillance + sensitive data: 0.4 + 0.4 + 0.1 = 0.9, capped at 1.0
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);

        // Act
        var result = await _sut.AssessRiskAsync("CN", "health-data");

        // Assert
        result.IsRight.Should().BeTrue();
        var assessment = result.Match(Right: a => a, Left: _ => throw new InvalidOperationException("Expected Right"));
        assessment.Score.Should().BeLessThanOrEqualTo(1.0);
        // 0.4 + 0.4 + 0.1 = 0.9, still under cap
        assessment.Score.Should().BeApproximately(0.9, 0.01);
    }

    [Fact]
    public async Task AssessRiskAsync_NullDestination_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _sut.AssessRiskAsync(null!, "personal-data");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AssessRiskAsync_NullDataCategory_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _sut.AssessRiskAsync("US", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
