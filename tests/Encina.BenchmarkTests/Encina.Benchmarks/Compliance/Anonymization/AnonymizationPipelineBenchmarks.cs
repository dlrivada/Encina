using BenchmarkDotNet.Attributes;
using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.Benchmarks.Compliance.Anonymization;

/// <summary>
/// Benchmarks for the full anonymization pipeline behavior end-to-end.
/// Measures the overhead of automatic attribute-based anonymization in the Encina pipeline comparing:
/// - Block mode with anonymization attributes (full transformation)
/// - Warn mode (logs but proceeds)
/// - Disabled mode (skip transformation entirely)
/// - No attribute (skip branch — attribute cache lookup only)
/// </summary>
/// <remarks>
/// <para>
/// These benchmarks measure the realistic pipeline overhead, including:
/// - Attribute cache lookup (static ConcurrentDictionary per closed generic type)
/// - Reflection-based property scanning (cached after first call)
/// - Cryptographic transformation via IAnonymizer/IPseudonymizer/ITokenizer
/// - OpenTelemetry instrumentation overhead
/// </para>
/// <para>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*AnonymizationPipelineBenchmarks*"
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class AnonymizationPipelineBenchmarks
{
    private ServiceProvider _blockProvider = null!;
    private ServiceProvider _warnProvider = null!;
    private ServiceProvider _disabledProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _blockProvider = BuildProvider(AnonymizationEnforcementMode.Block);
        _warnProvider = BuildProvider(AnonymizationEnforcementMode.Warn);
        _disabledProvider = BuildProvider(AnonymizationEnforcementMode.Disabled);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _blockProvider.Dispose();
        _warnProvider.Dispose();
        _disabledProvider.Dispose();
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Block Mode with Anonymization (Baseline)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Pipeline: Block mode, with [Anonymize] attribute")]
    public async Task<int> Pipeline_BlockMode_WithAnonymize()
    {
        using var scope = _blockProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new AnonymizeBenchCommand("John Smith", "john@example.com");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Warn Mode (Proceeds with Warning)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: Warn mode, with [Anonymize] attribute")]
    public async Task<int> Pipeline_WarnMode()
    {
        using var scope = _warnProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new AnonymizeBenchCommand("Maria Garcia", "maria@company.org");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Disabled Mode (Skip Transformation)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: Disabled mode (no transformation)")]
    public async Task<int> Pipeline_DisabledMode()
    {
        using var scope = _disabledProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new AnonymizeBenchCommand("Wei Zhang", "wei@university.edu");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — No Attribute (Skip Branch)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: No attribute (skip branch)")]
    public async Task<int> Pipeline_NoAttribute()
    {
        using var scope = _blockProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new NoAnonymizeBenchCommand("any-data");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Infrastructure
    // ────────────────────────────────────────────────────────────

    private static ServiceProvider BuildProvider(AnonymizationEnforcementMode mode)
    {
        var services = new ServiceCollection();
        services.AddEncina(config =>
            config.RegisterServicesFromAssemblyContaining<AnonymizeBenchCommand>());

        services.AddEncinaAnonymization(options =>
        {
            options.EnforcementMode = mode;
            options.TrackAuditTrail = false;
            options.AutoRegisterFromAttributes = false;
        });

        services.AddScoped<IRequestHandler<AnonymizeBenchCommand, int>, AnonymizeBenchHandler>();
        services.AddScoped<IRequestHandler<NoAnonymizeBenchCommand, int>, NoAnonymizeBenchHandler>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });
    }

    // Command WITH response type that has [Anonymize] attributes
    private sealed record AnonymizeBenchCommand(string Name, string Email) : IRequest<int>;

    private sealed class AnonymizeBenchHandler : IRequestHandler<AnonymizeBenchCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(AnonymizeBenchCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(42));
    }

    // Command WITHOUT any anonymization attributes — measures the "no attribute" branch
    private sealed record NoAnonymizeBenchCommand(string Data) : IRequest<int>;

    private sealed class NoAnonymizeBenchHandler : IRequestHandler<NoAnonymizeBenchCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(NoAnonymizeBenchCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(99));
    }
}
