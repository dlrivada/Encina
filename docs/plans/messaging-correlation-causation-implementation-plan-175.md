# Implementation Plan: Correlation and Causation Ids in Messaging Telemetry

> **Issue**: [#175](https://github.com/dlrivada/Encina/issues/175)
> **Type**: Feature (observability core)
> **Related**: #1164 (the persisted envelope that already lists `CorrelationId` and `CausationId`, SPEC-002 REQ-015), #177 (owns the constants class this plan adds one key to), #132 and #186 (trace context), #1237 (saga correlation by external key, a different concept), ADR-054, ADR-062, ADR-028
> **Complexity**: Low-Medium (most of the issue's text is already done or belongs to #1164; what is left is a causation rule, a helper and the span tags)
> **Estimated Scope**: ~120 lines of production code + ~380 lines of tests + 1 documentation file, in three pull requests of at most 15 files and 900 lines each (the third opens after #1164)

---

## Summary

### What exists today (read on 2026-11-18, main `f484cdf4f`)

- `IRequestContext` already has both ids, as strings: `string CorrelationId` ("Always present", taken from `Activity.Current?.Id` or a new id) and `string? CausationId` ("The id of the message or request that caused this request", `src/Encina/Abstractions/IRequestContext.cs`, properties documented at lines 60-75). `RequestContext.CausationId` is set only through the internal `WithCausationId` (`RequestContext.cs:301`); copies and nested dispatches keep it (`ForNestedDispatch`). The issue's `Guid` and "Add `CausationId` to `IRequestContext`" are therefore obsolete.
- The persisted form exists: `PersistedRequestIdentity(Kind, ActorId, TenantId, CorrelationId, CausationId)` (`src/Encina/Identity/PersistedRequestIdentity.cs`), produced by `RequestIdentity.ToPersisted(tenantId, correlationId, causationId)` (`RequestIdentity.cs:201`) and validated and restored by `RequestContextScopeFactory.RunRestoredAsync`, which applies `.WithCausationId(persisted.CausationId)` (`RequestContextScopeFactory.Restore.cs:50`). A search of `src` finds no caller of `ToPersisted` or `RunRestoredAsync` outside the identity folder: no store writes or reads the envelope yet (#1164, open).
- The messaging row interfaces (`IOutboxMessage`, `IInboxMessage`, `IScheduledMessage`, 31 implementers in `src`) have no correlation or causation member. The issue asks for `Guid CorrelationId` and `Guid? CausationId` on all three, with columns on 10 providers; #1164 (SPEC-002 REQ-015, "Settled by elimination 2026-10-17: implement as written") already specifies the envelope `UserId`, `TenantId`, `CorrelationId`, `CausationId` on the outbox, inbox and scheduled rows, written in the same transaction.
- Consumers of the context ids today: Marten event metadata (`src/Encina.Marten/EventMetadataEnrichmentService.cs:62-70`, `ConfigureMartenEventMetadata.cs:56-66`), `DomainEventEnvelope` (`src/Encina.DomainModeling/DomainEventEnvelope.cs`) and the event tags `event.correlation_id` and `event.causation_id` (`ActivityTagNames.EventMetadata`). No messaging span carries either id: `MessagingActivityEnricher` and `MessagingEnricherPipelineBehavior` (`src/Encina.OpenTelemetry/`) tag message id, type, retry count and state only.

### What this plan delivers

The parts of #175 that are neither done nor #1164's:

1. **The causation rule** (Arkency pattern, the issue's "Correlation Pattern"): work done inside the dispatch of a message is caused by that message. `PersistedRequestIdentity.ForHandling(string messageId)` returns the identity with `CausationId = messageId` (the correlation id is kept), so a processor restores the context with the message under dispatch as the cause; every message or event written in that context then persists the right cause with no change to `ToPersisted`.
2. **Span tags** for correlation and causation on messaging spans: `messaging.message.conversation_id` (the semantic-convention key for a correlation id) and `encina.messaging.causation_id`, set by `MessagingEnricherPipelineBehavior` from the context.
3. After #1164 lands, the store enrichers read the same ids from the envelope (third pull request).

- Shared pieces and owner: the persisted envelope is #1164's; the constants class `MessagingSemanticConventions` is #177's (this plan adds `ConversationId` use and one Encina-specific key to it; #177 creates `ConversationId` itself); the trace context is #186's and #132's.
- Affected packages: `Encina` (core), `Encina.OpenTelemetry`.
- Provider category: Database (10) only through #1164; nothing in this plan touches a provider.
- Not delivered, with the reason: the interface members, the 10-provider schemas and the store updates (#1164); the audit-trail integration the issue defers (the audit correlation is read from the context by the audit stores today).

---

## Design Choices

<details>
<summary><strong>1. What happens to the issue's interface members, columns and store changes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Leave them to #1164: one persisted envelope (`UserId`, `TenantId`, `CorrelationId`, `CausationId`) per message row** | One schema change on 10 providers instead of two; the same restore path rebuilds the context; already settled | The issue's literal acceptance items are met by #1164 |
| **B) Add `Guid CorrelationId` and `Guid? CausationId` to the three interfaces here** | Matches the issue | A second schema change on the same tables; `Guid` against the string contract of the context; two representations to keep in sync |

### Chosen Option: **A — #1164 owns the persisted ids**

### Rationale

- #1164 lists the same two ids in its envelope and states the schema change is cheap only once, before 1.0; B would duplicate it with a different type.
- Maintainer action: when #1164 is picked up, amend the acceptance criteria of #175 that name the interfaces and stores to point at it (this plan never comments on an issue).

**Class:** Settled
**Settled by:** #1164 body ("a minimal context envelope persisted with each outbox, inbox and scheduled message: `UserId`, `TenantId`, `CorrelationId`, `CausationId`") and its comment of 2026-10-17 ("Implement as written"); B contradicts it

</details>

<details>
<summary><strong>2. Type of the ids</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `string` (the existing contract), bounded and sanitised by `InboundRequestNormalizer.Id` (at most 128 characters, no control characters)** | One contract for HTTP headers, the W3C activity id, messages and events | Not type-safe against a GUID |
| **B) `Guid`** | Type-safe | The correlation id is a client header or a 55-character W3C id; a GUID would reject both |

### Chosen Option: **A — `string`**

### Rationale

- `IRequestContext.CorrelationId`, `CausationId`, `PersistedRequestIdentity` and `DomainEventEnvelope` already use strings; `HttpContextInboundRequestExtensions.cs:98` takes the header or `Activity.Current?.Id`.

**Class:** Settled
**Settled by:** `src/Encina/Abstractions/IRequestContext.cs` (`string CorrelationId`, `string? CausationId`) and `InboundRequestNormalizer.Id`; B cannot hold the existing values

</details>

<details>
<summary><strong>3. How the message under dispatch becomes the cause of what its handler writes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The restore uses the message id as the context's causation: `RunRestoredAsync(persisted.ForHandling(message.Id.ToString()), ...)`; everything written in that context persists it through the existing `ToPersisted(tenant, context.CorrelationId, context.CausationId)`** | No change to `IRequestContext`, `ToPersisted` or the writer; Marten events appended in the handler get the right cause; the correlation id is untouched | The context no longer carries the message's own cause (it stays in the row) |
| **B) Keep `persisted.CausationId` on the context and put the message id in `Metadata` under a reserved key; the writer reads the key** | The context keeps the message's own cause | A reserved metadata key every writer must know; an untyped `object?` read; events would still get the message's cause, not the message |
| **C) A new `IRequestContext.MessageId` member** | Typed and explicit | A second interface change on the 8 implementers (shared with #186 and #2442) for one value |

### Chosen Option: **A — Message id is the context's cause** (recommended, pending the maintainer)

### Rationale

- A wins when events and messages written by a handler must name the message that triggered them (the convention the issue cites); the message's own cause is still stored in its row and in the audit of the dispatch. B wins if audit needs the cause of the dispatched message itself from the context at handling time; C wins if the maintainer wants the message id readable by every behavior.
- The documentation on `CausationId` ("the message or request that caused this request") is amended to say that inside a restored dispatch it is the message being dispatched.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Attribute keys</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `messaging.message.conversation_id` for the correlation id (the semantic-convention key "conversation ID, sometimes called correlation ID") and `encina.messaging.causation_id` for the cause** | Semantic convention where one exists (ADR-054 D3); `encina.*` lowercase dot-separated for the Encina-specific key | Two different prefixes |
| **B) `messaging.correlation_id` and `messaging.causation_id` (the issue's text)** | Matches the issue | Invented keys inside the reserved `messaging.*` namespace |
| **C) Reuse `event.correlation_id` and `event.causation_id`** | Existing constants | Names events, not messages |

### Chosen Option: **A — Semantic key for correlation, `encina.*` for causation**

### Rationale

- ADR-054 D3: attributes follow the latest messaging conventions and every key lives in one constants class (#177); settled point 4: lowercase, dot-separated names. Existing Encina-specific keys already use the `encina.` prefix (`encina.identity.kind` and `encina.sharding.*` in `src/Encina/Diagnostics/ActivityTagNames.cs`).
- Ids are span attributes only, never metric tags: they have unbounded cardinality (ADR-054 D4 allows only the tenant as a metric tag).

**Class:** Trivial
**Settled by:** ADR-054 D3 and settled point 4; `src/Encina/Diagnostics/ActivityTagNames.cs` (`encina.identity.kind`)

</details>

<details>
<summary><strong>5. Where the tags are set, and whether a log scope is added</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MessagingEnricherPipelineBehavior` tags the current activity from `IRequestContext`; no log scope** | Works for every message kind today with no envelope; one place; no allocation per request | The enrichers of the stores do not tag the ids until #1164 exposes them |
| **B) A + the `EnrichWith*Message` methods read the ids from the envelope after #1164** | The ids are also on store-level spans | Depends on the shape of #1164's public surface; third pull request |
| **C) A + an `ILogger.BeginScope` with both ids per request** | Ids appear on every log line | An allocation and a scope per request; `ActivityTrackingOptions` already adds trace and span ids to every log line |

### Chosen Option: **B — Behavior now, store enrichers after #1164, no log scope** (recommended, pending the maintainer)

### Rationale

- B wins once #1164 exposes the envelope on the message interfaces; until then A is the whole delivery. C wins for a deployment that searches logs by correlation id and does not use the trace id; that deployment can add a scope in its own behavior, and the correlation id is already on every response through `X-Correlation-ID` (`EncinaContextMiddleware.cs:154`).
- The issue's "CorrelationId/CausationId in log scope" item is not delivered under this recommendation.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | No new `EncinaError` code and no logging: `ForHandling` and the tags cannot fail, so there is no ADR-062 `ErrorCategory` and no EventId | Settled | ADR-062 (a category is chosen where an `EncinaError` is created) |
| C2 | `ForHandling` throws `ArgumentException` for a null, empty or whitespace `messageId` (a programming error at a call site, like `ToPersisted`'s `correlationId` check at `RequestIdentity.cs:199`) | Trivial | `RequestIdentity.ToPersisted` |
| C3 | The correlation id is client-controlled but bounded and sanitised on entry; it is never a metric tag; no tenant tag or tenant-qualified id is added here (ADR-054 D4), so the reserved `default` tenant segment does not apply | Settled | `InboundRequestNormalizer.Id`; ADR-054 D4 |
| C4 | A restored context built from an `External` source keeps the existing rule (anonymous, inbound, trusted tenant); `ForHandling` changes only the causation id | Settled | `RequestContextScopeFactory.Restore.cs:42-50` (external rows are not trusted); `AGENTS.md` section 3 (fail closed) |
| C5 | Public name: `PersistedRequestIdentity.ForHandling(string messageId)` | Dilemma (recommended, pending the maintainer) | public surface; alternatives are `WithCause` or an extension method |

---

## Implementation Phases

### Phase 1: Span Tags from the Request Context (pull request C1)

> **Goal**: Messaging spans carry the correlation and causation ids.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.OpenTelemetry/Behaviors/MessagingEnricherPipelineBehavior.cs`: when a messaging context is present (the existing `hasMessagingContext` branch), set `MessagingSemanticConventions.ConversationId` to `context.CorrelationId` and `MessagingSemanticConventions.CausationId` to `context.CausationId` when it is not null. Both keys come from the class of #177, which merges first (`ConversationId` is created there; this pull request adds `CausationId = "encina.messaging.causation_id"` to the Encina-specific group).
2. **Modify** `src/Encina/Diagnostics/MessagingSemanticConventions.cs` (one constant), `src/Encina/PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.OpenTelemetry.json` and add `changelog.d/175-messaging-correlation-tags.added.md` through `mechanical-fixer`.
3. Tests: new `tests/Encina.UnitTests/OpenTelemetry/MessagingEnricherCorrelationTests.cs` (an `ActivityListener` captures the tags for an outbox, inbox, saga and scheduled message in `context.Metadata`; a null cause adds no causation tag; no messaging metadata adds no tag; no listener, no activity, no exception); edit `tests/Encina.ContractTests/Infrastructure/OpenTelemetry/MessagingEnricherPipelineBehaviorContractTests.cs`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #175, plan docs/plans/messaging-correlation-causation-implementation-plan-175.md,
phase 1. IRequestContext already has string CorrelationId and string? CausationId. The constants class
MessagingSemanticConventions comes from the plan of #177 and must be merged first.

TASK:
1. In MessagingEnricherPipelineBehavior tag the current Activity with the correlation id (messaging.message.conversation_id)
   and, when not null, the causation id (encina.messaging.causation_id) whenever a messaging context is present.
2. Add the CausationId constant to the Encina-specific group of the constants class.
3. Tests with a real ActivityListener; PublicAPI, manifest and changelog through mechanical-fixer.

KEY RULES:
- Ids are span attributes only, never metric tags. Never tag ErrorMessage or EncinaError.Message.
- Zero cost without a listener: keep the Activity.Current null check.

REFERENCE FILES:
- src/Encina.OpenTelemetry/Behaviors/MessagingEnricherPipelineBehavior.cs
- src/Encina/Diagnostics/MessagingSemanticConventions.cs (from #177)
- tests/Encina.ContractTests/Infrastructure/OpenTelemetry/MessagingEnricherPipelineBehaviorContractTests.cs
```

</details>

---

### Phase 2: The Causation Rule (pull request C2)

> **Goal**: A processor can restore a context whose cause is the message it dispatches.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Identity/PersistedRequestIdentity.cs`: `public PersistedRequestIdentity ForHandling(string messageId)` (`ArgumentException.ThrowIfNullOrWhiteSpace`; returns `this with { CausationId = messageId }`; XML docs with an example of the processor call).
2. **Modify** the `CausationId` XML remarks of `src/Encina/Abstractions/IRequestContext.cs` (inside a restored dispatch it is the message being dispatched).
3. **Modify** `src/Encina/PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.json` and add `changelog.d/175-persisted-identity-for-handling.added.md` through `mechanical-fixer`; the page `docs/features/messaging-correlation.md` through `docs-writer` (how correlation and causation flow, what a handler writes, why no log scope).
4. Tests: `tests/Encina.UnitTests/Identity/PersistedRequestIdentityForHandlingTests.cs` (the correlation, tenant, kind and actor are kept; a restored context built from the result reports the message id as `CausationId`; a message written in that context persists it through `ToPersisted`); `tests/Encina.GuardTests/Identity/PersistedRequestIdentityGuardTests.cs` (null, empty and whitespace `messageId`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #175 plan docs/plans/messaging-correlation-causation-implementation-plan-175.md, phase 2. Maintainer answers to
Design Choice 3 apply: if option A was not chosen, stop and report.

TASK:
1. Add PersistedRequestIdentity.ForHandling(string messageId) and amend the CausationId remarks of IRequestContext.
2. Unit and guard tests using the real RequestContextScopeFactory restore path; docs page through docs-writer.

KEY RULES:
- Keep the correlation id, tenant, kind and actor unchanged; external rows keep the existing untrusted rule.
- No change to ToPersisted, RunRestoredAsync or IRequestContext members.

REFERENCE FILES:
- src/Encina/Identity/PersistedRequestIdentity.cs, RequestContextScopeFactory.Restore.cs, RequestIdentity.cs (:201)
- tests/Encina.UnitTests/Identity/
```

</details>

---

### Phase 3: Store Enrichers after the Envelope (pull request C3, opens after #1164)

> **Goal**: Store-level spans carry the persisted ids.

<details>
<summary><strong>Tasks</strong></summary>

1. After #1164 merges, read its public envelope surface on `main` and extend `EnrichWithOutboxMessage`, `EnrichWithInboxMessage` and `EnrichWithScheduledMessage` in `src/Encina.OpenTelemetry/Enrichers/MessagingActivityEnricher.cs` to tag the persisted correlation and causation ids; update the existing enricher unit, contract and guard tests, the OpenTelemetry manifest and a changelog fragment. The exact file list is fixed at that moment (expected 6 files).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #175 plan docs/plans/messaging-correlation-causation-implementation-plan-175.md, phase 3, only after #1164 merged
and the maintainer chose option B of Design Choice 5.

TASK:
1. Tag the persisted correlation and causation ids in the three message enrichers using the keys of phases 1 and 2.

KEY RULES:
- Re-read the #1164 public surface on main first; never add a Guid member to the message interfaces.
- Manifest and changelog through mechanical-fixer.

REFERENCE FILES:
- src/Encina.OpenTelemetry/Enrichers/MessagingActivityEnricher.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| Correlation id and causation id in evented systems (Arkency) | A reply keeps `correlation_id` and takes the source `message_id` as `causation_id` | The rule of Design Choice 3 |
| OpenTelemetry messaging semantic conventions | `messaging.message.conversation_id` | The correlation key |
| ADR-054 | D3 names, settled point 4, D4 metric tag cardinality | Keys and span-only ids |
| ADR-062 | Error category contract | No error is created (register row C1) |
| SPEC-002 REQ-015 | The originating actor and tenant survive background dispatch | The envelope of #1164 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IRequestContext.CorrelationId`, `CausationId` | `src/Encina/Abstractions/IRequestContext.cs` | The source of both ids |
| `PersistedRequestIdentity`, `ToPersisted`, `RunRestoredAsync` | `src/Encina/Identity/` | The persisted form and restore path that #1164 wires |
| `MessagingEnricherPipelineBehavior` | `src/Encina.OpenTelemetry/Behaviors/` | Where the tags are set |
| `MessagingSemanticConventions` | `src/Encina/Diagnostics/` (plan #177) | The keys |
| `EventMetadataEnrichmentService` | `src/Encina.Marten/EventMetadataEnrichmentService.cs:62-70` | The existing consumer that benefits from the rule |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina`, `Encina.OpenTelemetry` | none | No logging code is added (register row C1); the block 5970-5999 assigned to this family is not used by this plan |

### Estimated File Count

Every count includes tests, the coverage manifest, PublicAPI, the changelog fragment and the documentation file; each pull request builds and passes alone.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| C1 | Span tags from the context | 0 | 3 (`MessagingEnricherPipelineBehavior.cs`, `MessagingSemanticConventions.cs`, PublicAPI) | 1 | 1 | 1 | 1 (changelog) | 7 | ~300 |
| C2 | The causation rule | 0 | 3 (`PersistedRequestIdentity.cs`, `IRequestContext.cs`, PublicAPI) | 2 | 0 | 1 | 2 (changelog, feature page) | 8 | ~350 |
| C3 | Store enrichers (after #1164) | 0 | 1 | 0 | 3 | 1 | 1 | 6 | ~250 |
| **Total** | | **0** | **7** | **3** | **4** | **3** | **4** | **21** | **~900** |

The columns sum to the total (0 + 7 + 3 + 4 + 3 + 4 = 21; 7 + 8 + 6 = 21).

Per-file coverage targets (per flag, one sentence each):

| File | Unit | Guard | Contract | Property | Justification |
|------|------|-------|----------|----------|---------------|
| `Behaviors/MessagingEnricherPipelineBehavior.cs` | 90 | 70 | 70 | 0 | The tag branches are driven with a real listener for the four message kinds; the existing guard and contract tests execute the real behavior |
| `Identity/PersistedRequestIdentity.cs` | 95 | 80 | 0 | 0 | One new method and the record; unit tests drive the restore path and the guard run reaches the argument checks |
| `Core`/`Abstractions/IRequestContext.cs` | 0 | 0 | 0 | 0 | Only XML remarks change in an interface with no coverable lines; `RequestContext` carries the targets |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #175 - correlation and causation ids in messaging telemetry - in three small pull requests.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no compatibility layers; edit source only with the Edit/Write tools; PublicAPI,
  manifests and changelog fragments through mechanical-fixer; documentation through docs-writer.
- IRequestContext already carries string CorrelationId and string? CausationId; the persisted envelope and the 10-provider
  schema are issue #1164, not this plan.

IMPLEMENTATION OVERVIEW:
1. C1: MessagingEnricherPipelineBehavior tags messaging.message.conversation_id (correlation) and encina.messaging.causation_id
   (cause) from the request context; the keys live in MessagingSemanticConventions (#177).
2. C2: PersistedRequestIdentity.ForHandling(messageId): the message under dispatch becomes the cause of what its handler writes.
3. C3 (after #1164): the store enrichers read the persisted ids from the envelope.

KEY PATTERNS:
- Ids are span attributes, never metric tags; strings, not Guids; no member added to the message interfaces.
- Tests use a real ActivityListener and the real restore path.

REFERENCE FILES:
- src/Encina/Identity/PersistedRequestIdentity.cs, src/Encina/Identity/RequestContextScopeFactory.Restore.cs
- src/Encina.OpenTelemetry/Behaviors/MessagingEnricherPipelineBehavior.cs
- docs/plans/messaging-correlation-causation-implementation-plan-175.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Per-message metadata, not cacheable |
| 2 | OpenTelemetry | ✅ Phases 1 and 3 | Span attributes for both ids |
| 3 | Structured Logging | ⏭️ Deferred | No log scope under the recommended Design Choice 5; the deployment that needs one adds it in its own behavior |
| 4 | Health Checks | ❌ N/A | No health dimension |
| 5 | Validation | ❌ N/A | Ids are bounded and sanitised on entry (`InboundRequestNormalizer`); `ForHandling` checks its argument |
| 6 | Resilience | ❌ N/A | Local, no external call |
| 7 | Distributed Locks | ❌ N/A | No shared state |
| 8 | Transactions | ⏭️ #1164 | The ids are persisted in the same transaction as the message by the envelope of #1164 |
| 9 | Idempotency | ❌ N/A | Ids are not part of any idempotency key; the message id is the idempotency key of the dispatch (#1164) |
| 10 | Multi-Tenancy | ❌ N/A | The tenant travels in the same envelope (#1164); this plan adds no tenant tag (ADR-054 D4) |
| 11 | Module Isolation | ❌ N/A | Ids cross modules by design; `ModuleId` is #2442 |
| 12 | Audit Trail | ⏭️ Deferred | The audit stores already read the correlation id from the context; a per-message audit correlation needs the envelope and is covered by #1164 |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `MessagingSemanticConventions` | Plan #177 (S1) | C1 needs `ConversationId`; this plan names it by issue number |
| `IRequestContext` ids and the restore path | ✅ On `main` | Cited above |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| The persisted envelope and its store wiring | #1164 | Phase 3 opens after it; the writer and the processors call `ForHandling` |
| Trace context | #186, #132 | A separate pair of fields in the same envelope |
| Saga correlation by external key | #1237 | A different concept (a key that finds a saga), not these ids |

---

## Next Steps

1. Link this plan from #175 and ask the maintainer to answer Design Choices 3 and 5 and register row C5 when work starts.
2. Implement #177 S1 first, then C1 and C2; open C3 only after #1164 merges and the maintainer chose option B of Design Choice 5. Amend the acceptance criteria of #175 (interfaces, stores, schemas, log scope) to the outcome of Design Choices 1 and 5 when it is picked up.
