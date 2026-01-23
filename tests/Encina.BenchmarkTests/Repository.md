# Benchmark Tests - Repository Pattern

## Status: Not Implemented

## Justification

Benchmark tests for the Repository pattern are not implemented for the following reasons:

### 1. Repository Overhead is Negligible

The Repository pattern adds minimal overhead:
- **EntityMappingBuilder.Build()**: One-time cost at startup, cached for application lifetime
- **GetId()**: Single property accessor call
- **SQL Generation**: Pre-built at construction time, O(1) lookup

### 2. Performance is Determined by Underlying Technology

The actual performance characteristics are determined by:
- Database query execution time
- Network latency
- Connection pooling efficiency
- Serialization/deserialization (for MongoDB)

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify correct behavior without database overhead
- **Property Tests**: Verify behavior across varied inputs
- **Integration Tests**: Validate real database interactions

### 4. Recommended Benchmarks (if needed)

If benchmarks are required, focus on:

```csharp
[MemoryDiagnoser]
public class EntityMappingBenchmarks
{
    [Benchmark]
    public IEntityMapping<Order, Guid> BuildMapping()
    {
        return new EntityMappingBuilder<Order, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .MapProperty(o => o.CustomerId)
            .Build();
    }
}

[MemoryDiagnoser]
public class SpecificationSqlBuilderBenchmarks
{
    [Benchmark]
    public string BuildWhereClause()
    {
        var spec = new OrderByCustomerSpecification(Guid.NewGuid());
        return _sqlBuilder.BuildWhereClause(spec);
    }
}
```

### 5. Why Not Include These Benchmarks

- **EntityMappingBuilder.Build()**: Called once at startup, not a hot path
- **GetId()**: Single delegate invocation, ~1ns overhead
- **SQL generation**: Pre-computed at repository construction, cached

## Related Files

- `src/Encina.*/Repository/EntityMappingBuilder.cs` - Mapping configuration
- `src/Encina.*/Repository/SpecificationSqlBuilder.cs` - SQL generation
- `tests/Encina.UnitTests/*/Repository/` - Unit tests for all providers

## Date: 2026-01-24

## Issue: #279
