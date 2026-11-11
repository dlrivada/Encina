# Implementation Plan: `Encina.Compliance.Consent` — Consent Cooldown After Refusal (Digital Omnibus draft GDPR Art. 88a)

> **Issue**: [#810](https://github.com/dlrivada/Encina/issues/810)
> **Type**: Feature
> **Milestone**: v0.15.0 — EU Compliance: NIS2 & Digital Omnibus (parent EPICs #809, #1186)
> **Complexity**: Medium (8 phases, one package, no database provider matrix, three pull requests)
> **Estimated Scope**: ~650 lines of production code + ~1,300 lines of tests, split into three pull requests

---

## Summary

Add a **consent cooldown after refusal** to `Encina.Compliance.Consent`: when a data subject declines a consent request for a purpose, the application records the refusal (`ConsentRefused`), and for a configurable period (six months at the draft law's minimum) the package answers "do not ask again" (`ConsentSolicitationDecision.CooldownActive`). The behaviour implements a provision of a proposal that is **not adopted** (COM(2025) 837), so it is **off by default**, named by its draft article in the XML documentation and the package specification, and the persisted shape exists and works while it is off (SPEC-002 REQ-024, DEC-012, AC-024).

### What the issue asks and what SPEC-002 adds

- The issue body asks for `ConsentCooldownAfterRefusal` (a `TimeSpan`), `CooldownPerPurpose`, a `ConsentRefused` event, a check before soliciting that returns `CooldownActive`, a 180-day minimum validated in `ConsentOptionsValidator`, and a duration that can change without a restart (depends on #817).
- The maintainer's comment on the issue (SPEC-002 REQ-024, DEC-012, AC-024, 2026-09-24) adds: off by default while COM(2025) 837 is not adopted; the draft article named in the XML documentation and in the package specification; a test that the default follows current law; a test that the persisted state, events and model values this issue adds are written and read back unchanged while the option is off; the issue closes before `1.0.0-rc.1`.
- SPEC-002 §3.5 (`docs/specifications/SPEC-002-eu-regulatory-readiness.md`, line 136) maps "single-click reject; six-month re-ask moratorium" to **draft GDPR Art. 88a(3)(c), 88a(4)(a) and (c)** and assigns the moratorium to #810. SPEC-002 line 130 says the procedure (2025/0360(COD)) is awaiting committee decision and current law stays the default.
- **Unverified legal claims.** The issue cites an "EDPB-EDPS Joint Opinion 2/2026" and "Art. 3" of the Omnibus. Neither can be verified from the repository (the same citation is reported unverifiable in `docs/plans/dsar-rejection-reasons-implementation-plan-814.md`, line 70). SPEC-002 does not say which paragraph carries the moratorium or whether it is worded in days or calendar months. The XML documentation therefore cites only "COM(2025) 837, draft GDPR Art. 88a(4)" with the letter left out until the proposal text is checked; Design Choice 3 depends on that check.

### Current state of the code (nothing of #810 exists yet; facts re-checked on `7a0049c07`)

- **No refusal concept.** `Events/ConsentEvents.cs` has six records (`ConsentGranted`, `ConsentWithdrawn`, `ConsentExpired`, `ConsentRenewed`, `ConsentVersionChanged`, `ConsentReconsentProvided`). `Model/ConsentStatus.cs:17-55` has **four** values (`Active`, `Withdrawn`, `Expired`, `RequiresReconsent`); the issue lists three.
- **No solicitation entry point.** `IConsentService` (`Abstractions/IConsentService.cs:40-207`) has grant, withdraw, renew, reconsent and five queries. Nothing answers "may the application ask this subject for consent to this purpose?". `ConsentRequiredPipelineBehavior.Handle` (`ConsentRequiredPipelineBehavior.cs:85-192`) validates *existing* consent before a request runs; it never asks.
- **Every consent is its own stream.** `DefaultConsentService.GrantConsentAsync` creates the aggregate with `Guid.NewGuid()` (`Services/DefaultConsentService.cs:160`), although the remarks of `Aggregates/ConsentAggregate.cs:12-14` call the id deterministic from subject and purpose. A refusal has no aggregate to append to, because `ConsentAggregate` can only be created by `Grant` (`ConsentAggregate.cs:157-195`).
- **Options.** `ConsentOptions` (`ConsentOptions.cs:39-328`) has no cooldown property; `ConsentOptionsValidator.Validate` (`ConsentOptionsValidator.cs:27-73`) validates expiration days, enforcement mode and purpose definitions. The service reads options through `IOptions<ConsentOptions>` once, in its constructor (`Services/DefaultConsentService.cs:62, 105`); **no class under `src/` uses `IOptionsMonitor` today**, so the hot swap the issue asks for does not exist anywhere yet. #817 (open) migrates the compliance options.
- **Tenant scope.** All reads and writes already resolve the ambient tenant and fail closed through `TryResolveTenantScope` (`Services/DefaultConsentService.cs:378-391`) and `TryResolveWriteTenantScope` (`:402-428`), with `RequireTenantContext` auto-detection (`ConsentOptions.cs:148`, SPEC-002 DEC-006/DEC-009, #1315). A cooldown is tenant-scoped data, so it reuses them.
- **Caching.** `GetConsentAsync` and `GetConsentBySubjectAndPurposeAsync` cache read models for five minutes (`Services/DefaultConsentService.cs:442-517`); `HasValidConsentAsync` reads the read model without the cache (`:558-607`). The issue's "consent state should not be cached" is true for the cooldown only because this plan makes it so (Cross-Cutting row 1).
- **Draft-law precedent.** `DataSubjectRightsOptions.EnableOmnibusArticle12RefusalGrounds` (`src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs:96`) is the existing off-by-default switch for a COM(2025) 837 behaviour.
- **Event IDs.** `ComplianceConsent = (8200, 8299)` (`src/Encina/Diagnostics/EventIdRanges.cs:326`). Used: 8200-8207, 8230-8232, 8240-8243, 8250, 8260-8268 (`Diagnostics/ConsentLogMessages.cs`). The assembly is already mapped in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs:63`. The plan for #811 reserves 8269-8274.
- **Coverage manifest.** `.github/coverage-manifest/Encina.Compliance.Consent.json` has package targets (unit 70, guard 20, contract 15, property 15) and no integration target; `ConsentOptions.cs`, `ConsentProjection.cs` and `Events/ConsentEvents.cs` have no or minimal per-file targets. Every new or touched file needs per-flag targets with one-sentence justifications (AGENTS.md §9).

### Scope

- **Affected package**: `Encina.Compliance.Consent` only (plus its tests, README, feature guide and coverage manifest). No new package, no new project reference.
- **Provider category**: none of the AGENTS.md §5 matrices. Consent is event-sourced on Marten (PostgreSQL); the database rule excludes Marten, and AGENTS.md §3 requires unit tests with a mocked `IAggregateRepository` and integration tests on Marten through Testcontainers.
- **Out of scope**: a withdrawal starting a cooldown (the issue speaks of refusal only); channel-scoped consent (#1199, which will widen the purpose key); consent-exempt purposes (#811, which shares `ConsentSolicitationDecision`); the `IOptionsMonitor` migration of the other compliance options (#817).
- **Estimated files**: 7 new source files, 9 modified source files, 12 test files, 2 justification files, 5 documentation and manifest files (~35 in total; table in Research).

### Pull request split (the work exceeds one worker pull request: 15 files, 1,500 lines)

| PR | Phases | Content | Files (approx.) | Lines (approx.) |
|----|--------|---------|-----------------|-----------------|
| 1. Persisted shape | 1, 2 | `ConsentRefused`, `ConsentRefusalAggregate`, read model, projection, Marten registration, PublicAPI; unit, guard and property tests; manifest entries | 11 | ~700 |
| 2. Options and service | 3, 4 | Options, validator, cooldown evaluator, `IConsentService` methods, `DefaultConsentService` (including the `IOptionsMonitor` read), `ConsentSolicitationDecision`; unit, guard and contract tests; manifest entries | 14 | ~1,000 |
| 3. Cross-cutting, observability, docs | 5, 6, 7 (integration), 8 | Diagnostics, log messages, startup warning, AC-024 tests, Marten integration tests, justification files, README, feature guide, changelog fragment | 12 | ~900 |

PR 2 depends on PR 1 and PR 3 on PR 2. If #811 lands first, PR 2 does not create `ConsentSolicitationDecision` and `GetSolicitationDecisionAsync`; it adds the `CooldownActive` member and the cooldown branch (Design Choice 8). Each worker brief states the file list and `Size:` line of its own pull request.

---

## Design Choices

<details>
<summary><strong>1. Where a refusal is persisted — a separate <code>ConsentRefusalAggregate</code> per tenant, subject and purpose</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add `ConsentStatus.Refused` and a `ConsentRefused` event to `ConsentAggregate`; a refusal creates a consent stream through a new static factory** | Matches the issue's task list ("add `ConsentRefused` to `ConsentEvents.cs` and `ConsentAggregate.cs`") and DEC-012's wording ("a consent-refusal state"); one read model type; `GetAllConsentsAsync` returns refusals for free | Changes a persisted enum that existing code switches on (`DefaultConsentValidator.cs:87-103`, `ConsentProjection`); a later `GrantConsentAsync` creates a *second* stream for the same subject and purpose, and `GetConsentBySubjectAndPurposeAsync` takes `readModels[0]` (`Services/DefaultConsentService.cs:516`), which may then return the refusal instead of the grant; every state-machine guard needs a `Refused` branch |
| **B) A new `ConsentRefusalAggregate` stream per (tenant, subject, purpose) with a deterministic stream id, its own `ConsentRefusalProjection` and `ConsentRefusalReadModel`** | Existing consent states, projection and queries untouched; repeated refusals append to one stream; concurrent first refusals collapse on `StreamAlreadyExists` (`src/Encina.Marten/MartenAggregateRepository.cs:311-319`); the same technique the maintainer chose for exemptions in the #811 plan (its Design Choice 3, decided 2026-10-06) | A new aggregate, projection, read model and registration (~4 files); an Art. 15 export would query two read models |
| **C) Store the refusal as metadata on a placeholder consent** | No new types | A placeholder `Active`-looking consent would pass `HasValidConsentAsync`; fails closed in the wrong direction |

### Chosen Option: **B — separate `ConsentRefusalAggregate`** (recommended, pending the maintainer)

### Rationale

- I recommend B because a refusal is not a consent: adding it to the consent lifecycle makes every existing reader of `ConsentStatus` and of `readModels[0]` decide what a refusal means, while a separate stream leaves the shipped behaviour byte-for-byte unchanged, which matters for a feature that is off by default.
- The stream id is a name-based UUID over tenant, subject and purpose, so concurrent first refusals produce one stream; the same helper technique is planned for #811 and should be shared (Prerequisites & Dependencies).
- If the maintainer picks A, Phase 2 extends `ConsentAggregate`, `ConsentProjection` and `ConsentReadModel` instead of creating the three new types, `ConsentStatus` gains `Refused`, and the validator and `DefaultConsentValidator` get a `Refused` branch; Phases 3-8 are unchanged.
- **Class:** Dilemma
- Scenario for A: the maintainer wants refusals to appear in the consent list of a subject and accepts touching the persisted enum. Scenario for B: the shipped consent behaviour must not change while the feature is off.

</details>

<details>
<summary><strong>2. What the event stores — the expiry computed at refusal time, even while the switch is off</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ConsentRefused` carries `CooldownExpiresAtUtc`, always computed as `OccurredAtUtc + ConsentCooldownAfterRefusal` at refusal time; the switch only controls whether it is enforced** | Matches the issue's event shape; the record shows the period that applied; turning the switch on later honours refusals already taken inside their period; the round trip while off writes a real value (AC-024) | A later change of the duration (hot swap, or the final text) does not alter refusals already stored |
| **B) Store only `OccurredAtUtc`; derive the expiry at check time from the current duration** | A hot swap applies to every stored refusal at once; the event never goes stale | The stored record no longer shows what period applied; the audit answer to "when could we ask again?" changes over time; differs from the issue's event |
| **C) Store the expiry only when the switch is on (null while off)** | The event says "no cooldown applied" while off | Refusals taken before the switch is turned on never count; a null expiry needs its own meaning |

### Chosen Option: **A — persist the expiry at refusal time** (recommended, pending the maintainer)

### Rationale

- I recommend A because the controller must be able to show, per refusal, the period it honoured (GDPR Art. 5(2) accountability), and because AC-024 asks the persisted values to survive the off state unchanged.
- The duration in force at refusal time is the one the subject was told about; changing the law later is a decision about future refusals unless the maintainer wants B.
- **Class:** Dilemma
- Scenario for A: an auditor asks what period applied to a refusal taken last year. Scenario for B: the adopted text changes the period and applies to refusals already recorded.

</details>

<details>
<summary><strong>3. Minimum and unit of the period — a <code>TimeSpan</code> floor of 184 days, not 180</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TimeSpan` with a 180-day default and floor, as the issue says** | Matches the issue and its acceptance criterion literally | SPEC-002 line 136 calls it a "six-month" moratorium; six calendar months span 181 to 184 days, so 180 days can end up to four days *before* six months (a refusal on 1 August is six months old on 1 February, 184 days later) |
| **B) `TimeSpan` with a 184-day default and floor (the longest six-calendar-month span)** | Never shorter than six calendar months, whichever month the refusal falls in; still rejects everything below 180 days, so the acceptance criterion ("minimum 180 days validated") holds; one `TimeSpan` property as the issue shows | Up to four days longer than needed in some months; the number differs from the issue's sample |
| **C) A calendar-month period (`int Months`) added to the refusal instant with `AddMonths`** | Exact calendar semantics | Departs from the issue's `TimeSpan` property; a month-based type does not fit a text worded in days; more code and tests |

### Chosen Option: **B — 184-day default and floor** (recommended, pending the maintainer)

### Rationale

- I recommend B because the floor exists to prevent a non-compliant configuration, and 180 days can be non-compliant by days; the cost of B is a few extra days of not asking.
- The floor lives in one constant (`ConsentOptions`), so the adopted text (days, months or another length) changes one line and one test; the validator applies it whether or not the switch is on, because the duration is used at refusal time even while off (Design Choice 2).
- This choice depends on the unverified wording of draft Art. 88a(4): if the proposal text says 180 days, option A is correct.
- **Class:** Dilemma
- Scenario for A: the text or the maintainer fixes the period at 180 days. Scenario for B: the text says "six months" and a refusal falls in a month pair longer than 180 days.

</details>

<details>
<summary><strong>4. What ends or extends a running cooldown — time only, anchored on the first refusal</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Strict: only time ends a cooldown; a repeated refusal during it is recorded but does not extend it; a later grant or withdrawal does not end it (a grant makes the decision `ConsentActive` anyway)** | Simple, deterministic projection; the stream shows when the application asked during a cooldown (a warning is logged); no cross-aggregate timestamp logic | After refuse, grant, withdraw inside the period the application still may not ask, although the subject acted since |
| **B) Latest act wins: a later grant ends the cooldown and a repeated refusal restarts it** | Follows the subject's most recent act | A second aggregate (consent) decides the state of the first; a wrongly repeated solicitation extends the moratorium; more rules to test |

### Chosen Option: **A — time only, anchored on the first refusal of an unbroken series** (recommended, pending the maintainer)

### Rationale

- I recommend A because the moratorium protects the subject from being asked again, so the safe default after refuse, grant, withdraw is to keep waiting; and A keeps the projection a pure function of the refusal stream.
- The projection keeps the existing expiry when a new `ConsentRefused` arrives with `OccurredAtUtc` before it, increments `RefusalCount`, and sets a new expiry only when the previous one has passed.
- `GrantConsentAsync` is **not** blocked by a cooldown: the draft restricts the controller *asking* ("re-ask moratorium", SPEC-002 line 136), not the subject consenting on their own initiative.
- **Class:** Dilemma
- Scenario for A: the application is audited for asking too soon. Scenario for B: the subject refused, then later consented and withdrew, and the application needs to ask for a different reason sooner.

</details>

<details>
<summary><strong>5. Hot swap of the duration — read <code>IOptionsMonitor&lt;ConsentOptions&gt;</code> in <code>DefaultConsentService</code> now</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `DefaultConsentService` takes `IOptionsMonitor<ConsentOptions>` instead of `IOptions<ConsentOptions>` and reads `CurrentValue` per call; the one-time opt-out log stays in the constructor** | Meets "hot-swappable" in this issue; a small, reversible step that #817 then repeats for the other classes; `IOptionsMonitor` needs no extra registration (`AddOptions` provides it) | Until #817 lands, one compliance class reads options differently from the others; the existing unit tests build the service with `Options.Create` and need a monitor stub |
| **B) Keep `IOptions<ConsentOptions>` and wait for #817** | One migration, one pattern, designed once | The acceptance criterion "hot-swappable" stays open until #817 lands; #810 must close before `1.0.0-rc.1` and #817 has no date |
| **C) A dedicated `IConsentCooldownSettings` singleton fed by `IOptionsMonitor` and used only by the cooldown code** | Isolates the new code from the old constructor | A second options abstraction that #817 would have to remove; extra DI surface |

### Chosen Option: **A — `IOptionsMonitor` in `DefaultConsentService`** (recommended, pending the maintainer)

### Rationale

- I recommend A because it delivers the acceptance criterion with the least new surface and aligns the class with where #817 is going.
- Phase 4 includes a test that an invalid reloaded value (below the floor) does not silently disable a running cooldown: the option factory validates on reload, and the service keeps using the last valid value and logs the failure by code (to be confirmed with `OptionsMonitor` behaviour in the first test).
- **Class:** Dilemma
- Scenario for A: #817 is not scheduled before the release candidate. Scenario for B: #817 is started now and defines a shared reload and validation pattern that this issue should not pre-empt.

</details>

<details>
<summary><strong>6. Retention of refusal events after the cooldown — keep the stream; track erasure and archival separately</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep the refusal stream indefinitely (append-only); erasure and archival of consent streams are one follow-up issue covering grants and refusals** | Matches how every other consent event is stored; the stream is the audit trail (Art. 5(2)); nothing is invented that `Encina.Marten` cannot do (it has no stream archive or truncate facility today) | A refusal record outlives its purpose, which sits badly with storage limitation (Art. 5(1)(e)) until the follow-up lands |
| **B) Archive or delete the refusal stream a grace period after the cooldown expires** | Data minimisation by design | Needs a Marten archival capability and a background job that do not exist; an expired cooldown still shows that the application honoured it; a bigger scope than the issue |
| **C) Keep the stream but pseudonymise the subject id after expiry** | Keeps the audit shape, drops the identifier | Needs crypto-shredding or key management that the Consent package does not use (no encryption type is referenced in the package) |

### Chosen Option: **A — keep the stream; one follow-up issue for erasure and archival** (recommended, pending the maintainer)

### Rationale

- I recommend A because B and C need platform work that is not part of this issue and that would apply to `ConsentGranted` (which carries an IP address and proof of consent) first.
- The follow-up issue is written with this plan (`artifacts/issues/consent-streams-erasure-retention.md`); it also covers the DSR gap (`Encina.Compliance.DataSubjectRights` does not reference the Consent package).
- **Class:** Dilemma
- Scenario for A: the 1.0 scope keeps one shared answer for all consent events. Scenario for B: a regulator or the reference application requires refusal records to disappear when the cooldown ends.

</details>

<details>
<summary><strong>7. How the draft behaviour is switched — a bool switch plus a duration, off by default</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EnableOmnibusConsentCooldown` (bool, default `false`) plus `ConsentCooldownAfterRefusal` (`TimeSpan`) and `CooldownPerPurpose`** | Same shape as `EnableOmnibusArticle12RefusalGrounds` in DSR; the duration keeps a default and can be validated whether or not the behaviour is on; the switch can flip at runtime with the hot swap | Two properties where the issue shows one |
| **B) A nullable `TimeSpan?` where `null` means off** | One property | The duration cannot be validated or documented separately from the on/off decision; the persisted expiry while off (Design Choice 2) has no value to compute from |

### Chosen Option: **A — switch plus duration**

### Rationale

- The switch is the explicit, logged opt-in that SPEC-002 DEC-006/DEC-012 require; the default `false` is current law (no cooldown), SPEC-002 REQ-024 and INV-003.
- Enabling it is logged once at startup by code (Phase 6) so it is never silent.
- **Class:** Settled
- **Settled by:** SPEC-002 REQ-024 and DEC-012 (draft behaviour off by default, named by its draft article); precedent `DataSubjectRightsOptions.EnableOmnibusArticle12RefusalGrounds` (`src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs:96`)

</details>

<details>
<summary><strong>8. Where the cooldown is asked — <code>IConsentService.GetSolicitationDecisionAsync</code>; the pipeline behavior and the grant stay unchanged</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IConsentService.GetSolicitationDecisionAsync(subjectId, purpose)` returning `ConsentSolicitationDecision` (`Solicit`, `ConsentActive`, `CooldownActive`, plus `Exempt` from #811), and `RefuseConsentAsync` to record a refusal** | One place where an application's consent prompt asks; shared with #811, whose maintainer-decided design adds the same enum and method; tenant scope and fail-closed behaviour come from the existing service | Changes a public interface (allowed pre-1.0) |
| **B) Block inside `ConsentRequiredPipelineBehavior`** | No new public method | The behavior validates existing consent and never asks, so it cannot prevent a prompt; blocking `GrantConsentAsync` would stop a subject from consenting voluntarily |
| **C) Expose only the read model and let the application compare dates** | Nothing to build | Every application re-implements the rule; ignores the switch and the per-purpose flag |

### Chosen Option: **A — one solicitation answer on `IConsentService`**

### Rationale

- The #811 plan (its Design Choice 6, decided by the maintainer 2026-10-06) says "#810 later adds `CooldownActive`" to the same enum; whichever issue lands first creates `ConsentSolicitationDecision` and the method.
- Order of the answer: `Exempt` (from #811) before `ConsentActive` before `CooldownActive` before `Solicit`. With the switch off the refusal read model is not queried at all, so the default costs nothing and follows current law.
- **Class:** Settled
- **Settled by:** #811 plan Design Choice 6 (maintainer decision, 2026-10-06, `docs/plans/consent-exempt-purposes-implementation-plan-811.md`); SPEC-002 §3.5 row "re-ask moratorium" (restricts asking, not consenting)

</details>

<details>
<summary><strong>9. Scope of the cooldown — per purpose by default; subject-wide when <code>CooldownPerPurpose</code> is false</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CooldownPerPurpose` defaults to `true`: a refusal blocks only that purpose; `false` blocks every purpose of the subject in the tenant** | Consent is granular and purpose-specific; the issue's sample sets it to `true`; the refusal stream is already per purpose | The `false` mode needs a subject-wide query |
| **B) Always subject-wide** | One query | Refusing marketing would block asking for analytics, against granular consent |

### Chosen Option: **A — per purpose by default**

### Rationale

- `ConsentAggregate` is "consent for a single (DataSubjectId, Purpose) pair, following GDPR Article 6(1)(a)" (`Aggregates/ConsentAggregate.cs:12-14`), so the default follows the existing model.
- **Class:** Trivial
- **Settled by:** GDPR Art. 6(1)(a) and Recital 32 (granular consent) as applied in `Aggregates/ConsentAggregate.cs:12-14`; the issue's own sample (`options.CooldownPerPurpose = true`)

</details>

<details>
<summary><strong>10. A lookup that fails — fail closed (no solicitation), never fail open</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A failed or tenant-less cooldown lookup returns `Left`; no refusal record found returns `Solicit`** | Follows the compliance-gate rule; an application cannot ask because the store was down | A store outage blocks the prompt |
| **B) On a lookup failure return `Solicit`** | The prompt always works | Re-asks a subject who refused whenever the store hiccups; swallows an error in infrastructure |

### Chosen Option: **A — fail closed**

### Rationale

- No refusal on record is the normal case, not missing context, so it answers `Solicit`; a missing tenant (when `RequireTenantContext` resolves to true) or a store `Left` is returned as the error, never turned into a decision.
- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (compliance and security gates fail closed; errors are never swallowed in background infrastructure) and SPEC-002 DEC-006

</details>

<details>
<summary><strong>11. Event and aggregate shape — the existing event conventions and Either-returning commands (ADR-053)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ConsentRefused(RefusalId, DataSubjectId, Purpose, Source, RefusedBy, OccurredAtUtc, CooldownExpiresAtUtc, TenantId, ModuleId) : INotification`; the aggregate's factory and command return `Either<EncinaError, ...>`; no IP address** | Same conventions as every record in `Events/ConsentEvents.cs` (`OccurredAtUtc`, `TenantId`, `ModuleId`); follows ADR-053; collects no personal data the cooldown does not need | Differs from the issue's literal record (`RefusedAtUtc`, no id, no tenant) |
| **B) The issue's literal record `ConsentRefused(DataSubjectId, Purpose, RefusedAtUtc, CooldownExpiresAtUtc)` and a throwing aggregate** | Matches the issue text | No tenant, so the event cannot be tenant-scoped; no stream id; copies the throwing pattern ADR-053 retires |

### Chosen Option: **A — existing conventions and ADR-053**

### Rationale

- `RefusedAtUtc` and `OccurredAtUtc` both satisfy the `AtUtc` naming rule (AGENTS.md §4); `OccurredAtUtc` is what the projection and handlers already read on every other event.
- The IP address is left out because the cooldown does not need it (GDPR Art. 5(1)(c), data minimisation); `Source` and `RefusedBy` give the audit what it needs.
- **Class:** Settled
- **Settled by:** ADR-053 (aggregate rule violations return `Either`); `Events/ConsentEvents.cs` conventions; AGENTS.md section 4 (UTC timestamps with the `AtUtc` suffix); GDPR Art. 5(1)(c)

</details>

---

## Implementation Phases

### Phase 1: Core Models & Events

> **Goal**: The persisted event and the public decision type. (PR 1)

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Compliance.Consent/Events/ConsentRefusalEvents.cs`** (new), namespace `Encina.Compliance.Consent.Events`:
   - `public sealed record ConsentRefused(Guid RefusalId, string DataSubjectId, string Purpose, string Source, string RefusedBy, DateTimeOffset OccurredAtUtc, DateTimeOffset? CooldownExpiresAtUtc, string? TenantId, string? ModuleId) : INotification`, in the style of `Events/ConsentEvents.cs`.
   - XML docs name the draft article ("COM(2025) 837, draft GDPR Art. 88a(4)", AC-024), say the event is written whether or not the switch is on, and say `CooldownExpiresAtUtc` is computed at refusal time (Design Choice 2).
2. **`src/Encina.Compliance.Consent/Model/ConsentSolicitationDecision.cs`** (new, only if #811 has not created it): `public enum ConsentSolicitationDecision { Solicit, ConsentActive, CooldownActive }`; `Exempt` is added by #811. XML docs: `CooldownActive` is draft-law behaviour, off by default.
3. **`PublicAPI.Unshipped.txt`**: every new public symbol (RS0016).
4. **Coverage manifest** `.github/coverage-manifest/Encina.Compliance.Consent.json`: entries with per-flag targets and one-sentence justifications for each new and touched file (written by `mechanical-fixer` from the lines the worker decides).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of the consent cooldown in Encina.Compliance.Consent (issue #810).

CONTEXT:
- Encina is a .NET 10 / C# 14 library, nullable enabled, pre-1.0, Railway Oriented Programming (Either<EncinaError, T>).
- Package: src/Encina.Compliance.Consent/ (event-sourced on Marten). There is no refusal concept today.
- SPEC-002 REQ-024 / DEC-012 / AC-024: draft-law behaviour (COM(2025) 837, draft GDPR Art. 88a(4)) is off by default and named by its draft article in XML docs.

TASK:
Create Events/ConsentRefusalEvents.cs with the sealed record ConsentRefused exactly as listed in the plan's Phase 1 Tasks, and Model/ConsentSolicitationDecision.cs unless it already exists on main (then add the CooldownActive member). Add the public symbols to PublicAPI.Unshipped.txt.

KEY RULES:
- Records implement INotification, timestamps are DateTimeOffset with the AtUtc suffix, TenantId and ModuleId are nullable strings, like ConsentGranted.
- XML docs on every public member; name the draft article and say "off by default"; never write a letter of Art. 88a(4) that has not been verified against the proposal text.
- No [Obsolete], no compatibility shims, English only; zero warnings; RS0016 clean.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Events/ConsentEvents.cs
- src/Encina.Compliance.Consent/Model/ConsentStatus.cs
- src/Encina.Compliance.Consent/PublicAPI.Unshipped.txt
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (§3.5, REQ-024, AC-024, DEC-012)
- docs/plans/consent-exempt-purposes-implementation-plan-811.md (Design Choice 6)
```

</details>

---

### Phase 2: Aggregate, Projection & Read Model

> **Goal**: Persist refusals on a deterministic stream and project the active cooldown. (PR 1)

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Compliance.Consent/Aggregates/ConsentRefusalAggregate.cs`** (new), `public sealed class ConsentRefusalAggregate : AggregateBase`:
   - State: `DataSubjectId`, `Purpose`, `TenantId`, `ModuleId`, `LastRefusedAtUtc`, `CooldownExpiresAtUtc`, `RefusalCount`.
   - `public static Guid StreamIdFor(string? tenantId, string dataSubjectId, string purpose)`: RFC 9562 version 8 UUID from SHA-256 of length-prefixed `tenant-or-'-'`, subject and purpose under a fixed namespace GUID (same technique as the #811 plan's stream id; share one helper if #811 lands first).
   - `public static Either<EncinaError, ConsentRefusalAggregate> Refuse(string? tenantId, string dataSubjectId, string purpose, string source, string refusedBy, DateTimeOffset? cooldownExpiresAtUtc, DateTimeOffset occurredAtUtc, string? moduleId = null)` and `public Either<EncinaError, Unit> RecordRefusal(string source, string refusedBy, DateTimeOffset? cooldownExpiresAtUtc, DateTimeOffset occurredAtUtc)` (ADR-053: argument guards throw, rule violations would be `Left`; there is none today, so both succeed).
   - `Apply(object)` switch for `ConsentRefused`: first event sets the identity fields; later events apply the anchoring rule of Design Choice 4 (keep the expiry while `OccurredAtUtc` is before it, otherwise take the new one) and increment `RefusalCount`.
2. **`src/Encina.Compliance.Consent/ReadModels/ConsentRefusalReadModel.cs`** (new), `IReadModel`: `Id`, `DataSubjectId`, `Purpose`, `TenantId`, `ModuleId`, `LastRefusedAtUtc`, `CooldownExpiresAtUtc`, `RefusalCount`, `LastModifiedAtUtc`, `Version`.
3. **`src/Encina.Compliance.Consent/ReadModels/ConsentRefusalProjection.cs`** (new): `IProjection<ConsentRefusalReadModel>`, `IProjectionCreator<ConsentRefused, ...>` and `IProjectionHandler<ConsentRefused, ...>` with the same anchoring rule; idempotent like `ConsentProjection`.
4. **`src/Encina.Compliance.Consent/ConsentMartenExtensions.cs`** (modify): `AddConsentAggregates` also calls `AddAggregateRepository<ConsentRefusalAggregate>()` and `AddProjection<ConsentRefusalProjection, ConsentRefusalReadModel>()` (lines 50-58).
5. **`PublicAPI.Unshipped.txt`** and the coverage manifest entries.
6. **Tests of this PR**: `ConsentRefusalAggregateTests`, `ConsentRefusalProjectionTests` (unit); `ConsentRefusalAggregateGuardTests` (guard); `ConsentRefusalPropertyTests` (property: the expiry never decreases across a series, the stream id is deterministic, tenant-sensitive and differs per purpose).
7. If the maintainer picks Design Choice 1 option A: replace items 1-3 with a `ConsentStatus.Refused` member, `ConsentAggregate.Refuse` and `ConsentProjection` handler, and adjust the guards in `DefaultConsentValidator` (lines 87-103).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of the consent cooldown in Encina.Compliance.Consent (issue #810).

CONTEXT:
- Phase 1 added the ConsentRefused event. Aggregates derive from AggregateBase (Encina.DomainModeling); read models are projected inline by Marten through Encina.Marten.Projections.
- ADR-053: aggregate commands return Either<EncinaError, ...>; argument guards still throw.
- Consent has no InMemory store; unit tests mock IAggregateRepository, integration tests use Marten on PostgreSQL.

TASK:
Create Aggregates/ConsentRefusalAggregate.cs (StreamIdFor, Refuse, RecordRefusal, Apply), ReadModels/ConsentRefusalReadModel.cs and ReadModels/ConsentRefusalProjection.cs as listed in the plan's Phase 2 Tasks, register aggregate and projection in ConsentMartenExtensions.AddConsentAggregates, add PublicAPI lines, and write the unit, guard and property tests of the Tasks list.

KEY RULES:
- The anchoring rule: a ConsentRefused whose OccurredAtUtc is before the current CooldownExpiresAtUtc keeps the expiry and only increments RefusalCount.
- Tenant id is part of the stream id; null and empty tenant are the same "no tenant" value.
- Time comes from the event (no DateTime.UtcNow); no personal data in exception or log text.
- Tests execute the real aggregate and projection (no reflection-only tests); Shouldly through Encina.Testing.Shouldly.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Aggregates/ConsentAggregate.cs
- src/Encina.Compliance.Consent/ReadModels/ConsentProjection.cs
- src/Encina.Compliance.Consent/ReadModels/ConsentReadModel.cs
- src/Encina.Compliance.Consent/ConsentMartenExtensions.cs
- src/Encina.Marten/MartenAggregateRepository.cs
- docs/architecture/adr/053-aggregate-rule-violations-return-either.md
- tests/Encina.UnitTests/Compliance/Consent/Aggregates/ConsentAggregateTests.cs
```

</details>

---

### Phase 3: Configuration & Options Validation

> **Goal**: The switch, the duration, the per-purpose flag and the floor. (PR 2)

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Compliance.Consent/ConsentOptions.cs`** (modify):
   - `public bool EnableOmnibusConsentCooldown { get; set; }` (default `false`).
   - `public TimeSpan ConsentCooldownAfterRefusal { get; set; } = MinimumConsentCooldownAfterRefusal;`
   - `public bool CooldownPerPurpose { get; set; } = true;`
   - `public static readonly TimeSpan MinimumConsentCooldownAfterRefusal = TimeSpan.FromDays(184);` (Design Choice 3; the XML docs explain why not 180).
   - XML docs name "COM(2025) 837, draft GDPR Art. 88a(4)", say "off by default; current law has no cooldown" and add an `<example>`.
2. **`src/Encina.Compliance.Consent/ConsentOptionsValidator.cs`** (modify): one failure when `ConsentCooldownAfterRefusal` is below the floor, whether or not the switch is on (the duration is used at refusal time). Update the validator's XML `<list>` of rules.
3. **`PublicAPI.Unshipped.txt`** and manifest entries for `ConsentOptions.cs` (unit) and `ConsentOptionsValidator.cs` (property, guard).
4. **Tests**: extend `ConsentOptionsTests` (defaults: switch false, duration 184 days, per purpose true) and `ConsentOptionsValidatorTests` (below floor fails, exactly at floor passes, switch off with a below-floor value still fails).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of the consent cooldown in Encina.Compliance.Consent (issue #810).

CONTEXT:
- ConsentOptions (ConsentOptions.cs) is a POCO bound through services.Configure; ConsentOptionsValidator is an internal IValidateOptions<ConsentOptions> registered in ServiceCollectionExtensions.AddEncinaConsent.
- SPEC-002 REQ-024: draft behaviour off by default, named by its draft article. Pre-1.0: no compatibility layers.

TASK:
Add EnableOmnibusConsentCooldown (default false), ConsentCooldownAfterRefusal (TimeSpan, default and floor 184 days) and CooldownPerPurpose (default true) to ConsentOptions, plus the floor constant, and validate the floor in ConsentOptionsValidator whether or not the switch is on. Extend the options and validator unit tests and add PublicAPI and manifest lines.

KEY RULES:
- Every option has XML docs with the draft article, the default and an example; no option may exist that no code reads (#3185): Phase 4 reads all three.
- Validation messages name the property and the floor, never a value that could identify a subject.
- If the maintainer chose 180 days in Design Choice 3, use TimeSpan.FromDays(180) and change the docs and tests accordingly.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ConsentOptions.cs
- src/Encina.Compliance.Consent/ConsentOptionsValidator.cs
- src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs (lines 80-96, the draft-law switch precedent)
- tests/Encina.UnitTests/Compliance/Consent/ConsentOptionsValidatorTests.cs
```

</details>

---

### Phase 4: Service Operations & Solicitation Decision

> **Goal**: Record a refusal and answer "may we ask?"; hot-swappable duration. (PR 2)

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Compliance.Consent/Services/ConsentCooldown.cs`** (new, `internal static`): pure functions kept apart from the service so the cyclomatic complexity of changed methods stays low (CRAP ≤ 10): `DateTimeOffset ExpiryFor(ConsentOptions options, DateTimeOffset occurredAtUtc)` (always `occurredAtUtc + ConsentCooldownAfterRefusal`, Design Choice 2) and `bool IsActive(ConsentRefusalReadModel refusal, DateTimeOffset nowUtc)`.
2. **`src/Encina.Compliance.Consent/Abstractions/IConsentService.cs`** (modify):
   - `ValueTask<Either<EncinaError, Guid>> RefuseConsentAsync(string dataSubjectId, string purpose, string source, string refusedBy, string? tenantId = null, string? moduleId = null, CancellationToken cancellationToken = default)`: returns the refusal stream id.
   - `ValueTask<Either<EncinaError, ConsentSolicitationDecision>> GetSolicitationDecisionAsync(string dataSubjectId, string purpose, CancellationToken cancellationToken = default)`.
3. **`src/Encina.Compliance.Consent/Services/DefaultConsentService.cs`** (modify):
   - Constructor takes `IOptionsMonitor<ConsentOptions>` instead of `IOptions<ConsentOptions>` and reads `CurrentValue` per call (Design Choice 5); the RequireTenantContext opt-out log stays in the constructor; `IsTenantContextRequired` (line 128) reads the current value.
   - New dependencies: `IAggregateRepository<ConsentRefusalAggregate>` and `IReadModelRepository<ConsentRefusalReadModel>`.
   - `RefuseConsentAsync`: `TryResolveWriteTenantScope`; compute the stream id; `LoadAsync`; on not found `ConsentRefusalAggregate.Refuse` then `CreateAsync`, and on `MartenErrorCodes.StreamAlreadyExists` reload and `RecordRefusal`; `SaveAsync`; the expiry is `ConsentCooldown.ExpiryFor` regardless of the switch; a refusal recorded while a cooldown is active is appended and logged (EventId in Phase 6). Exceptions other than cancellation become `ConsentErrors.ServiceError` with `ForLogging()`.
   - `GetSolicitationDecisionAsync`: `TryResolveTenantScope`; `HasValidConsentAsync` first (`ConsentActive`); when the switch is off return `Solicit` without querying refusals; otherwise query the refusal read model by tenant and subject (and purpose when `CooldownPerPurpose`) and return `CooldownActive` when any `ConsentCooldown.IsActive`; `Left` on any store error (Design Choice 10). Never cached.
4. **Tests**: new `DefaultConsentServiceRefusalTests` (mocked repositories, `FakeTimeProvider`, monitor stub whose `CurrentValue` changes between calls), extended `DefaultConsentServiceTests` constructor setup, `ConsentCooldownTests`, guard tests for the two methods, and `IConsentServiceContractTests` for the two methods.
5. **`PublicAPI.Unshipped.txt`** and manifest entries.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of the consent cooldown in Encina.Compliance.Consent (issue #810).

CONTEXT:
- Phases 1-3 added ConsentRefused, ConsentRefusalAggregate/Projection/ReadModel and the three options. DefaultConsentService (Services/DefaultConsentService.cs) wraps IAggregateRepository<ConsentAggregate> and IReadModelRepository<ConsentReadModel> and scopes every operation by the ambient tenant (TryResolveTenantScope / TryResolveWriteTenantScope).
- Compliance gates fail closed (AGENTS.md section 3). The cooldown is draft-law behaviour, off by default: with the switch off GetSolicitationDecisionAsync returns Solicit without reading refusals, while RefuseConsentAsync still records the event.

TASK:
Implement Services/ConsentCooldown.cs, the two IConsentService methods and their DefaultConsentService implementations as listed in the plan's Phase 4 Tasks, switch the service to IOptionsMonitor<ConsentOptions>, and write the unit, guard and contract tests.

KEY RULES:
- Time only from TimeProvider; the options are read per call through IOptionsMonitor.CurrentValue.
- Never log or tag the data subject id, the actor or EncinaError.Message; correlate by purpose and error code (#1314); exceptions go through ForLogging().
- GrantConsentAsync is NOT blocked by a cooldown (the draft restricts asking, not consenting).
- Keep changed methods at CRAP <= 10: put the branching in ConsentCooldown and small private helpers.
- Decision order when #811 has landed: Exempt, ConsentActive, CooldownActive, Solicit.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs (lines 55-130, 135-185, 378-428, 558-607)
- src/Encina.Compliance.Consent/Abstractions/IConsentService.cs
- src/Encina.Marten/MartenAggregateRepository.cs (lines 270-330)
- tests/Encina.UnitTests/Compliance/Consent/Services/DefaultConsentServiceTests.cs
- tests/Encina.ContractTests/Compliance/Consent/IConsentServiceContractTests.cs
- docs/plans/consent-exempt-purposes-implementation-plan-811.md (Phase 4)
```

</details>

---

### Phase 5: Cross-Cutting Integration

> **Goal**: Tenant, module, audit and the explicit non-integrations, each proven by a test. (PR 3)

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-tenancy**: the tenant id is in the stream id, on the event and on the read model; queries filter by `TenantId` through the existing helpers; a missing tenant fails closed when `RequireTenantContext` resolves to true. Add `ConsentCooldownIntegrationTests` cases: two tenants with the same subject and purpose do not see each other's cooldown; a missing tenant returns the tenant-required error.
2. **Module isolation**: `ModuleId` is carried on `ConsentRefused` and the read model as on `ConsentGranted`; `RefuseConsentAsync` takes it as a parameter (the pipeline context has no module id).
3. **Audit trail**: the refusal stream is the audit record; events are published as `INotification` by `EventPublishingPipelineBehavior` after the Marten commit. No second audit store.
4. **Caching**: the cooldown is not cached; add a test that two calls to `GetSolicitationDecisionAsync` around a `RefuseConsentAsync` see the refusal (no stale `Solicit`).
5. **Validation**: argument guards on both methods and the options floor (Phases 3-4).
6. **Idempotency**: a repeated or concurrent first refusal collapses on the deterministic stream id; an integration test runs two parallel `RefuseConsentAsync` calls and expects one stream with `RefusalCount` 2 and the first expiry.
7. **Registration completeness** (AGENTS.md §3, #1316): a DI test builds the provider with `ValidateOnBuild` and `ValidateScopes` for `AddEncinaConsent` plus `AddConsentAggregates` and resolves `IConsentService`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of the consent cooldown in Encina.Compliance.Consent (issue #810).

CONTEXT:
- Phases 1-4 are merged. AGENTS.md section 6 requires each of the 12 cross-cutting functions to be integrated, deferred or not applicable; the plan's matrix lists the outcome per function.
- Consent is event-sourced on Marten; integration tests use the Marten collection fixture on PostgreSQL through Testcontainers.

TASK:
Add the integration and DI tests listed in the plan's Phase 5 Tasks (two tenants, missing tenant, parallel first refusals, no stale decision after a refusal, ValidateOnBuild and ValidateScopes). Fix any production gap they reveal in the files of Phases 1-4.

KEY RULES:
- [Collection(MartenCollection.Name)], [Trait("Category","Integration")] and [Trait("Database","PostgreSQL")]; clear data in InitializeAsync; never call the fixture's DisposeAsync.
- A test never swallows an exception thrown by the code it drives.
- No Thread.Sleep; use FakeTimeProvider for time.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs
- tests/Encina.IntegrationTests/Compliance/Consent/ConsentPipelineIntegrationTests.cs
- tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs
- tests/Encina.UnitTests/Compliance/Consent/ServiceCollectionExtensionsTests.cs
```

</details>

---

### Phase 6: Observability

> **Goal**: Trace, count and log refusals and blocked solicitations; warn once when the draft behaviour is on. (PR 3)

<details>
<summary><strong>Tasks</strong></summary>

1. **`Diagnostics/ConsentDiagnostics.cs`** (modify, source `Encina.Compliance.Consent`): counters `consent.refusals.total` and `consent.solicitations.blocked.total` (instrument names follow ADR-054 D3: lowercase, dot-separated, as the existing `consent.granted.total`); tags `consent.purpose` and, when multi-tenancy is on, the tenant id (ADR-054 D4, SPEC-002 AC-044); activity `Consent.Solicitation` around `GetSolicitationDecisionAsync` and `Consent.Refusal` around `RefuseConsentAsync`. Never the subject id.
2. **`Diagnostics/ConsentCooldownLogMessages.cs`** (new, `internal static partial class`, `[LoggerMessage]`, XML doc "Event IDs: 8275-8278 (see EventIdRanges.ComplianceConsent)"):
   - 8275 `ConsentRefusalRecorded` (Information; purpose)
   - 8276 `ConsentRefusalDuringCooldown` (Warning; purpose; the application asked inside a cooldown)
   - 8277 `ConsentSolicitationBlockedByCooldown` (Information; purpose)
   - 8278 `ConsentCooldownEnabled` (Warning, once at startup; draft-law switch on; duration in days)
3. **Startup warning**: `ConsentAutoRegistrationHostedService.StartAsync` (line 46) logs 8278 before its early return when `EnableOmnibusConsentCooldown` is true, independent of `AutoRegisterFromAttributes`.
4. **EventIds**: no range change. 8275-8278 continue the packed sequence after #811's 8269-8274 inside `ComplianceConsent = (8200, 8299)` (`src/Encina/Diagnostics/EventIdRanges.cs:326`); the assembly is already mapped (`EncinaEventIdAllocationTests.cs:63`). If #810 lands first it takes 8269-8272 and #811 shifts; the second to land takes the next free block. Run the architecture tests.
5. **Tests**: in-memory exporter test for the two counters and the activity; a logger test for 8275-8278; a PII test that no log or tag carries the subject id (extend `ConsentPiiLeakTests`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of the consent cooldown in Encina.Compliance.Consent (issue #810).

CONTEXT:
- Package observability lives in Diagnostics/ConsentDiagnostics.cs (ActivitySource and Meter "Encina.Compliance.Consent") and Diagnostics/ConsentLogMessages.cs (LoggerMessage.Define, EventIds 8200-8268 used).
- ADR-021 and AGENTS.md section 7: EventIds are registered in src/Encina/Diagnostics/EventIdRanges.cs before use, packed sequentially; new code uses the [LoggerMessage] source generator.

TASK:
Add the counters, tags and activities, create Diagnostics/ConsentCooldownLogMessages.cs with EventIds 8275-8278 (or the next free block if #811 has not landed), call them from DefaultConsentService, and emit the startup warning from ConsentAutoRegistrationHostedService.StartAsync. Write the exporter, logger and PII tests.

KEY RULES:
- No data subject id, actor or EncinaError.Message in any log, tag or activity (#1314); the error code or exception type only.
- Run Encina.UnitTests architecture tests (EncinaEventIdAllocationTests, EventIdUniquenessRule) and fix any overlap.
- XML docs on the new internal class name the Event ID range.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Diagnostics/ConsentDiagnostics.cs
- src/Encina.Compliance.Consent/Diagnostics/ConsentLogMessages.cs
- src/Encina.Compliance.Consent/ConsentAutoRegistrationHostedService.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- tests/Encina.UnitTests/Compliance/Consent/ConsentPiiLeakTests.cs
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md (D3, D4)
```

</details>

---

### Phase 7: Testing

> **Goal**: Every applicable test type, per-flag targets met, AC-024 proven. (spread over the three pull requests; the AC-024 tests and the Marten tests land in PR 3)

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (new): `ConsentRefusalAggregateTests`, `ConsentRefusalProjectionTests`, `ConsentCooldownTests`, `DefaultConsentServiceRefusalTests`; (extended): `ConsentOptionsTests`, `ConsentOptionsValidatorTests`, `DefaultConsentServiceTests`.
2. **AC-024 tests** (PR 3, unit with mocks plus the Marten integration test):
   - the default follows current law: `new ConsentOptions().EnableOmnibusConsentCooldown` is false, and with a stored active refusal and the switch off `GetSolicitationDecisionAsync` returns `Solicit`;
   - the shape exists while off: with the switch off `RefuseConsentAsync` writes `ConsentRefused` with `CooldownExpiresAtUtc` equal to `OccurredAtUtc` plus the configured duration, and the read model reads back unchanged (Marten);
   - on: `CooldownActive` until the expiry, `Solicit` after `FakeTimeProvider` passes it; per-purpose and subject-wide modes; the hot swap: changing the duration and the switch through the options monitor stub changes the next call, and an invalid reload keeps the last valid value.
3. **Guard** (new `ConsentRefusalAggregateGuardTests`, extended `DefaultConsentServiceGuardTests`): null and whitespace arguments of both new methods and of the aggregate factories.
4. **Contract** (extended `IConsentServiceContractTests`): both methods return `Either`, fail closed on a missing tenant, and instantiate the real `DefaultConsentService`.
5. **Property** (new `ConsentRefusalPropertyTests`, FsCheck over generated instants and durations at or above the floor): a cooldown is active exactly while `now < OccurredAtUtc + duration`; the expiry never decreases across a series; refusals of different purposes do not affect each other when `CooldownPerPurpose` is true.
6. **Integration** (new `ConsentCooldownIntegrationTests`, Marten on PostgreSQL): the AC-024 round trip, two tenants, parallel first refusals, projection registered with a real store.
7. **Load and benchmark**: justification `.md` files, not code: add a cooldown section to `tests/Encina.LoadTests/Compliance/Consent/ConsentValidationLoadTests.md` and `tests/Encina.BenchmarkTests/Encina.Benchmarks/Compliance/Consent/ConsentBenchmarks.md` (low-throughput, off by default, one indexed read per decision).
8. **Per-flag obligations** (AGENTS.md §9): for each new or touched source file, targets and one-sentence justifications in the manifest (aggregate, projection and cooldown evaluator: unit 90, guard where it has parameters, property for the evaluator and the aggregate; `DefaultConsentService.cs`: raise its guard and add a unit target for the new methods; options and validator as in Phase 3; read model: unit through the projection). The pull request reports the measured per-flag coverage and the CRAP table of the changed methods (`.github/scripts/coverage-report.cs`, local crap-gate).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 (tests) of the consent cooldown in Encina.Compliance.Consent (issue #810).

CONTEXT:
- Test projects are consolidated under tests/ (Encina.UnitTests, GuardTests, ContractTests, PropertyTests, IntegrationTests); coverage is per flag from .github/coverage-manifest/Encina.Compliance.Consent.json; tests must execute real package code.
- AC-024 (SPEC-002): a test asserts the default follows current law; a test shows the added persisted state is written and read back unchanged while the option is off.

TASK:
Write the tests listed in the plan's Phase 7 Tasks, the two justification sections, and the manifest per-file targets with one-sentence justifications. Measure each flag with dotnet test --collect "XPlat Code Coverage" and the coverage-report script, and report per-flag coverage of the touched files against their targets plus the CRAP table.

KEY RULES:
- Assertions with Shouldly through Encina.Testing.Shouldly; no FluentAssertions; builders for test data; Arrange-Act-Assert; one behavior per test.
- Time through FakeTimeProvider; no Thread.Sleep; a test never swallows an exception thrown by the code it drives.
- Integration tests use the shared Marten collection fixture, never IClassFixture or new fixtures.
- Do not lower any target in the manifest (target ratchet).

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/Consent/ (all)
- tests/Encina.GuardTests/Compliance/Consent/DefaultConsentServiceGuardTests.cs
- tests/Encina.PropertyTests/Compliance/Consent/ConsentAggregatePropertyTests.cs
- tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs
- .github/coverage-manifest/Encina.Compliance.Consent.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 8: Documentation & Finalization

> **Goal**: XML docs, README, feature guide, changelog and the AC-024 documentation items. (PR 3)

<details>
<summary><strong>Tasks</strong></summary>

1. **XML docs** on every new public API (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`) naming "COM(2025) 837, draft GDPR Art. 88a(4)" and "off by default" (AC-024).
2. **`changelog.d/810-consent-cooldown-after-refusal.added.md`**: one or more bullets beginning with `-` (written by `mechanical-fixer`); never edit `CHANGELOG.md`.
3. **`src/Encina.Compliance.Consent/README.md`** (sections "Configuration Options", "Error Codes", "Consent Lifecycle", "GDPR Compliance"): the three options, the two service methods, the decision order, the off-by-default statement and the draft article (written by `docs-writer`).
4. **`docs/features/consent-management.md`** (section "Configuration Options", new section "Consent cooldown after refusal"): usage, the hot swap, the AC-024 statement.
5. **Package specification**: AC-024 needs the draft article named in the package specification, which #1209 creates; this plan adds the rows to #1209's specification when it exists, otherwise comments on #1209.
6. **`docs/INVENTORY.md`** and **`ROADMAP.md`**: update only if they list the Consent package files or the v0.15.0 items.
7. **ADR**: not required; the decisions are recorded in this plan, and the persisted-shape choice follows ADR-019 and ADR-053. Create one only if the maintainer's answer to Design Choice 1 or 5 sets a package-wide rule.
8. **Build and test verification**: `dotnet build --configuration Release` with 0 errors and 0 warnings; `dotnet test` for the five Consent test areas; every coverage flag reaches its own target in the manifest.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are finalizing the consent cooldown in Encina.Compliance.Consent (issue #810): documentation and verification.

CONTEXT:
- Documentation follows the encina-docs skill (.claude/skills/encina-docs/SKILL.md): one Diataxis quadrant per page, junior-readable English, real API names verified in src/, no hand-typed coverage figures.
- The behaviour is draft law (COM(2025) 837, draft GDPR Art. 88a(4)), off by default; the documentation says so wherever it names an option.

TASK:
Complete XML docs, the changelog fragment, the package README, the feature guide and the verification commands listed in the plan's Phase 8 Tasks.

KEY RULES:
- Never edit CHANGELOG.md; add changelog.d/810-consent-cooldown-after-refusal.added.md.
- Do not state a letter of Art. 88a(4) or the Joint Opinion cited by the issue unless verified against the proposal text.
- Never name a private client application; write "the reference application" if needed.
- Zero build warnings; PublicAPI files complete.

REFERENCE FILES:
- src/Encina.Compliance.Consent/README.md
- docs/features/consent-management.md
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (§3.5, REQ-024, AC-024)
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Relevance |
|--------|-----------|-----------|
| COM(2025) 837 (data and cyber Digital Omnibus), procedure 2025/0360(COD) | Draft GDPR Art. 88a(3)(c), 88a(4)(a) and (c) per SPEC-002 §3.5 line 136 | Single-click reject and six-month re-ask moratorium; not adopted; the letter that carries the moratorium and its wording (days or months) are unverified |
| GDPR, Reg. (EU) 2016/679 | Art. 5(2), Art. 7(1) | Accountability: the controller must show what it honoured, hence the persisted expiry per refusal |
| GDPR | Art. 5(1)(c), Art. 5(1)(e) | Data minimisation (no IP address on the event) and storage limitation (retention of refusal streams, Design Choice 6) |
| GDPR | Art. 6(1)(a), Recital 32 | Granular, purpose-specific consent: cooldown per purpose by default |
| ePrivacy Directive 2002/58/EC | Art. 5(3) | Current law for cookie consent; no cooldown; the default behaviour of Encina |
| EDPB-EDPS Joint Opinion 2/2026 (cited by the issue) | not given | Unverified from the repository; not cited in code or docs |
| SPEC-002 | REQ-024, DEC-012, AC-024, INV-003 | Draft behaviour off by default; shape persisted and round-tripped while off; closes before `1.0.0-rc.1` |
| SPEC-002 | DEC-006, DEC-009, REQ-061, REQ-062 | Fail-closed tenant scope; tenant-aware records; no personal data in telemetry |
| ADR-019, ADR-053, ADR-054 | Event-sourced compliance modules; Either-returning aggregate commands; observability naming and tenant tag | Aggregate and event shape, instrument names, tenant attribute |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `ConsentOptions` | `src/Encina.Compliance.Consent/ConsentOptions.cs:39-328` | Gains the draft-law switch, the cooldown duration and the per-purpose flag |
| `ConsentOptionsValidator` | `src/Encina.Compliance.Consent/ConsentOptionsValidator.cs:24-74` | Gains the cooldown floor rule |
| `ConsentAggregate` and `AggregateBase` | `src/Encina.Compliance.Consent/Aggregates/ConsentAggregate.cs:34` | Pattern for `ConsentRefusalAggregate` (Apply switch, RaiseEvent) |
| Consent events | `src/Encina.Compliance.Consent/Events/ConsentEvents.cs` | Record conventions (INotification, OccurredAtUtc, TenantId, ModuleId) |
| `ConsentProjection` and `ConsentReadModel` | `src/Encina.Compliance.Consent/ReadModels/` | Pattern for the refusal projection and read model |
| `DefaultConsentService` | `src/Encina.Compliance.Consent/Services/DefaultConsentService.cs:55-651` | Tenant scope helpers (`TryResolveTenantScope`, `TryResolveWriteTenantScope`); `RefuseConsentAsync` and `GetSolicitationDecisionAsync` are added here |
| `IConsentService` | `src/Encina.Compliance.Consent/Abstractions/IConsentService.cs:40-207` | Two new methods |
| `AddConsentAggregates` | `src/Encina.Compliance.Consent/ConsentMartenExtensions.cs:50-58` | Registers the refusal aggregate and projection |
| `MartenAggregateRepository.CreateAsync` and `StreamAlreadyExists` | `src/Encina.Marten/MartenAggregateRepository.cs:311-319` | Idempotent first refusal on a deterministic stream id |
| `ConsentDiagnostics` and `ConsentLogMessages` | `src/Encina.Compliance.Consent/Diagnostics/` | ActivitySource, Meter and EventId conventions |
| `ConsentAutoRegistrationHostedService` | `src/Encina.Compliance.Consent/ConsentAutoRegistrationHostedService.cs:46` | Runs once per host: place for the startup warning when the draft cooldown is on |
| `DataSubjectRightsOptions.EnableOmnibusArticle12RefusalGrounds` | `src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs:96` | Precedent for a draft-law switch that is off by default |
| `MartenCollection` fixture | `tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs:112` | Integration tests on PostgreSQL through Testcontainers |
| Consent-exempt purposes plan | `docs/plans/consent-exempt-purposes-implementation-plan-811.md` | Shares `ConsentSolicitationDecision`, `GetSolicitationDecisionAsync` and the deterministic stream id technique |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.Consent` (existing) | 8200-8299 (`EventIdRanges.ComplianceConsent`, `EventIdRanges.cs:326`) | Used: 8200-8207, 8230-8232, 8240-8243, 8250, 8260-8268 |
| `Encina.Compliance.Consent` exemptions (#811, planned) | 8269-8274 | Reserved by the #811 plan |
| **`Encina.Compliance.Consent` cooldown (new)** | **8275-8278** | Packed after #811; no new range; assembly already mapped in `EncinaEventIdAllocationTests` (line 63); if #810 lands first it takes 8269-8272 |
| `Encina.Compliance.Consent` remaining free | 8279-8299 | Left for later consent work |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New source files | 7 | Refusal events, aggregate, read model, projection, solicitation decision enum (unless #811 created it), cooldown evaluator, cooldown log messages |
| Modified source files | 9 | Options, validator, `IConsentService`, `DefaultConsentService`, Marten extensions, diagnostics, auto-registration hosted service, `PublicAPI.Unshipped.txt`, DI extensions only if needed |
| Unit test files | 7 | 4 new (aggregate, projection, evaluator, service refusal and decision) plus 3 extended (options, validator, service) |
| Guard test files | 2 | 1 new, 1 extended |
| Contract test files | 1 | `IConsentServiceContractTests` extended |
| Property test files | 1 | New cooldown invariants |
| Integration test files | 1 | New, Marten on PostgreSQL |
| Justification files | 2 | Load tests and benchmark tests (existing files, cooldown section added) |
| Documentation and manifest | 5 | Changelog fragment, package README, feature guide, coverage manifest, ROADMAP only if affected |
| **Total** | **35** | Split into three pull requests of 11, 14 and 12 files |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing the consent cooldown after refusal in Encina.Compliance.Consent for issue #810 (Digital Omnibus draft GDPR Art. 88a(4), off by default). Work in three pull requests (Phases 1-2, 3-4, 5-8), each with its own worker brief and at most 15 files and 1,500 changed lines.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0 (no backward compatibility, no [Obsolete]), Railway Oriented Programming with Either<EncinaError, T>.
- Encina.Compliance.Consent is event-sourced on Marten (PostgreSQL); no database provider matrix applies; no in-memory store; unit tests mock IAggregateRepository and integration tests use the Marten collection fixture.
- SPEC-002 REQ-024 / DEC-012 / AC-024: the draft behaviour is off by default, named by its draft article in XML docs and the package specification; a test proves the default follows current law; a test proves the added events and read model round-trip while the option is off. Closes before 1.0.0-rc.1.
- Rules: TimeProvider only; async calls with CancellationToken; no subject id, actor or EncinaError.Message in logs, traces or metrics; compliance gates fail closed; registration completeness proven with ValidateOnBuild and ValidateScopes; CRAP <= 10 on changed methods; per-flag coverage targets in the package manifest, never lowered.

IMPLEMENTATION OVERVIEW:
Phase 1: ConsentRefused event (RefusalId, DataSubjectId, Purpose, Source, RefusedBy, OccurredAtUtc, CooldownExpiresAtUtc, TenantId, ModuleId) and ConsentSolicitationDecision (Solicit, ConsentActive, CooldownActive).
Phase 2: ConsentRefusalAggregate on a deterministic stream id (version 8 UUID from SHA-256 of tenant, subject, purpose), ConsentRefusalReadModel, ConsentRefusalProjection (anchoring rule: a repeated refusal inside the cooldown does not extend it), registration in AddConsentAggregates.
Phase 3: EnableOmnibusConsentCooldown (false), ConsentCooldownAfterRefusal (TimeSpan, floor 184 days unless the maintainer chose 180), CooldownPerPurpose (true), validator floor.
Phase 4: ConsentCooldown evaluator, IConsentService.RefuseConsentAsync and GetSolicitationDecisionAsync, DefaultConsentService on IOptionsMonitor<ConsentOptions>; with the switch off no refusal is read and Solicit is returned, but refusals are still recorded.
Phase 5: tenant, module, audit and idempotency proven by Marten integration tests; DI completeness test.
Phase 6: counters, activities, [LoggerMessage] EventIds 8275-8278 in ComplianceConsent (8200-8299), startup warning when the switch is on.
Phase 7: unit, guard, contract, property and integration tests, AC-024 tests, justification sections, manifest targets.
Phase 8: XML docs, changelog.d/810-consent-cooldown-after-refusal.added.md, README, feature guide, build and test verification.

KEY PATTERNS:
- Aggregates: static factory raising the creation event; commands return Either (ADR-053); Apply(object) switch.
- Services: tenant scope as DefaultConsentService; exceptions become ConsentErrors.ServiceError with ForLogging(); decision order Exempt, ConsentActive, CooldownActive, Solicit.
- Options: draft-law switch like DataSubjectRightsOptions.EnableOmnibusArticle12RefusalGrounds.
- Integration tests: [Collection(MartenCollection.Name)], Category and Database traits, data cleared in InitializeAsync.
- Observability: ActivitySource + Meter "Encina.Compliance.Consent", [LoggerMessage] source generator, ADR-054 naming and tenant tag.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ (whole package)
- src/Encina.Marten/MartenAggregateRepository.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs
- docs/specifications/SPEC-002-eu-regulatory-readiness.md
- docs/plans/consent-exempt-purposes-implementation-plan-811.md
- docs/plans/consent-cooldown-implementation-plan-810.md (this plan)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The cooldown is deliberately not cached: a stale `Solicit` after a refusal would fail open (Design Choice 10). The existing five-minute read-model cache (`DefaultConsentService.cs:442-517`) is untouched and a test proves no stale decision (Phase 5) |
| 2 | OpenTelemetry | ✅ | `consent.refusals.total` and `consent.solicitations.blocked.total`, `Consent.Solicitation` and `Consent.Refusal` activities, purpose tag and tenant tag when multi-tenancy is on, never the subject id (Phase 6, ADR-054 D3 and D4) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` EventIds 8275-8278 in `ComplianceConsent` (8200-8299); startup warning when the draft switch is on (Phase 6) |
| 4 | Health Checks | ❌ | No new checkable dependency: the refusal store is the same Marten store `ConsentHealthCheck` already covers through `IConsentService` resolution |
| 5 | Validation | ✅ | Options floor in `ConsentOptionsValidator` and argument guards on both new methods (Phases 3 and 4) |
| 6 | Resilience | ❌ | No external call beyond the Marten store the package already uses; its failures return `Left` and the decision fails closed |
| 7 | Distributed Locks | ❌ | Concurrent first refusals are made safe by the deterministic stream id and `StreamAlreadyExists`; later appends rely on Marten optimistic concurrency, whose conflict is returned as `Left` |
| 8 | Transactions | ❌ | Each refusal appends to one stream, which Marten commits atomically with its inline projection; there is no multi-aggregate write, so no `IUnitOfWork` is needed (the issue's "atomic refusal and cooldown recording" holds by construction) |
| 9 | Idempotency | ✅ | Deterministic stream id per tenant, subject and purpose; a duplicate create is handled as "append to the existing stream" (Phase 4, tested in Phase 5) |
| 10 | Multi-Tenancy | ✅ | The issue said not applicable; `ConsentOptions.RequireTenantContext` (`ConsentOptions.cs:148`) shows consent is tenant-aware, so the cooldown is: tenant in the stream id, on the event and read model, reads filtered by tenant, fail closed on a missing tenant, two-tenant integration test (Phase 5) |
| 11 | Module Isolation | ✅ | `ModuleId` on `ConsentRefused` and the read model, as on `ConsentGranted`; `RefuseConsentAsync` takes it as a parameter (Phase 5) |
| 12 | Audit Trail | ✅ | The Marten stream of `ConsentRefusalAggregate` is the per-subject audit record, published as `INotification` after commit; the application's history read-back is the shared gap tracked in #3397 |

---

## Prerequisites & Dependencies

### Blocking

- None. Every type this plan uses exists on `main` at `7a0049c07`.

### Coordination

- **#811 (consent-exempt purposes, open)** touches `ConsentOptions`, `IConsentService`, `DefaultConsentService` and the solicitation answer. Run the two sequentially, not in parallel worktrees. Whichever lands first creates `ConsentSolicitationDecision`, `GetSolicitationDecisionAsync` and, if it is #811, the stream id helper; the second adds its member and branch. The second to land takes the next free EventId block (Phase 6).
- **#817 (IOptionsMonitor migration, open)**: if the maintainer picks Design Choice 5 option A, this issue switches `DefaultConsentService` first and #817 repeats the step for the other compliance classes; if option B, #810 waits for #817.
- **#1209 (Consent article-coverage specification, open)**: AC-024's "named in the package specification" lands there (Phase 8).
- **#1199 (channel-scoped consent)**, **#1197 (representation, deceased)**, **#1255 (minors)** change the consent purpose key and event model; no ordering requirement, the second to land rebases on the first. #1199 widens the purpose key, which the stream id must follow.
- **#3185 (options that nothing reads)**: every option added here is read by Phase 4 code and covered by a test, so none can be flagged.
- **#1316 (Consent contract tests are reflection-only, no Marten integration or DI test)**: Phases 5 and 7 add a Marten integration test and a `ValidateOnBuild` test for the new code; the rest of #1316 stays with that issue.

### Deferred

- Erasure, retention and archival of consent event streams (grants and refusals), and the missing link from Data Subject Rights to Consent data: one follow-up issue, `artifacts/issues/consent-streams-erasure-retention.md` (Design Choice 6).

---

## Next Steps

1. Link this plan from #810 and open the follow-up issue file for the orchestrator.
2. When work on #810 starts: re-check every Design Choice against today's code and open issues, then ask the maintainer the six Dilemmas (1 to 6); the answers go into `## Maintainer Decisions`.
3. Decide the order with #811 (Prerequisites & Dependencies) and write the worker brief for pull request 1 (Phases 1-2), with `Size:`, the exact file list and the per-file targets.
4. Implement pull request 1, then 2, then 3, each in its own worktree with one `issue-worker`; each phase is a self-contained commit.
5. Each pull request references #810 (only the last one says `Fixes #810`), reports per-flag coverage and CRAP of the touched files, and records the ADR-018 evaluation (the matrix above).
