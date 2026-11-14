# Implementation Plan: Enhanced Poison Message Detection and Handling

> **Issue**: [#135](https://github.com/dlrivada/Encina/issues/135)
> **Type**: Feature
> **Related**: #39 (recoverability, ADR-029), #583 (persistent dead letter queue, closed), #2013 (dead letter replay under the stored context; owns the audit port `IDeadLetterAuditSink`, a blocking prerequisite of the audit call, see Design Choice 7), #2229 (dead letter admin API), #2204 (`TotalAttempts` counts one too many), #2205 (failing dead letter store reruns exhausted messages), #747 (module isolation), ADR-029, ADR-036, ADR-046, ADR-050, ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium (one package plus core; no provider-dependent code under the recommended options of Dilemmas 2 and 6)
> **Estimated Scope**: ~1,500 lines of production code + ~3,600 lines of tests (~1,000 of them integration) + ~400 lines of documentation, in eight pull requests of at most 15 files and 1,000 lines each, if Dilemmas 2 and 6 take option A; the 10-provider store of Dilemma 2 option B or Dilemma 6 option B is a separate series

---

## Summary

### What exists today (read on 2026-11-11, main `68d08ddae`)

- `IErrorClassifier.Classify(EncinaError, Exception?)` returns `ErrorClassification` `Unknown`, `Transient` or `Permanent` (`src/Encina/Recoverability/IErrorClassifier.cs`); `DefaultErrorClassifier` checks a declared transience, then exception types and error codes, then the message text. `RecoverabilityOptions.ErrorClassifier` can replace it (`RecoverabilityOptions.cs:104`).
- `RecoverabilityPipelineBehavior` classifies in `HandleErrorResult` (`:407`) and `HandleException` (`:450`); a `Permanent` result stops the immediate retries and `HandlePermanentFailureAsync` (`:530`) builds the `FailedMessage`, captures it with `DeadLetterSourceCapture.CaptureFailedMessageAsync` and calls `OnPermanentFailure`. `RecoverabilityContext` keeps `RetryHistory`, `LastClassification` and `TotalAttempts` (`RecoverabilityContext.cs:55-105`).
- Payloads are deserialized in four background sites: `OutboxBatchProcessor.cs:182`, `DelayedRetryProcessor.cs:207`, `SchedulerOrchestrator.cs:450` and `DeadLetterManager.cs:360` (replay of a row that is already in the queue). None checks structure first; `IMessageSerializer.Deserialize` returns `null` on failure and each site maps that to its own failure today: the outbox calls `FailAsync` with `OutboxErrorCodes.DeserializationFailed` (store retry semantics), the delayed retry processor calls `FailTerminallyAsync`, the scheduler calls `MarkAsFailedAsync` with `SchedulingErrorCodes.DeserializationFailed`, and the manager rejects the replay with `DeadLetterErrorCodes.DeserializationFailed`.
- The dead letter queue is persistent (`IDeadLetterStore`, ADR-046) with sources `Recoverability`, `Outbox`, `Inbox`, `Scheduling`, `Saga` and `Choreography` (`DeadLetterSourcePatterns.cs`).
- Not in `src`: `IPoisonMessageDetector`, `PoisonClassification`, `PoisonAction`, `IQuarantineStore`, `IExceptionClassifier`, `MessageContext`. The issue's sketch `PoisonClassification.Transient/Permanent/Unknown` duplicates `ErrorClassification`.

### What this plan delivers

An opt-in poison layer in `Encina.Messaging`: finer classes (malformed payload, validation failure, security violation, resource exhausted) mapped from the existing classification, a configurable action per class, a cheap payload check before deserialization at three sites, quarantine of security violations, metrics, a health signal and an audit entry. Nothing changes unless `RecoverabilityOptions.PoisonMessage.Enabled` is set (default `false`); with it unset the detector is registered but never called, the payload guard is never called and the behavior is today's path.

- Affected packages: `Encina` (only if Dilemma 1 is option A), `Encina.Messaging`, `Encina.Security.Audit` (adapter owned by #2013; `Encina.Security.Audit` references `Encina.Messaging` in `Encina.Security.Audit.csproj`, so `Encina.Messaging` can only reach the audit store through the port, never the reverse), and the 10 provider packages only if Dilemma 2 or 6 is option B.
- Provider category: none for the detector; Database (10) only under Dilemma 2 option B or Dilemma 6 option B (`AGENTS.md` section 5). Under the recommended options the provider matrix is "none" and no store interface changes.
- Behavior change under opt-in (register row D12): with `Enabled = true` a malformed payload is captured in the dead letter queue and terminated on its first failure at the outbox, delayed retry and scheduler sites, instead of the per-site failure handling listed above; the issue's acceptance criterion requires it.

---

## Design Choices

<details>
<summary><strong>1. How do the finer classes relate to <code>IErrorClassifier</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extend `ErrorClassification` and `IErrorClassifier` with the new values** | One classifier for every retry layer (Polly retry, recoverability, delayed retry, job adapters); no second extension point | Changes a core enum that ADR-029 fixed at three values and that other layers switch on; every consumer must handle the new values |
| **B) Keep `ErrorClassification`; add `IPoisonMessageDetector` that maps (classification, error, history) to a `PoisonClassification` and a `PoisonAction`** | ADR-029 and the existing layers are untouched; opt-in (pay-for-what-you-use); the detector can use the failure history, which a classifier cannot | Two vocabularies to explain; the detector must call the classifier first |

### Chosen Option: **B — Poison layer over the existing classifier** (recommended, pending the maintainer)

### Rationale

- `RecoverabilityPipelineBehavior` already calls the classifier once per attempt; the detector runs right after it with the same inputs plus `RecoverabilityContext`, so no second classification pass exists.
- A wins when every consumer of the core enum (Polly, jobs) must see the finer classes; B wins when only the messaging recoverability path needs them.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where do quarantined messages live</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In the dead letter queue, with a reserved `ErrorCode` family (`poison.quarantined.*`) that `DeadLetterManager` refuses to replay (`dlq.replay_refused`); visibility in listings is Design Choice 6** | No new store, schema or tests on the 10 providers; tenant scoping, retention and the #2229 admin API are reused | The replay surface must refuse by code; a quarantined row shares the retention of the queue |
| **B) A new `IQuarantineStore` on the 10 database providers (the original task)** | Separate lifecycle, retention and access control; never replayable by mistake | A new store, script and tests on all 10 providers (`AGENTS.md` section 5), about eleven pull requests |

### Chosen Option: **A — Quarantine inside the dead letter queue** (recommended, pending the maintainer)

### Rationale

- The queue already stores the error code, exception type, source and tenant (`IDeadLetterMessage.cs`), which is all a quarantine record needs; `ReplayAsync` already refuses by state (`dlq.already_replayed`), so refusing by code is one more branch.
- Who may produce the family (fail closed): only the poison layer writes `poison.quarantined.*`, taking the code from the `PoisonDecision`, never from a handler's `EncinaError`; the replay refusal tests the prefix on the stored row, so a handler that emits the prefix can only make its own row non-replayable (the refusal fails closed), never make a quarantined row replayable.
- B wins when quarantined messages need different access control or retention than dead letters (for example a security team that is not the operators).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Are <code>PauseProcessing</code> and <code>ResourceExhausted</code> in this issue</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Yes, with a consumer-control abstraction implemented by every transport** | Complete feature | The transport category (10 existing, 6 planned) must stay consistent (`AGENTS.md` section 5); catching `OutOfMemoryException` is not reliably recoverable |
| **B) No: `ResourceExhausted` maps to delayed retry plus a metric and an alert; pausing is its own issue** | Bounded scope; no transport change | A consumer under resource pressure keeps consuming |

### Chosen Option: **B — No pause action in this issue** (recommended, pending the maintainer)

### Rationale

- `PoisonAction` ships with `Retry`, `DelayedRetry`, `ImmediateDeadLetter` and `QuarantineAndAlert`; a `PauseProcessing` value would be a promise no transport keeps.
- A wins when the first applications need to stop a consumer on their single transport and no flow control of its own exists.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Where the payload check runs and what the same-exception rule reads</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A shared `PayloadGuard` called before `Deserialize` at the four sites; the same-exception rule reads `RecoverabilityContext.RetryHistory`** | The check runs where the exception would be thrown; no new storage for history | Four call sites change |
| **B) Only inside the recoverability pipeline** | One place | The pipeline sees an already deserialized request, so a malformed payload never reaches it |

### Chosen Option: **A — Shared guard at the deserialization sites**

### Rationale

- Option B cannot satisfy the issue: `RecoverabilityPipelineBehavior.Handle` receives `TRequest` (`RecoverabilityPipelineBehavior.cs:98`), so deserialization has already happened and a malformed payload never reaches it; the sites are the only place it is visible.
- The guard returns `Left(poison.malformed_payload)` and the site captures it with `DeadLetterSourceCapture` at once (ADR-046). The same-exception rule reads `RetryHistory` entries (count of equal code and exception type pairs), not `TotalAttempts`, so #2204 does not change it (stated test condition: the unit tests assert on the history content).

**Class:** Settled
**Settled by:** ADR-046 decision 13 (capture before terminal state) and AGENTS.md section 3 (errors are never swallowed in background infrastructure); the only other option fails the issue's malformed-payload criterion for the verified reason above

</details>

<details>
<summary><strong>5. Registration and gating: when the detector and the guard run</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Register the detector, the options validator and the metrics always (cheap singletons) and gate at run time on `PoisonMessage.Enabled`; the payload guard is called only when it is set** | Registration order cannot change the outcome (the configure callback sets `Enabled` after `AddEncinaRecoverability` returns); the application's own detector still wins in any order (`TryAdd`); with `Enabled = false` the behavior is today's path | A few idle singletons when the layer is off |
| **B) Register only when `Enabled` is set** | No idle services | Reads a value that is not final at registration time, so the result depends on registration order; the DI proof cannot cover enabled-set-after-registration |

### Chosen Option: **A — Always register, gate at run time**

### Rationale

- Option B fails registration completeness (AGENTS.md section 3, proven by a DI test that builds with `ValidateOnBuild` and `ValidateScopes` and sets `Enabled` after registration); the DI test covers enabled, disabled and enabled-set-after-registration.
- The payload guard is gated on `Enabled` at the three sites, so a disabled layer changes nothing and the opt-in criterion holds. Because the gate is a run-time read, enabling poison handling needs no re-registration.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (registration completeness; every messaging pattern is optional and disabled by default); the conditional-registration option is excluded because `Enabled` is not final at registration time

</details>

<details>
<summary><strong>6. Are quarantined rows hidden from listings by the manager</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No hiding in `DeadLetterManager` or `IDeadLetterStore`: quarantined rows appear with their `poison.quarantined.*` code and the #2229 admin API applies its own policy on top** | No store query change, no paging break, no provider-dependent code; Messaging has no authorization concept to key a "quarantine policy" on | A raw `GetMessagesAsync` caller sees quarantined rows |
| **B) Change the `IDeadLetterStore` queries (filter on the code family) on all 10 providers, with an include-quarantined parameter** | Hidden by default at the source, paging stays correct | A store interface change, script and tests on all 10 providers (AGENTS.md section 5); every implementer changes in the same pull request |

### Chosen Option: **A — No hiding in the manager; the admin API filters** (recommended, pending the maintainer)

### Rationale

- Filtering in the manager would break paging (a page can come back shorter than requested), and the only way to filter at the source is option B.
- B wins when raw store consumers must never see quarantined rows, or when #2229 is not built first.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. How a security violation is audited</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Through the port owned by #2013 (`IDeadLetterAuditSink`), with one more member, `RecordQuarantineAsync`, defined by the #2013 plan; the call ships after #2013's port and adapter** | `Encina.Messaging` cannot reference `Encina.Security.Audit` (the reference goes the other way); one audit route for every dead letter operation; durable only when the application registers a durable `IOperationAuditStore` | The audit call (S4) is blocked on #2013 |
| **B) Call `IOperationAuditStore` directly from `Encina.Messaging`** | No dependency on #2013 | Impossible without a project reference from `Encina.Messaging` to `Encina.Security.Audit`, a cycle; a second audit route |

### Chosen Option: **A — The port of #2013, a blocking prerequisite of the audit call** (recommended, pending the maintainer)

### Rationale

- The issue requires that a security violation is quarantined and audited; until the port exists the criterion is not met, so #2013 is a blocking prerequisite of S4's audit call, not a recommendation. The port member `RecordQuarantineAsync(DeadLetterQuarantineAuditInput, CancellationToken)` returns `ValueTask<Either<EncinaError, Unit>>`; its input record carries message id, source pattern, tenant, poison class and error code, never payload or message text. The #2013 plan lists the same member (#2013).
- Durability: the `InMemoryOperationAuditStore` default of `AddEncinaAudit` satisfies the port's adapter, so the audit is not durable in production until #2382 (open) rejects test-only stores outside Development; this plan states the gap and does not close it.
- B wins only if the project reference direction is reversed, which no plan proposes.

**Class:** Dilemma

</details>

### Register of defaults and semantics

The issue's own register (C1 to C10) is mirrored in full; the rows below each carry a class. Rows marked Dilemma are recommendations, not decisions.

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | Capture in the dead letter queue before the terminal state; a failed capture leaves the message un-terminated | Settled | ADR-046 decision 13; AGENTS.md section 3 |
| C2 | A `Left` from a store or a send fails the operation | Settled | AGENTS.md section 3 |
| C3 | No `Drop` action: silent discard loses data without a record | Settled | AGENTS.md section 3 (errors never swallowed); ADR-029 |
| C4 | Logs, tags and alerts carry the error code or exception type, never `EncinaError.Message`; exceptions through `ForLogging()` | Settled | AGENTS.md section 3 (#1168, #1173, #1259, #1274) |
| C5 | Opt-in, disabled by default | Settled | AGENTS.md sections 1 and 3 |
| C6 | Options read through the recoverability registration so two registrations cannot disagree | Settled | #3262, #3460 (closed), #3263 |
| C7 | `TenantId` from the request context on the quarantine record; per-tenant thresholds out of this issue (follow-up issue to be opened, draft: `artifacts/decisions/plan-batch/followups/poison-per-tenant-thresholds.md`) | Settled | AGENTS.md section 6 row 10; ADR-046 |
| C8 | A `SecurityViolation` is an operation audit entry and a metric; the alert channel is the application's (log, metric, health check) | Settled | ADR-036; AGENTS.md section 6 row 12; the route is Design Choice 7 |
| C9 | EventIds in a registered range, packed sequentially | Trivial | AGENTS.md section 7; ADR-021 |
| C10 | Rejected alternatives stay rejected: retry count only, manual handler exception handling, an external poison service | Settled | the issue text; ADR-029 |
| D1 | Default `Actions` per class: `MalformedPayload` and `ValidationFailure` and `Permanent` to `ImmediateDeadLetter`, `SecurityViolation` to `QuarantineAndAlert`, `ResourceExhausted` to `DelayedRetry`, `Transient` and `Unknown` to `Retry` (today's behavior) | Dilemma (recommended, pending the maintainer) | none: a default value; `ValidationFailure` and `ResourceExhausted` are the open ones |
| D2 | Defaults `SameExceptionThreshold = 3`, `DetectionWindow = 10 minutes`, payload size bound 1 MiB, health check degraded above 10 detections per window | Dilemma (recommended, pending the maintainer) | none: numeric defaults |
| D3 | The issue's `MaxRetries` is not added; retries stay `RecoverabilityOptions.ImmediateRetries` and `DelayedRetries` | Dilemma (recommended, pending the maintainer) | one retry vocabulary instead of two |
| D4 | The detector is synchronous (`PoisonDecision Detect(...)`, no I/O) instead of the issue's `ValueTask` with `CancellationToken`; `PoisonDecision` carries class, action and the code to record | Dilemma (recommended, pending the maintainer) | the signature and record are public API |
| D5 | The action is named `ImmediateDeadLetter`, not `ImmediateDLQ` | Dilemma (recommended, pending the maintainer) | public name |
| D6 | New codes and categories (ADR-062): `poison.malformed_payload` (`ErrorCategory.InvalidRequest`), `poison.detector_failed` (`Internal`), `dlq.replay_refused` (`Forbidden`), recorded family `poison.quarantined.<class>`, created with the category overload | Dilemma (recommended, pending the maintainer) | public codes |
| D7 | Metric names `encina.poison.detected_total` and `encina.poison.quarantined_total` (the existing `encina.dlq.*_total` and `encina.retry.*_total` pattern) instead of the issue's `encina.messaging.poison.*` | Dilemma (recommended, pending the maintainer) | public names |
| D8 | Options validation rejects `Retry` for `SecurityViolation` and a threshold of 0 or less | Settled | AGENTS.md section 3 (compliance and security gates fail closed): retrying a security violation keeps it running |
| D9 | A custom detector that throws or returns no decision is treated as `ImmediateDeadLetter` with `poison.detector_failed`, logged by code and exception type, never retried silently | Dilemma (recommended, pending the maintainer) | fail closed is satisfied by `ImmediateDeadLetter` and by `QuarantineAndAlert` |
| D10 | The payload guard runs the serializer-independent checks (non-empty, size bound) always and the JSON validity check only for the built-in JSON serializer | Dilemma (recommended, pending the maintainer) | scope of the guard |
| D11 | The guard is not applied at `DeadLetterManager.cs:360`: that row is already in the queue, and capturing it would re-capture a dead letter into itself; its existing `dlq.deserialization_failed` rejection stays | Settled | ADR-046 (capture is idempotent on `(SourcePattern, SourceMessageId)`); verified `DeadLetterManager.cs:360-364` |
| D12 | Behavior change under `Enabled`: a malformed payload is dead-lettered on its first failure at the outbox, delayed retry and scheduler sites | Settled | the issue's acceptance criterion (malformed payload reaches the DLQ on first failure); C1 |

---

## Implementation Phases

### Phase 1: Models, Contract and Mapping

> **Goal**: The poison vocabulary and the default mapping, without behavior change.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Messaging/Poison/PoisonClassification.cs` (`NotPoison`, `Transient`, `Permanent`, `MalformedPayload`, `ValidationFailure`, `SecurityViolation`, `ResourceExhausted`, `Unknown`) and `PoisonAction.cs` (`Retry`, `DelayedRetry`, `ImmediateDeadLetter`, `QuarantineAndAlert`).
2. **Create** `IPoisonMessageDetector` (`PoisonDecision Detect(EncinaError error, Exception? exception, ErrorClassification classification, RecoverabilityContext context)`; synchronous, no I/O, register rows D4 and D9) and `DefaultPoisonMessageDetector` (error code first, exception type second, as `DefaultErrorClassifier` does; `JsonException` to `MalformedPayload`, `System.ComponentModel.DataAnnotations.ValidationException` to `ValidationFailure`, `System.Security.SecurityException` to `SecurityViolation`, `OutOfMemoryException` to `ResourceExhausted`; a `RetryHistory` run of equal `(code, exception type)` pairs at or above `SameExceptionThreshold` inside `DetectionWindow`, read with the injected `TimeProvider`, escalates to `ImmediateDeadLetter`). The `PoisonDecision` record carries class, action and the code to record.
3. **Create** `PoisonMessageOptions` (`Enabled` default `false`, `SameExceptionThreshold`, `DetectionWindow`, `Actions` dictionary with the defaults of register row D1) and expose it as `RecoverabilityOptions.PoisonMessage`. Start-up validation (threshold above 0, no `Retry` for `SecurityViolation`) is Phase 3.
4. **Modify** `PublicAPI.Unshipped.txt` of `Encina.Messaging` (through `mechanical-fixer`).
5. Tests of this phase ship in its pull request (S1): unit, guard, contract and property tests, and the per-file targets in `.github/coverage-manifest/Encina.Messaging.json` (one sentence of justification per file).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no backward compatibility). Issue #135 adds an opt-in poison layer over the
existing IErrorClassifier (src/Encina/Recoverability/IErrorClassifier.cs, ADR-029). The maintainer's answers to
Design Choices 1-3 of docs/plans/poison-message-detection-implementation-plan-135.md decide the shape.

TASK:
1. Create the enums, IPoisonMessageDetector, DefaultPoisonMessageDetector and PoisonMessageOptions under
   src/Encina.Messaging/Poison/, wired as RecoverabilityOptions.PoisonMessage (Enabled = false).
2. The detector takes the ErrorClassification already computed, never calls the classifier a second time.
3. Validate the options at start-up.

KEY RULES:
- No Drop action, no [Obsolete]; Either<EncinaError, T> for fallible work; XML docs on every public member.
- Error code or exception type only in any text; never EncinaError.Message.

REFERENCE FILES:
- src/Encina.Messaging/Recoverability/RecoverabilityOptions.cs, RecoverabilityContext.cs
- src/Encina/Recoverability/IErrorClassifier.cs
```

</details>

---

### Phase 2: Pipeline Integration and Payload Guard

> **Goal**: The detector decides at the points that classify today; malformed payloads dead-letter on the first failure.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `RecoverabilityPipelineBehavior.cs` (pull request S2): add an optional trailing `IPoisonMessageDetector? poisonDetector = null` constructor parameter (the six existing test files that construct the behavior with `new` compile unchanged and need no edit: `RecoverabilityPipelineBehaviorContractTests`, `RecoverabilityGuardTests`, `DeadLetterSourcesEndToEndScenario`, `ErrorMessageLeakTests`, `RecoverabilityPipelineBehaviorTests`, `DelayedRetryChainTests`; verified with `git grep -l "new RecoverabilityPipelineBehavior<" -- tests`). In `HandleErrorResult` (`:407`) and `HandleException` (`:450`) ask the detector when `PoisonMessage.Enabled` and map `ImmediateDeadLetter` and `QuarantineAndAlert` to the existing permanent path (`PermanentFailure`, `HandlePermanentFailureAsync`); `DelayedRetry` goes to `TryScheduleFirstDelayedRetryAsync`. A throwing detector is handled per register row D9.
2. **Create** `Poison/PayloadGuard.cs` (pull request S3; checks of register row D10) and call it, only when `PoisonMessage.Enabled`, before `Deserialize` in `OutboxBatchProcessor.cs:182`, `DelayedRetryProcessor.cs:207` and `SchedulerOrchestrator.cs:450`; on failure capture through `DeadLetterSourceCapture` first, then mark terminal (ADR-046 decision 13). The replay site `DeadLetterManager.cs:360` is not guarded (register row D11).
3. **Modify** `DeadLetterManager` (S3) so a row whose `ErrorCode` starts with `poison.quarantined.` is not replayed (`dlq.replay_refused`); listings are not filtered (Design Choice 6 option A).
4. **Create** the quarantine record: `ErrorCode` `poison.quarantined.security_violation`, source from the failing pattern, tenant from the request context.
5. Tests ship with each pull request (S2, S3): the unit tests of the pipeline mapping, the guard and the four sites, with their per-file targets in the manifest.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Phase 1 added the vocabulary. RecoverabilityPipelineBehavior classifies at HandleErrorResult and HandleException
and routes permanent failures to HandlePermanentFailureAsync and DeadLetterSourceCapture.

TASK:
1. Call the detector after classification when PoisonMessage.Enabled; map actions to the existing paths.
2. Add PayloadGuard and call it, only when PoisonMessage.Enabled, before Deserialize at the three sites named in the plan
   (not the replay site).
3. Make DeadLetterManager refuse replay of quarantined rows (error code family, dlq.replay_refused); do not filter lists.

KEY RULES:
- Capture before terminal state; a failed capture leaves the message un-terminated (AGENTS.md section 3).
- A Left from a store fails the operation (retry or dead letter), never reports success.
- Disabled by default: with Enabled = false the behavior is byte-for-byte today's path.

REFERENCE FILES:
- src/Encina.Messaging/Recoverability/RecoverabilityPipelineBehavior.cs
- src/Encina.Messaging/DeadLetter/DeadLetterSourceCapture.cs, DeadLetterManager.cs
- src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs, Recoverability/DelayedRetryProcessor.cs, Scheduling/SchedulerOrchestrator.cs
```

</details>

---

### Phase 3: Configuration, DI and Registration

> **Goal**: One registration, proven by a DI test.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `RecoverabilityServiceCollectionExtensions.cs` (S2): always `TryAddSingleton<IPoisonMessageDetector, DefaultPoisonMessageDetector>()` and register the options validator (a new `PoisonMessageOptionsValidator`, rules of register row D8); `Enabled` is read at run time (Design Choice 5); an application detector wins in any order.
2. **Create** the DI test with `ValidateOnBuild` and `ValidateScopes` for enabled, disabled and enabled-set-after-registration.
3. **Modify** `RecoverabilityRegistrationCheck.cs` so quarantine without `IDeadLetterStore` fails at start-up with a message naming the module and the missing piece (ADR-050).
4. `PoisonMetrics` and the health check are registered in S5 (the pull request that creates them); S2, S3 and S4 emit no metric and create no log class.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Recoverability registration lives in RecoverabilityServiceCollectionExtensions.cs; start-up checks follow
DelayedRetryStartupCheck and RecoverabilityRegistrationCheck.

TASK:
1. Register the detector and options validator always with TryAdd semantics; read PoisonMessage.Enabled at run time.
2. Fail start-up when QuarantineAndAlert is configured and no IDeadLetterStore is registered.
3. Add DI tests built with ValidateOnBuild and ValidateScopes: enabled, disabled and enabled set after registration.

KEY RULES:
- Registration completeness (AGENTS.md section 3): every option type and dependency resolved is registered.
- The application's own registration wins in any order.

REFERENCE FILES:
- src/Encina.Messaging/Recoverability/RecoverabilityServiceCollectionExtensions.cs
- src/Encina.Messaging/Recoverability/RecoverabilityRegistrationCheck.cs
- docs/architecture/adr/050-modules-may-require-modules.md
```

</details>

---

### Phase 4: Cross-Cutting Integration and Observability

> **Goal**: Metrics, logs, audit and the health signal.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `Diagnostics/PoisonMetrics.cs` on a meter named `Encina.Messaging` (ADR-054 D3): `encina.poison.detected_total` (tags class, action, error code; the tenant tag only when multi-tenancy is on, ADR-054 D4) and `encina.poison.quarantined_total` (register row D7); register it in S5.
2. **Create** `Poison/PoisonLog.cs` with six `[LoggerMessage]` EventIds 6650-6655 (detection per class, quarantine, malformed payload dead-lettered, same-exception escalation, detector failure, audit failure), the first log class of this plan, so S5 is the pull request that creates it; register `MessagingPoison = (6650, 6659)` in `EventIdRanges.cs` first (inside the block 6650-6689 assigned to #135 and #2013; #2013 starts at 6660) and add `EventIdRanges.MessagingPoison` to the `Encina.Messaging` entry of `EncinaEventIdAllocationTests`. The log calls in the pipeline behavior are added in S5.
3. **Create** the audit write for `SecurityViolation` through the audit port that #2013 owns (`IDeadLetterAuditSink.RecordQuarantineAsync`, adapter `OperationAuditDeadLetterSink` in `Encina.Security.Audit`, Design Choice 7). #2013's port and adapter are a blocking prerequisite of this task; S5 ships the metrics, log and health check first, and the audit call is its own small pull request, S6, opened only after #2013's port and adapter are on `main`. A failed audit write fails the quarantine operation (retry or dead letter per its semantics) and is logged by code only.
4. **Create** `PoisonRateHealthCheck : IEncinaHealthCheck` (degraded above the configured rate in the window, register row D2; `DefaultName` const, static `Tags`); register it with `PoisonMetrics`.
5. **Add** the ADR (poison classification relative to ADR-029; number assigned when the ADR pull request opens).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Retry metrics live in src/Encina/Recoverability/RetryMetrics.cs (encina.retry.*_total); dead letter metrics in
src/Encina.Messaging/Diagnostics/DeadLetterMetrics.cs. EventIds must be registered first (ADR-021).

TASK:
1. Register MessagingPoison (6650-6659) in EventIdRanges.cs and the allocation test map, then write PoisonLog (6650-6655).
2. Add PoisonMetrics, PoisonRateHealthCheck and the audit call for SecurityViolation.
3. Keep the tenant tag conditional on multi-tenancy being on.

KEY RULES:
- Tags and logs carry the error code or exception type, never EncinaError.Message; exceptions via ForLogging().
- EventIds packed sequentially, XML docs name the range.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina.Messaging/DeadLetter/DeadLetterLog.cs
```

</details>

---

### Phase 5: Testing

> **Goal**: Every applicable flag reaches its manifest target for `Encina.Messaging`.

<details>
<summary><strong>Tasks</strong></summary>

Tests and their per-file manifest targets ship in the pull request that changes the code (split table in Research); only the integration tests and the justifications are their own pull request (S7).

1. **Unit tests** (S1 to S6): default mapping per class, action defaults, same-exception threshold and window with a fake `TimeProvider`, `PayloadGuard`, quarantine refusal in `DeadLetterManager`, disabled path unchanged, a throwing detector (D9).
2. **Guard tests** for every new public method with parameters (S1, S2, S3, S5).
3. **Contract tests** for `IPoisonMessageDetector` over the default and a custom implementation (S1).
4. **Property tests** (FsCheck): classification is total and deterministic for any error and history (S1).
5. **Integration tests** (S7) on the shared collection fixtures: pipeline to dead letter capture on the 10 providers (capture before terminal state, quarantined row refuses replay); no `.md` justification.
6. Load and benchmark (S7): justification files (a per-message switch, not concurrent). A contract-test justification is never allowed.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Per-flag coverage (AGENTS.md section 9) for Encina.Messaging; targets in .github/coverage-manifest/Encina.Messaging.json.

TASK:
1. Write unit, guard, contract and property tests that instantiate the real classes.
2. Write integration tests on the existing Collection fixtures for the recoverability-to-dead-letter path.
3. Measure per-flag coverage and CRAP with coverage-report.cs; add per-file targets with justifications.

KEY RULES:
- Shouldly through Encina.Testing.Shouldly; no IClassFixture for database fixtures.
- CRAP <= 10 on changed methods: keep the detector a small switch plus helpers.

REFERENCE FILES:
- tests/Encina.IntegrationTests (Collections.cs files), tests/Encina.ContractTests
- .github/scripts/coverage-report.cs
```

</details>

---

### Phase 6: Documentation and Finalization

> **Goal**: Users can enable and tune poison handling.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on every new public API; `PublicAPI.Unshipped.txt`.
2. `changelog.d/135-poison-message-detection.added.md`.
3. `Encina.Messaging` README section, recoverability configuration guide, `docs/INVENTORY.md`, the ADR of Phase 4.
4. `dotnet build --configuration Release` with zero warnings; `dotnet test` with every flag at target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Poison handling is implemented and tested. Documentation is written by the docs-writer agent following the
encina-docs skill; figures are cited with covref markers, never typed.

TASK:
1. Complete XML docs and PublicAPI files; write the changelog fragment, README and guide sections.

KEY RULES:
- English only; never edit CHANGELOG.md directly.

REFERENCE FILES:
- changelog.d/README.md, .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| ADR-029 | Error classification: transient, permanent, unknown | Design Choice 1 |
| ADR-046 | Dead letter capture before terminal state | Design Choices 4 and 5 |
| ADR-036 | Operation audit store | Audit of security violations |
| ADR-050 | Modules may require modules | Start-up check for quarantine |
| ADR-054 D3, D4 | Meter per package; tenant tag only with multi-tenancy | Metrics |
| Azure Service Bus dead-letter queues, NServiceBus recoverability | Poison handling by failure class | Comparable products for the action set |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IErrorClassifier`, `DefaultErrorClassifier` | `src/Encina/Recoverability/IErrorClassifier.cs` | First classification |
| `RecoverabilityPipelineBehavior`, `RecoverabilityContext` | `src/Encina.Messaging/Recoverability/` | Integration point and failure history |
| `DeadLetterSourceCapture`, `DeadLetterManager` | `src/Encina.Messaging/DeadLetter/` | Capture and replay refusal |
| `RetryMetrics`, `DeadLetterMetrics` | `src/Encina/Recoverability/`, `src/Encina.Messaging/Diagnostics/` | Metric patterns |
| `IOperationAuditStore` | `src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs` | Audit, through the port of #2013 (`AddEncinaAudit` registers `InMemoryOperationAuditStore` by default, `ServiceCollectionExtensions.cs:96`) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` poison | 6650-6659 (new `MessagingPoison`, inside the block 6650-6689 assigned to #135 and #2013) | 6650-6655 used (six messages); 6656-6659 free inside the range; #2013 starts at 6660 |

### Estimated File Count

Every count includes tests, coverage manifest edits, PublicAPI files and changelog. No pull request uses a type, project or log class created by a later one; each enters its per-file manifest targets with its own tests.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Phase 1: vocabulary, options, default detector | 6 | 2 (`RecoverabilityOptions.cs`, PublicAPI) | 5 (unit x2, guard, contract, property) | 0 | 1 | 0 | 14 | ~1,000 |
| S2 | Pipeline integration, registration, `PoisonErrorCodes`, options validator, start-up check | 2 | 4 (pipeline, registration, `RecoverabilityRegistrationCheck.cs`, PublicAPI) | 4 (validator, pipeline, DI, guard) | 0 (the six existing constructors compile unchanged) | 1 | 0 | 11 | ~900 |
| S3 | `PayloadGuard` at the three sites | 1 | 4 (three sites, PublicAPI) | 2 (`PayloadGuard` unit, guard) | 3 (the unit tests of the three sites) | 1 | 0 | 11 | ~750 |
| S4 | Replay refusal of quarantined rows in `DeadLetterManager` | 0 | 3 (manager, `DeadLetterErrorCodes.cs`, PublicAPI) | 1 | 1 | 1 | 0 | 6 | ~350 |
| S5 | Metrics, log class and EventIds, health check, registration of both | 3 | 5 (`EventIdRanges.cs`, registration, pipeline, PublicAPI of `Encina` and of `Encina.Messaging`) | 3 (metrics, health check, guard) | 2 (`EncinaEventIdAllocationTests`, the DI test of S2) | 1 | 0 | 14 | ~800 |
| S6 | Audit call for a security violation (after #2013) | 0 | 2 (pipeline, PublicAPI) | 1 | 1 | 1 | 0 | 5 | ~300 |
| S7 | Integration tests on the 10 providers; load and benchmark justifications | 0 | 0 | 12 (10 integration, 2 `.md` justifications) | 0 | 0 | 0 | 12 | ~1,000 |
| S8 | Documentation | 0 | 0 | 0 | 0 | 0 | 6 (`Encina.Messaging` README, recoverability guide, `docs/INVENTORY.md`, ADR, ADR index, `changelog.d/135-poison-message-detection.added.md`) | 6 | ~400 |
| **Total** | | **12** | **20** | **28** | **7** | **6** | **6** | **79** | **~5,500** |

The columns sum to the total (12 + 20 + 28 + 7 + 6 + 6 = 79) and every statement of the total in this plan is 79 files. The largest pull request is 14 files; none exceeds 1,000 lines. S6 waits for #2013; S1 to S5, S7 and S8 do not.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #135 - opt-in poison message detection over the existing recoverability pipeline.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Railway Oriented Programming: Either<EncinaError, T>; a Left from a store fails the operation.
- The detector never replaces IErrorClassifier (answer of Design Choice 1); quarantine lives in the dead letter
  queue (Design Choice 2); no pause action (Design Choice 3), per the maintainer's recorded answers.

IMPLEMENTATION OVERVIEW:
1. Poison/ models, detector, options (Enabled = false); 2. pipeline integration and PayloadGuard at three sites;
3. always-on registration with TryAdd, run-time gate and start-up check; 4. metrics, EventIds 6650-6659 (six used),
audit call through the port of #2013, health check; 5. tests per flag in the pull request of the code;
6. docs and changelog. Eight pull requests (S1 to S8), see the Estimated File Count table.

KEY PATTERNS:
- Capture before terminal state; no Drop; codes only; TimeProvider for the detection window.
- EventIds registered in EventIdRanges.cs first; DI test with ValidateOnBuild and ValidateScopes.

REFERENCE FILES:
- src/Encina.Messaging/Recoverability/RecoverabilityPipelineBehavior.cs
- src/Encina.Messaging/DeadLetter/DeadLetterSourceCapture.cs
- docs/plans/poison-message-detection-implementation-plan-135.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | A per-message decision on the failure history |
| 2 | OpenTelemetry | ✅ Phase 4 | `PoisonMetrics` on the `Encina.Messaging` meter; codes only |
| 3 | Structured Logging | ✅ Phase 4 | `PoisonLog` EventIds 6650-6655 in the new range 6650-6659 |
| 4 | Health Checks | ✅ Phase 4 | `PoisonRateHealthCheck : IEncinaHealthCheck` (the `Encina.Messaging.Health` abstraction, no new project reference) degrades above a configured rate; registered with `PoisonMetrics` in S5 |
| 5 | Validation | ✅ Phases 2-3 | Options validated at start-up (`PoisonMessageOptionsValidator`, S2); `PayloadGuard` before deserialization (S3) |
| 6 | Resilience | ✅ Phase 2 | Part of the recoverability decision: retry, delayed retry or dead letter |
| 7 | Distributed Locks | ❌ N/A | A per-message decision |
| 8 | Transactions | ✅ Phase 2 | Capture before terminal state through the existing sources |
| 9 | Idempotency | ❌ N/A | Dead letter capture is already idempotent on `(SourcePattern, SourceMessageId)` (ADR-046) |
| 10 | Multi-Tenancy | ✅ Phases 2 and 4 | Tenant stamped on the quarantine record; tenant metric tag when tenancy is on; per-tenant thresholds are not in this issue (follow-up issue to be opened, draft: `artifacts/decisions/plan-batch/followups/poison-per-tenant-thresholds.md`) |
| 11 | Module Isolation | ⏭️ #747 | Module-scoped poison handling follows the module isolation issue |
| 12 | Audit Trail | ✅ Phase 4 | Security violations audited through the port of #2013, a blocking prerequisite of S6 (Design Choice 7); durable only with a durable `IOperationAuditStore` (#2382 related) |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Recoverability pipeline (#39) | ✅ Closed | Integration point exists |
| Persistent dead letter queue (#583) | ✅ Closed | Quarantine and capture |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Dead letter admin API | #2229 | Applies its own policy to quarantined rows (Design Choice 6 option A) |
| `TotalAttempts` off by one | #2204 | Not a prerequisite: the same-exception rule counts `RetryHistory` entries; the unit tests assert on the history content (stated test condition) |
| Startup validator rejecting test-only stores outside Development | #2382 | Closes the in-memory audit store gap of Design Choice 7; related, not blocking |

### Blocking for S6 only

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Audit port `IDeadLetterAuditSink` with `RecordQuarantineAsync` and the `OperationAuditDeadLetterSink` adapter | #2013 | The #2013 plan lists the same member (#2013); S6 opens only after both are on `main`. Without it the criterion "a security violation is quarantined and audited" is not met |

---

## Next Steps

1. Link this plan from #135 and ask the maintainer to answer Design Choices 1, 2, 3, 6 and 7 and the Dilemma rows D1 to D10 of the register when work starts.
2. Open the follow-up issues when the first pull request opens: per-tenant thresholds (draft: `artifacts/decisions/plan-batch/followups/poison-per-tenant-thresholds.md`) and the `PauseProcessing` consumer-control abstraction of Design Choice 3 option B (draft: `artifacts/decisions/plan-batch/followups/transport-consumer-pause.md`).
3. Implement S1 to S8 as the pull requests of the Estimated File Count split, `Fixes #135` on S8.
