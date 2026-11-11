# Implementation Plan: ISO/IEC 27001 and 27701 Control-Mapping Documents

> **Issue**: [#1252](https://github.com/dlrivada/Encina/issues/1252)
> **Type**: Feature (documentation only)
> **Complexity**: Low (5 phases, 2 pages, no code)
> **Estimated Scope**: ~450 lines of documentation in 5 files; no production code, no tests

---

## Summary

Write two reference pages under `docs/` that map ISO/IEC 27001:2022 Annex A controls and ISO/IEC 27701:2025 controller and processor controls to the Encina modules that help implement them, with the evidence for each mapping and what stays with the organisation. It tracks SPEC-002 REQ-057 and AC-039 (tracking id P-48, part of #1186). The two standards have different audiences (ISMS and PIMS), so there are two cross-linked pages, as the issue comment records ("Settled by elimination", 2026-10-17).

- **Standards**: ISO/IEC 27001:2022 Annex A; ISO/IEC 27701:2025 (edition 2, now a standalone PIMS standard; the 2019 edition is superseded, per the SPEC-002 fact check `E-factcheck.md`).
- **Controls named by SPEC-002 section 3.4**: A.8.10 information deletion, A.8.11 data masking, A.8.15 logging, A.8.24 use of cryptography, A.8.25 secure development life cycle, A.8.28 secure coding.
- **Estimated scope**: 5 files, about 450 lines (see Research).
- **Affected packages**: none; documentation only (`docs/`).
- **Provider category**: none.
- **Prerequisites**: none open. The evidence cited exists today (the modules below, their feature pages and the SPEC-001 `coverage-citations` gate). Related: #689 (compliance documentation hub) and P-17 (article-coverage specifications) share the same evidence; this plan links to them by number and does not wait for them. INV-002 binds the wording: Encina never claims coverage it does not cover, and no certification is claimed.
- **Note on the issue's acceptance list**: the lines about tenant awareness (REQ-061) and instrumentation (REQ-062) are the generic text of every SPEC-002 capability and apply to code; here they are recorded as not applicable, and the pages only cite where those controls exist in the modules.

---

## Design Choices

<details>
<summary><strong>1. Number of documents</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two documents, cross-linked** | Each page serves its audience (ISMS owner vs PIMS/privacy owner); each page stays short | Two pages to keep current |
| **B) One combined document** | One place | Mixes two standards with different structures and audiences |

### Chosen Option: **A — two cross-linked documents**

### Rationale

- The issue records this by elimination: the two standards have different audiences.

**Class:** Settled
**Settled by:** the issue #1252 (Alternatives Considered and the maintainer comment of 2026-10-17); SPEC-002 REQ-057 ("control-mapping documents")

</details>

<details>
<summary><strong>2. Where the pages live and their quadrant</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `docs/features/iso-27001-control-mapping.md` and `docs/features/iso-27701-control-mapping.md`, reference quadrant, `parent: "Features"`** | Next to the module pages they link to (`pii-masking.md`, `data-retention.md`, `crypto-shredding.md`, `audit-tracking.md`); the Features index has `has_children: true` | The folder is mostly module guides, and these are mappings |
| **B) A new `docs/compliance/` section** | Dedicated entry for regulators and integrators | A new navigation section for two pages, overlapping the compliance hub of #689 |
| **C) Under `docs/specifications/`** | Next to SPEC-002 | Specifications are normative inputs, not user documentation |

### Chosen Option: **A — `docs/features/`, reference quadrant**

### Rationale

- Follows the existing structure and the one-Diataxis-quadrant rule of the documentation skill; the compliance hub of #689 can link to both pages later.

**Class:** Trivial
**Settled by:** `.claude/skills/encina-docs/SKILL.md` (one quadrant per page, just-the-docs front matter); `docs/features/index.md` (`has_children: true`)

</details>

<details>
<summary><strong>3. Row schema and the honesty rule</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One table per standard with columns Control, Encina module and types, Evidence, Status (Covered, Partial, Organisational) and What stays with the organisation** | Every row states its limit; partial coverage is visible | Wider tables |
| **B) A list of controls Encina "supports"** | Short | Invites overclaiming and breaks INV-002 |

### Chosen Option: **A — a status column and an organisational column on every row**

### Rationale

- INV-002: partial coverage is stated as partial and Encina never claims a certification. A library cannot be the whole of an ISMS: policies, risk assessment and operation of the controls stay with the organisation.

**Class:** Settled
**Settled by:** SPEC-002 INV-002; SPEC-002 REQ-057 ("stating what stays with the organisation" in the issue summary)

</details>

<details>
<summary><strong>4. How ISO/IEC 27701:2025 controls are cited (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Map by control theme and title taken from the public ISO overview, and cite clause numbers only for the 27001 Annex A controls** | Accurate without a licensed copy; no risk of a wrong number | A reader must look up the clause numbers themselves |
| **B) Cite 27701:2025 clause numbers from a licensed copy** | Precise and auditable | The standard is paid; the numbers cannot be checked from the repository, and an error would be a false claim (INV-002) |
| **C) Anchor the mapping on the GDPR articles the controls support and name the 27701 theme** | Verifiable from the GDPR modules Encina already documents | A second level of indirection for an ISO auditor |

### Chosen Option: **A — theme-level mapping, numbers only where verified** (recommended, pending the maintainer)

### Rationale

- A wins when nobody on the project holds the licensed text; B wins if the maintainer provides the standard and integrators need clause numbers for their PIMS audit; C wins when the readers are privacy officers who think in GDPR articles.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Breadth of the ISO/IEC 27001 mapping (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The six controls SPEC-002 names, plus a statement that the other Annex A controls are organisational** | Matches REQ-057; every row has evidence; small to keep current | Does not help an organisation fill a full Statement of Applicability |
| **B) All 93 Annex A controls, each marked Covered, Partial or Organisational** | Usable as input for a Statement of Applicability | Large; many rows would say "organisational", and each Covered claim needs evidence |

### Chosen Option: **A — the six named controls** (recommended, pending the maintainer)

### Rationale

- A wins for the 1.0 contract (SPEC-002 section 3.4 and REQ-057 name these controls); B wins if integrators ask for a complete Statement of Applicability input and the maintainer is willing to maintain 93 rows.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Evidence inventory

> **Goal**: for each control, the Encina types, the tests or audit record and the feature page that prove it, before any prose.

<details>
<summary><strong>Tasks</strong></summary>

1. Build the evidence list per control from the modules named in Research: A.8.11 `Encina.Security.PII` (`IPIIMasker`, `PIIMaskingPipelineBehavior`); A.8.10 `Encina.Compliance.Retention` (`IRetentionDataEraser`, `IRetentionPolicyService`, ADR-031) and crypto-shredding (ADR-034); A.8.15 `Encina.Security.Audit` (`IOperationAuditStore`, `AuditPipelineBehavior`, ADR-036), `Encina.Security.AntiTampering` and `EventIdRanges.cs`; A.8.24 `Encina.Security.Encryption` (`IFieldEncryptor`, `IKeyProvider`, ADR-030) and `Encina.Security.Secrets`; A.8.25 and A.8.28 the zero-warnings build, the architecture tests, and the coverage, mutation and CRAP gates.
2. For each type, read the source and confirm that it exists and does what the row will say; note where the behaviour is partial (an in-memory key provider is not key management).
3. For each cited file, find its covref id in the package manifest (`.github/coverage-manifest/<Package>.json`) so the page can cite it.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: SPEC-002 REQ-057 asks for two control-mapping documents; INV-002 forbids claiming coverage that does not exist.
TASK: Produce an evidence table (control, module, types with file paths, ADR, tests, partial or complete) for A.8.10, A.8.11, A.8.15, A.8.24, A.8.25, A.8.28 by reading the code.
KEY RULES: read the code, never infer from names; state partial coverage as partial; no coverage figures typed by hand; write the table to the scratchpad, not to docs/.
REFERENCE FILES: docs/specifications/SPEC-002-eu-regulatory-readiness.md (section 3.4), docs/specifications/research/SPEC-002/C-sector-national.md (section 6), src/Encina.Security.PII, src/Encina.Compliance.Retention, src/Encina.Security.Audit, src/Encina.Security.Encryption.
```

</details>

### Phase 2: ISO/IEC 27001 page

> **Goal**: the Annex A mapping page (decision D2).

<details>
<summary><strong>Tasks</strong></summary>

1. Create `docs/features/iso-27001-control-mapping.md` through the `docs-writer` agent: front matter (`title`, `layout: default`, `parent: "Features"`), what the page is and is not (a mapping aid, not a certificate), the table of the six controls with the schema of choice 3, and a closing statement on the other Annex A controls (D2 option A).
2. Evidence cells cite files with covref markers (`<!-- covref: cov:Encina.Security.PII/PIIMaskingPipelineBehavior.cs:unit -->` style) and link the ADRs and feature pages; one `covref-table` per module where a folder is cited.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Phase 1 produced the evidence table. Decision D2 (breadth) is option A unless the maintainer recorded otherwise.
TASK: Write docs/features/iso-27001-control-mapping.md following the encina-docs skill (reference quadrant).
KEY RULES: no certification claim; every figure through covref/mutref markers (never typed); every identifier exists in src/; status column on every row; "what stays with the organisation" column on every row; English only, no emojis; the reference application is never named.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, docs/features/pii-masking.md, docs/testing/coverage-measurement-methodology.md (DocRef convention), docs/specifications/SPEC-001-coverage-docref-citations.md.
```

</details>

### Phase 3: ISO/IEC 27701 page

> **Goal**: the controller and processor mapping page (decision D1).

<details>
<summary><strong>Tasks</strong></summary>

1. Create `docs/features/iso-27701-control-mapping.md` through the `docs-writer` agent: controller controls mapped to `Encina.Compliance.Consent`, `LawfulBasis`, `DataSubjectRights`, `PrivacyByDesign`, `DPIA`, `Retention`, `CrossBorderTransfer`; processor controls mapped to `Encina.Compliance.ProcessorAgreements` and `BreachNotification`; the same row schema.
2. State the edition (2025, standalone PIMS) and how controls are cited under D1; cross-link to the 27001 page for the security controls both standards share.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: Phase 2 is merged in the working tree; D1 (how 27701:2025 controls are cited) is option A unless recorded otherwise.
TASK: Write docs/features/iso-27701-control-mapping.md in the same format.
KEY RULES: do not invent clause numbers; cite only numbers that were verified; partial coverage stated as partial; link each module to its feature page; citations by covref markers.
REFERENCE FILES: docs/features/data-subject-rights.md, docs/features/consent-management.md, docs/features/processor-agreements.md, docs/features/breach-notification.md, docs/specifications/research/SPEC-002/E-factcheck.md.
```

</details>

### Phase 4: Cross-links and navigation

> **Goal**: the pages are reachable and linked both ways.

<details>
<summary><strong>Tasks</strong></summary>

1. Link both pages from `docs/features/index.md`.
2. Link from the module feature pages the mapping names (for example `pii-masking.md`, `data-retention.md`, `audit-tracking.md`, `field-level-encryption.md`) with one sentence each ("Implements evidence for ISO/IEC 27001 A.8.11"), as the research note of SPEC-002 recommends.
3. If the compliance hub of #689 exists when this is implemented, link from it; otherwise leave a reference in the pull request.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: The two pages exist.
TASK: Add the index entries and the one-sentence back-links in the module pages.
KEY RULES: documentation pages go through the docs-writer; relative links; do not link a file that is not on main; do not claim more than the mapping page claims.
REFERENCE FILES: docs/features/index.md, docs/features/pii-masking.md, docs/features/data-retention.md.
```

</details>

### Phase 5: Verification and Finalization

> **Goal**: prove the citations and the pages.

<details>
<summary><strong>Tasks</strong></summary>

1. `dotnet run --file .github/scripts/cov-docs-render.cs -- --check-dangling` (the `coverage-citations` check) stays green; the link check passes; documentation lint passes.
2. `docs-reviewer` reviews both pages against the `encina-docs` skill (identifiers exist, no hand-typed figures, decisions linked to ADR and SPEC).
3. `changelog.d/1252-iso-control-mapping.added.md` (written by the mechanical-fixer).
4. Test types: no code, so unit, guard, contract, property, integration, load and benchmark tests do not apply; the link check and the `coverage-citations` job are the checks.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: Both pages and the cross-links are written.
TASK: Run the citation check, the link check and the documentation lint; fix every finding; request the docs-reviewer pass; add the changelog fragment through the mechanical-fixer.
KEY RULES: never edit CHANGELOG.md; no coverage figure typed by hand; report the exact commands and their output.
REFERENCE FILES: .github/scripts/cov-docs-render.cs, changelog.d/README.md, .claude/skills/encina-docs/SKILL.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance | Use in this feature |
| :--- | :--- | :--- |
| ISO/IEC 27001:2022 Annex A (93 controls) | A.8.10 information deletion, A.8.11 data masking, A.8.15 logging, A.8.24 use of cryptography, A.8.25 secure development life cycle, A.8.28 secure coding | The six controls SPEC-002 section 3.4 says map to existing modules |
| ISO/IEC 27701:2025 (edition 2, standalone PIMS standard; the 2019 edition is superseded) | Controller and processor controls | Mapped to the GDPR modules |
| SPEC-002 REQ-057 and AC-039 | The two documents exist in `docs/` | The acceptance of this issue |
| SPEC-002 INV-002 | Encina never claims coverage it does not cover; partial coverage is stated as partial | No certification claim, partial stated as partial |
| SPEC-001 | covref and mutref citation markers, `coverage-citations` CI job | Evidence without hand-typed figures |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| PII masking | `src/Encina.Security.PII` (`IPIIMasker`, `PIIMaskingPipelineBehavior`) | Evidence for A.8.11 |
| Retention and erasure | `src/Encina.Compliance.Retention` (`IRetentionDataEraser`, `IRetentionPolicyService`), ADR-031 | Evidence for A.8.10 |
| Crypto-shredding | ADR-020 and ADR-034, `docs/features/crypto-shredding.md` | Evidence for A.8.10 |
| Audit stores | `src/Encina.Security.Audit` (`IOperationAuditStore`, `AuditPipelineBehavior`), ADR-036 | Evidence for A.8.15 |
| Anti-tampering | `src/Encina.Security.AntiTampering` (`IRequestSigner`) | Log and request integrity for A.8.15 |
| Event IDs | `src/Encina/Diagnostics/EventIdRanges.cs` | Structured logging evidence for A.8.15 |
| Encryption | `src/Encina.Security.Encryption` (`IFieldEncryptor`, `IKeyProvider`), ADR-030 | Evidence for A.8.24 |
| Secrets | `src/Encina.Security.Secrets` | Key and secret handling for A.8.24 |
| Build and quality gates | `Directory.Build.props` analyzers, `Encina.Testing.Architecture`, coverage, mutation and CRAP gates | Evidence for A.8.25 and A.8.28 |
| GDPR modules | `src/Encina.Compliance.Consent`, `LawfulBasis`, `DataSubjectRights`, `ProcessorAgreements`, `BreachNotification`, `DPIA`, `CrossBorderTransfer`, `PrivacyByDesign` | The 27701 controller and processor controls |
| Existing feature pages | `docs/features/*.md` (pii-masking, data-retention, crypto-shredding, audit-tracking, field-level-encryption, data-subject-rights) | Link targets of the mapping tables |
| Citation tooling | `.github/scripts/cov-docs-render.cs`, `docs/testing/coverage-measurement-methodology.md` | covref markers |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | Documentation only; no `[LoggerMessage]` code and no range request |

### Estimated File Count

| Category | Files | Notes |
| :--- | :--- | :--- |
| Mapping documents | 2 | ISO/IEC 27001 page and ISO/IEC 27701 page under `docs/features/` |
| Cross-links | 2 | `docs/features/index.md` and the back-links in the module pages counted as one edit set |
| Metadata | 1 | Changelog fragment |
| **Total** | 5 | One worker pull request (limit 15 files, 1500 lines) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are producing the ISO/IEC 27001 and 27701 control-mapping documents for Issue #1252 (SPEC-002 REQ-057, AC-039).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0.
- Documentation goes through the docs-writer agent and follows .claude/skills/encina-docs/SKILL.md.
- INV-002: no coverage claim without evidence, partial stated as partial, no certification claim.

IMPLEMENTATION OVERVIEW:
1. Evidence inventory per control (read the code).
2. docs/features/iso-27001-control-mapping.md (six Annex A controls, decision D2).
3. docs/features/iso-27701-control-mapping.md (controller and processor controls, decision D1).
4. Cross-links from the Features index and the module pages.
5. Citation check, link check, docs-reviewer, changelog fragment.

KEY PATTERNS:
- Table schema: Control, Encina module and types, Evidence, Status, What stays with the organisation.
- Figures only through covref/mutref markers; markers inside code fences are not expanded.
- Link siblings and other plans by issue number, never by file.

REFERENCE FILES:
- docs/specifications/SPEC-002-eu-regulatory-readiness.md, docs/specifications/research/SPEC-002/C-sector-national.md, E-factcheck.md
- docs/features/pii-masking.md, data-retention.md, crypto-shredding.md, audit-tracking.md, field-level-encryption.md
- docs/testing/coverage-measurement-methodology.md, .github/scripts/cov-docs-render.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Documentation only |
| 2 | OpenTelemetry | ❌ | Documentation only; the mapping cites the instrumentation that exists |
| 3 | Structured Logging | ❌ | Documentation only; `[LoggerMessage]` and `EventIdRanges` are cited as A.8.15 evidence |
| 4 | Health Checks | ❌ | No new checkable dependency |
| 5 | Validation | ✅ | The `coverage-citations` job, the link check and the documentation lint validate the pages (Phase 5) |
| 6 | Resilience | ❌ | No call to an external system |
| 7 | Distributed Locks | ❌ | No shared state |
| 8 | Transactions | ❌ | No writes beyond files |
| 9 | Idempotency | ❌ | Not a message or request entry point |
| 10 | Multi-Tenancy | ❌ | Documentation only; multi-tenancy is cited where a control depends on it |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping here; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ❌ | Documentation only; audit stores are cited as evidence, not changed |

---

## Next Steps

1. Review and approve this plan.
2. The maintainer answers D1 and D2 when work on the issue starts; the answers go into the Maintainer Decisions section.
3. Implement Phases 1 to 5 as one worker pull request (5 files, about 450 lines) through the `docs-writer` agent.
4. The final commit references `Fixes #1252`.
