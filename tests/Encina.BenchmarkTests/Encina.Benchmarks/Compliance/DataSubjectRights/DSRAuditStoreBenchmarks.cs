using BenchmarkDotNet.Attributes;

using Encina.Compliance.DataSubjectRights;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.Benchmarks.Compliance.DataSubjectRights;

/// <summary>
/// Benchmarks for DSR audit store operations using InMemoryDSRAuditStore.
/// Measures throughput and allocations for audit trail operations:
/// - RecordAsync (write path — every DSR action generates an audit entry)
/// - GetAuditTrailAsync (read path — compliance reporting)
/// </summary>
/// <remarks>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*DSRAuditStoreBenchmarks*"
/// </code>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class DSRAuditStoreBenchmarks
{
    private InMemoryDSRAuditStore _store = null!;
    private string _requestIdWithManyEntries = null!;
    private DSRAuditEntry _templateEntry = null!;

    [Params(10, 100, 1000)]
    public int PreSeededEntries { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _store = new InMemoryDSRAuditStore(
            NullLogger<InMemoryDSRAuditStore>.Instance);

        _requestIdWithManyEntries = "request-with-trail";

        // Seed audit entries across multiple requests
        for (var i = 0; i < PreSeededEntries; i++)
        {
            var requestId = i < PreSeededEntries / 2
                ? _requestIdWithManyEntries
                : $"request-{i}";

            _store.RecordAsync(new DSRAuditEntry
            {
                Id = Guid.NewGuid().ToString(),
                DSRRequestId = requestId,
                Action = $"Action-{i % 5}",
                OccurredAtUtc = DateTimeOffset.UtcNow.AddMinutes(-i),
                PerformedByUserId = "system",
                Detail = $"Detail for entry {i}"
            }).AsTask().GetAwaiter().GetResult();
        }

        _templateEntry = new DSRAuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            DSRRequestId = "bench-write-request",
            Action = "BenchmarkAction",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            PerformedByUserId = "benchmark"
        };
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _store.Clear();
    }

    // ────────────────────────────────────────────────────────────
    //  RecordAsync — Write Path
    // ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "RecordAuditEntry (single)")]
    public async Task<bool> RecordAuditEntry_Single()
    {
        var entry = _templateEntry with { Id = Guid.NewGuid().ToString() };
        var result = await _store.RecordAsync(entry);
        return result.IsRight;
    }

    // ────────────────────────────────────────────────────────────
    //  GetAuditTrailAsync — Read Path (Compliance Reporting)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "GetAuditTrail (existing, many entries)")]
    public async Task<IReadOnlyList<DSRAuditEntry>> GetAuditTrail_ExistingRequest()
    {
        var result = await _store.GetAuditTrailAsync(_requestIdWithManyEntries);
        return result.Match(
            Left: _ => (IReadOnlyList<DSRAuditEntry>)Array.Empty<DSRAuditEntry>(),
            Right: v => v);
    }

    [Benchmark(Description = "GetAuditTrail (nonexistent request)")]
    public async Task<IReadOnlyList<DSRAuditEntry>> GetAuditTrail_NonexistentRequest()
    {
        var result = await _store.GetAuditTrailAsync("nonexistent-request");
        return result.Match(
            Left: _ => (IReadOnlyList<DSRAuditEntry>)Array.Empty<DSRAuditEntry>(),
            Right: v => v);
    }
}
