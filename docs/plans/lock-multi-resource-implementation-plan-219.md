# Implementation Plan: Multi-Resource Distributed Lock (`IDistributedMultiLockProvider`, all-or-nothing acquisition in a fixed order)

> **Issue**: [#219](https://github.com/dlrivada/Encina/issues/219)
> **Type**: Feature
> **Complexity**: Medium (one composing provider over any lock provider, 3 pull requests; the care is in rollback, time budget and expiry)
> **Estimated Scope**: ~700 lines of production code + ~1,350 lines of tests (about 2,050 lines over 3 pull requests; the per-PR estimates in "Estimated File Count" sum to it)
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027, DEC-009, AC-027; [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md); [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D3, D4, D6.3; [ADR-062](../architecture/adr/062-error-category-contract.md)
> **Depends on**: [#2667](https://github.com/dlrivada/Encina/issues/2667) (implements ADR-052; open, not a pending maintainer decision); PR 2 of [#215](https://github.com/dlrivada/Encina/issues/215) (EventId range) for this plan's PR 2
> **Related**: family `lock-primitives` ([#215](https://github.com/dlrivada/Encina/issues/215), [#216](https://github.com/dlrivada/Encina/issues/216), [#217](https://github.com/dlrivada/Encina/issues/217), [#218](https://github.com/dlrivada/Encina/issues/218)); #724 (telemetry decorator), #741 (tenant-scoped locks), #222 (metrics), #226 (RedLock)

---

## Summary

Some operations need several locks at once (a transfer locks the source and the destination account). Taking them one by one in caller code invites two defects: a partial hold when the second lock is unavailable, and deadlock when two callers take the same locks in different orders. This plan adds an opt-in **multi-resource lock** to `Encina.DistributedLock` as one class that **composes** `IDistributedLockProvider`, so it works on every lock provider of SPEC-000 DEC-009 with no provider code:

1. **All-or-nothing from the holder's view.** `TryAcquireMultipleAsync` returns `Right(Some(handle))` only when every lock was acquired; when any one is unavailable within the wait it releases the ones already taken (in reverse order) and returns `Right(None)`; a backend fault rolls back too and returns `Left`. It is not linearizable: other callers can observe the partial holds while acquisition is in progress (the issue's word "atomic" means all-or-nothing, not instantaneous).
2. **Deadlock prevention.** The resource names are de-duplicated and sorted with `StringComparer.Ordinal` before the first acquisition, so every caller of this API takes any two locks in the same order. (`OrderBy(r => r)` of the issue's sketch uses the culture-sensitive comparer, which can order the same names differently on machines with different cultures.) Ordering protects callers of this API from each other; a holder that takes further locks outside the call can still deadlock, and the README says so.
3. **One time budget.** `wait` is the budget of the whole call, shared by the sequential acquisitions (each inner call gets the remaining time), not `wait` per resource.
4. **Aligned expiry.** After the last lock is taken the provider extends every handle to the requested `expiry`, so the set lasts `expiry` from `AcquiredAtUtc` of the set instead of the first lock expiring first (Design Choice 4).
5. **Opt-in and a limit.** `AddEncinaDistributedMultiLock(Action<MultiLockOptions>?)` registers it; `MaxResources` (default 10) bounds the set.

**Standards**: ADR-052; ADR-054; ADR-062; SPEC-000 REQ-027; lock ordering as deadlock prevention (the resource-ordering rule in the references of the issue); Microsoft "Best practices for comparing strings" (ordinal comparison for identifiers).

**Estimated scope**: about 7 new production files, 2 modified, about 11 test files, README and changelog fragment; 3 pull requests.

**Affected packages**: `Encina.DistributedLock` only (the provider, options, error codes, log, registration); test projects `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.IntegrationTests`. No provider package changes.

**Provider category**: Distributed locks (AGENTS.md section 5): the multi-lock works on all seven providers by composition (InMemory, Redis, SQL Server now; PostgreSQL, MySQL, Azure Blob, DynamoDB when #207 to #210 land), proven by one contract scenario per provider. Database (10) does not apply.

**Shared pieces and ownership (family `lock-primitives`)**: the EventId range `DistributedLockPrimitives` (30550-30564) is registered by #215 (its PR 2); this plan uses 30562-30564. `DistributedLockDiagnostics` is created by whichever of #724 and #741 lands first. Each inner handle's fencing token (#218) is reachable through `IMultiLockHandle.Handles`.

**What the code does today** (`origin/main` at `326e65ebf`):

- Nothing of this feature exists: no `IDistributedMultiLockProvider`, `IMultiLockHandle` or `MultiLock*` type under `src/`.
- `IDistributedLockProvider.TryAcquireAsync` documents `wait` as the maximum time to wait for the lock to become available and `AcquireAsync` as waiting indefinitely; #2667 (open) changes both to `Either` shapes. `ILockHandle.ExtendAsync` resets the expiry to now plus the extension (`PEXPIRE` in the Redis script; `now.Add(extension)` in InMemory).
- The SQL Server provider holds one open connection per handle (`sp_getapplock`, `Session` owner); the planned PostgreSQL and MySQL providers do the same. A multi-lock of N resources therefore holds N connections on those providers, which is why `MaxResources` defaults to 10.
- No `src` code consumes `IDistributedLockProvider` (ADR-052 context); `Encina.DistributedLock` references `Encina` and `Encina.Messaging`, and has no `InternalsVisibleTo` for `Encina.GuardTests` (guard tests here target public types).
- `30550-30599` has no row in `docs/engineering/eventid-block-registry.md` and no claim in any plan on main or open plan branch (checked 2026-11-26).

---

## Design Choices

<details>
<summary><strong>1. Composition over the lock provider, or a native multi-key operation per provider</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MultiLockProvider` composes `IDistributedLockProvider`: sorted sequential acquisition with rollback, as in the issue's sketch** | One implementation for all seven providers, the tenant (#741) and telemetry (#724) decorators apply to every inner lock; no provider change | Not instantaneous: partial holds are visible while acquiring; N round trips |
| **B) Native multi-key acquisition where the backend has it (a Redis Lua script that sets N keys or none; one SQL batch)** | Truly all-or-nothing on those backends; fewer round trips | Only Redis and SQL have it; Azure Blob and DynamoDB cannot lease or condition several records in one call (DynamoDB `TransactWriteItems` could for items, but not for blob leases); a second code path per provider; bypasses the decorators |

### Chosen Option: **A - composition** (recommended, pending the maintainer)

### Rationale

- A wins when one behavior on all seven providers and the decorator chain matter; B wins for a Redis-only deployment where the partial-hold window of a high-contention set is unacceptable (a separate issue for a Redis fast path, not needed for the contract).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Ordering and de-duplication</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reject null or whitespace names, remove duplicates, sort with `StringComparer.Ordinal`; the handle's `Resources` is that sorted distinct list** | The same order in every process whatever its culture; a duplicate cannot deadlock the caller against itself (the lock is not re-entrant) | A caller that passed a duplicate sees one lock, documented |
| **B) Sort with the default string comparer** (`OrderBy(r => r)`) | Shortest code | Culture-sensitive: two replicas with different cultures can order the same names differently, which re-opens the deadlock |

### Chosen Option: **A**

### Rationale

- Only A guarantees one global order; B fails exactly when replicas run with different cultures. The sort happens on the raw names before any decorator scopes them: a tenant-scoped name maps to `@{tenant}:{name}` as a function of the tenant and the name, so callers that contend on the same underlying locks are ordered by the same raw names.

**Class:** Settled
**Settled by:** Microsoft "Best practices for comparing strings in .NET" (use ordinal comparison for identifiers that must compare identically on every machine) and the issue's own requirement "deadlock prevention via sorting"

</details>

<details>
<summary><strong>3. Time budget of the call</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One deadline for the whole call (`now + wait` read from `TimeProvider` at the start); each inner `TryAcquireAsync` gets the remaining time; zero or negative remaining is contention** | The caller's `wait` is honored; no N times `wait` | A slow first lock leaves less for the rest |
| **B) `wait` per resource** | Simplest | The call can take N times `wait` |

### Chosen Option: **A**

### Rationale

- `IDistributedLockProvider.TryAcquireAsync` defines `wait` as the maximum time the call waits; a composed call keeps that meaning.

**Class:** Trivial
**Settled by:** `src/Encina.DistributedLock/IDistributedLockProvider.cs` (the `wait` parameter documentation: "Maximum time to wait for the lock to become available")

</details>

<details>
<summary><strong>4. Expiry of the set</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each inner lock keeps the expiry it was acquired with, so the first lock expires up to the acquisition time before the last; the handle's `ExpiresAtUtc` is the earliest** | No extra calls | The set lasts less than `expiry` and unevenly; a slow acquisition can leave the first lock expired before the work starts |
| **B) After the last acquisition every handle is extended to `expiry` (N more calls); if an extension answers "not held", the set is rolled back and the call returns `Right(None)`** | The set lasts `expiry` from `AcquiredAtUtc` of the set, all locks aligned; a lock lost during acquisition is detected before the work starts | N extra round trips; a failed alignment looks like contention |

### Chosen Option: **B - aligned expiry** (recommended, pending the maintainer)

### Rationale

- B wins when callers reason "I hold everything for `expiry`"; A wins when acquisition time is negligible against `expiry` and the extra calls matter. `ExtendAllAsync` extends every handle and returns `Right(true)` only when all answered true; a partial result is `Right(false)` plus the log event 30564, and the caller decides to stop and dispose (no automatic rollback of the extended ones).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Rollback, release and cancellation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Rollback and dispose release every handle in reverse acquisition order, each in its own `try/catch`: a failing release is logged by exception type or error code (30563) and counted, never masks the original outcome, and never skips the remaining releases; the lock frees itself at its expiry anyway; on cancellation the rollback runs first, then `OperationCanceledException` propagates** | No lock is left behind because an earlier release failed; the caller gets the real reason | A release failure is visible only in telemetry (`DisposeAsync` cannot return an `Either`) |
| **B) Stop at the first failing release and surface it** | The failure is surfaced | Locks after it stay held until expiry |

### Chosen Option: **A**

### Rationale

- `IAsyncDisposable.DisposeAsync` must not throw and cannot return a value, and the acquire result must report why acquisition failed, not why a cleanup failed; the failure is not swallowed (logged and metered by code or type, never by message).

**Class:** Trivial
**Settled by:** .NET `IAsyncDisposable` guidance (a dispose operation should not throw) and `AGENTS.md` section 3 (errors are logged by error code or `ForLogging()`, never by message)

</details>

<details>
<summary><strong>6. Registration, options and what the telemetry may carry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaDistributedMultiLock(Action<MultiLockOptions>?)`: options with validation on start (`MaxResources` 1 to 1,000, default 10), a singleton `IDistributedMultiLockProvider` that takes the registered (outermost) `IDistributedLockProvider`, `InvalidOperationException` naming the missing call when none is registered; instruments in the provider itself (counter `encina.distributed_lock.multi.acquisitions`, tags `encina.lock.outcome` and `encina.lock.system`; histogram `encina.distributed_lock.multi.resources`; counter `encina.distributed_lock.multi.rollbacks`; span `encina.distributed_lock.multi.acquire`), no resource names anywhere, tenant tag through the #2676 helper when multi-tenancy is on** | Registration completeness; the tenant and telemetry decorators wrap every inner lock because the multi-lock sits on top of them | The call order matters |
| **B) Resolve the lock provider lazily and fail at first use** | Any order | Failure in production traffic |

### Chosen Option: **A**

### Rationale

- Only A fails at startup; the instruments are in the provider because it is one class and the inner decorators already count each lock. An invalid set (empty, a null or whitespace name, more than `MaxResources`, non-positive expiry) is a `Left`: `multilock.invalid_resources`, `multilock.too_many_resources`, `multilock.invalid_expiry`, each ADR-062 `Validation` (R3: field rules). A fault of an inner lock is returned unchanged (its code and category).

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (registration completeness, DI test with `ValidateOnBuild` and `ValidateScopes`) and [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D3, D4, D6.3

</details>

---

## Implementation Phases

### Phase 1: Abstractions, Provider, Options and Registration (PR 1)

> **Goal**: The contract and its implementation with unit and guard tests on the InMemory provider.

<details>
<summary><strong>Tasks</strong></summary>

1. **`MultiLock/IDistributedMultiLockProvider.cs`** (new, `src/Encina.DistributedLock/`) - `TryAcquireMultipleAsync(IReadOnlyCollection<string> resources, TimeSpan expiry, TimeSpan wait, TimeSpan retry, CancellationToken)` returning `Task<Either<EncinaError, Option<IMultiLockHandle>>>`, and `AcquireMultipleAsync(resources, expiry, ct)` returning `Task<Either<EncinaError, IMultiLockHandle>>` (`IReadOnlyCollection` instead of the issue's `IEnumerable` so the set is not enumerated twice).
2. **`MultiLock/IMultiLockHandle.cs`** (new) - `IAsyncDisposable` with `Resources` (sorted distinct), `Handles` (`IReadOnlyList<ILockHandle>` in acquisition order), `AcquiredAtUtc`, `ExpiresAtUtc` (the earliest inner expiry), `IsReleased` and `ExtendAllAsync(TimeSpan extension, CancellationToken = default)` returning `Task<Either<EncinaError, bool>>`; the issue's `AllAcquired` is dropped (a handle exists only when everything was acquired, so it is always true).
3. **`MultiLock/MultiLockOptions.cs`** and `MultiLockOptionsValidator` (new; `MaxResources`) and **`MultiLock/MultiLockErrorCodes.cs`** (new, public static; the three codes of Design Choice 6).
4. **`MultiLock/MultiLockProvider.cs`** (new, public sealed; constructor `(IDistributedLockProvider, IOptions<MultiLockOptions>, TimeProvider? timeProvider = null)`) - validate; sort and de-duplicate (Design Choice 2); one deadline (Design Choice 3); sequential inner `TryAcquireAsync` with the remaining wait; on `Right(None)` or `Left` or an exception roll back in reverse order with `CancellationToken.None`-safe releases (Design Choice 5); align the expiry (Design Choice 4); return the handle. The internal `MultiLockHandle` releases in reverse order on `DisposeAsync`.
5. **`ServiceCollectionExtensions.cs`** (modify, core) - `AddEncinaDistributedMultiLock(...)` per Design Choice 6.
6. **Tests** - unit on the InMemory provider with `FakeTimeProvider`: all acquired; the second of three unavailable rolls back the first (and the third is never tried); backend fault on the third returns `Left` after rollback; sorted order observed through a recording inner provider; two callers with opposite input orders do not deadlock; duplicate names collapse; the shared deadline (three locks each consuming a third of `wait`); aligned expiry; `ExtendAllAsync` all and partial; dispose releases in reverse order and continues after a failing release; cancellation rolls back then propagates; `MaxResources`; DI built with `ValidateOnBuild` and `ValidateScopes`, with and without a lock provider; guard tests (null provider, options; null or empty set and null or whitespace entries are `Left`).
7. **PublicAPI and manifests** (`mechanical-fixer`), **README** section and **changelog fragment** `changelog.d/219-distributed-multi-lock.added.md` (specialists).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #219: the multi-resource distributed lock.

CONTEXT:
- Issue #2667 has changed IDistributedLockProvider to the ADR-052 shapes (Either, Option for contention); read the current
  signatures. Contention is Right(None), a fault is Left, cancellation propagates.
- The multi-lock composes IDistributedLockProvider; it takes the registered (outermost) provider so the tenant and telemetry
  decorators wrap every inner lock.
- Time comes from TimeProvider; no DateTime.UtcNow.

TASK:
Create IDistributedMultiLockProvider, IMultiLockHandle, MultiLockOptions and validator, MultiLockErrorCodes and
MultiLockProvider, the registration and the tests of the Phase 1 tasks.

KEY RULES:
- Sort with StringComparer.Ordinal after removing duplicates; never the default comparer.
- One deadline for the whole call; each inner call gets the remaining time.
- Release in reverse order, each in its own try/catch; never mask the original outcome; never log resource names or
  EncinaError.Message.
- Invalid input returns Left (Validation), never throws; constructor guards throw ArgumentNullException.

REFERENCE FILES:
- src/Encina.DistributedLock/IDistributedLockProvider.cs, ILockHandle.cs
- src/Encina.DistributedLock.InMemory/InMemoryDistributedLockProvider.cs
- docs/architecture/adr/052-distributed-lock-failure-contract.md
```

</details>

### Phase 2: Observability (PR 2)

> **Goal**: Telemetry and logs for the multi-lock, using the family EventId range.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Diagnostics/MultiLockLog.cs`** (new, `internal static partial`) - 30562 acquisition rolled back (Debug: acquired count, requested count, reason `contended` | `failed` | `cancelled` | `expired`), 30563 release failed (Warning: phase `rollback` | `dispose`, exception type or error code), 30564 extend-all partial (Warning: succeeded count, total). Packed from 30562; the range `DistributedLockPrimitives` is #215's.
2. **`Diagnostics/MultiLockMetrics.cs`** (new, internal) - the instruments of Design Choice 6 on the shared `DistributedLockDiagnostics` meter; `MultiLockProvider.cs` (modify) emits them and the span.
3. **Tests** - unit (`Right(None)` is `contended`, never `failed`; rollback and release-failure events; the resources histogram records the count only; no resource name or tenant id in a log argument, a tag or a span attribute), guard for the new internal seams through the public provider; PublicAPI is untouched (internal types); manifest rows; load and benchmark justification `.md` files.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #219: telemetry of the multi-lock.

CONTEXT:
- EventIds 30562-30564 are inside the DistributedLockPrimitives range registered by #215; do not register a range here.
- ADR-054: Meter "Encina.DistributedLock", no _total suffix, tenant tag only when multi-tenancy is on, no resource names.

TASK:
Write MultiLockLog and MultiLockMetrics, emit them from MultiLockProvider, and add the tests.

KEY RULES:
- Log only the error code or exception type (ForLogging()); never EncinaError.Message or a resource name.
- Contention is not a failure: Right(None) counts as outcome contended.

REFERENCE FILES:
- docs/plans/distributed-lock-opentelemetry-implementation-plan-724.md
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 3: Contract and Integration Scenarios (PR 3)

> **Goal**: The multi-lock is proven on every lock provider that exists.

<details>
<summary><strong>Tasks</strong></summary>

1. **`tests/Encina.ContractTests/DistributedLock/MultiLockContractTests.cs`** (new) - an abstract base: two callers with opposite input orders over three resources both complete; a held resource makes the call `Right(None)` and leaves no other resource held; subclasses for InMemory, Redis and SQL Server; the four planned providers add one subclass each in their own PRs.
2. **Integration tests** (Redis and SQL Server through the shared fixtures): a set of five resources acquired and released; a set blocked by one external holder is rolled back; the SQL Server connections are all returned after dispose.
3. **Tenant decorator** - when #741 has merged, one test builds the multi-lock over the tenant-scoped provider and proves two tenants lock the same names without contention (the multi-lock inherits scoping from the inner provider).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #219: contract and integration tests of the multi-lock across lock providers.

CONTEXT:
- MultiLockProvider works on any IDistributedLockProvider; the contract proves it on every provider that exists.

TASK:
Write MultiLockContractTests as an abstract base with InMemory, Redis and SQL Server subclasses and the integration tests.

KEY RULES:
- Integration tests use shared [Collection] fixtures, never a per-class container; use short real expiries on Redis and SQL
  Server (they expire on the server); FakeTimeProvider only for the InMemory provider.
- Contract tests instantiate real implementations.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/DistributedLock/
- tests/Encina.ContractTests/
```

</details>

---

## Research

### Relevant Standards and Specifications

| Standard | Relevance |
|----------|-----------|
| SPEC-000 REQ-027, DEC-009, AC-027 | The seven providers and the contract tests over all of them |
| ADR-052 | `Either` outcomes the multi-lock composes |
| ADR-054 D3, D4, D6.3; ADR-062 | Telemetry conventions; categories of `multilock.*` codes |
| Deadlock prevention by a global resource order | The sort rule |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `IDistributedLockProvider`, `ILockHandle` | `src/Encina.DistributedLock/` | What is composed |
| `InMemoryDistributedLockProvider` | `src/Encina.DistributedLock.InMemory/` | Unit-test engine with `FakeTimeProvider` |
| `EncinaErrors.Create(code, category, message)` | `src/Encina/` | `Left` values |
| #741 tenant decorator, #724 telemetry decorator | plans on main | Wrap every inner lock because the multi-lock takes the outermost provider |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Provider | How it is covered | Status of the package | Proven by |
|----------|-------------------|-----------------------|-----------|
| InMemory | by composition | exists | PR 1 unit, PR 3 contract |
| Redis | by composition | exists | PR 3 contract and integration |
| SQL Server | by composition (one connection per handle) | exists | PR 3 contract and integration |
| PostgreSQL, MySQL, Azure Blob, DynamoDB | by composition | planned (#207 to #210) | one contract subclass in each package's PR |

### Event ID Allocation

| Package | Range | Ids |
|---------|-------|-----|
| `Encina.DistributedLock` (`DistributedLockPrimitives`, registered by #215) | `30550-30564` | #219 uses 30562-30564 (rolled back, release failed, extend-all partial); #215 30550-30552, #216 30553-30558, #217 30559-30561; no id below 10000 |

### Estimated File Count

| PR | Content | New | Modified | Tests, manifests, PublicAPI, docs | Total | Lines (est.) |
|----|---------|-----|----------|------------------------------------|-------|--------------|
| 1 | Abstractions, provider, options, registration, README, fragment | 6 (2 interfaces, options with its validator, codes, provider) | 2 (core `ServiceCollectionExtensions.cs`, core `PublicAPI.Unshipped.txt`) | 6 (unit, guard, DI test, manifest, README, fragment) | 14 | 1,000 |
| 2 | Log, metrics | 2 + 2 justification `.md` | 1 (`MultiLockProvider.cs`) | 3 (unit, guard, manifest) | 8 | 600 |
| 3 | Contract and integration | 1 contract file (base and three subclasses) + 2 integration files | 0 | 0 | 3 | 450 |

Total about 2,050 lines. Every PR is at most 15 files and 1,500 lines and builds and passes alone (PR 2 needs the range of #215 PR 2 merged).

### Per-file coverage targets (numeric, per flag)

| File | Unit | Guard | Contract | Integration | Justification |
|------|------|-------|----------|-------------|---------------|
| `MultiLockProvider.cs` | 95 | 30 | 70 | 30 | Logic over an injected lock provider with a fake clock, every branch reachable on the InMemory provider; guards cover the constructor and the set validation; the contract suite covers rollback on real providers |
| `MultiLockOptions.cs`, `MultiLockOptionsValidator.cs` | 95 | 20 | 0 | 0 | Pure validation |
| `MultiLockMetrics.cs` | 85 | 20 | 0 | 0 | Instruments tested with a `MeterListener` |
| `MultiLockLog.cs`, `MultiLockErrorCodes.cs`, the two interfaces | 0 | 0 | 0 | 0 | Source-generated log methods, constants and interfaces: no executable logic; covered through the provider |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is pre-1.0 (.NET 10, C# 14, nullable enabled). Rules are in AGENTS.md. Issue #219 adds a multi-resource distributed
lock to Encina.DistributedLock, composed over IDistributedLockProvider, working on all seven lock providers.

IMPLEMENTATION OVERVIEW:
1. Phase 1: IDistributedMultiLockProvider, IMultiLockHandle, MultiLockOptions, MultiLockErrorCodes, MultiLockProvider,
   AddEncinaDistributedMultiLock, unit and guard tests, README and fragment through the specialists.
2. Phase 2: MultiLockLog (30562-30564, inside #215's range) and MultiLockMetrics.
3. Phase 3: abstract contract suite and integration tests.

KEY PATTERNS:
- ADR-052 outcomes; contention is Right(None); sort with StringComparer.Ordinal after de-duplication.
- One deadline for the call; rollback in reverse order; release failures logged by code or type, never by message.
- Aligned expiry (extend every handle after the last acquisition) and a MaxResources limit.
- Take the outermost registered IDistributedLockProvider so tenant and telemetry decorators apply.
- Dilemmas 1 and 4 are pending the maintainer: implement the recommended options only after he answers.

REFERENCE FILES:
- docs/plans/lock-multi-resource-implementation-plan-219.md, src/Encina.DistributedLock/
- docs/architecture/adr/052-distributed-lock-failure-contract.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Lock state must not be cached |
| 2 | OpenTelemetry | ✅ Include | Phase 2: acquisitions, resources count, rollbacks and a span; no resource names; tenant tag only when multi-tenancy is on |
| 3 | Structured Logging | ✅ Include | Phase 2: `MultiLockLog` 30562-30564, ordering and partial failures by count and reason, never by name |
| 4 | Health Checks | ❌ N/A | Uses the underlying provider's health checks |
| 5 | Validation | ✅ Include | The set (non-empty, no null or whitespace name, at most `MaxResources`) and the expiry are validated as `Left` with category `Validation` |
| 6 | Resilience | ❌ N/A | Rollback is part of the feature; a retry around a partial acquisition would hold locks while retrying; the caller's `wait` is the budget |
| 7 | Distributed Locks | ✅ Include | This is a lock primitive composed over the provider |
| 8 | Transactions | ❌ N/A | Locks are independent of transactions |
| 9 | Idempotency | ❌ N/A | Acquiring the same set twice by one caller blocks on itself (locks are not re-entrant); the duplicate-name rule only de-duplicates within one call |
| 10 | Multi-Tenancy | ✅ Include | Inherited without code: the multi-lock takes the outermost provider, so the tenant-scoping decorator of #741 scopes every inner lock; a test proves it once #741 has merged. Tenant scoping for the other primitives is the family follow-up, see `artifacts/issues/lock-primitives-tenant-scoping.md` |
| 11 | Module Isolation | ❌ N/A | Modules are separated with `KeyPrefix` (the position of the #741 plan) |
| 12 | Audit Trail | ❌ N/A | Infrastructure coordination with no personal or regulated data |

---

## Prerequisites & Dependencies

| Item | State | Needed for |
|------|-------|------------|
| #2667 ADR-052 implementation | open | All PRs |
| #215 PR 2 (EventId range) | planned | PR 2 and PR 3 |
| #724 / #741 decorators and `DistributedLockDiagnostics`, #2676 tenant tag helper | plans on main / open | PR 2 and the tenant test of PR 3 |
| #207, #208, #209, #210 | plans on main, packages not written | Contract subclasses of the later providers |

---

## Next Steps

1. After #2667 merges open PR 1; PR 2 after #215 PR 2; PR 3 after PR 1.
2. The maintainer answers Dilemmas 1 and 4 when work on the issue starts; PR 1 is written for the recommended answers.
3. Open the tenant-scoping follow-up issue once for the whole family.
