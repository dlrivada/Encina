# Implementation Plan: Outbox `PartitionKey` Column on All 10 Providers (`Encina.Messaging` + database providers)

> **Issue**: [#1254](https://github.com/dlrivada/Encina/issues/1254)
> **Type**: Feature (SPEC-002 P-51, REQ-033 and REQ-042, AC-033; part of #1186)
> **Complexity**: Medium (8 phases, 10 database providers, about 70 files, delivered in 5 pull requests)
> **Estimated Scope**: about 700 lines of production code + about 1,100 lines of tests
> **Family**: message priority and partitioning (#123, #251, #301, #469, #1254). This plan OWNS `IOutboxMessage.PartitionKey` and `IPartitionedMessage`; the ordering and pacing behavior built on the column is #469. It merges FIRST in the family because it changes the same provider files as #123.

---

## Summary

Give every outbox message an optional, producer-set **partition key**: a nullable `PartitionKey` property on `IOutboxMessage`, a nullable, indexed column (or field) on all 10 database providers, and a producer path (`IPartitionedMessage`) that sets it. Stores persist and return it unchanged; a message without a key reads back as `null` (AC-033). No ordering behavior changes here: `OutboxBatchProcessor` still processes pending messages as it does today.

Why now: SPEC-002 DEC-005 (a) decided that the column ships before 1.0 because adding it later is a schema migration for every user. Ordered delivery per appointment (scenario S3) and paced submission per invoice series (S9) need it (#469).

Facts verified in today's code:

- `src/Encina.Messaging/Outbox/IOutboxMessage.cs` has `Id`, `NotificationType`, `Content`, `CreatedAtUtc`, `ProcessedAtUtc`, `ErrorMessage`, `RetryCount`, `NextRetryAtUtc` and no partition key.
- `IOutboxMessageFactory.Create(Guid id, string notificationType, string content, DateTime createdAtUtc)` (in `OutboxOrchestrator.cs`) is called from `OutboxOrchestrator.AddAsync`, `OutboxWriter` and `TransactionalOutboxPipelineBehavior`.
- Each of the 8 provider packages has its own `OutboxMessage`, `OutboxMessageFactory` and store; ADO.NET and Dapper packages create the table from `Scripts/001_CreateOutboxMessagesTable.sql` and `000_CreateAllTables.sql`; EF Core uses `OutboxMessageConfiguration`; MongoDB uses `MongoDbIndexCreator`.
- The outbox has no `TenantId` (open issue #737 adds it).

- **Scope**: the column, the index, the producer API, validation, one counter and one activity attribute. Not in scope: ordering, pacing, holding (#469), priority (#123).
- **Affected packages**: `Encina.Messaging`, `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.Testing.Fakes`, `Encina.Testing.Bogus`, `Encina.Testing`.
- **Provider category**: Database (10), all required (AGENTS.md section 5).

---

## Design Choices

<details>
<summary><strong>1. How the producer sets the key — `IPartitionedMessage` read by the three write paths (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) `IPartitionedMessage { string PartitionKey { get; } }`; the writer, orchestrator and transactional behavior read it from the notification and pass it to `IOutboxMessageFactory.Create`** | Works on every path, including `TransactionalOutboxPipelineBehavior` where the producer has no access to the factory; the interface is the one #469 already proposes | A message type that does not own its key cannot supply it |
| **B) An explicit `partitionKey` argument on `OutboxOrchestrator.AddAsync` only** | No new interface | Does not reach the domain-event path (`IHasNotifications`), which is the main producer |
| **C) Both** | Covers the message that cannot own its key | Two ways to set one value |

### Chosen Option: **A — `IPartitionedMessage` on the notification** (recommended, pending the maintainer)

### Rationale

- Option A wins when the key derives from message content (appointment id, invoice series), which are the two reference scenarios S3 and S9.
- Option C wins if a producer must set the key from outside the message; nothing in SPEC-002 asks for it, and it can be added later without touching storage.
- The interface lives in `Encina.Messaging` (`src/Encina.Messaging/Outbox/IPartitionedMessage.cs`); #469 adds the specialised variants (`ISagaPartitionedMessage`, `ITenantPartitionedMessage`, `IAggregatePartitionedMessage`) on top of it.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Type, length and validation of the key</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) `string?`, at most 256 characters, null means "no partition", empty or whitespace is rejected with `Left`** | Matches SPEC-002 (`string?`); 256 is the length the EF Core configurations already use for identifier-like columns; rejecting empty avoids two meanings for "no key" | A producer with a longer natural key must hash it |
| **B) `Guid?`** | Compact, fixed size | Saga ids fit, but tenant ids, appointment codes and invoice series do not |
| **C) Unbounded `string?`** | No limit to explain | Cannot be indexed on MySQL (`utf8mb4` key length limit) or SQL Server (900-byte index key limit) without prefixing |

### Chosen Option: **A — `string?` up to 256 characters, null for none, `Left` for empty or too long**

### Rationale

- The error code is `outbox.invalid_partition_key` (added to `OutboxErrorCodes`); validation returns `Either`, it never throws (ADR-001, ADR-006).
- 256 characters keeps the index key inside the SQL Server 900-byte nonclustered limit (`NVARCHAR(256)` is 512 bytes, plus the 8-byte `CreatedAtUtc`); the plan uses `NVARCHAR(256)` on SQL Server and `VARCHAR(256)` on MySQL and PostgreSQL.

**Class:** Settled
**Settled by:** SPEC-002 REQ-033 (nullable `PartitionKey`, set by the producer, persisted and returned unchanged) and issue #1254 ("`IOutboxMessage.PartitionKey` (`string?`)"); ADR-001 and ADR-006 for `Left` on invalid input.

</details>

<details>
<summary><strong>3. Index shape on the 10 providers</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Non-unique index on `(PartitionKey, CreatedAtUtc)`** | Serves the per-key, oldest-first lookup #469 needs; same definition on every provider; nulls allowed | Rows without a key are indexed too |
| **B) Filtered or partial index `WHERE PartitionKey IS NOT NULL`** | Smaller index | Only SQL Server and PostgreSQL support it; MySQL and MongoDB would differ, which breaks provider coherence |
| **C) No index until #469** | Nothing to maintain now | The issue asks for the index now; adding it later is another schema change |

### Chosen Option: **A — `(PartitionKey, CreatedAtUtc)` on every provider**

### Rationale

- Names follow the existing pattern: `IX_OutboxMessages_PartitionKey` (SQL Server, EF Core), `ix_outboxmessages_partitionkey` (PostgreSQL), MongoDB compound index created in `MongoDbIndexCreator.CreateOutboxIndexesAsync`.
- Option B wins only if rows without a key dominate and index size matters; measure first (benchmark in the Testing phase).

**Class:** Settled
**Settled by:** issue #1254 ("Column and index on the 10 providers") and AGENTS.md section 5 (provider coherence: the same shape on every provider).

</details>

<details>
<summary><strong>4. Tenant scoping of the key while the outbox has no `TenantId` (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Store the key opaque now; per-tenant partitions come from #737 (`TenantId` on `OutboxMessage`) and from #469 composing tenant and key** | Meets DEC-005 (column before 1.0) without waiting; no tenant column is invented here | The two-tenant acceptance test of #1254 cannot run until #737 lands |
| **B) Block #1254 on #737 and add `TenantId` to the same rows** | One tenant-aware design from the start | Delays the P0 column; #737 touches the same 10 providers and enlarges this change past the size limit |
| **C) The store prefixes the key with the current tenant id** | Per-tenant keys without a column | Stores would need `ITenantContext`; mixes tenancy into persistence; breaks "returns it unchanged" (AC-033) |

### Chosen Option: **A — opaque key now, tenant scoping through #737 and #469** (recommended, pending the maintainer)

### Rationale

- Option A wins when time-to-column matters, which is the point of DEC-005 (a).
- Option B wins if the maintainer wants the tenant acceptance bullets of #1254 to pass in the same pull request; the issue text copies the SPEC-002 tenant bullet, but the outbox row has no tenant to carry (verified: `IOutboxMessage` has no tenant property).
- AC-033 is satisfied by Option A (store and read back unchanged).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Interface shape: property on `IOutboxMessage` and parameter on the factory</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) `string? PartitionKey { get; set; }` on `IOutboxMessage` and a `string? partitionKey` parameter on `IOutboxMessageFactory.Create`** | Same style as the other settable properties (`RetryCount`, `NextRetryAtUtc`); one factory call per message | Changes the factory signature in 9 factories and 3 callers |
| **B) A second factory method `CreatePartitioned`** | Existing callers untouched | Two creation paths; pre-1.0 there is no compatibility to protect (AGENTS.md section 1) |

### Chosen Option: **A — property plus factory parameter**

### Rationale

- Pre-1.0: change completely rather than add a parallel method (AGENTS.md sections 1 and 3).
- #123 later adds `MessagePriority priority` to the same signature; the merge order in the Summary avoids a conflict.

**Class:** Settled
**Settled by:** AGENTS.md section 1 and section 3 (no legacy or compatibility layers; change completely).

</details>

---

## Implementation Phases

Five pull requests of at most 15 files and 1500 lines: PR-A phases 1 and 2 (core and fakes), PR-B phase 3 (ADO.NET), PR-C phase 4 (Dapper), PR-D phase 5 (EF Core and MongoDB), PR-E phases 6 to 8 (observability, tests, docs).

### Phase 1: Core Contract and Producer Path

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Outbox/IOutboxMessage.cs`: add `string? PartitionKey { get; set; }` with XML docs (null means no partition).
2. New `src/Encina.Messaging/Outbox/IPartitionedMessage.cs`: `string PartitionKey { get; }`.
3. New `src/Encina.Messaging/Outbox/OutboxPartitionKey.cs` (internal static): `Either<EncinaError, string?> Resolve(object notification)` returns null when the notification is not `IPartitionedMessage`, `Left` with `OutboxErrorCodes.InvalidPartitionKey` when the key is empty, whitespace or longer than 256 characters.
4. `OutboxOrchestrator.cs`: `IOutboxMessageFactory.Create(..., string? partitionKey)`, `OutboxErrorCodes.InvalidPartitionKey = "outbox.invalid_partition_key"`, and the call in `AddAsync`.
5. `OutboxWriter.cs` and `TransactionalOutboxPipelineBehavior.cs`: resolve and pass the key; a `Left` fails the add (no swallowing).
6. `PublicAPI.Unshipped.txt` for `Encina.Messaging` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina pre-1.0, plan docs/plans/outbox-partition-key-implementation-plan-1254.md (issue #1254, SPEC-002 P-51). IOutboxMessage has no partition key; IOutboxMessageFactory.Create(Guid, string, string, DateTime) is called from OutboxOrchestrator.AddAsync, OutboxWriter and TransactionalOutboxPipelineBehavior.
TASK: Implement Phase 1 Tasks 1 to 5. Do not change ordering or fetch behavior.
KEY RULES: Either<EncinaError, T> with no exceptions for invalid input (ADR-001, ADR-006); a Left from key validation fails the add and is never swallowed (AGENTS.md section 3); no [Obsolete], no compatibility overload; XML docs on every public member; the key value is never logged.
REFERENCE FILES: src/Encina.Messaging/Outbox/IOutboxMessage.cs, OutboxOrchestrator.cs, OutboxWriter.cs, TransactionalOutboxPipelineBehavior.cs.
```

</details>

### Phase 2: Testing Packages

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Testing.Fakes/Models/FakeOutboxMessage.cs` and `Stores/FakeOutboxStore.cs`: the property, stored and returned unchanged.
2. `src/Encina.Testing.Bogus/OutboxMessageFaker.cs`, `tests/Encina.TestInfrastructure/Builders/OutboxMessageBuilder.cs`, `src/Encina.Testing/Messaging/OutboxTestHelper.cs`: `WithPartitionKey`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Phase 1 added IOutboxMessage.PartitionKey.
TASK: Update the fakes, faker, builder and helper so every existing test compiles and tests can set a key.
KEY RULES: tests execute real code; no reflection-only tests; the fake store returns the key unchanged.
REFERENCE FILES: src/Encina.Testing.Fakes/Stores/FakeOutboxStore.cs, src/Encina.Testing.Bogus/OutboxMessageFaker.cs, tests/Encina.TestInfrastructure/Builders/OutboxMessageBuilder.cs.
```

</details>

### Phase 3: ADO.NET Providers (SqlServer, PostgreSQL, MySQL)

<details>
<summary><strong>Tasks</strong></summary>

For each ADO.NET package:

1. `Outbox/OutboxMessage.cs`: `PartitionKey` property.
2. `Outbox/OutboxMessageFactory.cs`: new `Create` signature.
3. `Outbox/OutboxStoreADO.cs`: the key in the insert statement and the data-reader mapping (null handled through `DBNull`).
4. `Scripts/001_CreateOutboxMessagesTable.sql` and `Scripts/000_CreateAllTables.sql`: `PartitionKey NVARCHAR(256) NULL` (SQL Server), `VARCHAR(256) NULL` (PostgreSQL lower-case `partitionkey`, MySQL), and the index of choice 3.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: ADO stores write SQL by hand and map readers by name or ordinal; scripts create the table (SQL Server bracketed identifiers, PostgreSQL lower-case, MySQL backticks).
TASK: Apply Phase 3 Tasks to the three ADO.NET packages, one package per commit.
KEY RULES: async methods with CancellationToken only; parameters, never concatenated values; nullable column; keep the scripts idempotent as they are; no migration helper (pre-1.0).
REFERENCE FILES: src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs, src/Encina.ADO.PostgreSQL/Scripts/001_CreateOutboxMessagesTable.sql, src/Encina.ADO.MySQL/Scripts/000_CreateAllTables.sql.
```

</details>

### Phase 4: Dapper Providers (SqlServer, PostgreSQL, MySQL)

<details>
<summary><strong>Tasks</strong></summary>

For each Dapper package: the same five files as Phase 3 with `OutboxStoreDapper.cs`. The insert lists the new column and parameter; the select already reads `*`, so the property name must match the column.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: Dapper stores use CommandDefinition and SELECT * mapped to the provider OutboxMessage class (src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs).
TASK: Apply Phase 4 to the three Dapper packages, mirroring Phase 3.
KEY RULES: same as Phase 3; PostgreSQL column mapping stays consistent with the existing lower-case convention.
REFERENCE FILES: src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs, src/Encina.Dapper.PostgreSQL/Outbox/OutboxStoreDapper.cs, src/Encina.Dapper.MySQL/Outbox/OutboxStoreDapper.cs.
```

</details>

### Phase 5: EF Core (3 providers) and MongoDB

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.EntityFrameworkCore/Outbox/OutboxMessage.cs`, `OutboxMessageFactory.cs`, `OutboxMessageConfiguration.cs` (`HasMaxLength(256)`, `IsRequired(false)`, `HasIndex(x => new { x.PartitionKey, x.CreatedAtUtc }).HasDatabaseName("IX_OutboxMessages_PartitionKey")`); `OutboxStoreEF.cs` needs no query change (the entity carries the property).
2. `src/Encina.MongoDB/Outbox/OutboxMessage.cs` (`[BsonElement("partitionKey")]`, `[BsonIgnoreIfNull]`), `OutboxMessageFactory.cs`, `MongoDbIndexCreator.cs` (compound index on `partitionKey` and `createdAtUtc`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: One EF Core store serves SqlServer, PostgreSQL and MySQL; MongoDB indexes are created in MongoDbIndexCreator.CreateOutboxIndexesAsync.
TASK: Implement Phase 5 Tasks 1 and 2.
KEY RULES: verify the EF model builds on all three relational providers; a missing MongoDB field must read back as null; async with CancellationToken.
REFERENCE FILES: src/Encina.EntityFrameworkCore/Outbox/OutboxMessageConfiguration.cs, src/Encina.MongoDB/Outbox/OutboxMessage.cs, src/Encina.MongoDB/MongoDbIndexCreator.cs.
```

</details>

### Phase 6: Cross-Cutting Integration and Observability

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs`: register `MessagingOutboxPartitionKey = (6748, 6749)` from the family block 6700-6749; add it to the `Encina.Messaging` entry in `EncinaEventIdAllocationTests`.
2. `src/Encina.Messaging/Diagnostics/OutboxPartitionKeyLogMessages.cs` (`[LoggerMessage]`): 6748 key rejected (error code only), 6749 message stored with a partition key (debug, no key value).
3. `OutboxProcessorMetrics.cs`: counter `encina.outbox.partitioned` on the shared `"Encina"` meter; `OutboxActivitySource.StartAdd`: boolean tag `encina.outbox.partitioned` (never the key).
4. Add `Encina.Messaging` unit tests for the registry entry through the existing architecture test.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: ADR-021: register the range first (6748-6749 assigned to this plan by the batch registry). Existing outbox metrics live in src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs.
TASK: Implement Phase 6 Tasks 1 to 3, packing the two EventIds sequentially.
KEY RULES: [LoggerMessage] source generator; the partition key may identify a data subject, so it is never a log argument, tag or metric dimension; run the EventId architecture tests.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs.
```

</details>

### Phase 7: Testing

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: `OutboxPartitionKey.Resolve` (not partitioned, valid, empty, whitespace, 256 and 257 characters), the three write paths pass the key, the factory of every provider sets it.
2. Guard: `IPartitionedMessage` consumers and the new factory parameter list.
3. Contract: store round-trip contract on the fake and the 10 real stores (key and null).
4. Integration (AC-033): on each of the 10 providers store a message with a key and read it back unchanged, and one without a key read back as null; shared `[Collection]` fixtures, `ClearAllDataAsync` in `InitializeAsync`.
5. Property: not needed (pure persistence round-trip covered by integration and contract); a `.md` justification is allowed. Load and benchmark: `.md` justifications (not a concurrent hot path; the index cost is measured in #469).
6. Coverage: per-file targets in `.github/coverage-manifest/Encina.Messaging.json` and each provider manifest, written by `mechanical-fixer`; CRAP at most 10 on changed methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT: AGENTS.md section 9: per-flag coverage, real code, Shouldly through Encina.Testing.Shouldly, shared Collection fixtures, never a .md for database integration tests.
TASK: Write the tests of Phase 7 Tasks 1 to 4 and the justification files of Task 5; report per-flag coverage against the manifest.
KEY RULES: AAA, deterministic, no Thread.Sleep; all 10 providers; SQLite excluded.
REFERENCE FILES: tests/Encina.IntegrationTests/ADO/SqlServer/Outbox/OutboxStoreADOTests.cs, tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Outbox/OutboxStoreEFIntegrationTests.cs, tests/Encina.IntegrationTests/Infrastructure/MongoDB/Stores/OutboxStoreMongoDBIntegrationTests.cs.
```

</details>

### Phase 8: Documentation and Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs; `PublicAPI.Unshipped.txt` in every touched package; `changelog.d/1254-outbox-partition-key.added.md` (all by `mechanical-fixer`).
2. `docs-writer`: configuration note in the outbox guide and package README; `docs/INVENTORY.md` if it lists the new file.
3. ADR: none of its own; the ordering mechanism ADR belongs to #469.
4. `dotnet build --configuration Release` with zero warnings; `dotnet test`; every flag at its target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT: documentation, changelog fragments and PublicAPI lines have path owners (docs-writer, mechanical-fixer).
TASK: Finish Phase 8 Tasks 1 to 4.
KEY RULES: English only; coverage figures only through covref markers; write "the reference application", never a private client name.
REFERENCE FILES: changelog.d/README.md, docs/plans/dsr-implementation-plan-404.md, docs/specifications/SPEC-002 section 5.11.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard or source | Relevance |
| --- | --- |
| SPEC-002 REQ-033, REQ-042, DEC-005 (a), P-51, AC-033 | The column now, nullable, producer-set, persisted unchanged on 10 providers |
| SPEC-002 REQ-061, AC-043 | Partitions are per tenant; carried by #737 and #469 |
| Enterprise Integration Patterns | Message key for ordered delivery (Resequencer, #469) |
| AGENTS.md sections 3, 5, 6, 7 | Opt-in, 10 providers, 12-function check, EventIds |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| --- | --- | --- |
| `IOutboxMessage`, `IOutboxMessageFactory`, `OutboxErrorCodes` | `src/Encina.Messaging/Outbox/` | Property, factory parameter, error code |
| `OutboxWriter`, `OutboxOrchestrator`, `TransactionalOutboxPipelineBehavior` | `src/Encina.Messaging/Outbox/` | Three write paths that resolve the key |
| Provider `OutboxMessage`, factory, store | `src/Encina.ADO.*`, `Encina.Dapper.*`, `Encina.EntityFrameworkCore`, `Encina.MongoDB` | Persist and return the key |
| SQL scripts | `src/Encina.ADO.*/Scripts`, `src/Encina.Dapper.*/Scripts` | Column and index |
| `OutboxProcessorMetrics`, `OutboxActivitySource` | `src/Encina.Messaging/Diagnostics/` | Counter and activity tag |

### Event ID Allocation

| Package | Range | Notes |
| --- | --- | --- |
| `Encina.Messaging` (`EventIdRanges.MessagingOutboxPartitionKey`, new) | 6748-6749 | From the family block 6700-6749 (plan-batch registry). Siblings: #123 6700-6714, #301 6715-6724, #251 6725-6734, #469 6735-6747 |

### Estimated File Count

| Category | Files | Notes |
| --- | --- | --- |
| Core new and modified | 9 | Interface, resolver, factory contract, 3 write paths, error code, diagnostics, `EventIdRanges.cs` |
| ADO.NET (3 providers) | 15 | Message, factory, store, 2 scripts each |
| Dapper (3 providers) | 15 | Same |
| EF Core and MongoDB | 7 | EF 4 files for 3 providers; Mongo 4 files (message, factory, store unchanged if mapped, index creator) |
| Testing packages | 5 | Fakes, faker, builder, helper |
| Tests | 18 | Unit, guard, contract, 10 integration, 3 justification files |
| Docs, changelog, PublicAPI | 12 | Per package |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined Prompt — All Phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14. Issue #1254 (SPEC-002 P-51), plan docs/plans/outbox-partition-key-implementation-plan-1254.md. Read AGENTS.md first. This plan merges first in the family (#1254, #123, #301, #469).

IMPLEMENTATION OVERVIEW: (1) IOutboxMessage.PartitionKey, IPartitionedMessage, validation, factory parameter, three write paths. (2) Fakes and builders. (3) ADO.NET x3. (4) Dapper x3. (5) EF Core and MongoDB. (6) EventIds 6748-6749, counter, activity tag. (7) Tests: AC-033 round trip on 10 providers. (8) Docs. Five pull requests of at most 15 files and 1500 lines.

KEY PATTERNS: nullable string up to 256 characters; null means no partition; Left for empty or too long; index on (PartitionKey, CreatedAtUtc) on every provider; the key is never logged or used as a metric dimension; no ordering change; async database calls with CancellationToken; shared [Collection] fixtures; no migration helpers.

REFERENCE FILES: src/Encina.Messaging/Outbox/*.cs, src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs, src/Encina.EntityFrameworkCore/Outbox/OutboxMessageConfiguration.cs, src/Encina.MongoDB/MongoDbIndexCreator.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/specifications/SPEC-002 section 5.11.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A persisted column; no read path that benefits from caching |
| 2 | OpenTelemetry | ✅ | Counter `encina.outbox.partitioned` and activity tag on the existing add activity; never the key value (Phase 6) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` 6748-6749 in `EventIdRanges.MessagingOutboxPartitionKey` (Phase 6) |
| 4 | Health Checks | ❌ | No new checkable dependency; stalled partitions belong to #469 |
| 5 | Validation | ✅ | Key validated on the write path; `Left` with `outbox.invalid_partition_key` (Phase 1) |
| 6 | Resilience | ❌ | No call to an external system |
| 7 | Distributed Locks | ❌ | No shared state is updated by concurrent hosts; claiming per key is #469 and #1251 |
| 8 | Transactions | ✅ | The key is written in the same `AddAsync` and transaction as the row (Phase 1) |
| 9 | Idempotency | ❌ | Not a message entry point; duplicate handling is #735, #148 and #1220 |
| 10 | Multi-Tenancy | ⏭️ | The outbox has no `TenantId`; #737 adds it, #469 composes tenant and key; see choice 4 (Dilemma) |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping here; `ModuleId` on messaging entities is #747 |
| 12 | Audit Trail | ❌ | No compliance-relevant operation beyond those already audited |

---

## Prerequisites & Dependencies

- None blocking: this plan merges first. #737 (`TenantId` on `OutboxMessage`) is advisable before #469 and decides the tenant acceptance bullets (choice 4).
- #123, #301 and #469 are rebased on this change; #469 reads the column and adds the ordering.

## Next Steps

1. Maintainer answers choices 1 and 4 when work starts; the answers go to `## Maintainer Decisions`.
2. Worker briefs per pull request (PR-A to PR-E) with the 15-file limit.
3. After merge: unblock #469 and rebase #123.
