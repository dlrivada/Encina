# BenchmarkTests - Compliance ProcessorAgreements (Core Module)

## Status: Not Implemented

## Justification

The core ProcessorAgreements module delegates all persistence to store implementations and uses straightforward control flow for DPA validation and processor management. Micro-benchmarking this module would measure dictionary lookups and simple property checks, providing no actionable optimization insights.

### 1. No Hot-Path Operations

Processor agreement validation is not a hot-path operation in typical applications:

- Processor registration happens during system configuration or when new data processors are onboarded
- `ProcessorValidationPipelineBehavior` performs a single attribute check + dictionary lookup per request — sub-microsecond
- DPA validation runs once per relevant request, not in tight loops
- Expiration monitoring runs at configurable intervals (not continuously)
- At these frequencies, the overhead of the C# logic layer is immeasurable compared to any real I/O

### 2. Simple Dictionary-Based Operations

The in-memory store operations are all `ConcurrentDictionary` operations:

- `InMemoryProcessorRegistry`: `ConcurrentDictionary<string, Processor>` — register/get/remove are O(1) amortized
- `InMemoryDPAStore`: `ConcurrentDictionary<string, DataProcessingAgreement>` keyed by processorId + by id — save/get are O(1) amortized, expiration queries are O(n) but DPAs are few (typically <100)
- `InMemoryProcessorAuditStore`: `ConcurrentDictionary<string, List<ProcessorAgreementAuditEntry>>` — append is O(1), retrieval by processor is O(1) lookup + O(n) copy

These operations are so simple that benchmarking them would produce sub-microsecond results dominated by measurement noise.

### 3. Validator Logic Is Sequential and Bounded

The `DefaultDPAValidator` follows a linear workflow per validation:

```
for each ValidateAsync invocation:
    look up processor in IProcessorRegistry (dictionary get)
    query IDPAStore for active DPA (dictionary get)
    check DPA expiration date (DateTime comparison)
    verify required terms are present (collection check)
    return validation result
```

The workflow is sequential per invocation with a fixed number of steps. The performance is entirely determined by the store implementation (in production: database query latency).

### 4. Pipeline Behavior Is a Two-Level Lookup

`ProcessorValidationPipelineBehavior` performs:

1. Attribute check: `typeof(TRequest)` for `[RequiresProcessorAgreement]` — cached in static `ConcurrentDictionary` after first invocation
2. Store query: single `ValidateDPAAsync` call to verify agreement status

Both operations are dictionary lookups with no algorithmic complexity to benchmark.

### 5. Expiration Handler Is a Simple Query + Publish

`CheckDPAExpirationHandler` queries the store for DPAs expiring within a configurable window, then publishes notifications for each. The query is a LINQ filter over dictionary values (O(n) with small n), and publishing is delegated to the messaging infrastructure. No computation to optimize.

### 6. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify correctness of all store operations, DPA validation workflow, pipeline behavior, expiration handler, health check, and options validation
- **Guard Tests**: Verify all public methods validate parameters correctly
- **Contract Tests**: Verify all provider implementations satisfy interface contracts
- **Property Tests**: Verify invariants across randomized inputs

### 7. Recommended Alternative

If performance profiling becomes necessary in the future:

1. Focus benchmarks on **database-backed store implementations** (ADO.NET, Dapper, EF Core, MongoDB) where query execution time is measurable
2. Use `BenchmarkSwitcher` with `[MemoryDiagnoser]` to measure allocations per validation cycle
3. Benchmark the full validate + audit workflow with varying processor/DPA counts
4. Use `--job short` for quick validation: `dotnet run -c Release -- --filter "*ProcessorAgreements*" --job short`

```csharp
// Example future benchmark structure
[MemoryDiagnoser]
public class ProcessorAgreementBenchmarks
{
    [Params(10, 100, 1000)]
    public int PreSeededProcessors { get; set; }

    [Benchmark(Baseline = true)]
    public async Task<bool> ValidateDPA_InMemory()
    {
        var result = await _validator.ValidateAsync(_processorId);
        return result.Match(Right: r => r, Left: _ => throw new Exception());
    }

    [Benchmark]
    public async Task<DataProcessingAgreement> GetDPAByProcessorId()
    {
        var result = await _store.GetByProcessorIdAsync("benchmark-processor");
        return result.Match(Right: r => r, Left: _ => throw new Exception());
    }

    [Benchmark]
    public async Task<IReadOnlyList<DataProcessingAgreement>> GetExpiringDPAs()
    {
        var result = await _store.GetExpiringAsync(DateTimeOffset.UtcNow.AddDays(30));
        return result.Match(Right: r => r, Left: _ => throw new Exception());
    }
}
```

## Related Files

- `src/Encina.Compliance.ProcessorAgreements/` - Core ProcessorAgreements module source
- `tests/Encina.UnitTests/Compliance/ProcessorAgreements/` - Unit tests
- `tests/Encina.GuardTests/Compliance/ProcessorAgreements/` - Guard tests
- `tests/Encina.ContractTests/Compliance/ProcessorAgreements/` - Contract tests
- `tests/Encina.PropertyTests/Compliance/ProcessorAgreements/` - Property tests

## Date: 2026-03-13
## Issue: #410
