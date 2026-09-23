# F — Reference use case: small private psychology practice in Spain

Research date: 2026-09-23. Read-only research; no repository file was changed.

**Scenario.** After 1.0, the maintainer builds a management application on Encina for a small private psychology practice in Spain (one to a few psychologists). It handles patients, appointments, clinical notes and records, invoices, and possibly online sessions and a patient portal. The goal: Encina must *facilitate*, and never *block*, every legal requirement of that app that falls within a framework's scope.

**Legend.**
- **[V]** verified in this session against an official source: BOE, AEPD, EC/DG SANTE, the COP (Consejo General de la Psicología) PDF, CNMC, or AEAT.
- **[S]** verified only through a reputable secondary source (law firm or specialist publication).
- **[K]** from background knowledge and not re-verified in this session. Treat these as claims to confirm.
- **[I]** inference or interpretation by the author.

EUR-Lex refused automated retrieval (HTTP 202 with an empty body). For that reason, EHDS provisions are cited through the European Commission's official EHDS FAQ (DG SANTE, v1.1, 26 March 2026), which cites article numbers.

---

## 1. Executive summary

1. **The core legal pattern of a clinical app** is:
   - special-category data processed without consent under GDPR Art. 9(2)(h)+(3);
   - a clinical record (historia clínica) that must be kept **at least 5 years from the discharge of each care episode** (Ley 41/2002 art. 17.1);
   - erasure requests refused during that period under GDPR Art. 17(3)(b)/(c);
   - after "deletion", **blocking** (bloqueo) rather than physical destruction for the limitation period (LOPDGDD art. 32);
   - a strong duty to log **who accessed which record** (AEPD resolution PD-00068-2026 of 13 July 2026, which reads GDPR Art. 15 in the light of EHDS Art. 9 and its 3-year log availability).
2. **Encina today cannot express the core retention/erasure pattern. Several components would actively work against it.** The following are verified by reading the code:
   - Retention only supports "delete after N" from tracking start. There is no minimum-retention floor and no anchoring to an event such as the discharge date.
   - The legal-hold check **fails open**. An error when checking holds is treated as "no hold", and deletion proceeds.
   - The retention sweep erases data but cannot transition the record to `Deleted`. It re-erases on every cycle.
   - DSR erasure never consults Retention or legal holds.
   - Crypto-shredding deletes **all** of a subject's keys when **any** single erasable field is erased. Clinical fields that are legally retained become unreadable.
   - No blocking (bloqueo) state exists.
   - `RegionRegistry.US` is hard-coded as having an adequacy decision, although the DPF covers only certified organisations.
3. **Read audit exists, but it is not evidential grade yet:**
   - it is fire-and-forget;
   - collection reads log no entity identifiers;
   - `RequirePurpose` only warns;
   - the purge default is 365 days (the EHDS expects 3 years).
4. **Nine compliance modules are Marten/PostgreSQL-only (ADR-019).** For a small practice this is acceptable if the app standardises on PostgreSQL. It must be stated explicitly in the 1.0 scope.
5. **Out of framework core, but relevant to the app:**
   - Verifactu invoicing records, mandatory for IRPF taxpayers from **1 July 2027** (RDL 15/2025);
   - B2B e-invoicing (RD 238/2026), not yet applicable;
   - trusted time-stamping and e-signatures (eIDAS);
   - the EHDS EEHRxF export and logging component (from 26 March 2029/2031).
   - The AI Act, MDR, NIS2, ENS and CRA are mostly **not applicable** to the base app. The conditions under which they would become applicable are in §3.

---

## 2. (a) Requirements table

Each row gives the ID, the requirement, its source and its status on 2026-09-23. The "Who" column uses these abbreviations:
- **App** = the practice's software;
- **Org** = organisational (not software);
- **FW** = can be facilitated by a framework.

| ID | Requirement | Source (article) | Status 2026-09-23 | Who | Verif. |
|---|---|---|---|---|---|
| R01 | Health data (special category) may be processed for healthcare by a professional under a secrecy obligation, without consent. Spanish law requires a legal basis of statutory rank; the health laws listed in DA 17ª provide it. | GDPR Art. 9(1), 9(2)(h), 9(3); LOPDGDD art. 9.2 and DA 17ª.1 (lists Ley 14/1986, 41/2002, 44/2003, etc.) | In force. LOPDGDD consolidated to 27/12/2025 | FW (model the Art. 9 condition), Org | [V] LOPDGDD; [K] GDPR wording |
| R02 | Transparency and information to patients (privacy notice), including the portal. | GDPR Arts. 12–14 | In force | App (text), FW (record the notice version shown) | [K] |
| R03 | Data protection by design and by default (minimisation, access restriction by default). | GDPR Art. 25 | In force | FW + App | [K] |
| R04 | **Records of processing are mandatory.** The fewer-than-250-employees exemption does not apply when special categories are processed or processing is not occasional. | GDPR Art. 30(1), 30(5) | In force | FW (register/export), Org | [K] |
| R05 | Security of processing: encryption and pseudonymisation; confidentiality, integrity, availability and resilience; timely restore; regular testing. | GDPR Art. 32(1)(a)–(d) | In force | FW + App + Org | [K] |
| R06 | Breach: notify the AEPD within 72 h; keep an internal register of every breach; tell patients when the risk is high. | GDPR Arts. 33(1), 33(5), 34 | In force | FW (workflow, register, deadline), Org | [K] |
| R07 | DPIA. Details in §3.2. The AEPD Art. 35.5 list exempts **individual self-employed health professionals**, "sin perjuicio" of the case where they significantly meet 2 or more criteria of the Art. 35.4 list. The 35.4 list includes criterion 4 (Art. 9 data), criterion 9 (vulnerable subjects, **minors under 14**) and criterion 10 (new technologies). | GDPR Art. 35(1), (3)(b), (4), (5); recital 91; AEPD lists 35.4 and 35.5 | In force | FW (DPIA tooling), Org | [V] AEPD lists |
| R08 | DPO required for "centros sanitarios legalmente obligados al mantenimiento de las historias clínicas", **except professionals practising "a título individual"**. | GDPR Art. 37; LOPDGDD art. 34.1.l | In force | Org | [V] |
| R09 | Data subject rights: access (one month), rectification, erasure with exceptions, restriction, portability, objection. | GDPR Arts. 12(3), 15–21, 17(3)(b)(c)(e) | In force | FW + App | [K] |
| R10 | Processor contracts with cloud, e-mail, video, invoicing SaaS and AI vendors. | GDPR Art. 28 | In force | Org, FW (processor register) | [K] |
| R11 | International transfers. The EU–US DPF adequacy decision (2023/1795) covers **only certified US organisations**. The General Court upheld it in T-553/23 Latombe (3 Sept 2025). The appeal **C-703/25 P is pending**. | GDPR Chapter V (Arts. 44–49) | DPF valid but contested | FW (transfer checks), Org | [S] |
| R12 | **Blocking (bloqueo).** When data is rectified or erased, the controller must block it: identify and reserve it with technical and organisational measures that prevent processing, **including viewing**. The only exception is making it available to courts, the prosecutor or competent authorities (including DPAs) during the limitation period of possible liabilities; after that it must be destroyed. Blocked data may not be used for any other purpose. If blocking is impossible or disproportionate, a secure copy with "evidencia digital" of authenticity is allowed. | LOPDGDD art. 32.1–32.5 | In force | **FW** | [V] |
| R13 | Minors. Consent to *data* processing is valid from age 14. *Clinical* informed consent by representation applies under 16, with a regime for 16 and over. | LOPDGDD art. 7; Ley 41/2002 art. 9.3–9.4 | In force | FW (representation model), App | [V] art. 7; [K] art. 9 detail |
| R14 | Deceased patients: relatives and heirs may access the data unless the patient forbade it. Relatives or de facto partners may access the clinical record unless the patient expressly forbade it. | LOPDGDD art. 3; Ley 41/2002 art. 18.4 | In force | FW (subject status and authorised requesters), App | [V] |
| R15 | The clinical record is the set of documents of each care process. It must be archived in any medium with **security, correct conservation and information recovery**. Mechanisms must guarantee "la autenticidad del contenido de la historia clínica y de los cambios operados en ella, así como la posibilidad de su reproducción futura". | Ley 41/2002 arts. 14.1–14.3, 15 | In force (consolidated 01/03/2023) | **FW** (versioning, integrity), App | [V] |
| R16 | Access by treating professionals for care. For judicial, epidemiological, research or teaching access, **identification data are kept separate from clinical data** (anonymity). Staff accessing the data are bound by secrecy. | Ley 41/2002 art. 16.1, 16.3, 16.6 | In force | FW (pseudonymised export, ABAC), Org | [V] |
| R17 | **Minimum retention: "como mínimo, cinco años contados desde la fecha del alta de cada proceso asistencial".** Professionals practising individually are responsible for custody. Security measures under data-protection law apply. Some regions set longer periods: Catalonia's Ley 21/2000 art. 12 reportedly requires certain documents until 20 years after death; the País Vasco sets 5 years. | Ley 41/2002 art. 17.1, 17.3, 17.6; regional laws | In force | **FW** (retention floor, event anchor, per-jurisdiction policy) | [V] national; [S] regional |
| R18 | Patient right of access and copies. The limits are third parties' confidentiality and the professionals' **"anotaciones subjetivas"**. Access by a representative is allowed. | Ley 41/2002 art. 18.1–18.3 | In force | **FW** (field-level access exclusion in DSR export), App | [V] |
| R19 | Clinical informed consent. This is a different legal concept from GDPR consent. | Ley 41/2002 arts. 8–10 | In force | App (documents), FW (evidence and signature) | [K] |
| R20 | Written clinical record, common to each centre and unique per patient, "tenderá a ser soportada en medios electrónicos". | Ley 44/2003 (LOPS) art. 4.7.a | In force (consolidated 05/06/2021) | App | [V] |
| R21 | A psicólogo general sanitario (PGS) practice needs a **health-centre authorisation** (RD 1277/2003: C.2.2 "consultas de otros profesionales sanitarios"; unit U.900 for PGS, or U.70 for clinical psychology). The practice is therefore a **centro sanitario**, with Ley 41/2002 centre duties and possibly DPO duty (R08). The PGS profession itself comes from Ley 33/2011 DA 7ª. | RD 1277/2003; Ley 33/2011 DA 7ª; regional authorisation rules (e.g. Gencat, Madrid) | In force | Org | [S] (colegios, Gencat) |
| R22 | Professional secrecy and records in the Código Deontológico:<br>• art. 40: secrecy, lifted only by the client's express consent;<br>• art. 41: disclosure to third parties only with authorisation;<br>• art. 42: the subject's right to know the content of a report;<br>• art. 43: lists for third parties without identifiers;<br>• art. 45: case presentations must be de-identified;<br>• **art. 46: written and electronic records kept under the psychologist's personal responsibility, secure against access by outsiders**;<br>• art. 47: third parties present need consent;<br>• art. 49: death does not lift secrecy.<br>No retention period is set. **A new Code is in draft**: the CNMC reported on it on 26 Aug 2026, and press reports say it adds AI provisions. | Código Deontológico del Psicólogo (as amended 2010/2014/2015), arts. 39–49 | In force; replacement pending | FW (access control, audit, anonymisation), Org | [V] |
| R23 | **Access traceability.** On request, the patient must generally be told *who* (identity), *when* and *what data* were accessed. Restrictions require a documented case-by-case balancing. EHDS Art. 9 requires that such information stay available **at least 3 years from each access**. | AEPD PD-00068-2026 (13 Jul 2026), interpreting GDPR Art. 15 with EHDS Art. 9; see also CJEU C-579/21 (Pankki S, 2023) | AEPD practice now; EHDS Art. 9 from 26 Mar 2029/2031 | **FW** | [S] AEPD via Cuatrecasas; [S] EHDS Art. 9; [K] C-579/21 |
| R24 | Patient portal or website:<br>• LSSI information duties;<br>• **cookie consent**, except for strictly necessary cookies;<br>• commercial e-mail rules. | Ley 34/2002 (LSSI) arts. 10, 21, 22.2 | In force | App, FW (consent records) | [K] |
| R25 | **Invoicing.** Health services are VAT-exempt under LIVA art. 20.Uno.3º but **still require an invoice**: RD 1619/2012 art. 3.1.a exempts from invoicing only exempt operations *other than* 20.Uno 2º–5º and others. | RD 1619/2012 art. 3.1.a (consolidated 31/03/2026) | In force | App | [V] |
| R26 | **Verifactu.** Invoicing software (SIF) must guarantee integrity, conservation, accessibility, legibility, traceability and inalterability of invoice records, through chained records, QR codes, and either submission to the AEAT or signed records. Mandatory for IS taxpayers from **1 Jan 2027** and **for others, including IRPF professionals, from 1 Jul 2027**. Software producers have had to comply since 29 Jul 2025. | Ley 58/2003 art. 29.2.j; RD 1007/2023; Orden HAC/1177/2024; **RDL 15/2025** (BOE 3 Dec 2025, validated 11 Dec 2025) | Not yet mandatory for the practice | App (or a dedicated package) | [S] (KPMG, Noticias Jurídicas); [K] technical articles |
| R27 | **B2B e-invoicing.** This applies only when the recipient is a business or professional, e.g. invoices to insurers or companies; **not to patients (B2C)**. It applies **12 months after the ministerial order** (turnover over €8M) or **24 months after it** (everyone else). The order was only a draft in public hearing (April–May 2026). | Ley 18/2022 art. 12; **RD 238/2026** (BOE 31 Mar 2026), DF 4ª, arts. 3–4 | Not applicable yet | App | [V] BOE; [S] order status |
| R28 | **EHDS.** Details in §3.3. Chapter II rights apply to priority categories (patient summary: problems, medication, treatment plans; ePrescription; eDispensation) **from 26 Mar 2029**, and to imaging, test results and discharge reports from 26 Mar 2031. Chapter III requires EHR systems placed on the market or put into service to include the **interoperability component (EEHRxF import/export) and the logging component** (Art. 25, Annex II), with CE marking, an EU declaration of conformity and registration (Arts. 37–41, 49). The dates are the same. **Micro-enterprises are exempt from secondary-use data-holder duties** (Art. 50). | Reg. (EU) 2025/327, Arts. 2(2)(k), 3–10, 13, 14, 25–41, 49–51, 105 | In force since 26 Mar 2025; not yet applicable | FW (logging, export), App | [V] via EC FAQ |
| R29 | **MDR.** The app is not a medical device while it only stores, archives, communicates or searches records. It **becomes medical-device software** if intended to give information for diagnosis or therapy decisions. Examples: automated risk scoring or interpretation of psychometric tests, suicide-risk prediction, digital therapeutic (CBT) modules. Rule 11 then gives class IIa or higher, which needs a notified body. A targeted MDR revision is pending (COM/2025/1023). | Reg. (EU) 2017/745 Art. 2(1), Annex VIII Rule 11, Art. 5(5) (in-house); MDCG 2019-11 rev.1 (June 2025) | In force | App (intended-purpose control), Org | [S] MDCG; [K] Rule 11 text |
| R30 | **AI Act**, if AI features exist (session transcription or summaries, a patient chatbot):<br>• Art. 4 AI literacy (softened by the Omnibus);<br>• **Art. 50 transparency** (chatbots, synthetic content), applicable from 2 Aug 2026, with Art. 50(2) grace to 2 Dec 2026 for systems already on the market;<br>• high-risk rules only if Annex III (e.g. emergency triage) or Annex I (an AI medical device), deferred to **2 Dec 2027 / 2 Aug 2028**.<br>Transcription or summarisation for the psychologist is **not high-risk per se**, but brings a GDPR DPIA (criterion 10) and processor/transfer issues. | Reg. (EU) 2024/1689 as amended by **Reg. (EU) 2026/1744** (OJ 24 Jul 2026, in force 27 Jul 2026) | Partly applicable | FW (AI registry, transparency), App | [S] |
| R31 | **NIS2** does not apply. Healthcare providers are in Annex I, but the size-cap rule (Art. 2(1): medium-sized or larger, i.e. 50+ staff or over €10M) excludes a micro practice. Spain had **not transposed** it as of July 2026 reports (anteproyecto of the Ley de Coordinación y Gobernanza de la Ciberseguridad). | Dir. (EU) 2022/2555 Art. 2, Annex I | Not applicable | — | [S] transposition; [K] Art. 2 |
| R32 | **ENS** applies only if the practice provides services to the public sector, e.g. under a concierto or contract. | RD 311/2022 art. 2 | Conditional | Org | [K] |
| R33 | **CRA.** An app built for own use and not placed on the market is outside scope. If the maintainer sells or distributes it, the CRA applies: reporting from 11 Sep 2026, full obligations from 11 Dec 2027. Encina itself as non-monetised FOSS is largely exempt; open-source stewards have light duties. | Reg. (EU) 2024/2847 | Conditional | Org | [S] |
| R34 | E-signature and trusted time-stamps for consent documents and evidential records. Optional, but they strengthen R15 and R12's "evidencia digital". | Reg. (EU) 910/2014 as amended by 2024/1183 (eIDAS2); Ley 6/2020 | Optional facilitator | FW (integration points) | [K] |
| R35 | Criminal protection of professional secrecy and of data. | Código Penal arts. 197, 199.2 | In force | Org | [K] |

---

## 3. Key analyses behind the table

### 3.1 Is the practice a "centro sanitario"?

[S]/[I] Yes, in practice. A PGS practice must obtain a health authorisation under RD 1277/2003 and the regional rules. Consequences:

1. It holds Ley 41/2002 duties as a centre: custody, archive and access.
2. If several psychologists practise together as a centre (a company, or a shared practice registered as one centre), LOPDGDD art. 34.1.l probably requires a **DPO**. A single self-employed psychologist is exempt.
3. The AEPD Art. 35.5 exemption from DPIA (item 4, "trabajadores autónomos que ejerzan de forma individual, en particular médicos, profesionales de la salud") no longer clearly applies.

### 3.2 Is a DPIA mandatory for a small practice?

[V] The AEPD 35.4 list requires a DPIA "en la mayoría de los casos" when two or more criteria are met. Health data is **criterion 4**. Two AEPD lists apply:
- The 35.5 list exempts the individual self-employed health professional, **unless** the processing significantly meets two or more 35.4 criteria.
- [V] Recital 91 says that patient data processed by an individual physician is not large scale, so Art. 35(3)(b) does not trigger.

[I] Result:

| Situation | DPIA? |
|---|---|
| A single psychologist with adult patients and no new technology | Probably not mandatory; the reasoning must be documented. |
| The practice treats **children under 14** (criterion 9) | A DPIA is expected. |
| The practice uses **AI transcription or summaries** or a novel portal (criterion 10) | A DPIA is expected. |
| The practice combines datasets across purposes (criterion 8) | A DPIA is expected. |
| A multi-psychologist centre | Treat as DPIA-expected. |

Encina's DPIA module (Marten-only) can support this. Its criteria should map to the AEPD lists (gap G15).

### 3.3 Does the EHDS apply to this app?

- [V] (EC FAQ Q22) An "EHR system" is any software that allows priority-category data "to be stored, intermediated, exported, imported, converted, edited or viewed", **intended by the manufacturer** for use by healthcare providers in patient care, or by patients. The FAQ's own example of what is in scope is "systems used by clinicians for recording notes … up to a patient management system".
- [V] Out of scope: pure appointment scheduling, and billing systems that do not process priority data.
- [S] (quoting the Regulation) "EHR systems … manufactured and used within health institutions established in the Union, as well as EHR systems offered as a service … shall be considered as having been put into service." **A self-developed in-house system therefore counts as put into service.**

[I] The decisive question is whether psychology notes contain **priority-category** data. The patient summary covers health problems, medication and treatment plans. A psychology record with a diagnosis (e.g. ICD-11 or DSM-5 codes) and a treatment plan is very likely patient-summary-relevant.

If it is in scope:
- from **26 Mar 2029**, the system must provide the EEHRxF interoperability and logging components;
- it must pass the EU testing environment;
- it needs technical documentation, an EU declaration of conformity, CE marking and EU database registration.

This is a heavy burden for a one-person practice. The patient-summary dataset and the logging specifications are due in implementing acts by 26 Mar 2027.

**Framework implication:** Encina should make the **logging component** (who, when, which data) achievable. It should leave EEHRxF (FHIR-based) export as an extension point (post-1.0) rather than block it.

- [V] Art. 50: a micro-enterprise healthcare provider is exempt from secondary-use data-holder duties unless national law extends them.

### 3.4 Minimum retention against erasure (the central conflict)

[V]/[I] The lifecycle of a clinical record:

1. **Active:** during care.
2. **Retained:** after discharge of the episode. At least 5 years under Ley 41/2002 art. 17.1; regional law may require longer. Erasure requests are **refused with reasons** under GDPR Art. 17(3)(b) (legal obligation) and (c) (Art. 9(2)(h)/(i) public-health purposes), possibly also (e) (legal claims).
3. **Erasure due:** after the floor passes, or when a patient's request becomes grantable. LOPDGDD art. 32 requires **blocking**, not destruction. Blocked data is invisible to normal users, available only to courts and authorities, and kept until the limitation period of liabilities ends. That period is [K] up to 5 years for general civil actions under Código Civil art. 1964.2, and 3 years for "very serious" LOPDGDD infringements under art. 72. Only then does **destruction** or crypto-shredding happen.
4. **Legal hold** (litigation or a court request) suspends any transition to destruction.

[I] The same applies to **rectification**: LOPDGDD art. 32.1 requires blocking on rectification too. Superseded versions of a clinical note must remain, blocked. This aligns naturally with event sourcing and versioning. It needs a "blocked history" access rule so that old versions are not shown to ordinary users.

Invoices follow a parallel track: tax retention (the LGT limitation period, [K] 4 years, arts. 66–70) and Verifactu inalterability.

### 3.5 AI features

- [S] Transcription or summarisation of sessions is not an Annex III high-risk use.
- [I] It becomes high-risk only if the AI function is (part of) a medical device, e.g. diagnostic suggestions or risk scores (Annex I, from 2 Aug 2028).
- The practical obligations are:
  - GDPR: a DPIA; an Art. 28 processor contract; Chapter V if the model is hosted outside the EU; possibly consent for recording sessions (Código Deontológico art. 47 and patient information);
  - AI Act: Art. 50 disclosure if patients interact with a chatbot; Art. 4 literacy measures.

---

## 4. (b) Capability matrix

"Scope" is **FW** (framework scope), **App** (application scope) or **Org** (organisational). Status is **Covered / Partial / Missing / Conflict**; Conflict means it would block or undermine compliance. Paths are relative to `src/` of the Encina repository.
- "(verified)" means I read the code myself in this session.
- Other statuses come from a read-only code sweep in this session. Where they rest only on a README, the Evidence column says so.

| # | Capability | Requirements | Scope | Encina status | Evidence / notes |
|---|---|---|---|---|---|
| C01 | Field-level encryption of clinical fields at rest | R05, R22 | FW | **Partial** | `Encina.Security.Encryption/EncryptionPipelineBehavior.cs`, `Attributes/EncryptAttribute.cs`, `Algorithms/AesGcmFieldEncryptor.cs`: AES-256-GCM on request properties in the pipeline. It is not an ORM value converter; see also orphan `EncryptedField` in #857. |
| C02 | Key management: KMS, key wrapping, rotation | R05 | FW | **Partial** | `Abstractions/IKeyProvider.cs`; `Encina.Messaging.Encryption.AwsKms/AwsKmsKeyProvider.cs`, `…AzureKeyVault/AzureKeyVaultKeyProvider.cs`. `Encina.Security.Secrets` is not wired to `IKeyProvider`. The crypto-shred key store keeps keys unwrapped (`Encina.Marten.GDPR/KeyStore/SubjectKeyDocument.cs`). |
| C03 | Pseudonymisation and anonymisation (separating identity from clinical data; de-identified case material) | R16, R22 (arts. 43, 45) | FW | **Covered** | `Encina.Compliance.Anonymization/Techniques/*`, `DefaultPseudonymizer.cs`, `DefaultTokenizer.cs`; token stores on 10 providers; `IRiskAssessor` (k-anonymity etc.). |
| C04 | **Read audit per record: who, what, when, purpose; evidential; at least 3 y; patient-facing query** | R23, R28 (Art. 9), R22 (art. 46) | FW | **Partial** | (verified) `Encina.Security.Audit/AuditedRepository.cs`: `_ = LogReadAccessAsync(...)` is fire-and-forget. `EntityId` is only set for `GetById`; collection reads pass `null`. `RequirePurpose` only logs a warning (line ~265). Only reads through Encina repositories are audited. Default purge is 365 days (`ReadAuditOptions`). Stores exist on 10 providers plus Marten. |
| C05 | Write audit and version history of record changes (authenticity of changes) | R15, R12 (rectification) | FW | **Partial / Covered** | `Encina.Security.Audit/AuditPipelineBehavior.cs`, `AuditOptions.RetentionDays = 2555`. Event-sourced aggregates (Marten) keep full history. There is no "blocked prior version" concept. |
| C06 | Tamper evidence: persisted hash chain or anchoring of audit and clinical records | R15, R12 (32.4 evidencia digital) | FW | **Partial** | `Encina.Compliance.Attestation/Providers/HashChainAttestationProvider.cs`: the chain is in memory, `StoragePath` is not used, and it is not wired to `IAuditStore`. `HttpAttestationProvider.cs` sends to external logs such as Rekor. |
| C07 | Trusted time-stamping (RFC 3161 / eIDAS qualified) and e-signature hooks | R34, R15, R19 | FW (integration) | **Missing** | No match in `src`. |
| C08 | **Retention with a minimum floor, anchored to an event (discharge or episode end), per jurisdiction and document type, including death-anchored periods** | R17, R26/R25 (tax), R12 | FW | **Conflict** | (verified) `Encina.Compliance.Retention/Services/DefaultRetentionRecordService.cs:94-95`: expiry is tracking start plus period. `RetentionPolicyType.EventBased` exists but nothing uses it. `RetentionPolicyAggregate.AutoDelete` is stored but not consulted by the sweep. The module only models "delete after N". |
| C09 | **Arbitration of erasure against retention**: a DSR erasure consults floor, hold and legal obligation and returns a reasoned partial refusal | R09, R17, R12 | FW | **Conflict** | (verified) `Encina.Compliance.DataSubjectRights/Erasure/DefaultDataErasureExecutor.cs` skips only fields with static `[PersonalData(LegalRetention = true)]`. There is no call into Retention or `ILegalHoldService`. `DSRErrors.ExemptionApplies` is defined but never used. Partial erasure with reasons (`ErasureResult`, `ErasureExemption`) does exist. |
| C10 | Legal hold, fail-closed | R12, R17 | FW | **Conflict** | (verified) `Encina.Compliance.Retention/RetentionEnforcementService.cs:156`: `hasHoldsResult.Match(Right: h => h, Left: _ => false)` treats an error as "no hold" and the sweep erases. Line 161 holds with `Guid.Empty`. |
| C11 | Retention sweep state machine | R17 | FW | **Conflict (bug)** | (verified) `GetExpiredRecordsAsync` returns `Status == Active` (line ~420). `RetentionRecordAggregate.MarkDeleted` throws unless the status is `Expired` (line 222). Nothing in `src` calls `MarkExpiredAsync`. The sweep erases, then fails to mark the record deleted, ignores the result and counts it as deleted, so the next cycle erases again. The sweep also passes the retention `EntityId` to `EraseAsync` as a subject id. |
| C12 | **Blocking (bloqueo) state**: retained, excluded from all processing and viewing, released only to a judicial or authority role, destroyed at the end of the limitation period, with an alternative secure-copy mode | R12, R17 | FW | **Missing** | No match for "bloqueo", "LOPDGDD", "Quarantine" or "Frozen". The closest pieces are Art. 18 restriction (`ProcessingRestrictionPipelineBehavior.cs`, which has no authority role or expiry) and `UnderLegalHold` (which suspends deletion, not access). |
| C13 | Crypto-shredding scoped per category, with a retention guard | R12 (final destruction), R17 | FW | **Conflict** | (verified) `Encina.Marten.GDPR/Erasure/CryptoShredErasureStrategy.cs:78-80` calls `DeleteSubjectKeysAsync(subjectId)` for **each erasable field**, and that deletes **all** key versions of the subject (`ISubjectKeyProvider.cs:31`). Erasing a marketing e-mail makes clinical fields encrypted with the same subject key unreadable, including `LegalRetention` ones. There is no reference to Retention or holds. The default `UsePostgreSqlKeyStore = false` keeps keys in memory, lost on restart. |
| C14 | Restriction of processing (Art. 18) | R09 | FW | **Partial** | `ProcessingRestrictionPipelineBehavior.cs`; the Art. 18(2) exceptions (legal claims, etc.) are not modelled. |
| C15 | **Access export with exclusions** (Ley 41 art. 18.3: third parties' data and "anotaciones subjetivas"; CD art. 42 serious-harm limit) | R18, R09 | FW | **Missing** | `DefaultDSRService.HandleAccessAsync`, `Model/AccessResponse.cs` cover access. No `Subjective`, `ThirdParty` or access-exclusion flag exists in `src` (grep, verified). |
| C16 | Portability / export formats; EEHRxF later | R09, R28 | FW | **Partial** | `Export/{Json,Csv,Xml}ExportFormatWriter.cs`; no EEHRxF/FHIR. |
| C17 | Consent records for non-clinical purposes (portal cookies, newsletter, session recording, AI use, research) | R24, R30, R22 (art. 47) | FW | **Covered** (Marten only) | `Encina.Compliance.Consent/Aggregates/ConsentAggregate.cs` (versions, proof, withdraw, renew), `ConsentRequiredPipelineBehavior.cs`. |
| C18 | **Representation**: minors (14 for data / 16 for clinical), guardians, both parents, voluntary proxies (cf. EHDS proxy services) | R13, R18.2, R28 | FW (model) + App | **Missing** | No `Guardian`, `Minor` or `Representative` in `src` (grep, verified). |
| C19 | Deceased subject status and authorised requesters, with the patient's prohibition flag | R14 | FW (model) + App | **Missing** | No match for `Deceased`. |
| C20 | Breach workflow: 72 h deadline, register, subject notification | R06 | FW | **Covered / Partial** | `Encina.Compliance.BreachNotification/Aggregates/BreachAggregate.cs`, `BreachDeadlineMonitorService.cs` (Marten only). `DefaultBreachNotifier.cs` only logs; there is no AEPD or subject channel. |
| C21 | DPIA support | R07 | FW | **Covered / Partial** | `Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs`, `RiskCriteria/SpecialCategoryDataCriterion.cs`, health template (Marten only). There is no mapping to the AEPD 35.4/35.5 lists. |
| C22 | Records of processing (Art. 30) | R04 | FW | **Covered** | `Encina.Compliance.GDPR/Model/ProcessingActivity.cs`, `Export/{Json,Csv}RoPAExporter.cs` (10 providers). |
| C23 | Art. 9(2) condition modelling (the special-category condition next to the Art. 6 basis) | R01, R04 | FW | **Missing** | `Encina.Compliance.GDPR/Model/LawfulBasis.cs` has only the six Art. 6 values; Art. 9(2) appears only in comments and DPIA text. |
| C24 | Special-category marking and masking in logs | R05, R22 | FW | **Partial** | `DataSubjectRights/Model/PersonalDataCategory.cs` (Health) is classification only. `Encina.Security.PII` masks only via explicit APIs, not globally. |
| C25 | Access control: treating-psychologist-only, break-the-glass with mandatory justification and audit | R16, R22 (art. 46), R28 (Art. 8 break-glass) | FW | **Partial** | `Encina.Security.ABAC` (XACML-style; you must write your own attribute provider). There is no break-glass concept (grep, verified). Multi-tenancy in `Encina.Tenancy`. |
| C26 | Transfers and residency, including a correct DPF model (certified-organisation basis) | R11 | FW | **Conflict (incorrect default)** | (verified) `Encina.Compliance.DataResidency/Model/RegionRegistry.cs:186-187`: `US` has `hasAdequacyDecision: true`. `Encina.Compliance.CrossBorderTransfer` has SCC/TIA aggregates but no DPF basis in `TransferBasis` (Marten only). |
| C27 | Processor agreements register | R10 | FW | **Covered** (Marten only) | `Encina.Compliance.ProcessorAgreements` (its README overclaims 10 providers). |
| C28 | AI system registry and Art. 50 transparency obligations | R30 | FW | **Partial** | `Encina.Compliance.AIAct/DefaultAIActClassifier.cs`, `Model/TransparencyObligationType.cs`; the registry is in memory only (#847 is open). |
| C29 | Invoicing ledger: Verifactu chain, QR, AEAT submission or signing; B2B e-invoice | R25–R27 | App (or an optional package) | **Missing** | No match. [I] A generic append-only hash-chained ledger (C06) could be reused by an app or satellite. |
| C30 | Backup and restore integrity and restore testing | R05(c)(d) | Org / infra | **Missing** (not framework core) | NIS2 `BusinessContinuityEvaluator` only checks configuration. |
| C31 | Secure patient messaging and portal | R05, R24 | App | **Partial** (building blocks) | `Encina.Messaging.Encryption*` encrypts broker payloads; there is no patient-messaging feature. It is app scope. |
| C32 | Persistence footprint for a small practice | all | FW | **Constraint, not a blocker** | Nine compliance modules are Marten/PostgreSQL-only (ADR-019): Consent, DSR tracking, LawfulBasis, Breach, DPIA, ProcessorAgreements, Retention, DataResidency, CrossBorderTransfer, plus Marten crypto-shredding. Choosing PostgreSQL for the whole app avoids a second database. `docs/architecture/adr/019-compliance-event-sourcing-marten.md` still mentions "13 providers". |
| C33 | Deterministic time in the retention aggregate | engineering rule | FW | **Bug** | `RetentionRecordAggregate.Apply` uses `DateTimeOffset.UtcNow` on hold release (line ~294). This breaks the `TimeProvider` rule and makes replay non-deterministic. |
| C34 | README accuracy (whether a compliance officer can trust the docs) | all | FW (docs) | **Conflict (docs)** | READMEs for BreachNotification, DataResidency, ProcessorAgreements and Retention describe stores or types that do not exist (`IBreachAuditStore`, `IResidencyAuditStore`, `IDPAStore`, `ILegalHoldManager`, `InMemoryRetention*Store`) and claim "10 providers". |

---

## 5. (c) Gaps and conflicts: candidate issues

Priority key:
- **pre-1.0 blocker**: would block the app, or silently destroy legally retained data;
- **pre-1.0**: needed for a credible compliance claim at 1.0;
- **post-1.0**: facilitator.

Existing related issues to link: #686 (compliance applicability resolver by jurisdiction), #857 (orphan `EncryptedField` attribute), #770 and #767 (retention and read-audit retention services to Scheduling), #798 (tenancy for Security.Audit/ABAC), #847 (AIAct to Marten), #1094 (compliance integration tests run in no CI shard; this likely explains why G04 was not caught).

| # | Candidate issue title | Rationale | Priority |
|---|---|---|---|
| G01 | `[FEATURE] Retention: minimum-retention floor and event-anchored start date (e.g. episode discharge) with per-jurisdiction/per-document-type policies` | Ley 41/2002 art. 17.1 requires "at least 5 years from each episode's discharge", with longer regional periods. Tax records need a similar floor. Today a policy can only express "delete N after tracking starts" (`DefaultRetentionRecordService.cs:94-95`), and `RetentionPolicyType.EventBased` is dead. A clinical app needs:<br>• `MinimumRetention` (the floor: erasure refused before it);<br>• an optional `MaximumRetention` (erasure due after it);<br>• an anchor event raised by the app (`EpisodeClosed`, `SubjectDeceased`), with the ability to re-anchor when a new episode opens;<br>• a year arithmetic that is calendar-based, not 365-day. | **pre-1.0 blocker** |
| G02 | `[BUG] RetentionEnforcementService treats a failed legal-hold check as "no hold" and deletes (fail-open)` | Line 156 maps `Left` to `false`, so an infrastructure error during the hold check leads to physical erasure of possibly held clinical data. Holds are also recorded with `Guid.Empty`. It must fail closed: skip and retry, and record a failed-check metric. | **pre-1.0 blocker** |
| G03 | `[BUG] Retention sweep erases Active records then cannot mark them Deleted; records are re-erased every cycle and the wrong id is passed as subject` | `GetExpiredRecordsAsync` returns `Active`; `MarkDeleted` requires `Expired`; nothing calls `MarkExpiredAsync`; the result of `MarkDeletedAsync` is ignored and the counter increments anyway. The audit trail of deletions (needed to prove compliance and for LOPDGDD art. 32 timing) is therefore wrong, and erasure is repeated. `EntityId` is also passed to `EraseAsync` as a data-subject id. | **pre-1.0 blocker** |
| G04 | `[FEATURE] DSR erasure must arbitrate against Retention floors, legal holds and Art. 17(3) exemptions, returning a reasoned partial refusal` | `DefaultDataErasureExecutor` only honours a static `[PersonalData(LegalRetention = true)]` with no time limit. It never calls `ILegalHoldService` or retention policies, and `DSRErrors.ExemptionApplies` is unused. A patient's erasure request during the 5-year floor must be refused for the clinical record, with the Art. 17(3)(b)/(c) reason and the date after which it becomes grantable, while contact and marketing data are still erased. Encina already has the `ErasureResult` and `ErasureExemption` vocabulary to carry this. | **pre-1.0 blocker** |
| G05 | `[FEATURE] Blocking (bloqueo, LOPDGDD art. 32) as a first-class data state: retained, invisible to normal processing, released only to an authorised authority role, destroyed after the limitation period` | Spanish law replaces immediate destruction with blocking after both rectification and erasure. Blocking covers viewing too, and destruction follows the limitation period. Proposed design:<br>• a `Blocked` state with `BlockedAtUtc`, `BlockedUntilUtc`, a reason and a legal basis;<br>• a query and pipeline filter that hides blocked data (and blocked prior versions after rectification);<br>• an audited, purpose-bound "authority disclosure" path;<br>• a scheduled transition to destruction or crypto-shredding.<br>Generalises to "Art. 18 restriction with statutory release roles". Without it, a Spanish controller must build this outside Encina and bypass the Retention and DSR modules. | **pre-1.0 blocker** (for the Spanish reference app); design jurisdiction-neutral |
| G06 | `[BUG] Crypto-shredding one erasable field destroys every key of the subject, including legally retained clinical data` | `CryptoShredErasureStrategy.EraseFieldAsync` calls `DeleteSubjectKeysAsync(subjectId)` per field, and that deletes all key versions. Needed:<br>• keys scoped per subject × data category (or per retention class);<br>• a guard that refuses shredding while any retention floor or legal hold covers data under that key;<br>• shredding as the *final* step after bloqueo.<br>Related hardening: key-encryption key via KMS for `SubjectKeyDocument`; documenting that backups keep deleted keys; changing the in-memory key-store default for production. | **pre-1.0 blocker** |
| G07 | `[FEATURE] Evidential read audit: fail-closed option, entity ids for collection reads, enforced purpose, ≥3-year default retention, per-subject "who accessed my data" query` | AEPD PD-00068-2026 (13 Jul 2026) expects disclosure of identity, time and data accessed; EHDS Art. 9 requires availability for at least 3 years. Current gaps:<br>• reads are fire-and-forget;<br>• `EntityId` is null for `GetAll`/`Find`/paged reads;<br>• `RequirePurpose` only warns;<br>• only repository reads are captured;<br>• the purge default is 365 days.<br>Also add a query by data subject (not only by entity) to answer Art. 15 requests, and map it to the EHDS logging component later. | **pre-1.0** |
| G08 | `[FEATURE] Persisted tamper-evident chain for audit and clinical record history (and wire Attestation to IAuditStore)` | Ley 41/2002 art. 14.3 requires mechanisms that guarantee the authenticity of the record and of its changes; LOPDGDD art. 32.4 relies on "evidencia digital". `HashChainAttestationProvider` keeps its chain in memory and ignores `StoragePath`. Needed: a persisted, provider-agnostic hash chain (or Merkle checkpoints) over audit entries and event streams, with a verification API. | **pre-1.0** |
| G09 | `[FEATURE] DSR access export with field-level exclusions (third-party data, professional's subjective annotations) and withholding reasons` | Ley 41/2002 art. 18.3 limits the patient's access where third parties' confidentiality or the professionals' "anotaciones subjetivas" are concerned; Código Deontológico art. 42 has a serious-harm limit. Needed: a `[PersonalData(AccessExcluded = …, Reason = …)]`-style marker or a runtime policy, so that `HandleAccessAsync` omits those fields and records why. Without it, the DSR access export would disclose them. | **pre-1.0** |
| G10 | `[FEATURE] Representation model for data subjects: minors, guardians, multiple holders of parental authority, voluntary proxies` | LOPDGDD art. 7 (14 years for data consent), Ley 41/2002 art. 9 (16 years for clinical consent; representation) and EHDS proxy services require consent and DSR requests to record *who acted for whom*, under what authority and until when. Consent and DSR aggregates have no such concept. | **pre-1.0** |
| G11 | `[FEATURE] Deceased data-subject status and authorised-requester rules (LOPDGDD art. 3, Ley 41/2002 art. 18.4)` | Relatives and heirs may request access unless the deceased prohibited it. The DSR module assumes a living requester equal to the subject. Model it as subject status plus a requester relationship (reusing G10), with a "prohibited" flag. | **pre-1.0** (small) |
| G12 | `[FEATURE] Model GDPR Art. 9(2) conditions alongside the Art. 6 lawful basis in RoPA and LawfulBasis` | Health processing needs both an Art. 6 basis and an Art. 9(2) condition (here (h), with Art. 9(3) secrecy and the national legal basis under LOPDGDD art. 9.2 / DA 17ª). The RoPA and the lawful-basis registry only hold Art. 6 values, so the Art. 30 record for a health practice cannot be complete. | **pre-1.0** |
| G13 | `[BUG] RegionRegistry marks the US as having an adequacy decision; DPF adequacy covers only certified organisations` | `RegionRegistry.cs:186-187` sets `hasAdequacyDecision: true` for all of the US. Transfers to non-certified US vendors (e.g. an AI transcription API) would pass the residency check unlawfully. The model should be "adequacy only for DPF-certified recipients". It should also add a DPF basis to `CrossBorderTransfer.TransferBasis` and record the pending CJEU appeal C-703/25 P as a known risk. | **pre-1.0** |
| G14 | `[DEBT] Compliance READMEs and ADR-019 describe stores and provider coverage that do not exist` | Four READMEs (BreachNotification, DataResidency, ProcessorAgreements, Retention) name non-existent types and claim 10-provider support; ADR-019 says "13 providers". The target users are compliance officers who rely on these claims. | **pre-1.0** |
| G15 | `[FEATURE] DPIA necessity screening mapped to AEPD Art. 35.4/35.5 lists (two-criteria rule, individual-professional exemption)` | Makes the "is a DPIA mandatory?" decision reproducible and documented for small health practices. Health data plus minors under 14, or plus AI, flips the answer. Could be implemented as a pluggable national criteria set; relates to #686. | **post-1.0** (pre-1.0 if cheap) |
| G16 | `[FEATURE] Breach notifier channels (AEPD submission package, patient communication templates) instead of log-only DefaultBreachNotifier` | Art. 33/34. The workflow and deadline exist, but notification is only a log line. At minimum, generate the Art. 33(3) content package and track the Art. 34 communications. | **post-1.0** |
| G17 | `[FEATURE] Trusted time-stamping (RFC 3161 / eIDAS qualified) and e-signature integration points` | Strengthens evidential value for record changes, consent documents and blocked-data copies (R12, R15, R34). An integration abstraction with one provider. | **post-1.0** |
| G18 | `[FEATURE] ORM-level field encryption (EF Core / Dapper / ADO / MongoDB value converters) for designated clinical columns` | Today's encryption is pipeline-level on request properties. For a health record, encryption at rest of specific columns with KMS-wrapped keys is the expected Art. 32 measure. Resolves the orphan `EncryptedField` attribute in #857. Must be implemented for all 10 providers per the house rule. | **pre-1.0** if `EncryptedField` stays public; otherwise **post-1.0** |
| G19 | `[FEATURE] Break-the-glass access with mandatory justification, time-boxing and review` | EHDS Art. 8 describes break-glass; Ley 41/2002 art. 16 limits access to treating professionals. ABAC has no emergency-override concept with a forced audit trail. Less critical for a 1–3 psychologist practice. | **post-1.0** |
| G20 | `[SPIKE] EHDS readiness: logging component and EEHRxF export as extension points for EHR-system apps built on Encina (dates 2029/2031)` | An in-house psychology EHR is probably an EHR system "put into service" (§3.3). Encina should not block the logging and interoperability components: read audit (G07) is the base for logging, and export (C16) the base for EEHRxF/FHIR. Watch the implementing acts due by 26 Mar 2027. | **post-1.0** (spike before 2027) |
| G21 | `[SPIKE] Append-only hash-chained ledger reusable for Verifactu-style invoice records` | Verifactu (mandatory from 1 Jul 2027 for IRPF professionals) needs chained, inalterable invoice records. Recommended: invoice logic stays in the app or a separate satellite, with Encina providing only the generic ledger primitive, shared with G08. | **post-1.0** |
| G22 | `[BUG] RetentionRecordAggregate.Apply uses DateTimeOffset.UtcNow (TimeProvider rule, non-deterministic replay)` | Violates the project rule "Time comes from TimeProvider". Event replay yields different timestamps. The ADR-019 code sample has the same problem. | **pre-1.0** (small) |
| G23 | `[DEBT] State the PostgreSQL/Marten requirement for compliance modules in the 1.0 scope and package READMEs` | Not a blocker for the reference app, provided the app standardises on PostgreSQL. It must be an explicit, documented 1.0 decision, because a SQL Server or MySQL shop would lose Consent, DSR, Retention, Breach, DPIA and crypto-shredding. | **pre-1.0** (docs) |

**Minimum set so that Encina does not block the reference app:** G01–G06, plus G07, G09, G12 and G13. These cover the retention/erasure/bloqueo lifecycle, access transparency and correct Art. 9 and transfer modelling.

---

## 6. (d) Uncertainties and ambiguities

1. **EHDS scope for a small in-house psychology EHR.** Two things are unclear: whether psychology notes carry "priority-category" data (the patient summary contents are only fixed by the implementing act due 26 Mar 2027), and how Chapter III obligations (CE marking, EU declaration of conformity, testing, registration) apply proportionately to a self-built system used by one practice. The "manufactured and used within health institutions … considered put into service" wording comes from a search snippet quoting the Regulation; I could not open EUR-Lex (automated access blocked). The dates and definitions come from the official EC FAQ (26 Mar 2026). [S]/[V]
2. **DPIA.** The AEPD 35.5 exemption for individual professionals is qualified by "cumpla, de forma significativa, con dos o más criterios". Whether health data alone plus one further criterion counts as "significant" is a judgement call. Multi-psychologist centres are not "individual".
3. **DPO.** When several self-employed psychologists share premises, it is unclear whether they are "a título individual" or one "centro sanitario" (LOPDGDD 34.1.l). It depends on the authorisation and registration.
4. **Regional retention rules.** Catalonia (reportedly up to 20 years after death for some documents, and 10 years since last attention for others), Galicia and País Vasco were only checked through secondary sources. The framework design should not hard-code 5 years anyway.
5. **What "alta" means in psychotherapy.** The discharge of a care episode is fuzzy when a patient simply stops attending. The app or the professional must set the anchor event, and the framework must allow a manual anchor and re-anchoring.
6. **Limitation periods for bloqueo.** The periods (civil liability [K] 5 years under Código Civil art. 1964.2; LOPDGDD infringement limitation 1–3 years under art. 72ff; criminal periods) were not verified in this session. The AEPD can set exceptions to blocking (art. 32.5), and I found no AEPD exception for health data.
7. **AEPD PD-00068-2026** was read through a law-firm summary (Cuatrecasas), not the original resolution. CJEU C-579/21 (Pankki S) is cited from knowledge: logs are data subject to access, but employee identity is not disclosed by default. The AEPD position is broader.
8. **Verifactu for self-developed software.** I did not verify how the AEAT treats a practice or maintainer that develops its own invoicing system for internal use: "productor" duties and a declaración responsable are likely. The IRPF date of 1 Jul 2027 comes from RDL 15/2025 via secondary sources (KPMG, Noticias Jurídicas); the BOE text of RDL 15/2025 was not opened.
9. **B2B e-invoicing timing** depends on a ministerial order that was in draft (April–May 2026). Its entry into force was not confirmed as of 2026-09-23.
10. **AI Act Omnibus** (Reg. (EU) 2026/1744). The dates (2 Dec 2027 / 2 Aug 2028; Art. 50 grace to 2 Dec 2026) come from law-firm summaries and the EUR-Lex listing; I did not read the OJ text. Some summaries describe the new Art. 4 wording differently.
11. **NIS2 Spanish transposition.** The last confirmed status is July 2026 (still in parliamentary process). Irrelevant for a micro practice, but relevant if the app is later offered to larger clinics.
12. **Código Deontológico.** A new code is in draft. The CNMC report of 26 Aug 2026 says nothing on records; the press reports AI provisions. Records and AI rules may change after 1.0.
13. **MDR boundary.** Whether automated scoring of standardised psychometric tests (without interpretation) is medical-device software is a borderline case. MDCG 2019-11 rev.1 has examples, but I did not read the specific ones. A targeted MDR revision is pending.
14. **Minors and separated parents** (Código Civil art. 156 as amended by LO 8/2021) and parental access to records of 12–16-year-olds were not verified. Treat G10 requirements as provisional.
15. **Encina findings** come from static reading. Items marked "(verified)" in §4 were read by me in the code; the others come from the read-only sweep, citing files. No tests were run. The runtime effect of G03 was inferred from the code paths and not reproduced.

---

## 7. Sources

**Official**
- [AEPD: list of processing requiring a DPIA (Art. 35.4)](https://www.aepd.es/documento/listas-dpia-es-35-4.pdf)
- [AEPD: list of processing not requiring a DPIA (Art. 35.5)](https://www.aepd.es/documento/listasdpia-35.5l.pdf)
- [BOE: LOPDGDD, LO 3/2018, consolidated to 27/12/2025](https://www.boe.es/buscar/act.php?id=BOE-A-2018-16673)
- [BOE: Ley 41/2002, consolidated to 01/03/2023](https://www.boe.es/buscar/act.php?id=BOE-A-2002-22188)
- [BOE: Ley 44/2003 (LOPS), consolidated to 05/06/2021](https://www.boe.es/buscar/act.php?id=BOE-A-2003-21340)
- [BOE: RD 1277/2003 (authorisation of health centres)](https://www.boe.es/buscar/doc.php?id=BOE-A-2003-19572)
- [BOE: RD 1619/2012 (invoicing), consolidated to 31/03/2026](https://www.boe.es/buscar/act.php?id=BOE-A-2012-14696)
- [BOE: RD 238/2026 (B2B e-invoicing)](https://www.boe.es/diario_boe/txt.php?id=BOE-A-2026-7295)
- [AEAT: Verifactu FAQ](https://sede.agenciatributaria.gob.es/Sede/iva/sistemas-informaticos-facturacion-verifactu/preguntas-frecuentes/sistemas-verifactu.html)
- [AEAT news: facturación electrónica obligatoria, 31 Mar 2026](https://sede.agenciatributaria.gob.es/Sede/todas-noticias/2026/marzo/31/facturacion-electronica-obligatoria.html)
- [Hacienda: draft ministerial order on the public e-invoicing solution](https://www.hacienda.gob.es/sgt/normativadoctrina/proyectos/16042026-proyecto-pom-factura-electronica.pdf)
- [European Commission (DG SANTE): EHDS FAQ v1.1, 26 Mar 2026](https://health.ec.europa.eu/document/download/4dd47ec2-71dd-49fc-b036-ad7c14f6ed68_en?filename=ehealth_ehds_qa_en.pdf)
- [EUR-Lex: EHDS Regulation (EU) 2025/327](https://eur-lex.europa.eu/eli/reg/2025/327/oj/eng)
- [EUR-Lex: Regulation (EU) 2026/1744 (Digital Omnibus on AI)](https://eur-lex.europa.eu/eli/reg/2026/1744/oj/eng)
- [Consejo General de la Psicología: Código Deontológico (in force)](https://www.cop.es/pdf/CodigoDeontologicodelPsicologo-vigente.pdf)
- [CNMC report on the draft Código Deontológico, 26 Aug 2026](https://www.cnmc.es/prensa/inf-codigo-deontologico-psicologia-20260826)
- [Gencat: authorisation of psychology consultations](https://tramits.gencat.cat/es/tramits/tramits-temes/Autoritzacio-de-consultes-de-psicologia-clinica-i-psicologia-general-sanitaria?moda=1)
- [DSN: anteproyecto de Ley de Coordinación y Gobernanza de la Ciberseguridad](https://www.dsn.gob.es/en/node/24160)
- [European Commission: CRA reporting obligations](https://digital-strategy.ec.europa.eu/en/policies/cra-reporting)

**Secondary**
- [Cuatrecasas: traceability of access to health data (AEPD PD-00068-2026)](https://www.cuatrecasas.com/en/global/life-sciences-healthcare/art/traceability-access-health-data)
- [Streamlex: EHDS Art. 9](https://streamlex.eu/articles/ehds-en-art-9/)
- [ehds-jurist.nl: EHDS timeline](https://ehds-jurist.nl/en/tijdlijn)
- [KPMG: RDL 15/2025 tax alert](https://assets.kpmg.com/content/dam/kpmgsites/es/pdf/2025/12/tax-alert-el-real-decreto-ley-15-2025-amplia-plazos-adaptacion-reglamento-verifactu.pdf.coredownload.inline.pdf)
- [Noticias Jurídicas: Verifactu postponed to 2027](https://noticias.juridicas.com/actualidad/noticias/20735-nueva-prorroga:-verifactu-no-sera-obligatorio-hasta-2027-para-sociedades-y-otros-contribuyentes/)
- [Cuatrecasas: mandatory B2B e-invoicing](https://www.cuatrecasas.com/es/spain/fiscalidad/art/facturacion-electronica-obligatoria-operaciones-b2b)
- [inza.blog: draft order on the public e-invoicing solution](https://inza.blog/2026/04/18/ya-tenemos-el-proyecto-de-la-orden-ministerial-que-regulara-la-solucion-publica-de-facturacion-electronica/)
- [Lewis Silkin: Digital Omnibus on AI enters into force](https://www.lewissilkin.com/insights/2026/07/27/the-digital-omnibus-on-ai-enters-into-force-today-102nedo)
- [Hunton: EU Digital Omnibus on AI enters into force](https://www.hunton.com/privacy-and-cybersecurity-law-blog/eu-digital-omnibus-on-ai-enters-into-force)
- [Emergo by UL: MDCG 2019-11 rev.1](https://www.emergobyul.com/news/european-revision-primary-software-guidance-mdcg-2019-11-revision-1-small-changes-meaningful)
- [Digital Policy Alert: Latombe appeal](https://digitalpolicyalert.org/event/35459-latombe-filed-appeal-against-general-court-dismissal-of-challenge-to-european-unionunited-states-data-protection-framework-adequacy-decision-in-latombe-v-commission)
- [IAPP: General Court upholds the DPF in Latombe](https://iapp.org/news/a/european-general-court-dismisses-latombe-challenge-upholds-eu-us-data-privacy-framework)
- [nisd2.eu: NIS2 status in Spain](https://nisd2.eu/es/wiki/timelines-and-status/nis2-status-spain)
- [PSN Sercon: regional retention periods for clinical records](https://blog.psnsercon.com/los-plazos-de-conservacion-de-las-historias-clinicas-en-las-distintas-comunidades-autonomas/)
- [COPIB: psychology consultation authorisation](https://copib.es/es/noticias/recordatorio-renovacion-autorizacion-funcionamiento-centros-sanitarios-especializados-consultas-gabinetes-psicologia-registro-correspondiente-centros-servicios-establecimientos-sanitarios)
- [COP Madrid: authorisation regime for psychology consultations](https://copmadrid.es/webcopm/recursos/autorizacion_consultaspsi.pdf)
- [Redacción Médica: psychology finalises its deontological code](https://www.redaccionmedica.com/secciones/otras-profesiones/psicologia-ultima-su-codigo-deontologico-con-novedades-en-ia-y-pacientes-3253)
