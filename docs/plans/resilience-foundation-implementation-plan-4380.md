# Implementation Plan: `Encina.Resilience` foundation: one package, per-type compiled Polly pipeline, attribute and options front-ends

> **Issue**: [#4380](https://github.com/dlrivada/Encina/issues/4380)
> **Type**: Feature (ADR-055 R1 to R4)
> **Complexity**: Very high (8 phases, no database providers, 23 pull requests)
> **Estimated Scope**: ~2,300 lines of production code, ~3,200 lines of tests, and the deletion of two packages (28 source files, 28 test files, 2 benchmark projects)

---

## Summary

Create the package `Encina.Resilience` that [ADR-055](../architecture/adr/055-resilience-family.md) R1 names and that no issue builds yet: at startup the attributes and options of each request type compile once into a Polly pipeline in the fixed order of R2 (rate limiter, total timeout, retry or hedging, circuit breaker, per-attempt timeout, chaos last), two behaviors at `PipelineStage.Admission` and `PipelineStage.Resilience` consume it, and two front-ends (attributes and `AddEncinaResilience(o => o.Default(...).For<T>(...))`) feed one model with the precedence options for a type > attributes > defaults (R3). The package then absorbs the retry, circuit breaker, rate limit and bulkhead of `Encina.Polly`, removes `Encina.Polly` and `Encina.Extensions.Resilience` completely (no alias, ADR-055 context: nothing of either shipped), and re-homes their EventIds. Every capability plan of the family (#136, #137, #138, #139, #141, #145, #335, #742, #744) is written against the contract this plan defines.

What exists today (read on `origin/main` at `19b761ffa`, after `git fetch`; the file lists were checked at `37b2edd32` and no listed file changed since):

- `src/Encina.Resilience` does not exist (`src/` has only `Encina.Polly` and `Encina.Extensions.Resilience`). `Encina.Polly` holds 21 files: the attributes `[Retry]`, `[CircuitBreaker]`, `[RateLimit]`, `[Bulkhead]` (`src/Encina.Polly/Attributes/`), five behaviors (`Behaviors/`), `BulkheadManager` and `AdaptiveRateLimiter`. `AddEncinaPolly` registers four open-generic behaviors with no `[PipelineStage]` (`src/Encina.Polly/ServiceCollectionExtensions.cs:46-72`), and each behavior builds its Polly pipeline per call (`RetryPipelineBehavior.cs`, `BuildRetryPipeline`). `Encina.Extensions.Resilience` holds `StandardResiliencePipelineBehavior`, `StandardResilienceOptions` and `AddEncinaStandardResilience` (`ServiceCollectionExtensions.cs:35,66`).
- `PipelineStage.Admission = 200` and `PipelineStage.Resilience = 1100` exist (`src/Encina/Pipeline/Ordering/PipelineStage.cs:47,95`) and `PipelineBuilder` orders steps by stage (`src/Encina/Pipeline/PipelineBuilder.cs:30`).
- Every rejection of the old behaviors is an uncoded `EncinaError.New(message)` (`BulkheadPipelineBehavior.cs:120,137,153`, `CircuitBreakerPipelineBehavior.cs:67`, `RateLimitingPipelineBehavior.cs:79`); the code `encina.ratelimit.exceeded` exists (`src/Encina/Errors/EncinaErrorCodes.cs:93`), `encina.timeout` exists (`:63`), and no code exists for an open circuit or a full bulkhead (open bug #3424).
- The continuation has no token: `RequestHandlerCallback<TResponse>()` (`src/Encina/Abstractions/RequestHandlerDelegate.cs:9`); the sibling plan #4382 changes it.
- The EventId ranges `Polly` 3900-3949 and `ExtensionsResilience` 3950-3999 are registered (`src/Encina/Diagnostics/EventIdRanges.cs:358,361`), used up to 3921 and 3950-3955; 3922-3924 are reserved by the #740 plan; the assembly map has the rows `Encina.Polly` and `Encina.Extensions.Resilience` (`tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs:82,100`).
- Direct Polly users: `Encina.Security.Secrets` (`ResilientSecretReaderDecorator.cs:38`, a `ResiliencePipeline`) and `Encina.Compliance.NIS2` (`Encina.Compliance.NIS2.csproj:24`, a `PackageReference` with no `using Polly` in its code).
- `Encina.Messaging` runs its own immediate-retry loop (`RecoverabilityPipelineBehavior.cs:117,325`, `RecoverabilityOptions.ImmediateRetries` at `:45`, `MaxJitterPercent` at `:135`).

What this plan delivers, all in `Encina.Resilience` unless said: the project, the contracts the sibling plans assume (strategy-factory per slot, rejection mapper, per-call contributor, `ResilienceContext` property keys, startup validation), the options model and `ResilienceTypeBuilder<TRequest>`, the compiler and registry, the conflict collector, the two behaviors and `AddEncinaResilience`, the retry, circuit breaker, bulkhead and rate limit strategies moved over, the removal of both old packages with their tests, benchmarks, manifests and references, the re-homed EventIds and the rejection codes. Documentation follows.

- **Affected packages**: `Encina.Resilience` (new), `Encina.Polly` and `Encina.Extensions.Resilience` (removed), `Encina.Security.Secrets` and `Encina.Compliance.NIS2` (their `Polly` package references, PR-20), `Encina.Cli` and core `PipelineOrderValidator` (names of the removed packages, PR-15), `Encina` core (`EventIdRanges.cs` summary text only), `Encina.Messaging` (Phase 8 only, Dilemma 7).
- **Provider category**: Resilience (AGENTS.md section 5, "Other categories"); no database, caching, transport, lock or validation matrix applies. The decorators over transports, cloud functions and shards are #742, #744 and #643 (R7).
- **Estimated size**: 23 pull requests of at most 15 files and 1,500 lines (table in "Implementation Phases").
- **Prerequisites**: #4382 (token and deadline) for PR-4 and later; the #2414 partitioned core has no implementation issue and is a prerequisite of the final rate-limit design (PR-11 and PR-13 use stand-ins); see "Prerequisites & Dependencies".

### Shared Pieces and Ownership (family resilience, ADR-055)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `Encina.Resilience` project, compiler, registry, options model, `ResilienceTypeBuilder<TRequest>`, the five hooks (strategy factory, rejection mapper, call contributor, property keys, startup validation), `AddEncinaResilience`, the two behaviors | **This plan (#4380)** | #136, #137, #138, #139, #141, #145, #335, #742, #744, #728 |
| `RequestHandlerCallback<TResponse>(CancellationToken)` and `IRequestContext.Deadline` (R6) | #4382 (sibling plan, by number) | PR-4 and later of this plan; #136, #141, #335 |
| Retry, circuit breaker, bulkhead and rate limit strategies, their attributes and rejection codes | **This plan** | #136 (retry-or-hedging check), #742, #744, #745, #643 (pipelines keyed by provider name) |
| `[Timeout]`, hedging, fallback, load shedding, adaptive concurrency, chaos | #335, #136, #137, #138, #139, #145 (sibling plans) | plug into this plan's slots |
| The `Meter` and `ActivitySource` holder, the telemetry enricher | #728 (plan on main) | this plan creates no instrument |
| The partitioned rate-limiting core (#2414 D1), tenant partitions | #2414 and #740 (a plan on main); the core has no implementation issue | this plan moves today's limiter as a stand-in (Dilemma 1) |
| EventIds 3925-3930 (inside the registered `Polly` range) | **This plan** | none |
| Immediate-retry half of R4 in `Encina.Messaging` | **This plan, Phase 8** (Dilemma 7) | none |

Order of the whole family: #4382 PR-1 and PR-2 and this plan's PR-1 to PR-3 may run in parallel; #4382 PR-2 (the token sweep) lands before this plan's PR-4 (the behaviors), and PR-4 before every capability plan of the siblings.

---

## Design Choices

<details>
<summary><strong>1. Rate limit and bulkhead before the #2414 core exists</strong></summary>

ADR-055 R2 turns the bulkhead into "a concurrency partition of the #2414 rate-limiting core" and deletes `BulkheadManager`; the core is described by #2414 D1 (decided 2026-10-23) but has no implementation issue (the #740 plan says the same). The issue asks to move the rate limit and bulkhead behaviors and remove `Encina.Polly` now.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Bulkhead as a `ConcurrencyLimiter` of `System.Threading.RateLimiting` inside Polly's rate-limiter strategy (the `Polly` package already referenced); `BulkheadManager` deleted; the existing `AdaptiveRateLimiter` moves unchanged behind the rate-limiter slot until the core replaces it** | Removes `Encina.Polly` completely now; the queue bound, `TimeProvider` and dispose defects of #2628, #2629, #2633 and #2634 disappear by construction; the later core swaps one strategy factory | Two stand-ins (a BCL limiter and the old adaptive limiter) that the core replaces; the adaptive limiter keeps its known accounting defects until then |
| **B) Move both classes unchanged (`BulkheadManager`, `AdaptiveRateLimiter`) behind strategies** | Smallest rewrite | Carries the defects of #2628 to #2634 into the new package and contradicts R2 (the manager is to be deleted) |
| **C) Do not move them; `Encina.Polly` stays with those two until the core exists** | No stand-in code | `Encina.Polly` is not removed (acceptance criterion of #4380) and two packages stay for one concept |

### Chosen Option: **A — BCL concurrency limiter for the bulkhead, adaptive limiter moved as a stand-in** (recommended, pending the maintainer)

### Rationale

- A wins when the package must be gone before the core lands and the bulkhead defects must not survive; C wins if the maintainer prefers no stand-in code and accepts a temporary `Encina.Polly`; B wins only for the smallest diff.
- The core itself (partitioned limiter, tenant key, `Retry-After` pause, instruments) is a feature of its own that has no implementation issue today (#2414 is a closed-decision SPIKE that is still open, #740 plans only the tenant part); the maintainer opens its issue with its plan, and it is a prerequisite recorded in "Prerequisites & Dependencies", not a part of this plan. Until it lands the stand-ins of PR-11 and PR-13 are the limiter.
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. How the compiler finds the request types to compile at startup</strong></summary>

R2 says the pipelines are compiled at startup and "attributes are read once". The compiler needs the set of request types; core has no handler registry (`IModuleHandlerRegistry` exists only for modules, `src/Encina/Modules/IModuleHandlerRegistry.cs:16`).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Eager: a catalog built from the `IRequestHandler<,>` service descriptors captured by `AddEncinaResilience`, plus every `For<T>()` type; compiled by a `ValidateOnStart` step; a type with neither an attribute, an options entry nor a default gets no pipeline and the behaviors pass through** | Conflicts and invalid options fail at startup (R4); no first-call cost; no lock on the hot path | The catalog reads descriptors, so handlers registered after the provider is built are not seen; startup cost grows with the number of request types |
| **B) Lazy: compile a type's pipeline on its first call and cache it; `For<T>()` types are validated at startup** | No descriptor scan; fast cold start with hundreds of request types | A conflict on a type nobody declared surfaces at its first call, not at startup; a compile failure at run time must fail closed with a coded `Left` (`encina.resilience.pipeline_unavailable`, ADR-062 category `Internal`, constant message) because running the request without its policy is not allowed |

### Chosen Option: **A — Eager from the handler descriptors** (recommended, pending the maintainer)

### Rationale

- A wins for servers where every conflict must stop the deployment (R4); B wins for serverless hosts where cold start counts and the types with a declared policy are few.
- The choice does not change the precedence, the order or the hooks.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Shape of the hooks the capabilities plug into</strong></summary>

The sibling plans assume five hooks: a factory per slot, a rejection mapper, a per-call contributor, `ResilienceContext` property keys and startup validation. They are consumed by nine plans, so their shape must be fixed once.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Interfaces `IResilienceStrategyFactory` (with a `ResilienceSlot`), `IResilienceRejectionMapper`, `IResilienceCallContributor`, registered in DI with `TryAddEnumerable` and collected by the compiler and the behaviors** | Each capability package or folder registers its own pieces; unit-testable with fakes; `ValidateOnBuild` proves them | More public types (5 interfaces/enum) than delegates |
| **B) Delegates on the options builder (`o.AddSlot(slot, (type, builder) => ...)`)** | Fewer types; configured in one place | A capability cannot register itself from `AddEncinaResilience`; scoped per-call services cannot be resolved by a delegate stored in a singleton |

### Chosen Option: **A — DI-registered interfaces** (recommended, pending the maintainer)

### Rationale

- A wins when capabilities ship as separate folders or packages that self-register (hedging, chaos, #742); B wins for an application that only tunes a few types.
- The contributor receives the request's `IServiceProvider` and the `ResilienceContext` before the pipeline runs, so a per-call (possibly scoped) service is placed in the context and never captured by the singleton pipeline.
- A contributor that throws or fails to supply its service fails closed: the request does not run, the behavior logs the failure (EventId 3929, exception type and stack through `ForLogging()`, never the message) and returns a coded `Left` (`encina.resilience.pipeline_unavailable`, `ErrorCategory.Internal` by ADR-062 R4, constant message). A request is never run without the per-call service its policy needs, under either option.
- `ResilienceTypeContext` (created in PR-1) is what `AppliesTo` and `Add` receive: the request `Type`, the resolved per-type options entry, an attribute lookup (`GetAttribute<TAttribute>()`, read once at compile) and the `TimeProvider`.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. An exception thrown by a handler inside the pipeline</strong></summary>

Today `RetryPipelineBehavior` catches every exception and returns `EncinaError.New(ex)` (`RetryPipelineBehavior.cs`, the `catch (Exception ex)` block) and `[Retry]` has `RetryOnAllExceptions` (`RetryAttribute.cs:65`). `PipelineBuilder` lets handler exceptions propagate (fail-fast, `PipelineBuilder.cs`, "Pure ROP" comments). ADR-055 settles that strategies handle `Either` results directly.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Strategies handle only `Left` outcomes; an exception from the handler propagates unchanged; `RetryOnAllExceptions` is removed; the only exceptions converted are Polly's own rejections (circuit open, rate limiter rejected, timeout) through the rejection mapper** | ROP (AGENTS.md section 3), the same fail-fast the builder applies; no handler bug is retried or hidden | A handler that lets a transient SDK exception escape is not retried |
| **B) Keep catch-and-convert: an exception becomes `Left(EncinaError.New(ex))` and is classified (retried when `Unknown`)** | Covers handlers that throw transient exceptions | Exceptions used as a result channel; a bug is retried; two behaviors for the same exception (builder and strategy) |

### Chosen Option: **A — Propagate** (recommended, pending the maintainer)

### Rationale

- A wins for code that follows ROP; B wins for applications whose handlers call SDKs that throw `HttpRequestException` and have no time to convert them. The caller's cancellation is never a `Left` of this package: `OperationCanceledException` propagates and `PipelineBuilder` converts it (`PipelineBuilder.cs`, `ExecuteBehaviorAsync`).
- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Error category of a full bulkhead</strong></summary>

ADR-062 gives circuit-open the category `Unavailable` (the #3424 rows: `RateLimited`, `Unavailable`, `Timeout`) and rate limit `RateLimited`; a bulkhead that is full is not named.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Unavailable` (503), code `encina.bulkhead.full`** | The server is out of capacity, not the caller over a quota; the retry classifier treats it as transient | A per-tenant partition would read as a quota |
| **B) `RateLimited` (429)** | A client can back off | Misleading for a global concurrency cap |

### Chosen Option: **A — `Unavailable`** (recommended, pending the maintainer)

### Rationale

- A wins for the global cap this plan builds; B wins once #740 adds per-tenant concurrency partitions that are a quota. Constant messages, no exception text; `EncinaErrorMetadataKeys.RetryAfter` only when known (ADR-062 header rules).
- **Class:** Dilemma

</details>

<details>
<summary><strong>6. The database circuit breaker (#2705)</strong></summary>

`DatabaseCircuitBreakerPipelineBehavior` is a second breaker behavior that reads `IDatabaseHealthMonitor` (`Encina.Polly/Behaviors/DatabaseCircuitBreakerPipelineBehavior.cs`); `DatabaseResilienceOptions.EnableCircuitBreaker` is never read (#2705). ADR-055 leaves "removed or wired" to the move into the R3 model.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A strategy factory for the breaker slot, switched on by `DatabaseResilienceOptions.EnableCircuitBreaker` (now read) and `DatabaseTransientErrorPredicate`, for request types that touch a database** | One breaker model; the dead flag is wired; no second behavior | The factory needs a rule for "touches a database" (an options marker per type) |
| **B) Remove the database breaker and the flag** | Smallest | Loses transient-database detection that the generic breaker does not have |
| **C) Keep the behavior as it is** | No change | A second stacked breaker, against R2 and ADR-050 |

### Chosen Option: **A — Strategy of the breaker slot** (recommended, pending the maintainer)

### Rationale

- A wins when database health must open the breaker; B wins if the maintainer judges the generic breaker enough.
- **Class:** Dilemma

</details>

<details>
<summary><strong>7. The immediate-retry half of R4 in `Encina.Messaging`</strong></summary>

The issue title says R1 to R4 but its body lists only the conflict collection. R4 also makes `Encina.Resilience` the single owner of immediate retries: Recoverability keeps delayed retries and the dead letter queue, requires `Encina.Resilience` when `ImmediateRetries > 0` (ADR-050 row) and loses `MaxJitterPercent` (D5). No open issue owns it (searched on 2026-11-25).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Include it as Phase 8 of this plan (PR-21 and PR-22). `RecoverabilityOptions.ImmediateRetries` stays as the switch and the count; `ImmediateRetryDelay`, `UseExponentialBackoffForImmediateRetries` and `MaxJitterPercent` are removed (delays come from Polly's delay types, D5); Messaging registers an options configurator that gives the request types its behavior wraps a default `RetryOptions` of `MaxAttempts = ImmediateRetries + 1` unless the type has its own entry** | One owner; the conflict check has its second layer; no orphan; one place to tune delays | The mapping is a new Messaging-to-Resilience dependency; 3 options disappear |
| **B) Leave it to a separate plan under a new issue** | This plan stays at 21 pull requests | A new FEATURE issue without an owner today; the immediate retry loop stays beside the new engine until then |

### Chosen Option: **A — Include as Phase 8** (recommended, pending the maintainer)

### Rationale

- A wins when R4 must be closed with the engine (ADR-055 title of R4); B wins when the maintainer wants Messaging changes planned by the messaging family.
- **Class:** Dilemma

</details>

<details>
<summary><strong>8. Two behaviors, one compile, fixed order</strong></summary>

ADR-055 R2 compiles one model per request type and consumes it with two behaviors: `PipelineStage.Admission` (rate limiter, bulkhead) and `PipelineStage.Resilience` (retry, timeout, breaker). The compiler therefore stores, per type, an admission pipeline and a resilience pipeline built from the same ordered slot list, which keeps the relative order (rate limiter before total timeout) because the stages order the behaviors.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two behaviors, each running its compiled pipeline** | Fixed order; one compile; the stages the pipeline already reserves | Two Polly pipelines per type |
| **B) One behavior per strategy** | Simple | Rejected by ADR-055 (2b): order depends on stage numbers |
| **C) An Encina-owned engine** | Full control | Rejected by ADR-055 (2c): re-implements Polly |

### Chosen Option: **A — Two behaviors**

### Rationale

- Only A satisfies the ADR text; the order test of PR-4 enforces it.

- **Class:** Settled
- **Settled by:** ADR-055 R2 (option 2a chosen, 2b and 2c rejected); `src/Encina/Pipeline/Ordering/PipelineStage.cs:47,95`

</details>

<details>
<summary><strong>9. Lifecycle, reload and precedence</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Compile once; rebuild only when the bound options change; a failed rebuild keeps the previous pipeline and stops further reloads; precedence options for a type > attributes > defaults** | Exactly the ADR text; a test covers precedence | Needs a configuration change token |
| **B) Rebuild per call** | Always current | Rejected by R2; allocation per call |

### Chosen Option: **A — Compile once, reload on change**

### Rationale

- The behavior is Polly's documented dynamic reload; the implementation logs `ReloadFailed` (EventId 3927) and tests it.

- **Class:** Settled
- **Settled by:** ADR-055 R3 ("Precedence" and "Pipeline lifecycle" bullets)

</details>

<details>
<summary><strong>10. No activation switches; `EncinaPollyOptions` goes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Remove `EncinaPollyOptions.EnableTelemetry` and `EnableLogging` (`ServiceCollectionExtensions.cs` of Polly); no switches in the new options** | Matches the maintainer decision | None |
| **B) Carry them over** | Familiar | Contradicts the decision |

### Chosen Option: **A — No switches**

### Rationale

- The maintainer removed activation switches for telemetry and logging; only A complies.

- **Class:** Settled
- **Settled by:** ADR-055 settled points ("No enable or disable switches for telemetry or logging"); #2277 and #728 (2026-10-16)

</details>

<details>
<summary><strong>11. Startup validation collects every problem</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Validators and the conflict collector aggregate all failures into one startup exception listing request types and codes, never messages** | One run shows every problem; follows `ValidateOnStart` | Aggregation code |
| **B) Fail on the first problem** | Simpler | Many restarts |

### Chosen Option: **A — Aggregate**

### Rationale

- It is the existing startup-validation convention; the failure lists request types and codes only.

- **Class:** Trivial
- **Settled by:** the convention of `ValidateOnStart` with `IValidateOptions` (`src/Encina/Identity/RequestIdentityServiceCollectionExtensions.cs:58-65`)

</details>

<details>
<summary><strong>12. Package references</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Resilience` references core `Encina`, `Polly` 8.6.6 and `Polly.Extensions` 8.7.0 (central versions), `Microsoft.Extensions.Options` and configuration binding; no `Microsoft.Extensions.Resilience`** | Same as the packages it replaces minus the HTTP standard handler (`Encina.Http`, #1233) | None |
| **B) Keep `Microsoft.Extensions.Http.Resilience` for the standard pipeline** | Familiar | A dependency the package no longer uses |

### Chosen Option: **A — Core plus Polly**

### Rationale

- Both packages it replaces reference exactly these (`Encina.Polly.csproj`), minus the HTTP standard handler.

- **Class:** Trivial
- **Settled by:** `Directory.Packages.props` (Polly 8.6.6, Polly.Extensions 8.7.0); ADR-055 R1 (the HTTP standard handler belongs to `Encina.Http`)

</details>

---

## Implementation Phases

Twenty-three pull requests, in this order; each builds alone with its own tests and registers every service it introduces (a DI test with `ValidateOnBuild` and `ValidateScopes` from PR-2 on). A capability is first added to `Encina.Resilience` (the new code is a copy, the old code still works in `Encina.Polly`), then the old copy is deleted in a later pull request, so no pull request leaves the build broken. Counts are by file name (production, tests, `PublicAPI`, coverage manifest, project and solution edits, `ProjectReference` and `<Using>` edits, READMEs), recounted against the files that exist on `origin/main` at `37b2edd32`.

| PR | Phase | Content | Files |
|----|-------|---------|-------|
| 1 | 1 | Project, contracts, `ResilienceTypeContext`, property keys, error codes | 14 |
| 2 | 1 | Options model, builder, validator, `AddEncinaResilience` skeleton with `ValidateOnStart`, DI test | 12 |
| 3 | 2 | Compiler, registry, conflict collector, catalog, log messages, their registration | 14 |
| 4 | 2 | The two behaviors, contributor handling, DI and end-to-end tests (needs #4382 PR-2) | 10 |
| 5 | 3 | Retry attribute, options, strategy factory, `IErrorClassifier` registration, integration test | 11 |
| 6 | 3 | Remove the `Encina.Polly.Benchmarks` project | 9 |
| 7 | 3 | Remove the retry behavior and the combined and registration tests from `Encina.Polly` | 15 |
| 8 | 4 | Circuit breaker attribute, options, factory, rejection mapper | 11 |
| 9 | 4 | Database breaker strategy (Dilemma 6) | 11 |
| 10 | 4 | Remove both breaker behaviors and the database predicate from `Encina.Polly` | 13 |
| 11 | 5 | Bulkhead strategy over `ConcurrencyLimiter` (Dilemma 1) | 12 |
| 12 | 5 | Remove bulkhead behavior and `BulkheadManager` | 12 |
| 13 | 5 | Rate limit attribute, options, factory, `AdaptiveRateLimiter` copied | 14 |
| 14 | 6 | Remove the rate limit behavior, the old limiter classes and their tests; drop the `Encina.Polly` assembly row | 15 |
| 15 | 6 | CLI, `PipelineOrderValidator` and their test name `Encina.Resilience` | 3 |
| 16 | 6 | Remove the `Encina.Polly` source package | 13 |
| 17 | 6 | Remove the tests and benchmarks of `Encina.Extensions.Resilience` | 10 |
| 18 | 6 | Remove the behavior and options of `Encina.Extensions.Resilience`; drop its assembly row | 6 |
| 19 | 6 | Remove the `Encina.Extensions.Resilience` source package | 13 |
| 20 | 7 | Secrets and NIS2 drop their direct `Polly` reference (ADR-055 R1) | 2 |
| 21 | 8 | Messaging: registration requirement and retry defaults (Dilemma 7) | 8 |
| 22 | 8 | Messaging: remove the immediate-retry loop and its options (Dilemma 7) | 12 |
| 23 | 8 | ADR-050 row, README, guide, contract tests, fragments | 14 |

Edits to `.github/workflows/benchmarks.yml` (lines 173 and 174 name the two benchmark projects) are the orchestrator's: PR-6 and PR-17 list the lines and the orchestrator applies them. `AssemblyRanges_HasNoStaleEntries` (`EncinaEventIdAllocationTests.cs:223-236`) fails when a mapped assembly declares no EventId, so the row of a package is removed in the pull request that deletes its last logger: `Encina.Polly` in PR-14 (its last logger, `RateLimitingPipelineBehavior`, ids 3915-3917) and `Encina.Extensions.Resilience` in PR-18 (`StandardResiliencePipelineBehavior`, 3950-3955).

### Phase 1: Package, Contracts and Options Model

<details>
<summary>Tasks</summary>

PR-1 (14 files; it introduces contracts and constants only, so there is no service to register): `src/Encina.Resilience/Encina.Resilience.csproj` (references per choice 12; `InternalsVisibleTo` for `Encina.UnitTests` and `Encina.GuardTests`; `PublicApiAnalyzers`), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, `Encina.slnx` (project row), `.github/coverage-manifest/Encina.Resilience.json`, `ResilienceSlot.cs` (enum `RateLimiter`, `TotalTimeout`, `Retry`, `CircuitBreaker`, `AttemptTimeout`, `Chaos`, with `Bulkhead` ordered after `RateLimiter` in the admission group), `IResilienceStrategyFactory.cs` (`ResilienceSlot Slot { get; }`, `bool AppliesTo(ResilienceTypeContext context)`, `void Add(ResiliencePipelineBuilder<Either<EncinaError, T>> builder, ResilienceTypeContext context)` as a generic method), `IResilienceRejectionMapper.cs` (`bool TryMap<T>(Exception exception, out Either<EncinaError, T> result)`), `IResilienceCallContributor.cs` (`ValueTask ContributeAsync(IServiceProvider services, ResilienceContext context, IRequestContext request, CancellationToken cancellationToken)`), `ResilienceErrorCodes.cs` (`encina.circuitbreaker.open`, `encina.bulkhead.full`, `encina.resilience.pipeline_unavailable`; used by PR-4 and PR-8), `ResilienceTypeContext.cs` (the request `Type`, the resolved per-type options entry, `GetAttribute<TAttribute>()`, the `TimeProvider`), `ResiliencePropertyKeys.cs` (`ResiliencePropertyKey<T>` statics for the request context and the request), `tests/Encina.UnitTests/Resilience/ResilienceContractsTests.cs`, `tests/Encina.UnitTests/Encina.UnitTests.csproj` (`ProjectReference`).

PR-2 (12 files; it registers the options, the validator with `ValidateOnStart` and nothing else, in `ResilienceServiceCollectionExtensions.cs` with `AddEncinaResilience()` and `AddEncinaResilience(Action<ResilienceOptions>)`, and its DI test `ResilienceRegistrationTests.cs`): `ResilienceOptions.cs` (`Default(Action<ResilienceTypeOptions>)`, `For<TRequest>(Action<ResilienceTypeBuilder<TRequest>>)`, configuration-bound dictionary), `ResilienceTypeOptions.cs` (a bag per type that each capability extends through a registered section), `ResilienceTypeBuilder.cs` (`ResilienceTypeBuilder<TRequest>`, the extension target of #136 and #335), `ResilienceOptionsValidator.cs` (internal `IValidateOptions`), `PublicAPI.Unshipped.txt`, manifest, `ResilienceOptionsTests.cs`, `ResilienceTypeBuilderTests.cs`, `ResilienceOptionsGuardTests.cs`, `tests/Encina.GuardTests/Encina.GuardTests.csproj`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina issue #4380, plan docs/plans/resilience-foundation-implementation-plan-4380.md. ADR-055 R1 to R3.
TASK: PR-1 then PR-2: create src/Encina.Resilience with the contracts and the options model named in the plan.
KEY RULES: no exceptions for business flow; time from TimeProvider; no [Obsolete]; XML docs with examples on every public member; ErrorCategory named on every EncinaErrors.Create call with a constant message; PublicAPI lines by mechanical-fixer; guard tests for every public parameter; nothing scoped captured by a singleton.
REFERENCE FILES: docs/architecture/adr/055-resilience-family.md, src/Encina.Polly/Encina.Polly.csproj, src/Encina.Polly/ServiceCollectionExtensions.cs, src/Encina/Pipeline/Ordering/PipelineStage.cs
```

</details>

### Phase 2: Compiler, Registry, Behaviors and Registration

<details>
<summary>Tasks</summary>

PR-3 (14 files; it registers the compiler, registry, catalog and collector by editing `ResilienceServiceCollectionExtensions.cs` and `ResilienceRegistrationTests.cs`, counted): `ResiliencePipelineCompiler.cs` (reads attributes once through each factory, resolves precedence options > attributes > defaults, adds strategies in `ResilienceSlot` order, builds the admission and resilience pipelines), `ResiliencePipelineRegistry.cs` (wraps Polly's `ResiliencePipelineRegistry<Type>`; `TryGet<T>(out ...)`; reload on bound-option change), `ResilienceConflictCollector.cs` (internal; collects conflicts such as two strategies in the retry slot; throws one aggregated exception at startup, type names and codes only), `ResilienceRequestTypeCatalog.cs` (choice 2), `Diagnostics/ResilienceLogMessages.cs` (EventIds 3925-3930), `src/Encina/Diagnostics/EventIdRanges.cs` (summary comment of `Polly` names `Encina.Resilience`), `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (adds the `Encina.Resilience` row), `PublicAPI.Unshipped.txt`, manifest, `ResiliencePipelineCompilerTests.cs` (order test with fake factories, precedence test with a test attribute), `ResilienceConflictCollectorTests.cs`, `ResilienceCompilerGuardTests.cs`.

PR-4 (10 files): `AdmissionPipelineBehavior.cs` (`[PipelineStage(PipelineStage.Admission)]`) and `ResiliencePipelineBehavior.cs` (`[PipelineStage(PipelineStage.Resilience)]`; before running, calls every contributor with the request's `IServiceProvider`; a contributor that throws fails closed with the coded `Left` of choice 3; maps Polly rejections through the mappers; propagates cancellation and handler exceptions), `ResilienceServiceCollectionExtensions.cs` (modified: the two behaviors, binding to `IConfiguration`, `TryAddEnumerable` of hooks), `PublicAPI.Unshipped.txt`, manifest, `ResiliencePipelineBehaviorTests.cs`, `ResilienceRegistrationTests.cs` (`ValidateOnBuild`, `ValidateScopes`, a scoped service in a contributor), `ResilienceBehaviorGuardTests.cs`, `tests/Encina.IntegrationTests/Resilience/ResiliencePipelineEndToEndTests.cs` (in-process, `[Trait("Category","Integration")]`), `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: PR-1 and PR-2 merged. PR-4 needs RequestHandlerCallback<TResponse>(CancellationToken) from #4382 PR-2.
TASK: Implement the compiler, registry, conflict collector, catalog and log messages (PR-3), then the two behaviors and AddEncinaResilience (PR-4).
KEY RULES: attributes read once at startup; a pipeline is never built per call; a type with no policy passes through; a type whose compile failed never runs unprotected; caller cancellation propagates (never a Left); constant error messages; logs by [LoggerMessage] in 3925-3930 only; CRAP <= 10 (split compile steps into small methods, no large switch).
REFERENCE FILES: src/Encina/Pipeline/PipelineBuilder.cs, src/Encina.Polly/Behaviors/RetryPipelineBehavior.cs, src/Encina/Identity/RequestIdentityServiceCollectionExtensions.cs
```

</details>

### Phase 3: Retry

<details>
<summary>Tasks</summary>

PR-5 (11 files): `Retry/RetryAttribute.cs` (with `BackoffType`, no `RetryOnAllExceptions`, choice 4), `Retry/RetryOptions.cs`, `Retry/RetryStrategyFactory.cs` (retry slot: retry only a `Left` that `IErrorClassifier` does not classify `Permanent` and only an idempotent command, ADR-029; the delay generator carries today's exact `Retry-After` rule `OutboundRetryRules` and the ceiling of `MaxDelayMs`; Polly delay types with `UseJitter`, D5), `ResilienceServiceCollectionExtensions.cs` (modified: the factory and `TryAddSingleton<IErrorClassifier, DefaultErrorClassifier>()`, the owner of that registration in `Encina.Resilience`, as `Encina.Polly` does at `ServiceCollectionExtensions.cs:58`), `PublicAPI.Unshipped.txt`, manifest, `RetryStrategyFactoryTests.cs` (the cases of `RetryPipelineBehaviorTests` and `RetryPipelineBehaviorOutboundTests`), `RetryPrecedenceTests.cs`, `RetryStrategyFactoryGuardTests.cs`, `ResilienceRegistrationTests.cs` (modified), `tests/Encina.IntegrationTests/Resilience/RetryEndToEndTests.cs` (the scenarios of `Observability/Polly/EndToEndIntegrationTests.cs`, which PR-7 deletes).

PR-6 (9 files): delete the benchmark project `tests/Encina.BenchmarkTests/Encina.Polly.Benchmarks/` (`Encina.Polly.Benchmarks.csproj`, `Program.cs`, `GlobalSuppressions.cs`, `RetryBenchmarks.cs`, `CircuitBreakerBenchmarks.cs`, `BulkheadBenchmarks.cs`, `RateLimitingBenchmarks.cs`), `.github/perf-manifest/Encina.Polly.Benchmarks.json` and its `Encina.slnx` row (line 150). It goes first because every benchmark file references a behavior a later pull request deletes. Line 173 of `benchmarks.yml` is the orchestrator's.

PR-7 (15 files): delete `RetryPipelineBehavior.cs`, `Diagnostics/RetryTelemetry.cs` and `Attributes/RetryAttribute.cs` from `Encina.Polly`; edit its `ServiceCollectionExtensions.cs`, `PublicAPI.Unshipped.txt` and manifest; delete `RetryPipelineBehaviorTests.cs`, `RetryPipelineBehaviorOutboundTests.cs`, `RetryAttributeTests.cs`, `BackoffTypeTests.cs`, `tests/Encina.GuardTests/Infrastructure/Polly/RetryPipelineBehaviorGuardsTests.cs`, `PolicyCombinationTests.cs` (its order cases are the compiler order test of PR-3), the Polly `tests/Encina.UnitTests/Polly/ServiceCollectionExtensionsTests.cs` (its assertions are the registration tests of `Encina.Resilience`) and `tests/Encina.IntegrationTests/Observability/Polly/EndToEndIntegrationTests.cs`; edit `ErrorClassifierRegistrationTests.cs`. `RetryTelemetry` (manifest unit 95, integration 30) goes with its file: its counters become Polly instruments (#728 R5), which is intended.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: PR-1 to PR-4 merged.
TASK: PR-5 adds the retry strategy to Encina.Resilience with the cases moved from the old behavior tests; PR-6 removes the Polly benchmark project; PR-7 removes the old retry from Encina.Polly.
KEY RULES: a Left classified Permanent is never retried; a command is retried only when idempotent; Retry-After above MaxDelayMs is returned unchanged; the metadata key is EncinaErrorMetadataKeys.RetryAfter; time through TimeProvider; the retry slot is the only retry layer (two retry strategies on a type is a startup conflict).
REFERENCE FILES: src/Encina.Polly/Behaviors/RetryPipelineBehavior.cs, src/Encina/Recoverability/IErrorClassifier.cs, docs/architecture/adr/029-recoverability-error-classification.md
```

</details>

### Phase 4: Circuit Breaker

<details>
<summary>Tasks</summary>

PR-8 (11 files): `CircuitBreaker/CircuitBreakerAttribute.cs`, `CircuitBreakerOptions.cs`, `CircuitBreakerStrategyFactory.cs`, `CircuitBreakerRejectionMapper.cs` (maps `BrokenCircuitException` to the coded `Left` `encina.circuitbreaker.open`, `ErrorCategory.Unavailable`, constant message), the registration edit, `PublicAPI`, manifest, `CircuitBreakerStrategyFactoryTests.cs`, `CircuitBreakerRejectionMapperTests.cs`, `CircuitBreakerGuardTests.cs`, `ResilienceRegistrationTests.cs`. State is per container, never a static field (#2411): the breaker lives in the singleton pipeline of the registry.

PR-9 (11 files): `DatabaseBreakerStrategyFactory.cs`, `DatabaseTransientErrorPredicate.cs` (copied; the old one is deleted in PR-10), registration, `PublicAPI` and manifest of `Encina.Resilience`, tests (factory, predicate copy `DatabaseTransientErrorPredicateTests.cs`, its guard test copy, DI test); wires `DatabaseResilienceOptions.EnableCircuitBreaker` (Dilemma 6).

PR-10 (13 files): delete `CircuitBreakerPipelineBehavior.cs`, `DatabaseCircuitBreakerPipelineBehavior.cs`, `CircuitBreakerAttribute.cs`, `Predicates/DatabaseTransientErrorPredicate.cs` and their six test classes (`CircuitBreakerPipelineBehaviorTests.cs`, `CircuitBreakerAttributeTests.cs`, `DatabaseCircuitBreakerPipelineBehaviorTests.cs`, `DatabaseTransientErrorPredicateTests.cs`, `tests/Encina.GuardTests/Database/DatabaseTransientErrorPredicateGuardsTests.cs`, `tests/Encina.GuardTests/Infrastructure/Polly/CircuitBreakerPipelineBehaviorGuardsTests.cs`) from `Encina.Polly`; edit its registration (`ServiceCollectionExtensions.cs`), `PublicAPI.Unshipped.txt` and its manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: PR-1 to PR-7 merged.
TASK: Move the circuit breaker and the database breaker into the breaker slot, then delete the old behaviors.
KEY RULES: open circuit is ErrorCategory.Unavailable with code encina.circuitbreaker.open and a constant message; Retry-After metadata only when the exception exposes it; no exception text in logs, errors or tags; breaker state is never static.
REFERENCE FILES: src/Encina.Polly/Behaviors/CircuitBreakerPipelineBehavior.cs, src/Encina.Polly/Behaviors/DatabaseCircuitBreakerPipelineBehavior.cs, docs/architecture/adr/062-error-category-contract.md
```

</details>

### Phase 5: Bulkhead and Rate Limit

<details>
<summary>Tasks</summary>

PR-11 (12 files): `Bulkhead/BulkheadAttribute.cs` (copied; validation by the options validator at startup, closing #2630), `BulkheadOptions.cs`, `BulkheadStrategyFactory.cs` (Polly rate-limiter strategy over a `ConcurrencyLimiter` with `QueueLimit`; time from `TimeProvider`), `RateLimiterRejectionMapper.cs` (maps `RateLimiterRejectedException` to `encina.bulkhead.full` or `encina.ratelimit.exceeded`), registration, `PublicAPI`, manifest, tests (factory with queue bound under concurrency, mapper, attribute validation, guard), DI test.

PR-12 (12 files): delete `BulkheadManager.cs`, `IBulkheadManager.cs`, `BulkheadPipelineBehavior.cs`, `BulkheadAttribute.cs` and the five bulkhead test classes (`BulkheadAttributeTests.cs`, `BulkheadManagerTests.cs`, `BulkheadPipelineBehaviorTests.cs`, `tests/Encina.GuardTests/Infrastructure/Polly/BulkheadManagerGuardsTests.cs`, `BulkheadPipelineBehaviorGuardsTests.cs`); edit registration, `PublicAPI`, manifest. Closes #2628, #2629, #2630, #2631, #2632, #2633, #2634 when merged (ADR-055 table).

PR-13 (14 files): `RateLimit/RateLimitAttribute.cs`, `RateLimitOptions.cs`, `RateLimitStrategyFactory.cs`, `IRateLimiter.cs`, `AdaptiveRateLimiter.cs`, `RateLimitResult.cs`, `RateLimitState.cs` (copied unchanged, a stand-in for the #2414 core; the old copies go in PR-14), registration, `PublicAPI`, manifest, copies of `AdaptiveRateLimiterTests.cs` and `RateLimitAttributeTests.cs`, new factory tests, one guard test file (the copy of `AdaptiveRateLimiterGuardsTests.cs`, which also holds the factory guards). The limiter key is the request type; there is no tenant dimension (tenant partitions, the fail-closed rule without tenant and the injective tenant-qualified key belong to #740 and the #2414 core).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: PR-1 to PR-10 merged.
TASK: Bulkhead over ConcurrencyLimiter, then the old bulkhead removal, then the rate limit stand-in.
KEY RULES: the queue bound must hold under concurrency (test with parallel callers); dispose never releases a bucket with outstanding permits; rejection is a coded Left (rate limit ErrorCategory.RateLimited, bulkhead Unavailable); the adaptive limiter moves with no logic change.
REFERENCE FILES: src/Encina.Polly/Bulkhead/BulkheadManager.cs, src/Encina.Polly/RateLimiting/AdaptiveRateLimiter.cs, src/Encina/Errors/EncinaErrorCodes.cs
```

</details>

### Phase 6: Remove `Encina.Polly` and `Encina.Extensions.Resilience`

<details>
<summary>Tasks</summary>

PR-14 (15 files): delete from `Encina.Polly` `RateLimitingPipelineBehavior.cs` (its last logger, ids 3915-3917), `RateLimitAttribute.cs`, `IRateLimiter.cs`, `AdaptiveRateLimiter.cs`, `RateLimitResult.cs` and `RateLimitState.cs`; edit its `ServiceCollectionExtensions.cs` (it then registers nothing), `PublicAPI.Unshipped.txt` and manifest; edit `EncinaEventIdAllocationTests.cs` (the `Encina.Polly` row at `:100` goes here, finding of `AssemblyRanges_HasNoStaleEntries`); delete `AdaptiveRateLimiterTests.cs`, `RateLimitAttributeTests.cs`, `RateLimitingPipelineBehaviorTests.cs`, `tests/Encina.GuardTests/Infrastructure/Polly/RateLimitingPipelineBehaviorGuardsTests.cs` and `AdaptiveRateLimiterGuardsTests.cs`.

PR-15 (3 files): the names of the removed packages outside them. `src/Encina.Cli/Commands/AddCommand.cs` maps the resilience providers `polly` and `standard` to `Encina.Polly` and `Encina.Extensions.Resilience` (`:252-253`, with the argument text at `:121` and the message at `:260`); it maps to the single package `Encina.Resilience`. `src/Encina/Pipeline/Ordering/PipelineOrderValidator.cs:105,109` lists both packages by name; the list names `Encina.Resilience`. `tests/Encina.UnitTests/Cli/Commands/AddCommandTests.cs:176` (`add resilience polly`) follows the command.

PR-16 (13 files): delete the rest of `src/Encina.Polly/` (`Encina.Polly.csproj`, `GlobalSuppressions.cs`, `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, `README.md`, `ServiceCollectionExtensions.cs`), `.github/coverage-manifest/Encina.Polly.json`, the `Encina.slnx` row (line 45), `.github/scripts/coverage-weights.json` (line 33) and, in the four test projects, the `ProjectReference` lines (`Encina.UnitTests.csproj:110`, `Encina.GuardTests.csproj:102`, `Encina.IntegrationTests.csproj:121`, `Encina.PropertyTests.csproj:82`) together with the `<Using Include="Encina.Polly" />` items (`Encina.GuardTests.csproj:72`, `Encina.IntegrationTests.csproj:92`), which sit in the same four files.

PR-17 (10 files): `tests/Encina.GuardTests/Extensions/Resilience/StandardResiliencePipelineBehaviorGuardsTests.cs`, the three unit test classes under `tests/Encina.UnitTests/Extensions/Resilience/`, `tests/Encina.IntegrationTests/Observability/Resilience/StandardResilienceEndToEndTests.cs`, the benchmark project `Encina.Extensions.Resilience.Benchmarks` (`.csproj`, `Program.cs`, `StandardResilienceBenchmarks.cs`), its perf manifest and the `Encina.slnx` row (line 149); line 174 of `benchmarks.yml` is the orchestrator's.

PR-18 (6 files): delete `StandardResiliencePipelineBehavior.cs` (its last logger, ids 3950-3955) and `StandardResilienceOptions.cs` from `Encina.Extensions.Resilience`; edit its `ServiceCollectionExtensions.cs`, `PublicAPI.Unshipped.txt` and manifest; edit `EncinaEventIdAllocationTests.cs` (the `Encina.Extensions.Resilience` row at `:82` goes here).

PR-19 (13 files): delete the rest of `src/Encina.Extensions.Resilience/` (`.csproj`, `GlobalSuppressions.cs`, both `PublicAPI` files, `README.md`, `ServiceCollectionExtensions.cs`), `.github/coverage-manifest/Encina.Extensions.Resilience.json`, the `Encina.slnx` row (line 34), `coverage-weights.json` (line 34) and the four test-project files that reference it (`Encina.UnitTests.csproj:183`, `Encina.GuardTests.csproj:148` and its `<Using>` at `:85`, `Encina.IntegrationTests.csproj:177`, `Encina.PropertyTests.csproj:142`). `AddEncinaStandardResilience` has no replacement: its job is `AddEncinaResilience(o => o.Default(...))` (#2704 closes with the removal). The files under `docs/coverage/data`, `docs/mutations/data` and `docs/benchmarks/data` are generated by their pipelines and not edited.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: PR-1 to PR-13 merged; every capability of the old packages exists in Encina.Resilience.
TASK: Remove what is left of the two old packages in the pull requests of the plan (PR-14 to PR-19), tests first, source second, and drop each package's EventId assembly row in the pull request that deletes its last logger.
KEY RULES: nothing aliased or kept; grep the repository for Encina.Polly and Encina.Extensions.Resilience before and after (tests, slnx, props, docs/coverage and docs/mutations data are generated and left to their pipelines); the full solution builds with zero warnings after each pull request; .github/workflows/* is edited by the orchestrator only.
REFERENCE FILES: Encina.slnx, .github/scripts/coverage-weights.json, tests/Encina.UnitTests/Encina.UnitTests.csproj
```

</details>

### Phase 7: Cross-Cutting Integration, EventIds and Direct Users

<details>
<summary>Tasks</summary>

PR-20 (2 files): `src/Encina.Security.Secrets/Encina.Security.Secrets.csproj` (the `Polly` package reference becomes a `ProjectReference` to `Encina.Resilience`; its decorator keeps using Polly types, which arrive transitively) and `src/Encina.Compliance.NIS2/Encina.Compliance.NIS2.csproj` (`:24`, a `Polly` package reference with no Polly code in the project: removed, no reference added). Both reference only the `Polly` package today, not `Encina.Polly`, so nothing breaks when the packages go; the change follows ADR-055 R1 ("`Encina.Security.Secrets` and `Encina.Compliance.NIS2` reference `Encina.Resilience` instead of Polly directly"), which is why it is Settled by that text and not a new dependency decision. The `Encina.Resilience` assembly row and the summary comment of `EventIdRanges.cs` were added in PR-3. The EventIds: the ids 3900-3921 of the deleted behaviors stay registered and unused (retry attempts, breaker transitions and rejections become Polly telemetry and span events, #728 R5); new ids are 3925-3930 (`PipelineCompiled`, `PipelineReloaded`, `PipelineReloadFailed`, `ConflictsCollected`, `CallContributorFailed`, `PipelineUnavailable`), packed, inside the registered `Polly` range after the 3922-3924 reserved by #740. If #4007 OP-2 lands first the same offsets apply in 60050-60099.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: both old packages removed.
TASK: Re-point the two direct users to Encina.Resilience per ADR-055 R1.
KEY RULES: ids only inside the registered range, packed; run the architecture tests (EncinaEventIdAllocationTests, EventIdRegistryAgreementTests); no logging of EncinaError.Message, exception type and stack through ForLogging() only.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md, src/Encina.Security.Secrets/Resilience/ResilientSecretReaderDecorator.cs
```

</details>

### Phase 8: Single Owner of Immediate Retries, Testing and Documentation

<details>
<summary>Tasks</summary>

PR-21 (8 files, Dilemma 7 option A, added first so immediate retries never stop working): `src/Encina.Messaging/Recoverability/RecoverabilityRetryConfigurator.cs` (an `IConfigureOptions<ResilienceOptions>` that applies the mapping `MaxAttempts = ImmediateRetries + 1` to the request types the behavior wraps, unless the type has its own entry), `RecoverabilityServiceCollectionExtensions.cs` (modified: a registration error, coded and raised at startup, when `ImmediateRetries > 0` and `Encina.Resilience` is not registered, ADR-050), `Encina.Messaging.csproj` (the `ProjectReference`), `PublicAPI.Unshipped.txt`, the Messaging manifest, `RecoverabilityRetryConfiguratorTests.cs`, `RecoverabilityResilienceRequirementTests.cs` and the Messaging DI test (modified).

PR-22 (12 files): `RecoverabilityPipelineBehavior.cs` (remove `ExecuteWithImmediateRetriesAsync`, `:325`), `RecoverabilityOptions.cs` (remove `ImmediateRetryDelay`, `UseExponentialBackoffForImmediateRetries` and `MaxJitterPercent`; `ImmediateRetries` stays), `PublicAPI.Unshipped.txt`, `README.md` of `Encina.Messaging`, the Messaging manifest, and the seven test files that use a removed member (found by `Select-String` on `origin/main` at `37b2edd32`): `RecoverabilityGuardTests.cs`, `DeadLetterSourcesEndToEndScenario.cs`, `DeadLetterSourcesCaptureTests.cs`, `RecoverabilityPipelineBehaviorTests.cs`, `DelayedRetryChainTests.cs`, `RecoverabilityOptionsCompositionTests.cs`, `RecoverabilityOptionsTests.cs` (the `Handle_JitterAboveHundredPercent_FloorsDelayAtZero` test goes, #2714). The other eleven files that mention only `ImmediateRetries` keep compiling and are untouched.

PR-23 (14 files): `docs/architecture/adr/050-modules-may-require-modules.md` (the closed-list row), package `README.md`, `docs/en/guides/resilience.md`, contract tests (`ResilienceStrategyFactoryContractTests.cs`, `ResilienceBehaviorContractTests.cs`) with their manifest targets, justification files for load and benchmark tests, `ROADMAP.md` if applicable, changelog fragments `4380-encina-resilience.added.md` and `4380-resilience-packages-removed.removed.md`. Documentation goes through `docs-writer`.

Test coverage per PR: unit, guard and integration tests are in their pull requests (above); property tests are justified (no invariant beyond the order test); load and benchmark tests are a `.md` justification (the compiled pipeline is built once; the per-call allocation benchmark is #728 R5).

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
CONTEXT: PR-1 to PR-20 merged.
TASK: Make Encina.Resilience the only owner of immediate retries, then document and finish.
KEY RULES: delayed retries and the dead letter queue stay in Messaging; a Left from a store fails the operation (never success); the registration error is a coded startup failure; docs by docs-writer with covref markers, no typed coverage figures.
REFERENCE FILES: src/Encina.Messaging/Recoverability/RecoverabilityPipelineBehavior.cs, docs/architecture/adr/050-modules-may-require-modules.md, docs/architecture/adr/055-resilience-family.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Standard | Relevance |
|----------|-----------|
| ADR-055 R1 to R4 | One package, compiled per-type pipeline, front-ends, single owner of immediate retries |
| ADR-029, ADR-050 | Retry only classified-transient errors; several retry layers on one request are a registration error |
| ADR-062 | `ErrorCategory` of every rejection: circuit open `Unavailable`, bulkhead full `Unavailable` (Dilemma 5), rate limit `RateLimited`, pipeline unavailable `Internal` |
| ADR-054 D4 | Tenant tag only when multi-tenancy is on; owned by #728, the context key carries the request context for it |
| AGENTS.md sections 3, 6, 7, 9 | ROP, `TimeProvider`, fail-closed, EventIds, per-flag targets, CRAP |
| Microsoft.Extensions.Resilience, Resilience4j | Comparable: one ordered pipeline, decorators |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `PipelineStage`, `PipelineBuilder` ordering | `src/Encina/Pipeline/Ordering/PipelineStage.cs`, `src/Encina/Pipeline/PipelineBuilder.cs` | The two behaviors declare their stage |
| `IErrorClassifier`, `DefaultErrorClassifier` | `src/Encina/Recoverability/` | Retry gating |
| `EncinaErrors.Create` with `ErrorCategory` | `src/Encina/Errors/` | Coded rejections |
| `IDatabaseHealthMonitor`, `DatabaseResilienceOptions` | core `Encina.Database` | Database breaker (Dilemma 6) |
| `ValidateOnStart` pattern | `src/Encina/Identity/RequestIdentityServiceCollectionExtensions.cs` | Startup validation |
| Old attributes, `AdaptiveRateLimiter` | `src/Encina.Polly/` | Moved |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Resilience` | 3925-3930 (inside the registered `Polly` range 3900-3949) | Six ids; 3922-3924 reserved by the #740 plan; 3900-3921 keep their meaning for surviving logs; no new range needed. Capability ids of the siblings come from their own pre-zone blocks 900-1049 |

### Estimated File Count

| Category | Count |
|----------|-------|
| New production files | 30 |
| Modified or deleted production files | 34 (28 deleted with the old packages) |
| New tests | 24 |
| Deleted or moved tests | 28 |
| Project, solution, manifest, `PublicAPI`, README, fragments | 22 |

### Per-File Coverage Targets (new files, set in the pull request that adds each file with its test file)

Every new file has a row, each set in the pull request that adds it and its test file; a 0 has its reason in the row. Files with no argument-checking code get guard 0 only where the reason says so; guard targets follow the share of argument checks (the existing evidence: `Encina.Cdc` guard 15, `CircuitBreakerPipelineBehavior` guard 50 where the logger check is the only guard).

| File (PR) | Unit | Guard | Integration | Justification |
|-----------|-----:|------:|------------:|---------------|
| `ResilienceSlot.cs`, `IResilienceStrategyFactory.cs`, `IResilienceRejectionMapper.cs`, `IResilienceCallContributor.cs` (1) | 0 | 0 | 0 | Enum and interfaces: no executable lines; the implementations carry the targets |
| `ResilienceTypeContext.cs` (1) | 95 | 30 | 0 | Small class driven by `ResilienceContractsTests`; its constructor and `GetAttribute` argument checks are about a third of the lines |
| `ResiliencePropertyKeys.cs` (1) | 90 | 0 | 0 | Static keys only; no parameters to guard |
| `ResilienceOptions.cs`, `ResilienceTypeBuilder.cs` (2) | 95 | 40 | 0 | Builders carry the argument checks that `ResilienceOptionsGuardTests` reach |
| `ResilienceTypeOptions.cs` (2) | 90 | 0 | 0 | Property bag with defaults and no argument checks |
| `ResilienceOptionsValidator.cs` (2) | 95 | 15 | 0 | One null check; the validation branches are unit-driven |
| `ResilienceServiceCollectionExtensions.cs` (2, extended in 3, 4, 5, 8, 9, 11, 13) | 95 | 30 | 80 | Registration tests with `ValidateOnBuild` and `ValidateScopes`; the end-to-end tests run the registered pipeline (old `ServiceCollectionExtensions.cs` of Polly: unit 55, integration 30, so higher) |
| `ResiliencePipelineCompiler.cs`, `ResiliencePipelineRegistry.cs` (3) | 95 | 15 | 0 | Order and precedence tests with fake factories reach every branch; the guard share is the constructor checks |
| `ResilienceConflictCollector.cs`, `ResilienceRequestTypeCatalog.cs` (3) | 95 | 20 | 0 | Pure logic; guard covers the two public entry points |
| `Diagnostics/ResilienceLogMessages.cs` (3) | 90 | 0 | 0 | Source-generated `[LoggerMessage]` partials with no parameters to check; the compiler and behavior tests emit every message |
| `AdmissionPipelineBehavior.cs`, `ResiliencePipelineBehavior.cs` (4) | 90 | 25 | 70 | Constructor guards are about a quarter of the lines; `ResiliencePipelineEndToEndTests` drives the pipeline |
| `Retry/RetryAttribute.cs` (5) | 95 | 0 | 80 | The old row keeps its numbers (unit 95, integration 80); the attribute has no argument checks, and `RetryEndToEndTests` of the same pull request carries the integration target |
| `Retry/RetryOptions.cs` (5) | 90 | 0 | 0 | Option bag; validated by the options validator tests |
| `Retry/RetryStrategyFactory.cs` (5) | 95 | 15 | 75 | Keeps the old `RetryPipelineBehavior` row (unit 95, guard 5, integration 75), guard raised to the constructor-check share; `RetryEndToEndTests` is in the same pull request |
| `CircuitBreaker/CircuitBreakerAttribute.cs`, `CircuitBreakerOptions.cs` (8) | 90 | 0 | 0 | Attribute and bag, no argument checks |
| `CircuitBreaker/CircuitBreakerStrategyFactory.cs`, `CircuitBreakerRejectionMapper.cs` (8) | 90 | 20 | 0 | Open, half-open and closed paths with `FakeTimeProvider` (old behavior: unit 90, guard 50) |
| `ResilienceErrorCodes.cs` (1) | 0 | 0 | 0 | Constants only, no executable lines |
| `Database/DatabaseBreakerStrategyFactory.cs` (9) | 90 | 15 | 0 | Predicate and health-monitor branches are unit-driven; guard is the constructor checks |
| `Database/DatabaseTransientErrorPredicate.cs` (9) | 90 | 40 | 0 | Pure classification code; the copied guard tests reach its argument checks |
| `Bulkhead/BulkheadAttribute.cs`, `BulkheadOptions.cs` (11) | 90 | 0 | 0 | Validation lives in the options validator, so these have no branches |
| `Bulkhead/BulkheadStrategyFactory.cs`, `RateLimiterRejectionMapper.cs` (11) | 90 | 15 | 0 | The queue bound is tested under parallel callers; guard is the constructor checks |
| `RateLimit/RateLimitAttribute.cs`, `RateLimitOptions.cs` (13) | 90 | 0 | 0 | Attribute and bag |
| `RateLimit/RateLimitStrategyFactory.cs` (13) | 90 | 15 | 0 | Strategy over the copied limiter; guard is the constructor checks |
| `RateLimit/AdaptiveRateLimiter.cs` (13) | 90 | 40 | 0 | Copied with its unit and guard tests (the old file had the package default); `AdaptiveRateLimiterGuardsTests` reach its argument checks |
| `RateLimit/IRateLimiter.cs`, `RateLimitResult.cs`, `RateLimitState.cs` (13) | 90 | 0 | 0 | Interface and small records driven by the limiter tests; no argument checks |
| `Messaging/Recoverability/RecoverabilityRetryConfigurator.cs` (21, in `Encina.Messaging`) | 95 | 30 | 0 | One mapping and two null checks, driven by `RecoverabilityRetryConfiguratorTests` |

The deleted files carry their old manifest rows away with them (a target is removed only together with its source file, the ratchet rule); `RetryTelemetry` (unit 95, integration 30) goes with its file because its counters become Polly instruments (#728 R5), which is intended. The package-level targets of the new `Encina.Resilience.json` are unit 85, guard 20 and integration 60, and every per-file row above is at or above them where it has coverable lines. Edits to `RecoverabilityPipelineBehavior.cs` and `RecoverabilityOptions.cs` in PR-22 only delete code and leave their existing rows unchanged.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #4380: create Encina.Resilience (ADR-055 R1 to R4) and remove Encina.Polly and Encina.Extensions.Resilience. AGENTS.md rules apply; #4382 (token on the continuation) lands before PR-4.

IMPLEMENTATION OVERVIEW: (1) project and contracts; (2) options model and builder; (3) compiler, registry, conflict collector, logs; (4) two behaviors and AddEncinaResilience; (5-13) retry, breaker, database breaker, bulkhead and rate limit added, their old copies deleted in later pull requests; (14-19) the two old packages and their references removed; (20) direct users; (21-23) Messaging immediate retries, ADR-050 row, docs.

KEY PATTERNS: attributes read once; precedence options > attributes > defaults; fixed slot order; two behaviors at Admission and Resilience; rejections are coded Lefts with named ErrorCategory and constant messages; cancellation propagates, never a Left; per-call scoped services only through the call contributor; time from TimeProvider; specialists for PublicAPI, manifests, fragments and docs.

REFERENCE FILES: docs/architecture/adr/055-resilience-family.md, src/Encina.Polly/ServiceCollectionExtensions.cs, src/Encina/Pipeline/Ordering/PipelineStage.cs, src/Encina/Errors/EncinaErrorCodes.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable; stampede protection and fail-safe belong to the cache providers (ADR-055 settled points) |
| 2 | OpenTelemetry | ⏭️ | #728 (R5); this plan puts the request context in the `ResilienceContext` so the enricher can read tenant (ADR-054 D4) and creates no instrument |
| 3 | Structured Logging | ✅ | EventIds 3925-3930, `[LoggerMessage]`, no `EncinaError.Message` |
| 4 | Health Checks | ❌ | No dependency of its own; the database breaker reads the existing `IDatabaseHealthMonitor` |
| 5 | Validation | ✅ | Options and conflicts validated at startup (choice 11) |
| 6 | Resilience | ✅ | This is the feature |
| 7 | Distributed Locks | ❌ | No shared state; breaker state is per container |
| 8 | Transactions | ❌ | The Resilience stage sits outside the transaction stage, so a retry re-runs the whole transaction |
| 9 | Idempotency | ✅ | A command is retried only when idempotent (ADR-029) |
| 10 | Multi-Tenancy | ⏭️ | Pipelines are keyed by request type, no tenant in any key; tenant partitions, fail-closed without tenant and injective keys are #740 and the #2414 core (the moved limiter has no tenant dimension) |
| 11 | Module Isolation | ❌ | Not applicable |
| 12 | Audit Trail | ❌ | Infrastructure concern |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|--------------|-------|--------|
| #4382: token on the continuation, `IRequestContext.Deadline` | Open; plan by number | PR-4 and later; PR-1 to PR-3 do not need it |
| The current `origin/main` (`37b2edd32` when this plan was revised) | Moves | Re-run the counts of "Implementation Phases" with `Select-String` at brief time |
| #728 telemetry plan | On main, choices pending | Instrument and enricher names; none created here |
| #2414 partitioned core | Open SPIKE with decisions D1 to D3; no implementation issue exists | Prerequisite of the final rate-limit and bulkhead design: PR-11 and PR-13 use the stand-ins of Dilemma 1 until the maintainer opens its issue and plan |
| #3424 (uncoded rejections) | Open bug | Closed by PR-8, PR-11 and the removals |
| #2628 to #2634, #2704 | Open | Absorbed by PR-11, PR-12 and PR-19 |
| #4007 OP-2 | Open | Ordering of EventId offsets (Phase 7) |
| `.github/workflows/benchmarks.yml` | Orchestrator's hot spot | Lines 173 (PR-6) and 174 (PR-17) edited by the orchestrator |

---

## Next Steps

1. Commit this plan, open its PR and link it from #4380.
2. When #4380 is picked up, re-check the choices against today's code and ask the maintainer about the Dilemmas (choices 1 to 7).
3. Brief workers for PR-1 to PR-23 in order, after #4382 PR-2 for PR-4 and later.
