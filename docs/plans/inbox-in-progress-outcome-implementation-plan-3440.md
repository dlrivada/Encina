# Implementation Plan: `Encina.Messaging` Inbox outcome contract for an in-progress duplicate (200 or 409)

> **Issue**: [#3440](https://github.com/dlrivada/Encina/issues/3440)
> **Type**: Feature
> **Complexity**: Medium (8 phases, no new per-provider code, tests on all 10 database providers, ~28 files in 3 pull requests)
> **Estimated Scope**: ~700-900 lines of production code + ~900-1,100 lines of tests and documentation

---

## Summary

The inbox pipeline can tell a caller only "a response" or "an error". A vendor that times out and redelivers a webhook while the first handler is still running therefore gets the same answer as a first delivery or, today, runs the handler a second time (`handlerRuns=2`, reproduced on [#2401](https://github.com/dlrivada/Encina/issues/2401) with the real `InboxPipelineBehavior` and `InboxOrchestrator`). SPEC-002 REQ-036 and AC-036 require that "a duplicate arriving while the first delivery is still in progress is answered with the configured 200 or 409 and processed once". [#2401](https://github.com/dlrivada/Encina/issues/2401) settled the mechanism (atomic claim plus a lease for crashed handlers, on the 10 database providers) but not the contract that tells the caller which of the three situations it is in. This feature adds that contract.

What it implements:

- `InboxOutcome` (`Processed`, `Replayed`, `InProgress`, `Failed`), the value the orchestrator derives for every delivery and records in telemetry. Whether it also travels in a new return type `InboxProcessResult<TResponse>` or `InboxOrchestrator.ProcessAsync` keeps returning `Either<EncinaError, TResponse>` and returns the in-progress case as a typed `Left` itself is Design Choice 1 (recommended: keep `Either`, one type fewer; the pre-1.0 rule means the old signature is replaced, never kept beside a new one).
- A typed `Left` with the new code `inbox.in_progress`, **created with `ErrorCategory.Conflict`** (ADR-062 already decides that the sibling `idempotency.in_progress` is `Conflict`, 409) and the remaining lease as a `TimeSpan` under `EncinaErrorMetadataKeys.RetryAfter`, so the pipeline carries the in-progress case to the HTTP layer without a new return type and without any host parsing the code text.
- In `Encina.AspNetCore`: the 409 answer comes from the ADR-062 category table; the configurable 200 answer (`EncinaAspNetCoreOptions.InboxInProgressResponse`, default pending the maintainer), `Retry-After` on a 409 and the per-endpoint override through the existing `statusCode` argument of `ToProblemDetails` are Design Choice 4 (an extension of the ADR-062 table, decided with #3419/#3420).
- Observability of the outcomes (a counter, a tag on the existing inbox activity, a log entry for the in-progress case).

Standards and specifications: SPEC-002 REQ-036 and AC-036; ADR-001 and ADR-006 (Railway Oriented Programming); ADR-048 (inbox record versus business transaction); ADR-062 (error category); ADR-054 D3 and D4 (meter name, tenant tag); the IETF idempotency-key draft (an in-flight request with the same key is answered 409). The issue's option B for the 200 answer is "200 with the previous outcome once available"; while a delivery is in progress no previous outcome exists (the first handler has not produced one) and once it exists the redelivery is `Replayed` and already answers 200 with the cached response, so the 200 option returns a small status body instead.

Estimated scope: 28 files (5 new and 5 modified production files, 5 metadata files, 10 test files, 3 documentation files), about 1,400-1,800 changed lines, shipped as three pull requests (see "Pull request split").

Affected packages: `Encina.Messaging` (outcome type, orchestrator, behavior, diagnostics), `Encina.AspNetCore` (mapping and option), `Encina` (EventId range registry). No provider package changes: the outcome is derived from the claim result of [#2401](https://github.com/dlrivada/Encina/issues/2401) inside the shared orchestrator.

Provider category: Database (10), but only through tests. The feature adds no SQL and no store method, so AGENTS.md section 5 is met by running the in-flight redelivery scenario on every provider (Phase 7), not by new provider implementations.

---

## Design Choices

<details>
<summary><strong>1. Outcome carrier — typed result from the orchestrator, typed <code>Left</code> through the pipeline</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `InboxProcessResult<T>` (outcome + `Either`, the outcome non-nullable with a `Failed` state) from the orchestrator; the behavior maps `InProgress` to a `Left` with code `inbox.in_progress`** | The outcome is inspectable by a caller of the orchestrator | A new public result type that has no consumer today: the only production caller (`InboxPipelineBehavior`) returns `.Result` and discards the outcome, and telemetry is recorded inside the orchestrator; `default(InboxProcessResult<T>)` would hold a default `Either` in the Bottom state; the return type of `ProcessAsync` changes, so every caller and test in the Research list changes in the same pull request. It is justified only by a named consumer (for example the Azure Functions or Lambda hosts, or a webhook endpoint reading the outcome) |
| **B) Keep `ProcessAsync` as it is and add `ProcessWithOutcomeAsync`** | Existing callers untouched | Two entry points for one operation; AGENTS.md section 3 forbids compatibility layers pre-1.0 |
| **C) An ambient accessor (`AsyncLocal<InboxOutcome>`) read by the HTTP layer** | The HTTP layer sees all three outcomes | Hidden state across an async pipeline, hard to test, breaks when the response is produced outside the request flow |
| **D) `ProcessAsync` keeps returning `Either<EncinaError, TResponse>`; the orchestrator derives the `InboxOutcome`, records it in the metric, the activity tag and the log from that one place, and returns `Left(inbox.in_progress)` itself for an in-progress duplicate** | One type fewer; no signature change, so no caller changes and the first pull request builds alone; satisfies ADR-001/006 and the no-compatibility-layer rule | The outcome is visible to telemetry and tests (through a captured metric or the log), not as a return value |

### Chosen Option: **D — keep `Either`, derive and record the outcome in the orchestrator** (recommended, pending the maintainer)

### Rationale

- ADR-001 and ADR-006 make `Either<EncinaError, T>` the only way an operation reports a non-success; an in-progress duplicate is a retryable condition of the request, not a response. A and D both satisfy that rule and the no-compatibility-layer rule, so the choice between them is not settled by them: D wins when no caller needs the outcome as a value (the case today), A wins if a consumer is named.
- A replay is a success for the HTTP caller (the cached response), so nothing in the HTTP layer needs to tell `Replayed` from `Processed`; telemetry does, through the counter of Phase 6.
- Under D the outcome unit tests assert the recorded metric and tag; under A they assert the returned value. The rest of this plan is written for D and notes where A differs.
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Default HTTP answer for an in-progress duplicate — 409 with Retry-After, or 200</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) 409 Conflict with `Retry-After` (lease remaining), 200 selectable by option** | An unambiguous in-flight signal; the IETF idempotency-key draft and payment APIs that dedupe by key answer a concurrent same-key request with 409; the lease expiry gives a truthful `Retry-After` | A vendor that treats every non-2xx as a failed delivery may count it against the endpoint or give up |
| **B) 200 OK with a body that says "in progress", 409 selectable by option** | Compatible with vendors that retry or disable the endpoint on any non-2xx; the vendor keeps its normal retry schedule | Hides an in-flight state behind a success status; a stuck handler is harder to notice from the sender side. **Interaction with Design Choice 3:** with the 200 answer the vendor stops redelivering, so if the first handler then crashes, the re-claim after the lease expiry never happens (no inbox processor re-runs claimed rows) and the message is lost; the guide states this, and a reclaim processor is either out of scope (an issue file) or required for the 200 option, which is a decision of #2401 |
| **C) No default: the option is required and startup validation fails when it is not set** | Forces the decision per application | Breaks "pay for what you use" (a working inbox would need an HTTP setting) and AddEncinaAspNetCore without options stops working |

### Chosen Option: **A — 409 Conflict with Retry-After by default (recommended, pending the maintainer)**

### Rationale

- REQ-036 fixes only that the answer is "200 or 409" and configurable; which one is the default depends on the vendors the application receives from. 409 wins for a first-party client or a vendor that retries on 4xx/5xx; 200 wins for a vendor that stops delivering or flags the endpoint unhealthy after non-2xx answers.
- The option exists in both cases, so the choice changes one default value, not the design; the recommendation follows the IETF draft and the key-deduplicating payment APIs.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Lease expiry of a crashed first handler — re-run within MaxRetries, or dead-letter (owned by #2401, adopted here)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The next delivery after the lease expires re-claims the row and re-runs the handler, counting the lost attempt against `MaxRetries`** | At-least-once for idempotent handlers; reuses `RetryCount` and the existing dead-letter capture once the retries are used up | A handler with irreversible side effects (a charge) may run twice if it crashed after the effect and before `MarkAsProcessedAsync` |
| **B) A row whose lease expired is dead-lettered and never re-run automatically** | No second execution of a handler that may have had an effect | Needs an operator or a replay tool for every crash; a short outage dead-letters healthy messages |
| **C) Configurable per request type** | Fits both kinds of handler | More configuration and a second code path to test on 10 providers |

### Chosen Option: **A — Re-run within MaxRetries, then the existing dead-letter path (recommended, pending the maintainer)**

### Rationale

- The behavior of an expired lease is part of the claim and lease mechanism that [#2401](https://github.com/dlrivada/Encina/issues/2401) implements, and it is decided in #2401's register entry, not here: this plan opens no second Dilemma, it adopts #2401's answer (so the maintainer is asked once) and only needs it to know what the caller sees after expiry (an `InProgress` outcome ends when the lease ends, then the delivery is `Processed` again). The table records the options for context.
- The interaction with Design Choice 2 (a 200 answer stops the vendor's redelivery, so a crashed first handler is never re-claimed) is stated in D2's cons and in the guide.
- A wins when handlers are idempotent or transactional with the inbox row (the ADR-048 model, where the processed mark commits with the business effect); B wins for handlers with irreversible external effects and no idempotency of their own. The Inbox's contract is idempotent processing of idempotent-by-construction handlers, which favors A as the default.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. How the host answers an in-progress duplicate under ADR-062 — category, Retry-After on a 409 and the 200 option</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The error is created with `ErrorCategory.Conflict` (409 from the ADR-062 table, no code-text mapping); the remaining lease travels as a `TimeSpan` under `EncinaErrorMetadataKeys.RetryAfter`; `ToProblemDetails` (and the table consumed by the three hosts) writes `Retry-After` on this 409, and answers 200 when `InboxInProgressResponse.Ok200` is set. This **extends ADR-062**: its header rule today allows `Retry-After` only with 429 or 503, and it has no 200 option. The 200 branch recognises the in-progress error by a typed details value (`InboxErrorMetadataKeys.Outcome == "in_progress"`), never by the code text. The extension is decided with the owners of #3419/#3420 and written into ADR-062's table by that work** | Follows the contract: the producer sets the category, no host parses a code; `Retry-After` is truthful (the lease remaining); REQ-036's configurable 200 or 409 is met | Needs an ADR-062 amendment (Retry-After on 409; a per-error 200 override) agreed with #3419 |
| **B) 409 only, strictly per ADR-062: no `Retry-After`, no 200 option** | No ADR change | Does not meet REQ-036 (200 or 409, configurable) and loses the truthful retry hint |
| **D) The 200 option and `Retry-After` on 409 as an `Encina.AspNetCore`-only setting; `Encina.AzureFunctions` and `Encina.AwsLambda` answer a plain 409** | No coordination with the other two hosts' slices | The three hosts answer the same duplicate differently (a function app returns 409 without a hint where an ASP.NET Core app returns 200), against the host coherence ADR-062 H2 and H3 establish |
| **C) Keep a branch on the string `inbox.in_progress` in `MapErrorCodeToStatusCode`** | Smallest diff | Adds to the code-text mapping that sub-issues S2 and H1 to H3 of #3419 delete; contradicts ADR-062 ("a host never parses the error code to choose a status") |

### Chosen Option: **A — category `Conflict` plus an agreed ADR-062 extension** (recommended, pending the maintainer)

### Rationale

- Under A the `Retry-After` on this 409 and the 200 override are **table data shared by the three hosts** (`Encina.AspNetCore`, `Encina.AzureFunctions`, `Encina.AwsLambda`; H1 to H3 of #3419 each consume the central table). This plan implements the `Encina.AspNetCore` consumer and its option (PR B); the two other hosts follow through the issue file `artifacts/issues/inbox-in-progress-azure-aws-hosts.md`, opened with this plan, so the hosts stay coherent. D is the alternative if the maintainer wants the setting local to ASP.NET Core.
- C is rejected by ADR-062. A and B both follow it; A is the only one that meets REQ-036, and the extension is a small, additive row, so it is a decision with #3419/#3420, not a local edit.
- Sequencing: the HTTP part (pull request B) starts after #3419's central status table (S2) and the AspNetCore host slice (H1) are merged; until then the 200 option and `Retry-After` have no table to extend. If B of this plan would land first, the rebase onto #3419 wins and the code-text branch is never written.
- The error message is still not exposed (AGENTS.md section 3: `EncinaError.Message` never reaches the client unless the Development-only option is on); the body carries the code only.
- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Provider scope — no new provider code; outcome derived in the shared orchestrator</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The orchestrator derives the outcome from the claim result of #2401; providers are covered by running the scenario on each** | One implementation, identical on all 10 providers; AGENTS.md section 5 coherence for free | Depends on the claim result of #2401 exposing "already processed" versus "in progress" distinctly |
| **B) Each store returns the outcome itself** | Stores could use provider-specific shortcuts | 10 implementations of logic that is not provider-specific; violates provider coherence |

### Chosen Option: **A — Derive in the orchestrator, prove on all providers by tests**

### Rationale

- The claim, the lease and the row states are provider code and belong to [#2401](https://github.com/dlrivada/Encina/issues/2401); the outcome is a pure function of the claim result and the cached row, so it lives in `Encina.Messaging`.
- **Class:** Settled
- **Settled by:** AGENTS.md section 5 (providers differ only in implementation; never mix provider-specific code into abstractions) and [#2401](https://github.com/dlrivada/Encina/issues/2401) (claim and lease on the 10 providers, settled by elimination on 2026-10-17)

</details>

<details>
<summary><strong>6. Health check for stuck in-progress rows — defer to the claim and lease work</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Defer: a follow-up issue adds a store count of rows past their lease and extends `InboxHealthCheck`** | This feature stays free of new store methods; the query belongs with the row state that #2401 creates | The first release of the outcome has no health signal for stuck rows |
| **B) Include now: a new `IInboxStore` method on 10 providers** | Complete signal immediately | Adds a store method and 10 SQL variants to a feature that otherwise has none; exceeds the 15-file limit; depends on the state column that #2401 introduces |

### Chosen Option: **A — Defer, with a follow-up issue**

### Rationale

- The row state ("claimed, lease expires at") is created by [#2401](https://github.com/dlrivada/Encina/issues/2401); the query that finds expired claims reuses it, so it should be written against the final schema. The draft of the issue is in the Cross-Cutting Integration Matrix notes and `artifacts/issues/`.
- **Class:** Settled
- **Settled by:** AGENTS.md section 6 (Cross-cutting integration check: Defer means open an issue and reference it) and the one-pull-request size rule of AGENTS.md section 11

</details>

---

## Implementation Phases

### Phase 1: Core Models — `InboxOutcome` and the error code

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/Inbox/InboxOutcome.cs`** (new), namespace `Encina.Messaging.Inbox`:
   - `public enum InboxOutcome { Processed, Replayed, InProgress, Failed }` with XML docs: `Processed` = this delivery ran the handler; `Replayed` = the answer is the cached response of an earlier delivery; `InProgress` = another delivery holds the claim and its lease has not expired; `Failed` = a store failure stopped the flow before any other outcome was reached (counted, never reported as success).
   - Under Design Choice 1 option A only: `public readonly record struct InboxProcessResult<TResponse>(InboxOutcome Outcome, Either<EncinaError, TResponse> Result)` with non-nullable outcome (the `Failed` state replaces the earlier nullable), and a named consumer in the plan.
2. **`src/Encina.Messaging/Inbox/InboxOrchestrator.cs`** (modify, `InboxErrorCodes` is declared in this file): add `public const string InProgress = "inbox.in_progress";` to `InboxErrorCodes`, and declare the new public static class `InboxErrorMetadataKeys` (the `Outcome` key, task 3) in the same file next to it, so it adds no file to the count.
3. The in-progress error is created with `EncinaErrors.Create(InboxErrorCodes.InProgress, ErrorCategory.Conflict, message, details: ...)` (ADR-062: the producer sets the category; `idempotency.in_progress` is already `Conflict`), with the remaining lease (minimum 1 second, computed from `TimeProvider`) as a `TimeSpan` under `EncinaErrorMetadataKeys.RetryAfter` and the typed marker `InboxErrorMetadataKeys.Outcome = "in_progress"` (Design Choice 4); the error message never contains the message id or the payload (AGENTS.md section 3). A unit test asserts `GetCategory` returns `Conflict`.
4. `src/Encina.Messaging/PublicAPI.Unshipped.txt`: add the new symbols (handed to `mechanical-fixer` at implementation).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina is pre-1.0, .NET 10 / C# 14, nullable enabled, no backward compatibility, ROP everywhere
(Either<EncinaError, T>). Issue #3440 adds an outcome contract to the inbox pipeline. Issue #2401 (claim and
lease on 10 providers) is a prerequisite and provides the claim result this plan consumes.
TASK: Create src/Encina.Messaging/Inbox/InboxOutcome.cs with the InboxOutcome enum described in the plan (and
InboxProcessResult<TResponse> only if the maintainer picked Design Choice 1 option A); add
InboxErrorCodes.InProgress ("inbox.in_progress") and the typed marker key; create the in-progress error with
ErrorCategory.Conflict and the remaining lease under EncinaErrorMetadataKeys.RetryAfter (ADR-062).
KEY RULES: XML docs on every public symbol; no [Obsolete]; no personal data or message ids in error
messages; update PublicAPI.Unshipped.txt; zero warnings.
REFERENCE FILES: src/Encina.Messaging/Inbox/InboxOrchestrator.cs (InboxErrorCodes),
src/Encina.Messaging/Inbox/IInboxMessage.cs, docs/architecture/adr/048-inbox-record-vs-business-transaction.md
```

</details>

### Phase 2: Orchestrator and Pipeline Behavior — derive the outcome, answer in-progress without running the handler

<details>
<summary><strong>Tasks</strong></summary>

1. **`InboxOrchestrator.ProcessAsync<TResponse>`** (modify; under Design Choice 1 option D the signature stays `ValueTask<Either<EncinaError, TResponse>>`, under option A it becomes `ValueTask<InboxProcessResult<TResponse>>` and the callers in the Research list change in the same pull request). Using the claim result of #2401 (`Acquired`, `AlreadyProcessed`, `InProgress` with lease expiry; the exact type belongs to that issue and is **a prerequisite note to re-check when pull request A starts**: this phase is specified against the shape described there and adapts to its final form):
   - claim acquired and the handler ran: outcome `Processed`, returning the handler's `Either` (a handler `Left` stays a cached business outcome per ADR-048; [#3441](https://github.com/dlrivada/Encina/issues/3441) may change how a retryable `Left` is cached, independent of this plan);
   - already processed: outcome `Replayed`, returning the deserialized cached response (today's `HandleExistingMessageAsync` branch);
   - held by another delivery: outcome `InProgress`, returning `Left(inbox.in_progress)` created per Phase 1 task 3; the handler is **not** invoked.
2. A store `Left` still fails the operation with that `Left`; the recorded outcome is `Failed` and nothing reports success (AGENTS.md section 3: errors are never swallowed); `Processed` is recorded only when the handler ran. The unit tests pin each case.
3. **`InboxPipelineBehavior<TRequest,TResponse>.ProcessIdempotentAsync`**: unchanged under option D (it already returns the orchestrator's `Either`); under option A it returns `.Result`.
4. Callers and tests of the orchestrator: unchanged under D; listed in Research for option A.
5. **Classification (ADR-029):** `inbox.in_progress` is non-retryable by Encina's own recoverability (the vendor retries): the default `IErrorClassifier` is extended so an outer retry behaviour does not immediately re-invoke the inbox, stay in progress and exhaust its retries or dead-letter the duplicate; a classifier unit test pins it in the same pull request.
6. Every outcome decision uses `TimeProvider` (already injected) for the lease comparison; no `DateTime.UtcNow`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Phase 1 created InboxOutcome. InboxOrchestrator.ProcessCoreAsync today reads the row, then runs the
handler for any unprocessed row (the defect of #2401). The claim API of #2401 is merged; re-read its claim result
type first and adapt to it.
TASK: In InboxOrchestrator.ProcessAsync derive Processed, Replayed, InProgress and Failed from the claim result and
the cached row and record them in one place; on InProgress do not call processCallback and return
Left(EncinaErrors.Create(InboxErrorCodes.InProgress, ErrorCategory.Conflict, ..., RetryAfter TimeSpan)). Keep the
return type Either unless the maintainer picked Design Choice 1 option A. Classify inbox.in_progress as
non-retryable in the default IErrorClassifier. Keep dead-letter capture and retry accounting as they are.
KEY RULES: TimeProvider only; a store Left fails the operation (never reports success); error messages carry no
message id, payload or tenant data; static analysis clean; keep CRAP <= 10 on every changed method by splitting
the outcome derivation into small methods.
REFERENCE FILES: src/Encina.Messaging/Inbox/InboxOrchestrator.cs, src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs,
tests/Encina.UnitTests/Messaging/Inbox/InboxOrchestratorTests.cs, tests/Encina.UnitTests/Messaging/Pipeline/InboxPipelineBehaviorTests.cs
```

</details>

### Phase 3: Configuration, DI & Registration — the HTTP option

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.AspNetCore/InboxInProgressResponse.cs`** (new): `public enum InboxInProgressResponse { Conflict409, Ok200 }`.
2. **`src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs`** (modify): `public InboxInProgressResponse InboxInProgressResponse { get; set; } = InboxInProgressResponse.Conflict409;` (default is the recommendation of Design Choice 2, pending the maintainer), with XML docs.
3. No new registration is needed: `AddEncinaAspNetCore(Action<EncinaAspNetCoreOptions>)` already registers the options; extend the existing DI test (`ValidateOnBuild`, `ValidateScopes`) to resolve the option with the new property (AGENTS.md section 3, registration completeness).
4. **`src/Encina.AspNetCore/InboxInProgressBody.cs`** (new): `public sealed record InboxInProgressBody(string Status, string ErrorCode)`, the body of the 200 answer.
5. `src/Encina.AspNetCore/PublicAPI.Unshipped.txt`: new enum, property and record.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: EncinaAspNetCoreOptions already holds IdempotencyKeyHeader and IncludeErrorMessageInProblemDetails.
TASK: Add the InboxInProgressResponse enum and the option with its default; extend the existing options/DI tests.
KEY RULES: pay-for-what-you-use (the option has a working default, nothing must be configured); XML docs;
PublicAPI.Unshipped.txt; no environment-dependent behavior.
REFERENCE FILES: src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs, src/Encina.AspNetCore/ServiceCollectionExtensions.cs
```

</details>

### Phase 4: HTTP Mapping — 409 with Retry-After or 200

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.AspNetCore/ProblemDetailsExtensions.cs`** (modify; **starts after #3419 S2 and H1 are merged**, Design Choice 4; if the central status table already replaced `MapErrorCodeToStatusCode`, this task edits that table's consumer instead and no code-text branch is written):
   - Status: the 409 comes from `ErrorCategory.Conflict` in the central table; **no branch on the string `inbox.in_progress` is added**. The 200 answer is chosen when `InboxInProgressResponse.Ok200` is set and the error carries the typed marker `InboxErrorMetadataKeys.Outcome == "in_progress"`; keep the methods small (a lookup, not another `if` chain) to hold CRAP at 10 or less.
   - `ToProblemDetails`: for the 409 of this error write `Retry-After` from the `TimeSpan` under `EncinaErrorMetadataKeys.RetryAfter` (whole seconds, minimum 1; the orchestrator already computed the remaining lease from `TimeProvider`, so the host needs no clock) and keep the `errorCode` extension; for the 200 answer return `Results.Ok(new InboxInProgressBody("in_progress", errorCode))` where `InboxInProgressBody` is a documented public sealed record in `Encina.AspNetCore` (no problem document, no message, no anonymous type).
   - `GetTitle`: 409 already maps to "Conflict".
2. An explicit `statusCode` argument still wins, which is the per-endpoint override (REQ-036 "configured" at endpoint level without a second API).
3. The error message is not exposed: unchanged `ShouldExposeErrorMessage` rule.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: ToProblemDetails honors an explicit statusCode. ADR-062: the host never parses the error code; the status
comes from the error's ErrorCategory. The in-progress duplicate arrives as Left created with ErrorCategory.Conflict,
the remaining lease as a TimeSpan under EncinaErrorMetadataKeys.RetryAfter and the typed marker
InboxErrorMetadataKeys.Outcome. The central status table of #3419 (S2, H1) is merged.
TASK: Answer 409 (from the category table) or 200 per EncinaAspNetCoreOptions.InboxInProgressResponse; write
Retry-After on the 409 from the RetryAfter TimeSpan; keep the explicit statusCode override; 200 returns the
documented InboxInProgressBody record, not a problem document.
KEY RULES: never branch on the string inbox.in_progress; never expose EncinaError.Message outside the Development
opt-in; no logging of the error message; fail closed is not applicable here (this is a conflict signal, not a gate);
CRAP <= 10 on each changed method.
REFERENCE FILES: src/Encina.AspNetCore/ProblemDetailsExtensions.cs, tests/Encina.UnitTests/AspNetCore (existing ProblemDetails tests)
```

</details>

### Phase 5: Cross-Cutting Integration

<details>
<summary><strong>Tasks</strong></summary>

1. Tenancy (function 10): the outcome is keyed by whatever key [#3437](https://github.com/dlrivada/Encina/issues/3437) settles. **#3437 (an open bug: tenant B already receives tenant A's cached response) merges before pull request A**, because `InProgress` is a second cross-tenant signal (tenant B reusing tenant A's key would be answered 409 with a `Retry-After`); pull request A therefore includes the two-tenant unit test asserting that the second tenant is never reported `Replayed` or `InProgress` on the first tenant's row, and it is green. If the maintainer decides to merge A before #3437, the pull request records that the signal is exactly as exposed as the existing cache leak and is closed by #3437.
2. Idempotency (function 9): the contract is the feature itself; the Inbox remains opt-in (`config.UseInbox = true`), and this plan adds nothing when the Inbox is off.
3. Deferred items are listed in the matrix with their issue file (`inbox-stuck-claims-health-check`, `inbox-outcome-resilience-retry-after`, `inbox-in-progress-azure-aws-hosts` for the 200 option and `Retry-After` in `Encina.AzureFunctions` and `Encina.AwsLambda`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: Phases 1-4 added the outcome, the orchestrator logic and the HTTP mapping.
TASK: Verify the cross-cutting rows of the plan's matrix are implemented or deferred with an issue; add the
two-tenant regression test once the key shape of #3437 is merged; do not add a store method.
KEY RULES: AGENTS.md section 6 (record Integrate/Defer/N-A per function); module isolation and audit stay N/A as
in the matrix; do not widen scope.
REFERENCE FILES: AGENTS.md section 6, src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs, artifacts/issues/
```

</details>

### Phase 6: Observability — counter, tag and log entry

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/Diagnostics/InboxOutcomeMetrics.cs`** (new, internal static, same shape as `MessagePurgeMetrics` but following ADR-054): counter `encina.messaging.inbox.outcome` (no `_total` suffix, ADR-054 point 4) on a meter named `Encina.Messaging` (ADR-054 D3: meters take their package name; the plain `"Encina"` meter that `MessagePurgeMetrics` still uses is one of the 27 being renamed, so this plan does not add to it; the implementation checks which meter the inbox code already uses and registers the name with `Encina.OpenTelemetry`), tag `outcome` with the four enum names only (bounded, no message id), and the tag `encina.tenant_id` when the `MultiTenancyMarker` is registered and no tenant tag when it is not (ADR-054 D4).
2. **`src/Encina.Messaging/Diagnostics/InboxActivitySource.cs`** (modify): `CompleteWithOutcome(Activity?, InboxOutcome)` sets the tag `inbox.outcome`; the existing `inbox.message_id` tag stays as it is today.
3. **`src/Encina.Messaging/Diagnostics/InboxOutcomeLog.cs`** (new, `[LoggerMessage]` source generator): EventId **5800** `InProgressDuplicate` (Information, message type and correlation id only) and **5801** `OutcomeRecorded` (Debug); 5802 to 5849 stay free for the family. Register `MessagingInboxIdempotency = (5800, 5849)` in `src/Encina/Diagnostics/EventIdRanges.cs` first (ADR-021), add `nameof(EventIdRanges.MessagingInboxIdempotency)` to the `Encina.Messaging` entry of `EncinaEventIdAllocationTests.AssemblyRanges`, add the field to `src/Encina/PublicAPI.Unshipped.txt` and run the architecture tests.
4. The orchestrator calls the metrics and log from one place (the outcome derivation), so the three paths cannot drift.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: ADR-021: register the EventId range first. Messaging 2800-2999 is exhausted (the plans of #718, #1200,
#1251 and #583 reserved 2961-2992); 5500-6999 was free on 2026-10-10 and the family's assigned block 5800-5849 (plan-batch registry, 2026-11-11) is still unused in EventIdRanges.cs.
TASK: Register MessagingInboxIdempotency (5800, 5849); write InboxOutcomeLog (5800, 5801), InboxOutcomeMetrics and
the activity tag; map the range to Encina.Messaging in EncinaEventIdAllocationTests; add the PublicAPI line.
KEY RULES: dimensions are bounded; the tenant id is a tag only when multi-tenancy is on (ADR-054 D4) and never a
log argument; EncinaError.Message never reaches a log, tag or
metric; exceptions go through ForLogging(); pack EventIds sequentially; re-check that 5800 is still free on the
branch before registering and take the next free block if not.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Messaging/Diagnostics/MessagePurgeMetrics.cs,
src/Encina.Messaging/Diagnostics/InboxActivitySource.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 7: Testing — 6 Test Types

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/Messaging/Inbox/InboxOrchestratorTests.cs`, `.../Pipeline/InboxPipelineBehaviorTests.cs`, new `InboxOutcomeMetricsTests.cs`, new `AspNetCore/InboxInProgressProblemDetailsTests.cs`, a classifier test): each outcome; the handler is invoked once when a second delivery arrives while the first is in progress; the in-progress error has category `Conflict` and a `RetryAfter` `TimeSpan`; the default classifier does not retry it; 409 with `Retry-After`; 200 with `InboxInProgressBody`; explicit `statusCode` override; lease expiry makes the next delivery `Processed` again; store `Left` is not reported as success and is recorded as `Failed`; two tenants and one key; `FakeTimeProvider` for every time case.
2. **Guard** (`tests/Encina.GuardTests/Messaging/Inbox/InboxGuardTests.cs`): the new mapper parameters throw `ArgumentNullException` on null (and `ProcessAsync` if its signature changed).
3. **Contract**: in PR A (new `tests/Encina.ContractTests/.../InboxOutcomeContractTests.cs`) `InProgress` always carries `inbox.in_progress` with category `Conflict`, `Replayed` and `Processed` never do, for every `IInboxStore` implementation used by the harness; the assertion that the HTTP answer for the error is the same for every store needs the mapping of PR B and lives there (new `InboxInProgressHttpContractTests.cs`).
4. **Property**: not required; the outcome is a three-way decision with no complex invariant beyond the unit and contract cases (AGENTS.md section 9: property tests only if the logic is complex).
5. **Integration** (new `tests/Encina.IntegrationTests/Messaging/InboxInFlightRedeliveryScenario.cs`, using `InboxPipelineHarnesses.cs`): a slow handler, a second delivery during it, one handler run, the second caller sees `inbox.in_progress`, then a third delivery after completion sees `Replayed`; runs on all 10 providers through the `[Collection("<Family>-<Database>")]` fixtures with `ClearAllDataAsync()` in `InitializeAsync()`. These share the concurrency fixtures of [#2401](https://github.com/dlrivada/Encina/issues/2401).
6. **Load and Benchmark**: justification files only (the outcome adds one branch to a path whose cost is the database round trip; concurrency correctness is proven by the integration scenario). Written as `.md` under the load and benchmark test projects.
7. Coverage: set per-file targets with one-sentence justifications in `.github/coverage-manifest/Encina.Messaging.json` and `Encina.AspNetCore.json` for every touched file (targets never go down; `mechanical-fixer` writes them in the PR that touches the file); measure each flag locally before the pull request and report the CRAP table. The outcome derivation is pure and fully unit-testable, so the proposed targets are demanding:

   | File | Unit | Guard | Contract | Property | Integration | Justification |
   |------|-----:|------:|---------:|---------:|------------:|---------------|
   | `Inbox/InboxOrchestrator.cs` (outcome derivation methods) | 95 | 100 | 85 | 0 | 80 | Four-way decision split into small methods over fakes; the in-flight scenario on 10 providers proves it end to end; property is not needed (no complex invariant) |
   | `Inbox/InboxOutcome.cs` | 100 | 0 | 0 | 0 | 0 | An enum; a value test covers it; no parameters to guard |
   | `Diagnostics/InboxOutcomeMetrics.cs`, `InboxOutcomeLog.cs`, `InboxActivitySource.cs` (changed members) | 95 | 0 | 0 | 0 | 0 | Instrument and log wrappers asserted with a metric listener and log capture; internal |
   | `AspNetCore/ProblemDetailsExtensions.cs` (changed members) | 95 | 100 | 90 | 0 | 0 | Pure mapping asserted for 409, `Retry-After`, 200 and the override |
   | `AspNetCore/EncinaAspNetCoreOptions.cs`, `InboxInProgressResponse.cs`, `InboxInProgressBody.cs` | 100 | 0 | 0 | 0 | 0 | Option, enum and record; resolved in the DI test |

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT: Per-flag coverage (unit, guard, contract, property, integration) is measured independently against
.github/coverage-manifest/{Package}.json. Tests must execute real package code. Shouldly through
Encina.Testing.Shouldly; FakeTimeProvider for time; database tests use shared [Collection] fixtures.
TASK: Implement the tests listed in the Tasks block; write the load and benchmark justification files; set the
manifest targets for the touched files; run each flag's test project with coverage and the crap-gate table.
KEY RULES: no Thread.Sleep; one behavior per test; no reflection-only tests; integration tests never create a
per-class fixture; never lower a manifest target; every database provider is covered (10).
REFERENCE FILES: tests/Encina.UnitTests/Messaging/Inbox/InboxOrchestratorTests.cs,
tests/Encina.IntegrationTests/Messaging/InboxPipelineScenario.cs, tests/Encina.IntegrationTests/Messaging/InboxPipelineHarnesses.cs,
tests/Encina.ContractTests (InboxPipelineBehaviorContractTests.cs), .github/coverage-manifest/Encina.Messaging.json
```

</details>

### Phase 8: Documentation & Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on every new public symbol (`InboxOutcome`, `InboxErrorCodes.InProgress`, `InboxErrorMetadataKeys`, `InboxInProgressResponse`, `InboxInProgressBody`, the option; `InboxProcessResult<T>` only under Design Choice 1 option A) with `<example>` for the HTTP mapping.
2. `changelog.d/3440-inbox-in-progress-outcome.added.md` (never edit `CHANGELOG.md`), handed to `mechanical-fixer`.
3. `docs/messaging/inbox.md`: the three outcomes, the 409 or 200 setting, the per-endpoint override and the lease behavior; drafted by `docs-writer`. The wording of the exactly-once claim is an acceptance item of [#2401](https://github.com/dlrivada/Encina/issues/2401) and is not changed here.
4. `docs/INVENTORY.md`: the new files. ADR: no new ADR; the outcome contract is recorded in the ADR that [#2401](https://github.com/dlrivada/Encina/issues/2401) writes for the claim and lease (if it writes none, this feature writes one; the ADR number is assigned when the ADR pull request opens), and the ADR-062 extension of Design Choice 4 (Retry-After on this 409 and the 200 option) is recorded in ADR-062 by the #3419 work. `ROADMAP.md`: SPEC-002 REQ-036 progress line only if the milestone text changes.
5. `PublicAPI.Unshipped.txt` for `Encina`, `Encina.Messaging` and `Encina.AspNetCore` (via `mechanical-fixer`).
6. Build verification: `dotnet build --configuration Release` with 0 errors and 0 warnings; test verification: `dotnet test`, every coverage flag at its own manifest target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT: Phases 1-7 are merged. Documentation follows the encina-docs skill (one Diataxis quadrant per page;
figures cited with covref markers, never typed).
TASK: Finish the XML docs, the changelog fragment, docs/messaging/inbox.md and docs/INVENTORY.md; verify the
PublicAPI files; run build and tests.
KEY RULES: English only; no hand-typed coverage figures; the exactly-once wording stays with #2401; do not name
any private client application (write "the reference application" if an example needs one).
REFERENCE FILES: docs/messaging/inbox.md, docs/architecture/adr/048-inbox-record-vs-business-transaction.md,
changelog.d/README.md, .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | How this feature meets it |
|----------|-------------|---------------------------|
| SPEC-002 REQ-036 | An in-progress duplicate is answered with 200 or 409 | The configurable mapping of Phase 4 |
| SPEC-002 AC-036 | A duplicate arriving while the first delivery is in progress is answered with the configured 200 or 409 and processed once | The in-flight redelivery scenario of Phase 7 on 10 providers (processed once is delivered by #2401) |
| ADR-001, ADR-006 | Operations return `Either<EncinaError, T>` | In-progress is a typed `Left` |
| ADR-048 | Inbox record versus business transaction | The outcome never changes where the processed mark commits |
| ADR-062 | Error category set where the error is created; hosts never parse the code; `Retry-After` only with 429/503 today; `idempotency.in_progress` is `Conflict` | `inbox.in_progress` is `Conflict`; the `Retry-After` on 409 and the 200 option are the extension of Design Choice 4 |
| ADR-054 D3, D4 | Package-named meter; no `_total`; tenant tag when tenancy is on | The outcome counter (Phase 6) |
| ADR-029 | Error classification for retry | `inbox.in_progress` is not retried by Encina |
| ADR-021 | EventId ranges are registered before use | `MessagingInboxIdempotency` (5800-5849) |
| IETF idempotency-key header draft | An in-flight request with the same key is answered 409 | Default of Design Choice 2 (pending the maintainer) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| InboxOrchestrator | src/Encina.Messaging/Inbox/InboxOrchestrator.cs | decides processed / replayed / in-progress from the store and returns the outcome |
| InboxPipelineBehavior<TRequest,TResponse> | src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs | unchanged under option D: returns the orchestrator's Either, in which in-progress is a Left with code inbox.in_progress (category Conflict) |
| IInboxStore / IInboxMessage | src/Encina.Messaging/Inbox/IInboxStore.cs, IInboxMessage.cs | claim result consumed here; claim and lease implemented by issue 2401 on 10 providers |
| InboxErrorCodes | src/Encina.Messaging/Inbox/InboxOrchestrator.cs | new constant InProgress = "inbox.in_progress" |
| ProblemDetailsExtensions.ToProblemDetails | src/Encina.AspNetCore/ProblemDetailsExtensions.cs | existing mapper of EncinaError to HTTP; gets the in-progress mapping and the Retry-After header |
| EncinaAspNetCoreOptions | src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs | gets the option for the in-progress answer (409 or 200) |
| MessagePurgeMetrics | src/Encina.Messaging/Diagnostics/MessagePurgeMetrics.cs | pattern for the new InboxOutcomeMetrics counter on the Encina meter |
| InboxActivitySource | src/Encina.Messaging/Diagnostics/InboxActivitySource.cs | gets the inbox.outcome tag |
| EventIdRanges | src/Encina/Diagnostics/EventIdRanges.cs | new range MessagingInboxIdempotency 5800-5849 |
| Inbox test harnesses | tests/Encina.IntegrationTests/Messaging/InboxPipelineHarnesses.cs, InboxPipelineScenario.cs | provider matrix of the in-flight redelivery scenario |

Callers of `InboxOrchestrator` to update **only under Design Choice 1 option A** (when `ProcessAsync` changes its return type; under the recommended option D none changes, which keeps pull request A building on its own): `InboxPipelineBehavior`, `MessagingServiceCollectionExtensions` (registration only), `tests/Encina.UnitTests/Messaging/Inbox/InboxOrchestratorTests.cs`, `InboxOrchestratorRetryTests.cs`, `tests/Encina.UnitTests/Messaging/Pipeline/InboxPipelineBehaviorTests.cs`, `tests/Encina.GuardTests/Messaging/Inbox/InboxGuardTests.cs`, `tests/Encina.IntegrationTests/Messaging/InboxRetryScenario.cs`, `DeadLetter/DeadLetterSourcesEndToEndScenario.cs`, `tests/Encina.UnitTests/Messaging/DeadLetter/DeadLetterSourcesCaptureTests.cs`.

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` | **5800-5801** used of the new `MessagingInboxIdempotency` = (5800, 5849) | 5800 `InProgressDuplicate`, 5801 `OutcomeRecorded`; 5802-5849 stay free for the family. The range is the family's block for inbox idempotency; the other plans of the family take the next free numbers of it (5805 onward) when they land. `Messaging` 2800-2999 has no free packed block left (2961-2992 are reserved by other plans) |
| `Encina.AspNetCore` | none | The mapper is a pure function; it logs nothing |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production source, new | 5 | InboxOutcome.cs, InboxOutcomeMetrics.cs, InboxOutcomeLog.cs, InboxInProgressResponse.cs, InboxInProgressBody.cs |
| Production source, modified | 5 | InboxOrchestrator.cs, InboxActivitySource.cs, EventIdRanges.cs, ProblemDetailsExtensions.cs, EncinaAspNetCoreOptions.cs (the default classifier file is a sixth, in PR A) |
| Metadata | 5 | PublicAPI.Unshipped.txt (Encina, Encina.Messaging, Encina.AspNetCore) and the two coverage manifests |
| Tests | 10 | orchestrator unit, metrics unit, classifier unit, mapping unit, guard, contract (PR A), HTTP contract (PR B), integration scenario, EventId allocation test map, load and benchmark justification files (2) |
| Documentation | 3 | changelog fragment, docs/messaging/inbox.md, docs/INVENTORY.md |
| **Total** | **~28** | Three pull requests, each within 15 files and 1,500 lines (counts below include tests and metadata) |

### Pull request split

Under the recommended Design Choice 1 option D nothing existing changes signature, so every pull request builds alone. If the maintainer picks option A, the callers listed above and `InboxGuardTests` move into PR A and PR A is recounted (it would exceed 15 files and need a split of its own).

| PR | Content | Files (approx.) |
|----|---------|-----------------|
| A | Phases 1, 2, 6 and Phase 5 task 1: `InboxOutcome`, `InboxOrchestrator`, `InboxActivitySource`, `InboxOutcomeMetrics`, `InboxOutcomeLog`, `EventIdRanges.cs`, the classifier file (7 production), PublicAPI of `Encina` and `Encina.Messaging`, Messaging manifest (3), orchestrator unit (with the two-tenant case), metrics-and-classifier unit (one file), EventId allocation test map, contract, integration scenario on the 10 providers (5 tests) | 15 |
| B | Phases 3, 4: 4 production files (`InboxInProgressResponse`, `InboxInProgressBody`, options, `ProblemDetailsExtensions`), PublicAPI and manifest of `Encina.AspNetCore`, mapping unit test, guard test, HTTP contract test | 9 |
| C | Phase 7 load and benchmark justifications; Phase 8 documentation and changelog | 6 |

Order: A requires [#2401](https://github.com/dlrivada/Encina/issues/2401) **and** [#3437](https://github.com/dlrivada/Encina/issues/3437) merged (Phase 5 task 1); B starts after #3419 S2 and H1 (Design Choice 4) and after A (it reads the error's category and details); C follows A and B.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
Encina is pre-1.0, .NET 10 / C# 14, nullable enabled, no backward compatibility (no [Obsolete], no aliases).
Issue #3440 adds an explicit outcome (Processed, Replayed, InProgress) to the inbox pipeline and lets the HTTP
layer answer an in-progress duplicate with 409 (Retry-After) or 200, as configured (SPEC-002 REQ-036/AC-036).
Prerequisite: issue #2401 (atomic claim and lease on the 10 database providers) is merged; issue #3437 (key
shape) and #3441 (handler Left) are independent and may land before or after.

IMPLEMENTATION OVERVIEW:
1. Encina.Messaging/Inbox: InboxOutcome enum, InboxErrorCodes.InProgress (created with ErrorCategory.Conflict and the
   remaining lease under EncinaErrorMetadataKeys.RetryAfter, ADR-062); InboxOrchestrator derives and records the
   outcome and keeps returning Either (Design Choice 1 option D, pending the maintainer); the default classifier
   does not retry inbox.in_progress.
2. Encina.AspNetCore (after #3419 S2/H1): InboxInProgressResponse enum + EncinaAspNetCoreOptions property +
   InboxInProgressBody; the 409 comes from the category table, Retry-After from the RetryAfter TimeSpan, the 200
   option is recognised by a typed details marker, never by the code text; explicit statusCode still wins.
3. Observability: InboxOutcomeMetrics counter, inbox.outcome activity tag, InboxOutcomeLog (EventIds 5800, 5801)
   with the range MessagingInboxIdempotency (5800, 5849) registered first in EventIdRanges.cs.
4. Tests: unit, guard, contract, integration on all 10 providers (in-flight redelivery); load and benchmark
   justification files; per-flag manifest targets for each touched file.
5. Docs: XML docs, changelog fragment, docs/messaging/inbox.md, docs/INVENTORY.md, PublicAPI files.

KEY PATTERNS:
- ROP: an in-progress duplicate is Left(inbox.in_progress); a store Left fails the operation, never success.
- TimeProvider for every time decision; no DateTime.UtcNow.
- EncinaError.Message never reaches logs, tags, metrics or the client (outside the Development opt-in).
- Bounded metric dimensions; [LoggerMessage] source generator; EventIds packed in a registered range.
- Provider coherence: no provider-specific code in Encina.Messaging; the 10 providers are proven by tests.
- CRAP <= 10 on every changed method; targets never go down; one pull request per phase group (A, B, C).

REFERENCE FILES:
docs/plans/inbox-in-progress-outcome-implementation-plan-3440.md, src/Encina.Messaging/Inbox/InboxOrchestrator.cs,
src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs, src/Encina.AspNetCore/ProblemDetailsExtensions.cs,
src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs, src/Encina/Diagnostics/EventIdRanges.cs,
src/Encina.Messaging/Diagnostics/MessagePurgeMetrics.cs, tests/Encina.IntegrationTests/Messaging/InboxPipelineHarnesses.cs,
docs/architecture/adr/048-inbox-record-vs-business-transaction.md, docs/specifications/SPEC-002 (REQ-036, AC-036)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The replayed answer is the inbox's own cached response (existing), not an `ICacheProvider` concern |
| 2 | OpenTelemetry | ✅ | `encina.messaging.inbox.outcome` counter (no `_total`) on the package-named `Encina.Messaging` meter, with the tenant tag when tenancy is on (ADR-054 D3 and D4), and the `inbox.outcome` tag on the inbox activity (Phase 6); the meter name is registered with `Encina.OpenTelemetry` |
| 3 | Structured Logging | ✅ | `InboxOutcomeLog` EventIds 5800 and 5801 in the new `MessagingInboxIdempotency` range (Phase 6) |
| 4 | Health Checks | ⏭️ | A check for rows stuck past their lease needs a new store query on 10 providers and the row state of #2401; follow-up issue file `artifacts/issues/inbox-stuck-claims-health-check.md` (to open after the plan merges) |
| 5 | Validation | ❌ | No new input: the idempotency key is validated by the existing `ValidateMessageId` |
| 6 | Resilience | ⏭️ | The vendor's retry schedule is outside Encina; honoring `Retry-After` on the outbound side belongs to the outbound resilience work; no issue needed yet, noted in the inbox guide |
| 7 | Distributed Locks | ❌ | The claim and lease of #2401 replace a lock per message (settled by elimination on 2026-10-17) |
| 8 | Transactions | ❌ | The processed mark keeps its ADR-048 behavior; the outcome is computed before and after it and changes no transaction boundary |
| 9 | Idempotency | ✅ | This is the idempotency outcome contract itself (Phases 1-4) |
| 10 | Multi-Tenancy | ✅ | The two-tenant regression test of Phase 5 (key shape owned by #3437); the outcome carries no tenant data |
| 11 | Module Isolation | ❌ | The inbox is not module-scoped and no `ModuleId` participates in the key |
| 12 | Audit Trail | ❌ | An in-progress answer is a transient flow-control signal, not a state change or a compliance event; telemetry covers it |

---

## Prerequisites & Dependencies

| Issue | Relation | Notes |
|-------|----------|-------|
| [#2401](https://github.com/dlrivada/Encina/issues/2401) | Blocks this feature | Atomic claim and lease on the 10 providers; supplies the claim result, the row state and the lease expiry; its wording acceptance item (no exactly-once claim until it ships) stays with it |
| [#3437](https://github.com/dlrivada/Encina/issues/3437) | Independent, shares the orchestrator | Key shape (tenant, source); this plan is key-agnostic; the Phase 5 test follows it |
| [#3441](https://github.com/dlrivada/Encina/issues/3441) | Independent, shares the orchestrator | Handler `Left` permanence; both change `ProcessAndCacheResponseAsync`-adjacent code, so the later pull request rebases |
| [#3452](https://github.com/dlrivada/Encina/issues/3452) | Parent EPIC | Inbox claim and tenant-scoped idempotency key |
| [#734](https://github.com/dlrivada/Encina/issues/734) | Consumer | The webhook idempotency work that exposes the 200 or 409 setting to endpoints |

---

## Next Steps

1. Wait for [#2401](https://github.com/dlrivada/Encina/issues/2401) before opening PR A; re-read its claim result type and adapt Phase 2 to it.
2. At pick-up: re-check the Design Choices against today's code, ask the maintainer the Dilemmas 1, 2 and 4 (outcome carrier, default status and its interaction with the lease, the ADR-062 extension for `Retry-After` and the 200 option; the lease-expiry behavior of Design Choice 3 is asked once, in #2401's register) and record the answers in `## Maintainer Decisions`; then run `check-plan.ps1` without `-Draft`.
3. Open the deferred follow-ups as issues: `artifacts/issues/inbox-stuck-claims-health-check.md` and `artifacts/issues/inbox-in-progress-azure-aws-hosts.md` (the two other hosts of Design Choice 4).
4. Confirm the `MessagingInboxIdempotency` range (5800-5849) is still free when PR A starts; coordinate the next free numbers with the plans of #3437, #3441 and #2401.
5. Measure per-flag coverage and the CRAP table for the touched files before each pull request.
