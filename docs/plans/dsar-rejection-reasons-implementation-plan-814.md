# Implementation Plan: DSAR Standardized Rejection Reasons — `DSRRejectionReason` (GDPR Art. 12(5), Digital Omnibus draft Art. 12(5))

> **Issue**: [#814](https://github.com/dlrivada/Encina/issues/814)
> **Type**: Feature
> **Parent**: [#880](https://github.com/dlrivada/Encina/issues/880) (EU Regulatory Compliance, Omnibus adaptations); tracked by [#1186](https://github.com/dlrivada/Encina/issues/1186) (SPEC-002), REQ-024, DEC-012, AC-024
> **Related**: [#1188](https://github.com/dlrivada/Encina/issues/1188) (P-02, per-category erasure refusal, same persisted refusal shape), [#1195](https://github.com/dlrivada/Encina/issues/1195) (P-07, withholding reasons on access exports), [#1197](https://github.com/dlrivada/Encina/issues/1197) (P-09, representation fields on the same aggregate), [#1212](https://github.com/dlrivada/Encina/issues/1212) (P-17, DSR article-coverage specification)
> **Complexity**: Low-Medium (5 phases, one package, no database provider)
> **Estimated Scope**: ~250-350 lines of production code + ~600-800 lines of tests

---

## Summary

Add a standardised, persisted reason code to the refusal of a data-subject request, so that every `Rejected` DSR request records *why* it was refused in a form that reports, dashboards and audits can aggregate, next to the free-text explanation the controller gives the data subject (GDPR Art. 12(4)). The draft-law ground of the Digital Omnibus (COM(2025) 837, amended GDPR Art. 12(5): refusal of access requests used for purposes other than data protection, "abusive intent") ships in the same enum but is **off by default**, behind an option named by its draft article; with the option off, only current-law grounds are accepted (SPEC-002 REQ-024, DEC-012, AC-024).

### What exists today (read on 2026-10-06)

Nothing of #814 is implemented. The refusal path is free text only:

| Element | Location | Today |
|---|---|---|
| Aggregate state | `src/Encina.Compliance.DataSubjectRights/Aggregates/DSRRequestAggregate.cs:114` | `string? RejectionReason` |
| Command | `DSRRequestAggregate.cs:261-272` | `Deny(string rejectionReason, DateTimeOffset deniedAtUtc)`; `ThrowIfNullOrWhiteSpace` on the text |
| Event (persisted in Marten) | `src/Encina.Compliance.DataSubjectRights/Events/DSRRequestEvents.cs:120-125` | `DSRRequestDenied(Guid RequestId, string RejectionReason, DateTimeOffset DeniedAtUtc, string? TenantId, string? ModuleId)` |
| Read model / projection | `Projections/DSRRequestReadModel.cs:113`, `Projections/DSRRequestProjection.cs:145-150` | copies the string |
| Service | `Abstractions/IDSRService.cs:126-129`, `Services/DefaultDSRService.cs:265-304` | `DenyRequestAsync(Guid, string, CancellationToken)`; `ArgumentException` from the aggregate falls into the generic `catch` and becomes `dsr.service_error` instead of `dsr.invalid_request` |
| Metric | `Diagnostics/DataSubjectRightsDiagnostics.cs:72` | `dsr.requests.denied.total` is declared and **never incremented** (no reference outside its declaration) |
| Logging | `DefaultDSRService.cs:270` | `LogDebug` string template, not `[LoggerMessage]` |
| Dead record | `Model/DSRRequest.cs` | the pre-Marten `DSRRequest` record (with `RejectionReason` string, line 102) has no caller in `src/` or `tests/`; `DSRRequestReadModel.cs:23-24` documents that it replaced it |

### What this plan delivers

- `DSRRejectionReason` enum (current-law grounds + one draft ground), stored on the `DSRRequestDenied` event, the aggregate and the read model.
- The free-text explanation stays mandatory (Art. 12(4) reasons to the subject; Art. 12(5) burden of demonstration), renamed `RejectionExplanation` so that `RejectionReason` is the code.
- `DataSubjectRightsOptions.EnableOmnibusArticle12RefusalGrounds` (default `false`): with it off, `DenyRequestAsync` refuses `AbusiveIntent` with `dsr.rejection_reason_not_enabled`.
- Observability of the deny path: the dormant `dsr.requests.denied.total` counter wired with a `dsr.rejection_reason` tag, an activity, two `[LoggerMessage]` events.
- A shared refusal vocabulary that P-02 (#1188) reuses for its per-category refusals (Design Choice 4).
- Removal of the dead `Model/DSRRequest.cs` record (pre-1.0, no legacy).

**Standards covered**: GDPR Art. 12(2) with Art. 11(2), Art. 12(4), Art. 12(5), Art. 23; COM(2025) 837 draft amendment of Art. 12(5) (off by default).

**Affected packages**: `Encina.Compliance.DataSubjectRights` only.

**Provider category**: none. The DSR module is event-sourced on Marten (ADR-019; SPEC-002 DEC-008 keeps event-sourced compliance modules Marten-only), so the 10-provider database rule does not apply; the new enum travels inside an existing Marten event. Integration tests run on Marten/PostgreSQL through the shared `MartenFixture` (`tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs`).

**Estimated files**: ~6 production files changed, 2 added, 1 deleted; ~10 test files changed or added; ~6 documentation files.

---

## Design Choices

<details>
<summary><strong>1. Value set of the enum — current-law grounds plus one gated draft ground</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The five values of the issue body** (`ManifestlyUnfounded`, `Excessive`, `AbusiveIntent`, `IdentityNotVerified`, `Other`) | Matches the issue text literally; smallest change | Cites Art. 12(6) for identity, which lets the controller *ask* for information; the refusal ground is Art. 12(2) with Art. 11(2). No code for a restriction under Union or national law (Art. 23, e.g. LOPDGDD) nor for a right whose conditions are not met (Art. 20(1), Art. 21(1)), so those land in `Other` and the statistics the issue wants lose meaning |
| **B) Current-law-complete set + one draft value** (`ManifestlyUnfounded`, `Excessive`, `IdentityNotVerifiable`, `RestrictedByLaw`, `RightNotApplicable`, `Other`; draft: `AbusiveIntent`) | Every common lawful refusal has its own code with an article anchor; `Other` stays rare; the draft value is the only one gated | Two values more than the issue; `RightNotApplicable` overlaps in spirit with the per-category exemptions of P-02 (resolved by Design Choice 4) |
| **C) Open string codes with a registry** (`DSRRejectionCode` value object, application registers codes) | Fully extensible per jurisdiction | Loses compile-time safety and cheap aggregation; persisted strings drift; no fixed set for the article-coverage specification (#1212) to cite |

### Chosen Option: **B — Current-law-complete set plus one gated draft value** (recommended, pending the maintainer)

### Rationale

- We recommend B because the issue's goal is auditability: a code that is right in law and specific enough to aggregate. Art. 12(5) (manifestly unfounded, excessive "in particular because of their repetitive character"), Art. 12(2) with Art. 11(2) (controller demonstrates it cannot identify the subject) and Art. 23 restrictions are the refusals controllers actually record today.
- Numbering is explicit and stable (`ManifestlyUnfounded = 1`, ..., `Other = 99`, `AbusiveIntent = 100`) with no `0` member, so an uninitialised value is never a valid reason; the persisted shape is fixed before 1.0 (SPEC-002 DEC-012 lists #814 among the shape changes).
- A C# 14 extension block (`extension(DSRRejectionReason reason)`) exposes `IsDraftLawGround` and `LegalBasis` (e.g. `"GDPR Art. 12(5)"`, `"COM(2025) 837, draft GDPR Art. 12(5)"`), so the gate, the docs and the article-coverage specification read one source.
- The issue's citation of an "EDPB-EDPS Joint Opinion 2/2026" for "demonstrable abusive intent" could not be verified from the repository; the XML docs cite only COM(2025) 837 until SPEC-002 §3.5 records the opinion.

</details>

<details>
<summary><strong>2. Event and aggregate shape — required code plus required explanation (no compatibility path)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Required code + required explanation**: `DSRRequestDenied(Guid RequestId, DSRRejectionReason RejectionReason, string RejectionExplanation, ...)`; `Deny(DSRRejectionReason, string, DateTimeOffset)` | Every refusal is classified; the subject still receives reasons (Art. 12(4)); the controller's demonstration is recorded (Art. 12(5) last sentence); today's non-empty-text rule is kept | Breaking change to `IDSRService`, the event and the read model (acceptable pre-1.0); existing dev event streams with the old event need re-creating |
| **B) Required code, explanation required only for `Other`** | Matches the issue's "Other requires free text" literally; less typing for common cases | A refusal for `ManifestlyUnfounded` with no recorded demonstration fails the Art. 12(5) burden of proof; Art. 12(4) reasons to the subject would be a bare code |
| **C) Optional code (`DSRRejectionReason?`), free text required** — the issue's "backward compatible" wording | No caller changes | Codes become optional, so statistics are incomplete forever; a compatibility path, which `AGENTS.md` §1 and §3 forbid pre-1.0 |

### Chosen Option: **A — Required code plus required explanation** (recommended, pending the maintainer)

### Rationale

- We recommend A because pre-1.0 the best shape wins over compatibility (`AGENTS.md` §1); the issue's "backward compatible" criterion is replaced by "free text still recorded", which A keeps.
- The issue's acceptance item "`Other` requires accompanying free-text" is still met (and tested): the explanation is required for every code, `Other` included.
- Naming: the property `RejectionReason` becomes the code (`DSRRejectionReason?` on the aggregate and read model, `null` until denied); the free text becomes `RejectionExplanation`. One concept per name, consistent with `ExtensionReason`.
- The aggregate validates `Enum.IsDefined` (throws `ArgumentOutOfRangeException`) and the non-empty explanation; it does not know options (the gate is Design Choice 3).

</details>

<details>
<summary><strong>3. Gating the draft ground — option checked by the service, value always representable</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Bool option `EnableOmnibusArticle12RefusalGrounds` (default `false`), checked in `DefaultDSRService.DenyRequestAsync`; `Left(dsr.rejection_reason_not_enabled)` when off** | Shape exists now (AC-024: "behind an option that is off by default and named by its draft article"); domain stays pure; one line to flip on adoption | The enum value is visible in IntelliSense while unusable by default (documented in XML docs) |
| **B) Two enums (`DSRRejectionReason` current law, `DSROmnibusRejectionReason` draft)** | Draft value cannot be used by accident | Two persisted fields or a union; merging them on adoption is the breaking change DEC-012 wants to avoid |
| **C) No gate; document that `AbusiveIntent` is draft** | Simplest | Violates REQ-024 and AC-024 (default must follow current law) |
| **D) Gate in the aggregate (pass the flag into `Deny`)** | Invariant enforced even when the aggregate is used without the service | Leaks configuration into the domain; every caller passes a flag |

### Chosen Option: **A — Option checked by the service** (recommended, pending the maintainer)

### Rationale

- We recommend A because it is exactly the AC-024 shape: the persisted value exists, the behaviour does not act until the option is on, and the default test asserts that only current-law grounds are accepted.
- The option name carries the draft article (`OmnibusArticle12`); its XML docs name COM(2025) 837 and the amended Art. 12(5), as REQ-024 requires.
- The refusal while off is logged (`[LoggerMessage]`, Warning) and returns a coded error; the error message never carries subject data.
- The projection and read model accept the value regardless of the option, so streams written while the option was on stay readable after it is switched off (the same "shape exists" guarantee AC-024 asks of #810, #811 and #815).

</details>

<details>
<summary><strong>4. Relation with P-02 (#1188) — one refusal vocabulary for whole-request and per-category refusals</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) #814 lands first and defines `DSRRejectionReason`; P-02's per-category outcome reuses it (`RightNotApplicable` + the existing `ErasureExemption` + legal basis + grantable-after date)** | One vocabulary across `DSRRequestDenied` and P-02's erasure outcome, as the #814 comment requires ("the reason codes live in the persisted DSR result shape used by SPEC-002 P-02"); P-02's AC "leaves room for the #814 refusal reasons without a breaking change" is met by construction | P-02 must reference this type; sequencing dependency between two open issues |
| **B) P-02 first defines its outcome shape; #814 adds the enum to it later** | P-02 is P0 in v0.14.0 and is not blocked | #814 would then change P-02's persisted shape, the breaking change both issues try to avoid |
| **C) Independent types (whole-request enum here, separate per-category codes in P-02)** | No coordination | Two vocabularies for one legal act (a refusal); reports must join them; contradicts the SPEC-002 §3.5 row ("the reason codes of REQ-004 (P-02) stay current-law codes" in the same shape) |

### Chosen Option: **A — #814 defines the shared vocabulary first** (recommended, pending the maintainer)

### Rationale

- We recommend A because #814 is small and has no prerequisites, while P-02 has three (P-01 #1187, P-03 #1189, P-45 #1248); landing #814 first gives P-02 a fixed type to embed and removes a later shape change.
- This plan does **not** build P-02's per-category result; it only fixes the enum and documents (XML docs + feature page) that P-02 reuses it. A note for the #1188 implementer goes into Next Steps.
- `ErasureExemption` (`Model/ErasureExemption.cs`) stays the Art. 17(3) exemption list; P-02 pairs it with `DSRRejectionReason.RightNotApplicable` or `RestrictedByLaw`.

</details>

<details>
<summary><strong>5. Decision support for "Excessive" — record only, no automatic detection</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Record only: the controller chooses the code; the existing `GetRequestsBySubjectAsync` (`IDSRService.cs:254`) gives the history** | Encina facilitates, never decides (SPEC-002 principle); no new API | The controller counts repeats itself |
| **B) Add `CountRequestsBySubjectAsync(subjectId, right, since)`** | Convenient evidence for repetitive requests | New query surface for a judgement the law leaves to the controller; overlaps the existing query |
| **C) Automatic flagging of repetitive requests (threshold option)** | Hands-off | A fixed threshold is a legal decision Encina must not take; risk of refusing lawful requests |

### Chosen Option: **A — Record only** (recommended, pending the maintainer)

### Rationale

- We recommend A because the refusal is the controller's decision and burden (Art. 12(5)); Encina's job is to record the code and the demonstration faithfully.
- The existing read-model query already returns the subject's previous requests with `ReceivedAtUtc`, enough to demonstrate repetition.

</details>

<details>
<summary><strong>6. Scope of observability work — wire the deny path only, defer the other lifecycle commands</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Instrument `DenyRequestAsync` only (counter `dsr.requests.denied.total` with `dsr.right_type` and `dsr.rejection_reason`, activity via `StartAggregateCommand("Deny", id)`, two `[LoggerMessage]` events); open a `[DEBT]` issue for the other lifecycle commands** | Keeps the PR on #814; meets SPEC-002 REQ-062 for the operation this issue changes | The other six lifecycle counters and `StartAggregateCommand` stay dormant until the debt issue lands |
| **B) Instrument every lifecycle command now** | Closes the whole gap at once | Mixes an unrelated fix into a shape-change PR; more CRAP-gate surface |
| **C) Leave the deny path as is** | Smallest diff | The reason codes exist to be counted; leaving the counter dormant defeats the issue's motivation |

### Chosen Option: **A — Deny path now, the rest as a debt issue** (recommended, pending the maintainer)

### Rationale

- We recommend A: `RequestsSubmittedTotal`, `RequestsVerifiedTotal`, `RequestsProcessingTotal`, `RequestsCompletedTotal`, `RequestsExtendedTotal`, `RequestsExpiredTotal` (`DataSubjectRightsDiagnostics.cs:44-88`) and `StartAggregateCommand` (`:164`) have no caller; that is a separate defect recorded in `artifacts/issues/plan-814-dsr-lifecycle-telemetry.md`.
- The rejection reason is a low-cardinality enum, safe as a metric tag; no subject identifier, explanation text or tenant id goes into metric tags (#1429). The tenant id goes on the activity as an attribute only (REQ-062).

</details>

---

## Implementation Phases

### Phase 1: Domain Model — Enum, Event, Aggregate, Read Model

> **Goal**: Fix the persisted refusal shape.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Compliance.DataSubjectRights/Model/DSRRejectionReason.cs`
   - `public enum DSRRejectionReason` in namespace `Encina.Compliance.DataSubjectRights`
   - Members with explicit values: `ManifestlyUnfounded = 1` (Art. 12(5)), `Excessive = 2` (Art. 12(5), repetitive character), `IdentityNotVerifiable = 3` (Art. 12(2) with Art. 11(2)), `RestrictedByLaw = 4` (Art. 23, Union or Member State law), `RightNotApplicable = 5` (conditions of the exercised right not met, e.g. Art. 17(3), 20(1), 21(1)), `Other = 99`, `AbusiveIntent = 100` (draft: COM(2025) 837, amended Art. 12(5); off by default)
   - XML docs per member citing the article; `<remarks>` on `AbusiveIntent` naming `DataSubjectRightsOptions.EnableOmnibusArticle12RefusalGrounds`
2. **Create** `src/Encina.Compliance.DataSubjectRights/Model/DSRRejectionReasonExtensions.cs`
   - `public static class DSRRejectionReasonExtensions` with a C# 14 `extension(DSRRejectionReason reason)` block:
     - `bool IsDraftLawGround { get; }` — `true` only for `AbusiveIntent`
     - `string LegalBasis { get; }` — article text per member
   - `// crap-exempt: single-question switch — maps each reason to its legal basis` above the `LegalBasis` switch if its complexity exceeds 10
3. **Modify** `Events/DSRRequestEvents.cs:120-125` — `DSRRequestDenied(Guid RequestId, DSRRejectionReason RejectionReason, string RejectionExplanation, DateTimeOffset DeniedAtUtc, string? TenantId, string? ModuleId) : INotification`; update XML docs (Art. 12(4), 12(5))
4. **Modify** `Aggregates/DSRRequestAggregate.cs`
   - Line 114: `public DSRRejectionReason? RejectionReason { get; private set; }`; add `public string? RejectionExplanation { get; private set; }`
   - Lines 261-272: `public void Deny(DSRRejectionReason rejectionReason, string rejectionExplanation, DateTimeOffset deniedAtUtc)` — terminal-status check unchanged; `if (!Enum.IsDefined(rejectionReason)) throw new ArgumentOutOfRangeException(...)`; `ArgumentException.ThrowIfNullOrWhiteSpace(rejectionExplanation)`
   - Lines 386-390: `Apply` sets both properties
5. **Modify** `Projections/DSRRequestReadModel.cs:113` — `DSRRejectionReason? RejectionReason`, add `string? RejectionExplanation`
6. **Modify** `Projections/DSRRequestProjection.cs:145-150` — copy both fields
7. **Delete** `Model/DSRRequest.cs` (dead pre-Marten record) and its lines in `PublicAPI.Unshipped.txt`; remove its entry from `.github/coverage-manifest/Encina.Compliance.DataSubjectRights.json`
8. **Update** `PublicAPI.Unshipped.txt` (RS0016/RS0017): new enum and extension members, changed event constructor and properties, aggregate and read-model properties

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no backward compatibility) has an event-sourced DSR module in
src/Encina.Compliance.DataSubjectRights/ (Marten, ADR-019). A DSR refusal today stores only free
text: DSRRequestAggregate.RejectionReason (string, Aggregates/DSRRequestAggregate.cs:114),
Deny(string, DateTimeOffset) at :261, event DSRRequestDenied (Events/DSRRequestEvents.cs:120),
read model DSRRequestReadModel.RejectionReason (Projections/DSRRequestReadModel.cs:113) and
DSRRequestProjection.Apply(DSRRequestDenied, ...) (:145). Issue #814 adds a standardised reason code.

TASK:
1. Create Model/DSRRejectionReason.cs: public enum DSRRejectionReason { ManifestlyUnfounded = 1,
   Excessive = 2, IdentityNotVerifiable = 3, RestrictedByLaw = 4, RightNotApplicable = 5, Other = 99,
   AbusiveIntent = 100 } with XML docs citing GDPR Art. 12(5), 12(2)+11(2), 23, and for AbusiveIntent
   "COM(2025) 837, draft amendment of GDPR Art. 12(5); off by default, see
   DataSubjectRightsOptions.EnableOmnibusArticle12RefusalGrounds". No member with value 0.
2. Create Model/DSRRejectionReasonExtensions.cs with a C# 14 extension block exposing
   bool IsDraftLawGround and string LegalBasis.
3. Change DSRRequestDenied to (Guid RequestId, DSRRejectionReason RejectionReason,
   string RejectionExplanation, DateTimeOffset DeniedAtUtc, string? TenantId, string? ModuleId).
4. Aggregate: RejectionReason becomes DSRRejectionReason?, add string? RejectionExplanation;
   Deny(DSRRejectionReason rejectionReason, string rejectionExplanation, DateTimeOffset deniedAtUtc)
   keeps the terminal-status InvalidOperationException, throws ArgumentOutOfRangeException for an
   undefined enum value and ArgumentException for a null/whitespace explanation; Apply sets both.
5. Read model and projection: same two fields.
6. Delete Model/DSRRequest.cs (no caller in src/ or tests/) and its PublicAPI lines and manifest entry.
7. Update PublicAPI.Unshipped.txt until the build has zero RS0016/RS0017.

KEY RULES:
- Pre-1.0: change the shape completely; no [Obsolete], no optional compatibility parameter.
- XML docs on every public member; English only.
- Zero warnings (CA1707: no underscores in public names).
- Do not touch the service yet (Phase 2); fix compile errors in tests only where the signature changed.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Aggregates/DSRRequestAggregate.cs
- src/Encina.Compliance.DataSubjectRights/Events/DSRRequestEvents.cs
- src/Encina.Compliance.DataSubjectRights/Projections/DSRRequestProjection.cs
- src/Encina.Compliance.DataSubjectRights/Projections/DSRRequestReadModel.cs
- src/Encina.Compliance.DataSubjectRights/Model/ErasureExemption.cs (enum doc style)
- docs/plans/dsar-rejection-reasons-implementation-plan-814.md (Design Choices 1-2)
```

</details>

---

### Phase 2: Service, Options and Errors

> **Goal**: Expose the code through `IDSRService` and gate the draft ground.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `DataSubjectRightsOptions.cs` — add `public bool EnableOmnibusArticle12RefusalGrounds { get; set; }` (default `false`); XML docs: "Draft law: COM(2025) 837 amends GDPR Art. 12(5) ... Current law stays the default (SPEC-002 REQ-024)"; add it to the `<example>`
2. **Modify** `DSRErrors.cs` — `public const string RejectionReasonNotEnabledCode = "dsr.rejection_reason_not_enabled";` and factory `RejectionReasonNotEnabled(DSRRejectionReason reason)` (message names the reason and the option, never the subject)
3. **Modify** `Abstractions/IDSRService.cs:114-129` — `ValueTask<Either<EncinaError, Unit>> DenyRequestAsync(Guid requestId, DSRRejectionReason rejectionReason, string rejectionExplanation, CancellationToken cancellationToken = default);` with XML docs on Art. 12(4)/(5) and the gate
4. **Modify** `Services/DefaultDSRService.cs`
   - Constructor: add `IOptions<DataSubjectRightsOptions> options` (store `options.Value`), guard with `ArgumentNullException.ThrowIfNull`
   - `DenyRequestAsync` (`:265`): before loading, validate input and return `Left`:
     - undefined enum value or blank explanation → `DSRErrors.InvalidRequest(...)` (today the aggregate's `ArgumentException` becomes `dsr.service_error` through the generic catch at `:299`)
     - `rejectionReason.IsDraftLawGround && !_options.EnableOmnibusArticle12RefusalGrounds` → log `DSRRejectionReasonNotEnabled` and return `DSRErrors.RejectionReasonNotEnabled(reason)`
   - then `aggregate.Deny(rejectionReason, rejectionExplanation, deniedAtUtc)`
   - Keep the method's CRAP ≤ 10: move the input checks into a private `ValidateDenyInput(...)` returning `Option<EncinaError>`
5. **No DI change**: `ServiceCollectionExtensions.cs:88-96` already configures `DataSubjectRightsOptions` and registers `DefaultDSRService` with `TryAddScoped`; the new constructor parameter resolves from the existing options registration. Keep the DI test that builds with `ValidateOnBuild` and `ValidateScopes` green (`AGENTS.md` §3, registration completeness).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Phase 1 of issue #814 added DSRRejectionReason (with the C# 14 extension property IsDraftLawGround)
and changed DSRRequestAggregate.Deny to Deny(DSRRejectionReason, string explanation, DateTimeOffset).
IDSRService.DenyRequestAsync (Abstractions/IDSRService.cs:126) and DefaultDSRService.DenyRequestAsync
(Services/DefaultDSRService.cs:265) still take a single string. SPEC-002 REQ-024/AC-024 require the
Omnibus draft ground (AbusiveIntent) to be off by default behind an option named by its draft article.

TASK:
1. Add bool EnableOmnibusArticle12RefusalGrounds (default false) to DataSubjectRightsOptions with XML
   docs naming COM(2025) 837 and the amended GDPR Art. 12(5).
2. Add DSRErrors.RejectionReasonNotEnabledCode = "dsr.rejection_reason_not_enabled" and its factory.
3. Change IDSRService.DenyRequestAsync to (Guid requestId, DSRRejectionReason rejectionReason,
   string rejectionExplanation, CancellationToken cancellationToken = default).
4. DefaultDSRService: inject IOptions<DataSubjectRightsOptions>; in DenyRequestAsync return
   DSRErrors.InvalidRequest for an undefined reason or blank explanation, return
   DSRErrors.RejectionReasonNotEnabled when the reason is a draft ground and the option is off,
   otherwise load, Deny, save as today. Extract the checks into a private helper so the method
   stays at CRAP <= 10.

KEY RULES:
- ROP: Either<EncinaError, T>; no exception escapes for invalid input.
- EncinaError.Message never contains the subject id or the explanation text.
- Time from the injected TimeProvider only.
- Registration completeness: the existing DI test with ValidateOnBuild/ValidateScopes must pass.
- PublicAPI.Unshipped.txt updated; zero warnings.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs
- src/Encina.Compliance.DataSubjectRights/Abstractions/IDSRService.cs
- src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs
- src/Encina.Compliance.DataSubjectRights/DSRErrors.cs
- src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 3: Cross-Cutting Integration and Observability

> **Goal**: Count and trace refusals by reason; keep tenant and module on the event.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `Diagnostics/DataSubjectRightsDiagnostics.cs`
   - Add `internal const string TagRejectionReason = "dsr.rejection_reason";` and `internal const string TagTenantId = "dsr.tenant_id";` (activity attribute only, never a metric tag)
   - Add `internal static void RecordDenied(DSRRejectionReason reason, DataSubjectRight rightType)` that increments `RequestsDeniedTotal` (`:72`) with `TagRightType` and `TagRejectionReason`
2. **Modify** `Diagnostics/DSRLogMessages.cs` — two `[LoggerMessage]` methods in the free slots of the registered range `ComplianceDSR` (8300-8349, `src/Encina/Diagnostics/EventIdRanges.cs:316`):
   - `8311` `DSRRequestDenied(string rightType, string rejectionReason)` — Information
   - `8312` `DSRRejectionReasonNotEnabled(string rejectionReason)` — Warning
   - XML doc names the range; no subject id, no explanation text
3. **Modify** `Services/DefaultDSRService.cs` `DenyRequestAsync`
   - `using var activity = DataSubjectRightsDiagnostics.StartAggregateCommand("Deny", requestId);` set `TagRejectionReason`, and `TagTenantId` from the loaded aggregate's `TenantId` when present
   - On successful save: `RecordDenied(...)`, `_logger.DSRRequestDenied(...)`, `RecordCompleted(activity)`; on `Left`: `RecordFailed(activity, error.GetCode())` (code only, never the message)
   - Replace the `LogDebug` string template at `:270`
4. **Multi-tenancy / module isolation**: `TenantId` and `ModuleId` already flow from the aggregate into `DSRRequestDenied` (`DSRRequestAggregate.cs:271`); no change beyond keeping them in the new constructor
5. **Audit trail**: the `DSRRequestDenied` event in the Marten stream is the audit record (ADR-019, Art. 5(2)); it now carries the code and the explanation

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #814, after Phases 1-2. DataSubjectRightsDiagnostics.RequestsDeniedTotal
(Diagnostics/DataSubjectRightsDiagnostics.cs:72) and StartAggregateCommand (:164) exist but nothing
calls them. DSRLogMessages uses the registered range ComplianceDSR 8300-8349
(src/Encina/Diagnostics/EventIdRanges.cs:316); 8300-8303, 8310 and 8320-8349 are used, 8304-8309 and
8311-8319 are free inside the range.

TASK:
1. Add TagRejectionReason ("dsr.rejection_reason") and TagTenantId ("dsr.tenant_id") constants and a
   RecordDenied(DSRRejectionReason, DataSubjectRight) helper incrementing RequestsDeniedTotal with
   right type and rejection reason tags.
2. Add [LoggerMessage] EventId 8311 DSRRequestDenied(rightType, rejectionReason) (Information) and
   8312 DSRRejectionReasonNotEnabled(rejectionReason) (Warning).
3. In DefaultDSRService.DenyRequestAsync start an activity with StartAggregateCommand("Deny", id),
   tag the reason and (activity only) the tenant id; on success record the metric, log 8311 and mark
   completed; on Left mark failed with the error code; on the gate refusal log 8312.
4. Remove the LogDebug string template in DenyRequestAsync.

KEY RULES:
- ADR-021: EventIds only inside the registered range of the package; the range is already mapped in
  tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs:66.
- No subject id, explanation text or EncinaError.Message in tags or logs (#1429, AGENTS.md section 3).
- Tenant id is an activity attribute only, never a metric tag (cardinality).
- CRAP <= 10 on DenyRequestAsync.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Diagnostics/DataSubjectRightsDiagnostics.cs
- src/Encina.Compliance.DataSubjectRights/Diagnostics/DSRLogMessages.cs
- src/Encina.Compliance.DataSubjectRights/ProcessingRestrictionPipelineBehavior.cs (tagging pattern)
- tests/Encina.UnitTests/Compliance/DataSubjectRights/DataSubjectRightsDiagnosticsSubjectIdLeakTests.cs
```

</details>

---

### Phase 4: Testing

> **Goal**: Every touched file meets its per-flag targets; AC-024's default-follows-current-law test exists.

<details>
<summary><strong>Tasks</strong></summary>

1. **Coverage manifest** — `.github/coverage-manifest/Encina.Compliance.DataSubjectRights.json` has no per-file `targets` today; add `targets` and one-sentence `justifications` for every touched file: `Model/DSRRejectionReason.cs`, `Model/DSRRejectionReasonExtensions.cs`, `Events/DSRRequestEvents.cs`, `Aggregates/DSRRequestAggregate.cs`, `Projections/DSRRequestProjection.cs`, `Projections/DSRRequestReadModel.cs`, `Services/DefaultDSRService.cs`, `DataSubjectRightsOptions.cs`, `DSRErrors.cs`, `Diagnostics/DataSubjectRightsDiagnostics.cs` (delegate to `mechanical-fixer`); run `--check-justifications`
2. **Unit** (`tests/Encina.UnitTests/Compliance/DataSubjectRights/`)
   - `Aggregates/DSRRequestAggregateTests.cs`: deny sets code + explanation; event carries both; each defined reason accepted; undefined value throws; terminal status throws
   - `Projections/DSRRequestProjectionTests.cs`: denied event copies both fields
   - `Services/DefaultDSRServiceTests.cs`: **default options reject `AbusiveIntent` with `dsr.rejection_reason_not_enabled` and accept every current-law reason** (AC-024); option on accepts `AbusiveIntent`; blank explanation and undefined value → `dsr.invalid_request` without loading the aggregate; `Other` with explanation succeeds
   - `Model/DSRRejectionReasonExtensionsTests.cs` (new): `IsDraftLawGround` true only for `AbusiveIntent`; `LegalBasis` non-empty for every member
   - `DataSubjectRightsOptionsTests.cs`: default of the new option is `false`
   - Diagnostics: in-memory `MeterListener`/`ActivityListener` test that a deny emits `dsr.requests.denied.total` with `dsr.rejection_reason` and an activity with the tenant attribute, and that no tag or log carries the subject id or the explanation (extend the existing leak tests)
3. **Guard** (`tests/Encina.GuardTests/Compliance/DataSubjectRights/`): `DSRRequestAggregateGuardTests.cs` (`:86`, `:98`) for the new explanation parameter and the undefined enum; `DefaultDSRServiceGuardTests.cs` for the new `IOptions` constructor parameter; `DefaultDSRServiceMethodGuardTests.cs` for the new signature
4. **Contract**: `tests/Encina.ContractTests/Compliance/DataSubjectRights/IDSRServiceContractTests.cs` — the deny contract with reason codes, run against the real `DefaultDSRService`
5. **Property** (`tests/Encina.PropertyTests/Compliance/DataSubjectRights/`): new `DSRRejectionReasonPropertyTests.cs` — for any defined reason and non-blank explanation, `Deny` then `Apply` round-trips both values; for any reason with `IsDraftLawGround == false`, the service result does not depend on the option
6. **Integration** (`tests/Encina.IntegrationTests/Compliance/DataSubjectRights/`, new folder): `DSRRejectionReasonMartenIntegrationTests.cs` with `[Collection(MartenCollection.Name)]`, `[Trait("Category", "Integration")]`, `[Trait("Database", "PostgreSQL")]` — append `DSRRequestDenied` with each reason through `IAggregateRepository<DSRRequestAggregate>`, reload, and read the inline projection: code and explanation survive Marten serialization. This also seeds the DSR integration folder that #1204 asks for.
7. **Load / Benchmark**: unchanged; the existing justifications `tests/Encina.LoadTests/Compliance/DataSubjectRights/DataSubjectRights.md` and `tests/Encina.BenchmarkTests/Encina.Benchmarks/Compliance/DataSubjectRights/DataSubjectRights.md` stay valid (a refusal is a single, non-concurrent, non-hot-path command)
8. **Measure** each flag from the repository root (`dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests`, then `coverage-report.cs`), and run the local CRAP check on the changed methods

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #814 Phases 1-3 changed the DSR refusal shape (DSRRejectionReason + explanation), gated the
draft ground AbusiveIntent behind DataSubjectRightsOptions.EnableOmnibusArticle12RefusalGrounds
(default false) and instrumented DefaultDSRService.DenyRequestAsync. SPEC-002 AC-024 requires a test
asserting that the default uses the current-law grounds.

TASK:
1. Add per-file targets and justifications for every touched file to
   .github/coverage-manifest/Encina.Compliance.DataSubjectRights.json (unit/guard/contract/property/
   integration as applicable) and run coverage-report.cs --check-justifications.
2. Unit tests: aggregate, projection, extension block, options default, service gate (default rejects
   AbusiveIntent with dsr.rejection_reason_not_enabled and accepts every current-law reason; option on
   accepts it), input validation (dsr.invalid_request), telemetry with in-memory listeners and no
   subject id or explanation in tags/logs.
3. Guard tests for the new parameters; contract test for IDSRService deny; FsCheck property test for
   the Deny/Apply round trip.
4. Integration test on Marten/PostgreSQL using [Collection(MartenCollection.Name)] and the shared
   MartenFixture: each reason round-trips through the event store and the inline projection.
5. Measure every flag and the CRAP of changed methods; all flags meet their targets.

KEY RULES:
- Shouldly via Encina.Testing.Shouldly; FsCheck via Encina.Testing.FsCheck; no FluentAssertions.
- Tests execute real package code; no reflection-only tests.
- Never IClassFixture or new MartenFixture(); no Thread.Sleep; outputs under artifacts/.
- Arrange-Act-Assert, one behaviour per test, descriptive names.

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/DataSubjectRights/Services/DefaultDSRServiceTests.cs
- tests/Encina.UnitTests/Compliance/DataSubjectRights/Aggregates/DSRRequestAggregateTests.cs
- tests/Encina.GuardTests/Compliance/DataSubjectRights/DSRRequestAggregateGuardTests.cs
- tests/Encina.ContractTests/Compliance/DataSubjectRights/IDSRServiceContractTests.cs
- tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs (Marten collection pattern)
- tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 5: Documentation and Finalization

> **Goal**: Document the codes, the draft gate and the P-02 reuse; ship the changelog fragment.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML docs** on every new or changed public member (enum members cite articles; option and `AbusiveIntent` name COM(2025) 837)
2. **Changelog**: `changelog.d/814-dsar-rejection-reasons.added.md` (reason codes, option) and `changelog.d/814-dsar-rejection-reasons.changed.md` (breaking `DenyRequestAsync`/`DSRRequestDenied` shape, `RejectionReason` now a code, `RejectionExplanation` added, `DSRRequest` record removed) — `mechanical-fixer`
3. **Package README** `src/Encina.Compliance.DataSubjectRights/README.md`: lifecycle section (`:10`, `:183-187`), options table (`:208-211`) gains the new option, error table gains `dsr.rejection_reason_not_enabled`, article table gains Art. 12(2), 12(5), 23 rows — `docs-writer`
4. **Feature page** `docs/features/data-subject-rights.md`: replace the stale pre-Marten rejection example (`store.UpdateStatusAsync(... Rejected ...)`, `:314-315`) with `DenyRequestAsync(id, DSRRejectionReason.Excessive, "...")`; add a "Refusal reasons" table (code, article, current law or draft) and the P-02 reuse note — `docs-writer`
5. **SPEC-002 / P-17**: no edit to SPEC-002; the DSR article-coverage specification (#1212) cites `DSRRejectionReason` when it is written
6. **docs/INVENTORY.md**: add the two new files and remove `Model/DSRRequest.cs`
7. **ROADMAP.md**: no change (milestone unchanged); **ADR**: none (no new pattern; the draft-law gate follows SPEC-002 REQ-024)
8. **PublicAPI**: `PublicAPI.Unshipped.txt` complete
9. **Build**: `dotnet build Encina.slnx --configuration Release` → 0 errors, 0 warnings
10. **Tests**: `dotnet test` → all pass; every coverage flag of the touched files at its manifest target

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #814 is implemented (Phases 1-4): DSRRejectionReason, DSRRequestDenied with code and
explanation, EnableOmnibusArticle12RefusalGrounds (default false), dsr.rejection_reason_not_enabled,
instrumented deny path, Model/DSRRequest.cs removed.

TASK:
1. Add changelog.d/814-dsar-rejection-reasons.added.md and .changed.md (see changelog.d/README.md).
2. Update src/Encina.Compliance.DataSubjectRights/README.md (options, errors, articles, lifecycle).
3. Update docs/features/data-subject-rights.md: replace the stale store.UpdateStatusAsync rejection
   example with IDSRService.DenyRequestAsync and add a refusal-reasons table (code, article,
   current law or draft, option) plus the note that P-02 (#1188) reuses the codes.
4. Update docs/INVENTORY.md.
5. Build Release with zero warnings; run all tests.

KEY RULES:
- Follow .claude/skills/encina-docs/SKILL.md (one Diataxis quadrant per page, identifiers verified in
  src/, no hand-typed coverage figures).
- Never edit CHANGELOG.md [Unreleased] by hand.
- English only; no AI attribution.

REFERENCE FILES:
- changelog.d/README.md
- src/Encina.Compliance.DataSubjectRights/README.md
- docs/features/data-subject-rights.md
- docs/INVENTORY.md
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (section 3.5, REQ-024, AC-024)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| GDPR Art. 12(4) | Inform the subject of the reasons for not taking action, Art. 77 complaint and Art. 79 remedy | Explanation stays mandatory (`RejectionExplanation`) |
| GDPR Art. 12(5) | Manifestly unfounded or excessive requests (in particular repetitive): charge a fee or refuse; controller bears the burden of demonstration | `ManifestlyUnfounded`, `Excessive`; explanation records the demonstration |
| GDPR Art. 12(2) with Art. 11(2) | Refusal only if the controller demonstrates it cannot identify the subject | `IdentityNotVerifiable` |
| GDPR Art. 12(6) | Controller may request additional information to confirm identity | Precedes `IdentityNotVerifiable`; not itself a refusal ground |
| GDPR Art. 23 | Restrictions by Union or Member State law | `RestrictedByLaw` |
| GDPR Art. 17(3), 20(1), 21(1) | Conditions or exemptions of specific rights | `RightNotApplicable` (per-category detail is P-02, #1188) |
| COM(2025) 837 (Digital Omnibus), draft amendment of GDPR Art. 12(5) | Refusal of access requests used for purposes other than data protection | `AbusiveIntent`, off by default (SPEC-002 §3.5, REQ-024, DEC-012, AC-024) |
| SPEC-002 REQ-061, REQ-062 | Tenant-aware, instrumented, no personal data in telemetry | Phase 3 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `DSRRequestAggregate.Deny` | `src/Encina.Compliance.DataSubjectRights/Aggregates/DSRRequestAggregate.cs:261` | Gains the code parameter |
| `DSRRequestDenied` | `src/Encina.Compliance.DataSubjectRights/Events/DSRRequestEvents.cs:120` | Persisted shape carrying the code |
| `DSRRequestProjection` / `DSRRequestReadModel` | `Projections/DSRRequestProjection.cs:145`, `Projections/DSRRequestReadModel.cs:113` | Query side |
| `DataSubjectRightsOptions` | `src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs` | Hosts the draft-law option |
| `DSRErrors` | `src/Encina.Compliance.DataSubjectRights/DSRErrors.cs` | New error code next to `InvalidRequestCode` (`:72`) |
| `RequestsDeniedTotal`, `StartAggregateCommand` | `Diagnostics/DataSubjectRightsDiagnostics.cs:72`, `:164` | Dormant today; wired by Phase 3 |
| `DSRLogMessages` | `Diagnostics/DSRLogMessages.cs` | EventIds 8311-8312 |
| `MartenFixture` / `MartenCollection` | `tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs` | Marten round-trip test |
| `ErasureExemption` | `src/Encina.Compliance.DataSubjectRights/Model/ErasureExemption.cs` | Stays the Art. 17(3) list; P-02 pairs it with the new codes |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.DataSubjectRights` | 8300-8349 (`EventIdRanges.ComplianceDSR`, `src/Encina/Diagnostics/EventIdRanges.cs:316`) | Registered and mapped (`EncinaEventIdAllocationTests.cs:66`). Used: 8300-8303, 8310, 8320-8349. This plan takes **8311** (`DSRRequestDenied`) and **8312** (`DSRRejectionReasonNotEnabled`), next to the lifecycle group 8320-8322; no new range needed. Free after this plan: 8304-8309, 8313-8319 |
| `Encina.Compliance.LawfulBasis` | 8350-8399 | Adjacent; not touched |

### File Count Estimate

| Category | Files | Notes |
|----------|-------|-------|
| New production files | 2 | `DSRRejectionReason.cs`, `DSRRejectionReasonExtensions.cs` |
| Changed production files | 8 | event, aggregate, read model, projection, `IDSRService`, `DefaultDSRService`, options, errors (+ diagnostics, log messages) |
| Deleted production files | 1 | `Model/DSRRequest.cs` |
| PublicAPI / manifest | 2 | `PublicAPI.Unshipped.txt`, coverage manifest |
| Tests | ~10 | unit (5), guard (3), contract (1), property (1), integration (1 new) |
| Documentation | ~5 | README, feature page, INVENTORY, 2 changelog fragments |
| **Total** | **~28** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #814 — DSAR standardized rejection reasons — in Encina.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Railway Oriented Programming: Either<EncinaError, T>; no exceptions for invalid input at the
  service boundary.
- Encina.Compliance.DataSubjectRights is event-sourced on Marten (ADR-019); no 10-provider rule.
- SPEC-002 REQ-024 / DEC-012 / AC-024: a provision of the Digital Omnibus draft (COM(2025) 837) is
  off by default, named by its draft article, and a test asserts that the default follows current law.

IMPLEMENTATION OVERVIEW:
Phase 1: DSRRejectionReason enum (ManifestlyUnfounded=1, Excessive=2, IdentityNotVerifiable=3,
  RestrictedByLaw=4, RightNotApplicable=5, Other=99, AbusiveIntent=100 draft) + C# 14 extension
  block (IsDraftLawGround, LegalBasis); DSRRequestDenied(RequestId, RejectionReason (enum),
  RejectionExplanation, DeniedAtUtc, TenantId, ModuleId); aggregate/read model/projection carry
  both; delete the dead Model/DSRRequest.cs.
Phase 2: IDSRService.DenyRequestAsync(Guid, DSRRejectionReason, string, CancellationToken);
  DataSubjectRightsOptions.EnableOmnibusArticle12RefusalGrounds (false); DSRErrors
  dsr.rejection_reason_not_enabled; DefaultDSRService validates input (dsr.invalid_request) and
  gates draft grounds.
Phase 3: dsr.requests.denied.total with dsr.right_type + dsr.rejection_reason; activity via
  StartAggregateCommand("Deny") with tenant attribute; [LoggerMessage] 8311 and 8312.
Phase 4: manifest targets; unit, guard, contract, property and Marten integration tests; AC-024
  default test.
Phase 5: changelog fragments, README, docs/features/data-subject-rights.md, INVENTORY.

KEY PATTERNS:
- Aggregate validates domain invariants (defined enum, non-blank explanation, terminal status);
  the service owns configuration (draft gate) and maps invalid input to Left.
- No subject id, explanation text or EncinaError.Message in tags, metrics or logs.
- EventIds only in ComplianceDSR 8300-8349.
- CRAP <= 10 on changed methods; per-flag coverage targets in the manifest.
- Shouldly via Encina.Testing.Shouldly; Marten integration tests on [Collection(MartenCollection.Name)].

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Aggregates/DSRRequestAggregate.cs
- src/Encina.Compliance.DataSubjectRights/Events/DSRRequestEvents.cs
- src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs
- src/Encina.Compliance.DataSubjectRights/Diagnostics/DataSubjectRightsDiagnostics.cs
- src/Encina.Compliance.DataSubjectRights/Diagnostics/DSRLogMessages.cs
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (section 3.5, REQ-024, AC-024, DEC-012)
- docs/plans/dsar-rejection-reasons-implementation-plan-814.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Refusal is a write; the existing `dsr:request:{id}` invalidation after deny (`DefaultDSRService.cs:286`) is kept unchanged |
| 2 | OpenTelemetry | ✅ Phase 3 | Activity `DSR.AggregateCommand` (operation `Deny`, reason, tenant attribute) and the dormant `dsr.requests.denied.total` counter wired with the reason tag; the other lifecycle commands are deferred to `artifacts/issues/plan-814-dsr-lifecycle-telemetry.md` |
| 3 | Structured Logging | ✅ Phase 3 | `[LoggerMessage]` EventIds 8311-8312 inside `ComplianceDSR` (ADR-021); replaces the `LogDebug` template |
| 4 | Health Checks | ❌ N/A | No new dependency; the existing `DataSubjectRightsHealthCheck` covers the store |
| 5 | Validation | ✅ Phase 2 | Input validation at the service boundary (defined enum, non-blank explanation, draft gate) returning `Left`; aggregate invariants as domain guards |
| 6 | Resilience | ❌ N/A | No external call added; Marten access is unchanged |
| 7 | Distributed Locks | ❌ N/A | Single-aggregate command; Marten optimistic concurrency on the stream already serialises concurrent denies |
| 8 | Transactions | ❌ N/A | One event appended to one stream by the existing `SaveAsync`; atomic by construction |
| 9 | Idempotency | ❌ N/A | A second deny on a `Rejected` request is refused by the terminal-status check (`DSRRequestAggregate.cs:263`) |
| 10 | Multi-Tenancy | ✅ Phase 3 | `TenantId` stays on `DSRRequestDenied` and the read model; tenant id added as an activity attribute (REQ-061/062); broader DSR tenancy is #1257 |
| 11 | Module Isolation | ✅ Phase 1 | `ModuleId` stays on the event and read model; no new scoping |
| 12 | Audit Trail | ✅ Phase 1 | The `DSRRequestDenied` event is the immutable audit record (Art. 5(2), ADR-019) and now carries the code and the explanation |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `Encina.Compliance.DataSubjectRights` on Marten (#404, #778) | ✅ Closed | Aggregate, event, projection exist |
| `MartenFixture` shared collection | ✅ Available | Used by Consent integration tests |

No blocking prerequisites.

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| P-02 erasure arbitration | [#1188](https://github.com/dlrivada/Encina/issues/1188) (open) | Should land **after** #814 and reuse `DSRRejectionReason` in its per-category outcome (Design Choice 4) |
| P-09 representation fields | [#1197](https://github.com/dlrivada/Encina/issues/1197) (open) | Changes the same event family; sequence the two PRs to avoid conflicts in `DSRRequestEvents.cs` |
| P-07 access withholding reasons | [#1195](https://github.com/dlrivada/Encina/issues/1195) (open) | Its withholding reasons are per field, not per request; keep vocabularies distinct but cross-reference in docs |
| P-17 DSR article coverage | [#1212](https://github.com/dlrivada/Encina/issues/1212) (open) | Cites `DSRRejectionReason` and the draft gate |
| Integration tests for DSR | [#1204](https://github.com/dlrivada/Encina/issues/1204) (open) | Phase 4 creates the first DSR Marten integration test; #1204 extends it |

### Findings recorded as issue drafts

- `artifacts/issues/plan-814-dsr-lifecycle-telemetry.md` — six lifecycle counters and `StartAggregateCommand` are declared but never used.
- `artifacts/issues/plan-814-dsr-inert-options.md` — `DefaultDeadlineDays`, `MaxExtensionDays`, `PublishNotifications` and `TrackAuditTrail` are validated or documented but never read; the aggregate hard-codes 30 days (`DSRRequestAggregate.cs:171`) and 2 months (`:301`).

---

## Next Steps

1. Maintainer decides Design Choices 1-6 (each is a recommendation).
2. Link this plan from #814; add a comment on #1188 that P-02 reuses `DSRRejectionReason` (Design Choice 4).
3. Open the two issue drafts under `artifacts/issues/plan-814-*.md`.
4. Implement Phases 1-5 in one worktree (one `issue-worker`); the PR references `Fixes #814` and records the ADR-018 evaluation above.
5. Close before `1.0.0-rc.1` (AC-024).
