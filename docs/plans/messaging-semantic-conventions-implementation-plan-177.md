# Implementation Plan: Messaging Semantic-Convention Attributes and the One Constants Class

> **Issue**: [#177](https://github.com/dlrivada/Encina/issues/177)
> **Type**: Feature (observability core)
> **Related**: ADR-054 (D3 names and semantic conventions, D5 scope), #725 (the 10 transports emit destination, partition and client attributes), #175 (adds the correlation and causation keys to the class of this plan), #1790 (dead and duplicate activity sources), #2385 (the outbox transport publisher), ADR-062
> **Complexity**: Low (constants, two enricher additions and two span tags; the design work is deciding which attributes belong to the stores and which to the transports)
> **Estimated Scope**: ~140 lines of production code + ~330 lines of tests, in two pull requests of at most 15 files and 900 lines each

---

## Summary

### What exists today (read on 2026-11-18, main `f484cdf4f`)

- `MessagingActivityEnricher` (`src/Encina.OpenTelemetry/Enrichers/MessagingActivityEnricher.cs`, public static) has `EnrichWithOutboxMessage`, `EnrichWithInboxMessage`, `EnrichWithSagaState` and `EnrichWithScheduledMessage`. It sets `messaging.system`, `messaging.operation.name`, `messaging.message.id`, `messaging.message.type` and the Encina-specific `messaging.message.processed`, `.processed_at`, `.retry_count`, `.error`, and for scheduled messages `.scheduled_at`, `.is_recurring`, `.cron_expression`. It returns at once when the activity is `null`.
- The keys live in `Encina.OpenTelemetry.ActivityTagNames` (`src/Encina.OpenTelemetry/ActivityTagNames.cs`: nested `Messaging`, `Systems`, `Operations`); the enricher is the only consumer in `src`. A second, `internal` class of the same name exists in core (`src/Encina/Diagnostics/ActivityTagNames.cs`) for request, sharding and exception keys. `Encina.Messaging` has no access to the OpenTelemetry package, so the two live spans of the stores use string literals: `outbox.notification_count` on `encina.outbox.enqueue` (`OutboxActivitySource.StartEnqueue`, called from `TransactionalOutboxPipelineBehavior.cs:173`) and `scheduling.batch_size` on `encina.scheduling.process_due` (`SchedulingActivitySource.StartProcessDue`/`StartProcessingCycle`, called from `ScheduledMessageProcessor.cs:147`; the cycle is completed with the real count at `:164`). The Inbox, Saga and `OutboxActivitySource` batch spans have no caller (issue #1790).
- Not in `src`: `messaging.destination.name`, `messaging.message.body.size`, `messaging.batch.message_count`, `messaging.client.id`, `messaging.destination.partition.id`; a `DestinationName` property on any message interface. `IOutboxMessage` has `Content` (the issue's text says `Payload`, which does not exist), `NotificationType`, `IsProcessed`-style state and no destination; `IScheduledMessage` has `Content` and `RequestType`; `IInboxMessage` stores no payload at all (only `Response`).
- ADR-054 D3: attributes follow the latest OpenTelemetry messaging semantic conventions, every key lives in one constants class, and `OTEL_SEMCONV_STABILITY_OPT_IN` support follows when messaging is declared stable. ADR-054 D5 puts transport instrumentation, including these attributes, on the 10 transports (#725 and #177).

### What this plan delivers

One public constants class reachable from every package that emits messaging attributes (core), the semantic-convention attributes that the stores can really know, and the batch count on the live batch spans:

- `MessagingSemanticConventions` in core with every messaging key (moved from `Encina.OpenTelemetry.ActivityTagNames`, extended with the five missing semantic-convention keys and `messaging.message.conversation_id`).
- `messaging.message.body.size` on the outbox and scheduled-message enrichers (UTF-8 byte count of `Content`).
- `messaging.batch.message_count` on the outbox enqueue span and the scheduling cycle span.
- Destination name, partition id and client id are set by the transports, not by message rows (Design Choice 2): #725 emits them with the constants of this plan.

- Shared pieces and owner: this plan owns `MessagingSemanticConventions`; #175 adds `encina.messaging.causation_id` to it; #725 and #132 use it; #186 and #176 add no key.
- Affected packages: `Encina` (core), `Encina.OpenTelemetry`, `Encina.Messaging`.
- Provider category: Messaging transport (10 existing, 6 planned, `AGENTS.md` section 5), through #725; the stores of the 10 database providers are untouched (no schema change).
- Deviations from the issue, each in a Design Choice: no `DestinationName` property on the three interfaces (2), no public `EnrichWithBatch` (3).

---

## Design Choices

<details>
<summary><strong>1. Where the one constants class lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `public static class MessagingSemanticConventions` in core, in namespace `Encina.Diagnostics`** | Reachable from `Encina.Messaging`, the 10 transports and `Encina.OpenTelemetry` without a new reference; one class as ADR-054 D3 requires | Public API in core; the existing public `ActivityTagNames.Messaging` members are removed (pre-1.0, no compatibility layer) |
| **B) Keep `Encina.OpenTelemetry.ActivityTagNames.Messaging`** | No move | `Encina.Messaging` and the transports cannot reach it without referencing the OpenTelemetry package (a dependency inversion); the stores keep literals, so there are two sources of truth |
| **C) The core `ActivityTagNames`, made reachable by `InternalsVisibleTo`** | No new public type | Needs an `InternalsVisibleTo` entry for each of the transports and `Encina.Messaging`; the 6 planned transports add six more |

### Chosen Option: **A — A public class in core** (recommended, pending the maintainer)

### Rationale

- A wins when the transports (10 now, 6 planned) must emit the same keys without referencing the OpenTelemetry package, which ADR-054 settled point 1 keeps out of libraries; B wins if the maintainer wants the constants to ship only with the OpenTelemetry package and accepts literals in the stores; C wins only for a small, closed set of consumers.
- The class is organised in three groups (semantic-convention keys, their value sets, Encina-specific keys); a unit test pins every key string so a rename is a visible change.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where `messaging.destination.name`, partition id and client id come from</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `DestinationName` property on `IOutboxMessage`, `IInboxMessage` and `IScheduledMessage` (the issue's text)** | The stores know the name | 3 interfaces, 31 implementers in `src` (24 database message classes, 7 in `Encina.Testing.Bogus`, `Encina.Testing.Fakes` and `Encina.Testing.FsCheck`) and a column on 10 providers; and no row has a destination today: the outbox dispatches notifications to handlers through `IEncina.Publish`, the transport publisher is the open issue #2385 |
| **B) The transport sets the attributes where it publishes or consumes (it knows the queue, topic, partition and client id), using the constants of Design Choice 1** | No schema change; the value is the real one; the same place that owns the span (#725) | The enrichers of the stores do not show a destination until a transport is in the path |
| **C) A destination derived from `NotificationType`** | No schema change | A type name is not a destination; it would emit a wrong attribute |

### Chosen Option: **B — Transports set destination, partition and client id**

### Rationale

- ADR-054 D5 assigns "transport instrumentation on all 10 transports" to #725 and #177, and D2 is defined on the consumer span the transport starts; the attributes are properties of that span.
- A scenario where A wins: an application that routes outbox rows to a destination chosen at write time. No such flow exists on `main`; when #2385 delivers the transport publisher it records the destination there.

**Class:** Settled
**Settled by:** ADR-054 D5 and the interfaces on `main` (`src/Encina.Messaging/Outbox/IOutboxMessage.cs`, `Inbox/IInboxMessage.cs`, `Scheduling/IScheduledMessage.cs`: no destination, no transport in the path); option C emits a wrong value and option A adds a column that nothing fills

</details>

<details>
<summary><strong>3. How the batch count is emitted</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A public `EnrichWithBatch(Activity?, int)` on `MessagingActivityEnricher` (the issue's text)** | Matches the issue | No caller in `src`: the live batch spans are created in `Encina.Messaging`, which cannot reference `Encina.OpenTelemetry`; a public method with no caller fails "every line serves a current purpose" |
| **B) The batch spans in `Encina.Messaging` set `messaging.batch.message_count` themselves, with the constant of Design Choice 1** | Tag set where the span and the real count are; no new public API | The enricher has no batch method |

### Chosen Option: **B — Set it on the spans that exist** (recommended, pending the maintainer)

### Rationale

- B wins on `main` today: `SchedulingActivitySource.CompleteProcessingCycle(activity, count)` already receives the real count and the outbox enqueue span knows `notifications.Count`; `scheduling.batch_size` carries the configured capacity, not the count (`ScheduledMessageProcessor.cs:147`).
- A wins if a future application-level processor outside `Encina.Messaging` needs to tag its own batch spans through the enricher; that method is then added with its caller.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Body size source and cost</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encoding.UTF8.GetByteCount(message.Content)` for outbox and scheduled messages, computed only after the existing `activity is null` early return; none for inbox and saga** | Exact byte size of the stored payload; no allocation; zero cost without a listener | O(n) per tagged span |
| **B) `Content.Length` (characters)** | O(1) | Not bytes, so not the semantic-convention meaning |
| **C) Add a size column to the rows** | O(1) read | A schema change on 10 providers for a diagnostic value |

### Chosen Option: **A — UTF-8 byte count after the null check**

### Rationale

- The semantic convention defines the attribute as the size of the message body in bytes. The enricher already returns before any work when there is no activity, so the cost is paid only by a sampled span.
- `IInboxMessage` stores no payload (only `Response`), so no inbox body size is emitted rather than a wrong one; the transport that receives the message knows the wire size (#725).

**Class:** Trivial
**Settled by:** the early return of `MessagingActivityEnricher.cs` (`activity is null`) and the OpenTelemetry messaging semantic conventions definition of `messaging.message.body.size`

</details>

<details>
<summary><strong>5. Which convention version, and the fate of the Encina-specific keys</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pin the latest names behind the one class (ADR-054 D3); keep the Encina-specific keys (`messaging.message.processed`, `.retry_count`, `.error`, `.scheduled_at`, `.is_recurring`, `.cron_expression`) with their current strings in an "Encina-specific" group** | No behavior change for the existing tags; one place to rename | The `messaging.message.*` prefix for custom keys is the semantic-convention namespace; renaming them is a separate breaking change |
| **B) Rename the custom keys to `encina.messaging.*` in this plan** | Cleaner namespaces | Changes the strings of every existing span, outside the scope of this issue |

### Chosen Option: **A — Pin and group; rename in its own change**

### Rationale

- ADR-054 D3: "Attributes follow the latest OpenTelemetry messaging semantic conventions. Every attribute key lives in one constants class." The grouping satisfies it; the rename is recorded in the follow-up issue file `messaging-custom-attribute-namespace`.

**Class:** Settled
**Settled by:** ADR-054 D3; option B is a separate decision about existing public tag strings and is not required by D3

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | No new `EncinaError` code, no logging: enrichment is silent and never fails, so there is no ADR-062 `ErrorCategory` and no EventId | Settled | ADR-062 (a category is chosen where an `EncinaError` is created); the enricher's existing no-throw, no-log contract |
| C2 | `EncinaError.Message` and the stored `ErrorMessage` never reach a tag; the existing `TagFailed` helper (a boolean only) is kept | Settled | `AGENTS.md` section 3 and the remarks of `MessagingActivityEnricher.cs` |
| C3 | No tenant tag is added here: ADR-054 D4's tenant attribute is emitted by `TenancyActivityEnricher` when multi-tenancy is on; there is no tenant-qualified id and the reserved `default` segment does not apply | Settled | ADR-054 D4; `src/Encina.OpenTelemetry/Enrichers/TenancyActivityEnricher.cs` |
| C4 | The moved constants keep their string values, so no existing span changes; `ActivityTagNames.Messaging`, `Systems` and `Operations` are removed from the public API of `Encina.OpenTelemetry` | Settled | `AGENTS.md` section 1 (pre-1.0, no compatibility layers, breaking changes done completely) |
| C5 | `messaging.system` keeps the values `outbox`, `inbox`, `scheduling` for now; the semantic convention defines the value as the broker, so these values deviate: recorded in the follow-up issue file `messaging-system-attribute-values` | Dilemma (recommended, pending the maintainer) | the alternative values (`encina`, or the transport name) are the decision of that issue; recorded here so the constants class does not freeze them silently |
| C6 | Public name and namespace: `MessagingSemanticConventions` in `Encina.Diagnostics` | Dilemma (recommended, pending the maintainer) | public surface; alternatives are `SemanticConventions.Messaging` as a nested type or the namespace `Encina` |

---

## Implementation Phases

### Phase 1: Constants Class and Enricher Attributes (pull request S1)

> **Goal**: One public class holds every messaging attribute key; the enrichers add body size.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Diagnostics/MessagingSemanticConventions.cs`: public static class with the semantic-convention keys (`System = "messaging.system"`, `OperationName`, `MessageId`, `MessageType`, `BodySize = "messaging.message.body.size"`, `BatchMessageCount = "messaging.batch.message_count"`, `DestinationName = "messaging.destination.name"`, `DestinationPartitionId = "messaging.destination.partition.id"`, `ClientId = "messaging.client.id"`, `ConversationId = "messaging.message.conversation_id"`), the value sets moved from `ActivityTagNames.Systems` and `Operations`, and the Encina-specific group moved from `ActivityTagNames.Messaging` (`Processed`, `ProcessedAt`, `RetryCount`, `Error`, `ScheduledAt`, `IsRecurring`, `CronExpression`). XML docs name the semantic-convention stability.
2. **Modify** `src/Encina.OpenTelemetry/ActivityTagNames.cs` (remove nested `Messaging`, `Systems`, `Operations`) and `src/Encina.OpenTelemetry/Enrichers/MessagingActivityEnricher.cs` (use the new class; add `TagBodySize(activity, content)` after the null checks of `EnrichWithOutboxMessage` and `EnrichWithScheduledMessage`).
3. **Modify** `src/Encina/PublicAPI.Unshipped.txt` (new class and members) and `src/Encina.OpenTelemetry/PublicAPI/PublicAPI.Unshipped.txt` (RS0017: remove the roughly 25 lines of the deleted nested `Messaging`, `Systems` and `Operations` members, which are listed in Unshipped only: the Shipped file has no `ActivityTagNames` line), `.github/coverage-manifest/Encina.json`, `.github/coverage-manifest/Encina.OpenTelemetry.json` and add `changelog.d/177-messaging-semantic-conventions.changed.md`, all through `mechanical-fixer`.
4. Tests: `tests/Encina.UnitTests/Diagnostics/MessagingSemanticConventionsTests.cs` (pins every key string, so a rename shows in review); `tests/Encina.UnitTests/OpenTelemetry/MessagingActivityEnricherBodySizeTests.cs` (outbox and scheduled body size equals the UTF-8 byte count for ASCII and multi-byte `Content`; empty `Content` gives 0; inbox and saga add no body size; a `null` activity does nothing; no `ErrorMessage` reaches a tag); edit `tests/Encina.UnitTests/OpenTelemetry/ActivityTagNamesTests.cs` (its `Messaging` region, the only test that references the removed nested classes, is deleted; its assertions move to the key-pinning test).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #177, plan docs/plans/messaging-semantic-conventions-implementation-plan-177.md,
phase 1. ADR-054 D3: attributes follow the latest OpenTelemetry messaging semantic conventions; every key lives in one
constants class. Encina.Messaging cannot reference Encina.OpenTelemetry, so the class goes in core.

TASK:
1. Create MessagingSemanticConventions in src/Encina/Diagnostics (public static, three groups) with the existing keys
   (same strings) and the new ones: body size, batch message count, destination name and partition id, client id,
   conversation id.
2. Make MessagingActivityEnricher use it and add messaging.message.body.size (UTF-8 byte count of Content) for outbox and
   scheduled messages after the activity null check; remove the nested Messaging, Systems and Operations classes from
   Encina.OpenTelemetry.ActivityTagNames.
3. Tests that execute the real enricher with a real Activity; PublicAPI (both packages), manifests and the changelog
   fragment through mechanical-fixer.

KEY RULES:
- Never tag ErrorMessage or EncinaError.Message. No DestinationName property on the message interfaces.
- Keep every existing key string unchanged. No [Obsolete], no alias.

REFERENCE FILES:
- src/Encina.OpenTelemetry/ActivityTagNames.cs, src/Encina.OpenTelemetry/Enrichers/MessagingActivityEnricher.cs
- tests/Encina.UnitTests/OpenTelemetry/ActivityTagNamesTests.cs
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md (D3)
```

</details>

---

### Phase 2: Batch Count on the Live Batch Spans (pull request S2)

> **Goal**: `messaging.batch.message_count` on the outbox enqueue and scheduling cycle spans.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs` (`StartEnqueue` sets `MessagingSemanticConventions.BatchMessageCount` next to `outbox.notification_count`), `src/Encina.Messaging/Diagnostics/SchedulingActivitySource.cs` (`CompleteProcessingCycle(activity, count)` sets it with the real count) and `.github/coverage-manifest/Encina.Messaging.json` and add `changelog.d/177-messaging-batch-count.added.md` through `mechanical-fixer`.
2. Tests: `tests/Encina.UnitTests/Messaging/Diagnostics/BatchMessageCountTests.cs` (an `ActivityListener` captures the tag: enqueue with 3 notifications gives 3; a cycle with 0 and with 5 due messages gives 0 and 5, not the configured `BatchSize`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #177 plan docs/plans/messaging-semantic-conventions-implementation-plan-177.md, phase 2. Phase 1 merged:
MessagingSemanticConventions.BatchMessageCount exists in core.

TASK:
1. Tag the outbox enqueue span with the real notification count and the scheduling cycle span with the real processed count
   (CompleteProcessingCycle already receives it), using the constant.
2. A test with a real ActivityListener for both spans; manifest and changelog through mechanical-fixer.

KEY RULES:
- Do not touch the dead Inbox, Saga and Outbox batch sources (issue #1790 removes them).
- Zero cost without a listener: keep the HasListeners guards.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs, SchedulingActivitySource.cs
- src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs (:147, :164)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| OpenTelemetry messaging semantic conventions | `messaging.destination.name`, `messaging.message.body.size`, `messaging.batch.message_count`, `messaging.client.id`, `messaging.destination.partition.id`, `messaging.message.conversation_id` (development stability) | The keys of the class |
| ADR-054 | D3 constants class and latest names; D5 transport instrumentation by #725 and #177 | Scope and placement |
| ADR-062 | Error category contract | No error is created (register row C1) |
| `AGENTS.md` sections 1 and 3 | Pre-1.0 breaking changes; no error messages in telemetry | Removing the old constants; no `ErrorMessage` tag |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `MessagingActivityEnricher` | `src/Encina.OpenTelemetry/Enrichers/MessagingActivityEnricher.cs` | Gains body size; uses the new class |
| `ActivityTagNames` (public, OpenTelemetry) | `src/Encina.OpenTelemetry/ActivityTagNames.cs` | Messaging members move out |
| `OutboxActivitySource`, `SchedulingActivitySource` | `src/Encina.Messaging/Diagnostics/` | Live batch spans |
| `TenancyActivityEnricher` | `src/Encina.OpenTelemetry/Enrichers/TenancyActivityEnricher.cs` | The tenant tag (ADR-054 D4), not changed |
| Enricher tests | `tests/Encina.UnitTests/OpenTelemetry/`, `tests/Encina.GuardTests/Infrastructure/OpenTelemetry/MessagingActivityEnricherGuardTests.cs` | Behavior to keep |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina`, `Encina.OpenTelemetry`, `Encina.Messaging` | none | No logging code is added (register row C1); the block 5970-5999 assigned to this family is not used by this plan |

### Estimated File Count

Every count includes tests, the coverage manifest, PublicAPI and the changelog fragment; each pull request builds and passes alone.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Constants class, body size | 1 | 4 (`ActivityTagNames.cs`, `MessagingActivityEnricher.cs`, 2 PublicAPI) | 2 | 1 | 2 | 1 (changelog) | 11 | ~520 |
| S2 | Batch count on live spans | 0 | 2 (`OutboxActivitySource.cs`, `SchedulingActivitySource.cs`) | 1 | 0 | 1 | 1 | 5 | ~200 |
| **Total** | | **1** | **6** | **3** | **1** | **3** | **2** | **16** | **~720** |

The columns sum to the total (1 + 6 + 3 + 1 + 3 + 2 = 16; 11 + 5 = 16). Guard tests: the new public class has no methods or parameters; the existing `MessagingActivityEnricherGuardTests` files already cover the enricher arguments and are unchanged.

Per-file coverage targets (per flag, one sentence each):

| File | Unit | Guard | Contract | Property | Justification |
|------|------|-------|----------|----------|---------------|
| `Diagnostics/MessagingSemanticConventions.cs` (core) | 0 | 0 | 0 | 0 | Compile-time constants have no executable lines and no parameters; the key-pinning test asserts them without raising coverage |
| `Enrichers/MessagingActivityEnricher.cs` (existing 90 unit, measured 98.21%) | 95 | 70 | 0 | 0 | Every new tag branch (outbox, scheduled, ASCII and multi-byte content, null activity) is driven with a real activity; the existing guard tests cover the null arguments; targets do not go down |
| `Diagnostics/OutboxActivitySource.cs`, `SchedulingActivitySource.cs` | 90 | 0 | 0 | 0 | The two edited methods are driven by an `ActivityListener`; internal static classes without parameters worth a guard test |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #177 - the missing OpenTelemetry messaging semantic-convention attributes - in two small pull
requests.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no compatibility layers; edit source only with the Edit/Write tools; PublicAPI,
  manifests and changelog fragments through mechanical-fixer.
- ADR-054 D3: latest semantic-convention names, every key in one constants class; D5: destination, partition and client id are
  set by the transports (#725), not by message rows.

IMPLEMENTATION OVERVIEW:
1. S1: MessagingSemanticConventions (core, public static) holding every messaging key; MessagingActivityEnricher uses it and
   adds messaging.message.body.size (UTF-8 bytes of Content) for outbox and scheduled messages; the old nested classes in
   Encina.OpenTelemetry.ActivityTagNames are removed.
2. S2: messaging.batch.message_count on the outbox enqueue span and the scheduling cycle span, with the real count.

KEY PATTERNS:
- Never tag ErrorMessage or EncinaError.Message; keep zero cost without a listener; do not touch the dead activity sources.
- No DestinationName on IOutboxMessage, IInboxMessage or IScheduledMessage; no public EnrichWithBatch.
- Tests execute the real enricher and real spans with an ActivityListener.

REFERENCE FILES:
- src/Encina.OpenTelemetry/Enrichers/MessagingActivityEnricher.cs, src/Encina.OpenTelemetry/ActivityTagNames.cs
- src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs, SchedulingActivitySource.cs
- docs/plans/messaging-semantic-conventions-implementation-plan-177.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Real-time attributes, not cacheable |
| 2 | OpenTelemetry | ✅ Phases 1-2 | This is the semantic-convention completion |
| 3 | Structured Logging | ❌ N/A | Attribute enrichment is silent (register row C1) |
| 4 | Health Checks | ❌ N/A | No health dimension |
| 5 | Validation | ❌ N/A | Values come from internal message properties |
| 6 | Resilience | ❌ N/A | Local attribute setting |
| 7 | Distributed Locks | ❌ N/A | No shared state |
| 8 | Transactions | ❌ N/A | Non-transactional |
| 9 | Idempotency | ❌ N/A | Setting a tag twice is harmless |
| 10 | Multi-Tenancy | ❌ N/A | The tenant tag is `TenancyActivityEnricher`'s (ADR-054 D4); nothing tenant-specific here |
| 11 | Module Isolation | ❌ N/A | Semantic conventions are universal |
| 12 | Audit Trail | ❌ N/A | Infrastructure attributes, not auditable state |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| ADR-054 accepted | ✅ On `main` | D3 and D5 |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Transport instrumentation emits destination, partition and client id | #725 | Uses the constants of S1; opens after S1 |
| Correlation and causation keys | #175 | Adds `ConversationId` use and `encina.messaging.causation_id` to the class |
| Dead and duplicate activity sources | #1790 | Removes the sources S2 does not touch |
| Outbox transport publisher | #2385 | Where a destination first exists for an outbox row |

---

## Next Steps

1. Link this plan from #177 and ask the maintainer to answer Design Choices 1 and 3 and register rows C5 and C6 when work starts.
2. Implement S1 then S2. Open the follow-up issue files `messaging-system-attribute-values` and `messaging-custom-attribute-namespace` when S1 opens (drafts in `artifacts/issues/` of the planning worktree).
3. Maintainer action: amend the acceptance criteria of #177 that name a `DestinationName` property and a public `EnrichWithBatch` to the outcome of Design Choices 2 and 3 (this plan never comments on an issue).
