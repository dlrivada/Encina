# Implementation Plan: `EntityChangeAuditStoreDynamoDB` — AWS DynamoDB backend for the entity-change audit store

> **Issue**: [#579](https://github.com/dlrivada/Encina/issues/579)
> **Type**: Feature
> **Complexity**: Medium (6 phases, 1 provider, a new package, ~11 source files + tests)
> **Estimated Scope**: ~850-1,150 lines of production code + ~1,400-1,900 lines of tests

---

## Summary

Implement the entity-change audit store (today `IAuditLogStore`, becoming `IEntityChangeAuditStore` under ADR-036) on Amazon DynamoDB as `EntityChangeAuditStoreDynamoDB`: serverless history of "what did this entity look like before and after each change", queried by entity with consistent reads. The store is built on the **ADR-036 contract** (`Either` results, `TimeProvider`, `TenantId`, `ModuleId`, `PurgeEntriesAsync`, `ChangedAtUtc` as `DateTimeOffset`, the writer's redaction hook), not on the samples of the issue, which use `DynamoDBContext.SaveAsync` (an upsert that silently overwrites), read the history through a GSI (eventually consistent, so a just-written entry can be missing), sort in memory and key the GSI by a concatenated `EntityType#EntityId` that is not injective.

What the code looks like today (origin/main `ff9a579c1`, re-checked for this revision; the plan branch was cut from `039cd3524`):

- `IAuditLogStore` lives in `src/Encina.DomainModeling/Auditing/IAuditLogStore.cs`; `AuditLogEntry` (`Id`, `EntityType`, `EntityId`, `Action`, `UserId`, `TimestampUtc` as `DateTime`, `OldValues`, `NewValues`, `CorrelationId`) and `InMemoryAuditLogStore` sit next to it. ADR-036 renames and reshapes all of it; that work is **#1778, open**.
- `Directory.Packages.props` pins `AWSSDK.SQS`, `AWSSDK.SimpleNotificationService`, `AWSSDK.SecretsManager`, `AWSSDK.KeyManagementService` (AWSSDK 4 line) and `Testcontainers.LocalStack` 4.15.0, but **not** `AWSSDK.DynamoDBv2` (latest stable on NuGet at the time of writing: 4.1.0.3, Apache-2.0). The #589 (DynamoDB dead letters) and #210 (DynamoDB lock) plans also add it; whichever lands first adds the line.
- `tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs` (image `localstack/localstack:4.14.0`, `Endpoint`, collection `Cloud-LocalStack`) exists; its `ClearAllDataAsync` clears SQS and SNS only, so DynamoDB tests delete their own table.
- SPEC-000 section 6.1 names #579 among the cloud-backed stores in 1.0, tested against LocalStack or DynamoDB Local.

**Provider category**: cloud audit store (AWS side of the AWS/Azure pair with #578), outside the 10-provider database matrix. **Affected packages**: a new DynamoDB package (name per Design Choice 1), `Encina.Security.Audit` (the contract, from #1778), `Encina` (EventId registry), `Directory.Packages.props`, test projects.

---

## Design Choices

<details>
<summary><strong>1. Package name and shape (Dilemma D1)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.AmazonDynamoDB`, store in an `EntityChangeAudit/` folder** | Service-named like `Encina.AmazonSQS`; the dead letter store of #589 can share the client registration and health check | Named after the service, not after audit |
| **B) `Encina.Security.Audit.DynamoDB`** | Audit backends under one prefix; per-feature installation | A second DynamoDB client registration when #589 ships |
| **C) `Encina.AWS.DynamoDB`** (the issue) | Groups AWS packages | A dotted `AWS.` prefix nobody else uses (`Encina.AwsLambda`, `Encina.AmazonSQS`) |

### Chosen Option: **A — `Encina.AmazonDynamoDB`** (recommended, pending the maintainer)

### Rationale

- The #589 plan recommends a per-feature `Encina.Messaging.DynamoDB`; the maintainer sets one rule for #579 and #589 together (the lock of #210 stays in the `Encina.DistributedLock.*` tree). A wins when several features share one AWS account setup; B wins when every feature must be installed alone.

</details>

<details>
<summary><strong>2. Key layout and consistency of the history read (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Base table: `PK` = length-prefixed `(tenant, entityType, entityId)`, `SK` = `{changedAtUtc fixed-width}#{entryId}`; history is a consistent `Query` with `ScanIndexForward = false`; a sparse GSI `RetentionIndex` (`RPK` = `{tenant}#{yyyyMMdd}`, `RSK` = `SK`) serves only the purge** | History is read-your-writes; purge finds old items without a scan; a GSI's eventual consistency is harmless for purge (a new item is never older than the cutoff) | One GSI to create and pay for |
| **B) The issue's schema: `Id` primary key, `EntityIndex` and `UserIndex` GSIs** | Direct lookup by entry id; user activity queries | History through a GSI can miss a just-written entry; the concatenated GSI key is not injective |
| **C) Option A without the GSI; purge scans the table** | No GSI | A full scan per purge |

### Chosen Option: **A — entity partition with consistent reads, retention GSI** (recommended, pending the maintainer)

### Rationale

- The database providers answer history read-your-writes; A keeps that on DynamoDB. B wins when user-activity queries matter more than consistent entity history (a `UserIndex` GSI can be added later without changing A); C wins for small tables.
- **How each option finds one tenant's items older than its cutoff** (the per-tenant purge of Design Choice 3): A queries `RetentionIndex` on the tenant's day partitions before the cutoff; B needs a tenant-and-time GSI added to the issue's schema; C scans with a filter on the tenant and `changedAtUtc`.
- **Key encoding (rule).** Each segment is written as `{length}:{value}` so the key is injective (`A#B` + `C` never equals `A` + `B#C`); a key over 2048 bytes (partition key limit) is a `Left` (`Validation`).
- **Tenant segment (rule).** With tenancy on, the entry's `TenantId` is the first segment; the reserved segment `default` marks tenant-less entries, written only when tenancy is off; a real tenant `default` is rejected on write (`InvalidRequest`, ADR-062 R3); with tenancy on and no tenant resolved, write and history read deny (`InvalidRequest`; `AGENTS.md` section 3), never falling back to `default`. The purge is not bound to the ambient tenant and has no cross-tenant opt-in: it receives the tenant explicitly from the retention service (Design Choice 3, #4182).

</details>

<details>
<summary><strong>3. TTL support (the issue's acceptance criterion) (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A per-tenant `PurgeEntriesAsync` is authoritative: the ADR-036 `EntityChangeAuditRetentionService` runs in a tenant-less scope, enumerates `ITenantStore.GetAllTenantsAsync` and calls the purge once per tenant with that tenant's cutoff (#4182); an opt-in `TtlBackstop` also writes an `expiresAtEpoch` attribute = `ChangedAtUtc` + the writing tenant's retention + `TtlBackstopMargin` and enables TTL on table creation** | The contract's per-tenant, counted purge stays the mechanism; TTL bounds cost if the service stops; meets the acceptance criterion as an option | Two mechanisms to explain; TTL deletes happen hours to days after expiry and are not logged by Encina; expiries are fixed at write time, so when a tenant's retention is lengthened or a legal hold applies, the opt-in TTL deletes audit items before the new retention or the hold ends |
| **B) TTL only; `PurgeEntriesAsync` deletes nothing and returns 0** | No delete cost | Deletion is not an audited, counted act; a policy change never reaches existing items; breaks provider coherence (`AGENTS.md` section 5) |
| **C) Purge only; no TTL** | One mechanism | Drops an acceptance criterion of the issue |

### Chosen Option: **A — purge authoritative, TTL as an opt-in backstop** (recommended, pending the maintainer)

### Rationale

- ADR-036 decision 1 requires `PurgeEntriesAsync` whatever TTL does. A wins over B because per-tenant retention (SPEC-002 REQ-007, #4182) needs a purge that follows each tenant's current policy; C wins when the maintainer prefers one mechanism and drops the criterion. The default `TtlBackstop = false`, so nothing expires behind the contract's back unless opted in (pay-for-what-you-use).
- Under every option the purge receives its tenant explicitly (signature from #4182 and #1778), never reads the ambient tenant, and has no cross-tenant opt-in; a call without a tenant while tenancy is on is a `Left` (`InvalidRequest`).
- **Tenant segments that `ITenantStore` no longer returns (rule, under every option).** Enumerating registered tenants alone would never purge two kinds of items, which GDPR Art. 5(1)(e) and SPEC-002 REQ-007 forbid keeping past their retention: items of an offboarded tenant (kept under path C of #2847 after its tenant record is removed) and `default`-segment items written before tenancy was switched on; the TTL backstop is off by default and cannot cover them. The store therefore also lists the distinct tenant segments it holds (`ListTenantSegmentsAsync`, or the name #1778 gives it) through a key-only segment item written on the first write of a segment in a process. Its shape follows the layout of Design Choice 2: under layouts A and C (partition and sort key) it is `PK = SEGMENTS`, `SK = {length-prefixed segment}`, listed with a consistent `Query` of that partition; under layout B (key `Id` only) it is `Id = SEGMENT#{length-prefixed segment}` with a `kind = segment` attribute in a sparse GSI keyed on `kind`, listed with a `Query` of that GSI (eventual consistency is harmless, because a segment is registered before its first entry and the listing only needs segments that already have old items). The segment item is written with an unconditional `PutItem` (an idempotent upsert of a key-only item; it never goes through the payload-hash comparison of Design Choice 8, which applies to entry items only). Write order and failure rules: the segment item is put before the first entry item of that segment; a failed put fails the write with a `Left`; the in-process "already registered" cache is set only after the put succeeds, so the next write retries it; segment items are never deleted. A failed or incomplete listing is a `Left` (`Internal`), never a shorter list reported as complete. The retention service purges every listed segment that `ITenantStore` does not return, including `default` once tenancy is on, under the residual policy #4182 defines (for example the policy recorded at offboarding, or the default retention of `EntityChangeAuditOptions`). This case is an acceptance item of #4182. The listing runs whether tenancy is on or off, so segments that real tenants wrote before tenancy was switched off are purged too.

</details>

<details>
<summary><strong>4. Items larger than the 400 KB limit (Dilemma D4)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Store `oldValues` and `newValues` as strings; when the item would exceed 400 KB, store them gzip-compressed as binary attributes (`oldValuesGz`, `newValuesGz`); still over the limit is a `Left` (`Validation`)** | JSON compresses several times, so far more entities can be audited; one item per entry | Compression code and a flag on read |
| **B) Reject over 400 KB with a `Left`** | Simplest | With the fail-closed writer, saving a large entity fails the unit of work |
| **C) Offload to S3** | No size limit | A second service, non-atomic two-service write, another credential |

### Chosen Option: **A — compress when needed, reject beyond** (recommended, pending the maintainer)

### Rationale

- B turns a storage limit into an application outage. C wins when entities routinely exceed what compression brings under 400 KB.

</details>

<details>
<summary><strong>5. Client registration and table creation (Dilemma D5)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An application-registered `IAmazonDynamoDB`, or options (`Region`, optional `ServiceUrl` for LocalStack) that build a client with the default credential chain; the table and GSI are created only when `CreateTableIfNotExists` is set (default `false`), on-demand billing** | Any credential, retry and endpoint setup is possible; no table appears in production by surprise | Two registration paths to test |
| **B) Options only; table created by default** | Zero setup | Encina creating production tables by default; IAM `CreateTable` needed at run time |

### Chosen Option: **A — supplied client or options; table creation opt-in** (recommended, pending the maintainer)

### Rationale

- Same registration shape as Design Choice 6 of the #210 plan. Long-term credentials (if ever set in options) are `[JsonIgnore]` and `ToString()` prints none. B wins for demos and tests, which can set the flag.

</details>

<details>
<summary><strong>6. Tamper evidence (Dilemma D6)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Append-only (conditional `PutItem` only; no `UpdateItem`), a SHA-256 `payloadHash` attribute, and a documented least-privilege IAM policy (writer: `PutItem`, `Query`, `GetItem`; retention: also `DeleteItem`, `BatchWriteItem`); a hash chain is left to #1240** | No new mechanism; a silent edit is detectable on the conflict path | A principal with `PutItem` without the condition can still overwrite an item and its hash |
| **B) A per-store hash chain now** | Rewrites detectable | Serialises writes; duplicates #1240 for one provider |

### Chosen Option: **A — append-only, payload hash, least-privilege IAM; chain through #1240** (recommended, pending the maintainer)

### Rationale

- DynamoDB has no write-once mode; immutability is an IAM property (an IAM condition cannot force `attribute_not_exists`). #1240 (open) owns the chain. B wins if chain verification is required before #1240 lands.

</details>

<details>
<summary><strong>7. Contract, naming, API level and failure semantics</strong></summary>

**Class:** Settled
**Settled by:** ADR-036 (decisions 1 and 7), ADR-001, ADR-006, ADR-062 (R2-R6), AGENTS.md section 3 (TimeProvider; fail closed; errors never swallowed), AGENTS.md section 4

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The ADR-036 contract on the low-level `IAmazonDynamoDB` API: class `EntityChangeAuditStoreDynamoDB`, `Either` results, `TenantId`, `ModuleId`; conditional `PutItem` with `ReturnValuesOnConditionCheckFailure = ALL_OLD`; a failed write is a `Left` that fails the unit of work; cancellation propagates** | Provider coherence; the conditional write and the stored item on conflict are available | Mapping written by hand |
| **B) `DynamoDBContext` (object persistence model) as in the issue** | Less mapping code | `SaveAsync` is an upsert without an `attribute_not_exists` condition or the old item on conflict, so a duplicate id overwrites an audit entry silently |

### Chosen Option: **A — the ADR-036 contract on the low-level API**

### Rationale

- B cannot express the conditional create that section 3 needs (no silent overwrite), and the old `IAuditLogStore` shape is removed by ADR-036 decision 7. The issue's criterion "`AuditLogDynamoItem` with GSI annotations" belongs to the object persistence model B excludes; it is met by the hand-written item mapper (Phase 1) and the GSI definition of the table initializer (Phase 2). Every `EncinaErrors.Create` passes a named `category:` and a constant message; only the AWS error code and HTTP status reach errors and logs, never `AmazonServiceException.Message`.

</details>

<details>
<summary><strong>8. Idempotent write</strong></summary>

**Class:** Settled
**Settled by:** AGENTS.md section 3 (errors never swallowed), AGENTS.md section 6 function 9, ADR-062 R2

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `PutItem` with `attribute_not_exists(<the table's partition key>)` (the item's own key under every layout of Design Choice 2); on `ConditionalCheckFailedException` compare the returned item's `payloadHash`: identical is success, different is `Left` (`AlreadyExists`)** | Safe retries of the same entry; a different payload under the same key, or an edited item, is never silent; no extra read (the old item comes back with the failure) | Detects duplicates only under the same full key (see Rationale) |
| **B) Unconditional `PutItem`** | Simplest | Overwrites an audit entry |
| **C) Treat every conditional failure as success** | No compare | Loses an entry on a collision |

### Chosen Option: **A — conditional put, compare on conflict**

### Rationale

- B and C lose or overwrite audit data silently.
- **Scope of the guarantee.** Under the recommended layout the sort key contains `ChangedAtUtc` as well as the entry id, so the condition covers a retry of the same entry (the writer builds an entry once, so a retry carries the same `ChangedAtUtc`) and a different payload under the same key. The same entry id written again with a different `ChangedAtUtc` is stored as a second item and is not detected by the store: entry-id uniqueness across timestamps is the writer's guarantee (the EF Core writer builds `Id: Guid.NewGuid().ToString()` for each entry, `src/Encina.EntityFrameworkCore/Auditing/AuditInterceptor.cs:377,423` on origin/main `ff9a579c1`), and the README says so.

</details>

<details>
<summary><strong>9. Erasure requests and immutability of entries</strong></summary>

**Class:** Settled
**Settled by:** maintainer decision of 2026-10-26 on #2847 (path C is the default for audit data: GDPR Art. 17(3)(b) and 17(3)(e)), ADR-036 decision 1

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No per-subject delete; items never updated; removal only by retention (the per-tenant `PurgeEntriesAsync`, plus TTL where Design Choice 3 enables it); minimisation by the writer's redaction hook** | Matches the audit erasure decision | An exceptional per-person deletion needs a separate capability (Marten counterpart: #2903) |
| **B) A per-subject delete method** | Answers erasure directly | Contradicts path C as the default |

### Chosen Option: **A — retention only, no per-subject delete**

### Rationale

- Path C records the request and keeps audit data until retention; B is the exceptional path, planned separately.

</details>

---

## Implementation Phases

### Phase 1: Package, options, item mapping and keys

<details>
<summary>Tasks</summary>

1. `src/Encina.AmazonDynamoDB/Encina.AmazonDynamoDB.csproj` (name per Design Choice 1; `AWSSDK.DynamoDBv2` at the AWSSDK 4 line, `Encina.Security.Audit`, PublicAPI analyzers, XML docs, `InternalsVisibleTo` for `Encina.UnitTests`, `Encina.PropertyTests`, `Encina.IntegrationTests`, `Encina.ContractTests`, not `Encina.GuardTests`, so the guard target of 0 for internal types stays valid); `Encina.slnx` row; PublicAPI x2; `Directory.Packages.props` line unless #589 or #210 added it.
2. `EncinaAmazonDynamoDBOptions` (public, sealed): `Region`, `ServiceUrl`; `ToString()` safe.
3. `EntityChangeAudit/EntityChangeAuditDynamoDBOptions` (public, sealed): `TableName` (default `EntityChangeAuditEntries`), `CreateTableIfNotExists` (`false`), `TtlBackstop` (`false`), `TtlBackstopMargin` (default 30 days), `CompressionThresholdBytes`. No cross-tenant option: the purge receives its tenant explicitly (Design Choice 3).
4. `EntityChangeAudit/EntityChangeAuditDynamoKeys` (internal): length-prefixed `PK`, fixed-width `SK`, `RPK` (Design Choice 2).
5. `EntityChangeAudit/EntityChangeAuditItemMapper` (internal): entry to and from `Dictionary<string, AttributeValue>`, compression (Design Choice 4), `payloadHash`, optional `expiresAtEpoch`; null attributes omitted.
6. Unit tests (keys with `#` and `:` in segments, colliding concatenations, fixed-width ordering with different offsets; mapper round trip, compression, 400 KB limit), options guard tests, manifest `.github/coverage-manifest/Encina.AmazonDynamoDB.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Encina is a .NET 10 / C# 14 library (ROP). The ADR-036 contract is delivered by #1778; build on its real shapes on main.
- AWSSDK.DynamoDBv2 may already be pinned by #589 or #210; add it only if missing, at the AWSSDK 4 line.

TASK:
Create the package, options, key builder and item mapper of the Phase 1 Tasks with tests and manifest.

KEY RULES:
- Keys length-prefixed and injective; SK fixed-width so string order is time order.
- Compression only above the threshold; over 400 KB is a Left (Validation).
- No [Obsolete], no compatibility layer; XML docs; PublicAPI complete.

REFERENCE FILES:
- docs/architecture/adr/036-three-audit-stores.md
- docs/plans/deadletter-dynamodb-store-implementation-plan-589.md
- src/Encina.AmazonSQS/Encina.AmazonSQS.csproj
```

</details>

---

### Phase 2: `EntityChangeAuditStoreDynamoDB` and its registration

<details>
<summary>Tasks</summary>

1. `EntityChangeAudit/EntityChangeAuditStoreDynamoDB.cs` (public, sealed) `: IEntityChangeAuditStore`; constructor `(IAmazonDynamoDB client, ITenantContext? tenantContext, IOptions<EntityChangeAuditDynamoDBOptions> options, TimeProvider timeProvider, ILogger<EntityChangeAuditStoreDynamoDB> logger)` (tenant abstraction as in #1778).
2. Write: tenant rules first; then, if this process has not yet registered the segment, an unconditional `PutItem` of the segment item of Design Choice 3 (a failure is a `Left` and stops the write; the cache is set only after success); then the conditional `PutItem` of the entry item (Design Choice 8); `ItemCollectionSizeLimitExceeded` and over-size `Left` (`Validation`); `ProvisionedThroughputExceeded`, `RequestLimitExceeded`, `ThrottlingException` and 5xx `Left` (`Unavailable`); other `AmazonDynamoDBException` `Left` (`Internal`) with the error code only.
3. History: `Query` on `PK` with `ConsistentRead = true`, `ScanIndexForward = false`, paged through the contract's `PagedResult<T>` with `ExclusiveStartKey` encoded as an opaque token.
4. `PurgeEntriesAsync` with the tenant and cutoff it receives (signature from #4182 and #1778): find that tenant's items older than the cutoff as Design Choice 2 describes (under option A, `RetentionIndex` per day partition of that tenant), `BatchWriteItem` deletes of 25, retrying `UnprocessedItems` with bounded backoff; any remaining failure is `Left` (`Internal`); returns the count; never reads the ambient tenant; with tenancy on and no tenant passed returns `Left` (`InvalidRequest`). The store must be constructible in the tenant-less scope of the retention service.
5. `EntityChangeAudit/EntityChangeAuditDynamoDBErrors.cs`; `EntityChangeAudit/EntityChangeAuditTableInitializer.cs` (internal `IHostedService`: `CreateTable` with on-demand billing, the GSI, TTL on `expiresAtEpoch` when `TtlBackstop`; only when `CreateTableIfNotExists`).
6. `ServiceCollectionExtensions`: `AddEncinaAmazonDynamoDB(this IServiceCollection, Action<EncinaAmazonDynamoDBOptions>? configure = null)` (`TryAddSingleton<IAmazonDynamoDB>` built from options unless registered) and `AddEncinaEntityChangeAuditDynamoDB(this IServiceCollection, Action<EntityChangeAuditDynamoDBOptions>? configure = null)`: options with `ValidateOnStart`, `TryAddSingleton(TimeProvider.System)`, the store as `IEntityChangeAuditStore` winning over the in-memory default in any order without overriding an application registration (mechanism from #1778), the initializer through `TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, EntityChangeAuditTableInitializer>())`.
7. EventId range and log class registered in this PR (the store and initializer log). DI test with `ValidateOnBuild` and `ValidateScopes`, both orders. CRAP <= 10 per method.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Phase 1 created the package, options, keys and mapper.

TASK:
Implement the store, errors, table initializer, both registrations, the EventId range and the log class, with unit, guard and DI tests.

KEY RULES:
- Conditional PutItem with ReturnValuesOnConditionCheckFailure = ALL_OLD; compare payloadHash.
- History with ConsistentRead; purge through the retention GSI; UnprocessedItems retried, never dropped.
- Only AWS error codes and status reach errors and logs; cancellation propagates.
- Hosted services through TryAddEnumerable.

REFERENCE FILES:
- docs/architecture/adr/062-error-category-contract.md
- docs/plans/lock-dynamodb-implementation-plan-210.md (client registration)
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 3: Health check

<details>
<summary>Tasks</summary>

1. `EntityChangeAudit/EntityChangeAuditDynamoDBHealthCheck.cs : IEncinaHealthCheck`: `DescribeTable` (status `ACTIVE`, GSI present); const `DefaultName`, static `Tags`; registered by `AddEncinaEntityChangeAuditDynamoDB`.
2. Unit and guard tests; manifest edit; `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Phase 2 shipped the store and its registration.

TASK:
Add the health check and register it.

KEY RULES:
- DefaultName const, Tags static array; status from DescribeTable only.

REFERENCE FILES:
- src/Encina.Kafka/Health/KafkaHealthCheck.cs
```

</details>

---

### Phase 4: Cross-cutting integration and observability

<details>
<summary>Tasks</summary>

1. EventId range `DynamoDBEntityChangeAudit = (52450, 52499)` in `src/Encina/Diagnostics/EventIdRanges.cs`, registered in PR 2 with its line in `src/Encina/PublicAPI.Unshipped.txt` (one line for the new field), the `AssemblyRanges` entry in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`, and in `docs/engineering/eventid-block-registry.md` a "Registered ranges" row `DynamoDBEntityChangeAudit | 52450-52499` (Zone `zone`) and a "Blocks" row `52450-52499` with status `registered`.
2. `EntityChangeAudit/Diagnostics/DynamoDBEntityChangeAuditLog.cs`, ids packed from 52450: 52450 table created, 52451 write failed (error code), 52452 duplicate write ignored, 52453 id conflict, 52454 entry too large, 52455 entry compressed, 52456 tenant purge completed (count), 52457 purge unprocessed items retried, 52458 tenant purge failed. 52459-52499 stay free.
3. The ADR-036 `InstrumentedEntityChangeAuditStore` decorates the store (DI test asserts it); ADR-054 D4 tenant attribute and tag only when multi-tenancy is on; logs never carry tenant, user or entity ids, values or messages.
4. **ErrorCategory (ADR-062)**: `entity_change_audit.dynamodb.write_failed` `Internal` (R4); `.unavailable` (throttling, 5xx) `Unavailable`; `.entry_too_large` and `.key_too_long` `Validation` (R3); `.id_conflict` `AlreadyExists` (R2); `.tenant_required` and `.reserved_tenant` `InvalidRequest` (R3); `.purge_failed` `Internal`. Cancellation propagates.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- AGENTS.md section 7: new ranges only in the 10000+ zone, registered before use; EventIdRegistryAgreementTests read the registry page.

TASK:
Register DynamoDBEntityChangeAudit 52450-52499 (in PR 2), write the log class, wire the categories and the decorator test.

KEY RULES:
- Ids packed from 52450; nothing outside the range.
- Never log EncinaError.Message, exception messages, values or ids.

REFERENCE FILES:
- docs/engineering/eventid-block-registry.md
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 5: Testing

> **Goal**: every applicable flag reaches its target in `.github/coverage-manifest/Encina.AmazonDynamoDB.json`, each target and its justification set in the pull request whose tests reach it.

<details>
<summary>Tasks</summary>

1. **Unit**: keys, mapper, every error mapping (conditional failure with same and different hash, size, throttling, 5xx, other), tenant rules, purge of the tenant passed in (never the ambient one; tenancy on and no tenant passed is `Left`), segment item put unconditionally before the entry and once per segment (a second process registering the same segment succeeds), a failed segment put fails the write and the next write retries it, a failed listing is `Left`, the listing runs with tenancy off, purge with unprocessed items, TTL attribute from the writing tenant's retention, initializer branches; `IAmazonDynamoDB` substituted (an interface; the request and response model classes have public constructors and setters), `FakeTimeProvider`.
2. **Guard** (`tests/Encina.GuardTests/AmazonDynamoDB/`): store constructor and methods, both registrations, options.
3. **Contract** (PR 4): a class deriving from the shared entity-change contract base on `[Collection("Cloud-LocalStack")]`.
4. **Integration** (PR 4, `[Collection("Cloud-LocalStack")]`, `[Trait("Category", "Integration")]`): idempotent repeated write, id conflict, consistent history right after a write, paging, compressed large values, colliding concatenations, tenant isolation, per-tenant purge from a scope with no ambient tenant (tenant A's cutoff deletes only tenant A's old items; tenant B with a longer retention keeps its own), a tenant removed from `ITenantStore` and the `default` segment written before tenancy was enabled are listed and purged under the residual policy, TTL attribute present when opted in, table creation, health check. The class deletes its table in `InitializeAsync` (the fixture clears SQS and SNS only) and calls `ClearAllDataAsync`. `LocalStackFixture` pins `localstack/localstack:4.14.0`, which starts without a `LOCALSTACK_AUTH_TOKEN` (#2699) and serves DynamoDB in the community edition; `Testcontainers.LocalStack` 4.15.0 (MIT) is pinned.
5. **Property** (PR 4): key injectivity, `SK` order equals time order, mapper round trip.
6. **Load/Benchmark**: `.md` justifications (append-only, not a hot path).

**Manifest rule**: the manifest is new (no existing floor to respect). PR 1 creates it with package-level `targets` for unit and guard; PR 4 adds the package-level contract, property and integration targets. Each PR adds the per-file `targets` and `justifications` below for the flags its own tests reach, so `coverage-report.cs --check-justifications` passes on every PR.

**Per-file targets** (`-` = not applicable; "PR" adds the target, its tests and its justification):

| File | Unit | Guard | Contract | Property | Integration | PR | Justification |
|------|:----:|:-----:|:--------:|:--------:|:-----------:|:--:|---------------|
| `EncinaAmazonDynamoDBOptions.cs` | 90 | 50 | - | - | 60 | 1, 4 | Unit: defaults, validation and the safe `ToString()`. Guard: invalid values on each validated property run the validation half of the file. Integration: the LocalStack tests build the options through the registration. |
| `EntityChangeAudit/EntityChangeAuditDynamoDBOptions.cs` | 90 | 50 | - | - | 60 | 1, 4 | Unit: defaults and validation of names and margins. Guard: invalid names and margins run the validation branches. Integration: built through the registration in the LocalStack tests. |
| `EntityChangeAudit/EntityChangeAuditDynamoKeys.cs` | 95 | 0 | - | 95 | 80 | 1, 4 | Unit: length prefixes, fixed-width sort keys and the key limit. Guard: 0, internal and `Encina.GuardTests` has no `InternalsVisibleTo`. Property: injectivity and time order over random values. Integration: every LocalStack write uses the keys. |
| `EntityChangeAudit/EntityChangeAuditItemMapper.cs` | 95 | 0 | - | 90 | 85 | 1, 4 | Unit: round trip, compression and the item limit. Guard: 0, internal with no `InternalsVisibleTo` for guard tests. Property: round trip of random entries. Integration: LocalStack round-trips compressed values. |
| `EntityChangeAudit/EntityChangeAuditStoreDynamoDB.cs` | 90 | 30 | 85 | - | 85 | 2, 4 | Unit: fault paths through a substituted client. Guard: constructor and method argument checks only, about a third of the lines of a store that does I/O. Contract: the shared suite drives every contract method. Integration: LocalStack covers write, history, purge and segment listing. |
| `EntityChangeAudit/EntityChangeAuditDynamoDBErrors.cs` | 100 | 0 | - | - | - | 2 | Unit: every factory method is called by the store tests. Guard: 0, internal with no validated parameters. |
| `EntityChangeAudit/EntityChangeAuditTableInitializer.cs` | 90 | 0 | - | - | 80 | 2, 4 | Unit: opt-in and TTL branches. Guard: 0, internal with no `InternalsVisibleTo` for guard tests. Integration: LocalStack proves creation. |
| `ServiceCollectionExtensions.cs` | 95 | 40 | - | - | - | 2 | Unit: the DI test builds the provider with ValidateOnBuild in both orders. Guard: null `services` and null `configure` run the argument checks before a short registration body. |
| `EntityChangeAudit/EntityChangeAuditDynamoDBHealthCheck.cs` | 90 | 30 | - | - | 70 | 3, 4 | Unit: active, missing-table and failure mappings. Guard: the constructor checks only. Integration: LocalStack proves the healthy path. |
| `EntityChangeAudit/Diagnostics/DynamoDBEntityChangeAuditLog.cs` | 0 | 0 | - | - | - | 2 | Unit: 0, source-generated `[LoggerMessage]` partial methods with no hand-written executable code. Guard: 0, same reason. |

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- Tests execute real package code; LocalStack is shared through the Cloud-LocalStack collection.

TASK:
Write the tests of Phase 5 and the manifest targets with justifications in the pull requests that reach them.

KEY RULES:
- Shouldly via Encina.Testing.Shouldly; no FluentAssertions, per-class containers or Thread.Sleep.
- Delete the test table in InitializeAsync; FakeTimeProvider.
- Targets never go down; run coverage-report.cs --check-justifications.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs
- tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/LocalStackCollection.cs
- tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs
```

</details>

---

### Phase 6: Documentation and finalization

<details>
<summary>Tasks</summary>

1. XML docs; `src/Encina.AmazonDynamoDB/README.md` (key layout, consistent reads, retention and TTL backstop, compression, IAM policy, LocalStack); a feature page under `docs/features/`; `docs/INVENTORY.md`; ROADMAP if affected (through `docs-writer`).
2. `changelog.d/579-dynamodb-entity-change-audit-store.added.md` (through `mechanical-fixer`).
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
| SPEC-000 section 6.1, DEC-009 | 1.0 scope | #579 named in 1.0; tested against LocalStack |
| SPEC-002 REQ-007, REQ-061 | Retention, isolation | Per-category retention; tenant isolation |
| #4182 (open, `[DEBT]`) | Per-tenant retention | Retention iterates `ITenantStore` with each tenant's policy; never the ambient tenant or a cross-tenant opt-in |
| GDPR Art. 5(1)(e), 17(3)(b) | Storage limitation, erasure exception | Purge at retention; recorded erasure (decision on #2847) |
| DynamoDB limits (vendor) | Items, keys, batches, TTL | 400 KB items, 2048-byte partition keys, 25 items per `BatchWriteItem`, TTL deletes asynchronously |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in the DynamoDB entity-change audit store |
|-----------|----------|-------------------------------------------------|
| `IAuditLogStore` (becomes `IEntityChangeAuditStore`) | `src/Encina.DomainModeling/Auditing/IAuditLogStore.cs` | The contract implemented after #1778 |
| `AuditLogEntry` (becomes `EntityChangeAuditEntry`) | `src/Encina.DomainModeling/Auditing/AuditLogEntry.cs` | The entry mapped to the item |
| `InMemoryAuditLogStore` | `src/Encina.DomainModeling/Auditing/InMemoryAuditLogStore.cs` | The default the store must win over |
| `EntityChangeAuditRetentionService`, `InstrumentedEntityChangeAuditStore` | delivered by #1778 | Per-tenant purge scheduling (#4182) and tracing |
| `ITenantStore.GetAllTenantsAsync` | `src/Encina.Tenancy/Abstractions/ITenantStore.cs` | Tenant enumeration of the retention service (#4182) |
| `OperationAuditRetentionService` | `src/Encina.Security.Audit/OperationAuditRetentionService.cs` | Shape of the tenant-less retention scope |
| `LocalStackFixture`, `Cloud-LocalStack` collection | `tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs`, `tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/LocalStackCollection.cs`, `tests/Encina.ContractTests/MessageBrokers/AmazonSQS/LocalStackCollection.cs` | Shared emulator |
| AWSSDK 4 entries, `Testcontainers.LocalStack` | `Directory.Packages.props` | Version line for `AWSSDK.DynamoDBv2` |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Registration of the new range |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.AmazonDynamoDB` (entity-change audit) | 52450-52499 (`DynamoDBEntityChangeAudit`) | Third of the five audit-stores-1 slots 52350-52599, the first free Security and compliance space after the #4007 reservations 50000-52349 (registry on origin/main `1dee4ba61`); ids 52450-52458 used, packed; registered by PR 2 with its registry rows |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production | ~11 | csproj, two options, keys, mapper, store, errors, initializer, registration, health check, log class |
| Tests | ~12 | unit x4, guard x2, DI, contract, integration, property, load/benchmark `.md` x2 |
| Metadata | ~10 | `Encina.slnx`, PublicAPI x2, manifest, `Directory.Packages.props`, `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, registry page, allocation test, test-project references |
| **Total** | **~39 with edits** | Every PR starts after #1778, builds and passes alone, at most 15 files and 1500 lines. **PR 1** (14 files): csproj, `Encina.slnx`, `Directory.Packages.props` (13 if already pinned), PublicAPI x2, two options, keys, mapper, `Encina.UnitTests` and `Encina.GuardTests` project references, unit tests x2, options guard tests, manifest (no service introduced). **PR 2** (15 files): store, errors, initializer, `ServiceCollectionExtensions`, log class, `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, registry page, `EncinaEventIdAllocationTests.cs`, package `PublicAPI.Unshipped.txt`, store unit tests, initializer unit tests, DI test, guard tests, manifest. **PR 3** (6 files): health check, registration edit, `PublicAPI.Unshipped.txt`, health check unit tests, guard tests, manifest. **PR 4** (<= 10 files): three test-project references, contract class, integration tests, property tests, two `.md` justifications, manifest. **PR 5** (4 files): README, feature page, `docs/INVENTORY.md`, changelog fragment |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution first.
- ROP: Either<EncinaError, T>; errors never swallowed; TimeProvider; async with CancellationToken; cancellation propagates.
- Issue #579: EntityChangeAuditStoreDynamoDB implements IEntityChangeAuditStore (ADR-036, delivered by #1778) in a new package (name per Design Choice 1, recommended Encina.AmazonDynamoDB).

IMPLEMENTATION OVERVIEW:
Phase 1: package, options, length-prefixed keys, item mapper with compression.
Phase 2: store (conditional put with compare, consistent paged history, per-tenant purge with the tenant passed by the retention service), initializer, registrations, EventId range 52450-52499, log class.
Phase 3: health check.
Phase 4: error categories, instrumentation, telemetry tags.
Phase 5: unit, guard, contract, integration (Cloud-LocalStack), property tests; manifest targets with justifications.
Phase 6: docs and changelog fragment.

KEY PATTERNS:
- Dilemmas D1-D6 are recommended, pending the maintainer: follow the answers recorded in the plan.
- Low-level IAmazonDynamoDB, never DynamoDBContext.SaveAsync (silent overwrite).
- Tenant: reserved segment default, deny writes and reads without tenant under tenancy; no cross-tenant opt-in exists.
- Append-only; no per-subject delete; per-tenant purge authoritative (the retention service iterates ITenantStore and passes each tenant, #4182), TTL only as an opt-in backstop.

REFERENCE FILES:
- docs/architecture/adr/036-three-audit-stores.md
- docs/architecture/adr/062-error-category-contract.md
- tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs
- docs/plans/deadletter-dynamodb-store-implementation-plan-589.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The issue's "cache frequent audit queries" is dropped: an audit read must be exact and current, so no issue is needed. |
| 2 | OpenTelemetry | ✅ | The ADR-036 `InstrumentedEntityChangeAuditStore` decorates the store; tenant tag per ADR-054 D4. |
| 3 | Structured logging | ✅ | Range 52450-52499 registered first; AWS error codes only. |
| 4 | Health checks | ✅ | Phase 3: `EntityChangeAuditDynamoDBHealthCheck` (`DescribeTable`). |
| 5 | Validation | ✅ | Argument checks throw before I/O; long keys and oversize items are `Validation` `Left` values. |
| 6 | Resilience | ✅ | The AWS SDK retry mode (standard, with throttling backoff) applies to every call; `UnprocessedItems` of the purge are retried with bounded backoff; no extra Polly policy. |
| 7 | Distributed locks | ❌ | Append-only store; the conditional put arbitrates concurrent writes of one key. |
| 8 | Transactions | ❌ | One entry per item; no unit of work spans DynamoDB. |
| 9 | Idempotency | ✅ | Conditional put with compare on conflict (Design Choice 8). |
| 10 | Multi-tenancy | ✅ | Tenant as the first key segment; reserved `default`; deny without tenant (Design Choice 2); per-tenant retention purge with the tenant passed explicitly (Design Choice 3, #4182). |
| 11 | Module isolation | ✅ | `ModuleId` stored on every item (supersedes the issue's "N/A"). |
| 12 | Audit trail | ✅ | This store IS the entity-change audit trail. |

---

## Prerequisites & Dependencies

- **#1778 (open, `[DEBT]`, no `needs-decision`, no blocking dependency)** delivers the contract, the retention service, the decorator, the in-memory default and the shared behavioral contract base. If it merges without a reusable base, the first of #577-#582 to reach its integration pull request adds it (the same fallback as the #582 plan).
- #589 (DynamoDB dead letters) shares Design Choice 1; #589 and #210 also add `AWSSDK.DynamoDBv2`.
- #1240 (open) owns the hash chain (Design Choice 6); #2903 (open) is the exceptional subject-scoped erasure path for Marten.
- **#4182 (open, `[DEBT]`)** decides the per-tenant retention mechanism (tenant enumeration through `ITenantStore`, each tenant's policy, the tenant-explicit purge signature, and the residual policy for segments `ITenantStore` no longer returns: offboarded tenants and the legacy `default` segment) before #1778 implements `EntityChangeAuditRetentionService`; Design Choice 3 and Phase 2 task 4 follow it, and the TTL backstop reads the writing tenant's policy from the same source.
- The EventId slot 52450-52499 lies after the #4007 reservations (50000-52349, #4174 merged).

---

## Next Steps

1. The maintainer answers D1 to D6 when work on #579 starts (D1 together with #589); the answers go into `## Maintainer Decisions`.
2. Wait for #4182 and #1778; re-check each Design Choice against main; `adversarial-reviewer` verifies the Settled ones.
3. Brief one worker per proposed pull request (5 pull requests).
4. The final pull request references `Fixes #579`.
