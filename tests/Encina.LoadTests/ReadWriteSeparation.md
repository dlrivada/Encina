# Load Tests - Read/Write Separation Pattern

## Status: Not Implemented

## Justification

Load tests for the Read/Write Separation pattern are not implemented for the following reasons:

### 1. Read/Write Separation is a Routing Pattern, Not a Hot Path

The Read/Write Separation pattern routes connections based on intent:

- `IReadWriteConnectionSelector.GetConnectionString()` - determines routing (O(1))
- `ReadWriteConnectionFactory.CreateConnection()` - creates a new connection
- `DatabaseRoutingScope` - sets ambient context using `AsyncLocal<T>`

### 2. Database Operations Are the Load-Sensitive Component

Meaningful load tests should target:

- Actual database query execution under high concurrency
- Connection pool exhaustion scenarios
- Replica selection strategy performance under load
- Network latency between primary and replicas

These are infrastructure-level concerns, not code-level concerns.

### 3. Replica Selection Strategies Are Lightweight

The replica selection operations:

- `RoundRobinReplicaSelector`: `Interlocked.Increment()` + array index
- `RandomReplicaSelector`: `Random.Next()` + array index
- `LeastConnectionsReplicaSelector`: Loop through connections, select minimum

All operations complete in nanoseconds.

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify connection factory behavior, routing context, selector strategies
- **Contract Tests**: Verify API consistency across all 12 providers
- **Property Tests**: Could verify replica selection invariants (future)

### 5. Recommended Load Test Approach (if needed)

For load testing read/write separation effectiveness:

```csharp
// NBomber scenario for read/write separation
var scenario = Scenario.Create("read-write-load", async context =>
{
    // 80% reads, 20% writes
    var intent = context.Random.Next(100) < 80
        ? DatabaseIntent.Read
        : DatabaseIntent.Write;

    using var scope = new DatabaseRoutingScope(intent);
    await using var connection = await _factory.CreateConnectionAsync();

    // Execute actual database operation
    if (intent == DatabaseIntent.Read)
    {
        await connection.QueryAsync<Product>("SELECT * FROM Products WHERE Id = @Id", new { Id = context.Random.Next(1000) });
    }
    else
    {
        await connection.ExecuteAsync("UPDATE Products SET Price = @Price WHERE Id = @Id", new { Price = 100, Id = context.Random.Next(1000) });
    }

    return Response.Ok();
})
.WithLoadSimulations(
    Simulation.InjectPerSec(rate: 1000, during: TimeSpan.FromMinutes(5))
);
```

### 6. Why Not Include These Load Tests Now

- **Routing logic**: Single dictionary lookup + strategy selection, ~10ns
- **Connection creation**: Dominated by actual connection pool behavior
- **Scope management**: `AsyncLocal<T>` operations, sub-microsecond

## Related Files

- `src/Encina.Messaging/ReadWriteSeparation/` - Core abstractions
- `src/Encina.*/ReadWriteSeparation/` - Provider implementations (12 providers)
- `tests/Encina.UnitTests/*/ReadWriteSeparation/` - Unit tests
- `tests/Encina.ContractTests/Database/ReadWriteSeparation/` - Contract tests

## Date: 2026-01-24

## Issue: #283
