# BenchmarkTests - EntityFrameworkCore AuditInterceptor

## Status: Not Implemented

## Justification

The `AuditInterceptor` does not warrant dedicated benchmark tests for the following technical reasons:

### 1. Negligible Overhead Relative to Database I/O

The audit interceptor performs the following operations during `SaveChangesAsync()`:

| Operation | Time Complexity | Estimated Cost |
|-----------|-----------------|----------------|
| Interface detection (`is ICreatedAtUtc`) | O(1) | ~10-50 nanoseconds |
| Property assignment (4 properties max) | O(1) | ~20-100 nanoseconds |
| `TimeProvider.GetUtcNow()` | O(1) | ~10-30 nanoseconds |
| `AsyncLocal<T>` access | O(1) | ~50-200 nanoseconds |

**Total interceptor overhead**: ~100-400 nanoseconds per entity

**Database round-trip**: 1-100+ milliseconds (6-7 orders of magnitude larger)

Benchmarking nanosecond-level overhead when the actual operation takes milliseconds provides no actionable insights.

### 2. Not a Hot Path

The interceptor runs once per `SaveChangesAsync()` call, not in tight loops or high-frequency scenarios. The database connection establishment, query execution, and network latency dominate the total execution time.

### 3. LogChangesToStore Serialization (Future Consideration)

When `AuditInterceptorOptions.LogChangesToStore = true`, JSON serialization occurs for each modified entity. This could become a hot path in bulk operation scenarios (1000+ entities per save).

**Recommendation**: If bulk audit logging becomes a requirement, implement benchmarks specifically for:
- `JsonSerializer.Serialize()` overhead per entity
- `IAuditLogStore.SaveAsync()` throughput
- Memory allocation patterns during serialization

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: 30+ tests verify correct behavior of all interceptor code paths
- **Guard Tests**: 19 tests verify parameter validation
- **Integration Tests**: Database-level testing covers real-world performance characteristics
- **Property Tests**: Verify invariants hold across varied inputs

### 5. Recommended Alternative

If performance concerns arise in production:

1. **Profile with dotTrace/PerfView**: Identify actual bottlenecks
2. **Use Application Insights**: Track `SaveChangesAsync()` duration with/without auditing
3. **Implement conditional benchmarks**: Only if `LogChangesToStore=true` with persistent store

```csharp
// Future benchmark if needed
[Benchmark]
public async Task SaveChangesAsync_WithAuditLog_1000Entities()
{
    var entities = Enumerable.Range(0, 1000)
        .Select(i => new AuditedEntity { Name = $"Entity-{i}" })
        .ToList();

    context.AddRange(entities);
    await context.SaveChangesAsync(); // Measures total including audit
}
```

## Related Files

- `src/Encina.EntityFrameworkCore/Auditing/AuditInterceptor.cs` - Main interceptor implementation
- `src/Encina.EntityFrameworkCore/Auditing/AuditInterceptorOptions.cs` - Configuration options
- `tests/Encina.UnitTests/EntityFrameworkCore/Auditing/AuditInterceptorTests.cs` - Unit test coverage

## Date: 2026-01-31
## Issue: #286
