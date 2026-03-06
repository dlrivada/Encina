# Load Tests - Marten GDPR Crypto-Shredding

## Status: Not Implemented

## Justification

Load tests are not implemented for the crypto-shredding feature because the performance characteristics are dominated by CPU-bound cryptographic operations rather than concurrency patterns.

### 1. Stateless Serializer Design

The `CryptoShredderSerializer` is a stateless decorator over Marten's `ISerializer`. Each serialization call is independent — there is no shared mutable state, no connection pooling, and no resource contention points that would benefit from load testing. Thread safety is guaranteed by the stateless design.

### 2. Cryptographic Performance Is Already Benchmarked

The core performance-critical operation — AES-256-GCM encryption/decryption — is already covered by:

- `Encina.Security.Encryption.Benchmarks` — measures raw encryption throughput
- `Encina.Marten.GDPR.Benchmarks/CryptoShredderSerializerBenchmarks.cs` — measures serializer overhead (plain vs encrypted)
- `Encina.Marten.GDPR.Benchmarks/SubjectKeyProviderBenchmarks.cs` — measures key lookup/creation throughput

### 3. No Concurrency Bottlenecks

The `InMemorySubjectKeyProvider` uses `ConcurrentDictionary` with per-subject `Lock` for thread safety. This is a well-understood concurrency pattern that doesn't require load testing to validate. The `PostgreSqlSubjectKeyProvider` delegates to Marten's `IDocumentSession`, whose concurrency behavior is already tested by Marten's own test suite.

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

- `src/Encina.Marten.GDPR/Serialization/CryptoShredderSerializer.cs` — Stateless serializer decorator
- `src/Encina.Marten.GDPR/KeyStore/InMemorySubjectKeyProvider.cs` — ConcurrentDictionary-based key store
- `tests/Encina.BenchmarkTests/Encina.Marten.GDPR.Benchmarks/` — Micro-benchmarks

## Date: 2026-03-05
## Issue: #322
