# Implementation Plan: Marten Subscription (Async Daemon) as an Opt-in Delivery Route for Domain Events

> **Issue**: [#2384](https://github.com/dlrivada/Encina/issues/2384)
> **Type**: Feature
> **Related**: #2383 (Outbox route, 1.0; owns the shared dispatch, the inbox hook and the `Encina.Marten` meter), #2239 (decision 2 of 2026-10-16: subscription route post-1.0; decision 5: coordination always on), #2381 and #2382 (module requirements), #207 (PostgreSQL lock provider), #1220 (per-handler completion), ADR-019, ADR-028, ADR-048, ADR-050, ADR-054
> **Complexity**: Medium (one package, one specialized provider: Marten on PostgreSQL)
> **Estimated Scope**: 42 files and about 3,400 changed lines (about 700-1,000 lines of production code, the rest tests, justification and documentation) over 5 pull requests of at most 15 files and 1,500 lines each, listed in "Estimated File Count". Post-1.0; starts only after the #2383 pull requests are merged

---

## Summary

### What exists today (read on 2026-11-11; every fact is re-read on the current `main` when work starts, because this plan starts only after #2383 merges)

- `AddEncinaMarten` (`src/Encina.Marten/ServiceCollectionExtensions.cs:35`) registers the aggregate repository and, while `AutoPublishDomainEvents` is true (default, `EncinaMartenOptions.cs:17`), `EventPublishingPipelineBehavior`, which publishes after the command and stops at the first failure (`EventPublishingPipelineBehavior.cs:139-145`). The #2383 plan removes that behavior and adds the Outbox route (`OutboxStoreMarten`, `MartenOutboxProcessor`, `InboxStoreMarten`, EventIds 2701-2720, `Encina.Marten` meter and `ActivitySource`); none of it is on `main` yet.
- Encina does not use the Marten async daemon: no `AddAsyncDaemon`, `ISubscription` or `SubscriptionBase` in `src/`. Encina applies its store configuration through the bridge `EncinaStoreOptionsConfigurator : IConfigureMarten` (`EncinaStoreOptionsConfigurator.cs`, `AddEncinaMartenStoreOptionsBridge` `:123`).
- Marten 9.38.0 (`Directory.Packages.props`) offers `Marten.Subscriptions.ISubscription.ProcessEventsAsync(EventRange, ISubscriptionController, IDocumentOperations, CancellationToken)`, `SubscriptionBase`, `IEventStoreOptions.Subscribe(ISubscription, Action<ISubscriptionOptions>)` and, on the Marten configuration expression, `AddAsyncDaemon(DaemonMode)` and `AddSubscriptionWithServices<T>(ServiceLifetime, ...)` (read from `Marten.xml` of the package). The documentation of `ISubscription` says the controller records skipped dead-letter events or stops the subscription, and the operations' writes commit with the progress update.
- Event sourcing category: Marten on PostgreSQL only; EventStoreDB is future work. The 10-database rule does not apply (`AGENTS.md` section 5; ADR-019).

### What this plan delivers

`services.AddEncinaMartenSubscription(...)`: a `MartenDomainEventSubscription` registered with the Marten async daemon that translates each committed event into its domain notification, publishes it through the dispatch core of #2383, returns failure to the daemon so the position is not advanced, and marks consumers processed through the same Inbox mark as the Outbox route, written in the page's `IDocumentOperations` so the mark commits with the progress update. The route is exclusive with the Outbox route (registration error).

- Affected packages: `Encina.Marten` (subscription, options, registration, health, logs, coverage manifest; it has no `PublicAPI.Unshipped.txt` on main, so no PublicAPI lines unless #2383 creates one), `Encina.Messaging` (only the startup requirement row, through #2382). No other package or provider.
- Size and split (AGENTS.md section 11): five pull requests in compile order, each with its own tests and manifest entries (per file, per flag, one-sentence justification), and no PR uses a type, log class or project created by a later one (the log class and EventIds are created in PR 2, the first PR that logs). PR 1: options, route switch and the Marten API proof (7 files, about 400 lines). PR 2: subscription, configurator, registration, startup checks, exclusion, log class (14 files, about 1,200 lines). PR 3: inbox mark, tenant flow, failure semantics, justification files (10 files, about 900 lines). PR 4: meter instruments and health check (8 files, about 600 lines). PR 5: documentation (3 files, about 300 lines).
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
| **A) A store-wide route: `EncinaMartenOptions.DomainEventDelivery` is `Outbox` (default) or `Subscription`; with `Subscription` the commit listener and the outbox processor of #2383 are not registered, the module-requirement row "reliable delivery" is met by the subscription, and `AddEncinaMartenSubscription` combined with an explicit `DomainEventDelivery = Outbox` is a startup error naming both (decision 5 says who sets the property)** | One route per store; the requirement rows stay simple; no event is delivered twice by two routes | An application cannot mix routes |
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
<summary><strong>4. Coordination and idempotency: Marten owns election and position, the Inbox mark joins the page</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The daemon runs in `HotCold` mode (one active runner per subscription by Marten's own PostgreSQL advisory lock); the missing daemon call is a startup error naming the piece; consumers use the Inbox mark `{eventId}:{handler}` written through the page's `IDocumentOperations`** | The runner election and the progress row belong to one owner (Marten), so no external lock can fence a stale runner; the mark commits with the position | Tied to Marten's daemon |
| **B) An `IDistributedLockProvider` around the subscription, marks in a separate transaction** | One lock abstraction across Encina | A second lock cannot protect Marten's progress row; marks and position can diverge, so a failure after the mark and before the position republishes an event whose mark says it is done, or the reverse |

### Chosen Option: **A — Marten owns election and position; the Inbox mark joins the page**

### Rationale

- Multi-instance coordination is always on (decision 5 of #2239); the startup-error-naming-the-piece rule is Design Choice 6 of #2383 (no self-registration). Idempotency follows ADR-048's enlisted split (the mark is written in the business transaction, here the page) and the per-handler hook of #2383 (choices 3 and 9); B cannot make the mark and the position atomic, so only A meets ADR-048.

**Class:** Settled
**Settled by:** decision 5 of 2026-10-16 on #2239, ADR-048, ADR-050, Design Choices 3, 6 and 9 of #2383

</details>

<details>
<summary><strong>5. Who sets the route, and the edits to the Outbox route files</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaMartenSubscription` sets `DomainEventDelivery = Subscription` itself, so one call is enough; an explicit `DomainEventDelivery = Outbox` configured by the application next to it is a startup error naming both; the registrations of #2383 (commit listener, outbox processor) and its requirement rows read the property, which is an edit of those files in PR 2 (counted in the file table)** | The common call works; no flag to remember | The property can be set from two places |
| **B) Two steps: the application sets `DomainEventDelivery = Subscription` and calls `AddEncinaMartenSubscription`; either one alone is a startup error** | One explicit setting | The first call the user writes fails at startup |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

A startup error whose text says "with the Outbox route on" would fail the default call (`DomainEventDelivery` defaults to `Outbox`); the maintainer chooses between convenience (A) and explicitness (B).

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Start position of a new subscription</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Default `Current`: a new subscription delivers events committed after it starts; `FromBeginning` and `FromSequence` are explicit** | No replay of history to a new consumer | An application that expects history must ask for it |
| **B) Default `FromBeginning`** | Nothing is missed | A new subscription republishes old events to consumers that may not be idempotent for them |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

Neither option follows from a rule; the default has a consequence for every new consumer, so it is the maintainer's choice.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. <code>Solo</code> daemon mode</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Solo` is always a startup error; tests and development use `HotCold` (which also works with one node)** | No environment detection; one rule | A developer cannot use `Solo` |
| **B) `Solo` is allowed in the Development environment and rejected elsewhere** | Matches Marten's usual development setup | Needs the Development check of #2382, which does not exist yet |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

"Outside tests" has no mechanism in the code today; A needs none and keeps coordination always on (decision 5 of #2239).

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Tenant source, mapping and ordering: the same as the dispatch core</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The tenant comes from the event metadata header `TenantId` that `EventMetadataEnrichmentService` writes when `CaptureTenantId` is on; an event type with no notification mapping, an unknown version and the order of handlers within an event behave exactly as the shared dispatch core of #2383 defines, so both routes act the same** | One behavior to document; provider coherence | The behavior is whatever #2383 decides |
| **B) Route-specific rules in the subscription** | Could differ per route | Two behaviors for the same event |

### Chosen Option: **A**

### Rationale

The header name and the capture option are in `src/Encina.Marten/EventMetadataEnrichmentService.cs` and `EventMetadataOptions.cs`; the dispatch core is reused, not copied (Phase 1 task 2).

**Class:** Trivial
**Settled by:** `src/Encina.Marten/EventMetadataEnrichmentService.cs` (tenant header) and the dispatch core of #2383

</details>

<details>
<summary><strong>9. An event without a tenant in a multi-tenant store</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The page fails (fail closed) and, with decision 3 option A, the position is held, so that event blocks every later one until it is fixed** | Nothing is delivered without a tenant | One tenant-less event blocks the subscription |
| **B) That one event is recorded in `DeadLetterStoreMarten` and the position advances** | The subscription keeps going | An event is skipped; with decision 3 option A it would be the only skipped class |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

AGENTS.md section 3 makes gates fail closed when context is missing; how a missing tenant interacts with the hold of decision 3 is the maintainer's choice, because the block is long lived.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Defaults, retries, health threshold and the number of subscriptions</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Name` = `domain-events`, `BatchSize` = 100, retry and backoff are the Marten daemon's own (no Encina option), the health check is Degraded above `LagThreshold` = 1000 events, and one subscription per store (the name only labels the progress row)** | Few new options; starting points | The numbers have no measurement behind them |
| **B) Several named subscriptions per store, each with its own options** | Separate consumers | Several progress rows, several routes to exclude against the Outbox route |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

The defaults are starting points to be confirmed by the maintainer or replaced after the integration tests measure a page; B grows the exclusion rule of decision 2.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Contract, Options and API Proof (PR 1, 7 files)

> **Goal**: Options, the dispatch seam and a test that proves the Marten 9.38.0 API facts the plan relies on.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Marten/Subscriptions/MartenSubscriptionOptions.cs` (`Name` default `domain-events`, `StartFromPosition` of enum `SubscriptionStart` `Current`, `FromBeginning`, `FromSequence`, in `SubscriptionStart.cs`; `BatchSize`; `LagThreshold`; defaults per decisions 6 and 10; `MaxAttempts` only if Design Choice 3 is B) and `DomainEventDelivery` (Design Choice 2, edit of `EncinaMartenOptions.cs`).
2. **Reuse, do not copy**: the event-to-notification dispatch core extracted by #2383 Phase 3 (publish through `IEncina.Publish`, per-handler invocation hook with the idempotency key). If it is not on `main`, this plan waits (prerequisite).
3. **Create** an integration proof on Marten and PostgreSQL (Testcontainers) of: the exact `ISubscriptionController` members for skip and stop, the `DaemonMode` values, how the configured daemon mode is read back from Marten (needed by the startup check of PR 2), the `ISubscriptionOptions` start-position members and that writes in `IDocumentOperations` commit with the progress update. Names in this plan marked "verified in Phase 1" are corrected there.
4. Tests in the same PR: options unit and guard tests (validated `BatchSize`, `LagThreshold`, `Name`), the API proof; manifest entries for the new files in `.github/coverage-manifest/Encina.Marten.json` with one-sentence justifications (`mechanical-fixer`). No PublicAPI lines unless #2383 creates a file for `Encina.Marten`.

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

### Phase 2: Subscription, Registration, Exclusion and Log Class (PR 2, 14 files)

> **Goal**: The route works and cannot be combined with the Outbox route.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `Subscriptions/MartenDomainEventSubscription.cs` (`SubscriptionBase`): per page, open a DI scope, rebuild the request context (tenant from the event metadata, correlation and cause), dispatch each event in order and return a failure to the daemon on the first `Left` (Design Choice 3).
2. **Create** `Subscriptions/MartenSubscriptionConfigurator.cs : IConfigureMarten` that calls `Events.Subscribe(subscription, options => ...)`, registered with `TryAddEnumerable` next to the existing bridge.
3. **Modify** `ServiceCollectionExtensions.cs`: `AddEncinaMartenSubscription(this IServiceCollection, Action<MartenSubscriptionOptions>)` (decision 5); validate at startup (in `Encina.Marten`, as an `IHostedService` check `MartenSubscriptionStartupCheck` like `DelayedRetryStartupCheck`): the daemon is running in `HotCold` (decision 7, read back as proved in PR 1), the Outbox route is off, an `IEncina` is registered. Each failure names the module, the missing or forbidden piece and ADR-050; when #2382 lands the rows move into its validator.
4. **Modify** the files #2383 creates for the commit listener and outbox processor registration (the `AddEncinaMarten` registration edit is the same `ServiceCollectionExtensions.cs`) and its requirement-row file, so they read `DomainEventDelivery` and "reliable commit-bound delivery" is met by the Outbox or the subscription accordingly; both edits are counted in the file table.
5. **Create** `Diagnostics/MartenSubscriptionLogMessages.cs` with `[LoggerMessage]` EventIds 2721-2730 inside the registered `Marten` range (2600-2799; 2701-2720 belong to #2383; the claim is recorded in the batch registry `artifacts/decisions/plan-batch/eventid-blocks.md`), packed sequentially from 2721 (page failed by code, daemon mode wrong, route conflict, start position, page delivered), and update the range comment in `EventIdRanges.cs`. No new range, so no `AssemblyRanges` edit.
6. Tests in the same PR: unit (subscription page dispatch with a substituted `IEncina` and a fake `TimeProvider`, startup check messages), DI test with `ValidateOnBuild` and `ValidateScopes`, guard (`AddEncinaMartenSubscription`, subscription constructor), contract (the subscription and the Outbox route share the dispatch seam), integration on Marten and PostgreSQL (two daemons in `HotCold` run one active subscription; exclusion with the Outbox route); manifest entries for every new file.

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

### Phase 3: Idempotency, Tenant Flow and Failure Semantics (PR 3, 10 files)

> **Goal**: At-least-once delivery with effectively-once consumers.

<details>
<summary><strong>Tasks</strong></summary>

1. **Implement** the per-handler Inbox mark through `InboxStoreMarten` of #2383 with the page's `IDocumentOperations` as the enlisted session; the key is `{eventId}:{handler}` (an edit of `MartenDomainEventSubscription` and a small `SubscriptionInboxMark` helper).
2. **Implement** the tenant flow: `TenantId` from the event metadata (`EventMetadataOptions`) into the restored request context; an event without a tenant in a multi-tenant store fails the page (decision 9; fail closed).
3. **Implement** Design Choice 3 (hold, or bounded retries and dead letter into `DeadLetterStoreMarten`); new log entries in `MartenSubscriptionLogMessages` (edit).
4. Tests in the same PR: unit (marks, handlers already marked are skipped), property test (FsCheck: the stored position never skips a failed event; a retried page marks each handler at most once; this is the only place the property test is written), integration on Marten and PostgreSQL (a failure holds the position and a later run delivers; tenant flow), guard for the helper; the load and benchmark justification files (`.md`, the daemon owns concurrency; allowed for load tests and benchmarks, never for unit, guard, contract or integration tests); manifest entries.

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

### Phase 4: Cross-Cutting Integration and Observability (PR 4, 8 files)

> **Goal**: Lag, liveness and logs.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `Subscriptions/MartenSubscriptionMetrics.cs` with the instruments on the `Encina.Marten` meter and `ActivitySource` created by #2383 (not a second meter): `encina.marten.subscription.lag` (observable gauge, events behind the high-water mark), `encina.marten.subscription.events_total` (tags outcome code, tenant only with multi-tenancy on) and a publish span per page.
2. **Create** `Subscriptions/MartenSubscriptionHealthCheck : IEncinaHealthCheck` (daemon liveness and lag above `LagThreshold`, decision 10; reports the stuck event id, never its content); register it (edit of `ServiceCollectionExtensions.cs`) and use the metrics from the subscription (edit).
3. The log class and EventIds 2721-2730 were created in PR 2; PR 4 adds no new range.
4. Tests in the same PR: metrics unit, health check unit, guard (health check constructor, validated arguments); manifest entries.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
#2383 creates the Encina.Marten Meter and ActivitySource and EventIds 2701-2720. This phase only adds instruments.

TASK:
1. Add the lag gauge, the events counter, the page span and the health check (PR 4); the logs 2721-2730 packed sequentially already exist from PR 2.

KEY RULES:
- Codes and exception types only in logs and tags; tenant tag only when multi-tenancy is on (ADR-054 D4).
- Health check: DefaultName const, static Tags array, scoped resolution through CreateScope().

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Marten/Health/MartenHealthCheck.cs
```

</details>

---

### Phase 5: Testing (not a separate PR: each PR carries its own tests)

> **Goal**: Every applicable flag reaches its manifest target for `Encina.Marten`, per file, in the PR that adds the file.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit tests**: options validation (PR 1), startup checks (each failure message) and page dispatch with a substituted `IEncina` and a fake `TimeProvider` (PR 2), marks (PR 3), metrics and health (PR 4).
2. **Guard tests** for new public members in every PR (null and range checks of options, `AddEncinaMartenSubscription`, constructors); **contract tests** that the subscription and the Outbox route share the dispatch seam (PR 2).
3. **Property tests** (FsCheck), written once in PR 3: the position never skips a failed event; a retried page marks each handler at most once.
4. **Integration tests** on Marten and PostgreSQL via Testcontainers: API proof (PR 1); two daemons in `HotCold` run one active subscription and the exclusion with the Outbox route (PR 2); failure holds the position and a later run delivers, tenant flow (PR 3).
5. Load and benchmark: justification files (the daemon owns concurrency), in PR 3.
6. Per-file targets with one-sentence justifications in `.github/coverage-manifest/Encina.Marten.json`, entered in the PR that adds the file and its tests (`mechanical-fixer`).

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

### Phase 6: Documentation and Finalization (PR 5, 3 files)

> **Goal**: Users can choose and operate the route.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs with an example ship with each PR (no PublicAPI file exists for `Encina.Marten` on main).
2. PR 5: `changelog.d/2384-marten-subscription-delivery.added.md`; the event-sourcing delivery guide with a section comparing the two routes (`Encina.Marten` has no package README on main); `docs/INVENTORY.md`.
3. The design record: #2383 (decision 16 of its plan) already creates the ADR for the event-sourcing delivery route and an ADR-048 amendment; this plan amends that ADR with a section for the subscription route and, if the maintainer's answers to decisions 2, 3 or 5 change the model, the amendment is part of PR 5 (a fourth file). This plan names no ADR number; the number is the one #2383 assigns.
4. `dotnet build --configuration Release` with zero warnings; `dotnet test` with every flag at target, in every PR.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
The route is implemented and tested. Documentation is written by the docs-writer agent following the encina-docs skill.

TASK:
1. Complete XML docs; write the changelog fragment, the delivery guide section and the INVENTORY entry.

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
| `EventMetadataOptions`, `MartenHealthCheck` | `src/Encina.Marten/EventMetadataOptions.cs`, `src/Encina.Marten/Health/MartenHealthCheck.cs` | Tenant flow; health pattern |
| Outbox route of #2383 (dispatch core, `InboxStoreMarten`, `DeadLetterStoreMarten`, meter) | `src/Encina.Marten/` after #2383 | Shared dispatch and marks |
| `DelayedRetryStartupCheck` | `src/Encina.Messaging/Recoverability/` | Startup check pattern |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Marten` subscription | 2721-2730 inside `Marten` (2600-2799); ids 2721-2725 used | After 2701-2720 of #2383; no new range; recorded in `artifacts/decisions/plan-batch/eventid-blocks.md` |

### Estimated File Count

Every added or edited file is counted, including manifests, the registry comment edit and the edits of the files #2383 creates; no existing signature changes, so no existing test caller changes.

| PR | Category | Files | Notes |
|----|----------|-------|-------|
| 1 | Source | 3 | `MartenSubscriptionOptions`, `SubscriptionStart`, `EncinaMartenOptions` edit |
| 1 | Tests | 3 | Options unit, options guard, API proof (integration) |
| 1 | Manifest | 1 | `Encina.Marten.json` |
| 1 | **Subtotal** | **7** | |
| 2 | Source | 7 | `MartenDomainEventSubscription`, `MartenSubscriptionConfigurator`, `MartenSubscriptionStartupCheck`, `ServiceCollectionExtensions` edit, requirement-row file edit (#2383), `MartenSubscriptionLogMessages`, `EventIdRanges.cs` comment edit |
| 2 | Tests | 6 | Subscription unit, startup check unit, DI, guard, contract, integration |
| 2 | Manifest | 1 | `Encina.Marten.json` |
| 2 | **Subtotal** | **14** | |
| 3 | Source | 3 | `SubscriptionInboxMark`, subscription edit, log messages edit |
| 3 | Tests | 4 | Unit, property, integration, guard |
| 3 | Justification | 2 | Load and benchmark `.md` |
| 3 | Manifest | 1 | `Encina.Marten.json` |
| 3 | **Subtotal** | **10** | |
| 4 | Source | 4 | `MartenSubscriptionMetrics`, `MartenSubscriptionHealthCheck`, `ServiceCollectionExtensions` edit, subscription edit |
| 4 | Tests | 3 | Metrics unit, health unit, guard |
| 4 | Manifest | 1 | `Encina.Marten.json` |
| 4 | **Subtotal** | **8** | |
| 5 | Docs | 2 | Delivery guide section, `docs/INVENTORY.md` |
| 5 | Changelog | 1 | `changelog.d/2384-marten-subscription-delivery.added.md` |
| 5 | **Subtotal** | **3** | A fourth file, the ADR amendment, only if the answers change the model |
| | **Total** | **42** | 7 + 14 + 10 + 8 + 3; each PR at most 15 files and 1,500 lines |

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
1. Options, route switch and Marten API proof (PR 1); 2. MartenDomainEventSubscription, IConfigureMarten registration,
start-up checks, exclusion with the Outbox route and EventIds 2721-2730 (PR 2); 3. inbox mark in the page operations,
tenant flow, failure semantics (PR 3); 4. lag gauge, health check (PR 4); 5. docs (PR 5). Each PR carries its own tests
and manifest entries.

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
| 11 | Module Isolation | ❌ N/A | Marten has no module-scoped streams or subscriptions today, so there is nothing to scope (#747 is about `ModuleId` on messaging entities, not subscriptions) |
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

1. Link this plan from #2384 and ask the maintainer to answer the Dilemmas (Design Choices 2, 3, 5, 6, 7, 9 and 10) when work starts (after #2383 merges).
2. Run the Phase 1 API proof first and correct any Marten member name in this plan.
3. Implement Phases 1 to 6 as five pull requests in the order of the table, `Fixes #2384` on the last.
