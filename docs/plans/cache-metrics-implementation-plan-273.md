# Implementation Plan: Cache Observability Gap (Request Outcomes by Request Type, Memory Evictions, Entry and Size Gauges)

> **Issue**: [#273](https://github.com/dlrivada/Encina/issues/273)
> **Type**: Feature (observability)
> **Related**: #723 (the sibling plan: provider decorator, `encina.cache.operations`, `encina.cache.operation.duration`, pub/sub decorator; this plan adds only what #723 does not), #266 (owns `CachePatternMetrics` and `encina.cache.stampede`), #268 (owns `encina.cache.failsafe`, stale served), #2408 (owns `encina.cache.invalidations`), #754 (provider health checks), #276 and #278 (diagnostics and auto-recovery, other issues), #2676 (tenant tag helper), #187 (dashboards), #906, #746, ADR-054 (D3, D4, D6.3), ADR-062
> **Complexity**: Medium (one counter in the pipeline behavior, one meter in the Memory provider; the weight is the boundary with #723, #266, #268 and #2408)
> **Estimated Scope**: ~280 lines of production code + ~700 lines of tests, in four pull requests of at most 15 files
> **Provider category**: Caching (8): the request-level counter sits in the pipeline behavior, above `ICacheProvider`, so it is identical on all 8 (Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB and the planned Memcached, #277); the process-local state metrics exist only where the process owns the state (Memory), see Design Choice 3.

---

## Summary

### What exists today (read on 2026-11-24, main `1a661f5c1`)

- `QueryCachingPipelineBehavior<TRequest, TResponse>` (`src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs`) already runs at every cache decision and **logs** them with `[LoggerMessage]`: hit (`LogCacheHit`, line 186), miss (`LogCacheMiss`, line 217), set (`LogCacheSet`, line 253), cache error (`LogCacheError`, lines 225 and 257) and the two bypasses (`LogTenantMissingBypassed`, `LogVaryByUserBypassed`, EventIds 3512 and 3513). It records no metric. The request type is `typeof(TRequest).Name`, a code identifier whose cardinality is the number of `[Cache]` request types, not data.
- No `Meter` exists in `Encina.Caching`, `Encina.Caching.Memory`, `Encina.Caching.Hybrid` or `Encina.Caching.Redis` (the only cache metrics today are `CacheInvalidationMetrics` in `Encina.Cdc` and `QueryCacheMetrics` in `Encina.EntityFrameworkCore`).
- `MemoryCacheProvider` (`src/Encina.Caching.Memory/MemoryCacheProvider.cs`) registers a post-eviction callback in three places (`SetAsync`, `GetOrSetAsync`, `SetWithSlidingExpirationAsync`) that only removes the key from its tracker; the eviction reason is received and ignored. `AddEncinaMemoryCache` calls `services.AddMemoryCache()` with default options (`TrackStatistics` false), so `IMemoryCache.GetCurrentStatistics()` returns `null` today. Encina's own `MemoryCacheOptions` (`DefaultExpiration`, in the same file) is a different type from `Microsoft.Extensions.Caching.Memory.MemoryCacheOptions`.
- `HybridCacheProvider.GetAsync` reads through `HybridCache.GetOrCreateAsync` with a captured factory flag (lines 57-78); the Microsoft API does not report whether a hit came from L1 or L2.
- The **#723 plan** (merged as a plan) puts the provider-level decorator in `Encina.OpenTelemetry`: `encina.cache.operations` (tags `encina.cache.operation` = `get`, `set`, `remove`, ...; `encina.cache.outcome` = `hit`, `miss`, `success`, `error`; `encina.cache.system`; `error.type`), `encina.cache.operation.duration` (`s`), span names `encina.cache.*` with a key prefix instead of the key, a pub/sub decorator with `encina.cache.pubsub.messages` (tag direction) and the tenant tag. Its Design Choice 4 leaves the rest of #273's catalog to this plan; its register row C4 (Hybrid L1 and L2 detail) says #273 decides it.
- `#266` plans `internal static partial class CachePatternMetrics` on `Meter("Encina.Caching", "1.0")` with `encina.cache.stampede`; `#267`, `#268`, `#269`, `#271` and `#278` add instruments in their own partial files (`encina.cache.failsafe`, `encina.cache.eager_refresh`, `encina.cache.circuit_state`, ...); `#2408` plans `encina.cache.invalidations`; `#275` plans `encina.cache.tenant.entries`.

### Issue item against plan

| Item of #273 | Where it is covered | What this plan does |
|--------------|---------------------|---------------------|
| `hits`, `misses`, `sets` by `request_type` | #723 counts operations per provider and system, not per request type | New counter `encina.cache.requests` in the behavior (tag `encina.cache.request_type`, outcome `hit`, `miss`, `stored`, `error`, `bypass`) |
| `removals`, `evictions` | #723 `operations` with `operation` = `remove`; #2408 `invalidations` | Memory evictions only (new counter, Design Choice 3); removals are not duplicated |
| `latency` | #723 `operation.duration` | Nothing |
| `value_size_bytes` | none | Not added (Design Choice 4) |
| `size_bytes`, `entry_count` gauges | none | Memory provider only (Design Choice 3) |
| `stampede_prevented`, `stale_served` | #266 `encina.cache.stampede`, #268 `encina.cache.failsafe` | Nothing |
| `backplane_messages` with `action` | #723 `encina.cache.pubsub.messages` (direction); invalidations by #2408 | Nothing: the action is payload semantics that the decorator never sees |
| Miss `reason` (expired, not_found) and `cache_level` (L1, L2) | none | Not emitted (register rows C3 and C4) |
| `ICacheMetrics`, `EnableMetrics`, `MetricsPrefix`, `RecordValueSizeHistogram`, `MetricsTags` | none | Design Choices 2 and 5 |
| Span attributes `cache.operation`, `cache.hit`, `cache.stale`, `cache.latency_ms`, `cache.key` | #723 spans (operation, outcome, key prefix, span duration); #268 for stale | Nothing; `cache.key` is never emitted (ADR-054 D6.3) |
| Health gauges and "health check based on metrics" | #754 (provider health checks) | Design Choice 6 |
| Grafana dashboard examples | #187 | A metric table in the caching README only |

### What this plan delivers

- `encina.cache.requests` in a new partial file `src/Encina.Caching/Diagnostics/CachePatternMetrics.Requests.cs` (on `Meter("Encina.Caching")` that #266 creates), recorded at the existing log sites of the behavior. Hit ratio per request type derives from the outcome tag.
- On `Meter("Encina.Caching.Memory")`, owned by the Memory package: `encina.cache.evictions` (counter, tag `encina.cache.eviction.reason` = `expired` or `capacity`; explicit removal and replacement are not evictions) and two observable gauges `encina.cache.entries` and `encina.cache.size` (unit `By`, estimated size, only when the size limit is configured) read from `IMemoryCache.GetCurrentStatistics()`, which returns data only when a new Encina option `MemoryCacheOptions.TrackStatistics` (default false, pay-for-what-you-use) is on.
- Two log events for the Memory gauge callback (EventIds 30500-30501, Design Choice 7).
- The tenant tag on `encina.cache.requests` through the helper of #2676, in its own pull request.
- Shared pieces and owner: #266 owns `CachePatternMetrics` (this plan adds a partial file after its PR 4); #723 owns the provider decorator; this plan owns `encina.cache.requests` and the Memory meter. Cardinality: the request type is bounded by the number of cacheable request types; no key, user, tenant text or message ever enters a tag.
- Affected packages: `Encina.Caching`, `Encina.Caching.Memory`, `Encina` (range registration). Not delivered: any instrument already planned by a sibling, value-size, hit-rate gauges, Hybrid L1 and L2, Redis server statistics.

---

## Design Choices

<details>
<summary><strong>1. Boundary with #723 and the other plans (what this plan adds)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add only the gap: request outcomes by request type, and Memory evictions and gauges; everything else is mapped to its owner in the documentation** | No duplicate instrument and no second write per cache call; one source of truth per signal | The issue's names (`encina.cache.hits`, `encina.cache.misses`, `encina.cache.sets`) do not exist as separate instruments |
| **B) Also emit separate `hits`, `misses`, `sets`, `removals` counters** | Matches the issue text | Each call writes two instruments for one event (the #723 counter already has the outcome tag); #723 rejected it for the same reason |

### Chosen Option: **A - Only the gap** (recommended, pending the maintainer)

### Rationale

- A wins on cost; B wins if #723 is dropped or the maintainer wants the exact catalog of the issue for dashboards that predate ADR-054. #723's Design Choice 4 reserves exactly these items for #273.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. A public `ICacheMetrics` or internal static recorders</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No public interface; `internal static` recorders in partial classes, as #266, #267 and #268 do** | No new public API; one way to record; instruments are the contract | A host cannot replace the recorder |
| **B) The issue's public `ICacheMetrics` (`RecordHit`, `RecordMiss`, `RecordLatency`, `RecordValueSize`) registered in DI and injected into the behavior** | Replaceable, mockable | Public surface for an internal concern; a constructor parameter on the behavior (`new QueryCachingPipelineBehavior<` has about 30 call sites, per the #266 plan) and on each provider; the `CacheLevel` and `MissReason` enums cannot be filled truthfully (register rows C3 and C4) |

### Chosen Option: **A - Internal static recorders** (recommended, pending the maintainer)

### Rationale

- A wins on API size and consistency with the sibling plans; B wins for a team that wants to forward cache events to a non-OpenTelemetry sink without parsing meters. Pre-1.0 the interface can be added later without a compatibility layer.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Process-local state metrics (evictions, entry count, size) and the provider matrix</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Memory provider only, where the process owns the state; documented as not available for the other providers** | Cheap, no network I/O in a metrics callback; evictions are visible through the callback the provider already registers | 1 of 8 providers; `AGENTS.md` section 5 asks for consistency across the category (Redis-family evictions happen on the server, Hybrid L1 is inside the Microsoft library) |
| **B) A also polls the Redis family (`DBSIZE`, `INFO memory`) from an observable gauge** | Covers 6 of 8 | A network call inside a metrics callback (an observable callback runs on the collection thread: a slow Redis stalls the exporter), needs resilience and its own error handling, and the numbers are server-wide, not Encina's keys |
| **C) None** | Nothing to maintain | The issue's size and eviction metrics are dropped |

### Chosen Option: **A - Memory only** (recommended, pending the maintainer)

### Rationale

- A wins now; B wins for operators who run only Redis-family caches and have no server-side exporter (Redis exporters already publish `DBSIZE` and memory), and would be a follow-up with its own resilience design; C wins if the maintainer holds the consistency rule strictly. Eviction reasons are mapped from `Microsoft.Extensions.Caching.Memory.EvictionReason`: `Expired` and `TokenExpired` give `expired`, `Capacity` gives `capacity`, `Removed`, `Replaced` and `None` are not evictions. The gauge callback never throws and returns no measurement when statistics are off (AGENTS.md section 3, background loops and timers).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Value size</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not emitted** | The behavior and `ICacheProvider` never see serialized bytes (the Memory provider stores objects, the Redis family and Hybrid serialize inside the provider); no double serialization | The issue's `value_size_bytes` histogram and `RecordValueSizeHistogram` are dropped |
| **B) Serialize again in the behavior when `RecordValueSizeHistogram` is on and the histogram is enabled** | Same on all 8 providers | A second serialization per stored value, with a serializer the behavior does not own (#274 is changing serialization) |
| **C) Each serializing provider records the size it wrote** | Exact, no extra work | Only the Redis family and Hybrid can; Memory cannot, so the 8 providers disagree |

### Chosen Option: **A - Not emitted** (recommended, pending the maintainer)

### Rationale

- A wins until #274 fixes the serialization seam; B wins for a team sizing a Redis cluster that accepts the cost; C wins once #274 lands a shared serializer abstraction. The decision is revisited after #274, which is the owner of the serializer.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Options surface</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No new observability options except `MemoryCacheOptions.TrackStatistics`; instruments are switched by subscribing the meter (ADR-054 settled point 7); service and environment tags come from the application's OpenTelemetry `Resource` (ADR-054 settled point 1)** | No second switch; names stay fixed for the architecture test of #1787 and the documentation | `MetricsPrefix`, `MetricsTags` and `EnableMetrics` of the issue do not exist |
| **B) The issue's `CachingOptions.EnableMetrics`, `MetricsPrefix`, `RecordValueSizeHistogram`, `MetricsTags`** | Matches the issue | A configurable prefix makes instrument names depend on configuration (the static scan and documentation cannot list them); `MetricsTags` duplicates the Resource; `RecordValueSizeHistogram` has nothing to switch (Design Choice 4) |

### Chosen Option: **A - One option** (recommended, pending the maintainer)

### Rationale

- A wins on fixed, documentable names; B wins for hosts that cannot configure a Resource and need a cache-specific prefix. `TrackStatistics` is the one real switch: statistics have a cost in `MemoryCache`, so the gauges are opt-in.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Health indicators from metrics</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No hit-rate, latency or error-rate gauges and no metrics-based health check: the backend derives them from `encina.cache.requests` and `encina.cache.operation.duration`; provider health stays with #754 and `QueryCacheHealthCheck`** | Rates are computed where they belong (a gauge of a rolling rate inside the process has a window nobody chose); no state | The issue's "health check based on metrics" is not built |
| **B) A `CacheMetricsHealthCheck` that keeps a rolling window of outcomes in memory and reports Degraded under a configured hit rate or error rate** | A readiness signal without a backend | In-process window state; thresholds are policy; a low hit rate right after a deploy is not a failure |

### Chosen Option: **A - Derive in the backend** (recommended, pending the maintainer)

### Rationale

- A wins on correctness of the rate windows; B wins for a team with no metrics backend that wants Kubernetes readiness to react to cache errors. If B is chosen it becomes its own issue after #754 (the health check family).

**Class:** Dilemma

</details>

<details>
<summary><strong>7. EventId block</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The block the maintainer assigned first: `CachingObservability` 30050-30099** | As assigned | 30000-30349 is reserved by #4007 (`CachingMemory` CA-1 holds 30050-30099 in the registry on main); two `reserved` rows would overlap and `EventIdRegistryAgreementTests` rule 4 fails |
| **B) `CachingObservability` 30500-30549, the first free 50-slot after the #4007 extent and after the slots of #276 (30350-30399), #278 (30400-30449) and #222 (30450-30499)** | Free per the registry on main and in every plan on main and open plan branch on 2026-11-24 (no row after 30349); same area base (Caching and distributed locks, 30000) | Differs from the first assignment |

### Chosen Option: **B - 30500-30549**

### Rationale

- Only B satisfies the registry (the coordinator confirmed the #4007 extents are not to be used). The pull request adds the `reserved` row `| 30500-30549 | Caching | #273 CachingObservability (ids 30500-30501) | reserved |` (status `registered` in the code pull request), one field `CachingObservability` in `src/Encina/Diagnostics/EventIdRanges.cs`, **one line in `src/Encina/PublicAPI.Unshipped.txt`** (counted in the size), the `AssemblyRanges` entry for `Encina.Caching.Memory` extended with the new name and the "Registered ranges" row. Ids 30500 and 30501 are packed from the start.
Re-slotted from 30400-30449 to 30500-30549 on 2026-11-24 after plan #278 and the #4174 reservations.

**Class:** Settled
**Settled by:** `docs/engineering/eventid-block-registry.md` rule 4 (reserved blocks do not overlap) and the #4007 extent Caching 30000-30349 (#4174)

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | No new `EncinaError` code (no error is created; cache exceptions are recorded by the behavior's existing handling), so no ADR-062 category; the instrumentation never denies or retries, so there is no fail-closed gate | Settled | ADR-062; `AGENTS.md` section 3 |
| C2 | No key, pattern, value, user, tenant text, `EncinaError.Message` or exception message in a tag, span attribute or log; `error.type` is the exception type name; the issue's `cache.key` span attribute is not emitted | Settled | ADR-054 settled point 2 and D6.3; `AGENTS.md` section 3 |
| C3 | No `reason` (expired or not found) on misses: `ICacheProvider.GetAsync<T>` returns `T?` for both cases and no provider can tell them apart; soft expiry is #266 and #268's `ExpiresAtUtc` envelope | Settled | `src/Encina.Caching/Abstractions/ICacheProvider.cs` (`GetAsync` returns `Task<T?>`) |
| C4 | No `cache_level` (L1 or L2): `HybridCache.GetOrCreateAsync` does not report the level that served a hit (`src/Encina.Caching.Hybrid/HybridCacheProvider.cs`, lines 57-78); this closes #723's register row C4 | Settled | the same file and the Microsoft `HybridCache` API |
| C5 | Zero cost without a listener: `Instrument.Enabled` before each record, `ObservableGauge` callbacks run only on collection; time from `TimeProvider` where a duration is needed (none here) | Settled | ADR-054 settled point 7 |
| C6 | Tenant tag on `encina.cache.requests` only when multi-tenancy is on, through the helper of #2676; #266's tag name `encina.tenant_id` and the helper agree or the helper wins in that pull request | Settled | ADR-054 D4; #2676 |
| C7 | Names: lowercase dotted, `s` or `By` units, no `_total`; tag keys `encina.cache.request_type`, `encina.cache.outcome`, `encina.cache.eviction.reason` | Settled | ADR-054 settled point 4 |

---

## Implementation Phases

### Phase 1: Request Outcome Counter (pull request M1, after #266 PR 4)

> **Not startable until #266's pull request that creates `CachePatternMetrics` has merged**: the new file is a partial of that class and the behavior call sites are the ones #266 edits.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Caching/Diagnostics/CachePatternMetrics.Requests.cs` (`internal static partial class CachePatternMetrics`): `Counter<long> encina.cache.requests`, `internal enum CacheRequestOutcome { Hit, Miss, Stored, Error, Bypass }`, `internal static void RecordRequest(string requestType, CacheRequestOutcome outcome)` guarded by `Instrument.Enabled`.
2. **Modify** `Behaviors/QueryCachingPipelineBehavior.cs`: one `RecordRequest(typeof(TRequest).Name, ...)` call beside each existing log call (hit, miss, set, error at both sites, both bypasses); no change to control flow, constructor or result.
3. Tests: edit `tests/Encina.UnitTests/Caching/Base/QueryCachingPipelineBehaviorTests.cs` (a `MeterListener` over a real `MemoryCacheProvider`: first call miss then stored, second call hit, a throwing provider records `error`, both bypass reasons record `bypass`, the tags hold the request type name and never the key, tenant or user); new `tests/Encina.UnitTests/Caching/Diagnostics/CacheRequestMetricsTests.cs` (instrument name, tag keys); manifest `Encina.Caching.json` and changelog `changelog.d/273-cache-request-metrics.added.md` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #273, plan docs/plans/cache-metrics-implementation-plan-273.md, phase 1.
#266 merged: CachePatternMetrics (Meter "Encina.Caching") exists as an internal static partial class.
QueryCachingPipelineBehavior logs hit, miss, set, error and bypass; it records no metric.

TASK:
1. Add CachePatternMetrics.Requests.cs with the encina.cache.requests counter and RecordRequest.
2. Call it beside each existing log call of the behavior.
3. Unit tests with a real MemoryCacheProvider and a MeterListener; manifest and changelog through mechanical-fixer.

KEY RULES:
- Tags: encina.cache.request_type (typeof(TRequest).Name) and encina.cache.outcome only; never a key, tenant, user, message.
- Do not change the behavior's constructor, control flow or result; zero work without a listener.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs, the plans of #266 and #723
```

</details>

---

### Phase 2: Memory Evictions (pull request M2)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Caching.Memory/Diagnostics/MemoryCacheMetrics.cs` (`internal static`: `Meter("Encina.Caching.Memory", "1.0")`, `Counter<long> encina.cache.evictions`, `internal static void RecordEviction(EvictionReason reason)` applying the mapping of Design Choice 3).
2. **Modify** `MemoryCacheProvider.cs`: the three post-eviction callbacks call `MemoryCacheMetrics.RecordEviction(reason)` after the tracker removal (a private `OnEvicted(object key, object? value, EvictionReason reason, object? state)` replaces the three duplicated lambdas).
3. Tests: edit `tests/Encina.UnitTests/Caching/Memory/MemoryCacheProviderTests.cs` (`Compact(1.0)` on a real `MemoryCache` gives `capacity`, a short absolute expiry gives `expired`, `RemoveAsync` and overwrite record nothing); manifest `Encina.Caching.Memory.json` and changelog `changelog.d/273-memory-cache-evictions.added.md` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #273 plan docs/plans/cache-metrics-implementation-plan-273.md, phase 2. MemoryCacheProvider registers three identical
post-eviction callbacks that only update the key tracker.

TASK:
1. Add MemoryCacheMetrics (Meter "Encina.Caching.Memory") with encina.cache.evictions.
2. Replace the three lambdas with one private OnEvicted that also records the eviction.
3. Unit tests with a real MemoryCache (Compact, short expiry); manifest and changelog through mechanical-fixer.

KEY RULES:
- Only Expired, TokenExpired (expired) and Capacity (capacity) count; Removed, Replaced and None do not.
- The callback never throws; no key in any tag.

REFERENCE FILES:
- src/Encina.Caching.Memory/MemoryCacheProvider.cs
```

</details>

---

### Phase 3: Entry and Size Gauges, Statistics Option and Logs (pull request M3)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs` (field `CachingObservability = (30500, 30549)`), `src/Encina/PublicAPI.Unshipped.txt` (one line), `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (the `Encina.Caching.Memory` entry gains the new name) and `docs/engineering/eventid-block-registry.md` (the `reserved` row and the "Registered ranges" row, through `docs-writer`).
2. **Create** `src/Encina.Caching.Memory/Diagnostics/MemoryCacheObservabilityLog.cs` (`internal static partial`: 30500 `StatisticsCallbackFailed` (Warning, exception through `ex.ForLogging()`), 30501 `StatisticsNotEnabled` (Debug, logged once when the gauges are created and `TrackStatistics` is off)).
3. **Modify** `MemoryCacheMetrics.cs` (observable gauges `encina.cache.entries` and `encina.cache.size` reading `IMemoryCache.GetCurrentStatistics()`; the callback catches everything, logs 30500 and returns no measurement) the gauges are created once by the `MemoryCacheProvider` constructor through `MemoryCacheMetrics.ObserveStatistics(IMemoryCache, ILogger)` (no new type).
4. **Modify** `MemoryCacheProvider.cs` (the `MemoryCacheOptions` class in the same file gains `bool TrackStatistics { get; set; }`) and `ServiceCollectionExtensions.cs` (`services.AddOptions<Microsoft.Extensions.Caching.Memory.MemoryCacheOptions>().Configure<IOptions<MemoryCacheOptions>>(...)` copies `TrackStatistics`; the registration neither replaces an application's own `AddMemoryCache` nor throws); `PublicAPI.Unshipped.txt` of `Encina.Caching.Memory` through `mechanical-fixer`.
5. Tests: new `tests/Encina.UnitTests/Caching/Memory/MemoryCacheMetricsTests.cs` (gauges report entry count and estimated size after sets when `TrackStatistics` is on, report nothing and log 30501 once when off, a throwing `IMemoryCache` double logs 30500 and does not throw, no key in any tag); edit `tests/Encina.UnitTests/Caching/Memory/ServiceCollectionExtensionsTests.cs` (provider built with `ValidateOnBuild` and `ValidateScopes`; the statistics option reaches the cache) and `tests/Encina.GuardTests/Infrastructure/Caching/MemoryCacheProviderGuardTests.cs` (the constructor is unchanged; a case proving `TrackStatistics` default false); manifest and changelog `changelog.d/273-memory-cache-gauges.added.md` through `mechanical-fixer`; README of `Encina.Caching.Memory` (metric table, how to enable statistics) through `docs-writer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #273 plan docs/plans/cache-metrics-implementation-plan-273.md, phase 3. M2 merged (MemoryCacheMetrics exists).
Block 30500-30549 is free per the registry on main and per every plan on main and open plan branch; 30000-30349 belongs to #4007.

TASK:
1. Register CachingObservability (EventIdRanges, PublicAPI line, AssemblyRanges entry, registry rows through docs-writer).
2. Add MemoryCacheObservabilityLog (30500, 30501) and the observable gauges encina.cache.entries and encina.cache.size.
3. Add MemoryCacheOptions.TrackStatistics and wire it into the Microsoft MemoryCacheOptions without replacing the application's registration.
4. Tests; PublicAPI, manifest, changelog through mechanical-fixer; README through docs-writer.

KEY RULES:
- The gauge callback never throws and never blocks; log by code and ex.ForLogging(); no key in any tag.
- Packed ids from 30500; range registered first (ADR-021).
- Registration completeness: build the provider with ValidateOnBuild and ValidateScopes.

REFERENCE FILES:
- src/Encina.Caching.Memory/ServiceCollectionExtensions.cs, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 4: Tenant Tag (pull request M4, after #2676)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `Diagnostics/CachePatternMetrics.Requests.cs`: the tenant tag from the helper of #2676 on `encina.cache.requests`, only when multi-tenancy is on and a tenant resolves; the behavior already holds the resolved tenant (`context.TenantId`), so no scoped service is injected into any singleton.
2. Tests: edit `QueryCachingPipelineBehaviorTests.cs` and `CacheRequestMetricsTests.cs` (tenant on, off, blank); changelog `changelog.d/273-cache-request-metrics-tenant.added.md` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #273 plan docs/plans/cache-metrics-implementation-plan-273.md, phase 4. M1 and #2676 merged.

TASK:
1. Add the tenant tag through the shared helper to encina.cache.requests.
2. Tests for tenant on, off and blank.

KEY RULES:
- Only when multi-tenancy is enabled; never the tenant text in a key, name or log; no baggage.

REFERENCE FILES:
- the helper of #2676, src/Encina.Caching/Diagnostics/CachePatternMetrics.Requests.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| ADR-054 | Settled points 1, 2, 4, 7; D3, D4, D6.3 | SDK is the application's, error text, names, zero cost, tenant tag, no data-bearing keys |
| `AGENTS.md` section 5 (Caching) | Consistency across the 8 providers | Behavior-level counter on all 8; Memory-only state metrics justified in Design Choice 3 |
| `AGENTS.md` section 3 | Background callbacks never throw; errors logged by code | Gauge callback |
| OpenTelemetry metrics API | Observable gauges, units `By` and `s` | Instruments |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `QueryCachingPipelineBehavior` | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | Already logs cache hit, miss, set and error per request type; the new counter is recorded at the same sites |
| `MemoryCacheProvider` | `src/Encina.Caching.Memory/MemoryCacheProvider.cs` | Process-local cache with post-eviction callbacks; source of the eviction counter and the gauges |
| `HybridCacheProvider` | `src/Encina.Caching.Hybrid/HybridCacheProvider.cs` | Wraps `HybridCache`, which does not report the level of a hit (register row C4) |
| `InstrumentedCacheProvider` | `src/Encina.OpenTelemetry/QueryCache/InstrumentedCacheProvider.cs` | Decorator extended by #723; emits the operations counter and duration histogram |
| `CachePatternMetrics` | #266, `src/Encina.Caching/Diagnostics/` (not on main yet) | Meter `Encina.Caching`; this plan adds a partial file |
| `CachingOptions` | `src/Encina.Caching/Configuration/CachingOptions.cs` | Unchanged (Design Choice 5) |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Registry where the new range is registered first |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching.Memory` (`CachingObservability`, new) | 30500-30549, free per the registry on main and every plan on 2026-11-24 | 30500 gauge callback failed, 30501 statistics not enabled; 30502-30549 free; #276 holds 30350-30399, #278 30500-30549, #222 30450-30499; 30000-30349 is #4007's |
| `Encina.Caching` (`Caching`, 3500-3549) | none | The behavior's existing ids 3508-3513 are untouched; no new log in the behavior |

### Estimated File Count

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| M1 | Request counter | 1 | 1 (`QueryCachingPipelineBehavior.cs`) | 1 | 1 | 1 | 1 (changelog) | 6 | ~330 |
| M2 | Memory evictions | 1 | 1 (`MemoryCacheProvider.cs`) | 0 | 1 | 1 | 1 (changelog) | 5 | ~220 |
| M3 | Gauges, option, range, logs | 1 (`MemoryCacheObservabilityLog.cs`) | 5 (`MemoryCacheMetrics.cs`, `MemoryCacheProvider.cs`, `ServiceCollectionExtensions.cs`, `EventIdRanges.cs`, `Encina` PublicAPI) + 1 (`Encina.Caching.Memory` PublicAPI) | 1 | 3 (allocation test, DI test, guard test) | 1 | 3 (registry page, README, changelog) | 15 | ~640 |
| M4 | Tenant tag (after #2676) | 0 | 1 | 0 | 2 | 0 | 1 (changelog) | 4 | ~110 |
| **Total** | | **3** | **9** | **2** | **7** | **3** | **6** | **30** | **~1,300** |

Columns sum: 3 + 9 + 2 + 7 + 3 + 6 = 30 = 6 + 5 + 15 + 4. M3 sits at the 15-file limit; if the implementer finds one more file is needed the README moves to M4. Test doubles and call sites: no interface changes; the behavior's constructor is not touched (about 30 call sites unchanged, per the #266 plan); the Memory provider constructor is unchanged.

Per-file coverage targets (manifest `Encina.Caching.json` package: contract 10, guard 30, unit 60; `Encina.Caching.Memory.json` package: guard 20, unit 60):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `Encina.Caching/Diagnostics/CachePatternMetrics.Requests.cs` | 95 | 0 | 0 | 0 | 0 | Every outcome is driven through the real behavior; internal static with no validated parameter, so guard cannot reach it; contract 0 because the counter is behavior-level, identical on all 8 providers by construction |
| `Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` (existing 98, 20, 0) | 98 | 20 | 0 | 0 | 0 | Targets stay; the added calls run in the existing hit, miss, error and bypass tests |
| `Encina.Caching.Memory/Diagnostics/MemoryCacheMetrics.cs` | 95 | 0 | 0 | 0 | 0 | The reason mapping is table-driven over every `EvictionReason`; gauge callbacks run in the statistics tests; internal static |
| `Encina.Caching.Memory/Diagnostics/MemoryCacheObservabilityLog.cs` | 0 | 0 | 0 | 0 | 0 | Generator-emitted `[LoggerMessage]` partials are not measured as hand-written lines and have no validated parameter; the events are asserted through a fake logger |
| `Encina.Caching.Memory/MemoryCacheProvider.cs` (existing 95, 20) | 96 | 20 | 0 | 0 | 0 | Unit rises by one point: the shared `OnEvicted` callback removes three duplicated lambdas and is driven by the eviction tests; guard stays |
| `Encina.Caching.Memory/ServiceCollectionExtensions.cs` (no per-file entry today) | 90 | 40 | 0 | 0 | 0 | The new option wiring and its two outcomes (statistics on and off) run in the DI test; guard covers the null `services` and null `configure` checks |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #273 - the cache observability gap that #723 does not cover - in four small pull requests. M1 waits for
#266's pull request that creates CachePatternMetrics; M4 waits for #2676.

PROJECT CONTEXT:
- Encina .NET 10 / C# 14, pre-1.0. Edit with Edit/Write only; PublicAPI, manifests and changelog through mechanical-fixer; README and
  the registry page through docs-writer.
- ADR-054: lowercase dotted instrument names, no _total, never data-bearing keys, error type only, zero cost without a listener.

IMPLEMENTATION OVERVIEW:
1. M1: encina.cache.requests in the pipeline behavior (outcomes hit, miss, stored, error, bypass; tag request type).
2. M2: Memory evictions counter on Meter "Encina.Caching.Memory". M3: entries and size gauges, TrackStatistics option, range
   30500-30549 and two logs. M4: tenant tag.

KEY PATTERNS:
- Do not add instruments a sibling owns: operations and duration (#723), stampede (#266), failsafe (#268), invalidations (#2408).
- No key, pattern, value, user, tenant text or message in any tag; the gauge callback never throws.
- Keep the behavior's constructor and every new QueryCachingPipelineBehavior< call site unchanged.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs, src/Encina.Caching.Memory/MemoryCacheProvider.cs
- the plans of #723, #266, #268 and #2408, docs/engineering/eventid-block-registry.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Phases 1-3 | This is the observability of the caching layer; it adds no cached read |
| 2 | OpenTelemetry | ✅ Phases 1-4 | This is the feature (the gap over #723) |
| 3 | Structured Logging | ✅ Phase 3 | `MemoryCacheObservabilityLog`, ids 30500-30501; the behavior's existing logs are unchanged |
| 4 | Health Checks | ❌ N/A | Provider health checks are #754 and `QueryCacheHealthCheck`; a metrics-based check is Design Choice 6 option B |
| 5 | Validation | ❌ N/A | No input; the only option is a boolean |
| 6 | Resilience | ❌ N/A | Passive layer: no external call; the Redis polling that would need it is Design Choice 3 option B |
| 7 | Distributed Locks | ❌ N/A | Acquires no lock |
| 8 | Transactions | ❌ N/A | Never commits or rolls back |
| 9 | Idempotency | ❌ N/A | Counter increments are additive; a repeated request is a repeated outcome |
| 10 | Multi-Tenancy | ✅ Phase 4 | Tenant tag through the helper of #2676 on `encina.cache.requests`; keys are never emitted |
| 11 | Module Isolation | ⏭️ Deferred | Module dimension on cache and lock telemetry: follow-up issue file `lock-cache-telemetry-module-dimension.md` (to be opened by the orchestrator; #746 is the cache key side) |
| 12 | Audit Trail | ❌ N/A | Infrastructure telemetry; no personal data may reach it (register row C2) |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| #266 pull request that creates `CachePatternMetrics` | Open (plan on main) | Only M1 and M4 wait; M2 and M3 are independent of it |
| #2676 tenant helper | Open | Only M4 waits |
| ADR-054 accepted | ✅ On `main` | D3, D4, D6 |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Provider decorator and pub/sub metrics | #723 | The boundary of Design Choice 1; no code dependency |
| Fail-safe, stampede, invalidation counters | #268, #266, #2408 | Own the instruments the issue lists for stale served, stampede and invalidation |
| Serialization seam | #274 | Revisit Design Choice 4 after it lands |
| Provider health checks | #754 | Separate from this telemetry |
| Dashboards | #187, #906 | Consume the metric table |
| Memcached provider | #277 | Covered by the behavior-level counter when it lands |

---

## Next Steps

1. Link this plan from #273 and ask the maintainer to answer Design Choices 1 to 6 when work starts (7 is settled).
2. Implement M2 and M3 first (independent of #266), then M1 after #266, then M4 after #2676.
3. Maintainer action: amend the acceptance criteria of #273 that name `ICacheMetrics`, `MetricsPrefix`, `MetricsTags`, the health gauges and the `cache.key` span attribute to the outcome of Design Choices 1, 2, 5 and 6 and register rows C2 to C4 (this plan never comments on an issue).
