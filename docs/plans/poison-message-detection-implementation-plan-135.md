# Implementation Plan: Enhanced Poison Message Detection and Handling

> **Issue**: [#135](https://github.com/dlrivada/Encina/issues/135)
> **Type**: Feature
> **Related**: #39 (recoverability, ADR-029), #583 (persistent dead letter queue, closed), #2013 (dead letter replay under the stored context), #2229 (dead letter admin API), #2204 (`TotalAttempts` counts one too many), #2205 (failing dead letter store reruns exhausted messages), ADR-029, ADR-036, ADR-046, ADR-050, ADR-054
> **Complexity**: Medium (one package plus core, one optional provider-dependent store behind Dilemma 2)
> **Estimated Scope**: ~900-1,300 lines of production code + ~1,800 lines of tests if Dilemma 2 is option A; the 10-provider store of option B is a separate series

---

## Summary

### What exists today (read on 2026-11-11, main `68d08ddae`)

- `IErrorClassifier.Classify(EncinaError, Exception?)` returns `ErrorClassification` `Unknown`, `Transient` or `Permanent` (`src/Encina/Recoverability/IErrorClassifier.cs`); `DefaultErrorClassifier` checks a declared transience, then exception types and error codes, then the message text. `RecoverabilityOptions.ErrorClassifier` can replace it (`RecoverabilityOptions.cs:104`).
- `RecoverabilityPipelineBehavior` classifies in `HandleErrorResult` (`:407`) and `HandleException` (`:450`); a `Permanent` result stops the immediate retries and `HandlePermanentFailureAsync` (`:530`) builds the `FailedMessage`, captures it with `DeadLetterSourceCapture.CaptureFailedMessageAsync` and calls `OnPermanentFailure`. `RecoverabilityContext` keeps `RetryHistory`, `LastClassification` and `TotalAttempts` (`RecoverabilityContext.cs:55-105`).
- Payloads are deserialized in four background sites: `OutboxBatchProcessor.cs:182`, `DelayedRetryProcessor.cs:207`, `SchedulerOrchestrator.cs:450` and `DeadLetterManager.cs:360` (replay). None checks structure first; a malformed payload is just another exception.
- The dead letter queue is persistent (`IDeadLetterStore`, ADR-046) with sources `Recoverability`, `Outbox`, `Inbox`, `Scheduling`, `Saga` and `Choreography` (`DeadLetterSourcePatterns.cs`).
- Not in `src`: `IPoisonMessageDetector`, `PoisonClassification`, `PoisonAction`, `IQuarantineStore`, `IExceptionClassifier`, `MessageContext`. The issue's sketch `PoisonClassification.Transient/Permanent/Unknown` duplicates `ErrorClassification`.

### What this plan delivers

An opt-in poison layer in `Encina.Messaging`: finer classes (malformed payload, validation failure, security violation, resource exhausted) mapped from the existing classification, a configurable action per class, a cheap payload check before deserialization at the four sites, quarantine of security violations, metrics, a health signal and an audit entry. Nothing changes unless `RecoverabilityOptions.PoisonMessage.Enabled` is set (default `false`).

- Affected packages: `Encina` (only if Dilemma 1 is option A), `Encina.Messaging`, `Encina.Security.Audit` (adapter, reuses #2013's port), and the 10 provider packages only if Dilemma 2 is option B.
- Provider category: none for the detector; Database (10) only under Dilemma 2 option B (`AGENTS.md` section 5).

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
| **A) In the dead letter queue, with a reserved `ErrorCode` family (`poison.quarantined.*`) that `DeadLetterManager` refuses to replay or list without the quarantine policy** | No new store, schema or tests on the 10 providers; tenant scoping, retention and the #2229 admin API are reused | The replay surface must refuse by code; a quarantined row shares the retention of the queue |
| **B) A new `IQuarantineStore` on the 10 database providers (the original task)** | Separate lifecycle, retention and access control; never replayable by mistake | A new store, script and tests on all 10 providers (`AGENTS.md` section 5), about eleven pull requests |

### Chosen Option: **A — Quarantine inside the dead letter queue** (recommended, pending the maintainer)

### Rationale

- The queue already stores the error code, exception type, source and tenant (`IDeadLetterMessage.cs`), which is all a quarantine record needs; `ReplayAsync` already refuses by state (`dlq.already_replayed`), so refusing by code is one more branch.
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

- The pipeline behavior receives `TRequest`, so deserialization has already happened; the sites are the only place a malformed payload is visible.
- The guard returns `Left(poison.malformed_payload)` and the site captures it with `DeadLetterSourceCapture` at once (ADR-046).

**Class:** Trivial
**Settled by:** AGENTS.md section 3 (errors are never swallowed in background infrastructure) and ADR-046 (capture through `DeadLetterSourceCapture`)

</details>

<details>
<summary><strong>5. Conventions applied without a choice</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Apply the repository rules: capture before terminal state, no `Drop` action, codes only in logs and tags, opt-in, options through the recoverability registration, tenant stamped, EventIds in a registered range** | No data is lost silently; no personal data in telemetry; consistent with the other patterns | None found |
| **B) Keep the issue's `Drop` action and per-class actions without a record** | Smaller dead letter table | Discarding a message without a record loses data silently |

### Chosen Option: **A — Repository rules**

### Rationale

- A failed capture leaves the message un-terminated (ADR-046 decision 13). Silent discard is rejected. A security violation is recorded as an operation audit entry and a metric; the alert channel is the application's (log, metric, health check), not a new notification system.
- The error text never leaves the process: `EncinaError.Message` is not logged, tagged or alerted; exceptions pass through `ForLogging()`.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (errors never swallowed, `EncinaError.Message` rule, opt-in patterns), AGENTS.md section 7 and ADR-021 (EventIds), ADR-036 (operation audit), ADR-046

</details>

---

## Implementation Phases

### Phase 1: Models, Contract and Mapping

> **Goal**: The poison vocabulary and the default mapping, without behavior change.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Messaging/Poison/PoisonClassification.cs` (`NotPoison`, `Transient`, `Permanent`, `MalformedPayload`, `ValidationFailure`, `SecurityViolation`, `ResourceExhausted`, `Unknown`) and `PoisonAction.cs` (`Retry`, `DelayedRetry`, `ImmediateDeadLetter`, `QuarantineAndAlert`).
2. **Create** `IPoisonMessageDetector` (`PoisonDecision Detect(EncinaError error, Exception? exception, ErrorClassification classification, RecoverabilityContext context)`; synchronous, no I/O) and `DefaultPoisonMessageDetector` (error code first, exception type second, as `DefaultErrorClassifier` does; `JsonException` to `MalformedPayload`, `System.ComponentModel.DataAnnotations.ValidationException` to `ValidationFailure`, `System.Security.SecurityException` to `SecurityViolation`, `OutOfMemoryException` to `ResourceExhausted`). The `PoisonDecision` record carries class, action and the code to record.
3. **Create** `PoisonMessageOptions` (`Enabled` default `false`, `SameExceptionThreshold`, `DetectionWindow`, `Actions` dictionary with defaults) and expose it as `RecoverabilityOptions.PoisonMessage`; validate at start-up (threshold above 0, no `Retry` for `SecurityViolation`).
4. **Modify** `PublicAPI.Unshipped.txt` of `Encina.Messaging` (through `mechanical-fixer`).

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

1. **Modify** `RecoverabilityPipelineBehavior.cs`: in `HandleErrorResult` (`:407`) and `HandleException` (`:450`) ask the detector (when enabled) and map `ImmediateDeadLetter` and `QuarantineAndAlert` to the existing permanent path (`PermanentFailure`, `HandlePermanentFailureAsync`); `DelayedRetry` goes to `TryScheduleFirstDelayedRetryAsync`; same-exception detection counts equal `(code, exception type)` pairs in `RetryHistory` inside `DetectionWindow`.
2. **Create** `Poison/PayloadGuard.cs` (structural check: non-empty, valid JSON for the JSON serializer, size bound) and call it before `Deserialize` in `OutboxBatchProcessor.cs:182`, `DelayedRetryProcessor.cs:207`, `SchedulerOrchestrator.cs:450` and `DeadLetterManager.cs:360`; on failure capture through `DeadLetterSourceCapture` first, then mark terminal (ADR-046 decision 13).
3. **Modify** `DeadLetterManager` so a row whose `ErrorCode` starts with `poison.quarantined.` is not replayed (`dlq.replay_refused` code) and is excluded from listings unless the caller holds the quarantine policy (Design Choice 2 option A).
4. **Create** the quarantine record: `ErrorCode` `poison.quarantined.security_violation`, source from the failing pattern, tenant from the request context.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Phase 1 added the vocabulary. RecoverabilityPipelineBehavior classifies at HandleErrorResult and HandleException
and routes permanent failures to HandlePermanentFailureAsync and DeadLetterSourceCapture.

TASK:
1. Call the detector after classification when PoisonMessage.Enabled; map actions to the existing paths.
2. Add PayloadGuard and call it before Deserialize at the four sites named in the plan.
3. Make DeadLetterManager refuse replay of quarantined rows (error code family) and hide them from default lists.

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

1. **Modify** `RecoverabilityServiceCollectionExtensions.cs`: `TryAddSingleton<IPoisonMessageDetector, DefaultPoisonMessageDetector>()` and the options validator, only when `PoisonMessage.Enabled`; an application detector wins in any order.
2. **Create** the DI test with `ValidateOnBuild` and `ValidateScopes` for enabled and disabled.
3. **Modify** `RecoverabilityRegistrationCheck.cs` so quarantine without `IDeadLetterStore` fails at start-up with a message naming the module and the missing piece (ADR-050).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Recoverability registration lives in RecoverabilityServiceCollectionExtensions.cs; start-up checks follow
DelayedRetryStartupCheck and RecoverabilityRegistrationCheck.

TASK:
1. Register the detector and options validator with TryAdd semantics, only when PoisonMessage.Enabled.
2. Fail start-up when QuarantineAndAlert is configured and no IDeadLetterStore is registered.
3. Add DI tests built with ValidateOnBuild and ValidateScopes, enabled and disabled.

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

1. **Create** `Diagnostics/PoisonMetrics.cs` on a meter named `Encina.Messaging` (ADR-054 D3): `encina.poison.detected_total` (tags class, action, error code; the tenant tag only when multi-tenancy is on, ADR-054 D4) and `encina.poison.quarantined_total`.
2. **Create** `Poison/PoisonLog.cs` with `[LoggerMessage]` EventIds 6650-6659 (register `MessagingPoison = (6650, 6669)` in `EventIdRanges.cs` first and add `EventIdRanges.MessagingPoison` to the `Encina.Messaging` entry of `EncinaEventIdAllocationTests`).
3. **Create** the audit write for `SecurityViolation` through the audit port that #2013 owns (`IDeadLetterAuditSink`, adapter in `Encina.Security.Audit`); until #2013 lands, this plan's task is the call site and the metric only.
4. **Create** `PoisonRateHealthCheck : IEncinaHealthCheck` (degraded above a configured rate in the window; `DefaultName` const, static `Tags`).
5. **Add** the ADR (poison classification relative to ADR-029; number assigned when the ADR pull request opens).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Retry metrics live in src/Encina/Recoverability/RetryMetrics.cs (encina.retry.*_total); dead letter metrics in
src/Encina.Messaging/Diagnostics/DeadLetterMetrics.cs. EventIds must be registered first (ADR-021).

TASK:
1. Register MessagingPoison (6650-6669) in EventIdRanges.cs and the allocation test map, then write PoisonLog.
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

1. **Unit tests**: default mapping per class, action defaults, same-exception threshold and window with a fake `TimeProvider`, `PayloadGuard`, quarantine refusal in `DeadLetterManager`, disabled path unchanged.
2. **Guard tests** for every new public method with parameters.
3. **Contract tests** for `IPoisonMessageDetector` over the default and a custom implementation.
4. **Property tests** (FsCheck): classification is total and deterministic for any error and history.
5. **Integration tests** on the shared collection fixtures: pipeline to dead letter capture on the 10 providers (capture before terminal state, quarantined row refuses replay); no `.md` justification.
6. Load and benchmark: justification files (a per-message switch, not concurrent).

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
| `IOperationAuditStore` | `src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs` | Audit, through the port of #2013 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` poison | 6650-6669 (new `MessagingPoison`, block assigned to this family) | 6650-6659 used by this plan; 6660-6669 reserved |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New production files | ~9 | Enums, detector, options, guard, metrics, log, health check |
| Changed production files | ~10 | Pipeline, four sites, manager, registration, `EventIdRanges.cs`, PublicAPI |
| Tests | ~14 | Unit, guard, contract, property, integration, DI, justification files |
| Documentation | ~6 | README, guide, ADR, INVENTORY, changelog |
| **Total** | **~39** | Split: S1 phases 1 and 3, S2 phase 2, S3 phase 4, S4 tests, S5 docs |

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
1. Poison/ models, detector, options (Enabled = false); 2. pipeline integration and PayloadGuard at four sites;
3. registration with TryAdd and start-up check; 4. metrics, EventIds 6650-6669, audit call, health check;
5. tests per flag; 6. docs and changelog.

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
| 3 | Structured Logging | ✅ Phase 4 | `PoisonLog` EventIds 6650-6659 in the new range |
| 4 | Health Checks | ✅ Phase 4 | `PoisonRateHealthCheck` degrades above a configured rate |
| 5 | Validation | ✅ Phases 1-2 | Options validated at start-up; `PayloadGuard` before deserialization |
| 6 | Resilience | ✅ Phase 2 | Part of the recoverability decision: retry, delayed retry or dead letter |
| 7 | Distributed Locks | ❌ N/A | A per-message decision |
| 8 | Transactions | ✅ Phase 2 | Capture before terminal state through the existing sources |
| 9 | Idempotency | ❌ N/A | Dead letter capture is already idempotent on `(SourcePattern, SourceMessageId)` (ADR-046) |
| 10 | Multi-Tenancy | ✅ Phases 2 and 4 | Tenant stamped on the quarantine record; tenant metric tag when tenancy is on; per-tenant thresholds are not in this issue (follow-up issue file proposed by the plan author) |
| 11 | Module Isolation | ⏭️ #747 | Module-scoped poison handling follows the module isolation issue |
| 12 | Audit Trail | ✅ Phase 4 | Security violations audited through the port of #2013 |

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
| Audit port for dead letter operations | #2013 | Owns `IDeadLetterAuditSink`; this plan only calls it |
| Dead letter admin API | #2229 | Lists quarantined rows only under its own policy |
| `TotalAttempts` off by one | #2204 | Affects the same-exception count; fix first or test around it |

---

## Next Steps

1. Link this plan from #135 and ask the maintainer to answer Design Choices 1 to 3 when work starts.
2. Open the follow-up issue for per-tenant thresholds when Phase 4 starts.
3. Implement Phases 1 to 6 as the pull requests of the Estimated File Count split, `Fixes #135` on the last.
