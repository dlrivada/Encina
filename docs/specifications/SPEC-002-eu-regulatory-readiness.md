# SPEC-002 — EU Regulatory Readiness

| | |
|---|---|
| **Status** | 🟡 DRAFT — pending maintainer approval. DEC-001 recorded as PROVISIONAL; DEC-002 … DEC-013 PROPOSED |
| **Author** | Specifier (Claude), from the regulatory research notes A–I of 2026-09-23 |
| **Date** | 2026-09-23 |
| **Refines** | [SPEC-000](SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-024 (per-package article coverage), within DEC-002, REQ-025 and REQ-026 |
| **Evidence** | Code read on `main` at `ce337e0c` (2026-09-23); issues #1142–#1155 opened from the same study; primary sources in §17 |
| **Supersedes** | — |

> This specification implements nothing, and it is **not legal advice**. It states what must be true of Encina so that an application built on it *can* comply with EU and Spanish law wherever that law touches what a framework does. Encina never takes over the obligations of the application, its controller or its producer. Requirements state *what*, not *how*; design choices go through the ADR process ([AI-DEVELOPMENT-MODEL.md](../engineering/AI-DEVELOPMENT-MODEL.md) §7). Legal facts carry the verification flag of the research note they come from (§3.1 legend); where the notes disagreed, the fact-check (note E) and the opened issues prevail.

---

## 1. Problem

SPEC-000 made EU regulatory compliance part of the 1.0 contract (DEC-002) and requires every compliance package to state which articles it covers (REQ-024). It does not say which laws matter, which of them bind Encina itself and which bind the applications built on it, or what "ready" means for a concrete application. A study made on 2026-09-23 against a real reference application, ConsultaPsicologica (a small Spanish psychology practice, §4), found that:

- the legal ground is moving: the AI Act Omnibus was adopted in July 2026, the data and cyber Omnibus is still a proposal, Spain has not transposed NIS2 and was referred to the CJEU, and Verifactu applies to the practice from 2027;
- several shipped compliance behaviours would work **against** the law in that application: the retention sweep erases data under legal hold when the hold lookup fails (#1143) and erases the same data every cycle (#1142); crypto-shredding one field destroys legally retained clinical data (#1144); DSR erasure never consults retention or holds; there is no blocking state for LOPDGDD art. 32; the whole United States is treated as adequate (#1145);
- cross-cutting defects remove the "who" from every compliance decision (#1147, #1148, #1149) and lose outbox messages without trace (#1150, #1151, #1152, #1153, #1154);
- none of the 15 per-package specifications required by SPEC-000 REQ-024 exists, and several package READMEs describe types and provider coverage that do not exist.

## 2. Scope and responsibility boundary

### 2.1 Principle

Maintainer intent (2026-09-23): EU law compliance is mandatory and pre-1.0; the application is responsible for applying the law; Encina must be ready so that the application **can** apply it, as far as it concerns Encina. **Encina facilitates, never blocks.** In this specification:

1. The application, through its controller or producer, applies the law and takes every legal decision (which retention period, which Art. 9(2) condition, which processors, what the privacy notice says).
2. Encina provides the mechanisms, defaults and extension points that such decisions need, and documents honestly what each package covers.
3. Encina **blocks** an application when any of these holds: a shipped default or behaviour makes a legally required outcome impossible or silently undoes it (a *conflict*); the application has to bypass an Encina module to comply; or a documentation claim cannot be relied on by a compliance officer.
4. Encina itself stays outside regulated roles (CRA or PLD manufacturer, Verifactu producer) for as long as the maintainer decides so (DEC-002, DEC-010).

### 2.2 In scope

- The EU acts and Spanish national law in §3, as far as they create technical requirements that a .NET application framework can enable.
- Encina's own position under the CRA, the PLD and export control, and the good-practice duties that let integrators meet their own CRA obligations.
- ConsultaPsicologica as the concrete acceptance case, through the PracticeManagement reference scenario (§4.4).

### 2.3 Non-goals

- Legal advice. The law map is research with its uncertainty flags.
- Taking over application or organisational obligations: privacy notices, DPA contracts, DPIA conclusions, breach decisions and notifications, the Verifactu *declaración responsable*, CE marking under the EHDS or the MDR, DPO appointment, staff training and AI literacy, ISMS certification, backups operations, physical security.
- New regulation packages beyond SPEC-000: DORA, eIDAS 2, Data Act, ENS and EHDS (#804–#808) stay post-1.0 (SPEC-000 §6).
- Sector logic inside Encina: Verifactu (DEC-002), payments (DEC-003), FHIR/EEHRxF, QR and PDF generation, money and VAT models.
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
| **GDPR**, Reg. (EU) 2016/679 | Applicable since 25 May 2018; unchanged (amendments only proposed, §3.5). EU–US DPF adequacy decision (EU) 2023/1795 upheld by the General Court (T-553/23 *Latombe*, 3 Sep 2025); appeal C-703/25 P pending | No (Encina processes no personal data and has no telemetry) | Yes | Art. 6 and Art. 9(2) records, consent records, DSR orchestration, retention and erasure with exemptions, restriction, breach clock, DPIA tooling, RoPA export, transfer checks, encryption and pseudonymisation, access audit | [V] dates; [S] DPF litigation |
| **ePrivacy**, Dir. 2002/58/EC (Spain: LSSI, Ley 34/2002, arts. 20–22) | In force; the ePrivacy Regulation is dormant; Art. 5(3) would move into the GDPR under the Omnibus proposal (§3.5) | No | Yes: terminal storage (cookies, SDKs) and commercial e-mail, SMS or WhatsApp | Consent per purpose and channel, proof of consent, withdrawal as easy as grant, enumerable strictly-necessary exemptions, LSSI art. 21.2 existing-client basis with opt-out | [V] LSSI; [V] AEPD cookie guide (May 2024) |
| **NIS2**, Dir. (EU) 2022/2555 | Transposition due 17 Oct 2024. CIR (EU) 2024/2690 applies since 7 Nov 2024, only to the listed digital-infrastructure providers. **Spain**: *anteproyecto* approved at first reading on 14 Jan 2025 and not sent to the Cortes; formal notice 28 Nov 2024, reasoned opinion 7 May 2025, CJEU referral 8–9 Jul 2026 (IP/26/1499). RDL 12/2018 and RD 43/2021 (NIS1) remain the Spanish baseline | No | If medium or large and in an Annex I/II sector (a micro practice is out by size) | Art. 21(2)(a)–(j) building blocks; 24 h / 72 h / 1-month incident clock (Art. 23); supply-chain metadata (EPIC #880) | [V] |
| **AI Act**, Reg. (EU) 2024/1689, as amended by **Reg. (EU) 2026/1744** (Digital Omnibus on AI, of 8 Jul 2026; OJ 24 Jul 2026; in force 27 Jul 2026) | Art. 5 prohibitions and Art. 4 from 2 Feb 2025. Art. 4 **replaced**: duty to "take measures to support" AI literacy, no guaranteed level. **New** prohibitions Art. 5(1)(ba) and (bb) from 2 Dec 2026. GPAI (Chapter V) from 2 Aug 2025; Commission enforcement from 2 Aug 2026; models placed earlier comply by 2 Aug 2027. Art. 50 from 2 Aug 2026, with the Art. 50(2) marking duty deferred to 2 Dec 2026 for generative systems already on the market (new Art. 111(4)). Annex III high-risk from **2 Dec 2027**; Annex I high-risk from **2 Aug 2028**; high-risk systems of public authorities by 2 Aug 2030. Spain: *Proyecto de Ley Orgánica* on AI (121/000096, 28 May 2026) in Congress | No (`Encina.Compliance.AIAct` is not an AI system) | If the app provides or deploys AI features | AI system registry, Art. 12 logging, Art. 14 oversight hooks, Art. 50 disclosure metadata (EPIC #881) | [V] |
| **Data Act**, Reg. (EU) 2023/2854 | Applicable since 12 Sep 2025. Art. 3(1) design duty for products placed on the market after 12 Sep 2026. Switching charges abolished from 12 Jan 2027 (Art. 29). Chapter IV applies from 12 Sep 2027 to some contracts concluded on or before 12 Sep 2025. 30-day switching transition (Art. 25(2)(a)); functional equivalence for IaaS (Art. 30(1)) | No | If a data holder of connected products or a data-processing-service provider | Structured, machine-readable export shared with GDPR Art. 20; export for switching (post-1.0, #806) | [V] |
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
| **Ley 41/2002** (patient autonomy) | Consolidated to 1 Mar 2023. A psychology *consulta* is an authorised health centre (RD 1277/2003) | No | If the app keeps clinical records | Retention **at least 5 years from the discharge of each care episode** (art. 17.1; longer in some regions); authenticity of the record and of its changes (art. 14); access limits for third parties' data and professionals' "anotaciones subjetivas" (art. 18.3); identification separated from clinical data for secondary access (art. 16.3) | [V] national; [S] regional periods |
| **Código Deontológico del Psicólogo** (arts. 39–49) | In force; a new code is in draft (CNMC report 26 Aug 2026, AI provisions reported) | No | Organisational and app duties of the psychologist | Access restricted to the professional (art. 46), de-identified case material (arts. 43, 45), audit | [V] code; [S] draft |
| **ENS**, RD 311/2022 | BOE 4 May 2022, in force 5 May 2022. Anexo II has **73** measures; **five** dimensions (confidentiality, integrity, traceability, authenticity, availability). Applies to private suppliers of the public sector (art. 2(3)) | No | If the app serves the public sector | Activity logging (op.exp.8, dimension T), protection of information (mp.info.*), level-based profile (post-1.0, #807) | [V] |
| **Verifactu**: RD 1007/2023; Orden HAC/1177/2024; RDL 15/2025 | Corporate-tax taxpayers from **1 Jan 2027**; everyone else, IRPF professionals included, from **1 Jul 2027**; producers since 29 Jul 2025. VAT-exempt health services are covered once software issues the invoices (AEAT FAQ, 22 Jul 2026). Producer sanction: €150,000 per year and system type (LGT art. 201 bis) | No, as long as Encina ships no Verifactu-relevant component (DEC-002). A library implementing chaining, QR or submission could make its publisher a component "fabricante" (AEAT developer FAQ v1.3 §5) | Yes, any app that issues invoices | Regulation-neutral primitives only: reliable outbox (REQ-017), ordered partitions, gapless sequence, persistent chained log (REQ-042, REQ-045, REQ-047) | [V] dates and FAQ; [S] sanction wording |
| **B2B e-invoicing**: Ley 18/2022 art. 12; RD 238/2026 | Applies 12 or 24 months after a ministerial order still in draft (public hearing Apr–May 2026); B2B only, not to patients | No | If the app invoices businesses | None | [V] RD; [S] order status |
| **DORA**, Reg. (EU) 2022/2554 | Applies since 17 Jan 2025. Level 2: RTS 2024/1772 (classification: Arts. 1–7 criteria, Art. 8 major incident, Art. 9 thresholds); RTS 2024/1774 (ICT risk framework); RTS 2025/301 (initial report within 4 h of classification and 24 h of awareness; intermediate within 72 h of the initial one; final within 1 month of the latest intermediate); ITS 2025/302 (templates); ITS 2024/2956 (register of information); RTS 2025/1190 (TLPT) | No | If a financial entity or its ICT provider | Generic audit, crypto and resilience primitives; DORA module post-1.0 (#804) | [V] |
| **eIDAS 2**, Reg. (EU) 2024/1183 | In force 20 May 2024. Wallets by 24 Dec 2026. Private relying parties required by law or contract to use strong authentication must accept the wallet on the user's voluntary request by **24 Dec 2027** (Art. 5f(2)); micro and small enterprises exempt; VLOPs under Art. 5f(3) | No | If a relying party in a listed sector and not micro or small | Trusted time-stamping and e-signature integration points (post-1.0, REQ-048); wallet module post-1.0 (#805) | [V] |
| **EHDS**, Reg. (EU) 2025/327 | OJ 5 Mar 2025; in force **25 Mar 2025**. General application 26 Mar 2027. Priority categories (a)–(c) and the EHR systems processing them from **26 Mar 2029**; (d)–(f) from 26 Mar 2031; secondary use from 26 Mar 2029 (parts 2027, 2031, 2035). Micro-enterprise providers exempt from secondary-use data-holder duties (Art. 50). Access information available at least 3 years (Art. 9) | No | If a healthcare provider or EHR-system manufacturer (an in-house EHR counts as "put into service" [S]) | Evidential access log (who, which subject, which data, when, origin) and export as extension points (REQ-007, REQ-052; #808) | [V] dates via EUR-Lex and EC FAQ; [S] in-house clause |
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

Procedure 2025/0360(COD) is at **"awaiting committee decision"** (joint ITRE/LIBE; draft report 22 Jun 2026; amendments 27 Jul 2026). There is no Council mandate (an Irish Presidency compromise of 3 Sep 2026 is reported [S]) and no trilogue. Adoption is not expected before late 2026, and application later still. **Current law stays the default** in Encina until the text is adopted (REQ-024, DEC-012).

| Proposed change | Draft provision | Encina impact if adopted | Default until then |
|---|---|---|---|
| Breach notification within 96 h, only for "high risk", through an ENISA single entry point | GDPR Art. 33(1) | Configurable deadline and threshold (#813), ENISA adapter (#812) | 72 h, "risk" |
| Pseudonymised data not personal for an entity that cannot identify the subject; implementing acts on criteria | GDPR Art. 4(1), Art. 41a | Anonymization documentation | Current definition |
| Single-click reject; six-month re-ask moratorium; first-party audience-measurement exemption | GDPR Art. 88a(3)(c), 88a(4)(a) and (c); applies 6 months after entry into force | Consent cooldown (#810), exempt purposes (#811) | Not enabled |
| Machine-readable consent signals (controllers after 24 months; non-SME browsers after 48) | GDPR Art. 88b | Pluggable consent-signal source | Not enabled |
| AI development under legitimate interest | GDPR Art. 88c | #815 | Not enabled |
| NIS2 reporting through an ENISA single entry point; a CRA Art. 14(3) report counts as NIS2 reporting; **timelines unchanged** | NIS2 Art. 23a, Art. 23(12) | #812 | 24 h / 72 h / 1 month |
| DGA repealed and folded into the Data Act; Data Act trade-secret refusal | Data Act Art. 4(8), 5(11) | #806 (post-1.0) | Current law |
| DORA, eIDAS, CER and EUDPR reporting through the single entry point | Several | None for 1.0 | Current law |

### 3.6 Review cadence

- §3 carries its verification date. It is re-verified before each minor tag of the 1.0 sequence (SPEC-000 DEC-005) and before `1.0.0-rc.1`, through a pull request (REQ-025).
- It is also re-verified within 30 days of any of these events: COM(2025) 837 published in the OJ; the Spanish NIS2 law published in the BOE; judgment in C-703/25 P; AI Act Commission guidelines (due by 1 Aug 2027 and 2 Sep 2027); EHDS implementing acts (due by 26 Mar 2027); the new Código Deontológico; the AEAT answer sought under DEC-002.

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

### 4.1 ConsultaPsicologica

ConsultaPsicologica is the private application that gave rise to SimpleMediator and then Encina. It has been paused since 2025-12-06 and will resume on Encina after 1.0. Its current state (note G):

- .NET 10; Blazor Server back office; minimal APIs with Problem+Json; PostgreSQL 16 with EF Core for commands, Dapper for reads, Hangfire on the same database;
- a *Scheduling* context (agenda, slots, reservations, cancellation policies) with a hand-rolled outbox synchronised to Google Calendar;
- a *Payments* context with a gateway router and a Stripe stub;
- no patient, invoice, consent or clinical-record model yet;
- planned: session packs, invoices with Verifactu, e-mail and WhatsApp reminders, a patient portal, online payments.

Its legal profile (notes F and H): even without clinical notes it processes special-category data, because an appointment with a psychologist reveals health information [I]. A *consulta* is an authorised health centre. Clinical records must be kept at least 5 years from each discharge and then blocked, not destroyed. Patients may learn who accessed their data (AEPD PD-00068-2026 of 13 Jul 2026 [S]; EHDS Art. 9 from 2029). Invoices are required even for VAT-exempt services, and Verifactu applies from 1 Jul 2027 (self-employed) or 1 Jan 2027 (company). The practice relies on US vendors (Google, Stripe, Meta, Microsoft). NIS2 (size), the DPO duty (single professional) and EHDS secondary-use duties (Art. 50) do not apply to a micro practice; the MDR, AI Act Art. 50, ENS and the CRA apply only under conditions the app can avoid or control.

### 4.2 Tiers (DEC-001)

- **Tier A**: the application's charter as written: agenda, session packs, invoices, payments, notifications, portal; no clinical data.
- **Tier B**: Tier A plus clinical records (*historia clínica*), clinical consents, minors and documents.

Working assumption, recorded by the maintainer on 2026-09-23: **Tier B, provisional**, pending a cost study. Going down is easier than going up, so requirements that only Tier B needs are marked **[Tier B]**; if DEC-001 later settles on Tier A, they leave the 1.0 contract without reopening anything else.

### 4.3 Reference scenario "PracticeManagement" (acceptance gate)

A test suite that ports a slim version of the application, run in CI Full (location: DEC-007). Stack: `WebApplicationFactory`; PostgreSQL through Testcontainers with Respawn; EF Core commands, Dapper queries and the Marten compliance modules on the same database; `FakeTimeProvider` in Europe/Madrid, including DST days; WireMock fakes for Google OAuth and Calendar, WhatsApp Cloud, Stripe-like and Redsys-like gateways and the AEAT (a Kestrel test server with `RequireCertificate` for mTLS); a fake e-mail channel; thin app-style adapters; entry points through minimal APIs, a Blazor Server circuit and a Hangfire job.

**Phase A** scenarios gate 1.0. **Phase B** scenarios are post-1.0, but the public API shipped in 1.0 must not need a breaking change to support them (REQ-038).

| # | Scenario | REQs | Phase |
|---|---|---|---|
| S0 | A command sent from a minimal API, a Blazor Server circuit event and a Hangfire job carries the right actor (therapist, operator, `system`) and tenant into the authorisation, audit, consent and cache-key behaviours; an operator is denied `clinical-notes:read` | REQ-015, REQ-016, REQ-019 | A |
| S1 | Booking writes the reservation and its outbox message in one EF transaction; the calendar fake receives exactly one insert; echoes are suppressed; a retry does not re-run handlers that already succeeded | REQ-017, REQ-031 | A |
| S2 | The calendar returns 5xx twice, then 200: delivered once. A 400 is not retried and is dead-lettered; exhaustion is logged, metered and visible in health | REQ-017, REQ-034 | A |
| S3 | Book, reschedule and cancel are delivered in order per reservation | REQ-042 | B |
| S4 | A reminder across the October DST change; move and cancel by key; a recurring 09:00 Europe/Madrid job stays at 09:00 local time | REQ-033 | A if DEC-005 (a), else B |
| S5 | A WhatsApp status webhook: valid, duplicate, bad signature, stale | REQ-036 | A |
| S6 | Payment: checkout, success webhook delivered twice (deduplicated) in phase A; out-of-order refund through a saga waiting for an event in phase B | REQ-036, REQ-044 | A / B |
| S7 | Refund saga producing a corrective invoice, including the failure path | REQ-044 | B |
| S8 | 50 concurrent invoices numbered gaplessly per series (app-level counter in phase A; `ISequenceGenerator` and a persistent chain valid across a restart in phase B) | REQ-045, REQ-047 | A / B |
| S9 | Verifactu-like submission: pacing, rejection of record N holds later records, server-directed wait honoured, mTLS | REQ-039, REQ-042 | B |
| S10 | OAuth refresh, concurrent refresh, `invalid_grant` pauses the partition | REQ-040 | B |
| S11 | Portal: a patient sees only their own data; two tenants stay isolated | REQ-015 | A |
| S12 | No WhatsApp marketing without channel consent while e-mail marketing is allowed; the LSSI art. 21.2 existing-client basis works with an opt-out; a reminder passes as transactional; the consent subject is the patient (`Guid` id), not the operator | REQ-014, REQ-016 | A |
| S13 | The therapist runs the Dapper agenda query and one read-audit entry per patient records who, when, which data and the purpose; "who accessed patient X in the last 3 years" returns it; with fail-closed on, an audit-store failure fails the read | REQ-007, REQ-008 | A |
| S14 | An erasure request for a patient with current-year invoices is partly refused (Art. 17(3)(b), with a grantable-after date) and marketing data is erased; retained data becomes blocked, invisible to therapist and operator queries and visible only through an audited authority disclosure; a legal hold stops the sweep; a failed hold check deletes nothing; the sweep marks each record deleted once. [Tier B] A clinical record under a 5-year floor from discharge is refused and re-anchored when a new episode opens | REQ-001 – REQ-005 | A |
| S15 | [Tier B] An access export omits subjective annotations and third-party data, with reasons; a guardian's request for a minor is recorded as acting for the minor; a relative's request for a deceased patient who prohibited access is refused | REQ-009, REQ-011 | A (Tier B) |
| S16 | Crypto-shredding the marketing category leaves clinical fields readable; shredding is refused while a floor or hold covers the key | REQ-006 | A |
| S17 | A transfer to a non-DPF-certified US recipient fails and to a certified one passes; Stripe is recorded as processor for payments and independent controller for fraud prevention; a consumer mail account is flagged as having no Art. 28 terms; calendar sync is blocked while the Google DPA is missing or expired | REQ-012, REQ-013 | A |
| S18 | The RoPA export lists "appointments and invoicing" with Art. 6(1)(b)/(c) and Art. 9(2)(h), and the processors with roles and transfer bases; a recorded breach shows its 72-hour deadline | REQ-010 | A |
| S19 | Creating a patient in EF Core and starting its retention record and consent in Marten either both commit or are reconciled; a fault injected between them leaves no untracked personal data | REQ-037 | A |
| S20 | Therapist notes and OAuth refresh tokens are redacted for the operator role (phase A); they are ciphertext in PostgreSQL and plaintext through the app (phase B, unless REQ-055 is promoted) | REQ-019, REQ-055 | A / B |
| S21 | Processed outbox and executed scheduled messages carrying personal data are purged after their retention period | REQ-018 | A |

## 5. Requirements

Identifiers are stable. Each requirement carries its 1.0 placement and the reason, using the SPEC-000 boundary:

- **Defect**: shipped behaviour conflicts with the law or with Encina's own documented contract. Pre-1.0 (SPEC-000 REQ-011; security-classified bugs cannot be deferred).
- **Shape**: changes a persisted format, a public contract or a security default. Pre-1.0, because breaking changes stop being free after 1.0 (SPEC-000 §2).
- **Capability**: additive. Post-1.0 unless the maintainer promotes it; the promotions this specification proposes are grouped in DEC-011.
- **Process/Docs**: pre-1.0 where SPEC-000 already requires it (SPEC-000 REQ-018 – REQ-021 and REQ-024).

### 5.1 Data lifecycle: retention, erasure, blocking

- **REQ-001** (pre-1.0, shape, DEC-011) A retention policy can state a **minimum** retention (a floor before which erasure is refused) and a maximum (after which erasure or blocking is due). The period starts from an event the application raises (episode discharge, end of fiscal year, death) rather than only from the tracking start; it can be re-anchored when a new episode opens; it uses calendar arithmetic, not 365-day years; policies vary by jurisdiction and document type. An *immutable record* class (invoices, Verifactu records) is corrected only by new records, never updated.
  *Rationale:* Ley 41/2002 art. 17.1 requires at least 5 years from each discharge, and some regions longer; invoices follow the tax limitation period (LGT, 4 years [K]; Código de Comercio art. 30, 6 years [S]). Today expiry is tracking start plus period (`src/Encina.Compliance.Retention/Services/DefaultRetentionRecordService.cs`) and `RetentionPolicyType.EventBased` is unused. *Source:* F R17, C08, G01; H amendment to G01.
- **REQ-002** (pre-1.0, defect, #1143) Retention enforcement never erases when it cannot establish that no legal hold applies: an error while checking holds skips the record, is observable (log and metric) and is retried; holds are recorded against the real entity, never `Guid.Empty`.
  *Rationale:* `RetentionEnforcementService` maps a failed hold lookup to "no hold" and erases. *Source:* F C10, G02.
- **REQ-003** (pre-1.0, defect, #1142, #1146, #770) The retention sweep moves each record from active to expired to deleted exactly once, erases by the correct data-subject id, counts only what it erased, and reads time only from `TimeProvider`, event replay included.
  *Rationale:* the sweep re-erases on every cycle and its deletion trail is wrong, so a LOPDGDD art. 32 timeline cannot be proved; `RetentionRecordAggregate` reads `DateTimeOffset.UtcNow`. *Source:* F C11, C33, G03, G22.
- **REQ-004** (pre-1.0, defect and shape) A data-subject erasure request consults retention floors, legal holds and declared Art. 17(3) exemptions before erasing anything, erases what is erasable, and returns a reasoned partial refusal per data category: the exemption, its legal basis and the date after which the request becomes grantable.
  *Rationale:* `src/Encina.Compliance.DataSubjectRights/Erasure/DefaultDataErasureExecutor.cs` honours only a static `[PersonalData(LegalRetention = true)]`, never calls `ILegalHoldService`, and `DSRErrors.ExemptionApplies` is unused. During the floor a patient's request must be refused for clinical records and invoices while contact and marketing data are erased (GDPR Art. 12(4), 17(3)(b), (c), (e)). *Source:* F C09, G04; G L4.
- **REQ-005** (pre-1.0, shape, DEC-011) Encina provides a **blocked** data state (LOPDGDD art. 32 *bloqueo*): data that is retained but excluded from all processing and viewing, including versions superseded by a rectification; released only through an audited, purpose-bound disclosure path to an authorised role (courts, prosecutors, supervisory authorities); destroyed or crypto-shredded when a configured limitation period ends; with a secure-copy alternative where blocking is disproportionate (art. 32.4). Relational stores get a blocking erasure strategy instead of hard delete, and Marten streams get the same state. The design is jurisdiction-neutral ("restriction with statutory release roles").
  *Rationale:* a Spanish controller must block, not destroy, after erasure and after rectification; without this state it has to bypass the Retention and DSR modules. No blocking concept exists in `src/`; the closest are Art. 18 restriction and legal hold. *Source:* F R12, C12, G05; G L5.
- **REQ-006** (pre-1.0, defect, #1144) Crypto-shredding is scoped per data subject and data category (or retention class): erasing one category never makes another unreadable. Shredding is refused while a retention floor or legal hold covers data under the key, and it is the final step after blocking. Immutable records never share a key with erasable data. The subject key store defaults to durable storage outside development and supports key wrapping by a KMS.
  *Rationale:* `src/Encina.Marten.GDPR/Erasure/CryptoShredErasureStrategy.cs` deletes every key of the subject for each erasable field; the default key store is in memory and loses keys on restart. *Source:* F C13, G06; H amendment to G06.

### 5.2 Access transparency

- **REQ-007** (pre-1.0, shape, DEC-011) Read audit can serve as evidence of access. An option makes the read fail when its audit entry cannot be stored; collection and paged reads record the entity ids they return; `RequirePurpose` can reject, not only warn; entries can carry the data-subject id and a data category; a query answers "who accessed data of subject X, when, which data, for which purpose"; for special-category data the retention of read-audit entries defaults to at least 3 years.
  *Rationale:* AEPD PD-00068-2026 [S] reads GDPR Art. 15 as entitling patients to the identity, time and data of accesses, and EHDS Art. 9 keeps that information for at least 3 years. Today `src/Encina.Security.Audit/AuditedRepository.cs` logs reads fire-and-forget, collection reads carry no ids, `RequirePurpose` only logs a warning and `ReadAuditOptions.RetentionDays` defaults to 365. The read-audit store defects #1128, #1129 and #1135 are prerequisites. *Source:* F R23, C04, G07.
- **REQ-008** (pre-1.0, capability, DEC-011) Read audit covers reads that do not go through Encina repositories, such as CQRS query handlers over Dapper, ADO.NET or EF projections: a request or response declares the subjects it exposes, and the audit behaviour writes one REQ-007 entry per subject.
  *Rationale:* the reference app reads only through Dapper query handlers, so repository audit captures nothing, and `AuditPipelineBehavior` records the request type and a payload hash, not the subjects. *Source:* G N5, L6.
- **REQ-009** [Tier B] (pre-1.0, shape and disclosure defect, DEC-011, conditional on DEC-001) A DSR access export can exclude declared fields or records (third parties' data, professionals' subjective annotations, serious-harm withholding) and records the reason for each exclusion.
  *Rationale:* Ley 41/2002 art. 18.3 and Código Deontológico art. 42; without this the export discloses what the law says to withhold. No such marker exists (`DefaultDSRService`, `AccessResponse`). *Source:* F R18, C15, G09.

### 5.3 Legal modelling

- **REQ-010** (pre-1.0, shape, DEC-011) Processing activities, the RoPA export and lawful-basis records carry, next to the Art. 6 basis, the Art. 9(2) condition for special-category data and a reference to the national legal basis where one is required (for example LOPDGDD art. 9.2 and DA 17ª).
  *Rationale:* health processing needs both; `src/Encina.Compliance.GDPR/Model/LawfulBasis.cs` has only the six Art. 6 values, so the Art. 30 record of a health practice cannot be complete. It applies in Tier A too. *Source:* F R01, R04, C23, G12; G L1.
- **REQ-011** [Tier B] (pre-1.0 for the persisted shape, post-1.0 for rules; DEC-011, conditional on DEC-001) Consent and DSR records can state who acted for whom (a minor and a guardian, holders of parental authority, a voluntary proxy), under which authority and until when; a data subject can be marked deceased, with the requesters authorised and any prohibition the subject expressed.
  *Rationale:* LOPDGDD arts. 3 and 7; Ley 41/2002 arts. 9.3–9.4, 18.2 and 18.4; EHDS proxy services. The persisted aggregates have no such fields. Whether the practice treats minors is open (§12). *Source:* F R13, R14, C18, C19, G10, G11.
- **REQ-012** (pre-1.0, defect, #1145) Transfer checks treat the EU–US Data Privacy Framework as adequacy only for DPF-certified recipients, offer the DPF as a transfer basis, and document the pending appeal C-703/25 P as a known risk.
  *Rationale:* `src/Encina.Compliance.DataResidency/Model/RegionRegistry.cs` marks the whole US adequate; the reference app depends on Google, Stripe, Meta and Microsoft. *Source:* F R11, C26, G13; H-R37.
- **REQ-013** (pre-1.0, shape, DEC-011) The processor register records, per vendor and purpose, whether the vendor acts as processor, independent controller or joint controller, and whether Art. 28 terms exist at all, so that `[RequiresProcessor]` can refuse or warn accordingly.
  *Rationale:* Stripe is a processor for payments and an independent controller for fraud and AML (Stripe DPA, 18 Nov 2025 [V]); consumer Google and Microsoft accounts have no DPA ([V]/[K]); `src/Encina.Compliance.ProcessorAgreements/Model/Processor.cs` has no role. *Source:* H-R23, H-R33, H-R34, C15, H05.
- **REQ-014** (pre-1.0, shape, DEC-011) Consent is scoped by purpose **and** channel (e-mail, SMS, WhatsApp, phone, terminal storage). It can record the LSSI art. 21.2 existing-client basis with an opt-out offered in every message. Outbound messages are marked transactional or commercial, so that reminders pass without marketing consent and promotions do not. The `Encina.Compliance.Consent` specification carries GDPR, ePrivacy Art. 5(3) and LSSI arts. 20–22 tables (SPEC-000 REQ-024).
  *Rationale:* LSSI arts. 21 and 22.1 [V]; WhatsApp Business Messaging Policy [V]; `ConsentPurposes` has no channel dimension. ePrivacy is in the 1.0 contract (SPEC-000 DEC-002). *Source:* H-R31, C14, H04.

### 5.4 Identity and context

- **REQ-015** (pre-1.0, defect, #1147, #1148) Every pipeline behaviour sees the actor, tenant, correlation id and idempotency key of the request, whatever the entry point: HTTP, Blazor Server circuits, background jobs with an explicit system actor, and outbox, inbox and scheduler dispatches that rebuild the context from persisted metadata. Request-context time comes from `TimeProvider`.
  *Rationale:* `IEncina.Send` builds an empty context, and `AuthorizationPipelineBehavior` denies every request in a circuit; compliance, audit, cache and idempotency behaviours lose the "who". *Source:* G N1, N2, F16; I C1.
- **REQ-016** (pre-1.0, defect, #1149) The data subject is resolved independently of the actor: subject ids of any type (string, `Guid`, strongly-typed ids) are supported; a declared subject property that cannot be read fails closed with a clear error; there is no silent fallback to the caller's user id.
  *Rationale:* in a practice the psychologist or operator is not the patient, so a consent or restriction check can be evaluated against the wrong person. *Source:* G N3.

### 5.5 Messaging reliability and storage limitation

- **REQ-017** (pre-1.0, defect, #1150, #1151, #1152, #1153, #1154) The documented at-least-once guarantee holds on every provider: a `Left` from dispatch is a failure, never "processed"; retries use exponential backoff with jitter; exhaustion is logged, metered, visible in health checks and moves the message to a dead-letter state; Hangfire jobs fail on `Left` so that Hangfire retries them; `ScheduleRecurringAsync` propagates a failed insert; MongoDB has an outbox processor.
  *Rationale:* AEAT submissions, refunds, reminders and calendar sync all go through the outbox and can be lost without trace today. Persistent dead-letter stores remain post-1.0 (#583, #149); the dead-letter *state* is not. *Source:* H C7, H02; I C2–C4.
- **REQ-018** (pre-1.0, shape, DEC-011) Processed outbox messages and executed scheduled messages are purged after a configurable retention period on all 10 database providers, as the inbox already is.
  *Rationale:* payloads carry personal and sometimes health data and are kept forever (GDPR Art. 5(1)(e)); `src/Encina.Messaging/Outbox/IOutboxStore.cs` has no purge, while `InboxOptions` has one. *Source:* H C8, H03.

### 5.6 Security defaults

- **REQ-019** (pre-1.0, shape, DEC-006) Security- and compliance-relevant behaviours fail closed when they cannot decide, and any skip is an explicit, logged opt-out. This covers HMAC validation without an `HttpContext` (#1155, I A7), legal-hold checks (REQ-002), subject resolution (REQ-016) and PII response masking, which gains an option to fail the request instead of returning the unmasked response. Response redaction can depend on role, permission or purpose, so that an operator never sees therapist notes.
  *Rationale:* secure-by-default (CRA Annex I Part I(2)(b)) falls on integrators; Código Deontológico art. 46; Ley 41/2002 art. 16. `src/Encina.Security.PII/PIIMaskingPipelineBehavior.cs` returns the original response when masking fails. *Source:* A §4 item 7; G N8, L9; I A7.
- **REQ-020** (pre-1.0 for the orphan attribute) The orphan `EncryptedField` attribute is consumed or removed before 1.0 (#857). Column-level encryption at rest through ORM value converters is REQ-055 (post-1.0 unless promoted).
  *Rationale:* GDPR Art. 32; the application's own ADR asks for encrypted OAuth tokens and notes, and pipeline encryption (`EncryptionPipelineBehavior`) does not encrypt columns. A public attribute that does nothing misleads. *Source:* F C01, G18; G F14, L10.
- **REQ-021** (pre-1.0, documentation honesty, #860) `Encina.Compliance.Attestation` either persists its hash chain or its README and specification state that the chain is in memory and not evidential. A persistent chained log is REQ-047.
  *Rationale:* Ley 41/2002 art. 14 (authenticity of changes) and LOPDGDD art. 32.4 (digital evidence); `HashChainAttestationProvider` keeps the chain in memory while the README claims self-hosted production use. *Source:* F C06, G08; I A9.

### 5.7 Documentation, claims and draft law

- **REQ-022** (pre-1.0, refines SPEC-000 REQ-024) Each of the 15 compliance packages has a SPEC-NNN with an article table: act, article, status (Covered, Partial, Not covered, Not applicable), Encina types, evidence (test or audit record) and tracking issue. The table includes the national articles the package helps apply: LOPDGDD art. 32 and Ley 41/2002 arts. 17 and 18.3 for Retention and DataSubjectRights; LSSI arts. 20–22 for Consent. It lists as "Not covered" what stays with the application or the organisation. The supporting packages `Encina.Security.Audit`, `Encina.Audit.Marten` and `Encina.Marten.GDPR` are cited as evidence where they implement an article. No coverage percentage is typed by hand (SPEC-001).
  *Rationale:* none of the 15 specifications exists; a compliance officer needs article-level claims. *Source:* D §2.2, §4.1; SPEC-000 REQ-024.
- **REQ-023** (pre-1.0) Package documentation matches the code: READMEs name only types that exist and only the providers actually supported; ADR-019 states the real provider set; every compliance package has a README; AIAct, Attestation, DataSubjectRights and GDPR have integration tests or a written justification.
  *Rationale:* the READMEs of BreachNotification, DataResidency, ProcessorAgreements and Retention name stores that do not exist and claim ten providers; [ADR-019](../architecture/adr/019-compliance-event-sourcing-marten.md) mentions "13 providers"; Anonymization, PrivacyByDesign, CrossBorderTransfer and `Encina.Security.Audit` have no README. *Source:* F C34, G14; D §4.2–§4.3; #1090, #689.
- **REQ-024** (pre-1.0, default behaviour, DEC-012) A behaviour that implements a provision of a proposal not yet adopted (COM(2025) 837: #810–#816) is off by default, names the draft article in its XML documentation and in its package specification, and leaves current-law behaviour as the default (72 hours and "risk" for GDPR Art. 33; no consent cooldown).
  *Rationale:* SPEC-000 REQ-026 schedules the Omnibus adaptations for 1.0 while the proposal is still in committee; a default that anticipates draft law would put applications out of line with current law. *Source:* E §2; B §1.6.
- **REQ-025** (process) §3 is re-verified as §3.6 says and its date is updated through a pull request; the pre-release checklist (#104) includes the step.
  *Rationale:* several dates in the research notes changed or were corrected within weeks. *Source:* B §10; E.

### 5.8 Encina's own legal position and supply chain

These requirements do not come from obligations on Encina (§3.2): they let integrators meet the CRA duties that do bind them, and they keep Encina outside the CRA and the PLD.

- **REQ-026** (pre-1.0, process, DEC-010) Encina keeps the conditions of non-commercial FOSS: no price; no release, binary or security fix gated on payment or donation; paid services, if any, optional and separate; no mandatory telemetry and no personal-data condition of use. SECURITY.md or the README states the position: MIT, natural-person maintainer, not placed on the market under Reg. (EU) 2024/2847 (guidance C(2026) 5252, Example 34) nor under Dir. (EU) 2024/2853 Art. 2(2); integrators remain responsible under CRA Art. 13(5) and 13(6).
  *Source:* A §1.2–§1.4, §2, §4 items 9–10.
- **REQ-027** (pre-1.0) Every published package has an SBOM (CycloneDX 1.6 or SPDX 2.3 or later, JSON, with licence, PURL and hashes) attached to its GitHub Release.
  *Rationale:* integrators' SBOM (Annex I Part II(1)) and due diligence (Art. 13(5), recital 34). `.github/workflows/sbom.yml` covers only the core package and uploads a workflow artifact. *Source:* A §4 item 1.
- **REQ-028** (pre-1.0, with SPEC-000 REQ-021) [SECURITY.md](../../SECURITY.md) states: supported versions and a support period in month and year from 1.0; best-effort response targets; that denial of service, ReDoS and resource exhaustion in Encina's own code are in scope; how integrators report vulnerabilities and share fixes (CRA Art. 13(6); guidance points 223 and 227); and the cryptographic primitives Encina uses (.NET BCL AES-GCM, HMAC-SHA-2, SHA-256; no primitives of its own).
  *Rationale:* an integrator's support period weighs the support of core components (Art. 13(8)); cryptographic needs are checked against component documentation (guidance point 171). Today SECURITY.md gives no period and no targets and puts denial of service out of scope. *Source:* A §4 items 3 and 8.
- **REQ-029** (pre-1.0, process) Every security fix is published as a GitHub Security Advisory for the NuGet ecosystem with a CVE requested, and the changelog keeps a security history; after 1.0, security fixes ship as security-only patch releases where feasible.
  *Rationale:* advisories feed NuGetAudit and the EUVD that integrators check (recital 34; Annex I Part II(2) and (4)). *Source:* A §4 items 4–5.
- **REQ-030** (pre-1.0, with SPEC-000 REQ-018 and REQ-019) Publishing to nuget.org uses Trusted Publishing (OIDC, no long-lived key) with the package-ID prefix reserved, in addition to the attestations and Sigstore signatures of SPEC-000 REQ-018; an OpenSSF Scorecard runs on the repository.
  *Source:* A §4 items 6 and 11.

### 5.9 Pre-1.0 shape changes found through the reference application

From note I §(c) C.2. A7 is covered by REQ-019, A8 by P-30 (§13), A9 by REQ-021, A10 by SPEC-000 REQ-027.

- **REQ-031** (A1, pre-1.0, shape) The outbox message id is the dispatch idempotency key; a handler that already succeeded is skipped when the message is retried (keyed by outbox id and handler type); dispatch attempts every handler and aggregates their errors instead of failing fast.
  *Rationale:* this changes the documented dispatch contract; today a retry re-runs every handler (duplicate calendar events and messages). Related: #733. *Source:* I A1.
- **REQ-032** (A2, pre-1.0, shape) Persisted message types use stable names from a registry (for example `[MessageType("consulta.reserva.confirmada.v1")]`) with an assembly-qualified fallback, and outbox, inbox and scheduled payloads can be versioned and upcast.
  *Rationale:* the assembly-qualified name is the persisted wire format; renaming a type after 1.0 would strand stored messages. Related: #134. *Source:* I A2.
- **REQ-033** (A3, pre-1.0 only if DEC-005 (a), shape) Scheduling gains a schedule key for upsert, cancel and reschedule on all 10 providers; time-zone-aware recurring cron that stores the zone; a shipped `ICronParser`; the orphan `Encina.EntityFrameworkCore.Scheduling.IMessageScheduler` implemented or removed; `DateTimeOffset` accepted.
  *Rationale:* schema and signature changes across 10 providers; a 09:00 Madrid reminder drifts at DST because cron is evaluated in UTC. Minimal slices of #146, #148 and #150. *Source:* I A3.
- **REQ-034** (A4, pre-1.0, shape) Outbound errors are coded and carry `http.status`, `http.retry_after` and `transient` metadata, never the response body, and use the context correlation id; `RetryPipelineBehavior` classifies through `IErrorClassifier` (moved to core), honours Retry-After and never retries a non-transient error.
  *Rationale:* error codes are public contract; retrying a 4xx on a non-idempotent send (WhatsApp) duplicates messages; response bodies can carry personal data. Evidence: `src/Encina.Refit/Handlers/RestApiRequestHandler.cs`, `src/Encina.Polly/Behaviors/RetryPipelineBehavior.cs`. *Source:* I A4.
- **REQ-035** (A5, pre-1.0, shape) The legacy `AddApplicationMessaging` alias is removed from `src/Encina/Core/ServiceCollectionExtensions.cs`.
  *Rationale:* it breaks the no-legacy rule and collides with the application's own method of the same name. *Source:* G N11; I A5.
- **REQ-036** (A6, pre-1.0 through #731 and #734) Inbound webhooks have a provider-neutral ingestion core: raw-body access; a configurable signature verifier (header, prefix, hex or Base64, canonical form such as `timestamp.body`, tolerance, several active secrets); replay protection through `INonceStore`; a provider event-id extractor; an inbox key made of source and event id; an in-progress duplicate answered with 200 or 409; size limits; fast acknowledgement. Vendor presets (Stripe-, Meta-, Redsys- and Google-style) are configuration, not SDK dependencies.
  *Rationale:* `Encina.Security.AntiTampering` verifies only its own canonical scheme (`HMAC/SignatureComponents.cs`) and cannot check vendor signatures. #731 and #734 are in v0.14.0; #202 stays post-1.0. *Source:* H C12, H08; I A6.

### 5.10 Persistence footprint and acceptance

- **REQ-037** (pre-1.0, docs and test, DEC-008) The 1.0 scope, ADR-019 and the package READMEs state that the nine event-sourced compliance modules and `Encina.Marten.GDPR` require PostgreSQL through Marten. Applications that keep their own data in EF Core or Dapper on the same PostgreSQL have a documented, tested pattern that keeps application writes and compliance-aggregate writes consistent: a shared connection and transaction, or an outbox bridge.
  *Rationale:* creating a patient in EF Core and starting its retention record or consent in Marten are dual writes today; either can fail and leave untracked personal data. *Source:* F C32, G23; G N6, L12.
- **REQ-038** (pre-1.0) The PracticeManagement reference scenario (§4.3) exists, its README maps it to ConsultaPsicologica, it runs in CI Full, and its Phase A scenarios pass. Phase B scenarios need no breaking change to a public API shipped in 1.0.
  *Source:* G §5; I (e).

### 5.11 Post-1.0 facilitators

Capabilities outside the 1.0 contract unless a decision promotes them. They are listed so that the pre-1.0 shape changes leave room for them.

| REQ | Capability | Source | Tracking |
|---|---|---|---|
| REQ-039 | `Encina.Http`: outbound auth providers from secrets, client certificates and mTLS, per-client rate limiter honouring Retry-After, coded error mapping, redacting logging, OpenTelemetry | I C.3 P1 | P-31 (DEC-004) |
| REQ-040 | `Encina.Security.OAuth`: encrypted token store on all 10 providers, single-flight refresh through `IDistributedLockProvider`, grant-revoked notification through the outbox, PKCE callback helper | I C.3 P1 | P-32 (DEC-004) |
| REQ-041 | `Encina.Notifications` and channel satellites: consent-aware multi-channel delivery, templates, delivery status, quiet hours, deduplication | G N9; I C.3 P1 | P-33 (DEC-004) |
| REQ-042 | Ordered outbox partitions with pacing (a nullable `PartitionKey`; the column may move pre-1.0 under DEC-005) | H H10; I C.3 P1 | #469 |
| REQ-043 | Domain events written to the outbox in the same commit on all 10 providers | I C.3 P2 | P-34 |
| REQ-044 | Saga correlation by external key and a wait-for-event step with timeout and compensation | I C.3 P2 | P-35 |
| REQ-045 | `ISequenceGenerator`: gapless per key, enlisted in the ambient transaction, on all 10 providers | G N7; H H07; I C.3 P2 | P-36 |
| REQ-046 | `Encina.Storage` (`IBlobStore`) with envelope encryption and retention, hold and blocking hooks | G N10; I C.3 P3 | P-37 (related #600, #311) |
| REQ-047 | Persistent hash-chained log with a pluggable hasher and a chain per partition, Attestation built on it, no Verifactu semantics | F G08, G21; I C.3 P3 | P-38 (related #860) |
| REQ-048 | Trusted time-stamping (RFC 3161, eIDAS qualified) and e-signature integration points | F G17 | P-39 |
| REQ-049 | Breach notifier channels: Art. 33(3) submission package and tracking of Art. 34 communications | F G16 | P-40 |
| REQ-050 | DPIA screening mapped to the AEPD Art. 35.4 and 35.5 lists | F G15 | P-41 (related #816, #686) |
| REQ-051 | Break-the-glass access with mandatory justification, time box and review | F G19 | P-42 |
| REQ-052 | EHDS readiness: logging component and EEHRxF export as extension points, before the implementing acts of 26 Mar 2027 | F G20 | P-43 (related #808) |
| REQ-053 | X.509 certificate retrieval in `Encina.Security.Secrets`, with expiry health check and rotation | H H06 | P-44 |
| REQ-054 | Relational `IPersonalDataLocator` for EF Core, Dapper and ADO.NET on all 10 providers | G N4 | P-45 |
| REQ-055 | Column-level encryption at rest through ORM value converters with KMS-wrapped keys, on all 10 providers | F G18 | P-46 (related #857) |
| REQ-056 | Row claiming (`SKIP LOCKED`, `READPAST`) in outbox and scheduler processors for multi-instance hosts | I (b) | P-47 |
| REQ-057 | ISO/IEC 27001 and 27701 control-mapping documents | C §6, §8 | P-48 |
| REQ-058 | VEX statements for dependency vulnerabilities not exploitable through Encina | A §4 item 2 | P-49 |

## 6. Coverage matrix

Status on 2026-09-23: **Covered**, **Partial**, **Missing** or **Conflict** (would block or undermine compliance). Paths are relative to the repository root; "P-nn" are proposed issues (§13).

| REQ | Package / type | Status | Evidence | Tracking |
|---|---|---|---|---|
| REQ-001 | `Encina.Compliance.Retention` policies | Conflict | `src/Encina.Compliance.Retention/Services/DefaultRetentionRecordService.cs`; `Model/RetentionPolicyType.cs` (`EventBased` unused) | P-01 |
| REQ-002 | `RetentionEnforcementService` | Conflict | `src/Encina.Compliance.Retention/RetentionEnforcementService.cs` | #1143 |
| REQ-003 | `RetentionEnforcementService`, `RetentionRecordAggregate` | Conflict | `src/Encina.Compliance.Retention/Aggregates/RetentionRecordAggregate.cs` | #1142, #1146, #770 |
| REQ-004 | `DefaultDataErasureExecutor` | Conflict | `src/Encina.Compliance.DataSubjectRights/Erasure/DefaultDataErasureExecutor.cs` | P-02 |
| REQ-005 | none (closest: `ProcessingRestrictionPipelineBehavior`, legal hold) | Missing | `src/Encina.Compliance.DataSubjectRights/ProcessingRestrictionPipelineBehavior.cs` | P-03 |
| REQ-006 | `CryptoShredErasureStrategy`, `ISubjectKeyProvider` | Conflict | `src/Encina.Marten.GDPR/Erasure/CryptoShredErasureStrategy.cs`; `src/Encina.Marten.GDPR/Abstractions/ISubjectKeyProvider.cs` | #1144, P-04 |
| REQ-007 | `AuditedRepository`, `ReadAuditOptions`, read-audit stores | Partial | `src/Encina.Security.Audit/AuditedRepository.cs`; `src/Encina.Security.Audit/ReadAuditOptions.cs` | P-05, #1128, #1129, #1135, #767, #751 |
| REQ-008 | `AuditPipelineBehavior` | Missing | `src/Encina.Security.Audit/AuditPipelineBehavior.cs` | P-06 |
| REQ-009 | `DefaultDSRService`, `AccessResponse`, `[PersonalData]` | Missing | `src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs`; `Model/AccessResponse.cs`; `Attributes/PersonalDataAttribute.cs` | P-07 |
| REQ-010 | `LawfulBasis`, `ProcessingActivity`, RoPA exporters | Missing | `src/Encina.Compliance.GDPR/Model/LawfulBasis.cs`; `Model/ProcessingActivity.cs` | P-08 |
| REQ-011 | Consent and DSR aggregates | Missing | no guardian, representative or deceased concept in `src/` | P-09 |
| REQ-012 | `RegionRegistry`, `TransferBasis` | Conflict | `src/Encina.Compliance.DataResidency/Model/RegionRegistry.cs`; `src/Encina.Compliance.CrossBorderTransfer/Model/TransferBasis.cs` | #1145 |
| REQ-013 | `Processor`, `[RequiresProcessor]` | Partial | `src/Encina.Compliance.ProcessorAgreements/Model/Processor.cs` | P-10 |
| REQ-014 | `ConsentPurposes`, `ConsentRequiredPipelineBehavior` | Partial | `src/Encina.Compliance.Consent/Model/ConsentPurposes.cs`; `src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs` | P-11, #810, #811 |
| REQ-015 | `Encina`, `RequestContext`, `EncinaContextMiddleware`, `AuthorizationPipelineBehavior` | Conflict | `src/Encina/Core/Encina.cs`; `src/Encina/Core/RequestContext.cs`; `src/Encina.AspNetCore/EncinaContextMiddleware.cs`; `src/Encina.AspNetCore/AuthorizationPipelineBehavior.cs` | #1147, #1148 |
| REQ-016 | Consent and DSR subject extraction | Conflict | `src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs`; `src/Encina.Compliance.DataSubjectRights/DefaultDataSubjectIdExtractor.cs` | #1149 |
| REQ-017 | Outbox orchestrator and processors, Hangfire adapters, scheduler | Conflict | `src/Encina.Messaging/Outbox/OutboxOrchestrator.cs`; `src/Encina.EntityFrameworkCore/Outbox/OutboxProcessor.cs`; `src/Encina.Hangfire/HangfireRequestJobAdapter.cs`; `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs` | #1150, #1151, #1152, #1153, #1154 |
| REQ-018 | `IOutboxStore`, scheduled-message stores | Missing | `src/Encina.Messaging/Outbox/IOutboxStore.cs` (no purge); `src/Encina.Messaging/Inbox/InboxOptions.cs` (inbox parity) | P-12 |
| REQ-019 | `HMACValidationPipelineBehavior`, `PIIMaskingPipelineBehavior` | Conflict | `src/Encina.Security.AntiTampering/Pipeline/HMACValidationPipelineBehavior.cs`; `src/Encina.Security.PII/PIIMaskingPipelineBehavior.cs` | #1155, P-13, #858 |
| REQ-020 | `EncryptedField`, `EncryptionPipelineBehavior` | Partial | `src/Encina.Security.Encryption/EncryptionPipelineBehavior.cs` | #857 |
| REQ-021 | `HashChainAttestationProvider` | Conflict (docs) | `src/Encina.Compliance.Attestation/Providers/HashChainAttestationProvider.cs` | #860 |
| REQ-022 | 15 compliance packages | Missing | `docs/specifications/` holds SPEC-000 to SPEC-002 only | P-17 |
| REQ-023 | Compliance READMEs, ADR-019, integration tests | Conflict (docs) | `docs/architecture/adr/019-compliance-event-sourcing-marten.md`; package READMEs | P-14, P-15, P-16, #689, #1090 |
| REQ-024 | Omnibus adaptations | Missing (not yet built) | #810–#816 open | acceptance criterion added to #810–#816 |
| REQ-025 | This specification, §3 | Partial | §3 of this document | #104 |
| REQ-026 | Licence and distribution model | Covered (statement missing) | `LICENSE`; no funding file, no telemetry | P-27 |
| REQ-027 | SBOM workflow | Partial | `.github/workflows/sbom.yml` (core package only, workflow artifact) | P-26 |
| REQ-028 | SECURITY.md | Partial | `SECURITY.md` | P-27 |
| REQ-029 | Advisory process | Partial | `SECURITY.md` promises GHSA; none published | P-28 |
| REQ-030 | Publishing | Missing | GitHub Packages only; no signing or attestation | #92, #93, #100, #101, P-29 |
| REQ-031 | Outbox processors | Missing | `src/Encina.EntityFrameworkCore/Outbox/OutboxProcessor.cs` | P-18, #733 |
| REQ-032 | Outbox and scheduler type names | Missing | assembly-qualified names in `OutboxOrchestrator` and `SchedulerOrchestrator` | P-19, #134 |
| REQ-033 | Scheduling | Missing | `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs`; `src/Encina.EntityFrameworkCore/Scheduling/IMessageScheduler.cs` | P-20, #146, #148, #150 |
| REQ-034 | Refit handler, Polly retry, error classifier | Conflict | `src/Encina.Refit/Handlers/RestApiRequestHandler.cs`; `src/Encina.Polly/Behaviors/RetryPipelineBehavior.cs`; `src/Encina.Messaging/Recoverability/IErrorClassifier.cs` | P-21 |
| REQ-035 | Core registration | Conflict (house rule) | `src/Encina/Core/ServiceCollectionExtensions.cs` | P-22 |
| REQ-036 | AntiTampering, inbox | Partial | `src/Encina.Security.AntiTampering/HMAC/SignatureComponents.cs` | #731, #734 (#202 post-1.0) |
| REQ-037 | Marten compliance modules, EF Core, Dapper | Partial | `docs/architecture/adr/019-compliance-event-sourcing-marten.md`; no shared EF and Marten transaction path | P-14, P-23 |
| REQ-038 | Reference scenario | Missing | — | P-25 |
| REQ-039 – REQ-058 | Post-1.0 facilitators (§5.11) | Missing | — | P-31 – P-49, #469 |

## 7. Acceptance criteria

| AC | Requirement | Criterion |
|---|---|---|
| AC-001 | REQ-001 | A policy with a 5-year floor anchored to an application event refuses erasure before the floor, re-anchors when a new episode opens and computes calendar years across a leap day; an immutable-record class rejects updates and accepts a corrective record; S14 passes. |
| AC-002 | REQ-002 | #1143 closed with a test in which the hold lookup returns `Left`: nothing is erased, the failure is logged and metered, and the record is retried on the next cycle. |
| AC-003 | REQ-003 | #1142 and #1146 closed; an integration test runs two sweep cycles and observes one erasure and one deleted transition per record; `Encina.Compliance.Retention` has no `DateTime.UtcNow` or `DateTimeOffset.UtcNow`. |
| AC-004 | REQ-004 | An erasure request for a subject with held data under a floor returns per-category results (erased; refused with the Art. 17(3) exemption and a grantable-after date) and touches no held data; S14 passes. |
| AC-005 | REQ-005 | Blocked data is absent from normal relational and Marten queries, versions superseded by a rectification are blocked, an authority disclosure writes an audit entry with purpose and role, and destruction happens at the end of the period under `FakeTimeProvider`; S14 passes. |
| AC-006 | REQ-006 | #1144 closed; shredding the marketing category leaves the clinical category readable; shredding under a floor or hold is refused; a production registration without a durable key store fails at start-up or is an explicit, logged opt-out; S16 passes. |
| AC-007 | REQ-007 | Tests show: a store failure with fail-closed fails the read; collection reads record ids; `RequirePurpose` rejects when so configured; the per-subject query returns the entries; the special-category default retention is at least 3 years; #1128, #1129 and #1135 closed; S13 passes. |
| AC-008 | REQ-008 | A Dapper query handler that declares its subjects produces one read-audit entry per subject; S13 passes. |
| AC-009 | REQ-009 | [Tier B] An access export omits a field marked as subjective annotation and returns the reason; S15 passes. |
| AC-010 | REQ-010 | The RoPA JSON and CSV exports carry the Art. 9(2) condition and the national basis; a special-category activity without an Art. 9(2) condition fails validation; S18 passes. |
| AC-011 | REQ-011 | [Tier B] Consent and DSR events carry acted-by, acted-for, authority and validity; a deceased subject's prohibition refuses a relative's request; S15 passes. |
| AC-012 | REQ-012 | #1145 closed; S17 transfer checks pass. |
| AC-013 | REQ-013 | A vendor registered as independent controller for a purpose makes `[RequiresProcessor]` on that purpose refuse or warn; "no Art. 28 terms" appears in the RoPA export; S17 passes. |
| AC-014 | REQ-014 | Channel-scoped refusal and grant, the art. 21.2 basis and the transactional marker are tested; the Consent specification has GDPR, ePrivacy and LSSI tables; S12 passes. |
| AC-015 | REQ-015 | #1147 and #1148 closed; S0 passes from a minimal API, a Blazor Server circuit and a Hangfire job. |
| AC-016 | REQ-016 | #1149 closed; a `Guid` subject resolves; an unreadable declared subject returns a coded error; S12 passes. |
| AC-017 | REQ-017 | #1150 – #1154 closed; per provider family, a handler failing twice and then succeeding is delivered once, and exhaustion produces a log entry, a metric, a degraded health check and a dead-letter state; S2 passes. |
| AC-018 | REQ-018 | The purge operation exists on the outbox and scheduled-message stores and is integration-tested on the 10 providers; S21 passes. |
| AC-019 | REQ-019 | #1155 closed with fail-closed as the default; a PII masking failure with fail-closed on returns `Left`; the operator receives redacted notes; S0 and S20 (phase A) pass. |
| AC-020 | REQ-020 | #857 closed, `EncryptedField` consumed or removed. |
| AC-021 | REQ-021 | #860 closed; README and Attestation specification match the behaviour. |
| AC-022 | REQ-022 | Fifteen SPEC-NNN files exist under `docs/specifications/` with the table of REQ-022; each package README links its specification; the SPEC-001 citation check stays green. |
| AC-023 | REQ-023 | Every type named in a compliance README exists in the package's `PublicAPI.Shipped.txt` or `PublicAPI.Unshipped.txt`; ADR-019 amended; READMEs exist for the four packages; the four packages have integration tests or a justification file. |
| AC-024 | REQ-024 | Each of #810–#816 closes with the option off by default and a test asserting that the default follows current law. |
| AC-025 | REQ-025 | The §3 verification date is within 30 days of each minor tag and of `1.0.0-rc.1`; #104 lists the step. |
| AC-026 | REQ-026 | SECURITY.md or README carries the legal-status statement; the release review confirms that no package sends data to the maintainer and that no release asset is gated. |
| AC-027 | REQ-027 | The `1.0.0-rc.1` release has one SBOM per published package attached. |
| AC-028 | REQ-028 | SECURITY.md contains the five elements of REQ-028. |
| AC-029 | REQ-029 | SECURITY.md or CONTRIBUTING.md documents the advisory process; the first security fix after approval has a GHSA with a CVE. |
| AC-030 | REQ-030 | `1.0.0-rc.1` is published through Trusted Publishing with the prefix reserved; the Scorecard workflow exists; #92, #93, #100 and #101 closed. |
| AC-031 | REQ-031 | A notification with two handlers where the second fails is retried and only the second handler runs again; both handlers' errors are aggregated; S1 passes. |
| AC-032 | REQ-032 | Stored rows carry the registry name; renaming the CLR type while keeping the name still dispatches; an upcaster migrates a v1 payload. |
| AC-033 | REQ-033 | If DEC-005 (a): upsert and cancel by key work on the 10 providers, and a 09:00 Europe/Madrid recurring job fires at 09:00 local time on both sides of a DST change; S4 passes. |
| AC-034 | REQ-034 | Outbound errors carry codes and metadata and no response body; a 400 is not retried; a 429 waits for Retry-After; S2 passes. |
| AC-035 | REQ-035 | `AddApplicationMessaging` no longer appears in any `PublicAPI` file. |
| AC-036 | REQ-036 | #731 and #734 closed; Stripe-, Meta- and Redsys-style schemes verify by configuration alone; S5 passes. |
| AC-037 | REQ-037 | ADR-019 and the READMEs state the PostgreSQL requirement; the consistency pattern is documented and S19 passes. |
| AC-038 | REQ-038 | All Phase A scenarios are green in CI Full on the release-candidate commit. |
| AC-039 | REQ-039 – REQ-058 | At approval, each has an open issue in a post-1.0 milestone, or a decision that promotes it. |
| AC-040 | This specification | It moves to APPROVED when DEC-001 … DEC-013 carry a human decision (DEC-001 may stay PROVISIONAL), the EPIC and the P-issues exist, and the row in `docs/specifications/README.md` shows the new status. |

## 8. Constraints

- `CLAUDE.md` rules apply to every change made for this specification: time from `TimeProvider`; secrets never leak through serialization or `ToString()`; asynchronous database calls with `CancellationToken`; no `[Obsolete]`, no legacy aliases.
- Provider coherence: a feature that touches stores ships on all 10 database providers, except the Marten-only compliance modules of ADR-019, whose scope DEC-008 states.
- Cross-cutting integration (ADR-018), EventId ranges (ADR-021) and the per-flag coverage model apply; documentation cites coverage through SPEC-001 markers, never by hand.
- Opt-in by default for features, safe by default for security- and compliance-relevant behaviour (INV-005).
- Pre-1.0: breaking changes are free and preferred over compatibility layers.
- No regulation-specific code that would make the maintainer a regulated party without a decision (INV-004).

## 9. Invariants

- **INV-001** No Encina default or behaviour silently destroys data an application must retain, or discloses data it must withhold, without an explicit application opt-in.
- **INV-002** Encina never claims coverage of an article it does not cover; partial coverage is stated as partial.
- **INV-003** Default behaviour follows the law in force on the verification date of §3; draft law is opt-in only.
- **INV-004** Encina ships no code whose purpose is a single regulation that would make its maintainer a CRA or PLD manufacturer, an open-source steward or a Verifactu producer, unless a DEC accepts that role.
- **INV-005** Security- and compliance-relevant behaviours fail closed unless an explicit, logged opt-out says otherwise.
- **INV-006** Agents do not change this document once approved; they propose changes as a pull request with the human as approver (as SPEC-000 INV-005).

## 10. Verification

| Requirement group | Method |
|---|---|
| Data lifecycle (REQ-001 – REQ-006) | Unit and property tests on the aggregates; Marten integration tests; reference scenarios S14 and S16 |
| Access transparency (REQ-007 – REQ-009) | Read-audit integration tests on the 10 providers and Marten; S13, S15 |
| Legal modelling (REQ-010 – REQ-014) | Unit and contract tests on the models and behaviours; RoPA export snapshot; S12, S17, S18 |
| Identity and context (REQ-015, REQ-016) | ASP.NET Core, Blazor Server and Hangfire integration tests; S0, S11 |
| Messaging (REQ-017, REQ-018, REQ-031 – REQ-034) | Provider integration tests with fault injection; S1, S2, S4, S21 |
| Security defaults (REQ-019 – REQ-021) | Unit tests of default options; S0, S20 |
| Documentation (REQ-022 – REQ-025) | Review against the article tables; README type-name check; SPEC-001 citation gate; law-map date check at each tag |
| Supply chain (REQ-026 – REQ-030) | Release artifacts of `1.0.0-rc.1`; SECURITY.md review; evidence report of SPEC-000 REQ-022 |
| Shape changes, persistence and acceptance (REQ-035 – REQ-038) | PublicAPI files; S5, S19; CI Full run of the reference scenario |

## 11. Decisions for the maintainer

Class C decisions (`AI-DEVELOPMENT-MODEL.md` §14). Agents present the options and a recommendation; the maintainer decides. Once decided, each becomes a line in this table or a short ADR, and the specification can move to APPROVED.

| ID | Decision | Options | Recommendation | Consequences | Status |
|---|---|---|---|---|---|
| **DEC-001** | Tier of the reference application | (a) Tier A: the charter as written, no clinical data; (b) Tier B: clinical records as well | (b) as the working assumption, settled after a cost study | Tier B keeps REQ-009 and REQ-011 (and S15) in the 1.0 gate. A later move to Tier A drops them and nothing else | **PROVISIONAL** — maintainer, 2026-09-23: Tier B; cost study pending ("going down is easier than going up") |
| **DEC-002** | Verifactu-specific code in Encina | (a) none in 1.0, regulation-neutral primitives only; (b) a separate package or repository; (c) inside Encina | (a). A spike (P-24) drafts an ADR on producer liability and the question for the AEAT; revisit (b) only with a written AEAT answer and an accepted producer role | The application builds its Verifactu module (VERI\*FACTU mode recommended: no XAdES, no event log) and its producer signs the *declaración responsable* listing Encina as a dependency; Encina avoids a possible component-producer exposure of €150,000 per year [S] | PROPOSED |
| **DEC-003** | Payments abstraction | (a) none; (b) an `Encina.Payments` package | (a): gateways are vendor-shaped (Stripe API against the Redsys signed redirect) and a common abstraction adds little | The application keeps its own gateway abstraction; Encina offers outbox, inbox, sagas, locks and webhook ingestion (REQ-036) | PROPOSED |
| **DEC-004** | First post-1.0 block | (a) `Encina.Http`, `Encina.Security.OAuth`, `Encina.Notifications`, plus ordered partitions (#469); (b) resume the existing post-1.0 milestone order; (c) no block | (a), in the order of the application's phases | P-31 – P-33 and #469 form one post-1.0 milestone | PROPOSED |
| **DEC-005** | Scheduling slices and `PartitionKey` in 1.0 | (a) promote REQ-033 (the minimal slices of #146, #148, #150) and add a nullable `PartitionKey` column now, with partition behaviour post-1.0; (b) keep all of them post-1.0 | (a): schema and signature changes across 10 providers are free now and breaking later | More work in v0.14.0; S4 joins Phase A | PROPOSED |
| **DEC-006** | Fail-closed security defaults | (a) fail closed by default, with an explicit, logged opt-out (#1155, P-13); (b) keep fail-open defaults and add an opt-in | (a) | Anyone relying on a silent skip breaks; nobody is affected pre-1.0 | PROPOSED |
| **DEC-007** | Home of the reference scenario | (a) the Encina repository (`tests/Encina.ReferenceScenarios.PracticeManagement/`), run in CI Full; (b) the ConsultaPsicologica repository against published packages; (c) the gate in Encina and the full application in its own repository | (a): the gate must run where the code changes, and the application repository is private and paused | CI Full grows; the scenario uses thin app-style adapters and copies no application code | PROPOSED |
| **DEC-008** | Marten next to EF Core and Dapper; ADR-019 scope for 1.0 | (a) state that the nine event-sourced compliance modules are PostgreSQL/Marten-only in 1.0 and document EF/Dapper and Marten coexistence (REQ-037); (b) build relational stores for them before 1.0; (c) leave it unstated | (a): the reference application already uses PostgreSQL 16; (b) is large | SQL Server and MySQL users do not get Consent, DSR, Retention, Breach, DPIA or crypto-shredding in 1.0, and are told so | PROPOSED |
| **DEC-009** | Tenancy of the reference application | (a) single practice, per-professional scoping through ABAC; (b) multi-practice SaaS | (a) for the 1.0 gate, keeping the two-tenant check of S11 | (b) keys OAuth tokens, gateway credentials, invoice series and senders per tenant and would promote tenancy work (#798, #747) | PROPOSED |
| **DEC-010** | Keep Encina non-commercial | (a) keep: no price, no gating, no mandatory telemetry; (b) a paid edition or paid support as a condition | (a) | (a) keeps Encina outside the CRA and the PLD. (b) makes the maintainer a CRA manufacturer (Art. 14 reporting at once, full CRA from 11 Dec 2027) and a PLD manufacturer | PROPOSED |
| **DEC-011** | Promote the compliance-lifecycle capabilities into 1.0 | (a) promote REQ-001, REQ-005, REQ-007, REQ-008, REQ-009 (Tier B), REQ-010, REQ-011 (Tier B), REQ-013, REQ-014 and REQ-018; (b) promote only the retention and erasure lifecycle (REQ-001, REQ-005); (c) keep them post-1.0 and document the gaps | (a): each changes a persisted format or a public contract, and without them the shipped modules block the reference application | About ten feature issues join the 1.0 contract; SPEC-000 INV-004 is met by citing this specification | PROPOSED |
| **DEC-012** | Omnibus draft provisions | (a) #810–#816 ship off by default while COM(2025) 837 is not adopted (REQ-024); (b) ship them as defaults | (a) | Clarifies SPEC-000 REQ-026; a pull request to SPEC-000 or a note in its §9 records it | PROPOSED |
| **DEC-013** | Tracking structure | (a) a new EPIC "EU regulatory readiness (SPEC-002)" with the P-issues as children; #880 and #881 kept as they are and linked as related; #873 cross-linked; (b) spread the children over #880, #881 and #873 | (a) | One place to follow SPEC-002; the scope of the existing EPICs does not change | PROPOSED |

### 11.1 Application decisions that do not block Encina

Open questions from note I and note H. They are ConsultaPsicologica's to take; the Encina consequence, where there is one, is stated.

| Question | Options | Encina consequence |
|---|---|---|
| Scheduler for business messages | Encina scheduling, or Hangfire when its dashboard is essential | None; both are supported (Hangfire after #1152) |
| E-mail provider | Microsoft 365 through Graph, SMTP, a transactional provider (a consumer account has no DPA, H-R33) | Informs the first channel satellite of REQ-041 |
| Payment gateway and invoice timing | Stripe or Redsys (native Bizum); invoice before or after payment; how session packs are invoiced | None (DEC-003) |
| Single or multiple instances | One VPS or several | With several instances, row claiming (REQ-056, P-47) becomes pre-1.0 |
| Verifactu approach | Own SIF in VERI\*FACTU mode; the AEAT's free application; a certified third party | None (DEC-002); informs P-24 |
| Producer and operator of the application | The maintainer supplies and hosts it, or the practitioner owns it | None; decides who signs the *declaración responsable* and whether a processor contract is needed |
| Patient messaging channel | E-mail and SMS through EU processors first, WhatsApp Cloud API later | Informs REQ-014 test data and REQ-041 |
| LanguageExt v5 | Before or after 1.0 | An Encina-wide question outside this specification |
| Resume before or after the post-1.0 rename | — | The rename breaks the application's namespaces again |

## 12. Open questions

1. Does the practice treat minors? If not, REQ-011 can move post-1.0 even under Tier B.
2. The cost study behind DEC-001.
3. AEPD PD-00068-2026 was read through a law-firm summary [S]; the original resolution should be read before REQ-007 is finalised.
4. EHDS scope for an in-house psychology EHR: whether psychology notes carry priority-category data depends on implementing acts due by 26 Mar 2027.
5. Limitation periods that end the blocked state (Código Civil art. 1964.2, 5 years; LOPDGDD art. 72 ff.) are [K]; regional retention periods (Catalonia and others) are [S].
6. Tax retention for a liberal professional: LGT 4 years only, or Código de Comercio art. 30 (6 years) as well [S].
7. Verifactu: whether a free, independently versioned library implementing RRSIF functions needs its own *declaración responsable* (note H U1); whether an application forwarding billing data to an external SIF is itself a component (U3); the exact wording of LGT art. 201 bis (U2).
8. Whether the 15 REQ-022 specifications are tracked as 15 issues or as one issue with a checklist.
9. No Spanish PLD transposition bill and no CRA penalty law were found; the searches may have missed a recent *anteproyecto*.
10. The DSA size thresholds were not re-verified [K]; irrelevant unless the application hosts user content.
11. The DPF certification status of Google, Stripe, Meta and Microsoft was not checked; it affects S17 test data, not REQ-012.

## 13. Tracking plan

### 13.1 EPIC

**P-00** `[EPIC] EU regulatory readiness (SPEC-002)`, parent of every P-issue below and of the existing issues listed in §13.3 that have no EPIC. No milestone (it spans v0.14.0 – v0.19.0 and post-1.0). Relation to existing EPICs (DEC-013):

- **#880** (v0.15.0, NIS2 and Digital Omnibus) stays as it is under SPEC-000 REQ-026 and is linked as related. P-11 lives in v0.15.0 next to #810 and #811, and REQ-024 adds one acceptance criterion to each of #810–#816 (comment, no new issue).
- **#881** (v0.16.0, AI Act) stays as it is under SPEC-000 REQ-025 and is linked as related. A comment asks #836–#847 to use the dates of Reg. (EU) 2026/1744: Art. 4 replaced, Art. 5(1)(ba)/(bb) from 2 Dec 2026, Art. 50(2) grace to 2 Dec 2026, Annex III from 2 Dec 2027, Annex I from 2 Aug 2028.
- **#873** (v0.14.0, Compliance Completion and Test Coverage) is cross-linked from P-15, P-16 and P-17.
- **#804–#808** (post-1.0 milestone #18) stay post-1.0. Comments correct their facts: #804 (RTS 2025/301, RTS 2025/1190), #805 (24 Dec 2027, micro and small exemption), #806 (DGA repeal proposed; Art. 29 and Chapter IV dates), #807 (73 measures, five dimensions, in force 5 May 2022), #808 (in force 25 Mar 2025; 2029 and 2031 dates; relation to P-43).

Priorities use the P0 – P3 scale of `ENCINA-1.0-RECONCILIATION.md` §4.

### 13.2 New issues proposed (not opened)

| P-id | Proposed title | Type | Priority | Milestone | REQ |
|---|---|---|---|---|---|
| P-01 | Retention: minimum-retention floor and event-anchored start (episode discharge, fiscal year, death), per jurisdiction and document type, immutable-record class | `[FEATURE]` | P0 | v0.14.0 — Hardening | REQ-001 |
| P-02 | DSR erasure ignores retention policies and legal holds; return a reasoned partial refusal (Art. 17(3)) with a grantable-after date | `[BUG]` | P0 | v0.14.0 | REQ-004 |
| P-03 | Blocked data state (LOPDGDD art. 32 *bloqueo*): hidden from processing and viewing, audited authority disclosure, scheduled destruction, relational blocking strategy | `[FEATURE]` | P0 | v0.14.0 | REQ-005 |
| P-04 | Marten.GDPR subject key store defaults to in-memory and keeps keys unwrapped | `[DEBT]` | P0 | v0.14.0 | REQ-006 |
| P-05 | Evidential read audit: fail-closed option, ids for collection reads, enforced purpose, special-category retention of at least 3 years, per-subject access query | `[FEATURE]` | P0 | v0.14.0 | REQ-007 |
| P-06 | Query-level read audit with data-subject resolution for reads outside Encina repositories | `[FEATURE]` | P0 | v0.14.0 | REQ-008 |
| P-07 | DSR access export with field-level exclusions and withholding reasons | `[FEATURE]` | P0 (Tier B) | v0.14.0 | REQ-009 |
| P-08 | Model GDPR Art. 9(2) conditions and national legal bases next to the Art. 6 basis (RoPA, LawfulBasis, processing activities) | `[FEATURE]` | P0 | v0.14.0 | REQ-010 |
| P-09 | Data-subject representation (minors, guardians, proxies) and deceased status in Consent and DSR records | `[FEATURE]` | P0 (Tier B) | v0.14.0 | REQ-011 |
| P-10 | ProcessorAgreements: vendor role per purpose and "no Art. 28 terms available" status | `[FEATURE]` | P0 | v0.14.0 | REQ-013 |
| P-11 | Consent: channel-scoped consent, LSSI art. 21.2 existing-client basis with per-message opt-out, transactional or commercial marker | `[FEATURE]` | P0 | v0.15.0 — EU Compliance: NIS2 & Digital Omnibus | REQ-014 |
| P-12 | Outbox and scheduled messages: purge processed messages after a configurable retention period on all 10 providers | `[FEATURE]` | P0 | v0.14.0 | REQ-018 |
| P-13 | PIIMaskingPipelineBehavior returns the unmasked response when masking fails; add fail-closed and role- or purpose-aware redaction | `[DEBT]` | P0 | v0.14.0 | REQ-019 |
| P-14 | Compliance READMEs and ADR-019 describe types and provider coverage that do not exist; state the PostgreSQL/Marten requirement | `[DEBT]` | P0 | v0.14.0 | REQ-023, REQ-037 |
| P-15 | Missing READMEs for Anonymization, PrivacyByDesign, CrossBorderTransfer and Security.Audit | `[DEBT]` | P0 | v0.18.0 — Documentation | REQ-023 |
| P-16 | Integration tests or written justification for AIAct, Attestation, DataSubjectRights and GDPR | `[TEST]` | P0 | v0.17.0 — Providers & Testing | REQ-023 |
| P-17 | Article-coverage specification for `Encina.Compliance.<Package>` (one issue per package, 15 in total) | `[DEBT]` | P0 | v0.18.0 | REQ-022 |
| P-18 | Outbox dispatch: outbox id as idempotency key, per-handler idempotency, aggregated handler errors | `[FEATURE]` | P0 | v0.14.0 | REQ-031 |
| P-19 | Stable persisted message type names and payload versioning for outbox, inbox and scheduled messages | `[FEATURE]` | P0 | v0.14.0 | REQ-032 |
| P-20 | Scheduling minimal slices: schedule key, time-zone-aware cron, `ICronParser`, orphan `IMessageScheduler` | `[FEATURE]` | P0 if DEC-005 (a), else P2 | v0.14.0 | REQ-033 |
| P-21 | Outbound error taxonomy and Retry-After-aware retry classification | `[DEBT]` | P0 | v0.14.0 | REQ-034 |
| P-22 | Remove the legacy `AddApplicationMessaging` alias | `[DEBT]` | P0 | v0.14.0 | REQ-035 |
| P-23 | Consistency between EF Core or Dapper application data and Marten compliance aggregates on one PostgreSQL | `[SPIKE]` | P0 | v0.14.0 | REQ-037 |
| P-24 | Verifactu producer liability: can Encina ship RRSIF-relevant components as FOSS? Decide the boundary in an ADR | `[SPIKE]` | P1 | v0.14.0 | DEC-002 |
| P-25 | PracticeManagement reference scenario, Phase A | `[TEST]` | P0 | v0.17.0 | REQ-038 |
| P-26 | Per-package SBOM attached to every GitHub Release | `[INFRA]` | P1 | v0.19.0 — Release Engineering | REQ-027 |
| P-27 | SECURITY.md: support period, response targets, scope, CRA Art. 13(6) channel, legal-status statement, cryptographic primitives | `[INFRA]` | P1 | v0.19.0 | REQ-026, REQ-028 |
| P-28 | Security advisory process: GHSA with CVE for every fix, security history in the changelog | `[INFRA]` | P1 | v0.19.0 | REQ-029 |
| P-29 | nuget.org Trusted Publishing, package-ID prefix and OpenSSF Scorecard | `[INFRA]` | P1 | v0.19.0 | REQ-030 |
| P-30 | `CLAUDE.md` lists `Encina.Extensions.Http.Resilience`, which does not exist | `[DEBT]` | P1 | v0.14.0 | SPEC-000 REQ-001 |
| P-31 | `Encina.Http`: outbound HTTP client with auth providers, client certificates, rate limiting and coded errors | `[FEATURE]` | P2 | first post-1.0 block (DEC-004) | REQ-039 |
| P-32 | `Encina.Security.OAuth`: token store, single-flight refresh, revoked-grant notification | `[FEATURE]` | P2 | first post-1.0 block | REQ-040 |
| P-33 | `Encina.Notifications` and channel satellites | `[FEATURE]` | P2 | first post-1.0 block | REQ-041 |
| P-34 | Domain events to the outbox in the same commit on all 10 providers | `[FEATURE]` | P2 | Post-1.0: Modular Monolith Architecture | REQ-043 |
| P-35 | Saga correlation by external key and wait-for-event step | `[FEATURE]` | P2 | Post-1.0: Advanced Resilience & Caching Patterns | REQ-044 |
| P-36 | `ISequenceGenerator`: gapless sequence per key on all 10 providers | `[FEATURE]` | P2 | Post-1.0: Data Integrity & Cross-Cutting Functions | REQ-045 |
| P-37 | `Encina.Storage` (`IBlobStore`) with encryption, retention, hold and blocking hooks | `[FEATURE]` | P2 | Post-1.0: Persistent Stores Ecosystem | REQ-046 |
| P-38 | Persistent hash-chained log store, with Attestation built on it | `[FEATURE]` | P2 | Post-1.0: Compliance Completion & Test Coverage | REQ-047 |
| P-39 | Trusted time-stamping and e-signature integration points | `[FEATURE]` | P2 | Post-1.0: EU Regulatory Compliance (#18) | REQ-048 |
| P-40 | Breach notifier channels: Art. 33(3) package and Art. 34 communications | `[FEATURE]` | P2 | Post-1.0: Compliance Completion & Test Coverage | REQ-049 |
| P-41 | DPIA screening mapped to the AEPD Art. 35.4 and 35.5 lists | `[FEATURE]` | P2 | Post-1.0: Compliance Completion & Test Coverage | REQ-050 |
| P-42 | Break-the-glass access with justification, time box and review | `[FEATURE]` | P2 | Post-1.0: Compliance Completion & Test Coverage | REQ-051 |
| P-43 | EHDS readiness: logging component and EEHRxF export as extension points | `[SPIKE]` | P2 (before 26 Mar 2027) | Post-1.0: EU Regulatory Compliance (#18) | REQ-052 |
| P-44 | X.509 certificate retrieval in `Encina.Security.Secrets` | `[FEATURE]` | P2 | first post-1.0 block | REQ-053 |
| P-45 | Relational `IPersonalDataLocator` on all 10 providers | `[FEATURE]` | P2 | Post-1.0: Compliance Completion & Test Coverage | REQ-054 |
| P-46 | Column-level encryption at rest through ORM value converters on all 10 providers | `[FEATURE]` | P2 | Post-1.0: Compliance Completion & Test Coverage | REQ-055 |
| P-47 | Row claiming in outbox and scheduler processors for multi-instance hosts | `[FEATURE]` | P2 (P0 if multi-instance, §11.1) | Post-1.0: Data Integrity & Cross-Cutting Functions | REQ-056 |
| P-48 | ISO/IEC 27001 and 27701 control-mapping documents | `[FEATURE]` | P2 | Post-1.0: Quality & Documentation | REQ-057 |
| P-49 | VEX statements for dependency vulnerabilities | `[INFRA]` | P2 | Post-1.0: Release Preparation | REQ-058 |

### 13.3 Existing issues to link

| Issue | Subject | Milestone today | Proposed | REQ |
|---|---|---|---|---|
| #1142, #1143, #1146 | Retention sweep, fail-open hold, `UtcNow` | none | v0.14.0 | REQ-002, REQ-003 |
| #1144 | Crypto-shredding deletes every key of the subject | none | v0.14.0 | REQ-006 |
| #1145 | US adequacy | none | v0.14.0 | REQ-012 |
| #1147, #1148, #1149 | Empty request context, Blazor Server, `Guid` subject ids | none | v0.14.0 | REQ-015, REQ-016 |
| #1150, #1151, #1152, #1153, #1154 | Outbox abandonment and `Left` handling, Hangfire, recurring insert, MongoDB processor | none | v0.14.0 | REQ-017 |
| #1155 | HMAC validation fails open | none | v0.14.0 | REQ-019 |
| #1128, #1129, #1135 | Audit store defects | v0.14.0 | unchanged | REQ-007 prerequisites |
| #770, #767 | Retention and read-audit retention services to Encina scheduling | v0.14.0 | unchanged | REQ-003, REQ-007 |
| #860 | Attestation deferred review items | v0.14.0 | unchanged | REQ-021 |
| #857 | Orphan attributes, `EncryptedField` among them | Post-1.0: Critical Bugs & Quality Debt | v0.14.0 | REQ-020 |
| #858 | PII hash helper is not an HMAC | v0.14.0 | unchanged | REQ-019 |
| #751 | Audit trail for ABAC decisions | v0.14.0 | unchanged | REQ-007 (related) |
| #731, #734 | Inbound webhook validation and idempotency | v0.14.0 | unchanged; add the REQ-036 requirements as a comment | REQ-036 |
| #733 | Idempotency for notification processing | v0.14.0 | unchanged | REQ-031 (related) |
| #689, #1090 | Compliance documentation hub; unbacked claims | v0.14.0 | unchanged | REQ-022, REQ-023 |
| #810 – #816 | Omnibus adaptations | v0.15.0 | unchanged; add the REQ-024 criterion | REQ-024 |
| #836 – #847 | AI Act children | v0.16.0 | unchanged; date comment | — |
| #207, #208 | PostgreSQL and MySQL locks | v0.17.0 | unchanged | SPEC-000 REQ-027 |
| #92, #93, #100, #101, #104 | Provenance, signing, NuGet names and workflow, pre-release checklist | v0.19.0 | unchanged; #104 gains the law-map step | REQ-025, REQ-030 |
| #146, #148, #150 | Scheduling features | Post-1.0: Advanced Scheduling Features | unchanged; minimal slices in P-20 if DEC-005 (a) | REQ-033 |
| #469 | Partitioned sequential messaging | Post-1.0: AI/LLM Integration | first post-1.0 block (DEC-004) | REQ-042 |
| #134, #583, #149 | Message versioning; persistent dead-letter stores; scheduled dead letters | post-1.0 milestones | unchanged | REQ-032, REQ-017 (related) |
| #202 | WebHook support | Post-1.0: Web Advanced & Developer Experience | unchanged | REQ-036 (related) |
| #600, #311 | Claim-check blob stores; Claim Check | post-1.0 milestones | unchanged | REQ-046 (related) |
| #686, #687 | Compliance applicability resolver; rule engine | Post-1.0: Compliance Completion & Test Coverage | unchanged | REQ-050 (related) |
| #798, #747 | Tenancy for Security.Audit and ABAC; ModuleId in messaging | post-1.0 milestones | unchanged; revisit if DEC-009 (b) | DEC-009 |
| #804 – #808 | DORA, eIDAS 2, Data Act, ENS, EHDS | Post-1.0: EU Regulatory Compliance (#18) | unchanged; fact-correction comments | REQ-052 (for #808) |

Counts: 49 P-ids (P-17 stands for 15 issues, so 63 new issues) plus the EPIC P-00.

## 14. Traceability

| Research note | Requirements and decisions |
|---|---|
| A (Encina itself: CRA, PLD, export control) | §3.2; REQ-019, REQ-026 – REQ-030, REQ-058; DEC-010 |
| B, C, E (law map; E prevails) | §3; REQ-024, REQ-025; DEC-012 |
| D (inventory) | REQ-022, REQ-023; §13.1 |
| F G01 – G06 | REQ-001 – REQ-006 |
| F G07, G09, G12, G13 | REQ-007, REQ-009, REQ-010, REQ-012 |
| F G08, G21 | REQ-021, REQ-047; DEC-002 |
| F G10, G11 | REQ-011 |
| F G14, G23 | REQ-023, REQ-037; DEC-008 |
| F G15 – G20, G22 | REQ-050, REQ-049, REQ-048, REQ-055, REQ-051, REQ-052, REQ-003 |
| G N1, N2, N3 | REQ-015, REQ-016 |
| G N4, N5, N6, N7, N8, N9, N10, N11 | REQ-054, REQ-008, REQ-037, REQ-045, REQ-019, REQ-041, REQ-046, REQ-035 |
| G §5 (acceptance checks) | §4.3; REQ-038 |
| H H01 – H08, H10 | DEC-002 (P-24), REQ-017, REQ-018, REQ-014, REQ-013, REQ-053, REQ-045, REQ-036, REQ-042 |
| H H09 (integration guides) | Folded into the README of the reference scenario (P-25) |
| H D1 – D7 | DEC-002, DEC-003, DEC-004; §11.1 |
| I C1 – C4 | REQ-015, REQ-017 |
| I A1 – A10 | REQ-031 – REQ-036, REQ-019, P-30, REQ-021, SPEC-000 REQ-027 |
| I C.3 (post-1.0 satellites) | REQ-039 – REQ-047; DEC-003, DEC-004 |
| I (e), (f) | §4.3; DEC-001, DEC-005, DEC-007, DEC-008, DEC-009; §11.1 |

## 15. Change log

| Date | Change |
|---|---|
| 2026-09-23 | DRAFT created from research notes A–I; issues #1142 – #1155 were opened from the same study. DEC-001 recorded as PROVISIONAL by the maintainer. |

## 16. Related documents

- [SPEC-000 — Encina 1.0 Baseline and Release Scope](SPEC-000-encina-1.0-baseline-and-release-scope.md) (REQ-024 – REQ-026, DEC-002)
- [SPEC-001 — DocRef citations for coverage](SPEC-001-coverage-docref-citations.md)
- [AI-DEVELOPMENT-MODEL.md](../engineering/AI-DEVELOPMENT-MODEL.md) (§5 – §7, §14)
- [ENCINA-1.0-RECONCILIATION.md](../engineering/ENCINA-1.0-RECONCILIATION.md) (§4, P0 – P3)
- [ADR-018 — Cross-cutting integration principle](../architecture/adr/018-cross-cutting-integration-principle.md)
- [ADR-019 — Compliance event sourcing with Marten](../architecture/adr/019-compliance-event-sourcing-marten.md)
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
- Consejo General de la Psicología, Código Deontológico: [COP](https://www.cop.es/pdf/CodigoDeontologicodelPsicologo-vigente.pdf); CNMC report on the draft code: [CNMC](https://www.cnmc.es/prensa/inf-codigo-deontologico-psicologia-20260826)

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
- Regional clinical-record retention (secondary): [PSN Sercon](https://blog.psnsercon.com/los-plazos-de-conservacion-de-las-historias-clinicas-en-las-distintas-comunidades-autonomas/)
- MDR software guidance MDCG 2019-11 rev.1 (secondary): [Emergo by UL](https://www.emergobyul.com/news/european-revision-primary-software-guidance-mdcg-2019-11-revision-1-small-changes-meaningful)
