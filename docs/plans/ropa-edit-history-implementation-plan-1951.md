# Implementation Plan: Edit History for Processing Activities (RoPA) on the 10 Database Providers

> **Issue**: [#1951](https://github.com/dlrivada/Encina/issues/1951)
> **Type**: Feature
> **Complexity**: High (11 phases across 9 packages, no new package, 107 file changes in 11 pull requests; see Estimated File Count)
> **Estimated Scope**: about 8,000 changed lines in total (production code, tests, manifests, PublicAPI and documentation), per pull request in Estimated File Count
> **Specification**: GDPR Art. 5(2), Art. 30; SPEC-002 REQ-061, REQ-062 (part of #1186)
> **Depends on**: #1778 (the entity-change audit store of ADR-036) and #1196 (the Art. 9(2) fields whose changes must be auditable)

---

## Summary

Every successful register or update of a processing activity (the controller's Art. 30 record, `IProcessingActivityRegistry`), including the start-up auto-registration, leaves an entity-change audit entry with the activity id, the tenant, the actor from the request context, the time and the changed fields. A failed audit write fails the change. The entry carries no personal data of data subjects.

**What exists today** (verified on `origin/main`): `IProcessingActivityRegistry` has `RegisterActivityAsync`, `GetAllActivitiesAsync`, `GetActivityByRequestTypeAsync` and `UpdateActivityAsync` (`src/Encina.Compliance.GDPR/Abstractions/IProcessingActivityRegistry.cs`). The update overwrites the row: `InMemoryProcessingActivityRegistry` replaces the dictionary value and the database registries run an UPDATE, so only `LastUpdatedAtUtc` remains. `GDPRAutoRegistrationHostedService.StartAsync` calls `InMemoryProcessingActivityRegistry.AutoRegisterFromAssemblies`, which is synchronous, returns `int` and writes the dictionary directly (`_activities.TryAdd`), so it never goes through `RegisterActivityAsync`. Eleven registries exist: `InMemoryProcessingActivityRegistry`, `ProcessingActivityRegistryADO` (SqlServer, PostgreSQL, MySQL), `ProcessingActivityRegistryDapper` (SqlServer, PostgreSQL, MySQL), `ProcessingActivityRegistryEF` (one class for the three EF Core databases, over the application's `DbContext`) and `ProcessingActivityRegistryMongoDB`. The ADO and Dapper registries are built from a connection string only and open their own connection per call; the EF registry calls `SaveChangesAsync`; the MongoDB registry takes a connection string, a database name and a collection name. They are registered by `AddEncinaProcessingActivity{ADO,Dapper}{SqlServer,PostgreSQL,MySQL}`, `AddEncinaProcessingActivityEFCore` and `AddEncinaProcessingActivityMongoDB`. Their traces and outcomes go through `src/Encina.Compliance.GDPR/Diagnostics/ProcessingActivityDiagnostics.cs`. `ProcessingActivity` has no tenant. `Encina.Compliance.GDPR` references only `Encina`; the entity-change store today is `IAuditLogStore` in `Encina.DomainModeling` (`LogAsync` returns `Task`, entry `AuditLogEntry` without tenant), implemented on ADO x3, Dapper x3, EF Core and MongoDB; ADR-036 renames it `IEntityChangeAuditStore` and gives it `Either`, `TenantId`, `ModuleId` and a redaction hook, which is #1778 (open).

**Settled before this plan** (comment on #1951, 2026-10-17): write an awaited entry through the entity-change audit store on every register and update, in the same unit of work, failing the change if the write fails. Moving the RoPA to Marten contradicts SPEC-002 DEC-008 and #681; a history table duplicates the audit stores on 10 providers; a notification would return `Right` with zero handlers (silent loss); audit is mandatory and awaited (#2381 sub-decision 5).

**What this plan delivers**

1. In `Encina.Compliance.GDPR`: `ProcessingActivityChange` (the neutral record of one change), `ProcessingActivityDiff` (a pure, explicit comparison of two `ProcessingActivity` values), `IProcessingActivityChangeRecorder`, an in-memory recorder, and an in-memory registry whose register, update and auto-registration all record.
2. In each provider package (ADO x3, Dapper x3, EF Core, MongoDB): the registry writes the entity-change entry through its own provider's store path, atomically with the change (Design Choice 1), using the request context for actor and tenant.
3. Telemetry (counter and trace tags), tests on all 10 providers and in memory.

**Out of scope**: the redaction rules and the store contract itself (#1778); the new RoPA fields and their validation (#1196); a read API or UI for the history (the entries are read through the entity-change store's query); retention of the audit entries (the store's options, ADR-036).

**Standards**: GDPR Art. 5(2) accountability, Art. 30 records of processing; SPEC-002 REQ-061, REQ-062, DEC-008; ADR-036 (three audit stores), ADR-019, ADR-054, ADR-062 (error category contract, merged).

**Affected packages**: `Encina.Compliance.GDPR`, `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`.

**Provider category**: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (one class), MongoDB, plus the in-memory default.

---

## Design Choices

Four Dilemmas are open (5, 8, 11 and 12); the maintainer answers them when work on the issue starts. Choices 1 to 4, 6, 7, 9 and 10 are Settled or Trivial.

<details>
<summary><strong>1. How the change and its audit entry commit together</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One transaction per provider.** ADO and Dapper open one connection and transaction and write the RoPA row and the entry in it; EF adds the entry to the same `DbContext` before the single `SaveChangesAsync`; MongoDB uses one session transaction. The entity-change store offers a write that joins an existing transaction (a requirement raised to #1778) | Atomic: the change and its record commit together or neither does; no false or missing history | #1778 must expose a transaction-aware write on its 8 provider stores; MongoDB needs a replica set; more provider-specific code |
| **B) Write-ahead.** Write the entry first through the store's plain write, then the change; a failed change leaves an entry that is marked `Attempted` by a following correction entry | Uses only the plain store write; no change to #1778 | Not "in the same unit of work", so it reopens the decision settled on #1951; the history can show an attempt that never took effect; weaker accountability |

### Chosen Option: **A — One transaction per provider**

### Rationale

- The decision settled on #1951 on 2026-10-17 says "in the same unit of work, failing the change if the write fails"; only A meets it on every provider. B would reopen that decision and is listed only to record why it is not taken.
- The existing registries already own their connection (ADO, Dapper) or `DbContext` (EF), so the extra code is local to each. The transaction-aware write of #1778 is a prerequisite (Prerequisites).

**Class:** Settled
**Settled by:** #1951 (comment of 2026-10-17: "in the same unit of work"); AGENTS.md section 3 (a failed audit write fails the operation); ADR-036 (a failed entity-change write fails the unit of work)

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
- The diff compares the properties of `ProcessingActivity` explicitly: `Name`, `Purpose`, `LawfulBasis`, `CategoriesOfDataSubjects`, `CategoriesOfPersonalData`, `Recipients`, `ThirdCountryTransfers`, `Safeguards`, `RetentionPeriod`, `SecurityMeasures`, and the fields #1196 adds. Four properties are excluded by name: `Id` and `RequestType` (the identity and the lookup key; the entry carries them as entity id and type, they are not changes), `CreatedAtUtc` and `LastUpdatedAtUtc` (set by the registry on every write; comparing them would make every update a change). A unit test in `Encina.UnitTests` (not a guard test: guard tests are for argument checks) enumerates the public properties of the record by reflection, fails when one is neither compared nor in that exclusion list, and then executes the diff for each compared property, so a new field cannot be forgotten.

**Class:** Trivial
**Settled by:** AGENTS.md section 5 (provider coherence: abstractions in the shared package, providers differ only in implementation); #686 plan Design Choice 1; `src/Encina.Compliance.GDPR/Encina.Compliance.GDPR.csproj` (references only `Encina`)

</details>

<details>
<summary><strong>4. Actor and time of the entry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Actor from `IRequestContextAccessor` at write time; the actor `UserId` is `null` when there is no principal (for example the start-up auto-registration); time from `TimeProvider`; the entry holds the entity type `ProcessingActivity`, the activity id, the action (created or updated), the changed fields (Design Choice 5) and the correlation id; no data subject data** | Matches the entry contract (`UserId` is nullable); one clock | A start-up registration shows no actor |
| **B) Fail the write when there is no principal** | Every entry has an actor | The start-up auto-registration and background registration would fail; the gate rule applies to gates, not to audit of the controller's own record |

### Chosen Option: **A — Ambient context, nullable actor**

### Rationale

- The entry contract already allows a missing user, and the controller's own start-up registration has no principal by nature.

**Class:** Trivial
**Settled by:** `src/Encina.DomainModeling/Auditing/AuditLogEntry.cs` (`UserId` is `string?`); AGENTS.md section 3 (time comes from `TimeProvider`)

</details>

<details>
<summary><strong>5. Old and new values, or field names only</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Names of the changed fields with their old and new values as text** | The history shows what the record said before and after, which is what an Art. 5(2) reviewer asks | More data in each audit entry; relies on the store's redaction hook (ADR-036) if a sensitive field is ever added |
| **B) Names of the changed fields only** (what the issue body says) | Data minimisation in the audit record; nothing to redact | The history shows that `Purpose` changed, not from what to what; the old wording is lost on overwrite |

### Chosen Option: **A — Names with old and new values** (recommended, pending the maintainer)

### Rationale

- A RoPA record holds categories and purposes, not data about data subjects (verified on the properties of `Model/ProcessingActivity.cs`), so the values are controller metadata; without them the overwrite loses the previous wording, which is what the issue wants to keep. B wins when the maintainer prefers the strictest minimisation in audit records and accepts that the old text is not recoverable.
- With B, `ProcessingActivityFieldChange` carries only the field name, and the diff tests assert names only.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Tenant of the entry, with tenancy on and off</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The tenant is `IRequestContext.TenantId` at write time; with tenancy off it is `null`, the value the request context carries when no tenant is resolved** | One source of the tenant for every store (REQ-061: "the tenant id taken from the request context") | An entry written with tenancy off has no tenant |
| **B) A fixed string (for example `"default"`) when tenancy is off** | Every entry has a tenant value | A value no other store writes; not taken from the request context |

### Chosen Option: **A — The request context's tenant, `null` with tenancy off**

### Rationale

- REQ-061 takes the tenant from the request context, and `IRequestContext.TenantId` is `string?` (`src/Encina/Abstractions/IRequestContext.cs`); B would invent a second convention.

**Class:** Trivial
**Settled by:** SPEC-002 REQ-061 (tenant taken from the request context, REQ-015); `src/Encina/Abstractions/IRequestContext.cs` (`TenantId` is `string?`)

</details>

<details>
<summary><strong>7. A missing audit store is a start-up error, there is no opt-out</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A database-backed registry requires a registered entity-change store (and, for EF, the audit entity in the application's model); startup validation fails otherwise; the in-memory registry gets an in-memory recorder by `TryAdd`** | Registration completeness; no silent unaudited RoPA | An application that registers a database registry must also register the store |
| **B) An option `AuditChanges = false`** | Flexible | A silent opt-out of an accountability record |

### Chosen Option: **A — Required store, startup check**

### Rationale

- The database store wins over the in-memory default in any registration order without overriding the application's own registration. A RoPA audit is not a territorial control, so no territorial opt-out applies either.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (registration completeness; only an explicit, logged opt-out disables a control); #2381 (audit is mandatory and awaited); GDPR Art. 5(2)

</details>

<details>
<summary><strong>8. Telemetry and logging scope</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A counter `encina.gdpr.ropa.changes` (tags `change`: created, updated, failed; tenant only when multi-tenancy is on) and trace tags through the existing `ProcessingActivityDiagnostics`; no new `[LoggerMessage]`: the registries have no logger today and return `Left`, which the caller logs by error code** | No logger added to eight registries; no EventId | Departs from the issue's matrix (logging ✅); the failure line is the caller's |
| **B) Each provider registry takes an `ILogger` and logs a failed audit write with an EventId from its own package's existing range** (claimed in the batch EventId registry when chosen) | A local log line in every provider, as the issue's matrix proposed | A logger and an EventId in eight packages; eight constructor and DI changes more |

### Chosen Option: **A — Counter and trace, `Left` for the caller to log** (recommended, pending the maintainer)

### Rationale

- Both satisfy AGENTS.md section 7 (B with ids in each package's own range). I recommend A because the `Left` already reaches a caller that logs by error code, and the counter's `failed` tag makes failures visible without a log line per provider. B wins when the maintainer wants each provider to log its own audit failure, as the issue's matrix says.
- With B, each provider phase adds its `*LogMessages.cs` file (one file more per provider phase, still within 15) and the batch EventId registry gets one row per package.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. The audit error code: category and scope</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `gdpr.ropa_audit_failed` with `ErrorCategory.Internal`, created through the category overload of `EncinaErrors.Create`; returned only when the audit write itself fails; every other failure (duplicate, not found, a database fault of the RoPA statement) keeps the registry's existing code** | Follows ADR-062 rule R4 (store failures are `Internal`); callers keep telling a conflict from an audit failure | None |
| **B) Every failure of an audited write returns `gdpr.ropa_audit_failed`** | One code | Erases the existing codes; a duplicate would look like an audit failure |

### Chosen Option: **A — `Internal`, audit failures only**

### Rationale

- ADR-062 (merged) requires a category on every `EncinaErrors.Create` in `src/` (its completeness scan fails otherwise) and its rule R4 puts store failures in `Internal`.

**Class:** Settled
**Settled by:** ADR-062 (rule R4; completeness scan `ErrorCategoryCompletenessStaticScanTests`); AGENTS.md section 3 (errors are never swallowed or disguised)

</details>

<details>
<summary><strong>10. The start-up auto-registration is audited</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AutoRegisterFromAssemblies` becomes `AutoRegisterFromAssembliesAsync` returning `ValueTask<Either<EncinaError, int>>` and records each activity it creates (actor `null`); `GDPRAutoRegistrationHostedService.StartAsync` awaits it and a `Left` fails the start (an `InvalidOperationException` carrying the error code only)** | Every register leaves an entry, as the issue's first acceptance criterion requires; fails closed | The method and its test callers change |
| **B) The auto-registration stays unaudited** | No change | Breaks the acceptance criterion "every register ... in memory" |

### Chosen Option: **A — Auto-registration goes through the recorder**

### Rationale

- The default registry's default path must be audited like any other register, and a failure in background infrastructure fails the operation (here, the host start).

**Class:** Settled
**Settled by:** #1951 (acceptance criterion: an audit entry on every register and update, in memory too); AGENTS.md section 3 (a `Left` in background infrastructure fails the operation)

</details>

<details>
<summary><strong>11. Bound of the in-memory recorder</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Unbounded, thread-safe list for the process lifetime** | Never loses an entry; the number of entries is the number of RoPA edits, which is small | Grows without limit in a pathological loop |
| **B) Bounded; on overflow the write returns a `Left` and the change fails** | Memory bounded; fails closed | A long-running development host eventually refuses RoPA edits |
| **C) Bounded; the oldest entry is dropped and a counter records it** | Memory bounded; edits never fail | A silent loss of audit entries, which AGENTS.md section 3 forbids |

### Chosen Option: **A — Unbounded** (recommended, pending the maintainer)

### Rationale

- The in-memory registry is itself unbounded and is meant for development and tests; RoPA edits are rare. B wins when the maintainer wants a hard memory bound on every in-memory default. C is listed only to record that it is excluded (silent loss).

**Class:** Dilemma

</details>

<details>
<summary><strong>12. A no-op update</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An update whose diff is empty writes no entry** (the excluded properties of Design Choice 3 do not count) | The history shows changes only; the record did not change | An attempted update that changed nothing is not visible |
| **B) An update with an empty diff writes an entry with an empty field list** | Every update attempt is visible | Noise in the history; an entry that records no change |

### Chosen Option: **A — No entry for a no-op update** (recommended, pending the maintainer)

### Rationale

- Art. 30 asks for the record and Art. 5(2) for the ability to show how it changed; a no-op does not change it. B wins when the maintainer wants every write attempt audited.

**Class:** Dilemma

</details>

---

## Implementation Phases

Each phase is one pull request of at most 15 files and 1500 changed lines; the file lists below are exact and count the tests, the existing test callers of every changed signature, the PublicAPI files and the coverage manifests. Phase 0 is the gate: #1778 is merged with the transaction-aware write that Design Choice 1 needs, and #1196 is merged so the diff covers its fields. No pull request uses a type created by a later one. Each pull request enters, per file and with a one-sentence justification, the coverage targets of the flags whose tests it adds. Every provider phase also adds the two-tenant case (tenant A's entries are never read for tenant B) to its integration test file.

### Phase 1: Neutral Model, Diff and Recorder

> **Goal**: The change model, the diff and the in-memory recorder exist and are tested on every flag that applies. **Size**: 14 files, about 1,100 lines.

<details>
<summary>Tasks</summary>

Folder `src/Encina.Compliance.GDPR/ProcessingActivityHistory/`, namespace `Encina.Compliance.GDPR`.

1. `ProcessingActivityChange.cs` (public sealed record): `Guid ActivityId`, `string? TenantId`, `string? ActorId`, `ProcessingActivityChangeKind Kind`, `IReadOnlyList<ProcessingActivityFieldChange> Fields`, `DateTimeOffset ChangedAtUtc`, `string? CorrelationId`; `ProcessingActivityChangeKind.cs` (public enum `Created`, `Updated`); `ProcessingActivityFieldChange.cs` (public sealed record: field name, old value and new value as text, Design Choice 5).
2. `ProcessingActivityDiff.cs` (public static): `Compare(ProcessingActivity? before, ProcessingActivity after)` returning the field changes; explicit per-property comparison with sequence equality for lists; the excluded properties of Design Choice 3; an empty result for a no-op update (Design Choice 12).
3. `IProcessingActivityChangeRecorder.cs`: `ValueTask<Either<EncinaError, Unit>> RecordAsync(ProcessingActivityChange change, CancellationToken cancellationToken)`; `InMemoryProcessingActivityChangeRecorder.cs` (thread-safe, bound per Design Choice 11).
4. `ProcessingActivityChangeFactory.cs` (public static): builds the change from the two values, the ambient `IRequestContext` (actor, tenant per Design Choice 6, correlation id) and `TimeProvider`.
5. `GDPRErrors.cs`: `gdpr.ropa_audit_failed` with `ErrorCategory.Internal` (Design Choice 9); `PublicAPI.Unshipped.txt`.
6. Tests: `tests/Encina.UnitTests/Compliance/GDPR/ProcessingActivityHistory/ProcessingActivityDiffTests.cs` (each property, no-op, lists, and the reflection completeness test of Design Choice 3), `.../ProcessingActivityChangeRecordingTests.cs` (the factory: actor null, tenant off gives `null`; the in-memory recorder), `tests/Encina.GuardTests/Compliance/GDPR/ProcessingActivityHistoryGuardTests.cs`, `tests/Encina.PropertyTests/Compliance/GDPR/ProcessingActivityDiffPropertyTests.cs` (FsCheck: the diff of any two activities lists exactly the compared properties that differ).
7. `.github/coverage-manifest/Encina.Compliance.GDPR.json`: unit, guard and property targets of the new files (the property target of `ProcessingActivityDiff.cs` is entered here, with the property test), each with a one-sentence justification.

Exact file list (14): the 7 new source files of tasks 1-4, `GDPRErrors.cs`, `PublicAPI.Unshipped.txt`, the manifest, and the 4 test files of task 6.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #1951; plan docs/plans/ropa-edit-history-implementation-plan-1951.md. Maintainer Decisions override the "recommended" choices.
- Encina.Compliance.GDPR references only Encina. ADR-062 (error categories) is merged.

TASK:
Create the change model, the diff, the recorder abstraction with its in-memory implementation and the factory, the error code
and the tests. Exactly the 14 files of the Phase 1 file list; at most 1500 lines.

KEY RULES:
- Time from TimeProvider; errors are never swallowed; EncinaErrors.Create with ErrorCategory.Internal (category overload).
- No data subject data, payload or exception message in the change, the error or the telemetry.
- The reflection completeness test lives in UnitTests and names the four excluded properties.

REFERENCE FILES:
- src/Encina.Compliance.GDPR/Model/ProcessingActivity.cs
- src/Encina.Compliance.GDPR/GDPRErrors.cs
- src/Encina/Abstractions/IRequestContextAccessor.cs
```

</details>

---

### Phase 2: In-Memory Registry and Auto-Registration

> **Goal**: The default registry audits register, update and the start-up auto-registration. **Size**: 13 files, about 900 lines.

<details>
<summary>Tasks</summary>

1. `InMemoryProcessingActivityRegistry.cs`: takes the recorder, the accessor and `TimeProvider` (optional constructor parameters with the in-memory defaults, so the five test files that construct it keep compiling); records on register and update; a recorder `Left` fails the call and the dictionary is not changed; `AutoRegisterFromAssemblies` becomes `AutoRegisterFromAssembliesAsync` (Design Choice 10).
2. `GDPRAutoRegistrationHostedService.cs`: awaits the auto-registration; a `Left` fails the start with an `InvalidOperationException` carrying the error code only.
3. `Diagnostics/ProcessingActivityDiagnostics.cs`: counter `encina.gdpr.ropa.changes` (tags `change`; tenant only when multi-tenancy is on, ADR-054 D4) and the trace tags.
4. `ServiceCollectionExtensions.cs` (GDPR): registers the in-memory recorder by `TryAdd`; `PublicAPI.Unshipped.txt`.
5. Existing test callers of `AutoRegisterFromAssemblies`: `tests/Encina.UnitTests/Compliance/GDPR/InMemoryProcessingActivityRegistryTests.cs` (also the new audit cases: register and update record, a failed recorder leaves the state unchanged), `tests/Encina.GuardTests/Compliance/GDPR/GDPRGuardTests.cs`, `tests/Encina.GuardTests/Compliance/GDPR/GDPRHappyPathGuardTests.cs`, `tests/Encina.ContractTests/Compliance/GDPR/GDPRPipelineBehaviorContractTests.cs`.
6. New and extended tests: `tests/Encina.UnitTests/Compliance/GDPR/GDPRAutoRegistrationHostedServiceTests.cs` (each auto-registered activity is recorded with a `null` actor; a recorder `Left` fails the start), `tests/Encina.UnitTests/Compliance/GDPR/ServiceCollectionExtensionsTests.cs` (the recorder is registered; `ValidateOnBuild` and `ValidateScopes`), `tests/Encina.UnitTests/Compliance/GDPR/ProcessingActivityDiagnosticsTests.cs` (in-memory exporter: the counter carries `change` and, with tenancy on, the tenant, and no data subject data, REQ-062).
7. Manifest: unit, guard and contract targets of the four changed source files, each with a one-sentence justification.

Exact file list (13): `InMemoryProcessingActivityRegistry.cs`, `GDPRAutoRegistrationHostedService.cs`, `ProcessingActivityDiagnostics.cs`, `ServiceCollectionExtensions.cs`, `PublicAPI.Unshipped.txt`, the manifest, the 4 caller test files of task 5 and the 3 test files of task 6.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Phase 1 is merged. Callers: `git grep -n "AutoRegisterFromAssemblies" -- tests src` before editing.

TASK:
Wire the recorder into the in-memory registry and the auto-registration, add the counter and the tests. Exactly the 13 files of
the Phase 2 file list.

KEY RULES:
- A recorder Left fails the call and changes nothing; a Left in the hosted service fails the start with the code only.
- Tenant tag only when tenancy is on (ADR-054 D4); CRAP <= 10 on every changed method.

REFERENCE FILES:
- src/Encina.Compliance.GDPR/InMemoryProcessingActivityRegistry.cs
- src/Encina.Compliance.GDPR/GDPRAutoRegistrationHostedService.cs
- src/Encina.Compliance.GDPR/Diagnostics/ProcessingActivityDiagnostics.cs
```

</details>

---

### Phase 3: EF Core Registry

> **Goal**: The EF Core registry audits atomically on SqlServer, PostgreSQL and MySQL. **Size**: 13 files, about 1,100 lines.

<details>
<summary>Tasks</summary>

1. `src/Encina.EntityFrameworkCore/ProcessingActivity/ProcessingActivityRegistryEF.cs`: the constructor takes the change mapping dependencies (accessor, `TimeProvider`); on register and update, load the current row (update), compute the diff, add the entity-change entry to the same `DbContext` and call `SaveChangesAsync` once; an empty diff writes nothing; only a failed audit write returns `gdpr.ropa_audit_failed` (Design Choice 9).
2. `src/Encina.EntityFrameworkCore/ProcessingActivity/ProcessingActivityAuditModelCheck.cs` (new, internal, `IValidateOptions` or start-up check): the application's model contains the entity-change audit entity (registration completeness).
3. `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs` (`AddEncinaProcessingActivityEFCore`): requires the store, registers the check; the store wins over the in-memory default in any order. `PublicAPI.Unshipped.txt`; `.github/coverage-manifest/Encina.EntityFrameworkCore.json`.
4. Existing test callers: `tests/Encina.UnitTests/EntityFrameworkCore/ProcessingActivity/ProcessingActivityRegistryEFTests.cs`, `tests/Encina.UnitTests/Compliance/StoreFailures/StoreFailureDoesNotLeakExceptionMessageTests.cs` (the EF cases), `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/ProcessingActivity/ProcessingActivityTestDbContext.cs` (adds the audit entity), and the integration files `.../SqlServer/ProcessingActivity/ProcessingActivityRegistryEFSqlServerTests.cs`, `.../PostgreSQL/ProcessingActivity/ProcessingActivityRegistryEFPostgreSqlTests.cs`, `.../MySQL/ProcessingActivity/ProcessingActivityRegistryEFMySqlTests.cs` (register and update leave an entry with the changed fields; no-op per Design Choice 12; a forced audit failure rolls the change back; tenant and actor present; two tenants; no data subject data).
5. New tests: `tests/Encina.GuardTests/EntityFrameworkCore/ProcessingActivity/ProcessingActivityRegistryEFGuardTests.cs`, `tests/Encina.UnitTests/EntityFrameworkCore/ProcessingActivity/ProcessingActivityAuditModelCheckTests.cs`.
6. Manifest: unit, guard and integration targets of the three source files, each with a one-sentence justification.

Exact file list (13): the 3 source files of tasks 1-3, `PublicAPI.Unshipped.txt`, the manifest, the 6 caller test files of task 4 and the 2 test files of task 5.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Phases 1-2 merged; #1778 provides IEntityChangeAuditStore and its EF entity. Plan Design Choices 1, 5, 9 and 12.

TASK:
Make ProcessingActivityRegistryEF write the change and its entry in one SaveChangesAsync, add the model check, update the
callers and write the tests. Exactly the 13 files of the Phase 3 file list.

KEY RULES:
- Database calls are asynchronous with a CancellationToken; no synchronous overloads.
- Integration tests use [Collection("EFCore-SqlServer")] and its siblings, ClearAllDataAsync in InitializeAsync.
- Never swallow a failed audit write; other failures keep their codes.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/ProcessingActivity/ProcessingActivityRegistryEF.cs
- tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/ProcessingActivity/ProcessingActivityTestDbContext.cs
```

</details>

---

### Phase 4: MongoDB Registry

> **Goal**: The MongoDB registry audits atomically. **Size**: 9 files, about 700 lines.

<details>
<summary>Tasks</summary>

1. `src/Encina.MongoDB/ProcessingActivity/ProcessingActivityRegistryMongoDB.cs`: one session transaction around the replace and the audit insert (`MongoSessionAccessor` pattern).
2. `src/Encina.MongoDB/ProcessingActivity/ProcessingActivityTransactionSupportCheck.cs` (new, internal start-up check): the deployment supports transactions (replica set) and the start fails closed otherwise.
3. `src/Encina.MongoDB/ServiceCollectionExtensions.cs` (`AddEncinaProcessingActivityMongoDB`): requires the store and registers the check. `PublicAPI.Unshipped.txt`; `.github/coverage-manifest/Encina.MongoDB.json`.
4. Existing test callers: `tests/Encina.GuardTests/MongoDB/ProcessingActivity/ProcessingActivityRegistryGuardTests.cs`, `tests/Encina.GuardTests/MongoDB/AuditAndComplianceGuardTests.cs`, `tests/Encina.IntegrationTests/Infrastructure/MongoDB/ProcessingActivity/ProcessingActivityRegistryMongoDBTests.cs` (moves from `[Collection("MongoDB")]` to `[Collection(MongoDbReplicaSetCollection.Name)]`, the single-node replica set fixture the outbox atomicity tests use; same cases as Phase 3).
5. New test: `tests/Encina.UnitTests/MongoDB/ProcessingActivity/ProcessingActivityTransactionSupportCheckTests.cs`.
6. Manifest: unit, guard and integration targets of the three source files, each with a one-sentence justification.

Exact file list (9): the 3 source files, `PublicAPI.Unshipped.txt`, the manifest, the 3 caller test files and the new test file.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Phases 1-3 merged. The replica-set fixture: tests/Encina.IntegrationTests/Infrastructure/MongoDB/Outbox/OutboxAtomicityMongoDBTests.cs.

TASK:
Make ProcessingActivityRegistryMongoDB write the change and its entry in one session transaction, add the transaction-support
check and the tests. Exactly the 9 files of the Phase 4 file list.

KEY RULES:
- Fail closed without a replica set; asynchronous calls with a CancellationToken; shared collection fixture only.

REFERENCE FILES:
- src/Encina.MongoDB/ProcessingActivity/ProcessingActivityRegistryMongoDB.cs
- src/Encina.MongoDB/Transactions/MongoSessionAccessor.cs
```

</details>

---

### Phase 5: ADO.NET SqlServer Registry

> **Goal**: The ADO SqlServer registry audits atomically. **Size**: 9 files, about 700 lines.

<details>
<summary>Tasks</summary>

1. `src/Encina.ADO.SqlServer/ProcessingActivity/ProcessingActivityRegistryADO.cs`: open one connection (`OpenAsync`), begin one transaction (`BeginTransactionAsync`), read the current row inside it (update), run the RoPA statement and the entity-change insert (SQL Server `@param`, `bit`), commit; any failure rolls back; only a failed audit write returns `gdpr.ropa_audit_failed`. The insert reuses the table and mapping of the provider's entity-change store (the transaction-aware write of #1778).
2. `src/Encina.ADO.SqlServer/ProcessingActivity/ProcessingActivityAuditSchemaCheck.cs` (new, internal start-up check): the entity-change table exists.
3. `src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs` (`AddEncinaProcessingActivityADOSqlServer`): requires the store and registers the check. `PublicAPI.Unshipped.txt`; `.github/coverage-manifest/Encina.ADO.SqlServer.json`.
4. Existing test callers: `tests/Encina.GuardTests/ADO/SqlServer/ProcessingActivity/ProcessingActivityRegistryADOGuardTests.cs`, `tests/Encina.IntegrationTests/ADO/SqlServer/ProcessingActivity/ProcessingActivityRegistryADOSqlServerTests.cs` (`[Collection("ADO-SqlServer")]`, the Phase 3 cases), `tests/Encina.UnitTests/Compliance/StoreFailures/StoreFailureDoesNotLeakExceptionMessageTests.cs` (the ADO case).
5. New test: `tests/Encina.UnitTests/ADO/SqlServer/ProcessingActivity/ProcessingActivityAuditSchemaCheckTests.cs`.
6. Manifest: unit, guard and integration targets of the three source files, each with a one-sentence justification.

Exact file list (9): the 3 source files, `PublicAPI.Unshipped.txt`, the manifest, the 3 caller test files and the new test file.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Phases 1-4 merged. The ADO registries are built from a connection string; each opens its own connection per call.

TASK:
Add the atomic audit write to the ADO SqlServer registry, the schema check and the tests. Exactly the 9 files of the Phase 5
file list.

KEY RULES:
- One connection and one transaction per write; OpenAsync, BeginTransactionAsync, ExecuteNonQueryAsync with the CancellationToken.
- Shared collection fixtures only; the same behavior and error codes as the EF Core and MongoDB registries.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/ProcessingActivity/ProcessingActivityRegistryADO.cs
- src/Encina.ADO.SqlServer/Auditing/AuditLogStoreADO.cs
```

</details>

---

### Phase 6: ADO.NET PostgreSQL Registry

> **Goal**: The ADO PostgreSQL registry audits atomically. **Size**: 8 files, about 600 lines.

<details>
<summary>Tasks</summary>

1. The Phase 5 tasks 1-3 in `src/Encina.ADO.PostgreSQL/` (`ProcessingActivityRegistryADO.cs`, new `ProcessingActivity/ProcessingActivityAuditSchemaCheck.cs`, `ServiceCollectionExtensions.cs` with `AddEncinaProcessingActivityADOPostgreSQL`), with the PostgreSQL dialect (`@param`, case-sensitive identifiers, `true`/`false`); `PublicAPI.Unshipped.txt`; `.github/coverage-manifest/Encina.ADO.PostgreSQL.json`.
2. Tests: `tests/Encina.GuardTests/ADO/PostgreSQL/ProcessingActivity/ProcessingActivityRegistryADOGuardTests.cs`, `tests/Encina.IntegrationTests/ADO/PostgreSQL/ProcessingActivity/ProcessingActivityRegistryADOPostgreSqlTests.cs` (`[Collection("ADO-PostgreSQL")]`), new `tests/Encina.UnitTests/ADO/PostgreSQL/ProcessingActivity/ProcessingActivityAuditSchemaCheckTests.cs`.
3. Manifest: unit, guard and integration targets of the three source files, each with a one-sentence justification.

Exact file list (8): the 3 source files, `PublicAPI.Unshipped.txt`, the manifest and the 3 test files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Phase 5 merged; mirror it for ADO PostgreSQL.

TASK:
Exactly the 8 files of the Phase 6 file list.

KEY RULES:
- PostgreSQL dialect; asynchronous calls with a CancellationToken; [Collection("ADO-PostgreSQL")].

REFERENCE FILES:
- src/Encina.ADO.SqlServer/ProcessingActivity/ProcessingActivityRegistryADO.cs (as changed in Phase 5)
- src/Encina.ADO.PostgreSQL/ProcessingActivity/ProcessingActivityRegistryADO.cs
```

</details>

---

### Phase 7: ADO.NET MySQL Registry

> **Goal**: The ADO MySQL registry audits atomically. **Size**: 8 files, about 600 lines.

<details>
<summary>Tasks</summary>

1. The Phase 5 tasks 1-3 in `src/Encina.ADO.MySQL/` (`AddEncinaProcessingActivityADOMySQL`), with the MySQL dialect (`@param`, backtick identifiers, `0`/`1`); `PublicAPI.Unshipped.txt`; `.github/coverage-manifest/Encina.ADO.MySQL.json`.
2. Tests: `tests/Encina.GuardTests/ADO/MySQL/ProcessingActivity/ProcessingActivityRegistryADOGuardTests.cs`, `tests/Encina.IntegrationTests/ADO/MySQL/ProcessingActivity/ProcessingActivityRegistryADOMySqlTests.cs` (`[Collection("ADO-MySQL")]`), new `tests/Encina.UnitTests/ADO/MySQL/ProcessingActivity/ProcessingActivityAuditSchemaCheckTests.cs`.
3. Manifest: unit, guard and integration targets of the three source files, each with a one-sentence justification.

Exact file list (8): the 3 source files, `PublicAPI.Unshipped.txt`, the manifest and the 3 test files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT:
- Phase 5 merged; mirror it for ADO MySQL.

TASK:
Exactly the 8 files of the Phase 7 file list.

KEY RULES:
- MySQL dialect; asynchronous calls with a CancellationToken; [Collection("ADO-MySQL")].

REFERENCE FILES:
- src/Encina.ADO.MySQL/ProcessingActivity/ProcessingActivityRegistryADO.cs
```

</details>

---

### Phase 8: Dapper SqlServer Registry

> **Goal**: The Dapper SqlServer registry audits atomically. **Size**: 9 files, about 700 lines.

<details>
<summary>Tasks</summary>

1. `src/Encina.Dapper.SqlServer/ProcessingActivity/ProcessingActivityRegistryDapper.cs`: the Phase 5 sequence with Dapper `ExecuteAsync(CommandDefinition)` on one connection and transaction; new `ProcessingActivity/ProcessingActivityAuditSchemaCheck.cs`; `ServiceCollectionExtensions.cs` (`AddEncinaProcessingActivityDapperSqlServer`); `PublicAPI.Unshipped.txt`; `.github/coverage-manifest/Encina.Dapper.SqlServer.json`.
2. Tests: `tests/Encina.GuardTests/Dapper/SqlServer/ProcessingActivity/ProcessingActivityRegistryDapperGuardTests.cs`, `tests/Encina.IntegrationTests/Dapper/SqlServer/ProcessingActivity/ProcessingActivityRegistryDapperSqlServerTests.cs` (`[Collection("Dapper-SqlServer")]`), `tests/Encina.UnitTests/Compliance/StoreFailures/StoreFailureDoesNotLeakExceptionMessageTests.cs` (the Dapper case), new `tests/Encina.UnitTests/Dapper/SqlServer/ProcessingActivity/ProcessingActivityAuditSchemaCheckTests.cs`.
3. Manifest: unit, guard and integration targets of the three source files, each with a one-sentence justification.

Exact file list (9): the 3 source files, `PublicAPI.Unshipped.txt`, the manifest and the 4 test files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
CONTEXT:
- Phases 1-7 merged; the Dapper registries mirror the ADO ones.

TASK:
Exactly the 9 files of the Phase 8 file list.

KEY RULES:
- CommandDefinition with the CancellationToken; one connection and one transaction; [Collection("Dapper-SqlServer")].

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/ProcessingActivity/ProcessingActivityRegistryDapper.cs
- src/Encina.Dapper.SqlServer/Auditing/AuditLogStoreDapper.cs
```

</details>

---

### Phase 9: Dapper PostgreSQL Registry

> **Goal**: The Dapper PostgreSQL registry audits atomically. **Size**: 8 files, about 600 lines.

<details>
<summary>Tasks</summary>

1. The Phase 8 task 1 in `src/Encina.Dapper.PostgreSQL/` (`AddEncinaProcessingActivityDapperPostgreSQL`), PostgreSQL dialect; `PublicAPI.Unshipped.txt`; `.github/coverage-manifest/Encina.Dapper.PostgreSQL.json`.
2. Tests: `tests/Encina.GuardTests/Dapper/PostgreSQL/ProcessingActivity/ProcessingActivityRegistryDapperGuardTests.cs`, `tests/Encina.IntegrationTests/Dapper/PostgreSQL/ProcessingActivity/ProcessingActivityRegistryDapperPostgreSqlTests.cs` (`[Collection("Dapper-PostgreSQL")]`), new `tests/Encina.UnitTests/Dapper/PostgreSQL/ProcessingActivity/ProcessingActivityAuditSchemaCheckTests.cs`.
3. Manifest: unit, guard and integration targets of the three source files, each with a one-sentence justification.

Exact file list (8): the 3 source files, `PublicAPI.Unshipped.txt`, the manifest and the 3 test files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 9</summary>

```text
CONTEXT:
- Phase 8 merged; mirror it for Dapper PostgreSQL.

TASK:
Exactly the 8 files of the Phase 9 file list.

KEY RULES:
- PostgreSQL dialect; CommandDefinition with the CancellationToken; [Collection("Dapper-PostgreSQL")].

REFERENCE FILES:
- src/Encina.Dapper.PostgreSQL/ProcessingActivity/ProcessingActivityRegistryDapper.cs
```

</details>

---

### Phase 10: Dapper MySQL Registry

> **Goal**: The Dapper MySQL registry audits atomically. **Size**: 8 files, about 600 lines.

<details>
<summary>Tasks</summary>

1. The Phase 8 task 1 in `src/Encina.Dapper.MySQL/` (`AddEncinaProcessingActivityDapperMySQL`), MySQL dialect; `PublicAPI.Unshipped.txt`; `.github/coverage-manifest/Encina.Dapper.MySQL.json`.
2. Tests: `tests/Encina.GuardTests/Dapper/MySQL/ProcessingActivity/ProcessingActivityRegistryDapperGuardTests.cs`, `tests/Encina.IntegrationTests/Dapper/MySQL/ProcessingActivity/ProcessingActivityRegistryDapperMySqlTests.cs` (`[Collection("Dapper-MySQL")]`), new `tests/Encina.UnitTests/Dapper/MySQL/ProcessingActivity/ProcessingActivityAuditSchemaCheckTests.cs`.
3. Manifest: unit, guard and integration targets of the three source files, each with a one-sentence justification.

Exact file list (8): the 3 source files, `PublicAPI.Unshipped.txt`, the manifest and the 3 test files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 10</summary>

```text
CONTEXT:
- Phase 8 merged; mirror it for Dapper MySQL.

TASK:
Exactly the 8 files of the Phase 10 file list.

KEY RULES:
- MySQL dialect; CommandDefinition with the CancellationToken; [Collection("Dapper-MySQL")].

REFERENCE FILES:
- src/Encina.Dapper.MySQL/ProcessingActivity/ProcessingActivityRegistryDapper.cs
```

</details>

---

### Phase 11: Cross-Provider Contract, Justifications and Documentation

> **Goal**: All 11 registries are proven equal and the feature is documented. **Size**: 8 files, about 500 lines.

<details>
<summary>Tasks</summary>

1. `tests/Encina.ContractTests/Compliance/GDPR/ProcessingActivityChangeContractTests.cs` (new): the in-memory registry and the EF Core registry over the EF in-memory test context produce an equal change for the same input (field names and values); the database registries are compared by the shared cases of their integration files (Phases 3-10). Manifest `Encina.Compliance.GDPR.json` and `Encina.EntityFrameworkCore.json`: contract targets of the registry files these tests cover, each with a one-sentence justification.
2. Load and benchmark justifications following the template of AGENTS.md section 9: `tests/Encina.LoadTests/Compliance/GDPR/ProcessingActivityHistory.md` and `tests/Encina.BenchmarkTests/Encina.Benchmarks/Compliance/GDPR/ProcessingActivityHistory.md` (a write path of the controller's own record, not concurrent or hot).
3. Documentation by the docs-writer agent: `src/Encina.Compliance.GDPR/README.md` and `docs/features/gdpr-compliance.md` section "History of processing activities" (what is recorded, the actor of an auto-registration, the store requirement per provider, MongoDB needs a replica set); `changelog.d/1951-ropa-edit-history.added.md`. No new ADR (it applies ADR-036).
4. `dotnet build --configuration Release` 0 warnings; `dotnet test` green; per-flag coverage at the targets of the manifests of the nine packages; CRAP of every new or changed method at most 10.

Exact file list (8): the contract test file, 2 manifests, 2 justification files, `README.md`, `docs/features/gdpr-compliance.md`, the changelog fragment.

</details>

<details>
<summary>Prompt for AI Agents — Phase 11</summary>

```text
CONTEXT:
- Phases 1-10 merged. Documentation goes through docs-writer; the changelog fragment and manifest lines through mechanical-fixer.

TASK:
Write the cross-provider contract test, the two justification files and the documentation. Exactly the 8 files of the
Phase 11 file list.

KEY RULES:
- Tests execute real code; Shouldly through Encina.Testing.Shouldly; justification files follow the template of AGENTS.md section 9.
- English only; coverage figures only through covref markers.

REFERENCE FILES:
- tests/Encina.ContractTests/Compliance/GDPR/ProcessingActivityRegistryProviderContractTests.cs
- docs/features/gdpr-compliance.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | What it asks of this feature |
|--------|-----------|------------------------------|
| GDPR | Art. 5(2), Art. 30 | The controller can show how its record of processing evolved |
| SPEC-002 | REQ-061, REQ-062, DEC-008 | Tenant on every record from the request context; instrumentation without personal data; the RoPA stays a 10-provider store |
| ADR-036 | Three audit stores | Entity-change entries through `IEntityChangeAuditStore` (today `IAuditLogStore`, #1778) |
| ADR-062 | Error category contract | `gdpr.ropa_audit_failed` is `Internal` (rule R4) |
| ADR-019 | Event-sourced compliance modules | Why the RoPA is not moved to Marten |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| Registry contract | `src/Encina.Compliance.GDPR/Abstractions/IProcessingActivityRegistry.cs` | The operations whose writes are audited |
| In-memory registry and auto-registration | `src/Encina.Compliance.GDPR/InMemoryProcessingActivityRegistry.cs`, `GDPRAutoRegistrationHostedService.cs` | Default; gains the recorder; auto-registration goes through it |
| Provider registries | `src/Encina.ADO.*/ProcessingActivity/`, `src/Encina.Dapper.*/ProcessingActivity/`, `src/Encina.EntityFrameworkCore/ProcessingActivity/`, `src/Encina.MongoDB/ProcessingActivity/` | The eight classes that write the entry |
| Entity-change store today | `src/Encina.DomainModeling/Auditing/IAuditLogStore.cs`, `AuditLogEntry.cs`, and the `AuditLogStore*` classes of the provider packages | Entry shape and tables to reuse; replaced by #1778 |
| Request context | `src/Encina/Abstractions/IRequestContextAccessor.cs`, `IRequestContext.cs` | Actor, tenant and correlation id |
| Diagnostics | `src/Encina.Compliance.GDPR/Diagnostics/ProcessingActivityDiagnostics.cs` | Counter and trace tags |
| MongoDB replica set fixture | `MongoDbReplicaSetFixture`, `MongoDbReplicaSetCollection` (used by `OutboxAtomicityMongoDBTests.cs`) | Transactions in the MongoDB integration tests |
| Coverage manifests | `.github/coverage-manifest/Encina.Compliance.GDPR.json` and the eight provider manifests | Per-file, per-flag targets |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.GDPR` | none used | `ComplianceGDPR` 8100-8199 has 8100-8116 used; Design Choice 8 A adds no `[LoggerMessage]` |
| Provider packages | none with Design Choice 8 A | With 8 B, each provider takes one id in its own existing range, claimed in the batch EventId registry before its phase |

### Estimated File Count

| Pull request | Phase | Files | Lines (about) | Contents |
|--------------|-------|-------|---------------|----------|
| PR 1 | 1 | 14 | 1,100 | GDPR core model, diff, recorder, factory, error; PublicAPI; manifest; 4 test files |
| PR 2 | 2 | 13 | 900 | in-memory registry, auto-registration, diagnostics, registration; PublicAPI; manifest; 7 test files |
| PR 3 | 3 | 13 | 1,100 | EF registry, model check, registration; PublicAPI; manifest; 8 test files |
| PR 4 | 4 | 9 | 700 | MongoDB registry, transaction check, registration; PublicAPI; manifest; 4 test files |
| PR 5 | 5 | 9 | 700 | ADO SqlServer registry, schema check, registration; PublicAPI; manifest; 4 test files |
| PR 6 | 6 | 8 | 600 | ADO PostgreSQL, same shape; 3 test files |
| PR 7 | 7 | 8 | 600 | ADO MySQL, same shape; 3 test files |
| PR 8 | 8 | 9 | 700 | Dapper SqlServer, same shape; 4 test files |
| PR 9 | 9 | 8 | 600 | Dapper PostgreSQL, same shape; 3 test files |
| PR 10 | 10 | 8 | 600 | Dapper MySQL, same shape; 3 test files |
| PR 11 | 11 | 8 | 500 | contract test, 2 manifests, 2 justification files, README, feature page, changelog fragment |
| **Total** | | **107** | **8,100** | 107 file changes in 11 pull requests; `StoreFailureDoesNotLeakExceptionMessageTests.cs`, the GDPR manifest and the EF manifest change in more than one pull request, so fewer distinct files are touched |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0, Railway Oriented Programming, no compatibility layers. Rules: AGENTS.md.
- Issue #1951 audits every register/update of a processing activity, including the start-up auto-registration, through the
  entity-change audit store (ADR-036, #1778) on all 11 registries. The Maintainer Decisions of the plan override its
  recommendations.

IMPLEMENTATION OVERVIEW:
1. GDPR core: ProcessingActivityChange, ProcessingActivityDiff, IProcessingActivityChangeRecorder, in-memory recorder, error code.
2. In-memory registry and auto-registration through the recorder; counter.
3. EF Core and MongoDB registries: change and entry in one SaveChanges / one session transaction.
4. ADO x3 and Dapper x3 registries, one pull request per database: one connection and transaction, provider SQL dialects.
5. Cross-provider contract test, justification files, documentation.

KEY PATTERNS:
- A failed audit write fails the change and rolls it back; other failures keep their codes; no data subject data anywhere.
- gdpr.ropa_audit_failed is ErrorCategory.Internal (ADR-062 R4).
- Actor and tenant from IRequestContextAccessor (tenant null with tenancy off), time from TimeProvider, async database calls.
- Registration completeness: a database registry requires the store; the database store wins over the in-memory default.
- Shared [Collection] fixtures for integration tests; CRAP <= 10 on changed methods; at most 15 files per pull request.

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
| 2 | OpenTelemetry | ✅ | Phase 2: counter `encina.gdpr.ropa.changes` and trace tags through `ProcessingActivityDiagnostics`, tenant only when tenancy is on |
| 3 | Structured Logging | ❌ | With Design Choice 8 A (recommended, pending the maintainer) the registries return `Left`, which the caller logs by error code, and no EventId is used; this departs from the issue's matrix, and option B restores a log line per provider |
| 4 | Health Checks | ❌ | Uses the existing audit store's health check |
| 5 | Validation | ❌ | Validation of the legal ground stays with #1196's validator |
| 6 | Resilience | ❌ | No external call |
| 7 | Distributed Locks | ❌ | No shared state beyond the database's own transaction |
| 8 | Transactions | ✅ | Phases 3 to 10: the change and its entry commit together or the change fails (Design Choice 1) |
| 9 | Idempotency | ❌ | Not a message entry point |
| 10 | Multi-Tenancy | ✅ | Phases 1 to 10: the entry carries the request context's tenant (REQ-061); a two-tenant case on every provider |
| 11 | Module Isolation | ❌ | No module scoping required |
| 12 | Audit Trail | ✅ | The feature itself: entity-change entries through the ADR-036 store, including the auto-registration |

---

## Prerequisites & Dependencies

### Required Prerequisites

- #1778 merged: `IEntityChangeAuditStore` with `Either`, `TenantId`, `ModuleId`, the redaction hook and a write that joins an existing transaction on every provider (Design Choice 1). This requirement is to be raised on #1778.
- #1196 merged so that the diff covers the Art. 9(2) condition and the national legal-basis reference.

### Recommended (Not Blocking)

- #681 (closed): the 10 registries this plan edits.
- #1186 (parent epic).

---

## Next Steps

1. Link this plan from #1951, #1778 (the transaction-aware write requirement) and #1196.
2. When #1951 is picked up, re-check each Design Choice against the merged #1778 contract; present the Dilemmas to the maintainer and record the answers in `## Maintainer Decisions`; re-count the file lists of the phases a decision changes (Design Choices 5 and 8).
3. Ship Phases 1 to 11 as separate pull requests referencing #1951; the last one carries `Fixes #1951`.
