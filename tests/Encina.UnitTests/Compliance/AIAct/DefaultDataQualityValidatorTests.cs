using Encina.Compliance.AIAct;
using Encina.Compliance.AIAct.Model;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.Compliance.AIAct;

/// <summary>
/// Unit tests for <see cref="DefaultDataQualityValidator"/>.
/// </summary>
public sealed class DefaultDataQualityValidatorTests
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly DefaultDataQualityValidator _sut;

    public DefaultDataQualityValidatorTests()
    {
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));
        _sut = new DefaultDataQualityValidator(_timeProvider);
    }

    [Fact]
    public async Task ValidateTrainingDataAsync_ReturnsDefaultPerfectScores()
    {
        // Act
        var result = await _sut.ValidateTrainingDataAsync("dataset-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        var report = (DataQualityReport)result;
        report.DatasetId.ShouldBe("dataset-1");
        report.CompletenessScore.ShouldBe(1.0);
        report.AccuracyScore.ShouldBe(1.0);
        report.ConsistencyScore.ShouldBe(1.0);
        report.MeetsAIActRequirements.ShouldBeTrue();
        report.EvaluatedAtUtc.ShouldBe(_timeProvider.GetUtcNow());
    }

    [Fact]
    public void ValidateTrainingDataAsync_WithNullDatasetId_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.ValidateTrainingDataAsync(null!).AsTask());
    }

    [Fact]
    public async Task DetectBiasAsync_ReturnsNoBiasDetected()
    {
        // Arrange
        var protectedAttributes = new List<string> { "gender", "race", "age" }.AsReadOnly();

        // Act
        var result = await _sut.DetectBiasAsync("dataset-2", protectedAttributes);

        // Assert
        result.IsRight.ShouldBeTrue();
        var report = (BiasReport)result;
        report.DatasetId.ShouldBe("dataset-2");
        report.ProtectedAttributes.ShouldBe(protectedAttributes);
        report.OverallFairness.ShouldBeTrue();
        report.EvaluatedAtUtc.ShouldBe(_timeProvider.GetUtcNow());
    }

    [Fact]
    public void DetectBiasAsync_WithNullDatasetId_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.DetectBiasAsync(null!, new List<string>()).AsTask());
    }

    [Fact]
    public void DetectBiasAsync_WithNullProtectedAttributes_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.DetectBiasAsync("dataset", null!).AsTask());
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DefaultDataQualityValidator(null!));
    }
}
