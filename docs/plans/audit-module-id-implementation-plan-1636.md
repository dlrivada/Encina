# Implementation Plan: Queryable `ModuleId` on the audit entries and queries of every audit store

> **Issue**: [#1636](https://github.com/dlrivada/Encina/issues/1636)
> **Type**: Feature
> **Complexity**: High (9 phases, 10 database providers plus Marten and in-memory, about 110 files, fourteen pull requests with Design Choice 3 B, eight with A)
> **Estimated Scope**: ~59 production and script files, ~40 test files, ~10 documentation, manifest and fragment files
> **Related plans**: #751 (stores the module as metadata today), #800, #798 (edits the same audit files; sequence them, see Prerequisites)

---

## Summary

Add a first-class `ModuleId` to the audit rows and the audit queries so a modular monolith can ask an audit store "what did module X do" server-side, on every provider.

**State of the code on `c49b987e9` (corrects the issue text).**

- The issue speaks of `AuditEntry`, `AuditQuery` and `IAuditStore` and of an "`IAuditStore` registration bug (ADO/Dapper/MongoDB)". ADR-036 renamed them: the row is `OperationAuditEntry` (`src/Encina.Security.Audit/OperationAuditEntry.cs`, `TenantId` at line 86), the query is `OperationAuditQuery` (`OperationAuditQuery.cs`), the port is `IOperationAuditStore`. The registration bug is fixed: all nine provider registrations now call `TryAddScoped<IOperationAuditStore, ...>` after `OperationAuditStoreRegistration.RemoveInMemoryDefault`.
- Nothing carries a module on an audit row. The ABAC decision audit writes the module only as the metadata key `abac.module_id` (`ABACDecisionAuditSchema.MetadataModuleId`, filled from `context.GetModuleName()` in `ABACDecisionRecordFactory.cs:57`, read back in `ABACDecisionAuditEntryMapper.cs:131`), which no store can filter. The module name lives in the request context metadata key `Encina.ModuleName` (`src/Encina/Modules/RequestContextModuleExtensions.cs`).
- Every store already persists and filters `TenantId` the way a module id would be: ADO and Dapper (columns in `028_CreateOperationAuditEntriesTable.sql` x6, filter in `BuildWhere`), EF Core (`OperationAuditEntryEntity`, `OperationAuditEntryEntityConfiguration` with a filtered index, `OperationAuditStoreEF`), MongoDB (`OperationAuditEntryDocument`, `OperationAuditStoreMongoDB`), Marten (plaintext structural field on `OperationAuditEntryRecordedEvent`, `OperationAuditEntryReadModel`, an index in `ConfigureMartenOperationAuditProjections`, `AuditEventEncryptor` copies), and `InMemoryOperationAuditStore`.
- Bounds today: the ABAC decision audit bounds identifiers through `ABACDecisionAuditEntryMapper.NormalizeIdentifier` (internal), which keeps a value that fits and replaces a longer one by `sha256:<64 hex>`, recording the field as hashed (`EntryBounds.Identifier`, lines 252-261); it never rejects. The other writers (`DefaultOperationAuditEntryFactory`, `AuditedRepository`, `AuditedReadOnlyRepository`) copy `TenantId` raw with no bound.
- Module names are case-insensitive everywhere in Encina: `ModuleRegistry` uses `StringComparer.OrdinalIgnoreCase`, `ModuleConfiguration` rejects two modules whose names differ only in case, and `RequestContextModuleExtensions` compares with `StringComparison.OrdinalIgnoreCase`. The audit `TenantId` columns use the database default collation (case-insensitive on SQL Server and MySQL, case-sensitive on PostgreSQL and MongoDB).
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
- Matching rules (case, blank filter) are Design Choice 7; the length rule is Design Choice 6.

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
| **A) `OperationAuditEntry` and `OperationAuditQuery` only (the issue text)** | Smaller (about 31 production files, eight pull requests) | The read audit trail still cannot be filtered by module, so #748's closure note ("covered by #1636") is not true; two stores with different capabilities |
| **B) Both operation and read audit: `ReadAuditEntry`, `ReadAuditQuery`, `ReadAuditEntryEntity`, the mapper, scripts `020` x6, 8 read store classes, `MartenReadAuditStore`, `InMemoryReadAuditStore`** | Coherent across the two purpose-named stores (ADR-036); honours the closure of #748 | About 28 more production files and six more pull requests |
| **C) Operation audit now, read audit as a separate issue** | Bounded now | Leaves the same provider matrix to be touched twice; the read audit comment on this issue becomes a promise |

### Chosen Option: **B — Both stores, the read audit in its own pull requests** (recommended, pending the maintainer)

### Rationale

- B wins when module-scoped access evidence matters (a module owner asking who read their entities), which is the point of function 11 in a modular monolith; A wins when the read trail is not used by module owners in 1.0 and the budget is tight.
- Pull requests are ordered so that the operation audit ships complete (PRs 1-7) before the read audit starts (PRs 8-13); the maintainer can stop after PR 7 and open a follow-up issue for the rest without rework.

</details>

<details>
<summary><strong>4. The ABAC decision audit: the column replaces the metadata key, with no compatibility copy</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (no compatibility layers or migration helpers) and section 1 (pre-1.0, breaking changes are done completely); the #751 plan (the metadata key was explicitly a stand-in "until this lands", per the issue's Related Issues).

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

<details>
<summary><strong>6. A module name longer than the column: hashed, or refused</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

The column is `VARCHAR(128)`. Today the ABAC writer hashes an over-long identifier and the other writers apply no bound, so a module name over 128 characters would make their `INSERT` fail and lose the audit row. Either option below closes that; they differ in what the caller sees.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Hash in every writer and in the filter: a shared `AuditIdentifierBounds.Normalize(value, maxLength)` in `Encina.Security.Audit` (the rule of `NormalizeIdentifier`, moved there and reused by the ABAC mapper) stores `sha256:<64 hex>` for a longer name; the query filter is normalised the same way, so filtering by the long name still matches** | The audit row is never lost; one rule for every writer; the ABAC behavior is unchanged | The stored value of a long name is a hash, readable only by hashing the candidate name |
| **B) Refuse at entry creation: the factories return `Left(encina.audit.module_id_too_long, ErrorCategory.Internal)` (ADR-062 R1: the module name is configuration, not caller input)** | Every stored module id is readable | The audited operation fails (or, where the audit write is best effort, the row is lost) because of a configuration value; the ABAC writer would have to change from hashing to refusing |

### Chosen Option: **A — Hash in every writer and in the filter** (recommended, pending the maintainer)

### Rationale

- A wins where the audit row must exist whatever the configuration (the audit trail is evidence); B wins where a readable module id matters more than the row, which a configuration check at start-up can achieve without failing operations (module names are known at registration). Under A the hash is computed over the lower-invariant name, so the case rule of Design Choice 7 survives hashing.

</details>

<details>
<summary><strong>7. Matching: case and blank filter values</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

Common to both options: a null, empty or whitespace-only `ModuleId` filter is "no filter" and returns every module (the blank-is-no-value rule of #3147), registered here because it is visible API, and tested on every store. Exact case-sensitive matching is excluded: Encina module names are case-insensitive (`ModuleRegistry`, `ModuleConfiguration`), so `Billing` and `billing` are the same module and must match the same rows.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Store the name as written and match case-insensitively on every provider: SQL Server and MySQL default collations (already case-insensitive), PostgreSQL `lower("ModuleId") = lower(@ModuleId)` with an index on `lower("ModuleId")`, MongoDB an index and query with a case-insensitive collation (strength 2), Marten `ModuleId.ToLower()` on an index of the lowered value, in-memory `StringComparison.OrdinalIgnoreCase`** | The row shows the module name as the application wrote it | Four different mechanisms to keep consistent; the PostgreSQL and Mongo indexes are expression or collation indexes |
| **B) Normalise to lower-invariant at write and at filter time on every provider; plain equality and plain indexes everywhere** | One rule, plain indexes, identical on every provider | The stored value is lower-cased (`billing`), not the registered spelling |

### Chosen Option: **B — Lower-invariant at write and filter** (recommended, pending the maintainer)

### Rationale

- B wins when uniform behavior across 10 providers and Marten matters more than the display form, which is the usual trade in this codebase (provider coherence, `AGENTS.md` section 5); A wins when operators read raw audit rows and expect the registered spelling. Each store's integration test filters `Billing` against rows written as `billing` and the reverse.

</details>

---

## Implementation Phases

### Phase 1: Core Models

> **Goal**: the property on the four types and the length constant.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `src/Encina.Security.Audit/OperationAuditEntry.cs`** (PR 1): add `public string? ModuleId { get; init; }` after `TenantId` with XML docs ("name of the module that handled the request, lower-invariant under Design Choice 7 B; null outside a modular monolith").
2. **Modify `OperationAuditQuery.cs`** (PR 1): add `public string? ModuleId { get; init; }`, the builder field `_moduleId`, `ForModule(string moduleId)` (no null guard, the same shape as `ForTenant`) and the assignment in `Build()`.
3. **Create `src/Encina.Security.Audit/AuditIdentifierBounds.cs`** (PR 1, `internal static`): `Normalize(string? value, int maxLength)` with the rule of Design Choice 6 and `NormalizeModuleId(string? value)` (blank to null, the Design Choice 7 case rule, then the length rule with 128). `OperationAuditEntry.ModuleId` and `OperationAuditQuery.ModuleId` apply it in their `init` accessor (C# 14 `field` keyword), so every writer, every filter and every provider store sees the same normalised value without calling the helper; a unit test assigns a blank, a mixed-case and a 200-character name to both and asserts the stored value.
4. **PR 8 only, Design Choice 3 option B**: modify `ReadAuditEntry.cs`, `ReadAuditEntryEntity.cs`, `ReadAuditEntryMapper.cs`, `ReadAuditQuery.cs` the same way.
5. **Modify `src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditSchema.cs`** (PR 7): remove `MetadataModuleId`; add `ModuleIdMaxLength = 128` (the same value as `TenantIdMaxLength`); the ABAC mapper's `NormalizeIdentifier` delegates to `AuditIdentifierBounds.Normalize`.
6. **PublicAPI** (mechanical-fixer): add the new members in the pull request that adds them; in PR 7 **delete** the line `const Encina.Security.ABAC.DecisionAudit.ABACDecisionAuditSchema.MetadataModuleId = "abac.module_id" -> string!` from `src/Encina.Security.ABAC/PublicAPI.Unshipped.txt` (line 974 on `c49b987e9`; it is unshipped, so the line is removed, not moved to a removed section).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
- Issue #1636. OperationAuditEntry/OperationAuditQuery are sealed records in src/Encina.Security.Audit; TenantId is the model to copy.

TASK:
PR 1: add ModuleId (string?) to the entry and the query, the builder method ForModule, AuditIdentifierBounds, and the in-memory filter. No writer fills the value yet. The read-audit equivalents are PR 8; the ABAC schema constant is PR 7.

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

1. **Modify `DefaultOperationAuditEntryFactory.cs`** (line ~114, PR 6): `ModuleId = context.GetModuleName()` (normalised by the property, Phase 1 task 3).
2. **Modify `AuditedRepository.cs`** (entry at line ~281, PR 6) and **`AuditedReadOnlyRepository.cs`** (read entry at line ~235, PR 13, Design Choice 3 B): the same from `_requestContext.GetModuleName()`.
3. **Modify `InMemoryOperationAuditStore.cs`** (`WhereIf(... query.TenantId ...)`, line ~176, PR 1): add the module predicate on the normalised filter value (a blank filter is ignored); **`InMemoryReadAuditStore.cs`** (line ~155, PR 8) likewise.
4. **Modify `ABACDecisionAuditEntryMapper.cs`** (PR 7): write `ModuleId = bounds.Identifier(nameof(OperationAuditEntry.ModuleId), record.ModuleId, ABACDecisionAuditSchema.ModuleIdMaxLength)` (the hashed field is recorded as today) and read `record.ModuleId = entry.ModuleId` (replace the metadata read at line 131 and write at line 187).
5. **Modify `ABACDecisionAuditQuery.cs`** (add `ModuleId`) and **`ABACDecisionAuditReader.ToStoreQuery`** (PR 7): pass `query.ModuleId` to `OperationAuditQuery.ModuleId`, whose `init` applies the same rule as the writers, so the filter value matches what was stored; the ABAC mapper's own `NormalizeIdentifier` delegates to `AuditIdentifierBounds.Normalize` (`Encina.Security.Audit` grants `InternalsVisibleTo` to `Encina.Security.ABAC`, or the helper is made public with XML docs if that grant does not exist).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- Phase 1 merged. Writers: DefaultOperationAuditEntryFactory, AuditedRepository, AuditedReadOnlyRepository, ABACDecisionAuditEntryMapper. Readers/filters: InMemory stores, ABACDecisionAuditReader.

TASK:
PR 6: fill ModuleId from context.GetModuleName() in the factory and AuditedRepository (the property normalises it). PR 7: move the ABAC decision audit from the abac.module_id metadata key to the column and add the ABAC query filter. Both only after every operation-audit store persists and filters the column (PRs 2-5).

KEY RULES:
- Unit tests execute the real factory/mapper/store (no reflection-only tests).
- A module name over 128 characters follows Design Choice 6 (A: hashed by AuditIdentifierBounds in every writer and in every filter, never truncated; B: refused with a Left); a blank filter is ignored; the case rule is Design Choice 7.

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
4. The read audit counterparts (`020_CreateReadAuditEntriesTable.sql` x6, `ReadAuditEntryEntityConfiguration.cs`, `ReadAuditEntryDocument.cs`) belong to PRs 9 to 11 (Design Choice 3 B).
5. Index shape follows Design Choice 7: under B (recommended) a plain or filtered index on `ModuleId` as for `TenantId`; under A an expression index on `lower("ModuleId")` on PostgreSQL and a case-insensitive collation index on MongoDB.

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

Every provider changes in the same pull request group (`AGENTS.md` section 5, provider coherence): PR 2 ADO.NET, PR 3 Dapper, PR 4 EF Core and MongoDB, PR 5 Marten, before any writer fills the column (Next Steps, Order). Every store treats a null filter as "no filter"; the value reaching the store is already normalised by `OperationAuditQuery.ModuleId` (blank to null, Design Choices 6 and 7), and each provider's integration test covers the module filter, a blank filter, and `Billing` against `billing`.

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
6. Read audit counterparts in PR 12 (`ReadAuditEntryRecordedEvent`, `ReadAuditEntryReadModel`, `ReadAuditEntryProjection`, `MartenReadAuditStore`).
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
- Same provider coherence rule and order: stores first (PRs 8-12), the AuditedReadOnlyRepository writer last (PR 13).
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
| Unit | factory fill, in-memory filter (module, blank, case), the normalisation of `ModuleId` on entry and query (blank, mixed case, 200 characters), ABAC mapper/reader/query, builder `ForModule`, Marten encryptor copy | `tests/Encina.UnitTests/Security/Audit/`, `.../Security/ABAC/` |
| Guard | new public members that take reference parameters; `ForModule` has no null guard (same shape as `ForTenant`, a null argument means "no filter"), so it is not a guard case | `tests/Encina.GuardTests/Security/` |
| Contract | module filter returns exactly the entries of the module, for every store; extends `OperationAuditStoreContractTests.cs` and `ReadAuditContractTests.cs` | `tests/Encina.ContractTests/Security/Audit/` |
| Property | for any entries and any module, `QueryAsync(ModuleId = m)` equals the in-memory filter; no entry of another module | `tests/Encina.PropertyTests/Security/Audit/OperationAuditStorePropertyTests.cs` (extend) |
| Integration | write entries of modules A and B (and none), filter by A; 10 providers plus Marten (shared `[Collection]` fixtures) | `tests/Encina.IntegrationTests/Security/Audit/` (extend each existing `OperationAuditStore*IntegrationTests`) |
| Load | justification `.md`: append-only, one extra predicate | `tests/Encina.LoadTests/Security/Audit/ModuleId.md` |
| Benchmark | justification `.md` | `tests/Encina.BenchmarkTests/Security/Audit/ModuleId.md` |

**Per-file targets** (proposed; the pull request measures and never lowers). "Baseline" is the per-file target in `.github/coverage-manifest/` on `c49b987e9`; "default" means the file has none and the package default applies (Security.Audit unit 70, guard 20; Audit.Marten unit 40, guard 15; EntityFrameworkCore unit 40, guard 10; MongoDB unit 30, guard 10).

| File | Baseline | Flags | Justification |
|------|----------|-------|---------------|
| `Security.Audit/OperationAuditEntry.cs` (PR 1) | unit 95, guard 90 | unit 95, guard 90 | one more init property with a normalising accessor, asserted by unit tests. |
| `Security.Audit/OperationAuditQuery.cs` (PR 1) | unit 95, guard 0 | unit 97, guard 0 | `ForModule` and the normalising accessor are pure and covered by unit; `ForModule` has no null guard by design (same as `ForTenant`), so guard stays 0. |
| `Security.Audit/AuditIdentifierBounds.cs` (new, PR 1) | new | unit 100, property 90 | pure functions; FsCheck proves idempotence and the 128-character bound. |
| `Security.Audit/InMemoryOperationAuditStore.cs` (PR 1) | unit 97 | unit 97 | one predicate, covered by the module, blank and case tests. |
| `Security.Audit/DefaultOperationAuditEntryFactory.cs` (PR 6) | unit 100, guard 90 | unit 100, guard 90 | one line in a fully covered method. |
| `Security.Audit/AuditedRepository.cs` (PR 6) | unit 100, guard 10, contract 25 | unit 100, guard 10, contract 25 | one assignment in a fully covered entry builder; targets kept. |
| `Security.Audit/AuditedReadOnlyRepository.cs` (PR 13) | unit 100, guard 20, contract 30, property 0 | unchanged | one assignment in a fully covered entry builder. |
| `Security.Audit/ReadAuditEntry.cs`, `ReadAuditQuery.cs`, `ReadAuditEntryEntity.cs`, `ReadAuditEntryMapper.cs` (PR 8) | default | unit 95, guard 80 | records, entity and mapper with one new member each, built and mapped in unit tests; guard covers the mapper's arguments. |
| `Security.Audit/InMemoryReadAuditStore.cs` (PR 8) | unit 99 | unit 99 | one predicate. |
| `Security.ABAC/DecisionAudit/ABACDecisionAuditSchema.cs` (PR 7) | unit 0 | unit 0 | constants only, no executable line. |
| `Security.ABAC/DecisionAudit/ABACDecisionAuditEntryMapper.cs` (PR 7) | unit 95, guard 45, contract 80, property 85, integration 90 | unchanged | the column write and read replace the metadata path in already covered methods. |
| `Security.ABAC/DecisionAudit/ABACDecisionAuditReader.cs` (PR 7) | unit 95, guard 75, contract 55, integration 75 | unchanged | one filter assignment in a covered method. |
| `Security.ABAC/DecisionAudit/ABACDecisionAuditQuery.cs` (PR 7) | unit 95, guard 66 | unchanged | one init property. |
| `ADO.*/Auditing/OperationAuditStoreADO.cs` (3, PR 2) | integration 80 | integration 80 | only integration reaches the SQL; one column and one predicate in covered statements. |
| `Dapper.*/Auditing/OperationAuditStoreDapper.cs` (3, PR 3) | integration 85 | integration 85 | same. |
| `ADO.*/Auditing/ReadAuditStoreADO.cs` (3, PR 9) | integration 87 | integration 87 | same. |
| `Dapper.*/Auditing/ReadAuditStoreDapper.cs` (3, PR 10) | integration 88 | integration 88 | same. |
| `EntityFrameworkCore/Auditing/OperationAuditEntryEntity.cs` (PR 4) | unit 95, guard 0 | unit 95, guard 0 | one property; no guardable parameter. |
| `EntityFrameworkCore/Auditing/OperationAuditEntryEntityConfiguration.cs` (PR 4) | unit 95 | unit 95 | the column and index are asserted on the model. |
| `EntityFrameworkCore/Auditing/OperationAuditStoreEF.cs` (PR 4) | integration 80 | integration 80 | LINQ predicate reaches SQL only in integration. |
| `EntityFrameworkCore/Auditing/ReadAuditEntryEntityConfiguration.cs` (PR 11) | default | unit 95 | the column and index are asserted on the model. |
| `EntityFrameworkCore/Auditing/ReadAuditStoreEF.cs` (PR 11) | integration 75 | integration 75 | same as the operation store. |
| `MongoDB/Auditing/OperationAuditEntryDocument.cs` (PR 4) | unit 80, guard 0 | unit 80, guard 0 | `FromEntry`/`ToEntry` map one more field. |
| `MongoDB/Auditing/OperationAuditStoreMongoDB.cs` (PR 4) | integration 80 | integration 80 | the filter reaches the server only in integration. |
| `MongoDB/Auditing/ReadAuditEntryDocument.cs` (PR 11) | default | unit 90 | mapping both ways in unit tests. |
| `MongoDB/Auditing/ReadAuditStoreMongoDB.cs` (PR 11) | integration 85 | integration 85 | same. |
| `Audit.Marten/Events/OperationAuditEntryRecordedEvent.cs` (PR 5) | unit 95, guard 45 | unit 95, guard 45 | one init property. |
| `Audit.Marten/Projections/OperationAuditEntryReadModel.cs` (PR 5) | unit 95, guard 95 | unit 95, guard 95 | one property. |
| `Audit.Marten/Projections/OperationAuditEntryProjection.cs` (PR 5) | unit 75, guard 10, integration 96 | unchanged | one copy line in a covered method. |
| `Audit.Marten/Projections/ConfigureMartenOperationAuditProjections.cs` (PRs 5, 12) | unit 95, guard 0, integration 100 | unchanged | one index line each. |
| `Audit.Marten/AuditEventEncryptor.cs` (PR 5) | default | unit 90, guard 80 | the copy in both directions is asserted by the encryptor test. |
| `Audit.Marten/AuditEncryptionScope.cs` (PR 5) | default | unit 0 | documentation only in this change; no executable line is touched. |
| `Audit.Marten/MartenOperationAuditStore.cs` (PR 5) | integration 72, unit 50, guard 8 | integration 72, unit 50, guard 8 | the Marten filter is proven on PostgreSQL; targets kept. |
| `Audit.Marten/Events/ReadAuditEntryRecordedEvent.cs` (PR 12) | default | unit 90, guard 40 | one init property, built in unit tests. |
| `Audit.Marten/Projections/ReadAuditEntryReadModel.cs`, `ReadAuditEntryProjection.cs` (PR 12) | unit 100 / guard 93 / integration 100; unit 87 / guard 27 / integration 82 | unchanged | one property and one copy line. |
| `Audit.Marten/MartenReadAuditStore.cs` (PR 12) | integration 66, unit 43, guard 10 | unchanged | one filter in a covered query. |
| the 12 SQL scripts (PRs 2, 3, 9, 10) | not measured | not measured | executed by the integration tests; scripts carry no coverage flag. |

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
| Core (Security.Audit models, bounds helper, factory, repositories, in-memory stores) | 6 | operation audit: entry, query, `AuditIdentifierBounds`, factory, `AuditedRepository`, in-memory store; read audit adds 6 more |
| ABAC (schema, mapper, query, reader) | 4 | the metadata key becomes the column |
| ADO and Dapper (6 stores plus 6 operation scripts) | 12 | read audit adds 12 more |
| EF Core (entity, configuration, store) and MongoDB (document, store) | 5 | read audit adds 4 more |
| Marten (event, read model, projection, projection configuration, encryptor, scope, store) | 7 | read audit adds 4 more |
| Tests (unit, guard, contract, property, integration extensions, justification .md) | 40 | new and extended, each in the pull request of its code |
| Manifests, PublicAPI, changelog fragments, READMEs, feature page | 12 | docs-writer and mechanical-fixer |
| Total (operation audit about 67, read audit about 43 more) | about 110 | fourteen pull requests with Design Choice 3 B, eight with A |

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
- Copy every TenantId touch point; the value is normalised by the ModuleId init accessor (blank to null, case per Design Choice 7, length per Design Choice 6); null or blank filter means "no filter"; null on a row means "no module".
- Order: stores persist and filter first (PRs 1-5), writers and the ABAC move last (PRs 6-7); read audit the same way (PRs 8-13).
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
| 5 | Validation | ✅ | Module id bounded and normalised by `AuditIdentifierBounds` (Design Choices 6 and 7); blank filter is "no filter". |
| 6 | Resilience | ❌ | No external call. |
| 7 | Distributed Locks | ❌ | Append-only rows. |
| 8 | Transactions | ❌ | Writes are single rows as today. |
| 9 | Idempotency | ❌ | No new write path. |
| 10 | Multi-Tenancy | ⏭️ | Orthogonal to `TenantId`; the tenant gate on the same stores is #798. |
| 11 | Module Isolation | ✅ | This feature (function 11 for the audit trail). |
| 12 | Audit Trail | ✅ | This feature changes the audit entries and queries. |

---

## Prerequisites & Dependencies

- **No hard prerequisite.** The registration defect named in the issue is fixed on `main`; ADR-036 renames are merged.
- **Sequencing**: this plan and #798 both edit `OperationAuditQuery.cs`, the audit stores and their tests (#798 PRs 1-2 add the tenant gate and touch the nine audit registrations). Land the pull requests of one plan at a time on those files.
- **Soft**: #2184 (pipeline stage order, so the module is set before the audit behavior), #753 and #747 (other module features), #1778 (entity-change audit, out of scope).

---

## Next Steps

1. Maintainer answers Design Choices 3, 6 and 7 when work on the issue starts (recorded in `## Maintainer Decisions`).
2. **Order** (finding of the review): the stores learn to persist and filter the column first, while no writer fills it; the writers and the ABAC move come last, after every store. Between PR 1 and PR 5 a `ModuleId` filter sent to a store whose pull request has not merged yet is ignored by that store; because no writer sets a module before PR 6, no row has one, no audit data is lost, the ABAC decision rows keep the metadata key until PR 7, and the changelog fragment and the documentation that announce the filter land only with PR 6 (operation audit) and PR 13 (read audit). No pull request ships a writer whose column a store drops.
3. Worker pull requests. Counts include tests, manifest entries, PublicAPI lines and changelog fragments; every one builds alone, carries the tests of its own files and stays within 15 files and 1500 lines. Files whose manifest already has targets that do not change (every store, script owner and entity listed in Phase 8) need no manifest edit.
   - **PR 1** (core): `OperationAuditEntry`, `OperationAuditQuery`, `AuditIdentifierBounds`, `InMemoryOperationAuditStore` (4); unit tests for the properties and builder, the bounds helper and the in-memory filter, the bounds property test, and the guard test (5); manifest, PublicAPI (2): 11 files.
   - **PR 2** (ADO.NET): three `028` scripts, three `OperationAuditStoreADO` (6); the three existing integration classes extended with the module filter, blank filter and case tests (3): 9 files.
   - **PR 3** (Dapper): same shape as PR 2: 9 files.
   - **PR 4** (EF Core and MongoDB): `OperationAuditEntryEntity`, `OperationAuditEntryEntityConfiguration`, `OperationAuditStoreEF`, `OperationAuditEntryDocument`, `OperationAuditStoreMongoDB` (5); the three EF Core integration classes and the MongoDB one (4); the entity unit test (1): 10 files.
   - **PR 5** (Marten): event, read model, projection, projection configuration, `AuditEventEncryptor`, `MartenOperationAuditStore`, `AuditEncryptionScope` docs (7); encryptor unit test, Marten integration test (2); manifest (the encryptor and scope have no targets yet) (1): 10 files.
   - **PR 6** (operation-audit writers): `DefaultOperationAuditEntryFactory`, `AuditedRepository` (2); their unit tests, the `OperationAuditStoreContractTests` and `OperationAuditStorePropertyTests` extensions, the `InstrumentedOperationAuditStore` pass-through test (5); changelog fragment `1636-audit-module-id.added.md` (1): 8 files.
   - **PR 7** (ABAC decision audit): `ABACDecisionAuditSchema`, `ABACDecisionAuditEntryMapper`, `ABACDecisionAuditReader`, `ABACDecisionAuditQuery` (4); mapper and reader unit tests (2); PublicAPI (the `MetadataModuleId` deletion and the new members), changelog fragment `1636-abac-audit-module-column.changed.md` (2): 8 files.
   - **PR 8** (read audit core, Design Choice 3 B only): `ReadAuditEntry`, `ReadAuditEntryEntity`, `ReadAuditEntryMapper`, `ReadAuditQuery`, `InMemoryReadAuditStore` (5); unit and guard tests (3); manifest, PublicAPI (2): 10 files.
   - **PR 9** (read audit ADO.NET): three `020` scripts, three `ReadAuditStoreADO`, three integration extensions: 9 files.
   - **PR 10** (read audit Dapper): same shape: 9 files.
   - **PR 11** (read audit EF Core and MongoDB): `ReadAuditEntryEntityConfiguration`, `ReadAuditStoreEF`, `ReadAuditEntryDocument`, `ReadAuditStoreMongoDB` (4); three EF Core and one MongoDB integration extensions (4); EF Core and MongoDB manifests (the configuration and the document have no targets yet) (2): 10 files.
   - **PR 12** (read audit Marten): `ReadAuditEntryRecordedEvent`, `ReadAuditEntryReadModel`, `ReadAuditEntryProjection`, `MartenReadAuditStore` (4); Marten integration extension (1); manifest (the event has no targets yet) (1): 6 files.
   - **PR 13** (read audit writer): `AuditedReadOnlyRepository`, its unit test, the `ReadAuditContractTests` extension, changelog fragment: 4 files.
   - **PR 14** (documentation): `docs/features/audit-tracking.md`, the READMEs of `Encina.Security.Audit`, `Encina.Security.ABAC` and `Encina.Audit.Marten` (docs-writer), the load and benchmark justification `.md` files: 6 files. No ADR (no design choice here needs one).
   - With Design Choice 3 A, PRs 8 to 13 are dropped and a follow-up issue covers the read audit trail.
4. Link this plan from #1636.
