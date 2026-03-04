# BenchmarkTests - Compliance BreachNotification (Core Module)

## Status: Not Implemented

## Justification

The core BreachNotification module delegates all persistence to store implementations and uses straightforward control flow for breach handling logic. Micro-benchmarking this module would measure dictionary lookups and simple enum comparisons, providing no actionable optimization insights.

### 1. No Hot-Path Operations

Breach notification is not a hot-path operation in typical applications:

- Breach detection runs on security events, not in tight loops -- breaches are rare, high-severity occurrences
- Detection rules perform a single `SecurityEventType` enum comparison per evaluation
- Deadline monitoring cycles run at configurable intervals (default: every 15 minutes), not continuously
- At these frequencies, the overhead of the C# logic layer is immeasurable compared to any real I/O

### 2. Simple Dictionary-Based Operations

The in-memory store operations are all `ConcurrentDictionary` operations:

- `RecordBreachAsync` -> `TryAdd()`, O(1) amortized
- `GetBreachAsync` -> `TryGetValue()`, O(1) amortized
- `UpdateBreachAsync` -> `ContainsKey` + key update, O(1) amortized
- `GetBreachesByStatusAsync` -> LINQ filter over `Values`, O(n) but rarely called
- `GetOverdueBreachesAsync` -> LINQ filter over `Values`, O(n) but only called once per monitoring cycle
- `RecordAsync` (audit) -> dictionary insert, O(1) amortized

These operations are so simple that benchmarking them would produce sub-microsecond results dominated by measurement noise.

### 3. Handler Logic Is Sequential and Bounded

The `DefaultBreachHandler` follows a linear workflow per breach:

```
for each handler method invocation:
    validate input parameters
    retrieve breach record (O(1) dictionary lookup)
    record audit entry (O(1) dictionary insert)
    execute core operation (delegate to notifier / update store)
    update breach status (O(1) dictionary update)
    record completion audit (O(1) dictionary insert)
    publish notification (if IEncina available)
```

The workflow is sequential per invocation with no algorithmic complexity to optimize. The performance is entirely determined by the store implementation (in production: database query latency) and the notifier implementation (in production: HTTP/email delivery).

### 4. Detection Rule Evaluation Is Trivial

Each of the four built-in detection rules (`UnauthorizedAccessRule`, `MassDataExfiltrationRule`, `PrivilegeEscalationRule`, `AnomalousQueryPatternRule`) performs a single `SecurityEventType` enum comparison in `EvaluateAsync`. If the event type matches, a `PotentialBreach` record is created with pre-defined severity and recommended actions. There is no aggregation, windowing, or pattern-matching computation to optimize.

### 5. Mapper Operations Are Trivial

The three mappers (`BreachRecordMapper`, `PhasedReportMapper`, `BreachAuditEntryMapper`) perform direct property-to-property copies with at most one enum cast and one `Enum.IsDefined` check. These are allocations + assignments with no computation to optimize.

### 6. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify correctness of all store operations, breach handler workflow, detection rules, notification delivery, mapper roundtrips, and options validation
- **Guard Tests**: Verify all public methods validate parameters correctly across all five primary classes
- **Contract Tests**: Verify both `IBreachRecordStore` and `IBreachAuditStore` implementations satisfy interface contracts

### 7. Recommended Alternative

If performance profiling becomes necessary in the future:

1. Focus benchmarks on **database-backed store implementations** (ADO.NET, Dapper, EF Core, MongoDB) where query execution time is measurable
2. Use `BenchmarkSwitcher` with `[MemoryDiagnoser]` to measure allocations per breach handling cycle
3. Benchmark the full detection + handling workflow with varying breach volumes to establish scaling characteristics
4. Use `--job short` for quick validation: `dotnet run -c Release -- --filter "*BreachNotification*" --job short`

```csharp
// Example future benchmark structure
[MemoryDiagnoser]
public class BreachNotificationBenchmarks
{
    [Params(10, 100, 1000)]
    public int ActiveBreachCount { get; set; }

    [Benchmark(Baseline = true)]
    public async Task<BreachRecord> HandleDetectedBreach_InMemory()
    {
        var result = await _handler.HandleDetectedBreachAsync(_potentialBreach);
        return result.Match(Right: r => r, Left: _ => throw new Exception());
    }

    [Benchmark]
    public async Task<BreachRecord> HandleDetectedBreach_SqlServer()
    {
        var result = await _sqlServerHandler.HandleDetectedBreachAsync(_potentialBreach);
        return result.Match(Right: r => r, Left: _ => throw new Exception());
    }

    [Benchmark]
    public async Task<IReadOnlyList<DeadlineStatus>> GetApproachingDeadlines()
    {
        var result = await _recordStore.GetApproachingDeadlineAsync(hoursRemaining: 24);
        return result.Match(Right: r => r, Left: _ => throw new Exception());
    }
}
```

## Related Files

- `src/Encina.Compliance.BreachNotification/` - Core breach notification module source
- `tests/Encina.UnitTests/Compliance/BreachNotification/` - Unit tests
- `tests/Encina.GuardTests/Compliance/BreachNotification/` - Guard tests
- `tests/Encina.ContractTests/Compliance/BreachNotification/` - Contract tests

## Date: 2026-03-03
## Issue: #408
