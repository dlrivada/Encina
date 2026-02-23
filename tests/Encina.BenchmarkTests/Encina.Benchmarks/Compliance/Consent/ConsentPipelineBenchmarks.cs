using BenchmarkDotNet.Attributes;
using Encina.Compliance.Consent;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.Benchmarks.Compliance.Consent;

/// <summary>
/// Benchmarks for the full consent pipeline behavior end-to-end.
/// Measures the overhead of consent validation in the Encina pipeline
/// comparing with-consent vs without-consent request processing.
/// </summary>
/// <remarks>
/// These benchmarks measure the realistic pipeline overhead, including:
/// - Attribute cache lookup
/// - Subject ID extraction via cached reflection
/// - Consent store lookup
/// - Validation result processing
/// - OpenTelemetry instrumentation overhead
///
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*ConsentPipelineBenchmarks*"
/// </code>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class ConsentPipelineBenchmarks
{
    private ServiceProvider _consentProvider = null!;
    private ServiceProvider _noConsentProvider = null!;
    private ServiceProvider _warnModeProvider = null!;
    private ServiceProvider _disabledProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Setup: Block mode with consent available (happy path)
        _consentProvider = BuildProvider(ConsentEnforcementMode.Block, seedConsent: true);

        // Setup: Block mode without consent (blocked path)
        _noConsentProvider = BuildProvider(ConsentEnforcementMode.Block, seedConsent: false);

        // Setup: Warn mode (logs but proceeds)
        _warnModeProvider = BuildProvider(ConsentEnforcementMode.Warn, seedConsent: false);

        // Setup: Disabled mode (skip validation entirely)
        _disabledProvider = BuildProvider(ConsentEnforcementMode.Disabled, seedConsent: false);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — With Valid Consent (Happy Path)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Pipeline: Block mode, consent present")]
    public async Task<int> Pipeline_BlockMode_ConsentPresent()
    {
        using var scope = _consentProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new ConsentBenchCommand("bench-user-0");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Missing Consent (Blocked)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: Block mode, consent missing")]
    public async Task<int> Pipeline_BlockMode_ConsentMissing()
    {
        using var scope = _noConsentProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new ConsentBenchCommand("no-consent-user");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Warn Mode (Proceeds with Warning)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: Warn mode, consent missing")]
    public async Task<int> Pipeline_WarnMode_ConsentMissing()
    {
        using var scope = _warnModeProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new ConsentBenchCommand("no-consent-user");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Disabled Mode (Skips Validation)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: Disabled mode (no validation)")]
    public async Task<int> Pipeline_DisabledMode()
    {
        using var scope = _disabledProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new ConsentBenchCommand("any-user");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — No Attribute (Skip Branch)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: No [RequireConsent] attribute")]
    public async Task<int> Pipeline_NoConsentAttribute()
    {
        using var scope = _consentProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new NoConsentBenchCommand("any-user");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Infrastructure
    // ────────────────────────────────────────────────────────────

    private static ServiceProvider BuildProvider(ConsentEnforcementMode mode, bool seedConsent)
    {
        var services = new ServiceCollection();
        services.AddEncina(config =>
            config.RegisterServicesFromAssemblyContaining<ConsentBenchCommand>());
        services.AddEncinaConsent(options =>
        {
            options.EnforcementMode = mode;
            options.AutoRegisterFromAttributes = false;
        });

        services.AddScoped<IRequestHandler<ConsentBenchCommand, int>, ConsentBenchHandler>();
        services.AddScoped<IRequestHandler<NoConsentBenchCommand, int>, NoConsentBenchHandler>();

        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });

        if (seedConsent)
        {
            var store = provider.GetRequiredService<IConsentStore>();
            for (var i = 0; i < 100; i++)
            {
                store.RecordConsentAsync(new ConsentRecord
                {
                    Id = Guid.NewGuid(),
                    SubjectId = $"bench-user-{i}",
                    Purpose = ConsentPurposes.Marketing,
                    Status = ConsentStatus.Active,
                    ConsentVersionId = "v1.0",
                    GivenAtUtc = DateTimeOffset.UtcNow,
                    Source = "benchmark",
                    Metadata = new Dictionary<string, object?>()
                }).AsTask().GetAwaiter().GetResult();
            }
        }

        return provider;
    }

    [RequireConsent(ConsentPurposes.Marketing, SubjectIdProperty = nameof(UserId))]
    private sealed record ConsentBenchCommand(string UserId) : IRequest<int>;

    private sealed class ConsentBenchHandler : IRequestHandler<ConsentBenchCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(ConsentBenchCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(42));
    }

    // Command WITHOUT [RequireConsent] — measures the "no attribute" branch
    private sealed record NoConsentBenchCommand(string UserId) : IRequest<int>;

    private sealed class NoConsentBenchHandler : IRequestHandler<NoConsentBenchCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(NoConsentBenchCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(99));
    }
}
