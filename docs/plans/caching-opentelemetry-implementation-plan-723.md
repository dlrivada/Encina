# Implementation Plan: OpenTelemetry for the Caching Providers and the Pub/Sub Backplane (Outcome Metrics, No Data-Bearing Keys)

> **Issue**: [#723](https://github.com/dlrivada/Encina/issues/723)
> **Type**: Feature (observability)
> **Related**: ADR-054 (D3 names, D4 tenant tag, D6 caches at Encina operation level, never data-bearing keys), #273 (cache metrics catalog, planned separately: boundary in Design Choice 4), #266 (single-flight, owns `CachePatternMetrics` on `Meter("Encina.Caching")`), #277 (Memcached provider), #2676 (tenant tag helper), #1788 (static scan), #1790 (duplicate sources), #906, ADR-062
> **Complexity**: Medium (one decorator extended, one added; the main change is removing a privacy defect)
> **Estimated Scope**: ~330 lines of production code + ~850 lines of tests, in three pull requests of at most 15 files

---

## Summary

### What exists today (read on 2026-11-20, main `e31de98cb`)

- `AddEncinaOpenTelemetry` already decorates `ICacheProvider` with `InstrumentedCacheProvider` (`src/Encina.OpenTelemetry/QueryCache/InstrumentedCacheProvider.cs`): spans `encina.querycache.get|set|remove|remove_by_pattern|exists|get_or_set|set_sliding|refresh`, a hit or miss outcome on `get`, no metric. Because it wraps the interface, all 8 cache providers are traced when the decorator is applied: Memory, Hybrid, Redis, and Valkey, Dragonfly, Garnet and KeyDB, which call `AddEncinaRedisCache` and so use `RedisCacheProvider` (`src/Encina.Caching.Valkey/ServiceCollectionExtensions.cs`). Memcached (#277) is not in `src`.
- **The spans carry the raw cache key**: `querycache.key` is set from the `key` argument on seven spans and `querycache.pattern` on one. Keys built by `DefaultCacheKeyGenerator` contain `t:{tenantId}` and `u:{userId}` (`src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs` documents both as personal data that no log may contain). This breaks ADR-054 D6.3 and `AGENTS.md` section 3.
- The ActivitySource name `Encina.QueryCache` has two owners: the decorator above and `src/Encina.EntityFrameworkCore/Diagnostics/QueryCacheActivitySource.cs`; `WithEncina()` subscribes it once. ADR-054 settled point 8 requires one owner per name (#1790).
- `IPubSubProvider` (`src/Encina.Caching/Abstractions/IPubSubProvider.cs`, implemented by `MemoryPubSubProvider` and `RedisPubSubProvider`, plus `FakePubSubProvider` in `Encina.Testing.Fakes`) has no instrumentation, so the backplane is invisible.
- No `Meter` exists in `Encina.Caching` today; #266 plans `CachePatternMetrics` on `Meter("Encina.Caching", "1.0")`. The cache providers are registered as singletons (`TryAddSingleton<ICacheProvider, ...>`).
- `ICacheProvider` has eight methods; `GetOrSetAsync` takes a factory, so the decorator can tell a hit (factory not called) from a miss.
- Tests today: `tests/Encina.UnitTests/OpenTelemetry/QueryCache/InstrumentedCacheProviderTests.cs`, `tests/Encina.GuardTests/Infrastructure/OpenTelemetry/InstrumentedCacheProviderGuardTests.cs` and `...MethodGuardTests.cs`.

### What this plan delivers

- Removes the key and pattern tags; replaces them with `encina.cache.key_prefix` (the segment before the first `:`, only when a `:` exists, at most 32 characters) (Design Choice 3).
- Moves the decorator's source to `Encina.Caching` and the span names to `encina.cache.*` (Design Choice 2).
- Two instruments: `encina.cache.operations` (counter; tags `encina.cache.operation`, `encina.cache.outcome` = `hit`, `miss`, `success`, `error`, `encina.cache.system`, `error.type` on errors) and `encina.cache.operation.duration` (histogram, unit `s`) on `Meter("Encina.Caching")`.
- `InstrumentedPubSubProvider`: spans for publish, subscribe and unsubscribe and a consumer span per delivered message (the subscription handler is wrapped), and counter `encina.cache.pubsub.messages` (tag `direction` = `publish`, `receive`). The channel name is never emitted.
- Tenant tag through the helper of #2676 (the providers are singletons, so no scoped service is injected).
- Shared pieces and owner: this plan owns the decorators and the instruments named above; #273 adds the rest of its catalog (sets, removals, evictions, sizes, stampede, stale served, backplane actions) in its own file; #266 owns `CachePatternMetrics`.
- Affected packages: `Encina.OpenTelemetry`. Provider category: Caching (8): all 8 by interface; proof by contract test over Memory, Hybrid and Redis implementations plus the identity mapping of the four Redis-protocol providers.

---

## Design Choices

<details>
<summary><strong>1. Where the cache instrumentation lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep the decorator in `Encina.OpenTelemetry`, extend it and add a pub/sub decorator** | Exists and is applied by one call; covers every provider including Memcached when it lands; no provider code | Order of registration (provider before `AddEncinaOpenTelemetry`); a host without the package gets nothing |
| **B) Decorators in `Encina.Caching`, applied by a new registration** | Telemetry without the OpenTelemetry package; same package as `CachePatternMetrics` | A new public registration and a second decoration order; moves tested code |
| **C) Instrumentation inside each provider** | Provider-specific detail (L1 and L2 of Hybrid) | 8 copies; ADR-054 D6.1 asks for Encina operation level, not driver level |

### Chosen Option: **A - Extend in place** (recommended, pending the maintainer)

### Rationale

- A wins on cost and coverage today; B wins if the maintainer wants cache telemetry for hosts that do not reference `Encina.OpenTelemetry`.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Source and meter names</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Source and meter `Encina.Caching`; span names `encina.cache.*`** | One owner per name once the decorator leaves `Encina.QueryCache`; the abstraction's package, as #266 and #741 do | Renames existing spans (pre-1.0, done completely) |
| **B) Keep `Encina.QueryCache`** | No rename | Two owners of one source (ADR-054 settled point 8) |
| **C) `Encina.OpenTelemetry`** | The emitting package | Mixes cache telemetry with the package name |

### Chosen Option: **A - `Encina.Caching`** (recommended, pending the maintainer)

### Rationale

- A wins for subscribers that enable Encina by package; the EF Core query cache keeps `Encina.QueryCache`. `WithEncina()` adds `AddSource("Encina.Caching")` and `AddMeter("Encina.Caching")`. C wins only if #1787 decides that decorator packages own their names.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. What replaces the key in telemetry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Nothing about the key** | Safest | The issue's `cache.key_prefix` is lost |
| **B) The first segment before `:` (only when a `:` exists, at most 32 characters)** | Groups operations by key family (`encina`, `orders`) with bounded cardinality | An application whose first segment is data (a template that starts with an id) leaks it; mitigated by the length cap and the documentation |
| **C) The redacted key from `CacheKeyRedactor`** | Reuses the helper | Still carries template-substituted values and the hash part; cardinality unbounded |

### Chosen Option: **B - First segment** (recommended, pending the maintainer)

### Rationale

- B wins when keys follow the Encina generators (`encina:t:...`) or a family-first template; A wins for a deployment whose keys start with identifiers. The pattern of `RemoveByPatternAsync` is treated the same way. A static test fixes ADR-054 D6.3 and #1788 scans for regressions.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Boundary with #273 and the provider label</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) This plan emits `encina.cache.operations` and `encina.cache.operation.duration` (hit ratio derives from the outcome tag); #273 adds the remaining instruments of its issue; the label `encina.cache.system` is derived from the provider type (`memory`, `hybrid`, `redis`); Valkey, Dragonfly, Garnet and KeyDB appear as `redis` because they run `RedisCacheProvider`** | No duplicate instruments; no public API | The product name of a Redis-protocol server is not visible |
| **B) Also emit dedicated `hits` and `misses` counters now** | Matches the #273 text | Duplicates #273 and doubles writes |
| **C) A public `CacheProviderName` registered by each satellite so Garnet reports `garnet`** | Exact product label | Public API in `Encina.Caching`, four satellite edits |

### Chosen Option: **A - Outcome tag, type-derived label** (recommended, pending the maintainer)

### Rationale

- A wins now; C wins if operators must tell Garnet from Redis in one dashboard (a small follow-up: the label is one tag); B only if #273 is dropped. #273's plan must reconcile with this boundary.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Cost, time, tenant and errors</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `HasListeners()` for spans, `Instrument.Enabled` for metrics, injected `TimeProvider`, tenant from the helper of #2676, error tag is the exception type name** | Zero cost without a listener; no scoped service in a singleton decorator | One more optional parameter |
| **B) Always measure** | Simpler | Pays per call |

### Chosen Option: **A - Guarded**

### Rationale

- ADR-054 settled points 2 and 7 and D4; `AGENTS.md` section 3.

**Class:** Trivial
**Settled by:** ADR-054 settled points 2 and 7 and D4; `AGENTS.md` section 3

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | No new `EncinaError` code (a cache exception propagates as before), so no ADR-062 category; no logging, no EventId (block 6380-6399 unused) | Settled | ADR-062; `AGENTS.md` section 7 |
| C2 | A failing cache call is recorded and rethrown unchanged; telemetry never changes a result or swallows an error | Settled | `AGENTS.md` section 3 |
| C3 | No key, value or channel is emitted; no tenant-qualified id is built, so the reserved `default` segment does not apply | Settled | ADR-054 D6.3, D4 |
| C4 | Hybrid L1/L2 detail is not emitted; it is a provider internal | Dilemma (recommended, pending the maintainer) | #273 (`cache_level`) decides it |

---

## Implementation Phases

### Phase 1: Safe Spans and Outcome Metrics (pull request C1)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.OpenTelemetry/Telemetry/CacheTelemetry.cs` (`internal static`): source and meter `Encina.Caching`, instruments, `KeyPrefix(string?)` (first segment rules of Design Choice 3), `System(ICacheProvider inner)` mapping the type namespace to `memory`, `hybrid`, `redis`, otherwise `other`.
2. **Modify** `QueryCache/InstrumentedCacheProvider.cs`: remove `querycache.key` and `querycache.pattern`; rename spans to `encina.cache.*`; add outcome (`GetOrSetAsync` miss detected by the factory wrapper), instruments, optional trailing `TimeProvider? timeProvider = null`.
3. **Modify** `ServiceCollectionExtensions.cs` (`AddSource("Encina.Caching")`, `AddMeter("Encina.Caching")`, remove nothing else).
4. Tests: edit `InstrumentedCacheProviderTests.cs` (no span tag contains the key or any substring of `t:tenant`/`u:user`; prefix rules; outcomes; `error.type` is the type name, never the message; counter and histogram; no work without listener); new `tests/Encina.UnitTests/OpenTelemetry/Telemetry/CacheTelemetryTests.cs`; edit the two guard test files (null `timeProvider` is valid: valid-result test); manifest `Encina.OpenTelemetry.json` and changelog `changelog.d/723-cache-telemetry.changed.md` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #723, plan docs/plans/caching-opentelemetry-implementation-plan-723.md, phase 1.
InstrumentedCacheProvider currently tags the raw cache key (t:{tenant}, u:{user}); ADR-054 D6.3 forbids it.

TASK:
1. Create CacheTelemetry (internal): ActivitySource and Meter "Encina.Caching", encina.cache.operations,
   encina.cache.operation.duration (unit s), KeyPrefix, System.
2. Edit InstrumentedCacheProvider: no key or pattern tag, spans encina.cache.*, outcome and metrics, optional TimeProvider.
3. Subscribe source and meter in WithEncina. Update unit and guard tests; manifest and changelog through mechanical-fixer.

KEY RULES:
- Never emit a key, a pattern, a value, EncinaError.Message or an exception message. Zero cost without a listener.
- Rethrow exceptions unchanged; optional parameters last (CS1737); existing "new InstrumentedCacheProvider(inner)" call sites compile.

REFERENCE FILES:
- src/Encina.OpenTelemetry/QueryCache/InstrumentedCacheProvider.cs, src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

---

### Phase 2: Pub/Sub Decorator and Tenant Tag (pull request C2, tenant part after #2676)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.OpenTelemetry/QueryCache/InstrumentedPubSubProvider.cs` (`internal sealed`, `IPubSubProvider`): spans `encina.cache.pubsub.publish|subscribe|unsubscribe`, a consumer span `encina.cache.pubsub.receive` around the wrapped handler, counter `encina.cache.pubsub.messages`; the handler wrapper preserves exceptions; no channel is emitted. Register with `DecorateService<IPubSubProvider>`.
2. Tenant tag through the helper of #2676 in `CacheTelemetry` (separate small commit; this part waits for #2676).
3. Tests: new `tests/Encina.UnitTests/OpenTelemetry/QueryCache/InstrumentedPubSubProviderTests.cs` (real `MemoryPubSubProvider`: publish then receive emits both counters and spans; handler exception still propagates), `tests/Encina.GuardTests/Infrastructure/OpenTelemetry/InstrumentedPubSubProviderGuardTests.cs` (null inner, channel, handler, null time provider valid); edit `ServiceCollectionExtensionsTests.cs` (provider built with `ValidateOnBuild` and `ValidateScopes` resolves both decorated singletons); manifest and changelog through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #723 plan docs/plans/caching-opentelemetry-implementation-plan-723.md, phase 2. Phase 1 merged (CacheTelemetry exists).

TASK:
1. Add InstrumentedPubSubProvider and register it in AddEncinaOpenTelemetry.
2. Wrap subscription handlers so each delivery gets a consumer span and counts one message.
3. Tests with the real MemoryPubSubProvider; guard and DI tests.

KEY RULES:
- No channel name, payload or tenant text in any tag; singletons only, never inject a scoped service.
- The decorator never swallows a handler exception.

REFERENCE FILES:
- src/Encina.Caching/Abstractions/IPubSubProvider.cs, src/Encina.Caching.Memory/MemoryPubSubProvider.cs
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 3: Proof Across Providers (pull request C3)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.ContractTests/Infrastructure/OpenTelemetry/CacheTelemetryContractTests.cs`: Memory and Hybrid and a Redis-backed provider (fake multiplexer) emit the same instrument names, tag keys and outcomes for the same call sequence; the four Redis-protocol registrations report `redis`.
2. **Create** `tests/Encina.IntegrationTests/Observability/OpenTelemetry/CacheTelemetryRedisIntegrationTests.cs` with the Redis fixture of the caching tests (shared collection, `ClearAllDataAsync()` equivalent): real Redis get, set, pub/sub delivery and the absence of the key in every tag.
3. `.github/coverage-manifest/Encina.OpenTelemetry.json` through `mechanical-fixer`; README section through `docs-writer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #723 plan docs/plans/caching-opentelemetry-implementation-plan-723.md, phase 3. Phases 1 and 2 merged.

TASK:
1. Contract test over the cache providers that exist (Memory, Hybrid, Redis); integration test on real Redis.
2. Manifest through mechanical-fixer, README through docs-writer.

KEY RULES:
- Shared collection fixtures only; tests execute real providers and real decorators; no Thread.Sleep.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure (Redis caching tests), src/Encina.Caching.Redis/ServiceCollectionExtensions.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| ADR-054 | Settled points 2, 4, 7, 8; D3, D4, D6 | Names, error text, scope, no data-bearing keys |
| `AGENTS.md` sections 3 and 5 | Redaction of keys; cache category consistency | All 8 providers |
| OpenTelemetry general conventions | Duration unit `s`, low-cardinality tags | Instruments |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `InstrumentedCacheProvider` | `src/Encina.OpenTelemetry/QueryCache/` | Extended |
| `CacheKeyRedactor` | `src/Encina.Caching/KeyGeneration/` | Reference for what keys contain |
| `MemoryPubSubProvider`, `RedisPubSubProvider` | `src/Encina.Caching.Memory/`, `src/Encina.Caching.Redis/` | Decorated |
| `DecorateService` | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` | Registration |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | No logging code (register row C1) |

### Estimated File Count

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| C1 | Phase 1 | 1 | 2 | 1 | 3 | 1 | 1 (changelog) | 9 | ~520 |
| C2 | Phase 2 | 1 | 1 | 2 | 1 | 1 | 1 (changelog) | 7 | ~480 |
| C3 | Phase 3 | 0 | 0 | 2 | 0 | 1 | 1 (README) | 4 | ~330 |
| **Total** | | **2** | **3** | **5** | **4** | **3** | **3** | **20** | **~1,330** |

Columns sum: 2 + 3 + 5 + 4 + 3 + 3 = 20 = 9 + 7 + 4. All new types are `internal`, so no PublicAPI change; no interface changes, so no fake changes (`FakeCacheProvider` and `FakePubSubProvider` untouched). `Encina.OpenTelemetry` has `InternalsVisibleTo` for UnitTests, GuardTests, ContractTests and IntegrationTests, so internal decorators are testable by all four.

Per-file coverage targets:

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `Telemetry/CacheTelemetry.cs` | 95 | 40 | 0 | 0 | 0 | Prefix rules, system mapping and instruments are table-driven; guard covers null inputs as valid results; internal static, no contract |
| `QueryCache/InstrumentedCacheProvider.cs` (existing 65 unit, 77 guard) | 85 | 77 | 20 | 0 | 40 | Every method's hit, miss, error and no-listener path is driven; guard stays; contract and integration newly cover three providers and real Redis |
| `QueryCache/InstrumentedPubSubProvider.cs` | 85 | 70 | 20 | 0 | 40 | Publish, subscribe, receive and handler-exception paths with the real memory provider; guard covers constructor and argument checks |
| `ServiceCollectionExtensions.cs` (existing 90, 35) | 90 | 35 | 0 | 0 | 0 | Targets stay; the new registrations are driven by the DI test |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #723 - OpenTelemetry for the caching providers - in three small pull requests.

PROJECT CONTEXT:
- Encina .NET 10 / C# 14, pre-1.0, no compatibility layers. Edit with Edit/Write only; manifests, changelog and PublicAPI through
  mechanical-fixer; README through docs-writer.
- ADR-054: lowercase dotted instrument names, seconds, no _total, error type only, never data-bearing keys, zero cost without a
  listener, tenant tag only when multi-tenancy is on (helper of #2676).

IMPLEMENTATION OVERVIEW:
1. C1: remove the raw key tags from InstrumentedCacheProvider, add encina.cache.* spans, outcome and the two instruments,
   source and meter Encina.Caching.
2. C2: InstrumentedPubSubProvider with consumer span per delivered message.
3. C3: contract test over Memory, Hybrid and Redis; integration test on real Redis.

KEY PATTERNS:
- Never emit a key, pattern, value, channel or message text. No scoped service in a singleton decorator.
- #273 adds the rest of the metric catalog; do not add hits, misses, sizes or evictions here.

REFERENCE FILES:
- src/Encina.OpenTelemetry/QueryCache/InstrumentedCacheProvider.cs, src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs
- src/Encina.Caching/Abstractions/ICacheProvider.cs, IPubSubProvider.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | It instruments the cache, it does not add cached reads |
| 2 | OpenTelemetry | ✅ Phases 1-3 | This is the feature for the caching providers and the backplane |
| 3 | Structured Logging | ❌ N/A | No EventId is added; the key redaction of existing log sites is unchanged (register row C1) |
| 4 | Health Checks | ❌ N/A | Observes operations; provides no health status |
| 5 | Validation | ❌ N/A | No input |
| 6 | Resilience | ❌ N/A | Adds no external call and never retries |
| 7 | Distributed Locks | ❌ N/A | Acquires no lock |
| 8 | Transactions | ❌ N/A | Never commits or rolls back |
| 9 | Idempotency | ❌ N/A | A redelivered pub/sub message is counted again, which is correct |
| 10 | Multi-Tenancy | ✅ Phase 2 | Tenant tag only when multi-tenancy is on, through the helper of #2676; keys are never emitted |
| 11 | Module Isolation | ❌ N/A | No module data |
| 12 | Audit Trail | ❌ N/A | No personal data may reach telemetry; this plan removes an existing leak |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| ADR-054 accepted | ✅ On `main` | D3, D4, D6 |
| #2676 tenant helper | Open | Only the tenant part of Phase 2 waits |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Cache metrics catalog | #273 | Planned separately; boundary in Design Choice 4 |
| Single-flight and `CachePatternMetrics` | #266 | Shares `Meter("Encina.Caching")` |
| Memcached provider | #277 | Covered by the decorator when it lands |
| Duplicate sources | #1790, #1787 | The `Encina.QueryCache` double owner is resolved here for the decorator |

---

## Next Steps

1. Link this plan from #723 and ask the maintainer to answer Design Choices 1 to 4 and register row C4 when work starts.
2. Implement C1 first (it removes the key leak), then C2 and C3.
3. Maintainer action: amend the acceptance criteria of #723 that name `cache.key_prefix`, `cache.type` and a "hit/miss ratio metric" to the outcome of Design Choices 3 and 4 (this plan never comments on an issue).
