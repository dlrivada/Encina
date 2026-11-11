# Implementation Plan: `Encina.Messaging` Outbox dispatch idempotency, per-handler completion and aggregated handler errors

> **Issue**: [#1220](https://github.com/dlrivada/Encina/issues/1220)
> **Type**: Feature
> **Complexity**: High (8 phases, 10 database providers, ~60 files in 5 pull requests)
> **Estimated Scope**: ~1,400-1,800 lines of production code + ~2,000-2,400 lines of tests

---

## Summary

When an outbox message has two notification handlers and the second fails, the retry re-runs both, so the first one produces its side effect twice (a duplicate calendar event, a duplicate message to a patient). Today `OutboxProcessorBase.PublishAsync` calls `encina.Publish(typedNotification, cancellationToken)`, which resolves every `INotificationHandler<T>` and dispatches them through the configured strategy; the default strategies stop at the first failure, and nothing records which handler already succeeded. SPEC-002 REQ-031 and AC-031 (reference scenario S1) require that the outbox id is the dispatch idempotency key, that a handler which already succeeded is skipped on retry (keyed by outbox id and handler type), and that every handler is attempted with the errors aggregated.

What it implements:

- The dispatch context carries the outbox id as `IRequestContext.IdempotencyKey`: `OutboxProcessorBase` calls the existing `IEncina.Publish<TNotification>(notification, IRequestContext, CancellationToken)` overload instead of the context-less one.
- A per-handler completion store (`IOutboxHandlerCompletionStore`) keyed by (outbox id, handler type), implemented on the 10 database providers; the row is written when a handler returns `Right`, in the handler's transaction where one exists (the ADR-048 enlistment pattern of the inbox).
- The notification dispatcher, for a dispatch that carries the outbox marker, skips handlers already completed, attempts every remaining handler and returns one aggregated error when any fails.
- Tenant-aware rows (REQ-061) and instrumentation without payloads (REQ-062).

Standards: SPEC-002 REQ-031, AC-031, S1, REQ-061 (AC-043), REQ-062 (AC-044); ADR-001 (ROP); ADR-048 (record versus business transaction); AGENTS.md section 3 (a `Left` is never reported as success).

Estimated scope: about 60 files (core 12, provider 33, tests 10, docs and metadata 5), shipped as 5 pull requests (see "Pull request split"). Affected packages: `Encina` (dispatcher, aggregation), `Encina.Messaging` (store abstraction, processor), the ADO.NET, Dapper and EF Core providers for SqlServer, PostgreSQL and MySQL, `Encina.MongoDB`, `Encina.OpenTelemetry` (instrumented store decorator), `Encina.Testing.Fakes`.

Provider category: Database (10), with one EF Core implementation verified on three databases.

---

## Design Choices

<details>
<summary><strong>1. Idempotency key and how it reaches the handlers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Outbox id as `IRequestContext.IdempotencyKey` via `Publish(notification, context, ct)`** | The overload exists; handlers that send requests inherit the key through `IRequestContextAccessor`; consistent with #1164 (context persisted with the message) | `OutboxProcessorBase` must build a context per message |
| **B) A new `IEncina.PublishFromOutbox(message, id)` method** | Explicit | New public API on the core interface for what a context already carries |
| **C) Pass the id through the notification object** | No dispatcher change | Pollutes every notification type |

### Chosen Option: **A — The existing context overload with the outbox id as key**

### Rationale

- The issue states the dispatched context carries the outbox id as its idempotency key (consistent with #1164) and the maintainer's 2026-10-17 comment settled "implement as written". Handlers observe the dispatch's context through `IRequestContextAccessor` (see `Encina.NotificationDispatcher`, ambient context).
- **Class:** Settled
- **Settled by:** SPEC-002 REQ-031 and #1220 (maintainer comment of 2026-10-17: settled by elimination, implement as written)

</details>

<details>
<summary><strong>2. Where skip and aggregation live — the core dispatcher, gated by the outbox marker</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.NotificationDispatcher` consults an optional `IHandlerCompletionGate` (from `Encina.Messaging`) when the context carries the outbox marker; unmarked publishes are unchanged** | One dispatcher; no reflection duplicated; opt-in per dispatch (pay-for-what-you-use); reuses the aggregate error of `ParallelWhenAllDispatchStrategy` | The core package gets a small abstraction that the messaging package implements |
| **B) A separate `OutboxNotificationDispatcher` in `Encina.Messaging` that resolves handlers itself** | Core untouched | Duplicates handler resolution, polymorphic lookup and compiled delegates; two dispatchers to keep in sync |
| **C) Make every publish with an idempotency key per-handler idempotent** | Uniform | Changes the documented fail-fast contract of `INotificationHandler` for every user; needs a store for non-outbox publishes |

### Chosen Option: **A — A gate consulted by the core dispatcher for marked dispatches (recommended, pending the maintainer)**

### Rationale

- B wins when the maintainer wants the core package free of any messaging-aware hook; A wins when avoiding two copies of the dispatch mechanics matters more. C is rejected because it changes behavior for every user.
- The documented contract in `src/Encina/Abstractions/INotificationHandler.cs` (fail-fast) stays true for ordinary publishes and is updated to describe the marked outbox dispatch.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Handler identity in the completion key</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `handler.GetType().FullName` (namespace and type name, no assembly version)** | Zero API surface; stable across rebuilds | Renaming or moving a handler class re-runs it once for rows still pending |
| **B) A `[HandlerName("...")]` attribute with fallback to the type name** | Survives refactors, like the message type names of #1221 | New public attribute and a registry; larger scope than the issue |

### Chosen Option: **A — The handler type's full name (recommended, pending the maintainer)**

### Rationale

- The completion rows live only until the outbox message is purged (days, not years), so a rename affects at most the messages pending during the deployment; B wins for deployments that rename handlers while thousands of messages are pending.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Shape of the aggregated error</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse the aggregate error built by `ParallelWhenAllDispatchStrategy` (all failure details in one `EncinaError`)** | Existing convention and tests; the retry and dead-letter path of `OutboxBatchProcessor` already treats any `Left` as a failed delivery | The aggregate carries codes, not messages: the persisted `ErrorMessage` of the outbox row stays a code list |
| **B) A new `AggregateHandlerError` type** | Typed access to each error | New type for what the existing error already represents |

### Chosen Option: **A — The existing aggregate error**

### Rationale

- `OutboxBatchProcessor` sends any `Left` through `PublishFailedAsync` (retry, then dead letter); the persisted failure reason stays an error code (never `EncinaError.Message`).
- **Class:** Trivial
- **Settled by:** AGENTS.md section 3 (errors are never swallowed; `EncinaError.Message` never reaches logs or plaintext storage) and `src/Encina/Dispatchers/Strategies/ParallelWhenAllDispatchStrategy.cs` (the existing aggregation convention)

</details>

<details>
<summary><strong>5. Provider scope and naming of the completion store</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IOutboxHandlerCompletionStore` in `Encina.Messaging`, `OutboxHandlerCompletionStore{ADO,Dapper,EF,MongoDB}` per provider family on all 10 providers** | Provider coherence; same shape as the outbox, inbox and saga stores | 33 provider files |
| **B) Store the completions inside the `OutboxMessages` row (a JSON list of completed handlers)** | No new table; one write | The row is rewritten by two writers (handler completion and batch outcome); lost updates; unbounded growth of a hot row |

### Chosen Option: **A — A dedicated store and table on all 10 providers**

### Rationale

- The issue asks for a per-handler completion table; B couples the completion write to the batch outcome write and loses one of them under concurrency.
- **Class:** Settled
- **Settled by:** AGENTS.md section 4 (stores are `{Pattern}Store{Provider}`) and AGENTS.md section 5 (every store feature on all 10 database providers)

</details>

<details>
<summary><strong>6. Lifetime of the completion rows</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Removed together with the outbox message by the processed-message purge** | One retention rule; no orphan rows | The purge (#1200) gains one more delete per batch |
| **B) Own retention option and purge** | Independent | Second timer and option for no benefit |

### Chosen Option: **A — Purged with the outbox message**

### Rationale

- A completion row has no meaning without its outbox message; the purge of #1200 already removes outbox rows by batch and deletes the completions in the same call.
- **Class:** Trivial
- **Settled by:** the plan of #1200 (`docs/plans/processed-message-purge-implementation-plan-1200.md`, purge of processed outbox messages) and AGENTS.md section 3 (Outbox is opt-in; no unbounded growth)

</details>

---

## Implementation Phases

### Phase 1: Core Models & Abstractions

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Outbox/IOutboxHandlerCompletionStore.cs` (new): `Task<Either<EncinaError, bool>> IsCompletedAsync(Guid outboxId, string handlerType, CancellationToken)`, `Task<Either<EncinaError, Unit>> MarkCompletedAsync(Guid outboxId, string handlerType, string? tenantId, CancellationToken)`, `Task<Either<EncinaError, int>> RemoveForMessagesAsync(IEnumerable<Guid> outboxIds, CancellationToken)`; all `Either`, all with a token.
2. `src/Encina/Abstractions/IHandlerCompletionGate.cs` (new, public): `ValueTask<bool> ShouldRunAsync(IRequestContext context, Type handlerType, CancellationToken)` and `ValueTask<Either<EncinaError, Unit>> RecordCompletionAsync(IRequestContext context, Type handlerType, CancellationToken)`.
3. `OutboxDispatchMarker` (new, internal to `Encina.Messaging`, public key constant in `Encina`): the context metadata key `encina.outbox.dispatch` set by the processor.
4. `OutboxErrorCodes`: `HandlerCompletionFailed = "outbox.handler_completion_failed"`.
5. `PublicAPI.Unshipped.txt` of `Encina` and `Encina.Messaging` (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina pre-1.0, .NET 10, no compatibility layers. Issue #1220 makes outbox dispatch idempotent per
handler. Provider abstractions live in Encina.Messaging; the core only gets the small gate interface.
TASK: Create IOutboxHandlerCompletionStore, IHandlerCompletionGate, the marker key and the error code.
KEY RULES: Either<EncinaError,T> returns; CancellationToken on every async member; XML docs; UTC timestamps with
the AtUtc suffix; PublicAPI.Unshipped.txt; no provider-specific code in abstractions.
REFERENCE FILES: src/Encina.Messaging/Outbox/IOutboxStore.cs, src/Encina/Abstractions/INotificationHandler.cs,
src/Encina/Abstractions/IRequestContext.cs
```

</details>

### Phase 2: Dispatcher and Processor — skip, attempt all, aggregate

<details>
<summary><strong>Tasks</strong></summary>

1. `OutboxProcessorBase.PublishAsync`: build the context (correlation id from the message, `WithIdempotencyKey(message.Id.ToString("D"))`, `WithMetadata(OutboxDispatchMarker.Key, true)`; the tenant id from the persisted context once #1164 lands, the default tenant before) and call `encina.Publish(notification, context, ct)`.
2. `Encina.NotificationDispatcher.ExecuteAsync`: when `context.Metadata` has the marker and an `IHandlerCompletionGate` is registered: before each handler `ShouldRunAsync`; after a `Right` `RecordCompletionAsync` (a failed record write is a handler failure, not success); dispatch with the aggregating strategy for marked dispatches whatever the global strategy is; zero handlers stays `Right` (and see #2385 for the integration-event route).
3. A gate registered but a marked dispatch without it is a startup validation error in the outbox registration, not a silent skip.
4. Keep CRAP at 10 or less: the marked path is its own method.
5. Update the fail-fast remark of `INotificationHandler`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Encina.NotificationDispatcher resolves handlers, runs them through the configured strategy and returns the
first error (Sequential, Parallel) or an aggregate (ParallelWhenAll). OutboxProcessorBase publishes without a context.
TASK: Pass the outbox id as the idempotency key with a marker; for marked dispatches skip completed handlers, run
the rest with the aggregating strategy, record each Right through the gate and return one aggregated Left.
KEY RULES: errors never swallowed (a Left from the gate or the store fails the dispatch); unmarked publishes keep
their behavior exactly; TimeProvider for timestamps; no EncinaError.Message in logs or persisted text.
REFERENCE FILES: src/Encina/Dispatchers/Encina.NotificationDispatcher.cs,
src/Encina/Dispatchers/Strategies/ParallelWhenAllDispatchStrategy.cs, src/Encina.Messaging/Outbox/OutboxProcessorBase.cs,
src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs
```

</details>

### Phase 3: Configuration, DI & Registration

<details>
<summary><strong>Tasks</strong></summary>

1. `MessagingServiceCollectionExtensions.AddMessagingServices`: when `UseOutbox` is on, register `IHandlerCompletionGate` (`OutboxHandlerCompletionGate`, scoped, wraps the store) and require an `IOutboxHandlerCompletionStore` (registered by the provider package with `TryAdd*`, before the core registration).
2. Every provider's `AddEncina{Provider}` registration adds its completion store; a store registered by the application wins (AGENTS.md section 3).
3. DI test building the provider with `ValidateOnBuild` and `ValidateScopes` for each provider registration and in both registration orders.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: AGENTS.md section 3, registration completeness: an AddEncina* method must register every dependency the
services it adds resolve, proven by a DI test with ValidateOnBuild and ValidateScopes.
TASK: Register the gate and the per-provider completion store; add the DI tests.
KEY RULES: TryAdd* for satellite registrations called before the core one; a database store wins over any
default in any order; the outbox stays opt-in (UseOutbox).
REFERENCE FILES: src/Encina.Messaging/MessagingServiceCollectionExtensions.cs, src/Encina.ADO.SqlServer (ServiceCollectionExtensions)
```

</details>

### Phase 4: Persistence Entity, Mapper & Provider Scripts and Multi-Provider Implementations — 10 database providers

<details>
<summary><strong>Tasks</strong></summary>

Table/collection `OutboxHandlerCompletions` with columns `OutboxMessageId`, `HandlerType`, `TenantId`, `CompletedAtUtc`, primary key (`OutboxMessageId`, `HandlerType`); `MarkCompletedAsync` is an idempotent insert (a duplicate key is success).

1. SQL scripts `031_CreateOutboxHandlerCompletionsTable.sql` in `Encina.ADO.{SqlServer,PostgreSQL,MySQL}/Scripts` and `Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/Scripts`, and the aggregate script `000_CreateAllTables.sql` where it lists tables.
2. `Outbox/OutboxHandlerCompletionStoreADO.cs` (x3) and `OutboxHandlerCompletionStoreDapper.cs` (x3): `OpenAsync`, parameterized SQL per AGENTS.md section 5 (SQL Server `@param` and `NVARCHAR`; PostgreSQL case-sensitive identifiers; MySQL backticks), enlisted in the ambient transaction like `InboxStore.MarkAsProcessedAsync` (ADR-048).
3. EF Core: `OutboxHandlerCompletion.cs`, `OutboxHandlerCompletionConfiguration.cs`, `OutboxHandlerCompletionStoreEF.cs` (one implementation for SqlServer, PostgreSQL, MySQL).
4. MongoDB: `OutboxHandlerCompletion.cs` (document), `OutboxHandlerCompletionStoreMongoDB.cs` (unique compound index, upsert).
5. `RemoveForMessagesAsync` on every store (Design Choice 6).
6. `InstrumentedOutboxHandlerCompletionStore` in `Encina.OpenTelemetry/MessagingStores` and `FakeOutboxHandlerCompletionStore` in `Encina.Testing.Fakes`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: AGENTS.md section 5: all 10 database providers, identical behavior; EF Core is one implementation on
three databases. Database calls are asynchronous with a CancellationToken.
TASK: Implement the completion store, entity/configuration/scripts per provider as in the Tasks block, one pull
request per provider family.
KEY RULES: idempotent insert; no synchronous database calls; tenant column filled from the context (default
tenant when tenancy is off); SQLite is out; follow the outbox store of the same provider for connection handling.
REFERENCE FILES: src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs, src/Encina.Dapper.PostgreSQL/Outbox/OutboxStoreDapper.cs,
src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs, src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs,
src/Encina.ADO.SqlServer/Scripts/001_CreateOutboxMessagesTable.sql
```

</details>

### Phase 5: Cross-Cutting Integration

<details>
<summary><strong>Tasks</strong></summary>

1. Multi-tenancy: the row carries the tenant id; every query filters by it; the two-tenant test (AC-043); with tenancy off a fixed default tenant applies.
2. Transactions: the completion write enlists in the handler's transaction when the ambient unit of work exists.
3. Validation: startup validation fails when the outbox is on and no completion store is registered.
4. Deferred rows of the matrix keep their reason.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: SPEC-002 REQ-061 and AC-043: tenant-aware records, two-tenant test, fixed default tenant when tenancy off.
TASK: Add the tenant filter and test, the transaction enlistment and the startup validation.
KEY RULES: fail closed on a missing store; no cross-tenant read; AGENTS.md section 6 record per function.
REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxWriter.cs, ITenantContext usages in src/Encina.Tenancy, docs/specifications (REQ-061)
```

</details>

### Phase 6: Observability

<details>
<summary><strong>Tasks</strong></summary>

1. Counters on the `Encina` meter: `encina.outbox.handlers_skipped_total` and `encina.outbox.handler_failures_total` with tags `handler_type` (bounded by the registered handler set) and `tenant` (SPEC-002 REQ-062 asks for the tenant as an attribute); a span `encina.outbox.dispatch_handler` with `encina.tenant_id` and `encina.handler_type`.
2. `[LoggerMessage]` in `OutboxDispatchLog` with EventIds 5805-5810 inside the family block (5800-5849) of `EventIdRanges.MessagingInboxIdempotency`, registered by plan #3440 (`docs/plans/inbox-in-progress-outcome-implementation-plan-3440.md`) or by whichever plan of the family lands first; assembly mapping for `Encina.Messaging` is added once in `EncinaEventIdAllocationTests`.
3. No payload, response body or subject identifier in any tag, metric or log (test with an in-memory exporter, AC-044).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: ADR-021 and SPEC-002 REQ-062. The family block 5800-5849 is registered once as
EventIdRanges.MessagingInboxIdempotency; this plan uses 5805-5810 and registers nothing if the block exists.
TASK: Add the counters, the span and OutboxDispatchLog; re-check on the branch that 5805-5810 are free.
KEY RULES: bounded tag dimensions; EncinaError.Message never in logs; exceptions through ForLogging(); packed ids.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Messaging/Diagnostics/MessagePurgeMetrics.cs,
tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 7: Testing — 7 Test Types

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: dispatcher marked path (skip, all attempted, aggregate), processor context building, gate; `FakeTimeProvider`.
2. Guard: every new public member.
3. Contract: `IOutboxHandlerCompletionStore` on every provider harness (idempotent insert, tenant filter, removal).
4. Property: for any failure pattern across handlers and retries, each handler succeeds at most once per outbox id.
5. Integration: all 10 providers through the shared `[Collection]` fixtures; AC-031 scenario: two handlers, the second fails, retry runs only the second, both errors aggregated; reference scenario S1.
6. Load and Benchmark: `.md` justifications (one extra indexed insert per handler on a path dominated by the handler itself).
7. Manifest targets with justifications for each touched file; CRAP table.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT: Per-flag coverage measured independently; tests run real package code; Shouldly through
Encina.Testing.Shouldly; shared [Collection] fixtures with ClearAllDataAsync.
TASK: Implement the listed tests and the two justification files; set manifest targets.
KEY RULES: no Thread.Sleep; no reflection-only tests; targets never go down; all 10 providers covered.
REFERENCE FILES: tests/Encina.IntegrationTests/Messaging/Outbox, tests/Encina.UnitTests/Messaging (outbox tests),
.github/coverage-manifest/Encina.Messaging.json
```

</details>

### Phase 8: Documentation & Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs; `changelog.d/1220-outbox-dispatch-idempotency.added.md` (via `mechanical-fixer`).
2. `docs/features/transactional-outbox.md` (by `docs-writer`): key, completion table, aggregation, the fail-fast change; remarks of `INotificationHandler`.
3. ADR for Design Choice 2 once decided (next free number at implementation time); `docs/INVENTORY.md`; PublicAPI files.
4. Build 0 warnings; tests at manifest targets.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT: Documentation follows the encina-docs skill; coverage figures are cited, never typed.
TASK: Finish XML docs, changelog, docs/features/transactional-outbox.md, ADR and INVENTORY.
KEY RULES: English only; do not name a private client application; PublicAPI files complete.
REFERENCE FILES: docs/features/transactional-outbox.md, .claude/skills/encina-docs/SKILL.md, changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | How this feature meets it |
|----------|-------------|---------------------------|
| SPEC-002 REQ-031 / AC-031 / S1 | Outbox id is the key; completed handlers skipped; errors aggregated | Phases 2 and 4 |
| SPEC-002 REQ-061 / AC-043 | Tenant-aware records | Phase 5 |
| SPEC-002 REQ-062 / AC-044 | Instrumented, no payloads | Phase 6 |
| ADR-048 | Record versus business transaction | Completion write enlists in the ambient transaction |
| ADR-021 | Registered EventId ranges | Family block 5800-5849 |
| ADR-001 | ROP | Gate and store return `Either` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| OutboxProcessorBase | src/Encina.Messaging/Outbox/OutboxProcessorBase.cs | builds the context and publishes |
| OutboxBatchProcessor | src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs | treats any Left as a failed delivery (retry, dead letter) |
| Encina.NotificationDispatcher | src/Encina/Dispatchers/Encina.NotificationDispatcher.cs | skip, attempt all, record |
| ParallelWhenAllDispatchStrategy | src/Encina/Dispatchers/Strategies/ | aggregation convention |
| IEncina.Publish(notification, context, ct) | src/Encina/Abstractions/IEncina.cs | carries the key |
| Outbox stores of 10 providers | src/Encina.{ADO,Dapper}.*/Outbox, Encina.EntityFrameworkCore/Outbox, Encina.MongoDB/Outbox | pattern for the new stores |
| Create scripts | src/Encina.{ADO,Dapper}.*/Scripts/ | new 031 script |
| InstrumentedOutboxStore pattern | src/Encina.OpenTelemetry/MessagingStores | decorator for the new store |
| MessagingServiceCollectionExtensions | src/Encina.Messaging/ | registration of the gate |
| Processed-message purge | docs/plans/processed-message-purge-implementation-plan-1200.md | removes completions with their message |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` | 5805-5810 inside `MessagingInboxIdempotency` (5800-5849) | Family block assigned 2026-11-11; #3440 uses 5800-5804. The first plan of the family to land registers the block; this plan registers nothing when it exists |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core abstractions and dispatcher | 12 | store interface, gate, marker, error code, dispatcher, processor, registration, gate implementation, metrics, log, fake store, instrumented store |
| Provider implementations | 33 | ADO 6, Dapper 6, EF 3, Mongo 2, 12 SQL scripts (6 new scripts and 6 aggregate-script edits), registrations 4 |
| Tests | 10 | unit, guard, contract, property, integration, DI tests, 2 justification files, EventId map |
| Documentation and metadata | 5 | changelog, outbox guide, ADR, INVENTORY, manifests |
| **Total** | **60** | Five pull requests |

### Pull request split

| PR | Content | Files (approx.) |
|----|---------|-----------------|
| A | Phases 1, 2, 3 (core, dispatcher, processor, registration), fake store, unit, guard, property tests | 14 |
| B | ADO.NET x3 stores, scripts, registrations, integration and contract additions | 14 |
| C | Dapper x3, same content | 14 |
| D | EF Core and MongoDB, instrumented store, same content | 12 |
| E | Phases 5-6 remainder (metrics, logs, tenant test), Phase 8 documentation, justifications | 6 |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
Encina is pre-1.0, .NET 10 / C# 14, nullable enabled, no backward compatibility. Issue #1220 (SPEC-002 REQ-031,
AC-031, S1): the outbox id is the dispatch idempotency key, a handler that completed is skipped on retry (keyed
by outbox id and handler type), every handler is attempted and errors are aggregated. All 10 database providers.

IMPLEMENTATION OVERVIEW:
1. IOutboxHandlerCompletionStore (Messaging), IHandlerCompletionGate (core), marker key, error code.
2. OutboxProcessorBase passes the context with the outbox id; the notification dispatcher skips, attempts all and
   aggregates for marked dispatches only (gate design pending the maintainer).
3. Registration and DI tests; per-provider table/collection, stores, scripts, instrumented and fake stores.
4. Tenant-aware rows, ambient-transaction enlistment, startup validation.
5. Counters, span, OutboxDispatchLog EventIds 5805-5810 in the family block 5800-5849.
6. Tests on all flags with the 10-provider integration matrix; docs, ADR and changelog.

KEY PATTERNS:
- ROP; a Left from a handler, the gate or a store fails the dispatch; TimeProvider; async DB calls with tokens.
- Provider coherence; EF Core one implementation on three databases; SQL differences per AGENTS.md section 5.
- No payload or EncinaError.Message in logs, tags or storage; bounded tag dimensions.
- CRAP <= 10; targets never go down; pull requests of at most 15 files.

REFERENCE FILES:
docs/plans/outbox-dispatch-idempotency-implementation-plan-1220.md, src/Encina.Messaging/Outbox/OutboxProcessorBase.cs,
src/Encina/Dispatchers/Encina.NotificationDispatcher.cs, src/Encina/Dispatchers/Strategies/ParallelWhenAllDispatchStrategy.cs,
src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs, src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs,
src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | No read path that benefits from caching; completion checks need read-your-writes |
| 2 | OpenTelemetry | ✅ | Counters and the per-handler span with the tenant attribute (Phase 6) |
| 3 | Structured Logging | ✅ | `OutboxDispatchLog` EventIds 5805-5810 in the family block (Phase 6) |
| 4 | Health Checks | ❌ | No new checkable dependency; the store is part of the existing outbox database |
| 5 | Validation | ✅ | Startup validation when the outbox is on and no completion store is registered (Phase 5) |
| 6 | Resilience | ❌ | No external call: handlers keep their own policies; retry is the existing outbox retry |
| 7 | Distributed Locks | ❌ | The idempotent insert with a composite primary key serializes duplicates without a lock |
| 8 | Transactions | ✅ | The completion write enlists in the handler's ambient transaction (Phase 4) |
| 9 | Idempotency | ✅ | The feature itself |
| 10 | Multi-Tenancy | ✅ | Tenant column and the two-tenant test (Phase 5) |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ❌ | A completion record is operational state, not a compliance event |

---

## Prerequisites & Dependencies

| Issue | Relation | Notes |
|-------|----------|-------|
| [#1164](https://github.com/dlrivada/Encina/issues/1164) | Soft dependency | Persisted request context; until it lands the tenant id comes from the ambient context or the default tenant |
| [#1200](https://github.com/dlrivada/Encina/issues/1200) | Coordinates | Purge removes completion rows with their message |
| [#2385](https://github.com/dlrivada/Encina/issues/2385) | Shares the processor | Outbox-to-transport publisher; its route replaces in-process dispatch for integration events and both plans change `OutboxProcessorBase` |
| [#1186](https://github.com/dlrivada/Encina/issues/1186) | Parent EPIC | SPEC-002 readiness |
| [#3440](https://github.com/dlrivada/Encina/issues/3440) | Shares the EventId block | Family block 5800-5849 |

---

## Next Steps

1. At pick-up, re-check the Design Choices against today's code, ask the maintainer Dilemmas 2 and 3 and record the answers in `## Maintainer Decisions`; run `check-plan.ps1` without `-Draft`.
2. Re-check that 5805-5810 are free and that the family block is registered before PR A.
3. Coordinate `OutboxProcessorBase` changes with the plan of #2385.
4. Measure per-flag coverage and the CRAP table for every touched file before each pull request.
