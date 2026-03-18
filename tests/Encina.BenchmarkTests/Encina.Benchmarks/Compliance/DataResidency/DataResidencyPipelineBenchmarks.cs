using BenchmarkDotNet.Attributes;
using Encina.Caching;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Abstractions;
using Encina.Compliance.DataResidency.Aggregates;
using Encina.Compliance.DataResidency.Attributes;
using Encina.Compliance.DataResidency.Model;
using Encina.Compliance.DataResidency.ReadModels;
using Encina.Marten;
using Encina.Marten.Projections;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.Benchmarks.Compliance.DataResidency;

/// <summary>
/// Benchmarks for the full data residency pipeline behavior end-to-end.
/// Measures the overhead of residency enforcement in the Encina pipeline comparing:
/// - Block mode with allowed region (allowed fast path)
/// - Block mode with non-allowed region (blocked)
/// - Warn mode (logs but proceeds)
/// - Disabled mode (skip validation entirely)
/// - No attribute (skip branch — attribute cache lookup only)
/// </summary>
/// <remarks>
/// <para>
/// These benchmarks measure the realistic pipeline overhead, including:
/// - Static attribute cache lookup (per-generic-type, CLR guarantee)
/// - Region context provider resolution
/// - Residency policy evaluation via IResidencyPolicyService
/// - OpenTelemetry instrumentation overhead
/// </para>
/// <para>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*DataResidencyPipelineBenchmarks*"
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class DataResidencyPipelineBenchmarks
{
    private ServiceProvider _blockAllowedProvider = null!;
    private ServiceProvider _blockNonAllowedProvider = null!;
    private ServiceProvider _warnProvider = null!;
    private ServiceProvider _disabledProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _blockAllowedProvider = BuildProvider(
            DataResidencyEnforcementMode.Block,
            allowedRegions: ["DE", "FR", "NL"],
            currentRegionCode: "DE");

        _blockNonAllowedProvider = BuildProvider(
            DataResidencyEnforcementMode.Block,
            allowedRegions: ["DE", "FR"],
            currentRegionCode: "US");

        _warnProvider = BuildProvider(
            DataResidencyEnforcementMode.Warn,
            allowedRegions: ["DE"],
            currentRegionCode: "US");

        _disabledProvider = BuildProvider(
            DataResidencyEnforcementMode.Disabled,
            allowedRegions: ["DE"],
            currentRegionCode: "US");
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _blockAllowedProvider.Dispose();
        _blockNonAllowedProvider.Dispose();
        _warnProvider.Dispose();
        _disabledProvider.Dispose();
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Block Mode, Allowed Region (Baseline)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Pipeline: Block mode, allowed region")]
    public async Task<int> Pipeline_BlockMode_Allowed()
    {
        using var scope = _blockAllowedProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new ResidencyBenchCommand("DE");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Block Mode, Non-Allowed Region (Blocked)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: Block mode, non-allowed (blocked)")]
    public async Task<int> Pipeline_BlockMode_Blocked()
    {
        using var scope = _blockNonAllowedProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new ResidencyBenchCommand("US");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Warn Mode (Proceeds with Warning)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: Warn mode, non-allowed")]
    public async Task<int> Pipeline_WarnMode()
    {
        using var scope = _warnProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new ResidencyBenchCommand("US");
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
        var command = new ResidencyBenchCommand("US");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — No Attribute (Skip Branch)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: No attribute (skip branch)")]
    public async Task<int> Pipeline_NoAttribute()
    {
        using var scope = _blockAllowedProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new NoResidencyBenchCommand("any-data");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Infrastructure
    // ────────────────────────────────────────────────────────────

    private static ServiceProvider BuildProvider(
        DataResidencyEnforcementMode mode,
        string[] allowedRegions,
        string currentRegionCode)
    {
        var services = new ServiceCollection();
        services.AddEncina(config =>
            config.RegisterServicesFromAssemblyContaining<ResidencyBenchCommand>());

        // Mock Marten repositories
        var policyReadRepo = Substitute.For<IReadModelRepository<ResidencyPolicyReadModel>>();
        var policyReadModel = new ResidencyPolicyReadModel
        {
            Id = Guid.NewGuid(),
            DataCategory = "personal-data",
            AllowedRegionCodes = allowedRegions,
            RequireAdequacyDecision = false,
            AllowedTransferBases = [TransferLegalBasis.StandardContractualClauses],
            IsActive = true,
            Version = 1
        };
        policyReadRepo.QueryAsync(
                Arg.Any<Func<IQueryable<ResidencyPolicyReadModel>, IQueryable<ResidencyPolicyReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Right<EncinaError, IReadOnlyList<ResidencyPolicyReadModel>>(
                    new List<ResidencyPolicyReadModel> { policyReadModel })));

        services.AddScoped<IAggregateRepository<ResidencyPolicyAggregate>>(_ =>
            Substitute.For<IAggregateRepository<ResidencyPolicyAggregate>>());
        services.AddScoped<IAggregateRepository<DataLocationAggregate>>(_ =>
            Substitute.For<IAggregateRepository<DataLocationAggregate>>());
        services.AddScoped<IReadModelRepository<ResidencyPolicyReadModel>>(_ => policyReadRepo);
        services.AddScoped<IReadModelRepository<DataLocationReadModel>>(_ =>
            Substitute.For<IReadModelRepository<DataLocationReadModel>>());
        services.AddSingleton(_ => Substitute.For<ICacheProvider>());

        // Mock IRegionContextProvider to return specified region
        var currentRegion = RegionRegistry.GetByCode(currentRegionCode)!;
        var regionProvider = Substitute.For<IRegionContextProvider>();
#pragma warning disable CA2012
        regionProvider.GetCurrentRegionAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Region>>(
                Right<EncinaError, Region>(currentRegion)));
#pragma warning restore CA2012
        services.AddSingleton(regionProvider);

        services.AddEncinaDataResidency(options =>
        {
            options.EnforcementMode = mode;
            options.AutoRegisterFromAttributes = false;
        });

        services.AddScoped<IRequestHandler<ResidencyBenchCommand, int>, ResidencyBenchHandler>();
        services.AddScoped<IRequestHandler<NoResidencyBenchCommand, int>, NoResidencyBenchHandler>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });
    }

    [DataResidency("DE", "FR", "NL", DataCategory = "personal-data")]
    private sealed record ResidencyBenchCommand(string Region) : IRequest<int>;

    private sealed class ResidencyBenchHandler : IRequestHandler<ResidencyBenchCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(ResidencyBenchCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(42));
    }

    private sealed record NoResidencyBenchCommand(string Data) : IRequest<int>;

    private sealed class NoResidencyBenchHandler : IRequestHandler<NoResidencyBenchCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(NoResidencyBenchCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(99));
    }
}
