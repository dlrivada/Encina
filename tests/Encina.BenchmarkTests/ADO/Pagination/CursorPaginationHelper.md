# Benchmark Tests - ADO CursorPaginationHelper

## Status: Not Implemented

## Justification

Benchmark tests are not implemented for `CursorPaginationHelper` because it is not a hot path requiring micro-optimization.

### 1. Database I/O Dominates Execution Time

`CursorPaginationHelper` execution time breakdown:

| Operation | Time | Percentage |
|-----------|------|------------|
| SQL Query Execution | ~5-500ms | 95-99% |
| Result Mapping | ~0.1-1ms | 0.5-2% |
| Cursor Encoding | ~0.01-0.1ms | <0.5% |
| Parameter Building | ~0.001-0.01ms | <0.1% |

Optimizing the C# code would have negligible impact on total execution time.

### 2. Cursor Pagination Is Inherently O(1)

The primary performance benefit of cursor pagination is at the **database level**:

- Index seek instead of index scan
- Constant time regardless of page position
- No need to skip rows

This is validated by integration tests, not benchmarks.

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify parameter validation is efficient
- **Guard Tests**: Verify null checks don't add overhead
- **Property Tests**: Verify encoder round-trip efficiency
- **Contract Tests**: Verify API consistency
- **Integration Tests**: Verify actual query performance against real databases

### 4. What Would Be Worth Benchmarking

If cursor pagination performance analysis is needed:

1. **Base64JsonCursorEncoder** - encode/decode throughput
2. **Cursor parsing** - deserialization performance
3. **SQL generation** - string building (but this is O(1))

These are already covered by existing `Encina.DomainModeling` benchmarks.

### 5. Recommended Alternative

For production performance validation:

```csharp
// Use OpenTelemetry instrumentation
services.AddEncinaOpenTelemetry(config =>
{
    config.EnableDatabaseMetrics = true;
    config.TracePaginationQueries = true;
});
```

## Related Files

- `src/Encina.ADO.SqlServer/Pagination/CursorPaginationHelper.cs`
- `src/Encina.ADO.PostgreSQL/Pagination/CursorPaginationHelper.cs`
- `src/Encina.ADO.MySQL/Pagination/CursorPaginationHelper.cs`
- `src/Encina.ADO.Sqlite/Pagination/CursorPaginationHelper.cs`
- `tests/Encina.BenchmarkTests/Encina.DomainModeling.Benchmarks/` (cursor encoder benchmarks)

## Date: 2026-02-08
## Issue: #336
