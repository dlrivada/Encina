# Benchmark Tests - Cursor vs Offset Pagination Comparison

## Status: Not Implemented

## Justification

Performance benchmarks comparing cursor-based vs offset-based pagination are not implemented because the performance difference is **well-documented database behavior**, not application-level code optimization.

### 1. Database-Level Performance Is Well-Known

The performance characteristics are dictated by SQL execution, not C# code:

| Metric | Offset Pagination | Cursor Pagination |
|--------|-------------------|-------------------|
| **Page 1** | O(page_size) | O(page_size) |
| **Page 100** | O(100 × page_size) | O(page_size) |
| **Page 10,000** | O(10,000 × page_size) | O(page_size) |
| **Index Usage** | Scan + Skip | Seek |

This is database engine behavior, not something our C# code can optimize.

### 2. Benchmarks Would Measure Database, Not Encina

```csharp
// What a benchmark would actually measure:
[Benchmark]
public async Task OffsetPagination_Page10000()
{
    // 99.9% of time: SQL Server skipping 10,000 rows
    // 0.1% of time: Encina code building query
    await repository.GetPageAsync(page: 10000, pageSize: 20);
}

[Benchmark]
public async Task CursorPagination_Page10000()
{
    // 99.9% of time: SQL Server seeking to cursor position
    // 0.1% of time: Encina code building query
    await repository.GetAfterAsync(cursor, pageSize: 20);
}
```

The benchmark would validate SQL Server behavior, not Encina code quality.

### 3. Integration Tests Already Validate This

Our integration tests verify:

- ✅ Cursor pagination returns correct results
- ✅ Offset pagination returns correct results
- ✅ Both work with all 4 database providers
- ✅ Cursor decoding/encoding is correct

### 4. Academic References

The O(1) vs O(n) performance difference is well-documented:

- [Use The Index, Luke - Pagination Done Right](https://use-the-index-luke.com/sql/partial-results/fetch-next-page)
- [SQL Server Execution Plans - Offset vs Keyset](https://sqlperformance.com/2015/01/t-sql-queries/pagination-with-offset-fetch)
- PostgreSQL, MySQL, and SQLite documentation on cursor-based pagination

### 5. When Benchmarks Would Be Valuable

Benchmarks would add value if we were optimizing:

1. **Cursor encoding/decoding** - but this is already O(1) JSON + Base64
2. **SQL query building** - but this is O(1) string concatenation
3. **Parameter mapping** - but this is O(page_size) regardless of approach

None of these are worth benchmarking given current implementation simplicity.

### 6. Recommended Alternative

For production performance validation, use actual database metrics:

```sql
-- SQL Server: Compare execution plans
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

-- Offset (observe: high logical reads at deep pages)
SELECT * FROM Orders ORDER BY Id OFFSET 100000 ROWS FETCH NEXT 20 ROWS ONLY;

-- Cursor (observe: constant logical reads)
SELECT * FROM Orders WHERE Id > @cursor ORDER BY Id FETCH NEXT 20 ROWS ONLY;
```

Or use OpenTelemetry instrumentation:

```csharp
services.AddEncinaOpenTelemetry(config =>
{
    config.EnableDatabaseMetrics = true;
    config.TracePaginationQueries = true;
});
```

## Related Files

- `src/Encina.DomainModeling/Pagination/CursorPaginatedResult.cs`
- `src/Encina.DomainModeling/Pagination/OffsetPaginatedResult.cs`
- `src/Encina.ADO.*/Pagination/CursorPaginationHelper.cs`
- `src/Encina.Dapper.*/Pagination/CursorPaginationHelper.cs`
- `tests/Encina.IntegrationTests/Pagination/` (actual DB validation)

## Date: 2026-02-08
## Issue: #293
