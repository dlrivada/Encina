# Implementation Plan: `ModuleId` on the Messaging Entities (Outbox, Saga, Scheduled Message, Dead Letter)

> **Issue**: [#747](https://github.com/dlrivada/Encina/issues/747)
> **Type**: Feature
> **Complexity**: High (10 phases, 10 database providers, about 130 files in 27 pull requests)
> **Estimated Scope**: ~1,100 lines of production code + ~3,000 lines of tests and scripts
> **Family**: modular monolith (#352 owns the module context producer, #360 the module graph, #358 the module event bus); this plan owns the module dimension of the messaging rows

---

## Summary

Add the module to the four messaging rows: `IOutboxMessage.ModuleId`, `ISagaState.ModuleId`, `IScheduledMessage.ModuleId` and `IDeadLetterMessage.ModuleId`, filled when the row is created from the ambient module, persisted by all 10 database providers, readable through a module-filtered query per store, and restored as the ambient module when a stored row is dispatched again (outbox delivery, scheduled delivery, saga timeout and compensation, dead letter replay is #2013's, see Design Choice 6).

**State of `origin/main` (`db1e7f688`), verified by reading and grep (corrects the issue text):**

- There is no `IModuleContext`. The ambient module is `IModuleExecutionContext.CurrentModule` (`src/Encina/Modules/Isolation/IModuleExecutionContext.cs`, an `AsyncLocal` implementation in `ModuleExecutionContext.cs`). It is set only by `ModuleExecutionContextBehavior` in `Encina.EntityFrameworkCore` and only with `UseModuleIsolation`; ADO.NET, Dapper and MongoDB register the context and nothing sets it. #352 (pull request 1) moves the producer to core and runs it whenever `AddEncinaModules` is used; until it merges the stamped value is `null` outside EF Core with module isolation, which this plan handles as "no module" and tests with an explicit scope (`IModuleExecutionContext.CreateScope`).
- The issue says "13 database providers". The matrix is 10 (`AGENTS.md` section 5): ADO.NET x3, Dapper x3, EF Core x3 (one `Encina.EntityFrameworkCore` package serves SQL Server, PostgreSQL and MySQL), MongoDB. SQLite and Oracle are out of the matrix.
- The entities are interfaces with one class per provider: `IOutboxMessage` (15 implementers: 8 provider classes, `FakeOutboxMessage`, `OutboxMessageFaker`, `ArbitraryMessages` generators, 2 benchmark factories, a property test double, a unit test double), `ISagaState` (14), `IScheduledMessage` (13) and `IDeadLetterMessage` (10, grep of `class|record ... : IXxx` over `src` and `tests`). The stores are `IOutboxStore` (10 implementers incl. `InstrumentedOutboxStore` and `FakeOutboxStore`), `ISagaStore` (12, incl. two test doubles), `IScheduledMessageStore` (10) and `IDeadLetterStore` (11, incl. one test double).
- Dead letters: #583 (closed) deferred the module to this issue (plan #583, B6; comment of 2026-10-09 on #747: "include `DeadLetterMessages` on the 10 providers"). `IDeadLetterMessage` already carries `TenantId`; rows are built from `DeadLetterData` (`src/Encina.Messaging/DeadLetter/IDeadLetterMessageFactory.cs`) by `DeadLetterOrchestrator.NewData` (line 236).
- Rows are created in `OutboxOrchestrator.AddAsync` (`_messageFactory.Create(...)`, line 103), `SchedulerOrchestrator.ScheduleAsync` / `ScheduleRecurringAsync` (lines 136 and 223), `SagaOrchestrator.StartAsync` (`_stateFactory.Create(...)`, line 123) and `DeadLetterOrchestrator.NewData`. Rows are dispatched again by `OutboxBatchProcessor` (publish callback, line 92), `ScheduledMessageProcessor` and the saga timeout and stuck paths.
- The sibling row dimensions are open and edit the same interfaces, scripts and stores: tenant on the saga is #738 (planned), tenant on outbox and scheduled rows is #737 and #739; audit rows get the module in #1636 and cache keys in #746. This plan follows their conventions (nullable column, in-place scripts, case rule of #1636).

**Not in scope**: `InboxMessage` (Design Choice 7); per-module processors and per-module processing policies (follow-up issue file `artifacts/issues/module-scoped-messaging-processors.md`); enforcing that a handler may only read its own module's rows (#352's boundary rule and #359's data isolation); dead letter replay context (#2013); module attribution of audit rows (#1636).

**Affected packages**: `Encina.Messaging`, `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.OpenTelemetry` (instrumented stores, meter registration), `Encina.Testing.Fakes`, `Encina.Testing.Bogus`, `Encina.Testing.FsCheck`.

**Provider category**: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (one store class per entity serves the three databases), MongoDB.

---

## Design Choices

<details>
<summary><strong>1. Where the module comes from when a row is created (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The ambient `IModuleExecutionContext.CurrentModule`, taken through an optional constructor parameter of each orchestrator (null when the module system is not registered)** | A caller cannot declare itself another module; works for rows written from nested `Send` calls; same source as #746 and #352 | Code on a thread that lost the `AsyncLocal` writes no module |
| **B) `IRequestContext.GetModuleName()` (the `Encina.ModuleName` metadata key)** | Travels with the request | Any code can set it with `WithModuleName` and nothing in the framework sets it, so a row could name another module (#352 reached the same finding) |
| **C) An explicit `moduleId` argument on every orchestrator method** | No ambient state | Breaks every caller and the issue's "auto-populated" criterion |

### Chosen Option: **A — the ambient `IModuleExecutionContext`** (recommended, pending the maintainer)

### Rationale

A wins when the stored module must be the module that really ran the code, which is what module filtering and context restoration rely on. B wins for hosts that already set the metadata key themselves and run outside the module pipeline. C wins when a row must be written on behalf of another module on purpose. Consistent with #746 (D1) and #352 (D3), which recommend the same source.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. How the module reaches the row (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The orchestrator sets `row.ModuleId` through the interface setter after the factory returns; the three row factory interfaces and `DeadLetterData` creation stay unchanged** | No change to `IOutboxMessageFactory`, `ISagaStateFactory`, `IScheduledMessageFactory` or their 24 implementations and test factories | The row is briefly created without a module |
| **B) A `moduleId` parameter on each factory `Create`** | One step | Changes three factory interfaces, eight provider factories each, fakes and every unit test that calls `Create` |

### Chosen Option: **A — stamp after `Create`** (recommended, pending the maintainer)

### Rationale

A wins when the change must stay small and testable in the orchestrators alone (#738 stamps the tenant the same way). B wins if the maintainer wants the factory to be the only place that builds a complete row. The dead letter carrier `DeadLetterData` gains an optional trailing `ModuleId` parameter instead, because dead letter rows are built from that record and not by a `Create(...)` with scalar parameters.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. The stored value and its case (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The registered module name as written, compared with the database collation** | No transformation | Module names compare case-insensitively everywhere in Encina (`ModuleRegistry` uses `StringComparer.OrdinalIgnoreCase`), but SQL Server and MySQL collations are case-insensitive while PostgreSQL and MongoDB are case-sensitive: the same filter would return different rows per provider |
| **B) Lower-invariant at write and in the filter, bounded to 128 characters, rejected when blank or longer** | Identical behavior on all 10 providers; same rule as #1636 (D7, recommended there) | The stored value is not the display casing |

### Chosen Option: **B — lower-invariant, `VARCHAR(128)`** (recommended, pending the maintainer)

### Rationale

B wins because provider coherence (`AGENTS.md` section 3) requires one answer for one filter, and #1636 recommends the same rule for the audit rows. A wins if the maintainer decides module names are case-sensitive identifiers. The normalization is one public static helper, `MessagingModuleId`, used by every writer and every filter (Phase 1). The column width follows `TenantId VARCHAR(128)` of the dead letter table (`src/Encina.ADO.MySQL/Scripts/029_CreateDeadLetterMessagesTable.sql`). No tenant-qualified identifier is created, so the reserved `default` tenant segment and the injectivity rule do not apply: `ModuleId` is a column, never part of a key.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. What a module filter returns</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Strict equality: a query for module X returns only rows whose `ModuleId` equals X; a null or blank module id is rejected with `ArgumentException`, never treated as "all rows" and never as "rows without a module"** | A missing module context cannot widen a query | A caller that wants every row uses the existing unfiltered member |
| **B) A null module id means "no filter"** | One member | Fail-open: a lost ambient module silently returns every module's rows |

### Chosen Option: **A — strict equality, blank is rejected**

### Rationale

Gates fail closed when context is missing (`AGENTS.md` section 3), and the issue's acceptance criterion is a module-filtered query, not an optional filter.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (compliance and security gates fail closed when context is missing) and the acceptance criterion "Module-filtered queries supported" of #747.

</details>

<details>
<summary><strong>5. The shape of the module-filtered queries (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One dedicated member per store, next to the existing one: `IOutboxStore.GetPendingForModuleAsync(moduleId, batchSize, maxRetries)`, `IScheduledMessageStore.GetDueForModuleAsync(moduleId, batchSize, maxRetries)`, `ISagaStore.GetActiveByModuleAsync(moduleId, batchSize)`, `IDeadLetterStore.GetByModuleAsync(moduleId, skip, take, newestFirst)`** | The unfiltered members keep their contract; each filter is explicit and strict (Design Choice 4); the same shape as #738's `GetActiveByTenantAsync` | Four interface members and four decorators |
| **B) An optional `string? moduleId = null` parameter on the existing members** | Fewer members | `null` would mean "all" (fail-open, Design Choice 4) and optional parameters on interface members are brittle across 10 implementers |
| **C) Put `ModuleId` in `DeadLetterFilter` and add filter objects for the other stores** | Reuses the dead letter filter | `DeadLetterFilter` properties null means "no filter" and providers would have to be changed in the same pull request as the property (a property that stores ignore is a silent leak) |

### Chosen Option: **A — dedicated members** (recommended, pending the maintainer)

### Rationale

A wins when strictness must be structural. B wins if the maintainer prefers a small interface and accepts a null-means-all parameter guarded by a gate. C wins for the dead letter queue only, where a filter object already exists. The query count stays four because per-module processors are a follow-up (issue file), not part of this plan.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Restoring the module at dispatch, and a row whose module is unknown (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Dispatch inside `IModuleExecutionContext.CreateScope(row.ModuleId)` when the row has a module and `IModuleRegistry` contains it; when the row names a module that is not registered, the delivery fails as a `Left` with `encina.messaging.module_unknown` (retry, then dead letter) and is never dispatched without a module** | Handlers run in the module that wrote the row; #352's boundary rule sees the real caller; an unknown module cannot bypass the rule | A removed module blocks its old rows until they are dead lettered |
| **B) Dispatch without module context when the module is unknown, and log** | Nothing blocks | The row runs as a host call and escapes the boundary rule |
| **C) Always set the stored name without checking the registry** | Simplest | A corrupted or forged value becomes the ambient module |

### Chosen Option: **A — restore, fail closed on an unknown module** (recommended, pending the maintainer)

### Rationale

A wins when module isolation must hold for background delivery (`AGENTS.md` section 3: a `Left` fails the operation, nothing reports success). B wins if module removal during the life of persisted rows is a real operating case. The restoration is active only when `IModuleExecutionContext` and `IModuleRegistry` are registered (optional constructor parameters, the same trigger style as `MultiTenancyMarker` in `DeadLetterManager`): with no module system registered there is nothing to restore or enforce. The ambient tenant restoration is #737/#738/#739's concern and is not touched. New code `encina.messaging.module_unknown` has `ErrorCategory.FailedPrecondition` (ADR-062).

**Class:** Dilemma

</details>

<details>
<summary><strong>7. `InboxMessage` is not given a module (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Out of scope: the inbox deduplicates external messages, which arrive with no source module; the consuming handler's module is already known by the handler registry** | Matches the issue (three entities) and the dead letter comment (one more) | A module cannot list the inbox rows its handlers processed |
| **B) Add `ModuleId` to `IInboxMessage` and the inbox stores as well** | One dimension on all five messaging tables | Another 10 providers and a processing-module meaning that the issue does not define |

### Chosen Option: **A — out of scope** (recommended, pending the maintainer)

### Rationale

A wins while no requirement asks which module processed an external message. B wins if per-module inbox views are wanted for the reference application's operations. Choosing B adds a plan of the same shape as this one for one more entity.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Pull request order (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Implementers first, interfaces last: each provider pull request adds `ModuleId` and the four new query members to its concrete classes (they compile against the old interfaces); the interface and orchestrator pull requests come after every implementer has them** | Every pull request builds and carries its own tests; no scaffold merges | The new members exist on concrete classes for several pull requests before the interfaces have them; 27 pull requests |
| **B) One pull request with the whole change (about 130 files)** | One review | Over the 15-file and 1500-line limits (needs `size-exception-approved`) |
| **C) Default interface members as a scaffold** | Interfaces first | A compatibility layer merges (`AGENTS.md` section 1) and the defaults would be untested production code |

### Chosen Option: **A — implementers first, interfaces last** (recommended, pending the maintainer)

### Rationale

A wins because the limits are hard gates and a half-wired interface would break the build between pull requests. This is the order #738 recommends for the saga row; the two plans touch the same provider files, so whichever merges second rebases.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Schema delivery: edit the scripts in place</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add the column and index to the existing create scripts (`001_`, `003_`, `004_`, `029_` and `000_CreateAllTables.sql` per package), the EF configuration and the Mongo index creator; no migration** | Pre-1.0 has no existing user and no migration support | Existing development databases are recreated |
| **B) Add `ALTER TABLE` migration scripts** | Upgrades in place | A migration helper, forbidden pre-1.0 |

### Chosen Option: **A — in-place scripts**

### Rationale

Encina is pre-1.0 with no existing user and no migration support.

**Class:** Settled
**Settled by:** `AGENTS.md` section 1 (pre-1.0: no backward compatibility, no migration support) and section 3 (no migration helpers).

</details>

<details>
<summary><strong>10. Telemetry names</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse the tag name `module.name` and add one counter `encina.messaging.module_dispatch_total` with tags `pattern`, `module`, `outcome`; the tenant tag only when multi-tenancy is on (ADR-054 D4)** | One tag vocabulary; module names are the registered set, so the tag is bounded | A new counter |
| **B) A new tag name** | None | Two names for one concept |

### Chosen Option: **A — reuse `module.name`**

### Rationale

`module.name` is already the tag of `ModuleActivitySource`.

**Class:** Trivial
**Settled by:** `src/Encina/Modules/Diagnostics/ModuleActivitySource.cs` (`activity?.SetTag("module.name", moduleName)`) and ADR-054 D4 (tenant tag only with multi-tenancy).

</details>

---

## Implementation Phases

### Phase 1: Foundation in `Encina.Messaging` (pull request 1)

> **Goal**: The normalizer and the dead letter carrier, so provider pull requests have one rule to call.

<details>
<summary>Tasks</summary>

1. `src/Encina.Messaging/MessagingModuleId.cs` (public static): `const int MaxLength = 128`; `string Normalize(string moduleId)` (trims edge white space, rejects blank or longer values with `ArgumentException`, returns `ToLowerInvariant()`); `string? NormalizeOrNull(string? moduleId)` (null and blank give null, used when stamping the ambient module).
2. `src/Encina.Messaging/DeadLetter/IDeadLetterMessageFactory.cs`: `DeadLetterData` gains the optional trailing parameter `string? ModuleId = null` (named-argument callers compile unchanged; the provider factories copy it in their own pull requests).
3. `DeadLetterStoreLimits`: `ModuleIdMaxLength = MessagingModuleId.MaxLength`.
4. Tests: unit `MessagingModuleIdTests` (case, trim, blank, 128 and 129 characters, null), guard (`Normalize` blank and null), property (idempotent, never longer than the input plus nothing, equal for inputs that differ only by case), `DeadLetterData` default; `PublicAPI.Unshipped.txt` and `Encina.Messaging.json` manifest through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #747; plan docs/plans/module-id-messaging-implementation-plan-747.md. D1-D3, D5-D8 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- Encina is pre-1.0: no compatibility overloads, no migration helpers.

TASK:
Add MessagingModuleId and the DeadLetterData.ModuleId parameter with the limits constant and their tests.

KEY RULES:
- Normalize is lower-invariant and bounded (D3); blank is an ArgumentException, never silently null.
- XML docs on every public member; zero warnings; PublicAPI.Unshipped.txt updated.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/IDeadLetterMessageFactory.cs
- src/Encina.Messaging/DeadLetter/DeadLetterInputs.cs
- tests/Encina.UnitTests/Messaging/DeadLetter
```

</details>

---

### Phase 2: ADO.NET and Dapper providers (6 packages, 12 pull requests)

> **Goal**: Column, state class, factory copy and SQL in each of the six packages, two pull requests per package: A = outbox and scheduled message, B = saga and dead letter (B after A, because both edit `000_CreateAllTables.sql`).

<details>
<summary>Tasks</summary>

Per package (`Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`):

1. Pull request A: `Outbox/OutboxMessage.cs` and `Scheduling/ScheduledMessage.cs` get `string? ModuleId` (compiles against the old interfaces); `Outbox/OutboxStoreADO|Dapper.cs` and `Scheduling/ScheduledMessageStoreADO|Dapper.cs` include `ModuleId` in the `INSERT` and in every `SELECT` column list and add `GetPendingForModuleAsync` and `GetDueForModuleAsync` (strict `ModuleId = @ModuleId`, the argument passed through `MessagingModuleId.Normalize`, the provider's paging syntax: SQL Server `TOP (@n)`, PostgreSQL and MySQL `LIMIT @n`, same retry and ordering predicate as the unfiltered member); scripts `Scripts/001_CreateOutboxMessagesTable.sql`, `Scripts/004_CreateScheduledMessagesTable.sql` and `Scripts/000_CreateAllTables.sql` get `ModuleId` (`VARCHAR(128) NULL`; SQL Server `NVARCHAR(128)`; MySQL with `utf8mb4_bin`, PostgreSQL as its neighbours) and an index `(ModuleId, ProcessedAtUtc, ...)` whose trailing columns follow the existing pending-query index of the script; `PublicAPI.Unshipped.txt`, manifest entries, and one integration test class per package `ModuleIdOutboxScheduledIntegrationTests` (`[Collection("ADO-<Db>")]` or `[Collection("Dapper-<Db>")]`: round trip of the column, strict filter returns only the module's rows, another module and a row without module are not returned, blank argument throws).
2. Pull request B: `Sagas/SagaState.cs`, `DeadLetter/DeadLetterMessage.cs`, `DeadLetter/DeadLetterMessageFactory.cs` (copies `data.ModuleId`), `Sagas/SagaStoreADO|Dapper.cs` (`GetActiveByModuleAsync`), `DeadLetter/DeadLetterStoreADO|Dapper.cs` (`GetByModuleAsync`), scripts `003_CreateSagaStatesTable.sql`, `029_CreateDeadLetterMessagesTable.sql` and `000_CreateAllTables.sql`, PublicAPI, manifest entries and the integration test class `ModuleIdSagaDeadLetterIntegrationTests`.
3. All database calls stay asynchronous with a `CancellationToken`; never `IDbConnection.Open()`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #747. The interfaces are NOT changed yet (D8): this pull request adds ModuleId and the module query members to ONE package's concrete classes so that it builds against the current interfaces.

TASK:
Apply the Phase 2 tasks to the one package and half (A or B) of this pull request; the six implementations must behave identically and differ only in SQL dialect.

KEY RULES:
- Strict equality (D4): a blank module id throws ArgumentException; a null column never matches a module.
- The argument goes through MessagingModuleId.Normalize (D3); the column is written normalized.
- SQL Server: @param, TOP (@n); PostgreSQL: LIMIT @n, case-sensitive identifiers; MySQL: LIMIT @n, backticks, utf8mb4_bin.
- Do not drop columns or add migrations (D9). Integration tests use the shared [Collection] fixture and ClearAllDataAsync in InitializeAsync, never IClassFixture.

REFERENCE FILES:
- src/Encina.ADO.PostgreSQL/Outbox/OutboxStoreADO.cs
- src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs
- src/Encina.ADO.MySQL/Scripts/029_CreateDeadLetterMessagesTable.sql
- tests/Encina.IntegrationTests/ADO/PostgreSQL
```

</details>

---

### Phase 3: EF Core and MongoDB providers (3 pull requests)

> **Goal**: The same behavior in `Encina.EntityFrameworkCore` (one implementation for SQL Server, PostgreSQL and MySQL, two pull requests) and `Encina.MongoDB` (one pull request).

<details>
<summary>Tasks</summary>

1. EF Core A (outbox and scheduled): `Outbox/OutboxMessage.cs`, `Outbox/OutboxMessageConfiguration.cs`, `Outbox/OutboxStoreEF.cs`, `Scheduling/ScheduledMessage.cs`, `Scheduling/ScheduledMessageConfiguration.cs`, `Scheduling/ScheduledMessageStoreEF.cs`: the property, `HasMaxLength(128)`, `HasIndex(...)` beside the existing indexes, the two module members as LINQ equality predicates; PublicAPI, manifest, and three integration test classes (`EFCore-SqlServer`, `EFCore-PostgreSQL`, `EFCore-MySQL`).
2. EF Core B (saga and dead letter): the same set for `Sagas/SagaState.cs`, `SagaStateConfiguration.cs`, `SagaStoreEF.cs`, `DeadLetter/DeadLetterMessage.cs`, `DeadLetterMessageConfiguration.cs`, `DeadLetterMessageFactory.cs`, `DeadLetterStoreEF.cs`. No EF migration is added (D9).
3. MongoDB: the four `*/{Outbox,Saga,ScheduledMessage,DeadLetter}Message|SagaState.cs` documents get the field, the four stores get the module members, `MongoDbIndexCreator` gets four compound indexes guarded by the existing `Use*` flags, `DeadLetterMessageFactory` copies `data.ModuleId`; PublicAPI, manifest, one integration test class on the MongoDB fixture.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Phase 2 pull requests are merged (issue #747); the interfaces are still unchanged (D8).

TASK:
Implement the Phase 3 tasks, one pull request per item (EF Core A, EF Core B, MongoDB).

KEY RULES:
- One EF store behavior for SQL Server, PostgreSQL and MySQL, proved by integration tests on each database.
- The lower-invariant value is written and filtered (D3); blank is an ArgumentException (D4).
- Index creation in MongoDB goes through MongoDbIndexCreator, guarded by the existing Use* flags.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Sagas/SagaStateConfiguration.cs
- src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs
- src/Encina.MongoDB/MongoDbIndexCreator.cs
- src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs
```

</details>

---

### Phase 4: Fakes, generators and test doubles (3 pull requests)

> **Goal**: The test infrastructure follows the entities; these pull requests build against the old interfaces.

<details>
<summary>Tasks</summary>

1. Pull request 17, `Encina.Testing.Fakes`: `FakeOutboxMessage`, `FakeSagaState`, `FakeScheduledMessage`, `FakeDeadLetterMessage` get `ModuleId`; `FakeOutboxStore`, `FakeSagaStore`, `FakeScheduledMessageStore`, `FakeDeadLetterStore` get the module members with the strict rule of D4; PublicAPI, manifest, unit tests.
2. Pull request 18, `Encina.Testing.Bogus` (`OutboxMessageFaker`, `SagaStateFaker`, `ScheduledMessageFaker`) and `Encina.Testing.FsCheck` (`ArbitraryMessages`): the property and a rule that generates a valid lower-invariant module or null; PublicAPI x2, manifests x2.
3. Pull request 19, benchmark states and test doubles that implement the message interfaces: `BenchmarkEntityFactory` in `Encina.ADO.Benchmarks` and `Encina.Dapper.Benchmarks`, the doubles in `OutboxOrchestratorTests`, `SagaOrchestratorTests`, `DeadLetterOrchestratorTests` and `RetryAfterCeilingProperties`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #747. The fake stores must behave exactly like the real stores for the module rules; the contract tests of Phase 9 run against them.

TASK:
Add ModuleId and the module members to the fakes, generators and doubles listed in the Phase 4 tasks.

KEY RULES:
- Pull requests 17-19 compile against the current interfaces: add members to classes, do not touch interfaces, do not call the new members through an interface-typed field.
- The fake normalizes through MessagingModuleId like the real stores.

REFERENCE FILES:
- src/Encina.Testing.Fakes/Stores/FakeOutboxStore.cs
- src/Encina.Testing.Bogus/SagaStateFaker.cs
- src/Encina.Testing.FsCheck/ArbitraryMessages.cs
- tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks/Infrastructure/BenchmarkEntityFactory.cs
```

</details>

---

### Phase 5: Interfaces and instrumented stores (3 pull requests)

> **Goal**: Every implementer now has the members; the interfaces and the decorators that are themselves implementers move together.

<details>
<summary>Tasks</summary>

1. Pull request 20: `IOutboxMessage`, `ISagaState`, `IScheduledMessage`, `IDeadLetterMessage` get `string? ModuleId { get; set; }` (XML docs: the lower-invariant name of the module that created the row, null for host work); PublicAPI, manifest, contract test `MessagingModuleIdContractTests` (every real row class and the fake round-trips the property).
2. Pull request 21: `IOutboxStore.GetPendingForModuleAsync`, `IScheduledMessageStore.GetDueForModuleAsync` and their `InstrumentedOutboxStore` and `InstrumentedScheduledMessageStore` delegations (the module is a span tag `module.name` on the existing activity, never an unbounded metric label); PublicAPI x2, manifests x2, unit tests of the two decorators.
3. Pull request 22: `ISagaStore.GetActiveByModuleAsync`, `IDeadLetterStore.GetByModuleAsync`, `InstrumentedSagaStore`, `InstrumentedDeadLetterStore` and the three store test doubles (`RecordingSagaStore` in `SagaRunnerOutcomeScenarios.cs`, `FailingUpdateStore` in `SagaRunnerTests.cs`, the dead letter store double in `AddDeadLetterQueueServicesTests.cs`); PublicAPI x2, manifests x2, unit tests.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Issue #747. All provider classes, fakes and doubles already have ModuleId and the module members (pull requests 2-19).

TASK:
Add the members to the interfaces in the three pull requests above, with the decorators and store doubles that implement them.

KEY RULES:
- Each pull request builds alone. A decorator delegates and adds only the span tag; it never swallows a Left.
- Update PublicAPI.Unshipped.txt of Encina.Messaging and Encina.OpenTelemetry; no [Obsolete], no default interface members.

REFERENCE FILES:
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedSagaStore.cs
- src/Encina.Messaging/Sagas/ISagaStore.cs
- tests/Encina.IntegrationTests/Infrastructure/Sagas/SagaRunnerOutcomeScenarios.cs
```

</details>

---

### Phase 6: Stamping and restoring in the orchestrators (pull requests 23 and 25)

> **Goal**: The behavior of the issue: auto-populate from the ambient module and restore it on dispatch.

<details>
<summary>Tasks</summary>

1. Pull request 23 (stamping): `OutboxOrchestrator.AddAsync`, `SchedulerOrchestrator.ScheduleAsync` and `ScheduleRecurringAsync`, `SagaOrchestrator.StartAsync`, `DeadLetterOrchestrator.NewData`: new optional constructor parameter `IModuleExecutionContext? moduleContext = null`; after the factory returns, `row.ModuleId = MessagingModuleId.NormalizeOrNull(moduleContext?.CurrentModule)` (D1, D2); the dead letter takes the ambient module when it is captured in module context, and `OutboxOrchestrator`'s dead letter capture passes the source row's module through `DeadLetterData.ModuleId` so that a dead letter keeps the module of the message that failed. Every changed constructor line in `PublicAPI.Unshipped.txt` (remove old, add new).
2. Pull request 25 (restoration, after the observability pull request 24): `MessagingModuleScope` (internal static, in `Encina.Messaging`): `IDisposable? Enter(IModuleExecutionContext? context, IModuleRegistry? registry, string? rowModuleId, out EncinaError? error)`; `OutboxBatchProcessor` wraps the publish callback, `SchedulerOrchestrator.ProcessDueMessagesAsync` (line 305, driven by the `ScheduledMessageProcessor` hosted service) wraps each message's dispatch, and `SagaOrchestrator.TimeoutAsync` (line 488) and the stuck-saga path wrap their step in it. An unregistered module returns `Left(encina.messaging.module_unknown)` (`ErrorCategory.FailedPrecondition`) from the delivery, which goes through the existing retry and dead letter handling. With no `IModuleExecutionContext` or no `IModuleRegistry` registered nothing is restored.
3. `MessagingModuleErrorCodes` (public static, `ModuleUnknown`), registered as ADR-062 category `FailedPrecondition`.
4. Registration: the optional parameters are resolved by the existing `AddEncinaMessaging` registrations (the types already exist as services when `AddEncinaModules` is used); no new service. DI test with `ValidateOnBuild` and `ValidateScopes`: with and without modules registered.
5. Tests: unit (stamp from an explicit scope, null outside a module, restoration inside the publish callback, unknown module returns the error and is not dispatched, host row dispatches without scope, a failing handler's `Left` is returned and the scope is disposed), guard for the new constructor parameters and the helper, contract (orchestrators stamp identically on the fakes), property (a row stamped under module M is dispatched with `CurrentModule == M` for any generated M).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Issue #747. The interfaces and every implementer have ModuleId (Phases 1-5). #352 pull request 1 supplies the module context producer; tests set the module with IModuleExecutionContext.CreateScope.

TASK:
Implement the Phase 6 tasks in src/Encina.Messaging.

KEY RULES:
- Never claim atomicity: the orchestrators never call SaveChangesAsync for the row (the transaction owners are #718, #720, #1236).
- A Left from the publish callback is returned, never swallowed; an unknown module fails the delivery and is never dispatched without a module (D6).
- No scoped service injected into a singleton; processors resolve IModuleExecutionContext through the scope they already create.
- Time from TimeProvider; EncinaError.Message never logged; exceptions through ForLogging().

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs
- src/Encina/Modules/Isolation/ModuleExecutionContext.cs
```

</details>

---

### Phase 7: Cross-Cutting Integration

> **Goal**: The functions marked Include in the matrix.

<details>
<summary>Tasks</summary>

1. Module isolation (function 11): the stamp, the filter and the restoration of Phases 2 to 6. The enforcement of who may read which rows stays with #352 and #359.
2. Multi-tenancy (function 10): the module column is independent of the tenant column; a tenant-scoped query (#737, #738, #739) and a module-scoped query are separate members and are never combined implicitly. No tenant-qualified identifier is created, so the reserved `default` tenant segment and the injectivity rule do not apply. The tenant tag on the new counter follows ADR-054 D4.
3. Deferred, as issue files in this plan's worktree: `artifacts/issues/module-scoped-messaging-processors.md` (per-module outbox and scheduler processors and per-module processing policies; the module-filtered members of Phase 5 are the building block).

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT:
- Issue #747; the cross-cutting matrix of the plan is the contract.

TASK:
Verify that each Include row of the matrix exists in code and tests, and that the issue file for per-module processors is written.

KEY RULES:
- A module filter and a tenant filter are never merged into one predicate by default.
- Never log EncinaError.Message or row content.

REFERENCE FILES:
- AGENTS.md sections 3 and 6
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

---

### Phase 8: Observability

> **Goal**: Traces, a counter and logs for the restoration path (pull request 24, which merges before the restoration of pull request 25; the span tag of the decorators is in pull requests 21 and 22).

<details>
<summary>Tasks</summary>

1. `src/Encina.Messaging/Diagnostics/MessagingModuleMetrics.cs`: `Meter` `Encina.Messaging.Module`, counter `encina.messaging.module_dispatch_total` with tags `pattern` (`outbox`, `scheduling`, `saga`), `module`, `outcome` (`dispatched`, `unknown_module`) and the tenant tag only when multi-tenancy is on (ADR-054 D4); registered by `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`.
2. `src/Encina.Messaging/Diagnostics/MessagingModuleLog.cs` (`[LoggerMessage]`): EventIds 7650 module stamped (Debug), 7651 module restored for dispatch (Debug), 7652 unknown module at dispatch (Warning, error code and module name only), 7653 restoration skipped because the module system is not registered (Debug). Range `EventIdRanges.MessagingModuleScope = (7650, 7664)`, registered first in `src/Encina/Diagnostics/EventIdRanges.cs` and in the `Encina.Messaging` entry of the allocation test map `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
3. The span tag `module.name` on the existing activities of the instrumented stores and processors (Phase 5).

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
CONTEXT:
- Issue #747. ADR-021: register the range first. ADR-054 D4: tenant tag only with multi-tenancy.

TASK:
Add the meter, the log class and the range with their tests.

KEY RULES:
- EventIds packed from 7650 with no gaps inside 7650-7664 (7654-7664 stay free).
- Logs carry error codes and module names only; never EncinaError.Message.
- Run the architecture tests that scan EventIds.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 9: Testing

> **Goal**: Per-flag coverage at the manifest targets, including real databases for all 10 providers.

<details>
<summary>Tasks</summary>

Tests travel with the pull request that adds the code they cover (see the split).

1. **Unit**: pull requests 1, 17, 21-23 (normalizer, fakes, decorators, orchestrators).
2. **Guard** (`tests/Encina.GuardTests`): `MessagingModuleId.Normalize` (null, empty, white space, 129 characters), each new store member (blank module id), each changed constructor (null `moduleContext` is valid: a test of the valid result, not an exception).
3. **Contract**: `MessagingModuleIdContractTests` runs the same module scenarios against `FakeOutboxStore`, `FakeSagaStore`, `FakeScheduledMessageStore`, `FakeDeadLetterStore` and the provider stores.
4. **Property**: the normalizer and "a row stamped under M is dispatched under M".
5. **Integration**: store-level tests in each provider pull request (Phases 2 and 3); the end-to-end test in pull request 26: a module-scoped outbox message is written under module M, processed by the real outbox processor, and the handler observes `CurrentModule == M`, on every provider through its shared collection (`ADO-`, `Dapper-` x `SqlServer`/`PostgreSQL`/`MySQL`, `EFCore-` x three, MongoDB); `ClearAllDataAsync` in `InitializeAsync`; never `IClassFixture`.
6. **Load**: justification file `tests/Encina.LoadTests/Messaging/ModuleId.md` (one extra column and equality predicate; the concurrent paths keep their existing load tests). **Benchmark**: justification file `tests/Encina.BenchmarkTests/Messaging/ModuleId.md`.
7. **Per-file targets** (each flag with a one-sentence justification written in the manifest; measured with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and `.github/scripts/coverage-report.cs`; baselines of edited files are measured at work start and a target is the measured value or higher):

   | File group | Unit | Guard | Contract | Property | Integration | Justification |
   |---|---|---|---|---|---|---|
   | `MessagingModuleId.cs`, `MessagingModuleScope.cs`, `MessagingModuleErrorCodes.cs` | 100 | 90 | 0 | 90 | 0 | Pure functions whose every branch is a unit test; guard reaches the argument checks; the property test covers the case rule |
   | Orchestrator module code (`Encina.Messaging`) | 95 | 80 | 80 | 60 | 0 | Stamp, restore and unknown module run with fake stores; the real stores have their own integration flag |
   | `MessagingModuleMetrics.cs`, `MessagingModuleLog.cs` | 90 | 0 | 0 | 0 | 0 | Counter and log calls are reached through the orchestrator unit tests; no parameterized public member |
   | Provider stores, row classes, factories (ADO.NET x3, Dapper x3, EF Core, MongoDB) | 0 | 90 | 80 | 0 | 80 | The SQL, LINQ and Mongo filters can only be proved on real databases (`AGENTS.md` section 5); guard and contract cover the new members |
   | Fakes, fakers, `ArbitraryMessages`, instrumented stores | 90 | 90 | 80 | 0 | 0 | Test doubles must behave like the real stores; delegation plus one span tag is fully reachable through a listener test |

</details>

<details>
<summary>Prompt for AI Agents — Phase 9</summary>

```text
CONTEXT:
- Issue #747. Tests travel with the pull request that adds the code they cover; the end-to-end test is pull request 26. AGENTS.md section 9 per-flag coverage model.

TASK:
Write the tests of Phase 9 Tasks 1-6 and fill the manifest targets with one-sentence justifications.

KEY RULES:
- Integration tests on real databases for all 10 providers, never a .md for them; Shouldly through Encina.Testing.Shouldly.
- Tests execute real code and never swallow an exception; a nullable parameter that accepts null gets a valid-result test, not an exception test.
- Targets never go down.

REFERENCE FILES:
- tests/Encina.UnitTests/Messaging/Outbox/OutboxOrchestratorTests.cs
- docs/testing/integration-tests.md
- .github/coverage-manifest/Encina.Messaging.json
```

</details>

---

### Phase 10: Documentation & Finalization

<details>
<summary>Tasks</summary>

1. XML docs on every new public member; `PublicAPI.Unshipped.txt` per touched package (through `mechanical-fixer`).
2. `changelog.d/747-messaging-module-id.added.md` (through `mechanical-fixer`); the messaging and module guides gain a "Module-scoped messages" section (through `docs-writer`); `docs/INVENTORY.md` and `ROADMAP.md` lines. ADR number assigned when the ADR pull request opens, only if a Dilemma is decided architecturally.
3. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target; CRAP of every changed method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 10</summary>

```text
CONTEXT:
- Issue #747 is implemented and tested.

TASK:
Write the documentation set of the Phase 10 tasks through the specialists that own them.

KEY RULES:
- Documentation pages go through docs-writer; changelog fragments, PublicAPI lines and manifests through mechanical-fixer; coverage figures are cited with covref markers, never typed; never edit CHANGELOG.md.

REFERENCE FILES:
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

Order rule (D8 option A): implementers first, interfaces last; every pull request builds, carries its own tests, `PublicAPI.Unshipped.txt` and manifest entries. Counts include tests and manifests.

| PR | Content | Approx. files | Approx. lines |
|----|---------|---------------|---------------|
| 1 | `Encina.Messaging`: `MessagingModuleId`, `DeadLetterData`, limits, PublicAPI, manifest, 3 test files | 8 | 350 |
| 2-7 | One per ADO.NET and Dapper package, part A: 2 row classes, 2 stores, 3 scripts, PublicAPI, manifest, integration test | 10 | 450 |
| 8-13 | One per ADO.NET and Dapper package, part B (after A): 2 row classes, factory, 2 stores, 3 scripts, PublicAPI, manifest, integration test | 12 | 500 |
| 14 | `Encina.EntityFrameworkCore` A: 2 rows, 2 configurations, 2 stores, PublicAPI, manifest, 3 integration classes | 11 | 550 |
| 15 | `Encina.EntityFrameworkCore` B: the same for saga and dead letter (plus the factory) | 12 | 600 |
| 16 | `Encina.MongoDB`: 4 documents, factory, 4 stores, index creator, PublicAPI, manifest, integration test | 13 | 700 |
| 17 | `Encina.Testing.Fakes`: 4 models, 4 stores, PublicAPI, manifest, unit tests | 11 | 500 |
| 18 | `Encina.Testing.Bogus` and `Encina.Testing.FsCheck`: 3 fakers, `ArbitraryMessages`, PublicAPI x2, manifests x2 | 8 | 250 |
| 19 | Benchmark states (2) and message doubles (4) | 6 | 150 |
| 20 | 4 message interfaces, PublicAPI, manifest, contract test | 7 | 250 |
| 21 | `IOutboxStore`, `IScheduledMessageStore`, 2 decorators, PublicAPI x2, manifests x2, 2 decorator tests | 10 | 450 |
| 22 | `ISagaStore`, `IDeadLetterStore`, 2 decorators, 3 store doubles, PublicAPI x2, manifests x2, 2 decorator tests | 13 | 600 |
| 23 | Stamping: the 4 orchestrators, PublicAPI, manifest, the 4 existing orchestrator unit test files (edited), guard test, DI test | 12 | 800 |
| 24 | Observability: `MessagingModuleMetrics`, `MessagingModuleLog`, `EventIdRanges`, allocation test map, OpenTelemetry `ServiceCollectionExtensions`, PublicAPI x2, manifests x2, 2 unit tests | 11 | 450 |
| 25 | Restoration: `MessagingModuleScope`, `MessagingModuleErrorCodes`, `OutboxBatchProcessor`, `SchedulerOrchestrator`, `SagaOrchestrator`, PublicAPI, manifest, unit, property and contract tests | 12 | 900 |
| 26 | End-to-end integration test on 10 providers (4 files: ADO, Dapper, EF Core, MongoDB classes), 2 justification files | 6 | 600 |
| 27 | Documentation: guide, inventory and roadmap lines, changelog fragment (through `docs-writer` and `mechanical-fixer`) | 4 | 300 |

Pull requests 2 to 7 are part A and 8 to 13 part B of the six ADO.NET and Dapper packages, so the sequence is 1 to 27. The orchestrator opens them in the order above; the six providers of a part are independent and can be reviewed in parallel. A pull request that measures over 15 files or 1,500 lines when it is briefed is split again.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| `AGENTS.md` section 3 | Gates fail closed; a `Left` is never swallowed in background infrastructure; `EncinaError.Message` never logged | Strict filter, unknown module fails the delivery |
| `AGENTS.md` section 5 | 10 database providers | Provider table |
| `AGENTS.md` section 6, function 11 | `ModuleId`, `IModuleContext` as the module dimension | The feature itself |
| ADR-054 D4 | Tenant tag only when multi-tenancy is on | Counter tags |
| ADR-062 | `ErrorCategory` on every error code | `encina.messaging.module_unknown` is `FailedPrecondition` |
| Modular monolith guidance (Spring Modulith event publication registry; Kamil Grzybek's modular monolith) | A published event or job belongs to the module that created it | Concept source named by the issue |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IModuleExecutionContext`, `ModuleExecutionContext.CreateScope` | `src/Encina/Modules/Isolation/` | Source of the module and the restoration scope |
| `IModuleRegistry` | `src/Encina/Modules/IModuleRegistry.cs` | Is the stored module registered |
| `ModuleActivitySource` (`module.name`) | `src/Encina/Modules/Diagnostics/ModuleActivitySource.cs` | Tag vocabulary |
| `OutboxOrchestrator`, `OutboxBatchProcessor` | `src/Encina.Messaging/Outbox/` | Stamp and restore |
| `SchedulerOrchestrator` (`ProcessDueMessagesAsync`), `ScheduledMessageProcessor` | `src/Encina.Messaging/Scheduling/` | Stamp and restore |
| `SagaOrchestrator` | `src/Encina.Messaging/Sagas/SagaOrchestrator.cs` | Stamp (line 123) and background paths |
| `DeadLetterOrchestrator.NewData`, `DeadLetterData` | `src/Encina.Messaging/DeadLetter/` | Dead letter stamp |
| `DeadLetterManager.AmbientTenantId` | `src/Encina.Messaging/DeadLetter/DeadLetterManager.cs` | Precedent for the optional-context trigger |
| Store files (4 entities x 9 providers) | `src/Encina.*/{Outbox,Sagas,Scheduling,DeadLetter}/` | Column and filter |
| `MongoDbIndexCreator` | `src/Encina.MongoDB/MongoDbIndexCreator.cs` | Compound indexes |
| `InstrumentedOutboxStore` and siblings | `src/Encina.OpenTelemetry/MessagingStores/` | Delegation and span tag |
| `FakeOutboxStore` and siblings | `src/Encina.Testing.Fakes/Stores/` | Test doubles |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (`MessagingModuleScope`, new) | 7650-7664, ids 7650-7653 used | From the assigned block 7650-7699 (registry `artifacts/decisions/plan-batch/eventid-blocks.md`); 7654-7664 free; the allocation test map gains the range under `Encina.Messaging`. Other plans of the family use 7665-7699 (#366 7665-7679, #365 7680-7689, #423 7690-7695, #306 7696-7699) |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Messaging core | ~14 | Helper, 4 interfaces, 4 store members, orchestrators, scope helper, metrics, log, error codes, `EventIdRanges` |
| ADO.NET x3 and Dapper x3 | ~66 | Per package: 4 row classes, 4 stores, factory, 5 scripts, PublicAPI, manifest |
| EF Core and MongoDB | ~20 | Rows, configurations, stores, factories, index creator |
| Fakes, fakers, instrumented stores, doubles | ~28 | With PublicAPI and manifests |
| Tests | ~30 | 12 provider integration classes, 3 EF classes, end-to-end, unit, guard, contract, property, DI |
| Documentation and fragments | ~6 | Guide, inventory, roadmap, changelog, 2 justification files |
| **Total** | **~130 across 27 pull requests** | The sum of the rows of the split table is the authority |

### Provider Matrix

| Provider | Column and row class | Store filter | Integration test |
|----------|----------------------|--------------|------------------|
| ADO-SqlServer, ADO-PostgreSQL, ADO-MySQL | Required | Required | `[Collection("ADO-<Db>")]` |
| Dapper-SqlServer, Dapper-PostgreSQL, Dapper-MySQL | Required | Required | `[Collection("Dapper-<Db>")]` |
| EFCore-SqlServer, EFCore-PostgreSQL, EFCore-MySQL | One entity class per kind | Required | `[Collection("EFCore-<Db>")]` x3 |
| MongoDB | Required (document field) | Required | MongoDB fixture |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers, no migrations.
- Issue #747: ModuleId on the outbox, saga, scheduled message and dead letter rows for the 10 database providers. Plan: docs/plans/module-id-messaging-implementation-plan-747.md; Dilemmas D1-D3 and D5-D8 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (the order of the 27 pull requests; every one builds):
1. MessagingModuleId and DeadLetterData.ModuleId (Phase 1).
2. ADO.NET and Dapper packages, part A then B (Phase 2); EF Core and MongoDB (Phase 3).
3. Fakes, generators, benchmark states and message doubles (Phase 4).
4. Message interfaces, then store interfaces with decorators and store doubles (Phase 5).
5. Orchestrators: stamping (23), observability (24), restoration (25) (Phases 6-8).
6. End-to-end tests on real databases (26), then documentation (27) (Phases 9-10).

KEY PATTERNS:
- The module comes from IModuleExecutionContext.CurrentModule, is stamped after the factory returns and is stored lower-invariant; filters are strict equality and a blank module id is an ArgumentException.
- A stored module is restored with CreateScope around the dispatch; an unregistered module fails the delivery as a Left.
- Dialects: SQL Server TOP (@n); PostgreSQL and MySQL LIMIT @n; MySQL utf8mb4_bin.
- Async database calls with CancellationToken; TimeProvider for time; no EncinaError.Message in logs; no atomicity claim (the orchestrators never call SaveChangesAsync).
- Tests: Shouldly, shared [Collection] fixtures, per-flag coverage from the manifests.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs
- src/Encina.Messaging/DeadLetter/DeadLetterOrchestrator.cs
- src/Encina/Modules/Isolation/ModuleExecutionContext.cs
- src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs
- src/Encina.MongoDB/MongoDbIndexCreator.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: messaging rows need read-your-writes; the module in cache keys is #746 |
| 2 | OpenTelemetry | ✅ | Phase 8: `module.name` span tag, counter `encina.messaging.module_dispatch_total`, tenant tag only with multi-tenancy (ADR-054 D4) |
| 3 | Structured Logging | ✅ | Phase 8: `MessagingModuleLog`, EventIds 7650-7653, error code and module name only |
| 4 | Health Checks | ❌ | Not applicable: no new dependency; the existing outbox and saga health checks are unchanged |
| 5 | Validation | ✅ | `MessagingModuleId.Normalize` bounds and rejects blank values; the stored module is checked against `IModuleRegistry` at dispatch (D6) |
| 6 | Resilience | ✅ | An unknown module is a failed delivery handled by the existing retry and dead letter path; no new external call |
| 7 | Distributed Locks | ❌ | Not applicable: module scoping does not change row claiming |
| 8 | Transactions | ❌ | Not applicable: the column is written with the row by the same store call; commit ownership stays with #718, #720 and #1236 |
| 9 | Idempotency | ❌ | Not applicable: no interaction with idempotency keys (#736) |
| 10 | Multi-Tenancy | ✅ | Independent column and queries; no combined default predicate; tenant tag on the counter (Phase 7) |
| 11 | Module Isolation | ✅ | This is the feature |
| 12 | Audit Trail | ⏭️ | Deferred: module on audit rows is #1636 (open); operator actions on dead letters are #2013 |

---

## Prerequisites & Dependencies

- #352 pull request 1 (module context producer in core) makes the ambient module available outside EF Core with module isolation; this plan's tests use an explicit scope, so no pull request of this plan waits for it, but the stamped value is `null` in production code paths until it merges.
- #738 (saga tenant), #737 and #739 (tenant on outbox and scheduled rows) and #1237 and #750 (saga correlation and audit) edit the same saga, outbox and scheduled files and scripts; the second to merge rebases. Their column and member names do not overlap with this plan's.
- #1636 and #746 recommend the same lower-invariant rule (D3) so the module is spelled one way across audit rows, cache keys and messaging rows.
- #583 and #2013 (dead letter stores and replay context) are closed or planned; `DeadLetterMessages` is in this plan's scope by the maintainer's comment of 2026-10-09 on #747.

---

## Next Steps

1. The maintainer answers D1, D2, D3, D5, D6, D7 and D8 when work on #747 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Pull request 1 first (the helper); then the provider pull requests in the order of the split.
3. Open the issue file `artifacts/issues/module-scoped-messaging-processors.md` when this plan merges.
