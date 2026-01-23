# Load Tests - Repository Pattern

## Status: Not Implemented

## Justification

Load tests for the Repository pattern are not implemented for the following reasons:

### 1. Repository is a Thin Abstraction Layer

The Repository pattern in Encina is a thin abstraction over:
- **ADO.NET**: Raw `IDbConnection` and `IDbCommand`
- **Dapper**: `SqlMapper` extension methods
- **EF Core**: `DbSet<T>` and `DbContext`
- **MongoDB**: `IMongoCollection<T>`

The performance characteristics are determined by the underlying data access technology, not the repository layer.

### 2. Load Testing Should Target the Underlying Provider

Meaningful load tests should be performed at:
- Database level (connection pooling, query performance)
- Application level (API endpoints, request handling)
- Infrastructure level (network latency, concurrent connections)

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify correct behavior and SQL generation
- **Guard Tests**: Ensure parameter validation doesn't impact performance
- **Property Tests**: Verify invariants hold under various inputs
- **Benchmarks**: Micro-benchmarks for specific operations (see BenchmarkTests)

### 4. Recommended Alternative

For load testing database operations:
1. Use NBomber or k6 to test API endpoints that use repositories
2. Use database-specific profiling tools (SQL Server Profiler, pgAdmin, etc.)
3. Run integration tests with concurrency using xUnit's `[Theory]` with parallel execution

```csharp
// Example: NBomber scenario
var scenario = Scenario.Create("repository-load", async context =>
{
    var repository = context.GetService<IFunctionalRepository<Order, Guid>>();
    var result = await repository.ListAsync(CancellationToken.None);
    return result.IsRight ? Response.Ok() : Response.Fail();
});
```

## Related Files

- `src/Encina.*/Repository/` - All repository implementations
- `tests/Encina.BenchmarkTests/` - Micro-benchmarks for performance-critical paths

## Date: 2026-01-24

## Issue: #279
