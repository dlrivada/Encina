# Implementation Plan: `Encina.Messaging` Outbox dispatch idempotency, per-handler completion and aggregated handler errors

> **Issue**: [#1220](https://github.com/dlrivada/Encina/issues/1220)
> **Type**: Feature
> **Verified against**: origin/main `c49b987e9` (2026-11-12)
> **Complexity**: High (8 phases, 10 database providers, ~101 files in 13 pull requests)
> **Estimated Scope**: ~1,400-1,800 lines of production code + ~2,000-2,400 lines of tests

---

## Summary

When an outbox message has two notification handlers and the second fails, the retry re-runs both, so the first one produces its side effect twice (a duplicate calendar event, a duplicate message to a patient). Today `OutboxProcessorBase.PublishAsync` calls `encina.Publish(typedNotification, cancellationToken)`, which resolves every `INotificationHandler<T>` and dispatches them through the configured strategy; the default strategies stop at the first failure, and nothing records which handler already succeeded. SPEC-002 REQ-031 and AC-031 (reference scenario S1) require that the outbox id is the dispatch idempotency key, that a handler which already succeeded is skipped on retry (keyed by outbox id and handler type), and that every handler is attempted with the errors aggregated.

What it implements:

- The dispatch context carries the outbox id as `IRequestContext.IdempotencyKey`: `OutboxProcessorBase` calls the existing `IEncina.Publish<TNotification>(notification, IRequestContext, CancellationToken)` overload instead of the context-less one.
- A per-handler completion store (`IOutboxHandlerCompletionStore`) keyed by (outbox id, handler type), implemented on the 10 database providers; the row is written when a handler returns `Right`, enlisted in the ambient transaction of the dispatcher's scope when one exists (the ADR-048 enlistment pattern of the inbox). **Delivery guarantee: at-least-once with a narrowed window, not at-most-once.** The completion is recorded after the handler returns, in the dispatcher's own fresh scope (`using var scope`, `Encina.NotificationDispatcher.cs` line 48), where a background processor has no ambient unit of work; a crash between the handler's side effect and `MarkCompletedAsync` re-runs that handler on the retry. The window closes only for a handler that takes part in a transaction of the same scope (it resolves the scope's transaction accessor), in which case the handler's writes and the completion row commit together. When `MarkCompletedAsync` fails after a successful handler the dispatch fails with `Left(outbox.handler_completion_failed)` (the message is retried and the handler may run again).
- The notification dispatcher, for a dispatch that carries the outbox marker, skips handlers already completed, attempts every remaining handler and returns one aggregated error when any fails.
- Tenant-aware rows (REQ-061, Design Choice 8) and instrumentation without payloads (REQ-062).

Standards: SPEC-002 REQ-031, AC-031, S1, REQ-061 (AC-043), REQ-062 (AC-044); ADR-001 (ROP); ADR-048 (record versus business transaction); AGENTS.md section 3 (a `Left` is never reported as success).

Estimated scope: about 101 files (production 45, tests 34, documentation and metadata 22), shipped as 13 pull requests (see "Pull request split"). Affected packages: `Encina` (dispatcher, aggregation), `Encina.Messaging` (store abstraction, processor), the ADO.NET, Dapper and EF Core providers for SqlServer, PostgreSQL and MySQL, `Encina.MongoDB`, `Encina.OpenTelemetry` (instrumented store decorator), `Encina.Testing.Fakes`.

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
- Registered values under Option A: `FullName` of the concrete handler type as resolved from the container; for a generic handler the closed type's `FullName` (which includes the type arguments' assembly-qualified names, so two closings never collide); for a nested type the `+`-separated `FullName` (unique). A `FullName` is `null` only for open generic parameters, which cannot be resolved handlers; the gate returns `Left(outbox.handler_completion_failed)` rather than a null key. A handler type registered twice for the same notification is one completion key: it runs once per dispatch under the gate, and the duplicate is skipped with the completion of the first (the plan documents it; ordinary unmarked publishes still run both).
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Shape of the aggregated error</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse the aggregate error shape of `ParallelWhenAllDispatchStrategy` (its `CreateAggregateError` extracted to an internal shared helper used by the marked path)** | Existing convention and tests; the retry and dead-letter path of `OutboxBatchProcessor` already treats any `Left` as a failed delivery | The aggregate's `details` carry `EncinaError.Message` per handler (verified: `["message"] = e.Error.Message` and a composed message string), so nothing may persist or log the details |
| **B) A new `AggregateHandlerError` type** | Typed access to each error | New type for what the existing error already represents |

### Chosen Option: **A — The existing aggregate shape, persisted as a code only**

### Rationale

- `OutboxBatchProcessor` sends any `Left` through `PublishFailedAsync` (retry, then dead letter); the persisted `ErrorMessage` is built from `error.GetCode()` only (verified: `PublishFailedAsync` uses `error.GetCode().IfNone("encina.unknown")`, line 232), so it holds the aggregate's code and never `EncinaError.Message`. The details with the messages stay in memory for the caller; no processor, log or tag reads `details` (a test pins the persisted text and the captured log output, AGENTS.md section 3, #1168 family).
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

- The issue asks for a per-handler completion table; B couples the completion write to the batch outcome write and loses one of them under concurrency (two writers rewrite one JSON value). Option B also complies with AGENTS.md sections 4 and 5, so the reason is technical, not a rule.
- **Class:** Trivial

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

- A completion row has no meaning without its outbox message, so the rows are removed in the same call that removes the message. The purge of processed messages is the #1200 work item (a plan on a branch, not code on main); this plan's `RemoveForMessagesAsync` is what #1200's purge calls. `IOutboxStore` has no deletion operation on main (verified), so until #1200 lands the completion rows grow exactly like the processed outbox rows they belong to, and no earlier.
- **Class:** Trivial

</details>

<details>
<summary><strong>7. Concurrency of a marked dispatch</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A marked dispatch runs the handlers sequentially, attempting every one even after a failure (a new "sequential, attempt all" behavior of the gate path, whatever the global strategy)** | The scoped completion store, its `DbContext` or ADO connection and the gate are used by one handler at a time (thread-safe); deterministic order and logs | Slower than parallel for slow handlers |
| **B) Keep the aggregating parallel strategy and give each handler its own child scope, completion store instance and connection** | Handlers still run concurrently | One scope and connection per handler; a handler that shares scoped state with another (the ambient transaction) loses it; more code on every provider |

### Chosen Option: **A — Sequential, attempt all (recommended, pending the maintainer)**

### Rationale

- `ParallelWhenAllDispatchStrategy` runs handlers concurrently with `Task.WhenAll`; a scoped `DbContext` or an ADO connection is not thread-safe, so the completion store cannot be shared across them. A wins on safety and simplicity; B wins when marked notifications have slow, independent handlers. Tests run parallel handlers on EF Core and ADO under the chosen option.
- **Class:** Dilemma

</details>

<details>
<summary><strong>8. Tenant of a completion row and the default tenant</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The tenant comes from the ambient context of the dispatch (once #1164 persists it, from the persisted context); with tenancy off the column holds the fixed value `default`, and a tenant id `default` (compared case-insensitively) is refused while tenancy is on** | One constant shared with the rule of #1239 (choice 7: `default` reserved for the default partition) and #122; a real tenant can never share rows with the no-tenant segment | A tenant literally named `default` cannot use the feature |
| **B) Nullable `TenantId` column, null when tenancy is off** | No reserved name | Two code paths in every query (`IS NULL` versus equality) on ten providers, and `NULL` has no tenant-style reserved-value check |

### Chosen Option: **A — Fixed `default`, refused as a real tenant id (recommended, pending the maintainer)**

### Rationale

- The completion key is (outbox id, handler type), so a collision would need two tenants with the same outbox id; the column is for the isolation proof (AC-043) and the purge, and the reserved value keeps the no-tenant segment disjoint. The constant is defined once in `Encina.Messaging` (the unresolved decision of #738 and #1164) and `MarkCompletedAsync` returns `Left(outbox.handler_completion_failed)` for a resolved tenant equal to it while tenancy is on.
- Observability follows ADR-054 D4: the tenant is a span attribute (`encina.tenant_id`) and a metric tag (`tenant`) **only when multi-tenancy is enabled** (the `MultiTenancyMarker` is registered); when it is off no tenant tag or attribute is emitted. The issue's "a fixed default tenant applies" concerns the stored column; the two statements agree because the tag is conditional and the column is not.
- **Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Core Models & Abstractions

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Outbox/IOutboxHandlerCompletionStore.cs` (new): `Task<Either<EncinaError, bool>> IsCompletedAsync(Guid outboxId, string handlerType, CancellationToken)`, `Task<Either<EncinaError, Unit>> MarkCompletedAsync(Guid outboxId, string handlerType, string? tenantId, CancellationToken)`, `Task<Either<EncinaError, int>> RemoveForMessagesAsync(IEnumerable<Guid> outboxIds, CancellationToken)`; all `Either`, all with a token.
2. `src/Encina/Abstractions/IHandlerCompletionGate.cs` (new, public): `ValueTask<bool> ShouldRunAsync(IRequestContext context, Type handlerType, CancellationToken)` and `ValueTask<Either<EncinaError, Unit>> RecordCompletionAsync(IRequestContext context, Type handlerType, CancellationToken)`.
3. `OutboxDispatchMarker` (new, internal to `Encina.Messaging`, public key constant in `Encina`): the context metadata key `encina.outbox.dispatch` set by the processor.
4. `OutboxErrorCodes`: `HandlerCompletionFailed = "outbox.handler_completion_failed"`, created with an `ErrorCategory` (ADR-062): `Unavailable` when the store call fails, `Internal` for a missing gate or an unusable handler key; and the marked-dispatch aggregate keeps the category of the existing `NotificationMultipleFailures` aggregate mapping (`UpstreamFailure`). A unit test asserts `GetCategory()` for each.
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

1. `OutboxProcessorBase.PublishAsync` (today `private static`, receiving only the callback; it becomes an instance method so it can resolve the optional gate; the constructor change of every provider's `OutboxProcessor` is listed in `PublicAPI.Unshipped.txt`, and the order with #2385, #1221 and #1251, which edit the same method, is: whichever lands first, the others rebase): when an `IHandlerCompletionGate` is registered (the presence is checked with `GetService<IHandlerCompletionGate>()` on the batch scope the processor already creates, `OutboxProcessorBase.cs:123`; the gate is scoped and the processor is a singleton hosted service, so the check never uses the root provider; the dispatcher creates its own scope per publish, `Encina.NotificationDispatcher.cs:48`, and resolves its own gate from it, so the marker is set only when the dispatcher's scope can resolve the same registration: both scopes come from the same root provider, and a registration that exists in one exists in the other), build the context (correlation id from the message, `WithIdempotencyKey(message.Id.ToString("D"))`, `WithMetadata(OutboxDispatchMarker.Key, true)`; the tenant id from the persisted context once #1164 lands, the ambient tenant or the fixed `default` before, Design Choice 8) and call `encina.Publish(notification, context, ct)`; when no gate is registered the processor dispatches as today (this keeps the first pull requests inert).
2. `Encina.NotificationDispatcher.ExecuteAsync`: when `context.Metadata` has the marker: resolve the gate; **no gate resolved is `Left(outbox.handler_completion_failed)`, never a skip** (fail closed; the processor only sets the marker when a gate exists, so this is reachable only through a caller that sets the marker by hand). Before each handler `ShouldRunAsync`; after a `Right` `RecordCompletionAsync` (a failed record write is a handler failure, not success); handlers run sequentially and every one is attempted (Design Choice 7), the errors aggregated by the shared helper of Design Choice 4; zero handlers stays `Right` (and see #2385 for the integration-event route).
3. The marked path lives in its own method (a new internal class `MarkedNotificationDispatch`).
4. Keep CRAP at 10 or less: the marked path is its own method.
5. Update the fail-fast remark of `INotificationHandler`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Encina.NotificationDispatcher resolves handlers, runs them through the configured strategy and returns the
first error (Sequential, Parallel) or an aggregate (ParallelWhenAll). OutboxProcessorBase publishes without a context.
TASK: Pass the outbox id as the idempotency key with a marker; for marked dispatches skip completed handlers, run
the rest sequentially (attempt all), record each Right through the gate and return one aggregated Left; a marked
dispatch with no gate is a Left, never a skip.
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

1. `Encina.Messaging` exposes one helper (`AddOutboxHandlerCompletion`) that registers `IHandlerCompletionGate` (`OutboxHandlerCompletionGate`, scoped, wraps the store); each provider registration calls it together with its completion store, so the gate exists exactly when a store does.
2. Every provider's `AddEncina{Provider}` registration adds its completion store; a store registered by the application wins (AGENTS.md section 3).
3. Startup validation (pull request D2, after every provider pull request, see the split): when `UseOutbox` is on and no `IOutboxHandlerCompletionStore` is registered, startup fails with a clear message naming the interface. It lands after every provider registration exists, so no earlier pull request breaks an existing outbox registration.
4. DI test building the provider with `ValidateOnBuild` and `ValidateScopes` for each provider registration and in both registration orders.

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

1. Multi-tenancy (Design Choice 8): the row carries the tenant id; every query filters by it; the two-tenant test (AC-043); with tenancy off the fixed value `default` is stored, and a tenant id `default` (case-insensitive) is refused while tenancy is on (tested).
2. Transactions: the completion write enlists in the ambient transaction of the dispatcher's scope when one exists (a handler that takes part in it commits with the completion row); otherwise it is a separate write and the window of the Summary applies.
3. Validation: startup validation fails when the outbox is on and no completion store is registered (lands in pull request D2, after every provider pull request, Phase 3).
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

1. Counters on the `Encina` meter: `encina.outbox.handlers_skipped_total` and `encina.outbox.handler_failures_total` with tag `handler_type` (bounded by the registered handler set) and, **only when multi-tenancy is enabled (ADR-054 D4)**, the tag `tenant`; a span `encina.outbox.dispatch_handler` with `encina.handler_type` and, only when multi-tenancy is enabled, `encina.tenant_id`. With multi-tenancy off no tenant tag or attribute is emitted (the stored column still holds `default`).
2. `[LoggerMessage]` in `OutboxDispatchLog` with EventIds 5805-5810 inside the family block (5800-5849) of `EventIdRanges.MessagingInboxIdempotency`, registered by plan #3440 or by whichever plan of the family lands first; assembly mapping for `Encina.Messaging` is added once in `EncinaEventIdAllocationTests`. This plan follows #3440's 5800-5804 with no gap; the sibling plans of #1221 and #2385 continue from 5811 (the family's packed order is #3440, #1220, #1221, #2385).
3. Health check (function 4, AC-044 "a health-check test covers each checkable dependency"): the completion store is a database dependency, but it uses the same database and connection configuration as the outbox store, so the existing database health check of that provider already covers reachability; PR D2 adds a test per provider family that a health check built from the provider's registration fails when the shared connection is unavailable, with the completion store registered. No separate check.
4. No payload, response body or subject identifier in any tag, metric or log (test with an in-memory exporter, AC-044).

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
4. Property: for any failure pattern across handlers and retries, no handler re-runs after its completion row is committed (the guarantee is at-least-once with a narrowed window, not at-most-once, see the Summary); a separate unit test pins the window: handler success followed by a failing `MarkCompletedAsync` fails the dispatch and the retry re-runs that handler. Further tests: the persisted `ErrorMessage` of an aggregate failure holds the code only and the captured logs hold no handler error text; parallel-capable handlers on EF Core and ADO run one at a time (Design Choice 7); a marked dispatch with no gate is a `Left`; `GetCategory()` of each new code.
5. Integration: all 10 providers through the shared `[Collection]` fixtures; AC-031 scenario: two handlers, the second fails, retry runs only the second, both errors aggregated; reference scenario S1.
6. Load and Benchmark: `.md` justifications (one extra indexed insert per handler on a path dominated by the handler itself).
7. Manifest targets per touched `src/` file (floors for the worker, never lowered; `.github/coverage-manifest/{Package}.json`):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|-----:|------:|---------:|---------:|------------:|---------------|
| `Encina/Dispatchers/Encina.NotificationDispatcher.cs`, `MarkedNotificationDispatch.cs` | 95 | 100 | 0 | 90 | 0 | The marked path is pure orchestration over fakes; every branch (skip, run, fail, record failure, no gate) is reachable in unit tests. |
| `OutboxProcessorBase.cs` (context building) | 95 | 100 | 0 | 0 | 0 | One method building a context; the gate-present and gate-absent branches are unit-testable. |
| `OutboxHandlerCompletionGate.cs` | 95 | 100 | 0 | 90 | 0 | Thin logic over the store abstraction with the tenant rule. |
| `OutboxHandlerCompletionStore{ADO,Dapper,EF,MongoDB}.cs` (x10 providers) | 0 | 100 | 100 | 0 | 90 | SQL and driver behavior is only provable on a real database through the shared collections and the store contract. |
| `FakeOutboxHandlerCompletionStore.cs`, `InstrumentedOutboxHandlerCompletionStore.cs` | 95 | 100 | 100 | 0 | 0 | Test double and decorator exercised by the unit tests and the contract suite. |

CRAP table of every changed method before each pull request.

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
3. ADR for the decided Dilemmas (2, 3, 7 and 8) once decided (ADR number assigned when the ADR pull request opens); `docs/INVENTORY.md`; PublicAPI files.
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
| `Encina.Messaging` | 5805-5810 inside `MessagingInboxIdempotency` (5800-5849) | Family block assigned 2026-11-11; #3440 uses 5800-5804. Packed order of the family: #3440 (5800-5804), this plan (5805-5810), #1221 (5811-5816), #2385 (5817-5822), with no gaps. The first plan of the family to land registers the block; this plan registers nothing when it exists |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core abstractions and dispatcher | 14 | store interface, gate, marker, error codes, dispatcher, marked-dispatch class, processor, registration helper, gate implementation, metrics, log, fake store, instrumented store, validator |
| Provider implementations | 33 | ADO 6, Dapper 6, EF 3, Mongo 2, 12 SQL scripts (6 new scripts and 6 aggregate-script edits), registrations 4 |
| Tests | 34 | unit, guard, contract, property, 10-provider integration, DI tests, health tests, 2 justification files, EventId map, existing tests that construct the processor; the shared test DDL `tests/Encina.TestInfrastructure/Schemas/SqlServerSchema.cs` and `MySqlSchema.cs`; the test `DbContext`s that must map the new entity (`TestEFDbContext.cs`, `TestPostgreSqlDbContext.cs`, `ContractTestDbContext.cs`, `tests/Encina.UnitTests/EntityFrameworkCore/TestDbContext.cs`) |
| Documentation and metadata | 20 | changelog, outbox guide, ADR, INVENTORY, one coverage manifest per touched package, PublicAPI files |
| **Total** | **101** | Thirteen pull requests |

### Pull request split

Counts include tests, coverage manifests and `PublicAPI.Unshipped.txt`. Each pull request builds alone and leaves existing outbox registrations working: the processor marks a dispatch only when a gate is registered (Phase 2), and the gate exists only where a provider registered its store, so the startup validation lands last.

| PR | Content | Files (approx.) |
|----|---------|-----------------|
| A1 | Abstractions: store interface, gate interface, marker, error codes, gate implementation, registration helper, unit and guard tests, PublicAPI of Encina and Messaging, 2 manifests | 13 |
| A2 | Dispatcher marked path, processor context, `INotificationHandler` remarks, fake store, unit and property tests, existing processor tests that change, the contract suite for `IOutboxHandlerCompletionStore` (run against the fake), manifests, PublicAPI of Fakes | 14 |
| B1 | SQL Server, ADO: store, `031` script, `000_CreateAllTables.sql` edit, registration, integration test, manifest, PublicAPI, plus the shared test DDL `SqlServerSchema.cs` | 8 |
| B2 | SQL Server, Dapper: same content without the shared DDL | 7 |
| B3 | PostgreSQL, ADO: same content (the PostgreSQL integration fixtures use the `031` script, no inline DDL file) | 7 |
| B4 | PostgreSQL, Dapper: same content | 7 |
| B5 | MySQL, ADO: same content plus the shared test DDL `MySqlSchema.cs` | 8 |
| B6 | MySQL, Dapper: same content without the shared DDL | 7 |
| C1 | EF Core: entity, configuration, store, registration, integration test, manifest, PublicAPI, and the four test `DbContext`s that map the new entity (`TestEFDbContext.cs`, `TestPostgreSqlDbContext.cs`, `ContractTestDbContext.cs`, `tests/Encina.UnitTests/EntityFrameworkCore/TestDbContext.cs`) | 11 |
| C2 | MongoDB (document, store, registration, integration test, manifest, PublicAPI) and `InstrumentedOutboxHandlerCompletionStore` in `Encina.OpenTelemetry` (class, test, manifest, PublicAPI) | 10 |
| D1 | Observability: metrics, `OutboxDispatchLog`, EventId range registration and `EncinaEventIdAllocationTests` map, metrics and log tests, manifest, PublicAPI | 9 |
| D2 | Startup validation and its tests, DI tests with `ValidateOnBuild` and `ValidateScopes` per provider family (ADO, Dapper, EF Core, MongoDB: 4 files), health tests per family (4 files), manifest, PublicAPI | 12 |
| E | Documentation, ADR, justification files, changelog | 6 |

A1 first; A2, B1 to B6, C1 and C2 depend on A1 and may land in any order after it; D1 depends on A2; D2 depends on every B and C pull request; E last. The 10-provider contract suite is written once in A2 against the fake; each provider pull request adds its integration test class that runs the same scenarios through its real store. Every pull request stays at 15 files or fewer.

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
2. OutboxProcessorBase passes the context with the outbox id (only when a gate is registered); the notification
   dispatcher skips, attempts all sequentially and aggregates for marked dispatches only (gate design and
   sequential execution pending the maintainer); a marked dispatch with no gate is a Left. Guarantee: at-least-once
   with a narrowed window.
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
| 2 | OpenTelemetry | ✅ | Counters and the per-handler span; the tenant tag and attribute only when multi-tenancy is on (ADR-054 D4, Phase 6) |
| 3 | Structured Logging | ✅ | `OutboxDispatchLog` EventIds 5805-5810 in the family block (Phase 6) |
| 4 | Health Checks | ✅ | The completion store shares the outbox database connection, which the existing provider health check already exercises; PR D2 adds a test per provider family (Phase 6 task 3) |
| 5 | Validation | ✅ | Startup validation when the outbox is on and no completion store is registered (Phase 3, pull request D2) |
| 6 | Resilience | ❌ | No external call: handlers keep their own policies; retry is the existing outbox retry |
| 7 | Distributed Locks | ❌ | The idempotent insert with a composite primary key serializes duplicates without a lock |
| 8 | Transactions | ✅ | The completion write enlists in the ambient transaction of the dispatcher's scope when one exists; otherwise it is a separate write and the at-least-once window applies (Summary, Phase 5) |
| 9 | Idempotency | ✅ | The feature itself (at-least-once with a narrowed window) |
| 10 | Multi-Tenancy | ✅ | Tenant column, fixed `default` reserved when tenancy is off, and the two-tenant test (Design Choice 8, Phase 5) |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ❌ | A completion record is operational state, not a compliance event |

---

## Prerequisites & Dependencies

| Issue | Relation | Notes |
|-------|----------|-------|
| [#1164](https://github.com/dlrivada/Encina/issues/1164) | Soft dependency | Persisted request context; until it lands the tenant id comes from the ambient context or the default tenant |
| [#1200](https://github.com/dlrivada/Encina/issues/1200) | Coordinates | Purge removes completion rows with their message |
| [#2385](https://github.com/dlrivada/Encina/issues/2385), [#1221](https://github.com/dlrivada/Encina/issues/1221), [#1251](https://github.com/dlrivada/Encina/issues/1251) | Share the processor | `OutboxProcessorBase` is edited by all four plans (#2385 routes integration events to a transport, #1221 resolves stored type names, #1251 changes row claiming); rebase order is the order in which they land, and the later pull request rebases onto the instance-method change of `PublishAsync` |
| [#1186](https://github.com/dlrivada/Encina/issues/1186) | Parent EPIC | SPEC-002 readiness |
| [#3440](https://github.com/dlrivada/Encina/issues/3440) | Shares the EventId block | Family block 5800-5849 (packed order in Event ID Allocation) |
| [#738](https://github.com/dlrivada/Encina/issues/738) | Soft dependency | Defines the default-tenant constant and tenancy marker used by Design Choice 8 |

---

## Next Steps

1. At pick-up, re-check the Design Choices against today's code, ask the maintainer Dilemmas 2, 3, 7 and 8 and record the answers in `## Maintainer Decisions`; run `check-plan.ps1` without `-Draft`.
2. Re-check that 5805-5810 are free and that the family block is registered before PR A1 (and again before D1, which adds the log class).
3. Coordinate `OutboxProcessorBase` changes with the plan of #2385.
4. Measure per-flag coverage and the CRAP table for every touched file before each pull request.
