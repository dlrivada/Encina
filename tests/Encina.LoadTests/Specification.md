# Load Tests - Specification Pattern

## Status: Not Implemented

## Justification

Load tests for the Specification pattern are not implemented for the following reasons:

### 1. Specification is a Pure In-Memory Pattern

The Specification pattern performs no I/O operations:
- `IsSatisfiedBy()` evaluates a compiled expression against an entity
- `ToExpression()` returns a pre-built expression tree
- `And()`, `Or()`, `Not()` compose expressions without side effects

### 2. SQL Translation is a One-Time Operation

`SpecificationSqlBuilder` translates specifications to SQL once per query:
- The SQL string is generated synchronously
- No network calls or database connections are involved
- The operation is CPU-bound and completes in microseconds

### 3. Load Testing Should Target Query Execution

Meaningful load tests should be performed at the database level:
- Query execution time under concurrent load
- Connection pool exhaustion scenarios
- Database server CPU/memory under high query volume

These are Repository-level concerns, not Specification concerns.

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify correct SQL generation and expression composition
- **Guard Tests**: Ensure parameter validation doesn't impact performance
- **Property Tests**: Verify invariants hold under varied inputs
- **Benchmarks**: Micro-benchmarks for SQL generation (see BenchmarkTests)

### 5. Recommended Alternative

For load testing database operations with specifications:
1. Use NBomber or k6 to test API endpoints that use specifications
2. Use database-specific profiling tools

```csharp
// Example: NBomber scenario for specification-based queries
var scenario = Scenario.Create("specification-load", async context =>
{
    var spec = new ActiveOrdersSpec();
    var result = await _repository.ListAsync(spec, CancellationToken.None);
    return result.IsRight ? Response.Ok() : Response.Fail();
});
```

## Related Files

- `src/Encina.DomainModeling/Specification.cs`
- `src/Encina.*/Repository/SpecificationSqlBuilder.cs`
- `tests/Encina.BenchmarkTests/` - Micro-benchmarks for performance-critical paths

## Date: 2026-01-24

## Issue: #280
