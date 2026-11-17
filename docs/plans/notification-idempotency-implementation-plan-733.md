# Implementation Plan: `Encina.Messaging` per-handler idempotency for notification processing

> **Issue**: [#733](https://github.com/dlrivada/Encina/issues/733)
> **Type**: Feature
> **Verified against**: origin/main `ec129cae5` (2026-11-17)
> **Complexity**: High (5 phases, tests on all 10 database providers, ~36 files in 5 pull requests)
> **Estimated Scope**: ~450-600 lines of production code + ~1,100-1,400 lines of tests

---

## Summary

A notification fans out to several handlers. When the same notification is delivered twice (an at-least-once transport, a vendor retry, an outbox retry), every handler runs again and repeats its side effect (the second email, the second record). Issue #733 asks that a duplicate delivery be detected and skipped **per handler**, with the key `{notification-type}:{notification-id}:{handler-type}` stored in the inbox, behind an opt-in flag, on every database provider.

**What the code does today (verified on `origin/main` `ec129cae5`)**:

- Notifications have no pipeline. `Encina.NotificationDispatcher` (`src/Encina/Dispatchers/Encina.NotificationDispatcher.cs`) resolves every `INotificationHandler<T>`, hands them to the configured dispatch strategy and states in its remarks "No behaviors or processors - notifications are simpler than requests". The issue's `NotificationIdempotencyPipelineBehavior<TNotification>` therefore has nothing to plug into; the dispatcher is the only place that sees one handler at a time.
- `INotification` is a marker without an id (`src/Encina/Abstractions/INotification.cs`). The only delivery identity in flight is `IRequestContext.IdempotencyKey`, which a caller sets with `IEncina.Publish<TNotification>(notification, IRequestContext, CancellationToken)`; a notification published from inside another dispatch inherits no key (`RequestContextDispatchExtensions.IsNestedDispatch`).
- `InboxPipelineBehavior<TRequest, TResponse>` and `InboxOrchestrator` serve `IRequest<T>` marked `IIdempotentRequest` only. The inbox record key is `MessageId` alone, `NVARCHAR(255)` primary key on SQL Server (`src/Encina.ADO.SqlServer/Scripts/002_CreateInboxMessagesTable.sql`); `RequestType` is `NVARCHAR(500)`.
- The merged plan of [#1220](https://github.com/dlrivada/Encina/issues/1220) adds, for dispatches that carry the outbox marker, an optional `IHandlerCompletionGate` that the dispatcher consults before each handler (`ShouldRunAsync`) and after it returns `Right` (`RecordCompletionAsync`). This plan is the second consumer of that hook: inbound notifications keyed by the delivery id, recorded in the inbox store. It builds on #1220 and never duplicates its outbox store.
- The Inbox itself has two open defects that this feature must not depend on blindly: [#2401](https://github.com/dlrivada/Encina/issues/2401) (no atomic claim: two concurrent deliveries both run the handler) and [#3441](https://github.com/dlrivada/Encina/issues/3441) (a handler `Left` is stored as the permanent response, so a transient failure is never retried).

What it implements:

- A marker in the request context (`encina.notification.idempotent`) that opts one delivery in, and a registration flag `MessagingConfiguration.UseNotificationIdempotency` (default `false`, requires `UseInbox`).
- `InboxHandlerCompletionGate` (`Encina.Messaging`): the gate the dispatcher consults for marked deliveries. For each (tenant, notification type, delivery key, handler type) it claims a row in `IInboxStore` before the handler, skips a handler already processed or in flight, records the completion after a `Right` and releases the claim after a `Left`.
- `NotificationHandlerKey`: the injective key composition (Design Choice 3).
- Telemetry (a decision counter, log entries) and tests on all 10 database providers.

**Delivery guarantee**: at-least-once with a narrowed window, not exactly-once. The completion is recorded after the handler returns, in the dispatcher's own scope (`using var scope`, `Encina.NotificationDispatcher.cs`), which has no ambient unit of work for a background caller; a crash between the handler's side effect and the record re-runs that handler on the redelivery (ADR-048 describes when the record and the business write share a transaction). No text of this plan or its documentation says "atomic" or "same transaction" about the dispatcher.

Standards and specifications: ADR-001 and ADR-006 (ROP); ADR-048 (inbox record versus business transaction); ADR-050 (Inbox row: the claim of #2401 is a condition of concurrent consumers); ADR-054 D3 and D4 (package-named meter, tenant tag when tenancy is on); ADR-062 (error category set where the error is created); ADR-021 (EventIds); the idempotent consumer pattern (comparable products: MassTransit's idempotent consumer and Wolverine's inbox deduplication, both per message and per consumer); AGENTS.md section 3 (a `Left` of a store or a handler is never reported as success; messaging patterns are opt-in).

Estimated scope: about 36 distinct files (6 new and 5 modified production files, 7 metadata files, 17 test files, 1 documentation file), shipped as five pull requests (A to E; see "Pull request split").

Affected packages: `Encina` (marker, dispatcher routing, `EventIdRanges.cs`), `Encina.Messaging` (gate, key, telemetry, configuration, registration). **No provider package changes**: the rows are inbox rows written through `IInboxStore`, so the existing stores on the 10 providers are reused (the issue says "all 13 database provider packages"; AGENTS.md section 5 has 10, Oracle and SQLite are out).

Provider category: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (one implementation `InboxStoreEF`, verified on three databases) and MongoDB, proven by the integration scenario of Phase 5, not by new store code.

---

## Design Choices

<details>
<summary><strong>1. The hook — the gate of #1220, or a new interception point in the dispatcher</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `IHandlerCompletionGate` of [#1220](https://github.com/dlrivada/Encina/issues/1220): a second implementation (`InboxHandlerCompletionGate`) selected by a second marker; the contract grows to what an inbound claim needs (a `Left` answer, the notification type, a failure callback)** | One dispatcher hook and one marked-dispatch code path; the gate decides run or skip per handler with the handler's own `Left` handled correctly (released, never cached) | Needs agreement on the final shape of #1220's interface before its first pull request; the contract is not on `origin/main` yet |
| **B) Wrap each handler call in `InboxOrchestrator.ProcessAsync` through a new core hook (the handler is the callback)** | Reuses claim, lease, retries, dead letter and the in-progress outcome ([#2401](https://github.com/dlrivada/Encina/issues/2401), [#3440](https://github.com/dlrivada/Encina/issues/3440)) with no second claim implementation | A second dispatcher hook beside the gate; inherits [#3441](https://github.com/dlrivada/Encina/issues/3441): the orchestrator stores a handler `Left` as the permanent response, so a transient handler failure would never be retried until #3441 ships a retryable class |
| **C) A private hook only for notifications, unrelated to #1220** | Independent of #1220 | Two near-identical hooks in the dispatcher; two marked-dispatch paths to test |

### Chosen Option: **A — one gate hook, extended** (recommended, pending the maintainer)

### Rationale

- A wins while #3441 is open: a handler `Left` is the normal way a notification handler reports a transient failure (`INotificationHandler` returns `Either`), and it must leave the delivery retryable. B wins after #3441 introduces a retryable error class, because it then removes the second claim implementation. C wins only if the maintainer rejects #1220's Design Choice 2.
- The shape this plan needs from the gate: `ShouldRunAsync(context, notificationType, handlerType, ct)` returning `Either<EncinaError, bool>` (a store failure fails the handler, never "run" and never "skip"), and `RecordFailureAsync(context, notificationType, handlerType, error, ct)`; the outbox gate ignores the type and implements the failure callback as a no-op. If #1220's first pull request ships the narrower shape, PR B of this plan changes it together with the outbox gate and its tests (two files).
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the per-handler record lives — inbox rows, the completion store of #1220, or a new store</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Inbox rows through `IInboxStore` (`AddAsync` claim, `MarkAsProcessedAsync`, `MarkAsFailedAsync`), one row per (tenant, notification type, delivery key, handler type)** | The wording of the issue ("in inbox store, same stores as command idempotency"); no schema change; the 10 providers are inherited; the retention purge (`InboxOptions.MessageRetentionPeriod`, `EnableAutomaticPurge`) cleans the rows; the atomic claim of #2401 applies | One inbox row per handler per delivery (a notification with 5 handlers writes 5 rows); the inbox's response column stays empty |
| **B) `IOutboxHandlerCompletionStore` of #1220 with a second key family** | One completion table | Keyed by `Guid outboxId`: inbound ids are strings (transport message ids, vendor event ids); the table has its own cleanup tied to outbox messages |
| **C) A new `INotificationDeduplicationStore` on 10 providers** | Exact fit | About 35 provider files for what the inbox already stores; a third deduplication table |

### Chosen Option: **A — inbox rows** (recommended, pending the maintainer)

### Rationale

- A wins when the application already runs the Inbox (ADR-048, the consumer path); B wins when the same notification must be deduplicated by both an outbox id and an inbound id with one table, which no issue asks for; C wins if inbox rows for handlers must carry fields the inbox does not have (a per-handler result).
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. The key — an opaque hash of length-prefixed segments, or a readable composed string</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MessageId = Base64Url(SHA-256(length-prefixed UTF-8 segments: tenant, notification type, delivery key, handler type))`, 43 characters** | Injective for practical purposes (no two tenant and key pairs map to one id, since every segment is length-prefixed before hashing); always fits the 255-character primary key; carries no payload or personal data; no separator-injection ambiguity | Not human readable; an operator maps a row back through the metadata columns (`RequestType` holds the handler type name) |
| **B) The issue's readable `{notification-type}:{notification-id}:{handler-type}` with tenant added** | Readable in the table | A full type name plus a 255-character delivery key plus a handler name exceeds 255 characters; `:` can occur inside a segment, so two different tuples can produce one string unless every segment is escaped |
| **C) Wait for [#3437](https://github.com/dlrivada/Encina/issues/3437) (key shape of the inbox, `needs-decision`) and adopt its columns** | Consistent with the inbox's final design | Blocks this feature on an open decision |

### Chosen Option: **A — hashed, length-prefixed key** (recommended, pending the maintainer)

### Rationale

- The tenant is part of the hashed input, so [#3437](https://github.com/dlrivada/Encina/issues/3437)'s tenant isolation holds regardless of its answer; the reserved segment `default` is used when tenancy is off, and with tenancy on a missing tenant fails closed (`Left`, no run). A wins on column limits and injectivity; B wins when operators read the table by hand; C wins if #3437 adds a `Tenant` column that a purge or a query by tenant needs.
- An FsCheck property test asserts injectivity over arbitrary segment values (including `:` and the empty string).
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. The notification id — the context idempotency key, or a member of the notification</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IRequestContext.IdempotencyKey`, set by whoever delivers (a transport consumer, a webhook endpoint, the outbox processor of #1220)** | The delivery identity is the envelope's, not the domain event's; the same rule as `InboxPipelineBehavior`; no change to any notification type | A caller that publishes without the context overload gets no protection; the gate fails closed when the marker is set without a key |
| **B) `IIdempotentNotification { string NotificationId }` on the notification** | The id travels with the object | Domain events rarely carry a delivery id; every notification type changes; two senders of one event type would collide |

### Chosen Option: **A — the context key** (recommended, pending the maintainer)

### Rationale

- A wins when the id comes from the transport envelope (a message id, a vendor event id), which is the case the issue names (at-least-once delivery); B wins when the event carries its own business event id and is published in-process from several sources.
- A marked delivery with no key fails closed: `Left(notification.idempotency_key_missing)`, category `InvalidRequest`, no handler runs.
- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Opt-in and its requirement on the Inbox</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MessagingConfiguration.UseNotificationIdempotency` (default `false`); enabling it without `UseInbox` is a registration error; a delivery is protected only when its context carries the marker** | Pay-for-what-you-use; the flag mirrors the issue's `config.UseNotificationIdempotency = true`; no behavior change for any existing publish | A flag and a per-delivery marker |
| **B) Every publish with an idempotency key is per-handler idempotent when the Inbox is on** | Nothing to mark | Changes the fail-fast contract of `INotificationHandler` for every caller and writes a row per handler per publish |

### Chosen Option: **A — opt-in flag plus marker**

### Rationale

- Every messaging pattern is optional and disabled by default, and a module may require another only per the closed list of ADR-050; the flag is an option of the Inbox module, whose store it needs.
- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (every messaging pattern is optional and disabled by default; `config.UseOutbox = true;`) and the [#1220](https://github.com/dlrivada/Encina/issues/1220) plan, Design Choice 2 (option C, every publish idempotent, is rejected because it changes the documented fail-fast contract)

</details>

<details>
<summary><strong>6. A handler failure — release the claim so the redelivery re-runs it</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A handler `Left` or exception releases the claim (`MarkAsFailedAsync`, counting a retry); the next delivery re-runs only that handler; the processed handlers stay skipped** | At-least-once for the failed handler; exactly the per-handler behavior the issue asks for | A handler that fails permanently is retried until `InboxOptions.MaxRetries`, then the gate answers `Left` (dead letter through the existing capture is the inbox's behavior) |
| **B) The claim stays until its lease expires** | No failure callback | A transient failure delays the retry by the lease; the redelivery is skipped as "in progress" |

### Chosen Option: **A — release on failure**

### Rationale

- The in-flight duplicate rule: while a row is claimed and its lease has not expired, a duplicate delivery is skipped (the original delivery stays unacknowledged until it completes, so a crash redelivers it); after the lease expires the next delivery re-claims.
- **Class:** Settled
- **Settled by:** [#2401](https://github.com/dlrivada/Encina/issues/2401) (settled by elimination on 2026-10-17: atomic claim with a lease for crashed handlers, so an unfinished claim is re-run after expiry) and `src/Encina.Messaging/Inbox/IInboxStore.cs` (`MarkAsFailedAsync` is the existing release-with-retry operation)

</details>

---

## Implementation Phases

### Phase 1: Core Models — the key, the error codes and the marker

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/Notifications/NotificationHandlerKey.cs`** (new, internal static): `string Compose(string tenant, string notificationType, string deliveryKey, string handlerType)` (length-prefixed UTF-8 segments, SHA-256, Base64Url, 43 characters).
2. **`src/Encina.Messaging/Notifications/NotificationIdempotencyErrorCodes.cs`** (new, public static): `KeyMissing = "notification.idempotency_key_missing"` (`InvalidRequest`), `TenantMissing = "notification.idempotency_tenant_missing"` (`InvalidRequest`), `StoreFailed = "notification.idempotency_store_failed"` (`Unavailable`), `RetriesExceeded = "notification.idempotency_retries_exceeded"` (`UpstreamFailure`, the category of the aggregate handler failure); each created with its ADR-062 category and asserted by a unit test through `GetCategory()`.
3. **`src/Encina/Abstractions/NotificationIdempotencyMarker.cs`** (new, public static class): `Key = "encina.notification.idempotent"` and the extension `IRequestContext WithIdempotentNotificationDelivery(this IRequestContext context)` (sets the marker through `WithMetadata`).
4. `PublicAPI.Unshipped.txt` of `Encina` and `Encina.Messaging` (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina is pre-1.0, .NET 10 / C# 14, nullable enabled, no backward compatibility, ROP everywhere
(Either<EncinaError, T>). Issue #733 makes notification handlers idempotent per delivery through the dispatcher gate
of issue #1220 and inbox rows. Design choices are in docs/plans/notification-idempotency-implementation-plan-733.md.
TASK: Create NotificationHandlerKey (length-prefixed segments, SHA-256, Base64Url), NotificationIdempotencyErrorCodes
(each created with its ADR-062 category) and NotificationIdempotencyMarker with WithIdempotentNotificationDelivery.
KEY RULES: XML docs on every public symbol; guard clauses on public parameters; no [Obsolete]; no personal data or
delivery keys in error messages; PublicAPI.Unshipped.txt; zero warnings.
REFERENCE FILES: src/Encina.Messaging/Inbox/InboxOrchestrator.cs (InboxErrorCodes), src/Encina/Abstractions/IRequestContext.cs,
src/Encina/Errors/ErrorCategory.cs, docs/plans/outbox-dispatch-idempotency-implementation-plan-1220.md
```

</details>

### Phase 2: Gate Implementation — claim, skip, record, release

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/Notifications/InboxHandlerCompletionGate.cs`** (new, public sealed, scoped): constructor `(IInboxStore store, InboxOptions options, ILogger<InboxHandlerCompletionGate> logger, TimeProvider? timeProvider = null)`; the gate implements the contract of Design Choice 1:
   - `ShouldRunAsync`: no key in the context is `Left(KeyMissing)`; with the `MultiTenancyMarker` registered and no tenant is `Left(TenantMissing)`; compose the key; `GetMessageAsync`: processed means skip (`false`), an unprocessed row inside its lease means skip as in progress, a row at `RetryCount >= MaxRetries` is `Left(RetriesExceeded)`, a missing row or an expired lease is claimed through `AddAsync` (the claim result of #2401; a lost race is a skip) and answers run; every store `Left` becomes `Left(StoreFailed)`.
   - `RecordCompletionAsync`: `MarkAsProcessedAsync` with an empty response; a `Left` is returned (the dispatcher counts it as the handler's failure, #1220).
   - `RecordFailureAsync`: `MarkAsFailedAsync` with the exception type or error code only (never the message).
   - Time from `TimeProvider`; the lease value is whatever #2401 defines.
2. Register as scoped by `AddEncinaMessaging` only when the flag is on (Phase 3); the gate is resolved by the dispatcher from its own scope, so no singleton captures it.
3. CRAP: one method per decision (`DecideAsync`, `ClaimAsync`), each under 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: issue #733, Phase 2. The gate contract is extended (Either answer, notification type, failure callback);
IInboxStore claim semantics come from issue #2401; read what they added before writing the claim step.
TASK: Implement InboxHandlerCompletionGate over IInboxStore as described in the plan.
KEY RULES: every store Left fails the handler and is never reported as skip or run; no DateTime.UtcNow; the delivery
key, tenant and EncinaError.Message never reach logs or tags; store errors by code only; a scoped service is never
injected into a singleton; XML docs; guard clauses.
REFERENCE FILES: src/Encina.Messaging/Inbox/IInboxStore.cs, src/Encina.Messaging/Inbox/InboxOrchestrator.cs,
src/Encina.Messaging/Inbox/InboxOptions.cs, docs/plans/inbox-in-progress-outcome-implementation-plan-3440.md
```

</details>

### Phase 3: Configuration, DI and Dispatcher Routing

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/MessagingConfiguration.cs`** (modify): `UseNotificationIdempotency` (bool, default `false`) with XML docs naming the delivery guarantee; included in the active-pattern count where the existing flags are listed (`MessagingConfiguration.cs:1020` region).
2. **`src/Encina.Messaging/MessagingServiceCollectionExtensions.cs`** (modify): when the flag is on, `UseInbox` must be on, otherwise a registration error naming both flags; register `InboxHandlerCompletionGate` as `IHandlerCompletionGate` with the select-by-marker rule below. The DI test of Phase 5 builds the provider with `ValidateOnBuild` and `ValidateScopes`.
3. **`src/Encina/Dispatchers/Encina.NotificationDispatcher.cs`** (modify, after #1220's marked path exists): the marked-dispatch branch of #1220 also triggers on `NotificationIdempotencyMarker.Key`; the gate is chosen by the marker present (outbox marker: the outbox gate; notification marker: the inbox gate); a marker with no registered gate is `Left` (fail closed), never a skip. Unmarked publishes are unchanged.
4. Both gates registered at once (an application with the outbox completion store and this flag) are told apart by marker, not by registration order: the gates are registered as keyed services whose key is the marker constant.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: issue #733, Phase 3. Issue #1220 already routes marked outbox dispatches to IHandlerCompletionGate.
TASK: Add UseNotificationIdempotency (requires UseInbox, registration error otherwise), register the inbox gate as a
keyed service, and let the dispatcher choose the gate by the marker present.
KEY RULES: opt-in and disabled by default; unmarked publishes keep today's behavior; a marked delivery with no gate is
Left; scoped services resolved from the dispatcher's own scope; DI test with ValidateOnBuild and ValidateScopes.
REFERENCE FILES: src/Encina/Dispatchers/Encina.NotificationDispatcher.cs, src/Encina.Messaging/MessagingConfiguration.cs,
src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
```

</details>

### Phase 4: Observability

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/Diagnostics/NotificationIdempotencyMetrics.cs`** (new, internal static): meter `Encina.Messaging` (ADR-054 D3, no `_total`), counter `encina.messaging.notification_idempotency.decisions` with tag `decision` (`run`, `skipped`, `in_progress`, `failed`) and tag `tenant` only when the `MultiTenancyMarker` is registered (ADR-054 D4). No handler or notification type as a tag (open sets).
2. **`src/Encina.Messaging/Diagnostics/NotificationIdempotencyLog.cs`** (new, `[LoggerMessage]`): EventIds 7830 (`DuplicateSkipped`), 7831 (`InProgressSkipped`), 7832 (`ClaimFailed`, exception through `ForLogging()`), 7833 (`CompletionRecordFailed`), 7834 (`FailureRecorded`), 7835 (`RetriesExceeded`), carrying the handler type name and the error code only, in the new range `MessagingNotificationIdempotency = (7830, 7839)` registered first in `src/Encina/Diagnostics/EventIdRanges.cs` and mapped to `Encina.Messaging` in the `AssemblyRanges` map of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
3. The current activity gets the tag `encina.notification_idempotency.decision`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: issue #733, Phase 4. ADR-054 D3/D4 govern meters and tenant tags; ADR-021 governs EventIds.
TASK: Add NotificationIdempotencyMetrics and NotificationIdempotencyLog (EventIds 7830-7835), register the range first.
KEY RULES: [LoggerMessage] source generator; ids packed from 7830 without gaps; never log the delivery key, the tenant,
the payload or EncinaError.Message; bounded metric dimensions; update the allocation test map.
REFERENCE FILES: src/Encina.Messaging/Diagnostics/MessagePurgeMetrics.cs, src/Encina/Diagnostics/EventIdRanges.cs,
tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 5: Testing and Documentation

<details>
<summary><strong>Tasks</strong></summary>

1. Unit (`tests/Encina.UnitTests/Messaging/Notifications/`): `NotificationHandlerKeyTests`, `InboxHandlerCompletionGateTests` (first delivery runs, second skips, in-flight skips, expired lease re-claims, `Left` releases, retries exceeded, missing key, missing tenant under tenancy, store `Left` fails closed), `NotificationIdempotencyMetricsTests`, a dispatcher test with two handlers (A succeeds, B fails: redelivery runs only B), and the two-tenant case (same key, two tenants, two runs).
2. Property (`Encina.PropertyTests`): `NotificationHandlerKey` injectivity over arbitrary strings.
3. Guard (`Encina.GuardTests`): the gate constructor and each method, `WithIdempotentNotificationDelivery(null)`.
4. Contract: the gate against a real `IInboxStore` fake from `Encina.Testing.Fakes` (the claim, skip and release behavior).
5. Integration (`Encina.IntegrationTests`, shared `[Collection]` fixtures, the existing inbox harness `InboxPipelineHarnesses.cs`): the scenario "two deliveries, two handlers, one fails once" on all 10 providers (ADO.NET and Dapper x SqlServer, PostgreSQL, MySQL; EF Core on the three databases; MongoDB), plus the concurrent duplicate scenario once #2401 is merged.
6. DI: the Messaging DI test extended: flag on without `UseInbox` is a registration error; flag on with `UseInbox` builds with `ValidateOnBuild` and `ValidateScopes`.
7. Load and benchmark: justification files (the gate adds one inbox read and one write per handler of a marked delivery; no new concurrency primitive).
8. Documentation through `docs-writer` and `mechanical-fixer`: XML docs, changelog fragment `changelog.d/733-notification-idempotency.added.md`, the inbox guide section, `docs/INVENTORY.md`, the `INotificationHandler` remarks (marked deliveries).
9. Verification: `dotnet build --configuration Release` (0 warnings), `dotnet test`, per-flag coverage against the table below, `crap-gate` table for the changed methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: issue #733. Tests must execute real package code. Database features require integration tests on real
databases through shared [Collection] fixtures on all 10 providers; a .md justification is never allowed for them.
TASK: Write the unit, property, guard, contract, integration and DI tests listed in the plan and the justification files
for load and benchmark; hand documentation to docs-writer.
KEY RULES: Shouldly through Encina.Testing.Shouldly; FakeTimeProvider; no Thread.Sleep; fixtures through the
constructor with ClearAllDataAsync in InitializeAsync; builders for test data.
REFERENCE FILES: tests/Encina.IntegrationTests/Messaging/InboxPipelineHarnesses.cs,
tests/Encina.IntegrationTests/Messaging/InboxPipelineScenario.cs, .github/coverage-manifest/Encina.Messaging.json
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | How this feature meets it |
|----------|-------------|---------------------------|
| ADR-001, ADR-006 | Operations return `Either<EncinaError, T>` | The gate answers `Either`; a store failure fails the handler |
| ADR-048 | Inbox record versus business transaction | The delivery guarantee paragraph; the record is written after the handler in the dispatcher's scope |
| ADR-050 (Inbox row) | The claim of #2401 is required when consumers run concurrently | Prerequisite; the flag requires `UseInbox` |
| ADR-062 | Error category set where the error is created | Phase 1 codes carry `InvalidRequest`, `Unavailable`, `UpstreamFailure` |
| ADR-054 D3, D4 | Package-named meter without `_total`; tenant tag when tenancy is on | `NotificationIdempotencyMetrics` |
| ADR-021 | EventId ranges are registered before use | `MessagingNotificationIdempotency` 7830-7839 |
| Idempotent consumer pattern (MassTransit, Wolverine) | Deduplicate per message and per consumer | One claim per (delivery, handler) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Encina.NotificationDispatcher` | `src/Encina/Dispatchers/Encina.NotificationDispatcher.cs` | The only per-handler interception point; routes marked deliveries to a gate |
| `IHandlerCompletionGate` (planned) | `src/Encina/Abstractions/` (from #1220) | The hook this feature implements a second time |
| `IInboxStore`, `InboxOptions`, `InboxMessage` rows | `src/Encina.Messaging/Inbox/` and the 10 provider inbox stores | Claim, processed mark, failure mark, retention purge |
| `IRequestContext.IdempotencyKey`, `WithMetadata`, `IsNestedDispatch` | `src/Encina/Abstractions/IRequestContext.cs`, `src/Encina/Core/RequestContextDispatchExtensions.cs` | Delivery identity and the marker |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | Tenant fail-closed rule and the tenant tag |
| `MessagePurgeMetrics` | `src/Encina.Messaging/Diagnostics/MessagePurgeMetrics.cs` | Pattern for the counter class |
| Inbox integration harness | `tests/Encina.IntegrationTests/Messaging/InboxPipelineHarnesses.cs`, `InboxPipelineScenario.cs` | Provider matrix of the scenario |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` | **7830-7835** used of the new `MessagingNotificationIdempotency` = (7830, 7839) | From the block 7800-7849 assigned to this family (7800-7819 `CachingIdempotency` is #334, 7840-7849 is #735); 7836-7839 stay free; `Messaging` 2800-2999 has no free packed block (2961-2992 are reserved by other plans) |

### Per-file coverage targets

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|-----:|------:|---------:|---------:|------------:|---------------|
| `Notifications/InboxHandlerCompletionGate.cs` | 92 | 70 | 70 | 0 | 80 | Every decision branch is unit-testable with a fake store; the contract suite runs the gate over a real fake; the integration scenario proves it on 10 providers; property has nothing to generate |
| `Notifications/NotificationHandlerKey.cs` | 100 | 0 | 0 | 100 | 0 | A pure function asserted exhaustively by unit and property tests; internal static, no `InternalsVisibleTo` for `Encina.GuardTests` |
| `Notifications/NotificationIdempotencyErrorCodes.cs` | 0 | 0 | 0 | 0 | 0 | Constants only, no executable code; categories asserted in the gate tests |
| `Diagnostics/NotificationIdempotencyMetrics.cs`, `NotificationIdempotencyLog.cs` | 85 | 0 | 0 | 0 | 0 | Recorded through a `MeterListener` and a fake logger; internal static, no guard-reachable parameters |
| `Abstractions/NotificationIdempotencyMarker.cs` (Encina) | 100 | 90 | 0 | 0 | 0 | One extension method with a null guard, fully asserted |
| `Dispatchers/Encina.NotificationDispatcher.cs` (modified) | current | current | 0 | 0 | 0 | Existing targets stay (they never go down); the marker branch is covered by the dispatcher unit test |
| `MessagingConfiguration.cs`, `MessagingServiceCollectionExtensions.cs` (modified) | current | current | 0 | 0 | 0 | Existing targets stay; the flag and the registration error are covered by the DI test |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production source, new | 6 | key, error codes, gate, metrics, log (`Encina.Messaging`) and the marker (`Encina`) |
| Production source, modified | 5 | `Encina.NotificationDispatcher.cs`, `MessagingConfiguration.cs`, `MessagingServiceCollectionExtensions.cs`, `EventIdRanges.cs`, the `INotificationHandler` remarks |
| Metadata | 7 | `PublicAPI.Unshipped.txt` (Encina, Encina.Messaging), manifests `Encina.json`, `Encina.Messaging.json`, changelog fragment, `docs/INVENTORY.md`, README of `Encina.Messaging` |
| Tests | 17 | unit (key, gate, metrics, dispatcher), property, guard (gate, marker), contract, integration scenario files (5: ADO.NET, Dapper, EF Core, MongoDB harness registrations and the shared scenario), DI test extended, allocation map, justification files (2) |
| Documentation | 1 | inbox guide section |
| **Total** | **~36 distinct** | Five pull requests (the table below sums to 42 because the `PublicAPI` files and manifests are touched in several) |

### Pull request split

| PR | Content | Files (approx.) |
|----|---------|-----------------|
| A | Phase 4 classes: `NotificationIdempotencyMetrics`, `NotificationIdempotencyLog`, `EventIdRanges.cs`, allocation map, `PublicAPI` of `Encina` and `Encina.Messaging`, manifests `Encina.json` and `Encina.Messaging.json`, metrics unit test | 9 |
| B | Phases 1 and 2: key, error codes, gate, `PublicAPI` and manifest of `Encina.Messaging`, unit tests (key, gate), property test, gate guard test, contract test; starts after [#1220](https://github.com/dlrivada/Encina/issues/1220) A1 (the gate contract), [#2401](https://github.com/dlrivada/Encina/issues/2401) (the claim) and A | 10 |
| C | Phase 3: marker, dispatcher routing, configuration flag, registration, `PublicAPI` of `Encina` and `Encina.Messaging`, manifests `Encina.json` and `Encina.Messaging.json`, marker guard test, dispatcher unit test, DI test extended | 11 |
| D | Phase 5 integration: the scenario on the 10 providers (5 test files) | 5 |
| E | Load and benchmark justification files, changelog fragment, README, guide section, `docs/INVENTORY.md`, `INotificationHandler` remarks | 7 |

Order: A, B, C, D, E. Each pull request builds alone: A adds unused telemetry classes; B adds a gate nothing registers; C registers it behind the flag (off by default) and routes only marked deliveries; D and E add tests and documentation. If #1220's first pull request ships a narrower gate than Design Choice 1 needs, the change to the interface and the outbox gate (two files plus their tests) is its own pull request B0 before B, counted at about 6 files.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
Encina is pre-1.0, .NET 10 / C# 14, nullable enabled, no backward compatibility (no [Obsolete], no aliases).
Issue #733 makes notification handlers idempotent per delivery and per handler. Notifications have no pipeline, so the
hook is the dispatcher gate of issue #1220; the record is an inbox row (issue #2401 provides the atomic claim).
Prerequisites: #1220 A1 (IHandlerCompletionGate and the marked dispatch) and #2401 merged.

IMPLEMENTATION OVERVIEW:
1. Encina: NotificationIdempotencyMarker (metadata key plus WithIdempotentNotificationDelivery); the dispatcher routes
   marked deliveries to the gate chosen by marker; a marker with no gate is Left.
2. Encina.Messaging: NotificationHandlerKey (length-prefixed segments, SHA-256, Base64Url), error codes (ADR-062
   categories), InboxHandlerCompletionGate (claim, skip, record, release) over IInboxStore, the flag
   UseNotificationIdempotency (requires UseInbox), registration.
3. Observability: NotificationIdempotencyMetrics (counter, tenant tag only when tenancy is on), NotificationIdempotencyLog
   EventIds 7830-7835 in the new range MessagingNotificationIdempotency (7830, 7839) registered first.
4. Tests on all 10 providers plus unit, property, guard, contract and DI; per-file targets from the plan.
5. Docs: XML docs, changelog fragment, inbox guide, INVENTORY, PublicAPI files.

KEY PATTERNS:
- ROP: a store Left or a handler Left is never reported as success or as skip; a missing key or tenant fails closed.
- At-least-once with a narrowed window; never write "atomic" or "same transaction" about the dispatcher.
- TimeProvider for time; no DateTime.UtcNow. Keys, tenants and EncinaError.Message never reach logs or tags.
- Scoped gate resolved from the dispatcher's own scope; never injected into a singleton.
- Provider coherence: no provider-specific code in Encina.Messaging; the 10 providers are proven by tests.
- CRAP <= 10 on every changed method; targets never go down; one pull request per phase group.

REFERENCE FILES:
docs/plans/notification-idempotency-implementation-plan-733.md, src/Encina/Dispatchers/Encina.NotificationDispatcher.cs,
src/Encina.Messaging/Inbox/IInboxStore.cs, src/Encina.Messaging/Inbox/InboxOrchestrator.cs,
docs/plans/outbox-dispatch-idempotency-implementation-plan-1220.md, src/Encina/Diagnostics/EventIdRanges.cs,
tests/Encina.IntegrationTests/Messaging/InboxPipelineHarnesses.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The per-handler record lives in the inbox store, not in a cache |
| 2 | OpenTelemetry | ✅ | Counter `encina.messaging.notification_idempotency.decisions` with tag `decision` and tag `tenant` only when multi-tenancy is on (ADR-054 D3 and D4) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` ids 7830-7839 in the new range `MessagingNotificationIdempotency`, handler type name and error code only |
| 4 | Health Checks | ❌ | The inbox store already has its health check and no new dependency is added |
| 5 | Validation | ✅ | A marked delivery with no idempotency key, or with no tenant under multi-tenancy, fails closed with `Left` instead of running unprotected |
| 6 | Resilience | ❌ | No external call beyond the inbox store, whose failures fail the handler visibly |
| 7 | Distributed Locks | ❌ | The atomic claim of [#2401](https://github.com/dlrivada/Encina/issues/2401) replaces a lock per message (settled by elimination on 2026-10-17) |
| 8 | Transactions | ✅ | The processed mark follows the ADR-048 enlistment of the inbox record, stated as at-least-once with a narrowed window (no atomicity claim) |
| 9 | Idempotency | ✅ | This is per-handler idempotency of notification processing |
| 10 | Multi-Tenancy | ✅ | The tenant is part of the hashed key, so two tenants with one delivery key never collide; missing tenant fails closed under tenancy |
| 11 | Module Isolation | ❌ | The key carries no module id and the inbox is not module scoped ([#761](https://github.com/dlrivada/Encina/issues/761) adds `ModuleId` to inbox rows separately) |
| 12 | Audit Trail | ❌ | A skipped duplicate delivery is a flow-control signal, not a state change or a compliance event |

---

## Prerequisites & Dependencies

| Issue | Relation | Notes |
|-------|----------|-------|
| [#1220](https://github.com/dlrivada/Encina/issues/1220) | Blocks PR A | Owns `IHandlerCompletionGate`, the marked dispatch path and the outbox gate; the shape this plan needs is in Design Choice 1 |
| [#2401](https://github.com/dlrivada/Encina/issues/2401) | Blocks PR A and the concurrency scenario | Atomic claim and lease on the 10 providers |
| [#3441](https://github.com/dlrivada/Encina/issues/3441) | Related (`Left` cached permanently) | The reason Design Choice 1 prefers the gate over the orchestrator; revisit when it ships |
| [#3437](https://github.com/dlrivada/Encina/issues/3437) | Related (`needs-decision`) | Inbox key shape; this plan hashes the tenant in, so it does not wait |
| [#736](https://github.com/dlrivada/Encina/issues/736), [#3440](https://github.com/dlrivada/Encina/issues/3440), [#1221](https://github.com/dlrivada/Encina/issues/1221), [#2385](https://github.com/dlrivada/Encina/issues/2385) | Siblings (merged plans) | #2385 publishes integration events through the outbox and relies on #1220's gate; no shared type with this plan |
| [#734](https://github.com/dlrivada/Encina/issues/734) | Not planned here | Webhook idempotency waits for the home decision of [#3388](https://github.com/dlrivada/Encina/issues/3388) |

---

## Next Steps

1. Before #1220's first pull request starts, agree the gate shape of Design Choice 1 with it (one comment on #1220); otherwise PR A takes the extension.
2. At pick-up: re-check the Design Choices against today's code, ask the maintainer Dilemmas 1, 2, 3 and 4 (the hook, the record store, the key, the id source) and record the answers in `## Maintainer Decisions`; then run `check-plan.ps1` without `-Draft`.
3. Confirm the `MessagingNotificationIdempotency` range (7830-7839) is still free when PR A starts.
4. Measure per-flag coverage and the CRAP table for the touched files before each pull request.
