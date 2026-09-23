# SPEC-002 research notes

These are the research notes of 2026-09-23 behind [SPEC-002 — EU Regulatory Readiness](../../SPEC-002-eu-regulatory-readiness.md). They are kept for traceability of the *Source:* entries of the specification.

- **They are research notes, not normative.** The specification states the requirements; the notes explain where they came from. Nothing in them binds Encina or its users, and none of it is legal advice.
- **Note E prevails over notes B and C.** E fact-checked B and C against primary sources; where they disagree, E is right, and SPEC-002 §3 already carries the corrected facts.
- Legal facts carry the verification flags of SPEC-002 §3.1: [V] primary source, [S] secondary source, [K] background knowledge, [I] inference, [U] not verified.
- **The reference application is private.** SPEC-002 uses a private application for a small Spanish psychology practice as its acceptance case. The analyses of that application's code, design and integrations are not published; where the specification relies on them, its *Source:* entry says "reference-application analysis (private)". The notes below describe only the generic scenario and the law that applies to it.
- The notes are excluded from the link check (`.github/lychee.toml`): they cite many hosts that refuse automated checks, and they are a snapshot, not maintained documentation.

| Note | Subject |
|---|---|
| [A](A-encina-itself.md) | Encina itself: CRA, PLD, export control |
| [B](B-horizontal.md) | EU horizontal acts |
| [C](C-sector-national.md) | Sector acts and Spanish national law |
| [D](D-encina-inventory.md) | Encina inventory: packages, issues, milestones |
| [E](E-factcheck.md) | Fact-check of B and C against primary sources |
| [F](F-health-practice.md) | Legal profile of a Spanish psychology practice |
| [H](H-integrations-law.md) | Integrations law: invoicing and Verifactu, payments, patient messaging, calendar and mail vendors |

Letter G is not used, and note H replaces an earlier note of the same letter, so that the *Source:* identifiers of SPEC-002 (for example H-R31) stay stable.
