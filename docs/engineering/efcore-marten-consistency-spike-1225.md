---
title: "Application data and compliance aggregates: keeping them consistent (spike #1225)"
layout: default
nav_exclude: true
---

# Application data and compliance aggregates: keeping them consistent (spike #1225)

This page is for the maintainer, who has to decide how an application that keeps its own data in EF Core or Dapper and its compliance records in Marten avoids personal data that no compliance record tracks, and for a contributor who needs the evidence. It records what the code does today at `origin/main` `af3742f54`, what rules and earlier decisions already settle, what other libraries do, and the dilemmas that remain, each with a recommendation. The recommendations are input, not decisions: [#1225](https://github.com/dlrivada/Encina/issues/1225) stays open until the maintainer decides, and the decisions then get an ADR.

**Status:** evidence gathered on 2026-10-25. **Issue:** [#1225](https://github.com/dlrivada/Encina/issues/1225) (v0.17.0; SPEC-002 P-23, priority P0; part of [#1186](https://github.com/dlrivada/Encina/issues/1186)). **Decision register:** `artifacts/decisions/1225.md` (a local working file of the orchestration, not in the repository; the D-keys below refer to it).

Pre-1.0, backward compatibility is not a criterion ([AGENTS.md](../../AGENTS.md) section 1). Nothing was run end to end: the Marten facts come from Marten's documentation, and the proof of concept the issue asks for is part of the work that follows the decisions.

## 1. The problem from the basics

A **dual write** is one business operation that writes to two places with two separate commits. If the process stops, or the second write fails, after the first commit, one place has the data and the other does not. No database rollback can fix it, because the first commit is already final.

Encina has exactly this shape. The nine event-sourced compliance modules (consent, retention, data subject requests, lawful basis, breach notification, DPIA, processor agreements, cross-border transfer, data residency) keep their records as Marten aggregates on PostgreSQL ([SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) DEC-008 (a), [ADR-019](../architecture/adr/019-compliance-event-sourcing-marten.md)). Applications keep their own data in EF Core or Dapper, often on the same PostgreSQL.

A concrete story. The psychology practice of the reference application registers a new patient.

1. The handler inserts the patient row with EF Core and calls `SaveChangesAsync` itself, so the row commits (an application choice; with Encina's `TransactionPipelineBehavior` the relational commit would instead happen after the whole pipeline).
2. `RetentionValidationPipelineBehavior` then calls `IRetentionRecordService.TrackEntityAsync` after the handler, which creates the retention aggregate in Marten and commits a second time.

Whichever order the two commits run in, they are two commits. If the process is restarted between them with the order above, the patient exists with health data (Art. 9) and no retention record. Nothing will ever erase that row when its retention period ends (Art. 5(1)(e)). If the second write was a consent grant, the practice may later be unable to prove consent (Art. 7(1)). SPEC-002 REQ-037 asks for a documented, tested pattern, and reference scenario S19 injects exactly this fault and expects that "both commit or are reconciled" (`SPEC-002-eu-regulatory-readiness.md:222`, `:350-351`, AC-037 at `:488`).

## 2. What the code does today

### 2.1 Two transaction systems that never meet

| Side | How it commits | Evidence |
| --- | --- | --- |
| Marten (compliance) | Every aggregate save commits on its own: `Events.Append` then `_session.SaveChangesAsync` | `src/Encina.Marten/MartenAggregateRepository.cs:206-208`, `CreateAsync` at `:298` |
| Marten joining an outside transaction | No code in `src/` builds a session on an external connection or transaction (`ForTransaction`, `ForConnection`, `SessionOptions` have no match) | Search of `src/` |
| EF Core (application) | `TransactionPipelineBehavior` takes a lease from `IDbTransactionAccessor`; only the owner commits | `src/Encina.EntityFrameworkCore/TransactionPipelineBehavior.cs` (owner rule documented at `:25`, class at `:54`) |
| ADO.NET and Dapper on PostgreSQL | `Encina.Messaging.TransactionPipelineBehavior` plus the units of work | `src/Encina.Messaging/TransactionPipelineBehavior.cs:41`, `src/Encina.ADO.PostgreSQL/UnitOfWork/UnitOfWorkADO.cs:148`, `src/Encina.Dapper.PostgreSQL/UnitOfWork/UnitOfWorkDapper.cs:157` |
| Relational outbox ([ADR-037](../architecture/adr/037-transactional-outbox.md)) | `TransactionalOutboxPipelineBehavior` joins or begins the lease and `OutboxWriter` writes the row in the same transaction | `src/Encina.Messaging/Outbox/TransactionalOutboxPipelineBehavior.cs:30`, `:106`; `OutboxWriter.cs:15` |
| Marten outbox ([ADR-050](../architecture/adr/050-modules-may-require-modules.md), [#2383](https://github.com/dlrivada/Encina/issues/2383)) | Planned: `OutboxStoreMarten` written in the Marten session, inbox enlisted in the Marten transaction; not in `src/` yet | ADR-050 line 43 |

Where the compliance modules start their aggregates: consent through `DefaultConsentService.GrantConsentAsync` (`DefaultConsentService.cs:135`, create at `:169`); retention through `DefaultRetentionRecordService.TrackEntityAsync` (`:79`), called by `RetentionValidationPipelineBehavior` after the handler runs (`:284`); data subject requests through `DefaultDSRService.SubmitRequestAsync` (`:152`). The other six modules hold the same `IAggregateRepository<T>`.

### 2.2 A rule already in the way

ADR-050 (with the [#2239](https://github.com/dlrivada/Encina/issues/2239) decision 4) makes Marten together with `TransactionPipelineBehavior` a startup error, because the relational transaction would not cover Marten and would be a false guarantee (ADR-050 line 117, table at line 45). The #2383 implementation plan widens that forbidden set to the `TransactionalOutboxPipelineBehavior` of [#718](https://github.com/dlrivada/Encina/issues/718) "when it lands", and says "Cross-store atomicity stays with #1225" (`docs/plans/es-reliable-delivery-implementation-plan-2383.md:130`, `:661`). That behavior already exists: ADR-037 records the ADO.NET, Dapper and EF Core slices of #718 as delivered, although #718 stays open for MongoDB and the removal of `OutboxPostProcessor`. So the outbox bridge of option B, as written, needs a behavior that the planned check forbids in any host that also runs Marten (dilemma K2).

### 2.3 Documentation

ADR-019 says nothing about EF Core or Dapper data next to Marten; its rejection of a dual write (line 310) is about an entity store plus an event log inside Encina. Several compliance READMEs still advertise "10 Database Providers" (for example the GDPR README line 20), which SPEC-002 P-14 already tracks. No README warns about the dual write.

### 2.4 Tests

`MartenFixture` and `EFCorePostgreSqlFixture` each start their own PostgreSQL container (`tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs:35-52`, `tests/Encina.TestInfrastructure/Fixtures/EntityFrameworkCore/EFCorePostgreSqlFixture.cs:19-32`), so no test today has both writes in one database. No test injects a process kill or network fault.

### 2.5 Claims in the issue to correct

| Claim | What the evidence shows |
| --- | --- |
| Decision criterion "Provider coverage (10 database providers)" is ticked | The issue's own scope is PostgreSQL only; SQL Server and MySQL applications are unsupported for these modules in 1.0 (SPEC-002 DEC-008 (a), line 549; ADR-019 lines 226-234). The criterion is EF Core and Dapper on PostgreSQL |
| Decision criterion "Breaking change impact" is ticked | Pre-1.0, not a criterion (AGENTS.md section 1) |
| Option A con: "PostgreSQL only" | Not a con: the modules are PostgreSQL only anyway (DEC-008 (a)) |
| "Marten supports external transactions on an `NpgsqlConnection`" | Marten documents `SessionOptions.ForConnection` and `SessionOptions.ForTransaction` ([sessions](https://martendb.io/documents/sessions.html)); nothing in the repository exercises them with Marten 9.38. The proof of concept must confirm it |
| Option B "idempotent by outbox id (P-18)" | P-18 is planned for v0.18.0 (SPEC-002 line 727), later than this spike's v0.17.0 |

## 3. What others do

| Library | Atomic scope | Source |
| --- | --- | --- |
| Marten | `ForTransaction` enlists a session in an existing Npgsql transaction; the session then does not commit, the transaction owner does. `ForConnection` reuses a connection but Marten still controls the transaction. Marten's EF Core projections run EF Core on the Marten session's connection, the reverse direction | [Sessions](https://martendb.io/documents/sessions.html), [EF Core projections](https://martendb.io/events/projections/efcore.html) |
| Npgsql EF Core | `UseNpgsql(connection)` with an open connection does not open or close it | [Npgsql EF Core](https://npgsql.org/efcore/) |
| Wolverine | EF Core middleware saves state and outbox messages together; with Marten, events and messages are saved atomically in Marten's outbox. EF Core and Marten can coexist in one application, but no documented transaction spans an EF Core write and a Marten write | [EF Core durability](https://wolverinefx.net/guide/durability/efcore/), [Marten integration](https://martendb.io/tutorials/wolverine-integration) |
| NServiceBus | The outbox stores outgoing messages in the business transaction and receivers deduplicate by message id, without coordinating two resource managers | [Outbox](https://docs.particular.net/nservicebus/outbox/) |
| MassTransit | Outbox and inbox tables mapped into the business `DbContext`, committed by `SaveChangesAsync` | [Outbox](https://masstransit.massient.com/documentation/configuration/middleware/outbox) |

Every library gets atomicity inside one store and bridges stores with an outbox and idempotent consumers. None offers atomicity across two stores without a shared transaction. No library or standard for reconciling a primary store against a compliance store was found; the closest generic ideas are outbox relays and "anti-entropy" sweeps (no source fetched).

## 4. EU and Spanish law (not legal advice)

- **Art. 5(1)(e)** (storage limitation): a patient row without a retention record is never erased by the retention machinery.
- **Art. 7(1)**: the controller must be able to demonstrate consent; a consent grant lost between two commits cannot be demonstrated.
- **Art. 5(2)** (accountability) and **Art. 30** (records of processing): the compliance aggregates are the controller's evidence.
- **Art. 9**: health data, a special category, in the reference application.
- **Art. 32(1)(b)**: integrity of processing systems.

The law does not prescribe a technique; it makes "personal data that no compliance record tracks" the failure to avoid, which is what S19 tests.

## 5. Settled points

Each has a citation; the ADR records them without reopening them.

| Point | Settled by |
| --- | --- |
| Scope: EF Core and Dapper applications on PostgreSQL next to Marten; relational stores for the modules and SQL Server or MySQL applications stay out | SPEC-002 REQ-037 (line 350), DEC-008 (a) (line 549); ADR-019 lines 226-234, 281 |
| Success means "both commit or are reconciled" after a fault between the two writes | SPEC-002 S19 (line 222) |
| While a compliance record is missing, compliance gates that need it deny (consent checks fail closed) | AGENTS.md section 3; SPEC-002 DEC-006 (line 547), INV-005 (line 514) |
| Tests run on real PostgreSQL through Testcontainers with real Encina code | AGENTS.md section 9; AC-037 (line 488) |
| The final S19 scenario belongs to the PracticeManagement reference project (P-25); this spike delivers the decision and a proof of concept, not S19 itself | SPEC-002 lines 548, 734 |
| Option A cannot rely on `TransactionPipelineBehavior` or `UseTransactions` while the ADR-050 forbidden pair stands | ADR-050 lines 43, 45, 117; #2239 decision 4 |
| The shared transaction, if any, is created inside the request's DI scope | ADR-037 lines 105, 108; ADR-048 line 52 |
| A bridge handler or store `Left` fails the operation (retry or dead letter), never reports success | AGENTS.md section 3; ADR-050 line 46 |
| A background reconciliation job, if any, runs under a distributed lock, reads time from `TimeProvider` and is tenant-scoped, restoring the tenant from persisted context | ADR-050 lines 75, 113; AGENTS.md section 3; SPEC-002 lines 384, 517 |
| The decision gets a new ADR and dated notes in ADR-019 and the module READMEs; no migration guide | AGENTS.md sections 1 and 8; ADR-019 line 15 (amendment style); SPEC-002 P-14 (line 723) |
| A new Encina API, if chosen, starts with a `[FEATURE]` issue and an implementation plan, and evaluates the 12 cross-cutting functions | AGENTS.md sections 6 and 11 |

## 6. Open decisions (dilemmas)

Effort is relative size. The register keys (D4, D10, ...) are given for traceability.

### K1. Which patterns Encina documents and tests

Register D4, D10, D25. REQ-037 names "a shared connection and transaction, or an outbox bridge"; S19 also accepts "reconciled".

| | K1a: A only (shared connection and transaction) | K1b: B only (outbox bridge) | K1c: A and B as two topologies | K1d: B plus C (reconciliation sweep) |
| --- | --- | --- | --- | --- |
| What it is | The EF Core or Dapper write and the Marten write share one PostgreSQL connection and transaction | The application write and an outbox message commit together; a handler starts the Marten aggregate | A for application and Marten data in the same database; B when they are in different databases or the compliance record may start later | B, with a periodic job that finds application rows without a compliance record |
| Better when | The practice runs on one PostgreSQL database and a patient must never exist without its records, not even briefly | The stores are in different databases, or coupling the `DbContext` and Marten session lifetimes is unacceptable | Both topologies matter for users | S19 is read literally ("or are reconciled") and dead-lettered bridge messages need a safety net |
| Pros | Zero window; simplest mental model; matches what every surveyed library does inside one store | Works across databases; reuses the delivered relational outbox | Each topology gets the pattern that wins in it | Closes B's failure cases |
| Cons | Only possible in the same database; blocked by the forbidden pair if it uses Encina's transaction behavior (K2); couples lifetimes | A window of untracked data until the handler runs; needs K2 and K4 answered | Twice the tests and pages | A job, its lock and its scanning cost |
| Effort | Medium | Medium | Large | Large |

A standalone reconciliation job (option C alone) is not listed by REQ-037, so it would need a SPEC-002 amendment; A plus C adds only protection against other writers (imports, manual SQL).

**Spike's recommendation: K1c**, with C only as B's failure sweep. The reference practice runs on one database, where A gives zero window; B covers the other topology that REQ-037 names.

### K2. Can the outbox bridge share a host with Marten?

Register D17. This is blocking for B in a single host. The #2383 plan forbids the relational `TransactionalOutboxPipelineBehavior` next to Marten, and ADR-050's closed table says a requirement or ban that is not in the table does not exist, so any change is an ADR-050 amendment.

| | K2a: keep the host-level ban; split hosts | K2b: amend ADR-050 so the ban covers only requests that touch Marten, or exempts the outbox behavior | K2c: application code writes the outbox row itself in the same `SaveChanges`, without the behavior |
| --- | --- | --- | --- |
| Better when | The simplest rule matters more than a single host | The reference practice is one host (it is) | Neither the ban nor the behavior may change |
| Pros | No rule change | One host, the delivered ADR-037 outbox works | No rule change, one host |
| Cons | B then needs a transport between hosts, which the issue does not mention | The startup check cannot see which requests touch Marten, so the narrower rule becomes a convention or an architecture test | Loses the automatic atomic outbox of ADR-037; easy to get wrong |
| Effort | Medium | Medium (ADR-050 amendment, check, tests) | Small per application, repeated by every user |

**Spike's recommendation: K2b**, exempting the outbox behavior when the request writes only relational data and its Marten work is deferred to the bridge handler. The single-host reference application is the main user.

### K3. How option A shares the transaction, and whether Encina ships a helper

Register D12, D37. Only relevant if K1 keeps A.

| Sub-choice | Options | Spike's recommendation |
| --- | --- | --- |
| **Who owns the connection and transaction** | (a) EF Core owns; Marten opens a session with `ForTransaction` on EF Core's transaction (inside `strategy.ExecuteAsync` when a retrying execution strategy is on, ADR-037 line 105); (b) Marten or Npgsql owns and EF Core joins with `UseNpgsql(connection)` (note: `InboxStoreEF` refuses a shared `DbConnection`, ADR-048 line 52); (c) an application-owned `NpgsqlConnection` shared by Dapper, EF Core and Marten | (a) for EF Core applications and (c) for Dapper ones; both to be proven by the proof of concept against Marten 9.38 |
| **Recipe or shipped API** | (a) a documented, tested recipe only; (b) a helper in `Encina.Marten` (for example a scoped session factory bound to the current relational transaction), which then needs a `[FEATURE]` plan, public API and per-flag coverage | (b): transaction ownership is the error-prone part, and a helper lets the compliance modules' `IAggregateRepository` join without each application writing glue |

### K4. Option B details

Register D5, D18, D19, D22. Only relevant if K1 keeps B.

| Sub-choice | Options | Spike's recommendation |
| --- | --- | --- |
| **What the outbox message is** | (a) a domain-event notification handled in process (AGENTS.md: "Outbox for publishing domain events"); (b) an integration event through the mapper ([ADR-028](../architecture/adr/028-domain-events-versus-integration-events.md)), so the compliance side could move to another process; (c) a dedicated, versioned command | (a) for 1.0 |
| **Idempotency of the handler** | (a) outbox id with per-handler completion (P-18), which lands in v0.18.0, after this spike's v0.17.0; (b) an inbox enlisted in the Marten transaction (needs `InboxStoreMarten`, #2383); (c) a deterministic stream id derived from the business key: a second `StartStream` collides (Marten documents `ExistingStreamIdCollisionException` for a non-partitioned store), and the handler then loads the existing aggregate and checks that it matches the command before treating the operation as done, because a collision alone does not prove the earlier start completed with the same data | (c) now, with that load-and-verify step, because it needs neither P-18 nor #2383; (b) once #2383 lands if two aggregates per key must be possible |
| **A bridge message that never succeeds** | (a) dead letter plus alert, with the reconciliation sweep repairing it; (b) also mark the application row as pending or blocked until the compliance record exists (the fail-closed reading of DEC-006) | (a), since compliance gates already deny without a record (section 5) |
| **Which failures, and write order** | Cover (a) only personal data without a record, or (b) also a compliance record without personal data (an orphan retention clock, an orphan consent); write the application row first (window of untracked data) or the Marten record first (possible orphans) | (a) with the application row first, as B and C are written; (b) only if orphan consent is judged a real risk |

### K5. How the reconciliation sweep finds application rows

Register D23. Only relevant if K1 keeps C as a complement.

| | K5a: an application-supplied query | K5b: Encina's `IPersonalDataLocator` (REQ-054, P-45) | K5c: a marker or watermark column |
| --- | --- | --- | --- |
| Better when | 1.0, where the application knows its schema | Generality across EF Core, Dapper and ADO.NET matters | Scan cost matters |
| Cons | Every application writes it | Depends on P-45 | A schema change in the application |

**Spike's recommendation: K5a** for 1.0, moving to K5b when P-45 lands.

### K6. Minor choices

| Choice | Options | Spike's recommendation |
| --- | --- | --- |
| ADO.NET on PostgreSQL (register D3) | Include it (same connection mechanism as Dapper) or state it as not covered (INV-002 allows partial coverage stated as partial) | Include it in the proof of concept if A is kept |
| Fault injection (D8) | A throwing decorator over `IAggregateRepository` (fast, does not prove database behaviour); an EF `DbCommandInterceptor` or Npgsql-level failure (proves A's rollback); a process kill between the commits (realism for B) | Interceptor for A, process kill for B, both in one shared-container fixture |
| Where the proof of concept lives (D33) | `tests/Encina.IntegrationTests` beside the Marten tests; the future PracticeManagement project (P-25, not yet created) | `tests/Encina.IntegrationTests`, reused by P-25 |
| ROADMAP.md (D36) | Update, or leave because SPEC-002 section 13.2 already tracks P-23 | Leave |

## 7. Proposed sequencing

Ordering is not a decision for the maintainer; this is the spike's proposal once K1-K6 are decided.

1. If K2b: the ADR-050 amendment, with the #2383 requirements row updated, before any B code.
2. A shared-container fixture (one PostgreSQL for EF Core, Dapper and Marten) and the proof of concept: a failing test that shows untracked personal data after a fault between the two writes.
3. Option A: the `ForTransaction` proof against Marten 9.38, then the helper (K3) through a `[FEATURE]` issue and plan.
4. Option B: the bridge handler with its idempotency choice, then the reconciliation sweep (K5).
5. The ADR, the how-to guide, and dated notes in ADR-019 and the module READMEs (with P-14).
6. S19 in the PracticeManagement project (P-25).

## 8. Unknowns

- Whether `SessionOptions.ForTransaction` works with Marten 9.38 and Npgsql 10 inside an EF Core execution strategy, and who disposes the connection.
- Whether the async daemon and Marten's per-tenant databases change the picture for A (the daemon is expected to see data only after commit; not verified).
- Whether `IDocumentSession` from Marten's `AddMarten` is scoped so that a request-scoped helper can replace it (not verified).
- Which of the nine modules start aggregates from application events rather than explicit service calls, beyond consent, retention and data subject requests.

## Related issues

[#1225](https://github.com/dlrivada/Encina/issues/1225), [#1186](https://github.com/dlrivada/Encina/issues/1186), [#2239](https://github.com/dlrivada/Encina/issues/2239), [#2383](https://github.com/dlrivada/Encina/issues/2383), [#718](https://github.com/dlrivada/Encina/issues/718), [#719](https://github.com/dlrivada/Encina/issues/719).
