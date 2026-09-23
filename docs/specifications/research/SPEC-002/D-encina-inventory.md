# Encina — EU Regulatory Compliance Inventory

Read-only inventory of the Encina repository (main branch, clean working tree, HEAD `0376289b`). Compiled 2026-09-23. No files were edited and no issues were opened as part of producing this document.

---

## 1. Packages

### 1.1 Compliance packages (`src/Encina.Compliance.*`)

All 15 packages below are the exact list SPEC-000 REQ-024 names as "the 1.0 list". Provider column reflects actual `.csproj` references, not filename guesses — see note under the table.

| Package | Purpose (from README) | Main abstractions | Regulation / Articles claimed | README | Manifest | Marten (ES) |
|---|---|---|---|---|---|---|
| `Encina.Compliance.GDPR` | Processing-activity tracking with automatic RoPA generation and lawful-basis validation | `IProcessingActivityRegistry`, `IGDPRComplianceValidator`, `IRoPAExporter`, `IDataProtectionOfficer`, `ILawfulBasisSubjectIdExtractor` | GDPR Art. 30 (RoPA), Art. 6 (lawful basis, LIA) | Yes | Yes | No (stateless registry) |
| `Encina.Compliance.Consent` | Declarative, attribute-based consent enforcement with full lifecycle via Marten ES | `IConsentService`, `IConsentValidator` | GDPR Art. 6(1)(a), 7, 8 — README does **not** carry the ePrivacy article table SPEC-000 REQ-024 requires (no "ePrivacy" or "2002/58" string found) | Yes | Yes | Yes |
| `Encina.Compliance.LawfulBasis` | Art. 6(1) lawful-basis management incl. EDPB three-part Legitimate Interest Assessment | `ILawfulBasisProvider`, `ILawfulBasisService` | GDPR Art. 6(1) | Yes | Yes | Yes |
| `Encina.Compliance.DataSubjectRights` | Processing-restriction enforcement, DSR request lifecycle, access/export/erasure orchestration | `IDSRService`, `IDataErasureExecutor`, `IDataErasureStrategy`, `IDataPortabilityExporter`, `IPersonalDataLocator`, `IExportFormatWriter` | GDPR Art. 12, 15–22 | Yes | Yes | Yes |
| `Encina.Compliance.DataResidency` | Region-based routing, cross-border transfer validation, data-location tracking | `IRegionRouter`, `IRegionContextProvider`, `IDataLocationService`, `ICrossBorderTransferValidator`, `IAdequacyDecisionProvider`, `IResidencyPolicyService` | GDPR Chapter V (Art. 44–49) | Yes | Yes | Yes |
| `Encina.Compliance.Retention` | Declarative retention enforcement, automatic expiration, legal holds | `IRetentionPolicyService`, `IRetentionRecordService`, `ILegalHoldService` | GDPR Art. 5(1)(e) (storage limitation) | Yes | Yes | Yes |
| `Encina.Compliance.Anonymization` | Anonymization, pseudonymization, tokenization techniques | `IAnonymizer`, `IPseudonymizer`, `ITokenizer`, `IAnonymizationTechnique`, `IRiskAssessor`, `IKeyProvider`, `ITokenMappingStore`, `IAnonymizationAuditStore` | GDPR Art. 4(5) (pseudonymization definition) | **No README** | Yes | No (stateless, per ADR-019) |
| `Encina.Compliance.BreachNotification` | 72-hour breach workflow: detection, supervisory-authority/data-subject notification, phased reporting, deadline monitoring | `IBreachDetector`, `IBreachDetectionRule`, `IBreachNotificationService`, `IBreachNotifier` | GDPR Art. 33, 34 | Yes | Yes | Yes |
| `Encina.Compliance.DPIA` | DPIA risk-assessment engine, DPO consultation workflow, expiration monitoring | `IDPIAService`, `IDPIAAssessmentEngine`, `IDPIATemplateProvider`, `IRiskCriterion` | GDPR Art. 35, 36 | Yes | Yes | Yes |
| `Encina.Compliance.PrivacyByDesign` | Data-minimization analysis, purpose-limitation, privacy-by-default pipeline behavior | `IPrivacyByDesignValidator`, `IDataMinimizationAnalyzer`, `IPurposeRegistry` | GDPR Art. 25 | **No README** | Yes | No (no own persistent state, per ADR-019) |
| `Encina.Compliance.CrossBorderTransfer` | SCC compliance, Transfer Impact Assessment (TIA), approved-transfer tracking | `ITransferValidator`, `ISCCService`, `ITIAService`, `ITIARiskAssessor`, `IApprovedTransferService`, `ITransferExpirationQueryService` | GDPR Chapter V, Schrems II | **No README** | Yes | Yes |
| `Encina.Compliance.ProcessorAgreements` | Processor registry, DPA lifecycle, sub-processor hierarchy, mandatory-terms validation | `IProcessorService`, `IDPAService` | GDPR Art. 28 | Yes | Yes | Yes |
| `Encina.Compliance.NIS2` | Stateless rule engine for the 10 mandatory risk-management measures, MFA/encryption/supply-chain checks, incident timelines | `INIS2ComplianceValidator`, `INIS2MeasureEvaluator`, `IMFAEnforcer`, `IEncryptionValidator`, `ISupplyChainSecurityValidator`, `INIS2IncidentHandler` | NIS2 Directive (EU 2022/2555) Art. 20, 21(2) | Yes | Yes | No |
| `Encina.Compliance.AIAct` | Risk classification, prohibited-practices detection, human-oversight enforcement, transparency obligations at pipeline level | `IAIActClassifier`, `IAIActComplianceValidator`, `IAISystemRegistry`, `IHumanOversightEnforcer`, `IDataQualityValidator`, `IAIActDocumentation` | EU AI Act (EU 2024/1689) — README states "risk classification, prohibited practices, human oversight, transparency" without enumerating articles | Yes | Yes | No (migration to ES tracked, open issue #847) |
| `Encina.Compliance.Attestation` | Provider-agnostic tamper-evident audit attestation; cryptographic proof + retroactive-modification detection | `IAuditAttestationProvider`, `IAttestationReceiptStore` | EU AI Act Art. 13, GDPR Art. 5.2 (accountability), NIS2 incident-evidence requirements | Yes | Yes | No |

Note on providers: every `Encina.Compliance.*` `.csproj` that references `EntityFrameworkCore`/`Dapper`/`MongoDB` packages (AIAct, Anonymization, BreachNotification, DataResidency, DataSubjectRights, DPIA, GDPR, ProcessorAgreements, Retention) does **not** contain a `DbContext`, `IDbConnection` or `IMongoDatabase` usage anywhere in `src/` — those references back health-check/DI extension points, not parallel store implementations. Per **ADR-019**, the 9 stateful modules (Consent, DataSubjectRights, LawfulBasis, BreachNotification, DPIA, ProcessorAgreements, Retention, DataResidency, CrossBorderTransfer) persist **exclusively through Marten/PostgreSQL event sourcing** — there is intentionally no EF/Dapper/ADO/Mongo store for them, which is a deliberate deviation from the Multi-Provider Implementation Rule in `CLAUDE.md` (compliance modules are one of the two named exceptions, event sourcing being the other). Anonymization and PrivacyByDesign are explicitly excluded from ES (stateless tools/pipeline behavior). GDPR core, NIS2 and Attestation are stateless/registry-based and never had a store. AIAct currently has **no persistence abstraction migrated to ES yet** (issue #847, open, milestone v0.16.0).

### 1.2 Test maturity (file counts per flag, `tests/Encina.*Tests/Compliance/<Area>`)

| Package | Unit | Integration | Guard | Contract | Property |
|---|---:|---:|---:|---:|---:|
| AIAct | 19 | **0** | 1 | 1 | 1 |
| Anonymization | 26 | 2 | 15 | 3 | 4 |
| Attestation | 5 | **0** | 1 | 1 | 1 |
| BreachNotification | 17 | 2 | 19 | 7 | 8 |
| Consent | 11 | 1 | 4 | 1 | 1 |
| CrossBorderTransfer | 15 | 3 | 19 | 5 | 9 |
| DataResidency | 27 | 2 | 20 | 1 | 5 |
| DataSubjectRights | 25 | **0** | 23 | 2 | 1 |
| DPIA | 21 | 3 | 11 | 2 | 4 |
| GDPR | 16 | **0** | 2 | 2 | 1 |
| LawfulBasis | 21 | 1 | 7 | 2 | 2 |
| NIS2 | 14 | 2 | 5 | 2 | 5 |
| PrivacyByDesign | 14 | 1 | 6 | 3 | 5 |
| ProcessorAgreements | 18 | 3 | 21 | 1 | 8 |
| Retention | 29 | 1 | 20 | 1 | 4 |

AIAct, Attestation, DataSubjectRights and GDPR have **zero integration-test files**, and there is no `.md` justification for that gap anywhere under `tests/` (justification files exist only for AIAct/DataSubjectRights **LoadTests** and **BenchmarkTests**, not IntegrationTests). Per `CLAUDE.md`'s testing standards, an IntegrationTests gap on a database/persistence-adjacent feature must be either filled or justified in writing — here it is neither.

### 1.3 Audit & event-sourced audit trail

| Package | Purpose | Main abstractions | Regulation claims | README | Manifest | Tests (U/IT/G/C/P) |
|---|---|---|---|---|---|---|
| `Encina.Security.Audit` | `IAuditStore`/`IReadAuditStore` abstractions, PII masking on audit entries, in-memory implementations | `IAuditStore`, `IReadAuditStore`, `IAuditEntryFactory`, `IPiiMasker`, `IReadAuditContext` | Generic audit trail; referenced by GDPR/NIS2/SOX use cases in ROADMAP | **No README** | Yes | 29/21/14/4/2 |
| `Encina.Audit.Marten` | Event-sourced `IAuditStore` on Marten/PostgreSQL with **temporal crypto-shredding** | (implements `IAuditStore`) | Positions itself for "GDPR retention, SOX + NIS2 + GDPR simultaneously"; no article-level table | Yes | Yes | 15 unit / 12 guard (AuditMarten folder) |
| `Encina.Marten.GDPR` | Crypto-shredding for Marten event-sourced systems: per-subject PII encryption, key deletion | (crypto-shredding helpers, not a store per se) | GDPR Art. 17 ("Right to be Forgotten") | Yes | Yes | 10 (Marten/GDPR folder, unit only found) |

### 1.4 Security packages (`src/Encina.Security.*`)

| Package | Purpose | Main abstractions | Regulation/standard claims | README | Manifest | Tests (U/IT/G/C/P) | Providers |
|---|---|---|---|---|---|---|---|
| `Encina.Security` | Transport-agnostic authorization pipeline behavior | attribute-based authorization | None specific | Yes | Yes | — | n/a |
| `Encina.Security.ABAC` | XACML 3.0 ABAC engine (subject/resource/action/environment attributes) | policy engine, PAP/PDP-style types | XACML 3.0 (OASIS standard, not EU regulation) | Yes | Yes | 46/8/39/6/2 | Has a persistent Policy Administration Point (issue #691, closed) |
| `Encina.Security.ABAC.Analyzers` | Roslyn analyzers for ABAC | — | n/a | **No README** | Yes | — | n/a |
| `Encina.Security.AntiTampering` | HMAC request signing, integrity verification, replay protection | `IRequestSigner`, `IRequestSigningClient`, `IKeyProvider`, `INonceStore` | Cited by Attestation as supporting NIS2 incident-evidence integrity, not itself regulation-scoped | Yes | Yes | 6/1/1/0/1 | n/a |
| `Encina.Security.Encryption` | Attribute-based field-level encryption (AES-256-GCM) | `[Encrypt]` pipeline | Cited as PCI-DSS/GDPR-relevant in ROADMAP, no article table | Yes | Yes | 11/1/1/1/1 | n/a |
| `Encina.Security.PII` | Attribute-based PII masking/data protection | `IPiiMasker`-adjacent, `[PII]` | GDPR-adjacent (masking), no article table | Yes | Yes | 18/0/3/1/1 | n/a |
| `Encina.Security.Sanitization` | Attribute-based input sanitization / output encoding | — | Generic security hygiene, not EU-regulation specific | Yes | Yes | 13/0/4/0/1 | n/a |
| `Encina.Security.Secrets` (+ AWS/Azure/GCP/HashiCorp satellites) | Secrets-management abstractions, ISP-style interfaces, DI-first, in-memory cache | `ISecretReader`, `ISecretWriter`, `ISecretRotator`, `ISecretRotationHandler` | None EU-regulation specific (referenced by NIS2 as an encryption/secret-hygiene dependency) | Yes (core + all 4 satellites) | Yes (all 5) | 46/1/24/2/1 (core) | 4 secret-vault backends: AWS Secrets Manager, Azure Key Vault, Google Cloud Secret Manager, HashiCorp Vault |

### 1.5 Encryption satellites (`src/Encina.Messaging.Encryption*`, `src/Encina.Security.Encryption`)

| Package | Purpose | README |
|---|---|---|
| `Encina.Messaging.Encryption` | Transparent AES-256-GCM payload encryption for Outbox/Inbox messages | Yes |
| `Encina.Messaging.Encryption.AwsKms` | AWS KMS key-provider backend | Yes |
| `Encina.Messaging.Encryption.AzureKeyVault` | Azure Key Vault key-provider backend | Yes |
| `Encina.Messaging.Encryption.DataProtection` | ASP.NET Core Data Protection key-provider backend | Yes |

All four have coverage manifests; no GCP/HashiCorp KMS backend exists for messaging encryption (only Secrets management has all 4 vault backends — messaging encryption has 3, missing a Google/HashiCorp-equivalent KMS backend).

---

## 2. Docs

### 2.1 `docs/features/*` compliance docs

Present: `gdpr-compliance.md`, `consent-management.md`, `lawful-basis-validation.md`, `data-subject-rights.md`, `data-residency.md`, `data-retention.md`, `crypto-shredding.md`, `breach-notification.md`, `dpia.md`, `cross-border-transfer.md`, `processor-agreements.md`, `nis2-compliance.md`, `aiact-compliance.md`, `audit-attestation.md`, `audit-marten.md`, `audit-tracking.md`, `anti-tampering.md`, `abac.md`, `authorization.md`, `security-authorization.md`, `field-level-encryption.md`, `message-encryption.md`, `pii-masking.md`, `sanitization.md`, `secrets-management.md` + 4 provider-specific secrets docs. There is **no `docs/features/*.md` file for Anonymization or PrivacyByDesign** either — confirmed by directly listing the directory (no `anonymiz*` or `privacy-by-design*` filename exists). Combined with §1.1 (no README for those two packages) and §1.1's Anonymization/CrossBorderTransfer/PrivacyByDesign README gaps, **PrivacyByDesign and Anonymization have no dedicated narrative documentation anywhere in the repo** — only XML doc comments and test names describe their behavior. CrossBorderTransfer has a feature doc but no package README.

### 2.2 SPEC-000 — 1.0 Baseline and Release Scope

`docs/specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md`, status **APPROVED 2026-09-21**.

- **DEC-002** (referenced at lines 21–23, 133–134, and the "Regulatory compliance (DEC-002)" section at line 77): compliance with the EU regulatory framework (GDPR + Digital Omnibus amendments, ePrivacy, NIS2, AI Act) is **part of the 1.0 contract**, not optional. New regulation packages beyond the ones that already exist (DORA, eIDAS2, Data Act, ENS, EHDS — issues #804–#808) are explicitly **post-1.0**.
- **REQ-024**: every 1.0 compliance package (the same 15 listed in §1.1) must have **its own specification** listing the articles it covers, the articles it explicitly does not cover, and the evidence (tests/audit records) per covered article. It also requires the ePrivacy Directive (2002/58/EC Art. 5(3), cookie-consent EDPB guidance) to be covered *only* by `Encina.Compliance.Consent`'s specification, with an ePrivacy article table next to the GDPR one. **None of these per-package specifications exist yet** — `docs/specifications/` contains only SPEC-000 and SPEC-001 (coverage citations). This is the single largest documentation gap against the 1.0 contract (see §4).
- **REQ-025**: the AI Act module must implement the obligations in **EPIC #881** (Arts. 9, 10, 10.2f, 11, 12, 13/50, 14, 43, 51–56, plus multi-tenancy, module isolation, Marten migration) before 1.0.
- **REQ-026**: the NIS2 lifecycle and Digital Omnibus adaptations in **EPIC #880** (#822–#827, #810–#816) must ship before 1.0; the five new regulation packages in that EPIC's neighborhood (DORA, eIDAS2, Data Act, ENS, EHDS) are confirmed post-1.0.

### 2.3 `docs/plans/*` compliance plans

Implementation plans: `aiact-implementation-plan-415.md`, `breach-notification-implementation-plan-408.md`, `dpia-implementation-plan-409.md`, `nis2-implementation-plan-414.md`, `privacy-by-design-implementation-plan-411.md`, `retention-implementation-plan-406.md`.

Marten/event-sourcing migration plans (per ADR-019): `aiact-marten-es-migration-plan.md`, `breach-notification-es-migration-plan-780.md`, `consent-marten-migration-plan-777.md`, `dpia-es-migration-plan-781.md`, `retention-es-migration-plan-783.md`.

No equivalent implementation-plan file exists for: Consent (#403), LawfulBasis (#413), DataSubjectRights (#404), DataResidency (#405), Anonymization (#407), ProcessorAgreements (#410), CrossBorderTransfer (#412), GDPR core (#402), Attestation (#803) — these packages shipped before the "every FEATURE issue gets a plan" rule was formalized, or their plans were not persisted under this naming convention.

### 2.4 ROADMAP.md

- Line 77 / 179–223: **v0.13.0 "Security & Compliance"** (closed, EPIC #668) is the milestone that delivered all 15 compliance packages plus the security packages, the Marten ES migration, `Encina.Audit.Marten`, NIS2 and AI Act — this section is accurate and matches shipped code.
- Lines 78–83 and 227–310 describe **v0.14.0 through v0.19.0** with titles that **no longer match the live GitHub milestones** (ROADMAP.md says v0.15.0 = "Messaging & EIP" and v0.16.0 = "Multi-Tenancy & Modular"; the actual GitHub milestones, per §3, are v0.15.0 = "EU Compliance: NIS2 & Digital Omnibus" and v0.16.0 = "AI Act"). **ROADMAP.md is stale relative to the milestone renumbering that happened after SPEC-000 was approved** — this is a documentation gap in itself (see §4).
- Lines 523–538 list the "Compliance Patterns - GDPR & EU Laws" backlog (dated "based on December 29, 2025 research") enumerating the same 15 packages plus NIS2/AI Act as "planned" — now all shipped, so this section is historical/superseded but not marked as such.

### 2.5 ADRs

| ADR | Title | Relevance |
|---|---|---|
| ADR-014 | Data Residency — GDPR Chapter V | Architecture for `Encina.Compliance.DataResidency` (pipeline-level enforcement vs. row-level security / network-level / middleware-only alternatives) |
| ADR-019 | Compliance Event Sourcing Strategy with Marten | Defines which 9 modules migrate to Marten ES and why 2 (Anonymization, PrivacyByDesign) do not; crypto-shredding for Art. 17 |
| ADR-020 | Temporal Crypto-Shredding Audit Store | Underlies `Encina.Audit.Marten`'s crypto-shredding design |
| ADR-030 | Encryption at the Serializer Level | Underlies `Encina.Messaging.Encryption` / `Encina.Security.Encryption` design |

No ADR exists yet for NIS2, AI Act, or Attestation architecture specifically (their design rationale lives only in their READMEs/implementation plans, not a numbered ADR).

---

## 3. Tracking (GitHub issues & milestones)

### 3.1 Milestones (relevant ones; `gh api repos/dlrivada/Encina/milestones?state=all --paginate`)

| # | Title | State | Open | Closed |
|---:|---|---|---:|---:|
| 48 | v0.14.0 — Hardening | open | 65 | 4 |
| 49 | **v0.15.0 — EU Compliance: NIS2 & Digital Omnibus** | open | 14 | 0 |
| 50 | **v0.16.0 — AI Act** | open | 13 | 0 |
| 18 | Post-1.0: EU Regulatory Compliance: DORA, eIDAS2, Data Act, ENS, EHDS (deferred items) | open | 6 | 1 |
| 21 | Post-1.0: Compliance Completion & Test Coverage (deferred items) | open | 8 | 3 |
| 22 | Post-1.0: AI Act Compliance & Testing Infrastructure (deferred items) | closed | 0 | 0 (empty — likely superseded by milestone 50) |
| 10 | Post-1.0: Security Core Completion (deferred items) | closed | 0 | 57 |

Confirms the task brief's premise: v0.15.0 is the NIS2/Digital-Omnibus milestone and v0.16.0 is the AI Act milestone (ROADMAP.md's table for these two milestone numbers is out of date, see §2.4).

### 3.2 v0.15.0 — EU Compliance: NIS2 & Digital Omnibus (all 14 issues open)

EPIC **#880** "v0.16.1 — EU Regulatory Compliance" is the umbrella. Children: #822 (Incident Lifecycle, Art. 23), #823 (Supply Chain, Art. 21.2.d), #824 (Risk Analysis, Art. 21.2.a), #825 (Coordinated Vulnerability Disclosure, Art. 12), #826 (Management Accountability, Art. 20), #827 (Reporting/Read Models). Digital Omnibus adaptations: #810 (consent cooldown), #811 (consent-exempt purposes), #812 (ENISA Single Entry Point adapter), #813 (breach severity threshold), #814 (DSAR rejection reasons), #815 (AI-training legitimate interest), #816 (DPIA EDPB criteria loader).

### 3.3 v0.16.0 — AI Act (all 13 issues open)

EPIC **#881** "v0.16.2 — AI Act Compliance & Testing Infrastructure" is the umbrella. Children map 1:1 to AI Act articles: #836 Risk Management (Art. 9), #837 Data Governance (Art. 10), #838 Bias Detection (Art. 10.2f), #839 Human Oversight (Art. 14), #840 Technical Documentation (Art. 11), #841 Transparency (Art. 13+50), #842 Record-Keeping (Art. 12), #843 GPAI Governance (Arts. 51–56), #844 Conformity Assessment (Art. 43), #845 Multi-Tenancy, #846 Module Isolation, #847 Marten ES migration.

### 3.4 Post-1.0 deferred compliance milestones

- **Milestone 18** (DORA/eIDAS2/DataAct/ENS/EHDS): #804 DORA, #805 eIDAS2, #806 DataAct, #807 ENS, #808 EHDS, plus #764 (DPIA resilience policies). Parent EPIC **#809** "EU Digital Omnibus Regulation — Impact Assessment and Preparedness" is **closed** (the assessment is done; the 5 regulation packages it spawned remain open and post-1.0).
- **Milestone 21** (Compliance Completion & Test Coverage): #686/#687/#688 (compliance applicability resolver / rule engine / OPA integration — a generalized "which regulation applies where" layer, not yet built), #798 (multi-tenancy for Security.Audit/ABAC), #775 (module isolation for ProcessorAgreements), #753, #636, #635. Parent EPIC **#873** "v0.13.6 — Compliance Completion & Test Coverage" lives in milestone 48 (v0.14.0 — Hardening), not in milestone 21 itself.

### 3.5 EPICs found

| EPIC | Title | Milestone | State |
|---|---|---|---|
| #668 | v0.13.0 — Security & Compliance | Post-1.0: Compliance Completion & Test Coverage (deferred) | Closed |
| #809 | EU Digital Omnibus Regulation — Impact Assessment and Preparedness | Post-1.0: EU Regulatory Compliance (deferred) | Closed |
| #873 | v0.13.6 — Compliance Completion & Test Coverage | v0.14.0 — Hardening | Open |
| #880 | v0.16.1 — EU Regulatory Compliance (NIS2/Omnibus) | v0.15.0 — EU Compliance: NIS2 & Digital Omnibus | Open |
| #881 | v0.16.2 — AI Act Compliance & Testing Infrastructure | v0.16.0 — AI Act | Open |

### 3.6 Search coverage

Searched via `gh issue list --state all --search "<term>"` for: GDPR, RGPD, NIS2, "AI Act", AIAct, "Data Act", DORA, eIDAS, CRA, "Cyber Resilience", EHDS, MDR, ENS, "ISO 27001", compliance, privacy, consent, retention, breach, DPIA, omnibus.

- **RGPD, CRA, MDR**: zero hits — Encina uses "GDPR" exclusively (no Spanish "RGPD" term in issues), has no issue framed around the EU Cyber Resilience Act by that name (DORA's #804 is operational-resilience, not CRA/product-cybersecurity), and no Medical Device Regulation issue exists.
- **"Cyber Resilience"** and **ISO 27001**: only incidental hits (DORA issue text, NIS2 supply-chain issue, CDC audit-log-streaming issue) — no dedicated package or issue targets CRA or ISO 27001 certification directly.
- Virtually every compliance-tagged issue found has a milestone assigned; only one unrelated, already-closed issue (#1089, about `.slnx` project membership, which merely mentions AIAct/Consent in its title) came back with `milestone:NONE`.
- Relevant labels exist: `area-gdpr`, `area-compliance`, `area-security`, `area-audit`, `area-auditing`.

---

## 4. Gaps visible from the code and docs alone

1. **REQ-024 per-package specifications do not exist.** SPEC-000 (approved 2026-09-21) requires all 15 compliance packages to have a specification enumerating covered/uncovered articles and evidence, and requires `Encina.Compliance.Consent`'s spec to carry an ePrivacy Art. 5(3) table. `docs/specifications/` contains only SPEC-000 and SPEC-001 — none of the 15 required specs exist, and the Consent README itself does not mention ePrivacy or Directive 2002/58/EC anywhere. This is the largest gap between the written 1.0 contract and the repository's current state.
2. **Four compliance packages have no IntegrationTests and no justification file**: AIAct, Attestation, DataSubjectRights, GDPR (core). `CLAUDE.md` requires either real integration tests or a `.md` justification for database-adjacent features; justification files exist for AIAct/DataSubjectRights LoadTests and BenchmarkTests but not for the missing IntegrationTests.
3. **Three compliance packages have no README**: `Encina.Compliance.Anonymization`, `Encina.Compliance.CrossBorderTransfer`, `Encina.Compliance.PrivacyByDesign`, plus `Encina.Security.Audit` and `Encina.Security.ABAC.Analyzers` outside compliance. `CrossBorderTransfer` at least has a `docs/features/cross-border-transfer.md` doc, so that gap is just package-level (NuGet) discoverability. **`Anonymization` and `PrivacyByDesign` have neither a README nor a `docs/features/*.md` file** — there is no narrative documentation for either package anywhere in the repository, only XML doc comments and test names.
4. **AIAct has not migrated to Marten event sourcing** (issue #847, open, in the v0.16.0 milestone) — it is the only one of the "should eventually be stateful" compliance modules still without an ES aggregate, and is tracked but not yet resolved.
5. **ROADMAP.md's milestone table (v0.14.0–v0.19.0 titles) is stale.** The live GitHub milestones were renumbered/retitled after SPEC-000 was approved (v0.15.0 is now "EU Compliance: NIS2 & Digital Omnibus", v0.16.0 is now "AI Act"), but `ROADMAP.md` lines 78–83 and 241–267 still describe v0.15.0 as "Messaging & EIP" and v0.16.0 as "Multi-Tenancy & Modular" with different linked milestone numbers. Anyone reading ROADMAP.md alone would misidentify which milestone ships EU compliance work.
6. **Regulations mentioned in the roadmap/backlog but genuinely absent from `src/`**: DORA, eIDAS2 (eIDAS 2.0), EU Data Act, ENS (Spanish national security scheme), EHDS (European Health Data Space) — all tracked as open `[FEATURE]` issues (#804–#808) under a milestone explicitly named "Post-1.0" and under EPIC #809 (closed, impact-assessment done). This is a **known and tracked** gap, not an undocumented one — consistent with SPEC-000 DEC-002's explicit deferral.
7. **No package targets the EU Cyber Resilience Act (CRA), ISO 27001 certification support, or MDR** by name anywhere in the issue tracker; NIS2's supply-chain-security work (#823) is the closest adjacent coverage. If any of these are wanted for a post-1.0 roadmap they currently have no placeholder issue.
8. **Messaging-level payload encryption (`Encina.Messaging.Encryption.*`) has 3 KMS backends (AWS, Azure, ASP.NET Data Protection) vs. 4 secret-vault backends in `Encina.Security.Secrets.*` (adds Google Cloud + HashiCorp Vault)** — an asymmetry between the two encryption/secrets provider families that isn't called out anywhere as intentional.
9. **A generic "which regulation applies where" layer does not exist yet**: issues #686 (Compliance Applicability Resolver), #687 (Rule-Based Compliance Engine / declarative regulation DSL) and #688 (OPA integration) are all open, unassigned to the 1.0 milestones, sitting in the Post-1.0 "Compliance Completion & Test Coverage" milestone — today, applicability (which regulation, which tenant, which jurisdiction) is implicit in which package/attribute a developer chooses to apply, not resolved by the framework.
10. **No ADR exists for NIS2, AI Act or Attestation** specifically (unlike DataResidency/ADR-014, the ES migration/ADR-019, and crypto-shredding/ADR-020) — their design rationale is only in READMEs and `docs/plans/*-implementation-plan-*.md` files, not a numbered architecture decision record.

---

## Sources consulted

- `src/Encina.Compliance.*`, `src/Encina.Security.*`, `src/Encina.Audit.Marten`, `src/Encina.Marten.GDPR`, `src/Encina.Messaging.Encryption*` (READMEs, `.csproj` files, `I*.cs` interfaces)
- `.github/coverage-manifest/*.json` (existence check only)
- `tests/Encina.{UnitTests,IntegrationTests,GuardTests,ContractTests,PropertyTests,LoadTests,BenchmarkTests}/{Compliance,Security,AuditMarten,Marten}/*`
- `docs/features/*.md`, `docs/specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md`, `docs/plans/*compliance/gdpr/nis2/aiact*`, `ROADMAP.md`, `docs/architecture/adr/{014,019,020,030}-*.md`
- `gh api repos/dlrivada/Encina/milestones?state=all --paginate`, `gh issue list --repo dlrivada/Encina --state all --search "<term>"` for the 21 terms listed in §3.6, `gh label list`
