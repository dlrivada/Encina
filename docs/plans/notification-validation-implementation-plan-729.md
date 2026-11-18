# Implementation Plan: Validation Support for INotification Handlers

> **Issue**: [#729](https://github.com/dlrivada/Encina/issues/729)
> **Type**: Feature (validation, messaging)
> **Related**: #730 (CDC event validation) and #732 (saga step validation) reuse the shared pieces this plan owns (`ValidationOrchestrator.ValidateMessageAsync` and `EvaluateMessageAsync`); #726 (central validation instrumentation); #702 (validator resolution cache, merged plan); #230 (owns `IRequestValidator<T>`), #233 (owns the `ErrorCode` contract), #3415 (a code-less validation `Left` never becomes a 400), ADR-001, ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium (one dispatcher hook, one orchestrator method, no persistence)
> **Estimated Scope**: ~2,700 lines in total (~420 production, ~1,700 tests, ~580 documentation), in four pull requests of at most 14 files and 1,000 lines each

---

## Summary

### What exists today (read on 2026-11-17, main `822c310c7`)

- `ValidationPipelineBehavior<TRequest, TResponse>` is constrained to `IRequest<TResponse>` (`src/Encina/Validation/ValidationPipelineBehavior.cs:36`). A notification is not a request, so it never meets it.
- Notifications have no pipeline at all. `NotificationDispatcher.ExecuteAsync` (`src/Encina/Dispatchers/Encina.NotificationDispatcher.cs`) creates a scope, resolves every `INotificationHandler<>` of the runtime type (`:52-65`), returns `Right` when there are none (`:67-73`) and hands the handler list to the configured dispatch strategy (`:79`). Its own remarks say "No behaviors or processors". The issue's `NotificationValidationPipelineBehavior<TNotification>` therefore has nothing to attach to.
- All three providers are already generic over the validated type: `IValidationProvider.ValidateAsync<TRequest>(TRequest, IRequestContext, CancellationToken)` has no constraint (`src/Encina/Validation/IValidationProvider.cs`), `FluentValidationProvider` resolves `IEnumerable<IValidator<TRequest>>` (`FluentValidationProvider.cs:44`), `DataAnnotationsValidationProvider` calls `Validator.TryValidateObject` on the instance, and `AddEncinaFluentValidation` registers every `IValidator<T>` it finds in the scanned assemblies regardless of `T` (`ServiceCollectionExtensions.cs:114-143`). Validators for notification types need no per-provider code (the issue's "validator discovery" item is already true).
- `ValidationOrchestrator.ValidateAsync<TRequest, TResponse>` returns a code-less `Left` (`EncinaError.New(message)`, `ValidationOrchestrator.cs:70-71`), which ADR-062 treats as `Internal`; it also joins the validators' own messages into the error text, which may echo submitted values.
- `DefaultErrorClassifier` (`src/Encina/Recoverability/IErrorClassifier.cs:134-157`) keeps an explicit list of permanent codes ("a failed validation"), so the recoverability pipeline (`RecoverabilityPipelineBehavior`, the Polly and Hangfire or Quartz adapters) treats a deterministic validation failure as permanent instead of retrying it; no code of this feature is in it yet. The built-in `OutboxBatchProcessor` does not classify errors (`src/Encina.Messaging/README.md:296`: it only schedules the next attempt), so an outbox row whose publish returns the rejection is retried on the outbox's own schedule until it is handled there.
- The notification activity (`Encina.Publish`) and the logs of the dispatcher live in the core range 100-199; there is no validation EventId range (`EventIdRanges.cs` has none).

### What this plan delivers

An opt-in gate in the notification dispatcher that validates the notification once, before the fan-out to its handlers, with whichever provider is registered:

1. **`ValidationOrchestrator.ValidateMessageAsync<TMessage>`** (owned by this plan, used by the notification gate; its companion `EvaluateMessageAsync` below is the piece #730 and #732 consume): validates any message object with the registered `IValidationProvider` and returns a coded, categorized `Left`; failure details carry property names and counts only. Its companion `EvaluateMessageAsync<TMessage>` returns the provider's `ValidationResult` unchanged, for callers that merge several validators (#730).
2. **`AddEncinaNotificationValidation(Action<NotificationValidationOptions>?)`**: registers the internal gate and the options (`Mode`: `Reject` by default, or `Skip`).
3. **The dispatcher hook**: after the handlers are resolved and only when at least one exists, the gate validates the runtime notification; `Reject` returns the `Left` and no handler runs, `Skip` logs and returns `Right` without running any handler.

Nothing changes for an application that does not call `AddEncinaNotificationValidation()`.

- Affected packages: `Encina` (core) only. `Encina.FluentValidation`, `Encina.DataAnnotations` and `Encina.MiniValidator` need no code change; their documentation pages gain a notification example.
- Provider category: Validation (3) (`AGENTS.md` section 5): one integration test per provider runs the same invalid notification through `IEncina.Publish` (Design Choice 4).
- Shared pieces, owned by this plan: `ValidateMessageAsync`, `EvaluateMessageAsync`, `MessageValidationErrorCodes` (`encina.validation.message_invalid`, `encina.validation.message_cancelled`, `encina.validation.provider_failed`) and the classifier entry; #730 and #732 build on them and open their pull requests after S1 of this plan.
- The issue's "optionally throw" criterion is Design Choice 5 (a Dilemma): the plan recommends `Reject` (a `Left`) in its place.
- Code names: the new codes use the `encina.validation.` prefix, not `validation.`, because #233's plan reserves `validation.<rule>` for localizable rule keys (`ValidationMessageKeys`); the `Encina.Testing` assertions already look for the `encina.validation` prefix. `EvaluateMessageAsync` and `ValidateMessageAsync` call the provider as `ValidateAsync` does today; whether they run inside the culture scope #233 adds to the orchestrator is #233's decision for the orchestrator as a whole, and nothing here depends on it.

---

## Design Choices

<details>
<summary><strong>1. Where validation hooks into notification publishing</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An internal gate called by `NotificationDispatcher` after handler resolution, enabled by registering it** | Smallest change: one call site, no new public pipeline concept; runs once per publish, before the fan-out; pay-for-what-you-use | Validation is the only cross-cutting step that notifications get; a second one (audit, tenant checks) needs its own hook or a later pipeline |
| **B) A new public notification pipeline (`INotificationPipelineBehavior<TNotification>` plus an ordering contract) with `NotificationValidationBehavior` as its first member** | Matches the issue's class name; later cross-cutting steps for notifications have a home | A new public contract, ordering rules and a dispatcher rewrite for one consumer; every other pipeline-related plan would need to say whether it applies to notifications |
| **C) Each handler validates its own notification** | No framework change | The very failure the issue describes: every handler repeats it or forgets it |

### Chosen Option: **A — Internal gate in the dispatcher** (recommended, pending the maintainer)

### Rationale

- A does the one thing the issue needs (validate once, before the blast radius) with the least public surface (`AGENTS.md` section 3: every line serves a current purpose).
- B wins when the maintainer wants a general notification pipeline for several upcoming cross-cutting features (audit of published notifications, tenant checks); the gate can then become the first behavior of that pipeline.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. What happens to an invalid notification (the default mode)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Reject` by default: `Publish` returns a `Left` (`encina.validation.message_invalid`, category `Validation`), no handler runs; `Skip` is an opt-in value of `NotificationValidationOptions.Mode`** | The publisher learns the notification was not delivered; recoverability policies classify the failure as permanent (register row C4), and the outbox keeps its own retry schedule; no silent loss | A `Left` surfaces in code paths that used to see success (an application that enables the gate and publishes invalid payloads sees failures it never saw) |
| **B) `Skip` by default (the issue's text: "log-and-skip")** | Enabling the gate cannot break a running application | `Publish` returns `Right` although no handler ran: the invalid notification is dropped and only a warning log shows it, which the issue itself calls a "silent failure" when it happens inside a handler |
| **C) `Warn`: log the failure and deliver anyway** | Zero behavior change; good for a rollout | Does not prevent the harm the issue describes (handlers still process the invalid payload) |

### Chosen Option: **A — `Reject` by default, `Skip` opt-in** (recommended, pending the maintainer)

### Rationale

- The issue proposes `Skip` as the default for being non-breaking, but Encina is pre-1.0 with no compatibility obligation (`AGENTS.md` section 1), and the gate is already opt-in by registration, so the application chose validation; reporting success for an undelivered notification is the outcome the issue wants to prevent.
- B wins for a team that enables the gate in production on a system that already emits some invalid notifications and must not fail the publishers while it cleans the data; `Skip` is available for that, and C can be added as a third value later without breaking A.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Which type is validated</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The runtime type of the notification (`notification.GetType()`), through a cached generic invoker** | Same type the dispatcher uses to resolve handlers (`Encina.NotificationDispatcher.cs:52` and `:63`), so a derived notification gets the validators registered for it | A cached compiled delegate per notification type (a bounded dictionary, the shape of `NotificationHandlerInvokerCache`) |
| **B) The compile-time type `TNotification`** | No reflection | A `Publish<INotification>(derived)` call validates the base type only, while handlers resolve for the derived type: the validators of the derived type never run |

### Chosen Option: **A — Runtime type** (recommended, pending the maintainer)

### Rationale

- Handler resolution and validation must agree on the type, or the acceptance criterion "notification payloads validated before handler execution" fails for polymorphic publishes; the dispatcher already chooses `notification?.GetType() ?? typeof(TNotification)` and resolves handlers with it (`src/Encina/Dispatchers/Encina.NotificationDispatcher.cs:52` and `:63`), with `NotificationHandlerInvokerCache` (`src/Encina/Core/Encina.cs:44`) as the cache precedent.
- B wins if the maintainer decides that notifications are always published with their concrete type and wants no per-type cache. This choice only applies if Design Choice 1 keeps the dispatcher gate.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Provider scope and where the provider registration happens</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Provider-neutral: the gate uses `ValidationOrchestrator` and so any registered `IValidationProvider`; the application registers the gate itself with `AddEncinaNotificationValidation()`** | All three providers work with no per-provider code; opt-in is explicit; one registration point | The application writes one more line next to its provider registration |
| **B) Each `AddEncina<Provider>` registers the gate automatically** | No extra line | Turns the feature on for every application that uses a validation package, against pay-for-what-you-use, and would change what `Publish` returns for them |
| **C) Three provider-specific gates** | Each could use its library's features | Triplicates the logic the orchestrator pattern exists to centralize |

### Chosen Option: **A — Provider-neutral, explicit registration** (recommended, pending the maintainer)

### Rationale

- The orchestrator pattern (`AGENTS.md` section 3) centralizes provider calls in `ValidationOrchestrator`; the feature is opt-in (`AGENTS.md` section 1, "pay-for-what-you-use").
- B wins if the maintainer wants validation of notifications on for every application that registers a validation package. The explicit registration also depends on Design Choices 1 and 2 (the gate and its options).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. The issue's "throw on failure" option</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No throwing mode: `Reject` returns a `Left`, which is the Railway Oriented Programming form of "fail"** | Consistent with ADR-001 and ADR-006 (no exceptions for business logic); callers handle one failure channel | The issue's literal "throw-on-failure" wording is not offered; a caller that wants an exception converts the `Left` itself |
| **B) A third mode `Throw` that raises an exception (for example `NotificationValidationException`) from `Publish`** | Matches the issue wording; useful for hosts that rely on exceptions to abort a unit of work | An exception type for a business failure, against `AGENTS.md` section 3; a second failure channel on `IEncina.Publish`, which returns `Either` |

### Chosen Option: **A — No throwing mode, `Reject` is the failing mode** (recommended, pending the maintainer)

### Rationale

- `AGENTS.md` section 3 says operations return `Either<EncinaError, T>` and never use exceptions for business logic.
- B wins only if the maintainer treats the issue's "throw-on-failure" acceptance criterion as binding for a host that cannot consume a `Left`.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | The failure details of the `Left` hold the message type name, the error count and the distinct failing property names (at most 20), never the validators' messages, never property values | Settled | `AGENTS.md` section 3 (`EncinaError.Message` and user data never reach logs or plaintext storage; #1168, #1173, #1259, #1274); ADR-054 settled point 2 |
| C2 | The `Left` message is the constant `Validation failed for {TypeName} with {n} error(s)`, not `ValidationResult.ToErrorMessage` (which joins validator texts that may echo values); the text is never logged | Settled | `AGENTS.md` section 3 (same rule as C1) |
| C3 | New codes, all created with the categorized `EncinaErrors.Create`: `encina.validation.message_invalid` (`ErrorCategory.Validation`, ADR-062 rule R3: field rules), `encina.validation.message_cancelled` (`ErrorCategory.Cancelled`, rule R5), `encina.validation.provider_failed` (`ErrorCategory.Internal`, rule R4: provider failures) | Settled | ADR-062 rules R3, R4, R5 (`docs/architecture/adr/062-error-category-contract.md`) |
| C4 | `encina.validation.message_invalid` is added to `DefaultErrorClassifier.PermanentErrorCodes`, so a recoverability policy treats an invalid notification as permanent instead of retrying it (the built-in outbox processor does not classify, `src/Encina.Messaging/README.md:296`); `encina.validation.provider_failed` stays unclassified (a provider outage can be transient) | Dilemma (follow-on of Design Choices 1 and 2 A, recommended, pending the maintainer) | `IErrorClassifier.cs:134-157` (the list exists for failures "whose outcome a retry cannot change"; a failed validation is named in its documentation); the code exists only if Design Choices 1 and 2 keep the gate |
| C5 | The gate runs only when at least one handler exists and before the dispatch strategy, so a notification nobody handles costs nothing and the validation happens once per publish, not once per handler | Dilemma (follow-on of Design Choice 1 A, recommended, pending the maintainer) | `Encina.NotificationDispatcher.cs:67-79` (zero handlers return before the execution phase); applies only if the dispatcher gate is chosen |
| C6 | The gate fails closed: an exception thrown by a validator, or a cancelled token, returns a `Left` (`encina.validation.provider_failed`, `encina.validation.message_cancelled`) and no handler runs; nothing is swallowed | Dilemma (follow-on of Design Choice 1 A, recommended, pending the maintainer) | `AGENTS.md` section 3 (gates fail closed; errors are never swallowed); applies only if the dispatcher gate is chosen |
| C7 | Telemetry on the `Encina.Publish` activity: `Encina.failure_reason` is set to the code on a rejection (existing tag), and `encina.tenant_id` only when `IRequestContext.TenantId` is not null; the reserved `default` tenant segment and injective tenant-qualified ids do not arise because the feature composes no key or id | Dilemma (follow-on of Design Choice 1 A, recommended, pending the maintainer) | ADR-054 D4 (tenant tag only when multi-tenancy is in use); `ActivityTagNames.FailureReason`; applies only if the dispatcher gate is chosen |
| C8 | EventIds 650-652 in the new range `ValidationNotification = (650, 659)` registered first in `EventIdRanges.cs`, packed from the start of the block 650-699 assigned to this family; 653-659 stay free | Trivial | `AGENTS.md` section 7 (ADR-021); `artifacts/decisions/plan-batch/eventid-blocks.md` |
| C9 | The gate is scoped and is resolved from the dispatcher's scope (`scope.ServiceProvider.GetService<INotificationValidationGate>()`), never injected into the singleton `Encina`; the options are a singleton `IOptions`; a missing gate means the feature is off | Dilemma (follow-on of Design Choice 1 A, recommended, pending the maintainer) | `AGENTS.md` section 3 (registration completeness; no scoped service in a singleton); `Encina.NotificationDispatcher.cs:48` (scope per publish); applies only if the dispatcher gate is chosen |
| C10 | `NotificationValidationMode` has two values, `Reject = 0` and `Skip = 1`; an out-of-range value is treated as `Reject` (fail closed) | Dilemma (follow-on of Design Choice 2 A, recommended, pending the maintainer) | `AGENTS.md` section 3 (gates fail closed); applies only if the two-value mode is chosen |
| C11 | Metrics (counts, duration, failure rate) are not added here; #726 instruments `ValidationOrchestrator` centrally and `ValidateMessageAsync` is covered by it, so the instrument has one owner | Trivial | #726; ADR-054 settled point 8 (one owner per `ActivitySource` name) and point 7 (instrumentation costs nothing without a listener) |

---

## Implementation Phases

### Phase 1: Shared Message Validation (pull request S1)

> **Goal**: Any message object can be validated with the registered provider and fail with a coded, categorized `Left`; #730 and #732 can build on it.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/MessageValidationErrorCodes.cs`: `public static class MessageValidationErrorCodes` with `MessageInvalid = "encina.validation.message_invalid"`, `MessageCancelled = "encina.validation.message_cancelled"`, `ProviderFailed = "encina.validation.provider_failed"` (register row C3), XML docs naming the category of each.
2. **Modify** `src/Encina/Validation/ValidationOrchestrator.cs`: add `public async ValueTask<Either<EncinaError, Unit>> ValidateMessageAsync<TMessage>(TMessage message, IRequestContext context, CancellationToken cancellationToken) where TMessage : class`. `ArgumentNullException.ThrowIfNull` on `message` and `context`; calls `_validationProvider.ValidateAsync(message, context, cancellationToken)`; an invalid result becomes `EncinaErrors.Create(MessageInvalid, ErrorCategory.Validation, <constant text>, details)` with details `validation.message_type`, `validation.error_count`, `validation.properties` (register rows C1, C2); an `OperationCanceledException` while the token is cancelled becomes `MessageCancelled`; any other exception becomes `EncinaErrors.FromException(ProviderFailed, ErrorCategory.Internal, ex, message: "The validation provider failed.")` with a constant `message:` (the overload otherwise copies `ex.Message` into the `Left`, `EncinaErrors.cs`), and a test asserts the `Left` text never contains the exception message. Also add the shared helper `public static ValidationFailureSummary Summarize(this ValidationResult result, int maxProperties = 20)` in `src/Encina/Validation/ValidationFailureSummary.cs` (the file holds a `public static class ValidationFailureSummaryExtensions` for the extension method and a `public sealed record ValidationFailureSummary` for its result, which carries the error count and at most 20 distinct property names; used by `ValidateMessageAsync`, by #730 and by #732 so the names-and-counts logic exists once). Also add `public ValueTask<ValidationResult> EvaluateMessageAsync<TMessage>(TMessage message, IRequestContext context, CancellationToken cancellationToken) where TMessage : class`: the same argument checks, then returns `_validationProvider.ValidateAsync(message, context, cancellationToken)` unchanged (an exception or a cancellation propagates to the caller); `ValidateMessageAsync` calls it, so the provider is invoked in one place. The existing `ValidateAsync<TRequest, TResponse>` is not touched (its code-less `Left` is #3415's concern).
3. **Modify** `src/Encina/Recoverability/IErrorClassifier.cs`: add `MessageValidationErrorCodes.MessageInvalid` to `PermanentErrorCodes` (register row C4).
4. **Modify** `src/Encina/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json` through `mechanical-fixer`.
5. Tests (new): `tests/Encina.UnitTests/Validation/ValidationOrchestratorMessageTests.cs` (`Summarize` caps and de-duplicates the names; valid result gives `Right`; invalid gives the code, `ErrorCategory.Validation`, the details with names and counts, no validator text and no property value in the error text; more than 20 properties are capped; cancellation; a throwing provider; each against a fake `IValidationProvider`; `EvaluateMessageAsync` returns the provider's result unchanged and lets an exception propagate) and `tests/Encina.GuardTests/Core/Validation/ValidationOrchestratorMessageGuardTests.cs` (null message and null context, for both methods; `Summarize` with a null result and with a non-positive bound, which reaches the 90 guard target of `ValidationFailureSummary.cs`). (edited) `tests/Encina.UnitTests/Recoverability/DefaultErrorClassifierTests.cs` (the new code is `Permanent`, `encina.validation.provider_failed` is not).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #729 plan docs/plans/notification-validation-implementation-plan-729.md, phase 1
of 4. ValidationOrchestrator (src/Encina/Validation/) delegates to the one registered IValidationProvider; ADR-062 gives
every EncinaError a category set with EncinaErrors.Create(code, category, message, ...).

TASK:
1. Create MessageValidationErrorCodes (three codes) and add ValidationOrchestrator.ValidateMessageAsync<TMessage>
   (TMessage : class) returning Either<EncinaError, Unit>, and EvaluateMessageAsync<TMessage> returning the provider's
   ValidationResult unchanged (ValidateMessageAsync calls it).
2. Invalid result: EncinaErrors.Create(MessageInvalid, ErrorCategory.Validation, constant text, details with
   validation.message_type, validation.error_count, validation.properties (distinct names, at most 20)).
3. Cancelled token: MessageCancelled (ErrorCategory.Cancelled). Other exception: FromException(ProviderFailed,
   ErrorCategory.Internal). Add MessageInvalid to DefaultErrorClassifier.PermanentErrorCodes.

KEY RULES:
- Never put ValidationError.ErrorMessage or any property value in the error text, details or logs.
- Leave ValidateAsync<TRequest,TResponse> unchanged. Tests execute the real orchestrator with a fake provider.
- PublicAPI.Unshipped.txt and the coverage manifest through mechanical-fixer.

REFERENCE FILES:
- src/Encina/Validation/ValidationOrchestrator.cs, ValidationResult.cs
- src/Encina/Recoverability/IErrorClassifier.cs (:134-157)
- docs/architecture/adr/062-error-category-contract.md
```

</details>

---

### Phase 2: Gate, Options, Registration and Logging (pull request S2)

> **Goal**: The gate exists, is configured by options and registered on request, and is proven in isolation; the dispatcher does not call it yet.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/NotificationValidationMode.cs` (`public enum NotificationValidationMode { Reject = 0, Skip = 1 }`) and `src/Encina/Validation/NotificationValidationOptions.cs` (`public sealed class NotificationValidationOptions { public NotificationValidationMode Mode { get; set; } = NotificationValidationMode.Reject; }`), register rows C10 and Design Choice 2.
2. **Create** `src/Encina/Validation/INotificationValidationGate.cs` (`internal interface`; `ValueTask<Either<EncinaError, bool>> ShouldDispatchAsync(object notification, IRequestContext context, CancellationToken cancellationToken)`: `Right(true)` deliver, `Right(false)` skipped, `Left` rejected) and `src/Encina/Validation/NotificationValidationGate.cs` (`internal sealed`; constructor `(ValidationOrchestrator orchestrator, IOptions<NotificationValidationOptions> options, ILogger<NotificationValidationGate>? logger = null)`; resolves a per-type invoker from a static `ConcurrentDictionary<Type, Func<ValidationOrchestrator, object, IRequestContext, CancellationToken, ValueTask<Either<EncinaError, Unit>>>>` built with `MakeGenericMethod` (Design Choice 3); applies the mode; logs by notification type name, code and error count only).
3. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: register `ValidationNotification = (650, 659)`; **create** `src/Encina/Validation/NotificationValidationLog.cs` (`[LoggerMessage]`, packed): 650 a notification was skipped as invalid (Warning), 651 a notification was rejected as invalid (Warning), 652 the validation of a notification failed unexpectedly (Error, exception through `ForLogging()`). XML docs name the range.
4. **Create** `src/Encina/Validation/NotificationValidationServiceCollectionExtensions.cs`: `public static IServiceCollection AddEncinaNotificationValidation(this IServiceCollection services, Action<NotificationValidationOptions>? configure = null)`; `services.AddOptions<NotificationValidationOptions>()`, applies `configure`, `TryAddScoped<INotificationValidationGate, NotificationValidationGate>()` (register row C9). It does not register a provider: the application registers one of the three, and `ValidateOnBuild` fails without one.
5. **Modify** `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (assembly `Encina` also maps to `ValidationNotification`), `src/Encina/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json` through `mechanical-fixer`.
6. Tests (new): `tests/Encina.UnitTests/Validation/NotificationValidationGateTests.cs` (valid notification delivers; `Reject` returns the `Left` and logs 651; `Skip` returns `Right(false)` and logs 650; a derived notification is validated by the validators of its runtime type; a throwing validator and a cancelled token fail closed; an out-of-range mode rejects; each against a real orchestrator with a fake provider), `NotificationValidationLogTests.cs` (EventIds, no message or value in the rendered text), `tests/Encina.GuardTests/Core/Validation/NotificationValidationGuardTests.cs` (null orchestrator and options in the gate constructor, null notification and context, null `services`; a null `configure` and a null logger are valid cases), `NotificationValidationRegistrationTests.cs` (provider built with `ValidateOnBuild` and `ValidateScopes`: with a provider registered, resolves in a scope; without one, the build fails; the options default to `Reject`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #729 plan docs/plans/notification-validation-implementation-plan-729.md, phase 2 of 4. Depends on S1
(ValidationOrchestrator.ValidateMessageAsync). The notification dispatcher is src/Encina/Dispatchers/Encina.NotificationDispatcher.cs.

TASK:
1. Add NotificationValidationMode, NotificationValidationOptions, the internal INotificationValidationGate and
   NotificationValidationGate, AddEncinaNotificationValidation and NotificationValidationLog (EventIds 650-652).
2. Register ValidationNotification (650, 659) in EventIdRanges.cs and map the Encina assembly to it in
   EncinaEventIdAllocationTests.

KEY RULES:
- The gate validates the runtime type through a cached invoker; Reject returns the Left, Skip returns Right(false).
- Fail closed on exceptions and cancellation. Log only the notification type name, the code and the error count.
- The gate is scoped; never inject it into a singleton. A DI test builds the provider with ValidateOnBuild and ValidateScopes.
- PublicAPI.Unshipped.txt and the manifest through mechanical-fixer.

REFERENCE FILES:
- src/Encina/Validation/ValidationOrchestrator.cs
- src/Encina/Core/Encina.cs (:44, NotificationHandlerInvokerCache)
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 3: Dispatcher Wiring and Provider Tests (pull request S3)

> **Goal**: `IEncina.Publish` validates before the fan-out, proven with each of the three providers.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Dispatchers/Encina.NotificationDispatcher.cs`: after the zero-handler return and before the execution phase, resolve `scope.ServiceProvider.GetService<INotificationValidationGate>()`; when it exists, call `ShouldDispatchAsync(notification, context, cancellationToken)`; a `Left` is returned after tagging the activity with `Encina.failure_reason` (and `encina.tenant_id` when present, register row C7) and setting the error status; `Right(false)` returns `Right(Unit.Default)` with the activity status `Ok`; `Right(true)` continues unchanged.
2. Tests (new): `tests/Encina.UnitTests/Dispatchers/NotificationValidationDispatchTests.cs` (through a real `Encina` and a real gate: a valid notification reaches every handler; `Reject` returns the `Left` and no handler ran; `Skip` returns `Right` and no handler ran; no handler registered means no validation; no gate registered means unchanged behavior; sequential and parallel strategies; the activity tags), and `tests/Encina.IntegrationTests/Validation/FluentValidation/NotificationValidationPipelineTests.cs`, `.../DataAnnotations/NotificationValidationPipelineTests.cs`, `.../MiniValidator/NotificationValidationPipelineTests.cs` (the same scenario through each provider's real registration: an invalid notification rejected before two handlers, a valid one delivered to both, the error code `encina.validation.message_invalid`, no container).
3. **Modify** `.github/coverage-manifest/Encina.json` through `mechanical-fixer`: the integration target of `NotificationValidationGate.cs` (its tests are in this PR) and the whole-file targets of `Encina.NotificationDispatcher.cs`, set not below the baseline measured first. No `PublicAPI` change is needed beyond S2.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #729 plan docs/plans/notification-validation-implementation-plan-729.md, phase 3 of 4. Depends on S2.

TASK:
1. In NotificationDispatcher.ExecuteAsync, after the zero-handler return, resolve INotificationValidationGate from the
   scope and call ShouldDispatchAsync; return the Left, or Right(Unit) when the gate says skip, else continue.
2. Add the dispatcher unit tests and one integration test per provider (FluentValidation, DataAnnotations, MiniValidator).

KEY RULES:
- No gate registered means no behavior change. Zero handlers means no validation.
- Tag Encina.failure_reason with the code; encina.tenant_id only when TenantId is not null; never the error message.
- Integration tests use the real provider registration and IEncina.Publish; no mocks of the dispatcher.

REFERENCE FILES:
- src/Encina/Dispatchers/Encina.NotificationDispatcher.cs (:35-79)
- src/Encina.FluentValidation/ServiceCollectionExtensions.cs, src/Encina.DataAnnotations/ServiceCollectionExtensions.cs,
  src/Encina.MiniValidator/ServiceCollectionExtensions.cs
- tests/Encina.IntegrationTests/Validation/
```

</details>

---

### Phase 4: Documentation and Finalization (pull request S4)

> **Goal**: The feature is documented and ready to close the issue.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `docs/features/notification-validation.md` through `docs-writer` (what is validated and when, the two modes and the consequence of `Skip`, one example per provider, how rejection interacts with recoverability policies (permanent) and with the outbox (its own retry schedule, no classification), the failure code and category); add its entry to `docs/features/index.md`.
2. **Modify** `docs/features/pipeline-behaviors.md` (a pointer: notifications have a dispatcher gate, not a pipeline behavior), `src/Encina.FluentValidation/README.md`, `src/Encina.DataAnnotations/README.md` and `src/Encina.MiniValidator/README.md` through `docs-writer`.
3. **Create** `changelog.d/729-notification-validation.added.md` through `mechanical-fixer`.
4. **Create** the justification files for Load and Benchmark tests (`AGENTS.md` section 9): `tests/Encina.LoadTests/Validation/NotificationValidation.md` and `tests/Encina.BenchmarkTests/Validation/NotificationValidation.md` (the gate holds no shared mutable state apart from the invoker cache; the overhead is one provider call per publish and is measured by #726's instrumentation).
5. Verification: `dotnet build --configuration Release` with zero warnings, `dotnet test` of the touched projects, per-flag coverage of the touched files against the table below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #729 plan docs/plans/notification-validation-implementation-plan-729.md, phase 4 of 4. Depends on S1 to S3.

TASK:
1. Write the feature page and update the pipeline-behaviors page and the three provider READMEs (docs-writer).
2. Add the changelog fragment (mechanical-fixer) and the two justification files for Load and Benchmark tests.

KEY RULES:
- Pages go through docs-writer; fragments through mechanical-fixer. Never type coverage figures by hand.
- No private client application is named: write "the reference application" if one is needed.
- Link sibling plans by issue number only; the ADR number is not needed (no ADR in this plan).

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- docs/features/pipeline-behaviors.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| ADR-001, ADR-006 | Railway Oriented Programming; no exceptions for business logic | `Reject` is a `Left`; the issue's "throw" option is not offered |
| ADR-062 | Category contract; rules R3 (field rules are `Validation`), R4 (provider failures are `Internal`), R5 (cancellation) | Categories of the three codes (register row C3) |
| ADR-054 D4 and settled points 2 and 7 | Tenant tag only with multi-tenancy; error status carries code or type only; no instrument without a listener design | Telemetry and log shape (rows C1, C7, C11) |
| `AGENTS.md` sections 1 and 3 | Pay-for-what-you-use; gates fail closed; errors never swallowed; validation uses the orchestrator pattern | Opt-in registration, fail-closed gate, provider-neutral design |
| Enterprise Integration Patterns: Message Filter / Invalid Message Channel | Invalid messages are routed away from consumers | Reject (to the caller and recoverability) and Skip semantics |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `NotificationDispatcher`, `NotificationHandlerInvokerCache` | `src/Encina/Dispatchers/Encina.NotificationDispatcher.cs`, `src/Encina/Core/Encina.cs` | The hook site and the cache precedent |
| `ValidationOrchestrator`, `IValidationProvider`, `ValidationResult` | `src/Encina/Validation/` | Provider-neutral validation of the notification |
| `ErrorCategory`, `EncinaErrors.Create`, `GetCategory` | `src/Encina/Errors/ErrorCategory.cs`, `src/Encina/Results/EncinaErrors.cs` | Coded, categorized errors |
| `DefaultErrorClassifier` | `src/Encina/Recoverability/IErrorClassifier.cs` | Permanent classification of the invalid notification |
| `ActivityTagNames`, `EncinaDiagnostics.ActivitySource` | `src/Encina/Diagnostics/` | The `Encina.Publish` activity the gate tags |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs`, `tests/Encina.UnitTests/Testing/Architecture/` | New range registration and mapping |
| The three validation providers | `src/Encina.FluentValidation`, `src/Encina.DataAnnotations`, `src/Encina.MiniValidator` | Unchanged; exercised by the integration tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` core (`ValidationNotification`, new, inside the block 650-699 assigned to the validation-3 family) | 650-659 | 650-652 used (three messages); 653-659 free; sibling ranges of the family: #730 uses `ValidationCdc` 660-669, #732 uses `ValidationSaga` 670-679 |

### Estimated File Count

Every count includes tests, coverage manifest edits, PublicAPI files, READMEs and the changelog fragment. No pull request uses a type created by a later one; each enters its per-file manifest targets with its own tests.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Shared `ValidateMessageAsync` and `EvaluateMessageAsync`, codes, classifier entry | 2 (`MessageValidationErrorCodes`, `ValidationFailureSummary`) | 3 (`ValidationOrchestrator`, `IErrorClassifier`, PublicAPI) | 2 (unit, guard) | 1 (`DefaultErrorClassifierTests`) | 1 | 0 | 9 | ~680 |
| S2 | Mode, options, gate, log, registration | 6 (`NotificationValidationMode`, `NotificationValidationOptions`, `INotificationValidationGate`, `NotificationValidationGate`, `NotificationValidationLog`, `NotificationValidationServiceCollectionExtensions`) | 2 (`EventIdRanges.cs`, PublicAPI) | 4 (gate, log, guard, registration) | 1 (`EncinaEventIdAllocationTests`) | 1 | 0 | 14 | ~950 |
| S3 | Dispatcher wiring and provider tests | 0 | 1 (`Encina.NotificationDispatcher.cs`) | 4 (dispatch unit, 3 integration) | 0 | 1 | 0 | 6 | ~700 |
| S4 | Documentation and finalization | 0 | 0 | 0 | 0 | 0 | 9 (feature page, `docs/features/index.md`, `pipeline-behaviors.md`, 3 provider READMEs, changelog fragment, 2 justification `.md`) | 9 | ~520 |
| **Total** | | **8** | **6** | **10** | **2** | **3** | **9** | **38** | **~2,850** |

The columns sum to the total (8 + 6 + 10 + 2 + 3 + 9 = 38) and the pull-request rows sum to 38 files (9 + 14 + 6 + 9). The three integration test projects already reference the three validation packages (`Encina.IntegrationTests.csproj`), so no csproj edit is needed. S1 is also the prerequisite of #730 and #732. Verify caller counts with `git grep` before each pull request: `new Encina(` appears in 20 places and is untouched because the gate is resolved from the scope, not injected.

Per-file coverage targets (per flag, with a one-sentence justification), written into `.github/coverage-manifest/Encina.json` by the pull request whose tests reach them. Manifest targets are whole-file; `Encina.NotificationDispatcher.cs` and `ValidationOrchestrator.cs` hold defaults only today, so the PR that edits them first measures the file's baseline per flag and sets a whole-file target not below it (the figures below are the targets for the changed members, reached by the new tests, and are confirmed against the measured baseline when the PR runs the coverage report):

| File | Unit | Guard | Property | Integration | Justification |
|------|------|-------|----------|-------------|---------------|
| `Validation/ValidationOrchestrator.cs` (changed members) | 95 | 60 | 0 | 0 | Valid, invalid, cancelled and throwing-provider paths run against a fake provider; the guard tests reach the two argument checks of the new method; the logic is a result conversion, so no property adds coverage, and the integration flag runs the same method through the gate in S3 without adding a line |
| `Validation/MessageValidationErrorCodes.cs` | 100 | 0 | 0 | 0 | Constants with no executable logic and no parameters |
| `Validation/NotificationValidationGate.cs` | 95 | 70 | 0 | 80, added to the manifest by S3 (the PR that holds its integration tests; S2 sets the unit and guard targets only) | Every mode and failure path runs with a fake provider; the guard tests reach the constructor and the two argument checks; the three provider integration tests run the reject and deliver paths through `IEncina.Publish` (the `Skip`, out-of-range and exception paths are covered by the unit flag only) |
| `Validation/NotificationValidationOptions.cs`, `NotificationValidationMode.cs` | 100 | 0 | 0 | 0 | One property with a default and an enum; nothing to guard |
| `Validation/NotificationValidationLog.cs` | 90 | 0 | 0 | 0 | Three source-generated methods exercised by the logging test; no parameters to guard |
| `Validation/NotificationValidationServiceCollectionExtensions.cs` | 90 | 90 | 0 | 0 | The DI test covers every line; the guard test covers the null check |
| `Dispatchers/Encina.NotificationDispatcher.cs` (changed block) | 90 | 0 | 0 | 80 | The hook branches run in the dispatch unit tests and in the provider integration tests; the block adds no parameter, so guard tests cannot reach more |
| `Recoverability/IErrorClassifier.cs` (changed line) | unchanged | unchanged | unchanged | unchanged | The manifest already holds targets for this file (for example contract 80, property 75, integration 80) and the target-ratchet forbids lowering them; the one new list entry is covered by the existing classifier tests, so no target is edited |
| `Validation/ValidationFailureSummary.cs` | 100 | 90 | 0 | 0 | A small pure helper with a bound and a distinct-names rule, fully reached by unit tests; the guard test reaches the null result and the non-positive bound |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #729 - validation for INotification handlers in Encina.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Validation follows the orchestrator pattern (AGENTS.md sections 3 and 5): IValidationProvider, ValidationOrchestrator,
  and one of FluentValidation, DataAnnotations or MiniValidator behind the provider interface.
- Railway Oriented Programming: Either<EncinaError, T>; every error code carries an ErrorCategory (ADR-062).
- Notifications have no pipeline: NotificationDispatcher resolves handlers and calls the dispatch strategy.

IMPLEMENTATION OVERVIEW:
1. ValidationOrchestrator.ValidateMessageAsync<TMessage> and EvaluateMessageAsync<TMessage> plus MessageValidationErrorCodes
   and the classifier entry (S1).
2. NotificationValidationMode/Options, the internal gate, AddEncinaNotificationValidation, log 650-652 (S2).
3. The dispatcher hook after the zero-handler return, dispatch unit tests and one integration test per provider (S3).
4. Feature page, README updates, changelog fragment, justification files (S4).
Four pull requests, see the Estimated File Count table.

KEY PATTERNS:
- Never log or store EncinaError.Message, validator messages or property values; names and counts only.
- Reject by default (Left), Skip opt-in; fail closed on exceptions and cancellation.
- Gate scoped and resolved from the dispatcher's scope; DI test with ValidateOnBuild and ValidateScopes.
- PublicAPI.Unshipped.txt, manifests and changelog through mechanical-fixer; documentation through docs-writer.

REFERENCE FILES:
- src/Encina/Dispatchers/Encina.NotificationDispatcher.cs, src/Encina/Validation/*.cs
- src/Encina/Recoverability/IErrorClassifier.cs, src/Encina/Diagnostics/EventIdRanges.cs
- docs/plans/dsr-implementation-plan-404.md (style)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Validation of a notification is per publish and depends on the payload; the validator resolution cache of #702 already speeds the provider call |
| 2 | OpenTelemetry | ✅ Phase 3 | The `Encina.Publish` activity gets `Encina.failure_reason` on a rejection and `encina.tenant_id` when a tenant is present (ADR-054 D4); duration and counts stay with #726 |
| 3 | Structured Logging | ✅ Phase 2 | `NotificationValidationLog` EventIds 650-652 in the new range 650-659, by notification type, code and error count only |
| 4 | Health Checks | ❌ N/A | In-process; no checkable dependency |
| 5 | Validation | ✅ All phases | This is the validation feature; the three providers are exercised by the integration tests |
| 6 | Resilience | ❌ N/A | No external call; an invalid notification is permanent for recoverability policies (row C4) |
| 7 | Distributed Locks | ❌ N/A | No shared state beyond a static type cache |
| 8 | Transactions | ❌ N/A | Validation runs before any handler; it starts no transaction and never calls `SaveChangesAsync` |
| 9 | Idempotency | ❌ N/A | Validation is repeatable and side-effect free; duplicates are the inbox's concern |
| 10 | Multi-Tenancy | ✅ Phase 3 | `TenantId` reaches the provider through `IRequestContext` and the activity tag; no tenant-qualified id is composed, so the reserved `default` segment does not arise; tenant-specific rules for the message gates are a follow-up (draft `artifacts/issues/validation-message-gates-tenant-module-scoped.md`, shared with #730 and #732) |
| 11 | Module Isolation | ⏭️ Follow-up | Module-scoped validators for the message gates are the same follow-up as row 10 |
| 12 | Audit Trail | ⏭️ Follow-up | Auditing of rejected notifications belongs to the audit family; the gate's log is operational, not an audit record |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Validation orchestrator pattern and the three providers | ✅ On `main` | `ValidationOrchestrator`, `IValidationProvider` |
| ADR-062 error category contract (`GetCategory`, categorized `EncinaErrors.Create`) | ✅ Merged | The three codes depend on it |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Central validation instrumentation | #726 | Instruments the orchestrator methods once it lands |
| Code-less validation `Left` never becomes a 400 | #3415 | Independent: the new codes are categorized from the start |
| Validator resolution cache | #702 | Speeds the provider call; independent |

---

## Next Steps

1. Link this plan from #729 and ask the maintainer to answer Design Choices 1 and 2 when work starts.
2. Implement S1 to S4 as the pull requests of the Estimated File Count split, `Fixes #729` on S4. Open S1 first: #730 and #732 wait for it.
3. After S1 merges, the plans of #730 and #732 proceed; both use `EvaluateMessageAsync` as owned here and build their own coded errors from the merged results of their validators.
