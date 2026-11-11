# Implementation Plan: Compliance Module Documentation Hub — `docs/compliance/` in six pull requests

> **Issue**: [#689](https://github.com/dlrivada/Encina/issues/689)
> **Type**: Feature (documentation)
> **Complexity**: High (about 35 pages; far above one worker pull request, so the plan proposes six sub-issues)
> **Estimated Scope**: about 4,000-5,000 lines of Markdown across six pull requests, no production code, no tests

---

## Summary

Build the holistic compliance guide the issue asks for: a hub under `docs/compliance/` that says what Encina does and does not do at application level, how it relates to governance, legal and infrastructure tools, one reference page per module, how the modules interact, which regulation articles each module covers, how to adopt the modules progressively, and the practices and questions that follow. Per-module feature pages already exist under `docs/features/` (more than twenty compliance and security pages, for example `consent-management.md`, `data-subject-rights.md`, `crypto-shredding.md`); the hub references them and does not repeat them (acceptance criterion "No duplication").

The issue is about 30 pages, which exceeds the pull request limit of 15 files and 1,500 changed lines (AGENTS.md section 11). **This plan proposes the split into six sub-issues, each one pull request**; the orchestrator opens them, and no worker is briefed on #689 itself.

| Sub-issue | Content | Files | Estimated lines | Depends on |
|-----------|---------|------:|----------------:|------------|
| S1 Foundations | `docs/compliance/index.md` (overview, layers, positioning, design principles), `industry-tools.md`, `architecture.md`, link from `docs/features/index.md` | 4 | about 600 | none |
| S2 GDPR module pages A | `modules/index.md`, consent, lawful-basis, data-subject-rights, anonymization, retention, data-residency | 7 | about 750 | S1 |
| S3 GDPR module pages B | breach-notification, read-auditing, crypto-shredding, pii-masking, encryption, sanitization | 6 | about 700 | S1 |
| S4 Advanced module pages | dpia, processor-agreements, privacy-by-design, cross-border, nis2, ai-act, abac, message-encryption | 8 | about 900 | S1 |
| S5 Modules the issue did not list | attestation, break-the-glass, anti-tampering, secrets (4 pages; 7 with the three planned ones if Design Choice 3 is answered with option A) | 4 | about 450 | S1 |
| S6 Cross-cutting pages | module-interactions, regulation-matrix, adoption-guide, best-practices, faq, README link | 6 | about 1,000 | S2 to S5 (links to every module page) |

S2 to S5 can run in parallel after S1; S6 is last. Each sub-issue repeats the verification of Phase 7 for its own files.

What the code, docs and open issues say today (read on 2026-11-11), which corrects the issue's original text:

- `docs/compliance/` does not exist. Every other documentation folder that groups pages uses `index.md` (`docs/features`, `docs/guides`, `docs/tutorials`, `docs/architecture`, `docs/sharding`, `docs/messaging`, `docs/examples`), and the theme (just-the-docs) takes its parent page from the folder's `index.md` with `has_children: true`. The hub entry is therefore `docs/compliance/index.md`, not `README.md` (Design Choice 4).
- Module names follow the code: Lawful Basis is `Encina.Compliance.LawfulBasis`; the DPA module is `Encina.Compliance.ProcessorAgreements`; crypto-shredding is `Encina.Marten.GDPR`; field encryption is `Encina.Security.Encryption`.
- Registration names that exist (checked in `src/`): `AddEncinaPII`, `AddEncinaEncryption`, `AddEncinaConsent`, `AddEncinaRetention`, `AddEncinaDataSubjectRights`, `AddEncinaMartenGdpr`, `AddEncinaAnonymization`, `AddEncinaDataResidency`, `AddEncinaBreachNotification`, `AddEncinaReadAuditing`, `AddEncinaLawfulBasis`, `AddEncinaGDPR`, `AddEncinaDPIA`, `AddEncinaAIAct`, `AddEncinaNIS2`, `AddEncinaPrivacyByDesign`, `AddEncinaCrossBorderTransfer`, `AddEncinaProcessorAgreements`, `AddEncinaMessageEncryption`, `AddEncinaABAC`, `AddEncinaBreakTheGlass`, `AddEncinaAntiTampering`, `AddEncinaSanitization`, `AddEncinaSecrets` and `AddEncinaAttestation`. **Not found in `src/`**: `AddEncinaComplianceResolver` and `AddEncinaComplianceOPA` (Levels 4 and 5 of the issue's adoption guide), and the issue's `AddEncinaNIS2Compliance` (the method is `AddEncinaNIS2`).
- The three modules the issue marks planned (resolver #686, rule engine #687, OPA #688) are open, carry no `needs-decision` label and have no code in `src/`; the issue's `RegulationConflictStrategy` FAQ answer describes them and must not be published as a feature.
- Packages in `src/` the original list did not name: `Encina.Compliance.Attestation`, `Encina.Security.BreakTheGlass`, `Encina.Security.AntiTampering`, `Encina.Security.Secrets` and its four providers (AwsSecretsManager, AzureKeyVault, GoogleCloudSecretManager, HashiCorpVault). By file name, there is no page under `docs/features/` for anonymization, privacy-by-design or break-the-glass, so those module pages carry more content than a thin pointer.
- The compliance modules default to blocking, and warn-only is the explicit opt-out (`docs/architecture/module-dependency-matrix.md`, #2409); the "opt-in everything" wording of the issue is corrected to: registering a module is opt-in, and a registered module fails closed (SPEC-002 REQ-019, DEC-006; AGENTS.md section 3).
- Provider count: the 10 database providers of AGENTS.md section 5 (the issue's earlier "13" included SQLite and Oracle, which are out, ADR-009 and ADR-024).
- SPEC-002 (`docs/specifications/SPEC-002-eu-regulatory-readiness.md`) scopes the regulation set to EU law and Spanish national law; the issue's Level 4 sample with `US-CA` and `BR` regions and its FAQ answer on CCPA and LGPD are outside that scope (Dilemma 2).

**Provider category**: none for the hub itself; module pages state which of the 10 database providers (and Marten, for event-sourced modules) each module supports, read from the code.

**Affected packages**: none changed. Documented: the compliance and security packages listed in the issue plus those named above.

**Size per sub-issue**: stated in the table above; each is within the 15-file and 1,500-line limit.

---

## Design Choices

<details>
<summary><strong>1. Module page strategy — thin pages that point to the existing feature page, or full pages</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Thin Reference pages in `docs/compliance/modules/` that give the regulation mapping, the registration method, the options table pointer, provider support and the interactions, and link the existing `docs/features/` page for the full usage; a full page only where no feature page exists (anonymization, privacy-by-design, break-the-glass)** | Meets "reference, don't repeat"; no duplicated usage text; each page small and reviewable | The reader follows a link for the detailed usage |
| **B) Full pages as the issue's eleven-section template describes, copying the content of the feature pages** | Self-contained hub | Duplicates more than twenty existing pages; two copies drift; contradicts the acceptance criterion "No duplication with existing `docs/features/`" |
| **C) Move the existing feature pages into `docs/compliance/modules/` and redirect** | One copy | A large rename across the site, README and package READMEs; links in code comments; out of scope of a documentation hub issue |

### Chosen Option: **A — Thin pages that link the feature pages (recommended, pending the maintainer)**

### Rationale

- Scenario where A wins: the feature pages are kept as the detailed guides and the hub only maps regulation to module; this is what the acceptance criteria say.
- Scenario where B wins: the maintainer wants the hub to read as one self-contained manual and accepts the duplication and the upkeep.
- Scenario where C wins: the maintainer wants to retire the "mixed mood" feature pages (the skill's section 1 says they are mixed today) in favour of one structure; it is a larger change with its own issue.
- A module page uses one fixed structure: what it does and which regulation it addresses; articles covered; registration (a method copied from `src/`); options (pointer to the feature page or to the options class); provider support; integration with other modules (a small Mermaid diagram); observability (health check, metrics, spans found in the package); link to the feature page and to the ADRs.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Non-EU jurisdictions in the adoption guide and the FAQ</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep the documentation to EU law and Spanish national law, as SPEC-002 scopes it; the FAQ answers "What about CCPA or LGPD?" with "outside the documented scope" and a link to SPEC-002; no `US-CA` or `BR` sample** | Every statement matches a scoped, reviewed regulation; no promise about law Encina has not been assessed against | A reader outside the EU finds no mapping |
| **B) Keep the issue's samples and FAQ answer for CCPA and LGPD, marked as examples of the planned resolver** | Shows what conditional enforcement is for | Presents untested legal equivalence ("GDPR modules cover most requirements") for laws SPEC-002 does not assess; the resolver sample cannot compile |
| **C) Add a short section on how the modules might map to other regimes, without claims** | Informs readers | Still legal content outside the assessed scope |

### Chosen Option: **A — EU and Spanish scope only (recommended, pending the maintainer)**

### Rationale

- Scenario where A wins: the documentation must make only claims that SPEC-002 backs, and the reference application is Spanish.
- Scenario where B or C wins: the maintainer wants the hub to speak to a wider market now and accepts unassessed legal wording.
- Territorial non-applicability is the only reason a control may be disabled, and it must be justified, audited and logged (SPEC-002 REQ-019, DEC-006); the FAQ states that without implying equivalence for other laws.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. The three planned modules (#686, #687, #688) — separate pages, or one section</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Three separate pages (`compliance-resolver.md`, `rule-engine.md`, `opa.md`) marked planned, with the expected scope from the issues** | Matches the issue's task list and acceptance criterion | Placeholder pages for code that does not exist; the skill says a page ships complete for its quadrant or it does not ship; expected scope can change when the issues are designed |
| **B) One "Planned modules" table in `architecture.md` (name, issue, one-line scope, status) and no separate pages; pages are written in the pull request that implements each module** | No placeholder pages; no promise beyond the issues | The issue's acceptance criterion is changed (three planned pages become three table rows) |
| **C) Wait: no mention until the modules exist** | Nothing to go stale | Readers see no sign of the roadmap; the issue's Level 4 and 5 samples have nowhere to land |

### Chosen Option: **B — One planned-modules table in `architecture.md` (recommended, pending the maintainer)**

### Rationale

- Scenario where B wins: the skill's rule against placeholder and "coming soon" pages applies, and the implementing pull requests are the right place for the real pages.
- Scenario where A wins: the maintainer wants the three pages to exist now as the specification of intent and accepts that they will be rewritten.
- Scenario where C wins: the maintainer wants the hub to describe only what ships.
- Under B the adoption guide has four levels that compile (Level 0 to Level 3) and a prose-only note for the planned Levels 4 and 5 with no code block; under A the two samples may appear as planned and not compiled.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Hub entry file and navigation — `docs/compliance/index.md` with `has_children: true`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `docs/compliance/index.md` (title "Compliance", `has_children: true`) and children with `parent: "Compliance"`; module pages in `docs/compliance/modules/` with an intermediate parent page** | The theme builds the navigation from front matter, as `docs/guides` and `docs/features` do | An extra `modules/index.md` file for the third level |
| **B) `docs/compliance/README.md` as the issue words it** | Renders on GitHub when browsing the folder | The site does not take a parent from it, so the pages need another parent; GitHub renders `index.md` less prominently |
| **C) No `modules/` subfolder: all module pages directly under `docs/compliance/`** | Two levels of navigation | Eighteen or more sibling files in one folder |

### Chosen Option: **A — `index.md` with `has_children`, and `modules/index.md` for the third level**

### Rationale

- The `modules/index.md` file is created by S2 and counted there as its seventh file; S3 to S5 may only merge after S2 (or each carries the file if S2 is delayed, with the second pull request dropping it).
- The issue's URL `docs/compliance/README.md` is replaced by `docs/compliance/index.md`; the README of the repository links the hub.

**Class:** Settled
**Settled by:** `docs/_config.yml` (just-the-docs, `nav_enabled: true`) and `.claude/skills/encina-docs/SKILL.md` section 2 (navigation comes from front matter, `parent:` is the index page's title); the existing folders `docs/features/index.md` and `docs/guides/index.md`.

</details>

<details>
<summary><strong>5. Industry tools relationship — claims limited to what each vendor publishes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A table of the ten tools of the issue with a one-line description of the layer each occupies, each row citing the vendor's own documentation read on a stated date, and "complementary" or "provider" as the relationship** | Verifiable; same method as the competitive-landscape assessment of 2026-10-10 | Vendor pages change; the date states when it was read |
| **B) The issue's table as written, with integration phrases such as "Purview classifies, Encina enforces"** | Short | States an integration that no Encina code implements |

### Chosen Option: **A — Cited relationship, no integration claim**

### Rationale

- Rows for HashiCorp Vault, Azure Key Vault (and the two other secret providers) point at real packages: `Encina.Security.Secrets.HashiCorpVault`, `.AzureKeyVault`, `.AwsSecretsManager`, `.GoogleCloudSecretManager`. OPA is a planned provider (#688) and is listed as planned, not as an available integration.
- The "Layer 5 Encina" diagram stays; the other layers are drawn as context, not as claims about those tools.

**Class:** Settled
**Settled by:** `docs/engineering/assessments/2026-10-10-competitive-landscape.md` (same sourcing method: read on a stated date, cited); `.claude/skills/encina-docs/SKILL.md` section 3 rule 4; AGENTS.md section 3 (fail-closed wording must not overstate).

</details>

<details>
<summary><strong>6. Regulation matrix statuses — read from the code and SPEC-002, never typed from memory</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each row names the module and the type or feature that implements the article, with a status of implemented, partial or not covered, and cites SPEC-002 where it states the scope; no coverage numbers** | Reviewable; consistent with the evidence rule | A row is a small research task |
| **B) Copy the issue's tick-and-circle table** | Fast | The original statuses were estimates from before the modules were implemented |

### Chosen Option: **A — Evidence per row**

### Rationale

- NIS2 and AI Act rows are written against the implemented `Encina.Compliance.NIS2` and `Encina.Compliance.AIAct` packages and against SPEC-002's table of the legal state (for example that Spain has not transposed NIS2).
- Coverage percentages come only from `<!-- covref: ... -->` citations (SPEC-001); the page types none.

**Class:** Settled
**Settled by:** AGENTS.md section 9 (documentation never types coverage figures; covref markers) and SPEC-001; SPEC-002 (scope and legal state); the issue's own alternatives list (item 7).

</details>

<details>
<summary><strong>7. Split of #689 into six sub-issues</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Six sub-issues (S1 to S6) in the table of the Summary, each a pull request within 15 files and 1,500 lines** | Within the pull request limit; S2 to S5 run in parallel; each reviewable | Six briefs; S6 waits for the others |
| **B) One pull request of about 35 files** | One review | Rejected by the spawn hook and the `pr-size` job unless `size-exception-approved`; unreviewable |
| **C) Three larger pull requests of about 12 files each** | Fewer briefs | Module pages vary in length; 12 files at about 100 lines each is near the line limit and leaves no margin |

### Chosen Option: **A — Six sub-issues**

### Rationale

- AGENTS.md section 11 requires an oversized issue to be decomposed into worker-sized sub-issues before any worker is briefed; the issue's own acceptance criterion repeats it.

**Class:** Settled
**Settled by:** AGENTS.md section 11 (a worker's issue is one pull request: at most 15 files and 1,500 changed lines; an oversized issue is decomposed before any worker is briefed; the `pr-size` job); #2900 and #2901.

</details>

---

## Implementation Phases

Each of Phases 1 to 6 is one sub-issue and one pull request; Phase 7 is the verification each of them repeats. The sub-issue files are written by the orchestrator from these phases with the `open-issue` skill (a `[FEATURE]` sub-issue each, parent #689).

### Phase 1: S1 — Foundations (`docs/compliance/index.md`, `industry-tools.md`, `architecture.md`)

<details>
<summary>Tasks</summary>

1. `docs/compliance/index.md` (front matter `title: "Compliance"`, `layout: default`, `nav_order`, `has_children: true`): what Encina does at application level and what it does not (the issue's sections 1.1 and 1.2), the layer diagram in Mermaid (Layer 5 highlighted), the design principles corrected for fail-closed behaviour (Design Choice 4 and the Summary), a map to the hub's pages, and the scope statement (EU and Spanish law, SPEC-002).
2. `docs/compliance/industry-tools.md`: the ten-tool table of Design Choice 5, each row with a source and a reading date.
3. `docs/compliance/architecture.md`: how enforcement enters the pipeline (`IPipelineBehavior<,>`), the attribute system (`[ProcessesPersonalData]`, `[RequireConsent]`, `[RetentionPeriod]`, `[PersonalData]` and the others found in `src/`), enforcement modes (Block, Warn, Disabled) with the default, how a registered module fails closed, and the planned modules table (#686, #687, #688) of Design Choice 3.
4. `docs/features/index.md`: one entry linking the hub.
5. `README.md` is not edited here; the README link goes with S6 so that it points to a complete hub.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library. Worktree: <worktree> (absolute paths only). You write Explanation pages of the Diataxis model for an architect evaluating Encina for compliance-sensitive applications. Plan: docs/plans/compliance-docs-hub-implementation-plan-689.md, sub-issue S1.

TASK
Create docs/compliance/index.md, docs/compliance/industry-tools.md and docs/compliance/architecture.md, and add one entry to docs/features/index.md, as in Phase 1 of the plan. Read first: docs/specifications/SPEC-002-eu-regulatory-readiness.md, docs/architecture/module-dependency-matrix.md, docs/architecture/adr/018-cross-cutting-integration-principle.md, docs/engineering/assessments/2026-10-10-competitive-landscape.md, docs/features/gdpr-compliance.md and the pipeline behaviors of the compliance packages.

KEY RULES
- The hub entry is index.md with has_children: true (not README.md); module pages will use parent "Compliance".
- Registration of a module is opt-in; a registered module fails closed and warn-only is an explicit logged opt-out (SPEC-002 REQ-019, DEC-006, AGENTS.md section 3). Do not write "no compliance feature is mandatory" without this.
- Industry tools: state only what the vendor publishes, with a source and a reading date; claim no integration that Encina code does not implement. OPA is planned (#688), not available.
- Scope is EU and Spanish law; do not discuss CCPA, LGPD or other regimes (pending the maintainer's answer to Design Choice 2).
- Mermaid only, no ASCII art; English only, no emojis; no hand-typed coverage figures; markdownlint and link check clean; commit locally, never push.

REFERENCE FILES
.claude/skills/encina-docs/SKILL.md, .claude/skills/encina-docs/diataxis.md, docs/specifications/SPEC-002-eu-regulatory-readiness.md, docs/architecture/adr/index.md, src/Encina.Compliance.GDPR/, src/Encina.Security/
```

</details>

### Phase 2: S2 — GDPR module pages A

<details>
<summary>Tasks</summary>

1. Create `docs/compliance/modules/index.md` (title "Modules", `parent: "Compliance"`, `has_children: true`) and six module pages with `parent: "Modules"`: `consent.md`, `lawful-basis.md`, `data-subject-rights.md`, `anonymization.md`, `retention.md`, `data-residency.md`.
2. Each page follows the structure of Design Choice 1 (thin page): purpose and regulation articles; registration method copied from `src/`; options pointer; provider support (which of the 10 database providers and Marten, read from the package); integration with other modules (a small Mermaid diagram); observability (health check, metrics, spans in the package); links to the feature page (`consent-management.md`, `lawful-basis-validation.md`, `data-subject-rights.md`, `data-retention.md`, `data-residency.md`) and the ADRs (ADR-014 for residency, ADR-031 for retention erasure).
3. `anonymization.md` has no feature page today, so it carries the usage (registration, options, the pseudonymization and anonymization operations found in `Encina.Compliance.Anonymization`).
4. Articles listed per page come from the module's code and XML docs, not from the issue table.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library. Worktree: <worktree> (absolute paths only). Sub-issue S2 of docs/plans/compliance-docs-hub-implementation-plan-689.md; S1 (docs/compliance/index.md) is merged.

TASK
Create docs/compliance/modules/index.md and the six pages listed in Phase 2, each from the module's package (src/Encina.Compliance.Consent, LawfulBasis, DataSubjectRights, Anonymization, Retention, DataResidency).

KEY RULES
- Thin pages: link docs/features/*.md for usage; do not copy it. anonymization.md has no feature page and carries its own usage.
- Every registration method, option and attribute is found in src/ first; compile any C# sample in a scratch project.
- Provider support is read from the package (10 database providers, Marten for event-sourced modules); say which are not supported.
- Event-sourced modules have no InMemory stores (AGENTS.md section 3): do not suggest one.
- English only, no emojis, Mermaid only, no coverage figures typed by hand; markdownlint and link check clean; commit locally, never push.

REFERENCE FILES
.claude/skills/encina-docs/SKILL.md, docs/features/consent-management.md, lawful-basis-validation.md, data-subject-rights.md, data-retention.md, data-residency.md, docs/architecture/adr/014-data-residency-gdpr-chapter-v.md, docs/architecture/adr/031-retention-erasure-port.md
```

</details>

### Phase 3: S3 — GDPR module pages B

<details>
<summary>Tasks</summary>

1. Six module pages under `docs/compliance/modules/`: `breach-notification.md`, `read-auditing.md`, `crypto-shredding.md`, `pii-masking.md`, `encryption.md`, `sanitization.md`.
2. Same structure as Phase 2; links to `breach-notification.md`, `read-auditing.md`, `crypto-shredding.md`, `pii-masking.md`, `field-level-encryption.md`, `sanitization.md` under `docs/features/`, and to ADR-019, ADR-020, ADR-034 and ADR-036 where they apply.
3. `crypto-shredding.md` states that it applies to event-sourced data stored with Marten (`Encina.Marten.GDPR`, registration `AddEncinaMartenGdpr`) and the erasure interaction with retention (ADR-031).
4. `read-auditing.md` distinguishes the three audit stores of ADR-036 (the Summary of that ADR is linked, not restated).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library. Worktree: <worktree> (absolute paths only). Sub-issue S3 of docs/plans/compliance-docs-hub-implementation-plan-689.md; S1 and the modules index are merged.

TASK
Create the six pages listed in Phase 3 from src/Encina.Compliance.BreachNotification, src/Encina.Security.Audit, src/Encina.Marten.GDPR, src/Encina.Security.PII, src/Encina.Security.Encryption and src/Encina.Security.Sanitization.

KEY RULES
- Thin pages that link docs/features/*.md; no copied usage text.
- Every identifier is found in src/; log and trace statements in samples never carry EncinaError.Message or exception messages (AGENTS.md section 3).
- State the provider support read from each package.
- Link ADRs; do not restate their decisions beyond one sentence.
- English only, no emojis, Mermaid only; markdownlint and link check clean; commit locally, never push.

REFERENCE FILES
.claude/skills/encina-docs/SKILL.md, docs/features/breach-notification.md, read-auditing.md, crypto-shredding.md, pii-masking.md, field-level-encryption.md, sanitization.md, docs/architecture/adr/019-compliance-event-sourcing-marten.md, 020-temporal-crypto-shredding-audit-store.md, 034-crypto-shredding-through-the-stj-contract.md, 036-three-audit-stores.md
```

</details>

### Phase 4: S4 — Advanced module pages

<details>
<summary>Tasks</summary>

1. Eight pages under `docs/compliance/modules/`: `dpia.md`, `processor-agreements.md`, `privacy-by-design.md`, `cross-border.md`, `nis2.md`, `ai-act.md`, `abac.md`, `message-encryption.md`.
2. Same structure; links to `dpia.md`, `processor-agreements.md`, `cross-border-transfer.md`, `nis2-compliance.md`, `aiact-compliance.md`, `authorization.md` and `abac/`, `message-encryption.md` under `docs/features/`. `privacy-by-design.md` has no feature page and carries its usage.
3. `nis2.md` and `ai-act.md` state the legal state from SPEC-002 section 3 (for example that Spain has not transposed NIS2 and that the AI Act text was amended in 2026) in one sentence each with the SPEC link, and never claim a certification.
4. Registration names: `AddEncinaNIS2` (not `AddEncinaNIS2Compliance`), `AddEncinaAIAct`, `AddEncinaDPIA`, `AddEncinaProcessorAgreements`, `AddEncinaPrivacyByDesign`, `AddEncinaCrossBorderTransfer`, `AddEncinaABAC`, `AddEncinaMessageEncryption`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library. Worktree: <worktree> (absolute paths only). Sub-issue S4 of docs/plans/compliance-docs-hub-implementation-plan-689.md; S1 and the modules index are merged.

TASK
Create the eight pages listed in Phase 4 from src/Encina.Compliance.DPIA, ProcessorAgreements, PrivacyByDesign, CrossBorderTransfer, NIS2, AIAct, src/Encina.Security.ABAC and src/Encina.Messaging.Encryption.

KEY RULES
- Thin pages that link docs/features/*.md; privacy-by-design.md carries its own usage.
- Legal state comes from SPEC-002, cited; no certification or compliance guarantee wording.
- Registration names are copied from src/ (AddEncinaNIS2, not AddEncinaNIS2Compliance).
- ABAC has a documentation tree under docs/features/abac/: link it, do not copy it.
- English only, no emojis, Mermaid only; markdownlint and link check clean; commit locally, never push.

REFERENCE FILES
.claude/skills/encina-docs/SKILL.md, docs/features/dpia.md, processor-agreements.md, cross-border-transfer.md, nis2-compliance.md, aiact-compliance.md, authorization.md, message-encryption.md, docs/specifications/SPEC-002-eu-regulatory-readiness.md
```

</details>

### Phase 5: S5 — Modules the issue did not list

<details>
<summary>Tasks</summary>

1. Four pages under `docs/compliance/modules/`: `attestation.md` (`Encina.Compliance.Attestation`, link `audit-attestation.md`), `break-the-glass.md` (`Encina.Security.BreakTheGlass`, no feature page, carries its usage), `anti-tampering.md` (`Encina.Security.AntiTampering`, link `anti-tampering.md`), `secrets.md` (`Encina.Security.Secrets` and its four providers, link `secrets-management.md` and the four provider pages).
2. If the maintainer answers Design Choice 3 with option A, add `compliance-resolver.md`, `rule-engine.md` and `opa.md` as planned pages (7 files in total, still within the limit); otherwise none.
3. Same thin structure as Phase 2.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library. Worktree: <worktree> (absolute paths only). Sub-issue S5 of docs/plans/compliance-docs-hub-implementation-plan-689.md; S1 and the modules index are merged.

TASK
Create the four pages of Phase 5 (attestation, break-the-glass, anti-tampering, secrets) from src/Encina.Compliance.Attestation, src/Encina.Security.BreakTheGlass, src/Encina.Security.AntiTampering and src/Encina.Security.Secrets with its four providers. Add the three planned pages only if the brief says Design Choice 3 was answered with option A.

KEY RULES
- Thin pages that link docs/features/*.md; break-the-glass.md carries its own usage.
- A planned page, if requested, says what the linked issue states and nothing more; it has no code block that does not compile.
- Every identifier is found in src/; secrets samples never print a secret value; options classes with secrets are shown with their [JsonIgnore] and ToString rule (AGENTS.md section 3).
- English only, no emojis, Mermaid only; markdownlint and link check clean; commit locally, never push.

REFERENCE FILES
.claude/skills/encina-docs/SKILL.md, docs/features/audit-attestation.md, anti-tampering.md, secrets-management.md, secrets-management-hashicorpvault.md, docs/architecture/adr/022-provider-agnostic-attestation.md
```

</details>

### Phase 6: S6 — Cross-cutting pages

<details>
<summary>Tasks</summary>

1. `docs/compliance/module-interactions.md` (Explanation): the interaction map as Mermaid, rebuilt from `docs/architecture/module-dependency-matrix.md` and the module pages (the issue's diagram is the starting point, re-checked edge by edge), at least five event chains traced in code (for example DSR erasure to crypto-shredding to audit), and the retention-versus-erasure conflict resolved as ADR-031 states.
2. `docs/compliance/regulation-matrix.md` (Reference): GDPR articles, NIS2 requirements and AI Act requirements, each row with the module, the implementing type or feature and a status, with evidence per Design Choice 6; at least the counts the issue asks for if the code supports them, fewer if not (a row without evidence is not written).
3. `docs/compliance/adoption-guide.md` (How-to): Levels 0 to 3 with samples that compile, using the registration names listed in the Summary (Level 2 uses `AddEncinaConsent`, `AddEncinaRetention`, `AddEncinaDataSubjectRights`); Levels 4 and 5 as one prose note linking #686 and #688 (Design Choice 3), no code.
4. `docs/compliance/best-practices.md` (How-to): actionable recommendations, each tied to a module page or rule; keep at least the issue's list that the code supports.
5. `docs/compliance/faq.md` (Explanation): answers that link to the pages; no answer about other legal regimes (Design Choice 2); no answer describing `RegulationConflictStrategy` or the resolver as available.
6. `README.md`: one link to the hub.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library. Worktree: <worktree> (absolute paths only). Sub-issue S6 of docs/plans/compliance-docs-hub-implementation-plan-689.md; S1 to S5 are merged, so every module page exists.

TASK
Create module-interactions.md, regulation-matrix.md, adoption-guide.md, best-practices.md and faq.md under docs/compliance/, and add one link to the hub in README.md, as in Phase 6.

KEY RULES
- Each edge of the interaction diagram and each row of the regulation matrix is backed by code in src/ or a SPEC-002 statement; rows without evidence are not written.
- Adoption samples compile in a scratch project; Levels 4 and 5 have no code (AddEncinaComplianceResolver and AddEncinaComplianceOPA do not exist).
- No claim about CCPA, LGPD or other regimes; no promise of compliance or certification.
- Coverage figures only through covref markers (SPEC-001); the pages type none.
- One Diataxis quadrant per page; link the module pages instead of repeating them.
- English only, no emojis, Mermaid only; markdownlint, link check and cov-docs-render --check-dangling clean; commit locally, never push.

REFERENCE FILES
.claude/skills/encina-docs/SKILL.md, docs/architecture/module-dependency-matrix.md, docs/specifications/SPEC-002-eu-regulatory-readiness.md, docs/specifications/SPEC-001-coverage-docref-citations.md, docs/architecture/adr/031-retention-erasure-port.md, docs/compliance/modules/
```

</details>

### Phase 7: Verification (repeated by every sub-issue)

<details>
<summary>Tasks</summary>

1. Run the checks of the encina-docs skill, section 5: `lychee --offline --config .github/lychee.toml 'docs/**/*.md' 'README.md'`, `npx --yes markdownlint-cli2 "docs/**/*.md" "#docs/INVENTORY.md"` and `cov-docs-render.cs --check-dangling`; when a tool is missing, say so and check relative links with `Test-Path`.
2. Compile every C# sample of the sub-issue in a scratch project outside the repository; paste the result in the PR.
3. For each page list the types and registration methods checked in `src/` (a short evidence list in the PR description).
4. Validate each Mermaid block renders (GitHub preview or the `docs.yml` build).
5. Run `docs-reviewer` on the sub-issue's pages.
6. `docs/INVENTORY.md`: add the pages if it lists them. No changelog fragment unless the maintainer says documentation is user-visible. No `src/`, PublicAPI, EventId or coverage-manifest change.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library. Worktree: <worktree> (absolute paths only). The pages of one sub-issue of docs/plans/compliance-docs-hub-implementation-plan-689.md are committed.

TASK
Run the verification of Phase 7, fix every finding and report the exact output of each command plus the evidence list.

KEY RULES
- Do not skip a tool silently: report a missing tool and check relative links with Test-Path.
- Compile samples in a scratch project outside the repository tree and paste the result.
- No changes under src/, no PublicAPI, EventId or coverage-manifest edits.
- English only, no emojis, no AI attribution; never push.

REFERENCE FILES
.claude/skills/encina-docs/SKILL.md sections 5 and 6, .github/lychee.toml, .markdownlint.json, .github/scripts/cov-docs-render.cs, docs/INVENTORY.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard or specification | Relevance | Where it applies |
|---------------------------|-----------|------------------|
| SPEC-002 (EU regulatory readiness) | Scope of regulations, legal state of NIS2 and the AI Act, fail-closed rule REQ-019 and DEC-006 | Overview, NIS2 and AI Act pages, regulation matrix, FAQ |
| GDPR (Regulation (EU) 2016/679), articles 4(5), 5, 6 to 8, 13 to 22, 25, 30, 32 to 35, 44 to 49, 89 | Article-to-module mapping | Module pages, regulation matrix |
| NIS2 (Directive (EU) 2022/2555) and the AI Act (Regulation (EU) 2024/1689 as amended) | Requirements mapped to `Encina.Compliance.NIS2` and `Encina.Compliance.AIAct` | Module pages, regulation matrix |
| LOPDGDD (Spain) | Spanish national layer named by SPEC-002 | Module pages where SPEC-002 cites it |
| ADR-014, 019, 020, 031, 034, 036 | Residency, event-sourced compliance, crypto-shredding, retention erasure, audit stores | Linked from the module pages |
| SPEC-001 | Citations instead of typed figures | Regulation matrix, any coverage statement |
| Diátaxis | One quadrant per page | `.claude/skills/encina-docs/diataxis.md` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| Per-module feature pages | docs/features/ (consent-management.md, data-subject-rights.md, crypto-shredding.md and the other compliance pages) | linked from the hub, not repeated |
| Features index | docs/features/index.md | gains a compliance hub link |
| Module dependency matrix | docs/architecture/module-dependency-matrix.md | source for the module-interactions page |
| SPEC-002 | docs/specifications/SPEC-002-eu-regulatory-readiness.md | scope of the regulations and the fail-closed rule |
| ADR-014 data residency | docs/architecture/adr/014-data-residency-gdpr-chapter-v.md | linked from the residency page |
| ADR-019 compliance event sourcing | docs/architecture/adr/019-compliance-event-sourcing-marten.md | linked |
| ADR-020 and ADR-034 | docs/architecture/adr/ | crypto-shredding decisions, linked |
| ADR-031 retention erasure port | docs/architecture/adr/031-retention-erasure-port.md | linked |
| ADR-036 audit stores | docs/architecture/adr/036-three-audit-stores.md | linked |
| Compliance packages | src/Encina.Compliance.* | registration methods and options read for each module page |
| Security packages | src/Encina.Security.* | registration methods and options read for each module page |
| Coverage manifests | .github/coverage-manifest/ | status citations (covref) instead of typed figures |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| None | None | Documentation only: no `[LoggerMessage]`, so nothing to register in `src/Encina/Diagnostics/EventIdRanges.cs` |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Sub-issue S1 foundations (docs/compliance/index.md, industry-tools.md, architecture.md, features/index.md link) | 4 | new and modified |
| Sub-issue S2 GDPR module pages A (consent, lawful-basis, data-subject-rights, anonymization, retention, data-residency) | 7 | new; includes docs/compliance/modules/index.md |
| Sub-issue S3 GDPR module pages B (breach-notification, read-auditing, crypto-shredding, pii-masking, encryption, sanitization) | 6 | new |
| Sub-issue S4 advanced module pages (dpia, processor-agreements, privacy-by-design, cross-border, nis2, ai-act, abac, message-encryption) | 8 | new |
| Sub-issue S5 modules the issue did not list (attestation, break-the-glass, anti-tampering, secrets) | 4 | new; 7 if the three planned pages are requested (Design Choice 3, option A) |
| Sub-issue S6 cross-cutting (module-interactions, regulation-matrix, adoption-guide, best-practices, faq, README link) | 6 | new and modified |
| Total | 35 | six pull requests, each within 15 files |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt — all sub-issues</summary>

```text
PROJECT CONTEXT
Encina is a pre-1.0 .NET 10 / C# 14 library (no backward compatibility, no [Obsolete]). You are a docs-writer in a git worktree <worktree> (absolute paths only; never touch the main checkout). Rules: AGENTS.md and .claude/skills/encina-docs/SKILL.md. Issue: https://github.com/dlrivada/Encina/issues/689, executed as six sub-issues (S1 to S6), one pull request each. No production code changes.

IMPLEMENTATION OVERVIEW
1. S1: docs/compliance/index.md (hub, has_children), industry-tools.md, architecture.md (pipeline, attributes, enforcement modes, planned-modules table), link from docs/features/index.md.
2. S2 to S5 (parallel after S1): thin module pages under docs/compliance/modules/ (index.md created by S2), each linking the existing docs/features/ page; full pages only for anonymization, privacy-by-design and break-the-glass.
3. S6: module-interactions.md, regulation-matrix.md, adoption-guide.md (Levels 0 to 3 compile; Levels 4 and 5 prose only), best-practices.md, faq.md, README link.
4. Every sub-issue: verify (lychee, markdownlint-cli2, cov-docs-render check, compile samples, evidence list, Mermaid render), run docs-reviewer, commit locally.

KEY PATTERNS
- Hub entry is index.md, not README.md; children use parent "Compliance" / "Modules".
- Registration is opt-in; registered modules fail closed; warn-only is an explicit logged opt-out (SPEC-002 REQ-019, DEC-006).
- Registration names are copied from src/ (AddEncinaNIS2, not AddEncinaNIS2Compliance); AddEncinaComplianceResolver and AddEncinaComplianceOPA do not exist.
- Scope is EU and Spanish law (SPEC-002); no claims about other regimes; no compliance or certification promise.
- Thin pages link docs/features/*.md and ADRs; no duplication; coverage figures only through covref markers.
- Answers to Dilemmas 1 to 3 (recommended: thin pages, EU and Spanish scope only, one planned-modules table) are applied once the maintainer confirms them.
- One Diataxis quadrant per page, every identifier verified in src/, English only, no emojis, Mermaid only.

REFERENCE FILES
docs/plans/compliance-docs-hub-implementation-plan-689.md, .claude/skills/encina-docs/SKILL.md, docs/specifications/SPEC-002-eu-regulatory-readiness.md, docs/architecture/module-dependency-matrix.md, docs/features/, src/Encina.Compliance.*, src/Encina.Security.*, src/Encina.Marten.GDPR
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Documentation only, no runtime code. |
| 2 | OpenTelemetry | ❌ | Documentation only; the module pages record the metrics and spans each module already emits, read from the code, and change nothing. |
| 3 | Structured Logging | ❌ | Documentation only; no `[LoggerMessage]`, so no EventId range is reserved. |
| 4 | Health Checks | ❌ | Documentation only; the module pages name each module's health check where one exists (see #907 for the health check pages). |
| 5 | Validation | ❌ | Documentation only, no runtime code. |
| 6 | Resilience | ❌ | Documentation only, no runtime code. |
| 7 | Distributed Locks | ❌ | Documentation only, no runtime code. |
| 8 | Transactions | ❌ | Documentation only, no runtime code. |
| 9 | Idempotency | ❌ | Documentation only, no runtime code. |
| 10 | Multi-Tenancy | ❌ | Documentation only; the architecture page documents tenant-aware compliance as it exists, no runtime change. |
| 11 | Module Isolation | ❌ | Documentation only, no runtime code. |
| 12 | Audit Trail | ❌ | Documentation only; the hub documents `Encina.Security.Audit` and the three audit stores of ADR-036, no runtime change. |

---

## Prerequisites & Dependencies

- #686, #687 and #688 (resolver, rule engine, OPA): open, no `needs-decision` label, not prerequisites. The hub documents them as planned only (Design Choice 3), and Levels 4 and 5 of the adoption guide wait for them.
- #907 (health checks documentation, plan `health-checks-docs-implementation-plan-907.md`): the module pages link its reference page only when it is merged.
- S1 must merge before S2 to S5; S6 after all of them. The orchestrator opens the sub-issues with parent #689 and the dependencies as `blocked-by` links.

---

## Next Steps

1. The orchestrator links this plan from #689, opens the six sub-issues (S1 to S6, each a `[FEATURE]` with its own acceptance by command and the file list of its phase) and asks the maintainer about Dilemmas 1, 2 and 3 when work on the first sub-issue starts. Dilemma 3 changes only S5 and S6; Dilemma 1 changes S2 to S5; Dilemma 2 changes S1 and S6.
2. Brief S1 first (it also needs Dilemma 2 for its scope statement).
3. After S6 merges, the orchestrator re-checks the hub against #686 to #688 if any has landed in the meantime.
