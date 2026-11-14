# Implementation Plan: `Encina.Compliance.DPIA` — Screening against the AEPD Art. 35.4 and 35.5 Lists

> **Issue**: [#1243](https://github.com/dlrivada/Encina/issues/1243)
> **Type**: Feature
> **Complexity**: Medium (7 phases, event-sourced module, no database provider matrix, 83 file changes in 7 pull requests; see Estimated File Count)
> **Estimated Scope**: about 5,750 changed lines in total (production code, two data files, tests, manifest and documentation), per pull request in Estimated File Count
> **Specification**: SPEC-002 REQ-050, AC-039 (tracking id P-41, priority P0); part of #1186
> **Depends on**: #816 (introduces `IDPIACriteriaProvider`, the criteria catalog and `LoadedCriteriaRiskCriterion`; its plan is merged in `docs/plans/`)

---

## Summary

`Encina.Compliance.DPIA` screens a processing activity against the lists of the Spanish supervisory authority (AEPD): processing that requires a data protection impact assessment (GDPR Art. 35(4)) and processing that does not (Art. 35(5)). The two lists are loadable criteria sets registered through the criteria-provider abstraction of #816 (`IDPIACriteriaProvider`, source origin `NationalList`), with the national lists as the default (the EDPB harmonised list of the Digital Omnibus draft is #816 and stays off by default, SPEC-002 REQ-024). The screening result names the list entries that match.

**What exists today** (verified on `origin/main`): `IDPIAAssessmentEngine` (`src/Encina.Compliance.DPIA/Abstractions/IDPIAAssessmentEngine.cs`) evaluates six built-in `IRiskCriterion` classes (`src/Encina.Compliance.DPIA/RiskCriteria/`) against a `DPIAContext` (`RequestType`, `ProcessingType`, `DataCategories`, `HighRiskTriggers` with the constants of `HighRiskTriggers.cs`, `Template`, `Metadata`) and returns a `DPIAResult` (`required init` properties `OverallRisk`, `IdentifiedRisks`, `ProposedMitigations`, `RequiresPriorConsultation`, `AssessedAtUtc`; `IsAcceptable` is `OverallRisk <= RiskLevel.Medium`). The criteria are the EDPB/WP 248 families; no national list exists. `RequiresDPIAAsync(Type)` in `DefaultDPIAAssessmentEngine` and `DPIARequiredPipelineBehavior` only check for `[RequiresDPIA]` on the request type. The aggregate `DPIAAggregate` already carries `TenantId` and `ModuleId`, and the positional record `DPIAEvaluated` records the result of an assessment in the Marten event stream. The #816 types (`IDPIACriteriaProvider`, `DPIACriterion`, `DPIACriteriaList`, `CriteriaSourceOrigin`, `LoadedCriteriaRiskCriterion`, `DPIAOptions.LoadCriteriaFrom*`) are planned by #816 and are not in `src/` yet.

**The #816 contract this plan builds on** (from the merged #816 plan): `IDPIACriteriaProvider.LoadCriteriaAsync(CancellationToken)` returns `ValueTask<Either<EncinaError, DPIACriteriaList>>`; a `DPIACriteriaList` carries `ListVersion`, `PublishedAtUtc` and `DPIACriterion(Id, Name, Description, DefaultRiskLevel, ApplicableSectors)` items; sources are registered through `DPIAOptions.LoadCriteriaFrom*` with `CriteriaSourceOptions` (`Name`, `Origin`, `IsRequired`); `LoadedCriteriaRiskCriterion` matches criterion ids against `DPIAContext.HighRiskTriggers`; a required source whose first load fails yields a `RiskItem` at `RiskLevel.VeryHigh` (category `CriteriaUnavailable`), so the assessment fails closed. An AEPD rule (all-of / any-of conditions, list kind) does not fit `DPIACriterion`, so Design Choice 7 settles how the AEPD data flows through that contract.

**What this plan delivers**

1. AEPD list data: two versioned embedded resources, one per list, each entry with an id, the list kind, an English description, a machine-evaluable rule over `DPIAContext` and its source reference (AEPD document, item number, date).
2. `AepdCriteriaProvider`, an `IDPIACriteriaProvider` registered by default as #816 sources with origin `NationalList`, which loads and validates the resources once.
3. A screening step (`DPIAScreening`) that matches a `DPIAContext` and returns a result naming the matching entries per list; the engine's assessment consults it and `DPIAResult` and `DPIAEvaluated` record the matches.
4. Observability (counter, span, three log events) and tenant-aware recording (REQ-061, REQ-062).

**Out of scope**: the EDPB harmonised list and its loader (#816); other member states' lists; changes to `DPIARequiredPipelineBehavior` (Design Choice 10); an interactive transcription tool for the lists (the data is transcribed once and reviewed).

**Standards**: GDPR Art. 35(3), 35(4), 35(5), 35(7), 36(1); EDPB WP 248 rev.01; AEPD list of processing that requires a DPIA (Art. 35.4) and the AEPD list of processing that does not (Art. 35.5); SPEC-002 REQ-024, REQ-050, REQ-061, REQ-062, AC-024, AC-039, AC-043, AC-044, note F and G15; ADR-054; ADR-062 (error category contract, merged).

**Affected packages**: `Encina.Compliance.DPIA` only (new folder `NationalLists/`, named after the feature as AGENTS.md section 4 asks).

**Provider category**: none for the 10 database providers: the module is event-sourced on Marten (PostgreSQL) and SPEC-002 DEC-008 (a) keeps the nine event-sourced compliance modules Marten-only in 1.0 (ADR-019). Integration tests run against Marten on PostgreSQL through Testcontainers.

---

## Design Choices

Nine Dilemmas are open (1, 2, 4, 7, 8, 9, 10, 11 and 13); the maintainer answers them when work on the issue starts. Choices 3, 5, 6, 12, 14 and 15 are Settled or Trivial. The phases below are written for the recommended options; where another option changes a phase, the phase says how.

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
| **A) A 35.5 match is informational.** It is reported with the result but never lowers a requirement raised by a 35.4 entry or by the existing criteria (two or more WP 248 criteria); when both a requirement and a 35.5 entry match, the requirement stands and the conflict is logged (EventId 8899, Design Choice 5) | Fails closed: when in doubt a DPIA is required; no automatic "not required" | An activity that the authority excludes may still be flagged by the generic criteria |
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
| **A) Two embedded, versioned JSON resources served by `AepdCriteriaProvider` through the #816 abstraction; the national lists are the default and the EDPB list stays off** | Lists change as data; the provider abstraction is shared with #816; REQ-024 respected | A schema, a loader and a validator to maintain |
| **B) Hard-code the criteria in the engine** | Fewest types | A list update becomes a code change; rejected on the issue |
| **C) Only the EDPB list** | Reuses #816 | A draft law as the only source; contradicts REQ-024 |

### Chosen Option: **A — Embedded data through the provider abstraction**

### Rationale

- The issue and its 2026-10-17 comment settle it: B and C are rejected in the body and the national lists are the current-law source (SPEC-002 note F, G15).
- How a load failure of the embedded data behaves is Design Choice 8; how the data reaches the assessment is Design Choice 7; whether the lists can be switched off is Design Choice 11.

**Class:** Settled
**Settled by:** #1243 (body and comment of 2026-10-17); SPEC-002 REQ-024, REQ-050, note F and G15; #816

</details>

<details>
<summary><strong>4. Recording the screening</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add `NationalListMatches` (entry ids with list kind, list version) to `DPIAResult` and to the `DPIAEvaluated` event, applied by `DPIAAggregate.Evaluate`** | The matches live in the same event as the assessment they explain; additive for Marten; no new stream | A screening without an assessment (`RequiresDPIAAsync`) is recorded only by metric and log; the positional `DPIAEvaluated` changes, so its callers change in the same pull request |
| **B) A new event `DPIAScreened`** | Records screening independently of an assessment; `DPIAEvaluated` unchanged | Two events per assessment; the aggregate does not exist before an assessment, so a screening without an assessment still has no stream |

### Chosen Option: **A — Extend `DPIAResult` and `DPIAEvaluated`** (recommended, pending the maintainer)

### Rationale

- Row 12 of the matrix (audit trail) asks that screening results are recorded on the DPIA aggregate; both options do that under ADR-019, and neither AGENTS.md section 1 nor REQ-061 chooses between them.
- I recommend A because the matches explain the assessment's risk items and belong in the same event; pre-1.0, the event shape changes completely, with no upcaster. B wins when the maintainer wants the screening history readable on its own (for example, a later screening of an existing assessment without a new evaluation).
- With B, Phase 3 adds `Events/DPIAScreened` and an `Apply` instead of changing `DPIAEvaluated`, and `DPIAProjectionTests.cs` does not change in Phase 3.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. EventIds and telemetry names</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Use the free tail of the existing range `ComplianceDPIA` 8800-8899 (8897, 8898, 8899, the ids the batch EventId registry assigns to #1243); loading messages come from #816's `DPIACriteriaLogMessages` (6400-6429), because the AEPD data is an ordinary #816 source; instruments on the existing `Encina.Compliance.DPIA` source and meter, ADR-054 names** | No new range; the package already maps to its range; no duplicate loading messages | Only three ids remain; a fourth message needs a new range from the registry |
| **B) A new range from a free block** | Room to grow | A second range for one assembly to maintain for three messages; not assigned to this plan by the registry |

### Chosen Option: **A — 8897-8899 and ADR-054 names**

### Rationale

- Which range owns which message: `ComplianceDPIA` 8897-8899 owns the screening messages of this plan; `ComplianceDPIACriteria` 6400-6429 (owned by #816) owns every message about loading a criteria source, including the AEPD sources (load succeeded with count and list version, source failed with error code). This plan allocates no id in 6400-6429.
- 8897 `NationalListsNotApplied` (Warning, logged once at start when the lists are declared not applicable, Design Choice 11); 8898 `NationalListMatched` (Debug, entry ids only); 8899 `NationalListConflict` (Information, a requirement and a 35.5 entry both match, Design Choice 2). If Design Choice 11 is A (no opt-out), 8897 is not allocated and the other two take 8897 and 8898, so the ids stay packed. Phase 2 confirms by grep and `EventIdRanges.GetAllRanges()` that the tail is free (the highest id in use on `main` is 8896).

**Class:** Trivial
**Settled by:** AGENTS.md section 7 and ADR-021 (ids inside a package's existing range, packed); the batch EventId registry of #3387 (DPIA 8897-8899 for #1243); ADR-054 settled point 4 (instrument names)

</details>

<details>
<summary><strong>6. Tenant awareness and what is never recorded</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Matches are recorded with the aggregate's tenant; the criteria data is global and read-only; no payload, response body or data subject identifier appears in any tag, log or event** | Meets REQ-061 and REQ-062; no per-tenant copies of a public list | A tenant cannot hold its own list (not a requirement) |
| **B) Per-tenant list overrides** | Flexibility | A capability nobody asked for; a public list copied per tenant |

### Chosen Option: **A — Global read-only lists, tenant on the record**

### Rationale

- No requirement asks for per-tenant lists, and an unrequested capability is not added (pay for what you use). With tenancy off the fixed default tenant applies and no tenant configuration is needed (AC-043).

**Class:** Trivial
**Settled by:** AGENTS.md section 1 (pay-for-what-you-use, every feature opt-in); SPEC-002 REQ-061, REQ-062, AC-043, AC-044

</details>

<details>
<summary><strong>7. How the AEPD data flows through the #816 contract</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extend the #816 model.** `DPIACriterion` gains an optional rule (all-of / any-of conditions) and a list kind (`Generic`, `RequiresDpia`, `NoDpiaRequired`); the #816 JSON schema gains the optional fields; `LoadedCriteriaRiskCriterion` evaluates the rule when present and never raises risk for a `NoDpiaRequired` criterion; `DPIAScreening` reads the matches from the catalog snapshot | One evaluation path for every list; other national lists can use rules too | Changes four #816 files (criterion, schema mapper, criterion evaluator, their tests) and the #816 JSON schema; the screening result still needs its own type, because `IRiskCriterion` returns only a `RiskItem?` |
| **B) Separate screening path.** `AepdCriteriaProvider` is registered as two #816 sources (`aepd-art-35-4`, `aepd-art-35-5`, origin `NationalList`, `IsRequired = true`) so the catalog, the health check and the failure policy see them; the entries it publishes carry ids in the reserved `aepd.` namespace; `LoadedCriteriaRiskCriterion` skips criteria from these two built-in sources; `DPIAScreening` evaluates the AEPD rules from the provider's frozen data | The #816 model and schema stay as merged; the 35.5 entries can never raise risk through #816; one change to one #816 file | Two consumers of the same data (catalog for status and versions, screening for rules) |

### Chosen Option: **B — Separate screening path, AEPD sources excluded from `LoadedCriteriaRiskCriterion`** (recommended, pending the maintainer)

### Rationale

- With neither choice, registering the AEPD data as an ordinary #816 source would make `LoadedCriteriaRiskCriterion` evaluate the 35.5 entries and raise risk from them (contradicting Design Choice 2), and not registering it would bypass #816's catalog, health data and fail-closed handling. Both options avoid both problems.
- I recommend B because it keeps the merged #816 contract intact and keeps every national-list decision (35.5 informational, conflict) in one place. A wins when the maintainer wants rule-based criteria to be a general #816 feature that other lists and custom sources can use.
- With A, Phase 1 also modifies the #816 `DPIACriterion` and its schema mapper, Phase 3 modifies `LoadedCriteriaRiskCriterion` to evaluate rules instead of skipping the AEPD sources, and the per-pull-request file lists are re-counted before work starts.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Load failure of the embedded AEPD data</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fail the start and keep #816's runtime backstop.** A start-up check (`ValidateOnStart`) loads both resources and fails the start if one is missing, malformed or has a duplicate entry id; if the data is unavailable at run time anyway, the screening returns a `Left` (`dpia.national_list_unavailable`) and the engine adds a `RiskItem` at `RiskLevel.VeryHigh`, as #816 does for an unavailable required source | The data ships in the assembly, so a defect is a packaging defect that shows at the first start; fail closed at run time as in #816 | Two mechanisms for one failure |
| **B) #816's semantics only.** No start-up check; the two required AEPD sources follow #816 Design Choice 5 (a failed first load gives the `VeryHigh` `CriteriaUnavailable` item until the source loads) | One mechanism for every source | A broken resource is noticed only from the risk items and the health check, never by a failed start |

### Chosen Option: **A — Fail the start; `VeryHigh` at run time** (recommended, pending the maintainer)

### Rationale

- Both options fail closed (AGENTS.md section 3); they differ in when a broken resource is noticed. I recommend A because embedded data cannot recover by retrying, so failing the start reports the defect at once. B wins when the maintainer wants every criteria source, built-in or not, to behave the same.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Risk level of a 35.4 match</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A fixed `RiskLevel.High`** for every 35.4 match ("National list Art. 35(4): <entry id>") | `IsAcceptable` becomes false (`OverallRisk <= Medium` fails), so a DPIA is required, without forcing prior consultation (`RequiresPriorConsultation` is `>= VeryHigh`) | Every listed processing weighs the same |
| **B) A per-entry level in the data file** (default `High`, reviewed per entry like the rule) | An entry the authority describes as very high risk can say so | A legal judgement per entry; `VeryHigh` forces Art. 36 prior consultation from the listing alone |

### Chosen Option: **A — Fixed `High`** (recommended, pending the maintainer)

### Rationale

- GDPR Art. 35(4) lists say that a DPIA is required; Art. 36(1) ties prior consultation to the residual risk the DPIA finds, not to the listing. `High` expresses exactly "a DPIA is required". B wins when the maintainer or a DPO wants the data file to grade entries.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Effect of the screening on the <code>[RequiresDPIA]</code> gate</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The gate is unchanged.** `RequiresDPIAAsync(Type)` and `DPIARequiredPipelineBehavior` keep checking only `[RequiresDPIA]`; the screening runs inside assessments and raises risk there; the documentation says a 35.4 match does not gate requests by itself | No new abstraction; the issue asks for screening, not for a new gate | A request whose processing matches a 35.4 entry but carries no attribute is never gated |
| **B) The behaviour screens each request** through a new per-request context source (for example an `IDPIAContextProvider`) and blocks or warns on a 35.4 match without an approved assessment | Gates listed processing even without the attribute | A new abstraction and a per-request context that the pipeline does not have today; larger than the issue |

### Chosen Option: **A — Gate unchanged, documented** (recommended, pending the maintainer)

### Rationale

- The pipeline knows only the request type, not the processing context the rules need. I recommend A and a clear statement in the feature page (Phase 7). B wins when the maintainer wants the national lists to protect requests the developer forgot to mark; it would then be a follow-up feature issue opened when B is chosen.

**Class:** Dilemma

</details>

<details>
<summary><strong>11. Switching the national lists off</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No switch.** The AEPD lists always apply once `AddEncinaDPIA` is called | Nothing can disable the control | A controller outside the AEPD's jurisdiction is screened against a list that does not apply to it |
| **B) `DPIAOptions.UseNationalLists` (bool, default `true`)**; `false` logs a Warning at start (8897) | Simple | A plain switch disables a compliance control for any reason, which the project's compliance rule does not allow |
| **C) A free-text declaration** `DeclareNationalListsNotApplicable(string justification)` | Justified and logged | Any non-blank text passes ("we do not want it" as well as a territorial reason), so it is a justified version of B, not a territorial one; a controller under another lead authority is left without any Art. 35(4) list |
| **D) A structured territorial declaration.** The bindable option `DPIAOptions.NationalListsNotApplicable` (type `NationalListsNonApplicability`, `null` by default) carries `Basis` (enum `NationalListsTerritorialBasis`: `GdprNotApplicable`, the controller has no EU establishment and processes no data of data subjects in the EU, GDPR Art. 3; or `OtherLeadAuthority`, the controller's lead supervisory authority is not the AEPD, GDPR Art. 56), `LeadAuthorityCountryCode` (ISO 3166-1 alpha-2, required for `OtherLeadAuthority`, not `ES`, absent for `GdprNotApplicable`), `Justification` (non-blank, never personal data) and `DeclarationVersion` (non-blank reference to the controller's documented decision). `DPIAOptionsValidator` fails the start when a field is missing or inconsistent, and, for `OtherLeadAuthority`, unless at least one other criteria source with origin `NationalList` is registered through #816 (the other authority's list replaces the AEPD's, so no gap). The AEPD sources are then not registered; Warning 8897 is logged once at start as an operational signal; and the audit record is the event: every `DPIAEvaluated` carries the whole declaration (`NationalListsNotApplied`, `null` when the lists apply), so an auditor reading the stream sees the basis, the authority, the justification and the version | Only a territorial reason passes, by construction; the alternative list is enforced; the reason is on every audit record, not only in a log | Two more public types, a validator rule that reads the #816 sources, and the event grows |

### Chosen Option: **D — Structured territorial declaration, recorded on every `DPIAEvaluated`** (recommended, pending the maintainer)

### Rationale

- A compliance control is never optional: the only reason to disable one is that it does not apply in the controller's territory, and then only justified, audited, and without leaving a gap with no alternative (the maintainer's compliance rule of 2026-10-30; AGENTS.md section 3: "the only opt-out is explicit and logged"). B and C do not meet it and are listed only to record why.
- I recommend D because the AEPD lists are territorial by nature and D makes the territory the only accepted reason. A wins when the maintainer prefers no opt-out at all for 1.0. When #686 (applicability resolver by jurisdiction, open) lands, the declaration can be derived from the resolver instead of configured by hand; that is a later change, not part of this plan.
- With A, Phases 5 and 6 are dropped and the EventIds move as Design Choice 5 says.

**Class:** Dilemma

</details>

<details>
<summary><strong>12. Comparison and order of matches</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `StringComparer.OrdinalIgnoreCase` for trigger names, data categories and processing type; matches ordered by list kind (35.4 first), then by entry id (ordinal)** | Same comparison as the existing criteria (`SpecialCategoryDataCriterion`); deterministic output for tests and events | None |
| **B) Case-sensitive comparison, data-file order** | Literal | Differs from the existing criteria; order depends on the file |

### Chosen Option: **A — Ordinal ignore case, ordered by kind and id**

### Rationale

- The existing DPIA criteria compare data categories with `StringComparer.OrdinalIgnoreCase` (#409); a deterministic order is what a property test and an event comparison need.

**Class:** Trivial
**Settled by:** #409 (the existing DPIA criteria compare with `StringComparer.OrdinalIgnoreCase`); AGENTS.md section 9 (deterministic tests)

</details>

<details>
<summary><strong>13. Language of the entry descriptions</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) English description plus the source reference** (document, item number, date); no Spanish text in the repository; file names without a language suffix (`aepd-art-35-4.json`, `aepd-art-35-5.json`) | Meets the English-only rule for code and documentation | An auditor reads the original wording in the AEPD document |
| **B) English description plus the verbatim Spanish item text** in a `SourceText` field | The original wording travels with the rule it was read into | Spanish text in the repository, which the English-only rule may not allow for data |

### Chosen Option: **A — English description and source reference** (recommended, pending the maintainer)

### Rationale

- Code, comments and documentation are English only (AGENTS.md section 10); whether a verbatim legal quotation inside a data file counts is not written down. I recommend A because the source reference is enough to check each reading. B wins when the maintainer wants the auditor to compare reading and original without leaving the file.

**Class:** Dilemma

</details>

<details>
<summary><strong>14. Outcome names of the screening counter</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Bounded `outcome` values `requires`, `listed_exempt_informational`, `no_match`, `not_applied`, `error`** | No value reads as "no DPIA required" (Design Choice 2); bounded cardinality | Longer names |
| **B) `requires`, `not_required_listed`, `no_match`, `error`** | Short | `not_required_listed` reads as "no DPIA required", which Design Choice 2 forbids |

### Chosen Option: **A — Names that never claim "not required"**

### Rationale

- Tag values are bounded and carry no data (ADR-054 D6); the name of a 35.5 match must say it is informational. `not_applied` exists only when Design Choice 11 is D, and is emitted by the engine (Phase 6).

**Class:** Trivial
**Settled by:** ADR-054 (settled point 4 and D6: bounded instrument tags without data); AGENTS.md section 3 (fail closed: no automatic "not required")

</details>

<details>
<summary><strong>15. Where the tenant tag of the screening telemetry comes from</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `DPIAScreening` takes `IRequestContextAccessor` (`src/Encina/Abstractions/IRequestContextAccessor.cs`) and tags the span and the counter with `encina.tenant_id` from `IRequestContext.TenantId` when multi-tenancy is on; with tenancy off no tenant tag is emitted** | The tenant comes from the same place every Encina store reads it; the screening owns its own instruments | One more constructor dependency, registered in Phase 3 |
| **B) Emit the tag in `DefaultDPIAService`**, which has the aggregate's tenant | No new dependency for the screening | The span is the screening's; a second file in Phase 2 and the screening's own counter untagged |

### Chosen Option: **A — Request context in `DPIAScreening`**

### Rationale

- REQ-061 takes the tenant from the request context (REQ-015) and ADR-054 D4 puts the tenant on spans and metrics only when multi-tenancy is on; A does exactly that in the class that owns the instruments, and B would split one instrument's tags across two classes.

**Class:** Trivial
**Settled by:** SPEC-002 REQ-061 (tenant from the request context); ADR-054 D4 (tenant as attribute and tag only when multi-tenancy is on)

</details>

---

## Implementation Phases

Each phase is one pull request of at most 15 files and 1500 changed lines; the file lists below are exact and count the tests, the existing test callers of every changed signature, the `.csproj`, the PublicAPI file and the coverage manifest. Phase 0 is the gate: the #816 pull requests that create `IDPIACriteriaProvider`, the catalog, `LoadedCriteriaRiskCriterion` and `DPIAOptions.LoadCriteriaFrom*` are merged (if they are not, Phase 1 stops and reports). No pull request uses a type created by a later one. Each pull request enters, per file and with a one-sentence justification, the coverage targets of the flags whose tests it adds.

### Phase 1: List Model, Data Files and Provider

> **Goal**: Both AEPD lists are loadable, validated data. **Size**: 15 files, about 1,150 lines (the data files dominate).

<details>
<summary>Tasks</summary>

Folder `src/Encina.Compliance.DPIA/NationalLists/`, namespace `Encina.Compliance.DPIA.NationalLists`.

1. `AepdListKind.cs` (public enum: `RequiresDpia` for Art. 35(4), `NoDpiaRequired` for Art. 35(5)); `AepdListEntry.cs` (public sealed record: `string Id`, `AepdListKind Kind`, `string Description` in English, `AepdEntryRule Rule`, `string SourceReference`); `AepdEntryRule.cs` (public sealed record: `IReadOnlyList<string> AllOfTriggers`, `IReadOnlyList<string> AnyOfTriggers`, `IReadOnlyList<string> AnyOfDataCategories`, `string? ProcessingType`; trigger names are the constants of `HighRiskTriggers`).
2. `AepdCriteriaSet.cs` (public sealed record: `AepdListKind Kind`, `string Version`, `DateOnly PublishedOn`, `string SourceUrl`, `IReadOnlyList<AepdListEntry> Entries`).
3. `Resources/aepd-art-35-4.json` and `Resources/aepd-art-35-5.json` (embedded resources; names per Design Choice 13): the entries transcribed from the official AEPD documents with item numbers and publication dates; Phase 1 starts by confirming the current published versions of both lists and records the URL and date in each file (the transcription is reviewed against the source by a second person before the pull request opens). `Encina.Compliance.DPIA.csproj` gains the two `EmbeddedResource` items.
4. `AepdCriteriaLoader.cs` (internal): reads and validates the resources (schema, unique ids in the `aepd.` namespace, known trigger names, non-empty rule); returns `Either<EncinaError, AepdCriteriaSet>`.
5. `AepdCriteriaProvider.cs` (public sealed): loads both sets once into frozen collections, exposes them to the screening, and implements `IDPIACriteriaProvider` of #816 for each list (one `DPIACriteriaList` per list kind, `ListVersion` from the data file, entries mapped to `DPIACriterion` with the `aepd.` ids; Design Choice 7). No I/O after the first load.
6. `src/Encina.Compliance.DPIA/DPIAErrors.cs` additions with fixed messages and an `ErrorCategory` through the category overload of `EncinaErrors.Create` (ADR-062): `dpia.national_list_missing` (`ErrorCategory.Internal`: the resource is part of the assembly), `dpia.national_list_invalid` (`ErrorCategory.Internal`), `dpia.national_list_unavailable` (`ErrorCategory.Internal`, used by Phase 2: the data is local, so ADR-062 rule R4 applies, not the outbound `Unavailable` of rule R7); `PublicAPI.Unshipped.txt`.
7. Tests: `tests/Encina.UnitTests/Compliance/DPIA/NationalLists/AepdCriteriaLoaderTests.cs` (valid, missing, malformed, duplicate id, unknown trigger, each asserting the error code and category), `.../AepdCriteriaProviderTests.cs` (both sets, versions, `DPIACriteriaList` mapping), `tests/Encina.GuardTests/Compliance/DPIA/NationalLists/AepdCriteriaProviderGuardTests.cs` (null arguments of the public records and of the provider's public members).
8. `.github/coverage-manifest/Encina.Compliance.DPIA.json`: unit and guard targets for every new file of this phase, each with a one-sentence justification.

Exact file list (15): the 8 new files of tasks 1-5 (`AepdListKind.cs`, `AepdListEntry.cs`, `AepdEntryRule.cs`, `AepdCriteriaSet.cs`, two JSON resources, `AepdCriteriaLoader.cs`, `AepdCriteriaProvider.cs`), `Encina.Compliance.DPIA.csproj`, `DPIAErrors.cs`, `PublicAPI.Unshipped.txt`, the manifest, and the 3 test files of task 7.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #1243; plan docs/plans/dpia-aepd-screening-implementation-plan-1243.md; the abstraction IDPIACriteriaProvider and
  DPIACriteriaList come from #816 (merged). Maintainer Decisions of the plan override the "recommended" choices.
- The module is event-sourced on Marten (ADR-019). Error codes take an ErrorCategory (ADR-062, merged).

TASK:
Create the NationalLists model, the loader, the two embedded data resources and AepdCriteriaProvider with tests.
Exactly the 15 files of the Phase 1 file list; at most 1500 lines.

KEY RULES:
- Criteria are data, not code; every entry has an English description and a source reference; loading validates.
- EncinaErrors.Create with an ErrorCategory (category overload), never the two-argument overload.
- XML docs on every public member; RS0016 must not fire; manifest targets per file with a justification.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Abstractions/IDPIATemplateProvider.cs
- src/Encina.Compliance.DPIA/DefaultDPIATemplateProvider.cs
- src/Encina.Compliance.DPIA/Model/HighRiskTriggers.cs
- src/Encina.Compliance.DPIA/DPIAErrors.cs
```

</details>

---

### Phase 2: Matching, Screening and Telemetry

> **Goal**: A context is screened, observably, without yet changing the engine. **Size**: 15 files, about 1,250 lines.

<details>
<summary>Tasks</summary>

1. `NationalLists/AepdEntryMatcher.cs` (internal static): evaluates an `AepdEntryRule` against a `DPIAContext` (Design Choice 12: ordinal ignore case, deterministic, no I/O).
2. `Abstractions/IDPIAScreening.cs` and `NationalLists/DPIAScreening.cs` (public sealed): `ValueTask<Either<EncinaError, DPIAScreeningResult>> ScreenAsync(DPIAContext, CancellationToken)`; `NationalLists/DPIAScreeningResult.cs` (public sealed record: matches ordered per Design Choice 12, list versions, `bool Conflict` when a requirement and a 35.5 entry both match); the constructor takes `AepdCriteriaProvider`, `IRequestContextAccessor` (Design Choice 15), `TimeProvider` and `ILogger<DPIAScreening>`, and no later phase changes it; `Model/NationalListMatch.cs` (public sealed record: entry id, list kind, list version). A `Left` (`dpia.national_list_unavailable`) when the provider's data is not loaded; never "not required".
3. `Diagnostics/DPIALogMessages.cs`: 8898 `NationalListMatched` (Debug, entry ids only) and 8899 `NationalListConflict` (Information); the XML docs name the range.
4. `Diagnostics/DPIADiagnostics.cs`: counter `encina.dpia.screenings` (tag `outcome` per Design Choice 14) and span `encina.dpia.screen`, both on the existing `Encina.Compliance.DPIA` source and meter; `encina.tenant_id` as span attribute and metric tag, emitted by `DPIAScreening` from `IRequestContext.TenantId` only when multi-tenancy is on (Design Choice 15, ADR-054 D4).
5. Tests: `tests/Encina.UnitTests/Compliance/DPIA/NationalLists/AepdEntryMatcherTests.cs` (one case per entry of both lists, AC-039), `.../DPIAScreeningTests.cs` (conflict, order, `Left` on unavailable data, in-memory exporter test of the counter and span including that no tag or log carries a payload or direct identifier, AC-044), `tests/Encina.GuardTests/Compliance/DPIA/NationalLists/DPIAScreeningGuardTests.cs`, `tests/Encina.PropertyTests/Compliance/DPIA/DPIAScreeningPropertyTests.cs` (FsCheck: the same context gives the same matches; the only property test of this plan), `tests/Encina.ContractTests/Compliance/DPIA/IDPIAScreeningContractTests.cs`, `tests/Encina.ContractTests/Compliance/DPIA/AepdCriteriaProviderContractTests.cs` (the #816 `IDPIACriteriaProvider` contract base run against `AepdCriteriaProvider`).
6. `PublicAPI.Unshipped.txt`; manifest: unit, guard, property and contract targets for the files of tasks 1-4 and the contract target of `AepdCriteriaProvider.cs`, each with a one-sentence justification.

Exact file list (15): `AepdEntryMatcher.cs`, `IDPIAScreening.cs`, `DPIAScreening.cs`, `DPIAScreeningResult.cs`, `NationalListMatch.cs`, `DPIALogMessages.cs`, `DPIADiagnostics.cs`, `PublicAPI.Unshipped.txt`, the manifest, and the 6 test files of task 5.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Phase 1 is merged. Plan Design Choices 1, 2, 5, 12 and 14 (Maintainer Decisions override the recommendations).
- Diagnostics: src/Encina.Compliance.DPIA/Diagnostics/DPIADiagnostics.cs and DPIALogMessages.cs (highest EventId 8896).

TASK:
Implement the matcher, DPIAScreening and its result types, EventIds 8898-8899, the counter and the span, with the tests of the
task list. Exactly the 15 files of the Phase 2 file list; at most 1500 lines.

KEY RULES:
- A 35.5 match never lowers a requirement (unless the maintainer decided otherwise); a failure to screen is a Left, never "not required".
- Instrument names lowercase dotted without _total (ADR-054); tenant tag only when tenancy is on (ADR-054 D4).
- No personal data, payload or subject identifier in any event, tag or log; CRAP <= 10 on every new method.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/RiskCriteria/SpecialCategoryDataCriterion.cs
- src/Encina.Compliance.DPIA/Diagnostics/DPIADiagnostics.cs
- tests/Encina.ContractTests/Compliance/DPIA/IDPIAServiceContractTests.cs
```

</details>

---

### Phase 3: Engine Integration, Event and Registration

> **Goal**: Every assessment is screened and records the matches; the AEPD sources are registered by default. **Size**: 15 files, about 1,100 lines.

<details>
<summary>Tasks</summary>

1. `DefaultDPIAAssessmentEngine.cs`: the constructor gains `IDPIAScreening screening` (null-checked); `BuildAssessmentAsync` calls an extracted `ScreenNationalListsAsync` helper (precedent `ResolveTemplateAsync`, #1488, keeps the method within CRAP 10): a 35.4 match adds a `RiskItem` at the level of Design Choice 9, a 35.5 match is recorded only (Design Choice 2), a `Left` adds a `RiskItem` at `RiskLevel.VeryHigh` (Design Choice 8).
2. `Model/DPIAResult.cs`: `NationalListMatches` (`IReadOnlyList<NationalListMatch>`, default empty, not `required`, so no existing caller changes).
3. `Events/DPIAEvents.cs`: the positional `DPIAEvaluated` gains `NationalListMatches` (Design Choice 4 A); `Aggregates/DPIAAggregate.cs`: `Evaluate` passes them and `Apply` keeps them.
4. `RiskCriteria/LoadedCriteriaRiskCriterion.cs` (#816): skips the criteria of the two built-in AEPD sources (Design Choice 7 B).
5. `ServiceCollectionExtensions.cs` (`AddEncinaDPIA`): `TryAddSingleton<AepdCriteriaProvider>`; the two AEPD sources (`aepd-art-35-4`, `aepd-art-35-5`, origin `NationalList`, `IsRequired = true`) registered as #816 `ConfiguredCriteriaSource` entries by default; `TryAddScoped<IDPIAScreening, DPIAScreening>`; `IRequestContextAccessor` is resolved from the Encina core registration and the DI test proves it; the start-up load check of Design Choice 8 A (`ValidateOnStart`).
6. Existing test callers of the changed signatures: `tests/Encina.GuardTests/Compliance/DPIA/DefaultDPIAAssessmentEngineGuardTests.cs` (new constructor argument plus its null test), `tests/Encina.UnitTests/Compliance/DPIA/DefaultDPIAAssessmentEngineTests.cs` (constructor plus the national-list step with the screening mocked: 35.4 raises the item, 35.5 does not, a `Left` gives `VeryHigh`), `tests/Encina.UnitTests/Compliance/DPIA/DPIAExceptionMessageLeakTests.cs` (constructor), `tests/Encina.UnitTests/Compliance/DPIA/ReadModels/DPIAProjectionTests.cs` (`new DPIAEvaluated(` with the new arguments only).
7. New and extended tests: `tests/Encina.UnitTests/Compliance/DPIA/Aggregates/DPIAAggregateTests.cs` (the event carries the matches), the #816 unit test file of `LoadedCriteriaRiskCriterion` (AEPD criteria are skipped, other `NationalList` sources are not), `tests/Encina.UnitTests/Compliance/DPIA/NationalLists/DPIANationalListsRegistrationTests.cs` (DI test building the provider with `ValidateOnBuild` and `ValidateScopes`; an application's own `IDPIAScreening` registered before or after `AddEncinaDPIA` is kept; and the AC-024 test `AddEncinaDPIA_DefaultOptions_LoadsBothAepdListsAndNoEdpbOrigin`: with default options the catalog holds both AEPD sources with origin `NationalList` and no `EdpbHarmonisedList` origin, completing the half of AC-024 that #816 leaves to this plan).
8. `PublicAPI.Unshipped.txt`; manifest: unit and guard targets of the changed source files of tasks 1-5, each with a one-sentence justification.

Exact file list (15): `DefaultDPIAAssessmentEngine.cs`, `DPIAResult.cs`, `DPIAEvents.cs`, `DPIAAggregate.cs`, `LoadedCriteriaRiskCriterion.cs`, `ServiceCollectionExtensions.cs`, `PublicAPI.Unshipped.txt`, the manifest, the 4 caller test files of task 6 and the 3 test files of task 7.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Phases 1-2 merged. Plan Design Choices 2, 4, 7, 8, 9 and 15 (Maintainer Decisions override the recommendations).
- Engine: src/Encina.Compliance.DPIA/DefaultDPIAAssessmentEngine.cs; event: Events/DPIAEvents.cs (DPIAEvaluated, positional).
- Existing callers: `git grep -n "new DefaultDPIAAssessmentEngine(\|new DPIAEvaluated(" -- tests src` before editing.

TASK:
Wire the screening into the engine, extend DPIAResult/DPIAEvaluated/DPIAAggregate, skip the AEPD sources in
LoadedCriteriaRiskCriterion, register everything in AddEncinaDPIA and write the tests of the task list, including the AC-024
default test. Exactly the 15 files of the Phase 3 file list; at most 1500 lines.

KEY RULES:
- Registration completeness: every dependency resolved by the new services is registered, proven by a DI test with
  ValidateOnBuild and ValidateScopes (AGENTS.md section 3).
- Fail closed: an unavailable list is a VeryHigh risk item, never "not required"; CRAP <= 10 on every changed method.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs
- src/Encina.Compliance.DPIA/ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/Compliance/DPIA/DefaultDPIAAssessmentEngineTests.cs
```

</details>

---

### Phase 4: Read Model and Marten Integration

> **Goal**: The read side shows the matches and the full flow runs against Marten. **Size**: 6 files, about 500 lines.

<details>
<summary>Tasks</summary>

1. `ReadModels/DPIAReadModel.cs` and `ReadModels/DPIAProjection.cs`: carry `NationalListMatches`.
2. Tests: `tests/Encina.UnitTests/Compliance/DPIA/ReadModels/DPIAProjectionTests.cs` (the projection applies the matches); `tests/Encina.IntegrationTests/Compliance/DPIA/DPIANationalListsIntegrationTests.cs` (Marten on PostgreSQL through Testcontainers, the shared collection fixture: an assessment stores `DPIAEvaluated` with the matches and the tenant; a two-tenant test shows tenant A never reads tenant B's DPIA records; with tenancy off the fixed default tenant applies).
3. `PublicAPI.Unshipped.txt`; manifest: unit targets of the two read-model files and integration targets of the engine, screening, aggregate and projection files, each with a one-sentence justification.

Exact file list (6): `DPIAReadModel.cs`, `DPIAProjection.cs`, `PublicAPI.Unshipped.txt`, the manifest, `DPIAProjectionTests.cs`, `DPIANationalListsIntegrationTests.cs`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Phases 1-3 merged. Event-sourced module: integration tests run against Marten on PostgreSQL via Testcontainers.

TASK:
Extend the read model and projection and write the projection and Marten integration tests. Exactly the 6 files of the
Phase 4 file list.

KEY RULES:
- Tests execute real package code; Shouldly through Encina.Testing.Shouldly; shared [Collection] fixture; no Thread.Sleep.
- Two-tenant isolation and the default tenant with tenancy off (SPEC-002 AC-043).

REFERENCE FILES:
- src/Encina.Compliance.DPIA/ReadModels/DPIAProjection.cs
- tests/Encina.IntegrationTests/Compliance/DPIA/DPIAAggregateIntegrationTests.cs
```

</details>

---

### Phase 5: Territorial Declaration, Validation and Registration

> **Goal**: The structured declaration of Design Choice 11 D is bindable, validated at start and switches the AEPD sources off. **Size**: 14 files, about 700 lines. Dropped when Design Choice 11 is A.

<details>
<summary>Tasks</summary>

1. `Model/NationalListsTerritorialBasis.cs` (public enum `GdprNotApplicable`, `OtherLeadAuthority`) and `Model/NationalListsNonApplicability.cs` (public sealed class with settable properties so configuration binding fills it: `Basis`, `LeadAuthorityCountryCode`, `Justification`, `DeclarationVersion`; XML docs say the justification must never hold personal data).
2. `DPIAOptions.cs`: `NationalListsNonApplicability? NationalListsNotApplicable` (default `null`). `DPIAOptionsValidator.cs`: when the property is set, fails the start for an undefined `Basis`, a blank `Justification` or `DeclarationVersion`, a missing or malformed `LeadAuthorityCountryCode` (or `ES`) for `OtherLeadAuthority`, a country code given for `GdprNotApplicable`, and, for `OtherLeadAuthority`, the absence of any #816 criteria source with origin `NationalList` in the options. Every rule fires on a bound value, so each is testable.
3. `ServiceCollectionExtensions.cs`: the AEPD sources are not registered when the declaration is set; `NationalLists/NationalListsApplicabilityReporter.cs` (internal hosted service) logs 8897 `NationalListsNotApplied` (Warning) once at start with the basis, the country code and the declaration version (operational signal only; the audit record is the event of Phase 6). Until Phase 6 merges the engine still screens, which is the protective side.
4. `Diagnostics/DPIALogMessages.cs`: 8897.
5. Tests: `tests/Encina.UnitTests/Compliance/DPIA/DPIAOptionsTests.cs` (extended: the declaration binds from configuration), `tests/Encina.UnitTests/Compliance/DPIA/DPIAOptionsValidatorTests.cs` (extended: one test per rule of task 2, including `OtherLeadAuthority` with and without another `NationalList` source), `tests/Encina.UnitTests/Compliance/DPIA/NationalLists/NationalListsApplicabilityReporterTests.cs` (the Warning with EventId 8897; nothing logged when the lists apply), `tests/Encina.UnitTests/Compliance/DPIA/NationalLists/DPIANationalListsRegistrationTests.cs` (extended: no AEPD source when declared; `ValidateOnBuild` and `ValidateScopes`), `tests/Encina.GuardTests/Compliance/DPIA/NationalLists/NationalListsApplicabilityReporterGuardTests.cs` (null constructor arguments of the reporter).
6. `PublicAPI.Unshipped.txt`; manifest: unit and guard targets of the files of tasks 1-4, each with a one-sentence justification.

Exact file list (14): `NationalListsTerritorialBasis.cs`, `NationalListsNonApplicability.cs`, `DPIAOptions.cs`, `DPIAOptionsValidator.cs`, `ServiceCollectionExtensions.cs`, `NationalListsApplicabilityReporter.cs`, `DPIALogMessages.cs`, `PublicAPI.Unshipped.txt`, the manifest, and the 5 test files of task 5. `DPIAScreening` does not change, so its test callers do not either.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Phases 1-4 merged. Plan Design Choice 11 as decided by the maintainer (skip Phases 5 and 6 if it is A).

TASK:
Add the structured territorial declaration, its start-up validation, the registration change, the start-up Warning 8897 and
the tests. Exactly the 14 files of the Phase 5 file list.

KEY RULES:
- Only a territorial basis passes; OtherLeadAuthority requires another NationalList source (no gap without an alternative).
- The justification is configuration text, never personal data; the log is an operational signal, not the audit record.
- A hosted-service failure never escapes (AGENTS.md section 3).

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DPIAOptions.cs
- src/Encina.Compliance.DPIA/DPIAOptionsValidator.cs
- src/Encina.Compliance.DPIA/DPIAAutoRegistrationHostedService.cs
```

</details>

---

### Phase 6: The Declaration on the Audit Record

> **Goal**: Every assessment made while the lists are declared not applicable records the whole declaration on `DPIAEvaluated`. **Size**: 12 files, about 700 lines. Dropped when Design Choice 11 is A.

<details>
<summary>Tasks</summary>

1. `DefaultDPIAAssessmentEngine.cs` (it already takes `IOptions<DPIAOptions>`): when the declaration is set, the national-list step does not call the screening, emits the counter outcome `not_applied` and copies the declaration into the result; the constructor does not change.
2. `Model/DPIAResult.cs`: `NationalListsNotApplied` (`NationalListsNonApplicability?`, default `null`, not `required`). `Events/DPIAEvents.cs`: the positional `DPIAEvaluated` gains `NationalListsNotApplied`; `Aggregates/DPIAAggregate.cs` passes and keeps it; `ReadModels/DPIAReadModel.cs` and `ReadModels/DPIAProjection.cs` carry it.
3. Tests: `tests/Encina.UnitTests/Compliance/DPIA/DefaultDPIAAssessmentEngineTests.cs` (declared: no screening call, the declaration on the result, outcome `not_applied`), `tests/Encina.UnitTests/Compliance/DPIA/ReadModels/DPIAProjectionTests.cs` (`new DPIAEvaluated(` caller plus the projected declaration), `tests/Encina.UnitTests/Compliance/DPIA/Aggregates/DPIAAggregateTests.cs` (the event carries the declaration), `tests/Encina.IntegrationTests/Compliance/DPIA/DPIANationalListsIntegrationTests.cs` (extended: Marten stores `DPIAEvaluated` with basis, country code, justification and version, read back from the stream).
4. `PublicAPI.Unshipped.txt`; manifest: unit and integration targets of the changed files, each with a one-sentence justification.

Exact file list (12): `DefaultDPIAAssessmentEngine.cs`, `DPIAResult.cs`, `DPIAEvents.cs`, `DPIAAggregate.cs`, `DPIAReadModel.cs`, `DPIAProjection.cs`, `PublicAPI.Unshipped.txt`, the manifest, and the 4 test files of task 3 (the only callers of the changed signatures are `DPIAAggregate.cs` and `DPIAProjectionTests.cs`, verified with `git grep -n "new DPIAEvaluated("`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Phases 1-5 merged. Plan Design Choice 11 D; the event stream is the audit record (ADR-019).

TASK:
Record the declaration on every DPIAEvaluated, skip the screening while it is set, and write the tests. Exactly the 12 files of
the Phase 6 file list.

KEY RULES:
- The audit record holds basis, country code, justification and declaration version; no personal data.
- CRAP <= 10 on every changed method; extract helpers instead of growing BuildAssessmentAsync.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DefaultDPIAAssessmentEngine.cs
- src/Encina.Compliance.DPIA/Events/DPIAEvents.cs
```

</details>

---

### Phase 7: Justifications, Documentation and Finalization

> **Goal**: The feature is documented and recorded. **Size**: 6 files, about 350 lines.

<details>
<summary>Tasks</summary>

1. Load and benchmark justifications following the template of AGENTS.md section 9: `tests/Encina.LoadTests/Compliance/DPIA/AepdScreening.md` and `tests/Encina.BenchmarkTests/Encina.Benchmarks/Compliance/DPIA/AepdScreening.md` (not a concurrent hot path; both lists are small frozen collections).
2. Package README section on the national lists (`src/Encina.Compliance.DPIA/README.md`); `docs/features/dpia.md` gains "National lists (AEPD)" with the list versions, the meaning of a 35.5 match, the fact that a 35.4 match does not gate requests by itself (Design Choice 10) and the territorial non-applicability declaration; by the docs-writer agent.
3. `changelog.d/1243-dpia-aepd-screening.added.md`; `docs/INVENTORY.md` rows. No ADR: the choices follow ADR-019 and the #816 abstraction.
4. `dotnet build --configuration Release` with 0 warnings; `dotnet test` green; per-flag coverage at the manifest targets; CRAP of every new or changed method at most 10; the pull request records the ADR-018 evaluation.

Exact file list (6): the 2 justification files, `README.md`, `docs/features/dpia.md`, the changelog fragment, `docs/INVENTORY.md`. `ROADMAP.md` is not changed: P-41 is tracked by #1186.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT:
- Phases 1-6 merged. Documentation goes through docs-writer; the changelog fragment through mechanical-fixer.

TASK:
Write the two justification files, the README and feature-page sections and the changelog fragment.

KEY RULES:
- English only; list versions cited from the data files, never typed twice; coverage figures only through covref markers.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/README.md
- docs/features/dpia.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | What it asks of this feature |
|--------|-----------|------------------------------|
| GDPR | Art. 35(3), 35(4), 35(5), 35(7), 36(1) | Cases that require a DPIA; supervisory-authority lists of processing that does and does not require one; prior consultation follows the residual risk |
| EDPB WP 248 rev.01 | Nine high-risk criteria | The existing six criteria; the generic "two or more criteria" rule that a 35.5 match must not silently override |
| AEPD | Art. 35.4 list and Art. 35.5 list | The two national criteria sets, transcribed with item numbers and dates |
| SPEC-002 | REQ-024, REQ-050, REQ-061, REQ-062, AC-024, AC-039, AC-043, AC-044 | National lists by default, EDPB list off; tests over both lists; tenant-aware; instrumented without personal data |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| Assessment engine | `src/Encina.Compliance.DPIA/DefaultDPIAAssessmentEngine.cs` | Hosts the national-list step |
| Context and result | `src/Encina.Compliance.DPIA/Model/DPIAContext.cs`, `DPIAResult.cs`, `HighRiskTriggers.cs` | Inputs of the rules; result extended |
| Aggregate and events | `src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs`, `Events/DPIAEvents.cs` | `DPIAEvaluated` records the matches with tenant and module |
| Criteria provider, catalog and loaded criterion (from #816) | `IDPIACriteriaProvider`, `IDPIACriteriaCatalog`, `LoadedCriteriaRiskCriterion` | The AEPD sources are registered through them (Design Choice 7) |
| Errors | `src/Encina.Compliance.DPIA/DPIAErrors.cs`, ADR-062 | New codes with an `ErrorCategory` |
| Diagnostics | `src/Encina.Compliance.DPIA/Diagnostics/DPIADiagnostics.cs`, `DPIALogMessages.cs` | Source, meter, log messages |
| Registration | `src/Encina.Compliance.DPIA/ServiceCollectionExtensions.cs` | `AddEncinaDPIA` |
| Coverage manifest | `.github/coverage-manifest/Encina.Compliance.DPIA.json` | Per-file, per-flag targets |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.DPIA` | 8897-8899 inside `ComplianceDPIA` 8800-8899 | Assigned to #1243 by the batch EventId registry; 8897 `NationalListsNotApplied` (Phase 5), 8898 `NationalListMatched` and 8899 `NationalListConflict` (Phase 2); if Design Choice 11 is A, the two used ids are 8897-8898 |
| `ComplianceDPIACriteria` (#816) | 6400-6429 | Owned by the #816 plan; logs the loading of every source, including the AEPD sources; this plan allocates nothing in it |

### Estimated File Count

| Pull request | Phase | Files | Lines (about) | Contents |
|--------------|-------|-------|---------------|----------|
| PR 1 | 1 | 15 | 1,150 | 8 new model, data, loader and provider files; `.csproj`; `DPIAErrors.cs`; PublicAPI; manifest; 3 test files |
| PR 2 | 2 | 15 | 1,250 | matcher, screening interface, screening, result, match; log messages; diagnostics; PublicAPI; manifest; 6 test files |
| PR 3 | 3 | 15 | 1,100 | engine, result, events, aggregate, `LoadedCriteriaRiskCriterion`, registration; PublicAPI; manifest; 4 existing caller test files; 3 test files |
| PR 4 | 4 | 6 | 500 | read model, projection; PublicAPI; manifest; 2 test files |
| PR 5 | 5 | 14 | 700 | basis enum, declaration, options, validator, registration, reporter, log messages; PublicAPI; manifest; 5 test files |
| PR 6 | 6 | 12 | 700 | engine, result, event, aggregate, read model, projection; PublicAPI; manifest; 4 test files |
| PR 7 | 7 | 6 | 350 | 2 justification files, README, feature page, changelog fragment, inventory |
| **Total** | | **83** | **5,750** | 83 file changes in 7 pull requests; `PublicAPI.Unshipped.txt` and the manifest change in six of them, so fewer distinct files are touched |

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
1. NationalLists model, two embedded data files with English descriptions and source references, loader, AepdCriteriaProvider.
2. Matcher, DPIAScreening, EventIds 8898-8899, counter and span.
3. Engine integration, DPIAResult/DPIAEvaluated/aggregate carry the matches, AEPD sources registered by default and skipped
   by LoadedCriteriaRiskCriterion, AC-024 default test.
4. Read model and Marten integration with tenants.
5. Structured territorial declaration, start-up validation, Warning 8897 (only if Design Choice 11 is D).
6. The declaration recorded on every DPIAEvaluated (only if Design Choice 11 is D).
7. Justifications and documentation.

KEY PATTERNS:
- Data not code; a failure to screen is a Left and a VeryHigh risk item, never "not required"; a 35.5 match never lowers a
  requirement by default.
- Error codes with an ErrorCategory (ADR-062); EventIds only 8897-8899, packed.
- Tenant on the record; no payload or data subject identifier in tags, logs or events; TimeProvider for time.
- DI test with ValidateOnBuild and ValidateScopes; CRAP <= 10 on changed methods; at most 15 files per pull request.

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
| 2 | OpenTelemetry | ✅ | Phase 2: counter `encina.dpia.screenings` and span `encina.dpia.screen`, tenant as attribute and tag only when tenancy is on (ADR-054 D4), no payloads |
| 3 | Structured Logging | ✅ | Phases 2 and 5: `[LoggerMessage]` 8897-8899 inside the registered `ComplianceDPIA` range; loading messages from #816's range |
| 4 | Health Checks | ❌ | No new checkable dependency; the AEPD sources appear in #816's catalog health data like any source |
| 5 | Validation | ✅ | Phases 1, 3 and 5: loader, start-up load check and options validated at start (`ValidateOnStart`) |
| 6 | Resilience | ❌ | No call to an external system |
| 7 | Distributed Locks | ❌ | No shared state updated by concurrent hosts |
| 8 | Transactions | ❌ | Single-operation writes through the aggregate |
| 9 | Idempotency | ❌ | Not a message or request entry point |
| 10 | Multi-Tenancy | ✅ | Phases 3 and 4: matches recorded with the aggregate's tenant; two-tenant test; fixed default tenant when tenancy is off (REQ-061) |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; the aggregate's existing `ModuleId` is untouched |
| 12 | Audit Trail | ✅ | Phases 3 and 6: screening matches and, while the lists are declared not applicable, the whole territorial declaration recorded in `DPIAEvaluated` on the DPIA aggregate |

---

## Prerequisites & Dependencies

### Required Prerequisites

- #816 merged with `IDPIACriteriaProvider`, the catalog, `LoadedCriteriaRiskCriterion` and `DPIAOptions.LoadCriteriaFrom*`; its plan owns the abstraction, the EDPB loader and the range 6400-6429.
- Confirmation of the current AEPD publications for both lists (Phase 1 task 3): version, publication date and URL.

### Recommended (Not Blocking)

- #686 (applicability resolver): when it lands the DPIA gate consults it; this plan does not depend on it.

---

## Next Steps

1. Link this plan from #1243 and #816.
2. When #1243 is picked up, re-check each Design Choice against the merged #816 code; present the Dilemmas to the maintainer and record the answers in `## Maintainer Decisions`; re-count the file lists of the phases a decision changes (Design Choices 4, 7 and 11).
3. Ship Phases 1 to 7 as separate pull requests referencing #1243; the last one carries `Fixes #1243`.
