# Implementation Plan: Extended Projections — Multi-Destination and Provider-Agnostic Support

> **Issue**: [#535](https://github.com/dlrivada/Encina/issues/535)
> **Type**: Feature
> **Complexity**: Very High (8 phases, about 14 PRs, a new package family; decomposed into sub-issues before any worker is briefed)
> **Estimated Scope**: about 120 files, about 9,000 changed lines over about 14 PRs

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

**Size and decomposition (AGENTS.md section 11)**: this is an EPIC-sized issue. It must be decomposed into worker-sized sub-issues before any brief; the proposed split is in "Prerequisites & Dependencies". The plan details the first group (Phases 1 to 4) and gives the destination phases (5 and 6) as one repeatable pattern.

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

### Chosen Option: **A — a dedicated destination contract**

### Rationale

Every destination can honor upsert, delete and a position; the query side stays on each store's own client or on `IReadModelRepository` where the store supports it. Failures return `Left` and fail the batch for that destination; they are never swallowed.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (Railway Oriented Programming; errors never swallowed in background infrastructure; provider coherence)

</details>

---

## Implementation Phases

### Phase 1: Extract the Provider-Agnostic Abstractions

<details>
<summary><strong>Tasks</strong></summary>

1. New project `src/Encina.Projections/` (referencing `Encina` and `Encina.DomainModeling`).
2. Move from `src/Encina.Marten/Projections/`: `IProjection`, `IProjectionHandler`, `IProjectionCreator`, `IProjectionDeleter`, `IReadModel`, `ProjectionContext`, `IReadModelRepository`, `ProjectionStatus`, `ProjectionRegistry` and the neutral error codes; namespaces become `Encina.Projections`. Marten keeps its implementations and references the new package. No forwarding types, no `[Obsolete]`.
3. Add `IProjectionDestination<TReadModel>` and `ProjectionCheckpoint` (D5).
4. Update every consumer and test namespace; move the matching manifest entries from `Encina.Marten.json` to a new `Encina.Projections.json` (targets do not go down).
5. Split into two PRs because of the move churn: 1a contracts and registry, 1b repository, status and Marten rewiring.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Projection contracts live in src/Encina.Marten/Projections and are provider neutral. Issue #535 moves them to a new Encina.Projections package (Dilemma D2, recommended A).
TASK: Create the project, move the listed types with git mv, add IProjectionDestination and ProjectionCheckpoint, rewire Encina.Marten and all tests.
KEY RULES: move completely, no forwarding aliases, no [Obsolete]; PublicAPI files for both packages; manifest targets never decrease; the solution builds with zero warnings after every PR.
REFERENCE FILES: src/Encina.Marten/Projections/IProjection.cs, src/Encina.Marten/Projections/ProjectionContext.cs, src/Encina.Marten/Projections/ProjectionRegistry.cs, .github/coverage-manifest/Encina.Marten.json
```

</details>

### Phase 2: Routing, Configuration and Checkpoint Store

<details>
<summary><strong>Tasks</strong></summary>

1. `AddEncinaProjections(Action<ProjectionRoutingBuilder>)` with `Project<TReadModel>()` returning a builder that collects destinations; `ProjectionRoutingTable` resolves destinations per read model.
2. `IProjectionCheckpointStore` and an in-memory implementation for tests (a durable store comes with each destination package, so the checkpoint lives next to the data it describes).
3. Options (`ProjectionsOptions`: batch size, parallelism, `CatchUpThreshold`) validated at registration; registration completeness test with `ValidateOnBuild` and `ValidateScopes`.

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

### Phase 3: Async Projection Worker

<details>
<summary><strong>Tasks</strong></summary>

1. `IProjectionEventSource` (reads events after a `GlobalPosition` in order); the Marten implementation lives in `Encina.Projections.Marten` (Phase 4).
2. `ProjectionWorker : BackgroundService`: per projection and destination it reads from its checkpoint, applies a batch through `IProjection`, writes through `IProjectionDestination`, then advances the checkpoint (D3-B). `PeriodicTimer` loop catching per iteration; a `Left` fails that destination's batch (retry next iteration, error code logged), never advances the checkpoint, never stops other destinations.
3. Parallelism across projections bounded by options; a distributed lock per projection and destination (`IDistributedLockProvider`) so only one node writes.
4. Lag per destination = head position minus checkpoint position (`TimeProvider` for the time form).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: Routing and the checkpoint store exist. Inline projection dispatch (MartenInlineProjectionDispatcher) stays for read-your-writes; the worker adds eventually consistent destinations.
TASK: Implement IProjectionEventSource, ProjectionWorker, lag calculation and the lock per projection and destination.
KEY RULES: a PeriodicTimer loop catches per iteration, a Left fails only that destination, cancellation from the stopping token ends the loop; TimeProvider only; never log EncinaError.Message; tests use FakeTimeProvider and never swallow a thrown exception.
REFERENCE FILES: src/Encina.Marten/Projections/MartenProjectionManager.cs, src/Encina.DistributedLock/IDistributedLockProvider.cs, docs/en/guides/TESTING.md
```

</details>

### Phase 4: Marten Destination and Event Source, Elasticsearch Destination

<details>
<summary><strong>Tasks</strong></summary>

1. `Encina.Projections.Marten`: `MartenProjectionEventSource` (reads `QueryAllRawEvents` after a sequence), `MartenProjectionDestination<T>` (wraps today's `MartenReadModelRepository`), durable checkpoint document, `ToMarten()` builder extension.
2. `Encina.Projections.Elasticsearch`: destination with index per read model, bulk upsert, checkpoint document; `ToElasticsearch()`; client package added to `Directory.Packages.props`.
3. Contract tests that every destination passes (upsert idempotence, delete, checkpoint round trip); integration tests with Marten on PostgreSQL and Elasticsearch in Docker via the shared fixtures.

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

### Phase 5: Remaining Destinations (repeated pattern, one sub-issue each)

<details>
<summary><strong>Tasks</strong></summary>

1. `.Redis` (StackExchange.Redis hashes or JSON; Redis-protocol caches such as Valkey and Garnet work through the same client), `.MongoDB` (collection per read model), `.CosmosDb` (container per read model; free local emulator for tests).
2. `.Sql`, one sub-issue per family and database (D4-A): ADO.NET, Dapper and EF Core on SQL Server, PostgreSQL and MySQL, with scripts per database (`@param`, `TOP (@n)` versus `LIMIT @n`, quoting rules of AGENTS.md section 5).
3. Each destination passes the shared contract test suite from Phase 4 and ships its own integration test on the matching shared fixture.

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

### Phase 6: Observability and Health

<details>
<summary><strong>Tasks</strong></summary>

1. Register `Projections = (7290, 7299)` in `src/Encina/Diagnostics/EventIdRanges.cs` for the core worker and add `Encina.Projections` to `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`. Each destination package takes a range from the orchestrator's registry when its sub-issue is briefed (to be assigned by the orchestrator).
2. `ProjectionLog` (`[LoggerMessage]`), `ProjectionMetrics` (`encina.projections.lag_seconds` gauge, `encina.projections.events_processed` counter, tags projection and destination), activity `encina.projections.apply`.
3. `ProjectionLagHealthCheck : IEncinaHealthCheck` (degraded above a configured lag).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: The worker exists; the range 7290-7299 is assigned to #535's core package from the family block 7250-7299.
TASK: Register the range first, then add the log class, the meter, the activity source and the lag health check.
KEY RULES: sequential EventIds; error codes only in logs; health check with DefaultName const and static Tags, scoped resolution through CreateScope().
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Marten/Health/MartenHealthCheck.cs, src/Encina.Tenancy/Diagnostics/TenancyMetrics.cs
```

</details>

### Phase 7: Testing

<details>
<summary><strong>Tasks</strong></summary>

1. Unit and guard tests for routing, checkpointing and the worker; contract tests (one suite, run against every destination); property tests: replaying any event prefix twice yields the same read model.
2. Integration tests per destination; load tests for parallel projections; benchmarks for throughput per destination.
3. Per-file targets with justifications in `.github/coverage-manifest/Encina.Projections*.json` (`mechanical-fixer`).

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

### Phase 8: Documentation and Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. Documentation (`docs-writer`): projections guide with an example for each destination, one README per package, ADR for D1 to D4 (ADR number assigned when the ADR pull request opens), `docs/INVENTORY.md`, `ROADMAP.md` check.
2. Changelog fragments `535-extended-projections.added.md` and `535-projection-abstractions-moved.changed.md`; `PublicAPI` lines (`mechanical-fixer`).
3. Verification: zero warnings, every flag at target in every touched manifest, CRAP of changed methods at most 10.

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
| `Encina.Projections` (new range `Projections`) | 7290-7299 | From the family block 7250-7299: #320 owns 7250-7259, #323 7260-7269, #324 7270-7279, #326 7280-7289, this plan 7290-7299 |
| Each `Encina.Projections.*` destination | to be assigned by the orchestrator | One range per package when its sub-issue is briefed |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core package (contracts moved, destination, routing, worker, options) | 24 | Two PRs for the move |
| Marten and Elasticsearch packages | 18 | Source, destination, options, DI, README |
| Redis, MongoDB, Cosmos packages | 21 | Seven files per package |
| Relational packages (ADO, Dapper, EF Core on three databases) | 27 | Three family packages, scripts per database |
| Diagnostics and health | 4 | Log, metrics, health, activity |
| Tests (unit, guard, contract, property, integration, load, benchmark) | 20 | Contract suite shared by all destinations |
| Docs, changelog, PublicAPI, manifests | 10 | Guide, ADR, fragments, manifests |
| **Total** | **124** | About 14 PRs |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14. Issue #535 makes projections provider agnostic and multi-destination. It is EPIC-sized: each destination is its own sub-issue and PR. Follow AGENTS.md: ROP, provider coherence, background loops never throw, TimeProvider, per-flag coverage, no compatibility layers.

IMPLEMENTATION OVERVIEW: Phase 1 extract Encina.Projections (move, no aliases). Phase 2 routing, options, checkpoint store. Phase 3 async worker with a checkpoint per projection and destination. Phase 4 Marten source and destination, Elasticsearch destination, shared contract suite. Phase 5 Redis, MongoDB, Cosmos DB and the relational destinations, one sub-issue each. Phase 6 EventIds 7290-7299, diagnostics, lag health. Phase 7 tests. Phase 8 docs. Dilemmas D1 (destinations), D2 (home), D3 (failure policy) and D4 (relational scope) follow the maintainer's answers.

KEY PATTERNS: Either for every operation; idempotent upsert plus checkpoint; PeriodicTimer loops catching per iteration; TryAdd* registration proven by a ValidateOnBuild test; sequential EventIds registered first; shared integration fixtures.

REFERENCE FILES: src/Encina.Marten/Projections, src/Encina.Marten/ServiceCollectionExtensions.cs, src/Encina.DistributedLock/IDistributedLockProvider.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/plans/dsr-implementation-plan-404.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Caching projected read models in front of a destination; the Redis destination is storage, not a cache layer; open an issue after the first destinations exist |
| 2 | OpenTelemetry | ✅ | Lag gauge, throughput counter, apply span (Phase 6) |
| 3 | Structured Logging | ✅ | `ProjectionLog` with EventIds 7290-7299, error codes only (Phase 6) |
| 4 | Health Checks | ✅ | Lag health check (Phase 6) |
| 5 | Validation | ❌ | Projections process internal, already validated events; options are validated at registration |
| 6 | Resilience | ✅ | Retry, timeout and circuit breaker on external destination calls inside each destination package (Phases 4 and 5) |
| 7 | Distributed Locks | ✅ | One writer per projection and destination (Phase 3) |
| 8 | Transactions | ⏭️ | Atomic apply plus checkpoint is possible only for transactional destinations (Marten, SQL, Mongo transactions); idempotent upsert makes it unnecessary for correctness; revisit per destination |
| 9 | Idempotency | ✅ | Upsert by id and a checkpoint make replay idempotent (Phases 1 and 3) |
| 10 | Multi-Tenancy | ⏭️ | Tenant-scoped projections follow #326 and the tenant issues #737 to #741 |
| 11 | Module Isolation | ⏭️ | Module-scoped destinations are not designed today; open an issue when a module needs them |
| 12 | Audit Trail | ❌ | Projections are derived infrastructure; the events are the audit record |

---

## Prerequisites & Dependencies

- Decomposition before briefs (AGENTS.md section 11). Proposed sub-issues in order: S1a contracts move, S1b repository and Marten rewiring, S2 routing and checkpoints, S3 async worker, S4 Marten source and destination, S5 Elasticsearch, S6 Redis, S7 MongoDB, S8 Cosmos DB, S9 to S11 relational ADO.NET, Dapper and EF Core (three databases each), S12 diagnostics and health. Each carries its own file list, baseline, per-file targets and acceptance by command.
- #324 (blue-green rebuild) builds on the same registry; the move in S1 renames its namespaces mechanically. Brief S1 and #324 PR 1 in sequence, not in parallel.
- #326 (tenancy) is independent; tenant-scoped projections wait for it.
- New central package versions (Elasticsearch, Cosmos DB clients, matching Testcontainers modules) are verified against the current stable releases when S5 and S8 are briefed.

---

## Next Steps

1. The maintainer answers D1 to D4 when work on #535 starts; the answers go into `## Maintainer Decisions`.
2. Decompose #535 into the sub-issues above (the orchestrator opens them) and write the brief for S1a.
3. Re-check the Design Choices and run `adversarial-reviewer` on the Settled choice before the first brief.
