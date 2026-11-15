# Implementation Plan: `Encina.Compliance.NIS2` — Risk Analysis Lifecycle & Evidence Capture (Art. 21(2)(a))

> **Issue**: [#824](https://github.com/dlrivada/Encina/issues/824)
> **Type**: Feature
> **Complexity**: High (event-sourced aggregate, risk register projection, review monitor, 5 pull requests)
> **Estimated Scope**: 55 file touches and about 5,450 lines including tests and per-PR metadata; split into 5 pull requests, see "Pull request split"
> **Milestone**: v0.15.0 — EU Compliance: NIS2 & Digital Omnibus
> **Provider Category**: None for databases (single event-sourcing provider: Marten on PostgreSQL, ADR-027)
> **Family**: #822 to #826. Shared pieces are owned by the plan of #822; the opt-in decision for evaluators is owned by the plan of #823 (Design Choice 3).

---

## Summary

`RiskAnalysisEvaluator` passes when `NIS2Options.HasRiskAnalysisPolicy` is true and adds GDPR alignment details (Art. 35) from `IGDPRComplianceValidator` and `IProcessingActivityRegistry`. The verdict rests on one boolean: nothing records that a risk analysis was performed, by whom, with which method, which risks it found or what was decided about them.

This plan adds an event-sourced **risk analysis lifecycle** in the style of the GDPR DPIA module:

- `RiskAnalysisAggregate` (an `AggregateBase`) holding one analysis cycle: scope, methodology, assessor, the identified risks with severity and likelihood, a treatment decision per risk, mitigation evidence and management's acceptance of the residual risk.
- `INIS2RiskAnalysisService`, an inline Marten projection that serves the **risk register** and the **posture trend** (one snapshot per completed analysis), an opt-in review monitor, a health check addition and `nis2.risk_analysis.*` metrics.
- A review date derived from the highest residual risk, so higher risk is reviewed sooner.

**Standards**: NIS2 Directive (EU) 2022/2555 Art. 21(1) and 21(2)(a), (f); ISO/IEC 27005 and NIST SP 800-30 for the iterative cycle and the severity by likelihood matrix. **Not in scope**: automatic risk discovery, control libraries, an automatic review trigger from incident events (Design Choice 5), changes to the incident, supplier or accountability modules.

**Affected packages**: `Encina.Compliance.NIS2` (extended). The `Encina` core, the `Encina.Marten` and `Encina.DomainModeling` references, the EventId range and the registration entry point come from the #822 plan.

**Provider category**: none of the 10 database providers; event sourcing (Marten) is excluded from the database matrix by AGENTS.md section 5.

### Corrections to the issue text (checked against today's code)

| Issue text | Today's code | Plan decision |
|---|---|---|
| `RiskAnalysisStatus` has `InProgress` and `RisksIdentified` | `RiskIdentified` can repeat; a status for "at least one" and one for "in progress" cannot both be reached by a distinct event | `Initiated`, `RisksIdentified` (after the first `RiskIdentified`), `TreatmentPlanned`, `TreatmentInProgress`, `ReviewComplete`, `Accepted`, `RequiresReview`; `InProgress` is dropped (Design Choice 6) |
| `RiskAnalysisCompleted` "next review scheduled" | Needs the review calculator | The aggregate computes `NextReviewDueAtUtc` from the highest residual risk (Design Choice 3) |
| Metrics `encina.nis2.risk_analysis.*` | The package meter `Encina.Compliance.NIS2` names instruments `nis2.*` | `nis2.risk_analysis.*` |
| "Trend tracking" (Motivation) | No event or read model in the issue | The projection stores one `RiskPostureSnapshot` per completed analysis |
| Cache follow-up (matrix row 1) | Reads are inline-projection lookups | Caching is not applicable; no follow-up issue |

### Shared pieces

| Piece | Owner | What this plan does |
|---|---|---|
| EventId range `ComplianceNIS2EventSourced` (9800-9899) | #822 plan | Allocates **9850-9869** (20 ids) |
| Project references `Encina.Marten`, `Encina.DomainModeling` (and `Encina.Marten.GDPR` if the personal-data Dilemma resolves to B) | #822 plan, PR 1 | Depends on that pull request |
| `AddNIS2EventSourcing()` | #822 plan, Phase 4 | Adds the aggregate repository and projection lines |
| ADR-064 | #822 plan | Referenced |
| Personal data in events (`PerformedBy`, `acceptedBy`) | Dilemma 6 of the #822 plan | Applied in Design Choice 8 |
| Evaluators read the register when the workflow is registered | Dilemma 3 of the #823 plan | Applied in Design Choice 4 |

---

## Design Choices

<details>
<summary><strong>1. Aggregate granularity — one stream per analysis cycle holding its risks</strong></summary>

- **Class**: Trivial
- **Settled by**: the issue's Proposed Solution (`RiskAnalysisAggregate` with `Risks`); the convention of `DPIAAggregate` (`src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs`), which holds its assessment result and mitigations in one stream.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One aggregate per analysis cycle; risks are entries of the aggregate keyed by `RiskId`** | A cycle closes and is accepted as a unit; matches the issue and DPIA | A very large cycle makes a large stream |
| B) One aggregate per risk | Risks live across cycles | The acceptance of the analysis spans many streams |

### Chosen Option: **A**

### Rationale

- The risk register across cycles is the projection (`RiskRegisterEntry` rows keyed by tenant and risk id), not an aggregate.

</details>

<details>
<summary><strong>2. Scoring — severity by likelihood matrix with bands</strong></summary>

- **Class**: Dilemma

The issue's acceptance criterion ("Risk register with severity x likelihood scoring") fixes that a score is severity times likelihood. NIST SP 800-30 and ISO/IEC 27005 treat risk as a function of likelihood and impact but prescribe neither a 5x5 scale nor cut-offs, so the scale and the bands below are conventions, not citations.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Five-level `RiskSeverity` and `RiskLikelihood` (1 to 5); `Score = severity x likelihood` (1 to 25); default bands `Low` 1-4, `Medium` 5-9, `High` 10-16, `Critical` 17-25 in a pure `RiskScoring` class, the band thresholds being options** | Familiar matrix, testable, adjustable per entity | The default cut-offs are a convention nobody has approved yet |
| B) Qualitative matrix with fixed bands (no thresholds in options) | One behaviour for everyone | An entity with its own methodology (ISO 27005 asks each organisation to define its criteria) cannot express it |
| C) The organisation supplies its own scoring function (`IRiskScorer`) with no default bands | Fits any methodology | The module is unusable out of the box; every user writes a scorer |

### Chosen Option: **A — default matrix, bands as options (recommended, pending the maintainer)**

### Rationale

- A is better while there is no sector-specific method to cite: the defaults work, and the validator rejects thresholds that overlap or leave gaps. B is better when comparability across entities matters more than fit; C when most users already have a scoring method.
- Residual risk after mitigation is scored the same way (`RiskMitigationImplemented` carries the residual severity and likelihood), and the highest residual score drives the review date. Out-of-range values return `Left` in the service.

</details>

<details>
<summary><strong>3. Review scheduling — from the highest residual band, options with defaults</strong></summary>

- **Class**: Dilemma

The periods drive `NextReviewDueAtUtc`, the review monitor, the health check (`Degraded`) and the evaluator verdict, so they have user consequences. Only the 365-day value has a code precedent (`DPIAOptions.DefaultReviewPeriod`, used by `DPIAReviewReminderService`); the 90 and 180 days have no citation.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `RiskAnalysisOptions.ReviewPeriods` per band with defaults `Critical` 90 days, `High` 180 days, `Medium` and `Low` 365 days (365 from the DPIA default; 90 and 180 are conventions)** | One rule, configurable, higher risk is reviewed sooner | Two of the defaults are conventions the maintainer must approve |
| B) One review period (365 days, the DPIA precedent) | Fully cited, simple | Ignores the risk level the issue asks the review date to follow |
| C) No default: the entity must set every period, validation fails otherwise | No invented number | Not usable out of the box |

### Chosen Option: **A — options-only defaults (recommended, pending the maintainer)**

### Rationale

- A keeps the review date tied to the band as the issue asks, and the values are options, so approval of different numbers changes a default, not the design. B is better if the maintainer wants no unreferenced number; C if the entity must own its policy.
- `RiskAnalysisCompleted` stores the computed `NextReviewDueAtUtc` so the date stays auditable if the options change later.

</details>

<details>
<summary><strong>4. Relation to the boolean policy flag and the evaluator</strong></summary>

- **Class**: Dilemma

The question and its options belong to the family decision in the #823 plan (Design Choice 3); this plan applies it.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) When the workflow is registered, `RiskAnalysisEvaluator` is satisfied only if the latest analysis is `Accepted` and its review is not overdue; otherwise it keeps reading `HasRiskAnalysisPolicy` and its GDPR details** | Opt-in; the register becomes the evidence | Two paths until a later decision removes one |
| B) Remove `HasRiskAnalysisPolicy`; the register is the only source | One source | Every NIS2 user needs Marten |

### Chosen Option: **A (recommended, pending the maintainer; follows the #823 plan, Design Choice 3)**

### Rationale

- A is better while stateless users exist; B wins if NIS2 is declared Marten-only for 1.0. The GDPR alignment details of the evaluator stay in both paths.
- The evaluator reads the register through `INIS2RiskAnalysisService` (a read, not a gate), resolved optionally. Lifetimes: `RiskAnalysisEvaluator` is a singleton (its constructor takes only a logger) and the service is scoped over the Marten repository, so the evaluator never captures it: it resolves the service from `context.ServiceProvider` inside a scope created with `IServiceScopeFactory` from that provider, which is safe whether the context's provider is scoped or the root. If the service is not registered, `GetService` returns null and the boolean path runs. The DI test covers both configurations with `ValidateScopes`.

</details>

<details>
<summary><strong>5. Review triggered by an incident — a service method, no automatic subscription</strong></summary>

- **Class**: Settled
- **Settled by**: AGENTS.md section 1 (every feature opt-in, pay-for-what-you-use) and ADR-050 (a module requires other modules only by the closed list).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TriggerReviewAsync(analysisId, reason)` appends `RiskReviewTriggered`; an application wires its own `INotificationHandler<NIS2IncidentDetected>` if it wants incidents to trigger reviews** | No coupling between the risk and incident modules; opt-in | The application writes a short handler |
| B) The risk module subscribes to incident events itself | Works out of the box | The risk module would require the incident module, which ADR-050 does not list |

### Chosen Option: **A**

### Rationale

- The issue's `RiskReviewTriggered` ("Periodic review due or triggered by incident") is covered by both the monitor (periodic) and the service method (incident); the docs show the three-line handler.

</details>

<details>
<summary><strong>6. Status set and transitions</strong></summary>

- **Class**: Trivial
- **Settled by**: the issue's `RiskAnalysisStatus` and event table; `DPIAAggregate` status handling.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Initiated -> RisksIdentified -> TreatmentPlanned -> TreatmentInProgress -> ReviewComplete -> Accepted`, with `RequiresReview` reachable from `ReviewComplete` and `Accepted`; `InProgress` dropped** | Each status follows from an event | One value fewer than the issue |
| B) Keep `InProgress` | Matches the issue | No event produces it |

### Chosen Option: **A**

### Rationale

- Rules: `RiskIdentified` from `Initiated`, `RisksIdentified`; `RiskTreatmentDecided` for an identified risk once; the status becomes `TreatmentPlanned` when every risk has a decision; `RiskMitigationImplemented` for a risk decided as mitigate (status `TreatmentInProgress` until all mitigate risks have evidence); `RiskAnalysisCompleted` when every risk is treated (and mitigated where decided); `RiskAnalysisAccepted` from `ReviewComplete`; `RiskReviewTriggered` and `RiskEscalated` from `ReviewComplete` or `Accepted`; either event sets the STATUS to `RequiresReview` (the event does not set a flag); a new cycle is a new aggregate. Idempotent triggering: the aggregate keeps `TriggeredForDueAtUtc` (the due date a `RiskReviewTriggered` was last raised for) and returns a `Left` with the `RiskAnalysisReviewAlreadyTriggered` rule code for a second trigger for the same due date, so the monitor treats it as already done.
- Illegal transitions are `Left` values with a rule code, never exceptions (ADR-053): commands return `Either<EncinaError, Unit>`, `Initiate` returns `Either<EncinaError, RiskAnalysisAggregate>`, decided before `RaiseEvent`. `DPIAAggregate` still throws only because [#2668](https://github.com/dlrivada/Encina/issues/2668) (the ADR-053 implementation and its shared rule-code catalogue) is open; this plan adds `RiskAnalysisInvalidTransition` to `NIS2Errors` and moves it onto the catalogue when #2668 lands.

</details>

<details>
<summary><strong>7. Evidence — references, not documents</strong></summary>

- **Class**: Trivial
- **Settled by**: the issue (Motivation: "proof that risk analysis was performed, by whom, with what methodology"); ADR-019 (immutable events).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Events carry an evidence reference (URI or document id) and a content hash (`EvidenceSha256`), never the document** | Small events, tamper evidence, nothing sensitive in the log | The store of the document is the application's |
| B) Embed the evidence | Self-contained | Large immutable events, cannot be erased |

### Chosen Option: **A**

### Rationale

- The hash lets an auditor check that the referenced document is the one recorded.

</details>

<details>
<summary><strong>8. Assessor and acceptor identity in the events</strong></summary>

- **Class**: Trivial
- **Settled by**: the #822 plan, Dilemma 6 (personal data in events).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `PerformedByUserId` and `AcceptedByUserId` as separate nullable properties, annotated as the shared decision requires; risk descriptions documented as personal-data-free** | Follows the family outcome | Free text may name people |
| B) Decide here | Independent | Contradicts the family decision |

### Chosen Option: **A**

### Rationale

- The aggregate keeps the identifier only (the issue's `PerformedBy` string is a user id, not a name).

</details>

<details>
<summary><strong>9. Storage, aggregate base, id and event delivery</strong></summary>

- **Class**: Settled
- **Settled by**: ADR-019 (and its 2026-10-16 amendment on Outbox delivery), ADR-027, ADR-050, AGENTS.md section 3 (no InMemory stores for event-sourced compliance modules); `src/Encina.DomainModeling/AggregateBase.cs`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Marten, `AggregateBase` with `Guid Id`, events implementing `INotification`** | Same as every compliance aggregate | PostgreSQL needed for tests |
| B) CRUD table with status | Simple | Mutable history, rejected by ADR-019 |

### Chosen Option: **A**

### Rationale

- Unit tests mock `IAggregateRepository<RiskAnalysisAggregate>`; integration tests use Marten on PostgreSQL through Testcontainers.

</details>

---

## Implementation Phases

### Pull request split (read first)

<details>
<summary>Tasks</summary>

55 file touches and about 5,450 lines, over the limit of 15 files and 1,500 lines per pull request (AGENTS.md section 11; the blocking `pr-size` job counts tests, PublicAPI, manifest and docs). Split in 5, counts including every file:

| PR | Phases | Files | Test files | Files / lines (approx.) |
|---|---|---|---|---|
| 1 | 1 (tasks 1 to 3) | five enums (`RiskAnalysisStatus`, `RiskSeverity`, `RiskLikelihood`, `RiskTreatment`, `RiskBand`), `IdentifiedRisk`, `RiskScoring`, `PublicAPI.Unshipped.txt`, manifest (9) | unit scoring, property scoring (2) | 11 files / 800 |
| 2 | 1 (tasks 4 to 6) | events, aggregate, `NIS2Errors` (transition codes), `PublicAPI.Unshipped.txt`, manifest (5) | unit aggregate, guard, property (3) | 8 files / 1,400 |
| 3 | 2, 3 | service interface, commands, default service, `NIS2Errors`, read model, register entry, projection, `PublicAPI.Unshipped.txt`, manifest (9) | unit service, unit projection, guard, contract (4) | 13 files / 1,400 |
| 4 | 4 | `NIS2MartenExtensions`, `ServiceCollectionExtensions`, `NIS2Options` and validator, `RiskAnalysisEvaluator`, `PublicAPI.Unshipped.txt`, manifest (7) | unit evaluator, DI test, integration (3) | 10 files / 900 |
| 5 | 5, 6, 8 | monitor, `NIS2ComplianceHealthCheck`, `NIS2Diagnostics`, `NIS2LogMessages`, `PublicAPI.Unshipped.txt`, manifest, changelog fragment, feature page, README, `docs/INVENTORY.md` (10) | unit monitor, unit health check, guard (3) | 13 files / 950 |

If a PR measures over 15 files or 1,500 lines, the worker splits it again; it never asks for `size-exception-approved`.

Prerequisite: PR 1 of the #822 plan is merged.

</details>

### Phase 1: Core Model, Events & Aggregate (PR 1: tasks 1 to 3; PR 2: tasks 4 to 6)

<details>
<summary>Tasks</summary>

1. `Model/RiskAnalysisStatus.cs`, `RiskSeverity.cs`, `RiskLikelihood.cs`, `RiskTreatment.cs` (`Accept`, `Mitigate`, `Transfer`, `Avoid`), `RiskBand.cs`.
2. `Model/IdentifiedRisk.cs`: record (`RiskId`, `Description`, severity, likelihood, treatment, `MitigationPlan`, residual severity and likelihood, `IdentifiedAtUtc`).
3. `Model/RiskScoring.cs` (internal static): `Score`, `Band`, and `NextReviewDue(highestResidualBand, completedAtUtc, RiskAnalysisOptions)`.
4. `Events/RiskAnalysisEvents.cs`: eight records implementing `INotification` (`RiskAnalysisInitiated`, `RiskIdentified`, `RiskTreatmentDecided`, `RiskMitigationImplemented`, `RiskAnalysisCompleted`, `RiskAnalysisAccepted`, `RiskReviewTriggered`, `RiskEscalated`) with timestamps, tenant and module ids; evidence fields per Design Choice 7; person ids separate nullable properties.
5. `Aggregates/RiskAnalysisAggregate.cs`: `AggregateBase`; static `Initiate(...)`; `IdentifyRisk`, `DecideTreatment`, `ImplementMitigation`, `Complete`, `AcceptResidualRisk`, `TriggerReview`, `Escalate`, each returning `Either<EncinaError, Unit>` (`Initiate` returns `Either<EncinaError, RiskAnalysisAggregate>`; ADR-053); `Apply` switch; properties of the issue plus `HighestResidualBand`, `EscalationCount`, `TriggeredForDueAtUtc`.
6. Tests: unit (each legal transition, each illegal one returns `Left` with the rule code and raises no event, scoring bands, review dates), guard, property (score in 1..25; the review date shortens with the band; a risk is decided once).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
You are implementing Phase 1 of the risk analysis lifecycle for Encina.Compliance.NIS2 (Issue #824).

CONTEXT:
- Encina is .NET 10 / C# 14; the aggregate takes timestamps as parameters (TimeProvider in the service).
- The EventId range 9800-9899, the Marten and DomainModeling references and AddNIS2EventSourcing come from the plan of issue 822; this phase uses ids 9850-9869 only.
- Pattern for structure, events and Apply: src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs and Events/DPIAEvents.cs; error style per docs/architecture/adr/053-aggregate-rule-violations-return-either.md (DPIAAggregate still throws until #2668; do not copy that).
- Two pull requests: PR 1 = enums, IdentifiedRisk, RiskScoring; PR 2 = events and aggregate.

TASK:
1. Create the enums, IdentifiedRisk, RiskScoring, the 8 events and RiskAnalysisAggregate (Design Choices 1 to 3, 6, 7).
2. Add PublicAPI.Unshipped.txt lines and XML docs.
3. Write unit, guard and property tests that instantiate the real aggregate.

KEY RULES:
- AggregateBase with Guid Id; no clock reads; commands return Either<EncinaError, Unit> (Initiate: Either<EncinaError, RiskAnalysisAggregate>); an illegal transition is a Left with the RiskAnalysisInvalidTransition rule code decided before RaiseEvent; never throw InvalidOperationException for a rule (ADR-053). A second RiskReviewTriggered for the same due date is a Left (TriggeredForDueAtUtc guard).
- Evidence is a reference plus a SHA-256 hash, never the document.
- Person ids are separate nullable properties; no [Obsolete]; zero warnings.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs
- src/Encina.Compliance.DPIA/DPIAOptions.cs
```

</details>

### Phase 2: Service, Errors & Default Implementation (PR 3)

<details>
<summary>Tasks</summary>

1. `Abstractions/INIS2RiskAnalysisService.cs`: `InitiateAnalysisAsync`, `IdentifyRiskAsync`, `DecideTreatmentAsync`, `ImplementMitigationAsync`, `CompleteAnalysisAsync`, `AcceptResidualRiskAsync`, `TriggerReviewAsync`, `GetOverdueReviewsAsync`, `GetRiskRegisterAsync`; each `ValueTask<Either<EncinaError, ...>>`. Commands in `Model/`.
2. `Services/DefaultNIS2RiskAnalysisService.cs` (internal): repository, read-model query, `TimeProvider`, `IOptions<NIS2Options>`, `ILogger`; validates scores and blank text; composes the aggregate's `Either` with `Bind` (no transition pre-check: the aggregate owns the state machine, ADR-053).
3. `NIS2Errors.cs`: `RiskAnalysisNotFound`, `RiskAnalysisCommandInvalid`, `RiskAnalysisStoreFailed` (`RiskAnalysisInvalidTransition` and `RiskAnalysisReviewAlreadyTriggered` came with the aggregate in PR 2).
4. Missing tenant or assessor identity where required is denied (issue Alternatives 10).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
You are implementing Phase 2 of the risk analysis lifecycle for Encina.Compliance.NIS2 (Issue #824).

CONTEXT:
- Phase 1 delivered RiskAnalysisAggregate. Pattern: src/Encina.Compliance.DPIA/Services/DefaultDPIAService.cs.

TASK:
1. Create INIS2RiskAnalysisService, the commands and the internal default implementation.
2. Add the NIS2Errors factories; out-of-range scores are Left, never exceptions.
3. Unit tests with NSubstitute for IAggregateRepository; guard tests; a contract test.

KEY RULES:
- ROP; log error codes, never EncinaError.Message; exceptions through ForLogging().
- Do not wrap repository calls in NIS2ResilienceHelper (it returns a fallback on failure).

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Services/DefaultDPIAService.cs
- src/Encina.Compliance.NIS2/NIS2Errors.cs
```

</details>

### Phase 3: Projection, Risk Register & Trend (PR 3)

<details>
<summary>Tasks</summary>

1. `ReadModels/RiskAnalysisReadModel.cs`: analysis id, status, scope, methodology, `NextReviewDueAtUtc`, `HighestResidualBand`, tenant and module ids, the list of `RiskPostureSnapshot` (score and band at each completion for the trend).
2. `ReadModels/RiskRegisterEntry.cs`: one row per risk (tenant, risk id, analysis id, severity, likelihood, residual band, treatment, status).
3. `ReadModels/RiskAnalysisProjection.cs`: inline projection for the eight events, idempotent, constructor-injected dependencies only.
4. Queries: overdue reviews and the register filtered by band.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
You are implementing Phase 3 of the risk analysis lifecycle for Encina.Compliance.NIS2 (Issue #824).

CONTEXT:
- Mirror src/Encina.Compliance.DPIA/ReadModels/DPIAProjection.cs.

TASK:
1. Create the read model, the register entry and the projection handling the 8 events.
2. Add the overdue and register queries.
3. Unit-test every handler; keep one integration test that registers the projection with a real Marten store.

KEY RULES:
- Idempotent projection; AtUtc timestamps; dependencies by constructor, never IServiceProvider.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/ReadModels/DPIAProjection.cs
```

</details>

### Phase 4: Configuration, DI & Evaluator Integration (PR 4)

<details>
<summary>Tasks</summary>

1. `NIS2MartenExtensions` (from the #822 plan): add `AddAggregateRepository<RiskAnalysisAggregate>()` and `AddProjection<RiskAnalysisProjection, RiskAnalysisReadModel>()`.
2. `ServiceCollectionExtensions.cs`: `TryAddScoped<INIS2RiskAnalysisService, ...>` with its dependencies.
3. `NIS2Options`: `RiskAnalysisOptions` (`ReviewPeriods` per band, band thresholds, `EnableRiskReviewMonitoring`, `RiskReviewCheckInterval` default 1 hour); extend `NIS2OptionsValidator`.
4. Design Choice 4: `RiskAnalysisEvaluator` (a singleton) uses the register when the service is registered, resolving it in a scope as Design Choice 4 describes, otherwise unchanged; GDPR alignment details kept in both paths.
5. DI test with `ValidateOnBuild` and `ValidateScopes`, with and without the workflow.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
You are implementing Phase 4 of the risk analysis lifecycle for Encina.Compliance.NIS2 (Issue #824).

CONTEXT:
- AddNIS2EventSourcing exists (plan of issue 822); RiskAnalysisEvaluator is in src/Encina.Compliance.NIS2/Evaluators.

TASK:
1. Register the aggregate, projection and service; add and validate the options.
2. Make RiskAnalysisEvaluator use the register when the service is registered, otherwise unchanged.
3. Write the DI test (ValidateOnBuild, ValidateScopes) for both configurations.

KEY RULES:
- TryAdd for every default; opt-in; PublicAPI.Unshipped.txt for new public members.
- Design Choice 4 is recommended, pending the maintainer: keep the fallback path small and isolated.

REFERENCE FILES:
- src/Encina.Compliance.NIS2/Evaluators/RiskAnalysisEvaluator.cs
- src/Encina.Compliance.NIS2/ServiceCollectionExtensions.cs
- src/Encina.Compliance.DPIA/DPIAMartenExtensions.cs
```

</details>

### Phase 5: Cross-Cutting Integration, Review Monitor & Health Check (PR 5)

<details>
<summary>Tasks</summary>

1. `NIS2RiskReviewMonitorService.cs` (`BackgroundService`, `PeriodicTimer`, scope per iteration): for each overdue analysis append `RiskReviewTriggered` once per due date; per-iteration `try/catch`; failures logged by error code.
2. `Health/NIS2ComplianceHealthCheck.cs`: add the risk analysis check (overdue review or no accepted analysis gives `Degraded`) behind `AddHealthCheck`.
3. Tenant and module scoping on queries; the event stream is the audit record.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
You are implementing Phase 5 of the risk analysis lifecycle for Encina.Compliance.NIS2 (Issue #824).

CONTEXT:
- Pattern: src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs and src/Encina.Compliance.NIS2/Health/NIS2ComplianceHealthCheck.cs.

TASK:
1. Create the monitor and register it only when EnableRiskReviewMonitoring is true.
2. Extend the health check.
3. Tests with FakeTimeProvider and a throwing store: the loop survives and the failure is logged.

KEY RULES:
- A PeriodicTimer loop catches per iteration; OperationCanceledException from the stopping token ends the loop (AGENTS.md section 3).
- Triggering is idempotent per due date.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs
- src/Encina.Compliance.NIS2/Health/NIS2ComplianceHealthCheck.cs
```

</details>

### Phase 6: Observability (PR 5)

<details>
<summary>Tasks</summary>

1. `NIS2Diagnostics`: `StartRiskAnalysisOperation(...)` activity, instruments `nis2.risk_analysis.operations.total` (Counter, tags operation and outcome), `nis2.risk_analysis.risks.total` (Counter, tag band), `nis2.risk_analysis.overdue_reviews` (ObservableGauge from the last monitor sweep).
2. `NIS2LogMessages`: `[LoggerMessage]` ids 9850-9869 packed from 9850 (one per transition, invalid transition, store failure, monitor started/stopped/iteration failed, review overdue).
3. Run the EventId architecture tests.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
You are implementing Phase 6 of the risk analysis lifecycle for Encina.Compliance.NIS2 (Issue #824).

CONTEXT:
- Phases 1 to 5 delivered the domain, service, projection and monitor; this phase instruments them.

TASK:
1. Add the activity, instruments and logger messages with EventIds 9850-9869 packed sequentially.
2. Record metrics in the service and monitor.
3. Run the EventId architecture tests.

KEY RULES:
- Range registered by the #822 plan; [LoggerMessage] source generator; no sparse ids.
- Metric tags are low cardinality: operation, outcome, band; never the analysis or risk id.

REFERENCE FILES:
- src/Encina.Compliance.NIS2/Diagnostics/NIS2Diagnostics.cs
- src/Encina.Compliance.NIS2/Diagnostics/NIS2LogMessages.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 7: Testing (PRs 1 to 5)

<details>
<summary>Tasks</summary>

| Type | Scope | Location |
|---|---|---|
| Unit | Aggregate transitions and replay, scoring and review calculator, service with a substituted repository, projection handlers, monitor, health check, evaluator with and without the register | `tests/Encina.UnitTests/Compliance/NIS2/` |
| Guard | Public methods and constructors | `tests/Encina.GuardTests/Compliance/NIS2/` |
| Contract | `INIS2RiskAnalysisService` and the aggregate commands: illegal transitions (rule code `RiskAnalysisInvalidTransition`) and out-of-range scores return `Left`, raise no event and never throw | `tests/Encina.ContractTests/Compliance/NIS2/` |
| Property | Score 1 to 25, band monotonic, review date shortens with the band, replay equals live state | `tests/Encina.PropertyTests/Compliance/NIS2/` |
| Integration | Full lifecycle on Marten and PostgreSQL through the shared Testcontainers fixture; register and trend projections with a real store | `tests/Encina.IntegrationTests/Compliance/NIS2/` |
| Load / Benchmark | Not implemented: low volume, no hot path; `.md` justification files | `tests/Encina.LoadTests`, `tests/Encina.BenchmarkTests` |

Per-file per-flag targets with one-sentence justifications go into `.github/coverage-manifest/Encina.Compliance.NIS2.json`; targets only go up. CRAP of new or changed methods is 10 or less; the aggregate `Apply` switch carries the `crap-exempt` marker.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
You are implementing Phase 7 (tests) of the risk analysis lifecycle for Encina.Compliance.NIS2 (Issue #824).

CONTEXT:
- Consolidated test projects under tests/, folder Compliance/NIS2.

TASK:
1. Write unit, guard, contract, property and integration tests as listed, per pull request for the code it adds.
2. Integration tests use a shared [Collection] fixture for PostgreSQL and clear data in InitializeAsync.
3. Add manifest targets per new file and run the per-flag coverage commands of AGENTS.md section 9.

KEY RULES:
- Tests execute real package code; Shouldly through Encina.Testing.Shouldly.
- A test never swallows an exception thrown by the code it drives.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Compliance/BreachNotification/BreachNotificationAggregateIntegrationTests.cs
- .github/coverage-manifest/Encina.Compliance.NIS2.json
```

</details>

### Phase 8: Documentation & Finalization (PR 5)

<details>
<summary>Tasks</summary>

- XML docs on every new public API.
- `changelog.d/824-nis2-risk-analysis.added.md`; never edit `CHANGELOG.md`.
- `PublicAPI.Unshipped.txt` for `Encina.Compliance.NIS2`.
- `docs/features/nis2-compliance.md` (risk analysis section, including the incident-handler example of Design Choice 5), `src/Encina.Compliance.NIS2/README.md`, `docs/INVENTORY.md` via `docs-writer`.
- No ADR: the design follows ADR-019 and ADR-064 of the #822 plan.
- Verification: `dotnet build --configuration Release` with 0 warnings; the five NIS2 test folders pass; every coverage flag reaches its manifest target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
You are finishing the risk analysis lifecycle for Encina.Compliance.NIS2 (Issue #824).

CONTEXT:
- Code and tests are done; this phase closes documentation, public API tracking and verification.

TASK:
1. Complete XML docs and PublicAPI.Unshipped.txt entries.
2. Add the changelog fragment changelog.d/824-nis2-risk-analysis.added.md.
3. Update docs/features/nis2-compliance.md and the package README (docs-writer).
4. Build and test; report per-flag coverage of every new file against its manifest target.

KEY RULES:
- Documentation never types coverage figures by hand (covref markers).
- Do not edit CHANGELOG.md.

REFERENCE FILES:
- changelog.d/README.md
- docs/features/nis2-compliance.md
```

</details>

---

## Research

### NIS2 Article References

| Article | Requirement | Applies in this plan |
| :--- | :--- | :--- |
| Art. 21(1) | Appropriate and proportionate measures to manage risks to network and information systems, all-hazards approach | Frame of the workflow |
| Art. 21(2)(a) | Policies on risk analysis and information system security | The analysis lifecycle and register |
| Art. 21(2)(f) | Policies and procedures to assess the effectiveness of risk-management measures | Mitigation evidence, residual risk, review |
| Art. 20(1) | Management bodies approve and oversee the measures | `RiskAnalysisAccepted` by management |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| AggregateBase (Guid id, RaiseEvent, Apply) | src/Encina.DomainModeling/AggregateBase.cs | Base class of the new aggregate |
| IAggregateRepository<TAggregate> | src/Encina.Marten | Load, save, create of event-sourced aggregates |
| DPIAAggregate, DPIAEvents, DPIAProjection | src/Encina.Compliance.DPIA | Reference pattern for risk assessment aggregate, events and projection |
| DPIAReviewReminderService and DPIAOptions (DefaultReviewPeriod 365 days) | src/Encina.Compliance.DPIA | Reference pattern for the opt-in review hosted service and review period option |
| DPIAMartenExtensions (AddDPIAAggregates) | src/Encina.Compliance.DPIA | Reference pattern for aggregate registration |
| RiskAnalysisEvaluator | src/Encina.Compliance.NIS2/Evaluators | Current verdict from NIS2Options.HasRiskAnalysisPolicy plus GDPR alignment details |
| NIS2Options.HasRiskAnalysisPolicy | src/Encina.Compliance.NIS2/NIS2Options.cs | Boolean kept when the workflow is not registered |
| NIS2Diagnostics and NIS2LogMessages | src/Encina.Compliance.NIS2/Diagnostics | ActivitySource, Meter, logger messages |
| NIS2ComplianceHealthCheck | src/Encina.Compliance.NIS2/Health | Health check pattern |
| AddNIS2EventSourcing and range 9800-9899 | Plan of issue 822 | Shared pieces owned by 822 |

### Event ID Allocation

| Package | Range | Notes |
| :--- | :--- | :--- |
| Encina.Compliance.NIS2 | 9200-9299 | Existing; 9288-9299 unused |
| Encina.Compliance.NIS2 (event-sourced modules) | 9800-9899 | Range `ComplianceNIS2EventSourced` registered by the plan of issue 822 |
| Issue 824 risk analysis | 9850-9869 | 20 ids, this plan |

### File Count Estimate

Per pull request, tests and metadata included (same figures as the split table; PublicAPI, manifest and `NIS2Errors` are touched in several PRs and count in each):

| PR | Files | Approx. lines |
| :--- | :--- | :--- |
| 1 Enums, IdentifiedRisk, scoring | 11 | 800 |
| 2 Events and aggregate | 8 | 1,400 |
| 3 Service, projection, register | 13 | 1,400 |
| 4 Registration, options, evaluator | 10 | 900 |
| 5 Monitor, health check, observability, docs | 13 | 950 |
| Total | 55 | 5,450 |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library (pre-1.0, no backward compatibility). You extend src/Encina.Compliance.NIS2 with an event-sourced risk analysis lifecycle (issue #824, NIS2 Art. 21(2)(a)). The store is Marten on PostgreSQL. Read AGENTS.md first. The EventId range, project references and AddNIS2EventSourcing come from the plan of issue 822; the evaluator opt-in decision from the plan of issue 823.

IMPLEMENTATION OVERVIEW:
1. Model: status, severity, likelihood, treatment and band enums, IdentifiedRisk, RiskScoring (1..25 score, bands, review date), commands.
2. Eight events (INotification) and RiskAnalysisAggregate (AggregateBase, one stream per analysis cycle).
3. INIS2RiskAnalysisService + internal default over IAggregateRepository; NIS2Errors factories.
4. RiskAnalysisProjection with RiskAnalysisReadModel, RiskRegisterEntry and posture snapshots.
5. Registration lines in AddNIS2EventSourcing, options, RiskAnalysisEvaluator reads the register when registered.
6. NIS2RiskReviewMonitorService (opt-in) and the health check addition.
7. Diagnostics and [LoggerMessage] ids 9850-9869.
8. Unit, guard, contract, property, integration tests; load and benchmark justifications.
9. Docs, PublicAPI, changelog fragment, coverage manifest. Deliver in the 5 pull requests of the split.

KEY PATTERNS:
- Mirror DPIAAggregate, DPIAEvents, DPIAProjection, DPIAReviewReminderService, DPIAMartenExtensions.
- ROP (ADR-053): aggregate commands return Either, an illegal transition is a Left with a rule code, never InvalidOperationException; the singleton evaluator resolves the scoped service in a scope; EncinaError.Message never reaches logs; TimeProvider for time; PeriodicTimer loop catches per iteration.
- NIS2ResilienceHelper returns a fallback on failure: never wrap store calls in it.
- Design Choices 2, 3 and 4 are Dilemmas recommended, pending the maintainer: bands, review periods and the evaluator fallback path are options or isolated code.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs
- src/Encina.Compliance.NIS2/Evaluators/RiskAnalysisEvaluator.cs
- docs/architecture/adr/019-compliance-event-sourcing-marten.md, 050-modules-may-require-modules.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Reads are inline-projection lookups, and a cached verdict could report an accepted analysis after a review was triggered; the issue's follow-up issue is not needed |
| 2 | OpenTelemetry | ✅ | Phase 6: activity per operation, `nis2.risk_analysis.*` instruments on the package meter |
| 3 | Structured Logging | ✅ | Phase 6: `[LoggerMessage]` ids 9850-9869 of the range registered by the #822 plan |
| 4 | Health Checks | ✅ | Phase 5: overdue review or no accepted analysis gives Degraded |
| 5 | Validation | ✅ | Commands validated in the service (blank text, severity and likelihood bounds, duplicate risk ids) before any event |
| 6 | Resilience | ❌ | No external call; store calls are not wrapped in `NIS2ResilienceHelper`, which swallows failures |
| 7 | Distributed Locks | ⏭️ | Triggering is idempotent per due date and Marten optimistic concurrency protects duplicates; multi-instance coordination is deferred to [#1251](https://github.com/dlrivada/Encina/issues/1251), the issue ADR-050 names for it |
| 8 | Transactions | ✅ | The Marten session commits the event append atomically; no `TransactionPipelineBehavior` (ADR-050) |
| 9 | Idempotency | ✅ | Aggregate version, a risk decided once, the per-due-date trigger guard |
| 10 | Multi-Tenancy | ✅ | `TenantId` in events, aggregate and read models |
| 11 | Module Isolation | ✅ | `ModuleId` on events and read models, as `DPIAAggregate`; the issue left the decision to the plan |
| 12 | Audit Trail | ✅ | The event stream is the audit trail; person identity follows the shared personal-data decision |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|---|---|---|
| #822 plan, PR 1: range 9800-9899, references, `AddNIS2EventSourcing` | Planned | PR 1 of this plan depends on it |
| #823 plan, Design Choice 3 (evaluators and the static configuration) | Pending the maintainer | Design Choice 4 follows its outcome |
| Personal-data decision (Dilemma 6 of the #822 plan) | Pending the maintainer | Person ids stay separate nullable properties until decided |
| #1603 NIS2 reads the system clock instead of `TimeProvider` | Open | New code takes `TimeProvider`; the old code is not touched here |

---

## Next Steps

1. Maintainer answers the Dilemmas of Design Choices 2 (scoring bands), 3 (review periods) and 4 (evaluator, shared with #823, Design Choice 3) when work on #824 starts; each is marked "recommended, pending the maintainer".
2. Brief PR 1 after the #822 PR 1 merges; PR 2 to 5 follow in order.
3. Run `check-plan.ps1` without `-Draft` after the adversarial review sets the `Verified` lines.
