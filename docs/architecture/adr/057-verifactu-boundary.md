# ADR-057: The Verifactu Boundary: Regulation-Neutral Primitives Only (Accepted)

## Status

**Accepted** - 2026-10-27, issue [#1226](https://github.com/dlrivada/Encina/issues/1226). Options B and C were excluded by [SPEC-002](../../specifications/SPEC-002-eu-regulatory-readiness.md) DEC-002 (a) on 2026-09-24; the maintainer decided the remaining points on 2026-10-27.

**This ADR is not legal advice.** It records an engineering boundary and the reasoning behind it, as the repository's own research notes describe it.

## Context

Verifactu is the Spanish tax agency's (AEAT) invoicing-record regime (RD 1007/2023, Orden HAC/1177/2024, RDL 15/2025). Per the SPEC-002 regulatory table (section 3), a library implementing chaining, QR or submission could make its publisher a component "fabricante" under the AEAT developer FAQ, with a producer sanction of 150,000 euros per year and system type under LGT art. 201 bis (source quality [S] in the table, not verified against the BOE text).

SPEC-002 DEC-002 decided on 2026-09-24 that public Encina ships no Verifactu code and offers regulation-neutral primitives only; the Verifactu module belongs to the application, whose producer signs the declaración responsable and carries the producer liability. SPEC-002 INV-004 forbids code whose purpose is a single regulation that would make the maintainer a Verifactu producer, unless a DEC accepts that role, and none does. DEC-002 also requires an ADR that records which primitives are regulation-neutral, before any sequence or chain primitive ships (SPEC-002 section 11.2, table row 29; tracked by P-24 in section 13.2).

Three of those primitives are planned and not yet in `src/`: the gapless sequence (P-36, `ISequenceGenerator` per REQ-045), the persistent chained log (P-38, REQ-047) and outbox partitions with pacing (#469, REQ-042). The order holds: this ADR is P-24, milestone v0.14.0, ahead of P-38 and P-36.

## Decision

Public Encina ships no Verifactu code. It ships the primitives listed below, each regulation-neutral in the sense defined below. Options B (a separate Verifactu package or repository) and C (Verifactu inside Encina) stay excluded: both make the maintainer a producer, which INV-004 forbids.

### Settled points

These points follow from a rule and were not open choices:

- **Public names (D10)**: this ADR fixes capabilities and a naming rule (generic, domain-free names; no regulation-named type) and quotes only `ISequenceGenerator` from REQ-045. The implementation plans of P-36 and P-38 fix the other names.
- **Legal claim strength (D16)**: the claim that the primitives do not make the publisher a component producer is a reasoned position with the residual risk stated (see "Residual risks"), not an unqualified statement.
- **What integrators are told (D20)**: the Verifactu module belongs to the application, and the application's producer lists Encina and its version as a non-RRSIF-specific dependency in the declaración responsable (H, line 113, option (a) of the framework's choices). This ADR gives no checklist for the producer, because that moves toward legal advice.
- **Risks (D14)**: SPEC-002 section 12 question 7 is recorded as risks in this ADR (U1 to U3); U2 is tracked by [#2927](https://github.com/dlrivada/Encina/issues/2927) because it is a reading task, not an AEAT question.
- **Who contacts authorities (D18)**: agents never contact authorities, and a binding DGT ruling (LGT art. 88) can only be requested by a taxpayer. No consultation is sought (see "No AEAT or DGT consultation"), so the question of who asks does not arise.

### The list of primitives (D8)

The regulation-neutral primitives are:

| Primitive | Source | State |
| --- | --- | --- |
| Gapless sequence, `ISequenceGenerator`, gapless per key, enlisted in the ambient transaction | REQ-045, P-36 | Planned |
| Persistent hash-chained log with a pluggable hasher and a chain per partition | REQ-047, P-38 | Planned |
| Outbox partitions with pacing | REQ-042, #469 | Planned |
| The reliable outbox | `Encina.Messaging` outbox ([ADR-037](037-transactional-outbox.md)); its delivery guarantee fixes are tracked by REQ-017 | Shipped |
| The nullable `PartitionKey` column on the outbox | P-51 (REQ-042) | Planned |
| `HashChainAttestationProvider` | [ADR-022](022-provider-agnostic-attestation.md), `src/Encina.Compliance.Attestation/Providers/HashChainAttestationProvider.cs` | Shipped |

The first three are the ones DEC-002 names. The last three are listed so that every piece a Verifactu module would build on has a recorded position. In particular, `HashChainAttestationProvider` already ships and is a chaining implementation; it predates this ADR and stays as it is. Per DEC-009 of SPEC-002, sequences and chained logs are keyed per tenant (AC-043).

### The mTLS client is not on the list (D9)

The mTLS client and X.509 retrieval (REQ-039 and REQ-053, P-31 and P-44) are not listed: DEC-002 and the SPEC-002 section 3 table do not name them as part of the boundary. To cover the "submission" function that the AEAT developer FAQ puts on its relevant list (H, line 103), this ADR states the position directly: `Encina.Http` has no AEAT endpoint, no Verifactu preset and no RRSIF type, and gets none. The package is itself planned (P-31) and not yet in `src/`.

### What regulation-neutral means (D11)

A primitive is regulation-neutral when all of these hold:

1. It has no Verifactu field names, formats or endpoints.
2. It ships no default, preset or hasher that reproduces the RRSIF concatenation. A pluggable hasher with a ready-made RRSIF implementation would leave a generic type one line of configuration away from a Verifactu hash.
3. It has no regulation-named option.

The reasoning that makes such a primitive neutral is the one in H line 110: an append-only hash-chained log or a gap-free counter is not "implementing RRSIF requirements" until the integrator configures it for that purpose.

### How neutrality is checked (D12)

By both an architecture test and a review checklist, each planned and owned by a follow-up issue ([#2925](https://github.com/dlrivada/Encina/issues/2925) for the test, [#2926](https://github.com/dlrivada/Encina/issues/2926) for the checklist). The architecture test is a mechanical floor that fails CI; the checklist judges what a token scan cannot, such as a neutrally named type that implements the RRSIF hash. The cost of a miss is the producer exposure above, which justifies the upkeep of two mechanisms.

### Where the check looks (D13)

At `src/`, plus an allow-list of files that may name the regulation: this ADR, SPEC-002 and its research notes, and knowledge records. Shipped code is clean, and the documents that explain the boundary stay legible. SPEC-002 scenario S9 ("Verifactu-like submission") keeps its name.

### No AEAT or DGT consultation (D17)

No AEAT or DGT consultation was sought, and no dated follow-up is set (sine die). The research notes make a written AEAT answer a precondition only before a Verifactu-specific package ships (H, line 114), and Encina ships none. The re-verification trigger of SPEC-002 section 3.6 ("an AEAT answer on component producers, if P-24 seeks one") therefore stays dormant; if an answer is ever obtained, section 3 is re-verified by pull request within 30 days (REQ-025).

### Where the boundary is published (D23)

In this ADR, plus one sentence in the README of each new primitive package, which the implementation plans of P-36 and P-38 require. A reader of the sequence or chain package meets the boundary there. A SECURITY.md statement and a docs page are not part of this decision; the SECURITY.md legal-status statement belongs to P-27.

### Residual risks

The claim above is a reasoned position, not a certainty. The research notes say the argument that the integrator, not the publisher, is the producer "is strongest there, though still not certain, because 'conservación inalterada' is on the relevant list" (H, line 110); the same list names record generation, chaining and submission (H, line 103). The persistent chained log is a chaining primitive. An AEAT answer could contradict this ADR.

The open points of SPEC-002 section 12 question 7 are recorded here as risks:

| Id | Risk | Tracking |
| --- | --- | --- |
| U1 | Whether a free, independently versioned library implementing RRSIF functions needs its own declaración responsable; the AEAT FAQs on open source and on components point different ways. Unresolved without a written AEAT answer (H, line 123). | ADR risk only |
| U2 | The exact wording of LGT art. 201 bis: read only through a secondary source, and whether free software without sales is sanctionable is untested (H, line 124). | [#2927](https://github.com/dlrivada/Encina/issues/2927) (a reading task of the BOE text) |
| U3 | Whether an application that captures billing data and forwards it to an external SIF is itself a billing component (H, line 125). It concerns the application, not Encina. | ADR risk only |

## Alternatives considered

- **Option B, a separate Verifactu package or repository** and **option C, Verifactu inside Encina**: excluded by DEC-002 (a) and INV-004. Both make the maintainer a Verifactu producer.
- **D8, only the three primitives of DEC-002**: not chosen. The Attestation hash chain already ships and is a chaining implementation, and an unlisted primitive has no recorded position.
- **D9, list the mTLS client**: not chosen. DEC-002 and the section 3 table do not name it, and listing it would extend the boundary beyond the decision. Saying nothing was also rejected, because the submission function would have no recorded position.
- **D11, only the three bans (field names, formats, endpoints)**: not chosen, because a pluggable hasher with a ready-made RRSIF implementation would satisfy them and still be one configuration away from a Verifactu hash. Adding a ban on invoice-chaining recipes in the docs was not chosen either; it costs examples that help other domains.
- **D12, an architecture test only** (cannot judge semantics) or **a review checklist only** (depends on discipline): not chosen; both are used.
- **D13, `src/` only** (no allow-list needed, but the ADR and SPEC would be outside the rule's wording) and **`src/`, `tests/` and `docs/`** (requires renaming SPEC-002 scenario S9): not chosen.
- **D17, an informal AEAT query or a binding DGT ruling**: not chosen. An answer is a precondition only before a Verifactu-specific package ships, and a binding ruling needs a taxpayer to frame the question and takes months.
- **D23, this ADR only** (a reader of the primitive's package would not meet the boundary) and **README plus SECURITY.md plus a docs page** (SECURITY.md work is P-27): not chosen.
- **An unqualified claim of neutrality (D16)**: not chosen, because it states more than the research notes support.

## Consequences

- The maintainer stays outside the producer regime on the evidence the repository holds, and the boundary is recorded before any sequence or chain primitive ships.
- The primitives serve other domains too, since they carry no regulation semantics.
- Integrators write the Verifactu module themselves, which costs them effort.
- The two neutrality checks and the allow-list need upkeep as documents and primitives are added.
- The claim stays a reasoned position; an AEAT answer could change it, and the conditional re-verification trigger in SPEC-002 remains.
- P-36 and P-38 are blocked by this ADR, and their names are fixed in their own plans, not here.

### What each point implies for the code

| Point | What it implies | Owner |
| --- | --- | --- |
| Neutrality criterion (D11) and scope (D13) | An architecture test scanning `src/` for regulation-named tokens, with the allow-list above | [#2925](https://github.com/dlrivada/Encina/issues/2925) |
| Review checklist (D12) | A checklist for pull requests that touch the listed primitives, judging the RRSIF concatenation, defaults, presets and option names | [#2926](https://github.com/dlrivada/Encina/issues/2926) |
| Publication (D23) | One sentence stating the boundary in the README of each new primitive package | The implementation plans of P-36 and P-38 |
| U2 | Read the BOE text of LGT art. 201 bis and record the result | [#2927](https://github.com/dlrivada/Encina/issues/2927) |
| `HashChainAttestationProvider` | No change | None |
| `Encina.Http` (D9) | No AEAT endpoint, Verifactu preset or RRSIF type when it is built | The implementation plan of P-31 |

## Related

- [SPEC-002](../../specifications/SPEC-002-eu-regulatory-readiness.md): DEC-002, INV-004, REQ-042, REQ-045, REQ-047, section 12 question 7
- [SPEC-002 research note H](../../specifications/research/SPEC-002/H-integrations-law.md): the Verifactu analysis and U1 to U3
- [ADR-022](022-provider-agnostic-attestation.md): `HashChainAttestationProvider`
- SPEC-002 section 13.2 defines the P-n ids used above (P-24, P-27, P-31, P-36, P-38, P-44, P-51)
- The D-numbers (D8 to D23) are the decision labels of the register in the [#1226 body](https://github.com/dlrivada/Encina/issues/1226) and the maintainer's [decision comment](https://github.com/dlrivada/Encina/issues/1226#issuecomment-6439624866)
- Issues: [#1226](https://github.com/dlrivada/Encina/issues/1226), [#469](https://github.com/dlrivada/Encina/issues/469)

## Date

2026-10-27
