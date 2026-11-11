# Implementation Plan: Health Checks for the Memory and Hybrid Cache Providers

> **Issue**: [#754](https://github.com/dlrivada/Encina/issues/754)
> **Type**: Feature
> **Complexity**: Medium (5 phases, about 24 files, split into 3 pull requests)
> **Estimated Scope**: about 300 lines of production code, about 700 lines of tests
> **Milestone**: Encina 1.0 (caching category, SPEC-000 REQ-027)

---

## Summary

Give the two caching providers that have no health check one, and settle how the four Redis-protocol providers are reported. Re-checked against today's code, only `Encina.Caching.Memory` and `Encina.Caching.Hybrid` lack a check. `Encina.Caching.Valkey`, `.Dragonfly`, `.Garnet` and `.KeyDB` are wrappers whose `AddEncina<Product>Cache` methods all call `AddEncinaRedisCache`, which registers `RedisHealthCheck` through `RegisterHealthCheck` (`src/Encina.Caching.Redis/ServiceCollectionExtensions.cs`, lines 163 and 171-178) when `RedisCacheOptions.ProviderHealthCheck.Enabled` is true. The 2026-10-05 comment on the issue looked only for `*HealthCheck*.cs` files inside those projects.

The plan adds `MemoryCacheHealthCheck` and `HybridCacheHealthCheck` (both `EncinaHealthCheck`), a `ProviderHealthCheck` property on `MemoryCacheOptions` and `HybridCacheProviderOptions`, their registration in `AddEncinaMemoryCache` and `AddEncinaHybridCache` / `AddEncinaHybridCacheWithOptions`, and one registration test per Redis-protocol product that proves exactly one check is registered.

**Affected packages:**

- `Encina.Caching.Memory` and `Encina.Caching.Hybrid` - check, options, registration, logging, README.
- `Encina.Caching.Redis` and the four wrappers - only if the maintainer answers Design Choices 1 or 3 with the changing option.
- `Encina.Messaging` - dependency only. `Encina.Caching` references `Encina.Messaging`, so Memory and Hybrid reach `EncinaHealthCheck` and `ProviderHealthCheckOptions` without a new `ProjectReference`.

**Provider category**: Caching, 8 providers (AGENTS.md section 5): Memory (L1), Hybrid (L1+L2), Redis, Valkey, Dragonfly, Garnet, KeyDB, Memcached (planned, #277). After this plan seven have a check; Memcached gets one when it is built (Design Choice 6).

**Sibling plans (health-checks family)**: this plan owns every change to the shared `ProviderHealthCheckOptions` type and the tag convention for caches (`encina`, `cache`, `<provider>`, `ready`). #755 (locks) and #622 (transports) use per-provider tags and follow the answer to Design Choice 3. #454 owns the probe endpoints that filter by tag.

---

## Design Choices

<details>
<summary><strong>1. How the four Redis-protocol providers are reported</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep sharing `RedisHealthCheck` and its name `encina-redis`** | No duplication, no change; the four products speak the Redis wire protocol and the check pings every endpoint (`RedisHealthCheck` documents this); one registration, one alert | Every dashboard says "redis" for Valkey, Dragonfly, Garnet and KeyDB |
| **B) Each wrapper pre-sets `ProviderHealthCheck.Name` and `Tags`** (these properties already exist; no new option is needed) | Accurate names and per-product alerting; the wrappers' four overloads route through the options-taking `AddEncinaRedisCache` | 16 wrapper methods change; the overloads without options must call the options overload with a no-op lock action |

### Chosen Option: **A - keep sharing `RedisHealthCheck`** (recommended, pending the maintainer)

### Rationale

- The issue proposed "a provider-name option"; the code already has `ProviderHealthCheckOptions.Name` and `Tags`, so Option B costs wrapper changes only, not a new type.
- Option B wins when an operator runs more than one of these products side by side and alerts per product.
- Class: Dilemma

</details>

<details>
<summary><strong>2. Depth of the in-process check for Memory and Hybrid</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Resolve the provider and confirm it is not disposed** | No side effect on the application's cache | Proves little; Hybrid with a dead L2 would still report Healthy |
| **B) Set, get and remove a probe key** | Proves the write path, serialization and the L2 write | Writes a probe key into the application's cache (memory pressure, eviction, an L2 write on every probe) |
| **C) `ICacheProvider.ExistsAsync` on a unique probe key never written** | Read-only; for Hybrid the read goes through `HybridCache.GetOrCreateAsync` with both cache writes disabled (`HybridCacheProvider.GetAsync`), so it reaches the distributed L2; for Memory it exercises the provider call path | Does not prove the write path; Memory gains little over A |

### Chosen Option: **C - read-only `ExistsAsync` of a probe key** (recommended, pending the maintainer)

### Rationale

- Network-backed L2 is the dependency worth probing, and a read reaches it without writing into the application's cache.
- Option B wins when a broken serializer or L2 write path must surface in the probe and the write is acceptable; Option A wins only when the check exists to show the registration worked.
- Class: Dilemma

</details>

<details>
<summary><strong>3. Default tags of the shared options</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each provider's options set their own tags** (`encina`, `cache`, `memory` and so on); `RedisCacheOptions` also sets `encina`, `cache`, `redis` | Local; fixes Redis and the four wrappers in this plan; no behavior change for other categories | Other categories fix themselves one by one (#755 does for the InMemory lock) |
| **B) Change the default of `ProviderHealthCheckOptions.Tags` to `["encina", "ready"]`** | One change for every provider | Alters the tag filters of every check built from the shared options; hosts that select `database` for probes would lose checks |

### Chosen Option: **A - each provider's options set their own tags** (recommended, pending the maintainer)

### Rationale

- Verified in code: `ProviderHealthCheckOptions.Tags` defaults to `["encina", "database", "ready"]` and `RedisCacheOptions.ProviderHealthCheck` (`RedisCacheProvider.cs` line 335) is a `new()` instance, so the Redis check is tagged `database` in practice; the `["encina", "cache", "redis", "ready"]` fallback inside `RedisHealthCheck` never applies because the options object is never null.
- Option B wins when the maintainer wants every provider relabelled in one step.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Base type, options property and result content</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Derive from `EncinaHealthCheck`; options property `public ProviderHealthCheckOptions ProviderHealthCheck { get; } = new();`** | Same as `RedisHealthCheck` and `NATSHealthCheck`; the base catches exceptions and returns the type name only | None for this scope |
| **B) Implement `IEncinaHealthCheck` directly** | Matches the lock checks | Repeats the try/catch and the no-message rule in each check |

### Chosen Option: **A - derive from `EncinaHealthCheck`**

### Rationale

- Failure results carry the exception type only; no `EncinaError.Message` or exception message reaches a result.
- Class: Trivial
- Settled by: AGENTS.md section 3 (`EncinaError.Message` never reaches health-check results) and `src/Encina.Caching.Redis/Health/RedisHealthCheck.cs`.

</details>

<details>
<summary><strong>5. When the registration decides</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Eager local options instance: invoke the configure action on a local `MemoryCacheOptions` / `HybridCacheProviderOptions`, register when `ProviderHealthCheck.Enabled`** | Same moment as `RegisterHealthCheck` for Redis; the same configure action still goes to `services.Configure` | The configure action runs twice |
| **B) Decide at resolution through `IOptions<T>`** | Single configure run | A second pattern in the family; the host cannot see at registration time whether a check exists |

### Chosen Option: **A - eager local options instance**

### Rationale

- Memory and Hybrid pass options through `services.Configure(configure)` today, so the check needs the instance up front; the InMemory lock package already does this in `AddEncinaDistributedLockInMemoryCore`.
- The registration is proven by a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes`.
- Class: Trivial
- Settled by: AGENTS.md section 3 (registration completeness) and `src/Encina.Caching.Redis/ServiceCollectionExtensions.cs` (`RegisterHealthCheck`).

</details>

<details>
<summary><strong>6. Memcached</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The Memcached provider ships its own check with #277** | The check is written against a provider that exists | The category is incomplete until #277 lands |
| **B) Pre-write a Memcached check now** | Category looks complete | Code against a package that does not exist |

### Chosen Option: **A - the check ships with the provider**

### Rationale

- No `Encina.Caching.Memcached` project exists in `src/`.
- Class: Settled
- Settled by: AGENTS.md section 5 (caching row: every provider supports the category's operations) and #277.

</details>

---

## Implementation Phases

### Phase 1: Memory provider check (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Caching.Memory/Health/MemoryCacheHealthCheck.cs`: `public sealed class MemoryCacheHealthCheck : EncinaHealthCheck`, `DefaultName = "encina-cache-memory"`, constructor `(ICacheProvider provider, ProviderHealthCheckOptions options)` with `ArgumentNullException.ThrowIfNull`, `CheckHealthCoreAsync` linking the caller token with `options.Timeout` and calling `provider.ExistsAsync("health:cache:" + Guid.NewGuid().ToString("N"), token)` (Design Choice 2); timeout returns `Unhealthy("<Name> health check timed out after <n>s")`.
2. **Modify** `MemoryCacheOptions` (declared in `src/Encina.Caching.Memory/MemoryCacheProvider.cs`): add `public ProviderHealthCheckOptions ProviderHealthCheck { get; } = new() { Tags = ["encina", "cache", "memory", "ready"] };` (Design Choice 3).
3. **Modify** `src/Encina.Caching.Memory/ServiceCollectionExtensions.cs`: in `AddEncinaMemoryCache` build a local `MemoryCacheOptions`, invoke `configure`, and call a new `internal RegisterHealthCheck(services, options)` that registers `options.ProviderHealthCheck` and `IEncinaHealthCheck` when enabled.
4. **Create** `src/Encina.Caching.Memory/Health/MemoryCacheHealthLog.cs` (Phase 4) and update `PublicAPI.Unshipped.txt` (`mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library. Issue #754 adds health checks to the caching providers that have none.
Encina.Caching.Memory references Encina.Caching, which references Encina.Messaging, so EncinaHealthCheck and
ProviderHealthCheckOptions (namespace Encina.Messaging.Health) are available without a new ProjectReference.

TASK:
1. Create MemoryCacheHealthCheck as described in the plan.
2. Add MemoryCacheOptions.ProviderHealthCheck with cache tags.
3. Register the check in AddEncinaMemoryCache through an internal RegisterHealthCheck helper.
4. Build and add every new public symbol to PublicAPI.Unshipped.txt.

KEY RULES:
- Derive from EncinaHealthCheck; failure results carry the exception type only.
- The probe never writes into the application's cache (read-only ExistsAsync of a unique key), unless the maintainer chose another option.
- Do not modify ProviderHealthCheckOptions in this phase.
- XML docs with an example; sealed classes; nullable enabled; zero warnings.

REFERENCE FILES:
- src/Encina.Caching.Redis/Health/RedisHealthCheck.cs
- src/Encina.Caching.Redis/ServiceCollectionExtensions.cs (RegisterHealthCheck)
- src/Encina.Caching.Memory/MemoryCacheProvider.cs
- src/Encina.Caching.Memory/ServiceCollectionExtensions.cs
- src/Encina.Messaging/Health/EncinaHealthCheck.cs
```

</details>

### Phase 2: Hybrid provider check (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Caching.Hybrid/Health/HybridCacheHealthCheck.cs`, same shape as Phase 1 (`DefaultName = "encina-cache-hybrid"`); the probe is `ExistsAsync`, which for Hybrid reads L1 and L2 with both writes disabled.
2. **Modify** `HybridCacheProviderOptions` (declared in `src/Encina.Caching.Hybrid/HybridCacheProvider.cs`): add `ProviderHealthCheck` with tags `encina`, `cache`, `hybrid`, `ready`.
3. **Modify** `src/Encina.Caching.Hybrid/ServiceCollectionExtensions.cs`: register in both `AddEncinaHybridCache` and `AddEncinaHybridCacheWithOptions` through one shared internal helper, so the two overloads cannot diverge.
4. Log class (Phase 4), `PublicAPI.Unshipped.txt`, manifest.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged. Hybrid has two registration methods; both must register the check once.

TASK:
Create HybridCacheHealthCheck, add HybridCacheProviderOptions.ProviderHealthCheck, register from both overloads
through one internal helper, add PublicAPI lines.

KEY RULES:
- A read-only probe: HybridCacheProvider.GetAsync already disables local and distributed writes for its read.
- Registering twice (both overloads called) must not duplicate the check: use TryAddEnumerable semantics or a guard.
- Failure results carry the exception type only; zero warnings.

REFERENCE FILES:
- src/Encina.Caching.Hybrid/HybridCacheProvider.cs (GetAsync, ExistsAsync, HybridCacheProviderOptions)
- src/Encina.Caching.Hybrid/ServiceCollectionExtensions.cs
- src/Encina.Caching.Memory/Health/MemoryCacheHealthCheck.cs
```

</details>

### Phase 3: Redis-protocol providers (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

1. **Tests first**: one registration test per product (`AddEncinaValkeyCache`, `AddEncinaDragonflyCache`, `AddEncinaGarnetCache`, `AddEncinaKeyDBCache`) with `ValidateOnBuild` and `ValidateScopes`, asserting exactly one `IEncinaHealthCheck` of type `RedisHealthCheck` and that `Enabled = false` registers none. These pass without production changes and close the verification item of the issue.
2. **Modify** `src/Encina.Caching.Redis/RedisCacheProvider.cs` (`RedisCacheOptions`, line 335): initialize `ProviderHealthCheck` with tags `encina`, `cache`, `redis`, `ready` (Design Choice 3, Option A).
3. **If the maintainer chooses Option B of Design Choice 1**: each wrapper's four `AddEncina<Product>Cache` overloads set `ProviderHealthCheck.Name` (`encina-valkey` and so on) and the product tag through the options overload of `AddEncinaRedisCache`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Valkey, Dragonfly, Garnet and KeyDB are wrappers over AddEncinaRedisCache; the Redis package registers RedisHealthCheck.

TASK:
Write the four registration tests, then apply the tag change to RedisCacheOptions. Apply the per-product naming only
if the plan's Maintainer Decisions section says Option B.

KEY RULES:
- Tests execute the real AddEncina<Product>Cache extension methods; no reflection-only tests.
- A tag change must keep RedisHealthCheckTests green; update expectations deliberately.
- Do not touch ProviderHealthCheckOptions defaults unless Design Choice 3 chose Option B.

REFERENCE FILES:
- src/Encina.Caching.Valkey/ServiceCollectionExtensions.cs (and the other three wrappers)
- src/Encina.Caching.Redis/ServiceCollectionExtensions.cs
- src/Encina.Caching.Redis/RedisCacheProvider.cs
- tests/Encina.UnitTests/Caching/Redis/Health/RedisHealthCheckTests.cs
```

</details>

### Phase 4: Observability and structured logging

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Caching.Memory/Health/MemoryCacheHealthLog.cs` with `HealthCheckFailed(ILogger, string failureType)` (EventId 3565) and `HealthCheckTimedOut(ILogger, double timeoutSeconds)` (EventId 3566), inside `CachingMemory` (3550-3599; 3550-3564 are used).
2. **Create** `src/Encina.Caching.Hybrid/Health/HybridCacheHealthLog.cs` with EventIds 3657 and 3658, inside `CachingHybrid` (3650-3699; 3650-3656 are used).
3. Log the exception type through `ForLogging()` only; no new range, so `EncinaEventIdAllocationTests` needs no map change.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
The Memory and Hybrid packages own EventId ranges 3550-3599 and 3650-3699, used up to 3564 and 3656.

TASK:
Add the two internal partial log classes with [LoggerMessage] methods and call them from the checks.

KEY RULES:
- Source generator only; EventIds packed sequentially after the last used id.
- Never log an exception message or EncinaError.Message; use ForLogging().
- CA1848 stays clean because the generator is used.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Caching.Memory/MemoryCacheProvider.cs (existing LoggerMessage methods)
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 5: Testing, documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. Unit tests per check in `tests/Encina.UnitTests/Caching/Memory/Health/` and `.../Hybrid/Health/`: healthy with the real provider, unhealthy when the provider throws, timeout, disabled option registers nothing, tags and name, no message leakage. Hybrid tests use a real `HybridCache` built from `AddHybridCache` with `AddDistributedMemoryCache`.
2. Guard tests for both constructors; contract test that every cache check honors `IEncinaHealthCheck` (name, non-empty tags, never throws).
3. Property, integration, load and benchmark: not required (deterministic in-process logic; the Redis-protocol products are covered by the existing Redis tests); each manifest `reason` says so.
4. `mechanical-fixer`: coverage manifests for the touched files of `Encina.Caching.Memory.json`, `Encina.Caching.Hybrid.json` (and `Encina.Caching.Redis.json` for the options change), changelog fragment `changelog.d/754-cache-health-checks.added.md`, PublicAPI lines.
5. `docs-writer`: Memory and Hybrid package READMEs, the caching section of `docs/guides/health-checks.md`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
The checks, options, registrations and logging exist. Tests, manifests and documentation remain.

TASK:
Write the tests above, measure per-flag coverage of every touched file, record targets with one-sentence
justifications in the manifests, and update the documentation through the specialists.

KEY RULES:
- Tests instantiate real providers; Shouldly through Encina.Testing.Shouldly; FakeTimeProvider; AAA.
- CRAP of every added method 10 or less; keep CheckHealthCoreAsync small.
- Documentation never types coverage figures; changelog fragment under changelog.d/ only.

REFERENCE FILES:
- tests/Encina.UnitTests/Caching/Redis/Health/RedisHealthCheckTests.cs
- .github/coverage-manifest/Encina.Caching.Memory.json
- .github/coverage-manifest/Encina.Caching.Hybrid.json
- docs/guides/health-checks.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 5 (caching) | A feature touching the category is consistent across all 8 providers |
| AGENTS.md section 3 | No error or exception message in results; registration completeness proven by a DI test |
| AGENTS.md section 6, function 4 | A checkable dependency exposes a health check |
| SPEC-000 REQ-027 | All 8 caching providers are in 1.0 scope |
| `Microsoft.Extensions.Caching.Hybrid` | `HybridCache.GetOrCreateAsync` flags disable the writes of a read |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `EncinaHealthCheck`, `ProviderHealthCheckOptions`, `HealthCheckResult` | `src/Encina.Messaging/Health/` | Base type, options, result |
| `RedisHealthCheck` and `RegisterHealthCheck` | `src/Encina.Caching.Redis/` | Reference for the check and its registration |
| `QueryCacheHealthCheck` | `src/Encina.Caching/Health/QueryCacheHealthCheck.cs` | Second cache-side `EncinaHealthCheck` |
| `MemoryCacheProvider`, `MemoryCacheOptions` | `src/Encina.Caching.Memory/MemoryCacheProvider.cs` | Provider and options to extend |
| `HybridCacheProvider`, `HybridCacheProviderOptions` | `src/Encina.Caching.Hybrid/HybridCacheProvider.cs` | Provider and options to extend |
| Wrapper extensions | `src/Encina.Caching.{Valkey,Dragonfly,Garnet,KeyDB}/ServiceCollectionExtensions.cs` | Call `AddEncinaRedisCache`; registration tests |
| `ICacheProvider.ExistsAsync` | `src/Encina.Caching/Abstractions/ICacheProvider.cs` | The read-only probe |
| `EventIdRanges.CachingMemory`, `CachingHybrid` | `src/Encina/Diagnostics/EventIdRanges.cs` | Existing ranges; ids 3565-3566 and 3657-3658 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching.Memory` | 3550-3599 (existing) | 3550-3564 used; this plan takes 3565-3566 |
| `Encina.Caching.Hybrid` | 3650-3699 (existing) | 3650-3656 used; this plan takes 3657-3658 |

No new range is registered.

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 4 | 5 | Two checks and two log classes; the two provider files (options), two `ServiceCollectionExtensions.cs`, `RedisCacheProvider.cs` |
| Tests | 7 | 1 | Unit (Memory, Hybrid, four registration tests in one file), guard, contract; `RedisHealthCheckTests.cs` expectations |
| Documentation | 0 | 3 | Memory README, Hybrid README, `docs/guides/health-checks.md` |
| Configuration | 1 | 5 | Changelog fragment; two `PublicAPI.Unshipped.txt`, three manifests |
| **Total** | **12** | **14** | 26 files: three pull requests, PR 1 Memory (about 10 files), PR 2 Hybrid (about 10), PR 3 Redis-protocol tests, tags and docs (about 6); each under 15 files |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #754 adds health checks to the Memory
and Hybrid cache providers and verifies that Valkey, Dragonfly, Garnet and KeyDB register RedisHealthCheck.

IMPLEMENTATION OVERVIEW:
1. MemoryCacheHealthCheck and HybridCacheHealthCheck (EncinaHealthCheck) probing a read-only ExistsAsync.
2. ProviderHealthCheck property on MemoryCacheOptions and HybridCacheProviderOptions with cache tags.
3. Registration helper called from AddEncinaMemoryCache and both Hybrid overloads when Enabled.
4. Registration tests for the four Redis-protocol wrappers; RedisCacheOptions cache tags.
5. EventIds 3565-3566 (Memory) and 3657-3658 (Hybrid) inside existing ranges.
6. Unit, guard and contract tests; manifests, README, guide and changelog through the specialists.
Three pull requests: Memory, Hybrid, Redis-protocol.

KEY PATTERNS:
- Failure results carry the exception type name only; the timeout comes from ProviderHealthCheckOptions.Timeout.
- The options property is `{ get; } = new()`; registration is gated by Enabled at registration time.
- This plan owns changes to ProviderHealthCheckOptions; apply Design Choices 1, 2 and 3 as the Maintainer Decisions section says.

REFERENCE FILES:
- src/Encina.Caching.Redis/Health/RedisHealthCheck.cs
- src/Encina.Caching.Redis/ServiceCollectionExtensions.cs
- src/Encina.Caching.Memory/ServiceCollectionExtensions.cs
- src/Encina.Caching.Hybrid/ServiceCollectionExtensions.cs
- src/Encina.Messaging/Health/EncinaHealthCheck.cs
- .github/coverage-manifest/Encina.Caching.Memory.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | A probe must reflect real-time state and never writes a cached value |
| 2 | OpenTelemetry | ⏭️ Defer | Health-check telemetry is one cross-provider concern owned by the probe work in #454; no metric here |
| 3 | Structured Logging | ✅ Include | Phase 4: EventIds 3565-3566 and 3657-3658, exception type only |
| 4 | Health Checks | ✅ Include | This is the feature |
| 5 | Validation | ❌ N/A | No input |
| 6 | Resilience | ✅ Include | Timeout from `ProviderHealthCheckOptions.Timeout` bounds the probe |
| 7 | Distributed Locks | ❌ N/A | No shared state change |
| 8 | Transactions | ❌ N/A | No mutation |
| 9 | Idempotency | ❌ N/A | Probes are repeatable |
| 10 | Multi-Tenancy | ❌ N/A | Provider-level probe |
| 11 | Module Isolation | ❌ N/A | Provider-level probe |
| 12 | Audit Trail | ❌ N/A | Infrastructure probe |

**Deferred items:**

- OpenTelemetry for health checks: the probe work in #454 and the umbrella #622.

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `EncinaHealthCheck` and `ProviderHealthCheckOptions` | ✅ Available | `src/Encina.Messaging/Health/` |
| `RegisterHealthCheck` reference | ✅ Available | Redis cache package |
| EventId ranges 3550-3599 and 3650-3699 | ✅ Available | Free ids after 3564 and 3656 |
| Maintainer answers to Design Choices 1, 2, 3 | Pending | Asked when work starts |
| Memcached provider | ⏭️ Later | #277 delivers its own check |

No blocking prerequisites.

---

## Next Steps

1. The orchestrator asks the maintainer Design Choices 1, 2 and 3 when work starts, records the answers under `## Maintainer Decisions`, then briefs three workers (PR 1 Memory, PR 2 Hybrid, PR 3 Redis-protocol).
2. After merge, comment on #754 with the result and on #755 and #622 with the tag convention that was chosen.
