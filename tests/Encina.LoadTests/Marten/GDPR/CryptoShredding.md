# Load Tests - Marten GDPR Crypto-Shredding

## Status: Not Implemented

## Justification

A load test is not implemented yet; it is tracked by #1769. Until it lands, concurrency is covered by the unit test below.

### 1. Per-Call Frames, Not a Stateless Decorator

Since #1698 the `CryptoShredderSerializer` installs a System.Text.Json contract modifier and is no longer stateless. Each serializer call pushes a `CryptoShreddingFrame` that holds its key caches, a lazily created DI scope and the owners waiting for decryption. Write frames are `[ThreadStatic]`, read frames and the locator's subject filter are `AsyncLocal`, buffer-writer output is staged in a `[ThreadStatic]` buffer, and the type classification caches are static `ConcurrentDictionary` instances. Frames are never shared across calls, so a key erased by Art. 17 is never served from a cache.

The isolation of these frames under concurrency is covered by `tests/Encina.UnitTests/Marten/GDPR/Nested/CryptoShreddingCallScopeAndDomainOwnerTests.cs` (`ParallelReadsAndWrites_NeverBleedKeysOrFramesAcrossCalls`): 32 parallel tasks write and asynchronously read nested documents of 4 subjects through one serializer and assert that every call reads back its own value and that no frame is left on the thread or the async flow. The NBomber load test with sustained parallel writers and readers is #1769.

### 2. Cryptographic Performance Is Already Benchmarked

The core performance-critical operation — AES-256-GCM encryption/decryption — is already covered by:

- `Encina.Security.Encryption.Benchmarks` — measures raw encryption throughput
- `Encina.Marten.GDPR.Benchmarks/CryptoShredderSerializerBenchmarks.cs` — measures serializer overhead (plain vs encrypted)
- `Encina.Marten.GDPR.Benchmarks/SubjectKeyProviderBenchmarks.cs` — measures key lookup/creation throughput

### 3. No Concurrency Bottlenecks

The `InMemorySubjectKeyProvider` uses `ConcurrentDictionary` with per-subject `Lock` for thread safety. This is a well-understood concurrency pattern that doesn't require load testing to validate. The `PostgreSqlSubjectKeyProvider` serializes key creation, rotation and erasure per subject with a PostgreSQL transaction-scoped advisory lock and insert semantics (#1699); its races (concurrent first writers, concurrent rotations, writers racing an erasure, insert conflicts) are proven by `PostgreSqlSubjectKeyProviderConcurrencyIntegrationTests` against a real PostgreSQL instance, and the in-memory provider's by its unit and property tests.

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: 10 test files covering all components in isolation
- **Guard Tests**: 3 files verifying parameter validation
- **Property Tests**: FsCheck-based invariant verification (roundtrip, forget, rotation)
- **Contract Tests**: 2 files verifying interface contracts
- **Integration Tests**: 4 files testing full end-to-end flows with real PostgreSQL
- **Benchmark Tests**: 2 files measuring serializer overhead and key provider throughput

### 5. Recommended Alternative

If load testing becomes necessary (e.g., to validate throughput under sustained event ingestion), the recommended approach would be an NBomber scenario that:

1. Creates a Marten store with `CryptoShredderSerializer`
2. Appends events with PII at a target rate (e.g., 1000 events/sec)
3. Measures serialization latency percentiles (p50, p95, p99)
4. Validates that crypto overhead stays below 5ms per event

This would be more valuable as a system-level benchmark rather than an isolated load test.

## Related Files

- `src/Encina.Marten.GDPR/Serialization/CryptoShredderSerializer.cs` — Serializer wrapper with the contract modifier
- `src/Encina.Marten.GDPR/Serialization/CryptoShreddingCallScope.cs` — Per-call frames ([ThreadStatic] write, AsyncLocal read)
- `tests/Encina.UnitTests/Marten/GDPR/Nested/CryptoShreddingCallScopeAndDomainOwnerTests.cs` — Parallel frame-isolation test
- `src/Encina.Marten.GDPR/KeyStore/InMemorySubjectKeyProvider.cs` — ConcurrentDictionary-based key store
- `tests/Encina.BenchmarkTests/Encina.Marten.GDPR.Benchmarks/` — Micro-benchmarks

## Date: 2026-10-05
## Issue: #322, #1698 (load test: #1769)
