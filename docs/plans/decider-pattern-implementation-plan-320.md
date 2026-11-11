# Implementation Plan: Decider Pattern Support for Functional Event Sourcing (`Encina.Marten`)

> **Issue**: [#320](https://github.com/dlrivada/Encina/issues/320)
> **Type**: Feature
> **Complexity**: Medium (5 phases, 2 PRs, one provider)
> **Estimated Scope**: about 26 files (about 14 source and test-helper files, the rest tests, docs and manifest entries), about 1,900 changed lines over 2 PRs

---

## Summary

Event-sourced aggregates in Encina are class based today: `IAggregateRepository<TAggregate>` (`src/Encina.Marten/IAggregateRepository.cs`), `MartenAggregateRepository<TAggregate>` and `SnapshotAwareAggregateRepository<TAggregate>` over `IAggregate` (`src/Encina.DomainModeling/IAggregate.cs`), with `AggregateTestBase` in `src/Encina.Testing/EventSourcing`. This plan adds the **Decider pattern** next to them: business logic as two pure functions, `Decide(command, state)` and `Evolve(state, event)`, so decisions are tested as data in, data out, without mocks.

**What it implements**

- `IDecider<TCommand, TEvent, TState>` with `Decide` returning `Either<EncinaError, IReadOnlyList<TEvent>>` (an invalid command is a `Left`, never an empty success), `Evolve`, `InitialState` and `IsTerminal`.
- `DeciderBase<TCommand, TEvent, TState>` (helpers for `Left`/`Right` construction and a default `IsTerminal`).
- A Marten repository, `DeciderAggregateRepository<TCommand, TEvent, TState>`, that loads the stream, folds it through `Evolve`, calls `Decide` and appends the result with optimistic concurrency; it returns the new events and the new state.
- `EncinaMartenOptions.UseDeciders` (opt-in, default false) and `AddDecider<TDecider, TCommand, TEvent, TState>()` registration.
- A Given/When/Then test helper in `Encina.Testing.EventSourcing` for deciders.

**Standards**: AGENTS.md section 3 (ROP, opt-in, `TimeProvider`, errors never swallowed), ADR-001, ADR-006, ADR-018, ADR-019, ADR-021, ADR-027.

**Affected packages**: `Encina.Marten` (code, `PublicAPI.Unshipped.txt`, manifest), `Encina.DomainModeling` only if Dilemma D1 chooses option A, `Encina.Testing` (namespace `Encina.Testing.EventSourcing`). No `Encina.EventStoreDB` package exists (future work).

**Provider category**: event sourcing (Marten primary, AGENTS.md section 5). The 10-database rule does not apply; integration tests run against Marten on PostgreSQL through Testcontainers.

**Size and split (AGENTS.md section 11)**: about 26 files exceed the 15-file limit, so the work ships as two worker PRs. PR 1: Phases 1 to 3 (abstractions, repository, options and DI, with unit, guard and contract tests; about 15 files, about 1,200 lines). PR 2: Phases 4 and 5 (diagnostics, test helper, property and integration tests, documentation; about 11 files, about 700 lines).

---

## Design Choices

<details>
<summary><strong>1. Home of <code>IDecider</code> (issue Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `Encina.DomainModeling` | Provider-agnostic, a future EventStoreDB provider reuses it, the pure abstraction has no store reference | DomainModeling gains an event-sourcing concept (it already holds `IAggregate` and `IDomainEvent`, so the concept is not new) |
| B. `Encina.Marten` | Smaller surface, no change to DomainModeling | The pure abstraction is tied to Marten; moving it later is a breaking move |

### Chosen Option: **A — `Encina.DomainModeling`** (recommended, pending the maintainer)

### Rationale

`IDecider` needs only `ICommand`, `IDomainEvent`, `Either` and `EncinaError`, none of which is Marten specific, and DomainModeling already hosts `IAggregate`, `AggregateBase` and `IDomainEvent`. A reader who expects a second event store (EventStoreDB is listed as future work in AGENTS.md section 5) wants A; if Marten stays the only store for the foreseeable future (ADR-027), B costs less and avoids touching DomainModeling's manifest.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Coexistence with class-based aggregates (issue Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Both models coexist | Existing event-sourced modules keep working; gradual adoption | Two models to document and test |
| B. Deciders only, the class-based repositories are removed | One model | Every module on `IAggregateRepository` must be rewritten |

### Chosen Option: **A — both coexist** (recommended, pending the maintainer)

### Rationale

`IAggregateRepository<T>` is consumed by event-sourced compliance modules (ADR-019: consent, DSR, retention and others register aggregates through `AddAggregateRepository<T>()` and mock `IAggregateRepository` in unit tests per AGENTS.md section 3). Removing it is a rewrite of those modules and is outside this feature. B wins only when no existing aggregate has to be migrated, which is not today's repository.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How a command reaches the decider: handler base class or pipeline behavior</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. A handler calls `IDeciderRepository.ExecuteAsync(streamId, command)` explicitly | Visible, ordinary handler, no magic, works with every existing pipeline behavior | One short handler per command |
| B. A pipeline behavior resolves the decider for commands marked `IDecidable` and short-circuits the handler | No handler code per command | A behavior that replaces the handler breaks the request/handler model and hides where the append happens |

### Chosen Option: **A — explicit repository call from the handler** (recommended, pending the maintainer)

### Rationale

The issue lists a "pipeline behavior for decider-based commands". Option A keeps the Encina request model intact; the only behavior needed is the existing `EventPublishingPipelineBehavior` (`src/Encina.Marten/EventPublishingPipelineBehavior.cs`) that publishes committed events. B wins for a uniform command set where per-command handlers are pure boilerplate; it can be added later on top of A without changing it.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Error semantics of <code>Decide</code> and of the repository</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `Either<EncinaError, IReadOnlyList<TEvent>>`; every failure is a `Left` with an error code | Matches ROP; an invalid command is visible | Slightly heavier signatures |
| B. `IEnumerable<TEvent>`, empty list means "rejected" (the original proposal) | Simple | Silent swallow: success and rejection look the same |

### Chosen Option: **A — `Either`**

### Rationale

AGENTS.md section 3 requires `Either<EncinaError, T>` and forbids swallowed errors. The repository also returns `Left` for a terminal state (`marten.decider_terminal_state`), a concurrency conflict (reusing `MartenErrorCodes.ConcurrencyConflict`) and load or save failures (reusing `LoadFailed`, `SaveFailed`). Time-dependent events take their timestamp from the command (set by the calling handler through `TimeProvider`), so `Decide` stays pure.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (Railway Oriented Programming; time comes from `TimeProvider`), ADR-001, ADR-006

</details>

<details>
<summary><strong>5. Loading and appending in the Marten repository</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Mirror `MartenAggregateRepository`: `Events.FetchStreamAsync`, fold with `Evolve`, `Events.Append(id, expectedVersion, events)`, `SaveChangesAsync`, then `InlineProjectionRelay` | Same concurrency, metadata enrichment and projection behavior as today; proven code path | Duplicates a little of the load/append sequence |
| B. Marten's `FetchForWriting` / aggregate stream API | Less code | Different concurrency and metadata path from the class-based repository; two behaviors to reason about |

### Chosen Option: **A — mirror the existing repository**

### Rationale

Following the established pattern keeps optimistic concurrency, `EventMetadataEnrichmentService` and inline projections identical for both models (`src/Encina.Marten/MartenAggregateRepository.cs`, lines 91 to 220).

**Class:** Trivial
**Settled by:** AGENTS.md section 3 (provider coherence) and the existing `MartenAggregateRepository` implementation

</details>

<details>
<summary><strong>6. Opt-in switch</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `EncinaMartenOptions.UseDeciders` (default false) gates the infrastructure; `AddDecider<...>()` registers one decider | Pay-for-what-you-use; one visible switch like `Projections.Enabled` | Two steps for the user |
| B. Only `AddDecider<...>()`, no flag | One step | Inconsistent with the other opt-in features of the options class |

### Chosen Option: **A — `UseDeciders` plus `AddDecider`**

### Rationale

AGENTS.md section 3 makes every feature opt-in, and `EncinaMartenOptions` already exposes `Projections.Enabled` and `Snapshots.Enabled`. `AddDecider` returns a registration error at startup validation when `UseDeciders` is false, never a silent no-op.

**Class:** Settled
**Settled by:** AGENTS.md section 1 (pay-for-what-you-use) and section 3 (registration completeness, opt-in)

</details>

---

## Implementation Phases

### Phase 1: Core Abstractions

<details>
<summary><strong>Tasks</strong></summary>

1. `IDecider<TCommand, TEvent, TState>` (`where TCommand : ICommand`, `where TEvent : IDomainEvent`); home per D1: `src/Encina.DomainModeling/Deciders/IDecider.cs` (A) or `src/Encina.Marten/Deciders/IDecider.cs` (B). Members: `Either<EncinaError, IReadOnlyList<TEvent>> Decide(TCommand, TState)`, `TState Evolve(TState, TEvent)`, `TState InitialState { get; }`, `bool IsTerminal(TState) => false`.
2. `DeciderBase<TCommand, TEvent, TState>` (abstract): `Accept(params TEvent[])` and `Reject(string code, string message)` helpers returning the `Either`; no mutable state.
3. `DeciderResult<TEvent, TState>` record: `Events`, `State`, `Version` returned by the repository.
4. New error code `marten.decider_terminal_state` in `src/Encina.Marten/MartenErrorCodes.cs`.
5. `PublicAPI.Unshipped.txt` lines for every public symbol (written by `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina is pre-1.0, .NET 10, C# 14. Issue #320 adds the Decider pattern beside the class-based aggregates (IAggregate, MartenAggregateRepository).
TASK: Create IDecider, DeciderBase and DeciderResult in the package chosen for Dilemma D1 (recommended: Encina.DomainModeling). Add the marten.decider_terminal_state error code. Decide returns Either<EncinaError, IReadOnlyList<TEvent>>.
KEY RULES: no [Obsolete]; XML docs with an example on every public type; Decide and Evolve are pure (no clock, no IO); nullable enabled; PublicAPI.Unshipped.txt updated.
REFERENCE FILES: src/Encina.DomainModeling/IAggregate.cs, src/Encina.DomainModeling/IDomainEvent.cs, src/Encina/Abstractions/CqrsContracts.cs, src/Encina.Marten/MartenErrorCodes.cs
```

</details>

### Phase 2: Marten Decider Repository

<details>
<summary><strong>Tasks</strong></summary>

1. `IDeciderRepository<TCommand, TEvent, TState>` in `src/Encina.Marten/Deciders/`: `Task<Either<EncinaError, DeciderResult<TEvent, TState>>> ExecuteAsync(Guid streamId, TCommand command, CancellationToken ct)`.
2. `DeciderAggregateRepository<TCommand, TEvent, TState>` (sealed, scoped). Constructor: `IDocumentSession`, `IDecider<...>`, `IRequestContext`, `ILogger<...>`, `IOptions<EncinaMartenOptions>`, `IEnumerable<IEventMetadataEnricher>?`, `TimeProvider?`, `IInlineProjectionDispatcher?` (same shape as `SnapshotAwareAggregateRepository`).
3. `ExecuteAsync`: fetch the stream (`_session.Events.FetchStreamAsync`), fold with `Evolve` from `InitialState`, return `Left(IsTerminal)` when terminal, call `Decide`, return its `Left` unchanged, append with the expected version, `SaveChangesAsync`, relay to `InlineProjectionRelay`, return `DeciderResult`. Concurrency exceptions map to `MartenErrorCodes.ConcurrencyConflict`; other exceptions to `SaveFailed` through `EncinaErrors.FromException` and `ex.ForLogging()`.
4. A new stream (no events) starts from `InitialState`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Phase 1 created IDecider and DeciderResult. MartenAggregateRepository (src/Encina.Marten) is the pattern for load and append.
TASK: Implement IDeciderRepository and DeciderAggregateRepository as described in the plan; every failure is a Left with a MartenErrorCodes code; never swallow an error.
KEY RULES: async with CancellationToken everywhere; NEVER log EncinaError.Message or exception messages (use ex.ForLogging()); Time through TimeProvider; unit tests mock IDocumentSession with NSubstitute and assert the Left paths.
REFERENCE FILES: src/Encina.Marten/MartenAggregateRepository.cs, src/Encina.Marten/Snapshots/SnapshotAwareAggregateRepository.cs, src/Encina.Marten/Projections/InlineProjectionRelay.cs
```

</details>

### Phase 3: Options, DI and Registration Completeness

<details>
<summary><strong>Tasks</strong></summary>

1. `EncinaMartenOptions.UseDeciders` (default false) in `src/Encina.Marten/EncinaMartenOptions.cs`, with XML docs and an example.
2. `ServiceCollectionExtensions.AddDecider<TDecider, TCommand, TEvent, TState>()`: `TryAddSingleton<IDecider<...>, TDecider>()` and `TryAddScoped<IDeciderRepository<...>, DeciderAggregateRepository<...>>()`; `AddEncinaMarten` registers nothing decider-related unless `UseDeciders` is true.
3. DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes` (AGENTS.md section 3, registration completeness), in `tests/Encina.UnitTests/Marten`.
4. Guard tests for the new public methods in `tests/Encina.GuardTests/Marten/Core`; contract test for `IDecider` in `tests/Encina.ContractTests/Marten/Core`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: Phases 1 and 2 are merged. AddEncinaMarten (src/Encina.Marten/ServiceCollectionExtensions.cs) registers features through AddProjections/AddSnapshots with TryAdd*.
TASK: Add UseDeciders and AddDecider; prove registration with a ValidateOnBuild + ValidateScopes test; add guard and contract tests that instantiate real implementations.
KEY RULES: TryAdd* only, never override an application registration; tests execute real package code (no reflection-only tests); Shouldly through Encina.Testing.Shouldly.
REFERENCE FILES: src/Encina.Marten/ServiceCollectionExtensions.cs, tests/Encina.GuardTests/Marten/Core/ServiceCollectionExtensionsGuardTests.cs, tests/Encina.ContractTests/Marten/Core/IAggregateRepositoryContractTests.cs
```

</details>

### Phase 4: Observability and Test Helper

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Marten/Deciders/DeciderLog.cs` with `[LoggerMessage]` EventIds in the new `MartenDecider` range (7250-7259): load, decision accepted, decision rejected (error code only), terminal state, append, concurrency conflict. Register the range in `src/Encina/Diagnostics/EventIdRanges.cs`, add `MartenDecider` to the `["Encina.Marten"]` entry of `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (the entry takes a list of range names).
2. `DeciderActivitySource` (`encina.marten.decider.execute`, tags `command.type`, `outcome`) and `DeciderMetrics` (`encina.marten.decider.decisions` counter tagged by outcome).
3. `DeciderSpecification<TCommand, TEvent, TState>` in `src/Encina.Testing/EventSourcing/`: `Given(events)`, `When(command)`, `ThenEvents(...)`, `ThenError(code)`, in the style of `AggregateTestBase`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: Encina.Marten has no ActivitySource or Meter today; Encina.Tenancy (TenancyActivitySource, TenancyMetrics) shows the house pattern. EventIds are registered first (ADR-021).
TASK: Register MartenDecider = (7250, 7259) in EventIdRanges.cs, write DeciderLog with [LoggerMessage] packed from 7250, add the activity source, the meter and the Given/When/Then helper.
KEY RULES: pack EventIds sequentially; log error codes, never messages; update the AssemblyRanges map; run the architecture tests.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Marten/Snapshots/SnapshotLog.cs, src/Encina.Tenancy/Diagnostics/TenancyActivitySource.cs, src/Encina.Testing/EventSourcing/AggregateTestBase.cs
```

</details>

### Phase 5: Tests, Documentation and Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. Property tests (`tests/Encina.PropertyTests`): folding is deterministic and order preserving; replaying `Decide` output through `Evolve` never throws for valid commands.
2. Integration test (`tests/Encina.IntegrationTests/Infrastructure/Marten`, `[Trait("Category", "Integration")]`): load, decide, append round trip on PostgreSQL through Testcontainers; stale expected version yields `ConcurrencyConflict`; terminal state yields `marten.decider_terminal_state`.
3. Coverage-manifest entries for each new `src/` file in `.github/coverage-manifest/Encina.Marten.json` (and `Encina.DomainModeling.json` or `Encina.Testing.json` as applicable) with per-flag targets and one-sentence justifications (`mechanical-fixer`).
4. Load test: justification `.md` (pure logic, concurrency is Marten's). Benchmark: optional, decider against class-based repository.
5. `docs`: guide with the worked order example, package README, ADR for D1 and D2 (ADR number assigned when the ADR pull request opens), `docs/INVENTORY.md`, changelog fragment `changelog.d/320-decider-pattern.added.md` (`docs-writer`, `mechanical-fixer`).
6. Verification: `dotnet build --configuration Release` with zero warnings; every flag at its manifest target; CRAP of changed methods at most 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: The decider abstractions, repository, options and diagnostics exist. Coverage is measured per flag against .github/coverage-manifest/Encina.Marten.json.
TASK: Write the property and integration tests, record per-file targets with justifications in the manifest, and finish docs and the changelog fragment.
KEY RULES: tests instantiate real implementations; integration tests use the shared Marten PostgreSQL fixture, never a per-class container; Shouldly through Encina.Testing.Shouldly; no hand-typed coverage figures in docs (covref markers).
REFERENCE FILES: tests/Encina.IntegrationTests/Infrastructure/Marten, .github/coverage-manifest/Encina.Marten.json, docs/testing/coverage-measurement-methodology.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| Chassaing, "Functional Event Sourcing Decider" (decide / evolve / initialState / isTerminal) | The shape of `IDecider` |
| Wolverine Aggregate Handler Workflow | Comparable product that implements the same idea on Marten |
| ADR-001, ADR-006 | Railway Oriented Programming, errors as values |
| ADR-019, ADR-027 | Event sourcing on Marten; why class-based aggregates stay |
| AGENTS.md sections 3, 6, 7, 9 | Opt-in, `TimeProvider`, 12 functions, EventIds, per-flag tests |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IAggregateRepository<TAggregate>` / `MartenAggregateRepository` | `src/Encina.Marten` | Pattern to mirror for load via `FetchStreamAsync` and append with expected version |
| `InlineProjectionRelay` | `src/Encina.Marten/Projections` | Relays saved events to inline projections |
| `MartenErrorCodes` | `src/Encina.Marten/MartenErrorCodes.cs` | `ConcurrencyConflict`, `LoadFailed`, `SaveFailed` reused; new decider code added here |
| `EncinaMartenOptions` | `src/Encina.Marten/EncinaMartenOptions.cs` | Gets `UseDeciders` |
| `AddEncinaMarten` | `src/Encina.Marten/ServiceCollectionExtensions.cs` | Registers the decider repository when `UseDeciders` is true |
| `IDomainEvent` | `src/Encina.DomainModeling/IDomainEvent.cs` | `TEvent` constraint |
| `ICommand` | `src/Encina/Abstractions/CqrsContracts.cs` | `TCommand` constraint |
| `AggregateTestBase` | `src/Encina.Testing/EventSourcing/AggregateTestBase.cs` | Style reference for the decider test helper |
| `MartenHealthCheck` | `src/Encina.Marten/Health/MartenHealthCheck.cs` | Connectivity already covered |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Marten` (new range `MartenDecider`) | 7250-7259 | Block 7250-7299 assigned to this family; this plan owns 7250-7259, siblings #323, #324, #326 and #535 own 7260-7269, 7270-7279, 7280-7289 and 7290-7299 |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Abstractions | 3 | `IDecider`, `DeciderBase`, `DeciderResult` |
| Marten repository and DI | 4 | Interface, repository, options edit, DI edit |
| Diagnostics | 3 | Activity source, metrics, log messages |
| Testing helper | 1 | `DeciderSpecification` |
| Unit and guard tests | 6 | Decide/Evolve, repository, DI, guards |
| Contract tests | 1 | `IDecider` |
| Property tests | 1 | Fold invariants |
| Integration tests | 1 | Marten on PostgreSQL |
| Docs, changelog, PublicAPI, manifest | 6 | Guide, ADR, fragment, PublicAPI, manifest, README |
| **Total** | **26** | Two PRs |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14, nullable enabled. Issue #320 adds the Decider pattern to the event-sourcing support in Encina.Marten, beside the class-based aggregate repositories. Follow AGENTS.md: ROP with Either<EncinaError, T>, opt-in features, TimeProvider, no EncinaError.Message in logs, per-flag coverage obligations.

IMPLEMENTATION OVERVIEW: Phase 1 abstractions (IDecider, DeciderBase, DeciderResult, error code). Phase 2 Marten DeciderAggregateRepository mirroring MartenAggregateRepository. Phase 3 UseDeciders and AddDecider with a ValidateOnBuild DI test. Phase 4 EventId range 7250-7259, DeciderLog, activity source, metrics, Given/When/Then helper. Phase 5 property and integration tests, manifest targets, docs, ADR, changelog fragment. Ship as two PRs (Phases 1-3, then 4-5). Dilemmas D1 (home), D2 (coexistence) and D3 (handler or behavior) follow the maintainer's answers in the plan's Maintainer Decisions section once it exists.

KEY PATTERNS: Left for every failure; TryAdd* registration; sequential EventIds registered first; shared integration fixtures; tests that execute real code; Shouldly through Encina.Testing.Shouldly.

REFERENCE FILES: src/Encina.Marten/MartenAggregateRepository.cs, src/Encina.Marten/ServiceCollectionExtensions.cs, src/Encina.Marten/EncinaMartenOptions.cs, src/Encina.DomainModeling/IAggregate.cs, src/Encina.Testing/EventSourcing/AggregateTestBase.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/plans/dsr-implementation-plan-404.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Aggregate state caching is a separate optimization; snapshots are #323. Issue to open at implementation time if wanted |
| 2 | OpenTelemetry | ✅ | `encina.marten.decider.execute` span and `encina.marten.decider.decisions` counter (Phase 4) |
| 3 | Structured Logging | ✅ | `DeciderLog` with EventIds 7250-7259, error codes only (Phase 4) |
| 4 | Health Checks | ❌ | Event store connectivity is already `MartenHealthCheck`; the decider adds no dependency |
| 5 | Validation | ❌ | Command validation runs before the handler through the validation pipeline; `Decide` validates state transitions only |
| 6 | Resilience | ⏭️ | Retry of a concurrency conflict is the caller's policy; a Polly integration would be a separate issue |
| 7 | Distributed Locks | ❌ | Optimistic concurrency on the stream version covers concurrent access (Phase 2) |
| 8 | Transactions | ✅ | One Marten session and one `SaveChangesAsync` append the events atomically (Phase 2) |
| 9 | Idempotency | ⏭️ | Duplicate command handling belongs to the inbox/idempotency behavior; no new issue needed unless requested |
| 10 | Multi-Tenancy | ⏭️ | Tenant-scoped streams arrive with #326; the decider repository uses the session it is given |
| 11 | Module Isolation | ⏭️ | Module-scoped streams are not designed in Marten today; open an issue when a module needs them |
| 12 | Audit Trail | ✅ | The appended events are the record; metadata enrichment stays active through the shared session |

---

## Prerequisites & Dependencies

- No blocking issue. #326 (tenancy) and #323 (snapshots) are independent; the decider repository reads whatever session the DI container supplies.
- Related, not blocking: #315 and #330 (alternative event-sourcing patterns).

---

## Next Steps

1. The maintainer answers D1, D2 and D3 when work on #320 starts; the answers go into `## Maintainer Decisions` and the choices are updated.
2. Re-check the Design Choices against the code, then run `adversarial-reviewer` on the Settled and Trivial choices.
3. Write the PR 1 brief (Phases 1 to 3) and, after it merges, the PR 2 brief (Phases 4 and 5).
