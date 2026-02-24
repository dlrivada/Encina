using BenchmarkDotNet.Attributes;
using Encina.Compliance.GDPR;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.Benchmarks.Compliance.GDPR;

/// <summary>
/// Benchmarks for lawful basis store operations using InMemoryLawfulBasisRegistry and InMemoryLIAStore.
/// Measures throughput and allocations for core lawful basis operations:
/// - GetByRequestTypeAsync (hot path in pipeline)
/// - RegisterAsync (write path)
/// - AutoRegisterFromAssemblies (startup path)
/// - LIA store operations (StoreAsync, GetByReferenceAsync, GetPendingReviewAsync)
/// </summary>
/// <remarks>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*LawfulBasisStoreBenchmarks*"
///
/// # Quick validation:
/// dotnet run -c Release -- --filter "*LawfulBasisStoreBenchmarks*" --job short
///
/// # List available benchmarks:
/// dotnet run -c Release -- --list flat --filter "*LawfulBasis*"
/// </code>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class LawfulBasisStoreBenchmarks
{
    private InMemoryLawfulBasisRegistry _registry = null!;
    private InMemoryLIAStore _liaStore = null!;
    private LawfulBasisRegistration _singleRegistration = null!;

    [Params(10, 100, 1000)]
    public int PreSeededRecords { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _registry = new InMemoryLawfulBasisRegistry();
        _liaStore = new InMemoryLIAStore();

        // Seed registry entries for read benchmarks
        for (var i = 0; i < PreSeededRecords; i++)
        {
            var regType = CreateSyntheticType($"Bench.Command{i}");
            _registry.RegisterAsync(new LawfulBasisRegistration
            {
                RequestType = regType,
                Basis = (LawfulBasis)(i % 6),
                Purpose = $"Benchmark purpose {i}",
                RegisteredAtUtc = DateTimeOffset.UtcNow
            }).AsTask().GetAwaiter().GetResult();
        }

        // Seed LIA records for read benchmarks
        for (var i = 0; i < PreSeededRecords; i++)
        {
            _liaStore.StoreAsync(CreateLIARecord($"LIA-BENCH-{i}",
                i % 3 == 0 ? LIAOutcome.Approved :
                i % 3 == 1 ? LIAOutcome.Rejected : LIAOutcome.RequiresReview))
                .AsTask().GetAwaiter().GetResult();
        }

        // Pre-build a registration for write benchmarks
        _singleRegistration = new LawfulBasisRegistration
        {
            RequestType = typeof(LawfulBasisStoreBenchmarks), // dummy type
            Basis = LawfulBasis.Contract,
            Purpose = "Benchmark write test",
            RegisteredAtUtc = DateTimeOffset.UtcNow
        };
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _registry = null!;
        _liaStore = null!;
    }

    // ────────────────────────────────────────────────────────────
    //  Registry — GetByRequestTypeAsync (Hot Path in Pipeline)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Registry: GetByRequestType (existing)")]
    public async Task<Option<LawfulBasisRegistration>> Registry_GetByRequestType_Existing()
    {
        var regType = CreateSyntheticType("Bench.Command0");
        var result = await _registry.GetByRequestTypeAsync(regType);
        return result.Match(Left: _ => Option<LawfulBasisRegistration>.None, Right: v => v);
    }

    [Benchmark(Description = "Registry: GetByRequestType (missing)")]
    public async Task<Option<LawfulBasisRegistration>> Registry_GetByRequestType_Missing()
    {
        var result = await _registry.GetByRequestTypeAsync(typeof(string));
        return result.Match(Left: _ => Option<LawfulBasisRegistration>.None, Right: v => v);
    }

    [Benchmark(Description = "Registry: GetByRequestTypeName (existing)")]
    public async Task<Option<LawfulBasisRegistration>> Registry_GetByRequestTypeName_Existing()
    {
        var result = await _registry.GetByRequestTypeNameAsync("Bench.Command0");
        return result.Match(Left: _ => Option<LawfulBasisRegistration>.None, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Registry — RegisterAsync (Write Path)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Registry: Register (upsert)")]
    public async Task<LanguageExt.Unit> Registry_Register_Upsert()
    {
        var registration = _singleRegistration with
        {
            RequestType = CreateSyntheticType($"BenchWrite.{Guid.NewGuid():N}")
        };
        var result = await _registry.RegisterAsync(registration);
        return result.Match(Left: _ => LanguageExt.Prelude.unit, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Registry — AutoRegisterFromAssemblies (Startup Path)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Registry: AutoRegisterFromAssemblies")]
    public void Registry_AutoRegisterFromAssemblies()
    {
        var freshRegistry = new InMemoryLawfulBasisRegistry();
        freshRegistry.AutoRegisterFromAssemblies([typeof(LawfulBasisStoreBenchmarks).Assembly]);
    }

    // ────────────────────────────────────────────────────────────
    //  LIA Store — GetByReferenceAsync (Hot Path for LegitimateInterests)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "LIA Store: GetByReference (existing)")]
    public async Task<Option<LIARecord>> LIAStore_GetByReference_Existing()
    {
        var result = await _liaStore.GetByReferenceAsync("LIA-BENCH-0");
        return result.Match(Left: _ => Option<LIARecord>.None, Right: v => v);
    }

    [Benchmark(Description = "LIA Store: GetByReference (missing)")]
    public async Task<Option<LIARecord>> LIAStore_GetByReference_Missing()
    {
        var result = await _liaStore.GetByReferenceAsync("LIA-NONEXISTENT");
        return result.Match(Left: _ => Option<LIARecord>.None, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  LIA Store — StoreAsync (Write Path)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "LIA Store: Store (new record)")]
    public async Task<LanguageExt.Unit> LIAStore_Store_New()
    {
        var record = CreateLIARecord($"LIA-WRITE-{Guid.NewGuid():N}", LIAOutcome.Approved);
        var result = await _liaStore.StoreAsync(record);
        return result.Match(Left: _ => LanguageExt.Prelude.unit, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  LIA Store — GetPendingReviewAsync (Filtered Query)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "LIA Store: GetPendingReview")]
    public async Task<IReadOnlyList<LIARecord>> LIAStore_GetPendingReview()
    {
        var result = await _liaStore.GetPendingReviewAsync();
        return result.Match(
            Left: _ => (IReadOnlyList<LIARecord>)Array.Empty<LIARecord>(),
            Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  FromAttribute — Reflection Path
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "FromAttribute (decorated type)")]
    public LawfulBasisRegistration? FromAttribute_DecoratedType()
    {
        return LawfulBasisRegistration.FromAttribute(typeof(BenchDecoratedCommand));
    }

    [Benchmark(Description = "FromAttribute (undecorated type)")]
    public LawfulBasisRegistration? FromAttribute_UndecoratedType()
    {
        return LawfulBasisRegistration.FromAttribute(typeof(string));
    }

    // ────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────

    private static Type CreateSyntheticType(string name)
    {
        // Uses string-keyed dictionary lookup in the registry via GetByRequestTypeNameAsync
        // For GetByRequestTypeAsync, we need actual types — use the name convention
        // to generate a reproducible type key
        return Type.GetType(name) ?? typeof(object);
    }

    private static LIARecord CreateLIARecord(string reference, LIAOutcome outcome) =>
        new()
        {
            Id = reference,
            Name = $"LIA {reference}",
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
            Outcome = outcome,
            Conclusion = outcome == LIAOutcome.Approved ? "Approved" : "Pending",
            AssessedAtUtc = DateTimeOffset.UtcNow,
            AssessedBy = "benchmark-runner"
        };

    [LawfulBasis(LawfulBasis.Contract)]
    private sealed record BenchDecoratedCommand;
}
