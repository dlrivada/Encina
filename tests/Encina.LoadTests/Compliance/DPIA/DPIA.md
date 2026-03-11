# LoadTests - Compliance DPIA (Core Module)

## Status: Not Implemented

## Justification

The core DPIA module (`Encina.Compliance.DPIA`) operates entirely in-memory using thread-safe `ConcurrentDictionary` data structures. Load testing this module would primarily stress-test .NET BCL concurrent collections rather than Encina code. The assessment engine evaluates risk criteria sequentially, and the real concurrency concerns exist only in database-backed provider implementations.

### 1. Thread-Safe by Design

The in-memory stores (`InMemoryDPIAStore`, `InMemoryDPIAAuditStore`) use `ConcurrentDictionary<string, T>` internally. This collection is specifically designed for high-concurrency scenarios and is extensively tested by the .NET runtime team. Load testing would effectively stress-test `ConcurrentDictionary`, not Encina code.

### 2. No Connection or Transaction Management

Unlike database-backed stores (where connection pooling, transaction coordination, and deadlocks are real concerns under load), the core module has:

- No database connections to pool or exhaust
- No transactions to coordinate or deadlock
- No I/O-bound operations that could bottleneck
- No network calls that could timeout or fail under pressure

Load tests are most valuable when they reveal resource contention issues. The core module has no shared external resources to contend over.

### 3. Sequential Assessment Engine

The `DefaultDPIAAssessmentEngine` evaluates each request through a linear workflow: iterate over registered `IRiskCriterion` instances, aggregate risk levels, build a `DPIAResult`, and store the assessment. There is no parallelism within the engine itself. The `DPIAReviewReminderService` (hosted service) runs monitoring cycles at configurable intervals using `PeriodicTimer` but never concurrently.

### 4. Risk Criteria Are Stateless

The 6 built-in risk criteria (`SystematicProfilingCriterion`, `SpecialCategoryDataCriterion`, `SystematicMonitoringCriterion`, `AutomatedDecisionMakingCriterion`, `LargeScaleProcessingCriterion`, `VulnerableSubjectsCriterion`) perform simple pattern matching on `DPIAContext` properties with no mutable state. Each criterion's evaluation is a pure function with deterministic output.

### 5. Pipeline Behavior Is a Single Lookup

`DPIARequiredPipelineBehavior` checks whether a request type has a `[RequiresDPIA]` attribute and whether an approved assessment exists. This is a single dictionary lookup followed by a conditional log/throw. Not a meaningful load test target.

### 6. Adequate Coverage from Other Test Types

- **Unit Tests**: Cover all store operations, assessment engine logic, risk criteria evaluation, pipeline behavior, template provider, and options validation
- **Guard Tests**: Verify all public methods reject null/invalid parameters correctly
- **Contract Tests**: Verify all 10 provider implementations satisfy `IDPIAStore` and `IDPIAAuditStore` contracts
- **Property Tests**: Verify invariants hold across randomized inputs for both store types
- **Integration Tests**: Pipeline tests (27) + Database store tests (12 per provider × 13 providers)

### 7. Recommended Alternative

If load testing becomes necessary in the future (e.g., for database-backed provider implementations):

1. Use NBomber scenarios to simulate concurrent assessment creation and retrieval across multiple tenants
2. Focus on database-backed stores (ADO.NET, Dapper, EF Core, MongoDB) where connection pooling is a real concern
3. Test concurrent `SaveAssessmentAsync` + `GetAssessmentAsync` operations against real databases
4. Measure expiration monitoring throughput with varying assessment volumes (100, 1K, 10K assessments past review date)

```csharp
// Example future load test structure
var scenario = Scenario.Create("dpia-assessment", async context =>
{
    var store = serviceProvider.GetRequiredService<IDPIAStore>();
    var assessment = new DPIAAssessment
    {
        Id = Guid.NewGuid(),
        RequestTypeName = $"LoadTestCommand_{context.ScenarioInfo.ThreadNumber}",
        Status = DPIAAssessmentStatus.Draft,
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    var result = await store.SaveAssessmentAsync(assessment, context.CancellationToken);
    return result.IsRight ? Response.Ok() : Response.Fail();
})
.WithLoadSimulations(
    Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(5)));
```

## Related Files

- `src/Encina.Compliance.DPIA/` - Core DPIA module source
- `tests/Encina.UnitTests/Compliance/DPIA/` - Unit tests
- `tests/Encina.GuardTests/Compliance/DPIA/` - Guard tests
- `tests/Encina.ContractTests/Compliance/DPIA/` - Contract tests
- `tests/Encina.PropertyTests/Compliance/DPIA/` - Property tests

## Date: 2026-03-11
## Issue: #409
