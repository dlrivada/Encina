# Implementation Plan: `ModuleId` on `InboxMessage` for module-scoped idempotency

> **Issue**: [#761](https://github.com/dlrivada/Encina/issues/761)
> **Type**: Feature
> **Complexity**: High (8 phases, 10 database providers, 14 pull requests; a file edited in several pull requests counts in each)
> **Estimated Scope**: ~700 lines of production code + ~7,000 lines of tests, SQL scripts, manifests and documentation (about 7,500 lines over 14 pull requests; the split table is the authority)
> **Family**: modular monolith. #747 owns the module column and the normalizer of the other four messaging rows (plan merged, code open); #2442 owns the module on the request context; #760 is the tenant sibling of this issue; #352 owns the module context producer. This plan owns the module-scoped inbox key

---

## Summary

Make inbox idempotency per module: the same external message id handled by two modules is processed once by each, instead of module A's processing hiding it from module B. `IInboxMessage` gains a nullable `ModuleId`; the inbox key (the existing `MessageId` column) becomes module-qualified for a module row; the module is filled from the request that creates the row; every one of the 10 database providers persists it; `IInboxStore` gains one strict module-filtered query.

**State of `origin/main` (`02b88ba5b`), verified by reading and grep (corrects the issue text).**

- The issue says "13 database providers" and `IModuleContext`. The matrix is 10 providers (`AGENTS.md` section 5): ADO.NET x3, Dapper x3, EF Core (one `Encina.EntityFrameworkCore` store class serves SQL Server, PostgreSQL and MySQL), MongoDB. There is no `IModuleContext`; the ambient module is `IModuleExecutionContext.CurrentModule` and #2442 adds the typed `IRequestContext.ModuleId`.
- **The inbox is keyed by `MessageId` alone.** `IInboxStore` has eight members, five of them addressed by `messageId` (`GetMessageAsync`, `MarkAsProcessedAsync`, `CacheHandlerErrorAsync`, `MarkAsFailedAsync`, `RemoveExpiredMessagesAsync(ids)`); the table has `MessageId` as primary key (`NVARCHAR(255)` SQL Server, `VARCHAR(255)` MySQL, `TEXT` PostgreSQL with lower-case unquoted names such as `messageid`, `HasKey(x => x.MessageId)` with `HasMaxLength(255)` in EF Core, `[BsonId]` in MongoDB). There is no tenant, module or other dimension on the row (a `Metadata` JSON column holds the `InboxMetadata`, except in MongoDB, whose `InboxMessage` has no such field).
- `InboxOrchestrator` (`src/Encina.Messaging/Inbox/InboxOrchestrator.cs`) is the single caller: `ProcessAsync` takes the key from `InboxPipelineBehavior` (`context.IdempotencyKey`), calls `GetMessageAsync` (line 131), `_messageFactory.Create(...)` and `AddAsync` for a new message (lines 153-160), and `MarkAsProcessedAsync`, `CacheHandlerErrorAsync` and `MarkAsFailedAsync` with the same id. It also feeds the dead letter capture with the message id as `SourceMessageId` (limit 256, `DeadLetterStoreLimits.SourceMessageIdMaxLength`). `InboxMetadata` (same file) carries `CorrelationId`, `UserId`, `TenantId` and `Timestamp`, built in `InboxPipelineBehavior` lines 83-89. The orchestrator is registered scoped (`MessagingServiceCollectionExtensions.cs` line 326).
- Implementers (grep over `src` and `tests`, class or record declarations): `IInboxMessage` has 14 (8 provider classes, `FakeInboxMessage`, `InboxMessageFaker`, `ArbitraryMessages`, two `BenchmarkEntityFactory` states, one unit test double in `InboxOrchestratorTests.cs`); `IInboxStore` has 12 (8 provider stores, `InstrumentedInboxStore`, `FakeInboxStore`, a store in `InboxPipelineScenario.cs`, one in `InboxOrchestratorRetryTests.cs`); `IInboxMessageFactory` has 8 provider factories (its interface does not change: the module reaches the row through `InboxMetadata`).
- The inbox runs in the request pipeline: it needs the module of the request. With #352's `Admission` stage the module scope is open before the inbox behavior; with #2442 `IRequestContext.ModuleId` is stamped by the dispatcher before any behavior runs.
- The sibling plans edit the same interface and decorators: #695 (a cache decorator of `IInboxStore`), #714 (a lock behavior around the inbox), #3440 (in-progress outcome on top of an `IInboxStore` claim of #2401) and #760 (tenant on the inbox row, open, no plan on `main`). Their identity of a message is the store key, so a module-qualified key flows through them unchanged (Design Choice 2).
- `EventIdRanges` has `MessagingInbox = (2100, 2199)`, used 2100-2112 (inbox store and purge logs); the orchestrator's private `Log` uses 2812-2817 inside `Messaging = (2800, 2999)`.

**Not in scope**: the other four messaging rows (#747); the tenant on the inbox (#760); the module on the request context (#2442); per-module inbox processors or retention (follow-up issue file `artifacts/issues/module-scoped-inbox-retention.md`); the module column in the CDC and read models; the dead letter tables' module column (#747 adds it).

**Affected packages**: `Encina.Messaging`, `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.OpenTelemetry` (instrumented store), `Encina.Testing.Fakes`, `Encina.Testing.Bogus`, `Encina.Testing.FsCheck`. **Provider category**: Database (10).

---

## Design Choices

<details>
<summary><strong>1. Where the inbox gets the module (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IRequestContext.ModuleId` (#2442), read in `InboxPipelineBehavior` and passed in `InboxMetadata.ModuleId`; the orchestrator gets no new constructor parameter** | Typed, stamped by the dispatcher from the handler's module, never settable by the caller; the behavior already reads `TenantId` and `UserId` from the same context | Needs #2442 pull requests 1 and 2 first |
| **B) The ambient `IModuleExecutionContext.CurrentModule`, taken through an optional constructor parameter of `InboxOrchestrator` (null when the module system is absent)** | The source #747 recommends for the other rows; independent of #2442 | Depends on #352's producer outside EF Core; the ambient value is `null` for code that lost the `AsyncLocal`; one more constructor parameter |
| **C) An explicit `moduleId` argument on `ProcessAsync`** | No ambient state | Breaks every caller and the issue's "auto-populated" criterion |

### Chosen Option: **A — the module of the request context** (recommended, pending the maintainer)

### Rationale

A wins when the module must be the module that really handles the request and no caller can claim another, which is what per-module idempotency depends on; the inbox is the one messaging pattern that sits inside the request pipeline, so the request context is there. B wins if #2442's Design Choice 1 is not accepted (then this plan reads the ambient value, as #747 does for the outbox, saga and scheduled message rows). C wins when a row must be written on behalf of another module on purpose. A blank or over-long module is not stored (Design Choice 6).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. The key of a module row (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A module-qualified key in the existing `MessageId` column: `module:<length>:<module>:<messageId>` (the length is the decimal number of characters of the normalized module, so the key parses uniquely); a row without a module keeps the plain `MessageId`; a plain id that starts with `module:` is rejected (reserved prefix); the whole key must fit 255 characters (`inbox.message_id_too_long` otherwise)** | Injective by construction (no two `(module, id)` pairs, and no host id, produce one key); no primary key change; all eight store members, the cache of #695, the locks of #714 and the claims of #2401 keep working with the key as their identity; dead letters fit their 256 limit; the raw id stays readable in the key | A host id starting with `module:` is refused; module rows accept a shorter external id (at least 115 characters with the longest module) |
| **B) A fixed-width key: `module:` plus the base64url SHA-256 of the same tuple** | No length limit; short keys | The raw id is no longer in the key (MongoDB has no `Metadata` field to keep it); only collision resistant, not injective by construction |
| **C) A composite primary key `(MessageId, ModuleId)` with `ModuleId` stored as an empty string for host rows and `moduleId` parameters on the five keyed store members** | The relational design | Changes five members of `IInboxStore` on 12 implementers while #695, #714, #3440 and #2401 edit the same interface; `ModuleId` null versus empty differs per provider (NULLs in unique indexes); the issue asks for a nullable column |

### Chosen Option: **A — a readable, injective, bounded key** (recommended, pending the maintainer)

### Rationale

A wins when the store contract must not move: it gives per-module idempotency by changing the key, one public helper, `InboxMessageKey.Create(moduleId, messageId)`, used by the orchestrator for every store call and for the dead letter `SourceMessageId`. B wins when external ids can be long and a length limit is unacceptable. C wins when a single query by the raw id across modules is required of the store itself. The `ModuleId` column is still stored (nullable, `VARCHAR(128)`) for the module-filtered query and operations. The reserved-prefix rejection returns `inbox.reserved_message_id_prefix`, ADR-062 category `Validation`, and the length rejection `inbox.message_id_too_long`, category `Validation` (both follow the existing `inbox.*` codes of `InboxErrorCodes`, for example `inbox.missing_message_id`, and are added to `PublicAPI.Unshipped.txt` as the `const` lines `InboxErrorCodes.ReservedMessageIdPrefix` and `InboxErrorCodes.MessageIdTooLong`); neither carries the id. The tenant sibling #760 would add a tenant segment in the same helper with the same length-prefix rule, so two tenants or two modules can never map to one key; the reserved `default` tenant segment of tenant-qualified ids is #760's, and no tenant segment is built here.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. The stored module and its case (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The module as `MessagingModuleId.Normalize` of #747 writes it: trimmed, lower-invariant, at most 128 characters, blank rejected** | One answer on all 10 providers (SQL Server and MySQL collations compare case-insensitively, PostgreSQL and MongoDB case-sensitively); the same value as the other four rows | Not the display casing |
| **B) The registered name as written** | No transformation | The same filter returns different rows per provider |

### Chosen Option: **A — the normalizer of #747** (recommended, pending the maintainer)

### Rationale

A wins because provider coherence (`AGENTS.md` section 3) needs one answer for one filter; #747's D3 recommends the same and #1636 the same for audit rows. B wins if module names are case-sensitive identifiers. `MessagingModuleId` is created by #747's pull request 1; this plan's pull request 1 waits for it (Prerequisites). `ModuleId` is a column and a key segment, never a tenant-qualified identifier, so the reserved `default` tenant segment does not apply.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. What the module filter returns</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Strict equality: `GetByModuleAsync(moduleId, skip, take)` returns only rows whose `ModuleId` equals the normalized module; a blank module is an `ArgumentException`, never "all rows" and never "rows without a module"** | A missing module context cannot widen a query | A caller that wants every row uses the existing members |
| **B) A null module means no filter** | One member | Fail open: a lost module returns every module's rows |

### Chosen Option: **A — strict equality, blank is rejected**

### Rationale

Gates fail closed when context is missing, and the acceptance criterion is a module-filtered query, not an optional filter.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (compliance and security gates fail closed when context is missing) and the acceptance criterion "Support module-filtered inbox queries" of #761; the same rule as Design Choice 4 of #747.

</details>

<details>
<summary><strong>5. The shape of the query member (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One dedicated member `Task<Either<EncinaError, IEnumerable<IInboxMessage>>> GetByModuleAsync(string moduleId, int skip, int take, CancellationToken cancellationToken = default)`, newest received first, `take` bounded (1 to 1000)** | Strict by shape; same style as `IDeadLetterStore.GetByModuleAsync` of #747; the existing members keep their contract | One interface member and one decorator member |
| **B) An optional `moduleId` parameter on `GetExpiredMessagesAsync`** | No new member | Null would mean "all" (fail open) and the expiry query is not the operator's query |
| **C) A filter object** | Extensible | No filter object exists for the inbox; one filter for one member is over-design |

### Chosen Option: **A — one dedicated member** (recommended, pending the maintainer)

### Rationale

A wins when strictness must be structural and the listing is for operators and tests. B wins if the only wanted filter is the cleanup of one module's expired rows. The cleanup processor keeps using `GetExpiredMessagesAsync`, which already returns every module's rows with their qualified keys, so per-module retention is the follow-up issue file.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Pull request order (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Implementers first, interfaces last: each provider pull request adds `ModuleId` and `GetByModuleAsync` to its concrete classes (they compile against the old interfaces); the interface and decorator pull request comes after every implementer has the members; the orchestrator pull request is last** | Every pull request builds and carries its own tests; no scaffold merges | The members exist on classes before the interfaces have them; 14 pull requests |
| **B) One pull request with the whole change** | One review | About 141 file touches: over the 15-file and 1500-line limits (needs `size-exception-approved`) |
| **C) Default interface members as a scaffold** | Interfaces first | A compatibility layer merges (`AGENTS.md` section 1) and the defaults are untested production code |

### Chosen Option: **A — implementers first, interfaces last** (recommended, pending the maintainer)

### Rationale

A wins because the limits are hard gates and a half-wired interface would break the build between pull requests; it is the order #747 recommends and the two plans touch different files (inbox files only), so they do not conflict. The plans of #695, #714, #3440 and #2401 add decorators or members to `IInboxStore` too: whichever interface-changing pull request merges second adds its member to the other's implementers (the list of 12 implementers is re-grepped at work start).

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Schema delivery: edit the scripts in place</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add the nullable column and an index to the existing create scripts (`002_CreateInboxMessagesTable.sql` and `000_CreateAllTables.sql` per package), the EF configuration and the MongoDB index creator; the key column keeps its width; no migration** | Pre-1.0 has no existing user and no migration support | Existing development databases are recreated |
| **B) `ALTER TABLE` migration scripts** | Upgrades in place | A migration helper, forbidden pre-1.0 |

### Chosen Option: **A — in-place scripts**

### Rationale

Encina is pre-1.0 with no existing user and no migration support.

**Class:** Settled
**Settled by:** `AGENTS.md` section 1 (pre-1.0: no backward compatibility, no migration support) and section 3 (no migration helpers).

</details>

<details>
<summary><strong>8. Telemetry and logs</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse the span tag `module.name` on the existing inbox activity (`InboxActivitySource`) and on the instrumented store; one log class `InboxModuleLog` with three messages, no per-request Information log; no new metric** | One tag vocabulary; the module set is bounded | None |
| **B) A new counter per module** | A number per module | An unbounded-looking label for a value the activity already carries |

### Chosen Option: **A — the existing tag and three logs**

### Rationale

`module.name` is the module tag of `ModuleActivitySource`; the tenant tag appears only where a tenant exists (ADR-054 D4).

**Class:** Trivial
**Settled by:** `src/Encina/Modules/Diagnostics/ModuleActivitySource.cs` (tag `module.name`), `src/Encina.Messaging/Diagnostics/InboxActivitySource.cs` and [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D4.

</details>

---

## Implementation Phases

### Phase 1: Foundation in `Encina.Messaging` (pull request 1)

> **Goal**: the key helper and the metadata member, so provider pull requests have one rule to call.

<details>
<summary>Tasks</summary>

1. `src/Encina.Messaging/Inbox/InboxMessageKey.cs` (public static): `const int MaxLength = 255`, `const string ReservedPrefix = "module:"`; `Either<EncinaError, string> Create(string? moduleId, string messageId)` (a null or blank module returns `messageId` after checking it does not start with the prefix; otherwise `module:<length>:<module>:<messageId>` with `moduleId` normalized through `MessagingModuleId.Normalize`; a key over `MaxLength` returns `inbox.message_id_too_long`); `bool TryParse(string key, out string? moduleId, out string messageId)` for tests and diagnostics.
2. `src/Encina.Messaging/Inbox/InboxOrchestrator.cs`: `InboxMetadata.ModuleId` (`string?`), `InboxErrorCodes.ReservedMessageIdPrefix` and `InboxErrorCodes.MessageIdTooLong` (both `ErrorCategory.Validation`, set where the error is created with the categorized overload; the values are `inbox.reserved_message_id_prefix` and `inbox.message_id_too_long`, and both constants get a `const` line in `src/Encina.Messaging/PublicAPI.Unshipped.txt`).
3. Tests: unit (`InboxMessageKeyTests`: host id unchanged, module key shape, length prefix, reserved prefix rejected, 255 and 256 boundaries, `TryParse` round trip), guard (`Create` blank message id and null message id; a null module is a valid-result test), property (injective: for any two different `(module, id)` pairs the keys differ, `TryParse(Create(m, id)) == (normalize(m), id)`, and no plain id equals a module key).
4. `PublicAPI.Unshipped.txt` and the `Encina.Messaging.json` manifest through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #761; plan docs/plans/inbox-module-id-implementation-plan-761.md. D1-D3, D4, D5 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- #747 pull request 1 provides MessagingModuleId.Normalize; if it is not on main this pull request waits.

TASK:
Add InboxMessageKey, InboxMetadata.ModuleId and the two error codes with their tests.

KEY RULES:
- The key must be injective: no two (module, id) pairs and no host id map to one key; property-test it.
- Errors are Either values with an ADR-062 category; the error never carries the id.
- XML docs on every public member; zero warnings; PublicAPI.Unshipped.txt updated.

REFERENCE FILES:
- src/Encina.Messaging/Inbox/InboxOrchestrator.cs
- src/Encina.Messaging/Inbox/IInboxMessage.cs
- docs/plans/module-id-messaging-implementation-plan-747.md
```

</details>

---

### Phase 2: ADO.NET and Dapper providers (6 packages, 6 pull requests)

> **Goal**: column, row class, factory copy, index and the module query in each of the six packages.

<details>
<summary>Tasks</summary>

Per package (`Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`), one pull request of 11 files:

1. `Inbox/InboxMessage.cs`: `string? ModuleId` (compiles against the old interface); `Inbox/InboxMessageFactory.cs`: copies `metadata?.ModuleId` through `MessagingModuleId.NormalizeOrNull`.
2. `Inbox/InboxStoreADO.cs` or `InboxStoreDapper.cs`: `ModuleId` in the `INSERT` and in every `SELECT` column list, and `GetByModuleAsync(moduleId, skip, take)`: strict `ModuleId = @ModuleId` (the argument through `MessagingModuleId.Normalize`), `ORDER BY ReceivedAtUtc DESC`, the provider's paging syntax (SQL Server `OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY`, PostgreSQL and MySQL `LIMIT @take OFFSET @skip`), `take` bounded 1 to 1000, a blank module an `ArgumentException`.
3. Scripts `Scripts/002_CreateInboxMessagesTable.sql` and `Scripts/000_CreateAllTables.sql`: `ModuleId` (`NVARCHAR(128) NULL` SQL Server, `VARCHAR(128) NULL` MySQL with `utf8mb4_bin`, `moduleid VARCHAR(128) NULL` PostgreSQL beside the lower-case names) and an index `(ModuleId, ReceivedAtUtc)`; the key column keeps its width (D2).
4. `PublicAPI.Unshipped.txt` and the manifest of the package (through `mechanical-fixer`); an integration test class `ModuleIdInboxIntegrationTests` (`[Collection("ADO-<Db>")]` or `[Collection("Dapper-<Db>")]`, `ClearAllDataAsync` in `InitializeAsync`): the column round trips, the same external id under two modules is stored as two rows and processed independently, the strict filter returns only the module's rows, a host row and another module's row are not returned, blank throws; the blank-module case added to the existing guard file (`tests/Encina.GuardTests/ADO/<Db>/InboxStoreADOGuardsTests.cs` or `tests/Encina.GuardTests/Dapper/<Db>/InboxStoreDapperGuard(s)Tests.cs`, exact name as on main) and the module case added to the existing `InboxMessageFactoryTests.cs` of the package.
5. All database calls stay asynchronous with a `CancellationToken`; never `IDbConnection.Open()`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #761. The interfaces are NOT changed yet (D5): this pull request adds ModuleId and GetByModuleAsync to ONE package's concrete classes so that it builds against the current interfaces. Phase 1 is merged.

TASK:
Apply the Phase 2 tasks to the one package of this pull request; the six implementations must behave identically and differ only in SQL dialect.

KEY RULES:
- Strict equality (Design Choice 4): a blank module id throws ArgumentException; a null column never matches a module.
- The argument and the stored value go through MessagingModuleId (D3); the key column is untouched (D2).
- SQL Server: @param, OFFSET/FETCH; PostgreSQL: lower-case unquoted names, LIMIT; MySQL: backticks, utf8mb4_bin.
- Integration tests use the shared [Collection] fixture, never IClassFixture; no migrations (Design Choice 7).

REFERENCE FILES:
- src/Encina.ADO.PostgreSQL/Inbox/InboxStoreADO.cs
- src/Encina.Dapper.SqlServer/Inbox/InboxStoreDapper.cs
- src/Encina.ADO.MySQL/Scripts/002_CreateInboxMessagesTable.sql
- tests/Encina.IntegrationTests/ADO/PostgreSQL/Inbox/InboxStoreADOTests.cs
```

</details>

---

### Phase 3: EF Core and MongoDB providers (2 pull requests)

<details>
<summary>Tasks</summary>

1. EF Core (pull request 8, 11 files): `Inbox/InboxMessage.cs` (`ModuleId`), `Inbox/InboxMessageConfiguration.cs` (`HasMaxLength(128)`, `HasIndex(x => new { x.ModuleId, x.ReceivedAtUtc })` beside the existing indexes), `Inbox/InboxMessageFactory.cs`, `Inbox/InboxStoreEF.cs` (`GetByModuleAsync` as a LINQ equality predicate with `Skip`/`Take`); PublicAPI, manifest, the module integration test as three classes (`EFCore-SqlServer`, `EFCore-PostgreSQL`, `EFCore-MySQL`, in the existing `Infrastructure/EntityFrameworkCore/<Db>/Inbox/` folders), the guard case in `InboxStoreEFGuardTests.cs` and the factory case in `InboxMessageFactoryTests.cs`. No EF migration is added (D7).
2. MongoDB (pull request 9, 9 files): `Inbox/InboxMessage.cs` (`ModuleId`), `Inbox/InboxMessageFactory.cs`, `Inbox/InboxStoreMongoDB.cs` (`GetByModuleAsync` with a strict filter and sort), `MongoDbIndexCreator.cs` (`CreateInboxIndexesAsync` gains the compound index `(ModuleId, ReceivedAtUtc)` named `IX_Inbox_Module`, guarded by the existing `UseInbox` flag), PublicAPI, manifest, one integration test class on the MongoDB fixture, the guard case in the inbox guard tests and the factory case in `tests/Encina.UnitTests/MongoDB/Inbox/InboxMessageFactoryTests.cs`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Issue #761. Phase 2 pull requests are merged; the interfaces are still unchanged (D5).

TASK:
Implement the Phase 3 tasks, one pull request per item (EF Core, MongoDB).

KEY RULES:
- One EF store behavior for SQL Server, PostgreSQL and MySQL, proved by integration tests on each database.
- The normalized value is written and filtered (D3); blank is an ArgumentException (Design Choice 4).
- MongoDB index creation goes through MongoDbIndexCreator, guarded by UseInbox.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Inbox/InboxMessageConfiguration.cs
- src/Encina.EntityFrameworkCore/Inbox/InboxStoreEF.cs
- src/Encina.MongoDB/MongoDbIndexCreator.cs
- src/Encina.MongoDB/Inbox/InboxStoreMongoDB.cs
```

</details>

---

### Phase 4: Fakes, generators and test doubles (pull request 10)

<details>
<summary>Tasks</summary>

1. `Encina.Testing.Fakes`: `FakeInboxMessage` gets `ModuleId`; `FakeInboxStore` gets `GetByModuleAsync` with the strict rule and the same key behavior (the fake stores whatever key it is given, like the real stores).
2. `Encina.Testing.Bogus` (`InboxMessageFaker`) and `Encina.Testing.FsCheck` (`ArbitraryMessages`): the property, generated as a valid normalized module or null. `Encina.Testing.FsCheck` has only a `PublicAPI.Shipped.txt` with `#nullable enable` and no `PublicAPI.Unshipped.txt`, so no line is added there.
3. Message doubles that implement `IInboxMessage`: `BenchmarkEntityFactory` in `Encina.ADO.Benchmarks` and `Encina.Dapper.Benchmarks` and the double in `tests/Encina.UnitTests/Messaging/Inbox/InboxOrchestratorTests.cs`.
4. PublicAPI of `Encina.Testing.Fakes` and `Encina.Testing.Bogus`, three manifests, the edits to `FakeInboxStoreTests.cs` and `InboxMessageFakerTests.cs`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #761. The fakes must behave exactly like the real stores for the module rules; the contract test of Phase 5 runs against them.

TASK:
Add ModuleId and GetByModuleAsync to the fakes, generators and doubles listed in the Phase 4 tasks.

KEY RULES:
- This pull request compiles against the current interfaces: add members to classes, do not touch interfaces.
- The fake normalizes through MessagingModuleId like the real stores.

REFERENCE FILES:
- src/Encina.Testing.Fakes/Stores/FakeInboxStore.cs
- src/Encina.Testing.Bogus/InboxMessageFaker.cs
- src/Encina.Testing.FsCheck/ArbitraryMessages.cs
- tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks/Infrastructure/BenchmarkEntityFactory.cs
```

</details>

---

### Phase 5: Interfaces and the instrumented store (pull request 11)

<details>
<summary>Tasks</summary>

1. `IInboxMessage.ModuleId { get; set; }` and `IInboxStore.GetByModuleAsync(...)` with XML docs (the normalized module of the request that created the row, null for host work).
2. `InstrumentedInboxStore` (`src/Encina.OpenTelemetry/MessagingStores/`): delegates the new member and tags the existing activity `module.name`; the two store doubles outside the provider packages (`InboxPipelineScenario.cs`, `InboxOrchestratorRetryTests.cs`) gain the member.
3. PublicAPI of `Encina.Messaging` and `Encina.OpenTelemetry`, two manifests, `InstrumentedInboxStoreTests` and `InstrumentedInboxStoreGuardTests` edits, contract test `InboxModuleContractTests` (every real row class and the fake round-trips the property; the fake and the instrumented store apply the strict rule).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Issue #761. All provider classes, fakes and doubles already have ModuleId and the query (pull requests 2-10).

TASK:
Add the members to the interfaces, the decorator and the store doubles in one pull request that builds alone.

KEY RULES:
- A decorator delegates and adds only the span tag; it never swallows a Left.
- No [Obsolete], no default interface members. Re-grep implementers of IInboxMessage and IInboxStore at work start.

REFERENCE FILES:
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedInboxStore.cs
- src/Encina.Messaging/Inbox/IInboxStore.cs
- tests/Encina.IntegrationTests/Messaging/InboxPipelineScenario.cs
```

</details>

---

### Phase 6: The behavior, the orchestrator and observability (pull request 12)

> **Goal**: the behavior of the issue: auto-populate from the request and scope idempotency per module.

<details>
<summary>Tasks</summary>

1. `InboxPipelineBehavior.ProcessIdempotentAsync`: `InboxMetadata.ModuleId = context.ModuleId` (D1).
2. `InboxOrchestrator.ProcessAsync`: the first step builds `storeKey = InboxMessageKey.Create(metadata?.ModuleId, messageId)`; a `Left` is returned as the operation's result (a reserved prefix or a too-long key fails the request, nothing is stored); `storeKey` is used for `GetMessageAsync`, `AddAsync` (through the factory, with `metadata.ModuleId`), `MarkAsProcessedAsync`, `CacheHandlerErrorAsync`, `MarkAsFailedAsync` and the dead letter `SourceMessageId` (`InboxAttempt.MessageId`). The existing log and metric arguments keep the raw id out of new tags.
3. `src/Encina.Messaging/Diagnostics/InboxModuleLog.cs` (`[LoggerMessage]`): EventIds 7700 module-scoped key built (Debug, module name only), 7701 reserved prefix rejected (Warning, error code only), 7702 message id too long (Warning, error code only). Range `EventIdRanges.MessagingInboxModule = (7700, 7709)` registered first in `src/Encina/Diagnostics/EventIdRanges.cs` and in the `Encina.Messaging` entry of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
4. `InboxActivitySource`: the tag `module.name` when a module is present.
5. Tests: unit (the same raw id under two modules is processed twice and each cached response is returned to its own module; a host request and a module request with the same raw id are independent; a duplicate under the same module returns the cached response; the reserved prefix and the length limit return their codes and store nothing; the dead letter capture receives the module key; an ordering test asserts that the module is already present when the inbox behavior runs), guard (behavior and orchestrator constructors unchanged: no new guard cases; a null `ModuleId` in `InboxMetadata` is a valid-result test), DI test (`ValidateOnBuild` and `ValidateScopes`, with and without modules), property (a stored key parses back to its module and id), contract (`InboxPipelineBehaviorContractTests` edit).
6. `PublicAPI.Unshipped.txt`, manifest through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Issue #761. The interfaces and every implementer have ModuleId (Phases 1-5). #2442 pull requests 1 and 2 supply IRequestContext.ModuleId; tests set it with WithModuleId.

TASK:
Implement the Phase 6 tasks in src/Encina.Messaging.

KEY RULES:
- Every store call and the dead letter capture use the same storeKey; never mix the raw id and the key.
- A Left from the key helper fails the operation visibly; nothing is stored and nothing reports success (AGENTS.md section 3).
- Never claim atomicity: the inbox marks the message in the store call and the business transaction is owned elsewhere (ADR-048).
- Register the EventId range first; ids packed from 7700 with no gaps; logs carry error codes and module names only, never the message id or EncinaError.Message.

REFERENCE FILES:
- src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs
- src/Encina.Messaging/Inbox/InboxOrchestrator.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Messaging/Inbox/InboxOrchestratorTests.cs
```

</details>

---

### Phase 7: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Idempotency (function 9): Phases 1 and 6, the feature itself.
2. Module isolation (function 11): the key, the column and the filter; the enforcement of who may read which rows stays with #352 and #359.
3. Multi-tenancy (function 10): the module and a future tenant are independent segments of the key built in one helper (Design Choice 2); the tenant column and key segment are #760.
4. Deferred as an issue file in this plan's worktree: `artifacts/issues/module-scoped-inbox-retention.md` (per-module purge and expiry using `GetByModuleAsync`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT:
- Issue #761; the cross-cutting matrix of the plan is the contract.

TASK:
Verify that each Include row of the matrix exists in code and tests, and that the issue file is written.

KEY RULES:
- A module filter and a tenant filter are never merged into one predicate by default.
- Never log the message id, the response or EncinaError.Message.

REFERENCE FILES:
- AGENTS.md sections 3 and 6
- docs/plans/saga-tenant-implementation-plan-738.md
```

</details>

---

### Phase 8: Testing and Documentation (pull requests 13 and 14)

<details>
<summary>Tasks</summary>

Tests travel with the pull request that adds the code they cover (see the split).

1. **Unit**: pull requests 1 to 12. **Guard** (`tests/Encina.GuardTests`): `InboxMessageKey.Create` and the blank-module cases of the store members in the per-store guard files (PR 2-9, 10, 11). **Contract**: `InboxModuleContractTests` (PR 11) and the behavior contract (PR 12). **Property**: key injectivity and round trip (PR 1, 12).
2. **Integration**: store-level tests in each provider pull request; the end-to-end test in pull request 13: the same external message id is sent to two module handlers with the real inbox behavior, each handler runs once, a replay of each returns its cached response, on every provider through its shared collection (`ADO-` and `Dapper-` x `SqlServer`/`PostgreSQL`/`MySQL`, `EFCore-` x three, MongoDB: 10 classes in 4 files); `ClearAllDataAsync` in `InitializeAsync`; never `IClassFixture`. **Load** and **Benchmark**: justification files `tests/Encina.LoadTests/Messaging/InboxModuleId.md` and `tests/Encina.BenchmarkTests/Messaging/InboxModuleId.md` (one extra column and one string concatenation; the existing inbox load and benchmark tests cover the path).
3. **Documentation** (pull request 14): XML docs on every new public member; the messaging guide gains "Module-scoped idempotency" (through `docs-writer`); `docs/INVENTORY.md` line; `changelog.d/761-inbox-module-id.added.md` (through `mechanical-fixer`).
4. **Per-file targets** (written into the manifests with one-sentence justifications; baselines of edited files are measured at work start with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and `.github/scripts/coverage-report.cs`; a target is the measured value or higher):

   | File group | Unit | Guard | Contract | Property | Integration | Justification |
   |---|---|---|---|---|---|---|
   | `Inbox/InboxMessageKey.cs` | 100 | 90 | 0 | 90 | 0 | Public pure function whose every branch is a unit test; guard reaches the argument checks; the property test proves injectivity |
   | `InboxOrchestrator.cs`, `InboxPipelineBehavior.cs` (edits) | 95 | 80 | 80 | 60 | 0 | Key use, rejection and per-module caching run with fake stores; the real stores have their own integration flag |
   | `InboxModuleLog.cs` | 90 | 0 | 0 | 0 | 0 | Source-generated log methods reached through the orchestrator tests; no parameterized public member |
   | Provider stores, row classes, factories (ADO.NET x3, Dapper x3, EF Core, MongoDB) | 60 | 90 | 0 | 0 | 80 | The SQL, LINQ and Mongo filters can only be proved on real databases (`AGENTS.md` section 5); guard cases cover the blank-module checks; contract 0 because the contract test runs against fakes and row classes, not databases |
   | Fakes, instrumented store | 90 | 90 | 80 | 0 | 0 | Test doubles must behave like the real stores (contract test of PR 11); delegation plus one span tag is reachable through a listener test |
   | Fakers and `ArbitraryMessages` | 90 | 0 | 0 | 90 | 0 | Generators have no parameterized member worth a guard test; their output is exercised by the property tests of PR 1 and 12 |

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
CONTEXT:
- Issue #761. Tests travel with the pull request that adds the code they cover; the end-to-end test is pull request 13; AGENTS.md section 9 per-flag coverage model.

TASK:
Write the tests of Phase 8 and the documentation set through the specialists that own them, and fill the manifest targets with one-sentence justifications.

KEY RULES:
- Integration tests on real databases for all 10 providers, never a .md for them; Shouldly through Encina.Testing.Shouldly.
- Documentation pages go through docs-writer; fragments, PublicAPI lines and manifests through mechanical-fixer; never edit CHANGELOG.md.
- A nullable parameter that accepts null gets a valid-result test, not an exception test. Targets never go down.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Messaging/InboxPipelineScenario.cs
- docs/testing/integration-tests.md
- .github/coverage-manifest/Encina.Messaging.json
- changelog.d/README.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

Order rule (D5 option A): implementers first, interfaces last; every pull request builds, carries its own tests, `PublicAPI.Unshipped.txt` and manifest entries. Prerequisites outside this plan: pull request 1 needs #747 pull request 1 (`MessagingModuleId`, whose source Dilemma D3 is still pending there) and pull request 12 needs #2442 pull requests 1 and 2, so the chain is unbuildable until those land.

| PR | Content | Files | Approx. lines |
|----|---------|------:|--------------:|
| 1 | `Encina.Messaging`: `InboxMessageKey`, `InboxOrchestrator.cs` (metadata member, two codes), PublicAPI, manifest, 3 test files | 7 | 450 |
| 2-7 | One per ADO.NET and Dapper package: row class, factory, store, 2 scripts, PublicAPI, manifest, integration test, guard edit, factory unit test edit | 11 | 520 |
| 8 | `Encina.EntityFrameworkCore`: row, configuration, factory, store, PublicAPI, manifest, 3 integration classes (3 files), guard edit, factory test edit | 11 | 640 |
| 9 | `Encina.MongoDB`: row, factory, store, index creator, PublicAPI, manifest, integration test, guard edit, factory test edit | 9 | 430 |
| 10 | Fakes (2), Bogus faker, FsCheck arbitraries, 2 benchmark states, 1 unit test double, PublicAPI x2, manifests x3, 2 test edits | 14 | 520 |
| 11 | `IInboxMessage`, `IInboxStore`, `InstrumentedInboxStore`, 2 store doubles, PublicAPI x2, manifests x2, 2 decorator test edits, contract test | 12 | 560 |
| 12 | `InboxPipelineBehavior`, `InboxOrchestrator`, `InboxModuleLog`, `InboxActivitySource`, `EventIdRanges`, allocation map, PublicAPI of `Encina.Messaging` and PublicAPI of `Encina` (the `EventIdRanges.MessagingInboxModule` field line, AGENTS section 7), manifest, orchestrator and behavior unit tests, DI test, property test, behavior contract edit | 14 | 1,000 |
| 13 | End-to-end integration test on 10 providers (4 files holding 10 classes, one `[Collection]` per class: ADO 3, Dapper 3, EF Core 3, MongoDB 1), 2 justification files | 6 | 600 |
| 14 | Documentation: messaging guide, inventory line, changelog fragment | 3 | 200 |

The recount: PRs 2 to 7 are one per package (6 pull requests, 11 files each = 66 file touches); the sequence is 1 to 14 (14 pull requests, 7 + 66 + 11 + 9 + 14 + 12 + 14 + 6 + 3 = 142 file touches; a file edited in several pull requests counts in each). PR 8 = 4 production + 1 PublicAPI + 1 manifest + 3 integration classes + 1 guard edit + 1 factory-test edit = 11; PR 12 = 6 production and registry files + 2 PublicAPI (`Encina.Messaging` and `Encina`) + 1 manifest + 2 unit test files + 1 DI test + 1 property test + 1 contract edit = 14. The 14-pull-request chain cannot be built until its prerequisites are on `main`: #747 pull request 1 (`MessagingModuleId`; its source Dilemma D3 is still pending there) before pull request 1 of this plan, and #2442 pull requests 1 and 2 before pull request 12; if either answer changes, the pull requests that call the prerequisite wait. A pull request that measures over 15 files or 1,500 lines when it is briefed is split again. The six providers of PRs 2 to 7 are independent and can be reviewed in parallel; PR 2 to 9 need PR 1; PR 11 needs PR 2 to 10; PR 12 needs PR 11 and #2442 pull requests 1 and 2.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| `AGENTS.md` section 3 | Gates fail closed; a `Left` is never swallowed; `EncinaError.Message` never logged | Strict filter, rejected keys fail the request |
| `AGENTS.md` section 5 | 10 database providers | Provider table |
| `AGENTS.md` section 6, functions 9 and 11 | Idempotency and the module dimension | The feature itself |
| [ADR-048](../architecture/adr/index.md) | Transaction boundary of the inbox mark | No atomicity claim |
| [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D4 | Tenant tag only when multi-tenancy is on | Span tags |
| [ADR-062](../architecture/adr/062-error-category-contract.md) | `ErrorCategory` on every error code | Both new codes are `Validation` |
| Modular monolith guidance (Spring Modulith event publication registry, Kamil Grzybek's modular monolith) | A consumed message belongs to the module that handled it | Concept source named by the issue |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `InboxOrchestrator`, `InboxMetadata`, `InboxErrorCodes` | `src/Encina.Messaging/Inbox/InboxOrchestrator.cs` | Key use and metadata member |
| `InboxPipelineBehavior` | `src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs` | Reads the request module |
| `IInboxStore`, `IInboxMessage` | `src/Encina.Messaging/Inbox/` | Interfaces to extend |
| `IRequestContext.ModuleId` | #2442 (planned) | Source of the module |
| `MessagingModuleId` | #747 pull request 1 (planned) | Normalizer |
| `DeadLetterSourceCapture`, `DeadLetterStoreLimits` | `src/Encina.Messaging/DeadLetter/` | The key is the `SourceMessageId` (limit 256) |
| Inbox store files (8 providers) | `src/Encina.*/Inbox/` | Column and filter |
| `MongoDbIndexCreator.CreateInboxIndexesAsync` | `src/Encina.MongoDB/MongoDbIndexCreator.cs` | Compound index |
| `InstrumentedInboxStore` | `src/Encina.OpenTelemetry/MessagingStores/InstrumentedInboxStore.cs` | Delegation and span tag |
| `FakeInboxStore` and fakers | `src/Encina.Testing.Fakes/`, `Encina.Testing.Bogus/`, `Encina.Testing.FsCheck/` | Test doubles |
| `InboxActivitySource` | `src/Encina.Messaging/Diagnostics/InboxActivitySource.cs` | Span tag |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (`MessagingInboxModule`, new) | 7700-7709, ids 7700-7702 used | From the assigned block 7700-7749 (registry `artifacts/decisions/plan-batch/eventid-blocks.md`); 7703-7709 free; the allocation test map gains the range under `Encina.Messaging`. Other plans of the family: #2442 7710-7719, #753 7720-7729 |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Messaging core, interfaces, decorator, behavior (PR 1, 11, 12) | 33 | 7 + 12 + 14, with their tests, PublicAPI and manifests |
| ADO.NET x3 and Dapper x3 (PR 2-7) | 66 | 6 x 11: row, factory, store, scripts, PublicAPI, manifest, integration, guard and factory-test edits |
| EF Core and MongoDB (PR 8, 9) | 20 | 11 + 9 |
| Fakes, fakers, doubles (PR 10) | 14 | |
| End-to-end tests and documentation (PR 13, 14) | 9 | 6 + 3 |
| **Total** | **142 file touches across 14 pull requests** | The sum of the rows of the split table is the authority |

### Provider Matrix

| Provider | Column and row class | Store filter | Integration test |
|----------|----------------------|--------------|------------------|
| ADO-SqlServer, ADO-PostgreSQL, ADO-MySQL | Required | Required | `[Collection("ADO-<Db>")]` |
| Dapper-SqlServer, Dapper-PostgreSQL, Dapper-MySQL | Required | Required | `[Collection("Dapper-<Db>")]` |
| EFCore-SqlServer, EFCore-PostgreSQL, EFCore-MySQL | One entity class | Required | `[Collection("EFCore-<Db>")]` x3 |
| MongoDB | Required (document field) | Required | MongoDB fixture |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers, no migrations.
- Issue #761: module-scoped inbox idempotency for the 10 database providers. Plan: docs/plans/inbox-module-id-implementation-plan-761.md; Dilemmas D1-D5 are recommended, pending the maintainer. Prerequisites: #747 pull request 1 (MessagingModuleId) and #2442 pull requests 1 and 2 (IRequestContext.ModuleId).

IMPLEMENTATION OVERVIEW (the order of the 14 pull requests, 2-7 being the six ADO.NET and Dapper packages; every one builds):
1. InboxMessageKey, InboxMetadata.ModuleId and the two error codes (Phase 1).
2. ADO.NET and Dapper packages, then EF Core and MongoDB (Phases 2-3).
3. Fakes, generators, benchmark states and message doubles (Phase 4).
4. IInboxMessage, IInboxStore, the instrumented store and its doubles (Phase 5).
5. Behavior and orchestrator: key, rejection, logs and range (Phase 6).
6. End-to-end tests on real databases, then documentation (Phase 8).

KEY PATTERNS:
- The key of a module row is module:<length>:<module>:<messageId> in the existing MessageId column; host rows keep the plain id; a plain id starting with module: is rejected; the key fits 255 characters; the store contract does not change.
- The module comes from IRequestContext.ModuleId through InboxMetadata, is normalized lower-invariant (#747) and stored in a nullable ModuleId column; filters are strict equality and a blank module id is an ArgumentException.
- Dialects: SQL Server OFFSET/FETCH; PostgreSQL LIMIT and lower-case names; MySQL LIMIT, utf8mb4_bin.
- Async database calls with CancellationToken; no EncinaError.Message or message ids in logs; no atomicity claim.
- Tests: Shouldly, shared [Collection] fixtures, per-flag coverage from the manifests.

REFERENCE FILES:
- src/Encina.Messaging/Inbox/InboxOrchestrator.cs
- src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs
- src/Encina.EntityFrameworkCore/Inbox/InboxStoreEF.cs
- src/Encina.MongoDB/MongoDbIndexCreator.cs
- src/Encina.ADO.PostgreSQL/Scripts/002_CreateInboxMessagesTable.sql
- docs/plans/module-id-messaging-implementation-plan-747.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: the inbox needs read-your-writes; #695's cache keys on the store key, so a module key is a different entry |
| 2 | OpenTelemetry | ✅ | Phase 6: span tag `module.name` on the inbox activity and the instrumented store; no new metric; tenant tag only where a tenant exists (ADR-054 D4) |
| 3 | Structured Logging | ✅ | Phase 6: `InboxModuleLog`, EventIds 7700-7702, error codes and module names only |
| 4 | Health Checks | ❌ | Not applicable: no new dependency; `InboxHealthCheck` is unchanged |
| 5 | Validation | ✅ | Phase 1: the key helper validates the reserved prefix and the length; `MessagingModuleId.Normalize` bounds the module |
| 6 | Resilience | ❌ | Not applicable: no external call is added; a rejected key fails the request and is not retried |
| 7 | Distributed Locks | ❌ | Not applicable: #714 hashes the raw key for its lock name, so two modules with one raw key only serialize each other (safe); module scoping does not change row claiming |
| 8 | Transactions | ❌ | Not applicable: the column is written with the row by the same store call; commit ownership stays with the transaction behaviors (ADR-048) |
| 9 | Idempotency | ✅ | This is the feature: per-module deduplication through the module-qualified key |
| 10 | Multi-Tenancy | ⏭️ | Deferred: the tenant on the inbox row and key is #760 (open); the key helper is built to take a tenant segment with the same length-prefix rule, and no tenant-qualified identifier is built here, so the reserved `default` segment does not apply |
| 11 | Module Isolation | ✅ | The column, the key and the filter; enforcement of who may read which rows stays with #352 and #359 |
| 12 | Audit Trail | ⏭️ | Deferred: module on audit rows is #1636 (open); operator actions on dead letters are #2013 |

---

## Prerequisites & Dependencies

- **#747 pull request 1** (`MessagingModuleId`, the normalizer): this plan's pull request 1 calls it; without it the helper cannot normalize the module. #747's own plan keeps the inbox out of scope and names #761 as its owner.
- **#2442 pull requests 1 and 2** (`IRequestContext.ModuleId` stamped by the dispatcher) before this plan's pull request 12; if the maintainer chooses the ambient source (D1 option B), pull request 12 reads `IModuleExecutionContext` instead and #352 pull request 1 is the prerequisite.
- **#760** (tenant on `InboxMessage`, open, no plan on `main`) edits the same row classes, scripts and key; whichever merges second rebases and extends `InboxMessageKey`. **#695, #714, #3440 and #2401** edit `IInboxStore`, its decorators or the inbox table; the second to merge adds its members and columns to the other's implementers.
- #1164 (persisted request context) is independent: the inbox row records the module of the request that created it, not a persisted context.

---

## Next Steps

1. The maintainer answers D1, D2, D3, D4 and D5 when work on #761 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Pull request 1 first (the helper); then the provider pull requests in the order of the split.
3. Open the issue file `artifacts/issues/module-scoped-inbox-retention.md` when this plan merges.
