# Implementation Plan: `Encina.Compliance.AIAct.TechnicalDocumentation` — Technical Documentation Generation (Art. 11, Annex IV)

> **Issue**: [#840](https://github.com/dlrivada/Encina/issues/840) (child of [#881](https://github.com/dlrivada/Encina/issues/881), milestone v0.16.0 — AI Act)
> **Type**: Feature
> **Complexity**: High (9 phases, 8 pull requests, 1 new package, 1 core removal, no persistence, ~79 files)
> **Estimated Scope**: ~2,000-2,500 lines of production code + ~2,000-2,400 lines of tests
> **Prerequisites**: none blocking, but ordering: the code that resolves a system (the `GeneralDescription` provider in PR 3a and the generator in PR 4) depends on the registry abstraction that exists when its worker is briefed: `IAISystemRegistry` of `main`, or `IAISystemService` if [#847](https://github.com/dlrivada/Encina/issues/847) has replaced it by then (the #847 plan recommends replacing it; Design Choice 3 note), in which case those two PRs are written after #847 lands; [#836](https://github.com/dlrivada/Encina/issues/836), [#837](https://github.com/dlrivada/Encina/issues/837), [#839](https://github.com/dlrivada/Encina/issues/839), [#842](https://github.com/dlrivada/Encina/issues/842) feed it later through one port

---

## Summary

Implement the EU AI Act Article 11 technical documentation capability: assemble the documentation of a high-risk AI system as ordered sections that follow the nine headings of Annex IV, collect evidence references for the conformity assessment, version the result so that it can be shown to be kept up to date (Art. 11(1)), and export it as JSON, Markdown or HTML. The package replaces the thin documentation seam of the core instead of living next to it.

### What is already in the code (verified on `7a0049c07`)

| Item | Location | State |
|---|---|---|
| `IAIActDocumentation` (`GenerateDocumentationAsync`, `UpdateDocumentationAsync`) | `src/Encina.Compliance.AIAct/Abstractions/IAIActDocumentation.cs` | Done; only `DefaultAIActDocumentation` and the DI registration use it |
| `DefaultAIActDocumentation` | `src/Encina.Compliance.AIAct/DefaultAIActDocumentation.cs` | Fills only `Description` from `IAISystemRegistry`; `UpdateDocumentationAsync` validates the system and stores nothing |
| Flat `TechnicalDocumentation` record | `src/Encina.Compliance.AIAct/Model/TechnicalDocumentation.cs` | Done; eight free-text fields that do not follow Annex IV |
| Registration | `ServiceCollectionExtensions.cs:86` area (`TryAddSingleton<IAIActDocumentation, DefaultAIActDocumentation>`) | Done |
| Tests and manifest entries naming the seam | `tests/Encina.GuardTests/Compliance/AIAct/AIActGuardExtendedTests.cs`, `tests/Encina.UnitTests/Compliance/AIAct/ServiceCollectionExtensionsTests.cs`, `DefaultAIActDocumentationTests.cs`, `.github/coverage-manifest/Encina.Compliance.AIAct.json` | Done; changed in Phase 1 |
| `IAISystemRegistry`, `AISystemRegistration` | `src/Encina.Compliance.AIAct/Abstractions/IAISystemRegistry.cs`, `Model/AISystemRegistration.cs` | Done; the only registry in `main` |
| Risk, dataset, oversight and log registries | none | #836, #837, #839, #842 (open) |
| Annex IV section model, evidence links, versions, exporters | none | **This plan** |

### Deviations from the issue text (each is put to the maintainer below or follows a binding rule)

- **The core documentation seam is removed, not kept beside the new generator.** The issue says the plan "removes or folds" the core types; two documentation models would be a compatibility layer (`AGENTS.md` §3). Design Choice 2.
- **Auto-population cannot read registries that do not exist.** The issue names `IRiskAssessmentRegistry` and `IDatasetRegistry` (not in the code). Design Choice 3 puts one port in this package.
- **`DocumentationProfile` and the version scheme are defined here** (the issue leaves both open): Design Choices 4 and 6.
- **`AnnexIVReference` keeps the nine headings as corrected in the issue** (data governance, human oversight and accuracy sit inside headings 2 and 3; the quality management system is Art. 17 and is not an Annex IV heading). The enum values are re-checked below.
- **Caching is decided here: none.** Art. 11(1) requires the documentation to be kept up to date, so a cached package would be the stale copy the Article forbids; staleness is made detectable instead (Design Choice 4).
- **Dates.** Annex III high-risk obligations, Art. 11 among them, apply from 2 Dec 2027 and Annex I high-risk from 2 Aug 2028 (Reg. (EU) 2026/1744, SPEC-002 §3.2). The package carries no date logic; XML docs and the feature page state the dates.

### Scope per pull request (one worker issue = one PR, `AGENTS.md` §11)

About 79 files and 4,400 lines, five times a worker PR, so eight sequential pull requests, each green on its own.

| PR | Phases | Content | Files (est.) | Lines (est.) |
|---|---|---|---|---|
| 1 | 1 | Remove the core documentation seam | 10 | ~300 changed |
| 2 | 2 (tasks 1-4, 7 for the models) | Package skeleton (csproj, two PublicAPI files, manifest, slnx entry), enum, section, evidence, package and profile models, with their unit and guard tests (3 test files) | 13 | ~700 |
| 3a | 2 (tasks 5-6) and 3 (tasks 1-3, providers 1-3) | Generator and provider abstractions, errors, evidence port and `DocumentationEvidence`, provider base class, `GeneralDescription`, `DevelopmentProcess` and `MonitoringFunctioningControl` providers, with tests (4 test files) | 13 | ~1,000 |
| 3b | 3 (providers 4-9) | `PerformanceMetrics`, `RiskManagement`, `LifecycleChanges`, `HarmonisedStandards`, `DeclarationOfConformity` and `PostMarketMonitoring` providers, with tests (3 test files) | 9 | ~700 |
| 4 | 4 | Generator, templates, content-hash versioning, options (tests included, 5 test files) | 10 | ~900 |
| 5 | 5 | Exporters (JSON, Markdown, HTML), DI, audit notifications (tests included, 5 test files) | 13 | ~1,100 |
| 6 | 6-8 | Observability, remaining tests, justifications | 10 | ~900 |
| 7 | 9 | Documentation (`docs-writer`), changelog, ADR | 6 | ~600 |

Test files are counted in every row, so no PR of the split exceeds 15 files or 1,500 lines (the PR count is eight pull requests: 1, 2, 3a, 3b, 4, 5, 6, 7). Counts are re-measured when each worker brief is written. **Affected packages**: `Encina.Compliance.AIAct` (types removed), `Encina.Compliance.AIAct.TechnicalDocumentation` (new).

**Provider category**: none (database, caching, transport, lock and validation matrices do not apply; persistence of versions and evidence is #847's).

**Family coordination (AI Act modules #838, #840, #841, #843).** Shared analysis types (`BiasReport`, `BiasIndicator`, `FairnessVerdict`, `ConfidenceInterval`) are owned by the #838 plan (Phase 1, core `Model/`); this plan reads them to fill Annex IV point 2(g) ("potentially discriminatory impacts") and does not redefine them. Art. 13 instructions for use are Annex IV point 1(h) content, so they are carried by this package's General Description section, not by #841 (see its plan). EventIds 9562-9575 are this plan's; #841 takes 9576-9587 and #843 takes 9588-9599; 9548-9561 belong to #838.

---

## Design Choices

<details>
<summary><strong>1. Package placement</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — New package `Encina.Compliance.AIAct.TechnicalDocumentation`** | Matches the issue and EPIC #881; exporters and template code stay out of the core; opt-in | One more package to version |
| **B — `TechnicalDocumentation/` folder in the core** | One assembly | The core gains three exporters and ~50 files for an opt-in feature |

### Chosen Option: **A — New package**

### Rationale

The issue title, the `new-package` label and the one-package-per-module layout of EPIC #881 decide it; nothing in the core pipeline needs to call the generator.

**Class:** Settled
**Settled by:** issue #840 (title, `new-package` label); EPIC #881; SPEC-000 REQ-025; `AGENTS.md` §1 (pay-for-what-you-use)

</details>

<details>
<summary><strong>2. The core `IAIActDocumentation`, `DefaultAIActDocumentation` and `TechnicalDocumentation`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Delete the three core types, their registration, tests and manifest entries (PR 1)** | One documentation model; no dead code | A breaking change for anyone who resolved `IAIActDocumentation` (nobody in the repository does) |
| **B — Keep the core types and make `DefaultAIActDocumentation` delegate to the new generator** | Existing callers keep compiling | A mapping from the flat record to the package is a compatibility layer |
| **C — Keep both** | No work | Two documentation models; the core one never reaches Annex IV |

### Chosen Option: **A — Delete the core seam**

### Rationale

Only the default class, the registration and tests use the seam (checked with a search of `src` and `tests`). `AGENTS.md` §1 and §3 forbid compatibility layers and dead code pre-1.0.

**Class:** Settled
**Settled by:** `AGENTS.md` §1 (no legacy, best solution first) and §3 (no compatibility layers); issue #840 Alternatives Considered item 2

</details>

<details>
<summary><strong>3. Where the content of each section comes from</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — One port `IDocumentationEvidenceSource` owned by this package: `GetEvidenceAsync(systemId, AnnexIVReference, ct)` returns content and evidence references; the owning modules (#836, #837, #839, #842) ship adapters later; `IAISystemRegistry` is read directly for heading 1** | Works today with supplied content; owners plug in without this package knowing their types; no dependency cycle | Adapters are separate work in other issues |
| **B — Depend on each owning module's registry type directly** | No port | Blocked until four open issues land; a reference to four modules |
| **C — Supplied content only; no auto-population** | Ships immediately | Fails the acceptance criterion "auto-population from the registries" |

### Chosen Option: **A — One evidence port** (recommended, pending the maintainer)

### Rationale

We recommend A: it delivers the criterion for `IAISystemRegistry` now and for the other registries as each owner implements the port. B wins if #836 and #837 merge first with a stable shared model; then the port is replaced by their types in a later PR. C is a subset of A (supplied content is the fallback when no source answers). A section whose source answers `Left` or has no content makes generation return a `Left` naming the section. Coordination with #847: the plan of #847 recommends replacing `IAISystemRegistry` with `IAISystemService`; this plan reads the system through whichever of the two exists when the PR 3a and PR 4 workers are briefed, and the system version used in the version string comes from the same abstraction (no `IAISystemRegistry`-only member is added by this plan).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Version scheme and "kept up to date" without a store</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Content-addressed version: `{system version}+{first 12 hex of SHA-256 over the canonical section content}`; the caller passes the last known version to `IsCurrentAsync`** | Deterministic (same input, same version); no store; staleness is a comparison; generation stays idempotent | The sequence of versions is the caller's (or #847's) to keep |
| **B — A sequential integer kept in a Marten stream** | Natural history | Blocked on #847; makes this package stateful |
| **C — A timestamp** | Trivial | Two generations of identical content differ; staleness cannot be shown |

### Chosen Option: **A — Content-addressed version** (recommended, pending the maintainer)

### Rationale

We recommend A. Art. 11(1) says the documentation "shall be kept up-to date"; the checkable fact is whether the documentation still matches the system, which a content hash answers. B wins once #847 persists packages and wants a numbered history; the hash stays as the integrity field. The interface gains `IsCurrentAsync(systemId, profile, knownVersion)` (a deviation from the issue's single method).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Missing, failing or empty sections</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Any section whose provider returns `Left`, or whose mandatory content is empty, makes the whole generation return `Left` naming the section** | Fail closed; no silently incomplete Annex IV | One missing item blocks the package |
| **B — Omit the section and list it in a warnings field** | Partial output | A package that looks complete and is not |

### Chosen Option: **A — Fail the generation**

### Rationale

Annex IV is a minimum content list ("at a minimum", Art. 11(1)). A caller who wants a draft passes `DocumentationProfile.AllowIncomplete = true`, which is explicit, logged and recorded in the package (the only opt-out, as `AGENTS.md` §3 allows).

**Class:** Settled
**Settled by:** `AGENTS.md` §3 (errors never swallowed; gates fail closed, only explicit logged opt-out); issue #840 Alternatives Considered item 5; Art. 11(1) AI Act

</details>

<details>
<summary><strong>6. Template mechanism for user-supplied content</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Plain text with `{{token}}` substitution from a dictionary (system name, version, provider, evidence references); no engine** | No dependency; deterministic; safe (no code execution) | No loops or conditions |
| **B — A template engine (Scriban, Razor)** | Loops, conditions, layouts | A dependency and a code-execution surface for text that comes from users |
| **C — No templates; supplied content is literal** | Smallest | Cannot reuse a template across systems |

### Chosen Option: **A — Token substitution** (recommended, pending the maintainer)

### Rationale

We recommend A for pay-for-what-you-use and safety. B wins for an organization with many systems and a shared layout who needs repeating blocks; they can render with their engine and pass literal content (option C is always available). An unknown token is a `Left`, never an empty string.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Export formats and output safety</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — JSON (System.Text.Json), Markdown and HTML as UTF-8 bytes; HTML encodes every user-supplied string; the file name is derived from the system id with invalid characters replaced** | The three formats of the acceptance criteria; no XSS from supplied content | HTML is plain, unstyled |
| **B — Add PDF** | A signature-ready format | A PDF dependency; outside the criteria |

### Chosen Option: **A — Three formats, encoded output**

### Rationale

The acceptance criteria name JSON, Markdown and HTML. Supplied content is untrusted text and the HTML exporter is the one place it could become markup, so it is encoded (OWASP XSS prevention guidance).

**Class:** Trivial
**Settled by:** issue #840 acceptance criteria ("Export to JSON, Markdown, and HTML formats"); OWASP Cross Site Scripting Prevention Cheat Sheet (https://cheatsheetseries.owasp.org/); `AGENTS.md` §3

</details>

<details>
<summary><strong>8. EventId allocation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Sub-allocate 9562-9575 in `ComplianceAIAct` and map the assembly to it** | The pattern of #842, #847 and #838; no new range | The family shares the tail of one range |
| **B — A new range from the 9800-9999 block** | Independent | The block is claimed by the #686-#688 plans |

### Chosen Option: **A — 9562-9575 in `ComplianceAIAct`**

### Rationale

#838 holds 9548-9561; this plan holds 9562-9575 (14 ids); #841 holds 9576-9587 and #843 holds 9588-9599.

**Class:** Trivial
**Settled by:** ADR-021; `AGENTS.md` §7; the EventId sections of the #838, #842 and #847 plans; `src/Encina/Diagnostics/EventIdRanges.cs:389`

</details>

---

## Implementation Phases

### Phase 1: Remove the Core Documentation Seam (PR 1)

> **Goal**: One documentation model in the repository.

<details>
<summary><strong>Tasks</strong></summary>

1. **Delete** `Abstractions/IAIActDocumentation.cs`, `DefaultAIActDocumentation.cs`, `Model/TechnicalDocumentation.cs` from `src/Encina.Compliance.AIAct/`.
2. **Modify `ServiceCollectionExtensions.cs`**: remove the `IAIActDocumentation` registration and its using.
3. **Modify `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`**: remove the symbols (RS0017) from whichever file lists them.
4. **Delete `tests/Encina.UnitTests/Compliance/AIAct/DefaultAIActDocumentationTests.cs`**; modify `ServiceCollectionExtensionsTests.cs` and `tests/Encina.GuardTests/Compliance/AIAct/AIActGuardExtendedTests.cs`.
5. **Modify `.github/coverage-manifest/Encina.Compliance.AIAct.json`**: remove the entries of deleted files (their source no longer exists, so the target ratchet allows it); update `totalFiles`.
6. **Fix the references to the removed names in the same PR** (through `docs-writer`): `docs/features/aiact-compliance.md` and `src/Encina.Compliance.AIAct/README.md` stop naming `IAIActDocumentation`, `DefaultAIActDocumentation` and `TechnicalDocumentation` (a search of `docs/` and the README lists every occurrence), so no published page names a deleted type between PR 1 and PR 7. The PR file count (10) includes these two pages.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #840 in Encina.Compliance.AIAct.

CONTEXT:
- IAIActDocumentation, DefaultAIActDocumentation and TechnicalDocumentation are replaced by the new package
  Encina.Compliance.AIAct.TechnicalDocumentation (later PRs). Pre-1.0: delete completely, no alias, no [Obsolete].

TASK:
Delete the three types, their DI registration, their tests and manifest entries; fix the tests that mention them; update the
PublicAPI files; confirm with a full build that nothing else referenced them.

KEY RULES:
- Zero warnings; RS0017 resolved. A deleted source file may lose its manifest entry; no other target is lowered.
- Do not touch unrelated registrations (AddEncinaAIAct stays complete: ValidateOnBuild test still passes).

REFERENCE FILES:
- src/Encina.Compliance.AIAct/ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/Compliance/AIAct/ServiceCollectionExtensionsTests.cs
- tests/Encina.GuardTests/Compliance/AIAct/AIActGuardExtendedTests.cs
- .github/coverage-manifest/Encina.Compliance.AIAct.json
```

</details>

---

### Phase 2: Package Skeleton, Models & Abstractions (PR 2; tasks 5-6 ship in PR 3a)

> **Goal**: A buildable package with the Annex IV vocabulary and the generator contracts.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create the project** `src/Encina.Compliance.AIAct.TechnicalDocumentation/` (csproj referencing `Encina.Compliance.AIAct`, `PublicAPI.*.txt`, `InternalsVisibleTo` the test projects), add to `Encina.slnx`, add `.github/coverage-manifest/Encina.Compliance.AIAct.TechnicalDocumentation.json` with `generate-coverage-manifest.cs`.
2. **Create `Model/AnnexIVReference.cs`**: nine values: `GeneralDescription` (1), `DevelopmentProcess` (2), `MonitoringFunctioningControl` (3), `PerformanceMetrics` (4), `RiskManagement` (5), `LifecycleChanges` (6), `HarmonisedStandards` (7), `DeclarationOfConformity` (8), `PostMarketMonitoring` (9).
3. **Create `Model/DocumentationSection.cs`** (`SectionId`, `Title`, `Order`, `Content`, `AnnexReference`, `Evidence`), `Model/EvidenceReference.cs` (`EvidenceType`, `Reference`, `CollectedAtUtc`, `Sha256` optional), `Model/TechnicalDocumentationPackage.cs` (`SystemId`, `SystemName`, `RiskLevel`, `Sections`, `GeneratedAtUtc`, `Version`, `ContentHash`, `IsComplete`).
4. **Create `Model/DocumentationProfile.cs`**: `SectionsToInclude` (empty means all nine), `SuppliedContent` (`IReadOnlyDictionary<AnnexIVReference, string>`), `Tokens`, `AllowIncomplete` (default false).
5. **Create `Abstractions/ITechnicalDocumentationGenerator.cs`** (`GenerateAsync`, `IsCurrentAsync`) and `Abstractions/IDocumentationSectionProvider.cs` (`SectionId`, `Order`, `AnnexReference`, `GenerateSectionAsync`).
6. **Create `TechnicalDocumentationErrors.cs`**: codes `aiact.documentation_system_not_found`, `aiact.documentation_section_failed`, `aiact.documentation_section_empty`, `aiact.documentation_unknown_token`, `aiact.documentation_export_failed`; factories never carry exception messages.
7. **Tests**: records, enum values (nine, in Annex order), errors; guard tests; manifest targets with justifications.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #840: the new package Encina.Compliance.AIAct.TechnicalDocumentation.

CONTEXT:
- Phase 1 removed the core documentation seam. Annex IV has nine numbered headings (general description; elements and
  development process; monitoring, functioning and control; performance metrics; risk management; lifecycle changes;
  harmonised standards; EU declaration of conformity; post-market monitoring). Data governance, human oversight, accuracy
  and cybersecurity are sub-items of headings 2 and 3.

TASK:
Create the project, slnx entry, PublicAPI files, manifest, models, the two abstractions and the errors class with tests.

KEY RULES:
- Either<EncinaError, T> everywhere; time from TimeProvider (no clock reads in records).
- XML docs name the Annex IV point; English only; zero warnings.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/Encina.Compliance.AIAct.csproj
- src/Encina.Compliance.AIAct/AIActErrors.cs
- src/Encina.Compliance.AIAct/Model/AISystemRegistration.cs
- .github/scripts/generate-coverage-manifest.cs
```

</details>

---

### Phase 3: Evidence Port & Default Section Providers (PR 3a: tasks 1-2 and providers 1-3; PR 3b: providers 4-9)

> **Goal**: One provider per Annex IV heading.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `Abstractions/IDocumentationEvidenceSource.cs`**: `ValueTask<Either<EncinaError, DocumentationEvidence?>> GetEvidenceAsync(string systemId, AnnexIVReference reference, CancellationToken)`; `Model/DocumentationEvidence.cs` (`Content`, `Evidence`).
2. **Create `Sections/AnnexIVSectionProviderBase.cs`** (internal): resolves content from supplied profile content, then the evidence sources, then fails with `aiact.documentation_section_empty`; applies token substitution.
3. **Create nine providers** in `Sections/`: `GeneralDescriptionSectionProvider` (reads `IAISystemRegistry`: name, provider, version, intended use; includes point 1(h) instructions for use, which carries the Art. 13 content), `DevelopmentProcessSectionProvider` (2(a)-(h), including data governance 2(d), human oversight 2(e) and the discriminatory-impact metrics 2(g), rendering a `BiasReport` when the evidence source supplies one), `MonitoringFunctioningControlSectionProvider`, `PerformanceMetricsSectionProvider`, `RiskManagementSectionProvider`, `LifecycleChangesSectionProvider`, `HarmonisedStandardsSectionProvider`, `DeclarationOfConformitySectionProvider`, `PostMarketMonitoringSectionProvider`.
4. **Tests**: each provider with supplied content, with an evidence source, with a failing source and with nothing (empty section is a `Left`); property test that `Order` is strictly increasing in Annex order.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #840 in Encina.Compliance.AIAct.TechnicalDocumentation.

CONTEXT:
- Models and abstractions exist (Phase 2). IAISystemRegistry (core) is the only registry in main; risk (#836), dataset (#837),
  oversight (#839) and log (#842) registries come later through IDocumentationEvidenceSource adapters.
- BiasReport/BiasIndicator are owned by the #838 plan (core Model); render them if supplied, do not redefine them.

TASK:
Create the evidence port, the provider base class and the nine default providers (Design Choices 3 and 5).

KEY RULES:
- A provider returns Left on a failing source or empty mandatory content; never an empty section.
- Unknown {{token}} is a Left; tokens come only from DocumentationProfile.Tokens and registry data.
- Sections carry EvidenceReference entries for the conformity assessment; CollectedAtUtc from TimeProvider.
- CRAP <= 10 per method; tests execute real providers.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/Abstractions/IAISystemRegistry.cs
- src/Encina.Compliance.AIAct/InMemoryAISystemRegistry.cs
- docs/plans/aiact-bias-detection-implementation-plan-838.md
- https://artificialintelligenceact.eu/annex/4/
```

</details>

---

### Phase 4: Generator, Templates & Versioning (PR 4)

> **Goal**: The orchestrator that builds, orders and versions a package.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `DefaultTechnicalDocumentationGenerator.cs`** (`ITechnicalDocumentationGenerator`): resolve the system through `IAISystemRegistry` (`Left(aiact.documentation_system_not_found)`), run the selected providers (ordered by `Order`), fail on the first failing section unless `AllowIncomplete`, set `GeneratedAtUtc` from `TimeProvider`.
2. **Create `Versioning/DocumentationVersion.cs`** (internal static): canonical serialization of sections (ordered, UTF-8) that contains only `SectionId`, `AnnexReference`, `Content` and, per evidence reference, `EvidenceType`, `Reference` and `Sha256`; it excludes `GeneratedAtUtc` and every `EvidenceReference.CollectedAtUtc`, so regenerating unchanged content gives the same hash and `IsCurrentAsync` can be true; SHA-256, version string `{registration.Version ?? "0"}+{hash12}`; `IsCurrentAsync` regenerates and compares.
3. **Create `Templates/TokenRenderer.cs`** (internal): `{{token}}` substitution, unknown token is a `Left`.
4. **Create `TechnicalDocumentationOptions.cs`** and its validator (`IValidateOptions`): default profile, maximum section length, allowed tokens.
5. **Tests**: ordering, hash stability across runs and machines, version changes when any section changes, `IsCurrentAsync` true/false, unknown token, `AllowIncomplete`, cancellation.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #840 in Encina.Compliance.AIAct.TechnicalDocumentation.

CONTEXT:
- Providers exist (Phase 3). Design Choice 4: version = system version + 12 hex of SHA-256 over canonical section content.

TASK:
Implement DefaultTechnicalDocumentationGenerator, DocumentationVersion, TokenRenderer, options and validator.

KEY RULES:
- Deterministic output: culture-invariant, ordinal comparisons, no timestamps in the hashed content (GeneratedAtUtc and EvidenceReference.CollectedAtUtc are excluded from the canonical form).
- Time only from TimeProvider; cancellation propagates; no exception becomes success.
- Methods small enough for CRAP <= 10.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/AIActOptionsValidator.cs
- src/Encina.Compliance.AIAct.TechnicalDocumentation/Sections/
- tests/Encina.PropertyTests/Compliance/AIAct/
```

</details>

---

### Phase 5: Exporters, DI & Audit Notifications (PR 5)

> **Goal**: Export in three formats and register everything.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `Abstractions/IDocumentationExporter.cs`**, `Model/ExportFormat.cs` (`Json`, `Markdown`, `Html`), `Model/ExportedDocumentation.cs` (`Content` bytes, `ContentType`, `FileName`, `Format`).
2. **Create `Exporters/JsonDocumentationExporter.cs`**, `MarkdownDocumentationExporter.cs`, `HtmlDocumentationExporter.cs` (encode all strings; sanitize the file name) and `DefaultDocumentationExporter.cs` (dispatches by format; unsupported format is a `Left`).
3. **Create `Notifications/TechnicalDocumentationGeneratedNotification.cs`** and `TechnicalDocumentationExportedNotification.cs` (system id, version, format, `OccurredAtUtc`; no content), published through `IEncina.Publish`; a publish `Left` fails the operation.
4. **Create `ServiceCollectionExtensions.cs`**: `AddEncinaAIActTechnicalDocumentation(Action<TechnicalDocumentationOptions>?)` registers options, validator, `TimeProvider`, the nine providers with `TryAddEnumerable`, generator, exporters. DI test with `ValidateOnBuild` and `ValidateScopes`.
5. **Tests**: golden-file tests per format, HTML injection test (`<script>` in supplied content is encoded), file-name sanitizing, publish failure, contract tests of the three interfaces on real implementations.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #840 in Encina.Compliance.AIAct.TechnicalDocumentation.

CONTEXT:
- Generator and providers exist (Phases 2-4). Core notifications are published with IEncina.Publish
  (src/Encina.Compliance.AIAct/InMemoryAISystemRegistry.cs).

TASK:
Implement the exporters, the dispatcher, two audit notifications and AddEncinaAIActTechnicalDocumentation.

KEY RULES:
- Registration completeness (AGENTS.md section 3): every option type and dependency registered; DI test with ValidateOnBuild and
  ValidateScopes.
- HTML output encodes every user-supplied string; JSON through System.Text.Json; UTF-8 bytes.
- Notifications carry ids and counts, never documentation content; a Left from Publish fails the operation.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/ServiceCollectionExtensions.cs
- src/Encina.Compliance.AIAct/Notifications/ProhibitedUseBlockedNotification.cs
- tests/Encina.UnitTests/Compliance/AIAct/ServiceCollectionExtensionsTests.cs
```

</details>

---

### Phase 6: Observability (PR 6)

> **Goal**: Spans, counters and fourteen log events.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `Diagnostics/TechnicalDocumentationDiagnostics.cs`**: `ActivitySource` and `Meter` named `Encina.Compliance.AIAct.TechnicalDocumentation`; spans `aiact.technical_documentation.generate` (system id, section count, outcome) and `aiact.technical_documentation.export` (format, outcome); counters `aiact.technical_documentation.generated` (tag outcome), `aiact.technical_documentation.exported` (tag format); histogram `aiact.technical_documentation.duration` (ms). System id on spans only.
2. **Create `Diagnostics/TechnicalDocumentationLogMessages.cs`** with `[LoggerMessage]`, ids 9562-9575 packed: generation started, completed, system not found, section failed, section empty, unknown token, incomplete allowed (opt-out), version computed, up to date, stale, export started, completed, export failed, notification publish failed.
3. **Modify `EncinaEventIdAllocationTests.AssemblyRanges`**: `["Encina.Compliance.AIAct.TechnicalDocumentation"] = [nameof(EventIdRanges.ComplianceAIAct)]`.
4. Instrument the generator and exporters.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #840 in Encina.Compliance.AIAct.TechnicalDocumentation.

CONTEXT:
- ComplianceAIAct is 9500-9599: 9500-9512 core, 9513-9529 #847, 9530-9547 #842, 9548-9561 #838, 9562-9575 this issue,
  9576-9587 #841, 9588-9599 #843.

TASK:
Create the diagnostics and log messages (14 [LoggerMessage] methods, ids 9562-9575, no gaps), map the assembly in the
architecture test and instrument the generator and exporters.

KEY RULES:
- ADR-021: no id outside the range; run the architecture tests. XML line: Event IDs: 9562-9575 (see EventIdRanges.ComplianceAIAct).
- Never log or tag EncinaError.Message; error codes and ForLogging() only; no high-cardinality metric tags.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Compliance.AIAct/Diagnostics/AIActDiagnostics.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 7: Cross-Cutting Integration (PR 5-6)

> **Goal**: The ADR-018 functions marked for this feature.

<details>
<summary><strong>Tasks</strong></summary>

1. Validation: options validator (Phase 4), profile validation in the generator (unknown section, unknown token), export format validation.
2. Audit trail: the two notifications (Phase 5).
3. Defer: tenant scoping (#845), module scoping (#846), persistence of versions and evidence (#847); no new issue is needed.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are verifying the cross-cutting integration of issue #840 in Encina.Compliance.AIAct.TechnicalDocumentation.

CONTEXT:
- Validation and audit notifications were built in Phases 4-5; the plan's matrix lists the rest as N/A or deferred.

TASK:
Confirm each Integrate row of the matrix has code and a test; add what is missing.

KEY RULES:
- No scope creep: tenant (#845), module (#846) and persistence (#847) are deferred.
- Errors are never swallowed.

REFERENCE FILES:
- docs/plans/aiact-technical-documentation-implementation-plan-840.md
- AGENTS.md
```

</details>

---

### Phase 8: Testing (spread over the PRs, completed in PR 6)

> **Goal**: Every flag reaches its target.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit**: generator, nine providers, token renderer, versioning, exporters, notifications, telemetry.
2. **Guard**: every public constructor and method parameter.
3. **Contract**: `ITechnicalDocumentationGenerator`, `IDocumentationSectionProvider`, `IDocumentationExporter`, `IDocumentationEvidenceSource` on real implementations.
4. **Property**: stable ordering; every section present in every format; export then parse (JSON) round trip; hash independent of input dictionary order.
5. **DI test** with `ValidateOnBuild` and `ValidateScopes`.
6. **Integration**, **Load**, **Benchmark**: `.md` justifications (no store, no concurrency, not a hot path) under `tests/Encina.IntegrationTests/`, `Encina.LoadTests/`, `Encina.BenchmarkTests/` `Compliance/AIAct/TechnicalDocumentation/`.
7. **Coverage manifest**: per-file targets and justifications; measure each flag; local CRAP table for changed methods (<= 10).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are completing the tests of issue #840 in Encina.Compliance.AIAct.TechnicalDocumentation.

CONTEXT:
- Phases 1-7 are implemented; earlier PRs brought their own tests.

TASK:
Write the missing tests and the three .md justifications; bring every flag to its manifest target.

KEY RULES:
- Tests execute real code; Shouldly via Encina.Testing.Shouldly; no Thread.Sleep; builders for test data.
- Justification files follow the AGENTS.md section 9 template. Do not lower any target.

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/AIAct/
- tests/Encina.BenchmarkTests/Compliance/AIAct/AIAct.md
- .github/coverage-manifest/Encina.Compliance.AIAct.TechnicalDocumentation.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 9: Documentation & Finalization (PR 7)

> **Goal**: Documentation, changelog, public API and verification.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on all public APIs (`<summary>`, `<remarks>` with the Annex IV point, `<param>`, `<returns>`, `<example>`).
2. `changelog.d/840-aiact-technical-documentation.added.md` and `changelog.d/840-aiact-documentation-seam.removed.md` (written by `mechanical-fixer`).
3. Package README; `docs/features/aiact-compliance.md` Art. 11 section with the article table required by SPEC-000 REQ-024 (Annex IV headings covered, evidence), through `docs-writer`; the references to the removed core names were already fixed in PR 1 (Phase 1 task 6).
4. `docs/INVENTORY.md`, `ROADMAP.md`; ADR (number assigned when the ADR pull request opens; `eventid-blocks.md` gives 068 to the AI Act family ADR of the #838 plan, so this ADR either extends that one or takes the next free number then): evidence port, content-hash versions, fail-closed sections.
5. `PublicAPI.Unshipped.txt` complete; `dotnet build Encina.slnx --configuration Release` (0 warnings); tests; every flag at target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of issue #840 in Encina.Compliance.AIAct.TechnicalDocumentation.

CONTEXT:
- Phases 1-8 are merged and measured.

TASK:
Write the changelog fragments, README, feature-page section with the Annex IV article table, INVENTORY, ROADMAP and ADR;
finish the public API files; verify build, tests and per-flag coverage.

KEY RULES:
- Never edit CHANGELOG.md [Unreleased]; fragments only. Docs never type coverage figures by hand (covref markers, SPEC-001).
- Pages go through the docs-writer agent; English only; no AI attribution; zero warnings.

REFERENCE FILES:
- changelog.d/README.md
- docs/features/aiact-compliance.md
- .claude/skills/encina-docs/SKILL.md
- docs/architecture/adr/index.md
```

</details>

---

## Research

### Relevant Standards & Specifications

Checked 2026-11-11 on the consolidated text at artificialintelligenceact.eu ("text read"); the EUR-Lex Official Journal page returned an empty challenge page to this tool, so the Official Journal itself was not read. Everything else is marked unverified.

| Provision | Requirement | Where in this plan | Status |
|-----------|-------------|--------------------|--------|
| AI Act Art. 11(1) | Technical documentation drawn up before placing on the market, kept up to date, demonstrating compliance, containing at a minimum the Annex IV elements; simplified form for SMEs/SMCs | Phases 3-4; versioning (Design Choice 4) | Text read; the Commission's simplified form for SMEs and SMCs is not modeled (no form exists to implement) and is recorded as deferred |
| AI Act Annex IV points 1-9 | 1 general description (including 1(h) instructions for use); 2 elements and development process (2(d) data, 2(e) human oversight, 2(g) validation, testing, metrics and "potentially discriminatory impacts", 2(h) cybersecurity); 3 monitoring, functioning, control; 4 appropriateness of performance metrics; 5 risk management system (Art. 9); 6 relevant lifecycle changes; 7 harmonised standards; 8 EU declaration of conformity (Art. 47); 9 post-market evaluation system (Art. 72) | Phases 2-3 (`AnnexIVReference`) | Text read; nine headings confirmed |
| AI Act Art. 13(2)-(3) | Instructions for use accompany high-risk systems | General Description section (Annex IV 1(h)) | Text read for paragraphs 1-2 and the start of 3 |
| AI Act Art. 10(2)(f) | Bias examination; its results feed Annex IV 2(g) | Phase 3 renders `BiasReport` (#838) | Text read |
| AI Act Art. 17, 47, 72 | Quality management system; declaration of conformity; post-market monitoring | Out of scope as generators; referenced by headings 8 and 9 | Unverified here |
| Reg. (EU) 2026/1744 | Annex III high-risk from 2 Dec 2027, Annex I from 2 Aug 2028 | Feature page, XML docs | From SPEC-002 §3.2 and the issue comment; not re-read in the Official Journal |
| SPEC-000 REQ-024, REQ-025; SPEC-002 REQ-022 | Article table; Art. 11 in 1.0 scope | Phase 9 | Repository documents |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IAISystemRegistry`, `AISystemRegistration` | `src/Encina.Compliance.AIAct/Abstractions/IAISystemRegistry.cs`, `Model/AISystemRegistration.cs` | Heading 1 content and system lookup |
| `BiasReport`, `BiasIndicator` (from #838 Phase 1) | `src/Encina.Compliance.AIAct/Model/` | Annex IV 2(g) rendering |
| `AIActErrors`, `AIActOptionsValidator` | `src/Encina.Compliance.AIAct/` | Error naming and validator style |
| Notifications and `IEncina.Publish` | `src/Encina.Compliance.AIAct/Notifications/`, `InMemoryAISystemRegistry.cs:109` | Generation and export audit |
| `AIActDiagnostics`, `AIActLogMessages` | `src/Encina.Compliance.AIAct/Diagnostics/` | Style of the package's diagnostics |
| `EventIdRanges.ComplianceAIAct`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs:389`, `tests/Encina.UnitTests/Testing/Architecture/` | Range 9562-9575 and assembly map |
| Coverage manifest generator | `.github/scripts/generate-coverage-manifest.cs` | New package manifest |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.AIAct` (#415, #847, #842) | 9500-9547 | In use or reserved by their plans |
| `Encina.Compliance.AIAct.BiasDetection` (#838) | 9548-9561 | Reserved by the #838 plan |
| **`Encina.Compliance.AIAct.TechnicalDocumentation` (#840)** | **9562-9575** | **14 ids inside `ComplianceAIAct`** |
| `Encina.Compliance.AIAct.Transparency` (#841) | 9576-9587 | Reserved by the #841 plan |
| `Encina.Compliance.AIAct.GPAI` (#843) | 9588-9599 | Reserved by the #843 plan |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core removal (Phase 1) | 10 | 3 deletions, registration, PublicAPI, 3 tests, manifest, README note |
| Package project files | 5 | csproj, two PublicAPI files, `Encina.slnx`, manifest |
| Models | 9 | `AnnexIVReference`, `DocumentationSection`, `EvidenceReference`, `TechnicalDocumentationPackage`, `DocumentationProfile`, `DocumentationEvidence`, `ExportFormat`, `ExportedDocumentation`, errors |
| Abstractions | 4 | generator, section provider, evidence source, exporter |
| Section providers | 10 | base class and nine providers |
| Generator, versioning, templates, options | 5 | generator, `DocumentationVersion`, `TokenRenderer`, options, validator |
| Exporters, DI, notifications | 7 | three exporters, dispatcher, `ServiceCollectionExtensions`, two notifications |
| Diagnostics | 2 | `TechnicalDocumentationDiagnostics`, `TechnicalDocumentationLogMessages` |
| Tests | ~21 | unit, guard, contract, property, DI, architecture map, three `.md` justifications |
| Documentation | 6 | changelog fragments, README, feature page, ADR, INVENTORY, ROADMAP |
| **Total** | **~79** | **Eight pull requests of 6 to 15 files** |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #840 — EU AI Act Art. 11 technical documentation (Annex IV) — as the new package
Encina.Compliance.AIAct.TechnicalDocumentation, after removing the core documentation seam. One phase per pull request.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0: no backward compatibility, no [Obsolete], best solution always.
- Railway Oriented Programming: every operation returns Either<EncinaError, T>. Compliance gates fail closed.
- The package persists nothing; versions and evidence persistence belong to #847. Output is deterministic.

IMPLEMENTATION OVERVIEW:
Phase 1: delete IAIActDocumentation, DefaultAIActDocumentation, TechnicalDocumentation, their registration, tests, manifest entries.
Phase 2: package skeleton; AnnexIVReference (nine headings), DocumentationSection, EvidenceReference,
         TechnicalDocumentationPackage, DocumentationProfile; ITechnicalDocumentationGenerator, IDocumentationSectionProvider; errors.
Phase 3: IDocumentationEvidenceSource port and nine section providers (heading 1 reads IAISystemRegistry; 2(g) renders BiasReport).
Phase 4: DefaultTechnicalDocumentationGenerator, content-hash DocumentationVersion, TokenRenderer, options + validator.
Phase 5: exporters (JSON, Markdown, HTML with encoding), dispatcher, two audit notifications, AddEncinaAIActTechnicalDocumentation.
Phase 6: ActivitySource/Meter and 14 log events 9562-9575 in ComplianceAIAct; architecture-test map.
Phase 7: verify the cross-cutting rows. Phase 8: all test types, three .md justifications, manifest targets.
Phase 9: docs-writer pages with the Annex IV article table, changelog fragments, ADR, INVENTORY, ROADMAP, PublicAPI.

KEY PATTERNS:
- TimeProvider only; unknown token, failing source or empty section is a Left naming the section; AllowIncomplete is explicit and logged.
- EncinaError.Message and exception messages never logged or tagged; HTML output encodes all supplied text.
- TryAdd registrations; DI completeness test with ValidateOnBuild and ValidateScopes; CRAP <= 10 per changed method.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/DefaultAIActDocumentation.cs
- src/Encina.Compliance.AIAct/ServiceCollectionExtensions.cs
- src/Encina.Compliance.AIAct/Abstractions/IAISystemRegistry.cs
- docs/plans/aiact-bias-detection-implementation-plan-838.md
- docs/plans/aiact-record-keeping-implementation-plan-842.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ❌ N/A | Art. 11(1) requires the documentation to be kept up to date; a cached package is the stale copy the Article forbids. Staleness is made detectable by the content-hash version (Design Choice 4), so no cache and no invalidation question remains |
| 2 | **OpenTelemetry** | ✅ Phase 6 | Spans `aiact.technical_documentation.generate` and `.export`; counters `generated` and `exported`; histogram `duration` |
| 3 | **Structured Logging** | ✅ Phase 6 | 14 `[LoggerMessage]` events, ids 9562-9575 inside `ComplianceAIAct` (ADR-021) |
| 4 | **Health Checks** | ❌ N/A | Generation is on demand; the evidence sources are the owners' dependencies |
| 5 | **Validation** | ✅ Phases 4-5 | Options validator; profile, token and format validation before any work; completeness validated before export |
| 6 | **Resilience** | ❌ N/A | No external call; evidence adapters of other modules own theirs |
| 7 | **Distributed Locks** | ❌ N/A | No shared mutable state |
| 8 | **Transactions** | ❌ N/A | The package persists nothing |
| 9 | **Idempotency** | ✅ Phase 4 | Generation is deterministic: same input, same version and hash; no duplicate side effects besides notifications |
| 10 | **Multi-Tenancy** | ⏭️ Defer ([#845](https://github.com/dlrivada/Encina/issues/845)) | Per-tenant documentation is part of the tenant-scoped AI Act enforcement |
| 11 | **Module Isolation** | ⏭️ Defer ([#846](https://github.com/dlrivada/Encina/issues/846)) | `ModuleId`-scoped enforcement is delivered package-wide |
| 12 | **Audit Trail** | ✅ Phase 5 | Generation and export notifications through `IEncina.Publish` |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | State | Why |
|---|---|---|
| `Encina.Compliance.AIAct` core ([#415](https://github.com/dlrivada/Encina/issues/415)) | Closed, in `main` | `IAISystemRegistry`, notifications, registration pattern |

No `needs-decision` issue blocks this plan.

### Recommended (Not Blocking)

- [#838](https://github.com/dlrivada/Encina/issues/838) (Phase 1): `BiasReport` and the verdict types for Annex IV 2(g).
- [#836](https://github.com/dlrivada/Encina/issues/836), [#837](https://github.com/dlrivada/Encina/issues/837), [#839](https://github.com/dlrivada/Encina/issues/839), [#842](https://github.com/dlrivada/Encina/issues/842): each ships an `IDocumentationEvidenceSource` adapter for its heading (risk management, data governance, human oversight, logs).
- [#847](https://github.com/dlrivada/Encina/issues/847): persisted package history and evidence.
- [#841](https://github.com/dlrivada/Encina/issues/841): its Art. 13 content is Annex IV 1(h) here.

---

## Next Steps

1. The maintainer answers Design Choices 3, 4 and 6 when work on #840 starts; the orchestrator records the answers in a `## Maintainer Decisions` section after this one and runs `check-plan` without `-Draft`.
2. `adversarial-reviewer` tries to break the Settled and Trivial choices (1, 2, 5, 7, 8) before the first worker brief, and `decision-extractor` lists any implicit choice.
3. Brief the eight pull requests in order, each with its re-measured `Size:` line, per-file coverage targets and the local CRAP command.
4. Update #840's acceptance criteria to the decided scope (evidence port, content-hash versions, `IsCurrentAsync`, core seam removed).
