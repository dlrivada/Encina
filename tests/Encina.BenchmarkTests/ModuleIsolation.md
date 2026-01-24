# Benchmark Tests - Module Isolation

## Status: Not Implemented

## Justification

Benchmarks for Module Isolation are intentionally not implemented for the following reasons:

### 1. Development-Only Feature

Module Isolation schema validation:
- Is only active in `DevelopmentValidationOnly` mode
- Is disabled in production environments
- Does not impact production performance

### 2. Simple Implementation

The implementation consists of:
- Thin connection wrappers (no complex logic)
- Regex-based schema extraction
- HashSet lookups for allowed schemas

All operations are O(1) or O(n) where n is the number of schemas in SQL (typically 1-5).

### 3. Not a Hot Path

Connection creation happens:
- Once per request/transaction (not per query)
- In development only (for validation)
- With negligible overhead compared to actual database operations

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify correctness of schema parsing
- **Contract Tests**: Ensure API consistency across providers
- **Property Tests**: Validate parsing with random inputs

### 5. Recommended Approach If Benchmarks Are Needed

If performance concerns arise, benchmark the core parsing logic:

```csharp
[MemoryDiagnoser]
public class ModuleIsolationBenchmarks
{
    private readonly ModuleSchemaRegistry _registry;
    private readonly string _simpleQuery = "SELECT * FROM orders.Products WHERE Id = @Id";
    private readonly string _complexQuery = "SELECT o.*, c.Name FROM orders.Orders o JOIN customers.Customers c ON o.CustomerId = c.Id WHERE o.Status = @Status";

    public ModuleIsolationBenchmarks()
    {
        var options = new ModuleIsolationOptions();
        options.AddModuleSchema("Orders", "orders");
        options.AddSharedSchemas("shared");
        _registry = new ModuleSchemaRegistry(options);
    }

    [Benchmark(Baseline = true)]
    public SchemaAccessValidationResult ValidateSimpleQuery()
        => _registry.ValidateSqlAccess("Orders", _simpleQuery);

    [Benchmark]
    public SchemaAccessValidationResult ValidateComplexQuery()
        => _registry.ValidateSqlAccess("Orders", _complexQuery);
}
```

Expected results:
- Simple query: < 1μs
- Complex query: < 5μs
- Memory allocation: < 1KB per validation

## Related Files

- `src/Encina/Modules/Isolation/ModuleSchemaRegistry.cs`
- `src/Encina/Modules/Isolation/IModuleSchemaRegistry.cs`

## Date: 2026-01-24
## Issue: #534
