#pragma warning disable CA1859 // Contract tests intentionally use interface types

using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Model;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Compliance.PrivacyByDesign;

/// <summary>
/// Contract tests for <see cref="IDataMinimizationAnalyzer"/> verifying the
/// <see cref="DefaultDataMinimizationAnalyzer"/> implementation behaves correctly
/// through the interface contract.
/// </summary>
[Trait("Category", "Contract")]
public class IDataMinimizationAnalyzerContractTests
{
    private readonly IDataMinimizationAnalyzer _analyzer;

    public IDataMinimizationAnalyzerContractTests()
    {
        _analyzer = new DefaultDataMinimizationAnalyzer(
            TimeProvider.System,
            NullLogger<DefaultDataMinimizationAnalyzer>.Instance);
    }

    #region Test Types

    [EnforceDataMinimization]
    public class AnalyzerTestRequest
    {
        public string RequiredField { get; set; } = "";

        [NotStrictlyNecessary(Reason = "Marketing analytics")]
        public string? OptionalField { get; set; }

        [NotStrictlyNecessary(Reason = "UX improvement")]
        public string? AnotherOptional { get; set; }
    }

    [EnforceDataMinimization]
    public class AnalyzerDefaultsTestRequest
    {
        public string Name { get; set; } = "";

        [PrivacyDefault(false)]
        public bool EnableTracking { get; set; }

        [PrivacyDefault(30)]
        public int RetentionDays { get; set; } = 30;
    }

    #endregion

    #region AnalyzeAsync Contract

    [Fact]
    public async Task Contract_AnalyzeAsync_ReturnsCorrectFieldClassification()
    {
        IDataMinimizationAnalyzer analyzer = _analyzer;
        var request = new AnalyzerTestRequest
        {
            RequiredField = "value",
            OptionalField = "should-be-flagged"
        };

        var result = await analyzer.AnalyzeAsync(request);
        result.IsRight.ShouldBeTrue("AnalyzeAsync should return Right");

        var report = result.Match(r => r, _ => null!);
        report.ShouldNotBeNull();

        // RequiredField should be classified as necessary.
        report.NecessaryFields.ShouldContain(f => f.FieldName == "RequiredField");

        // OptionalField and AnotherOptional should be classified as unnecessary.
        report.UnnecessaryFields.ShouldContain(f => f.FieldName == "OptionalField");
        report.UnnecessaryFields.ShouldContain(f => f.FieldName == "AnotherOptional");

        // OptionalField has a value, AnotherOptional does not.
        report.UnnecessaryFields.First(f => f.FieldName == "OptionalField").HasValue.ShouldBeTrue();
        report.UnnecessaryFields.First(f => f.FieldName == "AnotherOptional").HasValue.ShouldBeFalse();
    }

    [Fact]
    public async Task Contract_AnalyzeAsync_ScoreReflectsFieldRatio()
    {
        IDataMinimizationAnalyzer analyzer = _analyzer;
        var request = new AnalyzerTestRequest { RequiredField = "value" };

        var result = await analyzer.AnalyzeAsync(request);
        result.IsRight.ShouldBeTrue("AnalyzeAsync should return Right");

        var report = result.Match(r => r, _ => null!);
        report.ShouldNotBeNull();

        // 1 necessary field out of 3 total → score = 1/3 ≈ 0.333
        var expectedScore = (double)report.NecessaryFields.Count /
            (report.NecessaryFields.Count + report.UnnecessaryFields.Count);
        report.MinimizationScore.ShouldBe(expectedScore, 0.001);
    }

    #endregion

    #region InspectDefaultsAsync Contract

    [Fact]
    public async Task Contract_InspectDefaultsAsync_DetectsOverrides()
    {
        IDataMinimizationAnalyzer analyzer = _analyzer;
        var request = new AnalyzerDefaultsTestRequest
        {
            Name = "Test",
            EnableTracking = true, // Overrides default of false
            RetentionDays = 30     // Matches default
        };

        var result = await analyzer.InspectDefaultsAsync(request);
        result.IsRight.ShouldBeTrue("InspectDefaultsAsync should return Right");

        var defaults = result.Match(d => d, _ => []);
        defaults.Count.ShouldBe(2, "Should inspect both fields with PrivacyDefault attributes");

        var trackingField = defaults.First(d => d.FieldName == "EnableTracking");
        trackingField.MatchesDefault.ShouldBeFalse("EnableTracking=true deviates from default=false");
        trackingField.DeclaredDefault.ShouldBe(false);
        trackingField.ActualValue.ShouldBe(true);
    }

    [Fact]
    public async Task Contract_InspectDefaultsAsync_ReportsMatching()
    {
        IDataMinimizationAnalyzer analyzer = _analyzer;
        var request = new AnalyzerDefaultsTestRequest
        {
            Name = "Test",
            EnableTracking = false, // Matches default
            RetentionDays = 30      // Matches default
        };

        var result = await analyzer.InspectDefaultsAsync(request);
        result.IsRight.ShouldBeTrue("InspectDefaultsAsync should return Right");

        var defaults = result.Match(d => d, _ => []);
        defaults.Count.ShouldBe(2);

        defaults.ShouldAllBe(d => d.MatchesDefault, "All fields match their declared defaults");
    }

    #endregion
}
