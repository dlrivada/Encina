# LoadTests - DataSubjectRights

## Status: Not Implemented

## Justification

### 1. Event-Sourced Architecture Eliminates In-Memory Store Load Testing
The DSR module was migrated from multi-provider entity-based persistence to Marten event sourcing (Issue #778). The previous load tests targeted `InMemoryDSRRequestStore` and `InMemoryDSRAuditStore`, which no longer exist. Load testing for the event-sourced architecture requires a running PostgreSQL + Marten infrastructure, which belongs in integration tests.

### 2. Marten Event Store Performance Is Vendor-Validated
Marten's event store is optimized for append-only workloads with well-documented performance characteristics. Load testing Marten internals provides minimal value over Marten's own benchmarks.

### 3. Adequate Coverage from Other Test Types
- **Unit Tests**: Full aggregate state machine coverage (DSRRequestAggregateTests)
- **Guard Tests**: Parameter validation for aggregate and service (DSRRequestAggregateGuardTests, DefaultDSRServiceGuardTests)
- **Property Tests**: Domain invariants verified with random data (DSRRequestAggregatePropertyTests)
- **Contract Tests**: IDSRService behavioral contracts (IDSRServiceContractTests)

### 4. Recommended Alternative
When Marten integration tests are established, create Marten-specific load tests exercising:
- Concurrent aggregate creation throughput
- Read model projection rebuild performance
- HasActiveRestriction query latency under load

## Related Files
- `src/Encina.Compliance.DataSubjectRights/Aggregates/DSRRequestAggregate.cs`
- `src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs`
- `tests/Encina.UnitTests/Compliance/DataSubjectRights/Aggregates/DSRRequestAggregateTests.cs`

## Date: 2026-03-15
## Issue: #778
