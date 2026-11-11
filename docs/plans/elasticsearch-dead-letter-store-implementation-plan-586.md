# Implementation Plan: `DeadLetterStoreElasticsearch` — Elasticsearch/OpenSearch backend for the persistent dead letter queue

> **Issue**: [#586](https://github.com/dlrivada/Encina/issues/586)
> **Type**: Feature
> **Complexity**: Medium-High (6 phases, 1 new package shared with #577, ~18 source files + tests)
> **Estimated Scope**: ~1,200-1,600 lines of production code + ~1,800-2,400 lines of tests

---

## Summary

Implement `Encina.Messaging.DeadLetter.IDeadLetterStore` on Elasticsearch as `DeadLetterStoreElasticsearch`, so that dead letters can be searched (full text over error codes, request types, exception types and stack traces), aggregated and retained with an index lifecycle policy, while the store keeps every guarantee of the ADR-046 contract: oldest-first paged reads, exact counts, an atomic replay claim, the unique `(SourcePattern, SourceMessageId)` key (`AddAsync` returns `Right(false)` for a repeat), set-based deletes and expiry from `TimeProvider`. An optional capability interface adds `SearchAsync` and `GetStatisticsAsync`.

What the code looks like today (main `7a0049c07`):

- There is no `Encina.Elasticsearch` package, no Elasticsearch or OpenSearch client in `Directory.Packages.props`, and no such service in `docker-compose.yml`. The package is shared with #577 (audit store for Elasticsearch/OpenSearch); the first pull request to merge creates it.
- The contract is `IDeadLetterStore` (`src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs`, 11 methods, `Either<EncinaError, T>`). The issue's sample used a `DeadLetterMessage` type, `f.ErrorMessage` (the field is `ErrorCode`; `ExceptionMessage` was removed from the record because exception text can hold personal data, ADR-046), `f.Source` (it is `SourcePattern`) and `f.CreatedAtUtc` (it is `DeadLetteredAtUtc`). Searchable text is therefore `ErrorCode`, `RequestType`, `ExceptionType` and `ExceptionStackTrace`; request content (`RequestContent`) is not indexed for search.
- `DeadLetterManager` already computes statistics from indexed counts (ADR-046 Choice 5), so `GetStatisticsAsync` here is an optimisation, not a replacement.
- Existing backends: `DeadLetterStoreADO`/`Dapper` (x3 each), `DeadLetterStoreEF`, `DeadLetterStoreMongoDB`, `InstrumentedDeadLetterStore`, `FakeDeadLetterStore`; the behavior suite is `tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs`.

**Provider category**: search store, outside the 10-provider database matrix (`AGENTS.md` section 5). **Affected packages**: `Encina.Elasticsearch` (new, shared with #577), `Encina.Messaging` (no change), test projects, `Directory.Packages.props`.

---

## Design Choices

<details>
<summary><strong>1. One implementation for Elasticsearch and OpenSearch, or two (Dilemma D1)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One package on the official Elasticsearch .NET client** (`Elastic.Clients.Elasticsearch`) | Best fit for Elastic Cloud and self-managed Elasticsearch; one code path and one container suite | May fail against OpenSearch servers: the claim "OpenSearch is API compatible" of the issue is unverified and the two clients have diverged |
| **B) Two thin packages sharing one core over a small internal abstraction** | Supports AWS OpenSearch Service users with a first-class client | Two packages, two container test suites, an abstraction that has to hide the differences between the two query DSL clients |
| **C) One package on the OpenSearch client** (`OpenSearch.Client`) | Right choice if only OpenSearch is wanted | Leaves Elasticsearch users out |

### Chosen Option: **A — Elasticsearch client first; OpenSearch support only if a Phase 0 test proves it** (recommended, pending the maintainer)

### Rationale

- Phase 0 is a small verification: run the Phase 5 contract suite against an OpenSearch container with the Elasticsearch client. If it passes, OpenSearch is supported at no cost; if it does not, OpenSearch becomes a follow-up issue (option B) instead of blocking the 1.0 store.
- B wins when AWS OpenSearch Service users are a named audience; C wins if the maintainer decides the project's target is OpenSearch only. The decision is shared with #577, which uses the same package.

</details>

<details>
<summary><strong>2. The document `_id` (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The message `Id`** (a GUID) | `GetAsync` is a real-time get by id | Uniqueness of `(SourcePattern, SourceMessageId)` needs a second mechanism (a search before indexing is racy between hosts) |
| **B) A deterministic id from the source key** (`{SourcePattern}:{hash(SourceMessageId)}`) with `op_type=create` | Atomic uniqueness: a second capture gets HTTP 409 and returns `Right(false)` | `GetAsync(Guid)` becomes a term search on the stored `id` field (with the refresh policy of Design Choice 3 it is consistent) |

### Chosen Option: **B — deterministic id from the source key** (recommended, pending the maintainer)

### Rationale

- The unique source key is the one correctness rule that cannot be approximated: duplicate dead letters make a replay dispatch twice (ADR-046 Choice 6). B gets it from the engine in one operation.
- A wins if `GetAsync` by id dominates and duplicates are tolerated, which ADR-046 rejects. The hash is SHA-256 hex of the UTF-8 source id (an `_id` is limited to 512 bytes); the full key is stored in the document and compared on a 409 to rule out a collision.

</details>

<details>
<summary><strong>3. Read-your-writes: refresh policy (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `refresh=wait_for` on writes** | The manager sees its own writes: counts right after `AddAsync`, claims right after a read; the shared contract tests pass as written | Slower writes (bounded by the refresh interval, default 1 s) |
| **B) Default refresh, behavior documented** | Highest capture throughput | Counts and filtered pages lag; `DeadLetterStoreContract` (order, `GetCountAsync` after `AddAsync`) would need tolerance, which weakens the shared contract |

### Chosen Option: **A — `refresh=wait_for` on every write** (recommended, pending the maintainer)

### Rationale

- Dead letters are an exception path with few writes; correctness of the operator's view beats throughput. A also keeps the contract suite unchanged for every provider.
- B wins when the queue absorbs bursts of thousands of dead letters per second and operators accept a lagging view; the setting is an option (`RefreshPolicy`) so it can be changed by configuration without a code change.

</details>

<details>
<summary><strong>4. Where the search capability lives (Dilemma D4)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IDeadLetterStoreSearchable` in `Encina.Elasticsearch`** | No provider-specific shape in the abstractions package (`AGENTS.md` section 5: abstractions in `Encina.Messaging`, providers differ only in implementation) | The manager cannot use it without a reference to the package |
| **B) `IDeadLetterStoreSearchable` in `Encina.Messaging`** | Any future search-capable backend (OpenSearch, a database with full text) can implement it; the manager can offer search | Adds an abstraction nothing in core uses yet (pay-for-what-you-use) |

### Chosen Option: **A — in the package, returning `Either` and taking UTC instants** (recommended, pending the maintainer)

### Rationale

- Only one backend can implement it today. The interface moves to `Encina.Messaging` when a second implementation or a manager feature needs it; moving is a rename of a namespace, cheaper than carrying an unused abstraction.
- B wins if the manager UI gets a search box in 1.0. Signatures follow ADR-046 decision 6: `GetStatisticsAsync(DateTime fromUtc, DateTime toUtc, ...)` rejects a non-UTC `Kind`, results are `Either<EncinaError, T>`.

</details>

<details>
<summary><strong>5. Contract, mapping, claim and index lifecycle</strong></summary>

**Class:** Settled
**Settled by:** ADR-046 (decisions 1, 5, 6, 7, Choice 6), ADR-001, ADR-006, AGENTS.md section 3 (TimeProvider; errors never swallowed), https://www.elastic.co/guide/en/elasticsearch/reference/current/index-lifecycle-management.html

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Full ADR-046 contract; filter keys (`requestType`, `errorCode`, `correlationId`, `sourcePattern`, `sourceMessageId`, `tenantId`) as case-sensitive `keyword` fields (same comparison semantics as PostgreSQL and MongoDB, ADR-046 decision 1); `ErrorCode`, `RequestType`, `ExceptionType`, `ExceptionStackTrace` also `text` for search; atomic claim by a conditional update (`if_seq_no`/`if_primary_term`), conflict means `Right(false)`; `DeleteManyAsync` and `DeleteExpiredAsync` by `delete_by_query` (`conflicts=proceed`); ILM only as a retention backstop** | Provider coherence; set-based deletes return a count; ILM is an index-level mechanism (age, size), so it cannot express a per-document `ExpiresAtUtc`: `DeleteExpiredAsync` stays authoritative | The mapping is fixed before the first release (ADR-046 reasoning: a mapping change after 1.0 is a reindex) |
| **B) ILM as the only expiry** | No delete code | Cannot match `ExpiresAtUtc <= now` per document, so the contract fails |

### Chosen Option: **A — the full contract with `delete_by_query` and ILM as a backstop**

### Rationale

- The issue says an ILM policy keyed on `ExpiresAtUtc` is configuration; the vendor documentation shows ILM acts on indices (rollover, age, size), so the plan corrects it: ILM caps total retention, `DeleteExpiredAsync` enforces the per-message expiry.
- `delete_by_query` is not atomic across documents: version conflicts proceed, any `failures` entry in the response makes the call a `Left` (never reported as success), and the returned count is the `deleted` field.

</details>

<details>
<summary><strong>6. Package, naming, registration and tests</strong></summary>

**Class:** Settled
**Settled by:** AGENTS.md section 4 (`{Pattern}Store{Provider}`; the full provider name, not `Elastic`), AGENTS.md section 3 (registration completeness; secrets in options), AGENTS.md section 9 (shared `[Collection]` fixtures), #577

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Elasticsearch` shared with #577; `DeadLetterStoreElasticsearch`; `AddEncinaDeadLetterElasticsearch()` over `AddDeadLetterQueueServices`; `Testcontainers.Elasticsearch` for integration tests; client credentials in options marked `[JsonIgnore]` with `ToString()` overridden** | Matches the existing satellite pattern; one client registration for both issues | The first of #577 and #586 to merge creates the skeleton |
| **B) A separate package per feature** | Independent versions | Two packages to configure one cluster |

### Chosen Option: **A — one `Encina.Elasticsearch` package**

### Rationale

- The issue calls the package shared with #577. `Elastic.Clients.Elasticsearch` and `Testcontainers.Elasticsearch` are not pinned yet: Phase 1 adds them to `Directory.Packages.props` at the vendor LTS version (project default-images rule).

</details>

---

## Implementation Phases

### Phase 1: Package, mapping and document model

> **Goal**: the package skeleton (skipped if #577 landed first), the index mapping and the document type.

<details>
<summary>Tasks</summary>

1. `src/Encina.Elasticsearch/Encina.Elasticsearch.csproj`, options (`Uri`/cloud id, credentials: `[JsonIgnore]` and a `ToString()` that prints none of them), `AddEncinaElasticsearch` (single `ElasticsearchClient`, `TryAddSingleton`), PublicAPI files, add to `Encina.slnx`, pins in `Directory.Packages.props`.
2. `src/Encina.Elasticsearch/DeadLetter/DeadLetterDocument.cs`: public sealed class implementing `IDeadLetterMessage`; `DeadLetterMessageFactory.cs`.
3. `DeadLetterIndexMapping.cs`: explicit mapping (keyword and text fields of Design Choice 5, `date` for the instants, `dynamic: strict`), settings (`max_result_window` from the options, default 100,000), optional ILM policy and rollover alias created on startup only when `CreateIndexIfMissing` is true.
4. `DeadLetterIds.cs`: deterministic `_id` per Design Choice 2.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Encina is a .NET 10 / C# 14 library (Railway Oriented Programming). Issue #586: DeadLetterStoreElasticsearch implements IDeadLetterStore (ADR-046) in the new package src/Encina.Elasticsearch, shared with issue #577.

TASK:
Create the skeleton (if absent), the document, the factory, the explicit mapping and the id helper as listed in the Phase 1 Tasks.

KEY RULES:
- Credentials in options are [JsonIgnore] and never printed by ToString().
- Mapping is explicit and strict; filter keys are case-sensitive keyword fields; instants are UTC dates.
- No [Obsolete], no compatibility layer; XML docs on every public type; PublicAPI files complete.

REFERENCE FILES:
- src/Encina.MongoDB/DeadLetter/DeadLetterMessage.cs and DeadLetterMessageFactory.cs
- src/Encina.Messaging/DeadLetter/IDeadLetterMessage.cs
- src/Encina.Kafka/Encina.Kafka.csproj (satellite package layout)
```

</details>

---

### Phase 2: `DeadLetterStoreElasticsearch`

> **Goal**: the 11 contract methods.

<details>
<summary>Tasks</summary>

1. `src/Encina.Elasticsearch/DeadLetter/DeadLetterStoreElasticsearch.cs`: `sealed class DeadLetterStoreElasticsearch : IDeadLetterStore`; constructor `(ElasticsearchClient client, TimeProvider timeProvider, ILogger<DeadLetterStoreElasticsearch> logger, IOptions<DeadLetterElasticsearchOptions>? options = null)`.
2. `AddAsync`: index with `op_type=create`, refresh per option; HTTP 409 with the same stored source key means `Right(false)`; any other failure is `Left` (`dlq.store_failed`, exception type or status code only).
3. `DeadLetterQueryBuilder.cs` (internal): translates `DeadLetterFilter` to a bool query (term, range, `exists` for `ExpiresAtOrBeforeUtc`, `ExcludeReplayed` as `ReplayedAtUtc` missing or present), one small method per predicate group.
4. `GetAsync` (term on `id`), `GetMessagesAsync` (sort `deadLetteredAtUtc` then `id`, `newestFirst` reverses both, `from`/`size`), `GetCountAsync` (`_count`).
5. `TryClaimForReplayAsync`: get the document with its `_seq_no`/`_primary_term`, test the claim conditions with `claimExpiredBeforeUtc`, index with `if_seq_no`/`if_primary_term`; 409 means a lost claim, `Right(false)`. `MarkAsReplayedAsync` is the same pattern and updates only while `ReplayedAtUtc` is null.
6. `DeleteAsync` (delete by id), `DeleteManyAsync`/`DeleteExpiredAsync` (`delete_by_query`, `conflicts=proceed`, any `failures` is a `Left`), `SaveChangesAsync` returns `Right(Unit)`.
7. `DeadLetterStoreSearchable` surface: `IDeadLetterStoreSearchable` (Design Choice 4) with `SearchAsync(string query, ...)` (multi-match over the text fields, bounded `size`) and `GetStatisticsAsync(DateTime fromUtc, DateTime toUtc, ...)` (aggregations by error code, source pattern, day).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Phase 1 produced the document, mapping and id helper. The contract is src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs and the behavior suite tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs.

TASK:
Implement DeadLetterStoreElasticsearch, the query builder and the searchable interface as described in the Phase 2 Tasks.

KEY RULES:
- Either<EncinaError, T> everywhere; a client failure or a delete_by_query failure is a Left, never success; log error codes or exception types only.
- "Now" and expiry come from TimeProvider; instants are validated as DateTimeKind.Utc (ADR-046 decision 6).
- The claim is a conditional update (seq_no/primary_term); no locks.
- Keep every method at CRAP <= 10.

REFERENCE FILES:
- src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs
- src/Encina.Messaging/DeadLetter/DeadLetterFilter.cs
- src/Encina.Messaging/DeadLetter/DeadLetterStoreLimits.cs
```

</details>

---

### Phase 3: Configuration, DI and registration

<details>
<summary>Tasks</summary>

1. `DeadLetterElasticsearchOptions`: `IndexName` (default `encina-dead-letters`), `RefreshPolicy` (default wait-for), `MaxResultWindow` (default 100,000), `CreateIndexIfMissing` (default `false`), `IlmMaxAge` (optional), `AddHealthCheck`.
2. `AddEncinaDeadLetterElasticsearch(this IServiceCollection, Action<DeadLetterElasticsearchOptions>? = null, Action<DeadLetterOptions>? = null)`: options, `TryAddSingleton(TimeProvider.System)`, `AddDeadLetterQueueServices<DeadLetterStoreElasticsearch, DeadLetterMessageFactory>(true, options)`, and `IDeadLetterStoreSearchable` resolved to the same instance.
3. Health: `ElasticsearchHealthCheck : IEncinaHealthCheck` over `_cluster/health` (green/yellow healthy, red unhealthy), const `DefaultName`, static `Tags`.
4. DI test with `ValidateOnBuild` and `ValidateScopes`, both registration orders, and an application's own store kept.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Queue registration is shared: src/Encina.Messaging/DeadLetter/DeadLetterServiceCollectionExtensions.cs and AddDeadLetterQueueServices (first registration wins).

TASK:
Create the options, the registration, the health check and the DI test of Phase 3.

KEY RULES:
- Registration completeness: every option type and dependency the store resolves is registered; proven with ValidateOnBuild and ValidateScopes.
- Everything is opt-in; the store never overrides the application's own registration.
- Health check pattern: DefaultName const, Tags static array, scoped resolution.

REFERENCE FILES:
- src/Encina.Kafka/Health/KafkaHealthCheck.cs
- src/Encina.MongoDB/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 4: Cross-cutting integration and observability

<details>
<summary>Tasks</summary>

1. Register `ElasticsearchDeadLetter = (6935, 6949)` in `src/Encina/Diagnostics/EventIdRanges.cs` first (this family's block), add the assembly to `AssemblyRanges` in `EncinaEventIdAllocationTests.cs`, and the field to `src/Encina/PublicAPI.Unshipped.txt`.
2. `Diagnostics/DeadLetterElasticsearchLog.cs`: `[LoggerMessage]` ids 6935-6949, packed (store failure by operation, claim lost, duplicate capture, delete_by_query partial failure, mapping created, ILM policy created).
3. `InstrumentedDeadLetterStore` decorates the store; add a test. No tenant id in logs or tags (SPEC-002 REQ-062).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- ADR-021: ranges are registered in src/Encina/Diagnostics/EventIdRanges.cs before use.

TASK:
Register the range, write the log class and add the architecture-test entry.

KEY RULES:
- Ids packed sequentially inside 6935-6949; nothing outside the range.
- Never log EncinaError.Message, exception messages or tenant ids.
- Run EncinaEventIdAllocationTests.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Kafka/Log.cs
```

</details>

---

### Phase 5: Testing

> **Goal**: every flag reaches its target in `.github/coverage-manifest/Encina.Elasticsearch.json` (new, with justifications, unless #577 created it).

<details>
<summary>Tasks</summary>

1. **Phase 0 verification**: run the contract suite against an OpenSearch container with this client (Design Choice 1); the result is recorded in the PR and decides whether OpenSearch is in the first release.
2. **Unit**: query builder (every predicate), id helper (collision guard), mapping, error mapping, claim outcomes; client mocked.
3. **Guard**: every public method and the registration.
4. **Contract/integration**: `DeadLetterStoreContract` against an Elasticsearch container (`Testcontainers.Elasticsearch`, shared `[Collection]` fixture in `Encina.TestInfrastructure`), plus: concurrent claim from two stores, `delete_by_query` with a version conflict, search and statistics.
5. **Property**: the query builder agrees with an in-memory oracle for random filters.
6. **Load/Benchmark**: `.md` justifications (exception-path store), per `AGENTS.md` section 9.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- Tests execute real package code; containers are shared through [Collection] fixtures.

TASK:
Write the tests of Phase 5, the fixture and the manifest entries.

KEY RULES:
- Shouldly via Encina.Testing.Shouldly; never FluentAssertions or IClassFixture for containers.
- ClearAllDataAsync in InitializeAsync; no Thread.Sleep; FakeTimeProvider.
- Targets never go down; justify every target in one sentence.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/KafkaFixture.cs
- tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs
```

</details>

---

### Phase 6: Documentation and finalization

<details>
<summary>Tasks</summary>

1. XML docs on every public API; `src/Encina.Elasticsearch/README.md`; a section in `docs/features/dead-letter-queue.md` (mapping, refresh policy, ILM backstop); `docs/INVENTORY.md`.
2. `changelog.d/586-elasticsearch-dead-letter-store.added.md`.
3. ADR for the mapping and id decisions; number assigned when the ADR pull request opens.
4. Build 0 warnings; coverage and CRAP reported.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- Documentation: package README, docs/features/dead-letter-queue.md, changelog fragment, ADR.

TASK:
Complete the documentation tasks of Phase 6.

KEY RULES:
- Never edit CHANGELOG.md; use changelog.d/.
- No hand-typed coverage figures; English only; never name the reference application.

REFERENCE FILES:
- docs/features/dead-letter-queue.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Topic | Requirement |
|--------|-------|-------------|
| SPEC-000 DEC-008, REQ-030 | Dead letter queue in 1.0 | #584-#589 are additional backends (milestone v0.19.0) |
| ADR-046 | Persistent dead letter queue | Oldest first, `Right(false)` on a repeated source key, atomic claim, set-based deletes, UTC instants, no exception message stored |
| Elasticsearch documentation (vendor) | Optimistic concurrency, `op_type=create`, `delete_by_query`, ILM | `if_seq_no`/`if_primary_term`, HTTP 409 on conflict, ILM acts on indices |
| AGENTS.md section 3 | Options secrets | Credentials `[JsonIgnore]`, never printed |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in a new IDeadLetterStore backend |
|-----------|----------|------------------------------------------|
| `IDeadLetterStore` | `src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs` | The contract |
| `IDeadLetterMessage`, `DeadLetterData`, `IDeadLetterMessageFactory` | `src/Encina.Messaging/DeadLetter/` | The record and its factory |
| `DeadLetterFilter`, `DeadLetterErrorCodes`, `DeadLetterStoreLimits` | `src/Encina.Messaging/DeadLetter/` | Filter translation, error codes, page limits |
| `AddDeadLetterQueueServices` | `src/Encina.Messaging/DeadLetter/DeadLetterServiceCollectionExtensions.cs` | First-wins registration |
| `InstrumentedDeadLetterStore` | `src/Encina.OpenTelemetry/MessagingStores/InstrumentedDeadLetterStore.cs` | Tracing and metrics |
| `DeadLetterStoreContract` | `tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs` | Shared behavior suite |
| `DeadLetterStoreMongoDB` | `src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs` | Closest reference (document store, unique key, claim) |
| `KafkaHealthCheck`, `KafkaFixture` | `src/Encina.Kafka/Health/`, `tests/Encina.TestInfrastructure/Fixtures/` | Health check and container fixture patterns |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Elasticsearch` (dead letter) | 6935-6949 (`ElasticsearchDeadLetter`) | From this family's block 6900-6949; the audit store of #577 takes its own range from its family |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Package skeleton (skipped if #577 landed first) | ~6 | csproj, options, registration, PublicAPI x2, README |
| Dead letter production code (Phases 1-4) | ~11 | Document, factory, mapping, ids, query builder, store, searchable interface, options, registration, health, log |
| Tests (Phase 5) | ~9 | Unit, guard, contract/integration, property, fixture |
| Manifest, feature page, changelog, ADR | ~4 | Documentation phase |
| **Total** | **~30** | Exceeds 15 files. Proposed split: PR 1 = skeleton, document, mapping, ids, store, query builder (Phases 1-2); PR 2 = registration, health, logging, searchable interface (Phases 3-4); PR 3 = tests and documentation (Phases 5-6) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution first.
- Railway Oriented Programming: Either<EncinaError, T>; errors never swallowed; TimeProvider for time; async with CancellationToken.
- Issue #586: DeadLetterStoreElasticsearch implements IDeadLetterStore (ADR-046) in the new package Encina.Elasticsearch, shared with #577.

IMPLEMENTATION OVERVIEW:
Phase 1: skeleton (if absent), options, document, factory, strict mapping, deterministic ids.
Phase 2: the 11 contract methods (op_type=create for the unique key, conditional update for the claim, delete_by_query) and IDeadLetterStoreSearchable.
Phase 3: options, AddEncinaDeadLetterElasticsearch, health check, DI test.
Phase 4: EventIdRanges.ElasticsearchDeadLetter 6935-6949, log class, architecture test entry.
Phase 5: OpenSearch verification, unit, guard, contract/integration, property tests; manifest.
Phase 6: docs, changelog fragment, ADR.

KEY PATTERNS:
- Store DeadLetterStoreElasticsearch; DI AddEncinaDeadLetterElasticsearch() first-wins.
- Dilemmas D1-D4 are recommended, pending the maintainer: do not start Phase 1 before they are answered.
- ILM caps retention only; DeleteExpiredAsync enforces ExpiresAtUtc.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/ (contract, registration)
- src/Encina.MongoDB/DeadLetter/ (closest implementation)
- tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs
- docs/architecture/adr/046-persistent-dead-letter-queue.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Dead letters must not be cached; reads go to the index. |
| 2 | OpenTelemetry | ✅ | `InstrumentedDeadLetterStore` decorates the store; Phase 4 tests it. |
| 3 | Structured logging | ✅ | Phase 4: range 6935-6949 registered first, error codes and exception types only. |
| 4 | Health checks | ✅ | Phase 3: cluster health as `IEncinaHealthCheck`. |
| 5 | Validation | ✅ | Contract argument rules throw before I/O; input rules live in the orchestrator and manager (ADR-046 decision 6). |
| 6 | Resilience | ✅ | Transient failures map to `Left`; retry follows the client's settings; no extra policy in the store. |
| 7 | Distributed locks | ❌ | The claim is a conditional update, not a lock. |
| 8 | Transactions | ❌ | Elasticsearch has no multi-document ACID transactions; each operation is one document or one query. |
| 9 | Idempotency | ✅ | Deterministic `_id` with `op_type=create`; a repeated capture returns `Right(false)`. |
| 10 | Multi-tenancy | ✅ | `tenantId` keyword field filtered only when the filter names it (ADR-046 Choice 7); per-tenant indices are not planned. |
| 11 | Module isolation | ❌ | `IDeadLetterMessage` has no `ModuleId` today. |
| 12 | Audit trail | ⏭️ | Operator replay and delete audit is #2013 (issue exists); the index is not the audit store. |

---

## Prerequisites & Dependencies

- #583 (closed) fixed the contract; #577 (open) shares the package; #2013 (open) later adds `ActorId` and `CausationId` to the record (the mapping then gains two keyword fields, a reindex-free addition because the mapping is `dynamic: strict` and updated by a new field).
- `Elastic.Clients.Elasticsearch` and `Testcontainers.Elasticsearch` (and `Testcontainers.OpenSearch` for the Phase 0 check) must be added to `Directory.Packages.props`.
- No decision of another issue blocks this plan; D1-D4 block Phase 1.

---

## Next Steps

1. The maintainer answers D1 to D4 when work on #586 starts (D1 together with #577); the answers go into `## Maintainer Decisions`.
2. Re-check the Design Choices against main and the state of #577; `adversarial-reviewer` verifies the Settled ones.
3. Brief a worker per proposed pull request (3 pull requests).
4. Final commit references `Fixes #586`.
