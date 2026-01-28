# Provider SQL Dialect Comparison

This document provides benchmark result templates for comparing SQL generation performance across database providers using `SpecificationSqlBuilder<TEntity>`.

## Overview

The specification pattern translates C# expressions to SQL WHERE clauses. While the expression translation logic is shared across providers, SQL syntax varies (identifier quoting, pagination syntax, boolean representation).

## SQL Dialect Differences

| Feature | SQLite | SQL Server | PostgreSQL | MySQL |
|---------|--------|------------|------------|-------|
| **Identifier Quoting** | `"column"` | `[column]` | `"column"` | `` `column` `` |
| **Parameter Syntax** | `@param` | `@param` | `@param` | `@param` |
| **Boolean Literals** | `0/1` | `bit` | `true/false` | `0/1` |
| **LIMIT/OFFSET** | `LIMIT n OFFSET m` | `OFFSET m ROWS FETCH NEXT n ROWS ONLY` | `LIMIT n OFFSET m` | `LIMIT n OFFSET m` |
| **String Concat** | `\|\|` | `+` | `\|\|` | `CONCAT()` |

## Benchmark Results Template

### WHERE Clause Generation

#### Single Equality Condition

| Provider | Mean | Error | StdDev | Ratio | Allocated |
|----------|------|-------|--------|-------|-----------|
| SQLite | - ns | - ns | - ns | 1.00 | - B |
| SqlServer | - ns | - ns | - ns | - | - B |
| PostgreSQL | - ns | - ns | - ns | - | - B |
| MySQL | - ns | - ns | - ns | - | - B |

#### Multiple AND Conditions (3 criteria)

| Provider | Mean | Error | StdDev | Ratio | Allocated |
|----------|------|-------|--------|-------|-----------|
| SQLite | - ns | - ns | - ns | 1.00 | - B |
| SqlServer | - ns | - ns | - ns | - | - B |
| PostgreSQL | - ns | - ns | - ns | - | - B |
| MySQL | - ns | - ns | - ns | - | - B |

#### OR Combination

| Provider | Mean | Error | StdDev | Ratio | Allocated |
|----------|------|-------|--------|-------|-----------|
| SQLite | - ns | - ns | - ns | 1.00 | - B |
| SqlServer | - ns | - ns | - ns | - | - B |
| PostgreSQL | - ns | - ns | - ns | - | - B |
| MySQL | - ns | - ns | - ns | - | - B |

### String Operations

#### String.Contains (LIKE '%value%')

| Provider | Mean | Error | StdDev | Ratio | Allocated |
|----------|------|-------|--------|-------|-----------|
| SQLite | - ns | - ns | - ns | 1.00 | - B |
| SqlServer | - ns | - ns | - ns | - | - B |
| PostgreSQL | - ns | - ns | - ns | - | - B |
| MySQL | - ns | - ns | - ns | - | - B |

#### String.StartsWith (LIKE 'value%')

| Provider | Mean | Error | StdDev | Ratio | Allocated |
|----------|------|-------|--------|-------|-----------|
| SQLite | - ns | - ns | - ns | 1.00 | - B |
| SqlServer | - ns | - ns | - ns | - | - B |
| PostgreSQL | - ns | - ns | - ns | - | - B |
| MySQL | - ns | - ns | - ns | - | - B |

#### String.EndsWith (LIKE '%value')

| Provider | Mean | Error | StdDev | Ratio | Allocated |
|----------|------|-------|--------|-------|-----------|
| SQLite | - ns | - ns | - ns | 1.00 | - B |
| SqlServer | - ns | - ns | - ns | - | - B |
| PostgreSQL | - ns | - ns | - ns | - | - B |
| MySQL | - ns | - ns | - ns | - | - B |

### ORDER BY Clause Generation

#### Single Column

| Provider | Mean | Error | StdDev | Ratio | Allocated |
|----------|------|-------|--------|-------|-----------|
| SQLite | - ns | - ns | - ns | 1.00 | - B |
| SqlServer | - ns | - ns | - ns | - | - B |
| PostgreSQL | - ns | - ns | - ns | - | - B |
| MySQL | - ns | - ns | - ns | - | - B |

#### Multiple Columns (3 columns, mixed directions)

| Provider | Mean | Error | StdDev | Ratio | Allocated |
|----------|------|-------|--------|-------|-----------|
| SQLite | - ns | - ns | - ns | 1.00 | - B |
| SqlServer | - ns | - ns | - ns | - | - B |
| PostgreSQL | - ns | - ns | - ns | - | - B |
| MySQL | - ns | - ns | - ns | - | - B |

### Pagination Clause Generation

| Provider | Mean | Error | StdDev | Ratio | Allocated |
|----------|------|-------|--------|-------|-----------|
| SQLite | - ns | - ns | - ns | 1.00 | - B |
| SqlServer | - ns | - ns | - ns | - | - B |
| PostgreSQL | - ns | - ns | - ns | - | - B |
| MySQL | - ns | - ns | - ns | - | - B |

### Complete SELECT Statement

| Provider | Mean | Error | StdDev | Ratio | Allocated |
|----------|------|-------|--------|-------|-----------|
| SQLite | - μs | - μs | - μs | 1.00 | - B |
| SqlServer | - μs | - μs | - μs | - | - B |
| PostgreSQL | - μs | - μs | - μs | - | - B |
| MySQL | - μs | - μs | - μs | - | - B |

### Specification Reuse Pattern

#### Pre-built Specification (Cached)

| Provider | Mean | Error | StdDev | Ratio | Allocated |
|----------|------|-------|--------|-------|-----------|
| SQLite | - ns | - ns | - ns | 1.00 | - B |

#### Dynamic Specification (Created Each Time)

| Provider | Mean | Error | StdDev | Ratio | Allocated |
|----------|------|-------|--------|-------|-----------|
| SQLite | - ns | - ns | - ns | 1.00 | - B |

## Expected Results

### Performance Targets

| Operation | Target |
|-----------|--------|
| Simple WHERE | <10 μs |
| Complex WHERE (3+ conditions) | <20 μs |
| String operations | <15 μs |
| ORDER BY (single) | <5 μs |
| ORDER BY (multiple) | <10 μs |
| Pagination | <5 μs |
| Full SELECT | <50 μs |

### Provider Variance

All providers should perform similarly since:

1. Expression tree traversal is identical
2. String building is the primary cost
3. Dialect differences are minimal in SQL builder code

Expected variance: <5% between providers for identical operations.

## Optimization Opportunities

### Expression Caching

Specifications can be cached and reused:

```csharp
// Cache specification instances
private static readonly ActiveEntitiesSpec _activeSpec = new();

// Reuse in queries
var results = await _repository.ListAsync(_activeSpec);
```

### Pre-compiled SQL

For frequently used specifications, consider pre-compiling SQL at startup:

```csharp
// During initialization
_cachedSql = _sqlBuilder.BuildSelectStatement("Entities", _spec);

// During execution (no rebuild)
await _connection.QueryAsync<Entity>(_cachedSql, parameters);
```

## Running Benchmarks

```bash
# Run SQL builder benchmarks (SQLite only - shared logic)
cd tests/Encina.BenchmarkTests/Encina.Dapper.Benchmarks
dotnet run -c Release -- --filter "*SpecificationSqlBuilderBenchmarks*"

# Same for ADO.NET
cd tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks
dotnet run -c Release -- --filter "*SpecificationSqlBuilderBenchmarks*"

# Export results
dotnet run -c Release -- --filter "*SpecificationSqlBuilder*" --exporters json markdown
```

## Notes

### Why SQLite-Only for Most Benchmarks

The `SpecificationSqlBuilderBenchmarks` class uses SQLite as the representative provider because:

1. Expression translation logic is shared across all providers
2. Only identifier quoting and pagination syntax differ
3. These dialect differences have negligible performance impact

### Multi-Provider Testing

For comprehensive dialect testing, use integration tests rather than micro-benchmarks. Integration tests validate correctness across providers while benchmarks measure performance on the shared logic.

## Updating This Document

1. Run benchmarks with `--exporters markdown`
2. Copy results from `BenchmarkDotNet.Artifacts/results/`
3. Update tables in this document
4. Add date and environment info

## Benchmark Environment

- **Date**: [To be filled]
- **Machine**: [To be filled]
- **OS**: [To be filled]
- **.NET Version**: .NET 10
- **BenchmarkDotNet Version**: [To be filled]

## Change History

| Date | Change | Notes |
|------|--------|-------|
| 2026-01-28 | Initial template created | Issue #568 |
