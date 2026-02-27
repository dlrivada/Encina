using BenchmarkDotNet.Attributes;

using Encina.Compliance.DataSubjectRights;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.Benchmarks.Compliance.DataSubjectRights;

/// <summary>
/// Benchmarks for DSR request store operations using InMemoryDSRRequestStore.
/// Measures throughput and allocations for core DSR operations:
/// - GetByIdAsync (hot path in restriction checks)
/// - CreateAsync (write path)
/// - GetBySubjectIdAsync (subject lookup)
/// - UpdateStatusAsync (state transitions)
/// - HasActiveRestrictionAsync (pipeline check - critical hot path)
/// - GetPendingRequestsAsync / GetOverdueRequestsAsync (SLA monitoring)
/// </summary>
/// <remarks>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*DSRRequestStoreBenchmarks*"
///
/// # Quick validation:
/// dotnet run -c Release -- --filter "*DSRRequestStoreBenchmarks*" --job short
///
/// # List available benchmarks:
/// dotnet run -c Release -- --list flat --filter "*DSR*"
/// </code>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class DSRRequestStoreBenchmarks
{
    private InMemoryDSRRequestStore _store = null!;
    private string _existingRequestId = null!;
    private string _existingSubjectId = null!;
    private string _restrictedSubjectId = null!;

    [Params(10, 100, 1000)]
    public int PreSeededRecords { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _store = new InMemoryDSRRequestStore(
            TimeProvider.System,
            NullLogger<InMemoryDSRRequestStore>.Instance);

        // Seed records with various statuses and rights
        for (var i = 0; i < PreSeededRecords; i++)
        {
            var id = $"req-{i}";
            var subjectId = $"subject-{i % 50}";
            var right = (DataSubjectRight)(i % 8);
            var request = DSRRequest.Create(id, subjectId, right, DateTimeOffset.UtcNow.AddDays(-i % 35));
            _store.CreateAsync(request).AsTask().GetAwaiter().GetResult();

            // Complete ~40% of requests
            if (i % 5 is 0 or 1)
            {
                _store.UpdateStatusAsync(id, DSRRequestStatus.Completed, null)
                    .AsTask().GetAwaiter().GetResult();
            }
        }

        // Keep a reference to a known existing request
        _existingRequestId = "req-0";
        _existingSubjectId = "subject-0";

        // Create a subject with an active restriction
        _restrictedSubjectId = "subject-restricted";
        _store.CreateAsync(DSRRequest.Create(
            "restriction-req", _restrictedSubjectId, DataSubjectRight.Restriction,
            DateTimeOffset.UtcNow))
            .AsTask().GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _store.Clear();
    }

    // ────────────────────────────────────────────────────────────
    //  HasActiveRestrictionAsync — Critical Hot Path (Pipeline)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "HasActiveRestriction (restricted subject)")]
    public async Task<bool> HasActiveRestriction_RestrictedSubject()
    {
        var result = await _store.HasActiveRestrictionAsync(_restrictedSubjectId);
        return result.Match(Left: _ => false, Right: v => v);
    }

    [Benchmark(Description = "HasActiveRestriction (unrestricted subject)")]
    public async Task<bool> HasActiveRestriction_UnrestrictedSubject()
    {
        var result = await _store.HasActiveRestrictionAsync(_existingSubjectId);
        return result.Match(Left: _ => false, Right: v => v);
    }

    [Benchmark(Description = "HasActiveRestriction (nonexistent subject)")]
    public async Task<bool> HasActiveRestriction_NonexistentSubject()
    {
        var result = await _store.HasActiveRestrictionAsync("nonexistent-subject");
        return result.Match(Left: _ => false, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  GetByIdAsync — Single Read
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "GetById (existing)")]
    public async Task<Option<DSRRequest>> GetById_ExistingRecord()
    {
        var result = await _store.GetByIdAsync(_existingRequestId);
        return result.Match(Left: _ => Option<DSRRequest>.None, Right: v => v);
    }

    [Benchmark(Description = "GetById (missing)")]
    public async Task<Option<DSRRequest>> GetById_MissingRecord()
    {
        var result = await _store.GetByIdAsync("nonexistent-req");
        return result.Match(Left: _ => Option<DSRRequest>.None, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  GetBySubjectIdAsync — Subject Lookup
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "GetBySubjectId (existing)")]
    public async Task<IReadOnlyList<DSRRequest>> GetBySubjectId_ExistingSubject()
    {
        var result = await _store.GetBySubjectIdAsync(_existingSubjectId);
        return result.Match(
            Left: _ => (IReadOnlyList<DSRRequest>)Array.Empty<DSRRequest>(),
            Right: v => v);
    }

    [Benchmark(Description = "GetBySubjectId (missing)")]
    public async Task<IReadOnlyList<DSRRequest>> GetBySubjectId_MissingSubject()
    {
        var result = await _store.GetBySubjectIdAsync("nonexistent-subject");
        return result.Match(
            Left: _ => (IReadOnlyList<DSRRequest>)Array.Empty<DSRRequest>(),
            Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  CreateAsync — Write Path
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "CreateRequest (single)")]
    public async Task<bool> CreateRequest_Single()
    {
        var request = DSRRequest.Create(
            Guid.NewGuid().ToString(),
            "bench-write-subject",
            DataSubjectRight.Access,
            DateTimeOffset.UtcNow);
        var result = await _store.CreateAsync(request);
        return result.IsRight;
    }

    // ────────────────────────────────────────────────────────────
    //  UpdateStatusAsync — State Transition
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "UpdateStatus (state transition)")]
    public async Task<bool> UpdateStatus_StateTransition()
    {
        // Create then update to ensure it exists each iteration
        var id = Guid.NewGuid().ToString();
        await _store.CreateAsync(DSRRequest.Create(id, "bench-update", DataSubjectRight.Access, DateTimeOffset.UtcNow));
        var result = await _store.UpdateStatusAsync(id, DSRRequestStatus.InProgress, null);
        return result.IsRight;
    }

    // ────────────────────────────────────────────────────────────
    //  GetPendingRequestsAsync — SLA Monitoring
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "GetPendingRequests")]
    public async Task<IReadOnlyList<DSRRequest>> GetPendingRequests()
    {
        var result = await _store.GetPendingRequestsAsync();
        return result.Match(
            Left: _ => (IReadOnlyList<DSRRequest>)Array.Empty<DSRRequest>(),
            Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  GetOverdueRequestsAsync — SLA Violation Detection
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "GetOverdueRequests")]
    public async Task<IReadOnlyList<DSRRequest>> GetOverdueRequests()
    {
        var result = await _store.GetOverdueRequestsAsync();
        return result.Match(
            Left: _ => (IReadOnlyList<DSRRequest>)Array.Empty<DSRRequest>(),
            Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  GetAllAsync — Full Scan
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "GetAllRequests")]
    public async Task<IReadOnlyList<DSRRequest>> GetAllRequests()
    {
        var result = await _store.GetAllAsync();
        return result.Match(
            Left: _ => (IReadOnlyList<DSRRequest>)Array.Empty<DSRRequest>(),
            Right: v => v);
    }
}
