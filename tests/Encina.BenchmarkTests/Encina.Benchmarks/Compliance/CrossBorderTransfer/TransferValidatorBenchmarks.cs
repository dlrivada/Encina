using BenchmarkDotNet.Attributes;
using Encina.Compliance.CrossBorderTransfer;
using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.ReadModels;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.Benchmarks.Compliance.CrossBorderTransfer;

/// <summary>
/// Benchmarks for the cross-border transfer validation chain.
/// Measures throughput and allocations for each step of the GDPR Chapter V cascading validation:
/// - Adequacy decision check (fastest path — synchronous, no I/O)
/// - Approved transfer check (async service call)
/// - SCC agreement validation (async service call + result processing)
/// - TIA check (async service call + supplementary measures extraction)
/// - Full cascade to block (worst case — all 5 steps)
/// </summary>
/// <remarks>
/// <para>
/// Cross-border transfer validation is legally mandatory and executes on every request
/// decorated with <c>[RequiresCrossBorderTransfer]</c>. Benchmarking the overhead is
/// essential for capacity planning.
/// </para>
/// <para>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*TransferValidatorBenchmarks*"
///
/// # Quick validation:
/// dotnet run -c Release -- --filter "*TransferValidatorBenchmarks*" --job short
///
/// # List available benchmarks:
/// dotnet run -c Release -- --list flat --filter "*TransferValidator*"
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class TransferValidatorBenchmarks
{
    private ITransferValidator _validatorAdequate = null!;
    private ITransferValidator _validatorApproved = null!;
    private ITransferValidator _validatorSCC = null!;
    private ITransferValidator _validatorTIA = null!;
    private ITransferValidator _validatorBlock = null!;

    private ServiceProvider _adequateProvider = null!;
    private ServiceProvider _approvedProvider = null!;
    private ServiceProvider _sccProvider = null!;
    private ServiceProvider _tiaProvider = null!;
    private ServiceProvider _blockProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Adequacy path — fastest (synchronous check, no service calls)
        _adequateProvider = BuildProvider(adequateCountries: ["JP", "GB", "CH", "NZ", "KR"]);
        _validatorAdequate = _adequateProvider.GetRequiredService<ITransferValidator>();

        // Approved transfer path — one async service call
        _approvedProvider = BuildProvider(approvedRoutes: [("DE", "US", "personal-data")]);
        _validatorApproved = _approvedProvider.GetRequiredService<ITransferValidator>();

        // SCC path — one async service call + result processing
        _sccProvider = BuildProvider(sccProcessors: ["processor-1"]);
        _validatorSCC = _sccProvider.GetRequiredService<ITransferValidator>();

        // TIA path — deeper cascade (adequacy miss, approved miss, SCC miss, TIA hit)
        _tiaProvider = BuildProvider(tiaRoutes: [("DE", "BR", "financial-data")]);
        _validatorTIA = _tiaProvider.GetRequiredService<ITransferValidator>();

        // Block path — full cascade, all steps fail
        _blockProvider = BuildProvider();
        _validatorBlock = _blockProvider.GetRequiredService<ITransferValidator>();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _adequateProvider.Dispose();
        _approvedProvider.Dispose();
        _sccProvider.Dispose();
        _tiaProvider.Dispose();
        _blockProvider.Dispose();
    }

    // ────────────────────────────────────────────────────────────
    //  Adequacy Decision — Fastest Path (Baseline)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Validate: adequacy decision (fast path)")]
    public async Task<TransferValidationOutcome> Validate_AdequacyDecision()
    {
        var request = new TransferRequest
        {
            SourceCountryCode = "DE",
            DestinationCountryCode = "JP",
            DataCategory = "personal-data"
        };
        var result = await _validatorAdequate.ValidateAsync(request);
        return result.Match(Left: _ => null!, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Approved Transfer — One Service Call
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Validate: approved transfer")]
    public async Task<TransferValidationOutcome> Validate_ApprovedTransfer()
    {
        var request = new TransferRequest
        {
            SourceCountryCode = "DE",
            DestinationCountryCode = "US",
            DataCategory = "personal-data"
        };
        var result = await _validatorApproved.ValidateAsync(request);
        return result.Match(Left: _ => null!, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  SCC Agreement — Mid-Depth Cascade
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Validate: SCC agreement")]
    public async Task<TransferValidationOutcome> Validate_SCCAgreement()
    {
        var request = new TransferRequest
        {
            SourceCountryCode = "DE",
            DestinationCountryCode = "IN",
            DataCategory = "personal-data",
            ProcessorId = "processor-1"
        };
        var result = await _validatorSCC.ValidateAsync(request);
        return result.Match(Left: _ => null!, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  TIA — Deep Cascade
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Validate: TIA (deep cascade)")]
    public async Task<TransferValidationOutcome> Validate_TIA()
    {
        var request = new TransferRequest
        {
            SourceCountryCode = "DE",
            DestinationCountryCode = "BR",
            DataCategory = "financial-data"
        };
        var result = await _validatorTIA.ValidateAsync(request);
        return result.Match(Left: _ => null!, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Block — Full Cascade (Worst Case)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Validate: block (full cascade)")]
    public async Task<TransferValidationOutcome> Validate_Block()
    {
        var request = new TransferRequest
        {
            SourceCountryCode = "DE",
            DestinationCountryCode = "CN",
            DataCategory = "health-data"
        };
        var result = await _validatorBlock.ValidateAsync(request);
        return result.Match(Left: _ => null!, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Infrastructure
    // ────────────────────────────────────────────────────────────

    private static ServiceProvider BuildProvider(
        string[]? adequateCountries = null,
        (string Source, string Dest, string Category)[]? approvedRoutes = null,
        string[]? sccProcessors = null,
        (string Source, string Dest, string Category)[]? tiaRoutes = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var adequacyProvider = Substitute.For<IAdequacyDecisionProvider>();
        adequacyProvider.HasAdequacy(Arg.Any<Region>())
            .Returns(callInfo =>
            {
                var region = callInfo.Arg<Region>();
                return adequateCountries?.Contains(region.Code) == true;
            });
        services.AddSingleton(adequacyProvider);

        var approvedService = Substitute.For<IApprovedTransferService>();
#pragma warning disable CA2012 // ValueTask instances returned from NSubstitute mock setups are consumed by the framework
        approvedService.IsTransferApprovedAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var source = callInfo.ArgAt<string>(0);
                var dest = callInfo.ArgAt<string>(1);
                var category = callInfo.ArgAt<string>(2);
                var isApproved = approvedRoutes?.Any(t =>
                    t.Source == source && t.Dest == dest && t.Category == category) == true;
                return new ValueTask<Either<EncinaError, bool>>(Right<EncinaError, bool>(isApproved));
            });
        services.AddScoped<IApprovedTransferService>(_ => approvedService);

        var sccService = Substitute.For<ISCCService>();
        sccService.ValidateAgreementAsync(
                Arg.Any<string>(), Arg.Any<SCCModule>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var processorId = callInfo.ArgAt<string>(0);
                var isValid = sccProcessors?.Contains(processorId) == true;
                return new ValueTask<Either<EncinaError, SCCValidationResult>>(
                    Right<EncinaError, SCCValidationResult>(new SCCValidationResult
                    {
                        IsValid = isValid,
                        AgreementId = isValid ? Guid.NewGuid() : null,
                        Module = isValid ? SCCModule.ControllerToProcessor : null,
                        Version = isValid ? "2021/914" : null,
                        MissingMeasures = [],
                        Issues = isValid ? [] : ["No SCC agreement"]
                    }));
            });
        services.AddScoped<ISCCService>(_ => sccService);

        var tiaService = Substitute.For<ITIAService>();
        tiaService.GetTIAByRouteAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var source = callInfo.ArgAt<string>(0);
                var dest = callInfo.ArgAt<string>(1);
                var category = callInfo.ArgAt<string>(2);
                var match = tiaRoutes?.Any(t =>
                    t.Source == source && t.Dest == dest && t.Category == category) == true;
                if (match)
                {
                    return new ValueTask<Either<EncinaError, TIAReadModel>>(
                        Right<EncinaError, TIAReadModel>(new TIAReadModel
                        {
                            Id = Guid.NewGuid(),
                            SourceCountryCode = source,
                            DestinationCountryCode = dest,
                            DataCategory = category,
                            Status = TIAStatus.Completed,
                            RiskScore = 0.45,
                            CreatedAtUtc = DateTimeOffset.UtcNow,
                            LastModifiedAtUtc = DateTimeOffset.UtcNow,
                            RequiredSupplementaryMeasures = []
                        }));
                }
                return new ValueTask<Either<EncinaError, TIAReadModel>>(
                    Left<EncinaError, TIAReadModel>(EncinaError.New("TIA not found")));
            });
#pragma warning restore CA2012
        services.AddScoped<ITIAService>(_ => tiaService);

        services.AddEncinaCrossBorderTransfer(options =>
        {
            options.EnforcementMode = CrossBorderTransferEnforcementMode.Block;
            options.DefaultSourceCountryCode = "DE";
        });

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });
    }
}
