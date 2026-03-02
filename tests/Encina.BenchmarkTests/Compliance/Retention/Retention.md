# BenchmarkTests - Compliance Retention (Core Module)

## Status: Not Implemented

## Justification

The core Retention module delegates all persistence to store implementations and uses straightforward control flow for enforcement logic. Micro-benchmarking this module would measure dictionary lookups and simple comparisons, providing no actionable optimization insights.

### 1. No Hot-Path Operations

Retention enforcement is not a hot-path operation in typical applications:

- Enforcement cycles run at configurable intervals (default: every 60 minutes), not in tight loops
- Policy evaluation is a single dictionary lookup per data category
- Legal hold checks are single dictionary lookups per entity ID
- At these frequencies, the overhead of the C# logic layer is immeasurable compared to any real I/O

### 2. Simple Dictionary-Based Operations

The in-memory store operations are all `ConcurrentDictionary` operations:

- `CreateAsync` → `TryAdd()`, O(1) amortized
- `GetByIdAsync` → `TryGetValue()`, O(1) amortized
- `GetExpiredRecordsAsync` → LINQ filter over `Values`, O(n) but rarely called (once per enforcement cycle)
- `UpdateStatusAsync` → `TryGetValue` + key update, O(1) amortized

These operations are so simple that benchmarking them would produce sub-microsecond results dominated by measurement noise.

### 3. Enforcement Logic Is Sequential and Bounded

The `DefaultRetentionEnforcer.EnforceRetentionAsync()` loop processes expired records one by one:

```
for each expired record:
    check legal hold (O(1) dictionary lookup)
    update status (O(1) dictionary update)
    record audit entry (O(1) dictionary insert)
```

The loop is linear in the number of expired records, with no algorithmic complexity to optimize. The performance is entirely determined by the store implementation (in production: database query latency).

### 4. Mapper Operations Are Trivial

The four mappers (`RetentionRecordMapper`, `RetentionPolicyMapper`, `LegalHoldMapper`, `RetentionAuditEntryMapper`) perform direct property-to-property copies with at most one enum cast and one `Enum.IsDefined` check. These are allocations + assignments with no computation to optimize.

### 5. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify correctness of all store operations, enforcement logic, legal hold management, policy evaluation, and mapper roundtrips (230+ tests)
- **Guard Tests**: Verify all public methods validate parameters correctly (92 tests)
- **Property Tests**: Verify retention invariants across randomized inputs with FsCheck (58 tests)
- **Contract Tests**: Verify all store implementations satisfy interface contracts (51 tests)
- **Integration Tests**: Test full DI pipeline, lifecycle flows, and concurrent access (14 tests)

### 6. Recommended Alternative

If performance profiling becomes necessary in the future:

1. Focus benchmarks on **database-backed store implementations** (ADO.NET, Dapper, EF Core) where query execution time is measurable
2. Use `BenchmarkSwitcher` with `[MemoryDiagnoser]` to measure allocations per enforcement cycle
3. Benchmark the full enforcement cycle with varying record volumes to establish scaling characteristics
4. Use `--job short` for quick validation: `dotnet run -c Release -- --filter "*Retention*" --job short`

```csharp
// Example future benchmark structure
[MemoryDiagnoser]
public class RetentionEnforcementBenchmarks
{
    [Params(100, 1000, 10000)]
    public int ExpiredRecordCount { get; set; }

    [Benchmark(Baseline = true)]
    public async Task<DeletionResult> EnforceRetention_InMemory()
    {
        var result = await _enforcer.EnforceRetentionAsync();
        return result.Match(Right: r => r, Left: _ => throw new Exception());
    }

    [Benchmark]
    public async Task<DeletionResult> EnforceRetention_SqlServer()
    {
        var result = await _sqlServerEnforcer.EnforceRetentionAsync();
        return result.Match(Right: r => r, Left: _ => throw new Exception());
    }
}
```

## Related Files

- `src/Encina.Compliance.Retention/` - Core retention module source
- `tests/Encina.UnitTests/Compliance/Retention/` - Unit tests (230+ tests)

## Date: 2026-03-01
## Issue: #406
