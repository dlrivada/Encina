# Implementation Plan: Queryable `ModuleId` on the audit entries and queries of every audit store

> **Issue**: [#1636](https://github.com/dlrivada/Encina/issues/1636)
> **Type**: Feature
> **Complexity**: High (9 phases, 10 database providers plus Marten and in-memory, about 85 files, six pull requests)
> **Estimated Scope**: ~59 production and script files, ~19 test files, ~7 documentation files
> **Related plans**: `abac-decision-audit-implementation-plan-751.md` (stores the module as metadata today), `audit-marten-implementation-plan-800.md`, `audit-abac-multitenancy-implementation-plan-798.md` (edits the same audit files; sequence them, see Prerequisites)

---

## Summary

Add a first-class `ModuleId` to the audit rows and the audit queries so a modular monolith can ask an audit store "what did module X do" server-side, on every provider.

**State of the code on `7a0049c07` (corrects the issue text).**

- The issue speaks of `AuditEntry`, `AuditQuery` and `IAuditStore` and of an "`IAuditStore` registration bug (ADO/Dapper/MongoDB)". ADR-036 renamed them: the row is `OperationAuditEntry` (`src/Encina.Security.Audit/OperationAuditEntry.cs`, `TenantId` at line 86), the query is `OperationAuditQuery` (`OperationAuditQuery.cs`), the port is `IOperationAuditStore`. The registration bug is fixed: all nine provider registrations now call `TryAddScoped<IOperationAuditStore, ...>` after `OperationAuditStoreRegistration.RemoveInMemoryDefault`.
- Nothing carries a module on an audit row. The ABAC decision audit writes the module only as the metadata key `abac.module_id` (`ABACDecisionAuditSchema.MetadataModuleId`, filled from `context.GetModuleName()` in `ABACDecisionRecordFactory.cs:57`, read back in `ABACDecisionAuditEntryMapper.cs:131`), which no store can filter. The module name lives in the request context metadata key `Encina.ModuleName` (`src/Encina/Modules/RequestContextModuleExtensions.cs`).
- Every store already persists and filters `TenantId` the way a module id would be: ADO and Dapper (columns in `028_CreateOperationAuditEntriesTable.sql` x6, filter in `BuildWhere`), EF Core (`OperationAuditEntryEntity`, `OperationAuditEntryEntityConfiguration` with a filtered index, `OperationAuditStoreEF`), MongoDB (`OperationAuditEntryDocument`, `OperationAuditStoreMongoDB`), Marten (plaintext structural field on `OperationAuditEntryRecordedEvent`, `OperationAuditEntryReadModel`, an index in `ConfigureMartenOperationAuditProjections`, `AuditEventEncryptor` copies), and `InMemoryOperationAuditStore`.
- The read audit trail has the same shape (`ReadAuditEntry`, `ReadAuditQuery`, `IReadAuditStore`, table `020_CreateReadAuditEntriesTable.sql` x6, `ReadAuditStoreADO/Dapper/EF/MongoDB`, `MartenReadAuditStore`, `InMemoryReadAuditStore`) and has no module either. #748 was closed as a duplicate of this issue with the note "check #748's `IReadAuditStore` module filter is covered by #1636".

**Scope.** `ModuleId` on `OperationAuditEntry` and `OperationAuditQuery` (and, per Design Choice 3, on `ReadAuditEntry` and `ReadAuditQuery`), filled by the entry factories from the request context, persisted and filtered on 10 providers, Marten and in-memory; the ABAC decision audit moves the module from metadata to the column and gains a `ModuleId` query filter.

**Out of scope.** `IAuditLogStore` (entity-change audit, #1778); module-scoped authorization or module-scoped stores (#753 covers module activation audit); the tenant gate (#798).

**Affected packages**: `Encina.Security.Audit`, `Encina.Security.ABAC`, `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.Audit.Marten`. **Provider category**: Database (10 providers) plus Marten.

---

## Design Choices

<details>
<summary><strong>1. Shape: a first-class nullable `string? ModuleId` column and query filter, named after the existing identifiers</strong></summary>

**Class:** Settled
**Settled by:** the maintainer's comment on [#1636](https://github.com/dlrivada/Encina/issues/1636) "Settled by elimination (2026-10-17)" (first-class `ModuleId` on entry and query, on all 10 providers plus Marten and in-memory), ADR-036 (names), `AGENTS.md` section 6 function 11 (`ModuleId`).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `string? ModuleId` on the entry and the query, an indexed column or field on every store** | Server-side filter; mirrors `TenantId`; no join | Schema change on 10 providers and Marten |
| **B) Keep it in `Metadata`** | No schema change | Not queryable server-side (the issue's reason to exist) |
| **C) A separate store per module** | Hard separation | Heavy; breaks the shared-store model (settled in the comment) |

### Chosen Option: **A — A nullable `ModuleId` property and column**

### Rationale

- Module isolation is cross-cutting function 11, and the audit trail must be filterable by it.
- `string?` matches `TenantId`, `ABACDecisionAuditRecord.ModuleId` and `BreachAggregate.ModuleId`; the column is `VARCHAR(128) NULL` like `TenantId`, with a filtered index `WHERE ModuleId IS NOT NULL`.
- Matching is exact (as for `TenantId`), not case-insensitive like `EntityType`.

</details>

<details>
<summary><strong>2. Where the value comes from: the request context module name</strong></summary>

**Class:** Trivial
**Settled by:** `ABACDecisionRecordFactory.cs:57` (`ModuleId = inputs.Context.GetModuleName()`) and `RequestContextModuleExtensions.GetModuleName` (`src/Encina/Modules/RequestContextModuleExtensions.cs`).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `context.GetModuleName()` in `DefaultOperationAuditEntryFactory`, `AuditedRepository` and `AuditedReadOnlyRepository`** | One existing extension; null when no module is set (no cost for non-modular apps) | Depends on the module behavior having run before the audit behavior |
| **B) Resolve through `IModuleHandlerRegistry.GetModuleName(handlerType)` inside the factory** | Independent of pipeline order | The factory has no handler type; a second lookup path |
| **C) `IModuleExecutionContext`** | Typed | Scoped service; the entry factory is a singleton |

### Chosen Option: **A — The request context metadata key**

### Rationale

- The ordering dependency is the existing one of the ABAC decision audit and is covered by the pipeline stage contract (#2184).

</details>

<details>
<summary><strong>3. Which audit stores get the module: operation audit only, or the read audit trail as well</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `OperationAuditEntry` and `OperationAuditQuery` only (the issue text)** | Smaller (about 31 production files, three pull requests) | The read audit trail still cannot be filtered by module, so #748's closure note ("covered by #1636") is not true; two stores with different capabilities |
| **B) Both operation and read audit: `ReadAuditEntry`, `ReadAuditQuery`, `ReadAuditEntryEntity`, the mapper, scripts `020` x6, 8 read store classes, `MartenReadAuditStore`, `InMemoryReadAuditStore`** | Coherent across the two purpose-named stores (ADR-036); honours the closure of #748 | About 28 more production files and two more pull requests |
| **C) Operation audit now, read audit as a separate issue** | Bounded now | Leaves the same provider matrix to be touched twice; the read audit comment on this issue becomes a promise |

### Chosen Option: **B — Both stores, the read audit in its own pull requests** (recommended, pending the maintainer)

### Rationale

- B wins when module-scoped access evidence matters (a module owner asking who read their entities), which is the point of function 11 in a modular monolith; A wins when the read trail is not used by module owners in 1.0 and the budget is tight.
- Phases are ordered so that the operation audit ships complete before the read audit starts; the maintainer can stop after Phase 6 (the plan's PRs 1-3) and open a follow-up issue for the rest without rework.

</details>

<details>
<summary><strong>4. The ABAC decision audit: the column replaces the metadata key, with no compatibility copy</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (no compatibility layers or migration helpers) and section 1 (pre-1.0, breaking changes are done completely); `abac-decision-audit-implementation-plan-751.md` (the metadata key was explicitly a stand-in "until this lands", per the issue's Related Issues).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Write the column; delete `ABACDecisionAuditSchema.MetadataModuleId`; the reader maps the column to `ABACDecisionAuditRecord.ModuleId`; add `ABACDecisionAuditQuery.ModuleId`** | One representation | Old rows written before the change lose their module on read (no installed base: pre-1.0) |
| **B) Write both the column and the metadata key** | Old readers keep working | A compatibility layer and a second source of truth |

### Chosen Option: **A — Column only**

### Rationale

- There is no existing user (`AGENTS.md` section 1); keeping two representations would be exactly the legacy the rules forbid.

</details>

<details>
<summary><strong>5. Schema delivery and Marten: scripts edited in place; the module is a plaintext structural field</strong></summary>

**Class:** Trivial
**Settled by:** `AGENTS.md` sections 1 and 3 (scripts of a pre-1.0 package are edited, no migration helpers) and `AuditEncryptionScope` in `src/Encina.Audit.Marten/AuditEncryptionScope.cs` (structural fields such as `TenantId` stay plaintext so queries survive crypto-shredding).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Edit `028` and `020` scripts in place; add the field to the Marten event, read model and `AuditEventEncryptor` as plaintext, plus a document index** | Consistent with the `TenantId` precedent | Developers recreate their local tables |
| **B) New `ALTER TABLE` scripts per provider** | Existing tables upgrade | Migration helpers are prohibited; doubles the scripts |
| **C) Encrypt the module with the PII fields** | Hides module names | A module name is not personal data and could not be queried after shredding |

### Chosen Option: **A — In-place scripts and a plaintext structural field**

### Rationale

- A module identifier names code, not a person; the filtered index mirrors `IX_OperationAuditEntries_TenantId`.

</details>

---

## Implementation Phases

### Phase 1: Core Models

> **Goal**: the property on the four types and the length constant.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `src/Encina.Security.Audit/OperationAuditEntry.cs`**: add `public string? ModuleId { get; init; }` after `TenantId` with XML docs ("name of the module that handled the request; null outside a modular monolith").
2. **Modify `OperationAuditQuery.cs`**: add `public string? ModuleId { get; init; }`, the builder field `_moduleId`, `ForModule(string moduleId)` and the assignment in `Build()`.
3. **Modify `ReadAuditEntry.cs`, `ReadAuditEntryEntity.cs`, `ReadAuditEntryMapper.cs`, `ReadAuditQuery.cs`** the same way (Design Choice 3, Phase 6 onwards).
4. **Modify `src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditSchema.cs`**: remove `MetadataModuleId`; add `ModuleIdMaxLength = 128` (the same value as `TenantIdMaxLength`).
5. **PublicAPI** lines via mechanical-fixer.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
- Issue #1636. OperationAuditEntry/OperationAuditQuery are sealed records in src/Encina.Security.Audit; TenantId is the model to copy.

TASK:
Add ModuleId (string?) to the entry and the query (and the read-audit equivalents in the later PR), the builder method ForModule, and the schema length constant.

KEY RULES:
- XML docs on every public member; PublicAPI.Unshipped.txt via mechanical-fixer.
- No [Obsolete], no compatibility shims; delete the metadata constant.

REFERENCE FILES:
- src/Encina.Security.Audit/OperationAuditQuery.cs (TenantId, ForTenant)
- src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditSchema.cs
```

</details>

### Phase 2: Filling the Value & In-Memory Stores

> **Goal**: every writer sets the module; the in-memory stores filter it; the ABAC decision audit uses the column.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `DefaultOperationAuditEntryFactory.cs`** (line ~114): `ModuleId = context.GetModuleName()`.
2. **Modify `AuditedRepository.cs`** (entry at line ~281) and **`AuditedReadOnlyRepository.cs`** (read entry at line ~235, Design Choice 3): set `ModuleId` from `_requestContext.GetModuleName()`.
3. **Modify `InMemoryOperationAuditStore.cs`** (`WhereIf(... query.TenantId ...)`, line ~176): add the module predicate; **`InMemoryReadAuditStore.cs`** (line ~155) likewise.
4. **Modify `ABACDecisionAuditEntryMapper.cs`**: write `ModuleId = bounds.Identifier(nameof(OperationAuditEntry.ModuleId), record.ModuleId, ABACDecisionAuditSchema.ModuleIdMaxLength)` and read `record.ModuleId = entry.ModuleId` (replace the metadata read at line 131 and write at line 187).
5. **Modify `ABACDecisionAuditQuery.cs`** (add `ModuleId`) and **`ABACDecisionAuditReader.ToStoreQuery`**: normalise with `NormalizeIdentifier(query.ModuleId, ModuleIdMaxLength)`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- Phase 1 merged. Writers: DefaultOperationAuditEntryFactory, AuditedRepository, AuditedReadOnlyRepository, ABACDecisionAuditEntryMapper. Readers/filters: InMemory stores, ABACDecisionAuditReader.

TASK:
Fill ModuleId from context.GetModuleName() everywhere an entry is created and filter it in the in-memory stores and the ABAC reader/query. Remove the abac.module_id metadata path.

KEY RULES:
- Unit tests execute the real factory/mapper/store (no reflection-only tests).
- A module name over the length limit is rejected by the same bounds rule as the other identifiers (no truncation of a filter value).

REFERENCE FILES:
- src/Encina.Security.Audit/DefaultOperationAuditEntryFactory.cs
- src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditEntryMapper.cs
- src/Encina/Modules/RequestContextModuleExtensions.cs
```

</details>

### Phase 3: Persistence — SQL Scripts, EF Entity & Mongo Document

> **Goal**: the column, index and field exist.

<details>
<summary><strong>Tasks</strong></summary>

1. **SQL scripts** `028_CreateOperationAuditEntriesTable.sql` in `Encina.ADO.{SqlServer,PostgreSQL,MySQL}` and `Encina.Dapper.{SqlServer,PostgreSQL,MySQL}`: add `ModuleId VARCHAR(128) NULL` after `TenantId` and a filtered/partial index `IX_OperationAuditEntries_ModuleId` (`WHERE ModuleId IS NOT NULL`; MySQL has no partial index, use a plain index as `IX_OperationAuditEntries_TenantId` does there).
2. **EF Core**: `OperationAuditEntryEntity.cs` (property), `OperationAuditEntryEntityConfiguration.cs` (max length 128, filtered index via `IndexFilters.IsNotNull`), `OperationAuditStoreEF.cs` mapping at lines ~309 and ~334.
3. **MongoDB**: `OperationAuditEntryDocument.cs` (field, `FromEntry`/`ToEntry` at lines ~155 and ~182) and an index where the collection indexes are created.
4. The read audit counterparts (`020_CreateReadAuditEntriesTable.sql` x6, `ReadAuditEntryEntityConfiguration.cs`, `ReadAuditEntryDocument.cs`) belong to PR 4-5 (Design Choice 3).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- Every store already maps TenantId; ModuleId follows it column by column. Scripts are edited in place (pre-1.0).

TASK:
Add the column, the index and the entity/document mapping for the operation audit on all providers except Marten.

KEY RULES:
- SQL dialects: PostgreSQL quoted case-sensitive identifiers; SQL Server filtered index; MySQL backticks, plain index.
- No ALTER scripts, no migration helpers.

REFERENCE FILES:
- src/Encina.ADO.PostgreSQL/Scripts/028_CreateOperationAuditEntriesTable.sql
- src/Encina.EntityFrameworkCore/Auditing/OperationAuditEntryEntityConfiguration.cs
- src/Encina.MongoDB/Auditing/OperationAuditEntryDocument.cs
```

</details>

### Phase 4: Multi-Provider Store Implementations (10 database providers)

> **Goal**: persist and filter the module.

<details>
<summary><strong>Tasks</strong></summary>

| Provider | File | Change |
|----------|------|--------|
| ADO-SqlServer | `src/Encina.ADO.SqlServer/Auditing/OperationAuditStoreADO.cs` | add `ModuleId` to the INSERT column list and values, the four `SELECT` lists, `AddParameter("@ModuleId")`, the reader mapping and `AddText(whereClause, command, "ModuleId", query.ModuleId)` |
| ADO-PostgreSQL | `src/Encina.ADO.PostgreSQL/Auditing/OperationAuditStoreADO.cs` | same, quoted identifiers (the `TenantId` hits are at lines 62-119, 323, 382, 444) |
| ADO-MySQL | `src/Encina.ADO.MySQL/Auditing/OperationAuditStoreADO.cs` | same |
| Dapper-SqlServer / PostgreSQL / MySQL | `src/Encina.Dapper.*/Auditing/OperationAuditStoreDapper.cs` | same with the Dapper parameter object |
| EFCore-SqlServer / PostgreSQL / MySQL | `src/Encina.EntityFrameworkCore/Auditing/OperationAuditStoreEF.cs` (one class) | `WhereIf(filtered, !string.IsNullOrWhiteSpace(query.ModuleId), e => e.ModuleId == query.ModuleId)` (line ~209) |
| MongoDB | `src/Encina.MongoDB/Auditing/OperationAuditStoreMongoDB.cs` | `AddIf(filters, ..., () => builder.Eq(d => d.ModuleId, query.ModuleId))` (line ~280) |

Every provider changes in the same pull request group (`AGENTS.md` section 5, provider coherence).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
- Phase 3 columns exist. Each store lists TenantId in the INSERT, four SELECTs and BuildWhere; ModuleId is added at exactly those places.

TASK:
Implement persistence and filtering of ModuleId in the 8 store classes (10 providers). One provider per commit; run its unit and guard tests before the next.

KEY RULES:
- Parameterised SQL only; asynchronous calls with CancellationToken.
- Integration tests use shared [Collection] fixtures and ClearAllDataAsync.

REFERENCE FILES:
- src/Encina.ADO.PostgreSQL/Auditing/OperationAuditStoreADO.cs
- src/Encina.Dapper.PostgreSQL/Auditing/OperationAuditStoreDapper.cs
- tests/Encina.IntegrationTests/Security/Audit/OperationAuditStoreADOPostgreSqlIntegrationTests.cs
```

</details>

### Phase 5: Marten Audit Store

> **Goal**: the event, read model, projection, encryptor and store carry the module.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Events/OperationAuditEntryRecordedEvent.cs`**: `public string? ModuleId { get; init; }` (plaintext, structural).
2. **`Projections/OperationAuditEntryReadModel.cs`** and **`OperationAuditEntryProjection.cs`**: property and copy.
3. **`Projections/ConfigureMartenOperationAuditProjections.cs`** (line ~105): `.Index(x => x.ModuleId)`.
4. **`AuditEventEncryptor.cs`** (lines ~112 and ~184): copy `ModuleId` in both directions.
5. **`MartenOperationAuditStore.cs`**: filter in `ApplyUserTenantAndTypeFilters` (line ~420) and map back (line ~560).
6. Read audit counterparts in PR 4 (`ReadAuditEntryRecordedEvent`, `ReadAuditEntryReadModel`, `ReadAuditEntryProjection`, `MartenReadAuditStore`).
7. Update the `AuditEncryptionScope` documentation: `ModuleId` is a structural field.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
- Encina.Audit.Marten is event sourced; TenantId is a plaintext structural field with an index on the read model.

TASK:
Mirror every TenantId touch point for ModuleId in the operation audit part of Encina.Audit.Marten.

KEY RULES:
- Structural fields stay plaintext so queries survive crypto-shredding (AuditEncryptionScope).
- Integration tests run on PostgreSQL through Testcontainers.

REFERENCE FILES:
- src/Encina.Audit.Marten/MartenOperationAuditStore.cs
- src/Encina.Audit.Marten/AuditEventEncryptor.cs
- tests/Encina.IntegrationTests/Infrastructure/Marten/Audit/MartenOperationAuditStoreUtcFilterIntegrationTests.cs
```

</details>

### Phase 6: The Read Audit Trail (Design Choice 3, option B)

> **Goal**: the same capability for `IReadAuditStore`. Skipped if the maintainer picks option A.

<details>
<summary><strong>Tasks</strong></summary>

1. Core: `ReadAuditEntry`, `ReadAuditEntryEntity`, `ReadAuditEntryMapper`, `ReadAuditQuery` (done in Phase 1), `InMemoryReadAuditStore` and the entry creation in `AuditedReadOnlyRepository` (Phase 2).
2. Scripts `020_CreateReadAuditEntriesTable.sql` x6; `ReadAuditStoreADO` x3, `ReadAuditStoreDapper` x3 (column in INSERT/SELECT, `if (!string.IsNullOrWhiteSpace(query.ModuleId))` in the filter, as the `TenantId` filter at `ReadAuditStoreADO.cs:293`), `ReadAuditStoreEF` (line ~170) with `ReadAuditEntryEntityConfiguration`, `ReadAuditEntryDocument` and `ReadAuditStoreMongoDB` (line ~207).
3. Marten: `ReadAuditEntryRecordedEvent`, `ReadAuditEntryReadModel`, `ReadAuditEntryProjection`, `ConfigureMartenOperationAuditProjections` index, `MartenReadAuditStore` (line ~306).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
- Phases 1-5 are merged for the operation audit. The read audit trail mirrors it file for file.

TASK:
Repeat Phases 1-5 for the read audit trail.

KEY RULES:
- Same provider coherence rule; all providers in the group of pull requests 4 and 5.
- Contract tests extend ReadAuditContractTests to include the module filter.

REFERENCE FILES:
- src/Encina.ADO.PostgreSQL/Auditing/ReadAuditStoreADO.cs
- src/Encina.MongoDB/Auditing/ReadAuditStoreMongoDB.cs
- tests/Encina.ContractTests/Security/Audit/ReadAudit/ReadAuditContractTests.cs
```

</details>

### Phase 7: Observability

> **Goal**: confirm that no new instrument is needed and keep the existing ones correct.

<details>
<summary><strong>Tasks</strong></summary>

1. `InstrumentedOperationAuditStore` (`src/Encina.OpenTelemetry/Audit/InstrumentedOperationAuditStore.cs`) forwards the query object unchanged: a unit test proves the module filter reaches the inner store.
2. No new `[LoggerMessage]`, metric or span: the feature adds no operation. No EventId range is touched.
3. If the maintainer later wants a module tag on audit metrics, it follows ADR-054 D3/D4 conventions in its own issue.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
- The instrumented decorator wraps IOperationAuditStore and passes queries through.

TASK:
Add the pass-through unit test for ModuleId. Add nothing else to observability.

KEY RULES:
- No unregistered EventIds; none are needed.

REFERENCE FILES:
- src/Encina.OpenTelemetry/Audit/InstrumentedOperationAuditStore.cs
```

</details>

### Phase 8: Testing — 7 Test Types

> **Goal**: per-flag targets met; module filtering proven on every store.

<details>
<summary><strong>Tasks</strong></summary>

| Test type | Scope | Location |
|-----------|-------|----------|
| Unit | factory fill, in-memory filter, ABAC mapper/reader/query, builder `ForModule`, Marten encryptor copy | `tests/Encina.UnitTests/Security/Audit/`, `.../Security/ABAC/` |
| Guard | `ForModule(null)`, new public members | `tests/Encina.GuardTests/Security/` |
| Contract | module filter returns exactly the entries of the module, for every store; extends `OperationAuditStoreContractTests.cs` and `ReadAuditContractTests.cs` | `tests/Encina.ContractTests/Security/Audit/` |
| Property | for any entries and any module, `QueryAsync(ModuleId = m)` equals the in-memory filter; no entry of another module | `tests/Encina.PropertyTests/Security/Audit/OperationAuditStorePropertyTests.cs` (extend) |
| Integration | write entries of modules A and B (and none), filter by A; 10 providers plus Marten (shared `[Collection]` fixtures) | `tests/Encina.IntegrationTests/Security/Audit/` (extend each existing `OperationAuditStore*IntegrationTests`) |
| Load | justification `.md`: append-only, one extra predicate | `tests/Encina.LoadTests/Security/Audit/ModuleId.md` |
| Benchmark | justification `.md` | `tests/Encina.BenchmarkTests/Security/Audit/ModuleId.md` |

**Per-file targets** (proposed; the pull request measures and never lowers):

| File | Flags | Justification |
|------|-------|---------------|
| `OperationAuditEntry.cs` (existing unit 95, guard 90) | unit 95, guard 90 | one more init property |
| `OperationAuditQuery.cs` (existing unit 95, guard 0) | unit 97, guard 0 | the builder method is pure and covered by unit; no guard parameter |
| `DefaultOperationAuditEntryFactory.cs` (existing unit 100, guard 90) | unit 100, guard 90 | one line in a fully covered method |
| `InMemoryOperationAuditStore.cs` (existing unit 97) | unit 97 | one predicate |
| the 8 provider store classes | integration 80 | only integration reaches the SQL and the Mongo filter |
| `MartenOperationAuditStore.cs` | integration 70 | Marten filter proven on PostgreSQL |

CRAP gate: the SQL builders gain one branch each; keep each changed method at or below 10 by extracting the predicate into the existing helper.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT:
- Per-flag coverage (AGENTS.md section 9); tests execute real code; Shouldly via Encina.Testing.Shouldly; shared Collection fixtures.

TASK:
Write the tests of the table, extending the existing OperationAuditStore*IntegrationTests classes. Update the coverage manifests through mechanical-fixer with locally measured targets.

KEY RULES:
- No FluentAssertions, no Thread.Sleep, test data with builders.
- Justification .md files only for load and benchmark.

REFERENCE FILES:
- tests/Encina.ContractTests/Security/Audit/OperationAuditStoreContractTests.cs
- tests/Encina.PropertyTests/Security/Audit/OperationAuditStorePropertyTests.cs
- tests/Encina.IntegrationTests/Security/Audit/OperationAuditStoreEFCore (EFCore folder)
```

</details>

### Phase 9: Documentation & Finalization

> **Goal**: documents, public API, changelog.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on all new public API.
2. `changelog.d/1636-audit-module-id.added.md` and `changelog.d/1636-abac-audit-module-column.changed.md` (mechanical-fixer).
3. `docs/features/audit-tracking.md` (the issue's documentation item) and the package READMEs of `Encina.Security.Audit`, `Encina.Security.ABAC` and `Encina.Audit.Marten` (docs-writer).
4. `PublicAPI.Unshipped.txt` for `Encina.Security.Audit` and `Encina.Security.ABAC`.
5. Build `dotnet build Encina.slnx --configuration Release` with 0 warnings; every flag at target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```text
CONTEXT:
- Code phases are merged; documentation and public API remain.

TASK:
Delegate pages to docs-writer and fragments, PublicAPI and manifest lines to mechanical-fixer; verify the build.

KEY RULES:
- Never edit CHANGELOG.md; no hand-typed coverage figures in docs.

REFERENCE FILES:
- changelog.d/README.md
- docs/features/audit-tracking.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Requirement | Use in this feature |
|--------|-------------|---------------------|
| ADR-036 | purpose-named audit stores | names and which stores are in scope |
| ADR-018 and `AGENTS.md` section 6 function 11 | module isolation is a cross-cutting function | motivation |
| SPEC-002 REQ-007, REQ-008 | read and write audit | the read trail in Design Choice 3 |
| Maintainer comment on #1636 (2026-10-17) | first-class `ModuleId`, all stores | Design Choice 1 |
| #748 (closed as a duplicate) | read-audit module filter must be covered here | Design Choice 3 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `TenantId` in every audit store (model to copy) | `src/Encina.Security.Audit/OperationAuditEntry.cs:86`, `OperationAuditQuery.cs:49`, the 8 store classes, Marten read model | each touch point is repeated for the module |
| `RequestContextModuleExtensions.GetModuleName` | `src/Encina/Modules/RequestContextModuleExtensions.cs` | source of the value |
| `DefaultOperationAuditEntryFactory`, `AuditedRepository`, `AuditedReadOnlyRepository` | `src/Encina.Security.Audit/` | entry creation sites |
| `ABACDecisionAuditEntryMapper`, `ABACDecisionAuditReader`, `ABACDecisionAuditQuery` | `src/Encina.Security.ABAC/DecisionAudit/` | the metadata key becomes the column; query gains the filter |
| `IndexFilters.IsNotNull` and the `TenantId` filtered index | `src/Encina.EntityFrameworkCore/Auditing/OperationAuditEntryEntityConfiguration.cs:123-126` | EF index |
| `OperationAuditStoreRegistration` and the nine registrations | `src/Encina.Security.Audit/OperationAuditStoreRegistration.cs` | already correct (the issue's registration bug is fixed) |
| Existing store tests | `tests/Encina.IntegrationTests/Security/Audit/`, `tests/Encina.ContractTests/Security/Audit/` | extended, not duplicated |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | no new `[LoggerMessage]`; no range is touched (Phase 7) |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core (Security.Audit models, factory, repositories, in-memory stores) | 8 | operation audit: entry, query, factory, `AuditedRepository`, in-memory store; read audit adds 5 more |
| ABAC (schema, mapper, query, reader) | 4 | the metadata key becomes the column |
| ADO and Dapper (6 stores plus 6 operation scripts) | 12 | read audit adds 12 more |
| EF Core (entity, configuration, store) and MongoDB (document, store) | 5 | read audit adds 5 more |
| Marten (event, read model, projection, projection configuration, encryptor, store) | 6 | read audit adds 5 more |
| Tests (unit, guard, contract, property, integration, justification .md) | 19 | new and extended |
| Documentation (READMEs, feature page, changelog fragments, PublicAPI) | 7 | docs-writer and mechanical-fixer |
| Total (operation audit about 55, read audit about 28 more) | 85 | six pull requests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina, .NET 10 / C# 14, pre-1.0, ROP (Either<EncinaError, T>). Issue #1636: ModuleId on OperationAuditEntry/OperationAuditQuery (and the read audit trail) on 10 database providers, Marten and in-memory.
- ADR-036 names: IOperationAuditStore, OperationAuditEntry, OperationAuditQuery, IReadAuditStore. The issue's IAuditStore/AuditEntry names are obsolete.

IMPLEMENTATION OVERVIEW:
1. Add ModuleId to entry and query (builder ForModule), schema length constant, remove the abac.module_id metadata path.
2. Fill it from context.GetModuleName() in the factory and the audited repositories; filter it in the in-memory stores and the ABAC reader.
3. Add VARCHAR(128) NULL column and a filtered index to scripts 028 (and 020 for the read trail) in place; EF entity/configuration; Mongo field and index.
4. Persist and filter it in the 8 store classes and in Marten (plaintext structural field).
5. Tests of 7 types; two-module integration test per provider and Marten.
6. Docs, changelog fragments, PublicAPI, manifests via specialists.

KEY PATTERNS:
- Copy every TenantId touch point; exact-match filter; null means "no module".
- Scripts edited in place; no ALTER scripts, migration helpers or [Obsolete].
- Provider coherence: all 10 providers and Marten move together.
- Shared [Collection] fixtures; Shouldly through Encina.Testing.Shouldly.

REFERENCE FILES:
- src/Encina.Security.Audit/OperationAuditQuery.cs
- src/Encina.ADO.PostgreSQL/Auditing/OperationAuditStoreADO.cs
- src/Encina.Audit.Marten/MartenOperationAuditStore.cs
- src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditEntryMapper.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Audit rows are never cached. |
| 2 | OpenTelemetry | ❌ | No new operation; `InstrumentedOperationAuditStore` only forwards the new filter (Phase 7). |
| 3 | Structured Logging | ❌ | No new operation and no new EventId. |
| 4 | Health Checks | ❌ | No new checkable dependency. |
| 5 | Validation | ✅ | Module id length bounded like the other identifiers (`ModuleIdMaxLength`). |
| 6 | Resilience | ❌ | No external call. |
| 7 | Distributed Locks | ❌ | Append-only rows. |
| 8 | Transactions | ❌ | Writes are single rows as today. |
| 9 | Idempotency | ❌ | No new write path. |
| 10 | Multi-Tenancy | ⏭️ | Orthogonal to `TenantId`; the tenant gate on the same stores is #798 (plan `audit-abac-multitenancy-implementation-plan-798.md`). |
| 11 | Module Isolation | ✅ | This feature (function 11 for the audit trail). |
| 12 | Audit Trail | ✅ | This feature changes the audit entries and queries. |

---

## Prerequisites & Dependencies

- **No hard prerequisite.** The registration defect named in the issue is fixed on `main`; ADR-036 renames are merged.
- **Sequencing**: this plan and `audit-abac-multitenancy-implementation-plan-798.md` both edit `OperationAuditQuery.cs`, the audit stores' registrations and tests. Land the PRs of one plan at a time on those files.
- **Soft**: #2184 (pipeline stage order, so the module is set before the audit behavior), #753 and #747 (other module features), #1778 (entity-change audit, out of scope).

---

## Next Steps

1. Maintainer answers Design Choice 3 when work on the issue starts (recorded in `## Maintainer Decisions`).
2. Worker pull requests (each at most 15 files and 1500 lines):
   - **PR 1**: Phases 1-2 for the operation audit (core, factory, repository, in-memory, ABAC mapper/reader/query) plus unit, guard and contract tests: about 14 files.
   - **PR 2**: Phases 3-4 for ADO and Dapper (6 stores, 6 scripts) plus their integration test extensions: 12 files if the six integration extensions go to PR 3.
   - **PR 3**: Phases 3-5 for EF Core, MongoDB and Marten, plus the six ADO/Dapper integration test extensions and the property tests: about 15 files.
   - **PR 4-5** (only with option B): read audit core, EF, Mongo, Marten (PR 4); read audit ADO and Dapper (PR 5).
   - **PR 6**: documentation, ADR note, justification `.md` files.
3. Link this plan from #1636.
