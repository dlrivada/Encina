# Implementation Plan: Extended Projections — Multi-Destination and Provider-Agnostic Support

> **Issue**: [#535](https://github.com/dlrivada/Encina/issues/535)
> **Type**: Feature
> **Complexity**: Very High (8 phases, 17 PRs, a new package family; decomposed into sub-issues before any worker is briefed)
> **Estimated Scope**: about 192 files over 16 PRs plus the namespace move (about 160 files, one mechanical PR under a size exception, decision 6), about 14,000 changed lines in total; every figure is an estimate that each sub-issue replaces with its exact file list (AGENTS.md section 11)

---

## Summary

The projection system is tied to Marten and PostgreSQL. `IProjection<TReadModel>`, `IProjectionHandler<in TEvent, TReadModel>`, `IProjectionCreator`, `IProjectionDeleter`, `IReadModel`, `ProjectionContext` (provider neutral: stream id, sequence, `GlobalPosition`, event type, metadata), `IReadModelRepository<T>`, `IProjectionManager`, `ProjectionRegistry` and `ProjectionOptions` all live in `src/Encina.Marten/Projections/`, next to the Marten implementations (`MartenProjectionManager`, `MartenReadModelRepository`, `MartenInlineProjectionDispatcher`). Read models are stored in the PostgreSQL instance that holds the events; processing is inline only (`ProjectionOptions.UseInlineProjections`, "async projections: future feature"), and progress is an in-memory dictionary. No `Encina.Projections*` package and no `AddEncinaProjections` exist.

**What it implements**

- A provider-agnostic `Encina.Projections` package (D2) holding the projection contracts and a destination abstraction, `IProjectionDestination<TReadModel>` (upsert, delete, get checkpoint), with `Encina.Marten` referencing it.
- Routing configuration: `AddEncinaProjections(config => config.Project<OrderSummary>().ToMarten().ToElasticsearch())`.
- An asynchronous projection worker with a **checkpoint per projection and destination** (D3), catch-up from the checkpoint, parallel projections and lag measurement.
- Destination packages: `Encina.Projections.Marten`, `.Elasticsearch`, `.Redis`, `.MongoDB`, `.Sql` (relational, D4) and `.CosmosDb`, staged by D1.
- Lag health check and diagnostics. The full multi-destination rebuild uses the blue-green manager of #324.

**Standards**: AGENTS.md section 3 (provider coherence, ROP, background loops, errors not swallowed), section 5 (database rule), SPEC-000 DEC-009 (everything with a free local emulator is in 1.0), ADR-018, ADR-021, ADR-027.

**Affected packages**: `Encina.Marten` (types move out), new `Encina.Projections` and `Encina.Projections.*`; `Directory.Packages.props` gains Elasticsearch and Cosmos client packages (neither is referenced today; StackExchange.Redis 2.12.14 and MongoDB.Driver 3.12.0 already are).

**Provider category**: event sourcing for the source (Marten) and, for destinations, the database rule of AGENTS.md section 5 applies to the relational destination (D4); Redis, Elasticsearch and Cosmos DB are storage destinations outside the matrix, each consistent with the same `IProjectionDestination` contract.

**Size and decomposition (AGENTS.md section 11)**: this is an EPIC-sized issue. It must be decomposed into worker-sized sub-issues before any brief; each sub-issue is one PR of at most 15 files and 1,500 lines with its own tests, manifest entries (per file, per flag, one-sentence justification), PublicAPI lines and, where user visible, changelog fragment. The per-PR table below is the proposed decomposition, with files counted per PR (source, tests, manifests, PublicAPI, README, edits of shared files). No sub-issue uses a type, project or log class created by a later one: the worker's log class and EventIds are created in S4, the first logger.

| PR | Content | Files | Lines (estimate) |
|----|---------|-------|------------------|
| S1 | Move of the neutral contracts to the new `Encina.Projections` project and the namespace change in every consumer (decision 6) | about 160 (size exception, mechanical) | about 1,200 changed lines |
| S2 | `IProjectionDestination`, `ProjectionCheckpoint`, `InMemoryProjectionDestination`, shared contract suite, tests, manifest, PublicAPI | 9 | about 700 |
| S3 | Routing builder and table, checkpoint store abstraction and in-memory store, options, `AddEncinaProjections`, DI test | 12 | about 900 |
| S4 | `IProjectionEventSource`, `ProjectionWorker`, lag calculation, `ProjectionWorkerLog`, metrics, activity source, EventId range and allocation test, tests | 15 | about 1,400 |
| S5 | `Encina.Projections.Marten`: event source, destination, checkpoint document, `ToMarten()`, README, tests | 15 | about 1,200 |
| S6 | `Encina.Projections.Elasticsearch` (client package added to `Directory.Packages.props`) | 15 | about 1,200 |
| S7 | `Encina.Projections.Redis` | 15 | about 1,100 |
| S8 | `Encina.Projections.MongoDB` | 15 | about 1,100 |
| S9 | `Encina.Projections.CosmosDb` (client package and emulator fixture) | 15 | about 1,200 |
| S10 | `Encina.Projections.Sql.Ado`: shared base and SQL Server | 13 | about 1,100 |
| S11 | `Encina.Projections.Sql.Ado`: PostgreSQL and MySQL | 10 | about 800 |
| S12 | `Encina.Projections.Sql.Dapper`: shared base and SQL Server | 13 | about 1,000 |
| S13 | `Encina.Projections.Sql.Dapper`: PostgreSQL and MySQL | 10 | about 800 |
| S14 | `Encina.Projections.Sql.EFCore`: shared base and SQL Server | 13 | about 1,100 |
| S15 | `Encina.Projections.Sql.EFCore`: PostgreSQL and MySQL | 10 | about 800 |
| S16 | `ProjectionLagHealthCheck`, options, registration, tests | 7 | about 500 |
| S17 | Guide, ADR, `docs/INVENTORY.md`, changelog fragments | 5 | about 400 |

S1 is the one PR that cannot be split: a namespace change of types used by 153 files (counted on this branch: 58 in `tests/Encina.UnitTests`, 19 in `tests/Encina.GuardTests`, 19 in `src/Encina.Marten`, 9 in `Encina.Compliance.Retention`, 7 in `Encina.Compliance.ProcessorAgreements`, 7 in `tests/Encina.IntegrationTests`, the rest in other compliance packages and test projects) must compile as one change, because no forwarding types or aliases are allowed (AGENTS.md section 3).

---

## Design Choices

<details>
<summary><strong>1. Which destinations ship in 1.0 (issue Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Marten and Elasticsearch only (the issue's acceptance minimum) | Least work and test infrastructure; two destinations prove the abstraction | Redis, SQL, MongoDB and Cosmos DB users wait |
| B. Marten plus all five listed, delivered in stages (core and Marten, then Elasticsearch, then the others) | Matches the motivation; SPEC-000 DEC-009 puts everything with a free local emulator in 1.0 (Cosmos DB has one, Elasticsearch runs in Docker) | Much more code and test infrastructure |

### Chosen Option: **B — all, in stages** (recommended, pending the maintainer)

### Rationale

DEC-009 widened the 1.0 scope to everything that can be tested locally, and every listed destination can be. The staging makes each destination its own sub-issue, so B can stop after any stage. A wins when the abstraction should be proven by two destinations before more are written.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Home of the abstractions (issue Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. New `Encina.Projections` package | Destinations do not reference Marten; the contracts are already provider neutral (`ProjectionContext.GlobalPosition`) | One more package |
| B. `Encina.DomainModeling` or core | Fewer packages | The core grows with event-sourcing concepts |

### Chosen Option: **A — `Encina.Projections`** (recommended, pending the maintainer)

### Rationale

The names in the issue (`Encina.Projections.*`) already assume A, and a destination package that must reference `Encina.Marten` to reach `IProjection` would contradict provider coherence. B wins if projections are considered a core concept of every Encina application.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. When one destination of a projection fails (issue Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. The projection fails and retries all destinations | Consistent read models | A slow destination blocks the others |
| B. A checkpoint per projection and destination | Isolated failures; each destination catches up alone | Read models differ for a while; lag is reported per destination |

### Chosen Option: **B — checkpoint per destination** (recommended, pending the maintainer)

### Rationale

Destinations are chosen for different purposes (search, real time, reporting), so independence matches the use cases. A wins when destinations must agree at every moment.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. How the relational destination meets the database rule (new Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. A relational destination for all providers of the matrix: ADO.NET, Dapper and EF Core on SQL Server, PostgreSQL and MySQL, plus MongoDB (the `.MongoDB` destination), each as its own sub-issue | Follows AGENTS.md section 5 literally; a read model can live in any supported database | About ten implementations |
| B. SQL Server (ADO, Dapper, EF Core) first; other databases later | Matches the issue text ("SQL Server for BI tools"); smaller first delivery | The rule requires every provider-dependent feature on all 10; stopping at SQL Server needs an explicit exception |

### Chosen Option: **A — all providers, staged** (recommended, pending the maintainer)

### Rationale

Both are valid as a delivery order; only A ends rule compliant without an exception. B wins when the maintainer grants a time-boxed exception for the first release.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Destination contract and idempotent apply</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `IProjectionDestination<TReadModel>` with upsert-by-id, delete, and checkpoint (`GlobalPosition`) read and write, every method `Either<EncinaError, T>` | Replay is idempotent; matches `IReadModelRepository` | One more interface next to the repository |
| B. Reuse `IReadModelRepository<T>` as the write side | No new interface | The repository exposes queries and counts that search or cache destinations cannot honor |

### Chosen Option: **A — a dedicated destination contract** (recommended, pending the maintainer)

### Rationale

Every destination can honor upsert, delete and a position; the query side stays on each store's own client or on `IReadModelRepository` where the store supports it. Failures return `Left` and fail the batch for that destination; they are never swallowed. Option B also satisfies Railway Oriented Programming: `IReadModelRepository<T>` already returns `Either<EncinaError, ...>` (`GetByIdAsync`, `StoreAsync` in `src/Encina.Marten/Projections/IReadModelRepository.cs`), so the cited rules do not exclude it. B wins when one write and query contract should serve every store that can also query, at the price of search and cache destinations having to refuse the query members.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. How the namespace move is delivered</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. The neutral contracts (`IProjection`, `IProjectionHandler`, `IProjectionCreator`, `IProjectionDeleter`, `IReadModel`, `IReadModelRepository`, `ProjectionContext`, `ProjectionStatus`, `ProjectionRegistry` and its siblings, `ProjectionErrorCodes`; 7 files) move to the new project and the namespace becomes `Encina.Projections`, in one mechanical PR that touches the 153 consumer files, under the label `size-exception-approved` that the maintainer applies | The final names are right; no alias or forwarding type | One PR far over 15 files and 1,500 lines (it changes lines, not logic); the pull request size check needs the exception |
| B. The 7 files move to the new project but keep the namespace `Encina.Marten.Projections` | The move fits 15 files; no consumer changes | The namespace names the wrong package for good, or a later PR with the same 153 files has to rename it |
| C. The contracts stay in `Encina.Marten`; `Encina.Projections` holds only the destination abstraction and destination packages reference `Encina.Marten` | No move at all | A destination package depends on Marten (the reason for D2 disappears) |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

Pre-1.0 the best solution is done completely (AGENTS.md section 1), and the churn is mechanical; it is performed with a reviewed C# file-based script, not by hand and not by a PowerShell replace on source files, and if a hook blocks that step the step stops and is reported. B wins when the maintainer will not grant a size exception.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. What stays in Marten, and how the other destinations rebuild</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `IProjectionManager`, `RebuildOptions`, `ProjectionOptions`, `IInlineProjectionDispatcher` and the internal `ProjectionLog` (Marten EventIds 2664-2700) stay in `Encina.Marten`; the new worker's log class is `ProjectionWorkerLog`; the rebuild of a non-Marten destination is a reset of its checkpoint, after which the worker replays, and the zero-downtime Marten rebuild is the blue-green manager of #324 (open) | No name clash; the manager is Marten specific (`QueryAllRawEvents`); smallest move | The issue lists `IProjectionManager` among the moved abstractions |
| B. `IProjectionManager` moves and becomes destination aware | One rebuild API for every destination | A larger redesign that overlaps #324 |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

`MartenProjectionManager` replays from the Marten event store and keeps status in memory; a destination-aware manager is the design question the maintainer must answer together with #324. The issue's acceptance "rebuild across all destinations" is then met for non-Marten destinations by the checkpoint reset and for Marten by #324.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Where the durable checkpoint lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Next to the data: each destination package stores the checkpoint of a (projection, destination) pair in its own store (a Marten document, an Elasticsearch document, a Redis key, a row or document in the SQL, Mongo and Cosmos stores) | Atomic with the data where the store is transactional; no extra dependency | Every destination implements the checkpoint operations; the in-memory store of S3 is for tests only |
| B. One central checkpoint store (a PostgreSQL table through Marten) | One implementation | Every destination package depends on that store; a destination on another database loses atomicity |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

Each destination can then pass the same contract test for checkpoint round trips. B wins when all projections already run next to Marten and only the read model location varies.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Checkpoint key and apply order</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Key is (projection name, destination name); events are applied in global position order; the checkpoint advances only after the destination reports success (apply, then advance), so replay after a crash is idempotent by upsert | A failure never reports progress it did not make | An event can be applied twice after a crash, which idempotent upsert absorbs |
| B. Advance the checkpoint, then apply | At-most-once | A failed apply loses the event silently |

### Chosen Option: **A**

### Rationale

Option B reports success for an event that was not applied, which AGENTS.md section 3 forbids for background infrastructure (a `Left` fails the operation and never reports success).

**Class:** Settled
**Settled by:** AGENTS.md section 3 (errors are never swallowed in background infrastructure)

</details>

<details>
<summary><strong>10. Defaults and bounds of the worker</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `BatchSize` 1000 (the value of `RebuildBatchSize` in `ProjectionOptions`), `MaxParallelProjections` 4, `PollInterval` 1 second, `CatchUpThreshold` 100 events; all validated greater than zero | Starting points with a precedent in the existing option | The parallelism and the threshold have no measurement behind them (the load test of S4 measures them) |
| B. No defaults: every option required | No arbitrary number | Every user must choose before first use |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

Only the batch size has a precedent; the rest are the maintainer's to confirm or to replace with numbers from the load test.

**Class:** Dilemma

</details>

<details>
<summary><strong>11. Lock key and an unavailable lock</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Key `projection:{name}:{destination}`; contention (`Right(None)` under ADR-052, a null handle on the interface as on main today) skips the iteration for that pair; a lock fault fails that iteration and is logged by error code; the worker never writes without the lock, and with no `IDistributedLockProvider` registered startup validation fails | One writer per pair; no second writer on any outcome | A node without the provider cannot run the worker |
| B. Run unlocked when no provider is registered | Works on one node without configuration | Two nodes would write the same destination |

### Chosen Option: **A**

### Rationale

Function 7 of the cross-cutting check (AGENTS.md section 6) names `IDistributedLockProvider` for concurrent access to shared state, and ADR-052 fixes that contention is a normal outcome and a fault is an error; running unlocked removes the guarantee the lock exists for.

**Class:** Settled
**Settled by:** AGENTS.md section 6 (function 7) and ADR-052

</details>

<details>
<summary><strong>12. A batch that keeps failing (poison event)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Retry the batch with backoff up to `MaxAttempts` (default 5); then that (projection, destination) pair is marked `Faulted`, stops, is visible in the lag health check (unhealthy) and in the logs by error code, and the other pairs continue; an operator resets the checkpoint or fixes the handler | No event is skipped silently; no unbounded retry | The destination stays behind until an operator acts |
| B. Move the failing event to a projection dead-letter store and continue | The destination keeps going | A new dead-letter abstraction; the read model silently misses the event until someone reads the dead letters |
| C. Retry forever | No new state | A poison event blocks the destination for good with no state change an operator can see |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

AGENTS.md section 3 allows retry, dead letter or an exception per the semantics of the operation; which one a projection uses is a product choice. The default of 5 attempts is a starting point without measurement.

**Class:** Dilemma

</details>

<details>
<summary><strong>13. Projection with no destination, and inline plus worker on one read model</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. A projection with no destination named is a startup validation error, and a read model written inline into Marten may not also be routed to the Marten destination of the worker (startup validation error) | No silent double writer; no silent no-op | An existing inline projection keeps working only without a routing entry |
| B. No destination defaults to Marten, and both writers are allowed | Convenient | Two writers on one Marten read model race with each other (inline dispatch is read-your-writes, the worker is eventually consistent) |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

Inline dispatch (`UseInlineProjections`, default true) stays for read-your-writes and the worker adds eventually consistent destinations (Phase 3); two writers on the same read model would defeat both.

**Class:** Dilemma

</details>

<details>
<summary><strong>14. Names of indexes, collections, containers and Redis keys, and the Redis value shape</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. The name derives from the read model type name through one naming function, must match `^[a-z][a-z0-9_-]{0,62}$` after conversion, is rejected otherwise and can be overridden by the destination options; Cosmos DB partition key is the read model id; the Redis value is a JSON string (System.Text.Json) under the key `encina:projections:{name}:{id}`, because JSON modules and hashes of nested types are not available on every Redis-protocol server (Valkey, Garnet, KeyDB) | One rule for all destinations; no injection through a name; works on plain Redis-protocol servers | A type name that does not convert needs an override |
| B. Per-destination naming rules and Redis hashes | Each store uses its idiom | Nested read models do not fit a hash; names differ per package |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

Names reach DDL, index creation and key construction, so they are validated, not built from unvalidated input. B wins when a store's idiom matters more than uniformity.

**Class:** Dilemma

</details>

<details>
<summary><strong>15. Lag: threshold and time form</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Lag is measured in events (head position minus checkpoint position) and in time (now from `TimeProvider` minus the timestamp of the last applied event); the health check is Degraded above `LagDegradedThreshold` events (default 1000) and Unhealthy when a pair is `Faulted` | Both forms are available; no clock reads outside `TimeProvider` | The default is a starting point without measurement |
| B. Events only | Simpler | An idle stream with old events reports no lag in time |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

The time form needs the event timestamp from `ProjectionContext` metadata; the threshold is the maintainer's to confirm.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Extract the Provider-Agnostic Abstractions (S1 and S2)

<details>
<summary><strong>Tasks</strong></summary>

S1 (the move, decision 6; blocked until the maintainer answers decisions 2 and 6):

1. New project `src/Encina.Projections/` (referencing `Encina` and `Encina.DomainModeling`), added to `Encina.slnx`.
2. Move with `git mv` from `src/Encina.Marten/Projections/` the 7 files that hold the neutral contracts: `IProjection.cs` (`IProjection<T>`, `IProjectionHandler`, `IProjectionCreator`, `IProjectionDeleter`), `IReadModel.cs`, `IReadModelRepository.cs`, `ProjectionContext.cs`, `ProjectionStatus.cs`, `ProjectionRegistry.cs` (with `ProjectionRegistration` and `ProjectionHandlerType`) and `ProjectionErrorCodes.cs`; namespaces become `Encina.Projections` (decision 6, option A). `IProjectionManager`, `RebuildOptions`, `ProjectionOptions`, `IInlineProjectionDispatcher`, the Marten implementations and the internal `ProjectionLog` stay (decision 7). No forwarding types, no `[Obsolete]`.
3. Update every consumer and test namespace (153 files counted on this branch) with a reviewed C# file-based script, then build; move the matching manifest entries from `Encina.Marten.json` to a new `Encina.Projections.json` (targets do not go down; the ratchet compares the manifests with the base branch) and the PublicAPI lines of the moved types; the project gets its own `PublicAPI` files.
4. Acceptance by command: the full `Encina.slnx` builds with zero warnings; `dotnet test` of the unit, guard, contract, property and integration projects passes; the per-flag report shows no target lowered.

S2 (9 files): `IProjectionDestination<TReadModel>` and `ProjectionCheckpoint` (decisions 5, 8 and 9), `InMemoryProjectionDestination`, the shared contract test suite (an abstract base run against every destination), unit, guard and property tests (replaying any event prefix twice yields the same read model), `Encina.Projections.json` manifest entries and PublicAPI lines, in the same PR as the tests that meet them.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Projection contracts live in src/Encina.Marten/Projections and are provider neutral. Issue #535 moves them to a new Encina.Projections package (Dilemma D2, recommended A).
TASK: S1: create the project, move the 7 listed files with git mv, rewire Encina.Marten and all consumers and tests in one mechanical PR. S2: add IProjectionDestination, ProjectionCheckpoint, the in-memory destination and the shared contract suite.
KEY RULES: move completely, no forwarding aliases, no [Obsolete]; PublicAPI files only where the package has them (the new project creates its own); manifest targets never decrease and are entered in the PR that adds the tests; the solution builds with zero warnings after every PR.
REFERENCE FILES: src/Encina.Marten/Projections/IProjection.cs, src/Encina.Marten/Projections/ProjectionContext.cs, src/Encina.Marten/Projections/ProjectionRegistry.cs, .github/coverage-manifest/Encina.Marten.json
```

</details>

### Phase 2: Routing, Configuration and Checkpoint Store (S3, 12 files)

<details>
<summary><strong>Tasks</strong></summary>

1. `AddEncinaProjections(Action<ProjectionRoutingBuilder>)` with `Project<TReadModel>()` returning a builder that collects destinations; `ProjectionRoutingTable` resolves destinations per read model; a projection with no destination is a startup validation error (decision 13).
2. `IProjectionCheckpointStore` and an in-memory implementation for tests (a durable store comes with each destination package, decision 8).
3. Options (`ProjectionsOptions`: batch size, parallelism, poll interval, `CatchUpThreshold`, `MaxAttempts`, defaults per decisions 10 and 12) validated at registration; registration completeness test with `ValidateOnBuild` and `ValidateScopes`.
4. Tests and manifest entries in the same PR: routing and options unit tests, guard tests (`AddEncinaProjections` null services and delegate, option ranges), the DI test; PublicAPI lines for the new public types.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Encina.Projections exists with the contracts. Registration style: TryAdd* in AddEncina* extensions.
TASK: Implement the routing builder, the routing table, the checkpoint store abstraction with its in-memory default, the options and the DI registration.
KEY RULES: an AddEncinaProjections call registers every option and dependency it resolves (proven by a ValidateOnBuild test); a destination registered by the application is never overridden; XML docs with examples.
REFERENCE FILES: src/Encina.Marten/ServiceCollectionExtensions.cs, src/Encina.Messaging/ServiceCollectionExtensions.cs
```

</details>

### Phase 3: Async Projection Worker and Diagnostics (S4, 15 files)

<details>
<summary><strong>Tasks</strong></summary>

1. `IProjectionEventSource` (reads events after a `GlobalPosition` in order); the Marten implementation lives in `Encina.Projections.Marten` (S5).
2. `ProjectionWorker : BackgroundService`: per projection and destination it reads from its checkpoint, applies a batch through `IProjection`, writes through `IProjectionDestination`, then advances the checkpoint (D3-B, decision 9). `PeriodicTimer` loop catching per iteration; a `Left` fails that destination's batch (retry up to `MaxAttempts`, then `Faulted`, decision 12; error code logged), never advances the checkpoint, never stops other destinations.
3. Parallelism across projections bounded by options; a distributed lock per projection and destination (`IDistributedLockProvider`, decision 11) so only one node writes.
4. Lag per destination in events and in time (decision 15), `TimeProvider` only.
5. Diagnostics created here, the first PR that logs: `Projections = (7290, 7299)` registered in `src/Encina/Diagnostics/EventIdRanges.cs` (inside the block assigned to this family in `artifacts/decisions/plan-batch/eventid-blocks.md`) and `Encina.Projections` added to `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`; `ProjectionWorkerLog` with eight ids packed from 7290 (batch applied, checkpoint advanced, destination failed (code), pair faulted, lock contention, lock failed, lag sampled, worker stopped), 7298 and 7299 free; `ProjectionMetrics` and the activity source.
6. Tests and manifest entries in the same PR: worker unit tests with `FakeTimeProvider` (a thrown exception is never swallowed by the test), lag unit tests, diagnostics unit tests, guard tests, the property test of S2 re-run through the worker, the allocation test edit. The Marten-backed integration tests ship with S5.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: Routing and the checkpoint store exist. Inline projection dispatch (MartenInlineProjectionDispatcher) stays for read-your-writes; the worker adds eventually consistent destinations.
TASK: Implement IProjectionEventSource, ProjectionWorker, lag calculation and the lock per projection and destination, with ProjectionWorkerLog, metrics and the EventId range 7290-7299 registered first.
KEY RULES: a PeriodicTimer loop catches per iteration, a Left fails only that destination, cancellation from the stopping token ends the loop; TimeProvider only; never log EncinaError.Message; tests use FakeTimeProvider and never swallow a thrown exception.
REFERENCE FILES: src/Encina.Marten/Projections/MartenProjectionManager.cs, src/Encina.DistributedLock/IDistributedLockProvider.cs, docs/en/guides/TESTING.md
```

</details>

### Phase 4: Marten Destination and Event Source, Elasticsearch Destination (S5 and S6, 15 files each)

<details>
<summary><strong>Tasks</strong></summary>

1. S5, `Encina.Projections.Marten`: `MartenProjectionEventSource` (reads `QueryAllRawEvents` after a sequence), `MartenProjectionDestination<T>` (wraps today's `MartenReadModelRepository`), durable checkpoint document, `ToMarten()` builder extension, README.
2. S6, `Encina.Projections.Elasticsearch`: destination with index per read model (names per decision 14), bulk upsert, checkpoint document; `ToElasticsearch()`; client package added to `Directory.Packages.props` (the version is verified against the current stable release when S6 is briefed).
3. Each PR ships the contract test class that runs the shared suite of S2 against its destination, unit and guard tests, an integration test (Marten on PostgreSQL; Elasticsearch in Docker) on the shared fixtures, its manifest entries and PublicAPI lines.
4. EventIds: these packages need a range of their own; none is assigned, because the block 7250-7299 is consumed by #320, #323, #324, #326 and this plan. A new block is to be requested from the batch registry before S5 is briefed; until then a destination package logs nothing.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: The worker and contracts exist. Elasticsearch and Cosmos clients are not yet in Directory.Packages.props.
TASK: Implement the Marten source and destination and the Elasticsearch destination, with their builder extensions and contract tests.
KEY RULES: every destination exposes the same IProjectionDestination behavior; connection settings in options classes override ToString() and use [JsonIgnore]; resilience (retry, timeout) on external calls; integration tests use shared collection fixtures, never a per-class container.
REFERENCE FILES: src/Encina.Marten/Projections/MartenReadModelRepository.cs, src/Encina.Testing.Testcontainers, tests/Encina.ContractTests/Marten/Core/MartenReadModelRepositoryContractTests.cs
```

</details>

### Phase 5: Remaining Destinations (repeated pattern, one sub-issue each: S7 to S15)

<details>
<summary><strong>Tasks</strong></summary>

1. S7 `.Redis` (StackExchange.Redis, JSON string values per decision 14; Redis-protocol servers such as Valkey and Garnet work through the same client), S8 `.MongoDB` (collection per read model), S9 `.CosmosDb` (container per read model; free local emulator for tests).
2. S10 to S15 `.Sql.*`, two sub-issues per family (D4-A): ADO.NET, Dapper and EF Core, each with SQL Server plus the shared base first and PostgreSQL plus MySQL second, with scripts per database (`@param`, `TOP (@n)` versus `LIMIT @n`, quoting rules of AGENTS.md section 5); table names validated per decision 14.
3. Each destination passes the shared contract test suite from S2 and ships its own integration test on the matching shared fixture (`[Collection("<Family>-<Database>")]`), unit and guard tests, manifest entries and PublicAPI lines, and a package README (counted in its PR); the file counts per PR are in the table at the top.
4. Resilience (retry, timeout) on external calls lives in each destination package; a destination-neutral resilience option shared by all packages is not part of this plan and is named in the S3 brief if the maintainer wants one.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: The contract test suite and two destinations exist. This phase is run once per destination, each as its own worker issue.
TASK: Implement one destination package against IProjectionDestination, with its builder extension, options and tests.
KEY RULES: database destinations use async calls with CancellationToken only; the same behavior on every provider; Testcontainers (or the emulator) through shared fixtures; package README per package.
REFERENCE FILES: src/Encina.Projections.Marten, src/Encina.Caching.Redis, src/Encina.MongoDB
```

</details>

### Phase 6: Observability and Health (diagnostics in S4, health check in S16)

<details>
<summary><strong>Tasks</strong></summary>

1. The EventId range, `ProjectionWorkerLog` (not `ProjectionLog`: the Marten class of that name stays, decision 7), `ProjectionMetrics` (`encina.projections.lag_seconds` gauge, `encina.projections.events_processed` counter, tags projection and destination) and the activity `encina.projections.apply` are created in S4 (Phase 3 task 5).
2. S16: `ProjectionLagHealthCheck : IEncinaHealthCheck` (Degraded above `LagDegradedThreshold`, Unhealthy when a pair is `Faulted`, decisions 12 and 15), its options and registration, unit and guard tests, manifest entries; 7 files.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: The worker exists; the range 7290-7299 is assigned to #535's core package from the family block 7250-7299.
TASK: S4 registers the range first, then adds the log class, the meter and the activity source; S16 adds the lag health check.
KEY RULES: sequential EventIds; error codes only in logs; health check with DefaultName const and static Tags, scoped resolution through CreateScope().
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Marten/Health/MartenHealthCheck.cs, src/Encina.Tenancy/Diagnostics/TenancyMetrics.cs
```

</details>

### Phase 7: Testing (not a phase: every sub-issue carries its own tests)

<details>
<summary><strong>Tasks</strong></summary>

1. Each sub-issue ships the unit, guard, contract, property and integration tests that apply to its files, and the per-file targets with one-sentence justifications in the matching manifest (`Encina.Projections.json` or the destination package's `Encina.Projections.<Name>.json`, `mechanical-fixer`), in the same PR as the tests that meet them; its acceptance by command is the per-flag table from the coverage report and `--check-justifications`. The contract suite is written once in S2 and run against every destination.
2. Load test (parallel projections) and benchmark (throughput per destination): written with S4 and S5 where the code exists; a destination without them states the justification `.md` in its PR (allowed for load tests and benchmarks, never for unit, guard, contract or database-integration tests).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT: Coverage is per flag against the manifests of the new packages.
TASK: Write the test types of the plan for the package at hand.
KEY RULES: tests execute real code; Shouldly through Encina.Testing.Shouldly; FsCheck through Encina.Testing.FsCheck; benchmarks use BenchmarkSwitcher, never BenchmarkRunner.Run<T>().
REFERENCE FILES: tests/Encina.ContractTests, tests/Encina.PropertyTests, docs/testing/coverage-measurement-methodology.md
```

</details>

### Phase 8: Documentation and Finalization (S17, 5 files; READMEs ship with their packages)

<details>
<summary><strong>Tasks</strong></summary>

1. Documentation (`docs-writer`): projections guide with an example for each destination, ADR for D1 to D4 and decisions 5 to 15 (ADR number assigned when the ADR pull request opens), `docs/INVENTORY.md`, `ROADMAP.md` check; one README per package is part of that package's PR.
2. Changelog fragments `changelog.d/535-extended-projections.added.md` (S17) and `changelog.d/535-projection-abstractions-moved.changed.md` (in S1, with the move); `PublicAPI` lines ship with the PR that adds the types (`mechanical-fixer`).
3. Verification in every PR: zero warnings, every flag at target in every touched manifest, CRAP of changed methods at most 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT: Code and tests of the sub-issue are done.
TASK: Finish the package README, the guide section, the fragment and the PublicAPI lines.
KEY RULES: English only; no hand-typed coverage figures; one Diataxis quadrant per page.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| SPEC-000 DEC-009 and section 6 | Destinations with a free local emulator are in 1.0 scope |
| AGENTS.md section 5 | Database rule for the relational destination |
| ADR-027, ADR-021 | Marten as event store; EventId registry |
| Projection rebuild patterns (event-driven.io) | Async projections with checkpoints |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| Projection contracts, `ProjectionContext`, `ProjectionRegistry` | `src/Encina.Marten/Projections` | Moved to `Encina.Projections` |
| `MartenReadModelRepository`, `MartenProjectionManager` | `src/Encina.Marten/Projections` | Basis of the Marten destination |
| `IDistributedLockProvider` | `src/Encina.DistributedLock/IDistributedLockProvider.cs` | One writer per projection and destination |
| `MartenHealthCheck` | `src/Encina.Marten/Health/MartenHealthCheck.cs` | Pattern for the lag health check |
| `Encina.MongoDB`, `Encina.Caching.Redis` | `src/Encina.MongoDB`, `src/Encina.Caching.Redis` | Client usage patterns for two destinations |
| `Encina.Testing.Testcontainers` | `src/Encina.Testing.Testcontainers` | Docker fixtures for integration tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Projections` (new range `Projections`) | 7290-7299 (ids 7290-7297 used) | Inside the block 7250-7299 assigned to the family in the batch registry (`artifacts/decisions/plan-batch/eventid-blocks.md`), which records the sub-ranges of the sibling plans |
| Each `Encina.Projections.*` destination | block to be requested | The block 7250-7299 is fully consumed; a new block is requested from the batch registry before S5 is briefed (a package that logs needs a range of its own, ADR-021) |

### Estimated File Count

The authoritative count is the per-PR table in "Size and decomposition" at the top (S1 about 160 files under a size exception, S2 to S17 192 files in 16 PRs of at most 15 files, the sum is the Estimated Scope in the header). Counts include source, tests, manifests, PublicAPI, README, `Directory.Packages.props` and `Encina.slnx` edits and the registry edits; they are estimates that each sub-issue replaces with its exact file list and measured baseline. The statement of the total is the one in the header.

| Group | PRs | Files | Notes |
|-------|-----|-------|-------|
| Namespace move | S1 | about 160 | One mechanical PR under a size exception (decision 6) |
| Core package | S2, S3, S4 | 36 | 9 + 12 + 15 |
| Marten and Elasticsearch | S5, S6 | 30 | 15 + 15 |
| Redis, MongoDB, Cosmos DB | S7, S8, S9 | 45 | 15 each |
| Relational (ADO.NET, Dapper, EF Core) | S10 to S15 | 69 | 13 + 10 per family |
| Health, docs | S16, S17 | 12 | 7 + 5 |
| **Total** | **17** | **about 352** | 160 + 192; S2 to S17 are 192 files over 16 PRs |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14. Issue #535 makes projections provider agnostic and multi-destination. It is EPIC-sized: each destination is its own sub-issue and PR. Follow AGENTS.md: ROP, provider coherence, background loops never throw, TimeProvider, per-flag coverage, no compatibility layers.

IMPLEMENTATION OVERVIEW: S1 extract Encina.Projections (move, no aliases, one mechanical PR). S2 destination contract and shared contract suite. S3 routing, options, checkpoint store. S4 async worker with a checkpoint per projection and destination, EventIds 7290-7299, diagnostics. S5 and S6 Marten and Elasticsearch destinations. S7 to S15 Redis, MongoDB, Cosmos DB and the relational destinations, one sub-issue each. S16 lag health. S17 docs. Every sub-issue carries its own tests and manifest entries. Dilemmas D1 (destinations), D2 (home), D3 (failure policy), D4 (relational scope) and the decisions marked Dilemma in Design Choices follow the maintainer's answers.

KEY PATTERNS: Either for every operation; idempotent upsert plus checkpoint; PeriodicTimer loops catching per iteration; TryAdd* registration proven by a ValidateOnBuild test; sequential EventIds registered first; shared integration fixtures.

REFERENCE FILES: src/Encina.Marten/Projections, src/Encina.Marten/ServiceCollectionExtensions.cs, src/Encina.DistributedLock/IDistributedLockProvider.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/plans/dsr-implementation-plan-404.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The Redis destination is storage, not a cache layer, and no cache sits in front of projected read models on main, so there is nothing to integrate |
| 2 | OpenTelemetry | ✅ | Lag gauge, throughput counter, apply span (S4) |
| 3 | Structured Logging | ✅ | `ProjectionWorkerLog` with EventIds 7290-7297, error codes only (S4) |
| 4 | Health Checks | ✅ | Lag health check (S16) |
| 5 | Validation | ❌ | Projections process internal, already validated events; options are validated at registration |
| 6 | Resilience | ✅ | Retry, timeout and circuit breaker on external destination calls inside each destination package (S5 to S15) |
| 7 | Distributed Locks | ✅ | One writer per projection and destination (decision 11, S4) |
| 8 | Transactions | ❌ | Idempotent upsert plus apply-then-advance (decision 9) make replay correct without a transaction across the destination and its checkpoint; a destination whose store is transactional writes both in one transaction inside its own package |
| 9 | Idempotency | ✅ | Upsert by id and a checkpoint make replay idempotent (S2 and S4) |
| 10 | Multi-Tenancy | ⏭️ | Tenant-scoped projections follow #326 (open, tenant-aware event sourcing); nothing tenant specific is built here |
| 11 | Module Isolation | ❌ | Marten has no module-scoped streams or destinations today, so there is nothing to scope |
| 12 | Audit Trail | ❌ | Projections are derived infrastructure; the events are the audit record |

---

## Prerequisites & Dependencies

- Decomposition before briefs (AGENTS.md section 11): the 17 sub-issues S1 to S17 of the table at the top, in that order. Each carries its own exact file list, measured baseline (with the command), per-file targets with justification and acceptance by command.
- #324 (blue-green rebuild, open) builds on the same registry; the move in S1 renames its namespaces mechanically. Brief S1 and #324 PR 1 in sequence, not in parallel.
- #326 (tenancy, open) is independent; tenant-scoped projections wait for it.
- A new EventId block for the destination packages is requested from the batch registry before S5.
- New central package versions (Elasticsearch, Cosmos DB clients, matching Testcontainers modules) are verified against the current stable releases when S5 and S8 are briefed.

---

## Next Steps

1. The maintainer answers the Dilemmas (D1 to D4 and decisions 5 to 8, 10, 12 to 15) when work on #535 starts; the answers go into `## Maintainer Decisions`; decision 6 also decides whether the maintainer applies `size-exception-approved` to S1.
2. Decompose #535 into the sub-issues above (the orchestrator opens them) and write the brief for S1.
3. Re-check the Design Choices and run `adversarial-reviewer` on the Settled choices (9 and 11) before the first brief.
