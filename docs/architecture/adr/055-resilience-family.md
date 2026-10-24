# ADR-055: The Resilience Family: One Package, One Engine, One Retry Owner

## Status

**Accepted** - 2026-10-24, issue [#2691](https://github.com/dlrivada/Encina/issues/2691). The maintainer decided the ten open points of the spike on 2026-10-24, in the comment thread of the issue: R1 option 1a, R2 option 2a, R3 option 3a, R4 option 4b (with the amendment of [ADR-050](050-modules-may-require-modules.md) and the confirmed reading that Recoverability's immediate and delayed phases are one layer in two phases), R5 option 5b, R6 option 6c, R7 option 7a, R8 option 8b, D5 option D5a with (i), D7b option D7b-b. The evidence is in the [spike report](../../engineering/resilience-family-spike-2691.md).

Decision D4 of [#2414](https://github.com/dlrivada/Encina/issues/2414) (where the rate-limiting core lives) is resolved by R1: the core lives in `Encina.Resilience`. Decisions D1 to D3 of #2414 (2026-10-23) are inputs, recorded in the [spike report for #2414](../../engineering/rate-limiters-spike-2414.md).

## Context

Encina has two resilience packages for mediator requests, `Encina.Polly` and `Encina.Extensions.Resilience`, plus an immediate-retry loop hand-written in `Encina.Messaging` recoverability, plus Polly used directly by `Encina.Security.Secrets` and `Encina.Compliance.NIS2`, and a future `Encina.Http` over Microsoft.Extensions.Http.Resilience ([#1233](https://github.com/dlrivada/Encina/issues/1233)). Registering both packages raises no error and stacks two retries and two circuit breakers on one request. The two packages treat a business `Left` differently (one classifies it, the other converts it into an exception and retries it), no timeout cancels the work it abandons because the pipeline continuation has no token, and telemetry comes in four shapes. About twenty issues now in 1.0 ([SPEC-000](../../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) DEC-009) add hedging, chaos, timeouts, load shedding, adaptive concurrency, distributed limiter state and provider resilience, and several assume an `Encina.Resilience` package that does not exist. ADR-050 already forbids several retry layers on the same request, but no check implements it.

Nothing of either package has shipped (both `PublicAPI.Shipped.txt` files are empty), so the design is replaced completely, with no compatibility layer ([AGENTS.md](../../../AGENTS.md) sections 1 and 3). Section 2 of the spike report lists the measurements with file and line.

## Decision

### Settled points

These are not choices. Each has a citation in section 5 of the spike report; this ADR records them without reopening them.

| Point | Settled by |
| --- | --- |
| A strategy handles `Either` results directly; a `Left` is never converted into an exception to drive a strategy | [AGENTS.md](../../../AGENTS.md) section 3 (ROP); Polly v8 handles results through `ShouldHandle` |
| Every retry, in any front-end, retries only errors classified retryable and a command only when it is idempotent | [ADR-029](029-recoverability-error-classification.md); [#2411](https://github.com/dlrivada/Encina/issues/2411) (2026-10-17); ADR-050 resilience row |
| Several retry layers on the same request are a registration error | ADR-050 resilience row (the scope of "layer" is R4) |
| Every rejection is a coded `Left`: open circuit, timeout (`encina.timeout`), rate limit (`encina.ratelimit.exceeded`), bulkhead full, shed load; delays travel as metadata | AGENTS.md section 3; ADR-029 |
| One partitioned rate-limiting core over `PartitionedRateLimiter`, request type by tenant by default and fail-closed without tenant, adaptive throttling as an opt-in decorator that counts transient errors only | [#2414](https://github.com/dlrivada/Encina/issues/2414) D1 = 1a, D2 = 2a, D3 = 3a |
| Rate limiter and circuit breaker stay independent | [#2278](https://github.com/dlrivada/Encina/issues/2278) decision A |
| Circuit-breaker state lives in a per-container singleton, never in a static field | #2411 |
| `Encina.Http` is built on Microsoft.Extensions.Http.Resilience, partitions per client by tenant, and a 429 or 503 with `Retry-After` pauses that partition. The client is the named HTTP client, always known at registration; a call without tenant context is denied unless it opts out explicitly and the opt-out is logged (#2414 D3), never routed to a shared anonymous partition | #1233 (2026-10-17); SPEC-002 REQ-039, REQ-061; #2414 D3 |
| Telemetry follows [ADR-054](054-observability-conventions-and-trace-propagation.md): one `ActivitySource` and one `Meter` named `Encina.<Package>`, lowercase dotted instrument names, error code or exception type only, tenant tag only when multi-tenancy is on, no cost without a listener | ADR-054 settled points 2, 4, 7 and D3, D4 |
| No enable or disable switches for telemetry or logging; the enrichment switches proposed in #143 are out | [#2277](https://github.com/dlrivada/Encina/issues/2277), [#728](https://github.com/dlrivada/Encina/issues/728) (2026-10-16) |
| Time comes from `TimeProvider` | AGENTS.md section 3 |
| Stampede protection, fail-safe and eager refresh are caching-provider features on every cache, not resilience strategies | AGENTS.md section 5, caching row |
| Resilience for transports is consistent across all 10 transports; for cloud functions across the cloud triangle | AGENTS.md section 5; SPEC-000 DEC-009 ([#2692](https://github.com/dlrivada/Encina/issues/2692)) |
| Every capability is opt-in | AGENTS.md section 1 |
| Resilience behaviors declare their pipeline stage: `Admission` for limiters and bulkhead, `Resilience` for retry, timeout and breaker | `PipelineStage` already reserves both stages |

### R1. One package, `Encina.Resilience` (option 1a)

`Encina.Resilience` (planned; it does not exist in `src/` yet, and neither do the `Encina.Http` package, the shared `Retry-After` function and the `DelayGenerator` named below) replaces `Encina.Polly` and `Encina.Extensions.Resilience`, which are removed completely. It holds the engine (Polly v8), the request attributes, the options, the partitioned rate-limiting core of #2414 and every new resilience capability: load shedding, hedging, fallback, chaos and adaptive concurrency. `Encina.Http`, the providers, `Encina.Security.Secrets` and `Encina.Compliance.NIS2` reference `Encina.Resilience` instead of Polly directly. An issue that planned its own package (#138 `Encina.LoadShedding`, or any per-capability package) puts its code here.

Reason: Polly is already a dependency of both current packages, of Secrets, of NIS2 and, through Http.Resilience, of `Encina.Http`, so a split core saves little. One package gives the retry-layer conflict check (R4) and the telemetry (R5) a single owner.

### R2. One Polly pipeline per request type, compiled at startup, in a fixed order (option 2a)

At startup, the attributes and options of each request type compile into one `ResiliencePipeline<Either<EncinaError, T>>`, kept in Polly's `ResiliencePipelineRegistry`. Attributes are read once; no pipeline is rebuilt per call. Two behaviors consume it: one at `PipelineStage.Admission` (rate limiter and bulkhead) and one at `PipelineStage.Resilience`.

The order is fixed:

```text
rate limiter, total timeout, retry (or hedging instead), circuit breaker, per-attempt timeout, chaos last
```

The hand-written bulkhead becomes a concurrency partition of the #2414 rate-limiting core, and `BulkheadManager` is deleted. A user-defined strategy is written as a Polly strategy. Retry delays come from Polly's delay types or a single `DelayGenerator` (D5, D7b).

Reason: it is the only option that guarantees the order without relying on the user, reads attributes once, fits R1 and removes the hand-written bulkhead with its defects.

### R3. Attributes and options feed one model (option 3a)

Two front-ends produce the same per-type options, which R2 compiles once at startup. The request attributes are `[Retry]`, `[CircuitBreaker]`, `[Timeout]`, `[RateLimit]`, `[Bulkhead]` and `[Hedging]`, among others. The options are written in code or `appsettings.json` as `AddEncinaResilience(o => o.Default(...).For<T>(...))`. `[Retry]`, `[CircuitBreaker]`, `[RateLimit]` and `[Bulkhead]` exist today in `Encina.Polly` and move to `Encina.Resilience`; `[Timeout]`, `[Hedging]` and `AddEncinaResilience` are planned.

- **Precedence:** options for a type > that type's attributes > global defaults. A test covers it.
- Options bind to `IConfiguration` and reload through Polly's registry, so an operator tunes an environment without recompiling and configures request types the user does not own, such as those shipped by Encina packages.
- Pipeline lifecycle: a type's pipeline is compiled once at startup (R2) and rebuilt only when its bound options change (a configuration change token), never per call; attributes are compile-time and never trigger a rebuild. The registry swaps the pipeline for new executions, executions already started finish on the pipeline they began with, and a rebuild that fails keeps the previous pipeline and is logged (Polly's dynamic-reload behavior, to be confirmed by a test in the implementation).
- The explicit, logged no-tenant opt-out of #2414 D3 is expressible in both front-ends.
- The bulkhead is configured like every other strategy, by attribute or by `For<T>()`; this answers #2631.
- Registration completeness (AGENTS.md section 3): every `AddEncina*` method of the package is proven with `ValidateOnBuild` and `ValidateScopes`.

Reason: it is the only option that covers per-environment tuning, foreign request types and visibility of the policy next to the request, at startup-only cost thanks to R2.

### R4. A single owner of immediate retries (option 4b)

`Encina.Messaging` Recoverability keeps only delayed retries and the dead letter queue. The immediate retry of a message is done by the `Encina.Resilience` pipeline of its request type, with the same classifier, idempotency rule, `Retry-After` handling and telemetry.

- **ADR-050 amendment:** Messaging requires `Encina.Resilience` when `RecoverabilityOptions.ImmediateRetries > 0`, enforced as a registration error. The row is added to the closed list of ADR-050 on the same date.
- **Confirmed reading of ADR-050:** Recoverability's immediate and delayed phases are one retry layer in two phases, as in NServiceBus, not two stacked layers.
- A retry inside an HTTP client used by a handler (`Encina.Http`) is a different operation, not the same request; it is documented, not detected.
- `RecoverabilityOptions.MaxJitterPercent` is removed (D5).

Reason: one immediate-retry engine is what R2 aims for; D5 and D7b get one home instead of three; it matches MassTransit's split of in-memory retry and redelivery.

### R5. Polly's instruments, enriched by Encina, plus Encina spans (option 5b)

- `Encina.Resilience` configures Polly's `TelemetryOptions` with an enricher that adds the request type, the error code and the tenant (only when multi-tenancy is on, ADR-054 D4). It adds no `EncinaError.Message` and no message or patient data (ADR-054 settled point 2; GDPR Art. 5(1)(c) and Art. 25).
- The request span gets events (retry, breaker state change, rejection) from `ActivitySource("Encina.Resilience")`.
- Encina creates its own instruments, named per ADR-054 D3 under `Meter("Encina.Resilience")`, only for what Polly does not measure: adaptive concurrency state, shed load and partition count.
- `WithEncina()` subscribes the `Polly` meter explicitly, so the mediator and `Encina.Http` share one set of `resilience.polly.*` metrics with no double counting (ADR-054 D6.2).
- The current `encina.retry.attempts_total` and `encina.retry.skipped_total` counters, and the loose use of `Meter("Encina")` and `ActivitySource("Encina")` by the resilience code, go away in the same change. No activation switches.
- To verify in the implementation, with a benchmark: the enricher reads the request context without a per-call allocation (ADR-054 settled point 7).

Reason: `Encina.Http` makes `resilience.polly.*` unavoidable, so one shared set of instruments avoids a user seeing two families for the same strategies.

### R6. The continuation carries the token; the context carries the deadline (option 6c)

`RequestHandlerCallback<TResponse>` takes a `CancellationToken`, written `RequestHandlerCallback<TResponse>(CancellationToken)` (planned; today the delegate has no parameter). Every behavior passes down the token the rest of the pipeline must use, so Polly's timeout and hedging tokens reach the handler. `IRequestContext` gains an absolute deadline (UTC, computed with `TimeProvider`; planned); the timeout behavior shortens it, and outbound clients (HTTP, gRPC, database) propagate it, the earliest deadline winning, as gRPC's call-context propagation does. A timeout is reported with its error code `encina.timeout` (ADR-054).

All 111 files in `src/` that implement `IPipelineBehavior<,>` and the `nextStep()` call sites in `src/` and `tests/` change in one mechanical pass, before more behaviors are added. There is no compatibility overload.

Reason: it is the only option under which #335 (timeout), #141 (cascading timeouts) and #136 (hedging cancels the losing attempt) work as their issues describe. A timed-out first attempt no longer keeps running and sending.

### R7. Provider resilience through decorators from `Encina.Resilience` (option 7a)

Decorators wrap `IMessageTransport` (all 10 transports, #742), the cloud function adapters (AWS, Azure, GCP, #744), gRPC and GraphQL (#745) and the shard executor (#643). Each uses a pipeline from the R2 registry keyed by provider name or shard id, so a per-shard circuit breaker is just a key. One options model (R3) and one telemetry (R5) apply; one shared test suite validates the decorator once for every provider of a category (AGENTS.md section 5).

The decorators cover operations (publish, consume, call, shard query). Connection recovery stays inside each SDK and is documented per provider. The SDK's own retry is disabled wherever Encina retries (ADR-050, one retry layer), and only retryable failures are retried (ADR-029). The precedent is `ResilientSecretReaderDecorator` in `Encina.Security.Secrets`.

To verify per provider during the implementation: which SDKs retry by default and how to disable it. #643 lists 13 providers where AGENTS.md lists 10 database providers; that issue corrects it.

Reason: coherence across the 10 transports is built in, and the decorator is proved once.

### R8. A rate limiter state store per cache (option 8b)

A new abstraction `IRateLimiterStateStore` (planned) is implemented in each cache package with its best atomic operation: Lua scripts in the Redis family (Redis, Valkey, Dragonfly, Garnet, KeyDB), compare-and-swap in Memcached ([#277](https://github.com/dlrivada/Encina/issues/277)), local state in Memory and the Hybrid L1. `ICacheProvider` stays as it is. The distributed limiter keeps the exact algorithm chosen in #2414 (the partitioned core, sliding window or token bucket), following the RedisRateLimiting pattern of `System.Threading.RateLimiting` over store-side atomic scripts.

- `Encina.Resilience` depends only on the abstraction, never on a concrete cache.
- If the cache fails, the limiter falls back to the local limiter and logs it (#704). A request without tenant context is still denied (#2414 D3).
- Counter keys carry only the type, tenant and client (the partition key), no personal data (GDPR Art. 5(1)(c), ADR-054 D6.3). Spans and metrics exist on all 8 caches per ADR-054 D6.

Reason: each cache uses its best atomic operation while the `ICacheProvider` contract stays small, and the algorithm stays exact.

### D5. Polly's jitter formulas as they are; no jitter on delayed retries (option D5a with (i))

- Immediate retries use Polly's own delay types with `UseJitter`: Exponential uses Decorrelated Jitter V2; Constant and Linear use plus or minus 25 % (verified in Polly 8.6.6 `RetryHelper.cs`). There is no Encina-owned jitter formula, and the behavior is the same as `Encina.Http` gets through Http.Resilience.
- Documentation states that Decorrelated V2's real medians for the first retries are shorter than the configured exponential steps, and that the base, not the median, is what is configured.
- **(i)** `RecoverabilityOptions.MaxJitterPercent` is removed and no delayed retry without `Retry-After` gets jitter: the minute-level steps and the polling scheduler already spread them. When the server sent `Retry-After`, the upward-only jitter of D7b applies instead. The test `Handle_JitterAboveHundredPercent_FloorsDelayAtZero` ([#2714](https://github.com/dlrivada/Encina/issues/2714)) goes with the option.

Reason: it is the formula Polly chose for fewer retry peaks than AWS Full Jitter, it needs no code of ours, and it gives one behavior across the library.

### D7b. Upward-only jitter on top of Retry-After (option D7b-b)

When a server dictates the wait (`Retry-After` on 429 or 503), the delay is `Retry-After + rand(0, 10 % x Retry-After)`, capped at +30 s. It is never shorter than what the server asked. One shared function in `Encina.Resilience` applies it in the three places that honour `Retry-After`: the retry `DelayGenerator` of the per-type pipeline (immediate retries), the first delayed retry and `DelayedRetryProcessor`. Without `Retry-After`, Polly's formulas apply (D5). The percentage and the cap are documented defaults; the random source is thread-safe (`Random.Shared`, as in #2714) and time comes from `TimeProvider`. It fits the partition pause on 429 or 503 of #2414, which releases every waiter at once, and keeps the `Retry-After` cap of [#2588](https://github.com/dlrivada/Encina/issues/2588).

Reason: it is the only option that spreads the herd without disobeying the server (RFC 9110 section 10.2.3).

## Alternatives considered

Each option and the scenario where it wins is in section 7 of the [spike report](../../engineering/resilience-family-spike-2691.md).

- **R1, a Polly-free `Encina.Resilience` plus `Encina.Resilience.Polly` (1b)**: rejected. Two packages to explain, and most users reference both. Its benefit, limits and shedding without Polly, is small because Polly is a dependency anyway.
- **R1, keep `Encina.Polly` and `Encina.Extensions.Resilience` with new roles (1c)**: rejected. Two packages stay for one concept and the confusion of the context section is reduced, not removed.
- **R1, one package per capability (1d)**: rejected. About eight packages with a README, manifest and tests each, and a conflict check that spans packages.
- **R2, one behavior per strategy (2b)**: rejected. One Polly pipeline per strategy per call, ordering depends on stage numbers and two strategies of one kind can still stack.
- **R2, Encina's own engine (2c)**: rejected. It re-implements hedging, chaos and telemetry that Polly has.
- **R3, options only (3b)**: rejected. The policy is no longer visible on the request.
- **R3, attributes only (3c)**: rejected. No per-environment tuning and no way to configure a type the user does not own.
- **R4, detect and reject both retries at startup (4a)**: rejected. Two retry implementations and two option models would stay.
- **R4, treat recoverability's immediate retries as outside the rule**: dropped, because ADR-050 forbids several retry layers on the same request without exception.
- **R5, Encina instruments only (5a)**: rejected. `Encina.Http` emits the Polly instruments anyway, so a user would see two families for the same strategies.
- **R5, emit both families for the same events**: dropped, because ADR-054 D6.2 rules out duplicating the instrumentation of the library underneath.
- **R6, deadline and token in the request context only (6a)**: rejected. A handler that uses its own `cancellationToken` parameter is not cancelled, and there are two tokens to choose from.
- **R6, the continuation takes a token only (6b)**: rejected. There is no deadline to send to the next service, so #141 would not work.
- **R7, each provider package builds its own pipeline (7b)**: rejected. Ten transport implementations to keep coherent by review.
- **R7, native SDK resilience plus an Encina breaker (7c)**: rejected. Behavior differs per SDK, against the category rule, and ADR-029 classification is not applied.
- **R8, an atomic primitive on `ICacheProvider` (8a)**: rejected. Sliding windows need more than an increment, and Memory and Hybrid state stays local.
- **R8, a distributed lock around get and set (8c)**: rejected. Two round trips plus a lock per request, and lock providers are a different category.
- **D5, a `DelayGenerator` with Full Jitter (D5b), Equal Jitter (D5c) or the current plus or minus percentage (D5d)**: rejected. Each is code of ours to keep and test and a second model next to `Encina.Http`; Full Jitter gives some waits near zero against a failed gateway; the percentage windows are narrow.
- **D5, jitter on every delayed retry (ii), or only on the first as today (iii)**: rejected. (ii) adds an option to validate; (iii) is incoherent.
- **D7b, the exact `Retry-After` (D7b-a)**: rejected. Every client in a partition returns at the same instant, which the partition pause of #2414 makes more likely.
- **D7b, symmetric jitter around `Retry-After` (D7b-c)**: rejected. About half of the retries arrive before the time the server asked, which provokes more 429 responses.

## Consequences

### Positive

- One package, one engine and one retry owner for every request, message, transport and shard; the conflict between layers can be checked in one place.
- The order of strategies is fixed and the attributes are read once, not per call.
- A timeout and a hedged attempt cancel the work they abandon.
- One set of metrics across the mediator, `Encina.Http` and Secrets, and one options model for attributes and configuration.
- The herd of retries is spread without disobeying a server's `Retry-After`.

### Negative

- Every user of a limiter takes a Polly dependency (R1).
- A custom strategy must be a Polly strategy (R2).
- The R6 signature change touches all 111 `IPipelineBehavior<,>` implementations in `src/` and the `nextStep()` call sites in `src/` and `tests/`.
- `Encina.Messaging` depends on `Encina.Resilience` when immediate retries are on (R4).
- Part of the resilience metrics live under the `Polly` meter, which `WithEncina()` must subscribe explicitly (R5).
- There are eight `IRateLimiterStateStore` implementations to write and test, one per cache (R8).
- Decorrelated Jitter V2 medians are shorter than the configured exponential steps and must be documented (D5).

### What each point implies for the code

The owners are the issues below; the scope lines are the new scope of each issue.

| Implication | Owner |
| --- | --- |
| Absorbed and closed when the implementation lands: `BulkheadManager` is deleted (R2) | [#2628](https://github.com/dlrivada/Encina/issues/2628), [#2629](https://github.com/dlrivada/Encina/issues/2629), [#2633](https://github.com/dlrivada/Encina/issues/2633), [#2634](https://github.com/dlrivada/Encina/issues/2634) |
| Absorbed and closed when the implementation lands: attributes are read once (R2) | [#2632](https://github.com/dlrivada/Encina/issues/2632) |
| Absorbed and closed when the implementation lands: attribute validation moves to the R3 options model | [#2630](https://github.com/dlrivada/Encina/issues/2630) |
| Resolved by this ADR and closed with it: the bulkhead is configured by attribute or `For<T>()` (R3) | [#2631](https://github.com/dlrivada/Encina/issues/2631) |
| Already closed as superseded by D5: its `TimeProvider` part disappears with R4 and its validation part with `MaxJitterPercent` | [#2745](https://github.com/dlrivada/Encina/issues/2745) |
| Scope change: the code goes into `Encina.Resilience`; there is no `Encina.LoadShedding` package (R1) | [#138](https://github.com/dlrivada/Encina/issues/138) |
| Scope change: `Encina.Resilience` is now the real package | [#141](https://github.com/dlrivada/Encina/issues/141) (also depends on R6), [#143](https://github.com/dlrivada/Encina/issues/143) (loses its enable switches per #728 and #2277; its telemetry follows R5) |
| Scope change: the work is in `Encina.Resilience` | [#145](https://github.com/dlrivada/Encina/issues/145), [#136](https://github.com/dlrivada/Encina/issues/136) |
| Scope change: depends on the R6 continuation token and deadline | [#335](https://github.com/dlrivada/Encina/issues/335), [#136](https://github.com/dlrivada/Encina/issues/136) |
| Scope change: transport, cloud, gRPC and GraphQL, and shard resilience are decorators over the R2 registry (R7) | [#742](https://github.com/dlrivada/Encina/issues/742), [#744](https://github.com/dlrivada/Encina/issues/744), [#745](https://github.com/dlrivada/Encina/issues/745), [#643](https://github.com/dlrivada/Encina/issues/643) |
| Scope change: the design is `IRateLimiterStateStore` per cache (R8) | [#704](https://github.com/dlrivada/Encina/issues/704) |
| Scope change: resilience telemetry follows R5 | [#728](https://github.com/dlrivada/Encina/issues/728) |
| Scope change: `Encina.Http` references `Encina.Resilience` (R1) | [#1233](https://github.com/dlrivada/Encina/issues/1233) |
| Scope change: its retry rules apply to the single engine | [#2411](https://github.com/dlrivada/Encina/issues/2411) |
| Defect of a removed package: absorbed by R1 to R3 and closed when `Encina.Extensions.Resilience` is removed | [#2704](https://github.com/dlrivada/Encina/issues/2704) |
| Scope change: the option lives in core (`DatabaseResilienceOptions`), so removing the packages does not absorb it; whether it is removed or wired is settled when the database circuit breaker moves into the R3 options model of `Encina.Resilience` | [#2705](https://github.com/dlrivada/Encina/issues/2705) |
| Documentation is written after the implementation | [#904](https://github.com/dlrivada/Encina/issues/904) |
| The `AGENTS.md` section 5 resilience category row names `Encina.Resilience` | [#2692](https://github.com/dlrivada/Encina/issues/2692) |

## Related

- [Spike report for #2691](../../engineering/resilience-family-spike-2691.md), [spike report for #2414](../../engineering/rate-limiters-spike-2414.md)
- [ADR-029](029-recoverability-error-classification.md), [ADR-050](050-modules-may-require-modules.md) (amended by this ADR), [ADR-054](054-observability-conventions-and-trace-propagation.md), [SPEC-000](../../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md)
- Issues: [#2691](https://github.com/dlrivada/Encina/issues/2691), [#2414](https://github.com/dlrivada/Encina/issues/2414), [#2278](https://github.com/dlrivada/Encina/issues/2278), [#277](https://github.com/dlrivada/Encina/issues/277), [#2714](https://github.com/dlrivada/Encina/issues/2714), [#2588](https://github.com/dlrivada/Encina/issues/2588)

## Date

2026-10-24
