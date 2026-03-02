# LoadTests - Compliance Retention (Core Module)

## Status: Not Implemented

## Justification

The core Retention module (`Encina.Compliance.Retention`) operates entirely in-memory using thread-safe `ConcurrentDictionary` data structures. Load testing this module would primarily stress-test .NET BCL concurrent collections rather than Encina code. The enforcement logic processes records sequentially within each cycle, and the real concurrency concerns exist only in database-backed provider implementations.

### 1. Thread-Safe by Design

The in-memory stores (`InMemoryRetentionRecordStore`, `InMemoryRetentionPolicyStore`, `InMemoryRetentionAuditStore`, `InMemoryLegalHoldStore`) use `ConcurrentDictionary<string, T>` internally. This collection is specifically designed for high-concurrency scenarios and is extensively tested by the .NET runtime team. Load testing would effectively stress-test `ConcurrentDictionary`, not Encina code.

### 2. No Connection or Transaction Management

Unlike database-backed stores (where connection pooling, transaction coordination, and deadlocks are real concerns under load), the core module has:

- No database connections to pool or exhaust
- No transactions to coordinate or deadlock
- No I/O-bound operations that could bottleneck
- No network calls that could timeout or fail under pressure

Load tests are most valuable when they reveal resource contention issues. The core module has no shared external resources to contend over.

### 3. Sequential Enforcement Processing

The `DefaultRetentionEnforcer.EnforceRetentionAsync()` processes expired records sequentially within a single enforcement cycle. There is no parallelism within the enforcement loop itself. The `RetentionEnforcementService` (hosted service) runs enforcement cycles at configurable intervals but never concurrently — each cycle completes before the next starts.

### 4. Legal Hold Operations Are Idempotent

The `DefaultLegalHoldManager` operations (apply, release, check) are designed to be idempotent:

- `ApplyHoldAsync` checks for existing active holds before creating
- `ReleaseHoldAsync` validates hold state before releasing
- `IsUnderHoldAsync` is a pure read operation

These operations do not benefit from load testing since their correctness under concurrent access is guaranteed by the underlying `ConcurrentDictionary`.

### 5. Adequate Coverage from Other Test Types

- **Unit Tests**: Cover all store operations, enforcement logic, legal hold management, and policy evaluation with explicit assertions (230+ tests)
- **Guard Tests**: Verify all public methods reject null/invalid parameters correctly (92 tests)
- **Property Tests**: Verify retention invariants (record creation, policy evaluation, mapper roundtrips) across randomized inputs (58 tests)
- **Contract Tests**: Verify all store implementations satisfy their interface contracts uniformly (51 tests)
- **Integration Tests**: Test full DI pipeline, lifecycle flows, and concurrent record creation (14 tests)

### 6. Recommended Alternative

If load testing becomes necessary in the future (e.g., for database-backed provider implementations):

1. Use NBomber scenarios to simulate concurrent enforcement cycles across multiple tenants
2. Focus on database-backed stores (ADO.NET, Dapper, EF Core) where connection pooling and transaction management are real concerns
3. Test concurrent legal hold apply/release operations against real databases to verify isolation guarantees
4. Measure enforcement throughput with varying record volumes (1K, 10K, 100K expired records)

```csharp
// Example future load test structure
var scenario = Scenario.Create("retention-enforcement", async context =>
{
    var enforcer = serviceProvider.GetRequiredService<IRetentionEnforcer>();
    var result = await enforcer.EnforceRetentionAsync(context.CancellationToken);
    return result.IsRight ? Response.Ok() : Response.Fail();
})
.WithLoadSimulations(
    Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(5)));
```

## Related Files

- `src/Encina.Compliance.Retention/` - Core retention module source
- `tests/Encina.UnitTests/Compliance/Retention/` - Unit tests (230+ tests)
- `tests/Encina.IntegrationTests/Compliance/Retention/` - Integration tests (14 tests)

## Date: 2026-03-01
## Issue: #406
