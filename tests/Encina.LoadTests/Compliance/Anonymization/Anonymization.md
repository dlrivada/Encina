# LoadTests - Compliance Anonymization (Core Module)

## Status: Not Implemented

## Justification

The core Anonymization module (`Encina.Compliance.Anonymization`) operates entirely in-memory using thread-safe data structures and CPU-bound cryptographic operations. Load testing this module would not yield meaningful insights beyond what unit tests already provide.

### 1. Thread-Safe by Design

The in-memory stores (`InMemoryAnonymizationRuleStore`, `InMemoryTokenMappingStore`, `InMemoryAnonymizationAuditStore`) use `ConcurrentDictionary<TKey, TValue>` internally. This collection is specifically designed for high-concurrency scenarios and is extensively tested by the .NET runtime team. Load testing would effectively stress-test `ConcurrentDictionary`, not Encina code.

### 2. No Connection or Transaction Management

Unlike database-backed stores (where connection pooling, transaction coordination, and deadlocks are real concerns under load), the core module has:

- No database connections to pool or exhaust
- No transactions to coordinate or deadlock
- No I/O-bound operations that could bottleneck
- No network calls that could timeout or fail under pressure

Load tests are most valuable when they reveal resource contention issues. The core module has no shared external resources to contend over.

### 3. CPU-Bound Cryptographic Operations

The anonymization strategies (AES-256-GCM encryption, HMAC-SHA256 tokenization, SHA-256 hashing) are CPU-bound operations using .NET BCL implementations. These operations:

- Scale linearly with CPU cores (no shared state between operations)
- Have well-documented performance characteristics from Microsoft
- Do not exhibit degradation under concurrent access (each operation is independent)

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: Cover concurrent access scenarios with explicit multi-threaded test cases, verify all anonymization strategies produce correct output, and test store operations under concurrent reads and writes
- **Guard Tests**: Verify all public methods reject null/invalid parameters correctly
- **Property Tests**: Verify anonymization invariants (reversibility of tokenization, consistency of hashing, uniqueness of encrypted outputs) across randomized inputs
- **Contract Tests**: Verify all `IAnonymizationStrategy` implementations satisfy the interface contract

### 5. Recommended Alternative

If load testing becomes necessary in the future (e.g., to measure throughput of cryptographic operations for capacity planning):

1. Use BenchmarkDotNet with `[ThreadingDiagnoser]` to measure contention
2. Create a simple console harness that runs N concurrent anonymization operations and measures throughput
3. Focus on the `AnonymizationService` orchestrator rather than individual stores

For database-backed `ITokenMappingStore` implementations, load testing should be done at the provider level (see `Phase8-TokenMappingStore.md`).

## Related Files

- `src/Encina.Compliance.Anonymization/` - Core anonymization module source
- `tests/Encina.UnitTests/Compliance/Anonymization/` - Unit tests including concurrency scenarios

## Date: 2026-02-28
## Issue: #407
