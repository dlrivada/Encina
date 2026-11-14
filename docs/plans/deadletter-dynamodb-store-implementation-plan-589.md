# Implementation Plan: Persistent `IDeadLetterStore` for AWS DynamoDB

> **Issue**: [#589](https://github.com/dlrivada/Encina/issues/589)
> **Type**: Feature
> **Parent**: [#875](https://github.com/dlrivada/Encina/issues/875) (EPIC)
> **Related**: #583 (the ten database dead letter stores, closed), #588 (Azure counterpart), #579 (DynamoDB audit store), #210 (DynamoDB lock), SPEC-000 DEC-009
> **Complexity**: Medium (one new package, LocalStack in CI, no relational schema)
> **Estimated Scope**: ~700-900 lines of production code + ~1,800 lines of tests; exceeds one worker pull request, split proposed in Phase 1

---

## Summary

### What exists today (re-verified on 2026-11-14, main `c871956bf`)

- The contract is `IDeadLetterStore` (`src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs`): every member returns `Either<EncinaError, T>`. `AddAsync(IDeadLetterMessage)` is `Right(true)` when stored and `Right(false)` when `(SourcePattern, SourceMessageId)` already exists; the others are `GetAsync`, `GetMessagesAsync(filter, skip, take, newestFirst)`, `GetCountAsync`, `TryClaimForReplayAsync(id, claimExpiredBeforeUtc)`, `MarkAsReplayedAsync`, `DeleteAsync`, `DeleteManyAsync`, `DeleteExpiredAsync` and `SaveChangesAsync`. The issue's original snippet (`DynamoDBContext.SaveAsync`) is stale.
- The ten database providers implement it with a **global** unique index on `(SourcePattern, SourceMessageId)`, the order `(DeadLetteredAtUtc, Id)` and `DeadLetterStoreLimits` (`MaxPageSize = 1000`).
- Registration is `AddEncinaDeadLetterQueue<TStore, TFactory>` (`DeadLetterServiceCollectionExtensions.cs:24`, `:47`), `TryAdd*` based. `InstrumentedDeadLetterStore` already decorates any `IDeadLetterStore` (`Encina.OpenTelemetry/ServiceCollectionExtensions.cs:152`).
- `Testcontainers.LocalStack` is in `Directory.Packages.props`; `AWSSDK.DynamoDBv2` is not (only SQS, SNS, SecretsManager and KMS are).

### What this plan delivers

`DeadLetterStoreDynamoDB` with an `IDeadLetterMessageFactory`, options, a registration extension, a health check and tests against LocalStack. The issue's own table sketch (`Id` primary key, a `Source` GSI, a `expiration` TTL attribute) is refined where the contract needs more: a uniqueness marker item and an expiry index (Design Choice 4).

- Affected packages: a new DynamoDB dead letter package (name per Design Choice 1), `Encina` (EventId registry), `Directory.Packages.props`; `Encina.Messaging` is consumed, unchanged.
- Provider category: Cloud (the Azure/AWS pair of `AGENTS.md` section 5; GCP is post-1.0, SPEC-000 REQ-028). The Azure counterpart is #588.

---

## Design Choices

<details>
<summary><strong>1. Package layout — a dead letter package, or one DynamoDB package shared with audit and locks</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new package `Encina.Messaging.DynamoDB`** (the name in the issue) | Pay-for-what-you-use (`AGENTS.md` section 1); an application that stores only dead letters in DynamoDB takes only this package | A second DynamoDB client registration if the application also uses the audit store (#579) or the lock (#210) |
| **B) One DynamoDB package shared with #579 and #210** | One client registration, one connection configuration, one health check | Locks and dead letters have different owners and release cadences; the lock (`Encina.DistributedLock.*`) and messaging families live in different package trees today |

### Chosen Option: **A — A new package** (recommended, pending the maintainer)

### Rationale

- It follows the layout of the per-vendor packages (`Encina.Messaging.Encryption.AwsKms`). The duplicate client registration is avoided by registering `IAmazonDynamoDB` with `TryAdd`, so the first package that needs it wins and the others reuse it.
- B wins when one team owns all three DynamoDB uses and wants a single package to reference.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Is DynamoDB Streams integration part of this issue</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Include it** (optional in the issue's acceptance criteria) | Real-time notification of new dead letters | `IDeadLetterStore` has no notification member, so it needs a new public API; Azure Table Storage has no equivalent, so providers diverge |
| **B) Defer it to a cross-provider issue** | Keeps this store identical in behavior to the other nine; one answer for Cosmos change feed, Streams and the database providers | No push alerts on DynamoDB until that issue lands |

### Chosen Option: **B — Defer to a cross-provider issue** (recommended, pending the maintainer)

### Rationale

- `AGENTS.md` section 3 (provider coherence): providers differ only in implementation. The notification question is shared with the Cosmos change feed of #588, whose plan writes the follow-up issue (`plan-588-deadletter-change-notification`); this plan does not duplicate it.
- A wins for operators who want push alerts on AWS now and accept an AWS-only feature.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Mapping — the low-level API or the object persistence model</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `DynamoDBContext` attributes** (the issue's snippet) | Less code | Reflection-based mapping, harder to keep trim/AOT friendly; conditional expressions and transactions still need the low-level request types |
| **B) Low-level `IAmazonDynamoDB` with explicit attribute maps** | Exact control of `ConditionExpression`, `TransactWriteItems` and `UpdateItem`, which the uniqueness marker and the claim require; easy to unit test with a mock of one interface | More mapping code (one mapper class) |

### Chosen Option: **B — Low-level `IAmazonDynamoDB`** (recommended, pending the maintainer)

### Rationale

- The two operations that define the contract, the atomic add (message plus marker) and the claim, are conditional low-level requests in either option; mapping the rest by hand keeps one style and one mock surface. `AGENTS.md` section 9 asks tests to execute real package code, and a hand-written mapper is straightforward to cover.
- A wins when the team prefers attribute-based entities and accepts the reflection cost.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Uniqueness, expiry, claim, order — contract semantics on a key-value table</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Marker item plus `TransactWriteItems`; conditional claim; read-time expiry filter; sparse GSIs for order and expiry** | The unique rule is enforced atomically, as in the ten database stores; the claim is one conditional update; expired rows are never returned even before DynamoDB's lazy TTL deletes them | Two items per message (message and marker), a transaction on every add and delete, two GSIs |
| **B) A GSI on `(SourcePattern, SourceMessageId)` and a query before the write** | One item per message | A GSI is eventually consistent and cannot enforce uniqueness: two concurrent adds both succeed (a race, `AGENTS.md` section 3 idempotency) |
| **C) The pair as the primary key of the message item plus a GSI by `Id`** | One item per message; uniqueness by the primary key | `GetAsync(Guid)`, `TryClaimForReplayAsync`, `MarkAsReplayedAsync` and `DeleteAsync` locate a message by `Id`, and a GSI cannot be read consistently, so a message just added could be missing (read-your-write of the ten database stores is lost) |

### Chosen Option: **A — Marker item plus transactional write**

### Rationale

- Only A satisfies both cited requirements: B cannot enforce uniqueness (it races) and C cannot give read-your-write by `Id` (a GSI is always eventually consistent, https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/GSI.html); the base-table `GetItem` by `Pk = "M#{Id}"` can be read consistently.
- Item layout (single table `EncinaDeadLetters`, string `Pk`/`Sk`): the message is `Pk = "M#{Id}"`; the marker is `Pk = "S#" + {SourcePattern.Length} + "#" + {SourcePattern} + "#" + {SourceMessageId}`, carrying the message `Id`. The **length prefix makes the key injective**: `("a#b","c")` gives `S#3#a#b#c` and `("a","b#c")` gives `S#1#a#b#c`, so no two pairs share a marker (a collision would reject a distinct message as a duplicate: a lost dead letter). Add is a `TransactWriteItems` of the items of Design Choice 6 with `attribute_not_exists(Pk)` on the marker; a `TransactionCanceledException` whose cancellation reason for the **marker** is `ConditionalCheckFailed` is `Right(false)`. Any other cancellation reason (`TransactionConflict`, `ThrottlingError`, `ProvisionedThroughputExceeded`, `ValidationError`) is not a duplicate: it is a `Left` (see "Error categories"). Delete removes the same items in one transaction.
- Consistency: every base-table read (`GetItem`, base-table `Query`) sets `ConsistentRead = true`. What the listing and the expiry delete read is Design Choice 6.
- Claim: `UpdateItem` with `ConditionExpression = attribute_exists(Pk) AND attribute_not_exists(ReplayedAtUtc) AND (attribute_not_exists(ReplayClaimedAtUtc) OR ReplayClaimedAtUtc <= :expired)`; `ConditionalCheckFailedException` is `Right(false)`. **Mapper rules**: a null value is omitted from the item, never written with the `NULL` type (`attribute_not_exists` requires the attribute to be absent); dates are fixed-width UTC strings (`DateTime.ToString("O")` of a UTC value, always `yyyy-MM-ddTHH:mm:ss.fffffffZ`) so string comparison in the claim and in the sort key `{DeadLetteredAtUtc}#{Id:D}` is chronological; the mapper unit tests cover both.
- Expiry: `ExpiresAtUtc` is **read-time authoritative**: reads add `ExpiresAtUtc > :now`, and `DeleteExpiredAsync` removes the message and its other items in one transaction. The `ttl` attribute (Unix seconds, on every item of the message) is only a lazy backstop (documented as typically within days) and DynamoDB deletes the items independently, so a marker can disappear before its message (a re-add is then accepted while the old row is still present but expired: harmless, the old row is unreachable) or after it (a re-add is rejected until `DeleteExpiredAsync` or TTL removes it, as an expired row still occupies the unique pair on the database stores until it is deleted).
- Errors are `Left` (ADR-001/ADR-006), time from `TimeProvider`, arguments validated against `DeadLetterStoreLimits` before any I/O, secrets in options `[JsonIgnore]` with a redacting `ToString()` (`AGENTS.md` section 3, #851). ADR-054 D4: `InstrumentedDeadLetterStore` carries no tenant tag (its XML says so) and this package emits no telemetry of its own, so it adds none.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (ROP, TimeProvider, idempotency, secrets in options), ADR-001, ADR-006, the `IDeadLetterStore` XML contract (read-your-write and uniqueness of the pair), and https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/transaction-apis.html (transactions), https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/GSI.html (GSI eventual consistency) and https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/TTL.html (lazy TTL deletion)

</details>

<details>
<summary><strong>5. Index layout for ordered, filtered listing</strong></summary>

### Options Considered

`GetMessagesAsync` orders by `(DeadLetteredAtUtc, Id)` with skip and take across all sources unless a filter narrows it. A DynamoDB query returns items in sort-key order within **one** partition-key value of the index.

| Option | Pros | Cons |
|--------|------|------|
| **A) Index partitioned by `SourcePattern`, sort `{DeadLetteredAtUtc}#{Id:D}`** (a GSI `ByDeadLettered` or base-table list items, see Design Choice 6) | Cheap per-source queries (the issue's `SourceIndex`); `SourcePattern` is small in practice, so the keys spread over a handful of values | An unfiltered listing queries each source and merges client-side (a k-way merge bounded by the number of distinct sources) |
| **B) GSI with one constant partition value** | One query returns the global order | One hot index partition for every write; throttling at high dead letter rates |
| **C) GSI partitioned by `TenantId`** | Tenant queries are single-partition | Unfiltered and per-source listings scatter across tenants; a null tenant needs a sentinel |

### Chosen Option: **A — Partition by `SourcePattern`** (recommended, pending the maintainer)

### Rationale

- Dead letters are low-volume, but a constant key (B) is the classic DynamoDB hot-key pattern, and `SourcePattern` is small in practice (the `DeadLetterSourcePatterns` constants; the column itself accepts any string up to 64 characters, so the layout never assumes a closed set) and the merge is bounded by the number of distinct sources. The tenant filter is applied as a filter expression on A.
- C wins in a multi-tenant deployment where almost every query names a tenant; B wins only if the global order must be one query and the write rate is known to be small.
- **Listing mechanics (all in Phase 2, each tested)**: (i) a tenant, source-message, error-code or date filter is a `FilterExpression`, which DynamoDB applies **after** the page `Limit`, so a page can come back short or empty while matches remain: the store loops on `LastEvaluatedKey` until `take` rows are collected or the key is exhausted; (ii) `skip` costs `skip + take` rows read from each source before the merge, stated in the README; (iii) `GetCountAsync` is a `Query` with `Select = COUNT` over every page and every source (with the filter, looping on `LastEvaluatedKey`); (iv) `TransactWriteItems` accepts at most 100 items and each message is 2 or 3 items (Design Choice 6), so `DeleteManyAsync` deletes at most 33 (or 50) messages per transaction, reads each message first to build the marker key from its `SourcePattern` and `SourceMessageId`, and is **not atomic across batches** (a partial failure returns a `Left` after earlier batches committed; the call is idempotent and can be repeated).

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Consistency of the listing and of the expiry delete (GSI versus base-table list items)</strong></summary>

### Options Considered

The ten database stores are read-your-write: after `AddAsync` returns `Right(true)`, `GetAsync`, `GetMessagesAsync`, `GetCountAsync` and `DeleteManyAsync` see the row. A GSI cannot be read consistently, so a GSI-served listing can miss a message added a moment ago and `DeleteExpiredAsync` through a GSI can skip a new row. LocalStack replicates immediately, so the integration tests would not show it.

| Option | Pros | Cons |
|--------|------|------|
| **A) Sparse GSIs `ByDeadLettered` and `ByExpiry`, eventual consistency documented** | Two items per message (message and marker); the issue's GSI layout | Weaker than the other stores on listing, count and `DeleteMany`; the divergence is documented in the README and the contract tests need a tolerance for this provider |
| **B) Base-table list items**: a third item per message, `Pk = "L#" + {SourcePattern.Length} + "#" + {SourcePattern}`, `Sk = "{DeadLetteredAtUtc}#{Id:D}"`, written in the same transaction, queried with `ConsistentRead = true`; `DeleteExpiredAsync` queries the list partitions with an `ExpiresAtUtc` filter, looping on `LastEvaluatedKey` | Read-your-write on every path, same contract as the database stores, no GSI to provision | Three items per message (at most 33 messages per delete transaction); an expiry delete reads the whole list partition (low volume); the list partition is one key per source, bounded by DynamoDB's partition throughput, adequate for dead letter rates |

### Chosen Option: **B — Base-table list items with consistent reads** (recommended, pending the maintainer)

### Rationale

- B keeps the contract every consumer relies on (a replay tool that lists, then replays, never misses a fresh dead letter) and needs no per-provider tolerance in the shared contract tests. A wins when weaker listing consistency is acceptable and one item fewer per message matters (cost and the 50-per-transaction delete).
- With A, base-table `GetItem` still sets `ConsistentRead = true`; only listing, count and expiry delete are eventually consistent, and the README says so.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Oversized payloads</strong></summary>

### Options Considered

A DynamoDB item is capped at 400 KB and `DeadLetterStoreLimits` caps no payload field (`RequestContent`, stack trace, context), so a large serialized request would fail on DynamoDB and succeed on the ten database stores.

| Option | Pros | Cons |
|--------|------|------|
| **A) Reject before I/O with a coded `Left` (`dlq.payload_too_large`, Validation)** | Predictable, no partial write; the limit is documented | A dead letter other stores accept is refused here |
| **B) Overflow into chunk items** | Accepts larger payloads | Extra items in the transaction (100-item limit), more code and failure modes |
| **C) Truncate** | Never refuses | Corrupt replay; loses the data a dead letter preserves |

### Chosen Option: **A — Reject before I/O** (recommended, pending the maintainer)

### Rationale

- A dead letter that cannot be replayed faithfully must not be stored half. A contract test with an oversized payload proves it. B wins if payloads above 400 KB are expected.

**Class:** Dilemma

</details>

### Error categories (ADR-062)

Every `Left` is created with `EncinaErrors.Create(code, category, ...)` and a `GetCategory` unit test asserts each row; one code carries one category. The codes marked shared are added to `DeadLetterErrorCodes` by the shared prerequisite PR S0 of the Azure plan (#588), reused here (whichever of #588 and #589 lands first adds them; the other reuses them without a second definition).

| Code | Category | Used for |
|------|----------|----------|
| `dlq.store_failed`, `dlq.get_failed`, `dlq.query_failed`, `dlq.claim_failed`, `dlq.mark_replayed_failed`, `dlq.delete_failed` | `Internal` | Non-transient SDK faults on the matching operation |
| `dlq.store_throttled` (shared, new) | `RateLimited` | `ProvisionedThroughputExceededException`, `RequestLimitExceeded`, `ThrottlingException` and a `ThrottlingError` cancellation reason, after the SDK retries |
| `dlq.store_unavailable` (shared, new) | `Unavailable` | Connection failure, DNS, 5xx |
| `dlq.store_timeout` (shared, new) | `Timeout` | Client or request timeout not caused by the caller's token |
| `dlq.store_conflict` (new, this plan) | `Conflict` | `TransactionConflict` cancellation reason (a concurrent transaction on the same item) |
| `dlq.payload_too_large` (shared, new) | `Validation` | Rejected before any I/O (Design Choice 7) |

An `OperationCanceledException` from the caller's token propagates as cancellation. Only the code, never the AWS message, is stored, logged or tagged.

---

## Implementation Phases

### Phase 1: Foundations — Package, Dependency, EventIds, Split

> **Goal**: Create the build scaffolding and fix the pull request split.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Messaging.DynamoDB/Encina.Messaging.DynamoDB.csproj` (name per Design Choice 1; layout of `src/Encina.Messaging.Encryption.AwsKms`), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, README, and a row in `Encina.slnx`.
2. **Modify** `Directory.Packages.props`: add `AWSSDK.DynamoDBv2` (the same major as the other AWSSDK entries).
3. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: add `MessagingDeadLetterDynamoDB = (6820, 6829)` (family block 6800-6849) and the entry of the `AssemblyRanges` map in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
4. **Create** `.github/coverage-manifest/Encina.Messaging.DynamoDB.json` with the per-file, per-flag targets of the table "Per-file coverage targets" (Phase 4), each with its one-sentence justification.
5. **Split** into five pull requests, each with its own tests and manifest rows so the blocking `crap-gate` can pass (a production-only PR cannot reach any per-flag target), each at most 15 files and 1,500 lines **counting tests, manifests, PublicAPI, `ProjectReference` lines and DI/contract tests**:

   | PR | Content | Files (approx.) |
   |----|---------|-----------------|
   | S0 | The shared error codes (`dlq.store_throttled`, `dlq.store_unavailable`, `dlq.store_timeout`, `dlq.payload_too_large`) with ADR-062 categories, defined once with #588 (whichever plan lands first adds them) | 5 |
   | D1 | Package foundations: csproj, two PublicAPI files, `Directory.Packages.props` (`AWSSDK.DynamoDBv2`), `Encina.slnx`, `EventIdRanges.cs`, the allocation-test map, mapper, key builder (length-prefixed), message, factory, manifest; unit tests (mapper round trip incl. omitted nulls and fixed-width dates, keys incl. `#` in both parts) and guard tests | 15 |
   | D2 | Store and registration: store, options, `ServiceCollectionExtensions`, `Log.cs`, health check, PublicAPI update, `dlq.store_conflict` in `DeadLetterErrorCodes` with its `PublicAPI.Unshipped.txt` line; unit tests with a mock `IAmazonDynamoDB` (transaction cancellation reasons, throttling, `GetCategory` per code), DI test (`ValidateOnBuild`, `ValidateScopes`, both orders, pre-registered client), guard tests | 13 |
   | D3 | Contract, property (merge order) and integration tests on the LocalStack collection fixture (service list `dynamodb`), two load/benchmark justification `.md` files, manifest update | 7 |
   | D4 | README, feature page, `docs/INVENTORY.md`, changelog fragment; carries `Fixes #589` | 4 |

   Line counts are re-measured when each PR is briefed.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0) needs a persistent IDeadLetterStore on AWS DynamoDB (issue #589).
The contract is src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs. Per-vendor packages follow the
layout of src/Encina.Messaging.Encryption.AwsKms.

TASK:
1. Create the project, PublicAPI files, README and slnx entry (name per the answer to Design Choice 1).
2. Add AWSSDK.DynamoDBv2 to Directory.Packages.props.
3. Register EventId range 6820-6829 in EventIdRanges.cs and the AssemblyRanges map of
   EncinaEventIdAllocationTests.
4. Create the coverage manifest.

KEY RULES:
- ADR-021: register the range before any [LoggerMessage].
- Zero warnings; RS0016/RS0017 in PublicAPI.Unshipped.txt.
- No store code in this phase.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina.Messaging.Encryption.AwsKms/
- .github/coverage-manifest/Encina.Messaging.Encryption.AwsKms.json
```

</details>

---

### Phase 2: `DeadLetterStoreDynamoDB`

> **Goal**: The store passes the shared contract against LocalStack.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `DeadLetter/DeadLetterItemMapper.cs` (message to and from attribute maps; null values omitted, never the `NULL` type; dates as fixed-width UTC `"O"` strings; `ttl` in Unix seconds), `DeadLetter/DeadLetterKeys.cs` (the length-prefixed marker, list and sort keys of Design Choices 4 and 6), `DeadLetter/DeadLetterMessage.cs` (the `IDeadLetterMessage` record the mapper returns) and `DeadLetter/DeadLetterMessageFactory.cs` (`Create(DeadLetterData)`).
2. **Create** `DeadLetter/DeadLetterStoreDynamoDB.cs` implementing `IDeadLetterStore` with the layout of Design Choices 4 to 7: transactional add of the message, marker (and list item under Design Choice 6 B) with `attribute_not_exists(Pk)` on the marker and `Right(false)` only for a `ConditionalCheckFailed` reason on the marker; `ConsistentRead = true` on every base-table read; conditional claim and replay marking; listing with the loop on `LastEvaluatedKey`, the cost of `skip` and `GetCountAsync` as `Select = COUNT` over all pages (Design Choice 5); `DeleteManyAsync` in transactions of at most 100 items after a read of each message, not atomic across batches; `DeleteExpiredAsync` through the structure of Design Choice 6; the payload size check before I/O (Design Choice 7).
3. **Create** `DynamoDbDeadLetterOptions.cs` (table name, GSI names when Design Choice 6 A, region/service URL, credentials `[JsonIgnore]` with a redacting `ToString()`, SDK retry mode and max attempts) and `ServiceCollectionExtensions.cs` (`AddEncinaDeadLetterDynamoDB(...)`: `TryAddSingleton` of `IAmazonDynamoDB` so the application's client wins in any order, table creation on demand, then `AddEncinaDeadLetterQueue<DeadLetterStoreDynamoDB, DeadLetterMessageFactory>`). Table creation needs the `dynamodb:CreateTable` permission and is racy across instances, so a `ResourceInUseException` is treated as success and creation can be switched off by an option when the table is provisioned externally (the README lists the IAM actions with and without it).
4. **Create** `Diagnostics/Log.cs` with `[LoggerMessage]` ids 6820-6829 and `Health/DynamoDbDeadLetterHealthCheck.cs`.
5. **Update** `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Phase 1 created Encina.Messaging.DynamoDB and registered EventIds 6820-6829. The ten database stores
(for example src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs) are the behavioral reference.

TASK:
1. Implement the mapper, keys, message, factory and DeadLetterStoreDynamoDB per the plan's Design
   Choices 4-7: message item Pk "M#{Id}", marker item with the LENGTH-PREFIXED key, TransactWriteItems
   for add and delete, UpdateItem with a ConditionExpression for the claim, ConsistentRead = true on
   base-table reads, read-time filter on ExpiresAtUtc, the listing structure the maintainer chose in
   Design Choice 6, loops on LastEvaluatedKey for filtered pages and counts.
2. ConditionalCheckFailed / TransactionCanceled(ConditionalCheckFailed on the MARKER only) -> Right(false);
   every other AmazonDynamoDBException or cancellation reason -> Left(EncinaError) created with
   EncinaErrors.Create and the ADR-062 category of the plan's "Error categories" table, and no
   exception message.
3. Options with [JsonIgnore] credentials and a redacting ToString(); TryAdd registration.
4. Log.cs ids 6820-6829 and a health check using DescribeTable.

KEY RULES:
- TimeProvider injected; never DateTime.UtcNow.
- Arguments validated against DeadLetterStoreLimits before any I/O (throw).
- CancellationToken on every SDK call; page queries by LastEvaluatedKey, never assume one page.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs, DeadLetterFilter.cs, DeadLetterStoreLimits.cs
- src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs
- src/Encina.Messaging/DeadLetter/DeadLetterServiceCollectionExtensions.cs
- src/Encina.Messaging/Health/DeadLetterHealthCheck.cs
```

</details>

---

### Phase 3: Cross-Cutting Integration and Observability

> **Goal**: Registration completeness, telemetry reuse, resilience and tenancy.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** DI tests with `ValidateOnBuild` and `ValidateScopes`, in both registration orders, with an application-registered store winning and with `IAmazonDynamoDB` registered first.
2. **Verify** by test that `AddEncinaOpenTelemetry` wraps the registered `IDeadLetterStore` (no new `Meter`).
3. **Map** `TenantId` through `DeadLetterFilter.TenantId`/`AllTenants` as the database stores do. The tenant is an attribute filtered by `FilterExpression`, never part of a key, so the reserved `default` tenant segment and tenant-qualified ids do not arise; a missing tenant context is denied by the gate above the store (`DeadLetterErrorCodes.TenantRequired`), not by the store.
4. **Expose** the SDK retry mode and attempts through the options.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
The store exists (Phase 2). AGENTS.md section 3 requires proven registration completeness; section 6
the 12-function evaluation recorded in the plan matrix.

TASK:
1. Write DI tests for AddEncinaDeadLetterDynamoDB before and after AddEncinaDeadLetterQueue.
2. Write the decorator test for AddEncinaOpenTelemetry.
3. Check tenant filter semantics against the contract text in IDeadLetterStore.

KEY RULES:
- No new Meter or ActivitySource in this package (InstrumentedDeadLetterStore covers it).
- Tests execute real package code.

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs (line 152)
- src/Encina.Messaging/DeadLetter/DeadLetterServiceCollectionExtensions.cs
```

</details>

---

### Phase 4: Testing

> **Goal**: Every applicable flag reaches its manifest target; LocalStack runs in CI.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit tests**: mapper round trips, key building, `Left` on SDK failure with a mock `IAmazonDynamoDB`, fake `TimeProvider`.
2. **Guard tests**: every public method with parameters; `DeadLetterStoreLimits` rules.
3. **Contract tests**: the `IDeadLetterStore` contract the ten database stores pass, with the real store.
4. **Property tests**: the k-way merge ordering `(DeadLetteredAtUtc, Id)` and filter semantics, only if the merge logic stays non-trivial.
5. **Integration tests**: a shared `[Collection("LocalStack-DynamoDB")]` fixture with `Testcontainers.LocalStack` whose services list includes `dynamodb`; concurrent `TryClaimForReplayAsync` (one winner), concurrent duplicate `AddAsync` (one `Right(true)`), `#` in both parts of the pair (two distinct messages both stored), a filtered page that is short on the first DynamoDB page, an oversized payload (`dlq.payload_too_large`), read-time expiry before TTL deletion, `ClearAllDataAsync` in `InitializeAsync`; never a `.md` justification. LocalStack replicates immediately, so eventual consistency is not observable there: consistency is enforced by the request parameters, asserted in unit tests with the mock `IAmazonDynamoDB` (`ConsistentRead = true`).
6. **Justification files**: `.md` for load and benchmark tests.

#### Per-file coverage targets (percent, per flag)

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `DeadLetter/DeadLetterItemMapper.cs` | 95 | 90 | 70 | 0 | 80 | Pure mapping, fully unit tested; no property flag because round trips are enumerated cases, not complex logic |
| `DeadLetter/DeadLetterKeys.cs` | 95 | 90 | 60 | 90 | 70 | Pure key building; injectivity with `#` is a property |
| `DeadLetter/DeadLetterMessage.cs` | 90 | 0 | 70 | 0 | 80 | Record with no validated parameters |
| `DeadLetter/DeadLetterMessageFactory.cs` | 95 | 90 | 80 | 0 | 80 | Copies values; the null guard is the only branch |
| `DeadLetter/DeadLetterStoreDynamoDB.cs` | 85 | 90 | 85 | 60 | 85 | Cancellation reasons and fault mapping through a mock; contract and LocalStack cover the happy paths; merge order is the property |
| `DynamoDbDeadLetterOptions.cs` | 90 | 90 | 0 | 0 | 50 | Validation is unit and guard work; contract tests do not drive options |
| `ServiceCollectionExtensions.cs` | 90 | 95 | 0 | 0 | 70 | DI tests are unit; contract tests do not register services |
| `Diagnostics/Log.cs` | 60 | 0 | 0 | 0 | 40 | Source-generated logging, no validated parameters |
| `Health/DynamoDbDeadLetterHealthCheck.cs` | 90 | 90 | 0 | 0 | 80 | `DescribeTable` probe; no contract surface |

Modified existing files (`EventIdRanges.cs`, the allocation-test map, `DeadLetterErrorCodes.cs`) keep their manifest baseline; each PR reports the measured per-flag value of every touched file against its target (`coverage-report.cs` "Per-file targets" table).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Per-flag coverage (AGENTS.md section 9): unit, guard, contract, property and integration each reach
their own target in .github/coverage-manifest/Encina.Messaging.DynamoDB.json.

TASK:
1. Write the unit, guard, contract and (if kept) property tests of the plan.
2. Write the integration tests on a shared LocalStack collection fixture, including the concurrent
   claim, the concurrent duplicate add and the read-time expiry test.
3. Add load/benchmark justification .md files.
4. Measure per-flag coverage with coverage-report.cs and report it against the manifest.

KEY RULES:
- Shouldly through Encina.Testing.Shouldly; never FluentAssertions.
- Shared collection fixtures, never IClassFixture.
- CRAP <= 10 on changed methods.

REFERENCE FILES:
- tests/Encina.IntegrationTests (existing Testcontainers collections)
- .github/scripts/coverage-report.cs
- docs/testing/integration-tests.md
```

</details>

---

### Phase 5: Documentation and Finalization

> **Goal**: Users can configure the store; records are complete.

<details>
<summary><strong>Tasks</strong></summary>

1. XML doc comments on every public API.
2. `changelog.d/589-dynamodb-dead-letter-store.added.md`.
3. Package README (table schema, GSIs, TTL behavior, IAM permissions), a feature page, `docs/INVENTORY.md` row.
4. ADR only if Design Choice 1 sets a package-layout policy (ADR number assigned when the ADR pull request opens).
5. `dotnet build --configuration Release` with zero warnings; `dotnet test` with every flag at its target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
The store is implemented and tested. Documentation is written by the docs-writer agent following
the encina-docs skill; figures are cited with covref markers, never typed.

TASK:
1. Complete XML docs and the PublicAPI files.
2. Write the changelog fragment and the README (schema, indexes, TTL, IAM actions needed).
3. Update INVENTORY.md.

KEY RULES:
- English only; never edit CHANGELOG.md directly; no hand-typed coverage numbers.

REFERENCE FILES:
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| `IDeadLetterStore` XML contract | Returns, ordering, claim and expiry semantics | The shared contract tests |
| SPEC-000 DEC-009, section 6.1 | Cloud items with a free local emulator are inside 1.0 | LocalStack in CI |
| SPEC-000 REQ-028 | GCP post-1.0 | No Firestore/Bigtable store |
| ADR-001, ADR-006 | Railway Oriented Programming | `Either<EncinaError, T>` |
| ADR-018, ADR-021 | Cross-cutting evaluation, EventIds | Matrix below, range 6820-6829 |
| ADR-062 | Error category contract | Table "Error categories" in Design Choices |
| DynamoDB transactions and TTL | Atomic multi-item writes; lazy TTL deletion | Marker item, read-time expiry filter |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IDeadLetterStore`, `IDeadLetterMessage`, `IDeadLetterMessageFactory`, `DeadLetterData` | `src/Encina.Messaging/DeadLetter/` | The contract |
| `DeadLetterFilter`, `DeadLetterStoreLimits`, `DeadLetterErrorCodes` | same folder | Filtering, argument rules, error codes |
| `AddEncinaDeadLetterQueue<TStore, TFactory>` | `DeadLetterServiceCollectionExtensions.cs:24`, `:47` | Registration |
| `InstrumentedDeadLetterStore` | `src/Encina.OpenTelemetry/MessagingStores/` | Telemetry without store code |
| `DeadLetterHealthCheck` | `src/Encina.Messaging/Health/` | Pending-count health, store-agnostic |
| `Encina.Messaging.Encryption.AwsKms` | `src/` | Layout of an AWS vendor package |
| `Testcontainers.LocalStack` | `Directory.Packages.props` | Integration tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging.DynamoDB` | 6820-6829 (new: `MessagingDeadLetterDynamoDB`) | From the family block 6800-6849; registered in `EventIdRanges.cs` and the `AssemblyRanges` map first |
| #588 (Azure stores) | 6800-6819 | Sibling plan, same block |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New production files | ~11 | Mapper, message, factory, store, options, DI extension, Log, health check, csproj, README, 2 PublicAPI |
| Changed production files | 3 | `EventIdRanges.cs`, `Directory.Packages.props`, `Encina.slnx` |
| Coverage manifest | 1 | One per package |
| Tests | ~12 | Unit, guard, contract, property, integration, fixture, 2 justification files |
| Documentation | ~4 | README, feature page, INVENTORY, changelog fragment |
| Shared error codes (S0) | 5 | Shared with #588 |
| **Total** | **~36** | Five pull requests S0, D1-D4 (Phase 1 item 5), each at most 15 files and 1,500 lines counting tests, manifests, PublicAPI and fragments |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #589 - persistent IDeadLetterStore for AWS DynamoDB.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Railway Oriented Programming: Either<EncinaError, T>; infrastructure failures are Left.
- Cloud category: Azure/AWS pair; the Azure sibling is #588. LocalStack runs in CI (SPEC-000 DEC-009).

IMPLEMENTATION OVERVIEW:
1. Foundations: package, AWSSDK.DynamoDBv2, EventId range 6820-6829, manifest, the five-PR split
   (each PR with its own tests), shared error codes with ADR-062 categories.
2. Store: low-level IAmazonDynamoDB; message item plus length-prefixed uniqueness marker written by
   TransactWriteItems; conditional UpdateItem claim; ConsistentRead on base-table reads; read-time
   expiry; listing structure per Design Choice 6 (maintainer's answer); loops on LastEvaluatedKey.
3. Cross-cutting: DI tests with ValidateOnBuild/ValidateScopes, decorator-based telemetry, health
   check, SDK retry options, tenant filter semantics.
4. Tests per flag on LocalStack; docs and changelog.

KEY PATTERNS:
- TimeProvider injected, never DateTime.UtcNow; CancellationToken on every call.
- Secrets in options: [JsonIgnore] + redacting ToString().
- EncinaError.Message never reaches logs; exception to logger through ForLogging().
- TryAdd registration so the application's store (and IAmazonDynamoDB) wins in any order.
- Shared collection fixtures for integration tests; Shouldly only.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs
- src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/plans/deadletter-dynamodb-store-implementation-plan-589.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Write-mostly store; a cached read would break the replay-claim contract |
| 2 | OpenTelemetry | ✅ Phase 3 | `InstrumentedDeadLetterStore` already decorates every `IDeadLetterStore`; a test proves the store is wrapped; no new `Meter`; no tenant tag of its own (ADR-054 D4: the decorator carries none) |
| 3 | Structured Logging | ✅ Phase 2 | `[LoggerMessage]` ids 6820-6829, registered first (ADR-021) |
| 4 | Health Checks | ✅ Phase 2 | `DescribeTable` connectivity check; the pending-count `DeadLetterHealthCheck` is store-agnostic |
| 5 | Validation | ✅ Phase 2 | `DeadLetterStoreLimits` argument rules before any I/O |
| 6 | Resilience | ✅ Phase 3 | AWS SDK retry mode and attempts through options; no second layer |
| 7 | Distributed Locks | ❌ N/A | The conditional update is the claim |
| 8 | Transactions | ❌ N/A | `TransactWriteItems` only inside add and delete (marker plus message); `SaveChangesAsync` is a no-op like the non-EF stores |
| 9 | Idempotency | ✅ Phase 2 | Marker item with `attribute_not_exists`, returned as `Right(false)` |
| 10 | Multi-Tenancy | ✅ Phase 3 | `TenantId` attribute and the `DeadLetterFilter.TenantId`/`AllTenants` contract |
| 11 | Module Isolation | ❌ N/A | `IDeadLetterMessage` carries no `ModuleId` |
| 12 | Audit Trail | ❌ N/A | A store is not an audit surface; replay actions are audited by the dead letter manager above the store, identically for all providers |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Persistent dead letter contract and ten database stores (#583) | ✅ Closed | The behavioral reference |
| `Testcontainers.LocalStack` | ✅ Available | In `Directory.Packages.props` |
| `AWSSDK.DynamoDBv2` | Phase 1 | Added by this plan |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Azure counterpart | #588 | Same family; its plan writes the shared change-notification follow-up |
| DynamoDB audit store and lock | #579, #210 | Relevant to Design Choice 1 option B only |

---

## Next Steps

1. Link this plan from #589 and ask the maintainer to answer Design Choices 1 (package), 2 (Streams), 3 (mapping), 5 (index layout), 6 (listing consistency) and 7 (oversized payloads) when work starts.
2. Implement Phases 1-5 as the five pull requests of Phase 1 item 5 (S0 is shared with #588), `Fixes #589` on the last (D4).
