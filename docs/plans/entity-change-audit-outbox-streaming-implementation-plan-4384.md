# Implementation Plan: Publish streamed entity-change audit entries through the outbox

> **Issue**: [#4384](https://github.com/dlrivada/Encina/issues/4384)
> **Type**: Feature
> **Related**: #580 (Kafka composite store), #581 (Redis Streams composite store), #577 (Elasticsearch store; its Design Choice 11, Dilemma D7, decides where the writer runs), #1778 (entity-change contract, ADR-036), #2424 (the writer swallows persistence failures), #1200 (purge of processed outbox messages), #1251 (outbox row claiming), #2450 (integration tests of the entity-change stores), #2987 (MongoDB stores and the request's session), ADR-036, ADR-050, ADR-054, ADR-062
> **Complexity**: High (a shared writer and handler in one package, enlistment and a read-by-id on nine store classes across the 10 database providers, two sink wirings; no new persistence)
> **Estimated Scope**: ~1,000 lines of production code + ~2,800 lines of tests + ~300 lines of documentation, in nine pull requests of at most 12 files and 1,200 lines each

---

## Summary

### What exists today (read on 2026-11-26, `origin/main` `8206ffc70`)

- **None of the pieces this issue builds on is on `main`.** `IEntityChangeAuditStore` does not exist (`git grep IEntityChangeAuditStore -- src` is empty); the store is `IAuditLogStore` (`src/Encina.DomainModeling/Auditing/IAuditLogStore.cs`), returning `Task`, with one write method `LogAsync`, implemented by `AuditLogStoreADO` and `AuditLogStoreDapper` (three databases each), `AuditLogStoreEF`, `AuditLogStoreMongoDB` and `InMemoryAuditLogStore`. The Kafka and Redis Streams composite stores exist only as plans (#580, #581); ADR-036 and #1778 reshape the contract.
- **The writer runs after the commit and swallows failures.** `AuditInterceptor` captures entries in `SavingChanges` and persists them in `SavedChanges` and `SavedChangesAsync` (`src/Encina.EntityFrameworkCore/Auditing/AuditInterceptor.cs:140-161`); both persist paths catch the exception and only log it (`:396-399`, `:441-444`), and the pending list is an `AsyncLocal` (`:72`). #2424 records the debt and #577 D7 decides where the writer should run; neither is decided.
- **The stores of the three relational families do not join a caller's transaction.** `AuditLogStoreADO` takes only an `IDbConnection` (`src/Encina.ADO.SqlServer/Auditing/AuditLogStoreADO.cs:22, 32`) and `AuditLogStoreDapper` the same (`src/Encina.Dapper.SqlServer/Auditing/AuditLogStoreDapper.cs:27, 37`); the MongoDB store takes an `IMongoClient` and no session accessor (`AuditLogStoreMongoDB.cs:35-36`). `AuditLogStoreEF` adds the entity and calls `SaveChangesAsync` itself (`AuditLogStoreEF.cs:47-48`), on the scoped `DbContext`. The outbox stores do join: `OutboxStoreADO` and `OutboxStoreDapper` take an optional `IDbTransactionAccessor` and enlist every command in the scope's transaction (`src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs:17-41`), `OutboxStoreMongoDB` an optional `IMongoSessionAccessor` (`OutboxStoreMongoDB.cs:19, 32-37`), and `OutboxStoreEF` adds to the same `DbContext`.
- **The outbox and its transaction contract exist and are opt-in.** `OutboxOrchestrator.AddAsync<TNotification>` serializes the notification, creates a message with a new `Guid` (`Guid.NewGuid()`) and calls `IOutboxStore.AddAsync`; it never saves or commits (`src/Encina.Messaging/Outbox/OutboxOrchestrator.cs:87-114`), and it is registered scoped (`MessagingServiceCollectionExtensions.cs:283`). `ITransactionLeaseSource.BeginOrJoinAsync` gives the scope's one transaction to the first caller (the owner, the only one that commits) and a non-owning lease to later callers whose rollback marks the transaction rollback-only (`src/Encina.Messaging/Transactions/ITransactionLeaseSource.cs`); it is registered for the three relational families (`IDbTransactionAccessor`, `MessagingServiceCollectionExtensions.cs:307`, EF at `ServiceCollectionExtensions.cs:248`) and MongoDB (`IMongoSessionAccessor`, `Encina.MongoDB/ServiceCollectionExtensions.cs:513`). `TransactionalOutboxPipelineBehavior` is the precedent: begin or join, write, roll back on any `Left`, commit (`TransactionalOutboxPipelineBehavior.cs:100-160`). `config.UseOutbox` is the opt-in flag (`MessagingConfiguration.cs:87`).
- **The outbox publishes through `IEncina.Publish`.** The processor deserializes each message and calls `Publish`; a `Left` or exception marks the message failed, retried and finally dead lettered (`src/Encina.Messaging/Outbox/OutboxProcessorBase.cs:24-25, 126-171`). A transport is reached by an `INotificationHandler<T>` (`src/Encina/Abstractions/INotificationHandler.cs:36`); the issue's phrase "publishes through the existing transport" therefore means a handler that calls the sink.
- **The outbox never forgets a processed row yet.** `IOutboxStore` has no purge; #1200 (open) adds a 30-day default purge of processed messages and states that dead-lettered messages are never purged automatically. Whatever the outbox holds therefore outlives the audit retention unless it holds no values.
- Not in `src`: any outbox row, handler or option that carries an audit entry; a marker for a store that enlists in the scope's transaction; any audit-stream error code.

### What this plan delivers

An opt-in way to make the entity-change audit entry and its streaming message commit together, for every composite sink (Kafka, Redis Streams, any later one):

1. **One shared piece in `Encina.Security.Audit`**: `EntityChangeAuditStreamed` (an `INotification` that carries identifiers, never values), `EntityChangeAuditOutboxWriter` (begin or join the scope's transaction, run the inner store write, add the message, commit if it owns the transaction, roll back on any `Left`), `EntityChangeAuditStreamedHandler` (reads the entry back by tenant and id through the new additive interface `IEntityChangeAuditEntryReader` and hands it to the named `IEntityChangeAuditStreamSink`), the marker `IEntityChangeAuditStoreEnlistsInTransaction` and a start-up check.
2. **Enlistment and the read-by-id on the nine store classes** that cover the ten database providers and the in-memory store (ADO.NET x3, Dapper x3, EF Core, MongoDB, `InMemoryEntityChangeAuditStore`), with ten provider integration tests (rollback leaves neither entry nor message).
3. **The `PublishThroughOutbox` option** of the two composite stores, default `false`, wired in the Kafka and Redis Streams packages after #580 and #581 land.

Atomic unit, stated precisely: the entry write and the message write commit or roll back together, in the transaction the writer runs in. Whether that transaction is also the business change's depends on where the writer runs, which #577 D7, #1778 and #2424 decide, and the feature behaves differently under each answer (choice 1 states all three: the combination is supported under D7 B and D7 C, and refused under D7 A, where the entry is already an outbox message).

- **Affected packages**: `Encina.Security.Audit` (shared piece), `Encina.ADO.{SqlServer,PostgreSQL,MySQL}`, `Encina.Dapper.{SqlServer,PostgreSQL,MySQL}`, `Encina.EntityFrameworkCore`, `Encina.MongoDB` (store enlistment), the Kafka and Redis Streams audit packages of #580 and #581. The issue also ticks `Encina.Messaging`: it gets no code.
- **Provider category**: Database (10), because the entry and the message must share one unit of work on every provider (`AGENTS.md` section 5); the transports are only sinks (Kafka, Redis Streams), outside the matrix.
- **Shared pieces**: this plan owns everything under `Encina.Security.Audit/Streaming/`; #580 and #581 own their composite stores and sinks and consume the option and the shared registration; #1778 owns the main contract this plan assumes and does not change; the reader interface of choice 3 is this plan's.

---

## Design Choices

<details>
<summary><strong>1. What is atomic, and where the writer runs</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The unit is {inner write, outbox message}: the writer begins or joins the scope's transaction through `ITransactionLeaseSource`, runs the inner write and the outbox add inside it, and commits only when it owns the transaction; any `Left` rolls the lease back. The combination with D7 option A is refused at start-up** | Removes the failure the issue names (stored, never streamed) under D7 B and D7 C, with one code path on the ten providers; no coupling to the delivery step of D7 A | Not atomic with the business change while the writer stays after the commit; unsupported under D7 A (below) |
| **B) The same writer, but it skips its own outbox message when it runs inside an outbox delivery (an ambient flag set by the delivery step of D7 A)** | Supported under all three D7 answers | Couples this plan to the implementation of D7 A (the flag has no owner until #1778 or #2424 builds that delivery step); under D7 A the skip means `PublishThroughOutbox` does nothing and the sink step runs inside the retried delivery |

What each answer of #577 D7 means for this feature (D7, Design Choice 11 of the plan of #577, is open):

| D7 answer (as written in #577) | Where the composite's write runs | Effect of this plan's writer |
|--------------------------------|----------------------------------|------------------------------|
| **A**: the writer records the entry as an outbox message before the commit; a delivery step writes it to the backend later | In the outbox processor's scope, after the delivery message was picked up | The writer would own a new lease there and enqueue a second, chained message, while the entry already travels by value in the first message; the first message's retry already repeats {inner write, publication} until both succeed, so the chain adds nothing and a by-reference message (choice 3) hides nothing, because the values are already in the outbox. Option A therefore refuses the combination; option B skips the second message |
| **B**: the writer stores the entry in `SavingChanges`, before the commit; a failed write fails the save | Inside the business save | The pair is atomic; it is also atomic with the business change only when an application unit of work already holds the lease (the writer then joins it); without one the pair commits before the business save, which is the "entry for a change that never happened" cost D7 names |
| **C**: the writer stays after the commit and surfaces the failure | After the commit, with no active transaction | The writer owns its lease; the pair is atomic and independent of the business change; #2424 covers the business side |

### Chosen Option: **A — The {inner write, message} pair in one lease; refused under D7 A** (recommended, pending the maintainer)

### Rationale

- The claim that matters is the one the issue makes, "stored but never streamed": it is removed under D7 B and C. Under D7 A it is already removed by the first message, so the option adds only a second copy and is refused rather than made to look atomic. Whether an entry should survive the rollback of the business change (#2987 says audit stores usually must stay independent) is D7's and #2987's question, not this plan's. B wins when the maintainer answers D7 with option A and still wants the sink step to be a separate outbox message; the refusal needs a registration marker from the D7 A implementation, which does not exist yet, so until then the start-up check cannot detect it and the README states the incompatibility.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the shared piece lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Security.Audit`, folder `Streaming/`, next to the entity-change contract** | The contract moves there by ADR-036; the package already references `Encina.Messaging` (`src/Encina.Security.Audit/Encina.Security.Audit.csproj`, `ProjectReference` to `Encina.Messaging`), so no new dependency edge; every sink package references it anyway | The package grows by about eight types |
| **B) Duplicate the writer and handler in each sink package** | Each sink package is self-contained | Two copies of the transaction logic that must stay identical |

### Chosen Option: **A — `Encina.Security.Audit`**

### Rationale

- The shared logic is the transaction handling, which must be identical for every sink and tested once on the ten providers; the sink packages stay thin.

**Class:** Trivial
**Settled by:** ADR-036 decision 1 (`IEntityChangeAuditStore` lives in `Encina.Security.Audit`); `src/Encina.Security.Audit/Encina.Security.Audit.csproj` (the existing `Encina.Messaging` reference)

</details>

<details>
<summary><strong>3. What the outbox message carries</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) By value: the redacted entry as JSON in the message (the issue's "same JSON the composite produces")** | Self-contained; no read at publication; the publication works even if the stored entry is later purged | A second copy of old and new values in the outbox table: the outbox purge of #1200 is not delivered, keeps dead-lettered rows forever and defaults to 30 days for processed ones, which may exceed a tenant's audit retention; ADR-036 decision 1 forbids keeping old and new values indefinitely by default |
| **B) By reference through a new additive interface: tenant segment, entry id, sink name and schema version only; the handler reads the entry through `IEntityChangeAuditEntryReader.GetAsync(tenantSegment, entryId, ct)`, implemented by the nine stores of this plan and registered next to each provider's store, and passes it to the sink** | The outbox message holds no values, so its retention, dead-lettering and the missing purge do not matter (GDPR Art. 5(1)(c) and 5(1)(e)); `IEntityChangeAuditStore` does not change, so the two composites, the stores of #575 to #579 and #582 and every fake keep compiling; implementers to touch: 9 (ADO x3, Dapper x3, EF, MongoDB, in-memory) | One extra read per publication; a purge that runs before publication turns the message into a permanent failure that dead-letters visibly; a store without a reader cannot use the option (start-up refusal) |
| **C) By value, plus a dedicated purge of audit-stream messages (processed and dead-lettered) bounded by the shortest tenant retention** | Self-contained | New purge logic on ten providers, which is #1200's scope plus dead letters; the entry copy exists until the purge runs |
| **D) By reference through a new member of `IEntityChangeAuditStore`** | One interface | Changes the contract of ADR-036 and #1778: 17 implementers to touch (the 9 stores above, the Kafka and Redis Streams composites, and the stores of the plans of #575, #576, #577, #578, #579 and #582) plus every test double |

### Chosen Option: **B — By reference through `IEntityChangeAuditEntryReader`** (recommended, pending the maintainer)

### Rationale

- B is the only option that needs no change to outbox retention and no change to the contract. The claim "the outbox holds no personal data" holds only when the writer is not D7 A: under D7 A the first message carries the entry by value (choice 1), so this choice does not make that copy disappear; it only avoids a second one. A and C win when the inner store cannot serve a read by id (a custom store); D wins when the maintainer wants the read to be part of the contract for every backend. For the sink, publishing the same entry twice (retry) is harmless because consumers deduplicate on the entry id (choice 6).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Whether the outbox requirement is added to ADR-050</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Amend the closed list of ADR-050 with the row "entity-change audit streaming with `PublishThroughOutbox`: requires the Outbox and a transaction source", enforced by the start-up check** | `AGENTS.md` section 3 allows a module to require other modules only through the closed list of ADR-050; the requirement is visible where every other requirement is | An ADR amendment, decided by the maintainer |
| **B) Treat the check as validation of an option, with no ADR row** | No ADR change | A requirement enforced at start-up outside the list the rule names: ADR-050 rule 1 says a requirement that is not in the table does not exist |

### Chosen Option: **A — Amend ADR-050**

### Rationale

- Requirements that depend on an option are already rows of the table (the Delayed retries row, conditional on `EnableDelayedRetries`, and the amendment of 2026-10-24 for `ImmediateRetries`), so option B does not comply. The amendment row is written by the documentation PR and, like every ADR change, goes to the maintainer in that PR.

**Class:** Settled
**Settled by:** ADR-050 rule 1 (`050-modules-may-require-modules.md:31`: a requirement not in the table does not exist; adding one is an amendment) and the conditional-row precedents at `:7` and `:56`; `AGENTS.md` section 3 (closed list of ADR-050)

</details>

<details>
<summary><strong>5. How the writer knows the inner store joins the transaction</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A marker `IEntityChangeAuditStoreEnlistsInTransaction` implemented by the eight provider store classes; the start-up check refuses an inner store without it (a custom or in-memory store)** | Fail closed: never silently non-atomic; trivial to implement and test | A custom store that does enlist must add the marker |
| **B) Document the requirement and trust the registration** | No new type | A store that does not enlist produces a stored-but-not-streamed or streamed-but-not-stored entry with no signal |
| **C) A runtime check that the inner write used the lease's transaction (compare transaction identity)** | Exact | Provider-specific code in the shared writer; ADO, Dapper, EF and Mongo each expose a different handle |

### Chosen Option: **A — Marker and start-up refusal** (recommended, pending the maintainer)

### Rationale

- A is the cheapest option that fails closed (`AGENTS.md` section 3). C wins when the maintainer wants the guarantee proven per call rather than declared per store.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Message identity and duplicates</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep the outbox's own random message id; duplicates are tolerated and consumers deduplicate on the entry id header that #580 and #581 already define** | No change to `OutboxOrchestrator` or the ten outbox stores; the outbox is at-least-once by design (`OutboxProcessorBase.cs:24-25`) | A retried inner write (a lost acknowledgement) adds a second message for the same entry |
| **B) Derive the message id from the entry id so the outbox rejects the second row** | No duplicate rows | `OutboxOrchestrator.AddAsync` takes no id (`Guid.NewGuid()`, `OutboxOrchestrator.cs:87-114`), so `Encina.Messaging` changes, and ten stores must handle the key conflict as success |

### Chosen Option: **A — Random id, deduplicate on the entry id** (recommended, pending the maintainer)

### Rationale

- The issue says "outbox message id derived from the entry id"; that is not what the code does, and the same entry can reach the sink twice in direct mode too (#580, Design Choice 1). B wins when an exactly-once row count in the outbox is required.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Failure of the publication step, and which tenant it acts for</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The handler returns the sink's `Left` (or lets the exception escape), so the outbox processor retries and finally dead letters the message; the tenant is the segment in the message, never an ambient value, and a missing or invalid segment is a permanent `Left`** | Errors are never swallowed in background infrastructure; fails closed on missing context | A dead-lettered message needs an operator (`RequeueExhaustedAsync` exists) |
| **B) The handler logs the failure and returns success** | Never blocks the outbox | Reports success for an entry that was not streamed |

### Chosen Option: **A — Propagate; tenant from the message**

### Rationale

- The processor handles each cycle in a scope it creates itself (`OutboxProcessorBase.cs:120-126`, `CreateScope`), with no request context, so the message is the only source of the tenant; a failure that is reported as success is exactly what the audit-streaming feature exists to prevent.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (errors are never swallowed in background infrastructure; compliance gates fail closed on missing context), `OutboxProcessorBase.cs:24-25` (a `Left` from `Publish` fails the message)

</details>

<details>
<summary><strong>8. The option is opt-in and its absence of the outbox is a start-up error</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `PublishThroughOutbox` defaults to `false`; when `true` and the outbox, the transaction source or the marker is missing, start-up fails** | Every messaging pattern is optional and disabled by default; registration completeness | Applications must switch it on |
| **B) On by default when the outbox is registered** | Atomic by default | Makes a module require another without being in ADR-050's list; changes behavior for applications that registered the outbox for other reasons |

### Chosen Option: **A — Opt-in, start-up validation**

### Rationale

- B turns an optional pattern into a default and contradicts the rule that opt-in remains the rule for everything a module does not need.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (every messaging pattern is optional and disabled by default; registration completeness), ADR-050

</details>

---

## Implementation Phases

Nine pull requests, in this order, each at most 12 files and 1,200 lines (counted by file name): **PR-1** Phase 1 part a (contracts: notification, sink, reader, marker, errors, in-memory reader); **PR-2** Phase 1 part b (handler, log, handler registration); **PR-3** Phase 2 (writer, start-up check); **PR-4** Phase 3 (ADO.NET x3); **PR-5** Phase 4 (Dapper x3); **PR-6** Phase 5 (EF Core and MongoDB); **PR-7** Phase 6 (Kafka); **PR-8** Phase 7 (Redis Streams); **PR-9** Phase 10 (documentation). Observability (Phase 8) is spread over PR-2 and PR-3; testing (Phase 9) over every PR. PR-1 to PR-6 need #1778 merged; PR-7 needs #580; PR-8 needs #581; the purge of processed outbox messages (#1200) is not required under choice 3 option B.

### Phase 1: Contracts, reader and handler

<details>
<summary>Tasks</summary>

PR-1 (contracts):

1. `src/Encina.Security.Audit/Streaming/EntityChangeAuditStreamed.cs` (public sealed record, `INotification`): `string TenantSegment`, `string EntryId`, `string Sink`, `int SchemaVersion` (1). No values, no user id, no entity id.
2. `IEntityChangeAuditStreamSink.cs` (public interface): `string Name { get; }` and `ValueTask<Either<EncinaError, Unit>> PublishAsync(EntityChangeAuditEntry entry, CancellationToken cancellationToken)`; implemented by the Kafka and Redis Streams composites (PR-7, PR-8).
3. `IEntityChangeAuditEntryReader.cs` (public interface, choice 3 option B): `ValueTask<Either<EncinaError, EntityChangeAuditEntry>> GetAsync(string tenantSegment, string entryId, CancellationToken cancellationToken)`; the nine stores implement it in their PRs (the in-memory one in PR-1, `InMemoryEntityChangeAuditStore.cs` modified, with a new `tests/Encina.UnitTests/Security/Audit/InMemoryEntityChangeAuditEntryReaderTests.cs`).
4. `IEntityChangeAuditStoreEnlistsInTransaction.cs` (public marker interface).
5. `EntityChangeAuditStreamingErrors.cs` (internal static): `audit.entity_change_stream.entry_not_found` (`ErrorCategory.Internal`), `audit.entity_change_stream.tenant_invalid` (`ErrorCategory.InvalidRequest`), `audit.entity_change_stream.sink_not_registered` (`ErrorCategory.Internal`); constant messages; every `EncinaErrors.Create` passes a named `category:`.
6. `PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.Security.Audit.json` and `changelog.d/4384-entity-change-audit-outbox-streaming.added.md` (`mechanical-fixer`).

PR-2 (handler):

7. `EntityChangeAuditStreamedHandler.cs` (public sealed, `INotificationHandler<EntityChangeAuditStreamed>`): constructor `(IEntityChangeAuditEntryReader reader, IEnumerable<IEntityChangeAuditStreamSink> sinks, ILogger<EntityChangeAuditStreamedHandler> logger, ITenantContext? tenantContext = null)`; `Handle` ignores a message whose `Sink` names another sink (returns `Right`), validates the tenant segment, reads the entry, and calls the sink; a missing entry is `Internal`; every `Left` is returned unchanged. Tenancy is on when an `ITenantContext` is registered, the same signal the composite of #580 uses (its constructor takes `ITenantContext? tenantContext`); the handler reads only whether it is present, never its value (the processor scope has no request tenant): with tenancy off the segment must be the reserved `default`, with tenancy on a segment `default` is `InvalidRequest`.
8. `Diagnostics/EntityChangeAuditStreamingLog.cs` (internal static partial, `[LoggerMessage]`): EventIds 5019 to 5023 (see Research): enqueue rolled back, publication failed, streamed entry not found, streamed tenant invalid, start-up check failed.
9. `EntityChangeAuditStreamingServiceCollectionExtensions.cs` (public static): `AddEncinaEntityChangeAuditOutboxStreaming(this IServiceCollection services)` registers the handler as `INotificationHandler<EntityChangeAuditStreamed>` (`TryAddEnumerable`); PR-3 extends it with the writer and the start-up check.
10. `PublicAPI.Unshipped.txt` and the manifest (modified, `mechanical-fixer`).
11. Tests: `tests/Encina.UnitTests/Security/Audit/Streaming/EntityChangeAuditStreamedHandlerTests.cs` (tenancy on and off, other sink ignored, missing entry), `EntityChangeAuditStreamingRegistrationTests.cs` (provider built with `ValidateOnBuild` and `ValidateScopes`, with and without `ITenantContext`), `tests/Encina.GuardTests/Security/Audit/EntityChangeAuditStreamingGuardTests.cs` (constructor arguments; the optional `tenantContext` accepts null).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina issue #4384, plan docs/plans/entity-change-audit-outbox-streaming-implementation-plan-4384.md, Phase 1. #1778 is merged: IEntityChangeAuditStore and EntityChangeAuditEntry exist in Encina.Security.Audit; the reader interface is created here, not by #1778.
TASK: Create the notification, the sink contract, the reader interface, the marker, the error and log classes, the handler and the registration of the handler, with the tests of the Phase 1 Tasks, in the two pull requests the Tasks name.
KEY RULES: the message carries identifiers only, never values; a missing or invalid tenant segment is a permanent Left (InvalidRequest, ADR-062 R3), never the ambient tenant; the handler learns whether tenancy is on only from the presence of ITenantContext; the reserved default segment only when tenancy is off, and a tenant (tenant, entry id) pair is never built by string concatenation; every Left passes through unchanged and nothing is swallowed; EncinaError.Message never reaches logs or tags (error code and exception type only, exceptions through ForLogging); constant messages; EventIds 5019-5023 inside the existing SecurityAudit range; XML docs with examples; guard tests for the public constructor and method arguments.
REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxProcessorBase.cs, src/Encina/Abstractions/INotificationHandler.cs, docs/architecture/adr/036-three-audit-stores.md, docs/architecture/adr/062-error-category-contract.md
```

</details>

### Phase 2: The writer and the start-up check

<details>
<summary>Tasks</summary>

1. `Streaming/EntityChangeAuditOutboxWriter.cs` (public sealed, scoped): constructor `(ITransactionLeaseSource transactions, OutboxOrchestrator outbox, IOutboxStore outboxStore, ILogger<EntityChangeAuditOutboxWriter> logger)`; `ValueTask<Either<EncinaError, Unit>> WriteAsync(string tenantSegment, string entryId, string sink, Func<CancellationToken, ValueTask<Either<EncinaError, Unit>>> innerWrite, CancellationToken cancellationToken)`: `BeginOrJoinAsync`; run `innerWrite`; `outbox.AddAsync(new EntityChangeAuditStreamed(...))`; `outboxStore.SaveChangesAsync`; commit the lease (a joined lease commits nothing and the owner's commit decides); any `Left`, exception or cancellation rolls the lease back first and is returned or rethrown; the commit runs with `CancellationToken.None`, as `TransactionalOutboxPipelineBehavior` does (`:160`). The writer never calls `SaveChangesAsync` of a `DbContext` and never claims the business change is part of the unit.
2. `Streaming/EntityChangeAuditStreamingStartupCheck.cs` (internal sealed `IHostedService`): for each sink that enabled `PublishThroughOutbox`, fails start-up when `OutboxOrchestrator` or `IOutboxStore` is not registered, `config.UseOutbox` is off, no `ITransactionLeaseSource` is registered, or the registered `IEntityChangeAuditStore` chain has no store with the marker (choice 5), or no `IEntityChangeAuditEntryReader` is registered (choice 3); logs EventId 5023 with the reason enum only.
3. `EntityChangeAuditStreamingServiceCollectionExtensions.cs` (modified): registers the writer (`TryAddScoped`) and the check (`TryAddEnumerable`); the sink's own registration calls it when its option is on.
4. Tests (new): `EntityChangeAuditOutboxWriterTests.cs` (unit: commit when owner, no commit when joined, rollback on an inner `Left`, on an outbox `Left` and on cancellation, the commit uses a token that is not cancelled), `EntityChangeAuditStreamingStartupCheckTests.cs` (each missing prerequisite fails start-up; the DI graph builds with `ValidateOnBuild` and `ValidateScopes`), `tests/Encina.PropertyTests/Security/Audit/EntityChangeAuditOutboxWriterProperties.cs` (FsCheck: for any sequence of inner and outbox outcomes and cancellation, the transaction commits if and only if both writes succeed); modified: the Phase 1 guard test file.
5. `PublicAPI.Unshipped.txt` and the manifest (modified, `mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Issue #4384, plan docs/plans/entity-change-audit-outbox-streaming-implementation-plan-4384.md, Phase 2. Phase 1 is merged.
TASK: Create the writer and the start-up check with the tests of the Phase 2 Tasks.
KEY RULES: model the writer on TransactionalOutboxPipelineBehavior (begin or join, write, rollback on any Left, commit); OutboxOrchestrator.AddAsync never saves, so the writer calls IOutboxStore.SaveChangesAsync inside the lease and the lease owner commits; never inject a scoped service into a singleton (the start-up check uses IServiceScopeFactory); cancellation rolls back and rethrows, never a Left; the failure of any step fails the write (fail closed); only error codes in logs; CRAP <= 10 per method (small methods for begin, run, enqueue, finish).
REFERENCE FILES: src/Encina.Messaging/Outbox/TransactionalOutboxPipelineBehavior.cs, src/Encina.Messaging/Outbox/OutboxOrchestrator.cs, src/Encina.Messaging/Transactions/ITransactionLeaseSource.cs, src/Encina.Messaging/Transactions/ITransactionLease.cs
```

</details>

### Phase 3: ADO.NET enlistment (SqlServer, PostgreSQL, MySQL)

<details>
<summary>Tasks</summary>

1. For each of `src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/Auditing/EntityChangeAuditStoreADO.cs` (the file `AuditLogStoreADO.cs` after #1778's rename; modified): add the optional constructor parameter `IDbTransactionAccessor? transactionAccessor = null` exactly as `OutboxStoreADO` has it (`OutboxStoreADO.cs:29-41`): every command enlists in the scope's transaction when one is active and runs in autocommit otherwise; implement `IEntityChangeAuditStoreEnlistsInTransaction` and `IEntityChangeAuditEntryReader` (`GetAsync` by tenant segment and entry id, with the same tenant-qualified filter as the history read); the registration passes the accessor and registers the reader next to the store (the `ServiceCollectionExtensions.cs` of each package changes only if it builds the store by hand, and then counts in the PR).
2. Integration tests (new, one per database, collections `ADO-SqlServer`, `ADO-PostgreSQL`, `ADO-MySQL`, in `tests/Encina.IntegrationTests/Security/Audit/`): `EntityChangeAuditOutboxADO<Database>IntegrationTests.cs`: (a) commit leaves one entry and one `EntityChangeAuditStreamed` outbox row; (b) an inner failure leaves neither; (c) a failure after the inner write (the outbox add made to fail) leaves neither; (d) a joined lease whose owner rolls back leaves neither; (e) for ADO-SqlServer only, the real outbox processor runs once and a fake sink receives the entry; (f) the reader returns the stored entry and `NotFound`-free `Internal` for an unknown id.
3. One unit test file `tests/Encina.UnitTests/ADO/Auditing/EntityChangeAuditStoreADOTransactionTests.cs` (new): the command takes the accessor's transaction when one is active and none otherwise, and the reader's mapping.
4. One guard test file `tests/Encina.GuardTests/ADO/Auditing/EntityChangeAuditStoreADOGuardTests.cs` (new): the `GetAsync` arguments, and that the optional accessor accepts null (the valid result, not an exception).
5. Manifests of the three packages and the Security.Audit manifest (integration targets of the writer and handler) (`mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Issue #4384, plan docs/plans/entity-change-audit-outbox-streaming-implementation-plan-4384.md, Phase 3. Phases 1 and 2 are merged; #1778 renamed AuditLogStoreADO to EntityChangeAuditStoreADO.
TASK: Make the three ADO.NET entity-change stores enlist in the scope's transaction and add the integration tests.
KEY RULES: copy the enlistment shape of OutboxStoreADO exactly; the new constructor parameter is optional and null-tolerant (a guard test asserts the valid result of null, not an exception); async database calls with a CancellationToken only; every integration class uses its shared [Collection("ADO-<Database>")] fixture with ClearAllDataAsync in InitializeAsync, never IClassFixture or a new fixture; [Trait("Category","Integration")] and [Trait("Database","<Db>")]; real databases, no justification file.
REFERENCE FILES: src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs, src/Encina.ADO.SqlServer/Auditing/EntityChangeAuditStoreADO.cs, tests/Encina.IntegrationTests/Security/Audit/OperationAuditStoreADOSqlServerIntegrationTests.cs
```

</details>

### Phase 4: Dapper enlistment (SqlServer, PostgreSQL, MySQL)

<details>
<summary>Tasks</summary>

1. The same change for `src/Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/Auditing/EntityChangeAuditStoreDapper.cs` (modified), following `OutboxStoreDapper` (`IDbTransactionAccessor?`, marker interface).
2. Integration tests (new, collections `Dapper-SqlServer`, `Dapper-PostgreSQL`, `Dapper-MySQL`): `EntityChangeAuditOutboxDapper<Database>IntegrationTests.cs` with cases (a) to (d) and (f) of Phase 3; the stores also implement `IEntityChangeAuditEntryReader`.
3. Unit test `tests/Encina.UnitTests/Dapper/Auditing/EntityChangeAuditStoreDapperTransactionTests.cs` (new), guard test `tests/Encina.GuardTests/Dapper/Auditing/EntityChangeAuditStoreDapperGuardTests.cs` (new); the three manifests (`mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Issue #4384, plan docs/plans/entity-change-audit-outbox-streaming-implementation-plan-4384.md, Phase 4. Phases 1 to 3 are merged.
TASK: Make the three Dapper entity-change stores enlist in the scope's transaction and add the integration tests.
KEY RULES: same as Phase 3; mirror OutboxStoreDapper; shared [Collection("Dapper-<Database>")] fixtures.
REFERENCE FILES: src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs, src/Encina.Dapper.SqlServer/Auditing/EntityChangeAuditStoreDapper.cs
```

</details>

### Phase 5: EF Core and MongoDB

<details>
<summary>Tasks</summary>

1. `src/Encina.EntityFrameworkCore/Auditing/EntityChangeAuditStoreEF.cs` (modified): implement the marker and `IEntityChangeAuditEntryReader`. The store already works on the scoped `DbContext` that the EF `IDbTransactionAccessor` (registered from `ServiceCollectionExtensions.cs:248`) opens its transaction on, and `OutboxStoreEF` adds to the same context, so a lease transaction covers both; the PR verifies that `SaveChangesAsync` inside an active `DbContext` transaction does not commit it (EF behavior to confirm by the integration test) and changes the store only if it does.
2. `src/Encina.MongoDB/Auditing/EntityChangeAuditStoreMongoDB.cs` (modified): optional `IMongoSessionAccessor? sessionAccessor = null` as `OutboxStoreMongoDB` has it (`:19, 32-37`), every operation uses the session when one is active; marker interface and `IEntityChangeAuditEntryReader`.
3. Integration tests (new): `EntityChangeAuditOutboxEFCore{SqlServer,PostgreSql,MySql}IntegrationTests.cs` (collections `EFCore-SqlServer`, `EFCore-PostgreSQL`, `EFCore-MySQL`) and `EntityChangeAuditOutboxMongoDbIntegrationTests.cs` (the replica-set collection `MongoDbReplicaSetCollection`, name `MongoDB-ReplicaSet`, in `tests/Encina.IntegrationTests/Infrastructure/MongoDB/ReadWriteSeparation/`, because MongoDB transactions need a replica set) with cases (a) to (d) and (f) of Phase 3.
4. Unit tests (new): `tests/Encina.UnitTests/MongoDB/Auditing/EntityChangeAuditStoreMongoDBSessionTests.cs` and `tests/Encina.UnitTests/EntityFrameworkCore/Auditing/EntityChangeAuditStoreEFReaderTests.cs`; guard tests `tests/Encina.GuardTests/MongoDB/Auditing/EntityChangeAuditStoreMongoDBGuardTests.cs` and `tests/Encina.GuardTests/EntityFrameworkCore/Auditing/EntityChangeAuditStoreEFGuardTests.cs` (new); manifests of the two packages (`mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Issue #4384, plan docs/plans/entity-change-audit-outbox-streaming-implementation-plan-4384.md, Phase 5. Phases 1 to 4 are merged.
TASK: Add the marker to the EF Core store, make the MongoDB store session-aware, and add the four integration test classes.
KEY RULES: EF: do not call SaveChangesAsync from the writer; the store's own save must not commit the lease transaction (verify with the integration test, change only if proven); MongoDB needs a replica-set session, as the existing outbox integration tests do; shared collection fixtures only; no justification files for database integration tests.
REFERENCE FILES: src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs, src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs, src/Encina.MongoDB/Transactions/IMongoSessionAccessor.cs
```

</details>

### Phase 6: Kafka sink wiring

<details>
<summary>Tasks</summary>

1. In the Kafka audit package of #580 (`EntityChangeAuditKafkaOptions`, `EntityChangeAuditStoreKafka`, `AddEncinaEntityChangeAuditKafka`): add `bool PublishThroughOutbox { get; set; } = false`; the composite implements `IEntityChangeAuditStreamSink` (name `kafka`) with its existing produce step; with the option on, `RecordAsync` calls `EntityChangeAuditOutboxWriter.WriteAsync(tenant, entryId, "kafka", innerWrite, ct)` instead of producing; with it off, the direct path is unchanged; the registration calls `AddEncinaEntityChangeAuditOutboxStreaming()` only when the option is on.
2. Tests: unit (option off keeps the direct path, option on never calls the producer from the write path), DI (missing outbox fails start-up), guard (modified), the derived contract class of the composite (modified, option-on case), and a Kafka integration test (collection `Kafka`): outbox processor to topic, and a rolled-back write that produces nothing.
3. The package's `PublicAPI.Unshipped.txt` (the new option and the sink implementation), README of the Kafka audit package (`docs-writer`), manifest, changelog fragment (`mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Issue #4384, plan docs/plans/entity-change-audit-outbox-streaming-implementation-plan-4384.md, Phase 6. Phases 1 and 2 and #580 are merged.
TASK: Add PublishThroughOutbox to the Kafka composite store and wire it through the shared writer.
KEY RULES: default false; with the option on, the write path never produces directly; the sink step is the one the handler calls, so duplicates are possible and consumers deduplicate on the entry id header; a delivery failure in the handler is a Left that the outbox retries and dead letters; no values or user id in headers.
REFERENCE FILES: docs/plans/kafka-entity-change-audit-store-implementation-plan-580.md, src/Encina.Security.Audit/Streaming/EntityChangeAuditOutboxWriter.cs
```

</details>

### Phase 7: Redis Streams sink wiring

<details>
<summary>Tasks</summary>

1. The same change in the Redis Streams audit package of #581 (`AddEncinaEntityChangeAuditRedisStreams`, sink name `redisstreams`, `PublishThroughOutbox`), with the tests of Phase 6 (including the modified derived contract class) against the existing Redis collection fixture; the package's `PublicAPI.Unshipped.txt`, README, manifest and changelog fragment through the specialists.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Issue #4384, plan docs/plans/entity-change-audit-outbox-streaming-implementation-plan-4384.md, Phase 7. Phases 1 and 2 and #581 are merged.
TASK: Add PublishThroughOutbox to the Redis Streams composite store, as Phase 6 does for Kafka.
KEY RULES: same as Phase 6; the stream append is the sink step; use the shared Redis collection fixture.
REFERENCE FILES: docs/plans/redis-streams-entity-change-audit-store-implementation-plan-581.md, src/Encina.Security.Audit/Streaming/EntityChangeAuditStreamedHandler.cs
```

</details>

### Phase 8: Observability

<details>
<summary>Tasks</summary>

1. Outbox spans and metrics are the outbox's existing ones; the writer adds no instrument. A rolled-back write is counted under the existing transaction metrics of `Encina.Messaging` (no new meter).
2. The writer and the handler tag spans only with the sink name and, when multi-tenancy is on, the tenant (ADR-054 D4); never the entry id, entity type, entity id or user id (GDPR Art. 5(1)(c)).
3. `[LoggerMessage]` EventIds 5019-5023 (Research) in `EntityChangeAuditStreamingLog.cs`; error code and exception type only.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
CONTEXT: Issue #4384, plan docs/plans/entity-change-audit-outbox-streaming-implementation-plan-4384.md, Phase 8 (inside PR-2 and PR-3).
TASK: Add the log class and the span tagging rules.
KEY RULES: [LoggerMessage] source generator; 5019-5023 are inside the registered SecurityAudit range 5000-5099 and packed; re-check them against main before the PR; no EncinaError.Message anywhere.
REFERENCE FILES: src/Encina.Security.Audit/Log.cs, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 9: Testing

<details>
<summary>Tasks</summary>

1. Unit: handler (other sink ignored, invalid tenant, missing entry, sink `Left` passed through), writer (owner, joiner, each rollback), start-up check (each prerequisite), store enlistment per family.
2. Guard (`tests/Encina.GuardTests/`): the public constructors of `EntityChangeAuditOutboxWriter` and `EntityChangeAuditStreamedHandler`, `WriteAsync` arguments (null or blank `tenantSegment`, `entryId`, `sink`; null `innerWrite`), `AddEncinaEntityChangeAuditOutboxStreaming(null)`; for the optional accessor parameters of the store constructors, a test that null is accepted.
3. Contract: none new; the derived contract class of each composite (#580, #581) already runs the shared suite, and PR-7 and PR-8 add the option-on case to it.
4. Property: `EntityChangeAuditOutboxWriterProperties` (Phase 2).
5. Integration: ten database classes (Phases 3 to 5) plus one per sink (Phases 6 and 7); rollback leaves neither entry nor message.
6. Load and benchmark: justification files; the added cost is one outbox row per audited save, covered by the outbox's own suites.

</details>

<details>
<summary>Prompt for AI Agents — Phase 9</summary>

```text
CONTEXT: Issue #4384, plan docs/plans/entity-change-audit-outbox-streaming-implementation-plan-4384.md, Phase 9.
TASK: Complete whatever tests of the Phase 9 Tasks the earlier phases did not write.
KEY RULES: tests execute real code; FakeTimeProvider, never Thread.Sleep; Shouldly through Encina.Testing.Shouldly; shared collection fixtures; the guard targets are sized to the argument-check share of each file.
REFERENCE FILES: .github/coverage-manifest/Encina.Security.Audit.json, tests/Encina.IntegrationTests/Security/Audit/
```

</details>

### Phase 10: Documentation and finalization

<details>
<summary>Tasks</summary>

1. XML documentation with examples on every new public type; `README.md` of `Encina.Security.Audit` (a streaming section), `docs/features/` page on entity-change audit streaming with the atomic unit stated as in the Summary, and the amendment row of ADR-050 if choice 4 is answered A (`docs-writer`).
2. `changelog.d/4384-entity-change-audit-outbox-streaming.added.md` (PR-1), through `mechanical-fixer`; `PublicAPI.Unshipped.txt` lines for every new public symbol.
3. Build with zero warnings; `dotnet test` for the five flags of each touched package; per-flag measurement and `--check-justifications` as `AGENTS.md` section 9 describes.

</details>

<details>
<summary>Prompt for AI Agents — Phase 10</summary>

```text
CONTEXT: Issue #4384, plan docs/plans/entity-change-audit-outbox-streaming-implementation-plan-4384.md, Phase 10 (PR-9, plus the fragments of earlier PRs).
TASK: Documentation, ADR-050 row (if approved), fragments and verification.
KEY RULES: documentation goes to docs-writer, fragments, PublicAPI lines and manifests to mechanical-fixer; never type coverage figures by hand (covref markers); English only; never name a client application.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, docs/architecture/adr/050-modules-may-require-modules.md, changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Relevance |
|--------|-----------|
| Transactional outbox (dual-write problem; Debezium outbox, MassTransit and NServiceBus outboxes) | The pattern the issue applies to the audit stream |
| ADR-036 decision 1 | Old and new values are never kept indefinitely by default; a failed write fails closed; the contract lives in `Encina.Security.Audit` |
| ADR-050 | Closed list of module requirements; the outbox row for transports |
| ADR-054 D4 | Tenant tag only when multi-tenancy is on; no ids as attributes |
| ADR-062 R1, R3, R4 | Categories of the three new error codes |
| GDPR Art. 5(1)(c) and 5(1)(e) | The streaming message carries no values (choice 3 B), so it adds no copy that needs a separate retention; under D7 A of #577 the first message already holds the entry by value (choice 1) |
| `AGENTS.md` sections 3, 5, 6, 9 | Opt-in messaging patterns, fail closed, the 10-provider matrix, integration test fixtures |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `ITransactionLeaseSource`, `ITransactionLease`, `IDbTransactionAccessor`, `IMongoSessionAccessor` | `src/Encina.Messaging/Transactions/`, `src/Encina.MongoDB/Transactions/` | The one transaction of the scope |
| `TransactionalOutboxPipelineBehavior` | `src/Encina.Messaging/Outbox/` | The pattern of the writer (begin or join, rollback on `Left`, commit) |
| `OutboxOrchestrator`, `IOutboxStore`, `OutboxProcessorBase` | `src/Encina.Messaging/Outbox/` | Enqueue; retry and dead letter |
| `OutboxStoreADO`, `OutboxStoreDapper`, `OutboxStoreEF`, `OutboxStoreMongoDB` | per provider package | The enlistment shape the audit stores copy |
| `INotificationHandler<T>` | `src/Encina/Abstractions/INotificationHandler.cs` | The delivery step |
| `AuditInterceptor`, `AuditLogStore*` (to become `EntityChangeAuditStore*`) | `src/Encina.EntityFrameworkCore/Auditing/`, per provider | The writer and the stores |
| Integration collections `ADO-*`, `Dapper-*`, `EFCore-*`, `MongoDbReplicaSetCollection` (`MongoDB-ReplicaSet`), `Kafka`, the Redis collection | `tests/Encina.IntegrationTests/Infrastructure/` | The ten provider tests and the sink tests |

Provider matrix (ten database providers; one `AuditLogStore*` class each, EF Core's one class serves the three databases; implementers of the new reader: these eight classes plus the in-memory store, nine):

| Provider | Store class (after #1778) | Enlistment | Integration test (new) | PR |
|----------|---------------------------|------------|------------------------|----|
| ADO-SqlServer, ADO-PostgreSQL, ADO-MySQL | `EntityChangeAuditStoreADO` x3 | `IDbTransactionAccessor?` as `OutboxStoreADO` | `EntityChangeAuditOutboxADO<Database>IntegrationTests` x3 | PR-4 |
| Dapper-SqlServer, Dapper-PostgreSQL, Dapper-MySQL | `EntityChangeAuditStoreDapper` x3 | `IDbTransactionAccessor?` as `OutboxStoreDapper` | `EntityChangeAuditOutboxDapper<Database>IntegrationTests` x3 | PR-5 |
| EFCore-SqlServer, EFCore-PostgreSQL, EFCore-MySQL | `EntityChangeAuditStoreEF` x1 | Same scoped `DbContext` transaction (verify) | `EntityChangeAuditOutboxEFCore<Database>IntegrationTests` x3 | PR-6 |
| MongoDB | `EntityChangeAuditStoreMongoDB` | `IMongoSessionAccessor?` as `OutboxStoreMongoDB` | `EntityChangeAuditOutboxMongoDbIntegrationTests` (collection `MongoDB-ReplicaSet`) | PR-6 |
| In-memory (not a database provider) | `InMemoryEntityChangeAuditStore` | none (no marker: refused by the start-up check) | none | PR-1 (reader only) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Security.Audit` (`SecurityAudit`) | 5019-5023 | Five ids inside the registered range 5000-5099 (5000-5012 used on `main`; 5013-5015 claimed by #798 and 5016-5018 by #752); verified free in `docs/plans` and the plan-batch registry on `origin/main` `8206ffc70`; the claim row `SecurityAudit | 5019-5023 | #4384` is added to `artifacts/decisions/plan-batch/eventid-blocks.md` by the orchestrator when this plan's PR opens; re-check at PR time. The audit store ranges 52350-52599 (#577 to #581) are not used; the Kafka and Redis sink wirings add no new ids (they reuse the ranges of #580 and #581) |

### Estimated File Count

| PR | Files | Content |
|----|-------|---------|
| PR-1 | 10 | notification, sink contract, reader interface, marker, errors (five new files), `InMemoryEntityChangeAuditStore.cs` (modified), the in-memory reader test (new), `PublicAPI.Unshipped.txt`, `Encina.Security.Audit.json`, changelog fragment |
| PR-2 | 8 | handler, log class, registration extension, `PublicAPI.Unshipped.txt` (modified), manifest (modified), handler unit test, registration test, guard test |
| PR-3 | 10 | writer, start-up check, extension (modified), `PublicAPI.Unshipped.txt` (modified), manifest (modified), writer unit test, start-up unit test, property test, guard test (modified), registration test (modified) |
| PR-4 | 12 | three ADO stores, three integration tests, one unit test, one guard test, three package manifests, the Security.Audit manifest (integration targets of the writer and handler) |
| PR-5 | 11 | three Dapper stores, three integration tests, one unit test, one guard test, three manifests |
| PR-6 | 12 | EF store, MongoDB store, four integration tests, two unit tests, two guard tests, two manifests |
| PR-7 | 12 | Kafka option and composite and registration (three modified files), the package's `PublicAPI.Unshipped.txt`, manifest, README, unit, DI, guard (modified) and derived contract (modified) tests, Kafka integration test, changelog fragment |
| PR-8 | 12 | the same for Redis Streams |
| PR-9 | 4 | the feature page, the `Encina.Security.Audit` README, the ADR-050 row, `docs/INVENTORY.md` |

Each PR is within 12 files and 1,200 lines (counted by file name). Implementers of the new interface `IEntityChangeAuditEntryReader`: the nine stores above, counted in PR-1, PR-4, PR-5 and PR-6; the main contract `IEntityChangeAuditStore` does not change, so the two composites, the stores of the plans of #575 to #579 and #582 (6) and the test fakes of the contract keep compiling (choice 3 option D would have touched 17 implementers). The `new` sites of the changed store classes are the provider `ServiceCollectionExtensions.cs` registrations and the existing store tests, which compile unchanged because the new constructor parameter is optional.

Per-file coverage targets (baseline from the manifests on `origin/main` `8206ffc70`: `Auditing/AuditLogStoreADO.cs` and `AuditLogStoreDapper.cs` in each of the six relational packages carry `{integration: 0}`, `Auditing/AuditLogStoreMongoDB.cs` carries `{integration: 0}`, and `Auditing/AuditLogStoreEF.cs` has a row, `Encina.EntityFrameworkCore.json:126`, with `{integration: 0}` and the justification "integration tests are tracked by issue #2450" (open). None of these rows has a unit or guard target. The renamed files take the targets below, set from what each PR's own tests execute: the commit and rollback cases and the reader, not history, query and purge, which #2450's suite owns and which raise the integration targets further):

| File | Unit | Guard | Property | Integration | Justification |
|------|------|-------|----------|-------------|---------------|
| `Streaming/EntityChangeAuditOutboxWriter.cs` (PR-3; integration target set in PR-4) | 90 | 30 | 70 | 80 | Unit tests drive owner, joiner and every rollback; guard covers the four constructor checks and the `WriteAsync` argument checks only; the property test explores outcome sequences; the ADO integration tests of PR-4 execute commit and rollback on real transactions, which is every line of the writer |
| `Streaming/EntityChangeAuditStreamedHandler.cs` (PR-2; integration target set in PR-4) | 95 | 40 | 0 | 60 | Unit tests cover ignore, tenant on and off, missing entry and pass-through; guard covers the constructor checks (the optional tenant context accepts null) and the null notification; the ADO-SqlServer test of PR-4 runs the real processor to a fake sink |
| `Streaming/EntityChangeAuditStreamingStartupCheck.cs` (PR-3) | 90 | 0 | 0 | 0 | Guard 0: an internal type with no `InternalsVisibleTo` for `Encina.GuardTests`; it has no integration path beyond the registration test |
| `Streaming/EntityChangeAuditStreamingServiceCollectionExtensions.cs` (PR-2, modified in PR-3) | 92 | 30 | 0 | 0 | The DI test builds the graph with `ValidateOnBuild` in each registration case; the guard test reaches the null check and a valid-services case, the argument-check share of a small registration method |
| `Streaming/EntityChangeAuditStreamed.cs`, `IEntityChangeAuditStreamSink.cs`, `IEntityChangeAuditEntryReader.cs`, `IEntityChangeAuditStoreEnlistsInTransaction.cs` (PR-1) | 0 | 0 | 0 | 0 | A record and three interfaces: no executable code to cover; reached transitively by the handler and reader tests |
| `Streaming/EntityChangeAuditStreamingErrors.cs` (PR-1) | 95 | 0 | 0 | 0 | Internal static factory methods with constant messages, each asserted by the handler tests; guard 0 because they take no argument that is checked |
| `InMemoryEntityChangeAuditStore.cs` reader (PR-1, modified; the row is whatever #1778 sets, never lowered) | unchanged | unchanged | 0 | 0 | The reader test adds only covered lines |
| `Auditing/EntityChangeAuditStoreADO.cs` and `...Dapper.cs`, per database (PR-4, PR-5; today integration 0, no unit or guard row) | 30 | 10 | 0 | 45 | Unit 30: a mocked connection reaches the enlistment branch and the reader mapping (a sibling ADO store with a mocked-connection suite measures about 38%); guard 10: the `GetAsync` argument checks of the new guard file; integration 45: the commit, rollback and reader cases execute the write path and the reader through the real database, while history, query and purge stay with #2450, so a higher target is not reachable here |
| `Auditing/EntityChangeAuditStoreEF.cs` (PR-6; row `Encina.EntityFrameworkCore.json:126`, integration 0) | 25 | 10 | 0 | 45 | Same reasoning; the EF reader test and guard file; the three EF Core integration classes execute the store inside the lease transaction; the justification text of the row, which names #2450, is updated |
| `Auditing/EntityChangeAuditStoreMongoDB.cs` (PR-6; integration 0 today) | 25 | 10 | 0 | 40 | The `MongoDB-ReplicaSet` integration test executes the write path and the reader in a session; unit tests drive the session branch; other operations stay with #2450 |

The new flags are set in the manifests of the PR that adds the tests. The package-level targets of `Encina.Security.Audit` (unit 70, guard 20, contract 15, property 8, integration 52) are not lowered. CRAP of each added method is at most 10: the writer splits begin, run, enqueue and finish into small methods.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #4384: publish streamed entity-change audit entries through the outbox so the entry write and the publication commit together. AGENTS.md rules apply: opt-in messaging patterns, fail closed, errors never swallowed in background infrastructure, the 10-database-provider matrix, shared collection fixtures for integration tests. #1778 (the entity-change contract) is merged first; #580 and #581 before PR-6 and PR-7.

IMPLEMENTATION OVERVIEW: (1) EntityChangeAuditStreamed carries identifiers only; the handler reads the entry by tenant and id and calls the named sink; (2) EntityChangeAuditOutboxWriter begins or joins the scope's transaction, runs the inner write, adds the message, commits when it owns the transaction and rolls back on any Left or cancellation; (3) a marker and a start-up check refuse inner stores that do not enlist and missing outbox prerequisites; (4) the eight store classes enlist in the scope's transaction like their outbox stores; (5) ten provider integration tests prove rollback leaves neither entry nor message; (6) PublishThroughOutbox on the Kafka and Redis Streams composites, default false; (7) documentation and the ADR-050 row.

KEY PATTERNS: model the writer on TransactionalOutboxPipelineBehavior; OutboxOrchestrator.AddAsync never saves; the commit owner is the lease owner; duplicates are tolerated and consumers deduplicate on the entry id; tenant from the message, never ambient; EventIds 5019-5023; error codes with named ADR-062 categories and constant messages; no entry, entity or user ids in telemetry; specialists for PublicAPI, manifests, changelog and documentation.

REFERENCE FILES: src/Encina.Messaging/Outbox/TransactionalOutboxPipelineBehavior.cs, src/Encina.Messaging/Transactions/ITransactionLeaseSource.cs, src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs, docs/plans/kafka-entity-change-audit-store-implementation-plan-580.md, docs/architecture/adr/036-three-audit-stores.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A write path; nothing to cache, and an audit read must be exact |
| 2 | OpenTelemetry | ✅ | The outbox's existing spans and metrics; span tags are the sink name and, only when multi-tenancy is on, the tenant (ADR-054 D4); no entry, entity or user ids |
| 3 | Structured Logging | ✅ | EventIds 5019-5023 in the `SecurityAudit` range; error codes and exception types only |
| 4 | Health Checks | ✅ | The existing outbox health check covers a stuck or dead-lettered message; the start-up check covers the wiring; no new `IEncinaHealthCheck` |
| 5 | Validation | ✅ | Constructor and method arguments guarded; the start-up check validates the prerequisites; the tenant segment of a message is validated by the handler |
| 6 | Resilience | ✅ | The outbox processor retries and dead letters a failed publication; no extra policy in the writer |
| 7 | Distributed Locks | ❌ | Row claiming for several processors is #1251's concern, not this feature's |
| 8 | Transactions | ✅ | The purpose of the issue: one lease for the entry write and the message, owner commits, any `Left` rolls back |
| 9 | Idempotency | ✅ | The entry id travels in the message; a duplicate publication is tolerated and deduplicated by consumers (choice 6) |
| 10 | Multi-Tenancy | ✅ | The tenant segment travels in the message (reserved `default` only when tenancy is off; a real tenant `default` is rejected; no concatenated `tenant:id` keys); a missing or invalid segment is a permanent `Left` |
| 11 | Module Isolation | ❌ | `ModuleId` stays on the stored entry, which the handler reads back; the message carries none |
| 12 | Audit Trail | ✅ | This streams the entity-change audit trail itself and keeps the stored copy and the streamed copy consistent |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|--------------|-------|--------|
| #1778 (entity-change contract, ADR-036) | Open | Blocks PR-1 to PR-6: the contract and the renamed stores; the read of one entry by tenant and id is not in the ADR-036 contract and is added here as the separate interface `IEntityChangeAuditEntryReader` (choice 3), so #1778 needs no change |
| #2450 (integration tests of the entity-change stores, open) | Open | Owns the history, query and purge integration cases of these stores; PR-4 to PR-6 set only the targets their own tests reach and the rows are raised further by #2450 |
| #580 (Kafka composite store) and #581 (Redis Streams composite store) | Plans on `main`, issues open | Block PR-7 and PR-8 |
| #577 D7 (where the writer runs), #2424 (the writer swallows failures) | Open; D7 pending the maintainer | Not blocking, but the feature differs per D7 answer (choice 1): supported under B and C, refused under A |
| #1200 (purge of processed outbox messages) | Open | Not needed under choice 3 option B; it becomes a prerequisite if choice 3 is answered A |
| #1251 (outbox row claiming for several processors) | Open | Independent |
| #240 (Redis Streams transport) | Open | Not needed: the Redis Streams sink uses the audit package of #581 |
| #2987 (MongoDB stores outside the request's session escape its transaction; open) | Open | Its body says audit stores usually must stay independent of the business transaction; the MongoDB store here enlists only in the lease the writer owns or joins (choice 1), and PR-6 states its decision in the store's XML documentation |

---

## Next Steps

1. Commit this plan, open its PR and link it from #4384.
2. When #4384 is picked up, re-check the choices against today's code (in particular the renames of #1778 and the answer of #577 D7) and ask the maintainer about the Dilemmas (choices 1, 3, 5 and 6).
3. Brief workers for PR-1 to PR-9 in order, after #1778; PR-7 after #580 and PR-8 after #581.
