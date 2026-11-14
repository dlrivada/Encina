# Implementation Plan: Health Checks for the Memory and Hybrid Cache Providers

> **Issue**: [#754](https://github.com/dlrivada/Encina/issues/754)
> **Type**: Feature
> **Complexity**: Medium (5 phases, 31 files, split into 3 pull requests of 12, 12 and 7 files)
> **Estimated Scope**: about 300 lines of production code, about 700 lines of tests
> **Milestone**: Encina 1.0 (caching category, SPEC-000 REQ-027)

---

## Summary

Give the two caching providers that have no health check one, and settle how the four Redis-protocol providers are reported. Re-checked against today's code, only `Encina.Caching.Memory` and `Encina.Caching.Hybrid` lack a check. `Encina.Caching.Valkey`, `.Dragonfly`, `.Garnet` and `.KeyDB` are wrappers whose `AddEncina<Product>Cache` methods all call `AddEncinaRedisCache`, which registers `RedisHealthCheck` through `RegisterHealthCheck` (`src/Encina.Caching.Redis/ServiceCollectionExtensions.cs`, lines 163 and 171-178) when `RedisCacheOptions.ProviderHealthCheck.Enabled` is true. The 2026-10-05 comment on the issue looked only for `*HealthCheck*.cs` files inside those projects.

The plan adds `MemoryCacheHealthCheck` and `HybridCacheHealthCheck` (both `EncinaHealthCheck`), a `ProviderHealthCheck` property on `MemoryCacheOptions` and `HybridCacheProviderOptions`, their registration in `AddEncinaMemoryCache` and `AddEncinaHybridCache` (`AddEncinaHybridCacheWithOptions` delegates to it, so it is not touched), and one registration test per Redis-protocol product that proves exactly one check is registered. Every check receives the `ProviderHealthCheckOptions` instance of its own registration and probes its own provider (Design Choices 5 and 7), so several cache providers in one application cannot cross-wire.

**Affected packages:**

- `Encina.Caching.Memory` and `Encina.Caching.Hybrid` - check, options, registration, logging, README.
- `Encina.Caching.Redis` (cache tags, options capture in `RegisterHealthCheck`) and, only if the maintainer answers Design Choice 1 with Option B, the four wrappers (a fourth pull request).
- `Encina.Messaging` - dependency only. `Encina.Caching` references `Encina.Messaging`, so Memory and Hybrid reach `EncinaHealthCheck` and `ProviderHealthCheckOptions` without a new `ProjectReference`.

**Provider category**: Caching, 8 providers (AGENTS.md section 5): Memory (L1), Hybrid (L1+L2), Redis, Valkey, Dragonfly, Garnet, KeyDB, Memcached (planned, #277). After this plan seven have a check; Memcached gets one when it is built (Design Choice 6).

**Sibling plans (health-checks family)**: this plan owns every change to the shared `ProviderHealthCheckOptions` type and the tag convention for caches (`encina`, `cache`, `<provider>`, `ready`). #755 (locks) and #622 (transports) use per-provider tags and follow the answer to Design Choice 3, and the same per-registration options wiring as Design Choice 5 (recommended, pending the maintainer). #454 owns the probe endpoints that filter by tag.

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
- Unverified claim, verified in PR 2 before it is relied on: `HybridCache.GetOrCreateAsync` may treat an L2 read failure as a cache miss and call the factory, in which case a dead L2 would not make `ExistsAsync` throw and the check would report Healthy. `HybridCacheProvider.ExistsAsync` calls `GetAsync<object>`, whose `GetOrCreateAsync` passes `DisableLocalCacheWrite | DisableDistributedCacheWrite` (`HybridCacheProvider.cs` line 78). Phase 2 adds a task: run the probe against a faulting `IDistributedCache` substitute and record the outcome in this plan; unit tests do not use `AddDistributedMemoryCache` alone because it cannot fail. If the probe stays Healthy on a faulting L2, Option C does not meet the issue for Hybrid and the maintainer re-decides between B and C.
- The probe key is `health:cache:<guid>`, passed to the provider as is: neither `MemoryCacheProvider` nor `HybridCacheProvider` adds a key prefix (the prefixes in `Encina.Caching` belong to the query-cache pipeline) and `CacheKeyRedactor` is applied only when a provider logs a key. Trivial (no observable effect beyond the unique key).
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

### Chosen Option: **A - derive from `EncinaHealthCheck`** (recommended, pending the maintainer)

### Rationale

- Failure results carry the exception type only; no `EncinaError.Message` or exception message reaches a result.
- Option B also satisfies AGENTS.md section 3 (an own try/catch can return the type name only), so the cited rule does not decide; the convention points both ways (`RedisHealthCheck` derives from the base class, the lock checks implement the interface).
- Class: Dilemma

</details>

<details>
<summary><strong>5. Registration and options wiring (per-registration options)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Eager local options instance, per-registration capture: invoke the configure action on a local `MemoryCacheOptions` / `HybridCacheProviderOptions`, and when `ProviderHealthCheck.Enabled` register `IEncinaHealthCheck` with a factory that captures `options.ProviderHealthCheck`; the shared `ProviderHealthCheckOptions` type is NOT registered as a service** | Each check keeps its own `Name`, `Tags`, `Timeout` and `Enabled` whatever other providers are registered; the same configure action still goes to `services.Configure` | The configure action runs twice (a user-visible side effect when the action has side effects); differs from Redis today |
| **B) Copy Redis: `services.AddSingleton(options.ProviderHealthCheck)` plus `AddSingleton<IEncinaHealthCheck, ...>`** | Identical to `RegisterHealthCheck` in `src/Encina.Caching.Redis/ServiceCollectionExtensions.cs` (line 177) | Several providers in one application (Memory plus Redis, or Hybrid with a Redis L2, the normal Hybrid setup; the lock packages register the same type) each register a singleton of the same type and every check resolves the last one: Memory's check would take Redis's `Name`, `Tags`, `Timeout` and `Enabled`. `RedisHealthCheck` takes the bare `ProviderHealthCheckOptions?` (`RedisHealthCheck.cs` line 31) |
| **C) Decide at resolution through `IOptions<T>` or named options** | Single configure run | A second pattern in the family; the host cannot see at registration time whether a check exists |

### Chosen Option: **A - per-registration options by capture** (recommended, pending the maintainer)

### Rationale

- Memory and Hybrid pass options through `services.Configure(configure)` today, so the check needs the instance up front; the InMemory lock package plan (#755) uses the same capture, and #622 does for the transports.
- Option B is what Redis does today; it works only while one provider registers the type. PR 3 moves `RegisterHealthCheck` of Redis to the same capture (the file is touched anyway for the tags), so the four Redis-protocol products stop registering the shared type too; the lock packages are covered by a follow-up (follow-up issue to be opened, draft: artifacts/decisions/plan-batch/followups/lock-healthcheck-options-wiring.md).
- Proven by DI tests with `ValidateOnBuild` and `ValidateScopes`: Memory plus Redis and Hybrid plus Redis in one provider, each check reporting its own name and tags (PR 1 covers Memory plus a second check built from a different options instance, PR 2 Hybrid, PR 3 Redis plus Memory and Redis plus Hybrid).
- Class: Dilemma

</details>

<details>
<summary><strong>6. Memcached</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The Memcached provider ships its own check with #277** | The check is written against a provider that exists | The category is incomplete until #277 lands |
| **B) Pre-write a Memcached check now** | Category looks complete | Code against a package that does not exist |

### Chosen Option: **A - the check ships with the provider** (recommended, pending the maintainer)

### Rationale

- No `Encina.Caching.Memcached` project exists in `src/`.
- AGENTS.md section 5 (caching row) and #277 do not forbid Option B, so the cited rule does not decide; only the scope of the issue does.
- Class: Dilemma

</details>

<details>
<summary><strong>7. Probe target when several cache providers are registered</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The check takes `ICacheProvider`** | No registration change in the providers | `ICacheProvider` is registered with `TryAddSingleton`, so with Redis registered first the Memory check would probe the Redis provider; with Memory and Hybrid together, one check probes the other |
| **B) The check takes its concrete provider (`MemoryCacheProvider`, `HybridCacheProvider`, both public sealed classes)** | Each check probes its own provider whichever won `ICacheProvider`; the registration adds `TryAddSingleton<MemoryCacheProvider>()` and forwards `ICacheProvider` to it | The provider registration changes (one forwarding line per package) |

### Chosen Option: **B - concrete provider** (recommended, pending the maintainer)

### Rationale

- A health check that probes another provider reports the wrong dependency, which is the same cross-wiring defect as Design Choice 5 at the provider level.
- Option A wins when only one cache provider is ever registered; the issue's own example (Hybrid with a Redis L2) shows two are normal.
- The constructor shape becomes `(MemoryCacheProvider provider, ProviderHealthCheckOptions options, TimeProvider? timeProvider = null, ILogger<MemoryCacheHealthCheck>? logger = null)` (Hybrid analogous), with `ArgumentNullException.ThrowIfNull` on the first two.
- Class: Dilemma

</details>

<details>
<summary><strong>8. Default enablement of the new checks</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Enabled = true` by default** (the default of `ProviderHealthCheckOptions`, as for Redis) | Parity with Redis and the other providers; the check still runs only when the host calls `AddEncinaHealthChecks`; the issue example sets `Enabled = true` explicitly | Memory and Hybrid users get the registration without asking |
| **B) `Enabled = false` by default for the two new checks** | Matches "every feature is opt-in" (AGENTS.md section 1) literally | Differs from every other provider; a user forgets to enable it |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- Nothing executes unless the host opts in with `AddEncinaHealthChecks`, so the pay-for-what-you-use rule holds, but a registration is present by default; Redis parity and AGENTS.md section 1 point in different directions.
- Class: Dilemma

</details>

<details>
<summary><strong>9. Names, status mapping and timeout result</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Names `encina-cache-memory` and `encina-cache-hybrid`; failures are Unhealthy only; a timeout returns `Unhealthy("<Name> health check timed out after <n>s")`; the timeout source comes from `timeProvider.CreateCancellationTokenSource(options.Timeout)`** | Follows the `encina-<provider>` convention of the options documentation; the base class already returns Unhealthy for every exception; deterministic with a `FakeTimeProvider` (AGENTS.md section 3) | `ProviderHealthCheckOptions.FailureStatus` is ignored; `RedisHealthCheck` uses `encina-redis` (consistent) |
| **B) Honor `options.FailureStatus` (Unhealthy or Degraded)** | Honors a documented option | A mapping in each check; the base class has none |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- The default names are public (`DefaultName`) and are what probes show; a configured `Name` overrides them. Whether `FailureStatus` is honored is a user-visible semantic.
- Class: Dilemma

</details>

---

## Implementation Phases

### Phase 1: Memory provider check (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 12 files (the log class and its EventIds 3565-3566 are created here, because this PR is the first to log; Phase 4):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching.Memory/Health/MemoryCacheHealthCheck.cs` | new |
| 2 | `src/Encina.Caching.Memory/Health/MemoryCacheHealthLog.cs` | new (Phase 4) |
| 3 | `src/Encina.Caching.Memory/MemoryCacheProvider.cs` | modify (`MemoryCacheOptions`) |
| 4 | `src/Encina.Caching.Memory/ServiceCollectionExtensions.cs` | modify |
| 5 | `src/Encina.Caching.Memory/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 6 | `src/Encina.Caching.Memory/README.md` | new (no README exists today; `docs-writer`) |
| 7 | `tests/Encina.UnitTests/Caching/Memory/Health/MemoryCacheHealthCheckTests.cs` | new (check and registration tests) |
| 8 | `tests/Encina.GuardTests/Infrastructure/Caching/MemoryCacheHealthCheckGuardTests.cs` | new |
| 9 | `tests/Encina.ContractTests/Caching/CacheHealthCheckContractTests.cs` | new (Memory; PR 2 extends it with Hybrid) |
| 10 | `tests/Encina.ContractTests/Encina.ContractTests.csproj` | modify (`ProjectReference` to `Encina.Caching.Memory`; the project has no caching reference today) |
| 11 | `.github/coverage-manifest/Encina.Caching.Memory.json` | modify (`mechanical-fixer`) |
| 12 | `changelog.d/754-memory-cache-health-check.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `MemoryCacheHealthCheck`: `public sealed class MemoryCacheHealthCheck : EncinaHealthCheck`, `DefaultName = "encina-cache-memory"` (Design Choice 9), constructor `(MemoryCacheProvider provider, ProviderHealthCheckOptions options, TimeProvider? timeProvider = null, ILogger<MemoryCacheHealthCheck>? logger = null)` (Design Choice 7) with `ArgumentNullException.ThrowIfNull` on the first two; `CheckHealthCoreAsync` links the caller token with `timeProvider.CreateCancellationTokenSource(options.Timeout)` (a system timer would not follow a `FakeTimeProvider`) and calls `provider.ExistsAsync("health:cache:" + Guid.NewGuid().ToString("N"), token)` (Design Choice 2); a timeout returns `Unhealthy("<Name> health check timed out after <n>s")`.
2. **Modify** `MemoryCacheOptions` (declared in `src/Encina.Caching.Memory/MemoryCacheProvider.cs`): add `public ProviderHealthCheckOptions ProviderHealthCheck { get; } = new() { Tags = ["encina", "cache", "memory", "ready"] };` (Design Choice 3).
3. **Modify** `src/Encina.Caching.Memory/ServiceCollectionExtensions.cs`: in `AddEncinaMemoryCache` build a local `MemoryCacheOptions`, invoke `configure`, register `TryAddSingleton<MemoryCacheProvider>()` with `ICacheProvider` forwarding to it (Design Choice 7), and call a new `internal RegisterHealthCheck(services, options)` that, when enabled, registers `IEncinaHealthCheck` with a factory capturing `options.ProviderHealthCheck` and resolving `TimeProvider` and `ILogger<>` optionally (Design Choice 5); it never registers `ProviderHealthCheckOptions`.
4. **Tests (PR 1)**: unit tests for healthy, unhealthy when the provider throws, timeout on a `FakeTimeProvider`, disabled option registers nothing, tags and name, no message leakage, and a DI test with `ValidateOnBuild` and `ValidateScopes` that registers a second check built from another options instance and asserts each reports its own name and tags; guard tests for the constructor; contract test over the Memory check (name, non-empty tags including `encina`, no throw on a cancelled token).
5. **Manifest (PR 1, same PR as the tests)**: per-file targets with one-sentence justifications in `Encina.Caching.Memory.json` for `Health/MemoryCacheHealthCheck.cs` (unit 90, guard 15, contract 40), `Health/MemoryCacheHealthLog.cs` (unit 80, guard 0 and contract 0 because generated logger bodies run only when the check logs), `MemoryCacheProvider.cs` (the touched options property; keeps its existing targets) and `ServiceCollectionExtensions.cs` (unit 85, guard 15); the measured value replaces each estimate before the PR.

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
- The timeout source is TimeProvider.CreateCancellationTokenSource, never new CancellationTokenSource(timeout).
- Register the check with a factory that captures this registration's ProviderHealthCheckOptions; never AddSingleton(ProviderHealthCheckOptions). The check takes MemoryCacheProvider, not ICacheProvider.
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

PR 2 holds exactly these 12 files (PR 1 is merged first; the Hybrid log class and EventIds 3657-3658 are created here, the first PR that logs for Hybrid):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching.Hybrid/Health/HybridCacheHealthCheck.cs` | new |
| 2 | `src/Encina.Caching.Hybrid/Health/HybridCacheHealthLog.cs` | new (Phase 4) |
| 3 | `src/Encina.Caching.Hybrid/HybridCacheProvider.cs` | modify (`HybridCacheProviderOptions`) |
| 4 | `src/Encina.Caching.Hybrid/ServiceCollectionExtensions.cs` | modify |
| 5 | `src/Encina.Caching.Hybrid/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 6 | `src/Encina.Caching.Hybrid/README.md` | new (`docs-writer`) |
| 7 | `tests/Encina.UnitTests/Caching/Hybrid/Health/HybridCacheHealthCheckTests.cs` | new |
| 8 | `tests/Encina.GuardTests/Infrastructure/Caching/HybridCacheHealthCheckGuardTests.cs` | new |
| 9 | `tests/Encina.ContractTests/Caching/CacheHealthCheckContractTests.cs` | modify (adds the Hybrid check to the cross-provider contract) |
| 10 | `tests/Encina.ContractTests/Encina.ContractTests.csproj` | modify (`ProjectReference` to `Encina.Caching.Hybrid`) |
| 11 | `.github/coverage-manifest/Encina.Caching.Hybrid.json` | modify (`mechanical-fixer`) |
| 12 | `changelog.d/754-hybrid-cache-health-check.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `HybridCacheHealthCheck`, same shape as Phase 1 over `HybridCacheProvider` (`DefaultName = "encina-cache-hybrid"`); the probe is `ExistsAsync`, which for Hybrid reads through `GetOrCreateAsync` with both writes disabled.
2. **Verify before relying on the L2 claim (Design Choice 2)**: a unit test runs the probe against a `HybridCache` whose `IDistributedCache` substitute throws on read; the plan records the outcome (Healthy or Unhealthy) in a comment on the issue before the PR; if Healthy, stop and report to the maintainer.
3. **Modify** `HybridCacheProviderOptions` (declared in `src/Encina.Caching.Hybrid/HybridCacheProvider.cs`): add `ProviderHealthCheck` with tags `encina`, `cache`, `hybrid`, `ready`.
4. **Modify** `src/Encina.Caching.Hybrid/ServiceCollectionExtensions.cs`: register the check only in `AddEncinaHybridCache`. `AddEncinaHybridCacheWithOptions` ends with `return services.AddEncinaHybridCache(configureProvider)`, so registering in both would duplicate the check; the unit test calls `AddEncinaHybridCacheWithOptions` and asserts exactly one check. The provider is registered as `TryAddSingleton<HybridCacheProvider>()` with `ICacheProvider` forwarding to it (Design Choice 7).
5. **Tests and manifest (PR 2)**: unit, guard, the contract extension, DI tests with Hybrid plus a second check with different options, and the manifest targets with one-sentence justifications for the four touched source files, as in PR 1.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged. Hybrid has two registration methods; both must register the check once.

TASK:
Create HybridCacheHealthCheck, add HybridCacheProviderOptions.ProviderHealthCheck, register from
AddEncinaHybridCache only (AddEncinaHybridCacheWithOptions delegates to it), add PublicAPI lines.

KEY RULES:
- A read-only probe: HybridCacheProvider.GetAsync already disables local and distributed writes for its read.
- Verify with a faulting IDistributedCache that a dead L2 makes the probe Unhealthy; report the outcome.
- Calling AddEncinaHybridCacheWithOptions must register exactly one check.
- Per-registration options by capture; the check takes HybridCacheProvider.
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

PR 3 holds exactly these 7 files (PR 1 and PR 2 are merged first, so the multi-provider DI tests can use the Memory and Hybrid checks):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching.Redis/RedisCacheProvider.cs` | modify (`RedisCacheOptions` tags) |
| 2 | `src/Encina.Caching.Redis/ServiceCollectionExtensions.cs` | modify (`RegisterHealthCheck` captures the options, Design Choice 5) |
| 3 | `tests/Encina.UnitTests/Caching/RedisProtocolHealthCheckRegistrationTests.cs` | new (four products plus Redis with Memory and Hybrid) |
| 4 | `tests/Encina.UnitTests/Caching/Redis/Health/RedisHealthCheckTests.cs` | modify (tag and options expectations) |
| 5 | `.github/coverage-manifest/Encina.Caching.Redis.json` | modify (`mechanical-fixer`) |
| 6 | `changelog.d/754-cache-health-check-tags.changed.md` | new (`mechanical-fixer`; states that the Redis cache check changes from the tag `database` to `cache` and `redis`) |
| 7 | `docs/guides/health-checks.md` | modify (the caching section; `docs-writer`) |

Tasks:

1. **Tests first**: one registration test per product (`AddEncinaValkeyCache`, `AddEncinaDragonflyCache`, `AddEncinaGarnetCache`, `AddEncinaKeyDBCache`) with `ValidateOnBuild` and `ValidateScopes`, asserting exactly one `IEncinaHealthCheck` of type `RedisHealthCheck` and that `Enabled = false` registers none; plus Redis with Memory and Redis with Hybrid in one provider, each check reporting its own name and tags. The four product tests pass without production changes and close the verification item of the issue.
2. **Modify** `src/Encina.Caching.Redis/RedisCacheProvider.cs` (`RedisCacheOptions`, line 335): initialize `ProviderHealthCheck` with tags `encina`, `cache`, `redis`, `ready` (Design Choice 3, Option A).
3. **Modify** `RegisterHealthCheck` of the Redis package (`ServiceCollectionExtensions.cs`, lines 171-178): replace `services.AddSingleton(options.ProviderHealthCheck)` and the type-resolved check by a factory registration capturing `options.ProviderHealthCheck` (Design Choice 5).
4. **Manifest (PR 3)**: per-file targets with one-sentence justifications for `RedisCacheProvider.cs` and `ServiceCollectionExtensions.cs` of `Encina.Caching.Redis.json`.
5. **If the maintainer chooses Option B of Design Choice 1**: the wrapper changes (each wrapper's four `AddEncina<Product>Cache` overloads set `ProviderHealthCheck.Name`, for example `encina-valkey`, and the product tag through the options overload of `AddEncinaRedisCache`; 4 `ServiceCollectionExtensions.cs` files, 4 wrapper manifests, their tests) form a fourth pull request, because they do not fit PR 3 within 15 files.

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
- RegisterHealthCheck of Redis captures options.ProviderHealthCheck in a factory; it must not register ProviderHealthCheckOptions as a service.
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

1. **Create (in PR 1)** `src/Encina.Caching.Memory/Health/MemoryCacheHealthLog.cs` with `ProbeFailed(ILogger, Exception exception)` (EventId 3565; the caller passes `ex.ForLogging()`) and `ProbeTimedOut(ILogger, double timeoutSeconds)` (EventId 3566), inside `CachingMemory` (3550-3599; 3550-3564 are used).
2. **Create (in PR 2)** `src/Encina.Caching.Hybrid/Health/HybridCacheHealthLog.cs` with EventIds 3657 and 3658 of the same two methods, inside `CachingHybrid` (3650-3699; 3650-3656 are used).
3. Each check logs in two places only: the timeout path calls `ProbeTimedOut`, and a `catch (Exception ex) when (ex is not OperationCanceledException)` calls `ProbeFailed(logger, ex.ForLogging())` and rethrows, so `EncinaHealthCheck` still returns the type-name result. No new range, so `EncinaEventIdAllocationTests` needs no map change.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
The Memory and Hybrid packages own EventId ranges 3550-3599 and 3650-3699, used up to 3564 and 3656.

TASK:
Add the two internal partial log classes with [LoggerMessage] methods (ProbeFailed with ex.ForLogging(), ProbeTimedOut)
and call them from the checks. Each log class goes into the pull request that adds its check (Memory in PR 1, Hybrid in PR 2).

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

The tests, manifests, PublicAPI lines, fragments and documents are part of the pull request each one belongs to (the file tables of Phases 1 to 3); this phase only states what they contain and how the work is verified.

<details>
<summary><strong>Tasks</strong></summary>

1. Unit tests per check in `tests/Encina.UnitTests/Caching/Memory/Health/` and `.../Hybrid/Health/`: healthy with the real provider, unhealthy when the provider throws, timeout on a `FakeTimeProvider`, disabled option registers nothing, tags and name, no message leakage. Hybrid tests use a real `HybridCache` built from `AddHybridCache` with `AddDistributedMemoryCache` for the healthy path and a faulting `IDistributedCache` substitute for the L2 failure path (Phase 2 task 2).
2. Guard tests for both constructors (PR 1, PR 2); contract test that every cache check honors `IEncinaHealthCheck` (name, non-empty tags, never throws): created in PR 1 for the Memory check, extended in PR 2 with the Hybrid check. A contract-test `.md` justification is never written.
3. Property, integration, load and benchmark: not required (deterministic in-process logic; the Redis-protocol products are covered by the existing Redis tests); each manifest `reason` says so.
4. `mechanical-fixer`, per pull request: the package manifest with per-file per-flag targets and one-sentence justifications for every touched `src/` file (entered in the PR that adds the tests meeting them), the PublicAPI lines, and the changelog fragment named in the file table.
5. `docs-writer`: Memory README (PR 1), Hybrid README (PR 2), the caching section of `docs/guides/health-checks.md` (PR 3).

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
| Production code | 4 | 6 | Two checks and two log classes; the two provider files (options), the Memory and Hybrid `ServiceCollectionExtensions.cs`, `RedisCacheProvider.cs`, the Redis `ServiceCollectionExtensions.cs` |
| Tests | 6 | 4 | New: Memory unit, Hybrid unit, Redis-protocol registration unit, two guard files, the contract file; modified: the contract file (PR 2), `RedisHealthCheckTests.cs`, `ContractTests.csproj` (twice, PR 1 and PR 2) |
| Documentation | 2 | 1 | Memory README, Hybrid README (new); `docs/guides/health-checks.md` |
| Configuration | 3 | 5 | Three changelog fragments; two `PublicAPI.Unshipped.txt`, three manifests |
| **Total** | **15** | **16** | 31 file changes (29 distinct files: the contract file and `ContractTests.csproj` change in both PR 1 and PR 2); PR 1 = 12, PR 2 = 12, PR 3 = 7; each under the 15-file limit |

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
3. Registration helper called from AddEncinaMemoryCache and AddEncinaHybridCache (the WithOptions overload delegates) when Enabled; each check is registered with a factory capturing its own options and takes its concrete provider.
4. Registration tests for the four Redis-protocol wrappers; RedisCacheOptions cache tags; Redis RegisterHealthCheck captures its options.
5. EventIds 3565-3566 (Memory) and 3657-3658 (Hybrid) inside existing ranges.
6. Unit, guard and contract tests; manifests, README, guide and changelog through the specialists.
Three pull requests: Memory, Hybrid, Redis-protocol.

KEY PATTERNS:
- Failure results carry the exception type name only; the timeout comes from ProviderHealthCheckOptions.Timeout.
- The options property is `{ get; } = new()`; registration is gated by Enabled at registration time and never registers ProviderHealthCheckOptions as a service.
- The timeout source is TimeProvider.CreateCancellationTokenSource(options.Timeout).
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
| 2 | OpenTelemetry | ⏭️ Defer | Health-check telemetry is one cross-provider concern tracked by the umbrella #622; no metric here |
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

- OpenTelemetry for health checks: the umbrella #622 (#454 is the Kubernetes probe endpoints, not telemetry).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `EncinaHealthCheck` and `ProviderHealthCheckOptions` | ✅ Available | `src/Encina.Messaging/Health/` |
| `RegisterHealthCheck` reference | ✅ Available | Redis cache package |
| EventId ranges 3550-3599 and 3650-3699 | ✅ Available | Free ids after 3564 and 3656 |
| Maintainer answers to Design Choices 1 to 9 (every Dilemma) | Pending | Asked when work starts |
| Memcached provider | ⏭️ Later | #277 delivers its own check |

No blocking prerequisites.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1 to 9; 1, 2, 3, 5 and 7 change the code) when work starts, records the answers under `## Maintainer Decisions`, then briefs three workers (PR 1 Memory, PR 2 Hybrid, PR 3 Redis-protocol).
2. After merge, comment on #754 with the result and on #755 and #622 with the tag convention and the options wiring that were chosen.
3. Follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/lock-healthcheck-options-wiring.md): the lock packages (Redis, SqlServer) register `ProviderHealthCheckOptions` by type and need the same per-registration capture.
