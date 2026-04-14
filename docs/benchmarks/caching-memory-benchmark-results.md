# Encina.Caching.Memory — Benchmark Results

> **Date**: 2026-04-14
> **Package**: `Encina.Caching.Memory`
> **Project**: `tests/Encina.BenchmarkTests/Encina.Caching.Memory.Benchmarks`
> **Backing store**: `Microsoft.Extensions.Caching.Memory.MemoryCache` (in-process L1)

This baseline measures the `MemoryCacheProvider` wrapper around the built-in `IMemoryCache`. All operations are CPU-bound and complete inside the process — no network, no serialization.

## Benchmark Environment

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KS 3.20GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  Runtime : .NET 10.0.5, X64 RyuJIT x86-64-v3
  GC      : Concurrent Workstation
  Job     : iterationCount=20, warmupCount=5, launchCount=Default
```

## Provider Operations

<!-- docref-table: bench:caching-memory/* -->
| Method | Mean | Allocated | DocRef | Notes |
|--------|-----:|----------:|--------|-------|
| **GetAsync_CacheHit** (baseline) | 33 ns | 144 B | `bench:caching-memory/get-hit` | Dictionary lookup + boxed-value unwrap |
| **GetAsync_CacheMiss** | 24 ns | 0 B | `bench:caching-memory/get-miss` | Fast-path miss, no allocation |
| **ExistsAsync_True** | 25 ns | 0 B | `bench:caching-memory/exists-true` | Dictionary contains-key |
| **ExistsAsync_False** | 24 ns | 0 B | `bench:caching-memory/exists-false` | Dictionary miss |
| **GetOrSetAsync_CacheHit** | 46 ns | 280 B | `bench:caching-memory/getorset-hit` | Hit path skips factory |
| **GetOrSetAsync_CacheMiss** ⚠ | 2,701 ns | 1,080 B | `bench:caching-memory/getorset-miss` | Invokes factory + populates cache (bimodal) |
| **SetAsync** ⚠ | 1,919 ns | 712 B | `bench:caching-memory/set` | Entry creation + eviction-callback registration (bimodal) |
| **SetWithSlidingExpirationAsync** ⚠ | 1,973 ns | 801 B | `bench:caching-memory/set-sliding` | Timer-backed sliding window |
| **RemoveAsync** | 708 ns | 784 B | `bench:caching-memory/remove` | Entry lookup + eviction callback |
<!-- /docref-table -->

⚠ = listed in `stabilityOverrides` (inherent variance — see manifest).

**Key observations**:

- **Hot paths stay below 50 ns** — `GetAsync_CacheHit`, `ExistsAsync*`, `GetOrSetAsync_CacheHit` are all cheaper than a typical `Dictionary` allocation. Reads are dominated by the provider's own `ValueTask<T?>` plumbing, not the underlying cache.
- **`GetAsync_CacheMiss` allocates 0 bytes** — the miss path returns `default(T)` without wrapping.
- **Writes cost ~2 µs and ~700-1,100 B** regardless of whether they set, set-with-sliding, or populate on miss. The cost is in the `MemoryCacheEntry` + eviction token registration that `IMemoryCache` forces, not in the Encina wrapper.
- **Three write benchmarks are marked as expected-unstable**: `SetAsync` and `GetOrSetAsync_CacheMiss` are flagged bimodal by BDN's MultimodalDistribution detector; `SetWithSlidingExpirationAsync` is unstable because the timer-backed window introduces real scheduler noise. Numbers are meaningful for order-of-magnitude comparisons but not stable for regression detection.

## How to reproduce

```pwsh
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.Caching.Memory.Benchmarks `
  -- --job short --filter "*" --exporters json `
  --artifacts artifacts/performance/caching-memory
```

## Related

- Perf manifest: `.github/perf-manifest/Encina.Caching.Memory.Benchmarks.json`
- Coverage dashboard: <https://dlrivada.github.io/Encina/performance/>
- Sibling caches: `docs/benchmarks/caching-{hybrid,redis}-benchmark-results.md`
