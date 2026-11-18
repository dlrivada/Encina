# Implementation Plan: Cascading timeout coordination (timeout budget) in `Encina.Resilience`

> **Issue**: [#141](https://github.com/dlrivada/Encina/issues/141)
> **Type**: Feature
> **Complexity**: High (7 phases, database provider matrix for the integration tests, 4 pull requests)
> **Estimated Scope**: ~500 lines of production code + ~1,500 lines of tests (about 2,300 changed lines in total, four pull requests)

---

## Summary

Make inner operations respect the time left by outer ones: a request has one absolute deadline, a `TimeoutBudget` reads what is left and hands a shorter child budget to a nested call, outbound HTTP calls carry the remaining time, and database calls stop when the deadline passes. The comment of 2026-10-23 on #141 moves it into the 1.0 scope; [ADR-055](../architecture/adr/055-resilience-family.md) R6 (decision 6c) fixes the design: the pipeline continuation carries the token and `IRequestContext` carries the absolute deadline. The body of the issue is replaced where it contradicts that: its `TimeoutBudgetAccessor` (a second `AsyncLocal`), its `DateTime.UtcNow`, its `TimeoutBudgetExceededException` and its default-on 30 s timeout.

What exists today (read in `origin/main` at `f484cdf4f`):

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

- **Class:** Settled
- **Settled by:** ADR-055 R6 ("`IRequestContext` gains an absolute deadline"; outbound clients propagate it, the earliest wins) and `src/Encina/Abstractions/IRequestContextAccessor.cs`

</details>

<details>
<summary><strong>4. Errors and time source</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `Left` with the existing code `encina.timeout` (category `Timeout`) and a constant message; details `timeout.reason` = `budget_below_minimum` or `budget_expired`; time from `TimeProvider` (`GetUtcNow`, `CreateCancellationTokenSource`)** | ROP; no new code, so no new ADR-062 row; testable with `FakeTimeProvider` | The issue's exception type does not exist |
| **B) `TimeoutBudgetExceededException` and `DateTime.UtcNow`** | Matches the issue sketch | Exceptions for business flow; untestable clock |

### Chosen Option: **A — `Left` and `TimeProvider`**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (ROP; time comes from `TimeProvider`), ADR-055 R6 (`encina.timeout`) and ADR-062 (category `Timeout`)

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
| **B) The handler in `Encina.Http` (#1233)** | One place for outbound handlers | Package not on main |
| **C) Absolute UTC deadline header** | Exact | Sensitive to clock skew between services |

### Chosen Option: **A — Handler in `Encina.Resilience`, relative milliseconds** (recommended, pending the maintainer)

### Rationale

- A wins when services run on different clocks or before #1233; B wins once #1233 owns every outbound handler; C wins in a single-clock datacenter.
- The inbound side (`Encina.AspNetCore`) reads the same header and can only shorten the deadline, never extend it (the earliest deadline wins, R6); a malformed or non-positive value is ignored and logged without the value.

- **Class:** Dilemma

</details>

<details>
<summary><strong>8. Telemetry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Span tags on the request span; `encina.resilience.timeout.budget.remaining` histogram (seconds, unit `s`) and `encina.resilience.timeout.budget.expired` counter under `Meter("Encina.Resilience")`; tenant tag only with multi-tenancy** | Polly does not measure a budget; ADR-054 naming | Two instruments |
| **B) Logs only** | Smallest | No metrics |

### Chosen Option: **A — Two instruments and span tags**

### Rationale

- **Class:** Settled
- **Settled by:** ADR-055 R5 (Encina instruments only for what Polly does not measure) and ADR-054 D3 and D4

</details>

---

## Implementation Phases

Four pull requests: **PR-1** Phases 1 to 3 (budget, behavior, registration); **PR-2** Phase 5 (observability) and the unit tests of Phase 6 for it; **PR-3** Phase 4 (HTTP handler and inbound parsing); **PR-4** the database integration tests of Phase 6.

### Phase 1: Core Models

<details>
<summary>Tasks</summary>

1. `src/Encina.Resilience/Timeouts/TimeoutBudget.cs`: `public readonly struct TimeoutBudget` built by `TimeoutBudget.FromContext(IRequestContext context, TimeProvider timeProvider)` (null when the context has no deadline): `TimeSpan Remaining`, `bool IsExpired`, `TimeoutBudget CreateChild(TimeSpan? maxChildTimeout = null)` (never longer than `Remaining`), `CancellationToken GetCancellationToken()` through `timeProvider.CreateCancellationTokenSource(Remaining)`. Guards on both parameters.
2. `TimeoutBudgetOptions.cs`: `TimeSpan? DefaultTimeout` (null), `TimeSpan MinimumBudget` (default 100 ms), `TimeSpan MaxInboundBudget` (upper cap for an inbound header), `bool PropagateToHttpClients` is not an option (propagation is added per client by the handler, opt-in).
3. `TimeoutBudgetOptionsValidator.cs` (internal): positive values, `MinimumBudget` below `DefaultTimeout`.

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

1. `src/Encina.Resilience/Timeouts/DeadlineBudgetBehavior.cs`: `IPipelineBehavior<,>` at `PipelineStage.Admission` (first of its stage): when the context has no deadline and `DefaultTimeout` is set, passes `context.WithDeadline(now + DefaultTimeout)` down; when `Remaining < MinimumBudget`, returns `Left` `EncinaErrors.Create(EncinaErrorCodes.Timeout, category: ErrorCategory.Timeout, "The request has no time budget left.", details: reason)` without running the handler; tags the span. Enforcement stays with #335 (choice 2). Constructor: `TimeoutBudgetOptions` through `IOptions`, `TimeProvider? timeProvider = null`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 types exist.
TASK: Implement DeadlineBudgetBehavior.
KEY RULES: Left with a literal ErrorCategory.Timeout through the named category: argument and a constant message; never ex.Message; the behavior holds no scoped service (options and TimeProvider are singletons); declare PipelineStage.Admission.
REFERENCE FILES: src/Encina/Results/EncinaErrors.cs, src/Encina/Pipeline/Ordering/PipelineStageAttribute.cs
```

</details>

### Phase 3: Configuration and DI

<details>
<summary>Tasks</summary>

1. `TimeoutBudgetServiceCollectionExtensions.cs`: `AddEncinaTimeoutBudget(this IServiceCollection, Action<TimeoutBudgetOptions>? configure = null)` registers options with the validator (`ValidateOnStart`), the behavior (`TryAddEnumerable`), and `TimeProvider` through `TryAddSingleton`.
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

1. `Http/DeadlinePropagationHandler.cs` (`DelegatingHandler`): reads the deadline from `IRequestContextAccessor`, sets `X-Encina-Timeout-Ms` to the remaining whole milliseconds (never above an existing header: the smaller wins), links the request token to the remaining time through `TimeProvider`; no context means no header and no change (nothing is denied: propagation is not an access gate).
2. `Http/HttpClientBuilderExtensions.cs`: `AddEncinaDeadlinePropagation(this IHttpClientBuilder)`; `Microsoft.Extensions.Http` package reference in `Encina.Resilience.csproj`.
3. `src/Encina.AspNetCore/EncinaContextMiddleware.cs` and its options: when `TimeoutBudgetOptions` is registered, parse `X-Encina-Timeout-Ms` into a deadline capped at `MaxInboundBudget`; shorten only; malformed ignored.

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

1. `EventIdRanges.cs`: `ResilienceTimeoutBudget = (936, 945)`; add to the `Encina.Resilience` entry of `AssemblyRanges` and, for ids emitted by the host, none (all ids are emitted by `Encina.Resilience`; the middleware logs through its existing `EncinaContextMiddlewareLog`).
2. `Diagnostics/TimeoutBudgetLogMessages.cs`: 936 `DeadlineAssigned` (Debug), 937 `BudgetBelowMinimum` (Warning: request type, remaining ms), 938 `InboundDeadlineIgnored` (Debug: reason only), 939 `DeadlinePropagated` (Debug); 940-945 spare. Ids 936-945 are free in the repository.
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
2. Guard (`tests/Encina.GuardTests/Resilience/Timeouts/`): `FromContext` null context and null time provider; `CreateChild` negative; `AddEncinaTimeoutBudget` null services; null `configure` accepted.
3. Contract: not applicable beyond the unit tests (no new public interface); justification in the PR.
4. Integration (PR-4, `tests/Encina.IntegrationTests/Resilience/Timeouts/`): a base class plus one class per provider using the collections `ADO-SqlServer`, `ADO-PostgreSQL`, `ADO-MySQL`, `Dapper-*` (3), `EFCore-*` (3) and the MongoDB fixture; each runs a long command (`WAITFOR`, `pg_sleep`, `SLEEP`, a long Mongo operation) with a short deadline token and asserts cancellation. Load and benchmark: justification files (the budget is arithmetic on a struct).

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
| `PipelineStage.Admission` | `src/Encina/Pipeline/Ordering/PipelineStage.cs` | Behavior stage |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Resilience` (`ResilienceTimeoutBudget`) | 936-945 | From the block 900-949; 936-939 used, 940-945 spare |

### Estimated File Count

| PR | Files | Content |
|----|-------|---------|
| PR-1 | 12 | 5 production (budget, options, validator, behavior, registration), PublicAPI, 4 tests (budget, behavior, registration, guard), manifest, changelog |
| PR-2 | 9 | log messages, telemetry, `EventIdRanges.cs`, `EncinaEventIdAllocationTests.cs`, 2 tests, PublicAPI, manifest, README |
| PR-3 | 13 | handler, builder extension, `Encina.Resilience.csproj`, middleware, AspNetCore options, 3 tests, 2 PublicAPI, 2 manifests, feature page |
| PR-4 | 12 | base class, 10 provider classes, 2 justification `.md` |

Each PR is within 15 files and 1,500 lines; no test double changes belong to this plan (the `IRequestContext` implementers are updated by R6).

Per-file targets (new files; baseline: none exists on `origin/main`):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `TimeoutBudget.cs` | 95 | 100 | 0 | 0 | 0 | Arithmetic and guards fully reached by unit and guard tests in PR-1 |
| `TimeoutBudgetOptions.cs` | 95 | 0 | 0 | 0 | 0 | Properties and defaults; no parameters |
| `TimeoutBudgetOptionsValidator.cs` | 95 | 0 | 0 | 0 | 0 | Internal, one test per rule |
| `DeadlineBudgetBehavior.cs` | 90 | 0 | 0 | 0 | 0 | Every branch driven with a fake clock; guards of the constructor covered by unit |
| `TimeoutBudgetServiceCollectionExtensions.cs` | 90 | 100 | 0 | 0 | 0 | Guarded public method plus DI test |
| `DeadlinePropagationHandler.cs` | 90 | 0 | 0 | 0 | 0 | A fake inner handler observes the header |
| `HttpClientBuilderExtensions.cs` | 90 | 100 | 0 | 0 | 0 | Guarded public method |
| `TimeoutBudgetTelemetry.cs`, `TimeoutBudgetLogMessages.cs` | 85 | 0 | 0 | 0 | 0 | Listener-based tests |

CRAP of each added method is at most 10 (inbound parsing split into small methods).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #141: timeout budget in Encina.Resilience (ADR-055 R6). AGENTS.md rules apply. The foundation, R6 and #335 are merged first; if missing, stop and report.

IMPLEMENTATION OVERVIEW: (1) TimeoutBudget over IRequestContext.Deadline; (2) DeadlineBudgetBehavior (default deadline when configured, fail-fast Left encina.timeout); (3) AddEncinaTimeoutBudget; (4) DeadlinePropagationHandler and inbound header parsing in Encina.AspNetCore; (5) EventIds 936-945, histogram and counter; (6) unit, guard and per-provider integration tests (token cancels the command); (7) docs and fragments.

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
| 3 | Structured Logging | ✅ | EventIds 936-939 |
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
2. When #141 is picked up, ask the maintainer about the Dilemmas (choices 1, 2, 6, 7).
3. Brief workers for PR-1 to PR-4 in order, after the foundation, R6 and #335 are merged.
