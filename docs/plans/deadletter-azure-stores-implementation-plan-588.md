# Implementation Plan: Persistent `IDeadLetterStore` for Azure Table Storage and Azure Cosmos DB

> **Issue**: [#588](https://github.com/dlrivada/Encina/issues/588)
> **Type**: Feature
> **Parent**: [#875](https://github.com/dlrivada/Encina/issues/875) (EPIC)
> **Related**: #583 (the ten database dead letter stores, closed), #589 (AWS DynamoDB counterpart), #578 and #582 (Azure audit stores), SPEC-000 DEC-009 (cloud items with a free local emulator are in 1.0)
> **Complexity**: Medium-High (two new packages, two emulators in CI, no relational schema)
> **Estimated Scope**: ~1,400-1,900 lines of production code + ~3,500 lines of tests; exceeds one worker pull request, split proposed in Phase 1

---

## Summary

### What exists today (read on 2026-11-11, main `942c0f124`)

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

- Only A keeps the **global** unique rule of the database stores without a cross-partition write, in both services. `SourcePattern` is a closed set (`DeadLetterSourcePatterns`: Outbox, Inbox, Saga, Scheduling, Recoverability, and so on), so the partition count is small by design and dead letters are low-volume by definition (a message is dead-lettered after its retries are exhausted).
- The hot-partition risk is real only at the per-partition throughput and size limits of the two services (published in their scalability targets), far above dead letter rates; Phase 2 states the limits it relies on in the package README. B wins in a multi-tenant deployment whose tenants reuse source message ids and want tenant-scoped uniqueness; C wins only at extreme volume.
- Time order inside a partition: the Table `RowKey` of a message is `M|{DeadLetteredAtUtc ticks, 19 digits, inverted for newest-first}|{Id}` plus an `I|{Id}` pointer in a small index table for `GetAsync(Guid)`; Cosmos orders with a composite index on `(/deadLetteredAtUtc, /id)`. Phase 2 validates this layout with a spike against Azurite before the rest is written.

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
- Claim: `TryClaimForReplayAsync` is one conditional update, `Right(true)` only to the winner: Table Storage `UpdateEntityAsync` with `ETag` (`If-Match`), Cosmos `ReplaceItemAsync` with `IfMatchEtag`; a 412 is `Right(false)`.
- Expiry: a read filters `ExpiresAtUtc` against `TimeProvider`; Cosmos TTL may be enabled as an extra, never as the contract.
- Telemetry: `InstrumentedDeadLetterStore` decorates every `IDeadLetterStore` (`Encina.OpenTelemetry/ServiceCollectionExtensions.cs:152`), so the new packages add no store-level `Meter`; the issue's proposed `ActivitySource` is replaced by that decorator.
- Secrets: the options hold a connection string or account key, so those properties are `[JsonIgnore]` and `ToString()` is overridden (`AGENTS.md` section 3, #851).

**Class:** Settled
**Settled by:** AGENTS.md section 3 (ROP, TimeProvider, secrets in options), ADR-001, ADR-006, the `IDeadLetterStore` XML contract, and `Encina.OpenTelemetry/ServiceCollectionExtensions.cs:152`

</details>

---

## Implementation Phases

### Phase 1: Foundations — Packages, Dependencies, EventIds, Split

> **Goal**: Create the build scaffolding both stores need, and fix the pull request split.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Messaging.AzureTable/Encina.Messaging.AzureTable.csproj` and `src/Encina.Messaging.CosmosDB/Encina.Messaging.CosmosDB.csproj` (names per D1; copy the layout of `src/Encina.Messaging.Encryption.AzureKeyVault`), each with `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, `GlobalSuppressions.cs` if needed, a README and a row in `Encina.slnx`.
2. **Modify** `Directory.Packages.props`: add `Microsoft.Azure.Cosmos` and the Cosmos DB Testcontainers module (`Testcontainers.CosmosDb`).
3. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: add `MessagingDeadLetterAzureTable = (6800, 6809)` and `MessagingDeadLetterCosmosDB = (6810, 6819)` (block 6800-6849 assigned to this family; this plan uses 6800-6819) and the two entries of the `AssemblyRanges` map of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
4. **Create** `.github/coverage-manifest/Encina.Messaging.AzureTable.json` and `Encina.Messaging.CosmosDB.json` with per-file targets and one-sentence justifications.
5. **Split** into pull requests of at most 15 files and 1,500 lines: S1 Azure Table package (production), S2 Azure Table tests, S3 Cosmos package (production), S4 Cosmos tests, S5 documentation. Foundations (items 1-4 for one service) travel with S1 and S3.

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
2. **Create** `DeadLetter/DeadLetterStoreAzureTable.cs` implementing `IDeadLetterStore` with the key layout of Design Choice 2: add as an entity-group transaction of the message row and the `S|{sourceMessageId}` marker (a conflict is `Right(false)`), the `I|{Id}` pointer, ETag-guarded claim and replay marking, filtered queries with server-side `Filter` strings and `MaxPageSize` paging, `DeleteManyAsync` in batches of 100.
3. **Create** `AzureTableDeadLetterOptions.cs` (account/connection data `[JsonIgnore]` with a redacting `ToString()`, table names, SDK retry options) and `ServiceCollectionExtensions.cs` (`AddEncinaDeadLetterAzureTable(...)` registers the `TableServiceClient`, creates tables on first use, and calls `AddEncinaDeadLetterQueue<DeadLetterStoreAzureTable, DeadLetterMessageFactory>`).
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
   SourcePattern; RowKey M|{inverted ticks}|{Id}; marker S|{SourceMessageId}; pointer I|{Id} in an
   index table). Validate arguments with DeadLetterStoreLimits before any I/O (throw).
2. Every infrastructure failure (RequestFailedException other than 404/409/412) becomes
   Left(EncinaError) with a DeadLetterErrorCodes code; never put exception messages in the error.
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

1. **Create** `DeadLetter/DeadLetterMessage.cs` (JSON document with `id`, `sourcePattern` as partition key, camelCase names via `[JsonPropertyName]`) and `DeadLetterMessageFactory.cs`.
2. **Create** `DeadLetter/DeadLetterStoreCosmosDB.cs`: container with partition key `/sourcePattern`, unique key `/sourceMessageId`, composite index `(/deadLetteredAtUtc, /id)`; add via `CreateItemAsync` (409 is `Right(false)`); claim and replay with `IfMatchEtag` (412 is `Right(false)`); queries with parameterized SQL (never string concatenation) and `OFFSET/LIMIT`; `DeleteManyAsync` by transactional batch per partition.
3. **Create** options (connection string/key `[JsonIgnore]`, database and container names, `CosmosClientOptions` retry), `ServiceCollectionExtensions.cs` (`AddEncinaDeadLetterCosmosDB`), `Diagnostics/Log.cs` with ids 6810-6819 and `Health/CosmosDBDeadLetterHealthCheck.cs`.
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

1. **Create** DI tests building the provider with `ValidateOnBuild` and `ValidateScopes` for both registrations, in both registration orders, with an application-registered store winning (`AGENTS.md` section 3, registration completeness).
2. **Verify** that `AddEncinaOpenTelemetry` decorates the new stores through the `IDeadLetterStore` descriptor with a test (no new `Meter`).
3. **Map** `TenantId` through `DeadLetterFilter.TenantId`/`AllTenants` exactly as the database stores do (a query returns every tenant unless the filter names one).
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
4. **Property tests** (`tests/Encina.PropertyTests/...`): result ordering `(DeadLetteredAtUtc, Id)` and filter semantics on Table Storage (inverted-tick keys).
5. **Integration tests** (`tests/Encina.IntegrationTests/...`): `[Collection("Azurite")]` and `[Collection("CosmosDb")]` fixtures with `Testcontainers.Azurite`/`Testcontainers.CosmosDb`, concurrent `TryClaimForReplayAsync` (exactly one winner), read-time expiry, `ClearAllDataAsync` in `InitializeAsync`; never a `.md` justification.
6. **Justification files**: `.md` for load and benchmark tests (append-mostly store, no hot path).

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
| **Total** | **~53** | Six pull requests (Phase 1 item 5) |

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
   Directory.Packages.props, EventId ranges 6800-6819, manifests, the six-PR split.
2. Azure Table store: PartitionKey = SourcePattern, RowKey M|{inverted ticks}|{Id}, marker row for
   the unique rule in the same entity-group transaction, pointer table for GetAsync(Guid).
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
| 2 | OpenTelemetry | ✅ Phase 4 | `InstrumentedDeadLetterStore` already decorates every `IDeadLetterStore`; a test proves the new stores are wrapped; no new `Meter` |
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

1. Link this plan from #588 and ask the maintainer to answer Design Choices 1 (package layout), 2 (key layout) and 3 (change feed) when work starts.
2. Open the follow-up issue `artifacts/issues/plan-588-deadletter-change-notification.md` (change notification across providers).
3. Implement Phases 1-6 as the six pull requests of Phase 1 item 5, each referencing `Fixes #588` only on the last.
