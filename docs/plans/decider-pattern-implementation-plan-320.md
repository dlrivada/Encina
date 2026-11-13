# Implementation Plan: Decider Pattern Support for Functional Event Sourcing (`Encina.Marten`)

> **Issue**: [#320](https://github.com/dlrivada/Encina/issues/320)
> **Type**: Feature
> **Complexity**: Medium (5 phases, 4 PRs, one provider)
> **Estimated Scope**: 36 files and about 3,000 changed lines over 4 PRs (the table in "Estimated File Count" counts every added and edited file; the figures assume the recommended answers to the Dilemmas)

---

## Summary

Event-sourced aggregates in Encina are class based today: `IAggregateRepository<TAggregate>` (`src/Encina.Marten/IAggregateRepository.cs`), `MartenAggregateRepository<TAggregate>` and `SnapshotAwareAggregateRepository<TAggregate>` over `IAggregate` (`src/Encina.DomainModeling/IAggregate.cs`), with `AggregateTestBase` in `src/Encina.Testing/EventSourcing`. This plan adds the **Decider pattern** next to them: business logic as two pure functions, `Decide(command, state)` and `Evolve(state, event)`, so decisions are tested as data in, data out, without mocks.

**What it implements**

- `IDecider<TCommand, TEvent, TState>` with `Decide` returning `Either<EncinaError, IReadOnlyList<TEvent>>` (an invalid command is a `Left`, never an empty success), `Evolve`, `InitialState` and `IsTerminal`.
- `DeciderBase<TCommand, TEvent, TState>` (helpers for `Left`/`Right` construction and a default `IsTerminal`).
- A Marten repository, `DeciderAggregateRepository<TCommand, TEvent, TState>`, that loads the stream, folds it through `Evolve`, calls `Decide` and appends the result with optimistic concurrency; it returns the new events and the new state.
- `AddDecider<TDecider, TCommand, TEvent, TState>()` registration, opt-in because nothing is registered until it is called (decision 6; the `EncinaMartenOptions.UseDeciders` flag of the original proposal is option A of that decision).
- A Given/When/Then test helper in `Encina.Testing.EventSourcing` for deciders.

**Standards**: AGENTS.md section 3 (ROP, opt-in, `TimeProvider`, errors never swallowed), ADR-001, ADR-006, ADR-018, ADR-019, ADR-021, ADR-027.

**Affected packages**: `Encina.Marten` (code and manifest; it has no `PublicAPI.Unshipped.txt` on main, so no PublicAPI lines are written for it), `Encina.DomainModeling` if Dilemma D1 chooses option A (code, manifest and its existing `PublicAPI.Unshipped.txt`), `Encina.Testing` (namespace `Encina.Testing.EventSourcing`; no PublicAPI file either, manifest only). No `Encina.EventStoreDB` package exists (future work).

**Provider category**: event sourcing (Marten primary, AGENTS.md section 5). The 10-database rule does not apply; integration tests run against Marten on PostgreSQL through Testcontainers.

**Size and split (AGENTS.md section 11)**: 36 files exceed the 15-file limit, so the work ships as four worker PRs of at most 15 files and 1,500 lines each, counted with every edited file, test, manifest entry, PublicAPI line, justification and changelog fragment (the table in "Estimated File Count" adds up per PR). No PR uses a type, log class or project created by a later PR, and each coverage target is entered in the same PR as the tests that meet it.

| PR | Content | Files | Lines (estimate) |
|----|---------|-------|------------------|
| 1 | Phase 1: `IDecider`, `DeciderBase`, `DeciderResult` with unit, guard and contract tests, manifest and PublicAPI | 8 | about 600 |
| 2 | Phase 2 and the diagnostics of Phase 4: repository, error code, `DeciderLog` with the EventIds, activity source, metrics, with unit and guard tests and the manifest | 12 | about 1,100 |
| 3 | Phase 3 and the tests of Phase 5: `AddDecider`, `DeciderSpecification`, DI, guard, property and integration tests, load justification, manifests | 11 | about 1,000 |
| 4 | Documentation: guide, ADR, README, INVENTORY, changelog fragment | 5 | about 400 |

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

The issue lists a "pipeline behavior for decider-based commands" under its Provider Implementation tasks; if D3 = A is accepted that task changes to an explicit handler call, and the issue text is updated when the maintainer answers. Option A keeps the Encina request model intact; the only behavior needed is the existing `EventPublishingPipelineBehavior` (`src/Encina.Marten/EventPublishingPipelineBehavior.cs`) that publishes committed events. B wins for a uniform command set where per-command handlers are pure boilerplate; it can be added later on top of A without changing it.

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

AGENTS.md section 3 requires `Either<EncinaError, T>` and forbids swallowed errors, so the signature of `Decide` is settled; the other behaviours that used to be bundled here are separate entries (4a to 4e) because no rule fixes them. A concurrency conflict reuses `MartenErrorCodes.ConcurrencyConflict` and load or save failures reuse `LoadFailed` and `SaveFailed`, as `MartenAggregateRepository` does.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (Railway Oriented Programming), ADR-001, ADR-006, ADR-053

</details>

<details>
<summary><strong>4a. Behaviour on a terminal state</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `Left` with the new code `marten.decider_terminal_state`, category `Conflict` | The caller sees that the stream no longer accepts commands; ADR 062 rule R2 gives an invalid transition the category `Conflict` (not `FailedPrecondition`, which rule R2 reserves until #3420) | One more error code |
| B. `Right` with an empty event list | No new code | A command on a finished stream looks like a success |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

ADR 062 (merged, PR #3599) fixes the category of an invalid transition; the new code uses the category overload `EncinaErrors.Create(code, ErrorCategory.Conflict, message)`. The name follows the `marten.<snake_case>` convention of `src/Encina.Marten/MartenErrorCodes.cs`.

**Class:** Dilemma

</details>

<details>
<summary><strong>4b. When <code>IsTerminal</code> is checked</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. After the fold of the stored events and before `Decide` | The decider is never asked to decide on a finished stream | The events returned by `Decide` can still end the stream; the next command is the one that is rejected |
| B. Also after folding the new events | Reports the end of the stream in the same call | Two checks and two meanings of the same `Left` |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

The Chassaing decider uses `isTerminal` to refuse further commands; A is the smaller contract and the one the Given/When/Then helper can state. B only adds information to the result and can be added later without changing A.

**Class:** Dilemma

</details>

<details>
<summary><strong>4c. Timestamps inside events</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. The calling handler puts the timestamp in the command, taken from `TimeProvider` | `Decide` stays a pure function; tests need no clock | The handler must remember to set it |
| B. The decider takes a `TimeProvider` through its constructor | Timestamp is not the handler's job | The decider is no longer pure; a test needs a fake clock |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

AGENTS.md section 3 requires time to come from `TimeProvider` and says nothing about where the call happens, so both options comply: the choice is the purity of `Decide`, which is the point of the pattern.

**Class:** Dilemma

</details>

<details>
<summary><strong>4d. An empty event list from <code>Decide</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `Right(empty)` is a valid no-op: nothing is appended and `DeciderResult` has no events and the unchanged version | An idempotent command needs no special code | A decider that forgets to emit looks like a no-op |
| B. An empty list is rejected with a new error code | Forces an explicit decision | The idempotent "already done" case must be a `Left`, which is wrong |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

Accepting a no-op keeps the repository free of business rules; a decider that must reject returns a `Left` through `Reject`. When the list is empty the repository does not call `SaveChangesOrEjectAsync` and does not relay to inline projections.

**Class:** Dilemma

</details>

<details>
<summary><strong>4e. Names of the new error code</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `marten.decider_terminal_state` in `MartenErrorCodes` | Same `marten.<snake_case>` shape as `marten.aggregate_not_found`, `marten.concurrency_conflict`; names the cause | One more constant |
| B. A code outside `MartenErrorCodes` | Keeps Marten constants untouched | Breaks the one-file home of the package's codes |

### Chosen Option: **A**

### Rationale

The shape is the convention of the existing constants in `src/Encina.Marten/MartenErrorCodes.cs`.

**Class:** Trivial
**Settled by:** `src/Encina.Marten/MartenErrorCodes.cs` (existing codes)

</details>

<details>
<summary><strong>5. Loading and appending in the Marten repository</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Mirror `MartenAggregateRepository`: `Events.FetchStreamAsync`, fold with `Evolve`, `Events.Append(id, expectedVersion, events)` where the expected version is the stream version after the append (stream length plus the new events, as the existing repository computes it), `SaveChangesOrEjectAsync`, then `InlineProjectionRelay`; an empty stream starts with `Events.StartStream` and a collision maps to `MartenErrorCodes.StreamAlreadyExists` | Same concurrency, metadata enrichment and projection behavior as today; proven code path | Duplicates a little of the load/append sequence |
| B. Marten's `FetchForWriting` / aggregate stream API | Less code | Different concurrency and metadata path from the class-based repository; two behaviors to reason about |

### Chosen Option: **A — mirror the existing repository**

### Rationale

Following the established pattern keeps optimistic concurrency, `EventMetadataEnrichmentService` and inline projections identical for both models (`src/Encina.Marten/MartenAggregateRepository.cs`: `LoadAsync`, `SaveAsync`, `CreateAsync`). `ThrowOnConcurrencyConflict` is honoured as in `SaveAsync`.

**Class:** Trivial
**Settled by:** AGENTS.md section 3 (provider coherence) and the existing `MartenAggregateRepository` implementation

</details>

<details>
<summary><strong>6. Opt-in switch</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `EncinaMartenOptions.UseDeciders` (default false) gates the infrastructure; `AddDecider<...>()` registers one decider and a startup validation fails when a decider is registered and the flag is false | One visible switch like `Projections.Enabled` | Two steps for the user; the options are configured after `AddDecider` runs, so the check needs an `IValidateOptions<EncinaMartenOptions>` with `ValidateOnStart`, one more file and tests; the flag name is a new public option |
| B. Only `AddDecider<...>()`, no flag | The decider is registered only when the user calls it, which already is opt-in; no flag, no validator, 2 files fewer | Differs from the `Projections.Enabled` style of the options class |

### Chosen Option: **B — `AddDecider` only** (recommended, pending the maintainer)

### Rationale

Both options satisfy opt-in (AGENTS.md section 1, pay-for-what-you-use): under B nothing decider-related is registered until the user calls `AddDecider`, so the flag adds surface without adding safety. The error semantics of A (how a registration without the flag fails) and the property name `UseDeciders` are decisions only the maintainer takes if A is chosen. The file counts assume B; A adds `EncinaMartenOptions.cs`, one validator and one validator test to PR 3 (14 files, still within the limit).

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Stream key, new streams and the content of the result</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `Guid` stream id (as `IAggregateRepository.LoadAsync(Guid id, ...)`); a stream with no events is started with `StartStream`; `DeciderResult` returns the new events, the state after folding them and the stream version after the append | Same key shape as the class-based path; the caller gets the new state without a second load | A string-keyed stream cannot be used |
| B. Generic stream key (`Guid` or `string`) | Covers string-keyed streams | Marten stream identity is configured per store; the class-based path would not match |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

Marten supports one stream identity per store configuration, and `src/Encina.Marten/IAggregateRepository.cs` uses `Guid`; changing the key type is a store-wide decision outside this feature.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Exceptions thrown by <code>Decide</code> or <code>Evolve</code>, and events <code>Evolve</code> cannot fold</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. The repository catches an exception from `Evolve` while loading (`LoadFailed`) or from `Decide` or the append (`SaveFailed`) and returns a `Left` with the exception logged through `ex.ForLogging()`; an `Evolve` that meets an unknown event returns the state unchanged (documented in `IDecider`) | Mirrors the catch-all of `MartenAggregateRepository`; a decider defect never escapes as an exception | A defect looks like an infrastructure failure code |
| B. Exceptions propagate | A defect is loud | The caller of `ExecuteAsync` gets exceptions where every other path is a `Left` |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

AGENTS.md section 3 says errors are never swallowed: the `Left` carries the code and the exception type reaches the log through `ForLogging()`, so nothing is hidden; whether a defect should be an exception instead is a choice of style.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Decider lifetime, snapshots and event publishing</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. The decider is a singleton and must be stateless and thread-safe (documented in `IDecider`); the repository is scoped; the stream is always folded from the first event (no snapshots in this feature); events reach subscribers through the existing `EventPublishingPipelineBehavior`, the repository does not publish | Pure functions make a singleton safe; one path to publish | A long stream is folded in full on every command until a snapshot integration issue exists; the pipeline behavior needs the same session |
| B. Scoped decider, snapshot-aware fold and repository-side publishing | A decider may use scoped services; short folds | Impure deciders; snapshots (#323) and publishing change the repository and are a different feature |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

The pattern defines a decider as pure functions, so a singleton is safe, and `SnapshotAwareAggregateRepository` is specific to `IAggregate`. Snapshot support for deciders is a follow-up (see the cross-cutting matrix row 1); the order of the publish and the append is the existing pipeline behavior's.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Telemetry tags and retry of a concurrency conflict</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. The span tags are `command.type` (the type name) and `outcome` (`accepted`, `rejected`, `terminal`, `conflict`, `failed`: a closed set, not the error code string); neither the stream id nor command contents are tagged; the repository does not retry a conflict | Bounded cardinality; no personal data or ids in telemetry; the caller keeps its own retry policy | Less detail per rejection (the code is in the log) |
| B. Tag `outcome` with the error code and the stream id | More detail | Unbounded cardinality for the id; a personal-data risk when the id is a person's id |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

AGENTS.md section 3 limits what reaches logs and tags to error codes and exception types, and the stream id of a person's aggregate can identify that person (compliance modules are event sourced on Marten, ADR-019); the closed `outcome` set keeps the metric series bounded. Option B also keeps the message out of the tags, so the rule does not settle the choice.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Core Abstractions (PR 1)

<details>
<summary><strong>Tasks</strong></summary>

1. `IDecider<TCommand, TEvent, TState>` (`where TCommand : ICommand`, `where TEvent : IDomainEvent`); home per D1: `src/Encina.DomainModeling/Deciders/IDecider.cs` (A) or `src/Encina.Marten/Deciders/IDecider.cs` (B). Members: `Either<EncinaError, IReadOnlyList<TEvent>> Decide(TCommand, TState)`, `TState Evolve(TState, TEvent)`, `TState InitialState { get; }`, `bool IsTerminal(TState) => false`. XML docs state that an implementation is stateless and thread-safe (decision 9) and that `Evolve` returns the state unchanged for an event it does not know (decision 8).
2. `DeciderBase<TCommand, TEvent, TState>` (abstract): `Accept(params TEvent[])` and `Reject(string code, string message)` helpers returning the `Either`; no mutable state. `Reject` takes a category as well (`Reject(string code, ErrorCategory category, string message)` calling the category overload of `EncinaErrors.Create`, ADR 062).
3. `DeciderResult<TEvent, TState>` record: `Events`, `State`, `Version` returned by the repository (same package as `IDecider`).
4. Tests in the same PR: unit (`DeciderBase` Accept/Reject, terminal default), guard (`DeciderBase.Accept` and `Reject` null and whitespace arguments, in `tests/Encina.GuardTests`), contract (`IDecider` implemented by a real test decider).
5. Manifest entries for the three new files, per flag, with a one-sentence justification each, in the same PR as these tests (`mechanical-fixer`): `Encina.DomainModeling.json` under D1 = A, `Encina.Marten.json` under D1 = B.
6. `PublicAPI.Unshipped.txt` lines for the three public types: only under D1 = A, in the existing `src/Encina.DomainModeling/PublicAPI.Unshipped.txt` (`Encina.Marten` has no PublicAPI file on main, so under D1 = B there is nothing to write).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina is pre-1.0, .NET 10, C# 14. Issue #320 adds the Decider pattern beside the class-based aggregates (IAggregate, MartenAggregateRepository).
TASK: Create IDecider, DeciderBase and DeciderResult in the package chosen for Dilemma D1 (recommended: Encina.DomainModeling), with their unit, guard and contract tests. Decide returns Either<EncinaError, IReadOnlyList<TEvent>>.
KEY RULES: no [Obsolete]; XML docs with an example on every public type; Decide and Evolve are pure (no clock, no IO); nullable enabled; PublicAPI.Unshipped.txt only where the package has the file (DomainModeling); guard tests for every public method with validated parameters.
REFERENCE FILES: src/Encina.DomainModeling/IAggregate.cs, src/Encina.DomainModeling/IDomainEvent.cs, src/Encina/Abstractions/CqrsContracts.cs, src/Encina.Marten/MartenErrorCodes.cs
```

</details>

### Phase 2: Marten Decider Repository and Diagnostics (PR 2)

<details>
<summary><strong>Tasks</strong></summary>

1. `IDeciderRepository<TCommand, TEvent, TState>` in `src/Encina.Marten/Deciders/`: `Task<Either<EncinaError, DeciderResult<TEvent, TState>>> ExecuteAsync(Guid streamId, TCommand command, CancellationToken ct)`.
2. `DeciderAggregateRepository<TCommand, TEvent, TState>` (sealed, scoped). Constructor: `IDocumentSession`, `IDecider<...>`, `IRequestContext`, `ILogger<...>`, `IOptions<EncinaMartenOptions>`, `IEnumerable<IEventMetadataEnricher>?`, `TimeProvider?` (used for the duration metric), `IInlineProjectionDispatcher?` (same shape as `SnapshotAwareAggregateRepository`).
3. `ExecuteAsync`: fetch the stream (`_session.Events.FetchStreamAsync`), fold with `Evolve` from `InitialState`, return `Left(marten.decider_terminal_state)` when terminal (decisions 4a, 4b), call `Decide`, return its `Left` unchanged, append (decision 5: expected version after the append, `StartStream` for an empty stream, `SaveChangesOrEjectAsync`), relay to `InlineProjectionRelay`, return `DeciderResult` (decision 7); an empty `Right` appends nothing (4d). Concurrency exceptions map to `MartenErrorCodes.ConcurrencyConflict` (or rethrow when `ThrowOnConcurrencyConflict` is set, as `SaveAsync` does); a stream collision to `StreamAlreadyExists`; other exceptions to `LoadFailed` or `SaveFailed` through `EncinaErrors.FromException` and `ex.ForLogging()` (decision 8).
4. The error code `marten.decider_terminal_state` in `src/Encina.Marten/MartenErrorCodes.cs`, created with `EncinaErrors.Create(code, ErrorCategory.Conflict, message)` (ADR 062, merged in PR #3599).
5. Diagnostics, created here because this is the first PR that logs: `src/Encina.Marten/Deciders/DeciderLog.cs` with `[LoggerMessage]` EventIds in the new `MartenDecider` range, `DeciderActivitySource` (`encina.marten.decider.execute`, tags `command.type`, `outcome`, decision 10) and `DeciderMetrics` (`encina.marten.decider.decisions` counter tagged by `outcome`). The EventId range is registered in `src/Encina/Diagnostics/EventIdRanges.cs` and `MartenDecider` is added to the `["Encina.Marten"]` entry of `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (the entry takes a list of range names).
6. Tests in the same PR: unit (`DeciderAggregateRepositoryTests` for every `Left` path, the no-op path and the constructor; `DeciderDiagnosticsTests` for the log, activity source and meter), guard (`DeciderAggregateRepositoryGuardTests`: constructor arguments, `ExecuteAsync` with a null command).
7. Manifest entries for each new `src/` file in `.github/coverage-manifest/Encina.Marten.json`, per flag with a one-sentence justification, in this PR (`mechanical-fixer`). No PublicAPI lines: `Encina.Marten` has no `PublicAPI.Unshipped.txt` on main.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Phase 1 created IDecider and DeciderResult. MartenAggregateRepository (src/Encina.Marten) is the pattern for load and append.
TASK: Implement IDeciderRepository and DeciderAggregateRepository as described in the plan, with the error code, DeciderLog, activity source and meter; register the EventId range MartenDecider first. Every failure is a Left with a MartenErrorCodes code; never swallow an error.
KEY RULES: async with CancellationToken everywhere; NEVER log EncinaError.Message or exception messages (use ex.ForLogging()); Time through TimeProvider; the new error code uses the ErrorCategory overload of EncinaErrors.Create (ADR 062); EventIds 7250-7259 packed sequentially from 7250; update the AssemblyRanges map; unit tests mock IDocumentSession with NSubstitute and assert the Left paths; guard tests for the constructor and ExecuteAsync.
REFERENCE FILES: src/Encina.Marten/MartenAggregateRepository.cs, src/Encina.Marten/Snapshots/SnapshotAwareAggregateRepository.cs, src/Encina.Marten/Snapshots/SnapshotLog.cs, src/Encina.Marten/Projections/InlineProjectionRelay.cs, src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Tenancy/Diagnostics/TenancyActivitySource.cs
```

</details>

### Phase 3: DI and Registration Completeness (PR 3)

<details>
<summary><strong>Tasks</strong></summary>

1. `ServiceCollectionExtensions.AddDecider<TDecider, TCommand, TEvent, TState>()` in `src/Encina.Marten/ServiceCollectionExtensions.cs`: `TryAddSingleton<IDecider<...>, TDecider>()` and `TryAddScoped<IDeciderRepository<...>, DeciderAggregateRepository<...>>()`; `AddEncinaMarten` registers nothing decider-related (opt-in by calling `AddDecider`, decision 6 option B). Under decision 6 option A the PR also adds `EncinaMartenOptions.UseDeciders` (default false) in `src/Encina.Marten/EncinaMartenOptions.cs` and the `IValidateOptions` check.
2. DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes` (AGENTS.md section 3, registration completeness), in `tests/Encina.UnitTests/Marten`; it also proves an application's own `IDecider` or repository registration is not overridden (`TryAdd*`).
3. Guard tests: `AddDecider` null services (edit of `tests/Encina.GuardTests/Marten/Core/ServiceCollectionExtensionsGuardTests.cs`) and `DeciderSpecification` public constructor and methods (new `DeciderSpecificationGuardTests.cs`), both under `tests/Encina.GuardTests`.
4. The contract test for `IDecider` already ships in PR 1.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: Phases 1 and 2 are merged. AddEncinaMarten (src/Encina.Marten/ServiceCollectionExtensions.cs) registers features through AddProjections/AddSnapshots with TryAdd*.
TASK: Add AddDecider (and UseDeciders only if the maintainer chose option A of decision 6); prove registration with a ValidateOnBuild + ValidateScopes test; add guard tests that instantiate real implementations.
KEY RULES: TryAdd* only, never override an application registration; tests execute real package code (no reflection-only tests); Shouldly through Encina.Testing.Shouldly.
REFERENCE FILES: src/Encina.Marten/ServiceCollectionExtensions.cs, tests/Encina.GuardTests/Marten/Core/ServiceCollectionExtensionsGuardTests.cs, tests/Encina.ContractTests/Marten/Core/IAggregateRepositoryContractTests.cs
```

</details>

### Phase 4: Event IDs and Test Helper (PR 2 and PR 3)

<details>
<summary><strong>Tasks</strong></summary>

1. In PR 2 (the first PR that logs): `src/Encina.Marten/Deciders/DeciderLog.cs` with `[LoggerMessage]` EventIds packed sequentially from 7250 in the new `MartenDecider` range (7250-7259, inside the block assigned to this family in the batch registry `artifacts/decisions/plan-batch/eventid-blocks.md`; ids 7250 to 7257 are used: load, decision accepted, decision rejected (error code only), terminal state, append, concurrency conflict, load failed, save failed; 7258 and 7259 stay free). Register the range in `src/Encina/Diagnostics/EventIdRanges.cs` and add `MartenDecider` to `AssemblyRanges` as in Phase 2 task 5. The activity source and the meter are Phase 2 task 5.
2. In PR 3: `DeciderSpecification<TCommand, TEvent, TState>` in `src/Encina.Testing/EventSourcing/`: `Given(events)`, `When(command)`, `ThenEvents(...)`, `ThenError(code)`, in the style of `AggregateTestBase`, with its unit and guard tests and its entry in `.github/coverage-manifest/Encina.Testing.json` (per flag, one-sentence justification). `Encina.Testing` has no PublicAPI file on main.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: Encina.Marten has no ActivitySource or Meter today; Encina.Tenancy (TenancyActivitySource, TenancyMetrics) shows the house pattern. EventIds are registered first (ADR-021).
TASK: In PR 2 register MartenDecider = (7250, 7259) in EventIdRanges.cs and write DeciderLog with [LoggerMessage] packed from 7250 (8 ids); in PR 3 add the Given/When/Then helper.
KEY RULES: pack EventIds sequentially; log error codes, never messages; update the AssemblyRanges map; run the architecture tests.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Marten/Snapshots/SnapshotLog.cs, src/Encina.Tenancy/Diagnostics/TenancyActivitySource.cs, src/Encina.Testing/EventSourcing/AggregateTestBase.cs
```

</details>

### Phase 5: Tests, Documentation and Finalization (PR 3 and PR 4)

<details>
<summary><strong>Tasks</strong></summary>

1. PR 3, property tests (`tests/Encina.PropertyTests/Marten`): folding is deterministic and order preserving; replaying `Decide` output through `Evolve` never throws for valid commands.
2. PR 3, integration test (`tests/Encina.IntegrationTests/Infrastructure/Marten`, `[Trait("Category", "Integration")]`, shared Marten PostgreSQL fixture): load, decide, append round trip on PostgreSQL through Testcontainers; stale expected version yields `ConcurrencyConflict`; terminal state yields `marten.decider_terminal_state`.
3. PR 3, per-file manifest targets for the property and integration tests and for `AddDecider` and `DeciderSpecification` in `.github/coverage-manifest/Encina.Marten.json` and `Encina.Testing.json`, each with a one-sentence justification (`mechanical-fixer`); the unit, guard and contract targets of the earlier files were entered in PRs 1 and 2 with their tests.
4. PR 3, load test: justification `.md` in the load-test project (pure logic, concurrency is Marten's; a justification is allowed for load tests, never for unit, guard or contract tests). Benchmark: optional, decider against class-based repository.
5. PR 4, `docs`: guide with the worked order example, README of the package that hosts `IDecider` (`src/Encina.DomainModeling/README.md` exists; `src/Encina.Marten` has none), ADR for D1 and D2 (ADR number assigned when the ADR pull request opens), `docs/INVENTORY.md`, changelog fragment `changelog.d/320-decider-pattern.added.md` (`docs-writer`, `mechanical-fixer`).
6. Verification in every PR: `dotnet build --configuration Release` with zero warnings; every flag at its manifest target; CRAP of changed methods at most 10.

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
| `EncinaMartenOptions` | `src/Encina.Marten/EncinaMartenOptions.cs` | `ThrowOnConcurrencyConflict` and `Metadata` are honoured; gets `UseDeciders` only under decision 6 option A |
| `ServiceCollectionExtensions` | `src/Encina.Marten/ServiceCollectionExtensions.cs` | Gets `AddDecider`; `AddEncinaMarten` registers nothing decider-related |
| `IDomainEvent` | `src/Encina.DomainModeling/IDomainEvent.cs` | `TEvent` constraint |
| `ICommand` | `src/Encina/Abstractions/CqrsContracts.cs` | `TCommand` constraint |
| `AggregateTestBase` | `src/Encina.Testing/EventSourcing/AggregateTestBase.cs` | Style reference for the decider test helper |
| `MartenHealthCheck` | `src/Encina.Marten/Health/MartenHealthCheck.cs` | Connectivity already covered |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Marten` (new range `MartenDecider`) | 7250-7259 (ids 7250-7257 used) | Inside the block 7250-7299 assigned to the family in the batch registry (`artifacts/decisions/plan-batch/eventid-blocks.md`); sub-ranges of the sibling plans are recorded there, not here |

### Estimated File Count

Every added or edited file is counted: tests, manifests, PublicAPI, error-code and EventId registry edits, justification and changelog. Existing callers: none, because no existing signature changes (every change adds a type or a method).

| PR | Category | Files | Notes |
|----|----------|-------|-------|
| 1 | Abstractions | 3 | `IDecider`, `DeciderBase`, `DeciderResult` |
| 1 | Tests | 3 | Unit (`DeciderBase`), guard (`DeciderBase`), contract (`IDecider`) |
| 1 | Manifest, PublicAPI | 2 | `Encina.DomainModeling.json` (D1 = A), existing `PublicAPI.Unshipped.txt` of `Encina.DomainModeling` |
| 1 | **Subtotal** | **8** | 7 under D1 = B (no PublicAPI file; manifest `Encina.Marten.json`) |
| 2 | Repository and diagnostics | 5 | `IDeciderRepository`, `DeciderAggregateRepository`, `DeciderLog`, `DeciderActivitySource`, `DeciderMetrics` |
| 2 | Edits | 3 | `MartenErrorCodes.cs`, `EventIdRanges.cs`, `EncinaEventIdAllocationTests.cs` |
| 2 | Tests | 3 | Repository unit, diagnostics unit, repository guard |
| 2 | Manifest | 1 | `Encina.Marten.json` |
| 2 | **Subtotal** | **12** | |
| 3 | Source | 2 | `ServiceCollectionExtensions.cs` edit, `DeciderSpecification` |
| 3 | Tests | 6 | DI unit, `DeciderSpecification` unit, AddDecider guard (edit), `DeciderSpecification` guard, property, integration |
| 3 | Justification | 1 | Load-test `.md` |
| 3 | Manifest | 2 | `Encina.Marten.json`, `Encina.Testing.json` |
| 3 | **Subtotal** | **11** | 14 under decision 6 option A |
| 4 | Docs | 4 | Guide, ADR, README (D1 = A), `docs/INVENTORY.md` |
| 4 | Changelog | 1 | `changelog.d/320-decider-pattern.added.md` |
| 4 | **Subtotal** | **5** | |
| | **Total** | **36** | 8 + 12 + 11 + 5; each PR at most 15 files and 1,500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14, nullable enabled. Issue #320 adds the Decider pattern to the event-sourcing support in Encina.Marten, beside the class-based aggregate repositories. Follow AGENTS.md: ROP with Either<EncinaError, T>, opt-in features, TimeProvider, no EncinaError.Message in logs, per-flag coverage obligations.

IMPLEMENTATION OVERVIEW: Phase 1 abstractions (IDecider, DeciderBase, DeciderResult) with tests and manifest. Phase 2 Marten DeciderAggregateRepository mirroring MartenAggregateRepository, with the error code (ErrorCategory overload, ADR 062), the EventId range 7250-7259, DeciderLog, activity source and metrics. Phase 3 AddDecider with a ValidateOnBuild DI test, the Given/When/Then helper, property and integration tests. Phase 5 docs, ADR, changelog fragment. Ship as four PRs (8, 12, 11 and 5 files). The Dilemmas (D1 home, D2 coexistence, D3 handler or behavior, and the numbered decisions marked Dilemma in Design Choices) follow the maintainer's answers in the plan's Maintainer Decisions section.

KEY PATTERNS: Left for every failure; TryAdd* registration; sequential EventIds registered first; shared integration fixtures; tests that execute real code; Shouldly through Encina.Testing.Shouldly.

REFERENCE FILES: src/Encina.Marten/MartenAggregateRepository.cs, src/Encina.Marten/ServiceCollectionExtensions.cs, src/Encina.Marten/EncinaMartenOptions.cs, src/Encina.DomainModeling/IAggregate.cs, src/Encina.Testing/EventSourcing/AggregateTestBase.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/plans/dsr-implementation-plan-404.md
```

</details>

---

## Cross-Cutting Integration Matrix

Legend: ✅ Integrate (implemented in this plan); ⏭️ Defer (a follow-up issue is to be opened); ❌ Not applicable (the one-sentence reason is in Notes).

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Aggregate state caching and snapshot-aware folding for deciders (see decision 9): follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/decider-snapshots-caching.md); snapshots of class-based aggregates are #323 |
| 2 | OpenTelemetry | ✅ | `encina.marten.decider.execute` span and `encina.marten.decider.decisions` counter (Phase 2, PR 2) |
| 3 | Structured Logging | ✅ | `DeciderLog` with EventIds 7250-7257, error codes only (Phase 2, PR 2) |
| 4 | Health Checks | ❌ | Event store connectivity is already `MartenHealthCheck`; the decider adds no dependency |
| 5 | Validation | ❌ | Command validation runs before the handler through the validation pipeline; `Decide` validates state transitions only |
| 6 | Resilience | ⏭️ | Retry of a concurrency conflict is the caller's policy; a Polly integration is a follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/decider-conflict-retry.md) |
| 7 | Distributed Locks | ❌ | Optimistic concurrency on the stream version covers concurrent access (Phase 2) |
| 8 | Transactions | ✅ | One Marten session and one `SaveChangesOrEjectAsync` append the events atomically (Phase 2) |
| 9 | Idempotency | ⏭️ | Duplicate command handling belongs to the inbox/idempotency behavior; a decider-specific deduplication key is a follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/decider-idempotency.md) |
| 10 | Multi-Tenancy | ⏭️ | Tenant-scoped streams are #326 (open, a sibling plan); the decider repository uses the session it is given |
| 11 | Module Isolation | ❌ | Marten has no module-scoped streams today; nothing to scope until a module needs them |
| 12 | Audit Trail | ✅ | The appended events are the record; metadata enrichment stays active through the shared session |

---

## Prerequisites & Dependencies

- No blocking issue. #326 (tenancy) and #323 (snapshots) are independent; the decider repository reads whatever session the DI container supplies.
- Related, not blocking: #315 and #330 (alternative event-sourcing patterns).

---

## Next Steps

1. The maintainer answers the Dilemmas (D1, D2, D3 and decisions 4a to 4d, 6 to 9 and 10) when work on #320 starts; the answers go into `## Maintainer Decisions` and the choices are updated.
2. Re-check the Design Choices against the code, then run `adversarial-reviewer` on the Settled and Trivial choices.
3. Write the briefs one PR at a time: PR 1 (Phase 1), then PR 2 (Phase 2), PR 3 (Phase 3 and the tests of Phase 5), PR 4 (documentation).
4. Open the follow-up issues of the Defer rows (drafts in `artifacts/decisions/plan-batch/followups/`).
