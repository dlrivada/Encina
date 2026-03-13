# LoadTests - Compliance ProcessorAgreements (Core Module)

## Status: Not Implemented

## Justification

The core ProcessorAgreements module (`Encina.Compliance.ProcessorAgreements`) operates entirely in-memory using thread-safe `ConcurrentDictionary` data structures. Load testing this module would primarily stress-test .NET BCL concurrent collections rather than Encina code. The DPA validator performs sequential lookups, and the real concurrency concerns exist only in database-backed provider implementations.

### 1. Thread-Safe by Design

The in-memory stores (`InMemoryProcessorRegistry`, `InMemoryDPAStore`, `InMemoryProcessorAuditStore`) use `ConcurrentDictionary<string, T>` internally. This collection is specifically designed for high-concurrency scenarios and is extensively tested by the .NET runtime team. Load testing would effectively stress-test `ConcurrentDictionary`, not Encina code.

### 2. No Connection or Transaction Management

Unlike database-backed stores (where connection pooling, transaction coordination, and deadlocks are real concerns under load), the core module has:

- No database connections to pool or exhaust
- No transactions to coordinate or deadlock
- No I/O-bound operations that could bottleneck
- No network calls that could timeout or fail under pressure

Load tests are most valuable when they reveal resource contention issues. The core module has no shared external resources to contend over.

### 3. Sequential Validator Logic

The `DefaultDPAValidator` evaluates each DPA through a linear workflow: look up the processor in the registry, query the DPA store, check expiration dates, and verify required terms. There is no parallelism within the validator itself. The `CheckDPAExpirationHandler` queries the store for expiring DPAs and publishes notifications sequentially.

### 4. Pipeline Behavior Is Attribute Lookup + Single Store Query

`ProcessorValidationPipelineBehavior` checks whether a request type has a `[RequiresProcessorAgreement]` attribute (cached in a static `ConcurrentDictionary` after first invocation) and then performs a single store query to verify DPA status. This is a two-level validation (attribute check, then store lookup) but both operations are sub-microsecond in the in-memory implementation.

### 5. Health Check Is a Simple Service Resolution

`ProcessorAgreementHealthCheck` resolves registered services from the DI container and queries stores for basic availability. It performs no complex computations and runs at configurable health check intervals, not under sustained load.

### 6. Adequate Coverage from Other Test Types

- **Unit Tests**: Cover all store operations, DPA validation logic, pipeline behavior, expiration handler, health check, and options validation
- **Guard Tests**: Verify all public methods reject null/invalid parameters correctly
- **Contract Tests**: Verify all provider implementations satisfy `IProcessorRegistry`, `IDPAStore`, and `IProcessorAuditStore` contracts
- **Property Tests**: Verify invariants hold across randomized inputs for all store types
- **Integration Tests**: Pipeline tests + Database store tests (per provider x 13 providers)

### 7. Recommended Alternative

If load testing becomes necessary in the future (e.g., for database-backed provider implementations):

1. Use NBomber scenarios to simulate concurrent DPA creation, validation, and expiration checks across multiple tenants
2. Focus on database-backed stores (ADO.NET, Dapper, EF Core, MongoDB) where connection pooling is a real concern
3. Test concurrent `RegisterProcessorAsync` + `ValidateDPAAsync` operations against real databases
4. Measure expiration monitoring throughput with varying DPA volumes (100, 1K, 10K agreements past expiration date)

```csharp
// Example future load test structure
var scenario = Scenario.Create("processor-agreements", async context =>
{
    var store = serviceProvider.GetRequiredService<IDPAStore>();
    var agreement = new DataProcessingAgreement
    {
        Id = Guid.NewGuid().ToString(),
        ProcessorId = $"processor-{context.ScenarioInfo.ThreadNumber}",
        Status = DPAStatus.Active,
        CreatedAtUtc = DateTimeOffset.UtcNow,
        ExpiresAtUtc = DateTimeOffset.UtcNow.AddYears(1)
    };

    var result = await store.SaveAsync(agreement, context.CancellationToken);
    return result.IsRight ? Response.Ok() : Response.Fail();
})
.WithLoadSimulations(
    Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(5)));
```

## Related Files

- `src/Encina.Compliance.ProcessorAgreements/` - Core ProcessorAgreements module source
- `tests/Encina.UnitTests/Compliance/ProcessorAgreements/` - Unit tests
- `tests/Encina.GuardTests/Compliance/ProcessorAgreements/` - Guard tests
- `tests/Encina.ContractTests/Compliance/ProcessorAgreements/` - Contract tests
- `tests/Encina.PropertyTests/Compliance/ProcessorAgreements/` - Property tests

## Date: 2026-03-13
## Issue: #410
