# Implementation Plan: Request timeouts (`[Timeout]`, `IHasTimeout`) in `Encina.Resilience`

> **Issue**: [#335](https://github.com/dlrivada/Encina/issues/335)
> **Type**: Feature
> **Complexity**: Medium (7 phases, no database providers, 3 pull requests)
> **Estimated Scope**: ~450 lines of production code + ~900 lines of tests (about 1,500 changed lines in total, three pull requests)

---

## Summary

Give every request type a configurable timeout that stops the work it abandons and answers a coded `Left`: a `[Timeout]` attribute, an `IHasTimeout` interface for a per-instance value, and the options front-end, all compiled once per request type into the single resilience pipeline of [ADR-055](../architecture/adr/055-resilience-family.md) R2 and R3. The comment of 2026-10-23 on #141 and the table of ADR-055 move #335 into the design of the family: its work is in `Encina.Resilience`, and it depends on the R6 continuation token and context deadline, because without them a timed-out handler keeps running and sending.

What exists today (read in `origin/main` at `098caa920`):

- No timeout behavior: `git grep -i "TimeoutAttribute\|IHasTimeout"` over `src`, `tests` and `docs/plans` returns no match. The code `EncinaErrorCodes.Timeout = "encina.timeout"` exists (`src/Encina/Errors/EncinaErrorCodes.cs:63`); `ErrorCategory.Timeout = 11` exists (`src/Encina/Errors/ErrorCategory.cs`) and every host already maps that category to 504 (ADR-062 table; `src/Encina.AwsLambda/README.md:195`). `IErrorClassifier` already treats `encina.timeout` as transient (`src/Encina/Recoverability/IErrorClassifier.cs:174`).
- The continuation `RequestHandlerCallback<TResponse>()` (`src/Encina/Abstractions/RequestHandlerDelegate.cs`) takes no `CancellationToken` and `IRequestContext` (`src/Encina/Abstractions/IRequestContext.cs`) has no deadline: R6 adds both and has no issue yet (the sibling plan #141 and #136 name the same gap).
- The package ADR-055 R1 names, `Encina.Resilience`, does **not exist in `src/`**, and no open issue creates it. The two packages it replaces, `Encina.Polly` (attributes, `RetryPipelineBehavior`, ...) and `Encina.Extensions.Resilience`, contain no timeout.
- The ambient context exists: `IRequestContextAccessor` (`src/Encina/Abstractions/IRequestContextAccessor.cs`), set by entry points and restored by the dispatcher.
- Telemetry names are the pending choices 1 and 8 of the #728 plan (on main).

What this plan delivers, all in `Encina.Resilience`: `TimeoutAttribute`, `IHasTimeout`, `TimeoutOptions` and its validator, the strategy factory that adds Polly's total timeout (outermost slot) and per-attempt timeout (innermost slot but chaos) of the fixed order, the mapper that turns Polly's `TimeoutRejectedException` into a coded `Left`, the deadline-shortening call contributor, five `[LoggerMessage]` definitions and the tests. The issue's `TimeoutPipelineBehavior<,>`, `TimeoutFallback` enum and `config.UseTimeouts` call are replaced where they contradict ADR-055 (choices 2 and 5).

- **Affected packages**: `Encina.Resilience` (planned), `Encina` core (`EventIdRanges.cs` only). The issue also ticks `Encina.AspNetCore` and `Encina.Caching.*`: neither needs code (hosts already map category `Timeout` to 504; the cached-value fallback is choice 5).
- **Provider category**: Resilience (AGENTS.md section 5, "Other categories"); no database, caching, transport, lock or validation matrix applies.
- **Estimated size**: 3 pull requests of at most 15 files and 1,500 lines, split in "Implementation Phases".
- **Prerequisites**: the foundation (package, compiler, options front-ends) and R6 (token and deadline), described in "Prerequisites & Dependencies".

### Shared Pieces and Ownership (family resilience-patterns-1 and resilience-patterns-2)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `Encina.Resilience` project, per-type compiler and registry (R2), options front-ends and precedence (R3), strategy-factory slots and the per-call contributor hook | The foundation issue (issue file `resilience-foundation-encina-resilience.md` of the family resilience-patterns-1) | every plan of both families |
| `RequestHandlerCallback<TResponse>(CancellationToken)` and `IRequestContext.Deadline` (R6) | The R6 issue (issue file `resilience-r6-pipeline-token-and-deadline.md`) | this plan, #136, #141 |
| `TimeoutAttribute`, `IHasTimeout`, `TimeoutOptions`, the total and per-attempt strategies, the rejection-to-`Left` mapper, the deadline-shortening contributor | **This plan (#335)** | #141 (enforcement, its choice 2) |
| `TimeoutBudget`, the budget behavior, deadline header propagation | #141 (sibling plan, by number) | reads the deadline this plan shortens |
| Fallback on a timed-out request (`[Fallback]`) | #137 (sibling plan, by number) | choice 5 of this plan |
| The `Meter("Encina.Resilience")` holder and the instrument-name convention | #728 (plan on main; its choices 1 and 8 pending) | choice 8 of this plan |
| EventIds 950-959 (`ResilienceTimeout`), 950-954 used | This plan | none |

---

## Design Choices

<details>
<summary><strong>1. Where the code lives while `Encina.Resilience` and R6 do not exist (shared with #136, #137, #138, #139 and #141)</strong></summary>

ADR-055 R1 puts timeouts in `Encina.Resilience` and R6 lets a timeout cancel the handler. Neither exists in `src/`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Wait for the foundation and R6; this plan is written against the R2 compiler, the R3 options model and the R6 token** | One implementation in its final home; nothing that R1 deletes; the order of the spike report | Blocked on two unplanned pieces |
| **B) Build a standalone `TimeoutPipelineBehavior<,>` in `Encina.Polly` now** | Ships without the foundation | Without the R6 token the behavior can only return a `Left` on time while the handler keeps running (the defect R6 exists to remove); thrown away when R1 removes the package; stacks a second pipeline per call (R2 option 2b was rejected) |
| **C) The first pull request creates a minimal `Encina.Resilience` project and compiler stub** | Unblocks the plan | The first plan to land defines the foundation's shape for every capability |

### Chosen Option: **A — Wait for the foundation and R6** (recommended, pending the maintainer)

### Rationale

- A wins when the 1.0 order of the spike report is kept; B wins if timeouts must ship before the foundation and an abandoned handler is acceptable until R6; C wins if the foundation should grow out of the first capability. The same question is asked once for the whole resilience family (the shared Dilemma 1 of #136).
- Choices 2 to 9 are written for A. Under B or C the package changes and choice 2 is re-planned; no other choice depends on where the code lives.

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Form and registration: a strategy of the per-type pipeline, or the `TimeoutPipelineBehavior<,>` of the issue</strong></summary>

The issue sketches a behavior that links a `CancellationTokenSource`, catches the cancellation and returns `HandleTimeout`, registered with `config.UseTimeouts(...)`. ADR-055 R2 and R3 fix another shape.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Polly's timeout strategies added by the foundation's compiler in the fixed slots (total timeout first after the rate limiter, per-attempt timeout after the breaker); configured by `[Timeout]` and by `AddEncinaResilience(o => o.For<T>(...))`** | The order is guaranteed; attributes are read once; precedence options over attributes over defaults is tested once for every strategy; one registration entry | The class and the `UseTimeouts` call named in the issue are not created |
| **B) A `TimeoutPipelineBehavior<,>` with its own linked token and `config.UseTimeouts`** | Matches the issue text | R2 option 2b was rejected by the maintainer; a second pipeline per call; a second options model |

### Chosen Option: **A — Strategies of the per-type pipeline** (recommended, pending the maintainer)

### Rationale

- A is what ADR-055 R2 (2a chosen, 2b rejected) and R3 (one model for attributes and options) decided; the plan keeps it a Dilemma only because it does not hold under choice 1 option B, where there is no compiler. The acceptance criterion "`TimeoutPipelineBehavior<,>` implemented" is met by the Resilience-stage behavior that runs the compiled pipeline, with the timeout strategy inside it; no class of that name exists.
- Default: no timeout unless an attribute, `IHasTimeout` or an options entry sets one (AGENTS.md section 1, opt-in); `Default(...)` of the options model may set a global timeout, and an invalid value fails at startup (`ValidateOnStart`).

- **Class:** Dilemma

</details>

<details>
<summary><strong>3. One timeout or two: total and per-attempt</strong></summary>

R2 fixes the order "rate limiter, total timeout, retry (or hedging), circuit breaker, per-attempt timeout, chaos". The issue asks for one timeout per request. When a request has retry, one hanging attempt of a single total timeout consumes the whole budget and leaves nothing for the retries.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[Timeout(Seconds = n)]` is the total timeout; an optional `AttemptSeconds` adds the per-attempt timeout (null: none)** | Fills both slots of the fixed order; a request with retry stays bounded; nothing changes for a request without retry | Two properties to document and validate (`AttemptSeconds` must be below `Seconds`) |
| **B) Only the total timeout now** | Smaller | The per-attempt slot of R2 has no owner (no open issue plans it), and a retried request cannot bound one attempt |

### Chosen Option: **A — Total and optional per-attempt** (recommended, pending the maintainer)

### Rationale

- A wins when requests have `[Retry]` or `[Hedging]`; B wins if the maintainer wants the per-attempt slot as its own later issue.
- Both timeouts end in the same mapper (choice 6), so the error is the same; the span event carries the kind (`total` or `attempt`).

- **Class:** Dilemma

</details>

<details>
<summary><strong>4. `IHasTimeout`: a per-instance timeout in a pipeline compiled once</strong></summary>

R2 compiles a pipeline per request type at startup and forbids rebuilding it per call; `IHasTimeout.Timeout` is a property of the instance.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The compiled timeout strategy takes its value from a per-call generator (Polly's `TimeoutGenerator`) that reads the request from the `ResilienceContext`; the attribute or options value is the fallback when the instance returns no value** | Meets the acceptance criterion; no per-call pipeline; the same strategy serves both sources | The generator runs once per call and per attempt; a request type with the interface always has the strategy |
| **B) Drop `IHasTimeout`; the attribute and the options cover the need** | Smaller surface | An acceptance criterion of the issue is left out; a timeout that depends on the payload (a report size) cannot be expressed |

### Chosen Option: **A — Generator reads the instance** (recommended, pending the maintainer)

### Rationale

- A wins when a timeout depends on the request data; B wins if every timeout in the application is per type. Precedence for a type with several sources: options entry for the type, then `IHasTimeout`, then the attribute, then the global default; a test pins it (R3's precedence extended by the instance source, which exists only for this interface).
- The interface lives in `Encina.Resilience` with the attribute, as `[Retry]` does in `Encina.Polly` today.

- **Class:** Dilemma

</details>

<details>
<summary><strong>5. The issue's `TimeoutFallback` (return default, return cached, execute fallback)</strong></summary>

The issue lists four fallbacks. #137 plans a `[Fallback]` strategy with handlers and a failure-category trigger set that includes `Timeout`; a cached-value fallback belongs to the fail-safe cache pattern of #268.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No fallback enum: a timeout is a `Left` with code `encina.timeout`; a request that wants a default or a handler adds `[Fallback]` (#137); the cached value is #268's pattern** | One fallback implementation; no cache dependency in the timeout code | The acceptance criterion "multiple fallback strategies" is met by composition, not by an enum; depends on #137 for anything beyond the `Left` |
| **B) Keep `TimeoutFallback` with `ThrowException` (the `Left`), `ReturnDefault` and `ExecuteFallback` in this plan** | The acceptance criterion is met literally | A second fallback mechanism next to #137; `default(TResponse)` for a command is a silent success |
| **C) Keep all four, including `ReturnCached` through `ICacheProvider`** | Literal | Pulls the caching category (8 providers) into a timeout feature |

### Chosen Option: **A — Left only; fallback by composition** (recommended, pending the maintainer)

### Rationale

- A wins on one owner per behavior; B wins if #137 is not built before timeouts ship; C wins only if the maintainer wants a cached answer on every timeout without the #268 pattern.
- Under A, `ThrowException` of the issue is the default and only behavior, and it is a `Left`, never an exception (AGENTS.md section 3).

- **Class:** Dilemma

</details>

<details>
<summary><strong>6. How a timeout becomes a `Left`, and why a caller cancellation never does</strong></summary>

Polly's timeout strategy signals expiry with `TimeoutRejectedException` inside the pipeline. ADR-055 settled points say a rejection is a coded `Left` and a `Left` is never converted to an exception to drive a strategy; ADR-062 rule R5 says a cancellation is `Cancelled`, never counted as a server fault.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A mapper at the boundary of the pipeline run: `TimeoutRejectedException` becomes `Left` `EncinaErrors.Create(EncinaErrorCodes.Timeout, category: ErrorCategory.Timeout, "The request timed out.", details: kind and configured milliseconds)`; an `OperationCanceledException` whose token is the caller's stays a cancellation (`encina.request.cancelled`, `Cancelled`)** | Coded `Left`; no message of an exception reaches a result (constant message); the caller's cancellation keeps its own result | One mapper to maintain |
| **B) Let `TimeoutRejectedException` escape to the dispatcher** | No mapper | Violates ROP and the settled point that every rejection is a coded `Left`; hosts would answer 500 |

### Chosen Option: **A — Boundary mapper**

### Rationale

- B does not comply, so A is the only option; it holds under every option of choices 1 to 5 (the mapper is the same code wherever the pipeline lives, and the total and per-attempt timeouts share it). The per-attempt `Left` category is `Timeout`, which `IErrorClassifier` treats as transient, so the retry layer may retry an attempt that timed out (and only an idempotent command, ADR-029).
- The error carries `EncinaErrorCodes.Timeout`, an existing code: no new code and no new ADR-062 row; the detail keys are `encina.timeout.kind` (`total` or `attempt`) and `encina.timeout.limit_ms`, constants in `TimeoutDetailKeys` in the style of `EncinaErrorMetadataKeys`. A timeout never carries `Retry-After`.
- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (ROP, no exception for business flow); ADR-055 settled points ("every rejection is a coded `Left`", timeout `encina.timeout`); ADR-062 (category `Timeout`; rule R5 for cancellation)

</details>

<details>
<summary><strong>7. Making the shortened deadline visible to the handler and to outbound calls</strong></summary>

R6 says the timeout behavior shortens the context deadline and outbound clients propagate it. The continuation carries only the token, and `IRequestContext.WithDeadline` (R6, planned) returns a new context, so a shortened deadline cannot travel through `next`. #141 asks the same question (its choice 11).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A call contributor `TimeoutDeadlineContributor` (owner file `Timeouts/TimeoutDeadlineContributor.cs`) sets `accessor.RequestContext = context.WithDeadline(min(existing, now + timeout))` for the call and restores the previous context in `finally`; readers other than behaviors read `IRequestContextAccessor.RequestContext`** | Works with R6 as decided; nothing leaks past the call; the earliest deadline wins | A behavior that reads its own `context` parameter does not see the shortened deadline |
| **B) The R6 continuation also carries the context** | Every behavior sees the deadline | Extends the R6 decision (token only) and its mechanical pass; belongs to the R6 issue |

### Chosen Option: **A — Accessor set and restore** (recommended, pending the maintainer)

### Rationale

- A wins with R6 as decided; B wins if every behavior must read the deadline from its parameter. The answer is the same as #141 choice 11 and is asked once.
- The contributor sets nothing when the effective timeout is not shorter than the existing deadline (the earliest wins) and never extends one. The time source is `TimeProvider`.

- **Class:** Dilemma

</details>

<details>
<summary><strong>8. Telemetry of a timeout</strong></summary>

R5: Polly's `resilience.polly.*` instruments are enriched by Encina; Encina creates instruments only for what Polly does not measure; the request span gets events from `ActivitySource("Encina.Resilience")`. No activation switch (#728, #2277).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No Encina instrument: Polly's strategy-event counter already counts `OnTimeout`; this plan adds the `timeout` span event (kind and limit) through the single event path of #728; names are the ones #728 settles** | No double counting (ADR-054 D6.2); no name invented here | The event name and tags wait for #728's choices |
| **B) Encina-owned `encina.resilience.timeout.*` counter and histogram** | Self-contained | Duplicates what Polly measures, against R5 |

### Chosen Option: **A — Polly's instruments plus the span event** (recommended, pending the maintainer)

### Rationale

- A wins on R5; B wins only if the maintainer wants a timeout histogram by request type without relying on Polly's dimensions. The tenant tag is added only when multi-tenancy is on (ADR-054 D4).
- Logs are in PR-2 (choice of EventIds 950-954); they carry the request type, the kind and the configured limit, never an `EncinaError.Message`.

- **Class:** Dilemma

</details>

<details>
<summary><strong>9. A handler that ignores its token</strong></summary>

Polly's timeout is optimistic: it cancels the token and waits for the callback to finish. A handler that does not observe the token overruns the limit.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Optimistic only: the token is cancelled at the limit and the call returns when the handler returns; the documentation says handlers must observe the token (AGENTS.md section 3 already requires it for database calls)** | No abandoned work; no leak; one behavior | A handler that ignores the token delays the answer past the limit |
| **B) Also race the continuation against the timer and answer the `Left` on time while the abandoned work continues** | The caller gets the answer on time | Abandoned work keeps sending and writing, the very defect R6 removes; races on a scoped `DbContext` still in use |

### Chosen Option: **A — Optimistic only** (recommended, pending the maintainer)

### Rationale

- A wins when correctness of side effects matters more than the caller's latency; B wins for read-only handlers behind an SLA. A test with a handler that ignores the token documents the behavior.

- **Class:** Dilemma

</details>

---

## Implementation Phases

Three pull requests, in order: **PR-1** Phases 1 and 2 with the registration of the factory and the mapper and their tests (the strategies work with the token of R6; the deadline is not shortened yet); **PR-2** the contributor of Phase 3 and Phase 5 (deadline shortening, logs and span event; it modifies the factory, the mapper, the registration and their tests); **PR-3** the contract test, the justification files and Phase 7 (documentation and changelog). Every PR needs the foundation and R6 merged (choice 1 option A); under B or C the split is recounted.

### Phase 1: Core Models

<details>
<summary>Tasks</summary>

1. `src/Encina.Resilience/Timeouts/TimeoutAttribute.cs`: `[AttributeUsage(AttributeTargets.Class)] public sealed class TimeoutAttribute : Attribute` with `double Seconds` and `double? AttemptSeconds` (no constructor parameters; validation at startup by the options validator, never a setter exception, so the attribute has no executable branches).
2. `IHasTimeout.cs`: `public interface IHasTimeout { TimeSpan? Timeout { get; } }`.
3. `TimeoutOptions.cs`: `TimeSpan? Total`, `TimeSpan? Attempt`; the per-type options type of the foundation (R3) gains this section; `TimeoutOptionsValidator.cs` (internal): positive values, `Attempt` below `Total`.
4. `TimeoutDetailKeys.cs`: `encina.timeout.kind`, `encina.timeout.limit_ms`, and the two kind values.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina issue #335, plan docs/plans/request-timeout-implementation-plan-335.md. Encina.Resilience exists with the compiler and options model; R6 landed (token on the continuation, IRequestContext.Deadline and WithDeadline).
TASK: Create TimeoutAttribute, IHasTimeout, TimeoutOptions, its validator and TimeoutDetailKeys.
KEY RULES: no exception for business flow; no [Obsolete]; XML docs with examples on every public member; the attribute has no constructor parameters; invalid options fail at startup.
REFERENCE FILES: src/Encina.Polly/Attributes/RetryAttribute.cs, src/Encina/Errors/EncinaErrorMetadataKeys.cs, docs/architecture/adr/055-resilience-family.md
```

</details>

### Phase 2: Default Implementation — Strategies and Mapper

<details>
<summary>Tasks</summary>

1. `Timeouts/TimeoutStrategyFactory.cs` (internal): implements the foundation's strategy-factory contract; for a request type with a resolved timeout it adds `AddTimeout(new TimeoutStrategyOptions { TimeoutGenerator = ... })` in the total slot and, when `Attempt` is set, in the per-attempt slot; the generator reads the request from the `ResilienceContext`, and when it implements `IHasTimeout` returns its value (choice 4), else the compiled value. Precedence: options for the type, `IHasTimeout`, attribute, default.
2. `Timeouts/TimeoutRejectionMapper.cs` (internal): `Either<EncinaError, T> Map<T>(Exception ex, CancellationToken callerToken)`: `TimeoutRejectedException` to the coded `Left` of choice 6; `OperationCanceledException` with `callerToken.IsCancellationRequested` is left to the caller (cancellation propagates, never a `Left` of this mapper). The foundation's behavior calls the mapper through its rejection-mapper hook (assumed contract).
3. Both internal; `InternalsVisibleTo` for `Encina.UnitTests` and `Encina.GuardTests` in `Encina.Resilience.csproj` (a foundation acceptance item; added in PR-1 only if missing, counted as a file there).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 done.
TASK: Implement the timeout strategy factory and the rejection mapper.
KEY RULES: time only through TimeProvider (pass it to the strategy options); a caller's cancellation is never reported as a timeout; the Left uses EncinaErrors.Create with the named category: ErrorCategory.Timeout and a constant message (never an exception message); no Retry-After on a timeout.
REFERENCE FILES: src/Encina/Errors/EncinaErrorCodes.cs, src/Encina/Results/EncinaErrors.cs, src/Encina/Recoverability/IErrorClassifier.cs
```

</details>

### Phase 3: Configuration, DI and the Deadline Contributor

<details>
<summary>Tasks</summary>

1. `Timeouts/TimeoutDeadlineContributor.cs` (internal sealed; constructor `(IRequestContextAccessor accessor, TimeProvider timeProvider)`, both injected, so no public constructor takes an internal type): the per-call contributor of choice 7; sets `WithDeadline(min(existing, now + total))` and restores the previous context in `finally`; nothing when the timeout is not shorter.
2. `ResilienceServiceCollectionExtensions.cs` (the foundation's file, modified): `TryAddEnumerable` of the factory, the mapper and the contributor, so `AddEncinaResilience` is the only registration entry (choice 2). The DI test of the foundation (`ValidateOnBuild`, `ValidateScopes`) gains the timeout services.
3. `PublicAPI.Unshipped.txt` of `Encina.Resilience`: the attribute, the interface, the options section, the detail keys (`mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Phases 1 and 2 done.
TASK: Add the deadline contributor and register the timeout services in AddEncinaResilience.
KEY RULES: the contributor restores the previous context in finally and never extends a deadline; singleton services take no scoped dependency (resolve per call through the accessor); registration is proven with ValidateOnBuild and ValidateScopes.
REFERENCE FILES: src/Encina/Abstractions/IRequestContextAccessor.cs, src/Encina/Core/RequestContextAccessor.cs
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Tenant tag on the span event only when multi-tenancy is on (ADR-054 D4); no tenant-qualified key exists, so the reserved `default` segment does not apply.
2. Hosts need no change: category `Timeout` is already mapped to 504 by `Encina.AspNetCore`, `Encina.AzureFunctions` and `Encina.AwsLambda`; a test of PR-1 asserts the category of the produced error, and no host file is touched.
3. Deferred item of the issue (tenant-scoped timeouts) is the issue file `artifacts/issues/tenant-scoped-timeouts.md`, written with this plan.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Phases 1 to 3 done.
TASK: Verify the cross-cutting rows of the plan (tenant tag, host mapping) and add only what they need.
KEY RULES: no new host code; no tenant tag unless multi-tenancy is on.
REFERENCE FILES: docs/architecture/adr/062-error-category-contract.md, docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs`: register `ResilienceTimeout = (950, 959)`; the core `PublicAPI.Unshipped.txt` line; the `AssemblyRanges` entry for `Encina.Resilience` in `EncinaEventIdAllocationTests.cs` (the entry exists after the foundation; this PR adds the new range name to it).
2. `Timeouts/TimeoutLogMessages.cs`: `[LoggerMessage]` 950 `TimeoutElapsed` (Warning; request type, kind, limit ms), 951 `TimeoutDeadlineShortened` (Debug), 952 `TimeoutIgnoredBecauseCallerCancelled` (Debug), 953 `TimeoutOptionsInvalid` (Error, at startup), 954 `TimeoutInstanceValueInvalid` (Warning: `IHasTimeout` returned a non-positive value, the compiled value is used); 955 to 959 spare. No error message, no request data.
3. `TimeoutStrategyFactory.cs` and `TimeoutRejectionMapper.cs` (modified): emit 950 to 954; the `timeout` span event through the #728 event path.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Phases 1 to 4 merged.
TASK: Register the EventId range, add the log messages and emit them from the factory and the mapper.
KEY RULES: register the range in EventIdRanges.cs first (ADR-021), pack ids 950-954 without gaps; [LoggerMessage] source generator; never log EncinaError.Message or request data; exception through ForLogging().
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 6: Testing

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/Resilience/Timeouts/`, with `FakeTimeProvider` and a handler that awaits the token): timeout elapses and returns the coded `Left` with category `Timeout`; a caller cancellation stays a cancellation; the handler token is cancelled at the limit (R6); `IHasTimeout` value wins over the attribute; options for the type win over `IHasTimeout`; the global default applies only when set; per-attempt timeout with a retrying type; the deadline is shortened for the call and the previous context is restored after it (also on exception); a handler that ignores the token overruns and the test documents it (choice 9).
2. Guard (`tests/Encina.GuardTests/Resilience/Timeouts/`): the public members of this plan have no validated parameters (attribute and options are property bags; the validator and factory are internal and guarded through the unit tests), so no guard test is added; the justification is in the manifest row (a guard target of 0 only with that reason).
3. Contract (`tests/Encina.ContractTests/Resilience/`): a test request implementing `IHasTimeout` runs through a real compiled pipeline and is honored (executes code).
4. Property, integration, load, benchmark: not applicable; the timeout is arithmetic plus a Polly strategy; justification files for load and benchmark (`tests/Encina.LoadTests/Resilience/Timeouts/Timeouts.md`, `tests/Encina.BenchmarkTests/Resilience/Timeouts/Timeouts.md`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Phases 1 to 5 done.
TASK: Write the Phase 6 tests.
KEY RULES: tests execute real package code; FakeTimeProvider, never Thread.Sleep; Shouldly through Encina.Testing.Shouldly; a test never swallows an exception thrown by the code it drives.
REFERENCE FILES: .github/coverage-manifest/Encina.Polly.json, docs/en/guides/TESTING.md
```

</details>

### Phase 7: Documentation and Finalization

<details>
<summary>Tasks</summary>

1. `docs/features/resilience-timeouts.md` and the `Encina.Resilience` README section (`docs-writer`): the attribute, the interface, the precedence, the optimistic behavior (a handler must observe its token), the deadline.
2. `changelog.d/335-request-timeout.added.md`, `PublicAPI.Unshipped.txt`, the `Encina.Resilience.json` manifest rows (`mechanical-fixer`).
3. Verification: `dotnet build` 0 warnings; the tests filtered to `Timeouts`; EventId architecture tests; `--check-justifications`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Phases 1 to 6 green.
TASK: Documentation and verification.
KEY RULES: never edit CHANGELOG.md; documentation by docs-writer, metadata by mechanical-fixer; no hand-typed coverage figures (cite covref markers).
REFERENCE FILES: changelog.d/README.md, .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Relevance |
|--------|-----------|
| ADR-055 R2, R3, R5, R6 | Fixed order, one model, telemetry, token and deadline |
| ADR-062 | Category `Timeout` for `encina.timeout`; rule R5 for cancellation |
| ADR-029 | A timed-out attempt is transient; commands retry only when idempotent |
| ADR-054 D3, D4 | Names; tenant tag |
| Polly v8 timeout strategy (optimistic, `TimeoutRejectedException`, `TimeoutGenerator`) | Strategy shape, choices 4, 6 and 9 |
| gRPC deadlines (`DEADLINE_EXCEEDED`), Go `context.WithTimeout` | The earliest deadline wins |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `EncinaErrorCodes.Timeout`, `ErrorCategory.Timeout` | `src/Encina/Errors/` | The error of choice 6 |
| `IErrorClassifier` | `src/Encina/Recoverability/IErrorClassifier.cs` | Timeout is transient |
| `IRequestContextAccessor` | `src/Encina/Abstractions/` | Deadline visibility (choice 7) |
| `PipelineStage.Resilience` | `src/Encina/Pipeline/Ordering/PipelineStage.cs` | Stage of the behavior that runs the pipeline |
| Host mapping of `Timeout` to 504 | `src/Encina.AspNetCore`, `src/Encina.AwsLambda`, `src/Encina.AzureFunctions` | No host change |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Resilience` (`ResilienceTimeout`) | 950-959 | New range from the block 950-999; 950-954 used, 955-959 spare; verified free by repository-wide search on `098caa920` |

### Estimated File Count

| PR | Files | Content |
|----|-------|---------|
| PR-1 | 15 | 7 production (`TimeoutAttribute`, `IHasTimeout`, `TimeoutOptions`, `TimeoutOptionsValidator`, `TimeoutDetailKeys`, `TimeoutStrategyFactory`, `TimeoutRejectionMapper`), `ResilienceServiceCollectionExtensions.cs` (modified), `Encina.Resilience.csproj` (`InternalsVisibleTo`, only if missing), 3 tests (factory with precedence, mapper, options with validator), the DI test (modified), `PublicAPI.Unshipped.txt`, manifest rows |
| PR-2 | 14 | `TimeoutDeadlineContributor.cs`, `ResilienceServiceCollectionExtensions.cs` and the DI test (modified), the contributor test, `TimeoutLogMessages.cs`, `EventIdRanges.cs`, core `PublicAPI.Unshipped.txt`, `EncinaEventIdAllocationTests.cs`, `TimeoutStrategyFactory.cs` and `TimeoutRejectionMapper.cs` (modified, they emit the logs), their two tests (modified, log and span-event assertions), Resilience `PublicAPI.Unshipped.txt`, manifest |
| PR-3 | 7 | the contract test, 2 justification `.md` (load, benchmark), feature page, README section, changelog fragment, manifest row |

Each PR is within 15 files and 1,500 lines. No test double changes belong to this plan (the `IRequestContext` implementers are updated by R6).

Per-file targets (new files; baseline: none exists on `origin/main`):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `TimeoutAttribute.cs` | 90 | 0 | 0 | 0 | 0 | Property bag read by the factory tests; no parameters, so no guard test can reach it |
| `IHasTimeout.cs` | 0 | 0 | 0 | 0 | 0 | Interface, no executable code; its behavior is covered through the contract test of PR-2 on the factory |
| `TimeoutOptions.cs`, `TimeoutDetailKeys.cs` | 90 | 0 | 0 | 0 | 0 | Property bags and constants, read by the validator and mapper tests |
| `TimeoutOptionsValidator.cs` | 95 | 0 | 0 | 0 | 0 | Internal; one test per rule through the `InternalsVisibleTo` of Phase 2 |
| `TimeoutStrategyFactory.cs` | 90 | 0 | 80 | 0 | 0 | Every branch (total, per-attempt, instance value, precedence) with a fake clock; the contract test runs a real compiled pipeline; internal, no public parameters |
| `TimeoutRejectionMapper.cs` | 95 | 0 | 0 | 0 | 0 | Three branches (rejection, caller cancellation, other exception) driven directly |
| `TimeoutDeadlineContributor.cs` | 90 | 0 | 0 | 0 | 0 | Set, restore on exception, not-shorter case; internal, constructor reached through DI |
| `TimeoutLogMessages.cs` | 85 | 0 | 0 | 0 | 0 | Log assertions in PR-2 with a fake logger |

CRAP of each added method is at most 10 (the factory splits the generator into small methods).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #335: request timeouts in Encina.Resilience (ADR-055 R2, R3, R6). AGENTS.md rules apply. The foundation and R6 are merged first; if missing, stop and report.

IMPLEMENTATION OVERVIEW: (1) TimeoutAttribute, IHasTimeout, TimeoutOptions and validator; (2) a strategy factory that adds Polly's total and per-attempt timeout in the fixed slots and a mapper to the coded Left encina.timeout; (3) a deadline-shortening call contributor and registration in AddEncinaResilience; (4) EventIds 950-954 and the span event; (5) unit and contract tests with FakeTimeProvider; (6) documentation and fragments.

KEY PATTERNS: Left never an exception; a caller cancellation is never a timeout; opt-in (no timeout unless configured); time through TimeProvider; ErrorCategory.Timeout with a constant message; the deadline is set and restored through IRequestContextAccessor; specialists for PublicAPI, changelog, manifest and docs.

REFERENCE FILES: docs/architecture/adr/055-resilience-family.md, src/Encina/Errors/EncinaErrorCodes.cs, src/Encina/Abstractions/IRequestContextAccessor.cs, src/Encina/Recoverability/IErrorClassifier.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Timeouts act on live calls; a cached answer on timeout is #268's pattern (choice 5) |
| 2 | OpenTelemetry | ✅ | Polly's instruments plus a `timeout` span event (choice 8); tenant tag only with multi-tenancy |
| 3 | Structured Logging | ✅ | EventIds 950-954 (PR-2) |
| 4 | Health Checks | ❌ | No dependency or persistent state |
| 5 | Validation | ✅ | Options validated at startup; an invalid `IHasTimeout` value is ignored and logged |
| 6 | Resilience | ✅ | This is a resilience capability; it fills two slots of the R2 order |
| 7 | Distributed Locks | ❌ | No shared state |
| 8 | Transactions | ❌ | A cancelled handler rolls back through its own token handling; no transaction code here |
| 9 | Idempotency | ❌ | A timed-out command is not retried unless idempotent (ADR-029); the retry layer decides |
| 10 | Multi-Tenancy | ⏭️ | Tenant tag only; tenant-scoped timeout values are the issue file `tenant-scoped-timeouts` (related plan: #740) |
| 11 | Module Isolation | ❌ | Module-independent |
| 12 | Audit Trail | ❌ | Infrastructure concern |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|--------------|-------|--------|
| The foundation: `Encina.Resilience`, compiler, options front-ends, strategy-factory slots, rejection-mapper hook, call-contributor hook | No issue exists: issue file `resilience-foundation-encina-resilience.md` (family resilience-patterns-1, to be opened by the orchestrator); the hooks are acceptance items there | Blocks implementation (choice 1) |
| R6: token on the continuation and `IRequestContext.Deadline` | No issue exists: issue file `resilience-r6-pipeline-token-and-deadline.md` | Blocks cancellation of the handler (choices 6, 7 and 9) |
| #141 (cascading timeouts) | Plan by the sibling family, not on main | Reads the deadline this plan shortens; its choice 2 assumes this plan enforces |
| #137 (fallback) and #268 (fail-safe cache) | #137 sibling plan; #268 open | Fallback by composition (choice 5) |
| #728 telemetry plan | On main, choices pending | Event path and names (choice 8) |
| #3424 (open bug: rejections carry no code) | Open | Concerns the packages R1 removes; the timeout here already carries `encina.timeout` |

---

## Next Steps

1. Commit this plan, open its PR and link it from #335.
2. When #335 is picked up, re-check the choices against today's code and ask the maintainer about the Dilemmas (choices 1, 2, 3, 4, 5, 7, 8 and 9).
3. Brief workers for PR-1 to PR-3 in order, after the foundation and R6 are merged.
