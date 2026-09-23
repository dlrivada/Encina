# E — Fact-check of B-horizontal.md and C-sector-national.md against primary sources

Checked on 2026-09-23. The act texts were read directly on EUR-Lex (HTML of the OJ text), BOE (consolidated text), the EP Legislative Observatory (OEIL), the Congreso de los Diputados initiative register, Consilium, digital-strategy.ec.europa.eu and iso.org. Verdicts:

- **CONFIRMED**: the primary source says what the note says.
- **CORRECTED**: the primary source contradicts the note; the correct value is given.
- **UNVERIFIABLE**: no primary source could be reached, or the source is paywalled.

---

## 1. Regulation (EU) 2026/1744: the AI Act digital omnibus

Source: EUR-Lex, OJ L 2026/1744, <https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32026R1744> (ELI <https://eur-lex.europa.eu/eli/reg/2026/1744/oj/eng>).

| Claim (B §0, §4) | Verdict | Primary-source value |
|---|---|---|
| Regulation (EU) 2026/1744 exists; it amends 2024/1689, 2018/1139 and 2023/1230 | CONFIRMED | Regulation of 8 July 2026, "Digital Omnibus on AI" |
| Published in the OJ on 24 July 2026 | CONFIRMED | OJ L, 24.7.2026 |
| Entered into force on 27 July 2026 | CONFIRMED | Art. 4: "third day following that of its publication", which is 27.7.2026 |
| Annex III high-risk obligations move to **2 Dec 2027** | CONFIRMED | New Art. 113(c)(i): Chapter III Sections 1–3 apply from 2 December 2027 to systems that are high-risk under Art. 6(2) and Annex III |
| Annex I high-risk obligations move to **2 Aug 2028** | CONFIRMED | New Art. 113(c)(ii): 2 August 2028 for Art. 6(1) and Annex I |
| "Art. 4 AI literacy … not delayed / unchanged by the omnibus" | **CORRECTED** | Art. 4 was **replaced**, not left as it was. Providers and deployers must now "take measures to support the development of AI literacy". The new text says the obligation "does not require providers or deployers to guarantee any specific level of AI literacy". The duty is softened, not deferred. |
| "Art. 50 transparency duties not delayed" | **PARTLY CORRECTED** | Art. 50 still applies from 2 Aug 2026. However, new **Art. 111(4)** gives providers of generative systems placed on the market **before 2 Aug 2026** until **2 Dec 2026** to comply with the Art. 50(2) marking (watermarking) obligation. |
| "Prohibited practices (Art. 5) not delayed" | CONFIRMED, with an addition | The existing Art. 5 prohibitions still apply from 2 Feb 2025. The omnibus **adds new prohibitions**: Art. 5(1)(ba) covers non-consensual intimate deepfakes of identifiable persons, and Art. 5(1)(bb) covers AI that generates CSAM. Both, with Art. 5(1a)/(1b), **apply from 2 Dec 2026** (new Art. 113(a)). |
| GPAI obligations (Art. 53 ff.) not delayed | CONFIRMED | No date change to Chapter V |
| Not stated in B | addition | New Art. 113(d): Arts. 102–110 (the amendments to other acts) apply from 27 July 2026. Amended Art. 111(2): high-risk systems used by public authorities must comply by 2 Aug 2030. The Commission must publish guidelines by 1 Aug 2027 on the interplay with Annex I Section A legislation, and by 2 Sep 2027 on the post-market-monitoring template. |
| C §9 "high-risk-system obligations from Aug 2026/2027" | **CORRECTED** | 2 Dec 2027 (Annex III) and 2 Aug 2028 (Annex I) |

What was **not** deferred: Chapters I–II (apart from the new prohibitions), GPAI (Chapter V), governance and penalties, and the general application of Art. 50 on 2 Aug 2026. The one exception is the Art. 50(2) grace period to 2 Dec 2026 for systems already on the market.

## 2. The data and cyber "Digital Omnibus", COM(2025) 837, procedure 2025/0360(COD)

Sources:
- OEIL: <https://oeil.europarl.europa.eu/oeil/en/procedure-file?reference=2025/0360(COD)>
- Proposal text: <https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:52025PC0837>
- Consilium simplification page: <https://www.consilium.europa.eu/en/policies/simplification/>

| Claim | Verdict | Primary-source value |
|---|---|---|
| Not adopted; still in the ordinary legislative procedure | CONFIRMED | OEIL stage: **"Awaiting committee decision"**. ITRE/LIBE joint committee (co-rapporteurs Salla and Kaljurand, appointed 25 Feb 2026). Draft report PE786.818 dated 22 Jun 2026; committee amendments tabled 27 Jul 2026. There has been no committee vote, no plenary mandate and no trilogue. |
| Council has no common position yet | CONFIRMED | Consilium's simplification page (last reviewed 29 Jun 2026) lists no Council mandate on the data proposal; for Omnibus VII it lists only the AI file. Agence Europe reports an Irish Presidency compromise text dated 3 Sep 2026 (secondary source). The Council is therefore still at working-party level. |
| "Data Omnibus … DORA also touched" | CONFIRMED | Besides GDPR, EUDPR, ePrivacy, NIS2, CER and the Data Act, it amends **DORA 2022/2554** and **eIDAS 910/2014** (single-entry-point reporting) and Reg. 2018/1724 |
| GDPR Art. 33: 72h becomes **96h**, and a "high risk" threshold | CONFIRMED (as proposal) | Proposed Art. 33(1): notify "not later than 96 hours" via the ENISA single-entry point, only where a breach is likely to result in a high risk |
| New GDPR **Art. 41a** (implementing acts on pseudonymisation) | CONFIRMED (as proposal) | "The Commission may adopt implementing acts to specify means and criteria to determine whether data resulting from pseudonymisation no longer constitutes personal data for certain entities" |
| New **Art. 88c** (AI development via legitimate interest) | CONFIRMED (as proposal) | |
| Single-click reject and a **six-month** moratorium "in new Art. 88b" | **CORRECTED (article number)** | Both are in proposed **Art. 88a(4)(a) and (c)**; Art. 88a applies 6 months after entry into force. **Art. 88b** is the machine-readable-signal article: controllers must accept automated signals (applies after 24 months, media service providers exempt), and **non-SME web-browser providers** must provide the means (Art. 88b(6), applies after 48 months). |
| Audience-measurement cookie exemption | CONFIRMED (as proposal) | Art. 88a(3)(c): first-party aggregated audience measurement "solely for its own use" |
| NIS2: "simplify/harmonise incident-reporting timelines" (B §3.6) | **CORRECTED** | The proposal does **not** change the 24h/72h/1-month NIS2 timeline. It (a) creates a new **Art. 23a** single-entry point run by ENISA, (b) routes Art. 23(1) notifications through it, (c) adds Art. 23(12), under which a CRA Art. 14(3) report counts as NIS2 reporting, and (d) extends voluntary notification (Art. 30) to use the single-entry point. |
| Data Act: trade-secret refusal in Art. 4(8)/5(11) | CONFIRMED (as proposal) | Refusal is allowed where there is a high risk of disclosure to third-country entities or jurisdictions with weaker protection |
| B §6 "Data Governance Act not touched by the Digital Omnibus" | **CORRECTED** | The proposal **repeals** Reg. 2022/868 (the DGA), together with 2018/1807 (Free Flow of Non-Personal Data), 2019/1024 (Open Data) and 2019/1150 (P2B). Their content is folded into the Data Act. |

## 3. Spain's NIS2 transposition and INFR(2024)0270

Sources:
- Congreso initiative register, XV Legislatura, proyectos de ley 121/000035 to 121/000114, checked one by one: <https://www.congreso.es/es/busqueda-de-iniciativas>
- DSN: <https://www.dsn.gob.es/en/node/24160>
- Commission: <https://digital-strategy.ec.europa.eu/en/news/commission-refers-ireland-spain-france-and-netherlands-court-justice-failing-transpose-rules> (IP/26/1499, <https://ec.europa.eu/commission/presscorner/detail/en/ip_26_1499>)
- BOE RDL 12/2018: <https://www.boe.es/buscar/act.php?id=BOE-A-2018-12257>

| Claim | Verdict | Primary-source value |
|---|---|---|
| Anteproyecto approved by the Council of Ministers on 14 Jan 2025 | CONFIRMED | First reading (primera vuelta) |
| "…remains in **parliamentary processing**" (B §3.2) | **CORRECTED** | The bill has **not been sent to the Cortes**. No "Proyecto de Ley de Coordinación y Gobernanza de la Ciberseguridad" appears among the Government bills registered in Congress from Sep 2024 to 16 Sep 2026 (121/000035–121/000114). It is still an **anteproyecto** inside the Government (mandatory reports and Consejo de Estado opinion, then second reading). A BOE resolution of 17 Dec 2025 (BOE-A-2026-511, Puertos del Estado) still calls it "Anteproyecto … (en tramitación)". It is not in the BOE. |
| Infringement: "reasoned opinion 7 May 2025, formal notice/second warning 19 May 2026" | **CORRECTED** | Letter of formal notice on **28 Nov 2024**, reasoned opinion on **7 May 2025**, then **referral to the CJEU** on 8–9 Jul 2026 (with IE, FR and NL). The Commission asks for a **lump sum plus daily penalties**. There was no "19 May 2026" step. |
| Spain's current baseline is "direct effect + Ley 8/2011 PIC + ENS" (B §3.5) | **CORRECTED (incomplete)** | The NIS1 transposition, **Real Decreto-ley 12/2018** (with RD 43/2021 implementing it), is still in force. The BOE consolidated text was last updated on 30 Mar 2022 and is not repealed. It is the operative Spanish baseline for network and information security until the NIS2 law passes. |
| Additional finding | new | Congress has a **Proyecto de Ley Orgánica para el buen uso y la gobernanza de la inteligencia artificial** (121/000096, presented 28 May 2026), which is relevant to the AI Act strand |

## 4. Data Act 2023/2854

Source: <https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32023R2854> (Arts. 25, 29, 30, 50).

| Claim | Verdict | Primary-source value |
|---|---|---|
| Applies from 12 Sep 2025 | CONFIRMED | Art. 50 |
| Art. 3(1) design obligation for products placed on the market after 12 Sep 2026 | CONFIRMED | Art. 50, third paragraph ("placed on the market **after** 12 September 2026") |
| Switching charges end on 12 Jan 2027 | CONFIRMED | Art. 29(1): no switching charges from 12 Jan 2027. Art. 29(2)–(3): from 11 Jan 2024 to 12 Jan 2027 only reduced, cost-based charges are allowed. |
| B §5.2: contracts concluded before 12 Sep 2025 get carve-outs "until 12 January 2027" | **CORRECTED** | This conflates two rules. The pre-12-Sep-2025 transition concerns **Chapter IV** (unfair B2B data-sharing terms). Chapter IV applies from **12 Sep 2027** to contracts concluded on or before 12 Sep 2025 that are of indefinite duration or expire at least 10 years after 11 Jan 2024. The 12 Jan 2027 date belongs only to the **Art. 29 switching-charge** phase-out. |
| 30-day maximum transitional period | CONFIRMED | Art. 25(2)(a): "mandatory maximum transitional period of 30 calendar days" |
| "Functional equivalence (Art. 29)" | **CORRECTED (article)** | Functional equivalence for IaaS is **Art. 30(1)**, not Art. 29 |

## 5. DORA Level-2 acts

| Act | Verdict | Primary-source value |
|---|---|---|
| Classification RTS **2024/1772** | CONFIRMED | Delegated Reg. of 13 Mar 2024, OJ 25.6.2024. <https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32024R1772>. C §1 "Art. 1–8 materiality thresholds" is **CORRECTED**: Arts. 1–7 are the classification criteria, Art. 8 defines a major incident, Art. 9 sets the materiality thresholds and Art. 10 covers significant cyber threats. |
| ICT risk framework RTS **2024/1774** | CONFIRMED | Delegated Reg. of 13 Mar 2024, OJ 25.6.2024. <https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32024R1774>. C §1 article mapping is **CORRECTED**: asset management is **Arts. 4–5** (not 2–4); encryption and key management are **Arts. 6–7** (Arts. 8–10 cover ICT operations, capacity and vulnerability/patch management); logging is **Art. 12** (Art. 11 is data and system security); network security is **Arts. 13–14** (Arts. 15–18 cover project, acquisition, change and physical security). |
| Incident-report content and time-limits RTS **2025/301** | CONFIRMED (the number C flagged as uncertain is right) | Delegated Reg. of 23 Oct 2024, OJ 20.2.2025. <https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32025R0301> |
| Timelines | CONFIRMED, with one precision | Art. 5(1): **initial** notification within 4h of classification as major and no later than 24h after awareness. **Intermediate** report within **72h of submitting the initial notification**. **Final** report within **1 month of the (latest updated) intermediate report**, not one month after the incident. A deadline falling on a weekend or holiday can shift (Art. 5(4)). |
| Templates ITS **2025/302** | CONFIRMED | Implementing Reg. of 23 Oct 2024, OJ 20.2.2025. <https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32025R0302> |
| Register of information ITS **2024/2956** | CONFIRMED | Implementing Reg. of 29 Nov 2024, OJ 2.12.2024. <https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32024R2956> |
| TLPT RTS "adopted mid-2024 per JC 2024-29" | **CORRECTED** | JC 2024-29 was only the ESAs' final draft. The act is **Commission Delegated Regulation (EU) 2025/1190** of 13 Feb 2025, OJ 18.6.2025. <https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32025R1190> |

## 6. EHDS 2025/327

Sources: <https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32025R0327> (Art. 105) and the EUR-Lex document-information page <https://eur-lex.europa.eu/legal-content/EN/ALL/?uri=CELEX:32025R0327>.

| Claim (C §3) | Verdict | Primary-source value |
|---|---|---|
| Regulation of 11 Feb 2025, OJ 5.3.2025 | CONFIRMED | |
| Entered into force on 26 Mar 2025 | **CORRECTED** | EUR-Lex gives the date of entry into force as **25 Mar 2025** (publication plus 20 days) |
| General application from 26 Mar 2027 | CONFIRMED | Art. 105, second paragraph |
| Priority categories and EHR requirements | CONFIRMED, with precision | Arts. 3–15, 23(2)–(6), 25–27 and 47–49 apply from **26 Mar 2029** to priority categories (a)–(c) (patient summaries, ePrescriptions, eDispensations) and to the EHR systems that process them. They apply from **26 Mar 2031** to categories (d)–(f) (medical images, lab results, discharge reports). Chapter III applies to EHR systems put into service under Art. 26(2) from 26 Mar 2031. Chapter IV (secondary use) applies from 26 Mar 2029, with some provisions from 2027, 2031 and 2035. |
| "January 2026 EHR certification milestone" | **CORRECTED (not in the regulation)** | Art. 105 contains no such date. The earliest EHR-system date is 26 Mar 2029. |

## 7. eIDAS 2 (2024/1183)

Sources: <https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32024R1183> (Arts. 5a(1), 5f) and <https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32024R2979>.

| Claim (C §2) | Verdict | Primary-source value |
|---|---|---|
| OJ 30.4.2024, in force 20 May 2024 | CONFIRMED | |
| Member States provide a wallet by 24 Dec 2026 | CONFIRMED (derived) | Art. 5a(1): "within 24 months of the date of entry into force of the implementing acts referred to in [5a(23)] and 5c(6)". The first batch of those acts (e.g. CIR 2024/2979 of 28 Nov 2024, OJ 4.12.2024, in force on the 20th day) entered into force on **24 Dec 2024**, which gives **24 Dec 2026**. |
| Private relying parties must accept about 12 months later (Dec 2027) | CONFIRMED, with precision | Art. 5f(2): **36 months** after those implementing acts, i.e. **24 Dec 2027**. It applies to private relying parties required by law or contract to use strong user authentication (transport, energy, banking, financial services, social security, health, drinking water, postal, digital infrastructure, education, telecoms). **Micro and small enterprises are exempt**, and acceptance is **only upon the user's voluntary request**. |
| VLOPs must accept | CONFIRMED | Art. 5f(3): a VLOP that requires user authentication must accept the wallet on the user's voluntary request, for minimum data. The article sets no separate deadline. |
| Public-sector acceptance | CONFIRMED | Art. 5f(1) |

## 8. ENS (RD 311/2022)

Source: <https://www.boe.es/buscar/act.php?id=BOE-A-2022-7191>.

| Claim (C §5) | Verdict | Primary-source value |
|---|---|---|
| Measure count in Anexo II ("73 vs 75") | **RESOLVED: 73** | Counted from the BOE consolidated text: org.1–4 (4); op.pl.1–5, op.acc.1–6, op.exp.1–10, op.ext.1–4, op.nub.1, op.cont.1–4, op.mon.1–3 (33); mp.if.1–7, mp.per.1–4, mp.eq.1–4, mp.com.1–4, mp.si.1–5, mp.sw.1–2, mp.info.1–6, mp.s.1–4 (36). The total is **73**. |
| Applies to private suppliers of the public sector | CONFIRMED | Art. 2(3): it applies to the information systems of private-sector entities that provide services or solutions to public-sector entities under a contractual relationship, including the obligation to have a security policy (Art. 12). Contract specifications (pliegos) must say so. |
| "Trazabilidad … one of its **six** protected dimensions" | **CORRECTED** | There are **five** dimensions (Anexo I): Confidencialidad, Integridad, Trazabilidad, Autenticidad, Disponibilidad. op.exp.8 "Registro de la actividad" applies to dimension T. |
| "In force since 4 May 2022 (BOE publication)" | **CORRECTED** | Published in BOE no. 106 on **4 May 2022**; **in force from 5 May 2022** |

## 9. LOPDGDD (LO 3/2018)

Sources:
- <https://www.boe.es/buscar/act.php?id=BOE-A-2018-16673> (consolidated text, last updated 27 Dec 2025)
- <https://www.boe.es/buscar/act.php?id=BOE-A-2025-26698>

| Claim | Verdict | Primary-source value |
|---|---|---|
| C §11 "adaptation deadline of 28 December 2026" | **CORRECTED** | No LOPDGDD provision has that deadline. The only 2025–2026 change is **Ley 10/2025, de 26 de diciembre** (customer-service law), whose DF 4 rewrites **Art. 23.1 LOPDGDD**. Art. 23.1 covers advertising-exclusion (Robinson) systems and adds "servicios de preferencia". That change has been **in force since 28 Dec 2025**, with no deadline. The 28 Dec 2026 date is the 12-month deadline in Ley 10/2025's **Disposición transitoria única** for companies to adapt their *customer-service* channels. It is not a data-protection obligation. |
| B §1.5 "no material change" | CONFIRMED (with the Art. 23.1 note above) | |
| C §11 "Título X, Art. 24" (whistleblowing) | minor correction | Art. 24 sits in Título IV, not Título X; Título X is the digital-rights title. Ley 2/2023 now governs internal reporting channels. |

## 10. Cyber Resilience Act (2024/2847)

Sources:
- <https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32024R2847> (Arts. 26, 71)
- <https://digital-strategy.ec.europa.eu/en/library/commission-publishes-new-guidance-support-timely-cyber-resilience-act-implementation>

| Claim | Verdict | Primary-source value |
|---|---|---|
| Reporting obligations (Art. 14) from **11 Sep 2026** | CONFIRMED | Art. 71(2). They now apply (since 11 Sep 2026). |
| Full application **11 Dec 2027** | CONFIRMED | Art. 71(2) |
| Not stated | addition | Chapter IV (notification of conformity assessment bodies, Arts. 35–51) has applied since **11 Jun 2026**. OJ 20.11.2024; in force 10 Dec 2024. |
| Commission guidance on open source and commercial activity | **Published** | **C(2026) 5252**, a Communication with an annex "Commission guidance on the application of the CRA". Published **July 2026** (the page is dated 27 Jul 2026). It covers scope (including **free and open-source software** and remote data processing), substantial modification, support periods, reporting and risk assessment, with 67 examples. It is non-binding and was adopted under Art. 26, which requires guidance on "free and open-source software". Secondary sources say it details when FOSS is monetised (and so placed on the market) and what open-source stewards must do; only the official page was read, not the full annex. |

---

## Other [UNCERTAIN] items in B and C

| File/§ | Claim | Verdict | Source |
|---|---|---|---|
| B §2.2 | AEPD cookie guide aligns (granular, reject as easy as accept) | CONFIRMED | AEPD "Guía sobre el uso de las cookies", latest edition May 2024 (the July 2023 update adapted it to EDPB Guidelines 03/2022). Accept and reject must be at the same level. <https://www.aepd.es/guias/guia-cookies.pdf> |
| B §2.3 | Browser-signal duty falls on browser vendors; sites must honour signals | CONFIRMED (as proposal) | Proposed Art. 88b(1)–(2) and (6), see §2 |
| B §3.2 | CIR 2024/2690 "applies from 18 October 2024" | **CORRECTED** | Published in the OJ on 18.10.2024 with entry into force on the 20th day, so it has **applied since 7 Nov 2024**. <https://eur-lex.europa.eu/legal-content/EN/TXT/HTML/?uri=CELEX:32024R2690> |
| B §5.3 | Art. 4(8)/5(11) trade-secret refusal is a draft | CONFIRMED | See §2 |
| C §7 | PSD3/PSR "final OJ texts expected H1 2026 … effective ~2027" | **CORRECTED** | OEIL 2023/0210(COD) (PSR) shows "Awaiting Council's 1st reading position". ECON approved the agreed text on 5 May 2026, and an EP plenary is forecast for **14 Dec 2026** (early second reading). The PSR is **not published**. With about 18 months of transition, it will apply around **mid-2028** at the earliest. <https://oeil.europarl.europa.eu/oeil/en/procedure-file?reference=2023/0210(COD)> |
| C §8 | ISO/IEC 27701:2019 is current, "no announced revision" | **CORRECTED** | **ISO/IEC 27701:2025 (Edition 2)** is published and is now a standalone PIMS requirements standard. The 2019 edition is superseded. <https://www.iso.org/standard/27701> |
| C §9 | ISO/IEC 42001 Annex A: 38 controls in 9 areas | UNVERIFIABLE (paywalled) | Consistent with ISO OBP previews; the standard text was not read |
| C §12 | CER: Spain has not transposed; only an anteproyecto under consultation | **CORRECTED (partly)** | Not transposed, confirmed. But the **Proyecto de Ley de protección y resiliencia de las entidades críticas (121/000088)** was sent to Congress on **18 Mar 2026** and published in BOCG A-88-1 on 27 Mar 2026. It is in the Comisión de Interior amendment phase, with the deadline extended to 30 Sep 2026 and full legislative competence delegated to the committee. The Commission referred Spain (with BG, FR, LU, NL, PL, SE) to the CJEU (IP/26/910). Congress: <https://www.congreso.es/es/busqueda-de-iniciativas?p_p_id=iniciativas&p_p_lifecycle=0&p_p_state=normal&p_p_mode=view&_iniciativas_mode=mostrarDetalle&_iniciativas_legislatura=XV&_iniciativas_id=121%2F000088>. Commission: <https://ec.europa.eu/commission/presscorner/detail/da/ip_26_910> |
| C §1 | DORA applies from 17 Jan 2025 | CONFIRMED | Art. 64 DORA |
