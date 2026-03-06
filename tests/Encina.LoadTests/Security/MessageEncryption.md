# LoadTests - Message Encryption

## Status: Not Implemented

## Justification

Message encryption (`Encina.Messaging.Encryption`) does not have concurrency-sensitive coordination logic that would benefit from load testing.

### 1. No Concurrent State Management

The encryption components are stateless or thread-safe by design:

- `DefaultMessageEncryptionProvider`: Stateless — delegates to `IFieldEncryptor` and `IKeyProvider`
- `EncryptingMessageSerializer`: Stateless decorator — no shared mutable state
- `EncryptedPayloadFormatter`: Static pure functions — inherently thread-safe
- `EncryptedMessageAttributeCache`: Uses `ConcurrentDictionary` — already thread-safe
- `DefaultTenantKeyResolver`: Reads from immutable `IOptions<T>` — no locking needed

### 2. Load Behavior Determined by Underlying Components

The actual load characteristics are determined by:

- **IFieldEncryptor** (AES-256-GCM): CPU-bound, scales linearly with core count
- **IKeyProvider**: Key retrieval latency depends on the KMS backend (cloud KMS providers would need their own load tests)
- **Database persistence**: Load testing encryption within outbox/inbox stores is covered by the existing store load tests

### 3. Adequate Coverage from Other Test Types

- **Unit Tests** (62 tests): Concurrent behavior is not needed — operations are isolated per message
- **Benchmark Tests**: Measure per-operation latency and memory allocation at different payload sizes, which is more relevant than concurrent load for this module
- **Property Tests**: Verify invariants hold for randomly generated inputs

### 4. Recommended Alternative

If load testing becomes necessary:

- Add an NBomber scenario that simulates high-throughput message encryption/decryption
- Focus on throughput (messages/second) and latency distribution (p50, p95, p99)
- Test with realistic payload sizes from production telemetry
- Include both `EncryptAllMessages = true` and attribute-based encryption modes

## Related Files

- `src/Encina.Messaging.Encryption/` - Source package
- `tests/Encina.BenchmarkTests/Encina.Messaging.Encryption.Benchmarks/` - Benchmark tests
- `tests/Encina.UnitTests/Messaging/Encryption/` - Unit tests

## Date: 2026-03-06
## Issue: #129
