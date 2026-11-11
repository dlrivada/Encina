# Implementation Plan: `Encina.Messaging` Saga compensation step replay idempotency

> **Issue**: [#736](https://github.com/dlrivada/Encina/issues/736)
> **Type**: Feature
> **Complexity**: High (7 phases, 10 database providers, ~55 files in 5 pull requests)
> **Estimated Scope**: ~1,200-1,600 lines of production code + ~1,800-2,200 lines of tests

---

## Summary

A compensation that runs twice can refund twice or release stock twice. Today the only compensation progress record is `ISagaState.CurrentStep` (`src/Encina.Messaging/Sagas/ISagaState.cs`). `SagaRunner.HandleStepFailureAsync` persists `Compensating` through `SagaOrchestrator.StartCompensationAsync`, runs every compensation delegate in process (`SagaRunner.CompensateAsync`, reverse order, a failing compensation does not stop the others) and only afterwards records the steps as done with `CompleteCompensationAsync`, which loops `SagaOrchestrator.CompensateStepAsync` (it only decrements `CurrentStep`). A crash between running the delegates and recording them replays every compensation, and a failure moves the saga to `Failed` through `FailAsync` with no per-step record of what already ran.

What it implements:

- A per-step compensation record in the saga state (which step indices were compensated, with `CompensatedAtUtc`), persisted by every `ISagaStore` implementation.
- The key `{saga-id}:{step-index}:compensate`, derived from `SagaId` and the step index and handed to the compensation delegate through `IRequestContext.IdempotencyKey` (`WithIdempotencyKey` already exists), so a handler can deduplicate its external call on it.
- `SagaRunner` skips a step already recorded and records each step per the order chosen in Design Choice 3.
- Concurrent replays of one saga are made safe by a conditional update on the state row (Design Choice 4).
- Telemetry: a skipped-replay counter and a tag on the existing saga activity, plus log entries in the registered saga range.

Standards: the Saga pattern requires idempotent compensating actions (comparable products: payment APIs with idempotency keys, Temporal and Durable Functions replay-safe activities); ADR-001 (ROP); ADR-043 (reserved by the plan of #1251) for the row-claim convention.

Estimated scope: about 55 files (core 8, provider 31, tests 11, docs and metadata 5), shipped as 5 pull requests (see "Pull request split"). Affected packages: `Encina.Messaging`, `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.OpenTelemetry` (`InstrumentedSagaStore` passes the new state through unchanged), `Encina.Testing.Fakes` (`FakeSagaState`).

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

### Chosen Option: **A — A compensation record on the saga state**

### Rationale

- The saga store is the only durable record of the saga; the issue's own analysis settles on the state record. A separate store would add a second consistency boundary.
- Pre-1.0: the column is added to the create scripts `003_CreateSagaStatesTable.sql` (6 SQL scripts), the EF configuration and the Mongo document; no migration helper (AGENTS.md section 3).
- **Class:** Settled
- **Settled by:** AGENTS.md section 5 (every store feature on every provider) and the maintainer-approved issue text of #736 (Alternatives Considered 2)

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
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Concurrent replays of the same saga — conditional update or a distributed lock</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Conditional update of the state row (`WHERE LastUpdatedAtUtc = @expected`, Mongo filter on the same field); the loser re-reads and skips recorded steps** | No lock provider needed (pay-for-what-you-use); same primitive as row claiming | A lost race costs one retry; every provider needs the predicate |
| **B) `IDistributedLockProvider` per saga id around compensation** | Serializes whole compensation | Forces a lock provider on every saga user; lock expiry can still allow two runners |
| **C) Document single-runner as a requirement** | No code | Two workers recovering a stuck saga do exist (`GetStuckSagasAsync`) |

### Chosen Option: **A — Conditional update on the state row (recommended, pending the maintainer)**

### Rationale

- Option B wins where the compensation itself must never overlap even for delegates that ignore the key; A wins everywhere else and keeps locks optional. The row-claiming convention of ADR-043 (reserved by the plan of #1251) uses the same predicate style.
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
- **Class:** Trivial
- **Settled by:** AGENTS.md section 4 (naming: `AtUtc` suffix, descriptive identifiers) and `src/Encina.Messaging/Sagas/SagaErrorCodes.cs` (the `saga.*` convention)

</details>

---

## Implementation Phases

### Phase 1: Core Models — the record and the key

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Sagas/ISagaState.cs`: add `string? CompensatedStepsJson { get; set; }` with XML docs.
2. `src/Encina.Messaging/Sagas/SagaCompensationKey.cs` (new, internal static): `Create(Guid sagaId, int stepIndex)` returns `$"{sagaId:D}:{stepIndex}:compensate"`.
3. `src/Encina.Messaging/Sagas/SagaErrorCodes.cs`: `CompensationConflict = "saga.compensation_conflict"`.
4. `src/Encina.Testing.Fakes/Models/FakeSagaState.cs` and `src/Encina.Testing.Bogus/SagaStateFaker.cs`: implement the property.
5. `PublicAPI.Unshipped.txt` of `Encina.Messaging` (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina pre-1.0, .NET 10, no backward compatibility. Issue #736 records which compensation steps of a
saga ran. ISagaState is implemented by ADO x3, Dapper x3, EF Core, MongoDB, FakeSagaState.
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

1. `SagaOrchestrator`: new `RecordCompensatedStepAsync(Guid sagaId, int stepIndex, CancellationToken)` reads the state, adds the step with `CompensatedAtUtc = TimeProvider.GetUtcNow()`, and writes through `ISagaStore.UpdateAsync` with the conditional predicate of Phase 3; a lost race returns `Left(saga.compensation_conflict)` after which the caller re-reads. New `GetCompensatedStepsAsync`.
2. `SagaRunner.CompensateAsync`: before each delegate, skip a step already in the record (log `StepCompensationSkipped`, count the skip); run the delegate with `requestContext.WithIdempotencyKey(SagaCompensationKey.Create(sagaId, i))`; after success (Design Choice 3, Option A) call `RecordCompensatedStepAsync`. `CompensateAsync` receives the `sagaId`.
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

For each provider add the column/field, map it in the state class and factory, and make `UpdateAsync` conditional on the previous `LastUpdatedAtUtc` (zero rows affected returns `Left(saga.compensation_conflict)`; asynchronous calls with `CancellationToken`):

1. ADO.NET: `src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/Sagas/SagaState.cs`, `SagaStateFactory.cs`, `SagaStoreADO.cs`, and `Scripts/003_CreateSagaStatesTable.sql` (column `CompensatedStepsJson` as `NVARCHAR(MAX)` / `TEXT` / `LONGTEXT`).
2. Dapper: `src/Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/Sagas/SagaState.cs`, `SagaStateFactory.cs`, `SagaStoreDapper.cs` and the same scripts.
3. EF Core (SqlServer, PostgreSQL, MySQL through `SagaStoreEF`): `SagaState.cs`, `SagaStateConfiguration.cs`, `SagaStateFactory.cs`, `SagaStoreEF.cs` (concurrency token on `LastUpdatedAtUtc`).
4. MongoDB: `Sagas/SagaState.cs`, `SagaStateFactory.cs`, `SagaStoreMongoDB.cs` (filter on `LastUpdatedAtUtc`).
5. `Encina.Testing.Fakes/Stores/FakeSagaStore.cs`: same conditional semantics.

SQL differences follow AGENTS.md section 5 (SQL Server `@param` and `NVARCHAR`; PostgreSQL case-sensitive identifiers; MySQL backticks).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: AGENTS.md section 5: a store feature is implemented on all 10 database providers with identical
behavior; EF Core is one implementation verified on three databases. Database calls are async with a
CancellationToken.
TASK: Add CompensatedStepsJson to the saga state of every provider and make UpdateAsync conditional on the
previous LastUpdatedAtUtc, returning saga.compensation_conflict on zero rows.
KEY RULES: no synchronous database calls; SQL per provider as in AGENTS.md section 5; edit the create scripts
(pre-1.0, no migration helpers); one pull request per provider family (see the split).
REFERENCE FILES: src/Encina.ADO.SqlServer/Sagas/SagaStoreADO.cs, src/Encina.Dapper.PostgreSQL/Sagas/SagaStoreDapper.cs,
src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs, src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary><strong>Tasks</strong></summary>

1. Transactions (function 8): the record is written with the saga state `UpdateAsync` (one write); wrapping a delegate's own work is #720.
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
3. New `[LoggerMessage]` entries in `SagaLog` inside `EventIdRanges.MessagingSaga` (2200-2299, highest used 2205): 2206 `StepCompensationSkipped`, 2207 `CompensationRecordFailed`, 2208 `CompensationConflict`. No new range; `Encina.Messaging` is already mapped in `EncinaEventIdAllocationTests`.
4. Logs and tags carry saga id and step index only, never the saga data.

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

1. Unit: orchestrator and runner replay scenarios against `FakeSagaStore` (crash after delegate before record; replay skips; lost race re-reads; failed record fails the saga), `FakeTimeProvider`.
2. Guard: new public members of `SagaOrchestrator` and the fake store.
3. Contract: `tests/Encina.ContractTests/EntityFrameworkCore/SagaContractTests.cs` extended, plus the same contract run against the other store families through their harnesses.
4. Property: for any sequence of crashes and replays each recorded step is compensated at most once (FsCheck, real orchestrator and fake store).
5. Integration: all 10 providers through the shared `[Collection]` fixtures: existing `SagaStore*Tests` classes gain the compensation-record round trip and two concurrent replays (one conflict). Never a `.md` justification.
6. Load and Benchmark: `.md` justifications (`tests/Encina.LoadTests/Sagas/CompensationIdempotency.md`, `tests/Encina.BenchmarkTests/Sagas/CompensationIdempotency.md`).
7. Manifest targets with one-sentence justifications for every touched `src/` file; CRAP table before each pull request.

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
3. ADR recording Design Choices 3 and 4 once decided (next free number at implementation time).
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
| ADR-043 (reserved by the plan of #1251) | Row claiming by conditional update | Phase 3 predicate on `LastUpdatedAtUtc` |
| ADR-021 | EventIds from a registered range | 2206-2208 inside `MessagingSaga` |

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
| `Encina.Messaging` | 2206-2208 inside `MessagingSaga` (2200-2299) | Highest in use on main is 2205; re-check before editing. No new range |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core (Messaging, Testing.Fakes, Bogus) | 8 | interface, key helper, error code, orchestrator, runner, metrics, fake state and store |
| Provider implementations | 31 | ADO 9, Dapper 9, EF 4, Mongo 3, 6 SQL scripts |
| Tests | 11 | unit, guard, contract, property, integration additions, 2 justification files |
| Documentation and metadata | 5 | changelog, sagas guide, ADR, INVENTORY, manifests (PublicAPI per package edited with the code) |
| **Total** | **55** | Five pull requests |

### Pull request split

| PR | Content | Files (approx.) |
|----|---------|-----------------|
| A | Phases 1, 2, 5, fakes, unit, guard and property tests | 14 |
| B | ADO.NET x3: state, factory, store, script, integration tests | 14 |
| C | Dapper x3: state, factory, store, script, integration tests | 14 |
| D | EF Core and MongoDB: state, config, factory, store, integration tests, contract tests | 12 |
| E | Documentation, ADR, load and benchmark justifications, changelog | 6 |

B, C and D depend on A; A depends on #720 only for coordination of the same `UpdateAsync` change (whichever lands second rebases).

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
Encina is pre-1.0, .NET 10 / C# 14, nullable enabled, no backward compatibility. Issue #736 makes saga
compensation replay-safe: a per-step record in the saga state, the key {saga-id}:{step-index}:compensate handed
to the delegate through IRequestContext.WithIdempotencyKey, skipped steps on replay, and a conditional update on
the state row for concurrent replays. All 10 database providers are in scope.

IMPLEMENTATION OVERVIEW:
1. ISagaState.CompensatedStepsJson, SagaCompensationKey, SagaErrorCodes.CompensationConflict, fakes.
2. SagaOrchestrator.RecordCompensatedStepAsync/GetCompensatedStepsAsync; SagaRunner.CompensateAsync skips and
   records (record after success, pending the maintainer).
3. Every provider: column/field, state, factory, conditional UpdateAsync, create scripts.
4. Observability: counter, activity tag, EventIds 2206-2208 in MessagingSaga.
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
| 3 | Structured Logging | ✅ | EventIds 2206-2208 inside the registered `MessagingSaga` range (Phase 5) |
| 4 | Health Checks | ❌ | No new checkable dependency |
| 5 | Validation | ❌ | No new input |
| 6 | Resilience | ⏭️ | The retry policy of a failed compensation belongs to #720 (open, same stores) |
| 7 | Distributed Locks | ❌ | Replaced by the conditional update of Design Choice 4 (Dilemma, recommended A); a lock stays optional |
| 8 | Transactions | ✅ | The record is written with the state update; atomic wrapping of the delegate is #720 |
| 9 | Idempotency | ✅ | The feature itself (key and record) |
| 10 | Multi-Tenancy | ❌ | Saga state is not tenant-scoped today and this feature adds no `TenantId` |
| 11 | Module Isolation | ❌ | Saga state has no module scoping |
| 12 | Audit Trail | ⏭️ | State-transition audit is #750 (open) |

---

## Prerequisites & Dependencies

| Issue | Relation | Notes |
|-------|----------|-------|
| [#720](https://github.com/dlrivada/Encina/issues/720) | Coordinates | Transactional safety of compensation; same stores and orchestrator, so the later pull request rebases |
| [#750](https://github.com/dlrivada/Encina/issues/750) | Deferred integration | Audit of saga transitions |
| [#733](https://github.com/dlrivada/Encina/issues/733), [#735](https://github.com/dlrivada/Encina/issues/735) | Siblings | Idempotency of notifications and scheduled re-execution; no shared code |

---

## Next Steps

1. At pick-up, re-check the Design Choices against today's code, ask the maintainer Dilemmas 3 and 4 and record the answers in `## Maintainer Decisions`; run `check-plan.ps1` without `-Draft`.
2. Re-check that 2206-2208 are still free in `MessagingSaga` when PR A starts.
3. Coordinate the `UpdateAsync` change with #720 before PR B.
4. Measure per-flag coverage and the CRAP table for every touched file before each pull request.
