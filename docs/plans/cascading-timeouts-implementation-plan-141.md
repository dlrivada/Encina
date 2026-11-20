# Implementation Plan: Cascading timeout coordination (timeout budget) in `Encina.Resilience`

> **Issue**: [#141](https://github.com/dlrivada/Encina/issues/141)
> **Type**: Feature
> **Complexity**: High (7 phases, database provider matrix for the integration tests, 5 pull requests)
> **Estimated Scope**: ~500 lines of production code + ~1,500 lines of tests (about 2,300 changed lines in total, five pull requests)

---

## Summary

Make inner operations respect the time left by outer ones: a request has one absolute deadline, a `TimeoutBudget` reads what is left and hands a shorter child budget to a nested call, outbound HTTP calls carry the remaining time, and database calls stop when the deadline passes. The comment of 2026-10-23 on #141 moves it into the 1.0 scope; [ADR-055](../architecture/adr/055-resilience-family.md) R6 (decision 6c) fixes the design: the pipeline continuation carries the token and `IRequestContext` carries the absolute deadline. The body of the issue is replaced where it contradicts that: its `TimeoutBudgetAccessor` (a second `AsyncLocal`), its `DateTime.UtcNow`, its `TimeoutBudgetExceededException` and its default-on 30 s timeout.

What exists today (read in `origin/main` at `277402987`):

- No deadline anywhere: `IRequestContext` (`src/Encina/Abstractions/IRequestContext.cs`) has `CorrelationId`, `TenantId`, `Timestamp`, `Metadata` and no deadline; `RequestHandlerCallback<TResponse>()` has no token. R6 adds both and has no issue yet.
- The ambient context already exists: `IRequestContextAccessor` (`src/Encina/Abstractions/IRequestContextAccessor.cs`, `src/Encina/Core/RequestContextAccessor.cs`), used by the database providers, so no new `AsyncLocal` is needed.
- `EncinaErrorCodes.Timeout = "encina.timeout"` exists (`src/Encina/Errors/EncinaErrorCodes.cs:63`) with the ADR-062 category `Timeout`.
- `EncinaContextMiddleware` (`src/Encina.AspNetCore/EncinaContextMiddleware.cs`) builds the context for an HTTP request.
- `Encina.Resilience` and `Encina.Http` (#1233, plan on main) are not on main.

Deliverables: `TimeoutBudget` (a read view over the context deadline), the budget behavior (assigns a deadline when the application configured a default, fails fast below a minimum), `AddEncinaTimeoutBudget`, a `DeadlinePropagationHandler` for named HTTP clients, inbound header parsing in `Encina.AspNetCore`, provider integration tests proving the database calls stop at the deadline, telemetry and tests.

- **Affected packages**: `Encina.Resilience` (planned), `Encina.AspNetCore`, `Encina` core (`EventIdRanges.cs`); the ten database providers only through integration tests (Design Choice 6).
- **Provider category**: Resilience for the code; Database (10) for the integration tests (ADO.NET x3, Dapper x3, EF Core x3, MongoDB).
- **Prerequisites**: foundation and R6 (tied to Dilemma 1 of the #136 plan); #335 for enforcement (Design Choice 2).

### Shared Pieces and Ownership

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `Encina.Resilience` project and compiler | foundation issue (`resilience-foundation-encina-resilience.md`) | all |
| `IRequestContext.Deadline`, `WithDeadline`, token on the continuation | R6 issue (`resilience-r6-pipeline-token-and-deadline.md`) | this plan, #136, #335 |
| `[Timeout]`, the total-timeout strategy that links the token to the deadline and shortens it | #335 (sibling plan, by number; not on main) | this plan relies on it |
| `TimeoutBudget`, budget behavior, `AddEncinaTimeoutBudget`, `DeadlinePropagationHandler`, inbound parsing | this plan (#141) | #335, #1233 may add the handler to its chain |
| EventIds 936-945 (`ResilienceTimeoutBudget`) | this plan | none |

---

## Design Choices

<details>
<summary><strong>1. Where the code lives while `Encina.Resilience` does not exist</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Wait for the foundation and R6** | Final home; no throwaway code | Blocked on two unplanned pieces |
| **B) Build in core `Encina` and `Encina.AspNetCore` now** | The budget is plain core logic | The issue's own affected-packages list says `Encina.Resilience`; R6 is needed either way |

### Chosen Option: **A — Wait for the foundation and R6** (recommended, pending the maintainer)

### Rationale

- A follows the spike report order; B wins if the maintainer wants the budget to ship with R6 without the rest of the foundation. Asked once for the family (#136 choice 1).

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Who enforces the deadline: this plan's behavior or #335's timeout strategy</strong></summary>

R6 says "the timeout behavior shortens" the deadline; #335 owns `[Timeout]` and the timeout behavior. #141 asks for a budget behavior that links a token and throws on expiry.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One enforcer: #335's total-timeout strategy links the token to `Deadline` and returns `encina.timeout`; this plan adds the budget view, the default assignment, the fail-fast check, propagation and telemetry** | No two timeout layers; `ErrorCategory.Timeout` produced in one place | #141's enforcement needs #335 merged first |
| **B) This plan enforces with its own behavior** | Independent of #335 | Two behaviors link tokens to the same deadline; duplicated `encina.timeout` creation |

### Chosen Option: **A — #335 enforces** (recommended, pending the maintainer)

### Rationale

- A wins on one owner per behavior. B wins if #141 must ship alone: its fail-fast and propagation still work without enforcement, but nothing cancels in-flight work until #335 lands.
- Consequence for names: under A this plan produces only the below-minimum rejection (the detail value `budget_below_minimum`, and a rejected counter). Under B it also produces expiry, so it adds the detail value `budget_expired` and counts expirations; those two additions exist only under B.

- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Ambient access: a new `TimeoutBudgetAccessor` or the existing accessor and the context deadline</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TimeoutBudget` is built from `IRequestContext.Deadline`; code without the context reads `IRequestContextAccessor.RequestContext`** | One ambient mechanism; the deadline travels with identity, tenant and correlation | The deadline requires R6 |
| **B) A new `AsyncLocal`-based `TimeoutBudgetAccessor`** | Matches the issue | A second ambient store that can disagree with the context |

### Chosen Option: **A — Context deadline plus the existing accessor**

### Rationale

- The choice holds under both options of choice 1: it concerns where the deadline is stored (the context), not which package hosts the budget. Reading the accessor from a background continuation sees the identity of the request that started it; the budget reads only the deadline from it, never the identity.
- **Class:** Settled
- **Settled by:** ADR-055 R6 ("`IRequestContext` gains an absolute deadline"; outbound clients propagate it, the earliest wins) and `src/Encina/Abstractions/IRequestContextAccessor.cs`

</details>

<details>
<summary><strong>4. Errors and time source</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `Left` with the existing code `encina.timeout` (category `Timeout`) and a constant message; a detail naming the reason (names in choice 12); time from `TimeProvider` (`GetUtcNow`, `CreateCancellationTokenSource`)** | ROP; no new code, so no new ADR-062 row; testable with `FakeTimeProvider` | The issue's exception type does not exist |
| **B) `TimeoutBudgetExceededException` and `DateTime.UtcNow`** | Matches the issue sketch | Exceptions for business flow; untestable clock |

### Chosen Option: **A — `Left` and `TimeProvider`**

### Rationale

- The `Left` is for the budget's own fail-fast and expiry. A caller's cancellation is never reported as a timeout: it keeps its own result (`encina.request.cancelled`, category `Cancelled`, ADR-062 rule R5) and is not counted as a server fault. The choice holds under both options of choice 1 (the package changes, the contract does not).
- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (ROP; time comes from `TimeProvider`), ADR-055 R6 (`encina.timeout`) and ADR-062 (category `Timeout`; rule R5 for cancellation)

</details>

<details>
<summary><strong>5. Defaults: is a timeout applied to every request</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Opt-in: `AddEncinaTimeoutBudget` registers the behavior; `DefaultTimeout` is null unless the application sets it; no deadline means no budget** | Pay-for-what-you-use | The sample of the issue sets 30 s explicitly |
| **B) Default 30 s for every request when the package is referenced** | Safe by default | Default-on behavior |

### Chosen Option: **A — Opt-in**

### Rationale

- **Class:** Settled
- The choice holds under both options of choice 1. No default hides a validation: an invalid option fails at startup (`ValidateOnStart`), never silently defaults.
- **Settled by:** AGENTS.md section 1 (every feature opt-in) and ADR-055 settled points ("Every capability is opt-in")

</details>

<details>
<summary><strong>6. Database command timeout integration (acceptance criterion)</strong></summary>

The criterion "database command timeout integration" is not the same as cancelling a command. AGENTS.md section 3 requires every database call to take a `CancellationToken`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Token only: the pipeline token (linked to the deadline by #335's strategy) already reaches every awaited database call; this plan adds integration tests per provider proving a long command stops at the deadline** | No production change in 10 providers; relies on the existing rule that every call takes a token | A driver may stop slower than a `CommandTimeout`; the server may keep running the statement |
| **B) Also set `CommandTimeout` (ADO.NET, Dapper), `SetCommandTimeout` (EF Core) and `MaxTime` (MongoDB) from the remaining budget** | The server also stops | About 30 call sites in 10 providers, each changing a connection or command factory; provider-specific units |

### Chosen Option: **A — Token only, proved per provider** (recommended, pending the maintainer)

### Rationale

- A wins on size and coherence (no per-provider drift); B wins if a runaway statement must be killed server-side after the client gave up.
- The provider matrix is covered by PR-4: one integration test per provider (Dapper x3, ADO.NET x3, EF Core x3, MongoDB) through the shared collection fixtures.

- **Class:** Dilemma

</details>

<details>
<summary><strong>7. HTTP propagation: where the handler lives and what the header carries</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `DeadlinePropagationHandler` in `Encina.Resilience`, header `X-Encina-Timeout-Ms` carrying the remaining milliseconds** | Works before `Encina.Http` exists; relative time has no clock-skew problem (gRPC `grpc-timeout` and Envoy `x-envoy-expected-rq-timeout-ms` are relative too) | A non-standard header name |
| **B) The handler in `Encina.Http` (#1233)** | One place for outbound handlers; `Encina.Resilience` takes no `Microsoft.Extensions.Http` reference | Package not on main |
| **C) Absolute UTC deadline header** | Exact | Sensitive to clock skew between services |

### Chosen Option: **A — Handler in `Encina.Resilience`, relative milliseconds** (recommended, pending the maintainer)

### Rationale

- A wins when services run on different clocks or before #1233; B wins once #1233 owns every outbound handler; C wins in a single-clock datacenter.
- Option A adds the package reference `Microsoft.Extensions.Http` to `Encina.Resilience.csproj` (already pinned centrally at 10.0.12 in `Directory.Packages.props`), so every user of `Encina.Resilience` takes it transitively; B avoids that cost. This dependency is part of the choice.
- The inbound side reads the same header and can only shorten the deadline, never extend it (the earliest deadline wins, R6); a malformed or non-positive value is ignored and logged without the value. A header may create a deadline where none exists (a finite deadline is shorter than none), so no upper cap option exists (`MaxInboundBudget` is not created). Where the parse lives is choice 9.

- **Class:** Dilemma

</details>

<details>
<summary><strong>8. Telemetry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Span tags on the request span; a remaining-budget histogram (seconds, unit `s`) and a rejected counter (below-minimum fail-fasts) named by the convention #728 settles (proposed `encina.resilience.timeout.budget.remaining` and `encina.resilience.timeout.budget.rejected`; under choice 2 option B an expired counter is added); tenant tag only with multi-tenancy** | Polly does not measure a budget; ADR-054 naming | Two instruments |
| **B) Logs only** | Smallest | No metrics |

### Chosen Option: **A — Two instruments and span tags** (recommended, pending the maintainer)

### Rationale

- ADR-055 R5 and ADR-054 D3 and D4 fix the rules, not these names. #728 (plan on main) leaves its choices 1 (who holds the `Meter("Encina.Resilience")` instance) and 8 (instrument names) pending: this plan creates no `Meter` of its own and takes that holder and convention. A wins when operators need the remaining budget at handler entry; B wins if span tags are enough.
- **Class:** Dilemma

</details>

<details>
<summary><strong>9. Where the inbound header is parsed</strong></summary>

The parse must run in the ASP.NET Core request path (`EncinaContextMiddleware`, `src/Encina.AspNetCore/EncinaContextMiddleware.cs`). `Encina.AspNetCore.csproj` references Encina, Caching, IdGeneration, Messaging and three Compliance and Security projects, not `Encina.Resilience` (verified).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A small `DeadlineHeaderOptions` type (with the header name) in core `Encina`, registered as a direct singleton; the middleware reads it with `GetService`; the budget behavior in `Encina.Resilience` only consumes the context deadline** | No new ProjectReference for any ASP.NET host (pay-for-what-you-use); the host stays free of Polly | A resilience-flavoured type in core |
| **B) `Encina.AspNetCore` gets a ProjectReference to `Encina.Resilience`** | Options type stays with the budget | Every ASP.NET host takes Polly and the resilience package |
| **C) A separate opt-in middleware in `Encina.Resilience` with a `Microsoft.AspNetCore.App` framework reference** | `Encina.AspNetCore` untouched | Framework reference for every non-web user of the package |

### Chosen Option: **A — Options in core** (recommended, pending the maintainer)

### Rationale

- A wins on dependencies; B wins if the maintainer accepts Polly in every host for one options type; C wins if the host must stay untouched. The middleware logs through its own `EncinaContextMiddlewareLog` with EventId 232 (`InboundDeadlineIgnored`, claimed inside the existing `AspNetCore` range 200-249, which is free from 232 in the registry of the batch and in the repository; the orchestrator registers the claim), so the id is emitted by the assembly that owns the range.
- **Class:** Dilemma

</details>

<details>
<summary><strong>10. Stage of the budget behavior and its name</strong></summary>

ADR-055 reserves `PipelineStage.Admission` for limiters and bulkhead and `Resilience` for retry, timeout and breaker. The issue names the class `TimeoutBudgetBehavior<,>`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TimeoutBudgetBehavior<,>` at `PipelineStage.Resilience`, the outermost behavior of that stage (a negative `Order`)** | Follows the stage ADR-055 assigns to timeouts; the issue's class name | The rate limiter in Admission runs before the fail-fast check |
| **B) At `PipelineStage.Admission`, first of its stage** | A request with no budget left is refused before consuming a rate-limit permit | Puts a timeout concern in the stage reserved for limiters |

### Chosen Option: **A — Resilience stage, outermost** (recommended, pending the maintainer)

### Rationale

- A wins on fidelity to ADR-055 and the issue; B wins when doomed requests must not consume limiter capacity. The class keeps the issue's name `TimeoutBudgetBehavior<,>` (the earlier name `DeadlineBudgetBehavior` is dropped).
- **Class:** Dilemma

</details>

<details>
<summary><strong>11. How a deadline assigned by the behavior reaches downstream readers</strong></summary>

`RequestHandlerCallback` carries only a token (R6) and `WithDeadline` returns a new `IRequestContext`, so the context assigned by `TimeoutBudgetBehavior` cannot travel through `next`. The readers of the deadline are `DeadlinePropagationHandler`, the handler code and #335's strategy. `RequestContextAccessor` accepts a set that keeps the same identity and origin and, during a dispatch, the same tenant, so a deadline change (a copy with `WithDeadline`) is accepted; its value is bound to the innermost live scope holder and is not readable after the scope ends.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The behavior sets the new context through `IRequestContextAccessor` for the call and restores the previous one in `finally`; readers other than pipeline behaviors read `accessor.RequestContext`; #335's strategy reads the same accessor** | Works with R6 as decided; nothing leaks past the call (restore in `finally`, holder invalidated at scope end); tests: the handler sees the deadline, and the accessor holds the original context after the request | A behavior that reads its own `context` parameter does not see the assigned deadline |
| **B) R6 makes the continuation carry the context: `RequestHandlerCallback<TResponse>(IRequestContext, CancellationToken)`** | Every behavior sees the deadline | Extends the R6 decision (token only) and the mechanical pass; belongs to the R6 issue |
| **C) No assignment in the pipeline: the default deadline is set where the context is created (the host middleware or `IEncina.Send`)** | No mutation of ambient state | A host that builds contexts by hand gets no default; touches core dispatch |

### Chosen Option: **A — Accessor set and restore** (recommended, pending the maintainer)

### Rationale

- A wins with R6 as decided; B wins if every behavior must read the deadline from its own parameter; C wins if the default should be a property of the entry point. A deadline supplied by the host (the inbound header, PR-3) is already in the context and needs none of this.
- **Class:** Dilemma

</details>

<details>
<summary><strong>12. Names of the detail key and its value</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The detail key `encina.timeout.reason` and the value `budget_below_minimum` (the one value the fail-fast of this plan produces), constants in `TimeoutBudgetDetailKeys` in the style of `EncinaErrorMetadataKeys`** | Follows the existing constants class; no value without a producer | Public names |
| **B) No detail** | Smaller | A caller cannot tell the fail-fast from other timeouts |

### Chosen Option: **A — The key, and the value this plan produces**

### Rationale

- **Class:** Trivial
- **Settled by:** the constants style of `src/Encina/Errors/EncinaErrorMetadataKeys.cs` (`encina.*` keys)

</details>

---

## Implementation Phases

Five pull requests, in order: **PR-1** Phases 1 to 3 (budget, behavior, registration, with their tests); **PR-2** Phase 5 (observability; it modifies the behavior of PR-1 to emit the logs and instruments); **PR-3** the inbound half of Phase 4 (`DeadlineHeaderOptions`, its registration, the middleware); **PR-4** the outbound half of Phase 4 (HTTP handler and builder extension); **PR-5** the database integration tests of Phase 6 and the documentation page. Every PR needs the `Encina.Resilience` project and R6 to exist (foundation merged); under option C of the shared Dilemma 1 of the #136 plan (creating the project in the first PR) the split is recounted.

### Phase 1: Core Models

<details>
<summary>Tasks</summary>

1. `src/Encina.Resilience/Timeouts/TimeoutBudget.cs`: `public readonly struct TimeoutBudget` built by `static bool TryFromContext(IRequestContext context, TimeProvider timeProvider, out TimeoutBudget budget)` (false when the context has no deadline; a struct is never null): `TimeSpan Remaining`, `bool IsExpired`, `TimeoutBudget CreateChild(TimeSpan? maxChildTimeout = null)` (never longer than `Remaining`), `TimeoutBudgetScope CreateScope()` where `TimeoutBudgetScope` (its own file `TimeoutBudgetScope.cs`, public, in `PublicAPI.Unshipped.txt`, with its own target row) is a disposable that owns the `CancellationTokenSource` created through `timeProvider.CreateCancellationTokenSource(Remaining)` and exposes `Token`; disposing it releases the timer, so a call never leaks a timer until expiry (a bare `GetCancellationToken()` would). Guards on both parameters of `FromContext` and on `maxChildTimeout` (negative rejected).
2. `TimeoutBudgetOptions.cs`: `TimeSpan? DefaultTimeout` (null: opt-in, choice 5) and `TimeSpan MinimumBudget` (default 100 ms, the issue sample's value). There is no `MaxInboundBudget` and no `PropagateToHttpClients` (propagation is added per client by the handler, opt-in).
3. `TimeoutBudgetOptionsValidator.cs` (internal): positive values, `MinimumBudget` below `DefaultTimeout`; an invalid value fails at startup.
4. `TimeoutBudgetDetailKeys.cs`: the constants of choice 12 (`encina.timeout.reason` and its one value, `budget_below_minimum`).
5. `src/Encina.Resilience/Encina.Resilience.csproj`: `InternalsVisibleTo` for `Encina.UnitTests` and `Encina.GuardTests`, a foundation acceptance item, added here only if missing (counted below).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina issue #141, plan docs/plans/cascading-timeouts-implementation-plan-141.md. Encina.Resilience exists; R6 landed (IRequestContext.Deadline, WithDeadline, token on the continuation).
TASK: Create TimeoutBudget, TimeoutBudgetOptions and the validator.
KEY RULES: time only through TimeProvider; no AsyncLocal of our own; no exception for expiry (a Left is built by the behavior); guards on public parameters; XML docs with examples.
REFERENCE FILES: src/Encina/Abstractions/IRequestContext.cs, src/Encina/Abstractions/IRequestContextAccessor.cs
```

</details>

### Phase 2: Default Implementation — the Budget Behavior

<details>
<summary>Tasks</summary>

1. `src/Encina.Resilience/Timeouts/TimeoutBudgetBehavior.cs`: `IPipelineBehavior<,>` at `PipelineStage.Resilience`, outermost of the stage (choice 10): when the context has no deadline and `DefaultTimeout` is set, assigns `context.WithDeadline(now + DefaultTimeout)` and makes it visible downstream through the mechanism of choice 11 (the ambient accessor, set for the call and restored in `finally`); when `Remaining < MinimumBudget`, returns `Left` `EncinaErrors.Create(EncinaErrorCodes.Timeout, category: ErrorCategory.Timeout, "The request has no time budget left.", details: reason)` without running the handler; tags the span. Expiry during the call is #335's (choice 2): this behavior produces only the below-minimum `Left`. Constructor: `(IOptions<TimeoutBudgetOptions> options, IRequestContextAccessor accessor, TimeProvider? timeProvider = null)`. A caller cancellation propagates untouched.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 types exist.
TASK: Implement TimeoutBudgetBehavior.
KEY RULES: Left with a literal ErrorCategory.Timeout through the named category: argument and a constant message; never ex.Message; a caller cancellation is never reported as a timeout; the behavior holds no scoped service (options and TimeProvider are singletons); declare the stage chosen in choice 10.
REFERENCE FILES: src/Encina/Results/EncinaErrors.cs, src/Encina/Pipeline/Ordering/PipelineStageAttribute.cs
```

</details>

### Phase 3: Configuration and DI

<details>
<summary>Tasks</summary>

1. `TimeoutBudgetServiceCollectionExtensions.cs`: `AddEncinaTimeoutBudget(this IServiceCollection, Action<TimeoutBudgetOptions>? configure = null)` registers `TimeoutBudgetOptions` with the validator (`ValidateOnStart`), the behavior (`TryAddEnumerable`), `IRequestContextAccessor` through `TryAddSingleton` (core registers the same implementation; `TryAdd` keeps the application's own) and `TimeProvider` through `TryAddSingleton`. In PR-3 the same method also registers the core type `DeadlineHeaderOptions` as a directly registered singleton instance with `TryAddSingleton` (not through `IOptions<>`, which always resolves with defaults; `Encina.AspNetCore` finds it with `GetService<DeadlineHeaderOptions>()` without referencing `Encina.Resilience`, as in Phase 4 task 3), and the registration test of PR-3 asserts that `GetService<DeadlineHeaderOptions>()` is null on a provider built without `AddEncinaTimeoutBudget` and non-null after the call; before PR-3 the method registers nothing for the header.
2. Registration test with `ValidateOnBuild` and `ValidateScopes`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Behavior exists.
TASK: Add AddEncinaTimeoutBudget and its DI test.
KEY RULES: every option type it resolves is registered; guards on services; a nullable configure accepts null (valid-result test).
REFERENCE FILES: src/Encina.Polly/ServiceCollectionExtensions.cs
```

</details>

### Phase 4: Provider and Host Integration

<details>
<summary>Tasks</summary>

1. `Http/DeadlinePropagationHandler.cs` (`DelegatingHandler`): reads the deadline from `IRequestContextAccessor`, sets `X-Encina-Timeout-Ms` to the remaining whole milliseconds (never above an existing header: the smaller wins), links the request token to the remaining time through `TimeProvider` in a `TimeoutBudgetScope` disposed per request (no timer outlives the request); no context means no header and no change (nothing is denied: propagation is not an access gate).
2. `Http/HttpClientBuilderExtensions.cs`: `AddEncinaDeadlinePropagation(this IHttpClientBuilder)`; `Microsoft.Extensions.Http` package reference in `Encina.Resilience.csproj`.
3. Inbound (choice 9, option A): `src/Encina/Abstractions/DeadlineHeaderOptions.cs` (a public options class with `string HeaderName` defaulting to `X-Encina-Timeout-Ms`; the opt-in is its registration by `AddEncinaTimeoutBudget`, so there is no flag; with its `src/Encina/PublicAPI.Unshipped.txt` line), and `src/Encina.AspNetCore/EncinaContextMiddleware.cs` plus `EncinaContextMiddlewareLog.cs`: detection: `AddEncinaTimeoutBudget` registers `DeadlineHeaderOptions` as a directly registered singleton instance (not through `IOptions<>`, which always resolves with defaults), and the middleware reads it per request with `HttpContext.RequestServices.GetService<DeadlineHeaderOptions>()`; null means the feature is off. The middleware constructor is unchanged, so the 10 `new EncinaContextMiddleware(` sites in three test files need no edit; the new cases are added to the existing `EncinaContextMiddlewareTests`. When it is present, parse `X-Encina-Timeout-Ms` into a deadline through `context.WithDeadline`; shorten only; malformed or non-positive ignored and logged by EventId 232 without the value. No ProjectReference to `Encina.Resilience` is added.
4. `DeadlinePropagationHandler`: when the deadline has passed it cancels the outbound request; the resulting `OperationCanceledException` is converted into a `Left` by the outbound layer (`Encina.Http`, #1233), never by this handler; a caller cancellation keeps its own category.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Phases 1 to 3 merged.
TASK: Add the handler, its builder extension and the inbound parsing.
KEY RULES: the earliest deadline wins; an inbound value can only shorten; never log the header value; time through TimeProvider; the handler is transient and reads the context through the accessor (no scoped service captured).
REFERENCE FILES: src/Encina.AspNetCore/EncinaContextMiddleware.cs, docs/plans/http-implementation-plan-1233.md
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. `EventIdRanges.cs`: `ResilienceTimeoutBudget = (936, 945)`, its line in `src/Encina/PublicAPI.Unshipped.txt` (`mechanical-fixer`), and its name in the `["Encina.Resilience"]` entry of `AssemblyRanges` (create the entry if no earlier plan of the family did; an entry holds several ranges). All ids of this range are emitted by `Encina.Resilience`; the inbound log of the middleware is EventId 232 inside the `AspNetCore` range (choice 9).
2. `Diagnostics/TimeoutBudgetLogMessages.cs`: 936 `DeadlineAssigned` (Debug), 937 `BudgetBelowMinimum` (Warning: request type, remaining ms), 938 `DeadlinePropagated` (Debug); 939-945 spare. Ids 936-945 are free in the repository.
3. `TimeoutBudgetTelemetry.cs`: the histogram and counter of choice 8.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: ADR-021, ADR-054, ADR-055 R5.
TASK: Register 936-945 first, then log messages and telemetry.
KEY RULES: ids packed from 936; emitted only by the mapped assembly; no error message or header value in logs; tenant tag only with multi-tenancy.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 6: Testing

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/Resilience/Timeouts/`): remaining, child never longer, expiry with `FakeTimeProvider`, default assigned only when configured, fail-fast below minimum with `Timeout` category, earliest deadline wins, inbound parse (valid, malformed, oversized, longer than existing).
2. Guard (`tests/Encina.GuardTests/Resilience/Timeouts/`): `TryFromContext` null context and null time provider; `CreateChild` negative; the `TimeoutBudgetBehavior` constructor `(IOptions<TimeoutBudgetOptions> options, IRequestContextAccessor accessor, TimeProvider? timeProvider = null)` with null options and null accessor, and a null `timeProvider` accepted (valid result); the `DeadlinePropagationHandler` constructor `(IRequestContextAccessor accessor, ILogger<DeadlinePropagationHandler> logger, TimeProvider? timeProvider = null)` with null accessor and null logger, and a null `timeProvider` accepted (PR-4); `AddEncinaTimeoutBudget` null services; null `configure` accepted.
3. Contract: not applicable beyond the unit tests (no new public interface); justification in the PR.
4. Integration (PR-5, `tests/Encina.IntegrationTests/Resilience/Timeouts/`; `Encina.IntegrationTests.csproj` gains a `ProjectReference` to `Encina.Resilience`, counted): a base class plus one class per provider using the collections `ADO-SqlServer`, `ADO-PostgreSQL`, `ADO-MySQL`, `Dapper-*` (3), `EFCore-*` (3) and the MongoDB fixture; each sends a real request through `IEncina` with `AddEncinaTimeoutBudget` (a short `DefaultTimeout`), whose handler takes `TimeoutBudget.TryFromContext(accessor.RequestContext, timeProvider, out var budget)` and `budget.CreateScope()` and passes the scope's token to a long provider command (`WAITFOR`, `pg_sleep`, `SLEEP`, a long Mongo operation), then asserts the cancellation; the test executes `TimeoutBudgetBehavior` and `TimeoutBudget`, not a raw command with a hand-made token. Load and benchmark: justification files (the budget is arithmetic on a struct).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Phases 1 to 5 done.
TASK: Write the Phase 6 tests.
KEY RULES: real code; collection fixtures only (never IClassFixture or new fixtures); ClearAllDataAsync in InitializeAsync; no Thread.Sleep; Shouldly via Encina.Testing.Shouldly.
REFERENCE FILES: tests/Encina.IntegrationTests/, .github/coverage-manifest/Encina.Polly.json
```

</details>

### Phase 7: Documentation and Finalization

<details>
<summary>Tasks</summary>

1. README of `Encina.Resilience` and `docs/features/resilience-timeout-budget.md` (`docs-writer`): the header contract, the shorten-only rule, DB behavior (token, not `CommandTimeout`).
2. `changelog.d/141-cascading-timeouts.added.md`, `PublicAPI.Unshipped.txt` (`Encina.Resilience`, `Encina.AspNetCore`), manifests (`mechanical-fixer`).
3. Verification: build 0 warnings; tests filtered to `Timeouts`; EventId architecture tests; `--check-justifications`.

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
| gRPC deadlines (`grpc-timeout`), Envoy `x-envoy-expected-rq-timeout-ms`, Go `context.WithDeadline` | Relative propagation, earliest deadline wins |
| ADR-055 R6, R5 | Context deadline, token, telemetry |
| ADR-062 | Category `Timeout` for `encina.timeout`; no new code |
| ADR-054 D3, D4 | Names, tenant tag |
| AGENTS.md section 3 | ROP, `TimeProvider`, async database calls with token |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `IRequestContextAccessor`, `RequestContext` | `src/Encina/Abstractions/`, `src/Encina/Core/RequestContext.cs` | Ambient context and deadline carrier |
| `EncinaErrorCodes.Timeout` | `src/Encina/Errors/EncinaErrorCodes.cs:63` | The error code |
| `EncinaContextMiddleware` | `src/Encina.AspNetCore/EncinaContextMiddleware.cs` | Inbound header |
| Shared fixtures `ADO-*`, `Dapper-*`, `EFCore-*` | `tests/Encina.IntegrationTests/` | Provider tests |
| `PipelineStage.Resilience` | `src/Encina/Pipeline/Ordering/PipelineStage.cs` | Behavior stage (choice 10 A) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Resilience` (`ResilienceTimeoutBudget`) | 936-945 | From the block 900-949; 936-938 used, 939-945 spare |

### Estimated File Count

| PR | Files | Content |
|----|-------|---------|
| PR-1 | 15 | 7 production (`TimeoutBudget`, `TimeoutBudgetScope`, options, validator, `TimeoutBudgetDetailKeys`, `TimeoutBudgetBehavior`, registration), `Encina.Resilience.csproj` (`InternalsVisibleTo`, only if missing), `PublicAPI.Unshipped.txt`, 5 tests (budget, behavior, validator, registration, guard), manifest |
| PR-2 | 11 | log messages, telemetry, `TimeoutBudgetBehavior.cs` (modified: emits the logs and instruments), `EventIdRanges.cs`, core `PublicAPI.Unshipped.txt`, `EncinaEventIdAllocationTests.cs`, telemetry test, behavior test (modified: log assertions), Resilience `PublicAPI.Unshipped.txt`, manifest, README |
| PR-3 | 10 | `DeadlineHeaderOptions.cs` (core), core `PublicAPI.Unshipped.txt`, `EncinaContextMiddleware.cs`, `EncinaContextMiddlewareLog.cs`, the registration extension (modified: registers `DeadlineHeaderOptions`), 2 tests (`EncinaContextMiddlewareTests` modified, registration test modified), manifests (AspNetCore, Encina core, Resilience) |
| PR-4 | 8 | handler (constructor `(IRequestContextAccessor accessor, ILogger<DeadlinePropagationHandler> logger, TimeProvider? timeProvider = null)`, emitting EventId 938 from the log class of PR-2; the handler test asserts 938), builder extension, `Encina.Resilience.csproj` (`Microsoft.Extensions.Http`), handler test, guard test (modified), Resilience `PublicAPI.Unshipped.txt`, manifest, changelog fragment |
| PR-5 | 15 | base class, 10 provider classes (ADO.NET x3, Dapper x3, EF Core x3, MongoDB), `Encina.IntegrationTests.csproj` (`ProjectReference` to `Encina.Resilience`), 2 justification `.md` (load and benchmark), feature page `docs/features/resilience-timeout-budget.md` |

Each PR is within 15 files and 1,500 lines; no test double changes belong to this plan (the `IRequestContext` implementers are updated by R6).

Per-file targets (new files; baseline: none exists on `origin/main`):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `TimeoutBudget.cs` | 95 | 100 | 0 | 0 | 0 | Arithmetic and guards fully reached by unit and guard tests in PR-1 |
| `TimeoutBudgetOptions.cs` | 95 | 0 | 0 | 0 | 0 | Properties and defaults; no parameters |
| `TimeoutBudgetDetailKeys.cs` | 0 | 0 | 0 | 0 | 0 | Constants only, no executable code |
| `TimeoutBudgetOptionsValidator.cs` | 95 | 0 | 0 | 0 | 0 | Internal, reached through the `InternalsVisibleTo` of Phase 1 task 5; one test per rule |
| `TimeoutBudgetScope.cs` | 90 | 0 | 0 | 0 | 0 | Dispose releases the source (a fake clock proves the timer is gone); no validated parameters (PR-1) |
| `TimeoutBudgetBehavior.cs` | 90 | 100 | 0 | 0 | 0 | Every branch driven with a fake clock, including the accessor set and restore; the guard test sends a null `options` and a null `accessor` to the public constructor and accepts a null `timeProvider` (PR-1) |
| `TimeoutBudgetServiceCollectionExtensions.cs` | 90 | 100 | 0 | 0 | 0 | Guarded public method plus DI test |
| `DeadlinePropagationHandler.cs` | 90 | 100 | 0 | 0 | 0 | A fake inner handler observes the header; the guard test sends a null `accessor` and a null `logger` to the public constructor and accepts a null `timeProvider` (PR-4); the handler test asserts the EventId 938 log |
| `HttpClientBuilderExtensions.cs` | 90 | 100 | 0 | 0 | 0 | Guarded public method |
| `Encina/Abstractions/DeadlineHeaderOptions.cs` (core) | 95 | 0 | 0 | 0 | 0 | The header-name property, read by the middleware and registration tests (PR-3) |
| `Encina.AspNetCore/EncinaContextMiddleware.cs` and `EncinaContextMiddlewareLog.cs` (modified) | no lowering of the existing targets; the new branch is covered by the new middleware tests | 0 | 0 | 0 | 0 | Existing per-file targets in the AspNetCore manifest stay; the new parse branch has valid, malformed, non-positive and longer-than-existing cases |
| `TimeoutBudgetTelemetry.cs`, `TimeoutBudgetLogMessages.cs` | 85 | 0 | 0 | 0 | 0 | Listener-based tests |

CRAP of each added method is at most 10 (inbound parsing split into small methods).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #141: timeout budget in Encina.Resilience (ADR-055 R6). AGENTS.md rules apply. The foundation, R6 and #335 are merged first; if missing, stop and report.

IMPLEMENTATION OVERVIEW: (1) TimeoutBudget over IRequestContext.Deadline; (2) TimeoutBudgetBehavior (default deadline when configured, fail-fast Left encina.timeout); (3) AddEncinaTimeoutBudget; (4) DeadlinePropagationHandler and inbound header parsing in Encina.AspNetCore; (5) EventIds 936-945, histogram and counter; (6) unit, guard and per-provider integration tests (token cancels the command); (7) docs and fragments.

KEY PATTERNS: no second AsyncLocal; earliest deadline wins, inbound can only shorten; opt-in; time through TimeProvider; Left with ErrorCategory.Timeout and a constant message; specialists for PublicAPI, changelog, manifest, docs.

REFERENCE FILES: src/Encina/Abstractions/IRequestContextAccessor.cs, src/Encina.AspNetCore/EncinaContextMiddleware.cs, src/Encina/Errors/EncinaErrorCodes.cs, docs/architecture/adr/055-resilience-family.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Timeouts act on live calls |
| 2 | OpenTelemetry | ✅ | Histogram, counter and span tags (ADR-055 R5); tenant tag only with multi-tenancy |
| 3 | Structured Logging | ✅ | EventIds 936-938 (and 232 in the AspNetCore range) |
| 4 | Health Checks | ❌ | No dependency or persistent state |
| 5 | Validation | ✅ | Options validation at startup; inbound header validated and ignored when malformed |
| 6 | Resilience | ✅ | Completes the timeout family with #335 |
| 7 | Distributed Locks | ❌ | No shared state |
| 8 | Transactions | ❌ | Wraps operations; cancellation of a transaction is the provider's rollback on cancel |
| 9 | Idempotency | ❌ | Caller responsibility |
| 10 | Multi-Tenancy | ✅ | Tenant tag only when multi-tenancy is on; no tenant-qualified key, so the `default` segment does not apply; tenant-scoped budgets are not planned |
| 11 | Module Isolation | ❌ | Module-independent |
| 12 | Audit Trail | ❌ | Infrastructure concern |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|--------------|-------|--------|
| Foundation and R6 (issue files `resilience-foundation-encina-resilience.md`, `resilience-r6-pipeline-token-and-deadline.md`) | Not on main | Block implementation (Dilemma 1, #136) |
| #335 timeout strategy | Plan by the sibling family, not on main | Enforcement (Design Choice 2) |
| #1233 `Encina.Http` | Plan on main, not built | May add the handler to its chain |
| #745 (open) | gRPC deadline propagation is outside the acceptance criteria; the gRPC client side is tracked there | None |

---

## Next Steps

1. Commit this plan, open its PR and link it from #141.
2. When #141 is picked up, ask the maintainer about the Dilemmas (choices 1, 2, 6, 7, 8, 9, 10 and 11).
3. Brief workers for PR-1 to PR-5 in order, after the foundation, R6 and #335 are merged.
