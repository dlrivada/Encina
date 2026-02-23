using BenchmarkDotNet.Attributes;
using Encina.Compliance.Consent;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.Benchmarks.Compliance.Consent;

/// <summary>
/// Benchmarks for consent store operations using InMemoryConsentStore.
/// Measures throughput and allocations for core consent operations:
/// - HasValidConsentAsync (hot path in pipeline)
/// - RecordConsentAsync (write path)
/// - GetConsentAsync / GetAllConsentsAsync (read paths)
/// - BulkRecordConsentAsync (batch path)
/// - WithdrawConsentAsync (state transition path)
/// </summary>
/// <remarks>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*ConsentStoreBenchmarks*"
///
/// # Quick validation:
/// dotnet run -c Release -- --filter "*ConsentStoreBenchmarks*" --job short
///
/// # List available benchmarks:
/// dotnet run -c Release -- --list flat --filter "*Consent*"
/// </code>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class ConsentStoreBenchmarks
{
    private InMemoryConsentStore _store = null!;
    private ConsentRecord _singleConsent = null!;
    private List<ConsentRecord> _bulkConsents = null!;

    [Params(10, 100, 1000)]
    public int PreSeededRecords { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _store = new InMemoryConsentStore(
            TimeProvider.System,
            NullLogger<InMemoryConsentStore>.Instance);

        // Seed records for read benchmarks
        for (var i = 0; i < PreSeededRecords; i++)
        {
            _store.RecordConsentAsync(CreateConsentRecord($"subject-{i}", ConsentPurposes.Marketing))
                .AsTask().GetAwaiter().GetResult();
        }

        // Pre-build a consent record for write benchmarks
        _singleConsent = CreateConsentRecord("bench-write-subject", ConsentPurposes.Analytics);

        // Pre-build bulk consent list
        _bulkConsents = Enumerable.Range(0, 100)
            .Select(i => CreateConsentRecord($"bulk-bench-subject-{i}", ConsentPurposes.Personalization))
            .ToList();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _store.Clear();
    }

    // ────────────────────────────────────────────────────────────
    //  HasValidConsentAsync — Hot Path (Pipeline Check)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "HasValidConsent (existing)")]
    public async Task<bool> HasValidConsent_ExistingRecord()
    {
        var result = await _store.HasValidConsentAsync("subject-0", ConsentPurposes.Marketing);
        return result.Match(Left: _ => false, Right: v => v);
    }

    [Benchmark(Description = "HasValidConsent (missing)")]
    public async Task<bool> HasValidConsent_MissingRecord()
    {
        var result = await _store.HasValidConsentAsync("nonexistent-subject", ConsentPurposes.Marketing);
        return result.Match(Left: _ => false, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  RecordConsentAsync — Write Path
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "RecordConsent (single)")]
    public async Task<Unit> RecordConsent_Single()
    {
        var consent = _singleConsent with { Id = Guid.NewGuid() };
        var result = await _store.RecordConsentAsync(consent);
        return result.Match(Left: _ => unit, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  GetConsentAsync — Single Read
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "GetConsent (existing)")]
    public async Task<Option<ConsentRecord>> GetConsent_ExistingRecord()
    {
        var result = await _store.GetConsentAsync("subject-0", ConsentPurposes.Marketing);
        return result.Match(Left: _ => Option<ConsentRecord>.None, Right: v => v);
    }

    [Benchmark(Description = "GetConsent (missing)")]
    public async Task<Option<ConsentRecord>> GetConsent_MissingRecord()
    {
        var result = await _store.GetConsentAsync("nonexistent-subject", ConsentPurposes.Marketing);
        return result.Match(Left: _ => Option<ConsentRecord>.None, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  GetAllConsentsAsync — Multi-Record Read
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "GetAllConsents")]
    public async Task<IReadOnlyList<ConsentRecord>> GetAllConsents()
    {
        var result = await _store.GetAllConsentsAsync("subject-0");
        return result.Match(
            Left: _ => (IReadOnlyList<ConsentRecord>)System.Array.Empty<ConsentRecord>(),
            Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  BulkRecordConsentAsync — Batch Write
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "BulkRecordConsent (100 records)")]
    public async Task<BulkOperationResult> BulkRecordConsent_100()
    {
        var consents = _bulkConsents.Select(c => c with { Id = Guid.NewGuid() }).ToList();
        var result = await _store.BulkRecordConsentAsync(consents);
        return result.Match(
            Left: _ => BulkOperationResult.Success(0),
            Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  WithdrawConsentAsync — State Transition
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "WithdrawConsent")]
    public async Task<Unit> WithdrawConsent()
    {
        // Record then withdraw to ensure it exists each iteration
        var subjectId = $"withdraw-bench-{Guid.NewGuid():N}";
        await _store.RecordConsentAsync(CreateConsentRecord(subjectId, ConsentPurposes.Marketing));

        var result = await _store.WithdrawConsentAsync(subjectId, ConsentPurposes.Marketing);
        return result.Match(Left: _ => unit, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────

    private static ConsentRecord CreateConsentRecord(string subjectId, string purpose) =>
        new()
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            Purpose = purpose,
            Status = ConsentStatus.Active,
            ConsentVersionId = "v1.0",
            GivenAtUtc = DateTimeOffset.UtcNow,
            Source = "benchmark",
            Metadata = new Dictionary<string, object?>()
        };
}
