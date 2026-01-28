# Benchmark Tests - Specification Pattern

## Status: Implemented

## Overview

Specification SQL builder benchmarks have been implemented for both Dapper and ADO.NET providers in Issue #568.

## Implemented Benchmarks

| Project | File | Benchmarks |
|---------|------|------------|
| Encina.Dapper.Benchmarks | `Repository/SpecificationSqlBuilderBenchmarks.cs` | 12 |
| Encina.ADO.Benchmarks | `Repository/SpecificationSqlBuilderBenchmarks.cs` | 12 |

### Operations Benchmarked

| Operation | Description |
|-----------|-------------|
| `BuildWhereClause_SingleEquality` | Simple `WHERE Id = @p0` (baseline) |
| `BuildWhereClause_MultipleAnd` | `WHERE A AND B AND C` |
| `BuildWhereClause_OrCombination` | `WHERE A OR B` |
| `BuildWhereClause_StringContains` | `WHERE Name LIKE '%value%'` |
| `BuildWhereClause_StringStartsWith` | `WHERE Name LIKE 'value%'` |
| `BuildWhereClause_StringEndsWith` | `WHERE Name LIKE '%value'` |
| `BuildOrderByClause_SingleColumn` | Single column ordering |
| `BuildOrderByClause_MultipleColumns` | Multi-column ordering |
| `BuildPaginationClause` | LIMIT/OFFSET generation |
| `BuildSelectStatement_Complete` | Full SELECT with all clauses |
| `SpecificationReuse_PreBuilt` | Pre-instantiated specification |
| `SpecificationReuse_DynamicCreation` | New specification each time |

### Specification Classes Used

| Specification | Expression |
|---------------|------------|
| `SimpleEqualitySpec` | `e => e.Id == id` |
| `MultipleAndConditionsSpec` | `e => e.Category == category && e.IsActive == isActive && e.Quantity > minQuantity` |
| `OrCombinationSpec` | `e => e.Id == id1 \|\| e.Id == id2` |
| `StringContainsSpec` | `e => e.Name.Contains(value)` |
| `StringStartsWithSpec` | `e => e.Name.StartsWith(value)` |
| `StringEndsWithSpec` | `e => e.Name.EndsWith(value)` |
| `SingleColumnOrderSpec` | Single ORDER BY |
| `MultiColumnOrderSpec` | Multiple ORDER BY with mixed directions |
| `PaginatedQuerySpec` | WHERE + ORDER BY + LIMIT/OFFSET |

### Running Benchmarks

```bash
# Dapper SQL builder benchmarks
cd tests/Encina.BenchmarkTests/Encina.Dapper.Benchmarks
dotnet run -c Release -- --filter "*SpecificationSqlBuilderBenchmarks*"

# ADO.NET SQL builder benchmarks
cd tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks
dotnet run -c Release -- --filter "*SpecificationSqlBuilderBenchmarks*"
```

## Performance Targets

| Operation | Target |
|-----------|--------|
| Simple WHERE | <10 μs |
| Complex WHERE | <20 μs |
| String operations | <15 μs |
| Full SELECT | <50 μs |

## Related Files

- `tests/Encina.BenchmarkTests/Encina.Dapper.Benchmarks/Repository/SpecificationSqlBuilderBenchmarks.cs`
- `tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks/Repository/SpecificationSqlBuilderBenchmarks.cs`
- `tests/Encina.BenchmarkTests/Encina.Dapper.Benchmarks/README.md`
- `tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks/README.md`
- `docs/benchmarks/provider-sql-dialect-comparison.md` - Result templates

## Date: 2026-01-28

## Issue: #568

## Previous Status

Previously marked as "Not Implemented" (Issue #280) because specification overhead was considered negligible. However, benchmarks were implemented in Issue #568 to provide comprehensive SQL generation performance metrics and enable pattern optimization analysis.
