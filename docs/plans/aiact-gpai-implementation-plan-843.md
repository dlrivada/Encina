# Implementation Plan: `Encina.Compliance.AIAct.GPAI` — General-Purpose AI Model Governance (Arts. 51-56)

> **Issue**: [#843](https://github.com/dlrivada/Encina/issues/843) (child of [#881](https://github.com/dlrivada/Encina/issues/881), milestone v0.16.0 — AI Act)
> **Type**: Feature
> **Complexity**: High (8 phases, 6 pull requests, 1 new package, no core change, Marten event-sourced registry, ~70 files)
> **Estimated Scope**: ~1,900-2,400 lines of production code + ~2,200-2,700 lines of tests
> **Prerequisites**: none blocking; SPEC-000 §6 excludes only AI/LLM integration, and this package is a register and validator over documents a model provider keeps, with no model or inference code

---

## Summary

Implement governance for providers of general-purpose AI models under Chapter V of the AI Act: a model registry, classification of models with systemic risk (compute threshold and Commission designation), a validator that reports the status of each provider obligation of Arts. 52, 53, 54 and 55 against recorded evidence, and a model card generator. The package holds references to the documents a provider keeps (technical documentation, downstream information, copyright policy, public training-content summary, adversarial testing and incident evidence); it does not hold models, weights or inference code.

### What is already in the code (verified on `7a0049c07`)

| Item | Location | State |
|---|---|---|
| GPAI types | none in `src/` | No `GPAI`, `ModelCard` or model registry type exists |
| System-level classification | `src/Encina.Compliance.AIAct/Abstractions/IAIActClassifier.cs`, `Model/AIRiskLevel.cs` | For AI systems, a different subject from models |
| System registry pattern | `Abstractions/IAISystemRegistry.cs`, `InMemoryAISystemRegistry.cs` (publishes notifications through `IEncina`) | Notification pattern reused; its in-memory store is what #847 replaces and is not copied |
| Marten aggregate pattern in a separate compliance package | `src/Encina.Compliance.Consent/Aggregates/`, `Events/`, `ReadModels/`, `ConsentMartenExtensions.cs` | Done; the shape this package follows |
| Notifications, diagnostics, EventIds | `Notifications/*`, `Diagnostics/AIActDiagnostics.cs`, `EventIdRanges.ComplianceAIAct` (9500-9599) | Patterns reused; this plan takes 9588-9599 |

### Deviations from the issue text (each is put to the maintainer below or follows a binding rule)

- **`ComputeThresholdFlops` is renamed `TrainingComputeFlops`** (it is the model's compute, not the threshold); the threshold is configuration with the default of Art. 51(2). Designation by Commission decision (Art. 51(1)(b)) is a second route to systemic risk, so `HasSystemicRisk` is derived, never a free flag.
- **Evidence is recorded by reference, not as inline text.** `TrainingDataSummary` and `CopyrightPolicy` strings become `GPAIEvidence` references (reference, version, `IsPublic`, recorded date); the Art. 53(1)(d) summary must be "made publicly available".
- **`GPAIComplianceResult` booleans become obligation statuses** (Met, NotMet, Pending, NotApplicable) because the open-source exemption of Art. 53(2) and the transitional deadline of Art. 111(3) make a boolean wrong.
- **Obligations beyond the issue's list are covered**: Art. 52(1) notification to the Commission within two weeks, Art. 54 authorised representative for third-country providers, and the Art. 53(4) and 55(2) reliance on codes of practice (Design Choice 3). Art. 56 (the AI Office encouraging codes of practice) imposes nothing on providers and is excluded in the article table.
- **No in-memory default.** The issue asks for in-memory defaults; `AGENTS.md` §3 says event-sourced compliance modules have no InMemory stores (unit tests mock `IAggregateRepository`, integration tests run Marten on PostgreSQL through Testcontainers). The registry is a Marten event-sourced aggregate from the first pull request (Design Choice 1); the acceptance criterion "In-memory default implementations" is dropped.
- **Dates.** GPAI obligations apply from 2 Aug 2025; models placed on the market before that date comply by 2 Aug 2027 (Art. 111(3)); Commission enforcement from 2 Aug 2026 (SPEC-002 §3.2). Reg. (EU) 2026/1744 does not change them.

### Scope per pull request (one worker issue = one PR, `AGENTS.md` §11)

About 70 files and 3,900 lines, so six sequential pull requests.

| PR | Phases | Content | Files (est.) | Lines (est.) |
|---|---|---|---|---|
| 1 | 1 | Package skeleton, models, events, errors | 15 | ~800 |
| 2 | 2 | Aggregate, Marten registry, projection, systemic-risk classifier, options | 15 | ~1,100 |
| 3 | 3-4 | Obligation rules, compliance validator, model card generator | 11 | ~1,000 |
| 4 | 5 | Registration (`AddGPAIAggregates`, `AddEncinaAIActGPAI`), audit notifications, health check, DI tests | 8 | ~600 |
| 5 | 6-7 | Observability, remaining tests, Marten integration suite, justifications | 13 | ~900 |
| 6 | 8 | Documentation (`docs-writer`), changelog, ADR | 6 | ~600 |

Counts are re-measured when each worker brief is written. **Affected packages**: `Encina.Compliance.AIAct.GPAI` (new); the core is not changed.

**Provider category**: event sourcing (Marten on PostgreSQL), ADR-019 and ADR-027. The 10-provider database rule does not apply (`AGENTS.md` §5 excludes event sourcing). The package references `Encina.Marten` itself, as `Encina.Compliance.Consent` does (`ConsentMartenExtensions.AddConsentAggregates`), so it does not wait for #847.

**Family coordination (AI Act modules #838, #840, #841, #843).** Nothing here redefines the shared bias types of #838 or the Annex IV types of #840 (Annex XI and XII documents are a different list). EventIds 9588-9599 are this plan's; #838 holds 9548-9561, #840 holds 9562-9575, #841 holds 9576-9587.

---

## Design Choices

<details>
<summary><strong>1. State of the model registry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — `InMemoryGPAIModelRegistry` as the default (the issue's criterion)** | Ships without Marten; testable | An InMemory store in an event-sourced compliance module, which `AGENTS.md` §3 forbids; registrations lost on restart |
| **B — A Marten event-sourced `GPAIModelAggregate` with an inline projection, from the first pull request; no in-memory default** | Complies with the rule; durable and auditable history; the dates the Art. 52(1) and Art. 111(3) checks use survive restarts; unit tests mock `IAggregateRepository`, integration tests use Marten on PostgreSQL through Testcontainers | More files (aggregate, events, projection, integration suite); a Marten reference in the package |
| **C — Options-declared, read-only catalog of models** | No store | Evidence and notification dates change over a model's life; the issue's `RegisterModelAsync` disappears; still not an event-sourced record |

### Chosen Option: **B — Marten event-sourced aggregate from the first PR**

### Rationale

`AGENTS.md` §3 is explicit: event-sourced compliance modules have no InMemory stores; unit tests mock `IAggregateRepository` (NSubstitute) and integration tests run against Marten on PostgreSQL through Testcontainers. The package follows the shape of `Encina.Compliance.Consent` (`Aggregates/`, `Events/`, `ReadModels/`, `Services/`, `AddConsentAggregates()` in `ConsentMartenExtensions.cs`), references `Encina.Marten` and `Encina.DomainModeling` itself and does not wait for #847, which migrates the core's own registries. The acceptance criterion "In-memory default implementations" is dropped and the issue is updated.

**Class:** Settled
**Settled by:** `AGENTS.md` §3 (event-sourced compliance modules have no InMemory stores); ADR-019; ADR-027; SPEC-002 DEC-008; `src/Encina.Compliance.Consent/ConsentMartenExtensions.cs`

</details>

<details>
<summary><strong>2. What adherence to a code of practice means</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Adherence (`AdheredCodesOfPractice`) is recorded as information; every obligation still needs its own evidence to be Met** | Fail closed; an unverifiable claim never makes a gap disappear; the Article only says providers "may rely" on a code to demonstrate compliance | A provider that follows a code must still point to evidence per obligation |
| **B — Adherence to a listed, accepted code marks the covered obligations Met** | Less paperwork for adherents | The library cannot know which code covers what or whether the Commission accepted it; a boolean claim could mask real gaps |

### Chosen Option: **A — Informational record, evidence still required** (recommended, pending the maintainer)

### Rationale

Art. 53(4) and Art. 55(2): providers "may rely on codes of practice within the meaning of Article 56 to demonstrate compliance", until a harmonised standard is published; reliance is a way to demonstrate, not a presumption the library can grant. B wins for an adherent to a Commission-assessed code, once a machine-readable list of what that code covers exists; the option is then a configuration of covered obligations per code, added without changing the model.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Systemic-risk classification</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Derived: `HasSystemicRisk = TrainingComputeFlops > threshold OR designated by Commission decision`; the threshold is `GPAIOptions.SystemicRiskComputeThresholdFlops` with the default 10^25** | Follows Art. 51(1)(b) and (2) exactly; amendable by delegated act (Art. 51(3)) without a release | A model above the threshold may argue otherwise (Art. 52(2)): recorded as a reference only |
| **B — A free `HasSystemicRisk` flag set by the caller (the issue's record)** | Simple | Inconsistent with the compute figure; a caller can mislabel |
| **C — A constant 10^25** | Simplest | The Commission can change it by delegated act |

### Chosen Option: **A — Derived with a configurable threshold**

### Rationale

Art. 51(2) presumes high-impact capabilities when cumulative training computation exceeds 10^25 floating point operations ("greater than"); Art. 51(1)(b) adds designation by Commission decision; Art. 51(3) lets the Commission amend the thresholds. Missing compute with no designation means "not classified": the validator reports it as `Pending` with a reason, never as "no systemic risk".

**Class:** Settled
**Settled by:** Art. 51(1)-(3) AI Act; `AGENTS.md` §3 (fail closed on missing context); issue #843 Alternatives Considered item 2

</details>

<details>
<summary><strong>4. Obligation statuses, open-source exemption and transition deadline</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Per obligation a status (`Met`, `NotMet`, `Pending`, `NotApplicable`) with the article reference: open-source non-systemic models are `NotApplicable` for Art. 53(1)(a) and (b) (Art. 53(2)); models placed on the market before 2 Aug 2025 are `Pending` until 2 Aug 2027 and `NotMet` afterwards (Art. 111(3)); no placement date means no transition (fail closed)** | Correct in law; fail closed on missing context; the report explains each line | A richer result than the issue's booleans |
| **B — The issue's booleans** | Matches the issue | Cannot express an exemption or a transition |

### Chosen Option: **A — Status per obligation**

### Rationale

Art. 53(2): the obligations in 53(1)(a) and (b) do not apply to providers of models under a free and open-source licence whose parameters, architecture and usage information are public, and the exception never applies to models with systemic risk. Art. 111(3): providers of general-purpose AI models placed on the market before 2 August 2025 comply by 2 August 2027. The cutoff dates are in `GPAIOptions`, not constants.

**Class:** Settled
**Settled by:** Art. 53(2) and Art. 111(3) AI Act; SPEC-002 §3.2 and INV-003; `AGENTS.md` §3 (compliance gates fail closed)

</details>

<details>
<summary><strong>5. Evidence by reference and the model card source</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Evidence is a reference record per obligation (`Kind`, `Reference`, `Version`, `IsPublic`, `RecordedAtUtc`); model card text is supplied as `ModelCardContent` on the registration and rendered by the generator; a missing mandatory field is a `Left`** | The documents stay where the provider keeps them (Annex XI and XII content is large); `IsPublic` can be checked for Art. 53(1)(d) | The library does not validate what the reference points to |
| **B — Inline text for the summary and policy (the issue's strings)** | Self-contained | Cannot show that the summary is public; large text in the registry |

### Chosen Option: **A — References plus supplied model card content**

### Rationale

Art. 53(1)(d) requires a sufficiently detailed summary that is "made publicly available", which a record with `IsPublic` and a reference represents and an inline string does not. The model card sections follow the issue's record (description, intended use, limitations, training data description, evaluation results, ethical considerations), the structure of the published Model Cards proposal.

**Class:** Settled
**Settled by:** Art. 53(1)(a)-(d) AI Act; issue #843 Alternatives Considered item 7 (Model Cards, https://modelcards.withgoogle.com/about)

</details>

<details>
<summary><strong>6. Notification to the Commission and authorised representatives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Include both as obligations: Art. 52(1) is `NotMet` when the compute threshold has been met for more than two weeks (`ThresholdMetAtUtc`, from `TimeProvider`) and `CommissionNotifiedAtUtc` is empty; Art. 54(1) is `NotMet` for a third-country provider with no `AuthorisedRepresentativeId`** | Closes real, dated duties the issue's list misses | Two more registration fields |
| **B — Only Arts. 51, 53, 55 as in the issue's interfaces** | Smaller | A systemic-risk model could be reported compliant while overdue for notification |

### Chosen Option: **A — Cover Arts. 52 and 54**

### Rationale

Art. 52(1): the provider shall notify the Commission without delay and within two weeks after the Art. 51(1)(a) requirement is met or it becomes known that it will be met. Art. 54(1): providers established in third countries appoint, by written mandate, an authorised representative established in the Union before placing a model on the Union market. The exact exemptions of Art. 54 beyond paragraph 1, Art. 52(2) (substantiated arguments that a model does not present systemic risk) and Art. 52(3)-(5) were not read and are recorded as references only (unverified).

**Class:** Settled
**Settled by:** Art. 52(1) and Art. 54(1) AI Act; issue #843 ("Corrections and gaps" item on Arts. 52, 54 and 56); SPEC-000 REQ-024

</details>

<details>
<summary><strong>7. Health check, package placement and EventIds</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — New package `Encina.Compliance.AIAct.GPAI` with a registry health check (`GPAIHealthCheck`: the Marten store is reachable); EventIds 9588-9599 sub-allocated in `ComplianceAIAct`** | Matches the issue and EPIC #881; the registry has a checkable dependency now; the pattern of the family | One more registration |
| **B — Core folder, no health check** | One assembly | The core grows; an unreachable store would go unnoticed |

### Chosen Option: **A — New package, registry health check, 9588-9599**

### Rationale

The issue and its `new-package` label place the package; the first draft left the registry health check to this plan, which decides it: with a Marten store there is a dependency to check. Messages are `[LoggerMessage]` packed from 9588 with the XML line `Event IDs: 9588-9599 (see EventIdRanges.ComplianceAIAct)`.

**Class:** Trivial
**Settled by:** issue #843 (title, `new-package` label); EPIC #881; ADR-021; `AGENTS.md` §7; the EventId sections of the #838, #842 and #847 plans; `src/Encina/Diagnostics/EventIdRanges.cs:389`

</details>

---

## Implementation Phases

### Phase 1: Package Skeleton, Models & Errors (PR 1)

> **Goal**: A buildable package with the GPAI vocabulary.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create the project** `src/Encina.Compliance.AIAct.GPAI/` (csproj referencing `Encina.Compliance.AIAct`, `Encina.Marten`, `Encina.DomainModeling`, `PublicAPI.*.txt`, `InternalsVisibleTo` the test projects), add to `Encina.slnx`, create its coverage manifest with `generate-coverage-manifest.cs`. Create `Events/GPAIModelEvents.cs`: `GPAIModelRegistered`, `GPAIEvidenceRecorded`, `GPAICommissionNotified`, `GPAISystemicRiskDesignated`, `GPAIModelDetailsUpdated` (records, timestamps supplied by the service from `TimeProvider`).
2. **Create `Model/GPAIModelRegistration.cs`**: `ModelId`, `ModelName`, `ProviderId`, `ProviderEstablishedInUnion`, `AuthorisedRepresentativeId?`, `TrainingComputeFlops?`, `DesignatedByCommission` plus `DesignationReference?`, `IsOpenSourceLicensed`, `PlacedOnMarketAtUtc?`, `ThresholdMetAtUtc?`, `CommissionNotifiedAtUtc?`, `Evidence` (list of `GPAIEvidence`), `DownstreamProviders`, `AdheredCodesOfPractice`, `ModelCardContent?`, `RegisteredAtUtc`.
3. **Create `Model/GPAIEvidence.cs`** (`Obligation`, `Reference`, `Version?`, `IsPublic`, `RecordedAtUtc`), `Model/GPAIObligation.cs` (`TechnicalDocumentation` 53(1)(a), `DownstreamInformation` 53(1)(b), `CopyrightPolicy` 53(1)(c), `TrainingContentSummary` 53(1)(d), `CommissionNotification` 52(1), `AuthorisedRepresentative` 54(1), `AdversarialTesting` 55(1)(a), `SystemicRiskAssessment` 55(1)(b), `SeriousIncidentReporting` 55(1)(c), `Cybersecurity` 55(1)(d)), `Model/GPAIObligationStatusKind.cs` (`Met`, `NotMet`, `Pending`, `NotApplicable`), `Model/GPAIObligationStatus.cs` (`Obligation`, `Status`, `ArticleReference`, `Reason`).
4. **Create `Model/GPAIComplianceResult.cs`** (`ModelId`, `HasSystemicRisk`, `Statuses`, `IsCompliant`, `EvaluatedAtUtc`), `Model/ModelCardContent.cs` (description, intended use, limitations, training data description, evaluation results, ethical considerations) and `Model/ModelCard.cs`.
5. **Create `GPAIErrors.cs`**: codes `aiact.gpai_model_not_found`, `aiact.gpai_invalid_registration`, `aiact.gpai_model_card_incomplete`; factories never carry exception messages.
6. **Tests**: records and errors; guard tests; manifest targets with justifications.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #843: the new package Encina.Compliance.AIAct.GPAI.

CONTEXT:
- Chapter V obligations: Art. 51 classification (compute greater than 10^25 or Commission designation), Art. 52(1) notification within
  two weeks, Art. 53(1)(a)-(d) provider duties, Art. 54(1) representative, Art. 55(1)(a)-(d) systemic-risk duties.
- Evidence is recorded by reference (plan Design Choice 5); statuses replace booleans (Design Choice 4).

TASK:
Create the project, slnx entry, PublicAPI files, manifest, the models listed in the plan and the errors class, with tests.

KEY RULES:
- Either<EncinaError, T> everywhere; time never read inside records; no model, weights or inference code in the package.
- XML docs cite the article of each field; English only; zero warnings.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/Encina.Compliance.AIAct.csproj
- src/Encina.Compliance.AIAct/Model/AISystemRegistration.cs
- src/Encina.Compliance.AIAct/AIActErrors.cs
- .github/scripts/generate-coverage-manifest.cs
```

</details>

---

### Phase 2: Marten Aggregate, Registry & Systemic-Risk Classification (PR 2)

> **Goal**: Register models durably and classify them.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `Abstractions/IGPAIModelRegistry.cs`**: `RegisterModelAsync`, `GetModelAsync`, `GetSystemicRiskModelsAsync` (as in the issue; `Left(aiact.gpai_model_not_found)` for an unknown id).
2. **Create `Aggregates/GPAIModelAggregate.cs`** (`AggregateBase`, deterministic `Guid` stream id from the `ModelId` with a name-based UUID), `ReadModels/GPAIModelReadModel.cs` and `ReadModels/GPAIModelProjection.cs` (inline, keyed by `ModelId`), and `Services/MartenGPAIModelRegistry.cs` (`IGPAIModelRegistry` over `IAggregateRepository<GPAIModelAggregate>` and the read model; a repeat registration of the same `ModelId` is an update event, not a second stream). No in-memory implementation exists.
3. **Create `Classification/SystemicRiskClassifier.cs`** (internal static): `bool? IsSystemicRisk(registration, threshold)`; null when compute is missing and no designation (Design Choice 3).
4. **Create `GPAIOptions.cs`** and validator: `SystemicRiskComputeThresholdFlops` (1e25), `GpaiObligationsApplyFromUtc` (2025-08-02), `LegacyModelDeadlineUtc` (2027-08-02), `CommissionNotificationWindowDays` (14).
5. **Create `RegistrationValidator.cs`** (internal): non-blank ids, non-negative compute, designation reference when designated, evidence kinds known.
6. **Tests**: unit tests mock `IAggregateRepository` (NSubstitute): threshold boundary (exactly 10^25 is not above), designation, missing compute, repeat registration, unknown id, optimistic-concurrency conflict mapped to a `Left`, validator failures; integration tests in `tests/Encina.IntegrationTests/Compliance/AIAct/GPAI/` with `[Collection(MartenCollection.Name)]` against PostgreSQL through Testcontainers (register, read back, evidence recorded, replay equals projection).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #843 in Encina.Compliance.AIAct.GPAI.

CONTEXT:
- Models and events exist (Phase 1). Design Choices 1 and 3: the registry is a Marten event-sourced aggregate (no in-memory store,
  AGENTS.md section 3), derived systemic-risk classification with a configurable threshold, strictly "greater than" per Art. 51(2).

TASK:
Implement IGPAIModelRegistry, GPAIModelAggregate, GPAIModelReadModel and projection, MartenGPAIModelRegistry, SystemicRiskClassifier,
GPAIOptions and validator, RegistrationValidator. Unit tests mock IAggregateRepository; integration tests use the Marten collection.

KEY RULES:
- Integration tests: [Collection(MartenCollection.Name)], [Trait("Category", "Integration")], never a per-class fixture.
- Projections take dependencies through IDocumentOperations/constructor injection, never IServiceProvider.
- Missing compute without designation is "not classified", never "no systemic risk".
- Time from TimeProvider; registration publishes an audit notification and a publish Left fails the call.
- Thresholds and dates come from options; CRAP <= 10 per method; tests execute real classes.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Aggregates/ConsentAggregate.cs
- src/Encina.Compliance.Consent/ReadModels/ConsentProjection.cs
- src/Encina.Compliance.Consent/ConsentMartenExtensions.cs
- src/Encina.Compliance.AIAct/AIActOptionsValidator.cs
```

</details>

---

### Phase 3: Obligation Rules & Compliance Validator (PR 3)

> **Goal**: A per-obligation status report.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `Abstractions/IGPAIComplianceValidator.cs`**: `ValidateComplianceAsync(modelId, ct)`.
2. **Create `Rules/ObligationRules.cs`** (internal): one small method per obligation returning a `GPAIObligationStatus`: 53(1)(a) and (b) `NotApplicable` for open-source non-systemic models; 53(1)(c) needs a copyright-policy evidence record; 53(1)(d) needs a training-summary record with `IsPublic`; 52(1) `NotMet` when the threshold was met more than the window ago and no notification is recorded; 54(1) for non-Union providers; 55(1)(a)-(d) only for systemic-risk models.
3. **Create `Rules/TransitionRules.cs`** (internal): models placed before `GpaiObligationsApplyFromUtc` are `Pending` until `LegacyModelDeadlineUtc`, then `NotMet`; no placement date means no transition.
4. **Create `DefaultGPAIComplianceValidator.cs`**: loads the model, evaluates every obligation with `TimeProvider.GetUtcNow()`, sets `IsCompliant` only when no status is `NotMet` or `Pending`, with code-only reasons.
5. **Tests**: each obligation in each status, the open-source exemption not applying to a systemic-risk model, transition boundaries, 52(1) window boundary (day 14 versus 15), representative rule, unknown model, code of practice recorded but not changing a status.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #843 in Encina.Compliance.AIAct.GPAI.

CONTEXT:
- Registry and classifier exist (Phase 2). Design Choices 2, 4 and 6 of the plan define the statuses, the exemption, the transition and
  the Art. 52/54 rules; adherence to a code of practice is informational only.

TASK:
Implement ObligationRules, TransitionRules, IGPAIComplianceValidator and DefaultGPAIComplianceValidator.

KEY RULES:
- Fail closed: unknown model is a Left; missing evidence is NotMet (or Pending only inside the transition window); no placement
  date never grants the transition.
- Reasons are codes or fixed phrases, never user text or EncinaError.Message.
- One rule per method, CRAP <= 10; time only from TimeProvider.

REFERENCE FILES:
- src/Encina.Compliance.AIAct.GPAI/Model/
- src/Encina.Compliance.AIAct/DefaultAIActComplianceValidator.cs
- https://artificialintelligenceact.eu/article/53/
```

</details>

---

### Phase 4: Model Card Generator (PR 3)

> **Goal**: A model card from the registration's supplied content.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `Abstractions/IModelCardGenerator.cs`**: `GenerateModelCardAsync(modelId, ct)`.
2. **Create `DefaultModelCardGenerator.cs`**: load the model; a missing `ModelCardContent` or blank mandatory field is `Left(aiact.gpai_model_card_incomplete)` naming the field; render `ModelCard` with `GeneratedAtUtc` from `TimeProvider`; training data description may cite the public summary reference.
3. **Tests**: complete card, each missing field, unknown model, deterministic output for a fixed clock.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #843 in Encina.Compliance.AIAct.GPAI.

CONTEXT:
- Design Choice 5: model card content is supplied on the registration; the generator renders and validates it.

TASK:
Implement IModelCardGenerator and DefaultModelCardGenerator with tests.

KEY RULES:
- Never invent content; a missing mandatory field is a Left naming the field.
- Time from TimeProvider; Either<EncinaError, T>; CRAP <= 10.

REFERENCE FILES:
- src/Encina.Compliance.AIAct.GPAI/Model/ModelCard.cs
- src/Encina.Compliance.AIAct.GPAI/Model/ModelCardContent.cs
- https://modelcards.withgoogle.com/about
```

</details>

---

### Phase 5: Configuration, DI, Health Check & Audit Notifications (PR 4)

> **Goal**: One registration call and audit evidence.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create notifications** `GPAIModelRegisteredNotification` (model id, systemic-risk flag, `OccurredAtUtc`) and `GPAIComplianceEvaluatedNotification` (model id, count of `NotMet`, count of `Pending`, `OccurredAtUtc`), published through `IEncina.Publish`; no evidence text.
2. **Create `GPAIMartenExtensions.cs`** with `AddGPAIAggregates()` (aggregate repository and projection registration, as `AddConsentAggregates()`) and **`ServiceCollectionExtensions.cs`** with `AddEncinaAIActGPAI(Action<GPAIOptions>?)`: options, validator, `TimeProvider`, `MartenGPAIModelRegistry` (`TryAdd`, an application registry wins in any order), classifier, validator, model card generator, health check.
3. **Create `Health/GPAIHealthCheck.cs`** (`DefaultName`, static `Tags`, scoped resolution via `IServiceProvider.CreateScope()`): the registry's Marten store answers a read.
4. **DI test** with `ValidateOnBuild` and `ValidateScopes`, in both registration orders with `AddEncinaAIAct`, with the Marten services registered; **tests**: end-to-end register, validate and generate a card (integration, Marten collection); publish failure fails the call.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #843 in Encina.Compliance.AIAct.GPAI.

CONTEXT:
- Registry, validator and card generator exist. Core notifications are published with IEncina.Publish.

TASK:
Add the two audit notifications and AddEncinaAIActGPAI with a ValidateOnBuild/ValidateScopes DI test.

KEY RULES:
- Registration completeness (AGENTS.md section 3): every option type and dependency registered (including the Marten aggregate
  services and the health check); an application's registry wins in any order.
- A Left from Publish fails the operation; notifications carry ids and counts only.

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

1. **Create `Diagnostics/GPAIDiagnostics.cs`**: source and meter `Encina.Compliance.AIAct.GPAI`; span `aiact.gpai.validate` (model id, outcome); counters `aiact.gpai.models_registered` (tag systemic-risk flag) and `aiact.gpai.compliance_checks` (tag outcome).
2. **Create `Diagnostics/GPAILogMessages.cs`**, `[LoggerMessage]`, ids 9588-9599 packed: model registered, registration rejected, model not found, systemic risk classified, classification pending (compute missing), notification overdue (Art. 52), obligation not met, obligation pending (transition), registry unavailable (store failure), validation completed, model card generated, model card incomplete.
3. **Modify `EncinaEventIdAllocationTests.AssemblyRanges`**: `["Encina.Compliance.AIAct.GPAI"] = [nameof(EventIdRanges.ComplianceAIAct)]`.
4. A notification publish failure is logged with the generic registry-failure event by code (counted in the 12 above through "registration rejected" and "validation completed" outcomes).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #843 in Encina.Compliance.AIAct.GPAI.

CONTEXT:
- ComplianceAIAct is 9500-9599: 9548-9561 #838, 9562-9575 #840, 9576-9587 #841, 9588-9599 this issue.

TASK:
Create GPAIDiagnostics and GPAILogMessages (12 [LoggerMessage] methods, ids 9588-9599, no gaps), map the assembly in the
architecture test, instrument the registry, validator and generator.

KEY RULES:
- ADR-021: no id outside the range; run the architecture tests. XML line: Event IDs: 9588-9599 (see EventIdRanges.ComplianceAIAct).
- Never log or tag EncinaError.Message or evidence text; error codes and ForLogging() only; model id on spans only, not on metrics.

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

1. **Unit**: registry, classifier, every obligation rule, transition rules, validator, card generator, notifications, telemetry.
2. **Guard**: every public constructor and method parameter.
3. **Contract**: `IGPAIModelRegistry`, `IGPAIComplianceValidator`, `IModelCardGenerator` on real implementations.
4. **Property**: classification monotonic in compute; the list of `NotMet` obligations equals the obligations without evidence (for non-exempt, non-transition models); status list always has one entry per applicable obligation.
5. **DI test** (Phase 5). **Integration**, **Load**, **Benchmark**: `.md` justifications under `Compliance/AIAct/GPAI/` for Load and Benchmark (append-only streams per model, no shared state; not a hot path). Integration is implemented, not justified: Marten on PostgreSQL through Testcontainers (`[Collection(MartenCollection.Name)]`) for the registry, projection, evidence and notification-date round trips and the validator against stored models.
6. **Coverage manifest**: per-file targets and justifications; measure each flag; local CRAP table for changed methods (<= 10).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are completing the tests of issue #843 in Encina.Compliance.AIAct.GPAI.

CONTEXT:
- Phases 1-6 are implemented; earlier PRs brought their own tests.

TASK:
Write the missing tests (including the Marten integration suite) and the two .md justifications (load, benchmark); bring every flag
to its manifest target.

KEY RULES:
- Tests execute real code; Shouldly via Encina.Testing.Shouldly; FakeTimeProvider; no Thread.Sleep. Do not lower any target.
- Justification files follow the AGENTS.md section 9 template.

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/AIAct/
- tests/Encina.BenchmarkTests/Compliance/AIAct/AIAct.md
- .github/coverage-manifest/Encina.Compliance.AIAct.GPAI.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 8: Documentation & Finalization (PR 5)

> **Goal**: Documentation, changelog, public API and verification.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on all public APIs (`<summary>`, `<remarks>` with the article, `<param>`, `<returns>`, `<example>`).
2. `changelog.d/843-aiact-gpai.added.md` (written by `mechanical-fixer`).
3. Package README; `docs/features/aiact-compliance.md` Chapter V section with the article table of SPEC-000 REQ-024 (Art. 51 covered; 52, 53, 54, 55 covered; 56 excluded as it binds the AI Office), through `docs-writer`; state the dates and that the package is a register, not an LLM integration.
4. `docs/INVENTORY.md`, `ROADMAP.md`; ADR only if an architectural decision arises: the next number neither used nor reserved in `docs/architecture/adr/index.md`.
5. `PublicAPI.Unshipped.txt` complete; build with 0 warnings; tests; every flag at target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of issue #843 in Encina.Compliance.AIAct.GPAI.

CONTEXT:
- Phases 1-7 are merged and measured.

TASK:
Write the changelog fragment, README, feature-page section with the article table, INVENTORY and ROADMAP entries (and the ADR if
required); finish the public API files; verify build, tests and per-flag coverage.

KEY RULES:
- Never edit CHANGELOG.md [Unreleased]; fragments only. Docs never type coverage figures by hand (covref markers, SPEC-001).
- Pages go through the docs-writer agent; English only; no AI attribution; zero warnings.

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
| AI Act Art. 51(1)-(3) | Classification as a model with systemic risk by high-impact capabilities or Commission decision; presumed when cumulative training computation is greater than 10^25 floating point operations; delegated acts may amend thresholds | Design Choice 3, Phase 2 | Text read |
| AI Act Art. 52(1) | Provider notifies the Commission within two weeks after the Art. 51(1)(a) condition is met or known to be met | Design Choice 6, Phase 3 | Text read for paragraph 1; paragraphs 2-5 (rebuttal arguments and the rest) unverified |
| AI Act Art. 53(1)(a)-(d) | Technical documentation (Annex XI), information for downstream providers (Annex XII), copyright policy, public training-content summary on the AI Office template | Phases 1, 3 | Text read; the contents of Annexes XI and XII are unverified here |
| AI Act Art. 53(2) | Open-source exemption from 53(1)(a) and (b), never for systemic-risk models | Design Choice 4 | Text read |
| AI Act Art. 53(4), 55(2) | Reliance on codes of practice to demonstrate compliance until a harmonised standard is published | Design Choice 2 | Text read |
| AI Act Art. 54(1) | Third-country providers appoint an authorised representative in the Union before placing a model on the market | Design Choice 6 | Text read for paragraph 1; the remaining paragraphs and the exemption are unverified |
| AI Act Art. 55(1)(a)-(d) | Systemic-risk providers: model evaluation and adversarial testing, systemic risk assessment and mitigation, serious incident tracking and reporting, cybersecurity | Phase 3 | Text read |
| AI Act Art. 56 | AI Office encourages codes of practice | Excluded (binds the AI Office) | Text read for paragraph 1 |
| AI Act Art. 111(3) | Models placed on the market before 2 Aug 2025 comply by 2 Aug 2027 | Design Choice 4 | Text read |
| Reg. (EU) 2026/1744 | Amends the AI Act (Digital Omnibus); no change to Chapter V dates assumed | Feature page | SPEC-002 §3.2 and the issue comment; not re-read in the Official Journal |
| Commission enforcement from 2 Aug 2026 | Enforcement powers over GPAI providers | Feature page | From SPEC-002 §3.2; unverified here |
| SPEC-000 REQ-024, REQ-025, §6 | Article table; Arts. 51-56 in scope; only AI/LLM integration excluded | Phase 8 | Repository documents |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ConsentAggregate`, `ConsentProjection`, `ConsentMartenExtensions` | `src/Encina.Compliance.Consent/` | Pattern for the Marten aggregate, inline projection and `AddGPAIAggregates()` |
| `MartenCollection` fixture | `tests/Encina.IntegrationTests/` | Integration tests on PostgreSQL |
| Notifications and `IEncina.Publish` | `src/Encina.Compliance.AIAct/Notifications/`, `InMemoryAISystemRegistry.cs:109` | Audit evidence |
| `AIActErrors`, `AIActOptionsValidator` | `src/Encina.Compliance.AIAct/` | Error naming and validator style |
| `ServiceCollectionExtensions` | `src/Encina.Compliance.AIAct/ServiceCollectionExtensions.cs` | Registration pattern (`TryAdd`, `TimeProvider`) |
| `AIActDiagnostics`, `AIActLogMessages` | `src/Encina.Compliance.AIAct/Diagnostics/` | Style of the package's diagnostics |
| `EventIdRanges.ComplianceAIAct`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs:389` | Range 9588-9599 and assembly map |
| Coverage manifest generator | `.github/scripts/generate-coverage-manifest.cs` | New package manifest |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.AIAct` (#415, #847, #842) | 9500-9547 | In use or reserved |
| `Encina.Compliance.AIAct.BiasDetection` (#838) | 9548-9561 | Reserved by the #838 plan |
| `Encina.Compliance.AIAct.TechnicalDocumentation` (#840) | 9562-9575 | Reserved by the #840 plan |
| `Encina.Compliance.AIAct.Transparency` (#841) | 9576-9587 | Reserved by the #841 plan |
| **`Encina.Compliance.AIAct.GPAI` (#843)** | **9588-9599** | **12 ids, the last of `ComplianceAIAct`; any further AI Act package needs a new range** |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Package project files | 5 | csproj, two PublicAPI files, `Encina.slnx`, manifest |
| Models and errors | 11 | registration, evidence, obligation, status kind, status, result, model card content, model card, errors, options, options validator |
| Abstractions | 3 | registry, compliance validator, model card generator |
| Marten aggregate, events, read model, projection, registry | 5 | `GPAIModelEvents`, `GPAIModelAggregate`, `GPAIModelReadModel`, `GPAIModelProjection`, `MartenGPAIModelRegistry` |
| Classifier, rules, validators | 6 | classifier, registration validator, obligation rules, transition rules, validator, generator |
| DI, health check, notifications | 5 | `ServiceCollectionExtensions`, `GPAIMartenExtensions`, `GPAIHealthCheck`, two notifications |
| Diagnostics | 2 | `GPAIDiagnostics`, `GPAILogMessages` |
| Tests | ~27 | unit, guard, contract, property, Marten integration, DI, architecture map, two `.md` justifications |
| Documentation | 6 | changelog fragment, README, feature page, ADR (conditional), INVENTORY, ROADMAP |
| **Total** | **~70** | **Six pull requests of 6 to 15 files** |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #843 — EU AI Act Chapter V (Arts. 51-56) general-purpose AI model governance — as the new package
Encina.Compliance.AIAct.GPAI. One phase group per pull request.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0: no backward compatibility, no [Obsolete], best solution always.
- Railway Oriented Programming: Either<EncinaError, T>; compliance gates fail closed (missing evidence or context is never a pass).
- The package is a register and validator over references to documents a provider keeps; no model, weights, inference or LLM code.
- Event-sourced compliance module (AGENTS.md section 3): the registry is a Marten aggregate from the first PR, no InMemory store;
  unit tests mock IAggregateRepository, integration tests use Marten on PostgreSQL through Testcontainers.

IMPLEMENTATION OVERVIEW:
Phase 1: package skeleton (references Encina.Marten); stream events; GPAIModelRegistration (TrainingComputeFlops, designation, open-source flag, placement date, notification
         dates, evidence list, model card content), GPAIEvidence, GPAIObligation, statuses, GPAIComplianceResult, ModelCard, errors.
Phase 2: IGPAIModelRegistry, GPAIModelAggregate + inline projection + MartenGPAIModelRegistry, SystemicRiskClassifier (strictly greater than 10^25 or designated),
         GPAIOptions + validator, RegistrationValidator.
Phase 3: ObligationRules (53(1)(a)-(d), 52(1), 54(1), 55(1)(a)-(d)), TransitionRules (2 Aug 2025 to 2 Aug 2027),
         DefaultGPAIComplianceValidator; open-source exemption never applies to systemic-risk models.
Phase 4: DefaultModelCardGenerator from supplied ModelCardContent.
Phase 5: two audit notifications and AddEncinaAIActGPAI with DI tests in both registration orders.
Phase 6: ActivitySource/Meter and 12 log events 9588-9599; architecture-test map.
Phase 7: all test types (Marten integration included) and two .md justifications. Phase 8: docs-writer pages with the Chapter V article table, changelog, INVENTORY, ROADMAP.

KEY PATTERNS:
- TimeProvider only; thresholds and dates from options; reasons are codes, never user text or EncinaError.Message.
- Code-of-practice adherence is informational only; every obligation still needs its own evidence to be Met.
- TryAdd registrations; DI completeness test with ValidateOnBuild and ValidateScopes; CRAP <= 10 per changed method.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ConsentMartenExtensions.cs
- src/Encina.Compliance.AIAct/ServiceCollectionExtensions.cs
- src/Encina.Compliance.AIAct/Diagnostics/AIActLogMessages.cs
- docs/plans/aiact-transparency-implementation-plan-841.md
- docs/plans/aiact-marten-event-sourcing-implementation-plan-847.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ❌ N/A | Validation reflects the current evidence and clock; a cached result would hide a missed deadline |
| 2 | **OpenTelemetry** | ✅ Phase 6 | Span `aiact.gpai.validate`; counters `aiact.gpai.models_registered` and `aiact.gpai.compliance_checks` |
| 3 | **Structured Logging** | ✅ Phase 6 | 12 `[LoggerMessage]` events, ids 9588-9599 inside `ComplianceAIAct` (ADR-021) |
| 4 | **Health Checks** | ✅ Phase 5 | `GPAIHealthCheck`: the Marten store behind the registry answers a read (Design Choice 7) |
| 5 | **Validation** | ✅ Phases 2, 4 | Options validator; registration validation; model card field validation |
| 6 | **Resilience** | ❌ N/A | No external call |
| 7 | **Distributed Locks** | ❌ N/A | One stream per model; concurrent updates are resolved by Marten optimistic concurrency, mapped to a `Left` |
| 8 | **Transactions** | ✅ Phase 2 | Each registration or evidence command appends its events and updates the inline projection in one `SaveChangesAsync` |
| 9 | **Idempotency** | ✅ Phase 2 | Registration is keyed by `ModelId`; a repeat is the same registration |
| 10 | **Multi-Tenancy** | ⏭️ Defer ([#845](https://github.com/dlrivada/Encina/issues/845)) | Per-tenant model registries are part of the tenant-scoped AI Act enforcement |
| 11 | **Module Isolation** | ⏭️ Defer ([#846](https://github.com/dlrivada/Encina/issues/846)) | `ModuleId`-scoped enforcement is delivered package-wide |
| 12 | **Audit Trail** | ✅ Phase 5 | Registration and compliance-evaluation notifications through `IEncina.Publish` |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | State | Why |
|---|---|---|
| `Encina.Compliance.AIAct` core ([#415](https://github.com/dlrivada/Encina/issues/415)) | Closed, in `main` | Notification and registration patterns, `ComplianceAIAct` range |

No `needs-decision` issue blocks this plan.

### Recommended (Not Blocking)

- [#847](https://github.com/dlrivada/Encina/issues/847): migrates the core's own registries to Marten; this package does not wait for it and follows the `Encina.Compliance.Consent` shape.
- [#840](https://github.com/dlrivada/Encina/issues/840): a GPAI provider that also builds high-risk systems documents them separately; no shared types.
- [#845](https://github.com/dlrivada/Encina/issues/845), [#846](https://github.com/dlrivada/Encina/issues/846): tenant and module scoping.
- A new EventId range if another AI Act package is planned: `ComplianceAIAct` is full after this plan.

---

## Next Steps

1. The maintainer answers Design Choice 2 when work on #843 starts; the orchestrator records the answers in a `## Maintainer Decisions` section after this one and runs `check-plan` without `-Draft`.
2. `adversarial-reviewer` tries to break the Settled and Trivial choices (1, 3, 4, 5, 6, 7) before the first worker brief, and `decision-extractor` lists any implicit choice.
3. Brief the six pull requests in order, each with its re-measured `Size:` line, per-file coverage targets and the local CRAP command.
4. Update #843's acceptance criteria to the decided scope (Marten registry instead of "In-memory default implementations", renamed compute field, evidence references, obligation statuses, Arts. 52 and 54).
