# BenchmarkTests - Compliance PrivacyByDesign (Core Module)

## Status: Not Implemented

## Justification

The core PrivacyByDesign module delegates all persistence to store implementations and uses straightforward reflection-based analysis for data minimization, purpose limitation, and default privacy checks. The reflection results are cached per type in static `ConcurrentDictionary` fields, meaning the hot path is a dictionary lookup followed by property value reads. Micro-benchmarking this module would measure dictionary lookups and `PropertyInfo.GetValue()` calls, providing no actionable optimization insights.

### 1. Reflection Is Cached — One-Time Cost

The primary cost in the Privacy by Design pipeline is reflection-based property inspection. However, `DefaultDataMinimizationAnalyzer.MetadataCache` and `DataMinimizationPipelineBehavior.AttributeCache` cache all reflection metadata per request type in static `ConcurrentDictionary` instances. After the first invocation for each type, subsequent calls are O(1) dictionary lookups with zero reflection overhead. Benchmarking would only measure the initial warm-up (irrelevant in production) or the steady-state (sub-microsecond dictionary hits).

### 2. No Algorithmic Complexity

The validation logic is linear in the number of properties:
- Data minimization: iterate properties once, check `NotStrictlyNecessaryAttribute`
- Purpose limitation: iterate properties once, check against allowed fields set
- Default privacy: iterate properties once, compare actual vs declared defaults

All operations are O(n) where n is the number of properties on the request type (typically 5-20). There are no sorting, searching, or graph traversal algorithms that would benefit from benchmarking.

### 3. Pipeline Behavior Overhead Is Negligible

`DataMinimizationPipelineBehavior` performs two static dictionary lookups (attribute cache and ProcessesPersonalData cache), resolves optional services from DI, and calls the validator. The enforcement mode check is a single enum comparison. The total overhead is sub-microsecond and dominated by the underlying handler's execution time.

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: Cover all analyzer operations, validator logic, pipeline behavior (all enforcement modes), purpose registry, health check, and options validation
- **Guard Tests**: Verify all public methods reject null/invalid parameters
- **Contract Tests**: Verify interface contracts across implementations
- **Property Tests**: Verify score calculation invariants
- **Integration Tests**: End-to-end pipeline tests with real DI and concurrent access

### 5. Recommended Alternative

If benchmarking becomes necessary in the future (e.g., to optimize reflection caching for NativeAOT scenarios):

1. Benchmark `FieldMetadataCache.Build()` cold-start time for types with varying property counts (5, 20, 50, 100 properties)
2. Compare `PropertyInfo.GetValue()` vs source-generated accessors
3. Benchmark `ConcurrentDictionary.GetOrAdd()` contention under high parallelism
4. Profile memory allocation per validation call (target: zero-allocation steady-state)

```csharp
// Example future benchmark structure
[MemoryDiagnoser]
public class PrivacyByDesignBenchmarks
{
    private DefaultDataMinimizationAnalyzer _analyzer = null!;
    private SampleRequest _request = null!;

    [GlobalSetup]
    public void Setup()
    {
        _analyzer = new DefaultDataMinimizationAnalyzer(
            TimeProvider.System,
            NullLogger<DefaultDataMinimizationAnalyzer>.Instance);
        _request = new SampleRequest { ProductId = "P001", Quantity = 5 };

        // Warm up cache
        _analyzer.AnalyzeAsync(_request).GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true)]
    public MinimizationReport AnalyzeAsync_CachedType()
    {
        var result = _analyzer.AnalyzeAsync(_request).GetAwaiter().GetResult();
        return result.Match(r => r, _ => null!);
    }
}
```

## Related Files

- `src/Encina.Compliance.PrivacyByDesign/` - Core PrivacyByDesign module source
- `tests/Encina.UnitTests/Compliance/PrivacyByDesign/` - Unit tests
- `tests/Encina.GuardTests/Compliance/PrivacyByDesign/` - Guard tests
- `tests/Encina.ContractTests/Compliance/PrivacyByDesign/` - Contract tests
- `tests/Encina.PropertyTests/Compliance/PrivacyByDesign/` - Property tests
- `tests/Encina.IntegrationTests/Compliance/PrivacyByDesign/` - Integration tests

## Date: 2026-03-14
## Issue: #411
