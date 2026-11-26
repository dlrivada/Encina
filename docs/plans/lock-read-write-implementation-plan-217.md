# Implementation Plan: Distributed Read/Write Lock (`IDistributedReadWriteLockProvider`, shared readers or one exclusive writer)

> **Issue**: [#217](https://github.com/dlrivada/Encina/issues/217)
> **Type**: Feature
> **Complexity**: High (a new lock primitive with an upgrade operation across seven providers; 8 pull requests, each at most 15 files)
> **Estimated Scope**: ~2,300 lines of production code + ~4,200 lines of tests (about 6,500 lines over 8 pull requests; the per-PR estimates in "Estimated File Count" sum to it)
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027, DEC-009, AC-027; [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md); [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D3, D4, D6.3; [ADR-062](../architecture/adr/062-error-category-contract.md)
> **Depends on**: [#2667](https://github.com/dlrivada/Encina/issues/2667) (implements ADR-052; open, not a pending maintainer decision); PR 2 of [#215](https://github.com/dlrivada/Encina/issues/215) (EventId range) for this plan's PR 2; the four planned provider packages (#207 to #210) for PRs 5 to 8
> **Related**: family `lock-primitives` ([#215](https://github.com/dlrivada/Encina/issues/215) semaphore, [#216](https://github.com/dlrivada/Encina/issues/216) election, [#218](https://github.com/dlrivada/Encina/issues/218) fencing, [#219](https://github.com/dlrivada/Encina/issues/219) multi-lock); lock plans #226, #741, #724, #222, #207, #208, #209, #210

---

## Summary

`IDistributedLockProvider` serializes everyone, readers included. For read-heavy resources (configuration, cache invalidation, CQRS queries against a command-exclusive resource) that wastes throughput. This plan adds an opt-in **distributed read/write lock** to `Encina.DistributedLock`: any number of concurrent readers, or one exclusive writer, plus an **upgradeable read** that can be promoted to write.

1. **Abstraction.** `IDistributedReadWriteLockProvider` and `IReadWriteLockHandle`, written to the ADR-052 shapes: acquired is `Right(Some(handle))`, contention or an elapsed wait is `Right(None)`, a backend fault is `Left`, cancellation propagates. The issue's `IAsyncDisposable` results become `IReadWriteLockHandle` (so `Mode`, `ExpiresAtUtc`, `ExtendAsync` and the upgrade need no cast).
2. **Three modes.** `Read` (shared with other readers and with one upgradeable), `Write` (exclusive), `UpgradeableRead` (shared with readers, exclusive among upgradeables, promotable to write once the other readers are gone).
3. **Native where the engine has it.** PostgreSQL uses shared and exclusive advisory locks; SQL Server uses `sp_getapplock` modes `Shared`, `Update` and `Exclusive`. Everywhere else a per-resource state record updated atomically (Redis Lua, DynamoDB conditional write, Azure Blob `If-Match`, a MySQL table row, InMemory under a lock).
4. **Observability by interface.** One decorator over any provider; no telemetry code in the seven providers.
5. **Opt-in.** Nothing is registered unless `AddEncinaDistributedReadWriteLock*()` of a provider is called.

**Standards**: ADR-052; ADR-054; ADR-062; SPEC-000 REQ-027; PostgreSQL "Explicit Locking, Advisory Locks" (`pg_advisory_lock_shared`); SQL Server `sp_getapplock` lock modes; Medallion `DistributedLock` reader-writer locks and the classic readers-writer lock (named comparable products, not dependencies).

**Estimated scope**: about 20 new production files, 8 modified, about 26 test files, README and changelog fragment; 8 pull requests.

**Affected packages**: `Encina.DistributedLock` (abstractions, decorator, telemetry), `Encina.DistributedLock.InMemory`, `.Redis`, `.SqlServer`; later `.PostgreSQL`, `.MySQL`, `.AzureBlob`, `.DynamoDB`; `Encina` (none: the EventId range is #215's); test projects `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.IntegrationTests`.

**Provider category**: Distributed locks (AGENTS.md section 5): all seven providers; the cloud ones are tested with Azurite and LocalStack. `Encina.DistributedLock.MongoDB` (#609) is not in the seven of SPEC-000 DEC-009 and is not planned here. The database matrix (10) does not apply.

**Shared pieces and ownership (family `lock-primitives`)**: the EventId range `DistributedLockPrimitives` (30550-30564) is registered by #215 (its PR 2); this plan uses 30559-30561. The schema-provisioning option of the SQL providers (`EnsureSemaphoreSchema` in #215 Design Choice 3) has a sibling here for the MySQL state table only (`EnsureReadWriteSchema`, same default and same answer); SQL Server and PostgreSQL need no table. The decorator pattern and `DistributedLockDiagnostics` are those of #724 and #215.

**What the code does today** (`origin/main` at `326e65ebf`):

- Nothing of this feature exists: `src/Encina.DistributedLock/` has `IDistributedLockProvider`, `ILockHandle`, `DistributedLockOptions`, `LockAcquisitionException`, `ServiceCollectionExtensions` and a README; no read/write type exists under `src/`.
- `SqlServerDistributedLockProvider` calls `sp_getapplock` with `@LockMode = 'Exclusive'` and `@LockOwner = 'Session'` (`SqlServerDistributedLockProvider.cs:171-176`), holding one connection per handle; the other modes are unused today. The Redis lock key is `{KeyPrefix}:lock:{resource}`, so a read/write lock uses its own kind segment (`{KeyPrefix}:rwlock:{resource}`) and never collides with a plain lock of the same name.
- The planned provider packages choose session-bound locks with a dedicated connection per handle and a `TimeProvider` timer that enforces expiry (#207 for PostgreSQL, #208 for MySQL); the native read/write modes reuse that handle design.
- Test projects: no read/write lock contract class exists; `Encina.DistributedLock` has no `InternalsVisibleTo` for `Encina.GuardTests` (guard tests here target public types).
- `30550-30599` has no row in `docs/engineering/eventid-block-registry.md` and no claim in any plan on main or open plan branch (checked 2026-11-26).

---

## Design Choices

<details>
<summary><strong>1. Interface shape: a lock object per name (the issue) or the resource passed per call</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IDistributedReadWriteLockProvider.CreateReadWriteLock(name)` returning an `IDistributedReadWriteLock` with the acquire methods, as in the issue** | The issue's API; a named lock object can be passed around | A second object type with no state of its own; `name` validation throws at creation (no I/O) |
| **B) The provider takes the resource per call (`TryAcquireReadAsync(resource, expiry, wait, retry, ct)`, `...WriteAsync`, `...UpgradeableReadAsync`), like `IDistributedLockProvider` and the semaphore of #215** | One shape across the lock primitives; decorators and DI identical | Deviates from the issue's code block |

### Chosen Option: **B - resource per call** (recommended, pending the maintainer)

### Rationale

- B wins on coherence of the family (the same decorator, telemetry and tenant-scoping code works on all primitives); A wins when callers want a reusable named lock object as Medallion has. The issue's `IAsyncDisposable` results are replaced by `IReadWriteLockHandle` under both options.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Mechanism: native modes where the engine has them, or one uniform state record everywhere</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Native on PostgreSQL (`pg_try_advisory_lock_shared` / `pg_try_advisory_lock`) and SQL Server (`sp_getapplock` `Shared` / `Update` / `Exclusive`); a per-resource state record on Redis, DynamoDB, Azure Blob, MySQL and InMemory** | The engine's own lock manager does the work (no table on PG and SQL Server); session loss frees the lock at once; the issue's design | Two families of implementation behind one contract (the contract suite is the proof) |
| **B) One state record for all seven providers** | One algorithm; identical behavior | Needs a table on PostgreSQL and SQL Server (schema provisioning); ignores shared modes the engines already provide |

### Chosen Option: **A - native where available** (recommended, pending the maintainer)

### Rationale

- A wins when the engines' own lock managers are preferred and tables are unwelcome; B wins when one algorithm with identical behavior on all seven providers matters more than avoiding a table.

- MySQL `GET_LOCK` has an exclusive mode only, Azure Blob leases are exclusive only, Redis and DynamoDB have no lock manager, so those need the record under both options; the only choice is whether PostgreSQL and SQL Server use their native modes, and only A avoids a table there. The upgradeable mode: SQL Server's `Update` mode is compatible with `Shared` and exclusive among updaters; PostgreSQL gets the same property from a gate key `{resource}:upgrade` taken exclusively by the one upgradeable holder (two holders that both held a shared lock and both asked for exclusive would deadlock). The SQL Server upgrade (`Update` to `Exclusive` on the same session) is accepted only when the contract scenario `UpgradeWaitsForOtherReaders` passes on SQL Server; if the lock manager does not convert, SQL Server uses the PostgreSQL gate-key design instead (same observable behavior).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Fairness: may a continuous stream of readers starve a writer</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No fairness guarantee in the contract; documented per provider (SQL Server queues waiters so a waiting writer blocks later readers; PostgreSQL, Redis and the state-record providers admit new readers while a writer waits)** | Implementable on every provider; no extra state | A writer can starve under continuous reads on some providers; the writer's `wait` bounds the harm |
| **B) Writer preference on every provider (a "writer waiting" marker blocks new readers)** | No starvation | Native PostgreSQL advisory locks cannot do it without a second gate and polling; more state and failure modes (a crashed waiting writer must not block readers: the marker needs its own expiry) |

### Chosen Option: **A - documented, no guarantee** (recommended, pending the maintainer)

### Rationale

- A wins when writes are rare and bounded by `wait`; B wins when writers must not wait behind a read flood. Acquisition is not re-entrant: a holder that requests the write mode while holding a read handle of its own waits for itself until `wait` elapses (`Right(None)`); upgrade is the supported path.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Upgrade semantics</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Only an `UpgradeableRead` handle can upgrade; `TryUpgradeToWriteLockAsync(wait, retry, ct)` waits until the other readers are gone and returns `Right(true)`, `Right(false)` when `wait` elapsed (the handle stays an upgradeable read), `Left` on a fault; at most one upgradeable holder per resource** | No deadlock between two upgraders (the second `TryAcquireUpgradeableRead` is contention); the handle's `Mode` becomes `Write` after success | The issue's parameterless `TryUpgradeToWriteLockAsync(ct)` gains `wait` and `retry` |
| **B) Any read handle may try to upgrade** | Simplest surface | Two readers upgrading at once deadlock until timeout |

### Chosen Option: **A**

### Rationale

- Only A is free of the upgrade deadlock; the added `wait` and `retry` are needed because upgrading must wait for readers to drain, exactly as `TryAcquireAsync` waits.

**Class:** Settled
**Settled by:** the classic readers-writer upgrade rule (Medallion `DistributedLock` upgradeable read locks and .NET `ReaderWriterLockSlim` upgradeable mode allow a single upgradeable holder for the same reason)

</details>

<details>
<summary><strong>5. Telemetry and error codes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A decorator `InstrumentedDistributedReadWriteLockProvider` over any provider with bounded tags (`encina.lock.mode` = `read` / `write` / `upgradeable`, `encina.lock.outcome`, `encina.lock.system`), no resource names, tenant tag through the #2676 helper when multi-tenancy is on; error codes in `ReadWriteLockErrorCodes` with ADR-062 categories** | All seven providers covered; one place to review | Provider-internal events are not visible |
| **B) Instrument each provider** | Internal events visible | Seven copies and a temptation to tag resource names |

### Chosen Option: **A**

### Rationale

- Codes: `rwlock.invalid_resource` (`Validation`, R3), `rwlock.invalid_expiry` (`Validation`), `rwlock.not_upgradeable` (`Conflict`, R2: the operation does not fit the handle's state; a handle that is not an upgradeable read), `rwlock.backend_failure` (`Internal`, R4). Instruments (no `_total` suffix): counter `encina.distributed_rwlock.acquisitions`, histogram `encina.distributed_rwlock.acquisition.duration` (`s`), up-down counter `encina.distributed_rwlock.held`, counter `encina.distributed_rwlock.upgrades` (tag outcome `upgraded` | `contended` | `failed`).

**Class:** Trivial
**Settled by:** [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D3, D4, D6.3 and `AGENTS.md` section 3 (`EncinaError.Message` never reaches logs)

</details>

---

## Implementation Phases

### Phase 1: Abstractions, Error Codes and the InMemory Provider (PR 1)

> **Goal**: The contract, its reference implementation and the contract suite every provider must pass.

<details>
<summary><strong>Tasks</strong></summary>

1. **`ReadWriteLock/IDistributedReadWriteLockProvider.cs`** (new, `src/Encina.DistributedLock/`) - `TryAcquireReadAsync`, `TryAcquireWriteAsync`, `TryAcquireUpgradeableReadAsync` (`resource, expiry, wait, retry, ct`) returning `Task<Either<EncinaError, Option<IReadWriteLockHandle>>>`, and `AcquireReadAsync`, `AcquireWriteAsync` (`resource, expiry, ct`) returning `Task<Either<EncinaError, IReadWriteLockHandle>>`.
2. **`ReadWriteLock/IReadWriteLockHandle.cs`** (new) - `IAsyncDisposable` with `Resource`, `Mode`, `LockId`, `AcquiredAtUtc`, `ExpiresAtUtc`, `IsReleased`, `ExtendAsync(TimeSpan, CancellationToken = default)` (same return shape as `ILockHandle.ExtendAsync` after #2667) and `TryUpgradeToWriteLockAsync(TimeSpan wait, TimeSpan retry, CancellationToken)` returning `Task<Either<EncinaError, bool>>`; `ReadWriteLockMode` enum (`Read`, `Write`, `UpgradeableRead`).
3. **`ReadWriteLock/ReadWriteLockErrorCodes.cs`** (new, public static) - the four codes of Design Choice 5 (a separate class so this plan does not edit `LockErrorCodes`; working names re-aligned with the naming #2667 uses).
4. **`Encina.DistributedLock.InMemory/InMemoryDistributedReadWriteLockProvider.cs`** (new, public sealed; constructor `(IOptions<InMemoryLockOptions>, TimeProvider? timeProvider = null)`) - per resource a state record `{ writer, readers, upgradeable }` with expiries under a per-entry lock; expired holders are pruned on each call; wait and retry with `Task.Delay(..., _timeProvider, ct)`.
5. **`Encina.DistributedLock.InMemory/ServiceCollectionExtensions.cs`** (modify) - `AddEncinaDistributedReadWriteLockInMemory()` with `TryAddSingleton`.
6. **Tests** - unit (readers share, writer excludes readers and writers, upgradeable coexists with readers and excludes a second upgradeable, upgrade waits for readers and fails on `wait`, expiry with `FakeTimeProvider`, extend, release, cancellation, DI with `ValidateOnBuild` and `ValidateScopes`); guard (null options, null or whitespace resource is a `Left`, non-positive expiry is a `Left`); contract (`tests/Encina.ContractTests/DistributedLock/DistributedReadWriteLockContractTests.cs`: abstract base, InMemory subclass); PublicAPI and manifests via `mechanical-fixer`; README section via `docs-writer`; changelog fragment `changelog.d/217-distributed-read-write-lock.added.md`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #217: the distributed read/write lock abstraction and its InMemory provider.

CONTEXT:
- Issue #2667 has changed IDistributedLockProvider to the ADR-052 shapes; mirror them. Contention is Right(None).
- Modes: Read (shared), Write (exclusive), UpgradeableRead (shared with readers, exclusive among upgradeables).
- Time comes from TimeProvider; no DateTime.UtcNow.

TASK:
Create the interfaces, ReadWriteLockMode, ReadWriteLockErrorCodes and InMemoryDistributedReadWriteLockProvider, the
registration and the unit, guard and contract tests of the Phase 1 tasks. The contract tests are an abstract base.

KEY RULES:
- Invalid input returns Left (Validation), never throws; a handle that is not an upgradeable read returns Left (Conflict).
- An expired holder never blocks anyone; pruning happens on every call.
- Never put the resource name in an error message, log argument or exception message.

REFERENCE FILES:
- src/Encina.DistributedLock/IDistributedLockProvider.cs, ILockHandle.cs
- src/Encina.DistributedLock.InMemory/InMemoryDistributedLockProvider.cs
- docs/architecture/adr/052-distributed-lock-failure-contract.md
```

</details>

### Phase 2: Observability (PR 2)

> **Goal**: Spans, metrics and logs for every read/write lock provider through one decorator.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Diagnostics/ReadWriteLockLog.cs`** (new, `internal static partial`) - 30559 contended (Debug: mode, system, wait ms), 30560 backend failure (Error: system, error code only), 30561 upgrade denied or failed (Information: outcome, wait ms). Packed from 30559; the range `DistributedLockPrimitives` is #215's.
2. **`Diagnostics/InstrumentedDistributedReadWriteLockProvider.cs`** and **`InstrumentedReadWriteLockHandle.cs`** (new, internal sealed; the wrapper keeps `IReadWriteLockHandle` and forwards `TryUpgradeToWriteLockAsync`, recording the upgrade outcome and updating `Mode` through the inner handle) - the instruments of Design Choice 5; `encina.lock.system` from the type-name table of #724, extended.
3. **`ServiceCollectionExtensions.cs`** (modify, core) - `AddEncinaDistributedReadWriteLockTelemetry()` decorates the registered provider; `InvalidOperationException` when none is registered.
4. **Tests** - unit (`Right(None)` is `contended`, never `failed`; the mode tag; no resource name in any tag, log or span), guard, DI test; load and benchmark justification `.md` files; PublicAPI and manifest rows.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #217: telemetry for the distributed read/write lock.

CONTEXT:
- EventIds 30559-30561 are inside the DistributedLockPrimitives range registered by #215; do not register a range here.
- ADR-054: Meter "Encina.DistributedLock", no _total suffix, tenant tag only when multi-tenancy is on, no resource names.

TASK:
Write ReadWriteLockLog, the instrumented provider and handle and AddEncinaDistributedReadWriteLockTelemetry with tests.

KEY RULES:
- Only the error code or exception type is logged; never EncinaError.Message or the resource name.
- The handle wrapper stays an IReadWriteLockHandle and forwards every member.

REFERENCE FILES:
- docs/plans/distributed-lock-opentelemetry-implementation-plan-724.md
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 3: Redis (PR 3) and SQL Server (PR 4)

> **Goal**: The two existing production providers.

<details>
<summary><strong>Tasks</strong></summary>

1. **PR 3 - `RedisDistributedReadWriteLockProvider.cs`** (new) - key `{KeyPrefix}:rwlock:{resource}` as a hash plus a sorted set of reader tickets scored by expiry; Lua scripts: acquire read (prune expired readers and writer, fail when a live writer exists), acquire write (fail when a live writer or any live reader or an upgradeable exists), acquire upgradeable (fail when a writer or another upgradeable exists), upgrade (succeed when the only live reader is the caller's ticket), release and extend ownership-checked; time from `redis.call('TIME')`; `RedisException` becomes `Left`. Registration `AddEncinaDistributedReadWriteLockRedis()`; unit, guard, contract subclass, integration.
2. **PR 4 - `SqlServerDistributedReadWriteLockProvider.cs`** (new) - one open connection per handle; `sp_getapplock` with `@LockMode` `Shared`, `Update` or `Exclusive`, `@LockOwner = 'Session'`, `@LockTimeout = 0` polled every `retry`; release by `sp_releaseapplock` and connection close; expiry enforced by a `TimeProvider` timer as in the plans of #207 and #208; upgrade by requesting `Exclusive` on the same session (acceptance: the contract scenario `UpgradeWaitsForOtherReaders` passes; otherwise the gate-key design of PostgreSQL, Design Choice 2). Registration, unit, guard, contract subclass, integration on the shared SQL Server fixture. No table, no schema option.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing one pull request of Phase 3 of issue #217: a read/write lock provider for Redis or SQL Server.

CONTEXT:
- DistributedReadWriteLockContractTests (Phase 1) is an abstract base; add one subclass for your provider.
- Database calls are asynchronous with a CancellationToken; never Open().
- ADR-052 outcomes: contention is Right(None); a backend fault is Left; cancellation propagates.

TASK:
Implement the provider named in your brief with the mechanism of the Phase 3 tasks, the registration and the tests, and
register the provider type in the telemetry type-name table.

KEY RULES:
- No resource name in errors, logs or tags.
- Stop and report if the contract cannot be met on the engine.

REFERENCE FILES:
- src/Encina.DistributedLock.Redis/RedisDistributedLockProvider.cs
- src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs
- tests/Encina.ContractTests/DistributedLock/DistributedReadWriteLockContractTests.cs
```

</details>

### Phase 4: PostgreSQL, MySQL, Azure Blob and DynamoDB (PRs 5 to 8)

> **Goal**: The four planned providers, one pull request each, after their package merged.

<details>
<summary><strong>Tasks</strong></summary>

1. **PR 5 - PostgreSQL** (after #207): `pg_try_advisory_lock_shared` for read, `pg_try_advisory_lock` for write, and the gate key `{resource}:upgrade` (exclusive) for the upgradeable mode; the upgrade acquires the exclusive lock on the same session; the dedicated-connection handle of #207.
2. **PR 6 - MySQL** (after #208): a state table row per resource (`EncinaDistributedReadWriteLock`: resource hash, writer ticket and expiry, upgradeable ticket and expiry, reader tickets in a child table or a JSON column), serialized with `GET_LOCK` around a short transaction; opt-in `EnsureReadWriteSchema` (default `false`, #215 Design Choice 3); `UTC_TIMESTAMP(6)` for expiry.
3. **PR 7 - Azure Blob** (after #209): the state record as a small JSON blob body updated with `If-Match` on its ETag (retry on `412`, bounded), expiry evaluated by the client `TimeProvider`; Azurite tests.
4. **PR 8 - DynamoDB** (after #210): one item per resource with a version attribute and conditional `UpdateItem` (`ConditionalCheckFailedException` retried, bounded); LocalStack tests.
5. Each PR adds the contract subclass, PublicAPI and manifest lines, the registration, the package README line and the telemetry type-name row.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing one pull request of Phase 4 of issue #217: a read/write lock provider for a lock package that exists.

CONTEXT:
- AGENTS.md section 5: a lock feature is complete for 1.0 when all seven providers cover it; the cloud providers are tested
  with Azurite and LocalStack. The contract suite is shared; add one subclass.

TASK:
Implement the provider named in your brief with the mechanism of the Phase 4 tasks and make the contract suite pass on the
provider's own fixture.

KEY RULES:
- ADR-052 outcomes; no resource name in errors, logs or tags; the optimistic-retry loops are bounded and return Right(None)
  or Left, never spin.
- Do not change the abstraction; report if the contract cannot be met.

REFERENCE FILES:
- docs/plans/lock-postgresql-implementation-plan-207.md (or -208, -209, -210)
- tests/Encina.ContractTests/DistributedLock/DistributedReadWriteLockContractTests.cs
```

</details>

### Phase 5: Documentation and Finalization

> **Goal**: Users can find and use the read/write lock.

<details>
<summary><strong>Tasks</strong></summary>

1. **README** of `Encina.DistributedLock` (via `docs-writer`, in PR 1): usage with `await using`, the three modes, upgrade, the per-provider fairness table (Design Choice 3), non-reentrancy.
2. **Changelog fragment** `changelog.d/217-distributed-read-write-lock.added.md` (via `mechanical-fixer`, in PR 1).
3. **XML documentation** on every public member, with an example on `IDistributedReadWriteLockProvider`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are finishing issue #217: documentation of the distributed read/write lock.

CONTEXT:
- Seven providers implement IDistributedReadWriteLockProvider (the planned four as their PRs merge).

TASK:
Write the README section and the changelog fragment through the specialists; verify the XML docs.

KEY RULES:
- Documentation never types coverage figures by hand; English only.

REFERENCE FILES:
- src/Encina.DistributedLock/README.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Standard | Relevance |
|----------|-----------|
| SPEC-000 REQ-027, DEC-009, AC-027 | Seven providers, contract tests over all |
| ADR-052 | `Either` outcomes |
| ADR-054 D3, D4, D6.3; ADR-062 | Telemetry conventions, error categories |
| PostgreSQL advisory locks; SQL Server `sp_getapplock` | Native shared and exclusive modes |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `IDistributedLockProvider`, `ILockHandle` | `src/Encina.DistributedLock/` | Shape to mirror |
| `SqlServerDistributedLockProvider` (`sp_getapplock`, one connection per handle) | `src/Encina.DistributedLock.SqlServer/` | Base of the native SQL Server modes |
| `RedisDistributedLockProvider` Lua scripts | `src/Encina.DistributedLock.Redis/` | Ownership-checked script pattern |
| `EncinaErrors.Create(code, category, message)` | `src/Encina/` | `Left` values |
| Lock health checks | provider `Health/` folders | Probe the connection the read/write lock uses (no new health check) |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Provider | Mechanism | Status of the package | Lands in |
|----------|-----------|-----------------------|----------|
| InMemory | state record under a lock, `TimeProvider` | exists | PR 1 |
| Redis | hash plus sorted set, Lua | exists | PR 3 |
| SQL Server | `sp_getapplock` `Shared` / `Update` / `Exclusive`, session | exists | PR 4 |
| PostgreSQL | `pg_try_advisory_lock_shared` / `pg_try_advisory_lock` plus gate key | planned (#207) | PR 5 |
| MySQL | state table, `GET_LOCK` around a short transaction | planned (#208) | PR 6 |
| Azure Blob | JSON state blob, `If-Match` | planned (#209) | PR 7 |
| DynamoDB | one item, conditional update | planned (#210) | PR 8 |

### Event ID Allocation

| Package | Range | Ids |
|---------|-------|-----|
| `Encina.DistributedLock` (`DistributedLockPrimitives`, registered by #215) | `30550-30564` | #217 uses 30559-30561 (contended, backend failure, upgrade denied); #215 30550-30552, #216 30553-30558, #219 30562-30564; no id below 10000 |

### Estimated File Count

| PR | Content | New | Modified | Tests, manifests, PublicAPI, docs | Total | Lines (est.) |
|----|---------|-----|----------|------------------------------------|-------|--------------|
| 1 | Abstractions, codes, InMemory, README, fragment | 4 (2 interfaces, the enum sits in the handle file, codes, provider) | 1 (InMemory registration) | 9 (unit, guard, contract, 2 PublicAPI, 2 manifests, README, fragment) | 14 | 1,050 |
| 2 | Decorator, log | 5 (log, provider wrapper, handle wrapper, 2 justification `.md`) | 3 (core registration, PublicAPI, manifest) | 3 (unit, guard, DI test) | 11 | 800 |
| 3 | Redis | 1 | 2 (registration, PublicAPI) | 5 (unit, guard, contract subclass, integration, manifest) | 8 | 800 |
| 4 | SQL Server | 1 | 2 | 5 | 8 | 800 |
| 5 | PostgreSQL | 1 | 2 | 5 | 8 | 700 |
| 6 | MySQL | 2 (provider, DDL script) | 3 | 5 | 10 | 800 |
| 7 | Azure Blob | 1 | 2 | 5 | 8 | 800 |
| 8 | DynamoDB | 1 | 2 | 5 | 8 | 750 |

Total about 6,500 lines. Every PR is at most 15 files and 1,500 lines and builds and passes alone: PR 1 adds the interfaces with one implementation, PRs 3 to 8 only add implementers.

### Per-file coverage targets (numeric, per flag)

| File | Unit | Guard | Contract | Integration | Justification |
|------|------|-------|----------|-------------|---------------|
| `InMemoryDistributedReadWriteLockProvider.cs` | 95 | 25 | 80 | 0 | In-process logic fully reachable with `FakeTimeProvider`; guards cover constructor arguments; the contract suite covers the public behavior |
| `InstrumentedDistributedReadWriteLockProvider.cs`, `InstrumentedReadWriteLockHandle.cs` | 90 | 25 | 0 | 0 | Decorator over a mockable inner provider |
| `RedisDistributedReadWriteLockProvider.cs` | 70 | 25 | 70 | 75 | Script argument and result handling unit-testable with a mocked `IDatabase`; script atomicity only on a real Redis |
| `SqlServerDistributedReadWriteLockProvider.cs` | 65 | 25 | 70 | 75 | Result-code classification unit-testable; lock modes and conversion need a real server |
| `ReadWriteLockErrorCodes.cs`, `IDistributedReadWriteLockProvider.cs`, `IReadWriteLockHandle.cs` | 0 | 0 | 0 | 0 | Constants and interfaces: no executable code; covered through the providers |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is pre-1.0 (.NET 10, C# 14, nullable enabled). Rules are in AGENTS.md. Issue #217 adds a distributed read/write
lock (shared readers, exclusive writer, upgradeable read) to Encina.DistributedLock and to every lock provider of
SPEC-000 DEC-009.

IMPLEMENTATION OVERVIEW:
1. Phase 1: IDistributedReadWriteLockProvider, IReadWriteLockHandle, ReadWriteLockMode, ReadWriteLockErrorCodes, InMemory
   provider, abstract contract suite.
2. Phase 2: instrumented decorator, ReadWriteLockLog (30559-30561, inside #215's range).
3. Phase 3: Redis (Lua state record), SQL Server (sp_getapplock Shared/Update/Exclusive).
4. Phase 4: PostgreSQL, MySQL, Azure Blob, DynamoDB after their packages (#207-#210).
5. Phase 5: README section and changelog fragment through the specialists.

KEY PATTERNS:
- ADR-052 outcomes; contention is Right(None); errors carry a code and an ErrorCategory (ADR-062).
- At most one upgradeable holder per resource; upgrade waits for other readers.
- TimeProvider for time; asynchronous database calls with CancellationToken; no resource names in logs, tags or errors.
- Dilemmas 1, 2 and 3 are pending the maintainer: implement the recommended options only after he answers.

REFERENCE FILES:
- docs/plans/lock-read-write-implementation-plan-217.md, src/Encina.DistributedLock/
- docs/architecture/adr/052-distributed-lock-failure-contract.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Lock state must be real-time; the issue's cache scenarios are consumers of the lock, not part of it |
| 2 | OpenTelemetry | ✅ Include | Phase 2: acquisitions, duration, held, upgrades; mode tag bounded; no resource names; tenant tag only when multi-tenancy is on |
| 3 | Structured Logging | ✅ Include | Phase 2: `ReadWriteLockLog` 30559-30561, error code only |
| 4 | Health Checks | ❌ N/A | The existing lock health checks probe the same connection or backend; the issue deferred it and nothing new is checkable |
| 5 | Validation | ✅ Include | Resource and expiry validated as `Left` with category `Validation` (Phase 1) |
| 6 | Resilience | ❌ N/A | Auto-release by expiry and the caller's `wait`/`retry` budget; a retry policy around acquire would double the wait |
| 7 | Distributed Locks | ✅ Include | This is a lock primitive on all seven providers |
| 8 | Transactions | ❌ N/A | Locks are independent of database transactions; the MySQL state transaction is internal |
| 9 | Idempotency | ❌ N/A | Releasing twice is a no-op (`IsReleased`) |
| 10 | Multi-Tenancy | ⏭️ Defer | Tenant scoping of lock keys is #741 for `IDistributedLockProvider` only; the same derivation for the primitives is one follow-up, see `artifacts/issues/lock-primitives-tenant-scoping.md` |
| 11 | Module Isolation | ❌ N/A | Modules are separated with `KeyPrefix` (the position of the #741 plan) |
| 12 | Audit Trail | ❌ N/A | Infrastructure coordination with no personal or regulated data |

---

## Prerequisites & Dependencies

| Item | State | Needed for |
|------|-------|------------|
| #2667 ADR-052 implementation | open | All PRs |
| #215 PR 2 (EventId range) | planned | PR 2 |
| #724 / #741 `DistributedLockDiagnostics`, #2676 tenant tag helper | plans on main / open | PR 2 |
| #207, #208, #209, #210 | plans on main, packages not written | PRs 5 to 8 |

---

## Next Steps

1. After #2667 merges, open PR 1; PR 2 after #215 PR 2; PRs 3 and 4 in parallel; PRs 5 to 8 as the provider packages merge.
2. The maintainer answers Dilemmas 1, 2 and 3 when work on the issue starts; PR 1 is written for the recommended answers.
3. Open the tenant-scoping follow-up issue once for the whole family.
