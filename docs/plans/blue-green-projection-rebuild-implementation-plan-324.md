# Implementation Plan: Blue-Green Projection Rebuild (Zero-Downtime) for `Encina.Marten`

> **Issue**: [#324](https://github.com/dlrivada/Encina/issues/324)
> **Type**: Feature
> **Complexity**: High (5 phases, 3 PRs, one provider, a feasibility spike first)
> **Estimated Scope**: about 34 files, about 2,900 changed lines over 3 PRs

---

## Summary

`IProjectionManager.RebuildAsync<TReadModel>` (`src/Encina.Marten/Projections/IProjectionManager.cs`, implemented by `MartenProjectionManager`) rebuilds in place: with the default `RebuildOptions.DeleteExisting = true` it first runs `deleteSession.DeleteWhere<TReadModel>(_ => true)` and then replays `QueryAllRawEvents()` in batches, so the read model is empty or partial while it runs. The progress lives in an in-memory `ConcurrentDictionary<string, ProjectionStatus>`; there is no persisted rebuild state, no catch-up notion and no strategy in `ProjectionOptions` (`Enabled`, `UseInlineProjections`, `RebuildBatchSize`, `AutoRebuildOnStartup`, `ThrowOnProjectionError`). This plan adds a **blue-green rebuild**: replay into a green copy while blue serves reads, catch up with the live event store, then swap atomically.

**What it implements**

- `IProjectionRebuildManager` (`StartRebuildAsync`, `GetStatusAsync`, `SwitchToGreenAsync`, `AbortAsync`, all `Either<EncinaError, T>`) with `RebuildOperation`, `RebuildStatus`, `RebuildPhase` and `RebuildStrategy` (shape per Dilemma D1).
- `BlueGreenRebuildManager`: creates the green copy (isolation per D2), replays history in batches, tails the event store until the lag is under `CatchUpThreshold` (D3), then switches manually or automatically (`AutoSwitchOnCatchUp`).
- Rebuild state persisted as a Marten document so a restart resumes or reports correctly; one rebuild per projection at a time under `IDistributedLockProvider`.
- A rebuild-status `IEncinaHealthCheck`, diagnostics and tests. The CLI command and audited operator actions are deferred (issue file in the report).

**Standards**: AGENTS.md section 3 (ROP, background loops never throw), section 6 (12 functions), ADR-018, ADR-021, ADR-027.

**Affected packages**: `Encina.Marten` (code, `PublicAPI.Unshipped.txt`, manifest); a new project reference from `Encina.Marten` to `Encina.DistributedLock` (which references only `Encina` and `Encina.Messaging`, so no cycle). `Encina.Cli` is not changed in this plan.

**Provider category**: event sourcing (Marten, AGENTS.md section 5). The 10-database rule does not apply to the Marten read-model store; integration tests need Marten on PostgreSQL through Testcontainers.

**Size and split (AGENTS.md section 11)**: about 34 files and 2,900 lines exceed one worker PR. PR 1: Phase 1 (models, abstractions, options, error codes, the feasibility spike test; about 13 files). PR 2: Phase 2 (`BlueGreenRebuildManager` and its unit and integration tests; about 12 files). PR 3: Phases 3 to 5 (hosted worker, DI, health check, diagnostics, remaining tests and docs; about 14 files).

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

---

## Implementation Phases

### Phase 1: Models, Abstractions, Options and Feasibility Spike

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Marten/Projections/Rebuild/`: `RebuildPhase` (`NotStarted`, `CreatingGreenSchema`, `ReplayingHistory`, `CatchingUpLive`, `ReadyToSwitch`, `Switching`, `Completed`, `Failed`, `Aborted`), `RebuildStrategy` (`InPlace`, `BlueGreen`), `RebuildOperation` (id, projection name, started at), `RebuildStatus` (phase, total and processed events, lag, `ReadyToSwitch` computed against `CatchUpThreshold`, estimated remaining time).
2. `IProjectionRebuildManager` as in the issue, all methods `Either<EncinaError, T>` (home per D1).
3. `ProjectionOptions`: `RebuildStrategy`, `GreenSchemaPrefix`, `AutoSwitchOnCatchUp`, `CatchUpThreshold` (validated: prefix is a valid identifier, threshold greater than zero).
4. New codes in `ProjectionErrorCodes` (`PROJECTION_REBUILD_IN_PROGRESS`, `_SWITCH_FAILED`, `_NOT_READY`, `_ABORTED`, `_SCHEMA_FAILED`).
5. Feasibility test (integration, D2): on a real Marten store, create a green copy of one document table in a second schema and swap it with the blue table in one transaction while a reader runs; the result decides D2.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina.Marten projections live in src/Encina.Marten/Projections. MartenProjectionManager rebuilds in place and keeps status in memory. Issue #324 adds a blue-green rebuild.
TASK: Add the rebuild models, IProjectionRebuildManager, the ProjectionOptions properties and error codes, and an integration test proving the schema swap on PostgreSQL (the feasibility spike).
KEY RULES: all operations return Either<EncinaError, T>; options validated at registration; no [Obsolete]; XML docs with examples; PublicAPI.Unshipped.txt; tests execute real code.
REFERENCE FILES: src/Encina.Marten/Projections/IProjectionManager.cs, src/Encina.Marten/Projections/ProjectionOptions.cs, src/Encina.Marten/Projections/ProjectionStatus.cs, src/Encina.Marten/Projections/ProjectionErrorCodes.cs
```

</details>

### Phase 2: `BlueGreenRebuildManager`

<details>
<summary><strong>Tasks</strong></summary>

1. `BlueGreenRebuildManager` (sealed, singleton like `MartenProjectionManager`): constructor takes `IDocumentStore`, `ProjectionRegistry`, `IDistributedLockProvider`, `IOptions<EncinaMartenOptions>`, `ILogger`, `TimeProvider?`.
2. `StartRebuildAsync<TProjection>`: look up the registration, take the lock (`Left` `REBUILD_IN_PROGRESS` when held), persist a `ProjectionRebuildOperation`, create the green schema copy, start the replay (`QueryAllRawEvents` batches reusing the apply logic of `MartenProjectionManager`).
3. Catch-up: after history, tail from the checkpoint until lag is under `CatchUpThreshold`; set `ReadyToSwitch`; with `AutoSwitchOnCatchUp` call the switch.
4. `SwitchToGreenAsync`: final drain and table swap in one transaction (D2/D3); `AbortAsync`: drop the green schema, mark `Aborted`, release the lock.
5. Unit tests with a substituted store and `FakeTimeProvider` (phase machine, lag maths); integration test of zero-downtime reads during a rebuild and of abort cleanup.

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

### Phase 3: Worker, Configuration and DI

<details>
<summary><strong>Tasks</strong></summary>

1. `ProjectionRebuildWorker : BackgroundService`: a `PeriodicTimer` loop over running operations that advances catch-up and auto-switches; it catches per iteration, fails the operation visibly, and ends only on the stopping token.
2. `AddProjections` in `ServiceCollectionExtensions` registers `IProjectionRebuildManager`, the worker and the options validation when `RebuildStrategy` is `BlueGreen` (`TryAdd*`).
3. DI test with `ValidateOnBuild` and `ValidateScopes` (it must also resolve `IDistributedLockProvider`; a missing provider is a registration error, not a runtime null).
4. `ProjectionRebuildHealthCheck : EncinaHealthCheck` (`DefaultName` const, static `Tags`), Degraded while a rebuild is `Failed`.

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

### Phase 4: Observability

<details>
<summary><strong>Tasks</strong></summary>

1. Register `MartenProjectionRebuild = (7270, 7279)` in `src/Encina/Diagnostics/EventIdRanges.cs` and add it to the `["Encina.Marten"]` list of `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
2. `ProjectionRebuildLog` (`[LoggerMessage]`, 7270 onward): started, phase changed, replay progress, ready to switch, switched, aborted, failed (error code).
3. `ProjectionRebuildMetrics`: counter `encina.marten.projection.rebuild.processed_events`, gauge `encina.marten.projection.rebuild.lag_seconds`; activity `encina.marten.projection.rebuild` (`projection.type`, `phase`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: Projection logging lives in ProjectionLog.cs (EventIds 2664-2700). The range 7270-7279 is assigned to #324 from the family block 7250-7299.
TASK: Register the range first, then add the log class, the meter and the activity source and call them from the manager and the worker.
KEY RULES: sequential EventIds; log error codes only; update AssemblyRanges; run the architecture tests.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Marten/Projections/ProjectionLog.cs, src/Encina.Tenancy/Diagnostics/TenancyMetrics.cs
```

</details>

### Phase 5: Testing, Documentation and Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. Guard tests for every public method; contract test for `IProjectionRebuildManager`; property test: progress is monotonic and phases only move forward.
2. Integration tests on the shared Marten PostgreSQL fixture: zero-downtime proof (reads succeed throughout), manual and automatic switch, abort cleanup, restart resumes from the persisted checkpoint. Load test: reads during a rebuild never fail. Benchmark: justification `.md` (rare operation).
3. Per-file targets with justifications in `.github/coverage-manifest/Encina.Marten.json` (`mechanical-fixer`).
4. Documentation (`docs-writer`): rebuild guide (when to use in-place, when blue-green), README, ADR for D1 to D3 (ADR number assigned when the ADR pull request opens), `docs/INVENTORY.md`, changelog fragment `324-blue-green-projection-rebuild.added.md`.
5. Verification: zero warnings, every flag at target, CRAP of changed methods at most 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: The manager, worker, health check and diagnostics exist; coverage is per flag against .github/coverage-manifest/Encina.Marten.json.
TASK: Write the remaining tests, the manifest targets, the docs and the fragment.
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
| `MartenInlineProjectionDispatcher` | `src/Encina.Marten/ServiceCollectionExtensions.cs` (registration) | Keeps writing blue during a rebuild (D3-A) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Marten` (new range `MartenProjectionRebuild`) | 7270-7279 | From the family block 7250-7299: #320 owns 7250-7259, #323 7260-7269, this plan 7270-7279, #326 7280-7289, #535 7290-7299 |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Models and abstractions | 6 | Phase, strategy, operation, status, interface, codes edit |
| Manager, worker, health check | 4 | Manager, worker, applier extraction, health check |
| Options, DI | 2 | `ProjectionOptions`, `ServiceCollectionExtensions` |
| Diagnostics | 2 | Log class, metrics/activity |
| Unit and guard tests | 8 | Manager, worker, options, DI, guards |
| Contract and property tests | 2 | Interface, progress invariants |
| Integration and load tests | 4 | Spike, zero downtime, restart, load |
| Docs, changelog, PublicAPI, manifest | 6 | Guide, ADR, fragment, PublicAPI, manifest, README |
| **Total** | **34** | Three PRs |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14. Issue #324 adds a zero-downtime blue-green projection rebuild to Encina.Marten, next to the existing in-place IProjectionManager.RebuildAsync. Follow AGENTS.md: ROP, background loops never throw, TimeProvider, no EncinaError.Message in logs, per-flag coverage.

IMPLEMENTATION OVERVIEW: Phase 1 models, IProjectionRebuildManager, options, codes and the schema-swap feasibility test. Phase 2 BlueGreenRebuildManager (start, replay, catch-up by sequence, switch, abort, lock). Phase 3 worker, DI and health check. Phase 4 EventId range 7270-7279, log class, metrics. Phase 5 tests, manifest, docs, fragment. Three PRs (Phase 1, Phase 2, Phases 3-5). Dilemmas D1 (API), D2 (isolation) and D3 (catch-up) follow the maintainer's answers; D2 depends on the Phase 1 spike.

KEY PATTERNS: Either for every operation; persisted rebuild state; PeriodicTimer loops catching per iteration; TryAdd* registration with a ValidateOnBuild test; sequential EventIds registered first; shared integration fixtures.

REFERENCE FILES: src/Encina.Marten/Projections/MartenProjectionManager.cs, src/Encina.Marten/Projections/ProjectionOptions.cs, src/Encina.Marten/ServiceCollectionExtensions.cs, src/Encina.DistributedLock/IDistributedLockProvider.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/plans/dsr-implementation-plan-404.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Invalidating cached read-model results after a switch; open an issue when a caching consumer of projections exists |
| 2 | OpenTelemetry | ✅ | Progress counter, lag gauge and `encina.marten.projection.rebuild` span (Phase 4) |
| 3 | Structured Logging | ✅ | `ProjectionRebuildLog`, EventIds 7270-7279, error codes only (Phase 4) |
| 4 | Health Checks | ✅ | `ProjectionRebuildHealthCheck` (Phase 3) |
| 5 | Validation | ✅ | Options validated at registration (Phase 1 and 3) |
| 6 | Resilience | ⏭️ | Retry of failed replay batches is not built in; a failed batch fails the operation visibly, and a Polly policy is a separate issue |
| 7 | Distributed Locks | ✅ | One rebuild per projection (Design Choice 4, Phase 2) |
| 8 | Transactions | ✅ | Final drain and table swap in one PostgreSQL transaction (Phase 2) |
| 9 | Idempotency | ✅ | Replay is idempotent because read models are upserted by id; restart resumes from the persisted checkpoint (Phase 2) |
| 10 | Multi-Tenancy | ⏭️ | Per-tenant rebuilds depend on #326 |
| 11 | Module Isolation | ⏭️ | Module-scoped projections are not designed in Marten today |
| 12 | Audit Trail | ⏭️ | Switch and abort are operator actions; structured audit needs the operator identity of the admin surface, deferred with the CLI command (issue file in the report) |

---

## Prerequisites & Dependencies

- #535 (extended projections) will move the projection abstractions; this plan keeps its types under `Encina.Marten.Projections` and the move, if chosen there, is mechanical.
- #326 (tenancy) is independent; per-tenant rebuilds wait for it.
- #317 and #315 named in the original text are not projection work.

---

## Next Steps

1. The maintainer answers D1, D2 and D3 when work on #324 starts; the answers go into `## Maintainer Decisions`.
2. Run PR 1 first: its feasibility test settles D2 before the manager is written.
3. Open the deferred issue (CLI command and audited operator actions) and write the PR briefs.
