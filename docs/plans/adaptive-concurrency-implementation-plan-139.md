# Implementation Plan: Adaptive concurrency control (Gradient and AIMD) in `Encina.Resilience`

> **Issue**: [#139](https://github.com/dlrivada/Encina/issues/139)
> **Type**: Feature
> **Complexity**: High (7 phases, no database providers, 5 pull requests)
> **Estimated Scope**: ~800 lines of production code + ~1,400 lines of tests (about 2,300 changed lines in total, five pull requests)

---

## Summary

A concurrency limit that tunes itself: instead of a fixed bulkhead size, the limit of a request type moves with the latency and error rate the type shows, as in Netflix's `concurrency-limits`. This plan delivers `[AdaptiveConcurrency]`, two algorithms (Gradient and AIMD, the two the acceptance criteria require), a limiter that rejects work above the current limit with a coded `Left`, rolling latency and error tracking, and instruments for the limit. The comment of 2026-10-23 on #139 moves it into the 1.0 scope; [ADR-055](../architecture/adr/055-resilience-family.md) places the work in `Encina.Resilience` (R1), as a strategy of the per-type pipeline (R2), configured by attribute and options (R3).

What exists today (read in `origin/main` at `e31de98cb`):

- Nothing adaptive: `git grep -E "AdaptiveConcurrency|ConcurrencyAlgorithm" -- src` has no match. `Encina.Polly` has a hand-written `BulkheadManager` (`src/Encina.Polly/Bulkhead/BulkheadManager.cs`) with a fixed limit and open defects (#2628-#2634). ADR-055 R2 deletes it and turns the bulkhead into a concurrency partition of the #2414 rate-limiting core; neither the core nor `Encina.Resilience` exists in `src/`, and R6 has no issue.
- `AdaptiveRateLimiter` (`src/Encina.Polly/RateLimiting/AdaptiveRateLimiter.cs`) is a different thing (it adapts a request rate after failures) and is rebuilt by #2414 D2; this plan does not touch it.
- #131 (backpressure) has a section on adaptive concurrency; the spike report merges that section into #139. #131 and #144 have no plan on main; their plans consume the limiter defined here.

- **Affected packages**: `Encina.Resilience` (planned), `Encina` core (`EventIdRanges.cs`). The issue also ticks `Encina.AspNetCore`; it needs no code.
- **Provider category**: Resilience; no database, caching, transport, lock or validation matrix applies.
- **Prerequisites**: the foundation and R6 (tied to Dilemma 1 of the #136 plan).

### Shared Pieces and Ownership

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `Encina.Resilience`, compiler, options front-ends | foundation issue (`resilience-foundation-encina-resilience.md`) | all |
| R6 token and deadline | R6 issue (`resilience-r6-pipeline-token-and-deadline.md`) | #136, #141, #335 |
| `RollingOutcomeWindow` (latency percentile and error rate over a time window) | this plan (#139, PR-1) | #138 (utilization provider: p99 and error rate); #131 and #144 (sibling plans, by number) |
| `IAdaptiveConcurrencyLimiter`, `IConcurrencyAlgorithm`, `ConcurrencyMetrics` | this plan (#139) | #131, #144 |
| The #2414 core partition | #2414 (open; the core has no plan) | the seam in Design Choice 2 |
| EventIds 924-935 (`ResilienceAdaptiveConcurrency`) | this plan | none |

---

## Design Choices

<details>
<summary><strong>1. Where the code lives while `Encina.Resilience` does not exist</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Wait for the foundation; implement in `Encina.Resilience`** | Final home | Blocked on an unplanned foundation |
| **B) Build now in `Encina.Polly`** | Ships earlier | Rework when R1 removes the package |

### Chosen Option: **A — Wait for the foundation** (recommended, pending the maintainer)

### Rationale

- A follows the spike report order; B wins if adaptive limiting must ship first. Asked once for the family (#136 choice 1). Under B the paths, namespaces, the EventId assembly map and the project differ; every PR needs the project to exist (foundation merged, or option C of choice 1 creating it in PR-1 with the `ProjectReference` edits of the test projects, which would add files to PR-1).

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Integration with the bulkhead (acceptance criterion "integration with the existing `BulkheadManager`")</strong></summary>

`BulkheadManager` is deleted by R2 and its replacement, a concurrency partition of the #2414 core, is not built.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Ship a standalone `AdaptiveConcurrencyLimiter` that derives from `System.Threading.RateLimiting.RateLimiter`, so the #2414 core can host it as a partition; it fills the concurrency slot of the Admission pipeline; the bulkhead integration is the core's adoption of this limiter** | Deliverable now; the BCL base class is the seam; no code for `BulkheadManager`, which is deleted | The criterion is met by seam, not by an existing bulkhead |
| **B) Wait for the #2414 core and build the adaptive limit as its partition** | Literal integration | Blocked on an unplanned core; the BCL `ConcurrencyLimiter` has a fixed `PermitLimit`, so the core would need a mutable limiter anyway |
| **C) Modify `BulkheadManager` now** | Literal text of the issue | Edits code R2 deletes and ties to open defects |

### Chosen Option: **A — Standalone limiter on the BCL base class** (recommended, pending the maintainer)

### Rationale

- A wins when the limiter must ship independently of the core; B wins if the maintainer prefers one limiter hierarchy built once; C wins only if the old package must keep working until 1.0, which R1 rules out.
- A type with both `[Bulkhead]` and `[AdaptiveConcurrency]` is a startup error (one concurrency limit per type) in the validator; nothing is silently ignored.

- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Which algorithms</strong></summary>

The issue enum lists Gradient, AIMD, Vegas and FixedWindow; the acceptance criteria require "at least 2 algorithm implementations (Gradient, AIMD)".

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Gradient and AIMD; the enum has exactly those two members; `IConcurrencyAlgorithm` stays public for custom algorithms** | Meets the criterion; no dead enum members | Vegas and FixedWindow (named in the issue's enum and task list) are not offered |
| **B) All four** | Matches the enum and the whole task list | Two algorithms nobody asked to ship first; more tests and options |

### Chosen Option: **A — Gradient and AIMD** (recommended, pending the maintainer)

### Rationale

- A wins when the acceptance criteria ("at least 2 algorithm implementations (Gradient, AIMD)") are the scope and a custom `IConcurrencyAlgorithm` covers the rest; B wins if the maintainer wants the issue's full enum at 1.0. Omitting Vegas and FixedWindow is recorded here, not silently dropped.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Smooth adjustments and the algorithm formulas</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Gradient as Netflix's `Gradient2Limit`: `gradient = clamp(rttNoLoad / rttSample, 0.5, 1.0)`, `new = limit * gradient + queueAllowance`, smoothed `limit * (1 - s) + new * s`, clamped to `[Min, Max]`; AIMD as `AIMDLimit`: increase by 1 when in-flight is at least half the limit and no drop, multiply by `0.9` on a drop; at most one adjustment per sample window** | Proven, bounded, no oscillation beyond the smoothing | `rttNoLoad` needs a decaying minimum |
| **B) Raw issue formula `limit * RTTnoLoad / RTTactual` with no clamp or smoothing** | Simplest | Oscillates; the criterion "smooth limit adjustments" is not met |

### Chosen Option: **A — Netflix formulas with smoothing and one adjustment per window**

### Rationale

- A drop is a rejected-by-downstream outcome: a transient `Left` (ADR-029 classification), a timeout, or an error rate above `ErrorThresholdPercent` in the window.
- **Class:** Settled
- **Settled by:** Netflix `concurrency-limits` (`Gradient2Limit`, `AIMDLimit`; named in the issue's references) and the #139 criterion "smooth limit adjustments"

</details>

<details>
<summary><strong>5. When the limit is recomputed</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Inline: when a request completes and the sample window (measured with `TimeProvider.GetTimestamp`) has elapsed, the completing call computes the new limit under a lock-free compare-and-swap guard** | No timer, no background loop that can throw; deterministic with `FakeTimeProvider` | With no traffic the limit does not move (there is nothing to measure) |
| **B) A `PeriodicTimer` hosted service, per the issue's `AdjustmentIntervalMs`** | Fixed cadence | A background loop that must never let an exception escape (AGENTS.md section 3); needs a host, a lifecycle and per-iteration catch |

### Chosen Option: **A — Inline window evaluation** (recommended, pending the maintainer)

### Rationale

- A wins on simplicity and testability; B wins if gauges must change while the service is idle. `AdjustmentIntervalMs` becomes `SampleWindow`.

- **Class:** Dilemma

</details>

<details>
<summary><strong>6. The partition key</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One limiter per request type (the registry key of R2)** | Protects the process and the downstream per type; no tenant segment, so no reserved-`default` or injectivity question; bounded count | No per-tenant fairness |
| **B) Per request type and tenant, denied without tenant when multi-tenancy is on (as #2414 D3 for rate limits)** | Tenant fairness | Limiter count grows with tenants; a missing tenant must be denied, a harsh rule for a process-protection control |

### Chosen Option: **A — Per request type** (recommended, pending the maintainer)

### Rationale

- A wins when the goal is protecting a dependency; B wins when one tenant's slow requests must not consume the limit of others (tenant-scoped limiting is #740).

- **Class:** Dilemma

</details>

<details>
<summary><strong>7. Rejection error code</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New code `encina.concurrency.limit_exceeded`, ADR-062 category `Unavailable`, constant message, details `concurrency.limit` and `concurrency.in_flight`** | Available now; 503, not 429 (the server, not the caller, is saturated) | May duplicate the "bulkhead full" code the #2414 core will define |
| **B) Reuse the "bulkhead full" code of the #2414 core** | One code for both | The core defines no code yet |

### Chosen Option: **A — A new code now** (recommended, pending the maintainer)

### Rationale

- A wins until the core exists; B wins if the maintainer wants a single code when the core lands (the rename is mechanical, pre-1.0). The error is built with `EncinaErrors.Create(code, category: ErrorCategory.Unavailable, ...)`.

- **Class:** Dilemma

</details>

<details>
<summary><strong>8. Configuration front-ends</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[AdaptiveConcurrency]` (`LatencyTargetMs`, `ErrorThresholdPercent`, `MinConcurrency`, `MaxConcurrency`, `Algorithm`) plus `For<T>().AdaptiveConcurrency(...)`; precedence options > attribute > defaults; the global defaults carry `InitialConcurrency` 50, `MinConcurrency` 10, `MaxConcurrency` 200, `LatencyTargetMs` 100, `ErrorThresholdPercent` 5, `SmoothingFactor` 0.2 and `SampleWindow` 1 s (the values of the issue's samples), AIMD backoff 0.9 and the Gradient clamp 0.5 to 1.0 (Netflix `AIMDLimit` and `Gradient2Limit` defaults); the feature stays opt-in (nothing applies without the attribute or `For<T>()`); an invalid value fails at startup, never defaults silently** | Both front-ends feed one model; the defaults are cited | None |
| **B) Attribute only** | Smaller | Rejected by R3 (3c) |

### Chosen Option: **A — Both front-ends**

### Rationale

- **Class:** Settled
- **Settled by:** ADR-055 R3 (option 3a; precedence options for a type > attributes > global defaults)

</details>

<details>
<summary><strong>9. Telemetry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Observable gauges for the limit and the in-flight count, a rejected counter and a span event `concurrency.limit_changed`, named by the convention #728 settles (proposed `encina.resilience.concurrency.limit`, `.in_flight`, `.rejected`); tag `request.type`; tenant tag only with multi-tenancy** | R5 names adaptive concurrency state as Encina-owned; Polly measures none of it | Per-type series |
| **B) Logs only** | Smallest | No state visibility |

### Chosen Option: **A — Gauges, counter and span event** (recommended, pending the maintainer)

### Rationale

- ADR-055 R5 says Encina creates instruments for "adaptive concurrency state, shed load and partition count" and ADR-054 D3 fixes the rules, but not the names; #728 (plan on main) names an overlapping family (`encina.resilience.bulkhead.concurrency`, `shed_load`) and leaves its choices 1 (who holds the `Meter("Encina.Resilience")` instance) and 8 (names) pending. This plan creates no `Meter` of its own and takes that holder and convention. A wins when operators need the limit over time; B wins if logs suffice.
- **Class:** Dilemma

</details>

---

## Implementation Phases

Five pull requests, in order: **PR-1** Phase 1 (models, algorithms, window); **PR-2** Phase 2 and the attribute, options and validator of Phase 3 (limiter, strategy, the options the limiter reads); **PR-3** the registration half of Phase 3 and Phase 4 (builder extensions, cross-cutting rules); **PR-4** Phase 5 (observability; it modifies the PR-2 emitters); **PR-5** the load tests of Phase 6 and Phase 7 (docs). Each needs the `Encina.Resilience` project to exist; PR-2 to PR-5 also need the foundation compiler and R6.

### Phase 1: Core Models and Algorithms

<details>
<summary>Tasks</summary>

1. `src/Encina.Resilience/Concurrency/ConcurrencyMetrics.cs`: `public sealed record ConcurrencyMetrics(double AverageLatencyMs, double P99LatencyMs, double ErrorRate, int InFlightRequests, int QueuedRequests, TimeSpan SampleWindow)`; constructor guards (non-negative, `ErrorRate` 0 to 1).
2. `IConcurrencyAlgorithm.cs`: `int CalculateNewLimit(ConcurrencyMetrics metrics, int currentLimit)` and `ConcurrencyAlgorithm.cs` enum `{ Gradient, Aimd }`. Both built-in implementations are public and call `ArgumentNullException.ThrowIfNull(metrics)`; their guard tests live in `Encina.GuardTests` (a custom implementation never receives null from the limiter, which always passes a built record).
2b. `src/Encina.Resilience/Encina.Resilience.csproj`: `InternalsVisibleTo` for `Encina.UnitTests` and `Encina.GuardTests`, a foundation acceptance item, added here only if missing (counted below).
3. `GradientConcurrencyAlgorithm.cs`, `AimdConcurrencyAlgorithm.cs` (choice 4), pure and deterministic.
4. `RollingOutcomeWindow.cs`: ring of time buckets (by `TimeProvider.GetTimestamp`) with a fixed log-scale latency histogram per bucket (p99 from the merged histogram), success and failure counts; `Record(TimeSpan latency, bool failed)`, `Snapshot()`; thread-safe with `Interlocked`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina issue #139, plan docs/plans/adaptive-concurrency-implementation-plan-139.md; package src/Encina.Resilience (or a project the foundation created).
TASK: Create the Phase 1 types in src/Encina.Resilience/Concurrency/ with unit tests for every formula and boundary.
KEY RULES: pure functions for the algorithms; time only through TimeProvider (inject it into the window); no allocation per Record call; XML docs with <example>; guards on public constructors; PublicAPI through mechanical-fixer.
REFERENCE FILES: src/Encina.Polly/RateLimiting/AdaptiveRateLimiter.cs, src/Encina.Polly/Bulkhead/BulkheadManager.cs
```

</details>

### Phase 2: Default Implementation — Limiter and Strategy

<details>
<summary>Tasks</summary>

1. `IAdaptiveConcurrencyLimiter.cs`: `ConcurrencyPermit? TryAcquire()`, `int CurrentLimit`, `int InFlight`. `ConcurrencyPermit` (own file `ConcurrencyPermit.cs`) is a sealed class, not a struct, with a one-shot `Interlocked` flag: `Complete(PermitOutcome outcome)` releases the slot exactly once, so a second call (or a copy) cannot release two slots; `PermitOutcome` is `Success`, `Failure` or `Cancelled`. A `Success` or `Failure` records latency (from `TimeProvider.GetTimestamp`) into the window and triggers the inline evaluation of choice 5; `Cancelled` releases the slot and records no sample.
2. `AdaptiveConcurrencyLimiter.cs`: derives from `System.Threading.RateLimiting.RateLimiter` and implements `IAdaptiveConcurrencyLimiter`; the limit is an `int` changed by compare-and-swap; the algorithm comes from `IConcurrencyAlgorithm`; an algorithm exception keeps the previous limit (it is logged with 926 in PR-4, when the log class exists; PR-2 keeps the limit silently).
3. `AdaptiveConcurrencyStrategyFactory.cs`: a Polly `ResilienceStrategy<Either<EncinaError, TResponse>>` that acquires a permit (a `Left` with code `encina.concurrency.limit_exceeded` and category `Unavailable` when none), runs the callback, and completes the permit with the outcome: `Failure` for a transient `Left`, a non-cancellation exception or a `Left` of category `Timeout`; `Cancelled` for an `OperationCanceledException` or a `Left` of category `Cancelled` (a caller that left is never a drop: it does not shrink the limit or enter the window, ADR-062 rule R5); the exception is rethrown after the permit is completed in `finally`. It sits in the concurrency slot of the Admission pipeline, after the rate limiter (R2).
4. `AdaptiveConcurrencyLimiterRegistry.cs` (internal, public interface not needed): a singleton registry of limiters keyed by request type, created from the merged options; no scoped dependency; read by the gauges in PR-4.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 merged.
TASK: Implement the limiter, the strategy and the registry of Phase 2.
KEY RULES: rejection is a Left built with EncinaErrors.Create using the named category: ErrorCategory.Unavailable argument and a constant message; no exception for rejection; no scoped service in the singleton registry; permits released on every path including cancellation and exceptions (Complete in finally); never block a thread.
REFERENCE FILES: src/Encina.Polly/Behaviors/BulkheadPipelineBehavior.cs, src/Encina/Results/EncinaErrors.cs
```

</details>

### Phase 3: Configuration and DI

<details>
<summary>Tasks</summary>

1. (PR-2, because the limiter reads the options) `AdaptiveConcurrencyAttribute.cs` and `AdaptiveConcurrencyOptions.cs` (choice 8), `AdaptiveConcurrencyOptionsValidator.cs` (internal; ranges, `Min <= Initial <= Max`, a type with `[Bulkhead]` fails).
2. (PR-3) `AdaptiveConcurrencyBuilderExtensions.cs`: `For<T>().AdaptiveConcurrency(Action<AdaptiveConcurrencyOptions>)`; the foundation's startup validation registration joins the validator; registration test with `ValidateOnBuild` and `ValidateScopes`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Limiter and strategy exist.
TASK: Add the attribute, options, validator, builder extension and DI test.
KEY RULES: precedence options > attribute > defaults tested; opt-in only (nothing registered without the attribute or For<T>()); guards on public parameters; a nullable parameter accepting null gets a valid-result test.
REFERENCE FILES: src/Encina.Polly/Attributes/BulkheadAttribute.cs, tests/Encina.UnitTests/Polly/
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Resilience: the Admission slot and the exclusivity with `[Bulkhead]` (choice 2).
2. Multi-tenancy: tenant tag only with multi-tenancy; the limiter key is the request type, so no tenant-qualified key exists and the reserved `default` segment does not apply.
3. Consumers: #138 reads `RollingOutcomeWindow` through its utilization provider (sibling plan).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Strategy and registration exist.
TASK: Finish the exclusivity rule and the tenant-tag rule.
KEY RULES: tenant tag only when MultiTenancyMarker is registered; no EncinaError.Message in telemetry.
REFERENCE FILES: src/Encina/Abstractions/MultiTenancyMarker.cs
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. `EventIdRanges.cs`: `ResilienceAdaptiveConcurrency = (924, 935)`, its line in `src/Encina/PublicAPI.Unshipped.txt` (`mechanical-fixer`), and its name in the `["Encina.Resilience"]` entry of `AssemblyRanges` (create the entry if no earlier plan of the family did; an entry holds several ranges). Ids 924-935 are free in the repository and emitted only by `Encina.Resilience`.
2. `Diagnostics/AdaptiveConcurrencyLogMessages.cs`: 924 `LimitChanged` (Information: request type, old, new, algorithm), 925 `PermitRejected` (Debug: request type, limit), 926 `AlgorithmFailed` (Warning, exception through `ForLogging()`), 927 `LimiterCreated` (Debug); 928-935 spare.
3. `AdaptiveConcurrencyTelemetry.cs`: the gauges, counter and span event of choice 9.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: ADR-021, ADR-054, ADR-055 R5.
TASK: Register 924-935 first, then log messages and telemetry.
KEY RULES: ids packed from 924, emitted only by Encina.Resilience; error code only in logs; no activation switch.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 6: Testing

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/Resilience/Concurrency/`): each algorithm (healthy, slow, errors, clamp at Min and Max, smoothing); the window (percentile, rollover with `FakeTimeProvider`); limiter (acquire up to the limit, reject above, release on exception and cancellation, a cancellation (exception or `Cancelled` `Left`) neither shrinks the limit nor enters the window, a double `Complete` releases once, one adjustment per window, algorithm failure keeps the limit); strategy through the real compiler; validator rules.
2. Guard (`tests/Encina.GuardTests/Resilience/Concurrency/`): `ConcurrencyMetrics` constructor, window constructor, builder extension.
3. Contract (`tests/Encina.ContractTests/Resilience/ConcurrencyAlgorithmContractTests.cs`): both algorithms stay within `[1, int.MaxValue]` for any metrics and never throw.
4. Property (`tests/Encina.PropertyTests/Resilience/ConcurrencyProperties.cs`): FsCheck: the limit always stays in `[Min, Max]`; with no failures and low latency it never decreases.
5. Load (`tests/Encina.LoadTests/Resilience/AdaptiveConcurrency/`): a simulated downstream whose latency degrades under concurrency shows the limit converging below the saturation point and in-flight never above the limit (excluded from CI; run locally with `DOTNET_JitObjectStackAllocationConditionalEscape=0`). Integration and benchmark: justification files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Phases 1 to 5 done.
TASK: Write the Phase 6 tests.
KEY RULES: real code; FakeTimeProvider, no Thread.Sleep; load test deterministic enough to assert bounds not exact values; Shouldly via Encina.Testing.Shouldly.
REFERENCE FILES: tests/Encina.UnitTests/Polly/, tests/Encina.LoadTests/
```

</details>

### Phase 7: Documentation and Finalization

<details>
<summary>Tasks</summary>

1. README of `Encina.Resilience` and `docs/features/resilience-adaptive-concurrency.md` (`docs-writer`), with the algorithm descriptions and the interaction with `[Bulkhead]`.
2. `changelog.d/139-adaptive-concurrency.added.md`, `PublicAPI.Unshipped.txt`, manifest (`mechanical-fixer`).
3. Verification: build 0 warnings; tests filtered to `Concurrency`; EventId architecture tests; `--check-justifications`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Phases 1 to 6 green.
TASK: Documentation and verification.
KEY RULES: never edit CHANGELOG.md; docs by docs-writer; metadata by mechanical-fixer; no hand-typed coverage figures.
REFERENCE FILES: changelog.d/README.md, .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Relevance |
|--------|-----------|
| Netflix `concurrency-limits` (`Gradient2Limit`, `AIMDLimit`, `VegasLimit`) | Algorithms and smoothing |
| TCP AIMD and Vegas | Origin of the algorithms |
| `System.Threading.RateLimiting.RateLimiter` | Base class that is the seam with the #2414 core |
| ADR-055 R2, R3, R5 | Form, front-ends, instruments |
| ADR-062 | New code `encina.concurrency.limit_exceeded`, category `Unavailable` |
| ADR-054 D3, D4 | Names, tenant tag |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `BulkheadManager` and attribute | `src/Encina.Polly/Bulkhead/`, `src/Encina.Polly/Attributes/BulkheadAttribute.cs` | Replaced by R2; the attribute shape is reused |
| `IErrorClassifier` | `src/Encina/Recoverability/` | Failure signal |
| `EncinaErrors.Create`, `ErrorCategory` | `src/Encina/Results/EncinaErrors.cs`, `src/Encina/Errors/ErrorCategory.cs` | Rejection error |
| `PipelineStage.Admission` | `src/Encina/Pipeline/Ordering/PipelineStage.cs` | Slot |
| Load test project | `tests/Encina.LoadTests/` | Load tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Resilience` (`ResilienceAdaptiveConcurrency`) | 924-935 | From the block 900-949; 924-927 used, 928-935 spare |

### Estimated File Count

| PR | Files | Content |
|----|-------|---------|
| PR-1 | 14 | 6 production (metrics, algorithm interface, algorithm enum, Gradient, AIMD, window), `Encina.Resilience.csproj` (`InternalsVisibleTo`, only if missing), `PublicAPI.Unshipped.txt`, 5 tests (algorithms, window, guard, contract, algorithm property), manifest |
| PR-2 | 15 | 8 production (attribute, options, validator, limiter interface, `ConcurrencyPermit`, limiter, limiter registry, strategy factory), `PublicAPI.Unshipped.txt`, 5 tests (limiter, strategy, options and validator, guard, limiter property), manifest. The limiter and the strategy log nothing in this PR |
| PR-3 | 7 | builder extensions, the foundation's startup-validation registration (a modified file of `Encina.Resilience`), `PublicAPI.Unshipped.txt`, 2 tests (registration, guard file modified), manifest, changelog fragment |
| PR-4 | 13 | log messages, telemetry, the limiter, the strategy factory and the registry (all three modified to log and expose the gauges), `EventIdRanges.cs`, core `PublicAPI.Unshipped.txt`, `EncinaEventIdAllocationTests.cs`, telemetry test, the limiter tests (modified: they assert 924 `LimitChanged` and 926 `AlgorithmFailed`), the strategy tests (modified: they assert 925 `PermitRejected` and, through the registry the strategy creates, 927 `LimiterCreated`; the registry has no test file of its own), Resilience `PublicAPI.Unshipped.txt`, manifest. PR-4 is not at the limit |
| PR-5 | 6 | 2 load test files, 2 justification `.md`, README, feature page |

Each PR is within 15 files and 1,500 lines (PR-1 about 1,100, PR-2 about 1,400); PR-2 sits at the file limit and defines the options type the limiter reads, so it builds alone (the attribute, options and validator moved from the earlier PR-3 into PR-2). The property test of each file is in the PR that adds the file, so its property target is reached there. The manifest of each PR carries the rows of the files it adds. Under option C of choice 1 the project creation adds files to PR-1 and the split is recounted. No interface of an existing type changes.

Per-file targets (new files; baseline: none exists on `origin/main`):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `ConcurrencyMetrics.cs` | 95 | 100 | 0 | 0 | 0 | Guards and values reached in PR-1 |
| `IConcurrencyAlgorithm.cs`, `ConcurrencyAlgorithm.cs` | 0 | 0 | 0 | 0 | 0 | Interface and enum, no executable code |
| `GradientConcurrencyAlgorithm.cs`, `AimdConcurrencyAlgorithm.cs` | 95 | 100 | 90 | 90 | 0 | Pure functions; the null-metrics guard test, the contract test and the FsCheck test of PR-1 execute them |
| `RollingOutcomeWindow.cs` | 90 | 100 | 0 | 0 | 0 | Bucket rollover driven by a fake clock; guarded constructor |
| `AdaptiveConcurrencyLimiter.cs` | 90 | 100 | 0 | 80 | 0 | Acquire, release, adjust paths in unit tests; property keeps bounds |
| `AdaptiveConcurrencyStrategyFactory.cs` | 90 | 0 | 0 | 0 | 0 | Run through the real compiler, including the cancellation outcome; internal |
| `IAdaptiveConcurrencyLimiter.cs` | 0 | 0 | 0 | 0 | 0 | Interface, no executable code |
| `ConcurrencyPermit.cs` | 95 | 0 | 0 | 0 | 0 | One-shot release: a double `Complete` releases once, `Cancelled` records no sample (PR-2 limiter tests); no validated parameters |
| `AdaptiveConcurrencyLimiterRegistry.cs` | 90 | 0 | 0 | 0 | 0 | One limiter per type, created from the options; internal, reached through the `InternalsVisibleTo` of Phase 1 |
| `AdaptiveConcurrencyAttribute.cs`, `AdaptiveConcurrencyOptions.cs` | 95 | 0 | 0 | 0 | 0 | Properties and merge, no parameters |
| `AdaptiveConcurrencyOptionsValidator.cs` | 95 | 0 | 0 | 0 | 0 | One test per rule; internal |
| `AdaptiveConcurrencyBuilderExtensions.cs` | 90 | 100 | 0 | 0 | 0 | Guarded public method |
| Telemetry and log messages | 85 | 0 | 0 | 0 | 0 | Listener-based tests |

CRAP of each method is at most 10: the evaluation is split into small methods (window snapshot, algorithm call, compare-and-swap).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #139: adaptive concurrency in Encina.Resilience (ADR-055). AGENTS.md rules apply. Every PR needs the Encina.Resilience project; PR-2 to PR-5 also need the foundation compiler and R6.

IMPLEMENTATION OVERVIEW: (1) metrics, algorithms (Gradient, AIMD), rolling window; (2) limiter on RateLimiter, strategy in the Admission concurrency slot; (3) attribute, options, validator, builder extension; (4) exclusivity with [Bulkhead], tenant tag rule; (5) EventIds 924-935 and gauges; (6) unit, guard, contract, property, load tests; (7) docs and fragments.

KEY PATTERNS: Netflix formulas with smoothing and clamp; one adjustment per window, inline, TimeProvider; per request type; rejection Left encina.concurrency.limit_exceeded with ErrorCategory.Unavailable; permits released in finally; specialists for PublicAPI, changelog, manifest, docs.

REFERENCE FILES: src/Encina.Polly/Bulkhead/BulkheadManager.cs, src/Encina.Polly/Attributes/BulkheadAttribute.cs, src/Encina/Results/EncinaErrors.cs, docs/architecture/adr/055-resilience-family.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Operates on live concurrency |
| 2 | OpenTelemetry | ✅ | Gauges, counter, span event (ADR-055 R5); tenant tag only with multi-tenancy |
| 3 | Structured Logging | ✅ | EventIds 924-927 |
| 4 | Health Checks | ❌ | The limit is exposed by gauges; a limit change is not a degraded dependency |
| 5 | Validation | ✅ | Options and attribute validated at startup |
| 6 | Resilience | ✅ | A strategy of the Admission pipeline |
| 7 | Distributed Locks | ❌ | Per-process limit by design; a distributed limit is #704's concern |
| 8 | Transactions | ❌ | Wraps operations transparently |
| 9 | Idempotency | ❌ | Caller responsibility; a rejected request never ran |
| 10 | Multi-Tenancy | ✅ | Tenant tag only with multi-tenancy; per-type limiter, no tenant key, so the `default` segment does not apply; per-tenant limiting is #740 |
| 11 | Module Isolation | ❌ | Module-independent |
| 12 | Audit Trail | ❌ | Infrastructure concern |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|--------------|-------|--------|
| Foundation and R6 (issue files `resilience-foundation-encina-resilience.md`, `resilience-r6-pipeline-token-and-deadline.md`) | Not on main | Block every PR (Dilemma 1, #136): even PR-1's files live in the `Encina.Resilience` project, which does not exist on main |
| #2414 core | Open, not planned | Adopts the limiter as a partition (choice 2) |
| #138 | Sibling plan | Consumes `RollingOutcomeWindow` after PR-1 |

---

## Next Steps

1. Commit this plan, open its PR and link it from #139.
2. When #139 is picked up, ask the maintainer about the Dilemmas (choices 1, 2, 3, 5, 6, 7 and 9).
3. Brief a worker for PR-1 once the `Encina.Resilience` project exists, then PR-2 to PR-5 once the foundation and R6 are merged.
