# Implementation Plan: `Encina.Compliance.DPIA` — Screening against the AEPD Art. 35.4 and 35.5 Lists

> **Issue**: [#1243](https://github.com/dlrivada/Encina/issues/1243)
> **Type**: Feature
> **Complexity**: Medium (5 phases, event-sourced module, no database provider matrix, about 28 files in 3 pull requests)
> **Estimated Scope**: ~900-1,200 lines of production code + ~1,400-1,800 lines of tests + two data files
> **Specification**: SPEC-002 REQ-050, AC-039 (tracking id P-41, priority P0); part of #1186
> **Depends on**: #816 (introduces `IDPIACriteriaProvider`; its plan is in the omnibus family)

---

## Summary

`Encina.Compliance.DPIA` screens a processing activity against the lists of the Spanish supervisory authority (AEPD): processing that requires a data protection impact assessment (GDPR Art. 35(4)) and processing that does not (Art. 35(5)). The two lists are loadable criteria sets served through the criteria-provider abstraction that #816 introduces (`IDPIACriteriaProvider`), with the national lists as the default (the EDPB harmonised list of the Digital Omnibus draft is #816 and stays off by default, SPEC-002 REQ-024). The screening result names the list entries that match.

**What exists today** (verified on `origin/main`): `IDPIAAssessmentEngine` (`src/Encina.Compliance.DPIA/Abstractions/IDPIAAssessmentEngine.cs`) evaluates six built-in `IRiskCriterion` classes (`src/Encina.Compliance.DPIA/RiskCriteria/`) against a `DPIAContext` (`RequestType`, `ProcessingType`, `DataCategories`, `HighRiskTriggers` with the constants of `HighRiskTriggers.cs`, `Template`, `Metadata`) and returns a `DPIAResult` (`OverallRisk`, `IdentifiedRisks`, `ProposedMitigations`, `RequiresPriorConsultation`, `AssessedAtUtc`). The criteria are the EDPB/WP 248 families; no national list exists. `RequiresDPIAAsync(Type)` in `DefaultDPIAAssessmentEngine` only checks for `[RequiresDPIA]` on the request type. The aggregate `DPIAAggregate` already carries `TenantId` and `ModuleId`, and `DPIAEvaluated` records the result of an assessment in the Marten event stream.

**What this plan delivers**

1. AEPD list data: two versioned embedded resources, one per list, each entry with an id, the list kind, a description, a machine-evaluable rule over `DPIAContext` and its source reference (AEPD document, item number, date).
2. `AepdCriteriaProvider`, an implementation of the #816 abstraction that loads and validates the resources once.
3. A screening step (`DPIAScreening`) that matches a `DPIAContext` and returns a result naming the matching entries per list; the engine's assessment consults it and `DPIAResult` and `DPIAEvaluated` record the matches.
4. Observability (counter, span, three log events) and tenant-aware recording (REQ-061, REQ-062).

**Out of scope**: the EDPB harmonised list and its loader (#816); other member states' lists; the Marten persistence and read models of the DPIA lifecycle (unchanged); an interactive transcription tool for the lists (the data is transcribed once and reviewed).

**Standards**: GDPR Art. 35(3), 35(4), 35(5), 35(7); EDPB WP 248 rev.01; AEPD list of processing that requires a DPIA (Art. 35.4) and the AEPD list of processing that does not (Art. 35.5); SPEC-002 REQ-024, REQ-050, REQ-061, REQ-062, AC-039, AC-043, AC-044, note F and G15.

**Affected packages**: `Encina.Compliance.DPIA` only (new folder `NationalLists/`, named after the feature as AGENTS.md section 4 asks).

**Provider category**: none for the 10 database providers: the module is event-sourced on Marten (PostgreSQL) and SPEC-002 DEC-008 (a) keeps the nine event-sourced compliance modules Marten-only in 1.0 (ADR-019). Integration tests run against Marten on PostgreSQL through Testcontainers.

---

## Design Choices

Two Dilemmas are open (1 and 2); the maintainer answers them when work on the issue starts. Choices 3 to 6 are Settled or Trivial.

<details>
<summary><strong>1. How a list entry matches a processing activity</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Automatic matching.** Each entry carries a declarative rule (all-of / any-of over `HighRiskTriggers` constants, data categories and `ProcessingType`) authored in the data file next to its source reference | The engine screens without the application naming the entry; one test per entry over both lists (AC-039) | The rule is an interpretation of narrative text; a wrong reading is a wrong screening, so each entry is reviewed against the AEPD document |
| **B) Declaration by the application.** `DPIAContext` names the AEPD entries it believes apply and the engine only validates ids and reports them | No interpretation inside Encina; deterministic | The controller must read the list itself; nothing is "screened"; the requirement says the result "names the matching list entries" |

### Chosen Option: **A — Automatic matching with a reviewed rule per entry** (recommended, pending the maintainer)

### Rationale

- The module exists to take this work off the controller; the existing criteria already work from `HighRiskTriggers` and `DataCategories`, so the context carries what the rules need. The source reference in each entry lets an auditor check the reading.
- B wins when the maintainer or the data protection officer of an application does not want any legal reading embedded in code and prefers the controller to assert the entries.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Effect of a 35.5 match on the DPIA requirement</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A 35.5 match is informational.** It is reported with the result but never lowers a requirement raised by a 35.4 entry or by the existing criteria (two or more WP 248 criteria); when both a requirement and a 35.5 entry match, the requirement stands and the conflict is logged | Fails closed: when in doubt a DPIA is required; no automatic "not required" | An activity that the authority excludes may still be flagged by the generic criteria |
| **B) A 35.5 match marks the activity "not required" when no 35.4 entry matches**, even if the generic criteria would require one | Honors the authority's exclusion list literally; less noise | A mis-modelled rule silently removes a DPIA; an automatic "not required" is the kind of silent weakening the compliance gates forbid |

### Chosen Option: **A — Informational 35.5 matches** (recommended, pending the maintainer)

### Rationale

- Compliance gates fail closed and only an explicit, logged opt-out disables a control (AGENTS.md section 3); an automatic exemption from a mis-modelled rule is not that.
- B wins for a controller with a documented decision to rely on the 35.5 list that wants the engine to apply it (to be added later as an explicit, logged option, not as default behavior).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Criteria as data, not code, with the national lists on by default</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two embedded, versioned JSON resources loaded by `AepdCriteriaProvider` through the #816 abstraction; the national lists are the default and the EDPB list stays off** | Lists change as data; the provider abstraction is shared with #816; REQ-024 respected | A schema, a loader and a validator to maintain |
| **B) Hard-code the criteria in the engine** | Fewest types | A list update becomes a code change; rejected on the issue |
| **C) Only the EDPB list** | Reuses #816 | A draft law as the only source; contradicts REQ-024 |

### Chosen Option: **A — Embedded data through the provider abstraction**

### Rationale

- The issue and its 2026-10-17 comment settle it: B and C are rejected in the body and the national lists are the current-law source (SPEC-002 note F, G15).
- Start-up validation loads both resources once into frozen collections and fails the start if a resource is missing, malformed or has a duplicate entry id.

**Class:** Settled
**Settled by:** #1243 (body and comment of 2026-10-17); SPEC-002 REQ-024, REQ-050, note F and G15; #816

</details>

<details>
<summary><strong>4. Recording the screening</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add `NationalListMatches` (entry ids with list kind, list version) to `DPIAResult` and to the `DPIAEvaluated` event, applied by `DPIAAggregate.Evaluate`** | The matches live in the same event as the assessment they explain; additive for Marten; no new stream | A screening without an assessment (`RequiresDPIAAsync`) is recorded only by metric and log |
| **B) A new event `DPIAScreened`** | Records screening independently of an assessment | Two events per assessment; the aggregate does not exist before an assessment |

### Chosen Option: **A — Extend `DPIAResult` and `DPIAEvaluated`**

### Rationale

- Row 12 of the matrix (audit trail) asks that screening results are recorded on the DPIA aggregate; `DPIAEvaluated` is where the result is stored, and the aggregate already carries tenant and module. Pre-1.0, so the event shape changes completely, with no upcaster.

**Class:** Trivial
**Settled by:** ADR-019 (event-sourced compliance modules); AGENTS.md section 1 (pre-1.0, no compatibility layers); SPEC-002 REQ-061

</details>

<details>
<summary><strong>5. EventIds and telemetry names</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Use the free tail of the existing range `ComplianceDPIA` 8800-8899 (8897, 8898, 8899); instruments on the existing `Encina.Compliance.DPIA` source and meter, ADR-054 names** | No new range; the package already maps to its range | Only three ids remain; a fourth message needs a new range from the family block |
| **B) A new range from the plan family block 6600-6649** | Room to grow | A second range for one assembly to maintain for three messages |

### Chosen Option: **A — 8897-8899 and ADR-054 names**

### Rationale

- 8897 `NationalListLoaded` (Information), 8898 `NationalListMatched` (Debug), 8899 `NationalListLoadFailed` (Error); Phase 1 confirms by grep and `EventIdRanges.GetAllRanges()` that the tail is free. The list-loading messages of the criteria provider itself belong to #816's range `ComplianceDPIACriteria` 6400-6429 (owned by its plan).

**Class:** Trivial
**Settled by:** AGENTS.md section 7 and ADR-021 (ids inside a package's existing range); ADR-054 settled point 4 (instrument names)

</details>

<details>
<summary><strong>6. Tenant awareness and what is never recorded</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Matches are recorded with the aggregate's tenant; the criteria data is global and read-only; no payload, response body or data subject identifier appears in any tag, log or event** | Meets REQ-061 and REQ-062; no per-tenant copies of a public list | A tenant cannot hold its own list (not a requirement) |
| **B) Per-tenant list overrides** | Flexibility | Contradicts "the national lists are the current-law source"; no requirement |

### Chosen Option: **A — Global read-only lists, tenant on the record**

### Rationale

- With tenancy off the fixed default tenant applies and no tenant configuration is needed (AC-043).

**Class:** Settled
**Settled by:** SPEC-002 REQ-061, REQ-062, AC-043, AC-044

</details>

---

## Implementation Phases

Each phase is one pull request of at most 15 files and 1500 changed lines. Phase 0 is the gate: the `IDPIACriteriaProvider` abstraction of #816 is merged (if it is not, Phase 1 stops and reports).

### Phase 1: List Model, Data Files and Provider

> **Goal**: Both AEPD lists are loadable, validated data. **Size**: about 12 files, 800 lines (the data files dominate).

<details>
<summary>Tasks</summary>

Folder `src/Encina.Compliance.DPIA/NationalLists/`, namespace `Encina.Compliance.DPIA.NationalLists`.

1. `AepdListKind.cs` (public enum: `RequiresDpia` for Art. 35(4), `NoDpiaRequired` for Art. 35(5)); `AepdListEntry.cs` (public sealed record: `string Id`, `AepdListKind Kind`, `string Description`, `AepdEntryRule Rule`, `string SourceReference`); `AepdEntryRule.cs` (public sealed record: `IReadOnlyList<string> AllOfTriggers`, `IReadOnlyList<string> AnyOfTriggers`, `IReadOnlyList<string> AnyOfDataCategories`, `string? ProcessingType`; trigger names are the constants of `HighRiskTriggers`).
2. `AepdCriteriaSet.cs` (public sealed record: `AepdListKind Kind`, `string Version`, `DateOnly PublishedOn`, `string SourceUrl`, `IReadOnlyList<AepdListEntry> Entries`).
3. `Resources/aepd-art-35-4.es.json` and `Resources/aepd-art-35-5.es.json` (embedded resources): the entries transcribed from the official AEPD documents with item numbers and publication dates; Phase 1 starts by confirming the current published versions of both lists and records the URL and date in each file (the transcription is reviewed against the source by a second person before the pull request opens).
4. `AepdCriteriaLoader.cs` (internal): reads and validates the resources (schema, unique ids, known trigger names, non-empty rule); returns `Either<EncinaError, AepdCriteriaSet>`.
5. `AepdCriteriaProvider.cs` (public sealed): implements `IDPIACriteriaProvider` of #816, loads both sets once into frozen collections; no I/O after start-up.
6. `src/Encina.Compliance.DPIA/DPIAErrors.cs` additions (`dpia.national_list_invalid`, `dpia.national_list_missing`, fixed messages); `DPIALogMessages.cs` 8897 and 8899; `PublicAPI.Unshipped.txt`.
7. Tests: loader (valid, missing, malformed, duplicate id, unknown trigger), provider, guard tests; `.github/coverage-manifest/Encina.Compliance.DPIA.json` per-file targets with one-sentence justifications.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #1243; plan docs/plans/dpia-aepd-screening-implementation-plan-1243.md; the abstraction IDPIACriteriaProvider comes from #816 (merged).
- Maintainer Decisions of the plan override the "recommended" choices. The module is event-sourced on Marten (ADR-019).

TASK:
Create the NationalLists model, the loader, the two embedded data resources and AepdCriteriaProvider with tests.
At most 15 files and 1500 lines.

KEY RULES:
- Criteria are data, not code; every entry has a source reference; loading validates and fails the start on any defect.
- EventIds 8897 and 8899 only (verify the tail is free first); [LoggerMessage] source generator.
- XML docs on every public member; RS0016 must not fire.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Abstractions/IDPIATemplateProvider.cs
- src/Encina.Compliance.DPIA/DefaultDPIATemplateProvider.cs
- src/Encina.Compliance.DPIA/Model/HighRiskTriggers.cs
```

</details>

---

### Phase 2: Matching, Screening and Engine Integration

> **Goal**: A context is screened and the matches are recorded. **Size**: about 10 files, 650 lines.

<details>
<summary>Tasks</summary>

1. `NationalLists/AepdEntryMatcher.cs` (internal static): evaluates an `AepdEntryRule` against a `DPIAContext` (case-insensitive, deterministic, no I/O).
2. `NationalLists/DPIAScreening.cs` (public sealed, `IDPIAScreening`): `ValueTask<Either<EncinaError, DPIAScreeningResult>> ScreenAsync(DPIAContext, CancellationToken)`; `DPIAScreeningResult` (public sealed record: matched entries per list, list versions, `bool Conflict` when a requirement and a 35.5 entry both match).
3. `DefaultDPIAAssessmentEngine` consults `IDPIAScreening` inside `BuildAssessmentAsync`: a 35.4 match raises a `RiskItem` ("National list Art. 35(4): <entry id>") at `RiskLevel.High`; a 35.5 match is recorded only (Design Choice 2); the matches go to the new `DPIAResult.NationalListMatches` (`IReadOnlyList<NationalListMatch>`). Keep the method within CRAP 10 by extracting the national-list step (the precedent is `ResolveTemplateAsync`, #1488).
4. `DPIAEvaluated` gains `NationalListMatches`; `DPIAAggregate.Evaluate` passes it; `DPIAAggregate` applies it; `DPIAProjection`/read model carry it.
5. Tests: one case per entry of both lists (AC-039), the conflict case, determinism (FsCheck property: the same context gives the same matches), guard tests, the engine with the screening mocked at the interface boundary, an aggregate test; `PublicAPI.Unshipped.txt`; manifest targets.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Phase 1 is merged. Plan Design Choices 1, 2 and 4 (Maintainer Decisions override the recommendations).
- Engine: src/Encina.Compliance.DPIA/DefaultDPIAAssessmentEngine.cs; event: Events/DPIAEvents.cs (DPIAEvaluated).

TASK:
Implement the matcher, DPIAScreening, the engine integration, and the additive event/aggregate/read-model fields, with tests.
At most 15 files and 1500 lines.

KEY RULES:
- A 35.5 match never lowers a requirement (unless the maintainer decided otherwise); a failure to screen is a Left, never "not required".
- Every changed or added method keeps CRAP <= 10; extract helpers instead of growing BuildAssessmentAsync.
- No personal data, payload or subject identifier in any event, tag or log.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs
- src/Encina.Compliance.DPIA/RiskCriteria/SpecialCategoryDataCriterion.cs
- src/Encina.Compliance.DPIA/ReadModels/DPIAProjection.cs
```

</details>

---

### Phase 3: Registration, Options and Observability

> **Goal**: The feature registers completely and is observable. **Size**: about 8 files, 400 lines.

<details>
<summary>Tasks</summary>

1. `ServiceCollectionExtensions.cs` (`AddEncinaDPIA`): `TryAddSingleton<AepdCriteriaProvider>` registered as the national provider of the #816 abstraction (the EDPB provider stays opt-in), `TryAddScoped<IDPIAScreening, DPIAScreening>`; `DPIAOptions` gains `bool UseNationalLists = true` with validation in `DPIAOptionsValidator`; a start-up check that loads both resources (`ValidateOnStart`).
2. Instruments on the existing `Encina.Compliance.DPIA` source and meter: `encina.dpia.screenings` (Counter, tag `outcome` with `requires`, `not_required_listed`, `no_match`, `error`; tenant tag only when multi-tenancy is on) and span `encina.dpia.screen` (`encina.tenant_id`); log `8898 NationalListMatched` at Debug with entry ids only.
3. Tests: DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes`, in both registration orders and with `UseNationalLists = false`; in-memory exporter test (AC-044) including that no tag or log carries a payload or direct identifier; EventId architecture tests (run, not new).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Phases 1-2 merged. AddEncinaDPIA is in src/Encina.Compliance.DPIA/ServiceCollectionExtensions.cs.

TASK:
Register the provider and screening completely, add the option, the instruments, the span and the tests.

KEY RULES:
- Registration completeness: every dependency resolved by the new services is registered, proven by a DI test with ValidateOnBuild and ValidateScopes.
- Instrument names lowercase dotted without _total or _count (ADR-054); tenant tag only when tenancy is on.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/ServiceCollectionExtensions.cs
- src/Encina.Compliance.DPIA/Diagnostics/DPIADiagnostics.cs
- src/Encina.Compliance.DPIA/DPIAOptionsValidator.cs
```

</details>

---

### Phase 4: Testing

> **Goal**: Every applicable flag reaches its target. **Size**: about 8 files, 600 lines.

<details>
<summary>Tasks</summary>

1. Unit and guard tests as in Phases 1-3 (all new public members).
2. Contract: the criteria-provider contract suite of #816 runs against `AepdCriteriaProvider`; `IDPIAScreening` contract.
3. Integration (`tests/Encina.IntegrationTests/Compliance/DPIA/`, Marten on PostgreSQL through Testcontainers, shared collection): an assessment stores `DPIAEvaluated` with the matches and the tenant; a two-tenant test shows tenant A never reads tenant B's DPIA records; with tenancy off the fixed default tenant applies.
4. Property: the same context gives the same matches (FsCheck). Load and benchmark: `.md` justifications (`tests/Encina.LoadTests/Compliance/DPIA/AepdScreening.md`, `tests/Encina.BenchmarkTests/.../AepdScreening.md`: not a concurrent hot path; both lists are small frozen collections).
5. Per-flag coverage at the targets of the manifest; CRAP of every new or changed method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Phases 1-3 merged. Event-sourced module: integration tests run against Marten on PostgreSQL via Testcontainers.

TASK:
Write the contract, integration and property tests and the two justification files; verify per-flag coverage and CRAP.

KEY RULES:
- Tests execute real package code; Shouldly through Encina.Testing.Shouldly; shared [Collection] fixture; no Thread.Sleep.
- Justification files follow the template of AGENTS.md section 9 (title, Status, Justification, Adequate Coverage, Date, Issue).

REFERENCE FILES:
- tests/Encina.IntegrationTests/ (an existing Marten collection definition)
- tests/Encina.ContractTests/Compliance/DPIA/IDPIAServiceContractTests.cs
```

</details>

---

### Phase 5: Documentation and Finalization

> **Goal**: The feature is documented and recorded. **Size**: about 5 files.

<details>
<summary>Tasks</summary>

1. XML docs with examples on all public APIs; package README section on the national lists.
2. Configuration guide: `docs/features/` DPIA page gains "National lists (AEPD)" with the list versions, the meaning of a 35.5 match and how to switch the lists off; by the docs-writer agent.
3. `changelog.d/1243-dpia-aepd-screening.added.md`; `ROADMAP.md` and `docs/INVENTORY.md` when affected. No ADR: the choices follow ADR-019 and the #816 abstraction.
4. `dotnet build --configuration Release` with 0 warnings; `dotnet test` green; the pull request records the ADR-018 evaluation (tracing, metrics, logging, health checks).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Phases 1-4 merged. Documentation goes through docs-writer; the changelog fragment and PublicAPI lines through mechanical-fixer.

TASK:
Finish XML docs, the README and feature-page sections and the changelog fragment.

KEY RULES:
- English only; list versions cited from the data files, never typed twice; coverage figures only through covref markers.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/README.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | What it asks of this feature |
|--------|-----------|------------------------------|
| GDPR | Art. 35(3), 35(4), 35(5), 35(7) | Cases that require a DPIA; supervisory-authority lists of processing that does and does not require one |
| EDPB WP 248 rev.01 | Nine high-risk criteria | The existing six criteria; the generic "two or more criteria" rule that a 35.5 match must not silently override |
| AEPD | Art. 35.4 list and Art. 35.5 list | The two national criteria sets, transcribed with item numbers and dates |
| SPEC-002 | REQ-024, REQ-050, REQ-061, REQ-062, AC-039, AC-043, AC-044 | National lists by default, EDPB list off; tests over both lists; tenant-aware; instrumented without personal data |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| Assessment engine | `src/Encina.Compliance.DPIA/DefaultDPIAAssessmentEngine.cs` | Hosts the national-list step |
| Context and result | `src/Encina.Compliance.DPIA/Model/DPIAContext.cs`, `DPIAResult.cs`, `HighRiskTriggers.cs` | Inputs of the rules; result extended |
| Aggregate and events | `src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs`, `Events/DPIAEvents.cs` | `DPIAEvaluated` records the matches with tenant and module |
| Criteria provider (from #816) | `IDPIACriteriaProvider` | Abstraction the AEPD provider implements |
| Diagnostics | `src/Encina.Compliance.DPIA/Diagnostics/DPIADiagnostics.cs`, `DPIALogMessages.cs` | Source, meter, log messages |
| Registration | `src/Encina.Compliance.DPIA/ServiceCollectionExtensions.cs` | `AddEncinaDPIA` |
| Coverage manifest | `.github/coverage-manifest/Encina.Compliance.DPIA.json` | Per-file, per-flag targets |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.DPIA` | 8897-8899 inside `ComplianceDPIA` 8800-8899 | Existing range; 8897 `NationalListLoaded`, 8898 `NationalListMatched`, 8899 `NationalListLoadFailed`; verify the tail is free in Phase 1 |
| `ComplianceDPIACriteria` (#816) | 6400-6429 | Owned by the #816 plan (criteria-provider messages) |
| Plan family block | 6600-6649 | Not used by this plan (assigned to #687 and #688) |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Source (`NationalLists/`, engine, aggregate, events, registration, diagnostics) | 14 | Edits to existing files are counted |
| Data resources | 2 | `aepd-art-35-4.es.json`, `aepd-art-35-5.es.json` |
| Tests (unit, guard, contract, property, integration) | 10 | Under `tests/Encina.*Tests/Compliance/DPIA/` |
| Justification files (load, benchmark) | 2 | `.md` |
| Manifest, PublicAPI, documentation, changelog | 5 | Docs through docs-writer |
| **Total** | **~33** | In 3 pull requests of at most 15 files |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0, Railway Oriented Programming, no compatibility layers. Rules: AGENTS.md.
- Issue #1243 adds the AEPD Art. 35.4 / 35.5 lists to Encina.Compliance.DPIA (event-sourced on Marten, ADR-019) through the
  IDPIACriteriaProvider abstraction of #816. The Maintainer Decisions of the plan override its recommendations.

IMPLEMENTATION OVERVIEW:
1. NationalLists model, two embedded data files with source references, loader and AepdCriteriaProvider.
2. Matcher, DPIAScreening, engine integration, DPIAResult/DPIAEvaluated/aggregate carry the matches.
3. Registration (UseNationalLists), instruments, span, EventIds 8897-8899.
4. Tests of every flag including Marten integration with tenants; documentation.

KEY PATTERNS:
- Data not code; start-up validation; a failure to screen is a Left, never "not required"; a 35.5 match never lowers a requirement by default.
- Tenant on the record; no payload or data subject identifier in tags, logs or events; TimeProvider for time.
- DI test with ValidateOnBuild and ValidateScopes; CRAP <= 10 on changed methods.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DefaultDPIAAssessmentEngine.cs
- src/Encina.Compliance.DPIA/Events/DPIAEvents.cs
- src/Encina.Compliance.DPIA/ServiceCollectionExtensions.cs
- .github/coverage-manifest/Encina.Compliance.DPIA.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | Phase 1: both criteria sets are loaded once into frozen collections (no `ICacheProvider` needed for static embedded data) |
| 2 | OpenTelemetry | ✅ | Phase 3: counter `encina.dpia.screenings` and span `encina.dpia.screen`, tenant as attribute when tenancy is on, no payloads |
| 3 | Structured Logging | ✅ | Phases 1 and 3: `[LoggerMessage]` 8897-8899 inside the registered `ComplianceDPIA` range |
| 4 | Health Checks | ❌ | No new checkable dependency; the data is embedded and validated at start-up |
| 5 | Validation | ✅ | Phases 1 and 3: loader and options validated at start (`ValidateOnStart`) |
| 6 | Resilience | ❌ | No call to an external system |
| 7 | Distributed Locks | ❌ | No shared state updated by concurrent hosts |
| 8 | Transactions | ❌ | Single-operation writes through the aggregate |
| 9 | Idempotency | ❌ | Not a message or request entry point |
| 10 | Multi-Tenancy | ✅ | Phases 2 and 4: matches recorded with the aggregate's tenant; two-tenant test; fixed default tenant when tenancy is off (REQ-061) |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; the aggregate's existing `ModuleId` is untouched |
| 12 | Audit Trail | ✅ | Phase 2: screening matches recorded in `DPIAEvaluated` on the DPIA aggregate |

---

## Prerequisites & Dependencies

### Required Prerequisites

- #816 merged with `IDPIACriteriaProvider` (the abstraction this plan implements); its plan owns the abstraction, the EDPB loader and the range 6400-6429.
- Confirmation of the current AEPD publications for both lists (Phase 1 task 3): version, publication date and URL.

### Recommended (Not Blocking)

- #686 (applicability resolver): when it lands the DPIA gate consults it; this plan does not depend on it.

---

## Next Steps

1. Link this plan from #1243 and #816.
2. When #1243 is picked up, re-check each Design Choice against the merged #816 abstraction; present the two Dilemmas to the maintainer and record the answers in `## Maintainer Decisions`.
3. Ship Phases 1, 2-3 and 4-5 as separate pull requests referencing #1243; the last one carries `Fixes #1243`.
