using BenchmarkDotNet.Attributes;
using Encina.Compliance.GDPR;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Benchmarks.Compliance.GDPR;

/// <summary>
/// Benchmarks for the full lawful basis pipeline behavior end-to-end.
/// Measures the overhead of lawful basis validation in the Encina pipeline
/// comparing different enforcement modes, basis types, and validation paths.
/// </summary>
/// <remarks>
/// These benchmarks measure the realistic pipeline overhead, including:
/// - Static attribute cache lookup
/// - Registry lookup for declared basis
/// - Consent status verification (for Consent basis)
/// - LIA validation (for LegitimateInterests basis)
/// - OpenTelemetry instrumentation overhead
///
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*LawfulBasisPipelineBenchmarks*"
/// </code>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class LawfulBasisPipelineBenchmarks
{
    private ServiceProvider _blockModeProvider = null!;
    private ServiceProvider _warnModeProvider = null!;
    private ServiceProvider _disabledProvider = null!;
    private ServiceProvider _consentBasisProvider = null!;
    private ServiceProvider _liaBasisProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Block mode with Contract basis registered (happy path)
        _blockModeProvider = BuildProvider(LawfulBasisEnforcementMode.Block, seedRegistrations: true, seedConsent: false, seedLIA: false);

        // Warn mode without registrations (logs but proceeds)
        _warnModeProvider = BuildProvider(LawfulBasisEnforcementMode.Warn, seedRegistrations: false, seedConsent: false, seedLIA: false);

        // Disabled mode (skip validation entirely)
        _disabledProvider = BuildProvider(LawfulBasisEnforcementMode.Disabled, seedRegistrations: false, seedConsent: false, seedLIA: false);

        // Consent basis — consent present (requires consent check)
        _consentBasisProvider = BuildProvider(LawfulBasisEnforcementMode.Block, seedRegistrations: true, seedConsent: true, seedLIA: false);

        // LegitimateInterests basis with LIA approved
        _liaBasisProvider = BuildProvider(LawfulBasisEnforcementMode.Block, seedRegistrations: true, seedConsent: false, seedLIA: true);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _blockModeProvider?.Dispose();
        _warnModeProvider?.Dispose();
        _disabledProvider?.Dispose();
        _consentBasisProvider?.Dispose();
        _liaBasisProvider?.Dispose();
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Contract Basis (Simple Happy Path)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Pipeline: Block mode, Contract basis (happy path)")]
    public async Task<int> Pipeline_BlockMode_ContractBasis()
    {
        using var scope = _blockModeProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new ContractBasisBenchCommand("bench-user-0");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Missing Basis (Blocked)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: Block mode, missing basis")]
    public async Task<int> Pipeline_BlockMode_MissingBasis()
    {
        using var scope = _blockModeProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new NoBasisBenchCommand("no-basis-user");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Warn Mode (Proceeds with Warning)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: Warn mode, missing basis")]
    public async Task<int> Pipeline_WarnMode_MissingBasis()
    {
        using var scope = _warnModeProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new ContractBasisBenchCommand("warn-user");
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
        var command = new ContractBasisBenchCommand("any-user");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — No Attribute (Skip Branch)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: No [LawfulBasis] attribute")]
    public async Task<int> Pipeline_NoLawfulBasisAttribute()
    {
        using var scope = _blockModeProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new NoBasisBenchCommand("any-user");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Consent Basis (Requires Consent Check)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: Consent basis, consent present")]
    public async Task<int> Pipeline_ConsentBasis_Present()
    {
        using var scope = _consentBasisProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new ConsentBasisBenchCommand("bench-user-0");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — LegitimateInterests Basis (Requires LIA Check)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pipeline: LegitimateInterests basis, LIA approved")]
    public async Task<int> Pipeline_LegitimateInterestsBasis_LIAApproved()
    {
        using var scope = _liaBasisProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new LegitimateInterestBenchCommand("bench-user-0");
        var result = await encina.Send(command);
        return result.Match(Left: _ => -1, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Infrastructure
    // ────────────────────────────────────────────────────────────

    private static ServiceProvider BuildProvider(
        LawfulBasisEnforcementMode mode,
        bool seedRegistrations,
        bool seedConsent,
        bool seedLIA)
    {
        var services = new ServiceCollection();
        services.AddLogging(); // Required: pipeline behaviors depend on ILogger<T>

        // Don't scan the assembly — it picks up processors from other benchmarks
        // that have dependencies we haven't registered (e.g., EncinaBenchmarks.CallRecorder)
        services.AddEncina();

        services.AddEncinaLawfulBasis(options =>
        {
            options.EnforcementMode = mode;
            options.ValidateLIAForLegitimateInterests = seedLIA;
            options.AutoRegisterFromAttributes = false; // Benchmarks seed registrations manually
        });

        // Register handlers manually (avoid assembly scanning)
        services.AddScoped<IRequestHandler<ContractBasisBenchCommand, int>, ContractBasisBenchHandler>();
        services.AddScoped<IRequestHandler<NoBasisBenchCommand, int>, NoBasisBenchHandler>();
        services.AddScoped<IRequestHandler<ConsentBasisBenchCommand, int>, ConsentBasisBenchHandler>();
        services.AddScoped<IRequestHandler<LegitimateInterestBenchCommand, int>, LegitimateInterestBenchHandler>();

        // Register a fake consent provider if needed
        if (seedConsent)
        {
            services.AddSingleton<IConsentStatusProvider>(new AlwaysGrantedConsentProvider());
        }

        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });

        if (seedRegistrations)
        {
            var registry = provider.GetRequiredService<ILawfulBasisRegistry>();
            registry.RegisterAsync(new LawfulBasisRegistration
            {
                RequestType = typeof(ContractBasisBenchCommand),
                Basis = LawfulBasis.Contract,
                RegisteredAtUtc = DateTimeOffset.UtcNow
            }).AsTask().GetAwaiter().GetResult();

            registry.RegisterAsync(new LawfulBasisRegistration
            {
                RequestType = typeof(ConsentBasisBenchCommand),
                Basis = LawfulBasis.Consent,
                RegisteredAtUtc = DateTimeOffset.UtcNow
            }).AsTask().GetAwaiter().GetResult();

            registry.RegisterAsync(new LawfulBasisRegistration
            {
                RequestType = typeof(LegitimateInterestBenchCommand),
                Basis = LawfulBasis.LegitimateInterests,
                LIAReference = "LIA-BENCH-001",
                RegisteredAtUtc = DateTimeOffset.UtcNow
            }).AsTask().GetAwaiter().GetResult();
        }

        if (seedLIA)
        {
            var liaStore = provider.GetRequiredService<ILIAStore>();
            liaStore.StoreAsync(new LIARecord
            {
                Id = "LIA-BENCH-001",
                Name = "Benchmark LIA",
                Purpose = "Benchmark processing",
                LegitimateInterest = "Performance measurement",
                Benefits = "Enables performance measurement of compliance pipeline",
                ConsequencesIfNotProcessed = "Unable to measure compliance overhead",
                NecessityJustification = "Required for benchmarks",
                AlternativesConsidered = ["Manual testing"],
                DataMinimisationNotes = "Only synthetic benchmark data used",
                NatureOfData = "Synthetic benchmark identifiers",
                ReasonableExpectations = "Data subjects expect performance testing",
                ImpactAssessment = "Minimal impact",
                Safeguards = ["In-memory only", "No real personal data"],
                Outcome = LIAOutcome.Approved,
                Conclusion = "Approved for benchmarks",
                AssessedAtUtc = DateTimeOffset.UtcNow,
                AssessedBy = "benchmark-runner"
            }).AsTask().GetAwaiter().GetResult();
        }

        return provider;
    }

    // ────────────────────────────────────────────────────────────
    //  Benchmark Command Types
    // ────────────────────────────────────────────────────────────

    [LawfulBasis(LawfulBasis.Contract)]
    private sealed record ContractBasisBenchCommand(string UserId) : IRequest<int>;

    private sealed class ContractBasisBenchHandler : IRequestHandler<ContractBasisBenchCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(ContractBasisBenchCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(42));
    }

    // Command WITHOUT [LawfulBasis] — measures the "no attribute" branch
    private sealed record NoBasisBenchCommand(string UserId) : IRequest<int>;

    private sealed class NoBasisBenchHandler : IRequestHandler<NoBasisBenchCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(NoBasisBenchCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(99));
    }

    [LawfulBasis(LawfulBasis.Consent)]
    private sealed record ConsentBasisBenchCommand(string UserId) : IRequest<int>;

    private sealed class ConsentBasisBenchHandler : IRequestHandler<ConsentBasisBenchCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(ConsentBasisBenchCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(77));
    }

    [LawfulBasis(LawfulBasis.LegitimateInterests, LIAReference = "LIA-BENCH-001")]
    private sealed record LegitimateInterestBenchCommand(string UserId) : IRequest<int>;

    private sealed class LegitimateInterestBenchHandler : IRequestHandler<LegitimateInterestBenchCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(LegitimateInterestBenchCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(55));
    }

    // Fake consent provider that always grants consent
    private sealed class AlwaysGrantedConsentProvider : IConsentStatusProvider
    {
        public ValueTask<Either<EncinaError, ConsentCheckResult>> CheckConsentAsync(
            string subjectId,
            IReadOnlyList<string> purposes,
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<Either<EncinaError, ConsentCheckResult>>(
                Right<EncinaError, ConsentCheckResult>(
                    new ConsentCheckResult(true, System.Array.Empty<string>())));
        }
    }
}
