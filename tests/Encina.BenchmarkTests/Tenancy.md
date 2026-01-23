# Benchmark Tests - Multi-Tenancy

## Status: Not Implemented

## Justification

BenchmarkDotNet benchmarks for multi-tenancy across all providers are not implemented for the following reasons:

### 1. Tenancy Overhead is SQL-Level, Not Code-Level
The multi-tenancy implementation adds:
- A single string interpolation for the WHERE clause (ADO/Dapper)
- A filter expression composition (MongoDB)
- A query filter application (EF Core)

These operations are:
- O(1) string concatenation or expression building
- Negligible compared to database query execution time
- Not meaningful to benchmark in isolation

### 2. What Would Be Benchmarked
Benchmarking tenancy would measure:
- ❌ Database query performance (already covered by provider benchmarks)
- ❌ String concatenation performance (not our code)
- ❌ Expression tree building (EF Core/MongoDB internals)

### 3. Existing Benchmarks Cover the Meaningful Parts
The existing benchmarks in `Encina.BenchmarkTests/` already measure:
- Repository operations (`Encina.Benchmarks/Inbox/`, `Outbox/`)
- EF Core operations (`Encina.EntityFrameworkCore.Benchmarks/`)
- Dapper operations (`Encina.Benchmarks/Inbox/InboxDapperBenchmarks.cs`)

Adding a `TenantId` filter doesn't change these benchmark results meaningfully.

### 4. Recommended Alternative
If benchmark data is needed:
1. Add tenant-aware variants to existing benchmarks
2. Compare with/without tenancy filtering
3. Expected result: < 1% overhead (within measurement noise)

### 5. Meaningful Future Benchmarks
If benchmarks are added, focus on:
- Entity mapping builder performance
- Options validation overhead
- ITenantProvider resolution speed

## Providers Not Benchmarked
- ADO: Sqlite, SqlServer, PostgreSQL, MySQL, Oracle
- Dapper: Sqlite, SqlServer, PostgreSQL, MySQL, Oracle
- EntityFrameworkCore
- MongoDB

## Related Files
- `tests/Encina.BenchmarkTests/Encina.Benchmarks/` - Core benchmarks
- `src/Encina.*/Tenancy/` - Tenancy implementations

## Date: 2026-01-23
## Issue: #282
