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
/// Benchmarks for the DPIA compliance service operations.
/// Measures throughput and allocations for each service method across the assessment lifecycle:
/// - Assessment creation (fast path — single aggregate write)
/// - Risk evaluation (assessment engine + risk criteria evaluation)
/// - DPO consultation request/response (state transitions)
/// - Assessment approval (terminal state transition)
/// - Assessment query by ID (cached read)
/// - Assessment query by request type (filtered lookup)
/// - Expired assessments query (filtered collection scan)
/// </summary>
/// <remarks>
/// <para>
/// DPIA assessment checks execute on every request when <c>DPIARequiredPipelineBehavior</c>
/// is active. The pipeline behavior calls <c>GetAssessmentByRequestTypeAsync</c> on every
/// decorated request, making it a hot-path operation requiring performance characterization.
/// </para>
/// <para>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*DPIAServiceBenchmarks*"
///
/// # Quick validation:
/// dotnet run -c Release -- --filter "*DPIAServiceBenchmarks*" --job short
///
/// # List available benchmarks:
/// dotnet run -c Release -- --list flat --filter "*DPIAService*"
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class DPIAServiceBenchmarks
{
    private IDPIAService _service = null!;
    private IDPIAAssessmentEngine _engine = null!;
    private Guid _existingAssessmentId;
    private DPIAContext _evaluationContext = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _existingAssessmentId = Guid.NewGuid();
        _service = BuildMockedService();
        _engine = BuildMockedEngine();
        _evaluationContext = new DPIAContext
        {
            RequestType = typeof(string),
            ProcessingType = "AutomatedDecisionMaking",
            DataCategories = ["personal-data", "biometric"],
            HighRiskTriggers = ["special-category-data", "systematic-monitoring"],
        };
    }

    // ────────────────────────────────────────────────────────────
    //  Assessment Creation
    // ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Create assessment (fast path)")]
    public async Task<Guid> CreateAssessment()
    {
        var result = await _service.CreateAssessmentAsync(
            "BenchmarkApp.Commands.ProcessData",
            "AutomatedDecisionMaking",
            "Benchmark test assessment");
        return result.Match(Right: id => id, Left: _ => Guid.Empty);
    }

    // ────────────────────────────────────────────────────────────
    //  Risk Evaluation
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Evaluate assessment (risk engine)")]
    public async Task<DPIAResult?> EvaluateAssessment()
    {
        var result = await _service.EvaluateAssessmentAsync(_existingAssessmentId, _evaluationContext);
        return result.Match(Right: r => r, Left: _ => null);
    }

    [Benchmark(Description = "RequiresDPIA check (pipeline hot-path)")]
    public async Task<bool> RequiresDPIA()
    {
        var result = await _engine.RequiresDPIAAsync(typeof(string));
        return result.Match(Right: r => r, Left: _ => false);
    }

    // ────────────────────────────────────────────────────────────
    //  DPO Consultation
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Request DPO consultation")]
    public async Task<Guid> RequestDPOConsultation()
    {
        var result = await _service.RequestDPOConsultationAsync(_existingAssessmentId);
        return result.Match(Right: id => id, Left: _ => Guid.Empty);
    }

    // ────────────────────────────────────────────────────────────
    //  State Transitions
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Approve assessment")]
    public async Task<Unit> ApproveAssessment()
    {
        var result = await _service.ApproveAssessmentAsync(
            _existingAssessmentId, "benchmark-approver", DateTimeOffset.UtcNow.AddDays(365));
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    [Benchmark(Description = "Reject assessment")]
    public async Task<Unit> RejectAssessment()
    {
        var result = await _service.RejectAssessmentAsync(
            _existingAssessmentId, "benchmark-rejector", "Benchmark rejection");
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    [Benchmark(Description = "Request revision")]
    public async Task<Unit> RequestRevision()
    {
        var result = await _service.RequestRevisionAsync(
            _existingAssessmentId, "benchmark-reviewer", "Needs more mitigations");
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    [Benchmark(Description = "Expire assessment")]
    public async Task<Unit> ExpireAssessment()
    {
        var result = await _service.ExpireAssessmentAsync(_existingAssessmentId);
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    // ────────────────────────────────────────────────────────────
    //  Query Operations
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Get assessment by ID (cached)")]
    public async Task<DPIAReadModel?> GetAssessmentById()
    {
        var result = await _service.GetAssessmentAsync(_existingAssessmentId);
        return result.Match(Right: rm => rm, Left: _ => null);
    }

    [Benchmark(Description = "Get assessment by request type (pipeline hot-path)")]
    public async Task<DPIAReadModel?> GetAssessmentByRequestType()
    {
        var result = await _service.GetAssessmentByRequestTypeAsync("BenchmarkApp.Commands.ProcessData");
        return result.Match(Right: rm => rm, Left: _ => null);
    }

    [Benchmark(Description = "Get expired assessments")]
    public async Task<IReadOnlyList<DPIAReadModel>?> GetExpiredAssessments()
    {
        var result = await _service.GetExpiredAssessmentsAsync();
        return result.Match(Right: list => list, Left: _ => null);
    }

    [Benchmark(Description = "Get all assessments")]
    public async Task<IReadOnlyList<DPIAReadModel>?> GetAllAssessments()
    {
        var result = await _service.GetAllAssessmentsAsync();
        return result.Match(Right: list => list, Left: _ => null);
    }

    // ────────────────────────────────────────────────────────────
    //  Test Infrastructure — Mocked Services
    // ────────────────────────────────────────────────────────────

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
