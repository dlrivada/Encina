# Implementation Plan: Audit Trail for Outbox, Inbox and Scheduled Message Processing

> **Issue**: [#749](https://github.com/dlrivada/Encina/issues/749)
> **Type**: Feature (compliance and operations; background processing is outside the request pipeline)
> **Complexity**: Medium (a port in `Encina.Messaging`, one adapter in `Encina.Security.Audit`, three hook points; no schema and no provider code; 7 pull requests of 5 to 9 files)
> **Estimated Scope**: ~420 lines of production code + ~1,100 lines of tests, documentation and manifests; 47 file touches in 7 pull requests
> **Family**: scheduling-3 (the scheduled-message processor documents #749 as its audit item). Siblings are cited by issue number only: #751 (the precedent of reusing `IOperationAuditStore` with `FailClosed` and an explicit `BestEffort` opt-out), #737, #739 and #760 (tenant on the three rows), #1164 (envelope and restored context), #720 (commit owner of the saga family), #152 (telemetry of the scheduler)

---

## Summary

### What exists today (read on 2026-11-25, `origin/main` `dfa55c93e`)

- The issue's premise holds for the outbox and the scheduled messages, and holds in part for the inbox. `AuditPipelineBehavior<,>` (`src/Encina.Security.Audit/AuditPipelineBehavior.cs`) audits commands and queries inside `IEncina.Send`. The outbox is published by `OutboxProcessorBase` (a `BackgroundService`, one subclass per provider: 8 `OutboxProcessor` classes) through `OutboxBatchProcessor.ProcessAsync`, and scheduled messages are dispatched by `ScheduledMessageProcessor` through `SchedulerOrchestrator.ProcessDueMessagesAsync`; neither passes through a pipeline, so neither is audited. The inbox runs **inside** the pipeline (`InboxPipelineBehavior` calls `InboxOrchestrator.ProcessAsync`), so the request is audited by the request audit, but the idempotency decisions (a duplicate answered from the cache, a message rejected for a missing id or for exhausted retries) are not.
- The issue says to emit "via `IAuditStore`". That type does not exist. The operation audit abstraction is `IOperationAuditStore` (`src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs`, `RecordAsync(OperationAuditEntry, ct)` returning `ValueTask<Either<EncinaError, Unit>>`), with `OperationAuditEntry` (`Id`, `CorrelationId`, `UserId`, `TenantId`, `Action`, `EntityType`, `EntityId`, `Outcome`, `ErrorMessage`, `TimestampUtc`, `StartedAtUtc`, `CompletedAtUtc`, payload fields, `Metadata`) and `AuditOutcome` (`Success`, `Failure`, `Denied`, `Error`). `AGENTS.md` section 6 names it as the audit integration point. It is implemented by the 10 database providers, Marten and an in-memory default (`AddEncinaAudit` registers `InMemoryOperationAuditStore` with `TryAdd`).
- **`Encina.Messaging` cannot reference `Encina.Security.Audit`**: `Encina.Security.Audit.csproj` has a `ProjectReference` to `Encina.Messaging` (read in the csproj), so the reverse reference is a cycle. The issue's "emit via the audit store from the processors" therefore needs a port owned by `Encina.Messaging` (Design Choice 1).
- The outcomes are decided in three places: `OutboxBatchProcessor.ProcessMessageAsync` (`src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs:166`; success after `MarkAsProcessedAsync`, failure through `FailAsync`, `ScheduleRetryAsync` and `ExhaustAsync`), `InboxOrchestrator` (`HandleExistingMessageAsync` returns the cached response, or `inbox.max_retries_exceeded`; `ProcessAndCacheResponseAsync` runs the handler; `ValidateMessageId` rejects a missing id), and `SchedulerOrchestrator.DispatchMessageAsync` and `MarkAsFailedAsync`. The orchestrators never call `SaveChangesAsync`.
- `OutboxBatchProcessor` is created in two places (`OutboxProcessorBase.ProcessPendingMessagesAsync` and `OutboxOrchestrator`) and by two tests; `new InboxOrchestrator(` has 15 sites in 5 files; `new SchedulerOrchestrator(` has 28 sites in 8 files. Every new constructor parameter below is optional and last, so none of those sites changes.
- Tenant on the rows: none today (#737, #739 and #760 add it); the inbox carries the request's tenant in `InboxMetadata.TenantId`.
- EventIds: `MessagingOutbox` 2000-2099 uses 2000-2006 (#737 claims 2007-2009); `MessagingInbox` 2100-2199 uses 2100-2112 (#760 claims 2113-2115); `MessagingScheduling` 2300-2399 (#739 claims 2390-2392, #159 claims 2307-2309).

### What the issue proposes and what changes

The issue proposes four events, `IAuditStore` and `config.AuditMessaging = true`. The plan keeps the four, adds the scheduled-message equivalent (the processor already names #749 for it; Design Choice 3), replaces `IAuditStore` by a port plus an adapter over `IOperationAuditStore`, and defines failure semantics, which the issue does not.

### What this plan delivers

- In `Encina.Messaging`: the port `IMessageProcessingAuditRecorder`, the record `MessageProcessingAuditEvent`, `MessagingAuditOptions` and `MessagingConfiguration.AuditMessaging` (default `false`), the internal `MessageAuditor` that applies the options, a startup validator, and hooks at the three outcome points.
- In `Encina.Security.Audit`: `OperationAuditMessageRecorder`, the adapter that maps an event to an `OperationAuditEntry` and writes it to `IOperationAuditStore`, registered by `AddEncinaAudit`.
- Events: `OutboxMessagePublished`, `OutboxMessageFailed`, `InboxMessageProcessed`, `InboxMessageFailed`, `InboxMessageRejected` (reasons `duplicate`, `max_retries_exceeded`, `invalid_message_id`), `ScheduledMessageExecuted`, `ScheduledMessageFailed`. Each carries message id, request or notification type name, timestamps, duration, outcome and the error **code** (never `EncinaError.Message`, never the payload).
- Shared pieces and owner: this plan owns the port, the event record and the three hooks; #751 owns the precedent; #737, #739 and #760 own the tenant columns (the entries carry the row's tenant once those land: Design Choice 6); #1164 owns the restored user and correlation (the entries carry them when the envelope provides them).
- Affected packages: `Encina.Messaging`, `Encina.Security.Audit`. Provider category: none directly (the audit store is the existing multi-provider abstraction); the hooks sit above the 10 database providers' stores, so no provider code changes and the integration test uses one real provider per run.
- Not delivered, with the reason: audit of the saga processor (#720 owns the saga commit owner and its own audit); a new audit table (the existing store is reused); read audit (`IReadAuditStore`, ADR-036).

---

## Design Choices

<details>
<summary><strong>1. Where the port lives (Dilemma)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A port `IMessageProcessingAuditRecorder` in `Encina.Messaging`, an adapter `OperationAuditMessageRecorder` in `Encina.Security.Audit` (which already references Messaging), registered by `AddEncinaAudit`** | Respects the project reference direction; the hooks see duration and outcome where they are decided; the audit store stays the one abstraction | A public port type and one adapter |
| **B) Decorators of `IOutboxStore`, `IInboxStore` and `IScheduledMessageStore` in `Encina.Security.Audit`, recording on `MarkAsProcessedAsync` and `MarkAsFailedAsync`** | No change to Messaging orchestrators | No duration, no duplicate or rejection (those never reach the store as marks), no type name at the store; three decorators and the inbox still unaudited |
| **C) Move the audit abstractions (`IOperationAuditStore`, `OperationAuditEntry`) to core** | Messaging can call the store directly | A large move of public API across packages and a PublicAPI churn unrelated to this issue |

### Chosen Option: **A — a port in Messaging and an adapter in Security.Audit** (recommended, pending the maintainer)

### Rationale

- A wins because the cycle rules out the direct call and B cannot see what the issue asks for (duration, rejection, failure detail); C wins only if the maintainer wants the audit abstraction in core for other reasons.

</details>

<details>
<summary><strong>2. Failure semantics of a failed audit write</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (errors are never swallowed in background infrastructure: a `Left` from a store inside a processor fails that operation; compliance gates fail closed and the only opt-out is explicit and logged), `OperationAuditOptions.FailureMode` (`FailClosed` by default, `BestEffort` explicit) and the same default in the plan of #751.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MessagingAuditFailureMode.FailClosed` is the default: a `Left` from the recorder fails the operation (the outbox message or scheduled message goes through its retry and dead-letter path with the code `messaging.audit_failed`; the inbox returns the `Left`); `BestEffort` is the explicit opt-out, logged once per drop with the error code** | No unaudited processing by accident; same default as the request audit | A failing audit store stalls processing until it recovers |
| **B) Best effort by default** | Processing never blocks on audit | A silent audit gap in a compliance control: contradicts the fail-closed rule |

### Chosen Option: **A — fail closed by default, `BestEffort` as the explicit logged opt-out**

### Rationale

- Only A complies. `messaging.audit_failed` is a new error code with ADR-062 category `Internal` (an infrastructure failure of the audit store, the category the inbox uses for `inbox.max_retries_exceeded`).

</details>

<details>
<summary><strong>3. Order of the audit write and the outcome mark (Dilemma)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Audit first, then mark: the outbox records `OutboxMessagePublished` after the publish and before `MarkAsProcessedAsync`; a failed audit write leaves the message unmarked, so it is published again (at-least-once, the outbox contract)** | A marked message always has its audit record; fail-closed is real | A duplicate publish after an audit outage; consumers need the idempotency the outbox already requires |
| **B) Mark first, then audit** | No duplicate publish | An audit failure after the mark is lost or must be reported without failing anything: the gap fail-closed forbids |

### Chosen Option: **A — audit before the mark** (recommended, pending the maintainer)

### Rationale

- A wins when the audit record is the compliance evidence; B wins if duplicate publication is costlier than a rare gap (then `BestEffort` is the way to ask for it). The inbox writes its record after the handler result is cached and before returning; the scheduler writes it after the dispatch and before `MarkAsProcessedAsync` or the reschedule. Because the orchestrators never commit, no claim of atomicity between the audit entry and the mark is made.

</details>

<details>
<summary><strong>4. Which events: scheduled messages too (Dilemma)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Outbox, inbox and scheduled messages: the recorder is shared and the scheduler hook is one more call site; this closes the "audit trail" item that `ScheduledMessageProcessor` documents against #749** | One coherent feature and one opt-in; the third hook costs about 40 lines | The issue's acceptance list names only outbox and inbox |
| **B) Outbox and inbox only, as the issue states** | Smaller | Leaves the processor's own documented gap and a second issue for the same port |

### Chosen Option: **A — all three** (recommended, pending the maintainer)

### Rationale

- A wins on cohesion (the maintainer rule: a follow-up that does not deserve its own plan goes inside the plan); B wins if scope must stay at the issue's list. Pull request 6 carries the scheduler hook alone, so B means skipping it.

</details>

<details>
<summary><strong>5. What an entry carries and where it is stored</strong></summary>

**Class:** Trivial
**Settled by:** `AGENTS.md` section 6 row 12 (`IOperationAuditStore` is the audit integration point), section 3 (`EncinaError.Message` never reaches plaintext storage), and the mapping precedent of the plan of #751.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `OperationAuditEntry` with `Action` = the event name, `EntityType` = the notification or request type name (`Type.FullName`, no assembly qualifier), `EntityId` = the message id, `Outcome` `Success` or `Failure`, `ErrorMessage` = the error code only, `StartedAtUtc` and `CompletedAtUtc` from the hook's `TimeProvider`, `CorrelationId` = the inbox correlation id or the message id, `Metadata` = source, retry count, reason; no payload fields** | Reuses every store, the retention service, masking and the health check; no new table | `ErrorMessage` holds a code, which the XML docs state |
| **B) A new messaging audit store and table on 10 providers** | A tailored schema | Duplicates a mature abstraction; 10 providers of work for no new capability |

### Chosen Option: **A — reuse `IOperationAuditStore`**

### Rationale

- The worker verifies that the audit retention and masking options (`OperationAuditOptions`) apply unchanged. No payload, no content and no exception text is recorded.

</details>

<details>
<summary><strong>6. Tenant and user on the entries</strong></summary>

**Class:** Settled
**Settled by:** SPEC-002 AC-043 and AC-044 (tenant-scoped data and a tenant dimension when tenancy is on) and ADR-054 D4; the rows' tenant comes from #737, #739 and #760.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The entry's `TenantId` is the row's tenant (the outbox, scheduled and inbox row columns, or `InboxMetadata.TenantId` for the inbox today); `UserId` is the restored user when #1164 provides it, else null; never read from the ambient context of a background processor** | The audited tenant is the tenant of the message; no wrong tenant from a background thread | Null until the columns land |
| **B) Ambient tenant of the processor scope** | Available today | A background scope has no request tenant: a wrong or empty tenant on a compliance record |

### Chosen Option: **A — the row's tenant**

### Rationale

- Only A is correct for a background processor. The code reads the tenant through each row interface after #737, #739 and #760 merge (Phase 3 to 5 name the member); before that the entry's tenant is null and the documentation says so.

</details>

<details>
<summary><strong>7. Opt-in surface and startup check</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (every messaging pattern is optional and disabled by default; a registration that needs a dependency fails visibly) and the startup-check precedent `OperationAuditMaskingStartupValidator`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MessagingConfiguration.AuditMessaging` (default `false`) and `MessagingConfiguration.MessagingAuditOptions` (failure mode), like `SchedulingOptions`; when enabled, a hosted startup validator throws `InvalidOperationException` if no `IMessageProcessingAuditRecorder` is registered (`AddEncinaAudit` was not called)** | Fails at startup, not at the first message; matches the issue's `config.AuditMessaging = true` | A hosted service for a check |
| **B) Enable auditing when a recorder is registered** | No flag | Auditing switches on by registering a package: not opt-in |

### Chosen Option: **A — flag plus startup validator**

### Rationale

- Only A is explicit opt-in and fails closed when the dependency is missing; the registration is proven by a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes` for both the enabled and disabled settings.

</details>

---

## Implementation Phases

### Phase 1: Port, event record and options (PR 1)

> **Goal**: The public types and the opt-in, with no behavior change.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Messaging/Audit/IMessageProcessingAuditRecorder.cs` (public interface: `ValueTask<Either<EncinaError, Unit>> RecordAsync(MessageProcessingAuditEvent auditEvent, CancellationToken cancellationToken = default)`).
2. **Create** `src/Encina.Messaging/Audit/MessageProcessingAuditEvent.cs` (public sealed record with its enums `MessageAuditSource` (`Outbox`, `Inbox`, `Scheduling`), `MessageAuditKind` (the seven events) and `MessageAuditOutcome`; properties `MessageId` (string), `MessageType`, `StartedAtUtc`, `CompletedAtUtc`, `Outcome`, `ErrorCode` (string?, code only), `RetryCount`, `Reason`, `CorrelationId`, `TenantId`, `UserId`; XML docs state that `ErrorCode` is never `EncinaError.Message`).
3. **Create** `src/Encina.Messaging/Audit/MessagingAuditOptions.cs` (public sealed class: `FailureMode` of the enum `MessagingAuditFailureMode` { `FailClosed` = 0, `BestEffort` }); **modify** `MessagingConfiguration.cs`: `AuditMessaging` (false) and `MessagingAuditOptions`.
4. Tests (new): unit tests of the record and options defaults; guard test for any constructor with a validated parameter (the record uses `required` members, so the guard test covers `MessagingAuditOptions` only if it validates); `PublicAPI.Unshipped.txt` and the manifest through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Issue #749, plan docs/plans/messaging-processing-audit-implementation-plan-749.md. Dilemmas 1, 3 and 4 are recommended, pending the maintainer.
- Encina.Messaging cannot reference Encina.Security.Audit (the reverse reference exists): the port lives in Messaging.

TASK:
Create the port, the event record, the options and the two configuration members.

KEY RULES:
- Public API gets XML docs and PublicAPI.Unshipped.txt lines; the event carries an error code, never EncinaError.Message and never a payload.
- Opt-in: AuditMessaging defaults to false. Pre-1.0: no compatibility layer.

REFERENCE FILES:
- src/Encina.Messaging/MessagingConfiguration.cs
- src/Encina.Security.Audit/OperationAuditOptions.cs
- src/Encina.Security.Audit/OperationAuditFailureMode.cs
```

</details>

### Phase 2: The auditor, the startup check, logging and registration (PR 2)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Messaging/Audit/MessageAuditor.cs` (`internal sealed class`, constructor `(IMessageProcessingAuditRecorder recorder, MessagingAuditOptions options, ILogger<MessageAuditor> logger)`): `ValueTask<Either<EncinaError, Unit>> RecordAsync(MessageProcessingAuditEvent e, CancellationToken ct)`. A `Left` from the recorder returns `Left(messaging.audit_failed, ErrorCategory.Internal)` under `FailClosed`; under `BestEffort` it logs the drop (EventIds below, error code only through the recorder's code) and returns `Right`. An exception from the recorder is converted to the same `Left` (the recorder contract is `Either`, a throw is a defect handled at this boundary and logged by `ForLogging()`).
2. **Create** `src/Encina.Messaging/Audit/MessagingAuditStartupValidator.cs` (`IHostedService`; `StartAsync` throws `InvalidOperationException` when `AuditMessaging` is on and no recorder is registered) and **modify** `MessagingServiceCollectionExtensions.cs`: when `config.AuditMessaging`, register `MessagingAuditOptions` (singleton), `MessageAuditor` (scoped; the recorder may be scoped) and the validator; nothing is registered when it is off.
3. **Create** `src/Encina.Messaging/Diagnostics/MessagingAuditLog.cs` (`internal static partial class`, `[LoggerMessage]`): outbox 2010 `OutboxAuditDropped` (Warning, code) and 2011 `OutboxAuditRecordFailed` (Warning, code); inbox 2116 and 2117; scheduling 2393 and 2394; all inside registered ranges (2010-2099, 2116-2199, 2393-2399), each range packed from the first free id after the sibling claims; registry rows and the claims table of the batch registry updated in this pull request.
4. Tests: `MessageAuditorTests` (`FailClosed` returns the code, `BestEffort` returns `Right` and logs, a throwing recorder is a `Left`, no log or result contains an error message), `MessagingAuditRegistrationTests` (provider built with `ValidateOnBuild` and `ValidateScopes` for enabled and disabled; enabled with no recorder fails at start), guard tests, manifest through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Phase 1 merged. The auditor is internal; Encina.Messaging grants InternalsVisibleTo to the unit, guard, contract and property test projects.

TASK:
Create MessageAuditor, the startup validator, MessagingAuditLog and the conditional registration.

KEY RULES:
- Default FailClosed; BestEffort is explicit and every drop is logged with the error code only.
- A recorder that throws is a defect: convert to a Left at this boundary, log through ForLogging().
- Registration completeness: provider builds with ValidateOnBuild and ValidateScopes; a missing recorder fails at startup.

REFERENCE FILES:
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina.Security.Audit/OperationAuditMaskingStartupValidator.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 3: The adapter in Encina.Security.Audit (PR 3)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Security.Audit/OperationAuditMessageRecorder.cs` (public sealed class implementing `IMessageProcessingAuditRecorder`; constructor `(IOperationAuditStore store, TimeProvider? timeProvider = null)`; maps the event as in Design Choice 5 and calls `RecordAsync`; the entry `Id` is a new `Guid`, `TimestampUtc` is the event's completion time).
2. **Modify** `src/Encina.Security.Audit/ServiceCollectionExtensions.cs` (`AddEncinaAudit`): `TryAddScoped<IMessageProcessingAuditRecorder, OperationAuditMessageRecorder>()` (scoped, because database stores are scoped; the default in-memory store is a singleton and is resolved fine).
3. Tests: unit (every event kind maps `Action`, `EntityType`, `Outcome`, code-only `ErrorMessage` and empty payload fields; a store `Left` is returned unchanged), guard (null store, null event), DI test for `AddEncinaAudit`, `PublicAPI.Unshipped.txt` and the manifest through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- The port exists in Encina.Messaging; Encina.Security.Audit already references Encina.Messaging.

TASK:
Create the adapter and register it in AddEncinaAudit.

KEY RULES:
- No payload, no content, no EncinaError.Message in the entry; the code goes in ErrorMessage.
- Time from TimeProvider; the recorder returns the store's Left unchanged.

REFERENCE FILES:
- src/Encina.Security.Audit/DefaultOperationAuditEntryFactory.cs
- src/Encina.Security.Audit/ServiceCollectionExtensions.cs
- src/Encina.Security.Audit/OperationAuditEntry.cs
```

</details>

### Phase 4: Outbox hook (PR 4)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs`: a trailing optional constructor parameter `MessageAuditor? auditor = null` after `deadLetterCapture` (4 `new OutboxBatchProcessor(` sites: 2 source, 2 tests; none changes). In `ProcessMessageAsync` time the publish with `_timeProvider`; after a successful publish call `auditor.RecordAsync(OutboxMessagePublished)` **before** `MarkAsProcessedAsync` (Design Choice 3); a `Left` becomes `FailAsync` with the code `messaging.audit_failed`; the failure outcomes (`FailAsync`, retry, exhaustion) record `OutboxMessageFailed` with the error code and retry count (a `Left` of that record is logged with 2011 and does not mask the original failure).
2. `src/Encina.Messaging/Outbox/OutboxProcessorBase.cs` (`ProcessPendingMessagesAsync`): `services.GetService<MessageAuditor>()` passed to the batch processor (null when auditing is off), and the same in `OutboxOrchestrator`'s creation of the batch processor.
3. The tenant of the entry is read from the row after #737 (`IOutboxMessage.TenantId`); the code compiles without it (null).
4. Tests: `OutboxBatchProcessorAuditTests` (published, failed, exhausted, cancellation records nothing, an audit `Left` under `FailClosed` leaves the message pending with the code, under `BestEffort` marks it processed and logs 2010), a property test (every processed message yields exactly one `Published` or one `Failed` event), manifest through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- MessageAuditor exists (Phase 2); the adapter exists (Phase 3). OutboxBatchProcessor is internal.

TASK:
Record the outbox outcomes through MessageAuditor in OutboxBatchProcessor and pass the auditor from the base processor.

KEY RULES:
- Audit before MarkAsProcessedAsync; a cancellation is not a failure and records nothing.
- Only error codes in events; the optional parameter is last.
- Never swallow a Left: FailClosed fails the message through the existing retry and dead-letter path.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs
- src/Encina.Messaging/Outbox/OutboxProcessorBase.cs
- tests/Encina.UnitTests/Messaging/Outbox/OutboxRetryAfterCeilingTests.cs
```

</details>

### Phase 5: Inbox and scheduler hooks (PRs 5 and 6)

<details>
<summary><strong>Tasks</strong></summary>

1. **PR 5**, `InboxOrchestrator` (`src/Encina.Messaging/Inbox/InboxOrchestrator.cs`): trailing optional `MessageAuditor? auditor = null` after `deadLetterCapture` (15 `new InboxOrchestrator(` sites in 5 files; none changes). Record `InboxMessageProcessed` after the response is cached in `ProcessAndCacheResponseAsync`, `InboxMessageFailed` for a failed handler attempt, `InboxMessageRejected` with reason `duplicate` in the cached-response branch, `max_retries_exceeded` in the retry-exhausted branch and `invalid_message_id` for a missing id. The `CorrelationId` and `TenantId` come from the existing `InboxAttempt` and `InboxMetadata`. A `Left` from the recorder under `FailClosed` is returned in place of the response (the handler's effect already happened: documented; Design Choice 3 note).
2. **PR 6**, `SchedulerOrchestrator`: trailing optional `MessageAuditor? auditor = null` after the last optional parameter that exists when the work starts (28 sites in 8 files; optional last, so none changes; the parameters other plans append (#739, #152) are also optional and last, the later merger appends after the earlier). Record `ScheduledMessageExecuted` after the dispatch and before the mark or reschedule, `ScheduledMessageFailed` in `MarkAsFailedAsync` (with whether it was dead-lettered in `Reason`).
3. Tests per pull request: orchestrator audit tests (each kind, `FailClosed` and `BestEffort`, no content or error text in any event), guard edit for the new optional parameter (null accepted and valid), manifests through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- MessageAuditor is in place; the outbox hook merged. The inbox and scheduler orchestrators construct their dependencies through DI with optional parameters.

TASK:
Add the inbox hook (PR 5) and the scheduler hook (PR 6) as in the plan tasks.

KEY RULES:
- Orchestrators never call SaveChangesAsync and the plan makes no atomicity claim about the audit entry and the mark.
- Optional parameter last; tenant and correlation from the row or attempt, never from an ambient background context.
- Error code only; no payload.

REFERENCE FILES:
- src/Encina.Messaging/Inbox/InboxOrchestrator.cs
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
- tests/Encina.UnitTests/Messaging/Inbox/InboxOrchestratorTests.cs
```

</details>

### Phase 6: Cross-cutting integration, end-to-end test and documentation (PR 7)

<details>
<summary><strong>Tasks</strong></summary>

1. Integration test (`tests/Encina.IntegrationTests/Messaging/MessagingAuditScenario.cs` plus one family class on the EF Core collection and the in-memory store): an outbox message published and one that fails to exhaustion, an inbox duplicate and a max-retries rejection, a scheduled message executed, each yields the expected `OperationAuditEntry` (`GetByEntityAsync`), and a failing audit store under `FailClosed` leaves the outbox message pending. Load and benchmark: `.md` justifications (one extra write per processed message; not a concurrent hot path of its own).
2. Documentation through `docs-writer` (the messaging audit section of the audit guide: opt-in, failure modes, the duplicate-publish consequence of fail-closed, what an entry holds) and `changelog.d/749-messaging-processing-audit.added.md` through `mechanical-fixer`; the XML docs of `ScheduledMessageProcessor` drop the "deferred" note for #749.
3. Build verification (`dotnet build --configuration Release`, zero warnings) and the tests of every flag.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- All hooks merged. Documentation pages go through docs-writer; fragments and PublicAPI lines through mechanical-fixer.

TASK:
Write the end-to-end scenario, the documentation and the fragment; remove the deferred note in the processor docs.

KEY RULES:
- Shared [Collection] fixtures only; ClearAllDataAsync in InitializeAsync.
- Never type coverage figures by hand in documentation; no private client application is named.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Scheduling/SchedulerCycleScenarios.cs
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

### Phase 7: Per-file coverage targets (entered in the pull request that carries the tests)

<details>
<summary><strong>Tasks</strong></summary>

| File | PR | Flag and target | Justification |
| --- | --- | --- | --- |
| `Audit/MessageAuditor.cs` | 2 | unit 95, guard 90 | Every mode and failure path runs against a substitute recorder; constructor guards are reachable through `InternalsVisibleTo`. |
| `Audit/MessagingAuditStartupValidator.cs` | 2 | unit 95 | Started with and without a recorder. |
| `Audit/MessageProcessingAuditEvent.cs`, `Audit/MessagingAuditOptions.cs`, `Audit/IMessageProcessingAuditRecorder.cs` | 1 | unit 0 | Property bags and an interface: no executable branch. |
| `Diagnostics/MessagingAuditLog.cs` | 2 | unit 0 | `[LoggerMessage]` declarations; generated bodies are not in the file. |
| `OperationAuditMessageRecorder.cs` | 3 | unit 95, guard 90 | One mapping per kind and the guards. |
| `Outbox/OutboxBatchProcessor.cs` | 4 | unit 99, guard 15, integration 80 (current, not lowered) | The new branches are unit tests; the scenario adds integration. |
| `Inbox/InboxOrchestrator.cs` | 5 | unit 98, guard 15, integration 75 (current, not lowered) | One test per kind and mode. |
| `Scheduling/SchedulerOrchestrator.cs` | 6 | unit 90, guard 30 (current, not lowered) | Executed and failed paths with a mocked store. |

Measure with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests` and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`. CRAP: the hook in each changed method is one call to a private helper so the method complexity does not grow past 10; the worker runs the local crap-gate table.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
- Targets live in .github/coverage-manifest/{Package}.json and never go down.

TASK:
Enter the per-file targets above with one-sentence justifications in the pull request that carries the tests.

KEY RULES:
- Read the manifest and write numbers; tests execute real code.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Messaging.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | Key Details |
|----------|-------------|-------------|
| `AGENTS.md` section 3 | Errors never swallowed in background infrastructure; gates fail closed; no `EncinaError.Message` in storage | Design Choices 2 and 5 |
| `AGENTS.md` section 6 row 12 | `IOperationAuditStore` is the audit point | Adapter |
| ADR-036 | Operation, entity-change and read audit stores | Reuse of the operation store |
| ADR-062 | `ErrorCategory` per code | `messaging.audit_failed` is `Internal` |
| SPEC-002 AC-043, AC-044 | Tenant on records and telemetry when tenancy is on | Design Choice 6 |
| ADR-054 D4 | No raw tenant id outside the audit record itself | Not in logs or tags |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IOperationAuditStore`, `OperationAuditEntry`, `AuditOutcome` | `src/Encina.Security.Audit/` | The sink |
| `OperationAuditMaskingStartupValidator` | `src/Encina.Security.Audit/` | Startup-check precedent |
| `OutboxBatchProcessor` | `src/Encina.Messaging/Outbox/` | Outbox outcome point |
| `InboxOrchestrator` | `src/Encina.Messaging/Inbox/` | Inbox outcome points |
| `SchedulerOrchestrator` | `src/Encina.Messaging/Scheduling/` | Scheduler outcome points |
| `InMemoryOperationAuditStore` | `src/Encina.Security.Audit/` | Default store in tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (`MessagingOutbox`, existing 2000-2099) | **2010-2011** | Used 2000-2006; #737 claims 2007-2009; packed after it |
| `Encina.Messaging` (`MessagingInbox`, existing 2100-2199) | **2116-2117** | Used 2100-2112; #760 claims 2113-2115 |
| `Encina.Messaging` (`MessagingScheduling`, existing 2300-2399) | **2393-2394** | #159 claims 2307-2309 and #739 2390-2392 |
| New range | none | No block is needed; the three ranges map to `Encina.Messaging` already |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production | 16 | 7 new (port, event record, options, auditor, validator, log, adapter) and 9 modified (configuration, registration, `AddEncinaAudit`, outbox batch processor, outbox base, outbox orchestrator, inbox orchestrator, scheduler orchestrator, processor docs note counted in the scheduler PR) |
| Tests | 18 | Unit, guard, DI, property and integration files across the pull requests |
| Coverage manifests | 7 | One per pull request touching a package |
| PublicAPI files | 3 | `Encina.Messaging` (PR 1 and 2) and `Encina.Security.Audit` (PR 3) |
| Documentation and changelog | 3 | Guide section, processor note, fragment |
| **Total touches** | **47** | In 7 pull requests of 8, 9, 7, 6, 5, 5 and 7 files |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no compatibility layers; time from TimeProvider.
- Issue #749: audit trail for background processing. Encina.Messaging cannot reference Encina.Security.Audit (cycle): a port in Messaging, an adapter in Security.Audit.

IMPLEMENTATION OVERVIEW:
PR1 port, event record, options, configuration; PR2 MessageAuditor, startup validator, log, registration; PR3 adapter and AddEncinaAudit; PR4 outbox hook; PR5 inbox hook; PR6 scheduler hook; PR7 end-to-end scenario and docs.

KEY PATTERNS:
- FailClosed by default, BestEffort explicit and logged; audit before the mark; error code only, no payload.
- Optional constructor parameters last; the row's tenant, never the ambient one.
- Startup validator fails when auditing is on and no recorder is registered.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs
- src/Encina.Messaging/Inbox/InboxOrchestrator.cs
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
- src/Encina.Security.Audit/ServiceCollectionExtensions.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Audit writes are not cacheable |
| 2 | OpenTelemetry | ⏭️ | Counters of audit failures belong with the observability plans (#152 for scheduling); logs carry the drops now |
| 3 | Structured Logging | ✅ | Phase 2: events 2010-2011, 2116-2117, 2393-2394, error codes only |
| 4 | Health Checks | ❌ | `OperationAuditStoreHealthCheck` already checks the audit store |
| 5 | Validation | ✅ | Startup validator rejects an enabled audit with no recorder |
| 6 | Resilience | ❌ | The audit store is a local abstraction; retry is the processor's own |
| 7 | Distributed Locks | ❌ | No shared state added |
| 8 | Transactions | ❌ | The orchestrators never commit; no atomicity claim between entry and mark |
| 9 | Idempotency | ✅ | The inbox rejections (`duplicate`) are recorded; an outbox republish after a fail-closed audit failure is documented |
| 10 | Multi-Tenancy | ✅ | Design Choice 6: the row's tenant once #737, #739 and #760 land |
| 11 | Module Isolation | ⏭️ | A `ModuleId` on entries follows #747's columns |
| 12 | Audit Trail | ✅ | This is the feature |

---

## Prerequisites & Dependencies

| Item | Relation | Note |
|------|----------|------|
| #751 | Precedent | Reuse of `IOperationAuditStore`, `FailClosed` default |
| #737, #739, #760 | Soft | Tenant on the rows; entries carry null before |
| #1164 | Soft | Restored user and correlation for the outbox and scheduler entries |
| #152, #159 | Siblings | Scheduler telemetry and analytics; independent |
| #720 | Sibling | Saga commit owner; saga audit is its own |

---

## Next Steps

1. Review this plan; the Dilemmas (choices 1, 3 and 4: where the port lives, audit-before-mark ordering and whether scheduled messages are included) are answered by the maintainer when work starts; the answers are then recorded in a `## Maintainer Decisions` section added at that time.
2. Implement pull requests 1 to 7 in order.
3. Link the plan from #749.
