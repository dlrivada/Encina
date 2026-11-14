# Implementation Plan: AI model training as a well-known legitimate interest for `Encina.Compliance.LawfulBasis` (Digital Omnibus, COM(2025) 837)

> **Issue**: [#815](https://github.com/dlrivada/Encina/issues/815)
> **Type**: Feature
> **Milestone**: v0.15.0 — EU Compliance: NIS2 & Digital Omnibus
> **Complexity**: Medium (6 phases, Marten module, a persisted-shape change, ~20 production files)
> **Estimated Scope**: ~900-1,300 lines of production code + ~1,800-2,400 lines of tests

---

## Summary

Add a **registry of legitimate-interest definitions** to `Encina.Compliance.LawfulBasis`, with three well-known AI interests (`ai-model-training`, `ai-bias-detection`, `ai-performance-testing`) whose safeguard requirements apply only when an option named after the draft article is enabled. The Digital Omnibus proposal COM(2025) 837 would treat the development and operation of AI as a possible legitimate interest under GDPR Art. 6(1)(f) subject to safeguards (SPEC-002 §3.5: "GDPR Art. 88c"). **This is draft law.** Under SPEC-002 REQ-024, DEC-012 and AC-024 the behaviour is off by default and named by its draft article, current law stays the default, and the persisted shape added by this issue (an optional interest id on the legitimate interest assessment, LIA) is written and read back unchanged while the option is off: the shape exists, the behaviour does not act.

### Standards covered

- GDPR Art. 6(1)(f) and Art. 5(2): legitimate interest and accountability. Current-law default: every legitimate-interest processing needs an approved LIA (`LawfulBasisValidationPipelineBehavior`, already present).
- GDPR Art. 21: the right to object. The metadata flag `SubjectCanObjectUnconditionally` records the draft's intent; its consumer is deferred (Design Choice 8).
- GDPR Art. 9: special categories. The flag `AllowsIncidentalSpecialCategoryData` records the draft's intent; its consumer is deferred (Design Choice 8).
- Digital Omnibus COM(2025) 837 (draft), "GDPR Art. 88c" per SPEC-002 §3.5: AI development under legitimate interest. **Unverified** against the published text: the article number, the exact safeguards (data minimisation, unconditional objection, handling of incidental special-category data) and whether the "well-known" status removes or only guides the balancing test are taken from the issue body and SPEC-002, not from the Official Journal. The procedure 2025/0360(COD) is still in committee (SPEC-002 §3.5). This plan never treats the proposal as adopted and never lets a well-known interest replace an approved LIA.

### What already exists

- `src/Encina.Compliance.LawfulBasis/` is event-sourced on Marten (ADR-019, SPEC-002 DEC-008 (a)). `Aggregates/LIAAggregate.cs:27` holds the LIA with a free-text `Purpose` (line 42), `DataMinimisationNotes`, `ImpactAssessment`, `Safeguards` (line 98) and `DPOInvolvement` (line 110). `Events/LIAEvents.cs` has `LIACreated`, `LIAApproved`, `LIARejected`, `LIAReviewScheduled`. `ReadModels/LIAReadModel.cs` and `ReadModels/LIAProjection.cs` mirror them.
- `Services/DefaultLawfulBasisService.cs:240` (`CreateLIAAsync`) and `:301` (`ApproveLIAAsync`) are the points where an LIA is recorded and decided. `:632` `HasApprovedLIAAsync` answers only "is there an approved LIA with this reference".
- `Pipeline/LawfulBasisValidationPipelineBehavior.cs:152` calls `ValidateLIAAsync` (lines 293-333) for `LegitimateInterests`: it requires `LIAReference` on `[LawfulBasis]` and an approved LIA, then applies the enforcement mode. It knows nothing about interests.
- `[LawfulBasis]` (`src/Encina.Compliance.GDPR/Attributes/LawfulBasisAttribute.cs`) carries `Purpose`, `LIAReference`, `LegalReference`, `ContractReference`. There is no interest id.
- `LawfulBasisOptions.cs` has `DefaultBases` and the fluent `DefaultBasis<TRequest>`, `ScanAssembly`. `LawfulBasisOptionsValidator.cs` checks the enforcement mode and the default bases only.
- The extensibility precedent is `src/Encina.Compliance.DPIA/Model/HighRiskTriggers.cs`: constants "defined as strings rather than an enum to support extensibility".
- The off-by-default precedent is `src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs:96` (`EnableOmnibusArticle12RefusalGrounds`, #814).
- Search of `src/` confirms that `WellKnownLegitimateInterests`, `DefineInterest` and any interest registry do not exist.
- EventIds: `ComplianceLawfulBasis` is 8350-8399 with ids up to 8386 in use (13 free).

### Corrections to the issue body

- #415 (AI Act) is closed and `src/Encina.Compliance.AIAct/` exists; the issue's "future AI compliance" is out of date. This plan does not depend on it.
- The issue's Test Matrix lists Integration as conditional; because Design Choice 7 changes the LIA event, the Marten integration test is required, not optional.
- The sample sets `AllowsIncidentalSpecialCategoryData` and `SubjectCanObjectUnconditionally` as if they acted. Nothing in `LawfulBasis` can act on them (no Art. 9 check, no objection handling in this package); this plan records them as metadata and defers the consumers (Design Choice 8). Without that, they would be properties with no purpose (AGENTS.md §3).

### Affected packages

- `Encina.Compliance.LawfulBasis`: constants, definition model, registry, options, validator, LIA events, aggregate, read model, projection, service, errors, DI, health check, diagnostics.
- `Encina` (core): none; EventIds are inside the existing range.
- Tests: `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.PropertyTests`, `Encina.IntegrationTests` (Marten on PostgreSQL through Testcontainers); `.md` justifications under `Encina.LoadTests` and `Encina.BenchmarkTests`.

### Provider category

None of the 10 database providers. The LIA aggregate is Marten on PostgreSQL only and event-sourced compliance modules have no InMemory store (AGENTS.md §3; ADR-019). Unit tests mock `IAggregateRepository`; the integration test uses Marten on PostgreSQL.

---

## Design Choices

<details>
<summary><strong>1. Interest identifiers — string constants</strong></summary>

**Class:** Trivial
**Settled by:** precedent `src/Encina.Compliance.DPIA/Model/HighRiskTriggers.cs` (string constants for extensibility without library changes); AGENTS.md section 4 (naming)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `public static class WellKnownLegitimateInterests` with `const string` values** | Applications add their own ids without a library release; same pattern as `HighRiskTriggers` | No compile-time exhaustiveness |
| **B) An enum** | Compile-time checks | Closed set; an unknown interest needs a library change |

### Chosen Option: **A — String constants**

### Rationale

- The issue lists three ids and expects applications to define more. A closed enum fails that.

</details>

<details>
<summary><strong>2. Where definitions are registered — fluent method on <code>LawfulBasisOptions</code></strong></summary>

**Class:** Trivial
**Settled by:** precedent `LawfulBasisOptions.DefaultBasis<TRequest>` and `ScanAssembly` (fluent configuration on the options class); AGENTS.md section 3 (options classes)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `options.DefineInterest(id, Action<LegitimateInterestDefinitionOptions>)` storing into a dictionary on `LawfulBasisOptions`; an `ILegitimateInterestRegistry` singleton reads it** | Matches the existing fluent style; one configuration point | The registry is read-only after start |
| **B) A separate `services.AddLegitimateInterest(...)` call** | Separate from options | A second configuration surface for the same package |

### Chosen Option: **A — `DefineInterest` on the options, read through `ILegitimateInterestRegistry`**

### Rationale

- The registry type gives consumers (and the deferred DSR and Art. 9 integrations) a stable read contract without exposing the options class.

</details>

<details>
<summary><strong>3. Off by default — an option named after the draft article, with built-in definitions behind it</strong></summary>

**Class:** Settled
**Settled by:** SPEC-002 REQ-024, DEC-012 and AC-024; precedent `DataSubjectRightsOptions.EnableOmnibusArticle12RefusalGrounds` (#814); AGENTS.md section 3 (fail closed)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `LawfulBasisOptions.EnableOmnibusArticle88cAiTrainingInterests` (default `false`). While `false` the three AI definitions are not registered and no AI-specific check runs. While `true` the registry contains the three built-in definitions with every requirement `true`, and an application cannot weaken them** | AC-024 is directly testable (default registry has no AI interest); the safeguards are not left to each application | The flag name embeds the article number from SPEC-002, unverified |
| **B) The applications define the AI interests themselves with `DefineInterest` and no flag** | No flag | The draft safeguards depend on each application's goodwill; no off switch to test; against REQ-024 |

### Chosen Option: **A — Flag plus built-in definitions**

### Rationale

- Custom interests (for example `fraud-prevention`) are defined with `DefineInterest` regardless of the flag; only the three ids of `WellKnownLegitimateInterests` are gated.
- The validator fails the start-up when the flag is `false` and an application defines one of the three ids, and when the flag is `true` and a definition of one of them sets a requirement to `false` (Design Choice 6).
- The XML documentation names the draft article as "GDPR Art. 88c (COM(2025) 837, draft, per SPEC-002 §3.5, unverified against the published text)". If the adopted numbering differs, renaming the flag is a pre-1.0 change.

</details>

<details>
<summary><strong>4. Persisted shape — an optional interest id on the LIA, stored whatever the flag</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Store nothing; the free-text `Purpose` is the link** | No persisted-shape change | The link is string matching on free text; a typo silently skips the check |
| **B) Add an optional `InterestId` (`string?`) to `LIACreated`, `LIAAggregate`, `LIAReadModel`, `LIAProjection` and a parameter on `ILawfulBasisService.CreateLIAAsync`, written and read back whatever the flag** | Explicit link; satisfies AC-024's "shape exists, behaviour does not act"; DEC-012 names #815 as a shape change to build now | Marten event schema grows by one nullable field; the service signature changes (allowed pre-1.0) |

### Chosen Option: **B — Optional `InterestId`, persisted whatever the flag** (recommended, pending the maintainer)

### Rationale

- I recommend B because of SPEC-002 DEC-012: the reason to build #815 before 1.0 is the persisted shape; shipping the flag without the field would defer exactly what DEC-012 wants done now. A wins only if the maintainer prefers no stored link and accepts string matching on `Purpose` (then Design Choice 5 becomes definition-time only).
- With the flag off, an LIA may still carry `InterestId = "ai-model-training"`; it is stored and returned unchanged and nothing checks it.

</details>

<details>
<summary><strong>5. Where the safeguards are enforced when the flag is on</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Start-up validation of definitions only** | Smallest change; no LIA flow change | An LIA for `ai-model-training` can omit its data-minimisation notes and impact assessment and still be approved |
| **B) A: plus the LIA service. `CreateLIAAsync` (and `ApproveLIAAsync`) return a `Left` with a new error code when the LIA's `InterestId` names a defined interest and a required safeguard is missing: `RequiresDataMinimization` needs non-blank `DataMinimisationNotes`; `RequiresRiskAssessment` needs non-blank `ImpactAssessment` and a non-empty `Safeguards`** | Enforced where the decision is recorded, once per LIA, not per request; fails closed | Couples the LIA flow to the registry; five-line rules over free text can only test presence, not quality |
| **C) B plus a check in `LawfulBasisValidationPipelineBehavior` on every request** | Catches an LIA approved before the flag was turned on | Extra read on a hot path; a second place for the same rule |

### Chosen Option: **B — Start-up validation and the LIA service** (recommended, pending the maintainer)

### Rationale

- I recommend B because an LIA is a record of a decision (Art. 5(2)); rejecting an incomplete one when it is written is the earliest and cheapest enforcement point. C wins for an application that turns the flag on with LIAs already approved; it can re-run `ScheduleLIAReviewAsync` instead. A wins only if the maintainer wants no change to LIA flows.
- The rules test presence of fields the LIA already has; they do not judge content. The unconditional-objection and special-category flags are not checked here (Design Choice 8).
- Fails closed (AGENTS.md §3) for an LIA that names an interest: with the flag on and an `InterestId` that names no defined interest, creation returns `lawfulbasis.interest_not_defined`.
- Limit: the check runs only when `InterestId` is non-null and the flag is on. An LIA for AI training created without an `InterestId` is an ordinary LIA and bypasses every safeguard check, because the purpose is free text and cannot be matched reliably. The documentation states this plainly; no claim of "fails closed" extends to an LIA that does not name its interest.
- Deviation from the issue: Alternatives 6 says the check covers "Safeguards, DPOInvolvement". The plan checks `DataMinimisationNotes`, `ImpactAssessment` and `Safeguards` (the fields the three draft safeguards map to) and does not check `DPOInvolvement`, which no draft safeguard requires; the maintainer can add it with this decision.

</details>

<details>
<summary><strong>6. Validation of definitions at start-up</strong></summary>

**Class:** Settled
**Settled by:** AGENTS.md section 3 (options validated at start; compliance gates fail closed); precedent `LawfulBasisOptionsValidator`

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `LawfulBasisOptionsValidator` rejects: a blank or duplicate id; a definition of a gated AI id while the flag is off; a gated AI definition that weakens a requirement while the flag is on** | Misconfiguration fails at start with a named property | One more section in the validator |
| **B) Ignore silently** | No code | A weakened statutory safeguard goes unnoticed |

### Chosen Option: **A — Fail at start**

### Rationale

- The three rules are independent and testable alone. The validator keeps its complexity down by delegating to `ValidateInterests(options, failures)`.

</details>

<details>
<summary><strong>7. Link from a request to its interest — through the LIA only</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A request names its LIA through the existing `LIAReference`; the LIA names the interest (`InterestId`)** | No change to `Encina.Compliance.GDPR`; one hop already exists | The attribute does not show the interest at a glance; the pipeline must load the LIA to know the interest |
| **B) New `InterestId` property on `[LawfulBasis]`** | Visible on the request; the pipeline can check the interest without loading the LIA (Design Choice 5 option C) | Touches another package (`Encina.Compliance.GDPR`) and its PublicAPI; two places to keep consistent |

### Chosen Option: **A — Through the LIA** (recommended, pending the maintainer)

### Rationale

- We recommend A because the LIA is already the record that carries the justification and the link needs one place; B wins if the maintainer wants the pipeline to check the interest on every request without loading the LIA.
- The pipeline behavior is unchanged except that, if the maintainer picks C in Design Choice 5, it reads the LIA it already loads.

</details>

<details>
<summary><strong>8. Metadata whose consumers live in other packages — recorded, consumers deferred</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Record `SubjectCanObjectUnconditionally` and `AllowsIncidentalSpecialCategoryData` on the definition, expose them through `ILegitimateInterestRegistry`, and defer their consumers to an issue** | Stays inside one package; the shape is built now as SPEC-002 DEC-012 asks; the draft's safeguards are fully represented | Two properties without a consumer today (dead data until the consumers exist); the XML docs say so |
| **B) Implement the objection and Art. 9 consumers now** | Behaviour complete | Spans `DataSubjectRights` and another package; exceeds the size limits and mixes draft behaviours into current-law paths |
| **C) Omit the two properties until a consumer exists** | No unused property, which is what "every line serves a current purpose" (AGENTS.md section 3) asks | The draft's safeguards are partly unrepresented in the definition until the consumers are built |

### Chosen Option: **A — Record and defer** (recommended, pending the maintainer)

### Rationale

- We recommend A because DEC-012 names #815 as a shape to build before 1.0, so the definition carries all three draft safeguards; C wins when the maintainer holds that a property without a consumer is dead API and prefers to add the two properties together with their consumers (the definition type is pre-1.0 and can change).
- The issue file is `artifacts/issues/plan-815-interest-metadata-consumers.md` (see Prerequisites). The shape is what DEC-012 asks to build now; the consumers can follow when the text is adopted.

</details>

---

## Implementation Phases

### Phase 1: Definition Model, Constants and Registry

> **Goal**: The types, with no behaviour change yet.

<details>
<summary><strong>Tasks</strong></summary>

All paths are in `src/Encina.Compliance.LawfulBasis/`, namespace `Encina.Compliance.LawfulBasis` unless stated.

1. `WellKnownLegitimateInterests.cs` (new, public static): `AIModelTraining = "ai-model-training"`, `AIBiasDetection = "ai-bias-detection"`, `AIPerformanceTesting = "ai-performance-testing"`, and `internal static bool IsGatedByOmnibus(string id)`. XML docs name the draft article, "off by default".
2. `Model/LegitimateInterestDefinition.cs` (new, public sealed record): `Id`, `DisplayName`, `RequiresDataMinimization`, `RequiresRiskAssessment`, `SubjectCanObjectUnconditionally`, `AllowsIncidentalSpecialCategoryData`, `DraftArticle` (`string?`, set for the built-ins).
3. `Model/LegitimateInterestDefinitionOptions.cs` (new, public sealed class): the mutable builder the lambda of `DefineInterest` configures; `internal LegitimateInterestDefinition Build(string id)`.
4. `Abstractions/ILegitimateInterestRegistry.cs` (new, public): `bool TryGet(string id, out LegitimateInterestDefinition definition)`, `IReadOnlyCollection<LegitimateInterestDefinition> GetAll()`.
5. `Services/DefaultLegitimateInterestRegistry.cs` (new, public sealed): built from `IOptions<LawfulBasisOptions>` and `ILogger<DefaultLegitimateInterestRegistry>`; adds the three built-ins when `EnableOmnibusArticle88cAiTrainingInterests` is `true`; case-sensitive ids.
6. `LawfulBasisOptions.cs` (modify): `public bool EnableOmnibusArticle88cAiTrainingInterests { get; set; }` (default `false`), `internal Dictionary<string, LegitimateInterestDefinition> Interests`, `public LawfulBasisOptions DefineInterest(string id, Action<LegitimateInterestDefinitionOptions> configure)` (`ArgumentException.ThrowIfNullOrWhiteSpace`, `ArgumentNullException.ThrowIfNull`).
7. `PublicAPI.Unshipped.txt`: lines for every new public symbol (written by `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Issue #815, plan docs/plans/ai-training-legitimate-interest-implementation-plan-815.md. Package
src/Encina.Compliance.LawfulBasis (event-sourced on Marten, ADR-019). The Digital Omnibus COM(2025) 837 is draft
law: the AI interests are off by default (SPEC-002 REQ-024, AC-024). Precedents: DPIA HighRiskTriggers (string
constants), DataSubjectRightsOptions.EnableOmnibusArticle12RefusalGrounds (off by default, named after the draft).

TASK:
Create WellKnownLegitimateInterests, LegitimateInterestDefinition, LegitimateInterestDefinitionOptions,
ILegitimateInterestRegistry and DefaultLegitimateInterestRegistry, and extend LawfulBasisOptions with
EnableOmnibusArticle88cAiTrainingInterests (default false) and DefineInterest. Register nothing yet.

KEY RULES:
- XML docs on every public member; name the draft article and "off by default"; say "proposed", not "will".
- ArgumentNullException / ArgumentException guards on public methods.
- No [Obsolete], no aliases. Add PublicAPI.Unshipped.txt lines.

REFERENCE FILES:
- src/Encina.Compliance.LawfulBasis/LawfulBasisOptions.cs
- src/Encina.Compliance.DPIA/Model/HighRiskTriggers.cs
- src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs
```

</details>

### Phase 2: Validation, Configuration and DI

> **Goal**: Start-up fails closed; the registry is registered.

<details>
<summary><strong>Tasks</strong></summary>

1. `LawfulBasisOptionsValidator.cs` (modify): add `ValidateInterests(options, failures)` with the three rules of Design Choice 6 and update the class XML docs.
2. `ServiceCollectionExtensions.cs` (modify): `services.TryAddSingleton<ILegitimateInterestRegistry, DefaultLegitimateInterestRegistry>();` (next to the other `TryAdd` lines); update the XML docs list of registrations.
3. `Diagnostics/LawfulBasisLogMessages.cs` (modify): EventId 8387 (Omnibus AI interests active) and 8388 (interest registry built, count), both logged once by the constructor of `DefaultLegitimateInterestRegistry`, a singleton built on first resolution, which therefore takes `ILogger<DefaultLegitimateInterestRegistry>`; 8387 is logged only when the flag is on and carries no interest content.
4. A DI test (Phase 6) builds the provider with `ValidateOnBuild` and `ValidateScopes` and resolves `ILegitimateInterestRegistry` with the flag off and on.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Plan #815, Phase 2. LawfulBasisOptionsValidator currently checks only the enforcement mode and DefaultBases.

TASK:
Add the three interest-validation rules (blank or duplicate id; gated AI id defined while the flag is off; gated
AI definition weakening a requirement while the flag is on) and register ILegitimateInterestRegistry with TryAdd.

KEY RULES:
- Fail closed at start-up with property-named messages; no option values that could be sensitive.
- Keep CRAP <= 10: put the rules in a helper, one rule per small method.
- Registration completeness: provider must build with ValidateOnBuild and ValidateScopes (AGENTS.md section 3).

REFERENCE FILES:
- src/Encina.Compliance.LawfulBasis/LawfulBasisOptionsValidator.cs
- src/Encina.Compliance.LawfulBasis/ServiceCollectionExtensions.cs
```

</details>

### Phase 3: Persisted Shape — `InterestId` on the LIA

> **Goal**: The optional field, stored whatever the flag.

<details>
<summary><strong>Tasks</strong></summary>

1. `Events/LIAEvents.cs` (modify): append `string? InterestId` as the last positional parameter of `LIACreated` (after `ModuleId`), so no existing positional value is reordered.
2. `Aggregates/LIAAggregate.cs` (modify): `public string? InterestId { get; private set; }`; `Create(...)` gains `string? interestId = null`; the `Apply(LIACreated)` handler sets it.
3. `ReadModels/LIAReadModel.cs` and `ReadModels/LIAProjection.cs` (modify): `InterestId` copied on `LIACreated`.
4. `Abstractions/ILawfulBasisService.cs` and `Services/DefaultLawfulBasisService.cs` (modify): `CreateLIAAsync(..., string? interestId = null, ...)` placed before `CancellationToken`.
5. Existing callers of `CreateLIAAsync` and `LIAAggregate.Create` in `src/` and tests are updated (search before editing).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Plan #815, Phase 3. SPEC-002 DEC-012 names #815 as a persisted-shape change to build before 1.0. AC-024 requires
the new state to be written and read back unchanged while the Omnibus option is off. Events are INotification
records published by Encina.Marten's EventPublishingPipelineBehavior.

TASK:
Add the optional InterestId to LIACreated, LIAAggregate, LIAReadModel, LIAProjection and CreateLIAAsync. Do not
reorder existing positional parameters.

KEY RULES:
- Nullable, no behaviour: with the flag off nothing reads InterestId.
- Update all callers found by a search of src/ and tests/.
- PublicAPI.Unshipped.txt lines for every changed signature (RS0016/RS0017).

REFERENCE FILES:
- src/Encina.Compliance.LawfulBasis/Events/LIAEvents.cs
- src/Encina.Compliance.LawfulBasis/Aggregates/LIAAggregate.cs
- src/Encina.Compliance.LawfulBasis/Services/DefaultLawfulBasisService.cs
```

</details>

### Phase 4: Enforcement in the LIA Service

> **Goal**: With the flag on, an incomplete LIA for a defined interest is rejected (Design Choice 5, option B).

<details>
<summary><strong>Tasks</strong></summary>

1. `Errors/LawfulBasisErrors.cs` (modify): codes `lawfulbasis.interest_not_defined` and `lawfulbasis.interest_safeguards_missing`, with factories following the existing pattern; the messages never contain LIA content.
2. `Services/DefaultLawfulBasisService.cs` (modify): inject `ILegitimateInterestRegistry`; in `CreateLIAAsync`, when `interestId` is non-null **and the flag is on**, resolve the definition (`interest_not_defined` if absent) and check the presence rules (`RequiresDataMinimization` needs non-blank `DataMinimisationNotes`; `RequiresRiskAssessment` needs non-blank `ImpactAssessment` and non-empty `Safeguards`). Put the checks in `private static Either<EncinaError, Unit> CheckInterestSafeguards(...)`. With the flag off the method skips all of this.
3. `ApproveLIAAsync` re-runs the same check on the loaded LIA when the flag is on.
4. `LawfulBasisDiagnostics.cs` (modify): counter `lawful_basis.legitimate_interest.rejections.total`, tags `interest` and `reason` (the interest id is configuration, not personal data).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Plan #815, Phase 4 (the plan recommends Design Choice 5 option B; follow the maintainer's recorded answer if it
differs). DefaultLawfulBasisService creates and approves LIAs on the Marten LIAAggregate.

TASK:
Implement the interest check in CreateLIAAsync and ApproveLIAAsync, behind the Omnibus flag, returning Left with
the two new error codes. Add the counter.

KEY RULES:
- Flag off: no registry lookup, no rejection, same results as today (AC-024 test).
- Errors are returned as Either; no exceptions for business logic (ADR-001, ADR-006).
- Logs carry error codes only; never LIA text or EncinaError.Message.
- CRAP <= 10: extract CheckInterestSafeguards.

REFERENCE FILES:
- src/Encina.Compliance.LawfulBasis/Services/DefaultLawfulBasisService.cs
- src/Encina.Compliance.LawfulBasis/Errors/LawfulBasisErrors.cs
- src/Encina.Compliance.LawfulBasis/Diagnostics/LawfulBasisDiagnostics.cs
```

</details>

### Phase 5: Observability and Health

> **Goal**: EventIds in the existing range, counter, health data.

<details>
<summary><strong>Tasks</strong></summary>

1. `Diagnostics/LawfulBasisLogMessages.cs` (modify): four `[LoggerMessage]` entries with EventIds **8387-8390** inside `ComplianceLawfulBasis` (8350-8399, highest used 8386), packed in the order the pull requests land: 8387 Omnibus AI interests active and 8388 interest registry built (count), both added with Phase 2 by the registry constructor; 8389 LIA rejected, interest not defined (error code) and 8390 LIA rejected, safeguards missing (error code, requirement name), added with Phase 4 by the service. An invalid definition at start-up needs no message: the validator's failures surface as an `OptionsValidationException` naming the property. No new range; `EventIdRanges.cs` and `EncinaEventIdAllocationTests` do not change.
2. `Health/LawfulBasisHealthCheck.cs` (modify): add `data["omnibusAiInterestsEnabled"]` and `data["definedInterestCount"]`; no interest content beyond counts.
3. XML docs: `/// Event IDs: 8387-8390 (see EventIdRanges.ComplianceLawfulBasis)`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Plan #815, Phase 5. ADR-021: EventIds live in registered ranges. LawfulBasis owns 8350-8399, ids up to 8386 used.

TASK:
Add EventIds 8387-8390, the health data entries, and run the EventId allocation architecture test.

KEY RULES:
- Packed sequentially, no gaps, no id outside 8350-8399.
- Never log EncinaError.Message, exception messages or LIA text; use error codes and ex.ForLogging().

REFERENCE FILES:
- src/Encina.Compliance.LawfulBasis/Diagnostics/LawfulBasisLogMessages.cs
- src/Encina.Compliance.LawfulBasis/Health/LawfulBasisHealthCheck.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 6: Testing, Documentation and Finalization

> **Goal**: Every flag at its target; AC-024 proven; docs name the draft article.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/Compliance/LawfulBasis/`): registry (flag off: no AI id; flag on: three built-ins with all requirements `true`; custom definitions); options defaults (flag `false`); validator rules (each failure and the valid combinations); service enforcement (flag off: an LIA with `InterestId = ai-model-training` and missing safeguards is created exactly as today, **the AC-024 behaviour test**; flag on: rejected with each error code; complete LIA accepted); DI test with `ValidateOnBuild` and `ValidateScopes`, flag off and on.
2. **Guard**: `DefineInterest`, `LegitimateInterestDefinitionOptions`, the registry and service constructors.
3. **Contract**: `ILegitimateInterestRegistry` contract base run against the default implementation.
4. **Property** (`Encina.Testing.FsCheck`): for any set of valid definitions the registry returns exactly the definitions registered; validator accepts iff its three rules hold.
5. **Integration** (`tests/Encina.IntegrationTests/`, Marten on PostgreSQL, the shared `[Collection]` fixture): create an LIA with `InterestId` while the flag is off, load it, assert `InterestId` and every other field round-trip unchanged (**the AC-024 shape test**); same with the flag on.
6. **Load / Benchmark**: `.md` justifications (configuration and a one-time check per LIA).
7. **Per-flag targets** (AGENTS.md §9). Baselines are not measured by this plan; the worker measures them and sets each target at or above the baseline (targets never go down). Planned targets for new files: `WellKnownLegitimateInterests.cs` unit 100; `Model/LegitimateInterestDefinition.cs` unit 100, guard 100; `DefaultLegitimateInterestRegistry.cs` unit 95, guard 100, contract 100, property 100; `LawfulBasisOptions.cs` unit 100; `LawfulBasisOptionsValidator.cs` unit 95, guard 100, property 90; `DefaultLawfulBasisService.cs` unit at least the baseline plus every new branch, integration for the LIA round trip.
8. **Documentation**: XML docs naming the draft article and "off by default"; `changelog.d/815-ai-training-legitimate-interest.added.md` (`mechanical-fixer`); package README; the LawfulBasis specification page row for GDPR Art. 6(1)(f) / draft Art. 88c as Partial, off by default (SPEC-002 AC-024; #1208 when it exists); a configuration section in the feature page (`docs-writer`); no ADR unless the maintainer's answers change the architecture.
9. Build with zero warnings; `dotnet test` on the five test projects; the per-flag measurement and `--check-justifications` commands of AGENTS.md §9.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Plan #815, Phase 6. Tests must execute real package code (AGENTS.md section 9). Event-sourced compliance modules
have no InMemory stores: unit tests mock IAggregateRepository with NSubstitute; the integration test uses Marten
on PostgreSQL through Testcontainers and the shared [Collection] fixture, ClearAllDataAsync in InitializeAsync.
Assertions use Shouldly through Encina.Testing.Shouldly; property tests use Encina.Testing.FsCheck.

TASK:
Write the tests of the Tasks block including the two AC-024 tests (default behaviour follows current law; the
InterestId shape round-trips with the option off), the DI test, the .md justifications, the docs by the proper
specialists, and run the build, tests and coverage measurement.

KEY RULES:
- Documentation says "proposed" and "draft", names the draft article as given in SPEC-002 section 3.5 and marks
  it unverified; it never types coverage figures by hand (covref markers).
- Public text names no private client application; use "the reference application" if needed.
- A test never swallows an exception thrown by the code it drives; no Thread.Sleep.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Compliance.LawfulBasis.json
- docs/en/guides/TESTING.md
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (section 3.5, REQ-024, AC-024)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Article / Section | Relevance |
|----------|-------------------|-----------|
| GDPR (Reg. (EU) 2016/679) | Art. 6(1)(f) | Legitimate interest; current-law default needs an approved LIA |
| GDPR | Art. 5(2) | Accountability; an LIA is a record of a decision |
| GDPR | Art. 9 | Special categories; the metadata flag for incidental data, consumer deferred |
| GDPR | Art. 21 | Right to object; the unconditional-objection flag, consumer deferred |
| Digital Omnibus COM(2025) 837 (draft) | "GDPR Art. 88c" per SPEC-002 §3.5 | AI development under legitimate interest; unverified against the published text; off by default |
| SPEC-002 | REQ-024, DEC-012, AC-024 | Draft behaviour off by default; test that the default follows current law; test that new persisted state round-trips while off |
| EDPB Opinion 28/2024 | AI models and personal data | Background for the three-part legitimate-interest test; cited only as context |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `LIAAggregate`, `LIAEvents`, `LIAReadModel`, `LIAProjection` | `src/Encina.Compliance.LawfulBasis/` (Aggregates, Events, ReadModels) | Optional `InterestId` |
| `DefaultLawfulBasisService` | `src/Encina.Compliance.LawfulBasis/Services/DefaultLawfulBasisService.cs` | `CreateLIAAsync` (line 240), `ApproveLIAAsync` (line 301) |
| `LawfulBasisValidationPipelineBehavior` | `src/Encina.Compliance.LawfulBasis/Pipeline/` | Unchanged; reads the LIA only if Design Choice 5 option C is chosen |
| `LawfulBasisOptions`, `LawfulBasisOptionsValidator` | `src/Encina.Compliance.LawfulBasis/` | Flag, `DefineInterest`, validation |
| `HighRiskTriggers` | `src/Encina.Compliance.DPIA/Model/HighRiskTriggers.cs` | String-constant precedent |
| `DataSubjectRightsOptions.EnableOmnibusArticle12RefusalGrounds` | `src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs:96` | Off-by-default naming precedent |
| `LawfulBasisLogMessages`, `LawfulBasisDiagnostics`, `LawfulBasisHealthCheck` | `src/Encina.Compliance.LawfulBasis/Diagnostics/`, `Health/` | EventIds 8387-8390, counter, health data |
| `LawfulBasisErrors` | `src/Encina.Compliance.LawfulBasis/Errors/LawfulBasisErrors.cs` | Two new error codes |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.LawfulBasis` | 8387-8390 (inside the registered `ComplianceLawfulBasis` 8350-8399) | Highest id in use is 8386; 13 free; no new range, no change to `EventIdRanges.cs` |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| New production files | 5 | constants, definition, definition options, registry interface and default |
| Modified production files | 13 | options, validator, DI, LIA event, aggregate, read model, projection, service interface and default, errors, log messages, diagnostics, health check |
| Tests | ~14 | unit 6, guard 2, contract 1, property 1, integration 1, justifications 2, DI test counted in unit |
| Documentation and records | 5 | README, specification row, feature page, changelog fragment, PublicAPI |

Total about 37 files, above the 15-file limit of one worker PR (AGENTS.md §11). Split into four PRs on #815; counts include tests and records (`PublicAPI.Unshipped.txt`, coverage manifest), each PR at most 15 files and 1,500 lines:

| PR | Phases | Files | Lines (est.) | Content |
|----|--------|------:|-------------:|---------|
| PR A | 1 and 2 | 14 | ~900 | 5 new production files; `LawfulBasisOptions`, validator, `ServiceCollectionExtensions`, `LawfulBasisLogMessages` (8387-8388); `PublicAPI.Unshipped.txt`, manifest; 3 unit test files (registry, options and validator, DI) |
| PR B | 3 | 14 | ~900 | `LIAEvents`, `LIAAggregate`, `LIAReadModel`, `LIAProjection`, `ILawfulBasisService`, `DefaultLawfulBasisService`; `PublicAPI.Unshipped.txt`, manifest; aggregate/projection unit test, Marten round-trip integration test; guard, contract and property tests for the PR A types (3); integration, load, benchmark justifications (3) |
| PR C | 4 | 9 | ~600 | `LawfulBasisErrors`, `DefaultLawfulBasisService`, `LawfulBasisDiagnostics`, `LawfulBasisLogMessages` (8389-8390); `PublicAPI.Unshipped.txt`, manifest; service enforcement unit test, guard test; two integration cases in the PR B test file are not counted (same file) |
| PR D | 5 and 6 (docs) | 8 | ~350 | `LawfulBasisHealthCheck`, manifest; health check unit test; README, specification row, feature page, changelog fragment, `PublicAPI.Unshipped.txt` if needed |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete implementation prompt (all phases)</strong></summary>

```text
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 / C# 14 library (nullable on, ROP with Either<EncinaError, T>, TimeProvider
injection, [LoggerMessage] logging with EventIds in registered ranges, TryAdd DI). AGENTS.md binds every change.
Encina.Compliance.LawfulBasis is event-sourced on Marten (ADR-019); it has no InMemory store. Issue #815 adds a
registry of legitimate-interest definitions with three AI interests for the Digital Omnibus proposal COM(2025) 837,
which is draft law and therefore off by default (SPEC-002 REQ-024, DEC-012, AC-024).
Plan: docs/plans/ai-training-legitimate-interest-implementation-plan-815.md (follow the Design Choices as decided
in its Maintainer Decisions section, once it exists).

IMPLEMENTATION OVERVIEW:
1. WellKnownLegitimateInterests, LegitimateInterestDefinition(+Options), ILegitimateInterestRegistry and default;
   LawfulBasisOptions.DefineInterest and EnableOmnibusArticle88cAiTrainingInterests (default false).
2. Validator rules (blank or duplicate id; gated id while flag off; weakened built-in while flag on); TryAdd registry.
3. Optional InterestId on LIACreated, LIAAggregate, LIAReadModel, LIAProjection and CreateLIAAsync, stored whatever
   the flag.
4. With the flag on, CreateLIAAsync and ApproveLIAAsync reject an incomplete LIA for a defined interest
   (lawfulbasis.interest_not_defined, lawfulbasis.interest_safeguards_missing).
5. Observability: EventIds 8387-8390 inside 8350-8399; counter; health data.
6. Tests on every flag including the two AC-024 tests; Marten round trip; .md justifications.
7. Docs by docs-writer, changelog fragment and PublicAPI by mechanical-fixer, zero warnings.

KEY PATTERNS:
- Draft law off by default and named by its draft article in XML docs; "proposed", never "will".
- Fail closed; Either returns; no exception message or LIA text in logs.
- A well-known interest never replaces an approved LIA.
- Pre-1.0: no [Obsolete], no aliases. CRAP <= 10 on changed methods. Per-flag coverage targets.

REFERENCE FILES:
- src/Encina.Compliance.LawfulBasis/Services/DefaultLawfulBasisService.cs
- src/Encina.Compliance.LawfulBasis/Aggregates/LIAAggregate.cs
- src/Encina.Compliance.LawfulBasis/LawfulBasisOptions.cs
- src/Encina.Compliance.DPIA/Model/HighRiskTriggers.cs
- src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs
- docs/specifications/SPEC-002-eu-regulatory-readiness.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Definitions are configuration held in memory; the existing LIA read-model cache keys are invalidated by the existing paths |
| 2 | OpenTelemetry | ✅ | Counter `lawful_basis.legitimate_interest.rejections.total` (Phase 4) |
| 3 | Structured Logging | ✅ | EventIds 8387-8390 in the registered range 8350-8399 (Phase 5) |
| 4 | Health Checks | ✅ | `LawfulBasisHealthCheck` reports the flag state and the number of defined interests (Phase 5) |
| 5 | Validation | ✅ | Start-up validation of definitions and LIA safeguard checks (Phases 2 and 4) |
| 6 | Resilience | ❌ | No external call |
| 7 | Distributed Locks | ❌ | The LIA stream uses Marten's optimistic concurrency; the registry is read-only |
| 8 | Transactions | ❌ | The LIA create is one aggregate save, as today; nothing new is atomic |
| 9 | Idempotency | ❌ | LIA creation semantics unchanged |
| 10 | Multi-Tenancy | ❌ | Not applicable: interest definitions are global configuration (the same law for every tenant of a deployment) and the LIA keeps its existing `TenantId`; nothing is integrated |
| 11 | Module Isolation | ❌ | Not applicable: definitions are global configuration and the LIA keeps its existing `ModuleId`; nothing is integrated |
| 12 | Audit Trail | ✅ | `LIACreated` with `InterestId` is the audit record (ADR-019); the rejection path is counted and logged by error code |

The deferred consumers (objection and special-category handling) are not cross-cutting functions; they are the follow-up drafted in `artifacts/issues/plan-815-interest-metadata-consumers.md`, which the orchestrator opens as an issue before this plan merges (the issue number then replaces the file path here and in Prerequisites).

---

## Prerequisites & Dependencies

### Must coordinate

| Item | State (2026-11-11) | Relationship |
|------|--------------------|--------------|
| #779 LawfulBasis Marten migration (plan `lawfulbasis-marten-migration-plan-779.md`) | Plan merged | The module is already on Marten; this plan edits the same aggregates and projection |
| #1208-style article-coverage specification for LawfulBasis | Open | Must list the draft Art. 88c row as Partial (draft, off) with REQ-024 wording |
| #814 DSAR refusal reasons | Closed | Naming and default-off precedent |
| #1186 SPEC-002 tracking | Open | AC-024: closes before `1.0.0-rc.1` |

### Advisable, not blocking

| Item | State | Relationship |
|------|-------|--------------|
| #415 Encina.Compliance.AIAct | Closed; module exists | An AI-system module can later cite `WellKnownLegitimateInterests` in its documentation; no code coupling |
| #817 `IOptionsMonitor` migration | Open | The registry reads options at start; hot reload of definitions is not required and is not planned |

### Missing prerequisites and findings, written as issue files

| File | Template | Finding |
|------|----------|---------|
| `artifacts/issues/plan-815-interest-metadata-consumers.md` | `[FEATURE]` | `SubjectCanObjectUnconditionally` and `AllowsIncidentalSpecialCategoryData` have no consumer: the Art. 21 objection path in `DataSubjectRights` and the special-category checks must read them from `ILegitimateInterestRegistry` |

---

## Next Steps

1. The maintainer decides Design Choices 4, 5, 7 and 8 when work on #815 starts; the orchestrator records them in a "Maintainer Decisions" section, updates each Chosen Option, then runs `adversarial-reviewer` on Choices 1, 2, 3 and 6.
2. Open the issue file above (`open-issue` skill) and link it here and from #815.
3. Link this plan from #815, then start PR A in a worktree (`worker-brief` skill) with the local CRAP table required in the brief.
4. Re-verify the draft article numbering and the safeguards against the Official Journal text within 30 days of any publication of COM(2025) 837 (SPEC-002 AC-025).
