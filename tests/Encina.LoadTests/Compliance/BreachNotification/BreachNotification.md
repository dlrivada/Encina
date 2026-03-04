# LoadTests - Compliance BreachNotification (Core Module)

## Status: Not Implemented

## Justification

The core BreachNotification module (`Encina.Compliance.BreachNotification`) operates entirely in-memory using thread-safe `ConcurrentDictionary` data structures. Load testing this module would primarily stress-test .NET BCL concurrent collections rather than Encina code. The breach handling logic processes each breach sequentially through a linear workflow, and the real concurrency concerns exist only in database-backed provider implementations.

### 1. Thread-Safe by Design

The in-memory stores (`InMemoryBreachRecordStore`, `InMemoryBreachAuditStore`) use `ConcurrentDictionary<string, T>` internally. This collection is specifically designed for high-concurrency scenarios and is extensively tested by the .NET runtime team. Load testing would effectively stress-test `ConcurrentDictionary`, not Encina code.

### 2. No Connection or Transaction Management

Unlike database-backed stores (where connection pooling, transaction coordination, and deadlocks are real concerns under load), the core module has:

- No database connections to pool or exhaust
- No transactions to coordinate or deadlock
- No I/O-bound operations that could bottleneck
- No network calls that could timeout or fail under pressure

Load tests are most valuable when they reveal resource contention issues. The core module has no shared external resources to contend over.

### 3. Sequential Breach Handling

The `DefaultBreachHandler` processes each breach through a linear workflow per invocation: validate input, record audit entry, execute core operation, update breach status, record completion audit, publish notification. There is no parallelism within the handler itself. The `BreachDeadlineMonitorService` (hosted service) runs monitoring cycles at configurable intervals using `PeriodicTimer` but never concurrently -- each cycle completes before the next starts.

### 4. Detection Rules Are Stateless

The built-in detection rules (`UnauthorizedAccessRule`, `MassDataExfiltrationRule`, `PrivilegeEscalationRule`, `AnomalousQueryPatternRule`) perform simple `SecurityEventType` enum comparisons with no mutable state. Each rule's `EvaluateAsync` method checks a single `EventType` match and creates a `PotentialBreach` record if matched. No aggregation, windowing, or stateful pattern matching is involved.

### 5. Adequate Coverage from Other Test Types

- **Unit Tests**: Cover all store operations, breach handler logic, detection rules, notification workflow, mapper roundtrips, and options validation with explicit assertions
- **Guard Tests**: Verify all public methods reject null/invalid parameters correctly across `DefaultBreachHandler`, `DefaultBreachDetector`, `DefaultBreachNotifier`, `InMemoryBreachRecordStore`, and `InMemoryBreachAuditStore`
- **Contract Tests**: Verify both `IBreachRecordStore` and `IBreachAuditStore` implementations satisfy their interface contracts uniformly

### 6. Recommended Alternative

If load testing becomes necessary in the future (e.g., for database-backed provider implementations):

1. Use NBomber scenarios to simulate concurrent breach detection and notification workflows across multiple tenants
2. Focus on database-backed stores (ADO.NET, Dapper, EF Core, MongoDB) where connection pooling and transaction management are real concerns
3. Test concurrent `HandleDetectedBreachAsync` + `NotifyAuthorityAsync` operations against real databases to verify isolation guarantees
4. Measure deadline monitoring throughput with varying breach volumes (100, 1K, 10K active breaches with approaching deadlines)

```csharp
// Example future load test structure
var scenario = Scenario.Create("breach-notification", async context =>
{
    var handler = serviceProvider.GetRequiredService<IBreachHandler>();
    var breach = new PotentialBreach
    {
        DetectionRuleName = "LoadTestRule",
        Severity = BreachSeverity.High,
        Description = $"Load test breach {context.ScenarioInfo.ThreadNumber}",
        SecurityEvent = SecurityEventFactory.CreateUnauthorizedAccess("load-test"),
        DetectedAtUtc = DateTimeOffset.UtcNow,
        RecommendedActions = []
    };

    var result = await handler.HandleDetectedBreachAsync(breach, context.CancellationToken);
    return result.IsRight ? Response.Ok() : Response.Fail();
})
.WithLoadSimulations(
    Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(5)));
```

## Related Files

- `src/Encina.Compliance.BreachNotification/` - Core breach notification module source
- `tests/Encina.UnitTests/Compliance/BreachNotification/` - Unit tests
- `tests/Encina.GuardTests/Compliance/BreachNotification/` - Guard tests
- `tests/Encina.ContractTests/Compliance/BreachNotification/` - Contract tests

## Date: 2026-03-03
## Issue: #408
