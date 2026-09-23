# Sector-specific, national and standard frameworks — technical capability analysis for Encina

Scope: DORA, eIDAS 2, MDR, EHDS, ENS, ISO 27001, plus PSD2/PSD3+PSR, ISO/IEC 27701, ISO/IEC 42001, IEC 62304, LOPDGDD, and (briefly, for completeness/exclusion) CER Directive, MiCA, and NIS2 (already partly addressed in Encina).
Date of analysis: 2026-09-23. Read-only research; all dates/status marked "as reported" where a search snippet could not be cross-verified against the primary EUR-Lex/BOE text directly.

**Legend for classification**
- 🟢 **horizontal-enabler** — Encina core/messaging/security should provide this as a generic, provider-agnostic capability (any app in any sector benefits).
- 🟡 **sector module** — worth an optional satellite package (`Encina.Compliance.*` style) because the obligation is narrow to one sector/entity type.
- ⚪ **out of scope** — not something an application framework should implement; belongs to organizational process, physical/legal measures, or a different layer.

---

## 1. DORA — Digital Operational Resilience Act

**Official reference:** Regulation (EU) 2022/2554 of 14 December 2022 (OJ L 333, 27.12.2022) — [EUR-Lex](https://eur-lex.europa.eu/eli/reg/2022/2554/oj). Level-2 measures already adopted:
- RTS on ICT risk management framework: Commission Delegated Regulation (EU) 2024/1774 — [EUR-Lex](https://eur-lex.europa.eu/eli/reg_del/2024/1774/oj)
- RTS on classification of major ICT-related incidents/significant cyber threats: Commission Delegated Regulation (EU) 2024/1772 — [EUR-Lex](https://eur-lex.europa.eu/eli/reg_del/2024/1772/oj)
- RTS on incident-report timelines: Commission Delegated Regulation (EU) 2025/301 (reported by secondary sources; verify OJ number before citing in product docs)
- ITS on incident-report content/templates: Commission Implementing Regulation (EU) 2025/302 (same caveat)
- ITS on the Register of Information (ICT third-party register): Commission Implementing Regulation (EU) 2024/2956 — per [ESMA JC 2023 85 final report](https://www.esma.europa.eu/sites/default/files/2024-01/JC_2023_85_-_Final_report_on_draft_ITS_on_Register_of_Information.pdf)
- RTS on threat-led penetration testing (TLPT): adopted mid-2024 per [ESMA/EBA/EIOPA joint report JC 2024-29](https://www.esma.europa.eu/sites/default/files/2024-07/JC_2024-29_-_Final_report_DORA_RTS_on_TLPT.pdf)

**Applies to:** Nearly all regulated EU financial entities — credit institutions, payment/e-money institutions, investment firms, crypto-asset service providers, insurers/reinsurers, CCPs, CSDs, trading venues, fund managers, crowdfunding platforms, credit-rating agencies — **and their "critical" ICT third-party service providers** (Art. 1, Art. 2 DORA; the CTPP oversight regime brings cloud/SaaS/software vendors indirectly into scope). Encina itself, if embedded in a bank/insurer/investment-firm application, is exactly this kind of ICT dependency.

**Dates/status as of 2026-09-23:** DORA has applied since **17 January 2025** (Art. 64). All cited RTS/ITS are already in force. This is a live, enforced regime, not a future one — treat as "now."

**Technical capabilities a framework could support** (Art./RTS references):
1. **ICT risk management framework artifacts** — asset inventory, dependency mapping, encryption-key lifecycle, network segmentation controls (DORA Art. 5–15; RTS 2024/1774 Arts. 2–4 asset management, Arts. 6–10 cryptography/key management, Arts. 13–18 network & infrastructure security). 🟢 *horizontal-enabler*: Encina's existing secrets/encryption/anti-tampering modules map directly here; a "compliance evidence export" for ICT asset inventory would help.
2. **Structured incident detection, classification and reporting pipeline** — capturing incident metadata (services affected, data loss, clients affected, duration, geographic spread) needed to auto-classify "major incident" against the materiality thresholds in RTS 2024/1772 Art. 1–8, then drive the staged notification workflow (initial notification, intermediate, final report — DORA Art. 19–20). 🟡 *sector module*: this is finance-specific reporting-to-authority logic (ESA templates), best as `Encina.Compliance.DORA` built on top of Encina's generic audit/logging primitives, not core.
3. **Structured/append-only audit logging with timestamps and actor identity** — required as evidentiary basis for incident root-cause analysis and ICT risk register (RTS 2024/1774 Art. 11-12 logging & monitoring). 🟢 *horizontal-enabler*: Encina's audit trail / structured logging (EventIdRanges, `IAuditStore`) already generalizes this; DORA is one more consumer, not a special case.
4. **ICT third-party register data model** — a machine-readable inventory of contractual arrangements, sub-outsourcing chains, criticality flags, per Art. 28(3) and ITS 2024/2956 templates. 🟡 *sector module*: this is a finance-regulatory reporting artifact (register of information submitted to competent authorities), not a generic app capability — package it as compliance tooling, not core.
5. **Resilience/failover testing hooks** (Art. 24–27, TLPT for significant entities) — chaos/failover testing is closer to infra/ops tooling than an app framework; Encina could expose health-check/circuit-breaker telemetry (already covered by `Encina.Polly`/resilience providers) that a TLPT exercise would probe. 🟢 the resilience providers already fill this; no new capability needed beyond what ADR-covered resilience packages do.

---

## 2. eIDAS 2 — European Digital Identity Regulation

**Official reference:** Regulation (EU) 2024/1183 amending Regulation (EU) 910/2014 (eIDAS), OJ L, 30.4.2024 — entry into force 20 May 2024; Wikipedia summary cross-checked against the regulation's staged provisions ([Regulation (EU) 2024/1183](https://en.wikipedia.org/wiki/Regulation_(EU)_2024/1183)).

**Applies to:** Member States (must issue an EUDI Wallet), qualified/non-qualified Trust Service Providers, and **"relying parties"** — defined broadly as large online platforms ("very large online platforms" under DSA), providers of services requiring strong authentication, and public-sector bodies — who must be technically capable of *accepting* the wallet and other eIDAS trust services. Any Encina-based business app that does identity verification, e-signature, or regulated-sector onboarding is a potential relying party.

**Dates/status as of 2026-09-23:** In force since May 2024, staged implementation. Per search results: Member States must provide a certified EUDI Wallet by **24 December 2026**; large/obligated private relying parties must accept it **12 months later (≈ December 2027)**. As of today the wallet itself is in pilot/rollout (Large-Scale Pilots concluded 2025, national wallets being certified through 2026) — **not yet mandatory for private relying parties**, but the technical requirement window (2026–2027) is imminent enough to plan for now. Mark as uncertain: exact OJ-cited transposition deadlines should be re-verified against the consolidated eIDAS text before quoting a hard date in customer-facing docs.

**Technical capabilities:**
1. **Relying-party wallet-acceptance protocol support** (OpenID4VP / ISO 18013-5 mDL presentation, attribute-based selective disclosure) for onboarding/authentication flows. 🟡 *sector module* — this is a specific protocol integration (`Encina.Identity.EUDIWallet` style), not a generic framework primitive; most Encina consumers won't need it, but those in regulated onboarding (banking, telco, healthcare, public administration) will.
2. **Qualified electronic signature/seal/timestamp verification** — validating QES/QESeal (per eIDAS Art. 3(12), Annex I/II/III as retained/amended) and Qualified Timestamps against EU Trusted Lists (LOTL). 🟡 *sector module*: a document/e-signature validation package makes sense as optional, similar to validation-provider pattern, but is not core messaging/data-access.
3. **Electronic ledgers** (new qualified trust service under eIDAS 2, similar to a tamper-evident distributed record) for sequencing/timestamping data entries with non-repudiation. 🟢 *horizontal-enabler, partially already covered*: Encina's anti-tampering/audit modules (hash-chaining, append-only stores) are conceptually the same primitive that an "electronic ledger" trust service needs; worth flagging as a natural home rather than a new module, but formal QTSP certification is out of scope for a library.
4. **Qualified certificate / QWAC-based TLS mutual authentication** for server identity. ⚪ *out of scope for a framework* — this is deployment/infrastructure (web server + certificate authority), not application code.

---

## 3. EHDS — European Health Data Space

**Official reference:** Regulation (EU) 2025/327 of 11 February 2025, OJ L, published 5 March 2025, entered into force 26 March 2025 — text mirrored by the Spanish Ministry of Health ([OJ_L_202500327_EN_TXT.pdf](https://www.sanidad.gob.es/areas/saludDigital/espacioEuropeoDS/docs/OJ_L_202500327_EN_TXT.pdf)). Background on the logging requirement: [PubMed 40588953 — "The Logging Component of the EHDS Regulation: A Technical Perspective"](https://pubmed.ncbi.nlm.nih.gov/40588953/).

**Applies to:** Manufacturers/vendors of **EHR systems** (defined product category with CE-style conformity marking obligations similar to MDR), healthcare providers, and — per the EHDS's own cross-reference — makers of medical devices/AI systems that claim EHR interoperability. Public and private sector both; Member States must designate a National Digital Health Authority.

**Dates/status as of 2026-09-23:** In force since 26 March 2025; staged application: general application **26 March 2027**, further milestones **26 March 2029** (priority-category primary-use data exchange — patient summaries, ePrescriptions), **2031**, **2035**. A Member-State milestone of **January 2026** for EHR vendors/providers to begin certifying interoperability/security compliance was reported by secondary sources — treat as uncertain until checked against the regulation's own annex/implementing-act calendar. Overall: this is a **future-dated, staged regime**, useful to track now, not yet enforceable.

**Technical capabilities:**
1. **The "European logging component"** — per the cited technical-perspective paper, this decomposes into five concrete, code-relevant elements: (a) identification of the data accessor, (b) identification of the data subject, (c) categorisation of the accessed data, (d) temporal logging (when access occurred), (e) data-origin tracking (provenance/lineage of the data). 🟢 **horizontal-enabler, high alignment with Encina today**: this is essentially a structured, queryable access-audit-log schema — very close to what `Encina` audit trail + PII/data-masking modules already need to produce. A generic "who accessed what field of which subject's record, when, and where the data came from" audit primitive would satisfy both EHDS and GDPR Art. 30/33 evidentiary needs; recommend exposing it as a first-class `IAccessAuditLog` capability rather than a health-specific one.
2. **Common European EHR exchange format support** (interoperability with FHIR-based EU specifications for patient summaries, ePrescriptions) — 🟡 *sector module*, this is healthcare-domain data modelling/serialization, appropriate for an `Encina.Compliance.EHDS` or `Encina.Healthcare.Interoperability` package, not core.
3. **Security/interoperability self-certification artifact generation** (conformity documentation) — ⚪ *out of scope for a framework*; this is a compliance-process/paperwork obligation on the vendor organization, not something code implements.

---

## 4. MDR — Medical Device Regulation (+ IEC 62304)

**Official reference:** Regulation (EU) 2017/745 of 5 April 2017 on medical devices (OJ L 117, 5.5.2017), fully applicable since 26 May 2021 (after COVID-related delay). Software lifecycle harmonised standard: **IEC 62304:2006 + AMD1:2015** "Medical device software — Software life cycle processes." Background: [Johner Institute — legacy devices](https://blog.johner-institute.com/regulatory-affairs/legacy-devices/), [Celegence — MDR software compliance](https://www.celegence.com/medical-device-software-compliance-eu-regulations-2017-745-2017-746/).

**Applies to:** Manufacturers of medical devices, **including Software as a Medical Device (SaMD)** — i.e., any standalone software with a medical purpose (diagnosis, monitoring, treatment support). Relevant to Encina only indirectly: **if a business application built on Encina is itself a medical device, or embeds Encina as a software component ("SOUP" — Software Of Unknown Provenance) inside a regulated medical device**, then the medical-device manufacturer inherits obligations toward Encina as a third-party component.

**Dates/status as of 2026-09-23:** MDR has been fully applicable since 26 May 2021; in force and enforced. IEC 62304 is a mature, stable standard (last substantive amendment 2015).

**Technical capabilities relevant to Encina (as a potential SOUP component, not as a first-party product):**
1. **SOUP documentation and traceability**: IEC 62304 §7.1.3/§8.1.2 requires manufacturers to document the identity, version, and known anomalies of any Software of Unknown Provenance embedded in a device. Encina should make this easy for downstream manufacturers by publishing precise **version provenance, changelog and known-issue metadata** (already partly satisfied by `CHANGELOG.md`, `PublicAPI.Shipped/Unshipped.txt`, semantic-versioned NuGet packages). 🟢 *horizontal-enabler, already substantially met* — no new capability, just make sure release/versioning discipline (already mandated in CLAUDE.md) stays rigorous, since it is literally what a manufacturer's SOUP dossier will cite.
2. **Software safety classification support (Class A/B/C)**: IEC 62304 §4.3 requires risk-based classification of software items; a framework cannot self-classify (that is the manufacturer's system-level risk analysis), but can support it by **not silently swallowing exceptions and by making failure modes observable** (deterministic error handling via `Either<EncinaError,T>`/ROP is a good fit). ⚪ *out of scope for a framework* to implement "classification" itself — this is a manufacturer-side process, not code.
3. **Deterministic, auditable behavior for verification/validation**: IEC 62304 §5.5–§5.7 (unit/integration/system testing with traceability to requirements). 🟢 Encina's own multi-flag coverage/obligations testing model is directly the kind of evidence a manufacturer would want from a SOUP supplier; no new capability needed, just visibility (e.g., a citable coverage/mutation dashboard, which Encina already has).

**Classification for Encina overall: ⚪ out of scope as a "framework must implement MDR" item** — Encina is not a medical device and should not attempt SaMD-specific business logic. The correct posture is "be a trustworthy, well-documented SOUP dependency," which is a documentation/process discipline, not a new module.

---

## 5. ENS — Esquema Nacional de Seguridad (Spain)

**Official reference:** Real Decreto 311/2022, de 3 de mayo, por el que se regula el Esquema Nacional de Seguridad — [BOE-A-2022-7191](https://www.boe.es/buscar/act.php?id=BOE-A-2022-7191), replacing RD 3/2010. Anexo II defines the security-measures catalogue (per secondary technical guides, e.g. [NexENS — 75 medidas](https://nexens.es/medidas-seguridad-ens), [summumsistemas — Anexo II](https://summumsistemas.es/blog/2026-06-27-ens-anexo-ii-medidas-seguridad)); note sources differ on the exact count (73 vs. 75 measures across editions) — verify against the BOE consolidated text before citing an exact number.

**Applies to:** All Spanish **public-sector entities** ("Administraciones Públicas") and, critically for Encina, **private-sector technology suppliers/contractors that provide services to the public administration** — i.e., any Spanish (or foreign) software vendor selling to Spanish government bodies inherits ENS obligations contractually. This is the clearest "sector module" trigger for a Spain-facing customer of Encina.

**Dates/status as of 2026-09-23:** In force since 4 May 2022 (BOE publication), currently enforced; this is the governing regime, not upcoming.

**Technical capabilities (Anexo II categories relevant to application code, by measure family):**
1. **`[op.exp.8] Registro de la actividad de los usuarios`** (activity logging) — requires recording user actions on the system with enough detail to support later trazabilidad (traceability) — who did what, when, on which resource. This measure's required rigor **increased significantly** in the 2022 revision per [isecauditors' 2022 changelog](https://blog.isecauditors.com/2022/05/novedades-actualizacion-esquema-nacional-seguridad-2022.html). 🟢 *horizontal-enabler* — directly the same capability as Encina's structured audit-trail/`IAuditStore`; ENS is one more consumer of a generic, well-designed audit log (actor, action, resource, timestamp, outcome), not a bespoke need.
2. **`[mp.info.*]` — protection of information measures** (e.g., data qualification/labelling, encrypted storage/transit, retention/erasure) — maps to Encina's encryption-at-rest, secrets-non-leak (`[JsonIgnore]`/`ToString()` override rule already in CLAUDE.md), and retention/crypto-shredding compliance modules. 🟢 *horizontal-enabler, already substantially covered* by existing security/compliance packages.
3. **Trazabilidad as a first-class security dimension** (alongside confidentiality, integrity, availability, authenticity) — ENS explicitly names trazabilidad as one of its six protected dimensions. This elevates "who did what when" logging from a nice-to-have to a formally audited control. 🟢 *horizontal-enabler* — reinforces that Encina's audit/logging design should treat traceability as a tier-1 cross-cutting concern (already function #12 "Audit Trail" and #3 "Structured Logging" in CLAUDE.md's 12 transversal functions), not an afterthought.
4. **Categorisation-driven control selection (Básica/Media/Alta)** — ENS requires different reinforcement levels (R1/R2/R3) of the same controls depending on system criticality. 🟡 *sector module*: a configuration profile ("ENS compliance level: Basic/Medium/High" toggling which audit fields/retention periods/crypto strength apply) would be genuinely useful as an opt-in Spain/public-sector package, layered on top of the generic audit/crypto primitives rather than duplicating them.
5. **Independent security audits / conformity declaration** (Art. 31 RD 311/2022 self-assessment or third-party audit depending on category). ⚪ *out of scope for a framework* — organizational/process obligation, not code.

---

## 6. ISO/IEC 27001:2022 — Information Security Management (Annex A controls)

**Official reference:** ISO/IEC 27001:2022, Annex A restructured into 4 themes / 93 controls (37 organizational, 8 people, 14 physical, 34 technological) — [ISO 27001 overview](https://en.wikipedia.org/wiki/ISO/IEC_27001), control breakdowns cross-checked against multiple vendor summaries (Scrut, DataGuard, HighTable); the control *numbers and titles* below are standard and stable across these sources.

**Applies to:** Any organization seeking ISMS certification; frequently referenced as a **de facto baseline** by DORA, NIS2, ENS-equivalent frameworks in other member states, and customer security questionnaires. Not a legal mandate by itself, but the most commonly demanded contractual proof of "adequate security" in B2B software procurement.

**Applicability status:** Current edition (2022) is the live standard; the 2013 edition's Annex A is superseded. Certification is voluntary and market-driven, so "status" is simply "current standard, ongoing relevance."

**Technological controls (Annex A.8) directly implementable in an application framework:**
1. **A.8.10 Information deletion** — ensuring data is deleted when no longer required, per policy/legal requirement. 🟢 *horizontal-enabler, already covered*: maps to Encina's GDPR/retention/crypto-shredding modules and soft-delete/hard-delete lifecycle in DomainModeling.
2. **A.8.11 Data masking** — obfuscating sensitive data at rest/in use/in logs. 🟢 *horizontal-enabler, already covered*: Encina's PII masking module is a direct implementation of this control; worth explicitly cross-referencing A.8.11 in that module's docs as a citable control mapping.
3. **A.8.15 Logging** — event logging covering user activities, exceptions, faults, and security events, protected against tampering. 🟢 *horizontal-enabler, already covered*: structured logging + `EventIdRanges` + audit trail satisfy this; the anti-tampering package additionally protects log integrity (control intent: logs must be tamper-evident).
4. **A.8.24 Use of cryptography** — policy-driven use of encryption for data at rest/in transit, key management. 🟢 *horizontal-enabler, already covered*: Encina's encryption/secrets modules and the CLAUDE.md rule on `[JsonIgnore]`-protected secrets are exactly this control in code form.
5. **A.8.25 Secure development life cycle** and **A.8.28 Secure coding** — secure-by-design SDLC practices, static analysis, secure coding standards. 🟢 *horizontal-enabler, already covered*: zero-warnings CA policy, ArchUnit architecture tests, mutation/coverage gates are the SDLC evidence this control expects; consider explicitly citing A.8.25/A.8.28 from `docs/architecture` to make the mapping auditable.
6. **A.8.26 Application security requirements**, **A.8.27 Secure system architecture and engineering principles** — layered validation (`Encina.Validation.*`), ROP-based explicit error handling, and provider abstraction are architecture-level answers to these controls. 🟢 *horizontal-enabler, already covered*.
7. **A.5.23 Information security for cloud services**, **A.5.19–5.22 supplier relationships** — 🟡 *sector module territory*: multi-provider health checks + a supply-chain SBOM/dependency-audit artifact (NuGet package provenance) would help customers demonstrate these controls, but the control itself is about vendor governance, partly outside code.

**Overall takeaway:** ISO 27001 Annex A's technological controls are the single **best-covered** framework in this study — Encina's existing security/compliance modules already implement most of A.8.10/8.11/8.15/8.24/8.25/8.28. The main gap is **explicit, citable documentation mapping** (a control-to-module table) rather than missing capability — recommend a `docs/compliance/iso27001-control-mapping.md` deliverable (separate from this research task).

---

## 7. PSD2 / PSD3 + PSR — Payment Services (Strong Customer Authentication)

**Official reference:** Current law: Directive (EU) 2015/2366 (PSD2) + RTS on SCA (Commission Delegated Regulation (EU) 2018/389). **Successor in progress:** PSD3 (a Directive) + the new Payment Services Regulation ("PSR", a Regulation, directly applicable) — per [Norton Rose Fulbright](https://www.nortonrosefulbright.com/en/knowledge/publications/cedd39c6/psd3-and-psr-from-provisional-agreement-to-2026-readiness) and [OneSpan](https://www.onespan.com/blog/psd3-psr-updates-2025), provisional political agreement was reached **27 November 2025**; final OJ texts expected **H1 2026**; entry into force with an **18–21 month transition** points to **effective compliance around 2027**.

**Applies to:** Payment service providers (banks, e-money institutions, payment institutions), and by extension any Encina-based application acting as a PSP, merchant PSP-integration, or account-information/payment-initiation service provider (AISP/PISP under open banking).

**Dates/status as of 2026-09-23:** PSD2 (current regime) is in force today. PSD3/PSR is **agreed but not yet published/applicable** — treat as "upcoming, ~2027," worth tracking, not yet a hard requirement.

**Technical capabilities:**
1. **Strong Customer Authentication (multi-factor: knowledge/possession/inherence) with dynamic linking** for payment initiation (PSD2 RTS 2018/389 Arts. 4–9, expected to carry over conceptually into PSR). 🟡 *sector module* — this is payments-domain authentication logic, a natural fit for an `Encina.Payments`/`Encina.Identity.SCA` package layered on generic auth abstractions, not core.
2. **Transaction-risk-analysis exemption logic** (RTS Art. 18) — fraud-rate-based SCA exemptions. 🟡 *sector module*, payments-specific.
3. **Open banking API consent and access-token lifecycle management** (AISP/PISP access, explicit consent, 90-day re-authentication) — 🟡 *sector module*.
4. **Underlying generic capabilities Encina already offers that this sector module would build on**: distributed locks (idempotent payment retries), outbox/inbox (reliable payment-event publishing/consumption), resilience providers (PSP/bank API calls), audit trail (non-repudiation of consent/authorization events). 🟢 these are already horizontal enablers; no new core work implied by PSD2/3 beyond the sector module itself.

---

## 8. ISO/IEC 27701:2019 — Privacy Information Management System (PIMS)

**Official reference:** ISO/IEC 27701:2019, an extension to ISO/IEC 27001/27002 — [ISO.org](https://www.iso.org/standard/71670.html).

**Applies to:** Any organization acting as a **PII controller and/or PII processor** operating within an ISMS; voluntary certification, widely used to demonstrate GDPR-alignment to customers/regulators (Art. 24/28 GDPR "appropriate technical and organisational measures" evidence).

**Status:** Current edition since 2019, stable, no announced revision as of 2026.

**Technical capabilities:**
1. **PIMS-specific controls for controllers** (consent capture/withdrawal, purpose limitation enforcement, data-subject-request handling, privacy-by-design records) and **for processors** (sub-processor disclosure, data-return/deletion at contract end). 🟢 *horizontal-enabler, already covered*: Encina's GDPR/Consent/DSR/LawfulBasis/PrivacyByDesign compliance modules are effectively an implementation of 27701's controller/processor annexes (Annex A/B of the standard). Recommend an explicit control-mapping doc, same pattern as ISO 27001 above, rather than new code.
2. **PII inventory / processing-activity record** (mirrors GDPR Art. 30 ROPA). 🟢 already implied by existing consent/lawful-basis stores; worth confirming a queryable "processing activity register" export exists.

---

## 9. ISO/IEC 42001:2023 — AI Management System (AIMS)

**Official reference:** ISO/IEC 42001:2023 — [ISO.org](https://www.iso.org/standard/42001), Annex A: 38 controls across 9 areas (A.2 policy, A.3 internal organization, A.4 resources, A.5 impact assessment, A.6 AI system lifecycle, A.7 data, A.8 information for interested parties, A.9 use of AI systems, A.10 third-party/customer relationships — grouping per [Snowflake summary](https://www.snowflake.com/en/artificial-intelligence/ai-governance/iso-42001/) and [A-LIGN](https://www.a-lign.com/articles/understanding-iso-42001); verify exact area count against the purchased standard text, as free summaries vary slightly).

**Applies to:** Organizations that develop, provide, or use AI systems; increasingly requested as complementary evidence for **EU AI Act** (Regulation (EU) 2024/1689) conformity, though not legally equivalent to it.

**Status:** Published 2023, current; relevant now given the EU AI Act's phased application (prohibited practices since Feb 2025, GPAI obligations since Aug 2025, high-risk-system obligations from Aug 2026/2027).

**Relevance to Encina:** Encina itself is not an AI system, but if it (or an app built on it) embeds AI features (e.g., an LLM-based validation/classification helper, anomaly detection in audit logs), AIMS controls become relevant to that specific feature.
1. **AI impact assessment records** (Annex A.5) and **AI system lifecycle documentation** (data provenance, model versioning, risk controls) — 🟡 *sector module, and only if/when Encina ships an AI-adjacent feature* — not relevant to the current messaging/data-access/security core; flag as "watch, don't build yet."
2. **Logging of AI system decisions for traceability** — 🟢 *would be a horizontal-enabler reuse* of the same audit-trail primitive discussed under DORA/EHDS/ENS above, if/when an AI feature is added; no new capability needed beyond making the existing audit trail applicable to AI-driven decisions too.

**Classification for Encina today: ⚪ out of scope** (no AI-system component exists in-framework yet) with a one-line reason: *Encina does not currently ship an AI/ML feature, so AIMS controls have nothing to attach to; revisit if/when one is added.*

---

## 10. IEC 62304 — Medical Device Software Lifecycle

Covered jointly with MDR in section 4 above (same standard, same conclusion: Encina is a potential SOUP dependency, not a SaMD product — discipline around versioning/changelog/testing evidence is what matters, not new code).

---

## 11. LOPDGDD — Ley Orgánica 3/2018 (Spain)

**Official reference:** Ley Orgánica 3/2018, de 5 de diciembre, de Protección de Datos Personales y garantía de los derechos digitales — [BOE-A-2018-16673](https://www.boe.es/buscar/act.php?id=BOE-A-2018-16673). In force since 7 December 2018; adapts/complements GDPR into Spanish law and adds Título X (digital rights: right to digital disconnection, digital education, etc., which are labour/consumer-law rights, not application-technical requirements).

**Applies to:** Any entity (public or private) processing personal data of individuals in Spain — same broad scope as GDPR, with Spain-specific procedural add-ons (e.g., AEPD sanctioning procedure specifics, data-protection-officer rules, video-surveillance/whistleblowing-channel rules).

**Dates/status as of 2026-09-23:** In force since 2018; a reported amendment/adaptation deadline of **28 December 2026** appeared in secondary sources for a specific subset of obligations — this needs verification against the BOE consolidated text and AEPD guidance before being treated as authoritative; flagged as uncertain.

**Technical capabilities:**
1. **Everything already required by GDPR** (lawful basis, consent, DSR, breach notification, DPIA, privacy by design) — LOPDGDD does not add materially different *technical* obligations beyond GDPR; it is mostly procedural/organizational (AEPD-specific forms, DPO registration, sanctioning regime) and a few Spain-specific rights (digital disconnection at work, is an employer/HR policy matter, not app code). 🟢 *horizontal-enabler, already covered* via Encina's existing GDPR compliance modules — no LOPDGDD-specific new capability identified.
2. **Whistleblowing/internal reporting channel technical requirements** (Título X, Art. 24 LOPDGDD, now overlaid by Ley 2/2023 transposing the EU Whistleblowing Directive) — secure, confidential internal-reporting channel with restricted access and retention limits. 🟡 *sector module, if Encina wanted to offer a compliance "whistleblowing channel" building block* (access control + retention + confidentiality), but this is a narrow, optional feature, not a data-access/messaging core concern.

**Classification: ⚪ out of scope as a distinct framework** with reason: *LOPDGDD's technical substance is GDPR's; Encina's GDPR modules already satisfy it — no incremental Spain-specific technical capability was found beyond the (optional, niche) whistleblowing-channel case.*

---

## 12. CER Directive — Critical Entities Resilience (brief, for completeness)

**Official reference:** Directive (EU) 2022/2557 of 14 December 2022. **Status:** Directive (needs national transposition, unlike DORA/EHDS which are Regulations). Per [critical-entities-resilience-directive.com — Spain page](https://www.critical-entities-resilience-directive.com/Transposition/Spain.html), **Spain has NOT completed transposition as of March 2026** — an Anteproyecto de Ley de Protección y Resiliencia de Entidades Críticas was under public consultation in 2025; the European Commission has sent Spain (and six other states) formal notices and reasoned opinions and is referring them to the CJEU for the delay.

**Applies to:** Operators of critical infrastructure in energy, transport, banking, financial-market infrastructure, health, drinking water, wastewater, digital infrastructure, public administration, space, and food sectors — designated individually by Member States, not a self-assessed general obligation.

**Classification: ⚪ out of scope for Encina as a framework**, one-line reason: *CER imposes physical/organizational resilience and entity-designation obligations decided by Member States on named critical operators; it has no distinct application-code control that isn't already covered by DORA-style ICT-risk/incident/audit primitives Encina already exposes. Not yet transposed in Spain, so no concrete national technical rule exists to build against today.*

---

## 13. MiCA — Markets in Crypto-Assets Regulation (relevance check only)

Not independently searched in depth because it is judged **not relevant** to Encina's positioning: MiCA (Regulation (EU) 2023/1114) governs crypto-asset issuance and crypto-asset-service-provider licensing/conduct (white papers, market-abuse rules, reserve-asset requirements for stablecoins). Encina has no crypto-asset-issuance or CASP-specific domain model.

**Classification: ⚪ out of scope**, reason: *MiCA's obligations are financial-instrument/licensing/market-conduct rules for crypto-asset issuers and exchanges — not a technical application capability a generic business-application framework would implement; a CASP built on Encina would need MiCA compliance at the business-process layer, but DORA (already covered above) is the ICT-technical overlay that actually touches code for such an entity.*

---

## 14. NIS2 — cross-reference (already partly addressed in Encina)

**Official reference:** Directive (EU) 2022/2555 (NIS2), national transposition required by Member States (Spain: transposition ongoing/delayed, similar timeline pressure as CER, since both directives share a family of "critical sector" obligations). Not independently re-researched here because **Encina already has a `Compliance Extensions` module explicitly named NIS2** (EventId range 9200–9499, per `src/Encina/Diagnostics/EventIdRanges.cs` and CLAUDE.md's Specialized Provider Categories table). Flagging for completeness since the question asked for "any other you judge relevant."

**Recommendation:** Cross-check the existing `Encina.NIS2`-equivalent module's technical capabilities (incident notification, risk-management measures, supply-chain security per NIS2 Art. 21) against the DORA/ENS logging-and-incident-classification capabilities identified above — there is likely reusable overlap (the same "structured incident classification + staged reporting" pattern recommended for DORA in section 1.2 applies to NIS2 Art. 23 incident notification too) rather than a separate implementation. **Classification: 🟢 horizontal-enabler, already in progress** — no new research finding beyond "make sure the DORA/NIS2/ENS incident-reporting pattern is unified into one generic capability rather than three siloed ones."

---

## Summary classification table

| Framework | Ref. | Binding today (2026-09-23)? | Classification | One-line rationale |
|---|---|---|---|---|
| DORA | Reg. 2022/2554 | Yes (since 17 Jan 2025) | 🟡 sector module (finance reporting/register) on top of 🟢 horizontal audit/crypto/resilience | Incident classification+reporting and ICT third-party register are finance-specific; underlying logging/crypto/resilience are already generic Encina capabilities |
| eIDAS 2 | Reg. 2024/1183 | Partially (wallet mandatory ~Dec 2026/2027) | 🟡 sector module (wallet/QES integration) | Protocol-specific identity/signature integration, narrow to regulated onboarding sectors |
| EHDS | Reg. 2025/327 | No (staged, from 2027/2029) | 🟢 horizontal-enabler (logging component) / 🟡 sector module (EHR format) | The 5-element access-logging model generalizes well; EHR interoperability format does not |
| MDR + IEC 62304 | Reg. 2017/745 / IEC 62304:2006+A1:2015 | Yes (since 2021) | ⚪ out of scope (Encina is a SOUP dependency, not SaMD) | Discipline (versioning, changelog, testing evidence) already required by CLAUDE.md; no new module |
| ENS | RD 311/2022 | Yes (since 2022) | 🟢 horizontal-enabler (logging/trazabilidad/crypto) / 🟡 sector module (level-based profile) | Traceability and info-protection measures map to existing audit/security modules; a Basic/Medium/High profile toggle would be a useful Spain/public-sector package |
| ISO 27001:2022 | ISO/IEC 27001:2022 | Yes (current standard) | 🟢 horizontal-enabler, largely already implemented | A.8.10/8.11/8.15/8.24/8.25/8.28 map almost 1:1 to existing Encina security/compliance modules; needs a citable mapping doc, not new code |
| PSD2 / PSD3+PSR | Dir. 2015/2366 / PSR (pending, ~2027) | PSD2 yes; PSD3/PSR no | 🟡 sector module (SCA, exemptions, open-banking consent) | Payments-specific auth/consent logic; built on already-generic locks/outbox/resilience/audit |
| ISO/IEC 27701:2019 | ISO/IEC 27701:2019 | Yes (current standard) | 🟢 horizontal-enabler, already implemented | Controller/processor PIMS controls mirror existing GDPR/Consent/DSR modules |
| ISO/IEC 42001:2023 | ISO/IEC 42001:2023 | Yes (current standard) | ⚪ out of scope today | No AI/ML feature exists in Encina yet to attach AIMS controls to |
| LOPDGDD | LO 3/2018 (Spain) | Yes (since 2018) | ⚪ out of scope as distinct framework | Technical substance is GDPR's, already covered; only niche add is an optional whistleblowing-channel package |
| CER Directive | Dir. 2022/2557 | No (Spain not transposed as of Mar 2026) | ⚪ out of scope | Physical/organizational critical-entity designation, not an app-code control; not yet transposed nationally |
| MiCA | Reg. 2023/1114 | Yes, but not applicable to Encina's domain | ⚪ out of scope | Crypto-asset issuance/licensing rules, not a generic app capability; DORA is the relevant ICT-technical overlay instead |
| NIS2 | Dir. 2022/2555 | Transposition-dependent | 🟢 horizontal-enabler, already in progress in Encina | Already has a dedicated Compliance Extensions module; recommend unifying its incident-reporting pattern with DORA/ENS rather than three separate ones |

## Key uncertainties flagged for follow-up verification against primary sources before quoting externally
- Exact OJ numbers/dates for DORA's incident-report-timeline RTS (reported as 2025/301) and ITS (reported as 2025/302) — confirm on EUR-Lex.
- EHDS's reported "January 2026 EHR certification" milestone — not found directly in the regulation's own staged-application article; likely a secondary-source simplification of the 26 March 2027 milestone.
- ENS Anexo II exact measure count (73 vs. 75 across different secondary summaries) — confirm against the BOE consolidated text.
- eIDAS 2's exact private-relying-party mandatory-acceptance date (reported as wallet availability 24 Dec 2026 + 12 months) — confirm against the amended eIDAS consolidated text/implementing acts once published.
- LOPDGDD "28 December 2026" adaptation deadline mentioned by one aggregator — could not cross-verify against BOE/AEPD directly in this pass.
