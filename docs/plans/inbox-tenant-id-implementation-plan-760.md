# Implementation Plan: `TenantId` on `InboxMessage` — Tenant-Scoped Idempotency

> **Issue**: [#760](https://github.com/dlrivada/Encina/issues/760)
> **Type**: Feature (SPEC-002 REQ-061, AC-043, AC-036; delivered with #1164, tracked by #1186)
> **Complexity**: High (7 phases, 10 database providers, 15 pull requests, about 115 file touches)
> **Estimated Scope**: ~800 lines of production code + ~3,800 lines of tests, SQL scripts, manifests and documentation
> **Related plans** (cited by issue number, not by link): #761 (module on the same row and key, owner of the key helper), #737 (the outbox row, sibling of this plan), #1257 (the shared tenant-scope primitive and the default-tenant rule), #346 (the tenant gate and `[TenantAgnostic]`), #3440, #695, #714 (decorators and claims of the same store), #2676 (tenant telemetry helper)

---

## Summary

Make inbox idempotency per tenant: the same external message id delivered for two tenants is processed once by each, instead of tenant A's processing hiding it from tenant B. `IInboxMessage` gains a typed, indexed, nullable `TenantId`; the inbox key (the existing `MessageId` column) becomes tenant-qualified for a tenant row; the tenant is resolved fail-closed from the request that creates the row; all 10 database providers persist it; `IInboxStore` gains two strict tenant-filtered queries (the list and the expired rows of one tenant). Multi-tenancy is required in 1.0 for every capability SPEC-002 adds or changes (REQ-061, AC-043, scenario S11); #737, #739 and #760 are delivered with #1164 (SPEC-002 P-50).

**State of `origin/main` (`f484cdf4f`), verified by reading and grep (corrects the issue text).**

- The issue says "13 database providers". The matrix is 10 (`AGENTS.md` section 5): ADO.NET x3, Dapper x3, EF Core x3 (one `InboxStoreEF` serves the three databases) and MongoDB.
- **The inbox is keyed by `MessageId` alone**: `InboxMessages.MessageId` is the primary key (`NVARCHAR(255)` on SQL Server, see `src/Encina.ADO.SqlServer/Scripts/002_CreateInboxMessagesTable.sql`); `IInboxStore` has eight members, five addressed by `messageId` (`GetMessageAsync`, `MarkAsProcessedAsync`, `CacheHandlerErrorAsync`, `MarkAsFailedAsync`, `RemoveExpiredMessagesAsync(ids)`). The table has no tenant; the tenant lives only in the `Metadata` JSON of `InboxMetadata.TenantId` (`InboxOrchestrator.cs` line 407), which MongoDB's `InboxMessage` does not even store. `InboxPipelineBehavior` sets `TenantId = context.TenantId` (line 87) with no fail-closed rule, and `InboxOrchestrator` looks the message up by `messageId` only (lines 131 and 201 to 218).
- The bug of the same root is #3437 (open, `needs-decision`): tenant B receives tenant A's cached response for one key; its Dilemma D-8 is the key shape, which is Design Choice 1 of this plan. The EPIC #3452 collects it with #2401 (atomic claim), #3440 (in-progress outcome), #3441, #3438 and #3442 (replay, blocked by #3437). The issue's own Dilemma is recorded here, not skipped.
- Implementers (grep of class or record declarations over `src` and `tests`): `IInboxMessage` has 14 (8 provider classes, `FakeInboxMessage` in `Encina.Testing.Fakes`, `FakeInboxMessage` in `InboxMessageFaker.cs`, `ArbitraryInboxMessage`, two `BenchmarkInboxMessage`, `TestInboxMessage`); `IInboxStore` has 12 (8 provider stores, `InstrumentedInboxStore`, `FakeInboxStore`, `SabotagingInboxStore` in `InboxPipelineScenario.cs`, `StatefulInboxStore` in `InboxOrchestratorRetryTests.cs`); `IInboxMessageFactory` has 8 provider factories plus 8 test stubs; its interface does not change (the tenant reaches the row through `InboxMetadata`, which the factory already receives).
- #761 (merged plan, code open) puts the module into the key with the helper `InboxMessageKey.Create(moduleId, messageId)` (`module:<length>:<module>:<messageId>`, reserved prefix rejected, key at most 255 characters) and states that the tenant sibling adds "a tenant segment in the same helper with the same length-prefix rule". #3440, #695, #714 and #2401 identify a message by the store key, so a tenant-qualified key flows through them unchanged.
- `EventIdRanges.MessagingInbox = (2100, 2199)`, ids 2100-2112 are in use (verified by grep) and no plan of the registry claims 2113 or above.

**Not in scope**: the module on the inbox (#761); persisting and restoring the request context on dispatch (#1164); the atomic claim and the in-progress outcome (#2401, #3440); the distributed idempotency key (#3438); per-tenant retention (Design Choice 6); dead letter replay of inbox rows (#3442).

**Affected packages**: `Encina.Messaging`, `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.OpenTelemetry` (instrumented store), `Encina.Testing.Fakes`, `Encina.Testing.Bogus`, `Encina.Testing.FsCheck`.

**Provider category**: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (one store class), MongoDB.

---

## Design Choices

<details>
<summary><strong>1. The key of a tenant row (Dilemma D1, the Dilemma D-8 of #3437)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A tenant-qualified key in the existing `MessageId` column: `tenant:<length>:<tenant>:<messageId>` (the length is the decimal number of characters of the tenant, so the key parses uniquely), composed with #761's module segment as `tenant:<n>:<tenant>:module:<m>:<module>:<messageId>`; a row without a tenant keeps the plain id; a raw id that starts with `tenant:` or `module:` is rejected; the whole key must fit 255 characters. The typed `TenantId` column is stored too, for the filtered queries** | Injective by construction (two tenant and id pairs never give one key, and no host id can forge a prefix); no primary key change; all eight store members, the caches of #695, the locks of #714 and the claims of #2401 keep the key as their identity | A raw id of a tenant row is shorter: with a 128-character tenant and a module the key cannot fit 255 (a 128-character tenant segment alone is 140 characters), so an oversized combination is rejected with `inbox.message_id_too_long`; ids starting with `tenant:` are refused |
| **B) A composite primary key `(TenantId, MessageId)` with the five keyed store members taking a `tenantId`** | The relational design; per-tenant purge and queries are the primary index | Changes five members of `IInboxStore` on 12 implementers while #695, #714, #3440 and #2401 edit the same interface; a primary key column cannot be NULL, so the no-tenant row needs the reserved default tenant of #1257 (Design Choice 2 B) or a sentinel; the dead letter `SourceMessageId` limit (256) still needs a composed id |
| **C) The key stays global and the application prefixes it** | No library change | Weakest safety: relies on every application developer; fails AC-043 and AC-036 by design |

### Chosen Option: **A — a readable, injective, bounded key** (recommended, pending the maintainer)

### Rationale

- A wins when the store contract must not move and the key must be injective: it gives per-tenant idempotency by changing the key through the one helper (`InboxMessageKey.Create(tenantId, moduleId, messageId)`, owned by #761 and extended here, Design Choice 2) used by the orchestrator for every store call and for the dead letter `SourceMessageId`. B wins when queries by tenant and a per-tenant purge are the main use and the interface can be changed once, before 1.0; C wins only for single-tenant deployments (#3437's own wording). The answer of the maintainer to D-8 of #3437 is the answer here; this plan's pull requests 1 to 11 (the column, the filtered queries) are valid under A and B, while B changes the primary key scripts and the five keyed members and needs a re-split.
- Rejections use #761's codes: `inbox.reserved_message_id_prefix` and `inbox.message_id_too_long`, both ADR-062 category `Validation`; neither carries the id or the tenant.
- A tenant id is bounded at 128 characters (the column and #1257's rule); the combined-key limit is what actually bounds a tenant row's raw id, and the guide documents it.

</details>

<details>
<summary><strong>2. Who owns the key helper (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) #761 owns `InboxMessageKey` (its PR 1); this plan's PR 12b extends it with the tenant parameter, the `tenant:` prefix and its tests** | One helper and one injective rule; the merged #761 plan already says the tenant segment is added to the same helper | PR 12b waits for #761's PR 1, which waits for #747's `MessagingModuleId` |
| **B) This plan creates a tenant-only helper `InboxTenantKey` and #761 composes it** | No wait on the module chain | Two helpers for one key; #761's merged plan is rewritten |
| **C) The first of the two plans to merge creates the helper with both optional segments** | No ordering rule | The second plan's tests describe a signature it did not design |

### Chosen Option: **A — #761 owns the helper, this plan extends it** (recommended, pending the maintainer)

### Rationale

- A wins when #761 is implemented first (its plan is on `main`); C wins if the tenant PR is briefed before #761's. The provider PRs 1 to 11 do not depend on the helper, so only PR 12b is gated. The final shape is `InboxMessageKey.Create(string? tenantId, string? moduleId, string messageId)`, returning `Either<EncinaError, string>`.

</details>

<details>
<summary><strong>3. The tenant when tenancy is off, and what a single-tenant row looks like (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `null`: no tenant segment, plain keys, `TenantId` null** | The issue's own shape; the shipped keys keep working | A null row and a request with a tenant is a special case; null is distinct in SQL unique indexes |
| **B) The reserved default tenant of #1257 (Design Choice 2 B): the key carries `tenant:7:default:` whenever the resolved tenant is non-null, the single-tenant row included** | One uniform rule; turning tenancy on later keeps the old keys valid, so no message is processed twice because its key changed; AC-043 wording ("a fixed default tenant applies") | Every key is 14 characters longer; `default` stays on old rows |

### Chosen Option: **B — whatever #1257 decides, the key and the column follow `TenantScope.TenantId`** (recommended, pending the maintainer)

### Rationale

- B wins when #1257's platform rule is the reserved default; A wins if it stays null. The plan hard-codes neither: the pipeline behavior writes `scope.TenantId` into `InboxMetadata.TenantId` and the helper adds the prefix whenever that value is non-null. Under B, `default` is rejected as a real tenant by #1257's resolver, so the segment is injective with the real tenants; under A a single-tenant row has the plain key.
- Enabling multi-tenancy on a database written under A re-keys nothing: the old plain rows keep their keys and a redelivery of an old message after the switch carries a prefixed key, so it is processed once more; pre-1.0 there is no installed base (`AGENTS.md` section 1).

</details>

<details>
<summary><strong>4. A multi-tenant application receives an idempotent request with no tenant</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (compliance and security gates fail closed when context is missing; the only opt-out is explicit and logged), SPEC-002 DEC-006, AC-036 and AC-043; the explicit opt-out is the `[TenantAgnostic]` attribute that #346 (Design Choice 2, Settled) defines.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `InboxPipelineBehavior` resolves `ITenantScopeResolver.ResolveWrite(context.TenantId, allowTenantless: typeof(TRequest) has [TenantAgnostic])` of #1257 and returns the `Left` (`encina.tenancy.tenant_required`, ADR-062 category `InvalidRequest`) before the orchestrator runs; the behavior is a second layer behind the gate of #346 (stage `Authorization`, Order 5, earlier than the inbox's `Idempotency` stage)** | A forgotten tenant can only deny, never store an unowned row; webhooks of the platform itself stay possible | An external provider that calls with no tenant must opt out explicitly |
| **B) Store a null-tenant row with a plain key** | Never blocks | Fail open: tenant B's request collides with every other tenantless request |

### Chosen Option: **A — Fail closed, `[TenantAgnostic]` as the explicit opt-out**

### Rationale

- Only A complies. One unit-test row per line:

  | Tenancy on | Context tenant | Request `[TenantAgnostic]` | Result |
  |---|---|---|---|
  | yes | absent | no | `Left(encina.tenancy.tenant_required)`, the orchestrator is not called |
  | yes | T | no | key and column carry T |
  | yes | absent | yes | plain key, no-tenant row, logged once per type (EventId 2114) |
  | yes | T | yes | key and column carry T |
  | no | absent | any | the scope tenant of Design Choice 3 (null, or `default`) |
  | no | T | any | key and column carry T |

- A request that carries a tenant the ambient context does not (or the reverse) gets `encina.tenancy.tenant_mismatch` (`Forbidden`), logged by code (EventId 2115). No new error code is created by this plan.

</details>

<details>
<summary><strong>5. Where the typed column is written</strong></summary>

**Class:** Trivial
**Settled by:** the factory contract `IInboxMessageFactory.Create(messageId, requestType, receivedAtUtc, expiresAtUtc, InboxMetadata? metadata)` already carries the tenant in `InboxMetadata.TenantId` (`src/Encina.Messaging/Inbox/InboxOrchestrator.cs` lines 407 and 421 to 450), and the precedent of the dead letter tables (`TenantId VARCHAR(128) NULL`, `src/Encina.ADO.MySQL/Scripts/000_CreateAllTables.sql` line 173).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each provider factory copies `metadata?.TenantId` into the new `TenantId` property of its row class; the factory interface is unchanged; the `Metadata` JSON keeps its copy because it serializes the same `InboxMetadata` instance** | No interface change; both values come from one instance in one call; the 8 factories each gain one line | The JSON copy is redundant (and absent in MongoDB) |
| **B) A new `tenantId` parameter on `IInboxMessageFactory.Create`** | Explicit | Changes 8 factories and 8 test stubs for no information the metadata does not already carry |

### Chosen Option: **A — copy from the metadata in each factory**

### Rationale

- The column is the source for queries and keys; the JSON copy is never read for decisions. Column shape: `NVARCHAR(128) NULL` with binary collation `Latin1_General_100_BIN2` (SQL Server), `VARCHAR(128) NULL` (PostgreSQL), `VARCHAR(128) COLLATE utf8mb4_bin NULL` (MySQL), a string field in MongoDB, so two tenants that differ only by case never alias (the pattern of `DeadLetterMessageConfiguration`); an index `IX_InboxMessages_Tenant (TenantId, ReceivedAtUtc)`; no migration (`AGENTS.md` section 1), the two scripts of each package are edited in place.

</details>

<details>
<summary><strong>6. The tenant-filtered queries (Dilemma D4)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two strict members: `GetByTenantAsync(string tenantId, int skip, int take, CancellationToken)` (newest received first, `take` 1 to 1000) and `GetExpiredByTenantAsync(string tenantId, int batchSize, CancellationToken)`; a null tenant is `ArgumentNullException`, a blank or padded one `ArgumentException`; the existing `GetExpiredMessagesAsync` stays the system-scope read of every tenant** | Strict by shape; the list and the cleanup the issue asks for; the same style as #761's `GetByModuleAsync` | Two members on 12 implementers |
| **B) An optional `tenantId` parameter on `GetExpiredMessagesAsync`** | One member | Null would mean "all tenants" (fail open) |
| **C) One member only (`GetByTenantAsync`) and per-tenant cleanup through the existing `RemoveExpiredMessagesAsync(ids)`** | Smaller interface | The expiry rule (processed and past `ExpiresAtUtc`) would be re-implemented by every caller |

### Chosen Option: **A — two strict members** (recommended, pending the maintainer)

### Rationale

- A wins when strictness must be structural (SPEC-002 AC-043); C wins if per-tenant cleanup is not needed in 1.0, in which case `GetExpiredByTenantAsync` is dropped. The cleanup processor keeps using `GetExpiredMessagesAsync` (retention is time-based, not per tenant); per-tenant retention is the follow-up issue file `artifacts/issues/inbox-tenant-retention.md`.

</details>

<details>
<summary><strong>7. Build order of the pull request split (Dilemma D5)</strong></summary>

**Class:** Dilemma

The change touches an interface with many implementers (the 14 `IInboxMessage` and 12 `IInboxStore` implementers of the Summary). A pull request that changes an interface before its implementers does not compile (`AGENTS.md` section 8), and a pull request is one package or provider, at most 15 files and 1,500 lines counting tests and manifests (section 11).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Implementers first, interface last: each provider pull request adds the `TenantId` property, the factory copy and the two query members to its concrete classes (they compile against the old interfaces) with its SQL and tests; the interface and decorator pull request comes after every implementer has the members; the behavior and orchestrator pull request is last** | Every pull request builds and carries its own tests; no scaffold merges | The members exist on classes before the interfaces have them; 15 pull requests |
| **B) One pull request** | One review | About 115 file touches: over the limits (needs `size-exception-approved`) |
| **C) Default interface members as a scaffold** | The interface goes first | A compatibility layer merges (`AGENTS.md` section 1) with untested defaults |

### Chosen Option: **A — implementers first, interface last** (recommended, pending the maintainer)

### Rationale

- A wins because the limits are hard gates and the order is the one #738, #737 and #761 follow; the plans of #695, #714, #3440 and #2401 add members or decorators to `IInboxStore` too, so whichever interface-changing pull request merges second adds its member to the other's implementers (the list of 12 is re-grepped when work starts). B wins with the `size-exception-approved` label; C is rejected only by the rule against compatibility layers.

</details>

---

## Implementation Phases

### Phase 1: ADO.NET and Dapper Providers (6)

> **Goal**: The column, the row class, the factory copy and the two queries in the six packages, one pull request per package (PRs 1 to 6).

<details>
<summary>Tasks</summary>

Per package (`Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`):

1. SQL scripts: add `TenantId` and `IX_InboxMessages_Tenant (TenantId, ReceivedAtUtc)` in `Scripts/002_CreateInboxMessagesTable.sql` and `Scripts/000_CreateAllTables.sql` (Design Choice 5; the existing `MessageId` key and expiry index stay).
2. `Inbox/InboxMessage.cs`: `public string? TenantId { get; set; }`; `Inbox/InboxMessageFactory.cs`: `TenantId = metadata?.TenantId` (an empty id is no tenant: `string.IsNullOrEmpty` maps to null, as `InboxOrchestrator` line 277 does for the dead letter).
3. `Inbox/InboxStoreADO.cs` or `InboxStoreDapper.cs`: `TenantId` in the `INSERT`, and the public methods `GetByTenantAsync` and `GetExpiredByTenantAsync` with the strict equality `TenantId = @TenantId` and the provider paging syntax (SQL Server `OFFSET ... FETCH` or `TOP (@n)`, PostgreSQL and MySQL `LIMIT @n OFFSET @skip`); they are concrete-class members until PR 12a adds them to `IInboxStore`. Database calls stay asynchronous with a `CancellationToken`.
4. The package's `PublicAPI.Unshipped.txt`, its manifest entries, a store-level integration test (`[Collection("ADO-<Db>")]` or `[Collection("Dapper-<Db>")]`: tenant round trip, strict queries isolate two tenants, a tenant differing only by case is another tenant), the guard edit (null tenant, blank tenant, non-positive `take` or `batchSize`) and the factory unit test edit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Issue #760; plan docs/plans/inbox-tenant-id-implementation-plan-760.md. Dilemmas D1-D5 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- The interfaces are NOT changed yet (D5): this pull request adds TenantId and the two query members to ONE package's concrete classes so that it builds against the current interfaces.

TASK:
Apply the Phase 1 tasks to the one package of this pull request; the six implementations behave identically and differ only in SQL dialect.

KEY RULES:
- Strict predicate: a null or blank tenant never matches every row.
- SQL Server: @param, TOP/OFFSET, NVARCHAR with BIN2 collation; PostgreSQL: LIMIT/OFFSET, case-sensitive identifiers; MySQL: LIMIT/OFFSET, backticks, utf8mb4_bin.
- Asynchronous database calls with a CancellationToken; never IDbConnection.Open().
- Do not change the MessageId key or add migrations; edit both scripts of the package.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Inbox/InboxStoreADO.cs
- src/Encina.Dapper.PostgreSQL/Inbox/InboxStoreDapper.cs
- src/Encina.ADO.SqlServer/Scripts/002_CreateInboxMessagesTable.sql
- src/Encina.ADO.MySQL/Scripts/000_CreateAllTables.sql (dead letter TenantId, line 173)
```

</details>

---

### Phase 2: EF Core and MongoDB Providers

> **Goal**: The same behavior in `InboxStoreEF` (three databases, PR 7) and `InboxStoreMongoDB` (PR 8).

<details>
<summary>Tasks</summary>

1. `src/Encina.EntityFrameworkCore/Inbox/InboxMessage.cs`: `TenantId`; `InboxMessageConfiguration.cs`: `HasMaxLength(128)`, the binary collation passed explicitly as `DeadLetterMessageConfiguration` does, `HasIndex(x => new { x.TenantId, x.ReceivedAtUtc })` named `IX_InboxMessages_Tenant`; `InboxMessageFactory.cs` copies the metadata tenant; `InboxStoreEF.cs`: the two members as LINQ predicates `m.TenantId == tenantId`. No EF migration. Integration tests: one class per database, the guard edit and the factory test edit.
2. `src/Encina.MongoDB/Inbox/InboxMessage.cs` (the class has no `Metadata` field today, so the column is the only tenant), `InboxMessageFactory.cs`, `InboxStoreMongoDB.cs`: the filter and the two members; `MongoDbIndexCreator` gets the compound index guarded by `UseInbox`; one integration test class, the guard edit, PublicAPI and manifest entries.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Phase 1 pull requests are merged (issue #760); the interfaces are still unchanged (D5).

TASK:
Implement the Phase 2 tasks, one pull request per package (Encina.EntityFrameworkCore, then Encina.MongoDB).

KEY RULES:
- One InboxStoreEF behavior for SQL Server, PostgreSQL and MySQL, proven by an integration test class on each database.
- The binary collation is passed explicitly per database, null on PostgreSQL, as DeadLetterMessageConfiguration requires.
- MongoDB index creation goes through MongoDbIndexCreator, guarded by the UseInbox option.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Inbox/InboxMessageConfiguration.cs
- src/Encina.EntityFrameworkCore/DeadLetter/DeadLetterMessageConfiguration.cs
- src/Encina.EntityFrameworkCore/Inbox/InboxStoreEF.cs
- src/Encina.MongoDB/Inbox/InboxStoreMongoDB.cs
- src/Encina.MongoDB/MongoDbIndexCreator.cs
```

</details>

---

### Phase 3: Fakes, Bogus, FsCheck and Test Doubles

> **Goal**: The test infrastructure follows the interface; every pull request still builds against the old one.

<details>
<summary>Tasks</summary>

1. PR 9, `src/Encina.Testing.Fakes/Models/FakeInboxMessage.cs` and `Stores/FakeInboxStore.cs`: `TenantId` and the two query members with the strict predicate, plus `PublicAPI.Unshipped.txt`, manifest, unit tests and the guard edit.
2. PR 10, `src/Encina.Testing.Bogus/InboxMessageFaker.cs` and `ArbitraryInboxMessage` in `src/Encina.Testing.FsCheck/ArbitraryMessages.cs`, with the Bogus `PublicAPI.Unshipped.txt` and both manifests.
3. PR 11 (test doubles only): the two `BenchmarkInboxMessage` classes (`tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks|Encina.Dapper.Benchmarks/Infrastructure/BenchmarkEntityFactory.cs`), `TestInboxMessage` (`tests/Encina.UnitTests/Messaging/Inbox/InboxOrchestratorTests.cs`) get the property; `SabotagingInboxStore` (`tests/Encina.IntegrationTests/Messaging/InboxPipelineScenario.cs`) and `StatefulInboxStore` (`tests/Encina.UnitTests/Messaging/Inbox/InboxOrchestratorRetryTests.cs`) get the two members.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Issue #760. The interfaces are NOT changed yet: PRs 9 to 11 add the members to the classes only.

TASK:
Update FakeInboxMessage, FakeInboxStore (PR 9), InboxMessageFaker, ArbitraryInboxMessage (PR 10) and the five test doubles (PR 11).

KEY RULES:
- The fake store behaves exactly like the real stores for the tenant rules (the contract test of PR 12a runs against it too).
- Do not touch the interfaces and do not call the new members through an IInboxStore field (that is the decorator, PR 12a).

REFERENCE FILES:
- src/Encina.Testing.Fakes/Stores/FakeInboxStore.cs
- src/Encina.Testing.Bogus/InboxMessageFaker.cs
- src/Encina.Testing.FsCheck/ArbitraryMessages.cs
```

</details>

---

### Phase 4: Interface, Decorator, Behavior and Orchestrator

> **Goal**: The tenant in the contract, the fail-closed rule and the tenant-qualified key.

<details>
<summary>Tasks</summary>

1. PR 12a, `IInboxMessage`: `string? TenantId { get; set; }`; `IInboxStore`: `GetByTenantAsync` and `GetExpiredByTenantAsync` (Design Choice 6); `src/Encina.OpenTelemetry/MessagingStores/InstrumentedInboxStore.cs` delegates them to `_inner`; PublicAPI of `Encina.Messaging` and `Encina.OpenTelemetry`, both manifests, the decorator test edit and the contract test `InboxStoreTenantContractTests` (run against `FakeInboxStore` and the provider stores).
2. PR 12b, `InboxPipelineBehavior`: the optional constructor parameter `ITenantScopeResolver? tenantScopeResolver = null` after `orchestrator` (optional parameters stay last, CS1737); in `ProcessIdempotentAsync` it resolves the write scope (matrix of Design Choice 4; the `[TenantAgnostic]` lookup is cached per closed generic type in a static field), returns the `Left` before the orchestrator, and sets `InboxMetadata.TenantId = scope.TenantId` instead of the raw `context.TenantId`. `InboxOrchestrator` builds every store key and the dead letter `SourceMessageId` with `InboxMessageKey.Create(metadata.TenantId, metadata.ModuleId, messageId)` (the helper of #761, extended with the tenant parameter and the `tenant:` reserved prefix; its `Left` is returned as is). `GetMessageAsync`, `MarkAsProcessedAsync`, `CacheHandlerErrorAsync`, `MarkAsFailedAsync` and `RemoveExpiredMessagesAsync` receive the composed key unchanged.
3. `InboxTenantLog` (new, `src/Encina.Messaging/Diagnostics/InboxTenantLog.cs`, `[LoggerMessage]` source generator), EventIds packed from 2113 inside the registered `MessagingInbox` range (2100-2199; 2100-2112 in use; verified free on `main`): 2113 write denied, no tenant (Warning, error code and request type only), 2114 tenant-agnostic request stored without a tenant (Information, once per type), 2115 tenant mismatch denied (Warning, error code only). The architecture test already maps `Encina.Messaging` to `MessagingInbox`.
4. Tests of PR 12b: the matrix of Design Choice 4, "tenant A and tenant B send the same idempotency key: the handler runs twice and each gets its own response" (the reproduction of #3437), the injectivity test (tenants `a:b` and `a` with ids `c` and `b:c` give four distinct keys), guard edits for the behavior constructor (a null resolver is valid: a test of the valid result), the DI test with `ValidateOnBuild` and `ValidateScopes`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
- Issue #760; plan docs/plans/inbox-tenant-id-implementation-plan-760.md. All implementers already have TenantId and the two query members (PRs 1 to 11). #1257's ITenantScopeResolver, #346's [TenantAgnostic] and #761's InboxMessageKey are merged.

TASK:
PR 12a: add the members to the interfaces and the decorator. PR 12b: resolve the tenant fail-closed in InboxPipelineBehavior and compose the tenant-qualified key in InboxOrchestrator.

KEY RULES:
- Fail closed: a multi-tenant application with no tenant returns Left before the orchestrator; [TenantAgnostic] is the only opt-out and is logged once per type.
- The key is injective: length-prefixed tenant segment, reserved prefixes rejected; the helper returns Either.
- Do not change IInboxMessageFactory; the factories copy metadata.TenantId.
- EncinaError.Message is never logged; EventIds 2113-2115 only, packed.
- Update PublicAPI.Unshipped.txt (remove the old behavior constructor line, add the new one); zero warnings.

REFERENCE FILES:
- src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs
- src/Encina.Messaging/Inbox/InboxOrchestrator.cs
- src/Encina.Messaging/Inbox/IInboxStore.cs
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedInboxStore.cs
```

</details>

---

### Phase 5: Cross-Cutting Integration and Observability

> **Goal**: The functions marked Include in the matrix.

<details>
<summary>Tasks</summary>

1. OpenTelemetry (in PR 13, after #2676's helper merges): `InstrumentedInboxStore` and the inbox activity set the attribute `encina.tenant_id` only when multi-tenancy is on (ADR-054 D4); the cardinality risk is documented in the observability guide (#906). No tenant value goes into a log: the three messages of Phase 4 carry error codes and the request type.
2. Structured logging: `InboxTenantLog` of Phase 4.
3. Idempotency and multi-tenancy: the feature itself; the two-tenant test of AC-036 and AC-043 is Phase 6 PR 13.
4. Deferred: module (#761), claim and in-progress outcome (#2401, #3440), restore on dispatch (#1164).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
- Issue #760; cross-cutting matrix of docs/plans/inbox-tenant-id-implementation-plan-760.md.

TASK:
Wire the tenant attribute on the inbox spans through the #2676 helper and check that every function marked Include is wired and the deferred ones stay out of the diff.

KEY RULES:
- ADR-054 D4: attribute encina.tenant_id only when multi-tenancy is on; no baggage.
- No tenant id and no EncinaError.Message in any log.

REFERENCE FILES:
- AGENTS.md section 6
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedInboxStore.cs
```

</details>

---

### Phase 6: Testing

> **Goal**: Per-flag coverage at the manifest targets, including real databases for all 10 providers.

<details>
<summary>Tasks</summary>

Tests travel with the pull request that adds the code they cover; none waits for the end.

1. **Unit** (PRs 9, 12a, 12b): the matrix of Design Choice 4, key composition, the two-tenants-one-key scenario, the fake store, the decorator.
2. **Guard** (PRs 1 to 9, 12b): the new public methods and the new constructor parameter (`GetByTenantAsync`, `GetExpiredByTenantAsync`: null tenant, blank tenant, bad `skip`, `take`, `batchSize`; `InboxPipelineBehavior`: a null resolver is valid, so a test of the valid result).
3. **Contract** (PR 12a): `InboxStoreTenantContractTests` against `FakeInboxStore` and the provider stores.
4. **Property (FsCheck)** (PR 12b): the key is injective for any tenant, module and id (generated with `:` in every segment), and no query of tenant A returns a row of B.
5. **Integration** (PRs 1 to 8, then PR 13): store-level tests in each provider pull request; the orchestrator-level two-tenant test (AC-036, AC-043, the bug of #3437) on all 10 providers through the shared collections in PR 13, `ClearAllDataAsync` in `InitializeAsync`.
6. **Load** (PR 13): concurrent requests with one idempotency key for two tenants (the concurrent claim itself is #2401's).
7. **Benchmark**: justification `tests/Encina.BenchmarkTests/Inbox/InboxTenant.md` (key composition is a string operation off the hot path), in PR 13.
8. **Per-file targets** (in each package manifest, one-sentence justifications written there; measured with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and `.github/scripts/coverage-report.cs`). Existing targets of touched files are held, never lowered:

   | File group | Unit | Guard | Contract | Property | Integration | Justification |
   |---|---|---|---|---|---|---|
   | `InboxOrchestrator.cs` | 95 | 15 | 0 | 60 | 75 | Unit, guard and integration are the current manifest targets and are held; the property flag proves key injectivity and isolation; contract has no reachable line |
   | `InboxPipelineBehavior.cs` | 95 | 5 | 5 | 60 | 85 | Held at the current targets; every row of the matrix runs with a fake orchestrator; property covers the tenant pair isolation |
   | `InboxTenantLog.cs` (new) | 90 | 0 | 0 | 0 | 0 | Generated logging methods exercised with a fake logger; guard 0 because the generated partial has no argument checks to reach |
   | `IInboxMessage.cs`, `IInboxStore.cs` | 0 | 0 | 0 | 0 | 0 | Interfaces without executable code (`defaultTests: []`) |
   | Provider stores and factories (ADO x3, Dapper x3, EF Core, MongoDB) | 0 | 10 | 80 | 0 | 90 | The SQL, LINQ and Mongo filters are provable only on real databases (`AGENTS.md` section 5); guard reaches the argument checks, contract the shared behavior |
   | `FakeInboxStore`, `FakeInboxMessage`, `InboxMessageFaker`, `ArbitraryInboxMessage` | 90 | 90 | 80 | 0 | 0 | Test doubles that must behave like the real stores for the tenant rules |
   | `InstrumentedInboxStore` | 90 | 90 | 0 | 0 | 0 | Delegation plus one attribute, fully reachable through a listener test |

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```
CONTEXT:
- Issue #760. Tests travel with the pull request that adds the code they cover; the orchestrator-level two-tenant test is PR 13. AGENTS.md section 9 per-flag coverage model; guard tests for every new public method and constructor parameter (#3798).

TASK:
Write the tests of Phase 6 Tasks 1-7 and fill the manifest targets.

KEY RULES:
- Integration tests on real databases for all 10 providers, never a .md; shared [Collection] fixtures; Shouldly via Encina.Testing.Shouldly.
- Tests execute real code; a test never swallows an exception; no Thread.Sleep.
- A nullable parameter that accepts null gets a test of the valid result in GuardTests.

REFERENCE FILES:
- tests/Encina.UnitTests/Messaging/Inbox/InboxOrchestratorTests.cs
- tests/Encina.GuardTests/Messaging/Inbox/InboxGuardTests.cs
- docs/testing/integration-tests.md
- .github/coverage-manifest/Encina.Messaging.json
```

</details>

---

### Phase 7: Documentation & Finalization

<details>
<summary>Tasks</summary>

1. XML docs on every new public member; `PublicAPI.Unshipped.txt` per touched package.
2. `changelog.d/760-inbox-tenant-id.added.md`; the inbox section of the messaging guide (key shape, the combined-key length limit, the reserved prefixes; through `docs-writer`); an ADR only if a Dilemma is decided architecturally (ADR number assigned when the ADR pull request opens).
3. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```
CONTEXT:
- Issue #760 is implemented and tested.

TASK:
Write the documentation set of Phase 7 Tasks through the specialists (docs-writer for pages, mechanical-fixer for the changelog fragment and PublicAPI lines).

KEY RULES:
- Coverage figures are cited with covref markers, never typed.
- Public text never names a private client application.

REFERENCE FILES:
- changelog.d/README.md
- src/Encina.Messaging/README.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

Order rule (D5 option A): implementers first, interface last; every pull request builds, carries its own tests, `PublicAPI.Unshipped.txt` and manifest entries. Counts include tests and manifests; each ADO.NET or Dapper row is the row class, the factory, the store, two scripts, PublicAPI, manifest, one store-level integration test, one guard edit and one factory test edit.

| PR | Content | Approx. files | Approx. lines |
|----|---------|---------------|---------------|
| 1 | `Encina.ADO.SqlServer`: the provider set above | 10 | 480 |
| 2 | `Encina.ADO.PostgreSQL`: same set | 10 | 480 |
| 3 | `Encina.ADO.MySQL`: same set | 10 | 480 |
| 4 | `Encina.Dapper.SqlServer`: same set | 10 | 480 |
| 5 | `Encina.Dapper.PostgreSQL`: same set | 10 | 480 |
| 6 | `Encina.Dapper.MySQL`: same set | 10 | 480 |
| 7 | `Encina.EntityFrameworkCore`: row, configuration, factory, store, PublicAPI, manifest, 3 integration classes, guard edit, factory test edit | 11 | 620 |
| 8 | `Encina.MongoDB`: row, factory, store, index creator, PublicAPI, manifest, integration test, guard edit, factory test edit | 9 | 450 |
| 9 | `Encina.Testing.Fakes`: `FakeInboxMessage`, `FakeInboxStore`, PublicAPI, manifest, unit test, guard edit | 6 | 340 |
| 10 | `Encina.Testing.Bogus` and `Encina.Testing.FsCheck`: faker, arbitrary, Bogus PublicAPI, 2 manifests | 5 | 200 |
| 11 | Test doubles only: 2 benchmark states, `TestInboxMessage`, `SabotagingInboxStore`, `StatefulInboxStore` | 5 | 160 |
| 12a | `IInboxMessage`, `IInboxStore`, `InstrumentedInboxStore`, PublicAPI x2, manifests x2, decorator test edit, contract test | 9 | 540 |
| 12b | `InboxPipelineBehavior`, `InboxOrchestrator`, `InboxMessageKey` (extended), `InboxTenantLog`, PublicAPI, manifest, unit tests (matrix, key, two tenants), property test, DI test, guard edit | 12 | 1,000 |
| 13 | Orchestrator two-tenant integration test on 10 providers (4 files holding 10 classes), load test, benchmark justification, tenant attribute on the instrumented store (needs #2676) | 8 | 1,000 |
| 14 | Documentation: changelog fragment, messaging guide section, inventory line | 3 | 200 |

A pull request that measures over 15 files or 1,500 lines when it is briefed is split again. PRs 1 to 11 may merge in any order; 12a needs 1 to 11; 12b needs 12a, #1257's PR A1, #346's PR A and #761's `InboxMessageKey`; PR 13 needs 12b and #2676's helper.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| SPEC-002 REQ-061, AC-043, AC-036, scenario S11 | Tenant-aware capabilities, a two-tenant isolation test, webhook idempotency per tenant | Key, column, tests |
| SPEC-002 DEC-006 | Gates fail closed | Design Choice 4 |
| IETF idempotency-key draft | A key is scoped to the client; a reused key with another payload is an error | Context of #3437 (payload fingerprint is #3441's and #3437's alternative, not built here) |
| ADR-054 D4 | Tenant as attribute and tag only when multi-tenancy is on | Phase 5 |
| ADR-062 | Error category contract | `tenant_required` (`InvalidRequest`), `tenant_mismatch` (`Forbidden`), the key codes (`Validation`) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `InboxPipelineBehavior`, `InboxOrchestrator`, `InboxMetadata` | `src/Encina.Messaging/Inbox/` | Resolution point, key composition, tenant carrier |
| `IInboxMessageFactory` and 8 provider factories | `src/Encina.*/Inbox/InboxMessageFactory.cs` | Copy of `metadata.TenantId` into the column |
| `ITenantScopeResolver`, `TenantScope` | #1257 PR A1 (core) | Fail-closed tenant resolution |
| `[TenantAgnostic]` | #346 PR A (core) | The explicit opt-out |
| `InboxMessageKey` | #761 PR 1 | The one injective key helper |
| `DeadLetterMessageConfiguration` binary collation | `src/Encina.EntityFrameworkCore/DeadLetter/` | Collation pattern |
| `MongoDbIndexCreator` | `src/Encina.MongoDB/MongoDbIndexCreator.cs` | Compound index |
| `InstrumentedInboxStore`, `FakeInboxStore` | `src/Encina.OpenTelemetry/MessagingStores/`, `src/Encina.Testing.Fakes/Stores/` | Decorator and double |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (inbox) | `MessagingInbox` 2100-2199 | Existing range; 2100-2112 in use; **this plan 2113-2115** (`InboxTenantLog`); verified free on `origin/main` and absent from the plan-batch registry; #761 uses 7700-7702 in its own range; no new range |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Messaging core | 6 | Two interfaces, behavior, orchestrator, key helper, log file |
| ADO.NET x3 and Dapper x3 | 42 | Row, factory, store, 2 scripts, PublicAPI, manifest per package |
| EF Core and MongoDB | 8 | Rows, configuration, factories, stores, index creator |
| Fakes, faker, arbitrary, decorator, test doubles | 14 | With PublicAPI and manifests |
| Tests | 38 | Store-level and orchestrator-level integration (20 classes), unit, contract, property, guard, DI, load |
| Documentation | 3 | Fragment, guide, inventory |
| **Total** | **about 115 across 15 pull requests** | The sum of the split rows is the authority |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers, no migrations.
- Issue #760: TenantId on the inbox row and a tenant-qualified idempotency key for all 10 database providers (SPEC-002 REQ-061, AC-043, AC-036). Plan: docs/plans/inbox-tenant-id-implementation-plan-760.md; Dilemmas D1-D5 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (the order of the 15 pull requests; every one builds):
1. ADO.NET and Dapper packages, one per pull request (Phase 1); EF Core and MongoDB (Phase 2).
2. Fakes, faker, arbitrary, test doubles (Phase 3).
3. Interfaces and decorator (PR 12a), then the fail-closed behavior and the tenant-qualified key (PR 12b) (Phase 4).
4. Observability, orchestrator-level tests on real databases, documentation (Phases 5-7).

KEY PATTERNS:
- Tenant from ITenantScopeResolver.ResolveWrite (#1257) in InboxPipelineBehavior; fail closed, [TenantAgnostic] the only logged opt-out.
- Key: InboxMessageKey.Create(tenantId, moduleId, messageId): length-prefixed segments, reserved prefixes rejected, at most 255 characters.
- Strict GetByTenantAsync and GetExpiredByTenantAsync; a null or blank tenant never matches every row.
- Dialects: SQL Server NVARCHAR BIN2; PostgreSQL and MySQL LIMIT; MySQL utf8mb4_bin.
- Async database calls with CancellationToken; no EncinaError.Message in logs; EventIds 2113-2115.
- Tests: Shouldly, shared [Collection] fixtures, per-flag coverage from the manifests, guard tests for new public API.

REFERENCE FILES:
- src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs
- src/Encina.Messaging/Inbox/InboxOrchestrator.cs
- src/Encina.EntityFrameworkCore/Inbox/InboxStoreEF.cs
- src/Encina.MongoDB/Inbox/InboxStoreMongoDB.cs
- docs/plans/inbox-module-id-implementation-plan-761.md (the sibling; read on main)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: the inbox needs read-your-writes; #695's cache keys on the store key, so a tenant-qualified key is a different entry |
| 2 | OpenTelemetry | ✅ | Phase 5: attribute `encina.tenant_id` only when multi-tenancy is on (ADR-054 D4), through #2676's helper; no baggage |
| 3 | Structured Logging | ✅ | Phase 4: `InboxTenantLog`, EventIds 2113-2115 in `MessagingInbox`; error codes and request type only |
| 4 | Health Checks | ❌ | Not applicable: no new dependency; the inbox health check is unchanged |
| 5 | Validation | ✅ | Phase 4: the key helper validates the reserved prefixes and the length; the resolver bounds the tenant (128 characters, blank is no tenant, padded rejected) |
| 6 | Resilience | ❌ | Not applicable: no external call is added; a rejected key fails the request and is not retried |
| 7 | Distributed Locks | ❌ | Not applicable: #714 hashes the key for its lock name, so a tenant-qualified key serializes per tenant; claiming is #2401's |
| 8 | Transactions | ❌ | Not applicable: the column is written with the row by the same store call; commit ownership stays with the transaction behaviors (ADR-048) and the orchestrator never calls `SaveChangesAsync` for the caller |
| 9 | Idempotency | ✅ | This is the feature: per-tenant deduplication through the tenant-qualified key |
| 10 | Multi-Tenancy | ✅ | The column, the key, the fail-closed rule and the strict queries |
| 11 | Module Isolation | ⏭️ | Deferred: the module on the inbox row and key is #761 (open, plan on main); the key helper is shared |
| 12 | Audit Trail | ⏭️ | Deferred: tenant on audited messaging operations has no inbox audit store; operator actions on dead letters are #2013 (open) |

---

## Prerequisites & Dependencies

- #1257 PR A1 (`ITenantScopeResolver`, the default-tenant rule) and #346 PR A (`[TenantAgnostic]`) before PR 12b; #761's `InboxMessageKey` before PR 12b; #2676's helper before PR 13.
- The answer of the maintainer to D-8 of #3437 decides Design Choice 1; the answer of #1257 decides Design Choice 3.
- Coordinate the interface with #695, #714, #3440 and #2401 (Design Choice 7) and the scripts with #761.

---

## Next Steps

1. The maintainer answers D1 to D5 when work on #760 starts (D1 together with D-8 of #3437); the answers go into `## Maintainer Decisions` and the markers are removed.
2. Brief pull request 1 first; the provider pull requests follow in order, then 12a and 12b.
