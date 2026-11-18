# Implementation Plan: Fallback and graceful degradation in `Encina.Resilience`

> **Issue**: [#137](https://github.com/dlrivada/Encina/issues/137)
> **Type**: Feature
> **Complexity**: Medium (7 phases, no database providers, 3 pull requests)
> **Estimated Scope**: ~500 lines of production code + ~800 lines of tests (about 1,800 changed lines in total, three pull requests)

---

## Summary

Add `[Fallback]` and `IFallbackHandler<TRequest, TResponse>` so that a failed query can return a substitute result: a default value, the value of a factory method on the request, or the answer of an alternative handler. The strategy is Polly v8's fallback strategy, compiled once per request type into the per-type pipeline ([ADR-055](../architecture/adr/055-resilience-family.md) R2, R3). #137 sits in milestone v0.18.0, which SPEC-000 DEC-009 moves into 1.0; it has no comment of its own, and the spike report (section 5) notes that #137 has no 1.0 decision beyond that milestone.

What exists today (read in `origin/main` at `f484cdf4f`):

- Nothing: `git grep -E "IFallbackHandler|FallbackAttribute|FallbackStrategy" -- src` returns no match. `src/Encina.Resilience` does not exist (ADR-055 R1 plans it; no open issue creates it) and `RequestHandlerCallback<TResponse>()` has no `CancellationToken` (R6, no issue yet).
- The fixed strategy order of ADR-055 R2 (rate limiter, total timeout, retry or hedging, circuit breaker, per-attempt timeout, chaos) does not mention fallback; Design Choice 3 settles the position as a Dilemma.
- ADR-055 settled points say stampede protection, fail-safe and eager refresh are caching-provider features, not resilience strategies; the open issue #268 (fail-safe) owns serving stale cache data. This shapes Design Choice 5.

Deliverables, all in `Encina.Resilience`: `FallbackAttribute`, `FallbackStrategy`, `FallbackOptions`, `IFallbackHandler<,>`, the strategy factory (default value, factory method, alternative handler), startup validation, a counter for applied fallbacks, four `[LoggerMessage]` definitions and tests.

- **Affected packages**: `Encina.Resilience` (planned), `Encina` core (`EventIdRanges.cs`). `Encina.AspNetCore` needs no change.
- **Provider category**: Resilience; no database, caching, transport, lock or validation matrix applies (the cached-value strategy is Design Choice 5).
- **Prerequisites**: the foundation and R6 (see "Prerequisites & Dependencies"), tied to Dilemma 1 of the #136 plan: if the foundation is not on main when #137 is picked up, sequencing follows the maintainer's answer there.

### Shared Pieces and Ownership

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `Encina.Resilience` project, per-type compiler, options front-ends | foundation issue (issue file `resilience-foundation-encina-resilience.md`) | all |
| R6 token and deadline | R6 issue (issue file `resilience-r6-pipeline-token-and-deadline.md`) | #136, #141, #335 |
| `FallbackAttribute`, `FallbackStrategy`, `IFallbackHandler<,>` | this plan (#137) | #335 (fallback on timeout) may reuse them (sibling plan, by number) |
| EventIds 906-911 (`ResilienceFallback`) | this plan | none |
| Where Dilemma 1 (sequencing) is answered | #136 plan, choice 1 | this plan |

---

## Design Choices

<details>
<summary><strong>1. Where the code lives while `Encina.Resilience` does not exist</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Wait for the foundation; implement in `Encina.Resilience`** | One implementation in its final home; nothing for R1 to delete | Blocked on an unplanned foundation |
| **B) Build now in `Encina.Polly`, move with the consolidation** | Ships earlier | Rework when R1 removes the package; a second behavior per strategy, which R2 rejected |

### Chosen Option: **A — Wait for the foundation** (recommended, pending the maintainer)

### Rationale

- A is the order of the spike report (foundation before new strategies). B wins if graceful degradation must ship before the foundation. The question is asked once for the five plans of the family (#136 choice 1).

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Strategy form: `FallbackPipelineBehavior` or a strategy of the per-type pipeline</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Polly's fallback strategy in the per-type pipeline** | Order guaranteed, attributes read once, `OnFallback` telemetry from Polly | The class named in the issue does not exist |
| **B) A `FallbackPipelineBehavior<,>`** | Matches the issue text | R2 option 2b was rejected |

### Chosen Option: **A — Polly's fallback strategy** (recommended, pending the maintainer)

### Rationale

- The acceptance criterion "`FallbackPipelineBehavior` implementation" is met by the Resilience-stage behavior that runs the compiled pipeline.
- This choice is a follow-on of choice 1 and is answered with it: under option A of choice 1 it is Settled by ADR-055 R2 (2a chosen, 2b and 2c rejected); under option B there is no compiler in `Encina.Polly`, so fallback would be a standalone `FallbackPipelineBehavior<,>`, replaced when R1 consolidates.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Position of the fallback in the fixed order</strong></summary>

The fixed order of R2 omits fallback. The two behaviors of R2 are an Admission behavior (rate limiter, bulkhead) and a Resilience behavior (the rest). Admission rejections (rate limit, bulkhead, shed load, adaptive limit) happen before the Resilience pipeline runs.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Outermost strategy of the Resilience-stage pipeline (before total timeout)** | Covers handler failures, total timeout, an open circuit and exhausted retries; a rejected request stays rejected | A request refused by the rate limiter, bulkhead or load shedding gets no fallback |
| **B) A separate fallback layer outside the Admission behavior** | The caller also gets a degraded answer when the system sheds load | Shedding exists to cut work; a fallback handler that queries a store adds work exactly when the system is overloaded |

### Chosen Option: **A — Outermost of the Resilience-stage pipeline** (recommended, pending the maintainer)

### Rationale

- A wins when degradation means "the dependency failed"; B wins for a catalog page that should always render something, even when shed.
- Polly's guidance and Resilience4j place the fallback outside retry and breaker; both options keep that.

- **Class:** Dilemma

</details>

<details>
<summary><strong>4. What may have a fallback: queries only, or commands too</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IQuery<>` requests only; `[Fallback]` or a handler on an `ICommand<>` is a startup error** | A failed command is never reported as a success with a default value; matches ROP (a failure is a `Left`) | A command that wants "accepted, will retry later" must model it in its handler |
| **B) Commands allowed, with the handler deciding** | Flexible (enqueue and answer "accepted") | A default value on a command hides a failure; the command did not happen |

### Chosen Option: **A — Queries only** (recommended, pending the maintainer)

### Rationale

- A wins in the reference application (a failed clinical-record write must never look like a success). B wins for a command whose fallback is a deliberate deferred acceptance.

- **Class:** Dilemma

</details>

<details>
<summary><strong>5. The cached-value strategy of the issue</strong></summary>

The issue lists `FallbackStrategy.CachedValue` ("last known good value from cache"), but it is not among the acceptance criteria (those name default values, factory methods and alternative handlers). ADR-055 settled points put fail-safe (serving stale data on failure) in the caching providers, and #268 is the open issue for it.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not implemented here; the enum has no `CachedValue` member; an application writes a cache-reading `IFallbackHandler` itself, and a later issue adapts #268's fail-safe** | No second stale-cache mechanism; no personal data copied into a new store; no dependency of `Encina.Resilience` on `Encina.Caching` (which references EF Core, Messaging and DistributedLock) | The issue's strategy is absent until #268 |
| **B) Read-only: read the entry the query cache wrote, through `ICacheProvider` and `ICacheKeyGenerator`** | Works with `[Cache]` today | An entry that expired is gone; a heavy package reference; tenant and user key rules must be repeated |
| **C) The fallback behavior writes its own last-known-good copy on every success** | Always available | Duplicates fail-safe against ADR-055; stores responses (possible health data) outside the caching rules; write cost on every call |

### Chosen Option: **A — Defer to #268** (recommended, pending the maintainer)

### Rationale

- A wins while ADR-055 keeps stale serving in the caching providers. B wins if a stale read of a live `[Cache]` entry is enough and `Encina.Caching` is acceptable as a dependency. C wins only if the maintainer wants resilience to own the copy.
- The omitted task is also recorded as an issue file (`resilience-fallback-cached-value.md`) so it is not lost.

- **Class:** Dilemma

</details>

<details>
<summary><strong>6. Context-aware selection (optional in the issue)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No separate selector: `IFallbackHandler.HandleFallback(request, originalError, ct)` receives the original error, so the handler chooses by error category, and may read the ambient context through `IRequestContextAccessor`** | No extra abstraction; the criterion is optional | The enum has no `ContextAware` member |
| **B) A `FallbackStrategy.ContextAware` with an `IFallbackSelector`** | Matches the enum in the issue | A second interface for what a handler can already do |

### Chosen Option: **A — The handler chooses** (recommended, pending the maintainer)

### Rationale

- A wins on simplicity; B wins if selection rules must be configured without code (per tenant tier, per environment).

- **Class:** Dilemma

</details>

<details>
<summary><strong>7. Which failures trigger a fallback</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `Left` whose `ErrorCategory` is `Unavailable`, `Timeout`, `UpstreamFailure` or `Internal` (ADR-062), and an exception from the handler; `FallbackOn` (an `ErrorCategory[]`) can widen it** | A validation or authorization error is never masked; protocol-neutral | A not-found cannot get a default unless listed |
| **B) Every `Left`** | Simple; matches the issue's "when primary operations fail" | A validation or forbidden error becomes a default value |

### Chosen Option: **A — Failure categories, widenable** (recommended, pending the maintainer)

### Rationale

- A wins for security and data correctness (a `Forbidden` must stay forbidden). B wins where the application wants "Guest" for a missing user, which A allows by listing `NotFound`.

- **Class:** Dilemma

</details>

<details>
<summary><strong>8. Result when the fallback itself fails</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Return the original error with added details, for both a `Left` of the fallback and an exception thrown by the factory, the handler or the JSON parse: `encina.fallback.failed = true` and either `encina.fallback.error_code` (the fallback's code) or `encina.fallback.exception_type` (the exception's type name); never a message** | The caller sees the real cause; its category is kept (ADR-062); no new code; no exception ever leaves the pipeline | The error is rebuilt with `EncinaErrors.Create` using the original code and category |
| **B) A new code `encina.fallback.failed`** | Distinguishable | Hides the original cause; needs an ADR-062 category (`Internal`) |
| **C) Return the fallback's own `Left`** | Polly's default | The original failure is lost |

### Chosen Option: **A — Original error with details** (recommended, pending the maintainer)

### Rationale

- A wins when operators and callers act on the primary failure. B wins when the fallback failure itself is the alarm. This plan adds no error code, so no ADR-062 category row is needed; the rebuilt error uses `EncinaErrors.Create(code, category: original category, constant message, details: ...)` and never `ex.Message`.

- **Class:** Dilemma

</details>

<details>
<summary><strong>9. Resolving the handler</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The Resilience behavior resolves `IFallbackHandler<,>` from the request's service provider for each call and passes it in `ResilienceContext` properties** | No scoped service captured by the singleton pipeline | None |
| **B) Capture the handler at startup** | No per-call work | A scoped handler in a singleton fails `ValidateScopes` |

### Chosen Option: **A — Per-call resolution**

### Rationale

- The compiled pipeline is a singleton in Polly's registry; the DI validation of AGENTS.md section 3 rejects a scoped dependency in it. A missing handler registration is an `InvalidOperationException` at startup naming the type.
- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (registration completeness proven with `ValidateOnBuild` and `ValidateScopes`)

</details>

<details>
<summary><strong>11. The attribute contract: default value format, factory binding and instance reuse</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) As the issue samples: `DefaultValue` is a JSON string (`"[]"`), `FallbackFactory = nameof(Method)` names a public static parameterless method of the request type, bound by reflection once at startup into a delegate; the JSON is parsed once at startup to validate it and deserialized again for each fallback, so no instance is shared between callers; a missing method, a non-static or wrongly typed method or unparsable JSON is an `InvalidOperationException` at startup naming the type** | Matches the issue; a returned list is never shared | A small parse per fallback |
| **B) Parse once and share the instance** | No per-call work | A caller mutating a returned list changes the next response |

### Chosen Option: **A — The issue's contract, deserialized per fallback**

### Rationale

- The property names `DefaultValue` and `FallbackFactory` are the issue's. Reflection binding happens once at startup, never per call.
- **Class:** Trivial
- **Settled by:** #137 "Attribute-based (Simple)" samples (`[Fallback(DefaultValue = "[]")]`, `[Fallback(FallbackFactory = nameof(GetDefaultUser))]`)

</details>

<details>
<summary><strong>12. The handler's public signature</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The issue's signature: `Task<Either<EncinaError, TResponse>> HandleFallback(TRequest request, EncinaError originalError, CancellationToken ct)`** | Matches the issue and its handler sample | `Task` allocates where `ValueTask` would not |
| **B) `ValueTask<...> HandleFallbackAsync(...)`, like `IPipelineBehavior.Handle`** | No allocation on a synchronous path; `Async` suffix convention | A public rename of the issue's interface |

### Chosen Option: **A — The issue's signature** (recommended, pending the maintainer)

### Rationale

- A wins on fidelity to the issue; B wins if the fallback is commonly answered synchronously from memory and the allocation matters. A fallback runs only after a failure, so the cost is off the hot path.

- **Class:** Dilemma

</details>

<details>
<summary><strong>10. Telemetry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Polly's `OnFallback` event for the count; one Encina counter named by the convention #728 settles (proposed `encina.resilience.fallback.applied`) with tags `request.type`, `fallback.strategy` (one value per strategy this plan implements: `default`, `factory`, `handler`; a value is added if choice 5 or 6 is answered with a new strategy) and the tenant only with multi-tenancy** | R5: Encina measures only what Polly does not; the strategy kind is unknown to Polly | One counter |
| **B) Polly's events only, no Encina instrument** | No names to agree | The applied strategy kind is invisible |

### Chosen Option: **A — One counter for the strategy kind** (recommended, pending the maintainer)

### Rationale

- ADR-055 R5 and ADR-054 D3 and D6.2 fix the rules, not these names. #728 (plan on main) leaves its choices 1 (who holds the `Meter("Encina.Resilience")` instance) and 8 (instrument names) pending, so this counter takes that holder and convention; this plan creates no `Meter` of its own and `FallbackTelemetry` only records through the holder. A wins when operators need the strategy kind; B wins if only the count matters.
- **Class:** Dilemma

</details>

---

## Implementation Phases

Three pull requests, in order: **PR-1** Phases 1 and 2 with their tests; **PR-2** Phases 3 and 4 (registration, builder extension, contract and DI tests); **PR-3** Phases 5 to 7. Every PR needs the `Encina.Resilience` project to exist (foundation merged); under option C of choice 1 (creating the project in the first PR) the split is recounted.

### Phase 1: Core Models and Contracts

<details>
<summary>Tasks</summary>

1. `src/Encina.Resilience/Fallback/FallbackStrategy.cs`: `public enum FallbackStrategy { DefaultValue, FactoryMethod, AlternativeHandler }`.
2. `FallbackAttribute.cs`: `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)] public sealed class FallbackAttribute : Attribute` with `string? DefaultValue { get; init; }` (JSON), `string? FallbackFactory { get; init; }` (use `nameof`, choice 11), `bool UseHandler { get; init; }` and `ErrorCategory[]? FallbackOn { get; init; }`; exactly one of the three sources.
3. `FallbackOptions.cs`: the merged model (`Strategy`, `DefaultValueJson`, `FactoryMethodName`, `FallbackOn`) with `public static FallbackOptions From(FallbackAttribute attribute)` for the R3 precedence (null attribute rejected with `ArgumentNullException.ThrowIfNull`, guard test).
4. `IFallbackHandler.cs`: `public interface IFallbackHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>` with `Task<Either<EncinaError, TResponse>> HandleFallback(TRequest request, EncinaError originalError, CancellationToken cancellationToken)` (choice 12).
5. `FallbackOptionsValidator.cs` (internal): the startup rules of choices 4, 9 and 11.
6. `src/Encina.Resilience/Encina.Resilience.csproj`: `InternalsVisibleTo` for `Encina.UnitTests` and `Encina.GuardTests` (internal validator, factory and telemetry are tested directly); a foundation acceptance item, added here only if missing (counted below).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina, issue #137, plan docs/plans/fallback-implementation-plan-137.md. src/Encina.Resilience exists with the per-type compiler (ADR-055 R1, R2) and the continuation takes a CancellationToken (R6).
TASK: Create the Phase 1 types in src/Encina.Resilience/Fallback/ following every Design Choice of the plan.
KEY RULES: queries only; no CachedValue member; the validator throws InvalidOperationException at startup; XML docs with <example>; nullable enabled; PublicAPI lines through mechanical-fixer.
REFERENCE FILES: src/Encina.Polly/Attributes/RetryAttribute.cs, src/Encina/Abstractions/CqrsContracts.cs, src/Encina/Errors/ErrorCategory.cs
```

</details>

### Phase 2: Default Implementation

<details>
<summary>Tasks</summary>

1. `FallbackStrategyFactory.cs` (internal): builds `FallbackStrategyOptions<Either<EncinaError, TResponse>>`: `ShouldHandle` (choice 7), `FallbackAction` (default value parsed with `System.Text.Json`, factory delegate, or the per-call handler read from `ResilienceContext` properties; a `Left` from the fallback becomes the enriched original error of choice 8), `OnFallback` (Phase 5 hook).
2. `FallbackPropertyKeys.cs` (internal): the per-call handler key.
3. The strategy is the outermost slot of the Resilience-stage pipeline (choice 3).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 types exist.
TASK: Implement FallbackStrategyFactory and FallbackPropertyKeys with Polly 8.6.6 AddFallback on the generic builder.
KEY RULES: never throw for a business failure; an error is rebuilt with EncinaErrors.Create(code, category: <original category>, <constant message>, details: ...) with the named category argument and never ex.Message; the handler is resolved per call, never captured; the fallback runs after Authorization (stage 500), so it only answers authorized callers.
REFERENCE FILES: src/Encina/Results/EncinaErrors.cs, src/Encina/Errors/EncinaErrorMetadataKeys.cs
```

</details>

### Phase 3: Configuration and DI

<details>
<summary>Tasks</summary>

1. `FallbackBuilderExtensions.cs`: `For<T>().Fallback(Action<FallbackOptions>)` (builder name from the foundation); guards on both arguments.
2. The validator joins the startup validation; application registers `IFallbackHandler<,>` itself.
3. Registration test with `ValidateOnBuild` and `ValidateScopes`, including a scoped handler.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Phases 1 and 2 done.
TASK: Add FallbackBuilderExtensions, wire the validator, write the registration test.
KEY RULES: no service added without its options; precedence options > attribute > defaults tested; guard tests for each public parameter.
REFERENCE FILES: src/Encina.Resilience (foundation builder), tests/Encina.UnitTests/Recoverability/ErrorClassifierRegistrationTests.cs
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Multi-tenancy: tenant tag on telemetry only with `MultiTenancyMarker` (ADR-054 D4); no tenant-qualified key exists in this plan (cache is deferred), so the reserved `default` segment does not apply.
2. Validation: the startup rules of choices 4, 5 and 9.
3. Pay-for-what-you-use: nothing is registered or applied without `[Fallback]` or `For<T>().Fallback(...)`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Strategy and registration exist.
TASK: Add the tenant-tag rule and finish the validator rules.
KEY RULES: opt-in only; tenant tag only when MultiTenancyMarker is registered; no EncinaError.Message in telemetry or logs.
REFERENCE FILES: src/Encina/Abstractions/MultiTenancyMarker.cs
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. `EventIdRanges.cs`: `ResilienceFallback = (906, 911)`; add its line to `src/Encina/PublicAPI.Unshipped.txt` (`mechanical-fixer`) and add its name to the `["Encina.Resilience"]` entry of `AssemblyRanges` in `EncinaEventIdAllocationTests.cs`, creating the entry if no earlier plan of the family did (an entry holds several ranges). Ids 906-911 are free (`git grep` of `EventId = 90x` and of the registry returns nothing) and are emitted only by the `Encina.Resilience` assembly.
2. `Diagnostics/FallbackLogMessages.cs`: 906 `FallbackApplied` (Warning: request type, strategy, original error code), 907 `FallbackFailed` (Warning: request type, fallback error code), 908 `FallbackSkippedNotEligible` (Debug), 909 `FallbackActionThrew` (Warning, emitted when the factory, the handler or the JSON parse throws; the caller then gets the original error with the exception-type detail of choice 8; the exception goes through `ForLogging()`); 910-911 spare.
3. `FallbackTelemetry.cs`: the counter of choice 10 and a span event `fallback.applied`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: ADR-021, ADR-054, ADR-055 R5.
TASK: Register 906-911 first, then add the log messages and telemetry; run the architecture tests.
KEY RULES: ids packed from 906; emitted only by the Encina.Resilience assembly; error code only in logs; exceptions through ForLogging(); no activation switch.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 6: Testing

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/Resilience/Fallback/`): default value returned for a `Unavailable` error; factory method; handler resolved per call (two calls, two scoped instances); `Validation` error not masked; `FallbackOn = [NotFound]` widens; fallback `Left` returns the original error with details and the original category; a command type fails validation; unparsable default fails at startup; two calls get distinct deserialized instances.
2. Guard (`tests/Encina.GuardTests/Resilience/Fallback/`): `FallbackOptions.From` null attribute (PR-1); builder extension nulls (PR-2); nullable `FallbackOn` null accepted (valid result). Unit also covers: a factory or handler that throws returns the original error with `encina.fallback.exception_type` and logs 909.
3. Contract (`tests/Encina.ContractTests/Resilience/FallbackHandlerContractTests.cs`): real handlers return `Right` or `Left` and never throw.
4. DI test (PR-1) and telemetry test with `MeterListener` (PR-2). Property, load, benchmark and integration: not applicable (stateless strategy over Polly); justification files in PR-2 as in #136.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Phases 1 to 5 done.
TASK: Write the Phase 6 tests against real code; update manifest through mechanical-fixer.
KEY RULES: Shouldly via Encina.Testing.Shouldly; FakeTimeProvider; no Thread.Sleep; a nullable parameter accepting null gets a valid-result test.
REFERENCE FILES: tests/Encina.UnitTests/Polly/, tests/Encina.GuardTests/Infrastructure/Polly/
```

</details>

### Phase 7: Documentation and Finalization

<details>
<summary>Tasks</summary>

1. XML docs with examples; `src/Encina.Resilience/README.md` section and `docs/features/resilience-fallback.md` (`docs-writer`).
2. `changelog.d/137-fallback.added.md`, `PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.Resilience.json` (`mechanical-fixer`).
3. Verification: build 0 warnings, tests filtered to `Resilience`, EventId architecture tests, `--check-justifications`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Phases 1 to 6 green.
TASK: Documentation and verification of Phase 7.
KEY RULES: never edit CHANGELOG.md; docs by docs-writer; fragments, PublicAPI, manifest by mechanical-fixer; no hand-typed coverage figures.
REFERENCE FILES: changelog.d/README.md, .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Relevance |
|--------|-----------|
| Polly v8 fallback strategy (`FallbackStrategyOptions<T>`: `ShouldHandle`, `FallbackAction`, `OnFallback`) | The engine |
| Resilience4j fallback | Comparable product; fallback outside retry and breaker |
| ADR-055 R2, R3, R5, settled point on fail-safe | Form, front-ends, telemetry, caching boundary |
| ADR-062 | No new error code; the enriched error keeps the original category |
| ADR-054 D3, D4 | Names, tenant tag |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `ErrorCategory`, `EncinaErrors.Create` | `src/Encina/Errors/ErrorCategory.cs`, `src/Encina/Results/EncinaErrors.cs` | Trigger set and enriched error |
| `EncinaErrorMetadataKeys` | `src/Encina/Errors/EncinaErrorMetadataKeys.cs` | Detail keys style |
| `ICommand<>`, `IQuery<>` | `src/Encina/Abstractions/CqrsContracts.cs` | Query-only rule |
| `PipelineStage.Resilience`, `Authorization` | `src/Encina/Pipeline/Ordering/PipelineStage.cs` | Fallback runs after authorization |
| `IRequestContextAccessor` | `src/Encina/Abstractions/IRequestContextAccessor.cs` | Context for handlers (choice 6) |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | Tenant tag |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Resilience` (`ResilienceFallback`) | 906-911 | From the block 900-949 (resilience-patterns-1); 906-909 used, 910-911 spare; #136 holds 900-905 |

### Estimated File Count

| Category | PR-1 | PR-2 | PR-3 | Files |
|----------|------|------|------|-------|
| Production | 7 | 1 | 5 | PR-1: `FallbackStrategy`, `FallbackAttribute`, `FallbackOptions`, `IFallbackHandler`, `FallbackOptionsValidator`, `FallbackStrategyFactory`, `FallbackPropertyKeys`. PR-2: `FallbackBuilderExtensions`. PR-3: `FallbackLogMessages`, `FallbackTelemetry`, `FallbackStrategyFactory` (modified, `OnFallback` hook), `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt` |
| Project edits | 1 | 0 | 0 | PR-1: `Encina.Resilience.csproj` (`InternalsVisibleTo`, only if missing) |
| Tests | 4 | 2 | 2 | PR-1: factory, validator, options, guard. PR-2: registration (DI), contract. PR-3: telemetry and log messages, `EncinaEventIdAllocationTests.cs` |
| Metadata and docs | 2 | 3 | 6 | PR-1: PublicAPI, manifest. PR-2: PublicAPI, manifest, changelog. PR-3: PublicAPI, manifest, README, feature page, 2 justification files |
| **Total** | **14** | **6** | **13** | PR-1 about 1,000 lines, PR-2 about 300, PR-3 about 500; no test double or implementer change |

Per-file targets (new files; baseline: none exists on `origin/main`); each is reached by the tests of the PR that adds the file:

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `FallbackStrategy.cs` | 0 | 0 | 0 | 0 | 0 | An enum has no executable code |
| `FallbackAttribute.cs` | 95 | 0 | 0 | 0 | 0 | Init properties only, read by options tests |
| `FallbackOptions.cs` | 95 | 100 | 0 | 0 | 0 | Merge logic covered by value tests; the guard test sends a null attribute to `From` (PR-1) |
| `FallbackPropertyKeys.cs` | 90 | 0 | 0 | 0 | 0 | Static keys read by the factory tests (PR-1) |
| `IFallbackHandler.cs` | 0 | 0 | 0 | 0 | 0 | Interface; the contract tests run implementations |
| `FallbackOptionsValidator.cs` | 95 | 0 | 0 | 0 | 0 | One failing and one passing test per rule; internal, reached through the `InternalsVisibleTo` of Phase 1 task 6; no validated public parameters, so no guard flag |
| `FallbackStrategyFactory.cs` | 90 | 0 | 0 | 0 | 0 | Each trigger and source path is driven through the real compiler; internal |
| `FallbackBuilderExtensions.cs` | 90 | 100 | 0 | 0 | 0 | Two guarded parameters |
| `FallbackLogMessages.cs` | 80 | 0 | 0 | 0 | 0 | Generated methods reached through strategy tests with a capturing logger |
| `FallbackTelemetry.cs` | 90 | 0 | 0 | 0 | 0 | Listener-based test |

CRAP of every method at most 10: the trigger predicate is split into small methods of complexity at most 4.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #137: [Fallback] in Encina.Resilience (ADR-055). AGENTS.md rules apply. The foundation and R6 are merged first; if missing, stop and report.

IMPLEMENTATION OVERVIEW: (1) FallbackStrategy, FallbackAttribute, FallbackOptions, IFallbackHandler<,>, validator; (2) FallbackStrategyFactory over Polly AddFallback, outermost of the Resilience-stage pipeline; (3) builder extensions and DI test; (4) tenant tag, opt-in; (5) EventIds 906-911 and the counter; (6) unit, guard, contract tests; (7) docs and fragments.

KEY PATTERNS: queries only; failure categories trigger, widenable; handler resolved per call; a failing fallback returns the original error with details; no CachedValue member; telemetry follows ADR-054; specialists for PublicAPI, changelog, manifest, docs.

REFERENCE FILES: src/Encina.Polly/Attributes/RetryAttribute.cs, src/Encina/Results/EncinaErrors.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/architecture/adr/055-resilience-family.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | The cached-value strategy waits for #268 (Design Choice 5); issue file `resilience-fallback-cached-value.md` |
| 2 | OpenTelemetry | ✅ | Counter and span event (ADR-055 R5); tenant tag only with multi-tenancy |
| 3 | Structured Logging | ✅ | EventIds 906-909 |
| 4 | Health Checks | ❌ | No dependency of its own; the fallback source's health belongs to it |
| 5 | Validation | ✅ | Startup validation of attribute and options |
| 6 | Resilience | ✅ | A resilience strategy in the compiled pipeline |
| 7 | Distributed Locks | ❌ | No shared state |
| 8 | Transactions | ❌ | Commands are excluded (choice 4) |
| 9 | Idempotency | ❌ | Queries only; nothing to deduplicate |
| 10 | Multi-Tenancy | ✅ | Tenant tag only when multi-tenancy is on; no tenant-qualified key, so the `default` segment does not apply |
| 11 | Module Isolation | ❌ | Module-independent |
| 12 | Audit Trail | ❌ | Infrastructure concern; the degraded answer is visible in telemetry |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|--------------|-------|--------|
| Foundation (`Encina.Resilience`, compiler, options front-ends) | Not on main; issue file `resilience-foundation-encina-resilience.md` | Blocks implementation (Dilemma 1, answered once in the #136 plan) |
| R6 token and deadline | Not on main; issue file `resilience-r6-pipeline-token-and-deadline.md` | The fallback action receives the continuation token |
| #268 fail-safe cache | Open | The deferred cached-value strategy |

---

## Next Steps

1. Commit this plan, open its PR and link it from #137.
2. When #137 is picked up, re-check against today's code and ask the maintainer about the Dilemmas (choices 1 to 8, 10 and 12).
3. Brief a worker for PR-1 (14 files), then PR-2 and PR-3, after the foundation and R6 are merged.
