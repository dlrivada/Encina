# Load Tests - ADO CursorPaginationHelper

## Status: Not Implemented

## Justification

Load tests are not implemented for `CursorPaginationHelper` because cursor pagination is NOT a concurrency feature.

### 1. No Concurrent State Management

`CursorPaginationHelper` is a stateless helper class:

- Each call is independent - no shared state between requests
- No connection pooling management - connection is injected
- No transaction coordination - single query per call
- No locking mechanisms - pure SQL query execution

### 2. Load Testing Would Test Database, Not CursorPaginationHelper

Any load test for cursor pagination would primarily measure:

- Database query performance (index scans, I/O)
- Connection pool behavior (outside CursorPaginationHelper scope)
- Network latency
- ADO.NET driver performance

None of these are controlled or managed by `CursorPaginationHelper`.

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: 52 tests covering parameter validation across all 4 providers
- **Guard Tests**: 32 tests verifying null parameter handling
- **Property Tests**: 20+ tests verifying invariants
- **Contract Tests**: 10 tests ensuring API consistency across providers
- **Integration Tests**: 20+ tests verifying real database pagination

### 4. Recommended Alternative

If database pagination performance needs validation:

1. Use **BenchmarkDotNet** to measure query execution time
2. Use **Database profiling** (SQL Server Profiler, EXPLAIN ANALYZE)
3. Use **Application Performance Monitoring** (APM) in production

## Related Files

- `src/Encina.ADO.SqlServer/Pagination/CursorPaginationHelper.cs`
- `src/Encina.ADO.PostgreSQL/Pagination/CursorPaginationHelper.cs`
- `src/Encina.ADO.MySQL/Pagination/CursorPaginationHelper.cs`
- `src/Encina.ADO.Sqlite/Pagination/CursorPaginationHelper.cs`
- `tests/Encina.UnitTests/ADO/*/Pagination/CursorPaginationHelperTests.cs`
- `tests/Encina.IntegrationTests/ADO/*/Pagination/CursorPaginationHelperIntegrationTests.cs`

## Date: 2026-02-08
## Issue: #336
