# A. EU law that applies to Encina itself (the library, not the apps built with it)

Research date: 2026-09-23. Scope: Encina as a software product and its maintainer. This is research, not legal advice. Where a point rests on non-binding guidance or on my own reading, it says so.

## 0. Facts about Encina used in this analysis (verified in the repo)

| Fact | Evidence |
|---|---|
| Licence: MIT, copyright "David Lozano Rivada" (a natural person) | `LICENSE` |
| No paid edition, no paid support offer, no donation or sponsor link (`.github/FUNDING.yml` does not exist), no telemetry | `README.md` (licence section only), `.github/` |
| Source code is public on GitHub (`dlrivada/Encina`) | repo |
| Releases: pre-release tags v0.10.0 to v0.13.0. CI packs **only `src/Encina/Encina.csproj`** and pushes it to **GitHub Packages** on `v*` tags. nuget.org publishing is still "Phase 6 – Pending" | `.github/workflows/ci-full.yml` (Publish to GitHub Packages step), `README.md` roadmap row "Phase 6" |
| SBOM: `sbom.yml` runs Syft on tags. It covers **only the core `Encina` package**, writes **SPDX JSON** and uploads it **only as a workflow artifact** (not as a release asset) | `.github/workflows/sbom.yml` |
| `SECURITY.md` exists: GitHub Private Vulnerability Reporting, coordinated disclosure, GHSA publication. It gives **no support period and no response times**, and it lists **DoS and third-party dependency issues as out of scope** | `SECURITY.md` |
| Package signing, build-provenance attestation: **none found** | workflows grep |
| Cryptography: Encina calls .NET BCL primitives (AES-GCM, HMAC-SHA*, SHA-256). It implements no primitives of its own | `src/Encina.Security.Encryption/Algorithms/AesGcmFieldEncryptor.cs`, `src/Encina.Security.AntiTampering/HMAC/HMACSigner.cs`, `src/Encina.Compliance.Anonymization/*`, `src/Encina.Marten.GDPR/...` |

---

## 1. Cyber Resilience Act (CRA), Regulation (EU) 2024/2847

Sources:
- Legal text: EUR-Lex CELEX 32024R2847, <https://eur-lex.europa.eu/eli/reg/2024/2847/oj> (retrieved through the Publications Office Cellar).
- Commission guidance **C(2026) 5252, 27 July 2026** (Annex "Commission guidance on the application of the CRA", 84 pp.): <https://digital-strategy.ec.europa.eu/en/library/commission-publishes-new-guidance-support-timely-cyber-resilience-act-implementation>. Annex PDF: <https://ec.europa.eu/newsroom/dae/redirection/document/131456>. The guidance says of itself that it is **not binding** (point 8). Only the CJEU can interpret the CRA authoritatively.

### 1.1 Dates of application (Art. 71(2), Art. 69)

| Date | What applies | Source |
|---|---|---|
| 10 Dec 2024 | Entry into force | Art. 71(1) |
| **11 Jun 2026** | Chapter IV (Arts. 35–51, notification of conformity assessment bodies) | Art. 71(2) |
| **11 Sep 2026** | **Art. 14 reporting** (actively exploited vulnerabilities and severe incidents) for manufacturers. It covers **all in-scope products, including those placed on the market before 11 Dec 2027** | Art. 71(2), Art. 69(3); guidance point 210 |
| 11 Sep 2026 | ENISA Single Reporting Platform (SRP) went live | <https://www.enisa.europa.eu/news/the-cra-single-reporting-platform-is-launched> |
| **11 Dec 2027** | Everything else: essential requirements (Annex I), Art. 13 incl. **13(5) due diligence** and **13(6) upstream reporting**, SBOM, support period, CE marking, **Art. 24 stewards** | Art. 71(2) |
| after 11 Dec 2027 | A product placed on the market before 11 Dec 2027 falls under the CRA's requirements only if it is **substantially modified** after that date (Art. 14 excepted) | Art. 69(2)–(3) |

Implementing and delegated acts and guidance published so far (per <https://digital-strategy.ec.europa.eu/en/factpages/cyber-resilience-act-implementation>):
- Implementing Regulation (EU) 2025/2392 (28 Nov 2025): technical descriptions of important and critical products (Annex III/IV).
- Delegated act on CSIRTs withholding notifications (adopted Dec 2025; the page gives the reference "(EU) 2026/0881". I have **not verified** that number in the OJ).
- FAQ on CRA implementation (3 Dec 2025), referenced in guidance point 5.
- Guidance C(2026) 5252 (27 Jul 2026).
- Still pending according to the same page and the secondary sources I found: the EUCC presumption-of-conformity delegated act (Q4 2026); harmonised standards (first deliverables Q3 2026, more by 30 Oct 2027); the **Art. 13(24) implementing act on SBOM format (not adopted)**; the **Art. 25 delegated act on voluntary FOSS security attestation (not adopted; ORC WG and Eclipse are preparing proposals)**. Uncertainty: I did not search the OJ exhaustively for adoptions made in the last few weeks.

### 1.2 Is Encina a "product with digital elements" placed on the market?

**It is software, so it can be a product with digital elements.** Art. 3(1) covers "software or hardware components being placed on the market separately". The CRA definition of software also covers source code (Art. 3(4); guidance points 17–22).

**But the CRA applies only to products that are *made available on the market*, meaning supplied "in the course of a commercial activity"** (Art. 3(22), recital 15; guidance point 10). The FOSS rules:

- **Recital 18**: "the provision of products with digital elements qualifying as free and open-source software that are not monetised by their manufacturers should not be considered to be a commercial activity". Components "intended for integration by other manufacturers" are made available on the market "only if the component is monetised by its original manufacturer". Financial support from manufacturers, contributions from manufacturers and "the mere presence of regular releases" do not in themselves make the activity commercial.
- **Recital 15** lists the commercial-activity signals: a price; paid technical support "where this does not serve only the recuperation of actual costs"; monetising other services through the software; requiring personal data as a condition of use (other than for security, compatibility or interoperability); "accepting donations exceeding the costs". "Accepting donations without the intention of making a profit should not be considered to be a commercial activity."
- **Guidance C(2026) 5252, Section 3**:
  - Point 23: sharing FOSS code "on publicly accessible repositories is generally not considered to be placing that code on the EU market". Unfinished code shared during development is not placed on the market either.
  - Point 44–46: to count as FOSS under Art. 3(48) the licence must grant all the rights **and** the source must be *openly* shared. MIT plus a public GitHub repository meets both conditions.
  - Point 49: responsibility lies with the "maintainers" who "publish and control" the FOSS. Contributors are out of scope (Example 13).
  - Point 53: when a **natural person** publishes a free/community version, that version "is not within the scope of the CRA".
  - Points 55–58, Example 18: optional, separately sold consultancy or training does **not** place the FOSS on the market. For a **natural person**, even assistance bundled with access is not commercial if the price only recovers actual costs, and those costs "includ[e] the person's reasonable living expenses".
  - Points 60–62, Examples 20–22: donations are fine, **even above costs**. They become a price if releases, security updates or binaries are **gated on donating**.
  - Points 63–65, Example 23: a third party (for example the maintainer's employer or a customer) funding development does not change the status.
  - Point 67: FOSS intended for integration is not placed on the market "unless it is also monetised by the person that publishes it".
  - Point 87: "Maintainers of FOSS components that are not placed on the market do not bear obligations in relation to other entities that may integrate such components". Whether the CRA applies "depends solely on whether the natural or legal person that publishes it places it on the market".
  - **Example 34 matches Encina's situation almost exactly**: "An individual developer publishes a FOSS library on a public package repository ... and actively maintains it ... adds a link to collect donations. A manufacturer ... integrates it ... The individual developer and the package repository do not have any obligations under the CRA. The manufacturer that integrates the library needs to exercise due diligence in accordance with Article 13(5)." (Similar: Example 27.)

**Conclusion (binding-law reading, supported by guidance): as long as the current model holds, Encina is not placed on the market and its maintainer is not a CRA "manufacturer".** The current model is an MIT licence, public source, a natural-person maintainer, no monetisation of the software itself, and no personal-data condition. None of Art. 13, Art. 14, Annex I, the SBOM duty, the support-period duty, conformity assessment or CE marking binds Encina. Confidence: high. The remaining risk is that the Blue Guide test is "case-by-case" (guidance point 40) and the guidance is non-binding.

### 1.3 Open-source software steward regime (Art. 3(14), Art. 24)

- Art. 3(14): a steward is "**a legal person**, other than a manufacturer, that has the purpose or objective of systematically providing support on a sustained basis for the development of specific products with digital elements, qualifying as free and open-source software and intended for commercial activities, and that ensures the viability of those products".
- **Encina's maintainer is a natural person, so he cannot be a steward.** Not binding on Encina today.
- If a legal person ever takes Encina over (for example a company or foundation created by the maintainer that sustains it for commercial use), Art. 24 would apply from **11 Dec 2027**:
  - (1) a documented, verifiable cybersecurity policy covering vulnerability handling that fosters voluntary Art. 15 reporting;
  - (2) cooperation with market surveillance authorities on request;
  - (3) Art. 14(1) reporting "to the extent that they are involved in the development". Art. 14(3)/(8) apply if the steward's own infrastructure is hit.
  - Stewards are **exempt from administrative fines** (Art. 64(10)(b)).
  - Guidance points 69–84 and Examples 25, 26, 29 and 31 show that a *company* publishing an unmonetised library that it maintains is a steward.
  - Timing uncertainty: ENISA's SRP launch note says steward reporting starts on 11 Dec 2027. The guidance (point 216) discusses steward reporting without a separate date. Art. 24 falls under the general 11 Dec 2027 date.

### 1.4 What would turn Encina into a CRA product (the triggers to avoid, or to plan for)

| Trigger | Effect | Source |
|---|---|---|
| A price for the software, or a paid "enterprise/LTS" edition with builds, fixes or features that only payers get | That edition is placed on the market and Encina becomes its manufacturer. **Art. 14 reporting applies at once (since 11 Sep 2026)**; the full CRA applies from 11 Dec 2027. A natural person's free community edition stays out | Guidance points 51–53, 57, Example 17 |
| Releases, binaries or security updates **gated on donations or sponsorship** | Treated as a price, so placed on the market | Guidance point 62, Examples 21–22 |
| Paid support that is a condition of access or maintenance, above cost recovery (for a natural person, cost includes reasonable living expenses) | Commercial | Recital 15; guidance points 56–58 |
| Mandatory telemetry or registration that processes personal data for purposes other than security, compatibility or interoperability | Commercial | Recital 15; guidance point 54, Example 16 |
| The maintainer ships his **own commercial product** built on Encina | He is the manufacturer of that product and must apply Art. 13(5) to Encina like anyone else | Art. 13(5); guidance point 88 |
| Encina is transferred to a legal person that sustains it for commercial use | Steward (Art. 24) from 11 Dec 2027 | Art. 3(14), 24 |
| Encina becomes a manufacturer's product **and** a package's core function is an Annex III category (for example identity or access management) | Possibly an "important product" (class I/II). Needs checking against Implementing Reg. 2025/2392. FOSS can use lighter procedures if its technical documentation is public (Art. 32(5)) | Art. 7, Art. 32(5). **Uncertain**, only relevant if a trigger above occurs |

Neutral (do **not** trigger): GitHub Sponsors or donations with no gating; the maintainer's employer or a client funding features; company employees contributing; regular releases; publishing on nuget.org or GitHub Packages; pre-1.0 status.

### 1.5 What the CRA requires of the **apps that integrate Encina** (binding on them, not on Encina)

Only when the app is itself a product with digital elements placed on the EU market. Software downloaded and executed on the user's side is in scope. **A purely server-side web app or SaaS reached through a browser is not a product with digital elements** unless it is the remote data processing of one (guidance points 20–21, Examples 5–6, Section 8). Many Encina users build back-end services, so part of the user base may be outside the CRA altogether. Self-hosted or on-prem distributed software, desktop or mobile clients and devices are in scope.

For in-scope apps:
- **Art. 13(5)** (from 11 Dec 2027): "manufacturers shall exercise due diligence when integrating components sourced from third parties so that those components do not compromise the cybersecurity of the product ..., including when integrating components of free and open-source software that have not been made available on the market".
  - Recital 34 lists typical checks: whether the component receives **regular security updates (its security update history)**; whether it is free of vulnerabilities in the **EUVD** or other public databases; additional security tests; CE marking where one exists (Encina has none).
  - Guidance point 171: evidence "may consist of documentation obtained from the component manufacturer, such as technical specifications, security documentation". Where the product relies on a component for **cryptographic functions**, the manufacturer must identify those needs and verify that the component meets them.
- **Art. 13(6)**: report vulnerabilities found in Encina **to Encina's maintainer** and share any fix.
  - Guidance point 223: follow the project's "security policies, coordinated vulnerability disclosure processes or designated channels". No duty to report if the maintainer already knows.
  - Point 225: no duty if the component is unmaintained.
  - Point 227: fixes must be shared in a licence-compatible way.
- **Art. 14** (from 11 Sep 2026, including products placed on the market before 2027): if a vulnerability in Encina is **actively exploited in their product**, they notify the CSIRT and ENISA through the SRP within 24 h, 72 h, then 14 days. A vulnerable but unreachable or unexploited Encina version is not reportable, but it must still be handled and reported upstream (guidance point 218).
- **Annex I Part II(1)**: their SBOM must cover "at the very least the top-level dependencies", so Encina packages will appear in it.
- **Art. 13(8)**: their support period (≥5 years unless the product's expected use is shorter). Guidance footnote 17 lists "the support periods of integrated components that provide core functions" as a factor. **Encina's own support policy therefore matters to its integrators.**

### 1.6 Voluntary routes that are open to Encina
- **Art. 15**: "natural or legal persons may notify any vulnerability ... on a voluntary basis" to a CSIRT or ENISA. For Spain, INCIBE-CERT handles private entities and citizens.
- **Art. 25**: future voluntary FOSS security attestation programmes. No delegated act yet. Worth watching, because an attestation would lighten integrators' Art. 13(5) work.

---

## 2. New Product Liability Directive (PLD), Directive (EU) 2024/2853

Source: CELEX 32024L2853, <https://eur-lex.europa.eu/eli/dir/2024/2853/oj>.

- **Software is a product**: Art. 4(1) ("it includes ... software"). Recital 13 says this holds "irrespective of ... software-as-a-service". Unlike the CRA, the PLD also covers SaaS. "The mere source code of software" is information, not a product (recital 13).
- **FOSS exclusion, Art. 2(2)**: "This Directive does not apply to free and open-source software that is developed or supplied outside the course of a commercial activity." Recital 14: "Providing such software on open repositories should not be considered as making it available on the market, unless that occurs in the course of a commercial activity". Supply for a price, or for personal data used other than for security, compatibility or interoperability, is commercial.
- **Recital 15**: when non-commercial FOSS is integrated into a commercial product, "it should be possible to hold that manufacturer liable ... but not the manufacturer of the software".
- **Application**: Art. 2(1) covers products placed on the market or put into service **after 9 Dec 2026**. Art. 21 repeals Directive 85/374/EEC from 9 Dec 2026, but the old directive still governs earlier products. **Transposition deadline 9 Dec 2026** (Art. 22).
- Relevant to integrators:
  - defectiveness takes into account "safety-relevant cybersecurity requirements" (Art. 7(2)(f));
  - the "later-defect" defence is unavailable for software, software updates or "a lack of software updates ... necessary to maintain safety" within the manufacturer's control (Art. 11(2));
  - compensable damage includes "destruction or corruption of data that are not used for professional purposes" (Art. 6(1)(c)); property used exclusively for professional purposes is excluded (Art. 6(1)(b)(iii));
  - a manufacturer that integrates software cannot seek recourse against a micro or small software-component manufacturer where the contract waives recourse (Art. 12(2)).
- **Spain**: I found **no transposing act in the BOE** as of 2026-09-23. The BOE entry for the directive lists no national measure, and searches found only doctrinal proposals (e.g. InDret, IDIBE), no *anteproyecto*. The current regime is the TRLGDCU (RDL 1/2007), Book III, arts. 128–149, which implements 85/374. **Uncertainty**: a bill may exist that the searches missed. Spain appears likely to miss the 9 Dec 2026 deadline. The directive's rules will still govern products placed on the market after that date once transposed, and national courts must interpret existing law consistently with it in the meantime.
- **Guidance C(2026) 5252, footnote 9** notes that computer code "itself" is not a product under the PLD (it cites recital 13). Encina ships compiled binaries (nupkg), so the operative exclusion for Encina is **Art. 2(2)**, not recital 13.

**Conclusion: not binding on Encina** while it stays non-commercial FOSS. Binding on apps that integrate Encina and are placed on the market after 9 Dec 2026 (including SaaS). They carry liability for damage caused by a defective Encina component (Art. 8(1), last subparagraph).

Residual national-law point (**my reading, uncertain**): outside the PLD, general Spanish fault-based liability (Código Civil art. 1902) and the rule that liability for *dolo* cannot be waived in advance (art. 1102 CC) mean that the MIT "AS IS" disclaimer is not an absolute shield in Spain against gross negligence or wilful misconduct. In practice the risk to a free-library maintainer is low. It is not specific to 2026.

---

## 3. Other acts that could bind a library maintainer directly

| Act | Applies to Encina? | Reason / source |
|---|---|---|
| **Dual-Use Regulation (EU) 2021/821**, Annex I, Cat. 5 Part 2 (consolidated 15.11.2025, CELEX 02021R0821-20251115) | **No licence needed for the public open-source code** | *General Software Note* (b): the lists "do not control 'software' which is ... 'In the public domain'". The GSN's carve-out for Cat. 5 Part 2 covers only entries **a** and **c**, **not b**. Definition: "'In the public domain' ... means 'technology' or 'software' which has been made available without restrictions upon its further dissemination (copyright restrictions do not remove ... from being 'in the public domain')". MIT on public GitHub/NuGet meets this. Encina also only calls standard BCL primitives. Spanish implementation: RD 679/2014 (foreign trade control of defence and dual-use material). **Caveat**: a closed or commercial fork, or private builds delivered to customers, would need a Cat. 5 Part 2 / Cryptography Note (Note 3) analysis. Apps that embed Encina's encryption and are sold need their own classification. Outside EU law: GitHub is US-hosted; under US EAR, publicly available open-source encryption source code using standard cryptography is generally not subject to notification (15 CFR 734.3(b)(3), 742.15(b)). **Not verified in this research, noted for completeness.** |
| **Cybersecurity Act (EU) 2019/881** and EUCC (Implementing Reg. (EU) 2024/482) | No, voluntary | Certification is voluntary. A future delegated act would give EUCC presumption of conformity under the CRA (Q4 2026, per the Commission implementation page). |
| **European Accessibility Act, Directive (EU) 2019/882** (Spain: Ley 11/2023) | No | Art. 2 lists specific products (consumer computers and OS, self-service terminals, consumer terminals, e-readers) and consumer services (e-commerce, banking, e-books, transport, etc.) from 28 Jun 2025. A developer library is none of these. Apps built with Encina that provide a listed consumer service may be bound (for example e-commerce). |
| **Digital Content Directive (EU) 2019/770** (Spain: TRLGDCU Book II, Title VIII) | No | It binds *traders* supplying *consumers*. Art. 3(5)(f) excludes FOSS supplied without a price where personal data are processed only for security, compatibility or interoperability. |
| **AI Act, Regulation (EU) 2024/1689** | No | Encina (including `Encina.Compliance.AIAct`) is not an AI system or GPAI model. Art. 2(12) also exempts FOSS AI systems unless they are high-risk, prohibited or covered by Art. 50. |
| **NIS2, Directive (EU) 2022/2555** | No | It applies to medium or large entities in Annex I/II sectors, not to an individual maintainer. |
| **GDPR** | Not to the product | Encina collects no data (no telemetry). It would matter if telemetry were added, which would also be a CRA commercial-activity signal (recital 15). |
| **Spanish CRA implementation** (market surveillance authority, penalties under Art. 64) | Only relevant if a trigger in 1.4 occurs | I did **not** find a Spanish designation or penalty law in the BOE. INCIBE-CERT is the CSIRT for private entities (see <https://www.incibe.es>). **Uncertain.** |

---

## 4. Good practice: what a responsible pre-1.0 framework should do anyway so that integrators can meet Art. 13(5), 13(6), 13(8) and Annex I Part II

These are **not legal obligations for Encina**. Each item maps to what integrators must show or do.

| # | Action | Why (legal hook for the integrator) | Encina today |
|---|---|---|---|
| 1 | **Per-package SBOM** (CycloneDX 1.6 or SPDX 2.3/3.0 JSON) for **every** published nupkg, attached to the GitHub Release (optionally also inside the nupkg). Include licence, PURL and hashes | Annex I Part II(1) (their SBOM, top-level deps at least); recital 34; guidance point 171 (component documentation as evidence) | SPDX for **core only**, workflow artifact only, not attached to the release |
| 2 | **VEX** statements (CycloneDX VEX or OpenVEX) when a dependency CVE is not exploitable through Encina | Guidance point 218: unexploitable or unreachable vulnerabilities are not reportable. VEX lets integrators show this | None |
| 3 | **SECURITY.md upgrade**: (a) a **support policy** stating which versions get security fixes and until when (after 1.0, e.g. latest minor of the current major, previous major for N months, stated as month and year); (b) best-effort **response targets**; (c) bring **DoS/ReDoS/resource-exhaustion in Encina code** into scope; (d) put **vulnerable versions of Encina's own dependencies** in scope for bumping (upstream bugs still go upstream); (e) state how integrators should send reports and fixes under Art. 13(6), with fixes under MIT | Art. 13(6) and guidance points 223 and 227 (integrators must use the project's CVD channel, fixes licence-compatible); Art. 13(8) and guidance footnote 17 (component support period is a factor) | PVR and CVD exist. No support period, no targets. DoS and dependencies out of scope |
| 4 | **Advisories**: publish every fix as a **GitHub Security Advisory with ecosystem "nuget"** and request a **CVE** through GitHub (a CNA). The GitHub Advisory Database feeds **NuGetAudit** (`dotnet restore` warnings), so integrators are alerted automatically. CVEs flow into ENISA's **EUVD** | Recital 34 (checks against EUVD and public databases); Annex I Part II(4) (disclose fixed vulnerabilities, which integrators must pass on) | GHSA promised in SECURITY.md; none published yet |
| 5 | **Security-only patch releases**, kept separate from feature releases where feasible, with a visible changelog **security history** | Annex I Part II(2) (security updates separate from functionality updates where feasible); recital 34 ("security updates history") | Not defined |
| 6 | **Provenance and integrity**: publish to **nuget.org** with **Trusted Publishing** (OIDC, no long-lived key); reserve the **package ID prefix**; nuget.org **repository signature** (automatic); **GitHub artifact attestations** (`actions/attest-build-provenance`, SLSA build provenance) for each nupkg and SBOM; `ContinuousIntegrationBuild`, deterministic builds, **SourceLink**, symbol packages. Author signing is optional (needs a paid code-signing certificate) | Annex I Part I(2)(f) integrity (integrators must protect their product's integrity, including components); recital 34 | GitHub Packages only; no signing or attestation |
| 7 | **Secure-by-default configuration** in every package: encryption on with current algorithms; no secrets in logs or `ToString()` (already a project rule); ABAC/authorisation **fail closed**; TLS required in transports; bounded inputs and timeouts | Annex I Part I(2)(b) (secure by default) and (e) (state-of-the-art encryption), which fall on integrators | Partly present (secret-redaction rule in CLAUDE.md) |
| 8 | **Security documentation for integrators** ("Security considerations" per package): threat model, crypto algorithms used (BCL AES-GCM, HMAC-SHA*; no custom primitives), key-management expectations, hardening guidance | Guidance point 171 (crypto needs verified against component documentation); also helps integrators with export classification | Scattered |
| 9 | Short **CRA/PLD status statement** in SECURITY.md or README: "Encina is FOSS (MIT), published by a natural person, not monetised; it is not placed on the market under Reg. (EU) 2024/2847 (see Commission guidance C(2026) 5252, Example 34) nor under Dir. (EU) 2024/2853 Art. 2(2). Integrators remain responsible under CRA Art. 13(5)/(6). Here are our SBOM, advisories and support policy." | Reduces integrator questionnaires; states the legal position explicitly | None |
| 10 | **Guard the non-commercial status**: never gate releases, binaries or security fixes behind payment or donations; keep any paid services optional and separate; no mandatory telemetry | Recital 15; guidance points 56–62 | Compliant today |
| 11 | Keep and extend existing hygiene: **CodeQL, Dependabot, secret scanning, branch protection** (already in place); add the **OpenSSF Scorecard** action and the **OpenSSF Best Practices** badge as external evidence | Recital 34 ("additional security tests"); guidance point 171 | CodeQL, Dependabot and secret scanning present |
| 12 | Optional: voluntarily report actively exploited Encina vulnerabilities to INCIBE-CERT or ENISA (Art. 15); follow Art. 25 attestation work (ORC WG / Eclipse) | Art. 15, 25 | n/a |

---

## 5. Bottom line

| Obligation | Binding on Encina / maintainer | Binding on apps that use Encina | Good practice for Encina |
|---|---|---|---|
| CRA Art. 14 reporting (since **11 Sep 2026**) | **No** (not placed on the market) | **Yes**, if the app is an in-scope product (including products placed before 2027) | Voluntary Art. 15; a solid CVD channel so integrators learn quickly |
| CRA Art. 13(5) due diligence, 13(6) upstream reporting, SBOM, support period, CE (from **11 Dec 2027**) | **No** | **Yes** (in-scope products) | SBOM, advisories, support policy, provenance, security docs (section 4) |
| CRA Art. 24 steward (from **11 Dec 2027**) | **No** (natural person). Would apply if a legal person took Encina over | n/a | Plan for it if Encina is ever moved into a company or foundation |
| PLD (products placed after **9 Dec 2026**; Spain not yet transposed) | **No** (Art. 2(2)) | **Yes**, including SaaS | Keep non-commercial; the MIT disclaimer already exists |
| Dual-use export control | **No** for the public source ("in the public domain", GSN b) | Possibly (classification of their own crypto products) | Document the crypto used |
| Cybersecurity Act, accessibility, AI Act, NIS2, Digital Content Dir. | **No** | Some (EAA for listed consumer services, NIS2 for entities, etc.) | n/a |

Uncertainties:
1. The guidance is non-binding and the test is case-by-case.
2. Whether Spain has a PLD or CRA-penalty bill in progress that the searches did not surface.
3. Exact references of recent delegated acts (2026/0881 not checked in the OJ).
4. Whether steward reporting starts on 11 Sep 2026 or 11 Dec 2027 (irrelevant to Encina today).
5. The US EAR point was not verified.

## Sources
- CRA text: <https://eur-lex.europa.eu/eli/reg/2024/2847/oj> (Arts. 3(1), 3(14), 3(22), 3(48), 13(5), 13(6), 13(8), 13(24), 14, 15, 24, 25, 64(10), 69, 71; recitals 15, 17, 18, 19, 34; Annex I Part II)
- Commission guidance C(2026) 5252 (27 Jul 2026): <https://digital-strategy.ec.europa.eu/en/library/commission-publishes-new-guidance-support-timely-cyber-resilience-act-implementation>; Annex PDF <https://ec.europa.eu/newsroom/dae/redirection/document/131456> (points 10–24, 40–89, 125–131, 167–173, 209–229; Examples 13, 17–23, 25–34; footnote 9)
- CRA implementation timeline: <https://digital-strategy.ec.europa.eu/en/factpages/cyber-resilience-act-implementation>
- CRA and open source (Commission): <https://digital-strategy.ec.europa.eu/en/policies/cra-open-source>
- ENISA SRP launch: <https://www.enisa.europa.eu/news/the-cra-single-reporting-platform-is-launched>
- PLD text: <https://eur-lex.europa.eu/eli/dir/2024/2853/oj> (Arts. 2, 4, 6, 7, 8, 11(2), 12(2), 21, 22; recitals 13–15)
- BOE entry for the PLD (no national measure listed): <https://www.boe.es/buscar/doc.php?id=DOUE-L-2024-81701>
- Dual-Use Regulation, consolidated 15.11.2025: <https://eur-lex.europa.eu/eli/reg/2021/821/2025-11-15/eng> (Annex I: General Software Note, Cat. 5 Part 2 Note 3, definition "In the public domain")
- European Accessibility Act: <https://eur-lex.europa.eu/eli/dir/2019/882/oj> (Art. 2)
- Art. 25 attestation work: <https://github.com/orcwg/cra-attestations>, <https://orcwg.org/blog/attestations-2025/>
- NuGet Trusted Publishing: <https://devblogs.microsoft.com/dotnet/enhanced-security-is-here-with-the-new-trust-publishing-on-nuget-org/>
