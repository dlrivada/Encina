# Implementation Plan: Edit History for Processing Activities (RoPA) on the 10 Database Providers

> **Issue**: [#1951](https://github.com/dlrivada/Encina/issues/1951)
> **Type**: Feature
> **Complexity**: High (5 phases across 9 provider-family packages, no new package, about 45 files in 5 pull requests)
> **Estimated Scope**: ~900-1,300 lines of production code + ~2,200-2,800 lines of tests
> **Specification**: GDPR Art. 5(2), Art. 30; SPEC-002 REQ-061, REQ-062 (part of #1186)
> **Depends on**: #1778 (the entity-change audit store of ADR-036) and #1196 (the Art. 9(2) fields whose changes must be auditable)

---

## Summary

Every successful register or update of a processing activity (the controller's Art. 30 record, `IProcessingActivityRegistry`) leaves an entity-change audit entry with the activity id, the tenant, the actor from the request context, the time and the changed fields. A failed audit write fails the change. The entry carries no personal data of data subjects.

**What exists today** (verified on `origin/main`): `IProcessingActivityRegistry` has `RegisterActivityAsync`, `GetAllActivitiesAsync`, `GetActivityByRequestTypeAsync` and `UpdateActivityAsync` (`src/Encina.Compliance.GDPR/Abstractions/IProcessingActivityRegistry.cs`). The update overwrites the row: `InMemoryProcessingActivityRegistry` replaces the dictionary value (`:84-102`) and the database registries run an UPDATE, so only `LastUpdatedAtUtc` remains. Eleven registries exist: `InMemoryProcessingActivityRegistry`, `ProcessingActivityRegistryADO` (SqlServer, PostgreSQL, MySQL), `ProcessingActivityRegistryDapper` (SqlServer, PostgreSQL, MySQL), `ProcessingActivityRegistryEF` (one class for the three EF Core databases, over the application's `DbContext`) and `ProcessingActivityRegistryMongoDB`. The ADO and Dapper registries are singletons built from a connection string and open their own connection per call; the EF registry calls `SaveChangesAsync`. `ProcessingActivity` has no tenant. `Encina.Compliance.GDPR` references only `Encina`; the entity-change store today is `IAuditLogStore` in `Encina.DomainModeling` (`LogAsync` returns `Task`, entry `AuditLogEntry` without tenant), implemented on ADO x3, Dapper x3, EF Core and MongoDB; ADR-036 renames it `IEntityChangeAuditStore` and gives it `Either`, `TenantId`, `ModuleId` and a redaction hook, which is #1778 (open).

**Settled before this plan** (comment on #1951, 2026-10-17): write an awaited entry through the entity-change audit store on every register and update, in the same unit of work, failing the change if the write fails. Moving the RoPA to Marten contradicts SPEC-002 DEC-008 and #681; a history table duplicates the audit stores on 10 providers; a notification would return `Right` with zero handlers (silent loss); audit is mandatory and awaited (#2381 sub-decision 5).

**What this plan delivers**

1. In `Encina.Compliance.GDPR`: `ProcessingActivityChange` (the neutral record of one change), `ProcessingActivityDiff` (a pure, explicit comparison of two `ProcessingActivity` values), `IProcessingActivityChangeRecorder` and an in-memory recorder used by `InMemoryProcessingActivityRegistry`.
2. In each provider package (ADO x3, Dapper x3, EF Core, MongoDB): the registry writes the entity-change entry through its own provider's store path, atomically with the change (Design Choice 1), using the request context for actor and tenant.
3. Telemetry (counter and trace tags), tests on all 10 providers and in memory.

**Out of scope**: the redaction rules and the store contract itself (#1778); the new RoPA fields and their validation (#1196); a read API or UI for the history (the entries are read through the entity-change store's query); retention of the audit entries (the store's options, ADR-036).

**Standards**: GDPR Art. 5(2) accountability, Art. 30 records of processing; SPEC-002 REQ-061, REQ-062, DEC-008; ADR-036 (three audit stores), ADR-019.

**Affected packages**: `Encina.Compliance.GDPR`, `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`.

**Provider category**: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (one class), MongoDB, plus the in-memory default.

---

## Design Choices

One Dilemma is open (1); the maintainer answers it when work on the issue starts. Choices 2 to 6 are Settled or Trivial.

<details>
<summary><strong>1. How the change and its audit entry commit together</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One transaction per provider.** ADO and Dapper open one connection and transaction and write the RoPA row and the entry in it; EF adds the entry to the same `DbContext` before the single `SaveChangesAsync`; MongoDB uses one session transaction. The entity-change store offers a write that joins an existing transaction (a requirement raised to #1778) | Atomic: the change and its record commit together or neither does; no false or missing history | #1778 must expose a transaction-aware write on its 8 provider stores; MongoDB needs a replica set; more provider-specific code |
| **B) Write-ahead.** Write the entry first through the store's plain write, then the change; a failed change leaves an entry that is marked `Attempted` by a following correction entry | Uses only the plain store write; no change to #1778 | The history can show an attempt that never took effect until the correction entry is written; two writes can disagree after a crash; weaker accountability |

### Chosen Option: **A — One transaction per provider** (recommended, pending the maintainer)

### Rationale

- The settled answer says "in the same unit of work"; only A keeps that literally on every provider, and the existing registries already own their connection (ADO, Dapper) or `DbContext` (EF), so the extra code is local to each.
- B wins if #1778 ships without a transaction-aware write and the maintainer wants the history before 1.0 without touching the eight audit stores, accepting the correction-entry model.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Audit store, not notification or history table</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An awaited entry through the entity-change audit store** (ADR-036), failing the change on error | Reuses the audit stores on 10 providers; retention, redaction and query come with the store; no silent loss | Depends on #1778 for the tenant-carrying contract |
| **B) A `ProcessingActivityChanged` notification recorded by the audit pipeline** | Decoupled | Returns `Right` with zero handlers: a silent loss (AGENTS.md section 3) |
| **C) A history table per provider** | Self-contained | A new table on 10 providers duplicating what the audit stores already provide |
| **D) Move processing activities to Marten** | Full history for free | Contradicts SPEC-002 DEC-008 and the 10-provider design of #681 |

### Chosen Option: **A — Entity-change audit store, awaited**

### Rationale

- Chosen by elimination in the issue comment of 2026-10-17; each rejected option has a written reason (silent loss, duplication, DEC-008).

**Class:** Settled
**Settled by:** ADR-036; SPEC-002 DEC-008; #681; #2381 (sub-decision 5); AGENTS.md section 3 (errors are never swallowed)

</details>

<details>
<summary><strong>3. Where the neutral change model and the diff live</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ProcessingActivityChange`, `ProcessingActivityDiff` and `IProcessingActivityChangeRecorder` in `Encina.Compliance.GDPR` (references only `Encina`); each provider package maps the change to its own store entry** | One diff and one test suite for all 11 registries; the core stays free of the audit packages | Eight packages carry a small mapping each |
| **B) The core references `Encina.Security.Audit` and writes through the store** | One writer | Adds a dependency to a package that references only `Encina`; cannot join each provider's transaction |
| **C) Each registry builds its own diff** | No new core types | Eleven copies of the same rule |

### Chosen Option: **A — Neutral model and diff in the GDPR core**

### Rationale

- The registries already live in the provider packages, which reference both the GDPR core and the audit packages; the precedent is the contract-in-core pattern of the compliance applicability resolver (#686 Design Choice 1).
- The diff compares the properties of `ProcessingActivity` explicitly (name, purpose, lawful basis, categories, recipients, third-country transfers, safeguards, retention, security measures, and the fields #1196 adds). A guard test enumerates the public properties of the record by reflection and fails when one is not compared, then executes the diff for each, so a new field cannot be forgotten.

**Class:** Trivial
**Settled by:** AGENTS.md section 5 (provider coherence: abstractions in the shared package, providers differ only in implementation); #686 plan Design Choice 1; `src/Encina.Compliance.GDPR/Encina.Compliance.GDPR.csproj` (references only `Encina`)

</details>

<details>
<summary><strong>4. Actor, tenant, time and what the entry contains</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Actor and tenant from `IRequestContextAccessor` at write time (the actor `UserId` is `null` when there is no principal, for example the start-up scan); time from `TimeProvider`; the entry holds the entity type `ProcessingActivity`, the activity id, the action (created or updated), the names of the changed fields and their old and new values, and the correlation id; no data subject data** | Matches the entry contract (`UserId` is nullable); one clock; tenant per REQ-061 | A start-up registration shows no actor |
| **B) Fail the write when there is no principal** | Every entry has an actor | The start-up scan and background registration would fail; the gate rule applies to gates, not to audit of the controller's own record |

### Chosen Option: **A — Ambient context, nullable actor, changed fields only**

### Rationale

- A RoPA record holds categories and purposes, not data about data subjects, so old and new values of the changed fields are safe; the redaction hook of the store (ADR-036) stays available for any future sensitive field. With tenancy off, a fixed default tenant applies.

**Class:** Trivial
**Settled by:** `src/Encina.DomainModeling/Auditing/AuditLogEntry.cs` (`UserId` is `string?`); SPEC-002 REQ-061; AGENTS.md section 3 (time comes from `TimeProvider`)

</details>

<details>
<summary><strong>5. A missing audit store is a start-up error, there is no opt-out</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A database-backed registry requires a registered entity-change store (and, for EF, the audit entity in the application's model); startup validation fails otherwise; the in-memory registry gets an in-memory recorder by `TryAdd`** | Registration completeness; no silent unaudited RoPA | An application that registers a database registry must also register the store |
| **B) An option `AuditChanges = false`** | Flexible | A silent opt-out of an accountability record |

### Chosen Option: **A — Required store, startup check**

### Rationale

- The database store wins over the in-memory default in any registration order without overriding the application's own registration.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (registration completeness; only an explicit, logged opt-out disables a control); #2381 (audit is mandatory and awaited); GDPR Art. 5(2)

</details>

<details>
<summary><strong>6. Telemetry and logging scope</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A counter `encina.gdpr.ropa.changes` (tags `change`: created, updated, failed; tenant only when multi-tenancy is on) and trace tags through the existing `ProcessingActivityDiagnostics`; no new `[LoggerMessage]`: the registries have no logger by design and return `Left` with code `gdpr.ropa_audit_failed`, which the caller logs by error code** | No constructor changes on eight registries; no new EventId | The issue's matrix proposed an EventId in `ComplianceGDPR`; this plan does not use one |
| **B) Add `ILogger` to the eight registries and log with EventIds 8117-8119 of `ComplianceGDPR`** | Local log line | Constructor and DI changes on eight packages, and those packages map only to their own EventId ranges in the architecture test |

### Chosen Option: **A — Counter and trace, `Left` for the caller to log**

### Rationale

- ADR-054 names the instrument; the error code never carries the exception message (AGENTS.md section 3).

**Class:** Trivial
**Settled by:** ADR-054 settled point 4; AGENTS.md sections 3 and 7; `src/Encina.Compliance.GDPR/Diagnostics/GDPRLogMessages.cs` (ids 8100-8116 used)

</details>

---

## Implementation Phases

Each phase is one pull request of at most 15 files and 1500 changed lines. Phase 0 is the gate: #1778 is merged with the transaction-aware write that Design Choice 1 option A needs (if the maintainer chooses B, the gate is the plain `IEntityChangeAuditStore` of #1778), and #1196 is merged so the diff covers its fields.

### Phase 1: Neutral Model, Diff, Recorder and In-Memory Registry

> **Goal**: The change model and diff exist and the in-memory registry audits. **Size**: about 12 files, 800 lines.

<details>
<summary>Tasks</summary>

Folder `src/Encina.Compliance.GDPR/ProcessingActivityHistory/`, namespace `Encina.Compliance.GDPR`.

1. `ProcessingActivityChange.cs` (public sealed record): `Guid ActivityId`, `string? TenantId`, `string? ActorId`, `ProcessingActivityChangeKind Kind` (public enum `Created`, `Updated`), `IReadOnlyList<ProcessingActivityFieldChange> Fields` (field name, old value, new value as text), `DateTimeOffset ChangedAtUtc`, `string? CorrelationId`.
2. `ProcessingActivityDiff.cs` (public static): `Compare(ProcessingActivity? before, ProcessingActivity after)` returning the field changes; explicit per-property comparison with sequence equality for lists; a no-op update yields an empty list (and no entry).
3. `IProcessingActivityChangeRecorder.cs`: `ValueTask<Either<EncinaError, Unit>> RecordAsync(ProcessingActivityChange change, CancellationToken cancellationToken)`; `InMemoryProcessingActivityChangeRecorder.cs` (bounded list, `TimeProvider`-free, thread-safe) registered by `TryAdd`.
4. `ProcessingActivityChangeFactory.cs` (public static): builds the change from the two values, the ambient `IRequestContext` (actor, tenant, correlation id) and `TimeProvider`.
5. `InMemoryProcessingActivityRegistry` takes the recorder and the accessor (optional constructor parameters with the in-memory defaults) and records on register and update; a recorder `Left` fails the call and the dictionary is not changed.
6. `GDPRErrors` additions: `gdpr.ropa_audit_failed`; `GDPRDiagnostics`: counter `encina.gdpr.ropa.changes`; `ServiceCollectionExtensions.cs` registers the recorder (`TryAdd`); `PublicAPI.Unshipped.txt`.
7. Tests: diff (each property, no-op, lists), factory (actor null, tenant off), recorder, in-memory registry (audit, failed recorder leaves state unchanged), guard tests, contract tests for the registry interface; manifest targets in `.github/coverage-manifest/Encina.Compliance.GDPR.json`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #1951; plan docs/plans/ropa-edit-history-implementation-plan-1951.md. Maintainer Decisions override the "recommended" choices.
- Encina.Compliance.GDPR references only Encina. The 11 registries are listed in the plan Summary.

TASK:
Create the change model, the diff, the recorder abstraction with its in-memory implementation and the factory, wire the in-memory registry,
add the error, the counter, the registration and the tests. At most 15 files and 1500 lines.

KEY RULES:
- Time from TimeProvider; a failed recorder write fails the call and changes nothing; errors are never swallowed.
- No data subject data, payload or exception message in the change, the error or the telemetry.
- A guard test must fail when a public property of ProcessingActivity is not compared by the diff.

REFERENCE FILES:
- src/Encina.Compliance.GDPR/InMemoryProcessingActivityRegistry.cs
- src/Encina.Compliance.GDPR/Abstractions/IProcessingActivityRegistry.cs
- src/Encina.Compliance.GDPR/Diagnostics/GDPRDiagnostics.cs
```

</details>

---

### Phase 2: EF Core and MongoDB Registries

> **Goal**: Two provider families audit atomically. **Size**: about 13 files, 1000 lines.

<details>
<summary>Tasks</summary>

1. `src/Encina.EntityFrameworkCore/ProcessingActivity/ProcessingActivityRegistryEF.cs`: on register and update, load the current row (update), compute the diff, add the entity-change entry to the same `DbContext` and call `SaveChangesAsync` once (the change and the entry commit together); an empty diff writes nothing; failures return `Left` with `gdpr.ropa_audit_failed`. A start-up check (`IValidateOptions`) verifies the application's model contains the entity-change audit entity (registration completeness).
2. `src/Encina.MongoDB/ProcessingActivity/ProcessingActivityRegistryMongoDB.cs`: one session transaction around the replace and the audit insert; the start-up check verifies the deployment supports transactions (replica set) and fails closed otherwise.
3. `AddEncinaProcessingActivityEFCore()` and the MongoDB registration require the store and register the recorder mapping; the store wins over the in-memory default in any order.
4. Tests: guard tests (registry constructors), integration tests on EF SqlServer, PostgreSQL, MySQL and MongoDB through shared collection fixtures (register leaves an entry; update leaves an entry with the changed fields; no-op leaves none; a forced audit failure rolls the change back; tenant and actor present; no data subject data), contract tests; manifest targets.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Phase 1 is merged; #1778 provides IEntityChangeAuditStore with the entry contract and a transaction-aware write.
- Plan Design Choice 1 (Maintainer Decisions override the recommendation).

TASK:
Make ProcessingActivityRegistryEF and ProcessingActivityRegistryMongoDB write the change and its entry atomically, add the start-up checks and the tests.
At most 15 files and 1500 lines.

KEY RULES:
- Database calls are asynchronous with a CancellationToken; no synchronous overloads.
- Integration tests use [Collection("EFCore-SqlServer")] and its siblings, ClearAllDataAsync in InitializeAsync; never IClassFixture or a per-class container.
- Never swallow a failed audit write.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/ProcessingActivity/ProcessingActivityRegistryEF.cs
- src/Encina.MongoDB/Transactions/MongoSessionAccessor.cs
- tests/Encina.IntegrationTests/ (the existing EF Core and MongoDB ProcessingActivity tests)
```

</details>

---

### Phase 3: ADO.NET Registries (SqlServer, PostgreSQL, MySQL)

> **Goal**: The ADO providers audit atomically. **Size**: about 12 files, 1000 lines.

<details>
<summary>Tasks</summary>

1. `ProcessingActivityRegistryADO` in each of `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`: open one connection (`OpenAsync`), begin one transaction (`BeginTransactionAsync`), run the RoPA statement and the entity-change insert with the provider's SQL dialect (SQL Server `@param` and `bit`; PostgreSQL `@param` and case-sensitive identifiers; MySQL `@param` and backticks), commit; any failure rolls back and returns `Left`. For an update, read the current row inside the transaction to compute the diff.
2. The entity-change insert reuses the provider's audit table and mapping from its `AuditLogStoreADO` (or the transaction-aware write of #1778), so the entry matches what the store reads.
3. Registration (`AddEncinaProcessingActivityADO...`) requires the audit schema (the SQL script of the entity-change table is applied by the existing script mechanism); a start-up check fails when the table is missing.
4. Tests: guard tests (3), integration tests with `[Collection("ADO-SqlServer")]`, `[Collection("ADO-PostgreSQL")]`, `[Collection("ADO-MySQL")]` (same cases as Phase 2), contract tests across the three; manifest targets.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Phases 1-2 merged. The ADO registries are singletons built from a connection string; each opens its own connection per call.

TASK:
Add the atomic audit write to the three ADO registries, the registration checks and the tests. At most 15 files and 1500 lines.

KEY RULES:
- One connection and one transaction per write; OpenAsync, BeginTransactionAsync, ExecuteNonQueryAsync with the CancellationToken.
- The same behavior and error codes on the three providers; only the SQL dialect differs.
- Shared collection fixtures only.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/ProcessingActivity/ProcessingActivityRegistryADO.cs
- src/Encina.ADO.SqlServer/Auditing/AuditLogStoreADO.cs
- tests/Encina.IntegrationTests/ADO/SqlServer/ProcessingActivity/ProcessingActivityRegistryADOSqlServerTests.cs
```

</details>

---

### Phase 4: Dapper Registries (SqlServer, PostgreSQL, MySQL)

> **Goal**: The Dapper providers audit atomically. **Size**: about 12 files, 1000 lines.

<details>
<summary>Tasks</summary>

1. `ProcessingActivityRegistryDapper` in `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`: the same sequence as Phase 3 with Dapper `ExecuteAsync(CommandDefinition)` on one connection and transaction.
2. Registration and start-up checks as in Phase 3.
3. Tests: guard tests (3), integration tests with `[Collection("Dapper-SqlServer")]`, `[Collection("Dapper-PostgreSQL")]`, `[Collection("Dapper-MySQL")]`, contract tests across the three; manifest targets.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Phases 1-3 merged; the Dapper registries mirror the ADO ones.

TASK:
Add the atomic audit write to the three Dapper registries, the registration checks and the tests. At most 15 files and 1500 lines.

KEY RULES:
- CommandDefinition with the CancellationToken; one connection and one transaction; no synchronous overloads.
- Identical behavior across the three providers; shared collection fixtures only.

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/ProcessingActivity/ProcessingActivityRegistryDapper.cs
- src/Encina.Dapper.SqlServer/Auditing/AuditLogStoreDapper.cs
- tests/Encina.IntegrationTests/Dapper/SqlServer/ProcessingActivity/ProcessingActivityRegistryDapperSqlServerTests.cs
```

</details>

---

### Phase 5: Cross-Cutting Integration, Cross-Provider Tests and Documentation

> **Goal**: All 11 registries are proven equal and the feature is documented. **Size**: about 10 files, 700 lines.

<details>
<summary>Tasks</summary>

1. Cross-provider contract test (`tests/Encina.ContractTests/Compliance/GDPR/`): the 11 registries write an equal change for the same input (field names and values), run against the in-memory registry and the provider registries that need no Docker; property test: the diff of any two activities lists exactly the properties that differ (FsCheck).
2. Multi-tenancy: a two-tenant integration case shows tenant A's entries are never read for tenant B; with tenancy off the fixed default tenant applies (REQ-061). Telemetry: in-memory exporter test that the counter carries `change` and, with tenancy on, the tenant, and no data subject data (REQ-062).
3. Load and benchmark: `.md` justification files (`tests/Encina.LoadTests/Compliance/GDPR/ProcessingActivityHistory.md`, `tests/Encina.BenchmarkTests/.../ProcessingActivityHistory.md`: a write path of the controller's own record, not concurrent or hot).
4. Documentation by the docs-writer agent: the GDPR README and feature page section "History of processing activities", XML docs with examples, `changelog.d/1951-ropa-edit-history.added.md`; ROADMAP and INVENTORY when affected. No new ADR (it applies ADR-036).
5. `dotnet build --configuration Release` 0 warnings; `dotnet test` green; per-flag coverage at the targets of the manifests of the nine packages; CRAP of every new or changed method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Phases 1-4 merged. Documentation goes through docs-writer; the changelog fragment and PublicAPI lines through mechanical-fixer.

TASK:
Write the cross-provider contract and property tests, the two-tenant and telemetry tests, the two justification files and the documentation.

KEY RULES:
- Tests execute real code; Shouldly through Encina.Testing.Shouldly; justification files follow the template of AGENTS.md section 9.
- English only; coverage figures only through covref markers.

REFERENCE FILES:
- tests/Encina.ContractTests/Compliance/GDPR/ProcessingActivityRegistryProviderContractTests.cs
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | What it asks of this feature |
|--------|-----------|------------------------------|
| GDPR | Art. 5(2), Art. 30 | The controller can show how its record of processing evolved |
| SPEC-002 | REQ-061, REQ-062, DEC-008 | Tenant on every record; instrumentation without personal data; event-sourced modules stay Marten-only, the RoPA stays a 10-provider store |
| ADR-036 | Three audit stores | Entity-change entries through `IEntityChangeAuditStore` (today `IAuditLogStore`, #1778) |
| ADR-019 | Event-sourced compliance modules | Why the RoPA is not moved to Marten |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| Registry contract | `src/Encina.Compliance.GDPR/Abstractions/IProcessingActivityRegistry.cs` | The four operations whose writes are audited |
| In-memory registry | `src/Encina.Compliance.GDPR/InMemoryProcessingActivityRegistry.cs` | Default; gains the recorder |
| Provider registries | `src/Encina.ADO.*/ProcessingActivity/`, `src/Encina.Dapper.*/ProcessingActivity/`, `src/Encina.EntityFrameworkCore/ProcessingActivity/`, `src/Encina.MongoDB/ProcessingActivity/` | The eight classes that write the entry |
| Entity-change store today | `src/Encina.DomainModeling/Auditing/IAuditLogStore.cs`, `AuditLogEntry.cs`, and the `AuditLogStore*` classes of the provider packages | Entry shape and tables to reuse; replaced by #1778 |
| Request context | `src/Encina/Abstractions/IRequestContextAccessor.cs` | Actor, tenant and correlation id |
| Diagnostics | `src/Encina.Compliance.GDPR/Diagnostics/GDPRDiagnostics.cs` | Counter and trace tags |
| Coverage manifests | `.github/coverage-manifest/Encina.Compliance.GDPR.json` and the provider manifests | Per-file, per-flag targets |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.GDPR` | none used | `ComplianceGDPR` 8100-8199 has 8100-8116 used; Design Choice 6 adds no `[LoggerMessage]`; if a later phase needs one, take 8117 |
| Provider packages | none | They keep their own ranges; no new message |
| Plan family block | 6600-6649 | Not used by this plan (assigned to #687, #688) |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Source: GDPR core | 8 | Model, diff, recorder, factory, in-memory recorder, errors, diagnostics, registration |
| Source: 8 provider registries and their registration checks | 14 | One registry edit and one registration edit per package |
| Tests (unit, guard, contract, property, integration) | 20 | Integration across 10 providers in shared collections |
| Justification files, manifests, PublicAPI | 8 | Nine manifests are edited across the phases |
| Documentation and changelog | 3 | Docs through docs-writer |
| **Total** | **~53** | In 5 pull requests of at most 15 files |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0, Railway Oriented Programming, no compatibility layers. Rules: AGENTS.md.
- Issue #1951 audits every register/update of a processing activity through the entity-change audit store (ADR-036, #1778) on all
  11 registries. The Maintainer Decisions of the plan override its recommendations.

IMPLEMENTATION OVERVIEW:
1. GDPR core: ProcessingActivityChange, ProcessingActivityDiff, IProcessingActivityChangeRecorder, in-memory recorder, in-memory registry.
2. EF Core and MongoDB registries: change and entry in one SaveChanges / one session transaction.
3. ADO x3 and Dapper x3 registries: one connection and transaction, provider SQL dialects, same error codes.
4. Cross-provider tests, two-tenant and telemetry tests, justification files, documentation.

KEY PATTERNS:
- A failed audit write fails the change and rolls it back; errors are never swallowed; no data subject data anywhere.
- Actor and tenant from IRequestContextAccessor, time from TimeProvider, async database calls with a CancellationToken.
- Registration completeness: a database registry requires the store; the database store wins over the in-memory default in any order.
- Shared [Collection] fixtures for integration tests; CRAP <= 10 on changed methods.

REFERENCE FILES:
- src/Encina.Compliance.GDPR/InMemoryProcessingActivityRegistry.cs
- src/Encina.ADO.SqlServer/ProcessingActivity/ProcessingActivityRegistryADO.cs
- src/Encina.DomainModeling/Auditing/IAuditLogStore.cs
- docs/architecture/adr/036-three-audit-stores.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Write path only |
| 2 | OpenTelemetry | ✅ | Phases 1 and 5: counter `encina.gdpr.ropa.changes` and trace tags, tenant only when tenancy is on |
| 3 | Structured Logging | ❌ | The registries have no logger by design and return `Left` with `gdpr.ropa_audit_failed`, which the caller logs by error code (Design Choice 6); no EventId is used |
| 4 | Health Checks | ❌ | Uses the existing audit store's health check |
| 5 | Validation | ❌ | Validation of the legal ground stays with #1196's validator |
| 6 | Resilience | ❌ | No external call |
| 7 | Distributed Locks | ❌ | No shared state beyond the database's own transaction |
| 8 | Transactions | ✅ | Phases 2 to 4: the change and its entry commit together or the change fails (Design Choice 1) |
| 9 | Idempotency | ❌ | Not a message entry point |
| 10 | Multi-Tenancy | ✅ | Phases 1 and 5: the entry carries the tenant (REQ-061); two-tenant test |
| 11 | Module Isolation | ❌ | No module scoping required |
| 12 | Audit Trail | ✅ | The feature itself: entity-change entries through the ADR-036 store |

---

## Prerequisites & Dependencies

### Required Prerequisites

- #1778 merged: `IEntityChangeAuditStore` with `Either`, `TenantId`, `ModuleId`, the redaction hook and (Design Choice 1, option A) a write that joins an existing transaction on every provider. This requirement is to be raised on #1778.
- #1196 merged so that the diff covers the Art. 9(2) condition and the national legal-basis reference.

### Recommended (Not Blocking)

- #681 (closed): the 10 registries this plan edits.
- #1186 (parent epic).

---

## Next Steps

1. Link this plan from #1951, #1778 (the transaction-aware write requirement) and #1196.
2. When #1951 is picked up, re-check each Design Choice against the merged #1778 contract; present the Dilemma to the maintainer and record the answer in `## Maintainer Decisions`.
3. Ship Phases 1 to 5 as separate pull requests referencing #1951; the last one carries `Fixes #1951`.
