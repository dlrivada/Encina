using BenchmarkDotNet.Attributes;
using Encina.Compliance.GDPR;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.Benchmarks.Compliance.GDPR;

/// <summary>
/// Benchmarks for the GDPR compliance pipeline behavior end-to-end.
/// Measures the overhead of GDPR compliance checks in the Encina pipeline comparing:
/// - Enforce mode with registered activity (compliant fast path)
/// - Enforce mode with unregistered activity (blocked)
/// - WarnOnly mode (logs but proceeds)
/// - No attribute (skip branch — attribute cache lookup only)
/// </summary>
/// <remarks>
/// <para>
/// These benchmarks measure the realistic pipeline overhead, including:
/// - Attribute cache lookup (ConcurrentDictionary)
/// - Processing activity registry lookup
/// - Compliance validator invocation
/// - OpenTelemetry instrumentation overhead
/// </para>
/// <para>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*GDPRPipelineBenchmarks*"
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class GDPRPipelineBenchmarks
{
    private ServiceProvider _enforceRegisteredProvider = null!;
    private ServiceProvider _enforceUnregisteredProvider = null!;
    private ServiceProvider _warnOnlyProvider = null!;
    private ServiceProvider _noAttributeProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _enforceRegisteredProvider = BuildProvider(GDPREnforcementMode.Enforce, registerActivity: true);
        _enforceUnregisteredProvider = BuildProvider(GDPREnforcementMode.Enforce, registerActivity: false, blockUnregistered: true);
        _warnOnlyProvider = BuildProvider(GDPREnforcementMode.WarnOnly, registerActivity: true);
        _noAttributeProvider = BuildProvider(GDPREnforcementMode.Enforce, registerActivity: true);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _enforceRegisteredProvider.Dispose();
        _enforceUnregisteredProvider.Dispose();
        _warnOnlyProvider.Dispose();
        _noAttributeProvider.Dispose();
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Enforce Mode, Registered Activity (Baseline)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Pipeline: Enforce, registered (compliant)")]
    public async Task<int> Pipeline_Enforce_Registered()
    {
        using var scope = _enforceRegisteredProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new GDPRBenchCommand("Order processing");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Enforce Mode, Unregistered Activity (Blocked)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: Enforce, unregistered (blocked)")]
    public async Task<int> Pipeline_Enforce_Unregistered()
    {
        using var scope = _enforceUnregisteredProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new GDPRBenchCommand("Unregistered processing");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — WarnOnly Mode (Proceeds with Warning)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: WarnOnly mode")]
    public async Task<int> Pipeline_WarnOnly()
    {
        using var scope = _warnOnlyProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new GDPRBenchCommand("Warn-only processing");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — No Attribute (Skip Branch)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: No attribute (skip branch)")]
    public async Task<int> Pipeline_NoAttribute()
    {
        using var scope = _noAttributeProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new NoGDPRBenchCommand("any-data");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Infrastructure
    // ────────────────────────────────────────────────────────────

    private static ServiceProvider BuildProvider(
        GDPREnforcementMode mode,
        bool registerActivity,
        bool blockUnregistered = false)
    {
        var services = new ServiceCollection();
        services.AddEncina(config =>
            config.RegisterServicesFromAssemblyContaining<GDPRBenchCommand>());

        services.AddEncinaGDPR(options =>
        {
            options.ControllerName = "Benchmark Corp";
            options.ControllerEmail = "privacy@benchmark.com";
            options.EnforcementMode = mode;
            options.BlockUnregisteredProcessing = blockUnregistered;
            options.AutoRegisterFromAttributes = false;
        });

        services.AddScoped<IRequestHandler<GDPRBenchCommand, int>, GDPRBenchHandler>();
        services.AddScoped<IRequestHandler<NoGDPRBenchCommand, int>, NoGDPRBenchHandler>();

        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });

        if (registerActivity)
        {
            var registry = provider.GetRequiredService<IProcessingActivityRegistry>();
            var now = DateTimeOffset.UtcNow;
            var activity = new ProcessingActivity
            {
                Id = Guid.NewGuid(),
                Name = "BenchmarkOrderProcessing",
                Purpose = "Benchmark order fulfillment",
                LawfulBasis = LawfulBasis.Contract,
                CategoriesOfDataSubjects = ["Customers"],
                CategoriesOfPersonalData = ["Name", "Email", "Address"],
                Recipients = ["Shipping Provider"],
                RetentionPeriod = TimeSpan.FromDays(2555),
                SecurityMeasures = "AES-256 encryption at rest",
                RequestType = typeof(GDPRBenchCommand),
                CreatedAtUtc = now,
                LastUpdatedAtUtc = now
            };
            registry.RegisterActivityAsync(activity).AsTask().GetAwaiter().GetResult();
        }

        return provider;
    }

    [ProcessingActivity(
        Purpose = "Benchmark order fulfillment",
        LawfulBasis = LawfulBasis.Contract,
        DataCategories = ["Name", "Email", "Address"],
        DataSubjects = ["Customers"],
        RetentionDays = 2555)]
    private sealed record GDPRBenchCommand(string Purpose) : IRequest<int>;

    private sealed class GDPRBenchHandler : IRequestHandler<GDPRBenchCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(GDPRBenchCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(42));
    }

    // Command WITHOUT GDPR attributes — measures the "no attribute" branch
    private sealed record NoGDPRBenchCommand(string Data) : IRequest<int>;

    private sealed class NoGDPRBenchHandler : IRequestHandler<NoGDPRBenchCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(NoGDPRBenchCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(99));
    }
}
