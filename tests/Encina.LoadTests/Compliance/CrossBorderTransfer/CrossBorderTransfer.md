# LoadTests - CrossBorderTransfer

## Status: Not Implemented

## Justification

### 1. Event-Sourced Architecture Limits Concurrency Testing Value
The CrossBorderTransfer module uses Marten event sourcing for aggregate persistence. Load testing event-sourced aggregates provides limited value because:
- Aggregate streams are inherently sequential (events append to a single stream)
- Concurrent writes to the same aggregate are handled by Marten's optimistic concurrency
- The actual load bottleneck is PostgreSQL, not the application layer

### 2. Pipeline Behavior Is Stateless
The `TransferBlockingPipelineBehavior` is a stateless pass-through that delegates to `ITransferValidator`. There is no shared state or connection pool to stress-test at the application layer.

### 3. Adequate Coverage from Other Test Types
- **Unit Tests**: Cover all service methods, aggregate state transitions, and error paths
- **Guard Tests**: Verify parameter validation for all public methods
- **Property Tests**: Verify aggregate invariants hold under varied inputs via FsCheck
- **Contract Tests**: Verify interface contracts and DI registration
- **Integration Tests**: Verify real Marten/PostgreSQL interactions

### 4. Recommended Alternative
If load testing is needed in the future, focus on:
- Marten event store throughput with concurrent aggregate creation
- Cache stampede scenarios under high concurrent transfer validation requests
- Pipeline behavior latency under sustained request throughput

## Related Files
- `src/Encina.Compliance.CrossBorderTransfer/` - Source files
- `tests/Encina.UnitTests/Compliance/CrossBorderTransfer/` - Unit tests
- `tests/Encina.PropertyTests/Compliance/CrossBorderTransfer/` - Property tests

## Date: 2026-03-14
## Issue: #412
