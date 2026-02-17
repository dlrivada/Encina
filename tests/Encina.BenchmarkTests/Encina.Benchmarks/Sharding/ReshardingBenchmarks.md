# Benchmark Tests - Online Resharding Workflow

## Status: Not Implemented

## Justification

Micro-benchmarks for the online resharding workflow are not implemented for the following reasons:

### 1. No Hot Paths in the Resharding Workflow

The resharding orchestrator and phase executor are thin coordination layers that:
- Call external services via `IReshardingServices` (I/O bound)
- Persist state via `IReshardingStateStore` (I/O bound)
- Execute callbacks (user-defined, I/O bound)
- Manage phase transitions (trivial enum comparisons)

There are no CPU-intensive algorithms, tight loops, or allocation-sensitive hot paths that would benefit from micro-benchmarking. The `ConcurrentDictionary` operations for active operation tracking are O(1) and well-benchmarked by the .NET runtime itself.

### 2. Long-Running Workflow Nature

BenchmarkDotNet is designed for operations completing in microseconds to milliseconds. Resharding phases operate on timescales of seconds to hours:
- Copy phase: batch iteration with database I/O per batch
- Replication phase: CDC convergence polling with configurable thresholds
- Verification phase: full data consistency checks across shards
- Cutover phase: topology swap with replication lag verification

Benchmarking these operations with mocked dependencies would only measure mock object overhead, providing no actionable performance insights.

### 3. Record Type Allocation is Negligible

The resharding record types (`ReshardingPlan`, `ReshardingProgress`, `ReshardingState`, etc.) are simple immutable records with no computed properties beyond `PhaseHistoryEntry.Duration` and `ReshardingResult.IsSuccess`. Their allocation cost is trivially small compared to the I/O operations they support.

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify correctness of all orchestrator logic, phase execution, state transitions
- **Guard Tests**: Ensure parameter validation across all public APIs
- **Property Tests**: Verify record equality semantics and computed property invariants
- **Contract Tests**: Confirm interface consistency across all resharding interfaces
- **Integration Tests**: Validate real workflow execution including state persistence

### 5. Recommended Alternative

If performance optimization is needed in the future, benchmark:
1. **State serialization**: If `ReshardingState` is serialized to JSON for persistence, benchmark serialization/deserialization throughput
2. **Batch size optimization**: Run real-database benchmarks with varying `CopyBatchSize` values to find the optimal balance between throughput and memory
3. **Progress calculation**: If `CalculateOverallPercent` in `CopyingPhase` becomes a bottleneck in high-row-count scenarios, benchmark the arithmetic

These would be targeted micro-benchmarks, not full workflow benchmarks.

## Related Files

- `src/Encina/Sharding/Resharding/ReshardingOrchestrator.cs` - Orchestrator coordination
- `src/Encina/Sharding/Resharding/Phases/` - Phase implementations
- `src/Encina/Sharding/Resharding/` - Record types
- `tests/Encina.UnitTests/Sharding/Resharding/` - Unit tests
- `tests/Encina.PropertyTests/Database/Sharding/ReshardingPropertyTests.cs` - Property tests

## Date: 2026-02-16
## Issue: #648
