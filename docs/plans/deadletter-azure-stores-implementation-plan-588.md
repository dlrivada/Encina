# Implementation Plan: Persistent `IDeadLetterStore` for Azure Table Storage and Azure Cosmos DB

> **Issue**: [#588](https://github.com/dlrivada/Encina/issues/588)
> **Type**: Feature
> **Parent**: [#875](https://github.com/dlrivada/Encina/issues/875) (EPIC)
> **Related**: #583 (the ten database dead letter stores, closed), #589 (AWS DynamoDB counterpart), #578 and #582 (Azure audit stores), SPEC-000 DEC-009 (cloud items with a free local emulator are in 1.0)
> **Complexity**: Medium-High (two new packages, two emulators in CI, no relational schema)
> **Estimated Scope**: ~1,400-1,900 lines of production code + ~3,500 lines of tests; exceeds one worker pull request, split proposed in Phase 1

---

## Summary

### What exists today (re-verified on 2026-11-14, main `c871956bf`)

- The contract is `IDeadLetterStore` in `src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs`: every member returns `Either<EncinaError, T>`. `AddAsync(IDeadLetterMessage)` is `Right(true)` when stored and `Right(false)` when `(SourcePattern, SourceMessageId)` already exists; the others are `GetAsync(Guid)`, `GetMessagesAsync(filter, skip, take, newestFirst)`, `GetCountAsync(filter)`, `TryClaimForReplayAsync(id, claimExpiredBeforeUtc)`, `MarkAsReplayedAsync(id, replayResult)`, `DeleteAsync`, `DeleteManyAsync(filter)`, `DeleteExpiredAsync` and `SaveChangesAsync`. The issue's original snippet (`Task AddAsync(DeadLetterMessage ...)`) is stale.
- The ten database providers of `AGENTS.md` section 5 implement it (`DeadLetterStoreADO`, `DeadLetterStoreDapper`, `DeadLetterStoreEF`, `DeadLetterStoreMongoDB`, each with a `DeadLetterMessageFactory`). All of them enforce a **global** unique index on `(SourcePattern, SourceMessageId)` (`029_CreateDeadLetterMessagesTable.sql`, `UX_DeadLetterMessages_Source`), order by `(DeadLetteredAtUtc, Id)` and honour `DeadLetterStoreLimits` (`MaxPageSize = 1000`, field length caps).
- Registration is `AddEncinaDeadLetterQueue<TStore, TFactory>` (`DeadLetterServiceCollectionExtensions.cs:24` and `:47`), a thin wrapper over `AddDeadLetterQueueServices`, which uses `TryAdd*` so the first registered store wins in any order. `Encina.OpenTelemetry` already decorates any `IDeadLetterStore` with `InstrumentedDeadLetterStore` (`ServiceCollectionExtensions.cs:152`), and `DeadLetterHealthCheck` (`Encina.Messaging/Health`) already reports the pending count through whatever store is registered.
- Not present: `Microsoft.Azure.Cosmos` and a Cosmos Testcontainers module in `Directory.Packages.props`. Present: `Azure.Data.Tables` 12.13.0, `Testcontainers.Azurite` 4.15.0 and `Azure.Identity`.

### What this plan delivers

Two stores, `DeadLetterStoreAzureTable` and `DeadLetterStoreCosmosDB`, each with an `IDeadLetterMessageFactory`, options, a registration extension, a connectivity health check and tests against Azurite and the Cosmos DB emulator. Standards covered: the `IDeadLetterStore` contract (shared contract tests), ADR-001/ADR-006 (ROP), ADR-018 (cross-cutting), ADR-021 (EventIds), SPEC-000 DEC-009 (emulators in CI).

- Affected packages: Encina.Messaging (consumed, unchanged), two new packages (names per D1), `Encina` (EventId registry), `Directory.Packages.props`.
- Provider category: Cloud (`AGENTS.md` section 5: the Azure/AWS pair; GCP is post-1.0, SPEC-000 REQ-028). The database matrix is closed by #583.

---

## Design Choices

<details>
<summary><strong>1. Package layout — one package per Azure service, or one shared Azure package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One package per service** (`Encina.Messaging.AzureTable`, `Encina.Messaging.CosmosDB`, the names in the issue) | Pay-for-what-you-use (`AGENTS.md` section 1); an application takes one SDK (`Azure.Data.Tables` or `Microsoft.Azure.Cosmos`); independent versions of the two heavy SDKs | Two README files, two health checks, two connection configurations |
| **B) One shared Azure package** with the audit stores of #578 and #582 | One connection configuration and one health check per Azure account | Pulls both SDKs into every user; couples dead letters to audit release cadence; the audit plans are not settled |

### Chosen Option: **A — One package per service** (recommended, pending the maintainer)

### Rationale

- An application that uses only Table Storage should not restore the Cosmos SDK, which is large. This is the same layout `Encina.Messaging.Encryption.AwsKms` and `Encina.Messaging.Encryption.AzureKeyVault` already use (one package per vendor service).
- B wins when an application runs dead letters and audit on the same Azure account and wants one registration; that gain can be added later as a convenience extension without merging packages.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Key layout of the stores — co-locate the uniqueness rule with the row</strong></summary>

### Options Considered

The contract needs four access paths: by `Id`, by the unique pair `(SourcePattern, SourceMessageId)`, ordered by `(DeadLetteredAtUtc, Id)` with skip and take, and filtered by tenant, source, error code and dates. Table Storage indexes only `(PartitionKey, RowKey)`. Cosmos DB enforces a unique key only **inside one logical partition**, so a globally unique pair needs a partition key derived from the pair. An entity-group transaction (Table Storage) and a transactional batch (Cosmos DB) are atomic only inside one partition.

| Option | Pros | Cons |
|--------|------|------|
| **A) Partition by `SourcePattern`** (the issue's proposal) | The unique rule and the row share a partition, so add is atomic (Table: batch of the row and a `S|{sourceMessageId}` marker; Cosmos: unique key on `/sourceMessageId`); per-source queries touch one partition | One hot partition when a single source dominates (the outbox is the usual one); a tenant query crosses partitions |
| **B) Partition by `TenantId`** (a constant for single-tenant applications), markers `S|{pattern}|{sourceMessageId}` in the same partition | Tenants are spread; tenant queries and tenant deletes are single-partition; add stays atomic | The contract's uniqueness is global, not per tenant: two tenants with the same `(pattern, id)` would both succeed, which differs from the ten database stores unless the source message ids are tenant-unique |
| **C) Hash bucket of `Id`** | Even write distribution | Uniqueness is no longer co-located (two writes, orphan repair needed); every filtered query fans out |

### Chosen Option: **A — Partition by `SourcePattern`** (recommended, pending the maintainer)

### Rationale

- Only A keeps the **global** unique rule of the database stores without a cross-partition write, in both services. `SourcePattern` is **small in practice** (`DeadLetterSourcePatterns` constants: Outbox, Inbox, Saga, Scheduling, Recoverability, and so on) but it is not a closed set in the type system: the column accepts any string (`DeadLetterStoreLimits.SourcePatternMaxLength = 64`, `SourceMessageIdMaxLength = 256`; `DeadLetterInputs` only rejects leading and trailing whitespace). The layout therefore must not assume a closed set: keys are escaped (below) and the partition count is only small by convention.
- The hot-partition risk is real only at the per-partition throughput and size limits of the two services (published in their scalability targets), far above dead letter rates; Phase 2 states the limits it relies on in the package README. B wins in a multi-tenant deployment whose tenants reuse source message ids and want tenant-scoped uniqueness; C wins only at extreme volume.
- **Key encoding (injective, valid)**: Table Storage forbids `/ \ # ?` and control characters in `PartitionKey` and `RowKey` and caps each key at 1 KiB (512 UTF-16 characters). One function `TableKeys.Encode(string)` percent-encodes every forbidden character, the separator `|` and `%` itself, so two different inputs never produce one output (`a|b`/`c` versus `a`/`b|c` stay distinct because `|` is escaped inside each part and the two parts live in different keys: `PartitionKey = Encode(SourcePattern)`, marker `RowKey = "S|" + Encode(SourceMessageId)`). An encoded key longer than 512 characters is rejected before any I/O with the coded `Left` `dlq.key_too_long` (Validation, see "Error categories"). In Cosmos the item `id` is the `Guid` and the partition and unique-key values accept any string (a partition key value up to 2 KB), so no escaping is needed there.
- **Order of writes (no cross-table atomicity)**: Table Storage has no transaction across tables. The add writes the `I|{Id}` pointer (in the pointer table) **first**, then one entity-group transaction with the message row, its index rows (Design Choice 5) and the `S|` marker (one partition, atomic). A crash between the two leaves an orphan pointer, which is harmless: a read that finds a pointer without a row treats the message as absent, and the next add of the same `Id` or pair repairs or removes it (`GetAsync` deletes an orphan pointer it meets). Every delete path (`DeleteAsync`, `DeleteManyAsync`, `DeleteExpiredAsync`) deletes in the reverse order: the partition transaction (row, index rows **and marker**, so the unique pair is freed exactly as the database stores free it) first, then the pointer. A crash after the transaction leaves an orphan pointer again, never a row without a pointer. Phase 5 adds fault-injection tests (a `TableClient` test double that throws between the steps) for both orders.
- Time order inside a partition and the read algorithm are Design Choice 5; Cosmos orders with a composite index on `(/deadLetteredAtUtc, /id)`. Phase 2 validates the Table layout with a spike against Azurite before the rest is written.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Is the Cosmos DB change feed part of this issue</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Include it** (the issue's acceptance criteria) | Real-time notification of new dead letters; operators can push alerts | `IDeadLetterStore` has no notification member, so it needs a new public API, a processor and a lease container; Table Storage and DynamoDB have no equivalent, breaking provider coherence |
| **B) Defer it to its own issue** | Keeps this issue to the store contract; the two stores behave like the other nine | No push alerts on Cosmos until the follow-up lands |

### Chosen Option: **B — Defer to its own issue** (recommended, pending the maintainer)

### Rationale

- `AGENTS.md` section 3 (provider coherence) says providers differ only in implementation; a feed available on one provider only is a new feature of the abstraction, not of a provider. The cross-provider notification question (DynamoDB Streams in #589 is the same) is better answered once, for all providers, in one issue (written with this plan: `artifacts/issues/plan-588-deadletter-change-notification.md`).
- A wins for operators who want push alerts on Cosmos and accept a Cosmos-only feature.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Contract semantics, expiry, replay claim and observability reuse</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Follow the shared contract exactly** (ROP `Left` for infrastructure failure, `TimeProvider`, `ExpiresAtUtc` plus `DeleteExpiredAsync`, claim as one conditional update with ETag, decorator for telemetry) | Passes the same contract tests as the ten database stores; nothing special to document | The services' native features (Cosmos TTL) only supplement the contract |
| **B) Use native TTL and change feed as the contract** | Less code on Cosmos | Cosmos TTL removes rows lazily and Table Storage has none, so reads could return expired rows and providers would diverge |

### Chosen Option: **A — Follow the shared contract exactly**

### Rationale

- Errors: `AGENTS.md` section 3 (ROP, errors never swallowed; ADR-001, ADR-006). Time: `TimeProvider` by injection (`AGENTS.md` section 3). Arguments are validated against `DeadLetterStoreLimits` before any I/O, as the database stores do.
- Claim: the contract condition is `ReplayedAtUtc IS NULL AND (ReplayClaimedAtUtc IS NULL OR ReplayClaimedAtUtc <= claimExpiredBeforeUtc)`. Neither service evaluates a predicate on update, so `TryClaimForReplayAsync` is a **read, a client-side evaluation of that condition, then one ETag-conditional write**: Table Storage `UpdateEntityAsync` with `ETag` (`If-Match`), Cosmos `ReplaceItemAsync` with `IfMatchEtag`. A condition that is false on the read is `Right(false)`; a 412 on the write means a concurrent claimer won and is also `Right(false)`; `Right(true)` goes only to the writer whose ETag matched. `MarkAsReplayedAsync` is likewise ETag-conditional on `ReplayedAtUtc` being unset (a 412 re-reads once and answers per the contract). The concurrent-claim integration test (Phase 5) proves exactly one winner.
- Expiry: a read filters `ExpiresAtUtc` against `TimeProvider`; Cosmos TTL may be enabled as an extra, never as the contract.
- Telemetry: `InstrumentedDeadLetterStore` decorates every `IDeadLetterStore` (`Encina.OpenTelemetry/ServiceCollectionExtensions.cs:152`), so the new packages add no store-level `Meter`; the issue's proposed `ActivitySource` is replaced by that decorator. ADR-054 D4 (tenant tags on telemetry): that decorator never carries the tenant (its XML says so) and these packages emit no telemetry of their own, so they add no tenant tag.
- Error categories (ADR-062, `EncinaErrors.Create(code, ErrorCategory.X, ...)`, read back with `GetCategory`): see "Error categories" below; only the code, never the SDK exception message, is stored, logged or tagged.
- Secrets: the options hold a connection string or account key, so those properties are `[JsonIgnore]` and `ToString()` is overridden (`AGENTS.md` section 3, #851).

**Class:** Settled
**Settled by:** AGENTS.md section 3 (ROP, TimeProvider, secrets in options), ADR-001, ADR-006, the `IDeadLetterStore` XML contract, and `Encina.OpenTelemetry/ServiceCollectionExtensions.cs:152`

</details>

<details>
<summary><strong>5. Read algorithm and order on Azure Table Storage</strong></summary>

### Options Considered

The contract orders by `(DeadLetteredAtUtc, Id)` globally, ascending or descending (`newestFirst`), with `skip` and `take`. Table Storage returns rows in `(PartitionKey, RowKey)` order only and only ascending; the partition is `SourcePattern`, so an unfiltered listing is ordered by source first, not by time.

| Option | Pros | Cons |
|--------|------|------|
| **A) Per-source ascending queries merged client side, plus key-only index rows for newest-first** | Correct global order in both directions; reads only what a page needs | Index rows `N|{inverted ticks}|{Id}` (key-only, same partition, same atomic batch) add write cost; `skip` costs `skip + take` rows per source (a k-way merge bounded by the source count); more code |
| **B) Read whole partitions and sort client side** | One row family, simplest code | Cost grows with the stored dead letters, not with the page; unbounded memory on a big backlog |

### Chosen Option: **A — merge per source, index rows for the opposite direction** (recommended, pending the maintainer)

### Rationale

- A serves the contract for both directions at a cost bounded by the page; the price is the documented cost of `skip` (it must read `skip + take` rows) and a larger layout. `GetCountAsync` is a count over the same filtered per-source queries. B is the choice when the backlog is known to stay small (hundreds of rows) and simplicity matters more than a bounded read.
- A property test (Phase 5) with several sources and both directions compares the Table result with a reference ordering.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Oversized payloads on the two services</strong></summary>

### Options Considered

`DeadLetterStoreLimits` caps no payload field (`RequestContent`, the stack trace, context), while a Table string property holds at most 64 KiB (32K characters) and an entity 1 MiB, and a Cosmos item 2 MiB. The ten database stores accept what these stores would refuse.

| Option | Pros | Cons |
|--------|------|------|
| **A) Reject before I/O with a coded `Left` (`dlq.payload_too_large`, Validation)** | Predictable, no partial write; the limit is documented per service | A dead letter that other stores accept is refused here; the caller must handle the code |
| **B) Split large values into chunk properties (Table) and rely on the 2 MiB item (Cosmos)** | Accepts larger payloads on Table | Chunking code, a second failure mode for payloads over 1 MiB anyway; behaviour still diverges above the entity limit |
| **C) Truncate with a marker** | Never refuses | Loses the data a dead letter exists to preserve; the replay would send a corrupt request |

### Chosen Option: **A — Reject before I/O with a coded `Left`** (recommended, pending the maintainer)

### Rationale

- A dead letter that cannot be replayed faithfully must not be stored half; A is the only option that keeps the data intact without new machinery. B wins if large payloads are expected on Table Storage. A contract test with an oversized payload proves the behaviour; the limit is stated in the README.

**Class:** Dilemma

</details>

### Error categories (ADR-062)

Every `Left` is created with `EncinaErrors.Create(code, category, ...)` and a `GetCategory` test asserts each row. The existing `dlq.*` store codes keep one category each; the transient SDK outcomes get **their own codes** so one code never carries two categories (the completeness scan of ADR-062 fails otherwise). The new codes are added to `DeadLetterErrorCodes` by a small shared prerequisite PR (S0 below) that the DynamoDB plan (#589) reuses.

| Code | Category | Used for |
|------|----------|----------|
| `dlq.store_failed`, `dlq.get_failed`, `dlq.query_failed`, `dlq.claim_failed`, `dlq.mark_replayed_failed`, `dlq.delete_failed` | `Internal` | Non-transient SDK faults on the matching operation |
| `dlq.store_throttled` (new) | `RateLimited` | Cosmos 429, Table Storage 503 `ServerBusy` and 429-style throttling after the SDK's own retries |
| `dlq.store_unavailable` (new) | `Unavailable` | Connection failure, DNS, 5xx other than throttling |
| `dlq.store_timeout` (new) | `Timeout` | `TaskCanceledException`/`RequestFailedException` timeout that is not the caller's token |
| `dlq.payload_too_large`, `dlq.key_too_long` (new) | `Validation` | Rejected before any I/O (Design Choices 6 and 2) |

An `OperationCanceledException` from the caller's own token still propagates as cancellation, never as a `Left`. The code, never the SDK message, is the only thing stored, logged or tagged.

---

## Implementation Phases

### Phase 1: Foundations — Packages, Dependencies, EventIds, Split

> **Goal**: Create the build scaffolding both stores need, and fix the pull request split.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Messaging.AzureTable/Encina.Messaging.AzureTable.csproj` and `src/Encina.Messaging.CosmosDB/Encina.Messaging.CosmosDB.csproj` (names per D1; copy the layout of `src/Encina.Messaging.Encryption.AzureKeyVault`), each with `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, `GlobalSuppressions.cs` if needed, a README and a row in `Encina.slnx`.
2. **Modify** `Directory.Packages.props`: add `Microsoft.Azure.Cosmos` and the Cosmos DB Testcontainers module (`Testcontainers.CosmosDb`).
3. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: add `MessagingDeadLetterAzureTable = (6800, 6809)` and `MessagingDeadLetterCosmosDB = (6810, 6819)` (block 6800-6849 assigned to this family; this plan uses 6800-6819) and the two entries of the `AssemblyRanges` map of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
4. **Create** `.github/coverage-manifest/Encina.Messaging.AzureTable.json` and `Encina.Messaging.CosmosDB.json` with the per-file, per-flag targets of the table "Per-file coverage targets" (Phase 5), each with its one-sentence justification.
5. **Split** into eight pull requests, each with its own tests and manifest rows so the blocking `crap-gate` can pass (a production-only PR cannot reach any per-flag target), each at most 15 files and 1,500 lines **counting tests, manifests, PublicAPI, `ProjectReference` lines and DI/contract tests**:

   | PR | Content | Files (approx.) |
   |----|---------|-----------------|
   | S0 | Shared error codes (`dlq.store_throttled`, `dlq.store_unavailable`, `dlq.store_timeout`, `dlq.payload_too_large`, `dlq.key_too_long`) in `DeadLetterErrorCodes`, the `EncinaErrors.Create` factory with ADR-062 categories, `PublicAPI.Unshipped.txt` of `Encina.Messaging`, `GetCategory` unit test, manifest row (shared with #589: whichever plan lands first adds it, the other reuses it) | 5 |
   | T1 | Table package foundations: csproj, two PublicAPI files, entity, factory, `TableKeys` (injective encoding), options, `Encina.slnx`, `EventIdRanges.cs`, the allocation-test map, manifest; unit tests (entity, factory, keys, options) and guard tests | 14 |
   | T2 | Table store and registration: store, `ServiceCollectionExtensions`, `Log.cs`, health check, PublicAPI update; unit tests with a fault-injecting `TableClient` double (including the crash-between-steps tests), DI test (`ValidateOnBuild`, `ValidateScopes`, pre-registered client, both registration orders), guard tests | 10 |
   | T3 | Table contract, property (ordering, key injectivity), integration tests with the Azurite collection fixture, two load/benchmark justification `.md` files, manifest update | 8 |
   | C1 | Cosmos foundations: `Directory.Packages.props` (`Microsoft.Azure.Cosmos`, `Testcontainers.CosmosDb`; restore with no NU190x warning, central pinning per ADR-060 if needed), csproj, PublicAPI, document entity, factory, options, `Encina.slnx`, `EventIdRanges.cs`, allocation-test map, manifest; unit and guard tests | 14 |
   | C2 | Cosmos store and registration (as T2) | 10 |
   | C3 | Cosmos contract, property, integration tests with the emulator fixture, justification files, manifest update | 8 |
   | D | Documentation: two READMEs, the feature page, `docs/INVENTORY.md`, changelog fragment; this PR carries `Fixes #588` | 5 |

   The PRs T and C are independent after S0; line counts are re-measured when each PR is briefed.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0) needs persistent IDeadLetterStore implementations for Azure Table
Storage and Azure Cosmos DB (issue #588). The contract is src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs.
New satellite packages follow the layout of src/Encina.Messaging.Encryption.AzureKeyVault.

TASK:
1. Create the two projects (names per the maintainer's answer to D1) with PublicAPI files, README,
   slnx entries. Reference Encina.Messaging and the SDK package only.
2. Add Microsoft.Azure.Cosmos and Testcontainers.CosmosDb to Directory.Packages.props (central versions).
3. Register EventId ranges 6800-6809 and 6810-6819 in EventIdRanges.cs and the AssemblyRanges map of
   EncinaEventIdAllocationTests.
4. Create the coverage manifests with per-flag targets and one-sentence justifications.

KEY RULES:
- ADR-021: the range is registered before any [LoggerMessage] uses it.
- Zero warnings; RS0016/RS0017 handled in PublicAPI.Unshipped.txt.
- No code of the stores in this phase.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina.Messaging.Encryption.AzureKeyVault/ (csproj, README, PublicAPI)
- .github/coverage-manifest/Encina.Messaging.Encryption.AzureKeyVault.json
```

</details>

---

### Phase 2: Azure Table Storage Store

> **Goal**: `DeadLetterStoreAzureTable` passes the shared contract against Azurite.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `DeadLetter/DeadLetterMessage.cs` (the `ITableEntity` implementing `IDeadLetterMessage`, UTC `DateTime` properties named as the interface) and `DeadLetter/DeadLetterMessageFactory.cs` (`Create(DeadLetterData)`; copies values, never serializes).
2. **Create** `DeadLetter/TableKeys.cs` (the injective `Encode` of Design Choice 2 and the 512-character check) and `DeadLetter/DeadLetterStoreAzureTable.cs` implementing `IDeadLetterStore` with the key layout and write order of Design Choice 2: pointer first, then one entity-group transaction of the message row, its index rows and the `S|{Encode(sourceMessageId)}` marker (a conflict on the marker is `Right(false)`; any other cancellation is `Left`); reads per Design Choice 5; claim and replay marking as read, client-side evaluation, ETag-conditional write (Design Choice 4); filtered queries with server-side `Filter` strings and `MaxPageSize` paging; every delete path removes row, index rows and marker, then the pointer; `DeleteManyAsync` in batches of 100 operations (a message is 2 or more operations, so fewer than 50 messages per batch, computed from the rows read); payload size check before I/O (Design Choice 6).
3. **Create** `AzureTableDeadLetterOptions.cs` (account/connection data `[JsonIgnore]` with a redacting `ToString()`, table names, SDK retry options) and `ServiceCollectionExtensions.cs` (`AddEncinaDeadLetterAzureTable(...)` registers `TableServiceClient` as a singleton with `TryAddSingleton`, so a client the application registered first or later wins in any order, and calls `AddEncinaDeadLetterQueue<DeadLetterStoreAzureTable, DeadLetterMessageFactory>`, whose store registration is `TryAdd`). Table creation is idempotent (`CreateIfNotExistsAsync`, a 409 is success) and race-safe across instances on first use; the tables may be provisioned externally, and then creation is skipped by an option (the caller needs no create permission).
4. **Create** `Diagnostics/Log.cs` with `[LoggerMessage]` ids 6800-6809 (grouped: store operations, claim outcome, table creation, failures by error code).
5. **Create** `Health/AzureTableDeadLetterHealthCheck.cs` (`DefaultName` const, static `Tags`, connectivity probe through a scoped `IServiceProvider`).
6. **Update** `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Phase 1 created the Encina.Messaging.AzureTable project and registered EventIds 6800-6809.
The ten database stores (for example src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs) are
the behavioral reference: global unique (SourcePattern, SourceMessageId), order (DeadLetteredAtUtc, Id),
claim = one conditional update, Right(false) for "not found / already replayed / live claim".

TASK:
1. Implement DeadLetterMessage (ITableEntity + IDeadLetterMessage), DeadLetterMessageFactory and
   DeadLetterStoreAzureTable with the key layout of the plan's Design Choice 2 (PartitionKey =
   Encode(SourcePattern); RowKey M|{ticks}|{Id}; marker S|{Encode(SourceMessageId)}; pointer I|{Id}
   in an index table written FIRST; deletes in reverse order incl. the marker) and the read
   algorithm of Design Choice 5. Validate arguments with DeadLetterStoreLimits before any I/O (throw).
2. Every infrastructure failure (RequestFailedException other than 404/409/412) becomes
   Left(EncinaError) created with EncinaErrors.Create and the ADR-062 category of the plan's
   "Error categories" table; never put exception messages in the error.
3. Options with [JsonIgnore] on secrets and a redacting ToString(); registration extension using
   TryAdd so a store registered by the application wins in any order.
4. Log.cs with [LoggerMessage] in 6800-6809; health check class.

KEY RULES:
- TimeProvider injected (optional parameter defaulting to TimeProvider.System); never DateTime.UtcNow.
- All SDK calls async with CancellationToken.
- Do not swallow a Left from the SDK path.
- AGENTS.md section 7: exception to logger goes through ForLogging().

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs, DeadLetterFilter.cs, DeadLetterStoreLimits.cs
- src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs
- src/Encina.Messaging/DeadLetter/DeadLetterServiceCollectionExtensions.cs
- src/Encina.Messaging/Health/DeadLetterHealthCheck.cs
```

</details>

---

### Phase 3: Azure Cosmos DB Store

> **Goal**: `DeadLetterStoreCosmosDB` passes the shared contract against the Cosmos DB emulator.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `DeadLetter/DeadLetterMessage.cs` (JSON document with `id` = the `Guid`, `sourcePattern` as partition key, camelCase names via `[JsonPropertyName]`) and `DeadLetterMessageFactory.cs`.
2. **Create** `DeadLetter/DeadLetterStoreCosmosDB.cs`: container with partition key `/sourcePattern`, unique key `/sourceMessageId` (partition and unique-key values accept any string, no escaping), composite index `(/deadLetteredAtUtc, /id)`; add via `CreateItemAsync` (409 is `Right(false)`); claim as read, client-side evaluation of the contract condition, then `ReplaceItemAsync` with `IfMatchEtag` (412 is `Right(false)`), replay marking ETag-conditional likewise (Design Choice 4); queries with parameterized SQL (never string concatenation) and `OFFSET/LIMIT`; `DeleteManyAsync` by transactional batch per partition (a delete frees the unique key with the item); payload size check against the 2 MiB item before I/O (Design Choice 6); 429 maps to `dlq.store_throttled`, 5xx and transport faults to `dlq.store_unavailable`, timeouts to `dlq.store_timeout`.
3. **Create** options (connection string/key `[JsonIgnore]`, database and container names, `CosmosClientOptions` retry), `ServiceCollectionExtensions.cs` (`AddEncinaDeadLetterCosmosDB` registers `CosmosClient` with `TryAddSingleton` so the application's client wins in any order; database and container creation is idempotent via `CreateIfNotExistsAsync` and race-safe on first use), `Diagnostics/Log.cs` with ids 6810-6819 and `Health/CosmosDBDeadLetterHealthCheck.cs`.
4. **Optional TTL**: `defaultTtl` on the container as a supplement to `DeleteExpiredAsync` (documented, off by default).
5. **Update** `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Phase 1 added Microsoft.Azure.Cosmos and EventIds 6810-6819. Cosmos unique keys apply inside one
logical partition, so the container uses partition key /sourcePattern with unique key
/sourceMessageId to keep the global (SourcePattern, SourceMessageId) rule of the database stores.

TASK:
1. Implement DeadLetterMessage (document), factory and DeadLetterStoreCosmosDB per the plan.
2. Container creation (CreateContainerIfNotExistsAsync) with the unique key and composite index; the
   emulator needs HTTP gateway mode and a trusted-certificate handler in tests only.
3. Parameterized queries only; map CosmosException 404/409/412 as in the plan, every other failure
   to Left(EncinaError) carrying the error code, never the exception message.
4. Options, registration (TryAdd), Log.cs (6810-6819), health check.

KEY RULES:
- TimeProvider injected; ExpiresAtUtc filtered on read.
- Secrets [JsonIgnore] + redacting ToString().
- CancellationToken on every SDK call; no .Result/.Wait().

REFERENCE FILES:
- The Phase 2 files of Encina.Messaging.AzureTable (same shape)
- src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs
- src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs
```

</details>

---

### Phase 4: Cross-Cutting Integration and Observability

> **Goal**: Registration completeness, telemetry reuse, resilience and multi-tenancy.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** DI tests building the provider with `ValidateOnBuild` and `ValidateScopes` for both registrations, in both registration orders, with an application-registered store winning and with a pre-registered `TableServiceClient`/`CosmosClient` winning (`AGENTS.md` section 3, registration completeness). They travel in the T2 and C2 pull requests.
2. **Verify** that `AddEncinaOpenTelemetry` decorates the new stores through the `IDeadLetterStore` descriptor with a test (no new `Meter`).
3. **Map** `TenantId` through `DeadLetterFilter.TenantId`/`AllTenants` exactly as the database stores do (a query returns every tenant unless the filter names one). The reserved `default` tenant segment and injective tenant-qualified ids do not arise: the tenant is a column filtered server side, never part of a key (the keys are `Encode(SourcePattern)`/`Encode(SourceMessageId)`, and the pair is globally unique as in the database stores). Missing tenant context is the caller's concern (the gate that returns `DeadLetterErrorCodes.TenantRequired` above the store), not the store's.
4. **Expose** the SDK retry policies through the options (no second retry layer).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Both stores exist (Phases 2-3). AGENTS.md section 3 requires proven registration completeness and
section 6 the 12-function evaluation recorded in the plan matrix.

TASK:
1. Write the DI tests (ValidateOnBuild + ValidateScopes) for AddEncinaDeadLetterAzureTable and
   AddEncinaDeadLetterCosmosDB, each before and after AddEncinaDeadLetterQueue, and with a store
   registered by the application first.
2. Add a test that AddEncinaOpenTelemetry wraps the registered IDeadLetterStore.
3. Check the tenant filter semantics against the contract text in IDeadLetterStore.

KEY RULES:
- No new Meter or ActivitySource in these packages.
- Tests execute real package code (no reflection-only tests).

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs (line 152)
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedDeadLetterStore.cs
- src/Encina.Messaging/DeadLetter/DeadLetterServiceCollectionExtensions.cs
```

</details>

---

### Phase 5: Testing

> **Goal**: Every applicable flag reaches its manifest target; both emulators run in CI.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit tests** (`tests/Encina.UnitTests/Messaging/AzureTable/`, `.../CosmosDB/`): mapping, key building, `Left` on SDK failure, fake `TimeProvider`.
2. **Guard tests** (`tests/Encina.GuardTests/...`): every public method with parameters; `DeadLetterStoreLimits` argument rules.
3. **Contract tests** (`tests/Encina.ContractTests/...`): the same `IDeadLetterStore` contract the ten database stores pass, instantiating both real stores.
4. **Property tests** (`tests/Encina.PropertyTests/...`): result ordering `(DeadLetteredAtUtc, Id)` in both directions with several sources and filter semantics on Table Storage (Design Choice 5); key injectivity (`TableKeys.Encode`) including the forbidden characters, `%`, and `a|b`/`c` versus `a`/`b|c`.
5. **Integration tests** (`tests/Encina.IntegrationTests/...`): `[Collection("Azurite")]` and `[Collection("CosmosDb")]` fixtures with `Testcontainers.Azurite`/`Testcontainers.CosmosDb`, concurrent `TryClaimForReplayAsync` (exactly one winner), read-time expiry, an oversized payload (`dlq.payload_too_large`), delete freeing the unique pair (re-add succeeds), `ClearAllDataAsync` in `InitializeAsync`; never a `.md` justification. Unit tests add the crash-between-steps cases with a fault-injecting `TableClient` double.
6. **Justification files**: `.md` for load and benchmark tests (append-mostly store, no hot path).
7. **Cosmos in CI**: `Microsoft.Azure.Cosmos` brings a transitive tree, so the C1 PR must restore with no NU190x audit warning (central pinning per ADR-060 if needed). The Linux emulator needs a large memory budget (several GB) and a trusted certificate, so the fixture runs the emulator in gateway mode with a certificate-validation handler in tests only, and the CI cost (start-up of about a minute and the memory) is stated in the PR; the Azurite container is light.

#### Per-file coverage targets (percent, per flag; the same file name in both packages carries the same targets unless noted)

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `DeadLetter/DeadLetterMessage.cs` | 90 | 0 | 70 | 0 | 85 | Property bag with no validated parameters, so no guard flag; no logic worth a property |
| `DeadLetter/DeadLetterMessageFactory.cs` | 95 | 90 | 80 | 0 | 80 | Copies values; the null guard of `Create` is the only branch |
| `DeadLetter/TableKeys.cs` (Table only) | 95 | 90 | 60 | 95 | 70 | Pure encoding; injectivity is exactly a property |
| `DeadLetter/DeadLetterStoreAzureTable.cs` | 85 | 90 | 85 | 60 | 85 | Fault paths through a `TableClient` double; the contract and Azurite cover the happy paths; ordering is the property |
| `DeadLetter/DeadLetterStoreCosmosDB.cs` | 85 | 90 | 85 | 40 | 85 | Same split; Cosmos orders server side, so the property flag only checks filters |
| `Azure*DeadLetterOptions.cs`, `CosmosDB*DeadLetterOptions.cs` | 90 | 90 | 0 | 0 | 50 | Validation is unit and guard work; contract tests do not drive options; only fixtures build them |
| `ServiceCollectionExtensions.cs` | 90 | 95 | 0 | 0 | 70 | DI tests are unit; contract tests do not register services |
| `Diagnostics/Log.cs` | 60 | 0 | 0 | 0 | 40 | Source-generated logging, no validated parameters |
| `Health/*DeadLetterHealthCheck.cs` | 90 | 90 | 0 | 0 | 80 | Connectivity probe; no contract surface and no property |

Modified existing files (`EventIdRanges.cs`, `EncinaEventIdAllocationTests.cs` map, `DeadLetterErrorCodes.cs`) keep their baseline from the manifest; each PR reports the measured per-flag value of every touched file against its target, from the `coverage-report.cs` "Per-file targets" table.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Per-flag coverage (AGENTS.md section 9): unit, guard, contract, property and integration each reach
their own target in .github/coverage-manifest/Encina.Messaging.AzureTable.json and
Encina.Messaging.CosmosDB.json. Tests must execute real package code.

TASK:
1. Write the unit, guard, contract and property tests listed in the plan for both stores.
2. Write integration tests with shared collection fixtures for Azurite and the Cosmos DB emulator
   (container start, ClearAllDataAsync, CreateConnection-style accessors), including the concurrent
   claim and the read-time expiry test.
3. Add load/benchmark justification .md files.
4. Measure per-flag coverage with coverage-report.cs and report it against the manifest targets.

KEY RULES:
- Shouldly through Encina.Testing.Shouldly; never FluentAssertions.
- Fixtures are shared collection fixtures, never IClassFixture, never disposed from a test.
- CRAP <= 10 on changed methods; measure with the crap-gate table before pushing.

REFERENCE FILES:
- tests/Encina.IntegrationTests (existing Testcontainers collections, Collections.cs files)
- .github/scripts/coverage-report.cs
- docs/testing/integration-tests.md
```

</details>

---

### Phase 6: Documentation and Finalization

> **Goal**: Users can configure both stores; records are complete.

<details>
<summary><strong>Tasks</strong></summary>

1. XML doc comments on every public API of both packages.
2. `changelog.d/588-azure-dead-letter-stores.added.md`.
3. README per package (including the Azurite and emulator instructions, and the key layout), a feature page under `docs/features/`, `docs/INVENTORY.md` rows, `ROADMAP.md` line if the milestone changes.
4. ADR only if D1 sets a package-layout policy (ADR number assigned when the ADR pull request opens).
5. `PublicAPI.Unshipped.txt` complete; `dotnet build --configuration Release` with zero warnings; `dotnet test` with every flag at its manifest target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Both stores are implemented and tested. Documentation is written by the docs-writer agent following
the encina-docs skill; figures are cited with covref markers, never typed.

TASK:
1. Complete the XML docs and the PublicAPI files.
2. Write the changelog fragment and the READMEs; document the key layout and that Cosmos TTL only
   supplements DeleteExpiredAsync.
3. Update INVENTORY.md and, if needed, ROADMAP.md.

KEY RULES:
- English only; no hand-typed coverage numbers.
- Never edit CHANGELOG.md directly.

REFERENCE FILES:
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
- docs/plans/dsr-implementation-plan-404.md (style)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| `IDeadLetterStore` XML contract | Returns, ordering, claim and expiry semantics | The shared contract tests |
| SPEC-000 DEC-009, section 6.1 | Cloud items with a free local emulator are inside 1.0 | Azurite and the Cosmos DB emulator in CI |
| SPEC-000 REQ-028 | GCP post-1.0 | No Firestore/Bigtable store |
| ADR-001, ADR-006 | Railway Oriented Programming | `Either<EncinaError, T>`, no exceptions for outcomes |
| ADR-018 | Cross-cutting integration | The 12-function matrix below |
| ADR-021 | EventId allocation | Ranges 6800-6819 |
| ADR-062 | Error category contract | `EncinaErrors.Create(code, ErrorCategory.X, ...)`, `GetCategory` tests (table in Design Choices) |
| ADR-054 D4 | Tenant tags on telemetry | None added by these packages |
| Azure Table Storage limits | Entity group transactions only inside one partition, 100 operations | Key layout (Design Choice 2) |
| Azure Cosmos DB | Unique keys scoped to a logical partition; ETag preconditions | Key layout and claim |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IDeadLetterStore`, `IDeadLetterMessage`, `IDeadLetterMessageFactory`, `DeadLetterData` | `src/Encina.Messaging/DeadLetter/` | The contract the stores implement |
| `DeadLetterFilter`, `DeadLetterStoreLimits`, `DeadLetterErrorCodes` | same folder | Filtering, argument rules, error codes |
| `AddEncinaDeadLetterQueue<TStore, TFactory>` | `DeadLetterServiceCollectionExtensions.cs:24`, `:47` | Registration |
| `InstrumentedDeadLetterStore` | `src/Encina.OpenTelemetry/MessagingStores/` | Telemetry without store code |
| `DeadLetterHealthCheck` | `src/Encina.Messaging/Health/` | Pending-count health, store-agnostic |
| `DeadLetterStoreMongoDB` | `src/Encina.MongoDB/DeadLetter/` | Behavioral reference for the non-SQL case |
| `Encina.Messaging.Encryption.AzureKeyVault` | `src/` | Layout of a vendor-specific package |
| `Testcontainers.Azurite` | `Directory.Packages.props` | Integration tests for Table Storage |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging.AzureTable` | 6800-6809 (new: `MessagingDeadLetterAzureTable`) | From the family block 6800-6849; registered in `EventIdRanges.cs` and the `AssemblyRanges` map first |
| `Encina.Messaging.CosmosDB` | 6810-6819 (new: `MessagingDeadLetterCosmosDB`) | Same block; if D1 selects one shared package, the two ranges map to that assembly |
| #589 (DynamoDB) | 6820-6829 | Sibling plan, same block |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New production files | ~22 | Per package: entity, factory, store, options, DI extension, Log, health check, csproj, README, 2 PublicAPI |
| Changed production files | 3 | `EventIdRanges.cs`, `Directory.Packages.props`, `Encina.slnx` |
| Coverage manifests | 2 | One per package |
| Tests | ~20 | Unit, guard, contract, property, integration per store, fixtures, 2 justification files |
| Documentation | ~6 | READMEs, feature page, INVENTORY, changelog fragment |
| Shared error codes (S0) | 5 | `DeadLetterErrorCodes`, factory, PublicAPI, test, manifest |
| **Total** | **~58** | Eight pull requests S0, T1-T3, C1-C3, D (Phase 1 item 5), each at most 15 files and 1,500 lines counting tests, manifests, PublicAPI and fragments |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #588 - persistent IDeadLetterStore for Azure Table Storage and Cosmos DB.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Railway Oriented Programming: Either<EncinaError, T>; infrastructure failures are Left.
- Cloud category: Azure/AWS pair; the sibling AWS store is #589. Emulators (Azurite, Cosmos DB
  emulator) run in CI (SPEC-000 DEC-009).

IMPLEMENTATION OVERVIEW:
1. Foundations: two packages, Microsoft.Azure.Cosmos + Testcontainers.CosmosDb in
   Directory.Packages.props, EventId ranges 6800-6819, manifests, the eight-PR split (each PR with
   its own tests), shared error codes with ADR-062 categories.
2. Azure Table store: PartitionKey = Encode(SourcePattern), injective key encoding, marker row for
   the unique rule in the same entity-group transaction, pointer table written first for
   GetAsync(Guid), deletes remove row, marker and pointer, per-source merge for the global order.
3. Cosmos store: partition key /sourcePattern, unique key /sourceMessageId, composite index.
4. Cross-cutting: DI tests with ValidateOnBuild/ValidateScopes, decorator-based telemetry, health
   checks, SDK retry options, tenant filter semantics of the contract.
5. Tests per flag against both emulators; docs and changelog.

KEY PATTERNS:
- TimeProvider injected, never DateTime.UtcNow; CancellationToken on every call.
- Secrets in options: [JsonIgnore] + redacting ToString().
- EncinaError.Message never reaches logs; exception to logger through ForLogging().
- TryAdd registration so the application's store wins in any order.
- Shared collection fixtures for integration tests; Shouldly only.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs
- src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/plans/deadletter-azure-stores-implementation-plan-588.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Write-mostly store; a cached read would break the replay-claim contract |
| 2 | OpenTelemetry | ✅ Phase 4 | `InstrumentedDeadLetterStore` already decorates every `IDeadLetterStore`; a test proves the new stores are wrapped; no new `Meter`; no tenant tag of their own (ADR-054 D4: the decorator carries none) |
| 3 | Structured Logging | ✅ Phases 2-3 | `[LoggerMessage]` ids 6800-6819, registered first (ADR-021) |
| 4 | Health Checks | ✅ Phases 2-3 | One connectivity check per package; the pending-count `DeadLetterHealthCheck` is store-agnostic |
| 5 | Validation | ✅ Phase 2 | `DeadLetterStoreLimits` argument rules before any I/O |
| 6 | Resilience | ✅ Phase 4 | Azure SDK retry policies exposed through options; no second layer |
| 7 | Distributed Locks | ❌ N/A | The conditional (ETag) update is the claim |
| 8 | Transactions | ❌ N/A | Entity-group transaction/batch only inside the add; `SaveChangesAsync` is a no-op like the non-EF stores |
| 9 | Idempotency | ✅ Phases 2-3 | Unique `(SourcePattern, SourceMessageId)` returned as `Right(false)` |
| 10 | Multi-Tenancy | ✅ Phase 4 | `TenantId` column and the `DeadLetterFilter.TenantId`/`AllTenants` contract |
| 11 | Module Isolation | ❌ N/A | `IDeadLetterMessage` carries no `ModuleId` |
| 12 | Audit Trail | ❌ N/A | A store is not an audit surface; replay actions are audited by the dead letter manager above the store, identically for all providers |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Persistent dead letter contract and ten database stores (#583) | ✅ Closed | The behavioral reference |
| `Testcontainers.Azurite`, `Azure.Data.Tables` | ✅ Available | In `Directory.Packages.props` |
| `Microsoft.Azure.Cosmos`, Cosmos DB Testcontainers module | Phase 1 | Added by this plan |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| AWS counterpart | #589 | Same family; shares D2-style notification question |
| Azure audit stores | #578, #582 | Relevant to D1 option B only |

---

## Next Steps

1. Link this plan from #588 and ask the maintainer to answer Design Choices 1 (package layout), 2 (key layout), 3 (change feed), 5 (Table read algorithm) and 6 (oversized payloads) when work starts.
2. Open the follow-up issue `artifacts/issues/plan-588-deadletter-change-notification.md` (change notification across providers).
3. Implement Phases 1-6 as the eight pull requests of Phase 1 item 5 (S0 is shared with #589), referencing `Fixes #588` only on the last (D).
