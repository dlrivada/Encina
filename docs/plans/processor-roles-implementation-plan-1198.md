# Implementation Plan: `Encina.Compliance.ProcessorAgreements` — Vendor Role per Purpose and "No Art. 28 Terms Available" Status

> **Issue**: [#1198](https://github.com/dlrivada/Encina/issues/1198)
> **Type**: Feature (SPEC-002 REQ-013, AC-013, scenario S17, tracking id P-10, P0)
> **Milestone**: EU regulatory readiness (parent EPIC #1186)
> **Complexity**: Medium-High (8 phases, two packages, no database provider matrix)
> **Estimated Scope**: ~700-900 lines of production code + ~1,500-1,900 lines of tests; three pull requests (see Scope)

---

## Summary

Record, **per vendor and purpose**, whether the vendor acts as **processor, independent controller or joint controller**, and whether **Art. 28 terms exist at all**, so that `[RequiresProcessor]` refuses (or, as an explicit opt-out, warns) when the vendor is not a processor for that purpose or has no valid Art. 28 terms, and so that the RoPA export lists the roles and "no Art. 28 terms available" (SPEC-002 REQ-013; a payment gateway is a processor for payments and an independent controller for fraud prevention; consumer mail and calendar accounts offer no Art. 28 terms, SPEC-002 note H rows H-R23, H-R33, H-R34).

### What the issue asks and what the code does today

- The issue asks for `ProcessorRole` (`Processor`, `IndependentController`, `JointController`) per vendor and purpose; `Art28TermsStatus` (`Signed`, `Expired`, `Missing`, `NotAvailable`); `[RequiresProcessor]` that refuses or warns by role and status; and a RoPA export that lists roles and "no Art. 28 terms".
- The issue body is **settled by elimination** (comment of 2026-10-17): one role per vendor is wrong (rejected); the refusal is the default by SPEC-002 DEC-006 and warn-only is an explicit, logged opt-out; `NotAvailable` is listed in the RoPA export.
- Today `Processor` (`Model/Processor.cs`) and `ProcessorAggregate` (`Aggregates/ProcessorAggregate.cs`) hold identity, country, contact, parent and sub-processor authorisation, with no role and no purpose. `RequiresProcessorAttribute` (`Attributes/RequiresProcessorAttribute.cs`) has only `ProcessorId`; its XML example uses ids such as `"stripe-payments"` while `ProcessorValidationPipelineBehavior.DispatchAsync` (lines 151-159) parses the id as a `Guid` and returns a validation error otherwise, so the documented usage does not work (corrected in Phase 3).
- The behaviour (`ProcessorValidationPipelineBehavior.cs:211-283`) asks `IDPAService.HasValidDPAAsync` and, on failure in `Block` mode, `ValidateDPAAsync` for a detailed error. It never reads a role. **`ProcessorAgreementOptions.EnforcementMode` defaults to `Warn`** ("to support gradual adoption", `ProcessorAgreementOptions.cs`), which is the permissive default that AGENTS.md §3 forbids for a compliance gate; a test pins it (`tests/Encina.UnitTests/Compliance/ProcessorAgreements/ProcessorAgreementOptionsTests.cs:17-20`).
- The RoPA lives in `Encina.Compliance.GDPR`: `IRoPAExporter.ExportAsync(IReadOnlyList<ProcessingActivity>, RoPAExportMetadata, ...)` (`Export/IRoPAExporter.cs`), implemented by `JsonRoPAExporter` and `CsvRoPAExporter`, both registered as singletons without dependencies (`src/Encina.Compliance.GDPR/ServiceCollectionExtensions.cs:90-91`). `ProcessingActivity.Recipients` is a list of strings; nothing links a recipient to a processor register. `Encina.Compliance.ProcessorAgreements.csproj` references `Encina.Compliance.GDPR`, not the reverse.
- **The processor register is not tenant-scoped.** `DefaultProcessorService.GetAllProcessorsAsync` (`Services/DefaultProcessorService.cs:242-256`) returns every tenant's processors, and `TenantId` is only passed at registration (lines 79 and 94); `DefaultDPAService` has the same shape (lines 87 and 100) and the package does not reference `Encina.Tenancy`. AC-043 ("a two-tenant test shows tenant A never reads ... tenant B's data") cannot hold for the new role reads until this is fixed (Design Choice 7).

### Scope

- **Affected packages**: `Encina.Compliance.ProcessorAgreements` (roles, validator, behaviour, options, observability) and `Encina.Compliance.GDPR` (shared vocabulary and the RoPA export), plus tests, READMEs, feature guide, coverage manifests, changelog fragment, `PublicAPI.Unshipped.txt` of both.
- **Provider category**: none of the AGENTS.md §5 matrices; event-sourced on Marten (SPEC-002 DEC-008 (a), ADR-019); unit tests mock `IAggregateRepository`, integration tests run on Marten/PostgreSQL through Testcontainers (AGENTS.md §3). The RoPA export changes no persisted `ProcessingActivity` shape, so the 10-provider rule is not touched (the #1196 plan, which does change that shape, is separate).
- **Estimated files**: ~11 new source files, ~16 modified source files, ~14 test files, ~6 documentation files.
- **Pull request split** (exceeds 15 files and 1,500 lines): **PR 1** = Phases 1-2 (shared types, aggregate, events, projection, service, purpose validator; ~15 files); **PR 2** = Phases 3, 5 and 6 (attribute, behaviour, options default, cross-cutting, observability); **PR 3** = Phases 4, 7 (rest) and 8 (RoPA, remaining tests, documentation).
- **Event IDs**: 9486-9493 in `ComplianceProcessorAgreements = (9400, 9499)` (9400-9485 are used).

### Related open issues that touch the same files

- #1196 (P-08, Art. 9(2) conditions) edits `ProcessingActivity`, its persistence and both RoPA exporters; this plan edits `RoPAExportMetadata` and the same exporters. Conflicts are additive (new columns or sections); land in either order and rebase.
- #1145 (US adequacy, transfer checks in S17) is closed and independent of this change; S17 (#1227) verifies both.
- #1251 (row claiming in outbox and scheduler processors) is about background processors, not vendor roles; #782 (the Marten migration of this module) is closed and is the base this plan builds on.

---

## Design Choices

<details>
<summary><strong>1. Where the shared vocabulary lives — <code>ProcessorRole</code>, <code>Art28TermsStatus</code> and the RoPA entry in <code>Encina.Compliance.GDPR</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Types in `Encina.Compliance.GDPR` (`Model/ProcessorRole.cs`, `Model/Art28TermsStatus.cs`, `Export/RoPAProcessorEntry.cs`); ProcessorAgreements uses them** | The RoPA exporters (in GDPR) can render roles without a new project reference; no cycle | The GDPR package names a processor concept (it already has `LawfulBasis`, `ProcessingActivity`, `Recipients`) |
| **B) Types in `Encina.Compliance.ProcessorAgreements`; GDPR references that package** | The concept lives with its register | `ProcessorAgreements.csproj` already references GDPR: B creates a project cycle |
| **C) Duplicate string values in both packages** | No shared type | Two vocabularies for one legal concept; the export and the check can drift |

### Chosen Option: **A — shared types in GDPR**

Class: Settled
Settled by: `src/Encina.Compliance.ProcessorAgreements/Encina.Compliance.ProcessorAgreements.csproj` (references `Encina.Compliance.GDPR`) and `src/Encina.Compliance.GDPR/Encina.Compliance.GDPR.csproj` (does not reference ProcessorAgreements)

### Rationale

- Enums use explicit values and no zero member: `ProcessorRole` `Processor = 1`, `IndependentController = 2`, `JointController = 3`; `Art28TermsStatus` `Signed = 1`, `Expired = 2`, `Missing = 3`, `NotAvailable = 4`.
- `RoPAProcessorEntry(string ProcessorName, string Purpose, ProcessorRole Role, Art28TermsStatus TermsStatus, string Country)` is the data the export needs and nothing more (no contact e-mail).

</details>

<details>
<summary><strong>2. Where the role is stored — on the processor aggregate, as a child collection keyed by purpose</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ProcessorRoleAssigned` and `ProcessorRoleRemoved` events on `ProcessorAggregate`, projected into `ProcessorReadModel.Roles`** | Same shape as the existing child collection (`AddSubProcessor`, `RemoveSubProcessor`); the role is part of the vendor's history and audit trail; one stream per vendor | The aggregate grows two methods and two events |
| **B) A separate `ProcessorRoleAggregate` per (vendor, purpose)** | Roles change independently of the vendor | Another aggregate, projection, read model and registration (~6 files) for a small collection |

### Chosen Option: **A — events on the processor aggregate**

Class: Trivial
Settled by: `src/Encina.Compliance.ProcessorAgreements/Aggregates/ProcessorAggregate.cs` (`AddSubProcessor` and `RemoveSubProcessor` model a child collection with events on the same stream)

### Rationale

- The role entry is `ProcessorPurposeRole(string Purpose, ProcessorRole Role, bool Art28TermsAvailable)`; assigning a role for an existing purpose replaces it (one role per vendor and purpose, so the rejected "one role per vendor" is not reintroduced).
- `Purpose` is a purpose identifier in the same lowercase hyphen-separated style as `ProcessingActivity.Purpose` and `ConsentPurposes`; it carries no personal data.

</details>

<details>
<summary><strong>3. The terms status — the declaration is stored, the rest is derived when checked</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Store only `Art28TermsAvailable` (false means the vendor offers none: `NotAvailable`); derive `Signed`, `Expired`, `Missing` from the active DPA at check time** | Always current: a DPA that expires flips the status without a write; reuses `DefaultDPAService.ValidateDPAAsync` | The status is a function, not a stored field |
| **B) Store the full `Art28TermsStatus` on the role entry** | Simple read | Goes stale when a DPA expires or is renewed; two writers (DPA service and role service) must keep it in step |

### Chosen Option: **A — store the declaration, derive the status**

Class: Trivial
Settled by: `src/Encina.Compliance.ProcessorAgreements/Services/DefaultDPAService.cs:456-478` (`HasValidDPAAsync` and `ValidateDPAAsync` compute validity at check time from the active DPA)

### Rationale

- A static `Art28TermsStatusResolver.Resolve(bool termsAvailable, DPAValidationResult? validation)` returns `NotAvailable` when the declaration says so, `Missing` when there is no active DPA, `Expired` when `DaysUntilExpiration` is zero or negative, otherwise `Signed`.
- The DPA is per processor (`DPAAggregate.ProcessorId`), so one active DPA covers every purpose of that vendor; a per-purpose DPA is not part of this issue.

</details>

<details>
<summary><strong>4. How <code>[RequiresProcessor]</code> names the purpose — a required <code>Purpose</code> property</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Purpose` becomes `required` on the attribute (like `ProcessorId`); a request type without it does not compile** | Fail closed; the role check cannot be skipped by omission | Every existing use names its purpose (pre-1.0, no installed base) |
| **B) `Purpose` optional; without it only the DPA is checked** | Existing uses compile | The role check silently does not run on those requests: the hole this issue closes |

### Chosen Option: **A — `Purpose` required**

Class: Settled
Settled by: `AGENTS.md` §1 (pre-1.0, breaking changes encouraged when they improve the design) and §3 (compliance gates fail closed; missing context denies), issue #1198 Motivation (a vendor can be processor for one purpose and controller for another)

### Rationale

- A role other than `Processor` for that purpose (independent or joint controller) refuses the request, because `[RequiresProcessor]` states that the request needs a processor; a purpose with no recorded role refuses with `processor.role_not_recorded` (fail closed).
- The XML examples are corrected to use GUID ids (the behaviour parses a `Guid`).

</details>

<details>
<summary><strong>5. Enforcement default — Block, warn is an explicit opt-out</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Change the default of `ProcessorAgreementOptions.EnforcementMode` to `Block`; `Warn` stays as an explicit opt-out, logged at startup** | Compliance gates fail closed; one option governs the DPA check and the role check | Existing tests and sample configurations that relied on `Warn` must set it explicitly |
| **B) Keep `Warn` as default and add a separate `RoleEnforcementMode` defaulting to `Block`** | No change to the DPA check default | Two modes for one gate; the weaker default for the DPA check stays |

### Chosen Option: **A — default `Block`**

Class: Settled
Settled by: issue #1198 comment of 2026-10-17 ("`[RequiresProcessor]` refuses by default, and warn-only is an explicit, logged opt-out"), SPEC-002 DEC-006, `AGENTS.md` §3

### Rationale

- `Defaults_EnforcementMode_ShouldBeWarn` (`ProcessorAgreementOptionsTests.cs:17`) becomes `Defaults_EnforcementMode_ShouldBeBlock`; any test fixture that expects pass-through now sets `Warn` explicitly.
- A new startup log (EventId 9490) reports `Warn` and `Disabled` as active opt-outs.

</details>

<details>
<summary><strong>6. How the RoPA export receives the roles — entries on the export metadata, built by a provider in ProcessorAgreements</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `RoPAExportMetadata` gains `IReadOnlyList<RoPAProcessorEntry> Processors` (default empty); ProcessorAgreements adds a scoped `IProcessorRoPAEntryProvider` that builds the list; the exporters render a processors section with the role and the terms text** | Exporters stay dependency-free singletons (as registered today); the lifetimes of the scoped Marten reads are untouched | The application must call the provider and pass the list; an export built without it lacks processors |
| **B) Exporters resolve an `IRoPAProcessorSource` from DI and include it automatically** | Complete by default (AGENTS.md §3, registration completeness) | The exporters become scoped and take a dependency; their registration, tests and every caller change |

### Chosen Option: **A — entries on the metadata** (recommended, pending the maintainer)

Class: Dilemma

### Rationale

- We recommend A because it keeps `IRoPAExporter` a pure formatter (its signature takes activities and metadata) and the change small; the provider is the only coupling point.
- B wins when the maintainer weighs completeness by default above formatter purity: any application that registers ProcessorAgreements then gets roles in every export without remembering to pass them. The data shape (`RoPAProcessorEntry`) is the same, so switching later is a registration change.
- Export text: `NotAvailable` renders as "no Art. 28 terms available" (AC-013 requires "no Art. 28 terms" to appear); `Missing` renders as "Art. 28 terms missing"; `Expired` and `Signed` as their names. JSON uses a `processors` array with `processorName`, `purpose`, `role`, `art28Terms`, `country`; CSV appends a separate `# Processors` block with the same columns.

</details>

<details>
<summary><strong>7. Tenant scoping of the processor register — fix the defect first, then build the role reads on it</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Open a bug issue for the missing tenant scope of the processor and DPA reads (`artifacts/issues/processor-agreements-tenant-scoping.md`), fix it with the Consent/DSR pattern (`TryResolveTenantScope`, `RequireTenantContext`, `Encina.Tenancy` reference), then build this feature on it** | One tenant pattern in the package; the defect is fixed regardless of P-10; this plan's reads are tenant-safe from day one | Adds a prerequisite pull request |
| **B) Introduce the tenant-scope helper inside #1198 for the new reads only** | No prerequisite | #1198 grows by a cross-cutting change; the existing reads stay leaky; two pull requests touch the same helper |
| **C) Store the tenant on the new events only and leave filtering to #1257** | Smallest #1198 | Fails the issue's tenant-aware acceptance item |

### Chosen Option: **A — tenant defect first** (recommended, pending the maintainer)

Class: Dilemma

### Rationale

- We recommend A because `DefaultProcessorService.GetAllProcessorsAsync` (`Services/DefaultProcessorService.cs:242-256`) and the DPA queries return all tenants' records today, which is a defect on its own, and fixing it once gives #1198 a tested pattern (the same choice as the #1197 plan, its Design Choice 6).
- B wins when the maintainer wants #1198 to ship without waiting for another pull request and accepts the larger diff; Phase 2 already lists the helper's contract, so the tasks move into #1198 unchanged.

</details>

---

## Implementation Phases

### Phase 1: Shared Vocabulary, Events, Aggregate and Read Model

> **Goal**: The persisted shape: role per vendor and purpose.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Compliance.GDPR/Model/ProcessorRole.cs`** and **`Model/Art28TermsStatus.cs`** (new enums, explicit values, no zero member, XML docs citing GDPR Art. 4(7), 4(8), 26 and 28); **`Export/RoPAProcessorEntry.cs`** (new `sealed record`, Phase 4 uses it).
2. **`Model/ProcessorPurposeRole.cs`** in ProcessorAgreements (new `sealed record`): `Purpose`, `Role`, `Art28TermsAvailable`.
3. **`Events/ProcessorEvents.cs`** (modify): `ProcessorRoleAssigned(Guid ProcessorId, string Purpose, ProcessorRole Role, bool Art28TermsAvailable, DateTimeOffset OccurredAtUtc) : INotification` and `ProcessorRoleRemoved(Guid ProcessorId, string Purpose, DateTimeOffset OccurredAtUtc) : INotification`.
4. **`Aggregates/ProcessorAggregate.cs`** (modify): `Roles` dictionary by purpose; `AssignRole(...)` (replaces the entry of that purpose; `ArgumentException` for an empty purpose) and `RemoveRole(purpose, occurredAtUtc)` (throws `InvalidOperationException` when the purpose has no role or the processor is removed); `Apply` cases.
5. **`ReadModels/ProcessorReadModel.cs`** and **`ReadModels/ProcessorProjection.cs`** (modify): `Roles` list; handlers for the two events.
6. **`PublicAPI.Unshipped.txt`** of both packages (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of processor roles in Encina.Compliance.ProcessorAgreements and Encina.Compliance.GDPR (issue #1198).

CONTEXT:
- ProcessorAgreements is event-sourced on Marten: ProcessorAggregate (Aggregates/ProcessorAggregate.cs) has Register, Update, Remove, AddSubProcessor, RemoveSubProcessor; events in Events/ProcessorEvents.cs; ProcessorProjection and ProcessorReadModel in ReadModels/.
- ProcessorAgreements references Encina.Compliance.GDPR (not the reverse), so shared types live in GDPR.

TASK:
Add ProcessorRole, Art28TermsStatus and RoPAProcessorEntry to GDPR; add ProcessorPurposeRole, the two role events, the aggregate methods, the read model collection and the projection handlers to ProcessorAgreements, as listed in the plan's Phase 1 Tasks.

KEY RULES:
- Enums have explicit values and no zero member.
- One role per vendor and purpose: assigning for an existing purpose replaces it.
- Events and read models carry purpose and role only; no names or contact data beyond what the aggregate already holds.
- XML docs on every public member; no [Obsolete] or compatibility shims.

REFERENCE FILES:
- src/Encina.Compliance.ProcessorAgreements/Aggregates/ProcessorAggregate.cs
- src/Encina.Compliance.ProcessorAgreements/Events/ProcessorEvents.cs
- src/Encina.Compliance.ProcessorAgreements/ReadModels/ProcessorProjection.cs
- tests/Encina.UnitTests/Compliance/ProcessorAgreements/ProcessorAggregateTests.cs
```

</details>

---

### Phase 2: Services and the Purpose Validator

> **Goal**: Roles can be assigned, read, and combined with the DPA into one verdict.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Abstractions/IProcessorService.cs`** and **`Services/DefaultProcessorService.cs`** (modify): `AssignRoleAsync(Guid processorId, string purpose, ProcessorRole role, bool art28TermsAvailable, CancellationToken)`, `RemoveRoleAsync(Guid processorId, string purpose, CancellationToken)`; `GetProcessorAsync` already returns the roles through the read model. Errors through `ProcessorAgreementErrors` (`NotFound`, `StoreError`), never the exception message in logs.
2. **`Model/Art28TermsStatusResolver.cs`** (new, `internal static`): the pure function of Design Choice 3.
3. **`Abstractions/IProcessorPurposeValidator.cs`** and **`Services/DefaultProcessorPurposeValidator.cs`** (new, scoped): `ValueTask<Either<EncinaError, ProcessorPurposeValidation>> ValidateAsync(Guid processorId, string purpose, CancellationToken)` composing `IProcessorService` and `IDPAService`; **`Model/ProcessorPurposeValidation.cs`** (new record): `IsValid`, `Role?`, `TermsStatus`, `FailureReason` (`NotAProcessor`, `RoleNotRecorded`, `TermsNotAvailable`, `TermsMissing`, `TermsExpired` or none).
4. **`ProcessorAgreementErrors.cs`** (modify): `NotAProcessorCode = "processor.not_a_processor"`, `RoleNotRecordedCode = "processor.role_not_recorded"`, `TermsNotAvailableCode = "processor.terms_not_available"` and factories.
5. **`ServiceCollectionExtensions.cs`** (modify): `TryAddScoped<IProcessorPurposeValidator, DefaultProcessorPurposeValidator>()`; a DI test builds the provider with `ValidateOnBuild` and `ValidateScopes`.
6. Tenant scope: the reads used here go through the tenant-scoped helper of the prerequisite fix (Design Choice 7).
7. **`PublicAPI.Unshipped.txt`** (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of processor roles in Encina.Compliance.ProcessorAgreements (issue #1198).

CONTEXT:
- Phase 1 added roles to ProcessorAggregate and ProcessorReadModel.
- IDPAService.ValidateDPAAsync (Services/DefaultDPAService.cs:478) returns a DPAValidationResult (DPAId, IsValid, Status, MissingTerms, DaysUntilExpiration).

TASK:
Add AssignRoleAsync and RemoveRoleAsync, the internal Art28TermsStatusResolver, IProcessorPurposeValidator with its default implementation and ProcessorPurposeValidation, the three error codes and the DI registration, as listed in the plan's Phase 2 Tasks.

KEY RULES:
- A purpose with no recorded role is a failure (RoleNotRecorded), never a pass.
- A role other than Processor fails the check for a request that needs a processor.
- Status is derived at check time from the active DPA; never stored.
- ROP: a Left from any service is returned as Left; time comes from TimeProvider; no EncinaError.Message in logs.

REFERENCE FILES:
- src/Encina.Compliance.ProcessorAgreements/Services/DefaultProcessorService.cs
- src/Encina.Compliance.ProcessorAgreements/Services/DefaultDPAService.cs
- src/Encina.Compliance.ProcessorAgreements/ProcessorAgreementErrors.cs
- src/Encina.Compliance.ProcessorAgreements/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 3: Attribute, Behaviour and Enforcement Default

> **Goal**: `[RequiresProcessor]` refuses by role and terms status.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Attributes/RequiresProcessorAttribute.cs`** (modify): `public required string Purpose { get; set; }`; XML examples use GUID ids and a purpose.
2. **`ProcessorValidationPipelineBehavior.cs`** (modify `RunCheckAsync`, lines 211-238): after the DPA fast path, call `IProcessorPurposeValidator.ValidateAsync(processorId, attribute.Purpose)`; map each `FailureReason` to its error in `Block` mode (`BuildBlockError`, lines 300-338, gains the three new cases) and to a logged warning in `Warn` mode; the fast path (`HasValidDPAAsync`) stays for the DPA but a valid DPA alone no longer passes a request whose role check fails. Extract the role step into a private method to keep CRAP at or below 10; keep `Disabled` skipping everything.
3. **`ProcessorAgreementOptions.cs`** (modify): `EnforcementMode` defaults to `Block`; XML docs updated (the "gradual adoption" sentence removed); **`ProcessorAgreementOptionsValidator.cs`** unchanged.
4. **Startup log** (EventId 9490) when `EnforcementMode` is `Warn` or `Disabled`.
5. The existing attribute scan (if any) reports a `[RequiresProcessor]` whose `Purpose` is empty.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of processor roles in Encina.Compliance.ProcessorAgreements (issue #1198).

CONTEXT:
- ProcessorValidationPipelineBehavior (ProcessorValidationPipelineBehavior.cs) parses RequiresProcessorAttribute.ProcessorId as a Guid, calls HasValidDPAAsync, and on failure in Block mode builds an error from ValidateDPAAsync.
- ProcessorAgreementOptions.EnforcementMode currently defaults to Warn.

TASK:
Add the required Purpose to the attribute, call IProcessorPurposeValidator from the behaviour, map the new failure reasons to errors (Block) or warnings (Warn), change the default enforcement mode to Block and log the opt-out, as listed in the plan's Phase 3 Tasks. Update the tests that pinned the Warn default.

KEY RULES:
- Compliance gates fail closed (AGENTS.md section 3): Block is the default; Warn and Disabled are explicit, logged opt-outs.
- A Left from the validator or an exception in Block mode fails the request; in Warn mode it is logged and the request proceeds, as today.
- The processor id and purpose may appear in tags; no EncinaError.Message or exception message in logs.
- Keep the static per-generic-type AttributeCache.

REFERENCE FILES:
- src/Encina.Compliance.ProcessorAgreements/ProcessorValidationPipelineBehavior.cs
- src/Encina.Compliance.ProcessorAgreements/Attributes/RequiresProcessorAttribute.cs
- src/Encina.Compliance.ProcessorAgreements/ProcessorAgreementOptions.cs
- tests/Encina.UnitTests/Compliance/ProcessorAgreements/ProcessorAgreementOptionsTests.cs
```

</details>

---

### Phase 4: RoPA Export of Roles and "No Art. 28 Terms"

> **Goal**: The RoPA lists each processor with its role and terms status.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Compliance.GDPR/Export/RoPAExportMetadata.cs`** (modify): `IReadOnlyList<RoPAProcessorEntry> Processors` with an empty default; **`JsonRoPAExporter.cs`** and **`CsvRoPAExporter.cs`** (modify): the `processors` array / `# Processors` block with the text of Design Choice 6; an empty list renders an empty array and no block.
2. **`Abstractions/IProcessorRoPAEntryProvider.cs`** and **`Services/DefaultProcessorRoPAEntryProvider.cs`** in ProcessorAgreements (new, scoped): `ValueTask<Either<EncinaError, IReadOnlyList<RoPAProcessorEntry>>> GetEntriesAsync(CancellationToken)` — one entry per (active processor, purpose) with the derived terms status; sorted by processor name then purpose; registered with `TryAddScoped`.
3. Logs EventId 9493 (entry count); no processor contact data in the entries.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of processor roles across Encina.Compliance.GDPR and Encina.Compliance.ProcessorAgreements (issue #1198).

CONTEXT:
- IRoPAExporter.ExportAsync(IReadOnlyList<ProcessingActivity>, RoPAExportMetadata) is implemented by JsonRoPAExporter and CsvRoPAExporter (singletons, no dependencies).
- RoPAProcessorEntry, ProcessorRole and Art28TermsStatus live in Encina.Compliance.GDPR (Phase 1); the derived terms status comes from IProcessorPurposeValidator (Phase 2).

TASK:
Add Processors to RoPAExportMetadata, render it in both exporters, and add IProcessorRoPAEntryProvider with its default implementation, as listed in the plan's Phase 4 Tasks.

KEY RULES:
- NotAvailable renders as "no Art. 28 terms available"; Missing as "Art. 28 terms missing"; Expired and Signed by name.
- Exporters stay dependency-free; do not change their lifetimes.
- No contact e-mail or personal data in the entries; deterministic ordering.

REFERENCE FILES:
- src/Encina.Compliance.GDPR/Export/JsonRoPAExporter.cs
- src/Encina.Compliance.GDPR/Export/CsvRoPAExporter.cs
- src/Encina.Compliance.GDPR/Export/RoPAExportMetadata.cs
- tests/Encina.UnitTests/Compliance/GDPR/Export/JsonRoPAExporterTests.cs
```

</details>

---

### Phase 5: Cross-Cutting Integration

> **Goal**: Tenant, audit and validation follow the module's rules.

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-tenancy**: role reads and the RoPA provider use the tenant-scoped helper of the prerequisite fix; a two-tenant test shows tenant A never reads or exports tenant B's processors or roles (AC-043). With tenancy off the fixed default tenant applies.
2. **Audit trail**: `ProcessorRoleAssigned` and `ProcessorRoleRemoved` are the audit events; `TrackAuditTrail` behaviour of the package is unchanged.
3. **Validation**: attribute purpose, role and terms are validated by the purpose validator and the behaviour (Phases 2-3).
4. Not applicable (see matrix): caching, health checks, resilience, locks, transactions, idempotency, module isolation.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are verifying the cross-cutting integration of processor roles in Encina.Compliance.ProcessorAgreements (issue #1198).

CONTEXT:
- The prerequisite tenant-scoping fix gives the package a tenant-scope helper; events are the audit trail.

TASK:
Route every new read through the tenant-scoped helper and add the two-tenant test of the plan's Phase 7.

KEY RULES:
- No query without the tenant predicate; no new audit store; do not cache role or terms decisions (they depend on a DPA that can expire).

REFERENCE FILES:
- src/Encina.Compliance.ProcessorAgreements/Services/DefaultProcessorService.cs
- tests/Encina.IntegrationTests/Compliance/ProcessorAgreements/ProcessorAgreementAggregateIntegrationTests.cs
```

</details>

---

### Phase 6: Observability

> **Goal**: Instruments and logs for role checks, without personal data (REQ-062, AC-044).

<details>
<summary><strong>Tasks</strong></summary>

1. **`Diagnostics/ProcessorAgreementDiagnostics.cs`** (modify): counter `processor_agreement.pipeline.role_refusals.total` (tags `processor.role`, `processor.failure_reason`), counter `processor_agreement.role.operations.total` (tag `processor.operation`); the `ProcessorAgreement.PipelineCheck` activity gains `processor.role` and `processor.purpose` (the tenant tag `encina.tenant_id` is already set, `ProcessorValidationPipelineBehavior.cs:197-199`). The issue's proposed names (`encina.processor.refusals`, `encina.processor.check`) are replaced by the package's naming (`processor_agreement.*`, `ProcessorAgreement.PipelineCheck`).
2. **`Diagnostics/ProcessorAgreementLogMessages.cs`** (modify, the file uses `[LoggerMessage]`): 9486 `ProcessorRoleAssigned`, 9487 `ProcessorRoleRemoved`, 9488 `ProcessorRoleCheckPassed`, 9489 `ProcessorRoleRefused` (role and reason code), 9490 `ProcessorEnforcementOptOutActive` (startup, mode name), 9491 `ProcessorRoleNotRecorded`, 9492 `ProcessorTermsNotAvailable`, 9493 `ProcessorRoPAEntriesBuilt` (count); XML doc "Event IDs: 9486-9493 (see EventIdRanges.ComplianceProcessorAgreements)".
3. No range change: the range 9400-9499 is registered and the assembly mapped (`EncinaEventIdAllocationTests.cs:72`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of processor roles in Encina.Compliance.ProcessorAgreements (issue #1198).

CONTEXT:
- ProcessorAgreementDiagnostics owns the ActivitySource, Meter and tag constants; ProcessorAgreementLogMessages uses [LoggerMessage] with EventIds 9400-9485 (the last used id is 9485).

TASK:
Add the two counters and the activity tags, and the log messages 9486-9493 as listed in the plan's Phase 6 Tasks; call them from the services, validator, behaviour and provider.

KEY RULES:
- EventIds packed sequentially 9486-9493 inside 9400-9499.
- No EncinaError.Message, exception message, contact data or vendor name in logs, tags or metrics: processor id, role and purpose only.
- Run EncinaEventIdAllocationTests.

REFERENCE FILES:
- src/Encina.Compliance.ProcessorAgreements/Diagnostics/ProcessorAgreementDiagnostics.cs
- src/Encina.Compliance.ProcessorAgreements/Diagnostics/ProcessorAgreementLogMessages.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 7: Testing

> **Goal**: Every flag reaches its manifest target; AC-013 and S17 are proven.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/Compliance/ProcessorAgreements/Roles/`): aggregate tests for the two events, `Art28TermsStatusResolverTests` (every combination), `DefaultProcessorPurposeValidatorTests` (processor with signed DPA passes; independent and joint controller refuse; no role refuses; `NotAvailable` refuses; expired DPA refuses), behaviour tests in `Block`, `Warn` and `Disabled`, options default test (`Block`), `DefaultProcessorRoPAEntryProviderTests`; GDPR exporter tests for the processors section ("no Art. 28 terms available" present in JSON and CSV); DI test with `ValidateOnBuild` and `ValidateScopes`.
2. **Guard**: public members of the services, validator and provider; `RoPAExportMetadata` with null processors.
3. **Contract**: `IProcessorPurposeValidator` and `IProcessorRoPAEntryProvider` against the real defaults.
4. **Property** (`tests/Encina.PropertyTests/Compliance/ProcessorAgreements/`): the refusal matrix over role, declaration and DPA state — a request passes if and only if the role is `Processor` and the status is `Signed`.
5. **Integration** (`[Collection(MartenCollection.Name)]`, PostgreSQL): S17 path — a gateway recorded as processor for payments and independent controller for fraud prevention; `[RequiresProcessor]` on fraud refuses, on payments passes; a consumer mail account with no terms is flagged and appears in the RoPA export; two-tenant isolation.
6. **Telemetry/privacy**: in-memory exporter test (AC-044); leak test: no vendor name, contact e-mail or exception message in tags, metrics or logs.
7. **Load and benchmark**: `.md` justifications (not concurrent or hot).
8. **Coverage manifests**: per-file targets and justifications in both `.github/coverage-manifest/Encina.Compliance.ProcessorAgreements.json` and `Encina.Compliance.GDPR.json` (via `mechanical-fixer`); measure each flag; CRAP gate on the changed behaviour methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of processor roles in Encina.Compliance.ProcessorAgreements (issue #1198).

CONTEXT:
- Tests live in tests/Encina.{Unit,Guard,Contract,Property,Integration}Tests/Compliance/ProcessorAgreements/ and .../GDPR/Export/. Unit tests mock IAggregateRepository (NSubstitute); integration tests use [Collection(MartenCollection.Name)] with MartenFixture.
- Assertions use Shouldly through Encina.Testing.Shouldly.

TASK:
Write the tests listed in the plan's Phase 7 Tasks; they must execute real package code.

KEY RULES:
- FakeTimeProvider; no Thread.Sleep; no swallowed exceptions.
- Property test: a request passes iff the role is Processor and the terms status is Signed.
- Include the S17 gateway and consumer-mail scenarios and the two-tenant test.
- Measure per-flag coverage against both manifests before reporting.

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/ProcessorAgreements/DefaultProcessorServiceTests.cs
- tests/Encina.IntegrationTests/Compliance/ProcessorAgreements/ProcessorAgreementAggregateIntegrationTests.cs
- tests/Encina.UnitTests/Compliance/GDPR/Export/JsonRoPAExporterTests.cs
```

</details>

---

### Phase 8: Documentation and Finalization

> **Goal**: Users know how to record roles and read the RoPA; the build is clean.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on every new public API (examples: gateway with two purposes, consumer mail with no terms).
2. `changelog.d/1198-processor-roles.added.md` (via `mechanical-fixer`), noting the changed default (`Block`) in a `changed` fragment `changelog.d/1198-processor-enforcement-default.changed.md`.
3. `src/Encina.Compliance.ProcessorAgreements/README.md`, `src/Encina.Compliance.GDPR/README.md` and the processor feature guide (via `docs-writer`).
4. `PublicAPI.Unshipped.txt` of both packages; verification: `dotnet build Encina.slnx --configuration Release` (0 warnings), the test projects of both packages, EventId architecture tests, `dotnet run --file .github/scripts/changelog-fragments.cs -- --check`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are finalising processor roles in Encina.Compliance.ProcessorAgreements (issue #1198).

CONTEXT:
- Documentation pages go to the docs-writer; changelog fragments and PublicAPI lines to the mechanical-fixer.

TASK:
Complete the plan's Phase 8 Tasks and run the verification commands.

KEY RULES:
- No hand-typed coverage figures in documentation (covref markers).
- Document the new Block default and the explicit Warn opt-out.
- Zero build warnings.

REFERENCE FILES:
- src/Encina.Compliance.ProcessorAgreements/README.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Provision | Relevance |
|----------|-----------|-----------|
| GDPR | Art. 4(7), 4(8), 26, 28 | Controller, processor, joint controllers, processor terms |
| GDPR | Art. 30(1)(d) | The RoPA lists the recipients, here the processors and their roles |
| SPEC-002 | REQ-013, AC-013, S17, note H (H-R23, H-R33, H-R34), DEC-006, DEC-008, DEC-011, REQ-061, REQ-062 | Requirement, acceptance, gateway and consumer-account cases, fail closed, Marten-only, shape before 1.0, tenancy, instrumentation |
| ADR-001, ADR-006, ADR-018, ADR-019, ADR-021 | ROP, cross-cutting, Marten, EventIds | Design rules followed |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ProcessorAggregate`, events, projection, read model | `src/Encina.Compliance.ProcessorAgreements/Aggregates/`, `Events/`, `ReadModels/` | Role child collection |
| `DefaultDPAService.HasValidDPAAsync` / `ValidateDPAAsync` | `Services/DefaultDPAService.cs:456-478` | Derived terms status |
| `ProcessorValidationPipelineBehavior` | `ProcessorValidationPipelineBehavior.cs` | Role check, error mapping, caches |
| `ProcessorAgreementOptions` and validator | `ProcessorAgreementOptions.cs` | Enforcement default |
| `ProcessorAgreementDiagnostics`, log messages | `Diagnostics/` | Instruments and EventIds 9486-9493 |
| `IRoPAExporter`, `JsonRoPAExporter`, `CsvRoPAExporter`, `RoPAExportMetadata` | `src/Encina.Compliance.GDPR/Export/` | RoPA section |
| `EncinaEventIdAllocationTests` | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs:72` | Assembly already mapped |
| `MartenFixture` and `MartenCollection` | `tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/` | Integration tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.ProcessorAgreements` (existing) | 9400-9499 (`EventIdRanges.ComplianceProcessorAgreements`) | Used: 9400-9485 |
| **This plan (#1198)** | **9486-9493** | Packed after 9485 in `ProcessorAgreementLogMessages.cs` |
| Left in the range | 9494-9499 | A further feature needs a second range |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New source | 11 | Enums (2), entry record, role record, resolver, validator interface and default, validation record, RoPA provider interface and default, errors addition |
| Modified source | 16 | Events, aggregate, read model, projection, service and interface, attribute, behaviour, options, errors, diagnostics, log messages, registration, metadata, two exporters |
| Tests | 14 | Unit 7, guard 1, contract 1, property 1, integration 1, leak 1, justifications 2 |
| Documentation | 6 | Two READMEs, feature guide, two changelog fragments, PublicAPI (two) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 library (no backward compatibility, ROP with Either<EncinaError, T>, TimeProvider for time, tenant-scoped reads). Encina.Compliance.ProcessorAgreements is event-sourced on Marten (PostgreSQL) and references Encina.Compliance.GDPR. Issue #1198 records a role (processor, independent controller, joint controller) per vendor and purpose and whether Art. 28 terms exist, makes [RequiresProcessor] refuse by role and terms, and lists roles in the RoPA export.

IMPLEMENTATION OVERVIEW:
1. Add ProcessorRole, Art28TermsStatus and RoPAProcessorEntry to GDPR; add role events, aggregate methods, read model and projection to ProcessorAgreements.
2. Add AssignRoleAsync/RemoveRoleAsync, the terms-status resolver and IProcessorPurposeValidator.
3. Make RequiresProcessorAttribute.Purpose required; call the validator from the behaviour; default EnforcementMode to Block.
4. Add Processors to RoPAExportMetadata, render them in both exporters, add IProcessorRoPAEntryProvider.
5. Use the tenant-scoped helper of the prerequisite fix; add counters, activity tags and EventIds 9486-9493.
6. Tests of every flag, S17 on Marten, two-tenant test; docs, changelog, PublicAPI.

KEY PATTERNS:
- Role per (vendor, purpose); the terms declaration is stored, the status is derived from the active DPA at check time.
- Fail closed: no recorded role, a controller role, missing, expired or unavailable terms all refuse in Block mode; Warn and Disabled are explicit opt-outs.
- No vendor name, contact data, EncinaError.Message or exception message in logs, tags or metrics; [LoggerMessage] with registered EventIds.
- TryAdd registrations; DI test with ValidateOnBuild and ValidateScopes.

REFERENCE FILES:
- src/Encina.Compliance.ProcessorAgreements/ (Aggregates, Events, Services, ProcessorValidationPipelineBehavior.cs, ProcessorAgreementOptions.cs)
- src/Encina.Compliance.GDPR/Export/
- docs/plans/processor-agreements-implementation-plan-410.md (original module plan)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Role and terms decisions depend on a DPA that can expire; caching would risk a stale pass |
| 2 | OpenTelemetry | ✅ | Counters and `ProcessorAgreement.PipelineCheck` tags with the tenant, no vendor names (Phase 6) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` EventIds 9486-9493 in `ComplianceProcessorAgreements` (ADR-021, Phase 6) |
| 4 | Health Checks | ❌ N/A | No new checkable dependency; `ProcessorAgreementHealthCheck` is unchanged |
| 5 | Validation | ✅ | Role and terms status are checked by `[RequiresProcessor]` through the purpose validator (Phases 2-3) |
| 6 | Resilience | ❌ N/A | No call to an external system |
| 7 | Distributed Locks | ❌ N/A | No shared state updated by concurrent hosts; the processor stream uses optimistic concurrency |
| 8 | Transactions | ❌ N/A | Single-aggregate writes |
| 9 | Idempotency | ❌ N/A | Not a message or request entry point; assigning the same role twice is a replace |
| 10 | Multi-Tenancy | ✅ | Tenant-aware per SPEC-002 REQ-061 on the prerequisite tenant-scoped reads (Phase 5) |
| 11 | Module Isolation | ❌ N/A | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ✅ | Role assignment and removal are audited events (Phase 5) |

---

## Prerequisites & Dependencies

### Blocking

- **Tenant scoping of the processor and DPA reads** (Design Choice 7, option A): the bug issue file `artifacts/issues/processor-agreements-tenant-scoping.md` is opened first; Phases 2 and 5 depend on its helper.

### Coordination

- **#1196** edits `ProcessingActivity` and both RoPA exporters; this plan edits `RoPAExportMetadata` and the exporters; rebase after whichever lands first.
- **#1257** owns the cross-module two-tenant suite; it reuses the tests of this plan and the prerequisite fix.
- **S17 (#1227)** verifies the whole path with the transfer checks of #1145.

### Deferred

- A per-purpose DPA (a DPA that covers only some purposes of a vendor): not part of REQ-013; the DPA stays per processor.

---

## Next Steps

1. Maintainer answers the two Dilemmas (Design Choices 6 and 7) when work on #1198 starts; answers go in a `## Maintainer Decisions` section appended to this plan.
2. Open the tenant-scoping bug issue and implement it before PR 1.
3. Brief the worker for PR 1 (Phases 1-2); PR 2 and PR 3 follow in order.
