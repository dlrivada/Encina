# Implementation Plan: `Encina.Compliance.DataSubjectRights` — Data Subject Request Rules (Deceased-Patient Access Prohibitions and Guardians Acting for Minors)

> **Issue**: [#2700](https://github.com/dlrivada/Encina/issues/2700)
> **Type**: Feature, [Tier B] (SPEC-002 REQ-011 rules part, scenario S15, REQ-038 and AC-038; SPEC-000 DEC-009, #2692)
> **Milestone**: v0.17.0 — Compliance Lifecycle (order #686, #687, this issue, #688; parent EPIC #1186)
> **Complexity**: Medium-High (8 phases, one package, depends on three unmerged features)
> **Estimated Scope**: ~600-800 lines of production code + ~1,400-1,800 lines of tests; three pull requests (see Scope)

---

## Summary

Implement the **rules that act on the REQ-011 fields** persisted by #1197: refuse a relative's request for a deceased patient who prohibited access in life (LOPDGDD art. 3), and check that a guardian, holder of parental authority or proxy acting for a subject holds an authority that is valid at the request time. Both rules fail closed, return a coded `Left` (never the subject's data) and are audited. This turns the "skeleton" of S15 into the real rule and removes the exemption in AC-038.

### What the issue asks and what the code does today

- The issue asks for: a deceased-subject rule (a requester not authorised by the subject, or authorised but covered by an expressed prohibition, gets `Left` with a reasoned code; an authorised requester without a prohibition proceeds); a guardian/proxy rule (accepted only when the authority exists and its validity covers the request time, from `TimeProvider`); fail-closed on missing representation, status or authority data, with an explicit logged opt-out only; every outcome audited and logged without `EncinaError.Message`; the registration `services.AddEncinaDataSubjectRequestRules()` "registered on top of the compliance DSL (#687)"; the S15 rules skeleton in the PracticeManagement scenario (#1227) replaced by the real scenario.
- The issue has **no open design choices** by its own statement; the order #686 then #687 then these rules then #688 and the entry into 1.0 are the maintainer's decision of 2026-10-23 (SPEC-000 DEC-009, #2692). SPEC-002 already carries the superseding text (REQ-011 at `docs/specifications/SPEC-002-eu-regulatory-readiness.md:268`, S15 at `:218`, AC-038 at `:489`).
- **Nothing the rules read exists yet.** #1197 (open, plan `docs/plans/data-subject-representation-implementation-plan-1197.md`) adds `DataSubjectRepresentation`, `RepresentationAuthority`, `IDataSubjectStatusService.GetStatusAsync` returning a `DataSubjectStatusReadModel` (deceased flag, `AuthorisedRequester` list with `AuthorisedRequesterBasis`, `DataSubjectAccessProhibition` with `ProhibitedRights` and `AppliesTo`) and `DSRRequestReadModel.Representation`. #686 (open, plan `docs/plans/compliance-applicability-resolver-implementation-plan-686.md`) adds the core contract `IComplianceApplicability`. #687 (open) has no plan yet.
- Today `DefaultDSRService.SubmitRequestAsync` (`Services/DefaultDSRService.cs:152-196`) creates a `DSRRequestAggregate` and returns its id; it takes neither a representation nor a requester. `DSRRequestAggregate.Deny` (`Aggregates/DSRRequestAggregate.cs:273-`) is allowed from any non-terminal status, including `Received`, and raises `DSRRequestDenied(reason, explanation, ...)`. `DSRRejectionReason` has `RestrictedByLaw` (legal basis text "GDPR Art. 23") and `IdentityNotVerifiable` ("GDPR Art. 12(2) with Art. 11(2)") (`Model/DSRRejectionReasonExtensions.cs`). The #1197 plan's chosen skeleton, `RepresentationRulesSkeleton` in `tests/Encina.UnitTests/Compliance/DataSubjectRights/Representation/`, is what this issue replaces.
- The rules are not an applicability question: the #686 resolver and the #687 DSL decide which capabilities apply to which tenant, region or subject (the 686 plan: compliance is never optional, only territorial non-applicability, traced and audited, may disable a control; designed for Spain and the EU). The plan therefore builds the rules as plain decision objects in the DSR package and uses #686/#687 for the one question they answer: whether the rule capability applies (Design Choice 3).

### Scope

- **Affected package**: `Encina.Compliance.DataSubjectRights` (plus tests, README, feature guide, coverage manifest, changelog fragment, `PublicAPI.Unshipped.txt`); `Encina` core only for the capability name if #686 does not already define an open capability set.
- **Provider category**: none of the AGENTS.md §5 matrices. The rules add no store: they read through #1197's abstractions. Unit tests mock `IAggregateRepository`/services; integration tests run on Marten/PostgreSQL through Testcontainers (AGENTS.md §3).
- **Estimated files**: ~10 new source files, ~7 modified source files, ~13 test files, ~5 documentation files.
- **Pull request split** (exceeds 15 files and 1,500 lines): **PR 1** = Phases 1-2 (rule contract, the two rules, evaluator, options; pure logic with unit, guard, contract and property tests); **PR 2** = Phases 3, 5, 6 (service integration with persisted denial, cross-cutting, observability, EventIds); **PR 3** = Phases 4, 7 (rest) and 8 (applicability and DSL registration once #686 and #687 are merged, S15, documentation).
- **Event IDs**: 9806-9813 in the second DSR range `ComplianceDSRExtensions = (9800, 9849)` that the #1195 plan registers (9800-9805 are #1195's).

### Related open issues that touch the same files

- #1197 (blocking: shape and read services), #686 and #687 (blocking: applicability contract and DSL, GitHub "blocked by" on this issue), #1195 (access withholding; edits `DefaultDSRService`, `DSRRequestEvents.cs`, `AccessRequest`; registers the shared EventId range), #1937 (instrumentation of the same service), #1227 (the reference scenario S15 lives there), #688 (optional alternative resolver, follows).

---

## Design Choices

<details>
<summary><strong>1. A requester who is not on the subject's authorised list — refuse (the issue's rule)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A request acting under `PersonLinkedToDeceased` for a deceased subject is accepted only when the requester id is in `AuthorisedRequesters` (with the right allowed for that requester) and no `AccessProhibition` covers the right and the requester's basis; anything else is refused** | The issue's wording and SPEC-002 REQ-011 ("the requesters authorised and any prohibition the subject expressed"); fail closed; the practice records the entitled persons when it records the death | A relative whom nobody listed is refused until the application records them |
| **B) Also accept a requester who declares a statutory basis (family or de facto relation, heir) when no prohibition covers it, even if not listed** | Closer to LOPDGDD art. 3.1, where persons linked to the deceased need no designation | The basis is a claim; accepting it depends on verifying the kinship (`VerifyIdentityAsync` covers identity, not kinship); the persisted shape has no place for an unverified basis |

### Chosen Option: **A — refuse unless listed** (recommended, pending the maintainer)

Class: Dilemma

### Rationale

- We recommend A because it is what the issue and REQ-011 state and it never discloses to an unverified claimant (AGENTS.md §3, fail closed).
- B wins in the reference application's scenario where a family arrives after a patient's death and nobody recorded them: option A refuses a person who has a statutory right to ask, and B accepts after the practice has verified kinship. If the maintainer chooses B, Phase 2 adds a requester-basis input to the rule (a field on the #1197 representation, `AuthorityReference` plus a basis value) and the rest of the plan is unchanged.

</details>

<details>
<summary><strong>2. How an intake refusal is recorded — persist the request and its denial, then return the coded <code>Left</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `SubmitRequestAsync` evaluates the rules before saving; on refusal it creates the aggregate with `Submit` and `Deny(RestrictedByLaw or IdentityNotVerifiable, explanation)` in one `CreateAsync`, then returns `Left` carrying the rule code and the request id** | The refusal is a durable, projected, auditable record (events), and the one-month reply duty of GDPR Art. 12(4) has a request to hang on; the acceptance "reasoned, audited outcome" is met by events, not by logs | The caller gets a `Left` for a request that exists; the error carries the id |
| **B) Return `Left` without persisting; log and count only** | No record for a refused request | Not audited durably; no request to answer within the deadline; logs are not an audit store (SPEC-002 REQ-062 forbids subject ids in telemetry) |

### Chosen Option: **A — persist submit and denial in one write, then `Left`** (recommended, pending the maintainer)

Class: Dilemma

### Rationale

- We recommend A because the issue demands both a `Left` with a reason code and an audited outcome, and only the event stream gives a durable audit.
- B wins when the maintainer treats a refusal at the door as "no request was received" (nothing stored about a claimant the controller could not verify), which is also a data-minimisation argument; the cost is no durable audit and no reply record.
- The rule code travels in the error (`dsr.rule.*`), the human explanation in `DSRRequestDenied.rejectionExplanation` is a fixed template naming the legal basis (LOPDGDD art. 3) and never the subject's data.

</details>

<details>
<summary><strong>3. How the rules relate to #686 and #687 — plain decision objects in the DSR package, gated by the applicability contract</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IDataSubjectRequestRule` implementations in the DSR package, evaluated by the service; the service asks #686's `IComplianceApplicability` whether the capability "data-subject request rules" applies; `AddEncinaDataSubjectRequestRules()` registers the rules and the capability, and the S15 configuration declares the regulation in the #687 DSL** | Matches what #686/#687 are (applicability and declarative enforcement); the decision logic stays testable without the DSL; no dependency from the rules on an unfinished DSL | The DSL registration step waits for #687's final API (Phase 4) |
| **B) Express the decisions themselves as DSL rules** | One mechanism | #687 describes applicability rules (`AppliesWhen(...).Enforces(capability)`), not request decisions over persisted status and time; it would have to grow a decision language |
| **C) Ignore #686/#687 and register the rules unconditionally** | Independent of unmerged work | Contradicts the maintainer's order and SPEC-002 REQ-011 ("built on the resolver and the DSL, verified by S15 with them"); no territorial-scope trace |

### Chosen Option: **A — decision objects, gated by applicability**

Class: Settled
Settled by: issue #2700 Alternatives Considered 1 ("rules live on the #686/#687 engine", maintainer order 2026-10-23, SPEC-000 DEC-009), SPEC-002 REQ-011 (`docs/specifications/SPEC-002-eu-regulatory-readiness.md:268`), the #686 plan (`docs/plans/compliance-applicability-resolver-implementation-plan-686.md`, compliance is never optional and only territorial non-applicability disables a control)

### Rationale

- Per the #686 plan the capability applies by default; a skip needs traced territorial evidence, so the default Spanish deployment always evaluates the rules.
- Until #686 and #687 land, PR 1 and PR 2 call the rules directly (the service holds the evaluator); Phase 4 inserts the applicability question and the DSL declaration. The exact names (`ComplianceCapability`, `AddRegulation`) are fixed by those issues' final contracts.

</details>

<details>
<summary><strong>4. What fails closed — every missing input denies</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A closed matrix: no status record for a request that claims `PersonLinkedToDeceased` denies (`status_missing`); a request without a representation for a subject recorded as deceased denies; a representation with an empty `RepresentativeId`, an undefined authority, or `ValidUntilUtc` not after the request time denies (`authority_invalid` or `authority_expired`); a `Left` from the status service denies and is not retried silently; the only opt-out is `EnforceRequestRules = false`, logged at startup** | AGENTS.md §3 and SPEC-002 DEC-006; no input combination passes by omission | An application with no status records refuses `PersonLinkedToDeceased` requests, which is the point |
| **B) Missing data passes (treated as "no restriction")** | Fewer refusals | A gate that opens on missing context |

### Chosen Option: **A — closed fail-closed matrix**

Class: Settled
Settled by: `AGENTS.md` §3 (compliance and security gates fail closed; errors are never swallowed), issue #2700 Proposed Solution ("Fail closed: missing representation, status or authority data denies"), SPEC-002 DEC-006

### Rationale

- Time comes only from `TimeProvider`; the validity test is `representation.ValidUntilUtc is null || representation.ValidUntilUtc > requestTime`.
- A request by the subject personally (no representation) for a living subject is outside both rules and proceeds.

</details>

<details>
<summary><strong>5. Rejection reason and error codes — map to the existing <code>DSRRejectionReason</code> values</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Deceased-subject refusals map to `DSRRejectionReason.RestrictedByLaw`; invalid or expired authority maps to `IdentityNotVerifiable`; rule codes `dsr.rule.requester_not_authorised`, `dsr.rule.prohibited_by_subject`, `dsr.rule.status_missing`, `dsr.rule.authority_invalid`, `dsr.rule.authority_expired` live in `DSRErrors`** | Reuses the closed enum and its legal-basis text (`DSRRejectionReasonExtensions.LegalBasis`); no new enum member for a persisted shape | The enum text says GDPR Art. 23; the specific article (LOPDGDD art. 3) goes in the explanation template |
| **B) A new `DSRRejectionReason` member per rule** | Exact legal basis per member | Changes a persisted enum that #814 just stabilised and the draft-law switch logic |

### Chosen Option: **A — reuse the existing reasons**

Class: Trivial
Settled by: `src/Encina.Compliance.DataSubjectRights/Model/DSRRejectionReasonExtensions.cs` (the `LegalBasis` mapping and the existing members) and `src/Encina.Compliance.DataSubjectRights/DSRErrors.cs` (code constants with the `dsr.` prefix)

### Rationale

- The refusal text never contains the subject's data, the requester id or the prohibition content.

</details>

---

## Implementation Phases

### Phase 1: Rule Contract and Models

> **Goal**: The vocabulary of a rule decision.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Abstractions/IDataSubjectRequestRule.cs`** (new): `string RuleId { get; }` and `ValueTask<Either<EncinaError, DataSubjectRequestRuleOutcome>> EvaluateAsync(DataSubjectRequestRuleContext context, CancellationToken cancellationToken = default)`.
2. **`Model/DataSubjectRequestRuleContext.cs`** (new `sealed record`): `string SubjectId`, `DataSubjectRight Right`, `DataSubjectRepresentation? Representation`, `DateTimeOffset RequestTimeUtc`, `string? TenantId` (the representation and status types come from #1197).
3. **`Model/DataSubjectRequestRuleOutcome.cs`** (new `sealed record`): `RuleOutcomeKind Kind` (`NotApplicable = 1`, `Accepted = 2`, `Refused = 3`), `string RuleId`, `string? Code`, `DSRRejectionReason? Reason`; static factories.
4. **`DSRErrors.cs`** (modify): the five codes of Design Choice 5 and `RequestRefusedByRule(string ruleId, string code, Guid? requestId)`; messages carry codes only.
5. **`PublicAPI.Unshipped.txt`** (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of data subject request rules in Encina.Compliance.DataSubjectRights (issue #2700).

CONTEXT:
- #1197 has landed: DataSubjectRepresentation, RepresentationAuthority, DataSubjectStatusReadModel and IDataSubjectStatusService exist in Encina.Compliance.GDPR / DataSubjectRights.
- DSRErrors (DSRErrors.cs) holds coded errors with the dsr. prefix; DSRRejectionReason has RestrictedByLaw and IdentityNotVerifiable.

TASK:
Create IDataSubjectRequestRule, DataSubjectRequestRuleContext, DataSubjectRequestRuleOutcome (with RuleOutcomeKind) and the five rule error codes with RequestRefusedByRule, as listed in the plan's Phase 1 Tasks.

KEY RULES:
- Enums have explicit values and no zero member; XML docs on every public member.
- Errors carry codes and the request id only: never the subject id, the requester id or any prohibition content.
- Time is an input (RequestTimeUtc), never read inside a rule.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/DSRErrors.cs
- src/Encina.Compliance.DataSubjectRights/Model/DSRRejectionReasonExtensions.cs
- docs/plans/data-subject-representation-implementation-plan-1197.md
```

</details>

---

### Phase 2: The Two Rules, the Evaluator and Options

> **Goal**: The decision logic, pure and tested before any integration.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Rules/DeceasedSubjectAccessRule.cs`** (new, `internal sealed`, `RuleId = "lopdgdd-art-3"`): applies when the representation's authority is `PersonLinkedToDeceased` or the subject's status is deceased; reads the status through `IDataSubjectStatusService`; the matrix: no status or no representation for a deceased subject refuses (`status_missing`); requester not in `AuthorisedRequesters`, or listed but without the right, refuses (`requester_not_authorised`); an `AccessProhibition` whose `ProhibitedRights` contains the right and whose `AppliesTo` contains the requester's `AuthorisedRequesterBasis` refuses (`prohibited_by_subject`) and always wins over the authorised list; otherwise accepts. Subject not deceased and authority `PersonLinkedToDeceased` refuses (`status_missing`).
2. **`Rules/RepresentationAuthorityRule.cs`** (new, `internal sealed`, `RuleId = "representation-authority"`): applies when a representation is present and its authority is not `PersonLinkedToDeceased`; refuses an empty `RepresentativeId` or undefined authority (`authority_invalid`) and a `ValidUntilUtc` not after `RequestTimeUtc` (`authority_expired`); accepts otherwise.
3. **`Rules/DataSubjectRequestRuleEvaluator.cs`** (new, `internal sealed`, scoped): runs every registered `IDataSubjectRequestRule` in `RuleId` order; the first `Refused` wins; any `Left` is returned as `Left` (fail closed); all `NotApplicable` or `Accepted` accepts.
4. **`DataSubjectRightsOptions.cs`** (modify): `bool EnforceRequestRules { get; set; } = true` with XML docs stating that `false` is an explicit, logged opt-out.
5. **`ServiceCollectionExtensions.cs`** (modify) or **`DataSubjectRequestRulesExtensions.cs`** (new): `AddEncinaDataSubjectRequestRules(this IServiceCollection services)` registers both rules (`TryAddEnumerable`), the evaluator, and everything they resolve (`IDataSubjectStatusService` is registered by `AddEncinaDataSubjectRights`); a DI test builds the provider with `ValidateOnBuild` and `ValidateScopes`.
6. **`PublicAPI.Unshipped.txt`** (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of data subject request rules in Encina.Compliance.DataSubjectRights (issue #2700).

CONTEXT:
- Phase 1 created IDataSubjectRequestRule, the context, the outcome and the error codes.
- IDataSubjectStatusService.GetStatusAsync(subjectId) returns Either<EncinaError, Option<DataSubjectStatusReadModel>> with the deceased flag, AuthorisedRequesters (RequesterId, Basis, Rights) and AccessProhibition (ProhibitedRights, AppliesTo) from #1197.

TASK:
Create DeceasedSubjectAccessRule, RepresentationAuthorityRule and DataSubjectRequestRuleEvaluator, add DataSubjectRightsOptions.EnforceRequestRules (default true) and AddEncinaDataSubjectRequestRules with its DI test, as listed in the plan's Phase 2 Tasks.

KEY RULES:
- Fail closed: missing status, representation or authority data refuses; a Left from the status service is returned as Left.
- A prohibition that covers the right and the requester's basis always wins over the authorised list.
- Time comes from the context's RequestTimeUtc; no clock reads inside a rule.
- Rules are stateless and thread-safe; no caching of status (it is mutable permission data).

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs
- src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs
- docs/plans/data-subject-representation-implementation-plan-1197.md
```

</details>

---

### Phase 3: Service Integration and Persisted Denial

> **Goal**: `SubmitRequestAsync` applies the rules and records a refusal.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Services/DefaultDSRService.cs`** (modify `SubmitRequestAsync`, lines 152-196): after #1197 gives it the `Representation` parameter, build the rule context with `TimeProvider.GetUtcNow()` and the resolved tenant, call the evaluator when `EnforceRequestRules` is true; on `Refused` create the aggregate with `Submit` and `Deny(reason, template explanation)` in a single `CreateAsync` and return `Left(DSRErrors.RequestRefusedByRule(...))` with the new request id; on `Left` from the evaluator return `Left` without creating anything; on accept continue as today. Extract the rule step into a private method to keep CRAP at or below 10.
2. **`Abstractions/IDSRService.cs`**: no new member (the parameter comes from #1197).
3. The explanation template lives in one internal static class (`DSRRuleExplanations`), English, naming the legal basis and the rule id only.
4. **`PublicAPI.Unshipped.txt`** (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of data subject request rules in Encina.Compliance.DataSubjectRights (issue #2700).

CONTEXT:
- DefaultDSRService.SubmitRequestAsync (Services/DefaultDSRService.cs:152-196) creates DSRRequestAggregate.Submit(...) and CreateAsync; DSRRequestAggregate.Deny is allowed from Received.
- Phase 2 created DataSubjectRequestRuleEvaluator and EnforceRequestRules.

TASK:
Evaluate the rules in SubmitRequestAsync; persist submit and denial in one CreateAsync on a refusal and return the coded Left, as listed in the plan's Phase 3 Tasks.

KEY RULES:
- Fail closed: an evaluator Left creates nothing and is returned as Left.
- The denial explanation is a fixed template: no subject data, requester id or prohibition content.
- Keep the existing tenant scope helper and ROP; keep CRAP at or below 10.
- No swallowed errors.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs
- src/Encina.Compliance.DataSubjectRights/Aggregates/DSRRequestAggregate.cs
- src/Encina.Compliance.DataSubjectRights/DSRErrors.cs
```

</details>

---

### Phase 4: Applicability and DSL Registration (blocked by #686 and #687)

> **Goal**: The rules are declared and gated through the compliance engine.

<details>
<summary><strong>Tasks</strong></summary>

1. Define the capability "data subject request rules" with the #686 contract (an open `ComplianceCapability` value or a new member, as the merged #686 decides) and ask `IComplianceApplicability.AppliesAsync` in the service before the evaluator; a skip is logged and traced with its evidence as #686 specifies (EventId 9813).
2. Provide the #687 declaration used by S15: `AddRegulation("LOPDGDD", rule => rule.AppliesWhen(Spain).Enforces(<capability>))`, in the PracticeManagement scenario configuration and in the documentation example.
3. `AddEncinaDataSubjectRequestRules()` also registers the capability with the applicability package when it is present (a soft reference through the core contract, no project reference from DSR to `Encina.Compliance.Applicability`).
4. Re-run the Phase 7 integration tests with the resolver and DSL registered; the optional OPA alternative (#688) is not required.
5. If #687's API cannot express the declaration, stop and report: the plan's Design Choice 3 is then re-opened with the maintainer.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of data subject request rules in Encina.Compliance.DataSubjectRights (issue #2700).

CONTEXT:
- #686 (IComplianceApplicability in core) and #687 (rule-based compliance DSL) are merged. Their final names replace the placeholders in the plan's Phase 4 Tasks.
- The service already evaluates the rules directly (Phase 3).

TASK:
Gate the evaluator with IComplianceApplicability, define the capability, and add the DSL declaration used by S15, as listed in the plan's Phase 4 Tasks.

KEY RULES:
- Compliance is never optional: only traced territorial non-applicability (#686) skips the rules; the default applies them.
- No project reference from DataSubjectRights to Encina.Compliance.Applicability.
- If the DSL cannot express the declaration, stop and report instead of improvising.

REFERENCE FILES:
- docs/plans/compliance-applicability-resolver-implementation-plan-686.md
- src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs
- src/Encina.Compliance.DataSubjectRights/ProcessingRestrictionPipelineBehavior.cs
```

</details>

---

### Phase 5: Cross-Cutting Integration

> **Goal**: Tenant, audit and validation behave like the rest of the module.

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-tenancy**: the rule context carries the resolved tenant; status and authority reads go through #1197's tenant-scoped `IDataSubjectStatusService`; a two-tenant test shows tenant A's prohibition and authorised requesters never apply to tenant B (AC-043).
2. **Audit trail**: the persisted `DSRRequestDenied` (Phase 3) is the audit record of a refusal; an accepted request is audited by the existing `DSRRequestSubmitted`.
3. **Validation**: the request shape is validated before the rules (subject and right already required); the representation's own validity is the guardian rule.
4. Not applicable (see matrix): caching, health checks, resilience, locks, transactions beyond the single create, idempotency, module isolation.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are verifying the cross-cutting integration of data subject request rules in Encina.Compliance.DataSubjectRights (issue #2700).

CONTEXT:
- DefaultDSRService resolves the ambient tenant for its operations; DSR events are the audit trail; #1197's status service is tenant-scoped.

TASK:
Pass the tenant into the rule context and add the two-tenant test of the plan's Phase 7.

KEY RULES:
- No new tenant logic, no new audit store, no caching of rule inputs.
- With tenancy off the fixed default tenant applies.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs
- tests/Encina.IntegrationTests/Compliance/DataSubjectRights/DSRTenantScopingIntegrationTests.cs
```

</details>

---

### Phase 6: Observability

> **Goal**: Instruments and logs for rule outcomes, without identifiers or prohibition content (REQ-062, AC-044).

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina/Diagnostics/EventIdRanges.cs`**: the range `ComplianceDSRExtensions = (9800, 9849)` and its second-range mapping for `Encina.Compliance.DataSubjectRights` in `EncinaEventIdAllocationTests.cs` are registered by the #1195 plan's Phase 6; when this issue lands first it registers them identically (with the `PublicAPI.Unshipped.txt` line).
2. **`Diagnostics/DSRRuleLogMessages.cs`** (new, `internal static partial class`, `[LoggerMessage]`, XML doc "Event IDs: 9806-9813 (see EventIdRanges.ComplianceDSRExtensions)"): 9806 `RequestRefusedRequesterNotAuthorised`, 9807 `RequestRefusedProhibitedBySubject`, 9808 `RequestRefusedAuthorityInvalid` (reason code), 9809 `RequestRefusedStatusMissing`, 9810 `RequestAcceptedByRules` (Debug), 9811 `RuleEvaluationFailed` (error code), 9812 `RequestRulesOptOutActive` (startup warning when `EnforceRequestRules` is false), 9813 `RequestRulesCapabilityNotApplicable` (Phase 4); each carries rule id, right type and code only.
3. **`Diagnostics/DataSubjectRightsDiagnostics.cs`** (modify): counter `dsr.rules.outcomes.total` (tags `dsr.rule`, `dsr.rule_outcome`, `dsr.right_type`); activity `DSR.Rules.Evaluate` with `dsr.rule`, `dsr.rule_outcome` and `encina.tenant_id`. The issue's names (`encina.compliance.dsr_rule_outcome_count`, `encina.compliance.dsr_rule.evaluate`, tag `rule.id`) are replaced by the package's naming (`dsr.*`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of data subject request rules in Encina.Compliance.DataSubjectRights (issue #2700).

CONTEXT:
- DataSubjectRightsDiagnostics owns the ActivitySource, Meter and instruments. The second DSR EventId range 9800-9849 is registered by the #1195 plan; 9800-9805 belong to #1195.

TASK:
Create DSRRuleLogMessages with EventIds 9806-9813, the counter and the activity as listed in the plan's Phase 6 Tasks, and call them from the evaluator and the service.

KEY RULES:
- [LoggerMessage] source generator, packed sequentially 9806-9813 inside the registered range; register the range first if it is not there yet.
- No subject id, requester id, prohibition content, explanation text or EncinaError.Message in any log, tag or metric.
- Run EncinaEventIdAllocationTests.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Diagnostics/DataSubjectRightsDiagnostics.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 7: Testing

> **Goal**: Every flag reaches its manifest target; S15 passes with no skipped rules part.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/Compliance/DataSubjectRights/Rules/`): `DeceasedSubjectAccessRuleTests` (every cell of the matrix: unlisted, listed without the right, prohibition covering right and basis, prohibition not covering the basis, no status, living subject with `PersonLinkedToDeceased`), `RepresentationAuthorityRuleTests` (empty id, undefined authority, expired at the exact instant, valid, no expiry), `DataSubjectRequestRuleEvaluatorTests` (first refusal wins, a `Left` fails), service tests for `SubmitRequestAsync` (accept, refuse with persisted denial, evaluator `Left` creates nothing, opt-out), DI test with `ValidateOnBuild` and `ValidateScopes`; all with `FakeTimeProvider`.
2. **Guard**: public members and constructors of the evaluator, the rules (through the public registration) and the options.
3. **Contract**: `IDataSubjectRequestRule` against both real rules.
4. **Property** (`tests/Encina.PropertyTests/Compliance/DataSubjectRights/`): for any generated status, requester list and right, a prohibition that covers the right and the requester's basis refuses whatever the authorised list says; with no status data the result is always a refusal.
5. **Integration** (`[Collection(MartenCollection.Name)]`, PostgreSQL): S15 rules part with the real aggregates of #1197 — a relative's access request for a deceased patient who prohibited access is refused with a stored `DSRRequestDenied`; an authorised requester without prohibition proceeds; a guardian with valid authority proceeds, an expired one is refused; two-tenant isolation. The skeleton `RepresentationRulesSkeleton` and its skipped test are replaced by the real scenario in #1227's PracticeManagement project (the README maps S15 to REQ-009 and REQ-011), and the AC-038 exemption wording is already removed from the SPEC.
6. **Telemetry/privacy**: in-memory exporter test (AC-044) and a leak test: no subject id, requester id or prohibition content in tags, metrics, logs or the denial explanation.
7. **Load and benchmark**: `.md` justifications (read-only decisions, not concurrent or hot).
8. **Coverage manifest**: per-file targets and justifications for every new or changed file in `.github/coverage-manifest/Encina.Compliance.DataSubjectRights.json` (via `mechanical-fixer`); measure each flag; CRAP gate on `SubmitRequestAsync`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of data subject request rules in Encina.Compliance.DataSubjectRights (issue #2700).

CONTEXT:
- DSR tests live in tests/Encina.{Unit,Guard,Contract,Property,Integration}Tests/Compliance/DataSubjectRights/. Unit tests mock IAggregateRepository (NSubstitute); integration tests use [Collection(MartenCollection.Name)].
- Assertions use Shouldly through Encina.Testing.Shouldly. The S15 skeleton from #1197 is RepresentationRulesSkeleton.

TASK:
Write the tests listed in the plan's Phase 7 Tasks; replace the skipped skeleton by the real scenario.

KEY RULES:
- FakeTimeProvider; test the exact expiry instant; no Thread.Sleep; no swallowed exceptions.
- Property test: a covering prohibition always wins; no status data always refuses.
- Measure per-flag coverage against the manifest targets before reporting.

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/DataSubjectRights/Services/DefaultDSRServiceTests.cs
- tests/Encina.IntegrationTests/Compliance/DataSubjectRights/DSRTenantScopingIntegrationTests.cs
- .github/coverage-manifest/Encina.Compliance.DataSubjectRights.json
```

</details>

---

### Phase 8: Documentation and Finalization

> **Goal**: Users know how to enable the rules and what they refuse; the build is clean.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on every new public API with the registration example.
2. `changelog.d/2700-dsr-request-rules.added.md` (via `mechanical-fixer`).
3. `src/Encina.Compliance.DataSubjectRights/README.md` and the DSR feature guide (via `docs-writer`): the rules, the matrix of refusals, the opt-out, the relation to #686/#687; the LOPDGDD art. 3 text is re-read before it is cited.
4. SPEC-002 REQ-011, S15, REQ-038 and AC-038 already carry the 2026-10-23 text; change them only if the implementation differs.
5. `PublicAPI.Unshipped.txt`; verification: `dotnet build Encina.slnx --configuration Release` (0 warnings), the five DSR test projects, EventId architecture tests, `dotnet run --file .github/scripts/changelog-fragments.cs -- --check`, and the S15 scenario in CI Full.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are finalising data subject request rules in Encina.Compliance.DataSubjectRights (issue #2700).

CONTEXT:
- Documentation pages go to the docs-writer; changelog fragments and PublicAPI lines to the mechanical-fixer.

TASK:
Complete the plan's Phase 8 Tasks and run the verification commands.

KEY RULES:
- No hand-typed coverage figures in documentation (covref markers).
- Do not publish a legal citation before it is checked against the law text.
- Zero build warnings.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/README.md
- docs/specifications/SPEC-002-eu-regulatory-readiness.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Provision | Relevance |
|----------|-----------|-----------|
| LOPDGDD (Organic Law 3/2018) | art. 3 | Persons linked to a deceased person, heirs and designated persons may request access, rectification or erasure unless the deceased prohibited it |
| Ley 41/2002 | art. 18.4 (as cited by the #1197 plan) | Access to the deceased's clinical history |
| GDPR | Art. 12(2), 12(4), 12(6), 23 | Handling requests, reasons for refusal, restrictions by law, identity confirmation |
| SPEC-002 | REQ-011, S15, REQ-038, AC-038, DEC-006, DEC-009, REQ-061, REQ-062 | Requirement, scenario, no exemption, fail closed, tenancy, instrumentation |
| SPEC-000 | DEC-009 (#2692) | Rules enter 1.0, order #686, #687, this issue, #688 |
| ADR-001, ADR-006, ADR-018, ADR-019, ADR-021 | ROP, cross-cutting, Marten, EventIds | Design rules followed |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `DefaultDSRService.SubmitRequestAsync` | `src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs:152-196` | Where the rules run |
| `DSRRequestAggregate.Submit` / `Deny` | `Aggregates/DSRRequestAggregate.cs` | Persisted denial |
| `DSRRejectionReason` and extensions | `Model/DSRRejectionReason.cs`, `Model/DSRRejectionReasonExtensions.cs` | Reason mapping |
| `DSRErrors` | `DSRErrors.cs` | Rule codes |
| `DataSubjectRightsDiagnostics` | `Diagnostics/DataSubjectRightsDiagnostics.cs` | Counter and activity |
| `IDataSubjectStatusService`, `DataSubjectRepresentation` (planned) | #1197 plan | Inputs of the rules |
| `IComplianceApplicability` (planned) | #686 plan | Applicability question (Phase 4) |
| `MartenFixture` and `MartenCollection` | `tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/` | Integration tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.DataSubjectRights` (existing) | 8300-8349 (`EventIdRanges.ComplianceDSR`) | No packed free ids (8304-8309 and 8317-8319 are kept for #1937) |
| #1195 | 9800-9805 in `ComplianceDSRExtensions = (9800, 9849)` | Registers the second range |
| **This plan (#2700)** | **9806-9813** in the same range | Packed after 9805; `DSRRuleLogMessages` |
| Left in the range | 9814-9849 | For #1937 and #1197 overflow |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New source | 10 | Rule contract, context, outcome, two rules, evaluator, registration extension, explanations, log messages, `RuleOutcomeKind` |
| Modified source | 7 | Service, options, errors, diagnostics, registration, `EventIdRanges.cs` (only if first), manifest |
| Tests | 13 | Unit 5, guard 1, contract 1, property 1, integration 2, leak 1, justifications 2 |
| Documentation | 5 | README, feature guide, changelog fragment, PublicAPI, SPEC check |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 library (no backward compatibility, ROP with Either<EncinaError, T>, TimeProvider for time, tenant-scoped reads). Encina.Compliance.DataSubjectRights is event-sourced on Marten (PostgreSQL). Issue #2700 implements the rules that act on #1197's persisted fields: refuse a relative's request for a deceased patient who prohibited access in life (LOPDGDD art. 3) and check that a guardian or proxy holds a valid authority at the request time. Both fail closed and are audited.

IMPLEMENTATION OVERVIEW:
1. Add IDataSubjectRequestRule, its context and outcome, and the rule error codes.
2. Add DeceasedSubjectAccessRule, RepresentationAuthorityRule and the evaluator; EnforceRequestRules (default true); AddEncinaDataSubjectRequestRules with a DI test.
3. Evaluate the rules in SubmitRequestAsync; on refusal persist Submit and Deny in one CreateAsync and return the coded Left.
4. When #686 and #687 are merged, gate the evaluator with IComplianceApplicability and add the DSL declaration used by S15.
5. Pass the tenant; add the counter, activity and EventIds 9806-9813.
6. Tests of every flag, the S15 rules part on Marten replacing the skeleton; docs, changelog, PublicAPI.

KEY PATTERNS:
- A prohibition that covers the right and the requester's basis always wins; every missing input refuses.
- Time comes from the context (TimeProvider at the service); validity is ValidUntilUtc null or after the request time.
- Refusal text and telemetry carry rule ids and codes only, never subject ids, requester ids or prohibition content.
- [LoggerMessage] with a registered range; TryAdd registrations; DI test with ValidateOnBuild and ValidateScopes.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/ (Services/DefaultDSRService.cs, Aggregates/DSRRequestAggregate.cs, DSRErrors.cs)
- docs/plans/data-subject-representation-implementation-plan-1197.md
- docs/plans/compliance-applicability-resolver-implementation-plan-686.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Per-request decisions on mutable status; caching would risk stale permissions |
| 2 | OpenTelemetry | ✅ | Counter and `DSR.Rules.Evaluate` activity with the tenant, codes only (Phase 6) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` EventIds 9806-9813 in the `ComplianceDSRExtensions` range (ADR-021, Phase 6) |
| 4 | Health Checks | ❌ N/A | No checkable external dependency |
| 5 | Validation | ✅ | The representation is validated by the guardian rule before the request proceeds (Phase 2) |
| 6 | Resilience | ❌ N/A | No external calls |
| 7 | Distributed Locks | ❌ N/A | Rules are read-only decisions |
| 8 | Transactions | ❌ N/A | One create of the aggregate (submit and denial together); no multi-store atomicity |
| 9 | Idempotency | ❌ N/A | Pure decision; the DSR pipeline owns request deduplication |
| 10 | Multi-Tenancy | ✅ | Status and authority lookups are tenant-scoped through #1197's service (Phase 5, SPEC-002 REQ-061) |
| 11 | Module Isolation | ❌ N/A | No module-scoped data |
| 12 | Audit Trail | ✅ | Every refusal is a persisted `DSRRequestDenied`; an acceptance is the existing `DSRRequestSubmitted` (Phase 3) |

---

## Prerequisites & Dependencies

### Blocking

- **#1197** (open): `DataSubjectRepresentation`, `RepresentationAuthority`, `IDataSubjectStatusService`, `DataSubjectStatusReadModel`, and the `Representation` parameter of `SubmitRequestAsync`. Phases 1-3 cannot start without it.
- **#686** (open) and **#687** (open, no plan yet): GitHub lists both as blocking this issue. Phases 1-3 and 5-7 do not call them and can be built and merged first; Phase 4 and the S15 DSL declaration need both.

### Coordination

- **#1195** edits `DefaultDSRService`, `DSRRequestEvents.cs` and `AccessRequest` and registers the second EventId range; **#1937** edits the service instrumentation; rebase after whichever lands first.
- **#1227** (PracticeManagement scenario): the S15 skeleton and its replacement live there.
- **#688** (OPA) follows this issue and is an optional alternative resolver.

### Deferred

- Kinship verification of a requester (Design Choice 1, option B): a follow-up only if the maintainer chooses B.

---

## Next Steps

1. Maintainer answers the two Dilemmas (Design Choices 1 and 2) when work on #2700 starts; answers go in a `## Maintainer Decisions` section appended to this plan.
2. Plan #687 (rule-based compliance DSL) before Phase 4 so that the declaration seam is fixed.
3. After #1197 lands, brief the worker for PR 1 (Phases 1-2); PR 2 follows; PR 3 waits for #686 and #687.
