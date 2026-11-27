# Implementation Plan: Distributed Semaphore (`IDistributedSemaphoreProvider`, N concurrent holders of a named resource)

> **Issue**: [#215](https://github.com/dlrivada/Encina/issues/215)
> **Type**: Feature
> **Complexity**: High (a new primitive across seven lock providers; 8 pull requests, each at most 15 files)
> **Estimated Scope**: ~2,500 lines of production code + ~4,400 lines of tests (about 6,900 lines over 8 pull requests; the per-PR estimates in "Estimated File Count" sum to it)
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027, DEC-009 (the seven lock providers); [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md); [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D3, D4, D6.3; [ADR-062](../architecture/adr/062-error-category-contract.md)
> **Depends on**: [#2667](https://github.com/dlrivada/Encina/issues/2667) (implements ADR-052; open, not a pending maintainer decision); the four planned provider packages ([#207](https://github.com/dlrivada/Encina/issues/207), [#208](https://github.com/dlrivada/Encina/issues/208), [#209](https://github.com/dlrivada/Encina/issues/209), [#210](https://github.com/dlrivada/Encina/issues/210)) for PRs 5 to 8
> **Related**: family `lock-primitives` ([#216](https://github.com/dlrivada/Encina/issues/216) election, [#217](https://github.com/dlrivada/Encina/issues/217) read/write lock, [#218](https://github.com/dlrivada/Encina/issues/218) fencing, [#219](https://github.com/dlrivada/Encina/issues/219) multi-lock); lock plans #226, #741, #724, #222, #717, #763, #755

---

## Summary

`IDistributedLockProvider` admits exactly one holder per resource. Rate limiting ("at most 100 concurrent calls to an external service"), connection quotas and parallel-upload limits need **N** holders. This plan adds an opt-in **distributed semaphore** next to the lock, in `Encina.DistributedLock`, implemented by every lock provider of SPEC-000 DEC-009 (InMemory, Redis, SQL Server, PostgreSQL, MySQL, Azure Blob, DynamoDB):

1. **Abstraction.** `IDistributedSemaphoreProvider` and `ISemaphoreHandle`, written to the ADR-052 shapes: acquired is `Right(Some(handle))`, a full semaphore or an elapsed wait is `Right(None)` (contention is not a failure), a backend fault is `Left`, cancellation propagates.
2. **Tickets with a lease.** A holder owns one ticket; every ticket has an expiry, so a crashed holder frees its slot on its own (the issue's "automatic stale ticket cleanup"). The issue's signatures have no `expiry` parameter although the handle has `ExpiresAtUtc` and `ExtendAsync`; this plan adds it (Design Choice 7).
3. **Per-provider mechanism.** Redis: a sorted set updated by one Lua script. SQL Server, PostgreSQL, MySQL: a ticket table updated inside a short transaction serialized per resource. InMemory: a dictionary under a lock, on `TimeProvider`. Azure Blob: one blob lease per slot. DynamoDB: one item per resource, a conditional update.
4. **Observability by interface.** One decorator over any `IDistributedSemaphoreProvider` emits spans, metrics and logs, so the seven providers need no telemetry code (the approach of #724 for the lock).
5. **Opt-in.** Nothing is registered unless `AddEncinaDistributedSemaphore*()` of a provider is called.

**Standards**: ADR-052; ADR-054 (D3 package-named meter, D4 tenant tag, D6.3 no data-bearing names); ADR-062 (error categories); SPEC-000 REQ-027; Redis "sorted set" semaphore recipe and Medallion `DistributedLock` semaphores (named comparable products, not dependencies).

**Estimated scope**: about 22 new production files, 8 modified, about 26 test files, README and changelog fragment; 8 pull requests (see Implementation Phases).

**Affected packages**: `Encina.DistributedLock` (abstractions, decorator, telemetry), `Encina.DistributedLock.InMemory`, `.Redis`, `.SqlServer`; later `.PostgreSQL`, `.MySQL`, `.AzureBlob`, `.DynamoDB` (packages of #207 to #210); `Encina` (EventId range registration only); test projects `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.IntegrationTests`.

**Provider category**: Distributed locks (AGENTS.md section 5): all seven providers; the cloud ones are tested with Azurite and LocalStack. Database (10) does not apply (the SQL ticket table belongs to the lock packages, not to the ADO/Dapper/EF matrix). `Encina.DistributedLock.MongoDB` ([#609](https://github.com/dlrivada/Encina/issues/609)) is not in the seven of SPEC-000 DEC-009 and is not planned here.

**Shared pieces and ownership (family `lock-primitives`)**: this plan owns the EventId range `DistributedLockPrimitives` (30550-30564) and its registration (field, `PublicAPI.Unshipped.txt` line, `AssemblyRanges` entry, registry row), in its PR 2; #216, #217 and #219 add only their ids afterwards, so their log-bearing pull requests start after PR 2 of this plan merges. #218 allocates no id. The schema-provisioning policy of the SQL providers (Design Choice 3) is decided here and followed by #217 and #218. The shared `DistributedLockDiagnostics` holder is created by whichever of #724 and #741 lands first; this plan adds instruments to it.

**What the code does today** (`origin/main` at `326e65ebf`):

- Nothing of this feature exists: `src/Encina.DistributedLock/` has `IDistributedLockProvider`, `ILockHandle`, `DistributedLockOptions`, `LockAcquisitionException`, `ServiceCollectionExtensions` and a README; no semaphore type exists anywhere under `src/`.
- `IDistributedLockProvider` still has the pre-ADR-052 shape (`Task<IAsyncDisposable?> TryAcquireAsync(resource, expiry, wait, retry, ct)`); #2667 (open) changes it. `ILockHandle` has `Resource`, `LockId`, `AcquiredAtUtc`, `ExpiresAtUtc`, `IsReleased` and `ExtendAsync(TimeSpan, CancellationToken)`.
- Providers key their state by a prefix plus a kind: Redis uses `{KeyPrefix}:lock:{resource}` (`RedisDistributedLockProvider.GetLockKey`), so a semaphore uses its own kind segment (`{KeyPrefix}:semaphore:{resource}`) and can never collide with a lock of the same name. Lock expiry extension is a reset to now plus the extension (`PEXPIRE` in the Redis `ExtendLockScript`; `now.Add(extension)` in InMemory), not an addition.
- The three existing provider packages have no `InternalsVisibleTo` entry for the test projects; `Encina.DistributedLock` has one for `Encina.UnitTests`, `Encina.IntegrationTests`, `Encina.ContractTests` and `Encina.PropertyTests` but **not for `Encina.GuardTests`**, so guard tests in this plan target public types only.
- The package has no EventId range (the #741 plan claims `6250-6299`, the #222 plan `30450-30499`); `30550-30599` has no row in `docs/engineering/eventid-block-registry.md` and no claim in any plan on main or open plan branch (checked 2026-11-26).
- Coverage manifests: `.github/coverage-manifest/Encina.DistributedLock.json` (package targets guard 20, unit 60) and the three provider manifests.

---

## Design Choices

<details>
<summary><strong>1. Interface shape: one provider interface or the issue's three</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `IDistributedSemaphoreProvider` (resource passed per call) and `ISemaphoreHandle`** | Same shape and DI pattern as `IDistributedLockProvider`; one registration; the issue's method list is kept | The acceptance criteria name `IDistributedSemaphore` as well; that name is dropped |
| **B) `IDistributedSemaphore` and `IDistributedSemaphoreProvider` as the issue lists them** | Matches the acceptance criteria literally | The issue's `IDistributedSemaphore` already takes the resource per call, so a second `IDistributedSemaphoreProvider` would either duplicate it or become a factory with no state |

### Chosen Option: **A - one provider interface** (recommended, pending the maintainer)

### Rationale

- A wins when the interface should mirror the lock provider (the DI and decorator story of #724 and #741 applies unchanged); B wins when the maintainer wants a per-semaphore object (`Create(name, maxCount)`) as Medallion has, which is a different design the issue's code block does not show.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Relational mechanism: a ticket table or one native lock per slot</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A ticket table (`Resource`, `TicketId`, `ExpiresAtUtc`) updated in a short transaction serialized per resource** (SQL Server `sp_getapplock` with owner `Transaction`; PostgreSQL `pg_advisory_xact_lock`; MySQL `GET_LOCK` inside the transaction) | One round trip per acquire whatever `maxCount` is; the issue's design; tickets survive a connection loss until their expiry | Needs a table (schema provisioning, Design Choice 3); expired tickets are removed by the acquire path |
| **B) `maxCount` native session locks named `{resource}:{slot}`, try each slot with a zero wait** | No table; a crashed session frees its slot at once | Up to `maxCount` round trips per acquire (a "100 concurrent calls" semaphore probes up to 100 locks); one open connection per live handle; the count query needs a second mechanism |

### Chosen Option: **A - a ticket table** (recommended, pending the maintainer)

### Rationale

- A wins for large `maxCount` or high acquire rates and when the handle must not pin a database connection; B wins for a small fixed number of slots on a database where the application may not create tables.
- Redis, DynamoDB and InMemory have a single natural mechanism each (Design Choice 4); Azure Blob uses B by nature (one lease per slot blob) because a blob lease is the only atomic primitive it offers.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Schema provisioning for the ticket table (SQL Server, PostgreSQL, MySQL)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The package ships the DDL script; an opt-in `EnsureSemaphoreSchema` option (default `false`) creates it at first use; a missing table is a `Left` with a backend-fault code naming no data** | The application owner controls DDL (least privilege); nothing is created implicitly; a missing table fails visibly | One extra setup step |
| **B) Create the table automatically at first use (default `true`, as `EfCoreMigrationOptions.AutoCreateHistoryTable`)** | Works out of the box | Requires DDL rights at runtime; implicit schema changes in production |

### Chosen Option: **A - script plus opt-in creation** (recommended, pending the maintainer)

### Rationale

- A wins when production database users have no DDL rights; B wins for development and demo setups. The #218 plan (fencing sequence) and the #217 plan (read/write state) follow this answer, so the three share one option name and one script file per database.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Mechanism per non-relational provider</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Redis: sorted set `{KeyPrefix}:semaphore:{resource}` (member = ticket id, score = expiry in epoch milliseconds) updated by one Lua script; DynamoDB: one item per resource with a map of tickets and a version attribute, conditional update; InMemory: dictionary under a lock; Azure Blob: one blob lease per slot** | Each uses the strongest atomic primitive of its backend; the Lua script and the conditional update are single atomic steps | Four implementations to keep consistent (the contract tests, Phase 5, are the proof) |
| **B) Build every provider on `IDistributedLockProvider` (a mutex protects a counter stored through the lock provider)** | One implementation | No provider stores a value, so a second store is needed; a mutex per acquire serializes all holders |

### Chosen Option: **A - the native primitive of each backend**

### Rationale

- The Lua script prunes expired members (`ZREMRANGEBYSCORE`), counts (`ZCARD`), adds (`ZADD`) and refreshes the key expiry (`PEXPIRE`) in one atomic step, and reads the time with `redis.call('TIME')` (Design Choice 6). A lease on a slot blob is atomic by Azure's contract.

**Class:** Trivial
**Settled by:** `AGENTS.md` section 5 (provider coherence: the abstraction is shared, implementations differ per provider) and the mechanisms the existing lock providers already use (Lua scripts in `RedisDistributedLockProvider`, a dictionary under a lock in `InMemoryDistributedLockProvider`)

</details>

<details>
<summary><strong>5. Capacity per call or per resource</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `maxCount` is a per-call argument, as in the issue; the check is "current tickets < the caller's `maxCount`"** | The issue's API; no registry of capacities | Two callers that pass different values for one resource see different limits; documented |
| **B) A configured capacity per resource name (`SemaphoreOptions.Limits[resource]`), the call takes none** | One authoritative number; callers cannot disagree | A new options surface and a name-to-limit table every replica must configure alike |

### Chosen Option: **A - per-call `maxCount`** (recommended, pending the maintainer)

### Rationale

- A wins when limits are chosen by the calling code (rate limits that differ per use); B wins when a platform team owns the limits. `maxCount < 1` is a `Left` with a validation code (ADR-062 R3: field rules are `Validation`).

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Which clock decides ticket expiry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The backend's clock** (Redis `TIME`, SQL Server `SYSUTCDATETIME()`, PostgreSQL `clock_timestamp()`, MySQL `UTC_TIMESTAMP(6)`) | Replicas with skewed clocks agree on expiry; no ticket expires early on a fast replica | Expiry tests need a real short TTL against a container, not a fake clock |
| **B) The client `TimeProvider`, passed as a parameter** | Fully testable with `FakeTimeProvider`; the rule of AGENTS.md section 3 applied everywhere | A replica whose clock runs fast evicts live tickets of others |

### Chosen Option: **A for the stores that have a server clock (Redis, SQL Server, PostgreSQL, MySQL); B where the store has none (InMemory, DynamoDB, Azure Blob)** (recommended, pending the maintainer)

### Rationale

- A wins when replicas are not clock-synchronized; B wins when the clock discipline is guaranteed and deterministic tests matter more. The client `TimeProvider` always drives the client-side `wait` and `retry` loop and `ExpiresAtUtc` of the handle.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Signatures that differ from the issue</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add `TimeSpan expiry` to both acquire methods; drop `ISemaphoreHandle.CurrentCount` (a stale snapshot; `GetCurrentCountAsync` is the live answer); return `Either` per ADR-052** | The handle's `ExpiresAtUtc` and `ExtendAsync` have a meaning; no stale property | Deviates from the issue's listing |
| **B) Keep the issue's signatures exactly** | Literal | No ticket TTL (a crash would hold a slot forever, contradicting "automatic stale ticket cleanup"); a property that is wrong a millisecond after the call |

### Chosen Option: **A**

### Rationale

- The issue itself requires auto-release and `ExpiresAtUtc`; both need an expiry input. `GetAvailableCountAsync` and `GetCurrentCountAsync` return `Either<EncinaError, int>` (a fault is `Left`, never `0`).

**Class:** Trivial
**Settled by:** [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md) (the contract of every lock primitive) and issue #215 acceptance ("automatic stale ticket cleanup", `ExpiresAtUtc`, `ExtendAsync`)

</details>

<details>
<summary><strong>8. Telemetry and logging: where they live and what they may carry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One decorator `InstrumentedDistributedSemaphoreProvider` and handle wrapper in `Encina.DistributedLock/Diagnostics/`, opt-in through `AddEncinaDistributedSemaphoreTelemetry()`; instruments on `Meter("Encina.DistributedLock")`; the resource name is never a tag, log argument or span attribute** | All seven providers covered with no per-provider code; same pattern as #724 | The decorator only sees outcomes, not provider-internal events such as reclaimed tickets |
| **B) Instrument inside each provider** | Provider-internal events visible | Seven copies; resource names would tempt tags |

### Chosen Option: **A - the decorator**

### Rationale

- ADR-054 D6.3 forbids data-bearing names and resource names are application text (`reports:{customerId}`); D3 fixes the package-named meter and `Encina`-style dotted names with no `_total` suffix; D4 adds the tenant id as a tag only when multi-tenancy is on, through the helper of #2676. Provider-internal events are not logged: expiry is part of the acquire step.

**Class:** Trivial
**Settled by:** [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D3, D4 and D6.3; `AGENTS.md` section 3 (`EncinaError.Message` never reaches logs)

</details>

---

## Implementation Phases

### Phase 1: Abstractions, Error Codes and the InMemory Provider (PR 1)

> **Goal**: The contract and its reference implementation, with the contract test suite that every later provider must pass.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Semaphore/IDistributedSemaphoreProvider.cs`** (new, `src/Encina.DistributedLock/`) - `TryAcquireAsync(string resource, int maxCount, TimeSpan expiry, TimeSpan wait, TimeSpan retry, CancellationToken)` returning `Task<Either<EncinaError, Option<ISemaphoreHandle>>>`; `AcquireAsync(resource, maxCount, expiry, ct)` returning `Task<Either<EncinaError, ISemaphoreHandle>>`; `GetAvailableCountAsync(resource, maxCount, ct)` and `GetCurrentCountAsync(resource, ct)` returning `Task<Either<EncinaError, int>>`. XML docs state contention versus fault and the per-call `maxCount` rule.
2. **`Semaphore/ISemaphoreHandle.cs`** (new) - `IAsyncDisposable` with `Resource`, `MaxCount`, `TicketId`, `AcquiredAtUtc`, `ExpiresAtUtc`, `IsReleased` and `ExtendAsync(TimeSpan, CancellationToken = default)` (the same return shape as `ILockHandle.ExtendAsync` after #2667).
3. **`Semaphore/SemaphoreErrorCodes.cs`** (new, public static) - codes in a separate class so this plan does not edit `LockErrorCodes` (created by #2667): `semaphore.invalid_max_count` (ADR-062 category `Validation`, R3), `semaphore.invalid_resource` (`Validation`), `semaphore.invalid_expiry` (`Validation`), `semaphore.backend_failure` (`Internal`, R4: store and provider failures). Each is created with `EncinaErrors.Create(code, ErrorCategory.X, message)`; the message never contains the resource name or the exception message. Working names, re-aligned with the naming of the codes #2667 creates when it merges.
4. **`Encina.DistributedLock.InMemory/InMemoryDistributedSemaphoreProvider.cs`** (new, public sealed) - constructor `(IOptions<InMemoryLockOptions>, TimeProvider? timeProvider = null)`; a `ConcurrentDictionary<string, SemaphoreEntry>` keyed by `{KeyPrefix}:semaphore:{resource}`; every mutation under a per-entry lock; expired tickets are pruned on each call; wait and retry loop with `Task.Delay(..., _timeProvider, ct)`.
5. **`Encina.DistributedLock.InMemory/ServiceCollectionExtensions.cs`** (modify) - `AddEncinaDistributedSemaphoreInMemory()` with `TryAddSingleton<IDistributedSemaphoreProvider, InMemoryDistributedSemaphoreProvider>()` (registers its options the way `AddEncinaDistributedLockInMemory` does).
6. **PublicAPI** - `src/Encina.DistributedLock/PublicAPI.Unshipped.txt` and `src/Encina.DistributedLock.InMemory/PublicAPI.Unshipped.txt` (via `mechanical-fixer`, lines decided by the worker).
7. **Tests** - unit (`tests/Encina.UnitTests/DistributedLock/InMemoryDistributedSemaphoreProviderTests.cs`: capacity, release, expiry with `FakeTimeProvider`, extend, wait elapses, cancellation, `maxCount` 1 equals a mutex, DI built with `ValidateOnBuild` and `ValidateScopes`); guard (`tests/Encina.GuardTests/Infrastructure/DistributedLock/InMemoryDistributedSemaphoreProviderGuardTests.cs`: null `options`, null or whitespace `resource`, non-positive `maxCount` is a `Left`, not an exception); contract (`tests/Encina.ContractTests/DistributedLock/DistributedSemaphoreContractTests.cs`: an abstract base with the scenarios, an InMemory subclass; the Redis and SQL Server subclasses are added by their PRs).
8. **Manifests** - `Encina.DistributedLock.json` and `Encina.DistributedLock.InMemory.json` rows for the new files (via `mechanical-fixer`); a `ProjectReference` from `Encina.ContractTests` to `Encina.DistributedLock.InMemory` if #2667 or #755 have not added it.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #215: the distributed semaphore abstraction and its InMemory provider.

CONTEXT:
- Issue #2667 has already changed IDistributedLockProvider to the ADR-052 shapes (Either, Option for contention)
  and created LockErrorCodes; read their current signatures and mirror them.
- Contention (semaphore full, wait elapsed) is Right(None), never a failure; a fault is Left with an ErrorCategory
  (ADR-062: Validation for field rules, Internal for store failures); cancellation propagates.
- Time comes from TimeProvider (AGENTS.md section 3); the provider takes an optional TimeProvider defaulting to System.

TASK:
Create IDistributedSemaphoreProvider, ISemaphoreHandle, SemaphoreErrorCodes and InMemoryDistributedSemaphoreProvider,
the AddEncinaDistributedSemaphoreInMemory registration, and the unit, guard and contract tests described in the Phase 1
tasks. The contract tests are an abstract base so later providers only add a subclass.

KEY RULES:
- maxCount < 1, empty resource and non-positive expiry return Left (Validation), never throw.
- A ticket past its expiry never counts; pruning happens on every call.
- Never put the resource name in an error message, log argument or exception message.
- ArgumentNullException guards on constructor parameters; XML documentation on every public member.

REFERENCE FILES:
- src/Encina.DistributedLock/IDistributedLockProvider.cs, ILockHandle.cs
- src/Encina.DistributedLock.InMemory/InMemoryDistributedLockProvider.cs, ServiceCollectionExtensions.cs
- docs/architecture/adr/052-distributed-lock-failure-contract.md
```

</details>

### Phase 2: Observability and the EventId Range (PR 2)

> **Goal**: Spans, metrics and logs for every semaphore provider through one decorator; this PR registers the family range.

<details>
<summary><strong>Tasks</strong></summary>

1. **Range registration (this plan owns it)** - add `DistributedLockPrimitives = (30550, 30564)` to `src/Encina/Diagnostics/EventIdRanges.cs`, one line in `src/Encina/PublicAPI.Unshipped.txt`, the `AssemblyRanges` entry for `Encina.DistributedLock` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (append to the list if #741 or #222 already added the key), the "Registered ranges" row and the `reserved` row `30550-30599 | Caching | lock primitives #215 30550-30552, #216 30553-30558, #217 30559-30561, #219 30562-30564` in `docs/engineering/eventid-block-registry.md`.
2. **`Diagnostics/SemaphoreLog.cs`** (new, `internal static partial`) - `[LoggerMessage]`: 30550 acquisition contended (Debug: system, wait ms, outcome), 30551 backend failure (Error: system, error code only), 30552 ticket lost on extend (Warning: system). No resource name, tenant id or `EncinaError.Message` is an argument.
3. **`Diagnostics/InstrumentedDistributedSemaphoreProvider.cs`** and **`InstrumentedSemaphoreHandle.cs`** (new, internal sealed; the handle wrapper keeps `ISemaphoreHandle`) - span `encina.distributed_semaphore.acquire`, counter `encina.distributed_semaphore.acquisitions` (tags `encina.lock.outcome` = `acquired` | `contended` | `failed`, `encina.lock.system`, `error.type` on failure, tenant tag through the #2676 helper when multi-tenancy is on), histogram `encina.distributed_semaphore.acquisition.duration` (`s`), up-down counter `encina.distributed_semaphore.held`. `encina.lock.system` comes from the explicit type-name table of #724, extended by the semaphore provider types.
4. **`ServiceCollectionExtensions.cs`** (modify, core) - `AddEncinaDistributedSemaphoreTelemetry()` decorates the registered `IDistributedSemaphoreProvider`; throws `InvalidOperationException` naming the missing registration when none exists (registration completeness, AGENTS.md section 3).
5. **Tests** - unit (outcomes map to the tag values; `Right(None)` is `contended`, never `failed`; no resource name in any tag, log or span), guard, DI test with `ValidateOnBuild` and `ValidateScopes`; load and benchmark justification files (`tests/Encina.LoadTests/DistributedLock/DistributedSemaphore.md`, `tests/Encina.BenchmarkTests/Encina.DistributedLock.Benchmarks/DistributedSemaphore.md`: coordination call, contended paths covered by the integration contention scenario of PR 3).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #215: telemetry for the distributed semaphore.

CONTEXT:
- AGENTS.md section 7: the EventId range is registered first in src/Encina/Diagnostics/EventIdRanges.cs; ids 30550-30552
  belong to this plan, packed with no gaps; also add the AssemblyRanges entry, the PublicAPI line and the registry rows.
- ADR-054 D3/D4/D6.3: package-named meter, no _total suffix, tenant tag only when multi-tenancy is on, no resource names.
- #724 (decorator pattern) and #741 (shared DistributedLockDiagnostics) are sibling plans; reuse the holder if it exists.

TASK:
Register the range, write SemaphoreLog, the instrumented provider and handle, and AddEncinaDistributedSemaphoreTelemetry
with its tests. Right(None) is contention, counted and logged at Debug, never as a failure.

KEY RULES:
- Only the error code or exception type may be logged (ForLogging()); never EncinaError.Message.
- The handle wrapper must remain an ISemaphoreHandle.
- Cardinality: tags are bounded sets (outcome, system, error.type).

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- docs/engineering/eventid-block-registry.md
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

### Phase 3: Redis Provider (PR 3)

> **Goal**: `RedisDistributedSemaphoreProvider` over a sorted set and one Lua script.

<details>
<summary><strong>Tasks</strong></summary>

1. **`RedisDistributedSemaphoreProvider.cs`** (new, `src/Encina.DistributedLock.Redis/`, public sealed; constructor `(IConnectionMultiplexer, IOptions<RedisLockOptions>, ILogger<...>?, TimeProvider? = null)`) - key `{KeyPrefix}:semaphore:{resource}`; one acquire script: `ZREMRANGEBYSCORE key -inf now`, `ZCARD`, `ZADD` when below the limit, `PEXPIRE key` to the latest member expiry, `now` from `redis.call('TIME')`; release `ZREM`; extend an ownership-checked `ZADD XX`; counts through `ZREMRANGEBYSCORE` then `ZCARD`. A `RedisException` becomes `Left` (`semaphore.backend_failure`), cancellation propagates.
2. **`ServiceCollectionExtensions.cs`** (modify) - `AddEncinaDistributedSemaphoreRedis()` (`TryAddSingleton`, reuses the connection registered by `AddEncinaDistributedLockRedis`; an `InvalidOperationException` when no `IConnectionMultiplexer` is registered).
3. **Quorum mode of #226** - the semaphore uses the first configured endpoint only (a sorted set cannot be quorum-written); the XML docs and README state that the semaphore is not quorum-protected.
4. **Tests** - unit with a mocked `IDatabase` (script arguments, `Left` mapping), guard, contract subclass over a Redis container (`Collection("Caching-Redis")` or the fixture the Redis lock integration tests use), integration: 50 concurrent acquirers with `maxCount` 5 never exceed 5 holders; a killed holder's ticket is reclaimed after its expiry; PublicAPI and manifest rows.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #215: the Redis semaphore provider.

CONTEXT:
- Redis lock keys are {KeyPrefix}:lock:{resource}; the semaphore uses {KeyPrefix}:semaphore:{resource}.
- All state changes happen inside one Lua script, so acquire is atomic; time is read with redis.call('TIME').
- ADR-052: contention is Right(None); a RedisException is Left; cancellation propagates.

TASK:
Create the provider, its registration, the unit, guard, contract and integration tests, and register the provider type in
the telemetry type-name table.

KEY RULES:
- No resource name in errors, logs or tags.
- The Redis provider reads no wall clock in C# for expiry; ExpiresAtUtc of the handle uses TimeProvider.
- Integration tests use the shared fixture, never a per-class container.

REFERENCE FILES:
- src/Encina.DistributedLock.Redis/RedisDistributedLockProvider.cs, ServiceCollectionExtensions.cs
- tests/Encina.IntegrationTests/Infrastructure/DistributedLock/RedisDistributedLockIntegrationTests.cs
```

</details>

### Phase 4: SQL Server Provider and Schema Script (PR 4)

> **Goal**: `SqlServerDistributedSemaphoreProvider` over a ticket table.

<details>
<summary><strong>Tasks</strong></summary>

1. **`SqlServerDistributedSemaphoreProvider.cs`** (new) - per call: open a connection (`OpenAsync(ct)`), `BEGIN TRAN`, `sp_getapplock @Resource = <hashed key>, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 0`-style serialization per resource, `DELETE` expired rows (`ExpiresAtUtc < SYSUTCDATETIME()`), `SELECT COUNT(*)`, `INSERT` the ticket when below the limit, `COMMIT`. The table is `[dbo].[EncinaDistributedSemaphore]` (`Resource nvarchar(256)`, `TicketId uniqueidentifier`, `ExpiresAtUtc datetime2`, primary key `(Resource, TicketId)`); the stored resource is the key `{KeyPrefix}:semaphore:{resource}` hashed to a fixed-length value when longer than 256 characters (SHA-256 hex), so two distinct resources never map to one row.
2. **`Scripts/DistributedSemaphore.sql`** (new, embedded or copied by the package) and `SqlServerLockOptions` additions: `EnsureSemaphoreSchema` (default `false`, Design Choice 3), `SemaphoreTableName`.
3. **`ServiceCollectionExtensions.cs`** (modify) - `AddEncinaDistributedSemaphoreSqlServer()`.
4. **Tests** - unit (SQL text, classification of SQL errors to `Left`), guard, contract subclass, integration on the shared SQL Server fixture: capacity under 50 concurrent acquirers, expiry reclaim, a missing table returns `Left` (no table name in the message), schema creation when `EnsureSemaphoreSchema` is on.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #215: the SQL Server semaphore provider.

CONTEXT:
- Database calls are asynchronous with a CancellationToken (AGENTS.md section 3); never the synchronous overloads.
- The existing provider uses sp_getapplock with Session ownership (src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs);
  the semaphore uses Transaction ownership because it only needs short mutual exclusion around the count.
- Expiry uses the server clock (design choice 6); the schema is opt-in (design choice 3).

TASK:
Create the provider, the DDL script, the options, the registration and the tests of the Phase 4 tasks.

KEY RULES:
- Parameterize every statement; the table name from options is validated as an identifier and quoted.
- Contention is Right(None); SQL errors are Left (semaphore.backend_failure); cancellation propagates.

REFERENCE FILES:
- src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs, SqlServerLockOptions.cs
- tests/Encina.IntegrationTests/Infrastructure/DistributedLock/SqlServerDistributedLockIntegrationTests.cs
```

</details>

### Phase 5: PostgreSQL, MySQL, Azure Blob and DynamoDB Providers (PRs 5 to 8)

> **Goal**: The four remaining providers, one pull request each, each starting after its provider package merged.

<details>
<summary><strong>Tasks</strong></summary>

1. **PR 5 - PostgreSQL** (after #207): `PostgreSqlDistributedSemaphoreProvider` in `Encina.DistributedLock.PostgreSQL`; the same table over `pg_advisory_xact_lock(hashtextextended(key, 0))` for serialization, `clock_timestamp()` for expiry; `AddEncinaDistributedSemaphorePostgreSql()`; the DDL script; tests and integration on the PostgreSQL fixture.
2. **PR 6 - MySQL** (after #208): `MySqlDistributedSemaphoreProvider`; serialization with `GET_LOCK(<64-char SHA-256 hex>, timeout)` on the transaction's connection, `UTC_TIMESTAMP(6)` for expiry; the DDL script; tests.
3. **PR 7 - Azure Blob** (after #209): `AzureBlobDistributedSemaphoreProvider`; slot blobs `semaphore/{sha256-hex}/{index}`, acquire the first slot whose lease is free (`409 LeaseAlreadyPresent` is occupied), renewal by the handle as in the lock handle of #209; `GetCurrentCountAsync` counts leased slot blobs; Azurite tests. Limit: `maxCount` is capped by `AzureBlobLockOptions.MaxSemaphoreSlots` (default 100; larger is `Left` `semaphore.invalid_max_count`), because every acquire may probe every slot.
4. **PR 8 - DynamoDB** (after #210): `DynamoDbDistributedSemaphoreProvider`; one item per resource with a `Tickets` map and a `Version` number; acquire is a read followed by a conditional `UpdateItem` on the version (retry on `ConditionalCheckFailedException`, bounded); expiry compares `now` supplied by the client `TimeProvider`; LocalStack tests.
5. Each PR adds the contract subclass, its PublicAPI and manifest lines, the registration, the README section of its package, and a row in the telemetry type-name table.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing one pull request of Phase 5 of issue #215: a semaphore provider for a lock package that already exists.

CONTEXT:
- The contract suite DistributedSemaphoreContractTests (Phase 1) is an abstract base; add one subclass per provider.
- AGENTS.md section 5: a lock feature is complete for 1.0 when all seven providers cover it; cloud providers are tested
  with Azurite and LocalStack.

TASK:
Implement the provider named in your brief against the mechanism in the Phase 5 tasks and make the contract suite pass
on the provider's own fixture.

KEY RULES:
- ADR-052 outcomes; no resource name in errors, logs or tags; asynchronous database calls with CancellationToken.
- Do not change the abstraction; if the contract cannot be met, stop and report.

REFERENCE FILES:
- docs/plans/lock-postgresql-implementation-plan-207.md (or -208, -209, -210) for the package layout
- tests/Encina.ContractTests/DistributedLock/DistributedSemaphoreContractTests.cs
```

</details>

### Phase 6: Documentation and Finalization

> **Goal**: Users can find and use the semaphore.

<details>
<summary><strong>Tasks</strong></summary>

1. **Package README** of `Encina.DistributedLock` (via `docs-writer`) - a "Semaphores" section: usage with `await using`, `maxCount` semantics, expiry and `ExtendAsync`, the per-provider table, the limits (Azure Blob slots, Redis quorum).
2. **Changelog fragment** `changelog.d/215-distributed-semaphore.added.md` (via `mechanical-fixer`).
3. **XML documentation** on every public member, with an example on `IDistributedSemaphoreProvider`.
4. `docs/INVENTORY.md` rows for the new files if that page lists them; `ROADMAP.md` unchanged unless the milestone changes.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are finishing issue #215: documentation of the distributed semaphore.

CONTEXT:
- Every provider of SPEC-000 DEC-009 now implements IDistributedSemaphoreProvider.

TASK:
Write the README section and the changelog fragment through the specialists; verify the XML docs of the public API.

KEY RULES:
- Documentation never types coverage figures by hand.
- English only; no private client application names.

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
| SPEC-000 REQ-027, DEC-009, AC-027 | The seven lock providers and the contract tests over all of them |
| ADR-052 | `Either` outcomes; contention is `Right(None)` |
| ADR-054 D3, D4, D6.3 | Meter naming, tenant tag, no data-bearing names |
| ADR-062 | Error categories of the new codes |
| Redis sorted-set semaphore recipe; Medallion `DistributedLock` semaphores | Named comparable products for the design |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `IDistributedLockProvider`, `ILockHandle` | `src/Encina.DistributedLock/` | Shape and DI pattern to mirror |
| `InMemoryLockOptions`, `RedisLockOptions`, `SqlServerLockOptions` | provider packages | Key prefix and connection settings reused by the semaphore providers |
| `EncinaErrors.Create(code, category, message)` | `src/Encina/` | Creating the `Left` values |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Range registration |
| `RedisDistributedLockHealthCheck`, `SqlServerDistributedLockHealthCheck` | provider `Health/` folders | Already probe the connection the semaphore uses (no new health check) |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Provider | Package | Mechanism | Status of the package | Lands in |
|----------|---------|-----------|-----------------------|----------|
| InMemory | `Encina.DistributedLock.InMemory` | dictionary under a lock, `TimeProvider` | exists | PR 1 |
| Redis | `Encina.DistributedLock.Redis` | sorted set, one Lua script | exists | PR 3 |
| SQL Server | `Encina.DistributedLock.SqlServer` | ticket table, `sp_getapplock` (Transaction) | exists | PR 4 |
| PostgreSQL | `Encina.DistributedLock.PostgreSQL` | ticket table, `pg_advisory_xact_lock` | planned (#207) | PR 5 |
| MySQL | `Encina.DistributedLock.MySQL` | ticket table, `GET_LOCK` | planned (#208) | PR 6 |
| Azure Blob | `Encina.DistributedLock.AzureBlob` | one lease per slot blob | planned (#209) | PR 7 |
| DynamoDB | `Encina.DistributedLock.DynamoDB` | one item, conditional update | planned (#210) | PR 8 |

### Event ID Allocation

| Package | Range | Ids |
|---------|-------|-----|
| `Encina.DistributedLock` (new field `DistributedLockPrimitives`, slot `30550-30599`) | `(30550, 30564)` registered by this plan | #215: 30550 contended, 30551 backend failure, 30552 ticket lost on extend; #216: 30553-30558; #217: 30559-30561; #218: none; #219: 30562-30564 |

### Estimated File Count

| PR | Content | New | Modified | Tests, manifests, PublicAPI, docs | Total | Lines (est.) |
|----|---------|-----|----------|------------------------------------|-------|--------------|
| 1 | Abstractions, codes, InMemory, README section | 4 | 1 (InMemory registration) | 10 (3 test files, 2 PublicAPI, 2 manifests, ProjectReference, README, fragment) | 15 | 1,050 |
| 2 | Range, decorator, log | 5 (log, provider, handle, 2 justification `.md`) | 7 (`EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, `AssemblyRanges` test, registry page, core `ServiceCollectionExtensions.cs`, core `PublicAPI.Unshipped.txt`, core manifest) | 3 (unit, guard, DI test) | 15 | 900 |
| 3 | Redis | 1 | 1 | 6 (unit, guard, contract subclass, integration, PublicAPI, manifest) | 8 | 800 |
| 4 | SQL Server | 2 (provider, script) | 2 (options, registration) | 7 | 11 | 950 |
| 5 to 8 | PostgreSQL, MySQL, Azure Blob, DynamoDB | 1 to 2 each | 1 to 2 each | 6 each | 9 to 10 each | 700 to 900 each |

The README section and the changelog fragment ride in PR 1 (counted above); later PRs add only their package README line.

Every PR is at most 15 files and 1,500 lines and builds and passes alone (PR 1 introduces the interfaces with one implementation; PRs 3 to 8 only add implementers).

### Per-file coverage targets (numeric, per flag; measured baseline recorded when the PR runs `dotnet test` with coverage)

| File | Unit | Guard | Contract | Integration | Justification |
|------|------|-------|----------|-------------|---------------|
| `InMemoryDistributedSemaphoreProvider.cs` | 95 | 25 | 80 | 0 | Pure in-process logic, every branch reachable with `FakeTimeProvider`; guards cover constructor parameters; the contract suite exercises the public behavior; no external system |
| `InstrumentedDistributedSemaphoreProvider.cs`, `InstrumentedSemaphoreHandle.cs` | 90 | 25 | 0 | 0 | Decorator with a mockable inner provider; contract tests target providers, not the decorator |
| `RedisDistributedSemaphoreProvider.cs` | 70 | 25 | 70 | 75 | Script result parsing and `Left` mapping are unit-testable with a mocked `IDatabase`; script atomicity only proves itself on a real Redis |
| `SqlServerDistributedSemaphoreProvider.cs` | 65 | 25 | 70 | 75 | SQL classification is unit-testable; transactions and `sp_getapplock` need a real server |
| `SemaphoreErrorCodes.cs`, `IDistributedSemaphoreProvider.cs`, `ISemaphoreHandle.cs` | 0 | 0 | 0 | 0 | Constants and interfaces: no executable code to measure; covered through the providers |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is pre-1.0 (.NET 10, C# 14, nullable enabled). Rules are in AGENTS.md. Issue #215 adds a distributed semaphore
(N concurrent holders of a named resource) to Encina.DistributedLock and to every lock provider of SPEC-000 DEC-009.

IMPLEMENTATION OVERVIEW:
1. Phase 1: IDistributedSemaphoreProvider, ISemaphoreHandle, SemaphoreErrorCodes, InMemory provider, contract suite.
2. Phase 2: DistributedLockPrimitives EventId range (30550-30564), instrumented decorator, SemaphoreLog (30550-30552).
3. Phase 3: Redis (sorted set + Lua). Phase 4: SQL Server (ticket table, opt-in schema).
4. Phase 5: PostgreSQL, MySQL, Azure Blob, DynamoDB, each after its package (#207-#210).
5. Phase 6: README section, changelog fragment (through the specialists).

KEY PATTERNS:
- ADR-052: Either<EncinaError, Option<handle>>; contention is Right(None); cancellation propagates.
- Errors carry a code and an ErrorCategory (ADR-062); never the resource name or exception message.
- Time through TimeProvider; database calls asynchronous with CancellationToken.
- Registration completeness: AddEncinaDistributedSemaphore*() registers everything it resolves; DI test with
  ValidateOnBuild and ValidateScopes.
- Telemetry: Meter "Encina.DistributedLock", no _total suffix, tenant tag only when multi-tenancy is on, no resource names.

REFERENCE FILES:
- docs/plans/lock-semaphore-implementation-plan-215.md, src/Encina.DistributedLock/, src/Encina.DistributedLock.Redis/
- docs/architecture/adr/052-distributed-lock-failure-contract.md, docs/plans/distributed-lock-opentelemetry-implementation-plan-724.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Semaphore state must be real-time; a cached count would admit extra holders |
| 2 | OpenTelemetry | ✅ Include | Phase 2: spans and metrics through the decorator; no resource name, tenant tag only when multi-tenancy is on (ADR-054 D4) |
| 3 | Structured Logging | ✅ Include | Phase 2: `SemaphoreLog` 30550-30552, error code only |
| 4 | Health Checks | ❌ N/A | The existing Redis and SQL Server lock health checks probe the same connection the semaphore uses; the issue's "backend connectivity" is already covered |
| 5 | Validation | ✅ Include | `maxCount`, resource and expiry validated, returned as `Left` with category `Validation` (Phase 1) |
| 6 | Resilience | ✅ Include | Auto-release through ticket expiry; wait and retry loop with the caller's budget; no Polly policy (a retry around acquire would double the caller's wait) |
| 7 | Distributed Locks | ✅ Include | This is a lock primitive; the ticket table serialization uses the database's own lock functions |
| 8 | Transactions | ❌ N/A | Coordination, not data mutation; the SQL ticket transaction is internal to the provider |
| 9 | Idempotency | ❌ N/A | Releasing a ticket twice is a no-op (`IsReleased`); acquisition is not a deduplicated operation |
| 10 | Multi-Tenancy | ⏭️ Defer | Tenant scoping of lock keys is #741 for `IDistributedLockProvider` only; applying the same key derivation to the semaphore is one follow-up, see `artifacts/issues/lock-primitives-tenant-scoping.md` |
| 11 | Module Isolation | ❌ N/A | Modules are separated with `KeyPrefix` (the position of the #741 plan); a module-scoped semaphore is a prefix, not new code |
| 12 | Audit Trail | ❌ N/A | Infrastructure coordination with no personal or regulated data; observability covers operations |

---

## Prerequisites & Dependencies

| Item | State | Needed for |
|------|-------|------------|
| #2667 ADR-052 implementation | open | All PRs (the `Either` shapes and `LockErrorCodes` conventions) |
| #724 / #741 shared `DistributedLockDiagnostics` | plans on main, code not written | PR 2 (reuse the holder, or create it if first) |
| #2676 tenant tag helper | open | PR 2 (tenant tag); until it lands the decorator emits no tenant tag |
| #207, #208, #209, #210 provider packages | plans on main, packages not written | PRs 5 to 8 |

---

## Next Steps

1. Open the pull request of PR 1 first; PR 2 follows (it registers the family EventId range that #216, #217 and #219 wait for).
2. The maintainer answers Dilemmas 1, 2, 3, 5 and 6 when work on the issue starts; PRs 1 and 2 do not depend on 2 and 3 (they concern SQL and the clock of SQL providers), so they can start earlier.
3. Open the tenant-scoping follow-up issue (`artifacts/issues/lock-primitives-tenant-scoping.md`) once, for the whole family.
