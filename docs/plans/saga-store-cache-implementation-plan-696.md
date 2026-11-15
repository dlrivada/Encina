# Implementation Plan: Distributed cache for saga state reads

> **Issue**: [#696](https://github.com/dlrivada/Encina/issues/696)
> **Type**: Feature
> **Complexity**: Medium (5 phases, 3 pull requests of 9, 12 and 12 files)
> **Estimated Scope**: about 450 lines of production code, about 1000 lines of tests
> **Milestone**: v0.18.0 - Integration & Platform Capabilities (caching, messaging, saga)

---

## Summary

The issue proposes `CachingSagaStoreDecorator`: a cache-aside `GetAsync(sagaId)` and a write-through `UpdateAsync`, so that the step path (`GetAsync`, mutate, `UpdateAsync`) of a multi-step saga reads the database once instead of once per step.

Re-checked against `origin/main` (db1e7f688), that design is unsafe on the path it targets, and the plan therefore changes its scope (Design Choice 1, a Dilemma, recommended option B):

1. **The step path mutates the state it reads.** `SagaOrchestrator.AdvanceAsync`, `CompleteAsync`, `StartCompensationAsync`, `CompensateStepAsync`, `FailAsync` and `TimeoutAsync` call `_store.GetAsync`, change the returned `ISagaState` in place, then call `_store.UpdateAsync` (`SagaOrchestrator.cs` lines 162-169, 226-247, 300-354, 392-394, 492-515).
2. **The EF store depends on that identity.** `SagaStoreEF.UpdateAsync` does nothing but set `LastUpdatedAtUtc` on the tracked entity (`// EF Core tracks changes automatically`) and rejects anything that is not its own `SagaState`; the commit is whoever calls `SaveChangesAsync`. A state served from a cache is a detached copy: the orchestrator would mutate it, `UpdateAsync` would stamp it, and `SaveChangesAsync` would persist nothing. That is a silent loss of saga progress.
3. **Nothing in `Encina.Messaging` owns the commit.** `SagaOrchestrator` never calls `SaveChangesAsync` (the stores' own callers elsewhere in the package are the outbox, scheduler and dead letter); the commit happens in the transaction behavior or the application. A write-through `Set` in `UpdateAsync` would publish a state that a failed commit then discards, and a read by another instance would act on it. The #720 plan (transactional compensation) keeps `ISagaStore.SaveChangesAsync` as the EF commit point and records "Caching: not applicable: saga state needs read-your-writes".
4. **There is no tenant on the saga row** (`ISagaState` has no tenant member), so the key mirrors the saga id.

The one safe use of the issue's idea is the **read-only status path**: `SagaOrchestrator.GetAsync<TSagaData>` (line 435) returns a `SagaStateSnapshot` and never writes. The plan adds a small port, `ISagaStatusReader`, in `Encina.Messaging`, used by that method, plus a caching implementation and an evicting store decorator in `Encina.Caching`. Step reads never use the cache; the stuck and expired queries are never cached.

This plan builds on the foundation of #695 (`StoreCacheKey`, `StoreCachingRegistration`, `StoreCacheTelemetry`, `StoreCacheLog` ids 350-353, `StoreCacheErrors`, the range `CachingStores` 350-389) and owns two shared pieces that #699 reuses: the **evicting write decorator pattern** and the **copy-on-read state entry**.

**Affected packages:** `Encina.Messaging` (the port, its default implementation, the optional constructor parameter, one registration line), `Encina.Caching` (decorators, options, registration). No provider package changes.

**Provider category**: Database (10) for the saga stores: the cache layer is provider independent, and the integration scenario runs against all ten (ADO x3, Dapper x3, EF Core x3, MongoDB), which is where the EF tracking hazard is proven. Caching (8): Memory (unit and integration) and the Redis family through the `ICacheProvider` contract.

**Sibling plans (caching family)**: #695 owns the shared foundation; #699 reuses the state-entry pattern; #720 (compensation transaction) adds `GetCompensationRetryDueAsync` to `ISagaStore`: the evicting decorator forwards unknown reads untouched, and when #720 lands its method is a pass-through with no cache. #2408 owns post-commit invalidation of request-level caches; it does not cover store-level eviction.

---

## Design Choices

<details>
<summary><strong>1. Scope: what the saga cache serves</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The issue as written: a decorator on `ISagaStore` that serves `GetAsync` and writes through in `UpdateAsync`** | Matches the acceptance list; shortest | Breaks the EF store (detached copies, lost updates, see Summary 2); publishes uncommitted state (Summary 3); two instances advancing one saga from stale reads |
| **B) Cache only the read-only status path (`SagaOrchestrator.GetAsync<T>`) through a new port, with an evicting `ISagaStore` decorator for writes** | Safe with every provider; the step path is untouched; status polling (UI, health dashboards) is the read-heavy use; bounded staleness is acceptable for a status view | Does not reduce the reads of the step path, the motivation of the issue; one new public interface |
| **C) Do not implement; record that saga state needs read-your-writes and close the issue as not applicable** | No code, no risk | Drops the one safe gain; the #720 plan already says "not applicable" for its own scope only |

### Chosen Option: **B - status reads only** (recommended, pending the maintainer)

### Rationale

- C wins if the maintainer judges status polling too rare to justify a public port; A would win only if the saga stores were changed to optimistic concurrency with a version column and the cache returned versioned copies that `UpdateAsync` verifies, which is a different feature (a version check on every provider) and not this issue.
- The issue's acceptance items "write-through", "TTL = saga timeout" and "invalidate on completion" become: evict on write, TTL bounded by `TimeoutAtUtc`, long TTL for terminal sagas (Design Choice 3). "Time-dependent queries bypass the cache" holds unchanged.
- Class: Dilemma

</details>

<details>
<summary><strong>2. The seam in Encina.Messaging</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `public interface ISagaStatusReader { Task<Either<EncinaError, Option<ISagaState>>> GetAsync(Guid sagaId, CancellationToken cancellationToken = default); }` with an internal `StoreSagaStatusReader(ISagaStore)` registered by `RegisterSagas` with `TryAddScoped`; `SagaOrchestrator` takes it as an optional trailing constructor parameter and `GetAsync<T>` reads through it (falls back to the store when null)** | Explicit contract: reads of the status view are a different operation from reads for update; additive (the 20 `new SagaOrchestrator(` sites in `src` and `tests` still compile because the parameter is optional); the cache is a decorator of the port | One public interface and one optional parameter |
| **B) The orchestrator tests `_store is ISagaStatusReader` and the evicting decorator implements it** | No registration line | A type test on a store couples the orchestrator to decorator internals; hidden behavior |
| **C) A query service in `Encina.Caching` that applications call instead of the orchestrator** | Messaging unchanged | The orchestrator's own `GetAsync<T>` stays uncached; two ways to read the same state |

### Chosen Option: **A - `ISagaStatusReader`** (recommended, pending the maintainer)

### Rationale

- Option C wins when the maintainer does not want a public change in `Encina.Messaging` for this issue; B is rejected because it makes the read path depend on the shape of a decorator.
- The reader is scoped (the default implementation wraps the scoped `ISagaStore`); the caching decorator receives only singletons besides the inner reader, so no scoped service reaches a singleton.
- Class: Dilemma

</details>

<details>
<summary><strong>3. Staleness, eviction and TTL</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Entry TTL `Duration` (default 5 seconds) for a non-terminal saga, `TerminalDuration` (default 1 hour) for `Completed` and `Compensated`, both capped by `TimeoutAtUtc` when present; the evicting decorator removes the key on `AddAsync` and `UpdateAsync` and again after `SaveChangesAsync` returns `Right` through the same store instance** | Terminal states never change, so a long TTL is exact; the non-terminal staleness is bounded by 5 seconds and by the two eviction points that exist | A reader on another instance can still see a pre-commit state for up to `Duration` (the commit is outside the store, Summary 3); stated in the public documentation |
| **B) TTL only, no eviction** | No decorator on `ISagaStore` | The status stays stale for the whole TTL after every step on the same instance |
| **C) Eviction on write plus a cross-instance invalidation message over `IPubSubProvider`** | Other instances see the change sooner | Needs a backplane and a message contract for a status view; the eviction point is still before the commit |

### Chosen Option: **A - bounded TTL with two eviction points** (recommended, pending the maintainer)

### Rationale

- C wins if status views must converge in under a second across instances and the application already runs a Redis backplane; the bounded staleness of A is the price of not owning the commit, which only #720 and the transaction behavior change.
- `Failed` is not terminal for caching: the #720 plan retries compensation of failed sagas, so a failed saga uses the short TTL.
- The copy-on-read entry (`CachedSagaEntry` and an internal `CachedSagaState : ISagaState`) carries plain strings, numbers and dates so every cache serializer round-trips it; the reader returns a fresh copy per call.
- Class: Dilemma

</details>

<details>
<summary><strong>4. What is never cached</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `GetStuckSagasAsync`, `GetExpiredSagasAsync` and every other `ISagaStore` member pass through uncached; `None` and `Left` results are never cached; an entry larger than `MaxCachedDataLength` (default 65536 characters of `Data`) is skipped** | These queries are time dependent (a cached list is wrong as time passes); an absent saga may be created a moment later; a `Left` must be seen | Large sagas are never served from the cache |
| **B) Cache the stuck and expired lists with a short TTL** | Fewer queries from the monitor | The monitor exists to find sagas that moved on; a stale list acts on finished sagas |

### Chosen Option: **A - pass through**

### Rationale

- **Class:** Settled
- **Settled by:** the acceptance of #696 ("time-dependent queries (`GetStuckSagasAsync`, `GetExpiredSagasAsync`) bypass cache") and AGENTS.md section 3 (errors are never swallowed: a `Left` from a store is never turned into a cached success). Option B contradicts the issue.

</details>

<details>
<summary><strong>5. Registration and failure semantics</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaSagaCaching(Action<SagaCachingOptions>?)` in `Encina.Caching` replaces the `ISagaStore` and `ISagaStatusReader` descriptors with the decorators through `StoreCachingRegistration` (throws at registration when sagas are not registered); a cache failure falls through and is counted, with `CachingOptions.ThrowOnCacheErrors` it returns `Left store_cache.failed` (`Internal`)** | Same behavior as the inbox cache of #695; one call; fail-fast | Call-order rule as in #695 |
| **B) Register the decorators through `SagaOptions.EnableCaching` in Messaging** | The issue's configuration style | Messaging cannot reference `Encina.Caching`; a flag without an implementation needs its own startup validation |

### Chosen Option: **A - extension in `Encina.Caching`**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (registration completeness: an `AddEncina*` method registers every dependency it resolves, proven by a DI test with `ValidateOnBuild` and `ValidateScopes`; Railway Oriented Programming for the failure value) and the project reference direction (`Encina.Caching` references `Encina.Messaging`, not the reverse, `Encina.Caching.csproj`). ADR-062 gives the code its category. The call-order mechanism itself is the choice of #695 (its Design Choice 2); this plan uses the same helper whichever option the maintainer picks.

</details>

<details>
<summary><strong>6. Keys and EventIds</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `StoreCacheKey.Build(prefix, null, "saga", sagaId.ToString("N"))` (no tenant, the row has none); EventIds only the shared 350-353 of #695** | Injective by construction; nothing new to register | The log lines say `store=saga` and cannot tell reader from writer failures |
| **B) New ids 354-357 for saga-specific events** | More precise logs | Ids that duplicate the shared meaning; ids 354-360 are reserved for #698 |

### Chosen Option: **A - shared builder and shared ids**

### Rationale

- **Class:** Trivial
- **Settled by:** the family block 350-399 of the plan batch registry (ids packed: 350-353 are #695's, 354-360 are #698's) and AGENTS.md section 7. The tenant segment helper is `StoreCacheKey.Tenant` of #695, unused here because the saga row has no tenant; if a tenant column appears the key gains it in the same change.

</details>

---

## Implementation Phases

### Phase 1: The status-reader port in Encina.Messaging (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 9 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Messaging/Sagas/ISagaStatusReader.cs` | new (public interface) |
| 2 | `src/Encina.Messaging/Sagas/StoreSagaStatusReader.cs` | new (internal default, wraps `ISagaStore`) |
| 3 | `src/Encina.Messaging/Sagas/SagaOrchestrator.cs` | modify (optional `ISagaStatusReader? statusReader = null`; `GetAsync<T>` reads through it) |
| 4 | `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs` | modify (`RegisterSagas`: `services.TryAddScoped<ISagaStatusReader, StoreSagaStatusReader>()`) |
| 5 | `src/Encina.Messaging/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 6 | `tests/Encina.UnitTests/Messaging/Sagas/SagaOrchestratorStatusReaderTests.cs` | new |
| 7 | `tests/Encina.GuardTests/Messaging/Sagas/SagaOrchestratorGuardTests.cs` | modify (the new optional parameter accepts null: a valid-result test, not an exception test) |
| 8 | `tests/Encina.UnitTests/Messaging/Sagas/SagaStatusReaderRegistrationTests.cs` | new (DI test: resolves with `ValidateOnBuild` and `ValidateScopes`; a replaced reader wins) |
| 9 | `.github/coverage-manifest/Encina.Messaging.json` | modify (`mechanical-fixer`) |

Tasks:

1. **Create** `public interface ISagaStatusReader` and `internal sealed class StoreSagaStatusReader(ISagaStore store)` returning `store.GetAsync(...)`.
2. **Modify** `SagaOrchestrator`: the constructor gains `ISagaStatusReader? statusReader = null` after `deadLetterCapture` (the last position, so every existing call compiles); `GetAsync<TSagaData>` calls `(_statusReader is null ? _store.GetAsync(...) : _statusReader.GetAsync(...))`. No other method changes: every write path still reads `_store.GetAsync` directly.
3. **Callers of the constructor** (verified by grep on `origin/main`): 20 `new SagaOrchestrator(` sites (6 in `SagaOrchestratorGuardTests.cs`, 6 in `SagaOrchestratorTests.cs`, others in four test files and `SagaRunnerOutcomeScenarios.cs`); none changes because the parameter is optional.
4. **Tests**: `GetAsync<T>` uses the reader when present and the store when absent; the write paths never touch the reader (the reader is a strict mock that fails on any call); a reader `Left` returns `None` as today; DI resolves the default reader.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #696 caches saga state reads, but only for the read-only status view
(SagaOrchestrator.GetAsync<T>); the step path mutates the state it reads and the EF store relies on change tracking, so it must never
read from a cache. This PR adds the port the cache will decorate.

TASK:
1. Create ISagaStatusReader (public) and StoreSagaStatusReader (internal) in src/Encina.Messaging/Sagas.
2. Add the optional trailing parameter to SagaOrchestrator and make only GetAsync<T> use it; register the default in RegisterSagas.
3. Write the unit, guard and DI tests; ask mechanical-fixer for the PublicAPI line and the manifest.

KEY RULES:
- No write path of SagaOrchestrator reads through the reader.
- The new parameter is optional and last; no existing call site changes.
- ROP: Either<EncinaError, Option<ISagaState>>; XML docs on every public member.
- Zero warnings; CRAP of every method at most 10.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs, ISagaStore.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs (RegisterSagas)
- tests/Encina.GuardTests/Messaging/Sagas/SagaOrchestratorGuardTests.cs
```

</details>

### Phase 2: The caching reader and the evicting store decorator (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 12 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/MessagingStores/CachingSagaStatusReader.cs` | new (internal sealed) |
| 2 | `src/Encina.Caching/MessagingStores/EvictingSagaStoreDecorator.cs` | new (internal sealed) |
| 3 | `src/Encina.Caching/MessagingStores/CachedSagaEntry.cs` | new (internal; entry record and `CachedSagaState`) |
| 4 | `src/Encina.Caching/MessagingStores/SagaCachingOptions.cs` | new (public) |
| 5 | `src/Encina.Caching/MessagingStores/StoreCachingServiceCollectionExtensions.cs` | modify (adds `AddEncinaSagaCaching`) |
| 6 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 7 | `tests/Encina.UnitTests/Caching/MessagingStores/CachingSagaStatusReaderTests.cs` | new |
| 8 | `tests/Encina.UnitTests/Caching/MessagingStores/EvictingSagaStoreDecoratorTests.cs` | new |
| 9 | `tests/Encina.GuardTests/Caching/StoreCachingGuardTests.cs` | modify (`AddEncinaSagaCaching(null)`, options setters) |
| 10 | `tests/Encina.UnitTests/Caching/MessagingStores/SagaCachingRegistrationTests.cs` | new (DI test with `ValidateOnBuild` and `ValidateScopes`) |
| 11 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 12 | `changelog.d/696-saga-status-cache.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `internal sealed class CachingSagaStatusReader : ISagaStatusReader` with `(ISagaStatusReader inner, ICacheProvider cache, SagaCachingOptions options, IOptions<CachingOptions> cachingOptions, TimeProvider timeProvider, StoreCacheTelemetry telemetry, ILogger<CachingSagaStatusReader> logger)`: a hit returns `Right(Some(new CachedSagaState(entry)))`; a miss calls the inner reader, returns `Left` and `None` untouched, and caches a `Some` whose `Data` is within `MaxCachedDataLength` with TTL = (`TerminalDuration` for `Completed`/`Compensated`, `Duration` otherwise) capped by `TimeoutAtUtc - now` (a cap at or below zero skips the write).
2. **Create** `internal sealed class EvictingSagaStoreDecorator : ISagaStore`: every member delegates to the inner store; `AddAsync` and `UpdateAsync` remove the key after the inner call returned `Right`; `SaveChangesAsync` removes the keys the instance recorded (a small per-scope list of ids touched since the last save) after the inner call returned `Right`. It never serves a read. The key is `StoreCacheKey.Build(prefix, null, "saga", sagaId.ToString("N"))`.
3. **Create** `public sealed class SagaCachingOptions { TimeSpan Duration (5 s); TimeSpan TerminalDuration (1 h); int MaxCachedDataLength (65536) }` with positive-value setters.
4. **Add** `AddEncinaSagaCaching` to `StoreCachingServiceCollectionExtensions`: guards `services`, configures the options, `StoreCachingRegistration.Decorate<ISagaStore>` with the evicting decorator and `Decorate<ISagaStatusReader>` with the caching reader; throws when sagas are not registered.
5. **Tests (unit)**: hit and miss; terminal and non-terminal TTL; the `TimeoutAtUtc` cap and the skipped write at or below zero; `None`, `Left` and oversized entries never cached; the copy is independent of the cached entry; eviction on `AddAsync`, `UpdateAsync` and after `SaveChangesAsync` and not after a `Left`; `GetStuckSagasAsync` and `GetExpiredSagasAsync` delegate with no cache call; a throwing cache falls through and with `ThrowOnCacheErrors` returns `Left`.
6. **Tests (guard)**: extension and options setters. The decorators are internal; their constructor checks are asserted by the unit tests (guard target 0 with that reason).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Issue #696. PR 1 added ISagaStatusReader to Encina.Messaging; PR 2 of #695 added StoreCacheKey, StoreCachingRegistration, StoreCacheTelemetry,
StoreCacheLog (350-353) and StoreCacheErrors to Encina.Caching.

TASK:
1. Create CachingSagaStatusReader, EvictingSagaStoreDecorator, CachedSagaEntry (+ CachedSagaState), SagaCachingOptions and AddEncinaSagaCaching.
2. The caching reader serves only the status path; the evicting decorator never serves a read.
3. Write the unit, guard and DI tests; ask mechanical-fixer for PublicAPI lines, the manifest and the changelog fragment.

KEY RULES:
- The step path never reads from the cache; stuck and expired queries pass through; None and Left are never cached.
- Eviction only after the inner call returned Right; SaveChangesAsync evicts the ids recorded since the last save.
- Return a fresh copy of the cached entry per call; never hand out a shared mutable instance.
- Keys through StoreCacheKey; no key, id or data in logs or tags; exceptions through ForLogging().
- No scoped service in a singleton. Zero warnings; CRAP of every method at most 10.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/ISagaStore.cs, SagaOrchestrator.cs, ISagaState.cs
- src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs
- src/Encina.Caching/MessagingStores (from #695)
- .github/coverage-manifest/Encina.Caching.json
```

</details>

### Phase 3: Per-provider integration scenario (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 12 files:

| # | File | Change |
|---|------|--------|
| 1 | `tests/Encina.IntegrationTests/Messaging/SagaCachingScenario.cs` | new (shared scenario) |
| 2-4 | `tests/Encina.IntegrationTests/ADO/{SqlServer,PostgreSQL,MySQL}/Sagas/SagaStoreADOCachingTests.cs` | new (3) |
| 5-7 | `tests/Encina.IntegrationTests/Dapper/{SqlServer,PostgreSQL,MySQL}/Sagas/SagaStoreDapperCachingTests.cs` | new (3) |
| 8-10 | `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/{SqlServer,PostgreSQL,MySQL}/Sagas/SagaStoreEF<Db>CachingTests.cs` | new (3) |
| 11 | `tests/Encina.IntegrationTests/Infrastructure/MongoDB/Stores/SagaStoreMongoDBCachingTests.cs` | new |
| 12 | `docs/messaging/sagas.md` | modify (`docs-writer`: the status cache, its bounded staleness, why the step path is not cached) |

The scenario asserts, per provider with a Memory cache: a status read after a step is served from the cache on the second call; after `UpdateAsync` and `SaveChangesAsync` the next status read is a miss and returns the new step; a full multi-step run through `SagaOrchestrator` with the caching registrations active persists every step (the EF regression guard of Summary 2: the run ends with the stored `CurrentStep` equal to the number of steps); a terminal saga is served for the long TTL (fake time). Same collections and harness style as the existing `SagaStore*Tests.cs` of each provider.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
PR 1 and PR 2 of issue #696 are merged. This PR proves the saga status cache against the ten database providers.

TASK:
1. Write SagaCachingScenario and one thin class per provider with its shared collection fixture.
2. The scenario must run a full multi-step saga with AddEncinaSagaCaching active and assert every step is persisted.
3. Ask docs-writer for the sagas.md section.

KEY RULES:
- [Collection("<Family>-<Database>")] shared fixtures only; ClearAllDataAsync in InitializeAsync; never dispose the shared fixture.
- Real stores and the real decorators; no mocks of either.

REFERENCE FILES:
- tests/Encina.IntegrationTests/ADO/PostgreSQL/Sagas/SagaStoreADOTests.cs
- tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Sagas/SagaStoreEFIntegrationTests.cs
- docs/messaging/sagas.md
```

</details>

### Phase 4: Observability, logging and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **Telemetry**: the shared `StoreCacheTelemetry` of #695 with `store=saga` (`hit`, `miss`, `skipped`, `failure`, and the eviction counter). No saga id, key or data in a tag; no tenant tag because the saga operations carry no tenant scope (ADR-054 D4, D6).
2. **Logs**: the shared ids 350-353 of the range `CachingStores`; no new ids. The `skipped` reason codes are `too_large` and `no_retention_left`.
3. **Error code**: the shared `store_cache.failed` (`ErrorCategory.Internal`, ADR-062 R4); this plan adds no code.
4. **Coverage targets** (per file, per flag; measured values replace the estimates before PR 2 and PR 3 open):

| File | unit | guard | integration | Justification |
|------|------|-------|-------------|---------------|
| `Sagas/StoreSagaStatusReader.cs` (Messaging) | 100 | 0 | 0 | One delegating call, fully unit tested; internal type without `InternalsVisibleTo` for `Encina.GuardTests` |
| `Sagas/SagaOrchestrator.cs` (Messaging, existing 90/45/30) | 90 | 45 | 30 | The touched method is covered by the new unit test; the existing targets are kept, never lowered |
| `MessagingStores/CachingSagaStatusReader.cs` | 95 | 0 | 70 | Every TTL and skip branch is unit tested; internal; the scenario reaches hit, miss and expiry against real stores |
| `MessagingStores/EvictingSagaStoreDecorator.cs` | 95 | 0 | 70 | Every member and eviction point unit tested; internal; the scenario proves persistence through it |
| `MessagingStores/CachedSagaEntry.cs` | 100 | 0 | 50 | Mapping fully unit tested; reached by the scenario on every hit |
| `MessagingStores/SagaCachingOptions.cs` | 100 | 100 | 0 | Setter validation reachable from both flags |
| `MessagingStores/StoreCachingServiceCollectionExtensions.cs` (added method) | 95 | 90 | 0 | DI test and guard tests reach every line |

5. **Contract, property, load, benchmark**: no new contract test (the decorated interface keeps its contract; the unit tests drive the real `FakeSagaStore` behind it); no property test (no non-trivial invariant beyond the unit cases); no load or benchmark test (a status view cached for seconds; the step path is not cached); the manifest `reason` says so.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
The saga status cache of issue #696 exists. Telemetry, logs and error codes are the shared ones of #695; this phase measures coverage and
records targets.

TASK:
Run each flag's test project with coverage, compare with the per-file targets of the plan, and ask mechanical-fixer to record the
targets with one-sentence justifications in Encina.Caching.json and Encina.Messaging.json.

KEY RULES:
- Never lower an existing target of SagaOrchestrator.cs.
- No new EventId or error code; the shared ones are used.
- Documentation never types coverage figures.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Caching.json
- .github/coverage-manifest/Encina.Messaging.json
```

</details>

### Phase 5: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation on `ISagaStatusReader`, `SagaCachingOptions` and `AddEncinaSagaCaching` (summary, remarks with the bounded-staleness statement and the call-order rule, example).
2. `docs/messaging/sagas.md` through `docs-writer` (PR 3); `changelog.d/696-saga-status-cache.added.md` (PR 2); PublicAPI of `Encina.Messaging` and `Encina.Caching`.
3. Build with `dotnet build --configuration Release` (0 warnings), `dotnet test` of each flag, `dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
Issue #696 is implemented. Finish documentation and verification.

TASK:
Write the XML docs, ask docs-writer for the sagas page, ask mechanical-fixer for the PublicAPI lines and the changelog fragment, and run
the build, tests and coverage checks.

KEY RULES:
- The docs say what is cached (the status view), what is not (the step path) and the staleness bound; they never type coverage figures.

REFERENCE FILES:
- docs/messaging/sagas.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 3 | ROP, errors never swallowed, registration completeness with a DI test, `TimeProvider` |
| AGENTS.md section 5 (database, caching) | The scenario covers the 10 database providers; Memory and the Redis family cover the cache side |
| ADR-062 | The shared error code states its `ErrorCategory` (`Internal`) |
| ADR-054 D4, D6 | No tenant tag without a tenant scope; no keys or data in telemetry |
| #720 plan | Saga state needs read-your-writes; `ISagaStore.SaveChangesAsync` stays the EF commit point |
| #712 | Cache Integration epic |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| ISagaStore, ISagaState | src/Encina.Messaging/Sagas | the decorated abstraction and the state shape |
| SagaOrchestrator | src/Encina.Messaging/Sagas/SagaOrchestrator.cs | the status path (`GetAsync<T>`) and the write paths that must not read the cache |
| SagaStoreEF | src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs | the change-tracking dependency that rules out caching the step path |
| RegisterSagas | src/Encina.Messaging/MessagingServiceCollectionExtensions.cs | registers the store once for all providers |
| InstrumentedSagaStore | src/Encina.OpenTelemetry/MessagingStores/InstrumentedSagaStore.cs | existing store decorator layered with the evicting decorator |
| StoreCacheKey, StoreCachingRegistration, StoreCacheTelemetry, StoreCacheLog, StoreCacheErrors | src/Encina.Caching/MessagingStores (#695) | the shared foundation |
| ICacheProvider, CachingOptions | src/Encina.Caching | the cache and `ThrowOnCacheErrors` |
| FakeSagaStore | src/Encina.Testing.Fakes/Stores/FakeSagaStore.cs | unit-test double of the inner store |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (`CachingStores` from #695) | 350-389 | This plan adds no id; it reuses 350-353; 354-360 are reserved by #698 |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 6 | 3 | New: port, default reader, caching reader, evicting decorator, entry, options; modified: `SagaOrchestrator.cs`, `MessagingServiceCollectionExtensions.cs`, `StoreCachingServiceCollectionExtensions.cs` |
| Tests | 16 | 2 | New: orchestrator unit, DI (PR 1); reader unit, decorator unit, DI (PR 2); scenario and 10 provider classes (PR 3); modified: orchestrator guard test, caching guard test |
| Documentation | 0 | 1 | `docs/messaging/sagas.md` |
| Configuration | 1 | 4 | Changelog fragment; `Encina.Messaging.json`, `Encina.Caching.json`, two PublicAPI files |
| **Total** | **23** | **10** | 3 pull requests: 9, 12 and 12 files (33 file touches), each at most 15 files and about 700 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #696: a cache for saga state reads. The saga step path
mutates the state it reads and the EF store relies on change tracking, so only the read-only status view (SagaOrchestrator.GetAsync<T>)
is cached; writes evict. It builds on the shared foundation of #695 in Encina.Caching.

IMPLEMENTATION OVERVIEW:
1. PR 1: ISagaStatusReader (Encina.Messaging), its default StoreSagaStatusReader, the optional constructor parameter and the use in
   GetAsync<T> only.
2. PR 2: CachingSagaStatusReader, EvictingSagaStoreDecorator, CachedSagaEntry, SagaCachingOptions and AddEncinaSagaCaching.
3. PR 3: integration scenario over the 10 database providers (full multi-step run) and the sagas page.

KEY PATTERNS:
- Step reads never touch the cache; stuck and expired queries pass through; None and Left are never cached.
- TTL: short for non-terminal sagas, long for Completed and Compensated, capped by TimeoutAtUtc; eviction on AddAsync, UpdateAsync and after SaveChangesAsync.
- A cache failure falls through; ThrowOnCacheErrors returns Left store_cache.failed (Internal).
- Keys through StoreCacheKey; shared EventIds 350-353; no key, id or data in telemetry or logs.
- Apply Design Choices 1, 2 and 3 as the Maintainer Decisions section says.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/ISagaStore.cs, SagaOrchestrator.cs
- src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina.Caching/MessagingStores
- .github/coverage-manifest/Encina.Messaging.json, Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | The feature: a cache of the saga status view with bounded staleness and eviction on write. |
| 2 | OpenTelemetry | ✅ Include | The shared counters with `store=saga`; no ids or data in tags; no tenant tag (no tenant scope, ADR-054 D4). |
| 3 | Structured Logging | ✅ Include | Shared ids 350-353; exceptions through `ForLogging()`. |
| 4 | Health Checks | ❌ N/A | The cache is optional and falls through; the cache providers have their own checks; `SagaHealthCheck` reads the store directly. |
| 5 | Validation | ✅ Include | Options setters reject non-positive values. |
| 6 | Resilience | ✅ Include | A cache outage never fails a saga operation; `ThrowOnCacheErrors` is the strict switch. |
| 7 | Distributed Locks | ❌ N/A | The step path is not cached, so there is no shared mutable cache entry to lock. |
| 8 | Transactions | ✅ Include | Nothing is published to the cache before the commit: the cache is filled only by reads, and eviction follows writes and `SaveChangesAsync`; the orchestrator itself never commits, so the plan claims bounded staleness, not atomicity. |
| 9 | Idempotency | ❌ N/A | Status reads are idempotent; saga idempotency is the inbox's concern. |
| 10 | Multi-Tenancy | ✅ Include | The saga row has no tenant, so the key has none; `StoreCacheKey` keeps keys injective and the tenant segment is ready for a tenant column. |
| 11 | Module Isolation | ❌ N/A | Sagas are not module scoped. |
| 12 | Audit Trail | ❌ N/A | A status read is not an audit event; saga transitions are audited by their own owners. |

**Deferred items:** none that need an issue from this plan. Optimistic concurrency with a version column on every saga store (the only way to cache the step path safely) is a different feature and is not opened here: it needs a maintainer decision first (Design Choice 1, option A).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| #695 foundation (`StoreCacheKey`, `StoreCachingRegistration`, telemetry, logs, errors) | ⏭️ Sibling plan | PR 2 of this plan needs PR 1 and PR 2 of #695 merged; PR 1 of this plan is independent |
| Saga stores of the 10 database providers | ✅ Available | ADO x3, Dapper x3, EF Core x3, MongoDB |
| #720 (compensation transaction) | ⏭️ Sibling plan | Adds `GetCompensationRetryDueAsync` to `ISagaStore`; the evicting decorator forwards it untouched (an implementer-first order is #720's concern) |
| Maintainer answers to Design Choices 1, 2 and 3 | Pending | Asked when work starts |

Blocking prerequisite for PR 2 only: #695 PR 1 and PR 2.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1, 2 and 3) when work starts, records the answers under `## Maintainer Decisions`, then briefs PR 1 in parallel with #695 and PR 2 and PR 3 after #695 PR 2.
2. If the maintainer chooses option C of Design Choice 1, close #696 with the evidence of the Summary; if option A, a version-column feature for the ten stores is opened first.
3. #699 reuses the evicting-decorator pattern and the copy-on-read entry after PR 2.
