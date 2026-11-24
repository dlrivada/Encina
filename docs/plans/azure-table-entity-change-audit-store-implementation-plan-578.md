# Implementation Plan: `EntityChangeAuditStoreAzureTable` — Azure Table Storage backend for the entity-change audit store

> **Issue**: [#578](https://github.com/dlrivada/Encina/issues/578)
> **Type**: Feature
> **Complexity**: Medium (6 phases, 1 provider, a new package, ~10 source files + tests)
> **Estimated Scope**: ~800-1,100 lines of production code + ~1,400-1,800 lines of tests

---

## Summary

Implement the entity-change audit store (today `IAuditLogStore`, becoming `IEntityChangeAuditStore` under ADR-036) on Azure Table Storage as `EntityChangeAuditStoreAzureTable`: low-cost, append-optimised history of "what did this entity look like before and after each change", queried by entity. The store is built on the **ADR-036 contract** (`Either` results, `TimeProvider`, `TenantId`, `ModuleId`, `PurgeEntriesAsync`, `ChangedAtUtc` as `DateTimeOffset`, the writer's redaction hook), not on the samples of the issue, which build an OData filter by string concatenation, sort the whole history in memory, use an entity type alone as partition key and rely on "built-in lifecycle management policies", which exist for Blob Storage only, not for tables.

What the code looks like today (origin/main `ff9a579c1`, re-checked for this revision; the plan branch was cut from `039cd3524`):

- `IAuditLogStore` lives in `src/Encina.DomainModeling/Auditing/IAuditLogStore.cs` (`Task LogAsync(AuditLogEntry, CancellationToken)`, `Task<IEnumerable<AuditLogEntry>> GetHistoryAsync(string entityType, string entityId, CancellationToken)`); `AuditLogEntry` (`Id`, `EntityType`, `EntityId`, `Action`, `UserId`, `TimestampUtc` as `DateTime`, `OldValues`, `NewValues`, `CorrelationId`) and `InMemoryAuditLogStore` sit next to it. ADR-036 renames and reshapes all of it; that work is **#1778, open**.
- `Directory.Packages.props` already pins `Azure.Data.Tables` 12.13.0 (MIT), `Azure.Identity` 1.21.0 and `Testcontainers.Azurite` 4.15.0. `tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs` (image `mcr.microsoft.com/azure-storage/azurite:3.37.0`, exposes `ConnectionString` and `TableEndpoint`, collection `Cloud-Azurite`, `tests/Encina.IntegrationTests/Infrastructure/Azurite/AzuriteCollection.cs`) exists. No package holds a Table Storage store yet; the #588 plan (dead letters, open) and the #582 plan (Cosmos DB audit, open) each propose an Azure package.
- No behavioral contract suite exists for the entity-change store; it is expected from #1778 (see Prerequisites).
- SPEC-000 section 6.1 names #578 among the cloud-backed stores that are in 1.0 because a free local emulator (Azurite) exists.

**Provider category**: cloud audit store (Azure side of the AWS/Azure pair with #579), outside the 10-provider database matrix. **Affected packages**: a new Azure Table package (name per Design Choice 1), `Encina.Security.Audit` (the contract, from #1778), `Encina` (EventId registry), test projects.

---

## Design Choices

<details>
<summary><strong>1. Package name and shape (Dilemma D1)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.AzureTableStorage`, store in an `EntityChangeAudit/` folder** | Service-named like `Encina.AzureServiceBus` and `Encina.Kafka`; later Table features (the dead letter store of #588) can share the client registration and health check | A package named after the service, not after audit |
| **B) `Encina.Security.Audit.AzureTable`** | Every audit backend discoverable under one prefix; pay-for-what-you-use per feature | A second Table client registration when #588 also ships |
| **C) `Encina.Azure.Storage`** (the issue) | One package for every Azure Storage service | Introduces a dotted `Azure.` prefix nobody else uses, and mixes Blob and Table SDKs |

### Chosen Option: **A — `Encina.AzureTableStorage`** (recommended, pending the maintainer)

### Rationale

- The #582 plan recommends the service-named `Encina.AzureCosmosDB` and the #588 plan recommends one package per feature; the maintainer sets one rule for #578, #582 and #588 together. A wins when several features share one storage account; B wins when every feature must be versioned and installed alone.

</details>

<details>
<summary><strong>2. Partition and row key layout (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) PartitionKey = encoded `(tenant, entityType, entityId)`; RowKey = `{inverted ticks:D19}_{entryId}`; plus retention index rows in a per-tenant, per-day partition `rix|{tenant}|{yyyyMMdd}`** | The history of one entity is one partition query in time order (newest first) with continuation tokens; purge reads only the index partitions older than the cutoff | Two writes per entry (index row first, then the entry row), not atomic across partitions |
| **B) PartitionKey = `{tenant}.{day}` (the issue's "by date", with the tenant segment first, as every layout requires)** | Purge by partition | Every history read scans many partitions |
| **C) Option A without index rows; purge scans the table with a filter on the tenant segment and `ChangedAtUtc`** | One write per entry | A full-table scan for every purge; cost grows with the table |

### Chosen Option: **A — entity partition, inverted-ticks row key, retention index rows** (recommended, pending the maintainer)

### Rationale

- The main query is entity history, which A keeps in one partition. B wins when purge cost dominates and history reads are rare; C wins for small tables where a scan costs little.
- **How each option finds the rows of one tenant older than its cutoff** (the per-tenant purge of Design Choice 8): A reads the tenant's index partitions `rix|{tenant}|{day}` for the days before the cutoff, then deletes the entry rows they point to in 100-operation batches per entity partition, and the index rows after their entries; B deletes that tenant's `{tenant}.{day}` partitions before the cutoff, never another tenant's; C queries with a filter on the tenant segment and `ChangedAtUtc`, scanning the table.
- **Index rows under option A.** Entity group transactions are limited to one partition, so the index row is written first (write-ahead) with `UpsertEntityAsync` (it holds only keys, so a repeat is harmless), then the entry row; a crash between the two writes leaves an index row without an entry, which the purge treats as already deleted.
- **Key encoding (rule).** Table keys forbid `/`, `\`, `#`, `?` and control characters and are limited to 1 KiB; each segment is base64url-encoded UTF-8 and the segments are joined with `.` (absent from the base64url alphabet), which is injective; a key over 1 KiB is a `Left` (`Validation`).
- **Tenant segment (rule).** With tenancy on the entry's `TenantId` is the first segment; the reserved segment `default` marks tenant-less entries, written only when tenancy is off; a real tenant `default` is rejected on write (`InvalidRequest`, ADR-062 R3); with tenancy on and no tenant resolved, write and history read deny (`InvalidRequest`; `AGENTS.md` section 3) and never fall back to `default`. The purge is not bound to the ambient tenant and has no cross-tenant opt-in: it receives the tenant explicitly from the retention service (Design Choice 8, #4182).

</details>

<details>
<summary><strong>3. Values larger than one property (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Split `OldValues` and `NewValues` across numbered string properties (`OldValues_0..n`, each at most 32 K UTF-16 characters, the 64 KiB property limit), up to the 1 MiB entity limit; above it a `Left` (`Validation`)** | Entities with large values can still be saved and audited; one row per entry | Reassembly code and a property-count limit (252) to test |
| **B) Reject any value over one property with a `Left`** | Simplest | With the fail-closed writer (ADR-036), saving any entity with more than 32 K characters of JSON fails |
| **C) Offload large values to Blob Storage** | No size limit | A second service, a second credential, a non-atomic two-service write |

### Chosen Option: **A — split across properties up to the entity limit** (recommended, pending the maintainer)

### Rationale

- B turns a storage limit into an application outage, because a failed entity-change write fails the unit of work. A keeps the store self-contained. C wins when entities routinely exceed 1 MiB of audited JSON.

</details>

<details>
<summary><strong>4. Client registration and credentials (Dilemma D4)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two paths: an application-registered `TableServiceClient`, or options (`ConnectionString`, or `Endpoint` with a `TokenCredential` from `Azure.Identity`) that build one** | Managed identity works; Azurite works with a connection string; an application that already configured Azure clients reuses them | Two paths to test |
| **B) Connection string only** | One path | No managed identity: shared keys in configuration |
| **C) Application-registered client only** | No secrets in Encina options | Every user writes the client setup, including for Azurite |

### Chosen Option: **A — registered client or options** (recommended, pending the maintainer)

### Rationale

- The connection string is marked `[JsonIgnore]` and `ToString()` prints none of the options' secrets (`AGENTS.md` section 3). B wins for the smallest surface; C wins when the maintainer wants Encina never to hold storage credentials.

</details>

<details>
<summary><strong>5. Tamper evidence (Dilemma D5)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Append-only use (`AddEntityAsync` only, never `UpsertEntity` or `UpdateEntity`), a SHA-256 `PayloadHash` property on every entry, and documented least-privilege access (writer SAS or role with add and read; the retention identity also delete); a hash chain is left to #1240** | No new mechanism; the hash makes a silent edit detectable on the conflict path | An account key holder can still rewrite a row and its hash |
| **B) A per-store hash chain now** | Rewrites detectable by a chain check | Serialises writes; duplicates #1240 for one provider |

### Chosen Option: **A — append-only, payload hash, least privilege; chain through #1240** (recommended, pending the maintainer)

### Rationale

- Table Storage has no immutable (WORM) mode; that exists for Blob Storage only, so immutability is a property of the credentials. #1240 (open) owns the cross-provider chain. B wins if chain verification is required before #1240 lands.

</details>

<details>
<summary><strong>6. Contract, naming and failure semantics</strong></summary>

**Class:** Settled
**Settled by:** ADR-036 (decisions 1 and 7, naming tables), ADR-001, ADR-006, ADR-062 (R2-R6), AGENTS.md section 3 (TimeProvider; fail closed; errors never swallowed; secrets in options), AGENTS.md section 4 (`{Pattern}Store{Provider}`)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Build on the ADR-036 contract: class `EntityChangeAuditStoreAzureTable`, `Either` results, `TenantId`, `ModuleId`, `PurgeEntriesAsync`; a failed write is a `Left` that fails the unit of work; cancellation propagates as `OperationCanceledException`** | Provider coherence; fail-closed auditing | Depends on #1778 |
| **B) Keep the old `IAuditLogStore` shape** | Buildable today | Removed by ADR-036 with no compatibility layer (decision 7) |

### Chosen Option: **A — the ADR-036 contract**

### Rationale

- ADR-036 says follow-up issues apply the names and do not revisit them. Every `EncinaErrors.Create` passes a named `category:` and a constant message; no `RequestFailedException.Message` reaches a `Left`, a log or a span (only `Status` and `ErrorCode`).

</details>

<details>
<summary><strong>7. Idempotent write</strong></summary>

**Class:** Settled
**Settled by:** AGENTS.md section 3 (errors are never swallowed), AGENTS.md section 6 function 9, ADR-062 R2

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEntityAsync`; on 409 (`EntityAlreadyExists`) read the stored row and compare `PayloadHash`: identical is success, different is `Left` (`AlreadyExists`)** | Safe retries of the same entry; a different payload under the same keys, or an edited row, is never silent | One point read on the rare conflict path |
| **B) Treat every 409 as success** | Simplest | Loses an entry silently on a key collision |
| **C) `UpsertEntityAsync`** | No conflict path | Overwrites an existing audit row: never acceptable |

### Chosen Option: **A — add, compare on conflict**

### Rationale

- B and C lose or overwrite audit data without a failure. The rule applies to the entry row under every key layout of Design Choice 2; helper rows that hold only keys belong to the layout chosen there.
- **Scope of the guarantee.** The keys of every layout contain `ChangedAtUtc` as well as the entry id, so the conflict check covers a retry of the same entry (the writer builds an entry once, so a retry carries the same `ChangedAtUtc`) and a different payload under the same keys. The same entry id written again with a different `ChangedAtUtc` lands as a second row and is not detected by the store: entry-id uniqueness across timestamps is the writer's guarantee (the EF Core writer builds `Id: Guid.NewGuid().ToString()` for each entry, `src/Encina.EntityFrameworkCore/Auditing/AuditInterceptor.cs:377,423` on origin/main `ff9a579c1`), and the README says so.

</details>

<details>
<summary><strong>8. Retention mechanism</strong></summary>

**Class:** Settled
**Settled by:** ADR-036 decision 1 (`PurgeEntriesAsync`, 2555-day default), SPEC-002 REQ-007, #4182 (retention iterates the tenants through `ITenantStore` with each tenant's policy, never the ambient tenant or a cross-tenant opt-in), Azure Table Storage service limits (no per-entity TTL and no lifecycle management for tables)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Per-tenant `PurgeEntriesAsync`: the ADR-036 `EntityChangeAuditRetentionService` runs in a tenant-less scope, enumerates `ITenantStore.GetAllTenantsAsync` and calls the purge once per tenant with that tenant's cutoff (once with the `default` segment when tenancy is off); the store deletes only that tenant's rows older than the cutoff, located as Design Choice 2 describes** | Per-tenant retention; counted, logged deletes; no dependence on an ambient tenant | Delete transactions cost money |
| **B) Service-side expiry** | — | Does not exist for Table Storage |

### Chosen Option: **A — per-tenant purge through the contract**

### Rationale

- The service offers no expiry for tables, so the contract's purge is the only mechanism, whatever key layout Design Choice 2 picks. The exact purge signature and the source of each tenant's policy are decided by #4182 and delivered by #1778; the store never reads the ambient tenant or an opt-in for the purge, and a call without a tenant while tenancy is on is a `Left` (`InvalidRequest`). A failed batch stops that tenant's purge with `Left` (`Internal`) and reports no partial success.
- **Tenant segments that `ITenantStore` no longer returns (rule).** Enumerating registered tenants alone would never purge two kinds of rows, which GDPR Art. 5(1)(e) and SPEC-002 REQ-007 forbid keeping past their retention: rows of an offboarded tenant (kept under path C of #2847 after its tenant record is removed) and `default`-segment rows written before tenancy was switched on. The store therefore also lists the distinct tenant segments it holds (`ListTenantSegmentsAsync`, or the name #1778 gives it): on the first write of a segment in a process it upserts a key-only row whose RowKey is `seg|{base64url segment}` (the encoded segment, never the raw tenant id, which may contain forbidden key characters) in a `tenants` partition, and the list is a query of that partition. Write order and failure rules: the segment row is upserted before the first entry row of that segment (write-ahead, like the index row); a failed upsert fails the write with a `Left`; the in-process "already registered" cache is set only after the upsert succeeds, so the next write retries it; segment rows are never deleted. A failed or incomplete listing query is a `Left` (`Internal`), never a shorter list reported as complete. The retention service purges every listed segment that `ITenantStore` does not return, including `default` once tenancy is on, under the residual policy #4182 defines (for example the policy recorded at offboarding, or the default retention of `EntityChangeAuditOptions`). This case is an acceptance item of #4182. The listing runs whether tenancy is on or off, so segments that real tenants wrote before tenancy was switched off are purged too.

</details>

<details>
<summary><strong>9. Erasure requests and immutability of entries</strong></summary>

**Class:** Settled
**Settled by:** maintainer decision of 2026-10-26 on #2847 (path C is the default for audit data: GDPR Art. 17(3)(b) and 17(3)(e)), ADR-036 decision 1 (retention, redaction hook in the writer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No per-subject delete; rows are never updated; removal only through `PurgeEntriesAsync`; minimisation through the writer's redaction hook** | Matches the audit erasure decision and the contract | An exceptional per-person deletion needs a separate capability (Marten counterpart: #2903) |
| **B) A per-subject delete method** | Answers erasure directly | Contradicts path C as the default |

### Chosen Option: **A — retention only, no per-subject delete**

### Rationale

- Path C records the request and keeps audit data until its retention expires; B is the exceptional path, planned separately.

</details>

---

## Implementation Phases

### Phase 1: Package, options, entity mapping and keys

<details>
<summary>Tasks</summary>

1. `src/Encina.AzureTableStorage/Encina.AzureTableStorage.csproj` (name per Design Choice 1; net10.0, `Azure.Data.Tables`, `Azure.Identity`, `Encina.Security.Audit`, PublicAPI analyzers, XML docs, `InternalsVisibleTo` for `Encina.UnitTests`, `Encina.PropertyTests`, `Encina.IntegrationTests`, `Encina.ContractTests`, not `Encina.GuardTests`, so the guard target of 0 for internal types stays valid); row in `Encina.slnx`; `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`.
2. `EncinaAzureTableStorageOptions` (public, sealed): `ConnectionString` (`[JsonIgnore]`), `Endpoint`, `ToString()` that prints no secret (Design Choice 4).
3. `EntityChangeAudit/EntityChangeAuditAzureTableOptions` (public, sealed): `TableName` (default `EntityChangeAuditEntries`, validated against table-name rules), `CreateTableIfNotExists` (default `false`). No cross-tenant option: the purge receives its tenant explicitly (Design Choice 8).
4. `EntityChangeAudit/EntityChangeAuditTableKeys` (internal): segment encoding, partition key, inverted-ticks row key, index partition and row keys (Design Choice 2).
5. `EntityChangeAudit/EntityChangeAuditTableMapper` (internal): ADR-036 entry to and from `TableEntity`, value splitting and reassembly (Design Choice 3), `PayloadHash`, `ChangedAtUtc` stored as `DateTimeOffset` in UTC.
6. Unit tests (keys: forbidden characters, colliding concatenations, 1 KiB limit; mapper: round trip, split values, entity limit) and guard tests of the options; manifest `.github/coverage-manifest/Encina.AzureTableStorage.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Encina is a .NET 10 / C# 14 library (Railway Oriented Programming). The ADR-036 contract (IEntityChangeAuditStore, EntityChangeAuditEntry) is delivered by #1778; build on its real shapes on main.
- Azure.Data.Tables 12.13.0 and Azure.Identity are pinned in Directory.Packages.props.

TASK:
Create the package, the options, the key builder and the mapper of the Phase 1 Tasks with their tests and manifest.

KEY RULES:
- Keys: base64url segments joined by '.', injective, 1 KiB limit as a Left (Validation).
- Values split across numbered properties up to the 1 MiB entity limit.
- Secrets [JsonIgnore]; ToString prints none.
- No [Obsolete], no compatibility layer; XML docs; PublicAPI files complete.

REFERENCE FILES:
- docs/architecture/adr/036-three-audit-stores.md
- src/Encina.MongoDB/Auditing/AuditLogDocument.cs
- src/Encina.AzureServiceBus/Encina.AzureServiceBus.csproj
```

</details>

---

### Phase 2: `EntityChangeAuditStoreAzureTable` and its registration

<details>
<summary>Tasks</summary>

1. `EntityChangeAudit/EntityChangeAuditStoreAzureTable.cs` (public, sealed) `: IEntityChangeAuditStore`; constructor `(TableServiceClient serviceClient, ITenantContext? tenantContext, IOptions<EntityChangeAuditAzureTableOptions> options, TimeProvider timeProvider, ILogger<EntityChangeAuditStoreAzureTable> logger)` (the tenant abstraction is the one #1778 uses).
2. Write: tenant rules first; then, if this process has not yet registered the segment, upsert the segment row of Design Choice 8 (a failure is a `Left` and stops the write; the cache is set only after success); then the rows of the layout chosen in Design Choice 2 (under option A: index row with `UpsertEntityAsync`, then entry row with `AddEntityAsync`); 409 compare (Design Choice 7); 413 or over-limit entity `Left` (`Validation`); 429, 500 and 503 `Left` (`Unavailable`); other `RequestFailedException` `Left` (`Internal`) carrying `Status` and `ErrorCode` only.
3. History: `QueryAsync<TableEntity>` with a filter built by `TableClient.CreateQueryFilter` (never string concatenation) on the partition key, newest first by row key, paged through the contract's `PagedResult<T>` with the service continuation token.
4. `PurgeEntriesAsync` with the tenant and cutoff it receives (signature from #4182 and #1778; Design Choices 2 and 8): deletes only that tenant's rows older than the cutoff; never reads the ambient tenant; with tenancy on and no tenant passed returns `Left` (`InvalidRequest`); returns the count. The store must be constructible in the tenant-less scope of the retention service.
5. `EntityChangeAudit/EntityChangeAuditAzureTableErrors.cs`.
6. `EntityChangeAudit/EntityChangeAuditTableInitializer.cs` (internal, `IHostedService`): creates the table only when `CreateTableIfNotExists`.
7. `ServiceCollectionExtensions`: `AddEncinaAzureTableStorage(this IServiceCollection, Action<EncinaAzureTableStorageOptions>)` (`TryAddSingleton` of a `TableServiceClient` built from the options unless one is registered) and `AddEncinaEntityChangeAuditAzureTable(this IServiceCollection, Action<EntityChangeAuditAzureTableOptions>? configure = null)`: options with `ValidateOnStart`, `TryAddSingleton(TimeProvider.System)`, the store as `IEntityChangeAuditStore` winning over the in-memory default in any order without overriding an application registration (mechanism from #1778), the initializer through `TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, EntityChangeAuditTableInitializer>())`.
8. Register the EventId range and the log class in this PR (the store and initializer log). DI test with `ValidateOnBuild` and `ValidateScopes`, both orders. Every method at CRAP <= 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Phase 1 created the package, options, keys and mapper.

TASK:
Implement the store, its errors, the table initializer, both registration methods, the EventId range and the log class, with unit, guard and DI tests.

KEY RULES:
- Either everywhere; a storage failure is a Left; only Status and ErrorCode reach errors and logs; cancellation propagates.
- Filters through TableClient.CreateQueryFilter; never concatenated OData.
- Tenancy on and no tenant denies writes and reads; real tenant "default" rejected; the purge deletes only the tenant passed by the retention service (#4182), never the ambient one.
- Hosted services through TryAddEnumerable.

REFERENCE FILES:
- docs/architecture/adr/062-error-category-contract.md
- src/Encina.Audit.Marten/ServiceCollectionExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 3: Health check

<details>
<summary>Tasks</summary>

1. `EntityChangeAudit/EntityChangeAuditAzureTableHealthCheck.cs : IEncinaHealthCheck`: a read-only probe (`QueryAsync` with `maxPerPage: 1` on the table, no access-policy call, so the writer's least-privilege credential suffices) mapping 404 to unhealthy "table missing"; const `DefaultName`, static `Tags`; registered by `AddEncinaEntityChangeAuditAzureTable`.
2. Unit and guard tests; manifest edit; `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Phase 2 shipped the store and its registration.

TASK:
Add the health check and register it in AddEncinaEntityChangeAuditAzureTable.

KEY RULES:
- DefaultName const, Tags static array; status from the response status code only.

REFERENCE FILES:
- src/Encina.Kafka/Health/KafkaHealthCheck.cs
```

</details>

---

### Phase 4: Cross-cutting integration and observability

<details>
<summary>Tasks</summary>

1. EventId range `AzureTableEntityChangeAudit = (52400, 52449)` in `src/Encina/Diagnostics/EventIdRanges.cs`, registered in PR 2 with: its line in `src/Encina/PublicAPI.Unshipped.txt` (one line for the new field), the assembly entry in `AssemblyRanges` of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`, and in `docs/engineering/eventid-block-registry.md` a "Registered ranges" row `AzureTableEntityChangeAudit | 52400-52449` (Zone `zone`) and a "Blocks" row `52400-52449` with status `registered`.
2. `EntityChangeAudit/Diagnostics/AzureTableEntityChangeAuditLog.cs`, ids packed from 52400: 52400 table created, 52401 write failed (status and error code), 52402 duplicate write ignored, 52403 id conflict, 52404 entry too large, 52405 orphan index row skipped, 52406 tenant purge completed (count), 52407 tenant purge failed. 52408-52449 stay free.
3. The ADR-036 `InstrumentedEntityChangeAuditStore` decorates the store (DI test asserts it); ADR-054 D4 tenant attribute and tag only when multi-tenancy is on; logs never carry tenant, user or entity ids, values or messages.
4. **ErrorCategory (ADR-062)**: `entity_change_audit.azure_table.write_failed` `Internal` (R4); `.unavailable` `Unavailable`; `.entry_too_large` and `.key_too_long` `Validation` (R3); `.id_conflict` `AlreadyExists` (R2); `.tenant_required` and `.reserved_tenant` `InvalidRequest` (R3); `.purge_failed` `Internal`. Cancellation propagates.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- AGENTS.md section 7: new ranges only in the 10000+ zone, registered before use; EventIdRegistryAgreementTests read the registry page.

TASK:
Register AzureTableEntityChangeAudit 52400-52449 (in PR 2), write the log class, wire the categories and the decorator test.

KEY RULES:
- Ids packed from 52400; no gap; nothing outside the range.
- Never log EncinaError.Message, exception messages, values or ids.

REFERENCE FILES:
- docs/engineering/eventid-block-registry.md
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 5: Testing

> **Goal**: every applicable flag reaches its target in `.github/coverage-manifest/Encina.AzureTableStorage.json`, each target and its justification set in the pull request whose tests reach it.

<details>
<summary>Tasks</summary>

1. **Unit**: keys, mapper, every error mapping (409 same and different hash, 413, 429, 503, other), tenant rules, purge of the tenant passed in (never the ambient one; tenancy on and no tenant passed is `Left`), segment registry row written before the entry and once per segment, a failed segment upsert fails the write and the next write retries it, a failed listing is `Left`, the listing runs with tenancy off, purge batching and orphan index rows, initializer branches; `TableClient` and `TableServiceClient` substituted (their members are virtual and `Azure.Response.FromValue`, `TableModelFactory` and the public `RequestFailedException` constructors build results), `FakeTimeProvider`.
2. **Guard** (`tests/Encina.GuardTests/AzureTableStorage/`): store constructor and methods, both registrations, options.
3. **Contract** (PR 4): a class deriving from the shared entity-change contract base on `[Collection("Cloud-Azurite")]`.
4. **Integration** (PR 4, `[Collection("Cloud-Azurite")]`, `[Trait("Category", "Integration")]`): idempotent repeated write, id conflict, history order and continuation, large values split and reassembled, a key with forbidden characters, colliding concatenations, tenant isolation, per-tenant purge from a scope with no ambient tenant (tenant A's cutoff deletes only tenant A's old rows; tenant B with a longer retention keeps its own), a tenant removed from `ITenantStore` and the `default` segment written before tenancy was enabled are listed and purged under the residual policy, purge with orphan index rows, table creation, health check. The class deletes its own table in `InitializeAsync` and also calls `ClearAllDataAsync`. Azurite (`mcr.microsoft.com/azure-storage/azurite:3.37.0`, MIT) runs through `Testcontainers.Azurite` 4.15.0 (MIT), both already used on main.
5. **Property** (PR 4): key injectivity and mapper round trip for random strings.
6. **Load/Benchmark**: `.md` justifications (append-only, not a hot path).

**Manifest rule**: the manifest is new (no existing floor to respect). PR 1 creates it with package-level `targets` for unit and guard; PR 4 adds the package-level contract, property and integration targets. Each PR adds the per-file `targets` and `justifications` below for the flags its own tests reach, so `coverage-report.cs --check-justifications` passes on every PR.

**Per-file targets** (`-` = not applicable; "PR" adds the target, its tests and its justification):

| File | Unit | Guard | Contract | Property | Integration | PR | Justification |
|------|:----:|:-----:|:--------:|:--------:|:-----------:|:--:|---------------|
| `EncinaAzureTableStorageOptions.cs` | 90 | 50 | - | - | 60 | 1, 4 | Unit: defaults, validation and the secret-free `ToString()`. Guard: invalid values on each validated property run the validation half of the file. Integration: the Azurite tests build the options through the registration. |
| `EntityChangeAudit/EntityChangeAuditAzureTableOptions.cs` | 90 | 50 | - | - | 60 | 1, 4 | Unit: defaults and table-name validation. Guard: invalid table names run the validation branches. Integration: built through the registration in the Azurite tests. |
| `EntityChangeAudit/EntityChangeAuditTableKeys.cs` | 95 | 0 | - | 95 | 80 | 1, 4 | Unit: encoding, forbidden characters and the 1 KiB limit. Guard: 0, internal and `Encina.GuardTests` has no `InternalsVisibleTo`. Property: injectivity over random segments. Integration: every Azurite write uses the keys. |
| `EntityChangeAudit/EntityChangeAuditTableMapper.cs` | 95 | 0 | - | 90 | 85 | 1, 4 | Unit: round trip, value splitting and the entity limit. Guard: 0, internal with no `InternalsVisibleTo` for guard tests. Property: round trip of random entries. Integration: Azurite round-trips split values. |
| `EntityChangeAudit/EntityChangeAuditStoreAzureTable.cs` | 90 | 30 | 85 | - | 85 | 2, 4 | Unit: fault paths through substituted clients. Guard: constructor and method argument checks only, about a third of the lines of a store that does I/O. Contract: the shared suite drives every contract method. Integration: Azurite covers write, history, purge and segment listing. |
| `EntityChangeAudit/EntityChangeAuditAzureTableErrors.cs` | 100 | 0 | - | - | - | 2 | Unit: every factory method is called by the store tests. Guard: 0, internal with no validated parameters. |
| `EntityChangeAudit/EntityChangeAuditTableInitializer.cs` | 90 | 0 | - | - | 80 | 2, 4 | Unit: opt-in and existing-table branches. Guard: 0, internal with no `InternalsVisibleTo` for guard tests. Integration: Azurite proves creation. |
| `ServiceCollectionExtensions.cs` | 95 | 40 | - | - | - | 2 | Unit: the DI test builds the provider with ValidateOnBuild in both orders. Guard: null `services` and null `configure` run the argument checks before a short registration body. |
| `EntityChangeAudit/EntityChangeAuditAzureTableHealthCheck.cs` | 90 | 30 | - | - | 70 | 3, 4 | Unit: healthy, missing-table and failure mappings. Guard: the constructor checks only. Integration: Azurite proves the healthy path. |
| `EntityChangeAudit/Diagnostics/AzureTableEntityChangeAuditLog.cs` | 0 | 0 | - | - | - | 2 | Unit: 0, source-generated `[LoggerMessage]` partial methods with no hand-written executable code. Guard: 0, same reason. |

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- Tests execute real package code; Azurite is shared through the Cloud-Azurite collection.

TASK:
Write the tests of Phase 5 and the manifest targets with justifications in the pull requests that reach them.

KEY RULES:
- Shouldly via Encina.Testing.Shouldly; no FluentAssertions, per-class containers or Thread.Sleep.
- ClearAllDataAsync in InitializeAsync; FakeTimeProvider.
- Targets never go down; run coverage-report.cs --check-justifications.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs
- tests/Encina.IntegrationTests/Infrastructure/Azurite/AzuriteCollection.cs
- tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs
```

</details>

---

### Phase 6: Documentation and finalization

<details>
<summary>Tasks</summary>

1. XML docs; `src/Encina.AzureTableStorage/README.md` (key layout, value splitting, retention, credentials, Azurite, least-privilege access); a feature page under `docs/features/`; `docs/INVENTORY.md`; ROADMAP if affected (through `docs-writer`).
2. `changelog.d/578-azure-table-entity-change-audit-store.added.md` (through `mechanical-fixer`).
3. ADR only if a Design Choice changes a rule of ADR-036; ADR number assigned when the ADR pull request opens.
4. Build 0 warnings; per-flag coverage and CRAP reported.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- Documentation: README, feature page, inventory, changelog fragment.

TASK:
Complete the documentation tasks of Phase 6.

KEY RULES:
- Never edit CHANGELOG.md; use changelog.d/.
- No hand-typed coverage figures; English only; never name the reference application.

REFERENCE FILES:
- docs/features/read-auditing.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Topic | Requirement |
|--------|-------|-------------|
| ADR-036 | Three audit stores | Entity-change contract, 2555-day retention, fail closed, redaction hook |
| ADR-062 | Error categories | Every code categorised (R2-R6) |
| ADR-054 D4 | Telemetry | Tenant attribute and tag only when multi-tenancy is on |
| SPEC-000 section 6.1, DEC-009 | 1.0 scope | #578 named in 1.0; tested against Azurite |
| SPEC-002 REQ-007, REQ-061 | Retention, isolation | Per-category retention; tenant isolation |
| #4182 (open, `[DEBT]`) | Per-tenant retention | Retention iterates `ITenantStore` with each tenant's policy; never the ambient tenant or a cross-tenant opt-in |
| GDPR Art. 5(1)(e), 17(3)(b) | Storage limitation, erasure exception | Purge at retention; recorded erasure (decision on #2847) |
| Azure Table Storage limits (vendor) | Keys, properties, batches | 1 KiB keys, forbidden characters, 64 KiB properties, 1 MiB entities, 100 operations per batch, one partition per batch |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in the Azure Table entity-change audit store |
|-----------|----------|----------------------------------------------------|
| `IAuditLogStore` (becomes `IEntityChangeAuditStore`) | `src/Encina.DomainModeling/Auditing/IAuditLogStore.cs` | The contract implemented after #1778 |
| `AuditLogEntry` (becomes `EntityChangeAuditEntry`) | `src/Encina.DomainModeling/Auditing/AuditLogEntry.cs` | The entry mapped to the table entity |
| `InMemoryAuditLogStore` | `src/Encina.DomainModeling/Auditing/InMemoryAuditLogStore.cs` | The default the store must win over |
| `EntityChangeAuditRetentionService`, `InstrumentedEntityChangeAuditStore` | delivered by #1778 | Per-tenant purge scheduling (#4182) and tracing |
| `ITenantStore.GetAllTenantsAsync` | `src/Encina.Tenancy/Abstractions/ITenantStore.cs` | Tenant enumeration of the retention service (#4182) |
| `OperationAuditRetentionService` | `src/Encina.Security.Audit/OperationAuditRetentionService.cs` | Shape of the tenant-less retention scope |
| `AzuriteFixture`, `Cloud-Azurite` collection | `tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs`, `tests/Encina.IntegrationTests/Infrastructure/Azurite/AzuriteCollection.cs` | Shared emulator for integration and contract tests |
| `Azure.Data.Tables`, `Azure.Identity`, `Testcontainers.Azurite` | `Directory.Packages.props` | Already pinned; no new package versions |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Registration of the new range |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.AzureTableStorage` (entity-change audit) | 52400-52449 (`AzureTableEntityChangeAudit`) | Second of the five audit-stores-1 slots 52350-52599, the first free Security and compliance space after the #4007 reservations 50000-52349 (registry on origin/main `1dee4ba61`); ids 52400-52407 used, packed; registered by PR 2 with its registry rows |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production | ~11 | csproj, two options, keys, mapper, store, errors, initializer, registration, health check, log class |
| Tests | ~12 | unit x4, guard x2, DI, contract, integration, property, load/benchmark `.md` x2 |
| Metadata | ~9 | `Encina.slnx`, PublicAPI x2, manifest, `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, registry page, allocation test, test-project references |
| **Total** | **~38 with edits** | Every PR starts after #1778 is on main, builds and passes alone, at most 15 files and 1500 lines. **PR 1** (13 files): csproj, `Encina.slnx`, PublicAPI x2, two options, keys, mapper, `Encina.UnitTests` and `Encina.GuardTests` project references, unit tests x2, options guard tests, manifest (no service is introduced, so no registration). **PR 2** (15 files): store, errors, initializer, `ServiceCollectionExtensions`, log class, `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, registry page, `EncinaEventIdAllocationTests.cs`, package `PublicAPI.Unshipped.txt`, store unit tests, DI test, guard tests, manifest, initializer unit tests. **PR 3** (6 files): health check, registration edit, `PublicAPI.Unshipped.txt`, health check unit tests, health check guard tests, manifest. **PR 4** (<= 11 files): `Encina.IntegrationTests`, `Encina.ContractTests`, `Encina.PropertyTests` references, an Azurite collection definition in `Encina.ContractTests` (the `Cloud-Azurite` collection is defined only in `Encina.IntegrationTests` today), contract class, integration tests, property tests, two `.md` justifications, manifest. **PR 5** (4 files): README, feature page, `docs/INVENTORY.md`, changelog fragment |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution first.
- Railway Oriented Programming: Either<EncinaError, T>; errors never swallowed; TimeProvider; async with CancellationToken; cancellation propagates.
- Issue #578: EntityChangeAuditStoreAzureTable implements IEntityChangeAuditStore (ADR-036, delivered by #1778) in a new package (name per Design Choice 1, recommended Encina.AzureTableStorage).

IMPLEMENTATION OVERVIEW:
Phase 1: package, options, injective keys, mapper with value splitting.
Phase 2: store (rows per the key layout of Design Choice 2, compare on conflict, paged history, per-tenant purge with the tenant passed by the retention service), initializer, registrations, EventId range 52400-52449, log class.
Phase 3: health check.
Phase 4: error categories, instrumentation, telemetry tags.
Phase 5: unit, guard, contract, integration (Cloud-Azurite), property tests; manifest targets with justifications.
Phase 6: docs and changelog fragment.

KEY PATTERNS:
- Dilemmas D1-D5 are recommended, pending the maintainer: follow the answers recorded in the plan.
- Tenant: reserved segment default, deny writes and reads without tenant under tenancy; retention purges one tenant at a time with the tenant passed explicitly by the retention service iterating ITenantStore (#4182); no cross-tenant opt-in exists.
- Append-only; no per-subject delete; retention through PurgeEntriesAsync only (tables have no TTL).
- Never log values, ids or exception messages; categories on every code.

REFERENCE FILES:
- docs/architecture/adr/036-three-audit-stores.md
- docs/architecture/adr/062-error-category-contract.md
- tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs
- src/Encina.Audit.Marten/ServiceCollectionExtensions.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The issue's "cache frequent audit queries" is dropped: an audit read must be exact and current, so no issue is needed. |
| 2 | OpenTelemetry | ✅ | The ADR-036 `InstrumentedEntityChangeAuditStore` decorates the store; tenant tag per ADR-054 D4. |
| 3 | Structured logging | ✅ | Range 52400-52449 registered first; status and error codes only. |
| 4 | Health checks | ✅ | Phase 3: `EntityChangeAuditAzureTableHealthCheck`. |
| 5 | Validation | ✅ | Argument checks throw before I/O; long keys and oversize entities are `Validation` `Left` values. |
| 6 | Resilience | ✅ | The Azure SDK retry policy (exponential, on 408/429/5xx) applies to every call; its options are exposed through the client options; no extra Polly policy. |
| 7 | Distributed locks | ❌ | Append-only store; `AddEntity` arbitrates concurrent writes of one key. |
| 8 | Transactions | ❌ | One entry per row; entity group batches serve only the purge; no unit of work spans Table Storage. |
| 9 | Idempotency | ✅ | Add with compare on conflict (Design Choice 7). |
| 10 | Multi-tenancy | ✅ | Tenant as the first key segment; reserved `default`; deny without tenant (Design Choice 2); per-tenant retention purge with the tenant passed explicitly (Design Choice 8, #4182). |
| 11 | Module isolation | ✅ | `ModuleId` stored on every row (supersedes the issue's "N/A"). |
| 12 | Audit trail | ✅ | This store IS the entity-change audit trail. |

---

## Prerequisites & Dependencies

- **#1778 (open, `[DEBT]`, no `needs-decision`, no blocking dependency)** delivers the contract, the retention service, the decorator, the in-memory default and the shared behavioral contract base. If it merges without a reusable base, the first of #577-#582 to reach its integration pull request adds it (the same fallback as the #582 plan).
- #582 (Cosmos DB) and #588 (Azure dead letters) are siblings; Design Choice 1 is decided for the three together.
- #1240 (open) owns the hash chain (Design Choice 5); #2903 (open) is the exceptional subject-scoped erasure path for Marten.
- No new package versions: `Azure.Data.Tables` 12.13.0 (MIT), `Azure.Identity` 1.21.0 (MIT) and `Testcontainers.Azurite` 4.15.0 (MIT) are pinned on origin/main `ff9a579c1`.
- **#4182 (open, `[DEBT]`)** decides the per-tenant retention mechanism (tenant enumeration through `ITenantStore`, each tenant's policy, the tenant-explicit purge signature, and the residual policy for segments `ITenantStore` no longer returns: offboarded tenants and the legacy `default` segment) before #1778 implements `EntityChangeAuditRetentionService`; Design Choice 8 and Phase 2 task 4 follow it.
- The EventId slot 52400-52449 lies after the #4007 reservations (50000-52349, #4174 merged).

---

## Next Steps

1. The maintainer answers D1 to D5 when work on #578 starts (D1 together with #582 and #588); the answers go into `## Maintainer Decisions`.
2. Wait for #4182 and #1778; re-check each Design Choice against main; `adversarial-reviewer` verifies the Settled ones.
3. Brief one worker per proposed pull request (5 pull requests).
4. The final pull request references `Fixes #578`.
