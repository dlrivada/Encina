# Implementation Plan: `Encina.Compliance.AIAct.Transparency` — Transparency & Disclosure Obligations (Art. 13, Art. 50)

> **Issue**: [#841](https://github.com/dlrivada/Encina/issues/841) (child of [#881](https://github.com/dlrivada/Encina/issues/881), milestone v0.16.0 — AI Act)
> **Type**: Feature
> **Complexity**: Medium-High (8 phases, 6 pull requests, 1 new package (recommended), 1 core hook, no persistence, ~62 files)
> **Estimated Scope**: ~1,500-1,900 lines of production code + ~1,600-2,000 lines of tests
> **Prerequisites**: none blocking; the maintainer's placement decision (Design Choice 1) changes Phases 1-2 only

---

## Summary

Implement the runtime transparency duties of EU AI Act Article 50 (chatbot interaction, marking of synthetic content, emotion recognition and biometric categorisation notices, deepfake and published-text disclosure) and the content of Article 13 instructions for use as far as the library can serve it. The core already detects `[AITransparency]` and only logs; this plan turns the obligation into something the pipeline can verify, records every disclosure as audit evidence, and adds pluggable content marking for Art. 50(2).

### What is already in the code (verified on `7a0049c07`)

| Item | Location | State |
|---|---|---|
| `AITransparencyAttribute` (`DisclosureText`, `ObligationType`) | `src/Encina.Compliance.AIAct/Attributes/AITransparencyAttribute.cs` | Done; no role (provider or deployer) and no exemption |
| `TransparencyObligationType` (`AIGeneratedContent`, `DeepfakeContent`, `EmotionRecognition`, `BiometricCategorisation`, `ChatbotInteraction`) | `src/Encina.Compliance.AIAct/Model/TransparencyObligationType.cs` | Done; the vocabulary reused here |
| `RequiresTransparency` | `DefaultAIActClassifier.cs:76` (high-risk or limited-risk), `DefaultAIActComplianceValidator.cs:106` (also when the attribute is present) | Done |
| Pipeline handling | `AIActCompliancePipelineBehavior.cs:160` and `:274` (`LogObligations` logs `TransparencyObligation` only) | Log-only; no disclosure is produced, verified or recorded |
| `AIActErrors.TransparencyRequired` and code `aiact.transparency_required` | `AIActErrors.cs:41`, `:151` | Defined and never returned |
| `AISystemRegistration` | `Model/AISystemRegistration.cs` | Has `RegisteredAtUtc` but no placed-on-the-market date |
| Notifications through `IEncina.Publish` | `Notifications/*`, `InMemoryAISystemRegistry.cs:109` | Done; pattern reused |
| Enforcer, content marking, disclosure records | none | **This plan** |

### Deviations from the issue text (each is put to the maintainer below or follows a binding rule)

- **Placement is a Dilemma** (Design Choice 1, recorded by the issue itself); the plan is written for the recommended option B and states what changes under A.
- **Names that do not compile or collide.** `MarkedContent.MarkedContent` (CS0542) becomes `Content`; `ContentType` becomes `ContentKind`, as the issue already corrects.
- **`IContentMarker` cannot be text-only.** Art. 50(2) covers audio, image, video and text; the marker takes a `MarkingRequest` over bytes and a media type (Design Choice 3).
- **Roles.** Art. 50 splits the duty: providers (paragraphs 1 and 2) and deployers (paragraphs 3 and 4). `TransparencyRequirement` carries the obligated party; the issue's record does not.
- **Exemptions of Art. 50 are first-class.** "Obvious from context", assistive editing, law-enforcement authorisation, artistic works (limited disclosure) and editorial responsibility are an explicit, logged `TransparencyExemption` on the attribute, never an implicit skip.
- **Art. 13 is not a runtime type.** Art. 13(2)-(3) instructions for use are Annex IV point 1(h) content; the #840 plan's General Description section carries it. This plan records that in the article table instead of inventing a parallel type.
- **Placed-on-the-market date.** The Art. 50(2) grace period needs the date a system was placed on the market, which `AISystemRegistration` lacks; Phase 1 adds `PlacedOnMarketAtUtc` (nullable).
- **Dates.** Art. 50(2) marking for generative systems placed on the market before 2 Aug 2026 is due by 2 Dec 2026 (new Art. 111(4)); Annex III high-risk obligations apply from 2 Dec 2027 and Annex I from 2 Aug 2028 (Reg. (EU) 2026/1744, SPEC-002 §3.2).

### Scope per pull request (one worker issue = one PR, `AGENTS.md` §11)

About 62 files and 3,300 lines, so six sequential pull requests.

| PR | Phases | Content | Files (est.) | Lines (est.) |
|---|---|---|---|---|
| 1 | 1 | Core hook, attribute exemption, registration date, enforcement option | 10 | ~700 |
| 2 | 2 | Package skeleton, models, `ITransparencyEnforcer`, `IContentMarker` | 15 | ~800 |
| 3 | 3 | Requirement catalog, enforcer, disclosure ledger, date rules, options | 11 | ~900 |
| 4 | 4-5 | Manifest marker, DI, audit notifications | 11 | ~800 |
| 5 | 6-7 | Observability, tests, justifications | 9 | ~700 |
| 6 | 8 | Documentation (`docs-writer`), changelog, ADR | 6 | ~600 |

Counts are re-measured when each worker brief is written. **Affected packages**: `Encina.Compliance.AIAct` (modified), `Encina.Compliance.AIAct.Transparency` (new, under option B).

**Provider category**: none; the 10-provider database rule does not apply (Marten event-sourced module, ADR-019; persistence of disclosures is #847).

**Family coordination (AI Act modules #838, #840, #841, #843).** Nothing here redefines the bias types of #838. Art. 13 content lives in #840's General Description section. EventIds 9576-9587 are this plan's (#838 holds 9548-9561, #840 holds 9562-9575, #843 holds 9588-9599).

---

## Design Choices

<details>
<summary><strong>1. Placement: in the core package or in a new package with a hook in the core</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Inside `Encina.Compliance.AIAct`** | `AIActCompliancePipelineBehavior` calls `ITransparencyEnforcer` directly; one `AddEncinaAIAct` call gives enforcement to anyone using `[AITransparency]`; no hook abstraction | Every user of the core carries marking code and its optional dependencies (C2PA tooling); against pay-for-what-you-use for applications that only classify |
| **B — New package `Encina.Compliance.AIAct.Transparency` plus a small hook interface in the core (`ITransparencyObligationHook`); with `[AITransparency]` present, `RequiresTransparency` true, Block mode and no hook registered, the pipeline returns `Left(aiact.transparency_required)`, unless `AIActOptions.TransparencyEnforcement` is set to `LogOnly` explicitly (logged)** | C2PA or watermark dependencies stay out of the core; consistent with every sibling module of EPIC #881; an application that forgets the package fails closed instead of logging silently | A hook abstraction and an option in the core; existing users of `[AITransparency]` must register the package or opt out |
| **C — Package only, no hook; the application calls the enforcer itself** | No core change | Nothing verifies that a disclosure happened; the pipeline stays log-only |

### Chosen Option: **B — New package with a fail-closed hook in the core** (recommended, pending the maintainer)

### Rationale

We recommend B for the reasons the issue gives (every sibling is a package; pay-for-what-you-use) and because the hook's fail-closed default turns today's log-only behavior into an enforced one without coupling the core to marking code. A wins where nearly every user of the core needs disclosure and wants a single registration; then Phases 1-2 collapse into a `Transparency/` folder in the core, the hook interface disappears and the rest of the plan is unchanged. C leaves the gap the issue is meant to close.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. When the obligation is determined and verified in the pipeline</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Determine before the handler (a scoped `ITransparencyContext` carries the requirement and `DisclosureText` so the handler or presentation layer can show it); verify after the handler that a disclosure was recorded in that scope; unmet in Block mode returns `Left(TransparencyRequired)`, in Warn mode logs** | The obligation is visible while the response is built; unmet duties are caught; matches the existing unused `AIActErrors.TransparencyRequired` | A failure after the handler has run (side effects done); the handler must call `RecordDisclosureAsync` |
| **B — Determine only (requirement into the context), never verify** | No late failure | An unmet duty is invisible, "cannot be shown to be met" is not detected |
| **C — Verify before the handler (an already recorded disclosure for the recipient context is required to run)** | No late failure; blocks before side effects | Impossible for first interaction: the notice is shown as part of the first response (Art. 50(5): at the latest at the first interaction) |

### Chosen Option: **A — Determine before, verify after** (recommended, pending the maintainer)

### Rationale

We recommend A. Art. 50(5) allows the information at the latest at the time of first interaction or exposure, so a pre-handler check (C) cannot be satisfied by a first response, and B cannot show compliance. A's late failure is acceptable because the check is about the response being returned: a response that should have carried the notice and did not is not returned in Block mode. C wins for a system with a separate consent screen before any request; that application records the disclosure before calling and A passes immediately.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Content representation and the default marking method</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — `MarkingRequest(ReadOnlyMemory<byte> Content, ContentKind Kind, string MediaType)`; the default marker `ManifestContentMarker` returns the content unchanged plus a machine-readable JSON manifest (claim "artificially generated", generator system id, kind, SHA-256 of the content, `MarkedAtUtc`); `IsMarkedAsync(content, manifest)` verifies the hash; C2PA and watermark markers plug in behind the same interface** | Works for every kind without format libraries; machine-readable and detectable; no heavy dependency | The marking travels beside the content, not inside it, until a C2PA or watermark marker is registered |
| **B — Text-only strings with an inline trailer** | Simple; marking inside the text | Cannot mark image, audio or video; a trailer is trivially stripped |
| **C — No default marker; the application must register one (`Left` otherwise)** | No misleading default | Out-of-the-box use fails; every user writes a marker |

### Chosen Option: **A — Bytes plus a sidecar manifest default** (recommended, pending the maintainer)

### Rationale

We recommend A: Art. 50(2) requires outputs "marked in a machine-readable format and detectable" and technical solutions "effective, interoperable, robust and reliable as far as this is technically feasible"; a sidecar manifest meets machine-readable and detectable for every kind, and says honestly (XML docs, feature page) that robustness against stripping needs an embedded method such as C2PA. B wins only for a text-only product; it can be registered as a custom marker. C wins where the maintainer judges a sidecar default too weak to count as compliance; then no default is registered and `Left(aiact.transparency_marker_not_registered)` is returned.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Who is bound and which exemptions exist</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — A static catalog mapping each `TransparencyObligationType` to its article, obligated party and exemptions: chatbot Art. 50(1) (provider), generated content Art. 50(2) (provider), emotion recognition and biometric categorisation Art. 50(3) (deployer), deepfake Art. 50(4) first subparagraph (deployer), published AI text Art. 50(4) second subparagraph (deployer); `[AITransparency(Exemption = ...)]` selects an explicit, logged exemption** | Mirrors the Article; every exemption is a visible decision | The enum has no value for published AI text; one is added |
| **B — Every obligation applies to everyone, no exemptions** | Simplest | Wrong in law (Art. 50(1)-(4) exceptions) and unusable for legitimate cases |

### Chosen Option: **A — Article-driven catalog with explicit exemptions**

### Rationale

Art. 50 is explicit about who and when. Exemptions modeled: obvious to a reasonably well-informed person (50(1)), assistive editing or no substantial alteration (50(2)), authorised by law for law-enforcement purposes (50(1)-(4)), evidently artistic, creative, satirical or fictional work with limited disclosure (50(4)), human review or editorial responsibility for published text (50(4)). An exemption is the only opt-out and is logged with the system id (`AGENTS.md` §3). The exact scope of the law-enforcement exemptions follows the Article text; interpretation beyond it is the application's.

**Class:** Settled
**Settled by:** Art. 50(1)-(5) AI Act; `AGENTS.md` §3 (fail closed; the only opt-out is explicit and logged); issue #841 Alternatives Considered items 2 and 4

</details>

<details>
<summary><strong>5. Date-dependent Art. 50(2) duty</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Evaluate with `TimeProvider` and `AISystemRegistration.PlacedOnMarketAtUtc`: a generative system placed on the market before 2 Aug 2026 is not required to mark before 2 Dec 2026 (new Art. 111(4)); a system with no recorded date gets no grace (fail closed); the cutoff dates are options defaults, not constants** | Follows the law on the date (SPEC-002 INV-003); fail closed on missing context | Core registration gains a nullable property |
| **B — Ignore the grace period; always enforce** | No date logic | Wrongly blocks lawful pre-existing systems until 2 Dec 2026 |
| **C — Always allow until 2 Dec 2026** | Simple | Wrongly relaxes systems placed on the market after 2 Aug 2026 |

### Chosen Option: **A — Date-aware with fail-closed default**

### Rationale

New Art. 111(4) (read on the consolidated text): providers of AI systems, including general-purpose AI systems, generating synthetic audio, image, video or text content that have been placed on the market before 2 August 2026 shall comply with Art. 50(2) by 2 December 2026. The defaults follow SPEC-002 §3.2 and INV-003; the cutoffs live in `TransparencyOptions`.

**Class:** Settled
**Settled by:** Art. 111(4) AI Act as amended by Reg. (EU) 2026/1744; SPEC-002 §3.2 and INV-003; `AGENTS.md` §3 (compliance gates fail closed)

</details>

<details>
<summary><strong>6. Disclosure records and EventIds</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Disclosures are scoped records in `ITransparencyContext` plus an audit notification; durable storage is #847; EventIds 9576-9587 sub-allocated in `ComplianceAIAct`** | No store here; follows #842 and #847; audit evidence without InMemory stores | No history until #847 |
| **B — An in-memory disclosure store** | A queryable history now | An InMemory store in an event-sourced module (`AGENTS.md` §3) |

### Chosen Option: **A — Scoped record plus notification**

### Rationale

Event-sourced compliance modules have no InMemory stores; the notification is the evidence until #847 persists it. `DisclosedAtUtc` and `MarkedAtUtc` come from `TimeProvider`.

**Class:** Trivial
**Settled by:** `AGENTS.md` §3 and §7; ADR-021; SPEC-002 DEC-008; the EventId sections of the #842 and #847 plans; `src/Encina/Diagnostics/EventIdRanges.cs:389`

</details>

---

## Implementation Phases

### Phase 1: Core Hook, Exemptions & Registration Date (PR 1, `Encina.Compliance.AIAct`)

> **Goal**: The core can ask an enforcer, fails closed without one and knows the placement date.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `Abstractions/ITransparencyObligationHook.cs`**: `ValueTask<Either<EncinaError, Unit>> BeginAsync(TransparencyHookContext context, CancellationToken)` and `ValueTask<Either<EncinaError, Unit>> CompleteAsync(TransparencyHookContext context, CancellationToken)`; `Model/TransparencyHookContext.cs` (`RequestType`, `SystemId`, `ObligationType`, `DisclosureText`, `Exemption`).
2. **Create `Model/TransparencyExemption.cs`**: `None`, `ObviousFromContext`, `AssistiveEditing`, `LawEnforcementAuthorised`, `ArtisticOrSatirical`, `EditorialResponsibility`; add `TransparencyObligationType.PublishedAIText`.
3. **Modify `Attributes/AITransparencyAttribute.cs`**: `TransparencyExemption Exemption { get; set; }`.
4. **Modify `Model/AISystemRegistration.cs`**: `DateTimeOffset? PlacedOnMarketAtUtc { get; init; }`.
5. **Modify `AIActOptions.cs`**: `TransparencyEnforcement` (`Enforce` default, `LogOnly`) and its use in `AIActOptionsValidator`.
6. **Modify `AIActCompliancePipelineBehavior.cs`** (`LogObligations`, lines ~160 and ~274): when `RequiresTransparency`, call the hook before `nextStep()` and `CompleteAsync` after; no hook in `Enforce` mode returns `Left(AIActErrors.TransparencyRequired(...))`; `LogOnly` keeps the log and logs the opt-out.
7. **Modify `PublicAPI.Unshipped.txt`**; tests: pipeline with hook, without hook (both modes), exemption logged, registration date round trip; manifest targets.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #841 in Encina.Compliance.AIAct.

CONTEXT:
- The pipeline only logs transparency obligations (AIActCompliancePipelineBehavior.cs LogObligations); AIActErrors.TransparencyRequired
  exists and is never returned. Design Choices 1 and 2 of the plan: a hook in the core, determine before and verify after,
  fail closed when no hook is registered unless TransparencyEnforcement = LogOnly.

TASK:
Add ITransparencyObligationHook, TransparencyHookContext, TransparencyExemption, PublishedAIText, the attribute Exemption,
PlacedOnMarketAtUtc, the option and validator rule, and the pipeline calls with tests and PublicAPI entries.

KEY RULES:
- Fail closed: missing hook in Enforce mode is a Left, never a silent pass; LogOnly is explicit and logged.
- No [Obsolete], no compatibility members; pipeline behavior stays CRAP <= 10 per method (extract helpers).
- A Left from the hook fails the request in Block mode and is logged by code in Warn mode.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs
- src/Encina.Compliance.AIAct/AIActErrors.cs
- src/Encina.Compliance.AIAct/AIActOptions.cs
- tests/Encina.UnitTests/Compliance/AIAct/AIActCompliancePipelineBehaviorTests.cs
```

</details>

---

### Phase 2: Package Skeleton, Models & Abstractions (PR 2)

> **Goal**: Buildable package with the contracts.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create the project** `src/Encina.Compliance.AIAct.Transparency/` (csproj referencing `Encina.Compliance.AIAct`, `PublicAPI.*.txt`, `InternalsVisibleTo` the test projects), add to `Encina.slnx`, create its coverage manifest.
2. **Create models**: `TransparencyRequirement` (`ObligationType`, `DisclosureText`, `IsMandatory`, `ArticleReference`, `ObligatedParty`, `Exemption`), `TransparencyObligatedParty` (`Provider`, `Deployer`), `TransparencyDisclosure` (as in the issue), `ContentKind` (`Text`, `Image`, `Audio`, `Video`, `Code`, `Multimodal`), `MarkingRequest`, `MarkedContent` (`Content`, `Manifest`, `Kind`, `MarkingMethod`, `MarkedAtUtc`).
3. **Create abstractions**: `ITransparencyEnforcer` (`DetermineRequirementsAsync<TRequest>`, `RecordDisclosureAsync`), `IContentMarker` (`MarkAsync(MarkingRequest, ...)`, `IsMarkedAsync(ReadOnlyMemory<byte>, ReadOnlyMemory<byte> manifest, ...)`, `string Method`).
4. **Create `TransparencyErrors.cs`**: codes `aiact.transparency_system_not_found`, `aiact.transparency_marker_not_registered`, `aiact.transparency_invalid_disclosure`, `aiact.transparency_marking_failed`.
5. **Tests**: records, errors, guards; manifest targets.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #841: the new package Encina.Compliance.AIAct.Transparency.

CONTEXT:
- Phase 1 delivered the core hook, TransparencyExemption and PlacedOnMarketAtUtc. Names that collide are renamed: ContentKind (not
  ContentType), MarkedContent.Content (not MarkedContent.MarkedContent, CS0542).

TASK:
Create the project, slnx entry, PublicAPI files, manifest, models, the two abstractions and the errors class, with tests.

KEY RULES:
- Either<EncinaError, T> everywhere; errors carry codes, never exception messages.
- XML docs cite the Art. 50 paragraph; English only; zero warnings.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/Encina.Compliance.AIAct.csproj
- src/Encina.Compliance.AIAct/Model/TransparencyObligationType.cs
- src/Encina.Compliance.AIAct/AIActErrors.cs
- .github/scripts/generate-coverage-manifest.cs
```

</details>

---

### Phase 3: Requirement Catalog, Enforcer & Date Rules (PR 3)

> **Goal**: The package decides what must be disclosed to whom and by when.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `Catalog/Article50Catalog.cs`** (internal static): per `TransparencyObligationType` the article reference, party and permitted exemptions (Design Choice 4).
2. **Create `Dates/Article50ApplicabilityRules.cs`** (internal): given the registration, the obligation and `TimeProvider.GetUtcNow()` decide `Applies` or `GracePeriodUntil`; no `PlacedOnMarketAtUtc` means no grace (Design Choice 5).
3. **Create `TransparencyOptions.cs`** and validator: `MarkingGraceCutoffPlacedBeforeUtc` (2026-08-02), `MarkingGraceEndsUtc` (2026-12-02), minimum disclosure text length, allowed exemptions per obligation.
4. **Create `DefaultTransparencyEnforcer.cs`** (`ITransparencyEnforcer`): resolve the registration through `IAISystemRegistry` (`Left(aiact.transparency_system_not_found)` when unknown), build the requirement, validate and record a disclosure (`DisclosureId` non-empty, matching system, text not blank) into the scoped ledger.
5. **Create `Context/TransparencyContext.cs`** (scoped): requirement and recorded disclosures of one request.
6. **Create `TransparencyObligationHook.cs`** (`ITransparencyObligationHook`): `BeginAsync` determines and stores, `CompleteAsync` verifies a disclosure matching the requirement exists unless exempt or in grace period.
7. **Tests**: every obligation type; each exemption; grace period boundaries (before, at and after each date, missing `PlacedOnMarketAtUtc`); unknown system; unmet and met verification; `FakeTimeProvider`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #841 in Encina.Compliance.AIAct.Transparency.

CONTEXT:
- Design Choices 2, 4 and 5: Article 50 catalog (provider duties 50(1)-(2), deployer duties 50(3)-(4)), explicit exemptions, determine
  before and verify after, grace to 2 Dec 2026 only for systems with a recorded placement before 2 Aug 2026.

TASK:
Implement Article50Catalog, Article50ApplicabilityRules, TransparencyOptions + validator, TransparencyContext,
DefaultTransparencyEnforcer and TransparencyObligationHook.

KEY RULES:
- Missing context fails closed: unknown system or missing placement date never means "no obligation".
- Time only from TimeProvider; dates come from options, not constants.
- An exemption is explicit and logged; no exception becomes success; CRAP <= 10 per method.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/Abstractions/ITransparencyObligationHook.cs
- src/Encina.Compliance.AIAct/DefaultAIActComplianceValidator.cs
- src/Encina.Compliance.AIAct/InMemoryAISystemRegistry.cs
- https://artificialintelligenceact.eu/article/50/
```

</details>

---

### Phase 4: Content Marking (PR 4)

> **Goal**: Pluggable marking with a dependency-free default.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `Marking/ContentManifest.cs`** (internal record serialized with System.Text.Json: claim `"artificially-generated"`, `SystemId`, `Kind`, `Sha256`, `MarkedAtUtc`, `Method`).
2. **Create `Marking/ManifestContentMarker.cs`** (`IContentMarker`, method `"manifest"`): content unchanged, manifest bytes returned in `MarkedContent.Manifest`; `IsMarkedAsync` recomputes the hash and compares; constant-time comparison.
3. **Document the plug-in points** for C2PA and watermark markers (no dependency, no implementation); the registration helper `AddContentMarker<T>()`.
4. **Tests**: mark then verify round trip for each `ContentKind`, tampered content fails verification, empty content is a `Left`, property test (marking is deterministic given the clock).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #841 in Encina.Compliance.AIAct.Transparency.

CONTEXT:
- Design Choice 3: bytes plus a sidecar manifest default; C2PA and watermark markers are future plug-ins behind IContentMarker.

TASK:
Implement ContentManifest, ManifestContentMarker and the AddContentMarker<T>() helper with tests.

KEY RULES:
- No dependency on C2PA tooling by default (pay-for-what-you-use). Constant-time hash comparison.
- The XML docs state that a sidecar manifest does not survive stripping and that embedded methods are needed for robustness.
- Time from TimeProvider; Either<EncinaError, T>; CRAP <= 10.

REFERENCE FILES:
- src/Encina.Compliance.AIAct.Transparency/Abstractions/IContentMarker.cs
- https://c2pa.org/
- tests/Encina.PropertyTests/Compliance/AIAct/
```

</details>

---

### Phase 5: Configuration, DI & Audit Notifications (PR 4)

> **Goal**: One registration call and audit evidence.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `Notifications/TransparencyDisclosureRecordedNotification.cs`** and `ContentMarkedNotification.cs` (system id, obligation or kind, method, `OccurredAtUtc`; never content), published through `IEncina.Publish`; a publish `Left` fails the operation.
2. **Create `ServiceCollectionExtensions.cs`**: `AddEncinaAIActTransparency(Action<TransparencyOptions>?)` registers options, validator, `TimeProvider`, `TransparencyContext` (scoped), enforcer, hook, `ManifestContentMarker` (`TryAdd`, an application marker wins).
3. **DI test** with `ValidateOnBuild` and `ValidateScopes`, in both registration orders with `AddEncinaAIAct`; a test that the pipeline now verifies disclosures end to end.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #841 in Encina.Compliance.AIAct.Transparency.

CONTEXT:
- Enforcer, hook and marker exist. Core notifications are published with IEncina.Publish.

TASK:
Add the two audit notifications and AddEncinaAIActTransparency with a ValidateOnBuild/ValidateScopes DI test.

KEY RULES:
- Registration completeness (AGENTS.md section 3): every option type and dependency registered; an application's own marker or hook wins.
- Notifications never carry content or disclosure text bodies longer than the id; a Left from Publish fails the operation.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/ServiceCollectionExtensions.cs
- src/Encina.Compliance.AIAct/Notifications/ProhibitedUseBlockedNotification.cs
- tests/Encina.UnitTests/Compliance/AIAct/ServiceCollectionExtensionsTests.cs
```

</details>

---

### Phase 6: Observability (PR 5)

> **Goal**: Spans, counters and twelve log events.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `Diagnostics/TransparencyDiagnostics.cs`**: source and meter `Encina.Compliance.AIAct.Transparency`; spans `aiact.transparency.determine` (request type, obligation type) and `aiact.transparency.mark` (content kind, method); counters `aiact.transparency.requirements_determined` (tag obligation type), `aiact.transparency.disclosures_recorded` (tag obligation type), `aiact.transparency.content_marked` (tag method).
2. **Create `Diagnostics/TransparencyLogMessages.cs`**, `[LoggerMessage]`, ids 9576-9587 packed: requirement determined, exemption applied, enforcer missing (opt-out or blocked), disclosure recorded, disclosure missing (blocked), disclosure missing (warned), content marked, marking failed, marker not registered, grace period applied, notification publish failed, disclosure invalid.
3. **Modify `EncinaEventIdAllocationTests.AssemblyRanges`**: `["Encina.Compliance.AIAct.Transparency"] = [nameof(EventIdRanges.ComplianceAIAct)]`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #841 in Encina.Compliance.AIAct.Transparency.

CONTEXT:
- ComplianceAIAct is 9500-9599: 9548-9561 #838, 9562-9575 #840, 9576-9587 this issue, 9588-9599 #843.

TASK:
Create TransparencyDiagnostics and TransparencyLogMessages (12 [LoggerMessage] methods, ids 9576-9587, no gaps), map the
assembly in the architecture test, instrument the enforcer, hook and marker.

KEY RULES:
- ADR-021: no id outside the range; run the architecture tests. XML line: Event IDs: 9576-9587 (see EventIdRanges.ComplianceAIAct).
- Never log or tag EncinaError.Message or content; error codes, system ids and ForLogging() only.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Compliance.AIAct/Diagnostics/AIActDiagnostics.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 7: Testing (spread over the PRs, completed in PR 5)

> **Goal**: Every flag reaches its target.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit**: catalog, applicability rules, enforcer, context, hook, marker, notifications, pipeline integration with `FakeTimeProvider`.
2. **Guard**: every public constructor and method parameter, in both packages.
3. **Contract**: `ITransparencyEnforcer`, `IContentMarker`, `ITransparencyObligationHook` on real implementations (the manifest marker and a test marker).
4. **Property**: mark then `IsMarkedAsync` round trip for random content; requirement determination total over every obligation type; grace-period monotonic in time.
5. **DI test** (Phase 5). **Integration**, **Load**, **Benchmark**: `.md` justifications under `Compliance/AIAct/Transparency/` (no store, no concurrency, marking cost is linear in size and not a hot path).
6. **Coverage manifest**: per-file targets and justifications; measure each flag; local CRAP table for changed methods (<= 10).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are completing the tests of issue #841 in Encina.Compliance.AIAct.Transparency and the core hook.

CONTEXT:
- Phases 1-6 are implemented; earlier PRs brought their own tests.

TASK:
Write the missing tests and the three .md justifications; bring every flag to its manifest target in both manifests.

KEY RULES:
- Tests execute real code; Shouldly via Encina.Testing.Shouldly; FakeTimeProvider; no Thread.Sleep. Do not lower any target.
- Justification files follow the AGENTS.md section 9 template.

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/AIAct/
- tests/Encina.BenchmarkTests/Compliance/AIAct/AIAct.md
- .github/coverage-manifest/Encina.Compliance.AIAct.Transparency.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 8: Documentation & Finalization (PR 6)

> **Goal**: Documentation, changelog, public API and verification.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on all public APIs (`<summary>`, `<remarks>` with the Art. 50 paragraph, `<param>`, `<returns>`, `<example>`).
2. `changelog.d/841-aiact-transparency.added.md` (and a `changed` fragment for the fail-closed pipeline behavior), written by `mechanical-fixer`.
3. Package README; `docs/features/aiact-compliance.md` Art. 13 and Art. 50 section with the article table (SPEC-000 REQ-024: Art. 13 covered through #840 Annex IV 1(h); Art. 50(1)-(4) covered; Art. 50(5) accessibility is the application's) through `docs-writer`.
4. `docs/INVENTORY.md`, `ROADMAP.md`; ADR (next number neither used nor reserved in `docs/architecture/adr/index.md`, reserved by PR 1): hook in the core, determine-before and verify-after, sidecar default.
5. `PublicAPI.Unshipped.txt` complete in both packages; build with 0 warnings; tests; every flag at target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of issue #841 in Encina.Compliance.AIAct.Transparency.

CONTEXT:
- Phases 1-7 are merged and measured.

TASK:
Write the changelog fragments, README, feature-page section with the article table, INVENTORY, ROADMAP and ADR; finish the
public API files; verify build, tests and per-flag coverage.

KEY RULES:
- Never edit CHANGELOG.md [Unreleased]; fragments only. Docs never type coverage figures by hand (covref markers, SPEC-001).
- Pages go through the docs-writer agent; state the dates of Reg. (EU) 2026/1744 and that the sidecar default is not tamper-proof.
- English only; no AI attribution; zero warnings.

REFERENCE FILES:
- changelog.d/README.md
- docs/features/aiact-compliance.md
- .claude/skills/encina-docs/SKILL.md
- docs/architecture/adr/index.md
```

</details>

---

## Research

### Relevant Standards & Specifications

Checked 2026-11-11 on the consolidated text at artificialintelligenceact.eu ("text read"); the EUR-Lex Official Journal page returned an empty challenge page, so the Official Journal itself was not read. Everything else is unverified.

| Provision | Requirement | Where in this plan | Status |
|-----------|-------------|--------------------|--------|
| AI Act Art. 50(1) | Providers design systems interacting directly with natural persons so that persons are informed; exceptions: obvious to a reasonably well-informed person, law-enforcement authorisation | Catalog (`ChatbotInteraction`), Phase 3 | Text read |
| AI Act Art. 50(2) | Providers of systems generating synthetic audio, image, video or text mark outputs machine-readably and detectable; effective, interoperable, robust, reliable as far as technically feasible; exceptions: assistive editing, no substantial alteration, law enforcement | `IContentMarker`, Phase 4 | Text read |
| AI Act Art. 50(3) | Deployers of emotion recognition or biometric categorisation systems inform exposed persons and process data under GDPR; law-enforcement exception | Catalog, Phase 3 | Text read |
| AI Act Art. 50(4) | Deployers disclose deepfakes (limited disclosure for evidently artistic works) and AI-generated text published to inform the public on matters of public interest (not where human review or editorial responsibility exists) | Catalog (`DeepfakeContent`, new `PublishedAIText`), Phase 3 | Text read |
| AI Act Art. 50(5) | Information clear and distinguishable at the latest at first interaction or exposure; accessibility | Design Choice 2; accessibility left to the application | Text read |
| AI Act Art. 13(1)-(3) | High-risk systems sufficiently transparent; accompanied by instructions for use with concise, complete, correct, clear information; minimum content list | Carried by Annex IV 1(h) in #840 | Text read for paragraphs 1-2 and the start of paragraph 3; the full list of 13(3) items is unverified here |
| AI Act Art. 111(4) (new) | Generative systems placed on the market before 2 Aug 2026 comply with Art. 50(2) by 2 Dec 2026 | Design Choice 5, Phase 3 | Text read, marked as new by the amendment |
| AI Act Art. 113 (application date of Art. 50) | Art. 50 applies from 2 Aug 2026 | Phase 3 options | Unverified here (not read) |
| Reg. (EU) 2026/1744 | Annex III high-risk from 2 Dec 2027, Annex I from 2 Aug 2028 | Feature page, XML docs | From SPEC-002 §3.2 and the issue comment; not re-read in the Official Journal |
| C2PA Content Credentials | Provenance standard for embedded marking | Plug-in point, Phase 4 | Unverified here; link only |
| SPEC-000 REQ-024, REQ-025; SPEC-002 DEC-008, INV-003 | Article table; scope; persistence; law on the date | Phases 3, 8 | Repository documents |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `AITransparencyAttribute`, `TransparencyObligationType` | `src/Encina.Compliance.AIAct/Attributes/`, `Model/` | Vocabulary; extended with `Exemption` and `PublishedAIText` |
| `AIActCompliancePipelineBehavior` | `src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs:160`, `:274` | Hook call sites |
| `AIActErrors.TransparencyRequired` | `src/Encina.Compliance.AIAct/AIActErrors.cs:151` | Returned when an unmet duty blocks |
| `IAISystemRegistry`, `AISystemRegistration` | `src/Encina.Compliance.AIAct/` | System lookup; new `PlacedOnMarketAtUtc` |
| Notifications and `IEncina.Publish` | `src/Encina.Compliance.AIAct/Notifications/`, `InMemoryAISystemRegistry.cs:109` | Audit evidence |
| `AIActOptions`, `AIActOptionsValidator` | `src/Encina.Compliance.AIAct/` | `TransparencyEnforcement` option |
| `EventIdRanges.ComplianceAIAct`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs:389` | Range 9576-9587 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.AIAct` (#415, #847, #842) | 9500-9547 | In use or reserved |
| `Encina.Compliance.AIAct.BiasDetection` (#838) | 9548-9561 | Reserved by the #838 plan |
| `Encina.Compliance.AIAct.TechnicalDocumentation` (#840) | 9562-9575 | Reserved by the #840 plan |
| **`Encina.Compliance.AIAct.Transparency` (#841)** | **9576-9587** | **12 ids inside `ComplianceAIAct`** |
| `Encina.Compliance.AIAct.GPAI` (#843) | 9588-9599 | Reserved by the #843 plan |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core hook and attribute changes (Phase 1) | 10 | hook, context, exemption, attribute, registration, options, validator, pipeline, PublicAPI, tests counted below |
| Package project files | 5 | csproj, two PublicAPI files, `Encina.slnx`, manifest |
| Models and errors | 8 | requirement, party, disclosure, `ContentKind`, `MarkingRequest`, `MarkedContent`, errors, options |
| Abstractions | 2 | `ITransparencyEnforcer`, `IContentMarker` |
| Catalog, rules, enforcer, context, hook, marker | 8 | catalog, applicability rules, enforcer, context, hook, manifest, marker, validator |
| DI and notifications | 3 | `ServiceCollectionExtensions`, two notifications |
| Diagnostics | 2 | `TransparencyDiagnostics`, `TransparencyLogMessages` |
| Tests | ~18 | unit, guard, contract, property, DI, architecture map, three `.md` justifications |
| Documentation | 6 | changelog fragments, README, feature page, ADR, INVENTORY, ROADMAP |
| **Total** | **~62** | **Six pull requests of 6 to 15 files** |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #841 — EU AI Act Art. 50 (and Art. 13 by reference) transparency — as the new package
Encina.Compliance.AIAct.Transparency plus a fail-closed hook in Encina.Compliance.AIAct (if the maintainer chose
placement option A, put the package code in a Transparency/ folder of the core and drop the hook interface).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0: no backward compatibility, no [Obsolete], best solution always.
- Railway Oriented Programming: Either<EncinaError, T>; compliance gates fail closed; the only opt-out is explicit and logged.
- Art. 50 splits duties: providers 50(1)-(2), deployers 50(3)-(4); information at the latest at first interaction (50(5)).
- Event-sourced module: no InMemory stores; disclosures are scoped records plus audit notifications (persistence is #847).

IMPLEMENTATION OVERVIEW:
Phase 1: core ITransparencyObligationHook, TransparencyHookContext, TransparencyExemption, PublishedAIText, attribute Exemption,
         PlacedOnMarketAtUtc, AIActOptions.TransparencyEnforcement, pipeline calls (determine before, verify after).
Phase 2: package skeleton, models (ContentKind, MarkingRequest, MarkedContent.Content), ITransparencyEnforcer, IContentMarker, errors.
Phase 3: Article50Catalog, applicability rules (grace to 2 Dec 2026 for systems placed before 2 Aug 2026), options, enforcer,
         TransparencyContext, TransparencyObligationHook.
Phase 4: ManifestContentMarker (sidecar JSON manifest with SHA-256), plug-in points for C2PA and watermark.
Phase 5: two audit notifications, AddEncinaAIActTransparency, DI tests in both registration orders.
Phase 6: ActivitySource/Meter and 12 log events 9576-9587; architecture-test map.
Phase 7: all test types and three .md justifications. Phase 8: docs-writer pages, changelog, ADR, INVENTORY, ROADMAP, PublicAPI.

KEY PATTERNS:
- TimeProvider only; unknown system or missing placement date never means "no obligation".
- EncinaError.Message and content never logged or tagged; error codes and ForLogging() only.
- TryAdd registrations; DI completeness test with ValidateOnBuild and ValidateScopes; CRAP <= 10 per changed method.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs
- src/Encina.Compliance.AIAct/Attributes/AITransparencyAttribute.cs
- src/Encina.Compliance.AIAct/AIActErrors.cs
- docs/plans/aiact-technical-documentation-implementation-plan-840.md
- docs/plans/aiact-record-keeping-implementation-plan-842.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ❌ N/A | Disclosures are per request and recipient and must not be reused across recipients |
| 2 | **OpenTelemetry** | ✅ Phase 6 | Spans `aiact.transparency.determine` and `.mark`; counters for requirements, disclosures and marked content |
| 3 | **Structured Logging** | ✅ Phase 6 | 12 `[LoggerMessage]` events, ids 9576-9587 inside `ComplianceAIAct` (ADR-021) |
| 4 | **Health Checks** | ❌ N/A | The enforcer is stateless and has no checkable dependency; a registered hook is verified by the DI test |
| 5 | **Validation** | ✅ Phases 3, 5 | Options validator; disclosure and marking inputs validated before use |
| 6 | **Resilience** | ❌ N/A | No external call in the default marker; a C2PA backend owns its own resilience |
| 7 | **Distributed Locks** | ❌ N/A | No shared mutable state |
| 8 | **Transactions** | ❌ N/A | The package persists nothing; the notification is the record |
| 9 | **Idempotency** | ❌ N/A | Disclosures are per request, identified by `DisclosureId`; marking is deterministic for the same content and clock |
| 10 | **Multi-Tenancy** | ⏭️ Defer ([#845](https://github.com/dlrivada/Encina/issues/845)) | Per-tenant disclosure rules are part of the tenant-scoped AI Act enforcement |
| 11 | **Module Isolation** | ⏭️ Defer ([#846](https://github.com/dlrivada/Encina/issues/846)) | `ModuleId`-scoped enforcement is delivered package-wide |
| 12 | **Audit Trail** | ✅ Phase 5 | Disclosure-recorded and content-marked notifications through `IEncina.Publish`; a publish failure fails the operation |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | State | Why |
|---|---|---|
| `Encina.Compliance.AIAct` core ([#415](https://github.com/dlrivada/Encina/issues/415)) | Closed, in `main` | Attribute, pipeline, registry, notifications |

No `needs-decision` issue blocks this plan; the issue's own Dilemma (placement) is recorded above.

### Recommended (Not Blocking)

- [#840](https://github.com/dlrivada/Encina/issues/840): carries the Art. 13 instructions-for-use content as Annex IV 1(h).
- [#847](https://github.com/dlrivada/Encina/issues/847): durable storage of disclosures.
- [#845](https://github.com/dlrivada/Encina/issues/845), [#846](https://github.com/dlrivada/Encina/issues/846): tenant and module scoping.
- [#842](https://github.com/dlrivada/Encina/issues/842): the record-keeping log can receive disclosure events through its recorder.

---

## Next Steps

1. The maintainer answers Design Choices 1, 2 and 3 when work on #841 starts; the orchestrator records the answers in a `## Maintainer Decisions` section after this one and runs `check-plan` without `-Draft`.
2. `adversarial-reviewer` tries to break the Settled and Trivial choices (4, 5, 6) before the first worker brief, and `decision-extractor` lists any implicit choice.
3. Brief the six pull requests in order, each with its re-measured `Size:` line, per-file coverage targets and the local CRAP command.
4. Update #841's acceptance criteria to the decided scope (roles, exemptions, byte-based marking, hook and `PlacedOnMarketAtUtc`).
