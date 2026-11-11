# Implementation Plan: `Encina.Messaging` Inbox outcome contract for an in-progress duplicate (200 or 409)

> **Issue**: [#3440](https://github.com/dlrivada/Encina/issues/3440)
> **Type**: Feature
> **Complexity**: Medium (8 phases, no new per-provider code, tests on all 10 database providers, ~28 files in 3 pull requests)
> **Estimated Scope**: ~700-900 lines of production code + ~900-1,100 lines of tests and documentation

---

## Summary

The inbox pipeline can tell a caller only "a response" or "an error". A vendor that times out and redelivers a webhook while the first handler is still running therefore gets the same answer as a first delivery or, today, runs the handler a second time (`handlerRuns=2`, reproduced on [#2401](https://github.com/dlrivada/Encina/issues/2401) with the real `InboxPipelineBehavior` and `InboxOrchestrator`). SPEC-002 REQ-036 and AC-036 require that "a duplicate arriving while the first delivery is still in progress is answered with the configured 200 or 409 and processed once". [#2401](https://github.com/dlrivada/Encina/issues/2401) settled the mechanism (atomic claim plus a lease for crashed handlers, on the 10 database providers) but not the contract that tells the caller which of the three situations it is in. This feature adds that contract.

What it implements:

- `InboxOutcome` (`Processed`, `Replayed`, `InProgress`) and `InboxProcessResult<TResponse>` (outcome, nullable when a store failure stops the flow before any outcome, plus the `Either`), the value `InboxOrchestrator.ProcessAsync` returns instead of a bare `Either<EncinaError, TResponse>` (pre-1.0: the old signature is replaced, not kept beside the new one).
- A typed `Left` with the new code `inbox.in_progress` and the lease expiry as error metadata, so the pipeline (which must keep returning `Either<EncinaError, TResponse>`) carries the in-progress case to the HTTP layer without a new return type.
- In `Encina.AspNetCore`: the mapping of that code to 409 with `Retry-After`, or to 200, as configured (`EncinaAspNetCoreOptions.InboxInProgressResponse`, default pending the maintainer), and a per-endpoint override through the existing `statusCode` argument of `ToProblemDetails`.
- Observability of the three outcomes (a counter, a tag on the existing inbox activity, a log entry for the in-progress case).

Standards and specifications: SPEC-002 REQ-036 and AC-036; ADR-001 and ADR-006 (Railway Oriented Programming); ADR-048 (inbox record versus business transaction); the IETF idempotency-key draft (an in-flight request with the same key is answered 409).

Estimated scope: 28 files (4 new and 6 modified production files, 5 metadata files, 10 test files, 3 documentation files), about 1,600-2,000 changed lines, shipped as three pull requests (see "Pull request split").

Affected packages: `Encina.Messaging` (outcome type, orchestrator, behavior, diagnostics), `Encina.AspNetCore` (mapping and option), `Encina` (EventId range registry). No provider package changes: the outcome is derived from the claim result of [#2401](https://github.com/dlrivada/Encina/issues/2401) inside the shared orchestrator.

Provider category: Database (10), but only through tests. The feature adds no SQL and no store method, so AGENTS.md section 5 is met by running the in-flight redelivery scenario on every provider (Phase 7), not by new provider implementations.

---

## Design Choices

<details>
<summary><strong>1. Outcome carrier — typed result from the orchestrator, typed <code>Left</code> through the pipeline</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `InboxProcessResult<T>` (outcome + `Either`) from the orchestrator; the behavior maps `InProgress` to a `Left` with code `inbox.in_progress`** | No change to `IPipelineBehavior` or `IEncina.Send`; the HTTP layer already maps `EncinaError` codes; the outcome stays inspectable in unit tests and telemetry | The `Replayed` versus `Processed` distinction is visible to the orchestrator caller and to telemetry only, not to the HTTP caller |
| **B) Keep `ProcessAsync` as it is and add `ProcessWithOutcomeAsync`** | Existing callers untouched | Two entry points for one operation; AGENTS.md section 3 forbids compatibility layers pre-1.0 |
| **C) An ambient accessor (`AsyncLocal<InboxOutcome>`) read by the HTTP layer** | The HTTP layer sees all three outcomes | Hidden state across an async pipeline, hard to test, breaks when the response is produced outside the request flow |

### Chosen Option: **A — `InboxProcessResult<T>` from the orchestrator, typed `Left` through the pipeline**

### Rationale

- ADR-001 and ADR-006 make `Either<EncinaError, T>` the only way an operation reports a non-success; an in-progress duplicate is a retryable condition of the request, not a response.
- The issue's own wording ("instead of, or in addition to") is resolved by AGENTS.md section 3: no compatibility layer, so `ProcessAsync` changes its return type and its callers (`InboxPipelineBehavior` and the tests listed in the Research section) are updated in the same pull request.
- A replay is a success for the HTTP caller (the cached response), so nothing in the HTTP layer needs to tell `Replayed` from `Processed`; telemetry does, through the counter of Phase 6.
- **Class:** Settled
- **Settled by:** ADR-001, ADR-006 and AGENTS.md section 3 (Railway Oriented Programming; no compatibility layers or aliases pre-1.0)

</details>

<details>
<summary><strong>2. Default HTTP answer for an in-progress duplicate — 409 with Retry-After, or 200</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) 409 Conflict with `Retry-After` (lease remaining), 200 selectable by option** | An unambiguous in-flight signal; the IETF idempotency-key draft and payment APIs that dedupe by key answer a concurrent same-key request with 409; the lease expiry gives a truthful `Retry-After` | A vendor that treats every non-2xx as a failed delivery may count it against the endpoint or give up |
| **B) 200 OK with a body that says "in progress", 409 selectable by option** | Compatible with vendors that retry or disable the endpoint on any non-2xx; the vendor keeps its normal retry schedule | Hides an in-flight state behind a success status; a stuck handler is harder to notice from the sender side |
| **C) No default: the option is required and startup validation fails when it is not set** | Forces the decision per application | Breaks "pay for what you use" (a working inbox would need an HTTP setting) and AddEncinaAspNetCore without options stops working |

### Chosen Option: **A — 409 Conflict with Retry-After by default (recommended, pending the maintainer)**

### Rationale

- REQ-036 fixes only that the answer is "200 or 409" and configurable; which one is the default depends on the vendors the application receives from. 409 wins for a first-party client or a vendor that retries on 4xx/5xx; 200 wins for a vendor that stops delivering or flags the endpoint unhealthy after non-2xx answers.
- The option exists in both cases, so the choice changes one default value, not the design; the recommendation follows the IETF draft and the key-deduplicating payment APIs.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Lease expiry of a crashed first handler — re-run within MaxRetries, or dead-letter</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The next delivery after the lease expires re-claims the row and re-runs the handler, counting the lost attempt against `MaxRetries`** | At-least-once for idempotent handlers; reuses `RetryCount` and the existing dead-letter capture once the retries are used up | A handler with irreversible side effects (a charge) may run twice if it crashed after the effect and before `MarkAsProcessedAsync` |
| **B) A row whose lease expired is dead-lettered and never re-run automatically** | No second execution of a handler that may have had an effect | Needs an operator or a replay tool for every crash; a short outage dead-letters healthy messages |
| **C) Configurable per request type** | Fits both kinds of handler | More configuration and a second code path to test on 10 providers |

### Chosen Option: **A — Re-run within MaxRetries, then the existing dead-letter path (recommended, pending the maintainer)**

### Rationale

- The behavior of an expired lease is part of the claim and lease mechanism that [#2401](https://github.com/dlrivada/Encina/issues/2401) implements; this plan only needs the answer to know what the caller sees after expiry (an `InProgress` outcome ends when the lease ends, then the delivery is `Processed` again).
- A wins when handlers are idempotent or transactional with the inbox row (the ADR-048 model, where the processed mark commits with the business effect); B wins for handlers with irreversible external effects and no idempotency of their own. The Inbox's contract is idempotent processing of idempotent-by-construction handlers, which favors A as the default.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Where the status mapping lives — the existing <code>ToProblemDetails</code> mapper</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extend `MapErrorCodeToStatusCode` and `ToProblemDetails` in `Encina.AspNetCore`** | One mapper for every Encina error; the per-endpoint override already exists (`statusCode` argument); no new public entry point to learn | The 200 answer is not a problem document, so it needs one small branch before the problem is built |
| **B) A new `InboxOutcomeResult` type with its own `IResult`** | Full control of body and headers for both answers | A second mapper beside `ToProblemDetails`; endpoints must pick the right one |
| **C) A middleware that rewrites responses by error code** | Invisible to endpoint code | Hidden behavior; middleware cannot see the typed `Left` of a minimal API handler |

### Chosen Option: **A — Extend the existing mapper**

### Rationale

- `ProblemDetailsExtensions.cs` already maps `EncinaError` codes to 400, 401, 403, 404, 409 and 500, adds the `errorCode` extension and honors an explicit `statusCode`; the in-progress code joins that table with its own branch and a `Retry-After` header from the error metadata.
- The error message is still not exposed (AGENTS.md section 3: `EncinaError.Message` never reaches the client unless the Development-only option is on); the body carries the code only.
- **Class:** Trivial
- **Settled by:** AGENTS.md section 3 (Provider coherence: abstractions in `Encina.Messaging`, HTTP concerns in `Encina.AspNetCore`) and the existing mapper `src/Encina.AspNetCore/ProblemDetailsExtensions.cs` (the established convention for every Encina error)

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

### Phase 1: Core Models — `InboxOutcome`, `InboxProcessResult<T>` and the error code

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/Inbox/InboxOutcome.cs`** (new), namespace `Encina.Messaging.Inbox`:
   - `public enum InboxOutcome { Processed, Replayed, InProgress }` with XML docs: `Processed` = this delivery ran the handler; `Replayed` = the answer is the cached response of an earlier delivery; `InProgress` = another delivery holds the claim and its lease has not expired.
   - `public readonly record struct InboxProcessResult<TResponse>(InboxOutcome? Outcome, Either<EncinaError, TResponse> Result)` with the `Left` of `InProgress` always carrying `InboxErrorCodes.InProgress`, and a static `Failed(EncinaError)` factory for a store failure (`Outcome` null: no outcome was reached).
2. **`src/Encina.Messaging/Inbox/InboxOrchestrator.cs`** (modify, `InboxErrorCodes` is declared in this file): add `public const string InProgress = "inbox.in_progress";` to `InboxErrorCodes`.
3. Metadata key for the lease expiry on the error: `inbox.lease_expires_at_utc` (ISO 8601 `O`), set through `EncinaErrors.Create(code, message, metadata)`; the error message never contains the message id or the payload (AGENTS.md section 3).
4. `src/Encina.Messaging/PublicAPI.Unshipped.txt`: add the new symbols (handed to `mechanical-fixer` at implementation).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina is pre-1.0, .NET 10 / C# 14, nullable enabled, no backward compatibility, ROP everywhere
(Either<EncinaError, T>). Issue #3440 adds an outcome contract to the inbox pipeline. Issue #2401 (claim and
lease on 10 providers) is a prerequisite and provides the claim result this plan consumes.
TASK: Create src/Encina.Messaging/Inbox/InboxOutcome.cs with the InboxOutcome enum and the
InboxProcessResult<TResponse> readonly record struct described in the plan; add InboxErrorCodes.InProgress
("inbox.in_progress") and the metadata key constant for the lease expiry.
KEY RULES: XML docs on every public symbol; no [Obsolete]; no personal data or message ids in error
messages; update PublicAPI.Unshipped.txt; zero warnings.
REFERENCE FILES: src/Encina.Messaging/Inbox/InboxOrchestrator.cs (InboxErrorCodes),
src/Encina.Messaging/Inbox/IInboxMessage.cs, docs/architecture/adr/048-inbox-record-vs-business-transaction.md
```

</details>

### Phase 2: Orchestrator and Pipeline Behavior — derive the outcome, answer in-progress without running the handler

<details>
<summary><strong>Tasks</strong></summary>

1. **`InboxOrchestrator.ProcessAsync<TResponse>`** (modify): return `ValueTask<InboxProcessResult<TResponse>>`. Using the claim result of #2401 (`Acquired`, `AlreadyProcessed`, `InProgress` with lease expiry; the exact type belongs to that issue, and this phase adapts to its final shape):
   - claim acquired and the handler ran: `Processed` with the handler's `Either` (a handler `Left` stays a cached business outcome per ADR-048; [#3441](https://github.com/dlrivada/Encina/issues/3441) may change how a retryable `Left` is cached, independent of this plan);
   - already processed: `Replayed` with the deserialized cached response (today's `HandleExistingMessageAsync` branch);
   - held by another delivery: `InProgress` with `Left(inbox.in_progress)`; the handler is **not** invoked.
2. A store `Left` still fails the operation with that `Left` and no outcome claims success (AGENTS.md section 3: errors are never swallowed): the result for an infrastructure failure uses `InboxOutcome.Processed` only when the handler ran, so the failure path returns `InboxProcessResult<T>.Failed(error)`, whose `Outcome` is null (no outcome was reached); the unit tests pin each case.
3. **`InboxPipelineBehavior<TRequest,TResponse>.ProcessIdempotentAsync`** (modify): `await orchestrator.ProcessAsync(...)` and return `.Result`, so the pipeline signature is unchanged.
4. Update the callers and tests that construct or call the orchestrator (listed in Research).
5. Every outcome decision uses `TimeProvider` (already injected) for the lease comparison; no `DateTime.UtcNow`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Phase 1 created InboxOutcome/InboxProcessResult<T>. InboxOrchestrator.ProcessCoreAsync today reads the
row, then runs the handler for any unprocessed row (the defect of #2401). The claim API of #2401 is merged.
TASK: Change InboxOrchestrator.ProcessAsync to return InboxProcessResult<TResponse>; derive Processed, Replayed
and InProgress from the claim result and the cached row; on InProgress do not call processCallback and return
Left(EncinaErrors.Create(InboxErrorCodes.InProgress, ..., lease expiry metadata)). Update
InboxPipelineBehavior to return the Result. Keep dead-letter capture and retry accounting as they are.
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
4. `src/Encina.AspNetCore/PublicAPI.Unshipped.txt`: new enum and property.

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

1. **`src/Encina.AspNetCore/ProblemDetailsExtensions.cs`** (modify):
   - `MapErrorCodeToStatusCode`: `inbox.in_progress` maps to `InboxInProgressResponse` (409 or 200); keep the method small (a lookup, not another `if` chain) to hold CRAP at 10 or less.
   - `ToProblemDetails`: for status 409 and the in-progress code, add the `Retry-After` header (seconds until `inbox.lease_expires_at_utc` from `TimeProvider`, minimum 1) and keep the `errorCode` extension; for the 200 answer return `Results.Ok(new { status = "in_progress", errorCode })` (no problem document, no message).
   - `GetTitle`: 409 already maps to "Conflict".
2. An explicit `statusCode` argument still wins, which is the per-endpoint override (REQ-036 "configured" at endpoint level without a second API).
3. The error message is not exposed: unchanged `ShouldExposeErrorMessage` rule.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: ToProblemDetails maps EncinaError codes to ProblemDetails and honors an explicit statusCode. The
in-progress duplicate arrives as Left with code inbox.in_progress and the metadata inbox.lease_expires_at_utc.
TASK: Map the code to 409 (default) or 200 per EncinaAspNetCoreOptions.InboxInProgressResponse; add Retry-After
on 409; keep the explicit statusCode override; 200 returns a small JSON status body, not a problem document.
KEY RULES: never expose EncinaError.Message outside the Development opt-in; no logging of the error message;
fail closed is not applicable here (this is a conflict signal, not a gate); time from TimeProvider resolved from
RequestServices with TimeProvider.System as the default; CRAP <= 10 on each changed method.
REFERENCE FILES: src/Encina.AspNetCore/ProblemDetailsExtensions.cs, tests/Encina.UnitTests/AspNetCore (existing ProblemDetails tests)
```

</details>

### Phase 5: Cross-Cutting Integration

<details>
<summary><strong>Tasks</strong></summary>

1. Tenancy (function 10): the outcome is keyed by whatever key [#3437](https://github.com/dlrivada/Encina/issues/3437) settles; this phase adds a unit test with two tenants and one key asserting that the second tenant is never reported `Replayed` or `InProgress` on the first tenant's row. It is a test dependency on #3437, not a design dependency: until #3437 lands the test is written against the then-current key and marked in the plan's Next Steps.
2. Idempotency (function 9): the contract is the feature itself; the Inbox remains opt-in (`config.UseInbox = true`), and this plan adds nothing when the Inbox is off.
3. Deferred items are listed in the matrix with their issue file (`inbox-stuck-claims-health-check`, `inbox-outcome-resilience-retry-after`).

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

1. **`src/Encina.Messaging/Diagnostics/InboxOutcomeMetrics.cs`** (new, internal static, same shape as `MessagePurgeMetrics`): counter `encina.messaging.inbox.outcome_total` on the `Encina` meter (already added to OpenTelemetry by `AddMeter("Encina")`), tag `outcome` with the three enum names only (bounded, no message id, no tenant).
2. **`src/Encina.Messaging/Diagnostics/InboxActivitySource.cs`** (modify): `CompleteWithOutcome(Activity?, InboxOutcome)` sets the tag `inbox.outcome`; the existing `inbox.message_id` tag stays as it is today.
3. **`src/Encina.Messaging/Diagnostics/InboxOutcomeLog.cs`** (new, `[LoggerMessage]` source generator): EventId **5500** `InProgressDuplicate` (Information, message type and correlation id only) and **5501** `OutcomeRecorded` (Debug). Register `MessagingInboxIdempotency = (5500, 5549)` in `src/Encina/Diagnostics/EventIdRanges.cs` first (ADR-021), add `nameof(EventIdRanges.MessagingInboxIdempotency)` to the `Encina.Messaging` entry of `EncinaEventIdAllocationTests.AssemblyRanges`, add the field to `src/Encina/PublicAPI.Unshipped.txt` and run the architecture tests.
4. The orchestrator calls the metrics and log from one place (the outcome derivation), so the three paths cannot drift.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: ADR-021: register the EventId range first. Messaging 2800-2999 is exhausted (the plans of #718, #1200,
#1251 and #583 reserved 2961-2992); 5500-6999 was free on 2026-10-10 and 5500 is still unused in EventIdRanges.cs.
TASK: Register MessagingInboxIdempotency (5500, 5549); write InboxOutcomeLog (5500, 5501), InboxOutcomeMetrics and
the activity tag; map the range to Encina.Messaging in EncinaEventIdAllocationTests; add the PublicAPI line.
KEY RULES: dimensions are bounded and carry no personal data; EncinaError.Message never reaches a log, tag or
metric; exceptions go through ForLogging(); pack EventIds sequentially; re-check that 5500 is still free on the
branch before registering and take the next free block if not.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Messaging/Diagnostics/MessagePurgeMetrics.cs,
src/Encina.Messaging/Diagnostics/InboxActivitySource.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 7: Testing — 6 Test Types

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/Messaging/Inbox/InboxOrchestratorTests.cs`, `.../Pipeline/InboxPipelineBehaviorTests.cs`, new `InboxOutcomeMetricsTests.cs`, new `AspNetCore/InboxInProgressProblemDetailsTests.cs`): each outcome; the handler is invoked once when a second delivery arrives while the first is in progress; 409 with `Retry-After`; 200; explicit `statusCode` override; lease expiry makes the next delivery `Processed` again; store `Left` is not reported as success; `FakeTimeProvider` for every time case.
2. **Guard** (`tests/Encina.GuardTests/Messaging/Inbox/InboxGuardTests.cs`): the changed `ProcessAsync` and the new mapper parameters throw `ArgumentNullException` on null.
3. **Contract** (new `tests/Encina.ContractTests/.../InboxOutcomeContractTests.cs`): `InProgress` always carries `inbox.in_progress`, `Replayed` and `Processed` never do; the HTTP mapping of the code is the same for every `IInboxStore` implementation used by the harness.
4. **Property**: not required; the outcome is a three-way decision with no complex invariant beyond the unit and contract cases (AGENTS.md section 9: property tests only if the logic is complex).
5. **Integration** (new `tests/Encina.IntegrationTests/Messaging/InboxInFlightRedeliveryScenario.cs`, using `InboxPipelineHarnesses.cs`): a slow handler, a second delivery during it, one handler run, the second caller sees `inbox.in_progress`, then a third delivery after completion sees `Replayed`; runs on all 10 providers through the `[Collection("<Family>-<Database>")]` fixtures with `ClearAllDataAsync()` in `InitializeAsync()`. These share the concurrency fixtures of [#2401](https://github.com/dlrivada/Encina/issues/2401).
6. **Load and Benchmark**: justification files only (the outcome adds one branch to a path whose cost is the database round trip; concurrency correctness is proven by the integration scenario). Written as `.md` under the load and benchmark test projects.
7. Coverage: set per-file targets with one-sentence justifications in `.github/coverage-manifest/Encina.Messaging.json` and `Encina.AspNetCore.json` for every touched file (targets never go down); measure each flag locally before the pull request and report the CRAP table.

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

1. XML docs on every new public symbol (`InboxOutcome`, `InboxProcessResult<T>`, `InboxErrorCodes.InProgress`, `InboxInProgressResponse`, the option) with `<example>` for the HTTP mapping.
2. `changelog.d/3440-inbox-in-progress-outcome.added.md` (never edit `CHANGELOG.md`), handed to `mechanical-fixer`.
3. `docs/messaging/inbox.md`: the three outcomes, the 409 or 200 setting, the per-endpoint override and the lease behavior; drafted by `docs-writer`. The wording of the exactly-once claim is an acceptance item of [#2401](https://github.com/dlrivada/Encina/issues/2401) and is not changed here.
4. `docs/INVENTORY.md`: the new files. ADR: no new ADR; the outcome contract is recorded in the ADR that [#2401](https://github.com/dlrivada/Encina/issues/2401) writes for the claim and lease (if it writes none, this feature writes one with the next free number at implementation time). `ROADMAP.md`: SPEC-002 REQ-036 progress line only if the milestone text changes.
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
| ADR-021 | EventId ranges are registered before use | `MessagingInboxIdempotency` (5500-5549) |
| IETF idempotency-key header draft | An in-flight request with the same key is answered 409 | Default of Design Choice 2 (pending the maintainer) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| InboxOrchestrator | src/Encina.Messaging/Inbox/InboxOrchestrator.cs | decides processed / replayed / in-progress from the store and returns the outcome |
| InboxPipelineBehavior<TRequest,TResponse> | src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs | maps the outcome to Either<EncinaError,TResponse>; in-progress becomes a Left with code inbox.in_progress |
| IInboxStore / IInboxMessage | src/Encina.Messaging/Inbox/IInboxStore.cs, IInboxMessage.cs | claim result consumed here; claim and lease implemented by issue 2401 on 10 providers |
| InboxErrorCodes | src/Encina.Messaging/Inbox/InboxOrchestrator.cs | new constant InProgress = "inbox.in_progress" |
| ProblemDetailsExtensions.ToProblemDetails | src/Encina.AspNetCore/ProblemDetailsExtensions.cs | existing mapper of EncinaError to HTTP; gets the in-progress mapping and the Retry-After header |
| EncinaAspNetCoreOptions | src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs | gets the option for the in-progress answer (409 or 200) |
| MessagePurgeMetrics | src/Encina.Messaging/Diagnostics/MessagePurgeMetrics.cs | pattern for the new InboxOutcomeMetrics counter on the Encina meter |
| InboxActivitySource | src/Encina.Messaging/Diagnostics/InboxActivitySource.cs | gets the inbox.outcome tag |
| EventIdRanges | src/Encina/Diagnostics/EventIdRanges.cs | new range MessagingInboxIdempotency 5500-5549 |
| Inbox test harnesses | tests/Encina.IntegrationTests/Messaging/InboxPipelineHarnesses.cs, InboxPipelineScenario.cs | provider matrix of the in-flight redelivery scenario |

Callers of `InboxOrchestrator` to update when `ProcessAsync` changes its return type: `InboxPipelineBehavior`, `MessagingServiceCollectionExtensions` (registration only), `tests/Encina.UnitTests/Messaging/Inbox/InboxOrchestratorTests.cs`, `InboxOrchestratorRetryTests.cs`, `tests/Encina.UnitTests/Messaging/Pipeline/InboxPipelineBehaviorTests.cs`, `tests/Encina.GuardTests/Messaging/Inbox/InboxGuardTests.cs`, `tests/Encina.IntegrationTests/Messaging/InboxRetryScenario.cs`, `DeadLetter/DeadLetterSourcesEndToEndScenario.cs`, `tests/Encina.UnitTests/Messaging/DeadLetter/DeadLetterSourcesCaptureTests.cs`.

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` | **5500-5504** used of the new `MessagingInboxIdempotency` = (5500, 5549) | 5500 `InProgressDuplicate`, 5501 `OutcomeRecorded`; 5502-5504 left for this feature's review fixes. The range is the family's block for inbox idempotency; the plans of #3437, #3441 and #2401 take the next free numbers of it (5505 onward) when they land. `Messaging` 2800-2999 has no free packed block left (2961-2992 are reserved by other plans) |
| `Encina.AspNetCore` | none | The mapper is a pure function; it logs nothing |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production source, new | 4 | InboxOutcome.cs, InboxOutcomeMetrics.cs, InboxOutcomeLog.cs, InboxInProgressResponse.cs |
| Production source, modified | 6 | InboxOrchestrator.cs, InboxPipelineBehavior.cs, InboxActivitySource.cs, EventIdRanges.cs, ProblemDetailsExtensions.cs, EncinaAspNetCoreOptions.cs |
| Metadata | 5 | PublicAPI.Unshipped.txt (Encina, Encina.Messaging, Encina.AspNetCore) and the two coverage manifests |
| Tests | 10 | orchestrator unit, behavior unit, metrics unit, mapping unit, guard, contract, integration scenario, EventId allocation test map, load and benchmark justification files (2) |
| Documentation | 3 | changelog fragment, docs/messaging/inbox.md, docs/INVENTORY.md |
| **Total** | **28** | Three pull requests, each within 15 files and 1,500 lines |

### Pull request split

| PR | Content | Files (approx.) |
|----|---------|-----------------|
| A | Phases 1, 2, 6: 7 production files, PublicAPI of `Encina` and `Encina.Messaging`, Messaging manifest, orchestrator, behavior and metrics unit tests, EventId allocation test map | 14 |
| B | Phases 3, 4: 3 production files, PublicAPI and manifest of `Encina.AspNetCore`, mapping unit test | 6 |
| C | Phase 7 guard, contract and integration tests, load and benchmark justifications; Phase 8 documentation and changelog | 8 |

A requires [#2401](https://github.com/dlrivada/Encina/issues/2401) merged; B can be built in parallel with A (it depends only on the error code string and the metadata key); C follows A and B.

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
1. Encina.Messaging/Inbox: InboxOutcome enum, InboxProcessResult<T>, InboxErrorCodes.InProgress; InboxOrchestrator
   .ProcessAsync returns the result; InboxPipelineBehavior returns .Result (pipeline signature unchanged).
2. Encina.AspNetCore: InboxInProgressResponse enum + EncinaAspNetCoreOptions property; ProblemDetailsExtensions
   maps inbox.in_progress to 409 with Retry-After or to 200; explicit statusCode still wins.
3. Observability: InboxOutcomeMetrics counter, inbox.outcome activity tag, InboxOutcomeLog (EventIds 5500, 5501)
   with the range MessagingInboxIdempotency (5500, 5549) registered first in EventIdRanges.cs.
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
| 2 | OpenTelemetry | ✅ | `encina.messaging.inbox.outcome_total` counter and the `inbox.outcome` tag on the inbox activity (Phase 6); the `Encina` meter is already added by `Encina.OpenTelemetry` |
| 3 | Structured Logging | ✅ | `InboxOutcomeLog` EventIds 5500 and 5501 in the new `MessagingInboxIdempotency` range (Phase 6) |
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
2. At pick-up: re-check the Design Choices against today's code, ask the maintainer the Dilemmas 2 and 3 (default status, lease-expiry behavior) and record the answers in `## Maintainer Decisions`; then run `check-plan.ps1` without `-Draft`.
3. Open the deferred follow-up as an issue: `artifacts/issues/inbox-stuck-claims-health-check.md`.
4. Confirm the `MessagingInboxIdempotency` range (5500-5549) is still free when PR A starts; coordinate the next free numbers with the plans of #3437, #3441 and #2401.
5. Measure per-flag coverage and the CRAP table for the touched files before each pull request.
