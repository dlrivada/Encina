# Load Tests - Online Resharding Workflow

## Status: Not Implemented

## Justification

Load tests for the online resharding workflow are not implemented for the following reasons:

### 1. Orchestrator Prevents Concurrent Resharding

The `ReshardingOrchestrator` uses a `ConcurrentDictionary<Guid, ReshardingProgress>` to track active operations and explicitly rejects concurrent resharding with error code `ConcurrentReshardingNotAllowed`. This design decision means there is at most **one** active resharding operation at any time, eliminating the primary use case for load testing (concurrent access patterns).

### 2. Resharding is a Long-Running Batch Operation

Unlike request-response patterns (APIs, queries), resharding is a long-running batch workflow:
- **Copy phase**: Iterates through all migration steps sequentially
- **Replication phase**: Waits for CDC convergence per step
- **Verification phase**: Validates consistency per step
- **Cutover phase**: Single atomic topology swap
- **Cleanup phase**: Sequential deletion per step

Each operation takes minutes to hours. Load testing is designed for high-throughput, short-duration operations, making it inappropriate for this workflow.

### 3. External Service Dependencies Dominate Performance

Resharding performance is entirely bounded by external services:
- Database bulk copy throughput (I/O bound)
- CDC replication lag (network + DB bound)
- Data verification queries (DB bound)
- Topology swap (distributed coordination)

The orchestrator and phase executor add negligible overhead — they are thin coordination layers. Load testing the coordinator without real databases would only test mock object performance.

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify sequential phase execution, state transitions, error handling, callbacks
- **Guard Tests**: Ensure parameter validation for all public methods
- **Property Tests**: Verify record invariants, progress tracking correctness
- **Contract Tests**: Confirm interface contracts for IReshardingOrchestrator, IReshardingStateStore, IReshardingServices
- **Integration Tests**: Validate real state persistence and workflow execution with SQLite

### 5. Recommended Alternative

If resharding load testing is needed in the future, focus on:
1. **State store throughput**: Test `IReshardingStateStore.SaveStateAsync` under concurrent read/write load (multiple services querying progress while orchestrator persists state)
2. **Bulk copy throughput**: Test `IReshardingServices.CopyBatchAsync` with real database connections under varying batch sizes
3. **CDC lag convergence**: Test replication lag monitoring under varying write loads on source shards

These would be provider-specific integration load tests, not orchestrator load tests.

## Related Files

- `src/Encina/Sharding/Resharding/ReshardingOrchestrator.cs` - Orchestrator (prevents concurrent resharding)
- `src/Encina/Sharding/Resharding/Phases/ReshardingPhaseExecutor.cs` - Sequential phase execution
- `src/Encina/Sharding/Resharding/IReshardingStateStore.cs` - State persistence interface
- `src/Encina/Sharding/Resharding/IReshardingServices.cs` - External service abstraction
- `tests/Encina.UnitTests/Sharding/Resharding/` - Unit tests
- `tests/Encina.PropertyTests/Database/Sharding/ReshardingPropertyTests.cs` - Property tests

## Date: 2026-02-16
## Issue: #648
