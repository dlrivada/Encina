using BenchmarkDotNet.Attributes;
using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Model;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.Benchmarks.Compliance.PrivacyByDesign;

/// <summary>
/// Benchmarks for the full Privacy by Design pipeline behavior end-to-end.
/// Measures the overhead of data minimization enforcement in the Encina pipeline comparing:
/// - Block mode with compliant request (allowed fast path)
/// - Block mode with non-compliant request (blocked with violations)
/// - Warn mode (logs but proceeds)
/// - Disabled mode (skip validation entirely)
/// - No attribute (skip branch — attribute cache lookup only)
/// </summary>
/// <remarks>
/// <para>
/// These benchmarks measure the realistic pipeline overhead, including:
/// - Attribute cache lookup (ConcurrentDictionary)
/// - Privacy validation chain invocation
/// - OpenTelemetry instrumentation overhead
/// - Notification publishing overhead (non-blocking)
/// </para>
/// <para>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*PrivacyByDesignPipelineBenchmarks*"
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class PrivacyByDesignPipelineBenchmarks
{
    private ServiceProvider _blockCompliantProvider = null!;
    private ServiceProvider _blockNonCompliantProvider = null!;
    private ServiceProvider _warnProvider = null!;
    private ServiceProvider _disabledProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _blockCompliantProvider = BuildProvider(PrivacyByDesignEnforcementMode.Block);
        _blockNonCompliantProvider = BuildProvider(PrivacyByDesignEnforcementMode.Block);
        _warnProvider = BuildProvider(PrivacyByDesignEnforcementMode.Warn);
        _disabledProvider = BuildProvider(PrivacyByDesignEnforcementMode.Disabled);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _blockCompliantProvider.Dispose();
        _blockNonCompliantProvider.Dispose();
        _warnProvider.Dispose();
        _disabledProvider.Dispose();
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Block Mode, Compliant Request (Baseline)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Pipeline: Block mode, compliant request")]
    public async Task<int> Pipeline_BlockMode_Compliant()
    {
        using var scope = _blockCompliantProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new PrivacyBenchCommand { ProductId = "P001", Quantity = 5 };
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Block Mode, Non-Compliant Request (Blocked)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: Block mode, non-compliant (blocked)")]
    public async Task<int> Pipeline_BlockMode_NonCompliant()
    {
        using var scope = _blockNonCompliantProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new PrivacyBenchCommand
        {
            ProductId = "P001",
            Quantity = 5,
            TrackingId = "track-123" // unnecessary field with value
        };
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Warn Mode (Proceeds with Warning)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: Warn mode, non-compliant")]
    public async Task<int> Pipeline_WarnMode()
    {
        using var scope = _warnProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new PrivacyBenchCommand
        {
            ProductId = "P001",
            Quantity = 5,
            TrackingId = "track-123"
        };
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
        var command = new PrivacyBenchCommand
        {
            ProductId = "P001",
            Quantity = 5,
            TrackingId = "track-123"
        };
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — No Attribute (Skip Branch)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: No attribute (skip branch)")]
    public async Task<int> Pipeline_NoAttribute()
    {
        using var scope = _blockCompliantProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new NoPrivacyBenchCommand("any-data");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Infrastructure
    // ────────────────────────────────────────────────────────────

    private static ServiceProvider BuildProvider(PrivacyByDesignEnforcementMode mode)
    {
        var services = new ServiceCollection();
        services.AddEncina(config =>
            config.RegisterServicesFromAssemblyContaining<PrivacyBenchCommand>());

        services.AddEncinaPrivacyByDesign(options =>
        {
            options.EnforcementMode = mode;
            options.PrivacyLevel = PrivacyLevel.Maximum;
            options.MinimizationScoreThreshold = 0.0;
        });

        services.AddScoped<IRequestHandler<PrivacyBenchCommand, int>, PrivacyBenchHandler>();
        services.AddScoped<IRequestHandler<NoPrivacyBenchCommand, int>, NoPrivacyBenchHandler>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });
    }

    [EnforceDataMinimization]
    private sealed class PrivacyBenchCommand : IRequest<int>
    {
        public string ProductId { get; set; } = "";
        public int Quantity { get; set; }

        [NotStrictlyNecessary(Reason = "Analytics tracking")]
        public string? TrackingId { get; set; }
    }

    private sealed class PrivacyBenchHandler : IRequestHandler<PrivacyBenchCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(PrivacyBenchCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(42));
    }

    // Command WITHOUT [EnforceDataMinimization] — measures the "no attribute" branch
    private sealed record NoPrivacyBenchCommand(string Data) : IRequest<int>;

    private sealed class NoPrivacyBenchHandler : IRequestHandler<NoPrivacyBenchCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(NoPrivacyBenchCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(99));
    }
}
