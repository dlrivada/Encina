# BenchmarkTests - DataSubjectRights

## Status: Not Implemented

## Justification

### 1. Event-Sourced Architecture Eliminates In-Memory Store Benchmarks
The DSR module was migrated from entity-based persistence to Marten event sourcing (Issue #778). The previous benchmarks targeted `InMemoryDSRRequestStore` and `InMemoryDSRAuditStore`, which no longer exist. Benchmarking the event-sourced model requires Marten infrastructure.

### 2. Aggregate Operations Are CPU-Trivial
The DSRRequestAggregate performs simple property assignments and status checks — these are O(1) operations that don't benefit from micro-benchmarking. The performance bottleneck is in Marten's event persistence, not in the aggregate logic.

### 3. Adequate Coverage from Other Test Types
- **Unit Tests**: Full lifecycle coverage (DSRRequestAggregateTests, DefaultDSRServiceTests)
- **Property Tests**: Invariant verification with random data (DSRRequestAggregatePropertyTests)

### 4. Recommended Alternative
If performance analysis is needed, benchmark at the Marten integration level:
- Event append throughput (events/sec)
- Projection rebuild duration
- HasActiveRestriction query with varying dataset sizes

## Related Files
- `src/Encina.Compliance.DataSubjectRights/Aggregates/DSRRequestAggregate.cs`
- `src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs`

## Date: 2026-03-15
## Issue: #778
