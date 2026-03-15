# BenchmarkTests - CrossBorderTransfer

## Status: Not Implemented

## Justification

### 1. Not a Hot Path
Cross-border transfer validation typically runs once per request via pipeline behavior. It is not a tight inner loop or frequently-called utility. The overhead of the validation chain (adequacy check → approved transfer check → SCC check → TIA check) is dominated by I/O to the event store and cache, not CPU computation.

### 2. Risk Assessment Is Simple Heuristic
The `DefaultTIARiskAssessor` uses HashSet lookups and simple arithmetic. These operations are O(1) and do not benefit from micro-benchmarking. Any real-world latency comes from the aggregate repository (network I/O), not the risk calculation.

### 3. Adequate Coverage from Other Test Types
- **Unit Tests** (7 files): Verify correctness of all computation paths
- **Guard Tests** (7 files): Verify parameter validation for all public methods
- **Property Tests** (6 files): Verify invariants hold for varied inputs (FsCheck)
- **Contract Tests** (4 files): Verify interface contracts and DI registration
- **Integration Tests** (2 files): Verify real Marten/PostgreSQL interactions — aggregate persistence, full service lifecycle, DI registration

### 4. Recommended Alternative
If benchmarking is needed in the future, focus on:
- Attribute reflection caching in `TransferBlockingPipelineBehavior` (ConcurrentDictionary lookup performance)
- Cache hit vs miss latency in transfer validation
- Marten event stream loading performance for large aggregates

## Related Files
- `src/Encina.Compliance.CrossBorderTransfer/` - Source files
- `tests/Encina.UnitTests/Compliance/CrossBorderTransfer/` - Unit tests
- `tests/Encina.GuardTests/Compliance/CrossBorderTransfer/` - Guard tests
- `tests/Encina.PropertyTests/Compliance/CrossBorderTransfer/` - Property tests
- `tests/Encina.ContractTests/Compliance/CrossBorderTransfer/` - Contract tests
- `tests/Encina.IntegrationTests/Compliance/CrossBorderTransfer/` - Integration tests (Marten/PostgreSQL)

## Date: 2026-03-14
## Issue: #412
