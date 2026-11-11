# Implementation Plan: Persistent `IRoutingSlipStore` for the 10 Database Providers

> **Issue**: [#590](https://github.com/dlrivada/Encina/issues/590)
> **Type**: Feature
> **Parent**: [#875](https://github.com/dlrivada/Encina/issues/875) (EPIC)
> **Related**: #594 (persistent `IDelayedRetryStore`, the same `Either` question), #296 (Process Manager pattern), #2381 and ADR-050 (module dependencies, closed), #2382 (startup validator), #583 (persistent dead letter stores, closed)
> **Complexity**: High (10 providers, a runner that does not use a store today, a resume design question)
> **Estimated Scope**: ~2,800-3,600 lines of production code + ~6,000 lines of tests; far above one worker pull request, split proposed in Phase 1

---

## Summary

### What exists today (read on 2026-11-11, main `942c0f124`)

- `IRoutingSlipStore` (`src/Encina.Messaging/RoutingSlip/IRoutingSlipStore.cs`) has six `Task`-returning members: `GetAsync(Guid)`, `AddAsync(IRoutingSlipState)`, `UpdateAsync(IRoutingSlipState)`, `GetStuckAsync(TimeSpan olderThan, int batchSize)`, `GetExpiredAsync(int batchSize)` and `SaveChangesAsync`. `IRoutingSlipState` holds `RoutingSlipId`, `SlipType`, `Data`, `Itinerary`, `ActivityLog`, `Status`, `CurrentStep`, `CreatedAtUtc`, `LastUpdatedAtUtc`, `CompletedAtUtc`, `TimeoutAtUtc` and `ErrorMessage` (string-typed, serialized). The issue's original listing (`CreateAsync(RoutingSlip)`, `GetByCorrelationIdAsync`, `UpdateStepAsync`, `CompleteAsync`, `FailAsync`, `GetInProgressAsync`) and its two-table sketch do not match `src`. There is no implementation of the store, and no entity.
- `RoutingSlipRunner` (`RoutingSlipRunner.cs`) does **not** use a store: `MessagingServiceCollectionExtensions.RegisterRoutingSlips` (lines 114-120) registers only `RoutingSlipOptions` and `IRoutingSlipRunner`. `RunAsync` generates the id (`Guid.NewGuid()`, line 75), executes all steps inline in one call, and returns a `RoutingSlipResult<TData>`.
- **Steps are delegates**: `RoutingSlipStepDefinition<TData>` holds `Name`, `Execute` and `Compensate` as `Func<...>` and `Metadata` (`RoutingSlipStepDefinition.cs`), and `RoutingSlipActivityEntry<TData>` keeps the compensation delegate. Delegates cannot be persisted. The state can store the step **names**, the serialized data and the position; resuming after a restart needs a way to rebuild the steps from a name. This is Design Choice 4.
- `RoutingSlipOptions` already defines `DefaultTimeout` (30 minutes), `StuckCheckInterval` (5 minutes), `StuckThreshold` (10 minutes) and `BatchSize` (100), and `RoutingSlipErrorCodes` defines `NotFound`, `InvalidStatus`, `DeserializationFailed`, `Timeout` and `AlreadyCompleted`; nothing reads them yet, which shows the intended design: a recovery pass over stuck and expired slips. `RoutingSlipStatus` has `Running`, `Completed`, `Compensating`, `Compensated`, `Failed` and `TimedOut`.
- ADR-050 (matrix row at line 53) lists a persistent routing slip store as **Required** when the module is enabled, enforced by startup validation when no store is registered; the validator is #2382, and until it exists an internal hosted check does the job (the pattern of `DelayedRetryStartupCheck`).
- Maintainer decision on #590 (SPEC-000 DEC-009, 2026-10-23): everything deferred is inside 1.0 except the items of section 6, and routing slips are not listed there, so they are in; the issue sits in `v0.19.0` and covers the 10 providers (SQLite is out, ADR-024).

### What this plan delivers

`RoutingSlipStore{ADO,Dapper,EF,MongoDB}` with a `RoutingSlipState` entity for each of the 10 providers, the migration scripts, registration when `UseRoutingSlips` is on, the runner wired to the store, the startup check, a recovery processor for stuck and expired slips, the answers to the Design Choices applied, and tests against real databases.

- Affected packages: `Encina.Messaging` (interface, runner, registration, startup check, recovery processor), `Encina.ADO.{SqlServer,PostgreSQL,MySQL}`, `Encina.Dapper.{SqlServer,PostgreSQL,MySQL}`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.OpenTelemetry` (decorator), `Encina.Testing.Fakes`.
- Provider category: Database (10), `AGENTS.md` section 5.

---

## Design Choices

<details>
<summary><strong>1. Schema shape — one table or two</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One table; `Itinerary` and `ActivityLog` are serialized columns** (what `IRoutingSlipState` implies) | No interface change; one write per update; portable across the 10 providers | Steps are not queryable with SQL |
| **B) Two tables, `routing_slips` and `routing_slip_steps`** (the original proposal) | Queryable step history and per-step status | The state must expose steps (interface change); every update is a transaction over both tables; per-provider SQL doubles |

### Chosen Option: **A — One table** (recommended, pending the maintainer)

### Rationale

- The `ActivityLog` already is the step history, and the recovery processor needs only status, position and timestamps (all columns). One table keeps the 10 providers identical in shape, as the dead letter stores are.
- B wins when operators must query step outcomes directly with SQL.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Should the store return `Either<EncinaError, T>`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Change the interface to `Either`** like `IDeadLetterStore` | Consistent with ROP (ADR-001, ADR-006); the runner and the processor handle an infrastructure `Left` explicitly; the optimistic-concurrency conflict of Design Choice 3 is a typed `Left` instead of an exception | Changes the interface and the runner |
| **B) Keep `Task` and exceptions** | No change to the core | Two error conventions in `Encina.Messaging`; a concurrency conflict surfaces as an exception the caller must know to catch |

### Chosen Option: **A — Change the interface to `Either`** (recommended, pending the maintainer)

### Rationale

- `AGENTS.md` section 3: operations return `Either<EncinaError, T>` and a `Left` from a store inside a processor fails that operation, never reports success. Pre-1.0 means the interface changes completely (`AGENTS.md` section 1).
- The same question is Design Choice 3 of #594; the answers must match so `Encina.Messaging` ends with one convention. B wins only if the maintainer wants this issue to stay out of the core contract.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Concurrent updates of one slip</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Optimistic concurrency with a `Version` column** (a `Version` member on `IRoutingSlipState`; `UpdateAsync` is `... WHERE routing_slip_id = @id AND version = @expected`) | No lost updates and no extra module; the recovery processor can take over a stuck slip by bumping the version, so a late original run fails visibly instead of overwriting | A new member on the state; a conflict is an outcome every writer handles |
| **B) An `IDistributedLockProvider` lock per slip held by the runner** | No schema change | A lock module becomes required (ADR-050 terms); the lock must outlive the run or the recovery pass races it |
| **C) Last write wins** (the current contract implies it) | Simplest | A recovered slip and a still-running original run can overwrite each other; silent data loss |

### Chosen Option: **A — Optimistic concurrency** (recommended, pending the maintainer)

### Rationale

- A single worker owns a slip during `RunAsync`, so concurrency arises between the original run and the recovery processor (a slow run flagged as stuck). A makes that race detectable without infrastructure and doubles as the idempotency guard for duplicate step completions (function 9).
- B wins if the application already runs a lock provider for other modules; C wins only when no recovery processor runs and exactly one worker owns every slip.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. What "resume" means — rebuilding steps from persisted names</strong></summary>

### Options Considered

The runner executes steps that are delegates, and only names, data and position can be persisted. The acceptance criterion says a slip "survives a restart and can be resumed".

| Option | Pros | Cons |
|--------|------|------|
| **A) Persist for visibility and recovery bookkeeping only**: the recovery processor marks stuck slips `Failed` and expired slips `TimedOut`; no automatic resume and no compensation after a crash | Small: no new abstraction; satisfies "stuck and expired slips are returned correctly" | A crashed slip's completed steps are never compensated; "resumed" is not delivered |
| **B) A definition registry**: `IRoutingSlipDefinitionRegistry` maps a `SlipType` to a factory of its `BuiltRoutingSlipDefinition<TData>` (registered at startup); the recovery processor deserializes `Data` with `IMessageSerializer`, rebuilds the steps by name from the definition, and resumes at `CurrentStep` or compensates the persisted `ActivityLog` entries | Delivers the acceptance criterion; compensation after a crash works; uses only names and data | A new public abstraction and a processor; steps added at runtime by other steps are resumable only when they exist in the definition by name (a documented limit); `TData` needs a serializable shape |
| **C) Make steps serializable handlers** (a step is a handler type resolved from DI by name, like a request handler) | The most natural fit for restart | Replaces the delegate-based builder API completely; a large breaking redesign of `RoutingSlipBuilder` and its tests |

### Chosen Option: **B — A definition registry and a recovery processor** (recommended, pending the maintainer)

### Rationale

- B is the smallest design that makes the stated acceptance criterion true without redesigning the builder. A is a valid first release when the maintainer accepts "visible and bounded, not resumable"; C wins if the project wants routing slips to be durable workflows and accepts replacing the builder API (pre-1.0 allows it, but the cost is the largest).
- The answer changes Phase 1 and the test scope; Phase 1 therefore states both paths.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Separation, time, error text, serialization, startup validation, names and scope</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A separate persistent store, the shared conventions and a startup check** (no shared saga store, no headers, no workflow engine) | Routing slips define their steps up front and sagas react to events, so two contracts stay clear; the state is visible and recoverable; lightweight | One more store and table per provider |
| **B) Reuse `ISagaStore`, carry the slip in message headers, or adopt a workflow engine (Temporal, Elsa)** | Fewer tables, or a ready engine | Headers give no visibility and cannot recover failures that need intervention; a saga store mixes two patterns; a workflow engine is heavyweight for sequential processing |

### Chosen Option: **A — A separate persistent store and the shared conventions**

### Rationale

- Time: `TimeProvider` injected into every store and the processor; stuck means `LastUpdatedAtUtc <= now - olderThan` (and status `Running` or `Compensating`), expired means `TimeoutAtUtc <= now` and not terminal (`AGENTS.md` section 3). Database calls are asynchronous with a `CancellationToken`.
- Error text: `ErrorMessage` holds an error code or exception type, never `EncinaError.Message` (`AGENTS.md` section 3). The runner today builds `EncinaErrors.Create(HandlerFailed, ex.Message)` for an unexpected exception; persisted text uses the code `RoutingSlipErrorCodes.HandlerFailed` and the exception type, and Phase 1 fixes that error to carry no raw exception message.
- Payload: `Data` and `ActivityLog` are serialized through `IMessageSerializer` (already registered with `TryAddDefaultMessageSerializer` by the other messaging patterns), so message encryption applies to personal data inside a slip.
- Startup validation: enabling routing slips with no registered `IRoutingSlipStore` fails host start (ADR-050 row at line 53), through an internal hosted check that migrates into #2382's validator. Names: `RoutingSlipStore{ADO|Dapper|EF|MongoDB}` (`AGENTS.md` section 4). Scope: the 10 providers (`AGENTS.md` section 5), SQLite out (ADR-024).
- No in-memory store is delivered (none exists today); tests use a fake in `Encina.Testing.Fakes`. A Development-only in-memory store, if wanted, rides with #2382.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (TimeProvider, async database calls, EncinaError.Message never in plaintext storage), AGENTS.md section 4 (naming) and section 5 (10 providers), ADR-024, and ADR-050 (matrix row at line 53, startup validation)

</details>

---

## Implementation Phases

### Phase 1: Core — Contract, Runner, Registry, Startup Check, Recovery Processor and Split

> **Goal**: Apply the answers to Design Choices 2 to 4 in `Encina.Messaging` before any provider work.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `IRoutingSlipStore.cs`: `Either<EncinaError, T>` returns (Design Choice 2); `IRoutingSlipState` gains `Version` and `TenantId`/`ModuleId` (Design Choice 3; tenant and module as typed columns like the dead letter row, `AGENTS.md` section 6, functions 10 and 11); `UpdateAsync` is conditional on the version and returns a typed concurrency `Left`.
2. **Modify** `RoutingSlipRunner.cs`: persist the state through the store at start (`AddAsync`), after each step (`UpdateAsync`, `ActivityLog` and `CurrentStep`) and at completion, failure, compensation and timeout; replace the raw `ex.Message` in the `HandlerFailed` error with the code; keep the in-process behavior when a store call is a `Left` (fail the run visibly, compensate).
3. **Create** (Design Choice 4 option B) `IRoutingSlipDefinitionRegistry`, its default implementation, a registration method on `MessagingConfiguration` or an `AddEncinaRoutingSlip<TData>(slipType, definitionFactory)` extension, and `RoutingSlipRecoveryProcessor` (hosted, `PeriodicTimer` per iteration catch, `StuckCheckInterval`, `StuckThreshold`, `BatchSize`): stuck slips are taken over with a version bump and resumed or compensated, expired slips are compensated and marked `TimedOut`. For option A the processor only marks `Failed`/`TimedOut`.
4. **Create** `RoutingSlipStartupCheck` (internal hosted service, the pattern of `DelayedRetryStartupCheck`) and call it from `RegisterRoutingSlips`.
5. **Modify** `Encina.Testing.Fakes` (a `FakeRoutingSlipStore`) and the existing runner tests (`RoutingSlipRunnerDiRegistrationTests` and the unit and guard tests under `tests/Encina.*Tests/Messaging/RoutingSlip`).
6. **Register** EventIds 6840-6849 as `MessagingRoutingSlip` in `EventIdRanges.cs` and in the `Encina.Messaging` entry of the `AssemblyRanges` map (the `Messaging` range 2800-2999 is almost full: 170 ids used, the last 2998); update `PublicAPI.Unshipped.txt`.
7. **Split**: S1 core contract and runner, S2 registry and recovery processor and startup check, S3-S5 ADO.NET, S6-S8 Dapper, S9 EF Core, S10 MongoDB, S11 cross-provider tests (contract, property, integration collections), S12 documentation. Each stays within 15 files and 1,500 lines.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no backward compatibility). RoutingSlipRunner (src/Encina.Messaging/
RoutingSlip/RoutingSlipRunner.cs) runs all steps inline and uses no store; IRoutingSlipStore has no
implementation. Steps are delegates (RoutingSlipStepDefinition<TData>), so only names, serialized data
and position can be persisted. The maintainer answered Design Choices 1-4 of
docs/plans/routing-slip-stores-implementation-plan-590.md (apply the recorded answers).

TASK:
1. Change IRoutingSlipStore to Either<EncinaError, T> returns; add Version, TenantId and ModuleId to
   IRoutingSlipState; make UpdateAsync conditional on Version with a typed concurrency Left.
2. Wire RoutingSlipRunner to the store (AddAsync at start, UpdateAsync after each step and at the end),
   serializing Data and ActivityLog with IMessageSerializer; replace the raw exception message in the
   HandlerFailed error by the error code.
3. If the answer to Design Choice 4 is the definition registry: add IRoutingSlipDefinitionRegistry and
   RoutingSlipRecoveryProcessor (stuck: bump the version, resume or compensate; expired: compensate and
   mark TimedOut). Otherwise the processor marks Failed/TimedOut only.
4. Add RoutingSlipStartupCheck (fail the host when UseRoutingSlips is on and no IRoutingSlipStore is
   registered, naming the module, ADR-050 and how to add a store); call it from RegisterRoutingSlips.
5. Add the fake store, update tests, register EventIds 6840-6849 and PublicAPI lines.

KEY RULES:
- No [Obsolete], no compatibility overload: change the interface completely.
- TimeProvider injected; EncinaError.Message never reaches logs or storage.
- The recovery loop catches per iteration and never lets an exception escape; cancellation propagates.
- A Left from the store fails the operation visibly; never report success.

REFERENCE FILES:
- src/Encina.Messaging/RoutingSlip/IRoutingSlipStore.cs, IRoutingSlipState.cs, RoutingSlipRunner.cs,
  RoutingSlipOptions.cs, RoutingSlipErrorCodes.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs (RegisterRoutingSlips, lines 114-120)
- src/Encina.Messaging/Recoverability/DelayedRetryStartupCheck.cs
- docs/architecture/adr/050-*.md (line 53)
```

</details>

---

### Phase 2: ADO.NET Stores (SQL Server, PostgreSQL, MySQL)

> **Goal**: Three `RoutingSlipStoreADO` classes with their scripts and registration.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** per package `RoutingSlip/RoutingSlipState.cs` (the `IRoutingSlipState` entity) and `RoutingSlip/RoutingSlipStoreADO.cs`, following `DeadLetter/` in the same package.
2. **Create** per package `Scripts/0NN_CreateRoutingSlipsTable.sql` (next free number; `030` is the highest on ADO.SqlServer today) with the columns of the issue's sketch plus `version`, `tenant_id` and `module_id`, and indexes on `(status, last_updated_at_utc)` and `(status, timeout_at_utc)`.
3. **Implement** `GetAsync`, `AddAsync`, the version-conditional `UpdateAsync` (zero rows affected is the concurrency `Left`), `GetStuckAsync` (`status IN ('Running','Compensating') AND last_updated_at_utc <= @threshold`), `GetExpiredAsync` (`timeout_at_utc <= @now` and non-terminal) with `TOP (@n)` or `LIMIT @n` per database, `SaveChangesAsync` (a no-op like the non-EF stores).
4. **Modify** each package's `ServiceCollectionExtensions` to register `IRoutingSlipStore` with `TryAdd` when `UseRoutingSlips` is on, including the tenancy variant; add EventIds inside `ADOSqlServer` 3200-3249, `ADOPostgreSQL` 3250-3299, `ADOMySQL` 3300-3349.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Phase 1 changed the core contract. ADO.NET providers share one folder structure per database package
(DeadLetter/, Scripts/). SQL differences (AGENTS.md section 5): SQL Server uses TOP (@n) and bit;
PostgreSQL and MySQL use LIMIT @n; PostgreSQL identifiers are case-sensitive; MySQL uses backticks.

TASK:
1. For Encina.ADO.SqlServer, Encina.ADO.PostgreSQL and Encina.ADO.MySQL create RoutingSlipState,
   RoutingSlipStoreADO and the SQL script of the plan.
2. Implement every IRoutingSlipStore member with OpenAsync/ExecuteReaderAsync/ExecuteNonQueryAsync and
   a CancellationToken (never Open()), UTC DateTime values from TimeProvider, parameterized SQL.
3. Register with TryAdd semantics when UseRoutingSlips is on.

KEY RULES:
- Left(EncinaError) on DbException with an error code, never the exception message.
- UpdateAsync with a stale Version returns the concurrency Left, not success.
- Same behavior in all three databases; the contract tests run them side by side.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/DeadLetter/DeadLetterStoreADO.cs and Scripts/029_CreateDeadLetterMessagesTable.sql
- src/Encina.ADO.PostgreSQL/DeadLetter/, src/Encina.ADO.MySQL/DeadLetter/
- src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs (line 82)
```

</details>

---

### Phase 3: Dapper Stores (SQL Server, PostgreSQL, MySQL)

> **Goal**: Three `RoutingSlipStoreDapper` classes with scripts and registration.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** per package `RoutingSlip/RoutingSlipState.cs` and `RoutingSlipStoreDapper.cs`, mirroring `DeadLetter/` of the same package.
2. **Create** the same SQL scripts as Phase 2 (identical schema per database).
3. **Implement** the members with `QueryAsync`/`ExecuteAsync` and `CommandDefinition` carrying the `CancellationToken`.
4. **Modify** each `ServiceCollectionExtensions` (registration, tenancy variant) and add EventIds inside `DapperSqlServer` 3350-3399, `DapperPostgreSQL` 3400-3449, `DapperMySQL` 3450-3499.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Phase 2 fixed the schema and statements per database. Dapper packages mirror the ADO ones.

TASK:
1. For Encina.Dapper.SqlServer, Encina.Dapper.PostgreSQL and Encina.Dapper.MySQL create the state
   entity and RoutingSlipStoreDapper, reusing the Phase 2 SQL text per database.
2. Use CommandDefinition with the CancellationToken for every call; map DbException to Left.
3. Register with TryAdd when UseRoutingSlips is on.

KEY RULES:
- Identical observable behavior to the ADO store of the same database, including the version check.
- TimeProvider injected; no synchronous database calls.

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/DeadLetter/DeadLetterStoreDapper.cs
- src/Encina.Dapper.PostgreSQL/ServiceCollectionExtensions.cs (line 65)
- the Phase 2 ADO routing slip stores
```

</details>

---

### Phase 4: EF Core Store (SQL Server, PostgreSQL, MySQL)

> **Goal**: One `RoutingSlipStoreEF` for the three EF providers.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.EntityFrameworkCore/RoutingSlip/RoutingSlipState.cs` (with `[ConcurrencyCheck]` or `IsConcurrencyToken` on `Version`), `RoutingSlipStateConfiguration.cs` (indexes of Phase 2) and `RoutingSlipStoreEF.cs`.
2. **Implement** `UpdateAsync` through the concurrency token (`DbUpdateConcurrencyException` is the concurrency `Left`) and the queries with LINQ.
3. **Modify** `ServiceCollectionExtensions.cs` (registration next to line 212, tenancy variant) and add EventIds inside `EntityFrameworkCore` 3000-3099.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Encina.EntityFrameworkCore is one package for SQL Server, PostgreSQL and MySQL. The dead letter store
is DeadLetterStoreEF with DeadLetterMessageConfiguration.

TASK:
1. Create the entity, IEntityTypeConfiguration and RoutingSlipStoreEF.
2. Map DbUpdateConcurrencyException to the typed concurrency Left; other DbException to Left with an
   error code only.
3. Register with TryAdd when UseRoutingSlips is on.

KEY RULES:
- CancellationToken on SaveChangesAsync and queries.
- No global tenant query filter may hide slips from the recovery processor, which reads every tenant.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/DeadLetter/
- src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs (lines 212-214)
```

</details>

---

### Phase 5: MongoDB Store

> **Goal**: `RoutingSlipStoreMongoDB`.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.MongoDB/RoutingSlip/RoutingSlipState.cs` and `RoutingSlipStoreMongoDB.cs`; `UpdateAsync` is `ReplaceOneAsync` with a filter on `_id` and `Version`.
2. **Modify** `EncinaMongoDbOptions` (collection name next to `Collections.DeadLetterMessages`), `MongoDbIndexCreator.cs` (indexes `(Status, LastUpdatedAtUtc)` and `(Status, TimeoutAtUtc)`), `ServiceCollectionExtensions` and `Log.cs` (ids after 3166 inside 3100-3199).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
MongoDB has no SQL; the dead letter store is the reference (DeadLetterStoreMongoDB, index creation in
MongoDbIndexCreator, collection names in EncinaMongoDbOptions.Collections).

TASK:
1. Create the document class and the store; version-conditional replace for UpdateAsync.
2. Add the collection option, indexes and registration (UseRoutingSlips).
3. Map MongoException to Left with an error code.

KEY RULES:
- UTC DateTime from TimeProvider; CancellationToken on every driver call.

REFERENCE FILES:
- src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs
- src/Encina.MongoDB/MongoDbIndexCreator.cs
- src/Encina.MongoDB/EncinaMongoDbOptions.cs
```

</details>

---

### Phase 6: Cross-Cutting Integration and Observability

> **Goal**: Telemetry, health and registration completeness.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.OpenTelemetry/MessagingStores/InstrumentedRoutingSlipStore.cs` (decorator, like `InstrumentedDeadLetterStore`) and its `DecorateService<IRoutingSlipStore>` line in `ServiceCollectionExtensions.cs` (next to line 152), so all 10 providers are instrumented once.
2. **Create** a health check reporting stuck and expired counts from `GetStuckAsync`/`GetExpiredAsync` (or a count member if the maintainer prefers) and store connectivity.
3. **Create** DI tests with `ValidateOnBuild` and `ValidateScopes` per provider: the host starts with `UseRoutingSlips` on and only the provider's registration; with no store the startup check fails.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Stores exist for all 10 providers. Telemetry for stores is done by decorators in
src/Encina.OpenTelemetry/MessagingStores/.

TASK:
1. Add InstrumentedRoutingSlipStore and register the decoration.
2. Add the routing slip health check (stuck/expired counts) with DefaultName const, static Tags and
   scoped resolution through IServiceProvider.CreateScope().
3. Write per-provider DI tests (ValidateOnBuild, ValidateScopes; with and without a store).

KEY RULES:
- No per-provider Meter; one decorator.
- EncinaError.Message never in tags or health data.

REFERENCE FILES:
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedDeadLetterStore.cs
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs (line 152)
- src/Encina.Messaging/Health/DeadLetterHealthCheck.cs
```

</details>

---

### Phase 7: Testing

> **Goal**: Every applicable flag reaches its manifest target for every touched package.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit tests** for all 10 stores, the runner's use of the store, the registry, the recovery processor and the startup check (fake `TimeProvider`; `Left` on database failure; stale version).
2. **Guard tests** for every public method with parameters (`batchSize`, `olderThan`, null state).
3. **Contract tests**: one `IRoutingSlipStore` contract run over all 10 stores (real implementations).
4. **Property tests** (FsCheck): stuck and expired selection and the state round trip (serialize, store, read, deserialize).
5. **Integration tests** on the shared collection fixtures (`ADO-`/`Dapper-`/`EFCore-` x `SqlServer`/`PostgreSQL`/`MySQL`, MongoDB): a slip survives a restart (new provider scope, same database), stuck and expired selection, concurrent update (one winner), recovery processor end to end (option B); `ClearAllDataAsync` in `InitializeAsync`; never a `.md` justification.
6. **Load tests** for concurrent updates of one slip (Design Choice 3 option A or B), else a justification file; a benchmark justification file.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
Per-flag coverage (AGENTS.md section 9) for Encina.Messaging, the 8 provider packages and
Encina.OpenTelemetry; targets in .github/coverage-manifest/{Package}.json.

TASK:
1. Write the unit, guard, contract and property tests of the plan for all 10 providers.
2. Write the integration tests on the existing shared collection fixtures, including the restart,
   the concurrent update and the recovery processor scenarios.
3. Add the load test (per Design Choice 3) or its justification, and the benchmark justification.
4. Measure per-flag coverage and CRAP with coverage-report.cs and report it against the manifests.

KEY RULES:
- Shouldly through Encina.Testing.Shouldly; shared collection fixtures only, never IClassFixture.
- Tests of a provider-dependent feature cover all 10 providers.

REFERENCE FILES:
- tests/Encina.IntegrationTests (Collections.cs files)
- tests/Encina.UnitTests/Messaging/RoutingSlip/
- .github/scripts/coverage-report.cs
```

</details>

---

### Phase 8: Documentation and Finalization

> **Goal**: Records complete; users can enable persistent routing slips.

<details>
<summary><strong>Tasks</strong></summary>

1. XML doc comments on every new or changed public API.
2. `changelog.d/590-routing-slip-stores.added.md` (and a `changed` fragment for the interface change).
3. README updates for the 8 provider packages, a routing slip configuration guide (including the definition registry and its limit for steps added at runtime), `docs/INVENTORY.md`.
4. Update the ADR-050 matrix row at line 53 (the store is no longer "planned"); ADR only if Design Choice 4 sets a policy (ADR number assigned when the ADR pull request opens).
5. `dotnet build --configuration Release` with zero warnings; `dotnet test` with every flag at its target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT:
All stores are implemented and tested. Documentation is written by the docs-writer agent following
the encina-docs skill; figures are cited with covref markers, never typed.

TASK:
1. Complete XML docs and PublicAPI files for every package touched.
2. Write the changelog fragments, the README updates, the configuration guide and the ADR-050 row.

KEY RULES:
- English only; never edit CHANGELOG.md directly; no hand-typed coverage numbers.

REFERENCE FILES:
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
- docs/architecture/adr/050-*.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| Enterprise Integration Patterns, Routing Slip | A message carries or is tracked through an itinerary of steps | The runner and the persisted itinerary |
| ADR-050 (matrix row at line 53) | A persistent store is required when the module is enabled; startup validation | `RoutingSlipStartupCheck` |
| SPEC-000 DEC-009 | Items not listed in section 6 are inside 1.0 | Scope of this plan |
| ADR-001, ADR-006 | Railway Oriented Programming | Design Choice 2 |
| ADR-024 | SQLite out of the provider matrix | 10 providers, not 13 |
| ADR-018, ADR-021 | Cross-cutting, EventIds | Matrix below, range 6840-6849 |
| `AGENTS.md` section 3 | Time, async DB calls, errors not swallowed, error text not stored, background loops | Design Choice 5 and the recovery processor |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IRoutingSlipStore`, `IRoutingSlipState`, `RoutingSlipStatus`, `RoutingSlipOptions`, `RoutingSlipErrorCodes` | `src/Encina.Messaging/RoutingSlip/` | The contract and the already-defined options and codes |
| `RoutingSlipRunner`, `RoutingSlipBuilder`, `RoutingSlipStepDefinition` | same folder | The runner to wire; the delegate-based steps |
| `RegisterRoutingSlips` | `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:114-120` | Registration point |
| `DelayedRetryStartupCheck` | `src/Encina.Messaging/Recoverability/` | Pattern for the startup check |
| Dead letter stores and scripts | `src/Encina.{ADO,Dapper}.*/DeadLetter/`, `Encina.EntityFrameworkCore/DeadLetter/`, `Encina.MongoDB/DeadLetter/` | Structural reference per provider |
| `InstrumentedDeadLetterStore` | `src/Encina.OpenTelemetry/MessagingStores/` | Pattern for the decorator |
| Shared integration collections | `tests/Encina.IntegrationTests` | `ADO-`, `Dapper-`, `EFCore-` x 3 and MongoDB |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` | 6840-6849 (new: `MessagingRoutingSlip`) | From the family block 6800-6849; the `Messaging` range 2800-2999 is nearly full (last id 2998), so the new store, registry and recovery processor logging gets its own range, mapped to the `Encina.Messaging` assembly in the `AssemblyRanges` map |
| `Encina.ADO.SqlServer`, `.PostgreSQL`, `.MySQL` | next free ids inside 3200-3249, 3250-3299, 3300-3349 | A handful per package; the allocation test verifies |
| `Encina.Dapper.SqlServer`, `.PostgreSQL`, `.MySQL` | next free ids inside 3350-3399, 3400-3449, 3450-3499 | Same |
| `Encina.EntityFrameworkCore` | next free ids inside 3000-3099 | Same |
| `Encina.MongoDB` | 3167+ inside 3100-3199 (3166 is the last used) | Same |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New production files | ~42 | Per provider: state and store; 6 SQL scripts; registry; recovery processor; startup check; decorator; health check |
| Changed production files | ~22 | Interface, runner, registration, 8 `ServiceCollectionExtensions`, options, index creator, `Log.cs` files, fakes, PublicAPI, `EventIdRanges.cs` |
| Coverage manifests | ~10 | One edit per package |
| Tests | ~45 | Unit, guard, contract, property, integration, DI, load or justification |
| Documentation | ~12 | READMEs, guide, ADR-050 row, INVENTORY, 2 changelog fragments |
| **Total** | **~131** | About twelve pull requests (Phase 1 item 7) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #590 - persistent IRoutingSlipStore for the 10 database providers.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Railway Oriented Programming: Either<EncinaError, T>; a Left from a store inside a processor fails
  the operation, never reports success.
- Database category: ADO.NET x3, Dapper x3, EF Core x3 (SQL Server, PostgreSQL, MySQL), MongoDB.
  SQLite is out of the matrix (ADR-024). ADR-050 requires a persistent store when routing slips are on.

IMPLEMENTATION OVERVIEW:
1. Core: IRoutingSlipStore to Either, Version/TenantId/ModuleId on the state, the runner persists state
   (steps are delegates, so persist names, serialized data and position), startup check, and per the
   answer to Design Choice 4 a definition registry plus a recovery processor.
2. ADO.NET, Dapper, EF Core, MongoDB stores with scripts and registrations (UseRoutingSlips).
3. Decorator-based telemetry, health check, per-provider DI tests.
4. Tests per flag across all 10 providers; docs, ADR-050 row and changelog.

KEY PATTERNS:
- TimeProvider injected; stuck = LastUpdatedAtUtc <= now - olderThan; expired = TimeoutAtUtc <= now.
- ErrorMessage stores an error code or exception type, never raw text.
- Data and ActivityLog go through IMessageSerializer.
- Async database calls with CancellationToken; parameterized SQL; version-conditional updates.
- TryAdd registration so the application's store wins in any order.
- Shared collection fixtures for integration tests; Shouldly only.

REFERENCE FILES:
- src/Encina.Messaging/RoutingSlip/IRoutingSlipStore.cs, RoutingSlipRunner.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs (lines 114-120)
- src/Encina.ADO.SqlServer/DeadLetter/DeadLetterStoreADO.cs
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedDeadLetterStore.cs
- docs/plans/routing-slip-stores-implementation-plan-590.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | The state is rewritten after every step and read by id only for recovery; a cached copy would be stale and break the version check |
| 2 | OpenTelemetry | ✅ Phase 6 | `InstrumentedRoutingSlipStore` decorator covers all 10 providers; the runner's step transitions are already logged through `RoutingSlipLog` |
| 3 | Structured Logging | ✅ Phases 1-5 | `[LoggerMessage]` ids 6840-6849 for the core and ids inside the provider ranges, packed sequentially, registered first (ADR-021) |
| 4 | Health Checks | ✅ Phase 6 | Stuck and expired counts and store connectivity |
| 5 | Validation | ✅ Phases 2-5 | Guard tests for `batchSize`, `olderThan` and null state; the slip data is validated by the builder |
| 6 | Resilience | ❌ N/A | Transient database retry belongs to the existing database resilience options (`src/Encina/Database/DatabaseResilienceOptions.cs`), shared by all stores; no second layer |
| 7 | Distributed Locks | ❌ N/A | Optimistic concurrency (Design Choice 3 option A) replaces a lock; it becomes ✅ only if the maintainer picks option B |
| 8 | Transactions | ✅ Phases 2-5 | Each add or update is one atomic statement or document write on one table; `SaveChangesAsync` is a no-op like the non-EF stores |
| 9 | Idempotency | ✅ Phase 1 | The `Version` check makes a duplicate step completion a typed concurrency `Left` instead of a second write |
| 10 | Multi-Tenancy | ✅ Phase 1 | Typed `TenantId` column on the state, filled from the request context at start |
| 11 | Module Isolation | ✅ Phase 1 | Typed `ModuleId` column on the state, filled from the request context at start |
| 12 | Audit Trail | ❌ N/A | The persisted `ActivityLog` is the durable step history; routing slips are not a compliance surface that SPEC-002 requires auditing |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Module dependency decision (ADR-050, #2381) | ✅ Closed | Requires a persistent store and startup validation |
| Persistent dead letter stores (#583) | ✅ Closed | Structural reference |
| `IMessageSerializer` registration helper (`TryAddDefaultMessageSerializer`) | ✅ Available | Used by the other messaging patterns |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Persistent `IDelayedRetryStore` | #594 | Same `Either` question; answers must match |
| Startup validator | #2382 | The routing slip check migrates into it |
| Process Manager pattern | #296 | Related pattern; no code dependency |

---

## Next Steps

1. Link this plan from #590 and ask the maintainer to answer Design Choices 1 to 4 when work starts (Design Choice 2 together with #594).
2. Implement Phases 1 to 8 as the pull requests of Phase 1 item 7, `Fixes #590` on the last.
