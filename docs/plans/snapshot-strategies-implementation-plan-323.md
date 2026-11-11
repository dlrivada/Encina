# Implementation Plan: Advanced Snapshot Strategies (TimeInterval, BusinessBoundary) for `Encina.Marten`

> **Issue**: [#323](https://github.com/dlrivada/Encina/issues/323)
> **Type**: Feature
> **Complexity**: Low-Medium (4 phases, 2 PRs, one provider)
> **Estimated Scope**: about 24 files, about 1,500 changed lines over 2 PRs

---

## Summary

`SnapshotOptions` (`src/Encina.Marten/Snapshots/SnapshotOptions.cs`) knows one trigger: `SnapshotEvery` (default 100 events), next to `KeepSnapshots` (3), `AsyncSnapshotCreation` (true) and the per-aggregate `ConfigureAggregate<TAggregate>(int snapshotEvery, int? keepSnapshots = null)`. `SnapshotAwareAggregateRepository<TAggregate>.TryCreateSnapshotAsync` decides with `aggregate.Version % snapshotEvery != 0` (line 398). This plan adds the **TimeInterval** and **BusinessBoundary** triggers and a **Composite** of triggers, evaluated by small policy objects instead of one hard-coded modulo.

**What it implements**

- `SnapshotStrategy` (`EventCount`, `TimeInterval`, `BusinessBoundary`, `Composite`) and the options `Strategy`, `SnapshotInterval` and a boundary-detector registration, global and per aggregate.
- Internal policies behind one internal `ISnapshotPolicy`: `EventCountSnapshotPolicy`, `TimeIntervalSnapshotPolicy`, `BusinessBoundarySnapshotPolicy`, `CompositeSnapshotPolicy` (any trigger fires).
- `ISnapshotBoundaryDetector<TAggregate>` for domain-event boundaries (`OrderCompleted`, month close).
- Snapshot results are no longer dropped: today the `Either` returned by `_snapshotStore.SaveAsync` and `PruneAsync` is discarded in both the async and the sync branch; the new code logs a `Left` by error code.
- The event-count trigger fires when a save crosses a multiple of `SnapshotEvery`, not only when the version lands exactly on one (a save that appends 3 events from version 99 to 102 never snapshots today).

**Standards**: AGENTS.md section 3 (opt-in, `TimeProvider`, errors not swallowed, no compatibility layers), ADR-018, ADR-021, ADR-027; event-store guidance (EventStoreDB/Kurrent) to snapshot at business boundaries.

**Affected packages**: `Encina.Marten` only (code, `PublicAPI.Unshipped.txt`, coverage manifest). No `Encina.EventStoreDB` package exists.

**Provider category**: event sourcing (Marten, AGENTS.md section 5); the 10-database rule does not apply. Integration tests run on Marten with PostgreSQL through Testcontainers.

**Size and split (AGENTS.md section 11)**: about 24 files exceed the 15-file limit. PR 1: Phases 1 and 2 (policies, options, repository change, unit, guard and property tests; about 15 files). PR 2: Phases 3 and 4 (diagnostics, integration tests, documentation; about 9 files).

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

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Where the time trigger gets its clock and its reference point</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `TimeProvider.GetUtcNow()` against the `CreatedAtUtc` of the latest snapshot read through `ISnapshotStore.GetLatestAsync`, evaluated on save | Exact, uses existing store API, testable with `FakeTimeProvider` | One extra read per save when the strategy includes a time trigger |
| B. A background timer that snapshots idle aggregates | Snapshots without a save | Timer callbacks must never throw, needs aggregate discovery, much more code |

### Chosen Option: **A — evaluate on save with `TimeProvider`**

### Rationale

`SnapshotAwareAggregateRepository` already receives a `TimeProvider` and already stamps `CreatedAtUtc` with it. "Every N hours" therefore means: the first save after the interval elapsed produces the snapshot. The extra read happens only when `Strategy` includes `TimeInterval`.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (time comes from `TimeProvider`; background timers must not throw), `ISnapshotStore<TAggregate>.GetLatestAsync`

</details>

<details>
<summary><strong>4. Event-count semantics: crossing a multiple, not landing on it</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Fire when `versionBefore / N < versionAfter / N` | Correct for multi-event saves | Behavior differs from today's modulo for such saves |
| B. Keep `Version % N == 0` | No change | Silently skips snapshots whenever a save does not end on a multiple; the composite "whichever comes first" cannot be built on equality |

### Chosen Option: **A — crossing semantics**

### Rationale

Equality is a latent defect of the only trigger today (`SnapshotAwareAggregateRepository.cs` line 398); a composite of triggers needs a threshold test. Pre-1.0 there is no compatibility concern.

**Class:** Settled
**Settled by:** AGENTS.md section 1 (pre-1.0: best solution, no compatibility layer) and the issue #323 acceptance "Composite strategy works correctly"

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

AGENTS.md section 3 forbids compatibility layers; `AggregateSnapshotConfig` (internal record) grows to carry the strategy. Callers in tests and docs are updated in PR 1.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (no legacy code or compatibility layers) and section 1 (pre-1.0)

</details>

---

## Implementation Phases

### Phase 1: Policies and Options

<details>
<summary><strong>Tasks</strong></summary>

1. `SnapshotStrategy` enum and `ISnapshotBoundaryDetector<TAggregate>` (`bool IsBoundary(TAggregate aggregate, object @event)`) in `src/Encina.Marten/Snapshots/`.
2. Internal `ISnapshotPolicy` with `bool ShouldSnapshot(SnapshotTriggerContext context)`; `SnapshotTriggerContext` record: aggregate, version before and after, new events, `nowUtc`, `lastSnapshotAtUtc`.
3. `EventCountSnapshotPolicy`, `TimeIntervalSnapshotPolicy`, `BusinessBoundarySnapshotPolicy`, `CompositeSnapshotPolicy` (OR over children).
4. `SnapshotOptions`: `Strategy`, `SnapshotInterval`; validation (`Interval` greater than zero for `TimeInterval`; `Composite` needs at least two triggers configured); replace `ConfigureAggregate` per Design Choice 5; extend `AggregateSnapshotConfig`.
5. `PublicAPI.Unshipped.txt` (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina.Marten snapshots (src/Encina.Marten/Snapshots). The only trigger today is Version % SnapshotEvery in SnapshotAwareAggregateRepository.TryCreateSnapshotAsync.
TASK: Add SnapshotStrategy, ISnapshotBoundaryDetector<TAggregate>, the internal ISnapshotPolicy with four implementations, and extend SnapshotOptions and AggregateSnapshotConfig. Event count fires on crossing a multiple; the composite is an OR.
KEY RULES: pure policy classes (clock and last snapshot time arrive in the context); invalid options fail at registration with a clear error, never silently; replace ConfigureAggregate(int, int?) completely; XML docs with examples; PublicAPI.Unshipped.txt.
REFERENCE FILES: src/Encina.Marten/Snapshots/SnapshotOptions.cs, src/Encina.Marten/Snapshots/SnapshotAwareAggregateRepository.cs, src/Encina.Marten/EncinaMartenOptions.cs
```

</details>

### Phase 2: Repository and DI

<details>
<summary><strong>Tasks</strong></summary>

1. `SnapshotAwareAggregateRepository`: replace the modulo with `ISnapshotPolicy` (built from `_snapshotConfig`); read `GetLatestAsync` only when the policy needs `lastSnapshotAtUtc`; keep `versionBefore = aggregate.Version - events.Length` as the crossing reference.
2. Log a `Left` from `SaveAsync` and `PruneAsync` by error code in both branches (today the `Either` is dropped); a snapshot failure still never fails the save.
3. `AddSnapshotBoundaryDetector<TAggregate, TDetector>()` in `ServiceCollectionExtensions` (`TryAddSingleton`); `AddSnapshotableAggregate<T>()` fails startup validation when the strategy needs a detector and none is registered.
4. DI test with `ValidateOnBuild` and `ValidateScopes`; unit tests per policy with `FakeTimeProvider`; guard tests; property test: composite equals the OR of its parts, crossing is monotonic; contract test for the detector interface.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Phase 1 created the policies. SnapshotAwareAggregateRepository is registered by AddSnapshotableAggregate<T>() (src/Encina.Marten/ServiceCollectionExtensions.cs).
TASK: Use the policy in TryCreateSnapshotAsync, log failed SaveAsync/PruneAsync results by error code, add AddSnapshotBoundaryDetector and the registration validation.
KEY RULES: TimeProvider only; never log EncinaError.Message; TryAdd* registration; tests instantiate the real repository with a substituted ISnapshotStore; update every caller of the old ConfigureAggregate overload.
REFERENCE FILES: src/Encina.Marten/Snapshots/SnapshotAwareAggregateRepository.cs, src/Encina.Marten/Snapshots/SnapshotLog.cs, tests/Encina.UnitTests/Marten, tests/Encina.ContractTests/Marten/Core/SnapshotAwareAggregateRepositoryContractTests.cs
```

</details>

### Phase 3: Observability

<details>
<summary><strong>Tasks</strong></summary>

1. Register `MartenSnapshotStrategies = (7260, 7269)` in `src/Encina/Diagnostics/EventIdRanges.cs`; add it to the `["Encina.Marten"]` list in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
2. `SnapshotStrategyLog` with `[LoggerMessage]` EventIds from 7260: trigger fired (strategy name), snapshot save failed (error code), prune failed (error code), invalid configuration.
3. `SnapshotMetrics` counter `encina.marten.snapshots.created` tagged `trigger` (`event_count`, `time`, `boundary`), and an activity `encina.marten.snapshot.create` (`aggregate.type`, `trigger`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: Snapshot logging lives in SnapshotLog.cs (EventIds 2629-2649 of the Marten range). The new range 7260-7269 is assigned to #323 from the family block 7250-7299.
TASK: Register the range first, write SnapshotStrategyLog, the counter and the activity, and call them from the repository.
KEY RULES: sequential EventIds from 7260; log error codes or ex.ForLogging() only; update AssemblyRanges; run the architecture tests.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Marten/Snapshots/SnapshotLog.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 4: Integration Tests, Documentation and Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. Integration tests (`tests/Encina.IntegrationTests/Infrastructure/Marten`, `[Trait("Category", "Integration")]`): each strategy creates a snapshot on a real Marten store; a multi-event save crossing the threshold snapshots; a failed prune is logged and the save still succeeds.
2. Manifest entries in `.github/coverage-manifest/Encina.Marten.json` for every new `src/` file with per-flag targets and one-sentence justifications (`mechanical-fixer`). Load test: justification `.md` (not concurrent code). Benchmark: optional, policy evaluation on the save path.
3. Documentation (`docs-writer`): snapshot section of the Marten guide, package README, ADR for D1 and D2 (ADR number assigned when the ADR pull request opens), changelog fragment `323-snapshot-strategies.added.md` and `323-snapshot-event-count-crossing.fixed.md`.
4. Verification: zero warnings, every flag at target, CRAP of changed methods at most 10.

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
| `Encina.Marten` (new range `MartenSnapshotStrategies`) | 7260-7269 | From the family block 7250-7299: #320 owns 7250-7259, this plan 7260-7269, #324 7270-7279, #326 7280-7289, #535 7290-7299 |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Policies and enum | 6 | Enum, detector interface, policy interface and context, four policies (counted with options) |
| Options and repository edits | 3 | `SnapshotOptions`, repository, DI |
| Diagnostics | 2 | Log class, metrics/activity |
| Unit and guard tests | 5 | Policies, repository, DI, guards |
| Contract and property tests | 2 | Detector contract, policy invariants |
| Integration tests | 1 | Marten on PostgreSQL |
| Docs, changelog, PublicAPI, manifest | 5 | Guide, ADR, two fragments, manifest |
| **Total** | **24** | Two PRs |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14. Issue #323 adds TimeInterval, BusinessBoundary and Composite snapshot triggers to Encina.Marten. Follow AGENTS.md: opt-in, TimeProvider, no swallowed errors, no compatibility layers, per-flag coverage.

IMPLEMENTATION OVERVIEW: Phase 1 SnapshotStrategy, detector interface, internal policies, options. Phase 2 repository uses the policy, logs failed store results, DI validation. Phase 3 EventId range 7260-7269, log class, metrics, activity. Phase 4 integration tests, manifest, docs, fragments. Two PRs (Phases 1-2, then 3-4). Dilemmas D1 (enum or pluggable) and D2 (meaning of boundary) follow the maintainer's answers.

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
| 5 | Validation | ✅ | Options validated at registration (Phase 1 and 2) |
| 6 | Resilience | ⏭️ | Snapshot store retry is a separate concern; a failed snapshot is logged and retried at the next trigger |
| 7 | Distributed Locks | ⏭️ | Two writers can both snapshot the same version; the store keys by aggregate and version so the second write is idempotent; a lock is only needed if that changes |
| 8 | Transactions | ❌ | A snapshot is an optimization written after the event append commits; it must not fail the save |
| 9 | Idempotency | ❌ | A snapshot per aggregate and version is idempotent by key |
| 10 | Multi-Tenancy | ⏭️ | Per-tenant snapshot policies arrive with #326 |
| 11 | Module Isolation | ⏭️ | Module-scoped streams are not designed in Marten today |
| 12 | Audit Trail | ❌ | The events are the audit record |

---

## Prerequisites & Dependencies

- No blocking issue. #326 (tenancy) and #320 (deciders) are independent; deciders can use snapshots only after their own plan decides it.
- #316 and #315 named in the original text are not snapshot work.

---

## Next Steps

1. The maintainer answers D1 and D2 when work on #323 starts; the answers go into `## Maintainer Decisions`.
2. Re-check the Design Choices against the code and run `adversarial-reviewer` on the Settled choices.
3. Write the PR 1 brief (Phases 1 and 2), then the PR 2 brief (Phases 3 and 4).
