# Load Tests - Module Isolation

## Status: Not Implemented

## Justification

Load testing for Module Isolation is intentionally not implemented for the following reasons:

### 1. Negligible Performance Overhead

Module Isolation validation occurs only in `DevelopmentValidationOnly` mode, which:
- Is designed for development environments only
- Is disabled in production by default
- Adds minimal overhead (regex parsing of SQL)

### 2. Not a Performance-Critical Path

In production environments using `SchemaWithPermissions`:
- Database permissions are enforced by the RDBMS engine
- No application-level validation occurs
- Performance is determined by database permission checks (microseconds)

### 3. Connection Factory Is Thin Wrapper

The `ModuleAwareConnectionFactory`:
- Wraps an existing connection factory
- Adds conditional schema validation wrapper
- Does not add significant memory or CPU overhead

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify validation logic correctness
- **Contract Tests**: Ensure API consistency
- **Benchmarks**: (if needed) Can measure regex parsing performance

### 5. Recommended Alternative

If load testing is required for Module Isolation:

1. Use BenchmarkDotNet to measure SQL parsing performance
2. Create synthetic tests with various SQL query sizes
3. Measure connection creation overhead in isolation

Example benchmark:
```csharp
[Benchmark]
public void ParseSchemaFromSimpleQuery()
{
    _registry.ValidateSqlAccess("Orders", "SELECT * FROM orders.Products");
}

[Benchmark]
public void ParseSchemaFromComplexJoin()
{
    _registry.ValidateSqlAccess("Orders",
        "SELECT o.*, p.* FROM orders.Orders o JOIN shared.Products p ON o.ProductId = p.Id");
}
```

## Related Files

- `src/Encina/Modules/Isolation/ModuleSchemaRegistry.cs`
- `src/Encina.*/Modules/ModuleAwareConnectionFactory.cs`

## Date: 2026-01-24
## Issue: #534
