# Implementation Plan: Saga Visibility — Step History, Query Service and Metrics

> **Issue**: [#128](https://github.com/dlrivada/Encina/issues/128)
> **Type**: Feature
> **Complexity**: High (8 phases, 10 database providers, about 147 files in 14 pull requests)
> **Estimated Scope**: ~1,700 lines of production code + ~4,500 lines of tests, scripts and documentation
> **Family**: the issue also lists a Process Manager and a saga visualizer. The Process Manager is the family of #392 (core), #317 (Marten store, commands) and #296 (decisions); the visualizer is #439. This plan implements only the saga read side and refers to those plans by issue number.

---

## Summary

Operators cannot see what a saga did. Today a saga row says where it is (`CurrentStep`, `Status`) and nothing else: no list of sagas by type or status, no per-step history, no completion statistics. #128 adds:

1. **Step history**: every step the runner executes, compensates, skips or times out is recorded (`ISagaStepStore`), with name, outcome, duration and error code.
2. **A query service** (`ISagaQueryService`): one saga, active sagas, by type, by status, a saga's history, backed by a provider read model (`ISagaQueryStore`).
3. **Metrics**: counts by type and status, completed/failed/compensated today, average and p95 completion time (`SagaMetricsSnapshot`), plus OpenTelemetry instruments.
4. **Configuration and retention**: opt-in switch, history retention, optional step snapshots.

**What exists today** (`origin/main` at `ff9a579c1`, 2026-11-24):

- `ISagaState` has `SagaId`, `SagaType`, `Data`, `Status` (a string), `CurrentStep` (an int), `StartedAtUtc`, `CompletedAtUtc`, `ErrorMessage`, `LastUpdatedAtUtc`, `TimeoutAtUtc` (`src/Encina.Messaging/Sagas/ISagaState.cs`). The six statuses are string constants in `SagaStatus` (`Running`, `Completed`, `Compensating`, `Compensated`, `Failed`, `TimedOut`; `SagaOrchestrator.cs`), not an enum.
- `ISagaStore` has `GetAsync`, `AddAsync`, `UpdateAsync`, `GetStuckSagasAsync`, `GetExpiredSagasAsync`, `SaveChangesAsync` and nothing that lists by type or status. Implementers: `SagaStoreADO` x3, `SagaStoreDapper` x3, `SagaStoreEF`, `SagaStoreMongoDB`, `FakeSagaStore` (`src/Encina.Testing.Fakes/Stores/FakeSagaStore.cs`) and the `InstrumentedSagaStore` decorator (`src/Encina.OpenTelemetry/MessagingStores/`); there is no in-memory default store in `Encina.Messaging`.
- Only `SagaRunner` knows step names (`BuiltSagaDefinition<TData>.Steps`) and can time each one; `SagaOrchestrator.AdvanceAsync` takes a saga id and an optional data function (`SagaRunner.cs` lines 144-275). `new SagaRunner(` has 15 call sites in tests and none in `src/` (DI builds it: `services.AddScoped<ISagaRunner, SagaRunner>()`, `MessagingServiceCollectionExtensions.cs` line 369).
- **The health check item of the issue already exists**: `SagaHealthCheck` reports stuck and expired sagas (`src/Encina.Messaging/Health/SagaHealthCheck.cs`).
- `SagaOptions` (stuck-saga threshold and batch, default timeout) is exposed as `MessagingConfiguration.SagaOptions` (`MessagingConfiguration.cs` line 750), enabled by `config.UseSagas`.
- The purge precedent is `MessagePurgeService : BackgroundService` (`src/Encina.Messaging/Purge/`).

**Corrections to the issue text** (the sketch cannot be built as written):

1. `Task<IEnumerable<T>>` methods become `ValueTask<Either<EncinaError, ...>>` (Railway Oriented Programming, `AGENTS.md` section 3); a missing saga is `Left(saga.not_found)` (existing code, `NotFound`).
2. `SagaStatus` is a class of string constants, so `GetSagasByStatusAsync(SagaStatus)` takes a `string` and `Dictionary<SagaStatus, int>` is `IReadOnlyDictionary<string, long>`.
3. `SagaStep.ErrorMessage` is replaced by `ErrorCode`: an error message must not reach plaintext storage (`AGENTS.md` section 3). `InputSnapshot` and `OutputSnapshot` are opt-in and go through the message serializer (Design Choice 5).
4. `SagaDetails.Data` as `object?` cannot be produced: the row holds serialized data of an unknown type. The details omit it by default (Design Choice 6).
5. `DateTime` + `TimeSpan` mix; timestamps are UTC `DateTime` with the `AtUtc` suffix like the saga row, and the duration is stored in milliseconds.
6. `config.Saga.*` is `config.SagaOptions.*` (the existing property).
7. "Step history recording must be transactional with saga state updates" cannot be promised as written: the orchestrator never calls `SaveChangesAsync`, and the commit owner is `SagaOrchestrator.CommitAsync` introduced by the #720 plan. This plan records the step in the same store family, under the same commit owner as the saga write, and claims no atomicity beyond that (Design Choice 3).
8. Process Manager and visualizer items belong to the sibling plans named above and are not implemented here.

**Standards and references**: NServiceBus ServiceInsight (the issue's reference); Oskar Dudycz, "Saga and Process Manager"; no law applies.

**Affected packages**: `Encina.Messaging` (contract, service, options, runner hook, purge, diagnostics), `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB` (read store, step store), `Encina.Testing.Fakes`, `Encina.OpenTelemetry` (instruments). The issue also ticks `Encina.AspNetCore`; an HTTP surface is deferred (matrix item 12).

**Provider category**: Database (10): the step table and the read queries are provider-specific SQL (`AGENTS.md` section 5).

**Shared pieces and who owns them**: this plan owns `ISagaStepStore`, `ISagaQueryStore`, `ISagaQueryService`, the `saga.invalid_query`, `saga.step_history_disabled` and `saga.query_tenant_required` codes and the EventId range `MessagingSagaVisibility` (12250-12269). The saga family shares the stores and scripts: #738 adds the tenant column (this plan's provider pull requests follow it), #720 owns the commit owner and compensation retry columns, #750 audits transitions (it records to the audit store; this plan records steps for diagnosis; they do not overlap), #1237 adds correlation tables. The saga EventId range 2200-2299 is full (#720 2206-2229, #738 2230-2249, #750 2250-2274, #1237 2275-2299).

---

## Design Choices

<details>
<summary><strong>1. The three other parts of the issue belong to sibling plans (Settled)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Plan only the read side here; the Process Manager is #392, #317, #296 and the visualizer is #439** | One pull request family per concern; the issue's checkboxes map to existing issues | The issue's acceptance list is spread over several plans |
| **B) Plan everything in this issue** | One plan | Duplicates three open issues with their own designs; a single plan of about 400 files |

### Chosen Option: **A — The read side only**

### Rationale

The Process Manager section of #128 is the same pattern as #296, #317 and #392, and its "Visualization Support" is #439; the health check exists. Duplicating them would give the repository two designs for one type.

- **Class:** Settled
- **Settled by:** issues #296, #317, #392 (Process Manager) and #439 (visualizer), and `src/Encina.Messaging/Health/SagaHealthCheck.cs` (stuck-saga detection exists).

</details>

<details>
<summary><strong>2. Where the read queries live (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new read-only `ISagaQueryStore` (list with a keyset page, status counts, completion durations) implemented in each provider package, plus a new `ISagaStepStore`** | Additive: no existing implementer breaks and no PR ordering constraint; opt-in; same pattern as the #1237 correlation store | Two more interfaces and two more classes per provider |
| **B) Add the query methods and the step methods to `ISagaStore`** | One interface | Every implementer (nine stores, `FakeSagaStore`, `InstrumentedSagaStore`, test doubles) must change in the same pull request or land first with defaults; #738, #720 and #1237 already change the same interface |
| **C) Query through the existing methods in memory** | No new SQL | `GetStuckSagasAsync` and `GetExpiredSagasAsync` cannot list by status or type; a full scan per query |

### Chosen Option: **A — A separate read store and a step store** (recommended, pending the maintainer)

### Rationale

- A wins because three sibling plans are rewriting `ISagaStore` and its nine implementers at the same time, and a read model has different needs (indexes, paging) from the write path. B wins when the maintainer prefers one interface and accepts the ordering with #738 and #720.
- The new interfaces are registered only when `config.UseSagaVisibility` is on (pay-for-what-you-use).
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Where a step is recorded (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In `SagaRunner`, after each `AdvanceAsync`, compensation or failure, through an optional trailing constructor parameter `ISagaStepStore?` (null when visibility is off)** | The runner knows the step name and times each step with `TimeProvider.GetTimestamp()`; the 15 existing `new SagaRunner(` sites do not change (optional last parameters) | Callers that drive `SagaOrchestrator` directly (the #297 state machine coordinator, hand-written code) are not recorded |
| **B) In `SagaOrchestrator`, with the step name passed as a new optional argument of `AdvanceAsync`** | Every caller is covered | The orchestrator has no step name or duration; `AdvanceAsync` gains a parameter that every caller must pass to be useful; 20 `new SagaOrchestrator(` sites in tests |
| **C) A decorator over `ISagaStore` that infers a step from each `UpdateAsync`** | No change to runner or orchestrator | Cannot know the step name, the duration or a skipped step; infers from `CurrentStep` only |

### Chosen Option: **A — In `SagaRunner`** (recommended, pending the maintainer)

### Rationale

- A records exactly the sagas run through the low-ceremony runner, which is where step names exist. B wins when direct orchestrator users must be recorded too; the state machine plan (#297) has its own transition history through #750.
- Failure semantics: a `Left` from `RecordStepAsync` fails the operation (Design Choice 7).
- Commit: the runner's recording call runs under the same commit owner as the saga write (`SagaOrchestrator.CommitAsync`, #720); until #720 lands the stores commit per statement (ADO.NET, Dapper) or at the caller's `SaveChangesAsync` (EF Core), and no atomicity is claimed.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. How metrics are computed (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Counts from `GetStatusCountsAsync` (a `GROUP BY` that every database supports); average and p95 computed in .NET over the newest `MetricsSampleSize` completed durations (default 1,000); plus OpenTelemetry instruments written by the runner** | Portable SQL on all ten providers; the same arithmetic everywhere; instruments cover the live view | p95 is over a sample, not over all history |
| **B) Only OpenTelemetry instruments; no `SagaMetricsSnapshot` record** | Least code | No in-process answer for "how many failed today" without a metrics backend |
| **C) Percentile computed in SQL (`PERCENTILE_CONT`, `percentile_cont`, windowing)** | Exact | Different syntax or no support per database (MySQL, MongoDB); three scripts per family |

### Chosen Option: **A — Counts in SQL, percentiles in .NET** (recommended, pending the maintainer)

### Rationale

- A wins on provider coherence (`AGENTS.md` section 5): the difference between databases is the `LIMIT`/`TOP` of one query. B wins when a metrics backend is mandatory; C when exact percentiles over millions of sagas matter more than portability.
- The snapshot is a point-in-time read; "today" is the UTC day of `TimeProvider`.
- **Class:** Dilemma

</details>

<details>
<summary><strong>5. What a recorded step contains (Settled)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `SagaStepRecord(Guid SagaId, int StepIndex, string StepName, DateTime ExecutedAtUtc, SagaStepResult Result, long DurationMs, string? ErrorCode, string? InputSnapshot, string? OutputSnapshot)`; snapshots only when `SnapshotDataOnEachStep` is on, serialized through `IMessageSerializer` (so encryption decorators apply)** | No error message in storage; personal data only on an explicit switch and through the encrypting path | A reader sees an error code, not a sentence |
| **B) The issue's `ErrorMessage` and always-on snapshots** | Readable | `EncinaError.Message` in plaintext storage; saga data copied on every step |

### Chosen Option: **A — Error code, opt-in snapshots through the serializer**

### Rationale

The saga row already stores only the error code (`FailAsync` persists the code, #1469) and serializes data through `IMessageSerializer`; the step record follows the same two rules.

- **Class:** Settled
- **Settled by:** `AGENTS.md` section 3 (`EncinaError.Message` never reaches plaintext storage) and the serializer precedent of `SagaOrchestrator` (issue #1237 plan, serialization path).

</details>

<details>
<summary><strong>6. Whether `SagaDetails` carries the saga data (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `SagaDetails` omits the data; a query returns identifiers, type, status, current step, step counts and timestamps** | The read API cannot leak personal data held in the saga payload; no type resolution | An operator who needs the payload reads it through the store |
| **B) `SagaDetails` carries `string? DataJson` when the caller asks (`includeData: true`)** | One call shows everything | The payload can hold personal data and the read API would expose it to every caller of the service |

### Chosen Option: **A — Omit the data** (recommended, pending the maintainer)

### Rationale

- A wins by default for a service that may later get an HTTP surface. B wins in a trusted internal tool; it would then need an authorization decision (matrix item 12) before shipping.
- **Class:** Dilemma

</details>

<details>
<summary><strong>7. A failure to record a step fails the operation (Settled)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `Left` from `RecordStepAsync` is returned by the runner like any store failure** | Visible; the saga's own error handling (retry, dead letter) applies | A history outage blocks sagas when visibility is on |
| **B) Log the failure and continue** | Sagas never depend on the history | The history silently loses steps |

### Chosen Option: **A — Fail the operation**

### Rationale

History is opt-in; when the operator turns it on, a hole in it is a defect, not a degradation.

- **Class:** Settled
- **Settled by:** `AGENTS.md` section 3 (errors are never swallowed in background infrastructure; a `Left` from a store inside an orchestrator fails that operation).

</details>

<details>
<summary><strong>8. Retention is a background purge by time (Trivial)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `SagaStepHistoryPurgeService : BackgroundService` deletes steps older than `StepHistoryRetention` (default 30 days) in batches, per cycle catching failures and never letting an exception escape** | Same shape as `MessagePurgeService`; idempotent on several nodes | A purge deletes the early steps of a saga that runs longer than the retention |
| **B) Delete the steps with the saga row** | History lives as long as the saga | Sagas are not purged today; the table would grow without bound |

### Chosen Option: **A — A purge service by time**

### Rationale

The issue's `config.Saga.StepHistoryRetention = TimeSpan.FromDays(30)` is a time window, and the repository already has the loop shape for it.

- **Class:** Trivial
- **Settled by:** issue #128 (Configuration: `StepHistoryRetention`), `src/Encina.Messaging/Purge/MessagePurgeService.cs` and `AGENTS.md` section 3 (background loops never let an exception escape).

</details>

---

## Implementation Phases

### Phase 1: Core Models, Errors & Abstractions

> **Goal**: The public contract in `Encina.Messaging`, no behavior.

<details>
<summary>Tasks</summary>

1. **`SagaStepResult`** enum: `Pending, Succeeded, Failed, Compensated, Skipped, TimedOut` (the issue's values, stored as the name).
2. **`SagaStepRecord`** (sealed record, Design Choice 5) and **`ISagaStepStore`**: `ValueTask<Either<EncinaError, Unit>> RecordStepAsync(SagaStepRecord, CancellationToken)`, `ValueTask<Either<EncinaError, IReadOnlyList<SagaStepRecord>>> GetStepsAsync(Guid sagaId, CancellationToken)`, `ValueTask<Either<EncinaError, int>> PurgeOlderThanAsync(DateTime cutoffUtc, int batchSize, CancellationToken)`.
3. **`SagaQuery`** (sealed record: `string? SagaType`, `string? Status`, `bool ActiveOnly`, `string? TenantId`, `int PageSize`, `(DateTime StartedAtUtc, Guid SagaId)? After`), **`SagaPage<T>`** (items plus the next cursor) and **`ISagaQueryStore`**: `ListAsync(SagaQuery, CT)`, `GetStatusCountsAsync(DateTime sinceUtc, CT)` returning `IReadOnlyList<SagaStatusCount>` (`SagaType`, `Status`, `Total`, `SinceCount`), `GetCompletionDurationsAsync(DateTime sinceUtc, int limit, CT)` returning durations in milliseconds, newest first. Keyset paging, never `OFFSET`.
4. **`SagaDetails`** (sealed record: ids, type, status, current step, `StepCount`, `CompletedStepCount`, timestamps; no data, no error text), **`SagaMetricsSnapshot`** (`ActiveSagas`, `CompletedToday`, `FailedToday`, `CompensatedToday`, `AverageCompletionTime`, `P95CompletionTime`, `SagasByType`, `SagasByStatus`) and **`ISagaQueryService`** (`GetSagaAsync`, `ListActiveAsync`, `ListByTypeAsync`, `ListByStatusAsync`, `GetHistoryAsync`, `GetMetricsAsync`, all `ValueTask<Either<EncinaError, ...>>`).
5. **Error codes** in `SagaErrorCodes` with ADR-062 categories: `saga.invalid_query` = `Validation` (page size outside 1-500, unknown status or empty type); `saga.step_history_disabled` = `FailedPrecondition` (history asked while off); `saga.query_tenant_required` = `InvalidRequest` (multi-tenancy on and no tenant in the context; mirrors `tenancy.tenant_required`). A missing saga reuses `saga.not_found` (`NotFound`: a saga addressed by a caller-supplied id, ADR-062 amendment R1). Messages name codes only.
6. **`PublicAPI.Unshipped.txt`** lines and the manifest entries for every new file.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>).
- Issue #128; plan docs/plans/saga-visibility-implementation-plan-128.md. Dilemmas D1-D4 are recommended, pending the maintainer: read the Maintainer Decisions section if it exists and follow the answers.
- SagaStatus is a class of string constants in src/Encina.Messaging/Sagas/SagaOrchestrator.cs, not an enum.

TASK:
Create the contract types, interfaces and error codes of Phase 1 Tasks with unit and guard tests.

KEY RULES:
- Sealed records, XML docs on every public member, nullable enabled, no [Obsolete].
- Every new error code has an ADR-062 ErrorCategory set at creation; messages name codes only, never EncinaError.Message.
- No error text, no saga data in SagaDetails or SagaStepRecord (snapshots only when opted in).
- Keyset paging only.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/ISagaStore.cs
- src/Encina.Messaging/Sagas/SagaErrorCodes.cs
- src/Encina.Messaging/Sagas/ISagaState.cs
```

</details>

---

### Phase 2: Configuration, DI & Runner Hook

> **Goal**: Opt-in switch, options, the service and the recording call.

<details>
<summary>Tasks</summary>

1. **`SagaOptions`** (existing class, `SagaOrchestrator.cs`): add `EnableStepHistory` (default `false`), `StepHistoryRetention` (default 30 days), `SnapshotDataOnEachStep` (default `false`), `MetricsSampleSize` (default 1,000), `StepHistoryPurgeInterval` (default 1 hour) with validation at startup; **`MessagingConfiguration.UseSagaVisibility`** (default `false`, requires `UseSagas`; enforced by startup validation, ADR-050).
2. **`SagaQueryService`** (scoped): validates the query, applies the tenant rule (Phase 4), calls the query store, computes the metrics snapshot (Design Choice 4), uses `TimeProvider` for "today" (the UTC day).
3. **`SagaRunner`** (existing): two optional trailing constructor parameters `ISagaStepStore? stepStore = null, TimeProvider? timeProvider = null`; after each step it builds a `SagaStepRecord` and calls `RecordStepAsync` when the store is not null; a `Left` is returned (Design Choice 7). No existing call site changes.
4. **Registration**: `RegisterSagas` in `MessagingServiceCollectionExtensions.cs` registers `ISagaQueryService` and the purge service when `UseSagaVisibility` is on; the provider extensions register `ISagaStepStore` and `ISagaQueryStore` with `TryAdd*`, so a database store wins in any registration order and never overrides the application's own. A DI test with `ValidateOnBuild` and `ValidateScopes` proves it.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Issue #128, Phase 2. Phase 1 types exist. SagaRunner is built by DI (AddScoped<ISagaRunner, SagaRunner>); 15 tests call new SagaRunner( and must not change.

TASK:
Add the options, UseSagaVisibility, SagaQueryService, the optional runner parameters and the registration, with unit, guard and DI tests.

KEY RULES:
- Optional parameters go last (CS1737); existing call sites compile unchanged.
- A Left from RecordStepAsync is returned by the runner, never logged and dropped.
- Time comes from TimeProvider; no scoped service is injected into a singleton.
- TryAdd* registrations; the DI test builds the provider with ValidateOnBuild and ValidateScopes.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina.Messaging/MessagingConfiguration.cs
```

</details>

---

### Phase 3: Multi-Provider Implementations

> **Goal**: `ISagaStepStore` and `ISagaQueryStore` on all ten providers.

<details>
<summary>Tasks</summary>

Per provider: a step table `SagaSteps` (`SagaId`, `StepIndex`, `StepName`, `ExecutedAtUtc`, `Result`, `DurationMs`, `ErrorCode`, `InputSnapshot`, `OutputSnapshot`, `TenantId`), the indexes `IX_SagaSteps_SagaId_StepIndex` and `IX_SagaSteps_ExecutedAtUtc`, plus `IX_SagaStates_Status_StartedAtUtc` and `IX_SagaStates_SagaType_StartedAtUtc` for the read queries; the store classes `SagaStepStore{Provider}` and `SagaQueryStore{Provider}`; `AddEncinaSagaVisibility{Provider}()` with `TryAdd*`.

| Package | Classes | Notes |
|---------|---------|-------|
| `Encina.ADO.SqlServer` | `SagaStepStoreADO`, `SagaQueryStoreADO` | `@param`, `TOP (@n)`, `bit`; script `00N_CreateSagaStepsTable.sql` and the aggregate `000_CreateAllTables.sql` |
| `Encina.ADO.PostgreSQL` | same | `LIMIT @n`, `true/false`, quoted identifiers |
| `Encina.ADO.MySQL` | same | `LIMIT @n`, `0/1`, backticks |
| `Encina.Dapper.SqlServer`, `.PostgreSQL`, `.MySQL` | `SagaStepStoreDapper`, `SagaQueryStoreDapper` | same SQL through Dapper |
| `Encina.EntityFrameworkCore` | `SagaStepStoreEF`, `SagaQueryStoreEF`, `SagaStepRecordEntity` and configuration | one class for SqlServer, PostgreSQL and MySQL; the step is staged on the `DbContext` and committed by the caller |
| `Encina.MongoDB` | `SagaStepStoreMongoDB`, `SagaQueryStoreMongoDB` | collection `saga_steps`, compound indexes, `$group` aggregation for counts |
| `Encina.Testing.Fakes` | `FakeSagaStepStore`, `FakeSagaQueryStore` | over `FakeSagaStore` for unit tests of consumers |

Every store: async with `CancellationToken` (`OpenAsync`, `ExecuteReaderAsync`), `Left` on infrastructure failure with the exception type only, `TimeProvider` where a timestamp is needed, tenant filter in every query when the saga row has `TenantId` (#738), parameterized SQL.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Issue #128, Phase 3. Run once per provider package, in the pull-request order of the split table. The contract and the options exist.

TASK:
Implement the step store, the query store, the schema script or mapping, the DI extension and the tests of ONE provider package.

KEY RULES:
- Async only with CancellationToken; NEVER the synchronous overloads.
- Keyset paging on (StartedAtUtc, SagaId); no OFFSET; all values are parameters.
- Provider coherence: same behavior on every package; a contract test instantiates each real store.
- Integration tests use [Collection("<Family>-<Database>")] shared fixtures and ClearAllDataAsync in InitializeAsync; never IClassFixture.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Sagas/SagaStoreADO.cs
- src/Encina.Dapper.PostgreSQL/Sagas/SagaStoreDapper.cs
- src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs and SagaStateConfiguration.cs
- src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs
- src/Encina.ADO.SqlServer/Scripts/003_CreateSagaStatesTable.sql
```

</details>

---

### Phase 4: Cross-Cutting Integration

> **Goal**: Apply the matrix items marked integrate.

<details>
<summary>Tasks</summary>

1. **Multi-tenancy (fail closed)**: when the saga row has `TenantId` (#738) and multi-tenancy is enabled, `SagaQueryService` takes the tenant from `IRequestContextAccessor`; a missing tenant returns `saga.query_tenant_required`; every query filters on it; a saga of another tenant is `saga.not_found` (no existence leak). The provider pull requests follow #738's column pull requests.
2. **Validation**: `SagaQuery` is validated before any store call (`saga.invalid_query`).
3. **Idempotency**: `RecordStepAsync` is idempotent by `(SagaId, StepIndex, Result)`: a repeated record of the same step outcome after a retry returns `Right(Unit)` without a second row (unique index).
4. **Health check**: the existing `SagaHealthCheck` is unchanged; a history-store failure surfaces as the saga operation's `Left`.
5. **Retention**: `SagaStepHistoryPurgeService : BackgroundService` runs `PurgeOlderThanAsync` per interval; each cycle catches its failure, logs the error code through `ForLogging()` and keeps running; concurrent nodes are safe because the delete is idempotent.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
- Issue #128, Phase 4. The #738 plan adds ISagaState.TenantId; this phase uses it.

TASK:
Add the tenant rule, validation, the idempotent step record and the purge service, with tests.

KEY RULES:
- Missing tenant with multi-tenancy on denies; another tenant's saga is saga.not_found.
- The purge loop never lets an exception escape; OperationCanceledException from the stopping token ends it.
- No EncinaError.Message, tenant id or saga id in a log message; error code or exception type only.

REFERENCE FILES:
- src/Encina.Messaging/Purge/MessagePurgeService.cs
- src/Encina.Tenancy/TenancyErrors.cs
- src/Encina.Messaging/Health/SagaHealthCheck.cs
```

</details>

---

### Phase 5: Observability

> **Goal**: Instruments, a span and logs under a registered range.

<details>
<summary>Tasks</summary>

1. **EventId range**: register `MessagingSagaVisibility = (12250, 12269)` in `src/Encina/Diagnostics/EventIdRanges.cs` BEFORE any `[LoggerMessage]`; add the registry row, the `PublicAPI.Unshipped.txt` line of `Encina` and the `Encina.Messaging` entry of `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`. Ids packed from 12250: step recorded (12250), step record failed (12251), history purged (12252), purge cycle failed (12253), query denied (12254), metrics snapshot failed (12255).
2. **`SagaVisibilityLog`** (`[LoggerMessage]`) in `src/Encina.Messaging/Diagnostics/`: the saga type and the error code only; never a saga id, tenant id, step name from data, snapshot or `EncinaError.Message`.
3. **Instruments** in the existing `Encina.Messaging` meter: `Counter<long> encina.saga.steps` (tags saga type, step result), `Histogram<double> encina.saga.step_duration` (ms), `Histogram<double> encina.saga.completion_time` (ms), `ObservableGauge<long> encina.saga.active` fed by the status counts; no `_total` suffix; the tenant tag only when multi-tenancy is on (ADR-054 D4). The saga type is bounded by the registered definitions; step names are not a tag.
4. **Span**: `encina.saga.query` in the existing `Encina.Messaging.Saga` source with the query kind.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
- Issue #128, Phase 5. The family block is 12250-12299 (the first free 50-id slot after the #4007 reservations and the #340 claim); this plan owns 12250-12269, the Process Manager family 12270-12289.

TASK:
Register the range, add the log class, the instruments and the span, instrument the runner and the service, and test them with a listener.

KEY RULES:
- Register in EventIdRanges.cs, the registry page, PublicAPI.Unshipped.txt and AssemblyRanges BEFORE using an id; ids packed from 12250 without gaps.
- Instruments have no _total suffix; tags are bounded; no raw tenant, saga or user id in a tag or log.
- Run EncinaEventIdAllocationTests and EventIdRegistryAgreementTests.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/SagaActivitySource.cs
- src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/engineering/eventid-block-registry.md
```

</details>

---

### Phase 6: Testing

> **Goal**: Every flag at its target.

<details>
<summary>Tasks</summary>

| Flag | Scope | Per-file target (justification) |
|------|-------|---------------------------------|
| Unit | `SagaQueryService`, the metrics arithmetic, runner recording, purge cycle, options validation, fakes | `SagaQueryService.cs` 90, `SagaStepHistoryPurgeService.cs` 85, `SagaRunner.cs` 85 (pure logic and a substituted store) |
| Guard | every public constructor and method with parameters, the DI extensions | `SagaQueryService.cs` 90, each provider DI extension 90 (null and empty arguments are the only branches) |
| Contract | `ISagaStepStore` and `ISagaQueryStore` against the fakes and each real provider store | each provider store 60 (the contract exercises record, list, count, purge) |
| Property | p95 and average are within the sample's bounds for any duration list (FsCheck); keyset paging returns every row once | `SagaQueryService.cs` 70 (one invariant per property over generated lists) |
| Integration | every provider on a real database through shared `[Collection]` fixtures: record, list by type and status, counts, purge | each provider store 70 (real SQL and indexes) |

Load: a justification `.md` (history is one insert per step; the contention is the saga row, covered by #2213). Benchmark: a justification `.md` (the read model is not a hot path).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```
CONTEXT:
- Issue #128, Phase 6.

TASK:
Write the tests of the table and the two justification files.

KEY RULES:
- Tests execute real code; Shouldly through Encina.Testing.Shouldly; FakeTimeProvider; no Thread.Sleep.
- Tests of a provider-dependent feature cover all 10 database providers.

REFERENCE FILES:
- tests/Encina.UnitTests/Messaging/Sagas/SagaRunnerTests.cs
- tests/Encina.GuardTests/Messaging/Sagas/SagaRunnerGuardTests.cs
- .github/coverage-manifest/Encina.Messaging.json
```

</details>

---

### Phase 7: Documentation & Finalization

> **Goal**: The mandatory documentation set.

<details>
<summary>Tasks</summary>

1. XML docs on every public member; `changelog.d/128-saga-visibility.added.md`.
2. `docs/features/saga-visibility.md` (enabling it, querying, metrics, retention, privacy of snapshots), the `Encina.Messaging` README, `docs/INVENTORY.md`, `ROADMAP.md`.
3. An ADR for the separate read store (number assigned when the ADR pull request opens).
4. `PublicAPI` files; `dotnet build --configuration Release` with zero warnings; per-flag coverage measured against the manifests.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```
CONTEXT:
- Issue #128, Phase 7. Delegated to the docs-writer agent for pages under docs/ and READMEs.

TASK:
Write the feature guide, README additions, the changelog fragment and the INVENTORY and ROADMAP lines.

KEY RULES:
- No hand-typed coverage figures; cite cov: references.
- Public text never names a private client application.

REFERENCE FILES:
- docs/plans/dsr-implementation-plan-404.md
- changelog.d/README.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

Every count includes the tests, manifest edits, PublicAPI lines and `ProjectReference` edits of its own code; each pull request builds and passes alone. The new interfaces are additive, so no pull request leaves the build broken.

| PR | Content | Files | Lines |
|----|---------|-------|-------|
| 1 | Phase 1 contract: 8 new types and interfaces, `Encina.Messaging` PublicAPI, manifest, 1 unit test, 1 guard test | 12 | 700 |
| 2 | Phase 2 options and wiring: `SagaOptions`, `MessagingConfiguration` flag, `SagaErrorCodes` (3 modified), `SagaRunner` hook (modified), `SagaQueryService`, registration (modified), PublicAPI, manifest, 3 unit tests, DI proof, guard | 13 | 1,200 |
| 3 | Phase 3 fakes: `FakeSagaStepStore`, `FakeSagaQueryStore`, PublicAPI, manifest, unit test, contract test | 6 | 500 |
| 4 | Phase 5 observability: log class, instruments, span, `EventIdRanges.cs`, registry page, `AssemblyRanges` test, `Encina` PublicAPI, 2 manifests, 2 unit tests | 12 | 800 |
| 5 | Phase 4 purge and idempotency: `SagaStepHistoryPurgeService`, 2 unit tests, property test, `PropertyTests` project reference, PublicAPI, manifest | 7 | 700 |
| 6-8 | Phase 3 ADO.NET: one pull request per provider: 2 stores, script, aggregate script, DI line, PublicAPI, manifest, unit, guard, integration test, contract registration | 12 each | 1,000 each |
| 9-11 | Phase 3 Dapper: one pull request per provider, same shape | 12 each | 1,000 each |
| 12 | Phase 3 EF Core: 2 stores, entity, configuration, DI line, PublicAPI, manifest, unit, guard, 3 integration classes | 14 | 1,300 |
| 13 | Phase 3 MongoDB: 2 stores, indexes, DI line, PublicAPI, manifest, unit, guard, integration | 9 | 800 |
| 14 | Phase 7 documentation: guide, README, INVENTORY, ROADMAP, ADR, changelog fragment, load and benchmark justification files | 8 | 600 |

Total: 14 pull requests, about 147 files (upper estimate); no pull request exceeds 15 files or 1,500 lines. Pull requests 6 to 13 follow #738's column pull requests and are independent of each other.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| NServiceBus ServiceInsight | Saga visualization and query for operators | The issue's motivation and a named comparable |
| Oskar Dudycz, "Saga and Process Manager" | Saga versus Process Manager | Why the Process Manager part is the #392 family |
| Azure Architecture Center, Saga pattern | Orchestrated sagas and compensation | Terminology |
| ADR-001, ADR-006 | Railway Oriented Programming | `Left` for every failure |
| ADR-018, ADR-021, ADR-054 D4, ADR-062 | Cross-cutting check, EventIds, tenant tag, error categories | Matrix, range, telemetry, codes |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ISagaStore` and `ISagaState` | `src/Encina.Messaging/Sagas/ISagaStore.cs`, `ISagaState.cs` | The write model; the row the read model queries; nine implementers |
| `SagaStatus`, `SagaOrchestrator` | `src/Encina.Messaging/Sagas/SagaOrchestrator.cs` | The six status strings; the orchestrator never calls `SaveChangesAsync` |
| `SagaRunner` | `src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs` | The only caller that knows step names and can time each step; place of the recording hook |
| `SagaHealthCheck` | `src/Encina.Messaging/Health/SagaHealthCheck.cs` | Already reports stuck and expired sagas; unchanged |
| `SagaActivitySource` and `MessagingStoreMetrics` | `src/Encina.Messaging/Diagnostics/` | Existing source `Encina.Messaging.Saga` and meter with the saga transition counter |
| `InstrumentedSagaStore` | `src/Encina.OpenTelemetry/MessagingStores/InstrumentedSagaStore.cs` | Decorator over `ISagaStore`; not changed (the new stores are separate interfaces) |
| `SagaOptions` | `src/Encina.Messaging/Sagas/SagaOrchestrator.cs` | Existing options exposed as `MessagingConfiguration.SagaOptions` |
| `MessagePurgeService` | `src/Encina.Messaging/Purge/MessagePurgeService.cs` | Shape of the retention loop |
| `SagaStateConfiguration` and `003_CreateSagaStatesTable.sql` | `src/Encina.EntityFrameworkCore/Sagas/`, `src/Encina.ADO.SqlServer/Scripts/` | Mapping and schema-script pattern for the step table |
| `FakeSagaStore` | `src/Encina.Testing.Fakes/Stores/FakeSagaStore.cs` | Base for the fake read and step stores |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Registry for the new range |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (saga store) | 2200-2299 | Full: #720 2206-2229, #738 2230-2249, #750 2250-2274, #1237 2275-2299 |
| `Encina` area Core and messaging, reserved by #4007 | 10000-12199 | Migration targets of existing ranges; not allocatable |
| Claimed by #340 (open plan branch) | 12200-12249 | `RequestEnrichment` |
| **`Encina.Messaging` (saga visibility)** | **12250-12269** | **New field `MessagingSagaVisibility = (12250, 12269)`; this plan uses 12250-12255** |
| Process Manager family (#392, #317, #296) | 12270-12289 | `ProcessManager` |
| Spare | 12290-12299 | Unassigned |

### Provider Matrix

| Provider category | Applies | Reason |
|-------------------|---------|--------|
| Database (10) | Yes | Step table and read queries: ADO.NET x3, Dapper x3, EF Core (3 databases), MongoDB |
| Caching, Transport, Locks, Validation, Cloud | No | None (a cache of query results is deferred, matrix item 1) |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| `Encina.Messaging` source (new or modified) | ~22 | Contract, options, service, runner hook, purge, diagnostics, DI |
| Provider stores and scripts | ~44 | Eight packages: two stores, entity or mapping, script, aggregate script, DI line |
| Fakes, registry and manifests | ~16 | Fakes, `EventIdRanges.cs`, registry page, `AssemblyRanges` test, manifests, PublicAPI |
| Tests | ~57 | Unit, guard, contract, property, DI, integration (eight classes), justification files |
| Documentation | ~8 | Guide, README, INVENTORY, ROADMAP, ADR, changelog fragment |
| **Total** | **~147 in 14 pull requests** | Equals the sum of the split table |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Railway Oriented Programming (Either<EncinaError, T>); no [Obsolete]; XML docs on every public API; zero warnings.
- Issue #128: saga visibility (step history, query service, metrics). The Process Manager and the visualizer of the issue are other plans (#392 family, #439). Plan: docs/plans/saga-visibility-implementation-plan-128.md. Dilemmas D1-D4 are recommended, pending the maintainer; read the Maintainer Decisions section first if it exists.

IMPLEMENTATION OVERVIEW:
1. Phase 1: contract (SagaStepRecord, ISagaStepStore, SagaQuery, ISagaQueryStore, SagaDetails, SagaMetricsSnapshot, ISagaQueryService, three error codes).
2. Phase 2: SagaOptions additions, UseSagaVisibility, SagaQueryService, optional runner parameters, registration.
3. Phase 3: step and query stores on ADO x3, Dapper x3, EF Core, MongoDB, plus fakes.
4. Phase 4: tenant rule, validation, idempotent step record, purge service. Phase 5: range 12250-12269, log class, instruments, span.
5. Phase 6: tests. Phase 7: documentation.

KEY PATTERNS:
- SagaStatus is a class of string constants; the orchestrator never calls SaveChangesAsync; SagaOrchestrator.CommitAsync (#720) is the commit owner; claim no atomicity.
- Steps carry an error code, never EncinaError.Message; snapshots are opt-in and go through IMessageSerializer.
- Optional trailing constructor parameters; the 15 new SagaRunner( sites and 20 new SagaOrchestrator( sites do not change.
- Keyset paging, parameterized SQL, async with CancellationToken, TimeProvider for every timestamp.
- Missing tenant with multi-tenancy on denies; another tenant's saga is saga.not_found.
- No _total suffix on instruments; no raw tenant, saga or user id in tags or logs; EventIds registered first and packed.
- Tests: Shouldly through Encina.Testing.Shouldly, real implementations, [Collection] fixtures, per-flag targets, CRAP <= 10 on changed methods.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/ISagaStore.cs
- src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs
- src/Encina.Messaging/Purge/MessagePurgeService.cs
- src/Encina.ADO.SqlServer/Sagas/SagaStoreADO.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/plans/dsr-implementation-plan-404.md (style)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Deferred (as the issue says): short-lived caching of query results; the saga store cache is #696; issue file `artifacts/issues/saga-visibility-query-cache.md` |
| 2 | OpenTelemetry | ✅ | Phase 5: counters, histograms, gauge and the `encina.saga.query` span in the existing `Encina.Messaging` source and meter; tenant tag only when multi-tenancy is on |
| 3 | Structured Logging | ✅ | Phase 5: `SagaVisibilityLog` in 12250-12269 (registered first); saga type and error code only |
| 4 | Health Checks | ❌ | Not applicable as new work: the issue's stuck-saga check already exists as `SagaHealthCheck` |
| 5 | Validation | ✅ | Phase 4: `SagaQuery` validated (`saga.invalid_query`); the issue marked it not applicable because the API is read-only, but a page size and a status are caller input |
| 6 | Resilience | ❌ | Not applicable: local database reads and writes; retry of a failed step record belongs to the saga's own error handling |
| 7 | Distributed Locks | ❌ | Not applicable: queries are read-only; the purge is an idempotent delete by cutoff, safe on several nodes |
| 8 | Transactions | ✅ | Phase 2: the step is recorded under the same commit owner as the saga write (`SagaOrchestrator.CommitAsync`, #720); until then the stores commit per statement or at the caller's `SaveChangesAsync`; no atomicity beyond that is claimed |
| 9 | Idempotency | ✅ | Phase 4: a repeated step record of the same `(saga, step, result)` returns `Right(Unit)` through a unique index |
| 10 | Multi-Tenancy | ✅ | Phase 4: tenant from `IRequestContextAccessor`, filter in every query, deny on a missing tenant; depends on #738 for the column (the issue deferred it; a read API without it would expose other tenants) |
| 11 | Module Isolation | ⏭️ | Deferred: `ModuleId` on messaging entities is #747; the read model gains the filter when it lands |
| 12 | Audit Trail | ⏭️ | Deferred: audit of saga transitions is #750; reads of the history API, an HTTP surface and its authorization are `artifacts/issues/saga-visibility-http-surface.md` |

---

## Prerequisites & Dependencies

- **#738** (tenant column and tenant-aware lookups): the provider pull requests (6-13) follow its column pull requests; pull requests 1-5 do not need it.
- **#720** (commit owner `SagaOrchestrator.CommitAsync`): advisable before the runner hook is used on EF Core; the plan claims no atomicity without it.
- **#1237** and **#750** change the same stores and scripts; each plan rebases onto the previous in merge order, and numbered script names are assigned when the pull request opens.
- The Process Manager (#392 family) and the visualizer (#439) are independent of this plan.

---

## Next Steps

1. The maintainer answers Dilemmas D1 to D4 when work on #128 starts; the answers go into `## Maintainer Decisions` at the end of this plan.
2. Open the two deferred issues named in the matrix and link them from #128.
3. Brief pull request 1 first; pull requests 2 to 5 follow in order, then the provider pull requests after #738's columns, then 14.
