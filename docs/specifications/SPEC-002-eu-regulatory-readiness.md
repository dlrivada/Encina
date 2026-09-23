# SPEC-002 — EU Regulatory Readiness

| | |
|---|---|
| **Status** | APPROVED — pending SPEC-000 amendment (AC-040). DECIDED by the maintainer: DEC-005 and DEC-011 (2026-09-23); DEC-001, DEC-002, DEC-003, DEC-006, DEC-007, DEC-008, DEC-009, DEC-010, DEC-012, DEC-013, DEC-014, DEC-015 and DEC-016 (2026-09-24). DEC-004 SUPERSEDED. Every decision is recorded; the specification becomes APPROVED when the SPEC-000 amendment pull request of §11.2 merges (AC-040) |
| **Author** | Specifier (Claude), from the regulatory research [notes](research/SPEC-002/README.md) of 2026-09-23 and a private analysis of the reference application |
| **Date** | 2026-09-23; revised 2026-09-24 with the maintainer's decisions |
| **Refines** | [SPEC-000](SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-024 (per-package article coverage), within SPEC-000 DEC-002, SPEC-000 REQ-025 and SPEC-000 REQ-026; **widens** the SPEC-000 1.0 scope through DEC-009, DEC-011 and §5.11 (decided 2026-09-23 and 2026-09-24), subject to the SPEC-000 amendment of §11.2 |
| **Evidence** | Code read on `main` at `ce337e0c` (2026-09-23); issues #1142–#1161 opened from the same study and its follow-ups; research notes in [research/SPEC-002/](research/SPEC-002/README.md); primary sources in §17 |
| **Supersedes** | — |

> This specification implements nothing, and it is **not legal advice**. It states what must be true of Encina so that an application built on it *can* comply with EU and Spanish law wherever that law touches what a framework does. Encina never takes over the obligations of the application, its controller or its producer. Requirements state *what*, not *how*; design choices go through the ADR process ([AI-DEVELOPMENT-MODEL.md](../engineering/AI-DEVELOPMENT-MODEL.md) §7). Legal facts carry the verification flag of the research note they come from (§3.1 legend); where the notes disagreed, the fact-check (note [E](research/SPEC-002/E-factcheck.md)) and the opened issues prevail.

**Research notes and source notation.** The research notes of 2026-09-23 are kept, non-normative, in [research/SPEC-002/](research/SPEC-002/README.md). A *Source:* entry names a note by its letter and an item inside it: "F C09" is item C09 of note [F](research/SPEC-002/F-health-practice.md), and "H-R31" is row H-R31 of note [H](research/SPEC-002/H-integrations-law.md). The reference application (§4.1) is private: where a requirement rests on the analysis of its code or design, the entry says **"reference-application analysis (private)"**, and the requirement's rationale carries the Encina evidence that anyone can check.

| Note | Subject |
|---|---|
| [A](research/SPEC-002/A-encina-itself.md) | Encina itself: CRA, PLD, export control |
| [B](research/SPEC-002/B-horizontal.md) | EU horizontal acts |
| [C](research/SPEC-002/C-sector-national.md) | Sector acts and Spanish national law |
| [D](research/SPEC-002/D-encina-inventory.md) | Encina inventory: packages, issues, milestones |
| [E](research/SPEC-002/E-factcheck.md) | Fact-check of B and C against primary sources (prevails over B and C) |
| [F](research/SPEC-002/F-health-practice.md) | Legal profile of a Spanish psychology practice |
| [H](research/SPEC-002/H-integrations-law.md) | Integrations law: invoicing and Verifactu, payments, patient messaging, calendar and mail vendors |

**Identifiers.** Unprefixed REQ, DEC, AC, INV and S identifiers are this specification's. Identifiers of SPEC-000 are always written with the prefix (SPEC-000 DEC-002, SPEC-000 REQ-024).

---

## 1. Problem

SPEC-000 made EU regulatory compliance part of the 1.0 contract (SPEC-000 DEC-002) and requires every compliance package to state which articles it covers (REQ-024). It does not say which laws matter, which of them bind Encina itself and which bind the applications built on it, or what "ready" means for a concrete application. A study made on 2026-09-23 against the reference application (a small Spanish psychology practice, §4), found that:

- the legal ground is moving: the AI Act Omnibus was adopted in July 2026, the data and cyber Omnibus is still a proposal, Spain has not transposed NIS2 and was referred to the CJEU, and Verifactu applies to the practice from 2027;
- several shipped compliance behaviours would work **against** the law in that application: the retention sweep erases data under legal hold when the hold lookup fails (#1143), erases the same data every cycle (#1142) and erases every data category of an entity instead of the expired one (#1160); releasing a legal hold reports success when it fails (#1161); crypto-shredding one field destroys legally retained clinical data (#1144); DSR erasure never consults retention or holds; there is no blocking state for LOPDGDD art. 32; the whole United States is treated as adequate (#1145);
- cross-cutting defects remove the "who" from every compliance decision (#1147, #1148, #1149) and lose outbox messages without trace (#1150, #1151, #1152, #1153, #1154);
- none of the 15 per-package specifications required by SPEC-000 REQ-024 exists, and several package READMEs describe types and provider coverage that do not exist.

## 2. Scope and responsibility boundary

### 2.1 Principle

Maintainer intent (2026-09-23): EU law compliance is mandatory and pre-1.0; the application is responsible for applying the law; Encina must be ready so that the application **can** apply it, as far as it concerns Encina. **Encina facilitates, never blocks.** In this specification:

1. The application, through its controller or producer, applies the law and takes every legal decision (which retention period, which Art. 9(2) condition, which processors, what the privacy notice says).
2. Encina provides the mechanisms, defaults and extension points that such decisions need, and documents honestly what each package covers.
3. Encina **blocks** an application when any of these holds: a shipped default or behaviour makes a legally required outcome impossible or silently undoes it (a *conflict*); the application has to bypass an Encina module to comply; or a documentation claim cannot be relied on by a compliance officer.
4. Encina itself stays outside regulated roles (CRA or PLD manufacturer, Verifactu producer), as the maintainer decided (DEC-002, DEC-010).

### 2.2 In scope

- The EU acts and Spanish national law in §3, as far as they create technical requirements that a .NET application framework can enable. SPEC-000 §2 names only EU law (GDPR and the Digital Omnibus, ePrivacy, NIS2, AI Act); the Spanish provisions that this specification turns into 1.0 requirements (LOPDGDD arts. 7 and 32, Ley 41/2002 arts. 17 and 18.3, LSSI arts. 20–22) enter the 1.0 contract only through DEC-011 and the SPEC-000 amendment of §11.2.
- Encina's own position under the CRA, the PLD and export control, and the good-practice duties that let integrators meet their own CRA obligations.
- The reference application as the concrete acceptance case, through the PracticeManagement reference scenario (§4.3).
- The integration and platform capabilities that such an application needs from a framework (§5.11), which the maintainer placed in the 1.0 contract on 2026-09-23.
- Multi-tenancy of every capability this specification adds or changes (DEC-009, REQ-061), and the OpenTelemetry and structured-logging instrumentation each one carries (DEC-010, REQ-062).

### 2.3 Non-goals

- Legal advice. The law map is research with its uncertainty flags.
- Taking over application or organisational obligations: privacy notices, DPA contracts, DPIA conclusions, breach decisions and notifications, the Verifactu *declaración responsable*, CE marking under the EHDS or the MDR, DPO appointment, staff training and AI literacy, ISMS certification, backups operations, physical security.
- New regulation packages beyond SPEC-000: DORA, eIDAS 2, Data Act, ENS and EHDS (#804–#808) stay post-1.0 (SPEC-000 §6; DEC-015). What the reference application needs from them is in 1.0 as generic extension points: EHDS logging and export (REQ-052) and trusted time-stamping and e-signature integration points (REQ-048).
- Sector logic inside Encina: Verifactu and payments (DEC-002, DEC-003), FHIR/EEHRxF, QR and PDF generation, money and VAT models.
- Implementing draft law as default behaviour (§3.5, REQ-024).
- End-user UI accessibility (EAA): Encina ships no end-user interface.

### 2.4 Responsibility split

| Layer | Owns | Examples |
|---|---|---|
| Encina | Mechanisms, safe defaults, extension points, honest documentation | Retention floor, fail-closed legal hold, blocked state, evidential read audit, consent per channel, article-coverage specifications |
| Application | Legal decisions and their configuration | Retention periods per document type, Art. 9(2) condition, processors used, privacy-notice text, Verifactu module |
| Organisation | Measures that are not software | DPO, DPIA sign-off, contracts, authority notifications, training |

## 3. Law map

Verified on **2026-09-23**. Re-verification follows §3.6.

### 3.1 Legend

- **Encina itself**: **No** = does not bind the library or its maintainer; **G** = does not bind, but good practice lets integrators meet their own duties.
- **Apps**: **Yes** = binds applications built on Encina that fall within the act's scope; **If …** = only under the stated condition.
- **Verif.**: **[V]** checked against a primary source (EUR-Lex, BOE, OEIL, Congreso, Commission, AEPD, AEAT, vendor terms); **[S]** reputable secondary source only; **[K]** background knowledge, not re-verified; **[I]** inference; **[U]** could not be verified.

### 3.2 EU horizontal acts

| Act | Status and key dates | Encina itself | Apps | What a framework can enable | Verif. |
|---|---|---|---|---|---|
| **GDPR**, Reg. (EU) 2016/679 | Applicable since 25 May 2018; unchanged (amendments only proposed, §3.5). EU–US DPF adequacy decision (EU) 2023/1795 upheld by the General Court (T-553/23 *Latombe*, 3 Sep 2025); appeal C-703/25 P pending | No (Encina processes no personal data and sends no telemetry to its maintainer; its OpenTelemetry instrumentation exports only to destinations the application configures) | Yes | Art. 6 and Art. 9(2) records, consent records, DSR orchestration, retention and erasure with exemptions, restriction, breach clock, DPIA tooling, RoPA export, transfer checks, encryption and pseudonymisation, access audit | [V] dates; [S] DPF litigation |
| **ePrivacy**, Dir. 2002/58/EC (Spain: LSSI, Ley 34/2002, arts. 20–22) | In force; the ePrivacy Regulation is dormant; Art. 5(3) would move into the GDPR under the Omnibus proposal (§3.5) | No | Yes: terminal storage (cookies, SDKs) and commercial e-mail, SMS or WhatsApp | Consent per purpose and channel, proof of consent, withdrawal as easy as grant, enumerable strictly-necessary exemptions, LSSI art. 21.2 existing-client basis with opt-out | [V] LSSI; [V] AEPD cookie guide (May 2024) |
| **NIS2**, Dir. (EU) 2022/2555 | Transposition due 17 Oct 2024. CIR (EU) 2024/2690 applies since 7 Nov 2024, only to the listed digital-infrastructure providers. **Spain**: *anteproyecto* approved at first reading on 14 Jan 2025 and not sent to the Cortes; formal notice 28 Nov 2024, reasoned opinion 7 May 2025, CJEU referral 8–9 Jul 2026 (IP/26/1499). RDL 12/2018 and RD 43/2021 (NIS1) remain the Spanish baseline | No | If medium or large and in an Annex I/II sector (a micro practice is out by size, Art. 2(1) [K]) | Art. 21(2)(a)–(j) building blocks; 24 h / 72 h / 1-month incident clock (Art. 23); supply-chain metadata (EPIC #880) | [V] status and dates; [K] size cap |
| **AI Act**, Reg. (EU) 2024/1689, as amended by **Reg. (EU) 2026/1744** (Digital Omnibus on AI, of 8 Jul 2026; OJ 24 Jul 2026; in force 27 Jul 2026) | Art. 5 prohibitions and Art. 4 from 2 Feb 2025. Art. 4 **replaced**: duty to "take measures to support" AI literacy, no guaranteed level. **New** prohibitions Art. 5(1)(ba) and (bb) from 2 Dec 2026. GPAI (Chapter V) from 2 Aug 2025; Commission enforcement from 2 Aug 2026; models placed earlier comply by 2 Aug 2027. Art. 50 from 2 Aug 2026, with the Art. 50(2) marking duty deferred to 2 Dec 2026 for generative systems already on the market (new Art. 111(4)). Annex III high-risk from **2 Dec 2027**; Annex I high-risk from **2 Aug 2028**; high-risk systems of public authorities by 2 Aug 2030. Spain: *Proyecto de Ley Orgánica* on AI (121/000096, 28 May 2026) in Congress | No (`Encina.Compliance.AIAct` is not an AI system) | If the app provides or deploys AI features | AI system registry, a complete Art. 5 practice catalogue (REQ-060), Art. 12 logging, Art. 14 oversight hooks, Art. 50 disclosure metadata (EPIC #881) | [V] |
| **Data Act**, Reg. (EU) 2023/2854 | Applicable since 12 Sep 2025. Art. 3(1) design duty for products placed on the market after 12 Sep 2026. Switching charges abolished from 12 Jan 2027 (Art. 29). Chapter IV applies from 12 Sep 2027 to some contracts concluded on or before 12 Sep 2025. 30-day switching transition (Art. 25(2)(a)); functional equivalence for IaaS (Art. 30(1)) | No | If a data holder of connected products or a data-processing-service provider | Structured, machine-readable export shared with GDPR Art. 20; export for switching (post-1.0, #806, DEC-015) | [V] |
| **Data Governance Act**, Reg. (EU) 2022/868 | Applicable since 24 Sep 2023. **Repeal proposed** by COM(2025) 837, content folded into the Data Act | No | If a data-intermediation or data-altruism service | Reuse of consent records; nothing generic | [V] |
| **European Accessibility Act**, Dir. (EU) 2019/882 (Spain: Ley 11/2023) | Applies since 28 Jun 2025 | No | If the app provides a listed consumer service (e-commerce, banking, …) | Nothing in core: Encina ships no end-user UI | [V] scope; [I] framework relevance |
| **Digital Services Act**, Reg. (EU) 2022/2065 | Applicable since 17 Feb 2024; duties scale with role (hosting, online platform, VLOP) and size | No | If the app hosts user content | Nothing for 1.0; an opt-in notice-and-action module could be built later | [V] dates; [K] size thresholds |
| **Cyber Resilience Act**, Reg. (EU) 2024/2847 | In force 10 Dec 2024. Chapter IV from 11 Jun 2026. **Art. 14 reporting from 11 Sep 2026**, also for products placed on the market before 11 Dec 2027. Everything else from **11 Dec 2027**. Commission guidance C(2026) 5252 of 27 Jul 2026 (non-binding). SBOM-format implementing act (Art. 13(24)) and FOSS attestation delegated act (Art. 25) not adopted | **G.** Not placed on the market while Encina is non-monetised FOSS published by a natural person (recitals 15 and 18; guidance points 23, 53, 67, 87; Example 34 matches Encina). The steward regime (Art. 24) needs a legal person | If the app is a product with digital elements placed on the EU market (a purely server-side SaaS is out unless it is the remote data processing of such a product): Art. 13(5) due diligence, Art. 13(6) upstream reporting, Annex I Part II SBOM, Art. 13(8) support period, Art. 14 reporting | Per-package SBOM, support period in SECURITY.md, GHSA and CVE advisories, provenance, secure defaults, legal-status statement (REQ-026–REQ-030) | [V] text and guidance; [S] steward reporting date |
| **Product Liability Directive**, Dir. (EU) 2024/2853 | Applies to products placed on the market after 9 Dec 2026; transposition due 9 Dec 2026. No Spanish transposing act found (TRLGDCU arts. 128–149 still govern) | No (Art. 2(2): FOSS outside a commercial activity) | Yes, SaaS included; no later-defect defence for missing security updates (Art. 11(2)) | Keep Encina non-commercial (DEC-010); ship security fixes promptly (REQ-029) | [V] text; [S]/[U] Spanish status |
| **Dual-Use Regulation**, Reg. (EU) 2021/821 (consolidated 15 Nov 2025) | General Software Note: public-domain software is not controlled | No (MIT on public GitHub/NuGet is "in the public domain") | If the app sells cryptographic products (own classification) | Document the .NET BCL primitives Encina calls (REQ-028) | [V] EU; [U] US EAR |
| **Cybersecurity Act**, Reg. (EU) 2019/881, EUCC | Voluntary certification; EUCC presumption of conformity under the CRA expected Q4 2026 | No | Voluntary | — | [V] |

### 3.3 Sector acts and Spanish national law

| Act | Status and key dates | Encina itself | Apps | What a framework can enable | Verif. |
|---|---|---|---|---|---|
| **LOPDGDD**, LO 3/2018 | Consolidated to 27 Dec 2025. Ley 10/2025 (DF 4) rewrote art. 23.1 (advertising-exclusion systems, "servicios de preferencia"), in force since 28 Dec 2025. There is **no** data-protection deadline on 28 Dec 2026 (that date is Ley 10/2025's customer-service transition) | No | Yes | **Blocking** (*bloqueo*, art. 32); data consent of minors from 14 (art. 7); deceased persons (art. 3); legal bases for health data (art. 9.2, DA 17ª); DPO for health centres except individual professionals (art. 34.1.l) | [V] |
| **Ley 41/2002** (patient autonomy) | Consolidated to 1 Mar 2023. A psychology practice is an authorised health centre (RD 1277/2003) [S] | No | If the app keeps clinical records | Retention **at least 5 years from the discharge of each care episode** (art. 17.1; longer in some regions); authenticity of the record and of its changes (art. 14); access limits for third parties' data and professionals' "anotaciones subjetivas" (art. 18.3); identification separated from clinical data for secondary access (art. 16.3) | [V] national; [S] regional periods; [S] health-centre status |
| **Código Deontológico del Psicólogo** (arts. 39–49) | In force; a new code is in draft (CNMC report 26 Aug 2026, AI provisions reported) | No | Organisational and app duties of the psychologist | Access restricted to the professional (art. 46), de-identified case material (arts. 43, 45), audit | [V] code; [S] draft |
| **ENS**, RD 311/2022 | BOE 4 May 2022, in force 5 May 2022. Anexo II has **73** measures; **five** dimensions (confidentiality, integrity, traceability, authenticity, availability). Applies to private suppliers of the public sector (art. 2(3)) | No | If the app serves the public sector | Activity logging (op.exp.8, dimension T), protection of information (mp.info.*), level-based profile (post-1.0, #807, DEC-015) | [V] |
| **Verifactu**: RD 1007/2023; Orden HAC/1177/2024; RDL 15/2025 | Corporate-tax taxpayers from **1 Jan 2027**; everyone else, IRPF professionals included, from **1 Jul 2027**; producers since 29 Jul 2025 [S]. VAT-exempt health services are covered once software issues the invoices (AEAT FAQ, 22 Jul 2026). Producer sanction: €150,000 per year and system type (LGT art. 201 bis) | No, as long as Encina ships no Verifactu-relevant component (DEC-002). A library implementing chaining, QR or submission could make its publisher a component "fabricante" (AEAT developer FAQ v1.3 §5) | Yes, any app that issues invoices | Regulation-neutral primitives only: reliable outbox (REQ-017), ordered partitions, gapless sequence, persistent chained log (REQ-042, REQ-045, REQ-047) | [V] dates and FAQ; [S] producer date; [S] sanction wording |
| **B2B e-invoicing**: Ley 18/2022 art. 12; RD 238/2026 | Applies 12 or 24 months after a ministerial order still in draft (public hearing Apr–May 2026); B2B only, not to patients | No | If the app invoices businesses | None | [V] RD; [S] order status |
| **DORA**, Reg. (EU) 2022/2554 | Applies since 17 Jan 2025. Level 2: RTS 2024/1772 (classification: Arts. 1–7 criteria, Art. 8 major incident, Art. 9 thresholds); RTS 2024/1774 (ICT risk framework); RTS 2025/301 (initial report within 4 h of classification and 24 h of awareness; intermediate within 72 h of the initial one; final within 1 month of the latest intermediate); ITS 2025/302 (templates); ITS 2024/2956 (register of information); RTS 2025/1190 (TLPT) | No | If a financial entity or its ICT provider | Generic audit, crypto and resilience primitives; DORA module post-1.0 (#804, DEC-015) | [V] |
| **eIDAS 2**, Reg. (EU) 2024/1183 | In force 20 May 2024. Wallets by 24 Dec 2026. Private relying parties required by law or contract to use strong authentication must accept the wallet on the user's voluntary request by **24 Dec 2027** (Art. 5f(2)); micro and small enterprises exempt; VLOPs under Art. 5f(3) | No | If a relying party in a listed sector and not micro or small | Trusted time-stamping and e-signature integration points (REQ-048); wallet module post-1.0 (#805, DEC-015) | [V] |
| **EHDS**, Reg. (EU) 2025/327 | OJ 5 Mar 2025; in force **25 Mar 2025**. General application 26 Mar 2027. Priority categories (a)–(c) and the EHR systems processing them from **26 Mar 2029**; (d)–(f) from 26 Mar 2031; secondary use from 26 Mar 2029 (parts 2027, 2031, 2035). Micro-enterprise providers exempt from secondary-use data-holder duties (Art. 50). Access information available at least 3 years (Art. 9) [S] | No | If a healthcare provider or EHR-system manufacturer (an in-house EHR counts as "put into service" [S]) | Evidential access log (who, which subject, which data, when, origin) and export as extension points (REQ-007, REQ-052; #808) | [V] dates via EUR-Lex and EC FAQ; [S] Art. 9 period; [S] in-house clause |
| **MDR**, Reg. (EU) 2017/745, with IEC 62304 | Fully applicable since 26 May 2021; targeted revision proposed (COM/2025/1023) | No (Encina may be SOUP) | If the app has a medical purpose (Rule 11: e.g. automated risk scoring) | SOUP evidence: versioned releases, changelog, known anomalies, test evidence | [S]/[K] |
| **PSD2**, Dir. 2015/2366 and RTS 2018/389; **PSD3/PSR** | PSD2 in force. PSR not published (OEIL: awaiting the Council first-reading position; EP plenary forecast 14 Dec 2026); application mid-2028 at the earliest | No | Payment providers; merchants only by contract (use the gateway's hosted SCA flow) | Nothing specific (DEC-003); outbox, inbox, sagas, locks, webhook ingestion | [V] status |
| **CER**, Dir. (EU) 2022/2557 | Spain not transposed; *Proyecto de Ley* 121/000088 in Congress since 18 Mar 2026; CJEU referral (IP/26/910) | No | If designated as a critical entity | Nothing beyond the NIS2 and DORA primitives | [V] |

### 3.4 Voluntary standards

| Standard | Status | Relevance | Verif. |
|---|---|---|---|
| ISO/IEC 27001:2022 | Current; Annex A has 93 controls | A.8.10, 8.11, 8.15, 8.24, 8.25 and 8.28 map to existing Encina modules; a control-mapping document would make that citable (REQ-057) | [S] |
| ISO/IEC 27701:2025 | Edition 2, now a standalone PIMS standard; the 2019 edition is superseded | Controller and processor controls map to the GDPR modules (REQ-057) | [V] |
| ISO/IEC 42001:2023 | Current AI management-system standard | Only for applications with AI features | [U] control count |
| PCI DSS v4.0.1 | Current | Apps using a hosted checkout stay in SAQ A and never receive card numbers; nothing for Encina | [S] |

### 3.5 Pending: the data and cyber Digital Omnibus, COM(2025) 837

Procedure 2025/0360(COD) is at **"awaiting committee decision"** (joint ITRE/LIBE; draft report 22 Jun 2026; amendments 27 Jul 2026). There is no Council mandate (an Irish Presidency compromise of 3 Sep 2026 is reported [S]) and no trilogue. Adoption is not expected before late 2026, and application later still. **Current law stays the default** in Encina until the text is adopted (REQ-024, DEC-012); the adaptations below are built before 1.0 behind options that are off by default.

| Proposed change | Draft provision | Encina impact if adopted | Default until then |
|---|---|---|---|
| Breach notification within 96 h, only for "high risk", through an ENISA single entry point | GDPR Art. 33(1) | Configurable deadline and threshold (#813), ENISA adapter (#812) | 72 h, "risk" |
| Pseudonymised data not personal for an entity that cannot identify the subject; implementing acts on criteria | GDPR Art. 4(1), Art. 41a | Anonymization documentation | Current definition |
| Single-click reject; six-month re-ask moratorium; first-party audience-measurement exemption | GDPR Art. 88a(3)(c), 88a(4)(a) and (c); applies 6 months after entry into force | Consent cooldown (#810), exempt purposes (#811) | Not enabled |
| Machine-readable consent signals (controllers after 24 months; non-SME browsers after 48) | GDPR Art. 88b | Pluggable consent-signal source | Not enabled |
| AI development under legitimate interest | GDPR Art. 88c | #815 | Not enabled |
| Refusal of access requests used for purposes other than data protection, with standard reasons [K] | GDPR Art. 12(5) [K] | #814; the reason codes of REQ-004 (P-02) stay current-law codes | Current Art. 12(5): "manifestly unfounded or excessive" |
| EU-wide lists of processing that does or does not need a DPIA, prepared by the EDPB [K] | GDPR Art. 35(4)–(6) [K] | #816 (loader for the harmonised list) | National lists (AEPD Art. 35.4 and 35.5) |
| NIS2 reporting through an ENISA single entry point; a CRA Art. 14(3) report counts as NIS2 reporting; **timelines unchanged** | NIS2 Art. 23a, Art. 23(12) | #812 | 24 h / 72 h / 1 month |
| DGA repealed and folded into the Data Act; Data Act trade-secret refusal | Data Act Art. 4(8), 5(11) | #806 (post-1.0) | Current law |
| DORA, eIDAS, CER and EUDPR reporting through the single entry point | Several | None for 1.0 | Current law |

### 3.6 Review cadence

- §3 carries its verification date. It is re-verified before each minor tag of the 1.0 sequence (SPEC-000 DEC-005) and before `1.0.0-rc.1`, through a pull request (REQ-025).
- It is also re-verified within 30 days of any of these events: COM(2025) 837 published in the OJ; the Spanish NIS2 law published in the BOE; judgment in C-703/25 P; AI Act Commission guidelines (due by 1 Aug 2027 and 2 Sep 2027); EHDS implementing acts (due by 26 Mar 2027); the new Código Deontológico; an AEAT answer on component producers, if P-24 seeks one (DEC-002).

Watch list:

| Date | Event |
|---|---|
| 2 Dec 2026 | AI Act new prohibitions Art. 5(1)(ba)/(bb); end of the Art. 50(2) grace period |
| 9 Dec 2026 | PLD applies to products placed on the market after this date |
| 24 Dec 2026 | EU Digital Identity Wallets due |
| 1 Jan 2027 | Verifactu for corporate-tax taxpayers |
| 12 Jan 2027 | Data Act switching charges abolished |
| 26 Mar 2027 | EHDS general application |
| 1 Jul 2027 | Verifactu for everyone else (the reference practice if self-employed) |
| 12 Sep 2027 | Data Act Chapter IV for legacy contracts |
| 2 Dec 2027 | AI Act Annex III high-risk obligations |
| 11 Dec 2027 | CRA fully applicable |
| 24 Dec 2027 | eIDAS 2 private relying parties |
| 2 Aug 2028 | AI Act Annex I high-risk obligations |
| 26 Mar 2029 | EHDS priority categories (a)–(c) and EHR systems |
| 26 Mar 2031 | EHDS categories (d)–(f) |

## 4. Reference use case

### 4.1 The reference application

The reference application is a private management application for a small Spanish psychology practice (one to a few psychologists). It is not published, and this specification does not describe its code, design or vendor choices; it states only the needs that such a practice implies. Its development starts when Encina reaches 1.0. The scenario needs:

- patients, appointments and session packs, with the calendar synchronised to a hosted calendar service over OAuth, and appointment reminders;
- clinical records (*historia clínica*), clinical consents and documents (Tier B, §4.2);
- invoices issued by the application under Verifactu, with corrective invoices;
- card and Bizum payments through payment gateways, with signed webhooks, refunds and chargebacks;
- patient messaging by e-mail, SMS and WhatsApp, transactional and commercial;
- a patient portal, and a Blazor Server back office next to minimal APIs;
- PostgreSQL, with EF Core for commands, Dapper for queries and the Marten compliance modules on the same database, and background jobs;
- several practices served by one deployment, each a tenant with its own data, credentials, invoice series, senders and keys (DEC-009). Other practices have shown interest, and each tenant pays for the infrastructure that clinical data requires; the first deployment may run a single tenant.

Its legal profile (notes [F](research/SPEC-002/F-health-practice.md) and [H](research/SPEC-002/H-integrations-law.md)): even without clinical notes it processes special-category data, because an appointment with a psychologist reveals health information [I]. A psychology practice is an authorised health centre [S]. Clinical records must be kept at least 5 years from each discharge and then blocked, not destroyed. Patients may learn who accessed their data (AEPD PD-00068-2026 of 13 Jul 2026 [S]; EHDS Art. 9 from 2029 [S]). Invoices are required even for VAT-exempt services, and Verifactu applies from 1 Jul 2027 (self-employed) or 1 Jan 2027 (company). A practice of this kind typically relies on US vendors for calendar, mail, messaging and payments, so transfer checks and processor roles matter. NIS2 (size [K]), the DPO duty (single professional), the eIDAS 2 wallet-acceptance duty (micro enterprise) and EHDS secondary-use duties (Art. 50) do not apply to a micro practice; the MDR, AI Act Art. 50, ENS and the CRA apply only under conditions the application can avoid or control. When one operator serves several practices, each practice is the controller of its patients' data and the operator is its processor (GDPR Art. 28), so records of processing, consents, processor registers and retention policies belong to the tenant, not to the deployment.

### 4.2 Tiers (DEC-001)

- **Tier A**: practice management without clinical data: appointments, session packs, invoices, payments, notifications, patient portal.
- **Tier B**: Tier A plus clinical records (*historia clínica*), clinical consents, minors and documents.

Decided by the maintainer on 2026-09-24 (DEC-001 (b)): **Tier B**. Encina must let an application handle clinical records within the law. The **[Tier B]** tag stays on the requirements that exist only because of clinical records, so that their origin remains visible; they are part of the 1.0 contract like any other. Consent given for a minor (REQ-059) is not a Tier B item: a practice that books appointments for minors needs it without clinical records, so it depends on §12 question 1, not on the tier.

### 4.3 Reference scenario "PracticeManagement" (acceptance gate)

A test suite that implements a slim, generic practice-management application, run in CI Full (location: DEC-007). It is written from the needs of §4.1 and copies no application code. Stack: `WebApplicationFactory`; PostgreSQL through Testcontainers with Respawn; EF Core commands, Dapper queries and the Marten compliance modules on the same database; `FakeTimeProvider` in Europe/Madrid, including DST days; WireMock fakes for an OAuth-protected calendar API, the WhatsApp Cloud API, an API-style card gateway and a Redsys-style signed-redirect gateway, and the AEAT (a Kestrel test server with `RequireCertificate` for mTLS); a fake e-mail channel; thin app-style adapters; entry points through minimal APIs, a Blazor Server circuit and background jobs (Encina scheduling and the Hangfire adapter).

**Every scenario gates 1.0** (maintainer decision of 2026-09-23, §5.11). The scenario runs with tenancy on (DEC-009): every scenario runs in one tenant, and S11 checks the isolation between two. The only part outside the gate is the rules part of S15, because the rules of REQ-011 stay post-1.0; it exists as a skeleton that compiles against the 1.0 public API (REQ-038). The **Order** column is only an ordering aid: order 1 needs the defects, the compliance lifecycle and the messaging shape changes; order 2 also needs a capability of §5.11 or the multi-tenancy of REQ-061.

The scenarios cover the eleven acceptance checks drawn from the private reference-application analysis: check 1 is S22, 2 is S0, 3 is S1, S4 and S17, 4 is S20, 5 is S13, 6 is S12, 7 is S23 and S15, 8 is S14, 9 is S19, 10 is S8 and 11 is S18. None was dropped.

| # | Scenario | REQs | Order |
|---|---|---|---|
| S0 | A command sent from a minimal API, a Blazor Server circuit event and a background job carries the right actor (therapist, operator, `system`) and tenant into the authorisation, audit, consent and cache-key behaviours; an operator is denied `clinical-notes:read`; a notification dispatched later from the outbox, an inbox message and a scheduled reminder carry the originating actor and tenant, rebuilt from persisted metadata | REQ-015, REQ-016, REQ-019 | 1 |
| S1 | Booking writes the appointment and its outbox message in one EF transaction; the calendar fake receives exactly one insert; echoes of the application's own changes are suppressed; a retry does not re-run handlers that already succeeded; a one-shot 24-hour reminder is scheduled and fires under `FakeTimeProvider` | REQ-017, REQ-031 | 1 |
| S2 | The calendar returns 5xx twice, then 200: delivered once. A 400 is not retried and is dead-lettered; exhaustion is logged, metered and visible in health; the dead-lettered message stays in the outbox table, is counted, and once requeued is delivered | REQ-017, REQ-034 | 1 |
| S3 | Book, reschedule and cancel are delivered in order per appointment | REQ-042 | 2 |
| S4 | A reminder across the October DST change; move and cancel by key; a recurring 09:00 Europe/Madrid job stays at 09:00 local time | REQ-033 | 1 |
| S5 | A WhatsApp status webhook: valid, duplicate, bad signature, stale | REQ-036 | 1 |
| S6 | Payment: checkout, success webhook delivered twice (deduplicated); an out-of-order refund handled by a saga waiting for an event | REQ-036, REQ-044 | 1 / 2 |
| S7 | Refund saga producing a corrective invoice, including the failure path | REQ-044 | 2 |
| S8 | 50 concurrent invoices numbered gaplessly per series through `ISequenceGenerator`, with a persistent chain that stays valid across a restart | REQ-045, REQ-047 | 2 |
| S9 | Verifactu-like submission: pacing, rejection of record N holds later records, server-directed wait honoured, mTLS | REQ-039, REQ-042, REQ-053 | 2 |
| S10 | OAuth refresh, concurrent refresh, `invalid_grant` pauses the partition | REQ-040 | 2 |
| S11 | Two practices run as tenants of one deployment. In the portal a patient sees only their own data. Each tenant's patients, outbox, inbox, saga and scheduled messages, read-audit entries, consents, processor register, retention policies and holds, OAuth tokens, gateway credentials, invoice series, notification senders, stored documents and encryption keys are scoped to the tenant: a request, a background job, an outbox dispatch or a retention sweep of tenant A never reads, sends with or decrypts with anything of tenant B; the same patient id in both tenants stays two subjects, and crypto-shredding it in tenant A leaves tenant B readable; both tenants number invoices of the same series key independently; an export or audit query of tenant A returns nothing of tenant B | REQ-015, REQ-061 | 2 |
| S12 | No WhatsApp marketing without channel consent while e-mail marketing is allowed; the LSSI art. 21.2 existing-client basis works with an opt-out; a reminder passes as transactional (a reminder is not a commercial communication [I]); the consent subject is the patient (`Guid` id), not the operator; the reminder is delivered through `Encina.Notifications` | REQ-014, REQ-016, REQ-041 | 1 / 2 |
| S13 | The therapist runs the Dapper appointment-list query and one read-audit entry per patient records who, when, which data and the purpose; "who accessed patient X in the last 3 years" returns it; with fail-closed on, an audit-store failure fails the read | REQ-007, REQ-008 | 1 |
| S14 | An erasure request for a patient with current-year invoices is partly refused (Art. 17(3)(b), with a grantable-after date) and marketing data is erased; retained data becomes blocked, invisible to therapist and operator queries and visible only through an audited authority disclosure; a legal hold stops the sweep; a failed hold check deletes nothing; a failed hold release is reported as a failure; the sweep marks each record deleted once and erases only the expired category. The patient's EF Core and Dapper rows are found through Encina's relational `IPersonalDataLocator` (REQ-054). [Tier B] A clinical record under a 5-year floor from discharge is refused and re-anchored when a new episode opens | REQ-001 – REQ-005, REQ-054 | 2 |
| S15 | [Tier B] An access export omits subjective annotations and third-party data, with reasons; a guardian's request for a minor is stored as acting for the minor, and a deceased patient's prohibition is stored with the subject (shape). The rules that act on those fields (refusing a relative's request for a deceased patient who prohibited access) are outside the 1.0 gate and exist as a skeleton | REQ-009, REQ-011 | 1 (Tier B) |
| S16 | Crypto-shredding the marketing category leaves clinical fields readable; shredding is refused while a floor or hold covers the key | REQ-006 | 1 |
| S17 | A transfer to a non-DPF-certified US recipient fails and to a certified one passes; a payment gateway is recorded as processor for payments and independent controller for fraud prevention (the roles of H-R23); a consumer mail account is flagged as having no Art. 28 terms; calendar sync is blocked while the calendar vendor's DPA is missing or expired | REQ-012, REQ-013 | 1 |
| S18 | The RoPA export lists "appointments and invoicing" with Art. 6(1)(b)/(c) and Art. 9(2)(h), and the processors with roles and transfer bases; a recorded breach shows its 72-hour deadline | REQ-010 | 1 |
| S19 | Creating a patient in EF Core and starting its retention record and consent in Marten either both commit or are reconciled; a fault injected between them leaves no untracked personal data | REQ-037 | 1 |
| S20 | Therapist notes and OAuth refresh tokens are redacted for the operator role, and they are ciphertext in PostgreSQL and plaintext through the application | REQ-019, REQ-055 | 1 / 2 |
| S21 | Processed outbox and executed scheduled messages carrying personal data are purged after their retention period | REQ-018 | 1 |
| S22 | Mediator: book, cancel, reschedule and complete run as Encina commands returning `Either<EncinaError, T>`; a double booking returns a coded error that becomes a 409 Problem+Json; validation, caching, idempotency and metrics come from Encina behaviours, not application code | REQ-038 | 1 |
| S23 | An access and portability request for patient X returns the appointments and invoices held in EF Core tables and the consents held in Marten streams, as one machine-readable export, through the same relational locator as S14 | REQ-004, REQ-054 | 2 |

## 5. Requirements

Identifiers are stable. Each requirement carries its 1.0 placement and the reason, using the SPEC-000 boundary:

- **Defect**: shipped behaviour conflicts with the law or with Encina's own documented contract. Pre-1.0 (SPEC-000 REQ-011; security-classified bugs cannot be deferred).
- **Shape**: changes a persisted format, a public contract or a security default. Pre-1.0, because breaking changes stop being free after 1.0 (SPEC-000 §2).
- **Capability**: additive. Post-1.0 unless a decision places it in 1.0; DEC-011 and the maintainer decision of §5.11 (both 2026-09-23) placed every capability of this section in 1.0, except the rules part of REQ-011.
- **Process/Docs**: pre-1.0 where SPEC-000 already requires it (SPEC-000 REQ-018 – REQ-021 and SPEC-000 REQ-024).

A "pre-1.0" placement that SPEC-000 does not already require was **decided** by the maintainer on 2026-09-23 (DEC-011 and §5.11) and enters the 1.0 contract when the SPEC-000 amendment of §11.2 is merged; §11.2 lists every such item, what drives it and what it costs.

### 5.1 Data lifecycle: retention, erasure, blocking

- **REQ-001** (pre-1.0, shape, DEC-011) A retention policy can state a **minimum** retention (a floor before which erasure is refused) and a maximum (after which erasure or blocking is due). The period starts from an event the application raises (episode discharge, end of fiscal year, death) rather than only from the tracking start; it can be re-anchored when a new episode opens; it uses calendar arithmetic, not 365-day years; policies vary by jurisdiction and document type, and a policy applies to its data category only (#1160). An *immutable record* class (invoices, Verifactu records) is corrected only by new records, never updated.
  *Rationale:* Ley 41/2002 art. 17.1 requires at least 5 years from each discharge, and some regions longer; invoices follow the tax limitation period (LGT, 4 years [K]; Código de Comercio art. 30, 6 years [S]). Today expiry is tracking start plus period (`src/Encina.Compliance.Retention/Services/DefaultRetentionRecordService.cs`), `RetentionPolicyType.EventBased` is unused, and enforcement erases every category of an entity when one expires (#1160). *Source:* F R17, C08, G01; H-R18.
- **REQ-002** (pre-1.0, defect, #1143, #1161) Retention enforcement never erases when it cannot establish that no legal hold applies: an error while checking holds skips the record, is observable (log and metric) and is retried; holds are recorded against the real entity, never `Guid.Empty`. Releasing a hold reports failure when the records it covers cannot be released, and the check for other holds on the same records fails closed.
  *Rationale:* `RetentionEnforcementService` maps a failed hold lookup to "no hold" and erases; `LiftHoldAsync` reports success when releasing records fails and its other-holds check fails open (#1161). *Source:* F C10, G02.
- **REQ-003** (pre-1.0, defect, #1142, #1146, #770, #1158, #1160) The retention sweep moves each record from active to expired to deleted exactly once, counts only what it erased, raises the alerts it documents, and reads time only from `TimeProvider`, event replay included. Erasure itself goes through the application-implemented `IRetentionDataEraser` port: for each expired `RetentionErasureTarget` (record id, entity id, data category, expiry, tenant id, module id), the sweep calls the port scoped to that one entity, that one data category and that tenant, and defers the erasure while another record of the same entity and data category is still retained (ADR-031, PR #1185). Sweep cycles on several instances do not overlap: a cycle runs under a distributed lock (`IDistributedLockProvider`), so two hosts never erase the same record twice.
  *Rationale:* the sweep re-erases on every cycle and its deletion trail is wrong, so a LOPDGDD art. 32 timeline cannot be proved; `RetentionRecordAggregate` reads `DateTimeOffset.UtcNow`; #1158 lists double-counted metrics, skipped alerts, re-erasure after a failed `MarkDeleted`, the missing cycle lock and stale documentation. #1142, #1143 and #1146 are in progress in PR #1157. *Source:* F C11, C33, G03, G22.
- **REQ-004** (pre-1.0, defect and shape, DEC-011) A data-subject erasure request consults retention floors, legal holds and declared Art. 17(3) exemptions before erasing anything, erases what is erasable, and returns a reasoned partial refusal per data category: the exemption, its legal basis and the date after which the request becomes grantable. The reason codes are current-law codes; the standardised refusal reasons of the Omnibus draft come with #814, built before 1.0 in the same persisted shape and off by default (REQ-024, DEC-012).
  *Rationale:* `src/Encina.Compliance.DataSubjectRights/Erasure/DefaultDataErasureExecutor.cs` honours only a static `[PersonalData(LegalRetention = true)]`, never calls `ILegalHoldService`, and `DSRErrors.ExemptionApplies` is unused. During the floor a patient's request must be refused for clinical records and invoices while contact and marketing data are erased (GDPR Art. 12(4), 17(3)(b), (c), (e)). The erasure finds relational data only through an `IPersonalDataLocator`; Encina ships none for EF Core, Dapper or ADO.NET: DEC-011 (d) puts one in 1.0 (REQ-054), and the arbitration works through whatever locator is registered, so an application with stores of its own can still add its locator. *Source:* F C09, G04; reference-application analysis (private).
- **REQ-005** (pre-1.0, shape, DEC-011) Encina provides a **blocked** data state (LOPDGDD art. 32 *bloqueo*): data that is retained but excluded from all processing and viewing, including versions superseded by a rectification; released only through an audited, purpose-bound disclosure path to an authorised role (courts, prosecutors, supervisory authorities); destroyed or crypto-shredded when a configured limitation period ends; with a secure-copy alternative where blocking is disproportionate (art. 32.4). Stores accessed through Encina (repositories and specifications on all 10 database providers, MongoDB included) get a blocking erasure strategy instead of hard delete, and Marten streams get the same state. Queries the application writes itself (Dapper or ADO.NET SQL, EF projections) are the application's part: Encina documents and ships the filter they apply (a column, view or query helper), and the application applies it. The design is jurisdiction-neutral ("restriction with statutory release roles").
  *Rationale:* a Spanish controller must block, not destroy, after erasure and after rectification; without this state it has to bypass the Retention and DSR modules. No blocking concept exists in `src/`; the closest are Art. 18 restriction and legal hold. Locating the relational rows to block needs the same locator as REQ-004. *Source:* F R12, C12, G05; reference-application analysis (private).
- **REQ-006** (pre-1.0, defect, #1144; key-store default and KMS wrapping, P-04) Crypto-shredding is scoped per data subject and data category (or retention class): erasing one category never makes another unreadable. Shredding is refused while a retention floor or legal hold covers data under the key, and it is the final step after blocking. Immutable records never share a key with erasable data. The subject key store defaults to durable storage outside development and supports key wrapping by a KMS.
  *Rationale:* `src/Encina.Marten.GDPR/Erasure/CryptoShredErasureStrategy.cs` deletes every key of the subject for each erasable field; the default key store is in memory and loses keys on restart. *Source:* F C13, G06; H-R18.

### 5.2 Access transparency

- **REQ-007** (pre-1.0, shape, DEC-011) Read audit can serve as evidence of access. An option makes the read fail when its audit entry cannot be stored; collection and paged reads record the entity ids they return; `RequirePurpose` can reject, not only warn; entries can carry the data-subject id and a data category; a query answers "who accessed data of subject X, when, which data, for which purpose"; retention of read-audit entries can be set per data category, and for special-category data it defaults to 3 years.
  **The 3-year default is a documented recommendation, not a legal requirement**: no rule applicable today fixes a period; EHDS Art. 9 [S] asks for at least 3 years only from 26 Mar 2029, and AEPD PD-00068-2026 [S] fixes none. The XML documentation and the package README say so, and the application sets its own period.
  *Rationale:* AEPD PD-00068-2026 [S] reads GDPR Art. 15 as entitling patients to the identity, time and data of accesses. Today `src/Encina.Security.Audit/AuditedRepository.cs` logs reads fire-and-forget, collection reads carry no ids, `RequirePurpose` only logs a warning and `ReadAuditOptions.RetentionDays` defaults to 365. The audit-store defects are prerequisites: #1128 and #1129 affect the write-audit stores, #1135 both the write- and the read-audit stores. *Source:* F R23, C04, G07.
- **REQ-008** (pre-1.0, capability, DEC-011) Read audit covers reads that do not go through Encina repositories, such as CQRS query handlers over Dapper, ADO.NET or EF projections: a request or response declares the subjects it exposes, and the audit behaviour writes one REQ-007 entry per subject.
  *Rationale:* an application that reads through query handlers (the reference scenario reads through Dapper query handlers) gets nothing from repository audit, and `AuditPipelineBehavior` records the request type and a payload hash, not the subjects. *Source:* reference-application analysis (private).
- **REQ-009** [Tier B] (pre-1.0, shape and disclosure defect, DEC-011, DEC-001 (b)) A DSR access export can exclude declared fields or records (third parties' data, professionals' subjective annotations, serious-harm withholding) and records the reason for each exclusion.
  *Rationale:* Ley 41/2002 art. 18.3 and Código Deontológico art. 42; without this the export discloses what the law says to withhold. No such marker exists (`DefaultDSRService`, `AccessResponse`). *Source:* F R18, C15, G09.

### 5.3 Legal modelling

- **REQ-010** (pre-1.0, shape, DEC-011) Processing activities, the RoPA export and lawful-basis records carry, next to the Art. 6 basis, the Art. 9(2) condition for special-category data and a reference to the national legal basis where one is required (for example LOPDGDD art. 9.2 and DA 17ª).
  *Rationale:* health processing needs both; `src/Encina.Compliance.GDPR/Model/LawfulBasis.cs` has only the six Art. 6 values, and the `Encina.Compliance.LawfulBasis` aggregate and read model (`src/Encina.Compliance.LawfulBasis/Aggregates/LawfulBasisAggregate.cs`, `ReadModels/LawfulBasisReadModel.cs`) record no Art. 9(2) condition either, so the Art. 30 record of a health practice cannot be complete. It applies in Tier A too. *Source:* F R01, R04, C23, G12.
- **REQ-011** [Tier B] (pre-1.0 for the persisted shape, post-1.0 for the rules; DEC-011, DEC-001 (b)) Consent and DSR records can state who acted for whom (a guardian or holder of parental authority for a minor's DSR request, a voluntary proxy), under which authority and until when; a data subject can be marked deceased, with the requesters authorised and any prohibition the subject expressed. Before 1.0 the fields are persisted and exposed; the rules that act on them (refusing a relative's request, checking a proxy's validity) are post-1.0 and must fit the shape without a breaking change. Consent given for a minor is REQ-059, outside the Tier B tag.
  *Rationale:* LOPDGDD art. 3; Ley 41/2002 arts. 9.3–9.4, 18.2 and 18.4; EHDS proxy services. The persisted aggregates have no such fields. *Source:* F R13, R14, C18, C19, G10, G11.
- **REQ-059** (pre-1.0, shape, DEC-011; not tied to DEC-001) Consent records can state that the subject is below the age of digital consent of the applicable jurisdiction (14 in Spain, LOPDGDD art. 7; configurable under GDPR Art. 8) and that consent was given or authorised by a holder of parental authority, identified in the record; the consent check refuses a minor's own consent below that age.
  *Rationale:* a practice that books appointments for minors processes their data without any clinical record, so the need does not depend on the tier. Numbered after the existing identifiers to keep them stable. *Source:* F R13, C18, G10.
- **REQ-012** (pre-1.0, defect, #1145) Transfer checks treat the EU–US Data Privacy Framework as adequacy only for DPF-certified recipients, offer the DPF as a transfer basis, and document the pending appeal C-703/25 P as a known risk.
  *Rationale:* `src/Encina.Compliance.DataResidency/Model/RegionRegistry.cs` marks the whole US adequate; a practice typically depends on US vendors for calendar, mail, messaging and payments. *Source:* F R11, C26, G13; H-R37.
- **REQ-013** (pre-1.0, shape, DEC-011) The processor register records, per vendor and purpose, whether the vendor acts as processor, independent controller or joint controller, and whether Art. 28 terms exist at all, so that `[RequiresProcessor]` can refuse or warn accordingly.
  *Rationale:* Stripe is a processor for payments and an independent controller for fraud and AML (Stripe DPA, 18 Nov 2025 [V]); consumer Google and Microsoft accounts have no DPA ([V]/[K]); `src/Encina.Compliance.ProcessorAgreements/Model/Processor.cs` has no role. *Source:* H-R23, H-R33, H-R34.
- **REQ-014** (pre-1.0, shape, DEC-011) Consent is scoped by purpose **and** channel (e-mail, SMS, WhatsApp, phone, terminal storage). It can record the LSSI art. 21.2 existing-client basis with an opt-out offered in every message. Outbound messages are marked transactional or commercial, so that reminders pass without marketing consent and promotions do not (an appointment reminder is not a commercial communication under LSSI Annex (f) [I]; the application decides how each message is marked). The `Encina.Compliance.Consent` specification carries GDPR, ePrivacy Art. 5(3) and LSSI arts. 20–22 tables (SPEC-000 REQ-024).
  *Rationale:* LSSI arts. 21 and 22.1 [V]; WhatsApp Business Messaging Policy [V]; `ConsentPurposes` has no channel dimension. ePrivacy is in the 1.0 contract (SPEC-000 DEC-002). *Source:* H-R30, H-R31.

### 5.4 Identity and context

- **REQ-015** (pre-1.0; defect for the live entry points, #1147, #1148; shape for persisted context, P-50, DEC-011) Every pipeline behaviour sees the actor, tenant, correlation id and idempotency key of the request, whatever the entry point: HTTP, Blazor Server circuits, background jobs with an explicit system actor, and outbox, inbox and scheduler dispatches. For the last three, the context of the request that created the message is **persisted with the message** (actor, tenant, correlation id, causation id) on all 10 database providers and rebuilt at dispatch, so that a handler running minutes later, on another host, still sees who caused it. Request-context time comes from `TimeProvider`.
  *Rationale:* `IEncina.Send` builds an empty context, and `AuthorizationPipelineBehavior` denies every request in a circuit; compliance, audit, cache and idempotency behaviours lose the "who". The outbox, inbox and scheduled-message tables store no context today, so even a fixed ambient context is lost at dispatch; that is a schema change on 10 providers. #1147 is implemented on branch `fix/request-context-1147` (an ambient request-context accessor in core and explicit context overloads) but does not cover persisted context. *Source:* reference-application analysis (private).
- **REQ-016** (pre-1.0, defect, #1149, in progress in PR #1159) The data subject is resolved independently of the actor: subject ids of any type (string, `Guid`, strongly-typed ids) are supported; a declared subject property that cannot be read fails closed with a clear error; there is no silent fallback to the caller's user id.
  *Rationale:* in a practice the psychologist or operator is not the patient, so a consent or restriction check can be evaluated against the wrong person. *Source:* reference-application analysis (private).

### 5.5 Messaging reliability and storage limitation

- **REQ-017** (pre-1.0, defect, #1150, #1151, #1152, #1153, #1154) The documented at-least-once guarantee holds on every provider: a `Left` from dispatch is a failure, never "processed"; retries use exponential backoff with jitter; exhaustion is logged, metered, visible in health checks and moves the message to a dead-letter state; Hangfire jobs fail on `Left` so that Hangfire retries them; `ScheduleRecurringAsync` propagates a failed insert; MongoDB has an outbox processor. A dead-lettered message **stays in the outbox table** in its dead-letter state, and each of the 10 outbox stores can count dead-lettered messages and requeue them, one by id or all that match a filter, for a new round of retries (decision on #1150: Option A, maintainer, 2026-09-24).
  *Rationale:* in a practice application AEAT submissions, refunds, reminders and calendar sync all go through the outbox and can be lost without trace today: `src/Encina.Messaging/Outbox/OutboxOrchestrator.cs` retries with a constant delay and never reports exhaustion. Keeping dead letters in the outbox table needs no new table and keeps them under the purge rules of REQ-018 (never purged automatically). Persistent dead-letter stores remain post-1.0 (#583, #149); the dead-letter *state*, its count and its requeue are not. #1152 and #1153 are in progress in PR #1159. *Source:* code read; reference-application analysis (private).
- **REQ-018** (pre-1.0, shape, DEC-011) Processed outbox messages and executed scheduled messages are purged after a configurable retention period on all 10 database providers, as the inbox already is. The defaults match the inbox (`InboxOptions.MessageRetentionPeriod` is 30 days, automatic purge on): 30 days after processing or execution, automatic purge on. Dead-lettered and pending messages are never purged automatically, and the application can lengthen the period or switch the purge off.
  *Rationale:* payloads carry personal and sometimes health data and are kept forever (GDPR Art. 5(1)(e)); `src/Encina.Messaging/Outbox/IOutboxStore.cs` has no purge, while `InboxOptions` has one. Evidence that a message was sent belongs in the application's records or the audit trail, not in the outbox. *Source:* code read; H-R28.

### 5.6 Security defaults

- **REQ-019** (pre-1.0, shape, DEC-006) Security- and compliance-relevant behaviours fail closed when they cannot decide, and any skip is an explicit, logged opt-out. This covers HMAC validation without an `HttpContext` (#1155), legal-hold checks (REQ-002), subject resolution (REQ-016) and PII response masking, which gains an option to fail the request instead of returning the unmasked response. Response redaction can depend on role, permission or purpose, so that an operator never sees therapist notes.
  *Rationale:* secure-by-default (CRA Annex I Part I(2)(b)) falls on integrators; Código Deontológico art. 46; Ley 41/2002 art. 16. `src/Encina.Security.PII/PIIMaskingPipelineBehavior.cs` returns the original response when masking fails. *Source:* A §4 item 7; reference-application analysis (private).
- **REQ-020** (pre-1.0 for the orphan attribute, DEC-011: #857 moves from "Post-1.0: Critical Bugs & Quality Debt (deferred items)" to "v0.14.0 — Hardening") The orphan `EncryptedField` attribute is consumed or removed before 1.0 (#857). Column-level encryption at rest through ORM value converters is REQ-055, in 1.0 through DEC-011 (e); `EncryptedField` becomes its marker.
  *Rationale:* GDPR Art. 32; a practice application must keep OAuth tokens and clinical notes encrypted at rest, and pipeline encryption (`EncryptionPipelineBehavior`) does not encrypt columns. A public attribute that does nothing misleads. *Source:* F C01, G18.
- **REQ-021** (pre-1.0, documentation honesty, #860) `Encina.Compliance.Attestation` either persists its hash chain or its README and specification state that the chain is in memory and not evidential. A persistent chained log is REQ-047.
  *Rationale:* Ley 41/2002 art. 14 (authenticity of changes) and LOPDGDD art. 32.4 (digital evidence); `HashChainAttestationProvider` keeps the chain in memory while the README claims self-hosted production use. *Source:* F C06, G08.

### 5.7 Documentation, claims and draft law

- **REQ-022** (pre-1.0, refines SPEC-000 REQ-024) Each of the 15 compliance packages has a SPEC-NNN with an article table: act, article, status (Covered, Partial, Not covered, Not applicable), Encina types, evidence (test or audit record) and tracking issue. The table includes the national articles the package helps apply: LOPDGDD art. 32 and Ley 41/2002 arts. 17 and 18.3 for Retention and DataSubjectRights; LSSI arts. 20–22 for Consent. It lists as "Not covered" what stays with the application or the organisation. The supporting packages `Encina.Security.Audit`, `Encina.Audit.Marten` and `Encina.Marten.GDPR` are cited as evidence where they implement an article. No coverage percentage is typed by hand (SPEC-001).
  *Rationale:* none of the 15 specifications exists; a compliance officer needs article-level claims. *Source:* D §2.2, §4.1; SPEC-000 REQ-024.
- **REQ-023** (pre-1.0) Package documentation matches the code: READMEs name only types that exist and only the providers actually supported; ADR-019 states the real provider set; every compliance package has a README; AIAct, Attestation, DataSubjectRights and GDPR have integration tests or a written justification.
  *Rationale:* the READMEs of BreachNotification, DataResidency, ProcessorAgreements and Retention name stores that do not exist and claim ten providers; [ADR-019](../architecture/adr/019-compliance-event-sourcing-marten.md) mentions "13 providers"; Anonymization, PrivacyByDesign, CrossBorderTransfer and `Encina.Security.Audit` have no README. *Source:* F C34, G14; D §4.2–§4.3; #1090, #689.
- **REQ-024** (pre-1.0, default behaviour and shape, DEC-012) A behaviour that implements a provision of a proposal not yet adopted (COM(2025) 837: #810–#816, §3.5) is off by default, names the draft article in its XML documentation and in its package specification, and leaves current-law behaviour as the default (72 hours and "risk" for GDPR Art. 33; no consent cooldown; no consent-exempt purposes beyond current law; the current Art. 12(5) grounds for refusing an access request; no AI-training legitimate interest; the national DPIA lists). All seven are **built before 1.0**, each behind an option that is off by default: #810 (a consent-refusal state and its events), #811 (exempt purposes in the purpose model), #814 (refusal reasons in the persisted DSR records) and #815 (AI-training values in the lawful-basis model) change persisted shapes or public models, so adding them after 1.0 would break the API; #812 (the ENISA single-entry-point adapter abstraction), #813 (a separate authority-notification threshold) and #816 (a DPIA criteria loader) are additive and cheap.
  *Rationale:* SPEC-000 REQ-026 schedules the Omnibus adaptations for 1.0 while the proposal is still in committee; a default that anticipates draft law would put applications out of line with current law. The maintainer's rule for DEC-012 (2026-09-24) is that a draft provision whose later adoption would need many changes, interface breaks or edits in many places is implemented before 1.0; the assessment of each issue against that rule is in DEC-012. *Source:* E §2; B §1.6; issue bodies of #810–#816 (read on 2026-09-24).
- **REQ-025** (process) §3 is re-verified as §3.6 says and its date is updated through a pull request; the pre-release checklist (#104) includes the step.
  *Rationale:* several dates in the research notes changed or were corrected within weeks. *Source:* B §10; E.
- **REQ-060** (pre-1.0, defect and shape, DEC-011) `Encina.Compliance.AIAct` lists every prohibited practice of Art. 5(1) as amended: it adds Art. 5(1)(b) (exploiting vulnerabilities due to age, disability or a social or economic situation), which is missing, and the new Art. 5(1)(ba) (non-consensual intimate deepfakes of identifiable persons) and 5(1)(bb) (AI that generates child sexual abuse material) added by Reg. (EU) 2026/1744 and applicable from 2 Dec 2026, with that date in their XML documentation. Until the catalogue is complete, the package, its README and its log messages do not claim to block prohibited practices "unconditionally" or to cover "8 prohibited practices"; they state which ones they cover (INV-002).
  *Rationale:* `src/Encina.Compliance.AIAct/Model/ProhibitedPractice.cs` (read on 2026-09-23) has eight values: (a), (c), (d), (e), (f) split into workplace and education, (g) and (h); (b) is absent. The README counts "8 prohibited practices, always blocked", and `AIActCompliancePipelineBehavior` and `AIActLogMessages` say "unconditionally". Adding enum members is a public-API change, free only before 1.0. The practice is declared by the application's classification, so shipping (ba) and (bb) before 2 Dec 2026 does not make any default follow unapplicable law (INV-003). *Source:* E §1 (AI Omnibus); code read.

### 5.8 Encina's own legal position and supply chain

These requirements do not come from obligations on Encina (§3.2): they let integrators meet the CRA duties that do bind them, and they keep Encina outside the CRA and the PLD.

- **REQ-026** (pre-1.0, process, DEC-010) Encina keeps the conditions of non-commercial FOSS: no price; no release, binary or security fix gated on payment or donation; paid services, if any, optional and separate; no telemetry that sends data to the maintainer or the project (phone-home), whether mandatory or on by default, and no personal-data condition of use. This concerns only data sent to the maintainer: Encina's own OpenTelemetry tracing, metrics and structured logging, which export only to destinations the application configures, are a design goal of the library (REQ-062), not a risk under this requirement. SECURITY.md or the README states the position: MIT, natural-person maintainer, not placed on the market under Reg. (EU) 2024/2847 (guidance C(2026) 5252, Example 34) nor under Dir. (EU) 2024/2853 Art. 2(2); integrators remain responsible under CRA Art. 13(5) and 13(6).
  *Source:* A §1.2–§1.4, §2, §4 items 9–10.
- **REQ-027** (pre-1.0) Every published package has an SBOM (CycloneDX 1.6 or SPDX 2.3 or later, JSON, with licence, PURL and hashes) attached to its GitHub Release.
  *Rationale:* integrators' SBOM (Annex I Part II(1)) and due diligence (Art. 13(5), recital 34). `.github/workflows/sbom.yml` covers only the core package and uploads a workflow artifact. *Source:* A §4 item 1.
- **REQ-028** (pre-1.0, with SPEC-000 REQ-021) [SECURITY.md](../../SECURITY.md) states: supported versions and a support period in month and year from 1.0; best-effort response targets; that denial of service, ReDoS and resource exhaustion in Encina's own code are in scope; how integrators report vulnerabilities and share fixes (CRA Art. 13(6); guidance points 223 and 227); and the cryptographic primitives Encina uses (.NET BCL AES-GCM, HMAC-SHA-2, SHA-256; no primitives of its own).
  *Rationale:* an integrator's support period weighs the support of core components (Art. 13(8)); cryptographic needs are checked against component documentation (guidance point 171). Today SECURITY.md gives no period and no targets and puts denial of service out of scope. *Source:* A §4 items 3 and 8.
- **REQ-029** (pre-1.0, process) Every security fix is published as a GitHub Security Advisory for the NuGet ecosystem with a CVE requested, and the changelog keeps a security history; after 1.0, security fixes ship as security-only patch releases where feasible.
  *Rationale:* advisories feed NuGetAudit and the EUVD that integrators check (recital 34; Annex I Part II(2) and (4)). *Source:* A §4 items 4–5.
- **REQ-030** (pre-1.0, with SPEC-000 REQ-018 and SPEC-000 REQ-019) Publishing to nuget.org uses Trusted Publishing (OIDC, no long-lived key) with the package-ID prefix reserved, in addition to the attestations and Sigstore signatures of SPEC-000 REQ-018; an OpenSSF Scorecard runs on the repository.
  *Source:* A §4 items 6 and 11.

### 5.9 Pre-1.0 shape changes found through the reference application

These come from the private analysis of the reference application; each rationale carries the Encina evidence anyone can check. Other items found by the same analysis are covered elsewhere: HMAC validation failing open by REQ-019, a package named in `CLAUDE.md` that does not exist by P-30 (§13), the in-memory Attestation chain by REQ-021, and the PostgreSQL and MySQL lock providers by SPEC-000 REQ-027.

- **REQ-031** (pre-1.0, shape) The outbox message id is the dispatch idempotency key; a handler that already succeeded is skipped when the message is retried (keyed by outbox id and handler type); dispatch attempts every handler and aggregates their errors instead of failing fast.
  *Rationale:* this changes the documented dispatch contract; today a retry re-runs every handler (duplicate calendar events and messages). Related: #733. *Source:* reference-application analysis (private).
- **REQ-032** (pre-1.0, shape) Persisted message types use stable names from a registry (for example `[MessageType("practice.appointment-booked.v1")]`) with an assembly-qualified fallback, and outbox, inbox and scheduled payloads can be versioned and upcast.
  *Rationale:* the assembly-qualified name is the persisted wire format; renaming a type after 1.0 would strand stored messages. Related: #134. *Source:* reference-application analysis (private).
- **REQ-033** (pre-1.0, shape, DEC-005 (a)) Scheduling gains a schedule key for upsert, cancel and reschedule on all 10 providers; time-zone-aware recurring cron that stores the zone; a shipped `ICronParser`; the orphan `Encina.EntityFrameworkCore.Scheduling.IMessageScheduler` implemented or removed; `DateTimeOffset` accepted.
  Under the same decision the outbox gains a nullable `PartitionKey` column on all 10 providers (P-51): set by the producer, persisted and returned by the store. The ordering and pacing behaviour built on it is REQ-042 (#469), also in 1.0 (§5.11).
  *Rationale:* schema and signature changes across 10 providers; a 09:00 Madrid reminder drifts at DST because cron is evaluated in UTC. Minimal slices of #146, #148 and #150. Adding the column after 1.0 is a schema migration for every user; adding it now is free. *Source:* reference-application analysis (private); H-R06 (AEAT flow control).
- **REQ-034** (pre-1.0, shape) Outbound errors are coded and carry `http.status`, `http.retry_after` and `transient` metadata, never the response body, and use the context correlation id; `RetryPipelineBehavior` classifies through `IErrorClassifier` (moved to core), honours Retry-After and never retries a non-transient error.
  *Rationale:* error codes are public contract; retrying a 4xx on a non-idempotent send (WhatsApp) duplicates messages; response bodies can carry personal data. Evidence: `src/Encina.Refit/Handlers/RestApiRequestHandler.cs`, `src/Encina.Polly/Behaviors/RetryPipelineBehavior.cs`. *Source:* reference-application analysis (private).
- **REQ-035** (pre-1.0, shape) The legacy `AddApplicationMessaging` alias is removed from `src/Encina/Core/ServiceCollectionExtensions.cs`.
  *Rationale:* it breaks the no-legacy rule, and a name this generic collides with application code. *Source:* reference-application analysis (private).
- **REQ-036** (pre-1.0 through #731 and #734) Inbound webhooks have a provider-neutral ingestion core: raw-body access; a configurable signature verifier (header, prefix, hex or Base64, canonical form such as `timestamp.body`, tolerance, several active secrets); replay protection through `INonceStore`; a provider event-id extractor; an inbox key made of source and event id; an in-progress duplicate answered with 200 or 409; size limits; fast acknowledgement. Vendor presets (Stripe-, Meta-, Redsys- and Google-style) are configuration, not SDK dependencies.
  *Rationale:* `Encina.Security.AntiTampering` verifies only its own canonical scheme (`HMAC/SignatureComponents.cs`) and cannot check vendor signatures. #731 and #734 are in v0.14.0; #202 stays post-1.0. The vendor schemes are named in DEC-016. *Source:* code read; H-R27, H-R30.

### 5.10 Persistence footprint and acceptance

- **REQ-037** (pre-1.0, docs and test, DEC-008) The 1.0 scope, ADR-019 and the package READMEs state that the nine event-sourced compliance modules and `Encina.Marten.GDPR` require PostgreSQL through Marten. Applications that keep their own data in EF Core or Dapper on the same PostgreSQL have a documented, tested pattern that keeps application writes and compliance-aggregate writes consistent: a shared connection and transaction, or an outbox bridge.
  *Rationale:* creating a patient in EF Core and starting its retention record or consent in Marten are dual writes today; either can fail and leave untracked personal data. *Source:* F C32, G23.
- **REQ-038** (pre-1.0) The PracticeManagement reference scenario (§4.3) exists, its README maps each scenario to the requirements it verifies and to the eleven acceptance checks of §4.3 without naming or describing the reference application, it runs in CI Full, and every scenario passes. The one exception is the rules part of S15 (REQ-011), which stays post-1.0: it exists as a skeleton that compiles against the 1.0 public API, so that the rules need no breaking change when they land.
  *Source:* reference-application analysis (private).

### 5.11 Integration and platform capabilities (in 1.0)

Capabilities that an application of the kind described in §4.1 needs from a framework. Until 2026-09-23 they were post-1.0 facilitators. On that date the maintainer placed **all of them in the 1.0 contract** (pre-1.0, capability) and accepted the later 1.0 date that follows. The reason: the reference application starts when Encina reaches 1.0, and building these capabilities once in the application and again in Encina would duplicate and diverge the code and leave Encina under-used. REQ-054 and REQ-055 were already in 1.0 through DEC-011 (d) and (e). DEC-004 (the first post-1.0 block) is superseded by this decision. Every capability of this section is tenant-aware (REQ-061) and instrumented (REQ-062); the channel satellites, webhook presets and storage providers shipped with them are those of DEC-016.

| REQ | Capability | Source | Tracking |
|---|---|---|---|
| REQ-039 | `Encina.Http`: outbound auth providers from secrets, client certificates and mTLS, per-client rate limiter honouring Retry-After, coded error mapping, redacting logging, OpenTelemetry | H-R06, H-R12; reference-application analysis (private) | P-31 |
| REQ-040 | `Encina.Security.OAuth`: encrypted token store on all 10 providers, single-flight refresh through `IDistributedLockProvider`, grant-revoked notification through the outbox, PKCE callback helper | H-R36; reference-application analysis (private) | P-32 |
| REQ-041 | `Encina.Notifications` and channel satellites (DEC-016): consent-aware multi-channel delivery, templates, delivery status, quiet hours, deduplication | H-R28 – H-R31; reference-application analysis (private) | P-33 |
| REQ-042 | Ordered outbox partitions with pacing, built on the nullable `PartitionKey` column of P-51 (DEC-005 (a)) | H-R06; reference-application analysis (private) | #469 |
| REQ-043 | Domain events written to the outbox in the same commit on all 10 providers | reference-application analysis (private) | P-34 |
| REQ-044 | Saga correlation by external key and a wait-for-event step with timeout and compensation | H-R27; reference-application analysis (private) | P-35 |
| REQ-045 | `ISequenceGenerator`: gapless per key, enlisted in the ambient transaction, on all 10 providers | H-R04, H-R13; reference-application analysis (private) | P-36 |
| REQ-046 | `Encina.Storage` (`IBlobStore`) with envelope encryption and retention, hold and blocking hooks; storage providers per DEC-016 | F R15; reference-application analysis (private) | P-37 (related #600, #311) |
| REQ-047 | Persistent hash-chained log with a pluggable hasher and a chain per partition, Attestation built on it, no Verifactu semantics (DEC-002) | F G08, G21; H-R04 | P-38 (related #860) |
| REQ-048 | Trusted time-stamping (RFC 3161, eIDAS qualified) and e-signature integration points | F G17 | P-39 |
| REQ-049 | Breach notifier channels: Art. 33(3) submission package and tracking of Art. 34 communications | F G16 | P-40 |
| REQ-050 | DPIA screening mapped to the AEPD Art. 35.4 and 35.5 lists | F G15 | P-41 (related #816, #686) |
| REQ-051 | Break-the-glass access with mandatory justification, time box and review | F G19 | P-42 |
| REQ-052 | EHDS readiness: logging component and EEHRxF export as extension points, revisited when the implementing acts are adopted (due by 26 Mar 2027) | F G20 | P-43 (related #808) |
| REQ-053 | X.509 certificate retrieval in `Encina.Security.Secrets`, with expiry health check and rotation | H-R12 | P-44 |
| REQ-054 | Relational `IPersonalDataLocator` for EF Core, Dapper and ADO.NET on all 10 providers (DEC-011 (d)); S14 and S23 depend on it | reference-application analysis (private) | P-45 |
| REQ-055 | Column-level encryption at rest through ORM value converters with KMS-wrapped keys, on all 10 providers (DEC-011 (e)); S20 depends on it | F G18; reference-application analysis (private) | P-46 (related #857) |
| REQ-056 | Row claiming (`SKIP LOCKED`, `READPAST`) in outbox and scheduler processors for multi-instance hosts | reference-application analysis (private) | P-47 |
| REQ-057 | ISO/IEC 27001 and 27701 control-mapping documents | C §6, §8 | P-48 |
| REQ-058 | VEX statements for dependency vulnerabilities not exploitable through Encina | A §4 item 2 | P-49 |

### 5.12 Cross-cutting: multi-tenancy and telemetry

- **REQ-061** (pre-1.0, shape, DEC-009 (b)) Every capability this specification adds or changes is **tenant-aware** on every store and provider it ships on. Persisted records carry the tenant id taken from the request context (REQ-015); queries and exports filter by it; background processing (outbox, inbox, saga and scheduler dispatch, the retention sweep, purges, row claiming) restores the tenant from persisted context and never mixes tenants in one unit of work. In particular:
  - credentials: OAuth tokens (REQ-040), outbound credentials and client certificates (REQ-039, REQ-053) and webhook secrets (REQ-036) are stored and resolved per tenant;
  - sequences and chains: invoice-series sequences (REQ-045) and chained logs (REQ-047) are keyed by tenant, so two tenants can use the same series key;
  - messaging and storage: notification senders and channel credentials (REQ-041) are configured per tenant, and blob storage (REQ-046) is partitioned by tenant;
  - keys: crypto-shredding subject keys (REQ-006) and column-encryption keys (REQ-055) are scoped per tenant, so one tenant's key material never decrypts another tenant's data, and shredding in one tenant never affects another;
  - audit: read and write audit (REQ-007, REQ-008) record the tenant, and the per-subject access query is scoped to it;
  - lifecycle and legal model: retention policies, legal holds and blocking (REQ-001 – REQ-005), consents (REQ-014, REQ-059), the processor register (REQ-013) and the RoPA (REQ-010) are per tenant, because each practice is its own controller (§4.1);
  - pacing: rate limits (REQ-039) and ordered partitions (REQ-042) are per tenant.

  A single-tenant application pays nothing: with tenancy off, a fixed default tenant applies and no tenant configuration is needed (pay-for-what-you-use).
  *Rationale:* the maintainer requires multi-tenancy in 1.0 even if the first deployment runs one tenant, because adding a tenant key to persisted records, sequence keys, key stores and credential stores after 1.0 breaks the API and the schema on the 10 providers and Marten. `Encina.Tenancy` already resolves tenants and offers connection strategies (`src/Encina.Tenancy/`), and several compliance modules carry a `TenantId`; `Encina.Marten.GDPR` (subject keys) and `Encina.Security.PII` carry none, the audit and ABAC stores do not filter by the current tenant automatically (#798), and the outbox, inbox, saga and scheduled-message entities have no tenant column (#737, #738, #739, #760). *Source:* maintainer decision DEC-009 (2026-09-24); code read on 2026-09-24.
- **REQ-062** (pre-1.0, process and shape, DEC-010; ADR-018, ADR-021) Every capability this specification adds or changes integrates Encina's observability functions: OpenTelemetry tracing (an `ActivitySource` with semantic attributes) and metrics (a `Meter`), structured logging through `[LoggerMessage]` with EventIds inside a range registered in `EventIdRanges` (ADR-021), and a health check where the capability has a checkable dependency (a store, a key store, an outbound endpoint, a certificate close to expiry). Telemetry carries no payloads, no response bodies (REQ-034) and no direct identifiers of data subjects; identifiers are pseudonymous or omitted, and the tenant id is an attribute. Telemetry goes only to the exporters and sinks the application configures; Encina never sends telemetry to its maintainer or project (REQ-026).
  *Rationale:* Encina is designed as a library with first-class telemetry, and the project enforces it through code, issue templates, reviews, tests and documentation (the cross-cutting rule of ADR-018). A compliance capability that cannot be observed can be neither operated nor evidenced: a retention sweep, a dead-lettered submission, a breach clock or a failing audit store must be visible. DEC-010 excludes only telemetry that sends data to the maintainer. *Source:* maintainer decision DEC-010 (2026-09-24); [ADR-018](../architecture/adr/018-cross-cutting-integration-principle.md); [ADR-021](../architecture/adr/021-eventid-uniqueness-enforcement.md).

## 6. Coverage matrix

Status on 2026-09-23 (REQ-061 and REQ-062: 2026-09-24): **Covered**, **Partial**, **Missing** or **Conflict** (would block or undermine compliance). Paths are relative to the repository root; "P-nn" are proposed issues (§13). Work in progress is named in the Tracking column (PR #1157 for #1142, #1143 and #1146; PR #1159 for #1149, #1152 and #1153; branch `fix/request-context-1147` for #1147); the status stays as read on `main` until the work merges.

| REQ | Package / type | Status | Evidence | Tracking |
|---|---|---|---|---|
| REQ-001 | `Encina.Compliance.Retention` policies | Conflict | `src/Encina.Compliance.Retention/Services/DefaultRetentionRecordService.cs`; `Model/RetentionPolicyType.cs` (`EventBased` unused) | P-01, #1160 |
| REQ-002 | `RetentionEnforcementService`, legal-hold release | Conflict | `src/Encina.Compliance.Retention/RetentionEnforcementService.cs` | #1143 (PR #1157), #1161 |
| REQ-003 | `RetentionEnforcementService`, `RetentionRecordAggregate` | Conflict | `src/Encina.Compliance.Retention/Aggregates/RetentionRecordAggregate.cs` | #1142, #1146 (PR #1157), #770, #1158, #1160 |
| REQ-004 | `DefaultDataErasureExecutor`, `IPersonalDataLocator` | Conflict | `src/Encina.Compliance.DataSubjectRights/Erasure/DefaultDataErasureExecutor.cs`; no relational locator (only `src/Encina.Marten.GDPR/Locator/MartenEventPersonalDataLocator.cs`) | P-02 (related #814), P-45 |
| REQ-005 | none (closest: `ProcessingRestrictionPipelineBehavior`, legal hold) | Missing | `src/Encina.Compliance.DataSubjectRights/ProcessingRestrictionPipelineBehavior.cs` | P-03, P-45 |
| REQ-006 | `CryptoShredErasureStrategy`, `ISubjectKeyProvider` | Conflict | `src/Encina.Marten.GDPR/Erasure/CryptoShredErasureStrategy.cs`; `src/Encina.Marten.GDPR/Abstractions/ISubjectKeyProvider.cs` | #1144, P-04 |
| REQ-007 | `AuditedRepository`, `ReadAuditOptions`, read-audit stores | Partial | `src/Encina.Security.Audit/AuditedRepository.cs`; `src/Encina.Security.Audit/ReadAuditOptions.cs` | P-05, #1128, #1129, #1135, #767, #751 |
| REQ-008 | `AuditPipelineBehavior` | Missing | `src/Encina.Security.Audit/AuditPipelineBehavior.cs` | P-06 |
| REQ-009 | `DefaultDSRService`, `AccessResponse`, `[PersonalData]` | Missing | `src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs`; `Model/AccessResponse.cs`; `Attributes/PersonalDataAttribute.cs` | P-07 |
| REQ-010 | `LawfulBasis`, `ProcessingActivity`, RoPA exporters, `Encina.Compliance.LawfulBasis` | Missing | `src/Encina.Compliance.GDPR/Model/LawfulBasis.cs`; `Model/ProcessingActivity.cs`; `src/Encina.Compliance.LawfulBasis/Aggregates/LawfulBasisAggregate.cs`; `src/Encina.Compliance.LawfulBasis/ReadModels/LawfulBasisReadModel.cs` | P-08 |
| REQ-011 | Consent and DSR aggregates | Missing | no guardian, representative or deceased concept in `src/` | P-09 |
| REQ-059 | Consent aggregate and `ConsentRequiredPipelineBehavior` | Missing | no minor or parental-authority concept in `src/Encina.Compliance.Consent/` | P-52 |
| REQ-012 | `RegionRegistry`, `TransferBasis` | Conflict | `src/Encina.Compliance.DataResidency/Model/RegionRegistry.cs`; `src/Encina.Compliance.CrossBorderTransfer/Model/TransferBasis.cs` | #1145 |
| REQ-013 | `Processor`, `[RequiresProcessor]` | Partial | `src/Encina.Compliance.ProcessorAgreements/Model/Processor.cs` | P-10 |
| REQ-014 | `ConsentPurposes`, `ConsentRequiredPipelineBehavior` | Partial | `src/Encina.Compliance.Consent/Model/ConsentPurposes.cs`; `src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs` | P-11, #810, #811 |
| REQ-015 | `Encina`, `RequestContext`, `EncinaContextMiddleware`, `AuthorizationPipelineBehavior` | Conflict | `src/Encina/Core/Encina.cs`; `src/Encina/Core/RequestContext.cs`; `src/Encina.AspNetCore/EncinaContextMiddleware.cs`; `src/Encina.AspNetCore/AuthorizationPipelineBehavior.cs`; outbox, inbox and scheduled-message tables store no context | #1147 (branch `fix/request-context-1147`), #1148, P-50 |
| REQ-016 | Consent and DSR subject extraction | Conflict | `src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs`; `src/Encina.Compliance.DataSubjectRights/DefaultDataSubjectIdExtractor.cs` | #1149 (PR #1159) |
| REQ-017 | Outbox orchestrator and processors, Hangfire adapters, scheduler | Conflict | `src/Encina.Messaging/Outbox/OutboxOrchestrator.cs`; `src/Encina.EntityFrameworkCore/Outbox/OutboxProcessor.cs`; `src/Encina.Hangfire/HangfireRequestJobAdapter.cs`; `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs` | #1150 (Option A: dead letters stay in the outbox table, count and requeue on the 10 stores), #1151, #1152 and #1153 (PR #1159), #1154 |
| REQ-018 | `IOutboxStore`, scheduled-message stores | Missing | `src/Encina.Messaging/Outbox/IOutboxStore.cs` (no purge); `src/Encina.Messaging/Inbox/InboxOptions.cs` (inbox parity) | P-12 |
| REQ-019 | `HMACValidationPipelineBehavior`, `PIIMaskingPipelineBehavior` | Conflict | `src/Encina.Security.AntiTampering/Pipeline/HMACValidationPipelineBehavior.cs`; `src/Encina.Security.PII/PIIMaskingPipelineBehavior.cs` | #1155, P-13, #858 |
| REQ-020 | `EncryptedField`, `EncryptionPipelineBehavior` | Partial | `src/Encina.Security.Encryption/EncryptionPipelineBehavior.cs` | #857, P-46 |
| REQ-021 | `HashChainAttestationProvider` | Conflict (docs) | `src/Encina.Compliance.Attestation/Providers/HashChainAttestationProvider.cs` | #860 |
| REQ-022 | 15 compliance packages | Missing | `docs/specifications/` holds SPEC-000 to SPEC-002 only | P-17 |
| REQ-023 | Compliance READMEs, ADR-019, integration tests | Conflict (docs) | `docs/architecture/adr/019-compliance-event-sourcing-marten.md`; package READMEs | P-14, P-15, P-16, #689, #1090 |
| REQ-024 | Omnibus adaptations | Missing (not yet built) | #810–#816 open | #810–#816, all before 1.0 and off by default (DEC-012); acceptance criterion added to each |
| REQ-025 | This specification, §3 | Partial | §3 of this document | #104 |
| REQ-026 | Licence and distribution model | Covered (statement missing) | `LICENSE`; no funding file; no telemetry sent to the maintainer | P-27 |
| REQ-027 | SBOM workflow | Partial | `.github/workflows/sbom.yml` (core package only, workflow artifact) | P-26 |
| REQ-028 | SECURITY.md | Partial | `SECURITY.md` | P-27 |
| REQ-029 | Advisory process | Partial | `SECURITY.md` promises GHSA; none published | P-28 |
| REQ-030 | Publishing | Missing | GitHub Packages only; no signing or attestation | #92, #93, #100, #101, P-29 |
| REQ-031 | Outbox processors | Missing | `src/Encina.EntityFrameworkCore/Outbox/OutboxProcessor.cs` | P-18, #733 |
| REQ-032 | Outbox and scheduler type names | Missing | assembly-qualified names in `OutboxOrchestrator` and `SchedulerOrchestrator` | P-19, #134 |
| REQ-033 | Scheduling; outbox `PartitionKey` | Missing | `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs`; `src/Encina.EntityFrameworkCore/Scheduling/IMessageScheduler.cs`; `src/Encina.Messaging/Outbox/IOutboxMessage.cs` (no partition key) | P-20, P-51, #146, #148, #150 |
| REQ-034 | Refit handler, Polly retry, error classifier | Conflict | `src/Encina.Refit/Handlers/RestApiRequestHandler.cs`; `src/Encina.Polly/Behaviors/RetryPipelineBehavior.cs`; `src/Encina.Messaging/Recoverability/IErrorClassifier.cs` | P-21 |
| REQ-035 | Core registration | Conflict (house rule) | `src/Encina/Core/ServiceCollectionExtensions.cs` | P-22 |
| REQ-036 | AntiTampering, inbox | Partial | `src/Encina.Security.AntiTampering/HMAC/SignatureComponents.cs` | #731, #734 (#202 post-1.0) |
| REQ-037 | Marten compliance modules, EF Core, Dapper | Partial | `docs/architecture/adr/019-compliance-event-sourcing-marten.md`; no shared EF and Marten transaction path | P-14, P-23 |
| REQ-038 | Reference scenario | Missing | — | P-25 |
| REQ-039 – REQ-058 | Integration and platform capabilities (§5.11), in 1.0 | Missing | — | P-31 – P-49, #469 |
| REQ-060 | `ProhibitedPractice`, AIAct README and log messages | Conflict (claim) | `src/Encina.Compliance.AIAct/Model/ProhibitedPractice.cs`; `src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs`; `src/Encina.Compliance.AIAct/README.md` | P-53 |
| REQ-061 | `Encina.Tenancy`; tenant columns and filters in the stores SPEC-002 adds or changes | Partial | `src/Encina.Tenancy/Abstractions/ITenantProvider.cs`; no tenant in `src/Encina.Marten.GDPR/` (subject keys) or `src/Encina.Security.PII/`; the outbox, inbox, saga and scheduled-message entities have no tenant column (#737, #738, #739, #760); the audit and ABAC stores do not filter by the current tenant automatically (#798) | P-54, #737, #738, #739, #760, #798, #596 |
| REQ-062 | Observability of the SPEC-002 capabilities | Partial (existing packages instrumented; the new capabilities are not built) | `src/Encina/Diagnostics/EventIdRanges.cs`; `docs/architecture/adr/018-cross-cutting-integration-principle.md` | criterion in every P-issue and in the EPIC P-00 |

## 7. Acceptance criteria

| AC | Requirement | Criterion |
|---|---|---|
| AC-001 | REQ-001 | A policy with a 5-year floor anchored to an application event refuses erasure before the floor, re-anchors when a new episode opens and computes calendar years across a leap day; for one entity type, policies that differ by jurisdiction (a regional period longer than the national 5 years) and by document type (clinical record, invoice) each resolve to their own floor, and expiry of one category leaves the others untouched (#1160 closed); an immutable-record class rejects updates and accepts a corrective record; S14 passes. |
| AC-002 | REQ-002 | #1143 closed with a test in which the hold lookup returns `Left`: nothing is erased, the failure is logged and metered, and the record is retried on the next cycle. #1161 closed with tests in which a failed release makes `LiftHoldAsync` return `Left` and a failed other-holds check keeps the records held. |
| AC-003 | REQ-003 | #1142, #1146 and #1158 closed; an integration test runs two sweep cycles and observes one erasure and one deleted transition per record; two concurrent cycles on two hosts erase each record once; `Encina.Compliance.Retention` has no `DateTime.UtcNow` or `DateTimeOffset.UtcNow`. |
| AC-004 | REQ-004 | An erasure request for a subject with held data under a floor returns per-category results (erased; refused with the Art. 17(3) exemption and a grantable-after date) and touches no held data, with relational data found through the registered `IPersonalDataLocator`; S14 and S23 pass. |
| AC-005 | REQ-005 | On each of the 10 database providers (MongoDB included) and on Marten, blocked data is absent from queries made through Encina repositories and specifications; versions superseded by a rectification are blocked; an authority disclosure writes an audit entry with purpose and role; the art. 32.4 alternative produces a secure copy whose hash is recorded in the audit trail and then releases the blocked original for destruction; at the end of the period the data is destroyed or crypto-shredded under `FakeTimeProvider` and the destruction is audited. The documented filter for application-written SQL is applied by the scenario's Dapper agenda query, which returns no blocked row (the application's part); S14 passes. |
| AC-006 | REQ-006 | #1144 closed; shredding the marketing category leaves the clinical category readable; shredding under a floor or hold is refused; a production registration without a durable key store fails at start-up or is an explicit, logged opt-out; with a fake KMS, stored subject keys are wrapped and unwrap only through it; an attempt to protect an immutable-record field with a key that also protects erasable data is rejected; S16 passes. |
| AC-007 | REQ-007 | Tests show: a store failure with fail-closed fails the read; collection reads record ids; `RequirePurpose` rejects when so configured; the per-subject query returns the entries; retention is configurable per data category and the special-category default is 3 years, and the XML documentation and README call it a recommendation, not a legal requirement; #1128, #1129 and #1135 closed; S13 passes. |
| AC-008 | REQ-008 | A Dapper query handler that declares its subjects produces one read-audit entry per subject; S13 passes. |
| AC-009 | REQ-009 | [Tier B] An access export omits a field marked as subjective annotation and returns the reason; S15 passes. |
| AC-010 | REQ-010 | The RoPA JSON and CSV exports carry the Art. 9(2) condition and the national basis; a special-category activity without an Art. 9(2) condition fails validation; S18 passes. |
| AC-011 | REQ-011 | [Tier B] Consent and DSR events persist and return acted-by, acted-for, authority and validity, and a subject's deceased status with its authorised requesters and prohibition, on the stores those aggregates use (shape only; the rules that act on them are verified when they land after 1.0); the shape parts of S15 pass. |
| AC-012 | REQ-012 | #1145 closed; the DataResidency and CrossBorderTransfer READMEs and specifications document the pending appeal C-703/25 P as a known risk and name the §3.6 re-verification trigger; S17 transfer checks pass. |
| AC-013 | REQ-013 | A vendor registered as independent controller for a purpose makes `[RequiresProcessor]` on that purpose refuse or warn; "no Art. 28 terms" appears in the RoPA export; S17 passes. |
| AC-014 | REQ-014 | Channel-scoped refusal and grant, the art. 21.2 basis and the transactional marker are tested; the Consent specification has GDPR, ePrivacy and LSSI tables; S12 passes. |
| AC-015 | REQ-015 | #1147, #1148 and P-50 closed; S0 passes from a minimal API, a Blazor Server circuit and a Hangfire job; an integration test on each of the 10 database providers stores a notification in the outbox from a request with a known actor and tenant, dispatches it from the outbox processor in a fresh scope, and asserts that the handler and the audit behaviour see that actor and tenant; the same holds for an inbox message and a scheduled message. |
| AC-016 | REQ-016 | #1149 closed; a `Guid` subject resolves; an unreadable declared subject returns a coded error; S12 passes. |
| AC-017 | REQ-017 | #1150 – #1154 closed; on each of the 10 database providers (MongoDB through its new outbox processor), a handler failing twice and then succeeding is delivered once, and exhaustion produces a log entry, a metric, a degraded health check and a dead-letter state; the dead-lettered message is still in the outbox table, the store's count of dead-lettered messages includes it, and requeuing it by id (and requeuing all that match a filter) makes it pending again and delivers it once; S2 passes. |
| AC-018 | REQ-018 | The purge operation exists on the outbox and scheduled-message stores and is integration-tested on the 10 providers, including the 30-day default and the rule that dead-lettered and pending messages are kept; S21 passes. |
| AC-019 | REQ-019 | #1155 closed with fail-closed as the default; a PII masking failure with fail-closed on returns `Left`; the operator receives redacted notes; S0 and S20 pass. |
| AC-020 | REQ-020 | #857 closed, with `EncryptedField` consumed as the marker of the column-level encryption of REQ-055. |
| AC-021 | REQ-021 | #860 closed; README and Attestation specification match the behaviour. |
| AC-022 | REQ-022 | Fifteen SPEC-NNN files exist under `docs/specifications/` with the table of REQ-022; each package README links its specification; the SPEC-001 citation check stays green. |
| AC-023 | REQ-023 | Every type named in a compliance README exists in the package's `PublicAPI.Shipped.txt` or `PublicAPI.Unshipped.txt`; ADR-019 amended; READMEs exist for the four packages; the four packages have integration tests or a justification file. |
| AC-024 | REQ-024 | All seven of #810–#816 are closed before `1.0.0-rc.1` (DEC-012). Each of #810–#813 and #815 closes with the draft behaviour off by default and a test asserting that the default follows current law; for #810, #811 and #815 a test also shows that the persisted state, events or model values they add are written and read back unchanged while the option is off (the shape exists, the behaviour does not act). #814 and #816 add capabilities that current law already allows (reason codes on a refused access request; loading DPIA criteria from an external list): each closes with its draft-specific part (the Omnibus refusal grounds; the EDPB harmonised list as a source) behind an option that is off by default and named by its draft article, and with a test asserting that the default uses the current-law grounds and the national lists. |
| AC-025 | REQ-025 | The §3 verification date is within 30 days of each minor tag and of `1.0.0-rc.1`; #104 lists the step. |
| AC-026 | REQ-026 | SECURITY.md or README carries the legal-status statement; the release review confirms that no package sends data to the maintainer and that no release asset is gated. |
| AC-027 | REQ-027 | The `1.0.0-rc.1` release has one SBOM per published package attached. |
| AC-028 | REQ-028 | SECURITY.md contains the five elements of REQ-028. |
| AC-029 | REQ-029 | SECURITY.md or CONTRIBUTING.md documents the advisory process. Every security fix released after approval has a GHSA for the NuGet ecosystem with a CVE requested; if no security fix is released before `1.0.0-rc.1`, a draft advisory in the repository shows that the process works end to end. |
| AC-030 | REQ-030 | `1.0.0-rc.1` is published through Trusted Publishing with the prefix reserved; the Scorecard workflow exists; #92, #93, #100 and #101 closed. |
| AC-031 | REQ-031 | A notification with two handlers where the second fails is retried and only the second handler runs again; both handlers' errors are aggregated; the per-handler completion record is integration-tested on each of the 10 database providers; S1 passes. |
| AC-032 | REQ-032 | Stored rows carry the registry name; renaming the CLR type while keeping the name still dispatches; an upcaster migrates a v1 payload. |
| AC-033 | REQ-033 | Upsert and cancel by key work on the 10 providers, and a 09:00 Europe/Madrid recurring job fires at 09:00 local time on both sides of a DST change; S4 passes; P-51 closed, and an integration test on each of the 10 providers stores an outbox message with a `PartitionKey` and reads it back unchanged, and one without it as null. |
| AC-034 | REQ-034 | Outbound errors carry codes and metadata and no response body; a 400 is not retried; a 429 waits for Retry-After; S2 passes. |
| AC-035 | REQ-035 | `AddApplicationMessaging` no longer appears in any `PublicAPI` file. |
| AC-036 | REQ-036 | #731 and #734 closed; Stripe-, Meta- and Redsys-style schemes verify by configuration alone; tests show that a body over the configured size limit is rejected before the signature is computed; that a duplicate arriving while the first delivery is still in progress is answered with the configured 200 or 409 and processed once; that a signature made with either of two active secrets verifies during rotation and one made with a retired secret does not; and that a replayed nonce or an event id already seen is refused through `INonceStore`; S5 passes. |
| AC-037 | REQ-037 | ADR-019 and the READMEs state the PostgreSQL requirement; the consistency pattern is documented and S19 passes. |
| AC-038 | REQ-038 | Every scenario S0–S23 is green in CI Full on the release-candidate commit, with tenancy on. The only exception is the rules part of S15, which exists as a skeleton that compiles against the `1.0.0-rc.1` public API and is skipped with a reason; if those rules could only be written with a change to a shipped public API, this criterion fails. The scenario README names no application and describes none. |
| AC-039 | REQ-039 – REQ-058 | Each capability has a P-issue (or #469) in a pre-1.0 milestone at approval, and before `1.0.0-rc.1` it is implemented on the providers it ships on, tenant-aware (AC-043) and instrumented (AC-044), and verified as follows: REQ-039 and REQ-053 by S9 and S10; REQ-040 by S10 and S20; REQ-041 by S12 with each channel satellite of DEC-016; REQ-042 by S3 and S9; REQ-043 by an integration test on each of the 10 providers in which a rolled-back command leaves no outbox row and a committed one leaves exactly one, and by S1; REQ-044 by S6 and S7; REQ-045 and REQ-047 by S8, with the sequence tested on each of the 10 providers; REQ-046 by S11 and an integration test per storage provider of DEC-016 in which a held or blocked blob cannot be deleted; REQ-048 by a test against a fake RFC 3161 authority; REQ-049 by S18 and a test of the Art. 33(3) package; REQ-050 by tests over both AEPD lists; REQ-051 by a test in which break-the-glass access without a justification is refused and one with a justification is audited and expires; REQ-052 by a test of the logging and export extension points; REQ-054 by S14 and S23; REQ-055 by S20 on each of the 10 providers; REQ-056 by a two-processor test on each of the 10 providers in which no message is processed twice; REQ-057 by the two mapping documents in `docs/`; REQ-058 by VEX statements published with `1.0.0-rc.1`. |
| AC-040 | This specification | It moves to APPROVED when DEC-001 … DEC-016 carry a human decision (DEC-004 SUPERSEDED counts as decided), the SPEC-000 amendment of §11.2 is merged, the EPIC and the P-issues exist, and the row in `docs/specifications/README.md` shows the new status. |
| AC-041 | REQ-059 | A consent for a subject below the configured age is refused unless it records a holder of parental authority, and one recorded by that holder is accepted; the age is configurable per jurisdiction with 14 as the Spanish value. |
| AC-042 | REQ-060 | P-53 closed; `ProhibitedPractice` has members for Art. 5(1)(a), (b), (ba), (bb), (c), (d), (e), (f) (workplace and education), (g) and (h), each naming its point and, for (ba) and (bb), the 2 Dec 2026 application date; no README, XML comment or log message of `Encina.Compliance.AIAct` claims a coverage the enum does not have. |
| AC-043 | REQ-061 | P-54, #737, #738, #739, #760, #798 and #596 closed; S11 passes. On each of the 10 database providers and on Marten, an integration test writes, under two tenants, the records of every store SPEC-002 adds or changes and shows that a query, an export, a DSR request and a background cycle (outbox, inbox, saga and scheduler dispatch, retention sweep, purge) run for tenant A never return, act on or export a record of tenant B. Tests show that a subject key or a column-encryption key of tenant A cannot decrypt tenant B's data; that crypto-shredding a subject in tenant A leaves the same subject id in tenant B readable; that two tenants get independent gapless sequences and chains for the same series key; and that OAuth tokens, gateway and channel credentials, client certificates and webhook secrets resolve per tenant, with a missing tenant configuration failing closed instead of falling back to another tenant's. With tenancy off, the same tests pass with no tenant configuration. |
| AC-044 | REQ-062 | Every P-issue, and every existing issue that SPEC-002 pulls into 1.0, records its ADR-018 evaluation, and its pull request shows the outcome for tracing, metrics, logging and health checks. For each capability: a test with an in-memory exporter asserts that its main operations emit their activity and their metric with the tenant as an attribute; `EncinaEventIdAllocationTests` covers its assembly, so its log messages have EventIds inside a registered range; a health-check test covers each checkable dependency; and a test asserts that no activity attribute, metric tag or log message of the capability carries a payload, a response body or a direct identifier of a data subject. The release review of AC-026 confirms that no package contains an endpoint that sends telemetry to the maintainer or the project. |

## 8. Constraints

- `CLAUDE.md` rules apply to every change made for this specification: time from `TimeProvider`; secrets never leak through serialization or `ToString()`; asynchronous database calls with `CancellationToken`; no `[Obsolete]`, no legacy aliases.
- Provider coherence: a feature that touches stores ships on all 10 database providers, except the Marten-only compliance modules of ADR-019, whose scope DEC-008 states.
- Cross-cutting integration (ADR-018), EventId ranges (ADR-021) and the per-flag coverage model apply; documentation cites coverage through SPEC-001 markers, never by hand.
- Opt-in by default for features, safe by default for security- and compliance-relevant behaviour (INV-005).
- Pre-1.0: breaking changes are free and preferred over compatibility layers.
- No regulation-specific code that would make the maintainer a regulated party without a decision (INV-004).
- Multi-tenancy (DEC-009): every capability this specification adds or changes is tenant-aware, and single-tenant applications need no tenant configuration (REQ-061).
- Observability (DEC-010): every capability this specification adds or changes is instrumented with OpenTelemetry and structured logging, and none sends telemetry to the maintainer (REQ-062).

## 9. Invariants

- **INV-001** No Encina default or behaviour silently destroys data an application must retain, or discloses data it must withhold, without an explicit application opt-in.
- **INV-002** Encina never claims coverage of an article it does not cover; partial coverage is stated as partial.
- **INV-003** Default behaviour follows the law **applicable** on the verification date of §3 (in force is not enough: a provision in force but not yet applicable does not change a default before its date). Draft law is opt-in only.
- **INV-004** Encina ships no code whose purpose is a single regulation that would make its maintainer a CRA or PLD manufacturer, an open-source steward or a Verifactu producer, unless a DEC accepts that role.
- **INV-005** Security- and compliance-relevant behaviours fail closed unless an explicit, logged opt-out says otherwise.
- **INV-006** Agents do not change this document once approved; they propose changes as a pull request with the human as approver (as SPEC-000 INV-005).
- **INV-007** Every capability Encina ships for this specification can be observed through OpenTelemetry traces and metrics and structured logs that the application routes; Encina never sends telemetry to its maintainer or project.
- **INV-008** With tenancy on, no Encina store, query, export, background cycle, cache entry, key or credential returns, uses or affects the data of a tenant other than the one in the request context or in the persisted context of the message being processed.

## 10. Verification

| Requirement group | Method |
|---|---|
| Data lifecycle (REQ-001 – REQ-006) | Unit and property tests on the aggregates; Marten and 10-provider integration tests for blocking; reference scenarios S14, S16 and S23 |
| Access transparency (REQ-007 – REQ-009) | Read-audit integration tests on the 10 providers and Marten; S13, S15 |
| Legal modelling (REQ-010 – REQ-014, REQ-059) | Unit and contract tests on the models and behaviours; RoPA export snapshot; S12, S17, S18 |
| Identity and context (REQ-015, REQ-016) | ASP.NET Core, Blazor Server and Hangfire integration tests; outbox, inbox and scheduler dispatch tests on the 10 providers; S0, S11 |
| Messaging (REQ-017, REQ-018, REQ-031 – REQ-034) | Provider integration tests with fault injection; S1, S2, S4, S21 |
| Security defaults (REQ-019 – REQ-021) | Unit tests of default options; S0, S20 |
| Documentation and claims (REQ-022 – REQ-025, REQ-060) | Review against the article tables; README type-name check; SPEC-001 citation gate; law-map date check at each tag; unit test over the `ProhibitedPractice` members |
| Supply chain (REQ-026 – REQ-030) | Release artifacts of `1.0.0-rc.1`; SECURITY.md review; evidence report of SPEC-000 REQ-022 |
| Shape changes, persistence and acceptance (REQ-035 – REQ-038) | PublicAPI files; S5, S19, S22; CI Full run of the reference scenario and compilation of the S15 rules skeleton |
| Integration and platform capabilities (REQ-039 – REQ-058) | The per-capability tests of AC-039, on the 10 providers where the capability has a store; S1, S3, S6 – S12, S14, S18, S20, S23 |
| Multi-tenancy (REQ-061) | Two-tenant integration tests on the 10 providers and Marten; key, sequence and credential isolation tests; S11 |
| Telemetry (REQ-062) | In-memory exporter tests per capability; `EncinaEventIdAllocationTests`; health-check tests; ADR-018 evaluation in every pull request |

## 11. Decisions for the maintainer

Class C decisions (`AI-DEVELOPMENT-MODEL.md` §14). Agents present the options and a recommendation; the maintainer decides. Once decided, each becomes a line in this table or a short ADR, and the specification can move to APPROVED.

| ID | Decision | Options | Recommendation | Consequences | Status |
|---|---|---|---|---|---|
| **DEC-001** | Tier of the reference application | (a) Tier A: practice management without clinical data; (b) Tier B: clinical records as well | (b) | REQ-009 and REQ-011 (and S15) are in the 1.0 gate; Encina must let an application keep clinical records within the law. Other practices are interested, so multi-tenant use is expected (DEC-009), with each practice paying for the infrastructure that clinical data requires | **DECIDED** — maintainer, 2026-09-24: (b). No longer provisional; no cost study gates the tier |
| **DEC-002** | Verifactu-specific code in Encina | (a) none in 1.0, regulation-neutral primitives only; (b) a separate package or repository; (c) inside Encina | (a). P-24 records the boundary in an ADR: which primitives are regulation-neutral (sequence, chained log, outbox pacing) and why they do not make the publisher a component producer | Public Encina ships no Verifactu code. The Verifactu module belongs to the application, whose producer signs the *declaración responsable* and carries the producer liability (H-R08 – H-R11); the maintainer keeps that module outside the public repository, in the application or a private library. Encina avoids a possible component-producer exposure of €150,000 per year [S] | **DECIDED** — maintainer, 2026-09-24: (a) |
| **DEC-003** | Payments abstraction | (a) none; (b) an `Encina.Payments` package | (a): gateways are vendor-shaped (an API-style gateway against a signed-redirect gateway) and a common abstraction adds little | The application keeps its own gateway abstraction; Encina offers outbox, inbox, sagas, locks and webhook ingestion (REQ-036) | **DECIDED** — maintainer, 2026-09-24: (a) |
| **DEC-004** | First post-1.0 block | (a) `Encina.Http`, `Encina.Security.OAuth`, `Encina.Notifications`, plus ordered partitions (#469); (b) resume the existing post-1.0 milestone order; (c) no block | — | There is no post-1.0 block: those capabilities are in 1.0 (§5.11) | **SUPERSEDED** by DEC-011 and the maintainer decision of §5.11 (2026-09-23) |
| **DEC-005** | Scheduling slices and `PartitionKey` in 1.0 | (a) REQ-033 (the minimal slices of #146, #148, #150) and a nullable `PartitionKey` column now; (b) keep them post-1.0 | (a): schema and signature changes across 10 providers are free now and breaking later | P-20 and P-51 are P0; S4 is in the gate. The partition behaviour built on the column (REQ-042, #469) is also in 1.0 (§5.11) | **DECIDED** — maintainer, 2026-09-23: (a) |
| **DEC-006** | Fail-closed security defaults | (a) fail closed by default, with an explicit, logged opt-out (#1155, P-13); (b) keep fail-open defaults and add an opt-in | (a) | Anyone relying on a silent skip breaks; nobody is affected pre-1.0 | **DECIDED** — maintainer, 2026-09-24: (a) |
| **DEC-007** | Home of the reference scenario | (a) the Encina repository (`tests/Encina.ReferenceScenarios.PracticeManagement/`), run in CI Full on every pull request that CI Full covers; (b) the reference application's private repository, testing against published Encina packages; (c) the gate in Encina and the full application in its own repository | (a): the gate must run where the code changes, so that a pull request that breaks a scenario fails before it merges; under (b) a break is found only after a package is published, and the evidence lives in a private repository nobody else can inspect. (c) is (a) plus the application's own tests, which the maintainer runs privately | Under (a) CI Full grows by the scenario's run time, and the scenario is written from §4.1 with thin app-style adapters, copying no application code and naming no application, so it is public test code like any other. Under (b) the 1.0 gate depends on a private repository | **DECIDED** — maintainer, 2026-09-24: (a)/(c). A generic `PracticeManagement` reference scenario lives in the Encina repository and runs in CI Full (the 1.0 acceptance gate); the private application keeps its own tests later, in its own repository |
| **DEC-008** | Marten next to EF Core and Dapper; ADR-019 scope for 1.0 | (a) state that the nine event-sourced compliance modules are PostgreSQL/Marten-only in 1.0 and document EF/Dapper and Marten coexistence (REQ-037); (b) build relational stores for them before 1.0; (c) leave it unstated | (a): the reference scenario runs on PostgreSQL; (b) is large | SQL Server and MySQL users do not get Consent, DSR, Retention, Breach, DPIA or crypto-shredding in 1.0, and are told so | **DECIDED** — maintainer, 2026-09-24: (a) |
| **DEC-009** | Multi-tenancy in 1.0 | (a) single practice, per-professional scoping through ABAC, tenancy post-1.0; (b) multi-tenancy required in 1.0 for every capability SPEC-002 adds or changes | (b) | REQ-061, INV-008, AC-043, P-54 and S11 as a real two-tenant scenario. OAuth tokens, gateway credentials, webhook secrets, invoice-series sequences, chained logs, notification senders, storage, crypto-shredding and column-encryption keys, audit, consents, processor registers and retention policies are keyed per tenant. #737, #738, #739, #760, #798 and #596 move into 1.0; #125, #304, #326, #338 and #876 are linked. Retrofitting tenancy after 1.0 would break persisted shapes on the 10 providers and Marten | **DECIDED** — maintainer, 2026-09-24: (b), even if the first deployment runs one tenant |
| **DEC-010** | Keep Encina non-commercial | (a) keep: no price, no gating, no telemetry that sends data to the maintainer or the project; (b) a paid edition or paid support as a condition | (a) | (a) keeps Encina outside the CRA and the PLD. (b) makes the maintainer a CRA manufacturer (Art. 14 reporting at once, full CRA from 11 Dec 2027) and a PLD manufacturer. (a) concerns only data sent to the maintainer: Encina's OpenTelemetry tracing, metrics and structured logging, which export only where the application configures, are a design goal, enforced through code, issue templates, reviews, tests and documentation (REQ-062, INV-007) | **DECIDED** — maintainer, 2026-09-24: (a), with telemetry that works with excellence |
| **DEC-011** | **Pre-1.0 scope added by SPEC-002.** Which of the items SPEC-000 does not already require enter the 1.0 contract (the full list, its drivers and its cost are in §11.2) | (a) **full readiness**: every item of §11.2 table 1; (b) **lifecycle only**: the defects, REQ-001, REQ-004, REQ-005, REQ-015 persisted context and REQ-060, everything else post-1.0 with the gap documented; (c) **SPEC-000 as is**. Independent of (a)–(c): (d) the relational `IPersonalDataLocator` (REQ-054, P-45); (e) column-level encryption at rest (REQ-055, P-46) | (a) with (d) and (e) | Every row of §11.2 table 1 enters 1.0, and with the maintainer decision of §5.11 every capability of table 2 as well. Schema changes on the 10 database providers; Spanish national law enters the 1.0 contract; 1.0 arrives materially later, and the maintainer accepted that date on 2026-09-23. Approval needs the SPEC-000 amendment pull request of §11.2 (SPEC-000 INV-004, SPEC-000 INV-005) | **DECIDED** — maintainer, 2026-09-23: (a) + (d) + (e) |
| **DEC-012** | Omnibus draft provisions (#810–#816) | (a) ship off by default while COM(2025) 837 is not adopted (REQ-024); (b) ship them as defaults | (a), with the maintainer's rule: a draft provision whose later adoption would need many changes, interface breaks or edits in many places is implemented before 1.0, off by default. Assessment against the issue bodies (read on 2026-09-24): #810 adds a consent-refusal state and a `ConsentRefused` event (persisted consent shape); #811 adds exempt purposes to the purpose model and a `ConsentExemptionApplied` event; #814 adds a rejection-reason enum to the persisted DSR records; #815 adds AI-training values and safeguard metadata to the lawful-basis model. These four change shapes. #812 (an `IIncidentReportingAdapter` abstraction), #813 (one options property) and #816 (an `IDPIACriteriaProvider`) are additive and cheap. Build all seven before 1.0, off by default | All seven close before `1.0.0-rc.1` (AC-024). Clarifies SPEC-000 REQ-026; recorded in the same SPEC-000 amendment pull request as DEC-011 (§11.2) | **DECIDED** — maintainer, 2026-09-24: (a) with the rule above; all seven before 1.0, off by default |
| **DEC-013** | Tracking structure | (a) a new EPIC "EU regulatory readiness (SPEC-002)" with the P-issues as children; #880 and #881 kept as they are and linked as related; #873 cross-linked; (b) spread the children over #880, #881 and #873 | (a) | One place to follow SPEC-002; the scope of the existing EPICs does not change | **DECIDED** — maintainer, 2026-09-24: (a) |
| **DEC-014** | Milestone structure for the widened 1.0 | (a) keep "v0.14.0 — Hardening" for defects and debt in existing code, and add three pre-1.0 milestones: "v0.17.0 — Compliance Lifecycle", "v0.18.0 — Integration & Platform Capabilities" and "v0.20.0 — Reference Scenario"; items that already fit an existing pre-1.0 milestone stay there (Consent in v0.15.0, AI Act in v0.16.0, tests in v0.17.0, documentation in v0.18.0, release engineering in v0.19.0); (b) put everything into v0.14.0; (c) spread everything over the existing milestones v0.14.0 – v0.19.0 | (a): v0.14.0 would otherwise hold most of the 1.0 work and stop meaning "hardening"; three themed milestones can be planned and closed on their own. Open: their version numbers and where they sit relative to v0.15.0 and v0.16.0 in the SPEC-000 DEC-005 sequence, which the SPEC-000 amendment should fix | §13.2 uses the milestone names of (a). Under (b) or (c) only the Milestone column of §13.2 changes | **DECIDED** — maintainer, 2026-09-24: (a), as proposed. "v0.14.0 — Hardening" holds defects only; three new pre-1.0 milestones ("v0.17.0 — Compliance Lifecycle", "v0.18.0 — Integration & Platform Capabilities" and "v0.20.0 — Reference Scenario") are added; v0.15.0 and v0.16.0 are kept |
| **DEC-015** | Pre-existing post-1.0 regulation packages (#804–#808: DORA, eIDAS 2, Data Act, ENS, EHDS) | (a) keep them post-1.0, except what the reference application needs, which is in 1.0 as generic extension points (REQ-048, REQ-052); (b) bring them into 1.0 | (a): a micro practice is outside DORA, outside the ENS unless it serves the public sector, and exempt from the eIDAS 2 wallet-acceptance duty (Art. 5f(2)); the EHDS dates are 2029 and 2031, with implementing acts still due | The five packages stay in their post-1.0 milestone with the fact-correction comments of §13.1. An operator that offers the application to several practices may be a provider of data processing services under the Data Act (§12 question 12); the structured export of REQ-004 and S23 is the base, and #806 stays post-1.0 | **DECIDED** — maintainer, 2026-09-24: (a) |
| **DEC-016** | Channel satellites, webhook presets and storage providers shipped in 1.0 | (a) per category, the minimum that the reference scenario (§4.3) exercises plus one alternative; the rest post-1.0 as satellites; (b) a broad set per category; (c) abstractions only | (a). Notifications (REQ-041): e-mail through SMTP and through one HTTP mail API, one SMS provider, and the WhatsApp Cloud API. Webhook presets (REQ-036): an API-style timestamped HMAC scheme (Stripe-style), a signed-redirect scheme (Redsys-style), Meta (WhatsApp) and Google (calendar push notifications). Storage (REQ-046): the file system and one S3-compatible provider, plus Azure Blob, so that 1.0 covers AWS and Azure as the cloud rule of `CLAUDE.md` asks | Each satellite carries the full test matrix, tenancy (REQ-061) and telemetry (REQ-062); other providers follow after 1.0 without a change to the abstractions | **DECIDED** — maintainer, 2026-09-24: (a) |

### 11.1 Application decisions that do not block Encina

Open questions from the private analysis of the reference application and from note [H](research/SPEC-002/H-integrations-law.md). They are the reference application's to take; the Encina consequence, where there is one, is stated.

| Question | Options | Encina consequence |
|---|---|---|
| Scheduler for business messages | Encina scheduling, or Hangfire when its dashboard is essential | None; both are supported (Hangfire after #1152) |
| E-mail provider | A business mail suite through its API, an SMTP relay, or a transactional provider (a consumer account has no DPA, H-R33) | None; DEC-016 ships SMTP and one HTTP mail API |
| Payment gateway and invoice timing | An API-style card gateway or a signed-redirect bank gateway with native Bizum; invoice before or after payment; how session packs are invoiced | None (DEC-003) |
| Single or multiple instances | One host or several | None: row claiming (REQ-056, P-47) is in 1.0 either way |
| Verifactu approach | Own SIF in VERI\*FACTU mode; the AEAT's free application; a certified third party | None (DEC-002); informs P-24 |
| Producer and operator of the application | One operator hosts it for several practices (processor for each, §4.1), or each practitioner runs their own | None; decides who signs the *declaración responsable*, which processor contracts are needed, and whether the operator is a data-processing-service provider under the Data Act (§12 question 12) |
| Patient messaging channel | E-mail and SMS through EU processors first, WhatsApp Cloud API later | Informs REQ-014 test data; DEC-016 ships all three channels |
| LanguageExt v5 | Before or after 1.0 | An Encina-wide question outside this specification |
| Start before or after the post-1.0 rename | — | Starting before the rename means one namespace change in the application |

### 11.2 Scope decisions DEC-011 and §5.11 in detail

SPEC-000 DEC-002 fixed the 1.0 scope as the existing modules hardened plus EPICs #880 and #881. This specification places far more than that before 1.0: through the **Shape** rule of §5 (a persisted format, a public contract or a security default cannot change for free after 1.0), through DEC-011, through the maintainer decision of §5.11 and through DEC-009. The maintainer decided the whole scope at once: DEC-011 (a) + (d) + (e) and §5.11 on 2026-09-23, DEC-009 (b) and DEC-012 on 2026-09-24. This section lists every item so that the SPEC-000 amendment can cite it.

**Table 1 — items SPEC-002 places before 1.0 that SPEC-000 does not already require (DEC-011 and DEC-009)**

| # | Item | REQ | Why before 1.0 | Tracking | Decided through |
|---|---|---|---|---|---|
| 1 | Retention floor, event-anchored start, policies per jurisdiction, document type and category, immutable-record class | REQ-001 | Shape: retention events and policy model | P-01 | DEC-011 (a) |
| 2 | DSR erasure arbitration with a reasoned partial refusal | REQ-004 | Defect and shape: the DSR result contract | P-02 | DEC-011 (a) |
| 3 | Blocked data state (LOPDGDD art. 32) | REQ-005 | Shape: a new data state on the 10 providers and Marten | P-03 | DEC-011 (a) |
| 4 | Durable, KMS-wrappable subject key store by default | REQ-006 | Security default | P-04 | DEC-011 (a) |
| 5 | Evidential read audit | REQ-007 | Shape: audit entry and store | P-05 | DEC-011 (a) |
| 6 | Query-level read audit | REQ-008 | Capability: an application that reads through query handlers has no access audit without it | P-06 | DEC-011 (a) |
| 7 | Art. 9(2) condition and national legal basis | REQ-010 | Shape: RoPA, `LawfulBasis`, processing activities | P-08 | DEC-011 (a) |
| 8 | Processor role per vendor and purpose | REQ-013 | Shape: processor register | P-10 | DEC-011 (a) |
| 9 | Consent per purpose and channel, LSSI existing-client basis, transactional marker | REQ-014 | Shape: consent records | P-11 | DEC-011 (a) |
| 10 | Request context persisted with outbox, inbox and scheduled messages | REQ-015 | Shape: schema on the 10 providers | P-50 | DEC-011 (a) |
| 11 | Purge of processed outbox and executed scheduled messages | REQ-018 | Storage limitation; new store operation on the 10 providers | P-12 | DEC-011 (a) |
| 12 | PII masking fail-closed and role-aware redaction | REQ-019 | Security default | P-13 | DEC-011 (a), DEC-006 (a) |
| 13 | `EncryptedField` consumed as the marker of column encryption | REQ-020 | A public attribute that does nothing | #857, pulled forward from "Post-1.0: Critical Bugs & Quality Debt (deferred items)" | DEC-011 (a), (e) |
| 14 | National-law rows in the 15 package specifications | REQ-022 | SPEC-000 REQ-024 asks for EU articles only | P-17 (15 issues) | DEC-011 (a) |
| 15 | README, ADR-019 and integration-test honesty; PostgreSQL/Marten requirement stated | REQ-023, REQ-037 | Documentation claims | P-14, P-15, P-16 | DEC-011 (a), DEC-008 (a) |
| 16 | Law-map re-verification before each tag | REQ-025 | Process | #104 (comment) | DEC-011 (a) |
| 17 | Legal-status statement and SECURITY.md content (support period, targets, scope, CRA Art. 13(6) channel, primitives) | REQ-026, REQ-028 | Lets integrators meet their CRA duties | P-27 | DEC-011 (a), DEC-010 (a) |
| 18 | Per-package SBOM on every GitHub Release | REQ-027 | Same | P-26 | DEC-011 (a) |
| 19 | GHSA with a CVE for every security fix | REQ-029 | Same | P-28 | DEC-011 (a) |
| 20 | Trusted Publishing, package-ID prefix, OpenSSF Scorecard | REQ-030 | Same | P-29 | DEC-011 (a) |
| 21 | Outbox id as idempotency key and per-handler completion | REQ-031 | Shape: dispatch contract and a new table on the 10 providers | P-18 | DEC-011 (a) |
| 22 | Stable persisted message type names and payload versioning | REQ-032 | Shape: persisted wire format | P-19 | DEC-011 (a) |
| 23 | Outbound error taxonomy and Retry-After-aware retry | REQ-034 | Shape: error codes | P-21 | DEC-011 (a) |
| 24 | `AddApplicationMessaging` alias removed | REQ-035 | Shape: public API | P-22 | DEC-011 (a) |
| 25 | Provider-neutral webhook ingestion core | REQ-036 | Widens #731 and #734, already in v0.14.0 | #731, #734 (comment) | DEC-011 (a) |
| 26 | EF Core or Dapper and Marten consistency pattern | REQ-037 | Documentation and test | P-23 | DEC-011 (a), DEC-008 (a) |
| 27 | PracticeManagement reference scenario | REQ-038 | Acceptance gate | P-25 | DEC-011 (a) |
| 28 | Complete AI Act Art. 5 practice catalogue; no over-claim | REQ-060 | Defect in a claim and a public enum | P-53 | DEC-011 (a) |
| 29 | ADR on the Verifactu boundary: which primitives are regulation-neutral | DEC-002 | Records the boundary before any sequence or chain primitive ships | P-24 | DEC-002 (a) |
| 30 | `CLAUDE.md` names a package that does not exist | SPEC-000 REQ-001 | Documentation | P-30 | DEC-011 (a) |
| 31 | DSR access export with exclusions and reasons | REQ-009 | Shape | P-07 | DEC-011 (a), DEC-001 (b) |
| 32 | Representation and deceased status (shape only) | REQ-011 | Shape | P-09 | DEC-011 (a), DEC-001 (b) |
| 33 | Consent given for a minor | REQ-059 | Shape | P-52 | DEC-011 (a) |
| 34 | Scheduling minimal slices | REQ-033 | Shape: schema and signatures on the 10 providers | P-20 | DEC-005 (a) |
| 35 | Nullable outbox `PartitionKey` column | REQ-033 | Shape: schema on the 10 providers | P-51 | DEC-005 (a) |
| 36 | Relational `IPersonalDataLocator` | REQ-054 | Without it the application writes its own locator for REQ-004, REQ-005 and S23 | P-45 | DEC-011 (d) |
| 37 | Column-level encryption at rest | REQ-055 | GDPR Art. 32 at rest for EF Core and Dapper data; S20 | P-46 | DEC-011 (e) |
| 38 | Multi-tenancy of the SPEC-002 capabilities | REQ-061 | Shape: a tenant key in persisted records, sequences, key stores and credential stores on the 10 providers and Marten | P-54; #737, #738, #739, #760 (tenant column in the messaging entities), #798 (audit and ABAC), #596 (persistent tenant store), pulled forward from "Post-1.0: Multi-Tenancy Core" and "Post-1.0: Compliance Completion & Test Coverage (deferred items)" | DEC-009 (b) |
| 39 | Telemetry of the SPEC-002 capabilities | REQ-062 | Process and shape: activity names, metric names and EventIds are public contract | criterion in every P-issue and in P-00 | DEC-010 (a) |

**Table 2 — integration and platform capabilities placed in 1.0 by the maintainer decision of §5.11** (REQ-054 and REQ-055 are rows 36 and 37 above)

| # | Capability | REQ | Tracking | New store on the 10 providers |
|---|---|---|---|---|
| 40 | `Encina.Http` | REQ-039 | P-31 | — |
| 41 | `Encina.Security.OAuth` | REQ-040 | P-32 | token store |
| 42 | `Encina.Notifications` and the channel satellites of DEC-016 | REQ-041 | P-33 | delivery status |
| 43 | Ordered outbox partitions with pacing | REQ-042 | #469, moved from "Post-1.0: AI/LLM Integration" | — (column of row 35) |
| 44 | Domain events to the outbox in the same commit | REQ-043 | P-34 | — |
| 45 | Saga correlation by external key and wait-for-event | REQ-044 | P-35 | saga correlation columns |
| 46 | `ISequenceGenerator` | REQ-045 | P-36 | sequence table |
| 47 | `Encina.Storage` and the storage providers of DEC-016 | REQ-046 | P-37 | — (blob providers) |
| 48 | Persistent hash-chained log | REQ-047 | P-38 | chained-log store |
| 49 | Trusted time-stamping and e-signature integration points | REQ-048 | P-39 | — |
| 50 | Breach notifier channels | REQ-049 | P-40 | — (Marten) |
| 51 | DPIA screening against the AEPD lists | REQ-050 | P-41 | — |
| 52 | Break-the-glass access | REQ-051 | P-42 | — (audit) |
| 53 | EHDS logging and export extension points | REQ-052 | P-43 | — |
| 54 | X.509 certificate retrieval | REQ-053 | P-44 | — |
| 55 | Row claiming in outbox and scheduler processors | REQ-056 | P-47 | — (query change) |
| 56 | ISO/IEC 27001 and 27701 control mappings | REQ-057 | P-48 | — |
| 57 | VEX statements | REQ-058 | P-49 | — |

Counts, with every row of both tables: **P0 proposed issues**: 47 P-ids without condition (61 issues, since P-17 stands for 15), plus P-52 if the practice treats minors: up to 48 P-ids and 62 issues. **P1 proposed issues**: 6 (P-24, P-26 – P-30). **Existing issues moved into pre-1.0 milestones**: 25, namely the 17 without a milestone (#1142 – #1155, #1158, #1160, #1161), #857 and #469 from post-1.0 milestones, and the six tenancy issues of row 38. #731, #734 and #104 stay where they are and gain comments.

**Already required by SPEC-000, so not added by these decisions.** The `[BUG]` issues among #1142 – #1161 (SPEC-000 REQ-011 requires every open bug to be fixed or explicitly deferred), including the count and requeue of dead-lettered outbox messages that the fix of #1150 adds (Option A, REQ-017); the `[DEBT]` issues #1146, #1154, #1155 and #1158 are not covered by SPEC-000 REQ-011, so moving them into v0.14.0 (counted above) is part of DEC-011; #860 (REQ-021, already in v0.14.0); the EU article tables of REQ-022 (SPEC-000 REQ-024); REQ-024 and the seven Omnibus issues #810 – #816, already in "v0.15.0 — EU Compliance: NIS2 & Digital Omnibus" (SPEC-000 REQ-026, clarified by DEC-012); the AI Act EPIC and its multi-tenancy child #845 (SPEC-000 REQ-025); the SBOM in the evidence report (SPEC-000 REQ-022); attestations and signing (SPEC-000 REQ-018); reserved package identifiers (SPEC-000 REQ-019); a SECURITY.md that describes the actual process (SPEC-000 REQ-021).

**Schema work on the 10 database providers.** Six families of change drive most of the cost, each a migration plus integration tests on ADO.NET, Dapper and EF Core for SQL Server, PostgreSQL and MySQL, and on MongoDB:

- REQ-015: context columns (actor, tenant, correlation id, causation id) in the outbox, inbox and scheduled-message tables;
- REQ-018: purge operations, with an index on the processed or executed timestamp, in the outbox and scheduled-message stores;
- REQ-031: a per-handler completion table keyed by outbox id and handler type;
- REQ-033 (DEC-005 (a)): schedule key and time-zone columns in the scheduled-message tables;
- the outbox `PartitionKey` column (DEC-005 (a), P-51);
- REQ-061 (DEC-009 (b)): a tenant column, with its indexes and filters, in every store that SPEC-002 adds or changes, the saga table included.

Table 2 adds four stores on the 10 providers (the OAuth token store, notification delivery status, the sequence table and the chained-log store) and saga correlation columns. Smaller store changes come with REQ-005 (blocked state for data reached through Encina repositories), REQ-007 (subject id and data category in the read-audit stores) and REQ-032 (registry names and versions in the type columns).

**Spanish national law enters the 1.0 contract.** SPEC-000 §2 names GDPR and the Digital Omnibus, ePrivacy, NIS2 and the AI Act. With DEC-011 (a), 1.0 also carries requirements whose acceptance case is Spanish law: LOPDGDD art. 7 (REQ-059), art. 9.2 and DA 17ª (REQ-010, as a reference field) and art. 32 (REQ-005); Ley 41/2002 arts. 17.1 (REQ-001), 18.3 (REQ-009) and 18.4 (REQ-011); LSSI arts. 20–22 (REQ-014); and the national rows of REQ-022. The designs stay jurisdiction-neutral (policies per jurisdiction, "restriction with statutory release roles"); Spanish law is the acceptance case, not hard-coded behaviour.

**Consequence for the 1.0 date.** SPEC-000 §2 gives 1.0 no target date, so nothing slips on paper, but the 1.0 sequence grows by up to 62 P0 and 6 P1 new issues, 25 moved issues, six 10-provider schema families and four new stores. 1.0 arrives materially later than under SPEC-000 alone; **the maintainer accepted that later date on 2026-09-23**. The reference application starts when Encina reaches 1.0, so it starts later with it, but on a framework that it does not have to bypass for blocking, erasure arbitration, channel consent, access audit, tenancy or integrations. DEC-014 proposes how to split the work into milestones.

**Approval path.** SPEC-000 INV-004 lets the P0 backlog grow only with a reason that references a SPEC-000 requirement, and SPEC-000 INV-005 lets no agent change SPEC-000 except through a pull request the maintainer approves. The decisions of 2026-09-23 and 2026-09-24 therefore still need a **SPEC-000 amendment pull request** that:

1. extends SPEC-000 §2 to the national law of the reference application's member state where SPEC-002 turns it into a requirement;
2. adds a SPEC-000 requirement (next free identifier) stating that the pre-1.0 scope decided in SPEC-002 (DEC-009, DEC-011 and §5.11, tables 1 and 2 above) is implemented before 1.0, so that each P0 addition can cite it as SPEC-000 INV-004 asks;
3. narrows SPEC-000 §6 (non-goals), which excludes new integrations and new packages from 1.0, so that it no longer excludes the capabilities of §5.11, the channel satellites and storage providers of DEC-016, and the multi-tenancy of REQ-061;
4. records the DEC-012 clarification of SPEC-000 REQ-026 (all seven Omnibus adaptations built before 1.0, off by default);
5. places the milestones of DEC-014 in the SPEC-000 DEC-005 sequence, once DEC-014 is decided.

SPEC-002 moves to APPROVED only after that pull request is merged (AC-040).

## 12. Open questions

1. *(Resolved: yes, the practice treats minors (maintainer, 2026-09-24). REQ-059 and its tracking P-52 are unconditional pre-1.0.)*
2. *(Resolved: DEC-001 was decided (b) on 2026-09-24; no cost study gates the tier.)*
3. AEPD PD-00068-2026 was read through a law-firm summary [S]; the original resolution should be read before REQ-007 is finalised.
4. EHDS scope for an in-house psychology EHR: whether psychology notes carry priority-category data depends on implementing acts due by 26 Mar 2027.
5. Limitation periods that end the blocked state (Código Civil art. 1964.2, 5 years; LOPDGDD art. 72 ff.) are [K]; regional retention periods (Catalonia and others) are [S].
6. Tax retention for a liberal professional: LGT 4 years only, or Código de Comercio art. 30 (6 years) as well [S].
7. Verifactu: whether a free, independently versioned library implementing RRSIF functions needs its own *declaración responsable* (note [H](research/SPEC-002/H-integrations-law.md) U1); whether an application forwarding billing data to an external SIF is itself a component (U3); the exact wording of LGT art. 201 bis (U2).
8. *(Resolved: §13.2 P-17 tracks the 15 REQ-022 specifications as 15 issues, one per package, so that each package's pull request closes its own issue.)*
9. No Spanish PLD transposition bill and no CRA penalty law were found; the searches may have missed a recent *anteproyecto*.
10. The DSA size thresholds were not re-verified [K]; irrelevant unless the application hosts user content.
11. The DPF certification status of the US vendors named in note [H](research/SPEC-002/H-integrations-law.md) was not checked; it affects S17 test data, not REQ-012.
12. With several practices served by one operator (DEC-009), whether that operator is a provider of data processing services under the Data Act (Art. 2(8); switching duties in Chapter VI) [K], and how its switching and export duties relate to the structured export of REQ-004 and to #806 (post-1.0, DEC-015).

## 13. Tracking plan

### 13.1 EPIC

**P-00** `[EPIC] EU regulatory readiness (SPEC-002)`, parent of every P-issue below and of the existing issues listed in §13.3 that have no EPIC. No milestone (it spans every pre-1.0 milestone of DEC-014). Its body carries the REQ-062 checklist (ADR-018 evaluation, tracing, metrics, logging, health checks, no personal data in telemetry) that every child issue repeats. Relation to existing EPICs (DEC-013):

- **#880** ("v0.15.0 — EU Compliance: NIS2 & Digital Omnibus") stays as it is under SPEC-000 REQ-026 and is linked as related. P-11 and P-52 live in that milestone next to #810 and #811, and REQ-024 adds one acceptance criterion to each of #810–#816 (comment, no new issue).
- **#881** ("v0.16.0 — AI Act") stays as it is under SPEC-000 REQ-025 and is linked as related; P-53 lives in its milestone. A comment asks #836–#847 to use the dates of Reg. (EU) 2026/1744: Art. 4 replaced, Art. 5(1)(ba)/(bb) from 2 Dec 2026, Art. 50(2) grace to 2 Dec 2026, Annex III from 2 Dec 2027, Annex I from 2 Aug 2028.
- **#873** (Compliance Completion and Test Coverage, in "v0.14.0 — Hardening") is cross-linked from P-15, P-16 and P-17.
- **#876** ("Post-1.0: Multi-Tenancy Core") stays post-1.0 for the rest of its scope and is linked as related; the tenancy issues that SPEC-002 needs (#737, #738, #739, #760, #798, #596) move into 1.0 under DEC-009 and become children of P-00 as well.
- **#804–#808** ("Post-1.0: EU Regulatory Compliance: DORA, eIDAS2, Data Act, ENS, EHDS (deferred items)") stay post-1.0 (DEC-015). Comments correct their facts: #804 (RTS 2025/301, RTS 2025/1190), #805 (24 Dec 2027, micro and small exemption), #806 (DGA repeal proposed; Art. 29 and Chapter IV dates), #807 (73 measures, five dimensions, in force 5 May 2022), #808 (in force 25 Mar 2025; 2029 and 2031 dates; relation to P-43).

Priorities use the P0 – P3 scale of `ENCINA-1.0-RECONCILIATION.md` §4.

### 13.2 New issues proposed (not opened)

All milestones are named exactly as on GitHub. The three DEC-014 milestones were created on 2026-09-24 as "v0.17.0 — Compliance Lifecycle", "v0.18.0 — Integration & Platform Capabilities" and "v0.20.0 — Reference Scenario", and the pre-existing "v0.17.0 — Providers & Testing", "v0.18.0 — Documentation" and "v0.19.0 — Release Engineering" were renumbered to "v0.19.0 — Providers & Testing", "v0.21.0 — Documentation" and "v0.22.0 — Release Engineering" to make room. Every P-issue is pre-1.0; each carries the tenancy (REQ-061) and telemetry (REQ-062) criteria of P-00.

| P-id | Proposed title | Type | Priority | Milestone | REQ |
|---|---|---|---|---|---|
| P-01 | Retention: minimum-retention floor and event-anchored start (episode discharge, fiscal year, death), per jurisdiction, document type and data category, immutable-record class | `[FEATURE]` | P0 | v0.17.0 — Compliance Lifecycle | REQ-001 |
| P-02 | DSR erasure ignores retention policies and legal holds; return a reasoned partial refusal (Art. 17(3)) with a grantable-after date (related #814) | `[BUG]` | P0 | v0.14.0 — Hardening | REQ-004 |
| P-03 | Blocked data state (LOPDGDD art. 32 *bloqueo*): hidden from processing and viewing, audited authority disclosure, art. 32.4 secure copy, scheduled destruction, blocking strategy on the 10 providers | `[FEATURE]` | P0 | v0.17.0 — Compliance Lifecycle | REQ-005 |
| P-04 | Marten.GDPR subject key store defaults to in-memory and keeps keys unwrapped | `[DEBT]` | P0 | v0.14.0 — Hardening | REQ-006 |
| P-05 | Evidential read audit: fail-closed option, ids for collection reads, enforced purpose, retention per data category (3-year recommended default for special categories), per-subject access query | `[FEATURE]` | P0 | v0.17.0 — Compliance Lifecycle | REQ-007 |
| P-06 | Query-level read audit with data-subject resolution for reads outside Encina repositories | `[FEATURE]` | P0 | v0.17.0 — Compliance Lifecycle | REQ-008 |
| P-07 | DSR access export with field-level exclusions and withholding reasons | `[FEATURE]` | P0 | v0.17.0 — Compliance Lifecycle | REQ-009 |
| P-08 | Model GDPR Art. 9(2) conditions and national legal bases next to the Art. 6 basis (RoPA, GDPR `LawfulBasis`, `Encina.Compliance.LawfulBasis`, processing activities) | `[FEATURE]` | P0 | v0.17.0 — Compliance Lifecycle | REQ-010 |
| P-09 | Data-subject representation (guardians, proxies) and deceased status in Consent and DSR records: persisted shape | `[FEATURE]` | P0 | v0.17.0 — Compliance Lifecycle | REQ-011 |
| P-10 | ProcessorAgreements: vendor role per purpose and "no Art. 28 terms available" status | `[FEATURE]` | P0 | v0.17.0 — Compliance Lifecycle | REQ-013 |
| P-11 | Consent: channel-scoped consent, LSSI art. 21.2 existing-client basis with per-message opt-out, transactional or commercial marker | `[FEATURE]` | P0 | v0.15.0 — EU Compliance: NIS2 & Digital Omnibus | REQ-014 |
| P-12 | Outbox and scheduled messages: purge processed messages after a configurable retention period (30-day default) on all 10 providers | `[FEATURE]` | P0 | v0.18.0 — Integration & Platform Capabilities | REQ-018 |
| P-13 | PIIMaskingPipelineBehavior returns the unmasked response when masking fails; add fail-closed and role- or purpose-aware redaction | `[DEBT]` | P0 | v0.14.0 — Hardening | REQ-019 |
| P-14 | Compliance READMEs and ADR-019 describe types and provider coverage that do not exist; state the PostgreSQL/Marten requirement | `[DEBT]` | P0 | v0.14.0 — Hardening | REQ-023, REQ-037 |
| P-15 | Missing READMEs for Anonymization, PrivacyByDesign, CrossBorderTransfer and Security.Audit | `[DEBT]` | P0 | v0.21.0 — Documentation | REQ-023 |
| P-16 | Integration tests or written justification for AIAct, Attestation, DataSubjectRights and GDPR | `[TEST]` | P0 | v0.19.0 — Providers & Testing | REQ-023 |
| P-17 | Article-coverage specification for `Encina.Compliance.<Package>`, EU and national articles (one issue per package, 15 in total) | `[DEBT]` | P0 | v0.21.0 — Documentation | REQ-022 |
| P-18 | Outbox dispatch: outbox id as idempotency key, per-handler completion on the 10 providers, aggregated handler errors | `[FEATURE]` | P0 | v0.18.0 — Integration & Platform Capabilities | REQ-031 |
| P-19 | Stable persisted message type names and payload versioning for outbox, inbox and scheduled messages | `[FEATURE]` | P0 | v0.18.0 — Integration & Platform Capabilities | REQ-032 |
| P-20 | Scheduling minimal slices: schedule key, time-zone-aware cron, `ICronParser`, orphan `IMessageScheduler` | `[FEATURE]` | P0 | v0.18.0 — Integration & Platform Capabilities | REQ-033 |
| P-21 | Outbound error taxonomy and Retry-After-aware retry classification | `[DEBT]` | P0 | v0.14.0 — Hardening | REQ-034 |
| P-22 | Remove the legacy `AddApplicationMessaging` alias | `[DEBT]` | P0 | v0.14.0 — Hardening | REQ-035 |
| P-23 | Consistency between EF Core or Dapper application data and Marten compliance aggregates on one PostgreSQL | `[SPIKE]` | P0 | v0.17.0 — Compliance Lifecycle | REQ-037 |
| P-24 | ADR on the Verifactu boundary: the regulation-neutral primitives Encina ships (sequence, chained log, outbox pacing) and why they do not make its publisher a component producer | `[SPIKE]` | P1 | v0.14.0 — Hardening | DEC-002 |
| P-25 | PracticeManagement reference scenario: S0 – S23 with tenancy on, and the S15 rules skeleton | `[TEST]` | P0 | v0.20.0 — Reference Scenario | REQ-038 |
| P-26 | Per-package SBOM attached to every GitHub Release | `[INFRA]` | P1 | v0.22.0 — Release Engineering | REQ-027 |
| P-27 | SECURITY.md: support period, response targets, scope, CRA Art. 13(6) channel, legal-status statement, cryptographic primitives | `[INFRA]` | P1 | v0.22.0 — Release Engineering | REQ-026, REQ-028 |
| P-28 | Security advisory process: GHSA with CVE for every fix, security history in the changelog | `[INFRA]` | P1 | v0.22.0 — Release Engineering | REQ-029 |
| P-29 | nuget.org Trusted Publishing, package-ID prefix and OpenSSF Scorecard | `[INFRA]` | P1 | v0.22.0 — Release Engineering | REQ-030 |
| P-30 | `CLAUDE.md` lists `Encina.Extensions.Http.Resilience`, which does not exist | `[DEBT]` | P1 | v0.14.0 — Hardening | SPEC-000 REQ-001 |
| P-31 | `Encina.Http`: outbound HTTP client with auth providers, client certificates, rate limiting and coded errors | `[FEATURE]` | P0 | v0.18.0 — Integration & Platform Capabilities | REQ-039 |
| P-32 | `Encina.Security.OAuth`: token store, single-flight refresh, revoked-grant notification | `[FEATURE]` | P0 | v0.18.0 — Integration & Platform Capabilities | REQ-040 |
| P-33 | `Encina.Notifications` and the channel satellites of DEC-016 | `[FEATURE]` | P0 | v0.18.0 — Integration & Platform Capabilities | REQ-041 |
| P-34 | Domain events to the outbox in the same commit on all 10 providers | `[FEATURE]` | P0 | v0.18.0 — Integration & Platform Capabilities | REQ-043 |
| P-35 | Saga correlation by external key and wait-for-event step | `[FEATURE]` | P0 | v0.18.0 — Integration & Platform Capabilities | REQ-044 |
| P-36 | `ISequenceGenerator`: gapless sequence per key on all 10 providers | `[FEATURE]` | P0 | v0.18.0 — Integration & Platform Capabilities | REQ-045 |
| P-37 | `Encina.Storage` (`IBlobStore`) with encryption, retention, hold and blocking hooks, and the storage providers of DEC-016 | `[FEATURE]` | P0 | v0.18.0 — Integration & Platform Capabilities | REQ-046 |
| P-38 | Persistent hash-chained log store, with Attestation built on it | `[FEATURE]` | P0 | v0.17.0 — Compliance Lifecycle | REQ-047 |
| P-39 | Trusted time-stamping and e-signature integration points | `[FEATURE]` | P0 | v0.17.0 — Compliance Lifecycle | REQ-048 |
| P-40 | Breach notifier channels: Art. 33(3) package and Art. 34 communications | `[FEATURE]` | P0 | v0.17.0 — Compliance Lifecycle | REQ-049 |
| P-41 | DPIA screening mapped to the AEPD Art. 35.4 and 35.5 lists | `[FEATURE]` | P0 | v0.17.0 — Compliance Lifecycle | REQ-050 |
| P-42 | Break-the-glass access with justification, time box and review | `[FEATURE]` | P0 | v0.17.0 — Compliance Lifecycle | REQ-051 |
| P-43 | EHDS readiness: logging component and EEHRxF export as extension points | `[SPIKE]` | P0 | v0.17.0 — Compliance Lifecycle | REQ-052 |
| P-44 | X.509 certificate retrieval in `Encina.Security.Secrets` | `[FEATURE]` | P0 | v0.18.0 — Integration & Platform Capabilities | REQ-053 |
| P-45 | Relational `IPersonalDataLocator` on all 10 providers | `[FEATURE]` | P0 | v0.17.0 — Compliance Lifecycle | REQ-054 |
| P-46 | Column-level encryption at rest through ORM value converters on all 10 providers | `[FEATURE]` | P0 | v0.17.0 — Compliance Lifecycle | REQ-055 |
| P-47 | Row claiming in outbox and scheduler processors for multi-instance hosts | `[FEATURE]` | P0 | v0.18.0 — Integration & Platform Capabilities | REQ-056 |
| P-48 | ISO/IEC 27001 and 27701 control-mapping documents | `[FEATURE]` | P0 | v0.21.0 — Documentation | REQ-057 |
| P-49 | VEX statements for dependency vulnerabilities | `[INFRA]` | P0 | v0.22.0 — Release Engineering | REQ-058 |
| P-50 | Persist the request context (actor, tenant, correlation and causation ids) with outbox, inbox and scheduled messages and rebuild it at dispatch, on all 10 providers (with #737, #739, #760) | `[FEATURE]` | P0 | v0.18.0 — Integration & Platform Capabilities | REQ-015 |
| P-51 | Outbox: nullable `PartitionKey` column on all 10 providers (ordering behaviour is #469) | `[FEATURE]` | P0 | v0.18.0 — Integration & Platform Capabilities | REQ-033, REQ-042 |
| P-52 | Consent given for a minor: age of digital consent per jurisdiction (14 in Spain) and the holder of parental authority | `[FEATURE]` | P0 | v0.15.0 — EU Compliance: NIS2 & Digital Omnibus | REQ-059 |
| P-53 | AIAct `ProhibitedPractice` lacks Art. 5(1)(b) and the new (ba)/(bb), while the package claims to block prohibited practices unconditionally | `[BUG]` | P0 | v0.16.0 — AI Act | REQ-060 |
| P-54 | Multi-tenancy of the SPEC-002 capabilities: tenant-scoped subject and column-encryption keys, per-tenant credentials, sequences, senders and storage, tenant-aware background cycles, two-tenant isolation tests on the 10 providers and Marten | `[FEATURE]` | P0 | v0.18.0 — Integration & Platform Capabilities | REQ-061 |

### 13.3 Existing issues to link

"In progress" names open pull requests or branches on 2026-09-23; they do not change the proposed milestone.

| Issue | Subject | Milestone today | Proposed | In progress | REQ |
|---|---|---|---|---|---|
| #1142, #1143, #1146 | Retention sweep, fail-open hold, `UtcNow` | none | v0.14.0 — Hardening | PR #1157 | REQ-002, REQ-003 |
| #1158 | Retention enforcement follow-ups: double-counted metrics, skipped alerts, re-erasure after a failed `MarkDeleted`, no cycle lock, stale docs | none | v0.14.0 — Hardening | — | REQ-003 |
| #1160 | Retention enforcement erases every data category of an entity | none | v0.14.0 — Hardening | — | REQ-001, REQ-003 |
| #1161 | `LiftHoldAsync` reports success when releasing fails; other-holds check fails open | none | v0.14.0 — Hardening | — | REQ-002 |
| #1144 | Crypto-shredding deletes every key of the subject | none | v0.14.0 — Hardening | — | REQ-006 |
| #1145 | US adequacy | none | v0.14.0 — Hardening | — | REQ-012 |
| #1147 | Empty request context in pipeline behaviours | none | v0.14.0 — Hardening | branch `fix/request-context-1147` (ambient accessor and explicit overloads; persisted context is P-50) | REQ-015 |
| #1148 | Blazor Server circuits denied | none | v0.14.0 — Hardening | — | REQ-015 |
| #1149 | `Guid` subject ids ignored | none | v0.14.0 — Hardening | PR #1159 | REQ-016 |
| #1150, #1151, #1154 | Outbox abandonment (#1150: Option A decided, dead letters stay in the outbox table with count and requeue on the 10 stores), `Left` treated as processed, no MongoDB processor | none | v0.14.0 — Hardening | — | REQ-017 |
| #1152, #1153 | Hangfire jobs never fail on `Left`; recurring insert failure ignored | none | v0.14.0 — Hardening | PR #1159 | REQ-017 |
| #1155 | HMAC validation fails open | none | v0.14.0 — Hardening | — | REQ-019 |
| #1128, #1129 | Write-audit store defects (EF Core; ADO.NET and Dapper PostgreSQL) | v0.14.0 — Hardening | unchanged | — | REQ-007 prerequisites |
| #1135 | `AuditStoreEF` and `ReadAuditStoreEF` let provider exceptions escape (write- and read-audit stores) | v0.14.0 — Hardening | unchanged | — | REQ-007 prerequisite |
| #770, #767 | Retention and read-audit retention services to Encina scheduling | v0.14.0 — Hardening | unchanged | — | REQ-003, REQ-007 |
| #860 | Attestation deferred review items | v0.14.0 — Hardening | unchanged | — | REQ-021 |
| #857 | Orphan attributes, `EncryptedField` among them | Post-1.0: Critical Bugs & Quality Debt (deferred items) | v0.14.0 — Hardening | — | REQ-020 |
| #858 | PII hash helper is not an HMAC | v0.14.0 — Hardening | unchanged | — | REQ-019 |
| #751 | Audit trail for ABAC decisions | v0.14.0 — Hardening | unchanged | — | REQ-007 (related) |
| #731, #734 | Inbound webhook validation and idempotency | v0.14.0 — Hardening | unchanged; add the REQ-036 requirements as a comment | — | REQ-036 |
| #733 | Idempotency for notification processing | v0.14.0 — Hardening | unchanged | — | REQ-031 (related) |
| #689, #1090 | Compliance documentation hub; unbacked claims | v0.14.0 — Hardening | unchanged | — | REQ-022, REQ-023 |
| #810 – #813, #815 | Omnibus adaptations | v0.15.0 — EU Compliance: NIS2 & Digital Omnibus | unchanged; all built before 1.0, off by default (DEC-012); add the REQ-024 criterion | — | REQ-024 |
| #814, #816 | DSAR refusal reasons; DPIA criteria loader (Omnibus) | v0.15.0 — EU Compliance: NIS2 & Digital Omnibus | unchanged; built before 1.0, off by default (DEC-012); add the AC-024 wording for capabilities current law allows; #814 linked from P-02 | — | REQ-024, REQ-004 (for #814) |
| #836 – #847 | AI Act children | v0.16.0 — AI Act | unchanged; date comment | — | — |
| #207, #208 | PostgreSQL and MySQL locks | v0.19.0 — Providers & Testing | unchanged | — | SPEC-000 REQ-027 |
| #92, #93, #100, #101, #104 | Provenance, signing, NuGet names and workflow, pre-release checklist | v0.22.0 — Release Engineering | unchanged; #104 gains the law-map step | — | REQ-025, REQ-030 |
| #146, #148, #150 | Scheduling features | Post-1.0: Advanced Scheduling Features | unchanged; minimal slices in P-20 (DEC-005 (a)) | — | REQ-033 |
| #469 | Partitioned sequential messaging | Post-1.0: AI/LLM Integration | v0.18.0 — Integration & Platform Capabilities (§5.11); its column is P-51 (DEC-005 (a)) | — | REQ-042 |
| #134, #149 | Message versioning; scheduled dead letters | Post-1.0: Advanced Scheduling Features | unchanged | — | REQ-032, REQ-017 (related) |
| #583 | Persistent dead-letter stores | Post-1.0: Data Integrity & Cross-Cutting Functions (deferred items) | unchanged | — | REQ-017 (related) |
| #202 | WebHook support | Post-1.0: Web Advanced & Developer Experience | unchanged | — | REQ-036 (related) |
| #600 | Claim-check blob stores | Post-1.0: Persistent Stores Ecosystem | unchanged | — | REQ-046 (related) |
| #311 | Claim Check | Post-1.0: New Transport Providers | unchanged | — | REQ-046 (related) |
| #686, #687 | Compliance applicability resolver; rule engine | Post-1.0: Compliance Completion & Test Coverage (deferred items) | unchanged | — | REQ-050 (related) |
| #798 | Tenancy for Security.Audit and ABAC | Post-1.0: Compliance Completion & Test Coverage (deferred items) | v0.18.0 — Integration & Platform Capabilities (DEC-009 (b)) | — | REQ-061 |
| #737, #738, #739, #760 | `TenantId` in outbox, saga, scheduled and inbox messages | Post-1.0: Multi-Tenancy Core | v0.18.0 — Integration & Platform Capabilities (DEC-009 (b)); #737, #739 and #760 are delivered with P-50 | — | REQ-015, REQ-061 |
| #596 | Persistent `ITenantStore` implementations | Post-1.0: Multi-Tenancy Core | v0.18.0 — Integration & Platform Capabilities (DEC-009 (b)) | — | REQ-061 |
| #125, #304, #326, #338, #876 | Multi-tenancy messaging, pipeline behaviour, event sourcing, context middleware; EPIC Multi-Tenancy Core | Post-1.0: Multi-Tenancy Core | unchanged; linked as related from P-54, and their SPEC-002 parts are covered by P-54 and the issues above | — | REQ-061 (related) |
| #752, #845 | Audit trail for tenancy configuration changes; AIAct multi-tenancy | v0.14.0 — Hardening; v0.16.0 — AI Act | unchanged; linked from P-54 | — | REQ-061 (related) |
| #747 | ModuleId in messaging | Post-1.0: Multi-Tenancy Core | unchanged: module isolation, not tenancy | — | REQ-061 (related) |
| #804 – #808 | DORA, eIDAS 2, Data Act, ENS, EHDS | Post-1.0: EU Regulatory Compliance: DORA, eIDAS2, Data Act, ENS, EHDS (deferred items) | unchanged (DEC-015); fact-correction comments | — | REQ-052 (for #808) |

Counts: 54 P-ids (P-17 stands for 15 issues, so 68 new issues if P-52 is opened) plus the EPIC P-00, all pre-1.0. By priority: 47 P0 without condition (61 issues), 1 P0 under a condition (P-52, §12 question 1) and 6 P1. By proposed milestone (DEC-014): v0.14.0 — Hardening 8, v0.17.0 — Compliance Lifecycle 17, v0.18.0 — Integration & Platform Capabilities 16, v0.20.0 — Reference Scenario 1, v0.15.0 2, v0.16.0 1, v0.19.0 1, v0.21.0 3 (17 issues), v0.22.0 5. Existing issues moved into pre-1.0 milestones: 25 (18 into "v0.14.0 — Hardening"; #469 and the six tenancy issues into "v0.18.0 — Integration & Platform Capabilities").

## 14. Traceability

| Source | Requirements and decisions |
|---|---|
| A (Encina itself: CRA, PLD, export control) | §3.2; REQ-019, REQ-026 – REQ-030, REQ-058; DEC-010 |
| B, C, E (law map; E prevails) | §3; REQ-024, REQ-025; DEC-012 |
| C §6, §8 (voluntary standards) | §3.4; REQ-057 |
| D (inventory) | REQ-022, REQ-023; §13.1 |
| E §1 (AI Act Omnibus) | §3.2; REQ-060 |
| F G01 – G06 | REQ-001 – REQ-006 |
| F G07, G09, G12, G13 | REQ-007, REQ-009, REQ-010, REQ-012 |
| F G08, G21 | REQ-021, REQ-047; DEC-002 |
| F G10, G11 | REQ-011, REQ-059 |
| F G14, G23 | REQ-023, REQ-037; DEC-008 |
| F G15, G16, G17, G18, G19, G20, G22 | REQ-050, REQ-049, REQ-048, REQ-020 and REQ-055, REQ-051, REQ-052, REQ-003 |
| H-R01 – H-R11, H §5 (Verifactu and the producer question) | §3.3; DEC-002 (P-24); REQ-047 (no Verifactu semantics) |
| H-R04, H-R06, H-R12, H-R13, H-R18 | REQ-001, REQ-006, REQ-033, REQ-039, REQ-042, REQ-045, REQ-053 |
| H-R20 – H-R27 (payments) | DEC-003; REQ-013, REQ-036, REQ-044 |
| H-R28 – H-R33 (messaging) | REQ-014, REQ-018, REQ-036, REQ-041; DEC-016 |
| H-R34 – H-R38 (calendar, workspace, transfers) | REQ-012, REQ-013, REQ-040 |
| Reference-application analysis (private): identity and context | REQ-015, REQ-016 |
| Reference-application analysis (private): data access and platform | REQ-004, REQ-005, REQ-008, REQ-019, REQ-020, REQ-035, REQ-037, REQ-041, REQ-045, REQ-046, REQ-054, REQ-055 |
| Reference-application analysis (private): messaging and outbound HTTP | REQ-017, REQ-031 – REQ-034, REQ-039 – REQ-044, REQ-056; P-30 |
| Reference-application analysis (private): acceptance checks | §4.3 (S0 – S23, mapping of the eleven checks); REQ-038; DEC-007 |
| Code read on 2026-09-23 (`ProhibitedPractice`) | REQ-060 |
| Code read on 2026-09-24 (tenancy in `src/`) | REQ-061 |
| Issue bodies of #810 – #816 (read on 2026-09-24) | REQ-024; DEC-012; AC-024 |
| Adversarial review of this draft (PR #1156) | DEC-011 as the scope decision and §11.2; REQ-059, REQ-060; P-50 – P-53; S22, S23; AC-041, AC-042 |
| Maintainer decisions of 2026-09-23 | DEC-005, DEC-011 (a) + (d) + (e); §5.11 in 1.0 and the later 1.0 date; DEC-004 superseded; the reference application kept private and generic |
| Maintainer decisions of 2026-09-24 | DEC-001, DEC-002, DEC-003, DEC-006, DEC-008, DEC-009, DEC-010, DEC-012, DEC-013, DEC-015, DEC-016; #1150 Option A (REQ-017); REQ-061, REQ-062; INV-007, INV-008; AC-043, AC-044; P-54; S11 |

## 15. Change log

| Date | Change |
|---|---|
| 2026-09-23 | DRAFT created from research [notes A–I](research/SPEC-002/README.md); issues #1142 – #1155 were opened from the same study. DEC-001 recorded as PROVISIONAL by the maintainer. Counts: 58 REQ, 13 DEC, 40 AC, 49 P-ids, 22 scenarios. |
| 2026-09-23 | Adversarial review of the draft (PR #1156) applied. DEC-011 becomes the explicit pre-1.0 scope decision (§11.2: every item beyond SPEC-000, the 10-provider schema drivers, Spanish national law in the contract, the consequence for 1.0 and the SPEC-000 amendment it needs), with options (d) for the relational locator (REQ-054) and (e) for column encryption (REQ-055). New: REQ-059 (consent given for a minor, split from REQ-011's Tier B tag), REQ-060 (AI Act Art. 5 catalogue); P-50 (persisted request context), P-51 (outbox `PartitionKey`), P-52, P-53; S22 (mediator), S23 (DSR access across EF Core and Marten); AC-041, AC-042; stronger AC-001, AC-005, AC-006, AC-007, AC-011, AC-012, AC-015, AC-017, AC-018, AC-024, AC-029, AC-031, AC-033, AC-036, AC-038. Issues #1158, #1160, #1161 and PRs #1157, #1159 added. Research notes committed under `research/SPEC-002/`; verification flags, milestone names and SPEC-000 prefixes corrected. Counts: **60 REQ, 13 DEC, 42 AC, 53 P-ids (67 issues with P-17 × 15), 24 scenarios (S0 – S23)**. |
| 2026-09-24 | Maintainer decisions of 2026-09-23 and 2026-09-24 applied. **Privacy:** the reference application is private; the specification mentions it only as "a small Spanish psychology practice" and cites private evidence as "reference-application analysis (private)"; research notes G and I and the earlier note H are removed from the repository, and the generic law of H is kept as note [H](research/SPEC-002/H-integrations-law.md) with its row identifiers. **Scope:** DEC-011 DECIDED (a) + (d) + (e); every capability of §5.11 (REQ-039 – REQ-058) is in the 1.0 contract (§5.11 renamed "Integration and platform capabilities (in 1.0)"), with the later 1.0 date accepted; DEC-004 SUPERSEDED; the scenario phases become an ordering aid and every scenario gates 1.0 except the rules part of S15. **Decisions:** DEC-001 (b), DEC-002 (a) (the Verifactu module stays with the application and its producer), DEC-003 (a), DEC-005 (a), DEC-006 (a), DEC-008 (a), DEC-009 (b), DEC-010 (a) (only phone-home telemetry is excluded; Encina's telemetry is a design goal), DEC-012 (all seven Omnibus issues #810 – #816 built before 1.0, off by default, with the shape assessment), DEC-013 (a), DEC-015 (a) and DEC-016 (a) DECIDED; new DEC-014 (milestone structure) PROPOSED; DEC-007 stays PROPOSED. #1150 Option A in REQ-017 and AC-017. **New:** REQ-061 (multi-tenancy of every SPEC-002 capability) and REQ-062 (OpenTelemetry and structured logging for every SPEC-002 capability), INV-007, INV-008, AC-043, AC-044, P-54; S11 becomes a two-tenant scenario; §11.2 gains table 2 and the tenancy schema family; the SPEC-000 amendment now also narrows SPEC-000 §6; tenancy issues #737, #738, #739, #760, #798 and #596 and #469 move into 1.0; every P-issue is pre-1.0 and gets a DEC-014 milestone; §12 question 12 added. Counts: **62 REQ, 16 DEC, 44 AC, 8 INV, 54 P-ids (68 issues with P-17 × 15), 24 scenarios (S0 – S23)**. |
| 2026-09-24 | DEC-007 and DEC-014 decided by the maintainer. |
| 2026-09-24 | Milestones renumbered and the three DEC-014 milestones created. |
| 2026-09-24 | REQ-003 aligned with ADR-031 (retention erasure port). |
| 2026-09-24 | §12 question 1 answered (the practice treats minors); REQ-059 unconditional. |

## 16. Related documents

- [SPEC-000 — Encina 1.0 Baseline and Release Scope](SPEC-000-encina-1.0-baseline-and-release-scope.md) (SPEC-000 REQ-024 – REQ-026, SPEC-000 DEC-002, SPEC-000 INV-004 and SPEC-000 INV-005)
- [Research notes of 2026-09-23](research/SPEC-002/README.md) (non-normative)
- [SPEC-001 — DocRef citations for coverage](SPEC-001-coverage-docref-citations.md)
- [AI-DEVELOPMENT-MODEL.md](../engineering/AI-DEVELOPMENT-MODEL.md) (§5 – §7, §14)
- [ENCINA-1.0-RECONCILIATION.md](../engineering/ENCINA-1.0-RECONCILIATION.md) (§4, P0 – P3)
- [ADR-018 — Cross-cutting integration principle](../architecture/adr/018-cross-cutting-integration-principle.md)
- [ADR-019 — Compliance event sourcing with Marten](../architecture/adr/019-compliance-event-sourcing-marten.md)
- [ADR-021 — EventId uniqueness enforcement](../architecture/adr/021-eventid-uniqueness-enforcement.md)
- [SECURITY.md](../../SECURITY.md)

## 17. References

Retrieved or cited in the research notes of 2026-09-23. Secondary sources are marked (secondary). URLs shown as code rather than links belong to hosts that refuse automated link checks (HTTP 202 or 403 on 2026-09-23); they were valid in a browser.

### 17.1 EU legislation and procedures

- GDPR, Regulation (EU) 2016/679: [EUR-Lex](https://eur-lex.europa.eu/eli/reg/2016/679/oj)
- ePrivacy Directive 2002/58/EC: [EUR-Lex](https://eur-lex.europa.eu/eli/dir/2002/58/oj)
- NIS2, Directive (EU) 2022/2555: [EUR-Lex](https://eur-lex.europa.eu/eli/dir/2022/2555/oj); Implementing Regulation (EU) 2024/2690: [EUR-Lex](https://eur-lex.europa.eu/eli/reg_impl/2024/2690/oj/eng)
- AI Act, Regulation (EU) 2024/1689: [EUR-Lex](https://eur-lex.europa.eu/eli/reg/2024/1689/oj); Regulation (EU) 2026/1744 (Digital Omnibus on AI): [EUR-Lex](https://eur-lex.europa.eu/eli/reg/2026/1744/oj/eng)
- Digital Omnibus proposal COM(2025) 837: [EUR-Lex](https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:52025PC0837); procedure 2025/0360(COD): [OEIL](https://oeil.europarl.europa.eu/oeil/en/procedure-file?reference=2025/0360%28COD%29); Council simplification page: `https://www.consilium.europa.eu/en/policies/simplification/`; EDPB/EDPS statement: [EDPB](https://www.edpb.europa.eu/news/digital-omnibus-edpb-and-edps-support-simplification-and-competitiveness-while-raising-key_en)
- Data Act, Regulation (EU) 2023/2854: [EUR-Lex](https://eur-lex.europa.eu/eli/reg/2023/2854/oj)
- Data Governance Act, Regulation (EU) 2022/868: [EUR-Lex](https://eur-lex.europa.eu/eli/reg/2022/868/oj)
- European Accessibility Act, Directive (EU) 2019/882: [EUR-Lex](https://eur-lex.europa.eu/eli/dir/2019/882/oj)
- Digital Services Act, Regulation (EU) 2022/2065: [EUR-Lex](https://eur-lex.europa.eu/eli/reg/2022/2065/oj)
- Cyber Resilience Act, Regulation (EU) 2024/2847: [EUR-Lex](https://eur-lex.europa.eu/eli/reg/2024/2847/oj); Commission guidance C(2026) 5252: [announcement](https://digital-strategy.ec.europa.eu/en/library/commission-publishes-new-guidance-support-timely-cyber-resilience-act-implementation), [annex PDF](https://ec.europa.eu/newsroom/dae/redirection/document/131456); [implementation timeline](https://digital-strategy.ec.europa.eu/en/factpages/cyber-resilience-act-implementation); [CRA and open source](https://digital-strategy.ec.europa.eu/en/policies/cra-open-source); [CRA reporting](https://digital-strategy.ec.europa.eu/en/policies/cra-reporting); [ENISA Single Reporting Platform](https://www.enisa.europa.eu/news/the-cra-single-reporting-platform-is-launched); Art. 25 attestation work: [ORC WG](https://github.com/orcwg/cra-attestations)
- Product Liability Directive, Directive (EU) 2024/2853: [EUR-Lex](https://eur-lex.europa.eu/eli/dir/2024/2853/oj); BOE entry without national measure: [BOE](https://www.boe.es/buscar/doc.php?id=DOUE-L-2024-81701)
- Dual-Use Regulation (EU) 2021/821, consolidated 15 Nov 2025: [EUR-Lex](https://eur-lex.europa.eu/eli/reg/2021/821/2025-11-15/eng)
- DORA, Regulation (EU) 2022/2554: [EUR-Lex](https://eur-lex.europa.eu/eli/reg/2022/2554/oj); RTS 2024/1772: [EUR-Lex](https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32024R1772); RTS 2024/1774: [EUR-Lex](https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32024R1774); RTS 2025/301: [EUR-Lex](https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32025R0301); ITS 2025/302: [EUR-Lex](https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32025R0302); ITS 2024/2956: [EUR-Lex](https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32024R2956); RTS 2025/1190: [EUR-Lex](https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32025R1190)
- eIDAS 2, Regulation (EU) 2024/1183: [EUR-Lex](https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32024R1183); Implementing Regulation (EU) 2024/2979: [EUR-Lex](https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32024R2979)
- EHDS, Regulation (EU) 2025/327: [EUR-Lex](https://eur-lex.europa.eu/eli/reg/2025/327/oj/eng), [document information](https://eur-lex.europa.eu/legal-content/EN/ALL/?uri=CELEX:32025R0327); European Commission EHDS FAQ v1.1: [DG SANTE](https://health.ec.europa.eu/document/download/4dd47ec2-71dd-49fc-b036-ad7c14f6ed68_en?filename=ehealth_ehds_qa_en.pdf)
- Payment Services Regulation, procedure 2023/0210(COD): [OEIL](https://oeil.europarl.europa.eu/oeil/en/procedure-file?reference=2023/0210%28COD%29); EP Legislative Train: `https://www.europarl.europa.eu/legislative-train/theme-an-economy-that-works-for-people/file-revision-of-eu-rules-on-payment-services`
- NIS2 infringement referral (IE, ES, FR, NL): [Commission news](https://digital-strategy.ec.europa.eu/en/news/commission-refers-ireland-spain-france-and-netherlands-court-justice-failing-transpose-rules), [IP/26/1499](https://ec.europa.eu/commission/presscorner/detail/en/ip_26_1499); CER referral: [IP/26/910](https://ec.europa.eu/commission/presscorner/detail/da/ip_26_910)

### 17.2 Spanish legislation, bills and authorities

- LOPDGDD, LO 3/2018: [BOE](https://www.boe.es/buscar/act.php?id=BOE-A-2018-16673); Ley 10/2025: [BOE](https://www.boe.es/buscar/act.php?id=BOE-A-2025-26698)
- Ley 41/2002: [BOE](https://www.boe.es/buscar/act.php?id=BOE-A-2002-22188); Ley 44/2003: [BOE](https://www.boe.es/buscar/act.php?id=BOE-A-2003-21340); RD 1277/2003: [BOE](https://www.boe.es/buscar/doc.php?id=BOE-A-2003-19572)
- LSSI, Ley 34/2002: [BOE](https://www.boe.es/buscar/act.php?id=BOE-A-2002-13758)
- ENS, RD 311/2022: [BOE](https://www.boe.es/buscar/act.php?id=BOE-A-2022-7191)
- RDL 12/2018 (NIS1 transposition): [BOE](https://www.boe.es/buscar/act.php?id=BOE-A-2018-12257); NIS2 *anteproyecto*: [DSN](https://www.dsn.gob.es/en/node/24160); Congress initiatives search: [Congreso](https://www.congreso.es/es/busqueda-de-iniciativas)
- RD 1619/2012 (invoicing): [BOE](https://www.boe.es/buscar/act.php?id=BOE-A-2012-14696); RD 1007/2023 (Verifactu): [BOE](https://www.boe.es/buscar/act.php?id=BOE-A-2023-24840); Orden HAC/1177/2024: [BOE](https://www.boe.es/buscar/act.php?id=BOE-A-2024-22138); RDL 15/2025: [BOE](https://www.boe.es/buscar/doc.php?id=BOE-A-2025-24446), [validation](https://www.boe.es/buscar/doc.php?id=BOE-A-2025-25695); RD 238/2026 (B2B e-invoicing): [BOE](https://www.boe.es/diario_boe/txt.php?id=BOE-A-2026-7295)
- AEAT Verifactu FAQ: [declaración responsable](https://sede.agenciatributaria.gob.es/Sede/iva/sistemas-informaticos-facturacion-verifactu/preguntas-frecuentes/certificacion-sistemas-informaticos-declaracion-responsable.html), [scope](https://sede.agenciatributaria.gob.es/Sede/iva/sistemas-informaticos-facturacion-verifactu/preguntas-frecuentes/cuestiones-generales-ambitos-aplicacion.html), [VERI\*FACTU systems](https://sede.agenciatributaria.gob.es/Sede/iva/sistemas-informaticos-facturacion-verifactu/preguntas-frecuentes/sistemas-verifactu.html), [developer FAQ v1.3 (PDF)](https://sede.agenciatributaria.gob.es/static_files/AEAT_Desarrolladores/EEDD/IVA/VERI-FACTU/FAQs-Desarrolladores.pdf)
- AEPD: [DPIA list Art. 35.4](https://www.aepd.es/documento/listas-dpia-es-35-4.pdf), [list Art. 35.5](https://www.aepd.es/documento/listasdpia-35.5l.pdf), [cookie guide](https://www.aepd.es/guias/guia-cookies.pdf), [health professionals FAQ](https://www.aepd.es/preguntas-frecuentes/16-salud/2-profesionales-sanitarios), [guide for the health sector](https://www.aepd.es/documento/guia-profesionales-sector-sanitario.pdf)
- Consejo General de la Psicología, Código Deontológico: `https://www.cop.es/pdf/CodigoDeontologicodelPsicologo-vigente.pdf`; CNMC report on the draft code: [CNMC](https://www.cnmc.es/prensa/inf-codigo-deontologico-psicologia-20260826)

### 17.3 Standards and vendor terms

- ISO/IEC 27701:2025: `https://www.iso.org/standard/27701`; ISO/IEC 42001:2023: `https://www.iso.org/standard/42001`
- Stripe DPA (18 Nov 2025): [Stripe](https://stripe.com/legal/dpa)
- WhatsApp Cloud API data privacy and security: [Meta](https://developers.facebook.com/documentation/business-messaging/whatsapp/data-privacy-and-security/); WhatsApp Business Messaging Policy: [WhatsApp](https://whatsappbusiness.com/policy/)
- Google Cloud Data Processing Addendum: [Google](https://cloud.google.com/terms/data-processing-addendum/); Google Workspace data regions: [Google](https://knowledge.workspace.google.com/admin/compliance/compare-data-region-features-across-google-workspace-editions)
- NuGet Trusted Publishing: announced on the .NET blog ("Enhanced security is here with the new Trust Publishing on NuGet.org"); link omitted because that host currently fails the link scan (#1138)

### 17.4 Secondary sources

- AEPD PD-00068-2026 summary (secondary): [Cuatrecasas](https://www.cuatrecasas.com/en/global/life-sciences-healthcare/art/traceability-access-health-data)
- DPF litigation (secondary): [IAPP, *Latombe*](https://iapp.org/news/a/european-general-court-dismisses-latombe-challenge-upholds-eu-us-data-privacy-framework); [Digital Policy Alert, appeal](https://digitalpolicyalert.org/event/35459-latombe-filed-appeal-against-general-court-dismissal-of-challenge-to-european-unionunited-states-data-protection-framework-adequacy-decision-in-latombe-v-commission)
- Digital Omnibus negotiations (secondary): [Privacy Next, Sep 2026](https://www.privacynext.eu/resources/digital-omnibus-gdpr-negotiations-at-the-council-september-2026-update/); Kennedys: `https://www.kennedyslaw.com/en/thought-leadership/article/2026/the-2025-european-commission-eu-digital-omnibus-package-the-gdpr-regulation-eu-2016679/`
- AI Omnibus (secondary): [K&L Gates](https://www.klgates.com/EU-Digital-Omnibus-on-AI-Enters-Into-Force-7-31-2026); [Lewis Silkin](https://www.lewissilkin.com/insights/2026/07/27/the-digital-omnibus-on-ai-enters-into-force-today-102nedo)
- Verifactu and tax (secondary): [KPMG on RDL 15/2025](https://assets.kpmg.com/content/dam/kpmgsites/es/pdf/2025/12/tax-alert-el-real-decreto-ley-15-2025-amplia-plazos-adaptacion-reglamento-verifactu.pdf.coredownload.inline.pdf); [Iberley, LGT art. 201 bis](https://www.iberley.es/legislacion/articulo-201-bis-ley-general-tributaria); [Iberley, Código de Comercio art. 30](https://www.iberley.es/legislacion/articulo-30-codigo-comercio)
- Regional clinical-record retention (secondary), PSN Sercon: `https://blog.psnsercon.com/los-plazos-de-conservacion-de-las-historias-clinicas-en-las-distintas-comunidades-autonomas/`
- MDR software guidance MDCG 2019-11 rev.1 (secondary): [Emergo by UL](https://www.emergobyul.com/news/european-revision-primary-software-guidance-mdcg-2019-11-revision-1-small-changes-meaningful)
