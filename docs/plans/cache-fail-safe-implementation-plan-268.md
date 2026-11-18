# Implementation Plan: Fail-safe cache (serve stale on failure, soft and hard timeouts, throttle)

> **Issue**: [#268](https://github.com/dlrivada/Encina/issues/268)
> **Type**: Feature
> **Complexity**: Medium (2 code phases, 2 pull requests of 14 and 13 files)
> **Estimated Scope**: about 500 lines of production code, about 1,100 lines of tests
> **Milestone**: v0.18.0 — Integration & Platform Capabilities

---

## Summary

Issue #268 asks that a `[Cache]` request serves a stale entry when the handler fails or is slow: `FailSafeDurationSeconds` (how long a stale entry may be served), `SoftTimeoutMs` (serve stale if the handler is slower), `HardTimeoutMs` (stop waiting for the handler) and `FailSafeThrottleDurationSeconds` (do not call the handler again for a while after a failure), with metrics. Re-checked against `origin/main` (b4da9e482):

| Premise of the issue | State in the code |
|----------------------|-------------------|
| `CachePipelineBehavior` catches the factory exception and serves `entry.Value` | The class is `QueryCachingPipelineBehavior` (`src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs`). It does not catch handler exceptions, and a handler failure is normally a `Left` (ROP), not an exception: the issue's `catch (Exception)` sketch covers only half of the failure shapes. |
| Stale data is served for any failure | Serving stale data for a `Left` of category `Forbidden`, `Unauthenticated` or `NotFound` would hide a definitive answer (Design Choice 1). `EncinaErrors.GetCategory` exists (`src/Encina/Results/EncinaErrors.cs`, line 211; ADR-062). |
| The behavior can cancel the factory at `HardTimeoutMs` | `RequestHandlerCallback<TResponse>` takes no token (`src/Encina/Abstractions/RequestHandlerDelegate.cs`, line 9) and is bound to the request scope, so the behavior can stop waiting but cannot cancel the handler or keep it running after the response (Design Choice 3). |
| `CacheEntry<T>` can get `ExpiresAtUtc`, `FailSafeExpiresAtUtc`, `LastFailureAtUtc` | The entry is internal with `Value` and `CachedAtUtc` only (line 353); `ExpiresAtUtc` comes from #266 PR 1; this plan adds the other two. The provider TTL is the physical lifetime, so a stale entry survives only if the store TTL is the fail-safe lifetime. |
| The issue lists no option class members | Only attribute properties are listed; this plan adds no `CachingOptions` member. |

The plan stores entries with a physical TTL equal to the fail-safe lifetime and a logical expiry (`ExpiresAtUtc`); on a stale hit it runs the miss path and falls back to the stale value for eligible failures, applies the throttle through `LastFailureAtUtc`, and adds the two timeouts. All 8 providers (Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB, and the planned Memcached of #277) are served by the behavior through `SetAsync` with a longer TTL; none changes.

**Shared pieces (family cache-patterns-1):** `ExpiresAtUtc`, `CachePatternSettings`, `CachePatternLog`, `CachePatternMetrics`, the `CachingPatterns` range (450-499) and the request-flow contract are owned by #266. This plan owns `FailSafeExpiresAtUtc`, `LastFailureAtUtc`, `FailSafePolicy` and EventIds 462-468. The soft timeout reuses `IBackgroundCacheRefresher` of #267.

**Affected packages:** `Encina.Caching`. **Provider category:** Caching (8), behavior-level.

---

## Design Choices

<details>
<summary><strong>1. Which failures serve stale data (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Stale data only for infrastructure failures: an exception (other than the caller's cancellation) or a `Left` of category `Internal`, `Unavailable`, `Timeout`, `UpstreamFailure` or `RateLimited`; any other `Left` is returned as is and removes the stale entry (the handler gave a definitive answer)** | A `Forbidden`, `Unauthenticated`, `NotFound` or `Validation` answer is never masked by old data; deleted data is not served again | A handler that reports an outage with another category gets no fail-safe (it must use the right category) |
| **B) Any `Left` or exception serves stale data (the issue's sketch)** | Simple | A permission change or a deletion is hidden for up to the fail-safe lifetime, which is a security and correctness defect |

### Chosen Option: **A - infrastructure categories only** (recommended, pending the maintainer)

### Rationale

- ADR-062 defines the category contract; `Cancelled` is never a trigger (the caller left).
- Option B wins when every handler in the application reports all failures uniformly and the maintainer wants no category dependence.
- Class: Dilemma

</details>

<details>
<summary><strong>2. What `FailSafeDurationSeconds` measures (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Total age: a stale entry may be served until `CachedAtUtc + FailSafeDurationSeconds` (the issue's example: "serve stale up to 3600s old"); it must be at least `DurationSeconds`** | Matches the issue text; one number to reason about | A fail-safe value below the duration is an error |
| **B) Extra time after expiry: stale until `ExpiresAtUtc + FailSafeDurationSeconds`** | The name reads as "extra" | Contradicts the issue's example ("after 3600s no stale data is available") |

### Chosen Option: **A - total age** (recommended, pending the maintainer)

### Rationale

- The provider TTL becomes `max(FailSafeDurationSeconds, jittered Duration)` (jitter from #266 applies to the logical expiry).
- Option B wins if the maintainer reads "fail-safe duration" as an additional window.
- Class: Dilemma

</details>

<details>
<summary><strong>3. Soft and hard timeouts (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Race `nextStep()` against the timers: at the soft timeout with a stale entry, serve it and hand a background refresh to `IBackgroundCacheRefresher` (#267); at the hard timeout, serve stale if present else return `Left` `cache.hard_timeout` (category `Timeout`); the slow run is abandoned but observed (its result is discarded and its fault logged)** | No re-entry of the pipeline; reuses the #267 worker | The hard timeout stops waiting but does not cancel the handler; the slow run and the background refresh both load a slow backend; the abandoned `nextStep()` keeps running after the request ends and its scope is disposed, which is the very hazard #267 Design Choice 1 uses to reject capturing `nextStep()` (scoped services such as a `DbContext` throw `ObjectDisposedException` inside the abandoned run); the plan observes and logs that fault (468) but cannot prevent it |
| **B) Re-dispatch the request through `IEncina` in the same scope with a token owned by the behavior** | A real cancellation at the hard timeout | The outer pipeline behaviors run twice; a marker and re-entry rules are needed |
| **C) Ship the failure fallback and the throttle now; timeouts later in their own issue** | Smallest first release | Two acceptance criteria of the issue ("Soft and hard timeouts work correctly") are not met |

### Chosen Option: **A - race and background refresh** (recommended, pending the maintainer)

### Rationale

- The behavior cannot cancel or outlive its scope-bound handler, so true cancellation needs B; A is honest about "stop waiting" and documents it.
- The new error is created as `EncinaErrors.Create("cache.hard_timeout", category: ErrorCategory.Timeout, message: ...)` with a constant message: ADR-062 category `Timeout`. No exception message is copied.
- Option B wins where releasing database resources at the hard timeout matters; option C wins to deliver the failure fallback first, and wins outright where an abandoned handler touching a disposed scope is unacceptable (it is the only option with no abandoned run).
- A request with `VaryByUser = true` is not eligible for the background refresh of #267 (its Design Choice 2): its soft timeout serves the stale entry and schedules nothing, and its abandoned run is observed as above.
- Whole-entry access: the stale path needs `ExpiresAtUtc`, `FailSafeExpiresAtUtc` and `LastFailureAtUtc` of the entry read, so it relies on #266 PR 1 (`TryGetForCallerAsync` returns `(Found, Entry)`); this plan does not change that method.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Where the throttle state lives (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In the entry (`LastFailureAtUtc`, rewritten on a failure with the remaining physical TTL)** | Shared by every node through the cache; the issue's own design | One cache write per failure |
| **B) In-process dictionary of key to last failure time** | No write | Each node retries on its own; lost on restart |

### Chosen Option: **A - in the entry** (recommended, pending the maintainer)

### Rationale

- During an outage the extra write is small compared with the handler calls it prevents. Option B wins when writes to the cache are expensive or the cache itself is the failing part.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Invalid or unsupported combinations (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Throw `InvalidOperationException` at resolution (as #266 Design Choice 7 recommends) for: `FailSafeDurationSeconds` below `DurationSeconds`, a negative value, a soft timeout not below the hard timeout, fail-safe with `SlidingExpiration = true`; a soft or hard timeout set without `FailSafeDurationSeconds` is valid, as in the issue's own second example: the hard timeout then returns `cache.hard_timeout` (no stale entry exists) and the soft timeout is inert because there is never a stale entry to serve (documented)** | Deterministic, one place | The first request of the type fails |
| **B) Ignore the unsupported setting and log a Warning once** | The request works | Silent loss of protection |

### Chosen Option: **A - throw at resolution** (recommended, pending the maintainer)

### Rationale

- A sliding window has no fixed age to measure staleness against, so the combination has no defined meaning. Option B wins when a package must never fail a request over a setting.
- Class: Dilemma

</details>

<details>
<summary><strong>6. Reading stale entries is no new access path</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A stale entry is read and served only after the existing bypass checks (caching disabled, `VaryByUser` without a user, multi-tenancy on with no tenant) and only through the same tenant- and user-qualified key** | No new isolation risk; stale data is never visible to another tenant or user | None |
| **B) A separate stale key space** | Independent TTL | A second key to qualify, escape and invalidate (`InvalidatesCache` would miss it) |

### Chosen Option: **A - same key, same checks**

### Rationale

- **Class:** Trivial
- **Settled by:** the existing order in `QueryCachingPipelineBehavior.Handle` (`ShouldUseCache` at line 103 precedes key generation and every read) and AGENTS.md section 3 (gates fail closed when context is missing). This holds under every option of Design Choices 1 to 5, which change what is served, not whether the checks run.

</details>

---

## Implementation Phases

### Phase 1: Retention, failure fallback and throttle (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 14 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Attributes/CacheAttribute.cs` | modify (`FailSafeDurationSeconds`, `FailSafeThrottleDurationSeconds`) |
| 2 | `src/Encina.Caching/Patterns/CachePatternSettings.cs` | modify (fail-safe fields and validation; owned by #266) |
| 3 | `src/Encina.Caching/Patterns/FailSafePolicy.cs` | new (internal static) |
| 4 | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | modify (envelope fields, physical TTL, stale path) |
| 5 | `src/Encina.Caching/Diagnostics/CachePatternLog.FailSafe.cs` | new (ids 462, 465, 466) |
| 6 | `src/Encina.Caching/Diagnostics/CachePatternMetrics.FailSafe.cs` | new (partial class of #266) |
| 7 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 8 | `tests/Encina.UnitTests/Caching/FailSafePolicyTests.cs` | new |
| 9 | `tests/Encina.UnitTests/Caching/QueryCachingFailSafeTests.cs` | new |
| 10 | `tests/Encina.UnitTests/Caching/CachePatternSettingsTests.cs` | modify |
| 11 | `tests/Encina.IntegrationTests/Infrastructure/Caching/FailSafeIntegrationTests.cs` | new |
| 12 | `docs/features/query-caching.md` | modify (`docs-writer`) |
| 13 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 14 | `changelog.d/268-cache-fail-safe.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `internal static class FailSafePolicy`: `bool IsStale(DateTime nowUtc, DateTime expiresAtUtc)`; `bool CanServeStale(DateTime nowUtc, DateTime? failSafeExpiresAtUtc)`; `bool IsInfrastructureFailure(EncinaError error)` (Design Choice 1: `error.GetCategory()` in the five categories); `bool IsThrottled(DateTime nowUtc, DateTime? lastFailureAtUtc, TimeSpan throttle)`; `TimeSpan PhysicalTtl(TimeSpan failSafe, TimeSpan logicalDuration)`.
2. **Modify** `CacheAttribute` (`FailSafeDurationSeconds` 0 disables, `FailSafeThrottleDurationSeconds` 0 disables) and `CachePatternSettings.Resolve` (validation of Design Choice 5).
3. **Modify** the behavior. Store: when fail-safe is on, `SetAsync` receives `PhysicalTtl`, and the entry holds `ExpiresAtUtc` (logical) and `FailSafeExpiresAtUtc = CachedAtUtc + FailSafeDurationSeconds`. Read: the entry is returned (not just its value) from `TryGetFromCacheAsync`. Fresh (`now < ExpiresAtUtc`): served as today. Stale and fail-safe on: if throttled, serve it (log 465); else run the miss path (single-flight of #266 when its mode is on, so the stale decision is made once per flight and shared): on `Right`, store and return; on an eligible failure or an exception (not `OperationCanceledException` from the caller's token) and `CanServeStale`, serve the stale value, log 462 with the error code or `ex.ForLogging()`, and rewrite the entry with `LastFailureAtUtc = now` and the remaining physical TTL; on a non-eligible `Left`, remove the stale entry and return the `Left`; when stale data is no longer allowed, log 466 and return the failure unchanged (the exception is rethrown). An entry without `ExpiresAtUtc` (older build) is fresh.
4. **EventIds (packed, from #266's block):** 462 stale served on failure, 463 stale served on soft timeout, 464 hard timeout elapsed, 465 throttle active, 466 fail-safe exhausted, 467 abandoned run completed, 468 abandoned run faulted. PR 1 emits 462, 465, 466; PR 2 emits 463, 464, 467, 468. Counter `encina.cache.failsafe` with tag `outcome` (`stale_on_failure`, `stale_on_soft_timeout`, `hard_timeout`, `throttled`, `exhausted`) and the tag `encina.tenant_id` only with multi-tenancy on and a tenant present (ADR-054 D4). Logs carry the error code or exception type and the redacted key, never a message.
5. **Tests (unit):** fresh hit unchanged; with fail-safe off every existing behavior test passes; stale entry plus `Left` of each of the five categories serves stale, `Forbidden` / `Unauthenticated` / `NotFound` / `Validation` returns the `Left` and removes the entry; an exception serves stale, a caller cancellation propagates; beyond `FailSafeExpiresAtUtc` the failure is returned; the throttle skips the handler for the throttle period and resumes after it (`FakeTimeProvider`); the provider receives the physical TTL; a bypassed request (no tenant under multi-tenancy, anonymous with `VaryByUser`) never reads a stale entry; two tenants never see each other's stale entry; settings validation per Design Choice 5. **Integration (shared `RedisCollection`):** a real `RedisCacheProvider`: an entry survives its logical expiry for the fail-safe lifetime and disappears after the physical TTL. **Guard tests:** no new public member has parameters (attribute properties only), and the internal types have no `InternalsVisibleTo` for `Encina.GuardTests`: none added.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #268 adds fail-safe caching to QueryCachingPipelineBehavior. #266 added
CacheEntry<T>.ExpiresAtUtc, CachePatternSettings, CachePatternLog/Metrics partial classes and the CachingPatterns range 450-499.

TASK:
Add FailSafeDurationSeconds and FailSafeThrottleDurationSeconds, FailSafePolicy, the entry fields FailSafeExpiresAtUtc and LastFailureAtUtc,
the physical-TTL store, the stale path (failure fallback, throttle), CachePatternLog.FailSafe.cs (462, 465, 466) and the metrics file; write
the unit and Redis integration tests; ask mechanical-fixer for PublicAPI, manifest and changelog and docs-writer for the docs section.

KEY RULES:
- Stale data only for exceptions and Left categories Internal, Unavailable, Timeout, UpstreamFailure, RateLimited (as the maintainer answered Design Choice 1, given in your brief);
  any other Left removes the stale entry and is returned unchanged.
- Never serve stale data past FailSafeExpiresAtUtc; the bypass checks run before any read.
- Never log EncinaError.Message; exceptions through ForLogging(); time from TimeProvider.
- Keep Handle at CRAP 10 or below: put the stale path in private methods.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina/Results/EncinaErrors.cs (GetCategory)
- src/Encina/Errors/ErrorCategory.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/RedisCacheProviderIntegrationTests.cs
```

</details>

### Phase 2: Soft and hard timeouts (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 13 files and depends on #267 PR 2 and PR 3 (the refresher and the optional behavior parameter):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Attributes/CacheAttribute.cs` | modify (`SoftTimeoutMs`, `HardTimeoutMs`) |
| 2 | `src/Encina.Caching/Patterns/CachePatternSettings.cs` | modify (timeouts and validation) |
| 3 | `src/Encina.Caching/Patterns/FailSafeErrors.cs` | new (internal static: `HardTimeout()`) |
| 4 | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | modify (race, abandoned-run observation) |
| 5 | `src/Encina.Caching/Diagnostics/CachePatternLog.FailSafe.cs` | modify (ids 463, 464, 467, 468) |
| 6 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 7 | `tests/Encina.UnitTests/Caching/QueryCachingTimeoutTests.cs` | new |
| 8 | `tests/Encina.UnitTests/Caching/FailSafeErrorsTests.cs` | new |
| 9 | `tests/Encina.UnitTests/Caching/CachePatternSettingsTests.cs` | modify |
| 10 | `tests/Encina.UnitTests/Caching/QueryCachingFailSafeTests.cs` | modify (timeouts with a stale entry) |
| 11 | `docs/features/query-caching.md` | modify (`docs-writer`) |
| 12 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 13 | `changelog.d/268-cache-fail-safe-timeouts.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `FailSafeErrors.HardTimeout()` returning `EncinaErrors.Create(code: "cache.hard_timeout", category: ErrorCategory.Timeout, message: "The cache hard timeout elapsed before the handler completed.")` (named `category:`, constant message, no exception copied; ADR-062 category `Timeout`).
2. **Behavior** (Design Choice 3 option A): on the stale path with timeouts configured, start `nextStep()` and race it with `Task.Delay(soft, _timeProvider, token)` and the hard timer. Soft elapsed with a stale entry: serve it (log 463), call `_refresher.TrySchedule(...)` with the #267 re-dispatch delegate so the cache is refreshed in a live scope, and observe the abandoned run (`ContinueWith` that logs 467 when it completes and 468 with `ex.ForLogging()` when it faults; its result is discarded). Hard elapsed: serve stale if allowed, else return `FailSafeErrors.HardTimeout()` and log 464. Without a stale entry the soft timeout does nothing (there is nothing to serve) and only the hard timeout applies. With a null refresher the soft timeout serves stale and schedules nothing.
3. **Tests (unit, `FakeTimeProvider`, `TaskCompletionSource` gates):** soft timeout serves stale and schedules one refresh; hard timeout with and without a stale entry (the `Left` code and category `Timeout`); a handler finishing before the soft timeout is returned normally; the abandoned run's fault is logged and never unobserved; the constant error message equals the expected text and contains no request data; validation errors of Design Choice 5. **Guard tests:** internal types only: none.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged. #267 PR 2 and PR 3 are merged: IBackgroundCacheRefresher exists and the behavior takes it as an optional parameter.

TASK:
Add SoftTimeoutMs and HardTimeoutMs, FailSafeErrors.HardTimeout (ADR-062 category Timeout, named category: argument, constant message), the
race in the stale path, ids 463, 464, 467, 468, and the unit tests; ask mechanical-fixer for PublicAPI, manifest, changelog and docs-writer for docs.

KEY RULES:
- RequestHandlerCallback has no token: the behavior stops waiting, it never cancels or outlives the handler; document it.
- Observe every abandoned run (no unobserved exception); never log EncinaError.Message.
- Soft timeout without a stale entry does nothing; time from TimeProvider; no Thread.Sleep.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina/Results/EncinaErrors.cs
- src/Encina.Caching/Patterns/BackgroundCacheRefresher.cs (from #267)
```

</details>

### Phase 3: Coverage targets and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. **Error code:** `cache.hard_timeout`, ADR-062 category `Timeout`; the only new code. **EventIds** 462-468.
2. **Coverage targets (per flag).** Each row is entered in the manifest of the PR that adds the file and its tests, never later (#1762): PR 1 sets `FailSafePolicy.cs`, the two partial files and the behavior row; PR 2 sets `FailSafeErrors.cs` and raises the behavior row for the timeout paths:

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Patterns/FailSafePolicy.cs` | 100 | 0 | 0 | 0 | Five pure functions fully unit tested; internal without `InternalsVisibleTo` for the guard project |
| `Patterns/FailSafeErrors.cs` | 100 | 0 | 0 | 0 | One factory asserted in a unit test; internal |
| `Diagnostics/CachePatternLog.FailSafe.cs`, `CachePatternMetrics.FailSafe.cs` | 90 | 0 | 0 | 0 | Generated log bodies and counter calls run in the behavior tests |
| `Behaviors/QueryCachingPipelineBehavior.cs` | keeps 98 | keeps 20 | 0 | 40 | Never lowered; the stale and timeout paths are unit tested; the integration flag runs the Redis retention test |

   The contract flag stays 0 for the new files: they implement no public contract (the contract flag of `QueryCachingPipelineBehavior.cs` is 0 in the manifest); a contract justification file is not allowed, so the manifest reason states that the observable rules are asserted by unit tests. The worker records measured numbers in each PR description and never lowers a target.
3. Verification per PR: `dotnet build --configuration Release` with zero warnings; `dotnet test` of `Encina.UnitTests` (Caching), `Encina.GuardTests`, and for PR 1 `Encina.IntegrationTests` (Redis collection); per-flag coverage and the CRAP table of changed methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Both code PRs are merged.

TASK:
Verify each PR carried its own per-flag targets (table above, measured with coverage-report.cs) and fix any gap through mechanical-fixer;
run --check-justifications.

KEY RULES:
- Never lower an existing target (target-ratchet). No log, metric or error code beyond the plan.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| "stale-if-error" and "stale-while-revalidate" (RFC 5861) | The semantics of serving stale on error and revalidating in the background |
| ADR-062 | Error category contract: the eligibility rule and the `Timeout` category of `cache.hard_timeout` |
| AGENTS.md section 3 | Fail closed on missing context; no message in logs; time from `TimeProvider`; ROP |
| AGENTS.md section 5 (caching) | Consistent across all 8 providers |
| ADR-054 D4 | Tenant tag on metrics only with multi-tenancy on |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `QueryCachingPipelineBehavior`, `CacheEntry<T>` | `src/Encina.Caching/Behaviors/` | Store and read paths |
| `EncinaErrors.GetCategory`, `EncinaErrors.Create(code, category, message)` | `src/Encina/Results/EncinaErrors.cs` | Failure classification and the new error |
| `CachePatternSettings`, `CachePatternLog`, `CachePatternMetrics` | `src/Encina.Caching` (from #266) | Shared settings, partial log and metrics |
| `IBackgroundCacheRefresher` | `src/Encina.Caching` (from #267) | Background refresh at the soft timeout |
| `FakeTimeProvider` | `Microsoft.Extensions.Time.Testing` | Deterministic timeout tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (`CachingPatterns`, registered by #266) | 462-468 | 462 stale on failure, 463 stale on soft timeout, 464 hard timeout, 465 throttle, 466 exhausted, 467 abandoned run completed, 468 abandoned run faulted. If Design Choice 3 is answered with option C, 463, 464, 467 and 468 are released to the family |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 4 | 4 | New: policy, errors, two partial files; modified: attribute, settings, behavior, `PublicAPI.Unshipped.txt` |
| Tests | 4 | 3 | New: policy, fail-safe, timeout, errors tests plus the integration test; modified: settings and fail-safe tests |
| Documentation | 0 | 1 | `docs/features/query-caching.md` |
| Configuration | 2 | 1 | Two changelog fragments; the manifest |
| **Total** | **10** | **9** | 2 pull requests: 14 and 13 files, each at most 15 files and 1500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #268 serves stale cache entries when the handler fails or is slow. #266 owns
ExpiresAtUtc, CachePatternSettings, the Log/Metrics partial classes and EventIds 450-499; #267 owns IBackgroundCacheRefresher; this plan
uses ids 462-468.

IMPLEMENTATION OVERVIEW:
1. PR 1: attribute (FailSafeDurationSeconds, FailSafeThrottleDurationSeconds), FailSafePolicy, physical TTL, the stale path with failure
   fallback and throttle, logs 462/465/466, metrics, Redis retention integration test.
2. PR 2: SoftTimeoutMs / HardTimeoutMs, the race, cache.hard_timeout (ErrorCategory.Timeout), logs 463/464/467/468.

KEY PATTERNS:
- Stale only for exceptions and infrastructure categories; other Left removes the stale entry. Same key, same bypass checks.
- Constant error messages; no EncinaError.Message in logs; the behavior stops waiting, it never cancels the handler.
- Follow the maintainer's answers to Design Choices 1 to 5, given in your brief.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina/Results/EncinaErrors.cs
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Extends `QueryCachingPipelineBehavior`; physical TTL longer than the logical one, all 8 providers through `SetAsync`. |
| 2 | OpenTelemetry | ✅ Include | Counter `encina.cache.failsafe` on the `Encina.Caching` meter of #266; tenant tag per ADR-054 D4; meter registration is #723. |
| 3 | Structured Logging | ✅ Include | EventIds 462-468 in `CachingPatterns`. |
| 4 | Health Checks | ❌ N/A | A behavior-level pattern with no backend of its own. |
| 5 | Validation | ✅ Include | Attribute combinations validated at resolution (Design Choice 5). |
| 6 | Resilience | ✅ Include | Fail-safe is the resilience pattern; it does not replace Polly retry or circuit breaker, which run in their own behaviors. |
| 7 | Distributed Locks | ❌ N/A | No lock; coalescing of the failing call is #266's single-flight when enabled. |
| 8 | Transactions | ❌ N/A | Cache operations are independent of units of work. |
| 9 | Idempotency | ❌ N/A | Re-serving a cached value is not message deduplication. |
| 10 | Multi-Tenancy | ✅ Include | Stale entries use the tenant-qualified key and the existing bypass checks (Design Choice 6); the issue's deferral to #746 covers only module scoping. |
| 11 | Module Isolation | ⏭️ Defer | Module-scoped fail-safe keys follow #746 (open). |
| 12 | Audit Trail | ❌ N/A | Infrastructure with no compliance event. |

**Deferred items:** module dimension (#746, open).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| #266 PR 1 and PR 3 (`ExpiresAtUtc`, settings, log and metrics classes, EventId range) | Pending | Must be merged first |
| #267 PR 2 and PR 3 (`IBackgroundCacheRefresher`) | Pending | Needed by PR 2 of this plan only, and only if Design Choice 3 is answered with option A |
| ADR-062 error categories (`GetCategory`, `Create` with category) | ✅ Available | On `origin/main` |
| Maintainer answers to Design Choices 1 to 5 | Pending | Asked when work starts |

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1 to 5) when work starts, records the answers under `## Maintainer Decisions`, then briefs two workers in PR order.
2. After PR 1 merges, comment on #278 (auto-recovery, next family) that the stale path and `FailSafePolicy` exist.
