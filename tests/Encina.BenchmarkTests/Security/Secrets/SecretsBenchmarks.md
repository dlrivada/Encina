# BenchmarkTests - Security Secrets Management

## Status: Not Implemented

## Justification

### 1. Cached Reads Dominate
The `CachedSecretReaderDecorator` caches secret values with configurable TTL. After the first read, subsequent calls hit the in-memory cache (O(1) dictionary lookup). Benchmarking a dictionary lookup provides no actionable insight.

### 2. Provider Latency Is External
Secret retrieval latency is dominated by the cloud provider (Azure Key Vault ~50-200ms, AWS Secrets Manager ~30-150ms, HashiCorp Vault ~10-50ms). These are network I/O operations that BenchmarkDotNet cannot meaningfully measure.

### 3. Infrequent Operations
Secret reads occur at startup or on cache miss. Secret writes and rotations are rare administrative operations. Neither is a "hot path" that benefits from micro-benchmarking.

### 4. Adequate Coverage from Other Test Types
- **Unit Tests**: Cover all decorators (Cached, Audited, Resilient), failover logic, rotation coordination
- **Property Tests**: Verify invariants (secret never in logs, rotation continuity)
- **Integration Tests**: Cover ConfigurationSecretProvider and EnvironmentSecretProvider

### 5. Recommended Alternative
For production performance monitoring, use the built-in `SecretsMetrics` (OpenTelemetry counters/histograms) which track real-world cache hit rates and provider latencies.

## Related Files
- `src/Encina.Security.Secrets/` — Source
- `tests/Encina.UnitTests/Security/Secrets/` — Unit tests
- `tests/Encina.PropertyTests/Security/Secrets/` — Property tests

## Date: 2026-03-19
## Issue: #797
