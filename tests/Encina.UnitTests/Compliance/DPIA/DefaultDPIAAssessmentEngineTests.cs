#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.GDPR;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="DefaultDPIAAssessmentEngine"/>.
/// </summary>
public class DefaultDPIAAssessmentEngineTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IDPIAStore _store = Substitute.For<IDPIAStore>();
    private readonly IDPIAAuditStore _auditStore = Substitute.For<IDPIAAuditStore>();
    private readonly IDPIATemplateProvider _templateProvider = Substitute.For<IDPIATemplateProvider>();
    private readonly FakeTimeProvider _timeProvider = new(FixedNow);
    private readonly DPIAOptions _options = new() { TrackAuditTrail = false };

    private DefaultDPIAAssessmentEngine CreateSut(
        IEnumerable<IRiskCriterion>? criteria = null,
        IDataProtectionOfficer? dpo = null)
    {
        return new DefaultDPIAAssessmentEngine(
            criteria ?? [],
            _store,
            _auditStore,
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

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AssessAsync_NoCriteria_ReturnsLowRisk()
    {
        var sut = CreateSut();
        var context = CreateContext();

        var result = await sut.AssessAsync(context);

        result.IsRight.Should().BeTrue();
        var dpiaResult = (DPIAResult)result;
        dpiaResult.OverallRisk.Should().Be(RiskLevel.Low);
        dpiaResult.IdentifiedRisks.Should().BeEmpty();
        dpiaResult.RequiresPriorConsultation.Should().BeFalse();
    }

    [Fact]
    public async Task AssessAsync_SingleHighRiskCriterion_ReturnsHighRisk()
    {
        var riskItem = new RiskItem("Test", RiskLevel.High, "Test risk", "Mitigate");
        var criterion = CreateCriterion(riskItem);
        var sut = CreateSut([criterion]);
        var context = CreateContext();

        var result = await sut.AssessAsync(context);

        result.IsRight.Should().BeTrue();
        var dpiaResult = (DPIAResult)result;
        dpiaResult.OverallRisk.Should().Be(RiskLevel.High);
        dpiaResult.IdentifiedRisks.Should().HaveCount(1);
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

        result.IsRight.Should().BeTrue();
        var dpiaResult = (DPIAResult)result;
        dpiaResult.OverallRisk.Should().Be(RiskLevel.High);
        dpiaResult.IdentifiedRisks.Should().HaveCount(2);
    }

    [Fact]
    public async Task AssessAsync_CriterionReturnsNull_IsExcluded()
    {
        var sut = CreateSut([CreateCriterion(null)]);
        var context = CreateContext();

        var result = await sut.AssessAsync(context);

        result.IsRight.Should().BeTrue();
        var dpiaResult = (DPIAResult)result;
        dpiaResult.IdentifiedRisks.Should().BeEmpty();
        dpiaResult.OverallRisk.Should().Be(RiskLevel.Low);
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

        result.IsRight.Should().BeTrue();
        var dpiaResult = (DPIAResult)result;
        dpiaResult.IdentifiedRisks.Should().HaveCount(1);
        dpiaResult.OverallRisk.Should().Be(RiskLevel.Medium);
    }

    [Fact]
    public async Task AssessAsync_VeryHighRiskWithUnimplementedMitigations_RequiresPriorConsultation()
    {
        var riskItem = new RiskItem("VH", RiskLevel.VeryHigh, "Very high", "Must mitigate");
        var sut = CreateSut([CreateCriterion(riskItem)]);
        var context = CreateContext();

        var result = await sut.AssessAsync(context);

        result.IsRight.Should().BeTrue();
        var dpiaResult = (DPIAResult)result;
        dpiaResult.RequiresPriorConsultation.Should().BeTrue();
    }

    [Fact]
    public async Task AssessAsync_HighRisk_DoesNotRequirePriorConsultation()
    {
        var riskItem = new RiskItem("H", RiskLevel.High, "High risk", "Mitigate");
        var sut = CreateSut([CreateCriterion(riskItem)]);
        var context = CreateContext();

        var result = await sut.AssessAsync(context);

        result.IsRight.Should().BeTrue();
        var dpiaResult = (DPIAResult)result;
        dpiaResult.RequiresPriorConsultation.Should().BeFalse();
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

        result.IsRight.Should().BeTrue();
        var dpiaResult = (DPIAResult)result;
        dpiaResult.ProposedMitigations.Should().ContainSingle(m => m.Description == "Implement encryption");
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

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task AssessAsync_WithRiskMitigationSuggestion_IncludesInProposedMitigations()
    {
        var riskItem = new RiskItem("Cat", RiskLevel.Medium, "Desc", "Apply encryption");
        var sut = CreateSut([CreateCriterion(riskItem)]);
        var context = CreateContext();

        var result = await sut.AssessAsync(context);

        result.IsRight.Should().BeTrue();
        var dpiaResult = (DPIAResult)result;
        dpiaResult.ProposedMitigations.Should().Contain(m => m.Description == "Apply encryption");
    }

    [Fact]
    public async Task AssessAsync_SetsAssessedAtUtc()
    {
        var sut = CreateSut();
        var context = CreateContext();

        var result = await sut.AssessAsync(context);

        result.IsRight.Should().BeTrue();
        var dpiaResult = (DPIAResult)result;
        dpiaResult.AssessedAtUtc.Should().Be(FixedNow);
    }

    [Fact]
    public async Task AssessAsync_WithAuditTrailEnabled_RecordsAuditEntry()
    {
        _options.TrackAuditTrail = true;
        _auditStore.RecordAuditEntryAsync(Arg.Any<DPIAAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(unit));

        var sut = CreateSut();
        var context = CreateContext();

        await sut.AssessAsync(context);

        await _auditStore.Received(1).RecordAuditEntryAsync(
            Arg.Any<DPIAAuditEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssessAsync_WithAuditTrailDisabled_DoesNotRecordAuditEntry()
    {
        _options.TrackAuditTrail = false;
        var sut = CreateSut();
        var context = CreateContext();

        await sut.AssessAsync(context);

        await _auditStore.DidNotReceive().RecordAuditEntryAsync(
            Arg.Any<DPIAAuditEntry>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region RequiresDPIAAsync Tests

    [Fact]
    public async Task RequiresDPIAAsync_NullRequestType_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var act = async () => await sut.RequiresDPIAAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RequiresDPIAAsync_TypeWithAttribute_ReturnsTrue()
    {
        var sut = CreateSut();

        var result = await sut.RequiresDPIAAsync(typeof(TestCommandWithDPIA));

        result.IsRight.Should().BeTrue();
        ((bool)result).Should().BeTrue();
    }

    [Fact]
    public async Task RequiresDPIAAsync_TypeWithoutAttribute_ReturnsFalse()
    {
        var sut = CreateSut();

        var result = await sut.RequiresDPIAAsync(typeof(TestCommandWithoutDPIA));

        result.IsRight.Should().BeTrue();
        ((bool)result).Should().BeFalse();
    }

    #endregion

    #region RequestDPOConsultationAsync Tests

    [Fact]
    public async Task RequestDPOConsultationAsync_NoDPOConfigured_ReturnsError()
    {
        var sut = CreateSut();

        var result = await sut.RequestDPOConsultationAsync(Guid.NewGuid());

        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetEncinaCode().Should().Be(DPIAErrors.DPOConsultationRequiredCode);
    }

    [Fact]
    public async Task RequestDPOConsultationAsync_WithDPOEmail_LoadsAssessment()
    {
        _options.DPOEmail = "dpo@test.com";
        _options.DPOName = "Test DPO";

        var assessmentId = Guid.NewGuid();
        var assessment = new DPIAAssessment
        {
            Id = assessmentId,
            RequestTypeName = "Ns.TestCommand",
            Status = DPIAAssessmentStatus.InReview,
            CreatedAtUtc = FixedNow,
        };

        _store.GetAssessmentByIdAsync(assessmentId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<DPIAAssessment>>(Some(assessment))));
        _store.SaveAssessmentAsync(Arg.Any<DPIAAssessment>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(unit));
        _auditStore.RecordAuditEntryAsync(Arg.Any<DPIAAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(unit));

        var sut = CreateSut();

        var result = await sut.RequestDPOConsultationAsync(assessmentId);

        result.IsRight.Should().BeTrue();
        var consultation = (DPOConsultation)result;
        consultation.DPOEmail.Should().Be("dpo@test.com");
        consultation.DPOName.Should().Be("Test DPO");
        consultation.Decision.Should().Be(DPOConsultationDecision.Pending);
    }

    [Fact]
    public async Task RequestDPOConsultationAsync_WithGDPRDPOFallback_UsesDPOFromGDPRModule()
    {
        var dpo = Substitute.For<IDataProtectionOfficer>();
        dpo.Name.Returns("GDPR DPO");
        dpo.Email.Returns("gdpr-dpo@test.com");

        var assessmentId = Guid.NewGuid();
        var assessment = new DPIAAssessment
        {
            Id = assessmentId,
            RequestTypeName = "Ns.TestCommand",
            Status = DPIAAssessmentStatus.InReview,
            CreatedAtUtc = FixedNow,
        };

        _store.GetAssessmentByIdAsync(assessmentId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<DPIAAssessment>>(Some(assessment))));
        _store.SaveAssessmentAsync(Arg.Any<DPIAAssessment>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(unit));
        _auditStore.RecordAuditEntryAsync(Arg.Any<DPIAAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(unit));

        var sut = CreateSut(dpo: dpo);

        var result = await sut.RequestDPOConsultationAsync(assessmentId);

        result.IsRight.Should().BeTrue();
        var consultation = (DPOConsultation)result;
        consultation.DPOEmail.Should().Be("gdpr-dpo@test.com");
    }

    [Fact]
    public async Task RequestDPOConsultationAsync_AssessmentNotFound_ReturnsError()
    {
        _options.DPOEmail = "dpo@test.com";

        _store.GetAssessmentByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<DPIAAssessment>>(None)));

        var sut = CreateSut();

        var result = await sut.RequestDPOConsultationAsync(Guid.NewGuid());

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task RequestDPOConsultationAsync_StoreReturnsError_PropagatesError()
    {
        _options.DPOEmail = "dpo@test.com";

        var storeError = DPIAErrors.StoreError("GetAssessmentById", "Database error");
        _store.GetAssessmentByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, Option<DPIAAssessment>>(storeError)));

        var sut = CreateSut();

        var result = await sut.RequestDPOConsultationAsync(Guid.NewGuid());

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task RequestDPOConsultationAsync_DPOEmailInOptionsTakesPriority()
    {
        _options.DPOEmail = "options-dpo@test.com";
        _options.DPOName = "Options DPO";

        var dpo = Substitute.For<IDataProtectionOfficer>();
        dpo.Name.Returns("GDPR DPO");
        dpo.Email.Returns("gdpr-dpo@test.com");

        var assessmentId = Guid.NewGuid();
        var assessment = new DPIAAssessment
        {
            Id = assessmentId,
            RequestTypeName = "Ns.TestCommand",
            Status = DPIAAssessmentStatus.InReview,
            CreatedAtUtc = FixedNow,
        };

        _store.GetAssessmentByIdAsync(assessmentId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<DPIAAssessment>>(Some(assessment))));
        _store.SaveAssessmentAsync(Arg.Any<DPIAAssessment>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(unit));
        _auditStore.RecordAuditEntryAsync(Arg.Any<DPIAAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(unit));

        var sut = CreateSut(dpo: dpo);

        var result = await sut.RequestDPOConsultationAsync(assessmentId);

        result.IsRight.Should().BeTrue();
        var consultation = (DPOConsultation)result;
        consultation.DPOEmail.Should().Be("options-dpo@test.com");
    }

    #endregion

    #region Test Helpers

    [RequiresDPIA]
    private sealed class TestCommandWithDPIA;

    private sealed class TestCommandWithoutDPIA;

    #endregion
}
