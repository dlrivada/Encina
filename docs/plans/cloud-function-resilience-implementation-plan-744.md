# Implementation Plan: Resilience for cloud function providers (AWS Lambda, Azure Functions)

> **Issue**: [#744](https://github.com/dlrivada/Encina/issues/744)
> **Type**: Feature
> **Complexity**: High (7 phases, 2 cloud providers, 5 pull requests)
> **Estimated Scope**: ~350 lines of production code + ~900 lines of tests (about 1,500 changed lines in total, four pull requests)

---

## Summary

Make an Encina function stop gracefully before the platform kills it, and state who owns each retry, by giving the two cloud adapters a platform deadline and one resilience options section, on top of `Encina.Resilience`. SPIKE #2691 R7 (option 7a, comment of 2026-10-24 on #744; [ADR-055](../architecture/adr/055-resilience-family.md)) says the cloud function adapters get decorators from `Encina.Resilience` with the SDK's own retry disabled where Encina retries. The comment of 2026-10-23 moves #744 into the 1.0 scope, for the AWS and Azure sides of the triangle.

What exists today (read in `origin/main` at `0336d31c7`):

- **The adapters are inbound hosts and make no outbound call.** `git grep "HttpClient\|GrpcChannel"` over `src/Encina.AwsLambda` and `src/Encina.AzureFunctions` finds only a comment. The seams are `SqsMessageHandler.ProcessBatchAsync` and `EventBridgeHandler.ProcessAsync` (static helpers taking a callback, `src/Encina.AwsLambda/`), `LambdaContextExtensions.GetRemainingTimeMs` (`ILambdaContext.RemainingTime`), `EncinaFunctionMiddleware` (`src/Encina.AzureFunctions/`, a singleton `IFunctionsWorkerMiddleware`) and the Durable helpers (`OrchestrationContextExtensions.CreateRetryOptions`, `DurableSagaBuilder.WithRetry`, which use the platform's `RetryPolicy`). Taken literally, "retry on 429/503" and "circuit breaker for downstream calls" decorate nothing in these packages (the same finding #745 records for gRPC and GraphQL); the downstream calls are made by handlers, which the per-type pipeline of `Encina.Resilience` already covers.
- A function that exceeds the platform limit is killed mid-batch: for SQS the whole batch is then redelivered, including the records already processed. `EncinaAwsLambdaOptions.EnableSqsBatchItemFailures` (default true) is read by no handler today (only the options class, `AwsLambdaHealthCheck` and the docs): `SqsMessageHandler.ProcessBatchAsync` always returns a partial batch response, so its `false` meaning is undefined; choice 7 defines it for the new overload.
- `Encina.Resilience` does **not exist in `src/`**, no open issue creates it, and R6 (token on the continuation, `IRequestContext.Deadline`) is not on main. `Encina.GoogleCloudFunctions` does not exist: #205 is open in the milestone "Post-1.0: Cloud services without a local emulator".
- The adapters' logs use `[LoggerMessage]` in an internal `Log` class inside their own ranges (`AwsLambda` 4700-4749, ids up to 4710 used; `AzureFunctions` 4750-4799, ids up to 4758 used).

What this plan delivers: a provider-neutral `CloudFunctionResilienceOptions` in `Encina.Resilience` (`SafetyMargin`, `FunctionTimeout`) with a public validation registration, a platform-deadline source per adapter, new overloads of the SQS and EventBridge helpers that take `ILambdaContext`, stop starting records when the margin is reached and report them as batch item failures, a middleware step for Azure Functions, four log definitions in the adapters' own ranges, tests for the throttled-`Left` path, and the documentation of retry ownership (platform redelivery, Durable retry, Encina retry). It adds no retry or breaker code to the adapters (choice 3); the issue's proposals "Lambda retry-after" and "Azure Functions host lifecycle hooks" are choices 9 and 10.

- **Affected packages**: `Encina.Resilience` (planned), `Encina.AwsLambda`, `Encina.AzureFunctions`. `Encina.GoogleCloudFunctions` does not exist (choice 8).
- **Provider category**: Cloud (AGENTS.md section 5): AWS and Azure now, GCP post-1.0 (SPEC-000 section 6.1, REQ-028).
- **Estimated size**: 5 pull requests of at most 15 files and 1,500 lines.
- **Prerequisites**: the foundation, R6 and #335, described in "Prerequisites & Dependencies".

### Shared Pieces and Ownership (family resilience-patterns-1 and resilience-patterns-2)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `Encina.Resilience`, the registry, `IRequestContext.Deadline` and the token (R6) | The foundation and R6 issue files of the family resilience-patterns-1 | all |
| `[Timeout]` and the `encina.timeout` mapper | #335 (sibling plan, by number) | enforces the deadline this plan assigns |
| `IProviderResilience` executor | #742 (sibling plan, by number) | not used here: the adapters have no outbound call (choice 3) |
| `CloudFunctionResilienceOptions`, the Lambda and Azure deadline sources, the batch early stop | **This plan (#744)** | a future GoogleCloudFunctions package (#205) |
| EventIds 4711-4712 (`AwsLambda`) and 4759-4760 (`AzureFunctions`), inside the existing ranges | This plan | none; no id of the block 950-999 is used |

---

## Design Choices

<details>
<summary><strong>1. Where the code lives while `Encina.Resilience` and R6 do not exist (shared with #136, #141, #145, #335 and #742)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Wait for the foundation and R6** | One implementation, in its final home; the deadline needs R6 anyway | Blocked on two unplanned pieces |
| **B) Build only the token part now in the adapters (a linked token, no context deadline)** | Ships earlier; no dependency | The part that propagates to outbound calls waits for R6 anyway; rework later |
| **C) The first pull request creates a minimal `Encina.Resilience` project** | Unblocks the plan | The first plan to land defines the foundation's shape |

### Chosen Option: **A — Wait for the foundation and R6** (recommended, pending the maintainer)

### Rationale

- A wins when the 1.0 order of the spike report is kept; B wins if functions must stop gracefully before R6; C wins if the foundation should grow out of a capability. Asked once for the resilience family (the shared Dilemma 1 of #136).

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. What "resilience of a cloud function provider" means when the adapters call nothing</strong></summary>

R7 assumed decorators over a client-side call; the adapters are inbound. The decision is the maintainer's, as for #745.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Platform-aware inbound resilience: a platform deadline (graceful timeout), early stop of an SQS batch with the rest reported as failures, per-provider options, and the documented ownership of retries** | Implements the one criterion that is about the platform (graceful timeout) and the batch semantics that only the adapter can improve; no code that decorates nothing | The acceptance wording on retry and breaker is met by composition (choice 3) |
| **B) Literal: retry and breaker around the adapters** | Matches the issue text | Nothing to wrap; would stack a second retry on the platform's redelivery (ADR-050) |
| **C) Wait for a decision like the spike #3384 of #745 before planning more** | One answer for both inbound adapters | Blocks the graceful timeout, which is well defined |

### Chosen Option: **A — Platform-aware inbound resilience** (recommended, pending the maintainer)

### Rationale

- A wins on delivering what the adapters can honestly do; C wins if the maintainer wants the inbound interpretation settled for gRPC, GraphQL and functions together.

- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Retry and circuit breaker for 429/503 and downstream calls (acceptance criteria 1 and 3)</strong></summary>

A handler's downstream retry and breaker belong to the per-type pipeline (`[Retry]`, `[CircuitBreaker]`) and, for HTTP, to `Encina.Http` (#1233: `Retry-After` pauses a partition on 429 or 503). The platform redelivers a failed SQS record, retries a failed Lambda async invocation, and Durable Functions retries activities by `RetryPolicy`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No retry or breaker code in the adapters; the documentation states the three owners (handler pipeline, platform redelivery, Durable `RetryPolicy`) and the rule that one request has one retry layer; a transient `Left` (category `RateLimited` or `Unavailable`) becomes a batch item failure so the platform redelivers** | One layer per request (ADR-050, R4); no duplicated retry; breaker state stays in the pipeline's per-container singleton | Breaker state is per execution environment and resets on a cold start (documented) |
| **B) The adapter retries the callback around `processMessage`** | Meets the wording | A second layer on top of the pipeline and the platform; duplicates |

### Chosen Option: **A — Composition, no new layer** (recommended, pending the maintainer)

### Rationale

- A wins on ADR-050; B wins only for a function that has neither a handler pipeline retry nor platform redelivery.

- **Class:** Dilemma

</details>

<details>
<summary><strong>4. How the deadline is carried: a token only, or the token and the context deadline</strong></summary>

The Lambda helpers take a callback `Func<SQSMessage, Task<Either<...>>>` with no token; the callback calls `IEncina.Send`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A token only: a linked source cancelled at `RemainingTime - SafetyMargin`, passed to a new callback overload `Func<SQSMessage, CancellationToken, Task<...>>`** | Simple; no R6 dependency | Outbound calls do not see the deadline |
| **B) The token and, when an `IRequestContextAccessor` is passed, `IRequestContext.Deadline` set for the call (set, restore in `finally`)** | Outbound clients propagate the earliest deadline (R6); #335 and #141 enforce it | An optional parameter; depends on R6 |

### Chosen Option: **B — Token and context deadline** (recommended, pending the maintainer)

### Rationale

- B wins when handlers call other services; A wins for a function that calls nothing. The deadline never extends an existing one (the earliest wins) and time comes from `TimeProvider`. For Azure Functions the middleware holds the accessor through DI.

- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Per-provider configuration</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `Resilience` property of type `CloudFunctionResilienceOptions` on `EncinaAwsLambdaOptions` and `EncinaAzureFunctionsOptions`; both settings are null by default, so nothing applies until set** | One place per provider; discoverable; opt-in | The options classes grow by one property; both adapter packages take a `ProjectReference` to `Encina.Resilience` (and so Polly transitively) for two `TimeSpan?` properties |
| **B) A separate `AddEncina<Provider>Resilience()` with its own options** | Same shape as #742 | A second call for a function app that already configures the adapter |

### Chosen Option: **A — A section of the existing options** (recommended, pending the maintainer)

### Rationale

- A wins on discoverability; B wins for consistency with the transports and avoids the Polly reference for a function app that never uses the feature. Validation runs at startup through a **public** registration method of `Encina.Resilience` (`AddCloudFunctionResilienceValidation<TOptions>(services, selector, platformSuppliesLimit)`), because the validator class is internal and the adapters are other assemblies: values positive, `SafetyMargin` below `FunctionTimeout` when both are set, and a `SafetyMargin` without a `FunctionTimeout` fails startup unless the platform supplies the limit (AWS passes `true`; Azure passes `false`), so a configuration that would silently do nothing is rejected (log 4760 reports the rejected option name).

- **Class:** Dilemma

</details>

<details>
<summary><strong>6. Where the platform limit comes from</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) AWS from `ILambdaContext.RemainingTime` (exact, per invocation); Azure from the explicit `FunctionTimeout` option; no value means no deadline** | Opt-in; correct per platform | The Azure value is the application's statement |
| **B) Hard-coded defaults per hosting plan** | Nothing to configure | Default-on and wrong when `host.json` differs |

### Chosen Option: **A — Context on AWS, explicit option on Azure**

### Rationale

- B is default-on and cannot know the application's limit, so only A complies; it holds under every option of choices 1 to 5 (it concerns the source of the limit).
- **Class:** Settled
- **Settled by:** AGENTS.md section 1 (every capability opt-in) and ADR-055 settled points ("Every capability is opt-in")

</details>

<details>
<summary><strong>7. What happens to the rest of an SQS batch near the limit</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Before starting a record, if the remaining time is below the margin, stop and report every unstarted record (and only those) as `BatchItemFailure`; the loop always starts at least one record per invocation, so every invocation makes progress; the new overload honours `EnableSqsBatchItemFailures` (false: no early stop, the existing behavior)** | No mid-batch kill; no duplicate processing of done records | A slow record can still use the margin; an unstarted record is redelivered and its SQS receive count rises, so with a persistently slow batch it can reach the queue's `maxReceiveCount` and be dead-lettered without ever being tried (mitigated by the one-record minimum and by the margin; documented) |
| **B) Do nothing: process until the platform kills the function** | No change | The whole batch is redelivered, including processed records, and every record's receive count rises anyway |

### Chosen Option: **A — Stop starting records near the limit** (recommended, pending the maintainer)

### Rationale

- A wins when batches are large and processed records are costly to repeat; B wins for a queue with a very low `maxReceiveCount` and slow records, where early stop could dead-letter unseen records. With no deadline configured, which is the default (choice 6), the behavior is unchanged until an application opts in.

- **Class:** Dilemma

</details>

<details>
<summary><strong>8. The GCP side of the triangle</strong></summary>

AGENTS.md section 5 asks for the AWS/Azure/GCP triangle for cloud features; `Encina.GoogleCloudFunctions` does not exist (#205, open, post-1.0).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) AWS and Azure now; the options type is provider-neutral so GCP reuses it; the gap is recorded here and on #205 when that issue starts** | Matches SPEC-000 | The triangle is incomplete until #205 |
| **B) Build a GCP package for this** | Complete triangle | Out of 1.0 scope (no local emulator) |

### Chosen Option: **A — AWS and Azure; GCP recorded as a gap**

### Rationale

- B contradicts the scope decision, so A is the only option; it holds under every option of choices 1 to 7 (it concerns provider coverage).
- **Class:** Settled
- **Settled by:** SPEC-000 section 6.1 (REQ-028, GCP post-1.0) and AGENTS.md section 5 (Cloud row: "GCP is post-1.0 (SPEC-000 §6.1, REQ-028): note the GCP gap in the issue")

</details>

<details>
<summary><strong>9. "Lambda retry-after" (proposal 4 of the issue)</strong></summary>

The issue lists "Lambda retry-after" as a platform-specific item. `Retry-After` is already written to HTTP responses (`ApiGatewayResponseExtensions`); for SQS the redelivery delay is the queue's visibility timeout.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No code: a transient `Left` becomes a batch item failure and the queue's visibility timeout and redrive policy decide the delay; the documentation says so** | No SQS client dependency; one retry owner (the platform) | A server-dictated `Retry-After` is not honoured for SQS records |
| **B) The helper calls `ChangeMessageVisibility` with the error's `Retry-After` for a failed record** | Honours the dictated delay | A new AWS SDK dependency and an outbound call in an inbound adapter; a second delay owner next to the platform |

### Chosen Option: **A — The platform owns the delay** (recommended, pending the maintainer)

### Rationale

- A wins on ADR-050 (one retry layer); B wins if queues must obey `Retry-After` of a throttled downstream.

- **Class:** Dilemma

</details>

<details>
<summary><strong>10. "Azure Functions host lifecycle hooks" (proposal 4 of the issue)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No lifecycle code: the host's shutdown already reaches a function through the `CancellationToken` it takes; the middleware's deadline (choice 4) is the graceful-timeout mechanism; the documentation says so** | No claim about worker internals; nothing to maintain | A function that takes no token ignores a host shutdown |
| **B) The middleware also links the host's cancellation into the token and deadline it assigns** | Covers functions that take no token | Depends on worker behavior that must be verified; a second source of cancellation next to the deadline |

### Chosen Option: **A — Deadline only** (recommended, pending the maintainer)

### Rationale

- A wins on scope; B wins if functions are commonly written without a token.

- **Class:** Dilemma

</details>

---

## Implementation Phases

Five pull requests, in order: **PR-1** Phase 1 (`CloudFunctionResilienceOptions`, its validator and the public validation registration, in `Encina.Resilience` only); **PR-2** the AWS deadline and SQS half: the `Resilience` property, the `ProjectReference`, the validation registration, `LambdaContextExtensions`, the `SqsMessageHandler` overload with the early stop, the logs and their unit, guard, DI and integration tests; **PR-3** the `EventBridgeHandler` overload with its tests; **PR-4** the Azure half likewise (middleware step, with its integration test); **PR-5** Phase 7 (documentation and justification files). Every PR needs the foundation, R6 and #335 merged (choice 1 option A).

### Phase 1: Core Models

<details>
<summary>Tasks</summary>

1. `src/Encina.Resilience/CloudFunctions/CloudFunctionResilienceOptions.cs`: `TimeSpan? SafetyMargin`, `TimeSpan? FunctionTimeout` (both null: nothing applies); `CloudFunctionResilienceOptionsValidator.cs` (internal, generic over the owning options type, rules of choice 5); `CloudFunctionResilienceServiceCollectionExtensions.cs` (public): `AddCloudFunctionResilienceValidation<TOptions>(this IServiceCollection services, Func<TOptions, CloudFunctionResilienceOptions> selector, bool platformSuppliesLimit)`, with `ArgumentNullException.ThrowIfNull` on `services` and `selector`, registering the validator as `IValidateOptions<TOptions>` with `ValidateOnStart`; the adapters call this public method, so no `InternalsVisibleTo` between assemblies is needed.
2. PR-2 and PR-4 (not PR-1): `EncinaAwsLambdaOptions.cs` and `EncinaAzureFunctionsOptions.cs` each gain a `Resilience` property, and each package gains a `ProjectReference` to `Encina.Resilience` and calls the registration of task 1 from its `ServiceCollectionExtensions.cs`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina issue #744, plan docs/plans/cloud-function-resilience-implementation-plan-744.md. Encina.Resilience, R6 and the #335 mapper exist.
TASK: Add CloudFunctionResilienceOptions, its validator and the Resilience property on both adapter options.
KEY RULES: null by default (opt-in); invalid values fail at startup; XML docs with examples.
REFERENCE FILES: src/Encina.AwsLambda/EncinaAwsLambdaOptions.cs, src/Encina.AzureFunctions/EncinaAzureFunctionsOptions.cs
```

</details>

### Phase 2: Default Implementation — the Deadline Sources

<details>
<summary>Tasks</summary>

1. AWS: `LambdaContextExtensions.cs` (modified): `public static TimeSpan? GetDeadlineBudget(this ILambdaContext context, CloudFunctionResilienceOptions options)` returns `RemainingTime - SafetyMargin` or null when no margin is set; `SqsMessageHandler.cs` and `EventBridgeHandler.cs` (modified): new overloads that take `ILambdaContext`, the options, an optional `IRequestContextAccessor` and a token-taking callback (choice 4 B); `ProcessBatchAsync` checks the remaining time before each record (choice 7 A).
2. Azure: `EncinaFunctionMiddleware.cs` (modified): when `FunctionTimeout` and `SafetyMargin` are set, assigns the deadline through the accessor for the invocation and restores it in `finally`; the time source is `TimeProvider`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 merged.
TASK: Implement the deadline sources, the Lambda overloads and the middleware step.
KEY RULES: never extend an existing deadline; restore the previous context in finally; cancellation propagates; time only through TimeProvider; parameters of public methods guarded; the existing overloads keep their behavior when no margin is set.
REFERENCE FILES: src/Encina.AwsLambda/SqsMessageHandler.cs, src/Encina.AzureFunctions/EncinaFunctionMiddleware.cs, src/Encina/Abstractions/IRequestContextAccessor.cs
```

</details>

### Phase 3: Configuration and DI

<details>
<summary>Tasks</summary>

1. Both `ServiceCollectionExtensions.cs` (modified, in PR-2 and PR-4): call `AddCloudFunctionResilienceValidation` (which registers the validator as `IValidateOptions<TOptions>` and also calls `services.AddOptions<TOptions>().ValidateOnStart()`, which is idempotent next to the adapter's own `Configure`; the DI test of each PR asserts that an invalid section fails the provider build) (AWS with `platformSuppliesLimit: true`, Azure with `false`); the DI tests build with `ValidateOnBuild` and `ValidateScopes`.
2. `PublicAPI.Unshipped.txt` of the three packages (`mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Phases 1 and 2 done.
TASK: Register and validate the Resilience section in both adapters.
KEY RULES: TryAdd* registrations; ValidateOnBuild and ValidateScopes in the DI tests.
REFERENCE FILES: src/Encina.AzureFunctions/ServiceCollectionExtensions.cs
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. A transient `Left` of a record (category `RateLimited` or `Unavailable`) is a batch item failure (existing behavior of every `Left`), now documented; `Retry-After` stays in the HTTP responses already mapped by `ApiGatewayResponseExtensions` and `HttpResponseDataExtensions`.
2. Tenant: not keyed (a deadline is per invocation); telemetry tenant tag only with multi-tenancy (ADR-054 D4).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Phases 1 to 3 done.
TASK: Verify the cross-cutting rows; add only what they need.
KEY RULES: no new retry layer; no tenant key.
REFERENCE FILES: docs/architecture/adr/050-modules-may-require-modules.md
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. `src/Encina.AwsLambda/Log.cs` (modified): 4711 `SqsBatchStoppedNearDeadline` (Warning: processed, unstarted counts), 4712 `LambdaDeadlineAssigned` (Debug); `src/Encina.AzureFunctions/Log.cs` (modified): 4759 `FunctionDeadlineAssigned` (Debug), 4760 `FunctionDeadlineOptionInvalid` (Warning). Ids are inside the existing ranges; no id of the new block is used. Never log record bodies or message ids.
2. Spans and instruments follow R5 and #728; this plan creates none.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Phases 1 to 4 done.
TASK: Add the four log definitions and emit them.
KEY RULES: ids only inside the adapters' registered ranges (verify free by repository-wide search before use); packed; no message data in logs; [LoggerMessage] source generator.
REFERENCE FILES: src/Encina.AwsLambda/Log.cs, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 6: Testing

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/AwsLambda/`, `.../AzureFunctions/`): a fake `ILambdaContext` with a controlled `RemainingTime`; the batch stops before a record when the margin is reached and reports exactly the unstarted ones, always starts one record, and does not stop early when `EnableSqsBatchItemFailures` is false; no margin keeps today's behavior; the deadline is set and restored; the earliest deadline wins; options validation, including a `SafetyMargin` without a `FunctionTimeout` on Azure. Throttling (the acceptance criterion "unit tests for throttling"): a record whose `Left` has category `RateLimited` or `Unavailable` becomes a batch item failure, and an API Gateway and an HTTP trigger response for the same `Left` carry 429 or 503 with `Retry-After` when the error has it (existing mapping, now asserted for the throttled paths).
2. Guard (`tests/Encina.GuardTests/`): the new overloads (null event, null callback, null context, null options) and the new extension; an optional accessor accepted as null (valid result).
3. Integration: `tests/Encina.IntegrationTests` already runs the Lambda handlers and the Azure middleware in-process; each PR extends the existing integration test class of its code (SQS in PR-2, EventBridge in PR-3, the middleware in PR-4) with a short margin through real DI, so the new code is reached by the integration flag in the PR that adds it.
4. Contract, property, load, benchmark: not applicable beyond the above; justification files for load and benchmark.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Phases 1 to 5 done.
TASK: Write the Phase 6 tests.
KEY RULES: real code; FakeTimeProvider, never Thread.Sleep; Shouldly through Encina.Testing.Shouldly; existing manifest targets are never lowered.
REFERENCE FILES: tests/Encina.UnitTests/AwsLambda/SqsMessageHandlerTests.cs, .github/coverage-manifest/Encina.AwsLambda.json
```

</details>

### Phase 7: Documentation and Finalization

<details>
<summary>Tasks</summary>

1. READMEs of `Encina.AwsLambda` and `Encina.AzureFunctions` and `docs/features/cloud-function-resilience.md` (`docs-writer`): the deadline, the batch behavior, the table of retry owners, the per-environment breaker note, and the GCP gap.
2. `changelog.d/744-cloud-function-resilience.added.md`, `PublicAPI.Unshipped.txt`, manifests (`mechanical-fixer`).
3. Verification: `dotnet build` 0 warnings; tests filtered to the two adapters; EventId architecture tests; `--check-justifications`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Phases 1 to 6 green.
TASK: Documentation and verification.
KEY RULES: never edit CHANGELOG.md; docs by docs-writer, metadata by mechanical-fixer; no hand-typed coverage figures.
REFERENCE FILES: changelog.d/README.md, .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Relevance |
|--------|-----------|
| ADR-055 R4, R6, R7 | One retry owner; deadline; decorators |
| ADR-050 resilience row | One retry layer per request |
| SPEC-000 section 6.1 (REQ-028) | GCP post-1.0 |
| AWS Lambda `ILambdaContext.RemainingTime`; SQS partial batch response | Deadline source; batch item failures |
| Azure Functions isolated worker, Durable `RetryPolicy` | Host timeout; platform retry |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `LambdaContextExtensions.GetRemainingTimeMs` | `src/Encina.AwsLambda/LambdaContextExtensions.cs` | Deadline source |
| `SqsMessageHandler`, `EventBridgeHandler` | `src/Encina.AwsLambda/` | New overloads |
| `EncinaFunctionMiddleware` | `src/Encina.AzureFunctions/EncinaFunctionMiddleware.cs` | Deadline step |
| `IRequestContextAccessor` | `src/Encina/Abstractions/` | Deadline carrier |
| `ApiGatewayResponseExtensions`, `HttpResponseDataExtensions` | the two packages | Existing 429, 503, 504 mapping |

Cloud triangle:

| Provider | In this plan | Notes |
|----------|--------------|-------|
| AWS Lambda | Yes (PR-2, PR-3) | Exact remaining time |
| Azure Functions | Yes (PR-4) | Explicit `FunctionTimeout` option |
| GCP | No: #205, post-1.0 | Options type is provider-neutral |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `AwsLambda` (existing 4700-4749) | 4711-4712 | Ids to 4710 are used on main; verified by search |
| `AzureFunctions` (existing 4750-4799) | 4759-4760 | Ids to 4758 are used on main; verified by search |

### Estimated File Count

| PR | Files | Content |
|----|-------|---------|
| PR-1 | 7 | `CloudFunctionResilienceOptions`, validator (internal), `CloudFunctionResilienceServiceCollectionExtensions`, 2 tests (options and validator rules, guard), `Encina.Resilience` `PublicAPI.Unshipped.txt`, manifest |
| PR-2 | 14 | AWS package: `EncinaAwsLambdaOptions`, `Encina.AwsLambda.csproj`, `ServiceCollectionExtensions`, `LambdaContextExtensions`, `SqsMessageHandler`, `Log.cs` (6 modified); tests: SQS handler (throttled path, early stop), context extensions, DI, options, guard, the existing SQS integration test class (6, modified or new); `PublicAPI.Unshipped.txt`, manifest |
| PR-3 | 6 | `EventBridgeHandler` (modified), its unit test, guard test and integration test (modified), `PublicAPI.Unshipped.txt`, manifest |
| PR-4 | 12 | Azure package: `EncinaAzureFunctionsOptions`, `Encina.AzureFunctions.csproj`, `ServiceCollectionExtensions`, `EncinaFunctionMiddleware`, `Log.cs` (5 modified); tests: middleware, DI, options, guard, the existing middleware integration test (5); `PublicAPI.Unshipped.txt`, manifest |
| PR-5 | 6 | 2 justification `.md` (load, benchmark), 2 READMEs, feature page, changelog fragment |

Each PR is within 15 files and 1,500 lines. The existing manifest targets of both packages are not lowered (the numbers below are the targets of the new code in the file, set in the PR that adds it; a modified file keeps its existing per-file target if that is higher).

Per-file targets:

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `CloudFunctionResilienceOptions.cs`, validator (PR-1) | 95 | 0 | 0 | 0 | 0 | Property bag and rules; the internal validator is reached through the public registration, so no `InternalsVisibleTo` is needed |
| `CloudFunctionResilienceServiceCollectionExtensions.cs` (PR-1) | 90 | 100 | 0 | 0 | 0 | One guarded public method (null `services`, null `selector`) |
| `SqsMessageHandler.cs` (PR-2, modified; existing 90 / 95 / integration 95) | 95 | 95 | 0 | 0 | 95 | Early stop, one-record minimum, the false option and the throttled path driven by unit tests; the guard test sends null arguments to the new overload; the existing integration class gains a test for the new overload |
| `EventBridgeHandler.cs` (PR-3, modified; existing 95 / 95 / integration 100) | 95 | 95 | 0 | 0 | 100 | The overload is driven with a fake `ILambdaContext` and, for the integration floor, through the existing in-process integration class |
| `LambdaContextExtensions.cs` (PR-2, modified; existing 94 / 95 / 95) | 95 | 95 | 0 | 0 | 95 | The deadline helper, driven with a fake context and through the SQS integration test |
| `EncinaAwsLambdaOptions.cs` (PR-2, modified; existing 100 / 85 / 90) | 100 | 85 | 0 | 0 | 90 | One property added; existing floors kept |
| `EncinaFunctionMiddleware.cs` (PR-4, modified; existing 100 / guard 30 / integration 100) | 100 | 30 | 0 | 0 | 100 | New branch driven with a fake `FunctionContext` and through the existing in-process integration class, with real DI and a short `FunctionTimeout` |
| `EncinaAzureFunctionsOptions.cs` (PR-4, modified; existing 100 / 80 / 90) | 100 | 80 | 0 | 0 | 90 | One property added; existing floors kept |
| AWS `ServiceCollectionExtensions.cs` (PR-2, modified; existing guard 85 / integration 100) | 90 | 85 | 0 | 0 | 100 | DI tests with `ValidateOnBuild`; the validation call is reached through them and through the integration host |
| Azure `ServiceCollectionExtensions.cs` (PR-4, modified; existing 100 / 25 / 100) | 100 | 25 | 0 | 0 | 100 | Same |
| Both `Log.cs` (modified; existing 0 / 0 / 0) | 0 | 0 | 0 | 0 | 0 | Generated partial methods; no executable lines of our own, and the emitting code is covered above |

CRAP of each added method is at most 10 (the batch loop splits the time check from the record handling).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #744: resilience for cloud function adapters (ADR-055 R7, interpreted for inbound hosts). AGENTS.md rules apply. The foundation, R6 and #335 are merged first; if missing, stop and report.

IMPLEMENTATION OVERVIEW: (1) CloudFunctionResilienceOptions and the Resilience section on both adapters; (2) a platform deadline (Lambda RemainingTime, Azure FunctionTimeout) assigned as token and context deadline; (3) SQS batch early stop with batch item failures; (4) four logs inside the adapters' ranges; (5) unit, guard and integration tests; (6) docs on retry ownership and the GCP gap.

KEY PATTERNS: opt-in (null by default); never extend a deadline; no new retry layer; cancellation propagates; time through TimeProvider; specialists for PublicAPI, changelog, manifest and docs.

REFERENCE FILES: docs/architecture/adr/055-resilience-family.md, src/Encina.AwsLambda/SqsMessageHandler.cs, src/Encina.AzureFunctions/EncinaFunctionMiddleware.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | No cacheable data |
| 2 | OpenTelemetry | ✅ | R5 and #728 conventions; no instrument of our own; tenant tag only with multi-tenancy |
| 3 | Structured Logging | ✅ | 4711-4712 and 4759-4760 |
| 4 | Health Checks | ❌ | Existing adapter health checks stay |
| 5 | Validation | ✅ | Options validated at startup |
| 6 | Resilience | ✅ | Graceful timeout; retry ownership documented (choice 3) |
| 7 | Distributed Locks | ❌ | No shared state |
| 8 | Transactions | ❌ | The batch response is the platform's contract |
| 9 | Idempotency | ✅ | Early stop avoids redelivery of processed records |
| 10 | Multi-Tenancy | ❌ | The deadline is per invocation; no tenant key |
| 11 | Module Isolation | ❌ | Module-independent |
| 12 | Audit Trail | ❌ | Infrastructure concern |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|--------------|-------|--------|
| The foundation and R6 | No issue exists: issue files `resilience-foundation-encina-resilience.md` and `resilience-r6-pipeline-token-and-deadline.md` (family resilience-patterns-1) | Block implementation (choice 1) |
| #335 timeout | Sibling plan, by number | Enforces the assigned deadline |
| #205 (open) | Open, post-1.0 | GCP gap (choice 8) |
| #745 (open) and spike #3384 (open) | Open | Same inbound-host question for gRPC and GraphQL |
| #742 | Sibling plan, by number | Provider executor, not used here |

---

## Next Steps

1. Commit this plan, open its PR and link it from #744.
2. When #744 is picked up, re-check the choices against today's code and ask the maintainer about the Dilemmas (choices 1 to 5, 7, 9 and 10).
3. Brief workers for PR-1 to PR-5 in order, after the foundation, R6 and #335 are merged.
