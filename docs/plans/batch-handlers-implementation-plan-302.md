# Implementation Plan: Batch Handlers and Automatic Request Batching (`Encina.Batching` package: IBatchHandler, collector, pipeline behavior, dispatcher)

> **Issue**: [#302](https://github.com/dlrivada/Encina/issues/302)
> **Type**: Feature
> **Complexity**: Medium (5 phases, 4 pull requests of 14, 13, 9 and 11 files)
> **Estimated Scope**: about 750 lines of production code and about 1,200 lines of tests, split over four pull requests
> **Family**: msg-batching (#121, #252, #302, #339, #349). This plan **owns** the `Encina.Batching` package skeleton, `IBatchHandler<TRequest, TResponse>`, `IBatchableRequest<TResponse>`, `IBatchCollector`, `[BatchOptions]`, `BatchFailureStrategy`, `BatchingOptions`, `AddEncinaBatching` and the `Batching` EventId range. #349 and #339 (wrapper commands, strategies, deduplication) and #121 (modes, outbox and inbox integration) build on it; #252 (bulk publish to transports) is independent.

---

## Summary

The issue asks for handlers that process many requests in one invocation, a collector that gathers concurrent requests by time and size, a pipeline behavior that feeds it, `[BatchOptions]`, three failure strategies and a `SendBatch` entry point. Nothing of it exists: `git grep` of `IBatchHandler`, `IBatchableRequest`, `IBatchCollector` and `BatchFailureStrategy` over `src`, `tests` and `docs` on `origin/main` (`822c310c7`) finds no declaration; the only batching in `src` is the outbox cycle (`src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs`, which fetches a batch and still publishes one message at a time) and the transport publishers (#252).

| Issue item | Today | Owner |
|------------|-------|-------|
| `IBatchHandler<TRequest, TResponse>` | Missing. #121 declares a second, different signature of the same name | This plan (Design Choice 2); #121 adopts it |
| `IBatchableRequest<TResponse>`, `IBatchCollector`, `BatchingPipelineBehavior`, `[BatchOptions]` | Missing | This plan |
| Time and size triggers (count and delay) | Missing; payload-size trigger and `BatchingMode` are #121 | This plan (count and delay), #121 (bytes, modes) |
| Failure strategies | Missing | This plan (Design Choice 7) |
| `IBatchDispatcher`, `SendBatch` | Missing. An extension on `IEncina` cannot work: `IEncina` exposes no service provider | This plan, `IBatchDispatcher` only (Design Choice 9) |
| Wrapper commands and queries, strategies, builder | Missing | #349 (and #339) |

Delivered in a new package `Encina.Batching` (Design Choice 1): the contracts, a singleton `BatchCollector` that keeps one buffer per `(request type, tenant, batch key)` and flushes by size or by delay with `TimeProvider`, `BatchingPipelineBehavior<,>` that enqueues a batchable request and completes it with its own result, `IBatchDispatcher` for a direct batch, a flush that runs in a fresh DI scope under a declared service identity, `AddEncinaBatching`, an adapter that gives every batch handler the single-item `IRequestHandler` the dispatcher needs (Design Choice 10), telemetry and logging (EventIds 7750-7757).

**Provider category**: none. The package touches no database, cache or transport. Handlers use whatever stores they like; the batch of a flush shares one DI scope, so one unit of work or transaction accessor is shared by the whole batch (it is the handler that commits).

---

## Design Choices

<details>
<summary><strong>1. Placement: a new package `Encina.Batching` (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new package `Encina.Batching` that references `Encina` only** | Pay-for-what-you-use: an application without batching loads no collector or behavior; the package name matches #339 and #349, which both name `Encina.Batching`; #121 (messaging) can reference it without the reverse dependency | A new package to create (csproj, solution entry, manifest, README, PublicAPI files, event range) |
| **B) Types in core `Encina` under the namespace `Encina.Batching`** | No new package; the issue lists core as the affected package | Core grows by a collector, a timer and options that most applications never use; breaks "every feature opt-in" in assembly terms |
| **C) Contracts in core, collector and behavior in `Encina.Batching`** | A handler assembly can implement `IBatchHandler` without referencing the package | Two homes for one feature; core carries a contract nobody consumes without the package |

### Chosen Option: **A, a new package `Encina.Batching`** (recommended, pending the maintainer)

### Rationale

A wins because #339 and #349 already name the package and the opt-in principle of `AGENTS.md` section 1 favors a package over a core addition. B wins if the maintainer wants no new package before 1.0. C wins for libraries that only ship handlers. The package has no dependency on `Encina.Messaging`; #121 adds that reference in the other direction.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. One `IBatchHandler<TRequest, TResponse>`, index-aligned results</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Task<IReadOnlyList<Either<EncinaError, TResponse>>> HandleBatch(IReadOnlyList<TRequest> requests, CancellationToken)` with `TRequest : IRequest<TResponse>`; result `i` answers request `i`** | The collector can complete each caller by position; same ROP shape as `IRequestHandler.Handle` | A handler must return exactly one result per request (a count mismatch fails the whole flush, see Phase 2) |
| **B) #121's `Task<IEnumerable<Either<EncinaError, TResponse>>> HandleAsync(...)`, no constraint** | Matches #121's text | An `IEnumerable` has no position contract, and the same type name cannot exist twice |

### Chosen Option: **A, the signature of #302**

### Rationale

One interface per concept: a second `IBatchHandler` would be a parallel alias, which `AGENTS.md` section 3 forbids, and only A lets the collector map a result to its caller. The method is named `HandleBatch` because `IRequestHandler<,>.Handle` (`src/Encina/Abstractions/IRequestHandler.cs`) carries no `Async` suffix; #121's plan uses this interface unchanged.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (no compatibility aliases or layers), #302 body ("Returns results in the same order as input requests") and `src/Encina/Abstractions/IRequestHandler.cs`.

</details>

<details>
<summary><strong>3. Two entry points: the pipeline behavior and `IBatchDispatcher`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Both: the behavior batches concurrent single `Send` calls of an `IBatchableRequest`; `IBatchDispatcher.SendBatch` hands a known list to the handler at once** | Meets both acceptance items ("automatic batching collects concurrent requests", "batch handler works with multiple requests") | Two paths to test |
| **B) Only the dispatcher** | Smaller | The automatic path of the title is missing |
| **C) Only the behavior** | Smaller | A caller that already holds a list must fake concurrency |

### Chosen Option: **A, both**

### Rationale

Per-item outer behaviors (validation, authorization, idempotency) run before the batching behavior because it sits innermost, so batching never removes a per-item check; the dispatcher path has no per-item pipeline and says so in its XML docs.

**Class:** Settled
**Settled by:** #302 acceptance criteria (both items are listed) and `src/Encina/Abstractions/IPipelineBehavior.cs` (behaviors wrap the handler and may return without calling `nextStep`).

</details>

<details>
<summary><strong>4. Grouping key: request type, tenant, batch key; a missing tenant fails closed</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An internal `readonly record struct BatchKey(Type RequestType, string? TenantId, string? Key)`; when the container holds `MultiTenancyMarker` and the context has no tenant, the request is rejected with `batching.tenant_required`; when multi-tenancy is off the tenant is null for everyone** | A batch never mixes tenants; the key is a structure, so no two `(tenant, key)` pairs collapse into one string (injective); the reserved `default` tenant segment is never composed into a string here | One more error code |
| **B) Key by `IBatchableRequest.BatchKey` only (the issue)** | Simplest | Two tenants that return the same `BatchKey` share a batch and a handler call |
| **C) Key by the concatenated string `{tenant}:{key}`** | One string | Not injective: tenant `a:b` with key `c` equals tenant `a` with key `b:c` |

### Chosen Option: **A, a structured key with fail-closed tenant**

### Rationale

`AGENTS.md` section 3 requires gates to deny when context is missing, and a cross-tenant batch is a data-isolation defect. `BatchKey` is user-influenced text, so the key is bounded (Design Choice 8) and never logged in full.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (fail-closed gates) and `src/Encina/Abstractions/MultiTenancyMarker.cs` (the container signal that multi-tenancy is on).

</details>

<details>
<summary><strong>5. Flush timing uses `TimeProvider` and never throws from the timer callback (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `ITimer` from `TimeProvider.CreateTimer` per open buffer, started by the first item, disposed on flush; the callback catches everything and completes the items with a `Left`** | Testable with `FakeTimeProvider`; follows the background rule | A timer per open buffer (bounded by Design Choice 8) |
| **B) A single `PeriodicTimer` loop scanning all buffers** | One timer | A scan of every buffer each tick; delay precision equals the tick |

### Chosen Option: **A, a timer per open buffer** (recommended, pending the maintainer)

### Rationale

`AGENTS.md` section 3 requires time from `TimeProvider` and forbids a timer callback to throw (an unhandled exception on the thread-pool thread terminates the process with the system clock), but it allows both options, so the choice is open. A wins when the number of open buffers stays small (bounded by D8) and delay precision matters; B wins when a deployment expects many tenants and keys at once and one timer is cheaper than thousands. Either way, flushes start with `ExecutionContext.SuppressFlow()` so the ambient request context of the caller that happened to trigger the flush never leaks into the batch (see D6).

**Class:** Dilemma

</details>

<details>
<summary><strong>6. The identity and context a flush runs under (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IRequestContextScopeFactory.RunAsServiceAsync(BatchingOptions.ServiceIdentity, ..., new IdentityScopeOptions { TenantId = batch tenant })`; the flush gets a fresh DI scope and a new correlation id; an undeclared service identity makes every item fail with `Left`** | Uses the repository's own mechanism for background work (`IRequestContextScopeFactory`); the handler sees the batch's tenant, never another; fails closed | The application must declare a service identity (`AddEncinaServiceIdentity`) and the handler runs with that identity's permissions, not each caller's |
| **B) Restore the first caller's actor for the whole batch** | Handlers see a real user | The other callers' actions run as someone else: an authorization bypass |
| **C) Anonymous context with the tenant** | No declaration needed | Handlers that authorize fail for everyone; audit shows no actor |

### Chosen Option: **A, a declared service identity per package option** (recommended, pending the maintainer)

### Rationale

A wins because only it is both safe and uses an existing mechanism; B wins only for single-caller batches, which `IBatchDispatcher` already serves in the caller's own context (it runs in the caller's flow, with the caller's context, one scope). C wins for handlers that never authorize. Because `RunAsServiceAsync` is refused inside a user flow, flushes run with execution-context flow suppressed (Design Choice 5). Per-item identity, when a handler needs it, belongs on the request (a field of the request type), not on the batch context.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Failure strategies and their relation to #349's strategies (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `BatchFailureStrategy { ReturnIndividual, FailAll, RetryFailed }` here (handler-level, applied by the collector to the results of one flush), separate from #349's `BatchStrategy` (wrapper-level execution)** | The layers answer different questions: what the collector does with a handler's per-item `Left`s, versus how a wrapper runs many commands | Two enums with similar words; the XML docs and the README must separate them |
| **B) One shared enum in this package** | One concept | `RetryFailed` and `StopOnFirstError` have no meaning in the other layer |

### Chosen Option: **A, two enums** (recommended, pending the maintainer)

### Rationale

A wins when the maintainer sees handler batching and wrapper commands as separate features; B wins if a single vocabulary is worth some unused members. `ReturnIndividual` (default) hands each caller its own result; `FailAll` turns every result into one `batching.batch_failed` `Left` when any item failed (the per-item errors are not copied into it); `RetryFailed` calls `HandleBatch` once more with the failed items only, once (no loop), and returns the second results, so no hidden retry policy exists (retry policy is resilience, see the matrix).

**Class:** Dilemma

</details>

<details>
<summary><strong>8. A bound on open buffers and pending items (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `BatchingOptions.MaxPendingItems` (default 10,000) across all buffers: when full, the new request gets `Left` `batching.queue_full` (category `RateLimited`)** | Memory is bounded even with hostile `BatchKey` values; callers decide to retry | A burst can be rejected |
| **B) Block the caller until space is free** | No rejection | Callers can stall indefinitely; with a slow handler this is a deadlock risk |
| **C) No bound** | Simplest | Unbounded memory by tenant and key cardinality |

### Chosen Option: **A, reject when full** (recommended, pending the maintainer)

### Rationale

A wins when callers can handle a `Left`; B wins when the producers are internal and slowing them is the desired backpressure (messaging backpressure belongs to the msg-backpressure family, not to this collector). The check is one atomic counter, so it costs nothing on the hot path.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. No `SendBatch` extension on `IEncina`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IBatchDispatcher` registered by `AddEncinaBatching`, injected where needed** | Works with the real `IEncina` contract | The call site injects one more service |
| **B) The issue's `EncinaBatchExtensions.SendBatch(this IEncina, ...)`** | Reads like `Send` | `IEncina` has no service provider member, so the extension has nothing to resolve a handler from; it would need a downcast to the concrete class |

### Chosen Option: **A, the dispatcher only**

### Rationale

`src/Encina/Abstractions/IEncina.cs` declares `Send`, `Publish` and stream members only; an extension cannot reach DI without breaking the abstraction.

**Class:** Settled
**Settled by:** `src/Encina/Abstractions/IEncina.cs` (no service access) and `AGENTS.md` section 3 (no compatibility layers).

</details>

<details>
<summary><strong>10. The single-item handler the dispatcher requires (Dilemma D10)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaBatching` scans the given assemblies for `IBatchHandler<,>` and registers an internal adapter as `IRequestHandler<TRequest, TResponse>` for each request type; the adapter calls the collector, so a batch handler is the only handler a batchable request needs** | One class per request type; the pipeline still reaches the behavior | A handler assembly must be scanned; the adapter is a hidden registration |
| **B) The application also writes a single-item `IRequestHandler<,>` for every batchable request (it may delegate to the batch handler with a one-item list)** | No hidden registration | Two classes per request type; forgetting one yields `HandlerMissing` at runtime |

### Chosen Option: **A, the adapter** (recommended, pending the maintainer)

### Rationale

`RequestDispatcher.ExecuteAsync` (`src/Encina/Dispatchers/Encina.RequestDispatcher.cs`) resolves and validates an `IRequestHandler<,>` before the pipeline runs and returns the `HandlerMissing` error otherwise (the scanner registers handlers under `IRequestHandler<,>`, `MediatorAssemblyScanner.cs:43`). The batching behavior never calls `nextStep`, but it is only reached when a handler exists. A wins when the maintainer wants the feature to need one class per request; B wins when no hidden registration is acceptable. With A the pipeline never invokes the adapter (the behavior short-circuits); if another pipeline reaches it, its `Handle` returns `batching.handler_missing` (`Internal`) as a `Left`, never an exception.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Package, contracts and options (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.Batching/Encina.Batching.csproj` (references `Encina`), the `Encina.slnx` entry, `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` (the latter through `mechanical-fixer`), `.github/coverage-manifest/Encina.Batching.json`, both test project references (`tests/Encina.UnitTests/Encina.UnitTests.csproj`, `tests/Encina.GuardTests/Encina.GuardTests.csproj`) and `src/Encina.Batching/README.md` (through `docs-writer`).
2. `IBatchHandler<TRequest, TResponse>` (`where TRequest : IRequest<TResponse>`): `Task<IReadOnlyList<Either<EncinaError, TResponse>>> HandleBatch(IReadOnlyList<TRequest> requests, CancellationToken cancellationToken)`.
3. `IBatchableRequest<TResponse> : IRequest<TResponse>` with `string? BatchKey { get; }`.
4. `[BatchOptions]` (`AttributeUsage` class, `MaxBatchSize` default 100, `MaxDelayMs` default 50, `FailureStrategy`) and `BatchFailureStrategy` (D7).
5. `BatchingOptions` (`MaxBatchSize` 100, `MaxDelay` 50 ms, `FailureStrategy`, `MaxPendingItems` 10,000, `ServiceIdentity` (string, null until set), `ShutdownTimeout` 5 s) with a `Validate` method: sizes above zero and at most 10,000, delays above zero and at most 5 minutes, `ServiceIdentity` required when any batchable request is registered (checked by the collector's registration test in pull request 2).
6. Tests: unit `BatchingOptionsTests` (defaults, validation boundaries, attribute defaults and the way an attribute overrides the global option, a table of `[Theory]` rows).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina has no batching today. D1 (new package Encina.Batching, references Encina only), D2 (one IBatchHandler, index-aligned results) and D7 (BatchFailureStrategy) apply as recommended until the maintainer answers.
TASK: Create the package skeleton, the contracts, the attribute, the enum and BatchingOptions with their unit tests.
KEY RULES: C# 14, nullable enabled; XML docs with examples on every public member; no [Obsolete]; zero warnings; every public API line in PublicAPI.Unshipped.txt (through mechanical-fixer); manifest entry with per-flag targets and one-sentence justifications; Shouldly through Encina.Testing.Shouldly.
REFERENCE FILES: src/Encina/Abstractions/IRequestHandler.cs, src/Encina/Abstractions/IPipelineBehavior.cs, src/Encina.Messaging/Outbox/OutboxOptions.cs (options validation style), .github/coverage-manifest/Encina.Messaging.json.
```

</details>

### Phase 2: Collector, behavior and observability (pull request 2)

<details>
<summary>Tasks</summary>

1. `IBatchCollector` (public): `ValueTask<Either<EncinaError, TResponse>> EnqueueAsync<TRequest, TResponse>(TRequest request, BatchOptionsAttribute? options, IRequestContext context, CancellationToken cancellationToken)` where `TRequest : IBatchableRequest<TResponse>`.
2. `BatchCollector` (internal sealed, singleton, `IAsyncDisposable`): constructor `(IServiceScopeFactory, IRequestContextScopeFactory, IOptions<BatchingOptions>, TimeProvider, ILogger<BatchCollector>)` (every dependency is a singleton or a factory; no scoped service is injected). Buffers live in a `ConcurrentDictionary<BatchKey, Buffer>`; a `Buffer` holds the pending items (request plus a `TaskCompletionSource<Either<...>>` created with `RunContinuationsAsynchronously`) and its `ITimer`. Flush on size (the enqueue that fills the buffer detaches it) or on the timer; the flush is started with `ExecutionContext.SuppressFlow()` (D5) and runs D6's `RunAsServiceAsync`; inside, a new scope resolves `IBatchHandler<TRequest, TResponse>` (a missing handler completes every item with `batching.handler_missing`, category `Internal`, ADR-062 R4). Result handling: a count different from the request count completes every item with `batching.result_count_mismatch` (`Internal`); an exception completes every item with `batching.flush_failed` (`Internal`, exception type only through `ForLogging()`, never the message); D7 strategies run here, in a method of complexity at most 8. Cancellation: a caller token cancelled before the flush removes the item and cancels its task; after the flush started the handler runs anyway and the caller's await ends with `OperationCanceledException` (cancellation is not an error). `DisposeAsync` flushes every open buffer, waiting at most `ShutdownTimeout`; an enqueue after disposal returns `batching.collector_stopped` (`Unavailable`).
3. `BatchingPipelineBehavior<TRequest, TResponse>` (`where TRequest : IBatchableRequest<TResponse>`, public sealed, annotated `[PipelineStage(PipelineStage.HandlerPreparation)]`, the innermost stage, `src/Encina/Pipeline/Ordering/PipelineStage.cs`): reads `[BatchOptions]` once per closed type (static field), takes tenant from `IRequestContext.TenantId`, applies D4's fail-closed rule (`IServiceProviderIsService` check of `MultiTenancyMarker` done once at construction), then returns `collector.EnqueueAsync(...)`; it never calls `nextStep`.
4. `Diagnostics/BatchingLog.cs` (`[LoggerMessage]`, EventIds 7750-7757): 7750 request enqueued (Debug), 7751 batch flushed (Debug, size and reason), 7752 flush failed by exception type (Error), 7753 result count mismatch (Error), 7754 handler missing (Error), 7755 item cancelled before the flush (Debug), 7756 failed items retried (Information), 7757 pending items flushed at disposal (Warning, count). `Diagnostics/BatchingTelemetry.cs`: `ActivitySource` and `Meter` named `Encina.Batching` (ADR-054); `encina.batching.batches` and `encina.batching.items` counters (tags `request.type`, `flush.reason`, and the tenant tag only when multi-tenancy is on, ADR-054 D4), histograms `encina.batching.batch_size` and `encina.batching.flush_duration` (unit `s`); no instrument name ends in `_total` (ADR-054 rule 4).
5. `src/Encina/Diagnostics/EventIdRanges.cs`: `Batching = (7750, 7779)` (assigned block 7750-7799; #349 uses 7758-7765, #339 7766-7769, 7770-7779 stay in the range for them), the core `src/Encina/PublicAPI.Unshipped.txt` line, and the `Encina.Batching` entry of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
6. Tests: unit `BatchCollectorTests`, which drives the internal collector through the public `IBatchCollector` and covers its null checks there (size flush, delay flush with `FakeTimeProvider` and a gate instead of sleeping, tenants never share a batch, `a:b`/`c` versus `a`/`b:c` stay separate, missing tenant with the marker, count mismatch, exception, each strategy, queue full, cancellation before and after flush, dispose drains, enqueue after dispose), `BatchingPipelineBehaviorTests` (innermost stage attribute, never calls `nextStep`, attribute overrides options), guard `BatchingPipelineBehaviorGuardTests` (the public constructor's null checks); manifest entries. The internal collector has no guard test file (the type is internal); its null checks are unit-tested through `IBatchCollector`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Pull request 1 added the contracts and options. D4 (structured key, fail-closed tenant), D5 (TimeProvider timers that never throw), D6 (flush as a declared service identity), D7 (failure strategies) and D8 (bounded pending items) apply as recommended until the maintainer answers.
TASK: Implement the collector, the pipeline behavior (annotated with [PipelineStage(PipelineStage.HandlerPreparation)]), the log, the telemetry, the EventId range and their tests. Registration is pull request 3.
KEY RULES: No scoped service injected into a singleton (use IServiceScopeFactory); time only from TimeProvider; a timer callback and a flush task never let an exception escape and complete every item with a Left; never log or return an exception message or EncinaError.Message; cancellation is not an error; flushes start with ExecutionContext flow suppressed; no Thread.Sleep in tests; every method complexity 8 or less (CRAP <= 10); EventIds only 7750-7757 inside the registered range.
REFERENCE FILES: src/Encina/Identity/IRequestContextScopeFactory.cs, src/Encina/Abstractions/MultiTenancyMarker.cs, src/Encina/Pipeline/Ordering/PipelineStageAttribute.cs, src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs.
```

</details>

### Phase 3: Registration and the single-item adapter (pull request 3)

<details>
<summary>Tasks</summary>

1. `ServiceCollectionExtensions.AddEncinaBatching(this IServiceCollection, Action<BatchingOptions>? configure = null, params Assembly[] assemblies)`: `TryAddSingleton` of the collector (an `IBatchCollector` and the internal type resolve to the same instance); `AddOptions<BatchingOptions>().Configure(configure).Validate(...).ValidateOnStart()`; registers the behavior the way satellites do, `services.AddScoped(typeof(IPipelineBehavior<,>), typeof(BatchingPipelineBehavior<,>))` (as `MessagingServiceCollectionExtensions.cs:105` does for the transaction behavior), because `AddEncinaBatching` takes an `IServiceCollection`, not an `EncinaConfiguration`; the stage comes from the behavior's `[PipelineStage]` attribute (Phase 2 task 3). The open-generic registration is skipped by the container for requests that are not `IBatchableRequest<TResponse>`.
2. The internal adapter `BatchRequestHandlerAdapter<TRequest, TResponse>` (D10): `AddEncinaBatching` scans the given assemblies for `IBatchHandler<,>` implementations and registers one `IRequestHandler<TRequest, TResponse>` adapter per request type (scoped). Without an `IRequestHandler`, `RequestDispatcher` returns `HandlerMissing` before the pipeline reaches the behavior.
3. Tests: DI `BatchingRegistrationTests` builds the provider with `ValidateOnBuild` and `ValidateScopes`, with and without `AddEncina`, in both orders, and sends a batchable request through `IEncina.Send` to prove the adapter lets the request reach the behavior; unit `BatchRequestHandlerAdapterTests`; guard `ServiceCollectionExtensionsGuardTests` (null `services`); manifest, PublicAPI.Unshipped (through `mechanical-fixer`), README edit (`docs-writer`), changelog fragment `changelog.d/302-batch-handlers.added.md` (`mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: The collector and behavior exist (pull request 2). D10 (single-item adapter) applies as recommended until the maintainer answers.
TASK: Add AddEncinaBatching, the adapter and their DI, unit and guard tests.
KEY RULES: Register the behavior with services.AddScoped(typeof(IPipelineBehavior<,>), ...), never through EncinaConfiguration; no scoped service injected into the singleton collector; DI test with ValidateOnBuild and ValidateScopes through IEncina.Send; Left, never an exception, from the adapter.
REFERENCE FILES: src/Encina.Messaging/MessagingServiceCollectionExtensions.cs, src/Encina/Dispatchers/Encina.RequestDispatcher.cs, src/Encina/Dispatchers/MediatorAssemblyScanner.cs.
```

</details>

### Phase 4: Direct dispatcher (pull request 4)

<details>
<summary>Tasks</summary>

1. `IBatchDispatcher.SendBatch<TRequest, TResponse>(IReadOnlyList<TRequest> requests, CancellationToken)` returning `ValueTask<Either<EncinaError, IReadOnlyList<Either<EncinaError, TResponse>>>>` (outer `Left` only for a missing handler, an empty or oversized list (`batching.batch_too_large`, `InvalidRequest`), or a result-count mismatch) and `BatchDispatcher` (internal sealed, scoped: it resolves `IBatchHandler<,>` from its own scope, which is the caller's scope, so the caller's unit of work is shared). No per-item pipeline runs; the XML docs say so.
2. Registered by `AddEncinaBatching` (scoped), `TryAdd` (an edit of `ServiceCollectionExtensions.cs` from pull request 3 and one more assertion in `BatchingRegistrationTests`).
3. Tests: unit `BatchDispatcherTests` (includes its argument checks through `IBatchDispatcher`; there is no guard test file because the type is internal), property `BatchCollectorPropertyTests` (every enqueued item receives exactly one completion; result `i` always belongs to request `i`; tenants never share a flush), PropertyTests project reference; manifest, PublicAPI.Unshipped (through `mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: The collector and behavior exist. The dispatcher is the explicit entry for a known list (D3, D9).
TASK: Add IBatchDispatcher and BatchDispatcher with unit, guard and property tests.
KEY RULES: Scoped service, never injected into a singleton; Left for empty, oversized and mismatched results; no exception text in errors; XML docs state that per-item behaviors do not run on this path.
REFERENCE FILES: src/Encina.Batching/BatchCollector.cs, tests/Encina.PropertyTests (an existing FsCheck class), .github/coverage-manifest/Encina.Batching.json.
```

</details>

### Phase 5: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. OpenTelemetry (function 2) and logging (function 3): Phase 2 task 4.
2. Multi-tenancy (function 10): D4 and the tenant tag of ADR-054 D4; the reserved `default` tenant segment is not used as an identifier here (the tenant is a field of a structured key).
3. Validation (function 5): per-item validation already runs because the behavior is innermost; the dispatcher path does not validate (documented), and `IValidationProvider` for batches is the existing validation family's work.
4. Errors (ADR-062): `batching.flush_failed` `Internal`, `batching.result_count_mismatch` `Internal`, `batching.handler_missing` `Internal` (R4, a missing handler), `batching.tenant_required` `InvalidRequest` (R3, a missing tenant; R2 also forbids `FailedPrecondition` before #3420), `batching.queue_full` `RateLimited`, `batching.collector_stopped` `Unavailable`, `batching.batch_too_large` `InvalidRequest`, `batching.batch_failed` `Internal` (FailAll). No message of an exception or `EncinaError` reaches a log, tag or result.
5. Deferred items: caching, health check, resilience, locks, idempotency, module isolation and audit (matrix below).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Integrations are decisions recorded above; no store is added.
TASK: Verify each integration exists in code and tests and that every new error code has its ErrorCategory.
KEY RULES: Exception type only in logs; tenant tag only when multi-tenancy is on; fail closed without a tenant.
REFERENCE FILES: AGENTS.md sections 3 and 6, docs/architecture/adr/062-error-category-contract.md, docs/architecture/adr/054-observability-conventions-and-trace-propagation.md.
```

</details>

### Phase 6: Testing and documentation

<details>
<summary>Tasks</summary>

Per-flag targets, entered in `.github/coverage-manifest/Encina.Batching.json` in the pull request that adds each file; the package is new, so there is no baseline (each target is the measured value or higher, measured with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests` and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`).

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `BatchCollector.cs` | 92 | 0 | 0 | 80 | 0 |
| `BatchingPipelineBehavior.cs` | 95 | 80 | 0 | 0 | 0 |
| `ServiceCollectionExtensions.cs` | 95 | 90 | 0 | 0 | 0 |
| `BatchRequestHandlerAdapter.cs` | 95 | 0 | 0 | 0 | 0 |
| `BatchDispatcher.cs` | 95 | 0 | 0 | 0 | 0 |
| `BatchingOptions.cs`, `BatchOptionsAttribute.cs` | 100 | 0 | 0 | 0 | 0 |
| `Diagnostics/BatchingLog.cs`, `BatchingTelemetry.cs` | 80 | 0 | 0 | 0 | 0 |
| `IBatchHandler.cs`, `IBatchableRequest.cs`, `IBatchCollector.cs`, `IBatchDispatcher.cs`, `BatchFailureStrategy.cs` | 0 | 0 | 0 | 0 | 0 |

| File and flag | Justification |
|---------------|---------------|
| `BatchCollector.cs` unit 92, property 80 | Size and delay flushes, strategies, cancellation and disposal run with fakes and `FakeTimeProvider`; the property test covers ordering and completion; the rest is the defensive catch of the timer callback. Guard 0: the type is internal, with no `InternalsVisibleTo` for `Encina.GuardTests` (verified at work start; raised if present). |
| `BatchingPipelineBehavior.cs` unit 95, guard 80 | A small public class: every branch runs in unit tests; guard tests reach the constructor's null checks. |
| `BatchRequestHandlerAdapter.cs` unit 95 | The adapter's `Handle` returns one `Left`; unit tests call it directly. Guard 0: internal type. |
| `ServiceCollectionExtensions.cs` unit 95, guard 90 | The DI test builds the provider in each order; guard tests reach the null checks. |
| `BatchDispatcher.cs` unit 95 | Missing handler, empty, oversized and mismatched lists all run in unit tests; guard 0: internal type. |
| Options and attribute unit 100 | Pure data and validation. |
| Logging and telemetry unit 80 | Exercised through the collector tests; generated logger code is not directly reachable. |
| Interfaces and enum 0 in every flag | Interfaces and an enum have no executable lines; contract tests of the interface arrive with the first implementer outside the package. |

1. **Unit, Guard, Property** as in the phases; **Contract 0**: no implementer outside this package yet (the first is #349's `BatchCommandHandler` through its own executor, not `IBatchHandler`).
2. **Integration**: skipped with a reason, as `AGENTS.md` section 9 allows for a non-database feature: there is no external service, and a unit test with a real container (`AddEncina` plus `AddEncinaBatching`, `IEncina.Send` called concurrently) covers the end-to-end path.
3. **Load**: justification file `tests/Encina.LoadTests/Batching/Batching.md`: the collector's concurrency is exercised by the property test and by a unit test with parallel enqueues; a load run is justified only once a batch handler with a real store exists. **Benchmark**: justification file `tests/Encina.BenchmarkTests/Batching/Batching.md`: the benefit is measured in the handler's own bulk operation; a micro-benchmark of the collector alone would not support a decision.
4. **Documentation**: README sections for the collector (pull request 3) and the dispatcher (pull request 4) through `docs-writer`; changelog fragment `changelog.d/302-batch-handlers.added.md` (pull request 3) through `mechanical-fixer`; `docs/INVENTORY.md` line (pull request 4, the file exists on `origin/main`); the feature page `docs/features/request-batching.md` is owned by #349.
5. `dotnet build --configuration Release` with zero warnings; all tests green; every flag at its target; CRAP of every changed method below 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Per-flag targets live in .github/coverage-manifest/Encina.Batching.json; targets never go down; tests execute real code.
TASK: Write the tests, the two justification files, the manifest entries with one-sentence justifications in the same pull request as their tests, and the README and changelog fragment through the specialists that own them.
KEY RULES: Shouldly through Encina.Testing.Shouldly; no Thread.Sleep (gate with TaskCompletionSource and FakeTimeProvider); justification files only for load and benchmark; never edit CHANGELOG.md; no hand-typed coverage figures in docs.
REFERENCE FILES: .github/coverage-manifest/Encina.Messaging.json, tests/Encina.UnitTests/Messaging, changelog.d/README.md, .claude/skills/encina-docs/SKILL.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` section 3 | `TimeProvider`, timers never throw, fail-closed gates, no aliases, no swallowed errors | D2, D4, D5 |
| ADR-021 | EventId ranges registered first | `Batching` 7750-7779 |
| ADR-054 | `ActivitySource` and `Meter` named `Encina.*`; tenant tag when multi-tenancy is on | Phase 2 task 4 |
| ADR-062 | Error category on every new code | Phase 5 |
| ADR-052 (cancellation propagates) | Cancellation is not an error | Collector cancellation |
| Wolverine batch handlers; DataLoader (named by the issue) | Comparable products for batch handlers and per-key batching | Issue references |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IRequest<T>`, `IRequestHandler<,>`, `IPipelineBehavior<,>` | `src/Encina/Abstractions/` | Contracts the new types mirror |
| `PipelineStageAttribute`, `PipelineStage.HandlerPreparation` | `src/Encina/Pipeline/Ordering/` | Places the behavior at the innermost stage; registration is `services.AddScoped(typeof(IPipelineBehavior<,>), ...)` as in `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs` |
| `IRequestContextScopeFactory.RunAsServiceAsync` | `src/Encina/Identity/IRequestContextScopeFactory.cs` | Flush identity (D6) |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | Detects multi-tenancy without a package reference |
| `Encina.RequestDispatcher` creating a scope per `Send` | `src/Encina/Dispatchers/Encina.RequestDispatcher.cs` | Why batching needs its own scope per flush |
| `OutboxBatchProcessor` | `src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs` | Existing batch fetch, reused by #121 |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/`, `tests/Encina.UnitTests/Testing/Architecture/` | Range registration |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Batching` (`Batching`, new) | 7750-7779 registered; this plan uses 7750-7757 (all eight used) | From the assigned block 7750-7799 (registry `artifacts/decisions/plan-batch/eventid-blocks.md`); #349 7758-7765, #339 7766-7769, 7770-7779 spare, 7780-7799 left to the family |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 17 | csproj, 3 contracts, attribute, enum, options (PR1); `IBatchCollector`, `BatchCollector`, behavior, log, telemetry, `EventIdRanges` (PR2); extensions, adapter (PR3); `IBatchDispatcher`, `BatchDispatcher` (PR4, plus an edit of the extensions) |
| Public API, manifest, solution, project references | 8 | 2 PublicAPI files, core PublicAPI line, manifest, `Encina.slnx`, 2 test csproj (PR1), property csproj (PR4) |
| Tests | 10 | options unit, collector unit, behavior unit, behavior guard, DI, adapter unit, extension guard, dispatcher unit, property, allocation map edit |
| Justification files | 2 | load, benchmark (PR4) |
| Documentation | 3 | README, changelog fragment, inventory |
| **Pull request 1** | **14** | csproj, slnx, `PublicAPI.Shipped`, `PublicAPI.Unshipped`, `IBatchHandler`, `IBatchableRequest`, `BatchOptionsAttribute`, `BatchFailureStrategy`, `BatchingOptions`, manifest, 2 test csproj, README (created here), `BatchingOptionsTests`; about 450 lines |
| **Pull request 2** | **13** | 5 production (`IBatchCollector`, `BatchCollector`, behavior, log, telemetry), `EventIdRanges`, core PublicAPI line, allocation map test, Batching PublicAPI.Unshipped, manifest, 3 tests (collector, behavior, behavior guard); about 1,000 lines |
| **Pull request 3** | **9** | extensions, adapter, PublicAPI.Unshipped, manifest, 3 tests (DI, adapter unit, extension guard), README edit, changelog fragment; about 450 lines |
| **Pull request 4** | **13** | 2 production (`IBatchDispatcher`, `BatchDispatcher`), extensions edit, PublicAPI.Unshipped, manifest, property csproj, 3 tests (dispatcher unit, collector property, DI test edit), 2 justification files, README edit, inventory line; about 650 lines |

Callers and implementers, by grep on `origin/main` (2026-11-18): the new types have no existing caller or implementer; `IBatchHandler` and `IBatchableRequest` do not exist, so no test double of an existing interface needs a change. The behavior is registered with `services.AddScoped(typeof(IPipelineBehavior<,>), ...)`, the way `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs` does; `EncinaConfiguration.AddPipelineBehavior` is not used because `AddEncinaBatching` has no `EncinaConfiguration`. Every pull request adds production code only together with its own tests and builds alone: PR1 is contracts and options, PR2 depends on PR1, PR3 on PR2 and PR4 on PR3.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP (Either<EncinaError, T>). IEncina.Send creates a DI scope per request; there is no batching today. The new package Encina.Batching references Encina only.

IMPLEMENTATION OVERVIEW: PR1 package skeleton, IBatchHandler (index-aligned results), IBatchableRequest, [BatchOptions], BatchFailureStrategy, BatchingOptions; PR2 BatchCollector (structured key of request type, tenant and key; TimeProvider timers; flush as a declared service identity in a fresh scope; D7 strategies; bounded pending items), BatchingPipelineBehavior (innermost stage by attribute, never calls nextStep), log 7750-7757, telemetry, the Batching EventId range; PR3 AddEncinaBatching (behavior registered with services.AddScoped) and the single-item IRequestHandler adapter; PR4 IBatchDispatcher and the property test. Dilemmas D1, D5, D6, D7, D8 and D10 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: Time from TimeProvider; timers and flush tasks never throw and complete every item with a Left; no scoped service in a singleton; tenant in the key and fail closed without it; exception type only in logs; cancellation is not an error; targets never go down; CRAP <= 10.

REFERENCE FILES: src/Encina/Abstractions/IPipelineBehavior.cs, src/Encina/Identity/IRequestContextScopeFactory.cs, src/Encina.Messaging/MessagingServiceCollectionExtensions.cs, src/Encina/Diagnostics/EventIdRanges.cs, .github/coverage-manifest/Encina.Messaging.json, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Batching processes writes and reads handed to the handler; a cache in front of a batch handler is the handler's choice, and the cache family (#695 and siblings) owns cache layers. |
| 2 | OpenTelemetry | ✅ | `ActivitySource` and `Meter` `Encina.Batching`, counters and histograms with the tenant tag only when multi-tenancy is on (ADR-054). |
| 3 | Structured Logging | ✅ | `BatchingLog`, EventIds 7750-7757 in the registered `Batching` range. |
| 4 | Health Checks | ❌ | The collector is in-process and has no external dependency to check; saturation is visible through the pending counter metric. |
| 5 | Validation | ✅ | Per-item validation behaviors run before the innermost batching behavior; the dispatcher path states that it does not validate. |
| 6 | Resilience | ⏭️ | `RetryFailed` is a single re-invocation, not a policy; retry, timeout and circuit breaking of a batch belong to the resilience family (#742 for transports); a retry policy over a flush needs its own issue. |
| 7 | Distributed Locks | ❌ | Buffers are per process; coordinating batches across instances is not part of this feature. |
| 8 | Transactions | ✅ | A flush runs the handler in one fresh DI scope, so one `IDbTransactionAccessor` or unit of work is shared by the whole batch; the handler commits (nothing here calls `SaveChanges`), and a per-batch transaction wrapper for batchable requests is #349's AllOrNothing seam. |
| 9 | Idempotency | ⏭️ | Per-item deduplication is #339 (within one batch) and #733 (notification idempotency); not part of the collector. |
| 10 | Multi-Tenancy | ✅ | The tenant is part of the structured key; a missing tenant is rejected when multi-tenancy is on (D4). |
| 11 | Module Isolation | ⏭️ | A module identifier on a batch key is not needed for correctness; module-scoped batching waits for the module family (#747 for messaging entities). |
| 12 | Audit Trail | ⏭️ | The service identity of the flush is the actor; per-caller audit of batched operations is part of the audit family (#749); the request types are handlers' own concern. |

---

## Prerequisites & Dependencies

- None blocking. `Encina.Batching` depends only on `Encina` and `Microsoft.Extensions.*` packages already in `Directory.Packages.props`.
- **Required by**: #349 and #339 (package, `BatchOptionsAttribute` conventions, `AddEncinaBatching`), #121 (`IBatchHandler`, the collector and its options). Open them in this order: #302, #349, #339, #121.
- **Independent**: #252 (transport bulk publish).
- **Related, not blocking**: the resilience family (#742) and the validation family (#729) for deferred integrations.

---

## Next Steps

1. Ask the maintainer D1, D5, D6, D7, D8 and D10 when work on #302 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: package skeleton, contracts and options (14 files).
3. Pull request 2: collector, behavior, observability and the EventId range (13 files).
4. Pull request 3: registration, single-item adapter, README and changelog (9 files).
5. Pull request 4: dispatcher, property test, justification files, README and inventory (13 files).
