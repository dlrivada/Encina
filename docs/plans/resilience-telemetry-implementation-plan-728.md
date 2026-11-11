# Implementation Plan: Resilience Telemetry — OpenTelemetry instrumentation of `Encina.Polly` and `Encina.Extensions.Resilience`

> **Issue**: [#728](https://github.com/dlrivada/Encina/issues/728)
> **Type**: Feature
> **Complexity**: Medium (9 phases, no database providers, 4 pull requests)
> **Estimated Scope**: ~700-900 lines of production code + ~1,200-1,500 lines of tests

---

## Summary

Make retries, circuit-breaker state changes, bulkhead rejections and rate-limit rejections visible to operators through OpenTelemetry, following decision R5 (option 5b) of [ADR-055](../architecture/adr/055-resilience-family.md) and the naming rules of [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D3 and D4. The maintainer decision of 2026-10-16 (comment on #728) puts the feature in the 1.0 scope, with no enable or disable switch: the application subscribes with `AddMeter` and `AddSource`.

What exists today (read in `main` at `7a0049c07`):

- `src/Encina.Polly/Behaviors/RetryPipelineBehavior.cs` starts an `Encina.Retry` activity through `src/Encina.Polly/Diagnostics/RetryTelemetry.cs` (an `ActivitySource("Encina", "1.0")`) and counts retries through `src/Encina/Recoverability/RetryMetrics.cs` (`Meter("Encina", "1.0")`, `encina.retry.attempts_total`, `encina.retry.skipped_total`). `RetryMetrics.RecordSkipped` is also called by `Encina.Messaging` (`RecoverabilityPipelineBehavior`, `DelayedRetryProcessor`, `OutboxBatchProcessor`).
- `CircuitBreakerPipelineBehavior` and `DatabaseCircuitBreakerPipelineBehavior` build Polly pipelines whose `OnOpened`, `OnClosed` and `OnHalfOpened` callbacks only log. `BulkheadPipelineBehavior` (over the hand-written `BulkheadManager`) and `RateLimitingPipelineBehavior` (over `AdaptiveRateLimiter`) are not Polly strategies, so Polly never measures them. None of the pipelines calls `ConfigureTelemetry`.
- `StandardResiliencePipelineBehavior` (`Encina.Extensions.Resilience`) has no telemetry.
- `WithEncina()` in `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` subscribes explicit sources and only `AddMeter("Encina")`; it does not subscribe `Encina.Resilience` or Polly's `Polly` meter.

What this plan delivers: `ActivitySource("Encina.Resilience")` request spans with events (retry, breaker state change, rejection); Polly's `resilience.polly.*` instruments enriched with request type, error code and tenant (only when multi-tenancy is on); Encina-owned instruments under `Meter("Encina.Resilience")` only for what Polly does not measure (shed load by the bulkhead and the rate limiter, rate-limiter state, partition count); subscription in `WithEncina()`; removal of the `Encina.Retry` activity and of the `encina.retry.attempts_total` counter; a zero-allocation-without-listener benchmark; and a throttled-state health check that depends on [#2274](https://github.com/dlrivada/Encina/issues/2274).

- **Affected packages**: `Encina` (core, `Diagnostics/`, `Recoverability/RetryMetrics.cs`), `Encina.Polly`, `Encina.Extensions.Resilience`, `Encina.OpenTelemetry`.
- **Provider category**: Resilience (Polly, Extensions.Resilience; `Encina.Http` is deferred to [#1233](https://github.com/dlrivada/Encina/issues/1233)). No database, caching, transport, lock or validation provider matrix applies.
- **Estimated size**: about 40 files and 2,000-2,400 lines, which exceeds one worker pull request (15 files, 1,500 lines). The split into four pull requests is in "Implementation Phases" below.
- **Prerequisites**: [#2277](https://github.com/dlrivada/Encina/issues/2277) (removes the dead `EnableTelemetry` and `EnableLogging`) and [#2274](https://github.com/dlrivada/Encina/issues/2274) (rate-limiter state change) are open; see "Prerequisites & Dependencies".

---

## Design Choices

<details>
<summary><strong>1. Where the shared telemetry lives while `Encina.Resilience` does not exist yet</strong></summary>

ADR-055 R1 replaces `Encina.Polly` and `Encina.Extensions.Resilience` with a planned `Encina.Resilience` package that does not exist in `src/`, and no open issue creates it. R5 names `ActivitySource("Encina.Resilience")` and `Meter("Encina.Resilience")`. Both existing packages need the same source, meter and name constants, and neither references the other.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `ResilienceTelemetry` class in core `Encina` (`src/Encina/Diagnostics/ResilienceTelemetry.cs`)** | One `ActivitySource` and one `Meter` instance for both packages and for the future `Encina.Resilience`; names in one constants class (ADR-054 D3); core `ContractTests` can pin the names; same precedent as `RetryMetrics` in core | Adds a resilience-named type to core; the Polly-typed enrichers still have to exist twice (core has no Polly reference) |
| **B) Duplicate the class in each package** | No change to core | Two `ActivitySource("Encina.Resilience")` instances and two copies of the names; drift risk; cannot be pinned by one contract test |
| **C) Wait for `Encina.Resilience`** | Lands once, in its final home | Blocks a p0-mandatory 1.0 issue on an unscheduled package; telemetry stays invisible meanwhile |

### Chosen Option: **A — Shared class in core `Encina`** (recommended, pending the maintainer)

### Rationale

- A wins when `Encina.Resilience` is far away: the telemetry ships now and the future package reuses it unchanged.
- C wins if the maintainer prefers never to touch the two packages that R1 deletes; B wins only if core must stay free of any resilience name.
- **Class**: Dilemma.

</details>

<details>
<summary><strong>2. What happens to `RetryMetrics`, which `Encina.Messaging` also uses</strong></summary>

ADR-055 R5 removes `encina.retry.attempts_total`, `encina.retry.skipped_total` and the loose `Meter("Encina")` use "by the resilience code". `RetryMetrics.RecordAttempt` is called only by `RetryPipelineBehavior` (lines 132 and 138). `RetryMetrics.RecordSkipped` and the four reason constants are also called by three `Encina.Messaging` classes (`RecoverabilityPipelineBehavior`, `DelayedRetryProcessor`, `OutboxBatchProcessor`) whose delayed retries stay in Messaging under R4.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Delete `RetryMetrics` and the Messaging calls** | Literal reading of R5; no loose counter left | Messaging loses its skipped-retry counts (permanent, non-idempotent, `Retry-After` over the ceiling) that no Polly instrument replaces; touches three Messaging files and their tests |
| **B) Remove `RecordAttempt` and the Polly callers; keep `RecordSkipped` and the reasons for Messaging only** | Operators keep the skip counts of delayed retries; scope stays in the resilience packages; Polly's own retry events replace the attempts counter | `Meter("Encina")` survives in Messaging until the ADR-054 D3 meter rename covers Messaging |
| **C) Keep `RetryMetrics` untouched** | No change | Two retry-attempt families (Polly's and `encina.retry.attempts_total`), which R5 rules out |

### Chosen Option: **B — Remove the attempts counter and the Polly callers, keep the skipped counter for Messaging** (recommended, pending the maintainer)

### Rationale

- B wins in the scenario where an operator watches `encina.retry.skipped_total` for outbox and delayed-retry dead letters today: R5 speaks of "the resilience code", and the Messaging callers are not Polly code.
- A wins if the maintainer wants no `Meter("Encina")` left and accepts losing the Messaging skip counts, or moves them to a Messaging instrument in the same change.
- **Class**: Dilemma.

</details>

<details>
<summary><strong>3. Where the throttled-state health check lives</strong></summary>

The maintainer decision of 2026-10-16 asks for a throttled-state health check fed by the rate-limiter state-change event of #2274. `IEncinaHealthCheck` and `EncinaHealthCheck` live in `Encina.Messaging` (`src/Encina.Messaging/Health/`); `Encina.Polly` references only `Encina` core. `Encina.OpenTelemetry` already references `Encina.Messaging` and holds health checks (`Migrations/SchemaDriftHealthCheck.cs`, `Resharding/ReshardingHealthCheck.cs`) but does not reference `Encina.Polly`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In `Encina.Polly` (`Health/RateLimiterThrottledHealthCheck.cs`), adding a reference to `Encina.Messaging`** | Next to `IRateLimiter` and `RateLimitState`; follows the `QuartzHealthCheck` precedent of a health check inside its package | Polly package gains a Messaging dependency for one class; the package is slated for removal by R1 |
| **B) In `Encina.OpenTelemetry`, adding a reference to `Encina.Polly`** | Keeps observability code in the observability package | OpenTelemetry package gains a Polly dependency; Polly types leak into a package every app references |
| **C) Plain `Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck` in `Encina.Polly`** | No Messaging reference and no OpenTelemetry coupling | Does not use the `IEncinaHealthCheck` convention of AGENTS.md section 6 function 4 |

### Chosen Option: **A — In `Encina.Polly`** (recommended, pending the maintainer)

### Rationale

- A wins when the check must read the live `IRateLimiter` state without a new cross-package dependency chain beyond Messaging, which the Quartz and RabbitMQ packages already accept.
- B wins if the maintainer wants every Encina health check discoverable in `Encina.OpenTelemetry`; C wins if the Messaging reference is unwanted.
- **Class**: Dilemma.

</details>

<details>
<summary><strong>4. Instrument enrichment strategy</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Enrich Polly's `resilience.polly.*` instruments, add Encina-owned instruments only for gaps** | One family of instruments shared with the future `Encina.Http`; no double counting | Needs a Polly `MeteringEnricher` per package |
| **B) Encina instruments only** | Full control of names | Two families for the same strategies once `Encina.Http` exists |
| **C) Emit both families** | Maximum coverage | Duplicates the instrumentation of the library underneath |

### Chosen Option: **A — Enrich Polly, own only the gaps**

### Rationale

- Retry attempts, breaker transitions and timeouts are native Polly events (`OnRetry`, `OnCircuitOpened`, ...); the bulkhead and the rate limiter are hand-written and therefore the gaps.
- **Class**: Settled
- **Settled by**: ADR-055 R5 (decision R5, option 5b of SPIKE [#2691](https://github.com/dlrivada/Encina/issues/2691)), which rejects 5a and the "emit both" variant citing ADR-054 D6.2.

</details>

<details>
<summary><strong>5. Source, meter and instrument names; tenant tag</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Resilience` source and meter, lowercase dotted instrument names, tenant tag only when multi-tenancy is on** | Matches the 27-meter rename rule and the tenant rule across Encina | Meter name belongs to a package that does not exist yet |
| **B) Keep the loose `Encina` source and meter** | No migration | Contradicts ADR-054 D3 and R5 |

### Chosen Option: **A — `Encina.Resilience` names and the D4 tenant rule**

### Rationale

- The tenant is a metric tag and a span attribute only when multi-tenancy is on; its cardinality is documented in the observability guide ([#906](https://github.com/dlrivada/Encina/issues/906)).
- **Class**: Settled
- **Settled by**: ADR-054 D3 and D4; ADR-055 R5; SPEC-002 AC-044.

</details>

<details>
<summary><strong>6. Activation switches</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No switch; the application subscribes with `AddMeter` and `AddSource`** | Pay-for-what-you-use through OpenTelemetry's own opt-in; no dead options | None |
| **B) Keep `EncinaPollyOptions.EnableTelemetry`** | Familiar | The flag is never read today (#2277) |

### Chosen Option: **A — No switch**

### Rationale

- Without a listener the instruments cost nothing (`HasListeners()` guards, `StartActivity` returning `null`).
- **Class**: Settled
- **Settled by**: maintainer decision of 2026-10-16 on #728 and #2277; ADR-054 settled point 7.

</details>

<details>
<summary><strong>7. No error message and no outcome text in telemetry or Polly logging</strong></summary>

Polly's `ConfigureTelemetry(ILoggerFactory)` also enables Polly's own event logging, which formats outcomes.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ConfigureTelemetry(TelemetryOptions)` with the enricher and the default (null) logger factory; only the error code or the exception type is a tag** | Satisfies the `EncinaError.Message` rule; Encina's own `[LoggerMessage]` logs stay the single source | Polly's own log lines are not available |
| **B) Pass the application `ILoggerFactory` to Polly** | Polly's strategy logs for free | Risks printing outcome text, which can carry an `EncinaError.Message` |

### Chosen Option: **A — Enricher only, no Polly logger factory**

### Rationale

- A test asserts that no tag value and no log line contains an error message.
- **Class**: Settled
- **Settled by**: AGENTS.md section 3 (the `EncinaError.Message` rule); ADR-054 settled point 2; GDPR Art. 5(1)(c) and Art. 25 as cited in ADR-055 R5.

</details>

<details>
<summary><strong>8. Names of the Encina-owned instruments</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `encina.resilience.shed_load` (`Counter<long>`), `encina.resilience.rate_limiter.state_changes` (`Counter<long>`), `encina.resilience.rate_limiter.state` (`ObservableGauge<int>`), `encina.resilience.partitions` (`ObservableGauge<int>`) under `Meter("Encina.Resilience")`** | Lowercase dotted, no `_total`, unit in metadata (ADR-054 rule 4); no clash with `resilience.polly.*` | Names are new and need one review |
| **B) Names under `resilience.*`** | Looks like Polly's family | Easy to confuse with Polly's own instruments |

### Chosen Option: **A — `encina.resilience.*` instrument names**

### Rationale

- Bounded tags only: `resilience.policy`, request type, reason, state, tenant when multi-tenancy is on. Partition count is an observable gauge fed by the rate limiter's key count (one key per request type today).
- **Class**: Trivial
- **Settled by**: ADR-054 settled point 4 (instrument-name rules) and ADR-055 R5 (the three gap categories).

</details>

---

## Implementation Phases

**Split into pull requests** (each at most 15 files and 1,500 changed lines, one package family):

| PR | Phases | Content | Depends on |
|----|--------|---------|------------|
| PR-1 | 1, 2, 3 (retry and breakers) | `ResilienceTelemetry` in core, Polly enricher, `RetryPipelineBehavior`, `CircuitBreakerPipelineBehavior`, `DatabaseCircuitBreakerPipelineBehavior`, remove `RetryTelemetry` and `RecordAttempt`, unit and guard tests | #2277 merged first (it edits the same options file) |
| PR-2 | 3 (shed load), 5 (OpenTelemetry), 7 | Bulkhead and rate-limit instruments, rate-limiter state-change counter and gauge, `WithEncina()` subscriptions | PR-1; #2274 for the state-change event |
| PR-3 | 4, 8, 9 | `StandardResiliencePipelineBehavior` and its enricher, contract, integration and benchmark tests, READMEs and the observability guide, changelog fragment | PR-1 |
| PR-4 | 6 | `RateLimiterThrottledHealthCheck` and its tests | PR-2 and #2274 |

### Phase 1: Core Telemetry Model and Names

<details>
<summary><strong>Tasks</strong></summary>

1. Create `src/Encina/Diagnostics/ResilienceTelemetry.cs` (`public static class ResilienceTelemetry`, namespace `Encina.Diagnostics`): constants `SourceName = "Encina.Resilience"`, `MeterName = "Encina.Resilience"`, tag keys `resilience.policy`, `resilience.outcome`, `resilience.retry_attempt`, request-type and error-code keys, the tenant key `encina.tenant_id` (the key `RetryTelemetry` uses today), and the shared `ActivitySource` and `Meter` instances (version `"1.0"`).
2. Add helpers `StartRequestSpan(string requestName, string? tenantId)` returning `Activity?` (guarded by `HasListeners()`), `AddRetryEvent`, `AddBreakerStateEvent`, `AddRejectionEvent` (each a no-op when the activity is `null`) and `Complete(Activity?, bool isSuccess, string? errorCode)`.
3. Add the new symbols to `src/Encina/PublicAPI.Unshipped.txt` (through `mechanical-fixer`).
4. Constructor dependencies: none (static). Time is not read here; tag values are strings from the caller.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library (pre-1.0, no backward compatibility). Issue #728 adds OpenTelemetry to the resilience packages per ADR-055 R5 and ADR-054 D3/D4. Today the loose ActivitySource("Encina") lives in src/Encina.Polly/Diagnostics/RetryTelemetry.cs and Meter("Encina") in src/Encina/Recoverability/RetryMetrics.cs.

TASK:
Create src/Encina/Diagnostics/ResilienceTelemetry.cs: a public static class with SourceName/MeterName "Encina.Resilience", tag-key constants, one shared ActivitySource and one Meter, and helpers to start a request span and add retry / breaker-state / rejection events. Helpers must return null / do nothing when there is no listener (HasListeners guard).

KEY RULES:
- No cost without a listener: no allocation when HasListeners() is false.
- Tags carry only request type, error code or exception type, policy, outcome, attempt number and the tenant id (only when not null). NEVER EncinaError.Message, exception messages, URLs or personal data.
- XML docs on every public member; add symbols to PublicAPI.Unshipped.txt via the mechanical-fixer agent.
- Zero warnings; nullable enabled.

REFERENCE FILES:
- src/Encina.Polly/Diagnostics/RetryTelemetry.cs (current activity helper)
- src/Encina/Recoverability/RetryMetrics.cs (current meter style)
- docs/architecture/adr/055-resilience-family.md (R5), docs/architecture/adr/054-observability-conventions-and-trace-propagation.md (D3, D4)
- src/Encina.OpenTelemetry/Migrations/MigrationActivitySource.cs (source constant style)
```

</details>

### Phase 2: Polly Enricher (default implementation)

<details>
<summary><strong>Tasks</strong></summary>

1. Create `src/Encina.Polly/Diagnostics/PollyTelemetryEnricher.cs` (`internal sealed class PollyTelemetryEnricher : Polly.Telemetry.MeteringEnricher`): override `Enrich<TResult, TArgs>(in EnrichmentContext<TResult, TArgs> context)` to add request type, error code (from the `Either<EncinaError, T>` outcome via `GetCode()`, or the exception type) and the tenant when present.
2. Pass the per-call tenant through a `ResilienceContext` property (`ResiliencePropertyKey<string>`), so the cached per-request-type pipeline is built once and the enricher allocates nothing per call; the request type is fixed per pipeline and set once.
3. Create `src/Encina.Polly/Diagnostics/PollyTelemetry.cs` (internal helper): `ConfigureTelemetry(this ResiliencePipelineBuilder<T>, string requestName)` that calls Polly.Extensions `ConfigureTelemetry(new TelemetryOptions { MeteringEnrichers = { ... } })` with no logger factory (Design Choice 7).
4. Constructor dependencies: none; the enricher is stateless.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Encina.Polly pipelines are built once per request type and cached (see CircuitBreakerPipelineBehavior.GetOrCreateCircuitBreaker). They never call ConfigureTelemetry, so Polly's meter "Polly" (resilience.polly.strategy.events, resilience.polly.strategy.attempt.duration, resilience.polly.pipeline.duration) has no Encina context. Polly.Extensions is already referenced by src/Encina.Polly/Encina.Polly.csproj.

TASK:
Add an internal MeteringEnricher that adds request type, error code and the tenant (only when set) to Polly's metrics, and an internal extension that wires it into a ResiliencePipelineBuilder<Either<EncinaError, TResponse>> via ConfigureTelemetry(TelemetryOptions) WITHOUT a logger factory. Read the tenant from the ResilienceContext properties, not from a closure that captures the request.

KEY RULES:
- Error code = EncinaError code (GetCode()) or exception type name; never EncinaError.Message.
- No per-call allocation without a listener; verify with the benchmark of Phase 8.
- Verify against the installed Polly.Extensions version that the enricher base class and the TelemetryOptions member names used here exist; adjust the plan notes, not the rule.
- XML docs; internal types need no PublicAPI lines.

REFERENCE FILES:
- src/Encina.Polly/Behaviors/CircuitBreakerPipelineBehavior.cs (pipeline builder)
- src/Encina.Polly/Behaviors/RetryPipelineBehavior.cs (OnRetry, tenant handling)
- docs/architecture/adr/055-resilience-family.md (R5)
```

</details>

### Phase 3: Pipeline Behaviors — Retry, Breakers, Bulkhead, Rate Limiting

<details>
<summary><strong>Tasks</strong></summary>

1. `RetryPipelineBehavior.cs`: replace `RetryTelemetry.StartRetry`/`Complete` with `ResilienceTelemetry.StartRequestSpan`/`Complete`; in `OnRetry` call `AddRetryEvent` (attempt number, error code); remove the two `RetryMetrics.RecordAttempt` calls; keep `Skip(...)` and `RetryMetrics.RecordSkipped` per Design Choice 2 (option B); add `PollyTelemetry.ConfigureTelemetry`.
2. Delete `src/Encina.Polly/Diagnostics/RetryTelemetry.cs`. In `RetryMetrics.cs` remove `RecordAttempt` and the `Attempts` counter (option B).
3. `CircuitBreakerPipelineBehavior.cs` and `DatabaseCircuitBreakerPipelineBehavior.cs`: in `OnOpened`, `OnClosed`, `OnHalfOpened` (and the database variant's equivalents) call `AddBreakerStateEvent` next to the existing log lines; add `ConfigureTelemetry`; start the request span.
4. `BulkheadPipelineBehavior.cs`: on a rejected acquire (`RejectedBulkheadFull`, `RejectedQueueTimeout`, `RejectedCancelled`) add a rejection span event and increment `encina.resilience.shed_load` with `reason` tag.
5. `RateLimitingPipelineBehavior.cs`: on `!acquireResult.IsAllowed` add a rejection span event and increment `encina.resilience.shed_load`; on a state transition increment `encina.resilience.rate_limiter.state_changes` (the event of #2274 supplies the transition, so this part waits for #2274).
6. Existing `[LoggerMessage]` statements stay unchanged (EventIds 3900-3921); no new log message is added.
7. Update `src/Encina.Polly/PublicAPI.Unshipped.txt` (through `mechanical-fixer`) for any removed public member.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Encina.Polly behaviors: RetryPipelineBehavior (activity + RetryMetrics today), CircuitBreakerPipelineBehavior and DatabaseCircuitBreakerPipelineBehavior (callbacks only log), BulkheadPipelineBehavior and RateLimitingPipelineBehavior (hand-written, invisible to Polly). Phase 1 created ResilienceTelemetry (core) and Phase 2 the Polly enricher.

TASK:
Wire the five behaviors to ResilienceTelemetry: a request span with events for retry attempts (attempt number), breaker state changes and rejections; shed-load counter for bulkhead and rate-limit rejections; ConfigureTelemetry on the Polly pipelines (retry, both breakers). Delete RetryTelemetry.cs and RetryMetrics.RecordAttempt. Keep RetryMetrics.RecordSkipped for Encina.Messaging (Design Choice 2, option B, recommended pending the maintainer).

KEY RULES:
- Behaviors that return Left stay Either-based; telemetry never changes control flow and never throws (wrap nothing in try/catch that could hide a failure).
- Tags: request type, error code or exception type, policy, outcome, attempt, reason, tenant (only when not null). NEVER an error message.
- Do not add EventIds; existing 3900-3921 stay.
- Keep the unit tests green; add MeterListener / ActivityListener assertions per behavior.

REFERENCE FILES:
- src/Encina.Polly/Behaviors/*.cs
- src/Encina.Polly/Bulkhead/BulkheadManager.cs, src/Encina.Polly/RateLimiting/AdaptiveRateLimiter.cs
- src/Encina/Recoverability/RetryMetrics.cs, src/Encina.Messaging/Recoverability/DelayedRetryProcessor.cs
- tests/Encina.UnitTests/Polly/RetryPipelineBehaviorTests.cs
```

</details>

### Phase 4: Provider Integration — `Encina.Extensions.Resilience`

<details>
<summary><strong>Tasks</strong></summary>

1. Create `src/Encina.Extensions.Resilience/Diagnostics/ResilienceTelemetryEnricher.cs` (`internal sealed`, derives from `Polly.Telemetry.MeteringEnricher`): request type, error code or exception type (the behavior throws `EncinaResilienceException` for a `Left`, so the code comes from its `Error`), tenant when present.
2. Register the enricher in `src/Encina.Extensions.Resilience/ServiceCollectionExtensions.cs` through the Microsoft.Extensions.Resilience enricher registration (`AddResilienceEnricher`, name to confirm against the installed package), so every named pipeline of the provider reports through Polly's `Polly` meter.
3. `Behaviors/StandardResiliencePipelineBehavior.cs`: start a request span with `ResilienceTelemetry`, add events for the cases it already distinguishes (`BrokenCircuitException`, `TimeoutRejectedException`, `EncinaResilienceException`), complete the span with the error code.
4. `Encina.Http` is deferred (#1233): its telemetry arrives with that package and reuses the same enricher registration.
5. Update `PublicAPI.Unshipped.txt` if any public member changes.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Encina.Extensions.Resilience runs a Microsoft.Extensions.Resilience pipeline through StandardResiliencePipelineBehavior (no telemetry today). Microsoft.Extensions.Resilience already emits Polly's "Polly" meter instruments for registered pipelines; they lack Encina context.

TASK:
Add an internal enricher (request type, error code / exception type, tenant when present), register it in AddEncinaStandardResilience (see ServiceCollectionExtensions.cs), and instrument StandardResiliencePipelineBehavior with a request span and events using ResilienceTelemetry from core.

KEY RULES:
- Never use EncinaError.Message or exception messages as tags (the existing EncinaError.New($"...", ex) texts must not reach tags or logs).
- No new switch in StandardResilienceOptions.
- Confirm the enricher registration API name in the installed Microsoft.Extensions.Resilience package before coding; if it differs, record the difference in the PR description.
- Add guard, unit and integration tests (MeterListener / ActivityListener against a real pipeline).

REFERENCE FILES:
- src/Encina.Extensions.Resilience/Behaviors/StandardResiliencePipelineBehavior.cs
- src/Encina.Extensions.Resilience/ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/Extensions/Resilience/StandardResiliencePipelineBehaviorTests.cs
- tests/Encina.IntegrationTests/Observability/Resilience/StandardResilienceEndToEndTests.cs
```

</details>

### Phase 5: Configuration, DI and Subscription

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` `WithEncina()`: add `tracing.AddSource(ResilienceTelemetry.SourceName)`, `metrics.AddMeter(ResilienceTelemetry.MeterName)` and `metrics.AddMeter("Polly")`. Also add the two overloads `AddEncinaInstrumentation` for the tracer and meter builders (they currently add only `"Encina"`).
2. `src/Encina.Polly/ServiceCollectionExtensions.cs`: no new option. `EncinaPollyOptions.EnableTelemetry` and `EnableLogging` are removed by #2277 (a prerequisite), not here.
3. Registration completeness (AGENTS.md section 3): the enricher needs no DI in Polly (built in code); the Extensions.Resilience enricher registration is covered by a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes`.
4. Update `src/Encina.OpenTelemetry/PublicAPI.Unshipped.txt` only if public members change (none expected).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
WithEncina() subscribes explicit sources and only AddMeter("Encina"). The new Encina.Resilience source/meter and Polly's "Polly" meter would be invisible to an application that uses WithEncina().

TASK:
In src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs add the Encina.Resilience source, the Encina.Resilience meter and the Polly meter to WithEncina(), and to the two AddEncinaInstrumentation builder overloads. Add a unit test per entry that builds a TracerProvider / MeterProvider and observes a recorded measurement.

KEY RULES:
- Do not touch the wildcard/"every source subscribed" work of #1787; add explicit entries only.
- No enable/disable option (Design Choice 6).
- Zero warnings; XML docs unchanged unless behavior text changes.

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- tests/Encina.UnitTests (existing OpenTelemetry registration tests)
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md (D3, D6.2)
```

</details>

### Phase 6: Cross-Cutting Integration — Tenant and Throttled-State Health Check

<details>
<summary><strong>Tasks</strong></summary>

1. Tenant: every span and every enriched or Encina-owned instrument adds `encina.tenant_id` only when `IRequestContext.TenantId` is not null (ADR-054 D4); no tag when multi-tenancy is off.
2. Health check (PR-4, after #2274): `src/Encina.Polly/Health/RateLimiterThrottledHealthCheck.cs` deriving from `EncinaHealthCheck` (`src/Encina.Messaging/Health/EncinaHealthCheck.cs`), `public const string DefaultName = "encina-polly-ratelimiter"`, static tags array, reads the rate-limiter state through the state-change event or `IRateLimiter.GetState`; returns `Degraded` while a key is `Throttled`, `Healthy` otherwise; registration via a `AddRateLimiterHealthCheck` extension on `IHealthChecksBuilder` with `TryAdd*`.
3. Fail closed where context is missing: if the limiter cannot be resolved, the check reports `Unhealthy` rather than `Healthy`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
Maintainer decision 2026-10-16 (#728): the rate-limiter state-change event decided on #2274 feeds metrics and a throttled-state health check. IRateLimiter (src/Encina.Polly/RateLimiting/IRateLimiter.cs) exposes GetState(key) -> RateLimitState? (Normal, Throttled, Recovering); the state-change event does not exist until #2274 lands. IEncinaHealthCheck lives in Encina.Messaging.

TASK:
After #2274 is merged, add RateLimiterThrottledHealthCheck in Encina.Polly (Design Choice 3, option A, recommended pending the maintainer), its registration extension and tests. Add the tenant tag rule to the spans and instruments of earlier phases if any call site misses it.

KEY RULES:
- Follow the health check conventions: DefaultName const, static Tags array, scoped resolution via IServiceProvider.CreateScope().
- Health check results never include EncinaError.Message.
- Do not start this phase before #2274 is merged.

REFERENCE FILES:
- src/Encina.Messaging/Health/EncinaHealthCheck.cs, src/Encina.Quartz/Health/QuartzHealthCheck.cs
- src/Encina.Polly/RateLimiting/AdaptiveRateLimiter.cs
- https://github.com/dlrivada/Encina/issues/2274
```

</details>

### Phase 7: Observability — Encina-Owned Instruments

<details>
<summary><strong>Tasks</strong></summary>

1. In `ResilienceTelemetry` (Phase 1) define on `Meter("Encina.Resilience")`: `encina.resilience.shed_load` (`Counter<long>`, unit `{rejections}`, tags `resilience.policy`, `reason`, request type, tenant when on), `encina.resilience.rate_limiter.state_changes` (`Counter<long>`, tags `from`, `to`, request type), `encina.resilience.rate_limiter.state` (`ObservableGauge<int>`), `encina.resilience.partitions` (`ObservableGauge<int>`).
2. The gauges read `AdaptiveRateLimiter` state through a small provider callback registered by `AddEncinaPolly` (no static mutable state; `TimeProvider` is not needed because no time is read).
3. Structured logging: no new `[LoggerMessage]`; the Polly range 3900-3949 (3900-3921 used) and the ExtensionsResilience range 3950-3999 (3950-3955 used) have free slots if a later review wants a log line; any new id is registered first in `src/Encina/Diagnostics/EventIdRanges.cs` (ADR-021).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
Per ADR-055 R5 Encina-owned instruments exist only for what Polly does not measure: adaptive concurrency state, shed load and partition count. Retry, breaker and timeout events are native Polly events (enriched in Phase 2/4).

TASK:
Add the four instruments of Design Choice 8 to ResilienceTelemetry and record them from BulkheadPipelineBehavior and RateLimitingPipelineBehavior. The two observable gauges read the registered AdaptiveRateLimiter / BulkheadManager state.

KEY RULES:
- Instrument names lowercase dotted, unit in metadata, no _total suffix (ADR-054 rule 4).
- Bounded tag values only; no keys derived from user data.
- Counters must not allocate per call when no listener is attached (benchmark in Phase 8).
- Public API additions go to PublicAPI.Unshipped.txt via mechanical-fixer.

REFERENCE FILES:
- src/Encina.Polly/Bulkhead/BulkheadManager.cs (GetMetrics, TotalRejected)
- src/Encina.Polly/RateLimiting/AdaptiveRateLimiter.cs
- src/Encina.OpenTelemetry/Sharding/TimeBasedShardingMetrics.cs (observable gauge style)
```

</details>

### Phase 8: Testing

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/Polly/*`, `tests/Encina.UnitTests/Extensions/Resilience/*`): extend the existing behavior tests with `MeterListener` and `ActivityListener` assertions for each pattern (retry events with attempt number, breaker open/half-open/closed events, bulkhead and rate-limit rejections counted, enricher tags, no tenant tag when `TenantId` is null, no error message in any tag); tests for the removed `RetryTelemetry` and `RecordAttempt`; `FakeTimeProvider` for time.
2. **Guard** (`tests/Encina.GuardTests/Infrastructure/Polly/*`, `tests/Encina.GuardTests/Extensions/Resilience/*`): `ArgumentNullException` on every new public method with parameters (`ResilienceTelemetry` helpers).
3. **Contract** (`tests/Encina.ContractTests/`): pin `SourceName`, `MeterName`, tag keys and instrument names (possible because the class is in core `Encina`).
4. **Property**: not required; no new state-machine logic.
5. **Integration** (`tests/Encina.IntegrationTests/Observability/Polly/EndToEndIntegrationTests.cs`, `.../Resilience/StandardResilienceEndToEndTests.cs`): a real Polly pipeline with a `MeterListener` and `ActivityListener`; no Docker.
6. **Load**: not required (instrumentation, not concurrent state); justification file only if the folder requires it.
7. **Benchmark** (`tests/Encina.BenchmarkTests/Encina.Polly.Benchmarks/` new `TelemetryBenchmarks.cs`; `Encina.Extensions.Resilience.Benchmarks/`): no per-call allocation with no listener and with Polly's meter unsubscribed, enricher reading the request context without allocation (ADR-055 R5, ADR-054 settled point 7).
8. **Manifests** (`.github/coverage-manifest/Encina.Polly.json`, `Encina.Extensions.Resilience.json`, `Encina.OpenTelemetry.json`, and the core `Encina` manifest): per-flag targets with one-sentence justifications for each new or changed file, through `mechanical-fixer`; targets never go down. CRAP of every added or changed method must be at most 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
CONTEXT:
Tests live in the consolidated projects under tests/ (UnitTests, GuardTests, ContractTests, IntegrationTests, BenchmarkTests). Assertions use Shouldly through Encina.Testing.Shouldly; never FluentAssertions. Coverage is per flag against .github/coverage-manifest/{Package}.json.

TASK:
Write the tests of this plan's Phase 8 list so every new or changed src file reaches its per-flag target, including MeterListener/ActivityListener assertions for retry, breakers, bulkhead and rate limiting, a contract test pinning names, an end-to-end test against a real Polly pipeline, and a BenchmarkDotNet benchmark proving zero per-call allocation without a listener.

KEY RULES:
- Tests execute real package code; no reflection-only tests.
- Time through FakeTimeProvider; no Thread.Sleep.
- BenchmarkSwitcher.FromAssembly(...).Run(args, config), never BenchmarkRunner.Run<T>(); outputs under artifacts/performance/.
- Measure per-flag coverage with the command in AGENTS.md section 9 and report it against the manifest targets; CRAP <= 10 on changed methods.

REFERENCE FILES:
- tests/Encina.UnitTests/Polly/RetryPipelineBehaviorTests.cs
- tests/Encina.IntegrationTests/Observability/Polly/EndToEndIntegrationTests.cs
- tests/Encina.BenchmarkTests/Encina.Polly.Benchmarks/RetryBenchmarks.cs
- .github/coverage-manifest/Encina.Polly.json
```

</details>

### Phase 9: Documentation and Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML doc comments on every new public API (`ResilienceTelemetry` and its helpers), with `<summary>`, `<remarks>`, `<param>`, `<returns>` and `<example>` where useful.
2. `changelog.d/728-resilience-telemetry.added.md` (fragment; written by `mechanical-fixer`; never edit `CHANGELOG.md`); a second fragment `728-resilience-telemetry-retry-counter.removed.md` for the removed `encina.retry.attempts_total` counter and `Encina.Retry` activity.
3. Package READMEs `src/Encina.Polly/README.md` and `src/Encina.Extensions.Resilience/README.md`: how to subscribe with `AddMeter("Encina.Resilience")`, `AddMeter("Polly")` and `AddSource("Encina.Resilience")` (written by `docs-writer`).
4. A configuration section in the observability guide under `docs/` covering subscription and the tenant-tag cardinality note (cross-linked with #906) (`docs-writer`).
5. `ROADMAP.md`: not affected. `docs/INVENTORY.md`: update for the new files (`docs-writer`). ADR: none; ADR-054 and ADR-055 already record the decisions.
6. `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` of `Encina`, `Encina.Polly` and `Encina.Extensions.Resilience` complete (through `mechanical-fixer`).
7. Build verification: `dotnet build --configuration Release` with 0 errors and 0 warnings. Test verification: `dotnet test` passes and every coverage flag reaches its own target in `.github/coverage-manifest/{Package}.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
CONTEXT:
Issue #728 is done when the code of Phases 1-8 is merged. Documentation follows the encina-docs skill; CHANGELOG.md is never edited by hand.

TASK:
Delegate: changelog fragments, PublicAPI lines and coverage manifest entries to mechanical-fixer; README and guide pages to docs-writer. Ensure XML docs on every new public API and run the full build and test verification.

KEY RULES:
- Documentation never types coverage figures by hand; it cites covref markers.
- English only; no emojis; no private application names (write "the reference application" generically).
- Public API tracked in PublicAPI.*.txt; zero warnings.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
- src/Encina.Polly/README.md, src/Encina.Extensions.Resilience/README.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Standard | Relevance |
|----------|-----------|
| [ADR-055](../architecture/adr/055-resilience-family.md) R5 | Polly's instruments enriched by Encina, `Encina.Resilience` spans, Encina-owned instruments only for gaps, removal of the `encina.retry.*` counters, benchmark for zero cost |
| [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) | D3 names, D4 tenant tag, settled points 2, 4 and 7 (no message text, instrument-name rules, no cost without a listener) |
| ADR-018 / AGENTS.md section 6 | Twelve-function cross-cutting check |
| ADR-021 | EventId ranges registered first in `EventIdRanges.cs` |
| SPEC-002 AC-044 | Tenant tag on spans and metrics when multi-tenancy is on |
| SPEC-000 DEC-009 | The feature is in the 1.0 scope (milestone v0.14.0) |
| Polly v8 telemetry | Meter `Polly`, instruments `resilience.polly.strategy.events`, `resilience.polly.strategy.attempt.duration`, `resilience.polly.pipeline.duration` |
| OpenTelemetry .NET | `ActivitySource`, `Meter`, `MeterListener` and `ActivityListener` semantics |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `RetryMetrics` | `src/Encina/Recoverability/RetryMetrics.cs` | `RecordAttempt` removed; `RecordSkipped` kept for Messaging (Design Choice 2) |
| `RetryTelemetry` | `src/Encina.Polly/Diagnostics/RetryTelemetry.cs` | Replaced by `ResilienceTelemetry` and deleted |
| Pipeline behaviors | `src/Encina.Polly/Behaviors/*.cs`, `src/Encina.Extensions.Resilience/Behaviors/StandardResiliencePipelineBehavior.cs` | Instrumented in Phases 3 and 4 |
| `BulkheadManager`, `IBulkheadManager`, `BulkheadMetrics` | `src/Encina.Polly/Bulkhead/` | Source of rejection counts and the concurrency gauge |
| `AdaptiveRateLimiter`, `IRateLimiter`, `RateLimitState` | `src/Encina.Polly/RateLimiting/` | Source of the state and state-change instruments |
| `WithEncina()` | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` | Subscription of the new source and meters |
| `IEncinaHealthCheck`, `EncinaHealthCheck` | `src/Encina.Messaging/Health/` | Base of the throttled-state check |
| `IRequestContext.TenantId` | `src/Encina/Abstractions/IRequestContext.cs` | Tenant tag source |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Polly 3900-3949, ExtensionsResilience 3950-3999 |
| Coverage manifests | `.github/coverage-manifest/Encina.Polly.json`, `Encina.Extensions.Resilience.json`, `Encina.OpenTelemetry.json` | Per-flag targets for new and changed files |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Polly` | 3900-3949 | 3900-3921 in use; 3922-3949 free. No new log message planned |
| `Encina.Extensions.Resilience` | 3950-3999 | 3950-3955 in use; 3956-3999 free. No new log message planned |
| `Encina.OpenTelemetry` | 7000-7099 | Not used by this feature |
| New range | none | Needed only if a review adds a log line; it is then registered first in `EventIdRanges.cs` |

### File Count Estimate

| Category | New | Modified | Notes |
|----------|-----|----------|-------|
| Production code | 5 | 9 | New: `ResilienceTelemetry`, `PollyTelemetryEnricher`, `PollyTelemetry`, `ResilienceTelemetryEnricher`, `RateLimiterThrottledHealthCheck`. Modified: 6 behaviors, `RetryMetrics`, `WithEncina()` registration, `Encina.Extensions.Resilience` registration. Deleted: `RetryTelemetry.cs` |
| PublicAPI and manifests | 0 | 6 | `PublicAPI.Unshipped.txt` x3, coverage manifests x3 (through `mechanical-fixer`) |
| Unit tests | 2 | 8 | Existing behavior tests extended |
| Guard tests | 1 | 2 | |
| Contract tests | 1 | 0 | Pins names |
| Integration tests | 0 | 2 | Existing end-to-end tests extended |
| Benchmarks | 1 | 1 | Zero-allocation benchmark |
| Documentation | 0 | 5 | Two READMEs, observability guide, `docs/INVENTORY.md`, two changelog fragments |
| **Total** | **~10** | **~33** | About 2,000-2,400 changed lines over four pull requests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing OpenTelemetry instrumentation of the Encina resilience packages for Issue #728 (https://github.com/dlrivada/Encina/issues/728), following ADR-055 R5 and ADR-054 D3/D4.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always; no [Obsolete], no compatibility layers.
- Railway Oriented Programming: Either<EncinaError, T>; EncinaError.Message never reaches logs, tags or health results.
- Resilience category: Encina.Polly, Encina.Extensions.Resilience (Encina.Http is deferred to #1233).
- ADR-055 R1 will replace both packages with Encina.Resilience; this work lands in them now and reuses a shared class in core.

IMPLEMENTATION OVERVIEW:
PR-1 (Phases 1-3 retry and breakers): src/Encina/Diagnostics/ResilienceTelemetry.cs; Polly enricher; Retry, CircuitBreaker, DatabaseCircuitBreaker behaviors; delete RetryTelemetry.cs and RetryMetrics.RecordAttempt.
PR-2 (shed load, OpenTelemetry): bulkhead and rate-limit instruments; state-change counter/gauge (waits for #2274); WithEncina() subscribes Encina.Resilience source/meter and the Polly meter.
PR-3 (Phases 4, 8, 9): StandardResiliencePipelineBehavior + enricher; contract, integration, benchmark tests; READMEs, guide, changelog.
PR-4 (Phase 6): RateLimiterThrottledHealthCheck (after #2274).
Pending maintainer decisions: Design Choices 1 (class in core), 2 (keep RecordSkipped for Messaging), 3 (health check in Encina.Polly).

KEY PATTERNS:
- Source/meter "Encina.Resilience"; instruments lowercase dotted, no _total; tenant tag encina.tenant_id only when TenantId is not null.
- HasListeners() guards; no allocation without a listener; verified by a benchmark.
- No enable/disable switches; apps subscribe with AddMeter/AddSource.
- Tests: Shouldly via Encina.Testing.Shouldly, FakeTimeProvider, MeterListener/ActivityListener; per-flag coverage targets from .github/coverage-manifest; CRAP <= 10 on changed methods.
- Changelog fragments, PublicAPI lines and manifests through mechanical-fixer; docs through docs-writer.

REFERENCE FILES:
- docs/architecture/adr/055-resilience-family.md, docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
- src/Encina.Polly/Behaviors/*.cs, src/Encina.Polly/Diagnostics/RetryTelemetry.cs, src/Encina/Recoverability/RetryMetrics.cs
- src/Encina.Extensions.Resilience/Behaviors/StandardResiliencePipelineBehavior.cs
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- docs/plans/resilience-telemetry-implementation-plan-728.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: resilience telemetry reads no data that benefits from caching; the Polly pipelines are already cached per request type. |
| 2 | OpenTelemetry | ✅ | The feature itself: `ActivitySource("Encina.Resilience")`, `Meter("Encina.Resilience")`, enriched `resilience.polly.*`, `WithEncina()` subscription (Phases 1-5, 7). |
| 3 | Structured Logging | ✅ | Existing `[LoggerMessage]` statements (EventIds 3900-3921 and 3950-3955) stay; no new message and no new range is needed. Any later addition is registered first in `EventIdRanges.cs` (ADR-021). |
| 4 | Health Checks | ✅ | `RateLimiterThrottledHealthCheck` (Phase 6, PR-4), after #2274; placement is a pending maintainer decision (Design Choice 3). |
| 5 | Validation | ❌ | Not applicable: the feature receives no input from callers; it only observes. |
| 6 | Resilience | ✅ | The instrumented subject: retry, circuit breaker, database circuit breaker, bulkhead and rate limiting (Phases 3 and 4). |
| 7 | Distributed Locks | ❌ | Not applicable: no shared mutable state is introduced; the gauges read in-process state only. |
| 8 | Transactions | ❌ | Not applicable: telemetry has no atomicity requirement and never changes control flow. |
| 9 | Idempotency | ❌ | Not applicable: no operation is repeated by this feature; the idempotency rules of retry (`RetryAttribute`, `RetryNonIdempotent`) are unchanged. |
| 10 | Multi-Tenancy | ✅ | Tenant as span attribute and metric tag only when multi-tenancy is on (ADR-054 D4, SPEC-002 AC-044); cardinality note in the guide (#906) (Phase 6, task 1). |
| 11 | Module Isolation | ❌ | Not applicable: resilience pipelines are keyed by request type and the R5 enrichment lists request type, error code and tenant only; there is no module key in `IRequestContext` for these behaviors. |
| 12 | Audit Trail | ❌ | Not applicable: telemetry is operational, not a compliance record; no state change is audited by this feature. |

---

## Prerequisites & Dependencies

| Item | State | Effect on this plan |
|------|-------|---------------------|
| [#2277](https://github.com/dlrivada/Encina/issues/2277) | open | Removes the dead `EnableTelemetry` and `EnableLogging`; merge before PR-1 to avoid editing the same file |
| [#2274](https://github.com/dlrivada/Encina/issues/2274) | open | Supplies the rate-limiter state-change event; PR-2's state-change counter and PR-4's health check wait for it |
| [#2414](https://github.com/dlrivada/Encina/issues/2414) | open | Rate-limiter consolidation; may move the limiter that PR-2 instruments |
| [#1787](https://github.com/dlrivada/Encina/issues/1787) | open | Architecture test that every source and meter is subscribed; this plan adds explicit entries to `WithEncina()` and must satisfy it |
| [#1233](https://github.com/dlrivada/Encina/issues/1233) | open | `Encina.Http`: its telemetry reuses the enricher registration (deferred) |
| [#143](https://github.com/dlrivada/Encina/issues/143) | open | Possible duplicate flagged by CodeRabbit; ADR-055 states that R5 resolves its telemetry part |
| [#2691](https://github.com/dlrivada/Encina/issues/2691) | closed | Source of decision R5 |
| `Encina.Resilience` package (ADR-055 R1) | planned, no issue found in the open list | This work lands in the two current packages and the shared class in core, so the future package inherits it |

---

## Next Steps

1. Review and approve this plan.
2. Link it from issue #728.
3. When work on #728 starts, put Design Choices 1, 2 and 3 to the maintainer and record the answers in a `## Maintainer Decisions` section placed last.
4. Merge #2277, then start PR-1 (Phases 1-3); each phase is a self-contained commit.
5. PR-2 and PR-4 wait for #2274; PR-3 follows PR-1.
6. The final PR references `Fixes #728`.
