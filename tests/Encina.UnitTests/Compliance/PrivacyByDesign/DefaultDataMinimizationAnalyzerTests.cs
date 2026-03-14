#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.Compliance.PrivacyByDesign;

#region Test Request Types

public class AllNecessaryRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

[EnforceDataMinimization]
public class MixedRequest
{
    public string ProductId { get; set; } = "";

    [NotStrictlyNecessary(Reason = "Analytics only")]
    public string? ReferralSource { get; set; }

    [NotStrictlyNecessary(Reason = "Marketing", Severity = MinimizationSeverity.Violation)]
    public string? CampaignCode { get; set; }
}

public class WithDefaultsRequest
{
    [PrivacyDefault(false)]
    public bool ShareData { get; set; }

    [PrivacyDefault(null)]
    public string? MarketingConsent { get; set; }
}

#endregion

/// <summary>
/// Unit tests for <see cref="DefaultDataMinimizationAnalyzer"/>.
/// </summary>
public class DefaultDataMinimizationAnalyzerTests
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly DefaultDataMinimizationAnalyzer _sut;

    public DefaultDataMinimizationAnalyzerTests()
    {
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 14, 12, 0, 0, TimeSpan.Zero));
        _sut = new DefaultDataMinimizationAnalyzer(
            _timeProvider,
            NullLogger<DefaultDataMinimizationAnalyzer>.Instance);

        // Clear the static metadata cache between tests to avoid cross-test contamination.
        DefaultDataMinimizationAnalyzer.MetadataCache.Clear();
    }

    #region AnalyzeAsync

    [Fact]
    public async Task AnalyzeAsync_AllNecessaryRequest_ShouldReturnScoreOneAndEmptyUnnecessaryFields()
    {
        // Arrange
        var request = new AllNecessaryRequest { Name = "Alice", Email = "alice@example.com" };

        // Act
        var result = await _sut.AnalyzeAsync(request);

        // Assert
        result.IsRight.Should().BeTrue();
        var report = (MinimizationReport)result;
        report.MinimizationScore.Should().Be(1.0);
        report.UnnecessaryFields.Should().BeEmpty();
        report.NecessaryFields.Should().HaveCount(2);
        report.Recommendations.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_MixedRequestWithNullUnnecessaryFields_ShouldReturnCorrectScoreAndHasValueFalse()
    {
        // Arrange
        var request = new MixedRequest { ProductId = "P-001", ReferralSource = null, CampaignCode = null };

        // Act
        var result = await _sut.AnalyzeAsync(request);

        // Assert
        result.IsRight.Should().BeTrue();
        var report = (MinimizationReport)result;

        // Score = necessary(1) / total(3) = 0.333...
        report.MinimizationScore.Should().BeApproximately(1.0 / 3.0, 0.001);
        report.NecessaryFields.Should().HaveCount(1);
        report.UnnecessaryFields.Should().HaveCount(2);

        // All unnecessary fields should have HasValue = false (null values)
        report.UnnecessaryFields.Should().AllSatisfy(f => f.HasValue.Should().BeFalse());
    }

    [Fact]
    public async Task AnalyzeAsync_MixedRequestWithValuesSet_ShouldReturnCorrectScoreAndHasValueTrue()
    {
        // Arrange
        var request = new MixedRequest
        {
            ProductId = "P-001",
            ReferralSource = "google",
            CampaignCode = "SUMMER2026"
        };

        // Act
        var result = await _sut.AnalyzeAsync(request);

        // Assert
        result.IsRight.Should().BeTrue();
        var report = (MinimizationReport)result;

        // Score = necessary(1) / total(3) = 0.333...
        report.MinimizationScore.Should().BeApproximately(1.0 / 3.0, 0.001);
        report.UnnecessaryFields.Should().HaveCount(2);
        report.UnnecessaryFields.Should().AllSatisfy(f => f.HasValue.Should().BeTrue());

        // Should generate recommendations for fields with values
        report.Recommendations.Should().NotBeEmpty();
        report.Recommendations.Should().HaveCount(2);
    }

    [Fact]
    public async Task AnalyzeAsync_MixedRequest_ShouldPreserveSeverityFromAttribute()
    {
        // Arrange
        var request = new MixedRequest { ProductId = "P-001", ReferralSource = "google", CampaignCode = "CAMP" };

        // Act
        var result = await _sut.AnalyzeAsync(request);

        // Assert
        result.IsRight.Should().BeTrue();
        var report = (MinimizationReport)result;

        var referral = report.UnnecessaryFields.First(f => f.FieldName == "ReferralSource");
        referral.Severity.Should().Be(MinimizationSeverity.Warning);
        referral.Reason.Should().Be("Analytics only");

        var campaign = report.UnnecessaryFields.First(f => f.FieldName == "CampaignCode");
        campaign.Severity.Should().Be(MinimizationSeverity.Violation);
        campaign.Reason.Should().Be("Marketing");
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldReturnProperRequestTypeName()
    {
        // Arrange
        var request = new AllNecessaryRequest { Name = "Test", Email = "test@test.com" };

        // Act
        var result = await _sut.AnalyzeAsync(request);

        // Assert
        result.IsRight.Should().BeTrue();
        var report = (MinimizationReport)result;
        report.RequestTypeName.Should().Contain(nameof(AllNecessaryRequest));
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldIncludeAnalyzedAtUtcTimestamp()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 3, 14, 15, 30, 0, TimeSpan.Zero);
        _timeProvider.SetUtcNow(now);
        var request = new AllNecessaryRequest { Name = "A", Email = "B" };

        // Act
        var result = await _sut.AnalyzeAsync(request);

        // Assert
        result.IsRight.Should().BeTrue();
        var report = (MinimizationReport)result;
        report.AnalyzedAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task AnalyzeAsync_NullRequest_ShouldThrowArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _sut.AnalyzeAsync<AllNecessaryRequest>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region InspectDefaultsAsync

    [Fact]
    public async Task InspectDefaultsAsync_WithDefaultsMatchingDeclared_ShouldReturnAllMatchesDefaultTrue()
    {
        // Arrange
        var request = new WithDefaultsRequest { ShareData = false, MarketingConsent = null };

        // Act
        var result = await _sut.InspectDefaultsAsync(request);

        // Assert
        result.IsRight.Should().BeTrue();
        var defaults = result.Match(r => r, _ => (IReadOnlyList<DefaultPrivacyFieldInfo>)[]);
        defaults.Should().HaveCount(2);

        var shareData = defaults.First(d => d.FieldName == "ShareData");
        shareData.MatchesDefault.Should().BeTrue();
        shareData.DeclaredDefault.Should().Be(false);
        shareData.ActualValue.Should().Be(false);

        var marketingConsent = defaults.First(d => d.FieldName == "MarketingConsent");
        marketingConsent.MatchesDefault.Should().BeTrue();
        marketingConsent.DeclaredDefault.Should().BeNull();
        marketingConsent.ActualValue.Should().BeNull();
    }

    [Fact]
    public async Task InspectDefaultsAsync_WithOverriddenDefaults_ShouldReturnMatchesDefaultFalse()
    {
        // Arrange
        var request = new WithDefaultsRequest { ShareData = true, MarketingConsent = "yes" };

        // Act
        var result = await _sut.InspectDefaultsAsync(request);

        // Assert
        result.IsRight.Should().BeTrue();
        var defaults = result.Match(r => r, _ => (IReadOnlyList<DefaultPrivacyFieldInfo>)[]);
        defaults.Should().HaveCount(2);

        var shareData = defaults.First(d => d.FieldName == "ShareData");
        shareData.MatchesDefault.Should().BeFalse();
        shareData.ActualValue.Should().Be(true);

        var marketingConsent = defaults.First(d => d.FieldName == "MarketingConsent");
        marketingConsent.MatchesDefault.Should().BeFalse();
        marketingConsent.ActualValue.Should().Be("yes");
    }

    [Fact]
    public async Task InspectDefaultsAsync_RequestWithoutPrivacyDefaultAttributes_ShouldReturnEmptyList()
    {
        // Arrange
        var request = new AllNecessaryRequest { Name = "Alice", Email = "alice@example.com" };

        // Act
        var result = await _sut.InspectDefaultsAsync(request);

        // Assert
        result.IsRight.Should().BeTrue();
        var defaults = result.Match(r => r, _ => (IReadOnlyList<DefaultPrivacyFieldInfo>)[]);
        defaults.Should().BeEmpty();
    }

    [Fact]
    public async Task InspectDefaultsAsync_NullRequest_ShouldThrowArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _sut.InspectDefaultsAsync<AllNecessaryRequest>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion
}
