---
nav_exclude: true
---

# Resilience family: one design for Encina.Polly, Encina.Extensions.Resilience and Encina.Http (spike #2691)

This page is for the maintainer, who has to decide how the resilience packages of Encina are laid out before about twenty resilience issues enter 1.0, and for a contributor who needs the evidence behind that choice. It records what the code does today at `origin/main` 7b4a82bfe, what is already settled by a rule or a recorded decision, what other libraries do, where each capability could live, and the dilemmas that remain. Each dilemma ends with the spike's recommendation, which is input for the maintainer and not a decision: the issue stays open until the maintainer picks the options, and the decisions then get their own ADR (number assigned when it is written; 054 is the last one taken).

**Status:** evidence gathered on 2026-10-23 against the worktree at 7b4a82bfe (code inventory, issue bodies and comments, competitor documentation). **Issue:** [#2691](https://github.com/dlrivada/Encina/issues/2691). **Inputs:** the decisions D1-D3 of [#2414](https://github.com/dlrivada/Encina/issues/2414) (2026-10-23) and its [spike report](rate-limiters-spike-2414.md); decision D4 of #2414 (where the rate-limiting core lives) is part of dilemma R1 below. **Scope:** the 1.0 widening of 2026-10-23 ([SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) DEC-009, [#2692](https://github.com/dlrivada/Encina/issues/2692)).

Pre-1.0, backward compatibility and breaking-change cost are not criteria ([AGENTS.md](../../AGENTS.md) section 1). The public API of both packages is only in `PublicAPI.Unshipped.txt` (167 lines for `Encina.Polly`, 20 for `Encina.Extensions.Resilience`; both `PublicAPI.Shipped.txt` files hold only `#nullable enable`), so nothing has shipped.

## 1. The problem from the basics

**Resilience** is what a program does so that a failure of something it depends on (a database, an SMS gateway, a message broker) does not turn into a failure of the whole program. The usual tools, called **strategies**, are:

| Strategy | What it does | Kind |
| --- | --- | --- |
| Retry | Runs the operation again after a transient failure, waiting longer each time (backoff) | Reacts to a failure |
| Circuit breaker | After too many failures, stops calling the dependency for a while and fails fast, then lets a few calls test it again | Reacts to a failure |
| Timeout | Gives up when the operation takes too long, and should cancel the work it abandons | Prevents |
| Rate limiter | Lets only so many operations start per time window or per partition (decided in #2414) | Prevents |
| Bulkhead (concurrency limiter) | Lets only so many operations run at the same time, so one slow dependency cannot use every thread | Prevents |
| Hedging | Starts a second attempt when the first is slow and keeps whichever answers first | Reacts to slowness |
| Fallback | Returns a substitute result when the operation fails | Reacts to a failure |
| Chaos | Injects faults or latency on purpose, to test the other strategies | Testing |
| Load shedding, adaptive concurrency, backpressure | Reject or slow down incoming work when the process itself is overloaded | Prevents |

A **resilience pipeline** chains several strategies around one operation, in a fixed order (for example rate limiter, total timeout, retry, circuit breaker, attempt timeout). The order matters: a retry outside a circuit breaker lets the breaker see every attempt; a retry inside it does not.

A concrete story. A psychology practice runs the reference application ([SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md), scenario S11: several practices are tenants of one deployment). The application sends `SendAppointmentReminder` through `IEncina.Send`. The handler reads the patient's consent and contact data from the database and calls an SMS gateway over HTTP. Today:

- The developer adds `[Retry]` and `[CircuitBreaker]` from `Encina.Polly` to the command, and also calls `AddEncinaStandardResilience()` from `Encina.Extensions.Resilience` as well. Nothing warns. The Polly retry (`MaxAttempts = 3`) wraps the standard pipeline (3 retries, so up to 4 attempts), so one reminder can reach the gateway up to 12 times, and two circuit breakers with different thresholds count the same failures (section 2.3).
- The patient withdrew consent for SMS, so the handler returns a business `Left` ("no consent"). `Encina.Polly` does not retry it, because it asks `IErrorClassifier`. `Encina.Extensions.Resilience` converts the `Left` into an exception and retries it three times and counts it as a failure in its breaker (section 2.2). A business rule now opens a circuit.
- The standard pipeline's attempt timeout fires after 10 seconds, but the handler keeps running: the pipeline callback has no cancellation token, so the timeout abandons the work without cancelling it (section 2.4). The SMS may still be sent after the caller was told it timed out.
- The command also arrives as a message from a queue, and `Encina.Messaging` recoverability adds its own immediate retries: a third retry layer.
- When `Encina.Http` exists ([#1233](https://github.com/dlrivada/Encina/issues/1233)), the HTTP client to the gateway gets a fourth layer from Microsoft.Extensions.Http.Resilience, with its own retry, breaker and limiter.

Each layer was reasonable alone. Together they multiply attempts, disagree on what counts as a failure, and emit telemetry in four shapes. About twenty issues now in 1.0 would add hedging, chaos, timeouts, load shedding, adaptive concurrency, backpressure, distributed limiter state and provider-level resilience on top, and several of them assume a package, `Encina.Resilience`, that does not exist ([#141](https://github.com/dlrivada/Encina/issues/141), [#143](https://github.com/dlrivada/Encina/issues/143), [#145](https://github.com/dlrivada/Encina/issues/145)). This spike decides the shape they go into.

## 2. What the code does today

### 2.1 Two packages, same engine, different contracts

| | `Encina.Polly` | `Encina.Extensions.Resilience` |
| --- | --- | --- |
| Package references | `Polly`, `Polly.Extensions` (`src/Encina.Polly/Encina.Polly.csproj:13-18`) | `Polly`, `Polly.Extensions`, `Microsoft.Extensions.Resilience`, `Microsoft.Extensions.Http.Resilience` (`src/Encina.Extensions.Resilience/Encina.Extensions.Resilience.csproj:13-20`); no code uses a type from the two Microsoft packages (only Polly types were found) |
| Activation | Per request type, by attribute: `[Retry]`, `[CircuitBreaker]`, `[RateLimit]`, `[Bulkhead]`; every behavior reads the attribute by reflection on every call (`Behaviors/RetryPipelineBehavior.cs:64`, `Behaviors/BulkheadPipelineBehavior.cs:50`; [#2632](https://github.com/dlrivada/Encina/issues/2632)) | For every request, by registration: `AddEncinaStandardResilience()`; per request type with `AddEncinaStandardResilienceFor<TRequest, TResponse>()` |
| Engine | Polly v8 `ResiliencePipelineBuilder<Either<EncinaError, TResponse>>` for retry, circuit breaker and database circuit breaker (`RetryPipelineBehavior.cs:98`, `CircuitBreakerPipelineBehavior.cs:108`, `DatabaseCircuitBreakerPipelineBehavior.cs:129`); bulkhead and rate limiter are hand-written (`Bulkhead/BulkheadManager.cs:9`, `RateLimiting/AdaptiveRateLimiter.cs:23`) | One non-generic Polly v8 pipeline: rate limiter, total timeout (30 s), retry (3, exponential, jitter), circuit breaker (ratio 0.1), attempt timeout (10 s) (`Configuration/StandardResilienceOptions.cs:21-50`), kept in a `ResiliencePipelineRegistry<string>` keyed by request type name or `"Encina.StandardResilience"` (`ServiceCollectionExtensions.cs:12`, `:74-86`, `:126`) |
| Shape of the pipeline | One behavior per strategy, registered in the order Bulkhead, RateLimit, Retry, CircuitBreaker (`ServiceCollectionExtensions.cs:60-69`) | One behavior running the whole pipeline (`Behaviors/StandardResiliencePipelineBehavior.cs`) |
| Timeout | None | Total and per attempt |
| Bulkhead | `[Bulkhead]` with a `SemaphoreSlim` per key; seven open defects [#2628](https://github.com/dlrivada/Encina/issues/2628)-[#2634](https://github.com/dlrivada/Encina/issues/2634), and the configuration API is an open decision ([#2631](https://github.com/dlrivada/Encina/issues/2631)) | None (the Polly limiter strategy is a concurrency cap of 1000 by default, see the #2414 report) |
| Telemetry | Retry only: `ActivitySource("Encina")` (`Diagnostics/RetryTelemetry.cs:15`) and `Meter("Encina")` with `encina.retry.attempts_total` and `encina.retry.skipped_total` (`src/Encina/Recoverability/RetryMetrics.cs:17-26`); breaker, bulkhead and limiter only log. `EnableTelemetry` and `EnableLogging` are never read ([#2277](https://github.com/dlrivada/Encina/issues/2277)) | No `ActivitySource` and no `Meter`; logs only. Polly's own telemetry is not configured by either package |
| EventIds | 3900-3949, 3900-3921 used (`src/Encina/Diagnostics/EventIdRanges.cs:182`) | 3950-3999, 3950-3955 used (`EventIdRanges.cs:185`) |
| Tests | Unit 16 files, Guard 6, Integration 1, Benchmark 4; no Contract, Property or Load | Unit 3, Guard 1, Integration 1, Benchmark 1; no Contract, Property or Load |

Neither package references the other. Both are listed in `PipelineOrderValidator.BuiltInAssemblies` (`src/Encina/Pipeline/Ordering/PipelineOrderValidator.cs:105`, `:109`), and the core ordering model already reserves stages for them: `Admission = 200` "bulkhead and rate limiting" and `Resilience = 1100` "retry, timeout and circuit breaking" (`src/Encina/Pipeline/Ordering/PipelineStage.cs:44-47`, `:93-95`). No resilience behavior declares a stage, and rule R3 (a built-in step must declare one) is not in the default rule set (`PipelineOrderResolver.cs:65`), so today they all run in the `Application` stage in registration order.

### 2.2 How each package treats a `Left`

`Encina.Polly` gives Polly the `Either` itself as the result type. Its retry `ShouldHandle` skips errors that `IErrorClassifier` calls `Permanent`, skips non-idempotent requests through `OutboundRetryRules.AllowsAutomaticRetry` (`src/Encina/Recoverability/OutboundRetryRules.cs:33-44`), skips a retry whose `Retry-After` exceeds `MaxDelayMs`, and uses the server's `Retry-After` as the delay (`RetryPipelineBehavior.cs:145-189`). A `Left` never becomes an exception.

`Encina.Extensions.Resilience` throws an internal `EncinaResilienceException` for every `Left` so that Polly's default `ShouldHandle` (exceptions only) sees it, and unwraps it afterwards (`StandardResiliencePipelineBehavior.cs:75-77`, `:84-89`). Every business `Left` is therefore retried and counted by the breaker; there is no classifier, no idempotency check and no `Retry-After`. An open circuit and a timeout become uncoded `EncinaError.New(message, ex)` (`:90-103`).

Polly v8 does not need the exception: `ShouldHandle` receives the outcome, result or exception, and `PredicateBuilder<T>.HandleResult(...)` handles results ([Polly strategies](https://www.pollydocs.org/strategies/index.html)). `Encina.Polly` already uses that.

### 2.3 Registering both

Nothing detects it: both register open-generic `IPipelineBehavior<,>` (Polly with `AddTransient`, not deduplicated; Extensions with `AddScoped`, `ServiceCollectionExtensions.cs:89`) and no code of one mentions the other. With `AddEncinaPolly()` first, the order is Bulkhead, RateLimit, Retry, CircuitBreaker, then the standard pipeline, so the Polly retry wraps the standard retry (inferred from the code; no test combines them). [ADR-050](../architecture/adr/050-modules-may-require-modules.md) already forbids "several retry layers on the same request" with a registration error (resilience row), but the check is not implemented.

A third retry layer exists: `RecoverabilityPipelineBehavior` in `Encina.Messaging` (`src/Encina.Messaging/Recoverability/RecoverabilityPipelineBehavior.cs:37`) runs immediate retries with backoff and then schedules delayed retries, hand-written, without Polly.

### 2.4 Cancellation and timeouts

The pipeline continuation has no parameter: `public delegate ValueTask<Either<EncinaError, TResponse>> RequestHandlerCallback<TResponse>();` (`src/Encina/Abstractions/RequestHandlerDelegate.cs:9`). Every resilience behavior calls `nextStep()` and ignores the token Polly passes to its callback (`StandardResiliencePipelineBehavior.cs:68-72`, `RetryPipelineBehavior.cs:79`). The inner behaviors and the handler keep the token captured before the pipeline ran, so a Polly timeout returns to the caller but never cancels the work. `IRequestContext` carries an `IdempotencyKey` but no deadline. Timeout behavior ([#335](https://github.com/dlrivada/Encina/issues/335)), cascading timeouts ([#141](https://github.com/dlrivada/Encina/issues/141)) and hedging ([#136](https://github.com/dlrivada/Encina/issues/136), which must cancel the losing attempt) all depend on this.

### 2.5 Other resilience code

| Where | What | Engine |
| --- | --- | --- |
| `src/Encina/Recoverability/` | `IErrorClassifier` (Transient, Permanent, Unknown), `OutboundRetryRules`, `RetryAfterParser`, `OutboundErrorCodes`, `RetryMetrics` | none (shared rules) |
| `src/Encina.Messaging/Recoverability/` | Immediate and delayed retries, dead letter | hand-written |
| `src/Encina/Database/` and the provider health monitors (`DatabaseHealthMonitorBase.cs:51`) | Database health with an open or closed flag, read by `DatabaseCircuitBreakerPipelineBehavior`; `DatabaseResilienceOptions.EnableCircuitBreaker` (`DatabaseResilienceOptions.cs:55`) is never read | hand-written |
| `src/Encina/Sharding/Health/` | Per-shard health | hand-written |
| `src/Encina.Security.Secrets/Resilience/` | Timeout, retry and breaker decorators over secret readers, with their own metrics (`SecretsResiliencePipelineFactory.cs:41-50`) | Polly v8 directly |
| `src/Encina.Compliance.NIS2/NIS2ResilienceHelper.cs:23` | Internal helper | Polly v8 directly |
| `src/Encina.Refit/Handlers/RestApiRequestHandler.cs:105`, `:161-180` | Maps HTTP failures to `encina.outbound.*` codes with `Retry-After`; does not retry | none |
| `src/Encina.Caching.Redis/RedisCacheProvider.cs:133-154`, `Encina.Caching.Hybrid` | Stampede protection by lock key (Redis) or by HybridCache; no fail-safe | none |
| `src/Encina.InMemory/InMemoryMessageBus.cs:38-51`, `src/Encina.Cdc.Debezium/DebeziumHttpListener.cs:26-33` | Bounded channels (backpressure) | `System.Threading.Channels` |

Not found in `src/`: hedging, chaos, load shedding, adaptive concurrency, deadline propagation, an `Encina.Http` or `Encina.RateLimiting` package. Polly and the Microsoft packages are pinned centrally (`Directory.Packages.props:72-78`: Polly 8.6.6, Microsoft.Extensions.Resilience and Http.Resilience 10.5.0). `System.Threading.RateLimiting` is not pinned; on this machine it ships in the `Microsoft.AspNetCore.App` shared framework and not in `Microsoft.NETCore.App`, so a non-web package that uses it takes a NuGet dependency.

The only resilience page under `docs/` is `docs/features/database-resilience.md`; the comparison page is [#904](https://github.com/dlrivada/Encina/issues/904). No ADR covers the overlap.

## 3. The issues in scope and what they assume

| Issue | Asks for | Package it assumes |
| --- | --- | --- |
| [#740](https://github.com/dlrivada/Encina/issues/740) | Tenant-scoped rate limiting, tier quotas | Encina, Encina.Polly, Encina.AspNetCore |
| [#704](https://github.com/dlrivada/Encina/issues/704) | Limiter state shared across instances over `ICacheProvider`, local fallback on cache failure | Encina.Polly + Encina.Caching |
| [#154](https://github.com/dlrivada/Encina/issues/154) | Rate limit per scheduled message type in `ProcessDueMessagesAsync` | Encina.Messaging |
| [#143](https://github.com/dlrivada/Encina/issues/143) | Resilience tags and events on spans, meter `Encina.Resilience`, enable switches | "Encina.Resilience" (does not exist) |
| [#335](https://github.com/dlrivada/Encina/issues/335) | `TimeoutPipelineBehavior`, `[Timeout]`, fallback on timeout | core |
| [#136](https://github.com/dlrivada/Encina/issues/136) | `[Hedging]` over Polly `AddHedging` | Encina.Polly |
| [#145](https://github.com/dlrivada/Encina/issues/145) | Chaos behavior over Polly chaos strategies, environment gating | "Encina.Resilience" |
| [#139](https://github.com/dlrivada/Encina/issues/139) | Adaptive concurrency (Gradient, AIMD) integrated with the bulkhead | Encina.Polly |
| [#138](https://github.com/dlrivada/Encina/issues/138) | Load shedding by request priority and CPU | new `Encina.LoadShedding` |
| [#141](https://github.com/dlrivada/Encina/issues/141) | Timeout budget propagated to HTTP and database calls | "Encina.Resilience" |
| [#131](https://github.com/dlrivada/Encina/issues/131), [#144](https://github.com/dlrivada/Encina/issues/144) | Backpressure for producers and for streaming; both define a `BackpressureStrategy` with different members | core, Messaging; no streaming package named |
| [#742](https://github.com/dlrivada/Encina/issues/742) | Retry and breaker for the 10 transports, DLQ on exhaustion | Messaging, transports, Encina.Polly |
| [#744](https://github.com/dlrivada/Encina/issues/744) | Resilience for cloud functions, 429/503 retry | AzureFunctions, AwsLambda, GoogleCloudFunctions, Encina.Polly |
| [#745](https://github.com/dlrivada/Encina/issues/745) | gRPC and GraphQL retry, per-endpoint breaker, deadlines | Encina.Polly or Http.Resilience |
| [#643](https://github.com/dlrivada/Encina/issues/643) | One pipeline per shard, routing that skips open circuits | Encina.Polly |
| [#1233](https://github.com/dlrivada/Encina/issues/1233) | `Encina.Http`: auth, certificates, per client × tenant limiter honouring `Retry-After`, coded errors | new `Encina.Http` over Microsoft.Extensions.Http.Resilience |
| [#137](https://github.com/dlrivada/Encina/issues/137) | `[Fallback]`, fallback handlers, cached-value fallback | not stated |
| [#266](https://github.com/dlrivada/Encina/issues/266), [#268](https://github.com/dlrivada/Encina/issues/268) | Cache stampede protection; fail-safe and stale-while-revalidate | caching |
| [#2411](https://github.com/dlrivada/Encina/issues/2411), [#728](https://github.com/dlrivada/Encina/issues/728), [#2277](https://github.com/dlrivada/Encina/issues/2277), [#2628](https://github.com/dlrivada/Encina/issues/2628)-[#2634](https://github.com/dlrivada/Encina/issues/2634) | Defects and telemetry of today's packages | Encina.Polly, Encina.Extensions.Resilience |

Overlaps: rate limiting is spread over #740, #131, #154, #704 and the #2414 core; adaptive concurrency appears in #139 and in a section of #131; timeouts in #335, #141, #136 and #145; #742, #744 and #745 repeat the same retry and breaker design per provider family without a shared policy.

## 4. What other libraries do

### 4.1 Polly v8 and the Microsoft packages

| Component | Relevant facts | Source |
| --- | --- | --- |
| Polly v8 | Strategies: retry, circuit breaker, fallback, hedging (fallback and hedging only on the generic builder), timeout, rate limiter. Chaos strategies (fault, outcome, latency, behavior) are part of Polly since 8.3 and the docs place them last in the pipeline. `ShouldHandle` works on results without exceptions. `ResiliencePipelineRegistry<TKey>` holds named pipelines with `GetOrAddPipeline`, and reload through `AddReloadToken` (a failed reload keeps the old pipeline). No type named bulkhead: the concurrency limiter of `Polly.RateLimiting` over `System.Threading.RateLimiting` replaces it. A timeout throws `TimeoutRejectedException` | [strategies](https://www.pollydocs.org/strategies/index.html), [chaos](https://www.pollydocs.org/chaos/), [registry](https://www.pollydocs.org/pipelines/resilience-pipeline-registry.html), [rate limiter](https://www.pollydocs.org/strategies/rate-limiter.html) |
| Polly telemetry | Meter and logger named `Polly`; instruments `resilience.polly.strategy.events`, `resilience.polly.strategy.attempt.duration`, `resilience.polly.pipeline.duration`; events such as `OnRetry`, `OnCircuitOpened`, `OnRateLimiterRejected`, `Chaos.OnFault`; `TelemetryOptions` takes metering enrichers and listeners. No spans | [telemetry](https://www.pollydocs.org/advanced/telemetry.html) |
| Microsoft.Extensions.Resilience | Adds enrichment dimensions (`error.type`, `request.name`, `request.dependency.name`) on top of the Polly instruments; same `resilience.polly.*` names | [.NET resilience](https://learn.microsoft.com/en-us/dotnet/core/resilience/) |
| Microsoft.Extensions.Http.Resilience | `AddStandardResilienceHandler`, outermost first: rate limiter (1000, queue 0), total timeout 30 s, retry (3, exponential, jitter), circuit breaker (10 %, minimum 100, sampling 30 s, break 5 s), attempt timeout 10 s; handles 5xx, 408, 429. `ShouldRetryAfterHeader` defaults to true. `DisableForUnsafeHttpMethods()`. `AddStandardHedgingHandler` with per-authority breakers (`SelectPipelineByAuthority`). The docs say to add only one resilience handler per client and offer `RemoveAllResilienceHandlers()` | [HTTP resilience](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience), [ShouldRetryAfterHeader](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.http.resilience.httpretrystrategyoptions.shouldretryafterheader) |

`Encina.Extensions.Resilience` copies the standard HTTP handler's shape for mediator requests but uses none of the Microsoft APIs it references.

### 4.2 Resilience4j

One artifact per capability (circuitbreaker, ratelimiter, bulkhead with semaphore and thread-pool variants, retry, timelimiter, cache) plus `resilience4j-all` with a `Decorators` builder. In Spring Boot, annotations (`@CircuitBreaker`, `@Retry`, `@RateLimiter`, `@Bulkhead`, `@TimeLimiter`) and YAML configuration (`resilience4j.<type>.instances`) reach the same named instances: two configuration front-ends over one engine (the "same engine" reading is ours). The aspect order is fixed by default, Retry(CircuitBreaker(RateLimiter(TimeLimiter(Bulkhead(call))))), and can be changed with `*AspectOrder` properties. Metrics through Micrometer. ([getting started](https://resilience4j.readme.io/docs/getting-started), [Spring Boot](https://resilience4j.readme.io/docs/getting-started-3), [bulkhead](https://resilience4j.readme.io/docs/bulkhead)).

### 4.3 Messaging frameworks

| Framework | Configuration | Engine |
| --- | --- | --- |
| MassTransit | Fluent middleware at bus, endpoint or consumer level: `UseMessageRetry`, redelivery, circuit breaker, rate limiter, concurrency limit, kill switch; in-memory retries hold the message lock, long delays use redelivery | Own filter pipeline, no Polly ([exceptions](https://masstransit.massient.com/documentation/concepts/exceptions)) |
| NServiceBus | Recoverability: immediate retries, then delayed retries, then the error queue; unrecoverable exception types; no classic breaker, an automatic rate limit drops an endpoint to one message at a time after consecutive failures | Own ([recoverability](https://docs.particular.net/nservicebus/recoverability/)) |
| Wolverine | Attributes (`[RetryNow]`, `[MaximumAttempts]`) and fluent policies (`RetryWithCooldown`, `PauseThenRequeue`), per-endpoint circuit breaker; the docs advise one style | Own ([error handling](https://wolverinefx.net/guide/handlers/error-handling.html)) |
| Brighter | Handler pipeline with retry and breaker policies; migrated to Polly v8 | Polly (attribute details not verified) |

Every framework surveyed has one engine and one place where retry is decided per message. MassTransit and NServiceBus separate in-process immediate retries from broker-level delayed retries, as Encina's recoverability does.

### 4.4 Timeouts, overload and distributed limits

- gRPC for .NET: a call's deadline is an absolute UTC time that spans all retries; `EnableCallContextPropagation()` passes the deadline and cancellation of the incoming call to outgoing calls, and the smallest deadline wins ([deadlines](https://learn.microsoft.com/en-us/aspnet/core/grpc/deadlines-cancellation)). No general .NET library for propagating a remaining-time budget was found (search not exhaustive).
- Adaptive concurrency: Netflix concurrency-limits (Java; Vegas, Gradient2, AIMD) ([repository](https://github.com/Netflix/concurrency-limits)); no maintained .NET library was found. ASP.NET Core's rate-limiting middleware covers inbound fixed, sliding, token-bucket and concurrency limits ([rate limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)).
- Distributed limits: RedisRateLimiting implements the `System.Threading.RateLimiting` abstraction over Redis with Lua scripts ([repository](https://github.com/cristipufu/aspnetcore-redis-rate-limiting)); the abstraction stays the BCL one.
- FusionCache implements fail-safe, soft and hard factory timeouts, stampede protection and auto-recovery inside the cache, without Polly, and points to Polly for breakers ([repository](https://github.com/ZiggyCreatures/FusionCache)).

### 4.5 EU and Spanish law (not legal advice)

No law prescribes a resilience design. Resilience supports the availability and resilience of processing systems in GDPR Article 32(1)(b), the business-continuity and incident-handling measures of NIS2 (Directive (EU) 2022/2555) Article 21(2), and the availability dimension of the Esquema Nacional de Seguridad (Real Decreto 311/2022) for the Spanish public sector and its suppliers. Two points matter for the design. First, a retry that resends a health-related message after an ambiguous failure can disclose data twice; the idempotency rule of #2411 covers it. Second, chaos injection must never run unnoticed in production with real patient data; whether chaos is allowed in production at all is a configuration question of #145, and its gating must be explicit and logged like any other opt-out (SPEC-002 DEC-006).

## 5. Settled points

These are not dilemmas. Each has a citation; the ADR records them without reopening them.

| Point | Settled by |
| --- | --- |
| A resilience strategy handles `Either` results directly; a `Left` is never converted into an exception to drive a strategy. The throw in `StandardResiliencePipelineBehavior.cs:77` is a defect | [AGENTS.md](../../AGENTS.md) section 3 (ROP, no exceptions for business logic); Polly v8 handles results (section 4.1) |
| Every retry, in any front-end, retries only errors classified retryable and retries a command only when it is idempotent | [ADR-029](../architecture/adr/029-recoverability-error-classification.md); [#2411](https://github.com/dlrivada/Encina/issues/2411) decision (2026-10-17); [ADR-050](../architecture/adr/050-modules-may-require-modules.md) resilience row |
| Several retry layers on the same request are a registration error | ADR-050 resilience row (the scope of "layer" is dilemma R4) |
| Every rejection is a coded `Left`: open circuit, timeout (`encina.timeout`, `EncinaErrorCodes.cs:63`), rate limit (`encina.ratelimit.exceeded`, `:93`), bulkhead full, shed load; delays travel as metadata | AGENTS.md section 3; ADR-029 (classification by code); #2414 report section 5 |
| One partitioned rate-limiting core over `PartitionedRateLimiter`, request type × tenant by default and fail-closed without tenant, adaptive throttling as an opt-in SRE-style decorator counting transient errors only | [#2414](https://github.com/dlrivada/Encina/issues/2414) decisions D1 = 1a, D2 = 2a, D3 = 3a (2026-10-23) |
| Rate limiter and circuit breaker stay independent | [#2278](https://github.com/dlrivada/Encina/issues/2278) decision A (2026-10-16) |
| Circuit-breaker state lives in a per-container singleton, never in a static field | #2411 decision |
| `Encina.Http` is built on Microsoft.Extensions.Http.Resilience, partitions per client × tenant, and a 429 or 503 with `Retry-After` pauses that partition | [#1233](https://github.com/dlrivada/Encina/issues/1233) decision (2026-10-17); SPEC-002 REQ-039, REQ-061; #2414 report section 5 |
| Telemetry follows [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md): one `ActivitySource` and one `Meter` named `Encina.<Package>` per package, lowercase dotted instrument names, error code or exception type only, tenant tag when multi-tenancy is on, no cost without a listener | ADR-054 settled points 2, 4, 7 and D3, D4 |
| No enable or disable switches for telemetry or logging; the enrichment switches proposed in #143 are out | [#2277](https://github.com/dlrivada/Encina/issues/2277) and [#728](https://github.com/dlrivada/Encina/issues/728) decisions (2026-10-16) |
| Time comes from `TimeProvider`; the `DateTime.UtcNow` in the bodies of #141, #268 and #1233 is corrected when implemented | AGENTS.md section 3 |
| Stampede protection, fail-safe and eager refresh are caching-provider features implemented on every cache, not resilience strategies | AGENTS.md section 5, caching row |
| Resilience for transports is consistent across all 10 transports; for cloud functions across the cloud triangle | AGENTS.md section 5; SPEC-000 DEC-009 for GCP ([#2692](https://github.com/dlrivada/Encina/issues/2692) updates AGENTS.md) |
| Every capability is opt-in | AGENTS.md section 1 (pay-for-what-you-use) |
| The public API of both packages may be replaced completely, with no compatibility layer | AGENTS.md sections 1 and 3; nothing has shipped |
| Resilience behaviors declare their pipeline stage (`Admission` for limiters and bulkhead, `Resilience` for retry, timeout and breaker) | `PipelineStage.cs:44-47`, `:93-95` already reserve them; the default rule set not enforcing R3 is a separate gap |

### 5.1 Work that does not depend on the decisions, and work to hold

| Item | Note |
| --- | --- |
| [#2632](https://github.com/dlrivada/Encina/issues/2632), [#2411](https://github.com/dlrivada/Encina/issues/2411) retry classification | Needed in every option, but in R2 2a the behaviors it changes are rewritten; doing it first is thrown away |
| Bulkhead defects [#2628](https://github.com/dlrivada/Encina/issues/2628)-[#2634](https://github.com/dlrivada/Encina/issues/2634) | If R2 2a wins, the hand-written `BulkheadManager` is replaced by the BCL concurrency limiter inside the #2414 core and the defects of `BulkheadManager` itself (#2628, #2629, #2633, #2634) disappear with it; hold them until R2 is decided, as #2414 recommended for #2272, #2274, #2276 |
| [#2631](https://github.com/dlrivada/Encina/issues/2631) (bulkhead configuration API, needs-decision) | Answered by R3; close it into the ADR |
| Documentation [#904](https://github.com/dlrivada/Encina/issues/904) | Written after the ADR |
| Scope mismatch to reconcile (orchestrator) | The 2026-10-23 comment on [#879](https://github.com/dlrivada/Encina/issues/879) keeps caching items post-1.0, while the post-1.0 cut under DEC-009 puts milestones 23 and 24 (including #137, #266, #268, #278) in 1.0. #137 (fallback) has no 1.0 decision of its own. #643 cites 13 providers where AGENTS.md has 10 |

## 6. Capability-to-package map

The table places each capability under the three package layouts of dilemma R1 (option 1d, one package per capability, is too fine-grained to tabulate). "Core" is the `Encina` package; "Messaging" is `Encina.Messaging`.

| Capability | Issues | 1a: one `Encina.Resilience` | 1b: Polly-free core + Polly package | 1c: keep two packages with new roles |
| --- | --- | --- | --- | --- |
| Retry, circuit breaker, attempt and total timeout | #2411, #335 | `Encina.Resilience` | `Encina.Resilience.Polly` | `Encina.Extensions.Resilience` |
| Rate-limiting core + adaptive decorator (D4) | #2414, #740 | `Encina.Resilience` | `Encina.Resilience` | `Encina.Extensions.Resilience` |
| Bulkhead (concurrency limiter) | #2628-#2634 | `Encina.Resilience` | `Encina.Resilience` | `Encina.Extensions.Resilience` |
| Hedging, fallback, chaos | #136, #137, #145 | `Encina.Resilience` | `Encina.Resilience.Polly` | `Encina.Extensions.Resilience` |
| Load shedding, adaptive concurrency | #138, #139 | `Encina.Resilience` | `Encina.Resilience` | `Encina.Extensions.Resilience` |
| Attribute front-end (`[Retry]` and siblings) | #2631 | `Encina.Resilience` | `Encina.Resilience.Polly` | `Encina.Polly`, referencing `Encina.Extensions.Resilience` |
| Deadline and cancellation in the request context | #141, #335 | Core (see R6) | Core | Core |
| Distributed limiter state | #704 | See R8 | See R8 | See R8 |
| Backpressure for producers and streams | #131, #144 | Messaging and the transports (bounded channels exist there today, section 2.5) | same | same |
| Scheduled-message rate limit | #154 | Messaging, using the core limiter | same | same |
| Transport, cloud, gRPC and GraphQL resilience | #742, #744, #745 | See R7 | See R7 | See R7 |
| Per-shard resilience | #643 | See R7 | See R7 | See R7 |
| Outbound HTTP | #1233 | `Encina.Http` → `Encina.Resilience` | `Encina.Http` → `Encina.Resilience` (+ Http.Resilience) | `Encina.Http` → `Encina.Extensions.Resilience` |
| Stampede protection, fail-safe | #266, #268 | Caching packages (settled) | same | same |
| Telemetry | #728, #143 | See R5 | See R5 | See R5 |

Backpressure is placed in Messaging in every layout because #131 and #144 are about producers, consumers and streams, and the existing bounded channels live in the transports; it is a proposal, not a dilemma, and the two issues need one `BackpressureStrategy` before either is implemented.

## 7. Open decisions (dilemmas)

Effort is relative size only. Each option states the scenario in which it is the better fit. Options that break a rule are listed at the end of the dilemma with the reason.

### R1. Package layout, and where the rate-limiting core lives (#2414 D4)

| | 1a: one package `Encina.Resilience` | 1b: Polly-free `Encina.Resilience` + `Encina.Resilience.Polly` | 1c: keep `Encina.Polly` and `Encina.Extensions.Resilience` with new roles | 1d: one package per capability |
| --- | --- | --- | --- | --- |
| What it is | Replaces both packages: engine, both configuration front-ends, the rate-limiting core and every new strategy. `Encina.Http`, provider packages, `Encina.Security.Secrets` and `Encina.Compliance.NIS2` reference it instead of Polly directly | The core holds the abstractions, the rate-limiting core, bulkhead, load shedding and adaptive concurrency on `System.Threading.RateLimiting` only; the Polly package holds retry, breaker, timeout, hedging, fallback, chaos and the front-ends | `Encina.Extensions.Resilience` becomes the engine, options front-end and core (2414's 4a); `Encina.Polly` keeps only the attribute front-end and references it | `Encina.RateLimiting`, `Encina.LoadShedding`, `Encina.Hedging`, ... as several issues assume |
| Better when | The family is one concept for users, and one package with one README and one coverage manifest is easiest to document | Admission control (limits, shedding) must be usable without Polly, for example by `Encina.AspNetCore` or Messaging | The current names are valued and the change should touch the fewest files | Users are expected to take single capabilities and package size matters |
| Pros | No question of which package to pick; one place for the conflict check (R4) and the telemetry (R5); matches the "Encina.Resilience" several issues already assume | Limiter and shedding without Polly; clean split between "prevent" and "react" strategies | Smallest rename; 2414's dependency argument holds (`Encina.Http` sits in the Microsoft family) | Smallest dependency per user |
| Cons | Every user of a limiter takes Polly as a dependency; the package is large | Two packages to explain; the pipeline registry and the front-ends still need both, so most users reference both | Two packages stay for one concept; `Encina.Polly` becomes a thin shell; the confusion of section 1 is reduced but not removed | About eight packages, each with README, manifests and tests; the conflict check spans packages; the issue asks to avoid it unless justified |
| Effort | Large (rename, merge, move tests) | Large | Medium | Large, plus package overhead |

**Spike's recommendation: 1a.** Polly is already a dependency of every resilience package, of `Encina.Security.Secrets` and of `Encina.Compliance.NIS2`, and `Encina.Http` brings it through Microsoft.Extensions.Http.Resilience anyway, so the cost 1b avoids is small; one package is what makes the conflict check and the telemetry single-owner. Choose 1b if the maintainer wants load shedding or the limiter in `Encina.AspNetCore` or Messaging without Polly. Under 1a the answer to D4 is `Encina.Resilience`.

### R2. One engine, and how a request's strategies are composed

The engine question is almost answered by the code: retry and breaker already run on Polly v8 in both packages, and #2414 put the limiter on `PartitionedRateLimiter`. What remains is how a request's strategies are composed.

| | 2a: one pipeline per request type in a registry | 2b: one behavior per strategy | 2c: Encina's own engine |
| --- | --- | --- | --- |
| What it is | At startup, attributes and options for each request type are compiled into one Polly `ResiliencePipeline<Either<EncinaError, T>>` in a fixed order, kept in a `ResiliencePipelineRegistry`; one admission behavior (stage `Admission`) and one resilience behavior (stage `Resilience`) look it up. Bulkhead becomes a concurrency partition of the #2414 core | Today's `Encina.Polly` shape: each strategy is its own pipeline behavior with its own Polly pipeline, ordered by `PipelineStage` | Hand-written strategies, as `Encina.Messaging` recoverability, MassTransit, NServiceBus and Wolverine do |
| Better when | Users mostly combine several strategies on a request, and the order should be fixed by the library | Strategies are mostly used alone, and each should be replaceable as a behavior | Polly's exception and context model is judged a poor fit for ROP |
| Pros | One fixed order like Resilience4j and the Microsoft standard handler; attributes read once, not per call (#2632 disappears); reload of options supported by the registry; hedging and fallback need the generic pipeline anyway | Each strategy can be ordered, removed or replaced independently; smallest change from today | No third-party engine; full control |
| Cons | A request's pipeline is one unit: a custom strategy must be a Polly strategy | One behavior and one Polly pipeline per strategy per call; order between strategies depends on stage numbers; two strategies of the same kind can still stack | Re-implements what Polly has (hedging, chaos, telemetry); the four messaging frameworks needed years to mature theirs |
| Effort | Large | Medium | Very large |

**Spike's recommendation: 2a**, with the order fixed by the ADR (Polly and Microsoft order: limiter, total timeout, retry, breaker, attempt timeout; hedging replacing retry where configured; chaos last). Holding the bulkhead defects (section 5.1) follows from it.

### R3. Configuration front-ends

The bulkhead configuration API of #2631 (needs-decision) is a special case of this dilemma.

| | 3a: attributes and options, one model | 3b: options only | 3c: attributes only |
| --- | --- | --- | --- |
| What it is | `[Retry]`, `[CircuitBreaker]`, `[Timeout]`, `[RateLimit]`, `[Bulkhead]`, `[Hedging]` on the request, and `AddEncinaResilience(o => o.Default(...).For<TRequest>(...))` with `appsettings` binding; both produce the same options per request type | Code and configuration only, as Microsoft.Extensions.Resilience; attributes removed | Attributes only, plus global defaults |
| Better when | Some teams want the policy next to the request and others want it in configuration that operations can change | Policies change per environment and must not require a rebuild | Policies are part of the request's contract and never change per environment |
| Pros | Matches Resilience4j (annotations and YAML over the same instances) and Wolverine; covers both audiences | One source of truth; reloadable; no reflection | Visible in the code; simplest |
| Cons | Needs a precedence rule (proposed: options for a type override its attributes, which override global defaults) and a test for it; Wolverine advises using one style | Policy is no longer visible on the request | No per-environment tuning; no way to configure a type you do not own |
| Effort | Medium | Small | Small |

**Spike's recommendation: 3a**, with the precedence rule above recorded in the ADR. 3b is the honest alternative if the maintainer wants a single source of truth.

### R4. What counts as a retry layer, and recoverability's immediate retries

ADR-050 makes several retry layers on the same request a registration error. With R1 1a, two mediator resilience packages can no longer be registered together, so the remaining question is `Encina.Messaging` recoverability, which retries the same request in-process before it schedules a delayed retry. A retry inside a handler's HTTP client (`Encina.Http`) is a different operation, not the same request, and is not detectable at registration; it is documented.

| | 4a: detect and reject | 4b: one immediate-retry owner |
| --- | --- | --- |
| What it is | At startup, for each request type, an error when both the resilience retry and recoverability immediate retries are active | Recoverability keeps only delayed retries and the dead letter queue; immediate retries of a message come from the resilience retry (same classifier and idempotency rules) |
| Better when | The two retries are expected to diverge (recoverability is message-specific) | One retry engine is wanted everywhere, as R2 2a implies |
| Pros | Small; no change to recoverability | One retry implementation, one telemetry, one rule set; matches MassTransit's split (in-memory retry versus redelivery) |
| Cons | Two retry implementations stay, with two option models; users must choose per type | Recoverability must call the resilience pipeline, so Messaging depends on the resilience package when immediate retries are on (an ADR-050 conditional requirement) |
| Effort | Small | Medium |

Dropped: treating recoverability's immediate retries as outside the rule, because ADR-050 forbids several retry layers on the same request without exception.

**Spike's recommendation: 4b.** 4a is the cheaper fallback if Messaging must not depend on the resilience package.

### R5. Telemetry model

ADR-054 fixes names, attributes and the tenant tag. What it does not fix is who emits the strategy events: Polly already emits `resilience.polly.*` metrics and logs (no spans), and `Encina.Http` will emit them too through Microsoft.Extensions.Http.Resilience.

| | 5a: Encina instruments only | 5b: Polly's instruments, enriched, plus Encina spans |
| --- | --- | --- |
| What it is | `Encina.Resilience` meter with its own counters and histograms (attempts, rejections by reason, breaker state, duration) and spans; Polly telemetry left unconfigured | Encina configures Polly's `TelemetryOptions` with an enricher that adds request type, error code and tenant; the request span gets span events for retries, breaker transitions and rejections from `Encina.Resilience`; Encina adds its own instruments only for what Polly does not measure (adaptive decorator state, shed load, partition count) |
| Better when | Encina wants every instrument under its own name | One set of resilience metrics across mediator, HTTP and secrets is wanted |
| Pros | Names follow ADR-054 D3 everywhere | Mediator and `Encina.Http` metrics share one instrument set, as dashboards for Microsoft.Extensions.Http.Resilience already expect; no duplicated counting |
| Cons | `Encina.Http` emits Polly instruments anyway, so a user sees two families for the same strategies | Part of Encina's resilience metrics live under the `Polly` meter, which `WithEncina()` must subscribe explicitly |
| Effort | Medium | Medium |

Dropped: emitting both families for the same events, because ADR-054 D6.2 rules out duplicating the instrumentation of the library underneath (it names drivers; Polly is in the same position).

**Spike's recommendation: 5b**, because `Encina.Http` makes `resilience.polly.*` unavoidable. The existing `encina.retry.*_total` counters are renamed to the ADR-054 rules or removed in the same change.

### R6. Timeouts that cancel, and deadlines that propagate

The continuation `RequestHandlerCallback<TResponse>()` has no token (section 2.4), so no timeout can cancel the work it abandons, and hedging cannot cancel a losing attempt.

| | 6a: deadline and token in the request context | 6b: the continuation takes a token | 6c: both |
| --- | --- | --- | --- |
| What it is | `IRequestContext` gets a deadline and a cancellation token that a timeout behavior shortens; handlers and outbound clients read it | `RequestHandlerCallback<TResponse>(CancellationToken)`; each behavior passes the token it wants the rest of the pipeline to use | The continuation carries the token (cancellation); the context carries the absolute deadline (propagation to HTTP, gRPC and database calls, #141) |
| Better when | Changing every behavior's signature is to be avoided | Only cancellation matters, not propagation across process boundaries | Both timeouts and cascading deadlines are in 1.0, as they are |
| Pros | No signature change | Cancellation is explicit and type-checked down to the handler | Each concern in its natural place; mirrors gRPC (token per call, deadline per call chain) |
| Cons | A handler that uses its own `cancellationToken` parameter is not cancelled; two tokens to choose from | No deadline to send to the next service | Largest change: every pipeline behavior in `src/` and tests |
| Effort | Medium | Large | Large |

**Spike's recommendation: 6c.** It is the only option under which #335, #141 and #136 all work as their issues describe. If the maintainer defers #141's propagation, 6b alone is enough for timeouts and hedging.

### R7. Provider-level resilience (transports, cloud, gRPC and GraphQL, shards)

Issues #742, #744, #745 and #643 each propose their own retry and breaker. Many broker and cloud SDKs already retry internally, which would be another retry layer.

| | 7a: decorators from the resilience package | 7b: each provider package builds its pipeline | 7c: native SDK resilience plus Encina breaker and error mapping |
| --- | --- | --- | --- |
| What it is | Decorators over `IMessageTransport`, the cloud function adapters and the shard executor, keyed in the registry by provider name or shard id; one option model for all; SDK retries turned off where Encina retries | Each package references the resilience package and calls a pipeline at the points it knows (connection, publish, consume) | Leave retry to the broker or cloud SDK (RabbitMQ automatic recovery, Azure SDK retry); Encina adds a breaker, coded errors and telemetry |
| Better when | Coherence across the 10 transports is the priority (AGENTS.md section 5) | Each provider needs resilience at points a decorator cannot reach, such as connection recovery | The SDKs' retry is trusted and duplicating it is the larger risk |
| Pros | Coherent by construction; one test suite proves the decorator once; per-shard breakers are just keys (#643) | Fine-grained | Least code; no double retry with the SDK |
| Cons | Connection-level recovery stays inside each SDK anyway | Ten transport implementations to keep coherent by review | Behaviour differs per SDK, against the category rule; retry classification (ADR-029) is not applied |
| Effort | Large | Very large | Medium |

**Spike's recommendation: 7a** for operations, with each provider's connection recovery left to its SDK and documented. 7c conflicts with the coherence rule only partly (the breaker and errors are coherent), so it stays as an option.

### R8. Distributed limiter state over the caches (#704)

`ICacheProvider` offers get, set, remove, exists, get-or-set and refresh (`src/Encina.Caching/Abstractions/ICacheProvider.cs:53-140`) but no atomic increment or compare-and-set. A limiter that reads a count and writes it back loses updates between instances. The memory provider is per process, and Memcached ([#277](https://github.com/dlrivada/Encina/issues/277)) is planned.

| | 8a: add an atomic primitive to `ICacheProvider` | 8b: a dedicated limiter-state store per cache | 8c: lock around get and set |
| --- | --- | --- | --- |
| What it is | `IncrementAsync(key, delta, expiration)` (or a token-bucket primitive) on all 8 caches; the core's partitions use it | An `IRateLimiterStateStore` implemented in each cache package (Lua scripts on the Redis family, CAS on Memcached, local on Memory and Hybrid L1) | Distributed lock (`IDistributedLockProvider`) around a get-then-set on the cache |
| Better when | Other features also need atomic counters (quotas #275, idempotency counters) | The limiter's algorithm must be exact (sliding window, token bucket) on each cache | Throughput is low and simplicity matters |
| Pros | One small primitive, reusable | Each cache uses its best atomic operation; the `ICacheProvider` contract stays small | No new primitive |
| Cons | Sliding windows need more than an increment; Memory and Hybrid "distributed" state is still local | Eight implementations of a second abstraction | Two network round trips plus a lock per request; lock providers are a different category (5 providers) |
| Effort | Medium (8 providers) | Large (8 providers) | Medium |

**Spike's recommendation: 8b**, following the RedisRateLimiting precedent (the BCL abstraction over per-store atomic scripts). 8a is the smaller alternative if a fixed-window counter is accepted as the distributed algorithm. Under every option the local limiter remains the fallback on cache failure, as #704 asks, and that fallback is logged.

## 8. Proposed sequencing

Ordering is not a decision for the maintainer; this is the spike's proposal once R1-R8 are decided.

1. ADR with R1-R8 and the settled points; [#2692](https://github.com/dlrivada/Encina/issues/2692) updates AGENTS.md (resilience category row, GCP).
2. Foundation: the package layout (R1), the cancellation and deadline change (R6) and the pipeline registry with both front-ends (R2, R3). #2411, #2632, #2631 and the bulkhead defects #2628-#2634 are closed or absorbed here.
3. The #2414 core with tenant partitions and the adaptive decorator; #740; telemetry (R5, #728, #143 without switches); the ADR-050 conflict check (R4).
4. `Encina.Http` (#1233) on the core.
5. New strategies: timeout (#335), hedging (#136), fallback (#137), chaos (#145), cascading timeouts (#141).
6. Overload: load shedding (#138), adaptive concurrency (#139, merged with the section of #131), backpressure (#131 and #144 reconciled into one `BackpressureStrategy`), scheduled-message limits (#154).
7. Provider resilience (#742, #744, #745) and per-shard resilience (#643) on R7; distributed limiter state (#704) on R8.
8. Documentation [#904](https://github.com/dlrivada/Encina/issues/904) and the per-flag test obligations of each package.

Steps 5, 6 and 7 can run in parallel once step 3 is merged.

## 9. Unknowns

- Whether Polly's `TelemetryOptions` enrichers can read Encina's `IRequestContext` without a per-call allocation (affects R5 5b cost).
- The exact Brighter attribute model and MassTransit's breaker and concurrency-limit syntax (pages not opened).
- Which broker and cloud SDKs used by the 10 transports retry internally by default, and whether each can be switched off (affects R7).
- Whether `Polly.RateLimiting` arrives transitively today: `StandardResilienceOptions.cs:2` uses its namespace, but it is not pinned in `Directory.Packages.props`.

## Related issues

[#2691](https://github.com/dlrivada/Encina/issues/2691), [#2414](https://github.com/dlrivada/Encina/issues/2414), [#1233](https://github.com/dlrivada/Encina/issues/1233), [#2411](https://github.com/dlrivada/Encina/issues/2411), [#2278](https://github.com/dlrivada/Encina/issues/2278), [#728](https://github.com/dlrivada/Encina/issues/728), [#2277](https://github.com/dlrivada/Encina/issues/2277), [#2628](https://github.com/dlrivada/Encina/issues/2628), [#2629](https://github.com/dlrivada/Encina/issues/2629), [#2630](https://github.com/dlrivada/Encina/issues/2630), [#2631](https://github.com/dlrivada/Encina/issues/2631), [#2632](https://github.com/dlrivada/Encina/issues/2632), [#2633](https://github.com/dlrivada/Encina/issues/2633), [#2634](https://github.com/dlrivada/Encina/issues/2634), [#740](https://github.com/dlrivada/Encina/issues/740), [#704](https://github.com/dlrivada/Encina/issues/704), [#154](https://github.com/dlrivada/Encina/issues/154), [#143](https://github.com/dlrivada/Encina/issues/143), [#335](https://github.com/dlrivada/Encina/issues/335), [#136](https://github.com/dlrivada/Encina/issues/136), [#145](https://github.com/dlrivada/Encina/issues/145), [#139](https://github.com/dlrivada/Encina/issues/139), [#138](https://github.com/dlrivada/Encina/issues/138), [#141](https://github.com/dlrivada/Encina/issues/141), [#131](https://github.com/dlrivada/Encina/issues/131), [#144](https://github.com/dlrivada/Encina/issues/144), [#742](https://github.com/dlrivada/Encina/issues/742), [#744](https://github.com/dlrivada/Encina/issues/744), [#745](https://github.com/dlrivada/Encina/issues/745), [#643](https://github.com/dlrivada/Encina/issues/643), [#137](https://github.com/dlrivada/Encina/issues/137), [#266](https://github.com/dlrivada/Encina/issues/266), [#268](https://github.com/dlrivada/Encina/issues/268), [#879](https://github.com/dlrivada/Encina/issues/879), [#904](https://github.com/dlrivada/Encina/issues/904), [#2692](https://github.com/dlrivada/Encina/issues/2692), [#277](https://github.com/dlrivada/Encina/issues/277).
