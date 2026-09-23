# H — Integrations law: invoicing and Verifactu, payments, patient messaging, calendar and mail vendors

Date: 2026-09-23. Read-only research. Nothing was edited, pushed or opened.

**Scope.** The law that applies when a small Spanish health practice (the reference use case of note [F](F-health-practice.md)) issues invoices with software, takes card and Bizum payments through payment gateways, messages its patients by e-mail, SMS or WhatsApp, and synchronises appointments with a hosted calendar. The health-data profile itself is in note F; this note adds only what is new. It describes the scenario's needs, not any particular application. Row identifiers (H-R01 …) are kept stable because SPEC-002 cites them.

**Verification legend.**
- **[V]**: verified against a primary source (BOE, AEAT, EUR-Lex or EP, or the vendor's own terms or documentation).
- **[S]**: verified only through a reputable secondary source.
- **[K]**: background knowledge, not re-verified. Treat it as a claim to confirm.
- **[I]**: inference.

Vendor names (Stripe, Redsys, Meta, Google, Microsoft) appear only as examples of the kinds of service such a practice uses and of the contractual terms those services offer; they are not a choice made for any application.

---

## 0. Executive summary

1. **An application that issues invoices is a Verifactu "sistema informático de facturación" (SIF), and its producer must sign a *declaración responsable* for every version.**
   - A taxpayer who develops its own SIF must certify it [V].
   - VAT-exempt health services still fall under the regime once software is used to invoice [V AEAT FAQ, updated 22 Jul 2026].
   - Application date: **1 Jul 2027** for a self-employed (IRPF) professional, or **1 Jan 2027** for a company paying corporate tax (RDL 15/2025) [V].
   - Producers of systems must already be compliant.
2. **Producer liability decides whether a framework can ship Verifactu code.** Two AEAT answers pull in different directions [V]:
   - for open-source code, "the company that programs or integrates" makes the declaration;
   - for systems built from components, every component relevant to compliance needs its own declaration. The relevant components are those touching record generation, chaining, QR, submission, unaltered conservation and the event log.

   A Verifactu-specific library would plausibly make its publisher a component "fabricante". Sanctions are €150,000 per year and per system type [S] (LGT art. 201 bis). A framework that wants to stay outside the regime ships only regulation-neutral primitives and leaves the Verifactu module to the application (§4).
3. **VERI\*FACTU mode (real-time submission) is simpler than the non-verifiable mode.** VERI\*FACTU systems are presumed compliant by design, need no electronic signature (only the hash), and fall outside the art. 14.2 regime [V RD 1007/2023 art. 16]. The non-verifiable mode needs XAdES Enveloped signatures and an event log with a summary every 6 hours [V summary of the Orden HAC/1177/2024 text]. What remains in VERI\*FACTU mode: hash chain, QR, and a reliable, never-drop submission with a qualified certificate.
4. **Messages and calendar entries of a psychology practice are health data** [I]: a reminder or an appointment reveals that the recipient receives psychological care. Channels and calendar vendors need processor terms, minimal content and a transfer basis.
5. **Consumer accounts have no processor terms.** Consumer Google and Microsoft accounts are not covered by the vendors' data-processing addenda [V]/[K]; a practice needs business accounts under a DPA.
6. **Payment gateways can be processor and independent controller at once.** Stripe's DPA, for example, makes Stripe a processor for payment services and an independent controller for fraud prevention and AML [V].

---

## 1. Invoicing and Verifactu

| ID | Requirement | Source | Date / status | Who | Verif. |
|---|---|---|---|---|---|
| H-R01 | **Who is in scope.** The regime covers taxpayers of IS; IRPF taxpayers with economic activities; IRNR with a permanent establishment; and *atribución de rentas* entities (art. 3.1). It also covers **producers and sellers** of the systems "puestos a disposición" of those taxpayers (art. 3.2). SII taxpayers are excluded (art. 3.3, via RIVA art. 62.6). | RD 1007/2023 arts. 1.2, 3 (consolidated 03/12/2025) | In force | Practice | [V] |
| H-R02 | **Application dates.** 1 Jan 2027 for IS taxpayers; 1 Jul 2027 for everyone else. Producers were not deferred: they have had to comply since 29 Jul 2025. | RDL 15/2025 art. 3 (BOE 03/12/2025, validated 11/12/2025) amending DF 4ª of RD 1007/2023 | Current | Practice; producer | [V] dates; [S] the "producers not deferred" point |
| H-R03 | **VAT-exempt health services are not an escape.** A professional who issues invoices only for exempt operations must comply *if it uses invoicing software*. Only fully manual invoicing, or a plain spreadsheet or word processor without logic, escapes. | AEAT FAQ "ámbitos de aplicación" (updated 22 Jul 2026) | Current | Application | [V] |
| H-R04 | **SIF requirements** (art. 8): integrity and inalterability (corrections only through new records); traceability (chained records, timestamp); conservation, accessibility and legibility; and an event log. The registration record (*registro de alta*, art. 10) carries the previous record's identity plus part of its **hash**. Art. 12 adds hash and electronic signature. | RD 1007/2023 arts. 8, 10, 12 | In force | Application | [V] |
| H-R05 | **VERI\*FACTU mode.** When all records are sent to the AEAT continuously and automatically, the system is presumed compliant by design, art. 14.2 does not apply, and **no signature is needed, only the hash**. Once chosen, the mode lasts at least until the end of the calendar year. The AEAT's free invoicing application counts as VERI\*FACTU. | RD 1007/2023 art. 16; art. 7.b | In force | Application | [V] |
| H-R06 | **Technical specification.** The order defines a "billing component" (componente de facturación) and a "main billing component" (componente principal). Hash algorithm details are published at the AEAT portal (SHA-256 per list L12). The non-verifiable mode needs **XAdES Enveloped** signatures (ETSI EN 319 132) with a qualified certificate, and an **event log** with a summary at least every 6 operating hours. QR per ISO/IEC 18004, error-correction level M, about 30–40 mm, with the AEAT URL plus NIF, series and number, date and total. Submission is XML over the AEAT web service, with flow control (60 s wait, at most 1,000 records per submission). | Orden HAC/1177/2024 (BOE 28/10/2024, in force 29/10/2024) arts. 1.2, 9, 13, 14, 16, 21 | In force | Application | [V] (via a fetched-page summary; exact field concatenation not read) |
| H-R07 | **Invoice legend and QR.** The invoice must carry the QR and, in VERI\*FACTU mode, "Factura verificable en la sede electrónica de la AEAT" or "VERI\*FACTU". | RD 1619/2012 art. 6.5 (added by RD 1007/2023 DF 1ª; consolidated 31/03/2026) | In force from the application date | Application | [V] |
| H-R08 | **Declaración responsable** (producer's statement of compliance). The producer certifies compliance in writing (art. 13.1). The statement must be **visible in the system in every version** and given to the client (13.2). The producer keeps those of all versions (13.3). It identifies the system, its typology, **composition** and functionalities, and the producer (13.4). | RD 1007/2023 art. 13 | In force | **Producer of the application** | [V] |
| H-R09 | **Self-built and open-source software.** Every system in operation needs a certification. If the company developed it itself, it certifies it. With open source, "the company that programs the code or integrates parts of other software, open source or not" makes the statement and lists the components used. | AEAT FAQ "Certificación… declaración responsable" (updated 22 Jul 2026) | Current | Producer of the application | [V] |
| H-R10 | **Multi-component systems.** Every component relevant to compliance needs a certification from its manufacturer. Components irrelevant to compliance are exempt: those that do not affect record generation, chaining, invoice printing, QR, submission, the indefectible link, unaltered conservation or the event log. The main component's statement must name the components it calls, and their versions. | AEAT "Aclaraciones a dudas de los desarrolladores" v1.3, 4 Dec 2025, §5 (pp. 13-15) and §8-9 | Current | Every component producer | [V] |
| H-R11 | **Sanctions.** €150,000 per year and per system type for producing or selling non-compliant or uncertified systems; €50,000 per year for the user holding them. | LGT art. 201 bis (Ley 11/2021) | In force | Producer; practice | [S] (BOE consolidated text truncated when fetched) |
| H-R12 | **Submission credentials.** Submission requires a valid qualified certificate: the taxpayer's own, or a representative's through *apoderamiento* or "colaboración social" (Convenio 17). Client-certificate (mTLS) retrieval and rotation therefore matter to the application. | AEAT developer FAQ pp. 3, 33 | Current | Application | [V] |
| H-R13 | **Invoice duty and content for health services.** LIVA 20.Uno.3º services require an invoice (RD 1619/2012 art. 3.1.a). A full invoice must cite the exemption (art. 6.1.j). A simplified invoice is allowed up to €400 incl. VAT (art. 4). Numbering is correlative within a series [K]. Corrective invoices are governed by art. 15. Conservation follows the LGT (art. 19). | RD 1619/2012 (consolidated 31/03/2026) | In force | Application | [V] (fetched summary); [K] numbering |
| H-R14 | **Scope of the VAT exemption.** Only services by a qualified health professional, with a diagnostic, preventive or therapeutic purpose, are exempt; telematic delivery does not matter. Expert reports, courses, coaching and staff selection bear 21 % VAT. An invoicing application therefore needs a **per-service VAT treatment**, not one global "exempt" flag. | LIVA art. 20.Uno.3º; DGT V1601-14 (via SuperContable) | In force | Application | [S] |
| H-R15 | **Foral territories.** Taxpayers under Basque or Navarre tax jurisdiction are outside RD 1007/2023. The Basque Country uses TicketBAI (and Batuz in Bizkaia). | AEAT FAQ ámbitos | Current | Application (only if the practice is foral) | [V]; TicketBAI details [K] |
| H-R16 | **SII.** A regime exclusive of Verifactu. Mandatory for large companies (over €6M turnover), REDEME, VAT groups [K]. **Not applicable** to a small exempt practice [I]. | RD 1007/2023 art. 3.3; AEAT FAQ | — | — | [V] exclusivity; [K] SII thresholds |
| H-R17 | **B2B e-invoicing** (Crea y Crece): only for invoices to businesses (insurers, companies, employee-assistance programmes), **not to patients**. | See F R27 (Ley 18/2022 art. 12; RD 238/2026) | Awaits the ministerial order | Application | per F |
| H-R18 | **Retention of invoices and Verifactu records.** At least the LGT limitation period (4 years from the end of the filing period, arts. 66 and 70 [K]). Código de Comercio art. 30 requires 6 years for "empresarios" [S]; whether it applies to a liberal professional is uncertain. Invoices are exempt from erasure under GDPR Art. 17(3)(b), and **inalterable**: they are corrected only through corrective records, never updated. For a retention framework this means an *immutable fiscal record* class with a 4-to-6-year floor, erasure refused during the floor, and no crypto-shredding key shared with erasable marketing or contact data. | LGT; CCom art. 30; RD 1619/2012 art. 19; RD 1007/2023 art. 8 | In force | Application, with the framework's retention module | mixed |
| H-R19 | **Health data in Verifactu records.** In VERI\*FACTU mode each record (recipient NIF or name, operation description) goes to the AEAT in real time. The description should therefore be minimal ("Servicios profesionales de psicología"), with no clinical detail. | GDPR Art. 5(1)(c), Art. 6(1)(c) [I] | — | Application | [I] |

## 2. Payments

| ID | Requirement | Source | Date / status | Who | Verif. |
|---|---|---|---|---|---|
| H-R20 | **Strong customer authentication (SCA).** The payment service provider (PSP) applies it. The merchant's duty is contractual: use the gateway's 3-D Secure flow, and flag merchant-initiated or recurring charges (for example automatic renewal of session packs) correctly. A hosted checkout handles this. | PSD2 (Dir. 2015/2366) art. 97; RTS (EU) 2018/389 | In force | Gateway (PSP) | [K] |
| H-R21 | **PSD3/PSR.** Provisional agreement 27 Nov 2025; ECON approved the text 5 May 2026; formal adoption pending (EP status 1 Aug 2026); not yet in the Official Journal. PSPs carry more fraud liability, including impersonation and payee verification. Little falls directly on a merchant application. | EP Legislative Train | Pending | PSP | [V] status; [S] timelines |
| H-R22 | **PCI DSS v4.0.1.** Redirect to a hosted page = SAQ A. Embedded iframes kept SAQ A from 31 Mar 2025 only with new eligibility criteria: all payment-page elements come from a PCI DSS compliant PSP, and the merchant confirms its site is not susceptible to script attacks (replacing 6.4.3 / 11.6.1). The application must never receive a card number (PAN). | PCI SSC SAQ A (Jan 2025) | Current | Application and PSP | [S] |
| H-R23 | **Gateway roles.** A gateway can be a processor for one purpose and an independent controller for another. Example: under its DPA, Stripe is a processor for payment services and an **independent controller** for fraud prevention, AML/KYC, platform operation and product improvement; transfers rely on SCC and the DPF. An application should not put therapy details in checkout descriptions or metadata. | Stripe DPA (last updated 18 Nov 2025) | Current | Application | [V] |
| H-R24 | **Bizum and card collections are reported to the AEAT.** From 1 Jan 2026 banks and payment entities report monthly (modelo 170) the card and mobile-number (Bizum) collections of businesses and professionals. Manually recorded Bizum payments must reconcile with invoices. | RD 253/2025 (RGAT art. 38 bis); Orden HAC/747/2025 | In force | Application (reconciliation) | [S] |
| H-R25 | **Cash limit.** €1,000 when one party acts as a business or professional. | Ley 7/2012 art. 7 (as amended by Ley 11/2021) | In force | Application | [K] |
| H-R26 | **AML** (Ley 10/2010) does not apply: psychologists are not obliged subjects. KYC is the PSP's job. | Ley 10/2010 art. 2 | — | — | [K] |
| H-R27 | **Refunds and chargebacks.** A refund or chargeback of an invoiced service needs a **corrective invoice** (RD 1619/2012 art. 15) and a Verifactu corrective record. Gateway events must drive that flow reliably and idempotently: signed webhooks, deduplication on the provider's event id, and a saga from refund to corrective invoice. Gateway signature schemes differ (for example a timestamped HMAC header for API gateways; a per-order derived key for Redsys' signed redirect). | RD 1619/2012 art. 15 | In force | Application | [V] art. 15; [I] flow |

## 3. WhatsApp, e-mail and SMS

| ID | Requirement | Source | Date / status | Who | Verif. |
|---|---|---|---|---|---|
| H-R28 | **Reminders reveal health data** [I]. A message from a psychologist's practice reveals that the recipient receives psychological care, which is Art. 9 data under the broad reading of CJEU C-184/20 [K]. It needs an Art. 9(2)(h) basis, minimal content (no service type or diagnosis), a processor contract with the channel provider, and a transfer basis. | GDPR Arts. 9, 28, 44ff | In force | Application | [I] / [K] |
| H-R29 | **WhatsApp Cloud API.** Meta acts as **processor**. Messages are decrypted on Meta's servers (not end-to-end to the business). Retention is at most 30 days. EU/UK→US transfers use "appropriate GDPR-compliant transfer mechanisms". A Local Storage option exists. | Meta for Developers, "Data privacy & security" (page undated) | Current | Application | [V] |
| H-R30 | **WhatsApp Business Messaging Policy.** Opt-in is required (phone number plus explicit permission). Opt-outs must be honoured on or off WhatsApp. Businesses must not ask for card numbers, account numbers or ID numbers. Health information is prohibited "if applicable regulations prohibit distribution" to systems without heightened safeguards. Business-initiated messages need approved templates outside the 24-hour window. A "medical and healthcare products" commerce restriction also exists; whether it covers therapy services is unclear. | whatsappbusiness.com/policy (undated) | Current | Application | [V] |
| H-R31 | **Commercial messages vs reminders.** Promotional messages by e-mail or an "equivalent electronic means" (SMS, WhatsApp) need prior consent, or the art. 21.2 exception for similar services to existing clients with an opt-out in every message. Revocation must be free and simple (art. 22.1). Appointment reminders are **not** commercial communications under the Annex (f) definition [I]. Consent is therefore needed per purpose and per channel. | LSSI arts. 20, 21, 22.1, Annex f (consolidated 23/01/2025) | In force | Application, with the framework's consent module | [V] |
| H-R32 | **AEPD on e-mail.** Health professionals should avoid sending health information by e-mail or open networks unless it is encrypted. | AEPD FAQ-1638 | Current | Application | [V] |
| H-R33 | **Consumer e-mail accounts have no DPA.** A consumer Microsoft mail account (Outlook.com) has no GDPR Art. 28 processor terms. A business account (Microsoft 365 with its DPA) or an EU mail provider is needed. | Microsoft Products and Services DPA scope | — | Application | [K] |

## 4. Calendar, workspace and video

| ID | Requirement | Source | Date / status | Who | Verif. |
|---|---|---|---|---|---|
| H-R34 | **Consumer Google accounts are not covered.** Google's Cloud Data Processing Addendum covers Workspace, Cloud and others, **not consumer accounts or free Gmail**. Under it Google is a processor, and deletes customer data within 180 days after a 30-day recovery period. | cloud.google.com/terms/data-processing-addendum (undated) | Current | Application | [V] |
| H-R35 | **Data regions.** Workspace "fundamental data regions" (Europe) exist in Business Standard, Business Plus and above; Business Starter has none. | Workspace Admin Help, "compare data region features" | Current | Application | [V] |
| H-R36 | **Minimise calendar events.** Titles, descriptions and extended properties sent to a hosted calendar should hold no patient name and **no clinical or internal notes**; initials or neutral labels are enough. Calendar sync also needs OAuth tokens kept encrypted and refreshed safely. | GDPR Art. 5(1)(c), Art. 25 [I] | — | Application | [I] |
| H-R37 | **Transfers to US vendors** (calendar, mail, messaging, payments). They rely on DPF certification or SCC. The pending DPF appeal C-703/25 P is a risk; see F G13 (a transfer check that treats the whole US as adequate is wrong). | F R11/G13 | — | Application and framework | per F |
| H-R38 | **Video sessions** (if adopted): a processor DPA, preferably end-to-end encryption, no recording by default, and a telehealth consent notice. | GDPR Arts. 28, 32 | — | Application | [I] |

---

## 5. The Verifactu producer question for a framework

**Verified facts:**
1. Art. 3.2 applies the Regulation to producers and sellers of systems "puestos a disposición" of obligated taxpayers [V].
2. Art. 13 gives the producer the declaration, visible in every version, with the system's **composition** [V].
3. AEAT, open source: the company that programs or integrates, "ya sea o no de código abierto", makes the declaration "e indicar qué componentes utiliza" [V].
4. AEAT, multi-component: "tantas Certificaciones como componentes afectan al cumplimiento" [V].
5. The developer FAQ says every component that implements requirements must be certified by its manufacturer. It exempts only components irrelevant to compliance: those not affecting record generation, chaining, invoice printing, QR, submission, the component link, unaltered conservation or the event log (§5, pp. 13-15) [V]. A third-party component has "un ciclo evolutivo propio" and its own statement, and the main component's statement must name its version (p. 14 a–c) [V].

**Inference:**
- A NuGet package that implements Verifactu chaining, QR, record XML or AEAT submission is a "componente de facturación" in the Order's sense. Under fact 5, its publisher is a **fabricante** who should issue and keep a statement per version.
- Fact 3 suggests the integrator can absorb this for open-source code, but it was written about "software de facturación de código abierto" as a whole, not about third-party libraries with an independent lifecycle.
- The exposure is not zero. Art. 201 bis sanctions "fabricación, producción y comercialización" at €150,000 per year and per system type [S]. The "ventas" wording [S] may limit it to commercial activity, but that is unverified.
- **There is no FOSS carve-out comparable to the CRA's** (recitals 18–19; see note [A](A-encina-itself.md)).
- A regulation-neutral primitive (an append-only hash-chained log, a gap-free counter) is not "implementing RRSIF requirements" until the integrator configures it for that purpose. The argument that the integrator is the producer is strongest there, though still not certain, because "conservación inalterada" is on the relevant list.

**Options for a framework:**
- (a) Ship **no Verifactu-specific code** and document that position in an ADR; the application builds its Verifactu module (VERI\*FACTU mode) and its producer lists the framework and its version as a non-RRSIF-specific dependency in the *declaración responsable*.
- (b) Before any Verifactu-specific package, obtain a written answer from the AEAT (the developer channel, or a binding DGT ruling under LGT art. 88 if a taxpayer can frame it). The question: does a free, non-monetised, independently versioned library that computes the RRSIF hash, record XML and QR need its own declaración responsable?
- (c) An application can avoid being a SIF by issuing invoices in the **AEAT's free VERI\*FACTU application** (art. 7.b) and keeping only sessions, payments and receipts. Caveat: an application that "admits invoicing data" and forwards it to an external SIF may itself be a component under art. 1.2.a (U3).

---

## 6. Uncertainties

| # | Uncertainty |
|---|---|
| U1 | Whether an OSS library implementing RRSIF-relevant functions needs its own declaración responsable. The AEAT FAQ on open source and the developer FAQ on components point different ways (§5). Unresolved without a written AEAT answer. |
| U2 | **Art. 201 bis wording.** Read only through Iberley [S]; the BOE consolidated LGT text truncated when fetched. Whether "producción" of free software without "ventas" is sanctionable is untested. |
| U3 | **Application as a component.** Whether an application that captures billing data and forwards it to an external SIF (the AEAT application or a SaaS) is itself a "componente de facturación" under art. 1.2.a ("admitir la entrada de información de facturación por cualquier método"). |
| U4 | **Orden HAC/1177/2024 details** (hash concatenation, QR size, flow-control numbers) were read through a fetched-page summary, not article by article. The AEAT technical documents (hash specification, WSDL) were not opened. Whether the AEAT rejects or flags out-of-order chain submissions is also unverified. |
| U5 | **Record retention for a liberal professional.** Whether Código de Comercio art. 30 (6 years) binds a self-employed psychologist, or only the LGT periods (4 years plus filing period; longer where losses or credits are pending) apply. |
| U6 | PSD3/PSR Official Journal publication and application date after 1 Aug 2026; the SAQ A changes rest on secondary sources; RD 253/2025 / modelo 170 on secondary sources. |
| U7 | Whether the WhatsApp Commerce Policy's "medical and healthcare products" restriction covers therapy services or only goods. Whether Spanish law counts as "regulations that prohibit distribution" of health information to WhatsApp under the Business Messaging Policy. No AEPD statement on WhatsApp for health professionals was found. |
| U8 | The CJEU C-184/20 reading (indirect disclosure = special-category data) is applied to appointment reminders by inference [K]/[I]. |
| U9 | The Microsoft consumer-account DPA scope [K]; the Google Workspace Business Starter data-region gap is verified, but the Calendar-specific coverage line was not read. |
| U10 | Foral status (TicketBAI) is not assumed; the analysis assumes *territorio común*. |

---

## 7. Sources

**Primary**
- [BOE — RD 1007/2023 (consolidated, last update 03/12/2025)](https://www.boe.es/buscar/act.php?id=BOE-A-2023-24840)
- [BOE — RDL 15/2025, BOE-A-2025-24446](https://www.boe.es/buscar/doc.php?id=BOE-A-2025-24446) and [validation, BOE-A-2025-25695](https://www.boe.es/buscar/doc.php?id=BOE-A-2025-25695)
- [BOE — Orden HAC/1177/2024](https://www.boe.es/buscar/act.php?id=BOE-A-2024-22138)
- [BOE — RD 1619/2012 (consolidated 31/03/2026)](https://www.boe.es/buscar/act.php?id=BOE-A-2012-14696)
- [BOE — LSSI, Ley 34/2002 (consolidated 23/01/2025)](https://www.boe.es/buscar/act.php?id=BOE-A-2002-13758)
- [AEAT FAQ — Certificación: declaración responsable (updated 22 Jul 2026)](https://sede.agenciatributaria.gob.es/Sede/iva/sistemas-informaticos-facturacion-verifactu/preguntas-frecuentes/certificacion-sistemas-informaticos-declaracion-responsable.html)
- [AEAT FAQ — Ámbitos de aplicación (updated 22 Jul 2026)](https://sede.agenciatributaria.gob.es/Sede/iva/sistemas-informaticos-facturacion-verifactu/preguntas-frecuentes/cuestiones-generales-ambitos-aplicacion.html)
- [AEAT — Aclaraciones a dudas de los desarrolladores v1.3, 4 Dec 2025 (PDF)](https://sede.agenciatributaria.gob.es/static_files/AEAT_Desarrolladores/EEDD/IVA/VERI-FACTU/FAQs-Desarrolladores.pdf)
- [AEAT FAQ — Sistemas VERI\*FACTU](https://sede.agenciatributaria.gob.es/Sede/iva/sistemas-informaticos-facturacion-verifactu/preguntas-frecuentes/sistemas-verifactu.html)
- [European Parliament Legislative Train — Payment Services Regulation (status 01/08/2026)](https://www.europarl.europa.eu/legislative-train/theme-an-economy-that-works-for-people/file-revision-of-eu-rules-on-payment-services)
- [Stripe DPA (last updated 18 Nov 2025)](https://stripe.com/legal/dpa)
- [Meta — WhatsApp Cloud API data privacy & security](https://developers.facebook.com/documentation/business-messaging/whatsapp/data-privacy-and-security/)
- [WhatsApp Business Messaging Policy](https://whatsappbusiness.com/policy/)
- [Google Cloud Data Processing Addendum](https://cloud.google.com/terms/data-processing-addendum/)
- [Google Workspace — compare data region features across editions](https://knowledge.workspace.google.com/admin/compliance/compare-data-region-features-across-google-workspace-editions)
- [AEPD — FAQ profesionales sanitarios (FAQ-1638)](https://www.aepd.es/preguntas-frecuentes/16-salud/2-profesionales-sanitarios) and [Guía para profesionales del sector sanitario](https://www.aepd.es/documento/guia-profesionales-sector-sanitario.pdf)

**Secondary**
- [Iberley — LGT art. 201 bis](https://www.iberley.es/legislacion/articulo-201-bis-ley-general-tributaria)
- [Iberley — Código de Comercio art. 30](https://www.iberley.es/legislacion/articulo-30-codigo-comercio)
- [SuperContable — DGT V1601-14, IVA psicólogos](https://www.supercontable.com/pag/documentos/consultas/consultas_DGT_exencion_IVA_servicios_psicologicos_V1601-14.html)
- [Iberley — exención IVA psicología online](https://www.iberley.es/noticias/tributos-admite-exencion-iva-psicologia-online-36898)
- [HUMAN Security — SAQ A changes (FAQ 1588)](https://www.humansecurity.com/learn/blog/pci-dss-4-update-unpacking-the-changes-to-saq-a/)
- [Hyperproof — new SAQ A eligibility criteria](https://hyperproof.io/resource/pci-dss-4-0-update-new-saq-a-eligibility-criteria/)
- [Consejo General de Gestores — Bizum reporting 2026](https://www.consejogestores.org/noticias/nuevas-obligaciones-informativas-aeat-bizum-2026/)
- [Iberley — new bank reporting to AEAT from 2026](https://www.iberley.es/revista/la-nueva-informacion-cuentas-tarjetas-bizums-que-los-bancos-entidades-financieras-empezaran-suministrar-hacienda-2026-1464)
- [Norton Rose Fulbright — PSD3/PSR](https://www.nortonrosefulbright.com/en/knowledge/publications/cedd39c6/psd3-and-psr-from-provisional-agreement-to-2026-readiness)
- [Grupo Albatros — Verifactu postponed to 2027 (RDL 15/2025)](https://grupoalbatros.org/2026/02/23/verifactu-aplazamiento-2027-rdl-15-2025/)
