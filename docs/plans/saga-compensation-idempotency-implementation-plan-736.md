# Implementation Plan: `Encina.Messaging` Saga compensation step replay idempotency

> **Issue**: [#736](https://github.com/dlrivada/Encina/issues/736)
> **Type**: Feature
> **Verified against**: origin/main `c49b987e9` (2026-11-12)
> **Complexity**: High (7 phases, 10 database providers, ~96 files in 9 pull requests)
> **Estimated Scope**: ~1,200-1,600 lines of production code + ~1,800-2,200 lines of tests

---

## Summary

A compensation that runs twice can refund twice or release stock twice. Today the only compensation progress record is `ISagaState.CurrentStep` (`src/Encina.Messaging/Sagas/ISagaState.cs`). `SagaRunner.HandleStepFailureAsync` persists `Compensating` through `SagaOrchestrator.StartCompensationAsync`, runs every compensation delegate in process (`SagaRunner.CompensateAsync`, reverse order, a failing compensation does not stop the others) and only afterwards records the steps as done with `CompleteCompensationAsync`, which loops `SagaOrchestrator.CompensateStepAsync` (it only decrements `CurrentStep`). When `CompensateAsync` runs again for the same saga, every compensation runs again, and a failure moves the saga to `Failed` through `FailAsync` with no per-step record of what already ran.

**Who replays (verified with a repo-wide search on origin/main)**: nothing in `src/` re-invokes `SagaRunner.CompensateAsync` after a process restart: `GetStuckSagasAsync` has store implementations and README mentions but no caller, and the runner executes the delegates from the in-memory definition of a live run. The replay entry points this feature protects are therefore (1) any in-process re-entry of `CompensateAsync` for the same saga id (the retry of a failed compensation planned in #720 is one), and (2) the resume or recovery entry point of #1237 and #297, which will call it with a saga id. This plan adds the durable record and the skip logic; it adds no recovery processor (Design Choice 6, a Dilemma). The tests exercise (1): a fresh runner instance over a store that retained the record of the first run.

What it implements:

- A per-step compensation record in the saga state (which step indices were compensated, with `CompensatedAtUtc`), persisted by every `ISagaStore` implementation.
- The key `{saga-id}:{step-index}:compensate`, derived from `SagaId` and the step index and handed to the compensation delegate through `IRequestContext.IdempotencyKey` (`WithIdempotencyKey` already exists), so a handler can deduplicate its external call on it.
- `SagaRunner` skips a step already recorded and records each step per the order chosen in Design Choice 3.
- Concurrent replays of one saga are made safe by a version-checked update of the state row (Design Choice 4).
- Telemetry: a skipped-replay counter and a tag on the existing saga activity, plus log entries in the logging classes of the existing ranges.

Standards: the Saga pattern requires idempotent compensating actions (comparable products: payment APIs with idempotency keys, Temporal and Durable Functions replay-safe activities); ADR-001 (ROP); the row-claim convention is to be recorded in ADR-043 (reserved by #1251, not yet written).

Delivery guarantee against the issue's first acceptance criterion ("compensation steps execute at most once per saga step"): the recommended Option A of Design Choice 3 is at-least-once with a key, so the criterion holds literally only under Option B. The maintainer's answer to Design Choice 3 therefore also settles that criterion.

Estimated scope: about 96 distinct files (core 12, provider 39, tests, test doubles and test schemas 21, documentation and metadata 24), shipped as 9 pull requests (see "Pull request split"). Affected packages: `Encina.Messaging`, `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.OpenTelemetry` (`InstrumentedSagaStore` passes the new state through unchanged), `Encina.Testing.Fakes` (`FakeSagaState`).

Provider category: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (one implementation, `SagaStoreEF`, verified against three databases) and MongoDB. The original issue text says 13 providers; that is stale (AGENTS.md section 5; Oracle and SQLite are out, ADR-009 and ADR-024).

---

## Design Choices

<details>
<summary><strong>1. Where the per-step progress lives — in the saga state record</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A column or field on the saga state (`CompensatedStepsJson`), written with the existing `UpdateAsync`** | One consistency boundary; the state row is already the durable record; no new table on 10 providers | The record grows with the step count (small, bounded by the saga definition) |
| **B) A separate `SagaCompensationSteps` table/collection** | Row per step, easy to query | A second table and a second write on 10 providers; two records can disagree after a crash |
| **C) Reuse `CurrentStep` only** | No schema change | It is a counter, not a record: it cannot say which steps ran when delegates run before the count moves (the defect) |

### Chosen Option: **A — A compensation record on the saga state (recommended, pending the maintainer)**

### Rationale

- Option A keeps one consistency boundary: the state row is already the durable record and a separate table would add a second write that can disagree after a crash. Option B also satisfies AGENTS.md section 5 and the issue text, so the choice is the maintainer's.
- Consequence for delivery: under A the interface members land together with all 14 implementers found by `git grep ": ISagaState"` on origin/main (the 8 provider state classes, `FakeSagaState`, `SagaStateFaker`'s state, `ArbitrarySagaState` in `Encina.Testing.FsCheck`, the two `BenchmarkSagaState` classes and `TestSagaState` in `SagaOrchestratorTests`) in one pull request of about 26 files, over the 15-file limit, because it cannot build otherwise; see "Pull request split"; under B each provider pull request is additive and buildable alone.
- Pre-1.0: under A the columns are added to the create scripts `003_CreateSagaStatesTable.sql` (6 SQL scripts), the EF configuration and the Mongo document; no migration helper (AGENTS.md section 3).
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Idempotency key and how the delegate receives it</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `{saga-id}:{step-index}:compensate`, passed as `requestContext.WithIdempotencyKey(key)` to the existing delegate** | No change to the delegate signature `(data, IRequestContext, CancellationToken)`; consistent with the Inbox key convention | A handler that ignores the context key gets no protection from it |
| **B) A new delegate overload taking the key as a parameter** | Explicit | Public API change on `SagaStepBuilder` for no gain over the context key |
| **C) Hash of the saga data** | Detects data changes | Not stable across replays; not a key |

### Chosen Option: **A — The context idempotency key**

### Rationale

- `IRequestContext.WithIdempotencyKey` already exists and is what `InboxPipelineBehavior` reads, so a compensation that sends a command through `IEncina` inherits the key.
- Registered values, because handlers deduplicate on the key and it is public behavior: the step index is **0-based** (the index into `definition.Steps`, the loop variable `i` of `CompensateAsync`; the logs keep their existing `i + 1` display), and the saga id is formatted with `:D` (lowercase hyphenated), for example `3f2c...-...:1:compensate`.
- **Class:** Trivial
- **Settled by:** AGENTS.md section 3 (pattern choice: Inbox deduplication key) and `src/Encina/Abstractions/IRequestContext.cs` (`WithIdempotencyKey`)

</details>

<details>
<summary><strong>3. Order of the record relative to the compensation delegate</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Record after the delegate succeeds, key passed to the delegate (at least once)** | No compensation is ever lost; the handler deduplicates on the key | A duplicate is possible when the handler ignores the key |
| **B) Record before the delegate runs (at most once)** | A crash can never run a destructive compensation twice | A crash after the record and before the delegate finishes skips the compensation silently, unless the step is marked for manual review (AGENTS.md section 3: errors are never swallowed) |
| **C) Per step configurable** | Fits both kinds of compensation | Two code paths to test on 10 providers |

### Chosen Option: **A — Record after success, key to the delegate (recommended, pending the maintainer)**

### Rationale

- A wins when the compensation target accepts an idempotency key (most payment and inventory APIs); B wins for an irreversible effect with no key on the target. The recommendation is the issue's own (Option B in its numbering), because losing a compensation is the worse failure for money and stock.
- Acceptance criterion 1 of the issue ("at most once per saga step") is met literally only by Option B here; Option A is at-least-once with a key and is recorded as such in the documentation.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Concurrent replays of the same saga — conditional update or a distributed lock</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Version-checked update of the state row (a `Version` integer on `ISagaState`; `WHERE Version = @expected`, Mongo filter on the same field); the loser re-reads and skips recorded steps** | No lock provider needed (pay-for-what-you-use); same primitive as row claiming | A lost race costs one retry; every provider needs the predicate; every caller of `UpdateAsync` can now see a conflict |
| **B) `IDistributedLockProvider` per saga id around compensation** | Serializes whole compensation | Forces a lock provider on every saga user; lock expiry can still allow two runners |
| **C) Document single-runner as a requirement** | No code | Two workers recovering a stuck saga do exist (`GetStuckSagasAsync`) |

### Chosen Option: **A — Version-checked update on the state row (recommended, pending the maintainer)**

### Rationale

- Option B wins where the compensation itself must never overlap even for delegates that ignore the key; A wins everywhere else and keeps locks optional. The row-claiming convention to be recorded in ADR-043 (reserved by #1251, not yet written) uses the same predicate style.
- **Why a version and not `LastUpdatedAtUtc`** (verified: `SagaOrchestrator` sets `state.LastUpdatedAtUtc = now` before every `UpdateAsync`, lines 192, 232, 274, 328, 379 and 500, so the previous value is gone when the store runs; a timestamp also ties under `FakeTimeProvider` and within one tick, and MySQL and Mongo truncate precision): the predicate is a `Version` integer.
- **Mechanism under Option A (the contract all ten providers implement)**: `ISagaState.Version` (int) is read when the state is loaded; the orchestrator never modifies it; `UpdateAsync` writes `Version + 1` only where the stored `Version` equals the one the state object carries, and on success writes the new value back to the state object; zero rows affected returns `Left(saga.compensation_conflict)` (category `Conflict`). On EF Core `SagaStoreEF.UpdateAsync` only stages the change and `SaveChangesAsync` is a separate method: EF Core does not increment an `int` concurrency token by itself, so `SagaStoreEF.UpdateAsync` sets `Version = Version + 1` explicitly while the original value stays the one loaded (without it the token never changes and no conflict is ever detected); `Version` is configured as a concurrency token in `SagaStateConfiguration`, and `SagaStoreEF.SaveChangesAsync` catches `DbUpdateConcurrencyException` and returns the same `Left`. `SagaOrchestrator` never calls `SaveChangesAsync` today (verified), so this plan adds the one method `SagaOrchestrator.CommitAsync(CancellationToken)` that calls `ISagaStore.SaveChangesAsync` and returns its `Either`, the commit owner of the saga family (the #720 plan; the #297 plan commits the same way): `RecordCompensatedStepAsync` calls it, and the runner never touches `ISagaStore`. The sibling concurrency issue #2213 (unconditional `TimeoutAsync`) shares the `Version` column: whichever lands first adds it and the other rebases.
- **Callers**: `AdvanceAsync`, `CompleteAsync`, `StartCompensationAsync`, `FailAsync` and `TimeoutAsync` (and `CompensateStepAsync`) all pass the `Left` from `UpdateAsync` upward today; a conflict reaches them as a `Left` with the new code, and the runner treats it like any persistence `Left` (the saga is not reported successful). No caller retries on its own; the loser of a compensation race re-reads and skips recorded steps (`RecordCompensatedStepAsync`).
- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Names of the new state field and error code</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CompensatedStepsJson` (string?) on `ISagaState`; `SagaErrorCodes.CompensationConflict = "saga.compensation_conflict"`** | Follows the `...AtUtc`/descriptive naming and the `saga.*` code convention | None significant |
| **B) A typed collection on the interface** | Strongly typed at the abstraction | Each provider maps a collection differently; string keeps the interface provider-neutral like `Data` |

### Chosen Option: **A — A JSON string on the interface, typed inside `Encina.Messaging`**

### Rationale

- `ISagaState.Data` is already a serialized string; the same shape keeps the interface provider-neutral. A small internal record `CompensatedStep(int Index, DateTime CompensatedAtUtc)` is serialized by `SagaOrchestrator`.
- The error code is created with `ErrorCategory.Conflict` (ADR-062, `EncinaErrors.Create(code, ErrorCategory, message, ...)`).
- **Class:** Trivial
- **Settled by:** AGENTS.md section 4 (naming: `AtUtc` suffix, descriptive identifiers) and `src/Encina.Messaging/Sagas/SagaErrorCodes.cs` (the `saga.*` convention)

</details>

<details>
<summary><strong>6. Who re-enters compensation after a restart</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) This feature adds the record and the skip logic only; the recovery entry point is the resume of #1237 and #297 and the retry of #720** | Small, one concern; the record is what every entry point needs | Until one of those lands, only in-process re-entry is protected |
| **B) Also add a recovery processor over `GetStuckSagasAsync` that re-runs compensation** | A crash is actually recovered | Needs the saga definition and data at recovery time (not stored today), a hosted service and its options: that is #1237's design, duplicated here |

### Chosen Option: **A — Record and skip logic only (recommended, pending the maintainer)**

### Rationale

- No code in `src/` re-invokes `CompensateAsync` after a restart today, so the replay guard is only as useful as its entry points; B repeats what the resume plan of #1237 designs.
- **Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Core Models — the record and the key

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Sagas/ISagaState.cs`: add `string? CompensatedStepsJson { get; set; }` and `int Version { get; set; }` (Design Choice 4) with XML docs.
2. `src/Encina.Messaging/Sagas/SagaCompensationKey.cs` (new, internal static): `Create(Guid sagaId, int stepIndex)` returns `$"{sagaId:D}:{stepIndex}:compensate"` (0-based index, Design Choice 2).
3. `src/Encina.Messaging/Sagas/SagaErrorCodes.cs`: `CompensationConflict = "saga.compensation_conflict"`, created with `ErrorCategory.Conflict` (ADR-062); a unit test asserts `GetCategory()` returns `Conflict`.
4. Every other `ISagaState` implementer, found with `git grep ": ISagaState"` across `src/` and `tests/` (14 on origin/main): the 8 provider state classes, `src/Encina.Testing.Fakes/Models/FakeSagaState.cs`, `src/Encina.Testing.Bogus/SagaStateFaker.cs`, `ArbitrarySagaState` in `src/Encina.Testing.FsCheck/ArbitraryMessages.cs`, `BenchmarkSagaState` in `tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks/Infrastructure/BenchmarkEntityFactory.cs` and `.../Encina.Dapper.Benchmarks/Infrastructure/BenchmarkEntityFactory.cs`, and `TestSagaState` in `tests/Encina.UnitTests/Messaging/Sagas/SagaOrchestratorTests.cs`: implement both properties (PR A).
5. `PublicAPI.Unshipped.txt` of `Encina.Messaging` (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina pre-1.0, .NET 10, no backward compatibility. Issue #736 records which compensation steps of a
saga ran. ISagaState is implemented by ADO x3, Dapper x3, EF Core, MongoDB, FakeSagaState, SagaStateFaker,
ArbitrarySagaState, two BenchmarkSagaState and TestSagaState (git grep ": ISagaState").
TASK: Add ISagaState.CompensatedStepsJson, the internal key helper and the SagaErrorCodes entry; update the fake
and the faker.
KEY RULES: XML docs; UTC timestamps use the AtUtc suffix; no [Obsolete]; PublicAPI.Unshipped.txt; zero warnings.
REFERENCE FILES: src/Encina.Messaging/Sagas/ISagaState.cs, src/Encina.Messaging/Sagas/SagaErrorCodes.cs,
src/Encina.Testing.Fakes/Models/FakeSagaState.cs
```

</details>

### Phase 2: Orchestrator and Runner — skip recorded steps, record each step

<details>
<summary><strong>Tasks</strong></summary>

1. `SagaOrchestrator`: new `RecordCompensatedStepAsync(Guid sagaId, int stepIndex, CancellationToken)` reads the state, adds the step with `CompensatedAtUtc = TimeProvider.GetUtcNow()`, writes through `ISagaStore.UpdateAsync` with the version predicate of Design Choice 4 and then `CommitAsync`; a lost race returns `Left(saga.compensation_conflict)` after which the caller re-reads. It does not require the saga to be `Compensating` (the cancel and exception paths call `CompensateAsync` without `StartCompensationAsync`, unlike `CompensateStepAsync`). New `GetCompensatedStepsAsync`, and `CommitAsync(CancellationToken)` (calls `ISagaStore.SaveChangesAsync`, the commit point for EF Core; the runner never touches the store).
2. `SagaRunner.CompensateAsync`: before each delegate, skip a step already in the record (log `StepCompensationSkipped`, count the skip); run the delegate with `requestContext.WithIdempotencyKey(SagaCompensationKey.Create(sagaId, i))`; after success (Design Choice 3, Option A) call `RecordCompensatedStepAsync`. `CompensateAsync` receives the `sagaId`. Record writes and the read use `CancellationToken.None`, like every other persistence call of the runner (`HandleCancelledAsync` passes the already cancelled token to `CompensateAsync`, so a record written with it would fail in exactly the cancellation path); a unit test covers the cancelled-token path.
3. A failed record write is a failure of that step (it is not swallowed): the saga goes to `Failed` through the existing `FailAsync` path with the code.
4. `CompleteCompensationAsync` stays: it still moves the saga to `Compensated`.
5. Keep CRAP at 10 or less on `CompensateAsync` by extracting the per-step body.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: SagaRunner.CompensateAsync runs delegates in reverse order and does not stop on a failure.
SagaOrchestrator persists state through ISagaStore. Phase 1 added CompensatedStepsJson and the key helper.
TASK: Skip recorded steps; pass the key through the request context; record each step after its delegate
succeeds; surface a failed record or a lost race as a Left/Failed saga, never as success.
KEY RULES: TimeProvider only; Either everywhere in the orchestrator; error codes only in persisted text (never
exception messages); a compensation failure does not stop the others (existing behavior).
REFERENCE FILES: src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs, src/Encina.Messaging/Sagas/SagaOrchestrator.cs,
tests/Encina.IntegrationTests/Infrastructure/Sagas/SagaRunnerOutcomeScenarios.cs
```

</details>

### Phase 3: Persistence and Multi-Provider Implementations — 10 database providers

<details>
<summary><strong>Tasks</strong></summary>

For each provider add the `CompensatedStepsJson` and `Version` columns/fields, map them in the state class and factory, and make `UpdateAsync` conditional on the `Version` the state carries (Design Choice 4: increment on success, zero rows affected returns `Left(saga.compensation_conflict)`; on EF Core the check happens at `SaveChangesAsync`; asynchronous calls with `CancellationToken`):

1. ADO.NET: `src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/Sagas/SagaState.cs`, `SagaStateFactory.cs`, `SagaStoreADO.cs`, and `Scripts/003_CreateSagaStatesTable.sql` and the consolidated `Scripts/000_CreateAllTables.sql` (which holds a full second copy of `CREATE TABLE SagaStates`; column `CompensatedStepsJson` as `NVARCHAR(MAX)` / `TEXT` / `LONGTEXT`, `Version` as `INT NOT NULL DEFAULT 0`).
2. Dapper: `src/Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/Sagas/SagaState.cs`, `SagaStateFactory.cs`, `SagaStoreDapper.cs` and the same two scripts per package.
3. Test schemas that duplicate the table DDL (found with `git grep SagaStates`): `tests/Encina.TestInfrastructure/Schemas/SqlServerSchema.cs`, `MySqlSchema.cs` and `PostgreSqlSchema.cs` (inline `CREATE TABLE IF NOT EXISTS sagastates`, created by `PostgreSqlFixture` through `PostgreSqlSchema.CreateSagaSchemaAsync` for ADO and Dapper PostgreSQL), and the benchmark builders `AdoSchemaBuilder.cs` and `DapperSchemaBuilder.cs`; without them the integration databases lack the columns and ADO/Dapper integration fails on `Version`. `RespawnOptions.cs` and the schema-script integration tests only name the table and need no change; the SQLite DDL in `DapperStoreEnlistmentTests.cs` is out of the matrix (ADR-024) and is unchanged unless that test reads the saga store.
4. EF Core (SqlServer, PostgreSQL, MySQL through `SagaStoreEF`): `SagaState.cs`, `SagaStateConfiguration.cs`, `SagaStateFactory.cs`, `SagaStoreEF.cs` (`UpdateAsync` sets `Version = Version + 1` because EF Core does not increment an `int` token itself; `Version` is a concurrency token; `SaveChangesAsync` turns `DbUpdateConcurrencyException` into `Left(saga.compensation_conflict)`).
5. MongoDB: `Sagas/SagaState.cs`, `SagaStateFactory.cs`, `SagaStoreMongoDB.cs` (filter on `Version`).
6. `Encina.Testing.Fakes/Stores/FakeSagaStore.cs`: same conditional semantics.

SQL differences follow AGENTS.md section 5 (SQL Server `@param` and `NVARCHAR`; PostgreSQL case-sensitive identifiers; MySQL backticks).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: AGENTS.md section 5: a store feature is implemented on all 10 database providers with identical
behavior; EF Core is one implementation verified on three databases. Database calls are async with a
CancellationToken.
TASK: Add CompensatedStepsJson and Version to the saga state of every provider and make UpdateAsync conditional
on the Version the state carries, returning saga.compensation_conflict on zero rows (EF Core: at SaveChangesAsync).
KEY RULES: no synchronous database calls; SQL per provider as in AGENTS.md section 5; edit the create scripts
(pre-1.0, no migration helpers); one pull request per provider family (see the split).
REFERENCE FILES: src/Encina.ADO.SqlServer/Sagas/SagaStoreADO.cs, src/Encina.Dapper.PostgreSQL/Sagas/SagaStoreDapper.cs,
src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs, src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary><strong>Tasks</strong></summary>

1. Transactions (function 8): the record is written with the saga state `UpdateAsync` (one write) and committed through `SagaOrchestrator.CommitAsync` (`ISagaStore.SaveChangesAsync`); wrapping a delegate's own work is #720.
2. Idempotency (function 9): the key of Phase 1 and Phase 2.
3. Deferred items listed in the matrix: retry policy of a failed compensation (#720), saga transition audit (#750).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: Phases 1-3 implemented the record, the runner logic and the providers.
TASK: Verify the matrix rows marked Included are implemented; do not implement the deferred ones.
KEY RULES: AGENTS.md section 6; no scope growth; record each deferral with its issue number.
REFERENCE FILES: AGENTS.md section 6, this plan's Cross-Cutting Integration Matrix
```

</details>

### Phase 5: Observability

<details>
<summary><strong>Tasks</strong></summary>

1. `SagaActivitySource.StartSagaCompensate` gets the tag `saga.compensation.skipped_step` when a step is skipped.
2. New `src/Encina.Messaging/Diagnostics/SagaCompensationMetrics.cs` (internal, shape of `MessagePurgeMetrics`): counter `encina.saga.compensation_replay_skipped_total`, tags `saga_type` only.
3. New `[LoggerMessage]` entries, in the class that logs each event (there is no `SagaLog` class; verified: `MessagingSaga` 2200-2299 is used only by `SagaStoreLog.cs`, ids 2200-2205, and the runner's private `Log` class uses 2888-2898 of the `Messaging` range 2800-2999). Store-level conflicts are logged by the stores: 2206 `CompensationConflict` in `SagaStoreLog` (`MessagingSaga`, packed after 2205). The runner events go in the `Messaging` range in the first free ids after the contiguous run that ends at 2970 (verified free on origin/main: 2971 and 2972; re-check before editing, because sibling plans of the family also draw from this range): 2971 `StepCompensationSkipped`, 2972 `CompensationRecordFailed`. No new range; `Encina.Messaging` is already mapped in `EncinaEventIdAllocationTests`.
4. Logs and tags carry saga id and step index only, never the saga data. No tenant tag or attribute is emitted: saga state has no tenant (ADR-054 D4 applies when multi-tenancy is on and the state carries one, which #738 adds).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: ADR-021: EventIds come from a registered range; MessagingSaga is 2200-2299 and 2205 is the highest id
in use on main (re-check before editing).
TASK: Add the counter, the activity tag and three LoggerMessage entries packed sequentially after the highest
id in use.
KEY RULES: EncinaError.Message never reaches logs, tags or metrics; bounded tag dimensions; exceptions through
ForLogging().
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Messaging/Diagnostics/SagaActivitySource.cs,
src/Encina.Messaging/Diagnostics/MessagePurgeMetrics.cs
```

</details>

### Phase 6: Testing — 6 Test Types

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: orchestrator and runner replay scenarios against `FakeSagaStore` (in-process re-entry: a fresh runner over a store that kept the record of the first run, after a delegate ran and before `CompleteCompensationAsync`; replay skips; lost race re-reads; failed record fails the saga; record written with `CancellationToken.None` when the run token is cancelled), `FakeTimeProvider`; `saga.compensation_conflict` carries `ErrorCategory.Conflict`.
2. Guard: new public members of `SagaOrchestrator` and the fake store.
3. Contract: `tests/Encina.ContractTests/EntityFrameworkCore/SagaContractTests.cs` extended, plus the same contract run against the other store families through their harnesses.
4. Property: for any sequence of re-entries each step recorded before a re-entry is not run again by it (FsCheck, real orchestrator and fake store); under Design Choice 3 Option A a step whose delegate ran but whose record failed may run again, which the property states explicitly.
5. Integration: all 10 providers through the shared `[Collection]` fixtures: existing `SagaStore*Tests` classes gain the compensation-record round trip and two concurrent replays (one conflict). On EF Core the test uses two `DbContext` instances that load the same row, so `Version = Version + 1` and the concurrency token are what produce the single conflict. Never a `.md` justification.
6. Load and Benchmark: `.md` justifications (`tests/Encina.LoadTests/Sagas/CompensationIdempotency.md`, `tests/Encina.BenchmarkTests/Sagas/CompensationIdempotency.md`).
7. Manifest targets (`.github/coverage-manifest/{Package}.json`), per touched `src/` file, with the one-sentence justification (floors for the worker; never lowered):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|-----:|------:|---------:|---------:|------------:|---------------|
| `Sagas/LowCeremony/SagaRunner.cs` | 95 | 100 | 0 | 90 | 0 | Every branch of `CompensateAsync` (skip, record, failed record, cancelled token) is reachable with `FakeSagaStore`; no store behavior here. |
| `Sagas/SagaOrchestrator.cs` | 95 | 100 | 0 | 90 | 0 | Pure logic over `ISagaStore`; the conflict and re-read paths run against the fake store. |
| `Sagas/SagaCompensationKey.cs` | 100 | 0 | 0 | 100 | 0 | One pure function; the key format is public behavior and is pinned by a property test. |
| `Diagnostics/SagaCompensationMetrics.cs` | 90 | 100 | 0 | 0 | 0 | Counter wrapper tested with a `MeterListener`. |
| `SagaStoreADO.cs`, `SagaStoreDapper.cs` (x3 each), `SagaStoreMongoDB.cs`, `SagaStoreEF.cs` | 0 | 100 | 100 | 0 | 90 | Database code is covered by the shared-container integration collections and the store contract; unit tests would only restate mocks. |
| `FakeSagaStore.cs`, `FakeSagaState.cs` | 95 | 100 | 100 | 0 | 0 | The fake carries the same version semantics as the real stores and is the base of the unit tests. |

CRAP table of every changed method before each pull request.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: Per-flag coverage; tests execute real package code; Shouldly through Encina.Testing.Shouldly; shared
[Collection] fixtures with ClearAllDataAsync in InitializeAsync.
TASK: Implement the tests listed; write the two justification files; set manifest targets.
KEY RULES: no Thread.Sleep; no per-class database fixture; never lower a target; all 10 providers covered.
REFERENCE FILES: tests/Encina.IntegrationTests/ADO/SqlServer/Sagas/SagaStoreADOTests.cs,
tests/Encina.ContractTests/EntityFrameworkCore/SagaContractTests.cs, tests/Encina.IntegrationTests/Infrastructure/Sagas/SagaRunnerOutcomeScenarios.cs
```

</details>

### Phase 7: Documentation & Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on all new public members; `changelog.d/736-saga-compensation-idempotency.added.md` (via `mechanical-fixer`).
2. `docs/messaging/sagas.md`: the key, the record, the delivery guarantee chosen in Design Choice 3 (by `docs-writer`).
3. ADR recording the decided Dilemmas (1, 3, 4 and 6) once decided (ADR number assigned when the ADR pull request opens).
4. `docs/INVENTORY.md`, `PublicAPI.Unshipped.txt` of every touched package.
5. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its manifest target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT: Documentation follows the encina-docs skill; figures cited with covref, never typed.
TASK: Finish XML docs, changelog, docs/messaging/sagas.md, the ADR and INVENTORY.
KEY RULES: English only; no private client application named; PublicAPI files complete.
REFERENCE FILES: docs/messaging/sagas.md, .claude/skills/encina-docs/SKILL.md, changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | How this feature meets it |
|----------|-------------|---------------------------|
| Saga pattern literature | Compensating actions must be idempotent and retryable | Key passed to the delegate; recorded steps skipped on replay |
| ADR-001, ADR-006 | ROP, no exceptions for business flow | Conflict and record failures are `Left` |
| ADR-043 (reserved by #1251, to be written) | Row claiming by conditional update | Phase 3 predicate on `Version` |
| ADR-062 | Every error code has an `ErrorCategory` | `saga.compensation_conflict` is `Conflict` |
| ADR-021 | EventIds from a registered range | 2206 inside `MessagingSaga`; 2971-2972 inside `Messaging` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| ISagaState / ISagaStore | src/Encina.Messaging/Sagas/ | extended with the compensation record |
| SagaOrchestrator | src/Encina.Messaging/Sagas/SagaOrchestrator.cs | records and reads compensated steps |
| SagaRunner | src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs | skips recorded steps; passes the key |
| IRequestContext.WithIdempotencyKey | src/Encina/Abstractions/IRequestContext.cs | carries the key to the delegate |
| Saga stores of 10 providers | src/Encina.{ADO,Dapper}.*/Sagas/, Encina.EntityFrameworkCore/Sagas/, Encina.MongoDB/Sagas/ | persist the record |
| Saga create scripts | src/Encina.{ADO,Dapper}.*/Scripts/003_CreateSagaStatesTable.sql | new column |
| FakeSagaStore / FakeSagaState | src/Encina.Testing.Fakes | same conditional semantics for unit tests |
| SagaActivitySource | src/Encina.Messaging/Diagnostics/SagaActivitySource.cs | skip tag |
| Saga integration tests | tests/Encina.IntegrationTests/*/Sagas/ | provider matrix |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` | 2206 inside `MessagingSaga` (2200-2299) in `SagaStoreLog`; 2971-2972 inside `Messaging` (2800-2999) in the runner's `Log` class | Highest in use in `MessagingSaga` is 2205; the `Messaging` ids are the first free after the run ending at 2970; re-check both before editing. No new range, no gaps |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core (Messaging, Testing.Fakes, Bogus, FsCheck) | 12 | interface, key helper, error code, orchestrator, runner, metrics, activity source, log class, fake state and store, faker, `ArbitrarySagaState` file |
| Provider implementations | 39 | ADO 9, Dapper 9, EF 5, Mongo 3, 12 SQL scripts (`003_` and `000_` per ADO/Dapper package), 1 EF configuration |
| Tests, test doubles, test schemas | 21 | 13 test additions (unit, guard, contract, property, integration, 2 justification files); `TestSagaState` file and the 2 `BenchmarkEntityFactory.cs` files; `SqlServerSchema.cs`, `MySqlSchema.cs`, `PostgreSqlSchema.cs`, `AdoSchemaBuilder.cs`, `DapperSchemaBuilder.cs` |
| Documentation and metadata | 24 | changelog, sagas guide, ADR, INVENTORY; 10 `PublicAPI.Unshipped.txt` (Messaging, Fakes, 8 providers); 10 coverage manifests |
| **Total** | **96** | Nine pull requests; the per-PR counts below add up to more because a file touched by two pull requests (the Messaging `PublicAPI.Unshipped.txt` and manifest) is counted in each |

### Pull request split

Counts include tests, test doubles, coverage manifests and `PublicAPI.Unshipped.txt`. Under Design Choice 1 Option A the new interface members force every implementer into the same pull request (PR A below). Stated plainly: PR A is about 26 files, over the 15-file limit, and it is a request for the `size-exception-approved` label, linked to Dilemma 1 (the maintainer grants the label or picks Option B, under which no pull request needs it). It is an indivisible, mechanical one-property-per-class change well under 1500 lines. Every other pull request is 15 files or fewer.

| PR | Content | Files (approx.) |
|----|---------|-----------------|
| A (atomic, size exception) | `ISagaState` (+`CompensatedStepsJson`, `Version`) and all 14 implementers: the 8 provider state classes, `FakeSagaState`, `SagaStateFaker`, `ArbitrarySagaState` (`ArbitraryMessages.cs`), the 2 `BenchmarkEntityFactory.cs` files, `SagaOrchestratorTests.cs` (`TestSagaState`); `PublicAPI.Unshipped.txt` of Messaging, Fakes and the 8 provider packages: it builds only as a whole | 26 |
| S1 | Test and ADO schemas: `003_` and `000_` scripts of the 3 ADO packages (6), `SqlServerSchema.cs`, `MySqlSchema.cs`, `PostgreSqlSchema.cs` (shared by ADO and Dapper integration tests), `AdoSchemaBuilder.cs` | 10 |
| S2 | Dapper schemas: `003_` and `000_` scripts of the 3 Dapper packages (6), `DapperSchemaBuilder.cs` | 7 |
| B1 | Core model: key helper, error code, orchestrator (`RecordCompensatedStepAsync`, `GetCompensatedStepsAsync`, `CommitAsync`), `FakeSagaStore` (version semantics), unit, guard and property tests of those, Messaging and Fakes manifests and `PublicAPI.Unshipped.txt` | 12 |
| B2 | Runner and observability: `SagaRunner.CompensateAsync`, metrics, activity tag, `SagaStoreLog` and the runner `Log`, runner and metrics tests, manifest and `PublicAPI.Unshipped.txt` | 9 |
| C | ADO.NET x3: factory, store, integration tests, manifests | 12 |
| D | Dapper x3: factory, store, integration tests, manifests | 12 |
| E | EF Core and MongoDB: configuration, state factory, stores, integration tests, contract tests, manifests | 11 |
| F | Documentation, ADR, load and benchmark justifications, changelog | 6 |

S1 and S2 depend on nothing (nullable and defaulted columns; the stores ignore them until C and D) and may land right after or before A; B1 depends on A; B2 on B1; C, D and E depend on A, B1 and the schema pull requests they use (S1 for C and E's relational tests; S1 and S2 for D, because the Dapper integration tests share the S1 test schemas `SqlServerSchema.cs`, `MySqlSchema.cs` and `PostgreSqlSchema.cs`) and may land in any order; the orchestrator's conflict handling is inert until a provider enforces `Version`. #720 and #2213 change `UpdateAsync` or the same stores too (see Prerequisites): whichever lands second rebases.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
Encina is pre-1.0, .NET 10 / C# 14, nullable enabled, no backward compatibility. Issue #736 makes saga
compensation replay-safe: a per-step record in the saga state, the key {saga-id}:{step-index}:compensate handed
to the delegate through IRequestContext.WithIdempotencyKey, skipped steps on replay, and a version-checked update
on the state row for concurrent replays. All 10 database providers are in scope.

IMPLEMENTATION OVERVIEW:
1. ISagaState.CompensatedStepsJson and Version, SagaCompensationKey, SagaErrorCodes.CompensationConflict
   (ErrorCategory.Conflict), fakes.
2. SagaOrchestrator.RecordCompensatedStepAsync/GetCompensatedStepsAsync/CommitAsync; SagaRunner.CompensateAsync
   skips and records with CancellationToken.None (record after success, pending the maintainer).
3. Every provider: columns/fields, state, factory, version-checked UpdateAsync, create scripts.
4. Observability: counter, activity tag, EventId 2206 in MessagingSaga and 2971-2972 in Messaging.
5. Tests on all six flags with the 10-provider integration matrix; docs and changelog.

KEY PATTERNS:
- ROP; TimeProvider; async database calls with CancellationToken; error codes (never messages) persisted.
- Provider coherence: identical behavior on 10 providers; EF Core one implementation on three databases.
- Registered EventId range first; bounded metric tags; CRAP <= 10; targets never go down.

REFERENCE FILES:
docs/plans/saga-compensation-idempotency-implementation-plan-736.md, src/Encina.Messaging/Sagas/SagaOrchestrator.cs,
src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs, src/Encina.Messaging/Sagas/ISagaState.cs,
src/Encina.ADO.SqlServer/Sagas/SagaStoreADO.cs, src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs,
src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Replay detection needs read-your-writes on the state row; a cache would break it |
| 2 | OpenTelemetry | ✅ | Skipped-replay counter and `saga.compensation.skipped_step` tag (Phase 5) |
| 3 | Structured Logging | ✅ | EventId 2206 (`MessagingSaga`) and 2971-2972 (`Messaging`), both registered ranges (Phase 5) |
| 4 | Health Checks | ❌ | No new checkable dependency |
| 5 | Validation | ❌ | No new input |
| 6 | Resilience | ⏭️ | The retry policy of a failed compensation belongs to #720 (open, same stores) |
| 7 | Distributed Locks | ❌ | Replaced by the version-checked update of Design Choice 4 (Dilemma, recommended A); a lock stays optional |
| 8 | Transactions | ✅ | The record is written with the state update and committed through `SagaOrchestrator.CommitAsync`; atomic wrapping of the delegate is #720 |
| 9 | Idempotency | ✅ | The feature itself (key and record) |
| 10 | Multi-Tenancy | ❌ | Saga state is not tenant-scoped today and this feature adds no `TenantId` |
| 11 | Module Isolation | ❌ | Saga state has no module scoping |
| 12 | Audit Trail | ⏭️ | State-transition audit is #750 (open) |

---

## Prerequisites & Dependencies

| Issue | Relation | Notes |
|-------|----------|-------|
| [#720](https://github.com/dlrivada/Encina/issues/720) | Coordinates | Transactional safety of compensation; same stores, orchestrator and runner, and it also adds `SagaOrchestrator.CommitAsync` and changes the `SagaRunner` constructor: the later pull request rebases (the `CommitAsync` method is defined once, by whichever lands first) |
| [#1237](https://github.com/dlrivada/Encina/issues/1237), [#297](https://github.com/dlrivada/Encina/issues/297) | Coordinate, rebase order: #297, #1237, then this plan (or the reverse, the later rebases) | Both change `ISagaState`/`ISagaStore`, `SagaOrchestrator`, `SagaRunner`, the nine stores and `FakeSagaStore`; #1237 and #297 are also the resume entry points of Design Choice 6 |
| [#2213](https://github.com/dlrivada/Encina/issues/2213) | Shares the `Version` column | Concurrency control on the saga row; the first to land adds `Version` |
| [#750](https://github.com/dlrivada/Encina/issues/750) | Deferred integration | Audit of saga transitions |
| [#733](https://github.com/dlrivada/Encina/issues/733), [#735](https://github.com/dlrivada/Encina/issues/735) | Siblings | Idempotency of notifications and scheduled re-execution; no shared code |

---

## Next Steps

1. At pick-up, re-check the Design Choices against today's code, ask the maintainer Dilemmas 1, 3, 4 and 6 and record the answers in `## Maintainer Decisions`; run `check-plan.ps1` without `-Draft`.
2. Re-check that 2206 (`MessagingSaga`) and 2971-2972 (`Messaging`) are still free when PR B2 starts.
3. Coordinate the `UpdateAsync` change and `CommitAsync` with #720, #2213, #297 and #1237 before PR A.
4. Measure per-flag coverage and the CRAP table for every touched file before each pull request.
