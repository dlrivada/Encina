<!--
title: [BUG] GetStuckSagasAsync in Encina.MongoDB misses status filter causing TimedOut sagas to be permanently re-surfaced
labels: bug
milestone: v0.14.0 — Hardening
-->

## Description

The `GetStuckSagasAsync` method in `SagaStoreMongoDB.cs` constructs a filter using `Filter.Eq(s => s.CompletedAtUtc, null)` and `Filter.Lt(s => s.LastUpdatedAtUtc, threshold)` but omits any status filter. This creates a coherence defect where the MongoDB provider behaves differently from the other 9 providers (e.g., `SagaStoreADO.cs`), which correctly include `WHERE (Status = 'Running' OR Status = 'Compensating')`. Consequently, sagas with status `TimedOut` are incorrectly and permanently re-surfaced by `GetStuckSagasAsync` because `TimeoutAsync` sets `Status = TimedOut` without setting `CompletedAtUtc`, leaving `CompletedAtUtc` as null indefinitely.

## Steps to Reproduce

1. Review the filter construction in `src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs` at lines 129-132 within the `GetStuckSagasAsync` method.
2. Observe that the filter consists only of `Filter.Eq(s => s.CompletedAtUtc, null)` and `Filter.Lt(s => s.LastUpdatedAtUtc, threshold)`.
3. Compare this with the implementation in `src/Encina.ADO.SqlServer/Sagas/SagaStoreADO.cs` at lines 158-159, which includes `WHERE (Status = 'Running' OR Status = 'Compensating') AND LastUpdatedAtUtc < @ThresholdUtc`.
4. Contrast with the `GetExpiredSagasAsync` method in the same file at `src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs` lines 154-158, which correctly applies the `ActiveSagaStatuses` filter (`Running`, `Compensating`).
5. Note that `TimeoutAsync` in `SagaOrchestrator.cs` at line 447 sets `Status = TimedOut` but does not set `CompletedAtUtc`.
6. See the resulting behavior where `TimedOut` sagas are permanently returned by `GetStuckSagasAsync` on MongoDB, unlike the other 9 providers.

## Expected Behavior

The `GetStuckSagasAsync` method in the MongoDB provider should exclude sagas that are not in an active state (specifically `Running` or `Compensating`) to match the behavior of the other 9 providers. A saga with status `TimedOut` should not be returned by `GetStuckSagasAsync` once its status has left `Running`/`Compensating`.

## Actual Behavior

The `GetStuckSagasAsync` method returns sagas with status `TimedOut` indefinitely because the filter only checks for `CompletedAtUtc` being null and `LastUpdatedAtUtc` being less than the threshold. Since `TimedOut` sagas never have `CompletedAtUtc` set, they remain in the "stuck" batch permanently on the MongoDB provider, violating provider coherence requirements.

## Environment

- **Encina Version**: 0.14.0-dev
- **.NET Version**: .NET 10
- **OS**: Not applicable (found by static review of the code, not at runtime)
- **Package(s) Affected**: Encina.MongoDB

## Code Sample

```csharp
// Defect location: src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs:129-132
// Current implementation missing status filter:
var filter = Builders<Saga>.Filter.And(
    Builders<Saga>.Filter.Eq(s => s.CompletedAtUtc, null),
    Builders<Saga>.Filter.Lt(s => s.LastUpdatedAtUtc, threshold)
);
// Missing: Builders<Saga>.Filter.In(s => s.Status, new[] { SagaStatus.Running, SagaStatus.Compensating })

// Correct implementation in other providers (e.g., SagaStoreADO.cs:158-159):
// WHERE (Status = 'Running' OR Status = 'Compensating') AND LastUpdatedAtUtc < @ThresholdUtc
```

## Stack Trace

```
No stack trace available; defect identified via static code review of filter logic in SagaStoreMongoDB.cs:129-132 comparing provider coherence against SagaStoreADO.cs:158-159.
```

## Additional Context

This defect violates AGENTS.md §5 provider coherence which states that "providers share the same interfaces... and differ only in implementation." The MongoDB provider's `GetExpiredSagasAsync` method (lines 154-158 in the same file) correctly applies the status filter, highlighting that the omission in `GetStuckSagasAsync` is an implementation error rather than a design choice. Related issues include #16, #699, #696, and #181.