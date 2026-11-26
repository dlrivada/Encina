# Implementation Plan: Secondary cache provider fallback for cache auto-recovery

> **Issue**: [#4417](https://github.com/dlrivada/Encina/issues/4417)
> **Type**: Feature
> **Complexity**: Medium (2 code pull requests of 15 and 6 files, and a documentation pull request of 1 file)
> **Estimated Scope**: about 350 lines of production code, about 900 lines of tests
> **Milestone**: v0.18.0 — Integration & Platform Capabilities

---

## Summary

Issue #4417 adds the `UseSecondaryProvider` fallback strategy to the cache auto-recovery of #278 (open, plan merged): while the primary circuit is open, reads and writes go to a second registered cache provider. Re-checked against `origin/main` (326e65ebf):

| Premise of the issue | State in the code and the plans |
|----------------------|---------------------------------|
| A second `ICacheProvider` registration is available | Every provider registers `ICacheProvider` with `TryAdd` and one default instance (`src/Encina.Caching.Redis/ServiceCollectionExtensions.cs` builds one `IConnectionMultiplexer` singleton); a second instance of the same family needs its own multiplexer. `RedisCacheProvider` has a public constructor over `IConnectionMultiplexer` (`src/Encina.Caching.Redis/RedisCacheProvider.cs:36`), so an application can build a second provider itself; the plan adds a helper that registers it as a keyed singleton. |
| The decorator can switch backends | #278 puts recovery in `ResilientCacheProvider`, a singleton decorator over the registered `ICacheProvider` with a `CacheFallbackStrategy` and a `CacheCircuitBreaker`; the secondary is one more strategy there. The enum holds `SkipCache` and, only if #278 Design Choice 5 is answered B or C, `UseLocalMemory` (its PR 3); under #278's answer B its plan turns the secondary into this very issue, and under answer C #278 itself delivers a secondary provider and this plan has nothing left to add. So this plan is implemented only if #278 Design Choice 5 is answered A or B: under A the enum then holds `SkipCache` and `UseSecondaryProvider` and PR 1 needs #278 PR 2; under B PR 1 needs #278 PR 3 and the secondary branch sits next to the local-memory one. |
| The metric and the log range are new | #278 PR 2 already creates the counter `encina.cache.fallback_operations` (tag `strategy`) and PR 4 the recovery health check. The plan adds a tag value and a health data entry, no instrument. The range `CachingRecovery` 30400-30411 is full (twelve ids, all assigned by #278), so this plan takes its own range from block 500-549. |
| On recovery the secondary's entries are not copied back | True, and it creates a staleness question: an entry written to the secondary during an outage survives in the secondary after the primary recovers, and invalidations that run while the primary is healthy never reach it (Design Choice 3). |
| Keyed services in .NET | `AddKeyedSingleton<ICacheProvider>(key, factory)` and `IServiceProviderIsKeyedService` exist in the .NET 10 DI container; the decorator singleton resolves the keyed provider by key. |
| Tenants | The cache keys are tenant-qualified by the key generator (`sm:t:{tenant}:...`), identical on both providers, so tenant isolation holds on the secondary. The breaker is per backend, not per tenant: ADR-054 D4 (the tenant tag is for tenant-scoped facts; a backend outage is not one), so no tenant tag. |

The plan adds, in `Encina.Caching`, the strategy value, two options, an internal `SecondaryProviderFallback` (own circuit breaker, lifetime cap, tenant-neutral), a registration helper, a validation rule and a log range, and extends the health data. Caching (8): the decorator is provider-agnostic, so any pair of Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB and the planned Memcached of #277 works as primary and secondary.

**Shared pieces and ownership (family cache-patterns-2):** #278 owns `ResilientCacheProvider`, `CacheAutoRecoveryOptions`, `CacheFallbackStrategy`, `CacheCircuitBreaker`, `CacheRecoveryHealthCheck`, `AddEncinaCacheAutoRecovery` and `CachePatternMetrics.Recovery.cs`; this plan modifies them after #278 PR 4 (PR 3 adds the local-memory branch next to which the secondary branch sits). #4413 changes the decorator to forward `ILeveledCacheProvider`; the secondary branch forwards nothing leveled (a secondary is used through `ICacheProvider` only). This plan owns `CachingSecondaryFallback` 538-541. #4414 to #4416 are unrelated to this plan.

**Affected packages:** `Encina.Caching` (core), `Encina` (EventId registry). **Provider category:** Caching (8), decorator-level.

---

## Design Choices

<details>
<summary><strong>1. How the secondary is registered (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A keyed `ICacheProvider` singleton under `CacheAutoRecoveryOptions.SecondaryProviderKey` (default `encina.cache.secondary`), registered by the helper `AddEncinaCacheSecondaryProvider(factory)` that guarantees the singleton lifetime** | .NET keyed services; no new abstraction; the helper removes the lifetime mistake | The key is a string convention |
| **B) A dedicated `IFallbackCacheProvider : ICacheProvider` marker abstraction** | Explicit type; its own lifecycle | A new public abstraction that every provider would have to opt into; a second registration path |
| **C) Keep local memory only (the state of #278)** | Nothing to run | No standby cache |

### Chosen Option: **A - keyed singleton with a helper** (recommended, pending the maintainer)

### Rationale

- Option B wins when the secondary has a different lifecycle from the primary (for example created per region at runtime); C wins when no standby is run. The decorator is a singleton, so a scoped or transient secondary would be a short-lived service held for ever; the helper registers a singleton by construction, and an application that registers the keyed service itself owns its lifetime (documented; a scoped one fails at first resolution under `ValidateScopes`).
- Class: Dilemma

</details>

<details>
<summary><strong>2. What the secondary serves while the primary circuit is open (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reads try the secondary, writes go to the secondary only; the half-open probe goes to the primary; the secondary has its own breaker (same options, its own instance) and a secondary failure behaves as `SkipCache` for that operation (a miss, a skipped write, the factory runs); `RemoveByPatternAsync` is forwarded to the secondary** | The issue's proposed behavior; the application never fails because the standby is down; no write pays the primary's timeout | Two breakers to reason about |
| **B) Dual writes: also write to the primary while it is open** | The primary is warm on recovery | Every write pays the primary's timeout during the outage, which is what the breaker avoids |

### Chosen Option: **A - secondary only while open** (recommended, pending the maintainer)

### Rationale

- Option B wins when the primary's warm-up after recovery matters more than write latency during the outage. `RemoveByPatternAsync` pattern support differs per provider (Hybrid removes tracked keys only, #2402), which is the provider's contract.
- Class: Dilemma

</details>

<details>
<summary><strong>2b. Cancellation and the user factory are never counted</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A caller's cancellation rethrows `OperationCanceledException` and counts against neither breaker; in `GetOrSetAsync` the factory's exception is captured, rethrown unchanged and counts nowhere, and when the secondary cannot be used the factory runs directly** | The breakers measure the backends only | A flag per call to know where an exception came from |
| **B) Count every exception** | Simplest | A throwing handler or a cancelling caller opens a breaker |

### Chosen Option: **A - backend failures only**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (cancellation is not an error and propagates; exceptions of user code are not swallowed) and the signature `Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, ...)` in `src/Encina.Caching/Abstractions/ICacheProvider.cs` (the factory is the caller's code). Only A complies, whichever option Design Choice 2 takes.

</details>

<details>
<summary><strong>3. Stale entries in the secondary after a recovery (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Lifetime cap: every entry written to the secondary lives at most `SecondaryMaxDuration` (default 30 s, the cap of the local fallback in #278), and removals made while the circuit is open are forwarded to it** | No cross-node coordination; staleness is bounded; the same rule as `UseLocalMemory` | An outage that flaps within the cap can serve an entry invalidated in between |
| **B) A, and also forward every removal to the secondary while the primary is healthy (best effort)** | Invalidations are honored even across flaps | Every invalidation pays a second backend call, and the secondary's failures add noise |
| **C) Clear the secondary when the circuit closes** | Nothing survives a recovery | The secondary is a standby shared by many nodes: one node's recovery wipes the others' entries |

### Chosen Option: **A - lifetime cap** (recommended, pending the maintainer)

### Rationale

- Option B wins when outages flap and invalidations must be honored (prices, permissions); C wins for a secondary dedicated to one node. With sliding expiration the secondary stores the shorter of the window and the cap, and the absolute maximum is capped too.
- Class: Dilemma

</details>

<details>
<summary><strong>4. A missing or self-referencing secondary (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fail at start: the `IValidateOptions<CacheAutoRecoveryOptions>` of #278 (`ValidateOnStart`) gets one rule, `UseSecondaryProvider` requires the secondary registration chosen in Design Choice 1 (under option A of D1: a keyed `ICacheProvider` under `SecondaryProviderKey`, checked with `IServiceProviderIsKeyedService`) and a positive `SecondaryMaxDuration`; the decorator factory throws `InvalidOperationException` naming the key when the secondary is the same instance as the inner primary** | A configured standby that cannot work stops the host instead of degrading silently | A cache configuration error stops a host that could run without a cache |
| **B) Log a warning and behave as `SkipCache`** | The host always starts; availability first | A configured standby that does nothing, found only in the logs |

### Chosen Option: **A - fail at start** (recommended, pending the maintainer)

### Rationale

- Option B wins where a cache setting must never stop the host. AGENTS.md section 3 requires registration completeness for what a registration resolves; it does not decide between failing and degrading for an availability feature, so this is a Dilemma.
- A helper factory that resolves `ICacheProvider` from the container returns the decorator itself: the container reports a circular dependency while the decorator is being built, which the DI test asserts; the same-instance comparison covers a factory that returns the inner primary.
- No error code is created: validation failures are `OptionsValidationException` messages naming the option, never a value; operations never return a `Left` (a backend failure becomes a miss or a skipped write). So no ADR-062 category applies.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Telemetry and health</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The existing counter `encina.cache.fallback_operations` gets the tag value `secondary`; the recovery health check data gains `secondary_state` (a state name only); the status rule of #278 is unchanged; no tenant tag** | No new instrument; no key or exception message in a result | A secondary outage during a primary outage still reads as `Degraded` |
| **B) A new counter and a separate health check for the secondary** | Separate signal | More instruments for one decorator |

### Chosen Option: **A - extend the existing instruments**

### Rationale

- **Class:** Trivial
- **Settled by:** ADR-054 D4 (a backend failure domain is not a tenant fact, so no tenant tag) and AGENTS.md section 3 (health results, logs and tags never carry a key or an exception message). The plan changes no status rule: whatever #278 Design Choice 7 decides about `Healthy` or `Degraded` applies unchanged, and `secondary_state` is data only.

</details>

---

## Implementation Phases

### Phase 1: The secondary branch, options, registration and EventIds (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 needs #278 PR 2 merged (PR 3 too when #278 Design Choice 5 is answered B, see the Summary) and holds exactly these 15 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Recovery/CacheAutoRecoveryOptions.cs` | modify (the enum value `UseSecondaryProvider`, `SecondaryProviderKey`, `SecondaryMaxDuration`) |
| 2 | `src/Encina.Caching/Recovery/SecondaryProviderFallback.cs` | new (two internal sealed types in one file: `SecondaryProviderFallback` and the state holder `CacheSecondaryState`) |
| 3 | `src/Encina.Caching/Recovery/ResilientCacheProvider.cs` | modify (the strategy branch next to the existing fallback branches; the tag value `secondary`) |
| 4 | `src/Encina.Caching/CacheAutoRecoveryServiceCollectionExtensions.cs` | modify (`AddEncinaCacheSecondaryProvider`, the keyed lookup in the decorator factory, `TryAddSingleton<CacheSecondaryState>`, the validator rule) |
| 5 | `src/Encina.Caching/Recovery/CacheSecondaryFallbackLog.cs` | new (EventIds 538-541) |
| 6 | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (`CachingSecondaryFallback = (538, 541)`) |
| 7 | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify (add the range to the `Encina.Caching` entry) |
| 8 | `docs/engineering/eventid-block-registry.md` | modify (a row `CachingSecondaryFallback \| 538-541 \| Caching \| historical` and the block row split; read `EventIdRegistryAgreementTests` first) |
| 9 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 10 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 11 | `tests/Encina.UnitTests/Caching/SecondaryProviderFallbackTests.cs` | new (`FakeTimeProvider`) |
| 12 | `tests/Encina.UnitTests/Caching/ResilientCacheProviderTests.cs` | modify (the decorator with a secondary; registration and validator assertions) |
| 13 | `tests/Encina.GuardTests/Caching/CacheAutoRecoveryServiceCollectionExtensionsGuardTests.cs` | modify (the new helper; file created by #278 PR 2) |
| 14 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 15 | `changelog.d/4417-cache-secondary-provider-fallback.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `CacheSecondaryState` (internal sealed, a distinct service type registered with `TryAddSingleton` in PR 1, built from `IOptions<CacheAutoRecoveryOptions>` and `TimeProvider`): it owns the secondary's own `CacheCircuitBreaker` instance and exposes its `State` and `OpenedAtUtc`, so the decorator and the PR 2 health check resolve the same holder (the primary breaker stays #278's registered singleton; the secondary one is reachable only through this holder). **Create** `SecondaryProviderFallback(ICacheProvider secondary, CacheSecondaryState state, CacheFailureClassification classification, TimeSpan maxDuration, ILogger<...>)`, taking the internal composite `CacheFailureClassification` of #278 (a distinct type from `ICacheFailureClassifier`, so the Redis classifier adds to the defaults and `ValidateOnBuild` finds no cycle): one method per `ICacheProvider` operation; each acquires the holder's breaker, calls the secondary, records success or failure when the classification says backend failure and the token is not cancelled, and on failure answers as `SkipCache` does (a default read, a skipped write, `ExistsAsync` false, `RefreshAsync` false, the factory runs). Expirations are capped per Design Choice 3. A cancelled caller token rethrows `OperationCanceledException` and counts nowhere.
2. **Modify** `ResilientCacheProvider`: when the primary breaker is not acquirable and the strategy is `UseSecondaryProvider`, delegate to the fallback instead of `SkipCache`; the half-open probe still goes to the primary; the counter is incremented with `strategy = secondary`. The branch is one private method per operation so each method stays at CRAP 10 or below.
3. **Modify** the registration: `AddEncinaCacheSecondaryProvider(this IServiceCollection services, Func<IServiceProvider, ICacheProvider> factory)` (public; null services or factory throws `ArgumentNullException`) registers a keyed singleton under `SecondaryProviderKey` with `TryAdd`-style semantics (an existing keyed registration is kept); the decorator factory resolves the keyed provider only when the strategy is `UseSecondaryProvider` and throws `InvalidOperationException` naming the key when the result is the inner primary (a factory that resolves `ICacheProvider` returns the decorator itself, which the container reports as a circular dependency; both are tested). The validator rule is Design Choice 4. EventIds: 538 secondary fallback active (once per outage, operation name only), 539 secondary operation failed (operation and exception type), 540 secondary circuit opened, 541 secondary circuit closed; no key, no value and no exception message is logged.
4. **Tests:** reads and writes hit only the secondary while the primary is open and only the primary when closed; the probe goes to the primary; the secondary's failures never open the primary breaker and a primary failure never opens the secondary one; a failing secondary behaves as skip; a factory exception propagates unchanged and counts nowhere; cancellation counts nowhere; the cap holds for absolute and sliding expiration (`FakeTimeProvider`); removals while open reach the secondary; the same instance is rejected; a missing keyed registration fails host start; the helper registers a singleton; `CacheSecondaryState` is resolvable and shared (`ValidateOnBuild`); a helper factory that resolves `ICacheProvider` fails with the circular-dependency error; DI is built with `ValidateOnBuild` and `ValidateScopes` in both registration orders of #278; tenant-qualified keys reach the secondary unchanged. Guard tests: null services and null factory throw; the fallback class is internal (no `InternalsVisibleTo` for `Encina.GuardTests`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. #278 PRs 1-4 are merged: ResilientCacheProvider, CacheCircuitBreaker, CacheAutoRecoveryOptions,
CacheFallbackStrategy (SkipCache, UseLocalMemory), AddEncinaCacheAutoRecovery and the recovery health check exist.

TASK:
1. Add the UseSecondaryProvider value and options, SecondaryProviderFallback, the decorator branch and AddEncinaCacheSecondaryProvider.
2. Create CacheSecondaryFallbackLog (EventIds 538-541) and register CachingSecondaryFallback = (538, 541) in EventIdRanges.cs, the assembly map and the registry doc.
3. Write the unit, DI and guard tests; ask mechanical-fixer for the PublicAPI files, the manifest rows and the fragment.

KEY RULES:
- Only backend failures count; a caller's cancellation never counts and propagates; a factory exception propagates unchanged.
- The secondary has its own breaker; its failures never count against the primary.
- The secondary is a singleton; no scoped service is injected into a singleton; time from TimeProvider; no static state.
- Logs carry operation names and exception types only, never a key, a value or a message.
- Follow the maintainer's answers to Design Choices 1 to 4, given in your brief.
- Use the internal composite CacheFailureClassification of #278, never a bare ICacheFailureClassifier.

REFERENCE FILES:
- src/Encina.Caching/Abstractions/ICacheProvider.cs
- src/Encina.Caching.Redis/RedisCacheProvider.cs
- src/Encina.Caching/Recovery/ResilientCacheProvider.cs (after #278)
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 2: Health data, contract test and the Redis integration test (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 needs PR 1 and #278 PR 4 merged and holds exactly these 6 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Health/CacheRecoveryHealthCheck.cs` | modify (`secondary_state` in the result data; the status rule is unchanged) |
| 2 | `tests/Encina.UnitTests/Caching/CacheRecoveryHealthCheckTests.cs` | modify |
| 3 | `tests/Encina.ContractTests/Caching/SecondaryFallbackContractTests.cs` | new (the real decorator over two real `MemoryCacheProvider` instances; the project reference to `Encina.Caching.Memory` comes from #278 PR 2) |
| 4 | `tests/Encina.IntegrationTests/Infrastructure/Caching/CacheSecondaryFallbackIntegrationTests.cs` | new (shared Redis fixture) |
| 5 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 6 | `changelog.d/4417-cache-secondary-health.added.md` | new (`mechanical-fixer`) |
No public symbol is added in this PR, so no PublicAPI file changes. Tasks:

1. **Modify** the health check: it takes `CacheSecondaryState?` optionally (registered by PR 1; null when the strategy is off) and the data carries `state` and, with the strategy on, `secondary_state`, both state names only (never a key or an exception message).
2. **Contract test:** while the primary circuit is forced open, the decorator over two memory providers keeps the `ICacheProvider` contract on the secondary (get, set, remove, exists, refresh, get-or-set) and the entries expire at the cap.
3. **Integration test:** the primary is a `RedisCacheProvider` over an unreachable endpoint with a short connect timeout, the secondary a `RedisCacheProvider` over the shared container; after the primary breaker opens, writes and reads are served from the shared container (checked through a third client on the same container), the handler never runs twice for a cached value, and the primary breaker closes again when a reachable primary replaces the unreachable one in a second host. The test never creates its own container and never calls the fixture's `DisposeAsync`; it carries `[Trait("Category", "Integration")]` and the shared Redis collection attribute.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
PR 1 is merged: the decorator serves the secondary while the primary circuit is open.

TASK:
Add secondary_state to the health check data, write the contract test and the Redis integration test; ask mechanical-fixer for the manifest rows
and the fragment.

KEY RULES:
- Health data holds state names only; the status rule of #278 is unchanged.
- Integration tests use the shared Redis collection fixture; never a per-class container.

REFERENCE FILES:
- src/Encina.Caching/Health/CacheRecoveryHealthCheck.cs (after #278)
- tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs
- tests/Encina.ContractTests/Caching/ResilientCacheProviderContractTests.cs (after #278)
```

</details>

### Phase 3: Documentation (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly 1 file, written by `docs-writer`: `docs/features/handler-caching.md` (the "Resilience patterns" section of #278 gains "Secondary provider fallback": registering the standby, the lifetime cap and why entries are not copied back, no hand-typed figures). XML documentation with an `<example>` on `AddEncinaCacheSecondaryProvider` and the options is part of PR 1.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
PRs 1 and 2 are merged.

TASK:
Ask docs-writer for the subsection; verify links and lint.

KEY RULES:
- No hand-typed figures; cite tests by name; one Diataxis quadrant per page.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- docs/features/handler-caching.md
```

</details>

### Phase 4: Cross-cutting integration, observability and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **EventIds:** `CachingSecondaryFallback` 538-541, emitted only by `Encina.Caching`: 538 secondary fallback active, 539 secondary operation failed, 540 secondary circuit opened, 541 secondary circuit closed. Verified free against the registry (`500-549` assigned to cache-patterns-2) and the sibling plans (#272 500-507, #274 508-513, #275 514-519, #4411 520-523, #4412 524-525, #4413 526-527, #4414 528-533, #4415 534-537); 542-549 stay free. The issue asks for the recovery range, which #278 fills (30400-30411).
2. **Error codes:** none, see Design Choice 4.
3. **Coverage targets (per flag, set in the PR that adds each file; measured values go in the PR body):**

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Encina.Caching/Recovery/SecondaryProviderFallback.cs` (with `CacheSecondaryState`) | 95 | 0 | 0 in PR 1, 40 in PR 2 | 0 | One method per operation with failure, cap and cancellation paths unit tested in PR 1; the contract target of 40 is set in PR 2, the PR that adds `SecondaryFallbackContractTests.cs`; internal, and `Encina.Caching` grants no `InternalsVisibleTo` to the guard project |
| `Encina.Caching/Recovery/CacheSecondaryFallbackLog.cs` | 90 | 0 | 0 | 0 | Generated bodies run in the fallback tests; internal |
| `Encina.Caching/Recovery/ResilientCacheProvider.cs` | 95 | 0 | 70 | 40 | The numbers #278 sets (unit 95, contract 70, integration 40 from its PR 5; no row exists on `origin/main` today); never lowered; the new branch is covered by PR 1 unit tests and PR 2's contract and integration tests |
| `Encina.Caching/CacheAutoRecoveryServiceCollectionExtensions.cs` | 100 | 85 | 0 | 0 | The numbers #278 sets (unit 100, guard 85); the helper's null guards are guard tested |
| `Encina.Caching/Recovery/CacheAutoRecoveryOptions.cs` | 100 | 85 | 0 | 0 | The numbers #278 sets; two properties and one enum value are added and unit tested |
| `Encina.Caching/Health/CacheRecoveryHealthCheck.cs` | 100 | 0 | 0 | 0 | The number #278 sets (unit 100); one data entry is added and unit tested in PR 2 |

   The worker measures every flag per PR with `coverage-report.cs`; no target is lowered (`target-ratchet`) and the CRAP table of changed methods stays at 10 or below.
4. **Property, load, benchmark:** no property test (the cap and the routing are covered by unit grids); no load test or benchmark: the branch runs only while the primary is down and adds one breaker lock like #278's.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
The code pull requests are merged. Verification remains.

TASK:
Verify the per-flag targets and the EventId registration; run coverage-report.cs --check-justifications; fix omissions through mechanical-fixer.

KEY RULES:
- Never lower an existing target (target-ratchet); add no log, metric or error code beyond the plan.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Caching.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 3 | Cancellation propagates; user code exceptions are not swallowed; registration completeness; fail closed; no `EncinaError.Message` in logs |
| AGENTS.md section 5 | Caching (8): any pair of providers, behavior identical |
| ADR-054 D4 | The backend is not tenant data: no tenant tag |
| ADR-021 | EventId range registered before use |
| Circuit breaker pattern (Nygard, Release It!) | One breaker per backend, half-open probe |
| .NET keyed services | `AddKeyedSingleton`, `IServiceProviderIsKeyedService` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `ICacheProvider` | `src/Encina.Caching/Abstractions/ICacheProvider.cs` | Contract of both backends |
| `RedisCacheProvider` | `src/Encina.Caching.Redis/RedisCacheProvider.cs` | Public constructor for a second instance |
| `ResilientCacheProvider`, `CacheCircuitBreaker`, options, health check (#278) | `src/Encina.Caching/Recovery/`, `Health/` (after #278) | Host of the new branch |
| `RedisFixture` | `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs` | The shared container |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/` | New range and assembly map |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (new range `CachingSecondaryFallback`, block 500-549) | 538-541 | This plan, see Phase 4 |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 2 | 7 | Fallback, log; options, decorator, DI extension, health check, ranges, PublicAPI x2 |
| Tests | 4 | 3 | Unit (1), contract (1), integration (1), guard modification, unit modifications, allocation test |
| Documentation | 0 | 2 | Handler-caching page, registry doc |
| Configuration | 2 | 2 | Two fragments; manifest edited per PR |
| **Total** | **8** | **14** | 3 pull requests of 15, 6 and 1 files |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #4417 adds the UseSecondaryProvider strategy to the cache auto-recovery decorator of #278:
while the primary circuit is open, reads and writes go to a keyed second ICacheProvider.

IMPLEMENTATION OVERVIEW:
1. PR 1: options, SecondaryProviderFallback with its own breaker and lifetime cap, the decorator branch, the registration helper, EventIds 538-541.
2. PR 2: health data, contract test over two memory providers, Redis integration test.
3. PR 3: documentation by docs-writer.

KEY PATTERNS:
- Only backend failures count; cancellation propagates and counts nowhere; a factory exception is never counted.
- The secondary is a singleton, bounded by a lifetime cap, never copied back; validation fails the host start when it is missing.
- Follow the maintainer's answers to Design Choices 1 to 4, given in your brief.

REFERENCE FILES:
- src/Encina.Caching/Abstractions/ICacheProvider.cs
- src/Encina.Caching.Redis/RedisCacheProvider.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Extends #278; any pair of the 8 providers. |
| 2 | OpenTelemetry | ✅ Include | The existing `encina.cache.fallback_operations` counter gets the tag value `secondary`; no tenant tag (ADR-054 D4, backend failure domain). |
| 3 | Structured Logging | ✅ Include | `CacheSecondaryFallbackLog` 538-541, operation names and exception types only. |
| 4 | Health Checks | ✅ Include | `secondary_state` in the recovery check data. |
| 5 | Validation | ✅ Include | The secondary must be registered, durations positive, the secondary not the primary, at host start. |
| 6 | Resilience | ✅ Include | This feature: a second breaker and a standby backend. |
| 7 | Distributed Locks | ❌ N/A | Cache operations are idempotent and node-local in state. |
| 8 | Transactions | ❌ N/A | Independent of units of work. |
| 9 | Idempotency | ❌ N/A | Idempotent cache operations. |
| 10 | Multi-Tenancy | ❌ N/A | Keys are already tenant-qualified; backend state is not tenant scoped. |
| 11 | Module Isolation | ❌ N/A | Same. |
| 12 | Audit Trail | ❌ N/A | Operational, no data access. |

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| #278 PRs 1 to 4 (decorator, local fallback, health check) | Open | Plan merged; PR 3 (local memory) exists only if #278 Design Choice 5 is B or C; this plan is implemented only under #278 answer A or B (under C #278 delivers the secondary itself); PR 1 of this plan needs #278 PR 2 (and PR 3 under B), PR 2 needs the PR of #278 that creates the health check (its PR 4) |
| #4413 forwarding of `ILeveledCacheProvider` | Open | Independent; the secondary branch forwards nothing leveled |
| Block 500-549 of the EventId registry | ✅ Assigned | 538-541 are this plan's |
| Maintainer answers to Design Choices 1 to 4 | Pending | Asked when work starts; option C of Design Choice 1 ends the work |

Order inside the family: independent of #4414, #4416 and #4415.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas when work starts, records the answers under `## Maintainer Decisions`, then briefs the workers after #278 PR 4.
