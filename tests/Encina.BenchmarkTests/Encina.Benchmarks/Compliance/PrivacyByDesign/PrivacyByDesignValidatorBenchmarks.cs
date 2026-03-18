using BenchmarkDotNet.Attributes;
using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Benchmarks.Compliance.PrivacyByDesign;

/// <summary>
/// Benchmarks for the Privacy by Design validator and analyzer.
/// Measures throughput and allocations for each step of GDPR Article 25 validation:
/// - Data minimization analysis (cached reflection + property inspection)
/// - Purpose limitation check (purpose registry lookup + field matching)
/// - Default privacy inspection (default value comparison)
/// - Full validation (all three combined)
/// - Annotation scanning overhead (first-call vs cached)
/// </summary>
/// <remarks>
/// <para>
/// Privacy by Design validation executes on every request decorated with
/// <c>[EnforceDataMinimization]</c>. Benchmarking the overhead is essential for
/// capacity planning in high-throughput GDPR-regulated systems.
/// </para>
/// <para>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*PrivacyByDesignValidatorBenchmarks*"
///
/// # Quick validation:
/// dotnet run -c Release -- --filter "*PrivacyByDesignValidatorBenchmarks*" --job short
///
/// # List available benchmarks:
/// dotnet run -c Release -- --list flat --filter "*PrivacyByDesignValidator*"
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class PrivacyByDesignValidatorBenchmarks
{
    private IPrivacyByDesignValidator _validator = null!;
    private IDataMinimizationAnalyzer _analyzer = null!;
    private ServiceProvider _provider = null!;

    private CompliantBenchRequest _compliantRequest = null!;
    private NonCompliantBenchRequest _nonCompliantRequest = null!;
    private DefaultsBenchRequest _defaultsRequest = null!;
    private LargeFieldBenchRequest _largeFieldRequest = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _provider = BuildProvider();

        var scope = _provider.CreateScope();
        _validator = scope.ServiceProvider.GetRequiredService<IPrivacyByDesignValidator>();
        _analyzer = _provider.GetRequiredService<IDataMinimizationAnalyzer>();

        _compliantRequest = new CompliantBenchRequest { ProductId = "P001", Quantity = 5 };
        _nonCompliantRequest = new NonCompliantBenchRequest
        {
            ProductId = "P001",
            ReferralSource = "Google Ads",
            CampaignCode = "SUMMER2026"
        };
        _defaultsRequest = new DefaultsBenchRequest
        {
            ShareData = true, // overrides default of false
            MarketingConsent = "opt-in" // overrides default of null
        };
        _largeFieldRequest = new LargeFieldBenchRequest
        {
            Field1 = "v1", Field2 = "v2", Field3 = "v3", Field4 = "v4", Field5 = "v5",
            Field6 = "v6", Field7 = "v7", Field8 = "v8", Field9 = "v9", Field10 = "v10",
            Optional1 = "extra1", Optional2 = "extra2", Optional3 = "extra3",
            Optional4 = "extra4", Optional5 = "extra5"
        };

        // Warm up caches to measure steady-state performance
        _validator.ValidateAsync(_compliantRequest).AsTask().GetAwaiter().GetResult();
        _validator.ValidateAsync(_nonCompliantRequest).AsTask().GetAwaiter().GetResult();
        _validator.ValidateDefaultsAsync(_defaultsRequest).AsTask().GetAwaiter().GetResult();
        _analyzer.AnalyzeAsync(_largeFieldRequest).AsTask().GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _provider.Dispose();
    }

    // ────────────────────────────────────────────────────────────
    //  Full Validation — Compliant Request (Baseline)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Validate: compliant request (fast path)")]
    public async Task<PrivacyValidationResult> Validate_Compliant()
    {
        var result = await _validator.ValidateAsync(_compliantRequest);
        return result.Match(Left: _ => null!, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Full Validation — Non-Compliant Request (Violations)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Validate: non-compliant (violations detected)")]
    public async Task<PrivacyValidationResult> Validate_NonCompliant()
    {
        var result = await _validator.ValidateAsync(_nonCompliantRequest);
        return result.Match(Left: _ => null!, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Data Minimization Analysis Only
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Analyze: data minimization only")]
    public async Task<MinimizationReport> Analyze_Minimization()
    {
        var result = await _analyzer.AnalyzeAsync(_nonCompliantRequest);
        return result.Match(Left: _ => null!, Right: r => r);
    }

    // ────────────────────────────────────────────────────────────
    //  Default Privacy Inspection
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Validate: default privacy inspection")]
    public async Task<IReadOnlyList<DefaultPrivacyFieldInfo>> Validate_Defaults()
    {
        var result = await _validator.ValidateDefaultsAsync(_defaultsRequest);
        return result.Match(Left: _ => null!, Right: d => d);
    }

    // ────────────────────────────────────────────────────────────
    //  Large Field Count (15 properties)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Analyze: large field count (15 properties)")]
    public async Task<MinimizationReport> Analyze_LargeFieldCount()
    {
        var result = await _analyzer.AnalyzeAsync(_largeFieldRequest);
        return result.Match(Left: _ => null!, Right: r => r);
    }

    // ────────────────────────────────────────────────────────────
    //  Infrastructure
    // ────────────────────────────────────────────────────────────

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaPrivacyByDesign(options =>
        {
            options.EnforcementMode = PrivacyByDesignEnforcementMode.Block;
            options.PrivacyLevel = PrivacyLevel.Maximum;
            options.MinimizationScoreThreshold = 0.0;
        });

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });
    }

    [EnforceDataMinimization]
    private sealed class CompliantBenchRequest
    {
        public string ProductId { get; set; } = "";
        public int Quantity { get; set; }
    }

    [EnforceDataMinimization]
    private sealed class NonCompliantBenchRequest
    {
        public string ProductId { get; set; } = "";

        [NotStrictlyNecessary(Reason = "Analytics only")]
        public string? ReferralSource { get; set; }

        [NotStrictlyNecessary(Reason = "Marketing campaign")]
        public string? CampaignCode { get; set; }
    }

    private sealed class DefaultsBenchRequest
    {
        [PrivacyDefault(false)]
        public bool ShareData { get; set; }

        [PrivacyDefault(null)]
        public string? MarketingConsent { get; set; }
    }

    [EnforceDataMinimization]
    private sealed class LargeFieldBenchRequest
    {
        public string Field1 { get; set; } = "";
        public string Field2 { get; set; } = "";
        public string Field3 { get; set; } = "";
        public string Field4 { get; set; } = "";
        public string Field5 { get; set; } = "";
        public string Field6 { get; set; } = "";
        public string Field7 { get; set; } = "";
        public string Field8 { get; set; } = "";
        public string Field9 { get; set; } = "";
        public string Field10 { get; set; } = "";

        [NotStrictlyNecessary(Reason = "Optional data")]
        public string? Optional1 { get; set; }

        [NotStrictlyNecessary(Reason = "Optional data")]
        public string? Optional2 { get; set; }

        [NotStrictlyNecessary(Reason = "Optional data")]
        public string? Optional3 { get; set; }

        [NotStrictlyNecessary(Reason = "Optional data")]
        public string? Optional4 { get; set; }

        [NotStrictlyNecessary(Reason = "Optional data")]
        public string? Optional5 { get; set; }
    }
}
