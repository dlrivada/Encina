# Implementation Plan: Fair Lock Queuing (`IFairDistributedLockProvider`, FIFO tickets, optional priority, bounded queue)

> **Issue**: [#225](https://github.com/dlrivada/Encina/issues/225)
> **Type**: Feature
> **Complexity**: High (a queue protocol over seven backends, abandoned-waiter handling, a decorator surface, 11 pull requests)
> **Estimated Scope**: ~1,500 lines of production code + ~2,300 lines of tests (about 6,700 lines over 11 pull requests; the per-PR estimates in "Estimated File Count" sum to it)
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027, DEC-009, AC-027; [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md); [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D3, D4, D6.3; [ADR-062](../architecture/adr/062-error-category-contract.md)
> **Depends on**: [#2667](https://github.com/dlrivada/Encina/issues/2667) (ADR-052 implementation; open, not a pending maintainer decision); PRs 5 to 8 need the packages of [#207](https://github.com/dlrivada/Encina/issues/207), [#208](https://github.com/dlrivada/Encina/issues/208), [#209](https://github.com/dlrivada/Encina/issues/209), [#210](https://github.com/dlrivada/Encina/issues/210) (plans on main, not written); PR 9 needs the decorators of #724, #741 and #223; [#2405](https://github.com/dlrivada/Encina/issues/2405) (open bug: the SQL Server lock ignores its expiry) for SQL Server expiry scenarios
> **Related**: family `lock-pipeline` (#220, #221, #223, #224); #218 (per-resource counters for fencing, a separate counter from the ticket sequence), #741, #724, #226, #2653

---

## Summary

A lock that waits by polling with retries has no order: a waiter that always loses the race can starve under contention. The issue itself warns: "Only implement if fairness is a requirement". This plan adds an opt-in **fair queue** in front of any lock provider, so the seven providers of SPEC-000 DEC-009 get the same behavior from one engine:

1. **Tickets.** A waiter takes a ticket from a store-side sequence of its resource; the lock is attempted only by the head of the queue (lowest `(priority, sequence)` among live tickets). FIFO holds across processes because the order comes from the store, never from a local clock (Design Choice 4).
2. **ADR-052 shapes.** `IFairDistributedLockProvider : IDistributedLockProvider` has `TryAcquireWithQueueAsync(resource, expiry, wait, retry, LockQueueOptions?, ct)` returning `Either<EncinaError, Option<ILockHandle>>` (the issue's `IAsyncDisposable?` is dropped), `GetQueueDepthAsync` and `GetQueuePositionAsync` returning `Either<EncinaError, int>` and `Either<EncinaError, Option<int>>`.
3. **Bounded and self-cleaning.** `MaxQueueSize` rejects new waiters with a stable code; a ticket carries a short lease that the waiter refreshes while it waits, so a crashed waiter never blocks the head for longer than the lease.
4. **Priority, opt-in.** `UsePriority` (default false) orders by priority first; a high-priority waiter can starve low-priority ones (documented).
5. **Composition.** The engine wraps the registered provider and a per-backend `ILockQueueStore`; no lock provider changes its locking code.
6. **Wake-up by polling.** Waiters poll the head position at a configurable interval; native signals are an optimization recorded as Dilemma 3.

**Standards**: ADR-052; ADR-054; ADR-062; `AGENTS.md` section 3 (ROP, errors never swallowed, asynchronous database calls with a `CancellationToken`, time through `TimeProvider`), section 5 (seven lock providers consistent, SQL differences) and section 6.

**Affected packages**: `Encina.DistributedLock` (interface, engine, options, ordering, registration), `.InMemory`, `.Redis`, `.SqlServer` (one queue store each), the four planned packages (#207 to #210, one store each), the decorators of #741, #724, #223 (forwarding), `Encina` (EventId registration only when this plan is the first of the family with a log); test projects `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.PropertyTests`, `Encina.IntegrationTests`, `Encina.TestInfrastructure`.

**Provider category**: Distributed locks (`AGENTS.md` section 5): all seven providers get a store (InMemory, Redis, SQL Server now; PostgreSQL, MySQL, Azure Blob, DynamoDB when #207 to #210 land); complete for 1.0 only when contract tests cover all seven. Database (10) does not apply to the lock stores (SQL schemas follow the SQL differences of `AGENTS.md` section 5).

**What the code does today** (`origin/main` at `f9063538e`):

- Nothing of this feature exists: no `IFairDistributedLockProvider`, `LockQueueOptions` or queue type under `src/`. Waiting is a retry loop: `TryAcquireAsync(resource, expiry, wait, retry, ct)` retries every `retry` until `wait` elapses (`InMemoryDistributedLockProvider.cs`, `RedisDistributedLockProvider.cs`), so any waiter may win any attempt. SQL Server waits inside `sp_getapplock` (`SqlServerDistributedLockProvider.cs`), whose waiter order is not a contract of Encina. #2667 (open) changes the return shapes.
- **The Redis provider takes one shared `IConnectionMultiplexer`** (`Encina.DistributedLock.Redis/RedisDistributedLockProvider.cs`, constructor): the issue's `BLPOP` would hold that shared connection for its timeout, which is why this plan polls (Design Choice 3).
- The issue's Redis script (`ZRANGE ... SET lock ...`) races a plain `TryAcquireAsync` caller that takes the lock key between the head check and the `SET`; the engine lets the underlying lock arbitrate (the head only calls the normal `TryAcquireAsync`), and a caller that bypasses the queue by calling the plain API can still barge in. Fairness holds among callers that use the queue (documented).
- **Implementers**: this plan adds `IFairDistributedLockProvider`, whose implementers are the new engine (`FairDistributedLockProvider`) and the forwarding decorators of #741 (tenant), #724 (telemetry) and #223 (auto-extend): none exists yet; no existing class implements it. It adds no member to `ILockHandle` or `IDistributedLockProvider`; the five existing `IDistributedLockProvider` classes (InMemory, Redis, SQL Server, `Encina.Caching.Memory/MemoryDistributedLockProvider.cs:21`, `Encina.Caching.Redis/RedisDistributedLockProvider.cs:17`) and the three private `LockHandle` classes (InMemory `:250`, Redis `:223`, SQL Server `:242`) are untouched; a declaration-form multi-line search of `src/` and `tests/` finds no test class implementing either interface. `ILockQueueStore` is new: its implementers are the seven stores of the Provider Matrix (three exist as packages, four not written).
- `Encina.DistributedLock` has `InternalsVisibleTo` for `Encina.UnitTests`, `IntegrationTests`, `ContractTests` and `PropertyTests` (so the ordering rules can be property-tested), not for `Encina.GuardTests` (public types only). `tests/Encina.ContractTests` references no `Encina.DistributedLock*` project and has no Redis or SQL Server collection; `tests/Encina.IntegrationTests` has the shared `ADO-SqlServer` collection and no Redis collection.
- `.github/coverage-manifest/Encina.DistributedLock.json`: package targets guard 20, unit 60; per-file Redis target for `RedisDistributedLockProvider.cs` (unit 95, guard 14, integration 70) is untouched by this plan.

---

## Design Choices

<details>
<summary><strong>1. Where fairness lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Composition: one engine over `IDistributedLockProvider` plus an `ILockQueueStore` per backend; the lock providers keep their locking code** | One protocol, seven small stores; decorators and the multi-lock still see ordinary locks | Callers that bypass the queue can barge in |
| **B) Native: each provider implements `IFairDistributedLockProvider` itself, as the issue sketches** | A provider could make its own release queue-aware | Seven implementations of the protocol and of its failure handling; each one re-opens ADR-052 handling |

### Chosen Option: **A - composition** (recommended, pending the maintainer)

### Rationale

- A wins for a single, testable protocol and one place for abandoned-waiter logic; B wins if a backend offers a native fair primitive worth using (none of the seven does in the issue's own sketches: Redis sorted sets and a SQL table are stores, not fair locks).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the engine sits relative to the tenant decorator</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The engine wraps the raw provider (bottom of the chain); the decorators of #741, #724 and #223 implement `IFairDistributedLockProvider` by forwarding the scoped resource name; a decorator over a non-fair inner returns `Left` `lock_queue.not_supported`** | The queue key is the scoped lock name, so the queue domain equals the lock domain (two tenants never share a queue for a tenant-scoped name; a name #741 keeps global has one global queue) | Three decorators gain a forwarding surface (PR 9) |
| **B) The engine wraps the outermost provider and derives its own tenant segment from `IRequestContextAccessor`, failing closed when `RequireTenantContext ?? (tenant provider registered)` and no tenant is present (the pattern of `DefaultConsentService.cs:107` and `:128`)** | No change in other plans | The queue domain can differ from the lock domain: with #741 keeping a name global, tenants queue separately for one shared lock, so fairness across tenants is lost |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- A wins whenever tenant scoping is on (the queue and the lock must agree on the name); B wins when #741 is not used and a minimal engine is wanted (then its tenant segment is optional and no `default` segment is ever substituted). Under both options the resource name is never a log, tag or span value, and a tenant segment is escaped so two (tenant, resource) pairs never map to one queue key.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Provider scope and wake-up</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Seven stores (InMemory, Redis, SQL Server, PostgreSQL, MySQL, Azure Blob, DynamoDB), waiters poll their position every `PollInterval` (default 200 ms, validated `> 0`, `<= 5 s`)** | The seven-provider rule is met; one wake-up path; no extra connections held per waiter | Poll load grows with waiters; latency up to one interval |
| **B) The issue's three backends (Redis, SQL Server, PostgreSQL) with native signals (Redis pub/sub, PostgreSQL `LISTEN/NOTIFY`)** | Lower latency and load | PostgreSQL `LISTEN` needs one dedicated connection per waiter (the PostgreSQL lock already holds one per handle); MySQL, Azure Blob, DynamoDB, and InMemory would lack the feature, breaking `AGENTS.md` section 5 consistency |
| **C) A, with native signals added later as an optimization behind the same store interface** | Starts consistent, optimizes where measured | A second wake-up path to test later |

### Chosen Option: **A, with C left open** (recommended, pending the maintainer)

### Rationale

- A wins for consistency and bounded resources; B wins when waiter counts are high and only three backends matter. The seven-store scope is a deviation from the issue's acceptance criteria ("Redis, SQL Server, PostgreSQL"), made because `AGENTS.md` section 5 requires every provider of the category.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Ordering source</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A store-side, per-resource sequence assigned at enqueue (Redis `INCR`, SQL `IDENTITY`/sequence, PostgreSQL sequence, MySQL `AUTO_INCREMENT`, DynamoDB atomic `ADD` on a counter item, Azure Blob counter blob with `If-Match` retries, InMemory `Interlocked`); order is `(priority, sequence)` with `priority` ignored unless `UsePriority`** | True FIFO across processes whatever their clocks | One counter per resource per backend |
| **B) The waiter's local timestamp as the sorted-set score (the issue's `ZADD ... {timestamp}`)** | No counter | Clock skew between nodes lets a node with a fast clock jump the queue: not FIFO |

### Chosen Option: **A**

### Rationale

- The issue's goal is "FIFO ordering of lock acquisition"; only A delivers it across nodes. In Redis the score is `priority * 2^40 + sequence` (exact in a double up to `2^53`), so priorities are limited to 0 to 1000 and the sequence is per-resource (a resource would need over 10^12 waiters to overflow).

**Class:** Settled
**Settled by:** issue #225 Summary and Motivation ("ensure FIFO ordering of lock acquisition")

</details>

<details>
<summary><strong>5. Abandoned waiters and cancellation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every ticket has a lease (`TicketLease`, default 10 s) refreshed on each poll; head selection skips expired tickets and removes them best effort (logged as abandoned); on cancellation or timeout the waiter removes its own ticket in a `finally` with `CancellationToken.None`-safe cleanup, logged by code or type only** | A crashed waiter blocks the head for at most one lease; no stuck queue | One refresh write per poll |
| **B) No lease: a ticket lives until its owner removes it** | No refresh traffic | A crashed head blocks every waiter forever |

### Chosen Option: **A**

### Rationale

- Only A keeps the queue live; the refresh is the poll the waiter already makes, so it costs no extra call on stores that can read and refresh in one operation.

**Class:** Trivial
**Settled by:** `AGENTS.md` section 3 (background and waiting loops never leave failure unhandled; a lock must free itself, the rule behind lock expiry in `ILockHandle.ExpiresAtUtc`)

</details>

<details>
<summary><strong>6. Outcome codes and categories</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `lock_queue.full` is `RateLimited` (a bounded queue refusing new waiters), `lock_queue.invalid_options` is `Validation` (R3), `lock_queue.not_supported` and `lock_queue.store_unavailable` are `Internal` (R1, R4), a store fault is the store's `Left` as `lock_queue.backend_failure` (`Internal`); contention that outlasts `wait` is `Right(None)` as in ADR-052 (not an error)** | Stable codes with declared categories; clients can back off on `RateLimited` | A queue-full rejection counts as a client-side rate limit |
| **B) `lock_queue.full` as `Unavailable`** | Retry-elsewhere semantics | `Unavailable` reads as an outage to monitoring |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- A wins for monitoring by category; B wins when load balancers treat 503 as the signal to retry on another instance. Missing context never produces a `default` queue: with Design Choice 2 option A the tenant decision is the lock decorator's.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Contracts, Ordering and the Engine (PR 1)

> **Goal**: The protocol, testable against an in-test store.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Queue/IFairDistributedLockProvider.cs`**, **`LockQueueOptions.cs`**, **`ILockQueueStore.cs`**, **`LockQueueErrorCodes.cs`** (new, `src/Encina.DistributedLock/`) - `LockQueueOptions` (`MaxQueueSize` 100, `Priority` 100, `UsePriority` false, `WaiterId` optional so a caller can later ask for its position); `ILockQueueStore`: `EnqueueAsync`, `IsHeadAsync` (also refreshes the lease), `RemoveAsync`, `DepthAsync`, `PositionAsync` (all `Either` with the token).
2. **`Queue/LockQueueOrdering.cs`** (new, internal static) - `(priority, sequence)` comparison and the Redis score encoding; **`FairLockOptions.cs`** and **`FairLockOptionsValidator.cs`** (new) - `PollInterval`, `TicketLease`.
3. **`Queue/FairDistributedLockProvider.cs`** (new, public sealed; constructor `(IDistributedLockProvider inner, ILockQueueStore store, IOptions<FairLockOptions>, TimeProvider? timeProvider = null)`) - validate; enqueue (queue full is `Left`); loop until head, then call the inner `TryAcquireAsync` with the remaining wait; on acquire remove the ticket and return the handle; on timeout, cancellation or fault remove the ticket (Design Choice 5); one deadline for the call; the plain `IDistributedLockProvider` members forward to the inner provider unqueued.
4. **Tests** - against a test store: FIFO order of ten waiters, priority order when on and ignored when off, queue full, abandoned head skipped after the lease, cancellation removes the ticket, one deadline, faults returned with their code, property tests of the ordering (FsCheck: the order is total and stable); validator and constructor guard tests. PublicAPI lines and manifest entries (`mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #225: the fair lock queue contracts, ordering and engine.

CONTEXT:
- #2667 has changed IDistributedLockProvider to the ADR-052 Either shapes; read the current signatures. Contention is Right(None).
- Encina.DistributedLock has InternalsVisibleTo for UnitTests, PropertyTests and ContractTests but not GuardTests: guard-test public types only.
- Time comes from TimeProvider; Task.Delay(poll, timeProvider, ct).

TASK:
Create IFairDistributedLockProvider, LockQueueOptions, ILockQueueStore, LockQueueErrorCodes, LockQueueOrdering, FairLockOptions and its validator,
and FairDistributedLockProvider with the tests of the Phase 1 tasks.

KEY RULES:
- The order comes from the store sequence, never a local clock; remove the ticket on every exit path.
- Never log resource names, tenant values or EncinaError.Message; no exceptions for business outcomes.

REFERENCE FILES:
- src/Encina.DistributedLock/IDistributedLockProvider.cs, docs/architecture/adr/052-distributed-lock-failure-contract.md
- docs/plans/lock-multi-resource-implementation-plan-219.md (one-deadline pattern)
```

</details>

### Phase 2: InMemory Store and Registration (PR 2)

> **Goal**: A working fair lock for tests and single-instance use.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Queue/InMemoryLockQueueStore.cs`** (new, `src/Encina.DistributedLock.InMemory/`; `ConcurrentDictionary` of per-resource sorted lists under one lock per resource, `Interlocked` sequence, `TimeProvider` for leases).
2. **`ServiceCollectionExtensions.cs`** of the core (modify) - `AddEncinaDistributedLockFairQueue(Action<FairLockOptions>?)`: validated options, decorates the registered provider with the engine at the bottom of the chain (Design Choice 2), `InvalidOperationException` naming the missing call when no provider or no store is registered; the InMemory package registers its store with `TryAddSingleton` in `AddEncinaDistributedLockInMemory` (modify, InMemory `ServiceCollectionExtensions.cs`).
3. **Tests** - the engine over the InMemory provider and store with `FakeTimeProvider`; DI test with `ValidateOnBuild` and `ValidateScopes` with and without a store; README section and changelog fragment `changelog.d/225-fair-lock-queue.added.md` (specialists).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #225: the InMemory queue store and the registration.

CONTEXT:
- The engine decorates the registered provider and must be registered before the tenant, telemetry and auto-extend decorators.

TASK:
Create InMemoryLockQueueStore and AddEncinaDistributedLockFairQueue with the tests of the Phase 2 tasks.

KEY RULES:
- Registration completeness: everything the engine resolves is registered or the call fails at startup.

REFERENCE FILES:
- src/Encina.DistributedLock.InMemory/ServiceCollectionExtensions.cs, docs/plans/distributed-lock-opentelemetry-implementation-plan-724.md
```

</details>

### Phase 3: Redis and SQL Server Stores (PRs 3 and 4)

> **Goal**: Fair queuing on the two existing real backends.

<details>
<summary><strong>Tasks</strong></summary>

1. **PR 3, `Queue/RedisLockQueueStore.cs`** (new, `Encina.DistributedLock.Redis`) - sorted set per resource with the score of Design Choice 4, a counter key for the sequence, a ticket-lease key per waiter, one Lua script for "is head and refresh" that skips and removes expired tickets; no blocking commands on the shared multiplexer; registration `TryAdd` in the Redis `ServiceCollectionExtensions.cs` (modify).
2. **PR 4, `Queue/SqlServerLockQueueStore.cs`** (new) + `SqlServerLockQueueSchema.cs` (new) + `SqlServerLockOptions.cs` (modify: `CreateQueueTable`, table name) - `LockQueue` table as in the issue plus an `IDENTITY` sequence and `LeaseExpiresAtUtc`; `TOP (1)`, `@param`, `OpenAsync` and `ExecuteNonQueryAsync` with the token; opt-in creation, `Left` `lock_queue.store_unavailable` when the table is absent.
3. **Tests per PR** - unit (mocked `IDatabase`; schema text), guard, integration in shared fixtures (`ADO-SqlServer`; Redis through the shared fixture added by #220 PR 5 or by PR 3 when first), manifest rows that keep every existing target, PublicAPI lines.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #225: the Redis and SQL Server queue stores (one pull request each).

CONTEXT:
- The Redis provider shares one IConnectionMultiplexer: never BLPOP or any blocking command. SQL Server ignores lock expiry until #2405.

TASK:
Implement RedisLockQueueStore and SqlServerLockQueueStore with schema, registration and tests; one PR per store.

KEY RULES:
- Database calls asynchronous with the CancellationToken; parameterized SQL; never log resource names or EncinaError.Message.
- Do not lower any manifest target.

REFERENCE FILES:
- src/Encina.DistributedLock.Redis/RedisDistributedLockProvider.cs, src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs
```

</details>

### Phase 4: The Four Planned Providers (PRs 5 to 8)

> **Goal**: Seven stores, one contract.

<details>
<summary><strong>Tasks</strong></summary>

1. One PR per package after it merged: PostgreSQL #207 (`bigserial`, `LIMIT 1`, lowercase identifiers, table opt-in), MySQL #208 (`AUTO_INCREMENT`, backtick identifiers, `LIMIT 1`, table opt-in), Azure Blob #209 (ticket blobs named `{resource}/{sequence:D20}` so a prefix listing is in order, a counter blob incremented with `If-Match`; Azurite), DynamoDB #210 (partition key resource, sort key `{priority:D4}#{sequence:D20}`, atomic `ADD` counter item, TTL attribute; LocalStack). Each adds the store, its registration, tests, manifest rows, PublicAPI lines and one contract subclass.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #225: the queue store of one planned lock provider.

CONTEXT:
- The package of #207, #208, #209 or #210 has merged; read its provider before adding the store.

TASK:
Add the store, its registration, the tests and the contract subclass.

KEY RULES:
- SQL differences of AGENTS.md section 5; asynchronous database calls with the token; the sequence comes from the store.

REFERENCE FILES:
- docs/plans/lock-postgresql-implementation-plan-207.md (and 208, 209, 210), the merged package
```

</details>

### Phase 5: Decorator Forwarding (PR 9)

> **Goal**: The queue works through tenant scoping, telemetry and auto-extension.

<details>
<summary><strong>Tasks</strong></summary>

1. The tenant decorator (#741), `InstrumentedDistributedLockProvider` (#724) and `AutoExtendDistributedLockProvider` (#223) implement `IFairDistributedLockProvider` by forwarding `TryAcquireWithQueueAsync`, `GetQueueDepthAsync` and `GetQueuePositionAsync` with their own name mapping and wrapping; over a non-fair inner provider they return `Left` `lock_queue.not_supported`. Tests per decorator; PublicAPI lines; manifest rows.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #225: forwarding of the fair queue surface through the lock decorators.

CONTEXT:
- Each decorator changes its own mapping (tenant scoping, instrumentation, auto-extension) but the fair methods must reach the engine with the same transformed name.

TASK:
Add the IFairDistributedLockProvider surface to the three decorators with tests.

KEY RULES:
- Never swallow a Left; never log or tag the resource name.

REFERENCE FILES:
- docs/plans/tenant-scoped-lock-implementation-plan-741.md, docs/plans/distributed-lock-opentelemetry-implementation-plan-724.md, docs/plans/lock-auto-extend-implementation-plan-223.md
```

</details>

### Phase 6: Observability and Health (PR 10)

> **Goal**: Queue behavior is visible without leaking names.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Diagnostics/FairLockLog.cs`** (new, `internal static partial`) - 30617 waiter enqueued (Debug), 30618 lock granted from the queue (Debug: waited seconds), 30619 queue full so the waiter was rejected (Warning), 30620 abandoned ticket removed (Warning), 30621 wait exceeded the starvation threshold (Warning: waited seconds), 30622 queue store failure (Warning: error code). Ids 30617-30622 of the family range `DistributedLockPipeline` (30600-30649).
2. **`Diagnostics/FairLockMetrics.cs`** (new, internal) - counter `encina.distributed_lock.queue.requests` (tag `encina.lock.outcome`: `granted`, `timeout`, `full`, `failed`), histogram `encina.distributed_lock.queue.wait` (`s`), up-down counter `encina.distributed_lock.queue.waiters`; no `_total`; no resource name; tenant tag only with multi-tenancy (#2676).
3. **`Health/FairLockHealthCheck.cs`** (new; `IEncinaHealthCheck` with `DefaultName`, static `Tags`, scoped resolution through `IServiceProvider.CreateScope()`): healthy when the store answers a depth probe on a fixed sentinel name, `Degraded` above a configured wait threshold of the last window, `Unhealthy` on a store `Left` (error code only).
4. **EventId registration** only if this PR is the first of the family with a log (`EventIdRanges.cs` field `DistributedLockPipeline = (30600, 30649)`, core `PublicAPI.Unshipped.txt`, one registry row with the same bounds, `AssemblyRanges` entry; owned by #220 PR 4 by default).
5. **Tests** - `MeterListener`, log and health tests; load and benchmark: a load test of contention fairness is implemented (concurrent feature) and a benchmark justification `.md` file.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #225: telemetry and health of the fair queue.

CONTEXT:
- Ids 30617-30622 belong to the family range DistributedLockPipeline, registered by #220 PR 4 or by the first family PR with a log.
- Health checks follow RedisDistributedLockHealthCheck: DefaultName const, Tags static array, scoped resolution.

TASK:
Write FairLockLog, FairLockMetrics, FairLockHealthCheck, emit them from the engine and add the tests.

KEY RULES:
- Log error codes or exception types only; never resource names, tenant values or EncinaError.Message.

REFERENCE FILES:
- src/Encina.DistributedLock.Redis/Health/RedisDistributedLockHealthCheck.cs, docs/engineering/eventid-block-registry.md
```

</details>

### Phase 7: Contract, Property and Integration Scenarios (PR 11)

> **Goal**: One behavior on every provider that exists.

<details>
<summary><strong>Tasks</strong></summary>

1. **`tests/Encina.ContractTests/DistributedLock/FairLockContractTests.cs`** (new, abstract base plus an InMemory subclass) and **`Encina.ContractTests.csproj`** (modify: two `ProjectReference` lines unless #220 PR 5 added them; none exist today, nor any Redis or SQL Server fixture): FIFO of waiters, queue full, abandoned head skipped. Redis and SQL Server subclasses live in `Encina.IntegrationTests` (shared fixtures); each planned provider adds one subclass in its own PR.
2. **Property tests** (`tests/Encina.PropertyTests/DistributedLock/FairLockOrderingPropertyTests.cs`, new; the project already references the lock projects): for any interleaving of enqueue, refresh and expiry the grant order equals the `(priority, sequence)` order of the live tickets.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #225: contract, property and integration scenarios of the fair queue.

CONTEXT:
- Contract tests instantiate real implementations; Redis and SQL Server need Docker fixtures that only IntegrationTests has.

TASK:
Write the abstract contract base with the InMemory subclass, the property tests and the integration subclasses.

KEY RULES:
- Shared [Collection] fixtures only; FakeTimeProvider only on InMemory.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/DistributedLock/, tests/Encina.PropertyTests/
```

</details>

---

## Research

### Relevant Standards and Specifications

| Standard | Relevance |
|----------|-----------|
| SPEC-000 REQ-027, DEC-009, AC-027 | Seven lock providers, contract over all |
| ADR-052, ADR-054 D6.3, ADR-062 | Either shape, no data-bearing names, categories |
| Fairness in concurrent computing (ticket locks) | FIFO by a sequence number |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `IDistributedLockProvider`, handles | `src/Encina.DistributedLock*` | What the engine composes |
| `RedisDistributedLockHealthCheck` | `src/Encina.DistributedLock.Redis/Health/` | Health-check pattern |
| Multi-lock one-deadline pattern | #219 | Time budget of one call |
| Decorators | #741, #724, #223 | Forwarding surface |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Provider | Queue store | Status | Proven by |
|----------|-------------|--------|-----------|
| InMemory | per-resource sorted list, `Interlocked` sequence | exists | PR 2 unit, PR 11 contract |
| Redis | sorted set, counter key, lease keys, Lua head check | exists | PR 3 integration |
| SQL Server | opt-in `LockQueue` table, `IDENTITY` (expiry ignored until #2405) | exists | PR 4 integration |
| PostgreSQL | table, `bigserial`, `LIMIT 1` | planned (#207) | PR 5 |
| MySQL | table, `AUTO_INCREMENT`, backtick identifiers | planned (#208) | PR 6 |
| Azure Blob | ticket blobs plus counter blob (Azurite) | planned (#209) | PR 7 |
| DynamoDB | sort-key tickets plus counter item (LocalStack) | planned (#210) | PR 8 |

### Event ID Allocation

| Package | Range | Ids |
|---------|-------|-----|
| `Encina.DistributedLock` (`DistributedLockPipeline`, registered by #220 PR 4; first free 50-slot after 30599, no claim on main, in an open plan head or in the registry, checked 2026-11-27) | `30600-30649` | #225 uses 30617-30622; one registry row with identical bounds; no id below 10000 |

### Estimated File Count

| PR | Content | New | Modified | Tests, manifests, PublicAPI, docs | Total | Lines (est.) |
|----|---------|-----|----------|------------------------------------|-------|--------------|
| 1 | Contracts, ordering, engine | 8 | 1 (PublicAPI) | 5 (engine tests, ordering property tests, options tests, guard test, manifest) | 14 | 1,400 |
| 2 | InMemory store, registration | 1 | 3 (core and InMemory `ServiceCollectionExtensions.cs`, README) | 5 (store tests, DI test, guard test, manifest, fragment) + PublicAPI | 10 | 800 |
| 3 | Redis store | 1 | 2 (`ServiceCollectionExtensions.cs`, PublicAPI) | 4 (unit, guard, integration, manifest) (+2 Redis fixture if first) | 7 (9) | 700 |
| 4 | SQL Server store | 2 (store, schema) | 3 (options, `ServiceCollectionExtensions.cs`, PublicAPI) | 4 (unit, guard, integration, manifest) | 9 | 700 |
| 5 to 8 | One PR per planned provider | 2 | 3 | 4 | about 9 each | about 550 each |
| 9 | Decorator forwarding | 0 | 3 (the three decorators) | 5 (tests, PublicAPI, manifests) | 8 | 500 |
| 10 | Log, metrics, health (+4 if first of the family) | 3 | 2 | 6 (3 test files, manifest, 2 justification `.md`) | 11 (15) | 900 |
| 11 | Contract, property | 2 contract + 1 property | 1 (ContractTests csproj) | 0 | 4 | 500 |

Total about 6,700 lines over 11 PRs. Every PR is at most 15 files and 1,500 lines and builds and passes alone (PRs 5 to 8 wait for their packages; PR 9 waits for #741, #724 and #223).

### Per-file coverage targets (numeric, per flag)

| File | Unit | Guard | Contract | Integration | Justification |
|------|------|-------|----------|-------------|---------------|
| `FairDistributedLockProvider.cs` | 95 | 25 | 60 | 30 | Every branch against a test store and a fake clock; guards cover the constructor; contract and integration run real stores |
| `LockQueueOrdering.cs` | 95 | 0 | 0 | 0 | Pure logic with property tests; internal type with no `InternalsVisibleTo` for `Encina.GuardTests` |
| `FairLockOptionsValidator.cs`, `FairLockOptions.cs`, `LockQueueOptions.cs` | 95 | 40 | 0 | 0 | Pure validation |
| `InMemoryLockQueueStore.cs` | 95 | 20 | 60 | 0 | Every branch on a fake clock |
| `RedisLockQueueStore.cs`, `SqlServerLockQueueStore.cs`, `SqlServerLockQueueSchema.cs` | 85 | 15 | 0 | 70 | Mocked `IDatabase` and SQL text in unit; the real server in the shared fixtures |
| `FairLockMetrics.cs`, `FairLockHealthCheck.cs` | 85 | 20 | 0 | 0 | `MeterListener` and health-check tests; the health check has constructor guards |
| `IFairDistributedLockProvider.cs`, `ILockQueueStore.cs`, `LockQueueErrorCodes.cs`, `FairLockLog.cs` | 0 | 0 | 0 | 0 | Interfaces, constants and source-generated log methods: no executable logic; covered through the engine |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is pre-1.0 (.NET 10, C# 14, nullable enabled). Rules are in AGENTS.md. Issue #225 adds a fair FIFO queue in front of any lock provider
by composition (one engine plus an ILockQueueStore per backend), for all seven lock providers.

IMPLEMENTATION OVERVIEW:
1. Phase 1: IFairDistributedLockProvider, options, ordering, ILockQueueStore, the engine. Phase 2: InMemory store and registration.
2. Phase 3: Redis and SQL Server stores. Phase 4: the four planned providers' stores. Phase 5: decorator forwarding.
3. Phase 6: log 30617-30622, metrics, health check. Phase 7: contract, property and integration tests.

KEY PATTERNS:
- ADR-052 shapes; ticket order from a store-side sequence; ticket leases against abandoned waiters; no blocking commands on the shared Redis multiplexer.
- The engine sits at the bottom of the decorator chain so the queue key is the scoped lock name.
- Dilemmas 1, 2, 3 and 6 are pending the maintainer: implement the recommended options only after he answers.
- SQL Server ignores lock expiry until #2405: state it, never claim expiry-based behavior there.

REFERENCE FILES:
- docs/plans/lock-fair-queue-implementation-plan-225.md, docs/plans/lock-multi-resource-implementation-plan-219.md, src/Encina.DistributedLock*/
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Locks are ephemeral coordination primitives |
| 2 | OpenTelemetry | ✅ Include | Phase 6: request counter, wait histogram, waiters gauge; no resource names; tenant tag only with multi-tenancy (#2676) |
| 3 | Structured Logging | ✅ Include | Phase 6: `FairLockLog` 30617-30622, error codes and exception types only |
| 4 | Health Checks | ✅ Include | Phase 6: `FairLockHealthCheck` (store reachability, wait threshold) |
| 5 | Validation | ✅ Include | `LockQueueOptions` and `FairLockOptions` validated; invalid options are `Left` or start-up failure per their kind |
| 6 | Resilience | ❌ N/A | Store faults are returned as `Left`; a retry loop around a queue would hold a ticket while retrying; the waiter's `wait` is the budget |
| 7 | Distributed Locks | ❌ N/A | This is a lock feature, not a consumer |
| 8 | Transactions | ❌ N/A | Locks are separate from transactions |
| 9 | Idempotency | ❌ N/A | A repeated enqueue by one `WaiterId` replaces its own ticket |
| 10 | Multi-Tenancy | ✅ Include | Design Choice 2: the queue key is the scoped lock name through the #741 decorator; with option B a missing tenant fails closed (`RequireTenantContext ?? tenant provider registered`) and no `default` segment is used |
| 11 | Module Isolation | ❌ N/A | Modules are separated with `KeyPrefix` (the position of the #741 plan) |
| 12 | Audit Trail | ❌ N/A | Infrastructure coordination with no personal or regulated data; the issue's "audit lock queuing for fairness compliance" has no regulatory source (no SPEC-002 requirement names it) |

---

## Prerequisites & Dependencies

| Item | State | Needed for |
|------|-------|------------|
| #2667 ADR-052 implementation | open | All PRs |
| #207, #208, #209, #210 packages | plans on main, not written | PRs 5 to 8 |
| #741, #724, #223 decorators | plans on main | PR 9 |
| Family EventId range | registered by #220 PR 4 or the first family PR with a log | PR 10 |
| #2405 SQL Server expiry | open bug | Any SQL Server expiry statement |
| #2653 duplicate caching lock providers | open | The engine wraps whichever provider is registered; the two caching providers must follow ADR-052 |

---

## Next Steps

1. After #2667 merges open PR 1, then PRs 2, 3, 4; PRs 5 to 8 as each package merges; PR 9 after the decorators; PR 10 after PR 2; PR 11 after PR 2.
2. The maintainer answers Dilemmas 1, 2, 3 and 6 when work on the issue starts; PRs are written for the recommended answers.
