# ContractTests - Security AntiTampering

## Status: Not Implemented

## Justification

### 1. Minimal Interface Surface
`INonceStore` has only 2 methods (`TryAddAsync`, `ExistsAsync`) with straightforward semantics. The contract is simple enough that unit tests provide complete behavioral coverage.

### 2. Limited Implementations
Only 2 implementations exist: `InMemoryNonceStore` (testing) and `DistributedCacheNonceStore` (production). Both have dedicated unit tests verifying the same behavior.

### 3. Adequate Coverage from Other Test Types
- **Unit Tests**: Cover both implementations with identical test scenarios
- **Property Tests**: Verify "added nonce always exists" and "expired nonce never exists" invariants
- **Integration Tests**: Cover DistributedCacheNonceStore against real cache

### 4. Recommended Alternative
If additional implementations are added (e.g., Redis-native nonce store), create a shared contract test base class that all implementations must pass.

## Related Files
- `src/Encina.Security.AntiTampering/Abstractions/INonceStore.cs` — Interface
- `tests/Encina.UnitTests/Security/AntiTampering/` — Unit tests

## Date: 2026-03-15
