# Implementation Plan: Hedging — parallel redundant requests to cut tail latency in `Encina.Resilience`

> **Issue**: [#136](https://github.com/dlrivada/Encina/issues/136)
> **Type**: Feature
> **Complexity**: Medium (7 phases, no database providers, 2 pull requests)
> **Estimated Scope**: ~450 lines of production code + ~800 lines of tests (about 1,300 changed lines in total, two pull requests)

---

## Summary

Add `[Hedging]` to Encina: when the first attempt of a request is slow, or fails with a retryable error, a second attempt starts in parallel, the first acceptable result wins and the losing attempts are cancelled. The strategy is Polly v8's `AddHedging` (the maintainer decisions of [ADR-055](../architecture/adr/055-resilience-family.md) R2 and R3), configured by the `[Hedging]` attribute and by the options front-end, and compiled once per request type at startup into the single per-type pipeline. The comment of 2026-10-23 on #136 moves it into the 1.0 scope; its design follows ADR-055.

What exists today (read in `origin/main` at `f484cdf4f`; `src/Encina.Resilience` is not on main):

- Nothing of hedging: `git grep -i hedg -- src` returns no match. `Encina.Polly` has `[Retry]`, `[CircuitBreaker]`, `[RateLimit]` and `[Bulkhead]`; `Encina.Extensions.Resilience` has one standard pipeline. Both are removed by ADR-055 R1.
- The package that ADR-055 R1 names for every new resilience capability, `Encina.Resilience`, **does not exist in `src/`**, and no open issue creates it (the foundation, ADR-055 and the spike report section 8, steps 1 to 3). The pipeline continuation `RequestHandlerCallback<TResponse>()` in `src/Encina/Abstractions/RequestHandlerDelegate.cs` has no `CancellationToken`, so a hedged attempt cannot be cancelled (R6, planned, no issue yet).
- `RetryPipelineBehavior` (`src/Encina.Polly/Behaviors/RetryPipelineBehavior.cs`) already shows the rules a hedge must follow: it never retries a failure the `IErrorClassifier` marks `Permanent`, and it skips an ambiguous failure of a non-idempotent send unless the request carries an idempotency key or opts in (`OutboundRetryRules.AllowsAutomaticRetry`).

What this plan delivers, all in `Encina.Resilience` (ADR-055 R1): `HedgingAttribute`, `HedgingOptions` (with the validator that runs at startup), `IHedgingActionGenerator<TRequest, TResponse>` for custom hedged actions, the strategy factory that adds Polly's hedging strategy in the retry slot of the fixed order, a `winner` instrument and span events, six `[LoggerMessage]` definitions and the tests.

- **Affected packages**: `Encina.Resilience` (planned by the foundation), `Encina` core (`EventIdRanges.cs` only). The issue also ticks `Encina` core and `Encina.AspNetCore`; neither needs code (hedging is a pipeline strategy, and `Encina.AspNetCore` hosts only map the resulting error).
- **Provider category**: Resilience (AGENTS.md section 5, "Other categories"). No database, caching, transport, lock or validation matrix applies: hedging wraps whatever the handler calls, so it behaves the same on every provider.
- **Estimated size**: 2 pull requests of at most 15 files and 1,500 lines each, split in "Implementation Phases".
- **Prerequisites**: the foundation (package, pipeline compiler, options front-ends) and the R6 token change, both described in "Prerequisites & Dependencies" with the follow-up issue files that open them.

### Shared Pieces and Ownership (family resilience-patterns-1, then resilience-patterns-2 and resilience-otel-misc)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `Encina.Resilience` project, per-type pipeline compiler and registry (R2), options front-ends and precedence (R3), startup conflict collection | The foundation issue (issue file `resilience-foundation-encina-resilience.md`) | every plan of the family |
| `RequestHandlerCallback<TResponse>(CancellationToken)` and `IRequestContext` deadline (R6) | The R6 issue (issue file `resilience-r6-pipeline-token-and-deadline.md`) | this plan (loser cancellation), #141, #335 |
| `HedgingAttribute`, `HedgingOptions`, `IHedgingActionGenerator<,>`, `HedgedAction` | This plan (#136) | #145 chaos tests may hedge a chaos-injected attempt (sibling plan, by number only) |
| EventIds 900-905 (`ResilienceHedging`) | This plan | none |
| The retry-or-hedging exclusivity check at startup | This plan registers the rule; the compiler of the foundation runs it | #136 only |
| The `Meter("Encina.Resilience")` holder class | #728 (plan on main, its choice 1) | this plan reads it |

---

## Design Choices

<details>
<summary><strong>1. Where the code lives while `Encina.Resilience` does not exist (shared by #136, #137, #138, #139 and #141)</strong></summary>

ADR-055 R1 puts hedging in `Encina.Resilience`, a package that no issue creates yet, and R2 needs a compiler that does not exist either. The plan must say how it is built before them.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Wait for the foundation: the plan is written against the R2 compiler and the R3 options model, and implementation starts when the foundation is merged** | One implementation, in its final home; no code that R1 deletes; matches the order of the spike report (steps 1 to 3 before step 5) | Hedging is blocked on a foundation that is unplanned today; the plan names the contract it needs from it |
| **B) Build hedging now in `Encina.Polly` as a standalone behavior and move it with the consolidation** | Ships without the foundation | R2 rejected one behavior per strategy (2b); the work is thrown away or reworked when R1 removes the package; two retry-like layers could stack until then |
| **C) This plan's first pull request creates a minimal `Encina.Resilience` project and compiler stub** | Unblocks hedging | The first plan to land defines the foundation's shape for the other nine capabilities, without a design of its own |

### Chosen Option: **A — Wait for the foundation** (recommended, pending the maintainer)

### Rationale

- A wins when the 1.0 order of the spike report (ADR, foundation, rate-limiting core, then new strategies) is kept: it is the only option that adds no code to delete. B wins if hedging must ship before the foundation; C wins if the maintainer wants the foundation to grow out of the first capability.
- The same question is asked once for the five plans of this family and the plans of the siblings: the answer applies to all.
- The plan lists the contract it assumes from the foundation in "Prerequisites & Dependencies".

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Strategy form: a `HedgingPipelineBehavior` of its own, or a strategy of the per-type pipeline</strong></summary>

The issue asks for `HedgingPipelineBehavior<,>` and sketches a hand-written loop (start the primary, wait `Delay`, start the next). ADR-055 R2 fixes another shape.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Polly's hedging strategy, added by the compiler in the retry slot of the one per-type pipeline** | Polly already implements delay, parallel attempts, cancellation of the losers and the `OnHedging` telemetry event; the order is guaranteed; the attribute is read once | The class named in the issue is not created |
| **B) A `HedgingPipelineBehavior<,>` with its own loop** | Matches the issue text | R2 option 2b was rejected by the maintainer; a second pipeline per call; re-implements what Polly has |

### Chosen Option: **A — Polly's `AddHedging` inside the per-type pipeline**

### Rationale

- The acceptance criterion "`HedgingPipelineBehavior` that executes hedged requests" is met by the Resilience-stage behavior that runs the compiled pipeline (R2); no class of that name exists.
- The choice holds under every option of choice 1: where the code lives changes the package, never the strategy form (option B of choice 1 would still compile the strategy into a per-type pipeline, in the package that exists then).
- **Class:** Settled
- **Settled by:** ADR-055 R2 (option 2a chosen, 2b and 2c rejected) and the issue's own "Integration with Polly v8" section

</details>

<details>
<summary><strong>3. Hedging and retry on the same request type</strong></summary>

R2 writes "retry (or hedging instead)" in the fixed order, and ADR-055 settled points make several retry layers on one request a registration error. A type with `[Retry]` and `[Hedging]` (or the options equivalents) needs a rule.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Startup error naming the request type: a type has retry or hedging, never both** | Nothing is silently ignored; the behavior is the one the attributes say; coded failure at `ValidateOnBuild` | An application that wants both must choose |
| **B) Hedging wins; the retry attribute is ignored and a warning is logged** | Never fails the application start | An attribute does nothing, and a log line is easy to miss; the effective policy differs from the code next to the request |

### Chosen Option: **A — Startup error** (recommended, pending the maintainer)

### Rationale

- A wins when the policy visible on the request must be the policy that runs (the R3 argument for attributes). B wins for a library that ships a default `[Retry]` on shared request types that applications want to switch to hedging through configuration: options for a type override its attributes (R3), so the rule would have to say which one is dropped.
- The check runs in the foundation's conflict collection; this plan adds its row to it.

- **Class:** Dilemma

</details>

<details>
<summary><strong>4. What may be hedged: queries only, or commands with an idempotency guard</strong></summary>

A hedge executes the downstream pipeline and the handler more than once at the same time. Cancelling a loser does not undo what it already did. ADR-055 settled points say a command is retried only when it is idempotent.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[Hedging]` is valid on `IQuery<>` requests only; on an `ICommand<>` it is a startup error** | No duplicated side effect is possible; the idempotency behavior (`PipelineStage.Idempotency = 1400`, after `Resilience = 1100`) would answer a concurrent second attempt with `idempotency.in_progress`, which makes hedging a command with a key useless | A read-modify command with a safe retry rule cannot use hedging |
| **B) Commands allowed when the request carries an idempotency key or `HedgeNonIdempotent = true`, the same predicate as `RetryNonIdempotent`** | Same rule as retry; one concept | A hedged command with a key meets its own in-progress marker; `HedgeNonIdempotent = true` duplicates side effects by design |

### Chosen Option: **A — Queries only** (recommended, pending the maintainer)

### Rationale

- A wins in the reference application, where a hedged command could send a duplicate SMS or write a clinical record twice. B wins when the command is naturally idempotent (a keyed upsert in a replicated store) and the in-progress marker is not used.
- The issue's own cross-cutting note says hedged requests must be idempotent; A enforces it by type instead of by trust.

- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Which outcomes trigger a hedge: the classifier always, or the caller's predicate alone</strong></summary>

Besides the slow-first-attempt trigger (the delay), a failed attempt can start the next one immediately. The issue gives `options.ShouldHedge = (context, result) => result.IsLeft`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The `IErrorClassifier` decides (a `Permanent` error never hedges); `ShouldHedge` can only narrow it; `HedgeOnTimeout` adds `ErrorCategory.Timeout` results to the set** | Same rule as retry (ADR-029); a validation error is returned once, not N times | A caller cannot widen the set |
| **B) The caller's `ShouldHedge` replaces the classification** | Full control, as the issue sketches | A mistake hedges every `Left`, including permanent ones, multiplying load against a failing dependency |

### Chosen Option: **A — The classifier gates, the predicate narrows** (recommended, pending the maintainer)

### Rationale

- A wins in the common case and keeps retry and hedging coherent. B wins for a replicated read where even a not-found from one replica should be asked of another replica (replica lag).
- The delay trigger is independent of this choice: a hedge still starts after `DelayBetweenAttempts` while an attempt is pending, whatever it will return.

- **Class:** Dilemma

</details>

<details>
<summary><strong>6. Cancelling the losing attempts</strong></summary>

The acceptance criteria require "cancellation of pending requests when first succeeds".

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every attempt receives its own linked token from Polly's hedging; the continuation passes it down (R6), and a test proves the loser's token is cancelled** | Cancels the work the handler and its database and HTTP calls observe | Needs the R6 signature change first |
| **B) Ignore the loser's result and let it run to completion** | No dependency on R6 | The loser keeps running and sending, which R6 exists to prevent |

### Chosen Option: **A — Linked tokens through the continuation**

### Rationale

- Without the token no hedged attempt can be stopped; the plan therefore treats R6 as a prerequisite instead of faking cancellation.
- **Class:** Settled
- **Settled by:** ADR-055 R6 ("makes #335, #141 and #136 work as their issues describe"; a hedge cancels the losing attempt) and #136 acceptance criteria

</details>

<details>
<summary><strong>7. Custom hedged actions: an Encina interface or Polly's `ActionGenerator`</strong></summary>

The issue asks for `IHedgingActionGenerator` "for custom strategies" (for example send the second attempt to a replica).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IHedgingActionGenerator<TRequest, TResponse>`, resolved per call from the request's service provider and handed to the strategy through `ResilienceContext` properties; it returns a `HedgedAction` (use the primary callback, or run an alternative delegate)** | Typed to the request, testable with NSubstitute, matches the issue; no scoped service is captured by the compiled singleton pipeline | One more public interface |
| **B) Expose Polly's `ActionGenerator` delegate in `HedgingOptions`** | No new interface | Polly types in the public options; a delegate captured at startup cannot use scoped services |

### Chosen Option: **A — Encina interface, resolved per call** (recommended, pending the maintainer)

### Rationale

- A wins when the alternative needs scoped dependencies (a tenant-specific replica client). B wins for users who already write Polly delegates and want no Encina abstraction.
- The compiled pipeline is a singleton in Polly's registry (R2), so the generator can never be injected into it: the behavior resolves it for each call and places it in the context.

- **Class:** Dilemma

</details>

<details>
<summary><strong>8. Telemetry: what Encina measures</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Rely on Polly's `OnHedging` event and instruments for attempts; add one Encina counter for the winning attempt index and span events** | Follows R5 ("only for what Polly does not measure"); no double counting with `Encina.Http`'s hedging | One extra counter |
| **B) Encina counters for attempts and wins** | Self-contained | Duplicates `resilience.polly.*`, which ADR-054 D6.2 rules out |

### Chosen Option: **A — Winner counter and span events only**

### Rationale

- Polly reports that a hedge started but not which attempt won. The counter `encina.resilience.hedging.winner` (tags `request.type`, `hedging.attempt` = 0 for the primary, and the tenant only when multi-tenancy is on, ADR-054 D4) answers it with cardinality bounded by `MaxHedgedAttempts`.
- **Class:** Settled
- **Settled by:** ADR-055 R5 and ADR-054 D3, D4 and D6.2

</details>

---

## Implementation Phases

Two pull requests, each building and passing alone: **PR-1** (Phases 1 to 3 and their tests: model, strategy, registration) and **PR-2** (Phases 4 to 7: cross-cutting, observability, remaining tests, documentation). The file count of each is in "Estimated File Count".

### Phase 1: Core Models and Contracts

<details>
<summary>Tasks</summary>

1. `src/Encina.Resilience/Hedging/HedgingAttribute.cs`: `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)] public sealed class HedgingAttribute : Attribute` with `int MaxAttempts { get; init; } = 2` (total attempts including the primary, so `MaxHedgedAttempts = MaxAttempts - 1`), `int DelayMilliseconds { get; init; } = 1000` and `bool HedgeOnTimeout { get; init; } = true`. No constructor parameters: no guard.
2. `src/Encina.Resilience/Hedging/HedgingOptions.cs`: `public sealed class HedgingOptions` with `int MaxHedgedAttempts` (1 to 9, default 1), `TimeSpan DelayBetweenAttempts` (default 1 s; zero hedges all attempts at once, `Timeout.InfiniteTimeSpan` hedges only after a failure), `bool HedgeOnTimeout`, `Func<EncinaError, bool>? ShouldHedge` (narrows the classifier, choice 5). Includes `static HedgingOptions From(HedgingAttribute attribute)` for the R3 precedence merge (options for a type > attribute > defaults).
3. `src/Encina.Resilience/Hedging/IHedgingActionGenerator.cs`: `public interface IHedgingActionGenerator<TRequest, TResponse> where TRequest : IRequest<TResponse>` with `ValueTask<HedgedAction<TResponse>> GenerateAsync(HedgingActionContext<TRequest, TResponse> context, CancellationToken cancellationToken)`.
4. `src/Encina.Resilience/Hedging/HedgingActionContext.cs`: `public sealed record HedgingActionContext<TRequest, TResponse>(TRequest Request, IRequestContext RequestContext, int AttemptNumber)` and `public readonly record struct HedgedAction<TResponse>` with `static HedgedAction<TResponse> UsePrimary` and `static HedgedAction<TResponse> Run(Func<CancellationToken, ValueTask<Either<EncinaError, TResponse>>> action)`. The constructor of the context validates `Request` and `RequestContext` (`ArgumentNullException.ThrowIfNull`).
5. `src/Encina.Resilience/Hedging/HedgingOptionsValidator.cs`: `internal sealed class HedgingOptionsValidator` that returns the startup failures of choices 3 and 4 and the range checks (`MaxHedgedAttempts` in 1 to 9, `DelayBetweenAttempts` not negative unless infinite, a command type, a type that also has retry). It throws `InvalidOperationException` naming the request type and the rule, never the message of an error (registration errors, ADR-050).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #136 plan docs/plans/hedging-implementation-plan-136.md. Package src/Encina.Resilience exists (foundation merged, ADR-055 R1) with the per-type pipeline compiler and the options front-ends; the pipeline continuation takes a CancellationToken (R6).
TASK: Create the Phase 1 types of the plan in src/Encina.Resilience/Hedging/: HedgingAttribute, HedgingOptions, IHedgingActionGenerator<,>, HedgingActionContext<,> with HedgedAction<>, HedgingOptionsValidator. Apply the Chosen Option of every Design Choice of the plan.
KEY RULES: no [Obsolete]; XML docs with <example> on every public type; nullable enabled; no DateTime.UtcNow (TimeProvider only); the validator throws InvalidOperationException at startup, never returns an EncinaError; PublicAPI.Unshipped.txt lines through mechanical-fixer.
REFERENCE FILES: src/Encina.Polly/Attributes/RetryAttribute.cs, src/Encina.Polly/Behaviors/RetryPipelineBehavior.cs, src/Encina/Abstractions/CqrsContracts.cs
```

</details>

### Phase 2: Default Implementation — the Hedging Strategy

<details>
<summary>Tasks</summary>

1. `src/Encina.Resilience/Hedging/HedgingStrategyFactory.cs`: `internal static` factory that, from the merged `HedgingOptions` and the singleton `IErrorClassifier`, builds `HedgingStrategyOptions<Either<EncinaError, TResponse>>`: `MaxHedgedAttempts`, `Delay = DelayBetweenAttempts`, `ShouldHandle` (a thrown exception classified by `IErrorClassifier`, or a `Left` that is not `Permanent`, or, with `HedgeOnTimeout`, an `ErrorCategory.Timeout` result; then AND `ShouldHedge`), `ActionGenerator` (reads the `IHedgingActionGenerator` and the `IRequestContext` from `ResilienceContext.Properties` and returns the alternative delegate or the primary callback) and `OnHedging` (Phase 5 hook). The factory is the entry the foundation's compiler calls for the retry slot.
2. A `ResiliencePropertyKey` holder `HedgingPropertyKeys` (internal) for the per-call generator and request context, so no scoped service is ever captured by the compiled pipeline.
3. The all-attempts-fail result is the last completed attempt's `Left` (Polly's behavior, the issue's "return last error"); a test pins it.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 types exist in src/Encina.Resilience/Hedging/. The foundation compiler calls one factory per strategy and orders the strategies as: rate limiter, total timeout, retry (or hedging instead), circuit breaker, per-attempt timeout, chaos.
TASK: Implement HedgingStrategyFactory and HedgingPropertyKeys as in Phase 2 of the plan using Polly 8.6.6 AddHedging on ResiliencePipelineBuilder<Either<EncinaError, TResponse>>.
KEY RULES: classification through IErrorClassifier (ADR-029), a Permanent Left never hedges; never convert a Left into an exception; the per-call generator travels in ResilienceContext properties; time only through TimeProvider (the compiler sets it on the builder); cancellation of the losing attempt comes from the token the continuation receives.
REFERENCE FILES: src/Encina.Polly/Behaviors/RetryPipelineBehavior.cs, src/Encina/Recoverability/OutboundRetryRules.cs, the compiler of the foundation in src/Encina.Resilience
```

</details>

### Phase 3: Configuration and DI

<details>
<summary>Tasks</summary>

1. `src/Encina.Resilience/Hedging/HedgingBuilderExtensions.cs`: `public static ResilienceTypeBuilder<TRequest> Hedging<TRequest>(this ResilienceTypeBuilder<TRequest> builder, Action<HedgingOptions> configure)` for `AddEncinaResilience(o => o.For<T>().Hedging(...))` (the builder type name belongs to the foundation; adapt to it). It guards both arguments.
2. Registration: `HedgingOptionsValidator` joins the startup validation the foundation runs with `ValidateOnBuild`; `IHedgingActionGenerator<,>` implementations are registered by the application (`services.AddScoped<IHedgingActionGenerator<GetUserQuery, UserDto>, ReplicaGenerator>()`), never by the library.
3. The options bind from `IConfiguration` section `Encina:Resilience:Types:<RequestTypeName>:Hedging` through the R3 binding (documented; no new binding code in this plan).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Phases 1 and 2 are merged in the same pull request. The foundation exposes the For<T>() builder and a startup validation hook.
TASK: Add HedgingBuilderExtensions, wire the validator into the startup validation and write the registration test that builds the provider with ValidateOnBuild and ValidateScopes (AGENTS.md section 3, registration completeness).
KEY RULES: guards on every public parameter; precedence options for a type > attribute > defaults, covered by a test; no scoped service injected into a singleton; no AddEncina* method adds a service without its options.
REFERENCE FILES: src/Encina.Resilience (foundation builder), tests/Encina.UnitTests/Polly/ErrorClassifierRegistrationTests.cs
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Idempotency (function 9): the validator rejects `[Hedging]` on an `ICommand<>` (choice 4); an unprioritized request needs nothing else.
2. Multi-tenancy (function 10): hedging is tenant-independent; the winner counter and span events add the tenant tag only when `MultiTenancyMarker` is registered (ADR-054 D4); no key carries a tenant segment, so the reserved `default` segment does not apply.
3. A request without the Resilience-stage context (a direct pipeline run in a test) does not hedge and runs once (no context, no per-call generator).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: The hedging strategy and registration are in place.
TASK: Add the command rejection to HedgingOptionsValidator, the tenant tag rule to the telemetry calls and the no-context path.
KEY RULES: tenant tag only when MultiTenancyMarker is registered; never EncinaError.Message in logs or tags; fail toward a single execution, never toward duplicated side effects.
REFERENCE FILES: src/Encina/Abstractions/MultiTenancyMarker.cs, src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs`: add `/// <summary>Encina.Resilience — hedging strategy.</summary> public static readonly (int Min, int Max) ResilienceHedging = (900, 905);` and map `["Encina.Resilience"]` to include `nameof(EventIdRanges.ResilienceHedging)` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (`AssemblyRanges`).
2. `src/Encina.Resilience/Diagnostics/HedgingLogMessages.cs`: `[LoggerMessage]` definitions 900 `HedgeStarted` (Debug: request type, attempt), 901 `HedgeWinner` (Debug: request type, winning attempt), 902 `HedgingSkippedNoContext` (Debug), 903 `HedgedAttemptFailed` (Debug: request type, error code only, never the message), 904 `HedgingGeneratorFailed` (Warning: request type, exception through `ForLogging()`). 905 stays spare. XML docs name the range.
3. `HedgingTelemetry` (internal, uses the holder class of #728): `Counter<long>` `encina.resilience.hedging.winner`, an `Activity` event `hedging.winner` on the request span (`ActivitySource("Encina.Resilience")`), and the `OnHedging` callback that logs 900.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: AGENTS.md section 7 and ADR-021 govern EventIds; ADR-054 D3 and D4 govern names and tenant tags; ADR-055 R5 limits Encina instruments to what Polly does not measure.
TASK: Register the range 900-905 first, then add HedgingLogMessages, HedgingTelemetry and the callbacks of Phase 5. Run the architecture tests.
KEY RULES: ids packed from 900 without gaps (905 spare), [LoggerMessage] source generator, exception only through ForLogging(), no EncinaError.Message in any log, tag or activity; no activation switch.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, docs/plans/resilience-telemetry-implementation-plan-728.md
```

</details>

### Phase 6: Testing

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/Resilience/Hedging/`): `HedgingStrategyFactoryTests` with `FakeTimeProvider` and NSubstitute downstream: a slow primary starts a hedge after the delay and the faster hedge wins; a fast primary starts no hedge; a `Permanent` `Left` is returned once; a transient `Left` starts the next attempt at once; a timeout result hedges only with `HedgeOnTimeout`; `ShouldHedge = false` narrows; all attempts failing returns the last `Left`; the losing attempt's token is cancelled (assert `IsCancellationRequested`); the custom generator's alternative delegate runs for attempt 1 and `UsePrimary` re-runs the continuation. `HedgingOptionsValidatorTests`: command type, retry plus hedging, ranges. `HedgingOptionsTests`: attribute-to-options precedence.
2. Guard (`tests/Encina.GuardTests/Resilience/Hedging/HedgingGuardTests.cs`): `HedgingBuilderExtensions` null builder and null `configure`; `HedgingActionContext` null request and null request context; `HedgedAction.Run` null action; a nullable `ShouldHedge` accepts null (valid result, not an exception).
3. Contract (`tests/Encina.ContractTests/Resilience/HedgingActionGeneratorContractTests.cs`): `IHedgingActionGenerator<,>` implementations return `UsePrimary` or an action and never throw for a valid context; run against two real implementations (the test doubles of the unit tests).
4. DI (`tests/Encina.UnitTests/Resilience/Hedging/HedgingRegistrationTests.cs`): a provider with `ValidateOnBuild` and `ValidateScopes` resolves a hedged request through the real compiler.
5. Property: not applicable (Polly owns winner selection); justification in the PR description. Integration: not applicable beyond the DI test (no persistence or transport); the foundation owns the WireMock slice of `Encina.Http`. Load: `tests/Encina.LoadTests/Resilience/Hedging.md` justification (the strategy adds no shared mutable state; Polly owns concurrency). Benchmark: `tests/Encina.BenchmarkTests/Encina.Resilience.Benchmarks/Hedging.md` justification (the per-call allocation of the compiled pipeline is measured by the benchmark of #728, ADR-055 R5).
6. Per-file targets and CRAP are in "Research".

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Phases 1 to 5 are done. Tests live in the consolidated projects under tests/ (Encina.UnitTests, GuardTests, ContractTests); Shouldly through Encina.Testing.Shouldly; FakeTimeProvider from Microsoft.Extensions.Time.Testing.
TASK: Write the tests of Phase 6 against the real HedgingStrategyFactory and the real compiler; update the manifest targets through mechanical-fixer.
KEY RULES: tests execute real package code; no Thread.Sleep (advance FakeTimeProvider); one behavior per test; no test swallows an exception from FakeTimeProvider.Advance; guard tests include the nullable-accepts-null valid case.
REFERENCE FILES: tests/Encina.UnitTests/Polly/, tests/Encina.GuardTests/Infrastructure/Polly/, .github/coverage-manifest/Encina.Polly.json
```

</details>

### Phase 7: Documentation and Finalization

<details>
<summary>Tasks</summary>

1. XML docs with `<example>` on every public API (the attribute example from the issue, a generator example for a replica).
2. `src/Encina.Resilience/README.md` section "Hedging" and `docs/features/resilience-hedging.md` (via `docs-writer`): when to hedge, the query-only rule, the retry exclusivity, the generator example, the telemetry names. No hand-typed coverage figures.
3. `changelog.d/136-hedging.added.md` (via `mechanical-fixer`), `PublicAPI.Unshipped.txt` lines, `.github/coverage-manifest/Encina.Resilience.json` entries (via `mechanical-fixer`).
4. `ROADMAP.md`, `docs/INVENTORY.md` and an ADR need no change (ADR-055 holds the decisions).
5. Verification: `dotnet build --configuration Release` (0 warnings), `dotnet test` of the three test projects filtered to `Resilience`, the EventId architecture tests, `dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Phases 1 to 6 are green.
TASK: Finish the documentation of Phase 7 and run the verification commands.
KEY RULES: never edit CHANGELOG.md; fragment name <issue>-<slug>.<section>.md; README and docs pages by docs-writer; fragment, PublicAPI and manifest by mechanical-fixer; no hand-typed coverage figures (covref markers).
REFERENCE FILES: changelog.d/README.md, src/Encina.Resilience/README.md, .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Relevance |
|--------|-----------|
| Polly v8 hedging strategy (`HedgingStrategyOptions<T>`: `MaxHedgedAttempts` default 1, `Delay` default 2 s, `ActionGenerator`, `OnHedging`) | The engine; works on the generic builder with results, no exceptions |
| `Microsoft.Extensions.Http.Resilience` `AddStandardHedgingHandler` | Comparable product for HTTP; `Encina.Http` (#1233) uses it, so `resilience.polly.*` instruments are shared (ADR-055 R5) |
| Envoy retry hedging; "The Tail at Scale" (Dean and Barroso, 2013) | The pattern and the idempotency caveat |
| ADR-055 R2, R3, R5, R6 | Per-type compiled pipeline, front-ends, telemetry, token |
| ADR-029, ADR-050 | Classification by code; one retry layer |
| ADR-054 D3, D4, D6.2 | Names, tenant tag, no double instrumentation |
| ADR-062 | Error category: this plan adds no error code (a hedge returns the attempt's own `Left`, category unchanged) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `RetryPipelineBehavior`, `OutboundRetryRules` | `src/Encina.Polly/Behaviors/RetryPipelineBehavior.cs`, `src/Encina/Recoverability/OutboundRetryRules.cs` | The classification and idempotency rules a hedge copies; removed by R1, rules move to `Encina.Resilience` |
| `IErrorClassifier`, `DefaultErrorClassifier` | `src/Encina/Recoverability/` | Gate of choice 5 |
| `ICommand<>`, `IQuery<>` | `src/Encina/Abstractions/CqrsContracts.cs` | Query-only rule |
| `PipelineStage.Resilience` | `src/Encina/Pipeline/Ordering/PipelineStage.cs` | Stage of the behavior that runs the compiled pipeline |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | Tenant tag rule |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs`, `tests/Encina.UnitTests/Testing/Architecture/` | New range 900-905 |
| `QueryCachingPipelineBehavior` | `src/Encina.Caching/Behaviors/` | Pattern for the tenant-tag check |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Resilience` (`ResilienceHedging`) | 900-905 | From the block 900-949 of the batch (resilience-patterns-1). 900 `HedgeStarted`, 901 `HedgeWinner`, 902 `HedgingSkippedNoContext`, 903 `HedgedAttemptFailed`, 904 `HedgingGeneratorFailed`, 905 spare. The sibling plans #137 (906-911), #138 (912-923), #139 (924-935) and #141 (936-945) take the next sub-ranges of the same block; 946-949 stay free |

### Estimated File Count

| Category | PR-1 | PR-2 | Notes |
|----------|------|------|-------|
| Production | 7 | 3 | PR-1: attribute, options, generator interface, context and action, validator, factory, builder extensions. PR-2: `HedgingLogMessages.cs`, `HedgingTelemetry.cs`, `EventIdRanges.cs` |
| Tests | 5 | 3 | PR-1: factory, validator and options, registration, guard, contract. PR-2: telemetry, log messages and EventId map (`EncinaEventIdAllocationTests.cs` modified), the load and benchmark justification files (2 `.md`) counted below |
| Metadata and docs | 3 | 6 | PR-1: `PublicAPI.Unshipped.txt`, manifest, changelog fragment. PR-2: `PublicAPI.Unshipped.txt`, manifest, README, `docs/features/resilience-hedging.md`, 2 justification `.md` |
| **Total** | **15** | **12** | PR-1 about 1,150 lines, PR-2 about 450 lines; no `ProjectReference`, DI test double or interface-implementer change |

PR-1 sits exactly at the 15-file limit: if the foundation leaves a required project edit (a `ProjectReference` from a test project), move `HedgingActionGeneratorContractTests.cs` to PR-2.

Per-file coverage targets (new files; baseline: none of the files exists on `origin/main`, `git grep -i hedg -- src` is empty):

| File | Unit | Guard | Contract | Property | Integration | One-sentence justification |
|------|------|-------|----------|----------|-------------|----------------------------|
| `Hedging/HedgingAttribute.cs` | 95 | 0 | 0 | 0 | 0 | An attribute with init properties has no executable branch beyond the defaults, read by the options tests; no parameters to guard |
| `Hedging/HedgingOptions.cs` | 95 | 0 | 0 | 0 | 0 | The merge and range logic is fully covered by value tests; no constructor parameters |
| `Hedging/IHedgingActionGenerator.cs` | 0 | 0 | 0 | 0 | 0 | An interface has no executable code; the contract tests exercise implementations |
| `Hedging/HedgingActionContext.cs` | 95 | 100 | 0 | 0 | 0 | Two guarded constructor parameters and two factory methods, all reached by unit and guard tests |
| `Hedging/HedgingOptionsValidator.cs` | 95 | 0 | 0 | 0 | 0 | Each rule has one failing and one passing test; an internal type without `InternalsVisibleTo` for `Encina.GuardTests` cannot receive guard tests |
| `Hedging/HedgingStrategyFactory.cs` | 90 | 0 | 0 | 0 | 0 | Every predicate branch and the generator path are driven with a fake clock; internal, so no guard flag |
| `Hedging/HedgingBuilderExtensions.cs` | 90 | 100 | 0 | 0 | 0 | Public extension with two guarded parameters; the registration test reaches the body |
| `Diagnostics/HedgingLogMessages.cs` | 80 | 0 | 0 | 0 | 0 | Generated partial methods run through the strategy tests with a capturing logger |
| `Diagnostics/HedgingTelemetry.cs` | 90 | 0 | 0 | 0 | 0 | A `MeterListener` and `ActivityListener` assert the counter and event |

CRAP of every added method stays at or below 10: the factory's predicate is split into three private methods of complexity at most 4, fully covered.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #136: [Hedging] in Encina.Resilience (ADR-055 R1 to R3, R5, R6). AGENTS.md rules apply (ROP, no exceptions for business logic, TimeProvider, zero warnings, per-flag coverage, CRAP <= 10). The foundation (Encina.Resilience project, per-type pipeline compiler, options front-ends) and the R6 continuation token are merged first; if either is missing, stop and report.

IMPLEMENTATION OVERVIEW: (1) HedgingAttribute, HedgingOptions, IHedgingActionGenerator<,>, HedgingActionContext<,> and HedgedAction<>, HedgingOptionsValidator; (2) HedgingStrategyFactory over Polly AddHedging, in the retry slot of the fixed order; (3) HedgingBuilderExtensions and startup validation; (4) query-only rule, tenant tag rule; (5) EventIds 900-905, winner counter, span events; (6) unit, guard, contract and DI tests; (7) README, docs page, changelog fragment, PublicAPI, manifest.

KEY PATTERNS: a Permanent Left never hedges (IErrorClassifier); queries only; retry and hedging never on one type (startup InvalidOperationException naming the type); the per-call generator travels in ResilienceContext properties, never captured by the singleton pipeline; losing attempts are cancelled through the continuation token; telemetry names follow ADR-054 D3, tenant tag only with multi-tenancy; specialists for PublicAPI, changelog, manifest and docs.

REFERENCE FILES: src/Encina.Polly/Behaviors/RetryPipelineBehavior.cs, src/Encina.Polly/Attributes/RetryAttribute.cs, src/Encina/Recoverability/OutboundRetryRules.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/architecture/adr/055-resilience-family.md, docs/plans/resilience-telemetry-implementation-plan-728.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Hedging operates on live calls; a cached read is served before the Resilience stage (`PipelineStage.Caching = 1000`) and never reaches the strategy |
| 2 | OpenTelemetry | ✅ | `encina.resilience.hedging.winner` counter and span events on `ActivitySource("Encina.Resilience")` (ADR-055 R5); tenant tag only with multi-tenancy (ADR-054 D4); Polly's `OnHedging` instruments are not duplicated |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` 900-904 in `ResilienceHedging`; error code only, exceptions through `ForLogging()` |
| 4 | Health Checks | ❌ | Hedging has no dependency of its own to check; the health of the hedged endpoints belongs to their providers |
| 5 | Validation | ✅ | Options and attribute validation at startup (`HedgingOptionsValidator`); no request input is validated |
| 6 | Resilience | ✅ | This is a resilience strategy, ordered with retry, breaker and timeouts by the compiler (R2) |
| 7 | Distributed Locks | ❌ | No shared state: each call owns its attempts |
| 8 | Transactions | ❌ | Commands are rejected (choice 4), so no transaction is ever opened twice by a hedge |
| 9 | Idempotency | ✅ | Hedging is limited to queries by choice 4; the idempotency behavior (stage 1400) sits after the Resilience stage and is never hedged |
| 10 | Multi-Tenancy | ✅ | Tenant-independent behavior; the tenant tag appears on telemetry only when multi-tenancy is on; no tenant-qualified key exists, so the reserved `default` segment does not apply. Tenant-scoped hedging options: not planned (a `For<T>()` option applies to every tenant) |
| 11 | Module Isolation | ❌ | Module-independent |
| 12 | Audit Trail | ❌ | Infrastructure concern; no business action is performed by the strategy |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect on this plan |
|--------------|-------|---------------------|
| The foundation: `Encina.Resilience` project, per-type pipeline compiler and registry (R2), options front-ends (R3), startup conflict collection | No issue exists: opened from the issue file `resilience-foundation-encina-resilience.md` (to be opened by the orchestrator) | Blocks implementation (Design Choice 1). The plan assumes: a builder for `For<T>()`, a factory hook per strategy slot, a startup validation hook, `ResilienceContext` property keys for the request context |
| R6: `RequestHandlerCallback<TResponse>(CancellationToken)` and the context deadline | No issue exists: issue file `resilience-r6-pipeline-token-and-deadline.md` | Blocks loser cancellation (Design Choice 6). Counted today: 95 files under `src/` and 121 under `tests/` mention `RequestHandlerCallback<` |
| #728 plan (telemetry holder class, choice 1) | Plan on main, choices pending | The `Meter` and `ActivitySource` holder this plan reads |
| #2414 core and #1233 `Encina.Http` | Open | Not required: hedging of outbound HTTP is `Encina.Http`'s `AddStandardHedgingHandler` (a different operation, documented, not detected, ADR-055 R4) |

---

## Next Steps

1. Commit this plan, open its PR and link it from #136.
2. When #136 is picked up, re-check the choices against today's code (whether the foundation and R6 landed) and ask the maintainer about the Dilemmas (choices 1, 3, 4, 5 and 7).
3. Brief a worker for PR-1 (Phases 1 to 3 with their tests, 15 files), then PR-2, after the foundation and R6 are merged.
