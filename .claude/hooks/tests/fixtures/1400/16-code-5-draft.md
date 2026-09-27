<!--
title: [BUG] SagaStoreMongoDB.GetStuckSagasAsync missing status filter causes permanent misclassification of TimedOut sagas
labels: bug
milestone: v0.14.0 — Hardening
-->

## Description

The `GetStuckSagasAsync` method in the MongoDB provider lacks a status filter, unlike all other 9 providers and the sibling `GetExpiredSagasAsync` method in the same file. The filter currently only checks for `CompletedAtUtc` being null and `LastUpdatedAtUtc` being older than a threshold. However, `TimeoutAsync` sets the saga status to `TimedOut` without setting `CompletedAtUtc`. Consequently, on MongoDB, any saga that times out will be permanently and incorrectly reported as "stuck" because its `CompletedAtUtc` remains null and it is not filtered out by status. This violates provider coherence as other providers correctly exclude sagas whose status is not `Running` or `Compensating`.

## Steps to Reproduce

1. Configure an Encina MongoDB Saga Store.
2. Call method `TimeoutAsync` (via `SagaOrchestrator.cs:447`) to transition a saga to `TimedOut` status.
3. Pass parameter `thresholdUtc` to `GetStuckSagasAsync` in `SagaStoreMongoDB.cs:129-132`.
4. See error: The timed-out saga is returned in the results, despite no longer being in an active state.

## Expected Behavior

Timed-out sagas (status `TimedOut`) should be excluded from the results of `GetStuckSagasAsync`, consistent with the behavior of the other 9 providers and the local `GetExpiredSagasAsync` method, which both filter for `Running` or `Compensating` statuses.

## Actual Behavior

Timed-out sagas are permanently re-surfaced by `GetStuckSagasAsync` on MongoDB because the filter does not check the saga status and `CompletedAtUtc` is never set for timed-out sagas.

## Environment

- **Encina Version**: v0.14.0 (development/audit stage)
- **.NET Version**: Not specified in finding
- **OS**: Not specified in finding
- **Package(s) Affected**: Encina.MongoDB

## Code Sample

```csharp
// Minimal reproducible example demonstrating the defect
// File: src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs:129-132

// Current implementation (Defective):
var filter = Builders<SagaState>.Filter
    .Eq(s => s.CompletedAtUtc, null)
    .Lt(s => s.LastUpdatedAtUtc, thresholdUtc);
// Missing: .In(s => s.Status, ActiveSagaStatuses) 

// Comparison with correct implementation in same file (SagaStoreMongoDB.cs:154-158):
var expiredFilter = Builders<SagaState>.Filter
    .In(s => s.Status, ActiveSagaStatuses)
    .Gt(s => s.CompletedAtUtc, expirationThresholdUtc);
```

## Stack Trace

```
// Not applicable - this is a logical query defect, not an exception
```

## Additional Context

- **Location**: `src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs:129-132`
- **Reference Implementation**: `src/Encina.ADO.SqlServer/Sagas/SagaStoreADO.cs:158-159` shows the correct pattern: `WHERE (Status = 'Running' OR Status = 'Compensating') AND LastUpdatedAtUtc < @ThresholdUtc`.
- **Internal Consistency**: The adjacent `GetExpiredSagasAsync` method (`SagaStoreMongoDB.cs:154-158`) correctly uses the `ActiveSagaStatuses` filter. The two batches ("stuck" and "expired") are mutually exclusive on status in this fix; the defect is isolated to the stuck-batch misclassification.
- **Specification Violation**: Fails AGENTS.md §5 provider coherence ("providers share the same interfaces... and differ only in implementation").
- **Related Issues**:
  - #16 (This issue)
  - #699: [FEATURE] Add distributed cache for Choreography State Store lookups
  - #696: [FEATURE] Add distributed cache layer to Saga Store for state lookups
  - #181: [FEATURE] Observability: Create Encina.HealthChecks Package
