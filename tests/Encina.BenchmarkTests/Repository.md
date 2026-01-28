# Benchmark Tests - Repository Pattern

## Status: Implemented

## Overview

Repository benchmarks have been implemented for both Dapper and ADO.NET providers in Issue #568.

## Implemented Benchmarks

| Project | File | Benchmarks |
|---------|------|------------|
| Encina.Dapper.Benchmarks | `Repository/RepositoryBenchmarks.cs` | 20 |
| Encina.ADO.Benchmarks | `Repository/RepositoryBenchmarks.cs` | 20 |

### Operations Benchmarked

| Operation | Description |
|-----------|-------------|
| `Repository_GetByIdAsync` | Single entity retrieval by ID |
| `Repository_ListAsync` | Full table retrieval |
| `Repository_ListWithSpecification` | Filtered query via specification |
| `Repository_AddAsync` | Single entity insert |
| `Repository_UpdateAsync` | Single entity update |
| `Repository_DeleteAsync` | Single entity delete |
| `Repository_AddRangeAsync` | Batch insert (10, 100, 1000) |
| `Repository_UpdateRangeAsync` | Batch update (10, 100, 1000) |
| `Repository_DeleteRangeAsync` | Batch delete via specification |
| `Repository_CountAsync` | Aggregate count |
| `Repository_AnyAsync` | Existence check |
| `Repository_FirstOrDefaultAsync` | Single entity with specification |
| `RawDapper_*` / `RawAdo_*` | Direct comparison without repository |

### Running Benchmarks

```bash
# Dapper repository benchmarks
cd tests/Encina.BenchmarkTests/Encina.Dapper.Benchmarks
dotnet run -c Release -- --filter "*RepositoryBenchmarks*"

# ADO.NET repository benchmarks
cd tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks
dotnet run -c Release -- --filter "*RepositoryBenchmarks*"
```

## Related Files

- `tests/Encina.BenchmarkTests/Encina.Dapper.Benchmarks/Repository/RepositoryBenchmarks.cs`
- `tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks/Repository/RepositoryBenchmarks.cs`
- `tests/Encina.BenchmarkTests/Encina.Dapper.Benchmarks/README.md`
- `tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks/README.md`

## Date: 2026-01-28

## Issue: #568

## Previous Status

Previously marked as "Not Implemented" (Issue #279) because repository overhead was considered negligible. However, benchmarks were implemented in Issue #568 to provide comprehensive coverage and enable direct comparison between repository abstraction vs raw data access.
