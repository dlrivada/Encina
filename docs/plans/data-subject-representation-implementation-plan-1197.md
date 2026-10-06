# Implementation Plan: Data-Subject Representation and Deceased Status — persisted shape in `Encina.Compliance.Consent` and `Encina.Compliance.DataSubjectRights`

> **Issue**: [#1197](https://github.com/dlrivada/Encina/issues/1197)
> **Type**: Feature
> **Parent**: [#1186](https://github.com/dlrivada/Encina/issues/1186) (EU regulatory readiness, SPEC-002); SPEC-002 REQ-011, AC-011, scenario S15, tracking id **P-09** (P0, v0.17.0 — Compliance Lifecycle)
> **Related**: [#814](https://github.com/dlrivada/Encina/issues/814) (DSR rejection reasons, same event family, plan in PR #1930), [#1255](https://github.com/dlrivada/Encina/issues/1255) (P-52, consent given for a minor, reuses this shape), [#1937](https://github.com/dlrivada/Encina/issues/1937) (DSR lifecycle telemetry), [#1227](https://github.com/dlrivada/Encina/issues/1227) (P-25, reference scenario and the S15 rules skeleton), [#1195](https://github.com/dlrivada/Encina/issues/1195) (P-07, access export with withholding reasons), [#1257](https://github.com/dlrivada/Encina/issues/1257) (multi-tenancy of SPEC-002 capabilities)
> **Complexity**: Medium (10 phases, three packages, Marten only, no database provider)
> **Estimated Scope**: ~900-1,200 lines of production code + ~1,500-2,000 lines of tests

---

## Summary

Give Consent and DSR records a typed, persisted way to say **who acted for whom, under which authority and until when** (a holder of parental authority or a guardian for a minor, a voluntary proxy, a legal representative, a person linked to a deceased subject), and give a data subject a **status record** that can say the subject is deceased, when, who may request on the subject's behalf, and which rights the subject prohibited others from exercising. Before 1.0 only the shape is built: the fields are persisted in the Marten event streams, projected into the read models and exposed by the services. No rule acts on them (no proxy-validity check, no refusal of a relative's request); those rules are post-1.0 and must fit this shape without a breaking change, which a compiled-but-skipped skeleton of the S15 rule proves.

Standards covered (as the persisted shape they require, not as rules):

- LOPDGDD (LO 3/2018) art. 3 — persons linked to a deceased subject may exercise access, rectification and erasure unless the deceased expressly prohibited it; designated persons and institutions; executors.
- Ley 41/2002 arts. 9.3–9.4 (consent by representation, minors), 18.2 (access by duly accredited representation) and 18.4 (relatives' access to a deceased patient's record unless expressly prohibited).
- GDPR Art. 8 and Art. 12(6) context; EHDS (Regulation (EU) 2025/327) proxy services.

### What exists today (read on 2026-10-06, main at 5b485b12)

Nothing of #1197 is implemented. SPEC-002 §6 (gap table, line 414) records "no guardian, representative or deceased concept in `src/`"; a search for `Representative|Guardian|ActedFor|ActedBy|Deceased|OnBehalfOf|ParentalAuthority` in `src/` finds nothing.

| Element | Location | Today |
|---|---|---|
| Consent events | `src/Encina.Compliance.Consent/Events/ConsentEvents.cs:36-213` | Six records; `ConsentGranted.GrantedBy` (line 46) is "the actor who recorded the consent (may differ from the data subject)": it conflates the clerk who recorded the act with a representative who performed it |
| Consent aggregate | `src/Encina.Compliance.Consent/Aggregates/ConsentAggregate.cs:157-430` | `Grant`, `Withdraw`, `Renew`, `ChangeVersion`, `ProvideReconsent`, `Apply` |
| Consent read model / projection | `ReadModels/ConsentReadModel.cs:25-136`, `ReadModels/ConsentProjection.cs:30-188` | No representation field |
| Consent service | `Abstractions/IConsentService.cs:62-138`, `Services/DefaultConsentService.cs:135-` | Tenant-scoped since #1315 (`DefaultConsentService.cs:39-128`, `ConsentOptions.RequireTenantContext` at `ConsentOptions.cs:148`) |
| Consent telemetry | `Diagnostics/ConsentDiagnostics.cs:9-101` | Only a `Consent.Check` span (line 78) for the pipeline behaviour; no span on service commands. The issue's "existing span `encina.consent.record`" does not exist |
| DSR events | `src/Encina.Compliance.DataSubjectRights/Events/DSRRequestEvents.cs:29-174` | `DSRRequestSubmitted` (line 29) carries `SubjectId` and no actor at all |
| DSR aggregate / read model / projection | `Aggregates/DSRRequestAggregate.cs:160-177, 355-`, `Projections/DSRRequestReadModel.cs:26-195`, `Projections/DSRRequestProjection.cs:54-80` | No representation field |
| DSR service | `Abstractions/IDSRService.cs:68-74`, `Services/DefaultDSRService.cs:100-138` | `SubmitRequestAsync(subjectId, rightType, requestDetails, tenantId, moduleId, ct)`; **no tenant filtering on any query** (`:687-807`), cache keys without tenant (`:659`, `:771`) |
| Data-subject status | — | No aggregate, read model or service records anything about a subject outside a single request or consent |
| Event IDs | `src/Encina/Diagnostics/EventIdRanges.cs:313` (`ComplianceConsent` 8200-8299), `:316` (`ComplianceDSR` 8300-8349) | Consent uses 8200-8207, 8230-8232, 8240-8243, 8250, 8260-8268 (`LoggerMessage.Define`); DSR uses 8300-8303, 8310, 8320-8349; #814 takes 8311-8312 |

### Defect found while planning

The DSR service never filters by tenant (the DSR counterpart of #1315, closed for Consent). This plan's status record must be tenant-scoped (REQ-061), and the existing DSR request reads are not. The defect is drafted as `artifacts/issues/plan-1197-dsr-tenant-scoping.md` (`[BUG]`, v0.14.0) and is a prerequisite (Design Choice 6).

### Scope

- **Affected packages**: `Encina.Compliance.GDPR` (shared representation types), `Encina.Compliance.Consent`, `Encina.Compliance.DataSubjectRights`.
- **Provider category**: none of the database providers. Both aggregates are event-sourced on Marten (PostgreSQL); SPEC-002 DEC-008 (a) and ADR-019 keep event-sourced compliance modules Marten-only in 1.0, so integration tests run against Marten through Testcontainers (`AGENTS.md` §3).
- **Estimated files**: ~22 new, ~20 modified production and test files (Research, "Estimated File Count").

---

## Design Choices

<details>
<summary><strong>1. Placement of the shared representation types</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Compliance.GDPR` (`Model/DataSubjectRepresentation.cs`, `Model/RepresentationAuthority.cs`, `Abstractions/IRepresentedAct.cs`); `Encina.Compliance.Consent` gains a project reference to it** | One type for Consent, DSR and later P-52 (#1255); `Encina.Compliance.GDPR` is light (references only `Encina` and `Microsoft.Extensions.Diagnostics.HealthChecks`) and `Encina.Compliance.DataSubjectRights.csproj` already references it; representation of a data subject is a GDPR-wide concept | Consent takes a new dependency it did not need before; GDPR package grows by three files |
| **B) Duplicate the types in each package (`Encina.Compliance.Consent.DataSubjectRepresentation`, `Encina.Compliance.DataSubjectRights.DataSubjectRepresentation`)** | No new dependency | Two vocabularies for one legal concept; the post-1.0 rules and #1255 must map between them; drift risk on persisted shapes |
| **C) Core `Encina` package** | Every package can use it without a new reference | Puts GDPR-specific legal vocabulary into the mediator core, against pay-for-what-you-use |
| **D) New package `Encina.Compliance.Representation`** | Maximal isolation | One more NuGet package, csproj, README, PublicAPI and coverage manifest for three small types |

### Chosen Option: **A — `Encina.Compliance.GDPR`** (recommended, pending the maintainer)

### Rationale

- We recommend A because it gives one persisted vocabulary to the two aggregates this issue changes and to #1255 ("reusing the representation shape of P-09"), at the cost of one light reference from Consent.
- The status record and its prohibition types (Design Choice 4) stay in `Encina.Compliance.DataSubjectRights`, because they reference `DataSubjectRight` (`Model/DataSubjectRight.cs`) and belong to the DSR domain.
- An ADR records the placement so later compliance modules (for example a future clinical-records module) reuse it instead of re-inventing it.

</details>

<details>
<summary><strong>2. Shape of the representation value</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One nullable value object `DataSubjectRepresentation(string RepresentativeId, RepresentationAuthority Authority, DateTimeOffset? ValidUntilUtc, string? AuthorityReference)`; acted-for is the record's own subject id** | Null means "the subject acted personally"; all four facts travel together and cannot be half-set; no redundant subject id that could contradict `DataSubjectId`/`SubjectId`; `AuthorityReference` points to the accrediting document (court resolution, power of attorney) without storing it | AC-011's "acted-for" is read from the record's subject field, not from the value object; documentation must say so |
| **B) Four flat nullable fields on every event and read model (`ActedBy`, `ActedFor`, `Authority`, `ValidUntilUtc`), as the issue's Proposed Solution lists them** | Literal match to the issue | Four nullable parameters per event, any subset can be set; `ActedFor` duplicates the subject id and can disagree with it; validation spread over every command |
| **C) Value object with an explicit `ActedForSubjectId` field** | "Acted-for" is visible inside the value | Same redundancy as B; a validation rule must force it equal to the subject id, so the field carries no information |

### Chosen Option: **A — one nullable value object, acted-for implied by the record's subject** (recommended, pending the maintainer)

### Rationale

- We recommend A because a representation is one legal fact; making it a single value prevents inconsistent partial states, and the record already names whom it is about.
- `RepresentationAuthority` uses explicit, stable numbering and no `0` member: `ParentalAuthority = 1`, `Guardian = 2` (tutela or curatela with representation), `VoluntaryProxy = 3`, `LegalRepresentative = 4`, `PersonLinkedToDeceased = 5` (LOPDGDD art. 3, the case the S15 rule needs). New members are additive after 1.0.
- `RepresentativeId` is an identifier, never a name or contact data (data minimisation, GDPR Art. 5(1)(c)); it is personal data of a third person, so it never reaches logs, activity tags or metric tags (#1314, #1429).
- `ValidUntilUtc` is stored, not enforced: checking a proxy's validity is a post-1.0 rule (REQ-011). Structural validation only (non-blank id, defined enum value) returns `Left` with `consent.invalid_representation` / `dsr.invalid_representation`.
- The issue's AC-011 wording is met: the read model returns acted-by (`Representation.RepresentativeId`), acted-for (`DataSubjectId` / `SubjectId`), authority and validity.

</details>

<details>
<summary><strong>3. Which events carry the representation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The subject-side acts only: `ConsentGranted`, `ConsentWithdrawn`, `ConsentRenewed`, `ConsentReconsentProvided`, `DSRRequestSubmitted`, plus the status record's preference event; each implements `IRepresentedAct`** | Exactly the acts a data subject (or a representative) performs; controller-side events (`ConsentVersionChanged`, `DSRRequestVerified`, `…Processing`, `…Completed`, `…Denied`, `…Extended`, `…Expired`, `ConsentExpired`) stay unchanged, which also keeps the overlap with #814 (`DSRRequestDenied`) to file-level only; `IRepresentedAct` gives the post-1.0 rules and the contract tests one type across both modules | Six event constructors change (pre-1.0, acceptable) |
| **B) Every event of both aggregates** | Uniform | Puts a meaningless field on controller and system events (a deadline expiry has no representative); collides with #814 on `DSRRequestDenied` |
| **C) A separate `RepresentationRecorded` event appended next to the act** | Existing event constructors unchanged | Two events for one act; the read model must correlate them; a crash or a partial append could leave an act without its representation; harder to query |

### Chosen Option: **A — subject-side acts, with `IRepresentedAct`** (recommended, pending the maintainer)

### Rationale

- We recommend A because representation qualifies the act itself; it belongs on the event that records the act, and only the subject-side acts can be performed by a representative.
- The new parameter is the last positional parameter before the tenant/module pair where those exist, and nullable, so streams written before the change deserialize with `Representation = null` (Marten tolerates the missing member; asserted by an integration test).
- Read models: `ConsentReadModel.Representation` is the representation of the act that established the current state (grant, renewal, reconsent), and `ConsentReadModel.WithdrawalRepresentation` records who withdrew; `DSRRequestReadModel.Representation` comes from `DSRRequestSubmitted`. The full history stays in the event stream (`GetConsentHistoryAsync`).

</details>

<details>
<summary><strong>4. Persistence of the deceased status, authorised requesters and prohibition</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New event-sourced `DataSubjectStatusAggregate` in `Encina.Compliance.DataSubjectRights` (one stream per tenant and subject, deterministic id), with `DataSubjectStatusReadModel`, an inline projection and `IDataSubjectStatusService`** | The status is a fact about the subject, independent of any request or consent; a prohibition can be recorded while the subject is alive (Ley 41/2002 art. 18.4 requires it to exist before death); the event stream is the audit trail; same Marten pattern as the two existing aggregates | One new aggregate, projection, service and DI registration |
| **B) Fields on `DSRRequestAggregate` (deceased flag and prohibition per request)** | No new aggregate | A prohibition expressed in life has no request to live on; the same subject's status would be copied into every request and could disagree between requests |
| **C) An application-implemented `IDataSubjectStatusProvider`; Encina persists nothing** | Smallest Encina change | Not a persisted shape (AC-011 fails); the post-1.0 rules would have nothing typed to read |
| **D) Free-form metadata on the subject's consent records** | No new types | Rejected by the issue itself (Alternative 2): not queryable, not type-checked |

### Chosen Option: **A — `DataSubjectStatusAggregate` with a deterministic stream id** (recommended, pending the maintainer)

### Rationale

- We recommend A because it is the only option where a living subject's prohibition and a later death record share one typed, auditable record per subject.
- Events: `DataSubjectStatusOpened`, `DataSubjectAccessPreferencesRecorded` (prohibition plus authorised requesters, expressed by the subject or a representative, `IRepresentedAct`), `DataSubjectDeceasedRecorded` (date of death, recorder, evidence reference) and `DataSubjectDeceasedRecordRevoked` (correction of a mistaken death record, with a reason). The aggregate enforces state transitions only (no second death record, no revocation without one); it applies no access rule.
- Stream id: a name-based UUID (RFC 9562 v5) of `"{tenantId ?? "-"}:{subjectId}"` under a fixed namespace GUID, so concurrent first writes for the same subject collide on `CreateAsync` instead of producing two streams, and lookup needs no query. The subject id is never stored in clear outside the event and read model fields that already hold it.
- Prohibition shape: `DataSubjectAccessProhibition(IReadOnlyList<DataSubjectRight> ProhibitedRights, IReadOnlyList<AuthorisedRequesterBasis> AppliesTo, DateTimeOffset ExpressedAtUtc, string? EvidenceReference)`; requester shape: `AuthorisedRequester(string RequesterId, AuthorisedRequesterBasis Basis, IReadOnlyList<DataSubjectRight>? Rights)`, with `AuthorisedRequesterBasis` = `FamilyOrDeFactoRelation = 1`, `DesignatedPerson = 2`, `DesignatedInstitution = 3`, `ExecutorOfWill = 4`, `Heir = 5`, `LegalRepresentative = 6` (LOPDGDD art. 3.1-3.3).
- `DataSubjectDeceasedRecorded` is also the anchor #1187 (retention starting at death) can subscribe to.

</details>

<details>
<summary><strong>5. The post-1.0 rules: skeleton and extension point</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No new public interface; a compile-only skeleton `RepresentationRulesSkeleton` in `tests/Encina.UnitTests/Compliance/DataSubjectRights/Representation/` expresses the S15 rule against the public read models and value types, with one test skipped with a reason that names SPEC-002 REQ-011; #1227 (P-25) reuses it in the reference scenario** | Ships no public API that nothing calls ("every line serves a current purpose", `AGENTS.md` §3); the skeleton fails to compile the day the shape stops supporting the rule; adding the rule later is additive (a new behaviour, not a changed shape) | The post-1.0 wiring point (where the rule is invoked) is chosen later |
| **B) Public `IRepresentationRequestPolicy` shipped in 1.0, not invoked; the skeleton implements it** | The rule's contract is fixed now | A public interface with no caller in 1.0; its signature is guessed before the rules are designed and becomes a shipped API to keep |
| **C) Public policy interface invoked by `SubmitRequestAsync` and the consent commands, with a permit-all default** | The hook is live; post-1.0 only swaps the implementation | A permit-all "gate" in a compliance path contradicts fail-closed gates (`AGENTS.md` §3) and SPEC-002 DEC-006; behaviour exists before its rules are designed |

### Chosen Option: **A — compile-only skeleton in tests, no new public interface** (recommended, pending the maintainer)

### Rationale

- We recommend A because AC-038 asks that the rules "compile against the 1.0 public API" without a change to it, which a skeleton written against the persisted shape proves, while B and C add public surface whose design belongs to the post-1.0 rules issue.
- The skeleton covers both S15 rules named in REQ-011: refusing a `PersonLinkedToDeceased` request when the subject is deceased and `AccessProhibition` covers the right and the requester's basis, and refusing a representation whose `ValidUntilUtc` has passed.
- The skipped test uses `Assert.Skip("SPEC-002 REQ-011: rules are post-1.0; this skeleton only proves the 1.0 shape supports them")`, which satisfies "never skip a test without justification".

</details>

<details>
<summary><strong>6. Tenant scoping of the new status record and the DSR reads</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fix the DSR tenant defect first (`artifacts/issues/plan-1197-dsr-tenant-scoping.md`: ambient tenant, `RequireTenantContext` on `DataSubjectRightsOptions`, tenant in cache keys, `Encina.Tenancy` reference), then build the status service on that helper** | One tenant-scope pattern in the DSR package, copied from #1315; this plan's status service and the `Representation` read path are tenant-safe from day one; the bug is fixed regardless of P-09 | Adds a prerequisite PR before #1197 |
| **B) Introduce the tenant-scope helper inside #1197, for the status service only; the bug fix reuses it later** | No prerequisite | #1197 grows by a cross-cutting change; the existing DSR reads stay leaky until the bug lands; two PRs touch the same helper |
| **C) Store `TenantId` on the status events only and leave query filtering to #1257** | Smallest #1197 | Fails the issue's tenant-aware acceptance item ("queries ... filter by it; a two-tenant test shows tenant A never reads ... tenant B's data") |

### Chosen Option: **A — tenant defect first, then #1197 uses its helper** (recommended, pending the maintainer)

### Rationale

- We recommend A because the leak in `DefaultDSRService.cs:687-807` is a defect on its own (it can make `ProcessingRestrictionPipelineBehavior` block tenant B on tenant A's restriction), and fixing it once gives #1197 a tested pattern.
- If the maintainer prefers B, Phase 6 below already lists the helper's contract, so the same tasks move into #1197 unchanged.
- Consent is already tenant-scoped (#1315); the Consent half of this plan only keeps the tenant on the aggregate and needs no new tenant logic.

</details>

<details>
<summary><strong>7. Ordering with #814, #1937 and #1255</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) #814 → DSR tenant bug → #1197 → #1255 (P-52); #1937 before or after #1197, with the EventId split below** | #814 is small, has no prerequisites and is already planned (PR #1930); #1197 then rebases once on its `DSRRequestEvents.cs`, `IDSRService.cs`, `DefaultDSRService.cs`, projection and read-model changes; #1255 builds on the representation type instead of inventing one | #1255 sits in an earlier milestone (v0.15.0) than #1197 (v0.17.0): either #1197 moves to v0.15.0 or #1255 waits |
| **B) #1197 before #814** | P-09 is P0 | #814 then rebases on six changed DSR files; #814's plan already sequences itself first ("sequence the two PRs") |
| **C) #1255 first, defining its own parental-authority field; #1197 later generalises it** | Respects the current milestones | #1197 would change #1255's persisted consent shape: the breaking change both issues exist to avoid |

### Chosen Option: **A — #814, DSR tenant bug, #1197, then #1255** (recommended, pending the maintainer)

### Rationale

- We recommend A, and we recommend moving #1197 into v0.15.0 next to #1255, because #1255 depends on this shape and both are P0.
- Overlap with #814 is file-level only: #814 changes `DSRRequestDenied` and `DenyRequestAsync`; #1197 changes `DSRRequestSubmitted` and `SubmitRequestAsync`. Neither changes the other's event.
- EventIds: #814 takes 8311-8312; this plan takes 8313-8316 (DSR) and 8269-8270 (Consent); #1937 keeps 8304-8309 and 8317-8319. If #1937 needs more than nine, it registers a second DSR range rather than reusing these.
- #1937 instruments `SubmitRequestAsync` with `StartAggregateCommand("Submit")`. Whichever lands second adds to the other's activity: if #1197 lands first it starts that activity itself (Phase 7).

</details>

---

## Implementation Phases

### Phase 1: Shared Representation Model (`Encina.Compliance.GDPR`)

> **Goal**: One persisted representation vocabulary for Consent, DSR and #1255.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Compliance.GDPR/Model/RepresentationAuthority.cs` — `public enum RepresentationAuthority { ParentalAuthority = 1, Guardian = 2, VoluntaryProxy = 3, LegalRepresentative = 4, PersonLinkedToDeceased = 5 }`, namespace `Encina.Compliance.GDPR`, XML docs citing Ley 41/2002 arts. 9.3, 18.2, 18.4 and LOPDGDD art. 3
2. **Create** `src/Encina.Compliance.GDPR/Model/DataSubjectRepresentation.cs` — `public sealed record DataSubjectRepresentation(string RepresentativeId, RepresentationAuthority Authority, DateTimeOffset? ValidUntilUtc = null, string? AuthorityReference = null)`; `override ToString()` that omits `RepresentativeId` and `AuthorityReference` (personal data of the representative)
3. **Create** `src/Encina.Compliance.GDPR/Model/DataSubjectRepresentationValidation.cs` — `public static class DataSubjectRepresentationValidation` with `public static Option<string> Validate(DataSubjectRepresentation? representation)` returning `None` when null or valid, `Some(reason)` for blank `RepresentativeId` or an undefined `Authority` (structural only, no validity date check)
4. **Create** `src/Encina.Compliance.GDPR/Abstractions/IRepresentedAct.cs` — `public interface IRepresentedAct { DataSubjectRepresentation? Representation { get; } }`
5. **Modify** `src/Encina.Compliance.Consent/Encina.Compliance.Consent.csproj` — add `<ProjectReference Include="..\Encina.Compliance.GDPR\Encina.Compliance.GDPR.csproj" />`
6. **Modify** `src/Encina.Compliance.GDPR/PublicAPI.Unshipped.txt` — the new symbols (delegate to `mechanical-fixer`)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library (pre-1.0, no backward compatibility, nullable enabled, ROP with
Either<EncinaError, T>). Issue #1197 (SPEC-002 REQ-011, P-09) adds the persisted shape of data-subject
representation to the Consent and DSR aggregates. This phase creates the shared types in
src/Encina.Compliance.GDPR/, which references only src/Encina/. Encina.Compliance.DataSubjectRights already
references Encina.Compliance.GDPR; Encina.Compliance.Consent does not yet.

TASK:
1. Create Model/RepresentationAuthority.cs: enum with explicit values ParentalAuthority = 1, Guardian = 2,
   VoluntaryProxy = 3, LegalRepresentative = 4, PersonLinkedToDeceased = 5 (no 0 member).
2. Create Model/DataSubjectRepresentation.cs: sealed record (string RepresentativeId,
   RepresentationAuthority Authority, DateTimeOffset? ValidUntilUtc = null, string? AuthorityReference = null).
   Override ToString() to print only Authority and ValidUntilUtc.
3. Create Model/DataSubjectRepresentationValidation.cs: static Validate(DataSubjectRepresentation?) returning
   Option<string> (None = valid or null; Some("representative_id_required") or Some("authority_undefined")).
4. Create Abstractions/IRepresentedAct.cs with a single DataSubjectRepresentation? Representation { get; }.
5. Add a ProjectReference from Encina.Compliance.Consent.csproj to Encina.Compliance.GDPR.csproj.
6. Add every new public symbol to src/Encina.Compliance.GDPR/PublicAPI.Unshipped.txt.

KEY RULES:
- XML docs on every public type and member, citing LOPDGDD art. 3 and Ley 41/2002 arts. 9.3, 18.2, 18.4.
- The representative id is personal data of a third person: never in ToString(), logs or telemetry.
- Shape only: do not check ValidUntilUtc against the clock (that is a post-1.0 rule).
- No [Obsolete], no compatibility aliases. Zero warnings.

REFERENCE FILES:
- src/Encina.Compliance.GDPR/Model/ (record and enum style)
- src/Encina.Compliance.GDPR/Encina.Compliance.GDPR.csproj
- src/Encina.Compliance.Consent/Encina.Compliance.Consent.csproj
- docs/plans/data-subject-representation-implementation-plan-1197.md (Design Choices 1-3)
```

</details>

---

### Phase 2: Representation on Consent

> **Goal**: Every subject-side consent act records whether a representative performed it.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.Compliance.Consent/Events/ConsentEvents.cs`:
   - `ConsentGranted` (line 36): add `DataSubjectRepresentation? Representation` before `TenantId`; implement `IRepresentedAct`
   - `ConsentWithdrawn` (line 71), `ConsentRenewed` (line 129): add `DataSubjectRepresentation? Representation` after the actor parameter; implement `IRepresentedAct`
   - `ConsentReconsentProvided` (line 202): add `DataSubjectRepresentation? Representation` after `GrantedBy`; implement `IRepresentedAct`
   - XML docs: distinguish the recorder (`GrantedBy`, `WithdrawnBy`, `RenewedBy`) from the representative
2. **Modify** `src/Encina.Compliance.Consent/Aggregates/ConsentAggregate.cs` — `Grant(..., DataSubjectRepresentation? representation = null, string? tenantId = null, string? moduleId = null)`; `Withdraw(string withdrawnBy, string? reason, DateTimeOffset occurredAtUtc, DataSubjectRepresentation? representation = null)`; `Renew(..., DataSubjectRepresentation? representation = null)`; `ProvideReconsent(..., DataSubjectRepresentation? representation = null)`; properties `Representation` and `WithdrawalRepresentation`; `Apply` (line 369) sets them (renew and reconsent replace `Representation`; reconsent clears `WithdrawalRepresentation`)
3. **Modify** `src/Encina.Compliance.Consent/ReadModels/ConsentReadModel.cs` — `public DataSubjectRepresentation? Representation { get; set; }`, `public DataSubjectRepresentation? WithdrawalRepresentation { get; set; }`
4. **Modify** `src/Encina.Compliance.Consent/ReadModels/ConsentProjection.cs` — `Create` (line 52) and the `Apply` overloads for withdrawn (85), renewed (124) and reconsent (174) copy the fields, mirroring the aggregate
5. **Modify** `src/Encina.Compliance.Consent/Abstractions/IConsentService.cs` — `GrantConsentAsync`, `WithdrawConsentAsync`, `RenewConsentAsync`, `ProvideReconsentAsync` gain `DataSubjectRepresentation? representation = null` before `CancellationToken`
6. **Modify** `src/Encina.Compliance.Consent/Services/DefaultConsentService.cs` — each of the four commands validates with `DataSubjectRepresentationValidation.Validate` and returns `ConsentErrors.InvalidRepresentation(reason)` on `Some`; passes the value to the aggregate
7. **Modify** `src/Encina.Compliance.Consent/ConsentErrors.cs` — `public const string InvalidRepresentationCode = "consent.invalid_representation";` and `InvalidRepresentation(string reason)` (reason is a code, never the representative id)
8. **Modify** `src/Encina.Compliance.Consent/PublicAPI.Unshipped.txt` (delegate to `mechanical-fixer`)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 of issue #1197 created Encina.Compliance.GDPR.DataSubjectRepresentation, RepresentationAuthority,
DataSubjectRepresentationValidation and IRepresentedAct, and Encina.Compliance.Consent now references
Encina.Compliance.GDPR. Consent is event-sourced on Marten: events in Events/ConsentEvents.cs, aggregate in
Aggregates/ConsentAggregate.cs, inline projection ReadModels/ConsentProjection.cs into ConsentReadModel, service
Services/DefaultConsentService.cs (tenant-scoped since #1315). Pre-1.0: change constructors completely.

TASK:
1. Add DataSubjectRepresentation? Representation to ConsentGranted (before TenantId), ConsentWithdrawn and
   ConsentRenewed (after the actor), ConsentReconsentProvided (after GrantedBy); each implements IRepresentedAct.
2. ConsentAggregate: optional representation parameter on Grant, Withdraw, Renew, ProvideReconsent; properties
   Representation and WithdrawalRepresentation; Apply sets them (renew/reconsent replace Representation, reconsent
   clears WithdrawalRepresentation, withdraw sets WithdrawalRepresentation).
3. ConsentReadModel + ConsentProjection: same two properties, same semantics as the aggregate.
4. IConsentService/DefaultConsentService: representation parameter (before CancellationToken) on the four
   commands; validate with DataSubjectRepresentationValidation.Validate; on Some(reason) return
   ConsentErrors.InvalidRepresentation(reason) without touching the repository.
5. ConsentErrors: InvalidRepresentationCode = "consent.invalid_representation" and its factory.
6. Update PublicAPI.Unshipped.txt.

KEY RULES:
- Shape only: no rule reads Representation (no validity or authority check).
- The representative id never reaches a log, activity tag, metric tag or EncinaError message.
- Keep tenant handling exactly as DefaultConsentService does today (TryResolveWriteTenantScope, TenantsMatch).
- XML docs on every changed public member; zero warnings; CRAP <= 10 on changed methods.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Events/ConsentEvents.cs
- src/Encina.Compliance.Consent/Aggregates/ConsentAggregate.cs
- src/Encina.Compliance.Consent/ReadModels/ConsentProjection.cs
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- src/Encina.Compliance.Consent/ConsentErrors.cs
```

</details>

---

### Phase 3: Representation on DSR Requests

> **Goal**: A DSR request records whether a representative submitted it.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.Compliance.DataSubjectRights/Events/DSRRequestEvents.cs:29-37` — `DSRRequestSubmitted(Guid RequestId, string SubjectId, DataSubjectRight RightType, DateTimeOffset ReceivedAtUtc, DateTimeOffset DeadlineAtUtc, string? RequestDetails, DataSubjectRepresentation? Representation, string? TenantId, string? ModuleId) : INotification, IRepresentedAct`
2. **Modify** `Aggregates/DSRRequestAggregate.cs` — `Submit(..., string? requestDetails = null, DataSubjectRepresentation? representation = null, string? tenantId = null, string? moduleId = null)` (line 160); property `Representation`; `Apply` case `DSRRequestSubmitted` (line 359) sets it
3. **Modify** `Projections/DSRRequestReadModel.cs` — `public DataSubjectRepresentation? Representation { get; set; }`
4. **Modify** `Projections/DSRRequestProjection.cs:54-80` — `Create` copies `Representation`
5. **Modify** `Abstractions/IDSRService.cs:68-74` and `Services/DefaultDSRService.cs:100-138` — `SubmitRequestAsync(..., string? requestDetails = null, DataSubjectRepresentation? representation = null, string? tenantId = null, string? moduleId = null, CancellationToken)`; validate and return `DSRErrors.InvalidRepresentation(reason)`
6. **Modify** `DSRErrors.cs` — `InvalidRepresentationCode = "dsr.invalid_representation"` and factory
7. **Modify** `PublicAPI.Unshipped.txt` (delegate to `mechanical-fixer`)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Issue #1197, Phase 3. Encina.Compliance.DataSubjectRights is event-sourced on Marten: DSRRequestSubmitted in
Events/DSRRequestEvents.cs:29, DSRRequestAggregate.Submit at Aggregates/DSRRequestAggregate.cs:160 and Apply at
:355, inline projection Projections/DSRRequestProjection.cs (Create at :54) into DSRRequestReadModel, service
Services/DefaultDSRService.cs (SubmitRequestAsync at :100). #814 has already changed DSRRequestDenied and
DenyRequestAsync in the same files: rebase on it and do not touch the deny path. The shared types live in
Encina.Compliance.GDPR (DataSubjectRepresentation, IRepresentedAct, DataSubjectRepresentationValidation).

TASK:
1. Add DataSubjectRepresentation? Representation to DSRRequestSubmitted after RequestDetails; implement
   IRepresentedAct.
2. DSRRequestAggregate.Submit gains representation (after requestDetails); property Representation; Apply sets it.
3. DSRRequestReadModel.Representation; DSRRequestProjection.Create copies it.
4. IDSRService/DefaultDSRService.SubmitRequestAsync gains representation (after requestDetails); validate with
   DataSubjectRepresentationValidation.Validate; Some(reason) -> DSRErrors.InvalidRepresentation(reason).
5. DSRErrors: InvalidRepresentationCode = "dsr.invalid_representation" plus factory.
6. Update PublicAPI.Unshipped.txt.

KEY RULES:
- Only the submit path changes; controller-side events stay as they are.
- No rule reads Representation; no subject id or representative id in logs, tags or error messages (#1429).
- Pre-1.0: change the constructor completely, no overloads kept for the old shape.
- XML docs; zero warnings; CRAP <= 10 on changed methods.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Events/DSRRequestEvents.cs
- src/Encina.Compliance.DataSubjectRights/Aggregates/DSRRequestAggregate.cs
- src/Encina.Compliance.DataSubjectRights/Projections/DSRRequestProjection.cs
- src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs
- src/Encina.Compliance.DataSubjectRights/DSRErrors.cs
```

</details>

---

### Phase 4: Data-Subject Status Record

> **Goal**: A per-subject, tenant-scoped record of deceased status, authorised requesters and the subject's prohibition.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Compliance.DataSubjectRights/Model/AuthorisedRequesterBasis.cs` — enum `FamilyOrDeFactoRelation = 1, DesignatedPerson = 2, DesignatedInstitution = 3, ExecutorOfWill = 4, Heir = 5, LegalRepresentative = 6`
2. **Create** `Model/AuthorisedRequester.cs` — `public sealed record AuthorisedRequester(string RequesterId, AuthorisedRequesterBasis Basis, IReadOnlyList<DataSubjectRight>? Rights = null)`; `ToString()` without `RequesterId`
3. **Create** `Model/DataSubjectAccessProhibition.cs` — `public sealed record DataSubjectAccessProhibition(IReadOnlyList<DataSubjectRight> ProhibitedRights, IReadOnlyList<AuthorisedRequesterBasis> AppliesTo, DateTimeOffset ExpressedAtUtc, string? EvidenceReference = null)`
4. **Create** `Events/DataSubjectStatusEvents.cs` — all `: INotification`:
   - `DataSubjectStatusOpened(Guid StatusId, string SubjectId, DateTimeOffset OccurredAtUtc, string? TenantId, string? ModuleId)`
   - `DataSubjectAccessPreferencesRecorded(Guid StatusId, DataSubjectAccessProhibition? AccessProhibition, IReadOnlyList<AuthorisedRequester> AuthorisedRequesters, string RecordedBy, DataSubjectRepresentation? Representation, DateTimeOffset OccurredAtUtc, string? TenantId, string? ModuleId) : IRepresentedAct`
   - `DataSubjectDeceasedRecorded(Guid StatusId, DateTimeOffset DeceasedAtUtc, string RecordedBy, string? EvidenceReference, DateTimeOffset OccurredAtUtc, string? TenantId, string? ModuleId)`
   - `DataSubjectDeceasedRecordRevoked(Guid StatusId, string Reason, string RecordedBy, DateTimeOffset OccurredAtUtc, string? TenantId, string? ModuleId)`
5. **Create** `Aggregates/DataSubjectStatusAggregate.cs` — `public sealed class DataSubjectStatusAggregate : AggregateBase`; `static Open(Guid id, string subjectId, DateTimeOffset occurredAtUtc, string? tenantId, string? moduleId)`; `RecordAccessPreferences(...)`, `RecordDeceased(DateTimeOffset deceasedAtUtc, string recordedBy, string? evidenceReference, DateTimeOffset occurredAtUtc)` (throws `InvalidOperationException` when already deceased), `RevokeDeceasedRecord(string reason, string recordedBy, DateTimeOffset occurredAtUtc)` (throws when not deceased); properties `SubjectId`, `IsDeceased`, `DeceasedAtUtc`, `DeathEvidenceReference`, `AuthorisedRequesters`, `AccessProhibition`, `PreferencesRepresentation`, `TenantId`, `ModuleId`
6. **Create** `Aggregates/DataSubjectStatusId.cs` — `internal static class DataSubjectStatusId { public static Guid For(string? tenantId, string subjectId) }` (RFC 9562 v5 over `"{tenantId ?? "-"}:{subjectId}"`, fixed namespace GUID)
7. **Create** `Projections/DataSubjectStatusReadModel.cs` (`IReadModel`; same fields plus `LastModifiedAtUtc`, `Version`) and `Projections/DataSubjectStatusProjection.cs` (`IProjectionCreator<DataSubjectStatusOpened, …>`, handlers for the other three events)
8. **Create** `Abstractions/IDataSubjectStatusService.cs`:
   - `ValueTask<Either<EncinaError, Guid>> RecordAccessPreferencesAsync(string subjectId, DataSubjectAccessProhibition? accessProhibition, IReadOnlyList<AuthorisedRequester> authorisedRequesters, string recordedBy, DataSubjectRepresentation? representation = null, CancellationToken cancellationToken = default)`
   - `ValueTask<Either<EncinaError, Guid>> RecordDeceasedAsync(string subjectId, DateTimeOffset deceasedAtUtc, string recordedBy, string? evidenceReference = null, CancellationToken cancellationToken = default)`
   - `ValueTask<Either<EncinaError, Unit>> RevokeDeceasedRecordAsync(string subjectId, string reason, string recordedBy, CancellationToken cancellationToken = default)`
   - `ValueTask<Either<EncinaError, Option<DataSubjectStatusReadModel>>> GetStatusAsync(string subjectId, CancellationToken cancellationToken = default)`
9. **Create** `Services/DefaultDataSubjectStatusService.cs` — `internal sealed`; ctor `(IAggregateRepository<DataSubjectStatusAggregate>, IReadModelRepository<DataSubjectStatusReadModel>, TimeProvider, IRequestContextAccessor, IOptions<DataSubjectRightsOptions>, ILogger<DefaultDataSubjectStatusService>, ITenantProvider? tenantProvider = null)`; load-or-open by `DataSubjectStatusId.For(tenant, subjectId)`; `InvalidOperationException` from the aggregate maps to `DSRErrors.InvalidSubjectStatusTransition`
10. **Modify** `DSRErrors.cs` — `dsr.subject_status_not_found`, `dsr.invalid_subject_status_transition`, `dsr.invalid_subject_status_input` (blank requester id, empty prohibited rights)
11. **Modify** `PublicAPI.Unshipped.txt` (delegate to `mechanical-fixer`)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Issue #1197, Phase 4. A data subject needs one record (independent of any DSR request or consent) that can state
a prohibition expressed in life (Ley 41/2002 art. 18.4, LOPDGDD art. 3), the persons authorised to request on the
subject's behalf, and the subject's death. It is a new Marten event-sourced aggregate in
src/Encina.Compliance.DataSubjectRights/, following DSRRequestAggregate, DSRRequestProjection and DefaultDSRService.
The DSR tenant-scoping fix (prerequisite bug, see plan Design Choice 6) has added the ambient-tenant helper and
DataSubjectRightsOptions.RequireTenantContext; use them.

TASK:
1. Model: AuthorisedRequesterBasis (explicit values 1-6), AuthorisedRequester, DataSubjectAccessProhibition.
2. Events/DataSubjectStatusEvents.cs: DataSubjectStatusOpened, DataSubjectAccessPreferencesRecorded
   (IRepresentedAct), DataSubjectDeceasedRecorded, DataSubjectDeceasedRecordRevoked; all INotification and all
   carrying TenantId and ModuleId.
3. DataSubjectStatusAggregate with Open, RecordAccessPreferences, RecordDeceased, RevokeDeceasedRecord and Apply;
   only state-transition guards (no second death record, no revocation without one).
4. DataSubjectStatusId.For(tenantId, subjectId): RFC 9562 version-5 UUID over "{tenantId ?? "-"}:{subjectId}".
5. DataSubjectStatusReadModel + DataSubjectStatusProjection (creator on DataSubjectStatusOpened).
6. IDataSubjectStatusService + internal DefaultDataSubjectStatusService: load the stream by deterministic id; when
   absent, Open and apply the command in one CreateAsync; when present, apply and SaveAsync. Queries filter by the
   ambient tenant; a missing tenant fails closed when tenancy is registered.
7. DSRErrors: subject_status_not_found, invalid_subject_status_transition, invalid_subject_status_input.

KEY RULES:
- Shape only: nothing refuses a request because of a prohibition or a death record (post-1.0, REQ-011).
- Time comes from TimeProvider; Either<EncinaError, T> on every service method; CancellationToken everywhere.
- Subject, requester and representative ids never reach logs, activity tags, metric tags or error messages.
- EncinaError.Message never logged; exceptions logged through ForLogging().
- XML docs with legal references; zero warnings; CRAP <= 10.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Aggregates/DSRRequestAggregate.cs
- src/Encina.Compliance.DataSubjectRights/Projections/DSRRequestProjection.cs
- src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs (tenant pattern, lines 39-128)
- src/Encina.Marten/Projections/IProjection.cs, IReadModelRepository.cs
```

</details>

---

### Phase 5: Configuration & DI

> **Goal**: The status service and its Marten pieces register with the existing DSR registrations.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs:81-` — `services.TryAddScoped<IDataSubjectStatusService, DefaultDataSubjectStatusService>();` and the XML list of registrations
2. **Modify** `src/Encina.Compliance.DataSubjectRights/DSRMartenExtensions.cs` — `AddDSRRequestAggregates` also calls `services.AddAggregateRepository<DataSubjectStatusAggregate>()` and `services.AddProjection<DataSubjectStatusProjection, DataSubjectStatusReadModel>()`; update XML docs
3. **Test (Phase 8)** a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes` and resolves `IDataSubjectStatusService` (registration completeness, `AGENTS.md` §3)
4. No new options class: `DataSubjectRightsOptions.RequireTenantContext` comes from the prerequisite bug; `ConsentOptions` is unchanged

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
Issue #1197, Phase 5. DefaultDataSubjectStatusService, DataSubjectStatusAggregate and DataSubjectStatusProjection
exist (Phase 4). DSR registrations live in src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs
(AddEncinaDataSubjectRights, TryAdd*) and DSRMartenExtensions.cs (AddDSRRequestAggregates registers the aggregate
repository and the inline projection).

TASK:
1. Register IDataSubjectStatusService -> DefaultDataSubjectStatusService (scoped, TryAdd) in AddEncinaDataSubjectRights.
2. Register the status aggregate repository and projection in AddDSRRequestAggregates.
3. Update both XML doc lists of registrations.
4. Add a DI test (tests/Encina.UnitTests/Compliance/DataSubjectRights/ServiceCollectionExtensionsTests.cs) that
   builds with ValidateOnBuild = true and ValidateScopes = true and resolves IDataSubjectStatusService.

KEY RULES:
- Every dependency the service resolves is registered by these two methods (or by AddEncinaMarten).
- TryAdd so an application can replace the service.
- Zero warnings.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs
- src/Encina.Compliance.DataSubjectRights/DSRMartenExtensions.cs
- tests/Encina.UnitTests/Compliance/DataSubjectRights/ServiceCollectionExtensionsTests.cs
```

</details>

---

### Phase 6: Cross-Cutting Integration

> **Goal**: Tenant, audit and validation integrations from the matrix.

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-tenancy (✅)**:
   - Consent: the representation rides on the existing tenant-scoped commands; nothing new
   - DSR requests: `SubmitRequestAsync` uses the ambient-tenant resolution added by the prerequisite bug
   - Status record: `DataSubjectStatusId.For(tenant, subject)`; every read filters `DataSubjectStatusReadModel.TenantId` by the ambient tenant; a stream loaded by id with another tenant is "not found"; tenancy off → tenant `null` key `"-"`, no configuration needed
   - If Design Choice 6 is decided as B, this phase also adds the helper's contract: `private string? CurrentTenantId`, `IsTenantContextRequired => _options.RequireTenantContext ?? _isMultiTenantApplication`, `TryResolveTenantScope(operation, out tenantId, out error)`, `TenantsMatch(a, b)`, the `Encina.Tenancy` project reference and `RequireTenantContext` on `DataSubjectRightsOptions` (copy `DefaultConsentService.cs:109-128`)
2. **Audit trail (✅)**: the events are the audit trail (ADR-019); `GetConsentHistoryAsync` returns the representation on each act. No `IOperationAuditStore` call is added: SPEC-002 asks for the acting-for data inside the consent and DSR events, which this plan delivers
3. **Validation (✅)**: structural validation of `DataSubjectRepresentation`, `AuthorisedRequester` and `DataSubjectAccessProhibition` at the service boundary, returning `Left`; no new options to validate
4. **Module isolation (❌)**: `ModuleId` is carried on the new events exactly like the sibling events; no new scoping

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
Issue #1197, Phase 6. The representation shape (Phases 1-3) and the status record (Phase 4) exist. The DSR
tenant-scoping bug fix has (or, under Design Choice 6 option B, this phase adds) an ambient-tenant helper in the
DSR package copied from DefaultConsentService (#1315).

TASK:
1. Make DefaultDataSubjectStatusService tenant-safe: deterministic id from (ambient tenant, subject); every read
   filtered by TenantId; cross-tenant stream reported as not found; fail closed when tenancy is registered and no
   ambient tenant exists; explicit RequireTenantContext = false logged once.
2. Confirm SubmitRequestAsync stores the ambient tenant when the tenantId argument is null.
3. Structural validation at the service boundary for representation, requesters and prohibition (Left, never throw).
4. Keep ModuleId on every new event and read model, with no new scoping.

KEY RULES:
- Fail closed on missing tenant context (SPEC-002 DEC-006); opt-out explicit and logged.
- No rule acts on representation, prohibition or death (shape only).
- No tenant id in metric tags (cardinality); tenant id only as an activity attribute (Phase 7).

REFERENCE FILES:
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs (lines 39-128, 153-156, 198-215)
- src/Encina.Compliance.Consent/ConsentOptions.cs (RequireTenantContext, line 148)
- artifacts/issues/plan-1197-dsr-tenant-scoping.md
```

</details>

---

### Phase 7: Observability

> **Goal**: Trace and count represented acts and status changes without exposing any identifier.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.Compliance.Consent/Diagnostics/ConsentDiagnostics.cs`:
   - `internal static Activity? StartConsentCommand(string operation)` → span `Consent.Command` with `consent.operation`
   - tags `consent.represented` (bool), `consent.representation_authority` (enum name), `consent.tenant_id` (activity attribute only)
   - counter `consent.represented_acts.total` with tags `consent.operation`, `consent.representation_authority`
   - called by the four commands changed in Phase 2
2. **Create** `src/Encina.Compliance.Consent/Diagnostics/ConsentRepresentationLogMessages.cs` — `internal static partial class` with `[LoggerMessage]`:
   - `8269` `ConsentActRecordedByRepresentative(string operation, string authority)` — Information
   - `8270` `ConsentRepresentationInvalid(string operation, string reasonCode)` — Warning
3. **Modify** `src/Encina.Compliance.DataSubjectRights/Diagnostics/DataSubjectRightsDiagnostics.cs`:
   - tags `dsr.represented` (bool), `dsr.representation_authority`, `dsr.subject_status_change` (`access_preferences`, `deceased`, `deceased_revoked`), `dsr.tenant_id` (activity only; reuse #814's constant if it landed)
   - counter `dsr.requests.represented.total` (tags `dsr.right_type`, `dsr.representation_authority`)
   - counter `dsr.subject_status.changes.total` (tag `dsr.subject_status_change`)
   - `SubmitRequestAsync` starts `StartAggregateCommand("Submit", id)` (`DataSubjectRightsDiagnostics.cs:164`) if #1937 has not wired it, and sets the representation tags
   - the status service uses `StartAggregateCommand("RecordAccessPreferences" | "RecordDeceased" | "RevokeDeceasedRecord")` with no request id
4. **Modify** `src/Encina.Compliance.DataSubjectRights/Diagnostics/DSRLogMessages.cs` — `[LoggerMessage]`:
   - `8313` `DSRRequestSubmittedByRepresentative(string rightType, string authority)` — Information
   - `8314` `DSRRepresentationInvalid(string reasonCode)` — Warning
   - `8315` `DataSubjectStatusChanged(string change)` — Information
   - `8316` `DataSubjectStatusTransitionRejected(string change)` — Warning
5. No new EventId range: Consent stays in `ComplianceConsent` (8200-8299), DSR in `ComplianceDSR` (8300-8349); both assemblies are already mapped in `EncinaEventIdAllocationTests.cs:63,66`
6. XML docs naming the ranges (`/// Event IDs: 8269-8270 (see EventIdRanges.ComplianceConsent)`, `/// Event IDs: 8313-8316 (see EventIdRanges.ComplianceDSR)`)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
Issue #1197, Phase 7. SPEC-002 REQ-062 (AC-044): the main operations emit an activity and a metric with the tenant
as an attribute, logs use [LoggerMessage] with EventIds in a registered range, and no tag or log carries a payload
or a direct identifier of a data subject. ConsentDiagnostics today only has a Consent.Check span;
DataSubjectRightsDiagnostics has StartAggregateCommand (line 164) that #1937 wires for the lifecycle commands.
#814 used DSR EventIds 8311-8312; #1937 keeps 8304-8309 and 8317-8319.

TASK:
1. ConsentDiagnostics: StartConsentCommand(operation) span "Consent.Command"; tags consent.represented,
   consent.representation_authority, consent.tenant_id (activity only); counter consent.represented_acts.total.
2. ConsentRepresentationLogMessages ([LoggerMessage] source generator): EventIds 8269 and 8270.
3. DataSubjectRightsDiagnostics: representation and status-change tags; counters dsr.requests.represented.total and
   dsr.subject_status.changes.total; Submit and status commands start StartAggregateCommand.
4. DSRLogMessages: EventIds 8313-8316 ([LoggerMessage]).
5. Run the architecture tests (EncinaEventIdAllocationTests, EventIdUniquenessRule).

KEY RULES:
- Never tag or log subject id, representative id, requester id, evidence reference or free text.
- Tenant id is an activity attribute, never a metric tag.
- EventIds packed, inside the package range, no new range unless a collision with #1937 forces one.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Diagnostics/ConsentDiagnostics.cs
- src/Encina.Compliance.Consent/Diagnostics/ConsentLogMessages.cs
- src/Encina.Compliance.DataSubjectRights/Diagnostics/DataSubjectRightsDiagnostics.cs
- src/Encina.Compliance.DataSubjectRights/Diagnostics/DSRLogMessages.cs
- src/Encina/Diagnostics/EventIdRanges.cs (lines 313, 316)
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 8: Testing

> **Goal**: Every flag reaches its target; the shape round-trips through Marten; two tenants stay apart.

<details>
<summary><strong>Tasks</strong></summary>

1. **Coverage manifests** — add per-file `targets` and one-sentence `justifications` in `.github/coverage-manifest/Encina.Compliance.GDPR.json`, `Encina.Compliance.Consent.json` and `Encina.Compliance.DataSubjectRights.json` for every new or touched file (`mechanical-fixer`); run `--check-justifications`. The DSR manifest has no `integration` target today: add one for the status service and projection
2. **Unit** (`tests/Encina.UnitTests/Compliance/`):
   - `GDPR/DataSubjectRepresentationTests.cs` — validation branches, `ToString()` omits the ids
   - `Consent/Aggregates/ConsentAggregateTests.cs`, `Consent/Projections/ConsentProjectionTests.cs` — each act with and without representation; renew/reconsent replace; reconsent clears withdrawal representation
   - `Consent/Services/DefaultConsentServiceTests.cs` — invalid representation returns `consent.invalid_representation` and never calls the repository
   - `DataSubjectRights/Aggregates/DSRRequestAggregateTests.cs`, `Projections/DSRRequestProjectionTests.cs`, `Services/DefaultDSRServiceTests.cs` — same for submit
   - `DataSubjectRights/Aggregates/DataSubjectStatusAggregateTests.cs`, `Projections/DataSubjectStatusProjectionTests.cs`, `Services/DefaultDataSubjectStatusServiceTests.cs` — open-or-load, transitions, tenant mismatch, fail-closed tenant, `DataSubjectStatusId` determinism
   - Diagnostics: `ActivityListener`/`MeterListener` tests that each operation emits its span with the tenant attribute and its counter; extend `ConsentPiiLeakTests.cs` and `DataSubjectRightsDiagnosticsSubjectIdLeakTests.cs` so no tag or log carries subject, representative or requester ids
3. **Guard** (`tests/Encina.GuardTests/Compliance/`): public aggregate factories and commands of `DataSubjectStatusAggregate`, the new record constructors' required strings, `DefaultDataSubjectStatusService` constructor
4. **Contract** (`tests/Encina.ContractTests/Compliance/`): `IRepresentedActContractTests.cs` — for every subject-side act produced through its aggregate command (five consent/DSR acts plus the preferences event), `Representation` equals the value passed and the projected read model returns it; extend `IConsentServiceContractTests.cs` and `IDSRServiceContractTests.cs`
5. **Property** (`tests/Encina.PropertyTests/Compliance/`): `RepresentationRoundTripPropertyTests.cs` — for any generated `DataSubjectRepresentation` and act sequence, aggregate state and projected read model agree; `DataSubjectStatusPropertyTests.cs` — `DataSubjectStatusId.For` is deterministic and differs across tenants for the same subject
6. **Integration** (`tests/Encina.IntegrationTests/Compliance/`), `[Collection(MartenCollection.Name)]`, `[Trait("Category", "Integration")]`, `[Trait("Database", "PostgreSQL")]`:
   - `Consent/ConsentRepresentationMartenIntegrationTests.cs` — grant/withdraw/renew/reconsent with representation through `IAggregateRepository<ConsentAggregate>`, reload, read the inline projection
   - `DataSubjectRights/DSRRepresentationMartenIntegrationTests.cs` — submit with representation round-trips; a stream written with the previous `DSRRequestSubmitted` JSON (no `Representation` member) loads with `null`
   - `DataSubjectRights/DataSubjectStatusMartenIntegrationTests.cs` — preferences, death and revocation round-trip; two tenants with the same subject id get two streams and never read each other's status (AC-043)
7. **Rules skeleton (S15)** — `tests/Encina.UnitTests/Compliance/DataSubjectRights/Representation/RepresentationRulesSkeleton.cs` (compile-only static class: `ShouldRefuseDeceasedSubjectRequest(DSRRequestReadModel, DataSubjectStatusReadModel)`, `IsRepresentationExpired(DataSubjectRepresentation, DateTimeOffset)`) and `RepresentationRulesSkeletonTests.cs` with one test skipped with the SPEC-002 REQ-011 reason (Design Choice 5); #1227 reuses it
8. **Load / Benchmark** — update `tests/Encina.LoadTests/Compliance/DataSubjectRights/DataSubjectRights.md`, `tests/Encina.LoadTests/Compliance/Consent/ConsentValidationLoadTests.md`, `tests/Encina.BenchmarkTests/Encina.Benchmarks/Compliance/DataSubjectRights/DataSubjectRights.md` and `.../Consent/ConsentBenchmarks.md` with the representation and status paths: not concurrent hot paths (one append per act), adequate coverage from unit, contract, property and integration tests
9. Measure each flag (`dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests`, then `coverage-report.cs`) and the local CRAP table for changed methods

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
CONTEXT:
Issue #1197, Phases 1-7 are implemented: DataSubjectRepresentation in Encina.Compliance.GDPR; representation on
ConsentGranted/Withdrawn/Renewed/ReconsentProvided and DSRRequestSubmitted; DataSubjectStatusAggregate with its
projection and service; diagnostics with EventIds 8269-8270 and 8313-8316. Tests follow AGENTS.md §9: per-flag
coverage from .github/coverage-manifest/{Package}.json, Shouldly via Encina.Testing.Shouldly, real code executed
(no reflection-only tests), Marten integration tests through MartenFixture.

TASK:
1. Add per-file targets and justifications to the three coverage manifests; add an integration target to the DSR
   manifest.
2. Unit, guard, contract (IRepresentedAct across both modules), property (round-trip, deterministic status id)
   and Marten integration tests as listed in the plan's Phase 8 tasks, including the two-tenant status test and the
   old-JSON deserialization test.
3. The S15 rules skeleton (compile-only) with one skipped test whose reason cites SPEC-002 REQ-011.
4. Update the four load/benchmark justification .md files.
5. Measure every flag and the CRAP of changed methods; report the per-file table against the targets.

KEY RULES:
- [Collection(MartenCollection.Name)], [Trait("Category", "Integration")], [Trait("Database", "PostgreSQL")];
  never a per-class fixture; never dispose the fixture.
- No Thread.Sleep, no FluentAssertions, deterministic data via TimeProvider fakes.
- Leak tests: no subject, representative or requester id in any tag or log.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs
- tests/Encina.UnitTests/Compliance/DataSubjectRights/DataSubjectRightsDiagnosticsSubjectIdLeakTests.cs
- tests/Encina.UnitTests/Compliance/Consent/ConsentPiiLeakTests.cs
- tests/Encina.PropertyTests/Compliance/Consent/ConsentAggregatePropertyTests.cs
- tests/Encina.ContractTests/Compliance/DataSubjectRights/IDSRServiceContractTests.cs
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 9: S15 Shape Hand-off to the Reference Scenario

> **Goal**: #1227 (P-25) can write the shape part of S15 and reuse the rules skeleton without new API.

<details>
<summary><strong>Tasks</strong></summary>

1. Comment on #1227 (orchestrator) with the public members S15 needs: `IConsentService.GrantConsentAsync(..., representation)`, `IDSRService.SubmitRequestAsync(..., representation)`, `IDataSubjectStatusService.RecordAccessPreferencesAsync/RecordDeceasedAsync/GetStatusAsync`, the read-model properties, and the skeleton's path
2. Verify the S15 shape path compiles in a scratch test: a guardian's request for a minor stored as acting for the minor (`RepresentationAuthority.ParentalAuthority`), a deceased patient's prohibition stored with the subject
3. No production change in this phase

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
CONTEXT:
Issue #1197 delivers the shape S15 needs (SPEC-002 §4.3, line 218): "a guardian's request for a minor is stored
as acting for the minor, and a deceased patient's prohibition is stored with the subject (shape)". Issue #1227
(P-25) builds the reference scenario and keeps the rules part of S15 as a compiling, skipped skeleton (AC-038).

TASK:
1. Write a scratch unit test (deleted before commit, or kept as part of Phase 8 unit tests) that performs the S15
   shape steps through the public services only.
2. Draft the hand-off comment for #1227 listing the public members used and the skeleton path; the orchestrator
   posts it.

KEY RULES:
- Public API only; no InternalsVisibleTo shortcut.
- Workers never comment on issues: write the comment text into the report.

REFERENCE FILES:
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (lines 218, 268, 462, 489)
- tests/Encina.UnitTests/Compliance/DataSubjectRights/Representation/RepresentationRulesSkeleton.cs
```

</details>

---

### Phase 10: Documentation & Finalization

> **Goal**: Documented, tracked and green.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML docs** on every new or changed public API (`<summary>`, `<remarks>` with legal references, `<param>`, `<returns>`, `<example>` for the services)
2. **Changelog**: `changelog.d/1197-data-subject-representation.added.md` (representation shape, status record, telemetry) and `changelog.d/1197-data-subject-representation.changed.md` (changed event constructors and service signatures in Consent and DSR) — `mechanical-fixer`
3. **ADR**: `docs/architecture/adr/0NN-data-subject-representation-shape.md` (next free number at implementation time) — shared type placement, acted-for implied by the subject, status aggregate, rules post-1.0 — `docs-writer`
4. **Package READMEs**: `src/Encina.Compliance.Consent/README.md`, `src/Encina.Compliance.DataSubjectRights/README.md`, `src/Encina.Compliance.GDPR/README.md` — `docs-writer`
5. **Feature docs**: `docs/features/consent-management.md`, `docs/features/data-subject-rights.md` (representation, status record, "rules are post-1.0") — `docs-writer`
6. **docs/INVENTORY.md**: new files
7. **ROADMAP.md**: only if the maintainer moves #1197 to v0.15.0 (Design Choice 7)
8. **PublicAPI**: `PublicAPI.Unshipped.txt` of the three packages complete (RS0016/RS0017 clean)
9. **Release notes**: `docs/releases/v0.15.0/` or `v0.17.0/` per the milestone decided
10. **Build**: `dotnet build Encina.slnx --configuration Release` → 0 errors, 0 warnings
11. **Tests**: `dotnet test Encina.slnx --configuration Release` → all pass; every flag reaches its target in the three manifests; CRAP ≤ 10 on changed methods
12. **PR**: records the ADR-018 evaluation (matrix below), `Fixes #1197`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
CONTEXT:
Issue #1197, final phase. Code and tests are complete. Documentation follows the encina-docs skill (Diátaxis, real
API names, no hand-typed coverage figures, links to ADRs and SPEC-002).

TASK:
1. Complete XML docs on every new/changed public API.
2. Changelog fragments 1197-data-subject-representation.added.md and .changed.md (delegate to mechanical-fixer).
3. ADR for the representation shape (delegate to docs-writer).
4. READMEs of Encina.Compliance.Consent, Encina.Compliance.DataSubjectRights, Encina.Compliance.GDPR and the two
   feature pages (delegate to docs-writer).
5. docs/INVENTORY.md, PublicAPI.Unshipped.txt, release notes for the decided milestone.
6. Release build with zero warnings; full test run; per-flag coverage and CRAP table in the report.

KEY RULES:
- Never edit CHANGELOG.md [Unreleased] by hand.
- Coverage figures in docs only through covref markers.
- English only; no AI attribution anywhere.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
- docs/architecture/adr/019-compliance-event-sourcing-marten.md
- docs/features/consent-management.md, docs/features/data-subject-rights.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Shape it requires in this plan |
|--------|-----------|-------------------------------|
| LOPDGDD (LO 3/2018) art. 3.1 | Persons linked to a deceased by family or de facto relation, and heirs, may request access, rectification or erasure, unless the deceased expressly prohibited it or a law says otherwise | `RepresentationAuthority.PersonLinkedToDeceased`; `DataSubjectAccessProhibition.ProhibitedRights` and `AppliesTo`; `AuthorisedRequesterBasis.FamilyOrDeFactoRelation`, `Heir` |
| LOPDGDD art. 3.2 | Persons or institutions designated by the deceased; executor | `AuthorisedRequesterBasis.DesignatedPerson`, `DesignatedInstitution`, `ExecutorOfWill` |
| LOPDGDD art. 3.3 | Deceased minors and persons with disabilities: legal representatives, Ministerio Fiscal, support persons | `AuthorisedRequesterBasis.LegalRepresentative` |
| LOPDGDD art. 7 / GDPR Art. 8 | Age of digital consent (14 in Spain) | Out of scope here (#1255, P-52), which reuses `RepresentationAuthority.ParentalAuthority` |
| Ley 41/2002 art. 9.3 | Consent by representation (incapacity, minors under 16 in health care) | `ParentalAuthority`, `Guardian`, `LegalRepresentative` on consent acts |
| Ley 41/2002 art. 18.2 | Access to the clinical record by duly accredited representation | `DataSubjectRepresentation.AuthorityReference`, `VoluntaryProxy` on DSR requests |
| Ley 41/2002 art. 18.4 | Relatives' access to a deceased patient's record, unless the patient expressly prohibited it | Prohibition recorded in life on the status record; `DataSubjectDeceasedRecorded` |
| EHDS, Regulation (EU) 2025/327 | Proxy services for natural persons to act on behalf of others | `VoluntaryProxy`, `ValidUntilUtc` |
| GDPR Art. 5(1)(c), Art. 5(2) | Data minimisation; accountability | Identifier references only; events as the audit trail |
| SPEC-002 REQ-011, AC-011, S15, REQ-038, AC-038 | Shape before 1.0, rules after, skeleton compiles | Phases 1-4, 8 (skeleton), 9 |
| SPEC-002 REQ-061/AC-043, REQ-062/AC-044, DEC-006, DEC-008 (a), DEC-009 | Tenant-aware, instrumented, fail-closed, Marten-only | Phases 6-8 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `AggregateBase`, `RaiseEvent`, `Apply` | `src/Encina.DomainModeling/` | `DataSubjectStatusAggregate` |
| `IAggregateRepository<T>`, `AddAggregateRepository<T>()` | `src/Encina.Marten/` | Status aggregate persistence |
| `IProjection`, `IProjectionCreator`, `IProjectionHandler`, `IReadModelRepository<T>` | `src/Encina.Marten/Projections/IProjection.cs:83,112`, `IReadModelRepository.cs:43` | Status projection and reads |
| Consent aggregate, projection, service | `src/Encina.Compliance.Consent/` | Representation on four acts |
| DSR aggregate, projection, service | `src/Encina.Compliance.DataSubjectRights/` | Representation on submit; host of the status record |
| Tenant-scope pattern (#1315) | `src/Encina.Compliance.Consent/Services/DefaultConsentService.cs:39-128`; `ConsentOptions.cs:148` | Copied for DSR by the prerequisite bug and used by the status service |
| `DataSubjectRight` | `src/Encina.Compliance.DataSubjectRights/Model/DataSubjectRight.cs` | Prohibited rights, requester rights |
| `StartAggregateCommand` | `src/Encina.Compliance.DataSubjectRights/Diagnostics/DataSubjectRightsDiagnostics.cs:164` | Submit and status command spans |
| `ConsentDiagnostics` | `src/Encina.Compliance.Consent/Diagnostics/ConsentDiagnostics.cs` | New `Consent.Command` span and counter |
| `MartenFixture`, `MartenCollection` | `tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/` | Integration tests |
| Leak tests | `tests/Encina.UnitTests/Compliance/Consent/ConsentPiiLeakTests.cs`, `.../DataSubjectRights/DataSubjectRightsDiagnosticsSubjectIdLeakTests.cs` | Extended to representative and requester ids |
| `EncinaEventIdAllocationTests` | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs:63,66` | Both assemblies already mapped |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.Consent` | 8200-8299 (`EventIdRanges.ComplianceConsent`, `EventIdRanges.cs:313`) | Used: 8200-8207, 8230-8232, 8240-8243, 8250, 8260-8268 (`LoggerMessage.Define`). This plan takes **8269-8270** with the `[LoggerMessage]` source generator in a new partial class |
| `Encina.Compliance.DataSubjectRights` | 8300-8349 (`EventIdRanges.ComplianceDSR`, `EventIdRanges.cs:316`) | Used: 8300-8303, 8310, 8320-8349; #814 takes 8311-8312. This plan takes **8313-8316**. Left for #1937: 8304-8309, 8317-8319 (nine). If more are needed, register a second DSR range from the free 5450-6999 block and map it in `AssemblyRanges` |
| `Encina.Compliance.GDPR` | 8100-8199 | Not used: the shared types log nothing |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Shared model (Phase 1) | 4 new, 2 modified | Three types and an interface in GDPR; Consent csproj |
| Consent shape (Phase 2) | 6 modified | Events, aggregate, read model, projection, service interface and implementation, errors |
| DSR shape (Phase 3) | 6 modified | Same set for the submit path |
| Status record (Phase 4) | 10 new, 1 modified | Three model types, events, aggregate, id helper, read model, projection, service interface and implementation; errors |
| DI (Phase 5) | 2 modified | Registration and Marten extensions |
| Observability (Phase 7) | 1 new, 3 modified | Consent log messages; two diagnostics classes; DSR log messages |
| Tests (Phase 8) | ~14 new, ~12 modified | Unit, guard, contract, property, integration, skeleton; four justification `.md` updated |
| Documentation (Phase 10) | ~2 new, ~7 modified | Changelog fragments, ADR, three READMEs, two feature pages, inventory, PublicAPI ×3 |
| **Total** | **~31 new, ~39 modified** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #1197 — data-subject representation (guardians, proxies) and deceased status in the
Consent and DSR records: persisted shape only (SPEC-002 REQ-011, AC-011, S15, P-09).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, nullable enabled.
- Railway Oriented Programming: Either<EncinaError, T> on every service method; TimeProvider for time.
- Consent and DSR are event-sourced on Marten only (SPEC-002 DEC-008 (a), ADR-019); integration tests use
  MartenFixture through [Collection(MartenCollection.Name)].
- Prerequisites: #814 merged (DSR rejection reasons, same files), the DSR tenant-scoping bug fixed
  (artifacts/issues/plan-1197-dsr-tenant-scoping.md), unless the maintainer decided otherwise.

IMPLEMENTATION OVERVIEW:
Phase 1: Encina.Compliance.GDPR gains DataSubjectRepresentation, RepresentationAuthority (1-5),
         DataSubjectRepresentationValidation, IRepresentedAct; Consent references GDPR.
Phase 2: Representation on ConsentGranted/Withdrawn/Renewed/ReconsentProvided, aggregate, read model, projection,
         IConsentService (four commands), consent.invalid_representation.
Phase 3: Representation on DSRRequestSubmitted, aggregate Submit, read model, projection, SubmitRequestAsync,
         dsr.invalid_representation.
Phase 4: DataSubjectStatusAggregate (Opened, AccessPreferencesRecorded, DeceasedRecorded, DeceasedRecordRevoked),
         deterministic v5 stream id per (tenant, subject), read model, projection, IDataSubjectStatusService.
Phase 5: DI in AddEncinaDataSubjectRights and AddDSRRequestAggregates; ValidateOnBuild test.
Phase 6: Tenant scoping (fail closed), audit via events, structural validation; ModuleId carried.
Phase 7: Consent.Command span and consent.represented_acts.total; dsr.requests.represented.total and
         dsr.subject_status.changes.total; EventIds 8269-8270 (Consent) and 8313-8316 (DSR).
Phase 8: Unit, guard, contract (IRepresentedAct), property (round-trip, id determinism), Marten integration
         (round-trip, old JSON, two tenants); S15 rules skeleton compiled and skipped with a REQ-011 reason;
         load/benchmark justifications updated.
Phase 9: S15 hand-off to #1227.
Phase 10: XML docs, changelog fragments, ADR, READMEs, feature docs, INVENTORY, PublicAPI, release notes.

KEY PATTERNS:
- Shape only: nothing refuses or allows anything because of representation, prohibition or death.
- Representation is one nullable value; acted-for is the record's subject id.
- Only subject-side acts carry representation; controller-side events unchanged.
- Never log or tag subject, representative or requester ids, evidence references or free text; tenant id only as
  an activity attribute.
- Aggregates guard state transitions with InvalidOperationException; services map them to Left.
- [LoggerMessage] source generator for new log messages; EventIds inside the package ranges.
- Per-flag coverage targets with justifications; CRAP <= 10 on changed methods; zero warnings.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ (Events/ConsentEvents.cs, Aggregates/ConsentAggregate.cs,
  ReadModels/ConsentProjection.cs, Services/DefaultConsentService.cs)
- src/Encina.Compliance.DataSubjectRights/ (Events/DSRRequestEvents.cs, Aggregates/DSRRequestAggregate.cs,
  Projections/DSRRequestProjection.cs, Services/DefaultDSRService.cs, DSRMartenExtensions.cs)
- src/Encina.Compliance.GDPR/Model/
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs
- docs/plans/dsar-rejection-reasons-implementation-plan-814.md (sequencing)
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (REQ-011, AC-011, S15, REQ-038, AC-038)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | The status record is read rarely (per request handling) and a stale cached death or prohibition would be worse than a query; Consent and DSR read-model caching are unchanged (the DSR cache keys gain the tenant in the prerequisite bug) |
| 2 | OpenTelemetry | ✅ Phase 7 | `Consent.Command` span and `DSR.AggregateCommand` spans with representation and status-change tags and the tenant attribute; counters `consent.represented_acts.total`, `dsr.requests.represented.total`, `dsr.subject_status.changes.total` |
| 3 | Structured Logging | ✅ Phase 7 | `[LoggerMessage]` EventIds 8269-8270 (`ComplianceConsent`) and 8313-8316 (`ComplianceDSR`), no identifiers in messages |
| 4 | Health Checks | ❌ N/A | No new external dependency: the status record uses the Marten store the existing DSR health check already covers (`Health/DataSubjectRightsHealthCheck.cs`) |
| 5 | Validation | ✅ Phase 6 | Structural validation of representation, authorised requesters and prohibition at the service boundary, returning `Left`; no new options |
| 6 | Resilience | ❌ N/A | No call to an external system; Marten calls follow the existing services' error handling |
| 7 | Distributed Locks | ❌ N/A | Concurrent first writes for one subject collide on the deterministic stream id (Marten optimistic concurrency); no shared state outside the stream |
| 8 | Transactions | ❌ N/A | Each command appends to one stream in one `CreateAsync`/`SaveAsync` (opening event and first command together), which Marten commits atomically |
| 9 | Idempotency | ❌ N/A | Not a message entry point; the deterministic stream id prevents duplicate status streams and the aggregate refuses a second death record |
| 10 | Multi-Tenancy | ✅ Phase 6 | Tenant on every new event and read model, tenant-scoped reads, fail closed when tenancy is registered, tenant `null` (key `"-"`) when off; depends on the DSR tenant bug (`artifacts/issues/plan-1197-dsr-tenant-scoping.md`) |
| 11 | Module Isolation | ❌ N/A | SPEC-002 requires no module scoping; `ModuleId` is carried on the new events like the sibling events, and messaging `ModuleId` stays with #747 |
| 12 | Audit Trail | ✅ Phase 6 | The acting-for data is part of the consent and DSR events and of the status events, which are the audit trail under ADR-019 |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Consent on Marten (#777) and DSR on Marten (#778) | ✅ Closed | Aggregates, projections and services exist |
| Consent tenant scoping (#1315) | ✅ Closed | Pattern copied for DSR |
| #814 DSR rejection reasons | Open (plan in PR #1930) | Changes `DSRRequestEvents.cs`, `IDSRService.cs`, `DefaultDSRService.cs`, projection, read model and `DSRLogMessages.cs` (8311-8312); land first (Design Choice 7) |
| DSR tenant scoping bug | **Not yet an issue**: `artifacts/issues/plan-1197-dsr-tenant-scoping.md` | Design Choice 6; may be judged part of #1257 |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| DSR lifecycle telemetry | #1937 (open) | Wires `StartAggregateCommand` on submit; EventId split in Design Choice 7 |
| Consent given for a minor (P-52) | #1255 (open, v0.15.0) | Consumer of `DataSubjectRepresentation`; milestone earlier than #1197 |
| Reference scenario and S15 skeleton (P-25) | #1227 (open) | Reuses Phase 8's skeleton and Phase 9's hand-off |
| Access export with withholding reasons (P-07) | #1195 (open) | Other half of S15 (subjective annotations, third-party data) |
| Retention anchored at death | #1187 (open) | Can subscribe to `DataSubjectDeceasedRecorded` |
| Multi-tenancy of SPEC-002 capabilities | #1257 (open) | Owns the cross-cutting two-tenant suite; includes this record |

---

## Next Steps

1. The maintainer decides Design Choices 1-7 (the orchestrator records them in a "Maintainer Decisions" section)
2. Open the drafted bug `artifacts/issues/plan-1197-dsr-tenant-scoping.md` (or fold it into #1257) and decide the milestone question of Design Choice 7
3. Link this plan from #1197
4. Implement after #814 and the tenant bug, one phase per commit, in a worktree
5. Final PR references `Fixes #1197` and records the ADR-018 evaluation
