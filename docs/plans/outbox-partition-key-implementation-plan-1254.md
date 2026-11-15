# Implementation Plan: Outbox `PartitionKey` Column on All 10 Providers (`Encina.Messaging` + database providers)

> **Issue**: [#1254](https://github.com/dlrivada/Encina/issues/1254)
> **Type**: Feature (SPEC-002 P-51, REQ-033 and REQ-042, AC-033; part of #1186)
> **Complexity**: Medium (8 phases, 10 database providers, 120 file edits, delivered in 12 pull requests that each compile on their own)
> **Estimated Scope**: about 900 lines of production code (including SQL scripts) + about 2,200 lines of tests, documentation and manifests; no pull request over 450 changed lines
> **Family**: message priority and partitioning (#123, #251, #301, #469, #1254). This plan OWNS `IOutboxMessage.PartitionKey` and `IPartitionedMessage`; the ordering and pacing behavior built on the column is #469. It merges FIRST in the family because it changes the same provider files as #123.

---

## Summary

Give every outbox message an optional, producer-set **partition key**: a nullable `PartitionKey` property on `IOutboxMessage`, a nullable, indexed column (or field) on all 10 database providers, and a producer path (`IPartitionedMessage`) that sets it. Stores persist and return it unchanged; a message without a key reads back as `null` (AC-033). No ordering behavior changes here: `OutboxBatchProcessor` still processes pending messages as it does today.

Why now: SPEC-002 DEC-005 (a) decided that the column ships before 1.0 because adding it later is a schema migration for every user. Ordered delivery per appointment (scenario S3) and paced submission per invoice series (S9) need it (#469).

Facts verified on `origin/main` (2026-11-13):

- `src/Encina.Messaging/Outbox/IOutboxMessage.cs` has `Id`, `NotificationType`, `Content`, `CreatedAtUtc`, `ProcessedAtUtc`, `ErrorMessage`, `RetryCount`, `NextRetryAtUtc` and no partition key.
- `IOutboxMessageFactory.Create(Guid id, string notificationType, string content, DateTime createdAtUtc)` (declared in `OutboxOrchestrator.cs`) has two call sites: `OutboxOrchestrator.cs:98` (`AddAsync`) and `OutboxWriter.cs:65`. `TransactionalOutboxPipelineBehavior` builds an `OutboxWriter` and calls `WriteAsync`, so it needs no change of its own.
- `IOutboxMessage` has 15 implementers: the 8 provider `OutboxMessage` classes (ADO x3, Dapper x3, EF Core, MongoDB), `FakeOutboxMessage` (`Encina.Testing.Fakes`), the nested `FakeOutboxMessage` of `OutboxMessageFaker` (`Encina.Testing.Bogus`), `ArbitraryOutboxMessage` (`Encina.Testing.FsCheck`), `BenchmarkOutboxMessage` in the ADO and Dapper benchmark projects, `PendingMessage` in `RetryAfterCeilingProperties` and `TestOutboxMessage` in `OutboxOrchestratorTests`. `IOutboxMessageFactory` has 9 implementers (8 providers and `FakeOutboxMessageFactory`).
- The integration tests do not run the `Scripts/*.sql` files: the table is created by `tests/Encina.TestInfrastructure/Schemas/{SqlServer,PostgreSql,MySql}Schema.cs` (shared by ADO.NET and Dapper), and the benchmark projects have their own `AdoSchemaBuilder.cs` and `DapperSchemaBuilder.cs`. `OutboxStoreADORequeueTransactionTests` and `DapperStoreEnlistmentTests` (Dapper.SqlServer store) create the table with their own DDL.
- ADO.NET and Dapper packages ship the table in `Scripts/001_CreateOutboxMessagesTable.sql` and `000_CreateAllTables.sql` (`CREATE TABLE IF NOT EXISTS`); EF Core uses `OutboxMessageConfiguration`; MongoDB uses `MongoDbIndexCreator`.
- `OutboxProcessorMetrics` (shared `"Encina"` meter) and `OutboxActivitySource.StartAdd(string messageType, string messageId)` exist and are internal; `StartAdd` carries no tenant tag today.
- The outbox has no `TenantId` (open issue #737 adds it). `Encina.Messaging` already has an entry in `EncinaEventIdAllocationTests`; `EventIdRanges.MessagingOutbox` is 2000-2099.
- ADR 062 (error category contract) is merged (PR #3599): new error codes are created with `EncinaErrors.Create(code, ErrorCategory, message)`.

- **Scope**: the column, the index, the producer API, validation, one counter and one activity attribute. Not in scope: ordering, pacing, holding (#469), priority (#123).
- **Affected packages**: `Encina.Messaging`, `Encina`, `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.Testing.Fakes`, `Encina.Testing.Bogus`, `Encina.Testing.FsCheck`, plus `tests/Encina.TestInfrastructure` and the benchmark projects. `Encina.Testing` (`OutboxTestHelper`) and `Encina.Testing.Verify` (`EncinaVerify.PrepareOutboxMessages`) only read `IOutboxMessage`; they compile unchanged and the Verify snapshot keeps its current fields (the key is not added to snapshots; Trivial).
- **Provider category**: Database (10), all required (AGENTS.md section 5).

---

## Design Choices

<details>
<summary><strong>1. How the producer sets the key — `IPartitionedMessage` read by the write paths (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) `IPartitionedMessage { string PartitionKey { get; } }`; the writer and the orchestrator read it from the notification** | Works on every path, including `TransactionalOutboxPipelineBehavior` where the producer has no access to the factory; the interface is the one #469 already proposes | A message type that does not own its key cannot supply it |
| **B) An explicit `partitionKey` argument on `OutboxOrchestrator.AddAsync` only** | No new interface | Does not reach the domain-event path (`IHasNotifications`), which is the main producer |
| **C) Both** | Covers the message that cannot own its key | Two ways to set one value |

### Chosen Option: **A — `IPartitionedMessage` on the notification** (recommended, pending the maintainer)

### Rationale

- Option A wins when the key derives from message content (appointment id, invoice series), which are the two reference scenarios S3 and S9.
- Option C wins if a producer must set the key from outside the message; nothing in SPEC-002 asks for it, and it can be added later without touching storage.
- The interface lives in `Encina.Messaging` (`src/Encina.Messaging/Outbox/IPartitionedMessage.cs`); #469 adds the specialised variants on top of it.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Type of the key: `string?`, null means "no partition"</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) `string?`, null means no partition** | Matches SPEC-002 and the issue; fits saga ids, tenant ids, appointment codes and invoice series | A producer with a binary key must encode it |
| **B) `Guid?`** | Compact, fixed size | Does not fit the reference keys (appointment codes, invoice series) |

### Chosen Option: **A — `string?`**

### Rationale

- The issue and SPEC-002 REQ-033 fix the type: "`IOutboxMessage.PartitionKey` (`string?`)". Option B does not satisfy the cited text, so this is not a choice left open. Length, validation and comparison are separate choices (3 and 5).

**Class:** Settled
**Settled by:** SPEC-002 REQ-033 (nullable `PartitionKey`, set by the producer, persisted and returned unchanged) and issue #1254 ("`IOutboxMessage.PartitionKey` (`string?`)").

</details>

<details>
<summary><strong>3. Limit and validation of the key (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) At most 256 characters, null for none, empty or whitespace is `Left` `outbox.invalid_partition_key` (category `Validation`), the key is stored exactly as supplied (no trimming)** | 256 is the length the EF Core configurations already use for identifier-like columns; one meaning for "no key"; fits the SQL Server 900-byte and MySQL 3072-byte index key limits; nothing is silently rewritten | A producer with a longer natural key must hash it |
| **B) At most 128 or 450 characters** | 128 is smaller; 450 is the largest NVARCHAR key SQL Server can index with a second column | Another arbitrary number; 450 leaves no room for a composite index |
| **C) Reject only null-or-too-long; accept empty and whitespace as a key of their own** | Fewer rules | Two spellings of "no key" (null and empty) split a partition |
| **D) Trim the key before storing** | Tolerates stray spaces | The key is no longer returned unchanged (AC-033); two producers that differ only by spaces silently share a partition |

### Chosen Option: **A — 256 characters, `Left` for empty, whitespace or too long, no trimming** (recommended, pending the maintainer)

### Rationale

- Validation returns `Either`, it never throws (ADR-001, ADR-006); the code is added to `OutboxErrorCodes` and created with `ErrorCategory.Validation` (ADR 062 R3, field rules).
- Length is counted in UTF-16 code units (`string.Length`). SQL Server `NVARCHAR(256)` counts 2-byte units, MySQL `VARCHAR(256)` and PostgreSQL `VARCHAR(256)` count characters; a code-unit count is never lower than the character count, so a key that passes the check always fits the column. `NVARCHAR(256)` is 512 bytes, which with the 8-byte `CreatedAtUtc` stays inside the SQL Server 900-byte nonclustered index key limit.
- PostgreSQL uses `VARCHAR(256)` rather than `TEXT` so the database enforces the same limit as the check (Trivial, follows from this choice).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Index shape on the 10 providers (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Non-unique composite index on `(PartitionKey, CreatedAtUtc)`** | Serves the per-key, oldest-first lookup #469 needs; same definition on every provider; nulls allowed | Rows without a key are indexed too |
| **B) Index on `PartitionKey` alone** | Smaller entries | #469 still sorts by `CreatedAtUtc` per key |
| **C) Filtered or partial index `WHERE PartitionKey IS NOT NULL`** | Smaller index | Only SQL Server and PostgreSQL support it; MySQL and MongoDB would differ, which breaks provider coherence |
| **D) No index until #469** | Nothing to maintain now | The issue asks for the index now; adding it later is another schema change |

### Chosen Option: **A — `(PartitionKey, CreatedAtUtc)` on every provider** (recommended, pending the maintainer)

### Rationale

- The issue only says "column and index on the 10 providers", which options A, B and C all satisfy.
- Names follow the existing pattern: `IX_OutboxMessages_PartitionKey` (SQL Server, EF Core), `ix_outboxmessages_partitionkey` (PostgreSQL), `IX_OutboxMessages_PartitionKey` (MySQL), and a MongoDB compound index created in `MongoDbIndexCreator.CreateOutboxIndexesAsync`.
- Option C wins only if rows without a key dominate and index size matters; measure first (benchmark in #469).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Key comparison and collation across providers (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Ordinal (case-sensitive, accent-sensitive) on every provider: a binary collation on the column (`Latin1_General_100_BIN2` on SQL Server, `utf8mb4_bin` on MySQL), the default on PostgreSQL and MongoDB** | "A" and "a" are two partitions on all 10 providers; #469 behaves the same everywhere | Explicit collation clauses in four scripts and in the EF configuration |
| **B) The database's default collation** | No collation clause | MySQL and (usually) SQL Server treat "A" and "a" as one partition, PostgreSQL and MongoDB as two: the same producer groups differently per provider |
| **C) Normalise the key to one case before storing** | Case-insensitive everywhere | The key is no longer returned unchanged (AC-033) |

### Chosen Option: **A — ordinal comparison everywhere** (recommended, pending the maintainer)

### Rationale

- Provider coherence (AGENTS.md section 5) asks that switching provider changes only the registration; B breaks that for ordering semantics. The integration round trip adds a case-variant pair ("A" and "a") on every provider.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Tenant scoping of the key while the outbox has no `TenantId` (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Store the key opaque now; per-tenant partitions come from #737 (`TenantId` on `OutboxMessage`) and from #469 composing tenant and key** | Meets DEC-005 (column before 1.0) without waiting; no tenant column is invented here | The two-tenant acceptance test of #1254 cannot run until #737 lands |
| **B) Block #1254 on #737 and add `TenantId` to the same rows** | One tenant-aware design from the start | Delays the P0 column; #737 touches the same 10 providers and enlarges this change past the size limit |
| **C) The store prefixes the key with the current tenant id** | Per-tenant keys without a column | Stores would need `ITenantContext`; mixes tenancy into persistence; breaks "returns it unchanged" (AC-033) |

### Chosen Option: **A — opaque key now, tenant scoping through #737 and #469** (recommended, pending the maintainer)

### Rationale

- Option A wins when time-to-column matters, which is the point of DEC-005 (a).
- Option B wins if the maintainer wants the tenant acceptance bullets of #1254 to pass in the same pull request; the outbox row has no tenant to carry (verified: `IOutboxMessage` has no tenant property).
- AC-033 is satisfied by Option A (store and read back unchanged). Cross-cutting row 10 is "Defer: #737"; the two-tenant isolation test and the `encina.tenant_id` span tag are deferred with it, with a follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/outbox-partition-key-two-tenant-test.md) if #737 does not carry them.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Interface shape: how the key reaches the message (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) A `string? partitionKey` parameter on `IOutboxMessageFactory.Create`** | One call creates a complete message; no parallel path | The switch is atomic: the interface, 9 factories, `OutboxWriter`, `OutboxOrchestrator` and 17 test files that call `Create` change in one pull request (about 30 files, over the 15-file limit; it would need the `size-exception-approved` label) |
| **B) A second factory method `CreatePartitioned`** | Existing callers untouched | Two creation paths; 9 more factory methods |
| **C) A settable `string? PartitionKey { get; set; }` on `IOutboxMessage` (same style as `RetryCount` and `NextRetryAtUtc`); the two write paths assign it after `Create`** | No factory signature change; every pull request compiles on its own (providers add the property first, the interface member lands afterwards) | The factory returns a message whose key is assigned a line later |

### Chosen Option: **C — settable property, assigned by the write paths** (recommended, pending the maintainer)

### Rationale

- The review of this plan found that A cannot be split so that each step compiles; C changes no factory signature, so it fits the 15-file limit with the additive order of the Implementation Phases.
- If the maintainer picks A, its factory switch is one atomic change that the maintainer must size-approve; the rest of the split is unchanged.
- #123 later adds a priority to the same message; its plan decides its own shape.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Existing tables get the column? (no migration)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) The scripts create the column in new tables only; an existing table needs a manual `ALTER`, documented in the package README** | No migration code; matches pre-1.0 | A developer with a table from an earlier build adds the column by hand |
| **B) The scripts also `ALTER TABLE ... ADD COLUMN IF NOT EXISTS`** | Existing tables converge | A migration helper, which this repository does not carry |

### Chosen Option: **A — new tables only, README note for the manual `ALTER`**

### Rationale

- `CREATE TABLE IF NOT EXISTS` does not add a column to an existing table. Encina is pre-1.0 with no existing user and no migration support, so option B (a migration step in the scripts) is excluded by the cited rule.

**Class:** Settled
**Settled by:** AGENTS.md section 1 ("pre-1.0. There is no backward compatibility, no migration support and no existing user") and section 3 (no migration helpers).

</details>

<details>
<summary><strong>9. Smaller choices with no observable difference</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) MongoDB field `partitionKey` with `[BsonIgnoreIfNull]`; one EventId (6748, key rejected) and a counter instead of a per-message log; the Verify snapshot keeps its fields** | A missing field reads back as null; no DEBUG line per stored message (the counter carries the volume signal); no snapshot churn | None that a caller sees |
| **B) Always write `null`; a DEBUG log per message with a key** | Explicit field | One log line per stored message on the hot add path |

### Chosen Option: **A**

### Rationale

- Absent and null read back identically, a counter is the cheaper way to count partitioned adds, and no test or consumer reads the snapshot key.

**Class:** Trivial

</details>

---

## Implementation Phases

Twelve pull requests, each of at most 15 files and 1500 lines (the largest is about 450 lines), ordered so that `main` compiles after every merge. The order is additive first: the interface member lands only after every implementer already has the property (this order holds under choice 7 option C, the recommendation).

| PR | Content | Files | Lines (est.) |
| --- | --- | ---: | ---: |
| P1 | Core: `IPartitionedMessage`, resolver, error code (nothing consumes them yet) | 7 | 250 |
| P2 | ADO.SqlServer: property, column, index, store mapping, shared test schema | 11 | 250 |
| P3 | Dapper.SqlServer (and the Dapper enlistment test DDL) | 10 | 220 |
| P4 | ADO.PostgreSQL | 10 | 250 |
| P5 | Dapper.PostgreSQL | 9 | 200 |
| P6 | ADO.MySQL | 10 | 250 |
| P7 | Dapper.MySQL | 9 | 200 |
| P8 | EF Core and MongoDB | 12 | 400 |
| P9 | `IOutboxMessage.PartitionKey` and its non-provider implementers | 15 | 250 |
| P10 | Write paths, EventIds, log class | 12 | 350 |
| P11 | Counter, activity tag, exporter and no-leak tests | 8 | 300 |
| P12 | Contract test, documentation, justification files, changelog | 7 | 250 |
| **Total** | | **120** | **about 3,000** |

The total is a count of file edits: a file that two pull requests change (`OutboxOrchestrator.cs` and `OutboxWriter.cs` in P10 and P11, `AdoSchemaBuilder.cs` in P2, P4 and P6, `DapperSchemaBuilder.cs` in P3, P5 and P7) counts once in each. The header, this table and the Estimated File Count table all state 120.

### Phase 1: Core Contract (P1, additive)

<details>
<summary><strong>Tasks</strong></summary>

1. New `src/Encina.Messaging/Outbox/IPartitionedMessage.cs`: `string PartitionKey { get; }`.
2. New `src/Encina.Messaging/Outbox/OutboxPartitionKey.cs` (internal static): `Either<EncinaError, string?> Resolve(object notification)` returns `Right(null)` when the notification is not `IPartitionedMessage`, `Left` when the key is empty, whitespace or longer than 256 UTF-16 code units; the key is returned unchanged.
3. `OutboxOrchestrator.cs` (where `OutboxErrorCodes` lives): `OutboxErrorCodes.InvalidPartitionKey = "outbox.invalid_partition_key"`, created with `ErrorCategory.Validation`.
4. Tests (same PR): `tests/Encina.UnitTests/Messaging/Outbox/OutboxPartitionKeyTests.cs` (not partitioned, valid, empty, whitespace, 256 and 257 code units, a key with leading spaces returned unchanged, null notification), `tests/Encina.PropertyTests/Messaging/Outbox/OutboxPartitionKeyProperties.cs` (any non-whitespace string of 1 to 256 code units resolves to itself).
5. `PublicAPI.Unshipped.txt` of `Encina.Messaging` and the manifest entry of `Outbox/OutboxPartitionKey.cs` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina pre-1.0, plan docs/plans/outbox-partition-key-implementation-plan-1254.md (issue #1254, SPEC-002 P-51). Nothing consumes the new types yet; this pull request only adds them so that later pull requests compile.
TASK: Implement Phase 1 Tasks 1 to 4. Do not touch IOutboxMessage, the factory or any write path.
KEY RULES: Either<EncinaError, T> with no exceptions for invalid input (ADR-001, ADR-006); EncinaErrors.Create with ErrorCategory.Validation (ADR 062); XML docs on every public member; the key value is never logged; no [Obsolete].
REFERENCE FILES: src/Encina.Messaging/Outbox/IOutboxMessage.cs, src/Encina.Messaging/Outbox/OutboxOrchestrator.cs, src/Encina/Errors/ErrorCategory.cs.
```

</details>

### Phase 2: ADO.NET Providers (P2 SqlServer, P4 PostgreSQL, P6 MySQL)

<details>
<summary><strong>Tasks</strong></summary>

One pull request per database. For each ADO.NET package:

1. `Outbox/OutboxMessage.cs`: public `string? PartitionKey { get; set; }` (a concrete property; the interface member arrives in P9).
2. `Outbox/OutboxStoreADO.cs`: the key in the insert statement and the data-reader mapping (null through `DBNull`).
3. `Scripts/001_CreateOutboxMessagesTable.sql` and `Scripts/000_CreateAllTables.sql`: `PartitionKey NVARCHAR(256) NULL` (SQL Server), `partitionkey VARCHAR(256) NULL` (PostgreSQL), `PartitionKey VARCHAR(256) NULL` (MySQL), the binary collation of choice 5 and the index of choice 4.
4. The shared test schema `tests/Encina.TestInfrastructure/Schemas/{SqlServer,PostgreSql,MySql}Schema.cs`, the benchmark `AdoSchemaBuilder.cs` section of that database, and (P2 only) `OutboxStoreADORequeueTransactionTests`, whose own DDL needs the column; the package README gets the `ALTER` note of choice 8 (`docs-writer`).
5. Tests in the same PR: round trip on the real database through the shared `[Collection("ADO-<Database>")]` fixture (key, no key read back as null, a case-variant pair "A" and "a" stored as two keys).
6. `PublicAPI.Unshipped.txt` and the manifest entries of the package through `mechanical-fixer`.

Count per pull request: P2 11 (4 package files, 1 PublicAPI, 1 manifest, 1 schema, 1 benchmark builder, 1 existing test DDL, 1 integration test, 1 README), P4 and P6 10 (no existing-test DDL).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: ADO stores write SQL by hand and map readers by name or ordinal; scripts create the table (SQL Server bracketed identifiers, PostgreSQL lower-case, MySQL backticks); integration tests build the table from tests/Encina.TestInfrastructure/Schemas, not from the scripts.
TASK: Apply Phase 2 Tasks to ONE ADO.NET package per pull request.
KEY RULES: async methods with CancellationToken only; parameters, never concatenated values; nullable column; scripts stay as they are (CREATE TABLE IF NOT EXISTS, no ALTER, no migration helper); shared Collection fixtures; the benchmark schema builder changes in the same pull request as the store.
REFERENCE FILES: src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs, src/Encina.ADO.PostgreSQL/Scripts/001_CreateOutboxMessagesTable.sql, tests/Encina.TestInfrastructure/Schemas/SqlServerSchema.cs, tests/Encina.IntegrationTests/ADO/SqlServer/Outbox/OutboxStoreADOTests.cs.
```

</details>

### Phase 3: Dapper Providers (P3 SqlServer, P5 PostgreSQL, P7 MySQL)

<details>
<summary><strong>Tasks</strong></summary>

One pull request per database, after the ADO.NET pull request of the same database (which already changed the shared test schema). For each Dapper package: `OutboxMessage.cs` (concrete property), `OutboxStoreDapper.cs` (insert lists the new column and parameter; the select reads `*`, so the property name must match the column), the two scripts, the `DapperSchemaBuilder.cs` section of that database, the round-trip integration test (`[Collection("Dapper-<Database>")]`), `PublicAPI.Unshipped.txt`, the manifest entries and the README note. P3 also updates `DapperStoreEnlistmentTests` (its DDL feeds the Dapper.SqlServer store).

Count: P3 10 (4 package files, 1 PublicAPI, 1 manifest, 1 benchmark builder, 1 integration test, 1 README, 1 enlistment test); P5 and P7 9 (no enlistment test).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: Dapper stores use CommandDefinition and SELECT * mapped to the provider OutboxMessage class (src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs).
TASK: Apply Phase 3 to ONE Dapper package per pull request, mirroring Phase 2.
KEY RULES: same as Phase 2; PostgreSQL column mapping stays consistent with the existing lower-case convention.
REFERENCE FILES: src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs, src/Encina.Dapper.PostgreSQL/Outbox/OutboxStoreDapper.cs, src/Encina.Dapper.MySQL/Outbox/OutboxStoreDapper.cs, tests/Encina.UnitTests/Dapper/Messaging/DapperStoreEnlistmentTests.cs.
```

</details>

### Phase 4: EF Core (3 providers) and MongoDB (P8)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.EntityFrameworkCore/Outbox/OutboxMessage.cs` (concrete property) and `OutboxMessageConfiguration.cs` (`HasMaxLength(256)`, `IsRequired(false)`, the case-sensitive collation of choice 5 per provider, `HasIndex(x => new { x.PartitionKey, x.CreatedAtUtc }).HasDatabaseName("IX_OutboxMessages_PartitionKey")`); `OutboxStoreEF.cs` needs no query change (the entity carries the property).
2. `src/Encina.MongoDB/Outbox/OutboxMessage.cs` (`[BsonElement("partitionKey")]`, `[BsonIgnoreIfNull]`) and `MongoDbIndexCreator.cs` (compound index on `partitionKey` and `createdAtUtc`).
3. Integration round trip (AC-033) on EF Core SqlServer, PostgreSQL and MySQL (`OutboxStoreEFSqlServerTests`, `OutboxStoreEFPostgreSqlTests`, `OutboxStoreEFMySqlTests`) and on MongoDB (`OutboxStoreMongoDBIntegrationTests`), shared `[Collection]` fixtures.
4. `PublicAPI.Unshipped.txt` (EF Core, MongoDB), the two manifests, through `mechanical-fixer`.

Count: 4 src + 4 tests + 2 PublicAPI + 2 manifests = 12.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: One EF Core store serves SqlServer, PostgreSQL and MySQL; MongoDB indexes are created in MongoDbIndexCreator.CreateOutboxIndexesAsync.
TASK: Implement Phase 4 Tasks 1 to 4.
KEY RULES: verify the EF model builds on all three relational providers; a missing MongoDB field must read back as null; async with CancellationToken; shared Collection fixtures with ClearAllDataAsync in InitializeAsync.
REFERENCE FILES: src/Encina.EntityFrameworkCore/Outbox/OutboxMessageConfiguration.cs, src/Encina.MongoDB/Outbox/OutboxMessage.cs, src/Encina.MongoDB/MongoDbIndexCreator.cs.
```

</details>

### Phase 5: Interface Member and Non-Provider Implementers (P9)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Outbox/IOutboxMessage.cs`: `string? PartitionKey { get; set; }` with XML docs (null means no partition). The 8 provider classes already have the property (P2 to P8), so they compile unchanged.
2. The 7 other implementers in the same PR: `FakeOutboxMessage` (`src/Encina.Testing.Fakes/Models`), `OutboxMessageFaker` and its nested `FakeOutboxMessage` with `WithPartitionKey(string? key)` (`src/Encina.Testing.Bogus`), `ArbitraryOutboxMessage` (`src/Encina.Testing.FsCheck/ArbitraryMessages.cs`), the two `BenchmarkOutboxMessage` classes, `PendingMessage` in `RetryAfterCeilingProperties` and `TestOutboxMessage` in `OutboxOrchestratorTests`.
3. Guard test (the faker parameter is nullable and accepts null, so the test asserts the valid result): `tests/Encina.GuardTests/Testing/Bogus/OutboxMessageFakerPartitionKeyGuardTests.cs`.
4. `PublicAPI.Unshipped.txt` of `Encina.Messaging`, `Encina.Testing.Fakes`, `Encina.Testing.Bogus` and the manifests of Fakes, Bogus and FsCheck through `mechanical-fixer` (the interface has no executable lines; its manifest entry is unchanged).

Count: 4 src + 4 test implementers + 1 guard test + 3 PublicAPI + 3 manifests = 15.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: Phases 1 to 4 gave every provider OutboxMessage a concrete PartitionKey property. IOutboxMessage has 15 implementers (listed in the plan Summary).
TASK: Implement Phase 5 Tasks 1 to 4 so that the whole solution compiles.
KEY RULES: the fake stores return the key unchanged; no reflection-only tests; guard test for a nullable parameter asserts the valid result; dotnet build of Encina.slnx with zero warnings.
REFERENCE FILES: src/Encina.Testing.Fakes/Models/FakeOutboxMessage.cs, src/Encina.Testing.Bogus/OutboxMessageFaker.cs, src/Encina.Testing.FsCheck/ArbitraryMessages.cs.
```

</details>

### Phase 6: Write Paths and Logging (P10)

<details>
<summary><strong>Tasks</strong></summary>

1. `OutboxOrchestrator.AddAsync` and `OutboxWriter`: after `Create`, call `OutboxPartitionKey.Resolve(notification)`; a `Left` fails the add (no swallowing), a `Right` is assigned to `message.PartitionKey`.
2. `src/Encina/Diagnostics/EventIdRanges.cs`: register `MessagingOutboxPartitionKey = (6748, 6748)` from the family block 6700-6749 (the block map is owned by the family registry in the plan-batch registry); add it to the `Encina.Messaging` entry of `AssemblyRanges` in `EncinaEventIdAllocationTests`.
3. New `src/Encina.Messaging/Diagnostics/OutboxPartitionKeyLogMessages.cs` (`[LoggerMessage]`): 6748 key rejected (error code only). The log class is created here because this is the first pull request that logs.
4. `OutboxMessageBuilder.WithPartitionKey` in `tests/Encina.TestInfrastructure/Builders`.
5. Tests: `OutboxWriterTests`, `OutboxOrchestratorTests` and `TransactionalOutboxPipelineBehaviorTests` (the key reaches the store; a `Left` key fails the add on every path); no DI test is added because no registration changes (the existing registration tests with `ValidateOnBuild` and `ValidateScopes` keep running).
6. `PublicAPI.Unshipped.txt` of `Encina` (the new `EventIdRanges` field) and the manifest entries of `Encina` and `Encina.Messaging` through `mechanical-fixer`.

Count: 4 src (Orchestrator, Writer, log class, `EventIdRanges.cs`) + 1 builder + 1 architecture test + 3 unit test files + 1 PublicAPI + 2 manifests = 12.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: ADR-021: register the range first (6748 is assigned to this plan inside the family block 6700-6749). The two Create call sites are OutboxOrchestrator.cs:98 and OutboxWriter.cs:65; TransactionalOutboxPipelineBehavior reaches OutboxWriter.
TASK: Implement Phase 6 Tasks 1 to 5.
KEY RULES: [LoggerMessage] source generator; the partition key may identify a data subject, so it is never a log argument, tag or metric dimension; errors are never swallowed; run the EventId architecture tests.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, src/Encina.Messaging/Outbox/OutboxWriter.cs.
```

</details>

### Phase 7: Observability (P11)

<details>
<summary><strong>Tasks</strong></summary>

1. `OutboxProcessorMetrics.cs` (internal): counter `encina.outbox.partitioned` on the shared `"Encina"` meter, incremented by `OutboxWriter` for each message with a key; `OutboxActivitySource.StartAdd`: boolean tag `encina.outbox.partitioned` (never the key). The `encina.tenant_id` span tag is deferred with #737 (choice 6).
2. Tests: an in-memory exporter test of the counter and of the add-span attribute (AC-044); a test that no log argument, activity tag or metric dimension carries the key; the existing metrics tests updated.
3. Manifest entries for the touched files through `mechanical-fixer`.

Count: 4 src (metrics, activity source, writer, orchestrator) + 3 tests + 1 manifest = 8.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT: Existing outbox metrics live in src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs; StartAdd(messageType, messageId) is in OutboxActivitySource.cs.
TASK: Implement Phase 7 Tasks 1 to 3.
KEY RULES: the key is never a tag, metric dimension or log argument; use an in-memory exporter, never Thread.Sleep.
REFERENCE FILES: src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs, src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs.
```

</details>

### Phase 8: Contract, Documentation and Finalization (P12)

<details>
<summary><strong>Tasks</strong></summary>

1. Contract test (instantiates the real factories, no `.md`): `tests/Encina.ContractTests/Messaging/Outbox/OutboxPartitionKeyContractTests.cs` runs the 8 provider factories and `FakeOutboxMessageFactory` and asserts a created message reads `PartitionKey == null` and stores a set key unchanged.
2. Justification files: Load and Benchmark only (`.md`, not a concurrent hot path; the index cost is measured in #469): `tests/Encina.LoadTests/Messaging/OutboxPartitionKey.md` and `tests/Encina.BenchmarkTests/Messaging/OutboxPartitionKey.md`. Property is a real test (P1); unit, guard, contract and integration never use a `.md`.
3. `changelog.d/1254-outbox-partition-key.added.md` (`mechanical-fixer`).
4. `docs-writer`: configuration note in the outbox guide, `src/Encina.Messaging/README.md` and `docs/INVENTORY.md` if it lists the new file.
5. ADR: none of its own; the ordering mechanism ADR belongs to #469. `dotnet build --configuration Release` with zero warnings; `dotnet test`; every flag at its target.

Count: 1 contract + 2 justification + 1 changelog + 3 docs = 7.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT: documentation, changelog fragments and PublicAPI lines have path owners (docs-writer, mechanical-fixer).
TASK: Finish Phase 8 Tasks 1 to 5.
KEY RULES: English only; coverage figures only through covref markers; write "the reference application", never a private client name; no .md for contract tests.
REFERENCE FILES: changelog.d/README.md, docs/plans/dsr-implementation-plan-404.md, docs/specifications/SPEC-002 section 5.11.
```

</details>

### Per-file coverage targets (entered in the PR that carries the tests)

| File | PR | Flag and target | Justification |
| --- | --- | --- | --- |
| `Outbox/OutboxPartitionKey.cs` | P1 | unit 100 | Every branch of `Resolve` (not partitioned, valid, empty, whitespace, 256, 257, null notification) is a unit test. |
| `Outbox/OutboxPartitionKey.cs` | P1 | property 90 | The property test covers the success path for generated keys; the failure branches are unit-covered. |
| `Outbox/OutboxPartitionKey.cs` | P1 | guard 0 | The type is internal and `Encina.Messaging` grants no `InternalsVisibleTo` to `Encina.GuardTests`; the null-notification case is asserted in the unit flag. |
| `Outbox/IPartitionedMessage.cs` | P1 | unit 0 | An interface declaration has no executable lines. |
| Each provider `OutboxStore*.cs` | P2 to P8 | integration: the current manifest target, never lowered | The added insert and read-mapping lines are exercised by the round-trip test of the same pull request; the worker reads the current value from the manifest and only raises it. |
| Each provider `OutboxMessage.cs` | P2 to P8 | integration: current target | The property is assigned and read by the same round trip. |
| `Outbox/OutboxWriter.cs`, `Outbox/OutboxOrchestrator.cs` | P10, P11 | unit and guard: current targets, integration: current target | The new branch (key resolved, key rejected, counter) is covered by the unit tests of the same pull requests. |
| `Diagnostics/OutboxPartitionKeyLogMessages.cs` | P10 | unit 0 | `[LoggerMessage]` declarations; bodies are generated, so the file has no executable lines. |
| `Diagnostics/OutboxProcessorMetrics.cs`, `OutboxActivitySource.cs` | P11 | unit: current target | The exporter test drives the new counter and tag. |
| `Models/FakeOutboxMessage.cs`, `OutboxMessageFaker.cs`, `ArbitraryMessages.cs` | P9 | unit: current target; guard 40 for the faker | The guard test of the nullable `WithPartitionKey` is the only guard entry point of the new code. |

Acceptance mapping: AC-033 (round trip, 10 providers) in P2 to P8; AC-044 (counter and span attribute) in P11; the two-tenant AC is deferred with choice 6; ADR-018 twelve functions in the matrix below.

Guard tests for the new public API (#3798): `IPartitionedMessage`, `IOutboxMessage.PartitionKey`, `OutboxErrorCodes.InvalidPartitionKey` and `EventIdRanges.MessagingOutboxPartitionKey` have no parameters and no executable code, so no guard test applies; the new public method with a nullable parameter is `OutboxMessageFaker.WithPartitionKey` (test of the valid result in P9).

---

## Research

### Relevant Standards & Specifications

| Standard or source | Relevance |
| --- | --- |
| SPEC-002 REQ-033, REQ-042, DEC-005 (a), P-51, AC-033 | The column now, nullable, producer-set, persisted unchanged on 10 providers |
| SPEC-002 REQ-061, AC-043 | Partitions are per tenant; carried by #737 and #469 |
| ADR 062 (merged, PR #3599) | Category of `outbox.invalid_partition_key` (`Validation`, R3) |
| Enterprise Integration Patterns | Message key for ordered delivery (Resequencer, #469) |
| AGENTS.md sections 1, 3, 5, 6, 7 | No migration support, opt-in, 10 providers, 12-function check, EventIds |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| --- | --- | --- |
| `IOutboxMessage`, `IOutboxMessageFactory`, `OutboxErrorCodes` | `src/Encina.Messaging/Outbox/` | Property, error code |
| `OutboxWriter`, `OutboxOrchestrator`, `TransactionalOutboxPipelineBehavior` | `src/Encina.Messaging/Outbox/` | Two call sites resolve the key; the behavior reaches the writer |
| Provider `OutboxMessage`, factory, store | `src/Encina.ADO.*`, `Encina.Dapper.*`, `Encina.EntityFrameworkCore`, `Encina.MongoDB` | Persist and return the key |
| SQL scripts and test schemas | `src/Encina.ADO.*/Scripts`, `src/Encina.Dapper.*/Scripts`, `tests/Encina.TestInfrastructure/Schemas` | Column and index |
| `OutboxProcessorMetrics`, `OutboxActivitySource` | `src/Encina.Messaging/Diagnostics/` | Counter and activity tag |

### Event ID Allocation

| Package | Range | Notes |
| --- | --- | --- |
| `Encina.Messaging` (`EventIdRanges.MessagingOutboxPartitionKey`, new; the name differs from `MessagingOutbox` 2000-2099) | 6748-6748 | One id (6748), from the family block 6700-6749 owned by the family registry; sibling plans hold their own sub-ranges. `Encina.Messaging` already has an `AssemblyRanges` entry; the new field is added to it in P10 |

### Estimated File Count

| Category | Files | Notes |
| --- | --- | --- |
| P1 core | 7 | 3 src, 2 tests, PublicAPI, manifest |
| P2 to P7 ADO.NET and Dapper | 59 | 11 + 10 + 10 + 9 + 10 + 9 (src, scripts, shared schema, benchmark builder, tests, PublicAPI, manifest, README) |
| P8 EF Core and MongoDB | 12 | 4 src, 4 tests, 2 PublicAPI, 2 manifests |
| P9 interface and implementers | 15 | 4 src, 4 test implementers, 1 guard test, 3 PublicAPI, 3 manifests |
| P10 write paths | 12 | 4 src, builder, architecture test, 3 unit tests, PublicAPI, 2 manifests |
| P11 observability | 8 | 4 src, 3 tests, 1 manifest |
| P12 contract, docs, changelog | 7 | contract test, 2 justification files, changelog, 3 docs |
| **Total** | **120** | 7 + 59 + 12 + 15 + 12 + 8 + 7 (file edits) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined Prompt — All Phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14. Issue #1254 (SPEC-002 P-51), plan docs/plans/outbox-partition-key-implementation-plan-1254.md. Read AGENTS.md first. This plan merges first in the family (#1254, #123, #301, #469).

IMPLEMENTATION OVERVIEW: (1) core types and error code (additive). (2) ADO.NET x3 and (3) Dapper x3, one pull request per package. (4) EF Core and MongoDB. (5) IOutboxMessage.PartitionKey and the other implementers. (6) write paths, EventId 6748, log class. (7) counter and activity tag. (8) contract test, docs, changelog. Twelve pull requests of at most 15 files and 1500 lines, each compiling on its own.

KEY PATTERNS: nullable string up to 256 UTF-16 code units; null means no partition; Left (ErrorCategory.Validation) for empty, whitespace or too long; ordinal comparison; index on (PartitionKey, CreatedAtUtc) on every provider; the key is never logged or used as a metric dimension; no ordering change; async database calls with CancellationToken; shared [Collection] fixtures; no migration helpers.

REFERENCE FILES: src/Encina.Messaging/Outbox/*.cs, src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs, src/Encina.EntityFrameworkCore/Outbox/OutboxMessageConfiguration.cs, src/Encina.MongoDB/MongoDbIndexCreator.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/specifications/SPEC-002 section 5.11.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A persisted column; no read path that benefits from caching |
| 2 | OpenTelemetry | ✅ | Counter `encina.outbox.partitioned` and activity tag on the existing add activity; never the key value (Phase 7); the tenant tag is deferred with #737 |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` 6748 in `EventIdRanges.MessagingOutboxPartitionKey` (Phase 6) |
| 4 | Health Checks | ❌ | No new checkable dependency; stalled partitions belong to #469 |
| 5 | Validation | ✅ | Key validated on the write path; `Left` with `outbox.invalid_partition_key` (Phase 1, 6) |
| 6 | Resilience | ❌ | No call to an external system |
| 7 | Distributed Locks | ❌ | No shared state is updated by concurrent hosts; claiming per key is #469 and #1251 |
| 8 | Transactions | ✅ | The key is written in the same `AddAsync` and transaction as the row (Phase 6) |
| 9 | Idempotency | ❌ | Not a message entry point; duplicate handling is #735, #148 and #1220 |
| 10 | Multi-Tenancy | ⏭️ | Defer: #737 (open: `TenantId` on `OutboxMessage`); #469 composes tenant and key; see choice 6 (Dilemma) |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping here; `ModuleId` on messaging entities is #747 |
| 12 | Audit Trail | ❌ | No compliance-relevant operation beyond those already audited |

---

## Prerequisites & Dependencies

- None blocking: this plan merges first. #737 (`TenantId` on `OutboxMessage`) is advisable before #469 and decides the tenant acceptance bullets (choice 6).
- Merge order P1, then P2 to P8 (a Dapper pull request after the ADO.NET pull request of the same database, because the shared test schema changes there), then P9, P10, P11, P12. P9 needs P2 to P8; P10 needs P1 and P9; P11 needs P10.
- #123, #301 and #469 are rebased on this change; #469 reads the column and adds the ordering.

## Next Steps

1. Maintainer answers choices 1, 3, 4, 5, 6 and 7 when work starts; the answers go to `## Maintainer Decisions`.
2. Worker briefs per pull request (P1 to P12) with the 15-file limit.
3. After merge: unblock #469 and rebase #123.
