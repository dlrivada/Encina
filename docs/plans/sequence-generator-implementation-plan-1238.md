# Implementation Plan: `ISequenceGenerator` — Gapless Sequence per Key on All 10 Database Providers

> **Issue**: [#1238](https://github.com/dlrivada/Encina/issues/1238) (SPEC-002 P-36, REQ-045, AC-039, AC-043, scenario S8; parent [#1186](https://github.com/dlrivada/Encina/issues/1186))
> **Type**: Feature
> **Complexity**: High (one primitive, four different SQL and driver mechanisms, concurrency correctness on every provider)
> **Estimated Scope**: ~1,100-1,500 lines of production code + ~3,000-3,800 lines of tests; ~30 new and ~14 modified production files (split into 4 pull requests, see Next Steps)

---

## Summary

Invoices must be numbered without gaps per series (SPEC-002 note H, H-R04, H-R13). Database identity columns and sequences leave gaps on rollback, and numbering after commit leaves a window with no number. This plan adds `ISequenceGenerator`: it returns the next number for a (tenant, sequence key) pair, enlisted in the caller's ambient transaction, so a rollback returns the number. One row per (tenant, key) is locked with an atomic increment inside the caller's transaction on all 10 database providers. The primitive is regulation-neutral: Encina ships no Verifactu code (SPEC-002 DEC-002, ADR-057).

### What exists today (verified on `main` at `7a0049c0`)

| Area | Current state | Evidence |
|------|---------------|----------|
| Gapless sequence | None; the only "sequence" in `src/` is the Snowflake bit field of `Encina.IdGeneration` (range 8030-8099), unrelated | `src/Encina.IdGeneration/`, `src/Encina/Diagnostics/EventIdRanges.cs:316` |
| Ambient transaction | `ITransactionLeaseSource` (first caller owns commit, later callers join); `IDbTransactionAccessor` (ADO, Dapper, EF Core); `IMongoSessionAccessor` | `src/Encina.Messaging/Transactions/ITransactionLeaseSource.cs:22`, `IDbTransactionAccessor.cs:38`, `src/Encina.MongoDB/Transactions/IMongoSessionAccessor.cs:21` |
| How stores join the transaction | ADO and Dapper stores take `IDbTransactionAccessor?` and set the command transaction; EF Core uses `Database.CurrentTransaction`; MongoDB uses the session | `src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs:32`, `:340-366`; `src/Encina.EntityFrameworkCore/Transactions/DbContextTransactionAccessor.cs:68`; `src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs:51` |
| Row-lock SQL | No `UPDLOCK`, `FOR UPDATE`, `OUTPUT inserted`, `RETURNING` or `FindOneAndUpdate` usage anywhere in `src/`; the pattern is new on every provider | grep over `src/` |
| Upsert SQL | `MERGE` (SqlServer), `ON CONFLICT` (PostgreSQL), `ON DUPLICATE KEY UPDATE` (MySQL) exist in ABAC and dead-letter stores | `src/Encina.ADO.SqlServer/ABAC/PolicyStoreADO.cs:128`; `src/Encina.ADO.PostgreSQL/ABAC/PolicyStoreADO.cs:133`; `src/Encina.ADO.MySQL/ABAC/PolicyStoreADO.cs:132` |
| Scripts | Highest ADO script `030` (SqlServer, PostgreSQL); MySQL stops at `029` | `src/Encina.ADO.*/Scripts/` |
| MongoDB replica-set tests | Collection `MongoDB-ReplicaSet` | `tests/Encina.IntegrationTests/Infrastructure/MongoDB/ReadWriteSeparation/MongoDbReplicaSetCollection.cs:28` |
| Tenant | `IRequestContext.TenantId` is `string?`; Consent's fail-closed table | `src/Encina/Abstractions/IRequestContext.cs:125`; `src/Encina.Compliance.Consent/Services/DefaultConsentService.cs:121-128` |
| Verifactu boundary | ADR-057; naming rule D23: generic names, no regulation-named type or option | `docs/architecture/adr/057-verifactu-boundary.md` |
| Load tests | `.cs` concurrent examples under `tests/Encina.LoadTests/Security/ABAC/`; `.md` justification convention in `Messaging/` | `tests/Encina.LoadTests/` |

### Standards covered

SPEC-002 REQ-045, AC-039, AC-043, S8, DEC-002, REQ-061, REQ-062; ADR-057 (Verifactu boundary, D23 naming); gapless invoice numbering as a regulation-neutral requirement (Spanish invoicing rules, note H H-R04 and H-R13).

### Affected packages

- `Encina` (core): abstraction, tenant resolution, options, validator, in-memory store, health check, diagnostics (folder `Sequences/`).
- `Encina.ADO.{SqlServer,PostgreSQL,MySQL}`, `Encina.Dapper.{SqlServer,PostgreSQL,MySQL}`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`: the store and its registration.
- `tests/Encina.TestInfrastructure` (schemas), `src/Encina/Diagnostics/EventIdRanges.cs`.

### Provider category

**Database (10)**: ADO.NET x3, Dapper x3, EF Core (one project, tested on SqlServer, PostgreSQL and MySQL) and MongoDB (replica set required for transactions).

---

## Design Choices

<details>
<summary><strong>1. Placement and shape — the abstraction in core, tenant logic once, a provider-level store</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ISequenceGenerator` and `DefaultSequenceGenerator` in `Encina` (folder `Sequences/`) resolve the tenant, validate the key and delegate to a provider-level `ISequenceStore`** | Tenant fail-closed and key validation written once; providers differ only in SQL; matches "Encina (core)" in the issue | The core grows by a small folder |
| **B) Each provider implements `ISequenceGenerator` directly** | No extra interface | Ten copies of tenant and validation logic |
| **C) A new `Encina.Sequences` package** | Isolated | A package for roughly 12 files; the issue ticks core |

### Chosen Option: **A — core generator over a provider-level `ISequenceStore`**

**Class:** Settled
**Settled by:** issue #1238 (Affected Packages: `Encina (core)` and the 10 providers), AGENTS.md section 5 (providers share interfaces and differ only in implementation)

### Rationale

- `ISequenceStore.NextAsync(string tenantId, string sequenceKey, ct)` is the provider contract; `ISequenceGenerator.NextAsync(string sequenceKey, ct)` keeps the signature of the issue.
- The store also exposes `CheckAsync(ct)` for the health check (the table or collection is reachable).
- Names are generic (`Sequence`, `SequenceKey`); no type, option, namespace or metric name contains a regulation name (ADR-057 D23). An architecture test scans the public surface of the package for those names.

</details>

<details>
<summary><strong>2. Allocation algorithm — insert-if-missing, then an atomic increment that returns the new value</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Per provider: ensure the row exists, then one atomic statement that increments and returns the value, inside the caller's transaction (row lock held until commit or rollback)** | Gapless on rollback; one round trip for the hot path; concurrent callers on one key queue on the row lock | Callers on the same key serialise until the owning transaction ends (inherent to gapless) |
| **B) `SELECT ... FOR UPDATE` then `UPDATE`** | Familiar | Two round trips; SQL Server needs `UPDLOCK, ROWLOCK` hints; same serialisation |
| **C) Database sequences** | Fast | Not gapless on rollback; rejected by the issue |
| **D) Assign after commit** | No contention | A window with no number; rejected by the issue |

### Chosen Option: **A — insert-if-missing plus atomic increment-and-return**

**Class:** Settled
**Settled by:** issue #1238 (Proposed Solution: "one row per (tenant, sequence key) with a row lock inside the caller's transaction; rollback returns the number" and Alternatives 1 and 2) and its comment ("Settled by elimination")

### Rationale

- SQL Server: `INSERT ... SELECT ... WHERE NOT EXISTS (SELECT 1 FROM t WITH (UPDLOCK, HOLDLOCK) WHERE ...)`, then `UPDATE t SET LastValue = LastValue + 1 OUTPUT inserted.LastValue WHERE ...`.
- PostgreSQL: `INSERT ... ON CONFLICT DO NOTHING`, then `UPDATE ... SET "LastValue" = "LastValue" + 1 ... RETURNING "LastValue"`.
- MySQL: `INSERT IGNORE ...`, then `UPDATE ... SET LastValue = LAST_INSERT_ID(LastValue + 1)` and `SELECT LAST_INSERT_ID()` on the same connection.
- MongoDB: `findOneAndUpdate` with `$inc`, `upsert: true`, `returnDocument: After`, inside the session; a duplicate-key error on the first concurrent upsert is retried once.
- A row created by a transaction that rolls back disappears with it, so even the first number of a series is returned on rollback.

</details>

<details>
<summary><strong>3. No ambient transaction — fail closed</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) With no active transaction (or a MongoDB standalone server) return `Left(sequence.transaction_required)`** | The guarantee cannot be silently lost | Callers must begin the unit of work first |
| **B) Open a short private transaction** | Always returns a number | The number is consumed without the business commit: not gapless |

### Chosen Option: **A — fail closed**

**Class:** Settled
**Settled by:** issue #1238 ("Enlists in the ambient transaction; the number is consumed only if the transaction commits"), ADR-050 (MongoDB without a replica set refused for Transactions), AGENTS.md section 3 (fail closed)

### Rationale

- The lease check is the first step of every store call; the error metadata carries the code only.

</details>

<details>
<summary><strong>4. Key and tenant rules — validated keys, tenant-scoped rows, fail closed on a missing tenant</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Key 1 to 128 characters from `[A-Za-z0-9._:-]`; row key `(TenantId, SequenceKey)`; ambient tenant with `RequireTenantContext`, fixed default tenant `"-"` when tenancy is off** | Two tenants can use the same series; no injection surface; meets AC-043 | Keys with spaces must be encoded by the caller |
| **B) Any string key** | Flexible | Length and collation differences between providers; a lookup surface |
| **C) Tenant passed by the caller** | Explicit | Can be forgotten; no fail-closed default |

### Chosen Option: **A — validated key, tenant-scoped row**

**Class:** Settled
**Settled by:** SPEC-002 REQ-061 and AC-043, issue #1238 ("Keys are tenant-scoped"), AGENTS.md section 3 (fail closed), `src/Encina.Compliance.Consent/Services/DefaultConsentService.cs:128`

### Rationale

- Binary or case-sensitive collation on the key column of every relational provider so `A-2026` and `a-2026` are different series on all of them.
- The metric tag is a key hash (first 8 hex characters of SHA-256), never the key itself (issue observability table).

</details>

<details>
<summary><strong>5. Concurrency evidence — real per-provider load tests, not justifications</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `.cs` load test per provider family (50 to 200 concurrent allocations on one key, commit and rollback mix) and a property test on the interleaving** | Proves gaplessness under contention, which is the feature's value | Needs the containers; slower |
| **B) `.md` load justification** | Cheap | Not allowed for a concurrent feature (AGENTS.md section 9) |

### Chosen Option: **A — real concurrency tests**

**Class:** Settled
**Settled by:** AGENTS.md section 9 (load tests: implement for concurrent features; justification files never for database integration tests), issue #1238 Test Matrix (LoadTests Required)

### Rationale

- The integration test asserts: committed values form the range `1..N` with no gap and no duplicate; rolled-back transactions leave their numbers to the next caller.
- The benchmark is justified in an `.md` (dominated by the round trip), as the issue states.

</details>

---

## Implementation Phases

### Phase 1: Core Abstractions, Options and In-Memory Store (`Encina`)

> **Goal**: The contract and a transactional in-memory store for tests and development.

<details>
<summary><strong>Tasks</strong></summary>

#### New files in `src/Encina/Sequences/` (namespace `Encina.Sequences`)

1. `ISequenceGenerator.cs` — `ValueTask<Either<EncinaError, long>> NextAsync(string sequenceKey, CancellationToken cancellationToken = default)`.
2. `ISequenceStore.cs` — `NextAsync(string tenantId, string sequenceKey, CancellationToken)` and `CheckAsync(CancellationToken)`.
3. `DefaultSequenceGenerator.cs` — validates the key, resolves the tenant, calls the store, records telemetry. Constructor: `ISequenceStore`, `IRequestContextAccessor`, `IOptions<SequenceOptions>`.
4. `SequenceKeyValidator.cs` — pure; the rule of Design Choice 4.
5. `SequenceOptions.cs` — `StartAt` (default 1), `RequireTenantContext` (nullable), `CommandTimeout`; `SequenceOptionsValidator.cs`.
6. `SequenceErrors.cs` — `sequence.key_invalid`, `sequence.tenant_required`, `sequence.transaction_required`, `sequence.lock_timeout`, `sequence.store_failed`.
7. `InMemorySequenceStore.cs` — thread-safe; allocation is tentative and honoured only when the ambient lease commits (test double for the contract, not for production).
8. `ServiceCollectionExtensions.cs` (or the core registration file) — `AddEncinaSequences(Action<SequenceOptions>?)` with `TryAdd*`; `MessagingConfiguration` flag `UseSequenceStore` for the database stores.
9. `README` sentence in the package documentation: "a regulation-neutral primitive; Encina ships no Verifactu code" (issue comment, ADR-057 D23).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #1238 (ISequenceGenerator).

CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0, Railway Oriented Programming.
- Plan: docs/plans/sequence-generator-implementation-plan-1238.md, Design Choices 1, 3 and 4.

TASK:
Create the abstractions, the default generator, the key validator, options, errors, the in-memory store and the
registration, as listed in the Phase 1 Tasks. Add every public symbol to PublicAPI.Unshipped.txt of Encina.

KEY RULES:
- Names are generic: no type, option, namespace, metric or doc name contains a regulation name (ADR-057 D23).
- A missing tenant fails closed when required; with tenancy off use the fixed default tenant.
- No ambient transaction returns Left(sequence.transaction_required); never open a private transaction.
- Every async method takes a CancellationToken; XML docs on all public members.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs (tenant decision table)
- src/Encina.Messaging/Transactions/ITransactionLeaseSource.cs
- docs/architecture/adr/057-verifactu-boundary.md
```

</details>

---

### Phase 2: Persistence — Table and Scripts

> **Goal**: The shared shape of the sequence row.

<details>
<summary><strong>Tasks</strong></summary>

1. Scripts `NNN_CreateSequencesTable.sql` for ADO SqlServer, PostgreSQL and MySQL and the Dapper script folders (next free number at implementation time; plan #1234 uses 031), and the aggregate `000_CreateAllTables.sql`; primary key `(TenantId, SequenceKey)`, `LastValue bigint not null`, `LastUpdatedAtUtc`.
2. `tests/Encina.TestInfrastructure/Schemas/{SqlServerSchema,PostgreSqlSchema,MySqlSchema}.cs` and `EFCoreSchema.cs` entries.
3. EF Core: `src/Encina.EntityFrameworkCore/Sequences/SequenceRowEntity.cs`, `SequenceRowEntityConfiguration.cs`, `SequenceModelBuilderExtensions.cs`.
4. MongoDB: `SequenceDocument.cs`, unique index on `(tenantId, sequenceKey)` in `MongoDbIndexCreator.cs`, option and collection name.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #1238.

CONTEXT:
- Plan: docs/plans/sequence-generator-implementation-plan-1238.md, Design Choices 2 and 4.

TASK:
Create the scripts, test schemas, EF entity and configuration, and the MongoDB document and index.

KEY RULES:
- Case-sensitive, binary-collation key column on every relational provider.
- Primary key (TenantId, SequenceKey); SQL differences per AGENTS.md section 5.
- UTC timestamps with the AtUtc suffix.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Scripts/030_CreateBreakTheGlassElevationsTable.sql
- src/Encina.EntityFrameworkCore/BreakTheGlass/
- src/Encina.MongoDB/MongoDbIndexCreator.cs
```

</details>

---

### Phase 3: ADO.NET and Dapper Stores (6 providers)

> **Goal**: `ISequenceStore` on SqlServer, PostgreSQL and MySQL for both families.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/Sequences/SequenceStoreADO.cs` (3).
2. `src/Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/Sequences/SequenceStoreDapper.cs` (3).
3. Each takes `IDbTransactionAccessor?`; with no active transaction it returns `sequence.transaction_required`; a lock timeout (SQL Server error 1222, PostgreSQL `55P03`/`57014`, MySQL 1205) maps to `sequence.lock_timeout`.
4. Registration in the six `ServiceCollectionExtensions.cs` files: `RemoveInMemoryDefault` then `TryAddScoped`, gated by `UseSequenceStore`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #1238.

CONTEXT:
- Plan: docs/plans/sequence-generator-implementation-plan-1238.md, Design Choices 2 and 3. Phases 1-2 exist.

TASK:
Implement SequenceStoreADO and SequenceStoreDapper for SqlServer, PostgreSQL and MySQL and register them.

KEY RULES:
- Use exactly the statements of Design Choice 2; the increment and the read-back are one statement (MySQL: same connection).
- Database calls are async with a CancellationToken; never the synchronous overloads.
- Join the ambient transaction through IDbTransactionAccessor; never begin one.
- A database store wins over the in-memory default in any registration order.
- Errors map to Either; the SQL error text never reaches logs or error metadata.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs (transaction joining)
- src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs
- src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:113-118
```

</details>

---

### Phase 4: EF Core and MongoDB Stores

> **Goal**: The two remaining families.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.EntityFrameworkCore/Sequences/SequenceStoreEF.cs` — uses the scoped `DbContext` and `Database.CurrentTransaction` (via `DbContextTransactionAccessor`); raw SQL per database selected by `Database.ProviderName` through a small `SequenceSqlDialect` (the existing branching mechanism of the EF project is checked first and reused).
2. `src/Encina.MongoDB/Sequences/SequenceStoreMongoDB.cs` — `findOneAndUpdate` inside `IMongoSessionAccessor.CurrentSession`; no session returns `sequence.transaction_required`.
3. Registration: `RegisterSequenceStore<TDbContext>` in the EF `ServiceCollectionExtensions.cs` (pattern `:488`) and the MongoDB `ServiceCollectionExtensions.cs`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #1238.

CONTEXT:
- Plan: docs/plans/sequence-generator-implementation-plan-1238.md. Phases 1-3 exist.

TASK:
Implement SequenceStoreEF and SequenceStoreMongoDB and register them.

KEY RULES:
- EF: no SaveChanges inside the store; the statement runs in the context's current transaction.
- MongoDB: every operation uses the session; a duplicate-key error on the first upsert is retried once.
- A standalone MongoDB server fails closed.
- Async with CancellationToken; tenant filter on every operation.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Transactions/DbContextTransactionAccessor.cs
- src/Encina.EntityFrameworkCore/BreakTheGlass/BreakTheGlassStoreEF.cs
- src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs
```

</details>

---

### Phase 5: Cross-Cutting Integration

> **Goal**: Health, validation and tenancy.

<details>
<summary><strong>Tasks</strong></summary>

1. `Health/SequenceStoreHealthCheck.cs` (core) — calls `ISequenceStore.CheckAsync`; `DefaultName`, static `Tags`.
2. Startup validation: `SequenceOptionsValidator` registered with `ValidateOnStart`; DI test with `ValidateOnBuild` and `ValidateScopes` for each provider's registration.
3. Architecture test `SequencePackageHasNoRegulationNamesTests` asserting the public surface of the package contains none of the names listed in ADR-057.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #1238.

CONTEXT:
- Plan: docs/plans/sequence-generator-implementation-plan-1238.md, cross-cutting matrix.

TASK:
Add the health check, the startup validation wiring, the DI tests and the naming architecture test.

KEY RULES:
- Health check: scoped resolution through CreateScope(); result data carries codes only.
- The naming test reads the forbidden names from the ADR-057 list; it fails on any public type, member, namespace or option.

REFERENCE FILES:
- src/Encina.Security.BreakTheGlass/Health/BreakTheGlassHealthCheck.cs
- docs/architecture/adr/057-verifactu-boundary.md
- tests/Encina.UnitTests/Testing/Architecture/
```

</details>

---

### Phase 6: Observability

> **Goal**: Activity, metrics and logging (REQ-062).

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs` — register `Sequences = (5750, 5799)`; add it to the `Encina` entry of `AssemblyRanges` in `EncinaEventIdAllocationTests.cs`.
2. `Sequences/SequenceDiagnostics.cs` — `ActivitySource` and `Meter` `Encina.Sequences`; counter `encina.sequence.allocated` (tag `encina.sequence.key_hash`); histogram `encina.sequence.wait_time`; span `encina.sequence.next` with `encina.tenant_id`.
3. `Sequences/SequenceLogMessages.cs` — `[LoggerMessage]` EventIds 5750-5757, packed (allocated, transaction required, tenant required, lock timeout, store failed, key invalid).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #1238.

CONTEXT:
- Plan: docs/plans/sequence-generator-implementation-plan-1238.md; EventIds 5750-5799.

TASK:
Register the range, map it in the allocation test, add diagnostics and log messages and call them from the generator.

KEY RULES:
- Register the range before using an EventId; pack sequentially.
- Tags carry the key hash and tenant id only; never the key value, a number or the SQL error text.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina.Compliance.Consent/Diagnostics/
```

</details>

---

### Phase 7: Testing

> **Goal**: Every flag reaches its manifest target; gaplessness is proven on the 10 providers.

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: key validator, tenant table, generator flow, in-memory store, options validator, DI tests.
2. Guard and Contract: one `SequenceStoreContract` for the in-memory store and the 10 providers (first value, increment, rollback returns the number, two tenants independent, no transaction fails).
3. Property (FsCheck): for any interleaving of commit and rollback outcomes, committed numbers form a gapless range.
4. Integration: shared `[Collection]` fixtures on the 10 providers; 50 concurrent allocations per provider; MongoDB on `MongoDB-ReplicaSet` and a standalone fail-closed case.
5. Load: `.cs` concurrent allocation on one key per provider family under `tests/Encina.LoadTests/Database/Sequences/`; benchmark `.md` justification.
6. Manifest entries for `Encina.json` and each provider manifest with one-sentence justifications.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #1238.

CONTEXT:
- Plan: docs/plans/sequence-generator-implementation-plan-1238.md, Design Choice 5; manifests in .github/coverage-manifest/.

TASK:
Write the tests in the Phase 7 Tasks and the manifest entries.

KEY RULES:
- Tests execute real package code; Shouldly through Encina.Testing.Shouldly; no Thread.Sleep.
- Shared [Collection] fixtures; ClearAllDataAsync in InitializeAsync; never IClassFixture for databases.
- Tests of a provider-dependent feature cover all 10 providers.
- A test never swallows an exception thrown by the code it drives.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Messaging/Outbox/OutboxAtomicityTestsBase.cs
- tests/Encina.ContractTests/IdGeneration/IdGeneratorContractTests.cs
- tests/Encina.LoadTests/Security/ABAC/ABACDecisionAuditLoadTests.cs
```

</details>

---

### Phase 8: Documentation and Finalization

> **Goal**: Documentation, changelog, public API and build verification.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on every public member; `changelog.d/1238-sequence-generator.added.md`.
2. Core README section and `docs/features/sequences.md` (usage inside a unit of work, contention note, the neutral-primitive sentence); `docs/INVENTORY.md`.
3. `PublicAPI.Unshipped.txt` for `Encina` and the provider packages; build with zero warnings; per-flag coverage and CRAP table.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of issue #1238.

CONTEXT:
- Plan: docs/plans/sequence-generator-implementation-plan-1238.md.

TASK:
Write the documentation, changelog fragment and PublicAPI lines listed in the Phase 8 Tasks.

KEY RULES:
- Never type coverage figures by hand; use covref markers.
- The README states the primitive is regulation-neutral and Encina ships no Verifactu code.
- The PR records the ADR-018 evaluation of the 12 functions. English only.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- docs/architecture/adr/057-verifactu-boundary.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Relevance to this plan |
|--------|-----------|------------------------|
| SPEC-002 | REQ-045, AC-039, AC-043, S8, DEC-002, REQ-061, REQ-062 | Requirement, acceptance, scenario, no Verifactu semantics, tenancy, telemetry |
| ADR-057 | Verifactu boundary, D23 | Generic names only; README sentence |
| ADR-050 | Module requirements | MongoDB without a replica set refused for Transactions |
| SPEC-002 note H | H-R04, H-R13 | Gapless invoice numbering as the use case |
| ADR-018, ADR-021 | Cross-cutting, EventIds | Matrix, range 5750-5799 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Use in this feature |
|-----------|----------|---------------------|
| `ITransactionLeaseSource`, `IDbTransactionAccessor`, `IMongoSessionAccessor` | `src/Encina.Messaging/Transactions/`, `src/Encina.MongoDB/Transactions/` | Joining the caller's transaction |
| `DbContextTransactionAccessor` | `src/Encina.EntityFrameworkCore/Transactions/DbContextTransactionAccessor.cs:42` | EF Core current transaction |
| Outbox stores per provider | `src/Encina.{ADO,Dapper}.*/Outbox/`, `src/Encina.MongoDB/Outbox/` | Layout and transaction-joining pattern |
| `DefaultConsentService` tenant table | `src/Encina.Compliance.Consent/Services/DefaultConsentService.cs:121-128` | Tenant resolution |
| `MongoDbIndexCreator` | `src/Encina.MongoDB/MongoDbIndexCreator.cs` | Unique index |
| `MongoDB-ReplicaSet` collection | `tests/Encina.IntegrationTests/Infrastructure/MongoDB/ReadWriteSeparation/MongoDbReplicaSetCollection.cs:28` | MongoDB integration tests |
| Load-test examples | `tests/Encina.LoadTests/Security/ABAC/` | Concurrent allocation tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (sequences) | 5750-5799 (new `Sequences`) | 5750-5757 used; mapped to the existing `Encina` entry of the allocation test |
| Neighbours in the family | 5500-5549 (#1234), 5550-5699 (#1235), 5700-5749 (#1236) | Reserved by the sibling plans |

### Estimated File Count

| Category | New | Modified | Notes |
|----------|----:|---------:|-------|
| Core abstractions, options, validator, in-memory store, health, diagnostics | 11 | 2 | Phases 1, 5, 6 |
| Scripts and schemas | 8 | 4 | Phase 2 |
| ADO and Dapper stores | 6 | 6 | Phase 3 |
| EF Core and MongoDB | 6 | 4 | Phases 2 and 4 |
| Tests | ~30 | ~4 | Phase 7, one `.md` justification |
| Documentation | 4 | 3 | Phase 8 |

### Findings in the Current Code

- No row-lock or atomic-increment SQL exists anywhere in `src/`; each dialect is new (Design Choice 2).
- ADO MySQL has no script `030`; script numbers are aligned at implementation time with plan #1234.
- The EF Core project serves three databases, so SQL selection per provider name is part of Phase 4.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 / C# 14 library (nullable on, Railway Oriented Programming with Either<EncinaError, T>).
Issue #1238 (SPEC-002 P-36, REQ-045, AC-039, AC-043, S8) adds ISequenceGenerator: a gapless number per
(tenant, sequence key), enlisted in the caller's transaction, on all 10 database providers, regulation-neutral
(ADR-057). Plan: docs/plans/sequence-generator-implementation-plan-1238.md. Rules: AGENTS.md.

IMPLEMENTATION OVERVIEW:
1. Core: ISequenceGenerator, ISequenceStore, DefaultSequenceGenerator, key validator, options, errors, in-memory store.
2. Table and scripts, EF entity, Mongo document and unique index.
3. ADO x3 and Dapper x3 stores: insert-if-missing then atomic increment-and-return.
4. EF Core and MongoDB stores.
5. Health check, validation, DI tests, naming architecture test.
6. EventIds 5750-5799, diagnostics.
7. Tests: contract on 10 providers, property, 50 concurrent allocations, load tests.
8. Docs, changelog, PublicAPI, build, coverage, CRAP.

KEY PATTERNS:
- No ambient transaction means Left(sequence.transaction_required); never a private transaction.
- A missing tenant fails closed; keys are validated; tags carry a key hash, never the key.
- Generic names only; README states Encina ships no Verifactu code.
- Async database calls with CancellationToken; a database store wins over the in-memory default.
- Integration tests on shared [Collection] fixtures; every flag reaches its target; CRAP <= 10.

REFERENCE FILES:
- src/Encina.Messaging/Transactions/ (lease, accessors)
- src/Encina.{ADO,Dapper}.*/Outbox/, src/Encina.MongoDB/Outbox/, src/Encina.EntityFrameworkCore/Transactions/
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- docs/architecture/adr/057-verifactu-boundary.md
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A cached number would defeat gaplessness; no read path |
| 2 | OpenTelemetry | ✅ | `encina.sequence.next` span, `allocated` counter and `wait_time` histogram with `encina.tenant_id` and a key hash (Phase 6, REQ-062) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` EventIds 5750-5757 in the new `Sequences` range (Phase 6) |
| 4 | Health Checks | ✅ | `SequenceStoreHealthCheck`: table or collection reachable (Phase 5) |
| 5 | Validation | ✅ | Key validator and options validator at start-up (Phases 1 and 5) |
| 6 | Resilience | ❌ | No call to an external system; a lock timeout returns a `Left` and retrying belongs to the caller's transaction |
| 7 | Distributed Locks | ✅ | The row lock inside the transaction serialises concurrent callers on every provider (Design Choice 2) |
| 8 | Transactions | ✅ | Enlisted in the ambient transaction; fails closed without one (Design Choice 3) |
| 9 | Idempotency | ❌ | Not a message or request entry point; a retried transaction that rolled back correctly gets the same number |
| 10 | Multi-Tenancy | ✅ | Rows keyed by `(TenantId, SequenceKey)`, fail closed, default tenant when tenancy is off (Design Choice 4, AC-043) |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ❌ | No compliance-relevant operation beyond those already audited; the audit of an invoice belongs to the caller (P-38) |

---

## Prerequisites & Dependencies

| Item | State | Relation |
|------|-------|----------|
| #1186 EU regulatory readiness (SPEC-002) | open (EPIC) | Parent |
| ADR-057 Verifactu boundary (P-24) | merged | Naming rule D23 and the README sentence |
| P-38 Persistent hash-chained log | open | Reference scenario S8 combines both; not blocking |
| #718 Transactional outbox atomicity | merged | The lease and the transaction-joining pattern are reused |
| MongoDB replica set in CI | existing `MongoDB-ReplicaSet` collection | Required for the MongoDB integration tests |
| Plans #1234, #1235, #1236 | this family | EventId block 5750-5799 reserved here; script numbers aligned at implementation |

---

## Next Steps

1. No Dilemma is open: every Design Choice is Settled by the issue, ADR-050, ADR-057, SPEC-002 or AGENTS.md; they still need the adversarial verification before the worker brief.
2. The feature exceeds one worker pull request (15 files, 1500 lines). Split it into four pull requests: (a) Phases 1, 5 and 6 (core, health, diagnostics, in-memory store); (b) Phase 2 and the three ADO providers; (c) the three Dapper providers; (d) EF Core and MongoDB with the integration, load and documentation work.
3. Link this plan from #1238 and start sub-issue (a) with an `issue-worker`.
