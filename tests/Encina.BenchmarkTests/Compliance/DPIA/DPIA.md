# BenchmarkTests - Compliance DPIA (Core Module)

## Status: Not Implemented

## Justification

The core DPIA module delegates all persistence to store implementations and uses straightforward control flow for assessment handling. Micro-benchmarking this module would measure dictionary lookups and simple pattern matching, providing no actionable optimization insights.

### 1. No Hot-Path Operations

DPIA assessment is not a hot-path operation in typical applications:

- Assessment creation happens during system configuration or when new request types are registered
- `DPIARequiredPipelineBehavior` performs a single attribute check + dictionary lookup per request — sub-microsecond
- Risk criteria evaluation runs once per assessment, not in tight loops
- Expiration monitoring runs at configurable intervals (default: every hour), not continuously
- At these frequencies, the overhead of the C# logic layer is immeasurable compared to any real I/O

### 2. Simple Dictionary-Based Operations

The in-memory store operations are all `ConcurrentDictionary` operations:

- `SaveAssessmentAsync` -> dictionary insert/update, O(1) amortized
- `GetAssessmentAsync` -> LINQ FirstOrDefault over `Values`, O(n) but assessments are few (typically <100)
- `GetAssessmentByIdAsync` -> `TryGetValue()`, O(1) amortized
- `GetExpiredAssessmentsAsync` -> LINQ filter over `Values`, O(n) but called once per monitoring cycle
- `GetAllAssessmentsAsync` -> `Values.ToList()`, O(n)
- `DeleteAssessmentAsync` -> `TryRemove()`, O(1) amortized

These operations are so simple that benchmarking them would produce sub-microsecond results dominated by measurement noise.

### 3. Assessment Engine Logic Is Sequential and Bounded

The `DefaultDPIAAssessmentEngine` follows a linear workflow per assessment:

```
for each AssessAsync invocation:
    iterate over registered IRiskCriterion instances (6 built-in)
    evaluate each criterion against DPIAContext (simple pattern matching)
    aggregate risk levels (max comparison)
    build DPIAResult record
    store assessment
    record audit entry
```

The workflow is sequential per invocation with a fixed upper bound of 6 criteria evaluations. The performance is entirely determined by the store implementation (in production: database query latency).

### 4. Risk Criteria Evaluation Is Trivial

Each of the 6 built-in risk criteria performs simple property checks on `DPIAContext`:

- `SystematicProfilingCriterion`: checks `ProcessingType` string contains "profiling"
- `SpecialCategoryDataCriterion`: checks `DataCategories` collection for special categories
- `SystematicMonitoringCriterion`: checks `HighRiskTriggers` for monitoring indicators
- `AutomatedDecisionMakingCriterion`: checks for automated decision-making triggers
- `LargeScaleProcessingCriterion`: checks for large-scale processing indicators
- `VulnerableSubjectsCriterion`: checks for vulnerable subject indicators

These are string comparisons and collection checks with no computation to optimize.

### 5. Pipeline Behavior Is a Single Lookup

`DPIARequiredPipelineBehavior` checks `typeof(TRequest)` for `[RequiresDPIA]` attribute (reflection cache) and then performs a single `GetAssessmentAsync` call. The attribute check is cached after the first invocation. No algorithmic complexity to benchmark.

### 6. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify correctness of all store operations, assessment engine workflow, risk criteria, pipeline behavior, template provider, and options validation
- **Guard Tests**: Verify all public methods validate parameters correctly
- **Contract Tests**: Verify all 10 provider implementations satisfy interface contracts
- **Property Tests**: Verify invariants across randomized inputs

### 7. Recommended Alternative

If performance profiling becomes necessary in the future:

1. Focus benchmarks on **database-backed store implementations** (ADO.NET, Dapper, EF Core, MongoDB) where query execution time is measurable
2. Use `BenchmarkSwitcher` with `[MemoryDiagnoser]` to measure allocations per assessment cycle
3. Benchmark the full assess + store + audit workflow with varying risk criteria counts
4. Use `--job short` for quick validation: `dotnet run -c Release -- --filter "*DPIA*" --job short`

```csharp
// Example future benchmark structure
[MemoryDiagnoser]
public class DPIAAssessmentBenchmarks
{
    [Params(10, 100, 1000)]
    public int PreSeededAssessments { get; set; }

    [Benchmark(Baseline = true)]
    public async Task<DPIAAssessment> AssessAsync_InMemory()
    {
        var result = await _engine.AssessAsync(_context);
        return result.Match(Right: r => r, Left: _ => throw new Exception());
    }

    [Benchmark]
    public async Task<Option<DPIAAssessment>> GetAssessmentByTypeName()
    {
        var result = await _store.GetAssessmentAsync("BenchmarkCommand");
        return result.Match(Right: r => r, Left: _ => throw new Exception());
    }

    [Benchmark]
    public async Task<IReadOnlyList<DPIAAssessment>> GetExpiredAssessments()
    {
        var result = await _store.GetExpiredAssessmentsAsync(DateTimeOffset.UtcNow);
        return result.Match(Right: r => r, Left: _ => throw new Exception());
    }
}
```

## Related Files

- `src/Encina.Compliance.DPIA/` - Core DPIA module source
- `tests/Encina.UnitTests/Compliance/DPIA/` - Unit tests
- `tests/Encina.GuardTests/Compliance/DPIA/` - Guard tests
- `tests/Encina.ContractTests/Compliance/DPIA/` - Contract tests
- `tests/Encina.PropertyTests/Compliance/DPIA/` - Property tests

## Date: 2026-03-11
## Issue: #409
