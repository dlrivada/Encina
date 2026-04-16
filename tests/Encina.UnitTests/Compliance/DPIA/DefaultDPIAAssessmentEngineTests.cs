#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.GDPR;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="DefaultDPIAAssessmentEngine"/>.
/// </summary>
public class DefaultDPIAAssessmentEngineTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IDPIATemplateProvider _templateProvider = Substitute.For<IDPIATemplateProvider>();
    private readonly FakeTimeProvider _timeProvider = new(FixedNow);
    private readonly DPIAOptions _options = new();

    private DefaultDPIAAssessmentEngine CreateSut(
        IEnumerable<IRiskCriterion>? criteria = null,
        IDataProtectionOfficer? dpo = null)
    {
        return new DefaultDPIAAssessmentEngine(
            criteria ?? [],
            _templateProvider,
            Options.Create(_options),
            _timeProvider,
            NullLogger<DefaultDPIAAssessmentEngine>.Instance,
            dpo);
    }

    private static DPIAContext CreateContext(
        Type? requestType = null,
        string? processingType = null,
        IReadOnlyList<string>? dataCategories = null,
        IReadOnlyList<string>? triggers = null) => new()
        {
            RequestType = requestType ?? typeof(object),
            ProcessingType = processingType,
            DataCategories = dataCategories ?? [],
            HighRiskTriggers = triggers ?? [],
        };

    private static IRiskCriterion CreateCriterion(RiskItem? result, string name = "TestCriterion")
    {
        var criterion = Substitute.For<IRiskCriterion>();
        criterion.Name.Returns(name);
        criterion.EvaluateAsync(Arg.Any<DPIAContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(result));
        return criterion;
    }

    private static IRiskCriterion CreateThrowingCriterion(string name = "FailingCriterion")
    {
        var criterion = Substitute.For<IRiskCriterion>();
        criterion.Name.Returns(name);
        criterion.EvaluateAsync(Arg.Any<DPIAContext>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Criterion failure"));
        return criterion;
    }

    #region AssessAsync Tests

    [Fact]
    public async Task AssessAsync_NullContext_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var act = async () => await sut.AssessAsync(null!);

        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task AssessAsync_NoCriteria_ReturnsLowRisk()
    {
        var sut = CreateSut();
        var context = CreateContext();

        var result = await sut.AssessAsync(context);

        result.IsRight.ShouldBeTrue();
        var dpiaResult = (DPIAResult)result;
        dpiaResult.OverallRisk.ShouldBe(RiskLevel.Low);
        dpiaResult.IdentifiedRisks.ShouldBeEmpty();
        dpiaResult.RequiresPriorConsultation.ShouldBeFalse();
    }

    [Fact]
    public async Task AssessAsync_SingleHighRiskCriterion_ReturnsHighRisk()
    {
        var riskItem = new RiskItem("Test", RiskLevel.High, "Test risk", "Mitigate");
        var criterion = CreateCriterion(riskItem);
        var sut = CreateSut([criterion]);
        var context = CreateContext();

        var result = await sut.AssessAsync(context);

        result.IsRight.ShouldBeTrue();
        var dpiaResult = (DPIAResult)result;
        dpiaResult.OverallRisk.ShouldBe(RiskLevel.High);
        dpiaResult.IdentifiedRisks.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AssessAsync_MultipleCriteria_ReturnsMaxRiskLevel()
    {
        var lowRisk = new RiskItem("Low", RiskLevel.Low, "Low risk", null);
        var highRisk = new RiskItem("High", RiskLevel.High, "High risk", "Mitigate");

        var sut = CreateSut([
            CreateCriterion(lowRisk, "LowCriterion"),
            CreateCriterion(highRisk, "HighCriterion"),
        ]);
        var context = CreateContext();

        var result = await sut.AssessAsync(context);

        result.IsRight.ShouldBeTrue();
        var dpiaResult = (DPIAResult)result;
        dpiaResult.OverallRisk.ShouldBe(RiskLevel.High);
        dpiaResult.IdentifiedRisks.Count.ShouldBe(2);
    }

    [Fact]
    public async Task AssessAsync_CriterionReturnsNull_IsExcluded()
    {
        var sut = CreateSut([CreateCriterion(null)]);
        var context = CreateContext();

        var result = await sut.AssessAsync(context);

        result.IsRight.ShouldBeTrue();
        var dpiaResult = (DPIAResult)result;
        dpiaResult.IdentifiedRisks.ShouldBeEmpty();
        dpiaResult.OverallRisk.ShouldBe(RiskLevel.Low);
    }

    [Fact]
    public async Task AssessAsync_CriterionThrows_IsFaultIsolated()
    {
        var goodRisk = new RiskItem("Good", RiskLevel.Medium, "OK", null);
        var sut = CreateSut([
            CreateThrowingCriterion(),
            CreateCriterion(goodRisk, "GoodCriterion"),
        ]);
        var context = CreateContext();

        var result = await sut.AssessAsync(context);

        result.IsRight.ShouldBeTrue();
        var dpiaResult = (DPIAResult)result;
        dpiaResult.IdentifiedRisks.Count.ShouldBe(1);
        dpiaResult.OverallRisk.ShouldBe(RiskLevel.Medium);
    }

    [Fact]
    public async Task AssessAsync_VeryHighRiskWithUnimplementedMitigations_RequiresPriorConsultation()
    {
        var riskItem = new RiskItem("VH", RiskLevel.VeryHigh, "Very high", "Must mitigate");
        var sut = CreateSut([CreateCriterion(riskItem)]);
        var context = CreateContext();

        var result = await sut.AssessAsync(context);

        result.IsRight.ShouldBeTrue();
        var dpiaResult = (DPIAResult)result;
        dpiaResult.RequiresPriorConsultation.ShouldBeTrue();
    }

    [Fact]
    public async Task AssessAsync_HighRisk_DoesNotRequirePriorConsultation()
    {
        var riskItem = new RiskItem("H", RiskLevel.High, "High risk", "Mitigate");
        var sut = CreateSut([CreateCriterion(riskItem)]);
        var context = CreateContext();

        var result = await sut.AssessAsync(context);

        result.IsRight.ShouldBeTrue();
        var dpiaResult = (DPIAResult)result;
        dpiaResult.RequiresPriorConsultation.ShouldBeFalse();
    }

    [Fact]
    public async Task AssessAsync_WithProcessingType_ResolvesTemplate()
    {
        var template = new DPIATemplate
        {
            Name = "Test Template",
            Description = "Test",
            ProcessingType = "AutomatedDecisionMaking",
            Sections = [],
            RiskCategories = [],
            SuggestedMitigations = ["Implement encryption"],
        };

        _templateProvider.GetTemplateAsync("AutomatedDecisionMaking", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, DPIATemplate>>(template));

        var sut = CreateSut();
        var context = CreateContext(processingType: "AutomatedDecisionMaking");

        var result = await sut.AssessAsync(context);

        result.IsRight.ShouldBeTrue();
        var dpiaResult = (DPIAResult)result;
        dpiaResult.ProposedMitigations.Where(m => m.Description == "Implement encryption").ShouldHaveSingleItem();
    }

    [Fact]
    public async Task AssessAsync_TemplateNotFound_ProceedsWithoutTemplate()
    {
        _templateProvider.GetTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, DPIATemplate>>(
                DPIAErrors.TemplateNotFound("Unknown")));

        var sut = CreateSut();
        var context = CreateContext(processingType: "Unknown");

        var result = await sut.AssessAsync(context);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task AssessAsync_WithRiskMitigationSuggestion_IncludesInProposedMitigations()
    {
        var riskItem = new RiskItem("Cat", RiskLevel.Medium, "Desc", "Apply encryption");
        var sut = CreateSut([CreateCriterion(riskItem)]);
        var context = CreateContext();

        var result = await sut.AssessAsync(context);

        result.IsRight.ShouldBeTrue();
        var dpiaResult = (DPIAResult)result;
        dpiaResult.ProposedMitigations.ShouldContain(m => m.Description == "Apply encryption");
    }

    [Fact]
    public async Task AssessAsync_SetsAssessedAtUtc()
    {
        var sut = CreateSut();
        var context = CreateContext();

        var result = await sut.AssessAsync(context);

        result.IsRight.ShouldBeTrue();
        var dpiaResult = (DPIAResult)result;
        dpiaResult.AssessedAtUtc.ShouldBe(FixedNow);
    }

    #endregion

    #region RequiresDPIAAsync Tests

    [Fact]
    public async Task RequiresDPIAAsync_NullRequestType_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var act = async () => await sut.RequiresDPIAAsync(null!);

        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task RequiresDPIAAsync_TypeWithAttribute_ReturnsTrue()
    {
        var sut = CreateSut();

        var result = await sut.RequiresDPIAAsync(typeof(TestCommandWithDPIA));

        result.IsRight.ShouldBeTrue();
        ((bool)result).ShouldBeTrue();
    }

    [Fact]
    public async Task RequiresDPIAAsync_TypeWithoutAttribute_ReturnsFalse()
    {
        var sut = CreateSut();

        var result = await sut.RequiresDPIAAsync(typeof(TestCommandWithoutDPIA));

        result.IsRight.ShouldBeTrue();
        ((bool)result).ShouldBeFalse();
    }

    #endregion

    #region Test Helpers

    [RequiresDPIA]
    private sealed class TestCommandWithDPIA;

    private sealed class TestCommandWithoutDPIA;

    #endregion
}
