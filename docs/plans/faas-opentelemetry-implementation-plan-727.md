# Implementation Plan: OpenTelemetry for AWS Lambda and Azure Functions (Invocation Spans, Cold Start, Outcome Metrics, W3C Context)

> **Issue**: [#727](https://github.com/dlrivada/Encina/issues/727)
> **Type**: Feature (observability, cloud)
> **Related**: ADR-054 (D2 parent or link rule, D3 names, D4 tenant tag, D6 no data-bearing values, settled points 2, 3, 7), #132 (`TraceContextConsumer`, `IContextPropagator`), #186 (`RequestTraceContext`), #725 (transport instrumentation, the producer side of the SQS and EventBridge carriers), #744 (resilience of the same two packages, shares `Log.cs`), #2676 (tenant tag helper), #2693 (Google Cloud Functions, the part of #727 that needs a paid account), #3252 (Functions host smoke test), #906, ADR-062
> **Complexity**: High (neither package hands the instrumentation a context that carries a cold-start signal, so the entry point of each platform is a design choice)
> **Estimated Scope**: ~330 lines of production code + ~900 lines of tests, in six pull requests of at most 15 files
> **Provider category**: Cloud, AWS and Azure (`AGENTS.md` section 5). Google Cloud Functions is post-1.0 (SPEC-000 section 6.1, REQ-028): the issue was split on 2026-10-23 and the GCP part is #2693.

---

## Summary

### What exists today (read on 2026-11-24, main `5aee5679e`)

- **No telemetry exists in either package**: a search of `src/Encina.AwsLambda` and `src/Encina.AzureFunctions` finds no `ActivitySource`, no `Meter`, no `traceparent`, no `faas.*` and no `ColdStart`. `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` (`WithEncina`, lines 181-213) subscribes a hard-coded list of sources and meters and knows neither package; it does not reference them.
- **Azure Functions has one entry point, a middleware**: `EncinaFunctionMiddleware.Invoke(FunctionContext, FunctionExecutionDelegate)` (`src/Encina.AzureFunctions/EncinaFunctionMiddleware.cs`) already wraps every invocation, logs start, completion and failure through `Log` (EventIds 4750-4752) and stores the correlation id in `context.Items`. The worker's `FunctionContext` exposes `TraceContext` (`TraceParent`, `TraceState`) and `FunctionDefinition.Name` (the assembly `Microsoft.Azure.Functions.Worker.Core` declares both). The middleware has no clock and no cold-start signal.
- **AWS Lambda has no entry point at all**: the package is a set of static helpers. `SqsMessageHandler.ProcessBatchAsync(SQSEvent, processMessage, ILogger?, CancellationToken)` and `EventBridgeHandler.ProcessAsync(...)` take no `ILambdaContext`; `ApiGatewayResponseExtensions` only builds responses; `LambdaContextExtensions` reads an `ILambdaContext` the function passes in itself. The Lambda log events `LambdaExecutionStarting`, `LambdaExecutionCompleted` and `LambdaExecutionFailed` (EventIds 4700-4702 in `src/Encina.AwsLambda/Log.cs`) are declared and **never called** (the only `Log.*` call sites are the four tenant events in `LambdaContextExtensions.cs`).
- **Neither package reads a trace context**, and the tenant is resolved by the function body after the invocation has started (the middleware passes `"(pending)"` to `Log.ContextEnriched`), so a tenant is not known when a span starts.
- Neither csproj lets the unit tests see internal types: `Encina.AzureFunctions.csproj` declares `InternalsVisibleTo` for UnitTests, IntegrationTests, ContractTests, PropertyTests, LoadTests and Benchmarks (not GuardTests); `Encina.AwsLambda.csproj` declares none.
- Tests today: `tests/Encina.UnitTests/AwsLambda/*` and `.../AzureFunctions/*`, guard tests under `tests/Encina.GuardTests/Cloud/{AwsLambda,AzureFunctions}/`, integration tests under `tests/Encina.IntegrationTests/Cloud/{AwsLambda,AzureFunctions}/` (none uses LocalStack or the Functions host; `EncinaFunctionMiddlewareIntegrationTests.cs` runs the middleware in a composed worker pipeline). The shared fixtures `LocalStackFixture` (collection `Cloud-LocalStack`, with an SQS client) and `AzuriteFixture` (collection `Cloud-Azurite`) exist in `tests/Encina.TestInfrastructure/Fixtures/`, and `Encina.IntegrationTests` already declares both `[CollectionDefinition]` classes (`MessageBrokers/AmazonSQS/LocalStackCollection.cs`, `Infrastructure/Azurite/AzuriteCollection.cs`).
- Event ids: `AwsLambda` 4700-4749 uses 4700-4710 (4711-4712 are claimed by #744); `AzureFunctions` 4750-4799 uses 4750-4758 (4759-4760 are claimed by #744). This plan needs none (register row C2).

### What this plan delivers

- **One instrumentation per package, same shape**, in the package (Design Choice 1): `Encina.AwsLambda` and `Encina.AzureFunctions` each own an `ActivitySource` and a `Meter` of their own name, so a host switches one platform on without the other.
- **Entry points** (Design Choice 2): Azure through the existing middleware (no new registration, no new option); Lambda through a new public static `LambdaInstrumentation.TraceAsync<T>` and `TraceEitherAsync<T>` that the function wraps around its handler (an optional `ILogger` and `TimeProvider` as trailing parameters), because the package has no other seam that sees an `ILambdaContext` for every trigger, API Gateway included.
- **Cold start** (Design Choice 3): a per-process `ColdStartTracker` (internal) whose first `TryConsume()` returns `true`; the span tag `faas.coldstart` and the metric tag carry it. Cold-start ratio is derived from the counter (no second instrument).
- **Spans**: one `Server` span per invocation named after the function, with `cloud.provider` (`aws` or `azure`), `cloud.platform` (`aws_lambda`, `azure_functions`), `faas.name`, `faas.trigger` (a closed set: `http`, `pubsub`, `timer`, `datasource`, `other`, from the new `FaasTrigger` enum in `Encina.Diagnostics`), `faas.coldstart`, `faas.invocation_id` (span only, never a metric tag), `error.type` on failure (the `EncinaError` code or the exception type name, ADR-054 settled point 2).
- **Instruments** (Design Choice 4) on `Meter("Encina.AwsLambda")` and `Meter("Encina.AzureFunctions")`: `encina.faas.invocations` (counter; tags `faas.name`, `faas.trigger`, `faas.coldstart`, `cloud.provider`, `error.type` on failures) and `encina.faas.invoke.duration` (histogram, unit `s`, same tags). Error rate and cold-start ratio derive from the tags.
- **Incoming trace context** (Design Choice 5, after #132): HTTP triggers use the `traceparent` and `tracestate` headers as the parent of the invocation span; an SQS batch gets one consumer span per record with the record's `traceparent` message attribute as an `ActivityLink` (ADR-054 D2, `TraceContextConsumer` with `TraceDispatchKind.Batch`); the Azure middleware reads `FunctionContext.TraceContext`. X-Ray (`AWSTraceHeader`) is not read (ADR-054 settled point 3).
- **Tenant tag** (after #2676): through the shared helper, evaluated when the invocation ends (the tenant is resolved by function code after the span starts), absent when none resolved.
- **Registration**: `WithEncina()` subscribes the two sources and the two meters (four lines in `Encina.OpenTelemetry`, no project reference).
- **Shared pieces and owner**: this plan owns `FaasTrigger` (core), both `ColdStartTracker` copies and the `Encina.AwsLambda` and `Encina.AzureFunctions` telemetry classes. #132 owns `TraceContextConsumer`; #744 owns `Log.cs` ids 4711-4712 and 4759-4760; #723, #724 and #725 edit `WithEncina` too (a merge-order note, not a dependency).
- **Not delivered, with the reason**: GCP (post-1.0, #2693); memory-usage and CPU metrics (the Proposed Solution list of #727 has none, and `ILambdaContext` exposes only the limit); a cold-start duration histogram (the init phase is not visible to user code; cold start is a boolean); an options class (no switch: telemetry costs nothing without a listener, ADR-054 settled point 7).

---

## Design Choices

<details>
<summary><strong>1. Where the FaaS instrumentation lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In each cloud package, source and meter named after the package** | No new project reference (the issue's `Encina.OpenTelemetry` has no reference to either cloud package and would need the Functions worker and Lambda libraries); one owner per source name, one source per package (ADR-054 D3); a host enables one platform | The cold-start tracker and instrument creation exist twice (about 60 lines each) |
| **B) In `Encina.OpenTelemetry` as "cloud function enrichers"** | One package for telemetry, as the issue lists | `Encina.OpenTelemetry` gains references to `Microsoft.Azure.Functions.Worker` and the Lambda libraries, or the two cloud packages gain a reference to it; the cloud packages cannot start an invocation span themselves |
| **C) A shared helper in core `Encina`** | One copy of the tracker | Core learns a platform concept, and both packages need `InternalsVisibleTo` from core |

### Chosen Option: **A - In each cloud package** (recommended, pending the maintainer)

### Rationale

- A wins on dependencies and on the one-source-per-package rule; B wins if the maintainer wants a single package to own every telemetry decorator. The only shared public piece is the tiny `FaasTrigger` enum, in core `Encina.Diagnostics`, so both packages emit the same trigger values (a contract test proves it).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. How an invocation is traced when there is no context to hook</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Azure: the existing middleware. Lambda: a public static wrapper `LambdaInstrumentation.TraceAsync`/`TraceEitherAsync` the function calls around its handler** | Covers every Lambda trigger, API Gateway included (that package has no handler for it); the wrapper receives the `ILambdaContext`, which is where the function name, request id and version are; no change to existing signatures | The function author must call the wrapper (one line); two shapes (middleware and wrapper) |
| **B) Add an optional `ILambdaContext? lambdaContext = null` to `SqsMessageHandler` and `EventBridgeHandler`** | No new type | Covers neither API Gateway nor a hand-written handler; a nullable context silently gives no cold start; the SQS overloads already end in `jsonOptions` and `cancellationToken` |
| **C) Depend on the OpenTelemetry .NET contrib Lambda wrapper and only enrich** | Maintained cold-start logic | A new third-party dependency in a package that has none; the wrapper expects its own tracer provider, so Encina's meter and tag set would be a second layer |

### Chosen Option: **A - Middleware for Azure, wrapper for Lambda** (recommended, pending the maintainer)

### Rationale

- A wins because it is the only option that sees every Lambda trigger; B wins for a team that only uses the SQS and EventBridge helpers and refuses a wrapper call; C wins if the maintainer prefers to carry no cold-start code. The wrapper has two variants because the existing handlers return `Either<EncinaError, T>`: `TraceEitherAsync` marks the span as error with the error **code** on a `Left`, `TraceAsync` records the exception type and rethrows. Both return the handler's own result unchanged (a failure inside the instrumentation never changes a result).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. What "cold start" means</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The first invocation seen by the process** (one static `Interlocked.Exchange` per package) | Matches the OpenTelemetry FaaS convention (`faas.coldstart` is true on the first invocation of an execution environment) and the contrib wrappers; works on both platforms with no platform API | On Lambda with provisioned concurrency or SnapStart the first invocation is reported cold although the init ran before the invoke |
| **B) A, but `false` when `AWS_LAMBDA_INITIALIZATION_TYPE` is `provisioned-concurrency` or `snap-start` (Lambda only)** | Reflects what the caller experienced | Platform-specific branch; the variable and its values are documented by AWS and must be re-checked at implementation; Azure has no equivalent |
| **C) Cold when the invocation starts within a fixed time of the process start** | No flag | A magic number; wrong for a function that is idle after a long init |

### Chosen Option: **A - First invocation in the process** (recommended, pending the maintainer)

### Rationale

- A wins on simplicity and consistency across the two platforms; B wins for teams that use provisioned concurrency and read the ratio as a latency signal (a one-line change inside `ColdStartTracker`). If no listener is attached on the first invocation the flag is still consumed (the invocation is not cold later). The tracker is an instance (`ColdStartTracker.Process` is the static default) so tests control it.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Instrument names</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `encina.faas.invocations` and `encina.faas.invoke.duration` on the per-package meters** | The `encina.<area>.<name>` shape of the sibling plans (#723, #724); no clash with other instrumentation of the same function | Vendor FaaS dashboards that key on `faas.invocations` do not pick them up |
| **B) The OpenTelemetry FaaS metric names `faas.invocations` and `faas.invoke_duration`** | Recognised by backends | A semantic-convention name (still in development) owned by another instrumentation when the contrib wrapper is also enabled: two emitters, one name |

### Chosen Option: **A - `encina.faas.*`** (recommended, pending the maintainer)

### Rationale

- A wins now; B wins if the target backend ships FaaS dashboards on the semantic-convention names and Encina is the only emitter. Lowercase dotted names, seconds, no `_total` (ADR-054 settled point 4). The attribute keys `faas.name`, `faas.trigger`, `faas.coldstart`, `cloud.provider` and `cloud.platform` follow the conventions as the issue asks and live in one internal constants class per package (ADR-054 D3).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Incoming context for batches</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) SQS batch: the invocation span is a new trace; each record gets a consumer span from `TraceContextConsumer` (`TraceDispatchKind.Batch`) linked to the record's `traceparent`** | Reuses the single helper of #132 (ADR-054 D2: batches are a new trace with a link); each message is traceable end to end; no custom link logic | One span per record (up to 10 records on a standard queue, 10,000 with a batch window), costing nothing without a listener |
| **B) One invocation span with a link per record** | One span | Not expressible with the single-incoming helper; a link cap (the SDK default of 128) silently drops producers on large batches |

### Chosen Option: **A - Per-record consumer spans** (recommended, pending the maintainer)

### Rationale

- A wins on reuse and traceability; B wins when batches are very large and per-record spans are too many for the backend. HTTP (API Gateway headers, Azure `FunctionContext.TraceContext`) is a child of the incoming context, the `Send` rule of D2; EventBridge has no standard header slot, so its invocation is a new trace unless the producer passes a context (the producer side belongs to #725).

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | No new `EncinaError` code (error codes of the handler pass through, a thrown exception is recorded by type), so no ADR-062 category; the instrumentation never denies or retries, so there is no fail-closed gate | Settled | ADR-062; `AGENTS.md` section 3 |
| C2 | No new EventId: the Lambda wrapper calls the declared and unused `LambdaExecutionStarting`, `LambdaExecutionCompleted`, `LambdaExecutionFailed` (4700-4702); the Azure middleware keeps 4750-4752; 4711-4712 and 4759-4760 stay #744's; no new range, so nothing is added to the `Cloud and integration` block of the registry (80000-80149 is held by #4007 for these two packages and Refit) | Settled | `AGENTS.md` section 7; `docs/engineering/eventid-block-registry.md` |
| C3 | No tenant text, user id, request body, header value other than the W3C pair, `EncinaError.Message` or exception message in a tag; `faas.invocation_id` is a span tag only; no tenant-qualified id is built, so the reserved `default` segment does not apply | Settled | ADR-054 settled point 2 and D6.3; `AGENTS.md` section 3 |
| C4 | Zero cost without a listener: `ActivitySource.HasListeners()` before the span, `Instrument.Enabled` before each record; time from an injected `TimeProvider` (optional trailing parameter, default `TimeProvider.System`) | Settled | ADR-054 settled point 7; `AGENTS.md` section 3 |
| C5 | Only the W3C `traceparent` and `tracestate` are read; X-Ray `AWSTraceHeader` and the legacy `Diagnostic-Id` are ignored; baggage is never read or written | Settled | ADR-054 settled point 3 and D4 |
| C6 | An invalid or oversized incoming `traceparent` is ignored (the span starts a new trace); the invocation is never failed because of a telemetry input | Settled | ADR-054 settled point 7 (telemetry never changes a result); #132 (`RequestTraceContext.TryParse`) |
| C7 | Tenant tag: ADR-054 D4 requires it when multi-tenancy is on; it is added in its own pull request after #2676, read when the invocation ends, absent when no tenant is resolved or multi-tenancy is off | Settled | ADR-054 D4; #2676 |

---

## Implementation Phases

### Phase 1: Lambda Instrumentation (pull request F1)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Diagnostics/FaasTrigger.cs` (`public enum FaasTrigger { Http, PubSub, Timer, DataSource, Other }` with XML docs naming the OpenTelemetry `faas.trigger` values `http`, `pubsub`, `timer`, `datasource`, `other`); add its lines to `src/Encina/PublicAPI.Unshipped.txt` (through `mechanical-fixer`).
2. **Create** `src/Encina.AwsLambda/Telemetry/ColdStartTracker.cs` (`internal sealed`, `bool TryConsume()` using `Interlocked.Exchange`, `static ColdStartTracker Process { get; }`) and `Telemetry/LambdaTelemetry.cs` (`internal static`: `ActivitySource` and `Meter` named `Encina.AwsLambda`, version `1.0`, `encina.faas.invocations`, `encina.faas.invoke.duration`, the tag-key constants, `ToTagValue(FaasTrigger)` mapping to the closed set).
3. **Create** `src/Encina.AwsLambda/LambdaInstrumentation.cs` (`public static`): `Task<TResult> TraceAsync<TResult>(ILambdaContext context, FaasTrigger trigger, Func<Task<TResult>> handler, ILogger? logger = null, TimeProvider? timeProvider = null)` and `Task<Either<EncinaError, TResult>> TraceEitherAsync<TResult>(ILambdaContext context, FaasTrigger trigger, Func<Task<Either<EncinaError, TResult>>> handler, ILogger? logger = null, TimeProvider? timeProvider = null)`; plus an internal overload taking a `ColdStartTracker`. Each starts the span (null without a listener), sets the attributes of Design Choice 2, calls `Log.LambdaExecutionStarting/Completed/Failed` when a logger is supplied (the failure through `ex.ForLogging()`), records the counter and histogram in a `finally`, maps `Left` to `error.type` = the error code and an exception to its type name, and rethrows exceptions unchanged. Null `context` or `handler` throw `ArgumentNullException`; an out-of-range `trigger` throws `ArgumentOutOfRangeException`; null `logger` and `timeProvider` are valid.
4. **Modify** `src/Encina.AwsLambda/Encina.AwsLambda.csproj` (`InternalsVisibleTo` for `Encina.UnitTests`, `Encina.IntegrationTests` and `Encina.ContractTests`; the package has none today) and `src/Encina.AwsLambda/PublicAPI.Unshipped.txt` (through `mechanical-fixer`).
5. Tests: new `tests/Encina.UnitTests/AwsLambda/LambdaInstrumentationTests.cs` (a fake `ILambdaContext`; an `ActivityListener` and a `MeterListener`: success, `Left`, thrown exception rethrown unchanged, tags on span and metric, no `faas.invocation_id` on the metric, no listener returns the handler result with no work, the result is returned unchanged, logger null and supplied), new `tests/Encina.UnitTests/AwsLambda/ColdStartTrackerTests.cs` (first call true, then false, 64 concurrent calls give exactly one true), new `tests/Encina.GuardTests/Cloud/AwsLambda/LambdaInstrumentationGuardTests.cs` (null context, null handler, invalid trigger, null logger and null time provider valid with a valid-result test); manifests `Encina.AwsLambda.json` and `Encina.json` and changelog `changelog.d/727-lambda-telemetry.added.md` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #727, plan docs/plans/faas-opentelemetry-implementation-plan-727.md, phase 1 (Lambda).
src/Encina.AwsLambda has only static helpers (SqsMessageHandler, EventBridgeHandler, LambdaContextExtensions); Log.cs declares
LambdaExecutionStarting/Completed/Failed (4700-4702) and nothing calls them.

TASK:
1. Add the public enum Encina.Diagnostics.FaasTrigger in core (PublicAPI line through mechanical-fixer).
2. Add ColdStartTracker and LambdaTelemetry (internal) and the public static LambdaInstrumentation with TraceAsync and TraceEitherAsync.
3. Add InternalsVisibleTo (UnitTests, IntegrationTests, ContractTests) to Encina.AwsLambda.csproj.
4. Unit and guard tests with real listeners; manifests and changelog through mechanical-fixer.

KEY RULES:
- Never put a tenant, user, header value, request body, EncinaError.Message or exception message in a tag or log; faas.invocation_id is a span tag only.
- Return the handler's result unchanged; rethrow exceptions unchanged; zero work without a listener.
- Optional parameters last (CS1737); null logger and null TimeProvider are valid inputs and get valid-result tests.
- Use the existing Log events 4700-4702; add no EventId.

REFERENCE FILES:
- src/Encina.AwsLambda/Log.cs, LambdaContextExtensions.cs, SqsMessageHandler.cs
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

---

### Phase 2: Azure Functions Instrumentation (pull request F2, after F1 for `FaasTrigger`)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.AzureFunctions/Telemetry/ColdStartTracker.cs` and `Telemetry/AzureFunctionsTelemetry.cs` (`internal`; source and meter `Encina.AzureFunctions`; the same instruments and tag keys; `ResolveTrigger(FunctionContext)` reads the `BindingMetadata.Type` of the trigger binding in `context.FunctionDefinition.InputBindings` and maps `httpTrigger` to `Http`, `timerTrigger` to `Timer`, `queueTrigger`, `serviceBusTrigger`, `eventHubTrigger` and `eventGridTrigger` to `PubSub`, `blobTrigger` and `cosmosDBTrigger` to `DataSource`, and everything else (orchestration, activity, entity and unknown triggers) to `Other`).
2. **Modify** `EncinaFunctionMiddleware.cs`: a new optional trailing constructor parameter `TimeProvider? timeProvider = null` (existing `new EncinaFunctionMiddleware(options, logger)` sites compile: 5 in `EncinaFunctionMiddlewareTests.cs`, 2 in `EncinaFunctionMiddlewareIntegrationTests.cs`, 2 in `GuardClauseTests.cs`); `Invoke` starts the span before `EnrichContext`, sets the attributes, and records in a `finally`; the existing log calls and the `throw;` stay.
3. **Modify** `PublicAPI.Unshipped.txt` (the constructor line changes; through `mechanical-fixer`).
4. Tests: edit `tests/Encina.UnitTests/AzureFunctions/EncinaFunctionMiddlewareTests.cs` (span and instruments on success and on a throwing next delegate, trigger mapping for each binding type including an unknown one, cold start true once, no listener); new `tests/Encina.UnitTests/AzureFunctions/Telemetry/AzureFunctionsTelemetryTests.cs`; edit `tests/Encina.GuardTests/Cloud/AzureFunctions/GuardClauseTests.cs` (null `timeProvider` is valid: valid-result test) and `tests/Encina.IntegrationTests/Cloud/AzureFunctions/EncinaFunctionMiddlewareIntegrationTests.cs` (the composed pipeline emits one span per invocation); manifest `Encina.AzureFunctions.json` and changelog `changelog.d/727-azure-functions-telemetry.added.md` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #727 plan docs/plans/faas-opentelemetry-implementation-plan-727.md, phase 2 (Azure). F1 merged (FaasTrigger exists).

TASK:
1. Add ColdStartTracker and AzureFunctionsTelemetry (internal) and use them in EncinaFunctionMiddleware.Invoke (span, counter, histogram).
2. Map the trigger binding type to FaasTrigger; add an optional trailing TimeProvider constructor parameter.
3. Update unit, guard and integration tests; PublicAPI, manifest and changelog through mechanical-fixer.

KEY RULES:
- No tenant, user, header, message text in a tag; the existing Log calls and the rethrow stay; zero work without a listener.
- Keep every existing new EncinaFunctionMiddleware(options, logger) call site compiling.

REFERENCE FILES:
- src/Encina.AzureFunctions/EncinaFunctionMiddleware.cs, Log.cs
- tests/Encina.UnitTests/AzureFunctions/EncinaFunctionMiddlewareTests.cs
```

</details>

---

### Phase 3: Subscription and Cross-Platform Contract (pull request F3)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`: in `WithEncina`, `tracing.AddSource("Encina.AwsLambda")`, `tracing.AddSource("Encina.AzureFunctions")`, `metrics.AddMeter("Encina.AwsLambda")` and `metrics.AddMeter("Encina.AzureFunctions")` (strings, no project reference; the other plans of the family edit the same method, so this pull request rebases on whichever lands first).
2. **Modify** `tests/Encina.ContractTests/Encina.ContractTests.csproj`: ProjectReferences to `Encina.AwsLambda` and `Encina.AzureFunctions` (the project references neither today; counted).
3. **Create** `tests/Encina.ContractTests/Cloud/FaasTelemetryContractTests.cs`: the two packages emit the same instrument names, unit, tag keys and trigger values for success and failure.
4. Tests: edit `tests/Encina.UnitTests/OpenTelemetry/ServiceCollectionExtensionsTests.cs` (the four names are subscribed). Manifest `Encina.OpenTelemetry.json`, changelog `changelog.d/727-faas-telemetry-subscription.added.md` through `mechanical-fixer`; README sections of `src/Encina.AwsLambda/README.md` and `src/Encina.AzureFunctions/README.md` (how to wrap a handler, the metric and tag tables, cardinality) through `docs-writer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #727 plan docs/plans/faas-opentelemetry-implementation-plan-727.md, phase 3. F1 and F2 merged.

TASK:
1. Subscribe the two sources and two meters in WithEncina (strings only).
2. Add the two ProjectReferences to the contract test project and the contract test.
3. Edit the WithEncina unit test; manifest and changelog through mechanical-fixer; READMEs through docs-writer.

KEY RULES:
- Contract tests instantiate the real instrumentation of both packages and read real instruments through a MeterListener.

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs, tests/Encina.UnitTests/OpenTelemetry/ServiceCollectionExtensionsTests.cs
```

</details>

---

### Phase 4: Incoming Trace Context (pull request F4, after #132 and #186)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `LambdaInstrumentation.cs`: a new optional parameter `IReadOnlyDictionary<string, string>? headers = null` before `logger` (the existing call sites of F1 are inside F1's tests only, all updated here); when `trigger` is `Http` the span is started through `TraceContextConsumer.StartConsumer` with the `Send` rule and the case-insensitive `traceparent` and `tracestate` headers extracted with `IContextPropagator`.
2. **Modify** `SqsMessageHandler.cs` (both `ProcessBatchAsync` overloads): per record, a consumer span from `TraceContextConsumer.StartConsumer(..., TraceDispatchKind.Batch)` with the record's `traceparent` and `tracestate` message attributes (`record.MessageAttributes`), disposed after the record; no span without a listener; the batch loop and the batch response are untouched.
3. **Modify** `EncinaFunctionMiddleware.cs`: the parent comes from `context.TraceContext.TraceParent` and `TraceState` through the same helper (`Send` rule for HTTP, `Batch` for queue and Service Bus triggers).
4. **Modify** `PublicAPI.Unshipped.txt` of `Encina.AwsLambda` (the changed signatures); tests: edit `LambdaInstrumentationTests.cs`, `SqsMessageHandlerTests.cs`, `EncinaFunctionMiddlewareTests.cs` (valid, invalid and absent headers; per-record link for a three-record batch); manifests and changelog `changelog.d/727-faas-trace-context.added.md` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #727 plan docs/plans/faas-opentelemetry-implementation-plan-727.md, phase 4. F1-F3 merged; #132 (TraceContextConsumer,
IContextPropagator) and #186 (RequestTraceContext) merged.

TASK:
1. Parent the HTTP invocation span on the incoming traceparent; give each SQS record a consumer span linked to its traceparent
   message attribute (TraceDispatchKind.Batch); parent the Azure span on FunctionContext.TraceContext.
2. Update the tests and PublicAPI; manifests and changelog through mechanical-fixer.

KEY RULES:
- ADR-054 D2 only through TraceContextConsumer; W3C traceparent and tracestate only, never baggage or X-Ray.
- An invalid header is ignored; never fail an invocation because of telemetry input.

REFERENCE FILES:
- the plan of #132, src/Encina.AwsLambda/SqsMessageHandler.cs, src/Encina.AzureFunctions/EncinaFunctionMiddleware.cs
```

</details>

---

### Phase 5: Tenant Tag (pull request F5, after #2676)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `Telemetry/LambdaTelemetry.cs` and `Telemetry/AzureFunctionsTelemetry.cs`: the tenant tag from the helper of #2676 on the invocation span and both instruments, read in the `finally` when the invocation ends, only when multi-tenancy is on and a tenant resolved; absent otherwise.
2. Tests: edit `LambdaInstrumentationTests.cs` and `AzureFunctionsTelemetryTests.cs` (tenant on, off, blank, resolved after the span started); changelog `changelog.d/727-faas-telemetry-tenant.added.md` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #727 plan docs/plans/faas-opentelemetry-implementation-plan-727.md, phase 5. #2676 merged (shared tenant tag helper).

TASK:
1. Add the tenant tag through the helper, evaluated when the invocation ends.
2. Tests for tenant on, off, blank and late-resolved.

KEY RULES:
- Only when multi-tenancy is enabled; never baggage; never the tenant in a span name.

REFERENCE FILES:
- the helper of #2676, src/Encina.AwsLambda/Telemetry/, src/Encina.AzureFunctions/Telemetry/
```

</details>

---

### Phase 6: Proof Against the Local Emulators (pull request F6)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.IntegrationTests/Cloud/AwsLambda/LambdaTelemetryLocalStackIntegrationTests.cs` with `[Collection(LocalStackFixture.CollectionName)]` (the collection definition exists in this assembly): send three messages to a LocalStack SQS queue through `fixture.SqsClient` with `traceparent` message attributes, receive them, run `LambdaInstrumentation.TraceEitherAsync` around `SqsMessageHandler.ProcessBatchAsync` and assert one invocation span, three linked record spans, the instruments and the absence of any message text in a tag.
2. **Create** `tests/Encina.IntegrationTests/Cloud/AzureFunctions/FunctionsTelemetryAzuriteIntegrationTests.cs` with `[Collection(AzuriteFixture.CollectionName)]`: the composed worker pipeline of `EncinaFunctionMiddlewareIntegrationTests` with a queue message read from Azurite, asserting the span, the instruments and `faas.trigger` = `pubsub`. A run on the real Functions host is #3252's scope; this pull request states the same limit as `tests/Encina.IntegrationTests/Cloud/AzureFunctions/DurableFunctionsHost.md`.
3. Manifests through `mechanical-fixer` (the integration targets of the new files); changelog not needed (tests only).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Issue #727 plan docs/plans/faas-opentelemetry-implementation-plan-727.md, phase 6. F1-F4 merged.

TASK:
1. LocalStack-backed SQS test around SqsMessageHandler inside LambdaInstrumentation (record spans linked to the message attributes).
2. Azurite-backed queue test through the composed worker pipeline.

KEY RULES:
- Shared collection fixtures only (Cloud-LocalStack, Cloud-Azurite); never dispose the fixture; no Thread.Sleep; skip nothing silently.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs, AzuriteFixture.cs
- tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/AmazonSQSPublisherIntegrationTests.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| ADR-054 | Settled points 2, 3, 4, 7; D2, D3, D4, D6 | Error text, W3C only, names, zero cost, parent or link rule, tenant tag, no data-bearing values |
| OpenTelemetry FaaS semantic conventions | `faas.name`, `faas.trigger`, `faas.coldstart`, `faas.invocation_id`, `cloud.provider`, `cloud.platform` | Attribute keys the issue names |
| SPEC-000 section 6.1, REQ-028 | GCP is post-1.0 | Scope: AWS and Azure only |
| `AGENTS.md` section 5 (Cloud) | AWS and Azure triangle; GCP gap noted | Provider coverage |
| W3C Trace Context | `traceparent`, `tracestate` | Incoming context |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `EncinaFunctionMiddleware` | `src/Encina.AzureFunctions/EncinaFunctionMiddleware.cs` | The Azure invocation entry point; starts the span and records the metrics |
| `FunctionContext.TraceContext` | `Microsoft.Azure.Functions.Worker` (`TraceParent`, `TraceState`) | The incoming W3C context supplied by the Functions host |
| `SqsMessageHandler` | `src/Encina.AwsLambda/SqsMessageHandler.cs` | The static SQS batch handler; gets the per-record consumer span |
| `EventBridgeHandler` | `src/Encina.AwsLambda/EventBridgeHandler.cs` | The static EventBridge handler; no context parameter, traced through the wrapper |
| `LambdaContextExtensions` | `src/Encina.AwsLambda/LambdaContextExtensions.cs` | Reads function name, request id and remaining time from `ILambdaContext` |
| `Log` (AwsLambda, AzureFunctions) | `src/Encina.AwsLambda/Log.cs`, `src/Encina.AzureFunctions/Log.cs` | Existing lifecycle events 4700-4702 and 4750-4752 are reused; no new id |
| `WithEncina` | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` | Subscribes the new sources and meters |
| `TraceContextConsumer` | #132, namespace `Encina.Tracing` (not on main yet) | Applies the ADR-054 D2 parent or link rule |
| `LocalStackFixture`, `AzuriteFixture` | `tests/Encina.TestInfrastructure/Fixtures/` | Emulator proof (collections `Cloud-LocalStack`, `Cloud-Azurite`) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `AwsLambda` (existing 4700-4749) | none new | Reuses 4700-4702 (declared, unused today); 4711-4712 are #744's |
| `AzureFunctions` (existing 4750-4799) | none new | Reuses 4750-4752; 4759-4760 are #744's |
| Cloud and integration block | none | The maintainer's premise (the next free 50-slot from 80050) is superseded: 80000-80149 is held by #4007 (Refit, `AwsLambda`, `AzureFunctions`); no id is needed here |

### Estimated File Count

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| F1 | Lambda instrumentation | 4 (`FaasTrigger.cs`, `ColdStartTracker.cs`, `LambdaTelemetry.cs`, `LambdaInstrumentation.cs`) | 3 (`Encina.AwsLambda.csproj`, 2 PublicAPI) | 3 | 0 | 2 | 1 (changelog) | 13 | ~800 |
| F2 | Azure instrumentation | 2 | 2 (`EncinaFunctionMiddleware.cs`, PublicAPI) | 1 | 3 | 1 | 1 (changelog) | 10 | ~650 |
| F3 | Subscription and contract | 0 | 1 (`ServiceCollectionExtensions.cs`) | 1 | 1 | 1 | 4 (ContractTests csproj, 2 READMEs, changelog) | 8 | ~330 |
| F4 | Incoming context | 0 | 4 (`LambdaInstrumentation.cs`, `SqsMessageHandler.cs`, `EncinaFunctionMiddleware.cs`, PublicAPI) | 0 | 3 | 2 | 1 (changelog) | 10 | ~480 |
| F5 | Tenant tag | 0 | 2 | 0 | 2 | 0 | 1 (changelog) | 5 | ~140 |
| F6 | Emulator proof | 0 | 0 | 2 | 0 | 2 | 0 | 4 | ~380 |
| **Total** | | **6** | **12** | **7** | **9** | **8** | **8** | **50** | **~2,780** |

Columns sum: 6 + 12 + 7 + 9 + 8 + 8 = 50 = 13 + 10 + 8 + 10 + 5 + 4. Every pull request is within 15 files and 1500 lines and builds alone. Implementers and doubles: no interface changes; the constructor change of `EncinaFunctionMiddleware` is optional-trailing, and a grep of `new EncinaFunctionMiddleware(` on main finds 9 sites (5 + 2 + 2), all compiling unchanged. `FaasTrigger` is new, so no existing code references it.

Per-file coverage targets (`Encina.AwsLambda.json` package targets: guard 85, unit 55, integration 97; `Encina.AzureFunctions.json`: guard 15, unit 55, integration 94):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `Encina.AwsLambda/LambdaInstrumentation.cs` | 90 | 55 | 20 | 0 | 75 | Success, `Left`, exception, no-listener and logger paths run with real listeners; guard covers the three argument checks and the valid null optionals; the contract test adds the cross-platform tags; integration runs it around the SQS handler on LocalStack; property 0 because the logic is a straight sequence with no input space worth generating |
| `Encina.AwsLambda/Telemetry/LambdaTelemetry.cs` | 95 | 0 | 20 | 0 | 0 | Instrument creation and the trigger mapping table run in the unit tests; internal static with no validated parameter, so guard cannot reach it |
| `Encina.AwsLambda/Telemetry/ColdStartTracker.cs` | 100 | 0 | 0 | 0 | 0 | Two methods, both driven including a 64-way concurrent call; no parameters |
| `Encina/Diagnostics/FaasTrigger.cs` | 0 | 0 | 20 | 0 | 0 | An enum has no executable line; the contract test fixes the member set and the tag values |
| `Encina.AwsLambda/SqsMessageHandler.cs` (existing file) | stays | stays | 0 | 0 | 80 | Existing targets stay (never lowered); the per-record span lines are covered by unit tests and by the LocalStack run |
| `Encina.AzureFunctions/EncinaFunctionMiddleware.cs` (existing 100, 30, 100) | 100 | 30 | 20 | 0 | 100 | Targets stay; every new branch (each trigger mapping, error and no-listener) is driven with a mocked context and the composed pipeline |
| `Encina.AzureFunctions/Telemetry/AzureFunctionsTelemetry.cs` | 95 | 0 | 20 | 0 | 40 | Mapping and instruments are table-driven; internal static, no validated parameter; the Azurite pipeline adds the integration lines |
| `Encina.AzureFunctions/Telemetry/ColdStartTracker.cs` | 100 | 0 | 0 | 0 | 0 | Same as the Lambda copy |
| `Encina.OpenTelemetry/ServiceCollectionExtensions.cs` | stays | stays | 0 | 0 | 0 | The four added lines run in the existing `WithEncina` test, so the targets stay |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #727 - OpenTelemetry for AWS Lambda and Azure Functions - in six small pull requests (F1 to F6).

PROJECT CONTEXT:
- Encina .NET 10 / C# 14, pre-1.0, no compatibility layers. Edit with Edit/Write only; PublicAPI, manifests and changelog through
  mechanical-fixer; READMEs through docs-writer.
- ADR-054: lowercase dotted instrument names in seconds, no _total, error type or code only, W3C traceparent and tracestate only, no
  baggage, zero cost without a listener, tenant tag only when multi-tenancy is on (#2676 helper), D2 parent or link rule through the
  TraceContextConsumer of #132.

IMPLEMENTATION OVERVIEW:
1. F1: FaasTrigger (core), ColdStartTracker, LambdaTelemetry, public LambdaInstrumentation.TraceAsync/TraceEitherAsync.
2. F2: the same for Azure inside EncinaFunctionMiddleware (trigger from the binding metadata).
3. F3: WithEncina subscription and the cross-platform contract test.
4. F4 (after #132, #186): incoming context, per-record SQS spans. F5 (after #2676): tenant tag.
5. F6: LocalStack and Azurite integration proof.

KEY PATTERNS:
- No tenant, user, header value, body or message text in a tag; faas.invocation_id on spans only; no new EventId (reuse 4700-4702 and
  4750-4752).
- Keep every new EncinaFunctionMiddleware(options, logger) call site compiling; optional parameters last.
- Contract and integration tests run real instrumentation; shared collection fixtures only.

REFERENCE FILES:
- src/Encina.AzureFunctions/EncinaFunctionMiddleware.cs, src/Encina.AwsLambda/SqsMessageHandler.cs, src/Encina.AwsLambda/Log.cs
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md, the plans of #132 and #744
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Instrumentation observes invocations; it adds no cached read |
| 2 | OpenTelemetry | ✅ Phases 1-6 | This is the feature |
| 3 | Structured Logging | ✅ Phase 1 | The existing lifecycle events (4700-4702, 4750-4752) are called by the new entry points; no new EventId (register row C2) |
| 4 | Health Checks | ❌ N/A | `AwsLambdaHealthCheck` and `AzureFunctionsHealthCheck` exist; this feature provides no health status |
| 5 | Validation | ❌ N/A | The only input is a W3C header, validated by `RequestTraceContext.TryParse` of #186 and ignored when invalid |
| 6 | Resilience | ❌ N/A | Adds no external call and never retries; deadlines and retries are #744 |
| 7 | Distributed Locks | ❌ N/A | Acquires no lock |
| 8 | Transactions | ❌ N/A | Never commits or rolls back |
| 9 | Idempotency | ❌ N/A | A redelivered SQS message produces a second consumer span, which is correct |
| 10 | Multi-Tenancy | ✅ Phase 5 | Tenant tag through the helper of #2676, read at the end of the invocation; absent when none resolved |
| 11 | Module Isolation | ❌ N/A | No module data in an invocation |
| 12 | Audit Trail | ❌ N/A | No personal data may reach telemetry (register row C3) |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| ADR-054 accepted | ✅ On `main` | D2, D3, D4, D6 |
| #132 `TraceContextConsumer` and #186 `RequestTraceContext` | Open | Only F4 waits |
| #2676 tenant helper | Open | Only F5 waits |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Resilience of the same packages | #744 | Owns ids 4711-4712 and 4759-4760 and edits `Log.cs`; no code overlap with this plan |
| Producer side of the SQS and EventBridge carriers | #725 | Writes the `traceparent` message attribute F4 reads |
| Functions host smoke test | #3252 | The real host run; F6 uses Azurite and the composed worker pipeline |
| Google Cloud Functions telemetry | #2693 | Post-1.0 |
| Observability guide | #906 | Documents the cardinality of `faas.name` |

---

## Next Steps

1. Link this plan from #727 and ask the maintainer to answer Design Choices 1 to 5 when work starts.
2. Implement F1 and F2 first (they are independent apart from `FaasTrigger`), then F3; F4 after #132 and #186, F5 after #2676, F6 last.
3. Maintainer action: amend the acceptance criteria of #727 that name "Encina.OpenTelemetry (cloud function enrichers)" and the memory-usage motivation to the outcome of Design Choice 1 and the not-delivered list (this plan never comments on an issue).
