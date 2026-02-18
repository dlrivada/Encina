# BenchmarkTests - Compliance GDPR

## Status: Not Implemented

## Justification

The `Encina.Compliance.GDPR` module does not warrant dedicated benchmark tests for the following technical reasons:

### 1. Negligible Overhead Relative to Request Processing

The GDPR compliance pipeline behavior performs the following operations per request:

| Operation | Time Complexity | Estimated Cost |
|-----------|-----------------|----------------|
| `ConcurrentDictionary.GetOrAdd()` (attribute cache) | O(1) | ~50-100 nanoseconds |
| `ConcurrentDictionary.TryGetValue()` (registry lookup) | O(1) | ~50-200 nanoseconds |
| `ComplianceResult` allocation | O(1) | ~20-50 nanoseconds |
| `Activity.SetTag()` (tracing) | O(1) | ~100-300 nanoseconds |
| Logger calls | O(1) | ~50-200 nanoseconds |

**Total pipeline behavior overhead**: ~300-850 nanoseconds per request

**Typical request handler**: 1-100+ milliseconds (5-6 orders of magnitude larger)

Benchmarking sub-microsecond overhead when the actual operation takes milliseconds provides no actionable insights.

### 2. Not a Hot Path

The compliance behavior runs once per request, not in tight loops. The dictionary lookups are already optimized via `ConcurrentDictionary` with cached attribute reflection results.

### 3. RoPA Export is Low-Frequency

JSON and CSV export operations are administrative tasks performed occasionally (quarterly/annual compliance audits). The export processes a small number of activities (typically 10-50), making serialization overhead negligible.

| Scenario | Activities | Estimated Time |
|----------|-----------|----------------|
| Small app | 5-10 | < 1ms |
| Medium app | 20-50 | < 5ms |
| Large enterprise | 100-200 | < 20ms |

These are not hot paths that benefit from micro-optimization.

### 4. Attribute Reflection is Cached

The `ProcessingActivityAttribute` lookup uses a `ConcurrentDictionary<Type, ...>` cache. After the first request per type, subsequent lookups are pure dictionary reads with no reflection overhead.

### 5. Adequate Coverage from Other Test Types

- **Unit Tests**: 60+ tests verify correct behavior of all pipeline paths, exporters, and validators
- **Guard Tests**: Parameter validation for all public methods
- **Property Tests**: FsCheck invariants for registry operations, compliance results, exporter consistency
- **Contract Tests**: Interface contract verification for all public interfaces

### 6. Recommended Alternative

If performance concerns arise in production:

1. **Profile with dotTrace/PerfView**: Identify if GDPR pipeline is actually a bottleneck
2. **Use OpenTelemetry**: The pipeline already emits tracing spans - monitor `gdpr.compliance.check` duration
3. **Implement targeted benchmarks**: Only if custom validator or database-backed registry shows latency

```csharp
// Future benchmark if needed (custom validator scenario)
[Benchmark]
public async Task CompliancePipeline_WithDatabaseRegistry_PerRequest()
{
    var request = new SampleCommand();
    var context = RequestContext.CreateForTest();
    await _behavior.Handle(request, context, () => _next, CancellationToken.None);
}
```

## Related Files

- `src/Encina.Compliance.GDPR/GDPRCompliancePipelineBehavior.cs` - Pipeline behavior
- `src/Encina.Compliance.GDPR/InMemoryProcessingActivityRegistry.cs` - Registry with caching
- `src/Encina.Compliance.GDPR/Export/` - JSON and CSV exporters
- `tests/Encina.UnitTests/Compliance/GDPR/` - Unit test coverage

## Date: 2026-02-17
## Issue: #402
