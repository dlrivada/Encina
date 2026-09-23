#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.Services;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
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
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(Right: a => a, Left: _ => throw new InvalidOperationException("Expected Right"));
        assessment.Score.ShouldBeInRange(0.1 - 0.01, 0.1 + 0.01);
        assessment.Factors.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task AssessRiskAsync_HighSurveillanceCountry_ReturnsHighRisk()
    {
        // Arrange
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);

        // Act
        var result = await _sut.AssessRiskAsync("CN", "personal-data");

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(Right: a => a, Left: _ => throw new InvalidOperationException("Expected Right"));
        // Base 0.4 + High surveillance 0.4 = 0.8
        assessment.Score.ShouldBeInRange(0.8 - 0.01, 0.8 + 0.01);
        assessment.Factors.ShouldContain(f => f.Contains("surveillance"));
    }

    [Fact]
    public async Task AssessRiskAsync_FiveEyesCountry_ReturnsMediumRisk()
    {
        // Arrange
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);

        // Act
        var result = await _sut.AssessRiskAsync("US", "personal-data");

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(Right: a => a, Left: _ => throw new InvalidOperationException("Expected Right"));
        // Base 0.4 + Five Eyes 0.2 = 0.6
        assessment.Score.ShouldBeInRange(0.6 - 0.01, 0.6 + 0.01);
        assessment.Factors.ShouldContain(f => f.Contains("Five Eyes"));
    }

    [Fact]
    public async Task AssessRiskAsync_NineEyesCountry_ReturnsModerateRisk()
    {
        // Arrange — FR is a Nine Eyes country (not in Five Eyes)
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);

        // Act
        var result = await _sut.AssessRiskAsync("FR", "personal-data");

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(Right: a => a, Left: _ => throw new InvalidOperationException("Expected Right"));
        // Base 0.4 + Nine Eyes 0.15 = 0.55
        assessment.Score.ShouldBeInRange(0.55 - 0.01, 0.55 + 0.01);
        assessment.Factors.ShouldContain(f => f.Contains("Nine Eyes"));
    }

    [Fact]
    public async Task AssessRiskAsync_FourteenEyesCountry_ReturnsLowerModerateRisk()
    {
        // Arrange — DE is a Fourteen Eyes country (not in Nine or Five Eyes)
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);

        // Act
        var result = await _sut.AssessRiskAsync("DE", "personal-data");

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(Right: a => a, Left: _ => throw new InvalidOperationException("Expected Right"));
        // Base 0.4 + Fourteen Eyes 0.1 = 0.5
        assessment.Score.ShouldBeInRange(0.5 - 0.01, 0.5 + 0.01);
        assessment.Factors.ShouldContain(f => f.Contains("Fourteen Eyes"));
    }

    [Fact]
    public async Task AssessRiskAsync_SensitiveDataCategory_IncreasesRisk()
    {
        // Arrange — Non-adequate, non-alliance country with sensitive data
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);

        // Act
        var result = await _sut.AssessRiskAsync("BR", "health-data");

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(Right: a => a, Left: _ => throw new InvalidOperationException("Expected Right"));
        // Base 0.4 + sensitive 0.1 = 0.5
        assessment.Score.ShouldBeInRange(0.5 - 0.01, 0.5 + 0.01);
        assessment.Factors.ShouldContain(f => f.Contains("sensitive"));
    }

    [Fact]
    public async Task AssessRiskAsync_HighSurveillancePlusSensitive_CapsAtOne()
    {
        // Arrange — High surveillance + sensitive data: 0.4 + 0.4 + 0.1 = 0.9, capped at 1.0
        _adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);

        // Act
        var result = await _sut.AssessRiskAsync("CN", "health-data");

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(Right: a => a, Left: _ => throw new InvalidOperationException("Expected Right"));
        assessment.Score.ShouldBeLessThanOrEqualTo(1.0);
        // 0.4 + 0.4 + 0.1 = 0.9, still under cap
        assessment.Score.ShouldBeInRange(0.9 - 0.01, 0.9 + 0.01);
    }

    [Fact]
    public async Task AssessRiskAsync_NullDestination_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _sut.AssessRiskAsync(null!, "personal-data");

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task AssessRiskAsync_NullDataCategory_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _sut.AssessRiskAsync("US", null!);

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    #region Real IAdequacyDecisionProvider — partial adequacy fail-closed (#1145)

    // These tests use the real DefaultAdequacyDecisionProvider instead of a mock. The assessor
    // calls IAdequacyDecisionProvider.HasAdequacy(region) with no isRecipientCertified argument
    // (it has no per-request certification context — unlike TransferBlockingPipelineBehavior, it
    // is never given an IRecipientCertificationResolver), so the call always defaults to
    // isRecipientCertified: false. For the United States (EU-US Data Privacy Framework) and
    // Canada (PIPEDA), whose adequacy decisions are partial, this means the assessor treats them
    // as NOT adequate — the same conservative, fail-closed answer whether a hypothetical
    // recipient would be certified, uncertified, or no resolver is registered at all, because the
    // assessor never has the means to confirm certification.

    private static DefaultTIARiskAssessor CreateSutWithRealAdequacyProvider()
    {
        var adequacyProvider = new DefaultAdequacyDecisionProvider(
            Microsoft.Extensions.Options.Options.Create(new DataResidencyOptions()),
            NullLogger<DefaultAdequacyDecisionProvider>.Instance);

        return new DefaultTIARiskAssessor(adequacyProvider, NullLogger<DefaultTIARiskAssessor>.Instance);
    }

    [Fact]
    public async Task AssessRiskAsync_UsWithRealAdequacyProvider_TreatsPartialAdequacyAsNotAdequate()
    {
        // Arrange
        var sut = CreateSutWithRealAdequacyProvider();

        // Act
        var result = await sut.AssessRiskAsync("US", "personal-data");

        // Assert — the real provider knows the US has a partial (DPF) adequacy decision, but the
        // assessor cannot confirm recipient certification, so it falls to the Five Eyes branch
        // (base 0.4 + Five Eyes 0.2 = 0.6) instead of the 0.1 "has adequacy" branch.
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(Right: a => a, Left: _ => throw new InvalidOperationException("Expected Right"));
        assessment.Score.ShouldBeInRange(0.6 - 0.01, 0.6 + 0.01);
        assessment.Factors.ShouldContain(f => f.Contains("does not have an EU adequacy decision"));
        assessment.Factors.ShouldContain(f => f.Contains("Five Eyes"));
    }

    [Fact]
    public async Task AssessRiskAsync_CaWithRealAdequacyProvider_TreatsPartialAdequacyAsNotAdequate()
    {
        // Arrange
        var sut = CreateSutWithRealAdequacyProvider();

        // Act
        var result = await sut.AssessRiskAsync("CA", "personal-data");

        // Assert — Canada's PIPEDA adequacy decision is likewise partial; the assessor treats it
        // as not adequate for the same reason (base 0.4 + Five Eyes 0.2 = 0.6, CA is also a
        // Five Eyes member).
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(Right: a => a, Left: _ => throw new InvalidOperationException("Expected Right"));
        assessment.Score.ShouldBeInRange(0.6 - 0.01, 0.6 + 0.01);
        assessment.Factors.ShouldContain(f => f.Contains("does not have an EU adequacy decision"));
        assessment.Factors.ShouldContain(f => f.Contains("Five Eyes"));
    }

    #endregion
}
