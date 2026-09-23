# B — Horizontal EU legal acts: technical capabilities a framework like Encina should expose

Research date: 2026-09-23. Status markers: `[CONFIRMED]` = corroborated by an EUR-Lex/official or reputable law-firm source cited inline; `[UNCERTAIN]` = single low/medium-authority source or fast-moving legislative text, verify before relying on it for compliance advice. This is not legal advice.

---

## 0. Executive summary — what changed with the "Digital Omnibus" (Nov 2025 → Sep 2026)

The Commission published two linked proposals on **19 November 2025** (COM(2025)837 and companion AI text) under the "Digital Omnibus" banner. They forked into two separate legislative tracks with very different fates:

| Track | Content | Status as of 2026-09-23 |
|---|---|---|
| **Digital Omnibus on AI** | Amends the AI Act (2024/1689), the EASA Regulation (EU) 2018/1139, and the Machinery Regulation (EU) 2023/1230 | **Adopted and in force.** Published as **Regulation (EU) 2026/1744** in the Official Journal on 24 July 2026, entered into force 27 July 2026. [CONFIRMED] — [lawandtechnology.eu](https://lawandtechnology.eu/en/digital-omnibus-on-ai-official-journal-regulation-2026-1744/), [K&L Gates](https://www.klgates.com/EU-Digital-Omnibus-on-AI-Enters-Into-Force-7-31-2026) |
| **"Data Omnibus"** (GDPR + ePrivacy + NIS2 incident reporting + Data Act simplification; DORA also touched) | Amends Regulation 2016/679, Directive 2002/58, Directive (EU) 2022/2555, Regulation (EU) 2023/2854 | **Still in the ordinary legislative procedure, not adopted.** As of September 2026 the Council (Irish Presidency) and Parliament (still awaiting a committee/plenary mandate) have not reached a common position; trilogues have not started. Adoption is not expected before **late 2026 at the earliest**, entry into force/application later still. [CONFIRMED as "not adopted"] — [Privacy Next, Sep 2026 update](https://www.privacynext.eu/resources/digital-omnibus-gdpr-negotiations-at-the-council-september-2026-update/), [europarl.europa.eu legislative train](https://www.europarl.europa.eu/legislative-train/theme-a-new-plan-for-europe-s-sustainable-prosperity-and-competitiveness/file-digital-package) |

**Practical consequence for Encina today (2026-09-23):**
- The **GDPR, ePrivacy Directive, NIS2 (as transposed) and Data Act text described below are the CURRENT LAW** — nothing in the "Data Omnibus" has changed them yet. Anything below labelled "Digital Omnibus proposal" is a **draft, not in force**, and could still change or be dropped in trilogue.
- The **AI Act IS changed**, by Regulation (EU) 2026/1744: the big news is a **deferral of most high-risk (Annex III) obligations from 2 August 2026 to 2 December 2027**, and of Annex I embedded high-risk systems from 2 August 2027 to **2 August 2028**. Prohibited practices (Art. 5), AI literacy (Art. 4), GPAI obligations (Art. 53 ff.) and Art. 50 transparency duties were **not delayed** and are already in force/applying on schedule. [CONFIRMED] — see §3.

---

## 1. GDPR — Regulation (EU) 2016/679

**Reference:** [Regulation (EU) 2016/679 (GDPR), EUR-Lex 32016R0679](https://eur-lex.europa.eu/eli/reg/2016/679/oj)
**Status (2026-09-23):** Fully in force since 25 May 2018. **Unchanged** — the Digital Omnibus proposal to amend it is still a draft (see §0 and §1.6).

### 1.1 Obligated parties
- **Controller** (Art. 4(7)): determines purposes/means of processing — typically the application owner/operator.
- **Processor** (Art. 4(8)): processes on behalf of a controller — typically Encina's adopters when they build SaaS/PaaS offerings, or Encina-based apps acting as sub-processors.
- **Joint controllers** (Art. 26): common in multi-tenant / platform scenarios.
- **Data Protection Officer** (Art. 37-39): mandatory for public bodies and large-scale/systematic processing.

### 1.2 Dates
In force / applicable since **25 May 2018**. No sunset; amendments only proposed (not adopted) as of today.

### 1.3 Technical capabilities a framework must support, by article

| Article(s) | Obligation | Technical capability Encina should expose |
|---|---|---|
| Art. 5(1)(c) | Data minimisation | Field-level projection/DTO shaping so only necessary columns/fields are read, stored, logged; opt-in inclusion rather than opt-out exclusion of PII in query results and logs |
| Art. 5(1)(e) | Storage limitation | Retention policies enforceable per entity/table (TTL, scheduled purge jobs) — ties into Encina's `Scheduling`/`ScheduledMessage` pattern and any retention module |
| Art. 5(1)(f), Art. 32 | Integrity & confidentiality / security of processing | Encryption at rest (column/field-level and transport TLS), pseudonymisation (Art. 4(5) definition; separate identifiers from attributes with a re-identification key store), access control, resilience testing hooks |
| Art. 6 / Art. 9 | Lawful basis for processing (incl. special categories) | A place to record/tag the lawful basis and category-of-data sensitivity per processing operation — supports the compliance modules (`LawfulBasis` per project's feature-folder convention) |
| Art. 7 | Conditions for consent | Consent record store: who consented, to what version of what notice, when, how withdrawn — auditable, immutable append-only log; "withdraw as easy as give" implies an API/idempotent revoke operation |
| Art. 12-14 | Transparent information / information to be provided | Machine-readable notice versioning so the app can prove which privacy notice text was shown at the time of an action |
| Art. 15 | Right of access | Query capability to assemble "everything held about subject X" across all stores/tables (a DSAR export builder) |
| Art. 16 | Right to rectification | Standard update paths plus an audit trail entry distinguishing subject-initiated corrections |
| Art. 17 | Right to erasure ("right to be forgotten") | Cascading/erasure workflow across primary stores, outbox/inbox messages, backups, caches, search indices, logs; must interoperate with Art. 17(3) exceptions (legal retention) — needs a "erase or anonymise, with exception flags" primitive |
| Art. 18 | Right to restriction of processing | A "frozen"/restricted flag on a record that suppresses processing while still storing data |
| Art. 20 | Right to data portability | Export in a **structured, commonly used, machine-readable format** (JSON/CSV/XML) — this is the same shape needed for Data Act Art. 3/4 exports, so the two can share plumbing |
| Art. 21 | Right to object | Suppression flag distinguishable from erasure (keep data, stop a specific processing purpose, e.g. profiling/marketing) |
| Art. 22 | Automated individual decision-making, incl. profiling | Flag/tag decisions taken by automated means (relevant where Encina interacts with the AI Act's Art. 22 GDPR interplay); needs human-review escalation hook |
| Art. 25 | Data protection by design and by default | Framework-level defaults: encryption on, minimal field exposure, audit-by-default on sensitive stores — "pay-for-what-you-use" opt-in should still default sensitive features to the safer configuration |
| Art. 28 | Processor obligations / DPA | Contract-level, but technically: sub-processor registry, instructions-only-processing enforcement (guard against out-of-scope processing), assistance capabilities for Art. 28(3)(e)-(h) (help the controller answer DSARs, breach notices, audits) |
| Art. 30 | Records of processing activities (RoPA) | A structured registry of processing activities, purposes, categories of data subjects/data, recipients, transfers, retention — ideally auto-populated from store/entity metadata |
| Art. 32 | Security of processing | Encryption, pseudonymisation, resilience, ability to restore availability/access (backup/DR hooks), regular testing/evaluation hooks (health checks, chaos/load test integration) |
| Art. 33-34 | Breach notification (72h to supervisory authority; "without undue delay" to data subjects if high risk) | **Breach detection + timer**: an event that timestamps "awareness," a state machine enforcing/reminding of the 72-hour SLA, a notification payload template, and an audit log of what/when/whom was notified — this is a natural `Encina.Compliance.BreachNotification` module already referenced in the project's EventId ranges (8100-8949 range, "BreachNotification") |
| Art. 35 | Data Protection Impact Assessment (DPIA) | Not purely technical, but a framework can expose risk-scoring hooks/telemetry (volume, special categories, automated decision flags) that feed a DPIA tool |
| Art. 44-49 | International transfers (adequacy, SCCs, BCRs, derogations) | Data residency / data routing controls: ability to pin storage and processing to specific regions, block cross-border replication unless a transfer mechanism is recorded — maps to the project's `DataResidency` and `CrossBorderTransfer` compliance modules |

### 1.4 Key EDPB / regulator guidance to keep in mind
- EDPB guidelines on breach notification, DPIA, and (importantly for AI features) the EDPB/EDPS joint opinion on the Digital Omnibus. [CONFIRMED] — [EDPB news, "Digital Omnibus: EDPB and EDPS support simplification... while raising key concerns"](https://www.edpb.europa.eu/news/digital-omnibus-edpb-and-edps-support-simplification-and-competitiveness-while-raising-key_en)

### 1.5 AEPD (Spain) — relevant national angle
Spain's LOPDGDD (Organic Law 3/2018) supplements GDPR; no material change reported that would alter these technical requirements. `[UNCERTAIN — not independently re-verified via BOE in this pass]`.

### 1.6 Digital Omnibus proposal for GDPR — DRAFT, NOT IN FORCE
For completeness (explicitly requested), the pending draft would, if adopted as currently negotiated:
- Amend **Art. 4(1)** (definition of personal data) to clarify that data is not "personal" for an entity that cannot itself identify the data subject, and add a **new Art. 41a** empowering implementing acts on pseudonymisation/identifiability criteria. The EDPB/EDPS have publicly opposed this change as inconsistent with CJEU case law. [CONFIRMED as proposed / opposed] — [White & Case](https://www.whitecase.com/insight-alert/gdpr-under-revision-key-takeaways-from-digital-omnibus-regulation-proposal), [EDPB news](https://www.edpb.europa.eu/news/digital-omnibus-edpb-and-edps-support-simplification-and-competitiveness-while-raising-key_en)
- Extend the **Art. 33** breach-notification deadline from 72 to **96 hours**, and raise the notification threshold to only breaches "likely to result in a **high** risk" (dropping the current "risk" standard for authority notification). `[UNCERTAIN — draft text, still being negotiated]` — [Kennedys](https://www.kennedyslaw.com/en/thought-leadership/article/2026/the-2025-european-commission-eu-digital-omnibus-package-the-gdpr-regulation-eu-2016679/)
- Insert a **new Art. 88c** on processing personal data for developing/testing/training/operating machine-learning models (an explicit AI/legitimate-interest gateway). `[UNCERTAIN — draft]` — [Kennedys](https://www.kennedyslaw.com/en/thought-leadership/article/2026/the-2025-european-commission-eu-digital-omnibus-package-the-gdpr-regulation-eu-2016679/)
- Migrate the ePrivacy "cookie rule" (current Art. 5(3) ePrivacy) substantively into the GDPR itself and introduce a **new Art. 88b** requiring **standardised, machine-readable consent/refusal/objection signals** (browser-level "reject all"/accept signals), a single-click accept/reject UI mandate, and a **six-month moratorium** on re-asking after a refusal. `[UNCERTAIN — draft, actively contested; Council text as of Sep 2026 reportedly drops some consent provisions]` — [iubenda](https://www.iubenda.com/en/blog/browser-level-consent-digital-omnibus/), [Taylor Wessing](https://www.taylorwessing.com/en/global-data-hub/2026/the-digital-omnibus-proposal/gdh---the-digital-omnibus---cookies), [Privacy Next Sep 2026](https://www.privacynext.eu/resources/digital-omnibus-gdpr-negotiations-at-the-council-september-2026-update/)

**Design implication for Encina (forward-looking, not urgent):** if a consent/consent-record module is built now, model it so a future machine-readable "Global Privacy Control"-style signal and a "reject persists 6 months" rule can be added without a breaking change — but do **not** implement the 96-hour timer or the narrowed breach threshold as the current behaviour; **72 hours and "risk" (not "high risk")** remain the legal baseline today.

---

## 2. ePrivacy Directive — 2002/58/EC (as amended)

**Reference:** [Directive 2002/58/EC (ePrivacy), EUR-Lex 32002L0058](https://eur-lex.europa.eu/eli/dir/2002/58/oj), as amended by Directive 2009/136/EC.
**Status:** In force; transposed by each Member State (in Spain via LSSI-CE / LOPDGDD). Proposed **ePrivacy Regulation** to replace the Directive has been dormant for years and is effectively being overtaken by the Digital Omnibus's plan to fold cookie rules into GDPR (§1.6) — **not adopted**.

### 2.1 Obligated parties
Any provider of an "information society service" that stores or accesses information on a user's terminal equipment (cookies, local storage, device fingerprinting, SDK identifiers). No controller/processor split — obligation runs to whoever sets/reads the storage, which can be the same entity as the GDPR controller or a separate ad-tech vendor integrated into the app.

### 2.2 Technical capabilities (Art. 5(3) + national transpositions)
- **Consent-before-access** for any non-strictly-necessary storage/access technology — a technical gate (e.g., script/tag loading deferred until consent granted).
- **Granular consent categories** (necessary / functional / analytics / marketing) commonly required by national DPAs (AEPD guidance in Spain follows this model) — a framework-level "consent category" concept that gates feature initialization.
- **Consent record & proof** — same underlying primitive as GDPR Art. 7 (see §1.3); one consent-record store can usually serve both.
- **Withdrawal mechanism** at least as easy as granting, and (per most DPA guidance, including AEPD) not requiring more steps than acceptance.
- Exemptions for "strictly necessary" cookies (session, security, load-balancing) need to be **enumerable/documented** in code so audits can verify no analytics/marketing storage runs before consent.

`[CONFIRMED for current-law baseline]` — general Art. 5(3) description corroborated across multiple law-firm sources; AEPD "Guía sobre el uso de cookies" broadly aligns (not re-fetched in this pass — `[UNCERTAIN, not re-verified against aepd.es this session]`).

### 2.3 Digital Omnibus effect — DRAFT, NOT IN FORCE
See §1.6: proposal would repeal/narrow Art. 5(3) scope for natural persons whose data is "personal data" (folding it into GDPR), add exemptions for **audience-measurement cookies**, and mandate **browser-level automated consent signals** (a new obligation on **browser vendors**, not on individual site operators, to build the signalling infrastructure — but site operators would need to **honour** the signal). `[UNCERTAIN — draft]` — [syrenis/Cassie](https://syrenis.com/resources/blog/proposed-new-cookie-exceptions-and-browser-signal-mandates/), [iubenda](https://www.iubenda.com/en/blog/browser-level-consent-digital-omnibus/)

**Design implication:** Encina's consent module (if/when built) should treat "consent signal source" as pluggable (explicit UI banner today, machine-readable browser signal tomorrow) rather than hard-coding a banner-only flow.

---

## 3. NIS2 — Directive (EU) 2022/2555

**Reference:** [Directive (EU) 2022/2555 (NIS2), EUR-Lex 32022L2555](https://eur-lex.europa.eu/eli/dir/2022/2555/oj)
**Implementing act:** [Commission Implementing Regulation (EU) 2024/2690, EUR-Lex 32024R2690](https://eur-lex.europa.eu/eli/reg_impl/2024/2690/oj/eng) — technical/methodological requirements for **specific digital infrastructure/service providers only** (DNS providers, TLD registries, cloud computing service providers, data centre service providers, content delivery networks, managed service/security service providers, online marketplaces, search engines, social-networking platforms, trust service providers).
**Status:** Directive deadline for national transposition was **17 October 2024** — the Commission has since opened infringement proceedings against multiple Member States including Spain (reasoned opinion 7 May 2025, formal notice/second warning 19 May 2026 under INFR(2024)0270) for incomplete transposition. `[CONFIRMED]` — search result summarizing Spain's status.

### 3.1 Obligated parties
- **Essential entities**: Annex I sectors (energy, transport, banking, financial market infrastructure, health, drinking/waste water, digital infrastructure, ICT service management (B2B), public administration, space) at medium+ size (≥50 staff or >€10M turnover, generally large-enterprise thresholds ≥250 staff/€50M for the strictest tier) — subject to **proactive** supervision.
- **Important entities**: Annex II sectors (postal/courier, waste management, chemicals, food, manufacturing, digital providers (online marketplaces, search engines, social networks), research) — **reactive** supervision (ex-post, complaint/incident-driven).
- Size thresholds broadly: medium (50-249 staff / €10-50M turnover) → important; large (≥250 staff / >€50M turnover) → essential, in-scope sector permitting. `[CONFIRMED, general description]` — search summary above.
- **A generic application framework like Encina is not itself the obligated entity** — its *adopters* who fall into Annex I/II sectors are. Encina's job is to make the technical measures **implementable** by those adopters.

### 3.2 Dates
- Directive in force 16 Jan 2023; national transposition due **17 Oct 2024** (missed by many states incl. Spain).
- Implementing Regulation 2024/2690 applies **from 18 October 2024** to the specific digital-infrastructure/service-provider categories it lists (not to all NIS2 entities).
- **Spain:** the "Anteproyecto/Proyecto de Ley de Coordinación y Gobernanza de la Ciberseguridad" (transposing NIS2) was approved by Council of Ministers 14 Jan 2025 and, as of mid-2026, remains in parliamentary processing, not yet published in the BOE; estimated entry into force during 2026 but not yet confirmed. `[CONFIRMED status, uncertain final date]` — [DSN official page](https://www.dsn.gob.es/en/node/24160), search summary.

### 3.3 Technical capabilities — Art. 21(2) risk-management measures (a)-(j)
Applies **identically** to essential and important entities (a common misconception is that essential entities face stricter *measures* — they don't; they face stricter *supervision*). `[CONFIRMED]`. The ten measure categories a framework should provide building blocks for:

| Art. 21(2) point | Measure | Encina building block |
|---|---|---|
| (a) | Risk analysis & information system security policies | Risk-register/asset-inventory hooks, config-as-code for security policy |
| (b) | Incident handling | Structured logging + an incident pipeline (outbox/inbox-style event capture, correlation IDs), integration with the 24h/72h/1-month reporting workflow (§3.4) |
| (c) | Business continuity, backup, disaster recovery, crisis management | Backup/restore hooks in store providers, health checks, `TimeProvider`-driven deterministic DR testing |
| (d) | Supply chain security | SBOM-friendly dependency metadata, provider/package inventory (naturally aligned with Encina's own "multi-provider" package structure) |
| (e) | Security in network/information systems acquisition, development, maintenance (incl. vulnerability handling/disclosure) | Secure-SDLC hooks: dependency scanning integration points, vulnerability disclosure log |
| (f) | Policies/procedures to assess effectiveness of measures | Telemetry/OpenTelemetry integration (already a first-class Encina cross-cutting concern), audit trail |
| (g) | Basic cyber hygiene & training | Not primarily technical, but framework can enforce secure defaults (e.g., forcing TLS, disabling weak ciphers) as a "hygiene by default" posture |
| (h) | Cryptography and encryption policies | Encryption-at-rest/in-transit primitives, key management abstraction, algorithm-agility (swap ciphers without app rewrite) |
| (i) | Human resources security, access control, asset management | RBAC/ABAC hooks (Encina already has ABAC in its Security Extensions range 9000-9199), asset/entity ownership metadata |
| (j) | Multi-factor authentication, secured voice/video/text comms, secured emergency comms | MFA integration points in auth pipelines, secure channel enforcement |

The **Implementing Regulation 2024/2690** annex spells these ten categories out in much greater technical/methodological detail (mapped to ISO/IEC 27001, ISO/IEC 27002, ETSI EN 319 401) but **only binds** DNS/cloud/data-centre/CDN/managed-service/marketplace/search/social-network/trust-service providers — i.e., it is directly relevant if Encina or an Encina-based product *is* one of those provider types (e.g., a managed-service or cloud offering built on Encina), not to arbitrary line-of-business apps. `[CONFIRMED]` — [EUR-Lex 2024/2690](https://eur-lex.europa.eu/eli/reg_impl/2024/2690/oj/eng), [OpenKRITIS mapping](https://www.openkritis.de/massnahmen/implementing-acts-it-nis2-mapping.html)

### 3.4 Art. 23 — reporting obligations (the 24h/72h/1-month cascade)
`[CONFIRMED]` — consistent across multiple sources:
1. **Early warning within 24 hours** of becoming aware of a significant incident — to the CSIRT/competent authority, indicating suspected unlawful/malicious cause and possible cross-border impact.
2. **Incident notification within 72 hours** — updates the early warning with initial severity/impact assessment and indicators of compromise.
3. **Final report within 1 month** of the incident notification — root cause, impact severity, mitigation/remedial measures; if the incident is ongoing, a progress report is due at the 1-month mark and the final report follows within 1 month of resolution.

**Technical capability**: an incident state machine with three SLA timers (T+24h, T+72h, T+1month), an "awareness" timestamp event as the SLA trigger, structured report payload templates matching each stage's minimum content, and audit logging of submissions — directly analogous to the GDPR Art. 33/34 breach module (§1.3) and could share the same underlying "regulatory incident clock" primitive.

### 3.5 Spain-specific
As above (§3.2) — transposition not yet complete as of Sep 2026; current enforceable baseline in Spain is the **directive's direct effect risk** plus existing sectoral cybersecurity rules (e.g., Ley 8/2011 PIC for critical infrastructure, ENS — Esquema Nacional de Seguridad — for public sector) pending the new law. `[UNCERTAIN — not independently re-verified against BOE this session]`.

### 3.6 Digital Omnibus and NIS2 — DRAFT, NOT IN FORCE
The pending "Data Omnibus" reportedly proposes to **simplify/harmonise NIS2 incident-reporting timelines/thresholds** (the question's premise). Search results did not surface specific adopted text; treat as still a Commission proposal under negotiation with the same "not before late 2026" timeline as the GDPR/ePrivacy strand (§0). `[UNCERTAIN — could not confirm specific NIS2 textual changes beyond the general "Data Omnibus also touches NIS2" framing]`.

---

## 4. AI Act — Regulation (EU) 2024/1689, as amended by Regulation (EU) 2026/1744

**Reference:** [Regulation (EU) 2024/1689 (AI Act), EUR-Lex 32024R1689](https://eur-lex.europa.eu/eli/reg/2024/1689/oj); amending act [Regulation (EU) 2026/1744](https://lawandtechnology.eu/en/digital-omnibus-on-ai-official-journal-regulation-2026-1744/).
**Status:** In force since 1 August 2024, applying in stages; **the "Digital Omnibus on AI" IS adopted and in force** (published OJ 24 July 2026, in force 27 July 2026) — this is the one part of the November-2025 Digital Omnibus package that has actually become law. `[CONFIRMED]`

### 4.1 Obligated parties (role-based)
- **Provider**: develops/places an AI system or GPAI model on the market.
- **Deployer**: uses an AI system under its authority in a professional context (most Encina adopters will be *deployers*, sometimes *providers* if they build/customize models into a product).
- **Importer / Distributor**: supply-chain roles for AI systems.
- **Product manufacturer**: where AI is embedded in a regulated product (Annex I) — e.g., machinery, medical devices.
- **GPAI model provider**: separate regime (Art. 53 ff.) for foundation/general-purpose models.
- **Authorised representative**: non-EU providers.

### 4.2 Dates (post-omnibus, i.e., current law as of 2026-09-23)
| Date | Applies to |
|---|---|
| 1 Aug 2024 | Regulation enters into force |
| **2 Feb 2025** | Chapter I (general provisions) + Chapter II Art. 5 **prohibited AI practices**; **Art. 4 AI literacy** obligation — unchanged by the omnibus. `[CONFIRMED]` |
| **2 Aug 2025** | GPAI model provider obligations (Art. 53 ff.) — documentation, downstream information, copyright policy; governance/penalties provisions. Unchanged by the omnibus; **Commission enforcement powers** (RFIs, model access, recall requests) start **2 Aug 2026**; pre-Aug-2025 models get until **2 Aug 2027** to comply. `[CONFIRMED]` |
| **2 Aug 2026** | Most remaining provisions incl. **Art. 50 transparency obligations** (unchanged by omnibus), governance/notifying-authority structures, penalties for the bulk of the Act |
| **2 Dec 2027** (was 2 Aug 2026) | Full high-risk obligations for **standalone Annex III** high-risk systems (employment, education, credit scoring, law enforcement, migration, critical infrastructure, etc.) — **deferred by Reg. 2026/1744** |
| **2 Aug 2028** (was 2 Aug 2027) | High-risk obligations for AI **embedded in Annex I regulated products** (machinery, medical devices, etc.) — **deferred by Reg. 2026/1744** |

`[CONFIRMED]` — [K&L Gates](https://www.klgates.com/EU-Digital-Omnibus-on-AI-Enters-Into-Force-7-31-2026), [Cloud Security Alliance research note](https://labs.cloudsecurityalliance.org/research/csa-research-note-eu-ai-act-omnibus-vii-deadline-delay-20260/), [Modulos](https://www.modulos.ai/blog/eu-ai-act-omnibus-now-law)

Note: the omnibus **does not repeal any high-risk obligation**, it only **resequences timing** — a framework should still build the capabilities below now, since Dec 2027/Aug 2028 will arrive.

### 4.3 Technical capabilities by article
| Article | Obligation | Technical capability |
|---|---|---|
| Art. 4 | AI literacy | Not code, but: documentation/training-material hooks for staff operating AI features (organisational, out of framework scope) |
| Art. 5 | Prohibited practices | A **pre-flight capability check / feature gate** that a framework could offer (e.g., a "guarded AI feature" flag forcing explicit review) so a deployer cannot trivially wire up e.g. real-time biometric categorisation or emotion-recognition-in-workplace features without a compliance gate — largely a governance/process concern, but a policy-engine hook is a legitimate framework building block |
| Art. 9 | Risk management system (continuous, iterative, throughout lifecycle) | Hooks to log/version risk assessments tied to a model/version; integrate with existing telemetry (OpenTelemetry) for drift/anomaly detection feeding back into risk review |
| Art. 10 | Data and data governance (training/validation/testing data quality) | Data lineage/provenance tracking for datasets used to train or fine-tune models plugged into the app; data-quality validation hooks (bias checks, representativeness) |
| Art. 12 | **Record-keeping / automatic logging** | High-risk AI systems must **technically support automatic event logging** over their lifetime — a **first-class logging capability** (append-only, tamper-evident) recording: each use period, the reference dataset/version checked against, input data leading to a "match"/decision, and identities of natural persons who verified results (for biometric-ID use cases specifically). This maps directly onto Encina's structured-logging/EventId infrastructure — a natural `AiAct`/`ModelAudit` logging surface with its own EventId range. `[CONFIRMED]` — search summary above |
| Art. 13 | Transparency and information to deployers | Machine-readable "model card" style metadata (capabilities, limitations, intended purpose) attached to a model/version identifier |
| Art. 14 | **Human oversight** | Interrupt/override hooks — a deployer-facing API to pause, override, or reject an automated output before it takes effect; "stop button" pattern; audit entry when a human overrides an AI output (ties to Art. 22 GDPR interplay in §1.3) |
| Art. 15 | Accuracy, robustness, cybersecurity | Versioned accuracy/robustness metrics store; adversarial-input resilience hooks; ties into NIS2-style resilience testing |
| Art. 50 | Transparency obligations (chatbots must disclose they're AI; synthetic/deepfake content must be labelled; emotion-recognition/biometric-categorisation disclosure) | A **disclosure flag/metadata tag** that a UI layer can render ("this content/interaction is AI-generated/assisted") — a generic capability any Encina-based chat/generation feature should expose by default |
| Art. 53 (GPAI) | Technical documentation, downstream info, copyright policy | Not applicable to an app framework unless Encina itself distributes a GPAI model (unlikely) — relevant mainly if Encina wraps/orchestrates third-party GPAI models, where a "model provenance & documentation" record is useful |

### 4.4 What the AI Digital Omnibus actually changed beyond dates
Per the omnibus's stated scope: it also introduces **new prohibited-practice clarifications and scope carve-outs**, and touches product-safety interplay (EASA, Machinery Regulation) for embedded high-risk AI. `[CONFIRMED at a high level, details not fully itemised in this pass]` — [White & Case](https://www.whitecase.com/insight-alert/eu-ai-omnibus-enters-force-amending-ai-act).

---

## 5. Data Act — Regulation (EU) 2023/2854

**Reference:** [Regulation (EU) 2023/2854 (Data Act), EUR-Lex 32023R2854](https://eur-lex.europa.eu/eli/reg/2023/2854/oj)
**Status:** Fully applicable since **12 September 2025** (Chapters II-XI); Chapter II's "access by design" sub-obligation (Art. 3(1) for products/services placed on the market) applies from **12 September 2026** for newly placed products. Amendments proposed by the Digital Omnibus (trade-secret protections, narrower B2G sharing, SME/legacy-contract carve-outs) are **still a draft, not adopted**, part of the same "Data Omnibus" track as §0/§1.6. `[CONFIRMED for application dates; CONFIRMED as draft for the omnibus amendments]`

### 5.1 Obligated parties
- **Data holder**: entity with the right/obligation to make product/related-service data available (often the manufacturer/service provider — e.g., an IoT vendor).
- **User**: the natural/legal person who owns, rents or leases the connected product, or otherwise uses the related service — has the access/portability/sharing right.
- **Third party**: recipient the user designates to receive shared data (excludes "gatekeepers" under the DMA, who cannot receive user-directed data under Art. 5).
- **Provider of data processing services** (cloud/edge — IaaS/PaaS/SaaS): subject to the switching regime (Chapter VI, Arts. 23-31).
- **Customer** of a data processing service: holds the switching right.
- **Public sector body**: recipient in the narrow B2G emergency-data-sharing regime (Chapter V).

### 5.2 Dates
- Entered into force 11 Jan 2024; **applicable from 12 September 2025** (per Art. 50) — this is when Chapters II-XI, including switching (Chapter VI) obligations, became enforceable. `[CONFIRMED]`
- **Access-by-design** (Art. 3(1)) applies to connected products/related services placed on the market **from 12 September 2026** — i.e., only products newly designed/marketed after that date must be "built" for default data accessibility; products already on the market by then are not retroactively required to be redesigned. `[CONFIRMED]` — [gamingtechlaw.com](https://www.gamingtechlaw.com/2026/08/data-act-access-by-design/)
- Contracts on data processing services concluded **before 12 September 2025** get transitional carve-outs (existing minimum-term clauses remain valid until 12 January 2027 at the latest, when the free/graduated egress-charge phase-down completes — verify exact date against Art. 29 before citing precisely). `[UNCERTAIN — transition/egress-fee phase-down exact end date not independently re-verified this session]`.

### 5.3 Technical capabilities by article group
| Articles | Obligation | Technical capability |
|---|---|---|
| Art. 3 | Access by design | Connected-product/related-service data (and metadata needed to interpret it) must be accessible **by default**, directly from the device where technically feasible, in an "easy, secure, free, comprehensive, structured, commonly used, machine-readable" format |
| Art. 4 | User's right of access to readily available data | An **export/read API** exposing raw product/service-generated data to the user, without undue delay, of the same quality the data holder itself has, free of charge, continuously/real-time where feasible — same "structured, machine-readable" bar as GDPR Art. 20; a framework can genuinely **share the export-pipeline plumbing** between GDPR portability and Data Act access |
| Art. 5 | Right to share with third parties | The same export/read capability but targeted at a user-designated third-party recipient — needs consent/authorisation capture and a delivery mechanism (webhook/API push or generated export handoff), plus the gatekeeper-exclusion check |
| Art. 4(8)/5(11) (per Digital Omnibus draft) | Trade-secret refusal | `[UNCERTAIN — draft]` a policy hook to flag/justify refusal of specific fields as trade secrets, with a "duly substantiated + notify authority" workflow — not current law yet |
| Chapter III (Arts. 8-9, not deeply researched this pass) | Fairness of contractual terms for data-sharing obligations imposed by other EU/national law | Mostly legal/contractual, minimal direct technical surface |
| Chapter V (Art. 14-22) | B2G data sharing in exceptional need (e.g., public emergency) | An on-demand bulk-export capability with strong access controls/audit, gated to a defined "exceptional need" trigger |
| **Chapter VI, Arts. 23-31** | **Switching between data processing services** | (a) **No technical/contractual lock-in**: standard, well-documented, publicly available APIs (Art. 30 open interoperability specifications where they exist for a service category); (b) **data export**: complete export of all "exportable data" (structured/semi-structured data, incl. configuration, and application/digital assets where feasible) in a structured, commonly used, machine-readable format before the switch completes; (c) **transition period**: switching processes must complete within a maximum **30-day mandatory transitional period** (extendable); (d) **egress-charge phase-down to zero**: withdrawal/switching/egress fees must be progressively reduced and **eliminated** after the statutory transition date; (e) **functional equivalence obligation** for IaaS specifically (Art. 29) — the new provider's service should allow the same core functionality post-switch. `[CONFIRMED at a general level]` |

### 5.4 Digital Omnibus effect on the Data Act — DRAFT, NOT IN FORCE
Proposed changes (per §5.2/§0): strengthened trade-secret refusal grounds (Art. 4(8), 5(11)), narrower B2G sharing (limited to genuine public emergencies), and contractual/SME carve-outs for agreements signed on/before 12 Sept 2025. `[CONFIRMED as proposed]` — [Bird & Bird](https://www.twobirds.com/en/insights/2025/eu-digital-omnibus-package-major-changes-to-the-data-act-proposed), [Greenberg Traurig](https://www.gtlaw.com/en/insights/2026/7/eu-digital-omnibus-package-proposes-amendments-to-data-act).

---

## 6. Data Governance Act — Regulation (EU) 2022/868 (brief, as requested)

**Reference:** [Regulation (EU) 2022/868 (DGA), EUR-Lex 32022R0868](https://eur-lex.europa.eu/eli/reg/2022/868/oj)
**Status:** Applicable since **24 September 2023**; not touched by the Digital Omnibus as far as found in this research pass.

- **Obligated parties**: providers of **data intermediation services** (data marketplaces/pools — subject to a **notification** regime, not a licence), and organisations engaging in **data altruism** (must register, be **non-profit**, meet transparency/purpose-limitation conditions).
- **Relevance to Encina**: low-to-moderate for a generic app framework. It mainly matters if Encina or an adopter builds a **data-intermediation platform** or a **data-altruism** registry feature — in which case relevant technical capabilities would be: a notification/registration record, purpose-limitation enforcement on altruism data, and a consent-withdrawal mechanism for data-altruism contributions (conceptually reusing the same consent-record primitive as GDPR Art. 7 / ePrivacy). No article-level technical mandate applies to a generic backend outside those two service types. `[CONFIRMED, general characterisation]` — [OECD/EY/Lexology summaries above].

---

## 7. European Accessibility Act — Directive (EU) 2019/882

**Reference:** [Directive (EU) 2019/882 (EAA), EUR-Lex 32019L0882](https://eur-lex.europa.eu/eli/dir/2019/882/oj)
**Status:** National transposition due 28 June 2022; **substantive application from 28 June 2025** (now in force). Not touched by the Digital Omnibus.

- **Obligated parties**: manufacturers, importers, distributors and **service providers** of the listed products/services (consumer banking, e-commerce, e-books, electronic communications, audiovisual media access services, transport e-ticketing/travel info, self-service terminals/ATMs, smartphones/computers).
- **Does it matter for a backend framework like Encina?** **Indirectly, not directly.** The EAA's accessibility requirements (WCAG-aligned, per Annex I) target **user-facing interfaces** (web/mobile UI, ATMs, self-service terminals) and **document formats**, not backend/API/data-layer code. A pure application framework (mediator, outbox/inbox, data access, caching) has **no EAA obligations of its own**, but:
  - If Encina ships or documents any **ASP.NET Core UI templates, admin dashboards, or generated HTML/PDF exports** (e.g., DSAR export documents, consent-management UI, health-check dashboards), those *front-end* artifacts would need to be accessible if shipped as part of a covered service.
  - A GDPR Art. 20 / Data Act Art. 4 **export format** could plausibly also need an **accessible variant** if the exported document itself is consumer-facing (e.g., an e-book invoice), but this is a stretch case, not a core requirement.
  - **Recommendation**: treat EAA as **out of scope for Encina's core packages**, note it only for any first-party UI/reference-app components Encina might ship. `[CONFIRMED re: scope/dates; the "framework relevance" assessment above is analysis, not sourced from a specific citation — mark as reasoned conclusion]`

---

## 8. Digital Services Act — Regulation (EU) 2022/2065 (kept short, as requested)

**Reference:** [Regulation (EU) 2022/2065 (DSA), EUR-Lex 32022R2065](https://eur-lex.europa.eu/eli/reg/2022/2065/oj)
**Status:** Fully applicable since 17 February 2024.

- **Obligated parties**: "intermediary services" (mere conduit, caching, hosting) — obligations scale with role and size (hosting > online platform > VLOP/VLOSE).
- **Relevance to a generic application framework**: **low, unless the app is a hosting/UGC platform.** Most Encina-based line-of-business apps (CRUD apps, internal tools, B2B SaaS without user-generated content) are **out of scope** or fall under the **micro/small enterprise exemption** (Art. 16 notice-and-action duties do not apply to entities with <50 staff and <€10M turnover, unless they're a VLOP). `[CONFIRMED]` — [digitalservicesact.cc Art. 16](https://digitalservicesact.cc/dsa/art16.html)
- **If** an Encina-based app does host third-party/user content (comments, marketplace listings, forums): technical capabilities worth having as opt-in building blocks are a **notice-and-action intake + audit trail** (Art. 16), a **statement-of-reasons** record for content moderation decisions (Art. 17), and a **transparency-reporting** data aggregation hook (Art. 24). None of this should be baked into the framework core given "pay-for-what-you-use"; a `Encina.Compliance.DSA` opt-in module would be the appropriate shape if ever built.

---

## 9. Cross-cutting technical building blocks (synthesis)

Reading across all acts above, the same small set of primitives recurs and should be built **once**, generically, and reused:

1. **Consent/authorisation record store** — serves GDPR Art. 7, ePrivacy Art. 5(3), DGA data-altruism withdrawal, Data Act Art. 5 third-party sharing authorisation.
2. **Structured/machine-readable export pipeline** — serves GDPR Art. 15/20 (access/portability), Data Act Art. 4/5 (product data access/sharing), Chapter VI Art. 23-31 (service-switching export).
3. **Regulatory incident clock (multi-stage SLA timer)** — serves GDPR Art. 33/34 (72h/undue delay) and NIS2 Art. 23 (24h/72h/1-month), with a pluggable timeline profile per regime.
4. **Tamper-evident append-only audit/event log** — serves GDPR Art. 30 (RoPA) & Art. 5 accountability, AI Act Art. 12 record-keeping, NIS2 Art. 21(f) effectiveness assessment, DSA Art. 17 statement-of-reasons.
5. **Data residency / routing control** — serves GDPR Art. 44-49 international transfers, and indirectly NIS2 supply-chain (Art. 21(d)) and Data Act trade-secret/non-EU-disclosure concerns.
6. **Retention/erasure policy engine with legal-hold exceptions** — serves GDPR Art. 17 (erasure) & Art. 5(1)(e) (storage limitation), and NIS2/Data Act adjacent record-keeping duties.
7. **Human-in-the-loop override/interrupt hook** — serves GDPR Art. 22 (automated decisions) and AI Act Art. 14 (human oversight).
8. **Disclosure/labelling metadata tag** — serves AI Act Art. 50 (AI-generated content/chatbot disclosure) and could double for DSA transparency duties.

None of these should be mandatory or bundled — consistent with Encina's opt-in philosophy, each maps naturally to an optional `Encina.Compliance.*` or `Encina.Security.*` satellite package, most of which (per CLAUDE.md's EventId range table) already have reserved ranges: GDPR/Consent/DSR/LawfulBasis/Anonymization/CryptoShredding/Retention/DataResidency/BreachNotification/DPIA/PrivacyByDesign (8100-8949), and NIS2/AI-Act-adjacent items would fit under the "Compliance Extensions" (9200-9499) or a new reserved block.

---

## 10. Open questions / things to verify before treating any of the above as final compliance guidance

- Exact **current** text of Art. 29 Data Act egress-fee zero-date and the precise end-date of transitional contract carve-outs — not independently re-verified against EUR-Lex full text this session.
- Whether the "Data Omnibus" trilogue has produced **any** provisional political agreement between this research date (23 Sep 2026) and when this document is read — check [europarl.europa.eu legislative train — Digital Omnibus](https://www.europarl.europa.eu/legislative-train/theme-a-new-plan-for-europe-s-sustainable-prosperity-and-competitiveness/file-digital-package) and [EDPB news](https://www.edpb.europa.eu/news_en) for updates, as this is the fastest-moving item in this report.
- Spain's NIS2 transposition law's actual BOE publication date and final text (not yet published as of the sources found).
- Whether the AI Digital Omnibus (2026/1744) changed anything in Art. 9/10/12/14/15 substance (this report only confirms the **date deferrals** with confidence; substantive drafting changes to those articles were not fully itemised).
