# Implementation Plan: `ScheduledMessageProcessor` — Missing Background Dispatcher for Scheduled Messages

> **Issue**: [#765](https://github.com/dlrivada/Encina/issues/765)
> **Type**: Technical Debt (Critical, Foundational)
> **Complexity**: Medium (7 phases, provider-agnostic implementation + DI wiring across messaging satellites)
> **Estimated Scope**: ~800-1,100 lines of production code + ~1,200-1,600 lines of tests
> **Milestone**: v0.13.4
> **Provider Category**: None (provider-agnostic; lives in `Encina.Messaging` core and depends only on the `IScheduledMessageStore` abstraction)

---

## Summary

Implement the `ScheduledMessageProcessor` — a `BackgroundService` that closes the final gap in the scheduling subsystem. Today, `IScheduledMessageStore` and its **8 provider implementations** (ADO ×3, Dapper ×3, EF Core, MongoDB) persist due messages correctly, and `SchedulerOrchestrator` already exposes `ProcessDueMessagesAsync(executeCallback)` with batch retrieval, deserialization, recurring support and store updates. **What is missing is the background service that periodically invokes the orchestrator and wires the execute callback to `IEncina.Send<>()` / `IEncina.Publish()`** — plus three latent design issues in the orchestrator that this PR fixes properly (not patched).

Without this processor, scheduled messages are persisted but **never executed** — the entire scheduling feature is inert. This PR delivers:

1. A `ScheduledMessageProcessor : BackgroundService` registered automatically when `config.UseScheduling = true` AND `SchedulingOptions.EnableProcessor = true`
2. A pluggable **`IScheduledMessageRetryPolicy`** abstraction with `ExponentialBackoffRetryPolicy` as default — replacing the orchestrator's current hardcoded flat-delay retry with a strategy that can be swapped (or eventually unified with `Encina.Polly` / `Encina.Extensions.Resilience`)
3. A pluggable **`IScheduledMessageDispatcher`** abstraction with `CompiledExpressionScheduledMessageDispatcher` as default — using compiled expression-tree delegates (cached per request `Type`) so the hot path is **zero reflection, zero `dynamic`, AOT-friendlier** than reflection-based or `dynamic`-based dispatch
4. Fully **ROP-compliant failure signalling** — `SchedulerOrchestrator.ProcessDueMessagesAsync` callback signature changes from `Func<..., Task>` to `Func<..., Task<Either<EncinaError, Unit>>>`. No exceptions are thrown to signal dispatch failure — the orchestrator inspects the `Either` and routes `Left` to the retry policy. This aligns with Encina's core philosophy ("no exceptions for business logic")
5. Cancellation propagation through the callback so host shutdown promptly aborts in-flight `IEncina.Send` / `Publish` calls
6. Dedicated observability via `SchedulingActivitySource`, new processor meter instruments, and a new `SchedulingProcessorLog` class with EventIds in the existing `MessagingScheduling` range (2300-2399)

Affected packages:

| Package | Change |
|---------|--------|
| **`Encina.Messaging`** | **New** abstractions `IScheduledMessageRetryPolicy` + `IScheduledMessageDispatcher`; **new** defaults `ExponentialBackoffRetryPolicy` + `CompiledExpressionScheduledMessageDispatcher`; **new** `ScheduledMessageProcessor.cs` + `SchedulingProcessorLog.cs` + `SchedulingProcessorMetrics.cs`; **modify** `SchedulerOrchestrator` (callback signature → `Either<EncinaError, Unit>`, retry policy injection, cancellation propagation); **modify** `MessagingServiceCollectionExtensions.cs` (register processor + abstractions when `UseScheduling`) |
| **`Encina.EntityFrameworkCore`**, **`Encina.Dapper.*`** (×3), **`Encina.ADO.*`** (×3), **`Encina.MongoDB`** | No source change. These packages already wire `AddMessagingServices<...>()` and will automatically register the new hosted service through the core extension method. Verify the DI chain compiles. |
| **`tests/Encina.UnitTests/Messaging/Scheduling/`** | New unit tests for processor loop, dispatch, exponential backoff, enablement gating |
| **`tests/Encina.IntegrationTests/Messaging/Scheduling/`** | End-to-end tests using SQLite + `[Collection("ADO-Sqlite")]` verifying a scheduled command is actually executed |
| **`tests/Encina.GuardTests/Messaging/Scheduling/`** | Null-argument guards for processor constructor |
| **`tests/Encina.ContractTests/Messaging/Scheduling/`** | Lifecycle contract (Start/Stop, cancellation, exception isolation) |

**Provider applicability**: The processor itself is provider-agnostic. It touches **zero provider-specific code** because it depends only on `IScheduledMessageStore` (already implemented for all 8 providers). This matches the issue's explicit guidance: *"The processor should live in `Encina.Messaging` (not in provider packages) because it only depends on `IScheduledMessageStore`"*.

---

## Design Choices

<details>
<summary><strong>1. Package Placement — New file inside <code>Encina.Messaging</code> (not per-provider)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Single `ScheduledMessageProcessor` in `Encina.Messaging/Scheduling/`** | Provider-agnostic, one source of truth, mirrors `SchedulerOrchestrator` placement, minimal maintenance | None — the only caveat is that `IEncina` must be registered in the DI container, which it always is when messaging is used |
| **B) Duplicate per provider (mirroring `OutboxProcessor`)** | Consistent with outbox pattern | Massive duplication (~8 copies), no justification (outbox processor duplicates because it uses provider-specific `DbContext` / `IDbConnection` directly — we do not) |
| **C) Separate `Encina.Messaging.Scheduling.Hosting` package** | Extra isolation | Over-engineered for a single file; forces a new NuGet package |

### Chosen Option: **A — Single `ScheduledMessageProcessor` in `Encina.Messaging/Scheduling/`**

### Rationale

- `IScheduledMessageStore` is a **pure abstraction** — no provider package leaks into the processor
- `SchedulerOrchestrator` already lives in `Encina.Messaging/Scheduling/` and the processor is a thin wrapper around it
- The `OutboxProcessor` duplication is an **accident of history**, not a pattern to imitate. Outbox processors each talk directly to their provider's ORM (`DbContext`, `IDbConnection`) because the outbox doesn't route through a shared orchestrator. We have no such constraint.
- The issue explicitly requests this placement
- Satellite DI chains already call `AddMessagingServices<>()` in core — a single `services.AddHostedService<ScheduledMessageProcessor>()` there wires all 8 providers automatically

</details>

<details>
<summary><strong>2. Reuse <code>SchedulerOrchestrator.ProcessDueMessagesAsync</code> vs bypass orchestrator</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Processor delegates to `SchedulerOrchestrator.ProcessDueMessagesAsync(executeCallback)`** | Reuses existing domain logic (cron rescheduling, recurring handling, retry on exception, batch loop, store calls), single implementation of message lifecycle | Requires small orchestrator tweak (exponential backoff in `MarkAsFailedAsync`) |
| **B) Processor reads `IScheduledMessageStore` directly and reimplements the loop** | Full control, avoids orchestrator coupling | Duplicates cron + retry + recurring logic, creates two sources of truth, contradicts DRY, and the orchestrator becomes dead code |
| **C) Merge orchestrator into processor** | One class | Destroys the domain separation: `SchedulerOrchestrator.ScheduleAsync/CancelAsync/ScheduleRecurringAsync` are user-facing API methods that belong in a domain-layer class, not a hosted service |

### Chosen Option: **A — Delegate to `SchedulerOrchestrator.ProcessDueMessagesAsync`**

### Rationale

- `SchedulerOrchestrator.ProcessDueMessagesAsync` already handles: batch retrieval, type deserialization, recurring vs one-shot branching, cron rescheduling, exception → `MarkAsFailedAsync`, cancellation check between messages. Reimplementing all of this would be pointless duplication.
- The orchestrator's `Func<IScheduledMessage, Type, object, Task> executeCallback` hook is the exact extension point we need
- The processor's role becomes: *"own the loop timing and provide the IEncina dispatch callback"*
- The single missing piece — exponential backoff — is a one-line fix inside `SchedulerOrchestrator.MarkAsFailedAsync`. See decision #3.
- Consequence: the processor file is small (~150-200 LOC), easy to reason about, and testable with a mock orchestrator

</details>

<details>
<summary><strong>3. Retry strategy — Pluggable <code>IScheduledMessageRetryPolicy</code> abstraction with exponential backoff default</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Hardcode exponential backoff inside `SchedulerOrchestrator.MarkAsFailedAsync`** | Smallest diff | Locks in one retry strategy forever; cannot integrate with `Encina.Polly` / `Encina.Extensions.Resilience`; cannot test the retry strategy independently of the orchestrator; no user override path |
| **B) Leave orchestrator at flat delay and add backoff in a second layer** | No orchestrator change | Two retry strategies in two places, impossible to reason about, contradicts the docs |
| **C) Define `IScheduledMessageRetryPolicy` interface + `ExponentialBackoffRetryPolicy` default + DI registration** | Pluggable strategy aligned with Encina's provider philosophy, decouples retry logic from orchestration logic, testable in isolation, swappable without recompiling, opens the door to a future `PollyScheduledMessageRetryPolicy` adapter without API churn | One extra interface and one extra concrete class; one extra DI registration |
| **D) Reuse the existing `Encina.Extensions.Resilience` API directly inside the orchestrator** | Maximum reuse | Couples `Encina.Messaging` core to a resilience provider package — wrong layering; not all consumers use the resilience extensions; introduces a transitive dependency that the messaging core does not need |

### Chosen Option: **C — `IScheduledMessageRetryPolicy` abstraction with `ExponentialBackoffRetryPolicy` default**

### Rationale

- **Pre-1.0 means we choose the best architecture, not the smallest diff.** Hardcoding the retry formula inside the orchestrator (option A) locks Encina into one strategy and is the kind of choice that becomes a regret in 6 months.
- Encina's whole identity is **pluggable abstractions over swappable implementations**. Validation has `IValidationProvider`, persistence has `IScheduledMessageStore`, caching has `ICacheProvider`, locking has `IDistributedLockProvider` — there is no reason retry should be a hardcoded exception.
- The interface is minimal: `RetryDecision Compute(int retryCount, int maxRetries, DateTime nowUtc)` returning `RetryDecision(DateTime? NextRetryAtUtc, bool IsDeadLettered)`. `nextRetryAtUtc == null` ⇒ dead-letter; otherwise reschedule.
- `ExponentialBackoffRetryPolicy` reads `SchedulingOptions.BaseRetryDelay` and computes `delay = BaseRetryDelay * 2^retryCount`, returning dead-letter when `retryCount + 1 >= maxRetries`. Constructor takes `(SchedulingOptions options, TimeProvider? timeProvider = null)`.
- `SchedulerOrchestrator` constructor takes `IScheduledMessageRetryPolicy retryPolicy` (required, not optional — the default will be registered in DI). The private `MarkAsFailedAsync` becomes a one-line delegation: `var decision = _retryPolicy.Compute(message.RetryCount, _options.MaxRetries, _timeProvider.GetUtcNow().UtcDateTime); await _store.MarkAsFailedAsync(message.Id, errorMessage, decision.NextRetryAtUtc, ct);`
- **Testability win**: the retry policy can be unit-tested in isolation against arbitrary `(retryCount, maxRetries, now)` tuples without spinning up a store or an orchestrator. The orchestrator is tested with a stub retry policy that returns deterministic decisions.
- **Future extension** (out of scope for #765, no work in this PR): a `PollyScheduledMessageRetryPolicy` adapter could live in a follow-up `Encina.Polly.Scheduling` package and bridge to the existing Polly resilience pipelines without touching `Encina.Messaging` again.
- Option D (direct dependency on `Encina.Extensions.Resilience`) is rejected because it creates a coupling in the wrong direction: `Encina.Messaging` is core, the resilience extension packages are higher-level. Core packages don't depend on extension packages.

</details>

<details>
<summary><strong>4. Execute callback — <code>IScheduledMessageDispatcher</code> abstraction with compiled-expression default</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reflection dispatch via cached `MethodInfo.Invoke` + `dynamic` for `ValueTask` unwrapping** | Smallest code, ~40 lines | `MethodInfo.Invoke` boxes/copies args on every call; `dynamic` invokes the C# runtime binder (~µs cost per first-time-per-type call); not AOT-friendly without `[DynamicallyAccessedMembers]` annotations; fights the language |
| **B) Store dispatch metadata on the message** | Zero runtime type discovery | Breaks `ScheduleAsync<T>` ergonomics, requires every consumer to declare dispatch kind at schedule time, impossible to retrofit |
| **C) Require all scheduled messages to implement an `IScheduledRequest` marker** | Explicit intent | Forces a marker on every command/notification — invasive and unnecessary |
| **D) Compile-time source generator scanning `IRequest<>` / `INotification` implementations to produce a typed dispatch table** | Zero runtime cost, zero reflection, fully AOT-compatible | Source generator must scan the consumer assembly (not `Encina.Messaging`); requires every consumer project to reference an analyzer package; meaningful infrastructure investment that exceeds the scope of #765 |
| **E) `IScheduledMessageDispatcher` abstraction + compiled expression-tree default** — build a `Func<IEncina, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>>` per request `Type` via `Expression.Lambda` and cache it | Zero reflection on hot path after first call per type; zero `dynamic`; near-native dispatch speed; abstraction lets advanced users replace the dispatcher (e.g., with a source-generator-based one in a follow-up) without touching the processor; testable in isolation; aligns with Encina's "abstraction over implementation" philosophy | Slightly more code than option A; expression trees still use some `[DynamicallyAccessedMembers]` for full AOT compatibility |

### Chosen Option: **E — `IScheduledMessageDispatcher` abstraction + compiled expression-tree default**

### Rationale

- **Pre-1.0 means we choose the best architecture, not the smallest diff.** Option A (reflection + `dynamic`) was the easy path; option E is the right path.
- The interface `IScheduledMessageDispatcher` exposes a single method:

  ```csharp
  ValueTask<Either<EncinaError, Unit>> DispatchAsync(
      Type requestType,
      object request,
      CancellationToken cancellationToken);
  ```

  Note: returns `Either<EncinaError, Unit>` — **not** `Task`. Failures are values, not exceptions (see Design Choice #5).
- The default implementation is `CompiledExpressionScheduledMessageDispatcher`. Algorithm:
  1. Constructor takes `(IEncina encina)`. No reflection in constructor.
  2. Static cache: `ConcurrentDictionary<Type, Func<IEncina, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>>>`
  3. On `DispatchAsync`:
     a. Look up the compiled delegate for `requestType`. If miss, build it.
     b. Building the delegate uses `System.Linq.Expressions`:
        - For `INotification` types: produce `(encina, req, ct) => MapPublishResult(encina.Publish<TConcrete>((TConcrete)req, ct))` where `TConcrete = requestType` and the result mapping discards the `Unit` from `Publish` and returns it as `Right(Unit.Default)` on success.
        - For `IRequest<TResponse>` types: produce `(encina, req, ct) => MapSendResult<TResponse>(encina.Send<TResponse>((IRequest<TResponse>)req, ct))` where the mapping converts `Right(value)` to `Right(Unit.Default)` (the processor doesn't care about response values — only success/failure).
        - For unknown shapes: produce a delegate that returns `Left(SchedulingErrorCodes.UnknownRequestShape)` once cached, so the failure path is also reflection-free on subsequent calls.
     c. Compile via `Expression.Lambda<...>().Compile()` and store in cache.
     d. Invoke the cached delegate.
  4. The mapping helper functions (`MapPublishResult`, `MapSendResult<T>`) are static generic methods on the dispatcher that take `ValueTask<Either<EncinaError, TResponse>>` and return `ValueTask<Either<EncinaError, Unit>>` — these are referenced from the expression tree via `MethodCallExpression`.
- **Performance**: After the first call per type, dispatch is a virtual call through the compiled delegate plus one `IEncina.Send`/`Publish` call. **Zero `MethodInfo.Invoke`, zero `dynamic`, zero boxing of arguments at the dispatch site**.
- **AOT-friendliness**: Annotate the dispatcher's expression-building helpers with `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]` so the trimmer preserves `Send`/`Publish` and the `IRequest<>`/`INotification` interfaces. For full NativeAOT, a source-generator-based dispatcher (option D) can replace the default in a follow-up issue without touching the processor or the abstraction.
- **Testability**: `IScheduledMessageDispatcher` is mockable for processor unit tests. The compiled-expression dispatcher gets its own unit tests verifying: (a) correct delegate produced for `INotification`, (b) correct delegate produced for `IRequest<TResponse>`, (c) cache hit on second call, (d) unknown shapes return `Left`, (e) `Left` from `IEncina` is propagated through, (f) cancellation propagates.
- **Extensibility**: A future `Encina.Messaging.Scheduling.SourceGen` package can supply a generated `IScheduledMessageDispatcher` for AOT scenarios — drop-in replacement, zero core changes.

</details>

<details>
<summary><strong>5. Success vs failure signalling — Callback returns <code>Either&lt;EncinaError, Unit&gt;</code> (pure ROP)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Callback throws on dispatch failure; orchestrator's `try/catch` handles it** | Matches the orchestrator's CURRENT signature (`Func<IScheduledMessage, Type, object, Task>`), zero orchestrator API changes | **Contradicts Encina's core philosophy: "no exceptions for business logic — use Either".** Forces a wrapper exception type (`ScheduledDispatchException`) just to carry an `EncinaError` that was already a value. Tests must `Assert.Throws` instead of `result.IsLeft.ShouldBeTrue()`. Inconsistent with every other callback signature in the codebase. |
| **B) Widen the callback signature to return `Either<EncinaError, Unit>`** | Pure ROP, consistent with the rest of Encina, zero exception ceremony, the orchestrator does `if (result.IsLeft) await retryViaPolicy(...)`, tests look like every other Encina test (`result.IsRight.ShouldBeTrue()`) | Requires orchestrator callback signature change — but pre-1.0 there are no external callers (that's literally why issue #765 exists) |
| **C) Out-parameter / result container** | Explicit | Awkward in async context, no upside vs B |

### Chosen Option: **B — Callback returns `Either<EncinaError, Unit>`**

### Rationale

- **Pre-1.0 means we choose the best architecture, not the smallest diff.** Throwing exceptions to signal a failure that was already a value (`EncinaError`) would mean wrapping it in a `ScheduledDispatchException` only to unwrap it again on the catch side — pure ceremony that contradicts ROP.
- Encina's entire identity is *"explicit error handling, no exceptions for business logic"*. Every store, every handler, every behavior returns `Either<EncinaError, T>`. The dispatcher must do the same.
- New callback signature:

  ```csharp
  Func<IScheduledMessage, Type, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>> executeCallback
  ```

- Orchestrator's `ProcessDueMessagesAsync` loop body becomes:

  ```csharp
  var dispatchResult = await executeCallback(message, requestType, request, cancellationToken);
  if (dispatchResult.IsLeft)
  {
      var error = dispatchResult.LeftToArray()[0];
      Log.DispatchFailed(_logger, message.Id, error.Code, error.Message);
      await MarkAsFailedAsync(message, error.Message, cancellationToken); // delegates to retry policy
      continue;
  }
  // dispatch succeeded — handle recurring vs one-shot
  ```

- The orchestrator still keeps a `try/catch` around the callback, but **only as a safety net for true bugs** (handler crashes, AccessViolationException, etc.). True exceptions are converted to a synthetic `EncinaError` with `code = "scheduling.dispatch_unhandled_exception"` and routed through the same `MarkAsFailedAsync` path so the retry policy still applies.
- **`ScheduledDispatchException` is removed from the plan entirely.** It was only needed in option A.
- **Simpler tests**: `(await processor.ProcessOnceAsync(ct)).IsRight.ShouldBeTrue()` — same shape as every other Encina test.
- **Pre-1.0 API change is free**: `SchedulerOrchestrator.ProcessDueMessagesAsync` is called by **zero** code today (the entire reason for #765). Changing the callback signature breaks nothing.

</details>

<details>
<summary><strong>6. Cancellation plumbing — Add <code>CancellationToken</code> to <code>ProcessDueMessagesAsync</code> callback</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extend the callback signature to `Func<IScheduledMessage, Type, object, CancellationToken, Task>`** | The dispatch call correctly honors `stoppingToken` cancellation mid-batch | Requires changing orchestrator signature (no external callers today) |
| **B) Capture the token via closure inside the processor** | No orchestrator change | Less explicit, callback has no way to abort between store-loop iterations |
| **C) Rely on orchestrator's own `cancellationToken` without propagating to callback** | Current behavior | Long-running `Send` calls can't be cancelled on host shutdown — bad |

### Chosen Option: **A — Extend the callback signature to pass the token**

### Rationale

- Pre-1.0, no callers of `ProcessDueMessagesAsync` exist yet (Issue #765 exists precisely because nobody wires it)
- Host shutdown must promptly cancel in-flight `IEncina.Send` calls
- Clean propagation: `BackgroundService.ExecuteAsync(stoppingToken)` → orchestrator → callback → `IEncina.Send(request, ct)`

</details>

<details>
<summary><strong>7. DI registration — Extend the <code>AddMessagingServices</code> generic signature</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add a 10th generic parameter `TScheduledProcessor : class, IHostedService` and register with `AddHostedService<TScheduledProcessor>()`** | Mirrors `TOutboxProcessor` pattern, each satellite chooses its processor | All 8 satellite packages must pass the new type argument — mechanical change |
| **B) Hard-code `services.AddHostedService<ScheduledMessageProcessor>()` inside the `if (config.UseScheduling)` block with no generic parameter** | Minimal change, zero satellite updates | Processor type cannot be overridden by downstream code (pre-1.0 we may or may not care; the outbox processor uses the generic parameter, so consistency matters) |
| **C) Add a separate `AddEncinaScheduledMessageProcessor()` method** | Isolated | Creates a dangling extension method that users must remember to call |

### Chosen Option: **B — Hard-code `AddHostedService<ScheduledMessageProcessor>()`**

### Rationale

- The processor is provider-agnostic by design — there is **no reason** for a satellite to override it
- The outbox processor generic parameter exists because outbox processors differ per provider (EF uses `DbContext`, ADO uses `IDbConnection`). That constraint does not apply here.
- Adding a 10th generic parameter touches 8 satellite files for zero benefit
- The satellite packages only need to ensure `TScheduledStore` is wired; the core extension method registers the processor automatically via `services.AddHostedService<ScheduledMessageProcessor>()`
- If a future user truly needs a custom processor, they can register their own `IHostedService` and set `SchedulingOptions.EnableProcessor = false` to suppress the default

</details>

<details>
<summary><strong>8. Enablement gating — Check <code>EnableProcessor</code> at both DI and runtime</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Check at DI time AND at `ExecuteAsync` start** | Both configurability layers honored, compile-time registration is simple | Two checks instead of one |
| **B) Only DI-time check (skip `AddHostedService` if disabled)** | Simpler registration | `EnableProcessor` can only be set via options at DI time — users cannot toggle at runtime |
| **C) Only runtime check** | Single point of enforcement | Wastes a hosted service slot, processor wakes up just to exit |

### Chosen Option: **A — Check at both layers**

### Rationale

- At DI time: inspect `config.SchedulingOptions.EnableProcessor` (read from the `MessagingConfiguration` instance), skip `AddHostedService` if false
- At runtime: `ExecuteAsync` re-reads `SchedulingOptions.EnableProcessor` from the injected `SchedulingOptions` singleton — if false, log once and return without entering the loop
- Consistent with `OutboxProcessor` which also reads `_options.EnableProcessor` at start (see `OutboxOptions.EnableProcessor`)
- Second check defends against DI-misconfiguration scenarios and supports hot-reload scenarios

</details>

---

## Implementation Phases

### Phase 1: Retry Policy Abstraction + Orchestrator Refactor (ROP Callback)

> **Goal**: Introduce the `IScheduledMessageRetryPolicy` abstraction with its `ExponentialBackoffRetryPolicy` default, then refactor `SchedulerOrchestrator` to (a) accept the retry policy via DI, (b) accept a fully-ROP callback returning `Either<EncinaError, Unit>`, and (c) propagate `CancellationToken` into the callback.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Messaging/Scheduling/IScheduledMessageRetryPolicy.cs`**:
   - `public interface IScheduledMessageRetryPolicy`
   - One method: `RetryDecision Compute(int retryCount, int maxRetries, DateTime nowUtc);`
   - XML doc explaining: `IsDeadLettered = true` ⇒ `NextRetryAtUtc` MUST be `null`; otherwise it MUST be in the future
   - The orchestrator owns the `TimeProvider` and passes `nowUtc` so the policy is deterministic and trivially testable

2. **Create `src/Encina.Messaging/Scheduling/RetryDecision.cs`**:
   - `public sealed record RetryDecision(DateTime? NextRetryAtUtc, bool IsDeadLettered)`
   - Static factory helpers for ergonomic construction:
     - `public static RetryDecision RetryAt(DateTime nextUtc) => new(nextUtc, IsDeadLettered: false);`
     - `public static RetryDecision DeadLetter() => new(null, IsDeadLettered: true);`

3. **Create `src/Encina.Messaging/Scheduling/ExponentialBackoffRetryPolicy.cs`**:
   - `public sealed class ExponentialBackoffRetryPolicy : IScheduledMessageRetryPolicy`
   - Constructor: `(SchedulingOptions options)` — `ArgumentNullException.ThrowIfNull(options)`
   - `Compute` algorithm:

     ```csharp
     public RetryDecision Compute(int retryCount, int maxRetries, DateTime nowUtc)
     {
         if (retryCount + 1 >= maxRetries)
             return RetryDecision.DeadLetter();
         var delayMs = _options.BaseRetryDelay.TotalMilliseconds * Math.Pow(2, retryCount);
         return RetryDecision.RetryAt(nowUtc.AddMilliseconds(delayMs));
     }
     ```

   - XML doc explaining the formula `delay = BaseRetryDelay * 2^retryCount` and dead-letter semantics

4. **Modify `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs`** (significant refactor):
   - **Add constructor parameter**: `IScheduledMessageRetryPolicy retryPolicy` (required, validated via `ArgumentNullException.ThrowIfNull`)
   - Store in `private readonly IScheduledMessageRetryPolicy _retryPolicy;`
   - **Change `ProcessDueMessagesAsync` callback type**:
     - From: `Func<IScheduledMessage, Type, object, Task> executeCallback`
     - To: `Func<IScheduledMessage, Type, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>> executeCallback`
   - **Replace the loop body's dispatch logic** (around lines 250-286) so that the dispatch result is inspected as `Either<EncinaError, Unit>`:

     ```csharp
     try
     {
         var requestType = Type.GetType(message.RequestType);
         if (requestType == null)
         {
             Log.UnknownRequestType(_logger, message.Id, message.RequestType);
             await MarkAsFailedAsync(message, $"Unknown request type: {message.RequestType}", cancellationToken).ConfigureAwait(false);
             continue;
         }

         var request = JsonSerializer.Deserialize(message.Content, requestType, JsonOptions);
         if (request == null)
         {
             Log.DeserializationFailed(_logger, message.Id, message.RequestType);
             await MarkAsFailedAsync(message, "Failed to deserialize request", cancellationToken).ConfigureAwait(false);
             continue;
         }

         var dispatchResult = await executeCallback(message, requestType, request, cancellationToken).ConfigureAwait(false);
         if (dispatchResult.IsLeft)
         {
             var error = dispatchResult.LeftToArray()[0];
             Log.DispatchFailed(_logger, message.Id, error.GetEncinaCode(), error.Message);
             await MarkAsFailedAsync(message, error.Message, cancellationToken).ConfigureAwait(false);
             continue;
         }

         if (message.IsRecurring)
             await HandleRecurringMessageAsync(message, cancellationToken).ConfigureAwait(false);
         else
             await _store.MarkAsProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);

         processedCount++;
         Log.MessageExecuted(_logger, message.Id);
     }
     catch (OperationCanceledException) { throw; }
     catch (Exception ex)
     {
         // Safety net for true bugs (handler crashes, AVE, etc).
         // Real failures use the Either path above.
         Log.ExecutionFailed(_logger, ex, message.Id);
         await MarkAsFailedAsync(message, ex.Message, cancellationToken).ConfigureAwait(false);
     }
     ```

   - **Replace `MarkAsFailedAsync`** with retry-policy delegation:

     ```csharp
     private async Task MarkAsFailedAsync(IScheduledMessage message, string errorMessage, CancellationToken cancellationToken)
     {
         var decision = _retryPolicy.Compute(message.RetryCount, _options.MaxRetries, _timeProvider.GetUtcNow().UtcDateTime);
         await _store.MarkAsFailedAsync(message.Id, errorMessage, decision.NextRetryAtUtc, cancellationToken).ConfigureAwait(false);
     }
     ```

   - **Add a new log method** `DispatchFailed(ILogger, Guid messageId, string errorCode, string errorMessage)` (Warning level) to the existing `Log` partial class — pick the next free EventId (e.g., `310`) within the orchestrator's local namespace
   - **Update XML docs** on `ProcessDueMessagesAsync` documenting: ROP callback signature, retry policy delegation, exception safety net semantics, cancellation propagation

5. **Update any existing (internal) callers of `ProcessDueMessagesAsync`** — search the repo; there should be none today (that's the whole point of Issue #765). If tests construct `SchedulerOrchestrator`, they must now pass an `IScheduledMessageRetryPolicy` (real or stub).

6. **Update `src/Encina.Messaging/PublicAPI.Unshipped.txt`** — add:
   - `Encina.Messaging.Scheduling.IScheduledMessageRetryPolicy`
   - `Encina.Messaging.Scheduling.IScheduledMessageRetryPolicy.Compute(int, int, System.DateTime) -> Encina.Messaging.Scheduling.RetryDecision`
   - `Encina.Messaging.Scheduling.RetryDecision` (record + ctor + properties)
   - `Encina.Messaging.Scheduling.RetryDecision.RetryAt(System.DateTime) -> Encina.Messaging.Scheduling.RetryDecision`
   - `Encina.Messaging.Scheduling.RetryDecision.DeadLetter() -> Encina.Messaging.Scheduling.RetryDecision`
   - `Encina.Messaging.Scheduling.ExponentialBackoffRetryPolicy` (sealed class + ctor)
   - Updated `SchedulerOrchestrator` constructor signature
   - Updated `ProcessDueMessagesAsync` signature (new callback type)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of Encina ScheduledMessageProcessor (Issue #765).

CONTEXT:
- Encina.Messaging has SchedulerOrchestrator (src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs) handling the full processing lifecycle for scheduled messages.
- Three latent design issues must be fixed PROPERLY (not patched):
  1. Hardcoded flat-delay retry → must become a pluggable IScheduledMessageRetryPolicy abstraction (Encina identity is "pluggable abstractions over swappable implementations").
  2. Callback throws exceptions to signal failure → must become Either<EncinaError, Unit> (Encina identity is "no exceptions for business logic — use Either").
  3. Callback receives no CancellationToken → must propagate so host shutdown aborts in-flight Send/Publish calls.
- Pre-1.0: no backward compatibility, no external callers exist (that is why issue #765 exists).

TASK:
Create IScheduledMessageRetryPolicy + RetryDecision + ExponentialBackoffRetryPolicy.
Refactor SchedulerOrchestrator to inject the retry policy, accept an Either-returning callback, and propagate cancellation.

KEY RULES:
- IScheduledMessageRetryPolicy.Compute is synchronous and deterministic — takes (retryCount, maxRetries, nowUtc); the orchestrator owns time
- RetryDecision is a sealed record with NextRetryAtUtc + IsDeadLettered + factory helpers RetryAt and DeadLetter
- ExponentialBackoffRetryPolicy formula: delay = options.BaseRetryDelay * 2^retryCount; dead-letter when retryCount+1 >= maxRetries
- SchedulerOrchestrator constructor: add IScheduledMessageRetryPolicy retryPolicy (required, ArgumentNullException.ThrowIfNull)
- ProcessDueMessagesAsync callback: Func<IScheduledMessage, Type, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>>
- Loop body inspects the Either result; on Left, log + delegate to retry policy; on Right, mark processed (or reschedule recurring)
- Keep a try/catch around the callback ONLY as a safety net for true bugs (handler crashes, etc.) — convert exception to MarkAsFailedAsync with the exception message
- MarkAsFailedAsync delegates entirely to _retryPolicy.Compute(...) — no inline math
- Add Log.DispatchFailed for the Left case (Warning level)
- Do NOT introduce any external dependencies (no Polly, no expression trees in this phase — they belong to Phase 2)
- Update PublicAPI.Unshipped.txt with all new public symbols

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs (the target)
- src/Encina.Messaging/Scheduling/SchedulingOptions.cs (documents BaseRetryDelay exponential behavior)
- src/Encina.EntityFrameworkCore/Outbox/OutboxProcessor.cs (reference for exponential backoff formula)
- src/Encina.Compliance.Consent/ (reference for sealed-class abstraction patterns)
```

</details>

---

### Phase 2: Dispatcher Abstraction + Compiled-Expression Default

> **Goal**: Define the `IScheduledMessageDispatcher` abstraction and ship `CompiledExpressionScheduledMessageDispatcher` as the default — a zero-reflection-on-hot-path, zero-`dynamic`, AOT-friendlier implementation built on cached compiled expression-tree delegates.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Messaging/Scheduling/IScheduledMessageDispatcher.cs`**:
   - `public interface IScheduledMessageDispatcher`
   - One method:

     ```csharp
     ValueTask<Either<EncinaError, Unit>> DispatchAsync(
         Type requestType,
         object request,
         CancellationToken cancellationToken);
     ```

   - XML doc explaining: returns `Either<EncinaError, Unit>` (NOT throwing exceptions); discards response value of `Send<TResponse>` because the processor only cares about success/failure; unknown shapes (neither `IRequest<>` nor `INotification`) return `Left(SchedulingErrorCodes.UnknownRequestShape)`

2. **Add `SchedulingErrorCodes.UnknownRequestShape`** to the existing `SchedulingErrorCodes` static class in `SchedulerOrchestrator.cs`:
   - `public const string UnknownRequestShape = "scheduling.unknown_request_shape";`

3. **Create `src/Encina.Messaging/Scheduling/CompiledExpressionScheduledMessageDispatcher.cs`**:
   - `public sealed class CompiledExpressionScheduledMessageDispatcher : IScheduledMessageDispatcher`
   - Constructor: `(IEncina encina)` — `ArgumentNullException.ThrowIfNull(encina)`
   - Static cache field:

     ```csharp
     private static readonly ConcurrentDictionary<Type, Func<IEncina, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>>> DelegateCache = new();
     ```

   - Annotate dispatcher type with `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]` for trimmer/AOT friendliness
   - `DispatchAsync` body:

     ```csharp
     public ValueTask<Either<EncinaError, Unit>> DispatchAsync(
         Type requestType,
         object request,
         CancellationToken cancellationToken)
     {
         ArgumentNullException.ThrowIfNull(requestType);
         ArgumentNullException.ThrowIfNull(request);

         var dispatcher = DelegateCache.GetOrAdd(requestType, BuildDispatchDelegate);
         return dispatcher(_encina, request, cancellationToken);
     }
     ```

   - `BuildDispatchDelegate(Type requestType)` algorithm:
     1. If `typeof(INotification).IsAssignableFrom(requestType)`:
        - Build an `Expression.Lambda<Func<IEncina, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>>>` that:
          - Casts `(object)request` → `requestType`
          - Calls the closed generic `IEncina.Publish<TNotification>` via `Expression.Call` with `MethodInfo.MakeGenericMethod(requestType)`
          - Returns the resulting `ValueTask<Either<EncinaError, Unit>>` directly (Publish already returns `Unit`)
        - Compile and return
     2. Else search `requestType.GetInterfaces()` for `i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)`:
        - Extract `responseType = i.GetGenericArguments()[0]`
        - Build an `Expression.Lambda<Func<IEncina, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>>>` that:
          - Casts `(object)request` → `i` (the closed `IRequest<TResponse>`)
          - Calls the closed generic `IEncina.Send<TResponse>` via `Expression.Call` with `MethodInfo.MakeGenericMethod(responseType)`
          - Wraps the result with a static helper `MapSendResult<TResponse>` that converts `Either<EncinaError, TResponse>` → `Either<EncinaError, Unit>` (`Right(Unit.Default)` on success)
        - Compile and return
     3. Else (unknown shape): return a constant delegate that ignores its arguments and yields `Left(EncinaErrors.Create(SchedulingErrorCodes.UnknownRequestShape, $"Type {requestType.FullName} is neither IRequest<> nor INotification"))`. Cached so the failure path is also reflection-free on subsequent calls.
   - Static helper methods (referenced from the expression trees):

     ```csharp
     private static async ValueTask<Either<EncinaError, Unit>> MapSendResult<TResponse>(
         ValueTask<Either<EncinaError, TResponse>> sendTask)
     {
         var result = await sendTask.ConfigureAwait(false);
         return result.Match<Either<EncinaError, Unit>>(
             Right: _ => Unit.Default,
             Left: err => err);
     }

     private static ValueTask<Either<EncinaError, Unit>> WrapPublishResult(
         ValueTask<Either<EncinaError, Unit>> publishTask) => publishTask;
     ```

   - The expression tree uses `MethodInfo` of these helper methods to keep the compiled lambda's body fully strongly-typed
   - **Internal visibility**: also expose an `internal int CacheCount => DelegateCache.Count;` for tests that verify cache hits

4. **Update `src/Encina.Messaging/PublicAPI.Unshipped.txt`** — add:
   - `Encina.Messaging.Scheduling.IScheduledMessageDispatcher`
   - `Encina.Messaging.Scheduling.IScheduledMessageDispatcher.DispatchAsync(System.Type!, object!, System.Threading.CancellationToken) -> System.Threading.Tasks.ValueTask<LanguageExt.Either<Encina.EncinaError, LanguageExt.Unit>>`
   - `Encina.Messaging.Scheduling.CompiledExpressionScheduledMessageDispatcher` (sealed class)
   - Constructor signature
   - `DispatchAsync` override
   - `Encina.Messaging.Scheduling.SchedulingErrorCodes.UnknownRequestShape` const field

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of Encina ScheduledMessageProcessor (Issue #765).

CONTEXT:
- Encina.Messaging has IEncina (src/Encina/Abstractions/IEncina.cs) with two generic methods:
  - Send<TResponse>(IRequest<TResponse> request, CancellationToken) → ValueTask<Either<EncinaError, TResponse>>
  - Publish<TNotification>(TNotification notification, CancellationToken) → ValueTask<Either<EncinaError, Unit>> where TNotification : INotification
- The scheduled message payload is deserialized into `object` at runtime; only `Type requestType` is known. We must dispatch to the correct generic overload without knowing TResponse at compile time.
- This dispatcher MUST NOT use `dynamic`, MUST NOT use `MethodInfo.Invoke` on the hot path, and MUST NOT throw exceptions to signal failure. It returns Either<EncinaError, Unit>.
- Phase 1 already changed the orchestrator callback to ROP. The processor (Phase 3) will wire `dispatcher.DispatchAsync` directly as the callback.

TASK:
Create IScheduledMessageDispatcher abstraction + CompiledExpressionScheduledMessageDispatcher default implementation in src/Encina.Messaging/Scheduling/.

KEY RULES:
- Use System.Linq.Expressions to build a Func<IEncina, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>> per request Type, cached in a static ConcurrentDictionary
- Notifications: Expression.Call to closed generic IEncina.Publish<TNotification> via MakeGenericMethod, return ValueTask directly
- Requests: Expression.Call to closed generic IEncina.Send<TResponse>, wrap result via static MapSendResult<TResponse> helper that converts Either<EncinaError, TResponse> → Either<EncinaError, Unit>
- Unknown shape: cache a constant delegate returning Left(SchedulingErrorCodes.UnknownRequestShape) — even the failure path is reflection-free on second call
- Annotate dispatcher type with [DynamicallyAccessedMembers] for AOT/trimmer friendliness
- ArgumentNullException.ThrowIfNull on all public method args
- Add SchedulingErrorCodes.UnknownRequestShape const to the existing SchedulingErrorCodes class in SchedulerOrchestrator.cs
- Expose internal `CacheCount` property for unit tests verifying cache hits
- NO `dynamic`. NO `MethodInfo.Invoke`. NO exceptions for control flow. ALL of those would defeat the purpose of choosing this design over the easier reflection approach.
- XML documentation on all public members
- Update PublicAPI.Unshipped.txt

REFERENCE FILES:
- src/Encina/Abstractions/IEncina.cs (target methods — note generic constraints and signatures)
- src/Encina/Abstractions/IRequest.cs and src/Encina/Abstractions/INotification.cs (marker interfaces)
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs (Phase 1 has already updated the callback signature here — verify before building)
- Microsoft docs on System.Linq.Expressions.Expression.Lambda and Expression.Call
```

</details>

---

### Phase 3: The `ScheduledMessageProcessor` Background Service

> **Goal**: Implement the `BackgroundService` itself — the outer loop, scope creation, orchestrator invocation, exception isolation, and enablement gating.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs`**:
   - `public sealed class ScheduledMessageProcessor : BackgroundService`
   - Dependencies: `IServiceProvider serviceProvider`, `SchedulingOptions options`, `ILogger<ScheduledMessageProcessor> logger`, `TimeProvider? timeProvider = null`
   - Constructor validates all required args with `ArgumentNullException.ThrowIfNull`; stores into `private readonly` fields
   - `protected override async Task ExecuteAsync(CancellationToken stoppingToken)`:
     1. If `_options.EnableProcessor == false`: log `ProcessorDisabled` and return
     2. Log `ProcessorStarting(interval, batchSize, maxRetries)`
     3. Loop `while (!stoppingToken.IsCancellationRequested)`:
        - Wrap `ProcessOnceAsync(stoppingToken)` in try/catch; on non-`OperationCanceledException`, log `ProcessorCycleFailed` with exception
        - `await Task.Delay(_options.ProcessingInterval, stoppingToken)` (propagates cancellation as `OperationCanceledException`, which falls through the outer while check)
     4. Log `ProcessorStopping`
   - `private async Task ProcessOnceAsync(CancellationToken cancellationToken)`:
     1. `using var scope = _serviceProvider.CreateScope();`
     2. Resolve `var orchestrator = scope.ServiceProvider.GetRequiredService<SchedulerOrchestrator>();`
     3. Resolve `var dispatcher = scope.ServiceProvider.GetRequiredService<IScheduledMessageDispatcher>();` — the dispatcher is registered as a scoped service in Phase 4 so it can capture the scope's `IEncina` instance
     4. Start an activity: `using var activity = SchedulingActivitySource.StartProcessingCycle(_options.BatchSize);`
     5. Invoke (the dispatcher's signature already matches the orchestrator's callback signature exactly — no lambda wrapping needed):

        ```csharp
        var result = await orchestrator.ProcessDueMessagesAsync(
            (msg, type, req, ct) => dispatcher.DispatchAsync(type, req, ct),
            cancellationToken);
        ```

     6. Pattern-match the `Either<EncinaError, int>`:
        - `Right(count)`: if `count > 0` log `ProcessorBatchCompleted(count)`; record meter via `SchedulingProcessorMetrics.RecordBatch(count, failureCount: 0)`; on `count == 0` log `ProcessorIdle` (Debug)
        - `Left(error)`: log `ProcessorBatchFailed(error)`; record meter with `failureCount = 1`
     7. Set `activity?.SetTag("scheduling.processed_count", count)` and `activity?.SetStatus(ActivityStatusCode.Ok | Error)` based on outcome

2. **Optional helper for cycle-level observability** — decide whether to add new members to `SchedulingActivitySource.cs`:
   - `StartProcessingCycle(int batchSize) → Activity?` — a new activity per polling iteration
   - Tags: `scheduling.batch_size`, `scheduling.processed_count`, `scheduling.failure_count`
   - If existing `SchedulingActivitySource` doesn't have cycle-level helpers, add them

3. **Update `src/Encina.Messaging/PublicAPI.Unshipped.txt`**:
   - `Encina.Messaging.Scheduling.ScheduledMessageProcessor` (sealed class)
   - `Encina.Messaging.Scheduling.ScheduledMessageProcessor.ScheduledMessageProcessor(...)` (constructor)
   - `override Encina.Messaging.Scheduling.ScheduledMessageProcessor.ExecuteAsync(System.Threading.CancellationToken) -> System.Threading.Tasks.Task`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of Encina ScheduledMessageProcessor (Issue #765).

CONTEXT:
- Phase 1 refactored SchedulerOrchestrator: callback now returns Either<EncinaError, Unit>, retry policy injected via IScheduledMessageRetryPolicy, CancellationToken propagated.
- Phase 2 created IScheduledMessageDispatcher abstraction + CompiledExpressionScheduledMessageDispatcher default. Its DispatchAsync(Type, object, CancellationToken) signature already matches the orchestrator's callback shape — the processor wires it directly with no lambda.
- The processor lives in Encina.Messaging (NOT per-provider) because it depends only on abstractions.

TASK:
Create ScheduledMessageProcessor.cs as a BackgroundService in src/Encina.Messaging/Scheduling/.

KEY RULES:
- sealed class, public, inherits BackgroundService (from Microsoft.Extensions.Hosting)
- Constructor dependencies: IServiceProvider, SchedulingOptions, ILogger<ScheduledMessageProcessor>, TimeProvider? (optional, defaults to TimeProvider.System)
- ArgumentNullException.ThrowIfNull for all required args
- ExecuteAsync:
  1. If !_options.EnableProcessor → log and return (gate check)
  2. Log processor starting with interval/batch/retries
  3. while (!stoppingToken.IsCancellationRequested): call ProcessOnceAsync in try/catch, await Task.Delay(interval, token)
  4. Catch non-OperationCanceledException in the loop to avoid crash-looping
  5. Log processor stopping after the loop exits
- ProcessOnceAsync:
  1. Create a scope via IServiceProvider.CreateScope() (scoped services MUST be resolved per scope)
  2. Resolve SchedulerOrchestrator AND IScheduledMessageDispatcher from scope.ServiceProvider — both are scoped (Phase 4 registers the dispatcher as scoped so it captures the scope's IEncina)
  3. Call orchestrator.ProcessDueMessagesAsync((msg, type, req, ct) => dispatcher.DispatchAsync(type, req, ct), cancellationToken)
  4. Inspect the returned Either<EncinaError, int>; log success/failure, emit metrics
- All log calls use the SchedulingProcessorLog LoggerMessage partial class (created in Phase 5)
- Start an OTel Activity per cycle via SchedulingActivitySource.StartProcessingCycle (extended in Phase 5)
- No exceptions escape ExecuteAsync except OperationCanceledException at shutdown
- NO direct instantiation of CompiledExpressionScheduledMessageDispatcher — always resolve via DI so users can swap the implementation
- Update PublicAPI.Unshipped.txt

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Outbox/OutboxProcessor.cs (loop pattern, exception handling, scoped resolution)
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs (Phase 1 — verify callback signature)
- src/Encina.Messaging/Scheduling/IScheduledMessageDispatcher.cs (Phase 2 — verify signature)
- src/Encina.Messaging/Diagnostics/SchedulingActivitySource.cs (existing activity source to extend in Phase 5)
- src/Encina.Messaging/Recoverability/DelayedRetryProcessor.cs (another BackgroundService example in the same project)
```

</details>

---

### Phase 4: DI Registration — Wire the Hosted Service

> **Goal**: Register `ScheduledMessageProcessor` as a hosted service whenever `config.UseScheduling = true`, ensuring all 8 satellite providers pick it up automatically.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs`**:
   - Inside `AddMessagingServices<...>()`, find the `if (config.UseScheduling)` block (around lines 109-115 per research)
   - The block currently registers: `SchedulingOptions` (singleton), `IScheduledMessageStore` (scoped), `IScheduledMessageFactory` (scoped), `SchedulerOrchestrator` (scoped)
   - **Add three new registrations** before the `SchedulerOrchestrator` registration so `TryAdd*` semantics work in user override scenarios:

     ```csharp
     services.TryAddSingleton<IScheduledMessageRetryPolicy>(sp =>
         new ExponentialBackoffRetryPolicy(sp.GetRequiredService<SchedulingOptions>()));

     services.TryAddScoped<IScheduledMessageDispatcher>(sp =>
         new CompiledExpressionScheduledMessageDispatcher(sp.GetRequiredService<IEncina>()));
     ```

   - **Note on lifetimes**:
     - `IScheduledMessageRetryPolicy` → singleton (stateless, depends only on `SchedulingOptions` which is also singleton)
     - `IScheduledMessageDispatcher` → scoped (depends on `IEncina` which is scoped; the static delegate cache is shared across instances anyway)
     - `TryAdd*` so users can register their own implementations BEFORE calling `AddMessagingServices<>` and have those win
   - **Update `SchedulerOrchestrator` registration** so DI resolves the retry policy:
     - Existing: `services.AddScoped<SchedulerOrchestrator>();`
     - Verify the orchestrator's constructor (post Phase 1 refactor) accepts `IScheduledMessageRetryPolicy` — DI will satisfy it automatically
   - **Add hosted service registration** (after the existing scheduling registrations):

     ```csharp
     if (config.SchedulingOptions.EnableProcessor)
     {
         services.AddHostedService<ScheduledMessageProcessor>();
     }
     ```

   - No generic parameter change needed on `AddMessagingServices<>` (see Design Choice #7) — the processor is provider-agnostic
   - Ensure the `using Encina.Messaging.Scheduling;` and `using Microsoft.Extensions.DependencyInjection.Extensions;` imports are present

2. **Verify satellite providers compile without changes**:
   - `Encina.EntityFrameworkCore/MessagingServiceCollectionExtensions.cs` (or wherever `AddEncinaEntityFrameworkCore` lives)
   - `Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/ServiceCollectionExtensions.cs`
   - `Encina.ADO.{SqlServer,PostgreSQL,MySQL}/ServiceCollectionExtensions.cs`
   - `Encina.MongoDB/ServiceCollectionExtensions.cs`
   - None of these need source changes — they all delegate to `AddMessagingServices<>` which now registers the new abstractions automatically
   - Run `dotnet build Encina.slnx --configuration Release` to verify

3. **Update `MessagingConfiguration` XML doc** for `UseScheduling`: mention that enabling this flag now automatically registers `IScheduledMessageRetryPolicy`, `IScheduledMessageDispatcher`, and (when `SchedulingOptions.EnableProcessor = true`) the `ScheduledMessageProcessor` hosted service. Users can override the retry policy or dispatcher by registering their own implementation BEFORE calling `AddEncina*()`.

4. **Update `src/Encina.Messaging/PublicAPI.Unshipped.txt`** with any new symbols (the abstractions and defaults are added in Phases 1-2; this phase adds no new public API of its own)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of Encina ScheduledMessageProcessor (Issue #765).

CONTEXT:
- Phases 1-3 complete: SchedulerOrchestrator fixed, dispatcher created, ScheduledMessageProcessor implemented.
- The core Encina.Messaging package has a shared DI extension MessagingServiceCollectionExtensions.AddMessagingServices<...>() that each satellite provider calls with their provider-specific type arguments. This is where the outbox processor gets registered via services.AddHostedService<TOutboxProcessor>().
- Scheduling already has a `if (config.UseScheduling)` block that registers the store, factory, and SchedulerOrchestrator. We need to add the hosted service inside it.

TASK:
Register ScheduledMessageProcessor in MessagingServiceCollectionExtensions.cs.

KEY RULES:
- Locate the `if (config.UseScheduling)` block in AddMessagingServices<>
- Add at the end of the block (after SchedulerOrchestrator registration):
    if (config.SchedulingOptions.EnableProcessor)
    {
        services.AddHostedService<ScheduledMessageProcessor>();
    }
- Do NOT add a generic parameter for the processor type — it is provider-agnostic (see Design Choice #7)
- Ensure `using Encina.Messaging.Scheduling;` is imported
- Zero changes required in satellite provider packages (Encina.ADO.*, Encina.Dapper.*, Encina.EntityFrameworkCore, Encina.MongoDB)
- Run `dotnet build Encina.slnx --configuration Release` — expect 0 errors, 0 warnings
- Update src/Encina.Messaging/PublicAPI.Unshipped.txt only if new public symbols were exposed

REFERENCE FILES:
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs (the target — see the UseScheduling block near line 109-115)
- src/Encina.Messaging/MessagingConfiguration.cs (to understand SchedulingOptions lookup)
```

</details>

---

### Phase 5: Observability — Diagnostics, Metrics & Logging

> **Goal**: Add dedicated OpenTelemetry activities, meter counters, and structured logging for processor lifecycle and per-cycle events.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Messaging/Diagnostics/SchedulingProcessorLog.cs`**:
   - `internal static partial class SchedulingProcessorLog`
   - Uses `[LoggerMessage]` source generator
   - **EventId allocation**: Use the existing `EventIdRanges.MessagingScheduling` (2300-2399) range. `SchedulingStoreLog.cs` already uses 2300-2306. Allocate **2320-2349** for processor logs (30 slots, future-proof).
   - Messages:
     - `2320` `Information` `ProcessorStarting` — `"ScheduledMessageProcessor starting. Interval={Interval}, BatchSize={BatchSize}, MaxRetries={MaxRetries}"`
     - `2321` `Information` `ProcessorStopping` — `"ScheduledMessageProcessor stopping"`
     - `2322` `Information` `ProcessorDisabled` — `"ScheduledMessageProcessor is disabled (SchedulingOptions.EnableProcessor = false)"`
     - `2323` `Debug` `ProcessorCycleStarted` — `"Processor cycle started"`
     - `2324` `Debug` `ProcessorBatchCompleted` — `"Processor batch completed. Processed={Processed}"`
     - `2325` `Warning` `ProcessorBatchFailed` — `"Processor batch failed: {ErrorCode} {ErrorMessage}"`
     - `2326` `Error` `ProcessorCycleFailed` — `"Processor cycle threw an unhandled exception"` (with `Exception` parameter)
     - `2327` `Warning` `ProcessorDispatchFailed` — `"Dispatch for scheduled message {MessageId} of type {RequestType} failed: {ErrorMessage}"`
     - `2328` `Debug` `ProcessorDispatchSucceeded` — `"Dispatch for scheduled message {MessageId} succeeded"`
     - `2329` `Information` `ProcessorIdle` — `"Processor cycle found no due messages"` (optional low-noise log)

2. **Extend `src/Encina.Messaging/Diagnostics/SchedulingActivitySource.cs`**:
   - Add a new helper: `internal static Activity? StartProcessingCycle(int batchSize)` that opens an `Activity` with name `"ScheduledMessageProcessor.Cycle"` and initial tags `scheduling.batch_size`
   - Add a finalizer helper: `internal static void CompleteProcessingCycle(Activity? activity, int processedCount, int failureCount)` — sets tags and `ActivityStatusCode.Ok` / `Error`
   - Keep the existing store-level activity helpers unchanged

3. **Extend `src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs`** (or create a new `SchedulingProcessorMetrics.cs` if the file is outbox-focused):
   - Add:
     - `Counter<long> scheduled_messages_processed_total` — tags: `outcome = success|failure`
     - `Counter<long> scheduled_messages_dispatched_total` — tags: `kind = request|notification`, `outcome = success|failure`
     - `Histogram<double> scheduled_processor_cycle_duration_seconds` — no tags
   - Prefer a new file `SchedulingProcessorMetrics.cs` to avoid bloating the generic `MessagingStoreMetrics`
   - Instrument the processor: record in `ProcessOnceAsync` after each cycle

4. **Update `src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs`** from Phase 3 to call these new helpers (cycle activity, metrics, logs)

5. **Update `src/Encina.Messaging/PublicAPI.Unshipped.txt`** if any new public symbols from diagnostics (usually internal)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of Encina ScheduledMessageProcessor (Issue #765).

CONTEXT:
- Phases 1-4 complete. The processor works functionally but lacks its own diagnostics, metrics, and structured logging.
- EventIdRanges.MessagingScheduling is already registered as (2300, 2399). SchedulingStoreLog.cs uses 2300-2306. We will use 2320-2349 for processor logs — no registry change needed because we stay within the already-registered range.
- Existing diagnostics to extend: SchedulingActivitySource.cs (activity helpers), MessagingStoreMetrics.cs (meter), or create SchedulingProcessorMetrics.cs.

TASK:
Create SchedulingProcessorLog.cs, extend SchedulingActivitySource.cs, and add processor-specific metrics.

KEY RULES:
- SchedulingProcessorLog.cs: internal static partial class with [LoggerMessage] attributes in the 2320-2349 range
- Event IDs strictly sequential, no gaps, packed at the start of the sub-range to avoid overflow
- Log levels match semantic severity: Info for lifecycle, Debug for cycle internals, Warning for retries, Error for unhandled exceptions
- SchedulingActivitySource.cs: add StartProcessingCycle(int batchSize) and CompleteProcessingCycle(Activity?, int processedCount, int failureCount) helpers
- Tags use constants at top of file: TagBatchSize = "scheduling.batch_size", TagProcessedCount = "scheduling.processed_count", TagFailureCount = "scheduling.failure_count"
- Metrics (new SchedulingProcessorMetrics.cs):
  - Counter<long> MessagesProcessed — tag outcome (success|failure)
  - Counter<long> MessagesDispatched — tags kind (request|notification), outcome
  - Histogram<double> CycleDurationSeconds
- All meter instruments use the existing Encina.Messaging Meter (reuse, don't create a new one) — check how OutboxMetrics does it in MessagingStoreMetrics.cs
- Processor must call all three diagnostics signals in ProcessOnceAsync: Activity, log, metric
- Use Activity.HasListeners() check before allocating activity tags
- Run build → 0 warnings

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/SchedulingStoreLog.cs (EventId pattern, LoggerMessage source generator)
- src/Encina.Messaging/Diagnostics/SchedulingActivitySource.cs (existing helpers)
- src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs (meter setup)
- src/Encina/Diagnostics/EventIdRanges.cs (range registry — DO NOT CHANGE, stay within 2300-2399)
```

</details>

---

### Phase 6: Cross-Cutting Integration

> **Goal**: Evaluate and wire the transversal functions marked ✅ in the matrix. For this issue, the ✅ integrations are: **OpenTelemetry**, **Structured Logging**, **Resilience (exponential backoff)**, **Transactions (atomic mark-as-processed)**. Caching, Health Checks, Validation, Distributed Locks, Idempotency, Multi-Tenancy, Module Isolation, and Audit Trail are either ❌ N/A or ⏭️ deferred per the issue body.

<details>
<summary><strong>Tasks</strong></summary>

1. **OpenTelemetry (✅)** — Covered in Phase 5 via `SchedulingActivitySource.StartProcessingCycle` + `SchedulingProcessorMetrics`. Confirm: activity links parent → cycle → per-message dispatch (if any); tags include `scheduling.batch_size`, `scheduling.processed_count`, `scheduling.failure_count`, `scheduling.message_id`, `scheduling.request_type`.

2. **Structured Logging (✅)** — Covered in Phase 5 via `SchedulingProcessorLog`. Confirm: all lifecycle transitions have a log; EventIds 2320-2329 are within the registered `MessagingScheduling` range.

3. **Resilience — Pluggable retry policy (✅)** — Covered in Phase 1 via the `IScheduledMessageRetryPolicy` abstraction with `ExponentialBackoffRetryPolicy` as the default. Confirm: formula is `BaseRetryDelay * 2^retryCount`; dead-letter (null `nextRetryAtUtc`) is used when retries exhausted; users can swap in their own policy via DI without touching the orchestrator. A future `Encina.Polly.Scheduling` adapter package can ship a `PollyScheduledMessageRetryPolicy` without core changes.

4. **Transactions — Atomic mark-as-processed (✅)** — Confirm the store's `MarkAsProcessedAsync` and `MarkAsFailedAsync` are atomic per their provider implementation. For `ScheduledMessageStoreEF` this is a single `SaveChangesAsync` call wrapped by the caller. For ADO/Dapper, this is a single UPDATE statement. **No processor-side change required** — the orchestrator calls these in sequence and the store guarantees atomicity. Verify in Phase 7 integration tests that a mid-flight cancellation does not leave a message in an inconsistent state.

5. **Health Checks (⏭️ deferred)** — Per issue, deferred. No action this PR.

6. **Distributed Locks (⏭️ deferred — #716)** — Per issue, deferred. The processor will currently run on every replica; duplicate processing is possible in a multi-instance deployment. Add a TODO comment in the processor file referencing #716 and linking to this issue.

7. **Idempotency (⏭️ deferred — #735)** — Per issue, deferred. Add a TODO comment referencing #735.

8. **Multi-Tenancy (⏭️ deferred — #739)** — Per issue, deferred. Add a TODO comment referencing #739.

9. **Audit Trail (⏭️ deferred — #749)** — Per issue, deferred. Add a TODO comment referencing #749.

10. **TODO comments** — Place at the top of `ScheduledMessageProcessor.cs` in an XML remarks block listing the four deferred integrations with issue links so users and future contributors see the roadmap.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of Encina ScheduledMessageProcessor (Issue #765).

CONTEXT:
- Phases 1-5 complete. The processor is functional and observable.
- Several cross-cutting integrations are deferred to follow-up issues (see issue #765 Cross-Cutting Integration Checklist).

TASK:
Verify ✅ integrations are wired and add TODO comments for ⏭️ deferred integrations.

KEY RULES:
- For ✅ items (OpenTelemetry, Structured Logging, Resilience, Transactions): verify the processor/orchestrator actually use them; no new code needed
- For ⏭️ items: add an XML <remarks> block at the top of ScheduledMessageProcessor.cs listing:
  - Distributed locks — see #716
  - Idempotency — see #735
  - Multi-tenancy — see #739
  - Audit trail — see #749
  - Health check for processor lag — deferred (separate issue TBD)
- Each TODO must reference the specific issue number, explain what's deferred, and what will replace it
- Transaction atomicity: double-check that store.MarkAsProcessedAsync and store.MarkAsFailedAsync are single-statement atomic operations in all 8 provider implementations
- Run build → 0 warnings

REFERENCE FILES:
- Issue #765 body (Cross-Cutting Integration Checklist table)
- src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs (from Phase 3)
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs (from Phase 1)
```

</details>

---

### Phase 7: Testing — Unit, Guard, Contract, Integration

> **Goal**: Verify processor behavior across all critical paths, with emphasis on real end-to-end dispatch through SQLite integration tests.

<details>
<summary><strong>Tasks</strong></summary>

#### 7a. Unit Tests (`tests/Encina.UnitTests/Messaging/Scheduling/`)

- `ExponentialBackoffRetryPolicyTests.cs` (tests the new abstraction in isolation — most valuable test class in the PR because the policy is now first-class):
  - `Compute_FirstRetry_ReturnsBaseDelay` — `retryCount=0`, `BaseRetryDelay=5s` → next at `now + 5s`
  - `Compute_SecondRetry_ReturnsDoubleDelay` — `retryCount=1` → next at `now + 10s`
  - `Compute_ThirdRetry_ReturnsQuadrupleDelay` — `retryCount=2` → next at `now + 20s`
  - `Compute_MaxRetriesExhausted_ReturnsDeadLetter` — `retryCount=2`, `maxRetries=3` → `IsDeadLettered=true`, `NextRetryAtUtc=null`
  - `Compute_DeterministicForSameInputs` — calling twice with the same args yields equal `RetryDecision`
  - `RetryDecision_RetryAt_ProducesNonDeadLettered`
  - `RetryDecision_DeadLetter_ProducesNullNextRetry`
- `CompiledExpressionScheduledMessageDispatcherTests.cs`:
  - `DispatchAsync_RequestType_InvokesSendOnEncina` — custom `TestRequest : IRequest<int>` + handler; mock `IEncina.Send<int>` returns `Right(42)`; assert dispatcher returns `Right(Unit.Default)` and `Send` was called once
  - `DispatchAsync_NotificationType_InvokesPublishOnEncina` — custom `TestNotification : INotification`; mock `IEncina.Publish<TestNotification>` returns `Right(Unit.Default)`; assert dispatcher returns `Right` and `Publish` was called once
  - `DispatchAsync_UnknownShape_ReturnsLeftUnknownRequestShape` — plain `object`; assert `Left(SchedulingErrorCodes.UnknownRequestShape)`, NO exception thrown
  - `DispatchAsync_SendReturnsLeft_PropagatesAsLeft` — mock returns `Left(error)`; dispatcher returns same `Left` (no exception, no wrapping)
  - `DispatchAsync_PublishReturnsLeft_PropagatesAsLeft` — same for notifications
  - `DispatchAsync_CachesDelegatePerType` — dispatch same type twice; verify `CacheCount` increments by 1 (not 2)
  - `DispatchAsync_DifferentTypes_BuildSeparateDelegates` — request + notification + unknown shape → 3 cache entries
  - `DispatchAsync_NullRequestType_ThrowsArgumentNullException` (guard)
  - `DispatchAsync_NullRequest_ThrowsArgumentNullException` (guard)
  - `DispatchAsync_CancellationToken_FlowsToEncinaCall` — verify the token passed to `DispatchAsync` is the same one received by the mock `IEncina.Send`
- `SchedulerOrchestratorRopCallbackTests.cs` (tests the Phase 1 callback refactor):
  - `ProcessDueMessagesAsync_CallbackReturnsRight_MarksProcessed`
  - `ProcessDueMessagesAsync_CallbackReturnsLeft_DelegatesToRetryPolicy` — verify `IScheduledMessageRetryPolicy.Compute` was called with the correct `(retryCount, maxRetries, now)` triple
  - `ProcessDueMessagesAsync_CallbackThrows_StillDelegatesToRetryPolicy` — safety net path
  - `ProcessDueMessagesAsync_CallbackThrowsOperationCanceled_Rethrows`
  - `ProcessDueMessagesAsync_RetryPolicyReturnsDeadLetter_StorePassedNullNextRetry`
  - `ProcessDueMessagesAsync_RecurringMessage_OnSuccess_RescheduledViaCron`
  - `ProcessDueMessagesAsync_CancellationTokenPropagatedToCallback`
- `ScheduledMessageProcessorTests.cs`:
  - `ExecuteAsync_ProcessorDisabled_ReturnsImmediately` — sets `EnableProcessor = false`, asserts no orchestrator invocation
  - `ExecuteAsync_NoDueMessages_CompletesCycleWithoutDispatch` — orchestrator returns `Right(0)`; no `IEncina` call
  - `ExecuteAsync_CancellationBeforeCycle_ExitsLoop`
  - `ExecuteAsync_OrchestratorThrows_DoesNotCrashLoop` — orchestrator throws transient exception, verifies loop continues after delay
  - `ExecuteAsync_DispatchSuccess_RecordsMetricsAndCompletesActivity`
  - `ExecuteAsync_DispatchFailureFromEither_RecordsFailureMetric` — mock dispatcher returns `Left`, processor logs `ProcessorBatchFailed`, NO exception thrown anywhere
  - `ExecuteAsync_ResolvesDispatcherFromScope` — verify processor uses DI-resolved `IScheduledMessageDispatcher`, not a hardcoded instance

**Target**: ~30-35 unit tests (more than originally planned because the new abstractions deserve their own coverage)

#### 7b. Guard Tests (`tests/Encina.GuardTests/Messaging/Scheduling/`)

- `ScheduledMessageProcessorGuardTests.cs` — null-check the 3 required constructor parameters (`serviceProvider`, `options`, `logger`); `timeProvider` is optional so no guard
- `CompiledExpressionScheduledMessageDispatcherGuardTests.cs` — null-check constructor `IEncina`; null-check `DispatchAsync(requestType, request, ct)` first two args
- `ExponentialBackoffRetryPolicyGuardTests.cs` — null-check constructor `SchedulingOptions`
- `SchedulerOrchestratorRetryPolicyGuardTests.cs` — verify the new constructor parameter `IScheduledMessageRetryPolicy retryPolicy` throws on null

**Target**: ~10-12 guard tests

#### 7c. Contract Tests (`tests/Encina.ContractTests/Messaging/Scheduling/`)

- `ScheduledMessageProcessorContractTests.cs` — instantiate the processor with a real in-memory store + `SchedulerOrchestrator` + `ExponentialBackoffRetryPolicy` + `CompiledExpressionScheduledMessageDispatcher`:
  - Verifies `BackgroundService.StartAsync` → `ExecuteAsync` → graceful `StopAsync`
  - Verifies cancellation propagates through to the dispatcher within reasonable time
  - Verifies exception isolation: a single failing message does not abort the loop
  - Verifies the processor resolves and uses any user-registered `IScheduledMessageDispatcher` override (substitute a fake dispatcher and confirm it received the calls)
- `IScheduledMessageRetryPolicyContractTests.cs` — abstract base class verifying the contract for `IScheduledMessageRetryPolicy` implementations:
  - `RetryAt_ReturnedDecision_HasNonNullNextRetryAndIsNotDeadLettered`
  - `DeadLetter_ReturnedDecision_HasNullNextRetryAndIsDeadLettered`
  - `Compute_NeverReturnsBothNullNextRetryAndNotDeadLettered` (invariant)
  - Concrete derived class: `ExponentialBackoffRetryPolicyContractTests` instantiates the default and runs the suite
- `IScheduledMessageDispatcherContractTests.cs` — abstract base class verifying contract:
  - `DispatchAsync_NeverThrowsForKnownShape` (only `ArgumentNullException` for null inputs is allowed)
  - `DispatchAsync_PropagatesEitherFromEncina`
  - Concrete derived class: `CompiledExpressionScheduledMessageDispatcherContractTests`

**Target**: ~10-15 contract tests (more than originally — abstractions deserve formal contracts so future implementations can be validated)

#### 7d. Integration Tests (`tests/Encina.IntegrationTests/Messaging/Scheduling/`)

> **CRITICAL**: Use `[Collection("ADO-Sqlite")]` or `[Collection("EFCore-Sqlite")]` shared fixtures. **Never** create per-class fixtures. Use `ClearAllDataAsync` in `InitializeAsync`. Follow the SQLite shared-connection rules (never dispose the shared connection).

- `ScheduledMessageProcessorSqliteIntegrationTests.cs` (use `[Collection("ADO-Sqlite")]`):
  - `Scheduled_Command_IsExecutedAfterScheduledTime` — register a test `IRequest<int>` + handler, schedule with 1s delay, start processor, wait up to 3s, assert handler ran and message marked processed
  - `Scheduled_Notification_IsPublishedAfterScheduledTime` — same but with `INotification` + handler
  - `Failing_Command_RetriesWithExponentialBackoff` — handler always returns `Left(error)`, schedule, let run for 5 cycles, verify `RetryCount` increments and `NextRetryAtUtc` doubles
  - `Failing_Command_DeadletteredAfterMaxRetries` — after `MaxRetries` attempts, `NextRetryAtUtc` is null and message is no longer due
  - `Recurring_Message_IsRescheduledAfterEachExecution` — use a fake `ICronParser` that always returns `now + 500ms`, run for 3 cycles, assert handler ran 3 times
  - `ProcessorDisabled_MessagesNotExecuted` — set `EnableProcessor = false`, verify messages remain in `Pending` state
  - `CancellationDuringCycle_GracefullyStops` — start processor, cancel mid-batch, verify no half-committed state

**Target**: ~7-10 integration tests (Sqlite only — covers the end-to-end contract; other providers are covered by existing `ScheduledMessageStore*IntegrationTests` for the store layer)

#### 7e. Property / Load / Benchmark Tests

- **Property tests (⏭️ skip)** — create justification `.md` file at `tests/Encina.PropertyTests/Messaging/Scheduling/ScheduledMessageProcessorPropertyTests.md`. Rationale: the processor's behavior is deterministic conditional on store content and time; the store layer already has property tests.
- **Load tests (⏭️ justify)** — create `tests/Encina.LoadTests/Messaging/Scheduling/ScheduledMessageProcessorLoadTests.md`. Rationale: the processor's hot path is bounded by `SchedulingOptions.BatchSize * PollingInterval`; load scenarios are best exercised at the store level (existing). Revisit when distributed lock support (#716) lands.
- **Benchmark tests (⏭️ justify)** — create `tests/Encina.BenchmarkTests/Messaging/Scheduling/ScheduledMessageProcessorBenchmarks.md`. Rationale: the dispatcher's reflection cost is amortized by the static cache; the processor's loop cost is dominated by the I/O delay (store + Encina.Send). Benchmark only becomes meaningful once the lock + dispatch combination is optimized.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of Encina ScheduledMessageProcessor (Issue #765).

CONTEXT:
- Phases 1-6 complete: retry policy abstraction + compiled-expression dispatcher abstraction + processor + diagnostics + DI all wired.
- Test infrastructure uses shared xUnit [Collection] fixtures (NEVER per-class). SQLite fixtures share an in-memory database connection — NEVER dispose it.
- Integration tests MUST execute real package code to generate coverage (reflection-only tests produce 0 coverage).
- The new abstractions (IScheduledMessageRetryPolicy + IScheduledMessageDispatcher) deserve formal contract tests so future implementations can be validated against the same suite.

TASK:
Create unit, guard, contract, and integration tests. Produce justification .md files for property/load/benchmark.

KEY RULES:
Unit Tests:
- ExponentialBackoffRetryPolicyTests is the most valuable new test class — covers 5+ scenarios of the retry math + RetryDecision factory helpers
- CompiledExpressionScheduledMessageDispatcherTests must verify NO exceptions are thrown for known shapes (Either is the failure channel)
- CompiledExpressionScheduledMessageDispatcherTests must verify the cache works (CacheCount internal property)
- CompiledExpressionScheduledMessageDispatcherTests must verify cancellation token flows through to mock IEncina
- SchedulerOrchestratorRopCallbackTests verifies the new Either<EncinaError, Unit> callback contract (NOT exception-based)
- Mock IServiceProvider to return a mock SchedulerOrchestrator + mock IScheduledMessageDispatcher; inject via scope
- Test disable gating, empty cycles, exception isolation, dispatch success/failure paths
- Fast (<10ms each), AAA pattern, descriptive names

Guard Tests:
- Use GuardClauses.xUnit library
- Cover ScheduledMessageProcessor + CompiledExpressionScheduledMessageDispatcher + ExponentialBackoffRetryPolicy + new SchedulerOrchestrator constructor parameter

Contract Tests:
- Abstract base class for IScheduledMessageRetryPolicy and IScheduledMessageDispatcher contracts
- Default implementations subclass the abstract base and run the same suite
- Verifies BackgroundService lifecycle, cancellation, exception isolation

Integration Tests:
- [Collection("ADO-Sqlite")] — reuse existing fixture
- ClearAllDataAsync in InitializeAsync
- Register real IEncina, real SchedulerOrchestrator, real Sqlite store
- Define a small test request (IRequest<int>) and notification (INotification) with real handlers in the test assembly
- Actually execute a scheduled command and verify the handler ran (use a counter/flag)
- Verify exponential backoff by measuring NextRetryAtUtc progression
- Verify recurring via a FakeCronParser that returns now + 500ms
- Tests must actually INSTANTIATE ScheduledMessageProcessor and call StartAsync/StopAsync to generate coverage

Justification .md files:
- Follow the template in CLAUDE.md § "Test Justification Documents"
- Include Status, Justification, Adequate Coverage from Other Test Types, Recommended Alternative, Related Files, Date, Issue

REFERENCE FILES:
- tests/Encina.UnitTests/Messaging/Outbox/OutboxProcessorTests.cs (unit test patterns)
- tests/Encina.IntegrationTests/ — find existing [Collection("ADO-Sqlite")] tests for the store layer
- tests/Encina.UnitTests/Messaging/Scheduling/ — where existing orchestrator tests live (if any)
- CLAUDE.md § "Collection Fixtures (Container Reduction Strategy)"
- CLAUDE.md § "SQLite Shared Connection Pattern"
```

</details>

---

### Phase 8: Documentation & Finalization

> **Goal**: Update all required project documentation, verify build, and finalize the PR.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML documentation** — every public member (processor class, exception class, extended activity helpers if public) has `<summary>`, `<remarks>` where needed, `<param>`, `<returns>`, `<example>`

2. **`CHANGELOG.md`** — add the following entries under `## [Unreleased]`:

   Under `### Added`:

   ```markdown
   - **Scheduling**: New `IScheduledMessageRetryPolicy` abstraction for pluggable retry strategies on scheduled message failures. Default implementation `ExponentialBackoffRetryPolicy` computes `delay = BaseRetryDelay * 2^retryCount` and dead-letters when `MaxRetries` is exhausted. Users can swap in their own policy via DI. (#765)
   - **Scheduling**: New `IScheduledMessageDispatcher` abstraction for pluggable dispatch strategies. Default implementation `CompiledExpressionScheduledMessageDispatcher` builds and caches `Func<IEncina, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>>` delegates per request `Type` via `System.Linq.Expressions` — zero reflection on the hot path, zero `dynamic`, AOT-friendlier than reflection-based dispatch. (#765)
   ```

   Under `### Fixed`:

   ```markdown
   - **Scheduling**: `ScheduledMessageProcessor` background service now executes scheduled messages that were previously persisted but never dispatched. The processor polls `IScheduledMessageStore.GetDueMessagesAsync` at the configured interval and dispatches each due message through the registered `IScheduledMessageDispatcher` (which in turn calls `IEncina.Send` for `IRequest<TResponse>` or `IEncina.Publish` for `INotification`). Retry uses the registered `IScheduledMessageRetryPolicy`; recurring messages are rescheduled via `ICronParser`. Registered automatically when `UseScheduling = true` and `SchedulingOptions.EnableProcessor = true` (default). (Fixes #765)
   ```

   Under `### Changed`:

   ```markdown
   - **Scheduling**: `SchedulerOrchestrator.ProcessDueMessagesAsync` callback signature is now `Func<IScheduledMessage, Type, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>>` (was `Func<IScheduledMessage, Type, object, Task>`). Failures are signalled as `Left(EncinaError)` (Railway Oriented Programming) instead of thrown exceptions. The `CancellationToken` parameter propagates host shutdown into in-flight `IEncina.Send`/`Publish` calls.
   - **Scheduling**: `SchedulerOrchestrator` constructor now requires an `IScheduledMessageRetryPolicy` argument (registered automatically by `AddMessagingServices`). The hardcoded flat-delay retry has been removed in favor of policy-based delegation.
   - **Scheduling**: `SchedulingErrorCodes` adds `UnknownRequestShape = "scheduling.unknown_request_shape"` for messages whose deserialized request type implements neither `IRequest<>` nor `INotification`.
   ```

3. **`ROADMAP.md`** — mark the scheduling feature as functional; link to #765 in the release notes section

4. **`src/Encina.Messaging/README.md`** — if it has a "Scheduling" section, update it with:
   - Enabling: `config.UseScheduling = true;`
   - Auto-registered processor; override by setting `SchedulingOptions.EnableProcessor = false`
   - Retry semantics, dead-lettering, recurring support

5. **`docs/features/scheduling.md`** — if the file does not exist, create a concise usage guide (configuration options, scheduling API, processor behavior, observability). If it exists, update the "Processing" section with concrete timing and retry details.

6. **`docs/INVENTORY.md`** — update the entry for Issue #765 / scheduling subsystem from "stores exist but processor missing" to "fully functional"

7. **`docs/architecture/adr/`** — **ADR REQUIRED**. This PR introduces three new public abstractions (`IScheduledMessageRetryPolicy`, `IScheduledMessageDispatcher`) with non-obvious design choices (compiled expression trees over reflection, ROP callback over exception throwing, retry policy pluggability over inlined math). Future contributors need the rationale. Create `docs/architecture/adr/0XX-scheduled-message-processor-design.md` documenting:
   - **Context**: scheduled messages were inert (Issue #765)
   - **Decision 1**: Provider-agnostic placement (single file in `Encina.Messaging`)
   - **Decision 2**: Pluggable retry policy abstraction with exponential backoff default
   - **Decision 3**: Compiled-expression dispatcher abstraction (zero reflection on hot path)
   - **Decision 4**: ROP failure signalling (callback returns `Either<EncinaError, Unit>`)
   - **Consequences**: extensibility unlocks (Polly adapter, source-gen dispatcher), test coverage requirements (formal contracts), cross-cutting deferrals (#716, #735, #739, #749)
   - Allocate the next free ADR number from `docs/architecture/adr/`

8. **`src/Encina.Messaging/PublicAPI.Unshipped.txt`** — final review, ensure every new public symbol is listed:
   - `Encina.Messaging.Scheduling.IScheduledMessageRetryPolicy` + `Compute` method
   - `Encina.Messaging.Scheduling.RetryDecision` (record + ctor + properties + factory helpers)
   - `Encina.Messaging.Scheduling.ExponentialBackoffRetryPolicy` (sealed class + ctor)
   - `Encina.Messaging.Scheduling.IScheduledMessageDispatcher` + `DispatchAsync` method
   - `Encina.Messaging.Scheduling.CompiledExpressionScheduledMessageDispatcher` (sealed class + ctor + `DispatchAsync` override)
   - `Encina.Messaging.Scheduling.ScheduledMessageProcessor` (sealed class + ctor + `ExecuteAsync` override)
   - `Encina.Messaging.Scheduling.SchedulingErrorCodes.UnknownRequestShape` (const)
   - Updated `SchedulerOrchestrator` constructor signature (new `IScheduledMessageRetryPolicy` parameter)
   - Updated `SchedulerOrchestrator.ProcessDueMessagesAsync` signature (new ROP callback type)

9. **Build verification**:
   - `dotnet build Encina.slnx --configuration Release` → **0 errors, 0 warnings**
   - `dotnet test tests/Encina.UnitTests/ tests/Encina.GuardTests/ tests/Encina.ContractTests/ --configuration Release` → all pass
   - `dotnet test tests/Encina.IntegrationTests/ --filter "Category=Integration&Database=SQLite"` → all pass
   - Coverage target: ≥85% line coverage on new files (processor, dispatcher, updated orchestrator)

10. **Commit message** (per CLAUDE.md — no AI attribution):

    ```
    feat(messaging): add ScheduledMessageProcessor with pluggable retry + dispatch (#765)

    Implements the missing background service that reads due scheduled
    messages from IScheduledMessageStore and dispatches them via IEncina.

    New abstractions:
    - IScheduledMessageRetryPolicy + ExponentialBackoffRetryPolicy default
    - IScheduledMessageDispatcher + CompiledExpressionScheduledMessageDispatcher
      default (zero reflection on hot path, expression-tree compiled delegates)

    SchedulerOrchestrator refactor:
    - ProcessDueMessagesAsync callback now returns Either<EncinaError, Unit>
      (Railway Oriented Programming, no exceptions for control flow)
    - CancellationToken propagated to dispatch callback
    - Constructor accepts IScheduledMessageRetryPolicy

    Fixes #765
    ```

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of Encina ScheduledMessageProcessor (Issue #765).

CONTEXT:
- Phases 1-7 complete: implementation, DI, observability, tests all in place.
- Finalization required: docs, CHANGELOG, build/test verification.

TASK:
Update documentation, verify build, finalize.

KEY RULES:
- CHANGELOG.md: add Fixed and Changed entries under Unreleased; cite #765
- Update src/Encina.Messaging/README.md if it has a Scheduling section
- Create or update docs/features/scheduling.md with usage, config, processor behavior, observability
- Update docs/INVENTORY.md if it tracks scheduling status
- Do NOT add AI attribution to commit messages or comments
- Build: dotnet build Encina.slnx --configuration Release → 0 errors, 0 warnings
- Test: dotnet test for unit, guard, contract, and Sqlite integration → all pass
- Coverage: verify ≥85% on new files
- PublicAPI.Unshipped.txt: every new public symbol must be listed

REFERENCE FILES:
- CHANGELOG.md (existing format)
- ROADMAP.md
- docs/INVENTORY.md
- src/Encina.Messaging/README.md
- CLAUDE.md § "Git Workflow" (no AI attribution rule)
```

</details>

---

## Research

### Acceptance Criteria Mapping

| # | Criterion (from Issue #765) | Phase | Status in Plan |
|---|-----------------------------|-------|----------------|
| 1 | `ScheduledMessageProcessor` background service implemented in `Encina.Messaging` | Phase 3 | ✅ New `BackgroundService` at `src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs` |
| 2 | Polls `IScheduledMessageStore.GetDueMessagesAsync()` at configurable interval | Phase 3 | ✅ Via `SchedulerOrchestrator.ProcessDueMessagesAsync`; interval from `SchedulingOptions.ProcessingInterval` |
| 3 | Dispatches due messages through `IEncina.Send<TResponse>()` or `IEncina.Publish()` | Phase 2 | ✅ `IScheduledMessageDispatcher` abstraction + `CompiledExpressionScheduledMessageDispatcher` default (compiled expression-tree delegates, zero reflection on hot path) |
| 4 | Handles success: `MarkAsProcessedAsync()` | Phase 1 | ✅ Inside refactored `SchedulerOrchestrator.ProcessDueMessagesAsync` after inspecting `Right` from the ROP callback |
| 5 | Handles failure: `MarkAsFailedAsync()` with exponential backoff retry | Phase 1 | ✅ `IScheduledMessageRetryPolicy` abstraction + `ExponentialBackoffRetryPolicy` default (`BaseRetryDelay * 2^retryCount`); orchestrator delegates entirely to the policy; users can swap |
| 6 | Handles recurring: `RescheduleRecurringMessageAsync()` with next cron execution time | Phase 1 (existing) | ✅ `SchedulerOrchestrator.HandleRecurringMessageAsync` already correct |
| 7 | Configurable via `SchedulingOptions` (ProcessingInterval, BatchSize, MaxRetries, BaseRetryDelay) | Phase 3 | ✅ Injected via constructor; existing `SchedulingOptions` fields reused |
| 8 | Registered when `config.UseScheduling = true` | Phase 4 | ✅ `AddHostedService<ScheduledMessageProcessor>()` inside the `UseScheduling` block |
| 9 | OpenTelemetry instrumentation (ActivitySource + Meter) | Phase 5 | ✅ Extended `SchedulingActivitySource` + new `SchedulingProcessorMetrics` |
| 10 | Structured logging with `[LoggerMessage]` source generators | Phase 5 | ✅ New `SchedulingProcessorLog.cs`, EventIds 2320-2349 |
| 11 | Unit tests covering all paths | Phase 7 | ✅ ~30-35 unit tests across retry policy, dispatcher, ROP callback, processor |
| 12 | Integration tests with at least SQLite | Phase 7 | ✅ ~7-10 Sqlite integration tests using `[Collection("ADO-Sqlite")]` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Plan |
|-----------|----------|--------------------|
| `IScheduledMessageStore` | `src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs` | Store abstraction, already implemented by 8 providers |
| `SchedulerOrchestrator` | `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs` | Domain logic; processor wraps `ProcessDueMessagesAsync` |
| `SchedulingOptions` | `src/Encina.Messaging/Scheduling/SchedulingOptions.cs` | `ProcessingInterval`, `BatchSize`, `MaxRetries`, `BaseRetryDelay`, `EnableProcessor`, `EnableRecurringMessages` — all already defined |
| `IScheduledMessage` | `src/Encina.Messaging/Scheduling/IScheduledMessage.cs` | Entity with `RequestType`, `Content`, `RetryCount`, `IsRecurring`, etc. |
| `IScheduledMessageFactory` | `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs` (nested) | Unchanged |
| `ICronParser` | `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs` (nested) | Unchanged — caller provides impl |
| `IEncina` | `src/Encina/Abstractions/IEncina.cs` | Target dispatcher; resolved per scope by processor |
| `IRequest<TResponse>` / `INotification` | `src/Encina/Abstractions/` | Marker interfaces for overload selection |
| `OutboxProcessor` | `src/Encina.EntityFrameworkCore/Outbox/OutboxProcessor.cs` (and per-provider copies) | Reference for loop structure, exception isolation, scoped resolution |
| `DelayedRetryProcessor` | `src/Encina.Messaging/Recoverability/DelayedRetryProcessor.cs` | Secondary reference — another in-core `BackgroundService` |
| `SchedulingActivitySource` | `src/Encina.Messaging/Diagnostics/SchedulingActivitySource.cs` | Extended in Phase 5 |
| `SchedulingStoreLog` | `src/Encina.Messaging/Diagnostics/SchedulingStoreLog.cs` | Reference for `[LoggerMessage]` pattern |
| `MessagingStoreMetrics` | `src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs` | Reference for `Meter` setup (or extend) |
| `MessagingServiceCollectionExtensions` | `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs` | DI hook for `AddHostedService` registration |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Confirms `MessagingScheduling = (2300, 2399)` — use 2320-2349 within it |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (outbox store) | 2000-2099 | Existing |
| `Encina.Messaging` (inbox store) | 2100-2199 | Existing |
| `Encina.Messaging` (saga store) | 2200-2299 | Existing |
| **`Encina.Messaging` (scheduling store)** | **2300-2399** | Existing; `SchedulingStoreLog` uses 2300-2306 |
| **— Scheduling store logs** | **2300-2306** | Already used |
| **— Scheduled message processor logs (new)** | **2320-2349** | **Allocated by this plan within the existing range — no `EventIdRanges.cs` change required** |
| `Encina.EntityFrameworkCore` (query cache) | 2400-2449 | Existing |
| `Encina.Messaging.Encryption` | 2450-2499 | Existing |

> **Note**: CLAUDE.md's EventId rules require ranges to be registered before use. The `MessagingScheduling = (2300, 2399)` range is already registered and covers the whole block. Using sub-slots (2320-2349) within an already-registered range does not require a new `EventIdRanges` entry. The architecture test enforces "within registered range", not "has a sub-range entry".

### File Count Estimate

| Category | Files | Notes |
|----------|-------|-------|
| Retry policy (Phase 1) | 3 | New `IScheduledMessageRetryPolicy.cs`, `RetryDecision.cs`, `ExponentialBackoffRetryPolicy.cs` |
| Orchestrator refactor (Phase 1) | 1 | Modify `SchedulerOrchestrator.cs` (constructor + callback signature + loop body + `MarkAsFailedAsync`) |
| Dispatcher abstraction (Phase 2) | 2 | New `IScheduledMessageDispatcher.cs`, `CompiledExpressionScheduledMessageDispatcher.cs` |
| Processor (Phase 3) | 1 | New `ScheduledMessageProcessor.cs` |
| DI (Phase 4) | 1 | Modify `MessagingServiceCollectionExtensions.cs` (register retry policy + dispatcher + hosted service) |
| Observability (Phase 5) | 2-3 | New `SchedulingProcessorLog.cs`, modified `SchedulingActivitySource.cs`, new `SchedulingProcessorMetrics.cs` |
| Public API | 1 | Modify `PublicAPI.Unshipped.txt` (many new symbols) |
| Unit tests (Phase 7a) | 4 | `ExponentialBackoffRetryPolicyTests.cs`, `CompiledExpressionScheduledMessageDispatcherTests.cs`, `SchedulerOrchestratorRopCallbackTests.cs`, `ScheduledMessageProcessorTests.cs` |
| Guard tests (Phase 7b) | 4 | Processor + Dispatcher + Retry policy + Orchestrator-retry-policy |
| Contract tests (Phase 7c) | 3 | Processor lifecycle, `IScheduledMessageRetryPolicy` contract, `IScheduledMessageDispatcher` contract |
| Integration tests (Phase 7d) | 1 | `ScheduledMessageProcessorSqliteIntegrationTests.cs` |
| Justifications (Phase 7e) | 3 | Property, load, benchmark `.md` files |
| Documentation (Phase 8) | 4-5 | `CHANGELOG.md`, `docs/features/scheduling.md`, `INVENTORY.md`, new ADR, optional README |
| **Total** | **~30-35 files touched** | ~13 new source files, ~12 new test files, ~5 docs/config |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing Issue #765 — the missing ScheduledMessageProcessor for Encina.Messaging.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library for CQRS + Event Sourcing with messaging patterns (Outbox, Inbox, Saga, Scheduling)
- Pre-1.0: NO backward compatibility, ALWAYS choose the best architectural option not the smallest diff
- Railway Oriented Programming: Either<EncinaError, T> everywhere — NEVER throw exceptions for business failures
- Pluggable abstractions over swappable implementations is Encina's identity
- The scheduling subsystem has IScheduledMessageStore implemented for 8 providers AND a SchedulerOrchestrator with ProcessDueMessagesAsync loop — but no BackgroundService invokes it, AND three latent design issues need fixing properly.

IMPLEMENTATION OVERVIEW:
New files in src/Encina.Messaging/Scheduling/:
- IScheduledMessageRetryPolicy.cs (public interface)
- RetryDecision.cs (public sealed record + factory helpers)
- ExponentialBackoffRetryPolicy.cs (public sealed default impl)
- IScheduledMessageDispatcher.cs (public interface)
- CompiledExpressionScheduledMessageDispatcher.cs (public sealed default impl, expression-tree based, zero reflection on hot path)
- ScheduledMessageProcessor.cs (public sealed BackgroundService)

New files in src/Encina.Messaging/Diagnostics/:
- SchedulingProcessorLog.cs (EventIds 2320-2349, within already-registered MessagingScheduling 2300-2399)
- SchedulingProcessorMetrics.cs (new Meter instruments)

Modified files:
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs (constructor takes IScheduledMessageRetryPolicy; ProcessDueMessagesAsync callback returns Either<EncinaError, Unit>; CancellationToken propagated; MarkAsFailedAsync delegates to retry policy)
- src/Encina.Messaging/Diagnostics/SchedulingActivitySource.cs (StartProcessingCycle helper)
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs (TryAddSingleton retry policy, TryAddScoped dispatcher, AddHostedService processor)
- src/Encina.Messaging/PublicAPI.Unshipped.txt (many new public symbols)

Phase 1: Retry policy abstraction + orchestrator refactor (constructor change + ROP callback + cancellation propagation + delegation to retry policy)
Phase 2: Dispatcher abstraction + compiled-expression default (zero reflection on hot path, zero dynamic, AOT-friendlier, returns Either)
Phase 3: ScheduledMessageProcessor BackgroundService — resolves dispatcher via DI, NEVER instantiates directly
Phase 4: DI registration of all 3 abstractions + hosted service inside the existing UseScheduling block
Phase 5: SchedulingProcessorLog (EventIds 2320-2349) + cycle Activity helpers + Meter instruments
Phase 6: Cross-cutting: verify OTel/Logging/Resilience/Transactions; add TODO comments for ⏭️ deferred items (#716, #735, #739, #749)
Phase 7: Unit tests (retry policy + dispatcher + ROP callback + processor), guard tests, contract tests for both abstractions, Sqlite integration tests; justification .md files for property/load/benchmark
Phase 8: CHANGELOG (Added + Fixed + Changed sections), README, docs/features/scheduling.md, INVENTORY, NEW ADR, build verification

KEY PATTERNS:
- ROP throughout: callback returns Either<EncinaError, Unit>; orchestrator inspects, on Left delegates to retry policy
- Dispatcher uses System.Linq.Expressions.Lambda<...>().Compile() per request Type, cached in static ConcurrentDictionary — zero MethodInfo.Invoke, zero `dynamic`
- Retry policy is synchronous, deterministic, takes (retryCount, maxRetries, nowUtc) — orchestrator owns time
- Processor runs as singleton (HostedService); creates a scope per cycle and resolves SchedulerOrchestrator + IScheduledMessageDispatcher from the scope
- EnableProcessor gating at both DI time and runtime
- Integration tests use [Collection("ADO-Sqlite")], real handler, real IEncina, real store, real processor — no mocks for end-to-end
- SQLite shared connection rules: never dispose, use ClearAllDataAsync in InitializeAsync
- NO ScheduledDispatchException — failures are values

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs (the domain logic we refactor)
- src/Encina.Messaging/Scheduling/SchedulingOptions.cs (configuration)
- src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs (abstraction)
- src/Encina.EntityFrameworkCore/Outbox/OutboxProcessor.cs (BackgroundService reference)
- src/Encina.Messaging/Recoverability/DelayedRetryProcessor.cs (another in-core BackgroundService)
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs (DI hook)
- src/Encina.Messaging/Diagnostics/SchedulingActivitySource.cs, SchedulingStoreLog.cs (diagnostics patterns)
- src/Encina/Abstractions/IEncina.cs (target dispatcher)
- src/Encina/Diagnostics/EventIdRanges.cs (MessagingScheduling range = 2300-2399)
- Microsoft docs on System.Linq.Expressions
- CLAUDE.md § "Scripting & Tooling Policy" (pwsh + C# scripts only — no bash/python)
- CLAUDE.md § "Structured Logging & EventId Allocation"
- CLAUDE.md § "Collection Fixtures (Container Reduction Strategy)"
- CLAUDE.md § "SQLite Shared Connection Pattern"
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ❌ N/A | The processor's hot path is `IScheduledMessageStore.GetDueMessagesAsync` — intentionally non-cacheable. Caching would serve stale results and miss newly due messages. |
| 2 | **OpenTelemetry** | ✅ Included | Phase 5: new cycle-level Activity via `SchedulingActivitySource.StartProcessingCycle`; new Meter instruments (`scheduled_messages_processed_total`, `scheduled_messages_dispatched_total`, `scheduled_processor_cycle_duration_seconds`). |
| 3 | **Structured Logging** | ✅ Included | Phase 5: new `SchedulingProcessorLog.cs` with `[LoggerMessage]` source generator, EventIds 2320-2349 within the already-registered `MessagingScheduling` range. |
| 4 | **Health Checks** | ⏭️ Deferred | Processor-lag health check is deferred to a follow-up issue (not yet filed). Per issue #765 body. A TODO comment in `ScheduledMessageProcessor.cs` will reference this. |
| 5 | **Validation** | ❌ N/A | Scheduled messages are validated at scheduling time via `SchedulerOrchestrator.ScheduleAsync`. Re-validation at execution time is unnecessary. |
| 6 | **Resilience** | ✅ Included | Phase 1: pluggable `IScheduledMessageRetryPolicy` abstraction with `ExponentialBackoffRetryPolicy` default (`delay = BaseRetryDelay * 2^retryCount`); dead-lettering after `MaxRetries` exhausted. Users can swap in a custom policy via DI; future `Encina.Polly.Scheduling` adapter package can ship a Polly-backed policy without core changes. Phase 3: exception isolation in `ExecuteAsync` outer loop prevents crash-looping. |
| 7 | **Distributed Locks** | ⏭️ Deferred (#716) | Without a distributed lock, multi-replica deployments will process the same message on every instance. Explicitly deferred to Issue #716. A TODO comment in `ScheduledMessageProcessor.cs` will cite #716. |
| 8 | **Transactions** | ✅ Included | `IScheduledMessageStore.MarkAsProcessedAsync` and `MarkAsFailedAsync` are single-statement atomic operations in all 8 provider implementations. No processor-side change needed. Verified in Phase 7 integration tests (no half-committed state on cancellation). |
| 9 | **Idempotency** | ⏭️ Deferred (#735) | Without idempotency tracking, a message dispatched successfully but failed-to-mark could be re-executed. Explicitly deferred to Issue #735. TODO comment will cite #735. |
| 10 | **Multi-Tenancy** | ⏭️ Deferred (#739) | `IScheduledMessage` does not currently carry `TenantId`, so the processor cannot set `ITenantContext` before dispatching. Explicitly deferred to Issue #739. TODO comment will cite #739. |
| 11 | **Module Isolation** | ⏭️ Deferred | No `ModuleId` field yet on `IScheduledMessage`. Follow-up issue can be filed when module isolation becomes generally available for background processors. TODO comment will note this. |
| 12 | **Audit Trail** | ⏭️ Deferred (#749) | Background processor audit events are covered by a broader initiative (#749). No per-execution audit entry in this PR. TODO comment will cite #749. |

**Summary**: 4 ✅ (included), 6 ⏭️ (deferred with issue references), 2 ❌ (N/A with justification). All ✅ items are wired into the implementation phases; all ⏭️ items have TODO comments in the processor source.

---

## Next Steps

1. **Review and approve this plan** — confirm placement, design choices, and phase boundaries
2. **Comment the plan link on Issue #765** — so CodeRabbit can validate subsequent PRs against the plan
3. **Phase 1 in a new session** — orchestrator fix is isolated, testable independently, and unblocks Phases 2-3
4. **Each phase = one commit** — keeps review small and bisectable
5. **Final commit references `Fixes #765`** and cites the milestone `v0.13.4`
