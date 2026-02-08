# LoadTests - EntityFrameworkCore Query Caching

## Status: Not Implemented

## Justification

The EF Core Query Caching interceptor (`QueryCacheInterceptor`) does not require dedicated load tests
for the following reasons:

### 1. Concurrency Is Managed by the Cache Provider

Cache stampede protection, concurrent read/write coordination, and throughput scaling are
responsibilities of the `ICacheProvider` implementations (Memory, Redis, Hybrid, etc.).
The interceptor is a thin orchestration layer that delegates all cache operations to the provider.

Load tests for cache providers already exist:
- `Encina.Caching.Benchmarks/QueryCachingPipelineBenchmarks.cs` — pipeline-level concurrency
- `Encina.Caching.Benchmarks/MemoryCacheProviderBenchmarks.cs` — provider throughput

### 2. EF Core Interceptor Is Single-Threaded per DbContext

EF Core `DbContext` is **not thread-safe** — each context instance handles one query at a time.
The interceptor's `ReaderExecuting`/`ReaderExecuted` callbacks are invoked sequentially per context,
making concurrent load testing on a single context invalid. Testing multiple contexts in parallel
would exercise the cache provider's concurrency, not the interceptor's.

### 3. Adequate Coverage from Other Test Types

- **Unit Tests** (184 tests): Full behavior coverage including error handling, cache hit/miss, invalidation
- **Guard Tests** (19 tests): All null parameter validation
- **Property Tests** (16 tests): Key determinism, uniqueness, format invariants
- **Contract Tests** (29 tests): API consistency, type hierarchy
- **Integration Tests**: End-to-end with real DbContext + Memory cache
- **Benchmarks**: Interceptor overhead measurement (key generation + cache access)

### 4. Recommended Alternative

If load testing of the query caching subsystem becomes necessary (e.g., under high QPS scenarios),
the recommended approach is to use the existing `Encina.LoadTests/Program.cs` harness with:

1. Multiple `DbContext` instances (one per worker thread)
2. A shared `ICacheProvider` (Redis or Memory)
3. Measure cache hit ratio and latency percentiles under sustained load

This would primarily validate the cache provider's behavior, which already has benchmarks.

## Related Files

- `src/Encina.EntityFrameworkCore/Caching/QueryCacheInterceptor.cs` — Source
- `tests/Encina.UnitTests/EntityFrameworkCore/Caching/QueryCacheInterceptorTests.cs` — Unit tests
- `tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks/QueryCachingPipelineBenchmarks.cs` — Pipeline benchmarks

## Date: 2026-02-08
## Issue: #291
