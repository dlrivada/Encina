# Implementation Plan: `MartenEntityChangeAuditStore` — the entity-change audit store on Marten

> **Issue**: [#575](https://github.com/dlrivada/Encina/issues/575)
> **Type**: Feature
> **Complexity**: Medium-High (6 phases, 1 existing package, 5 pull requests)
> **Estimated Scope**: ~900 lines of production code + ~1,600 lines of tests

---

## Summary

Implement the entity-change audit store (today `IAuditLogStore`, becoming `IEntityChangeAuditStore` under ADR-036, work item #1778) on Marten as `MartenEntityChangeAuditStore`, inside the existing `Encina.Audit.Marten` package. The issue's sample is replaced where it does not hold: it appends the old and new values of every change in clear text to an event stream named `{EntityType}-{EntityId}` (a key that is not injective and has no tenant), calls `SaveChangesAsync` for every entry, and presents "time travel" and "immutability" as features although the entries contain personal data that GDPR Art. 5(1)(e) and SPEC-002 REQ-007 oblige the system to be able to erase at the end of retention, which an immutable event stream cannot do without crypto-shredding.

What the code looks like today (`origin/main` `915db384e`):

- `IAuditLogStore` is in `src/Encina.DomainModeling/Auditing/IAuditLogStore.cs` (`Task LogAsync(AuditLogEntry, ...)`, `Task<IEnumerable<AuditLogEntry>> GetHistoryAsync(string entityType, string entityId, ...)`); its implementations are `AuditLogStoreADO` and `AuditLogStoreDapper` (x3 each), `AuditLogStoreEF`, `AuditLogStoreMongoDB` and `InMemoryAuditLogStore`. ADR-036 reshapes it (`Either`, `TimeProvider`, `TenantId`, `ModuleId`, `PurgeEntriesAsync`, a redaction hook for old and new values, `ChangedAtUtc` as `DateTimeOffset`) and moves it to `Encina.Security.Audit`; that work is **#1778, open**. Its only writer is the EF Core `AuditInterceptor`.
- `src/Encina.Audit.Marten` already holds `MartenOperationAuditStore` and `MartenReadAuditStore`, an `AuditEventEncryptor` (field encryption per temporal period), `ITemporalKeyProvider` with `DestroyKeysBeforeAsync(olderThanUtc, granularity, reference, ...)` (crypto-shredding), async projections registered by `ConfigureMartenOperationAuditProjections`, and `MartenOperationAuditRetentionService`. The package references `Encina.Marten`, `Encina.Marten.GDPR`, `Encina.Security.Audit` and `Encina.Security.Encryption`, and grants `InternalsVisibleTo` to the unit, integration, contract, property and guard test projects. `MartenReadAuditStore` builds stream ids as `read-audit:{entityType}:{entityId}` (no tenant segment) and calls `SaveChangesAsync` itself.
- **ADR-036 decision 6 states "A Marten implementation of the entity-change store is not part of 1.0"** (the interceptor is EF Core only; revisited if a non-EF writer appears), while SPEC-000 section 6 (DEC-009) lists the only exclusions of 1.0 and does not list it, and the issue sits in milestone v0.19.0. Design Choice 1 asks the maintainer to reconcile them.
- Siblings, cited by number: #578-#582 and #577 (the other non-EF entity-change backends, built on the same ADR-036 contract), #800 (the Marten operation and read stores and ADR-020 temporal crypto-shredding), #2847 (erasure recording), #1257 (tenant isolation, reserved `default` tenant), #4182 (per-tenant retention design).

**Provider category**: event sourcing (Marten primary, `AGENTS.md` section 5), outside the 10-provider database matrix; ADR-019 (event-sourced stores have no InMemory variant: unit tests mock `IDocumentSession` with NSubstitute, integration tests run on PostgreSQL through Testcontainers). **Affected packages**: `Encina.Audit.Marten`, `Encina` (EventId registry), test projects. `Encina.Security.Audit` is consumed (the contract), not changed here.

---

## Design Choices

<details>
<summary><strong>1. Does the Marten store go ahead given ADR-036 decision 6? (Dilemma D1)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Implement it and amend ADR-036 decision 6 in the same pull request series (the ADR number is assigned when the ADR pull request opens)** | SPEC-000 section 6 says anything not listed is inside 1.0 and the issue is in v0.19.0; event-sourced applications get one audit backend on the database they already run | The only writer today is the EF Core interceptor, so the store has no producer until an application writes entries itself or #1778 adds a non-EF writer |
| **B) Keep decision 6: park the issue in "Post-1.0: Technical exclusions (ADR/SPEC)" and list it in SPEC-000 section 6.2** | Honors the accepted ADR; no store without a writer | Needs a SPEC-000 amendment; contradicts the maintainer's DEC-009 direction |
| **C) Implement it, leave the ADR as it is** | Fastest | An accepted ADR and the code disagree |

### Chosen Option: **A — implement and amend the ADR** (recommended, pending the maintainer)

### Rationale

- A wins when an application is on Marten and wants entity-change history in the same PostgreSQL database, writing entries from its own handlers; B wins if the maintainer holds that no backend without a producer ships. The other backends of the backlog (#577-#582) have the same property, so the answer should be given for all of them together. The plan is written for A; under B nothing is built.

</details>

<details>
<summary><strong>2. Package placement (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Audit.Marten`, an `EntityChange` set of files next to the operation and read stores** | Reuses `AuditEventEncryptor`, `ITemporalKeyProvider`, the projection daemon setup, the retention service and the package's tests; one Marten audit package | The package grows a third store |
| **B) `Encina.Marten`, an `EntityChangeAudit/` folder** (the `DeadLetter/` precedent of #584) | Marten users get it without another package | `Encina.Marten` would reference `Encina.Security.Audit` and the encryption stack; crypto-shredding lives in `Encina.Audit.Marten`, so the encryption would have to be duplicated or moved |
| **C) A new `Encina.EntityChangeAudit.Marten` package** | Isolated | Duplicates the encryptor and key provider wiring |

### Chosen Option: **A — `Encina.Audit.Marten`** (recommended, pending the maintainer)

### Rationale

- A wins because the entries must be encrypted and shreddable with the machinery that already exists there; B wins only if the old and new values were stored in clear, which Design Choice 3 rejects. The class is `MartenEntityChangeAuditStore` (ADR-036 keeps the `Marten` prefix for Marten stores).

</details>

<details>
<summary><strong>3. Persistence model: encrypted events plus a read model (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An encrypted `EntityChangeAuditEntryRecordedEvent` appended to the entity's stream, with the old and new JSON as `EncryptedField`s under the temporal key of the entry's period, and an async `EntityChangeAuditEntryReadModel` for history queries (the shape of the read and operation stores)** | Immutable; erasable by destroying the temporal key (GDPR Art. 5(1)(e), SPEC-002 REQ-007); same operational model as the sibling stores | History is eventually consistent (Design Choice 6); a destroyed key makes the old and new values unreadable by design |
| **B) The issue's plaintext event, history by `FetchStreamAsync`** | Strongly consistent, simple | Personal data in clear in an immutable stream that can never be erased; the audit table would defeat the erasure the application owes |
| **C) A Marten document per entry (mutable)** | Deletable by retention | Gives up the append-only property that distinguishes the Marten backend from the SQL backends |

### Chosen Option: **A — encrypted events and a read model** (recommended, pending the maintainer)

### Rationale

- A wins whenever old and new values can hold personal data (always, for an audit of arbitrary entities); B is acceptable only for entities known to hold none, which a generic store cannot assume; C wins if immutability is not required. The writer's redaction hook (ADR-036 decision 1) still runs first: a sensitive property never reaches the event even encrypted. "Time travel" is the history query over `ChangedAtUtc`, not a Marten feature that this store adds.

</details>

<details>
<summary><strong>4. Stream identity and tenant (Dilemma D4)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Stream id `entity-change:{tenant}:{type}:{id}` with each segment percent-encoded for `%` and `:` (injective); the tenant segment is the entry's `TenantId`, the reserved `default` when tenancy is off; with tenancy on and no tenant, write and read deny (`InvalidRequest`, ADR-062 R3), never falling back to `default`; a real tenant named `default` is rejected on write** | No two (tenant, type, id) triples share a stream; fails closed; works with a single Marten store | The tenant is an application-level segment, not Marten's conjoined tenancy |
| **B) Marten conjoined tenancy (the session's tenant)** | Database-enforced isolation | Needs a tenant-scoped session at every call site, which background retention does not have; depends on the Marten tenancy plan (#326) |
| **C) The issue's `{EntityType}-{EntityId}`** | As written | `A-B` + `C` equals `A` + `B-C`; no tenant at all |

### Chosen Option: **A — injective, tenant-qualified stream id** (recommended, pending the maintainer)

### Rationale

- A wins until the maintainer decides on Marten conjoined tenancy for the whole package (#326); B wins when isolation must be enforced by the database. The read model rows carry `TenantId` and every query filters on it (a query without a tenant under tenancy is denied). The read and operation stores of the package have the non-injective, tenant-less ids that #798 and #1257 address; this store does not copy them.

</details>

<details>
<summary><strong>5. Retention: shredding periods versus per-tenant purge (Dilemma D5)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `PurgeEntriesAsync` destroys the temporal keys older than the cutoff through the existing `DestroyKeysBeforeAsync`, with one retention (`EntityChangeAuditOptions`, 2555 days by default) for every tenant; it returns the number of entries it made unreadable** | Works with today's key provider; the retention service of the package already runs it | The keys are per period, not per tenant: one tenant's shorter retention cannot be honored alone |
| **B) Tenant-scoped temporal keys (a tenant argument on `ITemporalKeyProvider`) so that `PurgeEntriesAsync(tenant, cutoff)` shreds one tenant** | Per-tenant retention, as the retention design of #4182 expects | Changes the key provider contract and the keys of the operation and read stores; a prerequisite larger than this issue |
| **C) Delete events and read model rows** | Per-tenant | Breaks the append-only guarantee and leaves events that cannot be proven deleted |

### Chosen Option: **A — one retention through the existing key destruction** (recommended, pending the maintainer)

### Rationale

- A wins when one legal retention applies to all tenants (the ADR-036 default); B wins when tenants must have different retentions, which is #4182's open design; the plan takes the tenant argument of the contract so that moving to B is a change inside the key provider, not in this store. The read model rows of destroyed periods are removed by the same service. The destruction carries a `KeyDestructionReference` (`RetentionReference`), recorded by the #2847 recorder.

</details>

<details>
<summary><strong>6. Projection lifecycle: async, as the sibling stores (Dilemma D6)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Async projection processed by the daemon (the package pattern)** | No projection cost on the write path; the same daemon and error policy guard as the other two stores | `GetHistoryAsync` right after a write may not see it; tests wait for the daemon |
| **B) Inline projection** | Read-your-writes; atomic with the event | Every audited entity change pays the projection; differs from the package |

### Chosen Option: **A — async projection** (recommended, pending the maintainer)

### Rationale

- A wins for the write volume of an entity-change audit (one entry per changed entity); B wins when callers read their own history immediately. The documentation says history is eventually consistent; the integration tests wait with `WaitForNonStaleProjectionDataAsync`, never with a sleep.

</details>

<details>
<summary><strong>7. Writes are durable on return and never swallowed</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (errors are never swallowed; fail closed; `TimeProvider`), ADR-036 decision 1 (`Either` results; a failed write is a `Left` that fails the unit of work), and `MartenReadAuditStore.LogReadAsync` (`src/Encina.Audit.Marten/MartenReadAuditStore.cs`), which appends and calls `SaveChangesAsync` before returning

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Encrypt, append and `SaveChangesAsync` inside the store call; any failure (no key, encryption error, database error) is a `Left` with a code (`MartenAuditErrors`), never a silent success; cancellation propagates** | The entry is durable when the call returns; same behavior as the read store | Not atomic with the audited entity change when that lives in another database; the README says so |
| **B) Append to the caller's session and let the caller commit** | Atomic when the caller shares the Marten session | The caller can forget to commit: a lost audit entry |

### Chosen Option: **A — durable on return**

### Rationale

- B would let an entry vanish without an error, which the fail-closed rule forbids. A duplicate entry id is detected by the write: the event id equals the entry id, so a retry of the same entry is idempotent and a different payload under the same id is a `Left` (`AlreadyExists`, ADR-062).

</details>

---

## Implementation Phases

### Phase 1: Event, encryption and stream keys (pull request 1)

<details>
<summary>Tasks</summary>

1. `Events/EntityChangeAuditEntryRecordedEvent.cs` (new): id, entity type, entity id, action, user id, `ChangedAtUtc`, tenant id, module id, encrypted old and new values (`EncryptedField`), correlation id.
2. `AuditEventEncryptor.cs` (edit): `EncryptEntityChangeEntryAsync(EntityChangeAuditEntry, ct)` using the temporal key of `ChangedAtUtc`'s period; no key is a `Left`.
3. `Internal/EntityChangeStreamKeys.cs` (new, internal): the injective stream id of Design Choice 4 and the tenant rule (`default` reserved, deny without tenant under tenancy).
4. Tests: `AuditEventEncryptorEntityChangeTests` (round trip, no key, destroyed key), `EntityChangeStreamKeysTests` (colliding triples, `:` and `%` in ids, reserved tenant), `EntityChangeAuditEntryRecordedEventTests`; `PublicAPI.Unshipped.txt`; `.github/coverage-manifest/Encina.Audit.Marten.json` (edit).

Files: 8.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- #1778 delivers IEntityChangeAuditStore and EntityChangeAuditEntry in Encina.Security.Audit (ADR-036).
- The Marten audit package already encrypts read and operation entries per temporal period.

TASK:
Add the entity-change event, its encryption method and the injective stream key builder, with tests.

KEY RULES:
- Old and new values are EncryptedField; the writer's redaction hook has already run.
- Stream id segments are percent-encoded for % and :; the tenant segment default is reserved.
- Either<EncinaError, T> everywhere; fixed messages; never EncinaError.Message in logs.
- XML docs; PublicAPI lines; numeric per-flag targets in the manifest.

REFERENCE FILES:
- src/Encina.Audit.Marten/AuditEventEncryptor.cs
- src/Encina.Audit.Marten/Events/ReadAuditEntryRecordedEvent.cs
- src/Encina.Audit.Marten/MartenAuditErrors.cs
```

</details>

---

### Phase 2: Projection and read model (pull request 2)

<details>
<summary>Tasks</summary>

1. `Projections/EntityChangeAuditEntryProjection.cs` and `Projections/EntityChangeAuditEntryReadModel.cs` (new): decrypt while projecting, `TenantId`, `EntityType`, `EntityId`, `ChangedAtUtc` indexed; a destroyed key projects a marker row (as `TemporalKeyDestroyedMarker` does for the other stores), never an exception.
2. `Projections/ConfigureMartenEntityChangeAuditProjections.cs` (new): registers the async projection; the `AuditProjectionErrorPolicyStartupGuard` already checks the error policy for the package.
3. Tests, `PublicAPI.Unshipped.txt`, manifest.

Files: 7.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Phase 1 shipped the event and encryptor.

TASK:
Add the async projection, its read model and the Marten options configurator.

KEY RULES:
- Projection dependencies come from IDocumentOperations and constructor injection, never IServiceProvider (AGENTS.md section 3).
- Keep at least one test that registers the projection with a real store.

REFERENCE FILES:
- src/Encina.Audit.Marten/Projections/ReadAuditEntryProjection.cs
- src/Encina.Audit.Marten/Projections/ConfigureMartenOperationAuditProjections.cs
```

</details>

---

### Phase 3: The store and its registration (pull request 3)

<details>
<summary>Tasks</summary>

1. `MartenEntityChangeAuditStore.cs` (new): implements `IEntityChangeAuditStore`; constructor `(IDocumentSession session, AuditEventEncryptor encryptor, ITemporalKeyProvider keyProvider, IOptions<...> options, ILogger<MartenEntityChangeAuditStore> logger, TimeProvider? timeProvider = null)`; write, history and purge per Design Choices 4, 5 and 7.
2. `ServiceCollectionExtensions.cs` (edit): `AddEncinaAuditMarten` registers the store when `UseEntityChangeAuditStore` is on, replacing the in-memory default in any registration order and never the application's own registration (the store of the package wins over `InMemoryEntityChangeAuditStore`).
3. Tests: `MartenEntityChangeAuditStoreTests` (NSubstitute `IDocumentSession`: every `Left` path), registration test with `ValidateOnBuild` and `ValidateScopes`, guard tests, `PublicAPI.Unshipped.txt`, manifest.

Files: 8.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- The event, encryptor, keys and read model exist.

TASK:
Implement MartenEntityChangeAuditStore and its registration.

KEY RULES:
- Durable on return: append and SaveChangesAsync; every failure is a Left with a MartenAuditErrors code.
- Tenancy on and no tenant: deny; default reserved; never inject a scoped service into a singleton.
- Time from TimeProvider; exception to logs through ForLogging().

REFERENCE FILES:
- src/Encina.Audit.Marten/MartenReadAuditStore.cs
- src/Encina.Audit.Marten/ServiceCollectionExtensions.cs
- src/Encina.Audit.Marten/MartenAuditErrors.cs
```

</details>

---

### Phase 4: Retention, health, EventIds and observability (pull request 4)

<details>
<summary>Tasks</summary>

1. Retention adds no new service: the `EntityChangeAuditRetentionService` of #1778 calls `PurgeEntriesAsync`; `MartenEntityChangeAuditStore.PurgeEntriesAsync` calls `DestroyKeysBeforeAsync` with a `RetentionReference` and removes the read model rows of the destroyed periods.
2. `Health/MartenEntityChangeAuditHealthCheck.cs` (new): `DefaultName`, static `Tags`; read-only probe of the read model.
3. `Diagnostics/MartenEntityChangeAuditLog.cs` (new, `[LoggerMessage]`); `src/Encina/Diagnostics/EventIdRanges.cs` (edit: `AuditMartenEntityChange = (52650, 52699)`), `src/Encina/PublicAPI.Unshipped.txt` (edit), `docs/engineering/eventid-block-registry.md` (edit), `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (edit: `AssemblyRanges`).
4. Spans and counters through the `InstrumentedEntityChangeAuditStore` of #1778, which wraps any store; the Marten-specific meter instruments of the package are not extended.
5. Tests for purge (count, no keys, a failed destruction), health and logs; package `PublicAPI.Unshipped.txt`; manifest.

Files: 12.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- The store writes and reads; retention needs the key destruction path.

TASK:
Implement PurgeEntriesAsync through DestroyKeysBeforeAsync, the health check, the log class and the EventId range.

KEY RULES:
- Register AuditMartenEntityChange (52650, 52699) in EventIdRanges.cs before using an id; pack ids from 52650; map Encina.Audit.Marten's new range in AssemblyRanges.
- A purge failure is a Left, never reported as success; logs carry codes and exception types only, never EncinaError.Message.
- The tenant is passed explicitly by the retention service (#4182); there is no cross-tenant opt-in.

REFERENCE FILES:
- src/Encina.Audit.Marten/MartenOperationAuditRetentionService.cs
- src/Encina.Audit.Marten/Crypto/ITemporalKeyProvider.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/engineering/eventid-block-registry.md
```

</details>

---

### Phase 5: Integration tests (pull request 5)

<details>
<summary>Tasks</summary>

1. `tests/Encina.IntegrationTests/Infrastructure/Marten/Audit/MartenEntityChangeAuditStoreIntegrationTests.cs` (new, next to `MartenAuditStorePurgeIntegrationTests.cs`): the existing Marten PostgreSQL shared collection fixture; write then wait for the non-stale projection, history order, tenant isolation (two tenants), reserved tenant, retention (a destroyed period reads as unreadable, others intact), idempotent retry, different payload under the same id.
2. `changelog.d/575-marten-entity-change-audit-store.added.md`.

Files: 2.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- Unit tests mock IDocumentSession; Marten behavior needs a real PostgreSQL.

TASK:
Write the integration tests on the shared Marten PostgreSQL collection fixture.

KEY RULES:
- Shared [Collection] fixture; never a per-class container; no Thread.Sleep (WaitForNonStaleProjectionDataAsync).
- [Trait("Category", "Integration")]; clean data in InitializeAsync.

REFERENCE FILES:
- tests/Encina.IntegrationTests (the existing Audit.Marten integration tests and the Marten fixture)
```

</details>

---

### Phase 6: Documentation and finalization

<details>
<summary>Tasks</summary>

1. By `docs-writer`: `src/Encina.Audit.Marten/README.md` (edit: the third store, encryption and retention semantics, eventual consistency, no atomicity with an entity change in another database), the audit guide page that explains which of the three stores to use (ADR-036), the ADR-036 decision 6 amendment (when Dilemma D1 is answered A).
2. Verification: `dotnet build --configuration Release` with 0 warnings; per-flag measurements against the Research targets.

Files: 3.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- The code of #575 is merged.

TASK:
Update the Audit.Marten README, the audit guide and, if the maintainer chose A, amend ADR-036 decision 6.

KEY RULES:
- Coverage cited with covref markers; English only; the reference application is never named.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- docs/architecture/adr/036-three-audit-stores.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| ADR-036 | The three stores, the entity-change contract, decision 6 (Marten not in 1.0) |
| ADR-019, ADR-027 | Event-sourced stores have no InMemory variant; Marten is the event-sourcing provider |
| GDPR Art. 5(1)(e), SPEC-002 REQ-007 | Storage limitation: old and new values must be erasable at the end of retention |
| ADR-062 | Categories: `AlreadyExists` for a conflicting id, `InvalidRequest` for a missing tenant under tenancy, existing `MartenAuditErrors` codes for store failures (this plan adds no error code) |
| SPEC-000 section 6 (DEC-009) | The only list of 1.0 exclusions; not listing #575 puts it in 1.0 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `AuditEventEncryptor`, `ITemporalKeyProvider`, `MartenTemporalKeyProvider` | `src/Encina.Audit.Marten/` | Field encryption and crypto-shredding |
| `MartenReadAuditStore`, `MartenOperationAuditStore` | `src/Encina.Audit.Marten/` | Patterns for append, `SaveChangesAsync` and queries |
| `ConfigureMartenOperationAuditProjections`, `AuditProjectionErrorPolicyStartupGuard` | `src/Encina.Audit.Marten/Projections/` | Async projection registration and error policy |
| `KeyDestructionReference`, `RetentionReference`, `ErasureReference` | `src/Encina.Audit.Marten/Crypto/` | Evidence of destruction (#2847) |
| `IEntityChangeAuditStore`, `EntityChangeAuditEntry`, `InstrumentedEntityChangeAuditStore`, `EntityChangeAuditOptions` | `Encina.Security.Audit` after #1778 | The contract and wrapper |

Per-file coverage targets (numeric, per flag):

| File | unit | guard | integration | Justification |
|------|-----:|------:|------------:|---------------|
| `MartenEntityChangeAuditStore.cs` | 85 | 10 | 80 | A substituted `IDocumentSession` reaches every `Left`; PostgreSQL covers append, projection reads and purge |
| `Internal/EntityChangeStreamKeys.cs` | 100 | 15 | 0 | Pure functions with guarded arguments; no I/O to integrate |
| `AuditEventEncryptor.cs` (the new method) | 90 | 10 | 60 | Encrypt, no key, destroyed key by unit tests; real keys by integration |
| `Projections/EntityChangeAuditEntryProjection.cs` | 85 | 0 | 80 | The projection API is reached through events; guard has no parameters to test; the daemon covers the rest |
| `Health/MartenEntityChangeAuditHealthCheck.cs` | 100 | 10 | 70 | Two outcomes |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Audit.Marten` (entity change) | `AuditMartenEntityChange = (52650, 52699)` | `AuditMarten = (2550, 2599)` is full; Security and compliance zone, after 52600-52649 (#3975); 52350-52599 are the entity-change backends of #577-#581; 52650 is the first free slot (checked on `origin/main` and the open `docs/plan-*` heads). Ids: 52650 write failed (exception type), 52651 key unavailable (fail closed), 52652 history query failed, 52653 purge completed (count), 52654 purge failed; 52655-52699 free |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Event, encryptor, keys (PR 1) | 8 | |
| Projection and read model (PR 2) | 7 | |
| Store and registration (PR 3) | 8 | |
| Retention, health, EventIds (PR 4) | 12 | Registry, `EventIdRanges.cs`, `AssemblyRanges`, PublicAPI x2 counted |
| Integration tests and changelog (PR 5) | 2 | |
| Documentation (PR 6) | 3 | `docs-writer`; includes the ADR-036 amendment |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is pre-1.0, .NET 10, C# 14; ROP with Either<EncinaError, T>.
- ADR-036 defines IEntityChangeAuditStore (work item #1778). Encina.Audit.Marten holds the Marten audit stores with temporal crypto-shredding.

IMPLEMENTATION OVERVIEW:
1. Phase 1: encrypted entity-change event, encryptor method, injective tenant-qualified stream keys.
2. Phase 2: async projection and read model.
3. Phase 3: MartenEntityChangeAuditStore and its registration.
4. Phase 4: retention by key destruction, health check, EventIds 52650-52699.
5. Phases 5-6: integration tests, docs, ADR-036 decision 6 amendment.

KEY PATTERNS:
- Old and new values are encrypted fields under the temporal key; erasure is key destruction.
- Stream id entity-change:{tenant}:{type}:{id}, segments percent-encoded, default tenant reserved, deny without tenant under tenancy.
- Durable on return, errors never swallowed, TimeProvider, exception types only in logs.

REFERENCE FILES:
- src/Encina.Audit.Marten/MartenReadAuditStore.cs
- src/Encina.Audit.Marten/AuditEventEncryptor.cs
- src/Encina.Audit.Marten/Crypto/ITemporalKeyProvider.cs
- docs/architecture/adr/036-three-audit-stores.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Audit history is read rarely and must be fresh; a cache would hide the destroyed-key state |
| 2 | OpenTelemetry | ✅ | Through `InstrumentedEntityChangeAuditStore` (#1778); no tenant id or entity id as a tag |
| 3 | Structured Logging | ✅ | `MartenEntityChangeAuditLog` 52650-52654 (Phase 4) |
| 4 | Health Checks | ✅ | `MartenEntityChangeAuditHealthCheck` (Phase 4) |
| 5 | Validation | ✅ | Tenant rule and key length checks at the store boundary (Phase 1); the entry itself is validated by the writer |
| 6 | Resilience | ❌ | A failed write is a `Left` that fails the unit of work; retrying inside the store would hide it |
| 7 | Distributed Locks | ❌ | Append to a stream and the entry-id check need no coordination |
| 8 | Transactions | ✅ | One Marten `SaveChangesAsync` per entry; not atomic with an entity change in another database (documented) |
| 9 | Idempotency | ✅ | The event id is the entry id: a retry is idempotent, a different payload is a `Left` |
| 10 | Multi-Tenancy | ✅ | Tenant-qualified injective stream id, `TenantId` on the read model, deny without tenant under tenancy, reserved `default` |
| 11 | Module Isolation | ✅ | `ModuleId` is carried on the event and the read model, as ADR-036 requires |
| 12 | Audit Trail | ✅ | This is the entity-change audit trail |

---

## Prerequisites & Dependencies

- **Hard prerequisite**: #1778 (open): `IEntityChangeAuditStore`, `EntityChangeAuditEntry`, `EntityChangeAuditOptions` and the redaction hook in `Encina.Security.Audit`. Phase 1 can start with the event and keys before it merges; Phase 3 cannot.
- **Decision prerequisite**: Design Choice 1 (ADR-036 decision 6); under B nothing in this plan is built.
- #4182 (open) decides per-tenant retention (Design Choice 5 option B); #800 and #2847 own the temporal keys and the erasure recording this plan reuses; #1257 owns the reserved tenant.

---

## Next Steps

1. The maintainer answers Dilemmas D1-D6 when work on the issue starts, D1 together with the other non-EF backends of the backlog (#577-#582).
2. After #1778 merges, cut PR 1 and report the per-file measurements against the targets above.
3. Register `AuditMartenEntityChange` in `EventIdRanges.cs` in the pull request that first uses an id (PR 4).
