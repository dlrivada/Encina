# Encina.Caching.Hybrid — Benchmark Results

> **Date**: 2026-04-14
> **Package**: `Encina.Caching.Hybrid`
> **Project**: `tests/Encina.BenchmarkTests/Encina.Caching.Hybrid.Benchmarks`
> **Backing store**: `Microsoft.Extensions.Caching.Hybrid.HybridCache` (.NET 10) — in-memory L1 + `AddDistributedMemoryCache()` as L2 stub

This baseline measures `HybridCacheProvider` over the .NET 10 `HybridCache`. Because the benchmark runs with the in-memory distributed cache stub (no network), all numbers reflect the provider's CPU cost plus `HybridCache`'s two-tier plumbing — not L2 transport latency.

## Benchmark Environment

```text
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KS 3.20GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  Runtime : .NET 10.0.5, X64 RyuJIT x86-64-v3
  GC      : Concurrent Workstation
  Job     : iterationCount=20, warmupCount=5, launchCount=Default
```

## Provider Operations

<!-- docref-table: bench:caching-hybrid/* -->
| Method | Mean | Allocated | DocRef | Notes |
|--------|-----:|----------:|--------|-------|
| **GetAsync_CacheHit** (baseline) | 403 ns | 496 B | `bench:caching-hybrid/get-hit` | HybridCache state-machine + serializer plumbing |
| **GetAsync_CacheMiss** | 384 ns | 616 B | `bench:caching-hybrid/get-miss` | Miss path still allocates async state |
| **ExistsAsync_True** | 393 ns | 616 B | `bench:caching-hybrid/exists-true` | Wrapper around GetAsync |
| **ExistsAsync_False** | 380 ns | 616 B | `bench:caching-hybrid/exists-false` | Wrapper around GetAsync |
| **GetOrSetAsync_CacheHit** | 413 ns | 560 B | `bench:caching-hybrid/getorset-hit` | Hit path skips factory |
| **GetOrSetAsync_CacheMiss** | 2,643 ns | 1,792 B | `bench:caching-hybrid/getorset-miss` | Factory invoke + L1+L2 populate |
| **GetOrSetAsync_WithTags** | 5,127 ns | 2,216 B | `bench:caching-hybrid/getorset-tags` | Adds tag-index bookkeeping |
| **SetAsync** | 1,680 ns | 1,064 B | `bench:caching-hybrid/set` | L1+L2 write |
| **SetWithSlidingExpirationAsync** | 1,747 ns | 1,134 B | `bench:caching-hybrid/set-sliding` | Sliding window on top of Set |
| **RemoveAsync** | 763 ns | 1,136 B | `bench:caching-hybrid/remove` | L1+L2 delete |
| **RemoveByTagAsync** | 3,634 ns | 2,448 B | `bench:caching-hybrid/remove-by-tag` | Tag lookup + fan-out removal |
<!-- /docref-table -->

**Key observations**:

- **Every read is ~10x the cost of `Encina.Caching.Memory`** — 400 ns vs. 33 ns. The overhead is the `HybridCache` state machine: async task allocation, serializer lookup, tier-coordination logic. On an in-process L2 this is pure overhead; with a real Redis/Garnet L2 the ~400 ns is dwarfed by network latency.
- **Cache misses cost more than hits** (616 B vs. 496 B) because the miss path allocates state to continue down to L2 before returning empty.
- **Tagged writes (`GetOrSetAsync_WithTags`) cost ~3x an untagged miss** (5.1 µs vs. 2.6 µs, 2.2 KB vs. 1.8 KB). Tag-index maintenance is not free.
- **`RemoveByTagAsync` has to traverse the tag index** and issue a removal per matching key; 3.6 µs / 2.4 KB for a single tagged item is the floor — expect linear growth with tag cardinality.
- **All benchmarks passed the two-tier stability rule** (CoV ≤ 10% at N=20). No `stabilityOverrides` needed for Hybrid on this workload.

## How to reproduce

```pwsh
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.Caching.Hybrid.Benchmarks `
  -- --job short --filter "*" --exporters json `
  --artifacts artifacts/performance/caching-hybrid
```

## Related

- Perf manifest: `.github/perf-manifest/Encina.Caching.Hybrid.Benchmarks.json`
- Coverage dashboard: <https://dlrivada.github.io/Encina/performance/>
- Sibling caches: `docs/benchmarks/caching-{memory,redis}-benchmark-results.md`
