# LoadTests - Security Field-Level Encryption

## Status: Not Implemented

## Justification

### 1. Stateless Transformation
Field-level encryption (`IFieldEncryptor`) is a stateless transformation — it receives plaintext, returns ciphertext (and vice versa). There is no shared state, no connection pool, no concurrent resource contention. Load testing stateless functions measures only CPU throughput, which is better captured by BenchmarkDotNet.

### 2. No Concurrency Concerns
Each encryption operation is independent. No locks, no shared buffers, no connection limits. AES-256 is thread-safe by design (each call creates its own cipher instance).

### 3. Adequate Coverage from Other Test Types
- **Unit Tests**: Cover all encryption/decryption paths, key rotation, error handling
- **Guard Tests**: Verify null/invalid parameter rejection
- **Property Tests**: Verify encrypt-then-decrypt roundtrip invariant
- **Benchmark Tests**: Measure actual crypto performance (AES throughput, key sizes)

### 4. Recommended Alternative
If load testing becomes needed (e.g., bulk field encryption under memory pressure), use NBomber with configurable concurrency levels targeting the `IFieldEncryptor` interface.

## Related Files
- `src/Encina.Security.Encryption/` — Source
- `tests/Encina.UnitTests/Security/Encryption/` — Unit tests
- `tests/Encina.BenchmarkTests/Encina.Benchmarks/Security/Encryption/` — Benchmarks (to be created)

## Date: 2026-03-15
