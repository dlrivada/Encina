using BenchmarkDotNet.Attributes;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using LanguageExt;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.Benchmarks.Compliance.ProcessorAgreements;

/// <summary>
/// Benchmarks for the Processor Agreements compliance service operations.
/// Measures throughput and allocations for each service method across the DPA lifecycle:
/// - DPA execution (fast path — single aggregate write)
/// - Agreement validation (hot-path — HasValidDPA check)
/// - Detailed DPA validation (full compliance check)
/// - Processor registration and lookup
/// - Sub-processor chain traversal
/// - Agreement query by processor ID
/// - Expiring agreements query
/// </summary>
/// <remarks>
/// <para>
/// Processor agreement validation executes on every request when <c>ProcessorValidationPipelineBehavior</c>
/// is active. The pipeline calls <c>HasValidDPAAsync</c> on every decorated request, making it a
/// hot-path operation requiring performance characterization.
/// </para>
/// <para>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*ProcessorAgreementServiceBenchmarks*"
///
/// # Quick validation:
/// dotnet run -c Release -- --filter "*ProcessorAgreementServiceBenchmarks*" --job short
///
/// # List available benchmarks:
/// dotnet run -c Release -- --list flat --filter "*ProcessorAgreementService*"
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class ProcessorAgreementServiceBenchmarks
{
    private IDPAService _dpaService = null!;
    private IProcessorService _processorService = null!;
    private Guid _existingProcessorId;
    private Guid _existingDPAId;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _existingProcessorId = Guid.NewGuid();
        _existingDPAId = Guid.NewGuid();
        _dpaService = BuildMockedDPAService();
        _processorService = BuildMockedProcessorService();
    }

    // ────────────────────────────────────────────────────────────
    //  DPA Validation (Pipeline Hot-Path)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "HasValidDPA (pipeline hot-path)")]
    public async Task<bool> HasValidDPA()
    {
        var result = await _dpaService.HasValidDPAAsync(_existingProcessorId);
        return result.Match(Right: r => r, Left: _ => false);
    }

    [Benchmark(Description = "ValidateDPA (detailed compliance check)")]
    public async Task<DPAValidationResult?> ValidateDPA()
    {
        var result = await _dpaService.ValidateDPAAsync(_existingProcessorId);
        return result.Match(Right: r => r, Left: _ => null);
    }

    // ────────────────────────────────────────────────────────────
    //  DPA Lifecycle Operations
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "ExecuteDPA (new agreement)")]
    public async Task<Guid> ExecuteDPA()
    {
        var result = await _dpaService.ExecuteDPAAsync(
            Guid.NewGuid(), FullyCompliantTerms(), true,
            ["data-analytics"], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(2));
        return result.Match(Right: id => id, Left: _ => Guid.Empty);
    }

    [Benchmark(Description = "AmendDPA (update terms)")]
    public async Task<Unit> AmendDPA()
    {
        var result = await _dpaService.AmendDPAAsync(
            _existingDPAId, FullyCompliantTerms(), true,
            ["analytics", "reporting"], "Added reporting purpose");
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    [Benchmark(Description = "AuditDPA (record audit)")]
    public async Task<Unit> AuditDPA()
    {
        var result = await _dpaService.AuditDPAAsync(
            _existingDPAId, "auditor-bench", "Compliance verified");
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    [Benchmark(Description = "RenewDPA (extend expiration)")]
    public async Task<Unit> RenewDPA()
    {
        var result = await _dpaService.RenewDPAAsync(
            _existingDPAId, DateTimeOffset.UtcNow.AddYears(2));
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    [Benchmark(Description = "TerminateDPA (end agreement)")]
    public async Task<Unit> TerminateDPA()
    {
        var result = await _dpaService.TerminateDPAAsync(
            _existingDPAId, "Benchmark termination");
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    // ────────────────────────────────────────────────────────────
    //  Query Operations
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "GetDPA by ID (cached read)")]
    public async Task<DPAReadModel?> GetDPAById()
    {
        var result = await _dpaService.GetDPAAsync(_existingDPAId);
        return result.Match(Right: rm => rm, Left: _ => null);
    }

    [Benchmark(Description = "GetActiveDPA by processor ID")]
    public async Task<DPAReadModel?> GetActiveDPAByProcessorId()
    {
        var result = await _dpaService.GetActiveDPAByProcessorIdAsync(_existingProcessorId);
        return result.Match(Right: rm => rm, Left: _ => null);
    }

    [Benchmark(Description = "GetExpiringDPAs (filtered scan)")]
    public async Task<IReadOnlyList<DPAReadModel>?> GetExpiringDPAs()
    {
        var result = await _dpaService.GetExpiringDPAsAsync();
        return result.Match(Right: list => list, Left: _ => null);
    }

    // ────────────────────────────────────────────────────────────
    //  Processor Operations
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "RegisterProcessor (new processor)")]
    public async Task<Guid> RegisterProcessor()
    {
        var result = await _processorService.RegisterProcessorAsync(
            "BenchProc", "DE", null, null, 0, SubProcessorAuthorizationType.Specific);
        return result.Match(Right: id => id, Left: _ => Guid.Empty);
    }

    [Benchmark(Description = "GetProcessor by ID (cached read)")]
    public async Task<ProcessorReadModel?> GetProcessorById()
    {
        var result = await _processorService.GetProcessorAsync(_existingProcessorId);
        return result.Match(Right: rm => rm, Left: _ => null);
    }

    [Benchmark(Description = "GetFullSubProcessorChain (BFS traversal)")]
    public async Task<IReadOnlyList<ProcessorReadModel>?> GetFullSubProcessorChain()
    {
        var result = await _processorService.GetFullSubProcessorChainAsync(_existingProcessorId);
        return result.Match(Right: list => list, Left: _ => null);
    }

    // ────────────────────────────────────────────────────────────
    //  Test Infrastructure — Mocked Services
    // ────────────────────────────────────────────────────────────

    private static DPAMandatoryTerms FullyCompliantTerms() => new()
    {
        ProcessOnDocumentedInstructions = true,
        ConfidentialityObligations = true,
        SecurityMeasures = true,
        SubProcessorRequirements = true,
        DataSubjectRightsAssistance = true,
        ComplianceAssistance = true,
        DataDeletionOrReturn = true,
        AuditRights = true
    };

    private IDPAService BuildMockedDPAService()
    {
        var service = Substitute.For<IDPAService>();

        var readModel = new DPAReadModel
        {
            Id = _existingDPAId,
            ProcessorId = _existingProcessorId,
            Status = DPAStatus.Active,
            MandatoryTerms = FullyCompliantTerms(),
            HasSCCs = true,
            ProcessingPurposes = ["data-analytics", "payment-processing"],
            SignedAtUtc = DateTimeOffset.UtcNow.AddDays(-60),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(300),
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-60),
            LastModifiedAtUtc = DateTimeOffset.UtcNow,
            Version = 3
        };

        var validationResult = new DPAValidationResult
        {
            ProcessorId = _existingProcessorId.ToString(),
            DPAId = _existingDPAId.ToString(),
            IsValid = true,
            MissingTerms = [],
            Warnings = [],
            DaysUntilExpiration = 300,
            ValidatedAtUtc = DateTimeOffset.UtcNow
        };

#pragma warning disable CA2012
        service.HasValidDPAAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(true)));

        service.ValidateDPAAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, DPAValidationResult>(validationResult)));

        service.ExecuteDPAAsync(
            Arg.Any<Guid>(), Arg.Any<DPAMandatoryTerms>(), Arg.Any<bool>(),
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Guid>(Guid.NewGuid())));

        service.AmendDPAAsync(
            Arg.Any<Guid>(), Arg.Any<DPAMandatoryTerms>(), Arg.Any<bool>(),
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default)));

        service.AuditDPAAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default)));

        service.RenewDPAAsync(
            Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default)));

        service.TerminateDPAAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default)));

        service.GetDPAAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, DPAReadModel>(readModel)));

        service.GetActiveDPAByProcessorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, DPAReadModel>(readModel)));

        service.GetExpiringDPAsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, IReadOnlyList<DPAReadModel>>(
                new List<DPAReadModel> { readModel }.AsReadOnly())));
#pragma warning restore CA2012

        return service;
    }

    private IProcessorService BuildMockedProcessorService()
    {
        var service = Substitute.For<IProcessorService>();

        var processorReadModel = new ProcessorReadModel
        {
            Id = _existingProcessorId,
            Name = "Stripe",
            Country = "US",
            ContactEmail = "dpo@stripe.com",
            Depth = 0,
            AuthorizationType = SubProcessorAuthorizationType.Specific,
            SubProcessorCount = 2,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-90),
            LastModifiedAtUtc = DateTimeOffset.UtcNow,
            Version = 3
        };

        var subProcessors = new List<ProcessorReadModel>
        {
            new()
            {
                Id = Guid.NewGuid(), Name = "Stripe Treasury", Country = "US",
                ParentProcessorId = _existingProcessorId, Depth = 1,
                AuthorizationType = SubProcessorAuthorizationType.Specific,
                CreatedAtUtc = DateTimeOffset.UtcNow, LastModifiedAtUtc = DateTimeOffset.UtcNow, Version = 1
            },
            new()
            {
                Id = Guid.NewGuid(), Name = "Stripe Atlas", Country = "IE",
                ParentProcessorId = _existingProcessorId, Depth = 1,
                AuthorizationType = SubProcessorAuthorizationType.Specific,
                CreatedAtUtc = DateTimeOffset.UtcNow, LastModifiedAtUtc = DateTimeOffset.UtcNow, Version = 1
            }
        }.AsReadOnly();

#pragma warning disable CA2012
        service.RegisterProcessorAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<Guid?>(),
            Arg.Any<int>(), Arg.Any<SubProcessorAuthorizationType>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Guid>(Guid.NewGuid())));

        service.GetProcessorAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, ProcessorReadModel>(processorReadModel)));

        service.GetFullSubProcessorChainAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, IReadOnlyList<ProcessorReadModel>>(subProcessors)));
#pragma warning restore CA2012

        return service;
    }
}
