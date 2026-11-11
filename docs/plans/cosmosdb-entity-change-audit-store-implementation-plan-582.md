# Implementation Plan: `EntityChangeAuditStoreCosmosDB` — Azure Cosmos DB backend for the entity-change audit store

> **Issue**: [#582](https://github.com/dlrivada/Encina/issues/582)
> **Type**: Feature
> **Complexity**: Medium (6 phases, 1 provider, a new package, ~14 source files + tests)
> **Estimated Scope**: ~900-1,200 lines of production code + ~1,500-2,000 lines of tests

---

## Summary

Implement the entity-change audit store (today `IAuditLogStore`, becoming `IEntityChangeAuditStore` under ADR-036) for Azure Cosmos DB (NoSQL API) as `EntityChangeAuditStoreCosmosDB`: globally distributed audit history of "what did this entity look like before and after each change", queried by entity, with a pull-model change feed for real-time consumers. The store is built on the **ADR-036 contract** (`Either` results, `TimeProvider`, `TenantId`, `ModuleId`, `PurgeEntriesAsync`, `ChangedAtUtc` as `DateTimeOffset`, a redaction hook in the writer), not on the samples of the issue, which keep `Task` returns, `DateTime.UtcNow`-style stamping, an unused `_jsonOptions` and an unbounded list.

What the code looks like today (main `7a0049c07`):

- `IAuditLogStore` still lives in `src/Encina.DomainModeling/Auditing/IAuditLogStore.cs` (`Task LogAsync(AuditLogEntry, CancellationToken)`, `Task<IEnumerable<AuditLogEntry>> GetHistoryAsync(string entityType, string entityId, CancellationToken)`); `AuditLogEntry` (`Id`, `EntityType`, `EntityId`, `Action`, `UserId`, `TimestampUtc` as `DateTime`, `OldValues`, `NewValues`, `CorrelationId`) and `AuditAction` sit next to it, with `InMemoryAuditLogStore`. ADR-036 (accepted 2026-10-03) renames and reshapes all of it; that work is **#1778, open**. This plan depends on it (see Prerequisites): until #1778 lands, the names and members below are the ADR's, not main's.
- There is no Cosmos DB code, no `Microsoft.Azure.Cosmos` and no `Testcontainers.CosmosDb` in `Directory.Packages.props`, and no Cosmos service in `docker-compose.yml` (the `cloud` profile has `localstack` and `azurite`). `Azure.Data.Tables` 12.13.0 is pinned (for #578). No `Encina.Azure*` package holds a store yet; the existing ones are `Encina.AzureServiceBus`, `Encina.AzureFunctions`, `Encina.Messaging.Encryption.AzureKeyVault` and `Encina.Security.Secrets.AzureKeyVault`.
- SPEC-000 section 6.1 puts cloud-backed stores that run against a local emulator in 1.0 (for example #578, #579, #588, #589); #582 is not named there, and its milestone is v0.14.0. Whether the Cosmos DB emulator is reliable enough in CI decides whether this issue stays in 1.0 (Design Choice 4).

**Provider category**: cloud audit store, outside the 10-provider database matrix; an additional backend of the entity-change interface (database providers are #574, closed, and #1778). **Affected packages**: `Encina.AzureCosmosDB` (new; name per Design Choice 3), `Encina.Security.Audit` (the contract, from #1778), test projects, `Directory.Packages.props`.

---

## Design Choices

<details>
<summary><strong>1. Partition key (Dilemma D1)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `entityType:entityId`** (the original) | History of one entity is one partition, the cheapest query; writes spread well | Tenant-wide or user-wide queries fan out across partitions; tenant isolation is only a filter |
| **B) Hierarchical partition key: `tenantId`, then `entityKey` (`entityType:entityId`)** | The entity history query supplies both levels and stays in one partition; a tenant-wide query targets that tenant's partitions only; a very large tenant is still spread by the second level (no hot partition); isolation and per-tenant cost follow the first level (SPEC-002 REQ-061) | Needs hierarchical partition keys (a recent SDK and service feature, to be confirmed on the emulator in Phase 0); entries with no tenant use a fixed sentinel value |
| **C) A synthetic key combining both in one string** | Works on any SDK | Cannot be queried by tenant alone without a fan-out; complexity of building and parsing the key |

### Chosen Option: **B — hierarchical key `tenantId` then `entityKey`** (recommended, pending the maintainer)

### Rationale

- The main query is the history of one entity, which B keeps single-partition; the multi-tenancy cross-cutting function (ADR-018) is served by the first level without giving up the entity query.
- A wins when the emulator or the SDK in use cannot do hierarchical keys, or tenancy is not used (every entry would share one sentinel first level, so B degrades to A). C wins only when both single-partition lookups must work on an old SDK.

</details>

<details>
<summary><strong>2. How retention is applied (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Container `defaultTtl` / per-document `ttl`** | Automatic, no request units spent on deletes, no code | Deletion is not an audited act, cannot differ per tenant, and bypasses `PurgeEntriesAsync` and the `EntityChangeAuditRetentionService` of ADR-036 |
| **B) `EntityChangeAuditRetentionService` calling `PurgeEntriesAsync`, as for every other provider** | Consistent across providers (`AGENTS.md` section 5: providers differ only in implementation); per-tenant and logged retention (SPEC-002 REQ-007); the purge is an explicit act | Request units are spent on deletes |
| **C) B for the contract, plus a document `ttl` as a safety net at 2x the retention** | Cost bounded if the service stops running | Two mechanisms to explain and test |

### Chosen Option: **B — retention through `PurgeEntriesAsync`** (recommended, pending the maintainer)

### Rationale

- The interface requires `PurgeEntriesAsync` regardless (ADR-036 decision 1), and the retention service is core; making Cosmos the one provider that expires differently would break coherence and per-tenant retention.
- A wins when one global retention is enough and delete cost matters; C wins when an unattended store must never grow without bound. The container is created without a default TTL (`ttl` unset) unless `RetentionTtlBackstop` is set, so C is a configuration, not a code branch.

</details>

<details>
<summary><strong>3. Package name and shape (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Azure.CosmosDB`** (the issue) | Groups Azure data packages under a prefix | Introduces a dotted `Azure` namespace nobody else uses |
| **B) `Encina.AzureCosmosDB`**, store in an `EntityChangeAudit/` folder | Follows the existing `Encina.AzureServiceBus` and `Encina.AzureFunctions` names; the feature folder rule (`AGENTS.md` section 4) keeps room for later Cosmos features | A package named after the service, not after audit |
| **C) `Encina.Security.Audit.CosmosDB`** | Follows the audit satellite pattern (`Encina.Audit.Marten`) | Cosmos DB features other than audit would need another package |

### Chosen Option: **B — `Encina.AzureCosmosDB`** (recommended, pending the maintainer)

### Rationale

- Table Storage (#578) uses a different SDK (`Azure.Data.Tables`), so the two Azure audit stores are separate packages either way; B only aligns the naming. C wins if the maintainer wants every audit backend discoverable under `Encina.Security.Audit.*`.

</details>

<details>
<summary><strong>4. Stay in 1.0 depending on the emulator (Dilemma D4)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep #582 in 1.0, gated by a Phase 0 emulator check** (container starts reliably in CI; hierarchical keys, transactional batch, TTL, paged queries and the change feed pull model work) | Consistent with SPEC-000 section 6.1 (cloud stores with a free local emulator are in 1.0); tests are real | CI time and possible flakiness of the emulator image |
| **B) Move #582 to post-1.0 now** | No CI risk before 1.0 | Contradicts the widened scope of SPEC-000 DEC-009 for no evidence yet |

### Chosen Option: **A — keep it, gated by Phase 0** (recommended, pending the maintainer)

### Rationale

- SPEC-000 section 6 is the complete list of exclusions and "cloud services without a free local emulator" is one; this plan does not decide whether the Cosmos emulator qualifies, it builds the evidence (Phase 0) for the maintainer. If Phase 0 fails, the issue returns to the maintainer with the failing criterion.
- B wins if the maintainer prefers to cut risk before 1.0 without waiting for the evidence.

</details>

<details>
<summary><strong>5. Change feed model (Dilemma D5)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pull model: `IEntityChangeAuditChangeFeed.ReadChangesAsync(continuation, ct)` over `GetChangeFeedIterator`; the caller stores the continuation token** | No lease container, no background service, pay-for-what-you-use; works with the emulator | The consumer owns progress and parallelism |
| **B) Change feed processor (push) with a lease container** | Distributes work across consumers, tracks progress | A second container, a hosted service and lease management inside the library; harder to test |

### Chosen Option: **A — pull model** (recommended, pending the maintainer)

### Rationale

- The issue calls the change feed optional. A gives the capability with the least machinery and no background loop to fail; B wins when several consumer instances must share one feed with automatic rebalancing.
- The interface lives in `Encina.AzureCosmosDB` (a capability only this provider has); returned entries are the ADR-036 entry type, results are `Either`, and the continuation token is an opaque string.

</details>

<details>
<summary><strong>6. Contract, naming and idempotent writes</strong></summary>

**Class:** Settled
**Settled by:** ADR-036 (decisions 1 and 7, naming tables), ADR-001, ADR-006, AGENTS.md section 3 (TimeProvider; fail closed on a failed write; secrets in options), AGENTS.md section 4 (`{Pattern}Store{Provider}`), AGENTS.md section 6 (idempotency, function 9)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Build on the ADR-036 contract: class `EntityChangeAuditStoreCosmosDB`, flag `UseEntityChangeAuditStore`, `EntityChangeAuditOptions`, `Either` results, `TimeProvider`, `TenantId`, `ModuleId`; the document `id` is the entry `Id`, so `CreateItemAsync` with a conflict (HTTP 409) means "already written" and is a success, not a duplicate row; a failed write is a `Left` that fails the unit of work; an entry over the 2 MB item limit is rejected with a typed `Left`, never truncated** | Provider coherence; safe retries; fail-closed auditing | Depends on #1778 landing first |
| **B) Keep the old `IAuditLogStore` shape** | Buildable today | Compatibility layer and a contract ADR-036 removes (no compatibility, ADR-036 decision 7) |

### Chosen Option: **A — the ADR-036 contract**

### Rationale

- ADR-036 says the follow-up issues apply the names and do not revisit them. The Cosmos client connection string/key lives in options marked `[JsonIgnore]` with `ToString()` overridden. The Cosmos SDK retries throttled requests (HTTP 429) itself; the retry counts and wait are exposed in the options.
- Tests: unit tests mock the container; integration tests run against the Cosmos DB emulator through Testcontainers in a shared `[Collection]` fixture (never a per-class container).

</details>

---

## Implementation Phases

### Phase 0: Emulator and SDK verification (gate for the rest)

> **Goal**: the evidence for Design Choices 1, 4 and 5 before any production code.

<details>
<summary>Tasks</summary>

1. Add `Microsoft.Azure.Cosmos` and `Testcontainers.CosmosDb` to `Directory.Packages.props` (versions to the latest stable at that moment, aligned with the `Testcontainers` 4.15.0 line) and an `EmulatorFixture` prototype.
2. Check against the emulator container, with timings in CI: start-up time and reliability over 10 runs; create database and container with a hierarchical partition key; create/read/query/delete items; `ttl` property; paged query with continuation token; transactional batch within a partition; `GetChangeFeedIterator` from the beginning; item size limit behavior.
3. Record the result in the pull request (a table, pass or fail per criterion). A failed criterion goes back to the maintainer as an update to Dilemma D4 (or D1, D5), never decided by the worker.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 0</strong></summary>

```
CONTEXT:
- Issue #582: Azure Cosmos DB (NoSQL API) backend of the entity-change audit store (ADR-036). No Cosmos code exists on main.

TASK:
Run the emulator and SDK verification of the Phase 0 Tasks and report a pass/fail table.

KEY RULES:
- No production code in this phase; only the package pins and a throwaway fixture prototype.
- Report, never decide: a failing criterion returns to the maintainer.
- Container tests use a shared [Collection] fixture shape (AGENTS.md section 9).

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs
- tests/Encina.TestInfrastructure/Fixtures/KafkaFixture.cs
- docs/specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md (section 6)
```

</details>

---

### Phase 1: Package, document model and container initialization

<details>
<summary>Tasks</summary>

1. `src/Encina.AzureCosmosDB/Encina.AzureCosmosDB.csproj` (net10.0, nullable, `Microsoft.Azure.Cosmos`, `Encina.Security.Audit`, PublicAPI analyzers, XML docs); add to `Encina.slnx`.
2. `EncinaCosmosDBOptions`: endpoint, key or connection string (`[JsonIgnore]`, `ToString()` prints none), `DatabaseName`, retry options; `EntityChangeAuditCosmosOptions`: `ContainerName` (default `entity-change-audit`), `CreateContainerIfNotExists` (default `false`), `RetentionTtlBackstop` (default off), `Throughput`.
3. `EntityChangeAudit/EntityChangeAuditDocument.cs`: `id` = entry `Id`, `tenantId` (sentinel when none), `entityKey` (`{EntityType}:{EntityId}`), `entityType`, `entityId`, `action`, `userId`, `moduleId`, `changedAtUtc` (ISO 8601 string, ordered correctly), `oldValues`, `newValues`, `correlationId`, optional `ttl`; mapper to and from the ADR-036 entry; explicit property names.
4. `EntityChangeAudit/CosmosContainerInitializer.cs`: creates the container only when `CreateContainerIfNotExists`: hierarchical key `/tenantId`, `/entityKey`; indexing policy including `/entityType/?`, `/entityId/?`, `/userId/?`, `/changedAtUtc/?`, `/tenantId/?`, `/moduleId/?` and excluding `/oldValues/*`, `/newValues/*` and everything else; composite index `/entityKey` + `/changedAtUtc` descending.
5. Client serializer: System.Text.Json-based (SDK option or a small `CosmosSerializer`, as Phase 0 shows).
6. `PublicAPI.Unshipped.txt` and `PublicAPI.Shipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Encina is a .NET 10 / C# 14 library (Railway Oriented Programming). The ADR-036 contract (IEntityChangeAuditStore, EntityChangeAuditEntry, ChangedAtUtc as DateTimeOffset) is delivered by #1778; build on its real shapes.

TASK:
Create the package, options, document, mapper and container initializer as listed in the Phase 1 Tasks.

KEY RULES:
- Options with a key or connection string are [JsonIgnore] and ToString() never prints them.
- The container is created only on opt-in; the library never changes an existing container.
- No [Obsolete], no compatibility layer; XML docs on every public type; PublicAPI files complete.

REFERENCE FILES:
- docs/architecture/adr/036-three-audit-stores.md
- src/Encina.DomainModeling/Auditing/AuditLogEntry.cs (the entry before #1778)
- src/Encina.MongoDB/ (document and options patterns)
```

</details>

---

### Phase 2: `EntityChangeAuditStoreCosmosDB`

<details>
<summary>Tasks</summary>

1. `EntityChangeAuditStoreCosmosDB : IEntityChangeAuditStore`; constructor `(Container container, TimeProvider timeProvider, IOptions<EntityChangeAuditCosmosOptions> options, ILogger<EntityChangeAuditStoreCosmosDB> logger)`; the container comes from a `CosmosClient` singleton.
2. The write method of the contract (named by #1778): `CreateItemAsync` with partition key `(tenantId, entityKey)`; HTTP 409 maps to success (idempotent retry); HTTP 413 or an oversize document to a typed `Left`; other `CosmosException` to `Left` carrying the status code only (never the response text), logged by status code.
3. The history query: parameterized `SELECT * FROM c WHERE c.entityKey = @k AND c.tenantId = @t ORDER BY c.changedAtUtc DESC` with a full partition key, paged through the contract's `PagedResult<T>` using the continuation token; a tenant-less query uses the sentinel; a cross-tenant query is explicit and fans out.
4. `PurgeEntriesAsync(olderThan, ...)`: query ids older than the instant (from the store's `TimeProvider` where the contract takes a retention, otherwise from the argument), delete per partition key with bounded parallelism, return the count; a failure of one delete stops with `Left`.
5. `IEntityChangeAuditChangeFeed` (Design Choice 5): `ReadChangesAsync(string? continuation, int maxItems, CancellationToken)` returning entries and the next continuation token.
6. Every call async with a `CancellationToken`; no synchronous Cosmos API; keep every method at CRAP <= 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Phase 1 created the document, mapper and initializer. The store implements the ADR-036 interface delivered by #1778.

TASK:
Implement EntityChangeAuditStoreCosmosDB and the change feed reader as described in the Phase 2 Tasks.

KEY RULES:
- Either<EncinaError, T> everywhere; a Cosmos failure is a Left (fail closed), never a swallowed success; log status codes and exception types only.
- "Now" comes from TimeProvider; queries are parameterized, never concatenated.
- Keep every method at CRAP <= 10.

REFERENCE FILES:
- src/Encina.MongoDB/ (an Either-returning document store)
- docs/architecture/adr/036-three-audit-stores.md
- src/Encina.DomainModeling/Auditing/IAuditLogStore.cs
```

</details>

---

### Phase 3: Configuration, DI and registration

<details>
<summary>Tasks</summary>

1. `AddEncinaCosmosDB(this IServiceCollection, Action<EncinaCosmosDBOptions>)`: one `CosmosClient` singleton (`TryAddSingleton`), `TryAddSingleton(TimeProvider.System)`, options validation (`ValidateOnStart`).
2. `AddEncinaEntityChangeAuditCosmosDB(this IServiceCollection, Action<EntityChangeAuditCosmosOptions>? = null)`: registers the store as `IEntityChangeAuditStore` so that it wins over the in-memory default in any registration order without overriding the application's own registration, the change feed reader, the container initializer (as a hosted service only when `CreateContainerIfNotExists`), and the retention service of ADR-036 through the core registration.
3. Health: `CosmosDBHealthCheck : IEncinaHealthCheck` (read database properties; const `DefaultName`, static `Tags`, scoped resolution).
4. DI test with `ValidateOnBuild` and `ValidateScopes` in both registration orders.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Core registration of the entity-change audit store comes from #1778 (AddEncinaAudit and its in-memory default). A database store must win over the in-memory default in any registration order.

TASK:
Create the registrations, the health check and the DI test of Phase 3.

KEY RULES:
- Registration completeness: every option and dependency resolved is registered; proven with ValidateOnBuild and ValidateScopes.
- Never override the application's own store registration.
- Everything is opt-in.

REFERENCE FILES:
- src/Encina.Audit.Marten/ServiceCollectionExtensions.cs
- src/Encina.Kafka/Health/KafkaHealthCheck.cs
```

</details>

---

### Phase 4: Cross-cutting integration and observability

<details>
<summary>Tasks</summary>

1. Register `AzureCosmosDBEntityChangeAudit = (6900, 6914)` in `src/Encina/Diagnostics/EventIdRanges.cs` first (this family's block 6900-6949), add the assembly to `AssemblyRanges` in `EncinaEventIdAllocationTests.cs`, and the field to `src/Encina/PublicAPI.Unshipped.txt`.
2. `Diagnostics/EntityChangeAuditCosmosLog.cs`: `[LoggerMessage]` ids 6900-6914 packed (write failed by status code, duplicate write ignored, entry too large, purge count, change feed page, container created).
3. The ADR-036 `InstrumentedEntityChangeAuditStore` decorates the store; add a test. No entity values, user ids or tenant ids in logs or tags.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- ADR-021: every EventId range is registered in src/Encina/Diagnostics/EventIdRanges.cs before use.

TASK:
Register the range, write the log class and the architecture-test entry.

KEY RULES:
- Ids packed sequentially inside 6900-6914; nothing outside.
- Never log EncinaError.Message, exception messages, OldValues/NewValues, user or tenant ids.
- Run EncinaEventIdAllocationTests.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Audit.Marten/Diagnostics/MartenAuditLog.cs
```

</details>

---

### Phase 5: Testing

> **Goal**: every applicable flag reaches its target in `.github/coverage-manifest/Encina.AzureCosmosDB.json` (new, with one-sentence justifications).

<details>
<summary>Tasks</summary>

1. **Unit**: document mapping, partition key building, query building, error mapping (409, 413, 429, others), purge, change feed paging; container mocked, `FakeTimeProvider`.
2. **Guard**: every public method and both registrations.
3. **Contract/integration**: the shared entity-change store contract (delivered by #1778 for the database providers; if it is not reusable, this plan's first test PR adds the base class) against the Cosmos DB emulator through a shared `[Collection]` fixture in `Encina.TestInfrastructure`; cases: idempotent repeated write, history ordering and paging, tenant isolation, purge, change feed continuation, oversize entry.
4. **Property**: mapper round trip for random entries; history is ordered by `ChangedAtUtc` descending.
5. **Load/Benchmark**: `.md` justifications (append-only writes; not a hot path), per `AGENTS.md` section 9.
6. Report per-flag coverage and the CRAP table.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- Tests execute real package code; the emulator is shared through a [Collection] fixture.

TASK:
Write the tests of Phase 5 and the manifest entries.

KEY RULES:
- Shouldly via Encina.Testing.Shouldly; never FluentAssertions or per-class containers; no Thread.Sleep.
- ClearAllDataAsync in InitializeAsync; deterministic time through FakeTimeProvider.
- Targets never go down; justify every target in one sentence.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs
- tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs (contract base pattern)
```

</details>

---

### Phase 6: Documentation and finalization

<details>
<summary>Tasks</summary>

1. XML docs on every public API; `src/Encina.AzureCosmosDB/README.md`; a configuration and scaling guide (partition key, request units, retention, change feed) under `docs/`; `docs/INVENTORY.md`; ROADMAP if affected.
2. `changelog.d/582-cosmosdb-entity-change-audit-store.added.md`.
3. ADR only if Design Choice 1 or 2 changes a rule of ADR-036; ADR number assigned when the ADR pull request opens.
4. Build 0 warnings; coverage and CRAP reported.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- Documentation: package README, configuration guide, changelog fragment.

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
| ADR-036 | Three audit stores | Entity-change contract: `Either`, `TimeProvider`, `TenantId`, `ModuleId`, `PurgeEntriesAsync`, `ChangedAtUtc`, redaction hook; retention 2555 days by default; fail closed |
| SPEC-000 section 6.1, DEC-009 | 1.0 scope | Cloud stores with a free local emulator are in 1.0; section 6 is the complete exclusion list |
| SPEC-002 REQ-007, REQ-061 | Retention, tenant isolation | Retention settable per data category; tenant isolation |
| AGENTS.md section 3 | Fail closed, secrets in options | A failed audit write fails the unit of work; keys `[JsonIgnore]` |
| Azure Cosmos DB documentation (vendor) | Partitioning, TTL, change feed | Hierarchical partition keys, `ttl`, pull-model change feed iterator, 2 MB item limit |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in the Cosmos DB entity-change audit store |
|-----------|----------|---------------------------------------------------|
| `IAuditLogStore` (becomes `IEntityChangeAuditStore`) | `src/Encina.DomainModeling/Auditing/IAuditLogStore.cs` | The contract the store implements after #1778 |
| `AuditLogEntry` (becomes `EntityChangeAuditEntry`) | `src/Encina.DomainModeling/Auditing/AuditLogEntry.cs` | The entry mapped to the Cosmos document |
| `AuditAction` (becomes `EntityChangeAction`) | `src/Encina.DomainModeling/Auditing/AuditAction.cs` | The change kind stored as a string |
| `InMemoryAuditLogStore` | `src/Encina.DomainModeling/Auditing/InMemoryAuditLogStore.cs` | The default the Cosmos store must win over in any registration order |
| `EntityChangeAuditOptions`, `EntityChangeAuditRetentionService`, `InstrumentedEntityChangeAuditStore` | delivered by #1778 (`Encina.Security.Audit`) | Retention, purge scheduling and tracing around the store |
| `AddEncinaAuditMarten` | `src/Encina.Audit.Marten/ServiceCollectionExtensions.cs` | Precedent for an audit store registration that replaces the default |
| `AzuriteFixture` | `tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs` | Pattern for the Cosmos emulator fixture |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Registration of the new range |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.AzureCosmosDB` (entity-change audit) | 6900-6914 (`AzureCosmosDBEntityChangeAudit`) | From this family's block 6900-6949; register before use |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Package and options (Phase 1) | ~5 | csproj, two options classes, registration, PublicAPI x2 |
| Store, document, initializer, change feed, health, log (Phases 1-4) | ~8 | Document and mapper, initializer, store, change feed interface and reader, health check, log |
| Tests (Phase 5) | ~8 | Unit, guard, contract/integration, property, fixture |
| Manifest, README, guide, changelog | ~4 | Documentation phase |
| **Total** | **~25** | Exceeds 15 files. Proposed split: PR 1 = Phase 0 evidence plus Phases 1-2 (package, document, initializer, store, unit and guard tests); PR 2 = Phases 3-4 (registration, health, logging, change feed); PR 3 = integration tests and documentation (Phases 5-6) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution first.
- Railway Oriented Programming: Either<EncinaError, T>; errors never swallowed; TimeProvider for time; async with CancellationToken.
- Issue #582: EntityChangeAuditStoreCosmosDB implements IEntityChangeAuditStore (ADR-036, delivered by #1778) in the new package Encina.AzureCosmosDB.

IMPLEMENTATION OVERVIEW:
Phase 0: emulator and SDK verification (hierarchical keys, TTL, paging, change feed); report only.
Phase 1: package, options, document and mapper, container initializer, serializer.
Phase 2: the store (idempotent create, paged history by partition key, purge) and the pull-model change feed reader.
Phase 3: AddEncinaCosmosDB and AddEncinaEntityChangeAuditCosmosDB (wins over in-memory in any order), health check, DI test.
Phase 4: EventIdRanges.AzureCosmosDBEntityChangeAudit 6900-6914, log class, architecture test entry.
Phase 5: unit, guard, contract/integration (emulator), property tests; manifest.
Phase 6: docs and changelog fragment.

KEY PATTERNS:
- Store EntityChangeAuditStoreCosmosDB; flag UseEntityChangeAuditStore; options EntityChangeAuditOptions (ADR-036).
- Dilemmas D1-D5 are recommended, pending the maintainer: do not start Phase 1 before they are answered and Phase 0 passes.
- Never log entry values, user or tenant ids; keys and connection strings never printed.

REFERENCE FILES:
- docs/architecture/adr/036-three-audit-stores.md
- src/Encina.DomainModeling/Auditing/ (contract before #1778)
- src/Encina.Audit.Marten/ (audit store registration precedent)
- tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The original "cache frequent audit queries" is dropped: an audit read must be exact and current, so a cache adds staleness to a compliance record and no issue is needed. |
| 2 | OpenTelemetry | ✅ | The ADR-036 `InstrumentedEntityChangeAuditStore` decorates the store; Phase 4 tests it. |
| 3 | Structured logging | ✅ | Phase 4: range 6900-6914 registered first, status codes and exception types only. |
| 4 | Health checks | ✅ | Phase 3: `CosmosDBHealthCheck` over the account connectivity. |
| 5 | Validation | ✅ | Argument rules of the contract throw before I/O; the entry size limit is a typed `Left`. |
| 6 | Resilience | ✅ | The Cosmos SDK retries throttled requests (429); its retry options are exposed in `EncinaCosmosDBOptions`; no extra Polly policy. |
| 7 | Distributed locks | ❌ | Append-only audit store; no concurrent state to guard. |
| 8 | Transactions | ❌ | One entry is one document in one partition; a transactional batch is not needed and no unit of work spans Cosmos DB. |
| 9 | Idempotency | ✅ | The document `id` is the entry `Id`; a conflict on create is a success (safe retry). |
| 10 | Multi-tenancy | ✅ | The entry carries `TenantId` (ADR-036) and the first level of the hierarchical partition key (Design Choice 1). |
| 11 | Module isolation | ✅ | The ADR-036 entry carries `ModuleId`; stored and indexed (supersedes the issue's original "N/A"). |
| 12 | Audit trail | ✅ | This store IS the entity-change audit trail. |

---

## Prerequisites & Dependencies

- **#1778 (open, `[DEBT]`)** delivers the contract this plan builds on: `IEntityChangeAuditStore`, `EntityChangeAuditEntry`, `EntityChangeAuditOptions`, the retention service, the instrumentation decorator and, ideally, the shared contract test base. Implementation of #582 starts after #1778 merges. #1778 carries no `needs-decision` label and no blocking dependency, so this plan is not blocked by an open maintainer decision.
- #578 (Azure Table Storage audit store) is a sibling; package naming (Design Choice 3) is aligned with it when either starts.
- `Microsoft.Azure.Cosmos` and `Testcontainers.CosmosDb` must be added to `Directory.Packages.props` (Phase 0).
- ADR-036 decision 6 (no Marten entity-change store in 1.0) does not affect this plan.

---

## Next Steps

1. The maintainer answers D1 to D5 when work on #582 starts (D4 depends on the Phase 0 evidence); the answers go into `## Maintainer Decisions`.
2. Wait for #1778; then re-check each Design Choice against main and the open issues; `adversarial-reviewer` verifies the Settled one.
3. Brief a worker per proposed pull request (3 pull requests), starting with Phase 0.
4. Final commit references `Fixes #582`.
