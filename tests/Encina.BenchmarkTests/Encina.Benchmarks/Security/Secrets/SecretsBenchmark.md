# BenchmarkTests - Security Secrets Management

## Status: Not Implemented

## Justification

### 1. Not a Hot Path
Secret retrieval is a startup/rotation operation (minutes/hours frequency). After initial load, secrets are cached in `IKeyProvider` and served from memory. The hot path is the cache lookup, not the secret provider.

### 2. External Provider Latency Dominates
Any benchmark would measure network latency to the cloud vault (Azure KV, AWS SM, etc.), not Encina code performance. Network benchmarks are not reproducible or meaningful for library optimization.

### 3. Adequate Coverage from Other Test Types
- **Unit Tests**: Cover provider logic, error handling, caching behavior
- **Integration Tests**: Cover cloud emulator connectivity (to be created)
- **Guard Tests**: Verify parameter validation

### 4. Recommended Alternative
If `InMemoryKeyProvider` lookup performance matters, benchmark `IKeyProvider.GetKeyAsync` — but this is a dictionary lookup (O(1)), unlikely to be a bottleneck.

## Related Files
- `src/Encina.Security.Secrets/` — Source
- `tests/Encina.UnitTests/Security/Secrets/` — Unit tests

## Date: 2026-03-15
