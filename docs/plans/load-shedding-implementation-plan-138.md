# Implementation Plan: Priority-based load shedding in `Encina.Resilience`

> **Issue**: [#138](https://github.com/dlrivada/Encina/issues/138)
> **Type**: Feature
> **Complexity**: High (7 phases, no database providers, 5 pull requests)
> **Estimated Scope**: ~800 lines of production code + ~1,300 lines of tests (about 2,200 changed lines in total, five pull requests)

---

## Summary

When the process is overloaded, drop the least important requests first so the important ones keep their capacity: a request has a priority, a utilization provider measures CPU, memory, p99 latency and error rate, each priority has a threshold, and above it requests are rejected with a probability that grows with the excess (the Netflix and Uber pattern). The comment of 2026-10-23 on #138 moves it into the 1.0 scope; [ADR-055](../architecture/adr/055-resilience-family.md) decided that load shedding lives in `Encina.Resilience` and that there is no `Encina.LoadShedding` package (R1, which overrides the `new-package` label and the "Other: `Encina.LoadShedding` (new package)" line of the issue).

What exists today (read in `origin/main` at `e31de98cb`):

- Nothing: `git grep -E "LoadShed|IPrioritizedRequest|IUtilizationProvider|RequestPriority" -- src` has no match. `Encina.Resilience`, the per-type compiler (R2) and R6 are not on main.
- The plan of #123 (on main) proposes `MessagePriority` and `IPrioritizedMessage` in `Encina.Messaging` (five levels, higher is more urgent) and notes that `IPrioritizedRequest` and `[Priority]` do not exist in the tree. The issue's own enum is `Critical = 0 ... Bulk = 3`, where a lower number is more important. Design Choice 4 settles the relation.
- `AdaptiveRateLimiter` (`src/Encina.Polly/RateLimiting/AdaptiveRateLimiter.cs`) throttles by request rate after failures; it does not look at the process.
- `IEncinaHealthCheck` lives in `Encina.Messaging` (`src/Encina.Messaging/Health/`).

- **Affected packages**: `Encina.Resilience` (planned), `Encina` core (`EventIdRanges.cs`). The issue also ticks `Encina` core and `Encina.AspNetCore`; no code there (hosts map the category).
- **Provider category**: Resilience; no database, caching, transport, lock or validation matrix applies.
- **Prerequisites**: foundation and R6 (tied to Dilemma 1 of the #136 plan).

### Shared Pieces and Ownership

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `Encina.Resilience`, compiler, options front-ends | foundation issue (`resilience-foundation-encina-resilience.md`) | all |
| `RequestPriority`, `IPrioritizedRequest`, `IUtilizationProvider`, `ILoadSheddingManager`, `UtilizationMetric` | this plan (#138) | #131 and #144 (sibling plans, by number) |
| Latency percentile and error rate window | #139 (`RollingOutcomeWindow`, plan not on main) or this plan, per Design Choice 8 | this plan |
| Health check placement rule | #728 plan, choice 3 (on main, pending) | this plan |
| EventIds 912-923 (`ResilienceLoadShedding`) | this plan | none |

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

- A follows the spike report order (overload strategies are step 6, after the foundation); B wins if shedding must ship first. Asked once for the family (#136 choice 1). Every PR needs the `Encina.Resilience` project to exist: even PR-1's pure model files live in it, so PR-1 is not independent of the foundation (under option C of this choice, creating the project and the test `ProjectReference` edits adds files to PR-1).

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Package: `Encina.LoadShedding` or `Encina.Resilience`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Resilience`** | One package for the family | The issue says new package |
| **B) A new `Encina.LoadShedding` package** | Matches the issue | R1 rejected one package per capability |

### Chosen Option: **A — `Encina.Resilience`**

### Rationale

- **Class:** Settled
- **Settled by:** ADR-055 R1 ("An issue that planned its own package (#138 `Encina.LoadShedding`) puts its code here") and its implications table

</details>

<details>
<summary><strong>3. Position in the Admission pipeline</strong></summary>

R2 lists the rate limiter and the bulkhead in the Admission behavior and does not place shedding.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) First strategy of the Admission pipeline, before the rate limiter** | The cheapest rejection runs first; a shed request consumes no rate-limit permit or concurrency slot | A request shed here is never counted by the rate limiter |
| **B) After the rate limiter and before the concurrency limit** | The rate limiter sees all traffic | Shed requests already took a rate-limit token |

### Chosen Option: **A — First in Admission** (recommended, pending the maintainer)

### Rationale

- A wins when shedding must protect the limiters themselves; B wins if rate-limit statistics must include every request.

- **Class:** Dilemma

</details>

<details>
<summary><strong>4. The priority type: its own `RequestPriority` or the `MessagePriority` of #123</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `RequestPriority { Critical = 0, Degraded = 1, BestEffort = 2, Bulk = 3 }` and `IPrioritizedRequest` in `Encina.Resilience`, with the ordering exposed by a documented comparison, never by the raw number** | Matches the issue; `Encina.Resilience` does not depend on `Encina.Messaging` | Two priority enums with opposite numeric orientation (here lower is more important; #123 higher is more urgent) |
| **B) Reuse `MessagePriority` of #123 (five levels)** | One concept | A resilience package would reference Messaging (ADR-055 keeps `Encina.Resilience` free of it; Messaging requires Resilience, R4); the levels carry other meaning (`Background`..`Critical`) |

### Chosen Option: **A — Its own `RequestPriority`** (recommended, pending the maintainer)

### Rationale

- A wins on package independence; B wins if the maintainer wants a single priority type across messaging and requests, in which case the type moves to core `Encina`. The XML docs and a unit test pin the orientation.

- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Priority source and the default for a request that declares none</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Precedence: the instance's `IPrioritizedRequest.Priority` (runtime data), then the per-type option `For<T>().LoadShedding(o => o.Priority = ...)`, then `DefaultPriority` = `Degraded`** | Dynamic priority per instance; configurable for types the user does not own (R3); the middle level never starves unclassified work and never makes it untouchable | Unclassified traffic is shed from the third threshold |
| **B) `DefaultPriority` = `Critical`** | Adopting shedding sheds nothing until types are classified | Unclassified traffic is never shed, so shedding does little until everything is classified |
| **C) `DefaultPriority` = `BestEffort`** | Sheds early | Surprises applications that adopt shedding without classifying |

### Chosen Option: **A — Instance, then option, then `Degraded`** (recommended, pending the maintainer)

### Rationale

- A wins when most traffic is ordinary; B wins for a gradual rollout; C wins where unclassified traffic is mostly background. The `EnablePrioritization` switch of the issue's sample is not created (without priorities, the default priority applies to everything): a dead option is not added (AGENTS.md section 1).
- Trust: the instance priority is read from the request object, so a request bound from client input could carry `Critical` and avoid shedding. The rule is that `IPrioritizedRequest.Priority` must be computed by the application from trusted data (the request type, the authenticated identity), never copied from a client field; the documentation says so, and a per-type option can pin a type's priority and take precedence over an instance value for types that bind client input (`For<T>().LoadShedding(o => o.PinPriority = true)`). A is therefore the recommendation only when the application owns its priorities; where requests deserialize a priority from the wire, the pinned option is the safe variant.

- **Class:** Dilemma

</details>

<details>
<summary><strong>6. How the signals combine into one utilization</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each selected signal is normalized to 0-100 (CPU and memory as a percentage, latency as `p99 / LatencyTarget`, error rate as a percentage) and the utilization is the maximum** | Any saturated signal sheds; explains itself in one number | One noisy signal can shed |
| **B) Weighted average** | Smooths noise | A saturated CPU is hidden by quiet memory |

### Chosen Option: **A — Maximum of the selected signals** (recommended, pending the maintainer)

### Rationale

- A wins for protection; B wins when signals are individually noisy. `UtilizationMetric` is a flags enum (`Cpu`, `Memory`, `Latency`, `ErrorRate`) that selects the signals (the issue's `CpuAndLatency` is `Cpu | Latency`).

- **Class:** Dilemma

</details>

<details>
<summary><strong>7. How CPU and memory are sampled</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Lazy: a read computes `Process.TotalProcessorTime` delta over elapsed `TimeProvider` time divided by `Environment.ProcessorCount` (container-aware) at most once per `SampleInterval`; memory from `GC.GetGCMemoryInfo()`** | No timer and no background loop that can throw; deterministic with `FakeTimeProvider` | Values are as fresh as the last request or the last metric collection |
| **B) A `PeriodicTimer` hosted sampler** | Fresh gauges while idle | A background loop that must never let an exception escape (AGENTS.md section 3), plus a hosted service lifecycle |

### Chosen Option: **A — Lazy sampling** (recommended, pending the maintainer)

### Rationale

- A wins on simplicity and testability; B wins if utilization must be exported continuously at low traffic.

- **Class:** Dilemma

</details>

<details>
<summary><strong>8. Source of p99 latency and error rate</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `RollingOutcomeWindow` that #139 plans (PR-1 of that plan)** | One implementation; a shed decision and a concurrency limit see the same numbers | Depends on a plan that is not on main; ordering #139 before #138 |
| **B) A window of its own in this plan** | Independent | Two near-identical classes |

### Chosen Option: **A — Reuse the window of #139** (recommended, pending the maintainer)

### Rationale

- A wins when the plans are implemented in the proposed order (#139 before #138); B wins if #138 must ship first, in which case #139 reuses this window and ownership flips.

- **Class:** Dilemma

</details>

<details>
<summary><strong>9. When a utilization signal cannot be read</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Treat the failed signal as absent: admit, log once per transition and count `utilization.unavailable`** | A broken sensor never turns into an outage; shedding is an availability control, not a compliance or security gate (AGENTS.md section 3 fail-closed rule does not apply) | Overload protection is off while the sensor is down |
| **B) Treat it as full utilization: shed every priority at or below a configured floor** | Protection stays on | A sensor failure becomes self-inflicted rejection |

### Chosen Option: **A — Admit, log, count** (recommended, pending the maintainer)

### Rationale

- A wins for availability; B wins where overload would damage a dependency that cannot be repaired.

- **Class:** Dilemma

</details>

<details>
<summary><strong>10. The rejection</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `Left` with a new code `encina.loadshedding.rejected`, `ErrorCategory.Unavailable` (503 over HTTP, `UNAVAILABLE` over gRPC), constant message, details `loadshedding.priority` and, when `RetryAfterHint` is configured, `EncinaErrorMetadataKeys.RetryAfter`** | Hosts map the category without parsing the code; consistent with the #139 rejection | Public names ADR-055 does not fix |
| **B) A `Left` with `ErrorCategory.RateLimited` (429)** | Reuses the rate-limit meaning | The server is overloaded, not the caller over a quota; 429 invites a client to slow down only itself |
| **C) The rejection codes and categories wait for #3424** | One decision for every rejection (bulkhead, timeout, breaker, shed) | #3424 is an open bug about other codes; shedding would block on it |

### Chosen Option: **A — New code with category `Unavailable`** (recommended, pending the maintainer)

### Rationale

- That a rejection is a coded `Left`, never an exception, is Settled by AGENTS.md section 3 (ROP) and the ADR-055 settled point "Every rejection is a coded `Left` ... shed load; delays travel as metadata"; the code name, category and detail keys are what this choice decides. ADR-062's table has no shed-load row. The error is built with `EncinaErrors.Create(code, category: ErrorCategory.Unavailable, message, details: ...)` and the message is a constant. The rejected request has not run, so a caller may retry it.
- A wins when shedding must be distinguishable and map to 503; B wins if shed requests should look like rate limits to clients; C wins if the maintainer wants one naming decision for all rejections first.
- **Class:** Dilemma

</details>

<details>
<summary><strong>11. The shedding algorithm</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The issue's formula: probability `min(1, (u - threshold) / (100 - threshold))` above the threshold; with a threshold of 100 the probability is 1 at `u >= 100`; the random source is injectable (`Random.Shared` by default) and `EnableGradualShedding = false` rejects with probability 1 above the threshold** | Matches the issue and the acceptance criteria; testable with a fixed source | None |
| **B) A deterministic hash of the request id** | Reproducible | Not the specified algorithm |

### Chosen Option: **A — The issue's gradual algorithm**

### Rationale

- **Class:** Settled
- **Settled by:** #138 "Shedding Algorithm" section and acceptance criterion "Gradual (probabilistic) shedding algorithm"

</details>

<details>
<summary><strong>12. Health check</strong></summary>

The issue marks Health Checks as included ("report current load/shedding state").

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Follow the placement rule chosen once in the #728 plan (choice 3) for the throttled-state check; this plan adds `LoadSheddingHealthCheck`, reporting `Degraded` while any priority is being shed** | One rule for the family | Waits for that answer |
| **B) Plain `IHealthCheck` in `Encina.Resilience`** | No reference to `Encina.Messaging` | May differ from the convention |

### Chosen Option: **A — Same placement rule as #728 choice 3** (recommended, pending the maintainer)

### Rationale

- The question is owned by #728 and answered once; A wins if the maintainer answers there for `IEncinaHealthCheck`, B if for the plain interface. The check never fails the application: shedding is expected behavior, so the state is `Degraded`, not `Unhealthy`.

- **Class:** Dilemma

</details>

<details>
<summary><strong>13. Telemetry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A utilization gauge, a shed counter (tags `request.priority`, `request.type`) and an admitted counter, plus a span event `loadshedding.rejected`, named by the convention #728 settles (proposed `encina.resilience.loadshedding.utilization`, `.shed`, `.admitted`); tenant tag only with multi-tenancy** | R5 names shed load as Encina-owned | Per-priority series (4) and per-type |
| **B) Logs only** | Smallest | No metrics |

### Chosen Option: **A — Gauge and counters** (recommended, pending the maintainer)

### Rationale

- ADR-055 R5 says Encina creates instruments for "shed load" and ADR-054 D3 fixes the rules, not the names. #728 (plan on main) already proposes a shed-load counter `encina.resilience.shed_load` (its choice 8, pending) and leaves its choice 1 (who holds the `Meter("Encina.Resilience")` instance) pending: the two names collide in meaning, so this plan takes the holder and the name that #728's answers settle, and creates no `Meter` of its own. A wins when operators need shed counts per priority; B wins if logs suffice.
- **Class:** Dilemma

</details>

<details>
<summary><strong>14. Built-in utilization providers</strong></summary>

The acceptance criterion reads "`IUtilizationProvider` abstraction with built-in implementations" (plural).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One built-in `ProcessUtilizationProvider` (CPU, memory, and latency and error rate from the window); applications supply their own `IUtilizationProvider` and it wins in any registration order** | Smallest; covers the pattern | One implementation where the criterion says several |
| **B) Also a provider over `Microsoft.Extensions.Diagnostics.ResourceMonitoring`** | Container-aware CPU and memory from a maintained source | A new package dependency not pinned in `Directory.Packages.props` (no `ResourceMonitoring` entry) |

### Chosen Option: **A — One built-in provider** (recommended, pending the maintainer)

### Rationale

- A wins on dependencies; B wins if cgroup accuracy matters and the dependency is accepted. The omission of a second built-in implementation is recorded here.
- **Class:** Dilemma

</details>

<details>
<summary><strong>15. Defaults of the options</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Thresholds` Bulk 60, BestEffort 70, Degraded 80, Critical 90; `Metric` = `Cpu | Latency`; `SampleInterval` 1 s; `EnableGradualShedding` true; `LatencyTarget` has no default and a configuration that selects `Latency` without one fails at startup; `RetryAfterHint` null** | The first three are the values of the issue samples (`CpuAndLatency`, thresholds), the interval is the issue's #139 sample cadence (1000 ms); invalid or missing input fails at startup instead of defaulting silently; opt-in | None |
| **B) Silent defaults for every value** | Easiest to adopt | A missing latency target would hide a misconfiguration |

### Chosen Option: **A — Cited defaults, startup failure for the rest** (recommended, pending the maintainer)

### Rationale

- The feature stays opt-in: nothing applies without `LoadShedding(...)`.
- The defaults are written for the recommended options of choices 4 (the four `RequestPriority` names), 5 (`Degraded`, `PinPriority`), 6 (the `Cpu | Latency` flags, normalization by `LatencyTarget`) and 7 (a 1 s lazy sampling interval). Under another answer they follow it: five `MessagePriority` levels need five thresholds, a timer sampler keeps the interval, a weighted average needs weights. The values taken from the issue's sample (`60, 70, 80, 90`, `CpuAndLatency`) and the opt-in rule hold under every option.
- **Class:** Dilemma

</details>

---

## Implementation Phases

Five pull requests, in order: **PR-1** Phase 1 (model and calculator); **PR-2** Phase 2 (providers, manager, strategy; needs `RollingOutcomeWindow` from #139 PR-1 merged first, choice 8; they log and record nothing yet); **PR-3** Phases 3 and 4 (configuration, health check); **PR-4** Phase 5 (observability; it modifies the three PR-2 emitters and their tests); **PR-5** the load tests of Phase 6 and Phase 7 (docs). Each needs the `Encina.Resilience` project to exist; PR-2 to PR-5 also need the foundation compiler and R6.

### Phase 1: Core Models and Calculator

<details>
<summary>Tasks</summary>

1. `src/Encina.Resilience/LoadShedding/RequestPriority.cs` (choice 4) with a `Compare` helper that orders by importance; `IPrioritizedRequest.cs` (`RequestPriority Priority { get; }`); `UtilizationMetric.cs` (`[Flags] enum`).
2. `LoadSheddingOptions.cs`: `Thresholds` (default `Bulk 60, BestEffort 70, Degraded 80, Critical 90`), `UtilizationMetric Metric` (default `Cpu | Latency`), `RequestPriority DefaultPriority = Degraded`, `bool EnableGradualShedding = true`, `TimeSpan? RetryAfterHint`, `TimeSpan? LatencyTarget` (no default; required when `Latency` is selected, failing at startup), `TimeSpan SampleInterval` (1 s), `bool PinPriority` per type (choice 5); the defaults are choice 15; `LoadSheddingOptionsValidator.cs` (internal: thresholds in 1 to 100 and not decreasing with importance).
3. `IUtilizationProvider.cs` (`double GetCpuUtilization()`, `GetMemoryUtilization()`, `GetLatencyP99()`, `GetErrorRate()` as in the issue, documented in 0-1 or milliseconds), `ILoadSheddingManager.cs` (`bool ShouldShed(RequestPriority priority)`, the probabilistic decision of one request; `bool IsShedding(RequestPriority priority)`, a state read of whether that priority's utilization is at or above its threshold, which the health check of PR-3 reads; and `double Utilization`). `IsShedding` is implemented by the manager in PR-2 and tested there (above, at and below the threshold, and consistency with the transition flag).
4. `SheddingCalculator.cs`: the pure rejection probability (choice 11).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina issue #138, plan docs/plans/load-shedding-implementation-plan-138.md; code goes in Encina.Resilience (ADR-055 R1), never a new package.
TASK: Create the Phase 1 types and unit tests for the calculator (thresholds, boundaries, threshold 100, gradual and hard modes) and the validator.
KEY RULES: pure functions; no EnablePrioritization option; orientation of RequestPriority documented and tested; XML docs with <example>; guards on public constructors; PublicAPI through mechanical-fixer.
REFERENCE FILES: docs/plans/message-priority-implementation-plan-123.md, src/Encina.Polly/RateLimiting/AdaptiveRateLimiter.cs
```

</details>

### Phase 2: Default Implementations — Utilization, Manager, Strategy

<details>
<summary>Tasks</summary>

1. `ProcessUtilizationProvider.cs` (choice 7), constructed with `TimeProvider` and the latency window (choice 8); a failed signal is reported through the manager (choice 9). Thread-safety: the provider is a singleton read concurrently; the "at most once per `SampleInterval`" gate is a compare-and-swap on the last-sample timestamp (`Interlocked.CompareExchange`), so two concurrent reads cannot both sample.
2. `LoadSheddingManager.cs`: singleton; combines the selected signals by maximum (choice 6), draws from the injectable random source and keeps the per-priority shedding state for the transition logs. Thread-safety: the per-priority state is an `int` flag changed with `Interlocked.Exchange`, so exactly one caller sees (and logs, in PR-4) a transition; the random source contract is `Func<double>` returning a value in `[0, 1)` that must be thread-safe (the default wraps `Random.Shared`); a concurrent test drives the manager from many tasks.
3. `LoadSheddingStrategyFactory.cs`: a Polly `ResilienceStrategy<Either<EncinaError, TResponse>>` first in the Admission pipeline (choice 3): resolves the priority (choice 5), asks the manager, returns the rejection `Left` (choice 10) or runs the callback and records latency and failure into the window. A cancellation (an `OperationCanceledException` or a `Left` of category `Cancelled`) records nothing: it is neither a failure nor a latency sample (ADR-062 rule R5), so the error rate that drives shedding is not raised by callers that left; a unit test pins it.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 merged.
TASK: Implement the provider, manager and strategy.
KEY RULES: the rejection is EncinaErrors.Create with the named category: ErrorCategory.Unavailable argument and a constant message; time only through TimeProvider; the manager and provider are singletons and hold no scoped service; no exception escapes a signal read.
REFERENCE FILES: src/Encina/Results/EncinaErrors.cs, src/Encina/Errors/EncinaErrorMetadataKeys.cs
```

</details>

### Phase 3: Configuration and DI

<details>
<summary>Tasks</summary>

1. `LoadSheddingBuilderExtensions.cs`: `AddEncinaResilience(o => o.LoadShedding(...))` (global) and `For<T>().LoadShedding(...)` (per-type priority); registers `IUtilizationProvider`, `ILoadSheddingManager` and the validator with `TryAdd*` so an application's own provider wins in any registration order.
2. Registration test with `ValidateOnBuild` and `ValidateScopes`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Strategy and manager exist.
TASK: Add the extensions and the DI test.
KEY RULES: opt-in only (nothing registered without the call); an application-registered IUtilizationProvider wins in any order; guards on public parameters; every option type resolved is registered.
REFERENCE FILES: src/Encina.Polly/ServiceCollectionExtensions.cs
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Health check (choice 12): `LoadSheddingHealthCheck` (reads `ILoadSheddingManager.IsShedding` for each priority and reports `Degraded` when any is true), tags and name constants per AGENTS.md; scoped resolution is unnecessary (the manager is a singleton).
2. Multi-tenancy: tenant tag only with multi-tenancy; no tenant-qualified key exists (utilization is process-wide), so the reserved `default` segment does not apply; tenant-scoped thresholds are the issue file `resilience-loadshedding-tenant-thresholds.md`.
3. Idempotency: a shed request has not run, so retry is safe.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Core pieces and DI exist.
TASK: Add the health check and finish the tenant tag rule.
KEY RULES: health state Degraded while shedding, never Unhealthy; no EncinaError.Message in results; placement per the answered #728 choice 3.
REFERENCE FILES: src/Encina.Messaging/Health/, src/Encina/Abstractions/MultiTenancyMarker.cs
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. `EventIdRanges.cs`: `ResilienceLoadShedding = (912, 923)`, its line in `src/Encina/PublicAPI.Unshipped.txt` (`mechanical-fixer`), and its name in the `["Encina.Resilience"]` entry of `AssemblyRanges` (create the entry if no earlier plan of the family did; an entry holds several ranges). Ids 912-923 are free in the repository and emitted only by `Encina.Resilience`.
2. `Diagnostics/LoadSheddingLogMessages.cs`: 912 `SheddingStarted` (Information, at the transition of a priority), 913 `SheddingStopped` (Information), 914 `RequestShed` (Debug: type, priority, utilization), 915 `UtilizationUnavailable` (Warning: signal name, exception through `ForLogging()`), 916 `PriorityDefaulted` (Debug); 917-923 spare.
3. `LoadSheddingTelemetry.cs`: instruments of choice 13.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: ADR-021, ADR-054, ADR-055 R5.
TASK: Register 912-923 first, then log messages and telemetry.
KEY RULES: ids packed from 912, emitted only by Encina.Resilience; transitions logged, not every shed request, at Information; no error message in logs; no activation switch.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 6: Testing

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/Resilience/LoadShedding/`): calculator; manager with a fixed random source and a fake provider (below threshold admits, above sheds, gradual probability, hard mode); provider with `FakeTimeProvider` and a failing signal; strategy through the real compiler (priority precedence, default, rejection `Left` with category `Unavailable`, `RetryAfter` detail); validator.
2. Guard (`tests/Encina.GuardTests/Resilience/LoadShedding/`): public constructors and extension methods; nullable `RetryAfterHint` null accepted (valid result).
3. Contract (`tests/Encina.ContractTests/Resilience/UtilizationProviderContractTests.cs`): real providers return values in range and never throw.
4. Property (`tests/Encina.PropertyTests/Resilience/SheddingProperties.cs`): probability is monotone in utilization and in priority importance (a more important priority never has a higher probability).
5. Load (`tests/Encina.LoadTests/Resilience/LoadShedding/`): a synthetic overload shows `Bulk` shed first and `Critical` admitted until its threshold (excluded from CI; local run with `DOTNET_JitObjectStackAllocationConditionalEscape=0`). Integration and benchmark: justification files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Phases 1 to 5 done.
TASK: Write the Phase 6 tests.
KEY RULES: real code; FakeTimeProvider, no Thread.Sleep; injectable random for determinism; Shouldly via Encina.Testing.Shouldly.
REFERENCE FILES: tests/Encina.UnitTests/Polly/, tests/Encina.LoadTests/
```

</details>

### Phase 7: Documentation and Finalization

<details>
<summary>Tasks</summary>

1. README of `Encina.Resilience` and `docs/features/resilience-load-shedding.md` (`docs-writer`): priorities, thresholds, signals, the orientation of `RequestPriority`.
2. `changelog.d/138-load-shedding.added.md`, `PublicAPI.Unshipped.txt`, manifest (`mechanical-fixer`).
3. Verification: build 0 warnings; tests filtered to `LoadShedding`; EventId architecture tests; `--check-justifications`.

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
| Netflix service-level prioritized load shedding (QCon SF 2025), Uber Cinnamon, Google SRE "Handling Overload" | The pattern |
| ADR-055 R1, R2, R5 | Package, form, instruments |
| ADR-062 | Category `Unavailable` for the new code `encina.loadshedding.rejected` |
| ADR-054 D3, D4 | Names, tenant tag |
| RFC 9110 section 10.2.3 | `Retry-After` on 503 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `EncinaErrors.Create`, `ErrorCategory`, `EncinaErrorMetadataKeys.RetryAfter` | `src/Encina/Results/EncinaErrors.cs`, `src/Encina/Errors/` | Rejection |
| `PipelineStage.Admission` | `src/Encina/Pipeline/Ordering/PipelineStage.cs` | Slot |
| `IEncinaHealthCheck` | `src/Encina.Messaging/Health/` | Health check (choice 12) |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | Tenant tag |
| Load test project | `tests/Encina.LoadTests/` | Load tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Resilience` (`ResilienceLoadShedding`) | 912-923 | From the block 900-949; 912-916 used, 917-923 spare |

### Estimated File Count

| PR | Files | Content |
|----|-------|---------|
| PR-1 | 14 | 8 production (`RequestPriority`, `IPrioritizedRequest`, `UtilizationMetric`, options, validator, `IUtilizationProvider`, `ILoadSheddingManager`, calculator), `Encina.Resilience.csproj` (`InternalsVisibleTo`, only if the foundation left it out), `PublicAPI.Unshipped.txt`, 3 tests (calculator, options and validator, property), manifest. No guard test file: no public API of PR-1 takes a validated parameter (the validator is internal; options and the calculator take values and have range rules tested by unit tests) |
| PR-2 | 10 | 3 production (provider, manager, strategy factory), `PublicAPI.Unshipped.txt`, 5 tests (provider, manager, strategy, contract, guard), manifest |
| PR-3 | 8 | 2 production (builder extensions, `LoadSheddingHealthCheck`), `PublicAPI.Unshipped.txt`, 3 tests (registration, health check, guard for the extensions), manifest, changelog fragment |
| PR-4 | 14 | log messages, telemetry, `ProcessUtilizationProvider.cs`, `LoadSheddingManager.cs` and `LoadSheddingStrategyFactory.cs` (the three PR-2 emitters, modified to log and record), `EventIdRanges.cs`, core `PublicAPI.Unshipped.txt`, `EncinaEventIdAllocationTests.cs`, telemetry test, provider, manager and strategy tests (modified: log and counter assertions), Resilience `PublicAPI.Unshipped.txt`, manifest |
| PR-5 | 6 | 2 load test files, 2 justification `.md`, README, feature page |

Each PR is within 15 files and 1,500 lines; PR-1 is 14 with the csproj line (the project edits of option C of choice 1 would be recounted). The property test of `SheddingCalculator` is in PR-1, the PR that adds the file, so its property target is reached there. The manifest of each PR carries the rows of the files it adds.

Per-file targets (new files; baseline: none exists on `origin/main`):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `RequestPriority.cs` | 90 | 0 | 0 | 0 | 0 | The enum file holds the importance `Compare` helper, reached by the calculator and orientation tests of PR-1 |
| `UtilizationMetric.cs`, `IPrioritizedRequest.cs`, `IUtilizationProvider.cs`, `ILoadSheddingManager.cs` | 0 | 0 | 0 | 0 | 0 | A flags enum and interfaces have no executable code |
| `LoadSheddingOptions.cs` | 95 | 0 | 0 | 0 | 0 | Defaults and properties |
| `LoadSheddingOptionsValidator.cs` | 95 | 0 | 0 | 0 | 0 | One test per rule; internal |
| `SheddingCalculator.cs` | 100 | 0 | 0 | 90 | 0 | Pure function, all branches and monotonicity property |
| `ProcessUtilizationProvider.cs` | 85 | 100 | 90 | 0 | 0 | Real process reads are non-deterministic; fake clock covers the arithmetic; contract runs the real provider; the guard test file of PR-2 sends null arguments to the public constructors |
| `LoadSheddingManager.cs` | 90 | 100 | 0 | 0 | 0 | Every branch with a fixed random source |
| `LoadSheddingStrategyFactory.cs` | 90 | 0 | 0 | 0 | 0 | Through the real compiler; internal |
| `LoadSheddingBuilderExtensions.cs` | 90 | 100 | 0 | 0 | 0 | Guarded public methods |
| Health check, telemetry, log messages | 85 | 0 | 0 | 0 | 0 | Listener and health-context tests |

CRAP of each method is at most 10 (signal normalization split into one small method per signal).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #138: priority load shedding in Encina.Resilience (ADR-055 R1, R2, R5). AGENTS.md rules apply. Every PR needs the Encina.Resilience project; PR-2 to PR-4 also need the foundation compiler and R6, and PR-2 needs RollingOutcomeWindow from the #139 plan merged first.

IMPLEMENTATION OVERVIEW: (1) RequestPriority, IPrioritizedRequest, options, calculator; (2) process utilization provider, manager, Admission strategy; (3) builder extensions and DI; (4) health check, tenant tag; (5) EventIds 912-923, gauge and counters; (6) unit, guard, contract, property and load tests; (7) docs and fragments.

KEY PATTERNS: precedence instance > per-type option > Degraded; maximum of selected signals; lazy sampling with TimeProvider; rejection Left encina.loadshedding.rejected with ErrorCategory.Unavailable; sensor failure admits and logs; health Degraded; specialists for PublicAPI, changelog, manifest, docs.

REFERENCE FILES: src/Encina.Polly/RateLimiting/AdaptiveRateLimiter.cs, src/Encina/Results/EncinaErrors.cs, docs/plans/message-priority-implementation-plan-123.md, docs/architecture/adr/055-resilience-family.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Operates on live calls |
| 2 | OpenTelemetry | ✅ | Gauge, counters, span event (ADR-055 R5); tenant tag only with multi-tenancy |
| 3 | Structured Logging | ✅ | EventIds 912-916 |
| 4 | Health Checks | ✅ | `LoadSheddingHealthCheck`, placement per #728 choice 3 (Design Choice 12) |
| 5 | Validation | ✅ | Options validated at startup |
| 6 | Resilience | ✅ | A strategy of the Admission pipeline |
| 7 | Distributed Locks | ❌ | Process-local by design |
| 8 | Transactions | ❌ | A shed request never started |
| 9 | Idempotency | ❌ | A shed request has not run; retry is safe |
| 10 | Multi-Tenancy | ⏭️ | Tenant tag only with multi-tenancy; process-wide utilization, no tenant key; tenant-scoped thresholds in issue file `resilience-loadshedding-tenant-thresholds.md` |
| 11 | Module Isolation | ❌ | Module-independent |
| 12 | Audit Trail | ❌ | Infrastructure concern; shed counts are metrics |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|--------------|-------|--------|
| Foundation and R6 (issue files `resilience-foundation-encina-resilience.md`, `resilience-r6-pipeline-token-and-deadline.md`) | Not on main | Block every PR (Dilemma 1, #136): even PR-1's files live in the `Encina.Resilience` project, which does not exist on main |
| #139 `RollingOutcomeWindow` | Plan not on main | Choice 8 |
| #728 choice 3 | Plan on main, pending | Choice 12 |
| #123 priority plan | On main | Choice 4 |

---

## Next Steps

1. Commit this plan, open its PR and link it from #138.
2. When #138 is picked up, ask the maintainer about the Dilemmas (choices 1, 3 to 10, 12, 13, 14 and 15).
3. Brief a worker for PR-1 once the `Encina.Resilience` project exists, then PR-2 (after #139 PR-1), PR-3 to PR-5 once the foundation and R6 are merged.
