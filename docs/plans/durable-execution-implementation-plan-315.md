# Implementation Plan: Durable Execution / Lightweight Workflow Engine (`Encina.DurableExecution`)

> **Issue**: [#315](https://github.com/dlrivada/Encina/issues/315)
> **Type**: Feature (new package, stores on all 10 database providers)
> **Related**: #303 (closed as a duplicate of this issue), #317 and #392 (process manager, a reactive pattern, not this one), #720 (saga compensation commit owner), #297 (saga state machine), #1257 (default-tenant rule), #1250 (column encryption), ADR-054, ADR-062
> **Complexity**: Very high (10 phases, 16 pull requests including the 8 provider stores)
> **Estimated Scope**: about 147 files and about 13,400 changed lines (about 4,500 of production code), in sixteen pull requests of at most 15 files and 1,500 lines each; the per-PR table in "Estimated File Count" is the authority

---

## Summary

### What exists today (read on 2026-11-27, `origin/main` `b0fba8cf3`)

- There is no `Encina.DurableExecution`, no workflow type and no package in `Encina.slnx` or a manifest. The nearest pieces are the **saga** (`ISagaStore`, `SagaOrchestrator`, the `SagaRunner` low-ceremony runner in `src/Encina.Messaging/Sagas/`) and **scheduling** (`IScheduledMessageStore`, `SchedulerOrchestrator`). `ISagaStore` persists one mutable `SagaState` row (JSON `Data`, `Status`, `CurrentStep`), which is a state snapshot, not an append-only history, so it cannot replay a workflow.
- Stores follow one shape: the interface in `Encina.Messaging`, the implementation inside each provider package in a feature folder (`src/Encina.ADO.SqlServer/Sagas/SagaStoreADO.cs`, `src/Encina.Dapper.*/Sagas/SagaStoreDapper.cs`, `src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs` (one EF package serves SQL Server, PostgreSQL and MySQL), `src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs`), schema scripts in each ADO and Dapper package's `Scripts/` (`031_...` is next on SQL Server and PostgreSQL, `030_...` on MySQL; the number is re-read on the branch).
- `IDistributedLockProvider` exists (`TryAcquireAsync`, `AcquireAsync`, `ExtendAsync`), `TimeProvider` is the clock rule, `IRequestContextScopeFactory.RunRestoredAsync` restores a persisted tenant and identity in background work (`src/Encina/Identity/`), and `MultiTenancyMarker` signals a multi-tenant application.
- The issue's sketches cannot be built as written: `ExecuteActivityAsync(Func<...>)` takes an anonymous lambda with no stable name to match against a recorded history, every call returns a raw value or throws (ROP, `AGENTS.md` section 3), `WorkflowHandle` is a record with method bodies, `DateTime CurrentTime`, and persistence is listed for two providers while `AGENTS.md` section 5 requires all 10 for a database-dependent feature.

### What this plan delivers

- **`Encina.DurableExecution`** (new package, references `Encina` and `Encina.Messaging` only for the shared messaging options): `IDurableWorkflow<TInput, TOutput>`, `IWorkflowContext` (`ExecuteActivityAsync` by name, `ExecuteChildWorkflowAsync`, `SleepAsync`, `WaitForSignalAsync`, `SideEffectAsync`, `CurrentTime`), `IWorkflowRunner` (`StartAsync`, `GetHandleAsync`), `WorkflowHandle<TOutput>` (result, signal, cancel, status), `ActivityOptions`, `WorkflowOptions`, every method returning `Either<EncinaError, T>`.
- **A deterministic replay engine over an append-only history**: the workflow method re-runs from the start after a restart; every durable call is matched against the recorded event at its sequence number, a recorded result is returned without running the activity, and a mismatch is a `Conflict` (non-determinism).
- **`IWorkflowStore`** (events appended with an expected sequence; workflow rows with tenant, status, lease and next-due time; due timers; buffered signals) implemented on **all 10 providers** (ADO.NET x3, Dapper x3, EF Core, MongoDB; EF Core is one package tested on three databases) plus an in-memory reference store for tests and development.
- **A hosted runner** that claims due workflows, resumes them under the persisted tenant and never lets an exception escape its loop (`AGENTS.md` section 3).
- **Observability, health check and EventIds** in a new range.
- **Not delivered**: payload encryption (#1250 owns column encryption; Next Steps), an `IOperationAuditStore` bridge (issue file `artifacts/issues/durable-execution-operation-audit.md`), a visual workflow UI, workflow versioning/patching (Design Choice 2 records the consequence), a Temporal-compatible wire protocol.
- Affected packages: `Encina.DurableExecution` (new), `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina` (EventIds). Provider category: database (10) plus none for the engine.

---

## Design Choices

<details>
<summary><strong>1. Where the stores live and how many providers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Abstractions, engine and in-memory store in `Encina.DurableExecution`; one store per provider inside the 8 existing provider packages in a `DurableExecution/` folder, named `WorkflowStore{Provider}` (`WorkflowStoreADO`, `WorkflowStoreDapper`, `WorkflowStoreEF`, `WorkflowStoreMongoDB`), registered by `AddEncinaDurableExecution{Provider}()`** | The house naming and layout of every other store; all 10 providers; switching provider is a DI change | Each provider package gains a `ProjectReference` to `Encina.DurableExecution` (8 csproj edits, counted per PR) |
| **B) Separate packages `Encina.DurableExecution.EntityFrameworkCore` and `.Dapper` as the issue lists** | Provider code stays out of the provider packages | Covers 2 of 10 providers, which `AGENTS.md` section 5 forbids; 8 more packages for full coverage |

### Chosen Option: **A - Stores in the provider packages, all 10 providers**

### Rationale

- A is the only option that satisfies the provider-coherence rule; the abstractions stay in a package the application opts into (pay-for-what-you-use), and the provider packages reference it, as they already reference `Encina.Messaging`. The 10 stores land one provider per pull request after the engine, so the engine is usable with the in-memory store first.

**Class:** Settled
**Settled by:** `AGENTS.md` section 5 (every provider-dependent feature MUST be implemented for all 10 providers; store naming `{Pattern}Store{Provider}` in section 4)

</details>

<details>
<summary><strong>2. Execution model and non-determinism detection</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Replay with detection: each durable call is keyed by its sequence number and its kind and name (activity name, timer, signal name, child type); on replay a different kind or name at a sequence number is `encina.durable.non_deterministic` (`Conflict`) and the workflow stops, never continues on a wrong history** | Catches a changed workflow definition and non-deterministic code before it corrupts a run | Workflow code must follow the determinism rules (documented: use `context.CurrentTime`, `SideEffectAsync`, no `Guid.NewGuid()`); changing a deployed workflow needs a new type name |
| **B) Replay without detection: return the recorded result of the N-th call whatever it is** | Less code | A changed definition silently returns a result of another call |
| **C) Persisted continuation (serialize the async state machine)** | No determinism rules | Not possible in .NET; rejected |

### Chosen Option: **A - Replay with detection** (recommended, pending the maintainer)

### Rationale

- A wins in every deployment that ever changes a workflow while runs are in flight; B wins only for very short workflows that finish within one deploy, which is not the "days or years" the issue targets. Versioning or patching of in-flight workflows is not built; the consequence (deploy a new type name, drain the old) is documented and a patching API is a separate decision for after 1.0 recorded in Next Steps.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Durable timers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Timer rows in the workflow store: `SleepAsync` and retry delays append a `TimerScheduled` event and set the workflow's `NextDueAtUtc`; the runner polls `GetDueWorkflowsAsync`** | One store, one commit per step; no second store to keep in agreement; works for every provider with the same query | A polling interval (default 5 s) bounds timer precision |
| **B) Reuse scheduled messages (`SchedulerOrchestrator.ScheduleAsync`) to resume the workflow** | Reuses a polled, retried, 10-provider mechanism | Two commits (history and schedule) with a crash window between them, so resume must be idempotent and a lost timer must be detected; couples the engine to messaging scheduling |

### Chosen Option: **A - Timers in the workflow store** (recommended, pending the maintainer)

### Rationale

- A wins because the history and the due time are written in the same store call, so a crash cannot lose a timer; B wins if the maintainer wants one scheduling mechanism for everything and accepts the idempotent-resume work. Time comes from `TimeProvider` everywhere; a fake clock drives the tests.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Preventing duplicate execution of one workflow</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Store fencing: the history has a unique key (TenantId, WorkflowId, Sequence); an append states the expected next sequence, a second runner that appends the same sequence gets `Conflict` and stops; a short lease (`LeaseOwner`, `LeaseExpiresAtUtc`) only keeps two runners from claiming the same due workflow** | Correct even if a lease expires while a runner is slow: the loser cannot write; one mechanism on all 10 providers | The loser may re-run an activity once before it notices (activities are at-least-once, see register row C6) |
| **B) `IDistributedLockProvider` per workflow id** | Reuses the lock providers (7 for 1.0) | A lock that expires mid-run lets two runners write; fencing is still needed, so the lock is redundant; lock providers are not all configured in every deployment |
| **C) A and B together (lock as a claim optimization)** | Fewer wasted resumes | Two mechanisms; needs a lock provider registered |

### Chosen Option: **A - Store fencing** (recommended, pending the maintainer)

### Rationale

- A is sufficient for correctness on every provider; C wins in a deployment with many runners and a lock provider, where it saves duplicate resumes; B alone is wrong. Cross-cutting function 7 is therefore marked as replaced by fencing, not applied.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Tenant scope and identity in background resumes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Tenant on the row: `StartAsync` captures `IRequestContext.TenantId`; when `DurableExecutionOptions.RequireTenantContext ?? _isMultiTenantApplication` (the flag is "`MultiTenancyMarker` is registered") a start with no tenant is `InvalidRequest`; every store call filters by tenant; the unique key is the pair (TenantId, WorkflowId), never a concatenated string; the reserved tenant literal `default` is used for single-tenant applications and a tenant named `default` is `Validation`; the runner resumes through `RunRestoredAsync` with the stored tenant and an anonymous identity (`PersistedIdentitySource.External`)** | Fails closed; injective key; the stored tenant is trusted data written by Encina, not caller input | The workflow's handlers run anonymous: gated handlers inside a workflow need an explicit service identity (documented) |
| **B) Tenant ignored; defer tenancy as the issue does** | Smaller | A shared store would let one tenant signal or read another's workflow; adding the column later changes the schema of 10 stores |

### Chosen Option: **A - Tenant on the row, fail closed** (recommended, pending the maintainer)

### Rationale

- A wins because the schema is cheapest to get right before any store ships; B wins only for an engine that is never multi-tenant. A signal or lookup of another tenant's workflow answers `NotFound`, never `Forbidden`, so existence is not revealed.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | Every public method returns `Either<EncinaError, T>` (`ValueTask` for hot context calls); an activity is `Func<CancellationToken, ValueTask<Either<EncinaError, TResult>>>` and is registered by a stable name (`ExecuteActivityAsync("reserve-inventory", ...)`) because replay matches names, not lambdas | Settled | `AGENTS.md` section 3 (ROP); ADR-001, ADR-006 |
| C2 | Error codes (prefix `encina.durable.`): `invalid_argument` (`Validation`), `reserved_tenant` (`Validation`), `tenant_required` (`InvalidRequest`), `workflow_not_found` (`NotFound`, id supplied by the caller), `workflow_exists` (`AlreadyExists`), `non_deterministic` (`Conflict`), `append_conflict` (`Conflict`), `workflow_cancelled` (`Cancelled`), `activity_failed` (`Internal`, after the retries), `signal_timeout` (`Timeout`), `store_failed` (`Internal`) | Settled | ADR-062 rules R1 to R4; no `FailedPrecondition` before #3420 |
| C3 | History stores an `EncinaError` as code and category only, never `Message`; an exception is stored as its type name | Settled | `AGENTS.md` section 3 (`EncinaError.Message` never reaches plaintext storage) |
| C4 | Time comes from `TimeProvider`; `IWorkflowContext.CurrentTime` is the timestamp of the history event being replayed (deterministic), a `DateTimeOffset` | Settled | `AGENTS.md` section 3 (time comes from `TimeProvider`) |
| C5 | The runner loop and every timer callback catch their failures, log by error code or `ForLogging()` and keep running; a failing workflow fails visibly (status `Failed`, history event) and is not swallowed; `OperationCanceledException` from the stopping token ends the loop | Settled | `AGENTS.md` section 3 (background loops never let an exception escape; errors never swallowed in background infrastructure) |
| C6 | Activities are at-least-once: a crash between running an activity and recording its result runs it again on resume; the guide says activities must be idempotent; the plan never describes activity execution and result recording as atomic | Trivial | Durable-execution model of Temporal and Azure Durable Functions (the issue's references); `AGENTS.md` section 3 |
| C7 | One store call is one commit of its own (`AppendEventsAsync` writes the events and updates the workflow row together); the engine never relies on `SaveChangesAsync` of another component and the EF store saves inside its call | Trivial | `src/Encina.Messaging/Sagas/ISagaStore.cs` contrast: durability needs per-step commits |
| C8 | Telemetry: `ActivitySource` and `Meter` named `Encina.DurableExecution`; counters `encina.durable.workflows` and `encina.durable.activities` (no `_total`) with tags `encina.durable.outcome` and `encina.durable.workflow_type` (a code type name); histogram `encina.durable.workflow.duration` in seconds; tenant tag through the #2676 helper when merged; never the workflow id, tenant id, input or output | Settled | ADR-054 decision 4 and D4 |
| C9 | Payloads (`Input`, activity results, signals) are stored as serialized JSON as given; the application owns their content; encryption is #1250's column-encryption mechanism and is not added here | Trivial | Plan scope; `AGENTS.md` section 11 (follow-up recorded in Next Steps) |
| C10 | `StartAsync` with an existing (tenant, workflow id) returns `workflow_exists`; a caller that wants a duplicate-safe start uses a deterministic id, which is the idempotency key | Settled | ADR-062 R2 (duplicates are `AlreadyExists`) |

---

## Implementation Phases

### Delivery order and size

Sixteen pull requests; each builds and passes alone with its own tests. Counts include csproj, `Encina.slnx`, `Directory.Packages.props`, PublicAPI, manifests, test-project references and the fragment.

| PR | Phase | Content | Files | Lines (est.) |
|----|-------|---------|-------|--------------|
| D1a | 1 | New package skeleton: csproj, `Encina.slnx`, options, error codes, `WorkflowStatus` | 11 | ~550 |
| D1b | 1 | Contracts: workflow, context, runner, handle, options, `IWorkflowStore`, records | 13 | ~800 |
| D2 | 2 | Replay engine, activities and retry, in-memory store, serializer | 10 | ~1,450 |
| D3 | 3 | Timers, signals, hosted runner, tenant gate, `AddEncinaDurableExecution` | 11 | ~1,400 |
| D4 | 4 | Child workflows, cancellation, `WorkflowHandle` bodies | 6 | ~1,000 |
| D5 | 5 | Store contract base (shared by all providers), its in-memory run, property tests | 7 | ~800 |
| D6 | 6 | Observability, health check, EventIds | 14 | ~900 |
| D7 to D12 | 7 | ADO.NET and Dapper stores, one provider per pull request | 9 (D7), 8 each (D8 to D12) | ~700 each |
| D13 | 8 | EF Core store (one package, tests on three databases) | 11 | ~1,000 |
| D14 | 9 | MongoDB store | 8 | ~700 |
| D15 | 10 | Guide, ADR, READMEs, INVENTORY, ROADMAP, changelog | 7 | ~600 |

Order: D1a, D1b, D2, D3, D4, D5, D6; D7 to D14 are independent of each other after D5 (each needs only D1b and the contract base of D5); D15 last. Phase 1 below covers D1a and D1b.

### Phase 1: Package, Contracts and Errors (pull requests D1a and D1b)

<details>
<summary>Tasks</summary>

1. **Create** `src/Encina.DurableExecution/Encina.DurableExecution.csproj` (references `Encina`; `Microsoft.CodeAnalysis.PublicApiAnalyzers`; `InternalsVisibleTo` for UnitTests, GuardTests, ContractTests, PropertyTests, IntegrationTests), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`; **modify** `Encina.slnx` and the UnitTests and GuardTests csproj references (D1a); the ContractTests, PropertyTests and TestInfrastructure references arrive in D5 and the IntegrationTests reference in D7. The contracts, `IWorkflowStore` and the records are D1b.
2. **Create** `IDurableWorkflow.cs`, `IWorkflowContext.cs`, `IWorkflowRunner.cs`, `WorkflowHandle.cs` (sealed class, methods `GetResultAsync`, `SignalAsync`, `CancelAsync`, `GetStatusAsync` over the runner), `ActivityOptions.cs`, `WorkflowOptions.cs`, `WorkflowStatus.cs`, `DurableExecutionErrorCodes.cs` (register row C2), `DurableExecutionOptions.cs` (`RequireTenantContext` as `bool?`, `PollingInterval` 5 s, `LeaseDuration` 60 s, `MaxBatchSize` 20).
3. **Create** `IWorkflowStore.cs` (`StartAsync`, `AppendEventsAsync(tenant, workflowId, expectedSequence, events)`, `ReadHistoryAsync`, `ClaimDueAsync`, `SignalAsync`, `GetStatusAsync`, each `Task<Either<EncinaError, T>>`) and the record types `WorkflowEvent`, `WorkflowRecord`.
4. Manifest `Encina.DurableExecution.json` and the PublicAPI lines through `mechanical-fixer`; unit and guard tests (`WorkflowOptionsTests`, `ActivityOptionsGuardTests`, `DurableExecutionOptionsTests`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #315, plan docs/plans/durable-execution-implementation-plan-315.md, phase 1.

TASK:
1. Create the Encina.DurableExecution project and the public contracts and records listed in the plan.
2. PublicAPI files and the manifest through mechanical-fixer; unit and guard tests.

KEY RULES:
- Every method returns Either<EncinaError, T>; activities are named; no DateTime, only DateTimeOffset; no throws for business failures.
- Error codes carry ErrorCategory per ADR-062 (never FailedPrecondition).
- RequireTenantContext is bool? : null means "follow whether MultiTenancyMarker is registered".
- XML docs with examples on every public member; zero warnings.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/ISagaStore.cs, src/Encina.DistributedLock/Encina.DistributedLock.csproj (small package layout)
- src/Encina.Compliance.Consent/ConsentOptions.cs (RequireTenantContext precedent)
```

</details>

### Phase 2: History, Replay Engine, In-Memory Store and Activities (pull request D2)

<details>
<summary>Tasks</summary>

1. **Create** `src/Encina.DurableExecution/Engine/WorkflowReplayContext.cs` (implements `IWorkflowContext`: cursor over the history, matches kind and name, returns a recorded result or executes and appends), `Engine/ActivityExecutor.cs` (retries with `ActivityOptions`, retry delay recorded as an `ActivityRetryScheduled` event; failure after the retries is `activity_failed`), `Engine/WorkflowEngine.cs` (runs one workflow to its next suspension), `Stores/InMemoryWorkflowStore.cs` (all `IWorkflowStore` members, `TimeProvider`, per-key lock), `WorkflowSerializer.cs` (`System.Text.Json`).
2. Tests: new `tests/Encina.UnitTests/DurableExecution/WorkflowReplayTests.cs` (replay returns recorded results, non-determinism is `Conflict`, retry then success, retry exhausted, a fake clock), `InMemoryWorkflowStoreTests.cs`, `tests/Encina.GuardTests/DurableExecution/WorkflowEngineGuardTests.cs`; manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
Issue #315 plan docs/plans/durable-execution-implementation-plan-315.md, phase 2. D1 merged.

TASK:
1. The replay context, activity executor, engine and in-memory store.
2. Unit and guard tests with a FakeTimeProvider.

KEY RULES:
- Replay matches (sequence, kind, name); a mismatch stops the run with encina.durable.non_deterministic.
- Activities are at-least-once; never call anything atomic. An append states the expected sequence; a conflict is append_conflict.
- Errors stored as code and category only, never EncinaError.Message. No exceptions escape the engine.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/ExponentialBackoffRetryPolicy.cs, src/Encina/Errors/EncinaError.cs
```

</details>

### Phase 3: Timers, Signals, Hosted Runner and Tenant Gate (pull request D3)

<details>
<summary>Tasks</summary>

1. **Create** `Engine/WorkflowRunnerService.cs` (`BackgroundService`: loop `ClaimDueAsync`, resume each workflow in its own scope through `IRequestContextScopeFactory.RunRestoredAsync`, catch per iteration), `Engine/WorkflowRunner.cs` (`IWorkflowRunner`: validates, applies the tenant rule of Design Choice 5, `StartAsync`, `GetHandleAsync`), `Engine/TenantGate.cs`, `ServiceCollectionExtensions.cs` (`AddEncinaDurableExecution(Action<DurableExecutionOptions>?)`, `TryAdd*` for the in-memory store so a database store wins in any order, the hosted service, the options and everything it resolves).
2. Context members `SleepAsync` and `WaitForSignalAsync` in `WorkflowReplayContext` (timer event, `NextDueAtUtc`, buffered signals, timeout as `signal_timeout`).
3. Tests: new unit tests for timers and signals with a fake clock, `TenantGateTests`, a DI test building the provider with `ValidateOnBuild` and `ValidateScopes` (the database store registered before and after the core registration), guard tests for the runner constructor; manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
Issue #315 plan docs/plans/durable-execution-implementation-plan-315.md, phase 3. D1 and D2 merged.

TASK:
1. Timers, signals, the hosted runner, the runner service and the tenant gate; the DI registration.
2. Tests, including the registration-order DI test.

KEY RULES:
- RequireTenantContext ?? multi-tenancy marker present => a start with no tenant is InvalidRequest; never invent "default"; a tenant literally named default is Validation; the store key is the pair (tenant, workflow id).
- A foreign workflow answers NotFound. The runner is a singleton and creates a scope per resume (IServiceScopeFactory); never inject a scoped service into it.
- The loop catches per iteration and never throws; a Left from the store fails that operation visibly.

REFERENCE FILES:
- src/Encina/Identity/IRequestContextScopeFactory.cs, src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs
```

</details>

### Phase 4: Child Workflows, Cancellation and Handle (pull request D4)

<details>
<summary>Tasks</summary>

1. **Modify** `WorkflowReplayContext.cs` (`ExecuteChildWorkflowAsync`: a `ChildStarted` event, the child's own history, the parent resumes on the child's completion event), `WorkflowRunner.cs` (`CancelAsync` appends `CancelRequested`; the next suspension returns `workflow_cancelled`), `WorkflowHandle.cs` (completed bodies).
2. Tests: unit (child result, child failure propagates as `Left`, cancel while sleeping, cancel after completion is a no-op), guard tests for the handle; manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
Issue #315 plan docs/plans/durable-execution-implementation-plan-315.md, phase 4. D1-D3 merged.

TASK:
1. Child workflows, cancellation and the handle operations.

KEY RULES:
- A child belongs to the parent's tenant; a child failure is a Left, never an exception; cancellation is an event, never a thread abort.
- Cancelled is its own category (ADR-062), not Internal.

REFERENCE FILES:
- docs/plans/durable-execution-implementation-plan-315.md (register rows C1, C2, C5)
```

</details>

### Phase 5: Store Contract Base and Property Tests (pull request D5)

<details>
<summary>Tasks</summary>

1. **Create** `tests/Encina.TestInfrastructure/DurableExecution/WorkflowStoreContractBase.cs` (abstract class of store behavior tests: start, append with expected sequence, conflict on a stale sequence, due claim, lease expiry, signals buffered, tenant isolation, history order; every provider integration test inherits it), `tests/Encina.ContractTests/DurableExecution/InMemoryWorkflowStoreContractTests.cs` (runs the base on the in-memory store), `tests/Encina.PropertyTests/DurableExecution/ReplayDeterminismPropertyTests.cs` (FsCheck: replaying a recorded history any number of times yields the same result; two distinct (tenant, workflow id) pairs never address one record).
2. **Modify** test csproj references and manifests.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
Issue #315 plan docs/plans/durable-execution-implementation-plan-315.md, phase 5. D1-D4 merged.

TASK:
1. The abstract store contract base, its in-memory run and the FsCheck replay properties.

KEY RULES:
- Contract and property tests instantiate real implementations; Shouldly through Encina.Testing.Shouldly.
- The base class has no provider code; each provider PR adds one derived class with its own collection fixture.

REFERENCE FILES:
- tests/Encina.ContractTests/EntityFrameworkCore/StoreContractTests.cs
```

</details>

### Phase 6: Observability, Health Check and EventIds (pull request D6)

<details>
<summary>Tasks</summary>

1. **Create** `Diagnostics/DurableExecutionDiagnostics.cs` (register row C8), `Diagnostics/DurableExecutionLog.cs` (`[LoggerMessage]`, ids 12500-12507: 12500 workflow started, 12501 workflow completed, 12502 workflow failed (code), 12503 activity retry scheduled, 12504 non-determinism detected, 12505 runner cycle failed (`ForLogging()`), 12506 tenant gate denied, 12507 fail-open option logged at startup), `Health/DurableExecutionHealthCheck.cs` (`DefaultName` const, `Tags` static array, scoped resolution through `IServiceProvider.CreateScope()`; unhealthy when the store is unreachable or the last runner cycle is older than three polling intervals).
2. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs` (`DurableExecution = (12500, 12549)`), `PublicAPI.Unshipped.txt` of `Encina` and of the package, `docs/engineering/eventid-block-registry.md` (one row in each parsed table, identical bounds), `EncinaEventIdAllocationTests.cs`, the engine files (record), `Encina.json`, `Encina.DurableExecution.json`.
3. Tests: `DurableExecutionDiagnosticsTests` (no id or payload in any tag), `DurableExecutionHealthCheckTests`; run `EventIdRegistryAgreementTests` and `EncinaEventIdAllocationTests`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
Issue #315 plan docs/plans/durable-execution-implementation-plan-315.md, phase 6. D1-D5 merged.

TASK:
1. Diagnostics, log, health check; register EventIdRanges.DurableExecution (12500, 12549) FIRST; registry rows; allocation map.

KEY RULES:
- ADR-021 and ADR-054: ids packed 12500-12507 without gaps, no _total suffix, seconds with unit s; no workflow id, tenant id, input or EncinaError.Message in a tag or log.
- The health check resolves scoped services through a scope; never inject scoped services into a singleton.

REFERENCE FILES:
- src/Encina.Messaging/Health/SagaHealthCheck.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md
```

</details>

### Phase 7: ADO.NET and Dapper Stores (pull requests D7 to D12)

<details>
<summary>Tasks</summary>

Six pull requests, one per provider, in this order: D7 ADO SqlServer, D8 ADO PostgreSQL, D9 ADO MySQL, D10 Dapper SqlServer, D11 Dapper PostgreSQL, D12 Dapper MySQL. Each one:

1. **Create** `DurableExecution/WorkflowStore{ADO|Dapper}.cs` (implements `IWorkflowStore`; every call `OpenAsync` and `Execute*Async` with the `CancellationToken`; SQL uses the provider's own syntax per `AGENTS.md` section 5: SQL Server `@param`, `TOP (@n)`, `bit`; PostgreSQL `LIMIT @n`, `true/false`, quoted case-sensitive identifiers; MySQL `LIMIT @n`, `0/1`, backticks), and `Scripts/{next number}_CreateDurableExecutionTables.sql` (tables `WorkflowExecutions` with `TenantId`, `WorkflowId`, `RunId`, `WorkflowType`, `Status`, `NextDueAtUtc`, `LeaseOwner`, `LeaseExpiresAtUtc`, `StartedAtUtc`, `CompletedAtUtc`; `WorkflowEvents` with the unique key (`TenantId`, `WorkflowId`, `Sequence`); `WorkflowSignals`).
2. **Modify** the package's `ServiceCollectionExtensions.cs` (`AddEncinaDurableExecution{ADO|Dapper}()` with `TryAdd*`, called before the core registration), `PublicAPI.Unshipped.txt`, the package `.csproj` (`ProjectReference` to `Encina.DurableExecution`), `.github/coverage-manifest/{package}.json`.
3. Tests: new `tests/Encina.IntegrationTests/{ADO|Dapper}/{Db}/DurableExecution/WorkflowStore{ADO|Dapper}Tests.cs` (inherits `WorkflowStoreContractBase`, shared `[Collection("{Family}-{Database}")]` fixture, `ClearAllDataAsync` in `InitializeAsync`), `tests/Encina.GuardTests/{ADO|Dapper}/{Db}/WorkflowStoreGuardTests.cs`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT:
Issue #315 plan docs/plans/durable-execution-implementation-plan-315.md, phase 7 (one provider per pull request). D1 and D5 merged.

TASK:
1. WorkflowStore{ADO|Dapper} for the named provider, its schema script, the DI extension, PublicAPI, ProjectReference, manifest.
2. Integration tests inheriting WorkflowStoreContractBase on the shared collection fixture; guard tests.

KEY RULES:
- Asynchronous database calls with the CancellationToken only; the provider's own SQL dialect; the history key is unique on (TenantId, WorkflowId, Sequence); an append states the expected sequence and a duplicate key is append_conflict.
- Errors are stored as code and category only. Every query filters by tenant.
- Never create a per-class fixture; never dispose the shared fixture from a test.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Sagas/SagaStoreADO.cs, src/Encina.ADO.SqlServer/Scripts/003_CreateSagaStatesTable.sql
- tests/Encina.IntegrationTests/ADO/SqlServer/Sagas/SagaStoreADOTests.cs
```

</details>

### Phase 8: EF Core Store (pull request D13)

<details>
<summary>Tasks</summary>

1. **Create** `src/Encina.EntityFrameworkCore/DurableExecution/WorkflowStoreEF.cs`, `WorkflowEntities.cs`, `WorkflowEntityConfiguration.cs` (unique index on the history key); **modify** `ServiceCollectionExtensions.cs` (`AddEncinaDurableExecutionEF()`), `PublicAPI.Unshipped.txt`, `Encina.EntityFrameworkCore.csproj`, `Encina.EntityFrameworkCore.json`.
2. Tests: new `tests/Encina.IntegrationTests/EFCore/{SqlServer,PostgreSQL,MySQL}/DurableExecution/WorkflowStoreEFTests.cs` (three classes inheriting the base, one per `[Collection("EFCore-<Database>")]`), `tests/Encina.GuardTests/EFCore/WorkflowStoreEFGuardTests.cs`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
CONTEXT:
Issue #315 plan docs/plans/durable-execution-implementation-plan-315.md, phase 8. D1 and D5 merged.

TASK:
1. WorkflowStoreEF with entities and configuration, the DI extension, the three database integration test classes.

KEY RULES:
- Save inside each store call (one commit per call); a unique-key violation on the history is append_conflict.
- The DbContext is scoped; the store is registered scoped; the runner reaches it through a scope.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs, src/Encina.EntityFrameworkCore/Sagas/SagaStateConfiguration.cs
```

</details>

### Phase 9: MongoDB Store (pull request D14)

<details>
<summary>Tasks</summary>

1. **Create** `src/Encina.MongoDB/DurableExecution/WorkflowStoreMongoDB.cs`, `WorkflowDocuments.cs` (unique compound index on tenant, workflow id and sequence); **modify** `ServiceCollectionExtensions.cs`, `PublicAPI.Unshipped.txt`, `Encina.MongoDB.csproj`, `Encina.MongoDB.json`.
2. Tests: new `tests/Encina.IntegrationTests/MongoDB/DurableExecution/WorkflowStoreMongoDBTests.cs` (inherits the base), `tests/Encina.GuardTests/MongoDB/WorkflowStoreMongoDBGuardTests.cs`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 9</summary>

```text
CONTEXT:
Issue #315 plan docs/plans/durable-execution-implementation-plan-315.md, phase 9. D1 and D5 merged.

TASK:
1. WorkflowStoreMongoDB, documents, indexes, DI extension, integration and guard tests.

KEY RULES:
- Cancellation tokens on every driver call; the unique index enforces the expected-sequence append; tenant filter on every query.

REFERENCE FILES:
- src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs
```

</details>

### Phase 10: Documentation and Finalization (pull request D15)

<details>
<summary>Tasks</summary>

1. Through `docs-writer`: `src/Encina.DurableExecution/README.md`, the provider README lines, `docs/features/durable-execution.md` (the issue's order-fulfillment example rewritten with named activities and `Either`; determinism rules; activities at-least-once; tenant behavior; choosing a store), the ADR draft (number assigned when the ADR pull request opens), `docs/INVENTORY.md`, `ROADMAP.md`.
2. `changelog.d/315-durable-execution.added.md` through `mechanical-fixer`.
3. Build with zero warnings; every flag reaches its targets; the PR reports measured per-file coverage.

</details>

<details>
<summary>Prompt for AI Agents — Phase 10</summary>

```text
CONTEXT:
Issue #315 plan docs/plans/durable-execution-implementation-plan-315.md, phase 10. All code merged.

TASK:
1. Ask docs-writer for the READMEs, guide, ADR draft, INVENTORY and ROADMAP lines; the fragment through mechanical-fixer.

KEY RULES:
- Documentation never types coverage figures by hand (covref markers); public text never names a private client application.
- The guide states that activities are at-least-once and must be idempotent.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md, docs/features/
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| Temporal documentation, Azure Durable Functions (issue references) | Event-history replay, determinism constraints, durable timers, signals | Engine model, Design Choice 2 |
| `AGENTS.md` sections 3 and 5 | ROP, fail-closed gates, background loops, 10-provider rule, store naming | Register rows, Design Choice 1 |
| ADR-054, ADR-062, ADR-021 | Telemetry, error categories, EventIds | Phase 6, register row C2 |
| #1257 and #388 plans | Reserved `default` tenant segment, injective tenant-qualified keys | Design Choice 5 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ISagaStore` store shape and `SagaStore{Provider}` classes | `src/Encina.Messaging/Sagas/`, provider `Sagas/` folders | Layout and DI pattern to copy |
| `ScheduledMessageProcessor` (polling loop) | `src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs` | Loop shape for the runner |
| `IRequestContextScopeFactory.RunRestoredAsync` | `src/Encina/Identity/IRequestContextScopeFactory.cs` | Tenant restore in the runner |
| `MultiTenancyMarker`, `IRequestContext` | `src/Encina/Abstractions/` | Multi-tenancy detection and tenant id |
| Provider `Scripts/` and collection fixtures | `src/Encina.ADO.*/Scripts/`, `tests/Encina.IntegrationTests/**/Collections.cs` | Schema scripts, shared containers |
| `SagaHealthCheck` | `src/Encina.Messaging/Health/SagaHealthCheck.cs` | Health check pattern |
| `EncinaEventIdAllocationTests`, `EventIdRegistryAgreementTests` | `tests/Encina.UnitTests/Testing/Architecture/` | Range registration |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.DurableExecution` (new field `DurableExecution`) | `12500-12549`, ids 12500-12507 used | Area Core and messaging, first free 50-id slot after this family's `MartenTemporal` (12450-12499, #314) and the claims on main through 12449; provider stores do not log (the engine logs a store `Left` by code); D6 registers it with one row in each parsed table of the registry |

### Estimated File Count

| PR | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| D1a | 6 (csproj, 2 PublicAPI, `DurableExecutionErrorCodes`, `DurableExecutionOptions`, `WorkflowStatus`) | 1 (`Encina.slnx`) | 1 (`DurableExecutionOptionsTests`) | 2 (UnitTests and GuardTests csproj references) | 1 | 0 | 11 | ~550 |
| D1b | 9 (`IDurableWorkflow`, `IWorkflowContext`, `IWorkflowRunner`, `WorkflowHandle`, `ActivityOptions`, `WorkflowOptions`, `IWorkflowStore`, `WorkflowEvent`, `WorkflowRecord`) | 1 (`PublicAPI.Unshipped.txt`) | 2 (`ActivityOptionsTests`, `WorkflowHandleGuardTests`) | 0 | 1 | 0 | 13 | ~800 |
| D2 | 5 (`WorkflowReplayContext`, `ActivityExecutor`, `WorkflowEngine`, `InMemoryWorkflowStore`, `WorkflowSerializer`) | 1 (`PublicAPI.Unshipped.txt`) | 3 | 0 | 1 | 0 | 10 | ~1,450 |
| D3 | 4 (`WorkflowRunnerService`, `WorkflowRunner`, `TenantGate`, `ServiceCollectionExtensions`) | 2 (`WorkflowReplayContext.cs`, `PublicAPI.Unshipped.txt`) | 4 | 0 | 1 | 0 | 11 | ~1,400 |
| D4 | 0 | 3 (`WorkflowReplayContext.cs`, `WorkflowRunner.cs`, `WorkflowHandle.cs`) | 2 | 0 | 1 | 0 | 6 | ~1,000 |
| D5 | 0 | 0 | 3 (contract base, its in-memory run, property tests) | 3 (`Encina.TestInfrastructure`, ContractTests and PropertyTests csproj references) | 1 | 0 | 7 | ~800 |
| D6 | 3 (`DurableExecutionDiagnostics`, `DurableExecutionLog`, `DurableExecutionHealthCheck`) | 5 (`EventIdRanges.cs`, `PublicAPI.Unshipped.txt` of `Encina` and of the package, `WorkflowEngine.cs`, `WorkflowRunnerService.cs`) | 2 | 1 (`EncinaEventIdAllocationTests.cs`) | 2 (`Encina.json`, `Encina.DurableExecution.json`) | 1 (`eventid-block-registry.md`) | 14 | ~900 |
| D7 | 2 | 3 (`ServiceCollectionExtensions.cs`, `PublicAPI.Unshipped.txt`, package csproj) | 2 | 1 (`Encina.IntegrationTests.csproj` reference) | 1 | 0 | 9 | ~700 |
| D8 to D12 (each) | 2 | 3 | 2 | 0 | 1 | 0 | 8 | ~700 |
| D13 | 3 (`WorkflowStoreEF`, `WorkflowEntities`, `WorkflowEntityConfiguration`) | 3 | 4 (three database classes, guard) | 0 | 1 | 0 | 11 | ~1,000 |
| D14 | 2 | 3 | 2 | 0 | 1 | 0 | 8 | ~700 |
| D15 | 0 | 0 | 0 | 0 | 0 | 7 (two READMEs, guide, ADR draft, `INVENTORY.md`, `ROADMAP.md`, fragment) | 7 | ~600 |

Total: 16 pull requests, 11 + 13 + 10 + 11 + 6 + 7 + 14 + 9 + 5 x 8 + 11 + 8 + 7 = 147 files, about 13,400 changed lines. Each store constructor is new, so each store needs a guard test (counted in "New tests"). The provider-package edits are in-scope csproj edits (8 `ProjectReference` additions to `Encina.DurableExecution`, one per provider PR; the EF package once). No existing interface changes: `IWorkflowStore` is new and has these implementers only: `InMemoryWorkflowStore`, `WorkflowStoreADO` x3, `WorkflowStoreDapper` x3, `WorkflowStoreEF`, `WorkflowStoreMongoDB` (11 implementers, all in this plan; verified by the declaration-form grep `class \w+\s*(\([^)]*\))?\s*:\s*[^{]*IWorkflowStore` on the branch at each PR). Provider csproj edits: 8 (one per provider package), each inside its own pull request.

Per-file coverage targets (new package; the manifest is created by D1 with these per-file targets; baseline of the existing store packages is read from their manifests on the branch and never lowered):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `Engine/WorkflowReplayContext.cs` | 90 | 40 | 0 | 70 | 0 | Replay branches are unit tested with a fake clock; the property test replays recorded histories |
| `Engine/ActivityExecutor.cs` | 90 | 40 | 0 | 0 | 0 | Retry and failure branches are table-driven unit tests |
| `Engine/WorkflowEngine.cs` | 85 | 40 | 0 | 0 | 0 | Suspension and completion paths over the in-memory store |
| `Engine/WorkflowRunnerService.cs` | 80 | 60 | 0 | 0 | 0 | Loop iterations with a fake clock; guard covers the constructor |
| `Engine/WorkflowRunner.cs` | 85 | 60 | 0 | 0 | 0 | Start, handle and cancel paths; tenant branches |
| `Engine/TenantGate.cs` | 95 | 0 | 0 | 0 | 0 | Every branch unit tested; no reference-type parameter checked |
| `Stores/InMemoryWorkflowStore.cs` | 90 | 60 | 80 | 0 | 0 | The contract base runs against it |
| `Workflow*Store{ADO|Dapper|EF|MongoDB}.cs` (8 files) | 0 | 60 | 0 | 0 | 85 | SQL and driver behavior needs the real database; the contract base runs on each; guard covers constructor arguments |
| `Health/DurableExecutionHealthCheck.cs` | 85 | 60 | 0 | 0 | 0 | Healthy, unreachable and stale-cycle branches |
| `Diagnostics/DurableExecutionDiagnostics.cs` | 80 | 0 | 0 | 0 | 0 | Listener test; internal static, no validated parameter |
| `Diagnostics/DurableExecutionLog.cs`, `DurableExecutionErrorCodes.cs`, `IWorkflowStore.cs`, `IDurableWorkflow.cs`, `IWorkflowContext.cs`, `IWorkflowRunner.cs` | 0 | 0 | 0 | 0 | 0 | Source-generated log class, constants and interfaces: no executable code of their own |

The provider-store rows state a unit target of 0 because the SQL and driver code only runs against a real database; the integration flag carries the demanding target (85) and the guard flag covers every constructor and method argument.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #315 - durable execution (Encina.DurableExecution) - in small pull requests (D1 to D15), in order.
Plan: docs/plans/durable-execution-implementation-plan-315.md.

PROJECT CONTEXT:
- Encina .NET 10 / C# 14, pre-1.0, no [Obsolete], no compatibility layers. Edit with Edit/Write only; PublicAPI lines, manifests and changelog
  through mechanical-fixer; READMEs, the guide and the ADR through docs-writer.
- All 10 database providers get a store (ADO x3, Dapper x3, EF Core once for three databases, MongoDB); each lives in its provider package.
- Replay model: append-only history, named activities, detection of non-determinism, at-least-once activities, fencing by expected sequence.

IMPLEMENTATION OVERVIEW:
1. D1: package, contracts, options, errors, IWorkflowStore. 2. D2: replay engine, activities, in-memory store.
3. D3: timers, signals, hosted runner, tenant gate, DI. 4. D4: child workflows, cancellation. 5. D5: store contract base and property tests.
6. D6: diagnostics, health check, EventIds 12500-12507. 7. D7-D14: provider stores. 8. D15: documentation.

KEY PATTERNS:
- ROP: Either everywhere; errors never swallowed; EncinaError.Message never stored or logged; ADR-062 categories (never FailedPrecondition).
- Fail closed on tenants; the key is the pair (tenant, workflow id); reserved tenant literal "default"; foreign workflow is NotFound.
- Time through TimeProvider; background loops catch per iteration; never inject a scoped service into a singleton.
- Database calls asynchronous with the CancellationToken; guard tests for every public constructor and method parameter.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/ISagaStore.cs, src/Encina.ADO.SqlServer/Sagas/SagaStoreADO.cs, src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs
- src/Encina/Identity/IRequestContextScopeFactory.cs, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | The history is the cache of activity results (a recorded result is returned without running the activity); no separate cache |
| 2 | OpenTelemetry | ✅ Phase 6 | `Encina.DurableExecution` source and meter, register row C8; never ids or payloads |
| 3 | Structured Logging | ✅ Phase 6 | `DurableExecutionLog`, ids 12500-12507; code and exception type only |
| 4 | Health Checks | ✅ Phase 6 | `DurableExecutionHealthCheck` (store reachable, runner cycle fresh) |
| 5 | Validation | ✅ Phase 3 | `StartAsync` validates arguments and, when registered, runs the input through `ValidationOrchestrator` |
| 6 | Resilience | ✅ Phase 2 | Activity retry with `ActivityOptions`; the retry delay is a durable timer so it survives a restart |
| 7 | Distributed Locks | ❌ N/A | Replaced by store fencing on the unique (tenant, workflow id, sequence) key (Design Choice 4); a lock would not make duplicates safe |
| 8 | Transactions | ❌ N/A | Each store call is one commit of its own (register row C7); activity execution and result recording are not atomic, activities are at-least-once (row C6); `IUnitOfWork` is not involved |
| 9 | Idempotency | ✅ Phase 3 | A deterministic workflow id is the start idempotency key (`workflow_exists`); signals are buffered once per name and sequence |
| 10 | Multi-Tenancy | ✅ Phase 3 | Design Choice 5; the issue deferred it, the plan integrates it because the schema is cheapest to get right now |
| 11 | Module Isolation | ❌ N/A | A workflow row carries no module identifier; the handlers an activity calls apply their own module gates |
| 12 | Audit Trail | ⏭️ | The append-only history is the record of every state transition (event, UTC time, code); a bridge to `IOperationAuditStore` is the issue file `artifacts/issues/durable-execution-operation-audit.md` ([DEBT]) |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Collection fixtures for the 8 provider families | Merged | `tests/Encina.IntegrationTests/**/Collections.cs` |
| `RunRestoredAsync` | Merged | `src/Encina/Identity/` |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Tenant tag helper | #2676 | D6 adds the tenant tag when it has merged |
| Column encryption of payloads | #1250 | Payload protection for personal data in histories |
| Saga compensation commit owner | #720 | Workflows may call saga operations as activities |
| Result union replacing `Either` | #3662 (ADR-064) | Local mapping change only |

---

## Next Steps

1. Link this plan from #315 and ask the maintainer to answer Design Choices 2, 3, 4 and 5 when work starts.
2. Implement D1 to D6 in order; D7 to D14 are independent after D5; D15 last.
3. Follow-up recorded here rather than as FEATURE issues: workflow patching/versioning API, payload encryption through #1250, a Temporal wire-compatible adapter. Maintainer action: amend the acceptance criteria of #315 that name two persistence packages (EF Core and Dapper), anonymous-lambda activities and distributed locks to the outcome of this plan (this plan never comments on an issue).
