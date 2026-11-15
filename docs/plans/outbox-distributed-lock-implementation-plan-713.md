# Implementation Plan: Distributed Lock Around the Outbox Processing Cycle (and the Shared Messaging Lock Contract)

> **Issue**: [#713](https://github.com/dlrivada/Encina/issues/713)
> **Type**: Feature
> **Complexity**: High (5 pull requests; this plan owns the contract the other messaging lock plans reuse)
> **Estimated Scope**: ~650-800 lines of production code + ~1,700-2,000 lines of tests
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027 (lock providers); [ADR-050](../architecture/adr/050-modules-may-require-modules.md) (the Outbox row allows "row claiming or a lock"); [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md) (lock failure contract); [ADR-054](../architecture/adr/054-observability-conventions.md) D4 (tenant tag); [ADR-062](../architecture/adr/062-error-category-contract.md) (error category)
> **Depends on**: #2667 (implements ADR-052; open, not a pending maintainer decision)
> **Related**: #714 (inbox), #715 (saga), #716 (scheduled messages) reuse the contract of this plan; #717 (leader election), #1251 (row claiming), #741 (tenant-scoped locks), #2382 (requirements validator), #218 (fencing), #223 (auto-extend), #2396 (processor health)

---

## Summary

Eight provider packages ship an `OutboxProcessor` that derives from `OutboxProcessorBase`, and every replica of an application runs one. Two replicas that poll at the same time read the same pending rows and publish them twice. This plan adds an **opt-in lock around each processing cycle** (the issue's "single writer" mode): a replica processes a batch only while it holds `encina:outbox:process`, so one replica at a time polls, publishes and saves the outcomes.

1. **Port.** `Encina.DistributedLock` references `Encina.Messaging`, so `Encina.Messaging` cannot name `IDistributedLockProvider`. A narrow port `IMessagingLockProvider` (with `IMessagingLockHandle`) lives in `Encina.Messaging`; the adapter over `IDistributedLockProvider` lives in `Encina.DistributedLock`. The port, its options, names, gate, flag and diagnostics are the **shared piece** of the family: this plan owns them and the plans #714, #715 and #716 reuse them.
2. **Cycle gate.** `OutboxProcessorBase.ProcessPendingMessagesAsync` acquires the lock after it creates the cycle scope and releases it after `IOutboxStore.SaveChangesAsync` returned (or after the empty-batch return), so the lock handle outlives the save call. The eight provider processors are not edited, because the base class is sealed around the loop.
3. **Outcomes.** Contention (`Right(None)`, ADR-052) skips the cycle and is counted, not logged as an error. A lock `Left` fails the cycle visibly (logged by error code, counted, retried at the next interval) and never falls back to processing without the lock (`AGENTS.md` section 3).
4. **Opt-in and enforcement.** `config.UseDistributedLocking = true` in `MessagingConfiguration` (one flag for the four messaging patterns, Dilemma 3); `AddEncinaMessagingDistributedLocking()` in `Encina.DistributedLock` registers the adapter; a startup check fails the host when the flag is on and no `IMessagingLockProvider` is registered (ADR-050 rule 3).
5. **What it does not claim.** The lock is a lease, not a fence: a replica that stalls beyond `OutboxOptions.LockExpiry` can still finish its batch while another replica starts (fencing is #218, row claiming is #1251). Delivery stays at-least-once.

**Standards**: ADR-052 (contention is `Right(None)`, a fault is `Left`); ADR-050 (the lock is a listed alternative for the Outbox row, enforced by startup validation); ADR-062 (error category of new codes: this plan adds none, a provider `Left` is returned unchanged); `AGENTS.md` sections 3 and 6.

**Estimated scope**: 11 new production files, 6 modified, 3 PublicAPI files, 2 coverage manifests (plus `Encina.json`), 16 test files; 5 pull requests (see Estimated File Count).

**Affected packages**: `Encina.Messaging` (port, options, gate, flag, processor base), `Encina.DistributedLock` (adapter, registration), `Encina` (EventId range entry only). The eight `*.OutboxProcessor` classes are not edited.

**Provider category**: Distributed locks as the engine (the adapter works over any `IDistributedLockProvider`; InMemory, Redis and SqlServer exist, PostgreSQL #207 and MySQL #208 are pending, Azure Blob #209 and DynamoDB #210 are not built; SPEC-000 DEC-009 lists exactly these seven); Database (10) as consumers of the one shared base class (one integration class per provider proves the adoption: ADO.NET and Dapper on SqlServer, PostgreSQL and MySQL, EF Core on the same three, and MongoDB).

**What the code does today** (`origin/main` at `db1e7f688`):

- `OutboxProcessorBase : BackgroundService` (`src/Encina.Messaging/Outbox/OutboxProcessorBase.cs`) has a sealed `ExecuteAsync` loop (`ProcessPendingMessagesAsync` then `Task.Delay(ProcessingInterval)`) and a cycle that creates a scope, resolves the store, `IEncina` and the serializer, runs `OutboxBatchProcessor`, then `store.SaveChangesAsync(CancellationToken.None)`. It takes `(IServiceProvider, ILogger, OutboxOptions, TimeProvider?)`; the eight providers derive from it (`src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/Outbox/OutboxProcessor.cs`, `src/Encina.Dapper.{...}/Outbox/OutboxProcessor.cs`, `src/Encina.EntityFrameworkCore/Outbox/OutboxProcessor.cs`, `src/Encina.MongoDB/Outbox/OutboxProcessor.cs`).
- No production code in `src/` consumes `IDistributedLockProvider` (ADR-052, Context table); the interface is pre-ADR-052 (`Task<IAsyncDisposable?>`), and `ILockHandle` has `DateTime ExpiresAtUtc` and `ExtendAsync` (`src/Encina.DistributedLock/ILockHandle.cs`). `ScheduledMessageProcessor` records the gap in its remarks ("See Issue #716").
- `Encina.DistributedLock.csproj` references `Encina` and `Encina.Messaging`, so the adapter cannot live in `Encina.Messaging` (project cycle).
- `MessagingConfiguration` has opt-in flags in the `UseOutbox` style (`src/Encina.Messaging/MessagingConfiguration.cs`); `DelayedRetryStartupCheck` (`src/Encina.Messaging/Recoverability/DelayedRetryStartupCheck.cs`) is the existing model of a startup check.
- EventIds: `Messaging = (2800, 2999)` and `MessagingOutbox = (2000, 2099)` exist; the block `7500-7549` is assigned to this family in the plan-batch registry and has no entry in `src/Encina/Diagnostics/EventIdRanges.cs` yet. `EncinaEventIdAllocationTests` maps `Encina.Messaging` to six ranges.
- The issue text says "All 13 database providers": the database matrix of `AGENTS.md` section 5 has 10 (Oracle and SQLite are out of it).
- Manifests: `.github/coverage-manifest/Encina.Messaging.json` (targets contract 15, guard 20, property 15, unit 70; `OutboxProcessorBase.cs` unit 85, guard 10, integration 70) and `Encina.DistributedLock.json` (guard 20, unit 60).

---

## Design Choices

<details>
<summary><strong>1. Where the abstraction lives: a narrow port in Encina.Messaging, or the lock interface in a shared core</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IMessagingLockProvider` port in `Encina.Messaging`, adapter in `Encina.DistributedLock`** | No project cycle; Messaging stays free of the lock package (pay-for-what-you-use); the port returns only what messaging needs (`Right(Some(handle))`, `Right(None)`, `Left`) | One more small interface and an adapter; two handle types |
| **B) Move the `IDistributedLockProvider` abstractions into `Encina` (core)** and let Messaging use them directly | One lock abstraction for every module | Moves a public abstraction that #2667, #741, #717, #226 and #763 are all editing now; widens core |

### Chosen Option: **A — a narrow port with an adapter** (recommended, pending the maintainer)

### Rationale

- A wins while messaging is the only consumer that cannot reference `Encina.DistributedLock` (the family plans #717 and #763 take the same split: abstraction in Messaging, implementation in `Encina.DistributedLock`); B wins if several core modules, such as caching stampede protection or the Snowflake lease (#2394), also need lock access without referencing the lock package.
- The port is small enough to move later: its three members map one to one onto ADR-052's `TryAcquireAsync` and `ILockHandle`.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. What the lock protects: the whole cycle, one batch id, or nothing beyond leader election and row claiming</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One lock per processing cycle, `encina:outbox:process` (the issue's `process:leader` shape), wait 0: a replica that finds it held skips the cycle** | Exactly one replica polls, publishes and saves at a time; a single acquire per cycle; works on every provider with no schema change | Serialises all outbox throughput on one replica at a time; the lock is a lease, so a stalled holder is not fenced |
| **B) A lock per batch, `encina:outbox:process:{batch-id}`** | Looks finer grained | The batch id is unknown before the query, so the lock would be taken after the rows are read and protects nothing; it needs row claiming (#1251) to mean anything |
| **C) No lock: rely on leader election (#717) and row claiming (#1251) only** | No second mechanism; row claiming allows parallel replicas | The issue is not delivered; ADR-050's Outbox row accepts "row claiming or a lock", and #1251 itself keeps the lock "for single-writer setups" |

### Chosen Option: **A — one lock per cycle** (recommended, pending the maintainer)

### Rationale

- A wins for a deployment with a few replicas that wants a safety net without schema changes, and as the single-writer mode that #1251 keeps; C wins at high replica counts where parallel claiming matters more than simplicity; the plan of #717 (Dilemma 2) asks the same question for election, so the maintainer's answer there decides whether the lock is the safety net behind election (both on, election checked first because it does no I/O) or a standalone mode.
- B is invalid on its own terms (the id does not exist before the read).
- The code is identical under A and C except that the adoption in Phase 4 is not built under C; the shared contract of Phases 1 to 3 is still needed by #714, #715 and #716.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. One flag for the four patterns, or one flag per pattern</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One flag, `config.UseDistributedLocking`, for outbox, inbox, saga and scheduling** | Matches the text of #713 and #714 ("shared with outbox"); one registration and one startup check | An application cannot lock only the sagas |
| **B) One flag per pattern (`UseOutboxLocking`, `UseInboxLocking`, ...)** | Each pattern opts in alone | Four flags and four startup checks; diverges from the issues' wording |

### Chosen Option: **A — one flag** (recommended, pending the maintainer)

### Rationale

- A wins when an application wants the safety net everywhere (the stated purpose, multi-instance deployments); B wins when only one pattern needs serialising, for example sagas with long steps, while a high-throughput outbox must not be serialised.
- Each pattern still adopts independently: the flag is read by each adopter, so the flag can land in PR 1 and the adopters in later pull requests, each a no-op until its code lands.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Lock lifetime: a fixed expiry per cycle, or renewal while the cycle runs</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A fixed `OutboxOptions.LockExpiry` (default 5 minutes, validated greater than `ProcessingInterval`); the gate logs EventId 7503 when the handle's `ExpiresAtUtc` has passed before the cycle ends** | No renewal loop; no dependency on #223 | A cycle longer than the expiry loses the lock while it runs |
| **B) Renew with `IMessagingLockHandle.ExtendAsync` during the cycle** | A long batch keeps its lock | A renewal task per cycle; a renewal failure must stop the batch, which `OutboxBatchProcessor` cannot do today; overlaps #223 (auto-extend) |

### Chosen Option: **A — fixed expiry, loss made visible** (recommended, pending the maintainer)

### Rationale

- A wins while a batch (default `BatchSize`) finishes well inside the expiry, the normal case; B wins for very slow transports, and then #223 provides renewal for every consumer instead of this plan adding one.
- The handle exposes `ExtendAsync` already (the port mirrors ADR-052's `ILockHandle`), so adopting #223 later does not change the contract.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Failure semantics of the gate</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Contention (`Right(None)`) skips the cycle and is counted; a lock `Left` fails the cycle visibly (error code logged, counter, retried next interval); the lock is never bypassed** | Follows ADR-052 and the fail-closed rule; the error is neither swallowed nor treated as success | A broken lock backend stops outbox delivery until it recovers |
| **B) On a lock `Left`, process without the lock** | Delivery continues | Duplicates exactly when the coordination is unhealthy; violates the fail-closed rule |
| **C) Treat contention as an error** | Simple | ADR-052 rejected this: the most common outcome would be logged as a failure |

### Chosen Option: **A — skip on contention, fail visibly on a fault**

### Rationale

- The gate returns an `Either`; `OutboxProcessorBase` logs the code (never `EncinaError.Message`, `AGENTS.md` section 3) and returns, the same way it already handles a `Left` from fetching pending messages.
- Cancellation is not an error: `OperationCanceledException` propagates, as `EitherHelpers.TryAsync` does (ADR-052).

**Class:** Settled
**Settled by:** ADR-052 (contention is `Right(None)`, a fault is `Left`, cancellation propagates), `AGENTS.md` section 3 (errors are never swallowed in background infrastructure; gates fail closed)

</details>

<details>
<summary><strong>6. Lock names and tenancy</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fixed global names built in one place (`MessagingLockNames`): `encina:outbox:process`, `encina:scheduled:process`, `encina:inbox:{hash}`, `encina:saga:{id}`; no tenant segment of its own** | One definition for the four plans; the processors are cross-tenant by nature; an application that lists a name in the tenant scoping of #741 gets `@{tenant}:{name}` from that decorator, whose rules (a tenant `default` is an ordinary tenant, no clash with global names) stay in one place | The outbox cycle is global even when rows are tenant-scoped |
| **B) Build the tenant segment here** | Per-tenant outbox cycles | Duplicates #741's escaping and fail-closed rule; a second place to keep injective |

### Chosen Option: **A — global names from one class, tenant scoping left to #741**

### Rationale

- The outbox processor reads rows of every tenant in one query, so there is no tenant at cycle level to scope by; emitting a tenant tag would invent one (ADR-054 D4 applies where a tenant exists: the inbox and saga plans tag spans and metrics with it when multi-tenancy is on).
- Names are literals, so two different names never map to one lock; the only parameterised ones (#714, #715) are made injective in their plans.

**Class:** Trivial
**Settled by:** ADR-054 D4 (the tenant is a tag where a tenant exists), the lock naming of `encina:dpia:review-reminder` (#763) and the `encina:leader:*` names of #717 (one `encina:` namespace), #741 (tenant derivation is the decorator's job)

</details>

<details>
<summary><strong>7. Enforcement: flag, explicit registration, startup check</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The flag plus `AddEncinaMessagingDistributedLocking()`; a startup check fails when the flag is on and no `IMessagingLockProvider` is registered; the gate fails closed when no port resolves** | A missing dependency is a startup error in any registration order | Two calls to make |
| **B) The flag auto-registers an in-memory lock** | Nothing to forget | An in-memory lock on several replicas locks nothing, silently |

### Chosen Option: **A — flag, registration, startup check**

### Rationale

- The check runs at host start, so the order of `AddEncinaMessaging`, the adapter registration and the provider registration does not matter; a DI test with `ValidateOnBuild` and `ValidateScopes` covers flag on and off in both orders.
- Rejecting an in-memory provider on several replicas is #2382's requirement, not duplicated here.

**Class:** Settled
**Settled by:** ADR-050 (rule 3: a requirement is enforced by registration or startup validation, never a silent default), `AGENTS.md` section 3 (registration completeness, gates fail closed)

</details>

---

## Implementation Phases

### Phase 1: The Port, Options and Names (Encina.Messaging) — PR 1

> **Goal**: The contract and the switch, with no behavior change.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.Messaging/Locking/` (new folder)

1. **`IMessagingLockProvider.cs`** — `ValueTask<Either<EncinaError, Option<IMessagingLockHandle>>> TryAcquireAsync(string lockName, TimeSpan expiry, TimeSpan wait, CancellationToken cancellationToken)`. `Right(Some)` acquired, `Right(None)` held elsewhere or wait elapsed, `Left` fault; cancellation propagates.
2. **`IMessagingLockHandle.cs`** — `IAsyncDisposable` with `DateTime ExpiresAtUtc { get; }` (UTC, the type `ILockHandle` uses) and `ValueTask<Either<EncinaError, bool>> ExtendAsync(TimeSpan extension, CancellationToken cancellationToken)`.
3. **`MessagingLockOptions.cs`** — `TimeSpan RetryInterval` (default 200 ms, positive): the interval the adapter passes to `TryAcquireAsync` while waiting.
4. **`MessagingLockOptionsValidator.cs`** — `IValidateOptions<MessagingLockOptions>`: `RetryInterval` positive.
5. **`MessagingLockNames.cs`** — `const string OutboxProcess = "encina:outbox:process"`, `ScheduledProcess = "encina:scheduled:process"` (used by #716), and the factories for the parameterised names that #714 and #715 add to this class in their own pull requests.

#### Existing files

6. **`MessagingConfiguration.cs`** (modify) — `bool UseDistributedLocking { get; set; }`, opt-in, XML docs naming the registration and the startup check.
7. **`Outbox/OutboxOptions.cs`** (modify) — `TimeSpan LockExpiry` (5 minutes) and `TimeSpan LockWait` (zero) with the setter checks of the file's style; `Validate` rejects a `LockExpiry` that is not greater than `ProcessingInterval` and a `LockWait` greater than `LockExpiry`.
8. **`PublicAPI.Unshipped.txt`** (Messaging; through `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #713: the messaging lock port, options and names in Encina.Messaging.

CONTEXT:
- Encina.DistributedLock references Encina.Messaging, so the port lives here and the adapter in Phase 3 lives there.
- ADR-052 fixes the shapes: TryAcquireAsync returns Either<EncinaError, Option<handle>>; contention is Right(None).
- This is the shared piece of plans #713, #714, #715 and #716: no behavior change in this pull request.

TASK:
Create IMessagingLockProvider, IMessagingLockHandle, MessagingLockOptions (+ validator), MessagingLockNames, the
UseDistributedLocking flag and OutboxOptions.LockExpiry / LockWait with their validation, as listed in the Phase 1
tasks. Write unit tests (options and validator, names), guard tests for the options setters, and a property test that
the parameterised names stay injective once #714 and #715 add them (for this pull request: the constants are distinct).

KEY RULES:
- No exceptions for business outcomes: the port returns Either.
- XML docs on every public member; ExpiresAtUtc is UTC.
- Public symbols go to PublicAPI.Unshipped.txt through mechanical-fixer.

REFERENCE FILES: src/Encina.DistributedLock/ILockHandle.cs, src/Encina.Messaging/Outbox/OutboxOptions.cs,
src/Encina.Messaging/MessagingConfiguration.cs, docs/architecture/adr/052-distributed-lock-failure-contract.md.
```

</details>

### Phase 2: The Gate, Diagnostics and the EventId Range (Observability) — PR 2

> **Goal**: One acquire helper with logs and metrics, and the registered EventId range.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.Messaging/Locking/`

1. **`MessagingLockGate.cs`** (internal static) — `ValueTask<Either<EncinaError, Option<IMessagingLockHandle>>> TryEnterAsync(IMessagingLockProvider? provider, MessagingLockMarker? marker, string pattern, string lockName, TimeSpan expiry, TimeSpan wait, ILogger logger, CancellationToken ct)`: no marker means the feature is off and the result is a no-op handle (`Right(Some(NoOpHandle))`); a marker with no provider is `Left` `messaging.lock_provider_missing` (category `FailedPrecondition`, ADR-062; the startup check normally prevents it); otherwise it calls the provider, records the outcome (acquired, contended, failed) and logs by error code. `NoOpHandle` is a private nested type.
2. **`MessagingLockMarker.cs`** (internal sealed) — registered exactly when `UseDistributedLocking` is on (Phase 3); the processors learn the flag from its presence, because they receive options and a service provider, not `MessagingConfiguration`.
3. **`MessagingLockLog.cs`** (internal static partial) — `[LoggerMessage]` 7500 `LockAcquireFailed` (Error: pattern, error code), 7501 `LockNotAcquired` (Debug: pattern), 7502 `LockReleaseFailed` (Warning: pattern, exception type via `ForLogging()`), 7503 `LockExpiredBeforeCompletion` (Warning: pattern).
4. **`Diagnostics/MessagingLockMetrics.cs`** — on the shared `Meter("Encina")`: `Counter<long> encina.messaging.lock.attempts_total` with tags `pattern` (outbox, inbox, saga, scheduling; bounded) and `outcome` (acquired, contended, failed). No tenant tag at cycle level (Design Choice 6); #714 and #715 add the tenant tag where a tenant exists.

#### `src/Encina`

5. **`Diagnostics/EventIdRanges.cs`** (modify) — `public static readonly (int Min, int Max) MessagingLocking = (7500, 7549);` before any message exists (ADR-021), with `Encina.Messaging` mapped to it in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (modify).
6. **`PublicAPI.Unshipped.txt`** of `Encina` (the field) and of Messaging, through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #713: the lock gate, its logs and metrics, and the EventId range.

CONTEXT:
- Phase 1 created the port, options and names. EventIds are registered first (ADR-021) in EventIdRanges.cs; the block
  7500-7549 is assigned to this family and ids are packed from 7500: 7500-7503 are this plan's, 7504 belongs to #714
  and 7505 to #715.
- AGENTS.md section 3: EncinaError.Message never reaches logs or tags; log the code or the exception type.

TASK:
Add the range MessagingLocking = (7500, 7549), map it for Encina.Messaging in EncinaEventIdAllocationTests, create
MessagingLockGate, MessagingLockMarker, MessagingLockLog and MessagingLockMetrics as listed, and unit-test every
outcome with an NSubstitute port and a FakeTimeProvider: off, provider missing (Left with the FailedPrecondition
category), acquired, contended, failed, cancelled.

KEY RULES:
- Contention is not an error: it is counted and logged at Debug only.
- Tags are bounded (pattern, outcome); no free text, no lock names with ids.

REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs,
src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs, src/Encina.Messaging/Diagnostics/SchedulingProcessorLog.cs.
```

</details>

### Phase 3: The Adapter, Registration and Startup Validation — PR 3 (first half)

> **Goal**: `IDistributedLockProvider` behind the port, and a startup error when the flag has nothing to run on.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.DistributedLock`

1. **`Messaging/DistributedLockMessagingLockProvider.cs`** (internal sealed) — implements `IMessagingLockProvider` over `IDistributedLockProvider` with the ADR-052 shapes: `Right(Some(ILockHandle))` becomes `Right(Some(wrapper))`, `Right(None)` stays `None`, a `Left` is returned unchanged. The private wrapper implements `IMessagingLockHandle` by delegating `ExpiresAtUtc`, `ExtendAsync` and `DisposeAsync`; a dispose exception is caught and logged with EventId 7502 by exception type and never rethrown from a `finally` path.
2. **`ServiceCollectionExtensions.cs`** (modify) — `AddEncinaMessagingDistributedLocking(this IServiceCollection services)`: `TryAddSingleton<IMessagingLockProvider, DistributedLockMessagingLockProvider>()`, `AddOptions<MessagingLockOptions>()` with the validator.

#### `src/Encina.Messaging`

3. **`Locking/MessagingLockStartupCheck.cs`** (internal sealed, `IHostedService`, modelled on `DelayedRetryStartupCheck`) — throws `InvalidOperationException` in `StartAsync` naming the module, the requirement, ADR-050 and the fix when the marker exists and no `IMessagingLockProvider` is registered.
4. **`MessagingServiceCollectionExtensions.cs`** (modify) — when `UseDistributedLocking` is on: register `MessagingLockMarker` and the startup check; it does not register `MessagingLockOptions` (the single place is the adapter registration, so options are registered once).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #713: the adapter, its registration and the startup check.

CONTEXT:
- #2667 (ADR-052) changes IDistributedLockProvider to return Either<EncinaError, Option<ILockHandle>>; this phase is
  written against that contract and starts after #2667 merges.
- Satellite registrations use TryAdd* and must work in any registration order (AGENTS.md section 3).

TASK:
Create DistributedLockMessagingLockProvider (internal), AddEncinaMessagingDistributedLocking, MessagingLockStartupCheck
and the flag-driven registration of the marker and check. Write unit tests with a substituted IDistributedLockProvider
(acquired, contended, Left unchanged, dispose failure logged by type), a DI test building the provider with
ValidateOnBuild and ValidateScopes for flag on and off in both registration orders, a contract test running the
same scenarios through the InMemory, Redis and SqlServer providers, and guard tests for the public extension.

KEY RULES:
- Never inject a scoped service into a singleton; the adapter and the options are singletons.
- Return the provider's Left unchanged: no new error code, no message copied.

REFERENCE FILES: src/Encina.DistributedLock/ServiceCollectionExtensions.cs, src/Encina.Messaging/Recoverability/DelayedRetryStartupCheck.cs,
src/Encina.DistributedLock.InMemory/ServiceCollectionExtensions.cs.
```

</details>

### Phase 4: Cross-Cutting Integration and Adoption in the Outbox Processor — PR 3 (second half)

> **Goal**: `OutboxProcessorBase` runs a cycle only while it holds the lock.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Outbox/OutboxProcessorBase.cs`** (modify) — in `ProcessPendingMessagesAsync`, after the scope is created: resolve `IMessagingLockProvider?` and `MessagingLockMarker?` from the cycle scope's services (never from the singleton constructor), call `MessagingLockGate.TryEnterAsync(..., "outbox", MessagingLockNames.OutboxProcess, _options.LockExpiry, _options.LockWait, ...)`. `Left`: log by code and return. `None`: return. `Some(handle)`: `await using` it around the existing body, including the `SaveChangesAsync` call, so the handle is disposed after the save returned; before the final return, when `handle.ExpiresAtUtc` is earlier than `TimeProvider.GetUtcNow()`, log 7503. The constructor and the eight provider subclasses do not change.
2. **Order with leader election.** When #717 is merged and its flag is on, its check runs before the lock (it does no I/O); this plan adds no code for it.
3. **Cross-cutting evaluation** (matrix): caching, resilience around the lock call and audit are not applicable; health and idempotency are deferred (#2396 owns processor liveness; the lock adds no new checkable dependency of its own).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #713: the lock around the outbox processing cycle.

CONTEXT:
- OutboxProcessorBase is sealed around the loop and creates one scope per cycle; the eight provider processors derive
  from it and must not change. The cycle saves its outcomes with IOutboxStore.SaveChangesAsync after the batch.
- The lock handle must be disposed after the save returns, so outcomes are persisted before another replica starts a
  cycle. Do not describe this as atomic or transactional: the lock is a lease.

TASK:
Gate ProcessPendingMessagesAsync as listed in the Phase 4 tasks. Unit tests in OutboxProcessorBaseLockTests with a
fake store, a substituted port and a FakeTimeProvider: flag off (no port call), acquired (batch runs, handle disposed
after SaveChangesAsync), contended (no store call), Left (no store call, error code logged, cycle counted failed),
expired handle (7503), cancellation. A test that a throwing batch still disposes the handle.

KEY RULES:
- Fail closed: a Left never leads to processing without the lock.
- Resolve the port from the cycle scope; log codes, not messages.

REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxProcessorBase.cs, tests/Encina.UnitTests/Messaging/Outbox/OutboxProcessorBaseTests.cs.
```

</details>

### Phase 5: Testing — PRs 3, 4 and 5

> **Goal**: Every flag reaches its targets and every provider proves the adoption.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit and guard** (in PRs 1 to 3): the files named in the earlier phases.
2. **Contract** (PR 3): `MessagingLockProviderContractTests` runs the same scenarios against the adapter over InMemory, Redis (Testcontainers fixture, `RedisFixture`) and SqlServer.
3. **Property** (PR 1): lock names are distinct constants; the parameterised names of #714 and #715 add their injectivity property to the same class.
4. **Integration** (PRs 4 and 5): an abstract `OutboxProcessorLockIntegrationTestsBase` under `tests/Encina.IntegrationTests/Messaging/` starts two processor instances of one provider over one database and one Redis lock and asserts that each pending message is published exactly once while both run; one concrete class per provider, each in its own `[Collection("<Family>-<Database>")]`: PR 4 has ADO and Dapper on SqlServer, PostgreSQL and MySQL (six classes), PR 5 has EF Core on the same three (three classes) and MongoDB. Provider rule of `AGENTS.md` section 9: the ten providers are covered.
5. **Load and benchmark**: justified in `.md` files (the lock adds one acquire per cycle, not a hot path).
6. **Per-file targets** (numeric, per flag; justification in one sentence each):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|-----:|------:|---------:|---------:|------------:|---------------|
| `Locking/MessagingLockGate.cs` | 95 | 0 | 0 | 0 | 0 | Pure branches covered by unit tests; internal type with no `InternalsVisibleTo` for `Encina.GuardTests` |
| `Locking/MessagingLockMarker.cs` | 90 | 0 | 0 | 0 | 0 | One empty type; reached through the gate and DI tests; internal |
| `Locking/MessagingLockLog.cs` | 60 | 0 | 0 | 0 | 0 | Source-generated; excluded from coverage by attribute, tested through the gate |
| `Locking/MessagingLockOptions.cs` and `MessagingLockOptionsValidator.cs` | 95 | 90 | 0 | 0 | 0 | Setter guards and validator rules are directly drivable |
| `Locking/MessagingLockNames.cs` | 95 | 0 | 0 | 80 | 0 | Constants and factories; the property test checks distinctness |
| `Locking/MessagingLockStartupCheck.cs` | 90 | 0 | 0 | 0 | 0 | Internal; DI tests with both registration orders |
| `Outbox/OutboxProcessorBase.cs` (changed lines) | 90 | 10 | 0 | 0 | 70 | Existing targets kept (unit 85, guard 10, integration 70); the lock paths add unit cases and the two-instance integration test |
| `Outbox/OutboxOptions.cs` | 95 | 40 | 0 | 0 | 0 | Existing targets kept; the two new properties add setter and `Validate` cases |
| `Messaging/DistributedLockMessagingLockProvider.cs` (DistributedLock) | 90 | 0 | 80 | 0 | 60 | Internal, guard 0 for that reason; the contract flag runs three real providers |
| `ServiceCollectionExtensions.cs` (DistributedLock) | 90 | 80 | 0 | 0 | 0 | Public extension: null guards; DI test builds the provider |

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #713: the tests that prove the lock across providers.

CONTEXT:
- Integration tests use shared [Collection("<Family>-<Database>")] fixtures, call ClearAllDataAsync in InitializeAsync
  and never create a per-class fixture. RedisFixture exists in tests/Encina.TestInfrastructure.
- Tests execute real package code (no reflection-only tests) and assert with Shouldly through Encina.Testing.Shouldly.

TASK:
Write the contract class over three lock providers, the abstract two-instance integration base and one concrete class per
provider (ten providers, see the Phase 5 tasks), and the .md justification files for load and benchmark tests. Check each
file against the per-file targets table and report the measured per-flag coverage.

KEY RULES:
- No Thread.Sleep; drive time with FakeTimeProvider where the code allows it, and with a bounded poll in integration tests.
- Never call the fixture's DisposeAsync from a test.

REFERENCE FILES: tests/Encina.IntegrationTests/Messaging, tests/Encina.TestInfrastructure/RedisFixture.cs,
tests/Encina.UnitTests/Messaging/Outbox/OutboxProcessorBaseTests.cs, docs/testing/integration-tests.md.
```

</details>

### Phase 6: Documentation and Finalization — PR 5

> **Goal**: Users can turn the lock on and know its limits.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on every new public member (the port, the handle, the options, `UseDistributedLocking`, `AddEncinaMessagingDistributedLocking`), with an example.
2. `changelog.d/713-outbox-distributed-lock.added.md`, through `mechanical-fixer`.
3. `src/Encina.Messaging/README.md` and `src/Encina.DistributedLock/README.md`: the flag, the registration, the single-writer limits, and the relation to #717 and #1251 (through `docs-writer`).
4. A page under `docs/features/` on messaging locks (shared with #714 to #716; owned by this plan), through `docs-writer`.
5. `ROADMAP.md` and `docs/INVENTORY.md`: only if the files list changed packages (no new package here).
6. ADR: none new; ADR-050 already lists the lock for the Outbox, Inbox and Scheduling rows.
7. Build and test verification: `dotnet build --configuration Release` with zero warnings; every flag reaches the targets above.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are finishing issue #713: documentation and verification.

CONTEXT:
- Documentation pages go through the docs-writer agent, the changelog fragment and PublicAPI lines through
  mechanical-fixer. Never type coverage figures by hand: cite cov: markers (SPEC-001).

TASK:
Complete the XML docs, request the fragment, the READMEs and the feature page, list every new public symbol in the
PublicAPI files, run the build and the tests, and report per-flag coverage against the Phase 5 targets.

KEY RULES:
- State the limits plainly: the lock is a lease without fencing; delivery stays at-least-once.
- Do not name a private client application; write "the reference application".

REFERENCE FILES: changelog.d/README.md, src/Encina.Messaging/README.md, docs/testing/coverage-measurement-methodology.md.
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Relevance |
|--------|-----------|
| ADR-052 | Shapes of the port: `Right(Some)`, `Right(None)`, `Left`; cancellation propagates |
| ADR-050 | The Outbox row lists "row claiming or a lock"; requirement enforced by startup validation |
| ADR-054 D4 | Tenant as span attribute and metric tag only where a tenant exists (not at cycle level) |
| ADR-062 | `ErrorCategory` of new codes (`messaging.lock_provider_missing` is `FailedPrecondition`) |
| SPEC-000 REQ-027 / DEC-009 | The seven lock providers the adapter must work with |
| Lease-based locks (Redis lock pattern; Kleppmann, "How to do distributed locking") | A lease is not a fence; the plan states the limit and defers fencing to #218 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `OutboxProcessorBase` | `src/Encina.Messaging/Outbox/OutboxProcessorBase.cs` | The single adoption point for eight provider processors |
| `IDistributedLockProvider`, `ILockHandle` | `src/Encina.DistributedLock/` | The engine behind the adapter |
| `DelayedRetryStartupCheck` | `src/Encina.Messaging/Recoverability/DelayedRetryStartupCheck.cs` | Model of the startup check |
| `OutboxProcessorMetrics`, `MessagingLog` | `src/Encina.Messaging/Diagnostics/` | Meter and log conventions to follow |
| `RedisFixture` | `tests/Encina.TestInfrastructure/RedisFixture.cs` | Real lock provider in contract and integration tests |
| Leader election (#717), row claiming (#1251) | plans | Sibling mechanisms; composition order stated in Phase 4 |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Lock provider | Status | Notes |
|---------------|--------|-------|
| InMemory | Exists | Single process only; rejecting it on several replicas is #2382 |
| Redis | Exists | Renewal must use the handle (ownership check, bug #2405) |
| SqlServer | Exists | `expiry` is ignored today (#2405); the lock lives with the connection |
| PostgreSQL (#207), MySQL (#208) | Pending | Covered by the contract test when they land |
| Azure Blob (#209), DynamoDB (#210) | Not built | Covered by the contract test when they land |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (new `MessagingLocking`) | 7500-7549 | Block assigned to this family; this plan registers the range in PR 2 and uses 7500-7503; #714 uses 7504, #715 uses 7505; #716 reuses 7500-7503; 7506-7539 stay unassigned (7540-7549 is the `MachineIdLease` range of #2394) |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production (new) | 11 | Port, handle, options, validator, names, marker, gate, log, metrics, startup check, adapter |
| Production (modified) | 6 | `MessagingConfiguration`, `OutboxOptions`, `OutboxProcessorBase`, `MessagingServiceCollectionExtensions`, `EventIdRanges`, `ServiceCollectionExtensions` (DistributedLock) |
| `PublicAPI.Unshipped.txt` | 3 | Messaging, Encina, DistributedLock |
| Coverage manifests | 3 | `Encina.Messaging.json`, `Encina.DistributedLock.json`, `Encina.json` |
| Tests | 22 | Unit (6), guard (2), contract (1), property (1), allocation map (1), integration base plus ten classes (11) |
| Documentation | 5 | Two READMEs, one feature page, one changelog fragment, two `.md` justifications |

PR split (each at most 15 files and 1500 lines, counting tests, `PublicAPI`, manifests, README and fragment; each builds and passes alone):

| PR | Content | Files | Lines (est.) |
|----|---------|------:|-------------:|
| 1 | Phase 1: port, options, names, flag, `OutboxOptions` fields, tests, `PublicAPI`, Messaging manifest | 13 | 650 |
| 2 | Phase 2: gate, marker, log, metrics, range, allocation map, tests, `PublicAPI` x2, manifests x2 | 13 | 850 |
| 3 | Phases 3 and 4: adapter, registration, startup check, processor gate, tests (unit, DI, contract, guard), `PublicAPI`, manifests; starts after #2667 and PR 2 | 14 | 1,200 |
| 4 | Phase 5 integration, SQL families: base, six ADO and Dapper classes, fixture helper | 8 | 900 |
| 5 | Phase 5 integration, EF Core x3 and MongoDB, plus Phase 6 (READMEs, feature page, fragment, justifications) | 11 | 800 |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases of issue #713</strong></summary>

```
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 / C# 14 library with Railway Oriented Programming (Either<EncinaError, T>). Issue #713 adds
an opt-in distributed lock around each outbox processing cycle and the shared messaging lock contract reused by #714,
#715 and #716. Read AGENTS.md sections 3, 5, 6, 7 and 9 and ADR-050, ADR-052, ADR-054, ADR-062 first.

IMPLEMENTATION OVERVIEW:
1. Encina.Messaging/Locking: IMessagingLockProvider, IMessagingLockHandle, options, names, marker, gate, logs (7500-7503),
   metrics; flag UseDistributedLocking; OutboxOptions.LockExpiry and LockWait.
2. EventIdRanges: MessagingLocking = (7500, 7549), mapped for Encina.Messaging.
3. Encina.DistributedLock: internal adapter over IDistributedLockProvider (ADR-052 shapes) and
   AddEncinaMessagingDistributedLocking; startup check in Messaging.
4. OutboxProcessorBase: acquire after the scope is created, dispose after SaveChangesAsync; contention skips, Left fails
   visibly, never processes without the lock.
5. Tests per file target; integration over the ten providers; docs through docs-writer; fragment through mechanical-fixer.

KEY PATTERNS:
- Fail closed; log codes and exception types only; contention is Right(None), not an error.
- Never inject a scoped service into a singleton; the processor resolves the port from its cycle scope.
- TimeProvider for every clock read; no Thread.Sleep; Shouldly through Encina.Testing.Shouldly.
- One pull request per phase group, at most 15 files and 1500 lines, each building alone.

REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxProcessorBase.cs, src/Encina.DistributedLock/, src/Encina.Messaging/MessagingConfiguration.cs,
src/Encina/Diagnostics/EventIdRanges.cs, docs/architecture/adr/052-distributed-lock-failure-contract.md, docs/plans/leader-election-implementation-plan-717.md.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Coordination state is not cacheable data |
| 2 | OpenTelemetry | ✅ | Phase 2: `encina.messaging.lock.attempts_total{pattern,outcome}`; no tenant tag at cycle level (ADR-054 D4 applies where a tenant exists) |
| 3 | Structured Logging | ✅ | Phase 2: `[LoggerMessage]` 7500-7503 in `MessagingLocking`, registered first |
| 4 | Health Checks | ⏭️ | Processor liveness is #2396; the lock adds no checkable dependency of its own (the lock provider has its own health check) |
| 5 | Validation | ✅ | Phase 1: options validator, `LockExpiry` greater than `ProcessingInterval`; Phase 3: startup check |
| 6 | Resilience | ❌ | No retry or breaker around the lock: a failed acquire is retried at the next interval by design |
| 7 | Distributed Locks | ✅ | The feature itself |
| 8 | Transactions | ❌ | The lock is not a transaction and the plan makes no atomicity claim; the processor's own save is unchanged |
| 9 | Idempotency | ⏭️ | Delivery stays at-least-once; consumers deduplicate (Inbox, #714) |
| 10 | Multi-Tenancy | ❌ | The outbox cycle is cross-tenant; per-tenant locks are #741's decorator, applied by name if an application lists it |
| 11 | Module Isolation | ❌ | Not applicable: no module scoping of a processor cycle |
| 12 | Audit Trail | ❌ | Not applicable: no compliance-relevant operation; processor audit is #749 |

---

## Prerequisites & Dependencies

- **#2667** (ADR-052 implementation) must merge before PR 3; PRs 1 and 2 do not touch `IDistributedLockProvider`. It is open and not a pending maintainer decision.
- **#717 Dilemma 2** (election, locks or both) does not block this plan: the shared contract is needed by #714 to #716 either way; only Phase 4 depends on the answer.
- **Not blocked** by an open decision of another issue: #2382 (requirements validator) and #218 (fencing) are related, not prerequisites.
- No follow-up issue file is needed: the deferred items have open issues (#2396 processor health, #223 auto-extend, #218 fencing).

## Next Steps

1. Open the plan PR; when work on #713 starts, ask the maintainer to answer Dilemmas 1 to 4, and record the answers in `## Maintainer Decisions` (Dilemma 2 together with #717's Dilemma 2).
2. After #2667 merges, brief PR 1 and PR 2 first; they have no dependency.
3. Open #714, #715 and #716 implementation work only after PR 2 of this plan merges (they reuse the gate, the marker and the range).
