# LoadTests - EntityFrameworkCore AuditInterceptor

## Status: Not Implemented

## Justification

The `AuditInterceptor` does not warrant dedicated load tests for the following technical reasons:

### 1. Inherits Database Concurrency Characteristics

The interceptor runs within `DbContext.SaveChangesAsync()`, which already has well-defined concurrency behavior:

- **DbContext is not thread-safe**: Each request gets its own scoped DbContext
- **Connection pooling**: Managed by the database provider, not the interceptor
- **Transaction isolation**: Controlled by EF Core, not affected by audit field population

Load testing the interceptor in isolation would not reveal any concurrency issues that don't already exist in EF Core itself.

### 2. No Shared Mutable State

The interceptor's design explicitly avoids shared state:

| Component | Concurrency Model |
|-----------|-------------------|
| `AsyncLocal<List<PendingAuditEntry>>` | Thread-isolated, no contention |
| `TimeProvider` | Stateless, injectable |
| `IRequestContext` | Scoped per request |
| `ILogger` | Thread-safe by design |

There are no locks, semaphores, or shared collections that could become bottlenecks under load.

### 3. InMemoryAuditLogStore is Test-Only

The `InMemoryAuditLogStore` uses `ConcurrentDictionary`, which is already battle-tested for concurrent access. Production implementations would use database-backed stores, which would have their own load test requirements.

### 4. Bottleneck is Always the Database

Under high concurrency:

```
Request → DbContext → AuditInterceptor → Database
            ↑                               ↑
         ~microseconds                 ~milliseconds
```

The database (connection pool, transaction log, disk I/O) will always be the limiting factor, not the in-memory interceptor operations.

### 5. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify correct behavior under single-threaded execution
- **Integration Tests**: Test against real databases with realistic workloads
- **Guard Tests**: Verify null-safety and parameter validation

### 6. When Load Tests Would Be Appropriate

Load tests for audit functionality would be appropriate if:

1. **Custom IAuditLogStore implementation**: A production store that writes to a separate audit database could have its own concurrency concerns
2. **Batch audit processing**: If audit entries are batched and processed asynchronously
3. **Distributed audit aggregation**: If audit entries are sent to external systems (Kafka, event hub)

**Recommended future load test scenario**:

```csharp
// Only implement if production store is added
[LoadTest]
public async Task AuditLogStore_UnderHighConcurrency_MaintainsThroughput()
{
    var store = new SqlAuditLogStore(connectionString); // Future implementation
    var tasks = Enumerable.Range(0, 100)
        .Select(_ => SimulateConcurrentAuditWrites(store));

    await Task.WhenAll(tasks);
    // Assert throughput and latency metrics
}
```

## Related Files

- `src/Encina.EntityFrameworkCore/Auditing/AuditInterceptor.cs` - Main interceptor implementation
- `src/Encina.DomainModeling/Auditing/InMemoryAuditLogStore.cs` - Test-only audit store
- `tests/Encina.UnitTests/EntityFrameworkCore/Auditing/AuditInterceptorTests.cs` - Unit test coverage

## Date: 2026-01-31
## Issue: #286
