# Implementation Plan: Blue-Green Projection Rebuild (Zero-Downtime) for `Encina.Marten`

> **Issue**: [#324](https://github.com/dlrivada/Encina/issues/324)
> **Type**: Feature
> **Complexity**: High (5 phases, 4 PRs, one provider, a feasibility spike first)
> **Estimated Scope**: 45 files and about 3,700 changed lines over 4 PRs (the table in "Estimated File Count" lists every file per PR; the figures assume the recommended answers to the Dilemmas)

---

## Summary

`IProjectionManager.RebuildAsync<TReadModel>` (`src/Encina.Marten/Projections/IProjectionManager.cs`, implemented by `MartenProjectionManager`) rebuilds in place: with the default `RebuildOptions.DeleteExisting = true` it first runs `deleteSession.DeleteWhere<TReadModel>(_ => true)` and then replays `QueryAllRawEvents()` in batches, so the read model is empty or partial while it runs. The progress lives in an in-memory `ConcurrentDictionary<string, ProjectionStatus>`; there is no persisted rebuild state, no catch-up notion and no strategy in `ProjectionOptions` (`Enabled`, `UseInlineProjections`, `RebuildBatchSize`, `AutoRebuildOnStartup`, `ThrowOnProjectionError`). This plan adds a **blue-green rebuild**: replay into a green copy while blue serves reads, catch up with the live event store, then swap atomically.

**What it implements**

- `IProjectionRebuildManager` (`StartRebuildAsync`, `GetStatusAsync`, `SwitchToGreenAsync`, `AbortAsync`, all `Either<EncinaError, T>`) with `RebuildOperation`, `RebuildStatus`, `RebuildPhase` and `RebuildStrategy` (shape per Dilemma D1).
- `BlueGreenRebuildManager`: creates the green copy (isolation per D2), replays history in batches, tails the event store until the lag is under `CatchUpThreshold` (D3), then switches manually or automatically (`AutoSwitchOnCatchUp`).
- Rebuild state persisted as a Marten document so a restart resumes or reports correctly; one rebuild per projection at a time under `IDistributedLockProvider`.
- A rebuild-status `IEncinaHealthCheck`, diagnostics and tests. The CLI command is tracked by the open issue #2611 (Encina.Cli Marten projection rebuild and stream inspection); audited operator actions are a follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/projection-rebuild-audit.md); the issue's "Marten async daemon integration" task is decision 11.

**Standards**: AGENTS.md section 3 (ROP, background loops never throw), section 6 (12 functions), ADR-018, ADR-021, ADR-027, ADR-052 (lock outcomes), ADR 062 (error categories, merged in PR #3599).

**Affected packages**: `Encina.Marten` (code and manifest; no `PublicAPI.Unshipped.txt` exists for it on main, so no PublicAPI lines); a new project reference from `Encina.Marten` to `Encina.DistributedLock` (which references only `Encina` and `Encina.Messaging`, so no cycle). `Encina.Cli` is not changed in this plan.

**Provider category**: event sourcing (Marten, AGENTS.md section 5). The 10-database rule does not apply to the Marten read-model store; integration tests need Marten on PostgreSQL through Testcontainers.

**Size and split (AGENTS.md section 11)**: 45 files and about 3,700 lines exceed one worker PR, so the work ships as four PRs of at most 15 files and 1,500 lines each. Each PR adds its own manifest entries (per file, per flag, one-sentence justification) with the tests that meet them; no PR uses a type, log class or project of a later PR, so the log class, metrics and EventIds are created in PR 2, the first PR that logs. The file lists per PR are in "Estimated File Count".

| PR | Content | Files | Lines (estimate) |
|----|---------|-------|------------------|
| 1 | Phase 1: models, `IProjectionRebuildManager`, the rebuild document, options, error codes, tests, the feasibility spike test | 14 | about 1,000 |
| 2 | Phase 2 and the diagnostics of Phase 4: `BlueGreenRebuildManager`, shared applier, log, metrics, EventIds, tests | 15 | about 1,500 (if over, the integration test moves to PR 3: 14 and 13 files) |
| 3 | Phase 3: worker, DI, health check, restart and load tests | 12 | about 900 |
| 4 | Documentation and changelog | 4 | about 300 |

PR 2 starts only after the maintainer has answered D2 (and D3): it implements whichever isolation the spike of PR 1 and the answer settle.

---

## Design Choices

<details>
<summary><strong>1. API shape (issue Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Extend `IProjectionManager.RebuildAsync` with a strategy | One entry point | A larger interface; the status model of a long operation (phases, lag, switch, abort) does not fit `long` results |
| B. A separate `IProjectionRebuildManager` | Focused API with its own status model; `IProjectionManager` keeps the in-place rebuild | Two ways to rebuild |

### Chosen Option: **B — separate `IProjectionRebuildManager`** (recommended, pending the maintainer)

### Rationale

`IProjectionManager.RebuildAsync` returns the number of replayed events and blocks until done, while blue-green is a start, observe, switch or abort lifecycle. A wins if users should never choose between two APIs and the maintainer prefers `RebuildOptions.Strategy = BlueGreen` with a returned handle.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. How green is isolated from blue (issue Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Second PostgreSQL schema (`GreenSchemaPrefix`), swapped by moving the table in one transaction | Clean drop on abort; after the swap the document table keeps its name, so no node needs to learn a pointer | Needs DDL rights to create schemas; Marten maps one table per document type per store, so green needs a second `IDocumentStore` with `DatabaseSchemaName` set to the green schema |
| B. Versioned table names in the same schema | No extra rights | The query side must resolve the active version on every node; a Marten document alias per version |

### Chosen Option: **A — second schema** (recommended, pending the maintainer)

### Rationale

Both options are valid. A wins when the application may create schemas, and it removes node coordination at switch time (queries resolve the table by name). B wins when schema creation is restricted by the database role. Phase 1 contains a feasibility test of the swap on a real Marten store; if the swap cannot be done in one transaction with the Marten schema, the plan falls back to B and this choice goes back to the maintainer.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How green catches up with live writes (new Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Tail the event store by sequence number from the replay checkpoint; a final drain runs inside the swap transaction | The write path (inline projections into blue) is untouched; the checkpoint is a plain number | The final drain holds a lock on blue for the time of the last small batch |
| B. Dual-write: the inline dispatcher also writes green while a rebuild runs | Green is current at all times | Changes `MartenInlineProjectionDispatcher` for every write; two writers per event; failure modes multiply |

### Chosen Option: **A — tail by sequence** (recommended, pending the maintainer)

### Rationale

`RebuildOptions.StartPosition`/`EndPosition` and `ProjectionStatus.LastProcessedPosition` already express positions. B wins only if the maintainer wants a zero-length final lock and accepts a change to the live write path.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. One rebuild per projection, enforced by a distributed lock</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `IDistributedLockProvider.TryAcquireAsync("projection-rebuild:{name}")` | Works across nodes with any registered provider (InMemory for tests, Redis, SqlServer, PostgreSQL) | New reference to `Encina.DistributedLock` |
| B. A PostgreSQL advisory lock through the Marten connection | No new reference | PostgreSQL only; a second locking mechanism next to the shared abstraction |

### Chosen Option: **A — `IDistributedLockProvider`**

### Rationale

The function-7 question of the cross-cutting check (concurrent access to shared state) names `IDistributedLockProvider` as the integration point.

**Class:** Settled
**Settled by:** AGENTS.md section 6, function 7 (Distributed locks: `IDistributedLockProvider`)

</details>

<details>
<summary><strong>5. Rebuild state is persisted; the worker loop never throws</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. A `ProjectionRebuildOperation` Marten document updated per batch; the worker is a `BackgroundService` with a `PeriodicTimer` loop that catches per iteration | Restart-safe; status visible to every node; failures become a `Failed` phase with an error code | One more document type |
| B. Keep status in memory like `MartenProjectionManager._statuses` | Nothing new | Lost on restart and invisible to other nodes |

### Chosen Option: **A — persisted state, per-iteration catch**

### Rationale

A failed batch fails the operation visibly (phase `Failed`, error code in the status and log), never reports success; cancellation from the stopping token still ends the loop.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (errors are never swallowed in background infrastructure; background loops and timers never let an exception escape)

</details>

<details>
<summary><strong>6. Defaults of the new options, and retention of finished operations</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `RebuildStrategy = InPlace` (today's behavior), `GreenSchemaPrefix = "encina_green_"`, `CatchUpThreshold = 100` events, `AutoSwitchOnCatchUp = false` (the operator switches); the last finished `ProjectionRebuildOperation` per projection is kept, older ones are deleted when a new operation starts | Nothing changes unless asked; a switch is always a human or explicit decision; bounded storage | An operator who wants a hands-off rebuild sets the flag |
| B. `AutoSwitchOnCatchUp = true` by default; operations kept forever | Hands-off | A swap happens without anyone looking; unbounded documents |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

The numbers are starting points with no measurement behind them (`RebuildBatchSize` defaults to 1000 in `src/Encina.Marten/Projections/ProjectionOptions.cs`); the swap of read-model data is the risky step, so it stays opt-in per AGENTS.md section 1 (every feature opt-in).

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Lock key, lock duration and a missing lock provider</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Key `projection-rebuild:{projection name}`; the lock is held only while starting and while switching, and the persisted operation (a non-terminal phase) is what excludes a second rebuild during the hours of replay; with `BlueGreen` and no `IDistributedLockProvider` registered, startup validation fails | No lock renewal (the provider interface has `TryAcquireAsync` and `AcquireAsync` with an expiry and no extend call); a crashed node cannot hold a lock for hours | The document is the guard in the middle, so a crash leaves a running operation that an operator must abort or the worker must resume |
| B. Hold the lock for the whole rebuild with a long fixed expiry; fall back to an in-process lock when no provider is registered | One mechanism | A crashed node blocks the projection until the expiry; the in-process fallback is wrong with several nodes |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

Decision 4 settles the use of `IDistributedLockProvider`; how long it is held and what a missing provider means are not settled by it. The behavior of contention follows ADR-052: `Right(None)` (the provider's "held elsewhere") is `PROJECTION_REBUILD_IN_PROGRESS`, a `Left` is a fault (`PROJECTION_REBUILD_LOCK_FAILED`). Today `IDistributedLockProvider.TryAcquireAsync` returns `IAsyncDisposable?` (null on contention, an exception on a fault, `src/Encina.DistributedLock/IDistributedLockProvider.cs`); the manager is written against the contract on main when PR 2 starts and maps a thrown exception to the same `LOCK_FAILED` code. A `Switch` while the lock is held elsewhere (for example by a crashed node, until its expiry) returns `PROJECTION_REBUILD_IN_PROGRESS` and is retried by the operator or the worker; it never proceeds without the lock.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. What happens to blue after the swap</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Blue is kept as an archive schema (the issue says "archive blue") until the next rebuild of the same projection replaces it or an operator drops it | A rollback is possible | Storage until dropped |
| B. Blue is dropped at once | No leftover | No rollback after a bad switch |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

Issue #324 lists "Archive blue" as the last step of the flow; a rollback path is the reason for a blue-green design. B wins when storage is tight.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Validation of the schema name (DDL is built from it)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. The whole schema name (`GreenSchemaPrefix` plus the projection name) must match `^[a-z][a-z0-9_]{0,62}$` (the PostgreSQL identifier limit of 63 bytes), is rejected with `PROJECTION_REBUILD_SCHEMA_FAILED` when it does not, and is always quoted as an identifier in DDL | Simple; fails visibly | A projection name with unusual characters must be renamed |
| B. Build the schema name from the prefix and a hash of the projection name | Any name works | Unreadable schema names for operators |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

The projection name comes from a registered type, but DDL string building from any input is an injection risk, so no unvalidated value reaches the statement under either option. A keeps the names readable; B wins when projection names cannot be controlled.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. New error codes and their categories</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `PROJECTION_REBUILD_IN_PROGRESS` (`Conflict`), `PROJECTION_REBUILD_NOT_READY` (`Conflict`), `PROJECTION_REBUILD_ABORTED` (`Conflict`), `PROJECTION_REBUILD_SWITCH_FAILED` (`Internal`), `PROJECTION_REBUILD_SCHEMA_FAILED` (`Internal`; a rejected name is `InvalidRequest`), `PROJECTION_REBUILD_LOCK_FAILED` (`Unavailable`), all created with the category overload of `EncinaErrors.Create` | The `PROJECTION_` prefix of `ProjectionErrorCodes` and ADR 062 (merged in PR #3599): rule R2 makes a mismatched version, an operation in progress and an invalid transition `Conflict` and keeps `FailedPrecondition` out until #3420 | Six constants |
| B. The same codes created with the two-argument `Create` (no category) | One argument fewer | The category would be inferred from the code text by `LegacyErrorCategoryInference`, the migration path ADR 062 retires |

### Chosen Option: **A**

### Rationale

The shape follows the existing constants (`PROJECTION_STORE_FAILED`, `PROJECTION_APPLY_FAILED` in `src/Encina.Marten/Projections/ProjectionErrorCodes.cs`) and the categories follow ADR 062.

**Class:** Trivial
**Settled by:** `src/Encina.Marten/Projections/ProjectionErrorCodes.cs` (naming) and ADR 062 rule R2 (categories)

</details>

<details>
<summary><strong>11. Marten async daemon integration (issue task)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Not part of this plan: Encina projections are `IProjection` types applied by `ProjectionRegistry` and replayed by `MartenProjectionManager` (`QueryAllRawEvents`), and `Encina.Marten` has no async daemon reference on main, so the rebuild uses its own replay | Works with the projections Encina has; no daemon dependency | The daemon's own rebuild is not used |
| B. Drive the rebuild through the Marten async daemon | Reuses Marten's rebuild | Applies to Marten-native projections, not `IProjection`; an opt-in daemon route is the separate issue #2384 |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

The issue's checkbox "Marten async daemon integration" presumes a daemon the package does not use; if #2384 (open, the async daemon as an opt-in delivery route for domain events) is built, a daemon-driven rebuild can be reconsidered then.

**Class:** Dilemma

</details>

<details>
<summary><strong>12. Lock-failure handling in the worker</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. The worker treats contention as "someone else owns it, skip this iteration", a fault as a failed iteration logged by error code, and a missing lock as a startup failure | Same semantics as the other background loops | None |
| B. Any lock fault ends the worker loop | Loud | An unhandled fault ends the loop silently or stops the host, which AGENTS.md section 3 forbids for background loops |

### Chosen Option: **A**

### Rationale

ADR-052 fixes the meaning of contention (a normal outcome) and of a fault; AGENTS.md section 3 requires per-iteration catching in a `PeriodicTimer` loop, with cancellation propagating.

**Class:** Settled
**Settled by:** ADR-052 and AGENTS.md section 3 (background loops and timers)

</details>

---

## Implementation Phases

### Phase 1: Models, Abstractions, Options and Feasibility Spike (PR 1, 14 files)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Marten/Projections/Rebuild/`: `RebuildPhase` (`NotStarted`, `CreatingGreenSchema`, `ReplayingHistory`, `CatchingUpLive`, `ReadyToSwitch`, `Switching`, `Completed`, `Failed`, `Aborted`), `RebuildStrategy` (`InPlace`, `BlueGreen`), `RebuildOperation` (the returned model: id, projection name, started at), `ProjectionRebuildOperation` (the persisted Marten document: id, projection name, phase, checkpoint, error code, timestamps; retention per decision 6), `RebuildStatus` (phase, total and processed events, lag, `ReadyToSwitch` computed against `CatchUpThreshold`, estimated remaining time).
2. `IProjectionRebuildManager` as in the issue, all methods `Either<EncinaError, T>` (home per D1).
3. `ProjectionOptions` (edit): `RebuildStrategy`, `GreenSchemaPrefix`, `AutoSwitchOnCatchUp`, `CatchUpThreshold` with the defaults of decision 6; validation: the schema name rule of decision 9, threshold greater than zero.
4. `ProjectionErrorCodes` (edit): the six codes of decision 10, created with the category overload of `EncinaErrors.Create` (ADR 062).
5. Feasibility test (integration, D2, shared Marten PostgreSQL fixture): on a real Marten store, create a green copy of one document table in a second schema and swap it with the blue table in one transaction while a reader runs; the result decides D2. It also shows how `ProjectionRebuildOperation` is registered in the Marten schema (the registration code itself ships in PR 3 with the DI).
6. Tests in the same PR: unit (status maths, `ReadyToSwitch`, option defaults and validation, name rule), guard (`ProjectionOptions` and `RebuildStatus` validated parameters in `tests/Encina.GuardTests`), property (progress is monotonic and phases only move forward). The contract test of `IProjectionRebuildManager` needs a real implementation and ships in PR 2.
7. Manifest entries for the six new files and raised targets for the two edited files in `.github/coverage-manifest/Encina.Marten.json`, per flag with one-sentence justifications (`mechanical-fixer`), in this PR. No PublicAPI lines (no file exists for `Encina.Marten`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina.Marten projections live in src/Encina.Marten/Projections. MartenProjectionManager rebuilds in place and keeps status in memory. Issue #324 adds a blue-green rebuild.
TASK: Add the rebuild models, IProjectionRebuildManager, the ProjectionOptions properties and error codes, and an integration test proving the schema swap on PostgreSQL (the feasibility spike).
KEY RULES: all operations return Either<EncinaError, T>; options validated at registration; new error codes use the ErrorCategory overload (ADR 062); no [Obsolete]; XML docs with examples; no PublicAPI file exists for Encina.Marten; guard tests for validated parameters; tests execute real code.
REFERENCE FILES: src/Encina.Marten/Projections/IProjectionManager.cs, src/Encina.Marten/Projections/ProjectionOptions.cs, src/Encina.Marten/Projections/ProjectionStatus.cs, src/Encina.Marten/Projections/ProjectionErrorCodes.cs
```

</details>

### Phase 2: `BlueGreenRebuildManager` and Diagnostics (PR 2, 15 files, blocked until D2 is answered)

<details>
<summary><strong>Tasks</strong></summary>

1. `BlueGreenRebuildManager` (sealed, singleton like `MartenProjectionManager`): constructor takes `IDocumentStore`, `ProjectionRegistry`, `IDistributedLockProvider`, `IOptions<EncinaMartenOptions>`, `ILogger`, `TimeProvider?`.
2. The apply logic of `MartenProjectionManager` (the private static `ApplyEventAsync`, `ProcessSingleEventAsync` and `LoadReadModelAsync`) moves into an internal `ProjectionEventApplier` used by both managers: a new file plus an edit of `src/Encina.Marten/Projections/MartenProjectionManager.cs` (both counted; the existing behavior and its tests do not change).
3. `StartRebuildAsync<TProjection>`: look up the registration, take the lock (decision 7: contention is `PROJECTION_REBUILD_IN_PROGRESS`), persist a `ProjectionRebuildOperation`, create the green schema copy (name rule of decision 9), start the replay (`QueryAllRawEvents` batches through the shared applier).
4. Catch-up: after history, tail from the checkpoint until lag is under `CatchUpThreshold`; set `ReadyToSwitch`; with `AutoSwitchOnCatchUp` call the switch.
5. `SwitchToGreenAsync`: final drain and table swap in one transaction (D2/D3), blue archived per decision 8; `AbortAsync`: drop the green schema, mark `Aborted`, release the lock.
6. Diagnostics, created here because the manager is the first logger: register `MartenProjectionRebuild = (7270, 7279)` in `src/Encina/Diagnostics/EventIdRanges.cs` and add it to the `["Encina.Marten"]` list of `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`; `ProjectionRebuildLog`, `ProjectionRebuildMetrics`, `ProjectionRebuildActivitySource` (Phase 4).
7. Tests in the same PR: manager unit tests with a substituted store and `FakeTimeProvider` (phase machine, lag maths, lock outcomes), applier unit tests, diagnostics unit tests, manager guard tests, the contract test of `IProjectionRebuildManager` with the real manager, and the integration test of zero-downtime reads during a rebuild and of abort cleanup. Manifest entries for every new file in this PR.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Phase 1 models and the spike result exist. MartenProjectionManager.ExecuteRebuildAsync shows batch replay (QueryAllRawEvents, ProcessSingleEventAsync).
TASK: Implement BlueGreenRebuildManager with start, status, switch and abort as described, reusing the event-apply code instead of copying it (extract an internal shared applier if needed).
KEY RULES: every failure is a Left with a PROJECTION_REBUILD_* code; time through TimeProvider; async with CancellationToken; never log EncinaError.Message; release the lock on every exit path.
REFERENCE FILES: src/Encina.Marten/Projections/MartenProjectionManager.cs, src/Encina.DistributedLock/IDistributedLockProvider.cs, src/Encina.Marten/Projections/ProjectionRegistry.cs
```

</details>

### Phase 3: Worker, Configuration and DI (PR 3, 12 files)

<details>
<summary><strong>Tasks</strong></summary>

1. `ProjectionRebuildWorker : BackgroundService`: a `PeriodicTimer` loop over running operations that advances catch-up and auto-switches; it catches per iteration, fails the operation visibly, handles lock outcomes per decision 12, and ends only on the stopping token. It logs through `ProjectionRebuildLog` (PR 2).
2. `AddProjections` in `ServiceCollectionExtensions` registers `IProjectionRebuildManager`, the worker, the `ProjectionRebuildOperation` document in the Marten schema and the options validation when `RebuildStrategy` is `BlueGreen` (`TryAdd*`); an application's own registration is never overridden.
3. DI test with `ValidateOnBuild` and `ValidateScopes` (it must also resolve `IDistributedLockProvider`; a missing provider is a startup validation failure, decision 7, not a runtime null).
4. `ProjectionRebuildHealthCheck : EncinaHealthCheck` (`DefaultName` const, static `Tags`), Degraded while a rebuild is `Failed`.
5. Tests in the same PR: worker unit (with `FakeTimeProvider`; a thrown exception is never swallowed by the test), health check unit, DI, guard (worker, health check, `AddProjections` edit of the existing `ServiceCollectionExtensionsGuardTests`), integration (restart resumes from the persisted checkpoint, automatic and manual switch), load test (reads during a rebuild never fail), and the benchmark justification `.md` (rare operation; allowed for benchmarks, never for unit, guard or contract tests). Manifest entries for the new files with the tests that meet them.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: BlueGreenRebuildManager exists. AddProjections registers the projection infrastructure; MartenHealthCheck is the health check pattern.
TASK: Add the worker, the registration and validation, and the health check.
KEY RULES: PeriodicTimer loops catch per iteration and never let an exception escape (cancellation still ends the loop); TryAdd* only; the DI test builds with ValidateOnBuild and ValidateScopes; hosted service tests use FakeTimeProvider and never swallow a thrown exception.
REFERENCE FILES: src/Encina.Marten/ServiceCollectionExtensions.cs, src/Encina.Marten/Health/MartenHealthCheck.cs, docs/en/guides/TESTING.md
```

</details>

### Phase 4: Observability (created in PR 2)

<details>
<summary><strong>Tasks</strong></summary>

1. Register `MartenProjectionRebuild = (7270, 7279)` in `src/Encina/Diagnostics/EventIdRanges.cs` (inside the block assigned to this family in `artifacts/decisions/plan-batch/eventid-blocks.md`) and add it to the `["Encina.Marten"]` list of `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
2. `ProjectionRebuildLog` (`[LoggerMessage]`, packed from 7270): started, phase changed, replay progress, ready to switch, switched, aborted, failed (error code), lock contention, lock failed (`ex.ForLogging()`) = 9 ids, 7270 to 7278.
3. `ProjectionRebuildMetrics`: counter `encina.marten.projection.rebuild.processed_events`, gauge `encina.marten.projection.rebuild.lag_seconds`; `ProjectionRebuildActivitySource` with the activity `encina.marten.projection.rebuild` (`projection.type`, `phase`). Tags never carry the schema name, stream ids or error messages. Wiring the Marten source and meter into `Encina.OpenTelemetry` is the follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/marten-otel-registration.md).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: Projection logging lives in ProjectionLog.cs (EventIds 2664-2700). The range 7270-7279 is assigned to #324 from the family block 7250-7299.
TASK: Register the range first, then add the log class, the meter and the activity source (PR 2) and call them from the manager; the worker of PR 3 calls them too.
KEY RULES: sequential EventIds (9 ids from 7270); log error codes only; update AssemblyRanges; run the architecture tests.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Marten/Projections/ProjectionLog.cs, src/Encina.Tenancy/Diagnostics/TenancyMetrics.cs
```

</details>

### Phase 5: Testing, Documentation and Finalization (tests in PRs 1 to 3, documentation in PR 4)

<details>
<summary><strong>Tasks</strong></summary>

1. Each PR carries its own tests and its own per-file manifest targets with justifications (`.github/coverage-manifest/Encina.Marten.json`, `mechanical-fixer`), as listed in Phases 1 to 3; Phase 5 adds none.
2. PR 4 (4 files), documentation (`docs-writer`): rebuild guide (when to use in-place, when blue-green), ADR for D1 to D3 and decisions 6 to 9 and 11 (ADR number assigned when the ADR pull request opens), `docs/INVENTORY.md`, changelog fragment `changelog.d/324-blue-green-projection-rebuild.added.md`. `Encina.Marten` has no package README on main.
3. Verification in every PR: zero warnings, every flag at target, CRAP of changed methods at most 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: The manager, worker, health check and diagnostics exist; coverage is per flag against .github/coverage-manifest/Encina.Marten.json.
TASK: PR 4 only: write the guide, the ADR, the INVENTORY entry and the fragment (docs-writer and mechanical-fixer); the tests and manifest targets shipped with PRs 1 to 3.
KEY RULES: shared fixtures only; no Thread.Sleep; Shouldly through Encina.Testing.Shouldly; docs cite coverage with covref markers only.
REFERENCE FILES: tests/Encina.IntegrationTests/Infrastructure/Marten, tests/Encina.LoadTests, docs/testing/coverage-measurement-methodology.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| Projection rebuild patterns (event-driven.io) | In-place, rolling and blue-green options |
| ADR-027 | Marten as the event-sourcing provider |
| ADR-021 | EventId registry and allocation test |
| AGENTS.md section 3 | Background loops, errors never swallowed, `TimeProvider` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IProjectionManager`, `MartenProjectionManager` | `src/Encina.Marten/Projections` | In-place rebuild stays; its batch replay and apply code is reused |
| `ProjectionRegistry`, `ProjectionStatus`, `RebuildOptions` | `src/Encina.Marten/Projections` | Registration lookup, position fields, batch size |
| `ProjectionOptions`, `ProjectionErrorCodes`, `ProjectionLog` | `src/Encina.Marten/Projections` | Extended with the strategy options and codes |
| `IDistributedLockProvider` | `src/Encina.DistributedLock/IDistributedLockProvider.cs` | One rebuild per projection |
| `MartenHealthCheck` | `src/Encina.Marten/Health/MartenHealthCheck.cs` | Pattern for the rebuild health check |
| `MartenInlineProjectionDispatcher` | `src/Encina.Marten/Projections/InlineProjectionDispatcher.cs` | Keeps writing blue during a rebuild (D3-A) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Marten` (new range `MartenProjectionRebuild`) | 7270-7279 (ids 7270-7278 used) | Inside the block 7250-7299 assigned to the family in the batch registry (`artifacts/decisions/plan-batch/eventid-blocks.md`), which records the sub-ranges of the sibling plans |

### Estimated File Count

Every added or edited file is counted: tests, manifest, registry edits, justification and changelog. `MartenProjectionManager.cs` is edited for the applier extraction; its existing tests keep passing unchanged (no signature changes). `Encina.Marten` has no PublicAPI file.

| PR | Category | Files | Notes |
|----|----------|-------|-------|
| 1 | Source | 8 | `RebuildPhase`, `RebuildStrategy`, `RebuildOperation`, `RebuildStatus`, `ProjectionRebuildOperation`, `IProjectionRebuildManager`, `ProjectionOptions` edit, `ProjectionErrorCodes` edit |
| 1 | Tests | 5 | Models unit, options unit, guard, property, schema-swap spike (integration) |
| 1 | Manifest | 1 | `Encina.Marten.json` |
| 1 | **Subtotal** | **14** | |
| 2 | Source | 7 | `BlueGreenRebuildManager`, `ProjectionEventApplier`, `MartenProjectionManager` edit, `EventIdRanges.cs` edit, `ProjectionRebuildLog`, `ProjectionRebuildMetrics`, `ProjectionRebuildActivitySource` |
| 2 | Tests | 7 | Manager unit, applier unit, diagnostics unit, manager guard, contract, integration (zero downtime, abort), `EncinaEventIdAllocationTests.cs` edit |
| 2 | Manifest | 1 | `Encina.Marten.json` |
| 2 | **Subtotal** | **15** | |
| 3 | Source | 3 | `ProjectionRebuildWorker`, `ProjectionRebuildHealthCheck`, `ServiceCollectionExtensions` edit |
| 3 | Tests | 8 | Worker unit, health unit, DI, guard (worker and health), `ServiceCollectionExtensionsGuardTests` edit, integration (restart, switch), load, benchmark `.md` justification |
| 3 | Manifest | 1 | `Encina.Marten.json` |
| 3 | **Subtotal** | **12** | |
| 4 | Docs | 3 | Guide, ADR, `docs/INVENTORY.md` |
| 4 | Changelog | 1 | `changelog.d/324-blue-green-projection-rebuild.added.md` |
| 4 | **Subtotal** | **4** | |
| | **Total** | **45** | 14 + 15 + 12 + 4; each PR at most 15 files and 1,500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14. Issue #324 adds a zero-downtime blue-green projection rebuild to Encina.Marten, next to the existing in-place IProjectionManager.RebuildAsync. Follow AGENTS.md: ROP, background loops never throw, TimeProvider, no EncinaError.Message in logs, per-flag coverage.

IMPLEMENTATION OVERVIEW: Phase 1 models, IProjectionRebuildManager, options, codes and the schema-swap feasibility test (PR 1). Phase 2 BlueGreenRebuildManager (start, replay, catch-up by sequence, switch, abort, lock) with Phase 4 EventId range 7270-7279, log class, metrics (PR 2, blocked until D2 is answered). Phase 3 worker, DI and health check (PR 3). Phase 5 docs and fragment (PR 4). Each PR carries its own tests and manifest targets. Dilemmas D1 (API), D2 (isolation), D3 (catch-up) and decisions 6 to 9 and 11 follow the maintainer's answers; D2 depends on the Phase 1 spike.

KEY PATTERNS: Either for every operation; persisted rebuild state; PeriodicTimer loops catching per iteration; TryAdd* registration with a ValidateOnBuild test; sequential EventIds registered first; shared integration fixtures.

REFERENCE FILES: src/Encina.Marten/Projections/MartenProjectionManager.cs, src/Encina.Marten/Projections/ProjectionOptions.cs, src/Encina.Marten/ServiceCollectionExtensions.cs, src/Encina.DistributedLock/IDistributedLockProvider.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/plans/dsr-implementation-plan-404.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | No caching consumer of projections exists on main, so there is no cached read-model result to invalidate after a switch |
| 2 | OpenTelemetry | ✅ | Progress counter, lag gauge and `encina.marten.projection.rebuild` span (Phase 4) |
| 3 | Structured Logging | ✅ | `ProjectionRebuildLog`, EventIds 7270-7278, error codes only (Phase 4) |
| 4 | Health Checks | ✅ | `ProjectionRebuildHealthCheck` (Phase 3) |
| 5 | Validation | ✅ | Options and the schema name validated (decision 9, Phases 1 and 3) |
| 6 | Resilience | ❌ | A failed batch fails the operation visibly (phase `Failed`) and the replay is restartable from the persisted checkpoint, so a retry is an operator or worker restart, not a hidden policy |
| 7 | Distributed Locks | ✅ | One rebuild per projection (decisions 4 and 7, Phase 2) |
| 8 | Transactions | ✅ | Final drain and table swap in one PostgreSQL transaction (Phase 2) |
| 9 | Idempotency | ✅ | Replay is idempotent because read models are upserted by id; restart resumes from the persisted checkpoint (Phase 2) |
| 10 | Multi-Tenancy | ⏭️ | Per-tenant rebuilds depend on #326 (open, tenant-aware event sourcing) |
| 11 | Module Isolation | ❌ | Marten has no module-scoped projections today, so there is nothing to scope |
| 12 | Audit Trail | ⏭️ | The issue marks this Integrate; the plan defers it because the audit record needs the operator identity of the admin surface (the CLI command of #2611): follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/projection-rebuild-audit.md) |

---

## Prerequisites & Dependencies

- #535 (extended projections) will move the projection abstractions; this plan keeps its types under `Encina.Marten.Projections` and the move, if chosen there, is mechanical.
- #326 (tenancy) is independent; per-tenant rebuilds wait for it.
- #317 and #315 named in the original text are not projection work.

---

## Next Steps

1. The maintainer answers the Dilemmas (D1, D2, D3 and decisions 6 to 9 and 11) when work on #324 starts; the answers go into `## Maintainer Decisions`.
2. Run PR 1 first: its feasibility test settles D2 before the manager (PR 2, blocked until D2 is answered) is written.
3. The CLI command is the open issue #2611; open the audit follow-up and the OpenTelemetry wiring issue from their drafts, and write the PR briefs one at a time.
