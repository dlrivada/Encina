# Implementation Plan: Distributed cache for choreography state lookups

> **Issue**: [#699](https://github.com/dlrivada/Encina/issues/699)
> **Type**: Feature
> **Complexity**: Medium (5 phases, 3 pull requests of 7, 12 and 12 files)
> **Estimated Scope**: about 350 lines of production code, about 800 lines of tests
> **Milestone**: v0.18.0 - Integration & Platform Capabilities (caching, messaging, saga)

---

## Summary

The issue asks for `CachingChoreographyStateStoreDecorator`: a cache-aside `GetAsync(correlationId)` and a write-through on `UpdateAsync`, `AddEventAsync` and `AddCompensationAsync`, "identical to the Saga Store caching pattern".

Re-checked against `origin/main` (db1e7f688), the plan finds that this issue sits on ground that does not exist yet and shares every hazard of #696:

1. **There is nothing to decorate today.** `IChoreographyStateStore` (`src/Encina.Messaging/Choreography/IChoreographyStateStore.cs`) has no implementation in `src/` and no DI registration (no `AddEncinaChoreography`, no `UseChoreography` on `MessagingConfiguration`); `docs/messaging/sagas.md` documents a feature that is not wired (open #1467, #1471, #1472). The persistent stores are #592 (database providers) and #593 (Marten, EventStoreDB). #592 also proposes a different interface (`GetByCorrelationIdAsync`, `UpdateStateAsync`, `RecordEventAsync`, ...), so member names here follow whatever #592 merges; the plan states the design by role (read, write, time-dependent query).
2. **The state is mutable, and a handler that reads it to update it is read-modify-write.** `IChoreographyState` has settable members (`CorrelationId`, `SagaType`, `StateData`, `Status`, `LastUpdatedAtUtc`, `EventCount`, ...); the reaction handlers documented in `docs/messaging/sagas.md` read from the store and write their results back through it. A cached instance handed to a mutating caller is a shared object, and a copy returned to a store that relies on change tracking (EF Core, the way `SagaStoreEF.UpdateAsync` does) would lose the change; see #696, Summary 1 to 3.
3. **The compensation list drives side effects.** `GetPendingCompensationsAsync` returns the compensations still to run, most recent first, and `MarkCompensationCompletedAsync` removes one; a stale list runs a compensation twice. It must never come from a cache. Neither may `GetStuckSagasAsync`.
4. **The contract is not ROP.** Members return `Task` or `Task<T>` and signal failure by exception; the decorator cannot return a `Left` and keeps that contract.

The plan therefore applies the pattern of #696 (its recommended option, pending the maintainer): a read-only status port, a caching reader for it, and an **evicting** decorator on the store for every write; the handler read-modify-write path never reads from the cache. It is sequenced **after #592 and #1467**, which create the store and its registration. This plan reuses the foundation of #695 (`StoreCacheKey`, `StoreCachingRegistration`, `StoreCacheTelemetry`, shared logs 350-353, `StoreCacheErrors`) and the copy-on-read entry and evicting-decorator pattern of #696.

**Affected packages:** `Encina.Messaging` (the port, its default implementation and the registration line inside the registration #592 adds), `Encina.Caching` (reader, decorator, options, registration). No provider package changes.

**Provider category**: Database (10) for the persistent stores #592 adds (ADO x3, Dapper x3, EF Core x3, MongoDB); the decorator is provider independent and #592's per-provider tests gain the cached scenario in PR 3. Event sourcing (#593, Marten, EventStoreDB) is outside the database matrix and works through the same interface. Caching (8): Memory (unit and integration), Redis family through the `ICacheProvider` contract.

**Sibling plans (caching family)**: #695 (foundation), #696 (the pattern and its Dilemmas: if the maintainer answers C there, the answer here is C), #698 and #697 (independent). #720's saga compensation work does not touch this store.

---

## Design Choices

<details>
<summary><strong>1. Scope and timing</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The issue as written once #592 exists: a decorator serving `GetAsync` and writing through, on the handler path** | Matches the acceptance list | The handler path is read-modify-write on a mutable state: stale reads lose updates across instances and a detached copy breaks tracking stores; it would need an optimistic version on `IChoreographyState` that no provider has |
| **B) The pattern of #696: a read-only status port with a caching reader, an evicting decorator on every write, the compensation and stuck queries uncached; implemented after #592 and #1467** | Safe with every provider; small; the status view of a running choreography (dashboards, support tools) is the read-heavy use | Does not reduce the handler's own reads, the motivation of the issue; one public interface |
| **C) Close the issue until #592 and the choreography event bus exist and show a measured need** | No code for a feature that cannot run today | Delays the cache; the pattern is cheap once #696 is built |

### Chosen Option: **B - status reads only, after #592** (recommended, pending the maintainer)

### Rationale

- C wins if the maintainer judges that nothing should be built on an unimplemented feature (and answers C on #696); A would win only with an optimistic version column on every store, a feature of its own.
- The issue's acceptance items "write-through" and "time-dependent queries bypass cache" become: evict on write and never cache `GetStuckSagasAsync`.
- Class: Dilemma

</details>

<details>
<summary><strong>2. What the reader returns and what is cached</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An internal immutable `CachedChoreographyEntry` of plain values (correlation id, saga type, state data, status, timestamps, event count, error message); a hit returns a fresh internal `CachedChoreographyState : IChoreographyState`; only a found state is cached; terminal statuses (`Completed`, `Compensated`) use `TerminalDuration` (default 1 hour), others `Duration` (default 5 seconds); `Failed` uses the short TTL; an entry over `MaxCachedStateLength` (default 65536 characters of `StateData`) is skipped** | No shared instance; serializer independent; `null` ("not found") is never cached, because the state may be created a moment later | One more copy type |
| **B) Cache the provider's state object** | No copy type | Shared mutable instance; provider types (tracking entities) in a shared cache |

### Chosen Option: **A - copy-on-read entry**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (provider coherence: no provider-specific type crosses an abstraction; the abstraction is `IChoreographyState` with settable members, `IChoreographyState.cs`) and the same hazard analysis as #696 (Summary 2). Option B hands one mutable object to concurrent callers.

</details>

<details>
<summary><strong>3. What is never cached</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `GetPendingCompensationsAsync` and `GetStuckSagasAsync` pass through; `CreateAsync`, `UpdateAsync`, `AddEventAsync`, `AddCompensationAsync` and `MarkCompensationCompletedAsync` pass through and then evict the state key after the inner call returned without throwing** | A compensation list or a stuck list is never stale; every write path invalidates | A reader on another instance may see a pre-commit state for up to `Duration` (the commit is outside the store); stated in the documentation |
| **B) Cache the compensation list with a short TTL** | Fewer reads in a failing choreography | A stale list runs a compensation twice |

### Chosen Option: **A - pass through and evict**

### Rationale

- **Class:** Settled
- **Settled by:** the acceptance of #699 ("time-dependent queries bypass cache (`GetStuckSagasAsync`)") and the contract of `GetPendingCompensationsAsync` / `MarkCompensationCompletedAsync` in `IChoreographyStateStore.cs` (the list is consumed as work is done); AGENTS.md section 3 (a saga compensates exactly what is pending). B contradicts both.

</details>

<details>
<summary><strong>4. Failure semantics and keys</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A failing cache call falls through to the inner store and is counted and logged by exception type (`ForLogging()`); with `CachingOptions.ThrowOnCacheErrors` the cache exception is rethrown, because the wrapped contract signals failure by exception; the key is `StoreCacheKey.Build(KeyPrefix, null, "choreo", correlationId)` (the id is percent-encoded; the state has no tenant); EventIds are the shared 350-353** | The decorator never adds a failure mode a caller did not already handle; the key is injective; nothing new to register | The strict switch throws, unlike the `Either` stores of #695 and #696, because this interface has no `Left` |
| **B) Convert to `Either`** | ROP | Changes the interface that #592 owns; not this issue |

### Chosen Option: **A - fall through, rethrow only under the strict switch**

### Rationale

- **Class:** Settled
- **Settled by:** `IChoreographyStateStore.cs` (members return `Task` and throw; there is no `Either`), AGENTS.md section 3 (an exception passed to a logger goes through `ForLogging()`; errors are never swallowed) and the family block of EventIds 350-399 of the plan batch registry (ids 350-353 are #695's shared failure logs). No new error code is created, so there is no `ErrorCategory` to state. Whether #592 turns the interface into ROP is #592's decision; if it does, this decorator returns `Left store_cache.failed` (`Internal`, ADR-062) under the strict switch like the sibling decorators.

</details>

<details>
<summary><strong>5. Registration</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaChoreographyCaching(Action<ChoreographyCachingOptions>?)` in `Encina.Caching` using `StoreCachingRegistration` of #695; it throws at registration when no `IChoreographyStateStore` is registered (today that is always the case on `main`, which is why this plan waits for #592) and replaces the store and the status reader with the decorators** | Same behavior and call-order rule as #695 and #696 | Unusable until #592 lands |
| **B) A flag on the choreography options in Messaging** | The issue's style | Messaging cannot reference `Encina.Caching` |

### Chosen Option: **A - extension in `Encina.Caching`**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (registration completeness proven by a DI test with `ValidateOnBuild` and `ValidateScopes`) and the project reference direction (`Encina.Caching` references `Encina.Messaging`, `Encina.Caching.csproj`). The call-order mechanism is the choice of #695 (its Design Choice 2); this plan follows whichever option the maintainer picks there.

</details>

---

## Implementation Phases

### Phase 1: The status-reader port in Encina.Messaging (pull request 1, after #592 and #1467)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 7 files (member names of the store are the ones #592 merges):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Messaging/Choreography/IChoreographyStatusReader.cs` | new (public; one read member returning the state or null) |
| 2 | `src/Encina.Messaging/Choreography/StoreChoreographyStatusReader.cs` | new (internal; wraps `IChoreographyStateStore`) |
| 3 | the choreography registration method #592 or #1467 adds in `src/Encina.Messaging` | modify (one `TryAddScoped<IChoreographyStatusReader, StoreChoreographyStatusReader>()` line) |
| 4 | `src/Encina.Messaging/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 5 | `tests/Encina.UnitTests/Messaging/Choreography/ChoreographyStatusReaderTests.cs` | new |
| 6 | `tests/Encina.UnitTests/Messaging/Choreography/ChoreographyStatusReaderRegistrationTests.cs` | new (DI test with `ValidateOnBuild` and `ValidateScopes`) |
| 7 | `.github/coverage-manifest/Encina.Messaging.json` | modify (`mechanical-fixer`) |

Tasks:

1. **Create** the port and its default implementation (one delegating call).
2. **Register** the default next to the store registration of #592/#1467; whichever class consumes the read-only view (a dashboard, a support endpoint) takes `IChoreographyStatusReader`; the handler path keeps `IChoreographyStateStore`.
3. **Tests**: the default delegates; the registration resolves it; a replaced reader wins.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #699 caches choreography state reads, but only the read-only status view, because the
handler path is read-modify-write. #592 and #1467 already merged the store and the registration; this PR adds the port the cache decorates.

TASK:
1. Create IChoreographyStatusReader (public) and StoreChoreographyStatusReader (internal).
2. Register the default next to the choreography store registration.
3. Write the unit and DI tests; ask mechanical-fixer for the PublicAPI line and the manifest.

KEY RULES:
- No write path and no compensation read uses the reader.
- XML docs on every public member; zero warnings; CRAP of every method at most 10.

REFERENCE FILES:
- src/Encina.Messaging/Choreography/IChoreographyStateStore.cs, IChoreographyState.cs
- the choreography registration added by #592 / #1467
- src/Encina.Messaging/Sagas/ISagaStatusReader.cs (#696, same shape)
```

</details>

### Phase 2: The caching reader and the evicting decorator (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 12 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/MessagingStores/CachingChoreographyStatusReader.cs` | new (internal sealed) |
| 2 | `src/Encina.Caching/MessagingStores/EvictingChoreographyStateStoreDecorator.cs` | new (internal sealed) |
| 3 | `src/Encina.Caching/MessagingStores/CachedChoreographyEntry.cs` | new (internal; entry and `CachedChoreographyState`) |
| 4 | `src/Encina.Caching/MessagingStores/ChoreographyCachingOptions.cs` | new (public) |
| 5 | `src/Encina.Caching/MessagingStores/StoreCachingServiceCollectionExtensions.cs` | modify (adds `AddEncinaChoreographyCaching`) |
| 6 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 7 | `tests/Encina.UnitTests/Caching/MessagingStores/CachingChoreographyStatusReaderTests.cs` | new |
| 8 | `tests/Encina.UnitTests/Caching/MessagingStores/EvictingChoreographyStateStoreDecoratorTests.cs` | new |
| 9 | `tests/Encina.UnitTests/Caching/MessagingStores/ChoreographyCachingRegistrationTests.cs` | new (DI test with `ValidateOnBuild` and `ValidateScopes`) |
| 10 | `tests/Encina.GuardTests/Caching/StoreCachingGuardTests.cs` | modify (`AddEncinaChoreographyCaching(null)`, options setters) |
| 11 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 12 | `changelog.d/699-choreography-state-cache.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `CachingChoreographyStatusReader` with the shape of `CachingSagaStatusReader` of #696: hit returns a fresh `CachedChoreographyState`; miss calls the inner reader, caches a found state within `MaxCachedStateLength` with the TTL of Design Choice 2, never caches `null` or a thrown inner failure (an inner exception propagates untouched, it is the store's failure and is never cached).
2. **Create** `EvictingChoreographyStateStoreDecorator : IChoreographyStateStore`: every member delegates; the five write members evict the state key after the inner call returned; `GetPendingCompensationsAsync` and `GetStuckSagasAsync` delegate with no cache call; no read is served from the cache.
3. **Create** `ChoreographyCachingOptions { Duration 5 s, TerminalDuration 1 h, MaxCachedStateLength 65536 }` with positive-value setters.
4. **Add** `AddEncinaChoreographyCaching` (guards `services`; options; `StoreCachingRegistration.Decorate` for the store and the reader; throws when no store is registered).
5. **Tests (unit)**: hit, miss, TTL by status, `null` and oversized never cached, the copy is independent, every write evicts and a throwing inner call does not evict-and-hide (the exception propagates), the two time-dependent members make no cache call, a throwing cache falls through and with `ThrowOnCacheErrors` rethrows.
6. **Tests (guard)**: extension and options setters; the decorators are internal (guard target 0 with that reason).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Issue #699. PR 1 added IChoreographyStatusReader; #695 added the foundation and #696 the pattern (CachingSagaStatusReader,
EvictingSagaStoreDecorator, CachedSagaEntry) in Encina.Caching.

TASK:
1. Create CachingChoreographyStatusReader, EvictingChoreographyStateStoreDecorator, CachedChoreographyEntry (+ CachedChoreographyState),
   ChoreographyCachingOptions and AddEncinaChoreographyCaching, mirroring the saga versions.
2. Write the unit, guard and DI tests; ask mechanical-fixer for PublicAPI lines, the manifest and the changelog fragment.

KEY RULES:
- Compensation and stuck queries never touch the cache; writes evict; null is never cached; every hit is a fresh copy.
- The interface throws on failure (no Either): a cache failure falls through, the strict switch rethrows.
- Keys through StoreCacheKey; shared EventIds 350-353; no key, id or state in logs or tags; exceptions through ForLogging().
- No scoped service in a singleton. Zero warnings; CRAP of every method at most 10.

REFERENCE FILES:
- src/Encina.Messaging/Choreography/IChoreographyStateStore.cs
- src/Encina.Caching/MessagingStores/CachingSagaStatusReader.cs, EvictingSagaStoreDecorator.cs (#696)
- .github/coverage-manifest/Encina.Caching.json
```

</details>

### Phase 3: Per-provider scenario in the store tests of #592 (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 12 files:

| # | File | Change |
|---|------|--------|
| 1 | `tests/Encina.IntegrationTests/Messaging/ChoreographyCachingScenario.cs` | new (shared scenario) |
| 2-4 | `tests/Encina.IntegrationTests/ADO/{SqlServer,PostgreSQL,MySQL}/Choreography/ChoreographyStoreADOCachingTests.cs` | new (3) |
| 5-7 | `tests/Encina.IntegrationTests/Dapper/{SqlServer,PostgreSQL,MySQL}/Choreography/ChoreographyStoreDapperCachingTests.cs` | new (3) |
| 8-10 | `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/{SqlServer,PostgreSQL,MySQL}/Choreography/ChoreographyStoreEF<Db>CachingTests.cs` | new (3) |
| 11 | `tests/Encina.IntegrationTests/Infrastructure/MongoDB/Stores/ChoreographyStoreMongoDBCachingTests.cs` | new |
| 12 | `docs/messaging/sagas.md` | modify (`docs-writer`: the status cache and its staleness bound, in the choreography section that #1467 and #1472 correct) |

The folders and fixtures are those #592 creates for its own store tests (the `{Family}-{Database}` collections); the scenario runs a full create, event, update, compensate and complete choreography with the caching registrations active and asserts the stored state after every step (the guard against lost updates), that a status read after a write is a miss and the next is a hit, and that the compensation list is always read from the store.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
PR 1 and PR 2 of issue #699 are merged on top of #592. This PR proves the cache against the database providers' choreography stores.

TASK:
1. Write ChoreographyCachingScenario and one thin class per provider with its shared collection fixture, next to #592's store tests.
2. Ask docs-writer for the sagas page section.

KEY RULES:
- [Collection("<Family>-<Database>")] shared fixtures only; ClearAllDataAsync in InitializeAsync; never dispose the shared fixture.
- Real stores and real decorators; no mocks of either.

REFERENCE FILES:
- the store integration tests added by #592
- tests/Encina.IntegrationTests/Messaging/SagaCachingScenario.cs (#696)
```

</details>

### Phase 4: Observability, logging and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **Telemetry**: the shared `StoreCacheTelemetry` of #695 with `store=choreography`; no correlation id, key or state in a tag; no tenant tag (the state has no tenant scope, ADR-054 D4, D6).
2. **Logs**: shared ids 350-353; no new ids. **Error codes**: none (Design Choice 4), so no `ErrorCategory` to state; if #592 adopts ROP the shared `store_cache.failed` (`Internal`) is used.
3. **Coverage targets** (per file, per flag; measured values replace the estimates before PR 2 and PR 3 open):

| File | unit | guard | integration | Justification |
|------|------|-------|-------------|---------------|
| `Choreography/StoreChoreographyStatusReader.cs` (Messaging) | 100 | 0 | 0 | One delegating call, fully unit tested; internal type without `InternalsVisibleTo` for `Encina.GuardTests` |
| `MessagingStores/CachingChoreographyStatusReader.cs` | 95 | 0 | 70 | Every TTL, skip and failure branch unit tested; internal; the scenario reaches hit, miss and expiry against real stores |
| `MessagingStores/EvictingChoreographyStateStoreDecorator.cs` | 95 | 0 | 70 | Every member and eviction unit tested; internal; the scenario proves persistence through it |
| `MessagingStores/CachedChoreographyEntry.cs` | 100 | 0 | 50 | Mapping fully unit tested; reached on every hit |
| `MessagingStores/ChoreographyCachingOptions.cs` | 100 | 100 | 0 | Setter validation reachable from both flags |
| `MessagingStores/StoreCachingServiceCollectionExtensions.cs` (added method) | 95 | 90 | 0 | DI test and guard tests reach every line |

4. **Contract, property, load, benchmark**: no new contract or property test (the decorated interface keeps its contract; the unit cases cover the invariants); no load or benchmark test (a status view cached for seconds; the handler path is not cached); the manifest `reason` says so.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
The choreography status cache of issue #699 exists. Telemetry and logs are the shared ones of #695.

TASK:
Measure each flag with coverage, compare with the per-file targets of the plan, and ask mechanical-fixer to record the targets with
one-sentence justifications in Encina.Caching.json and Encina.Messaging.json.

KEY RULES:
- No new EventId or error code; never lower an existing target.
- Documentation never types coverage figures.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Caching.json
- .github/coverage-manifest/Encina.Messaging.json
```

</details>

### Phase 5: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation on `IChoreographyStatusReader`, `ChoreographyCachingOptions` and `AddEncinaChoreographyCaching` (bounded staleness, call order, example).
2. `docs/messaging/sagas.md` through `docs-writer` (PR 3); `changelog.d/699-choreography-state-cache.added.md` (PR 2); PublicAPI of `Encina.Messaging` and `Encina.Caching`.
3. Build with `dotnet build --configuration Release` (0 warnings), `dotnet test` of each flag, `dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
Issue #699 is implemented. Finish documentation and verification.

TASK:
Write the XML docs, ask docs-writer for the sagas page section, ask mechanical-fixer for PublicAPI lines and the changelog fragment, and
run the build, tests and coverage checks.

KEY RULES:
- The docs say what is cached (the status view), what is not (the handler path, compensations, stuck queries) and the staleness bound.

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
| AGENTS.md section 3 | Provider coherence, errors not swallowed, `ForLogging()`, registration completeness with a DI test |
| AGENTS.md section 5 (database, caching) | The stores #592 adds cover the 10 database providers; the cache side is consistent across cache providers |
| ADR-054 D4, D6 | No tenant tag without a tenant scope; no keys or state in telemetry |
| ADR-062 | Applies only if #592 turns the interface into ROP (no code created here) |
| #592, #593, #1467 | The stores and the registration this plan waits for |
| #712 | Cache Integration epic |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| IChoreographyStateStore, IChoreographyState, ChoreographyStatus | src/Encina.Messaging/Choreography | the decorated abstraction and the state shape |
| ChoreographyOptions | src/Encina.Messaging/Choreography/ChoreographyOptions.cs | the existing options type (`PersistState`); unchanged |
| CachingSagaStatusReader, EvictingSagaStoreDecorator, CachedSagaEntry | src/Encina.Caching/MessagingStores (#696) | the pattern this plan mirrors |
| StoreCacheKey, StoreCachingRegistration, StoreCacheTelemetry, StoreCacheLog, StoreCacheErrors | src/Encina.Caching/MessagingStores (#695) | the shared foundation |
| ICacheProvider, CachingOptions | src/Encina.Caching | the cache and `ThrowOnCacheErrors` |
| Store integration tests of #592 | tests/Encina.IntegrationTests | fixtures and folders for PR 3 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (`CachingStores` from #695) | 350-389 | This plan adds no id; it reuses 350-353 |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 6 | 2 | New: port, default reader, caching reader, evicting decorator, entry, options; modified: the registration of #592/#1467, `StoreCachingServiceCollectionExtensions.cs` |
| Tests | 16 | 1 | New: reader unit, DI (PR 1); reader unit, decorator unit, DI (PR 2); scenario and 10 provider classes (PR 3); modified: `StoreCachingGuardTests.cs` |
| Documentation | 0 | 1 | `docs/messaging/sagas.md` |
| Configuration | 1 | 4 | Changelog fragment; two PublicAPI files, `Encina.Messaging.json`, `Encina.Caching.json` |
| **Total** | **23** | **8** | 3 pull requests: 7, 12 and 12 files (31 file touches), each at most 15 files and about 700 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #699: a cache for choreography state lookups. The
choreography store (#592) and its registration (#1467) must exist first. The handler path is read-modify-write on a mutable state, so
only a read-only status view is cached; writes evict; compensations and stuck queries are never cached.

IMPLEMENTATION OVERVIEW:
1. PR 1: IChoreographyStatusReader and its default (Messaging), registered next to the store.
2. PR 2: CachingChoreographyStatusReader, EvictingChoreographyStateStoreDecorator, CachedChoreographyEntry, ChoreographyCachingOptions,
   AddEncinaChoreographyCaching, tests, manifest, changelog.
3. PR 3: integration scenario over the 10 database providers and the sagas page.

KEY PATTERNS:
- Mirror #696: copy-on-read entries, short TTL for running states, long TTL for Completed and Compensated, null never cached.
- The interface throws (no Either): a cache failure falls through, the strict switch rethrows.
- Keys through StoreCacheKey; shared EventIds 350-353; no key, id or state in logs or tags.
- Apply Design Choice 1 as the Maintainer Decisions section says (and the answer given on #696).

REFERENCE FILES:
- src/Encina.Messaging/Choreography/IChoreographyStateStore.cs, IChoreographyState.cs
- src/Encina.Caching/MessagingStores (#695, #696)
- docs/messaging/sagas.md
- .github/coverage-manifest/Encina.Messaging.json, Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | The feature: a bounded-staleness cache of the choreography status view with eviction on write. |
| 2 | OpenTelemetry | ✅ Include | The shared counters with `store=choreography`; no ids or state in tags; no tenant tag (ADR-054 D4, D6). |
| 3 | Structured Logging | ✅ Include | Shared ids 350-353; exceptions through `ForLogging()`. |
| 4 | Health Checks | ❌ N/A | The cache is optional and falls through; the stores of #592 bring their own checks. |
| 5 | Validation | ✅ Include | Options setters reject non-positive values. |
| 6 | Resilience | ✅ Include | A cache outage never fails a choreography step; `ThrowOnCacheErrors` is the strict switch. |
| 7 | Distributed Locks | ❌ N/A | The handler path is not cached, so there is no shared mutable cache entry to lock. |
| 8 | Transactions | ✅ Include | Nothing is published to the cache before a commit: entries come only from reads; eviction follows writes; the plan claims bounded staleness, not atomicity. |
| 9 | Idempotency | ❌ N/A | Status reads are idempotent; event idempotency is the inbox's concern. |
| 10 | Multi-Tenancy | ✅ Include | The state has no tenant today, so the key has none; `StoreCacheKey` is injective and ready for a tenant column. |
| 11 | Module Isolation | ❌ N/A | Choreographies are not module scoped. |
| 12 | Audit Trail | ❌ N/A | A status read is not an audit event. |

**Deferred items:** none that need an issue. The documentation gap about the unimplemented choreography feature already has issues (#1467, #1471, #1472); the stores are #592 and #593.

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| #592 (persistent stores for the database providers) and #593 (Marten, EventStoreDB) | ⏭️ Open | Blocking for every PR of this plan: there is no store to decorate on `main` |
| #1467 (choreography has no implementation and no DI registration) | ⏭️ Open | The registration method where PR 1 adds the reader default |
| #695 foundation and #696 pattern | ⏭️ Sibling plans | PR 2 needs both merged |
| Maintainer answers to Design Choice 1 (and the answer on #696) | Pending | Asked when work starts |

Blocking prerequisites: #592 and #1467 for all PRs; #695 and #696 for PR 2.

---

## Next Steps

1. The orchestrator keeps #699 out of the work queue until #592 and #1467 are merged; then it asks the maintainer Design Choice 1 together with the answer on #696, records it under `## Maintainer Decisions` and briefs three workers.
2. If the maintainer answers C, comment on #699 with the evidence (no store, mutable read-modify-write state) and close it with #592 as the owner of any later cache.
3. After PR 3, comment on #699 with the integration result.
