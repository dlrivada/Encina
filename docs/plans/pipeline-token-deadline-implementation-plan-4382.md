# Implementation Plan: Pipeline continuation carries a `CancellationToken` and `IRequestContext` carries the deadline (ADR-055 R6)

> **Issue**: [#4382](https://github.com/dlrivada/Encina/issues/4382)
> **Type**: Feature (ADR-055 R6, option 6c)
> **Complexity**: High by breadth, low by logic (4 phases, no database providers, 3 pull requests or about 19 under Dilemma 1 option A)
> **Estimated Scope**: ~120 lines of new production code, ~250 lines of new tests, plus a mechanical edit of 67 files under `src/` and 124 under `tests/` (about 311 invocation lines)

---

## Summary

Let a timeout, a hedged attempt or a cascading budget cancel the work it abandons. Today the continuation of a pipeline behavior has no token: `public delegate ValueTask<Either<EncinaError, TResponse>> RequestHandlerCallback<TResponse>();` (`src/Encina/Abstractions/RequestHandlerDelegate.cs:9`), so a behavior that creates a Polly timeout or hedging token cannot hand it to the rest of the pipeline, and a handler that uses its own `cancellationToken` parameter keeps running after the timeout returned. [ADR-055](../architecture/adr/055-resilience-family.md) R6 (option 6c) fixes both: the continuation becomes `RequestHandlerCallback<TResponse>(CancellationToken cancellationToken)`, and `IRequestContext` gains an absolute UTC deadline whose earliest value wins, so outbound clients can propagate it as gRPC's call-context deadline does.

What exists today (read on `origin/main` at `19b761ffa`, after `git fetch`):

- The builder wraps the handler and every behavior in closures that capture one token: `current = () => ExecuteHandlerAsync(_handler, _request, _cancellationToken)` and `current = () => ExecuteBehaviorAsync(step, _request, _context, nextStep, _cancellationToken)` (`src/Encina/Pipeline/PipelineBuilder.cs:75,86`); `PipelineBuilder` is `internal` and takes the token in its constructor (`:38,47-58`); the dispatcher builds and calls it with `pipeline()` (`src/Encina/Core/Encina.cs:181-182`). `StreamPipelineBuilder` has its own `StreamHandlerCallback<TItem>` (`src/Encina/Pipeline/StreamPipelineBuilder.cs:64`), which this plan does not change (the stream behaviors pass the token through `WithCancellation`).
- Counted on `origin/main` with PowerShell `Select-String` over `Get-ChildItem -Recurse -Filter *.cs` (excluding `bin` and `obj`), one match per line: the pattern `RequestHandlerCallback<` matches 67 files under `src/` and 124 under `tests/` (UnitTests 60, ContractTests 32, GuardTests 21, BenchmarkTests 5, IntegrationTests 3, PropertyTests 2, NBomber 1); the literal pattern `nextStep\(\)` matches 72 files and 146 lines under `src/` and 55 files and 165 lines under `tests/`; the looser pattern `next(Step)?\(\)` matches 75 files and 151 lines under `src/` and 86 files and 328 lines under `tests/` (other tools and patterns give other figures, so the worker re-measures with these exact patterns); the only creator of a callback under `src/` is `PipelineBuilder.cs:75`, while between 65 files (pattern `RequestHandlerCallback<...> x = () =>`, a method group or `new`) and 84 files (pattern `() => ValueTask/Task/new`) of `tests/` create one with a lambda. The numbers in the issue (95 and 121) were measured earlier; these are today's.
- `IRequestContext` (`src/Encina/Abstractions/IRequestContext.cs`) has members `CorrelationId`, `CausationId`, `Identity`, `IdempotencyKey`, `TenantId`, `Timestamp`, `Metadata` and `WithMetadata`, `WithIdempotencyKey`, `WithTenantId`; no deadline. It has 8 implementers: `RequestContext` (`src/Encina/Core/RequestContext.cs`), `IdentityPinnedRequestContext` (`src/Encina.Caching/Behaviors/IdentityPinnedRequestContext.cs`) and six test doubles: `BenchmarkRequestContext` (`tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks/CacheKeyGeneratorBenchmarks.cs`), `TestRequestContext` in `SoftDeleteQueryFilterBehaviorGuardTests.cs`, `RoutingPipelineMongoDBIntegrationTests.cs`, `CacheKeyGeneratorPropertyTests.cs`, `EventMetadataEnrichmentServiceTests.cs` and `tests/Encina.TestInfrastructure/PropertyTests/TestRequestContext.cs`.
- `RequestContext` copies its fields in three places: the private copy constructor (`RequestContext.cs:91-98`), the nested-dispatch factory (`:211-220`) and the copy helper (`:236-240`).
- The sibling plans #136 (hedging), #141 (cascading timeouts) and #335 (timeouts) are written against this change; #4380 (`Encina.Resilience`) writes its two behaviors after it.

What this plan delivers: `IRequestContext.Deadline` and `WithDeadline` with the earliest-wins rule on the 8 implementers, the token on the continuation with every behavior, processor step, builder closure and test updated in one mechanical pass and no compatibility overload, a guard against a behavior passing a token other than the one it received, and the tests of both.

- **Affected packages**: `Encina` core, and every package that has an `IPipelineBehavior<,>` (the 67 files span 39 packages; see Phase 2), plus the test projects.
- **Provider category**: none; no provider-specific code changes (the database behaviors only pass the token down).
- **Estimated size**: see "Implementation Phases"; the delegate change is atomic.
- **Prerequisites**: none; it lands before more behaviors are written (ADR-055 R6, "before more behaviors are added").

### Shared Pieces and Ownership (family resilience, ADR-055)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `RequestHandlerCallback<TResponse>(CancellationToken)`, the builder change, the sweep | **This plan (#4382)** | #4380 (PR-4 and later), #136, #141, #335 |
| `IRequestContext.Deadline`, `WithDeadline`, the earliest-wins rule | **This plan** | #335 (shortens it), #141 (enforces it and propagates it outward) |
| The timeout strategies, the deadline-shortening contributor, deadline headers | #335 and #141 (sibling plans, by number) | read the deadline defined here |
| `Encina.Resilience` behaviors written with the new signature | #4380 | none |

Order: PR-1 (the deadline) is independent and may merge any time; PR-2 (the token) lands before #4380 PR-4 and before every capability plan of the family; PR-3 follows PR-2.

---

## Design Choices

<details>
<summary><strong>1. How to land the mechanical pass (the issue's Dilemma D1, sharpened)</strong></summary>

The issue recommends one pull request per directory family "each building". That does not hold as written: a lambda `() => ...` cannot convert to a delegate with a parameter, so changing the delegate breaks every creator of a callback at once, and 65 to 84 test files create one. A family split builds only with a temporary shim. The issue also forbids a compatibility overload.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Staged: first convert every test creator to one shared helper (about 5 pull requests of at most 15 files), then change the delegate with a temporary `CancellationToken cancellationToken = default` (builder, helper, 3 builder tests), then one pull request per package family changing `nextStep()` to `nextStep(cancellationToken)` (about 12), then a last pull request removing the default** | Every pull request is within 15 files and builds; the last one makes a missing token a compile error everywhere | About 19 pull requests; a temporary default value for a few weeks (not an overload, but a shim the final pull request deletes); the helper is test code written only to be rewritten; while the default exists, an unmigrated behavior still calls `nextStep()` with the default token, so the builder closure forwards `default` and the work downstream of that behavior does not see the caller's cancellation until the behavior migrates |
| **B) One pull request for the delegate, builder, every behavior and every test (about 195 files; the changed-line count is 311 invocation lines by the literal pattern and up to 479 by the looser one, doubled in a diff that counts additions and deletions, plus the callback creators, so it can approach 1,500), with the label `size-exception-approved`** | One atomic break, as ADR-055 R6 says ("all files change in one mechanical pass"); no shim, no helper; reviewed as one mechanical diff with the scan test of choice 5 | Over the 15-file limit: the blocking `pr-size` job fails until the maintainer applies the label; a large diff to review; a lines count near the 1,500 limit must be re-measured (`git diff --shortstat`) |
| **C) The issue's family split without a shim** | Small pull requests | Cannot compile: the delegate type changes for all callers at once |

### Chosen Option: **B — One pull request with a size exception** (recommended, pending the maintainer)

### Rationale

- B wins when the maintainer accepts the label for a mechanical sweep, which is what ADR-055 R6 describes; A wins if no size exception is acceptable. C is listed because it is the issue's wording; it is not an option that builds.
- The deadline (PR-1) and the proof tests (PR-3) are separate pull requests under both options, so only the sweep is in question. If the sweep exceeds 1,500 changed lines when measured, option A applies.
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Does a nested dispatch inherit the deadline</strong></summary>

A nested dispatch (a `Send` from inside a handler or behavior) gets a new context from `RequestContext.ForNestedDispatch` (`RequestContext.cs:211`) that inherits identity but not the idempotency key.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) It inherits the parent deadline** | A budget cascades through the handlers the request calls, which is what #141 needs and what gRPC and Go `context.WithDeadline` do | A nested dispatch that starts detached work outlives or is cut by the parent |
| **B) It starts without a deadline** | Consistent with the idempotency key rule | A handler that calls another handler loses the budget, so the timeout of the outer request no longer bounds the inner work |

### Chosen Option: **A — Inherit** (recommended, pending the maintainer)

### Rationale

- A wins for synchronous nesting (the common case); B wins for a handler that dispatches detached background work, which needs a context created from scratch because the earliest-wins rule (choice 5) cannot extend an inherited deadline.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. How "no behavior ignores the token" is enforced</strong></summary>

The acceptance criterion is "no behavior ignores the token it receives". Removing the default value (choice 1) makes omitting an argument a compile error; passing `CancellationToken.None` or `default` still compiles.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A source scan test in `Encina.UnitTests` that fails on `nextStep(CancellationToken.None)`, `nextStep(default)` and `nextStep(default(CancellationToken))` under `src/`, as `EncinaEventIdAllocationTests` scans sources** | Cheap, fast, no new project; precedent in the repository | A text scan: it does not see a token stored in a variable and passed |
| **B) A Roslyn analyzer shipped with the build** | Semantic | A new analyzer project and package to maintain for one rule |

### Chosen Option: **A — Source scan test** (recommended, pending the maintainer)

### Rationale

- A wins now; B wins if the rule must also bind applications that write their own behaviors.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Token on the continuation, deadline on the context</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Continuation takes the token; the context carries the deadline (6c)** | A handler's own token is cancelled by a timeout; there is a deadline to send to the next service | Touches every behavior |
| **B) Deadline and token in the context only (6a)** | Smaller | A handler using its `cancellationToken` parameter is not cancelled; two tokens to choose from |
| **C) The continuation takes a token only (6b)** | Smaller | No deadline to propagate, so #141 does not work |

### Chosen Option: **A — 6c**

### Rationale

- Only A satisfies the ADR text.

- **Class:** Settled
- **Settled by:** ADR-055 R6 (option 6c chosen, 6a and 6b rejected)

</details>

<details>
<summary><strong>5. Earliest deadline wins; no way to extend</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `WithDeadline(DateTimeOffset? deadline)` returns a copy whose deadline is the earlier of the current one and `deadline`; `null` leaves it unchanged; there is no method that lengthens or clears it** | A callee can never grant itself more time than its caller gave | A caller that wants a fresh budget builds a new context |
| **B) The new value replaces the old one** | Simple | A nested behavior could extend the budget the outer timeout set |

### Chosen Option: **A — Earliest wins**

### Rationale

- The ADR states "the earliest deadline winning"; B contradicts it.

- **Class:** Settled
- **Settled by:** ADR-055 R6 ("the earliest deadline winning, as gRPC's call-context propagation does")

</details>

<details>
<summary><strong>6. The deadline is opt-in and absolute UTC</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Deadline` is `DateTimeOffset?`, `null` by default; whoever sets it computes it from an injected `TimeProvider`; the context never reads the clock** | No change for a request that sets nothing; deterministic tests with `FakeTimeProvider` | Callers add the timeout themselves |
| **B) A default deadline on every context** | Always bounded | A hidden timeout on every request |

### Chosen Option: **A — Optional absolute UTC value**

### Rationale

- Every capability is opt-in and time comes from `TimeProvider`.

- **Class:** Trivial
- **Settled by:** AGENTS.md section 1 (opt-in) and section 3 (`TimeProvider`)

</details>

---

## Implementation Phases

Recommended path (choice 1 option B): three pull requests. Under option A the sweep of Phase 2 becomes the 19 pull requests described there. Counts are by file name.

| PR | Phase | Content | Files |
|----|-------|---------|-------|
| 1 | 1 | The deadline on `IRequestContext` and its 8 implementers, with tests | 14 |
| 2 | 2 | The token on the continuation: the delegate, the builder, every behavior, step and test | about 195 (label `size-exception-approved`) |
| 3 | 3 | Proof and enforcement tests, changelog fragment, documentation | 8 at most |

### Phase 1: The Deadline on the Request Context

<details>
<summary>Tasks</summary>

PR-1 (14 files):

1. `src/Encina/Abstractions/IRequestContext.cs`: `DateTimeOffset? Deadline { get; }` and `IRequestContext WithDeadline(DateTimeOffset? deadline);` with XML docs (earliest wins; `null` leaves it unchanged; absolute UTC).
2. `src/Encina/Core/RequestContext.cs`: `Deadline` property; `WithDeadline` returns a copy with `min(current, value)`; `Deadline` copied in the copy constructor (`:91-98`) and the copy helper (`:236-240`); `ForNestedDispatch` (`:211-220`) copies the parent deadline (choice 2).
3. `src/Encina.Caching/Behaviors/IdentityPinnedRequestContext.cs`: `Deadline => _inner.Deadline`; `WithDeadline(...)` => `new IdentityPinnedRequestContext(_inner.WithDeadline(deadline), Identity)`.
4. The six test doubles (listed in the Summary) gain the two members.
5. `src/Encina/PublicAPI.Unshipped.txt`: `Encina.IRequestContext.Deadline.get` and `WithDeadline` (and the same on `RequestContext`).
6. Tests: `tests/Encina.UnitTests/Core/RequestContextDeadlineTests.cs` (earliest wins, null no-op, copy keeps it, nested dispatch inherits), `tests/Encina.UnitTests/Caching/IdentityPinnedRequestContextDeadlineTests.cs`, `tests/Encina.ContractTests/Core/RequestContextDeadlineContractTests.cs` (the contract for the implementers that the contract project can reach), `tests/Encina.GuardTests/Core/RequestContextGuardTests.cs` (modified: `WithDeadline(null)` returns a context with the unchanged deadline; a nullable parameter that accepts null gets a valid-result test, not an exception test).

Count: 3 production + 6 doubles + `PublicAPI` + 4 tests = 14.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina issue #4382, plan docs/plans/pipeline-token-deadline-implementation-plan-4382.md, ADR-055 R6.
TASK: Add Deadline and WithDeadline to IRequestContext and its 8 implementers.
KEY RULES: absolute UTC; earliest wins; null leaves it unchanged; copies keep it; a nested dispatch inherits it; the context never reads a clock; XML docs with an example; PublicAPI lines by mechanical-fixer; guard test for the nullable parameter is a valid-result test.
REFERENCE FILES: src/Encina/Abstractions/IRequestContext.cs, src/Encina/Core/RequestContext.cs, src/Encina.Caching/Behaviors/IdentityPinnedRequestContext.cs
```

</details>

### Phase 2: The Token on the Continuation

<details>
<summary>Tasks</summary>

PR-2 (choice 1 option B; one mechanical pass):

1. `src/Encina/Abstractions/RequestHandlerDelegate.cs`: `public delegate ValueTask<Either<EncinaError, TResponse>> RequestHandlerCallback<TResponse>(CancellationToken cancellationToken);` with the XML doc updated.
2. `src/Encina/Pipeline/PipelineBuilder.cs`: the constructor loses the `CancellationToken` parameter and the `_cancellationToken` field (`:44,52,58`); the closures become `ct => ExecuteHandlerAsync(_handler, _request, ct)` and `ct => ExecuteBehaviorAsync(step, _request, _context, nextStep, ct)` (`:75,86`); `src/Encina/Core/Encina.cs:181-182` calls `pipeline(cancellationToken)`; the three test files that construct it change (`PipelineBuilderGuardTests.cs`, `PipelineErrorCategoryTests.cs`, `PipelineOrderValidationHostedServiceTests.cs`).
3. Every behavior and processor step: `await nextStep()` becomes `await nextStep(cancellationToken)`, passing down the token the rest of the pipeline must use: the one received, or a token derived from it (a linked source, Polly's timeout or hedging token). The pre-, post-, error- and post-commit processor steps wrapped by `WrapProcessors` in `PipelineBuilder.cs` and `IModulePipelineBehavior.cs` are among the 16 files of core.
4. Every test: a callback created with a lambda gets the parameter (`_ => ...` or `ct => ...`), a test behavior calls `nextStep(cancellationToken)`.
5. No `PublicAPI` file changes in this pull request: the delegate has no line in `src/Encina/PublicAPI.Shipped.txt` or `PublicAPI.Unshipped.txt` (a `Select-String` for `RequestHandlerCallback<TResponse>` followed by `.Invoke` or `.BeginInvoke` finds none; only the `Handle(...)` lines of the behaviors name the type, and they keep their signature). The worker builds with the analyzer on and records any line it asks for.
6. XML doc examples that show `await nextStep()` (for example `IPipelineBehavior.cs:33`, `IRequestContext.cs`) are updated.

By package, the 67 files under `src/` are: Encina 16, Encina.Messaging 5, Encina.Polly 5, Encina.EntityFrameworkCore 3, Encina.Caching 3, Encina.Security.Sanitization 2, and one each in 33 further packages (ADO and Dapper providers, MongoDB, Marten, AspNetCore, OpenTelemetry, Extensions.Resilience, Security, Security.ABAC, Security.Audit, Security.Encryption, Security.PII, Security.AntiTampering, Security.Secrets, and the 15 Compliance packages).

Under option A the same edits are cut into: about 5 pull requests moving every test creator to one helper in `tests/Encina.TestInfrastructure`; one pull request changing the delegate with `= default`, the builder, the helper and the three builder tests; about 12 pull requests of at most 15 files, one per package family above, each with its tests; one last pull request removing `= default`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: PR-1 merged. Build from the repository root with the Release configuration.
TASK: Change RequestHandlerCallback<TResponse> to take a CancellationToken and update every invocation and creation under src and tests.
KEY RULES: no compatibility overload and no default value in the final state; every behavior passes down the token it received or one derived from it, never CancellationToken.None or default; the builder keeps converting caller cancellation into the Cancelled Left it converts today; do not touch StreamHandlerCallback; zero warnings; run the local CRAP table for the touched Handle methods first.
REFERENCE FILES: src/Encina/Abstractions/RequestHandlerDelegate.cs, src/Encina/Pipeline/PipelineBuilder.cs, src/Encina/Core/Encina.cs, src/Encina/Abstractions/IPipelineBehavior.cs
```

</details>

### Phase 3: Cross-Cutting Integration, Observability and Testing

<details>
<summary>Tasks</summary>

PR-3 (at most 8 files: 3 tests, the fragment and up to 4 documentation pages):

1. `tests/Encina.UnitTests/Pipeline/PipelineTokenFlowTests.cs`: a handler behind two behaviors receives the caller's token; a behavior that passes a linked token makes the handler's token cancel when the source cancels; `nextStep` invoked after cancellation yields the `Cancelled` Left of the builder.
2. `tests/Encina.UnitTests/Testing/Architecture/NextStepTokenScanTests.cs` (choice 3): fails on `nextStep(CancellationToken.None)`, `nextStep(default)` or `nextStep(default(CancellationToken))` under `src/`.
3. `tests/Encina.PropertyTests/Core/RequestContextDeadlineProperties.cs`: FsCheck invariant, for any sequence of `WithDeadline` calls the result is the minimum and never later than a previous value.
4. `changelog.d/4382-pipeline-token-and-deadline.changed.md`; `ROADMAP.md` only if the resilience milestone line mentions R6. No manifest file is edited: the touched rows keep the numbers quoted in "Research".
5. Load, benchmark and integration tests: justified (the change moves an argument; the per-call allocation benchmark of the pipeline is #728 R5).

Observability: none; no instrument, span or log is added. Cross-cutting: see the matrix; the transaction behaviors pass the token like every other behavior.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: PR-1 and PR-2 merged.
TASK: Add the token-flow test, the source scan test, the property test and the changelog fragment.
KEY RULES: tests execute real pipeline code; deterministic (FakeTimeProvider, no sleeps); Shouldly through Encina.Testing.Shouldly; fragment by mechanical-fixer.
REFERENCE FILES: tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, tests/Encina.UnitTests/Core/PipelineErrorCategoryTests.cs
```

</details>

### Phase 4: Documentation and Finalization

<details>
<summary>Tasks</summary>

1. XML docs of the delegate, `IRequestContext.Deadline` and `WithDeadline` with examples (in PR-1 and PR-2).
2. The behavior guide and the pipeline reference page under `docs/` that show `await nextStep()` are updated by `docs-writer` (in PR-3; list found by grep for `nextStep()` in `docs/` and the package READMEs at that time).
3. The changelog fragment of PR-3; no ADR (ADR-055 R6 is the decision).
4. Build verification: `dotnet build --configuration Release` with zero warnings; `dotnet test` for the five flags of the touched projects.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: code and tests merged.
TASK: Update the documentation pages and READMEs that show nextStep() and finish the fragment.
KEY RULES: docs by docs-writer; no hand-typed coverage figures; examples compile against the new signature.
REFERENCE FILES: docs/en/guides, src/Encina/Abstractions/IPipelineBehavior.cs
```

</details>

---

## Research

### Relevant Standards and Specifications

| Standard | Relevance |
|----------|-----------|
| ADR-055 R6 | The decision: token on the continuation, deadline on the context, one mechanical pass |
| gRPC call-context deadline propagation, Go `context.WithDeadline` | Earliest deadline wins |
| AGENTS.md sections 1, 3, 9 | Opt-in, `TimeProvider`, guard tests, CRAP gate, `pr-size` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `PipelineBuilder`, `ExecuteBehaviorAsync` | `src/Encina/Pipeline/PipelineBuilder.cs:68-87,170-196` | The closures that carry the token; the cancellation-to-`Left` conversion stays |
| `RequestContext.ForNestedDispatch` | `src/Encina/Core/RequestContext.cs:211` | Deadline inheritance (choice 2) |
| `EncinaEventIdAllocationTests` | `tests/Encina.UnitTests/Testing/Architecture/` | Precedent for a source scan test |
| Coverage manifests | `.github/coverage-manifest/Encina.json`, `Encina.Caching.json` | Targets quoted below |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | No logging is added |

### Estimated File Count

| Category | PR-1 | PR-2 | PR-3 |
|----------|------|------|------|
| Production | 3 | 67 | 0 |
| Test doubles and tests | 10 | about 124 (60 unit, 32 contract, 21 guard, 5 benchmark, 3 integration, 2 property, 1 NBomber) | 3 |
| `PublicAPI`, manifest, fragment, docs | 1 | 1 | 4 |

### Per-File Coverage Targets (all unchanged; the numbers are the manifest rows)

Targets are not typed here: the plan documents never type coverage figures by hand, and a target is not a measured value that a coverage citation marker can render. Each row names the manifest that holds the unchanged targets: [`Encina.json`](../../.github/coverage-manifest/Encina.json) for core, [`Encina.Caching.json`](../../.github/coverage-manifest/Encina.Caching.json) for the caching file. The brief quotes the numbers from the manifest at brief time, and the PR proves that none is lowered (the `target-ratchet` job).

| File | Flags with a target | Manifest | Justification |
|------|---------------------|----------|---------------|
| `Core/RequestContext.cs` | unit, guard | `Encina.json` | Existing targets stay: the new members are driven by `RequestContextDeadlineTests` and the guard test of PR-1 |
| `Abstractions/IRequestContext.cs` | unit, guard (both zero) | `Encina.json` | Interface with no coverable lines (existing rows) |
| `Behaviors/IdentityPinnedRequestContext.cs` (Encina.Caching) | unit | `Encina.Caching.json` | Every member is read by `IdentityPinnedRequestContextDeadlineTests` |
| `Pipeline/PipelineBuilder.cs` | unit, guard, contract | `Encina.json` | Existing targets stay: the closures change shape and the cancellation branches stay covered by the unit tests |
| `Pipeline/EncinaBehaviorGuards.cs` | unit, guard | `Encina.json` | Untouched by the signature; stays |
| `Abstractions/RequestHandlerDelegate.cs` | none | `Encina.json` (no explicit targets) | A delegate declaration has no executable lines |

The 66 other files under `src/` keep their manifest rows: the edit changes one argument and no row is added, lowered or removed.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #4382: the pipeline continuation carries a CancellationToken and IRequestContext carries the deadline (ADR-055 R6, 6c). AGENTS.md rules apply.

IMPLEMENTATION OVERVIEW: (1) IRequestContext.Deadline and WithDeadline on the 8 implementers; (2) RequestHandlerCallback<TResponse>(CancellationToken) and every invocation and creation under src and tests in one mechanical pass; (3) a token-flow test, a source scan test, a property test and the fragment; (4) documentation.

KEY PATTERNS: no compatibility overload; earliest deadline wins; the context never reads a clock; behaviors pass down the token they received or one derived from it; caller cancellation still becomes the Cancelled Left in the builder; specialists for PublicAPI, changelog and docs.

REFERENCE FILES: docs/architecture/adr/055-resilience-family.md, src/Encina/Pipeline/PipelineBuilder.cs, src/Encina/Core/RequestContext.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The cache behaviors only pass the token down |
| 2 | OpenTelemetry | ❌ | No instrument or span; ADR-054 D4 tenant tags unaffected |
| 3 | Structured Logging | ❌ | No log added |
| 4 | Health Checks | ❌ | Not applicable |
| 5 | Validation | ❌ | Not applicable |
| 6 | Resilience | ✅ | The purpose: timeouts, hedging and budgets cancel abandoned work |
| 7 | Distributed Locks | ❌ | Not applicable |
| 8 | Transactions | ✅ | The transaction behaviors pass the token; the Resilience stage sits outside them |
| 9 | Idempotency | ❌ | A nested dispatch still does not inherit the idempotency key |
| 10 | Multi-Tenancy | ❌ | The deadline copy keeps the tenant like every other copied field; no tenant-keyed state |
| 11 | Module Isolation | ❌ | The module behavior adapter passes the token like the others |
| 12 | Audit Trail | ❌ | Not applicable |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|--------------|-------|--------|
| CRAP of the touched `Handle` methods | To measure before PR-2 with the local CRAP table (AGENTS.md section 9, `docs/engineering/crap-gate-design.md`) | A touched method above 10 fails the blocking `crap-gate`; such a method is split in a pull request before the sweep, outside this plan's count |
| Maintainer decision on choice 1 | Pending | Selects one sweep with the label or the staged path |
| #4380 | Plan by number | Starts its behaviors (PR-4) after PR-2 |
| #136, #141, #335 | Plans by number | Need PR-1 and PR-2 |
| The two benchmark projects of `Encina.Polly` and `Encina.Extensions.Resilience` | Part of the 5 benchmark files that create callbacks, deleted later by #4380 | Updated by the sweep anyway so the build stays green |

---

## Next Steps

1. Commit this plan, open its PR and link it from #4382.
2. When #4382 is picked up, re-check the counts against today's code and ask the maintainer about the Dilemmas (choices 1, 2 and 3).
3. Brief a worker for PR-1, then PR-2 (after the label decision), then PR-3; #4380 PR-4 follows PR-2.
