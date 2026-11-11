# Implementation Plan: Domain Events to the Outbox in the Same Commit — All 10 Database Providers

> **Issue**: [#1236](https://github.com/dlrivada/Encina/issues/1236) (SPEC-002 P-34, REQ-043, AC-039, scenario S1; parent [#1186](https://github.com/dlrivada/Encina/issues/1186))
> **Type**: Feature
> **Complexity**: High (one opt-in mode, four different transaction-enlistment mechanisms across the 10 providers)
> **Estimated Scope**: ~900-1,300 lines of production code + ~2,500-3,200 lines of tests; ~10 new and ~22 modified production files (split into 3 pull requests, see Next Steps)

---

## Summary

Domain events raised by aggregates are dispatched in process today, after `SaveChanges` (EF Core only). A crash after commit loses them, and dispatch before an explicit transaction commits publishes events for state that later rolls back. This plan adds an opt-in delivery mode in which every raised domain event is serialised into an outbox row inside the same transaction as the state change, on all 10 database providers: a rolled-back command leaves no outbox row, a committed one leaves exactly one (AC-039, S1). In-process dispatch stays the default (AGENTS.md section 3: every messaging pattern is optional).

### What exists today (verified on `main` at `7a0049c0`)

| Area | Current state | Evidence |
|------|---------------|----------|
| EF Core dispatch | `DomainEventDispatcherInterceptor` collects events in `SavingChanges` and publishes through `IEncina.Publish` in `SavedChanges`; its remarks say dispatch runs before an explicit transaction commits | `src/Encina.EntityFrameworkCore/DomainEvents/DomainEventDispatcherInterceptor.cs` (about `:100-130`, `:250`, `:320`) |
| Options | `DomainEventDispatcherOptions` (`Enabled`, `StopOnFirstError`, `RequireINotification`, `ClearEventsAfterDispatch`, `CollectEventsBeforeSave`); config `DomainEventsOptions` under `MessagingConfiguration.UseDomainEvents` | `src/Encina.EntityFrameworkCore/DomainEvents/DomainEventDispatcherOptions.cs:37-123`; `src/Encina.Messaging/DomainEvents/DomainEventsOptions.cs`; `src/Encina.Messaging/MessagingConfiguration.cs:306` |
| Registration | `RegisterDomainEvents` runs only when `UseDomainEvents` is true, and only in EF Core | `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:366-383` |
| Dapper, ADO, MongoDB | No domain-event dispatch of any kind | grep for `IDomainEvent` and `DomainEvents` finds hits only in `Encina.DomainModeling` and EF Core |
| Aggregate abstractions | `IAggregateRoot`, `AggregateRoot.RaiseDomainEvent`, `Entity.DomainEvents`; `IDomainEventCollector` for manual dispatch | `src/Encina.DomainModeling/` |
| Outbox | `IOutboxStore.AddAsync`, `OutboxWriter` (serialises with the runtime type), `OutboxMessage.NotificationType` (assembly-qualified string) | `src/Encina.Messaging/Outbox/IOutboxStore.cs:31`, `OutboxWriter.cs:60`, `IOutboxMessage.cs:21` |
| Same-transaction outbox for requests | `TransactionalOutboxPipelineBehavior` writes the notifications of `IHasNotifications` requests inside the lease (#718) | `src/Encina.Messaging/Outbox/TransactionalOutboxPipelineBehavior.cs:42`, `:163` |
| Transaction enlistment | `ITransactionLeaseSource`; `IDbTransactionAccessor` (ADO, Dapper), `DbContextTransactionAccessor` (EF), `IMongoSessionAccessor` (MongoDB) | `src/Encina.Messaging/Transactions/`, `src/Encina.EntityFrameworkCore/Transactions/DbContextTransactionAccessor.cs:42`, `src/Encina.MongoDB/Transactions/IMongoSessionAccessor.cs:21` |
| Stable message names (P-19) and persisted context (#1164) | Do not exist; the outbox stores the assembly-qualified type name and has no request-context column | SPEC-002 REQ-032 "Missing"; `IOutboxMessage.cs:21` |
| Per-handler completion (P-18) | Does not exist (SPEC-002 REQ-031 "Missing") | `docs/specifications/SPEC-002-eu-regulatory-readiness.md` |
| Existing atomicity tests | Base classes and per-provider tests for the 10 providers | `tests/Encina.IntegrationTests/Messaging/Outbox/OutboxAtomicityTestsBase.cs`, `.../ADO/*/Outbox/`, `.../Dapper/*/Outbox/`, `.../Infrastructure/EntityFrameworkCore/*/Outbox/`, `.../Infrastructure/MongoDB/Outbox/OutboxAtomicityMongoDBTests.cs` |

### Standards covered

SPEC-002 REQ-043, AC-039, S1; transactional outbox pattern (at-least-once publishing); AGENTS.md section 3 (Outbox, Transactions, errors never swallowed), ADR-048 and ADR-050 (module requirements).

### Affected packages

- `Encina.Messaging`: delivery mode, writer, validation, diagnostics.
- `Encina.DomainModeling`: tracking hook for non-EF providers.
- `Encina.EntityFrameworkCore`: interceptor outbox mode.
- `Encina.ADO.{SqlServer,PostgreSQL,MySQL}`, `Encina.Dapper.{SqlServer,PostgreSQL,MySQL}`, `Encina.MongoDB`: unit-of-work flush before commit.

### Provider category

**Database (10)**: ADO.NET x3, Dapper x3, EF Core (one project, tested on SqlServer, PostgreSQL and MySQL) and MongoDB (replica set). Each provider enlists through its own transaction mechanism; the writer is shared.

---

## Design Choices

<details>
<summary><strong>1. Opt-in shape — a delivery mode on the existing domain-events configuration</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `DomainEventsOptions.Delivery` enum (`InProcess` default, `Outbox`) under the existing `UseDomainEvents`** | One place to configure domain events; modes are exclusive by construction (no double delivery) | `UseDomainEvents` must now also be honoured by Dapper, ADO and MongoDB |
| **B) A new flag `UseDomainEventsOutbox` beside `UseDomainEvents`** | No change to the existing options | Two flags can contradict (both on); two registration paths |
| **C) Replace in-process dispatch by the outbox for everyone** | Simplest runtime | Breaks the rule that messaging patterns are optional and disabled by default |

### Chosen Option: **A — a `Delivery` mode on `DomainEventsOptions`**

**Class:** Settled
**Settled by:** issue #1236 comment ("Opt-in mode, as proposed"), AGENTS.md section 3 (every messaging pattern is optional and disabled by default)

### Rationale

- In `Outbox` mode the EF interceptor skips the in-process publish in `SavedChanges`; the outbox processor delivers later. The modes never run together, which is what "exactly one row, no direct dispatch" needs.
- The default stays `InProcess`, so applications that do not opt in see no change.

</details>

<details>
<summary><strong>2. Where events are gathered on the providers without a change tracker</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The unit of work tracks aggregates (`IUnitOfWork` gains a tracking call used by repositories and `IDomainEventCollector`) and flushes their events to the outbox before commit** | Same mental model as EF's change tracker; no change to handlers | Applications that do not use repositories or the collector must call the tracking method themselves |
| **B) A pipeline behavior writes the events of the handler's response, like `IHasNotifications`** | Reuses the #718 behavior | Only covers requests that return the aggregate or events; misses events raised on tracked entities not in the response |
| **C) Require EF Core for this mode** | One mechanism | Violates the 10-provider rule; Dapper, ADO and MongoDB users get nothing |

### Chosen Option: **A — unit-of-work tracking and flush before commit** (recommended, pending the maintainer)

### Rationale

- We recommend A because the issue asks for Dapper and ADO.NET through `IUnitOfWork` and MongoDB through a session transaction; the unit of work already owns commit and rollback on those providers (`UnitOfWorkDapper` takes `IDbTransactionAccessor`).
- The flush runs inside `SaveChangesAsync`/`CommitAsync` before the commit, through the shared `DomainEventOutboxWriter`, so a `Left` or an exception rolls back the state change and the rows together.
- B wins if the maintainer wants no new tracking API and accepts that only events carried by the request response are written; it is also complementary and already exists for `IHasNotifications`.

</details>

<details>
<summary><strong>3. Serialisation and identity — the existing outbox factory now, stable names later</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Write through `OutboxWriter` and `IOutboxMessageFactory` (assembly-qualified name today); one place changes when P-19 and #1164 land** | Works now; no dependency on missing features | Rows written now carry the old type name until P-19 |
| **B) Wait for P-19 and #1164** | Final format from the first row | Blocks the feature on two unplanned issues |

### Chosen Option: **A — existing writer and factory**

**Class:** Trivial
**Settled by:** issue #1236 (Proposed Solution mentions P-19 and #1164, neither exists in `src/`); the single seam is `OutboxWriter` at `src/Encina.Messaging/Outbox/OutboxWriter.cs:60`

### Rationale

- The event is written with its runtime type like every outbox notification; P-19 later replaces the type name inside the factory and the serializer, with no change in this plan's code.
- The event's own id (when `IDomainEvent` exposes one) becomes the outbox row id, so a retried flush cannot create a second row; this is checked in Phase 1 against `Encina.DomainModeling`.

</details>

<details>
<summary><strong>4. Outbox dependency — fail start-up when the mode is on without the outbox</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Startup validation fails with a clear error when `Delivery = Outbox` and `UseOutbox` is false** | No silent enabling; explicit configuration | One more setting to turn on |
| **B) Enable the outbox automatically** | Less configuration | A pattern turns on without being asked, against AGENTS.md section 3 |
| **C) Fall back to in-process dispatch** | Never breaks start-up | Silently weaker guarantee; the application believes it has at-least-once delivery |

### Chosen Option: **A — fail start-up** (recommended, pending the maintainer)

### Rationale

- We recommend A because a module may require another only through the closed list of ADR-050, enforced by registration errors or startup validation; this requirement (domain-event outbox needs the outbox and a transaction) is added to that list in the ADR written in Phase 9. Silent fallbacks are the failure the rule on swallowed errors forbids.
- B wins when the maintainer prefers convenience over explicitness; C is rejected because it hides a weaker guarantee.

</details>

<details>
<summary><strong>5. No transaction available — fail closed</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) With no active transaction (or a MongoDB standalone server) the flush returns a `Left` and the save fails** | Never writes events without atomicity | Callers must run inside the unit of work's transaction |
| **B) Write the rows in their own transaction** | Always works | Not the same commit; the guarantee the issue asks for is lost |

### Chosen Option: **A — fail closed**

**Class:** Settled
**Settled by:** ADR-050 (MongoDB without a replica set is refused for Transactions), AGENTS.md section 3 (compliance and security gates fail closed; errors are not swallowed), issue #1236 acceptance ("a rolled-back command leaves no outbox row and a committed one leaves exactly one")

### Rationale

- The `Left` carries the code `domain_events.transaction_required`; the existing `TransactionErrors.RollbackOnly` path of the #718 behavior is the model.

</details>

---

## Implementation Phases

### Phase 1: Core Writer, Options and Validation (`Encina.Messaging`, `Encina.DomainModeling`)

> **Goal**: The shared mechanism that turns raised events into outbox rows inside a lease.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/DomainEvents/DomainEventDelivery.cs` — enum `InProcess = 0`, `Outbox = 1`; `DomainEventsOptions.Delivery` property.
2. `src/Encina.Messaging/DomainEvents/DomainEventOutboxWriter.cs` — `WriteAsync(IReadOnlyCollection<INotification> events, ct)` returning `Either<EncinaError, int>`; requires an active lease from `ITransactionLeaseSource` (else `domain_events.transaction_required`); uses `OutboxWriter`; one row per event; the event id is the row id when available.
3. `src/Encina.Messaging/DomainEvents/DomainEventOutboxOptionsValidator.cs` — fails start-up when `Delivery = Outbox` and `UseOutbox` is false (Design Choice 4).
4. `src/Encina.Messaging/DomainEvents/DomainEventErrors.cs` — `domain_events.transaction_required`, `domain_events.not_notification`, `domain_events.write_failed`.
5. `src/Encina.DomainModeling/IUnitOfWork.cs` (modify) — a `TrackAggregate`-style member (name fixed in the implementation pull request; PublicAPI updated); `IDomainEventCollector` reused to drain events.
6. `src/Encina.Messaging/MessagingConfiguration.cs` (modify) — expose the `Delivery` setting through `DomainEventsOptions`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #1236 (domain events to the outbox in the same commit).

CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0, Railway Oriented Programming.
- Plan: docs/plans/domain-events-outbox-implementation-plan-1236.md, Design Choices 1-5.

TASK:
Create the delivery enum, DomainEventOutboxWriter, its validator and errors, and the unit-of-work tracking hook as
listed in the Phase 1 Tasks. Add every public symbol to the PublicAPI.Unshipped.txt of each package.

KEY RULES:
- In-process dispatch stays the default; the new mode is opt-in.
- The writer needs an active lease; with none it returns Left (fail closed), never opens its own transaction.
- A Left from OutboxWriter or the store fails the whole operation; never report success.
- Event payloads never reach logs or error metadata; only codes and counts.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxWriter.cs and TransactionalOutboxPipelineBehavior.cs
- src/Encina.Messaging/Transactions/ITransactionLeaseSource.cs
- src/Encina.DomainModeling/IUnitOfWork.cs and IDomainEventCollector.cs
- src/Encina.Messaging/DomainEvents/DomainEventsOptions.cs
```

</details>

---

### Phase 2: EF Core — Interceptor Outbox Mode

> **Goal**: Events become outbox entities in the same `SaveChanges`.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.EntityFrameworkCore/DomainEvents/DomainEventDispatcherInterceptor.cs` (modify) — in `Outbox` mode, `SavingChanges` and `SavingChangesAsync` add one `OutboxMessage` per collected event to the same `DbContext` (the same `SaveChanges`, so the same implicit or explicit transaction); `SavedChanges` skips publishing; events are cleared after the save succeeds.
2. `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs` (modify) — `RegisterDomainEvents` reads `Delivery`, registers the validator.
3. Events whose type is not an `INotification` return `domain_events.not_notification` (as `RequireINotification` does today).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #1236.

CONTEXT:
- Plan: docs/plans/domain-events-outbox-implementation-plan-1236.md; Phase 1 exists.

TASK:
Add the Outbox delivery mode to DomainEventDispatcherInterceptor and its registration.

KEY RULES:
- Rows are added to the DbContext being saved; do not call SaveChanges recursively and do not open a second context.
- In Outbox mode nothing is published in process.
- A save that fails leaves neither state nor outbox rows (same SaveChanges).
- CRAP <= 10 on every changed method: extract one private method per mode.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/DomainEvents/DomainEventDispatcherInterceptor.cs
- src/Encina.EntityFrameworkCore/Outbox/OutboxMessageFactory.cs
- src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:366-383
```

</details>

---

### Phase 3: ADO.NET and Dapper — Unit-of-Work Flush (6 providers)

> **Goal**: The unit of work writes the events inside its transaction before commit.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/UnitOfWork/UnitOfWorkADO.cs` (modify, 3 files) — before commit, drain tracked events and call `DomainEventOutboxWriter.WriteAsync`; on `Left` roll back and return it.
2. `src/Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/UnitOfWork/UnitOfWorkDapper.cs` (modify, 3 files) — same, using `IDbTransactionAccessor.Current`.
3. Provider `ServiceCollectionExtensions.cs` (6 files) — register the writer and validator when `UseDomainEvents` is on (they had no handling of the flag).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #1236.

CONTEXT:
- Plan: docs/plans/domain-events-outbox-implementation-plan-1236.md, Design Choice 2. Dapper and ADO have no domain-event dispatch today.

TASK:
Modify the six unit-of-work classes to flush tracked events through DomainEventOutboxWriter before commit, and register
the writer in the six provider ServiceCollectionExtensions.

KEY RULES:
- The six providers differ only in the transaction accessor type they pass; the flush logic is shared.
- Database calls are async with CancellationToken; never IDbConnection.Open().
- A Left from the writer rolls back the transaction and surfaces as the unit-of-work result.
- Registration completeness: a DI test with ValidateOnBuild and ValidateScopes.

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/UnitOfWork/UnitOfWorkDapper.cs
- src/Encina.ADO.SqlServer/UnitOfWork/UnitOfWorkADO.cs
- src/Encina.Messaging/Transactions/IDbTransactionAccessor.cs
```

</details>

---

### Phase 4: MongoDB — Session Transaction Flush

> **Goal**: The MongoDB unit of work writes the rows through the client session.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.MongoDB/UnitOfWork/` (modify) — flush through `DomainEventOutboxWriter` with the session from `IMongoSessionAccessor`; no session or a standalone server returns `domain_events.transaction_required` (Design Choice 5).
2. `src/Encina.MongoDB/ServiceCollectionExtensions.cs` (modify) — register writer and validator.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #1236.

CONTEXT:
- Plan: docs/plans/domain-events-outbox-implementation-plan-1236.md, Design Choices 2 and 5.

TASK:
Add the flush to the MongoDB unit of work and register the writer.

KEY RULES:
- Writes go through the session (InsertOneInSessionAsync); no write outside the session.
- A standalone server fails closed.

REFERENCE FILES:
- src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs
- src/Encina.MongoDB/Transactions/MongoSessionAccessor.cs
- tests/Encina.LoadTests/Messaging/OutboxAtomicityMongoDB.md
```

</details>

---

### Phase 5: Configuration, DI and Auto-Registration

> **Goal**: One consistent way to turn the mode on.

<details>
<summary><strong>Tasks</strong></summary>

1. `MessagingConfiguration` wiring so `config.UseDomainEvents = true; config.DomainEventsOptions.Delivery = DomainEventDelivery.Outbox;` works on all four provider families.
2. Validator registered with `ValidateOnStart`; DI test builds each provider's container with `ValidateOnBuild` and `ValidateScopes`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #1236.

CONTEXT:
- Plan: docs/plans/domain-events-outbox-implementation-plan-1236.md.

TASK:
Finish configuration and DI for all provider families and add the DI tests.

KEY RULES:
- AddEncina* registers every option and dependency it resolves; satellites use TryAdd*.
- The mode is off unless asked for.

REFERENCE FILES:
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- tests/Encina.UnitTests/Messaging/Transactions/TransactionalOutboxRegistrationTests.cs
```

</details>

---

### Phase 6: Cross-Cutting Integration

> **Goal**: Tenancy, idempotency and validation.

<details>
<summary><strong>Tasks</strong></summary>

1. Tenant: the outbox rows carry the tenant like any outbox row written by the request context; a two-tenant test shows tenant A's events are never written under tenant B.
2. Idempotency: the event id as row id (Design Choice 3); P-18 consumes it later.
3. Validation: `DomainEventOutboxOptionsValidator` (Phase 1).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #1236.

CONTEXT:
- Plan: docs/plans/domain-events-outbox-implementation-plan-1236.md, cross-cutting matrix.

TASK:
Add the tenancy and idempotency behaviour and their tests.

KEY RULES:
- The tenant id comes from the request context; with tenancy off a fixed default tenant applies.
- A repeated flush of the same event never creates a second row.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxWriter.cs
- src/Encina/Abstractions/IRequestContext.cs
```

</details>

---

### Phase 7: Observability

> **Goal**: Activity, counter and logging (REQ-062).

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs` — register `MessagingDomainEventOutbox = (5700, 5749)`; add it to the `Encina.Messaging` entry of `AssemblyRanges` in `EncinaEventIdAllocationTests.cs`.
2. `src/Encina.Messaging/DomainEvents/DomainEventOutboxDiagnostics.cs` — span `encina.domain_events.enqueue` with `encina.tenant_id` and `encina.message_type`; counter `encina.domain_events.enqueued`.
3. `DomainEventOutboxLogMessages.cs` — `[LoggerMessage]` EventIds 5700-5707, packed (flush started, rows written, transaction required, write failed, not a notification).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #1236.

CONTEXT:
- Plan: docs/plans/domain-events-outbox-implementation-plan-1236.md; EventIds 5700-5749.

TASK:
Register the range, map it to Encina.Messaging in the allocation test, add diagnostics and log messages, call them from the writer.

KEY RULES:
- Register the range before using an EventId; pack sequentially.
- Message type is the type name only; no payload, no subject identifier in tags or logs.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina.Messaging/Diagnostics/
```

</details>

---

### Phase 8: Testing

> **Goal**: Every flag reaches its manifest target; the 10-provider acceptance test passes.

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: writer (no lease, success, store failure), validator, interceptor modes, unit-of-work flush with fakes, DI tests.
2. Guard and Contract: one contract run on the 10 providers (rollback leaves none, commit leaves one per event).
3. Property: for any sequence of raised events and a commit or rollback outcome, rows equal events on commit and zero on rollback.
4. Integration: extend `OutboxAtomicityTestsBase` with a domain-event scenario for ADO x3, Dapper x3, EF Core x3 and MongoDB replica set; two-tenant test; in-memory exporter test; no payload in telemetry.
5. Load and benchmark `.md` justifications (issue: not a concurrent hot path).
6. Manifest entries in `Encina.Messaging.json`, `Encina.DomainModeling.json` and the provider manifests.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of issue #1236.

CONTEXT:
- Plan: docs/plans/domain-events-outbox-implementation-plan-1236.md; manifests in .github/coverage-manifest/.

TASK:
Write the tests in the Phase 8 Tasks and the manifest entries.

KEY RULES:
- Tests execute real package code; Shouldly through Encina.Testing.Shouldly.
- Shared [Collection] fixtures; ClearAllDataAsync in InitializeAsync; MongoDB uses the MongoDB-ReplicaSet collection.
- Tests of a provider-dependent feature cover all 10 providers.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Messaging/Outbox/OutboxAtomicityTestsBase.cs
- tests/Encina.ContractTests/Messaging/Transactions/TransactionalOutboxContractTests.cs
- tests/Encina.PropertyTests/Messaging/Transactions/TransactionAtomicityPropertyTests.cs
```

</details>

---

### Phase 9: Documentation and Finalization

> **Goal**: Documentation, changelog, ADR and build verification.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on every public member; `changelog.d/1236-domain-events-outbox.added.md`.
2. Package READMEs of `Encina.Messaging` and `Encina.DomainModeling`; `docs/features/` page on delivery modes.
3. ADR numbered at implementation time: the delivery mode and the addition to the ADR-050 requirement list (Design Choice 4).
4. PublicAPI files; build with zero warnings; per-flag coverage and CRAP table.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of issue #1236.

CONTEXT:
- Plan: docs/plans/domain-events-outbox-implementation-plan-1236.md.

TASK:
Write the documentation, changelog fragment, ADR and PublicAPI lines listed in the Phase 9 Tasks.

KEY RULES:
- Never type coverage figures by hand; use covref markers.
- The PR records the ADR-018 evaluation of the 12 functions.
- English only.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- docs/architecture/adr/index.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Relevance to this plan |
|--------|-----------|------------------------|
| SPEC-002 | REQ-043, AC-039, S1, REQ-061, REQ-062 | Requirement, acceptance, scenario, tenancy, telemetry |
| ADR-048, ADR-050 | Pipeline order, module requirements | Where the mode requires the outbox and a transaction |
| ADR-029 | Recoverability | Retry stays the application's decision |
| Transactional outbox pattern | At-least-once publication | The mechanism |
| ADR-018, ADR-021 | Cross-cutting, EventIds | Matrix, range 5700-5749 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Use in this feature |
|-----------|----------|---------------------|
| `DomainEventDispatcherInterceptor` | `src/Encina.EntityFrameworkCore/DomainEvents/` | EF outbox mode |
| `OutboxWriter`, `IOutboxStore`, `IOutboxMessageFactory` | `src/Encina.Messaging/Outbox/` | Row creation |
| `ITransactionLeaseSource`, `IDbTransactionAccessor`, `IMongoSessionAccessor` | `src/Encina.Messaging/Transactions/`, `src/Encina.MongoDB/Transactions/` | Enlistment on every provider |
| `IUnitOfWork` per provider | `src/Encina.{ADO,Dapper}.*/UnitOfWork/`, `src/Encina.MongoDB/UnitOfWork/` | Flush before commit |
| `IDomainEventCollector` | `src/Encina.DomainModeling/IDomainEventCollector.cs` | Draining events |
| `TransactionalOutboxPipelineBehavior` | `src/Encina.Messaging/Outbox/TransactionalOutboxPipelineBehavior.cs` | Fail-closed model |
| Atomicity tests | `tests/Encina.IntegrationTests/Messaging/Outbox/` | Extended, not duplicated |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (domain-event outbox) | 5700-5749 (new `MessagingDomainEventOutbox`) | 5700-5707 used; mapped to `Encina.Messaging` in the allocation test |
| Neighbours in the family | 5500-5549 (#1234), 5550-5699 (#1235), 5750-5799 (#1238) | Reserved by the sibling plans |

### Estimated File Count

| Category | New | Modified | Notes |
|----------|----:|---------:|-------|
| Core writer, options, validator, errors | 4 | 3 | Phase 1 |
| EF Core interceptor and registration | 0 | 2 | Phase 2 |
| ADO and Dapper unit of work and registration | 0 | 12 | Phase 3 |
| MongoDB | 0 | 2 | Phase 4 |
| Diagnostics | 2 | 2 | Phase 7 |
| Tests | ~14 | ~6 | Phase 8, two `.md` justifications |
| Documentation | 3 | 3 | Phase 9 |

### Findings in the Current Code

- `UseDomainEvents` is honoured only by EF Core; Dapper, ADO and MongoDB ignore it (Phases 3 and 4 give it meaning).
- The EF interceptor's own remarks admit the rollback hazard this plan removes.
- P-19, #1164 and P-18 do not exist; the plan uses the single seam in the outbox factory (Design Choice 3).

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 / C# 14 library (nullable on, Railway Oriented Programming with Either<EncinaError, T>).
Issue #1236 (SPEC-002 P-34, REQ-043, AC-039, S1) writes domain events to the outbox in the same commit as the state
change, opt-in, on all 10 database providers. Plan: docs/plans/domain-events-outbox-implementation-plan-1236.md.
Rules: AGENTS.md.

IMPLEMENTATION OVERVIEW:
1. Core: DomainEventDelivery mode, DomainEventOutboxWriter, validator, errors, unit-of-work tracking hook.
2. EF Core: interceptor adds outbox entities in the same SaveChanges; no in-process publish in Outbox mode.
3. ADO x3 and Dapper x3: unit of work flushes tracked events before commit through IDbTransactionAccessor.
4. MongoDB: flush through the session; standalone fails closed.
5. DI and configuration for all families; startup validation (Outbox mode requires UseOutbox).
6. Tenant and idempotency (event id as row id).
7. EventIds 5700-5749, diagnostics.
8. Tests on every flag across the 10 providers.
9. Docs, ADR, changelog, PublicAPI, build, coverage, CRAP.

KEY PATTERNS:
- Fail closed without a transaction; a Left rolls back state and rows together.
- In-process dispatch stays the default; the modes never run together.
- No event payload in logs, tags or errors. TimeProvider for time; async database calls with CancellationToken.
- Integration tests on shared [Collection] fixtures; every flag reaches its target; CRAP <= 10.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/DomainEvents/DomainEventDispatcherInterceptor.cs
- src/Encina.Messaging/Outbox/OutboxWriter.cs, TransactionalOutboxPipelineBehavior.cs
- src/Encina.Messaging/Transactions/
- src/Encina.{ADO,Dapper}.*/UnitOfWork/, src/Encina.MongoDB/UnitOfWork/
- tests/Encina.IntegrationTests/Messaging/Outbox/OutboxAtomicityTestsBase.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | No read path that benefits from caching |
| 2 | OpenTelemetry | ✅ | `encina.domain_events.enqueue` span and `enqueued` counter with `encina.tenant_id` (Phase 7) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` EventIds 5700-5707 in the new `MessagingDomainEventOutbox` range (Phase 7) |
| 4 | Health Checks | ❌ | No new checkable dependency; the outbox already has its own checks |
| 5 | Validation | ✅ | Startup validation that Outbox mode has the outbox enabled (Phase 1, Design Choice 4) |
| 6 | Resilience | ❌ | No call to an external system; delivery retries belong to the outbox processor |
| 7 | Distributed Locks | ❌ | No shared state updated by concurrent hosts; the outbox processor already claims rows |
| 8 | Transactions | ✅ | The whole point: event rows share the state change's transaction (Phases 2 to 4) |
| 9 | Idempotency | ✅ | The event id is the outbox row id, so a retried flush creates no second row (Design Choice 3; P-18 builds on it) |
| 10 | Multi-Tenancy | ✅ | Rows carry the tenant of the request context; fixed default tenant when tenancy is off (Phase 6, AC-043) |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ❌ | No compliance-relevant operation beyond those already audited |

---

## Prerequisites & Dependencies

| Item | State | Relation |
|------|-------|----------|
| #1186 EU regulatory readiness (SPEC-002) | open (EPIC) | Parent |
| #718 Transactional outbox atomicity | merged | The lease and the fail-closed model are reused |
| P-19 Stable message type names | open | Not blocking; replaces the type name inside the factory later (Design Choice 3) |
| #1164 Persisted request context | open | Not blocking; same seam |
| P-18 Per-handler completion | open | Not blocking; consumes the event id as idempotency key |
| #2988 MongoDB transaction pipeline behavior | open (`needs-decision`) | Independent: this plan flushes inside the unit of work's own transaction, not through a pipeline behavior |
| P-25 Reference scenario (S1) | open | Verifies this feature end to end |
| Plans #1234, #1235, #1238 | this family | EventId block 5700-5749 reserved here |

---

## Next Steps

1. The maintainer decides Design Choices 2 and 4 (the Dilemmas); the orchestrator records the answers in a `## Maintainer Decisions` section and updates each Chosen Option.
2. The feature exceeds one worker pull request (15 files, 1500 lines). Split it into three pull requests: (a) Phases 1, 5 and 7 (core writer, validation, DI, diagnostics) with the EF Core mode of Phase 2; (b) Phase 3 for the six ADO and Dapper providers; (c) Phase 4 for MongoDB with the remaining integration, property and documentation work.
3. Link this plan from #1236 and start sub-issue (a) with an `issue-worker`.
