using BenchmarkDotNet.Attributes;
using Encina.Caching;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Abstractions;
using Encina.Compliance.DataResidency.Aggregates;
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
/// Benchmarks for the data residency policy evaluation and region lookup operations.
/// Measures throughput and allocations for:
/// - Region registry lookup (baseline — static dictionary, zero I/O)
/// - Policy matching via IResidencyPolicyService.IsAllowedAsync
/// - Allowed regions retrieval via IResidencyPolicyService.GetAllowedRegionsAsync
/// - Cross-border transfer validation via ICrossBorderTransferValidator
/// </summary>
/// <remarks>
/// <para>
/// Data residency checks execute on every request decorated with <c>[DataResidency]</c>.
/// Region validation and policy evaluation are hot-path operations that directly impact
/// request latency. Benchmarking ensures compliance overhead stays within bounds.
/// </para>
/// <para>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*ResidencyPolicyBenchmarks*"
///
/// # Quick validation:
/// dotnet run -c Release -- --filter "*ResidencyPolicyBenchmarks*" --job short
///
/// # List available benchmarks:
/// dotnet run -c Release -- --list flat --filter "*ResidencyPolicy*"
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class ResidencyPolicyBenchmarks
{
    private IResidencyPolicyService _policyServiceAllowed = null!;
    private IResidencyPolicyService _policyServiceBlocked = null!;
    private ICrossBorderTransferValidator _transferValidator = null!;

    private ServiceProvider _allowedProvider = null!;
    private ServiceProvider _blockedProvider = null!;

    private Region _allowedRegion = null!;
    private Region _blockedRegion = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Allowed path — region is in the policy's allowed list
        _allowedProvider = BuildProvider(["DE", "FR", "NL", "BE", "AT"]);
        var allowedScope = _allowedProvider.CreateScope();
        _policyServiceAllowed = allowedScope.ServiceProvider.GetRequiredService<IResidencyPolicyService>();
        _transferValidator = _allowedProvider.GetRequiredService<ICrossBorderTransferValidator>();

        // Blocked path — region is NOT in the policy's allowed list
        _blockedProvider = BuildProvider(["DE", "FR"]);
        var blockedScope = _blockedProvider.CreateScope();
        _policyServiceBlocked = blockedScope.ServiceProvider.GetRequiredService<IResidencyPolicyService>();

        _allowedRegion = RegionRegistry.DE;
        _blockedRegion = RegionRegistry.GetByCode("US")!;
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _allowedProvider.Dispose();
        _blockedProvider.Dispose();
    }

    // ────────────────────────────────────────────────────────────
    //  Region Lookup — Baseline (Static Dictionary)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Region lookup (baseline)")]
    public Region? RegionLookup()
    {
        return RegionRegistry.GetByCode("DE");
    }

    // ────────────────────────────────────────────────────────────
    //  Policy Matching — Allowed Region
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Policy: IsAllowed (allowed region)")]
    public async Task<bool> PolicyMatching_Allowed()
    {
        var result = await _policyServiceAllowed.IsAllowedAsync("personal-data", _allowedRegion);
        return result.Match(Right: r => r, Left: _ => false);
    }

    // ────────────────────────────────────────────────────────────
    //  Policy Matching — Blocked Region
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Policy: IsAllowed (blocked region)")]
    public async Task<bool> PolicyMatching_Blocked()
    {
        var result = await _policyServiceBlocked.IsAllowedAsync("personal-data", _blockedRegion);
        return result.Match(Right: r => r, Left: _ => false);
    }

    // ────────────────────────────────────────────────────────────
    //  Allowed Regions Retrieval
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Policy: GetAllowedRegions")]
    public async Task<IReadOnlyList<Region>> GetAllowedRegions()
    {
        var result = await _policyServiceAllowed.GetAllowedRegionsAsync("personal-data");
        return result.Match(Right: r => r, Left: _ => []);
    }

    // ────────────────────────────────────────────────────────────
    //  Cross-Border Transfer — Intra-EEA (Fast Path)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Transfer: intra-EEA (DE→FR)")]
    public async Task<TransferValidationResult> TransferValidation_IntraEEA()
    {
        var result = await _transferValidator.ValidateTransferAsync(
            RegionRegistry.DE, RegionRegistry.FR, "personal-data");
        return result.Match(Right: r => r, Left: _ => default!);
    }

    // ────────────────────────────────────────────────────────────
    //  Cross-Border Transfer — Third Country
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Transfer: third country (DE→US)")]
    public async Task<TransferValidationResult> TransferValidation_ThirdCountry()
    {
        var result = await _transferValidator.ValidateTransferAsync(
            RegionRegistry.DE, RegionRegistry.GetByCode("US")!, "personal-data");
        return result.Match(Right: r => r, Left: _ => default!);
    }

    // ────────────────────────────────────────────────────────────
    //  Infrastructure
    // ────────────────────────────────────────────────────────────

    private static ServiceProvider BuildProvider(string[] allowedRegions)
    {
        var services = new ServiceCollection();
        services.AddLogging();

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

        services.AddEncinaDataResidency(options =>
        {
            options.EnforcementMode = DataResidencyEnforcementMode.Block;
            options.AutoRegisterFromAttributes = false;
        });

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });
    }
}
