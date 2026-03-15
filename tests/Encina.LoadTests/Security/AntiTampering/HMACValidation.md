# LoadTests - Security HMAC Validation

## Status: Not Implemented

## Justification

### 1. Atomic Check-and-Set Pattern
HMAC validation is a two-step atomic operation: (1) check nonce uniqueness, (2) verify HMAC signature. The nonce store uses `TryAddAsync` which is atomic by design. There is no complex concurrent state management.

### 2. Nonce Store Already Handles Concurrency
`InMemoryNonceStore` uses `ConcurrentDictionary` (thread-safe). `DistributedCacheNonceStore` delegates to `IDistributedCache` which handles its own concurrency. Load testing would measure the cache provider, not the anti-tampering logic.

### 3. Adequate Coverage from Other Test Types
- **Unit Tests**: Cover validation pipeline, nonce rejection, timestamp tolerance, signature verification
- **Integration Tests**: Cover DistributedCacheNonceStore against real cache
- **Property Tests**: Verify "replay always rejected" and "valid signature always accepted" invariants
- **Benchmark Tests**: Measure HMAC-SHA256/384/512 computation speed (to be created)

### 4. Recommended Alternative
If nonce store contention under high load is a concern, benchmark the `DistributedCacheNonceStore.TryAddAsync` throughput with BenchmarkDotNet against a real Redis instance.

## Related Files
- `src/Encina.Security.AntiTampering/` — Source
- `tests/Encina.UnitTests/Security/AntiTampering/` — Unit tests

## Date: 2026-03-15
