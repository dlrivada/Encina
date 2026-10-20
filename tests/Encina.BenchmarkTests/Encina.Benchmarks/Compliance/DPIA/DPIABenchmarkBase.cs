using BenchmarkDotNet.Attributes;
using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;
using LanguageExt;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.Benchmarks.Compliance.DPIA;

/// <summary>
/// Shared setup and mocked services for the DPIA service benchmarks.
/// </summary>
/// <remarks>
/// The DPIA service benchmarks are split into several classes so that each one stays well under the
/// 60-minute job limit of the weekly Benchmarks matrix (#2510); each concrete class becomes its
/// own matrix job. This base carries no benchmark attributes of its own, so the class
/// discovery and BenchmarkDotNet do not treat it as a benchmark class.
/// </remarks>
public abstract class DPIABenchmarkBase
{
    protected IDPIAService Service { get; private set; } = null!;
    protected IDPIAAssessmentEngine Engine { get; private set; } = null!;
    protected Guid ExistingAssessmentId { get; private set; }
    protected DPIAContext EvaluationContext { get; private set; } = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        ExistingAssessmentId = Guid.NewGuid();
        Service = BuildMockedService();
        Engine = BuildMockedEngine();
        EvaluationContext = new DPIAContext
        {
            RequestType = typeof(string),
            ProcessingType = "AutomatedDecisionMaking",
            DataCategories = ["personal-data", "biometric"],
            HighRiskTriggers = ["special-category-data", "systematic-monitoring"],
        };
    }

    private static IDPIAService BuildMockedService()
    {
        var service = Substitute.For<IDPIAService>();

        var readModel = new DPIAReadModel
        {
            Id = Guid.NewGuid(),
            RequestTypeName = "BenchmarkApp.Commands.ProcessData",
            ProcessingType = "AutomatedDecisionMaking",
            Reason = "Benchmark assessment",
            Status = DPIAAssessmentStatus.Approved,
            OverallRisk = RiskLevel.Medium,
            IdentifiedRisks = [new RiskItem("Test", RiskLevel.Medium, "Benchmark", null)],
            ProposedMitigations = [new Mitigation("Applied", "Technical", true, DateTimeOffset.UtcNow)],
            ApprovedAtUtc = DateTimeOffset.UtcNow.AddDays(-30),
            NextReviewAtUtc = DateTimeOffset.UtcNow.AddDays(335),
            LastModifiedAtUtc = DateTimeOffset.UtcNow,
            Version = 5
        };

        var dpiaResult = new DPIAResult
        {
            OverallRisk = RiskLevel.Medium,
            IdentifiedRisks = [new RiskItem("Test", RiskLevel.Medium, "Benchmark", null)],
            ProposedMitigations = [new Mitigation("Applied", "Technical", true, DateTimeOffset.UtcNow)],
            RequiresPriorConsultation = false,
            AssessedAtUtc = DateTimeOffset.UtcNow,
        };

#pragma warning disable CA2012
        service.CreateAssessmentAsync(
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Guid>(Guid.NewGuid())));

        service.EvaluateAssessmentAsync(
            Arg.Any<Guid>(), Arg.Any<DPIAContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, DPIAResult>(dpiaResult)));

        service.RequestDPOConsultationAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Guid>(Guid.NewGuid())));

        service.RecordDPOResponseAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DPOConsultationDecision>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default)));

        service.ApproveAssessmentAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default)));

        service.RejectAssessmentAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default)));

        service.RequestRevisionAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default)));

        service.ExpireAssessmentAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default)));

        service.GetAssessmentAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, DPIAReadModel>(readModel)));

        service.GetAssessmentByRequestTypeAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, DPIAReadModel>(readModel)));

        service.GetExpiredAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, IReadOnlyList<DPIAReadModel>>(
                new List<DPIAReadModel> { readModel }.AsReadOnly())));

        service.GetAllAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, IReadOnlyList<DPIAReadModel>>(
                new List<DPIAReadModel> { readModel }.AsReadOnly())));
#pragma warning restore CA2012

        return service;
    }

    private static IDPIAAssessmentEngine BuildMockedEngine()
    {
        var engine = Substitute.For<IDPIAAssessmentEngine>();

#pragma warning disable CA2012
        engine.RequiresDPIAAsync(Arg.Any<Type>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
#pragma warning restore CA2012

        return engine;
    }
}
