# Implementation Plan: `Encina.Compliance.Consent` — Consent-Exempt Purposes (Digital Omnibus draft GDPR Art. 88a)

> **Issue**: [#811](https://github.com/dlrivada/Encina/issues/811)
> **Type**: Feature
> **Milestone**: v0.15.0 — EU Compliance: NIS2 & Digital Omnibus (parent EPICs #880, #1186)
> **Complexity**: Medium (9 phases, one package, no database provider matrix)
> **Estimated Scope**: ~800-1,000 lines of production code + ~1,200-1,500 lines of tests

---

## Summary

Add **consent-exempt purposes** to `Encina.Compliance.Consent`: purposes that an application declares as not needing the data subject's consent, which `ConsentRequiredPipelineBehavior` lets through without a consent record while still writing a durable, per-subject audit record (`ConsentExemptionApplied`) instead of a `ConsentGranted` event.

### What the issue asks and what SPEC-002 adds

- The issue body asks for `options.DefineExemptPurpose(...)`, a `ConsentExemptionApplied` event, solicitation that skips exempt purposes, `WellKnownExemptPurposes.AggregatedMeasurement` and `.ServiceSecurity`, and tests.
- The maintainer's comment on the issue (SPEC-002 REQ-024, DEC-012, AC-024, 2026-09-24) makes the draft behaviour **off by default** while COM(2025) 837 is not adopted:
  - the draft behaviour is named by its draft article in the XML documentation and in the package specification;
  - a test asserts that the default follows current law;
  - a test shows that the persisted state, events and model values this issue adds are written and read back unchanged while the option is off (the shape exists, the behaviour does not act);
  - the issue must close before `1.0.0-rc.1`.
- SPEC-002 §3.5 (line 136 of `docs/specifications/SPEC-002-eu-regulatory-readiness.md`) maps the first-party audience-measurement exemption to draft **GDPR Art. 88a(3)(c)**, applying 6 months after entry into force. The letter of the security exemption is not given in SPEC-002 and must be verified against the proposal text before the XML documentation names it.
- SPEC-002 REQ-024 (line 308) says the current-law default is "no consent-exempt purposes beyond current law", and the ePrivacy row of §3 (line 90) lists "enumerable strictly-necessary exemptions" as an Encina facility. Design Choice 2 decides whether the current-law exemptions (ePrivacy Dir. 2002/58/EC Art. 5(3)) are part of this issue.

### Current state of the code (nothing of #811 exists yet)

- Every purpose needs consent. `ConsentRequiredPipelineBehavior.Handle` (`src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs:85-192`) sends every purpose of `[RequireConsent]` to `IConsentValidator.ValidateAsync` (line 140); there is no exemption branch.
- `DefaultConsentValidator.ValidateAsync` (`src/Encina.Compliance.Consent/DefaultConsentValidator.cs:50-122`) looks up a `ConsentReadModel` per purpose and fails a purpose that has none (line 77).
- `ConsentOptions.DefinePurpose` (`src/Encina.Compliance.Consent/ConsentOptions.cs:264-275`) and `PurposeDefinitionEntry` (lines 287-327) carry only `Description`, `RequiresExplicitOptIn`, `CanBeWithdrawnAnytime` and `DefaultExpirationDays`. `DetailedPurposeDefinitions` is read only by `ConsentOptionsValidator` (lines 53-68); nothing reads it at runtime.
- The event-sourced model is one `ConsentAggregate` per consent (`src/Encina.Compliance.Consent/Aggregates/ConsentAggregate.cs:34`), created with a random id (`Services/DefaultConsentService.cs:160`), projected by `ConsentProjection` into `ConsentReadModel` (`ReadModels/ConsentProjection.cs:30-38`). Events are in `Events/ConsentEvents.cs` (six records, none for exemption). `ConsentStatus` (`Model/ConsentStatus.cs:17-55`) has four values.
- There is no solicitation API. `IConsentService` (`Abstractions/IConsentService.cs:40-207`) has grant, withdraw, renew, reconsent and five queries; nothing answers "should the application ask this subject for consent to this purpose?". #810 (open) proposes the same kind of answer for its cooldown (`CooldownActive`).
- `Encina.Compliance.Consent` does not reference `Encina.Compliance.GDPR` (`Encina.Compliance.Consent.csproj:11-17`), where `LawfulBasis` lives (`src/Encina.Compliance.GDPR/Model/LawfulBasis.cs:17`). The issue's `p.LegalBasis = LawfulBasis.LegitimateInterests` needs either that reference or another model (Design Choice 4).
- `Encina.Marten`'s `MartenAggregateRepository.CreateAsync` returns `MartenErrorCodes.StreamAlreadyExists` on a stream-id collision (`src/Encina.Marten/MartenAggregateRepository.cs:311-319`), which makes a deterministic stream id usable as an idempotency key (Design Choice 3).
- Event IDs: the package range is `ComplianceConsent = (8200, 8299)` (`src/Encina/Diagnostics/EventIdRanges.cs:313`); the highest used id is 8268 (`Diagnostics/ConsentLogMessages.cs:299`). The assembly is already mapped in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs:63`.
- Coverage manifest `.github/coverage-manifest/Encina.Compliance.Consent.json` has package targets (unit 70, guard 20, contract 15, property 15) but no per-file `targets`/`justifications`; every new or touched file needs them (AGENTS.md §9).

### Scope

- **Affected package**: `Encina.Compliance.Consent` only (plus its tests, README, feature guide and coverage manifest).
- **Provider category**: none of the AGENTS.md §5 matrices. Consent is event-sourced on Marten (PostgreSQL); the database rule excludes Marten (AGENTS.md §5), and AGENTS.md §3 requires event-sourced compliance modules to unit-test with a mocked `IAggregateRepository` and integration-test on Marten through Testcontainers.
- **Estimated files**: ~13 new source files, ~12 modified source files, ~12 test files, ~5 documentation files.

### Related open issues that touch the same files

- #810 (consent cooldown after refusal): same `ConsentOptions`, `IConsentService`, solicitation answer. Coordinate per Prerequisites & Dependencies.
- #1199 (channel-scoped consent), #1197 (representation and deceased status), #1255 (minors): change the consent purpose and event model; they do not block this issue.
- #1209 (Consent article-coverage specification): AC-024 needs the draft article named in the package specification, which #1209 creates.
- #1920 (no `IConsentStatusProvider` bridge for LawfulBasis): unaffected, noted so the bridge treats exemptions consistently when it is built.

---

## Design Choices

<details>
<summary><strong>1. Where exempt purposes are defined — separate <code>DefineExemptPurpose</code> catalogue in <code>ConsentOptions</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Separate `DefineExemptPurpose(purpose, configure)` and an `ExemptPurposeDefinitions` dictionary on `ConsentOptions`, with its own `ExemptPurposeDefinitionEntry`** | Matches the issue's API; an exempt purpose cannot be confused with a consent purpose; the validator can reject a purpose defined both ways; entry carries exemption-only fields (`Basis`, `RequiresAggregation`) without polluting `PurposeDefinitionEntry` | Two catalogues on one options class; auto-registration must accept both as "known" |
| **B) An `IsExempt` flag plus exemption fields on the existing `PurposeDefinitionEntry`** | One catalogue; smallest options change | Consent-only fields (`RequiresExplicitOptIn`, `CanBeWithdrawnAnytime`, `DefaultExpirationDays`) become meaningless for exempt entries; easy to flip a consent purpose to exempt by mistake |
| **C) A separate `IConsentExemptionRegistry` service registered in DI (`services.AddConsentExemption(...)`)** | Runtime-mutable; per-tenant registries possible | A second registration surface for one concept; options validation cannot see it at startup; diverges from the existing `DefinePurpose` style |

### Chosen Option: **A — separate `DefineExemptPurpose` catalogue** (recommended, pending the maintainer)

### Rationale

- I recommend A because the issue already specifies `options.DefineExemptPurpose("aggregated-measurement", p => ...)`, and it keeps the two legal situations (consent, exemption) in separate types.
- `ConsentOptionsValidator` (`src/Encina.Compliance.Consent/ConsentOptionsValidator.cs:27-73`) can then fail fast when a purpose is in both `PurposeDefinitions` and `ExemptPurposeDefinitions`.
- Runtime decisions go through a public `IConsentExemptionPolicy` (Phase 4) whose default reads these options, so an application that needs per-tenant exemptions overrides the policy instead of needing option C.

</details>

<details>
<summary><strong>2. Draft-law gate and current-law exemptions — a basis enum with the draft bases behind one off-by-default switch</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One master switch `EnableDraftOmnibusExemptions` (default `false`); every exempt purpose is inert while it is off** | Simplest; obviously follows current law by default (SPEC-002 INV-003) | An application cannot use the current-law ePrivacy Art. 5(3) strictly-necessary exemption that SPEC-002 §3 lists as an Encina facility; the whole feature is dead code until adoption |
| **B) A `ConsentExemptionBasis` enum on each definition: current-law bases (ePrivacy Art. 5(3) transmission, strictly necessary for a service the subject requested) act by default; draft bases (Art. 88a audience measurement, security) act only when `EnableDraftOmnibusExemptions` is `true`** | Follows current law by default and still delivers what current law allows; the basis is recorded in each `ConsentExemptionApplied`, so the audit says *why* no consent was asked; adoption of COM(2025) 837 becomes a default flip, not a model change | Larger scope than the issue body; overlaps the terminal-storage channel of #1199 (P-11); the enum names legal provisions that need careful XML documentation |
| **C) A per-definition `IsDraftLaw` flag the application sets** | Flexible | Puts the legal classification on the application; nothing stops a draft exemption being marked current law; contradicts REQ-024 ("named by its draft article") |

### Chosen Option: **B — basis enum, draft bases gated** (recommended, pending the maintainer)

### Rationale

- I recommend B because REQ-024 (SPEC-002 line 308) states the current-law default as "no consent-exempt purposes **beyond** current law", which presumes current-law ones exist, and SPEC-002 §3 line 90 lists "enumerable strictly-necessary exemptions" for ePrivacy.
- The basis enum is the place where the draft article is named in XML documentation (AC-024), one member per provision.
- A draft-basis definition while the switch is off is accepted (the shape exists) and logged as a startup warning, never silently used; turning the switch on is logged too (SPEC-002 DEC-006 style: explicit opt-in, never silent).
- If the maintainer prefers A, Phase 1 drops the two current-law members and the rest of the plan is unchanged.

</details>

<details>
<summary><strong>3. Persistence and audit of an applied exemption — a separate <code>ConsentExemptionAggregate</code> per tenant, subject and purpose</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Append a `ConsentExemptionApplied` event on every request that uses an exempt purpose** | Complete per-request trail | Write on the hot path of every request; event volume grows with traffic (aggregated measurement fires on most requests); the trail duplicates what logs and traces already give |
| **B) Record it once per (tenant, subject, purpose) on a `ConsentAggregate` stream with a new `ConsentStatus.Exempt`** | Reuses the projection and `GetAllConsentsAsync`, so Art. 15 access exports show exemptions for free | Changes a persisted enum used in 58 test assertions; `GetConsentBySubjectAndPurposeAsync` (`Services/DefaultConsentService.cs:507-519`) takes `readModels[0]` and could return the exemption instead of a later real consent; every `ConsentAggregate` transition needs an Exempt guard |
| **C) Record it once per (tenant, subject, purpose) on a new `ConsentExemptionAggregate` stream with its own projection and `ConsentExemptionReadModel`; deterministic stream id makes the write idempotent** | Existing consent queries and states untouched; one write per subject and purpose, then cached; the event carries purpose, basis, definition version and timestamps, which is what an auditor needs; duplicate concurrent writes collapse on `StreamAlreadyExists` (`src/Encina.Marten/MartenAggregateRepository.cs:311-319`) | New aggregate, projection, read model and registration (~5 files); an Art. 15 export must query both read models |
| **D) No per-subject record: log and trace each use, record only the definitions at startup** | Cheapest | Does not meet the issue's acceptance criterion "exemption still recorded in audit trail" for the subject; logs are not an audit store (SPEC-002 REQ-062 forbids subject ids in telemetry) |

### Chosen Option: **C — separate `ConsentExemptionAggregate`** (recommended, pending the maintainer)

### Rationale

- I recommend C because it satisfies "exemption still recorded in audit trail" without touching the meaning of existing consent records and without a write per request.
- The stream id is a name-based UUID (RFC 9562 version 8, SHA-256 of `tenant|subject|purpose` under a fixed namespace), so two concurrent first requests produce one stream; `StreamAlreadyExists` is treated as "already recorded", not as a failure.
- The aggregate also records `ConsentExemptionRevoked` when the application stops relying on the exemption for a subject (for example after an Art. 21 objection handled by the application), so the read model can say whether the exemption is currently relied on.
- AC-024's round-trip test is natural here: with the draft switch off, `IConsentExemptionService.RecordExemptionAsync` still writes and reads back the event and read model, while the pipeline does not use draft-basis exemptions.

</details>

<details>
<summary><strong>4. Legal basis on an exempt purpose — record the exempting provision, not the GDPR <code>LawfulBasis</code> enum</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reference `Encina.Compliance.GDPR` and expose `LawfulBasis LegalBasis` on `ExemptPurposeDefinitionEntry`, as in the issue body** | Matches the issue text; reuses the Art. 6 enum | New package dependency for Consent (GDPR today depends on nothing from Consent and defines `IConsentStatusProvider` precisely to avoid coupling, `src/Encina.Compliance.GDPR/Abstractions/IConsentStatusProvider.cs`); under draft Art. 88a the exemption itself is the ground for not asking consent, so an Art. 6 value alone misdescribes it; #1196 is remodelling legal bases |
| **B) The `ConsentExemptionBasis` enum of Design Choice 2 is the recorded ground, plus an optional free-text `LawfulBasisReference` string (for example "GDPR Art. 6(1)(f)")** | No new dependency; the audit names the provision that exempts; the Art. 6 note stays available for the RoPA | The Art. 6 reference is a string, not a typed value; the issue's sample code changes |
| **C) A local copy of the Art. 6 enum inside Consent** | Typed, no dependency | Duplicated domain type that drifts from GDPR's and from #1196 |

### Chosen Option: **B — exempting provision plus an optional Art. 6 reference string** (recommended, pending the maintainer)

### Rationale

- I recommend B because what an auditor needs to see on an exemption is the provision that dispenses with consent; the Art. 6 basis of the subsequent processing belongs to `Encina.Compliance.LawfulBasis` and the RoPA, where #1196 is already reshaping it.
- It keeps the dependency direction the GDPR package chose (`IConsentStatusProvider` exists so GDPR does not depend on Consent; Consent depending on GDPR would make the pair tightly coupled for one enum).
- The issue's sample becomes `p.Basis = ConsentExemptionBasis.AudienceMeasurement; p.LawfulBasisReference = "GDPR Art. 6(1)(f)";`; the README shows the mapping.

</details>

<details>
<summary><strong>5. Pipeline integration — partition the purposes in <code>ConsentRequiredPipelineBehavior</code>; keep the validator a pure consent check</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `DefaultConsentValidator` treats active exempt purposes as satisfied and returns them in a new `ConsentValidationResult.ExemptPurposes` list; the pipeline records them** | Applications calling `IConsentValidator` directly see exemptions too | The validator answers "is there valid consent?", and for an exempt purpose the honest answer is "no consent, none needed"; mixing the two blurs the contract test of `IConsentValidator`; the validator would need write access to record |
| **B) The pipeline asks `IConsentExemptionPolicy` which purposes are actively exempt, records those through `IConsentExemptionService`, and sends only the remaining purposes to `IConsentValidator`** | Validator contract unchanged; one decision point (the policy) shared with the solicitation answer; a request mixing consent and exempt purposes still needs consent for the consent ones | The policy is called on every request with `[RequireConsent]` (a dictionary lookup, cached definitions) |
| **C) A separate `ConsentExemptionPipelineBehavior` registered before the consent behavior** | Isolated | Two behaviors must agree on which purposes the other handles; ordering dependency between behaviors; double attribute scan |

### Chosen Option: **B — partition in the existing behavior** (recommended, pending the maintainer)

### Rationale

- I recommend B because it keeps `IConsentValidator` and `DefaultConsentValidator` unchanged and puts the legal decision in one public, overridable policy.
- Failure to record an exemption is handled like a failed consent check: in `Block` mode the request fails with the store's error (AGENTS.md §3, compliance gates fail closed, errors never swallowed); in `Warn` mode it is logged and the request proceeds; `Disabled` skips everything as today (`ConsentRequiredPipelineBehavior.cs:99-103`).
- A request whose purposes are all exempt never calls the validator, so it adds no consent-store read.

</details>

<details>
<summary><strong>6. Solicitation answer — <code>IConsentService.GetSolicitationDecisionAsync</code>, shared with #810</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `GetSolicitationDecisionAsync(subjectId, purpose)` on `IConsentService`, returning `ConsentSolicitationDecision` (`Solicit`, `ConsentActive`, `Exempt`); #810 later adds `CooldownActive`** | Gives the issue's "exempt purposes bypass consent prompt" a concrete API; one place where a consent banner asks; #810 extends the same enum instead of inventing a second API | Changes a public interface (pre-1.0, allowed); needs coordination with #810 on the enum and method name |
| **B) Synchronous `IConsentExemptionPolicy.IsExempt(purpose)` only; the application combines it with `HasValidConsentAsync`** | Smallest | Every application re-implements the combination; #810 would add its own query, giving two half-answers |
| **C) No API; the application reads `ConsentOptions.ExemptPurposeDefinitions`** | Nothing to build | Ignores the draft gate and any overridden policy; the acceptance criterion "exempt purposes bypass consent prompt" is not testable in Encina |

### Chosen Option: **A — one solicitation answer on `IConsentService`** (recommended, pending the maintainer)

### Rationale

- I recommend A because the acceptance criterion talks about the consent prompt, and Encina has no prompt-side API today; a single tri-state answer is what a cookie or consent banner needs.
- Whichever of #811 and #810 lands first defines `ConsentSolicitationDecision` and the method; the second adds its member. Pre-1.0 an added enum member is free (AGENTS.md §1).
- The decision reads the policy (no store call) for `Exempt`, then `HasValidConsentAsync` (`Services/DefaultConsentService.cs:558-607`) for `ConsentActive`, so tenant scoping and fail-closed behaviour come from the existing query.

</details>

---

## Implementation Phases

### Phase 1: Core Models, Enums & Events

> **Goal**: The public shapes of the feature: exemption basis, well-known purposes, solicitation decision and the exemption events.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Compliance.Consent/Model/ConsentExemptionBasis.cs`** (new), namespace `Encina.Compliance.Consent`:
   - `public enum ConsentExemptionBasis`
   - `CommunicationTransmission` — current law, ePrivacy Dir. 2002/58/EC Art. 5(3), first exemption (sole purpose of carrying out the transmission).
   - `StrictlyNecessaryService` — current law, ePrivacy Art. 5(3), second exemption (strictly necessary for a service explicitly requested by the subscriber or user).
   - `AudienceMeasurement` — **draft**, GDPR Art. 88a(3)(c) as proposed by COM(2025) 837 (SPEC-002 §3.5); first-party aggregated audience measurement.
   - `ServiceSecurity` — **draft**, GDPR Art. 88a(3) as proposed by COM(2025) 837; the letter is verified against the proposal text before the XML documentation names it.
   - XML docs on each member: provision, current or draft, and "off by default while COM(2025) 837 is not adopted" on the draft members.
   - Extension or static helper `internal static bool IsDraftLaw(this ConsentExemptionBasis basis)`.
   - (If the maintainer picks Design Choice 2 option A, drop the two current-law members.)
2. **`src/Encina.Compliance.Consent/Model/WellKnownExemptPurposes.cs`** (new): `public static class WellKnownExemptPurposes` with `AggregatedMeasurement = "aggregated-measurement"` and `ServiceSecurity = "service-security"`; XML docs naming the basis each one is meant for. Follows `Model/ConsentPurposes.cs`.
3. **`src/Encina.Compliance.Consent/Model/ConsentSolicitationDecision.cs`** (new): `public enum ConsentSolicitationDecision { Solicit, ConsentActive, Exempt }` with XML docs; a remark that #810 adds `CooldownActive`.
4. **`src/Encina.Compliance.Consent/Events/ConsentExemptionEvents.cs`** (new), namespace `Encina.Compliance.Consent.Events`, both `sealed record ... : INotification` like `Events/ConsentEvents.cs`:
   - `ConsentExemptionApplied(Guid ExemptionId, string DataSubjectId, string Purpose, ConsentExemptionBasis Basis, string? LawfulBasisReference, bool RequiresAggregation, string AppliedBy, DateTimeOffset OccurredAtUtc, string? TenantId, string? ModuleId)`
   - `ConsentExemptionRevoked(Guid ExemptionId, string DataSubjectId, string Purpose, string RevokedBy, string? Reason, DateTimeOffset OccurredAtUtc)`
5. **`PublicAPI.Unshipped.txt`**: every new public symbol.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of consent-exempt purposes in Encina.Compliance.Consent (issue #811).

CONTEXT:
- Encina is a .NET 10 / C# 14 library, nullable enabled, Railway Oriented Programming (Either<EncinaError, T>).
- Package: src/Encina.Compliance.Consent/ (event-sourced on Marten). No exemption concept exists today.
- SPEC-002 REQ-024 / DEC-012 / AC-024: the draft-law behaviour (COM(2025) 837, draft GDPR Art. 88a) is off by default and named by its draft article in XML docs.
- SPEC-002 section 3.5 (docs/specifications/SPEC-002-eu-regulatory-readiness.md, line 136) maps audience measurement to draft GDPR Art. 88a(3)(c). Verify the letter of the security exemption against the proposal text; if you cannot, write "draft GDPR Art. 88a(3)" without a letter.

TASK:
Create Model/ConsentExemptionBasis.cs (CommunicationTransmission, StrictlyNecessaryService, AudienceMeasurement, ServiceSecurity plus internal IsDraftLaw helper), Model/WellKnownExemptPurposes.cs, Model/ConsentSolicitationDecision.cs (Solicit, ConsentActive, Exempt) and Events/ConsentExemptionEvents.cs (ConsentExemptionApplied, ConsentExemptionRevoked) exactly as listed in the plan's Phase 1 Tasks. Add the public symbols to PublicAPI.Unshipped.txt.

KEY RULES:
- Events are sealed records implementing INotification, timestamps DateTimeOffset with the AtUtc suffix, TenantId and ModuleId nullable strings, like ConsentGranted.
- XML docs on every public member: summary, remarks naming the legal provision and whether it is current law or draft, and "off by default" on draft members.
- No [Obsolete], no compatibility shims; English only.
- Zero warnings; PublicAPI analyzers (RS0016) must be clean.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Events/ConsentEvents.cs
- src/Encina.Compliance.Consent/Model/ConsentPurposes.cs
- src/Encina.Compliance.Consent/Model/ConsentStatus.cs
- src/Encina.Compliance.Consent/PublicAPI.Unshipped.txt
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (sections 3.5, REQ-024, AC-024, DEC-012, INV-003)
```

</details>

---

### Phase 2: Configuration & Options Validation

> **Goal**: `DefineExemptPurpose`, the draft switch, and fail-fast validation.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Compliance.Consent/ConsentOptions.cs`** (modify):
   - `public bool EnableDraftOmnibusExemptions { get; set; }` (default `false`), XML docs naming COM(2025) 837 and draft GDPR Art. 88a and stating that the default follows current law.
   - `public Dictionary<string, ExemptPurposeDefinitionEntry> ExemptPurposeDefinitions { get; } = new(StringComparer.Ordinal);`
   - `public ConsentOptions DefineExemptPurpose(string purpose, Action<ExemptPurposeDefinitionEntry>? configure = null)` — `ArgumentException.ThrowIfNullOrWhiteSpace(purpose)`, stores the entry, returns `this`. It does **not** add the purpose to `PurposeDefinitions`.
   - Nested `public sealed class ExemptPurposeDefinitionEntry`: `string? Description`, `ConsentExemptionBasis Basis` (default `StrictlyNecessaryService`), `string? LawfulBasisReference`, `bool RequiresAggregation`.
   - Update the class `<example>` with one current-law and one draft exempt purpose.
2. **`src/Encina.Compliance.Consent/ConsentOptionsValidator.cs`** (modify), new failures:
   - a purpose present in both `PurposeDefinitions`/`DetailedPurposeDefinitions` and `ExemptPurposeDefinitions`;
   - an empty purpose key in `ExemptPurposeDefinitions`;
   - `RequiresAggregation = true` with a basis other than `AudienceMeasurement`;
   - the Block-mode "no purposes" rule (lines 42-50) also counts `ExemptPurposeDefinitions`.
   - A draft basis with `EnableDraftOmnibusExemptions = false` is **not** a failure (the shape exists); it is logged at startup in Phase 7.
3. **`src/Encina.Compliance.Consent/ConsentAutoRegistrationHostedService.cs`** (modify `ValidatePurposes`, lines 114-140): a discovered purpose in `ExemptPurposeDefinitions` is known.
4. **`PublicAPI.Unshipped.txt`**: new members.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of consent-exempt purposes in Encina.Compliance.Consent (issue #811).

CONTEXT:
- Phase 1 added ConsentExemptionBasis, WellKnownExemptPurposes, ConsentSolicitationDecision and the exemption events.
- ConsentOptions (src/Encina.Compliance.Consent/ConsentOptions.cs) has DefinePurpose (lines 264-275) and the nested PurposeDefinitionEntry (lines 287-327).
- ConsentOptionsValidator (ConsentOptionsValidator.cs) validates expiration days and Block-mode purpose presence.
- ConsentAutoRegistrationHostedService.ValidatePurposes (lines 114-140) treats any purpose outside PurposeDefinitions as unknown.

TASK:
Add EnableDraftOmnibusExemptions (default false), ExemptPurposeDefinitions, DefineExemptPurpose and the nested ExemptPurposeDefinitionEntry (Description, Basis, LawfulBasisReference, RequiresAggregation) to ConsentOptions. Extend ConsentOptionsValidator with the four rules of the plan's Phase 2 Tasks. Make auto-registration accept exempt purposes as known. Update PublicAPI.Unshipped.txt.

KEY RULES:
- The default of EnableDraftOmnibusExemptions is false and its XML docs name COM(2025) 837 and draft GDPR Art. 88a (SPEC-002 REQ-024, AC-024).
- A draft-basis definition while the switch is off is valid configuration (it is logged later, never rejected).
- DefineExemptPurpose never adds to PurposeDefinitions; a purpose defined both ways is a validation failure.
- Fluent method returns this; guard with ArgumentException.ThrowIfNullOrWhiteSpace.
- Zero warnings; XML docs on every public member.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ConsentOptions.cs
- src/Encina.Compliance.Consent/ConsentOptionsValidator.cs
- src/Encina.Compliance.Consent/ConsentAutoRegistrationHostedService.cs
- tests/Encina.UnitTests/Compliance/Consent/ConsentOptionsValidatorTests.cs
```

</details>

---

### Phase 3: Exemption Aggregate, Projection & Read Model

> **Goal**: The persisted shape: one event stream per (tenant, subject, purpose), projected into a queryable read model.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Compliance.Consent/Aggregates/ConsentExemptionAggregate.cs`** (new), `public sealed class ConsentExemptionAggregate : AggregateBase` (same base as `ConsentAggregate`):
   - Properties: `DataSubjectId`, `Purpose`, `Basis`, `LawfulBasisReference`, `RequiresAggregation`, `AppliedAtUtc`, `RevokedAtUtc?`, `RevocationReason?`, `IsRevoked`, `TenantId?`, `ModuleId?`.
   - `public static ConsentExemptionAggregate Apply(Guid id, string dataSubjectId, string purpose, ConsentExemptionBasis basis, string? lawfulBasisReference, bool requiresAggregation, string appliedBy, DateTimeOffset occurredAtUtc, string? tenantId = null, string? moduleId = null)` (name it `Record` if `Apply` collides with the base `Apply(object)`), raises `ConsentExemptionApplied`.
   - `public void Revoke(string revokedBy, string? reason, DateTimeOffset occurredAtUtc)`; throws `InvalidOperationException` when already revoked.
   - `public void Reapply(string appliedBy, ConsentExemptionBasis basis, DateTimeOffset occurredAtUtc)` raises `ConsentExemptionApplied` again on a revoked stream (the stream id is deterministic, so a revoked exemption is re-applied on the same stream).
   - `protected override void Apply(object domainEvent)` for both events.
2. **`src/Encina.Compliance.Consent/ConsentExemptionStreamId.cs`** (new, `internal static`): `Guid For(string? tenantId, string dataSubjectId, string purpose)` — RFC 9562 version 8 UUID from SHA-256 of `"{tenant or '-'}|{subject}|{purpose}"` under a fixed namespace GUID; length-prefix each part so `"a|b"` and `"a" + "|b"` cannot collide.
3. **`src/Encina.Compliance.Consent/ReadModels/ConsentExemptionReadModel.cs`** (new), `public sealed class ConsentExemptionReadModel : IReadModel`: `Id`, `DataSubjectId`, `Purpose`, `Basis`, `LawfulBasisReference`, `RequiresAggregation`, `AppliedAtUtc`, `RevokedAtUtc?`, `IsRevoked`, `TenantId?`, `ModuleId?`, `LastModifiedAtUtc`, `Version`.
4. **`src/Encina.Compliance.Consent/ReadModels/ConsentExemptionProjection.cs`** (new): `IProjection<ConsentExemptionReadModel>`, `IProjectionCreator<ConsentExemptionApplied, ConsentExemptionReadModel>`, `IProjectionHandler<ConsentExemptionApplied, ...>` (re-apply), `IProjectionHandler<ConsentExemptionRevoked, ...>`; `ProjectionName => "ConsentExemptionProjection"`.
5. **`src/Encina.Compliance.Consent/ConsentMartenExtensions.cs`** (modify `AddConsentAggregates`, lines 50-58): `services.AddAggregateRepository<ConsentExemptionAggregate>(); services.AddProjection<ConsentExemptionProjection, ConsentExemptionReadModel>();`.
6. **`PublicAPI.Unshipped.txt`**.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of consent-exempt purposes in Encina.Compliance.Consent (issue #811).

CONTEXT:
- Consent is event-sourced on Marten: ConsentAggregate (Aggregates/ConsentAggregate.cs), ConsentProjection and ConsentReadModel (ReadModels/), registered by ConsentMartenExtensions.AddConsentAggregates.
- An exemption is recorded once per (tenant, subject, purpose) on its own stream, never on a ConsentAggregate stream, so existing consent queries and ConsentStatus stay unchanged.
- MartenAggregateRepository.CreateAsync returns MartenErrorCodes.StreamAlreadyExists on a stream-id collision (src/Encina.Marten/MartenAggregateRepository.cs:311-319); a deterministic stream id therefore makes the first write idempotent.

TASK:
Create ConsentExemptionAggregate, the internal ConsentExemptionStreamId helper (RFC 9562 version 8 UUID from SHA-256 of length-prefixed tenant, subject and purpose under a fixed namespace), ConsentExemptionReadModel and ConsentExemptionProjection as listed in the plan's Phase 3 Tasks, and register the aggregate repository and projection in AddConsentAggregates.

KEY RULES:
- Mirror ConsentAggregate: static factory raising the creation event, guarded transitions throwing InvalidOperationException, Apply(object) switch.
- Use SHA-256, not SHA-1 or MD5 (no weak-hash analyzer suppressions); set the version (8) and variant bits.
- Marten projections take dependencies through constructor injection or IDocumentOperations, never IServiceProvider (AGENTS.md section 3).
- Event-sourced modules have no in-memory store (AGENTS.md section 3).
- XML docs on every public member; zero warnings; PublicAPI.Unshipped.txt updated.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Aggregates/ConsentAggregate.cs
- src/Encina.Compliance.Consent/ReadModels/ConsentProjection.cs
- src/Encina.Compliance.Consent/ReadModels/ConsentReadModel.cs
- src/Encina.Compliance.Consent/ConsentMartenExtensions.cs
- src/Encina.Marten/MartenAggregateRepository.cs
- src/Encina.IdGeneration/Generators/UuidV7IdGenerator.cs (version and variant bit handling)
```

</details>

---

### Phase 4: Exemption Policy, Exemption Service & Solicitation Answer

> **Goal**: The runtime decision ("is this purpose exempt now?"), the record and query operations, and the consent-prompt answer.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Compliance.Consent/Abstractions/IConsentExemptionPolicy.cs`** (new):
   - `bool TryGetActiveExemption(string purpose, string? tenantId, [NotNullWhen(true)] out ExemptPurposeDefinition? exemption)`.
   - `ExemptPurposeDefinition` public sealed record (`Purpose`, `Basis`, `LawfulBasisReference`, `RequiresAggregation`, `Description`) in `Model/`, an immutable snapshot of the options entry.
2. **`src/Encina.Compliance.Consent/Services/DefaultConsentExemptionPolicy.cs`** (new, `internal sealed`): built from `IOptions<ConsentOptions>`; a purpose is actively exempt when it is in `ExemptPurposeDefinitions` and its basis is current law, or its basis is draft and `EnableDraftOmnibusExemptions` is `true`. Snapshot built once in the constructor (frozen dictionary). `tenantId` is ignored by the default (the parameter exists so an application can override per tenant).
3. **`src/Encina.Compliance.Consent/Abstractions/IConsentExemptionService.cs`** (new):
   - `ValueTask<Either<EncinaError, Guid>> RecordExemptionAsync(string dataSubjectId, string purpose, string appliedBy, string? tenantId = null, string? moduleId = null, CancellationToken cancellationToken = default)` — records the defined exemption whether or not it is active (this is what lets the AC-024 test write the shape with the switch off); returns `ConsentErrors.ExemptPurposeNotDefined` for an undefined purpose; `StreamAlreadyExists` maps to `Right(id)`; a revoked stream is re-applied.
   - `ValueTask<Either<EncinaError, Unit>> RevokeExemptionAsync(string dataSubjectId, string purpose, string revokedBy, string? reason = null, CancellationToken cancellationToken = default)`.
   - `ValueTask<Either<EncinaError, Option<ConsentExemptionReadModel>>> GetExemptionAsync(string dataSubjectId, string purpose, CancellationToken cancellationToken = default)`.
   - `ValueTask<Either<EncinaError, IReadOnlyList<ConsentExemptionReadModel>>> GetExemptionsAsync(string dataSubjectId, CancellationToken cancellationToken = default)` (feeds Art. 15 access exports).
4. **`src/Encina.Compliance.Consent/Services/DefaultConsentExemptionService.cs`** (new, `internal sealed`): dependencies `IAggregateRepository<ConsentExemptionAggregate>`, `IReadModelRepository<ConsentExemptionReadModel>`, `ICacheProvider`, `TimeProvider`, `IRequestContextAccessor`, `IOptions<ConsentOptions>`, `ILogger<DefaultConsentExemptionService>`, `ITenantProvider? tenantProvider = null`. Reuses the tenant resolution rules of `DefaultConsentService.TryResolveTenantScope`/`TryResolveWriteTenantScope` (`Services/DefaultConsentService.cs:378-428`): extract them into an `internal sealed class ConsentTenantScope` used by both services instead of duplicating them. Every exception path returns `ConsentErrors.ServiceError` and logs through `ForLogging()`.
5. **`src/Encina.Compliance.Consent/Abstractions/IConsentService.cs`** and **`Services/DefaultConsentService.cs`** (modify): `ValueTask<Either<EncinaError, ConsentSolicitationDecision>> GetSolicitationDecisionAsync(string dataSubjectId, string purpose, CancellationToken cancellationToken = default)` — `Exempt` when the policy says so; otherwise `ConsentActive` when `HasValidConsentAsync` is `true`; otherwise `Solicit`; a `Left` from `HasValidConsentAsync` propagates. `DefaultConsentService` takes `IConsentExemptionPolicy` in its constructor.
6. **`src/Encina.Compliance.Consent/ConsentErrors.cs`** (modify): `ExemptPurposeNotDefinedCode = "consent.exempt_purpose_not_defined"`, `ExemptionNotFoundCode = "consent.exemption_not_found"`, `ExemptionRecordFailedCode = "consent.exemption_record_failed"` with factories; messages never contain the subject id.
7. **`src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs`** (modify `AddEncinaConsent`, lines 74-108): `TryAddSingleton<IConsentExemptionPolicy, DefaultConsentExemptionPolicy>()`, `TryAddScoped<IConsentExemptionService, DefaultConsentExemptionService>()`.
8. **`PublicAPI.Unshipped.txt`**.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of consent-exempt purposes in Encina.Compliance.Consent (issue #811).

CONTEXT:
- Phases 1-3 added the models, options, ConsentExemptionAggregate, its projection and read model.
- DefaultConsentService (Services/DefaultConsentService.cs) shows the house patterns: tenant scope resolution (lines 378-428, fail closed when tenancy is registered and no tenant is present), tenant-scoped cache keys, ForLogging() on exceptions, no subject id in logs or traces (#1314, #1315).
- #810 (open) will add CooldownActive to ConsentSolicitationDecision; keep the decision method easy to extend.

TASK:
Create IConsentExemptionPolicy and its options-based DefaultConsentExemptionPolicy, the ExemptPurposeDefinition snapshot record, IConsentExemptionService and DefaultConsentExemptionService (record, revoke, get one, get all), extract the shared tenant-scope logic into an internal ConsentTenantScope used by both services, add GetSolicitationDecisionAsync to IConsentService and DefaultConsentService, add the three error codes to ConsentErrors, and register everything in AddEncinaConsent with TryAdd*.

KEY RULES:
- A draft-basis exemption is active only when ConsentOptions.EnableDraftOmnibusExemptions is true; current-law bases are active by default.
- RecordExemptionAsync records any defined exemption even when the draft switch is off (AC-024 round-trip), maps StreamAlreadyExists to success, and re-applies a revoked stream.
- Every Left from a repository is returned, never swallowed; exceptions become ConsentErrors.ServiceError (AGENTS.md section 3).
- Time from TimeProvider only; all database calls async with CancellationToken.
- Registration completeness: a DI test builds the provider with ValidateOnBuild and ValidateScopes (AGENTS.md section 3).
- No subject id in logs, metrics, traces or error messages.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- src/Encina.Compliance.Consent/Abstractions/IConsentService.cs
- src/Encina.Compliance.Consent/ConsentErrors.cs
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs
- src/Encina.Marten/MartenErrorCodes.cs
- tests/Encina.UnitTests/Compliance/Consent/ServiceCollectionExtensionsTests.cs
```

</details>

---

### Phase 5: Pipeline Behavior Integration

> **Goal**: `[RequireConsent]` requests let actively exempt purposes through and record the exemption.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs`** (modify):
   - Constructor adds `IConsentExemptionPolicy` and `IConsentExemptionService` by injection (the behavior is transient, `ServiceCollectionExtensions.cs:98`, so a scoped service is safe; no `IServiceProvider`).
   - After subject extraction (line 121), partition `attribute.Purposes` with `IConsentExemptionPolicy.TryGetActiveExemption(purpose, context.TenantId, ...)`.
   - For each exempt purpose: `RecordExemptionAsync(subjectId, purpose, appliedBy: context.UserId ?? subjectId, context.TenantId, ...)`. On `Left`: `Block` returns the error and records failure reason `ConsentErrors.ExemptionRecordFailedCode`; `Warn` logs and continues.
   - Remaining purposes go to `IConsentValidator.ValidateAsync` as today; when none remain, the validator is not called.
   - Activity tag `consent.exempt_purposes` (comma-joined purpose ids) and counter `consent.exemptions.applied` per exempt purpose (Phase 7).
   - Extract the partition and recording into private methods so `Handle` stays at CRAP ≤ 10 (AGENTS.md §9).
2. Behaviour with `EnforcementMode.Disabled` and with no attribute is unchanged.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of consent-exempt purposes in Encina.Compliance.Consent (issue #811).

CONTEXT:
- ConsentRequiredPipelineBehavior.Handle (src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs:85-192) validates every [RequireConsent] purpose through IConsentValidator.
- Phase 4 added IConsentExemptionPolicy (is a purpose actively exempt now?) and IConsentExemptionService (idempotent per-subject record).

TASK:
Partition the attribute's purposes into actively exempt and consent-requiring ones. Record each exempt purpose through IConsentExemptionService.RecordExemptionAsync; send only the remaining purposes to IConsentValidator (skip the call when none remain). In Block mode a failed record returns Left with failure reason consent.exemption_record_failed; in Warn mode it logs and proceeds. Add the exempt purposes as an activity tag.

KEY RULES:
- Compliance gates fail closed: never report success when the exemption record failed in Block mode (AGENTS.md section 3).
- The subject id never reaches logs, traces or metrics (#1314).
- Keep the static attribute and property caches as they are; no new reflection on the hot path.
- Every method you add or change has CRAP <= 10; split Handle into private helpers.
- Do not change IConsentValidator or DefaultConsentValidator.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs
- src/Encina.Compliance.Consent/Diagnostics/ConsentDiagnostics.cs
- tests/Encina.UnitTests/Compliance/Consent/ConsentRequiredPipelineBehaviorTests.cs
- docs/engineering/crap-gate-design.md
```

</details>

---

### Phase 6: Cross-Cutting Integration

> **Goal**: Apply the ✅ rows of the matrix: caching, tenancy, module id, validation, health check, audit.

<details>
<summary><strong>Tasks</strong></summary>

1. **Caching**: `DefaultConsentExemptionService.RecordExemptionAsync` checks a tenant-scoped cache marker `consent:exemption:tenant:{t}:subject:{s}:purpose:{p}` before writing and sets it after a successful or already-existing write, so only the first request per subject and purpose writes; `RevokeExemptionAsync` removes it. Same key style and tenant segment as `DefaultConsentService.TenantCacheSegment` (`Services/DefaultConsentService.cs:641-642`).
2. **Multi-tenancy**: `TenantId` on `ConsentExemptionApplied` and the read model; reads filter by the ambient tenant (shared `ConsentTenantScope` from Phase 4); stream id includes the tenant; a two-tenant integration test (Phase 8).
3. **Module isolation**: `ModuleId` carried on the event and read model, as `ConsentGranted` does; the pipeline passes `null` because `IRequestContext` has no module id (`src/Encina/Abstractions/IRequestContext.cs`).
4. **Health check**: `src/Encina.Compliance.Consent/Health/ConsentHealthCheck.cs` (modify) — when `ExemptPurposeDefinitions` is non-empty, `IConsentExemptionService` must resolve (Unhealthy otherwise); a draft-basis definition while the switch is off adds a Degraded warning "draft exemption defined but inactive"; data entry `draftOmnibusExemptions` with the switch state.
5. **Audit trail**: the Marten stream is the audit record; `ConsentExemptionApplied` and `ConsentExemptionRevoked` implement `INotification`, so `Encina.Marten`'s `EventPublishingPipelineBehavior` publishes them like the other consent events.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 (cross-cutting integration) of consent-exempt purposes in Encina.Compliance.Consent (issue #811).

CONTEXT:
- Phases 1-5 built the exemption model, service, policy and pipeline integration.
- The Cross-Cutting Integration Matrix of the plan marks Caching, OpenTelemetry, Structured Logging, Health Checks, Validation, Idempotency, Multi-Tenancy, Module Isolation and Audit Trail as included.

TASK:
Add the tenant-scoped "already recorded" cache marker to DefaultConsentExemptionService, confirm TenantId and ModuleId flow into events and read models with tenant-filtered reads, and extend ConsentHealthCheck as described in the plan's Phase 6 Tasks.

KEY RULES:
- Cache keys always include the tenant segment; a cache miss or cache failure never skips the write (the cache is an optimisation, the deterministic stream id is the guarantee).
- Health checks keep DefaultName and Tags, resolve through a created scope, and report no subject data.
- No subject id in any cache key that reaches telemetry; the key itself is fine (it is not logged).

REFERENCE FILES:
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- src/Encina.Compliance.Consent/Health/ConsentHealthCheck.cs
- src/Encina.Marten/EventPublishingPipelineBehavior.cs
```

</details>

---

### Phase 7: Observability

> **Goal**: Traces, metrics and `[LoggerMessage]` logs for exemptions, inside the registered Consent range.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Compliance.Consent/Diagnostics/ConsentDiagnostics.cs`** (modify):
   - Counters `consent.exemptions.applied` (tags `consent.purpose`, `consent.exemption_basis`, `consent.tenant_id`), `consent.exemptions.recorded` (first write only), `consent.exemptions.revoked`, `consent.exemptions.record_failed`.
   - Tag constants `TagExemptPurposes = "consent.exempt_purposes"`, `TagExemptionBasis = "consent.exemption_basis"`, `TagTenantId = "consent.tenant_id"` (SPEC-002 REQ-062: tenant id as an attribute).
   - Activity `Consent.Exemption.Record` started by `DefaultConsentExemptionService.RecordExemptionAsync`.
2. **`src/Encina.Compliance.Consent/Diagnostics/ConsentExemptionLogMessages.cs`** (new, `internal static partial class`, `[LoggerMessage]` source generator, XML doc "Event IDs: 8269-8274 (see EventIdRanges.ComplianceConsent)"):
   - 8269 `ConsentExemptionApplied` (Information; purpose, basis)
   - 8270 `ConsentExemptionAlreadyRecorded` (Debug; purpose)
   - 8271 `ConsentExemptionRecordFailed` (Error; purpose, error code)
   - 8272 `ConsentExemptionRevoked` (Information; purpose)
   - 8273 `ConsentDraftExemptionInactive` (Warning at startup; purpose, basis)
   - 8274 `ConsentDraftOmnibusExemptionsEnabled` (Warning at startup; count of draft-basis purposes)
3. Startup logging of 8273 and 8274: from `DefaultConsentExemptionPolicy`'s constructor is too late and too often; log once from `ConsentAutoRegistrationHostedService.StartAsync` (runs once per host), independent of `AutoRegisterFromAttributes`.
4. No range change: 8269-8274 continue the packed sequence after 8268 (`Diagnostics/ConsentLogMessages.cs:299`) inside `ComplianceConsent = (8200, 8299)`; `EncinaEventIdAllocationTests` already maps the assembly (line 63).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 (observability) of consent-exempt purposes in Encina.Compliance.Consent (issue #811).

CONTEXT:
- ConsentDiagnostics (Diagnostics/ConsentDiagnostics.cs) owns the ActivitySource and Meter "Encina.Compliance.Consent".
- Existing logs use LoggerMessage.Define in Diagnostics/ConsentLogMessages.cs, highest EventId 8268; the package range is ComplianceConsent = (8200, 8299) in src/Encina/Diagnostics/EventIdRanges.cs.
- New code uses the [LoggerMessage] source generator (AGENTS.md section 7).

TASK:
Add the four exemption counters, the tag constants and the Consent.Exemption.Record activity to ConsentDiagnostics; create Diagnostics/ConsentExemptionLogMessages.cs with EventIds 8269-8274 as listed in the plan's Phase 7 Tasks; emit the two startup warnings once from ConsentAutoRegistrationHostedService.StartAsync; wire the counters and logs into the service and pipeline.

KEY RULES:
- EventIds packed sequentially from 8269, all inside 8200-8299; run the architecture tests (EncinaEventIdAllocationTests, EventIdUniquenessRule).
- No subject id, actor id or EncinaError.Message in any log, tag or metric; log only error codes (AGENTS.md section 3, #1314).
- Metrics are tested with an in-memory exporter (SPEC-002 REQ-062).

REFERENCE FILES:
- src/Encina.Compliance.Consent/Diagnostics/ConsentDiagnostics.cs
- src/Encina.Compliance.Consent/Diagnostics/ConsentLogMessages.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- tests/Encina.UnitTests/Compliance/Consent/ConsentPiiLeakTests.cs
```

</details>

---

### Phase 8: Testing

> **Goal**: Every flag reaches its per-file target; AC-024's two tests exist.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/Compliance/Consent/`):
   - `ConsentExemptionAggregateTests` — apply, revoke, re-apply, invalid transitions.
   - `ConsentExemptionStreamIdTests` — deterministic, tenant-sensitive, version 8 and variant bits, no collision on separator injection.
   - `DefaultConsentExemptionPolicyTests` — current-law basis active by default; **draft basis inactive by default (AC-024: the default follows current law)**; draft basis active with the switch on.
   - `DefaultConsentExemptionServiceTests` (mocked `IAggregateRepository`, NSubstitute) — undefined purpose, `StreamAlreadyExists` maps to success, cache marker skips the write, repository `Left` returned, tenant fail-closed.
   - Extend `ConsentOptionsValidatorTests`, `ConsentOptionsTests`, `ConsentRequiredPipelineBehaviorTests` (all-exempt request skips the validator; mixed request validates only consent purposes; record failure blocks in Block and passes in Warn), `ServiceCollectionExtensionsTests` (`ValidateOnBuild` + `ValidateScopes`), `ConsentPiiLeakTests` (exemption logs and tags carry no subject id), a metrics test with an in-memory exporter.
   - `DefaultConsentServiceSolicitationTests` — `Exempt`, `ConsentActive`, `Solicit`, `Left` propagation.
2. **Guard** (`tests/Encina.GuardTests/Compliance/Consent/`): `ConsentExemptionAggregateGuardTests`, `DefaultConsentExemptionServiceGuardTests`, `DefineExemptPurpose` guard, new constructor parameters of the pipeline behavior.
3. **Contract** (`tests/Encina.ContractTests/Compliance/Consent/`): extend `IConsentServiceContractTests` with `GetSolicitationDecisionAsync`; new `IConsentExemptionServiceContractTests` and `IConsentExemptionPolicyContractTests` against the real default implementations.
4. **Property** (`tests/Encina.PropertyTests/Compliance/Consent/`): `ConsentExemptionPropertyTests` — for any purpose set, the purposes sent to the validator are exactly the non-exempt ones; with the switch off, no draft-basis purpose is ever exempt; stream id is a pure function of (tenant, subject, purpose).
5. **Integration** (`tests/Encina.IntegrationTests/Compliance/Consent/ConsentExemptionIntegrationTests.cs`, `[Collection(MartenCollection.Name)]`, `[Trait("Category", "Integration")]`, `[Trait("Database", "PostgreSQL")]`, as `ConsentTenantScopingIntegrationTests`):
   - **AC-024 round trip**: with `EnableDraftOmnibusExemptions = false`, `RecordExemptionAsync` for an `AudienceMeasurement` purpose writes `ConsentExemptionApplied`, and the event stream and `ConsentExemptionReadModel` read back with every field unchanged; the pipeline still requires consent for that purpose.
   - Concurrent first records for the same subject and purpose produce one stream.
   - Two-tenant isolation: same subject and purpose in two tenants give two streams, and each tenant reads only its own.
   - End-to-end pipeline with the switch on: request passes without consent and the exemption is readable.
6. **Load and benchmark**: update the existing justifications `tests/Encina.LoadTests/Compliance/Consent/ConsentValidationLoadTests.md` and `tests/Encina.BenchmarkTests/Encina.Benchmarks/Compliance/Consent/ConsentBenchmarks.md` with a paragraph on the exemption path (one cached dictionary lookup per purpose; one write per subject and purpose).
7. **Coverage manifest**: add per-file `targets` and one-sentence `justifications` for every new and touched file to `.github/coverage-manifest/Encina.Compliance.Consent.json`; measure with the AGENTS.md §9 commands and run `--check-justifications`; run the local CRAP table.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 (testing) of consent-exempt purposes in Encina.Compliance.Consent (issue #811).

CONTEXT:
- Phases 1-7 are implemented. Consent is event-sourced on Marten; unit tests mock IAggregateRepository with NSubstitute; integration tests run on Marten/PostgreSQL through the shared MartenCollection fixture.
- SPEC-002 AC-024 requires (1) a test that the default follows current law (draft exemptions inactive) and (2) a test that the events and read model this issue adds are written and read back unchanged while the option is off.

TASK:
Write the unit, guard, contract, property and integration tests listed in the plan's Phase 8 Tasks, update the two load/benchmark justification files, and add per-file targets and justifications to .github/coverage-manifest/Encina.Compliance.Consent.json. Measure every flag and the CRAP of changed methods.

KEY RULES:
- Tests execute real package code; no reflection-only tests (AGENTS.md section 9).
- Shouldly through Encina.Testing.Shouldly; never FluentAssertions; no Thread.Sleep; FakeTimeProvider for time.
- Integration tests use [Collection(MartenCollection.Name)] and clear data in InitializeAsync; never a per-class fixture or IClassFixture.
- Measure per flag: dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests, then dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report and -- --check-justifications.
- CRAP <= 10 on every method added or changed.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs
- tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs
- tests/Encina.UnitTests/Compliance/Consent/ConsentRequiredPipelineBehaviorTests.cs
- tests/Encina.ContractTests/Compliance/Consent/IConsentServiceContractTests.cs
- tests/Encina.PropertyTests/Compliance/Consent/ConsentAggregatePropertyTests.cs
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 9: Documentation & Finalization

> **Goal**: Every public API documented, draft article named everywhere AC-024 asks, changelog fragment, build and coverage green.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML documentation**: `<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>` on every new public API; the draft members and `EnableDraftOmnibusExemptions` name COM(2025) 837 and draft GDPR Art. 88a (AC-024).
2. **`changelog.d/811-consent-exempt-purposes.added.md`**: consent-exempt purposes, current-law ePrivacy Art. 5(3) bases active, draft Art. 88a bases off by default.
3. **`src/Encina.Compliance.Consent/README.md`**: new "Consent-Exempt Purposes" section after "Consent Purposes" with the two-switch table (current law on, draft off) and the solicitation answer.
4. **`docs/features/consent-management.md`**: section on exempt purposes, the exemption audit record and `GetSolicitationDecisionAsync` (follow the `encina-docs` skill; delegate to `docs-writer`).
5. **Package specification**: the Consent article-coverage specification is #1209 (open). If it exists when this issue closes, add the rows "draft GDPR Art. 88a(3)(c) — Partial (off by default)" and "ePrivacy Art. 5(3) exemptions — Covered"; otherwise leave a comment on #1209 with those rows (orchestrator).
6. **`docs/INVENTORY.md`**: list the new files of the Consent package.
7. **`ROADMAP.md`**: mark #811 done under v0.15.0 if listed.
8. **ADR**: not needed; the decisions are package-local and recorded in this plan (the draft-law rule is already SPEC-002 DEC-012).
9. **`PublicAPI.Unshipped.txt`**: final review.
10. **Release notes**: `docs/releases/` has no v0.15.0 folder yet; nothing to update unless the release-checklist creates it.
11. **Build**: `dotnet build Encina.slnx --configuration Release` → 0 errors, 0 warnings.
12. **Tests**: `dotnet test` all pass; every coverage flag reaches its target in `.github/coverage-manifest/Encina.Compliance.Consent.json` (per-flag obligations model).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 (documentation and finalization) of consent-exempt purposes in Encina.Compliance.Consent (issue #811).

CONTEXT:
- Phases 1-8 are implemented and tested.
- SPEC-002 AC-024: the draft behaviour is named by its draft article in the XML documentation and in the package specification (#1209 creates that specification).

TASK:
Complete XML docs, add changelog.d/811-consent-exempt-purposes.added.md, update the package README and docs/features/consent-management.md, docs/INVENTORY.md and ROADMAP.md, review PublicAPI.Unshipped.txt, and verify build and per-flag coverage.

KEY RULES:
- Never edit the [Unreleased] section of CHANGELOG.md; only the changelog.d fragment (changelog.d/README.md).
- Docs follow the encina-docs skill (one Diataxis quadrant per page, identifiers verified in src/, no hand-typed coverage figures).
- Build: 0 errors, 0 warnings. Tests: all pass; every flag at its manifest target.
- Commit message references the issue: "feat(consent): consent-exempt purposes with draft Omnibus bases off by default (#811)".

REFERENCE FILES:
- changelog.d/README.md
- src/Encina.Compliance.Consent/README.md
- docs/features/consent-management.md
- .claude/skills/encina-docs/SKILL.md
- docs/specifications/SPEC-002-eu-regulatory-readiness.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Relevance |
|--------|-----------|-----------|
| ePrivacy Directive 2002/58/EC | Art. 5(3) | Current law: storage or access on terminal equipment needs consent except for transmission or what is strictly necessary for a service explicitly requested; basis of `CommunicationTransmission` and `StrictlyNecessaryService` |
| LSSI, Ley 34/2002 (Spain) | Art. 22.2 | Spanish transposition of ePrivacy Art. 5(3), the national acceptance case (SPEC-002 §3, line 90) |
| COM(2025) 837 (data and cyber Digital Omnibus), procedure 2025/0360(COD) | Draft GDPR Art. 88a(3)(c) | First-party aggregated audience measurement without consent; not adopted (SPEC-002 §3.5, line 130-136) |
| COM(2025) 837 | Draft GDPR Art. 88a(3), security exemption | Security of the service or terminal; letter to verify against the proposal text |
| COM(2025) 837 | Draft GDPR Art. 88a(4)(a) and (c) | Single-click reject and re-ask moratorium: #810, not this issue |
| GDPR, Reg. (EU) 2016/679 | Art. 5(2), Art. 7(1) | Accountability: the controller must demonstrate why no consent was collected, hence the per-subject exemption record |
| GDPR | Art. 13, Art. 15 | Transparency and access: `GetExemptionsAsync` feeds an access export |
| GDPR | Art. 21 | Objection to processing on legitimate interests: the application handles it and calls `RevokeExemptionAsync` |
| SPEC-002 | REQ-024, DEC-012, AC-024, INV-003 | Draft behaviour off by default, named by draft article, round-trip test while off |
| SPEC-002 | REQ-061, REQ-062 | Tenant-aware records and telemetry with tenant id, no personal data |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `ConsentOptions`, `DefinePurpose` | `src/Encina.Compliance.Consent/ConsentOptions.cs:39-328` | Pattern for `DefineExemptPurpose` and the draft switch |
| `ConsentOptionsValidator` | `src/Encina.Compliance.Consent/ConsentOptionsValidator.cs:24-74` | New fail-fast rules |
| `ConsentRequiredPipelineBehavior` | `src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs:54-258` | Purpose partition and exemption recording |
| `ConsentAggregate`, `AggregateBase` | `src/Encina.Compliance.Consent/Aggregates/ConsentAggregate.cs:34` | Pattern for `ConsentExemptionAggregate` |
| `ConsentProjection`, `ConsentReadModel` | `src/Encina.Compliance.Consent/ReadModels/` | Pattern for the exemption projection and read model |
| `AddConsentAggregates` | `src/Encina.Compliance.Consent/ConsentMartenExtensions.cs:50-58` | Registration of the new aggregate and projection |
| `DefaultConsentService` tenant scope and cache keys | `src/Encina.Compliance.Consent/Services/DefaultConsentService.cs:378-428, 641-650` | Shared `ConsentTenantScope`, cache key style |
| `MartenAggregateRepository.CreateAsync` / `StreamAlreadyExists` | `src/Encina.Marten/MartenAggregateRepository.cs:270-330` | Idempotent first write with a deterministic stream id |
| `EventPublishingPipelineBehavior` | `src/Encina.Marten/EventPublishingPipelineBehavior.cs` | Publishes the new `INotification` events |
| `ConsentDiagnostics`, `ConsentLogMessages` | `src/Encina.Compliance.Consent/Diagnostics/` | ActivitySource, Meter, EventIds |
| `ConsentHealthCheck` | `src/Encina.Compliance.Consent/Health/ConsentHealthCheck.cs:37` | Exemption-service resolution and draft warning |
| `UuidV7IdGenerator` | `src/Encina.IdGeneration/Generators/UuidV7IdGenerator.cs` | Reference for version and variant bits of the version 8 stream id |
| `MartenCollection` fixture | `tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs:112-118` | Integration tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.Consent` (existing) | 8200-8299 (`EventIdRanges.ComplianceConsent`) | Used: 8200-8207, 8230-8232, 8240-8243, 8250, 8260-8268 |
| **`Encina.Compliance.Consent` exemptions (new)** | **8269-8274** | Packed after 8268; no new range; assembly already mapped in `EncinaEventIdAllocationTests` (line 63) |
| `Encina.Compliance.Consent` remaining free | 8275-8299 | Left for #810 (cooldown) and later consent work |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New source files | 13 | 3 model enums/classes, 1 snapshot record, 1 events file, aggregate, stream id helper, read model, projection, 2 interfaces, 2 services, 1 log messages file, tenant-scope helper (one may merge) |
| Modified source files | 12 | Options, validator, auto-registration, pipeline, IConsentService, DefaultConsentService, errors, DI, Marten extensions, diagnostics, health check, PublicAPI |
| Tests | ~12 | Unit 6 new + 6 extended, guard 3, contract 3, property 1, integration 1 |
| Justification updates | 2 | Load and benchmark `.md` |
| Documentation | 5 | Changelog fragment, README, feature guide, INVENTORY, ROADMAP; coverage manifest |
| **Total** | **~44** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing consent-exempt purposes in Encina.Compliance.Consent for issue #811 (Digital Omnibus draft GDPR Art. 88a, off by default).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0 (no backward compatibility), Railway Oriented Programming with Either<EncinaError, T>.
- Encina.Compliance.Consent is event-sourced on Marten (PostgreSQL); no database provider matrix applies; no in-memory store.
- SPEC-002 REQ-024 / DEC-012 / AC-024: draft-law behaviour is built before 1.0, off by default, named by its draft article in XML docs and the package specification; a test proves the default follows current law, and a test proves the new events and read model round-trip while the option is off.
- Rules: TimeProvider only; async DB calls with CancellationToken; no subject id or EncinaError.Message in logs, traces or metrics; compliance gates fail closed; registration completeness proven with ValidateOnBuild and ValidateScopes; CRAP <= 10 on changed methods; per-flag coverage targets in the package manifest.

IMPLEMENTATION OVERVIEW:
Phase 1: ConsentExemptionBasis (current-law ePrivacy Art. 5(3) bases, draft Art. 88a bases), WellKnownExemptPurposes, ConsentSolicitationDecision, ConsentExemptionApplied / ConsentExemptionRevoked events.
Phase 2: ConsentOptions.DefineExemptPurpose, ExemptPurposeDefinitions, EnableDraftOmnibusExemptions (default false); validator rules; auto-registration accepts exempt purposes.
Phase 3: ConsentExemptionAggregate on a deterministic stream id (version 8 UUID from SHA-256 of tenant, subject, purpose), ConsentExemptionProjection, ConsentExemptionReadModel, registration in AddConsentAggregates.
Phase 4: IConsentExemptionPolicy (+ options-based default), IConsentExemptionService (+ default; StreamAlreadyExists = already recorded), shared ConsentTenantScope, IConsentService.GetSolicitationDecisionAsync, error codes, DI.
Phase 5: ConsentRequiredPipelineBehavior partitions purposes; records exempt ones; validates the rest; fail closed in Block mode.
Phase 6: Cache marker, tenancy, module id, health check, audit via event stream.
Phase 7: Counters, activity, tags (tenant id included), [LoggerMessage] EventIds 8269-8274 in ComplianceConsent (8200-8299).
Phase 8: Unit, guard, contract, property, Marten integration tests (AC-024 round trip, concurrency, two tenants); manifest targets and justifications.
Phase 9: XML docs, changelog.d fragment, README, feature guide, INVENTORY, ROADMAP, #1209 specification rows.

KEY PATTERNS:
- Aggregates: static factory raising the creation event; guarded transitions throwing InvalidOperationException; Apply(object) switch.
- Services: tenant scope resolution as DefaultConsentService; tenant-scoped cache keys; exceptions become ConsentErrors.ServiceError with ForLogging().
- Pipeline: static attribute cache; Block / Warn / Disabled enforcement modes.
- Health check: DefaultName const, Tags array, scoped resolution.
- Integration tests: [Collection(MartenCollection.Name)], Category and Database traits, data cleared in InitializeAsync.
- Observability: ActivitySource + Meter "Encina.Compliance.Consent", [LoggerMessage] source generator for new logs.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ (whole package)
- src/Encina.Marten/MartenAggregateRepository.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs
- docs/specifications/SPEC-002-eu-regulatory-readiness.md
- docs/plans/consent-exempt-purposes-implementation-plan-811.md (this plan)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | Tenant-scoped "already recorded" marker so only the first request per subject and purpose writes (Phase 6); the policy snapshot is an in-memory frozen dictionary |
| 2 | OpenTelemetry | ✅ | `consent.exemptions.*` counters, `Consent.Exemption.Record` activity, `consent.exempt_purposes` and `consent.tenant_id` tags, in-memory exporter test (Phase 7) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` EventIds 8269-8274 inside `ComplianceConsent` (8200-8299); startup warnings for inactive draft exemptions and for the draft switch turned on (Phase 7) |
| 4 | Health Checks | ✅ | `ConsentHealthCheck` checks that `IConsentExemptionService` resolves when exempt purposes are defined and reports Degraded for an inactive draft exemption (Phase 6) |
| 5 | Validation | ✅ | `ConsentOptionsValidator` rejects a purpose defined both ways, empty keys, and `RequiresAggregation` with a non-measurement basis (Phase 2) |
| 6 | Resilience | ❌ | No external call: the only I/O is the Marten store the package already uses, whose failures return `Left` and fail the request in Block mode |
| 7 | Distributed Locks | ❌ | Concurrent first records are made idempotent by the deterministic stream id and `StreamAlreadyExists`, so no lock is needed |
| 8 | Transactions | ❌ | Each operation appends to one stream, which Marten commits atomically; there is no multi-aggregate write |
| 9 | Idempotency | ✅ | Deterministic version 8 stream id per (tenant, subject, purpose); a duplicate create is treated as already recorded (Phase 3, Phase 4) |
| 10 | Multi-Tenancy | ✅ | `TenantId` on events and read model, tenant in the stream id and cache keys, tenant-filtered reads failing closed, two-tenant integration test; per-tenant exemption policy possible by overriding `IConsentExemptionPolicy` |
| 11 | Module Isolation | ✅ | `ModuleId` on `ConsentExemptionApplied` and the read model, as on `ConsentGranted`; the pipeline passes null because `IRequestContext` has no module id |
| 12 | Audit Trail | ✅ | The Marten stream of `ConsentExemptionAggregate` is the per-subject audit record (`ConsentExemptionApplied`, `ConsentExemptionRevoked`), published as notifications; `GetExemptionsAsync` serves Art. 15 exports |

---

## Prerequisites & Dependencies

### Blocking

- None. Every type this plan uses exists on `main` at `5b485b12`.

### Coordination

- **#810 (consent cooldown, open)** touches `ConsentOptions`, `IConsentService`, `DefaultConsentService` and the solicitation answer. Whichever issue lands first introduces `ConsentSolicitationDecision` and `GetSolicitationDecisionAsync`; the second adds its member (`CooldownActive`). Run the two sequentially, not in parallel worktrees, to avoid conflicts in the same files. #810 should use EventIds from 8275.
- **#1209 (Consent article-coverage specification, open)** is where AC-024's "named in the package specification" lands; Phase 9 adds the rows or comments on #1209.
- **#1199, #1197, #1255** change the consent purpose and event model; no ordering requirement, but the second to land rebases on the first.
- **#817 (IOptionsMonitor migration, open)**: the policy snapshot reads `IOptions<ConsentOptions>` once; when #817 lands, the snapshot is rebuilt on change.
- **#1920 (LawfulBasis to Consent bridge, open)**: when built, the bridge should report an actively exempt purpose consistently with `IConsentExemptionPolicy`.

### No new issues

- No integration is deferred and no prerequisite is missing, so this plan writes no issue files.

---

## Next Steps

1. The maintainer reviews the six Design Choices and records the decisions (the orchestrator adds the decisions section).
2. Link this plan from #811.
3. Decide the order with #810 (Prerequisites & Dependencies).
4. Implement Phases 1-9 in one worktree with one `issue-worker`; each phase is a self-contained commit.
5. The pull request references `Fixes #811`, reports per-flag coverage and CRAP of the touched files, and records the ADR-018 evaluation (the matrix above).
