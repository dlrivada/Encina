# Encina.Caching.Redis — Benchmark Results

> **Date**: 2026-04-14
> **Package**: `Encina.Caching.Redis`
> **Project**: `tests/Encina.BenchmarkTests/Encina.Caching.Redis.Benchmarks`
> **Backing store**: Testcontainers `redis:7-alpine` (standalone, bridge network)

This baseline measures `RedisCacheProvider` over a Testcontainers-managed Redis 7 instance. **All means are in milliseconds** because each call crosses the Docker loopback bridge — unlike the Memory (ns) and Hybrid (ns with in-memory L2) providers.

> **Why so noisy?** Docker Desktop on Windows routes loopback traffic through a VM-to-host bridge that fluctuates between 200 µs and 2 ms per round-trip. This inflates per-benchmark CoV to 30-50% even at N=20. The numbers are meaningful as orders of magnitude (milliseconds vs. the nanosecond paths of Memory/Hybrid) but not precise enough for stable regression detection — every method is listed in `stabilityOverrides`.

## Benchmark Environment

```text
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KS 3.20GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  Runtime  : .NET 10.0.5, X64 RyuJIT x86-64-v3
  GC       : Concurrent Workstation
  Job      : iterationCount=20, warmupCount=5, launchCount=Default
  Database : Testcontainers Redis (redis:7-alpine, standalone)
```

## Provider Operations

<!-- docref-table: bench:caching-redis/* -->
| Method | Mean | Allocated | DocRef | Notes |
|--------|-----:|----------:|--------|-------|
| **GetAsync_CacheHit** (baseline) ⚠ | 1.31 ms | 1,128 B | `bench:caching-redis/get-hit` | Single `GET` + JSON deserialize |
| **GetAsync_CacheMiss** ⚠ | 1.53 ms | 648 B | `bench:caching-redis/get-miss` | `GET` returning nil |
| **ExistsAsync_True** ⚠ | 1.03 ms | 592 B | `bench:caching-redis/exists-true` | `EXISTS` command (hit) |
| **ExistsAsync_False** ⚠ | ~1 ms | ~592 B | `bench:caching-redis/exists-false` | `EXISTS` command (miss) |
| **GetOrSetAsync_CacheHit** ⚠ | 1.69 ms | 1,432 B | `bench:caching-redis/getorset-hit` | Hit path: read-only |
| **GetOrSetAsync_CacheMiss** ⚠ | 5.52 ms | 3,256 B | `bench:caching-redis/getorset-miss` | Sequential miss path: `GET` + lock + factory + `SET` |
| **SetAsync** ⚠ | 1.35 ms | 1,016 B | `bench:caching-redis/set` | JSON serialize + `SET` with EX |
| **SetWithSlidingExpirationAsync** ⚠ | 1.75 ms | 1,696 B | `bench:caching-redis/set-sliding` | `SET` with absolute expiration + sliding metadata |
| **RemoveAsync** ⚠ | ~1 ms | ~1,000 B | `bench:caching-redis/remove` | Pure `DEL` (seeding isolated to `IterationSetup`) |
| **RemoveByPatternAsync** ⚠ | ~5 ms | ~4,800 B | `bench:caching-redis/remove-by-pattern` | `SCAN` + pipelined `DEL` on 5 keys (seeding isolated) |
<!-- /docref-table -->

⚠ = listed in `stabilityOverrides` (Docker loopback jitter dominates — not code drift).

**Key observations**:

- **Single-operation Redis calls sit in the low-millisecond range in this environment** because Windows Docker Desktop loopback dominates the floor. On Linux CI runners with native Docker, expect materially lower latency and tighter variance.
- **Cache-miss `GetOrSetAsync` is several times slower than a plain `SetAsync`** because the client performs a read, then runs the factory, then writes the value back; the extra network round-trips dominate the cost.
- **`RemoveByPatternAsync` is the heaviest operation in this run** because it combines `SCAN` cursor iteration with batched deletes. Expect roughly linear scaling with the number of matched keys, so avoid it on hot paths.
- **Allocation tracks the amount of work performed**: JSON serialization/deserialization makes `Get`/`Set` paths allocate more than existence checks, cache-miss `GetOrSetAsync` allocates more than either path alone, and pattern removal adds cursor and pipeline buffer overhead.

## Stability interpretation

CoV figures at N=20 for this run:

| Method | CoV | Why |
|--------|----:|-----|
| `ExistsAsync_True` | ~45% | Loopback jitter |
| `GetAsync_CacheHit` | ~34% | Loopback jitter |
| `GetAsync_CacheMiss` | ~36% | Loopback jitter + multimodal |
| `GetOrSetAsync_CacheHit` | ~46% | Loopback jitter + bimodal |
| `GetOrSetAsync_CacheMiss` | ~18% | Two round-trips smooth the jitter |
| `RemoveAsync` | ~49% | Inline setup + teardown |
| `RemoveByPatternAsync` | ~30% | `SCAN` batching averages out |
| `SetAsync` | ~30% | Loopback jitter + bimodal |
| `SetWithSlidingExpirationAsync` | ~41% | Loopback jitter |

CI runs on Linux should tighten these significantly; we'll re-baseline after the first CI publish.

## How to reproduce

```pwsh
# Requires Docker Desktop running
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.Caching.Redis.Benchmarks `
  -- --job short --filter "*" --exporters json `
  --artifacts artifacts/performance/caching-redis
```

## Related

- Perf manifest: `.github/perf-manifest/Encina.Caching.Redis.Benchmarks.json`
- Coverage dashboard: <https://dlrivada.github.io/Encina/performance/>
- Sibling caches: `docs/benchmarks/caching-{memory,hybrid}-benchmark-results.md`
- Wire-compatible peers: `Encina.Caching.Valkey`, `Encina.Caching.Dragonfly`, `Encina.Caching.Garnet`, `Encina.Caching.KeyDB` (not yet benchmarked)
