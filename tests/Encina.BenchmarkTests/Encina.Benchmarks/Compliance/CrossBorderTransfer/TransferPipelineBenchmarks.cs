using BenchmarkDotNet.Attributes;
using Encina.Compliance.CrossBorderTransfer;
using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Attributes;
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
/// Benchmarks for the full cross-border transfer pipeline behavior end-to-end.
/// Measures the overhead of transfer validation in the Encina pipeline comparing:
/// - Block mode with adequate destination (allowed fast path)
/// - Block mode with non-adequate destination (blocked)
/// - Warn mode (logs but proceeds)
/// - Disabled mode (skip validation entirely)
/// - No attribute (skip branch — attribute cache lookup only)
/// </summary>
/// <remarks>
/// <para>
/// These benchmarks measure the realistic pipeline overhead, including:
/// - Attribute cache lookup (ConcurrentDictionary)
/// - Destination country extraction via cached reflection
/// - Transfer validation chain invocation
/// - OpenTelemetry instrumentation overhead
/// </para>
/// <para>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*TransferPipelineBenchmarks*"
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class TransferPipelineBenchmarks
{
    private ServiceProvider _blockAdequateProvider = null!;
    private ServiceProvider _blockNonAdequateProvider = null!;
    private ServiceProvider _warnProvider = null!;
    private ServiceProvider _disabledProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _blockAdequateProvider = BuildProvider(CrossBorderTransferEnforcementMode.Block,
            adequateCountries: ["JP", "GB"]);
        _blockNonAdequateProvider = BuildProvider(CrossBorderTransferEnforcementMode.Block);
        _warnProvider = BuildProvider(CrossBorderTransferEnforcementMode.Warn);
        _disabledProvider = BuildProvider(CrossBorderTransferEnforcementMode.Disabled);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _blockAdequateProvider.Dispose();
        _blockNonAdequateProvider.Dispose();
        _warnProvider.Dispose();
        _disabledProvider.Dispose();
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Block Mode, Adequate Destination (Baseline)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Pipeline: Block mode, adequate destination")]
    public async Task<int> Pipeline_BlockMode_Adequate()
    {
        using var scope = _blockAdequateProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new TransferBenchCommand("JP");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Block Mode, Non-Adequate Destination (Blocked)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: Block mode, non-adequate (blocked)")]
    public async Task<int> Pipeline_BlockMode_Blocked()
    {
        using var scope = _blockNonAdequateProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new TransferBenchCommand("CN");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Warn Mode (Proceeds with Warning)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: Warn mode, non-adequate")]
    public async Task<int> Pipeline_WarnMode()
    {
        using var scope = _warnProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new TransferBenchCommand("CN");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Disabled Mode (Skip Validation)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: Disabled mode (no validation)")]
    public async Task<int> Pipeline_DisabledMode()
    {
        using var scope = _disabledProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new TransferBenchCommand("CN");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — No Attribute (Skip Branch)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: No attribute (skip branch)")]
    public async Task<int> Pipeline_NoAttribute()
    {
        using var scope = _blockAdequateProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new NoTransferBenchCommand("any-data");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Infrastructure
    // ────────────────────────────────────────────────────────────

    private static ServiceProvider BuildProvider(
        CrossBorderTransferEnforcementMode mode,
        string[]? adequateCountries = null)
    {
        var services = new ServiceCollection();
        services.AddEncina(config =>
            config.RegisterServicesFromAssemblyContaining<TransferBenchCommand>());

        // Register mocked dependencies before AddEncinaCrossBorderTransfer
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
            .Returns(new ValueTask<Either<EncinaError, bool>>(Right<EncinaError, bool>(false)));
        services.AddScoped<IApprovedTransferService>(_ => approvedService);

        var sccService = Substitute.For<ISCCService>();
        sccService.ValidateAgreementAsync(
                Arg.Any<string>(), Arg.Any<SCCModule>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, SCCValidationResult>>(
                Right<EncinaError, SCCValidationResult>(new SCCValidationResult
                {
                    IsValid = false,
                    MissingMeasures = [],
                    Issues = ["No SCC agreement"]
                })));
        services.AddScoped<ISCCService>(_ => sccService);

        var tiaService = Substitute.For<ITIAService>();
        tiaService.GetTIAByRouteAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, TIAReadModel>>(
                Left<EncinaError, TIAReadModel>(EncinaError.New("TIA not found"))));
#pragma warning restore CA2012
        services.AddScoped<ITIAService>(_ => tiaService);

        services.AddEncinaCrossBorderTransfer(options =>
        {
            options.EnforcementMode = mode;
            options.DefaultSourceCountryCode = "DE";
        });

        services.AddScoped<IRequestHandler<TransferBenchCommand, int>, TransferBenchHandler>();
        services.AddScoped<IRequestHandler<NoTransferBenchCommand, int>, NoTransferBenchHandler>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });
    }

    [RequiresCrossBorderTransfer(DestinationProperty = nameof(DestinationCountryCode), DataCategory = "personal-data")]
    private sealed record TransferBenchCommand(string DestinationCountryCode) : IRequest<int>;

    private sealed class TransferBenchHandler : IRequestHandler<TransferBenchCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(TransferBenchCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(42));
    }

    // Command WITHOUT [RequiresCrossBorderTransfer] — measures the "no attribute" branch
    private sealed record NoTransferBenchCommand(string Data) : IRequest<int>;

    private sealed class NoTransferBenchHandler : IRequestHandler<NoTransferBenchCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(NoTransferBenchCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(99));
    }
}
