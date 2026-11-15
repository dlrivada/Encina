# Implementation Plan: `Encina.Compliance.Consent` — Consent Given for a Minor (Age of Digital Consent per Jurisdiction, Holder of Parental Authority)

> **Issue**: [#1255](https://github.com/dlrivada/Encina/issues/1255)
> **Type**: Feature (SPEC-002 REQ-059, AC-041, tracking id P-52, P0)
> **Milestone**: EU regulatory readiness (parent EPIC #1186)
> **Complexity**: Medium (8 phases, one package, no database provider matrix)
> **Estimated Scope**: ~500-650 lines of production code + ~1,100-1,400 lines of tests; two pull requests (see Scope)

---

## Summary

Let a consent record state that the subject is **below the age of digital consent** of the applicable jurisdiction (14 in Spain, LOPDGDD art. 7; configurable between 13 and 16 under GDPR Art. 8) and that consent was **given or authorised by an identified holder of parental authority**, and make the consent check **refuse a minor's own consent below that age**.

### What the issue asks and what the code does today

- The issue asks for: `AgeOfDigitalConsent` configurable per jurisdiction (default table with `ES = 14`, GDPR Art. 8 range 13-16); consent records that carry `SubjectIsMinor` (or an age band at grant time) and `AuthorisedBy` (the holder of parental authority, identified), reusing the representation shape of P-09; and `ConsentRequiredPipelineBehavior` refusing a minor's own consent below the age unless a parental-authority holder is recorded.
- The issue body is **settled by elimination** (comment of 2026-10-17): storing a birth date violates data minimisation and leaving the rule to the application accepts invalid consents (fail closed); both are rejected.
- Today nothing in `src/Encina.Compliance.Consent/` knows about minors or parental authority (SPEC-002 §6, row REQ-059). `ConsentGranted` (`Events/ConsentEvents.cs:36-49`) has no age or representation field; `DefaultConsentService.GrantConsentAsync` (`Services/DefaultConsentService.cs:135-185`) records any grant; `DefaultConsentValidator.ValidateAsync` (`DefaultConsentValidator.cs:50-122`) judges only status and expiry; `ConsentValidationResult` (`Model/ConsentValidationResult.cs`) carries `Errors`, `Warnings` and `MissingPurposes` only.
- **#1197 (representation) is the shape this issue reuses**: its plan (the #1197 plan) puts `DataSubjectRepresentation(RepresentativeId, Authority, ValidUntilUtc, AuthorityReference)` and `RepresentationAuthority` (`ParentalAuthority = 1`, `Guardian = 2`, ...) in `Encina.Compliance.GDPR`, adds a `Representation` field to the four consent events and a project reference from Consent to GDPR, and orders itself before this issue (its Design Choice 7, option C rejected because #1255 would otherwise define its own field and #1197 would then change a persisted shape). Nothing of #1197 is implemented yet, so this plan treats it as a prerequisite and names no second parental-authority field.
- Persisted consent shape: adding the fields after 1.0 is breaking (SPEC-002 DEC-011 (a)).

### Scope

- **Affected package**: `Encina.Compliance.Consent` (plus tests, README, feature guide, coverage manifest, changelog fragment, `PublicAPI.Unshipped.txt`). `Encina.Compliance.GDPR` is read through #1197's types, not changed here.
- **Provider category**: none of the AGENTS.md §5 matrices; event-sourced on Marten (SPEC-002 DEC-008 (a), ADR-019); unit tests mock `IAggregateRepository`, integration tests run on Marten/PostgreSQL through Testcontainers (AGENTS.md §3).
- **Estimated files**: ~4 new source files, ~12 modified source files, ~10 test files, ~4 documentation files.
- **Pull request split** (the feature plus tests exceeds 15 files). The counts include the tests of each phase, `PublicAPI.Unshipped.txt` and the coverage manifest; the 30 distinct files become about 44 file touches because the last two recur. Four pull requests, each checked against 15 files and 1,500 lines when its brief is written:

| PR | Phases | Content (with its tests) | Files (approx.) |
|----|--------|--------------------------|-----------------|
| 1 | 1, 2 | `ConsentGranted` fields, aggregate, read model, projection, options, validator, `AgeOfDigitalConsentPolicy`, PublicAPI; aggregate, projection, options and policy unit tests; manifest | 12 |
| 2 | 3 | `MinorConsentRule`, `ConsentErrors`, `IConsentService`, `DefaultConsentService`, PublicAPI; rule and service unit tests, guard and contract tests; manifest | 10 |
| 3 | 4, 5 | `ConsentValidationResult.MinorRefusedPurposes`, `DefaultConsentValidator`, behaviour, PublicAPI; validator and behaviour unit tests; two-tenant Marten integration test; manifest | 9 |
| 4 | 6, 7, 8 | Diagnostics, `ConsentMinorLogMessages`, startup log, PublicAPI; telemetry, leak and property tests; 2 justification files; README, feature guide, changelog fragment; manifest | 13 |

Each PR depends on the one before it, and PR 1 on #1197.
- **Event IDs**: 8290-8295 in `ComplianceConsent = (8200, 8299)`, after #811 (8269-8274), #1197 (8275-8276), #810 (8277-8280) and #1199 (8281-8289). The allocation is fixed whatever the order in which the pull requests land.

### Related open issues that touch the same files

- #1197 (blocking, representation shape), #1199 (channel; also edits `ConsentGranted`, `IConsentService.GrantConsentAsync`, `ConsentDiagnostics`), #811 and #810 (same options, service and behaviour). Order and parameter order are in Prerequisites & Dependencies.
- #2700 reads minors' representation on DSR requests; it does not touch Consent code.

---

## Design Choices

<details>
<summary><strong>1. What the record stores about the subject's age — a minor flag plus the threshold applied, never a birth date</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `bool? SubjectIsMinor`, `string? Jurisdiction` and `int? AgeOfDigitalConsentApplied` on `ConsentGranted`; the age in years is a transient argument of the grant, used to compute the flag and discarded** | Data minimisation (Art. 5(1)(c)); jurisdiction-independent; the threshold in force is kept as evidence of why the grant was judged as it was | Cannot answer "how old was the subject" later |
| **B) An age band enum (for example `Under14`, `From14To15`, `From16`)** | One field; usable by later rules (a minor of 14 may exercise some rights alone) | The band boundaries are jurisdiction-specific (13-16 in Art. 8), so bands either leak one country's thresholds or need a band per country |
| **C) Birth date or age in years on the record** | Any later question can be answered | Rejected by the issue: more personal data than needed |

### Chosen Option: **A — minor flag plus threshold applied** (recommended, pending the maintainer)

Class: Dilemma

### Rationale

- We recommend A because the refusal rule needs one bit per grant and the evidence of the threshold, which is exactly what GDPR Art. 5(1)(c) allows.
- B wins in an application that also needs the 14-year line for other rights (for example a Spanish minor of 14 or more acting alone on a DSR request); then the band is added in that issue and this record keeps the flag, because the flag is derivable from a band but not the other way round.

</details>

<details>
<summary><strong>2. Where the age of digital consent comes from — a per-jurisdiction table with Spain at 14 and the GDPR default of 16 as fallback</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ConsentOptions.AgeOfDigitalConsent` (jurisdiction code to years, validated to 13-16) pre-filled with `ES = 14` only, `FallbackAgeOfDigitalConsent = 16` (the GDPR Art. 8(1) default), `DefaultJurisdiction` option and a per-grant `jurisdiction` argument; the application adds other states** | Ships only what the issue and SPEC-002 name and what can be checked (Spain); an unknown jurisdiction gets the strictest legal value (fails closed) | Applications in other member states configure their value |
| **B) Pre-fill all 27 member-state values now** | Works out of the box across the EU | Each value is a national-law claim to verify and keep current; a wrong value silently accepts invalid consent |

### Chosen Option: **A — Spain at 14, fallback 16, application adds the rest** (recommended, pending the maintainer)

Class: Dilemma

### Rationale

- We recommend A because the table is legal data with a maintenance cost and the fallback is safe (16 is the highest age Art. 8(1) allows, so a missing entry never accepts a younger subject).
- B wins when the maintainer wants a verified EU table as a product feature; it can be added later as a data-only change (a separate issue with a source for each value) without touching the model.
- The jurisdiction resolved by the future applicability resolver (#686) can replace `DefaultJurisdiction` later; until then `DefaultJurisdiction` is `null`, which means the fallback applies, so a Spanish deployment sets `DefaultJurisdiction = "ES"` (the reference scenario does).

</details>

<details>
<summary><strong>3. A grant that does not state the age — refused by default, with an explicit opt-out</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ConsentOptions.RequireAgeDeclaration` defaults to `true`: a grant with `subjectAgeYears == null` is refused (`consent.age_not_declared`); setting it to `false` is an explicit, logged opt-out for applications that never process minors** | Fail closed: missing context denies (AGENTS.md §3); the rule cannot be bypassed by omission | Every grant call site must pass the age; an adults-only back office flips one option |
| **B) Default `false`: an undeclared age is treated as an adult** | No change for existing call sites | Accepts a minor's consent whenever the application forgets to say so: the exact gap the issue closes |

### Chosen Option: **A — age declaration required by default**

Class: Settled
Settled by: `AGENTS.md` §3 (a gate whose context is missing denies; the only opt-out is explicit and logged), SPEC-002 DEC-006 and REQ-059; option B is the over-permissive default that rule forbids, so only A complies

### Rationale

- REQ-059 and AGENTS.md §3 make the gate fail closed when context is missing, and the reference application books appointments for minors (the issue's Motivation).
- A B2B application whose subjects are always adults sets `RequireAgeDeclaration = false` once; that is the explicit opt-out, and the startup log records it (EventId 8295).

</details>

<details>
<summary><strong>4. Who may authorise a minor — the holder recorded through the #1197 representation type</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `DataSubjectRepresentation` from #1197: valid when `Authority` is `ParentalAuthority` or `Guardian`, `RepresentativeId` is present and `ValidUntilUtc` is null or after the grant time** | One persisted representation vocabulary for Consent, DSR and later rules; no second field to migrate; matches "reusing the representation shape of P-09" | Depends on #1197 landing first |
| **B) A `ConsentAuthorisedBy` string on the consent events** | Independent of #1197 | A second representation vocabulary; #1197 would then change this persisted shape (its rejected option C) |

### Chosen Option: **A — reuse the #1197 representation**

Class: Settled
Settled by: issue #1255 Proposed Solution ("reusing the representation shape of P-09"), the #1197 plan Design Choice 7, LOPDGDD art. 7 (patria potestad or tutela)

### Rationale

- `ParentalAuthority` and `Guardian` both map to LOPDGDD art. 7 ("titulares de la patria potestad o tutela"); the article text is re-read before the XML documentation cites it.
- Validity is checked **at the time of the grant**; whether a consent given by a parent stays valid when the child later reaches the age is a legal question left open here and documented as a limitation (the consent is not silently revoked, the application can re-solicit).

</details>

<details>
<summary><strong>5. Where the rule is enforced — at the grant and again at the check</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Both: `DefaultConsentService` refuses an invalid minor grant (and renewal or reconsent of a minor's record without a valid holder), and `DefaultConsentValidator` treats an existing record with `SubjectIsMinor == true` and no valid representation as missing** | Invalid consent never gets recorded, and one that exists anyway (a record written by a custom service, or before a rule change) is not honoured | Two places to keep in step; solved by one shared static rule class |
| **B) Only the pipeline behaviour at check time** | Smallest change | Invalid consents are stored and listed in exports as if valid |
| **C) Only the grant** | Smallest runtime cost | A stored invalid record is honoured by the check |

### Chosen Option: **A — grant and check, one shared rule**

Class: Settled
Settled by: `AGENTS.md` §3 (compliance gates fail closed), issue #1255 Alternatives Considered 2 ("leave it to the application": rejected), SPEC-002 AC-041

### Rationale

- A pure `MinorConsentRule` (static class) has two entry points that share one representation-validity check, so the two sides cannot drift: `EvaluateGrant` (declared age, threshold, representation, time) for the grant, and `EvaluateStored` (the stored `SubjectIsMinor` flag, the stored representation, time) for the check. The check never needs the age, which is not stored (Design Choice 1).
- The stored representation is evaluated at the **grant's own moment** (`ConsentReadModel.GivenAtUtc`), not at `now`: validity is judged when the consent was given (Design Choice 4), so a later lapse of the authority, for example at 18, does not silently invalidate a stored consent.

</details>

---

## Implementation Phases

### Phase 1: Core Models, Events, Aggregate and Read Model

> **Goal**: The persisted shape for the minor flag exists.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Events/ConsentEvents.cs`** (modify `ConsentGranted`, line 36): add `bool? SubjectIsMinor`, `string? Jurisdiction`, `int? AgeOfDigitalConsentApplied` (with #1197's `Representation`, already added by then); parameter order agreed with #1199 and #1197.
2. **`Aggregates/ConsentAggregate.cs`** (modify): properties for the three fields; `Grant(...)` and `Apply(ConsentGranted)`; `Renew` and `ProvideReconsent` take the flag and the representation as data only: the minor rule is evaluated in the service before the aggregate is called (Phase 3 task 2) and its failure is a `Left`; no new `throw` is introduced on the rule path (ADR-053; the existing throwing commands are not extended).
3. **`ReadModels/ConsentReadModel.cs`** and **`ReadModels/ConsentProjection.cs`** (modify): the three fields set in `Create(ConsentGranted, ...)`.
4. **`Model/MinorConsentOutcome.cs`** (new enum): `NotMinor = 1`, `AcceptedWithParentalAuthority = 2`, `RefusedNoParentalAuthority = 3`, `RefusedAuthorityInvalid = 4`, `RefusedAgeNotDeclared = 5`.
5. **`PublicAPI.Unshipped.txt`** (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of consent given for a minor in Encina.Compliance.Consent (issue #1255).

CONTEXT:
- Consent is event-sourced on Marten: ConsentGranted (Events/ConsentEvents.cs), ConsentAggregate, ConsentProjection, ConsentReadModel.
- #1197 already added DataSubjectRepresentation? Representation to the consent events and Consent references Encina.Compliance.GDPR.
- #1199 already added Channel, Basis and BasisReference to ConsentGranted.

TASK:
Add SubjectIsMinor (bool?), Jurisdiction (string?) and AgeOfDigitalConsentApplied (int?) to ConsentGranted, the aggregate, the read model and the projection, and add the MinorConsentOutcome enum, as listed in the plan's Phase 1 Tasks.

KEY RULES:
- Never store a birth date or the age in years; only the flag, the jurisdiction code and the threshold applied.
- Enum values explicit, no zero member; XML docs on every public member.
- No logs, tags or metrics may carry the representative id.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Events/ConsentEvents.cs
- src/Encina.Compliance.Consent/Aggregates/ConsentAggregate.cs
- src/Encina.Compliance.Consent/ReadModels/ConsentProjection.cs
- the #1197 plan (under docs/plans, found by issue number)
```

</details>

---

### Phase 2: Options — Age of Digital Consent per Jurisdiction

> **Goal**: The threshold is configurable and validated at startup.

<details>
<summary><strong>Tasks</strong></summary>

1. **`ConsentOptions.cs`** (modify): `Dictionary<string, int> AgeOfDigitalConsent` (keys: ISO 3166-1 alpha-2, ordinal ignore-case comparer) pre-filled with `["ES"] = 14`; `int FallbackAgeOfDigitalConsent { get; set; } = 16`; `string? DefaultJurisdiction { get; set; }`; `bool RequireAgeDeclaration { get; set; } = true`; XML docs citing GDPR Art. 8(1), LOPDGDD art. 7 and the fallback reasoning.
2. **`ConsentOptionsValidator.cs`** (modify): every value and the fallback within 13-16 (GDPR Art. 8(1)); every key two letters; `DefaultJurisdiction`, when set, two letters.
3. **`AgeOfDigitalConsentPolicy.cs`** (new, `internal sealed`): `int Resolve(string? jurisdiction)` returning the table value, else the fallback; logs EventId 8294 (once per unknown jurisdiction, and once with the code `none` when neither `DefaultJurisdiction` nor a per-grant jurisdiction is available) when the fallback is used.
4. **`PublicAPI.Unshipped.txt`** (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of consent given for a minor in Encina.Compliance.Consent (issue #1255).

CONTEXT:
- ConsentOptions (ConsentOptions.cs) and ConsentOptionsValidator (ConsentOptionsValidator.cs) configure and validate the module at startup.

TASK:
Add AgeOfDigitalConsent (ES = 14), FallbackAgeOfDigitalConsent (16), DefaultJurisdiction, RequireAgeDeclaration (true), the validator rules and the internal AgeOfDigitalConsentPolicy as listed in the plan's Phase 2 Tasks.

KEY RULES:
- Values between 13 and 16 inclusive (GDPR Art. 8(1)); the fallback is 16 so an unknown jurisdiction never accepts a younger subject.
- Turning RequireAgeDeclaration off is an explicit opt-out and is logged at startup.
- Options with no secrets need no ToString override; keep the style of DefinePurpose and the existing properties.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ConsentOptions.cs
- src/Encina.Compliance.Consent/ConsentOptionsValidator.cs
- tests/Encina.UnitTests/Compliance/Consent/ConsentOptionsValidatorTests.cs
```

</details>

---

### Phase 3: Grant-Side Refusal and the Shared Rule

> **Goal**: A minor's own consent below the age is never recorded.

<details>
<summary><strong>Tasks</strong></summary>

1. **`MinorConsentRule.cs`** (new, `internal static`): two pure functions over one shared representation-validity check: `MinorConsentOutcome EvaluateStored(bool subjectIsMinor, DataSubjectRepresentation? representation, DateTimeOffset evaluatedAtUtc)` (check side: `NotMinor` when the flag is false, else the representation-validity outcomes below) and `MinorConsentOutcome EvaluateGrant(int? subjectAgeYears, int threshold, DataSubjectRepresentation? representation, DateTimeOffset now, bool requireAgeDeclaration)` — `RefusedAgeNotDeclared` when the age is null and declaration is required; `NotMinor` when age is at or above the threshold; below it, `AcceptedWithParentalAuthority` only when the representation has a non-empty `RepresentativeId`, `Authority` of `ParentalAuthority` or `Guardian` and `ValidUntilUtc` null or after `now`, else `RefusedNoParentalAuthority` or `RefusedAuthorityInvalid`. No I/O, no clock read.
2. **`Abstractions/IConsentService.cs`** and **`Services/DefaultConsentService.cs`** (modify): `GrantConsentAsync` and `ProvideReconsentAsync` gain `int? subjectAgeYears = null, string? jurisdiction = null` (the age is never stored); the service resolves the threshold with `AgeOfDigitalConsentPolicy`, calls the rule with `TimeProvider.GetUtcNow()` and either refuses with `ConsentErrors.MinorConsentRefused(reason)` or passes `SubjectIsMinor`, `Jurisdiction` and the threshold to the aggregate. `RenewConsentAsync` and `WithdrawConsentAsync` on a record with `SubjectIsMinor == true`: renewal requires a valid representation in the act (the #1197 parameter), withdrawal does not (anyone entitled can withdraw: Art. 7(3)).
3. **`ConsentErrors.cs`** (modify): `MinorConsentRefusedCode = "consent.minor_refused"`, `AgeNotDeclaredCode = "consent.age_not_declared"`, `ParentalAuthorityInvalidCode = "consent.parental_authority_invalid"` and factories; the error carries the code, purpose and threshold, never the age or the representative.
4. **`PublicAPI.Unshipped.txt`** (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of consent given for a minor in Encina.Compliance.Consent (issue #1255).

CONTEXT:
- Phases 1-2 added the stored fields, the options and AgeOfDigitalConsentPolicy.
- DefaultConsentService.GrantConsentAsync (Services/DefaultConsentService.cs:135-185) resolves the tenant scope and creates the aggregate; the other commands follow the same shape.
- DataSubjectRepresentation and RepresentationAuthority come from Encina.Compliance.GDPR (#1197).

TASK:
Create the internal static MinorConsentRule, extend GrantConsentAsync and ProvideReconsentAsync with subjectAgeYears and jurisdiction, enforce the rule on renewal, and add the three error codes, as listed in the plan's Phase 3 Tasks.

KEY RULES:
- Time comes from TimeProvider; the rule itself reads no clock.
- Fail closed: an undeclared age with RequireAgeDeclaration is refused; an expired or empty representation is refused.
- The age in years is a transient argument: never stored, logged, tagged or put in an error.
- Errors are Either values (ROP), never exceptions.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- src/Encina.Compliance.Consent/ConsentErrors.cs
- src/Encina.Compliance.Consent/Aggregates/ConsentAggregate.cs
```

</details>

---

### Phase 4: Check-Side Refusal in the Validator and the Behaviour

> **Goal**: A stored record of a minor without a valid holder is not honoured.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Model/ConsentValidationResult.cs`** (modify): `IReadOnlyList<string> MinorRefusedPurposes` (empty by default) and an `Invalid(...)` overload that sets it.
2. **`DefaultConsentValidator.cs`** (modify, after the status switch at lines 66-87): when `consent.SubjectIsMinor == true` `MinorConsentRule.EvaluateStored` is called with the stored flag, the stored representation and the grant's own `GivenAtUtc` as the evaluation time (not `now`, so a representation whose `ValidUntilUtc` has passed since the grant does not invalidate a consent that was valid when given): the stored record is honoured only if it carries a valid representation (the read model's `Representation`); otherwise the purpose is added to `MissingPurposes` and `MinorRefusedPurposes`.
3. **`ConsentRequiredPipelineBehavior.cs`** (modify, the `!result.IsValid` branch at line 155): when `MinorRefusedPurposes` is not empty, return `ConsentErrors.MinorConsentRefused` instead of the generic missing-consent error in `Block` mode, and log in `Warn` mode.
4. The `consent.minor` tag on the existing `Consent.Check` activity (Phase 6).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of consent given for a minor in Encina.Compliance.Consent (issue #1255).

CONTEXT:
- DefaultConsentValidator.ValidateAsync (DefaultConsentValidator.cs:50-122) judges status and expiry per purpose; ConsentRequiredPipelineBehavior.Handle (ConsentRequiredPipelineBehavior.cs:85-192) maps an invalid result to ConsentErrors.MissingConsent in Block mode.
- MinorConsentRule (Phase 3) is the single rule: call EvaluateStored with the stored SubjectIsMinor flag, the stored representation and the read model's GivenAtUtc (never now); the age is not stored.

TASK:
Add MinorRefusedPurposes to ConsentValidationResult, re-check minors in DefaultConsentValidator and return the specific minor error from the behaviour, as listed in the plan's Phase 4 Tasks.

KEY RULES:
- A record with SubjectIsMinor == true and no valid representation is never valid, whatever its status.
- Enforcement modes keep their meaning: Block returns the error, Warn logs, Disabled skips.
- Keep CRAP at or below 10 on the changed methods: extract the minor branch into a private method.
- No subject id, age or representative id in logs, tags or metrics (#1314).

REFERENCE FILES:
- src/Encina.Compliance.Consent/DefaultConsentValidator.cs
- src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs
- src/Encina.Compliance.Consent/Model/ConsentValidationResult.cs
- tests/Encina.UnitTests/Compliance/Consent/ConsentRequiredPipelineBehaviorTests.cs
```

</details>

---

### Phase 5: Cross-Cutting Integration

> **Goal**: Tenant, audit and validation follow the module's rules.

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-tenancy**: no new tenant logic; the grant and read paths already resolve the tenant (`TryResolveTenantScope`, #1315). A two-tenant test shows a minor's consent in tenant A is not visible in tenant B (AC-043).
2. **Audit trail**: the consent events record the minor flag, jurisdiction, threshold and the representation (who authorised); no new audit store.
3. **Validation**: options validated at startup (Phase 2); the grant validated by the shared rule (Phase 3).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are verifying the cross-cutting integration of consent given for a minor in Encina.Compliance.Consent (issue #1255).

CONTEXT:
- DefaultConsentService resolves the ambient tenant for every operation (#1315); events are the audit trail.

TASK:
Confirm no query bypasses the tenant predicate in the new paths and add the two-tenant integration test of the plan's Phase 7.

KEY RULES:
- Do not add new tenant logic or a new audit store.
- With tenancy off the fixed default tenant applies.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs
```

</details>

---

### Phase 6: Observability

> **Goal**: Counters, activity tags and logs without subject identifiers (REQ-062, AC-044).

<details>
<summary><strong>Tasks</strong></summary>

1. **`Diagnostics/ConsentDiagnostics.cs`** (modify): counter `consent.minor.refusals.total` (tags `consent.jurisdiction`, `consent.failure_reason`), counter `consent.minor.grants.total` (tag `consent.jurisdiction`); the `Consent.Check` activity gains `consent.minor` (boolean) and `encina.tenant_id` (the tenant tag constant is introduced by the #1199 plan, which lands before this one in the order #1197, #1199, #1255; this phase rebases onto the constant and does not define it). The issue's names (`encina.consent.minor_refusals`, `encina.consent.check`) are replaced by the package's existing naming (`consent.*`, activity `Consent.Check`).
2. **`Diagnostics/ConsentMinorLogMessages.cs`** (new, `internal static partial class`, `[LoggerMessage]`, XML doc "Event IDs: 8290-8295 (see EventIdRanges.ComplianceConsent)"): 8290 `MinorConsentRefusedAtGrant`, 8291 `MinorConsentRecordedWithParentalAuthority`, 8292 `MinorConsentInvalidAtCheck`, 8293 `AgeNotDeclaredRefused`, 8294 `UnknownJurisdictionFallbackApplied`, 8295 `AgeDeclarationOptOutActive` (warning at startup when `RequireAgeDeclaration` is false); jurisdiction codes and purposes only.
3. No range change; the assembly is mapped in `EncinaEventIdAllocationTests.cs:63`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of consent given for a minor in Encina.Compliance.Consent (issue #1255).

CONTEXT:
- ConsentDiagnostics owns the ActivitySource, Meter and tag constants. EventIds up to 8289 are taken by earlier consent work (#811 8269-8274, #1197 8275-8276, #810 8277-8280, #1199 8281-8289).

TASK:
Add the two counters, the activity tags and ConsentMinorLogMessages with EventIds 8290-8295 as listed in the plan's Phase 6 Tasks, and call them from the service, validator and behaviour.

KEY RULES:
- [LoggerMessage] source generator, packed sequentially 8290-8295, inside 8200-8299.
- No subject id, age, representative id, authority reference or EncinaError.Message in any log, tag or metric.
- Run EncinaEventIdAllocationTests.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Diagnostics/ConsentDiagnostics.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 7: Testing

> **Goal**: Every flag reaches its manifest target; AC-041 is proven.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/Compliance/Consent/Minors/`): `MinorConsentRuleTests` (every outcome, boundary at threshold minus one, equal, plus one; expired representation; wrong authority), `AgeOfDigitalConsentPolicyTests`, options validator tests (13-16 range), service grant/renew/reconsent tests with `FakeTimeProvider`, validator and behaviour tests (Block, Warn, Disabled).
2. **Guard**: public members of the options and the new service arguments.
3. **Contract**: `IConsentService` minor members against the real default; `ConsentValidationResult.MinorRefusedPurposes`.
4. **Property** (`tests/Encina.PropertyTests/Compliance/Consent/`): for every configured jurisdiction and every age below its threshold the rule refuses without a valid holder and accepts with one; at or above the threshold it never requires a holder.
5. **Integration** (`[Collection(MartenCollection.Name)]`, PostgreSQL): minor grant refused, grant by holder accepted and read back, two-tenant isolation, check refuses a stored invalid record.
6. **Telemetry/privacy**: in-memory exporter test (AC-044) and a leak test: no age, subject id or representative id in tags, metrics or logs.
7. **Load and benchmark**: `.md` justifications (not concurrent or hot).
8. **Coverage manifest**: per-file targets and justifications for every new or changed file (via `mechanical-fixer`); measure each flag; CRAP gate on changed methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of consent given for a minor in Encina.Compliance.Consent (issue #1255).

CONTEXT:
- Tests live in tests/Encina.{Unit,Guard,Contract,Property,Integration}Tests/Compliance/Consent/. Unit tests mock IAggregateRepository (NSubstitute); integration tests use [Collection(MartenCollection.Name)].
- Assertions use Shouldly through Encina.Testing.Shouldly.

TASK:
Write the tests listed in the plan's Phase 7 Tasks; they must execute real package code.

KEY RULES:
- FakeTimeProvider, no Thread.Sleep, no swallowed exceptions.
- Property test: below the threshold refuse without a valid holder, accept with one; at or above never require one.
- Measure per-flag coverage against .github/coverage-manifest/Encina.Compliance.Consent.json before reporting.

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/Consent/Aggregates/ConsentAggregateTests.cs
- tests/Encina.PropertyTests/Compliance/Consent/ConsentValidationPropertyTests.cs
- tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs
```

</details>

---

### Phase 8: Documentation and Finalization

> **Goal**: Users know how to declare an age and a holder; the build is clean.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on every new public API, with a Spanish-minor example.
2. `changelog.d/1255-consent-minor.added.md` (via `mechanical-fixer`).
3. `src/Encina.Compliance.Consent/README.md` and `docs/features/consent-management.md` (via `docs-writer`): the age table, the fallback, `RequireAgeDeclaration`, the limitation on authority that lapses after the grant, and the pitfall that a deployment must set `DefaultJurisdiction` (for example `"ES"`) or pass `jurisdiction` per grant, otherwise the fallback of 16 applies and 14 and 15 year olds are treated as minors.
4. The Consent specification (#1209): GDPR Art. 8 and LOPDGDD art. 7 rows; LOPDGDD article text re-read before it is cited.
5. `PublicAPI.Unshipped.txt`; verification: `dotnet build Encina.slnx --configuration Release` (0 warnings), the five Consent test projects, EventId architecture tests, `dotnet run --file .github/scripts/changelog-fragments.cs -- --check`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are finalising consent given for a minor in Encina.Compliance.Consent (issue #1255).

CONTEXT:
- Documentation pages go to the docs-writer; changelog fragments and PublicAPI lines to the mechanical-fixer.

TASK:
Complete the plan's Phase 8 Tasks and run the verification commands.

KEY RULES:
- No hand-typed coverage figures in documentation (covref markers).
- Do not publish a legal citation before it is checked against the law text.
- Zero build warnings.

REFERENCE FILES:
- src/Encina.Compliance.Consent/README.md
- docs/features/consent-management.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Provision | Relevance |
|----------|-----------|-----------|
| GDPR | Art. 8(1) | Consent of a child below 16 (member states may lower to 13) needs the holder of parental responsibility |
| LOPDGDD (Organic Law 3/2018) | art. 7 | Spain: 14; below it the holder of patria potestad or tutela must consent |
| GDPR | Art. 5(1)(c), 7(1), 7(3) | Data minimisation (no birth date); proof of consent; withdrawal |
| SPEC-002 | REQ-059, AC-041, DEC-006, DEC-008, DEC-011, REQ-061, REQ-062 | Requirement, acceptance, fail closed, Marten-only, shape before 1.0, tenancy, instrumentation |
| ADR-001, ADR-006, ADR-018, ADR-019, ADR-021 | ROP, cross-cutting, Marten, EventIds | Design rules followed |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `DataSubjectRepresentation`, `RepresentationAuthority` (planned) | `Encina.Compliance.GDPR` per #1197 | Holder of parental authority |
| `ConsentAggregate`, events, projection, read model | `src/Encina.Compliance.Consent/` | Carry the minor fields |
| `DefaultConsentService` tenant scope | `Services/DefaultConsentService.cs` | Tenant isolation reused |
| `DefaultConsentValidator`, `ConsentRequiredPipelineBehavior` | `src/Encina.Compliance.Consent/` | Check-side refusal |
| `ConsentOptions`, `ConsentOptionsValidator` | `src/Encina.Compliance.Consent/` | Threshold table |
| `ConsentDiagnostics`, log messages | `Diagnostics/` | Instruments and EventIds |
| `MartenFixture` and `MartenCollection` | `tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/` | Integration tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.Consent` (existing) | 8200-8299 (`EventIdRanges.ComplianceConsent`) | Used: 8200-8207, 8230-8232, 8240-8243, 8250, 8260-8268 |
| #811, #1197, #810, #1199 | 8269-8274, 8275-8276, 8277-8280, 8281-8289 | Reserved by their plans |
| **This plan (#1255)** | **8290-8295** | Packed after 8289; new `ConsentMinorLogMessages` |
| Left in the range | 8296-8299 | A further Consent feature needs a second range |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New source | 4 | Rule, policy, outcome enum, log messages |
| Modified source | 12 | Events, aggregate, read model, projection, service and interface, validator, behaviour, result, options, options validator, errors, diagnostics |
| Tests | 10 | Unit 5, guard 1, contract 1, property 1, integration 1, leak 1, justifications 2 |
| Documentation | 4 | README, feature guide, specification rows, changelog fragment |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 library (no backward compatibility, ROP with Either<EncinaError, T>, TimeProvider for time, tenant-scoped reads). Encina.Compliance.Consent is event-sourced on Marten (PostgreSQL). Issue #1255 refuses a minor's own consent below the age of digital consent of the jurisdiction (14 in Spain) unless a holder of parental authority is recorded.

IMPLEMENTATION OVERVIEW:
1. Add SubjectIsMinor, Jurisdiction and AgeOfDigitalConsentApplied to ConsentGranted, the aggregate, read model and projection.
2. Add the age table (ES = 14), fallback 16, DefaultJurisdiction and RequireAgeDeclaration to ConsentOptions with validation (13-16).
3. Add the internal MinorConsentRule and refuse invalid grants, renewals and reconsents in DefaultConsentService.
4. Re-check stored minors in DefaultConsentValidator and return the specific error from the behaviour.
5. Verify tenancy and audit; add counters, activity tags and EventIds 8290-8295.
6. Tests of every flag and Marten integration; docs, changelog, PublicAPI.

KEY PATTERNS:
- The age in years is a transient argument, never stored or logged; only the flag, jurisdiction and threshold are persisted.
- Parental authority is #1197's DataSubjectRepresentation (ParentalAuthority or Guardian, valid at grant time).
- Fail closed: undeclared age refused unless the explicit opt-out is set; unknown jurisdiction uses 16.
- No subject id, age or representative id in logs, tags or metrics; [LoggerMessage] with registered EventIds.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ (Events, Aggregates, Services, DefaultConsentValidator.cs, ConsentRequiredPipelineBehavior.cs, ConsentOptions.cs)
- the #1197 plan and the #1199 plan (under docs/plans, found by issue number)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | No read path that benefits from caching |
| 2 | OpenTelemetry | ✅ | Counters and `Consent.Check` tags with the tenant, no identifiers (Phase 6) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` EventIds 8290-8295 in `ComplianceConsent` (ADR-021, Phase 6) |
| 4 | Health Checks | ❌ N/A | No new checkable dependency |
| 5 | Validation | ✅ | A minor's consent is refused without a valid holder; options validated at startup (Phases 2-4) |
| 6 | Resilience | ❌ N/A | No call to an external system |
| 7 | Distributed Locks | ❌ N/A | No shared state updated by concurrent hosts |
| 8 | Transactions | ❌ N/A | Single-aggregate writes |
| 9 | Idempotency | ❌ N/A | Not a message or request entry point |
| 10 | Multi-Tenancy | ✅ | Tenant-aware per SPEC-002 REQ-061 through the existing tenant scope (Phase 5) |
| 11 | Module Isolation | ❌ N/A | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ✅ | Consent events record who authorised (the representation), the minor flag and the threshold (Phase 5) |

---

## Prerequisites & Dependencies

### Blocking

- **#1197** (open): `DataSubjectRepresentation`, `RepresentationAuthority`, the `Representation` field on the consent events and the Consent to GDPR project reference. Do not start Phase 1 before it has landed or its parameter order is fixed.

### Coordination

- **Same-file changes**: #1199 (channel), #811, #810 edit `ConsentGranted`, the aggregate, the service and the behaviour. Land one after another; recommended order for the persisted shape: #1197, #1199, #1255.
- **EventIds**: this plan 8290-8295 after #1199's 8281-8289; #811 8269-8274, #1197 8275-8276 and #810 8277-8280 precede it, so no id is shared.
- **Tag constant** `TagTenantId` is introduced by whichever of #1199 and #1255 lands first.

### Deferred

- A verified table of all EU member states' ages (Design Choice 2, option B): a data-only follow-up if the maintainer wants it; not needed for REQ-059.
- Jurisdiction from the applicability resolver (#686): replaces `DefaultJurisdiction` later.
- Behaviour when the child reaches the age after a parent's consent: a legal question recorded as a documented limitation.

---

## Next Steps

1. Maintainer answers the two Dilemmas (Design Choices 1 and 2; Design Choice 3 is Settled by AGENTS.md section 3) when work on #1255 starts; answers go in a `## Maintainer Decisions` section appended to this plan.
2. Wait for #1197, then brief the worker for PR 1 (Phases 1-2).
3. Implement PR 2 to PR 4 in order and extend the Consent specification with #1209.
