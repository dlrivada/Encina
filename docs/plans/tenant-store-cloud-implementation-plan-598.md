# Implementation Plan: Cloud-native `ITenantStore` (Azure Cosmos DB, Azure Table Storage, AWS DynamoDB)

> **Issue**: [#598](https://github.com/dlrivada/Encina/issues/598)
> **Type**: Feature
> **Complexity**: Medium-High (6 phases, 3 new packages, 10 pull requests)
> **Estimated Scope**: ~1,500 lines of production code + ~2,600 lines of tests across three stores

---

## Summary

Implement the tenant catalog of #596 on three cloud-native stores: `TenantStoreCosmosDB`, `TenantStoreAzureTable` and `TenantStoreDynamoDB`, each tested against a free local emulator. SPEC-000 section 6.1 lists #598 among the cloud-backed stores that are in 1.0 because an emulator exists (Azurite for Table Storage, LocalStack for DynamoDB, the Cosmos DB emulator for Cosmos DB). The issue's samples are replaced where they do not fit today's contract: they implement an interface (`GetByIdAsync`, `Task<Tenant>`) that does not exist, build a DynamoDB key by string interpolation with no encoding rule, use the tenant id as a Table row key although `/`, `\`, `#` and `?` are forbidden in row keys, and catch the SDK exceptions for "not found" without saying what the writes do.

What the code looks like today (`origin/main` `4b0a600c7`):

- `ITenantStore` is read-only (`GetTenantAsync`, `GetAllTenantsAsync`, `ExistsAsync`; `ValueTask`, no `Either`); `InMemoryTenantStore` is the only implementation. The persistent contract, the id rules (`TenantIdRules`: 1-128 characters, ordinal and case-sensitive, never the reserved `default`), `ITenantStoreWriter`, `ITenantSecretProtector` for the connection string, `TenantStoreRegistration.Replace`, the `InstrumentedTenantStore` decorator and the behavior suite `TenantStoreBehaviorSuite` are **owned by the #596 plan**; this plan consumes them. The Redis store of #597 is the sibling that follows the same contract.
- `Directory.Packages.props` pins `Azure.Data.Tables` 12.13.0, `Azure.Identity` 1.21.0, `Testcontainers.Azurite` 4.15.0 and `Testcontainers.LocalStack` 4.15.0; it pins no Cosmos DB package, no `AWSSDK.DynamoDBv2` and no `Testcontainers.CosmosDb`. `tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs` (exposes `ConnectionString`, `TableEndpoint`) and `LocalStackFixture.cs` (image `localstack/localstack:4.14.0`, exposes `Endpoint`, collection `Cloud-LocalStack`) exist; there is no Cosmos DB fixture.
- No package holds an Azure Table, Cosmos DB or DynamoDB store yet. Open plans propose service-named packages for audit (#578 `Encina.AzureTableStorage`, #582 `Encina.AzureCosmosDB`, #579 `Encina.AmazonDynamoDB`) and dead letters (#588, #589), all as Dilemmas pending the maintainer; the Cosmos emulator question is the Phase 0 gate of #582.
- Consumers call the store on every scoped connection (`TenantConnectionFactory`, `TenantResolutionMiddleware`, `DefaultTenantProvider`); #711 plans a cache decorator for that.

**Provider category**: cloud, the Azure and AWS sides of the cloud triangle (`AGENTS.md` section 5); GCP is post-1.0 (SPEC-000 section 6.1, REQ-028), so the Firestore or Spanner equivalent is a known gap. Outside the 10-provider database matrix. **Affected packages**: `Encina.Tenancy.CosmosDB`, `Encina.Tenancy.AzureTableStorage`, `Encina.Tenancy.DynamoDB` (all new, names per Design Choice 1), `Encina` (EventId registry), `Directory.Packages.props`, `Encina.slnx`, test projects.

---

## Design Choices

<details>
<summary><strong>1. Package placement and names (Dilemma D1)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Three new packages `Encina.Tenancy.CosmosDB`, `Encina.Tenancy.AzureTableStorage`, `Encina.Tenancy.DynamoDB`, each with a `TenantStore` feature folder** | One package per service and per feature: pay-for-what-you-use; consistent with `Encina.Tenancy.AspNetCore` and `Encina.Tenancy.Redis` (#597); no coupling to the audit and dead letter plans whose package names are still open | Up to two packages per cloud service if the audit and dead letter stores ship (each reuses an SDK client that is already in DI, so there is no second client) |
| **B) The service-named packages of the audit plans** (`Encina.AzureCosmosDB`, `Encina.AzureTableStorage`, `Encina.AmazonDynamoDB`) with a `TenantStore/` folder (`AGENTS.md` section 4: the folder is named after the feature) | One package and one client registration per service | Each of those names is itself a Dilemma pending the maintainer (#578, #579, #582, #588, #589); every user of an audit store takes a `Encina.Tenancy` reference |
| **C) One package `Encina.Tenancy.Cloud` with the three SDKs** | One package | Every user takes all three SDK trees; against pay-for-what-you-use |
| **D) The issue's `Encina.MultiTenancy.*` names** | None | The tenancy packages are `Encina.Tenancy.*` |

### Chosen Option: **A — three `Encina.Tenancy.*` packages** (recommended, pending the maintainer)

### Rationale

- A wins when each backend should be installable alone and the tenancy family keeps one prefix; B wins if the maintainer decides one package per cloud service for every feature (answering the open names of #578, #579 and #582 the same way). Switching from A to B later is a package merge before 1.0.
- The registration reuses an `CosmosClient`, `TableServiceClient` or `IAmazonDynamoDB` that the application or another package already registered, and creates one from the options only when none is registered.

</details>

<details>
<summary><strong>2. Cosmos DB: partition key and item id (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Partition key `/tenantId` with the raw tenant id; item `id` = base64url of the UTF-8 tenant id** | A lookup is a point read (1 RU); the item id obeys Cosmos' forbidden characters (`/`, `\`, `?`, `#`) and is injective; the partition key value is the raw id, which Cosmos allows | `GetAllTenantsAsync` is a cross-partition query (fan-out), acceptable for an administrative call |
| **B) One constant partition key (`/catalog` = `tenants`)** | `GetAllTenantsAsync` and a batch are single-partition | Every lookup on the hottest path lands on one logical partition (10,000 RU/s ceiling); a catalog read on every request cannot scale |
| **C) The issue's `ReadItemAsync(tenantId, new PartitionKey(tenantId))`** | As written | The raw id as the item id fails for ids with the forbidden characters (the id rules of #596 allow them) |

### Chosen Option: **A — partition `/tenantId`, encoded item id** (recommended, pending the maintainer)

### Rationale

- A wins because lookups dominate and a catalog is small; B wins only if the catalog is listed far more often than looked up, which no consumer does (#4182 retention lists it once per cycle). Create is `CreateItemAsync` (HTTP 409 maps to `Left(tenancy.tenant_exists)`); update is `PatchItemAsync` setting `name`, `strategy`, `connectionString`, `schemaName` (JSON null when cleared, see Design Choice 3) and `updatedAtUtc`, which preserves `createdAtUtc` without a read and answers 404 as `Left(tenancy.tenant_not_found)`; delete maps 404 to the same code. A missing item on a read returns `null`, every other status throws (fail closed).

</details>

<details>
<summary><strong>3. Azure Table Storage and DynamoDB: key layouts and listing (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Table: `PartitionKey = tenants`, `RowKey = base64url(tenantId)`; DynamoDB: single-table `pk = TENANT#{tenantId}`, `sk = METADATA`, strongly consistent reads for lookups, listing through a sparse GSI (`gsi1pk = TENANTS` set only on tenant items)** | Row keys obey the forbidden characters and the 1 KiB limit for any id the rules of #596 admit, not only ASCII: an id is at most 128 UTF-16 characters, so at most 384 UTF-8 bytes (3 bytes per character in the worst case, a surrogate pair is 2 characters for 4 bytes), which base64url encodes to at most 512 ASCII characters, inside the Table row key (1 KiB), the Cosmos item id (1,023 characters) and the DynamoDB partition key (2,048 bytes for `TENANT#` plus the id; an ASCII id encodes to at most 171); the store still checks the encoded length against the service limit before any I/O and returns `Left(tenancy.tenant_id_invalid)` (`InvalidRequest`) when it is exceeded; the `TENANT#` prefix plus the whole id is injective; a lookup is a point read; the sparse GSI lists only tenants without a scan | Table: one partition per catalog (2,000 entities/s ceiling per partition, fine behind the #711 cache); DynamoDB: the GSI is eventually consistent, so a tenant created a moment ago may be missing from a list |
| **B) Table: partition by the first character of the encoded id; DynamoDB: `Scan` with a filter** | Spreads Table load; no GSI | Table: listing is a multi-partition query and a lookup needs the computed partition; DynamoDB: a scan reads the whole table, which also holds non-tenant data in a single-table design |
| **C) DynamoDB with a strongly consistent listing through a base-table partition (`pk = TENANTS`, one item per tenant)** | Consistent listing | A second item per tenant to keep in step on every write, so create and delete need a transaction |

### Chosen Option: **A — key layouts as listed** (recommended, pending the maintainer)

### Rationale

- A wins when lookups are the hot path and listing is administrative; C wins if the retention service (#4182) must see a tenant in the same second it was created. Lookups use `ConsistentRead = true` on DynamoDB so a deleted or changed tenant is never read from a stale replica. A listing that fails midway (a page error) throws and never returns a shorter list as complete.
- Table create is `AddEntityAsync` (409 is `tenant_exists`), update is `UpdateEntityAsync` in `UpdateMode.Merge` with `ETag.All` (preserves `createdAtUtc` without a read; 404 is `tenant_not_found`), delete is `DeleteEntityAsync`. DynamoDB create is `PutItem` with `attribute_not_exists(pk)`, update is `UpdateItem` with `attribute_exists(pk)`, delete is `DeleteItem` with `attribute_exists(pk)`; a `ConditionalCheckFailedException` maps to the matching `Left`.
- Clearing a field on update (a tenant moves from `DatabasePerTenant` to `SharedSchema`: connection string and schema name become null) must really clear it, or the old protected credential and schema stay in the record. Per provider: **Cosmos** `PatchItemAsync` uses `PatchOperation.Set` with a JSON null (or `Remove`) for each cleared field, which Cosmos applies; **Azure Table** `Merge` omits null properties from the payload and so would keep the old value, so the writer sends an explicit empty-string marker for a cleared field (`connectionString = ""`, `schemaName = ""`) and the mapper reads an empty string as null (a protected connection string is never empty, it carries the `tp1:` marker of #596; a schema name is never empty by `TenantInfo`); **DynamoDB** `UpdateItem` builds the update expression per call: `SET` for present fields and a `REMOVE` clause for each null field. The `TenantStoreBehaviorSuite` of #596 gets the case "update from `DatabasePerTenant` to `SharedSchema` returns a tenant with null connection string and schema name", which every store runs and which is the confirming test for the Table `Merge` behavior (documented, not run when this plan was written).

</details>

<details>
<summary><strong>4. Cosmos DB emulator gate (Dilemma D4)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Plan the Cosmos store behind the emulator evidence of #582 Phase 0 (start-up reliability, point reads, patch, cross-partition query on the Linux emulator in CI)** | One piece of evidence for every Cosmos store; consistent with SPEC-000 section 6.1 | The Cosmos pull requests wait for that evidence |
| **B) Plan Cosmos independently with its own Phase 0** | No wait | Two emulator evaluations of the same container |
| **C) Drop Cosmos from #598 and keep Table and DynamoDB** | Removes the heaviest CI dependency | The issue's first store; SPEC-000 names #598 as in 1.0 on the strength of emulators |

### Chosen Option: **A — reuse the #582 gate** (recommended, pending the maintainer)

### Rationale

- A wins on cost; B wins if #582 is deferred or fails its gate for a reason that does not affect point reads. If the gate fails, the Cosmos store returns to the maintainer with the failing criterion, as #582 states; Table and DynamoDB proceed.

</details>

<details>
<summary><strong>5. Client creation, authentication and resource creation (Dilemma D5)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A registered client wins; otherwise the options build one (Cosmos endpoint plus `TokenCredential`, Table `ServiceUri` plus `TokenCredential`, DynamoDB region or service URL plus the default credential chain); the container or table is created only when `EnsureCreated` is set (default `false`)** | Works with managed identity and least privilege; no create permission needed at runtime; nothing created by surprise | The operator provisions the container or table (the README shows the settings) |
| **B) Always create on first use** | Convenient | Needs create permission at runtime; hides a mis-pointed endpoint behind a new empty catalog, which then reads every tenant as unknown |
| **C) Connection string only** | Simplest | Shared-key secrets in configuration; no managed identity |

### Chosen Option: **A — registered client or options, creation opt-in** (recommended, pending the maintainer)

### Rationale

- A wins in production; B wins for a developer's first run, which `EnsureCreated = true` covers. The same shape as #578 and #579. An options class with a credential marks the property `[JsonIgnore]` and overrides `ToString()` (`AGENTS.md` section 3). The tenant's own connection string is protected through the #596 `ITenantSecretProtector` before it is written.

</details>

<details>
<summary><strong>6. Writes never overwrite and errors map to the shared codes</strong></summary>

**Class:** Settled
**Settled by:** ADR-062 (categories: `tenancy.tenant_exists` AlreadyExists, `tenancy.tenant_not_found` NotFound) and the #596 contract (`ITenantStoreWriter` returns `Either`; `AGENTS.md` section 3, railway oriented programming)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Conditional writes: create fails on an existing id, update and delete fail on a missing id, each as a coded `Left`; infrastructure failures throw (fail closed)** | The same observable behavior as the other nine stores and the behavior suite; no silent overwrite of another tenant's record | A conditional write costs the same as an unconditional one on all three services |
| **B) Upsert semantics** (the issue's `PutItem`, `UpsertEntity`) | Fewer branches | Create overwrites a tenant that exists, which changes its connection string silently |

### Chosen Option: **A — conditional writes**

### Rationale

- Only A satisfies the contract that every store runs through `TenantStoreBehaviorSuite`. Retries of transient failures are the SDKs' own (Design Choice 5 options expose `MaxRetryAttempts` and the retry mode); what remains after them throws.

</details>

---

## Implementation Phases

### Phase 1: Cosmos DB package skeleton and mapping (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.Tenancy.CosmosDB/Encina.Tenancy.CosmosDB.csproj` (new), `Directory.Packages.props` (edit: `Microsoft.Azure.Cosmos` pin at the version current when the pull request is cut, no NU190x audit warning), `Encina.slnx` (edit).
2. `TenantStoreCosmosDBOptions.cs` (new; endpoint, `TokenCredential`, database and container names, `EnsureCreated`, retry settings; `[JsonIgnore]` and `ToString()` on any secret), `Internal/TenantDocument.cs` (document and mapper, base64url item id).
3. Tests: `TenantDocumentTests` (round trip, ids with `/`, `#`, `?`, `\`, 128-character ids), options guard tests; `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.Tenancy.CosmosDB.json`; ProjectReference edits in `tests/Encina.UnitTests` and `tests/Encina.GuardTests` (2 csproj; the integration project reference comes with its tests).

Files: 12.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- #596 owns the tenant store contract and the id rules; this phase starts the Cosmos DB package.
- Design Choice 2 fixes the partition key and the item id encoding.

TASK:
Create Encina.Tenancy.CosmosDB with options and the document mapper, and their tests.

KEY RULES:
- Item id = base64url(UTF-8 tenant id); partition key value = the raw tenant id.
- Options with a credential: [JsonIgnore] on the property and a ToString() override that masks it.
- Pin Microsoft.Azure.Cosmos centrally; the restore must show no audit warning.
- XML docs, PublicAPI files, coverage manifest with numeric per-flag targets.

REFERENCE FILES:
- src/Encina.Tenancy/Abstractions/TenantInfo.cs
- docs/plans/cosmosdb-entity-change-audit-store-implementation-plan-582.md (by issue number: #582)
```

</details>

---

### Phase 2: Cosmos DB store, registration and EventIds (pull requests 2 and 3)

<details>
<summary>Tasks</summary>

1. PR 2: `TenantStoreCosmosDB.cs` (new; implements `ITenantStore` and `ITenantStoreWriter`; constructor `(Container container, ITenantSecretProtector? protector = null, TimeProvider? timeProvider = null)`; point reads, `PatchItemAsync`, cross-partition `GetItemQueryIterator` for listing, every call with the `CancellationToken`), `ServiceCollectionExtensions.cs` (new; `AddEncinaTenantStoreCosmosDB`, through `TenantStoreRegistration.Replace<TenantStoreCosmosDB>(services, provider: "cosmosdb")` of #596, wrapped by `InstrumentedTenantStore`; it also registers the health check below), `Health/TenantStoreCosmosDBHealthCheck.cs` (new; read of the container properties, status code only), unit tests with a substituted `Container`, health check unit tests, registration test (`ValidateOnBuild`, `ValidateScopes`, both orders, health check resolved), guard tests, package `PublicAPI.Unshipped.txt`, manifest. Files: 9 (store, extensions, health check, 4 test files, PublicAPI, manifest).
2. PR 3: `Diagnostics/TenantStoreCosmosDBLog.cs` (new), `EventIdRanges.cs` (edit: `TenancyCosmosDB = (80550, 80599)`), `src/Encina/PublicAPI.Unshipped.txt` (edit), `docs/engineering/eventid-block-registry.md` (edit), `EncinaEventIdAllocationTests.cs` (edit: `AssemblyRanges`), emulator fixture `tests/Encina.TestInfrastructure/Fixtures/CosmosDbFixture.cs` (new, gateway mode, shared collection; reused if #582 created it first) and its `[CollectionDefinition]` in `tests/Encina.IntegrationTests` (new: each test assembly declares its own, as for `AzuriteCollection`), `TenantStoreCosmosDBIntegrationTests.cs` (derives from `TenantStoreBehaviorSuite`), log unit tests, package `PublicAPI.Unshipped.txt`, manifest, `Encina.IntegrationTests.csproj` (edit), `Directory.Packages.props` (edit: `Testcontainers.CosmosDb` pin). Files: 13.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Phase 1 shipped the package, options and mapper.
- Writes are conditional (Design Choice 6); a missing item on a read is null; every other status throws.

TASK:
Implement TenantStoreCosmosDB, its registration and health check, then its diagnostics, emulator fixture and integration tests.

KEY RULES:
- CreateItemAsync 409 to Left(tenancy.tenant_exists); PatchItemAsync or DeleteItemAsync 404 to Left(tenancy.tenant_not_found).
- Register the EventId range in EventIdRanges.cs before using an id; ids packed from 80550; map the assembly in AssemblyRanges.
- TimeProvider for timestamps; connection string protected before the write; no EncinaError.Message or tenant id in tags or logs.
- Shared [Collection] emulator fixture; never a per-class container.

REFERENCE FILES:
- src/Encina.Tenancy/Stores/InMemoryTenantStore.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs
```

</details>

---

### Phase 3: Azure Table Storage store (pull requests 4, 5 and 6)

<details>
<summary>Tasks</summary>

1. PR 4 (skeleton): `src/Encina.Tenancy.AzureTableStorage/*.csproj` (new), `Encina.slnx` (edit), options, `Internal/TenantEntity.cs` (row key encoding), PublicAPI x2, manifest, 2 test csproj edits, unit and guard tests. Files: 11. `Azure.Data.Tables` is already pinned.
2. PR 5 (store): `TenantStoreAzureTable.cs`, `ServiceCollectionExtensions.cs` (`AddEncinaTenantStoreAzureTable`, provider tag `azuretable`, which also registers the health check), the health check (`Health/TenantStoreAzureTableHealthCheck.cs`), unit tests with a substituted `TableClient`, health check tests, registration test, guard tests, PublicAPI, manifest. Files: 9.
3. PR 6 (diagnostics and integration): log class, `EventIdRanges.cs` (`TenancyAzureTable = (80600, 80649)`), `src/Encina/PublicAPI.Unshipped.txt`, registry, `AssemblyRanges`, `TenantStoreAzureTableIntegrationTests` on the `Cloud-Azurite` collection (its `[CollectionDefinition]` already exists as `AzuriteCollection` in `tests/Encina.IntegrationTests`; the class deletes its own table in `InitializeAsync`), log unit tests, package PublicAPI, manifest, `Encina.IntegrationTests.csproj` edit. Files: 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Cosmos DB shipped the pattern; Table Storage uses PartitionKey "tenants" and RowKey = base64url(tenant id).

TASK:
Implement the Azure Table store in three pull requests: skeleton, store and registration, diagnostics and integration tests.

KEY RULES:
- AddEntityAsync 409 to tenant_exists; UpdateEntityAsync in Merge mode with ETag.All, 404 to tenant_not_found, a cleared field written as an empty string that the mapper reads as null (Merge never clears a null); DeleteEntityAsync 404 to tenant_not_found.
- Listing reads every page of the partition; a failed page throws, never a shorter list.
- The integration class deletes its table in InitializeAsync and calls the fixture's cleanup; never disposes the fixture.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs
- docs/plans/azure-table-entity-change-audit-store-implementation-plan-578.md (by issue number: #578)
```

</details>

---

### Phase 4: DynamoDB store (pull requests 7, 8 and 9)

<details>
<summary>Tasks</summary>

1. PR 7 (skeleton): `src/Encina.Tenancy.DynamoDB/*.csproj` (new), `Directory.Packages.props` (edit: `AWSSDK.DynamoDBv2` on the AWSSDK 4 line, shared with #579, #589 and #210: whichever lands first adds it), `Encina.slnx`, options, `Internal/TenantItem.cs` (attribute mapping, `TENANT#` key), PublicAPI x2, manifest, 2 test csproj edits, unit and guard tests. Files: 12.
2. PR 8 (store): `TenantStoreDynamoDB.cs` (low-level `IAmazonDynamoDB`; conditional expressions of Design Choice 3; `ConsistentRead = true` on lookups; paged GSI query for listing), registration (`AddEncinaTenantStoreDynamoDB`, provider tag `dynamodb`, which also registers the health check), `Health/TenantStoreDynamoDBHealthCheck.cs`, unit tests with a substituted client, health check tests, registration test, guard tests, PublicAPI, manifest. Files: 9. The `UpdateItem` expression is built per call with a `REMOVE` clause for each null field (Design Choice 3).
3. PR 9 (diagnostics and integration): log class, `EventIdRanges.cs` (`TenancyDynamoDB = (80650, 80699)`), `src/Encina/PublicAPI.Unshipped.txt`, registry, `AssemblyRanges`, `TenantStoreDynamoDBIntegrationTests` on `Cloud-LocalStack` (its `[CollectionDefinition]` already exists in `tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/LocalStackCollection.cs`; the class creates the table and the GSI and deletes it in `InitializeAsync`, because `LocalStackFixture.ClearAllDataAsync` clears SQS and SNS only), log unit tests, package PublicAPI, manifest, `Encina.IntegrationTests.csproj` edit. Files: 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- Single-table design: pk = TENANT#{id}, sk = METADATA, sparse GSI on gsi1pk = TENANTS.

TASK:
Implement the DynamoDB store in three pull requests: skeleton, store and registration, diagnostics and integration tests.

KEY RULES:
- PutItem with attribute_not_exists(pk); UpdateItem and DeleteItem with attribute_exists(pk); ConditionalCheckFailedException maps to the matching Left; UpdateItem uses REMOVE for every null field so a cleared connection string is deleted.
- Lookups use ConsistentRead; listing pages the GSI and a failed page throws.
- The unit tests substitute IAmazonDynamoDB; the integration tests build the client on LocalStackFixture.Endpoint.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs
- docs/plans/dynamodb-entity-change-audit-store-implementation-plan-579.md (by issue number: #579)
```

</details>

---

### Phase 5: Cross-cutting integration and observability

<details>
<summary>Tasks</summary>

1. All three stores are wrapped by the `InstrumentedTenantStore` of #596 (spans, `encina.tenancy.store.operations`, `encina.tenancy.store.duration` in seconds, tag `provider` set by the `provider` argument of `TenantStoreRegistration.Replace`; the names have no `_total` suffix because ADR-054 decision 4 binds new instruments, and #596 explains why the older `encina.tenancy.resolutions_total` keeps its name); the store-specific logs are the ids of Event ID Allocation; the record's tenant id and `EncinaError.Message` never reach a tag or a log.
2. The contract test for all three stores is the `TenantStoreBehaviorSuite` (create, duplicate, update, missing update, delete, missing delete, exists, list, ordinal ids, reserved id, protector round trip).
3. Integration of the health checks: each package registers its check under `DefaultName` (`encina-tenant-store-cosmosdb`, `encina-tenant-store-azuretable`, `encina-tenant-store-dynamodb`) with static `Tags`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- The three stores and their registrations exist.

TASK:
Verify the cross-cutting integration: decorator wiring, health checks, log ids, behavior suite on each emulator.

KEY RULES:
- No tenant id of a record and no EncinaError.Message in tags, spans or logs; exception type only.
- Health checks resolve through IServiceProvider.CreateScope() where scoped services are needed.

REFERENCE FILES:
- src/Encina.Tenancy/Diagnostics/TenancyLog.cs
- docs/plans/tenant-store-persistent-implementation-plan-596.md (by issue number: #596)
```

</details>

---

### Phase 6: Documentation and finalization (pull request 10)

<details>
<summary>Tasks</summary>

1. By `docs-writer`: one README per package (3 files), the cloud section of the tenancy feature page, `docs/INVENTORY.md` (edit); the pages state the provisioning each store needs (container, table, GSI), the roles or policies for least privilege, the DynamoDB listing consistency and the GCP gap.
2. `changelog.d/598-tenant-store-cloud.added.md` (in this pull request: it completes the feature, and every store pull request stays under 15 files).
3. Verification: `dotnet build --configuration Release` with 0 warnings; each flag against the Research targets.

Files: 6 (3 READMEs, the feature page edit, `docs/INVENTORY.md`, the changelog fragment).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- All three stores are merged and pass the behavior suite on their emulators.

TASK:
Write the three package READMEs and the tenancy page section; update INVENTORY.md.

KEY RULES:
- Coverage cited with covref markers; English only; the reference application is never named.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- src/Encina.Tenancy/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| SPEC-000 section 6.1, REQ-028 | Cloud stores with a free emulator are in 1.0 (#598 named); GCP is post-1.0 |
| SPEC-002 REQ-061 | Multi-tenancy in 1.0; reserved `default` tenant |
| ADR-062 | `tenancy.tenant_exists` AlreadyExists (defined by #596) and `tenancy.tenant_not_found` NotFound (already on `origin/main` as `TenancyErrors.TenantNotFoundCode`, reused); this plan adds no error code |
| ADR-054 D4 | Tenant tag only through #2676; the record's id is never a tag |
| Azure Table Storage key rules; Cosmos DB id rules; DynamoDB expressions | Forbidden characters, key limits, conditional writes |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `ITenantStoreWriter`, `ITenantSecretProtector`, `TenantIdRules`, `TenantStoreRegistration`, `InstrumentedTenantStore`, `TenantStoreBehaviorSuite` | `Encina.Tenancy`, `tests/Encina.TestInfrastructure` (#596) | Contract, registration, telemetry, tests |
| `AzuriteFixture` | `tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs` | Table Storage integration tests |
| `LocalStackFixture` | `tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs` | DynamoDB integration tests |
| Cosmos emulator fixture | created by #582 or by PR 3 here, whichever lands first | Cosmos integration tests |
| `Encina.Tenancy.Redis` | #597 | Sibling store of the same contract |

Per-file coverage targets (numeric, per flag):

| File | unit | guard | integration | Justification |
|------|-----:|------:|------------:|---------------|
| `TenantStoreCosmosDB.cs` | 90 | 10 | 85 | A substituted `Container` and `FeedIterator` reach every status mapping; the emulator covers patch, partition keys and cross-partition queries |
| `TenantStoreAzureTable.cs` | 90 | 10 | 90 | A substituted `TableClient` reaches each `Left` and the paging; Azurite covers merge updates and key rules |
| `TenantStoreDynamoDB.cs` | 90 | 10 | 90 | A substituted `IAmazonDynamoDB` reaches every conditional-failure branch; LocalStack covers expressions and the GSI |
| `Internal/TenantDocument.cs`, `Internal/TenantEntity.cs`, `Internal/TenantItem.cs` | 100 | 0 | 0 | Internal mappers: guard tests cannot reach them (`InternalsVisibleTo` excludes `Encina.GuardTests`); fully covered by unit tests |
| Options and `ServiceCollectionExtensions.cs` (x3) | 100 | 40 | 0 | Validation and DI proven with `ValidateOnBuild` |
| Health checks (x3) | 100 | 10 | 70 | Two outcomes each; the emulator covers the success path |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Tenancy.CosmosDB` | `TenancyCosmosDB = (80550, 80599)` | Cloud and integration zone; 80000-80549 are claimed or reserved by #4007 and the plans of #425, #387, #382, #391 and #388, so 80550 is the first free slot (checked on `origin/main` and the open `docs/plan-*` heads). Ids: 80550 container created, 80551 container creation failed, 80552 throttled by the service (status only), 80553 listing page failed; 80554-80599 free |
| `Encina.Tenancy.AzureTableStorage` | `TenancyAzureTable = (80600, 80649)` | 80600 table created, 80601 table creation failed, 80602 throttled, 80603 listing page failed; rest free |
| `Encina.Tenancy.DynamoDB` | `TenancyDynamoDB = (80650, 80699)` | 80650 table or index created, 80651 creation failed, 80652 throttled, 80653 listing page failed; rest free |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Cosmos DB: skeleton, store, diagnostics and integration (PRs 1-3) | 12 / 9 / 13 | Health check and its registration in PR 2; registry, `EventIdRanges.cs`, `AssemblyRanges`, `src/Encina/PublicAPI.Unshipped.txt`, the fixture, its `[CollectionDefinition]` and the pins counted in PR 3 |
| Azure Table: skeleton, store, diagnostics and integration (PRs 4-6) | 11 / 9 / 10 | |
| DynamoDB: skeleton, store, diagnostics and integration (PRs 7-9) | 12 / 9 / 10 | The `AWSSDK.DynamoDBv2` pin is in PR 7 |
| Documentation (PR 10) | 6 | `docs-writer` for the pages; the changelog fragment is in this pull request |

Every pull request is at most 15 files and passes alone; PR 2 depends on PR 1, PR 3 on PR 2, and the same within each store. All depend on the contract of #596.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is pre-1.0, .NET 10, C# 14; ROP with Either<EncinaError, T>.
- #596 owns the tenant store contract; #597 is the Redis sibling; this plan adds three cloud stores, each in its own Encina.Tenancy.* package.

IMPLEMENTATION OVERVIEW:
1. Per store: skeleton (package, options, mapper), then store and registration, then diagnostics, health check, emulator integration tests.
2. Cosmos DB: partition /tenantId, item id base64url, patch for update. Azure Table: PartitionKey tenants, RowKey base64url, merge update. DynamoDB: single table, pk TENANT#{id}, sparse GSI for listing, consistent lookups.
3. EventId ranges 80550-80699, registered in EventIdRanges.cs in the pull request that first uses an id.
4. READMEs and the feature page by docs-writer.

KEY PATTERNS:
- Conditional writes: create never overwrites, update and delete never create; coded Left for exists and not found; infrastructure failures throw.
- Tenant ids are ordinal and case-sensitive; the reserved id default is rejected by TenantIdRules; keys are injective.
- A registered SDK client wins; creation of containers and tables is opt-in; options with credentials are [JsonIgnore] and masked in ToString().
- No tenant id of a record and no EncinaError.Message in tags or logs.

REFERENCE FILES:
- src/Encina.Tenancy/Abstractions/ITenantStore.cs
- tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs
- tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Deferred to #711 (open): `CachedTenantStore` decorates any store; the issue's per-store cache is that decorator, written once |
| 2 | OpenTelemetry | ✅ | `InstrumentedTenantStore` (#596) with provider tags `cosmosdb`, `azuretable`, `dynamodb`; no record tenant id as a tag |
| 3 | Structured Logging | ✅ | Per-package logs in 80550-80699; exception type and status only |
| 4 | Health Checks | ✅ | One check per package; status codes only, no endpoint secrets in the result |
| 5 | Validation | ✅ | `TenantIdRules` on every write; options validated at startup |
| 6 | Resilience | ✅ | The SDKs' retry policies are configured through the options (`MaxRetryAttempts`, retry mode); no Polly layer is added on top, so an outage still reaches the health check |
| 7 | Distributed Locks | ❌ | Conditional single-item writes are atomic on all three services |
| 8 | Transactions | ❌ | One item per tenant; no multi-item write exists once `tenant_features` is out of scope (#596 Design Choice 4) |
| 9 | Idempotency | ✅ | A second create is `Left(tenancy.tenant_exists)`; no silent overwrite |
| 10 | Multi-Tenancy | ✅ | This is the tenant catalog: global by design; `default` reserved; keys injective |
| 11 | Module Isolation | ❌ | The catalog is cross-module by design |
| 12 | Audit Trail | ⏭️ | Deferred to #752 (open): tenant lifecycle audit sits in the administration service, written once for every store, not per backend |

---

## Prerequisites & Dependencies

- **Hard prerequisite**: PR 1 and PR 2 of #596 (contract, registration helper, decorator, behavior suite). The skeleton pull requests (PR 1, 4, 7) use none of them and can start earlier.
- Cosmos DB: the emulator evidence of #582 Phase 0 (Design Choice 4); if it fails, PRs 1-3 return to the maintainer.
- SDK pins: `Microsoft.Azure.Cosmos` and `Testcontainers.CosmosDb` (shared with #582 and #588), `AWSSDK.DynamoDBv2` (shared with #579, #589 and #210): the first pull request to land adds the pin and the others rebase.
- The package-name Dilemmas of #578, #579 and #582 and Design Choice 1 here should be answered together.

---

## Next Steps

1. The maintainer answers Dilemmas D1-D5 when work on the issue starts, D1 together with the package-name Dilemmas of #578, #579 and #582, and D4 together with the Phase 0 outcome of #582.
2. After #596's contract merges, start with the store whose emulator already exists (Azure Table on Azurite, then DynamoDB on LocalStack), and report the per-file measurements against the targets above.
3. Register each `Tenancy*` EventId range in `EventIdRanges.cs` in the pull request that first uses an id.
