# BenchmarkTests - Phase 8 TokenMappingStore Implementations

## Status: Not Implemented

## Justification

The `ITokenMappingStore` implementations are minimal data access wrappers where the performance-critical path is the database query itself, not the C# code. Micro-benchmarking these stores would measure database I/O latency, which varies by environment and is not meaningful in a BenchmarkDotNet context.

### 1. Store Overhead Is Negligible vs Database I/O

A typical `TokenMappingStore` method executes:

1. Build a SQL string or LINQ expression (constant-time, no allocation for parameterized queries)
2. Bind parameters (trivial object creation)
3. Execute the database call (milliseconds of I/O)
4. Map the result to a domain object (single-row mapping)

Steps 1, 2, and 4 combined take microseconds. Step 3 takes milliseconds. Benchmarking would show that 99.9%+ of execution time is in the database call, providing no actionable insight for optimizing the store code.

### 2. SQL Queries Are Trivially Simple

The queries across all 13 providers are single-table operations with no complexity to optimize:

- **No joins** to restructure
- **No subqueries** to flatten
- **No aggregations** to optimize
- **No dynamic query generation** (unlike Specification pattern queries)
- **No ORM overhead** worth measuring (even EF Core generates trivial SQL for these)

Compare this to the Specification/Repository pattern where query generation involves expression tree compilation, include resolution, and dynamic WHERE clause building -- those are legitimate benchmark candidates. Token mapping stores are not.

### 3. Database Performance Should Be Monitored in Production

The meaningful performance metric for token mapping operations is **end-to-end query latency in the target deployment environment**, which depends on:

- Database engine version and configuration
- Network latency between application and database
- Table size and index efficiency
- Concurrent load from other queries

None of these factors can be captured in a BenchmarkDotNet micro-benchmark, which runs against a local database in an isolated environment. Production monitoring (via OpenTelemetry, APM tools) is the appropriate mechanism for tracking token mapping performance.

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify correct SQL generation for all 13 providers, including provider-specific syntax (SQL Server `TOP`, PostgreSQL `LIMIT`, MySQL backtick identifiers, SQLite parameterized DateTime)
- **Guard Tests**: Verify parameter validation across all public methods and all providers
- **Property Tests**: Verify store invariants (idempotency, consistency, uniqueness) with randomized inputs
- **Contract Tests**: Verify behavioral equivalence across all 13 `ITokenMappingStore` implementations
- **Integration Tests**: Verify actual query execution against real databases, ensuring SQL correctness and type mapping fidelity

### 5. Recommended Alternative

If performance measurement becomes necessary for token mapping operations:

1. **Use OpenTelemetry instrumentation** already built into Encina to measure real query latency in production or staging environments
2. **Database-level query analysis**: Use `EXPLAIN ANALYZE` (PostgreSQL), execution plans (SQL Server), or `EXPLAIN` (MySQL) to optimize index usage on the `token_mappings` table
3. **Index verification benchmarks**: If implemented, focus on comparing query performance with and without indexes on `token`, `original_value_hash`, and `subject_id` columns

```sql
-- Example: Verify index usage for the most common query
EXPLAIN ANALYZE
SELECT original_value FROM token_mappings WHERE token = 'sample-token-value';
```

4. **If micro-benchmarks are truly needed**, measure only the C# overhead by mocking the database call:

```csharp
// Example: Benchmark only the parameter binding and result mapping
[MemoryDiagnoser]
public class TokenMappingStoreOverheadBenchmarks
{
    [Benchmark]
    public async Task<TokenMapping?> DapperStore_GetToken()
    {
        // Uses an in-memory SQLite database to minimize I/O variance
        return await _store.GetTokenAsync("subject", "field", "value");
    }
}
```

## Related Files

- `src/Encina.ADO.SqlServer/Anonymization/` - ADO.NET SQL Server implementation
- `src/Encina.ADO.PostgreSQL/Anonymization/` - ADO.NET PostgreSQL implementation
- `src/Encina.ADO.MySQL/Anonymization/` - ADO.NET MySQL implementation
- `src/Encina.ADO.Sqlite/Anonymization/` - ADO.NET SQLite implementation
- `src/Encina.Dapper.SqlServer/Anonymization/` - Dapper SQL Server implementation
- `src/Encina.Dapper.PostgreSQL/Anonymization/` - Dapper PostgreSQL implementation
- `src/Encina.Dapper.MySQL/Anonymization/` - Dapper MySQL implementation
- `src/Encina.Dapper.Sqlite/Anonymization/` - Dapper SQLite implementation
- `src/Encina.EntityFrameworkCore/Anonymization/` - EF Core implementation
- `src/Encina.MongoDB/Anonymization/` - MongoDB implementation
- `tests/Encina.UnitTests/Compliance/Anonymization/` - Unit tests
- `tests/Encina.IntegrationTests/Compliance/Anonymization/` - Integration tests

## Date: 2026-02-28
## Issue: #407
