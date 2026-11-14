# Implementation Plan: `DeadLetterStoreElasticsearch` — Elasticsearch/OpenSearch backend for the persistent dead letter queue

> **Issue**: [#586](https://github.com/dlrivada/Encina/issues/586)
> **Type**: Feature
> **Complexity**: Medium-High (6 phases, 1 new package shared with #577, ~18 source files + tests)
> **Estimated Scope**: ~1,200-1,600 lines of production code + ~1,800-2,400 lines of tests

---

## Summary

Implement `Encina.Messaging.DeadLetter.IDeadLetterStore` on Elasticsearch as `DeadLetterStoreElasticsearch`, so that dead letters can be searched (full text over error codes, request types, exception types and stack traces), aggregated and retained with an index lifecycle policy, while the store keeps every guarantee of the ADR-046 contract: oldest-first paged reads, exact counts, an atomic replay claim, the unique `(SourcePattern, SourceMessageId)` key (`AddAsync` returns `Right(false)` for a repeat), set-based deletes and expiry from `TimeProvider`. An optional capability interface adds `SearchAsync` and `GetStatisticsAsync`.

What the code looks like today (main `18ec8dd19`):

- There is no `Encina.Elasticsearch` package, no Elasticsearch or OpenSearch client in `Directory.Packages.props`, and no such service in `docker-compose.yml`. The package is shared with #577 (audit store for Elasticsearch/OpenSearch); who owns the skeleton is Dilemma D6.
- The contract is `IDeadLetterStore` (`src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs`, 10 methods, `Either<EncinaError, T>`). The issue's sample used a `DeadLetterMessage` type, `f.ErrorMessage` (the field is `ErrorCode`; `ExceptionMessage` was removed from the record because exception text can hold personal data, ADR-046), `f.Source` (it is `SourcePattern`) and `f.CreatedAtUtc` (it is `DeadLetteredAtUtc`). Searchable text is therefore `ErrorCode`, `RequestType`, `ExceptionType` and `ExceptionStackTrace`; request content (`RequestContent`) is not indexed for search.
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

- Phase 0 is a spike that runs before the first pull request, not a late test: it runs the dead letter operations (create, conditional update, `delete_by_query`, search) with the Elasticsearch client against a locally started OpenSearch container and records the result in the issue. If they work, OpenSearch is supported at no cost and a Phase 5 integration case keeps it so; if they do not, OpenSearch becomes a follow-up issue (option B) and does not block the 1.0 store. The spike needs no fixture or committed test.
- B wins when AWS OpenSearch Service users are a named audience; C wins if the maintainer decides the project's target is OpenSearch only. The decision is shared with #577, which uses the same package.

</details>

<details>
<summary><strong>2. The document `_id` (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The message `Id`** (a GUID) | `GetAsync` is a real-time get by id | Uniqueness of `(SourcePattern, SourceMessageId)` needs a second mechanism (a search before indexing is racy between hosts) |
| **B) A deterministic id from the source key** (`{SourcePattern}:{hash(SourceMessageId)}`) with `op_type=create` | Atomic uniqueness: a second capture gets HTTP 409 and returns `Right(false)` | `GetAsync(Guid)` becomes a term search on the stored `id` field (near-real-time: consistent only with the refresh policy of Design Choice 3), and `DeleteAsync(Guid)` has no `_id` to delete by: it is a term search on `id` followed by a delete by the found `_id` (or a `delete_by_query` on `id`); `refresh` applies to `delete_by_query` too |

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
<summary><strong>5. Contract, mapping and expiry (ILM is not the expiry mechanism)</strong></summary>

**Class:** Settled
**Settled by:** ADR-046 (decisions 1, 5, 6, 7, Choice 6), ADR-001, ADR-006, AGENTS.md section 3 (TimeProvider; errors never swallowed)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Full ADR-046 contract; filter keys (`requestType`, `errorCode`, `correlationId`, `sourcePattern`, `sourceMessageId`, `tenantId`) as case-sensitive `keyword` fields (same comparison semantics as PostgreSQL and MongoDB, ADR-046 decision 1); `ErrorCode`, `RequestType`, `ExceptionType`, `ExceptionStackTrace` also `text` for search; `DeleteExpiredAsync` authoritative for the per-document `ExpiresAtUtc`** | Provider coherence; the only option that satisfies the contract | The mapping is fixed before the first release (ADR-046 reasoning: a mapping change after 1.0 is a reindex) |
| **B) ILM as the only expiry** | No delete code | ILM acts on indices (rollover, age, size) and cannot match `ExpiresAtUtc <= now` per document, so the contract fails |

### Chosen Option: **A — the full contract; ILM never replaces `DeleteExpiredAsync`**

### Rationale

- The issue says an ILM policy keyed on `ExpiresAtUtc` is configuration; the vendor documentation shows ILM acts on indices, so the plan corrects it: only option A satisfies ADR-046 (a per-message expiry that returns a count).
- The optional ILM backstop (`IlmMaxAge`) is **off by default** and is data loss by design: an index-age delete removes dead letters that were never replayed, whatever their `ExpiresAtUtc`. The feature page and the options XML docs say so. When the index is missing and `CreateIndexIfMissing` is `false`, every operation returns a `Left` (`dlq.index_missing`, fail closed), never an implicit create.

</details>

<details>
<summary><strong>6. Claim and set-based delete mechanisms (Dilemma D5)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Claim by get plus index with `if_seq_no`/`if_primary_term`; set-based deletes by `delete_by_query` (`conflicts=proceed`)** | Optimistic concurrency documented by the vendor; a lost claim is HTTP 409, `Right(false)`; the delete returns a count | Two round trips per claim; `delete_by_query` is not atomic across documents (any `failures` entry makes the call a `Left`, the count is the `deleted` field) |
| **B) Claim by a scripted conditional `_update` (painless script checks `ReplayedAtUtc` and the claim window and sets `ReplayClaimedAtUtc`, `ctx.op = 'noop'` when not won)** | One round trip, the condition runs on the server next to the data | Script maintenance and an inline script permission on managed clusters; harder to unit test; the script language differs on OpenSearch |

### Chosen Option: **A — `if_seq_no`/`if_primary_term` and `delete_by_query`** (recommended, pending the maintainer)

### Rationale

- A needs no scripting permission and behaves the same on OpenSearch (Design Choice 1), and its conflict path maps directly to `Right(false)`.
- B wins when a claim path is hot enough that the second round trip matters, or when scripts are already allowed on the target cluster.

</details>

<details>
<summary><strong>7. Package placement and ownership of the shared skeleton (Dilemma D6)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One shared `Encina.Elasticsearch` package; the skeleton (csproj, `Directory.Packages.props` pins, `Encina.slnx` row, options, client registration, PublicAPI files, manifest, README) is its own first pull request owned by #586; #577 depends on it and rebases (a hard prerequisite, stated in both issues)** | One package and one client registration for both issues; no two pull requests create the same csproj, props entries, slnx rows and manifest | #577 waits for the skeleton of #586 |
| **B) The same shared package, with #577 owning the skeleton and #586 rebasing** | #577 is the lower number | #586 waits; the skeleton is shaped by the audit store |
| **C) A separate package per feature** (`Encina.Elasticsearch.DeadLetter`) | Independent versions, no ordering between the issues | Two packages to configure one cluster |

### Chosen Option: **A — one package, skeleton owned by #586 as its own first pull request** (recommended, pending the maintainer)

### Rationale

- "The first to merge creates the package" is a coordination defect: both pull requests would create the same project and conflict, and neither may depend on an unmerged package of the other. Naming one owner and making the skeleton a standalone first pull request removes the race. The name `DeadLetterStoreElasticsearch` (the full provider name, not `Elastic`) is fixed by `AGENTS.md` section 4.
- B wins if #577 is implemented first; C wins if the maintainer wants every Elasticsearch capability to be an independently versioned opt-in package. `Elastic.Clients.Elasticsearch` and `Testcontainers.Elasticsearch` are not pinned yet: the skeleton adds them to `Directory.Packages.props` at the vendor LTS version (project default-images rule).

</details>

<details>
<summary><strong>8. Paging beyond `max_result_window` (Dilemma D7)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `from`/`size` while `skip + take <= MaxResultWindow`, `search_after` on (`deadLetteredAtUtc`, `id`) with a point in time beyond it** | Any `skip >= 0` the contract allows works (`ReplayAllAsync` and `DeleteAllAsync` page through the whole queue); no error for a valid request | A deep skip walks the pages before it (cost grows with `skip`); a point in time is held during the walk |
| **B) `from`/`size` only; beyond the window the store returns a `Left` (`dlq.page_beyond_window`, category `InvalidRequest`) and the limit is a documented contract deviation** | Smallest code | A valid request of the contract fails on a large queue; the queue-wide operations stop at the window |

### Chosen Option: **A — `search_after` beyond the window** (recommended, pending the maintainer)

### Rationale

- The contract allows any `skip >= 0` with `take <= DeadLetterStoreLimits.MaxPageSize`; a store that returns an error for a valid request is not a provider that differs only in implementation (`AGENTS.md` section 5). `MaxResultWindow` (default 100,000, the cluster's own default) stays an option and the mapping sets it.
- B wins if the maintainer accepts a documented deviation for very large queues.

</details>

---

## Implementation Phases

### Phase 0: OpenSearch compatibility spike (before any pull request)

> **Goal**: decide Design Choice 1 with evidence before the first pull request; a gate never depends on a later phase.

<details>
<summary>Tasks</summary>

1. Start a local OpenSearch container (vendor LTS image) and an Elasticsearch container; with a throwaway console script (not committed) run, through `Elastic.Clients.Elasticsearch`, the operations the store uses: index with `op_type=create` (second call returns 409), index with `if_seq_no`/`if_primary_term` (stale values return 409), `delete_by_query` with `conflicts=proceed` and its `failures` field, a bool query with term/range/exists, `search_after` with a point in time, sort by `deadLetteredAtUtc` then `id`.
2. Record the result per server in the issue; failures against OpenSearch mean OpenSearch becomes a follow-up issue (Design Choice 1 option B) and the store ships for Elasticsearch only.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 0</strong></summary>

```
CONTEXT:
- Issue #586 (DeadLetterStoreElasticsearch) must decide whether the Elasticsearch client also works against OpenSearch.

TASK:
Run the operations listed in the Phase 0 Tasks against both servers with a throwaway script and record the observed results.

KEY RULES:
- No production code and no pull request; the output is a comment on the issue.
- Report a failure; do not work around it.

REFERENCE FILES:
- docs/plans/elasticsearch-dead-letter-store-implementation-plan-586.md (Design Choice 1)
```

</details>

---

### Phase 1: Package, mapping and document model

> **Goal**: the package skeleton (its own first pull request, owned by this issue per Design Choice 6 option A), the index mapping and the document type (the second pull request).

<details>
<summary>Tasks</summary>

1. `src/Encina.Elasticsearch/Encina.Elasticsearch.csproj`, options (`Uri`/cloud id, credentials: `[JsonIgnore]` and a `ToString()` that prints none of them), `AddEncinaElasticsearch` (single `ElasticsearchClient`, `TryAddSingleton`), PublicAPI files, add to `Encina.slnx`, pins in `Directory.Packages.props`.
2. `src/Encina.Elasticsearch/DeadLetter/DeadLetterDocument.cs`: public sealed class implementing `IDeadLetterMessage`; `DeadLetterMessageFactory.cs`.
3. `DeadLetterIndexMapping.cs`: explicit mapping (keyword and text fields of Design Choice 5, `date` for the instants, `dynamic: strict`), settings (`max_result_window` from the options, default 100,000), optional ILM policy and rollover alias created on startup only when `CreateIndexIfMissing` is true. The `date` type stores milliseconds while the orchestrator writes `DateTime` ticks, so the store truncates every instant to the millisecond before writing and says so in its XML docs (the `ExpiresAtUtc <= now` boundary can move by up to 1 ms; MongoDB behaves the same); the contract suite uses whole-second timestamps and passes.
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

> **Goal**: the 10 contract methods (`AddAsync`, `GetAsync`, `GetMessagesAsync`, `GetCountAsync`, `TryClaimForReplayAsync`, `MarkAsReplayedAsync`, `DeleteAsync`, `DeleteManyAsync`, `DeleteExpiredAsync`, `SaveChangesAsync`) and the searchable surface.

<details>
<summary>Tasks</summary>

1. `src/Encina.Elasticsearch/DeadLetter/DeadLetterStoreElasticsearch.cs`: `sealed class DeadLetterStoreElasticsearch : IDeadLetterStore`; constructor `(ElasticsearchClient client, TimeProvider timeProvider, ILogger<DeadLetterStoreElasticsearch> logger, IOptions<DeadLetterElasticsearchOptions>? options = null)`.
2. `AddAsync`: index with `op_type=create`, refresh per option; HTTP 409 with the same stored source key means `Right(false)`; a 409 whose stored key differs is a hash collision, a `Left` (`dlq.key_collision`), never `Right(false)`; any other failure is `Left` (`dlq.store_failed`, exception type or status code only).
   - **ErrorCategory (ADR-062)**: `Encina.Elasticsearch` is a new package outside `ErrorCategoryPending.txt`, so `ErrorCategoryCompletenessStaticScanTests` fails on the first uncategorized `EncinaErrors.Create` in `src/Encina.Elasticsearch`; every code is created with its category: `dlq.store_failed` `Internal` (R4: store and provider failures), `dlq.key_collision` `Internal` (an invariant of the id scheme), `dlq.delete_failed` `Internal` (a `failures` entry of `delete_by_query`: the call is a `Left`, never success), `dlq.index_missing` `Internal` (server-side wiring, R1), `dlq.store_unavailable` `Unavailable` (an unreachable cluster, retryable), `dlq.page_beyond_window` `InvalidRequest` (only if Design Choice 8 option B is picked). A lost claim and a repeated capture are `Right(false)`, not errors.
3. `DeadLetterQueryBuilder.cs` (internal): translates `DeadLetterFilter` to a bool query (term, range, `exists` for `ExpiresAtOrBeforeUtc`, `ExcludeReplayed` as `ReplayedAtUtc` missing or present), one small method per predicate group.
4. `GetAsync` (term on `id`, near-real-time: consistent through the refresh policy of Design Choice 3), `GetMessagesAsync` (sort `deadLetteredAtUtc` then `id`, `newestFirst` reverses both; `from`/`size` while `skip + take <= MaxResultWindow`, `search_after` with a point in time beyond it, Design Choice 8, so a valid `skip` never becomes a `Left`), `GetCountAsync` (`_count`).
5. `TryClaimForReplayAsync`: get the document with its `_seq_no`/`_primary_term`, test the claim conditions with `claimExpiredBeforeUtc`, index with `if_seq_no`/`if_primary_term`; 409 means a lost claim, `Right(false)`. `MarkAsReplayedAsync` is the same pattern and updates only while `ReplayedAtUtc` is null.
6. `DeleteAsync(Guid)`: the `_id` is derived from the source key, not from the message `Id`, so the store finds the document with a term search on `id` and deletes it by its `_id` (or a `delete_by_query` on `id`); `DeleteManyAsync`/`DeleteExpiredAsync` (`delete_by_query`, `conflicts=proceed`, any `failures` is a `Left`); `refresh` applies to every delete too, so a delete is visible to the next read; `SaveChangesAsync` returns `Right(Unit)`.
7. Searchable surface: `IDeadLetterStoreSearchable` (Design Choice 4), implemented by `DeadLetterStoreElasticsearch` (a `partial` class file `DeadLetterStoreElasticsearch.Searchable.cs`, so the scoped forwarding of Phase 3 resolves the concrete store) with `SearchAsync(string query, ...)` (multi-match over the text fields, bounded `size`) and `GetStatisticsAsync(DateTime fromUtc, DateTime toUtc, ...)` (aggregations by error code, source pattern, day).

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
2. `AddEncinaDeadLetterElasticsearch(this IServiceCollection, Action<DeadLetterElasticsearchOptions>? = null, Action<DeadLetterOptions>? = null)`: options, `TryAddSingleton(TimeProvider.System)`, `AddDeadLetterQueueServices<DeadLetterStoreElasticsearch, DeadLetterMessageFactory>(true, options)`, plus `TryAddScoped<DeadLetterStoreElasticsearch>()` and `AddScoped<IDeadLetterStoreSearchable>(sp => sp.GetRequiredService<DeadLetterStoreElasticsearch>())`: a scoped forwarding registration (the store is `TryAddScoped`, so a singleton forward would capture a scoped service, and the `IDeadLetterStore` registration may be decorated, so the forward targets the concrete type; the store is stateless, so two instances in one scope are equivalent).
3. Health: `ElasticsearchHealthCheck : IEncinaHealthCheck` over `_cluster/health` (green/yellow healthy, red unhealthy), const `DefaultName`, static `Tags`; the `ElasticsearchClient` is a singleton, so it is injected by the constructor (no scope).
4. DI test with `ValidateOnBuild` and `ValidateScopes`, both registration orders, and an application's own store kept.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Queue registration is shared: AddDeadLetterQueueServices in src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:495 (first registration wins; the store is registered TryAddScoped).

TASK:
Create the options, the registration, the health check and the DI test of Phase 3.

KEY RULES:
- Registration completeness: every option type and dependency the store resolves is registered; proven with ValidateOnBuild and ValidateScopes.
- Everything is opt-in; the store never overrides the application's own registration.
- Health check pattern: DefaultName const, Tags static array; the ElasticsearchClient is a singleton, so it is injected directly (no scope).
- IDeadLetterStoreSearchable is a scoped forwarding registration, never a singleton.

REFERENCE FILES:
- src/Encina.Kafka/Health/KafkaHealthCheck.cs
- src/Encina.MongoDB/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 4: Cross-cutting integration and observability

<details>
<summary>Tasks</summary>

1. Register `ElasticsearchDeadLetter = (6935, 6949)` in `src/Encina/Diagnostics/EventIdRanges.cs` first (this family's block), add the assembly to `AssemblyRanges` in `EncinaEventIdAllocationTests.cs`, and the field to `src/Encina/PublicAPI.Unshipped.txt`. These files and the log class land with the store (PR 3 of the split), because the store logs through them.
2. `Diagnostics/DeadLetterElasticsearchLog.cs`: `[LoggerMessage]` ids packed from the start of the range: 6935 store failure by operation, 6936 claim lost, 6937 duplicate capture, 6938 key collision, 6939 `delete_by_query` partial failure, 6940 index missing, 6941 mapping created, 6942 ILM policy created. Ids 6943-6949 stay reserved and unused (the range is registered whole; no gaps inside the used ids).
3. `InstrumentedDeadLetterStore` decorates the store; add a test. Tenant (ADR-054 D4): the tenant is a span attribute and a metric tag when multi-tenancy is on; `InstrumentedDeadLetterStore` on main never carries it, so this plan **defers** the tenant tag on the decorator (a decorator-wide change for every store; issue to open at implementation time), and the store never writes the tenant id into logs. The tenant is a stored `keyword` field and a filter value; the `_id` is derived from `(SourcePattern, SourceMessageId)` only (the ADR-046 unique key has no tenant part), so no tenant-qualified id exists to collide.

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
- Never log EncinaError.Message or exception messages (the tenant tag on telemetry is deferred, Phase 4 task 3).
- Run EncinaEventIdAllocationTests.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Kafka/Log.cs
```

</details>

---

### Phase 5: Testing

> **Goal**: every flag reaches its target in `.github/coverage-manifest/Encina.Elasticsearch.json` (new, created by the skeleton pull request of Design Choice 6; each pull request adds the rows of the files it contains, targets never go down).

Per-file targets (percent of coverable lines per flag; contract is 0 for every file because the derived `DeadLetterStoreContract` class needs a real server and runs in `Encina.IntegrationTests`, where it counts for the integration flag):

| File | Unit | Guard | Integration | Property | Justification |
|------|-----:|------:|------------:|---------:|---------------|
| `DeadLetterStoreElasticsearch.cs` | 80 | 90 | 85 | 0 | Mocked-client unit tests reach the branches and the container suite reaches the real index, update and delete calls; the oracle property test targets the query builder. |
| `DeadLetterStoreElasticsearch.Searchable.cs` (search and statistics) | 80 | 90 | 80 | 0 | Query construction is unit-tested, the aggregations only make sense against a real server. |
| `DeadLetterQueryBuilder.cs` | 95 | 0 | 70 | 90 | Internal type (no `InternalsVisibleTo` for the guard project), pure translation of every filter field; the property suite compares it with an in-memory oracle. |
| `DeadLetterDocument.cs` | 90 | 0 | 80 | 0 | Data class with `IsReplayed`/`IsExpiredAt` logic and no validated parameters. |
| `DeadLetterMessageFactory.cs` | 95 | 90 | 70 | 0 | One `Create` method with a null check. |
| `DeadLetterIndexMapping.cs` | 85 | 0 | 80 | 0 | Builds mapping and settings from options (no validated parameters); the integration suite creates a real index with it. |
| `DeadLetterIds.cs` | 95 | 90 | 60 | 90 | Pure hash and collision guard; the property suite proves stability and uniqueness. |
| `DeadLetterElasticsearchOptions.cs` | 90 | 0 | 0 | 0 | Properties with defaults, no parameters, no I/O. |
| `DeadLetterElasticsearchLog.cs` | 70 | 0 | 0 | 0 | Source-generated `[LoggerMessage]` partials reached through the store. |
| `ElasticsearchHealthCheck.cs` | 85 | 90 | 70 | 0 | Unit tests mock green/yellow/red; integration reaches the real `_cluster/health`. |
| `ServiceCollectionExtensions.cs` (package and dead letter) | 90 | 95 | 0 | 0 | Registration code reached by the DI test and null guards. |
| `EncinaElasticsearchOptions.cs` | 90 | 0 | 0 | 0 | Properties, `[JsonIgnore]` and `ToString()`; no parameters. |

<details>
<summary>Tasks</summary>

1. **OpenSearch regression case**: the Phase 0 result is kept alive by an integration case against `Testcontainers.OpenSearch` when Phase 0 showed it works (Design Choice 1); otherwise the follow-up issue carries it.
2. **Unit**: query builder (every predicate), id helper (collision guard), mapping, error mapping and its categories, claim outcomes, ms truncation of instants, `search_after` paging; client mocked.
3. **Guard**: every public method and the registration.
4. **Contract/integration**: `DeadLetterStoreContract` against an Elasticsearch container (`Testcontainers.Elasticsearch`, shared `[Collection]` fixture in `Encina.TestInfrastructure`), plus: concurrent claim from two stores, `delete_by_query` with a version conflict, a page beyond `MaxResultWindow`, a missing index with `CreateIndexIfMissing = false` returns `dlq.index_missing`, search and statistics.
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
| `AddDeadLetterQueueServices` | `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:495` (wrappers in `src/Encina.Messaging/DeadLetter/DeadLetterServiceCollectionExtensions.cs`) | First-wins registration |
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
| Package skeleton (Phase 1) | ~11 | csproj, `Directory.Packages.props`, `Encina.slnx` row, options, registration, PublicAPI x2, README, manifest, unit and guard tests |
| Dead letter production code (Phases 1-4) | ~16 | Document, factory, mapping, ids, query builder, store, searchable interface and partial, options, error codes, registration, health, log, `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, package `PublicAPI.Unshipped.txt` |
| Tests (Phases 1-5) | ~16 | Unit, guard, DI test, decorator test, contract/integration, property, container fixture, `AssemblyRanges` test entry, test-project `ProjectReference` edits |
| Manifest rows, feature page, changelog, ADR, INVENTORY, load/benchmark justification files | ~7 | Documentation phase |
| **Total** | **~50** | Exceeds 15 files. Tests and the log class travel with the code that needs them; each pull request builds and passes alone |

Proposed split (each at most 15 files and 1500 lines, counting tests, manifest rows, PublicAPI files, `EventIdRanges.cs`, slnx and props edits and test `ProjectReference` edits):

| PR | Content | Files | Estimated lines | Verifiable alone because |
|----|---------|-------|-----------------|--------------------------|
| 1 | Package skeleton: csproj, `Directory.Packages.props`, `Encina.slnx`, `EncinaElasticsearchOptions`, `AddEncinaElasticsearch`, PublicAPI x2, README, manifest, unit and guard tests (owned by #586; #577 rebases, Design Choice 6) | ~11 | ~450 | The client registration is tested on its own |
| 2 | Document, factory, mapping, ids, query builder, `DeadLetterElasticsearchOptions`, their unit and guard tests, manifest rows, PublicAPI lines | ~11 | ~1,000 | Pure types and options with their own tests |
| 3 | Store (10 contract methods), error codes with categories, log class, `EventIdRanges.cs`, `AssemblyRanges` test entry, `src/Encina/PublicAPI.Unshipped.txt`, package PublicAPI, manifest rows, unit and guard tests | ~13 | ~1,400 | The store builds with its log and options and is tested on a mocked client |
| 4 | `IDeadLetterStoreSearchable` and the partial with search and statistics, unit and guard tests, PublicAPI lines, manifest rows | ~6 | ~600 | Tested on a mocked client |
| 5 | `AddEncinaDeadLetterElasticsearch`, health check, DI test (`ValidateOnBuild`, `ValidateScopes`), health and guard tests, decorator test, manifest rows, PublicAPI lines | ~8 | ~600 | DI test against the store of PR 3 |
| 6 | Container fixture and `Testcontainers.Elasticsearch` pin, test-project `ProjectReference` edits, derived contract class, integration cases, property tests, load and benchmark justification files, manifest rows | ~10 | ~1,200 | Integration and property flags reach their targets on a real server |
| 7 | Package README section, `docs/features/dead-letter-queue.md`, `docs/INVENTORY.md`, changelog fragment, ADR | ~5 | ~400 | Documentation only |

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
Phase 0: OpenSearch compatibility spike (no pull request). Phase 2: the 10 contract methods (op_type=create for the unique key, conditional update for the claim, delete_by_query) and IDeadLetterStoreSearchable.
Phase 3: options, AddEncinaDeadLetterElasticsearch, health check, DI test.
Phase 4: EventIdRanges.ElasticsearchDeadLetter 6935-6949, log class, architecture test entry.
Phase 5: OpenSearch verification, unit, guard, contract/integration, property tests; manifest.
Phase 6: docs, changelog fragment, ADR.

KEY PATTERNS:
- Store DeadLetterStoreElasticsearch; DI AddEncinaDeadLetterElasticsearch() first-wins.
- Dilemmas D1-D7 are recommended, pending the maintainer: do not start Phase 1 before they are answered.
- Every new error code carries its ADR-062 ErrorCategory (the package is not in ErrorCategoryPending.txt).
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
- `Elastic.Clients.Elasticsearch` is added to `Directory.Packages.props` by the skeleton pull request and `Testcontainers.Elasticsearch` (and `Testcontainers.OpenSearch` for the Phase 5 OpenSearch case, if Phase 0 shows it works) by the integration pull request; the Phase 0 spike uses locally started containers and pins nothing.
- No decision of another issue blocks this plan; D1-D7 block Phase 1.

---

## Next Steps

1. The maintainer answers D1 to D7 when work on #586 starts (D1 and D6 together with #577), after the Phase 0 spike; the answers go into `## Maintainer Decisions`.
2. Re-check the Design Choices against main and the state of #577; `adversarial-reviewer` verifies the Settled ones.
3. Brief a worker per proposed pull request (7 pull requests, see the file count).
4. Final commit references `Fixes #586`.
