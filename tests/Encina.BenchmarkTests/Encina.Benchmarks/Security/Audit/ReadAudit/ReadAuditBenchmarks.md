# Benchmark Tests - Read Auditing

## Status: Not Implemented

## Justification

Read audit operations are not on the hot path. The audit recording happens as a
fire-and-forget side effect after the repository read has already returned data
to the caller. Micro-benchmarking the audit write path provides limited value
because the latency is dominated by database I/O, not CPU-bound code.

### 1. Fire-and-Forget Eliminates User-Facing Latency

The `AuditedRepository` decorator returns the read result immediately and logs
the audit entry asynchronously. The audit write time is never part of the user's
request latency, making micro-benchmarking of the audit path itself irrelevant
to user experience.

### 2. Database I/O Dominates Execution Time

Each `ReadAuditStore` implementation performs a single INSERT for `LogReadAsync`
and SELECT + COUNT queries for `QueryAsync`. The execution time is >99% database
I/O and <1% in-process code. BenchmarkDotNet measures in-process performance,
not database performance.

### 3. InMemoryReadAuditStore Is O(1) Amortized

The `ConcurrentDictionary` operations in `InMemoryReadAuditStore` are O(1) for
adds and O(n) for queries. This is standard .NET collection behavior and does
not warrant custom benchmarking.

### 4. Sampling Rate Reduces Volume

With per-entity sampling rates (typically 10-100%), the effective number of
audit writes is a fraction of total reads. The overhead per read operation is
a `Random.Shared.NextDouble()` comparison — nanosecond-level cost.

### 5. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify behavioral correctness of all store operations
- **Integration Tests**: Verify real database round-trip functionality
- **Property Tests**: Verify invariants with random data generation
- **Contract Tests**: Verify API consistency across all 10 providers

### 6. Recommended Alternative

If benchmarking becomes necessary (e.g., comparing providers for capacity planning),
use `BenchmarkSwitcher` with `--filter "*ReadAudit*"` and test against real
database backends, not in-memory stores.

## Related Files

- `src/Encina.Security.Audit/AuditedRepository.cs` — Fire-and-forget decorator
- `src/Encina.Security.Audit/InMemoryReadAuditStore.cs` — Thread-safe in-memory store
- `tests/Encina.UnitTests/Security/Audit/ReadAudit/` — Unit test coverage
- `tests/Encina.IntegrationTests/Security/Audit/ReadAudit/` — Integration test coverage

## Date: 2026-03-04
## Issue: #573
