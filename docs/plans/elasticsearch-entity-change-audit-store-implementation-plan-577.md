# Implementation Plan: `EntityChangeAuditStoreElasticsearch` — Elasticsearch backend for the entity-change audit store

> **Issue**: [#577](https://github.com/dlrivada/Encina/issues/577)
> **Type**: Feature
> **Complexity**: Medium (6 phases, 1 provider, a package shared with #586, ~10 source files + tests)
> **Estimated Scope**: ~800-1,100 lines of production code + ~1,400-1,900 lines of tests

---

## Summary

Implement the entity-change audit store (today `IAuditLogStore`, becoming `IEntityChangeAuditStore` under ADR-036) on Elasticsearch as `EntityChangeAuditStoreElasticsearch`: the history of "what did this entity look like before and after each change", searchable by structured fields and aggregatable for audit dashboards. The store is built on the **ADR-036 contract** (`Either` results, `TimeProvider`, `TenantId`, `ModuleId`, `PurgeEntriesAsync`, `ChangedAtUtc` as `DateTimeOffset`, the writer's redaction hook), not on the samples of the issue, which return `Task`, cap history at an unpaged 1,000 documents, use a fixed index name and index old and new values as full text.

What the code looks like today (origin/main `061222240`, re-checked for this revision; the plan branch was cut from `ce352001f`):

- `IAuditLogStore` lives in `src/Encina.DomainModeling/Auditing/IAuditLogStore.cs` (`Task LogAsync(AuditLogEntry, CancellationToken)`, `Task<IEnumerable<AuditLogEntry>> GetHistoryAsync(string entityType, string entityId, CancellationToken)`); `AuditLogEntry` (`Id`, `EntityType`, `EntityId`, `Action`, `UserId`, `TimestampUtc` as `DateTime`, `OldValues`, `NewValues`, `CorrelationId`) and `InMemoryAuditLogStore` sit next to it. ADR-036 (accepted 2026-10-03) renames and reshapes all of it; that work is **#1778, open**. Until #1778 lands, the names and members below are the ADR's, not main's.
- There is no `Encina.Elasticsearch` package, no Elasticsearch or OpenSearch client and no `Testcontainers.Elasticsearch` in `Directory.Packages.props`, no Elasticsearch service in `docker-compose.yml` and no Elasticsearch fixture in `tests/Encina.TestInfrastructure/Fixtures/`. The plan of #586 (Elasticsearch dead letter store, open) creates the same package; who owns its skeleton is Design Choice 1.
- No behavioral contract suite exists for the entity-change store (`tests/Encina.ContractTests/Security/Audit/` holds `OperationAuditStoreContractTests`, a sealed class, not a reusable base). It is expected from #1778 (see Prerequisites).
- Latest stable versions on NuGet at the time of writing: `Elastic.Clients.Elasticsearch` 9.5.3 (Apache-2.0), `OpenSearch.Client` 2.3.0 (Apache-2.0); the `Testcontainers.*` line on main is 4.15.0 (MIT). The Elasticsearch server runs in CI as a container; SPEC-000 section 6 does not exclude it (it is not a cloud service without a local emulator).

**Provider category**: search store, outside the 10-provider database matrix (`AGENTS.md` section 5); an additional backend of the entity-change interface (the database providers are #1778). **Affected packages**: `Encina.Elasticsearch` (new, shared with #586), `Encina.Security.Audit` (the contract, from #1778), `Encina` (EventId registry), test projects, `Directory.Packages.props`.

---

## Design Choices

<details>
<summary><strong>1. Package placement and ownership of the shared skeleton (Dilemma D1)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One shared `Encina.Elasticsearch` package; #586 owns the skeleton (csproj, pins, `Encina.slnx` row, client options, `AddEncinaElasticsearch`, PublicAPI files, manifest, README); #577 adds an `EntityChangeAudit/` feature folder after it** | One client registration and one connection configuration per cluster; the feature-folder rule (`AGENTS.md` section 4) keeps both stores apart | #577 waits for the skeleton pull request of #586 |
| **B) The same shared package, with #577 owning the skeleton** | #577 can start first | #586 waits; the skeleton is shaped by the audit store |
| **C) A separate package `Encina.Security.Audit.Elasticsearch`** | No ordering between the two issues; audit backends discoverable under one prefix | Two packages and two client registrations to configure one cluster |

### Chosen Option: **A — one package, skeleton owned by #586** (recommended, pending the maintainer)

### Rationale

- The same answer as Design Choice 6 of the #586 plan, which the maintainer decides together with this one. B wins if #577 is implemented first; C wins if every audit backend must be an independently versioned opt-in package.
- Whatever the answer, the pull request that creates the package adds `Elastic.Clients.Elasticsearch` to `Directory.Packages.props`, and the first integration pull request of either issue adds `Testcontainers.Elasticsearch` and the shared fixture; the second one reuses them.

</details>

<details>
<summary><strong>2. Elasticsearch, OpenSearch or both (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Official Elasticsearch client (`Elastic.Clients.Elasticsearch`); OpenSearch only if a Phase 0 run proves the store's calls work against it** | One code path and one container suite; best fit for Elastic Cloud and self-managed Elasticsearch | The issue's "Elasticsearch/OpenSearch" may shrink to Elasticsearch only |
| **B) Two implementations behind one internal seam** | Both servers supported for sure | Two clients, two suites, twice the maintenance |
| **C) OpenSearch client only** | Fits AWS OpenSearch Service users | Leaves Elasticsearch users out |

### Chosen Option: **A — Elasticsearch client first** (recommended, pending the maintainer)

### Rationale

- The same question as Design Choice 1 of the #586 plan; the package shares one client, so both issues get the same answer. B wins when AWS OpenSearch Service users are a named audience; C wins if the project targets OpenSearch only.

</details>

<details>
<summary><strong>3. Index layout and tenant isolation (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One index (default `encina-entity-change-audit`) with a `tenantId` keyword; every query of the store adds a `term` filter on the ambient tenant** | One mapping and one shard set; no index explosion with many tenants; cross-tenant reads are possible only through the explicit opt-in | Isolation is a filter, not a physical boundary |
| **B) One index per tenant (`{prefix}-{encoded tenant}`)** (the issue's deferred item) | Physical isolation; per-tenant deletion is an index delete | Index names must be lowercase and short, so tenant ids need an injective encoding; thousands of tenants mean thousands of shards (cluster limits) |

### Chosen Option: **A — one index, tenant filter on every query** (recommended, pending the maintainer)

### Rationale

- Both options keep the tenant rules of the family: with tenancy on, the entry's `TenantId` is stored; the reserved segment `default` marks tenant-less entries, written only when tenancy is off; a real tenant whose id is `default` is rejected on write with a `Left` (`InvalidRequest`, ADR-062 R3); with tenancy on and no tenant resolved, write, history and search deny with a `Left` (`InvalidRequest`; `AGENTS.md` section 3: gates fail closed) and never fall back to `default`.
- A history query or search across tenants needs an explicit cross-tenant opt-in on the call, logged by event id only (no ids), and denied by default (`Forbidden`, R6). The retention purge is not a request-time read: it receives the tenant explicitly (Design Choice 4) and never uses the ambient tenant or this opt-in.
- B wins when a tenant contract demands physical separation or per-tenant index deletion.
- Not an option: a data stream or rollover aliases with time-based backing indices. `_id` is unique only inside one backing index, so the idempotent create of Design Choice 9 would stop detecting duplicates after a rollover, and retention could not differ per tenant.

</details>

<details>
<summary><strong>4. How retention is applied</strong></summary>

**Class:** Settled
**Settled by:** ADR-036 decision 1 (`PurgeEntriesAsync`, 2555-day default), SPEC-002 REQ-007 (retention per data category), #4182 (retention iterates the tenants through `ITenantStore` with each tenant's policy, never the ambient tenant or a cross-tenant opt-in), GDPR Art. 5(1)(e)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Per-tenant `PurgeEntriesAsync` through `delete_by_query` on `tenantId = <given tenant>` and `changedAtUtc < <that tenant's cutoff>`; the ADR-036 `EntityChangeAuditRetentionService` runs in a tenant-less scope, enumerates `ITenantStore.GetAllTenantsAsync` and calls the purge once per tenant with that tenant's policy (once with the `default` segment when tenancy is off)** | Per-tenant retention; every provider purges the same way (`AGENTS.md` section 5); each purge is an explicit, counted act; the purge never depends on an ambient tenant | Deletes cost merges and I/O on large indices |
| **B) Index lifecycle management (ILM) deletes whole indices** | No delete cost | Needs time-based indices, which Design Choice 3 excludes; one retention for every tenant; bypasses `PurgeEntriesAsync` |

### Chosen Option: **A — per-tenant purge with the tenant passed explicitly**

### Rationale

- B cannot give each tenant its own retention and needs the backing indices that break the idempotent create, so A is the only compliant option. The exact purge signature (the tenant and cutoff it receives, and where each tenant's policy comes from) is decided by #4182 and delivered by #1778; this store filters on the tenant it is given and never reads the ambient tenant or an opt-in for the purge. A store call without a tenant while tenancy is on is a `Left` (`InvalidRequest`).
- `delete_by_query` runs with `conflicts=proceed`, `wait_for_completion=true` and `slices=auto`; its `failures` array, a `version_conflicts` count above zero, or `timed_out: true` (a delete-by-query can time out after some batches have already deleted documents) is a `Left` (`Internal`), never a partial success reported as complete; the retention service retries a failed tenant at its next run, and the purge is idempotent, so a partial delete followed by a retry converges. A failure for one tenant is reported for that tenant; the retention service's handling of the remaining tenants follows #4182.
- **Tenant segments that `ITenantStore` no longer returns (rule).** Enumerating registered tenants alone would never purge two kinds of entries, which GDPR Art. 5(1)(e) and SPEC-002 REQ-007 forbid keeping past their retention: entries of an offboarded tenant (kept under path C of #2847 after its tenant record is removed) and `default`-segment entries written before tenancy was switched on. The store therefore also lists the distinct tenant segments it holds (`ListTenantSegmentsAsync`, or the name #1778 gives it; here a `composite` aggregation on `tenantId`, paged), and the retention service purges every listed segment that `ITenantStore` does not return, including `default` once tenancy is on, under the residual policy #4182 defines (for example the policy recorded at offboarding, or the default retention of `EntityChangeAuditOptions`). This case is an acceptance item of #4182. The listing runs whether tenancy is on or off, so segments that real tenants wrote before tenancy was switched off are purged too. The listing fails closed: every search it issues sets `allow_partial_search_results=false`, and a response with `_shards.failed > 0`, a timeout or a transport failure is a `Left` (`Internal`), never a shorter list reported as complete, because a dropped segment would be kept past its retention.

</details>

<details>
<summary><strong>5. Visibility of a write to the history read (Dilemma D4)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No refresh on write; the history and search reads are near real time (about the `refresh_interval`, 1 s by default), documented** | The write inside the EF Core unit of work (ADR-036: a failed write fails it) stays fast; Elasticsearch's own guidance against per-request refresh | A history read right after a save may miss the newest entry |
| **B) `refresh=wait_for` on every write** | Read-your-writes like the database providers | Every audited save waits up to one refresh interval |
| **C) Refresh before each history read** | Exact reads, fast writes | A refresh per read is expensive under load and is shared by all writers of the index |

### Chosen Option: **A — near-real-time reads, documented** (recommended, pending the maintainer)

### Rationale

- The entity-change writer runs inside `SaveChanges` of every audited entity, so per-write latency is multiplied by the write volume; audit history is read by people and dashboards, not by the next line of the request. The derived contract test class forces a refresh before each read so the shared suite stays deterministic. B wins when an application reads history immediately after writing (for example a "changes" panel on the save response); the #586 plan chose B for dead letters, whose replay claim needs it, a need the audit store does not have.

</details>

<details>
<summary><strong>6. The optional search capability of the issue (Dilemma D5)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IEntityChangeAuditSearch` in the package: structured filters (entity type, entity id, user, module, action, time range), paged; `GetStatisticsAsync` (counts by action and entity type) and `GetTopUsersAsync` (terms aggregation); old and new values are stored but not indexed (`"index": false`)** | Covers the dashboard use of the issue; personal data inside old and new values is not searchable, so a search cannot locate a person by content | No free text over values |
| **B) A plus full-text search over old and new values (the issue's `text` mapping)** | The "search by any content" motivation of the issue | Makes the personal data held in the values searchable; larger index; values must already be redacted by the writer (ADR-036) for this to be acceptable |
| **C) Defer search to its own issue; ship only the contract** | Smallest pull requests | Drops an acceptance criterion of the issue |

### Chosen Option: **A — structured search and aggregations, values not indexed** (recommended, pending the maintainer)

### Rationale

- GDPR Art. 5(1)(c) (data minimisation) and Art. 25 (data protection by design) favor not indexing free-form personal data that the audit purpose does not need for lookup. Every search method takes UTC `DateTimeOffset` bounds, returns `Either<EncinaError, T>`, is bound to the ambient tenant (Design Choice 3) and caps its page size (`MaxSearchPageSize`, default 1,000; above it a `Left`, `Validation`). B wins when investigators must search value content and the writer's redaction hook is configured for every sensitive property; C wins if the maintainer wants the store first and search later.

</details>

<details>
<summary><strong>7. Tamper evidence (Dilemma D6)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Append-only use of the index (create only, no update API in the store), a SHA-256 `payloadHash` stored on every document, and documented least-privilege roles (writer: `create_doc` and `read`; retention: `read` and `delete`, both of which `delete_by_query` requires); a hash chain is left to #1240** | No new mechanism; the hash makes a silent edit detectable on the conflict path and gives #1240 a ready input | An operator with `index` privilege can still rewrite a document and its hash together |
| **B) A per-store hash chain (each document carries the previous hash) built now** | Rewrites become detectable by a chain check | Serialises writes per chain; duplicates #1240 for one provider only |

### Chosen Option: **A — append-only, payload hash, least-privilege roles; chain through #1240** (recommended, pending the maintainer)

### Rationale

- Elasticsearch has no write-once index mode; immutability is a property of the credentials. #1240 (open) owns the persistent hash-chained log across providers, so a chain here would be a second, provider-specific design. B wins if a regulator or customer requires chain verification of the entity-change trail before #1240 lands.

</details>

<details>
<summary><strong>8. Contract, naming and failure semantics</strong></summary>

**Class:** Settled
**Settled by:** ADR-036 (decisions 1 and 7, naming tables), ADR-001, ADR-006, ADR-062 (R2-R6), AGENTS.md section 3 (TimeProvider; fail closed; errors never swallowed; secrets in options), AGENTS.md section 4 (`{Pattern}Store{Provider}`)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Build on the ADR-036 contract: class `EntityChangeAuditStoreElasticsearch`, `EntityChangeAuditOptions` for retention, `Either` results, `TenantId`, `ModuleId`; a failed write is a `Left` returned to the writer (what the writer does with it is Design Choice 11); cancellation propagates as `OperationCanceledException`** | Provider coherence; fail-closed auditing | Depends on #1778 landing first |
| **B) Keep the old `IAuditLogStore` shape** | Buildable today | A contract ADR-036 removes, with no compatibility layer (decision 7) |

### Chosen Option: **A — the ADR-036 contract**

### Rationale

- ADR-036 says the follow-up issues apply the names and do not revisit them; B is excluded by its decision 7. Credentials (API key, password, cloud id with secret) live in the client options of the package, marked `[JsonIgnore]`, with `ToString()` printing none of them. Every `EncinaErrors.Create` passes a named `category:` and a constant message; no exception message or server response text reaches a `Left`, a log or a span.

</details>

<details>
<summary><strong>9. Idempotent write and the document id</strong></summary>

**Class:** Settled
**Settled by:** AGENTS.md section 3 (errors are never swallowed; an audit write that fails must fail visibly), AGENTS.md section 6 function 9 (idempotency), ADR-062 R2 (duplicates are `AlreadyExists`)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `_id` = injective tenant-qualified id `{len(tenant)}:{tenant}:{entryId}` with default routing; `op_type=create`; on HTTP 409 read the stored document, recompute the SHA-256 hash from its stored payload fields, and accept the retry as success only when the recomputed hash equals both the stored `payloadHash` and the incoming entry's hash; any mismatch is a `Left` (`AlreadyExists`) and is logged by event id** | Safe retries; an id collision, or a stored document whose payload was edited while its original `payloadHash` was kept, is never silent; default routing keeps `_id` unique in the index | One extra GET on the rare conflict path |
| **B) Treat every 409 as success** | Simplest | Silently loses an entry on an id collision |
| **C) Content-addressed `_id` (hash of the payload)** | Identical retries deduplicate by construction | Two different payloads with the same entry id both land: the collision is never detected |

### Chosen Option: **A — tenant-qualified id, create, compare on conflict**

### Rationale

- B and C both let an audit entry be lost or duplicated without a failure, which section 3 forbids; A is the only option that keeps retries safe and collisions visible. The length prefix makes the id injective (`A:B` + `C` never equals `A` + `B:C`); custom routing is not used because, with routing, `_id` uniqueness holds per shard only.
- It holds under both options of Design Choice 3: one index, or one index per tenant (where the tenant-qualified id is unique inside each tenant's index). Data streams and rollover aliases are excluded in Design Choice 3 because they would break it.

</details>

<details>
<summary><strong>10. Erasure requests and immutability of entries</strong></summary>

**Class:** Settled
**Settled by:** maintainer decision of 2026-10-26 on #2847 (path C is the default for audit data: GDPR Art. 17(3)(b) and 17(3)(e); the request is recorded, the data stays until its retention expires), ADR-036 decision 1 (retention through `PurgeEntriesAsync`, default 2555 days; redaction hook in the writer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No per-subject delete in the store; entries are never updated; removal happens only through `PurgeEntriesAsync` at retention; minimisation happens before the write through the ADR-036 redaction hook** | Matches the maintainer's audit erasure decision and the contract | An exceptional court order to delete one person's entries needs a separate, explicit capability (the Marten counterpart is #2903) |
| **B) A per-subject delete method on this store** | Answers erasure directly | Contradicts path C as the default and the append-only nature of the trail |

### Chosen Option: **A — retention only, no per-subject delete**

### Rationale

- The decision on #2847 makes recording plus retention the default for audit data; B is the exceptional path A of that decision, which is opt-in and planned separately (#2903 for Marten). The package README states that values are redacted by the writer, not by the store.

</details>

<details>
<summary><strong>11. Every committed change gets an audit entry (Dilemma D7)</strong></summary>

**Class:** Dilemma

The fact today (origin/main `061222240`): `AuditInterceptor` captures entries in `SavingChanges` and persists them in `SavedChanges`/`SavedChangesAsync`, after the database commit (`src/Encina.EntityFrameworkCore/Auditing/AuditInterceptor.cs:140-161`), and both persist paths catch the exception and only log it (lines 396-399 and 441-444; #2424 records the debt). With a database store a failed write cannot roll the change back either, but with Elasticsearch the entry can never join the database transaction, so the guarantee has to come from the design. ADR-036 decision 1 requires that a failed write fail closed; this choice decides how, for a non-transactional backend.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Outbox in the same database transaction: the writer records the entry as an outbox message inside the unit of work (before commit), and a delivery step writes it to Elasticsearch with retries and dead-lettering** | A committed change always has its entry, an uncommitted one never has; failures are retried, then dead-lettered visibly (AGENTS.md section 3: background errors are never swallowed) | Needs the outbox enabled (opt-in, ADR-050 if made a requirement); entries reach Elasticsearch with outbox latency |
| **B) Write before commit: the writer stores the entry in Elasticsearch in `SavingChanges`, and a failed write fails the save** | A committed change always has its entry; no outbox | A change rolled back after the write leaves an entry for a change that never happened, so the trail needs a rollback marker or an operator note |
| **C) Write after commit and make the failure visible: `SavedChanges` writes the entry and, on failure, throws a typed exception to the caller (the data is already committed) and records the entry for a later retry** | Simplest change of today's writer | The caller sees an error for a change that was committed; the retry store is a second mechanism that must itself be durable |

### Chosen Option: **A — outbox in the same transaction** (recommended, pending the maintainer)

### Rationale

- A is the only option where the entry and the change commit or roll back together, which is what the fail-closed rule of ADR-036 needs from a backend that cannot join the transaction. B wins when the outbox is not available and an occasional entry for a rolled-back change is acceptable; C wins when the latency of the outbox is not acceptable and the application can handle a post-commit error.
- The store itself does not change between options: it returns a `Left` on failure (Design Choice 8). The writer change belongs to #1778 and #2424 (both open); this plan only fixes what the Elasticsearch store needs from it. Until the writer is changed, the README states that a failed write is logged, not retried.

</details>

---

## Implementation Phases

### Phase 0: Client and server verification (no pull request)

<details>
<summary>Tasks</summary>

1. With locally started containers (Elasticsearch at the vendor's current LTS line and OpenSearch at its LTS line) and a throwaway script that is never committed, run through `Elastic.Clients.Elasticsearch` the calls the store uses: index with `op_type=create` (second call returns 409), GET by `_id`, `delete_by_query` with `conflicts=proceed` and its `failures` field, a `bool` query with `term` filters and `sort` on `changedAtUtc` descending with `search_after`, `terms` and `date_histogram` aggregations, index creation with a strict mapping and `"index": false` fields.
2. Verify that a real `ElasticsearchClient` can be built over `Elastic.Transport`'s `InMemoryRequestInvoker` with canned JSON responses (the unit-test seam); if it cannot, the store gets an internal `IElasticsearchEntityChangeAuditGateway` with `InternalsVisibleTo` for `Encina.UnitTests` only (guard tests exercise the public surface, so the per-file guard target of 0 for internal types stays valid).
3. The evidence is a pass/fail table in the worker's report; the orchestrator posts it on #577 (and on #586 for Design Choice 2). A failed criterion returns to the maintainer as an update of the affected Dilemma.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 0</strong></summary>

```
CONTEXT:
- Issue #577: Elasticsearch backend of the entity-change audit store (ADR-036). No Elasticsearch code exists on main.

TASK:
Run the client and server checks of the Phase 0 Tasks against local containers and report a pass/fail table.

KEY RULES:
- No production code, no pins and no pull request in this phase; the script is never committed.
- Report, never decide: a failing criterion goes back to the maintainer.

REFERENCE FILES:
- docs/plans/elasticsearch-dead-letter-store-implementation-plan-586.md (Phase 0 of the sibling store)
- tests/Encina.TestInfrastructure/Fixtures/KafkaFixture.cs (fixture shape)
```

</details>

---

### Phase 1: Document, mapping, options and index initializer

<details>
<summary>Tasks</summary>

1. `src/Encina.Elasticsearch/EntityChangeAudit/EntityChangeAuditDocument.cs` (internal): `tenantId`, `entityType`, `entityId`, `entryId`, `action`, `userId`, `moduleId`, `changedAtUtc` (UTC, ISO 8601 with offset `Z` and all seven fractional digits of `DateTimeOffset`, so the 100 ns tick precision of the contract is kept), `oldValues`, `newValues` (stored, `"index": false`), `correlationId`, `payloadHash`, `docId` (a copy of the tenant-qualified `_id`, the unique sort tie-breaker); mapper to and from the ADR-036 entry; `DocumentIdBuilder.Build(tenantSegment, entryId)` (length-prefixed, Design Choice 9).
2. `EntityChangeAudit/EntityChangeAuditElasticsearchOptions.cs` (public, sealed): `IndexName` (default `encina-entity-change-audit`, validated against Elasticsearch index-name rules), `CreateIndexIfNotExists` (default `false`), `NumberOfShards`, `NumberOfReplicas`, `MaxSearchPageSize` (default 1,000), `AllowCrossTenantQueries` (default `false`; history and search only, never the purge). Cluster credentials stay in the package's client options (skeleton, Design Choice 1).
3. `EntityChangeAudit/EntityChangeAuditIndexInitializer.cs` (internal, `IHostedService`): creates the index only when `CreateIndexIfNotExists`, with `dynamic: strict`, keywords for `tenantId`, `entityType`, `entityId`, `entryId`, `action`, `userId`, `moduleId`, `correlationId`, `payloadHash`, `docId` (keyword with doc values, sortable), `date_nanos` for `changedAtUtc` (the `date` type has millisecond resolution, so two changes in the same millisecond would fall back to the tie-breaker instead of time order), and `oldValues`/`newValues` as non-indexed `keyword` with `doc_values: false` (Design Choice 6, Dilemma D5, option A); an existing index is never changed; a mismatch of the mapping is logged and the initializer fails start-up with a typed exception message that names no value.
4. Register the EventId range (Phase 4 numbers) and the log class in this PR, because the initializer logs (index created, mapping mismatch).
5. No registration in this PR: the initializer is internal and is registered by `AddEncinaEntityChangeAuditElasticsearch` in Phase 2, so Phase 1 ships the types and their tests only (every type is internal except the options).
6. `PublicAPI.Unshipped.txt` of the package (options only).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Encina is a .NET 10 / C# 14 library (Railway Oriented Programming). The ADR-036 contract (IEntityChangeAuditStore, EntityChangeAuditEntry, ChangedAtUtc as DateTimeOffset) is delivered by #1778; build on its real shapes on main.
- The package src/Encina.Elasticsearch exists (skeleton from #586 or #577 per Design Choice 1).

TASK:
Create the document, mapper, id builder, options and index initializer of the Phase 1 Tasks, the EventId range registration and the log class.

KEY RULES:
- Index created only on opt-in; an existing index is never changed.
- oldValues and newValues are stored and not indexed.
- The document id is length-prefixed and tenant-qualified; test colliding concatenations.
- No [Obsolete], no compatibility layer; XML docs on every public type; PublicAPI files complete.

REFERENCE FILES:
- docs/architecture/adr/036-three-audit-stores.md
- src/Encina.MongoDB/Auditing/AuditLogDocument.cs (document pattern before #1778)
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/engineering/eventid-block-registry.md
```

</details>

---

### Phase 2: `EntityChangeAuditStoreElasticsearch` and its registration

<details>
<summary>Tasks</summary>

1. `EntityChangeAudit/EntityChangeAuditStoreElasticsearch.cs` (public, sealed) `: IEntityChangeAuditStore`; constructor `(ElasticsearchClient client, ITenantContext? tenantContext, IOptions<EntityChangeAuditElasticsearchOptions> options, TimeProvider timeProvider, ILogger<EntityChangeAuditStoreElasticsearch> logger)` (the exact tenant abstraction is the one #1778 uses for the database providers).
2. Write: resolve the tenant first (tenancy on and none resolved: `Left`, `InvalidRequest`; a real tenant `default`: `Left`, `InvalidRequest`); compute `payloadHash`; `IndexAsync` with `op_type=create`; 409 reads the stored document and compares (Design Choice 9); 413 or a document over `http.max_content_length` is a `Left` (`Validation`); 429 and 503 are `Left` (`Unavailable`); other transport failures are `Left` (`Internal`, R4) carrying the HTTP status code only.
3. History: `term` filters on `tenantId`, `entityType`, `entityId`, sort `changedAtUtc` desc then `docId` desc (`docId` is the tenant-qualified `_id`, unique across tenants, so a `search_after` page boundary can never fall between two entries with identical sort values, also in cross-tenant history and search where two tenants may share an `entryId` and a `changedAtUtc`), paged through the contract's `PagedResult<T>` with `search_after` (never `from` beyond `index.max_result_window`). Every search of the store (history, search capability, segment listing) sets `allow_partial_search_results=false` and treats `_shards.failed > 0` or `timed_out` as `Left` (`Unavailable` when shards are unavailable, otherwise `Internal`), so an incomplete audit trail is never returned as a success.
4. `PurgeEntriesAsync` with the tenant and cutoff it receives (signature from #4182 and #1778): `delete_by_query` per Design Choice 4 filtered on that tenant only; it never reads the ambient tenant and needs no opt-in; with tenancy on and no tenant passed it returns `Left` (`InvalidRequest`); returns the deleted count. `ListTenantSegmentsAsync` (Design Choice 4) returns the distinct `tenantId` values through a paged `composite` aggregation, so the retention service can purge offboarded tenants and the legacy `default` segment. The store is resolved by the tenant-less retention service, so it must not depend on a request-scoped tenant context to be constructed.
5. `EntityChangeAudit/EntityChangeAuditElasticsearchErrors.cs`: the codes and categories of Phase 4.
6. `EntityChangeAudit/ServiceCollectionExtensions.cs`: `AddEncinaEntityChangeAuditElasticsearch(this IServiceCollection, Action<EntityChangeAuditElasticsearchOptions>? configure = null)` registers the options (`ValidateOnStart`), `TryAddSingleton(TimeProvider.System)`, the store as `IEntityChangeAuditStore` so that it wins over the in-memory default in any registration order without overriding an application registration (the mechanism #1778 defines for the database stores), and the initializer with `TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, EntityChangeAuditIndexInitializer>())`. It requires `AddEncinaElasticsearch` (the client) and fails at build with a clear message when it is missing.
7. DI test with `ValidateOnBuild` and `ValidateScopes`, both registration orders. Every method stays at CRAP <= 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Phase 1 created the document, mapper, options and initializer. The store implements the ADR-036 interface delivered by #1778.

TASK:
Implement EntityChangeAuditStoreElasticsearch, its error codes and AddEncinaEntityChangeAuditElasticsearch as described in the Phase 2 Tasks, with a DI test.

KEY RULES:
- Either<EncinaError, T> everywhere; an Elasticsearch failure is a Left (fail closed); log status codes and exception types only; cancellation propagates.
- Every query carries the tenant filter; tenancy on and no tenant denies; cross-tenant history and search only with the explicit, logged opt-in.
- The purge filters on the tenant passed by the retention service (#4182), never on the ambient tenant or an opt-in.
- Hosted services through TryAddEnumerable; the store wins over the in-memory default in any order.
- Keep every method at CRAP <= 10.

REFERENCE FILES:
- docs/architecture/adr/036-three-audit-stores.md
- docs/architecture/adr/062-error-category-contract.md
- src/Encina.Audit.Marten/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 3: Search capability and health check

<details>
<summary>Tasks</summary>

1. `EntityChangeAudit/IEntityChangeAuditSearch.cs` (public): `SearchAsync(EntityChangeAuditSearchQuery query, CancellationToken)` returning `Either<EncinaError, PagedResult<EntityChangeAuditEntry>>`; `GetStatisticsAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken)` returning `Either<EncinaError, EntityChangeAuditStatistics>`; `GetTopUsersAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, int limit, CancellationToken)` returning `Either<EncinaError, IReadOnlyList<EntityChangeAuditUserActivity>>` (Design Choice 6).
2. `EntityChangeAuditSearchQuery`, `EntityChangeAuditStatistics`, `EntityChangeAuditUserActivity` (sealed records; `fromUtc > toUtc` and `limit` outside 1..`MaxSearchPageSize` are `Left`, `Validation`).
3. `EntityChangeAuditSearchElasticsearch` implementing it; registered by `AddEncinaEntityChangeAuditElasticsearch` (`TryAddSingleton<IEntityChangeAuditSearch, ...>`).
4. `EntityChangeAudit/EntityChangeAuditElasticsearchHealthCheck.cs : IEncinaHealthCheck` (index exists and the cluster answers; const `DefaultName`, static `Tags`), registered by the same method.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Phase 2 shipped the store and its registration. Design Choice 6 fixes the search shape (option A recommended; follow the answer recorded in the plan).

TASK:
Add IEntityChangeAuditSearch, its records and implementation, and the health check, all registered by AddEncinaEntityChangeAuditElasticsearch.

KEY RULES:
- Search is tenant-bound and capped by MaxSearchPageSize; old and new values are never queried.
- Results are Either; bounds are UTC DateTimeOffset.
- Health check: DefaultName const, Tags static array.

REFERENCE FILES:
- src/Encina.Kafka/Health/KafkaHealthCheck.cs
- docs/plans/elasticsearch-dead-letter-store-implementation-plan-586.md (search capability of the sibling)
```

</details>

---

### Phase 4: Cross-cutting integration and observability

<details>
<summary>Tasks</summary>

1. EventId range `ElasticsearchEntityChangeAudit = (52350, 52399)` in `src/Encina/Diagnostics/EventIdRanges.cs`, registered in the Phase 1 pull request together with: its line in `src/Encina/PublicAPI.Unshipped.txt` (one line for the new field), the assembly entry `["Encina.Elasticsearch"]` in `AssemblyRanges` of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (appended to the #586 range if that entry exists), and in `docs/engineering/eventid-block-registry.md` a "Registered ranges" row `ElasticsearchEntityChangeAudit | 52350-52399` (Zone `zone`, area Security and compliance) and a "Blocks" row `52350-52399` with status `registered`.
2. `EntityChangeAudit/Diagnostics/ElasticsearchEntityChangeAuditLog.cs`: `[LoggerMessage]` ids packed from 52350: 52350 index created, 52351 index mapping mismatch, 52352 write failed (status code), 52353 duplicate write ignored, 52354 id conflict, 52355 document too large, 52356 tenant purge completed (count), 52357 tenant purge failed (status code), 52358 cross-tenant query opt-in used, 52359 search failed (status code). Ids 52360-52399 stay free inside the range.
3. The ADR-036 `InstrumentedEntityChangeAuditStore` decorates the store through #1778's registration (a DI test asserts it). Telemetry follows ADR-054 D4: the tenant id is a span attribute and a metric tag only when multi-tenancy is on; logs never carry tenant, user or entity ids, values, exception messages or `EncinaError.Message`.
4. **ErrorCategory (ADR-062)** per code: `entity_change_audit.elasticsearch.write_failed` `Internal` (R4); `.unavailable` (429, 503, connection failure) `Unavailable`; `.entry_too_large` `Validation` (R3); `.id_conflict` `AlreadyExists` (R2); `.tenant_required` and `.reserved_tenant` `InvalidRequest` (R3); `.cross_tenant_denied` `Forbidden` (R6); `.purge_failed` and `.search_failed` `Internal`; `.invalid_search` `Validation`. Cancellation is not a code: `OperationCanceledException` propagates.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- AGENTS.md section 7 and ADR-021: every range is registered in EventIdRanges.cs before use, new blocks only in the 10000+ zone; the registry page is checked by EventIdRegistryAgreementTests.

TASK:
Register ElasticsearchEntityChangeAudit 52350-52399, write the log class, add the architecture-test entry and the registry rows, and wire the error categories.

KEY RULES:
- Ids packed from 52350 with no gap; nothing outside the range.
- Never log EncinaError.Message, exception messages, values, user, entity or tenant ids.
- Run EncinaEventIdAllocationTests and EventIdRegistryAgreementTests.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/engineering/eventid-block-registry.md
- src/Encina.Audit.Marten/Diagnostics/MartenAuditLog.cs
```

</details>

---

### Phase 5: Testing

> **Goal**: every applicable flag reaches its target in `.github/coverage-manifest/Encina.Elasticsearch.json` (created by the skeleton pull request), with one-sentence justifications set in the same pull request as the tests that reach them.

<details>
<summary>Tasks</summary>

1. **Unit** (PRs 1-3): mapper round trip, id builder (colliding concatenations), options validation, initializer branches, every error mapping (409 same and different hash, 409 where the stored payload was edited but its original `payloadHash` kept, 413, 429, 503, other), a delete-by-query response with `timed_out: true` returns `Left`, tenant rules (none resolved denies, real tenant `default` rejected, no fallback, cross-tenant query opt-in required and logged), purge filtered on the tenant passed in and never on the ambient one, purge with tenancy on and no tenant passed returns `Left`, segment listing across aggregation pages, a canned partial-shard response to history and to the segment listing returns `Left` (never a shorter result), the listing runs with tenancy off, purge failures, search validation; a real `ElasticsearchClient` over `InMemoryRequestInvoker` (or the gateway seam of Phase 0), `FakeTimeProvider`.
2. **Guard** (`tests/Encina.GuardTests/Elasticsearch/EntityChangeAudit/`): null and invalid arguments of the store constructor and methods, the search methods, the registration method and the options.
3. **Contract** (PR 4): a class deriving from the shared entity-change contract base (see Prerequisites) on the Elasticsearch collection fixture, refreshing the index before reads (Design Choice 5).
4. **Integration** (PR 4): idempotent repeated write, id conflict with a different payload, history order and `search_after` paging past `max_result_window`, two changes 100 ns apart in the same millisecond keep their time order, a cross-tenant page boundary between entries of two tenants that share `entryId` and `changedAtUtc` omits and repeats nothing, colliding concatenations, tenant isolation (tenant A never reads tenant B; `default` unreachable with tenancy on), purge of tenant A with its cutoff from a scope with no ambient tenant deletes only tenant A's old entries (tenant B, with a longer retention, keeps its own), the segment list contains a tenant removed from `ITenantStore` and the `default` segment written before tenancy was enabled, and both are purged under the residual policy, purge count and `failures` handling, search and aggregations, index created with the strict mapping, health check. Shared `[Collection]` fixture `ElasticsearchFixture` in `Encina.TestInfrastructure` (created by the first of #577 and #586 to reach integration; `Testcontainers.Elasticsearch` at the `Testcontainers` 4.15.0 line pinned on main, MIT; the Elasticsearch server image is used only in tests), `ClearAllDataAsync` in `InitializeAsync`.
5. **Property** (PR 4): mapper round trip and id-builder injectivity for random strings.
6. **Load/Benchmark**: `.md` justifications (append-only writes outside a hot path) per `AGENTS.md` section 9.

**Manifest rule**: `Encina.Elasticsearch.json` holds the package targets and per-file entries of the package skeleton (the #586 skeleton under option A of Design Choice 1, PR 0 of this plan under options B and C); this plan never lowers them. Each PR adds, with its tests, the per-file `targets` and `justifications` below for the flags it reaches, and raises the package-level `targets` for any flag it adds (contract, property and integration arrive in PR 4), so `coverage-report.cs --check-justifications` passes on every PR.

**Per-file targets** (`-` = not applicable to that flag; "PR" is the pull request that adds the target, its tests and its justification):

| File | Unit | Guard | Contract | Property | Integration | PR | Justification (one sentence per flag, copied into the manifest's `justifications`) |
|------|:----:|:-----:|:--------:|:--------:|:-----------:|:--:|---------------|
| `EntityChangeAudit/EntityChangeAuditDocument.cs` (document, mapper, id builder) | 95 | 0 | - | 90 | 85 | 1 (unit, guard), 4 (property, integration) | Unit: pure mapping and id building run without a server. Guard: 0, the type is internal and `Encina.GuardTests` has no `InternalsVisibleTo`. Property: random entries exercise the round trip and the id injectivity. Integration: every store test round-trips documents through the container. |
| `EntityChangeAudit/EntityChangeAuditElasticsearchOptions.cs` | 90 | 50 | - | - | 70 | 1, 4 | Unit: defaults and every validation branch. Guard: guard tests set each validated property to an invalid value, which runs the validation half of the file. Integration: the container tests build the options through the registration. |
| `EntityChangeAudit/EntityChangeAuditIndexInitializer.cs` | 85 | 0 | - | - | 85 | 1, 4 | Unit: opt-in, existing-index and mismatch branches on the in-memory transport. Guard: 0, internal with no `InternalsVisibleTo` for guard tests. Integration: the container proves the created mapping. |
| `EntityChangeAudit/EntityChangeAuditStoreElasticsearch.cs` | 90 | 30 | 85 | - | 85 | 2, 4 | Unit: error mapping and tenant rules on canned responses. Guard: guard tests reach the constructor and method argument checks only, about a third of the lines of a store that does I/O. Contract: the shared suite drives every contract method. Integration: the container covers write, history, purge and conflicts. |
| `EntityChangeAudit/EntityChangeAuditElasticsearchErrors.cs` | 100 | 0 | - | - | - | 2 | Unit: every factory method is called by the store tests. Guard: 0, internal with no validated parameters and no `InternalsVisibleTo` for guard tests. |
| `EntityChangeAudit/ServiceCollectionExtensions.cs` | 95 | 40 | - | - | - | 2 | Unit: the DI test builds the provider with ValidateOnBuild in both orders. Guard: null `services` and null `configure` run the argument checks, which precede a short registration body. |
| `EntityChangeAudit/EntityChangeAuditSearchElasticsearch.cs` (and its records) | 90 | 30 | - | - | 80 | 3, 4 | Unit: query building and validation on canned responses. Guard: argument and bound checks only, about a third of the lines. Integration: the container proves the aggregations. |
| `EntityChangeAudit/EntityChangeAuditElasticsearchHealthCheck.cs` | 90 | 30 | - | - | 70 | 3, 4 | Unit: healthy, missing-index and unreachable paths on canned responses. Guard: the constructor checks only. Integration: the container proves the healthy path. |
| `EntityChangeAudit/Diagnostics/ElasticsearchEntityChangeAuditLog.cs` | 0 | 0 | - | - | - | 1 | Unit: 0, source-generated `[LoggerMessage]` partial methods with no hand-written executable code. Guard: 0, same reason. |

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- Tests execute real package code; the Elasticsearch container is shared through a [Collection] fixture.

TASK:
Write the tests of Phase 5 and the manifest targets with their justifications, each in the pull request that reaches them.

KEY RULES:
- Shouldly via Encina.Testing.Shouldly; never FluentAssertions, per-class containers or Thread.Sleep.
- ClearAllDataAsync in InitializeAsync; FakeTimeProvider for time.
- Targets never go down; run coverage-report.cs --check-justifications.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/KafkaFixture.cs (collection fixture shape)
- tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs (contract base pattern)
- .github/coverage-manifest/Encina.Kafka.json (manifest format)
```

</details>

---

### Phase 6: Documentation and finalization

<details>
<summary>Tasks</summary>

1. XML docs on every public API; the `Encina.Elasticsearch` README gains an entity-change audit section (index layout, tenancy, retention, near-real-time reads, roles, values not searchable); a feature page under `docs/features/`; `docs/INVENTORY.md`; ROADMAP if affected (all through `docs-writer`).
2. `changelog.d/577-elasticsearch-entity-change-audit-store.added.md` (through `mechanical-fixer`).
3. ADR only if a Design Choice changes a rule of ADR-036; ADR number assigned when the ADR pull request opens.
4. Build 0 warnings; per-flag coverage and the CRAP table reported.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- Documentation: package README section, feature page, inventory, changelog fragment.

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
| ADR-036 | Three audit stores | Entity-change contract: `Either`, `TimeProvider`, `TenantId`, `ModuleId`, `PurgeEntriesAsync`, `ChangedAtUtc`, redaction hook; 2555-day default retention; fail closed |
| ADR-062 | Error categories | Every code carries a category (R2-R6 used here) |
| ADR-054 D4 | Telemetry | Tenant as attribute and tag only when multi-tenancy is on |
| SPEC-002 REQ-007, REQ-061 | Retention, tenant isolation | Retention settable per data category; tenant isolation |
| #4182 (open, `[DEBT]`) | Per-tenant retention | The retention service iterates `ITenantStore` with each tenant's policy; purge never relies on an ambient tenant or a cross-tenant opt-in |
| GDPR Art. 5(1)(c), 5(1)(e), 17(3)(b), 25 | Minimisation, storage limitation, erasure exception, by design | Values not indexed; retention purge; recorded erasure (decision on #2847) |
| Elasticsearch reference (vendor) | Index API, `op_type`, `delete_by_query`, `search_after`, refresh | Behaviour verified in Phase 0 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in the Elasticsearch entity-change audit store |
|-----------|----------|------------------------------------------------------|
| `IAuditLogStore` (becomes `IEntityChangeAuditStore`) | `src/Encina.DomainModeling/Auditing/IAuditLogStore.cs` | The contract implemented after #1778 |
| `AuditLogEntry` (becomes `EntityChangeAuditEntry`) | `src/Encina.DomainModeling/Auditing/AuditLogEntry.cs` | The entry mapped to the document |
| `InMemoryAuditLogStore` | `src/Encina.DomainModeling/Auditing/InMemoryAuditLogStore.cs` | The default the store must win over in any registration order |
| `AuditLogStoreMongoDB`, `AuditLogDocument` | `src/Encina.MongoDB/Auditing/` | Document-store precedent for mapping and queries |
| `EntityChangeAuditOptions`, `EntityChangeAuditRetentionService`, `InstrumentedEntityChangeAuditStore` | delivered by #1778 (`Encina.Security.Audit`) | Retention, purge scheduling and tracing around the store |
| `AddEncinaAuditMarten` | `src/Encina.Audit.Marten/ServiceCollectionExtensions.cs` | Precedent for an audit store registration that replaces only the default |
| `ITenantStore.GetAllTenantsAsync` | `src/Encina.Tenancy/Abstractions/ITenantStore.cs` | Tenant enumeration of the retention service (#4182) |
| `OperationAuditRetentionService` | `src/Encina.Security.Audit/OperationAuditRetentionService.cs` | Shape of the tenant-less retention scope (it calls `PurgeEntriesAsync` from a fresh scope with no tenant) |
| `IAuditAttestationProvider` | `src/Encina.Compliance.Attestation/Abstractions/IAuditAttestationProvider.cs` | Existing attestation; the persistent chain is #1240 (Design Choice 7) |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Registration of the new range |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Elasticsearch` (entity-change audit) | 52350-52399 (`ElasticsearchEntityChangeAudit`) | First free 50-id slot of the Security and compliance area after the #4007 reservations 50000-52349 (registry on origin/main `061222240`, where 52350-52599 is still free); the five audit-stores-1 plans take 52350-52599 packed (#577, #578, #579, #580, #581 in order); ids 52350-52359 used; registered by PR 1 with its registry rows; the #586 dead letter store keeps its own range |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production (Phases 1-4) | ~11 | document and mapper, options, initializer, store, errors, registration, search interface, search records, search implementation, health check, log class |
| Tests (Phase 5) | ~12 | unit x4, guard x2, DI, contract class, integration, property, fixture and collection |
| Metadata | ~8 | `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, registry page, allocation test, package `PublicAPI.Unshipped.txt`, manifest, `Directory.Packages.props`, test-project references |
| **Total** | **~36 with edits** | Exceeds 15 files. **PR 0** exists only under options B or C of Design Choice 1 (under option A the #586 skeleton PR plays its role): csproj (with `InternalsVisibleTo` for `Encina.UnitTests`, `Encina.PropertyTests`, `Encina.IntegrationTests`, `Encina.ContractTests`), `Encina.slnx` row, `Directory.Packages.props` (the client chosen in Design Choice 2: under option A `Elastic.Clients.Elasticsearch`, Apache-2.0, latest stable 9.5.3 at the time of writing; under option C `OpenSearch.Client`, Apache-2.0, 2.3.0), client options with `[JsonIgnore]` credentials and a safe `ToString()`, `AddEncinaElasticsearch` (one client, `TryAddSingleton`), `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`, README stub, the `ProjectReference` edits of `tests/Encina.UnitTests/Encina.UnitTests.csproj` and `tests/Encina.GuardTests/Encina.GuardTests.csproj`, options and registration unit tests, guard tests, DI test, manifest with package-level targets: 14 files (recounted by name; if it grows past 15, the README stub moves to PR 5). Every later PR starts after #1778 and the package skeleton are on main, builds and passes alone and stays at or below 15 files and 1500 lines. **PR 1** (13 files): `EntityChangeAuditDocument.cs` (document, mapper, id builder), options, initializer, log class, `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, registry page, `EncinaEventIdAllocationTests.cs`, package `PublicAPI.Unshipped.txt`, mapper unit tests, initializer unit tests, options guard tests, manifest (the initializer is internal and not yet registered, so the PR adds no service). **PR 2** (9 files): store, errors, registration (store, options, initializer through `TryAddEnumerable`), package `PublicAPI.Unshipped.txt`, store unit tests, DI test, store guard tests, registration guard tests, manifest. **PR 3** (9 files): search interface, records, implementation, health check, registration edit, `PublicAPI.Unshipped.txt`, unit tests, guard tests, manifest. **PR 4** (13 files at most): `Testcontainers.Elasticsearch` pin (if #586 has not added it), `ElasticsearchFixture` in `Encina.TestInfrastructure`, a collection definition in `Encina.IntegrationTests` and one in `Encina.ContractTests` (collection definitions are per assembly), `Encina.IntegrationTests`, `Encina.ContractTests` and `Encina.PropertyTests` project-reference edits (if missing), contract class, integration tests, property tests, two load/benchmark `.md` files, manifest. **PR 5** (docs, 4 files): README, feature page, `docs/INVENTORY.md`, changelog fragment |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution first.
- Railway Oriented Programming: Either<EncinaError, T>; errors never swallowed; TimeProvider for time; async with CancellationToken; cancellation propagates.
- Issue #577: EntityChangeAuditStoreElasticsearch implements IEntityChangeAuditStore (ADR-036, delivered by #1778) in Encina.Elasticsearch (shared with #586).

IMPLEMENTATION OVERVIEW:
Phase 0: client and server checks against local containers; report only.
Phase 1: document, mapper, id builder, options, index initializer, EventId range 52350-52399 and log class.
Phase 2: the store (create with compare on conflict, paged history with search_after, per-tenant purge with delete_by_query on the tenant passed by the retention service) and AddEncinaEntityChangeAuditElasticsearch.
Phase 3: IEntityChangeAuditSearch (structured filters, statistics, top users) and the health check.
Phase 4: error categories, instrumentation, telemetry tags.
Phase 5: unit, guard, contract, integration, property tests; manifest targets with justifications.
Phase 6: docs and changelog fragment.

KEY PATTERNS:
- Dilemmas D1-D7 are recommended, pending the maintainer: follow the answers recorded in the plan.
- Writer guarantee (D7, recommended outbox): a committed change always gets its entry; the store returns a Left on failure and never swallows it.
- Tenant: reserved segment default, deny when tenancy is on and no tenant resolves, cross-tenant history and search only with an explicit logged opt-in; tenant-qualified, length-prefixed document ids.
- Values stored, not indexed; no per-subject delete; retention through a per-tenant PurgeEntriesAsync called by the retention service for each tenant of ITenantStore (#4182), never through the ambient tenant.
- Every error code carries an ADR-062 category; never log values, ids or messages.

REFERENCE FILES:
- docs/architecture/adr/036-three-audit-stores.md
- docs/architecture/adr/062-error-category-contract.md
- src/Encina.DomainModeling/Auditing/ (contract before #1778)
- src/Encina.Audit.Marten/ServiceCollectionExtensions.cs
- docs/plans/elasticsearch-dead-letter-store-implementation-plan-586.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | An audit read must be exact and current; a cache adds staleness to a compliance record. |
| 2 | OpenTelemetry | ✅ | The ADR-036 `InstrumentedEntityChangeAuditStore` decorates the store; tenant tag only when multi-tenancy is on (ADR-054 D4). |
| 3 | Structured logging | ✅ | Phase 4: range 52350-52399 registered first; status codes and exception types only. |
| 4 | Health checks | ✅ | Phase 3: `EntityChangeAuditElasticsearchHealthCheck` (cluster reachable, index exists). |
| 5 | Validation | ✅ | Argument checks throw before I/O; oversize documents and invalid search bounds are typed `Left` values. |
| 6 | Resilience | ✅ | The Elasticsearch transport retries failed nodes; throttling (429) and unavailability are `Unavailable` so the caller's retry policy can act; no extra Polly policy in the store. |
| 7 | Distributed locks | ❌ | Append-only store; concurrency on one document is arbitrated by `op_type=create`. |
| 8 | Transactions | ⏭️ | Elasticsearch takes part in no database transaction. Today the EF Core writer persists entries in `SavedChanges`/`SavedChangesAsync`, after the commit, and catches and logs a failed write (`src/Encina.EntityFrameworkCore/Auditing/AuditInterceptor.cs:140-161, 396-399, 441-444` on origin/main `061222240`), so a committed change can end up with no audit entry. How a committed change is guaranteed an entry is Design Choice 11 (Dilemma D7); the writer itself is changed by #1778 and #2424 (open). |
| 9 | Idempotency | ✅ | Tenant-qualified `_id` from the entry id; identical retry is success, different payload is `AlreadyExists` (Design Choice 9). |
| 10 | Multi-tenancy | ✅ | `tenantId` on every document and query; reserved `default`; deny on missing tenant; explicit logged cross-tenant opt-in for history and search (Design Choice 3); per-tenant retention purge with the tenant passed explicitly (Design Choice 4, #4182). |
| 11 | Module isolation | ✅ | The ADR-036 entry carries `ModuleId`; stored as a keyword and filterable in search (supersedes the issue's "deferred"). |
| 12 | Audit trail | ✅ | This store IS the entity-change audit trail. |

---

## Prerequisites & Dependencies

- **#1778 (open, `[DEBT]`, no `needs-decision`, no blocking dependency)** delivers the contract: `IEntityChangeAuditStore`, `EntityChangeAuditEntry`, `EntityChangeAuditQuery`, `EntityChangeAuditOptions`, the retention service, the instrumentation decorator, the in-memory default and the shared behavioral contract base. If #1778 merges without a reusable base, the first of #577-#582 to reach its integration pull request adds it (the #582 plan states the same fallback) and the others derive from it.
- **#586 (open)** shares `Encina.Elasticsearch`: under Design Choice 1 option A its skeleton pull request merges first. The shared fixture and `Testcontainers.Elasticsearch` pin belong to whichever of the two reaches integration first.
- **#1240 (open)** owns the persistent hash chain (Design Choice 7); **#2903 (open)** is the exceptional subject-scoped erasure path for Marten.
- **#4182 (open, `[DEBT]`)** decides the per-tenant retention mechanism (tenant enumeration through `ITenantStore`, each tenant's policy, the tenant-explicit purge signature, and the residual policy for segments `ITenantStore` no longer returns: offboarded tenants and the legacy `default` segment) before #1778 implements `EntityChangeAuditRetentionService`; Design Choice 4 and Phase 2 task 4 follow it.
- The EventId slot 52350-52399 lies after the #4007 reservations (50000-52349, #4174 merged); the orchestrator keeps the block registry free of overlaps between families that plan in the same 1000 block.

---

## Next Steps

1. The maintainer answers D1 to D7 when work on #577 starts (D7 together with #1778 and #2424, which change the writer) (D1 and D2 together with #586), after Phase 0; the answers go into `## Maintainer Decisions`.
2. Wait for #4182, #1778 and the package skeleton; re-check each Design Choice against main; `adversarial-reviewer` verifies the Settled ones.
3. Brief one worker per proposed pull request (5 pull requests, 6 with PR 0 under options B or C of Design Choice 1).
4. The final pull request references `Fixes #577`.
