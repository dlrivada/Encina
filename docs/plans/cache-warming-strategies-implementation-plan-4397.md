# Implementation Plan: Cache warming TopHeavy and AccessPattern strategies

> **Issue**: [#4397](https://github.com/dlrivada/Encina/issues/4397)
> **Type**: Feature
> **Complexity**: Medium (4 code phases, 4 pull requests of 14, 11, 10 and 14 files)
> **Estimated Scope**: about 450 lines of production code, about 1,000 lines of tests
> **Milestone**: Encina 1.0 (caching category, SPEC-000 REQ-027)

---

## Summary

Issue #4397 adds the two strategies that the original cache-warming issue (#269) listed and its plan did not ship: warm only the highest-priority queries (top N percent) and warm by historical access patterns. Re-checked against `origin/main` (4047d9a8a); #269 is open and its plan is merged, so the engine and `WarmingStrategy { Sequential, Parallel }` do not exist yet and this plan builds on the types that plan defines (#269 Design Choice 3 chose to ship only `Sequential` and `Parallel`).

| Premise of the issue | State in the code |
|----------------------|-------------------|
| "Warm only the highest-priority queries (top N percent)" (the #269 text: `TopHeavy` "Only warm highest priority queries (top N%)") | `ICacheWarmer.Priority` is "lower = earlier" (#269 plan, Phase 1 task 1), so "highest priority" is the lowest values. The percentage applies to registered warmers, ordered by priority (Design Choice 2). |
| `AccessPattern` needs access statistics and "Encina collects none per cache key" | True: `QueryCachingPipelineBehavior` serves hits at `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs:179-186` and keeps no counter. |
| Access counters "keyed by the tenant-qualified key" (issue, cross-cutting row 10) | A cache key cannot rebuild a request: the key is `{prefix}:t:{tenant}:...:{RequestType}:{hash}` with the request's properties hashed (`DefaultCacheKeyGenerator.cs`), while a warmer needs request instances from its `IWarmingRequestSource<TRequest>` (#269). Per-key counters are therefore unbounded, hold tenant ids in memory and drive nothing. The counters are per request TYPE, bounded by the registered request warmers (Design Choice 5), which also satisfies "no unbounded memory growth". |
| In-process counters (issue D1) | They are empty at startup, the moment the #269 hosted service warms: with in-process counters `AccessPattern` has no data on a cold start and helps only a later `ICacheWarmingService.RunAsync` (after an invalidation, or a periodic re-warm). Design Choice 4 says what a run does with no data. |
| `TopHeavy` and `AccessPattern` are values of `WarmingStrategy` | `WarmingStrategy` is an execution mode (sequential or parallel, #269); these two are selections of WHICH warmers run. One enum value cannot say "top 20 percent, in parallel" (Design Choice 3). |

The plan adds a selection step to the #269 engine (`WarmingSelector`), the options `WarmingSelection` and `WarmingTopPercent`, and, for `AccessPattern`, `IWarmingAccessStatistics` with an in-process default fed by `QueryCachingPipelineBehavior` hits. Dispatch stays `IEncina.Send`, so the 8 cache providers (Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB and the planned Memcached of #277) behave identically and no provider changes. If the maintainer answers Design Choice 1 with option C (do not ship), only PR 1 (`TopHeavy`, which needs no statistics) is implemented.

**Family cache-warming-followups (#4396, #4397, #4398) - ownership and order.** #4396 (health check and lock) is implemented first, #4398 (tenant sets) second, this plan last; none of the three uses the others' types, but #4398 PR 2 and this plan's PR 3 and PR 4 edit the same lines of `RequestCacheWarmer.cs` and `CacheWarmingService.cs`, so the later one rebases. This plan owns `WarmingSelection`, `WarmingTopPercent`, `WarmingSelector`, `IWarmingAccessStatistics` and EventIds 494-495. All three edit `CacheWarmingService.cs`, `CachingOptions.cs`, `ServiceCollectionExtensions.cs` and `CachePatternLog.Warming.cs`; the later plan rebases on the earlier one. `QueryCachingPipelineBehavior.cs` is also edited by #266, #267, #268 and #271: this plan's PR 3 merges after the sequence recorded in the #267 plan (#266 PR 1, PR 3, PR 4, then #267 PR 3, then #268 PR 1 and PR 2, then #271 PR 5).

**Affected packages:** `Encina.Caching`. **Provider category:** Caching (8), behavior-level.

---

## Design Choices

<details>
<summary><strong>1. Where access statistics come from, and whether to ship them (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In-process counters per request type behind a public `IWarmingAccessStatistics` (default `WarmingAccessStatistics`, registered with `TryAddSingleton` so an application's own persisted implementation replaces it); no per-provider code** | Cheap, local, lock-free (`Interlocked`); small surface; a persisted implementation can be added later without changing the engine | Lost on restart: a cold-start run has no data (Design Choice 4); counts are per node |
| **B) Also ship a persisted implementation: a periodic snapshot of the counters written through `ICacheProvider` and read at startup** | Survives a node restart on a shared cache (the Redis family, Hybrid L2) | A flush of the cache loses it; no atomic increment across nodes, so counts are last-writer-wins snapshots; a flusher background service to maintain; Memory keeps nothing |
| **C) Do not ship `AccessPattern` (the enum keeps `TopHeavy` only)** | No statistics, no hit-path cost, smallest change | One of the two values of the issue is not delivered |

### Chosen Option: **A - in-process counters behind an interface** (recommended, pending the maintainer)

### Rationale

- The interface is the seam for B without committing to it now. Option B wins when `AccessPattern` must work on the first run after a restart in a fleet. Option C wins when the maintainer prefers no hit-path cost and no statistics surface.
- Class: Dilemma

</details>

<details>
<summary><strong>2. What `WarmingTopPercent` selects (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A percentage of the registered WARMERS: sorted by `Priority` ascending then registration order, the engine runs the first `max(1, ceiling(count x percent / 100))` (integer arithmetic); the option is an int from 1 to 100, default 20 (the issue's example)** | Matches the wording ("highest priority queries"); needs no statistics; deterministic | A warmer is all or nothing; a priority group can be cut in the middle (by registration order) |
| **B) A percentage of the REQUESTS each request warmer's source yields (the first N percent of `WarmingInstances`)** | Finer; keeps every warmer | The source order must already mean importance; custom warmers have no instances to cut |

### Chosen Option: **A - percentage of warmers** (recommended, pending the maintainer)

### Rationale

- Option B wins when a few warmers yield thousands of instances each and the tail of each list is the cost. The default 20 is the issue's configuration example, recommended and pending the maintainer.
- Class: Dilemma

</details>

<details>
<summary><strong>3. Shape of the option (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add `TopHeavy` and `AccessPattern` to `WarmingStrategy`, as in the issue; both run their selected warmers sequentially in priority order** | The issue's syntax (`WarmingStrategy = AccessPattern`); one option | Cannot combine selection with `Parallel` |
| **B) A second enum `WarmingSelection { All, TopHeavy, AccessPattern }` orthogonal to `WarmingStrategy`; the selected warmers then run as `Sequential` or `Parallel`** | Selection and execution combine; `WarmingStrategy` keeps its meaning | The issue's sample syntax changes (`o.WarmingSelection = ...`) |

### Chosen Option: **B - orthogonal selection** (recommended, pending the maintainer)

### Rationale

- Pre-1.0 the better design wins over the issue's sample. Option A wins when the issue's syntax is a hard requirement and combining selection with parallel execution is not needed.
- Class: Dilemma

</details>

<details>
<summary><strong>4. A run with no statistics (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AccessPattern` with no recorded hits for any selected candidate falls back to the `TopHeavy` order (priority ascending) and logs EventId 495** | A cold start still warms the most important warmers | The first run is not access-driven |
| **B) Warm nothing** | Honest: nothing is known | A cold-start run does no work and the feature looks broken after every restart |
| **C) Warm every warmer** | Safe | Defeats the percentage |

### Chosen Option: **A - fall back to priority order** (recommended, pending the maintainer)

### Rationale

- A warmer with no recorded hits ranks by priority below the ones with hits, so after traffic the order is by hits descending, then priority, then registration order; only an all-zero set triggers the log. Option B wins when a partial warm-up on a cold start is worse than none.
- Class: Dilemma

</details>

<details>
<summary><strong>5. What the counters hold</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One counter per request type of a registered request warmer (a frozen set built from the registrations); a type outside the set is ignored; no tenant, user or key is stored** | Bounded by configuration; nothing personal in memory; the count maps to a warmer | Counts hits per type, not per instance |
| **B) One counter per cache key (the tenant-qualified key)** | Finest | Unbounded; holds tenant ids; a key cannot rebuild the request a warmer dispatches |

### Chosen Option: **A - per request type, bounded**

### Rationale

- **Class:** Settled
- **Settled by:** the acceptance criterion of #4397 "No unbounded memory growth from statistics", the AGENTS.md section 3 tenant and personal-data rules, and the code: `DefaultCacheKeyGenerator` hashes the request's properties into the key, so a key is not invertible into the request a source would yield; only option A is bounded and usable. It applies when statistics ship (Design Choice 1 options A and B).

</details>

<details>
<summary><strong>6. Which hits count</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Only a hit that the behavior serves counts: a request that bypasses the cache (missing tenant under multi-tenancy, `VaryByUser` without a user, EventId 3513 and its sibling) is not a hit, and a dispatch from the warming engine carries the internal metadata marker `CacheWarmingMarker.Key` and is not counted** | Counts real access only; a warmed type does not rank itself higher by being warmed | One marker |
| **B) Count every dispatch that reaches the behavior** | Simplest | Warming's own second dispatch (a hit on an existing entry) inflates the type that is warmed; bypassed requests count as hits |

### Chosen Option: **A - served hits only**

### Rationale

- **Class:** Settled
- **Settled by:** the purpose stated in #4397 ("what traffic actually asks for", `AccessPattern` warms by historical access patterns) and `QueryCachingPipelineBehavior.ShouldUseCache` (`QueryCachingPipelineBehavior.cs:133-141`: a request that fails the tenant or user check never reads the cache, so it is not a hit); only option A counts access and nothing else. The marker is this plan's own mechanism (no sibling type is relied on). It applies when statistics ship (Design Choice 1 options A and B).

</details>

<details>
<summary><strong>7. Ordering, validation and cost</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Ties break by `Priority` then registration order (the order DI enumerates `IEnumerable<ICacheWarmer>`); `WarmingTopPercent` rejects values outside 1 to 100 with `ArgumentOutOfRangeException`; recording is one dictionary read and one `Interlocked.Increment`, benchmarked** | Deterministic and testable; invalid values fail at configuration; the hit-path cost is measured | None |
| **B) Unordered ties, no validation** | Less code | Non-deterministic runs; a 0 or 500 percent silently warms nothing or everything |

### Chosen Option: **A - deterministic, validated, benchmarked**

### Rationale

- **Class:** Trivial
- **Settled by:** the `ArgumentOutOfRangeException` setter convention of the #269 plan (Design Choice 7, AGENTS.md section 9 guard-test convention), the documented DI behavior that `IEnumerable<T>` resolves in registration order, and the issue's own Test Matrix row "BenchmarkTests: the counter sits on the hit path: benchmark it". Holds under every option of Design Choices 1 to 4.

</details>

<details>
<summary><strong>8. Statistics are recorded only when they are read</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The default `IWarmingAccessStatistics` is registered, and therefore hits are recorded, only when warming is enabled and the selection is `AccessPattern` (the `WarmingStrategy` value under Design Choice 3 option A); PR 2 and PR 3 register it whenever warming is enabled until PR 4 introduces the value and narrows the condition** | Pay for what you use; no hit-path cost for `All` and `TopHeavy` | A short interim in which a warming-enabled application records hits nothing reads (its cost is the one benchmarked in PR 3) |
| **B) Always register and record when warming is enabled** | Statistics available for custom use | Hit-path cost with no reader for most configurations |

### Chosen Option: **A - register and record only for AccessPattern**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 1 ("pay-for-what-you-use (every feature opt-in)") and section 3 (patterns optional and disabled by default); only option A complies once PR 4 has merged. It applies when statistics ship (Design Choice 1 options A and B).

</details>

---

## Implementation Phases

### Phase 1: TopHeavy selection (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 14 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Configuration/WarmingSelection.cs` | new (public enum: `All`, `TopHeavy`) |
| 2 | `src/Encina.Caching/Configuration/CachingOptions.cs` | modify (`WarmingSelection` default `All`, `WarmingTopPercent` default 20) |
| 3 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (copy the two options) |
| 4 | `src/Encina.Caching/Warming/WarmingSelector.cs` | new (internal static) |
| 5 | `src/Encina.Caching/Warming/CacheWarmingService.cs` | modify (select before grouping) |
| 6 | `src/Encina.Caching/Diagnostics/CachePatternLog.Warming.cs` | modify (id 494) |
| 7 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 8 | `tests/Encina.UnitTests/Caching/WarmingSelectorTests.cs` | new |
| 9 | `tests/Encina.UnitTests/Caching/CachingOptionsTests.cs` | modify |
| 10 | `tests/Encina.UnitTests/Caching/CacheWarmingServiceTests.cs` | modify |
| 11 | `tests/Encina.GuardTests/Infrastructure/Caching/CachingOptionsWarmingSelectionGuardTests.cs` | new |
| 12 | `docs/features/handler-caching.md` | modify (`docs-writer`: selection and `TopHeavy`) |
| 13 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 14 | `changelog.d/4397-cache-warming-topheavy.added.md` | new (`mechanical-fixer`) |

Tasks (the shape follows Design Choices 2 and 3; under option A of Design Choice 3 the enum value goes in `WarmingStrategy` and `WarmingSelection` is not created):

1. **Create** `enum WarmingSelection { All = 0, TopHeavy = 1 }`; `CachingOptions.WarmingSelection` (default `All`) and `WarmingTopPercent` (int, default 20, the setter throws `ArgumentOutOfRangeException` outside 1 to 100); copy both in `AddEncinaCaching` (the list at lines 53-72 is hand-written).
2. **Create** `WarmingSelector.Select(IReadOnlyList<ICacheWarmer> warmers, WarmingSelection selection, int percent)` returning the warmers to run: `All` returns the list; `TopHeavy` sorts by `Priority` ascending then the original index and takes `max(1, (int)((count * (long)percent + 99) / 100))`; an empty list returns empty.
3. **Engine:** after resolving the warmers in the planning scope and before grouping by priority, call the selector; log EventId 494 (Information, once per run: selected count, total, selection, percent) when the selection is not `All`; the report counts only warmers that ran (the unselected are not `Skipped`: they were never part of the run).
4. **Tests:** the selector (priority order, ties by registration order, the ceiling and the minimum of 1, 100 percent equals all, empty, one warmer), the options defaults and copy, the engine with `TopHeavy` runs only the selected warmers and logs 494; **guard tests:** `WarmingTopPercent` of 0, -1 and 101 throw, 1 and 100 are stored.
5. **Docs:** `docs-writer` adds the "Selecting warmers" section to `docs/features/handler-caching.md` citing the tests by name.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #4397 adds warmer selection to cache warming (#269, merged: ICacheWarmer with Priority,
CacheWarmingService, WarmingStrategy Sequential and Parallel, CachePatternLog.Warming.cs). EventId 494 of CachingPatterns (450-499, registered by
#266) is reserved for this issue.

TASK:
Add WarmingSelection (All, TopHeavy), the options WarmingSelection and WarmingTopPercent with validating setters, WarmingSelector, its use in the
engine and EventId 494; write the unit and guard tests; ask mechanical-fixer for PublicAPI, the manifest and the changelog and docs-writer for the
docs section.

KEY RULES:
- Follow the maintainer's answers to Design Choices 1 to 4, given in your brief; if Design Choice 3 is option A, add the value to WarmingStrategy.
- Deterministic order (priority, then registration order); the report counts only warmers that ran.
- Opt-in: the default selection is All, so the #269 behavior is unchanged.

REFERENCE FILES:
- src/Encina.Caching/Warming/CacheWarmingService.cs (from #269)
- src/Encina.Caching/Configuration/CachingOptions.cs
- tests/Encina.UnitTests/Caching/CacheWarmingServiceTests.cs (from #269)
```

</details>

### Phase 2: Access statistics (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 11 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Abstractions/IWarmingAccessStatistics.cs` | new |
| 2 | `src/Encina.Caching/Warming/WarmingAccessStatistics.cs` | new (internal sealed) |
| 3 | `src/Encina.Caching/Warming/WarmedRequestType.cs` | new (internal interface `IWarmedRequestType { Type RequestType { get; } }` and internal sealed `WarmedRequestType<TRequest>`) |
| 4 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (`AddEncinaRequestCacheWarmer` adds the descriptor; `AddEncinaCaching` registers the statistics with `TryAddSingleton` only when `EnableCacheWarming` is true) |
| 5 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 6 | `tests/Encina.UnitTests/Caching/WarmingAccessStatisticsTests.cs` | new |
| 7 | `tests/Encina.ContractTests/Caching/WarmingAccessStatisticsContractTests.cs` | new |
| 8 | `tests/Encina.GuardTests/Infrastructure/Caching/WarmingAccessStatisticsGuardTests.cs` | new |
| 9 | `tests/Encina.UnitTests/Caching/ServiceCollectionExtensionsTests.cs` | modify (`ValidateOnBuild` and `ValidateScopes`; an application registration wins; two request warmers) |
| 10 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 11 | `changelog.d/4397-cache-warming-access-statistics.added.md` | new (`mechanical-fixer`) |

This PR exists only under Design Choice 1 options A and B; under C it is dropped with PR 3 and PR 4.

Tasks:

1. **Create** `public interface IWarmingAccessStatistics { void RecordHit(Type requestType); long GetHits(Type requestType); }` (XML docs with an example of a persisted replacement) and the internal descriptor pair `IWarmedRequestType` / `WarmedRequestType<TRequest>`. `WarmingAccessStatistics(IEnumerable<IWarmedRequestType> types)` builds a read-only dictionary `Type -> long[1]` once (frozen set); `RecordHit` ignores a type outside the set and otherwise calls `Interlocked.Increment` on the slot; `GetHits` reads with `Volatile.Read` and returns 0 for an unknown type; null arguments throw `ArgumentNullException`. Nothing but a type and a count is stored, so no tenant, user or key can leak.
2. **Registration:** `AddEncinaRequestCacheWarmer<TRequest, ...>` adds `TryAddEnumerable(ServiceDescriptor.Singleton<IWarmedRequestType, WarmedRequestType<TRequest>>())`: the service type (`IWarmedRequestType`) differs from the implementation type, so `TryAddEnumerable` does not throw (the trap recorded in the #269 plan for descriptors whose implementation type equals the service type), and each closed `WarmedRequestType<TRequest>` is a distinct implementation, so two request warmers both register and the same request twice registers once. `AddEncinaCaching` registers `IWarmingAccessStatistics` (`TryAddSingleton`, so a registration made before or after by the application wins) when `EnableCacheWarming` is true; PR 4 narrows this to `WarmingSelection == AccessPattern` once that value exists (Design Choice 8), so until PR 4 merges a warming-enabled application records hits at the cost measured by the PR 3 benchmark. The DI test proves it builds with both validations, that a user registration replaces the default in either order, and that TWO different request warmers yield a statistics object that counts both types.
3. **Tests (unit):** counts per type, unknown types ignored, concurrency (many threads, exact total), `GetHits` of an unknown type is 0. **Contract tests** instantiate the real `WarmingAccessStatistics` through the interface and assert the contract any implementation must keep (recorded hits are readable, an unknown type reads 0, null throws), plus a second small implementation in the test to show the seam. **Guard tests** (#3798): `WarmingAccessStatisticsGuardTests` resolves `IWarmingAccessStatistics` from a provider built with `AddEncinaCaching` (warming enabled, one request warmer registered) and asserts that `RecordHit(null)` and `GetHits(null)` throw `ArgumentNullException`: the internal class is reached through the public interface, so no `InternalsVisibleTo` is needed.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged (WarmingSelection, WarmingSelector). #269 is merged (AddEncinaRequestCacheWarmer, RequestCacheWarmer).

TASK:
Add IWarmingAccessStatistics, the internal WarmingAccessStatistics and WarmedRequestType, the registrations and the unit and contract tests; ask
mechanical-fixer for PublicAPI, the manifest and the changelog.

KEY RULES:
- Counters per request type of a registered request warmer only; lock-free; nothing else stored; null arguments throw.
- TryAddSingleton so an application registration wins; registered only when EnableCacheWarming is true.
- Contract tests instantiate the real implementation.

REFERENCE FILES:
- src/Encina.Caching/ServiceCollectionExtensions.cs
- tests/Encina.ContractTests/Database/Caching/QueryCachingContractTests.cs
```

</details>

### Phase 3: Recording hits in the behavior (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 10 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | modify (optional trailing constructor parameter `IWarmingAccessStatistics? accessStatistics = null`; record a served hit) |
| 2 | `src/Encina.Caching/Warming/CacheWarmingMarker.cs` | new (internal static, the metadata key) |
| 3 | `src/Encina.Caching/Warming/RequestCacheWarmer.cs` | modify (stamp the marker on every dispatched context) |
| 4 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: the constructor signature) |
| 5 | `tests/Encina.UnitTests/Caching/QueryCachingPipelineBehaviorTests.cs` | modify |
| 6 | `tests/Encina.UnitTests/Caching/RequestCacheWarmerTests.cs` | modify (marker) |
| 7 | `tests/Encina.GuardTests/Infrastructure/Caching/QueryCachingPipelineBehaviorGuardTests.cs` | modify (null `accessStatistics` is valid) |
| 8 | `tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks/WarmingAccessRecordBenchmarks.cs` | new |
| 9 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 10 | `changelog.d/4397-cache-warming-access-recording.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Behavior:** the new parameter is the LAST optional one (the order of the family's optional parameters is fixed by the first PR that adds them; the worker appends); after the served hit at the point where `LogCacheHit` is called (`QueryCachingPipelineBehavior.cs:186`), and only when the context metadata does not carry `CacheWarmingMarker.Key` (Design Choice 6), call `accessStatistics?.RecordHit(typeof(TRequest))`. A bypass, a miss and a store never record. Registration needs no change: DI resolves the optional parameter when the statistics are registered (the behavior is registered by the existing code; the worker confirms the factory and keeps a null parameter valid).
2. **`RequestCacheWarmer`:** every context it dispatches (the anonymous context of #269, plus the tenant of #4398 when present) gets `WithMetadata(CacheWarmingMarker.Key, "1")` on a NEW instance (never mutating the original).
3. **Tests (unit):** a served hit increments the type; a miss, a tenant bypass, a `VaryByUser` bypass and a marked warming dispatch do not; a null statistics parameter changes nothing; **guard:** the optional parameter accepts null (valid result, not an exception); the marker is present on every warmer dispatch. **Benchmark:** the hit path with and without the recorder (`BenchmarkSwitcher.FromAssembly`, verify the filter with `--list flat`), results to `artifacts/performance/`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Phases 1 and 2 are merged: selection and IWarmingAccessStatistics exist and are registered when warming is enabled. The behavior is also edited by
#266, #267, #268 and #271; rebase on them.

TASK:
Record a served hit per request type in QueryCachingPipelineBehavior (optional last constructor parameter), add CacheWarmingMarker and stamp it in
RequestCacheWarmer, write the unit, guard and benchmark tests; ask mechanical-fixer for PublicAPI, the manifest and the changelog.

KEY RULES:
- Only a served hit counts; bypasses and warming dispatches never do; a null recorder changes nothing.
- Never mutate the caller's context: build a new one with the marker.
- BenchmarkSwitcher, never BenchmarkRunner.Run; materialize nothing lazily in a benchmark.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina.Caching/Warming/RequestCacheWarmer.cs (from #269)
- tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks/CacheInvalidationBenchmarks.cs
```

</details>

### Phase 4: AccessPattern selection (pull request 4)

<details>
<summary><strong>Tasks</strong></summary>

PR 4 holds exactly these 14 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Configuration/WarmingSelection.cs` | modify (add `AccessPattern`) |
| 2 | `src/Encina.Caching/Warming/WarmingSelector.cs` | modify (ranking by hits) |
| 3 | `src/Encina.Caching/Warming/IWarmerRequestType.cs` | new (internal interface: `Type RequestType`) |
| 4 | `src/Encina.Caching/Warming/RequestCacheWarmer.cs` | modify (implement it) |
| 5 | `src/Encina.Caching/Warming/CacheWarmingService.cs` | modify (pass the statistics to the selector; log 495) |
| 6 | `src/Encina.Caching/Diagnostics/CachePatternLog.Warming.cs` | modify (id 495) |
| 7 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: the enum value) |
| 8 | `tests/Encina.UnitTests/Caching/WarmingSelectorTests.cs` | modify |
| 9 | `tests/Encina.UnitTests/Caching/CacheWarmingServiceTests.cs` | modify |
| 10 | `docs/features/handler-caching.md` | modify (`docs-writer`: AccessPattern, the cold-start limit) |
| 11 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 12 | `changelog.d/4397-cache-warming-access-pattern.added.md` | new (`mechanical-fixer`) |
| 13 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (register the default statistics only when `WarmingSelection` is `AccessPattern`, Design Choice 8) |
| 14 | `tests/Encina.UnitTests/Caching/ServiceCollectionExtensionsTests.cs` | modify (not registered for `All` and `TopHeavy`, registered for `AccessPattern`) |

Tasks:

1. **Selector:** `AccessPattern` scores each warmer with `IWarmingAccessStatistics.GetHits` of its request type (`warmer is IWarmerRequestType`); a custom `ICacheWarmer` scores 0; the order is hits descending, then `Priority` ascending, then registration order, and the same top-percent count as `TopHeavy` applies (Design Choice 2). When every candidate scores 0 (or no statistics service is registered) the order is the `TopHeavy` order and the engine logs EventId 495 once (Design Choice 4).
2. **Engine:** resolves `IWarmingAccessStatistics?` from the planning scope with `GetService` (a missing service is the all-zero case) and passes it to the selector. `AddEncinaCaching` narrows the registration of the default statistics to `EnableCacheWarming` with `WarmingSelection == AccessPattern` (Design Choice 8), so `All` and `TopHeavy` record nothing.
3. **Tests:** ranking by hits, ties by priority, a custom warmer ranks by priority below warmers with hits, the all-zero fallback logs 495 and equals the `TopHeavy` set, a registered replacement implementation is used, a statistics implementation that throws fails the run visibly and never silently falls back (the exception is caught and logged by the hosted service, AGENTS.md section 3), the report counts only warmers that ran.
4. **Docs:** the issue names `docs/features/query-caching.md` (the EF Core `QueryCacheInterceptor` page); the `[Cache]` handler page is `docs/features/handler-caching.md` (#269 Phase 3), so this plan deliberately deviates from the issue's file name in PR 1 and PR 4. `docs-writer` extends the section with `AccessPattern`, the cold-start limit and how to register a persisted `IWarmingAccessStatistics`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Phases 1 to 3 are merged: selection (All, TopHeavy), IWarmingAccessStatistics registered when warming is enabled, hits recorded by the behavior.
EventId 495 of CachingPatterns (450-499, registered by #266) is reserved for this issue.

TASK:
Add AccessPattern to WarmingSelection, rank warmers by recorded hits with the cold-start fallback, expose the request type of RequestCacheWarmer,
add EventId 495 and the tests; ask mechanical-fixer for PublicAPI, the manifest and the changelog and docs-writer for the docs.

KEY RULES:
- Order: hits descending, priority ascending, registration order; an all-zero set falls back to priority order and logs once.
- A statistics failure is never swallowed into a fallback.
- EventIds 494-495 only, packed.

REFERENCE FILES:
- src/Encina.Caching/Warming/WarmingSelector.cs
- src/Encina.Caching/Warming/CacheWarmingService.cs (from #269)
```

</details>

### Phase 5: Coverage targets and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. **No new `EncinaError`** (selection and statistics return values, never errors): no ADR-062 `ErrorCategory` applies. **EventIds** 494-495 only: 494 selection applied, 495 no access statistics yet (fallback). **Telemetry:** no new meter instrument; the counter `encina.cache.warming` of #269 keeps its outcomes (the unselected warmers are not part of the run). No tenant tag, because the statistics hold no tenant (ADR-054 D4; #4398 adds the tag per pair).
2. **Coverage targets (per flag).** Each row is entered in the manifest of the PR that adds the file and its tests (#1762). Numbers quoted from `origin/main`: `Behaviors/QueryCachingPipelineBehavior.cs` unit 98, guard 20, contract 0 (this plan keeps all three); `Configuration/CachingOptions.cs` guard 85 and no other flag; `ServiceCollectionExtensions.cs` has no `targets` row on `main`. Targets set by the #269 plan (Phase 4) for the files this plan touches: `CachingOptions.cs` guard raised from 85 to 90, `ServiceCollectionExtensions.cs` unit 90 and guard 0, engine unit 95 and integration 0, request warmer unit 95 and integration 40, log file unit 90; the rows below restate those numbers.

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Warming/WarmingSelector.cs` (PR 1, PR 4) | 100 | 0 | 0 | 0 | Pure function, fully unit tested; internal without `InternalsVisibleTo` for the guard project |
| `Configuration/WarmingSelection.cs`, `Abstractions/IWarmingAccessStatistics.cs`, `Warming/WarmedRequestType.cs`, `Warming/IWarmerRequestType.cs`, `Warming/CacheWarmingMarker.cs` | 0 | 0 | 0 | 0 | An enum, an interface, a record, an internal interface and a constant: no executable lines of their own |
| `Warming/WarmingAccessStatistics.cs` (PR 2) | 95 | 25 | 80 | 0 | Counting, the frozen set and the concurrency test are unit tested; the contract flag runs the real class through the interface; the guard flag reaches the two argument checks through the public interface resolved from DI (`WarmingAccessStatisticsGuardTests`), about a quarter of the executable lines |
| `Behaviors/QueryCachingPipelineBehavior.cs` (PR 3) | 98 | 20 | 0 | 0 | The recording branch is unit tested; the optional parameter adds no argument check, so the guard value stays 20 |
| `Warming/RequestCacheWarmer.cs` (PR 3, PR 4) | 95 | 0 | 0 | 40 | The marker and the request-type members run in the existing unit tests of the file (#269 numbers) |
| `Warming/CacheWarmingService.cs` (PR 1, PR 4) | 95 | 0 | 0 | 0 | The selection runs in the engine unit tests (#269 numbers; 35 integration only if #4396 PR 3 has merged) |
| `Diagnostics/CachePatternLog.Warming.cs` (PR 1, PR 4) | 90 | 0 | 0 | 0 | Ids 494-495 run in the engine tests |
| `Configuration/CachingOptions.cs` (PR 1) | no row | 90 | 0 | 0 | One more validating setter, run by `CachingOptionsWarmingSelectionGuardTests`; 90 is the #269 value |
| `ServiceCollectionExtensions.cs` (PR 1, PR 2, PR 4) | 90 | 0 | 0 | 0 | Copy lines and registrations run by the DI tests with both validations; the numbers are #269's and are never lowered (`target-ratchet`) |

   No property test is written: the selector is a small deterministic function covered by unit cases; no integration test: the strategies change which warmers run, not what a cache provider stores, and the #269 Redis integration test already proves the fill.
3. Verification per PR: `dotnet build --configuration Release` with zero warnings; `dotnet test` of `Encina.UnitTests` (Caching), `Encina.GuardTests`, and for PR 2 `Encina.ContractTests`; per-flag coverage and the CRAP table of changed methods (the selector stays below CRAP 10 by splitting ranking and counting).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
The code PRs are merged; each carried its own per-flag targets.

TASK:
Verify with coverage-report.cs that each PR's measured numbers reach the targets above; fix any gap through mechanical-fixer in a small PR; run
--check-justifications and the crap-gate table of changed methods.

KEY RULES:
- Never lower an existing target (target-ratchet); no log, metric or error code beyond the plan.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| Cache warming strategies (priority-first, traffic-driven warming) | The two strategies the issue asks for |
| AGENTS.md section 3 | Errors never swallowed in background infrastructure; opt-in features; no tenant or personal data in memory or logs |
| AGENTS.md section 5 (caching) | Behavior-level, identical on all 8 providers |
| ADR-054 D4 | Tenant tag only with multi-tenancy on; none here |
| ADR-062 | No new `EncinaError`, so no category to assign |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `QueryCachingPipelineBehavior` | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs:133-141, 179-186` | Where a served hit is recorded and which requests bypass the cache |
| `DefaultCacheKeyGenerator` | `src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs` | Why per-key counters cannot drive warming |
| `CacheWarmingService`, `RequestCacheWarmer`, `CachePatternLog`, `CachePatternMetrics` | `src/Encina.Caching` (from #269 and #266) | Engine, request type, logging |
| `CacheRefreshMarker` (#267) | `src/Encina.Caching` (from #267) | Precedent for the warming metadata marker |
| `Encina.Caching.Benchmarks` | `tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks/` | Hit-path benchmark |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (`CachingPatterns` 450-499, registered by #266 PR 3, plan batch registry `cache-patterns-1`) | 494-495 | 494 selection applied (selected of total, selection, percent); 495 no access statistics, fell back to priority order. Family: #266 450-455, #267 456-461, #268 462-468, #269 469-477, #271 478-487, #4396 488-489, #4398 490-493; 496-499 stay free. No new range |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 7 | 17 | New: `WarmingSelection`, `WarmingSelector` (PR 1), `IWarmingAccessStatistics`, `WarmingAccessStatistics`, `WarmedRequestType` (PR 2), `CacheWarmingMarker` (PR 3), `IWarmerRequestType` (PR 4); modified once per PR that edits it: options, DI (PR 1, PR 2, PR 4), engine (PR 1, PR 4), request warmer (PR 3, PR 4), behavior (PR 3), log file (PR 1, PR 4), `WarmingSelection` and `WarmingSelector` (PR 4), `PublicAPI.Unshipped.txt` (PR 1 to PR 4) |
| Tests | 6 | 9 | New: selector, options guard, statistics, statistics guard, contract and benchmark files; modified: options, engine (PR 1, PR 4), DI (PR 2, PR 4), behavior, request-warmer, behavior-guard, selector tests |
| Documentation | 0 | 2 | `docs/features/handler-caching.md` in PR 1 and PR 4 |
| Configuration | 4 | 4 | Four changelog fragments; four manifest rows (one per PR) |
| **Total** | **17** | **32** | Counted per pull request entry: 49 entries in 4 pull requests of 14, 11, 10 and 14 files, each at most 15 files and 1500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #4397 adds TopHeavy and AccessPattern warmer selection to cache warming (#269). #266 owns
CachePatternLog and the CachingPatterns range 450-499; this plan uses ids 494-495.

IMPLEMENTATION OVERVIEW:
1. PR 1: WarmingSelection (All, TopHeavy), WarmingTopPercent, WarmingSelector, the engine applies it.
2. PR 2: IWarmingAccessStatistics with the in-process default, registered only when warming is enabled.
3. PR 3: the behavior records served hits per request type; warming dispatches carry a marker and never count; hit-path benchmark.
4. PR 4: AccessPattern ranking with the cold-start fallback; docs.

KEY PATTERNS:
- Counters per request type of a registered request warmer, never per key, never with tenant or user data.
- Deterministic order; a statistics failure is never swallowed into a fallback; a null recorder changes nothing.
- Follow the maintainer's answers to Design Choices 1 to 4, given in your brief (answer C to Design Choice 1 drops PR 2 to PR 4).

REFERENCE FILES:
- src/Encina.Caching/Warming/CacheWarmingService.cs (from #269)
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Extends warming through the existing behavior for all 8 providers; statistics are in-process, provider-independent. |
| 2 | OpenTelemetry | ✅ Include | No new instrument: the unselected warmers are not part of the run; the #269 counter keeps its outcomes. No tenant tag (ADR-054 D4). |
| 3 | Structured Logging | ✅ Include | EventIds 494-495 in `CachingPatterns`. |
| 4 | Health Checks | ❌ N/A | The report the #4396 health check reads counts only the warmers that ran; no check is added here. |
| 5 | Validation | ✅ Include | `WarmingTopPercent` is validated from 1 to 100. |
| 6 | Resilience | ❌ N/A | The #269 per-warmer timeout and isolation apply to the selected warmers. |
| 7 | Distributed Locks | ❌ N/A | Statistics are per node; the lock is #4396. |
| 8 | Transactions | ❌ N/A | Counters and cache operations are independent. |
| 9 | Idempotency | ❌ N/A | Warming is idempotent. |
| 10 | Multi-Tenancy | ✅ Include | Counters hold no tenant; a bypassed request is not a hit (fail closed); warming runs with the tenant rules of #269 and #4398. |
| 11 | Module Isolation | ⏭️ Defer | Follows the module dimension of #746 (open). |
| 12 | Audit Trail | ❌ N/A | Infrastructure with no compliance event. |

**Deferred items:** module-scoped statistics follow #746 (open); a persisted statistics implementation is the application's, behind `IWarmingAccessStatistics` (Design Choice 1 option B is the maintainer's choice).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| #269 PR 1 to PR 3 (engine, `RequestCacheWarmer`, options, `docs/features/handler-caching.md`) | Pending | PR 1 edits the engine and options; PR 4 needs `RequestCacheWarmer` |
| #266 PR 3 (`CachePatternLog`, range 450-499) | Pending | Needed for the ids of PR 1 and PR 4 |
| #266 PR 1 and PR 3 and #267 PR 3, #268, #271 PR 5 (edits of `QueryCachingPipelineBehavior`) | Pending | PR 3 rebases on them; the order is in the #267 plan |
| #4396 and #4398 | No shared type | No type of either is used here; #4398 PR 2 also edits `RequestCacheWarmer.cs` and `CacheWarmingService.cs` (the tenant of each dispatched context), so this plan's PR 3 and PR 4 rebase on it if it merged first |
| Maintainer answers to Design Choices 1 to 4 | Pending | Asked when work starts |

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1 to 4) when work starts, records the answers under `## Maintainer Decisions`, then briefs the workers in PR order after #269 is merged.
2. Implement this plan after #4396 and #4398 (see "Family cache-warming-followups" in the Summary).
