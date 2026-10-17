# Implementation Plan: Event-Sourcing Reliable Delivery — `OutboxStoreMarten` in the Marten Session, Inbox Idempotency for Domain Events, Mandatory Module Set

> **Issue**: [#2383](https://github.com/dlrivada/Encina/issues/2383)
> **Type**: Feature
> **Complexity**: High (9 phases, one specialized provider (Marten on PostgreSQL), ~105 files touched, created or removed)
> **Estimated Scope**: ~2,200-3,000 lines of production code + ~3,000-3,800 lines of tests
> **Base**: `origin/main` at `3808c2888` (2026-10-16). Every `file:line` below was read at that commit.

---

## Summary

Deliver the domain events of Marten event-sourced aggregates **reliably**: every event whose stream commits is also written, in the **same Marten session and the same PostgreSQL transaction**, as an outbox document (`OutboxStoreMarten`); a background processor publishes those documents with at-least-once semantics, retries with backoff, dead-letters what cannot be delivered, and coordinates several instances; consumers are made idempotent by the **existing Inbox, extended to domain events** (one processed mark per event id and handler, enlisted in the Marten transaction as ADR-048 defines). In-process publication after the command (`AutoPublishDomainEvents`, `EventPublishingPipelineBehavior`) is **removed**. Applications that use the event-sourcing route get the required modules registered or fail at startup with a clear message; `TransactionPipelineBehavior` together with Marten is a startup error.

### Why (the defect of #2239, verified on `main`)

1. The repository commits inside the handler: `_session.Events.Append(...)` then `await _session.SaveChangesAsync(...)` (`src/Encina.Marten/MartenAggregateRepository.cs:206`, `:208`).
2. A session listener records the committed events in memory (`src/Encina.Marten/CommittedEventCollector.cs:21-25`).
3. After the command, `EventPublishingPipelineBehavior` publishes them one by one and **stops at the first failure** (`src/Encina.Marten/EventPublishingPipelineBehavior.cs:139-145`), returning `Left` for a change that already committed (`:109`).
4. It is registered only when `AutoPublishDomainEvents` is true (`src/Encina.Marten/ServiceCollectionExtensions.cs:65-72`), which is the default (`src/Encina.Marten/EncinaMartenOptions.cs:17`).

A crash between steps 1 and 3, or one failing handler, loses the remaining events for good, with no record. Compliance modules depend on these events for legal deadlines (GDPR Art. 33: 72 hours; Art. 12(3): one month).

**Three further gaps, found by the 2026-10-16 study and re-verified at `3808c2888`; this plan closes them and tests them:**

- **The behavior wraps only commands** (`EventPublishingPipelineBehavior.cs:16`, `where TRequest : ICommand<TResponse>`). `BreachDetectionPipelineBehavior` runs on any `IRequest` (`src/Encina.Compliance.BreachNotification/BreachDetectionPipelineBehavior.cs:61-62`) and records breaches through `_breachService.RecordBreachAsync(...)` (`:215`). On a query, `BreachDetected` is committed but never published.
- **Background services write outside the pipeline.** `RetentionEnforcementService` resolves `IRetentionRecordService` from its own scope (`src/Encina.Compliance.Retention/RetentionEnforcementService.cs:133-135`) and changes records (`:451`, `:454`) without `IEncina.Send`; their events are never published.
- **No outbox can share the Marten transaction today.** There is no Marten outbox store and no reference to Outbox or Inbox in `src/Encina.Marten*`. `OutboxStoreADO.AddAsync` (PostgreSQL) builds its command without a transaction (`src/Encina.ADO.PostgreSQL/Outbox/OutboxStoreADO.cs:112-126`), and its pending read takes no row lock (`:52-59`). `OutboxPostProcessor` discards the `Either` of `AddAsync` and `SaveChangesAsync` (`src/Encina.Messaging/Outbox/OutboxPostProcessor.cs:92`, `:95`; removed by the #718 plan). The processor publishes only through `IEncina.Publish` (`src/Encina.Messaging/Outbox/OutboxProcessorBase.cs:170-184`).

A commit-bound outbox written by a **store-wide Marten session listener** closes all three, because it acts at `SaveChanges` and not in the request pipeline: any session that commits events, whether opened by a command, a query, a pipeline behavior or a background service, writes their outbox documents in the same transaction.

### What this plan implements

| # | Deliverable | Where |
| --- | --- | --- |
| 1 | `OutboxStoreMarten : IOutboxStore` (Marten documents) and `MartenOutboxSessionListener` (before `SaveChanges`, store-wide) | `Encina.Marten`, new `Outbox/` folder |
| 2 | `MartenOutboxProcessor : OutboxProcessorBase`, claiming rows with `FOR UPDATE SKIP LOCKED` under the #1251 claim contract, retry, DLQ | `Encina.Marten/Outbox/`, small hooks in `Encina.Messaging` |
| 3 | `InboxStoreMarten : IInboxStore` with the ADR-048 enlisted/independent split, and the per-handler idempotency hook in the notification dispatch | `Encina.Marten/Inbox/`, `Encina` core (hook) |
| 4 | `DeadLetterStoreMarten : IDeadLetterStore` (if Design Choice 12 = A) | `Encina.Marten/DeadLetter/` |
| 5 | Removal of `EventPublishingPipelineBehavior`, `CommittedEventCollector`, `AutoPublishDomainEvents`, `MartenErrorCodes.PublishEventsFailed` and their logs | `Encina.Marten` |
| 6 | Startup requirements of the ES route: required (Marten, outbox, inbox, DLQ store, non-test lock provider), forbidden (`TransactionPipelineBehavior`), conditional (tenancy, scheduling) | `Encina.Marten`, the 9 `Encina.Compliance.*` `Add*Aggregates` |
| 7 | Observability: `ActivitySource` and `Meter` named `Encina.Marten`, `[LoggerMessage]` in 2701-2740, health check of the Marten outbox | `Encina.Marten/Diagnostics/` |

**Affected packages**: `Encina.Marten` (main), `Encina.Messaging` (processor dispatch hook, keyed store resolution), `Encina` core (per-handler notification hook, shared with #1220), `Encina.Marten.GDPR` (crypto-shredding of loaded events on dispatch: tests only), the 9 compliance packages with `Add*Aggregates` (`BreachNotification`, `DataSubjectRights`, `DataResidency`, `LawfulBasis`, `CrossBorderTransfer`, `Consent`, `DPIA`, `Retention`, `ProcessorAgreements`: comments, registration requirements, tests), `Encina.OpenTelemetry` (instrumented store decorators if the keyed stores need them), `Encina.Testing` (outbox drain helper for tests).

**Provider category**: **Event sourcing (Marten on PostgreSQL)**, a specialized category outside the 10-database rule (AGENTS.md §5, "Message brokers, caching and event sourcing (Marten) are excluded from the database rule"; ADR-019 "The Specialized Provider Analogy", `docs/architecture/adr/019-compliance-event-sourcing-marten.md:29-31`). `OutboxStoreMarten`, `InboxStoreMarten` and `DeadLetterStoreMarten` follow `{Pattern}Store{Provider}` (AGENTS.md §4) and implement the same `Encina.Messaging` interfaces as the 10 database providers, so provider coherence holds: switching the store is a DI change. The 10 database providers are **unchanged** by this plan; their row claiming is #1251, their transactional outbox #718/#719, their per-handler completion #1220. Distributed locks: the 1.0 set of 5 (SPEC-000 DEC-003); a Marten deployment needs the PostgreSQL provider (#207). Transports: none touched; domain-event outbox rows must never reach a transport (#2385, ADR-028).

**SPEC-000**: 1.0 item. The compliance modules of SPEC-002 rely on these events for legal deadlines.

---

## Design Choices

Choices 1-6 were decided by the maintainer on 2026-10-16 (comment on #2239) and are recorded in `## Maintainer Decisions`. Choices 7-16 are settled on 2026-10-17 by elimination (no remaining dilemma); each shows the chosen option, its forcing rule and the settling reason, and is recorded in `## Maintainer Decisions` (entries 7-16).

<details>
<summary><strong>1. Module requirements — a module may require other modules (ADR-050, #2381; startup validation #2382)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A module may require other modules; closed list per module in an ADR** | The ES guarantees become true by construction; one rule for every module; enforced at startup | Amends AGENTS.md §3 ("NEVER make a pattern mandatory"); the list must be maintained |
| **B) Exception only for event sourcing** | Smallest change to the rule | Same need appears in other modules (Outbox needs Transactions, compliance gates need Audit); a second exception follows |
| **C) Keep everything opt-in; #2239 goes through its option C (DLQ, at-most-once)** | No rule change | The ES compliance modules lose legal obligations silently when the app does not opt in |

### Chosen Option: **A — modules may require other modules, closed list in an ADR** (decided by the maintainer, 2026-10-16)

### Rationale

- The ADR is #2381 (ADR-050 by the next-free-number rule; 037, 043 and 049 are reserved, `docs/architecture/adr/index.md:53-68`). The validator mechanism is #2382.
- The ES route's rows: **required** reliable commit-bound delivery (Outbox), consumer idempotency (Inbox), recoverability + DLQ, processor health/metrics, multi-instance coordination; **forbidden** `TransactionPipelineBehavior`; **conditional** multi-tenancy (multi-tenant apps) and scheduling (modules with deadlines).

</details>

<details>
<summary><strong>2. Delivery mechanism — Outbox with <code>OutboxStoreMarten</code> written in the same Marten session (listener before <code>SaveChanges</code>)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In-process publication after the command (today)** | No new infrastructure | Loses events on a handler `Left` or a crash; misses queries and background services (#2239) |
| **B) Marten subscription / async daemon with checkpoint** | Native position tracking; no second write | Daemon, HA leader election and checkpoint management to build; larger change for 1.0 |
| **C) Outbox: `OutboxStoreMarten` written in the same session by a listener before `SaveChanges`** | At-least-once tied to the commit; reuses the Outbox processor, retry, DLQ and metrics; consistent with ADR-028 | A second write per event; a Marten store and processor to maintain |

### Chosen Option: **C — Outbox with `OutboxStoreMarten` in the same Marten session/transaction** (decided by the maintainer, 2026-10-16)

### Rationale

- The subscription route is post-1.0 and opt-in (#2384). `AutoPublishDomainEvents` and the in-process publication are removed; option C of #2239 is dropped.
- A store-wide listener (registered on `StoreOptions.Listeners` through the existing `IConfigureMarten` bridge, `src/Encina.Marten/ServiceCollectionExtensions.cs:123-130`) runs for every session of the store, so queries and background services are covered too.

</details>

<details>
<summary><strong>3. Consumer idempotency — the existing Inbox extended to domain events, enlisted in the Marten transaction (ADR-048)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A bespoke Marten-document "processed" mark** | Simple; lives next to the events | A second idempotency mechanism beside the Inbox; no ADR-048 failure semantics (retry count, cached `Left`) |
| **B) The existing Inbox, extended to domain events (mark per event id and handler), enlisted per ADR-048** | One idempotency model; `MaxRetries` and cached handler errors survive rollback as ADR-048 requires | Needs an `InboxStoreMarten` and a per-handler hook in the dispatch |

### Chosen Option: **B — the existing Inbox extended to domain events, enlisted per ADR-048; the Marten-document mark only if enlisting proves infeasible** (decided by the maintainer, 2026-10-16)

### Rationale

- `InboxStoreMarten` implements `IInboxStore` (`src/Encina.Messaging/Inbox/IInboxStore.cs:23-120`): the enlisted writes (`MarkAsProcessedAsync`, reads) go through the scoped `IDocumentSession`, so they commit with the handler's own Marten writes; the independent writes (`AddAsync`, `MarkAsFailedAsync`, `CacheHandlerErrorAsync`) use a separate lightweight session so they survive a rollback. This adds a Marten row to ADR-048's "Mechanisms per family" table.
- Phase 1 proves enlisting with a real-PostgreSQL test before anything else is built; only if that test cannot pass does the fallback (A) come back to the maintainer.

</details>

<details>
<summary><strong>4. <code>TransactionPipelineBehavior</code> together with Marten — startup error</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Startup error (fail-fast) explaining that the Marten session is the unit of work** | The false guarantee never runs; one clear message | Applications mixing EF/Dapper data and Marten must wait for #1225 |
| **B) Log a warning** | Nothing breaks | The ADO/EF transaction opens a second connection and does not cover Marten: a false guarantee in production |
| **C) Documentation only** | No code | Same false guarantee, invisible |

### Chosen Option: **A — startup error** (decided by the maintainer, 2026-10-16)

### Rationale

- The forbidden types are `Encina.Messaging.TransactionPipelineBehavior<,>` and `Encina.EntityFrameworkCore.TransactionPipelineBehavior<,>` (and the `TransactionalOutboxPipelineBehavior<,>` of the #718 plan when it lands). Cross-store atomicity stays with #1225.

</details>

<details>
<summary><strong>5. Multi-instance coordination — always on: <code>FOR UPDATE SKIP LOCKED</code> claims and a distributed lock for single-runner work</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Always on** | No configuration mistake can duplicate deliveries; same code in one or many instances | A lock provider must be registered even for one instance |
| **B) Only when the application declares more than one instance** | Fewer requirements for small hosts | A scale-out without the declaration duplicates legal notifications (#763 shows the pattern) |

### Chosen Option: **A — always on** (decided by the maintainer, 2026-10-16)

### Rationale

- The claim follows the #1251 contract (ADR-043 reserved: claim methods, lease columns `ClaimedBy`, `ClaimToken`, `ClaimedUntilUtc`, fenced completion; `docs/plans/processor-row-claiming-implementation-plan-1251.md:71-130`).
- Single-runner work (purge of processed outbox and inbox documents, requeue sweeps) takes `IDistributedLockProvider` (`src/Encina.DistributedLock/IDistributedLockProvider.cs`).

</details>

<details>
<summary><strong>6. Compliance <code>Add*Aggregates</code> — they do not register Marten; the application fails at startup when Marten or a required module is missing</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each `Add*Aggregates` calls `AddEncinaMarten` itself** | One call less for the application | Hides the store configuration; nine places that may configure Marten differently |
| **B) They do not; startup fails with a clear message when `AddEncinaMarten` or a required module is missing** | One place configures Marten; the failure names what is missing | One more line in the host |

### Chosen Option: **B — no self-registration; startup error naming the missing piece** (decided by the maintainer, 2026-10-16)

### Rationale

- The nine methods today register only the repository and the projection (example: `src/Encina.Compliance.BreachNotification/BreachNotificationMartenExtensions.cs:52-53`). Each declares the ES requirement rows instead.

</details>

<details>
<summary><strong>7. Outbox payload — a reference to the committed Marten event, loaded at dispatch</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Copy: serialize the event into `Content` through `IMessageSerializer` (as `OutboxPostProcessor` does, `OutboxPostProcessor.cs:84`)** | Same shape as the 10 providers; dispatch needs no event load | A second copy of personal data outside the stream: crypto-shredding (`Encina.Marten.GDPR`) does not reach it, so an erased subject's data stays in the outbox until purge (GDPR Art. 17); doubles storage |
| **B) Reference: the document holds the Marten event id, stream id and sequence; the processor loads the event with `session.Events.LoadAsync(id)` at dispatch** | One copy of the data; crypto-shredding applies to the loaded event; small documents; the outbox id can be the event id | One extra read per dispatch; `IOutboxMessage.Content` carries the reference, so the shared batch processor needs a materialization hook |
| **C) Copy, encrypted with the subject key** | Shredding the key also hides the copy | Couples the outbox to the crypto-shredding key model; extra crypto work per write |

### Chosen Option: **B — reference to the committed event** (settled 2026-10-17)

Settled 2026-10-17 by elimination: a plain copy leaves personal data that crypto-shredding cannot reach (GDPR Art. 17, ADR-019 compliance modules); C couples the outbox to the key model for no gain; the reference keeps one copy.

### Rationale

- We recommend B because the compliance events carry personal data and erasure must reach every copy; with B there is no copy.
- The outbox document id is the Marten event id (`IEvent.Id`), which makes the per-handler Inbox key (`{eventId}:{handler}`) stable across retries.
- The load also applies the upcasters of `EventVersioning` (`src/Encina.Marten/Versioning/`), so a handler always receives the current event shape.

</details>

<details>
<summary><strong>8. Outbox storage and claim — a Marten document with duplicated claim columns and one raw-SQL claim statement</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Marten document `MartenOutboxMessage` stored with `session.Store(...)`; claim and filter fields duplicated as real columns (`Schema.For<T>().Duplicate(...)`); claim by one `UPDATE ... WHERE id IN (SELECT id ... FOR UPDATE SKIP LOCKED LIMIT @n) RETURNING data` on the document table** | Pure Marten write path in the session; schema managed by Marten; indexes on the duplicated columns | Raw SQL against Marten's table naming (`mt_doc_*`) for the claim |
| **B) A relational table written through `session.QueueSqlCommand(...)`** | Same table shape as `OutboxStoreADO` (PostgreSQL); plain SQL claim | Encina must create and migrate the table outside Marten's schema management; two schema owners in one database |
| **C) Reuse `OutboxStoreADO`/`OutboxStoreDapper` (PostgreSQL) enlisted in the Marten transaction** | No new store | Requires the #718 accessor to see Marten's connection; ties #2239 to #1225 (option A of that spike); the ES route would require an ADO/Dapper package |

### Chosen Option: **A — Marten document with duplicated claim columns** (settled 2026-10-17)

Settled 2026-10-17 by elimination: Choice 2 fixes "same Marten session"; B creates two schema owners; C ties #2239 to #1225 and forces a relational package on ES apps; Marten owns its schema (as Wolverine's Marten outbox does).

### Rationale

- We recommend A because Marten owns the schema (`AutoCreateSchemaObjects`, migrations) and the write is a normal `Store` in the same unit of work, which is the decided mechanism.
- The claim statement is the only raw SQL; it reads the table name from Marten's mapping (`store.Options.Storage.MappingFor(typeof(MartenOutboxMessage))`), never a literal.

</details>

<details>
<summary><strong>9. Where the per-handler Inbox check runs — a core per-handler invocation hook, shared with #1220</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A core hook around each notification handler invocation (`INotificationHandlerInvocationFilter`), active when the dispatch carries an idempotency key; the Marten processor sets the key to the outbox id** | One mechanism for #1220 (10 providers) and this plan; handlers stay unaware; works with every dispatch strategy (`src/Encina/Dispatchers/Encina.NotificationDispatcher.cs:79-95`) | Changes core dispatch; must be sequenced with #1220 |
| **B) The Marten processor resolves the handlers and invokes each itself** | No core change | Duplicates the dispatcher (strategies, logging, tracing); #1220 builds a second copy |
| **C) Decorate every `INotificationHandler<T>` registration** | No dispatcher change | Decoration of open generics at registration is fragile; misses handlers added later |

### Chosen Option: **A — core per-handler invocation hook, shared with #1220** (settled 2026-10-17)

Settled 2026-10-17 by elimination: B duplicates the dispatcher; C is fragile on open generics; #1220 needs the identical hook, so one mechanism is the only non-duplicating path (AGENTS.md §5 provider coherence).

### Rationale

- We recommend A because #1220 needs exactly this hook ("a handler that already succeeded is skipped when the message is retried, keyed by outbox id and handler type"); building it once avoids two dispatch paths.
- Whichever of #1220 and this plan lands first builds the hook; the other implements its store side only.

</details>

<details>
<summary><strong>10. Ordering — per-stream order: one in-flight message per stream</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No ordering guarantee (claim oldest first across all streams)** | Highest throughput; simplest claim | Two instances may dispatch `BreachNotifiedToAuthority` before `BreachDetected` of the same breach; handlers must cope |
| **B) Per-stream order: the claim takes only the oldest unprocessed message of each stream; a stream's next message waits until the previous one is processed or dead-lettered** | Handlers see a stream's events in commit order, which compliance handlers assume | A poisoned head blocks its stream until it is dead-lettered (bounded by `MaxRetries`); slightly more complex claim SQL (`DISTINCT ON (stream_id)`) |
| **C) Partitioned sequential messaging (#469)** | General solution | Not built; a larger feature |

### Chosen Option: **B — per-stream order** (settled 2026-10-17)

Settled 2026-10-17 by elimination: Axon, EventStoreDB and the Marten daemon preserve stream order; A would break the state-machine assumptions of the compliance handlers; head-of-line blocking is bounded by `MaxRetries` and then the DLQ (Choice 1 makes the DLQ mandatory); C is a feature not built.

### Rationale

- We recommend B because the compliance aggregates are state machines and their handlers react to transitions in order; a dead-lettered head releases its stream, so blocking is bounded.

</details>

<details>
<summary><strong>11. Dispatch latency — after-commit nudge of the processor</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Polling only (`OutboxOptions.ProcessingInterval`, default 30 s, `src/Encina.Messaging/Outbox/OutboxOptions.cs`)** | No extra mechanism | Handlers run up to 30 s after the command; tests and users notice the change from in-process publication |
| **B) Polling plus an in-process nudge: the listener's `AfterCommitAsync` signals the local processor (a `Channel`) to run a cycle now** | Near-immediate delivery on the instance that committed; polling still recovers everything after a crash | A small signal path to test |
| **C) PostgreSQL `LISTEN/NOTIFY`** | Wakes every instance | Connection held per instance; more infrastructure |

### Chosen Option: **B — polling plus after-commit nudge** (settled 2026-10-17)

Settled 2026-10-17 by elimination: Wolverine and MassTransit bus outbox dispatch right after commit and poll as recovery; durability is unchanged; C adds a held connection per instance; A regresses today's immediate publication.

### Rationale

- We recommend B because the nudge is a latency optimization only: durability comes from the document, and the poll recovers anything the nudge misses.

</details>

<details>
<summary><strong>12. Dead-letter store for a Marten-only application — <code>DeadLetterStoreMarten</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `DeadLetterStoreMarten : IDeadLetterStore` (Marten documents)** | A Marten-only app needs no relational package; capture and the outbox mark can commit in one session | An 11th implementation of the DLQ contract to keep coherent (ADR-046) |
| **B) Require a relational DLQ store (ADO/Dapper/EF PostgreSQL) on the same database** | No new store | Forces a second data-access stack on every ES app |

### Chosen Option: **A — `DeadLetterStoreMarten`** (settled 2026-10-17)

Settled 2026-10-17 by elimination: Choice 1 makes the DLQ mandatory and Choice 6 says Marten is configured in one place; B forces a second data-access stack on every ES app (Marten is outside the 10-database rule, AGENTS.md §5); the ADR-046 contract tests keep coherence.

### Rationale

- We recommend A because the DLQ is mandatory with ES (Choice 1) and the ES route should not require a second data-access stack. The store must pass the same DLQ contract tests as the 10 providers (oldest-first order, idempotent capture by source message, tenant column; ADR-046).

</details>

<details>
<summary><strong>13. Lock for single-runner work — <code>IDistributedLockProvider</code>, with the PostgreSQL provider (#207) as a prerequisite</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IDistributedLockProvider`; the ES route requires a non-test provider; #207 (PostgreSQL `pg_advisory_lock`) lands first so a Marten-only app can use its own database** | One lock abstraction across Encina (AGENTS.md §5); the 1.0 lock set covers it | Depends on #207 |
| **B) A Marten-internal advisory lock (`pg_try_advisory_lock` on the session connection)** | No dependency | A second lock mechanism outside the lock category; not reusable |
| **C) No lock: rely on fenced claims for everything** | Nothing to build | Purge and requeue sweeps run concurrently on every instance |

### Chosen Option: **A — `IDistributedLockProvider` with #207 first** (settled 2026-10-17)

Settled 2026-10-17 by elimination: AGENTS.md §5 and SPEC-000 DEC-003 fix the lock category (the PostgreSQL provider is one of the 5); Choice 5 already names the distributed lock; B is a second lock mechanism; C leaves purge and requeue sweeps concurrent.

### Rationale

- We recommend A because Choice 5 names the distributed lock and the lock category is fixed for 1.0 (SPEC-000 DEC-003). Dispatch itself needs no lock: the fenced claim covers it.

</details>

<details>
<summary><strong>14. Registration API and package — <code>AddEncinaMarten</code> registers the mandatory set itself, in <code>Encina.Marten</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaMarten` registers the outbox, inbox, DLQ store, listener and processor (auto-registration with `TryAdd*`, the application can replace a store); options under `EncinaMartenOptions.Outbox` and `.Inbox`** | Nothing mandatory can be forgotten; matches #2381 option A ("auto-register an internal dependency with no alternatives") | `AddEncinaMarten` grows |
| **B) Separate `AddEncinaMartenOutbox()` / `AddEncinaMartenInbox()`, enforced by startup validation** | Explicit host code | Two more lines that are always required; a check that exists only to say "call this" |
| **C) A new package `Encina.Marten.Messaging`** | Keeps `Encina.Marten` small | `Encina.Marten` already references `Encina.Messaging` (`src/Encina.Marten/Encina.Marten.csproj`); a package that every ES app must add anyway |

### Chosen Option: **A — `AddEncinaMarten` registers the mandatory set, in `Encina.Marten`** (settled 2026-10-17)

Settled 2026-10-17 by elimination: Choice 1 = A on #2381 (auto-registration with `TryAdd`, no alternatives); B is the "call this" validation that decision rejects; C adds a package every ES app needs anyway. The keyed `IOutboxStore` is an implementation detail.

### Rationale

- We recommend A because these components have no alternative inside the ES route; registering them is the safe default and startup validation is kept for what has several providers (the lock).
- The outbox store is registered as a **keyed** `IOutboxStore` (key `"marten"`) so it coexists with a relational outbox registered by an EF Core or Dapper package in the same host; `MartenOutboxProcessor` overrides `ResolveOutboxStore` (`OutboxProcessorBase.cs:118-119`) to take the keyed store.

</details>

<details>
<summary><strong>15. Startup validation — through <code>IEncinaModuleRequirements</code> (#2382), which lands first</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) #2382 lands first; this plan declares its rows (required, forbidden, conditional) through `IEncinaModuleRequirements`** | One failure format; the docs table is generated from the rows | This plan's Phase 5 waits for #2382 |
| **B) A Marten-local hosted service now (as `CryptoShreddingStartupValidationHostedService`, `src/Encina.Marten.GDPR/CryptoShreddingStartupValidationHostedService.cs:36`), replaced when #2382 lands** | No wait | Throwaway code; two failure formats for a while |

### Chosen Option: **A — declare the rows through #2382** (settled 2026-10-17)

Settled 2026-10-17 by elimination: AGENTS.md §3 forbids transitional code and compatibility layers; B is throwaway code; phase ordering is sequencing, not a decision.

### Rationale

- We recommend A because pre-1.0 we build the final mechanism once (AGENTS.md §3, no transitional code). Phases 1-4 do not depend on #2382; only Phase 5 does.

</details>

<details>
<summary><strong>16. Where the design is recorded — a new ADR for the ES delivery route plus an ADR-048 amendment; ADR-019/ADR-028 updated by #2381</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new ADR (the next free number after #2381's 050, reserved in this plan's PR) "Event-sourced domain events are delivered through a commit-bound Marten outbox"; ADR-048 gains a Marten row; #2381 updates ADR-019 (`:122`, publication by the behavior) and ADR-028 (`:13`, domain events dispatched in-process) and cites the new ADR** | Each ADR keeps one decision; the delivery design (listener, reference payload, per-stream order, keyed store) has a home | One more ADR |
| **B) Everything inside #2381's ADR-050 and its ADR-019/028 updates; only ADR-048 amended here** | Fewer documents | ADR-050 is about the module-requirements rule; mixing the delivery design into it blurs both |
| **C) Amend ADR-019 directly with the delivery design** | One ES document | ADR-019 is about compliance aggregates on Marten; the delivery design applies to every Marten aggregate |

### Chosen Option: **A — new ADR + ADR-048 amendment; ADR-019/028 via #2381** (settled 2026-10-17)

Settled 2026-10-17 by elimination: AGENTS.md §8 requires an ADR for architectural decisions; one decision per ADR; B blurs ADR-050 (module requirements); C scopes the delivery design to compliance aggregates only.

### Rationale

- We recommend A because the decision is architectural (new pattern on a specialized provider) and future contributors need it in one place. #2381's scope already includes "Updating ADR-019 and ADR-028 for the event-sourcing route decided in #2239".
- The reservation row goes into `docs/architecture/adr/index.md` "Reserved numbers" in the plan PR, after #2381 reserves 050.

</details>

---

## Implementation Phases

### Phase 1: Core Models, Options and the Enlisting Proof

> **Goal**: Prove on real PostgreSQL that the listener can store documents in the committing session and read event ids, then define the document models, options and error codes every later phase uses.

<details>
<summary><strong>Tasks</strong></summary>

#### Feasibility proof (first, before any production type)

1. **Integration test** `tests/Encina.IntegrationTests/Infrastructure/Marten/Outbox/MartenListenerEnlistingProofTests.cs` (`[Collection(MartenCollection.Name)]`, `[Trait("Category", "Integration")]`, `[Trait("Database", "PostgreSQL")]`):
   - A store-wide `DocumentSessionListenerBase` registered on `StoreOptions.Listeners` stores a document in `BeforeSaveChangesAsync(IDocumentSession, CancellationToken)` for each pending event of `session.PendingChanges.Streams()`; after `SaveChangesAsync` both the event and the document exist.
   - A forced failure after the document is stored (concurrency conflict on the stream) leaves **neither** the event nor the document.
   - `IEvent.Id` is non-empty and equal before and after the commit.
   - Lightweight and identity sessions both run the store-wide listener.
   - If any assertion cannot pass on Marten 9.38.0 (`Directory.Packages.props`), stop and report to the orchestrator: Design Choices 2, 3 and 8 return to the maintainer.

#### Models (`src/Encina.Marten/Outbox/`, `src/Encina.Marten/Inbox/`)

2. `MartenOutboxMessage` — `public sealed class` in `Encina.Marten.Outbox`, implements `IOutboxMessage` (`src/Encina.Messaging/Outbox/IOutboxMessage.cs:21-72`):
   - `Id (Guid)` = Marten event id; `NotificationType (string)`; `Content (string)` = reference JSON `{ "eventId", "streamId", "streamKey", "sequence" }` (Choice 7 = B); `CreatedAtUtc`, `ProcessedAtUtc?`, `ErrorMessage?`, `RetryCount`, `NextRetryAtUtc?`.
   - ES-specific: `StreamId (Guid)`, `StreamSequence (long)`, `TenantId (string?)`, `CorrelationId (string?)`, `CausationId (string?)`.
   - Claim fields per #1251: `ClaimedBy (string?)`, `ClaimToken (Guid?)`, `ClaimedUntilUtc (DateTime?)`.
   - `IsProcessed`, `IsDeadLettered(int maxRetries)` as in the relational entities.
3. `MartenInboxMessage` — implements `IInboxMessage` (`src/Encina.Messaging/Inbox/IInboxMessage.cs:21-85`); `MessageId` = `"{eventId:N}:{handlerTypeFullName}"`; `RequestType` = notification type name; `TenantId (string?)`.
4. `MartenOutboxDocumentConfiguration : IConfigureMarten` (internal) — maps both documents, duplicates `ProcessedAtUtc`, `RetryCount`, `NextRetryAtUtc`, `ClaimedUntilUtc`, `StreamId`, `StreamSequence`, `TenantId` as columns, adds the claim index `(processed_at_utc, stream_id, stream_sequence)`.

#### Options and errors

5. `MartenOutboxOptions` — `public sealed class` in `Encina.Marten.Outbox`: wraps `OutboxOptions` values used by the processor (`ProcessingInterval`, `BatchSize`, `MaxRetries`, `BaseRetryDelay`), `NudgeOnCommit (bool, default true)` (Choice 11), `LeaseDuration (TimeSpan, 5 min)`, `LeaseSafetyMargin (TimeSpan, 30 s)` (aligned with #1251), `PurgeProcessedAfter (TimeSpan?)` (#1200 alignment); `Validate()` throws `ArgumentOutOfRangeException` naming the property.
6. `MartenInboxOptions` — `MessageRetention (TimeSpan)`, `MaxRetries (int)`; `Validate()`.
7. `EncinaMartenOptions` (`src/Encina.Marten/EncinaMartenOptions.cs`): add `Outbox` and `Inbox` getter-only properties; **delete** `AutoPublishDomainEvents` (`:17`).
8. `MartenErrorCodes` (`src/Encina.Marten/MartenErrorCodes.cs`): add `marten.outbox.add_failed`, `marten.outbox.claim_failed`, `marten.outbox.claim_lost`, `marten.outbox.event_not_found`, `marten.inbox.write_failed`, `marten.requirements.transaction_behavior_forbidden`; **delete** `PublishEventsFailed`.
9. `PublicAPI.Unshipped.txt` of `Encina.Marten`: add the new symbols; remove `AutoPublishDomainEvents` and `PublishEventsFailed` lines (RS0017).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of the event-sourcing reliable delivery feature (Issue #2383) in Encina.

CONTEXT:
- Encina is a .NET 10 / C# 14 library; Railway Oriented Programming with Either<EncinaError, T>; pre-1.0, no backward compatibility, no [Obsolete].
- Marten 9.38.0 on PostgreSQL is the event-sourcing provider (ADR-027, ADR-019); it is outside the 10-database-provider rule.
- Today Encina.Marten publishes domain events in process after the command (src/Encina.Marten/EventPublishingPipelineBehavior.cs), which loses events (#2239). The decided replacement is an outbox document written in the same Marten session by a store-wide listener before SaveChanges.
- Read the plan docs/plans/es-reliable-delivery-implementation-plan-2383.md (Summary, Design Choices 2, 3, 7, 8) first.

TASK:
1. Write MartenListenerEnlistingProofTests (integration, real PostgreSQL through MartenFixture) proving: a document stored in BeforeSaveChangesAsync commits atomically with the appended events; a concurrency failure leaves neither; IEvent.Id is stable; lightweight and identity sessions both run a store-wide listener. If it cannot pass, STOP and report.
2. Create MartenOutboxMessage, MartenInboxMessage, MartenOutboxDocumentConfiguration, MartenOutboxOptions, MartenInboxOptions as listed in Phase 1 Tasks.
3. Add Outbox and Inbox options to EncinaMartenOptions; delete AutoPublishDomainEvents; add the new error codes and delete PublishEventsFailed.
4. Update PublicAPI.Unshipped.txt.

KEY RULES:
- Timestamps are DateTime UTC with the AtUtc suffix; time always from TimeProvider, never DateTime.UtcNow.
- Naming per AGENTS.md section 4: NotificationType/RequestType, ErrorMessage, RetryCount, NextRetryAtUtc.
- The outbox document holds a reference to the event (event id, stream id, sequence), never a copy of the event data.
- Integration tests use [Collection(MartenCollection.Name)], [Trait("Category","Integration")], [Trait("Database","PostgreSQL")], skip when the fixture is unavailable; Shouldly assertions; never dispose the fixture.
- XML documentation on every public type and member; zero warnings.
- Scripts in PowerShell only; no python, no bash constructs.

REFERENCE FILES:
- src/Encina.Marten/CommittedEventCollector.cs (existing listener on DocumentSessionListenerBase)
- src/Encina.Marten/MartenAggregateRepository.cs:172-221 (append and SaveChangesAsync)
- src/Encina.Marten/ServiceCollectionExtensions.cs:108-130 (IConfigureMarten bridge)
- src/Encina.Messaging/Outbox/IOutboxMessage.cs, src/Encina.Messaging/Inbox/IInboxMessage.cs
- tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs
- docs/plans/processor-row-claiming-implementation-plan-1251.md (claim fields)
```

</details>

---

### Phase 2: `OutboxStoreMarten` and the Commit-Bound Listener

> **Goal**: Every committed `INotification` event of every session writes exactly one outbox document in the same transaction; the store implements `IOutboxStore` with the #1251 claim contract.

<details>
<summary><strong>Tasks</strong></summary>

1. `MartenOutboxSessionListener` — `internal sealed class` in `Encina.Marten.Outbox`, derives `DocumentSessionListenerBase`:
   - `BeforeSaveChangesAsync(IDocumentSession session, CancellationToken token)`: for each `StreamAction` in `session.PendingChanges.Streams()` and each `IEvent` whose `Data is INotification`, `session.Store(new MartenOutboxMessage { Id = e.Id, StreamId = stream.Id, StreamSequence = e.Version, NotificationType = ..., Content = reference, CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime, TenantId/CorrelationId/CausationId from the event headers set by EventMetadataEnrichmentService (src/Encina.Marten/EventMetadataEnrichmentService.cs:96-104) })`.
   - `AfterCommitAsync(...)`: signals `MartenOutboxNudge` when `NudgeOnCommit` (Choice 11); never throws.
   - Dependencies: `TimeProvider`, `MartenOutboxNudge`, `IOptions<EncinaMartenOptions>`.
   - Registered store-wide through an `IConfigureMarten` (`StoreOptions.Listeners.Add`), so sessions opened by queries, behaviors and background services are covered (the gaps of `BreachDetectionPipelineBehavior.cs:61-62,215` and `RetentionEnforcementService.cs:133-135,451,454`).
2. `OutboxStoreMarten` — `public sealed class` in `Encina.Marten.Outbox`, implements `IOutboxStore` (`src/Encina.Messaging/Outbox/IOutboxStore.cs:23-128`) and the claim methods #1251 adds:
   - Constructor: `IDocumentSession session`, `IDocumentStore store`, `TimeProvider? timeProvider = null`.
   - `AddAsync` (explicit, for application-raised integration events in the same session) → `session.Store(...)`; returns `Left(marten.outbox.add_failed)` on exception.
   - `ClaimPendingMessagesAsync(MessageClaimRequest request, CancellationToken)` (#1251 shape) → one statement on the mapped document table: `UPDATE ... SET claimed_by, claim_token, claimed_until_utc WHERE id IN (SELECT DISTINCT ON (stream_id) id ... WHERE processed_at_utc IS NULL AND retry_count < @max AND (next_retry_at_utc IS NULL OR next_retry_at_utc <= @now) AND (claimed_until_utc IS NULL OR claimed_until_utc < @now) AND <head of stream: no earlier unprocessed sequence> ORDER BY stream_id, stream_sequence FOR UPDATE SKIP LOCKED LIMIT @n) RETURNING data` (Choices 8, 10). Async only, with `CancellationToken`.
   - `MarkAsProcessedAsync(Guid id, Guid claimToken, ...)`, `MarkAsFailedAsync(Guid id, Guid claimToken, string errorMessage, DateTime? nextRetryAtUtc, ...)`: fenced by the claim token; a lost claim returns `Left(marten.outbox.claim_lost)`.
   - `GetPendingCountAsync`, `GetExhaustedCountAsync`, `RequeueExhaustedAsync` (clears the claim columns), `SaveChangesAsync` → `session.SaveChangesAsync`.
   - `ErrorMessage` stores only the error code (AGENTS.md §3).
3. If #1251 has not landed when this phase starts, the claim methods follow its plan's Design Choices 1-3 as decided by then; record the dependency in the PR.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of Issue #2383: OutboxStoreMarten and the commit-bound session listener in Encina.Marten.

CONTEXT:
- Phase 1 added MartenOutboxMessage (document holding a reference to the Marten event), options and error codes, and proved that a store-wide listener can store documents in the committing session.
- The listener must run for EVERY session of the store: commands, queries (BreachDetectionPipelineBehavior records breaches on any IRequest) and background services (RetentionEnforcementService writes outside the pipeline).
- Multi-instance coordination is always on: claims use FOR UPDATE SKIP LOCKED with a lease and fenced completion, as the #1251 plan defines.
- Per-stream order: claim only the head (lowest unprocessed sequence) of each stream.

TASK:
1. Implement MartenOutboxSessionListener (BeforeSaveChangesAsync writes one MartenOutboxMessage per committed INotification event; AfterCommitAsync nudges the processor) and register it store-wide via IConfigureMarten.
2. Implement OutboxStoreMarten : IOutboxStore with the #1251 claim methods, fenced completion and counts, using one raw SQL claim statement on the Marten-mapped table (table name from Marten's mapping, never a literal).
3. Unit tests for every branch; integration tests on MartenFixture: a command, a query and a background service each produce outbox documents; a failed commit produces none.

KEY RULES:
- Every store method returns Task<Either<EncinaError, T>> and never throws for business outcomes; exceptions become Left with a marten.outbox.* code.
- Async database calls with CancellationToken only; never synchronous overloads.
- ErrorMessage, logs and tags never contain EncinaError.Message; only the error code.
- Time from TimeProvider (constructor parameter defaulting to TimeProvider.System).
- CRAP <= 10 on every method you add (measure locally with the crap-gate table).

REFERENCE FILES:
- src/Encina.Messaging/Outbox/IOutboxStore.cs
- src/Encina.ADO.PostgreSQL/Outbox/OutboxStoreADO.cs (relational store shape)
- docs/plans/processor-row-claiming-implementation-plan-1251.md (claim contract, lease, fencing)
- src/Encina.Marten/EventMetadataEnrichmentService.cs:96-104 (TenantId and other headers)
- src/Encina.Compliance.BreachNotification/BreachDetectionPipelineBehavior.cs, src/Encina.Compliance.Retention/RetentionEnforcementService.cs
```

</details>

---

### Phase 3: Processor, Dispatch by Reference and Dead-Letter

> **Goal**: Publish claimed outbox documents by loading the referenced event, restoring the request context, retrying with backoff and dead-lettering exhausted messages.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Encina.Messaging` hooks** (shared with #2385, which adds the transport route to the same processor):
   - `OutboxProcessorBase` (`src/Encina.Messaging/Outbox/OutboxProcessorBase.cs`): add `protected virtual ValueTask<Either<EncinaError, Unit>> DispatchAsync(IServiceProvider scopedServices, IOutboxMessage message, CancellationToken cancellationToken)`; the default keeps today's deserialize-and-`Publish` path (`:132-137`, `:170-184`). `OutboxBatchProcessor` (`src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs:89`) takes the dispatch delegate instead of a deserialized notification where needed.
   - Domain-event rows are **never** routed to a transport publisher (#2385, ADR-028): the Marten processor's override always dispatches in process.
2. `MartenOutboxProcessor` — `internal sealed class` in `Encina.Marten.Outbox`, derives `OutboxProcessorBase`:
   - `ResolveOutboxStore` → keyed `IOutboxStore` `"marten"` (Choice 14).
   - `DispatchAsync`: parse the reference; `session.Events.LoadAsync(eventId)` (upcasters and crypto-shredding apply); `Left(marten.outbox.event_not_found)` when absent; build `IRequestContext` with `CorrelationId`, `CausationId` = event id, `TenantId`, `IdempotencyKey` = outbox id (#1220 convention); `IEncina.Publish(notification, context)`.
   - Waits on `MartenOutboxNudge` or `ProcessingInterval`, whichever comes first.
   - Dependencies: `IServiceProvider`, `ILogger<MartenOutboxProcessor>`, `IOptions<EncinaMartenOptions>`, `MartenOutboxNudge`, `TimeProvider?`.
3. `MartenOutboxNudge` — `internal sealed class`, a bounded `Channel<bool>` (capacity 1, drop-write when full).
4. **Dead-letter** (Choice 12 = A): `DeadLetterStoreMarten : IDeadLetterStore` in `Encina.Marten.DeadLetter`, documents `MartenDeadLetterMessage`; passes the shared DLQ contract tests (ADR-046: oldest-first, idempotent capture by source message, tenant column). Exhausted outbox messages are captured through `DeadLetterSourceCapture` (`src/Encina.Messaging/DeadLetter/DeadLetterSourceCapture.cs:35`), which the batch processor already calls (`OutboxProcessorBase.cs:129-133`).
5. Recoverability: errors are classified by `DeadLetterSourceCapture.IsRetryable` (ADR-029); a non-retryable error dead-letters at once.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of Issue #2383: the Marten outbox processor and the dead-letter store.

CONTEXT:
- OutboxStoreMarten (Phase 2) holds documents that REFERENCE a committed Marten event (event id, stream id, sequence); the processor loads the event at dispatch so upcasters and crypto-shredding apply.
- OutboxProcessorBase in Encina.Messaging publishes deserialized notifications through IEncina.Publish and captures exhausted messages into the dead letter queue.
- Domain events are dispatched in process only; they never go to a transport (ADR-028; #2385 adds the transport route for integration events).

TASK:
1. Add a protected virtual DispatchAsync hook to OutboxProcessorBase (default = today's behavior) and adapt OutboxBatchProcessor; keep every existing test green.
2. Implement MartenOutboxProcessor (keyed "marten" store, dispatch by reference, request context restored with CorrelationId, CausationId, TenantId and IdempotencyKey = outbox id) and MartenOutboxNudge.
3. Implement DeadLetterStoreMarten : IDeadLetterStore and make it pass the existing DLQ contract tests.
4. Unit tests (mocked session and IEncina), integration tests on MartenFixture (publish, retry, exhaustion to DLQ, event not found).

KEY RULES:
- A Left from IEncina.Publish fails the message (retry, then dead-letter); it is never reported as success (AGENTS.md section 3, background infrastructure).
- Logs and activity tags carry the error code only, never EncinaError.Message; exceptions logged through ForLogging().
- The processor stops releasing its claims on cancellation; outcomes are saved with CancellationToken.None as OutboxProcessorBase does today.
- Time from TimeProvider.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxProcessorBase.cs, OutboxBatchProcessor.cs, OutboxRetryBackoff.cs
- src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs, DeadLetterSourceCapture.cs
- docs/architecture/adr/046-persistent-dead-letter-queue.md, 029-recoverability-error-classification.md
- src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs (document-store DLQ shape)
```

</details>

---

### Phase 4: Inbox for Domain Events

> **Goal**: A handler that already processed an event is skipped on redelivery; its processed mark commits with its own Marten writes; failure records survive a rollback.

<details>
<summary><strong>Tasks</strong></summary>

1. **Core hook** (Choice 9 = A; skip if #1220 already built it): `INotificationHandlerInvocationFilter` — `public interface` in `Encina`:
   - `ValueTask<Either<EncinaError, Unit>> InvokeAsync(NotificationHandlerInvocation invocation, Func<ValueTask<Either<EncinaError, Unit>>> next, CancellationToken cancellationToken)`; `NotificationHandlerInvocation` record: `HandlerType`, `Notification`, `IRequestContext Context`.
   - Called by `NotificationDispatcher` around `ExecuteHandlerAsync` (`src/Encina/Dispatchers/Encina.NotificationDispatcher.cs:82-94`) only when `context.IdempotencyKey` is set; resolved per scope; works with the three dispatch strategies.
2. `InboxStoreMarten` — `public sealed class` in `Encina.Marten.Inbox`, implements `IInboxStore`:
   - Enlisted (scoped `IDocumentSession`): `GetMessageAsync`, `MarkAsProcessedAsync`, `GetExpiredMessagesAsync`, `RemoveExpiredMessagesAsync`.
   - Independent (`store.LightweightSession()` per call, committed at once): `AddAsync`, `MarkAsFailedAsync` (the only place `RetryCount` grows), `CacheHandlerErrorAsync`.
   - `MarkAsProcessedAsync` stores the mark in the session; a session listener (`MartenInboxSessionListener`, `BeforeSaveChangesAsync`) makes sure a mark pending for the current invocation joins the handler's commit; when the handler wrote nothing to Marten, the filter commits the mark itself after `Right` (documented window: a crash between the handler's external side effect and the mark re-runs the handler, as ADR-048 states for unwrapped requests).
3. `MartenDomainEventInboxFilter` — `internal sealed class`, implements `INotificationHandlerInvocationFilter`; drives `InboxOrchestrator` semantics per ADR-048: `Right` → `MarkAsProcessedAsync`; `Left` → `CacheHandlerErrorAsync`; exception → `MarkAsFailedAsync`; any store `Left` fails the invocation.
4. Message id: `"{eventId:N}:{handlerType.FullName}"`; `RequestType` = notification type.
5. The bespoke Marten-document mark (Choice 3 fallback) is **not** built unless the Phase 1 proof failed.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of Issue #2383: Inbox idempotency for Marten domain events.

CONTEXT:
- The maintainer decided (2026-10-16) that the existing Inbox is extended to domain events: one processed mark per (event id, handler), enlisted in the Marten transaction as ADR-048 defines (enlisted writes join the business transaction; AddAsync, MarkAsFailedAsync and CacheHandlerErrorAsync commit independently).
- The Marten outbox processor (Phase 3) dispatches with IRequestContext.IdempotencyKey = outbox id = Marten event id.
- #1220 needs the same per-handler hook for the 10 database providers; build it once in core.

TASK:
1. Add INotificationHandlerInvocationFilter and NotificationHandlerInvocation in core and call the filters from NotificationDispatcher around each handler when the context has an IdempotencyKey (all three dispatch strategies).
2. Implement InboxStoreMarten : IInboxStore with the ADR-048 enlisted/independent split, MartenInboxSessionListener and MartenDomainEventInboxFilter.
3. Unit tests for each outcome mapping; integration tests on MartenFixture: duplicate delivery skips a handler that succeeded; a handler that fails after another succeeded re-runs only the failed one; a rolled-back handler commit leaves no processed mark but keeps RetryCount.

KEY RULES:
- Errors are never swallowed: a store Left fails the invocation and therefore the outbox message.
- No EncinaError.Message in logs, tags or stored ErrorMessage.
- Async with CancellationToken; TimeProvider for time.
- Do not change the semantics of dispatch without an idempotency key.

REFERENCE FILES:
- docs/architecture/adr/048-inbox-record-vs-business-transaction.md
- src/Encina.Messaging/Inbox/IInboxStore.cs, InboxOrchestrator.cs, InboxPipelineBehavior.cs
- src/Encina/Dispatchers/Encina.NotificationDispatcher.cs, src/Encina/Dispatchers/Strategies/*.cs
- src/Encina.EntityFrameworkCore/Inbox/InboxStoreEF.cs (independent writes on an isolated context)
- Issue #1220 (per-handler completion)
```

</details>

---

### Phase 5: Removal, Configuration, DI and Startup Requirements

> **Goal**: Delete the in-process publication, register the mandatory set in `AddEncinaMarten`, and fail at startup for every forbidden or missing piece.

<details>
<summary><strong>Tasks</strong></summary>

#### Removal (completely, no compatibility code)

1. Delete `src/Encina.Marten/EventPublishingPipelineBehavior.cs` and `src/Encina.Marten/CommittedEventCollector.cs`.
2. `src/Encina.Marten/ServiceCollectionExtensions.cs`: delete `AddDomainEventPublishing` (`:60-72`) and its call (`:54`).
3. `src/Encina.Marten/Log.cs`: delete `PublishingDomainEvents`, `FailedToPublishDomainEvent`, `PublishedDomainEvents` (EventIds 2614-2616, retired and not reused); update the range comment in `EventIdRanges.cs`.
4. Tests: delete `EventPublishingPipelineBehaviorTests`, `EventPublishingRegistrationTests` (unit), `EventPublishingPipelineBehaviorGuardTests`, `EventPublishingPipelineBehaviorContractTests`, `EventPublishingPipelineBehaviorIntegrationTests`; update `ServiceCollectionExtensionsTests` and `EncinaMartenOptionsTests`; replace by the tests of Phase 8.
5. The 21 event and aggregate files of the compliance packages that say events are published "by Encina.Marten's EventPublishingPipelineBehavior" (for example `src/Encina.Compliance.BreachNotification/Events/BreachNotificationEvents.cs:5-6`) are rewritten to name the commit-bound outbox.
6. `.github/coverage-manifest/Encina.Marten.json`: remove the two deleted files' entries; add the new files with per-flag targets and one-sentence justifications.
7. Coordinate with #2184 Phase 5 (built-in step migration): `EventPublishingPipelineBehavior` disappears from its registration table.

#### Registration (Choice 14 = A)

8. `AddEncinaMarten(Action<EncinaMartenOptions>)` additionally registers, with `TryAdd*`:
   - the keyed `IOutboxStore` `"marten"` → `OutboxStoreMarten` (scoped); `InboxStoreMarten`; `IDeadLetterStore` → `DeadLetterStoreMarten` (`TryAdd`: a relational DLQ registered by the app wins);
   - `MartenOutboxDocumentConfiguration` and the store-wide listeners through `IConfigureMarten`;
   - `MartenOutboxNudge` (singleton), `MartenOutboxProcessor` (hosted service), `MartenDomainEventInboxFilter` (`TryAddEnumerable`);
   - the options with `Validate()` on start.

#### Startup requirements (Choice 15 = A, through #2382)

9. `MartenEventSourcingRequirements : IEncinaModuleRequirements` (`Module = "Encina.Marten"`):
   - Requires: a non-test `IDistributedLockProvider` (Choice 13), the `IDeadLetterStore`, `TimeProvider`.
   - Forbids: `Encina.Messaging.TransactionPipelineBehavior<,>`, `Encina.EntityFrameworkCore.TransactionPipelineBehavior<,>` (and the #718 `TransactionalOutboxPipelineBehavior<,>` when it exists): message "Encina.Marten: the Marten session is the unit of work; remove AddEncina...UseTransactions or move the data to Marten. Cross-store atomicity is #1225. See ADR-050."
   - Conditional: tenancy (when `ITenantContext` is registered: outbox documents carry `TenantId`), scheduling (modules with deadlines; rows owned by those modules).
10. The 9 `Add*Aggregates` methods (`BreachNotification`, `DSRRequest`, `DataResidency`, `LawfulBasis`, `CrossBorderTransfer`, `Consent`, `DPIA`, `Retention`, `ProcessorAgreement`) register a requirements row "requires `AddEncinaMarten`" (Choice 6): the failure names the method, the missing call and ADR-050.
11. DI tests build the provider with `ValidateOnBuild` and `ValidateScopes` (AGENTS.md §3, registration completeness).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of Issue #2383: remove in-process publication from Encina.Marten, register the mandatory delivery set, and declare the startup requirements.

CONTEXT:
- Decided 2026-10-16: AutoPublishDomainEvents and EventPublishingPipelineBehavior are removed; TransactionPipelineBehavior with Marten is a startup error; compliance Add*Aggregates do not register Marten and fail at startup without it; the module requirement mechanism is IEncinaModuleRequirements from #2382 (must be merged before this phase).
- Pre-1.0: delete completely, no [Obsolete], no compatibility aliases.

TASK:
1. Delete EventPublishingPipelineBehavior, CommittedEventCollector, AddDomainEventPublishing, the three publishing log methods (EventIds 2614-2616 retired), and their tests; update every comment that names the behavior.
2. Register the outbox store (keyed "marten"), inbox store, DLQ store, listeners, nudge, processor and inbox filter in AddEncinaMarten with TryAdd*/TryAddEnumerable.
3. Implement MartenEventSourcingRequirements and the requirement rows of the 9 Add*Aggregates methods.
4. DI tests with ValidateOnBuild and ValidateScopes; startup tests for each forbidden and missing case asserting the message names module, requirement and ADR.
5. Update .github/coverage-manifest/Encina.Marten.json for removed and new files with justified per-flag targets.

KEY RULES:
- A database store registered by the application wins over the Marten default in any registration order, without overriding the application's own registration.
- Startup failures name the module, the missing or forbidden piece and the ADR; fail closed.
- Zero warnings; PublicAPI files updated (RS0016/RS0017).

REFERENCE FILES:
- src/Encina.Marten/ServiceCollectionExtensions.cs
- src/Encina.Compliance.*/*MartenExtensions.cs (9 files)
- src/Encina.Marten.GDPR/CryptoShreddingStartupValidationHostedService.cs (existing startup check style)
- Issue #2382 (IEncinaModuleRequirements), Issue #2381 (ADR-050)
- src/Encina.Messaging/TransactionPipelineBehavior.cs, src/Encina.EntityFrameworkCore/TransactionPipelineBehavior.cs
```

</details>

---

### Phase 6: Cross-Cutting Integration

> **Goal**: Wire tenancy, locks, health and the compliance gaps; make sure domain-event rows never leave the process.

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-tenancy (conditional)**: the listener copies `TenantId` from the event headers (`EventMetadataEnrichmentService.cs:101`); the processor restores it in the dispatch context; the claim request carries the tenant filter when the #1251 contract has one (#737/#1257 alignment). Marten native tenancy stays with #1257.
2. **Distributed lock**: purge of processed outbox and expired inbox documents runs once per cycle under `IDistributedLockProvider.TryAcquireAsync("encina:marten:outbox:purge", ...)`; dispatch never takes the lock (fenced claims).
3. **Health check**: `MartenOutboxHealthCheck` — `public sealed class : EncinaHealthCheck` in `Encina.Marten.Health`, `DefaultName = "encina-marten-outbox"`, `Tags = ["encina", "marten", "outbox", "ready"]`, resolves the keyed store in a scope (`IServiceProvider.CreateScope()`), reports backlog age, exhausted count and processor liveness (last cycle time); registered by `AddEncinaMarten` when `ProviderHealthCheck.Enabled`, else by the health-check registration (mandatory processor health, Choice 1).
4. **Transport exclusion**: a contract test proves the Marten processor never calls an `IOutboxTransportPublisher` (#2385) for a domain-event row.
5. **Compliance gaps**: no code change in `BreachDetectionPipelineBehavior` or `RetentionEnforcementService`; Phase 8 adds the tests proving their events are delivered.
6. **Not in scope, linked**: `BreachDeadlineMonitorService` fire-and-forget publication (`src/Encina.Compliance.BreachNotification/BreachDeadlineMonitorService.cs:153`, `:196`) is #2378 and #769; the dead option `UseOptimisticConcurrency` (`EncinaMartenOptions.cs:23`) is #2379; inline projection failures after commit (`MartenAggregateRepository.cs:215-220`) are #2153.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of Issue #2383: cross-cutting integration of the Marten outbox and inbox.

CONTEXT:
- Phases 1-5 built OutboxStoreMarten, the listener, MartenOutboxProcessor, InboxStoreMarten, DeadLetterStoreMarten and the registrations.
- Mandatory with event sourcing: processor health and metrics, multi-instance coordination. Conditional: multi-tenancy.
- Domain events are dispatched in process only; integration events reach brokers through #2385, never domain-event rows.

TASK:
1. Propagate TenantId from event headers to outbox documents and back into the dispatch context; add the tenant filter to the claim when the #1251 contract provides it.
2. Run purge under IDistributedLockProvider.TryAcquireAsync; never lock dispatch.
3. Implement MartenOutboxHealthCheck (backlog age, exhausted count, processor liveness).
4. Add the transport-exclusion contract test.

KEY RULES:
- Health checks: const DefaultName, static Tags array, scoped resolution through IServiceProvider.CreateScope().
- Health results never contain EncinaError.Message.
- Lock acquisition uses a timeout and the CancellationToken; a failed acquisition skips the purge for this cycle and is logged.

REFERENCE FILES:
- src/Encina.Messaging/Health/OutboxHealthCheck.cs, src/Encina.Marten/Health/MartenHealthCheck.cs
- src/Encina.DistributedLock/IDistributedLockProvider.cs
- src/Encina.Marten/EventMetadataEnrichmentService.cs
```

</details>

---

### Phase 7: Observability

> **Goal**: Trace each publish attempt, count outcomes and log with registered EventIds.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Marten/Diagnostics/MartenDeliveryDiagnostics.cs` — `internal static class`: `ActivitySource` `"Encina.Marten"` version `"1.0"`; `Meter` `"Encina.Marten"`.
   - Counters (`Counter<long>`): `encina.marten.outbox.written` (tag `event.type`), `encina.marten.outbox.published` (tags `event.type`, `outcome`), `encina.marten.outbox.dead_lettered` (tag `event.type`, `error.code`), `encina.marten.outbox.claim_lost`, `encina.marten.inbox.skipped_duplicates` (tag `handler`).
   - Histogram: `encina.marten.outbox.delivery_lag` (seconds from `CreatedAtUtc` to processed).
   - Spans: `encina.marten.outbox.publish` (tags `event.type`, `encina.outbox.attempt`, `encina.tenant_id` when set, `error.code` on failure); `encina.marten.outbox.claim` (tag `encina.claim.batch_size`).
   - The existing `OutboxProcessorMetrics` (`src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs:40`) keeps counting the shared batch outcomes.
2. `src/Encina.Marten/Diagnostics/MartenDeliveryLogMessages.cs` — `[LoggerMessage]`, EventIds **2701-2720** inside the existing `Marten` range (2600-2799): listener (2701-2703), claim (2704-2706), dispatch (2707-2711), dead-letter (2712-2713), inbox (2714-2717), purge and lock (2718-2719), requirements (2720). XML doc names the range.
3. `src/Encina/Diagnostics/EventIdRanges.cs`: update the `Marten` summary comment to list "delivery (2701-2720)" and "2614-2616 retired"; no new range is needed. `EncinaEventIdAllocationTests` already maps the `Encina.Marten` assembly.
4. Encina core: if Phase 4 adds logging to the dispatcher hook, it uses the `Core` range (100-199) next free ids.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of Issue #2383: observability of the Marten outbox and inbox.

CONTEXT:
- Encina.Marten has no ActivitySource or Meter today; its logs use EventIds 2600-2700 of the registered Marten range 2600-2799 (src/Encina/Diagnostics/EventIdRanges.cs).
- 2614-2616 are retired with the removed publishing behavior.

TASK:
1. Add MartenDeliveryDiagnostics (ActivitySource and Meter "Encina.Marten", the counters, histogram and spans listed in Phase 7 Tasks).
2. Add MartenDeliveryLogMessages with [LoggerMessage] EventIds 2701-2720, packed sequentially and grouped by area; update the Marten range comment in EventIdRanges.cs.
3. Instrument listener, store, processor, inbox filter and purge.
4. Run the architecture tests (EncinaEventIdAllocationTests, EventIdUniquenessRule).

KEY RULES:
- Tags and logs never carry EncinaError.Message or event payloads; error code and type names only; exceptions through ForLogging().
- Use the [LoggerMessage] source generator, not LoggerMessage.Define.
- No sparse EventId allocation.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs
- src/Encina.Audit.Marten/Diagnostics/MartenAuditActivitySource.cs
- src/Encina.Marten/Log.cs
```

</details>

---

### Phase 8: Testing — 7 Test Types

> **Goal**: Prove "no committed event is lost, no handler runs twice for one event" on real PostgreSQL, and reach every per-flag target of the touched files.

<details>
<summary><strong>Tasks</strong></summary>

#### 8a. Unit Tests (`tests/Encina.UnitTests/Marten/Outbox/`, `.../Inbox/`, `.../DeadLetter/`)

- Listener: only `INotification` events are written; reference content; headers copied; nudge signalled; no write when nothing is pending.
- `OutboxStoreMarten`: every method's `Right` and `Left` branch with a substituted session; fenced completion returns `claim_lost`.
- Processor: dispatch by reference, event not found, publish `Left` → retry, exhaustion → DLQ capture, non-retryable → immediate DLQ, nudge vs interval, cancellation releases claims.
- Inbox filter: `Right`/`Left`/exception mapping (ADR-048), duplicate skip, store `Left` fails the invocation, no key → pass-through.
- Core hook: each dispatch strategy invokes the filters per handler.
- Registration: every component resolves with `ValidateOnBuild`/`ValidateScopes`; an application store wins; forbidden transaction behaviors and missing `AddEncinaMarten` fail with the expected message.
- Target: ~110 tests, <1 ms each.

#### 8b. Guard Tests (`tests/Encina.GuardTests/Marten/Outbox/`)

- Null checks of every public constructor and method (`OutboxStoreMarten`, `InboxStoreMarten`, `DeadLetterStoreMarten`, `MartenOutboxHealthCheck`, options, the core filter API). ~30 tests.

#### 8c. Contract Tests (`tests/Encina.ContractTests/Marten/`)

- `DeadLetterStoreMarten` passes the existing DLQ contract (`tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs`, ADR-046).
- No shared `IOutboxStore` or `IInboxStore` store contract suite exists today: create `OutboxStoreContract` (including the #1251 claim and fencing rules) and `InboxStoreContract` (including the ADR-048 enlisted/independent split) as abstract suites in `tests/Encina.ContractTests/Messaging/`, run them for the Marten stores, and leave them reusable by the 10 database providers. Real implementations against `MartenFixture`, never reflection-only.
- Transport exclusion contract (Phase 6).

#### 8d. Property Tests (`tests/Encina.PropertyTests/Marten/`)

- FsCheck: for random sequences of commands, injected failures (handler `Left`, exception, crash points before/after commit, before/after mark) and random instance interleavings, every committed `INotification` event is delivered to every handler **at least once** and **processed at most once per handler**; per-stream order holds; uncommitted events are never delivered. Uses an in-memory model of the session plus the real listener, store and filter logic where possible.

#### 8e. Integration Tests (`tests/Encina.IntegrationTests/Infrastructure/Marten/Outbox/`, real PostgreSQL via Testcontainers, `[Collection(MartenCollection.Name)]`)

| Scenario | Assertion |
| --- | --- |
| Command commits events | One outbox document per `INotification` event, same transaction; handlers receive each event once |
| Commit fails (concurrency conflict) | No outbox document, no delivery |
| **Failure between commit and delivery**: processor host stopped after the commit, before dispatch | After restart, every event is delivered |
| **Restart mid-batch**: process killed after publish, before the mark | Redelivery happens; the Inbox skips handlers that already processed it |
| **Duplicate delivery**: the same document dispatched twice | Each handler's effect occurs once; `skipped_duplicates` counted |
| One of two handlers fails | Retry re-runs only the failed handler; after `MaxRetries` the message is in `DeadLetterStoreMarten` with its error code |
| **Two instances**: two processors on one database, 1,000 events across 50 streams | Every event processed exactly once per handler; per-stream order kept; no claim overlap |
| Lease expiry: an instance stalls past `LeaseDuration` | Another instance reclaims; the stalled completion returns `claim_lost` |
| **Query path**: a query through `BreachDetectionPipelineBehavior` records a breach | `BreachDetected` is delivered |
| **Background path**: `RetentionEnforcementService` cycle holds or erases a record | Its events are delivered |
| Crypto-shredded subject | The dispatched event carries the shredded (redacted) values; the outbox document holds no personal data |
| Multi-tenant context | `TenantId` reaches the handler's context |
| Startup | `TransactionPipelineBehavior` + Marten fails; `Add*Aggregates` without `AddEncinaMarten` fails; missing lock provider fails |
| Compliance modules | The existing compliance integration suites pass with the outbox drained through the test helper |

- `Encina.Testing`: `MartenOutboxTestDriver.DrainAsync()` runs processor cycles until the outbox is empty, so suites that asserted immediate publication drain explicitly.

#### 8f. Load Tests (`tests/Encina.LoadTests/Marten/Outbox/`)

- Implement: 4 concurrent processors × 10,000 events; assert no duplicate processing, report throughput and delivery lag (concurrent feature; AGENTS.md §9). Run locally with `DOTNET_JitObjectStackAllocationConditionalEscape=0`.

#### 8g. Benchmark Tests (`tests/Encina.BenchmarkTests/Marten/Outbox/`)

- Justify with `tests/Encina.BenchmarkTests/Marten/OutboxMarten.md` (`# Benchmark Tests - Marten Outbox`, `## Status: Not Implemented`): the write is one extra document per event inside an existing commit, and dispatch is I/O bound; the load test reports throughput and lag. `## Issue: #2383`.

#### Coverage

- Per-flag targets for every new and touched file in `.github/coverage-manifest/Encina.Marten.json` (and `Encina.Messaging.json`, `Encina.json` for the hooks), measured with each flag's test project and `.github/scripts/coverage-report.cs` (AGENTS.md §9); CRAP ≤ 10 on every changed method.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of Issue #2383: the full test matrix for the Marten commit-bound outbox, inbox and dead-letter store.

CONTEXT:
- The guarantee to prove: every committed INotification event is delivered to every handler at least once and processed at most once per handler, in per-stream order, across crashes, restarts, duplicates and two concurrent instances.
- Integration tests run on real PostgreSQL through MartenFixture (Testcontainers), collection "Marten".
- The study of 2026-10-16 found two paths the old behavior missed: a query through BreachDetectionPipelineBehavior and the RetentionEnforcementService background cycle; both must be covered.

TASK:
Write the unit, guard, contract, property, integration and load tests listed in Phase 8 Tasks, the benchmark justification file, and MartenOutboxTestDriver in Encina.Testing; update the coverage manifests with justified per-flag targets and measure them.

KEY RULES:
- Tests execute real package code; no reflection-only or type-only tests.
- Shouldly through Encina.Testing.Shouldly; never FluentAssertions; test projects reference Encina.Testing.* wrappers.
- No Thread.Sleep; use TimeProvider fakes and explicit drains; deterministic, independent tests.
- Integration classes use [Collection(MartenCollection.Name)], [Trait("Category","Integration")], [Trait("Database","PostgreSQL")]; never dispose the fixture; clean data in InitializeAsync.
- Outputs under artifacts/ only.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs
- tests/Encina.IntegrationTests/Infrastructure/Marten/Core/ (existing Marten integration style)
- tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs (existing DLQ contract; the outbox and inbox store suites are new)
- docs/testing/coverage-measurement-methodology.md, docs/testing/integration-tests.md
```

</details>

---

### Phase 9: Documentation & Finalization

> **Goal**: Record the decision, document usage and the breaking change, and pass build, tests and coverage.

<details>
<summary><strong>Tasks</strong></summary>

1. **ADR** (Choice 16 = A): new ADR "Event-sourced domain events are delivered through a commit-bound Marten outbox" (number reserved in the plan PR, next free after 050); amend ADR-048 "Mechanisms per family" with the Marten row and the domain-event key `{eventId}:{handler}`; ADR-019 (`:122`) and ADR-028 (`:13`) are updated by #2381 and link the new ADR. `docs/architecture/adr/index.md` updated.
2. **XML documentation** on every new public API (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`).
3. **Changelog fragments**: `changelog.d/2383-marten-reliable-delivery.added.md` (outbox, inbox, DLQ store, processor, health, metrics) and `changelog.d/2383-marten-auto-publish.removed.md` (`AutoPublishDomainEvents`, `EventPublishingPipelineBehavior`, in-process publication; handlers now run after the commit from the outbox).
4. **`src/Encina.Marten/README.md`**: delivery model, registration, requirements, test drain helper.
5. **`docs/features/`**: a how-to page for Marten reliable delivery (encina-docs skill, one Diátaxis quadrant, `docs-writer`), and the reference rows of the module requirements matrix page of #2381.
6. **`docs/INVENTORY.md`**: new files and components.
7. **`ROADMAP.md`**: mark #2383 under the 1.0 milestone; link #2384 as post-1.0.
8. **PublicAPI**: `Encina.Marten`, `Encina.Messaging`, `Encina` Unshipped files complete.
9. **`docs/releases/`**: release notes of the next version, breaking change section.
10. **Build**: `dotnet build Encina.slnx --configuration Release` → 0 errors, 0 warnings.
11. **Tests**: `dotnet test Encina.slnx --configuration Release` → all pass; each coverage flag reaches its manifest target for every touched file (per-flag model, no single percentage); `coverage-report.cs -- --check-justifications` passes.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of Issue #2383: documentation and finalization of Marten reliable delivery.

CONTEXT:
- Phases 1-8 replaced in-process publication of Marten domain events with a commit-bound outbox (OutboxStoreMarten), an Inbox for domain events (InboxStoreMarten), a dead-letter store and a processor with claims, health and metrics.
- ADR-050 (#2381) records the module-requirements rule and updates ADR-019 and ADR-028; this feature adds its own ADR and amends ADR-048.

TASK:
1. Write the new ADR and the ADR-048 amendment; update the ADR index.
2. Add the two changelog fragments under changelog.d/ (never edit CHANGELOG.md Unreleased).
3. Update src/Encina.Marten/README.md, docs/INVENTORY.md, ROADMAP.md, release notes, PublicAPI files.
4. Delegate the docs/features page to docs-writer with the encina-docs skill.
5. Run the full build and test commands and the coverage report; fix every gap.

KEY RULES:
- English only; no hand-typed coverage figures (use covref markers); no emojis.
- Pages under docs/ follow the encina-docs skill (one Diátaxis quadrant per page).
- Zero warnings; every flag at its manifest target.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
- docs/architecture/adr/048-inbox-record-vs-business-transaction.md, 019-compliance-event-sourcing-marten.md, 028-domain-events-versus-integration-events.md
- docs/architecture/adr/index.md (reserved numbers)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Requirement | How this plan meets it |
|--------|-------------|------------------------|
| GDPR Art. 33 | Notify the authority of a breach within 72 hours | `BreachDetected` and successors are delivered after any crash; DLQ makes a stuck notification visible |
| GDPR Art. 12(3) | Answer a data-subject request within one month | DSR events that start deadlines are never lost |
| GDPR Art. 5(2), Art. 30 | Accountability: demonstrate compliance | Dead-lettered events and metrics are the evidence that a failure was seen |
| GDPR Art. 17 | Erasure reaches every copy | Reference payload (Choice 7): the outbox holds no copy of personal data |
| GDPR Art. 32 | Integrity of processing | No silent loss of messages |
| NIS2 Art. 21, Art. 23 | Incident handling; 24 h / 72 h reporting | Same delivery guarantee as Art. 33 |
| EIP: Transactional Outbox, Idempotent Receiver, Dead Letter Channel, Competing Consumers | At-least-once delivery bound to the commit; deduplicating consumer; visible failures; several processors | Phases 2-4, 6 |
| PostgreSQL `SELECT ... FOR UPDATE SKIP LOCKED` | Non-blocking row claims | Claim statement (Phase 2) |
| Marten 9 session listeners (`IDocumentSessionListener`, `BeforeSaveChangesAsync`, `AfterCommitAsync`) and `StoreOptions.Listeners` | Hook into the unit of work of every session | Listener (Phase 2), proof (Phase 1) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IOutboxStore`, `IOutboxMessage` | `src/Encina.Messaging/Outbox/` | Contract of `OutboxStoreMarten` |
| `OutboxProcessorBase`, `OutboxBatchProcessor`, `OutboxRetryBackoff` | `src/Encina.Messaging/Outbox/` | Base of `MartenOutboxProcessor`; retry and batch outcomes |
| `OutboxProcessorMetrics`, `OutboxHealthCheck` | `src/Encina.Messaging/Diagnostics/`, `Health/` | Shared batch metrics; health-check pattern |
| `IInboxStore`, `InboxOrchestrator`, ADR-048 | `src/Encina.Messaging/Inbox/`, `docs/architecture/adr/048-*.md` | Contract and semantics of `InboxStoreMarten` |
| `IDeadLetterStore`, `DeadLetterSourceCapture`, ADR-046, ADR-029 | `src/Encina.Messaging/DeadLetter/` | DLQ store contract, capture, error classification |
| `IDistributedLockProvider` | `src/Encina.DistributedLock/` | Single-runner purge |
| `CommittedEventCollector` (to be removed) | `src/Encina.Marten/CommittedEventCollector.cs` | Pattern for a `DocumentSessionListenerBase` |
| `IConfigureMarten` bridge | `src/Encina.Marten/ServiceCollectionExtensions.cs:123-130` | Store-wide listeners and document mapping |
| `EventMetadataEnrichmentService` | `src/Encina.Marten/EventMetadataEnrichmentService.cs:96-104` | TenantId, UserId, correlation headers copied into outbox documents |
| `MartenFixture`, `MartenCollection` | `tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs` | Real PostgreSQL for integration, contract and load tests |
| `PipelineStage`, `IRequestPostCommitProcessor` (#2184) | `src/Encina/Pipeline/Ordering/PipelineStage.cs`, `src/Encina/Abstractions/IRequestPostCommitProcessor.cs` | Not used for delivery: the outbox write is a session listener, so it needs no stage; the post-commit hook is explicitly not for durable side effects |
| `IRequestContext.IdempotencyKey` | `src/Encina/Abstractions/IRequestContext.cs:116` | Carries the outbox id into the per-handler filter |
| #1251 claim contract | `docs/plans/processor-row-claiming-implementation-plan-1251.md` | Claim methods, lease, fenced completion |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Marten` (registered) | 2600-2799 | 2600-2700 in use; 2614-2616 retired with the removed publishing behavior |
| **`Encina.Marten` — delivery (proposed, inside the package range)** | **2701-2720** | **Listener, claim, dispatch, dead-letter, inbox, purge/lock, requirements; no new range to register; the `Marten` range comment in `EventIdRanges.cs` is updated** |
| `Encina` core (only if Phase 4 logs in the dispatcher) | 100-199 | Next free ids of the `Core` range |
| `Encina.Messaging` | 2800-2999 | Not used by this plan (2963-2988 are reserved by #718, #1200, #1251) |
| Free ranges (AGENTS.md §7, 2026-10-10) | 300-1099, 3850-3899, 5500-6999, 7100-7999, 8950-8999, 9700-9999 | Fallback only if the Marten range is exhausted; then the next free range is 300-349 |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| `Encina.Marten` production (Phases 1-7) | ~20 | Documents, options, listener(s), 3 stores, processor, nudge, filter, requirements, health, diagnostics, logs |
| `Encina.Messaging` / `Encina` core hooks | ~5 | Dispatch hook, per-handler filter, dispatcher change |
| Removed files | 2 production + 5 test | Behavior, collector and their tests |
| Compliance packages | ~30 | 9 `*MartenExtensions.cs` + 21 event/aggregate comments |
| Tests (Phase 8) | ~30-35 | Unit, guard, contract, property, integration, load, benchmark justification |
| Documentation & config (Phase 9) | ~12 | ADR, ADR-048, index, README, feature page, INVENTORY, ROADMAP, release notes, 2 changelog fragments, 3 manifests, PublicAPI |
| **Total** | **~105** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing Issue #2383 — reliable delivery of Marten domain events through a commit-bound outbox (OutboxStoreMarten), Inbox idempotency for domain events, and the mandatory module set of the event-sourcing route.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; ROP with Either<EncinaError, T>; pre-1.0: no backward compatibility, delete completely, no [Obsolete].
- Marten 9.38.0 on PostgreSQL is the event-sourcing provider, outside the 10-database rule (AGENTS.md section 5, ADR-019, ADR-027).
- Maintainer decisions of 2026-10-16 (#2239): modules may require modules (ADR-050, #2381, #2382); delivery through the Outbox with OutboxStoreMarten written in the same Marten session (listener before SaveChanges), AutoPublishDomainEvents removed, subscription post-1.0 (#2384); consumer idempotency through the existing Inbox enlisted per ADR-048; TransactionPipelineBehavior with Marten is a startup error; multi-instance coordination always on (FOR UPDATE SKIP LOCKED claims, distributed lock for single-runner work, #1251); compliance Add*Aggregates do not register Marten and fail at startup without it.
- Plan: docs/plans/es-reliable-delivery-implementation-plan-2383.md (read Design Choices and Maintainer Decisions first).

IMPLEMENTATION OVERVIEW:
Phase 1: Listener enlisting proof on real PostgreSQL; MartenOutboxMessage, MartenInboxMessage, options, error codes; AutoPublishDomainEvents deleted.
Phase 2: MartenOutboxSessionListener (store-wide, before SaveChanges) and OutboxStoreMarten with the #1251 claim contract and per-stream order.
Phase 3: OutboxProcessorBase dispatch hook; MartenOutboxProcessor (dispatch by event reference, context restored, nudge); DeadLetterStoreMarten.
Phase 4: Core per-handler invocation filter (shared with #1220); InboxStoreMarten with the ADR-048 split; MartenDomainEventInboxFilter.
Phase 5: Remove EventPublishingPipelineBehavior and CommittedEventCollector; AddEncinaMarten registers the mandatory set; requirement rows through IEncinaModuleRequirements (#2382).
Phase 6: Tenancy propagation, lock for purge, MartenOutboxHealthCheck, transport exclusion.
Phase 7: ActivitySource and Meter "Encina.Marten", [LoggerMessage] EventIds 2701-2720.
Phase 8: Unit, guard, contract, property, integration (commit/delivery failure, restart, duplicate, two instances, query path, background path), load; benchmark justified.
Phase 9: New ADR + ADR-048 amendment, README, feature page, changelog fragments, INVENTORY, ROADMAP, PublicAPI, build and coverage.

KEY PATTERNS:
- Store naming {Pattern}Store{Provider}: OutboxStoreMarten, InboxStoreMarten, DeadLetterStoreMarten; keyed IOutboxStore "marten" so a relational outbox can coexist.
- The outbox document references the event (id, stream, sequence); the processor loads it, so crypto-shredding and upcasters apply.
- Errors are never swallowed in background infrastructure: a Left retries, then dead-letters.
- EncinaError.Message never reaches logs, tags, health results or stored ErrorMessage.
- Time from TimeProvider; async database calls with CancellationToken only.
- Health checks: const DefaultName, static Tags, scoped resolution.
- Integration tests: [Collection(MartenCollection.Name)], traits Category=Integration and Database=PostgreSQL; Shouldly; no Thread.Sleep.
- Per-flag coverage targets with justifications in .github/coverage-manifest; CRAP <= 10 on changed methods.

REFERENCE FILES:
- src/Encina.Marten/ (EventPublishingPipelineBehavior.cs, CommittedEventCollector.cs, MartenAggregateRepository.cs, ServiceCollectionExtensions.cs, EncinaMartenOptions.cs, EventMetadataEnrichmentService.cs, Log.cs)
- src/Encina.Messaging/Outbox/, Inbox/, DeadLetter/, Health/, Diagnostics/
- src/Encina/Dispatchers/Encina.NotificationDispatcher.cs
- docs/architecture/adr/019, 028, 029, 046, 048; docs/plans/processor-row-claiming-implementation-plan-1251.md; docs/plans/outbox-atomicity-ado-implementation-plan-718.md
- tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: delivery path with no read that benefits from caching |
| 2 | OpenTelemetry | ✅ | `ActivitySource`/`Meter` `Encina.Marten`: publish and claim spans, written/published/dead-lettered/claim-lost/duplicate counters, delivery-lag histogram (Phase 7) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` EventIds 2701-2720 inside the registered `Marten` range (Phase 7) |
| 4 | Health Checks | ✅ | `MartenOutboxHealthCheck`: backlog age, exhausted count, processor liveness; mandatory with ES (Phase 6) |
| 5 | Validation | ✅ | Options `Validate()` at startup and startup requirement checks; no request input is added |
| 6 | Resilience | ✅ | Retry with backoff (`OutboxRetryBackoff`), error classification (ADR-029), dead-letter after `MaxRetries` (Phase 3) |
| 7 | Distributed Locks | ✅ | Fenced `FOR UPDATE SKIP LOCKED` claims for dispatch; `IDistributedLockProvider` for purge (Choices 5, 13; Phase 6) |
| 8 | Transactions | ✅ | The Marten session is the unit of work: outbox document, inbox mark and DLQ capture commit with it; `TransactionPipelineBehavior` forbidden (Choice 4; Phase 5) |
| 9 | Idempotency | ✅ | Inbox mark per (event id, handler), enlisted per ADR-048 (Phase 4) |
| 10 | Multi-Tenancy | ✅ | Conditional: `TenantId` from event headers to outbox documents and back to the dispatch context; tenant filter in the claim when #1251/#737 provide it; Marten native tenancy is #1257 |
| 11 | Module Isolation | ⏭️ | `IRequestContext` has no `ModuleId` today; scoping outbox documents and handlers by module needs a new issue (to be opened by the orchestrator with this plan's PR) |
| 12 | Audit Trail | ❌ | Not applicable: the event stream is the record of what happened (ADR-019 row 12); dead-letter entries and metrics are the operational trace of delivery |

---

## Prerequisites & Dependencies

| Item | Kind | Needed by | Notes |
|------|------|-----------|-------|
| #2381 (ADR-050, module requirements rule; updates ADR-019/ADR-028) | Prerequisite | Phases 5, 9 | The rule is decided; the ADR text is #2381's |
| #2382 (`IEncinaModuleRequirements`, startup validator) | Prerequisite (Choice 15) | Phase 5 | Phases 1-4 do not wait |
| #1251 (row claiming, ADR-043 reserved) | Prerequisite | Phase 2 | Claim contract shape; if not merged, Phase 2 follows its decided plan |
| #207 (PostgreSQL distributed lock) | Prerequisite (Choice 13) | Phase 6 | A Marten-only app needs a PostgreSQL lock provider |
| #1220 (per-handler completion) | Shared work | Phase 4 | Whoever lands first builds the per-handler hook |
| #2184 (pipeline execution order) | Coordination | Phase 5 | `EventPublishingPipelineBehavior` leaves #2184's built-in table; the outbox write and inbox check are not pipeline stages |
| #718 / #719 (transactional outbox for the 10 providers, ADR-037 reserved) | Coordination | Phase 5 | Its `TransactionalOutboxPipelineBehavior` joins the forbidden list; it removes `OutboxPostProcessor` |
| #2385 (outbox-to-transport publisher) | Coordination | Phases 3, 6 | Shares the processor dispatch hook; domain-event rows never go to a transport |
| #1200 (purge of processed messages) | Coordination | Phase 6 | Same retention option names |
| #737 (TenantId on OutboxMessage), #1257 (SPEC-002 multi-tenancy, Marten) | Related | Phase 6 | Tenant filter of claims; native Marten tenancy |
| #2384 (Marten subscription, post-1.0) | Related | — | Alternative route, opt-in |
| #1225 (EF/Dapper + Marten consistency) | Related | — | Named by the forbidden-combination message |
| #2378, #769 (`BreachDeadlineMonitorService`), #763 (DPIA reminder lock), #2379 (dead `UseOptimisticConcurrency`), #2153 (inline projection failure) | Related, out of scope | — | Found by the same study; each has its own issue |
| #2239 | Closed by this feature | — | `Fixes #2239` in the final PR together with `Fixes #2383` |

### Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Marten does not include documents stored in `BeforeSaveChangesAsync` in the same commit, or event ids are not final there | The decided mechanism fails | Phase 1 proof runs first; a failure returns Choices 2, 3, 8 to the maintainer before any other work |
| Handlers now run after the command, not inside it | Tests and code that assumed immediate publication break; read-your-writes in handlers becomes eventual | `MartenOutboxTestDriver.DrainAsync()`; release notes breaking-change section; after-commit nudge (Choice 11) |
| A poisoned stream head blocks its stream (Choice 10) | Later events of that stream wait | Bounded by `MaxRetries`, then dead-lettered; health check reports backlog age |
| Raw claim SQL depends on Marten's table and duplicated-column naming | Breaks on a Marten upgrade | Names read from Marten's mapping; integration test on every claim path; Dependabot Marten updates run the Marten suite |
| Parallel plans (#718, #1251, #1220, #2385) change `OutboxProcessorBase` | Merge conflicts, two dispatch paths | Prerequisites table; one hook shared with #2385; one per-handler hook shared with #1220 |
| An application keeps a relational outbox and Marten in one host | Wrong store resolved | Keyed `IOutboxStore` `"marten"`; DI test with both registered |
| Crypto-shredded events dispatched after erasure | Handlers receive redacted data | Intended (GDPR Art. 17); integration test pins it; documented for handler authors |
| Dispatch load reads every event once more | Extra PostgreSQL reads | Measured by the load test; batch load by id list if needed |

---

## Next Steps

1. Merge this plan (structure checked with `pwsh tools/ai/plans/check-plan.ps1 -Path docs/plans/es-reliable-delivery-implementation-plan-2383.md -Draft`).
2. Orchestrator: comment on #2383 with the plan link; open the Module Isolation deferral issue (matrix row 11); reserve the ADR number of Choice 16 in `docs/architecture/adr/index.md`.
3. When work on #2383 starts: re-check Choices 7-16 against `main` and run `check-plan` without `-Draft`.
4. Phases 1-4 can start before #2382; Phase 5 waits for #2382; Phase 6 needs #207.
5. Each phase is a self-contained commit; the final PR references `Fixes #2383` and `Fixes #2239`.

---

## Maintainer Decisions

Decisions 1-6 were taken on 2026-10-16 in the comment on #2239, before this plan was written. Entries 7-16 were settled on 2026-10-17 by elimination (no open question remains).

1. (2026-10-16) General rule, option (a): AGENTS.md §3 is amended so that a module MAY require other modules when it needs them for its guarantees; each module's closed list of required and forbidden modules is recorded in an ADR (#2381, ADR-050) and enforced by startup validation (#2382). Opt-in stays the rule for everything a module does not need. Mandatory with the event-sourcing route: reliable commit-bound delivery (Outbox), consumer idempotency (Inbox), recoverability + DLQ, processor health/metrics, multi-instance coordination. Forbidden: `TransactionPipelineBehavior`. Conditional: multi-tenancy, scheduling.
2. (2026-10-16) Delivery mechanism, option (c): for 1.0, Marten domain events are delivered through the Outbox with an `OutboxStoreMarten` written in the same Marten session/transaction (listener before `SaveChanges`); the Marten subscription / async daemon route comes post-1.0 as an opt-in alternative (#2384). In-process publication after the command (`AutoPublishDomainEvents`) is removed; option C of #2239 is dropped.
3. (2026-10-16) Consumer idempotency, option (b): the existing Inbox is extended to domain events (processed mark per event id and handler), enlisted in the Marten transaction as ADR-048 defines; a Marten-document mark is the fallback only if enlisting proves infeasible.
4. (2026-10-16) `TransactionPipelineBehavior` with Marten, option (a): startup error (fail-fast) explaining that the Marten session is the unit of work; cross-store atomicity stays with #1225.
5. (2026-10-16) Multi-instance coordination, option (a): always on; the processor claims rows with `FOR UPDATE SKIP LOCKED` and single-runner work takes a distributed lock (direction of ADR-043 / #1251).
6. (2026-10-16) Compliance `Add*Aggregates`, option (b): they do not register Marten themselves; the application fails at startup with a clear message when `AddEncinaMarten` or any module the ES route requires is missing.
7. (2026-10-17, settled by elimination) Outbox payload, option B: a reference to the committed Marten event (event id, stream id, sequence), loaded at dispatch. A plain copy leaves personal data that crypto-shredding cannot reach (GDPR Art. 17); C couples the outbox to the key model for no gain.
8. (2026-10-17, settled by elimination) Outbox storage, option A: a Marten document `MartenOutboxMessage` with duplicated claim columns. Choice 2 fixes the same Marten session; B creates two schema owners; C ties #2239 to #1225.
9. (2026-10-17, settled by elimination) Per-handler Inbox check, option A: a core per-handler invocation hook shared with #1220. B duplicates the dispatcher; C is fragile on open generics.
10. (2026-10-17, settled by elimination) Ordering, option B: per-stream order. Axon, EventStoreDB and the Marten daemon preserve stream order; a poisoned head is bounded by `MaxRetries` and then the DLQ; C is not built.
11. (2026-10-17, settled by elimination) Dispatch latency, option B: polling plus an after-commit nudge. Durability is unchanged; C adds a held connection per instance; A regresses immediate publication.
12. (2026-10-17, settled by elimination) Dead-letter store, option A: `DeadLetterStoreMarten`. Choice 1 makes the DLQ mandatory; B forces a second data-access stack on every ES app.
13. (2026-10-17, settled by elimination) Single-runner lock, option A: `IDistributedLockProvider` with #207 first. AGENTS.md §5 and SPEC-000 DEC-003 fix the lock category; B is a second mechanism; C leaves sweeps concurrent.
14. (2026-10-17, settled by elimination) Registration, option A: `AddEncinaMarten` registers the mandatory set in `Encina.Marten`. Choice 1 = A on #2381; B is the "call this" validation that decision rejects; C adds a package every ES app needs anyway.
15. (2026-10-17, settled by elimination) Startup validation, option A: declared through #2382. AGENTS.md §3 forbids transitional code; B is throwaway code.
16. (2026-10-17, settled by elimination) Design record, option A: a new ADR for the delivery route plus an ADR-048 amendment; ADR-019/028 via #2381. AGENTS.md §8 requires an ADR per architectural decision; one decision per ADR.
