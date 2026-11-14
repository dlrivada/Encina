# Implementation Plan: Advanced Snapshot Strategies (TimeInterval, BusinessBoundary) for `Encina.Marten`

> **Issue**: [#323](https://github.com/dlrivada/Encina/issues/323)
> **Type**: Feature
> **Complexity**: Low-Medium (4 phases, 4 PRs, one provider)
> **Estimated Scope**: 41 files and about 3,200 changed lines over 4 PRs (the table in "Estimated File Count" counts every added and edited file; the figures assume the recommended answers to the Dilemmas)

---

## Summary

`SnapshotOptions` (`src/Encina.Marten/Snapshots/SnapshotOptions.cs`) knows one trigger: `SnapshotEvery` (default 100 events), next to `KeepSnapshots` (3), `AsyncSnapshotCreation` (true) and the per-aggregate `ConfigureAggregate<TAggregate>(int snapshotEvery, int? keepSnapshots = null)`. `SnapshotAwareAggregateRepository<TAggregate>.TryCreateSnapshotAsync` decides with `aggregate.Version % snapshotEvery != 0` (line 398). This plan adds the **TimeInterval** and **BusinessBoundary** triggers and a **Composite** of triggers, evaluated by small policy objects instead of one hard-coded modulo.

**What it implements**

- `SnapshotStrategy` (`EventCount`, `TimeInterval`, `BusinessBoundary`, `Composite`) and the options `Strategy`, `SnapshotInterval` and a boundary-detector registration, global and per aggregate.
- Internal policies behind one internal `ISnapshotPolicy`: `EventCountSnapshotPolicy`, `TimeIntervalSnapshotPolicy`, `BusinessBoundarySnapshotPolicy`, `CompositeSnapshotPolicy` (any trigger fires).
- `ISnapshotBoundaryDetector<TAggregate>` for domain-event boundaries (`OrderCompleted`, month close).
- Snapshot results are no longer dropped: today the `Either` returned by `_snapshotStore.SaveAsync` and `PruneAsync` is discarded in both the async and the sync branch; the new code logs a `Left` by error code.
- The event-count trigger fires when a save crosses a multiple of `SnapshotEvery`, not only when the version lands exactly on one (a save that appends 3 events from version 99 to 102 never snapshots today; the #25 delta audit and the #52 audit both record the gap, see decision 4).

**Standards**: AGENTS.md section 3 (opt-in, `TimeProvider`, errors not swallowed, no compatibility layers), ADR-018, ADR-021, ADR-027; event-store guidance (EventStoreDB/Kurrent) to snapshot at business boundaries.

**Affected packages**: `Encina.Marten` only (code and coverage manifest; the package has no `PublicAPI.Unshipped.txt` on main, so no PublicAPI lines are written). No `Encina.EventStoreDB` package exists.

**Provider category**: event sourcing (Marten, AGENTS.md section 5); the 10-database rule does not apply. Integration tests run on Marten with PostgreSQL through Testcontainers.

**Size and split (AGENTS.md section 11)**: 41 files exceed the 15-file limit, so the work ships as four worker PRs of at most 15 files and 1,500 lines each, counted with every edited file, existing test caller, manifest entry, justification and changelog fragment. No PR uses a type, log class or project created by a later PR (the log class and the EventIds are created in PR 3, the first PR that logs, because the repository is the first logger), and each coverage target is entered in the same PR as the tests that meet it.

| PR | Content | Files | Lines (estimate) |
|----|---------|-------|------------------|
| 1 | Pure policies: enum, detector interface, `ISnapshotPolicy`, context, four policies, with unit, property and contract tests and the manifest | 12 | about 900 |
| 2 | Options: `AggregateSnapshotOptions`, `SnapshotOptions` and `EncinaMartenOptions` edits (the replaced overload and its callers), with tests and the manifest | 6 | about 400 |
| 3 | Repository, DI and diagnostics: repository edit, `AddSnapshotBoundaryDetector`, startup validator, `SnapshotStrategyLog`, metrics, activity source, EventId range, with tests and the manifest | 15 | about 1,300 |
| 4 | Integration tests, load justification, documentation and changelog | 8 | about 600 |

---

## Design Choices

<details>
<summary><strong>1. Shape of the trigger configuration (issue Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `SnapshotStrategy` enum plus fixed options | Simple for the three common cases, easy to validate at registration | Closed set |
| B. A public pluggable `ISnapshotPolicy` | Open for custom triggers | More to configure for the common cases; a larger public surface to keep stable |

### Chosen Option: **A — enum plus options, with an internal `ISnapshotPolicy`** (recommended, pending the maintainer)

### Rationale

The three triggers and their OR cover the issue; the domain-specific case is already open through `ISnapshotBoundaryDetector`. The policy interface stays internal, so promoting it to public later is additive. B wins when applications need triggers the library cannot anticipate (for example size-based triggers).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Meaning of "business boundary" (issue Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Named calendar boundaries (`EndOfDay`, `MonthEnd`) | Declarative | Needs a time zone and a definition of "end of day"; `BusinessBoundaries = ["EndOfDay"]` string names are untyped |
| B. Domain-event types detected by `ISnapshotBoundaryDetector<TAggregate>` | No time zone, domain driven, typed | The application writes a detector per aggregate |

### Chosen Option: **B — detector over domain events** (recommended, pending the maintainer)

### Rationale

Snapshots are taken on save and evaluated against the events just appended, so a boundary is naturally "this event closes a unit of business" (`OrderCompleted`). Calendar boundaries can be shipped later as a ready-made detector built on `TimeProvider` and an explicit `TimeZoneInfo`. A wins when boundaries are purely calendar based and users do not want to write code.

Under B the public shape is part of the answer: `ISnapshotBoundaryDetector<TAggregate>` with `bool IsBoundary(TAggregate aggregate, object @event)` (the event is passed as `object` because `IAggregate.UncommittedEvents` is untyped), one detector per aggregate type, registered with `TryAddSingleton`. It replaces the non-generic signature and the `BusinessBoundaries` string list written in the issue.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Where the time trigger gets its clock and its reference point</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `TimeProvider.GetUtcNow()` against the `CreatedAtUtc` of the latest snapshot read through `ISnapshotStore.GetLatestAsync`, evaluated on save | Exact, uses existing store API, testable with `FakeTimeProvider` | One extra read per save when the strategy includes a time trigger |
| B. A background timer that snapshots idle aggregates | Snapshots without a save | Timer callbacks must never throw, needs aggregate discovery, much more code |

### Chosen Option: **A — evaluate on save with `TimeProvider`** (recommended, pending the maintainer)

### Rationale

`SnapshotAwareAggregateRepository` already receives a `TimeProvider` and already stamps `CreatedAtUtc` with it. "Every N hours" therefore means: the first save after the interval elapsed produces the snapshot. The extra read happens only when `Strategy` includes `TimeInterval`. Option B also uses `TimeProvider` and AGENTS.md section 3 only says that a timer callback must not throw, so the rule does not select A: B wins when idle aggregates must be snapshotted without a save, at the cost of aggregate discovery and a hosted service.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Event-count semantics: crossing a multiple, not landing on it</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Fire when `versionBefore / N < versionAfter / N` | Correct for multi-event saves | Behavior differs from today's modulo for such saves |
| B. Keep `Version % N == 0` | No change | Silently skips snapshots whenever a save does not end on a multiple; the composite "whichever comes first" cannot be built on equality |

### Chosen Option: **A — crossing semantics** (recommended, pending the maintainer)

### Rationale

Equality is a latent defect of the only trigger today (`SnapshotAwareAggregateRepository.cs` line 398); the #52 audit (`docs/knowledge/audits/52/stages/tests.md`, finding 3) and the #25 delta audit (`docs/knowledge/audits/25/delta-2026-10/tests.md`, the behaviour-not-covered note) record that an aggregate that raises several events in one save can jump over a multiple and never snapshot. A composite of triggers needs a threshold test. Option B violates no cited rule and "pre-1.0 best solution" does not select one option, so the choice goes to the maintainer; when A is accepted it ships as the `.fixed` changelog fragment tied to those findings.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Per-aggregate override replaces the numeric overload</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Replace `ConfigureAggregate<T>(int, int?)` with `ConfigureAggregate<T>(Action<AggregateSnapshotOptions>)` | One way to configure; carries strategy, interval, keep count | Existing callers and tests change |
| B. Keep the numeric overload and add the lambda overload | No caller changes | Two ways to do the same thing; a compatibility layer |

### Chosen Option: **A — replace**

### Rationale

AGENTS.md section 3 forbids compatibility layers; `AggregateSnapshotConfig` (internal record) grows to carry the strategy. The existing callers change in PR 2 together with the signature: 11 `ConfigureAggregate` calls in `tests/Encina.UnitTests/Marten/Snapshots/SnapshotOptionsTests.cs`, the XML-doc sample in `src/Encina.Marten/EncinaMartenOptions.cs` (line 59) and the sample in `docs/INVENTORY.md` (line 1846, edited in PR 4). The `ConfigureAggregate` methods of `Encina.EntityFrameworkCore` are a different API and do not change.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (no legacy code or compatibility layers) and section 1 (pre-1.0)

</details>

<details>
<summary><strong>6. How <code>Composite</code> chooses its triggers, and the default interval</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `Composite` plus an explicit list of triggers (`Triggers`, at least two of `EventCount`, `TimeInterval`, `BusinessBoundary`) validated at startup; `SnapshotInterval` is `TimeSpan?` with no default and required when the time trigger is used | Explicit; the default `SnapshotEvery = 100` never turns the event trigger on by accident | One more property |
| B. `[Flags]` enum with no `Composite` member (flags combined with a bitwise OR), defaults as today | One property fewer | The issue's `Composite` name disappears; `SnapshotEvery` still has a default of 100 |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

`SnapshotEvery` defaults to 100, so "at least two triggers configured" cannot be read from the settings (`src/Encina.Marten/Snapshots/SnapshotOptions.cs`); an explicit list removes the ambiguity and keeps the issue's acceptance wording "Composite strategy works correctly". A default interval would be an arbitrary number with no evidence, so none is chosen.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Time trigger when no snapshot exists, and when the store read fails</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. With no snapshot (`lastSnapshotAtUtc` null) the time trigger fires on the first save; when `GetLatestAsync` returns `Left` the time trigger is skipped for that save and the error code is logged | An aggregate gets its first snapshot soon; a failing store does not cause a snapshot storm; the save never fails | A store outage delays time snapshots |
| B. With no snapshot wait one interval from the first event; on `Left` fire anyway | No early snapshot of a young aggregate | Needs the creation time of the stream; firing into a failing store also fails |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

A snapshot is an optimization written after the append commits; its failure never fails the save (existing behavior in `TryCreateSnapshotAsync`), so the question is only what to do with the trigger. B needs data the repository does not have (the first event's timestamp is not on `IAggregate`).

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Which saves evaluate the triggers, and what a crossing produces</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `SaveAsync` and `CreateAsync` both evaluate the triggers (today only `SaveAsync` calls `TryCreateSnapshotAsync`, line 183; `CreateAsync` never snapshots); a save that crosses several multiples produces one snapshot at the latest version | One rule for both entry points; the aggregate in hand is the only state available | Behavior change for `CreateAsync` |
| B. Only `SaveAsync` evaluates | No behavior change for `CreateAsync` | An aggregate created with more events than `SnapshotEvery` is never snapshotted by the create |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

One snapshot at the latest version is forced by the code: `Snapshot<T>(aggregate.Id, aggregate.Version, aggregate, ...)` is built from the aggregate after the save, and intermediate states would need a replay. Whether `CreateAsync` evaluates is the open part.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. How invalid configuration and a missing detector are surfaced</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Range checks throw at configuration time (`ArgumentOutOfRangeException`, as `ConfigureAggregate` does today for `snapshotEvery < 1`); cross-setting rules (interval required, detector registered, two or more triggers) are checked by an `IValidateOptions<EncinaMartenOptions>` registered by `AddSnapshotableAggregate` with `ValidateOnStart`, failing host startup | Fails at startup, not at first save; no registration-order problem, because options are configured after `AddSnapshotableAggregate` runs | One validator file |
| B. Everything throws from `ConfigureAggregate` and the setters | No validator | A detector registered after the aggregate is invisible at configuration time |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

Configuration errors are programmer errors, not business results, so exceptions at startup are not a breach of the railway rule (ADR-001). A detector that throws at runtime is caught in the repository, logged with `ex.ForLogging()` and treated as "not a boundary" so the save still succeeds; a missing detector cannot reach runtime for a strategy that needs one, because the validator fails startup first.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Policies (PR 1) and Options (PR 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 (12 files):

1. `SnapshotStrategy` enum and `ISnapshotBoundaryDetector<TAggregate>` (`bool IsBoundary(TAggregate aggregate, object @event)`, decision 2) in `src/Encina.Marten/Snapshots/`.
2. Internal `ISnapshotPolicy` with `bool ShouldSnapshot(SnapshotTriggerContext context)`; `SnapshotTriggerContext` record: aggregate, version before and after, new events, `nowUtc`, `lastSnapshotAtUtc`.
3. `EventCountSnapshotPolicy`, `TimeIntervalSnapshotPolicy`, `BusinessBoundarySnapshotPolicy`, `CompositeSnapshotPolicy` (OR over children; decisions 6 and 7).
4. Tests: `SnapshotPolicyTests` (unit, with `FakeTimeProvider`), a property test (the composite equals the OR of its parts; crossing is monotonic), a contract test for `ISnapshotBoundaryDetector<T>` with a real detector. The policies are internal; the guard obligation for them is covered by the unit tests because they have no validated public parameters, and the public `ISnapshotBoundaryDetector` is an interface with no code.
5. Manifest entries for the eight new files in `.github/coverage-manifest/Encina.Marten.json`, per flag with a one-sentence justification, in this PR (`mechanical-fixer`). No PublicAPI lines: `Encina.Marten` has no `PublicAPI.Unshipped.txt` on main.

PR 2 (6 files):

1. `AggregateSnapshotOptions` (new public class: `Strategy`, `SnapshotEvery`, `SnapshotInterval`, `KeepSnapshots`, `Triggers`); `SnapshotOptions`: `Strategy`, `Triggers`, `SnapshotInterval`; range checks throw at configuration time (decision 9); replace `ConfigureAggregate` per Design Choice 5 with `ConfigureAggregate<T>(Action<AggregateSnapshotOptions>)`; extend `AggregateSnapshotConfig`.
2. The existing callers move with the signature: the 11 calls in `tests/Encina.UnitTests/Marten/Snapshots/SnapshotOptionsTests.cs` and the XML-doc sample at `src/Encina.Marten/EncinaMartenOptions.cs` line 59.
3. Guard tests in `tests/Encina.GuardTests/Marten/Snapshots/SnapshotGuardTests.cs` (existing file, edited): `ConfigureAggregate` with a null delegate, `SnapshotEvery < 1`, `SnapshotInterval <= TimeSpan.Zero`, `KeepSnapshots < 0` (ArgumentOutOfRange), `Triggers` null.
4. Manifest entries for `AggregateSnapshotOptions.cs`, and raised targets for `SnapshotOptions.cs`, per flag with one-sentence justifications, in this PR.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina.Marten snapshots (src/Encina.Marten/Snapshots). The only trigger today is Version % SnapshotEvery in SnapshotAwareAggregateRepository.TryCreateSnapshotAsync.
TASK: PR 1: add SnapshotStrategy, ISnapshotBoundaryDetector<TAggregate> and the internal ISnapshotPolicy with four implementations, with unit, property and contract tests. PR 2: add AggregateSnapshotOptions and extend SnapshotOptions and AggregateSnapshotConfig, updating every caller of the old overload. Event count fires on crossing a multiple; the composite is an OR.
KEY RULES: pure policy classes (clock and last snapshot time arrive in the context); invalid options fail at configuration or startup with a clear error, never silently; replace ConfigureAggregate(int, int?) completely; XML docs with examples; guard tests for every validated parameter; no PublicAPI file exists for Encina.Marten.
REFERENCE FILES: src/Encina.Marten/Snapshots/SnapshotOptions.cs, src/Encina.Marten/Snapshots/SnapshotAwareAggregateRepository.cs, src/Encina.Marten/EncinaMartenOptions.cs
```

</details>

### Phase 2: Repository, DI and Diagnostics (PR 3)

<details>
<summary><strong>Tasks</strong></summary>

1. `SnapshotAwareAggregateRepository`: replace the modulo with `ISnapshotPolicy` (built from `_snapshotConfig`); read `GetLatestAsync` only when the policy needs `lastSnapshotAtUtc`; keep `versionBefore = aggregate.Version - events.Length` as the crossing reference; `CreateAsync` evaluates too (decision 8); a detector that throws is caught and logged (decision 9).
2. Log a `Left` from `SaveAsync` and `PruneAsync` by error code in both branches (today the `Either` is dropped); a snapshot failure still never fails the save.
3. `AddSnapshotBoundaryDetector<TAggregate, TDetector>()` in `ServiceCollectionExtensions` (`TryAddSingleton`); `AddSnapshotableAggregate<T>()` also registers `SnapshotOptionsValidator` (`IValidateOptions<EncinaMartenOptions>`, `ValidateOnStart`) which fails startup when the strategy needs a detector or an interval and none is set (decision 9).
4. Diagnostics, created here because the repository is the first logger: `SnapshotStrategyLog`, `SnapshotMetrics`, `SnapshotActivitySource`, the EventId range in `src/Encina/Diagnostics/EventIdRanges.cs` and the `AssemblyRanges` entry (Phase 3).
5. Tests: repository unit tests (edit of `SnapshotAwareAggregateRepositoryTests`: every trigger, crossing, `CreateAsync`, store `Left`, detector throwing), `SnapshotOptionsValidatorTests`, DI test with `ValidateOnBuild` and `ValidateScopes` (a new file), diagnostics unit test, guard tests: `AddSnapshotBoundaryDetector` null services in `tests/Encina.GuardTests/Marten/Core/ServiceCollectionExtensionsGuardTests.cs` and the repository constructor in `tests/Encina.GuardTests/Marten/Snapshots/SnapshotAwareAggregateRepositoryGuardTests.cs` (both edited). The property and contract tests shipped in PR 1.
6. Manifest: new entries for the validator, log, metrics and activity-source files and raised unit and guard targets for the repository and `ServiceCollectionExtensions.cs`, each with a one-sentence justification, in this PR.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Phase 1 created the policies. SnapshotAwareAggregateRepository is registered by AddSnapshotableAggregate<T>() (src/Encina.Marten/ServiceCollectionExtensions.cs).
TASK: Use the policy in TryCreateSnapshotAsync, log failed SaveAsync/PruneAsync results by error code, add AddSnapshotBoundaryDetector and the startup validator, plus the log class, metrics and activity source (register the EventId range first).
KEY RULES: TimeProvider only; never log EncinaError.Message; TryAdd* registration; tests instantiate the real repository with a substituted ISnapshotStore; update every caller of the old ConfigureAggregate overload.
REFERENCE FILES: src/Encina.Marten/Snapshots/SnapshotAwareAggregateRepository.cs, src/Encina.Marten/Snapshots/SnapshotLog.cs, tests/Encina.UnitTests/Marten, tests/Encina.ContractTests/Marten/Core/SnapshotAwareAggregateRepositoryContractTests.cs
```

</details>

### Phase 3: Observability (PR 3)

<details>
<summary><strong>Tasks</strong></summary>

1. Register `MartenSnapshotStrategies = (7260, 7269)` in `src/Encina/Diagnostics/EventIdRanges.cs`; add it to the `["Encina.Marten"]` list in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`. The range is inside the block assigned to this family in `artifacts/decisions/plan-batch/eventid-blocks.md`.
2. `SnapshotStrategyLog` with `[LoggerMessage]` EventIds packed from 7260: trigger fired (strategy name), store read failed (error code), snapshot save failed (error code), prune failed (error code), boundary detector threw (`ex.ForLogging()`) = 5 ids, 7260 to 7264.
3. `SnapshotMetrics` counter `encina.marten.snapshots.created` tagged `trigger` (`event_count`, `time`, `boundary`), and `SnapshotActivitySource` with the activity `encina.marten.snapshot.create` (`aggregate.type`, `trigger`). `Encina.Marten` has no `Meter` or `ActivitySource` on main, so these are the first; they expose their names as constants (`MeterName`, `SourceName` = `Encina.Marten`) and are created by the repository. Wiring the source and meter into `Encina.OpenTelemetry` (`AddSource`, `AddMeter` in its `ServiceCollectionExtensions`) is a follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/marten-otel-registration.md); until then an application adds them by name. They are internal types with no public API.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: Snapshot logging lives in SnapshotLog.cs (EventIds 2629-2649 of the Marten range). The new range 7260-7269 is assigned to #323 from the family block 7250-7299.
TASK: Register the range first, write SnapshotStrategyLog (5 ids), the counter and the activity, and call them from the repository (PR 3).
KEY RULES: sequential EventIds from 7260; log error codes or ex.ForLogging() only; update AssemblyRanges; run the architecture tests.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Marten/Snapshots/SnapshotLog.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 4: Integration Tests, Documentation and Finalization (PR 4)

<details>
<summary><strong>Tasks</strong></summary>

1. Integration tests (`tests/Encina.IntegrationTests/Infrastructure/Marten`, `[Trait("Category", "Integration")]`): each strategy creates a snapshot on a real Marten store; a multi-event save crossing the threshold snapshots; a failed prune is logged and the save still succeeds.
2. Manifest: integration-flag targets for the repository and the options, per file with a one-sentence justification, in this PR with the tests that meet them (`mechanical-fixer`). Load test: justification `.md` (not concurrent code; allowed for load tests, never for unit, guard or contract). Benchmark: optional, policy evaluation on the save path.
3. Documentation (`docs-writer`): snapshot section of the Marten guide, ADR for D1 and D2 (ADR number assigned when the ADR pull request opens), `docs/INVENTORY.md` (the `ConfigureAggregate` sample at line 1846); `Encina.Marten` has no package README on main. Changelog fragments `changelog.d/323-snapshot-strategies.added.md` and `changelog.d/323-snapshot-event-count-crossing.fixed.md` (tied to the #25 and #52 audit findings of decision 4).
4. Verification in every PR: zero warnings, every flag at target, CRAP of changed methods at most 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: Policies, repository changes and diagnostics exist. Coverage is per flag against .github/coverage-manifest/Encina.Marten.json.
TASK: Write the integration tests on the shared Marten PostgreSQL fixture, record per-file targets, and finish the docs and fragments.
KEY RULES: no per-class containers; Shouldly through Encina.Testing.Shouldly; docs cite coverage with covref markers only.
REFERENCE FILES: tests/Encina.IntegrationTests/Infrastructure/Marten, docs/testing/coverage-measurement-methodology.md, changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| EventStoreDB / Kurrent snapshot guidance | Snapshot at business boundaries rather than arbitrary counts |
| ADR-027 | Marten as the event-sourcing provider |
| ADR-021 | EventId registry and allocation test |
| AGENTS.md section 3 | `TimeProvider`, errors not swallowed, opt-in, no compatibility layers |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `SnapshotOptions`, `AggregateSnapshotConfig` | `src/Encina.Marten/Snapshots/SnapshotOptions.cs` | Extended with the strategy and interval |
| `SnapshotAwareAggregateRepository<T>` | `src/Encina.Marten/Snapshots/SnapshotAwareAggregateRepository.cs` | Replaces the modulo with the policy; already takes `TimeProvider` |
| `ISnapshotStore<T>`, `Snapshot<T>` | `src/Encina.Marten/Snapshots` | `GetLatestAsync` gives the last snapshot time |
| `SnapshotLog`, `SnapshotErrorCodes` | `src/Encina.Marten/Snapshots` | Style and codes for the new log class |
| `AddSnapshotableAggregate<T>()` | `src/Encina.Marten/ServiceCollectionExtensions.cs` | Registration and validation point |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Marten` (new range `MartenSnapshotStrategies`) | 7260-7269 (ids 7260-7264 used) | Inside the block 7250-7299 assigned to the family in the batch registry (`artifacts/decisions/plan-batch/eventid-blocks.md`), which records the sub-ranges of the sibling plans |

### Estimated File Count

Every added or edited file is counted, including the existing callers of the replaced `ConfigureAggregate` overload. `Encina.Marten` has no PublicAPI file, so no PublicAPI lines appear.

| PR | Category | Files | Notes |
|----|----------|-------|-------|
| 1 | Source | 8 | `SnapshotStrategy`, `ISnapshotBoundaryDetector`, `ISnapshotPolicy`, `SnapshotTriggerContext`, four policies |
| 1 | Tests | 3 | Policies unit, property, detector contract |
| 1 | Manifest | 1 | `Encina.Marten.json` |
| 1 | **Subtotal** | **12** | |
| 2 | Source | 3 | `AggregateSnapshotOptions` (new), `SnapshotOptions` edit, `EncinaMartenOptions` doc-sample edit |
| 2 | Tests | 2 | `SnapshotOptionsTests` (the 11 existing callers), `SnapshotGuardTests` |
| 2 | Manifest | 1 | `Encina.Marten.json` |
| 2 | **Subtotal** | **6** | |
| 3 | Source | 8 | Repository edit, `ServiceCollectionExtensions` edit, `SnapshotOptionsValidator`, `SnapshotStrategyLog`, `SnapshotMetrics`, `SnapshotActivitySource`, `EventIdRanges.cs`, `EncinaEventIdAllocationTests.cs` |
| 3 | Tests | 6 | Repository unit (edit), validator unit, DI, diagnostics unit, `ServiceCollectionExtensionsGuardTests` (edit), repository guard (edit) |
| 3 | Manifest | 1 | `Encina.Marten.json` |
| 3 | **Subtotal** | **15** | |
| 4 | Tests | 2 | Integration (Marten on PostgreSQL), load-test `.md` justification |
| 4 | Docs | 3 | Guide section, ADR, `docs/INVENTORY.md` |
| 4 | Changelog | 2 | `.added` and `.fixed` fragments |
| 4 | Manifest | 1 | `Encina.Marten.json` (integration targets) |
| 4 | **Subtotal** | **8** | |
| | **Total** | **41** | 12 + 6 + 15 + 8; each PR at most 15 files and 1,500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14. Issue #323 adds TimeInterval, BusinessBoundary and Composite snapshot triggers to Encina.Marten. Follow AGENTS.md: opt-in, TimeProvider, no swallowed errors, no compatibility layers, per-flag coverage.

IMPLEMENTATION OVERVIEW: Phase 1 SnapshotStrategy, detector interface, internal policies (PR 1), options (PR 2). Phase 2 repository uses the policy, logs failed store results, startup validator, with Phase 3 EventId range 7260-7269, log class, metrics, activity (PR 3). Phase 4 integration tests, manifest, docs, fragments (PR 4). Four PRs (12, 6, 15 and 8 files). Dilemmas D1 (enum or pluggable), D2 (meaning of boundary) and decisions 3, 4 and 6 to 9 follow the maintainer's answers.

KEY PATTERNS: pure policies with the clock in the context; crossing semantics for event count; TryAdd* registration; sequential EventIds registered first; shared integration fixtures.

REFERENCE FILES: src/Encina.Marten/Snapshots/SnapshotOptions.cs, src/Encina.Marten/Snapshots/SnapshotAwareAggregateRepository.cs, src/Encina.Marten/ServiceCollectionExtensions.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/plans/dsr-implementation-plan-404.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A snapshot is persisted aggregate state, not an `ICacheProvider` entry |
| 2 | OpenTelemetry | ✅ | `encina.marten.snapshots.created` counter and `encina.marten.snapshot.create` span (Phase 3) |
| 3 | Structured Logging | ✅ | `SnapshotStrategyLog`, EventIds 7260-7269 (Phase 3) |
| 4 | Health Checks | ❌ | Connectivity is `MartenHealthCheck`; no new dependency |
| 5 | Validation | ✅ | Options validated at configuration and at startup (decision 9, Phases 1 to 3) |
| 6 | Resilience | ❌ | A failed snapshot is logged by error code and retried at the next trigger, so a retry policy would add no guarantee |
| 7 | Distributed Locks | ❌ | Two writers can both snapshot the same version; the document id is the composite of aggregate id and version (`SnapshotEnvelope.CreateId` in `SnapshotEnvelope.cs`) and `MartenSnapshotStore.SaveAsync` stores it with `_session.Store`, so the second write replaces the first with the same state |
| 8 | Transactions | ❌ | The issue's own table says Integrate; this plan overrides it: a snapshot is written after the event append commits and must not fail the save, so it is outside the atomic unit |
| 9 | Idempotency | ❌ | A snapshot per aggregate and version is idempotent by its composite document id (see row 7) |
| 10 | Multi-Tenancy | ⏭️ | Tenant-aware event sourcing is #326 (open); this plan adds nothing tenant specific |
| 11 | Module Isolation | ❌ | Marten has no module-scoped streams today, so there is nothing to scope |
| 12 | Audit Trail | ❌ | The events are the audit record |

---

## Prerequisites & Dependencies

- No blocking issue. #326 (tenancy) and #320 (deciders) are independent; deciders can use snapshots only after their own plan decides it.
- #316 and #315 named in the original text are not snapshot work.

---

## Next Steps

1. The maintainer answers the Dilemmas (D1, D2 and decisions 3, 4 and 6 to 9) when work on #323 starts; the answers go into `## Maintainer Decisions`.
2. Re-check the Design Choices against the code and run `adversarial-reviewer` on the Settled choices.
3. Write the briefs one PR at a time: PR 1 (policies), PR 2 (options), PR 3 (repository, DI, diagnostics), PR 4 (integration tests and documentation).
4. Open the follow-up issue of the OpenTelemetry wiring (draft in `artifacts/decisions/plan-batch/followups/marten-otel-registration.md`).
