# BenchmarkTests - Compliance BreachNotification

## Status: Not Implemented

## Justification

Breach notification is not a hot-path operation. Breaches are rare, high-severity events — not tight-loop operations. Micro-benchmarking this module would provide no actionable optimization insights.

### 1. No Hot-Path Operations

- Breach detection runs on security events, not in tight loops — breaches are rare, high-severity occurrences
- The service layer performs aggregate create/load/save through Marten, where latency is dominated by PostgreSQL I/O
- Deadline monitoring cycles run at configurable intervals (default: every 15 minutes), not continuously
- At these frequencies, the overhead of the C# logic layer is immeasurable compared to database I/O

### 2. Event-Sourced Aggregate Operations Are I/O-Bound

After the Marten event sourcing migration (ADR-019), all breach lifecycle operations go through:
- `IAggregateRepository<BreachAggregate>.CreateAsync` — writes to PostgreSQL event stream
- `IAggregateRepository<BreachAggregate>.LoadAsync` — reads and replays events from PostgreSQL
- `IAggregateRepository<BreachAggregate>.SaveAsync` — appends events to PostgreSQL

The bottleneck is always PostgreSQL, not the C# aggregate logic. Benchmarking the aggregate in isolation would measure property assignments and list operations.

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: Cover all aggregate lifecycle transitions, service operations, detection rules, projection logic
- **Guard Tests**: Verify all public methods validate parameters correctly
- **Property Tests**: Verify invariants on breach records, audit entries, phased reports
- **Integration Tests**: Verify real Marten/PostgreSQL persistence, projection correctness, cache invalidation
- **Load Tests**: Validate throughput and latency under concurrent access (50 workers, 10K ops/worker)

### 4. Recommended Alternative

If performance profiling becomes necessary in the future:

1. Focus benchmarks on **Marten event stream performance** (event replay, projection throughput)
2. Use `BenchmarkSwitcher` with `[MemoryDiagnoser]` to measure allocations per service operation
3. Benchmark aggregate event replay with varying event counts (10, 100, 1000 events)

```csharp
// Example future benchmark structure
[MemoryDiagnoser]
public class BreachNotificationBenchmarks
{
    [Params(10, 100, 1000)]
    public int EventCount { get; set; }

    [Benchmark(Baseline = true)]
    public BreachAggregate ReplayBreachEvents()
    {
        var aggregate = new BreachAggregate();
        foreach (var evt in _events.Take(EventCount))
            aggregate.Apply(evt); // Would need internal access
        return aggregate;
    }
}
```

## Related Files

- `src/Encina.Compliance.BreachNotification/` - Core breach notification module source
- `tests/Encina.UnitTests/Compliance/BreachNotification/` - Unit tests
- `tests/Encina.IntegrationTests/Compliance/BreachNotification/` - Integration tests (Marten/PostgreSQL)
- `tests/Encina.LoadTests/Compliance/BreachNotification/` - Load tests

## Date: 2026-03-18
## Issue: #786
