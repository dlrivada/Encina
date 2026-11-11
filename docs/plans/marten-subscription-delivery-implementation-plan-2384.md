# Implementation Plan: Marten Subscription (Async Daemon) as an Opt-in Delivery Route for Domain Events

> **Issue**: [#2384](https://github.com/dlrivada/Encina/issues/2384)
> **Type**: Feature
> **Related**: #2383 (Outbox route, 1.0; owns the shared dispatch, the inbox hook and the `Encina.Marten` meter), #2239 (decision 2 of 2026-10-16: subscription route post-1.0; decision 5: coordination always on), #2381 and #2382 (module requirements), #207 (PostgreSQL lock provider), #1220 (per-handler completion), ADR-019, ADR-028, ADR-048, ADR-050, ADR-054
> **Complexity**: Medium (one package, one specialized provider: Marten on PostgreSQL)
> **Estimated Scope**: ~700-1,000 lines of production code + ~1,500 lines of tests. Post-1.0; starts only after the #2383 pull requests are merged

---

## Summary

### What exists today (read on 2026-11-11, main `68d08ddae`)

- `AddEncinaMarten` (`src/Encina.Marten/ServiceCollectionExtensions.cs:35`) registers the aggregate repository and, while `AutoPublishDomainEvents` is true (default, `EncinaMartenOptions.cs:17`), `EventPublishingPipelineBehavior`, which publishes after the command and stops at the first failure (`EventPublishingPipelineBehavior.cs:139-145`). The #2383 plan removes that behavior and adds the Outbox route (`OutboxStoreMarten`, `MartenOutboxProcessor`, `InboxStoreMarten`, EventIds 2701-2720, `Encina.Marten` meter and `ActivitySource`); none of it is on `main` yet.
- Encina does not use the Marten async daemon: no `AddAsyncDaemon`, `ISubscription` or `SubscriptionBase` in `src/`. Encina applies its store configuration through the bridge `EncinaStoreOptionsConfigurator : IConfigureMarten` (`EncinaStoreOptionsConfigurator.cs`, `AddEncinaMartenStoreOptionsBridge` `:123`).
- Marten 9.38.0 (`Directory.Packages.props`) offers `Marten.Subscriptions.ISubscription.ProcessEventsAsync(EventRange, ISubscriptionController, IDocumentOperations, CancellationToken)`, `SubscriptionBase`, `IEventStoreOptions.Subscribe(ISubscription, Action<ISubscriptionOptions>)` and, on the Marten configuration expression, `AddAsyncDaemon(DaemonMode)` and `AddSubscriptionWithServices<T>(ServiceLifetime, ...)` (read from `Marten.xml` of the package). The documentation of `ISubscription` says the controller records skipped dead-letter events or stops the subscription, and the operations' writes commit with the progress update.
- Event sourcing category: Marten on PostgreSQL only; EventStoreDB is future work. The 10-database rule does not apply (`AGENTS.md` section 5; ADR-019).

### What this plan delivers

`services.AddEncinaMartenSubscription(...)`: a `MartenDomainEventSubscription` registered with the Marten async daemon that translates each committed event into its domain notification, publishes it through the dispatch core of #2383, returns failure to the daemon so the position is not advanced, and marks consumers processed through the same Inbox mark as the Outbox route, written in the page's `IDocumentOperations` so the mark commits with the progress update. The route is exclusive with the Outbox route (registration error).

- Affected packages: `Encina.Marten` (subscription, options, registration, health, logs), `Encina.Messaging` (only the startup requirement row, through #2382). No other package or provider.
- Provider category: Event sourcing (Marten). EventStoreDB: not applicable.

---

## Design Choices

<details>
<summary><strong>1. The delivery route and the alternatives that stay rejected</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A Marten async daemon subscription, opt-in, post-1.0, next to the Outbox route** | Native position tracking, no second write per event; resumes after a failure | Needs the daemon and PostgreSQL advisory locks; failure semantics differ from the Outbox DLQ |
| **B) Keep the Outbox only** | One route to test and document | The double write per event; contradicts decision 2 of #2239 |
| **C) A projection that raises notifications** | Reuses the projection engine | Projections are not delivery guarantees; a rebuild would re-publish |

### Chosen Option: **A — Async daemon subscription**

### Rationale

- Recorded on #2239 (decision 2, 2026-10-16) and in the comment of 2026-10-17 on #2384; B and C are rejected there. Marten subscriptions and the Wolverine/Marten integration deliver from the event store itself.

**Class:** Settled
**Settled by:** decision 2 of 2026-10-16 on #2239, the comment of 2026-10-17 on #2384, ADR-019 and ADR-028

</details>

<details>
<summary><strong>2. How exclusive the two routes are</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A store-wide route: `EncinaMartenOptions.DomainEventDelivery` is `Outbox` (default) or `Subscription`; with `Subscription` the commit listener and the outbox processor of #2383 are not registered, the module-requirement row "reliable delivery" is met by the subscription, and `AddEncinaMartenSubscription` with the Outbox route on is a startup error naming both** | One route per store; the requirement rows stay simple; no event is delivered twice by two routes | An application cannot mix routes |
| **B) A partition per notification type or assembly (each handler set picks one route)** | Lets one application use both | The listener must know the partition; an event with handlers in both partitions needs two deliveries; harder to prove no loss |

### Chosen Option: **A — One store-wide route** (recommended, pending the maintainer)

### Rationale

- The issue asks for a registration error for "the same handler set"; a store-wide switch is the simplest rule that makes that statement true by construction (#2381 option A).
- B wins when a migrating application must move handler groups to the subscription one at a time.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. What a persistently failing handler does to the subscription</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Hold the position: failure goes back to the daemon, which retries with backoff and does not advance; the health check reports the lag and the stuck event** | Order kept (compliance events carry legal deadlines); nothing skipped silently; matches the acceptance criterion | One poison event blocks every later event of the subscription until fixed |
| **B) Bounded retries (`MaxAttempts`), then record the event in `DeadLetterStoreMarten` (#2383) through the controller's dead-letter facility and advance** | One poison event does not stop delivery; visible in the dead letter queue and the admin API (#2229) | Skips an event, so per-stream order is broken for that consumer; the replay must be correct |

### Chosen Option: **A — Hold the position** (recommended, pending the maintainer)

### Rationale

- `AGENTS.md` section 3 requires a failure to fail the operation visibly (retry or dead letter); A is the retry form and keeps the acceptance criterion "delivered at least once, position held".
- B wins when availability of later events matters more than order for the consumer. It can be added later as an option without changing A's default.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Coordination, idempotency, tenant and start position</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The daemon runs in `HotCold` mode (one active runner per subscription by Marten's own PostgreSQL advisory lock); the missing daemon call is a startup error naming the piece; consumers use the Inbox mark `{eventId}:{handler}` written through the page's `IDocumentOperations`; the tenant comes from the event metadata; the start position defaults to `Current`** | The runner election and the progress row belong to one owner (Marten), so no external lock can fence a stale runner; the mark commits with the position; no replay of history to a new subscription | `FromBeginning` must be asked for explicitly |
| **B) An `IDistributedLockProvider` around the subscription, marks in a separate transaction, history replayed from the start** | One lock abstraction across Encina | A second lock cannot protect Marten's progress row; marks and position can diverge; republishes old events to new consumers |

### Chosen Option: **A — Marten owns election and position; the Inbox mark joins the page**

### Rationale

- Multi-instance coordination is always on (decision 5 of #2239); the startup-error-naming-the-piece rule is Design Choice 6 of #2383 (no self-registration). Idempotency follows ADR-048's enlisted split and the per-handler hook of #2383 (choices 3 and 9). `Solo` mode is rejected at startup outside tests.

**Class:** Settled
**Settled by:** decision 5 of 2026-10-16 on #2239, ADR-048, ADR-050, Design Choices 3, 6 and 9 of #2383, Marten async daemon `HotCold` mode (comparable product)

</details>

---

## Implementation Phases

### Phase 1: Contract, Options and API Proof

> **Goal**: Options, the dispatch seam and a test that proves the Marten 9.38.0 API facts the plan relies on.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Marten/Subscriptions/MartenSubscriptionOptions.cs` (`Name` default `domain-events`, `StartFromPosition` of enum `SubscriptionStart` `Current`, `FromBeginning`, `FromSequence`; `BatchSize`; `MaxAttempts` only if Design Choice 3 is B) and `DomainEventDelivery` (Design Choice 2) on `EncinaMartenOptions`.
2. **Reuse, do not copy**: the event-to-notification dispatch core extracted by #2383 Phase 3 (publish through `IEncina.Publish`, per-handler invocation hook with the idempotency key). If it is not on `main`, this plan waits (prerequisite).
3. **Create** an integration proof on Marten and PostgreSQL (Testcontainers) of: the exact `ISubscriptionController` members for skip and stop, the `DaemonMode` values, the `ISubscriptionOptions` start-position members and that writes in `IDocumentOperations` commit with the progress update. Names in this plan marked "verified in Phase 1" are corrected there.
4. **Modify** `PublicAPI.Unshipped.txt` of `Encina.Marten` (through `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no backward compatibility). Issue #2384 adds a Marten async daemon subscription
as an opt-in delivery route, after the Outbox route of #2383 is merged. Apply the maintainer's answers to Design
Choices 2 and 3 of docs/plans/marten-subscription-delivery-implementation-plan-2384.md.

TASK:
1. Add MartenSubscriptionOptions and the DomainEventDelivery switch.
2. Write the Marten API proof test (controller members, DaemonMode values, start options, commit with progress).
3. Wire the dispatch seam from #2383; do not duplicate its code.

KEY RULES:
- Event-sourced modules have no InMemory stores; integration tests use Marten on PostgreSQL via Testcontainers.
- XML docs on every public member; no [Obsolete]; Either<EncinaError, T> for fallible Encina code.

REFERENCE FILES:
- src/Encina.Marten/EncinaMartenOptions.cs, ServiceCollectionExtensions.cs, EncinaStoreOptionsConfigurator.cs
- docs/plans/es-reliable-delivery-implementation-plan-2383.md
```

</details>

---

### Phase 2: Subscription, Registration and Exclusion

> **Goal**: The route works and cannot be combined with the Outbox route.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `Subscriptions/MartenDomainEventSubscription.cs` (`SubscriptionBase`): per page, open a DI scope, rebuild the request context (tenant from the event metadata, correlation and cause), dispatch each event in order and return a failure to the daemon on the first `Left` (Design Choice 3).
2. **Create** `Subscriptions/MartenSubscriptionConfigurator.cs : IConfigureMarten` that calls `Events.Subscribe(subscription, options => ...)`, registered with `TryAddEnumerable` next to the existing bridge.
3. **Modify** `ServiceCollectionExtensions.cs`: `AddEncinaMartenSubscription(this IServiceCollection, Action<MartenSubscriptionOptions>)`; validate at startup (in `Encina.Marten`, as an `IHostedService` check like `DelayedRetryStartupCheck`): the daemon is running in `HotCold`, the Outbox route is off, an `IEncina` is registered. Each failure names the module, the missing or forbidden piece and ADR-050; when #2382 lands the rows move into its validator.
4. **Modify** the #2383 requirement rows so "reliable commit-bound delivery" is met by the Outbox or the subscription according to `DomainEventDelivery`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Phase 1 proved the Marten APIs. Encina applies Marten configuration through IConfigureMarten
(EncinaStoreOptionsConfigurator). Start-up checks follow src/Encina.Messaging/Recoverability/DelayedRetryStartupCheck.cs.

TASK:
1. Implement MartenDomainEventSubscription and its IConfigureMarten registration.
2. Add AddEncinaMartenSubscription and the start-up checks (daemon HotCold, Outbox route off, IEncina present).
3. Update the requirement rows of #2383 for the two routes.

KEY RULES:
- A Left from the dispatch or from the inbox mark fails the page: the position is not advanced.
- Fail closed: a missing piece is a start-up error, never a silent fallback. Register with TryAdd semantics.

REFERENCE FILES:
- src/Encina.Marten/ServiceCollectionExtensions.cs (:123 bridge), EncinaStoreOptionsConfigurator.cs
- src/Encina.Messaging/Recoverability/DelayedRetryStartupCheck.cs
```

</details>

---

### Phase 3: Idempotency, Tenant Flow and Failure Semantics

> **Goal**: At-least-once delivery with effectively-once consumers.

<details>
<summary><strong>Tasks</strong></summary>

1. **Implement** the per-handler Inbox mark through `InboxStoreMarten` of #2383 with the page's `IDocumentOperations` as the enlisted session; the key is `{eventId}:{handler}`.
2. **Implement** the tenant flow: `TenantId` from the event metadata (`EventMetadataOptions`) into the restored request context; an event without a tenant in a multi-tenant store fails the page (fail closed).
3. **Implement** Design Choice 3 (hold, or bounded retries and dead letter into `DeadLetterStoreMarten`).
4. **Create** a property test: the stored position never skips a failed event.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
The subscription publishes events. InboxStoreMarten, the per-handler hook and DeadLetterStoreMarten come from #2383.

TASK:
1. Write the inbox mark in the page operations; skip handlers already marked.
2. Restore tenant and correlation from event metadata; fail the page when a multi-tenant event has no tenant.
3. Apply the failure semantics answered in Design Choice 3.

KEY RULES:
- Errors are never swallowed in background infrastructure (AGENTS.md section 3).
- EncinaError.Message never reaches logs, tags or storage; exceptions through ForLogging().

REFERENCE FILES:
- src/Encina.Marten/EventMetadataOptions.cs, docs/plans/es-reliable-delivery-implementation-plan-2383.md (choices 3, 9, 12)
```

</details>

---

### Phase 4: Cross-Cutting Integration and Observability

> **Goal**: Lag, liveness and logs.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** instruments on the `Encina.Marten` meter and `ActivitySource` created by #2383 (not a second meter): `encina.marten.subscription.lag` (observable gauge, events behind the high-water mark), `encina.marten.subscription.events_total` (tags outcome code, tenant only with multi-tenancy on) and a publish span per page.
2. **Create** `Subscriptions/MartenSubscriptionHealthCheck : IEncinaHealthCheck` (daemon liveness and lag above a threshold; reports the stuck event id, never its content).
3. **Create** `Diagnostics/MartenSubscriptionLogMessages.cs` with `[LoggerMessage]` EventIds 2721-2730 inside the registered `Marten` range (2600-2799; 2701-2720 belong to #2383); update the range comment in `EventIdRanges.cs`. No new range.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
#2383 creates the Encina.Marten Meter and ActivitySource and EventIds 2701-2720. This phase only adds instruments.

TASK:
1. Add the lag gauge, the events counter, the page span and the health check; add logs 2721-2730 packed sequentially.

KEY RULES:
- Codes and exception types only in logs and tags; tenant tag only when multi-tenancy is on (ADR-054 D4).
- Health check: DefaultName const, static Tags array, scoped resolution through CreateScope().

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Marten/MartenHealthCheck.cs
```

</details>

---

### Phase 5: Testing

> **Goal**: Every applicable flag reaches its manifest target for `Encina.Marten`.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit tests**: options validation, startup checks (each failure message), page dispatch with a substituted `IEncina` and a fake `TimeProvider`.
2. **Guard tests** for new public members; **contract tests** that the subscription and the Outbox route share the dispatch seam.
3. **Property tests** (FsCheck): the position never skips a failed event; a retried page marks each handler at most once.
4. **Integration tests** on Marten and PostgreSQL via Testcontainers: failure holds the position and a later run delivers; two daemons in `HotCold` run one active subscription; tenant flow; the exclusion with the Outbox route.
5. Load and benchmark: justification files (the daemon owns concurrency).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Per-flag coverage (AGENTS.md section 9) for Encina.Marten; targets in .github/coverage-manifest/Encina.Marten.json.

TASK:
1. Write the tests above with real implementations; measure per-flag coverage and CRAP with coverage-report.cs.

KEY RULES:
- Shouldly through Encina.Testing.Shouldly; Marten integration through the shared Testcontainers fixture.
- Keep at least one test that registers the subscription with a real store (no IServiceProvider in projections).

REFERENCE FILES:
- tests/Encina.IntegrationTests (Marten collections), .github/scripts/coverage-report.cs
```

</details>

---

### Phase 6: Documentation and Finalization

> **Goal**: Users can choose and operate the route.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs with an example; `PublicAPI.Unshipped.txt`.
2. `changelog.d/2384-marten-subscription-delivery.added.md`; `Encina.Marten` README section comparing the two routes; event-sourcing delivery guide; `docs/INVENTORY.md`.
3. ADR only if the maintainer's answer to Design Choice 2 or 3 changes the delivery model of the #2383 ADR (number assigned when the ADR pull request opens).
4. `dotnet build --configuration Release` with zero warnings; `dotnet test` with every flag at target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
The route is implemented and tested. Documentation is written by the docs-writer agent following the encina-docs skill.

TASK:
1. Complete XML docs and PublicAPI; write the changelog fragment, README section and the delivery guide.

KEY RULES:
- English only; never edit CHANGELOG.md; no hand-typed coverage numbers.

REFERENCE FILES:
- changelog.d/README.md, .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| #2239 decisions 2 and 5 | Subscription route post-1.0; coordination always on | Choices 1 and 4 |
| ADR-019, ADR-028 | Marten for event-sourced modules; domain events | Provider category and event model |
| ADR-048 | Inbox record versus business transaction | Enlisted idempotency mark |
| ADR-050 | Modules may require modules | Startup rows |
| Marten async daemon (`HotCold`), Wolverine Marten integration | Subscriptions deliver from the event store | Comparable products |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `AddEncinaMarten`, `EncinaStoreOptionsConfigurator` | `src/Encina.Marten/` | Registration bridge |
| `EventMetadataOptions`, `MartenHealthCheck` | `src/Encina.Marten/` | Tenant flow; health pattern |
| Outbox route of #2383 (dispatch core, `InboxStoreMarten`, `DeadLetterStoreMarten`, meter) | `src/Encina.Marten/` after #2383 | Shared dispatch and marks |
| `DelayedRetryStartupCheck` | `src/Encina.Messaging/Recoverability/` | Startup check pattern |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Marten` subscription | 2721-2730 inside `Marten` (2600-2799) | After 2701-2720 of #2383; no new range |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New production files | ~7 | Options, subscription, configurator, startup check, health check, log messages |
| Changed production files | ~5 | Registration, options, requirement rows, `EventIdRanges.cs` comment, PublicAPI |
| Tests | ~10 | Unit, guard, contract, property, integration, justification files |
| Documentation | ~5 | README, guide, INVENTORY, changelog |
| **Total** | **~27** | Two pull requests: code and tests, then documentation |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #2384 - a Marten async daemon subscription as an opt-in domain event delivery route.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Event sourcing category: Marten on PostgreSQL only. Railway Oriented Programming: a Left fails the page.
- Prerequisite: the Outbox route of #2383 is merged (shared dispatch, inbox hook, meter, dead letter store).

IMPLEMENTATION OVERVIEW:
1. Options, route switch and Marten API proof; 2. MartenDomainEventSubscription, IConfigureMarten registration,
start-up checks and exclusion with the Outbox route; 3. inbox mark in the page operations, tenant flow, failure
semantics; 4. lag gauge, health check, EventIds 2721-2730; 5. tests; 6. docs.

KEY PATTERNS:
- HotCold daemon; position never advances past a failed event; marks commit with the progress update.
- Codes only in logs and tags; TimeProvider for time; tests on Marten via Testcontainers.

REFERENCE FILES:
- src/Encina.Marten/ServiceCollectionExtensions.cs
- docs/plans/es-reliable-delivery-implementation-plan-2383.md
- docs/plans/marten-subscription-delivery-implementation-plan-2384.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | The subscription reads the event stream in order |
| 2 | OpenTelemetry | ✅ Phase 4 | Lag gauge, events counter, publish span on the `Encina.Marten` meter of #2383 |
| 3 | Structured Logging | ✅ Phase 4 | EventIds 2721-2730 inside the registered `Marten` range |
| 4 | Health Checks | ✅ Phase 4 | Daemon liveness and lag |
| 5 | Validation | ✅ Phase 2 | Options and start-up checks fail closed |
| 6 | Resilience | ✅ Phase 3 | The daemon retries with backoff; the failure semantics follow Design Choice 3 |
| 7 | Distributed Locks | ❌ N/A | Marten's `HotCold` advisory lock elects the runner; an `IDistributedLockProvider` cannot fence the progress row (Design Choice 4) |
| 8 | Transactions | ✅ Phase 3 | The inbox mark commits with the progress update in the page operations |
| 9 | Idempotency | ✅ Phase 3 | Inbox mark per event id and handler, shared with the Outbox route |
| 10 | Multi-Tenancy | ✅ Phase 3 | Tenant from the event metadata; a multi-tenant event without a tenant fails the page |
| 11 | Module Isolation | ⏭️ #747 | Module-scoped subscriptions follow the module isolation issue |
| 12 | Audit Trail | ❌ N/A | Delivery is not an auditable operator action; the events are the audit source |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Outbox route for Marten (#2383) | Open (plan on `main`) | Shared dispatch, inbox hook, meter, dead letter store, startup rows |
| Module requirement validator (#2382) | Open | The startup rows move into it; a local hosted-service check is used until then |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| PostgreSQL lock provider | #207 | Needed by #2383 for purge work, not by the daemon |
| Dead letter admin API | #2229 | Operates on rows written by Design Choice 3 option B, if chosen |

---

## Next Steps

1. Link this plan from #2384 and ask the maintainer to answer Design Choices 2 and 3 when work starts (after #2383 merges).
2. Run the Phase 1 API proof first and correct any Marten member name in this plan.
3. Implement Phases 1 to 6 as two pull requests, `Fixes #2384` on the last.
