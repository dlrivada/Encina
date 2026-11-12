# Implementation Plan: `Encina.Compliance.NIS2` — Management Accountability Lifecycle (Art. 20)

> **Issue**: [#826](https://github.com/dlrivada/Encina/issues/826)
> **Type**: Feature
> **Complexity**: Medium (event-sourced aggregate, overdue monitor, 3 pull requests)
> **Estimated Scope**: ~2,000 lines of production code + ~1,570 lines of tests (about 28 files; split into 3 pull requests)
> **Milestone**: v0.15.0 — EU Compliance: NIS2 & Digital Omnibus
> **Provider Category**: None for databases (single event-sourcing provider: Marten on PostgreSQL, ADR-027)
> **Family**: #822 to #826. Shared pieces are owned by the plan of #822 (`nis2-incident-lifecycle-implementation-plan-822.md`); the opt-in decision for evaluators is owned by the plan of #823 (`nis2-supply-chain-assessment-implementation-plan-823.md`, Design Choice 3).

---

## Summary

`ManagementAccountabilityRecord` (held in `NIS2Options.ManagementAccountability`) is a snapshot: a responsible person, a role, one acknowledgment date, a list of compliance areas and an optional training date. Its only consumer is `CyberHygieneEvaluator`, which checks that the record exists and that a training date is set. Nothing records that training expires, that the acknowledgment must be renewed when measures change, who holds which compliance area, or what oversight was exercised.

This plan adds an event-sourced **management accountability lifecycle** in the style of the GDPR DPIA module:

- `ManagementAccountabilityAggregate` (an `AggregateBase`), one stream per appointed responsible person, with training, acknowledgment, delegations and oversight evidence.
- `INIS2ManagementAccountabilityService`, an inline Marten projection (the overdue training and acknowledgment dashboard), an opt-in overdue monitor, a health check addition and `nis2.management_accountability.*` metrics.
- Validity periods for training and acknowledgment as options.

**Standards**: NIS2 Directive (EU) 2022/2555 Art. 20(1) (management approves, oversees and is liable) and Art. 20(2) (management training). **Not in scope**: employee training programmes (Design Choice 6), identity or HR system integration, changes to the incident, supplier or risk modules.

**Affected packages**: `Encina.Compliance.NIS2` (extended). The `Encina` core, the `Encina.Marten` and `Encina.DomainModeling` references, the EventId range and the registration entry point come from the #822 plan.

**Provider category**: none of the 10 database providers; event sourcing (Marten) is excluded from the database matrix by AGENTS.md section 5.

### Corrections to the issue text (checked against today's code)

| Issue text | Today's code | Plan decision |
|---|---|---|
| `ManagementAccountabilityStatus` has `Appointed` | `ManagementPersonAppointed` leaves the person without valid training or acknowledgment, so the status after it is `TrainingRequired`; `Appointed` is unreachable | `Appointed` is dropped (Design Choice 2) |
| Eight events | A delegation can be granted but not withdrawn | A ninth event `ComplianceDelegationRevoked` (Design Choice 7) |
| `ComplianceAreas` as strings in the existing record | `NIS2Measure` already lists the 10 Art. 21(2) measures | Areas stay strings (they may name areas beyond the measures); the docs recommend `NIS2Measure` names |
| Employee training "listed as missing but not in the aggregate" | Art. 20(2) obliges Member States to require management training and to encourage employee training | Out of scope; a follow-up issue file covers Art. 21(2)(g) (Design Choice 6) |
| Metrics `encina.nis2.management_accountability.*` | The package meter `Encina.Compliance.NIS2` names instruments `nis2.*` | `nis2.management_accountability.*` |

### Shared pieces

| Piece | Owner | What this plan does |
|---|---|---|
| EventId range `ComplianceNIS2EventSourced` (9800-9899) | #822 plan | Allocates **9885-9899** (15 ids) |
| Project references and `AddNIS2EventSourcing()` | #822 plan, PR 1 and Phase 4 | Depends on them; adds the repository and projection lines |
| ADR-064 | #822 plan | Referenced |
| Personal data in events (`ResponsiblePerson`, delegates) | Dilemma 6 of the #822 plan | Applied in Design Choice 8 |
| Evaluators read the register when the workflow is registered | Dilemma 3 of the #823 plan | Applied in Design Choice 5 |

---

## Design Choices

<details>
<summary><strong>1. Aggregate granularity — one stream per appointed responsible person</strong></summary>

- **Class**: Trivial
- **Settled by**: the issue's Proposed Solution (`ManagementAccountabilityAggregate` with `ResponsiblePerson`, `Delegations`, `TrainingHistory`); the convention of one aggregate per tracked subject in `DPIAAggregate`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One aggregate per appointment (random `Guid`); the person is a property** | Training, acknowledgment and delegations of one person stay together; revocation closes it | Two appointments of the same person (re-appointment) are two streams |
| B) One aggregate for the whole management body | One place | Concurrency across persons, no per-person revocation |

### Chosen Option: **A**

### Rationale

- A re-appointment after a revocation is a new stream; the projection groups by person id for the overview.

</details>

<details>
<summary><strong>2. Status track — training and acknowledgment both valid means Active</strong></summary>

- **Class**: Trivial
- **Settled by**: the issue's `ManagementAccountabilityStatus` and event table; `DPIAAggregate` status handling.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Statuses `TrainingRequired, TrainingCompleted, Acknowledged, Active, AcknowledgmentOverdue, TrainingOverdue, Revoked`; the aggregate also keeps `TrainingValidUntilUtc` and `NextAcknowledgmentDueAtUtc`** | Every status follows from an event; both overdue conditions are readable from the dates | `Appointed` dropped |
| B) Keep `Appointed` | Matches the issue | Unreachable |

### Chosen Option: **A**

### Rationale

- Rules: the appointment event sets `TrainingRequired`; `TrainingCompleted` sets `TrainingCompleted` unless the acknowledgment is already valid, then `Active`; `AcknowledgmentRecorded` sets `Acknowledged` unless training is already valid, then `Active`; when both overdue conditions hold the status is `TrainingOverdue` (training takes precedence); a renewal that restores the missing validity returns the status to `Active` or the other overdue status; `Revoked` is final and accepts no further event.

</details>

<details>
<summary><strong>3. Validity periods — 365 days, options</strong></summary>

- **Class**: Trivial
- **Settled by**: `DPIAOptions.DefaultReviewPeriod` (365 days) as the precedent; NIS2 Art. 20(2) says only "on a regular basis".

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TrainingValidity` 365 days and `AcknowledgmentRenewalPeriod` 365 days as options** | Annual cycle, configurable | A convention |
| B) No expiry | Simple | Training never lapses, the gap the issue names |

### Chosen Option: **A**

### Rationale

- The due dates are stored on the events (`TrainingValidUntilUtc`, `NextAcknowledgmentDueAtUtc`), so a later option change does not rewrite history. A measure change can require an earlier re-acknowledgment: `RecordAcknowledgmentAsync` may be called any time, and the overdue monitor uses the stored date.

</details>

<details>
<summary><strong>4. Overdue detection — hosted service with idempotent escalation</strong></summary>

- **Class**: Trivial
- **Convention followed**: issue #826 Alternatives Considered 8; `src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs` (opt-in through `DPIAOptions.EnableExpirationMonitoring`, `PeriodicTimer`); AGENTS.md section 3 (a `PeriodicTimer` loop catches per iteration; cancellation ends it). Option B (compute overdue on read) also complies with AGENTS.md section 3 and ADR-050, so the choice rests on the DPIA precedent and the issue, not on a rule.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `NIS2AccountabilityMonitorService` raises `TrainingOverdueEscalated` and `AcknowledgmentOverdueEscalated` once per due date** | Detects a lapse when nobody calls the service; feeds the gauge and health check | Needs duplicate protection with several instances |
| B) Compute overdue on read only | No background service | No escalation event, no audit of the lapse |

### Chosen Option: **A**

### Rationale

- Duplicate protection without a lock: the aggregate remembers the due date it escalated for (`LastEscalatedDueDate`, a field set by `Apply` from the escalation events, so replay keeps it) and an escalation for the same due date returns a `Left` rule code; Marten optimistic concurrency turns a race into a handled `Left`. A failed query or append is logged by error code and the loop continues. Registered only when `NIS2Options.EnableAccountabilityMonitoring` is true.

</details>

<details>
<summary><strong>5. Relation to the static record and the evaluator</strong></summary>

- **Class**: Dilemma

The question and its options belong to the family decision in the #823 plan (Design Choice 3); this plan applies it.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) When the workflow is registered, `CyberHygieneEvaluator` is satisfied only if at least one responsible person is `Active` and none is overdue; otherwise it keeps reading `NIS2Options.ManagementAccountability`** | Opt-in; the register becomes the evidence | Two paths until a later decision removes one |
| B) Remove `ManagementAccountabilityRecord` and the option; the register is the only source | One source | Every NIS2 user needs Marten |

### Chosen Option: **A (recommended, pending the maintainer; follows the #823 plan, Design Choice 3)**

### Rationale

- A is better while stateless users exist; B wins if NIS2 is declared Marten-only for 1.0. The cyber hygiene programme flag (`HasCyberHygieneProgram`) is still required in both paths.
- The evaluator reads the register through `INIS2ManagementAccountabilityService`, resolved optionally.

</details>

<details>
<summary><strong>6. Employee training programmes — out of scope, with a follow-up issue</strong></summary>

- **Class**: Trivial
- **Convention followed**: NIS2 Directive Art. 20(2) (Member States require management training and encourage entities to offer similar training to employees) and Art. 21(2)(g) (cyber hygiene and cybersecurity training as an entity measure, already covered by `HumanResourcesSecurityEvaluator` and `CyberHygieneEvaluator`); issue #826 Implementation Tasks (the plan decides). Art. 20(2) does not forbid option B; the reason for A is scope.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Management accountability covers management members only; employee training tracking is a separate feature under Art. 21(2)(g), drafted as `artifacts/issues/nis2-employee-training-programme.md`** | Matches the article; keeps this pull request set small | A second workflow later |
| B) Add employee training to this aggregate | One module | Employees are many, change often and belong to a different legal duty; the aggregate would grow into an HR system |

### Chosen Option: **A**

### Rationale

- The training record type (`TrainingRecord`: topic, date, evidence reference and hash, valid until) is written so the follow-up can reuse it.

</details>

<details>
<summary><strong>7. Delegations and oversight evidence</strong></summary>

- **Class**: Trivial
- **Settled by**: the issue's `ComplianceAreaDelegated` and `OversightActivityRecorded`; the evidence convention of the #824 plan (Design Choice 7: reference plus SHA-256 hash).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ComplianceDelegation(Area, DelegateId, DelegatedAtUtc)` entries; `ComplianceDelegationRevoked` withdraws one; oversight events carry a type (`Meeting`, `Review`, `Audit`), a date and an evidence reference with hash** | A delegation has a lifecycle; evidence is small and checkable | One event more than the issue |
| B) Delegations never withdrawn | Fewer events | A delegation to a person who left stays forever |

### Chosen Option: **A**

### Rationale

- `Area` is a string (as in the existing record) and must not be blank; the docs recommend the `NIS2Measure` names.

</details>

<details>
<summary><strong>8. Person identity in the events</strong></summary>

- **Class**: Dilemma

The question belongs to Dilemma 6 of the #822 plan (personal data in append-only Marten streams); this plan applies its outcome.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ResponsiblePersonId` and `DelegateId` are user ids in separate nullable properties, annotated as the shared decision requires; the display name and role are separate properties too** | Follows the family outcome; the shape is ready for either outcome of Dilemma 6 | The accountable manager (a natural person) is stored in an immutable stream |
| B) Events carry only an opaque appointment key; the person's identity lives in a mutable directory outside the stream | Erasure is a directory delete, the stream stays clean | A second store, and the proof of who was accountable depends on a mutable record, which weakens Art. 20(1) evidence |

### Chosen Option: **A (recommended, pending the maintainer; follows Dilemma 6 of the #822 plan)**

### Rationale

- This module is the strongest case for the shared decision: the data subject is the accountable manager, and the record exists to prove accountability under Art. 20(1), which argues for retention.
- Cost in the losing scenario: if the maintainer later picks erasure for Dilemma 6, an erasure request against an already written stream cannot be met without crypto-shredding the separate person properties (`Encina.Marten.GDPR`) or a stream rewrite; keeping the properties separate and nullable is what keeps that door open.

</details>

<details>
<summary><strong>9. Storage, aggregate base, id and event delivery</strong></summary>

- **Class**: Settled
- **Settled by**: ADR-019 (and its 2026-10-16 amendment on Outbox delivery), ADR-027, ADR-050, AGENTS.md section 3 (no InMemory stores for event-sourced compliance modules); `src/Encina.DomainModeling/AggregateBase.cs`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Marten, `AggregateBase` with `Guid Id`, events implementing `INotification`** | Same as every compliance aggregate | PostgreSQL needed for tests |
| B) CRUD entity plus audit log | Simple | Mutable history, rejected by ADR-019 |

### Chosen Option: **A**

### Rationale

- Unit tests mock `IAggregateRepository<ManagementAccountabilityAggregate>`; integration tests use Marten on PostgreSQL through Testcontainers.

</details>

---

## Implementation Phases

### Pull request split (read first)

<details>
<summary>Tasks</summary>

About 28 files, over the limit of 15 files and 1,500 lines (AGENTS.md section 11). Split:

| PR | Phases | Content | Files / lines (approx.) |
|---|---|---|---|
| 1 | 1 | Model, schedule calculator, events, aggregate, aggregate rule codes in `NIS2Errors`; unit, guard and property tests | 11 files / 1,380 |
| 2 | 2, 3, 4 | Service and errors, projection, registration, options, evaluator integration, contract and integration tests | 10 files / 1,350 |
| 3 | 5, 6, 7 (rest), 8 | Overdue monitor, health check, diagnostics, remaining tests, docs, manifest | 8 files / 900 |

Prerequisite: the PR 1 of the #822 plan is merged.

</details>

### Phase 1: Core Model, Events & Aggregate (PR 1)

<details>
<summary>Tasks</summary>

1. `Model/ManagementAccountabilityStatus.cs` (Design Choice 2), `Model/OversightActivityType.cs` (`Meeting`, `Review`, `Audit`).
2. `Model/ComplianceDelegation.cs` and `Model/TrainingRecord.cs` records.
3. `Model/AccountabilityScheduler.cs` (internal static): `TrainingValidUntil(completedAtUtc, options)`, `NextAcknowledgmentDue(acknowledgedAtUtc, options)`.
4. `Events/ManagementAccountabilityEvents.cs`: nine records implementing `INotification` (`ManagementPersonAppointed`, `ComplianceAreaDelegated`, `ComplianceDelegationRevoked`, `TrainingCompleted`, `AcknowledgmentRecorded`, `OversightActivityRecorded`, `TrainingOverdueEscalated`, `AcknowledgmentOverdueEscalated`, `ManagementPersonRevoked`) with timestamps, tenant and module ids; person ids separate nullable properties.
5. `Aggregates/ManagementAccountabilityAggregate.cs`: `AggregateBase`; static `Appoint(...)` returning `Either<EncinaError, ManagementAccountabilityAggregate>`; `Delegate`, `RevokeDelegation`, `CompleteTraining`, `Acknowledge`, `RecordOversight`, `EscalateTrainingOverdue`, `EscalateAcknowledgmentOverdue`, `Revoke` returning `Either<EncinaError, Unit>` (ADR-053): the rule is decided and the `Left` returned before `RaiseEvent`, so an illegal transition or a repeated escalation never throws and never raises an event; `ArgumentNullException.ThrowIfNull` and similar argument guards stay as exceptions. `Apply` switch; properties of the issue plus the two due dates and `LastEscalatedDueDate` (an event field, so replay keeps the escalation guard).
6. `NIS2Errors.cs`: the aggregate rule codes `ManagementAccountabilityInvalidTransition` (including any command on a `Revoked` stream), `ManagementAccountabilityEscalationAlreadyRecorded`, `ManagementAccountabilityDelegationDuplicate` and `ManagementAccountabilityDelegationNotFound`, as `nis2.management_accountability_*` codes in the existing style.
7. Tests: unit (status rules, both overdue conditions, renewal restores, revoked is final, every rule path returns the expected `Left` code and raises no event), guard, property (Active implies both dates in the future; revoked accepts nothing).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
You are implementing Phase 1 of the management accountability lifecycle for Encina.Compliance.NIS2 (Issue #826).

CONTEXT:
- Encina is .NET 10 / C# 14; the aggregate takes timestamps as parameters (TimeProvider in the service).
- The EventId range 9800-9899, the Marten and DomainModeling references and AddNIS2EventSourcing come from the plan of issue 822; this phase uses ids 9885-9899 only.
- Pattern: src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs; existing snapshot: src/Encina.Compliance.NIS2/Model/ManagementAccountabilityRecord.cs.

TASK:
1. Create the enums, records, scheduler, the 9 events and ManagementAccountabilityAggregate (Design Choices 1, 2, 3, 7), and the aggregate rule codes in NIS2Errors.
2. Add PublicAPI.Unshipped.txt lines and XML docs.
3. Write unit, guard and property tests that instantiate the real aggregate.

KEY RULES:
- AggregateBase with Guid Id; no clock reads; commands return Either<EncinaError, Unit> and the factory Either<EncinaError, ManagementAccountabilityAggregate> (ADR-053): an illegal transition returns a Left with a NIS2Errors code decided BEFORE RaiseEvent; never throw for a business rule. Argument guards (ThrowIfNull) are the only exceptions.
- DPIAAggregate throws InvalidOperationException on rule paths: do NOT copy that part of the pattern.
- Due dates are stored on the events, not recomputed from options on replay.
- Person ids are separate nullable properties; no [Obsolete]; zero warnings.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs
- src/Encina.Compliance.NIS2/Model/ManagementAccountabilityRecord.cs
```

</details>

### Phase 2: Service, Errors & Default Implementation (PR 2)

<details>
<summary>Tasks</summary>

1. `Abstractions/INIS2ManagementAccountabilityService.cs`: `AppointResponsiblePersonAsync`, `RecordTrainingAsync`, `RecordAcknowledgmentAsync`, `DelegateComplianceAreaAsync`, `RevokeDelegationAsync`, `RecordOversightActivityAsync`, `RevokeResponsiblePersonAsync`, `GetOverdueTrainingAsync`, `GetOverviewAsync`; each `ValueTask<Either<EncinaError, ...>>`. Commands in `Model/`.
2. `Services/DefaultNIS2ManagementAccountabilityService.cs` (internal): repository, read-model query, `TimeProvider`, `IOptions<NIS2Options>`, `ILogger`; validates commands and returns the aggregate's `Left` as is (the aggregate already decided the transition, Phase 1).
3. `NIS2Errors.cs`: `ManagementAccountabilityNotFound`, `...CommandInvalid`, `...StoreFailed` (the transition codes are Phase 1).
4. Fail-closed gate (issue Alternatives 10), with this predicate, read from `IRequestContextAccessor` (`IRequestContext.Identity.UserId` and `IRequestContext.TenantId`): the acting principal is ALWAYS required for a command (no context, or a null or blank `UserId`, is denied with `...CommandInvalid`); the tenant is required when tenancy is configured (the `MultiTenancyMarker` is registered, the same trigger as `DeadLetterManager`), and when required a null or blank `TenantId` is denied; queries need the tenant under the same condition. The only opt-out is not registering the workflow. Test rows, one per combination: {context absent, principal absent, principal present} x {tenancy configured with tenant, tenancy configured without tenant, tenancy not configured}. The #822 plan owns the family predicate; if it defines a different trigger, this plan follows it (the five NIS2 plans must agree).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
You are implementing Phase 2 of the management accountability lifecycle for Encina.Compliance.NIS2 (Issue #826).

CONTEXT:
- Phase 1 delivered ManagementAccountabilityAggregate. Pattern: src/Encina.Compliance.DPIA/Services/DefaultDPIAService.cs.

TASK:
1. Create INIS2ManagementAccountabilityService, the commands and the internal default implementation.
2. Add the NIS2Errors factories; every failure is a Left with an error code.
3. Unit tests with NSubstitute for IAggregateRepository; guard tests; a contract test.

KEY RULES:
- ROP; log error codes, never EncinaError.Message; exceptions through ForLogging().
- Do not wrap repository calls in NIS2ResilienceHelper (it returns a fallback on failure).
- Time from TimeProvider.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Services/DefaultDPIAService.cs
- src/Encina.Compliance.NIS2/NIS2Errors.cs
```

</details>

### Phase 3: Projection & Read Model (PR 2)

<details>
<summary>Tasks</summary>

1. `ReadModels/ManagementAccountabilityReadModel.cs`: person id and role, status, delegations, `TrainingValidUntilUtc`, `NextAcknowledgmentDueAtUtc`, tenant and module ids, oversight count and last date.
2. `ReadModels/ManagementAccountabilityProjection.cs`: inline projection for the nine events, idempotent, constructor-injected dependencies only.
3. Queries: overdue training and acknowledgment, and the overview grouped by person id.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
You are implementing Phase 3 of the management accountability lifecycle for Encina.Compliance.NIS2 (Issue #826).

CONTEXT:
- Mirror src/Encina.Compliance.DPIA/ReadModels/DPIAProjection.cs.

TASK:
1. Create the read model and the projection handling the 9 events.
2. Add the overdue and overview queries.
3. Unit-test every handler; keep one integration test that registers the projection with a real Marten store.

KEY RULES:
- Idempotent projection; AtUtc timestamps; dependencies by constructor, never IServiceProvider.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/ReadModels/DPIAProjection.cs
```

</details>

### Phase 4: Configuration, DI & Evaluator Integration (PR 2)

<details>
<summary>Tasks</summary>

1. `NIS2MartenExtensions` (from the #822 plan): add `AddAggregateRepository<ManagementAccountabilityAggregate>()` and `AddProjection<ManagementAccountabilityProjection, ManagementAccountabilityReadModel>()`.
2. `ServiceCollectionExtensions.cs`: `TryAddScoped<INIS2ManagementAccountabilityService, ...>` with its dependencies.
3. `NIS2Options`: `ManagementAccountabilityOptions` (`TrainingValidity`, `AcknowledgmentRenewalPeriod`, `EnableAccountabilityMonitoring`, `AccountabilityCheckInterval` default 1 hour); extend `NIS2OptionsValidator`.
4. Design Choice 5: `CyberHygieneEvaluator` uses the register when the service is registered, otherwise unchanged.
5. DI test with `ValidateOnBuild` and `ValidateScopes`, with and without the workflow.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
You are implementing Phase 4 of the management accountability lifecycle for Encina.Compliance.NIS2 (Issue #826).

CONTEXT:
- AddNIS2EventSourcing exists (plan of issue 822); CyberHygieneEvaluator is in src/Encina.Compliance.NIS2/Evaluators.

TASK:
1. Register the aggregate, projection and service; add and validate the options.
2. Make CyberHygieneEvaluator use the register when the service is registered, otherwise unchanged.
3. Write the DI test (ValidateOnBuild, ValidateScopes) for both configurations.

KEY RULES:
- TryAdd for every default; opt-in; PublicAPI.Unshipped.txt for new public members.
- Design Choice 5 is recommended, pending the maintainer: keep the fallback path small and isolated.

REFERENCE FILES:
- src/Encina.Compliance.NIS2/Evaluators/CyberHygieneEvaluator.cs
- src/Encina.Compliance.NIS2/ServiceCollectionExtensions.cs
- src/Encina.Compliance.DPIA/DPIAMartenExtensions.cs
```

</details>

### Phase 5: Cross-Cutting Integration, Overdue Monitor & Health Check (PR 3)

<details>
<summary>Tasks</summary>

1. `NIS2AccountabilityMonitorService.cs` (`BackgroundService`, `PeriodicTimer`, scope per iteration): escalates each overdue training and acknowledgment once per due date; per-iteration `try/catch`; failures logged by error code.
2. `Health/NIS2ComplianceHealthCheck.cs`: add the accountability check (any overdue, or no `Active` person, gives `Degraded`) behind `AddHealthCheck`.
3. Tenant and module scoping on queries; the event stream is the audit record.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
You are implementing Phase 5 of the management accountability lifecycle for Encina.Compliance.NIS2 (Issue #826).

CONTEXT:
- Pattern: src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs and src/Encina.Compliance.NIS2/Health/NIS2ComplianceHealthCheck.cs.

TASK:
1. Create the monitor and register it only when EnableAccountabilityMonitoring is true.
2. Extend the health check.
3. Tests with FakeTimeProvider and a throwing store: the loop survives and the failure is logged.

KEY RULES:
- A PeriodicTimer loop catches per iteration; OperationCanceledException from the stopping token ends the loop (AGENTS.md section 3).
- Escalation is idempotent per due date.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs
- src/Encina.Compliance.NIS2/Health/NIS2ComplianceHealthCheck.cs
```

</details>

### Phase 6: Observability (PR 3)

<details>
<summary>Tasks</summary>

1. `NIS2Diagnostics`: `StartManagementAccountabilityOperation(...)` activity, instruments `nis2.management_accountability.operations.total` (Counter, tags operation and outcome), `nis2.management_accountability.overdue` (ObservableGauge from the last monitor sweep, tag kind: training or acknowledgment).
2. `NIS2LogMessages`: `[LoggerMessage]` ids 9885-9899 packed from 9885 (one per transition group, invalid transition, store failure, monitor started/stopped/iteration failed, overdue found). Merge related transitions into one message if the 15 ids run short.
3. Run the EventId architecture tests.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
You are implementing Phase 6 of the management accountability lifecycle for Encina.Compliance.NIS2 (Issue #826).

CONTEXT:
- Phases 1 to 5 delivered the domain, service, projection and monitor; this phase instruments them.

TASK:
1. Add the activity, instruments and logger messages with EventIds 9885-9899 packed sequentially.
2. Record metrics in the service and monitor.
3. Run the EventId architecture tests.

KEY RULES:
- Range registered by the #822 plan; [LoggerMessage] source generator; no sparse ids.
- Tags are low cardinality: operation, outcome, kind; never a person id.

REFERENCE FILES:
- src/Encina.Compliance.NIS2/Diagnostics/NIS2Diagnostics.cs
- src/Encina.Compliance.NIS2/Diagnostics/NIS2LogMessages.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 7: Testing (PRs 1 to 3)

<details>
<summary>Tasks</summary>

| Type | Scope | Location |
|---|---|---|
| Unit | Aggregate status rules and replay, scheduler, service with a substituted repository, projection handlers, monitor, health check, evaluator with and without the register | `tests/Encina.UnitTests/Compliance/NIS2/` |
| Guard | Public methods and constructors | `tests/Encina.GuardTests/Compliance/NIS2/` |
| Contract | `INIS2ManagementAccountabilityService`: illegal transitions return `Left`, never throw | `tests/Encina.ContractTests/Compliance/NIS2/` |
| Property | Active implies both validity dates in the future; revoked is final; replay equals live state | `tests/Encina.PropertyTests/Compliance/NIS2/` |
| Integration | Full lifecycle on Marten and PostgreSQL through the shared Testcontainers fixture; projection registered with a real store | `tests/Encina.IntegrationTests/Compliance/NIS2/` |
| Load / Benchmark | Not implemented: very low volume, no hot path; `.md` justification files | `tests/Encina.LoadTests`, `tests/Encina.BenchmarkTests` |

Per-file per-flag targets go into `.github/coverage-manifest/Encina.Compliance.NIS2.json`; targets only go up. Starting values, each with its one-sentence justification written in the manifest:

| File group | Unit | Guard | Contract | Property | Integration | Justification |
|---|---|---|---|---|---|---|
| Aggregate, scheduler, events, model | 95 | 95 | 0 | 60 | 0 | Pure logic with every rule path returning a `Left`; the property flag checks the Active/Revoked invariants and replay; contract and integration are covered at the service level |
| Service and projection | 90 | 90 | 80 | 0 | 70 | Thin orchestration over the repository (unit with a substituted repository); the projection and the full lifecycle are proved against real Marten and PostgreSQL |
| Monitor, health check, diagnostics, evaluator change | 90 | 90 | 0 | 0 | 0 | Loop survival, escalation idempotence and health states are deterministic with `FakeTimeProvider`; no public contract of their own |

A group where a flag is 0 has no coverable lines for that flag, or the lines are covered by another group's test type (the flags do not share lines). CRAP of new or changed methods is 10 or less; the aggregate `Apply` switch carries the `crap-exempt` marker.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
You are implementing Phase 7 (tests) of the management accountability lifecycle for Encina.Compliance.NIS2 (Issue #826).

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

### Phase 8: Documentation & Finalization (PR 3)

<details>
<summary>Tasks</summary>

- XML docs on every new public API.
- `changelog.d/826-nis2-management-accountability.added.md`; never edit `CHANGELOG.md`.
- `PublicAPI.Unshipped.txt` for `Encina.Compliance.NIS2`.
- `docs/features/nis2-compliance.md` (management accountability section), `src/Encina.Compliance.NIS2/README.md`, `docs/INVENTORY.md` via `docs-writer`.
- No ADR: the design follows ADR-019 and ADR-064 of the #822 plan.
- Verification: `dotnet build --configuration Release` with 0 warnings; the five NIS2 test folders pass; every coverage flag reaches its manifest target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
You are finishing the management accountability lifecycle for Encina.Compliance.NIS2 (Issue #826).

CONTEXT:
- Code and tests are done; this phase closes documentation, public API tracking and verification.

TASK:
1. Complete XML docs and PublicAPI.Unshipped.txt entries.
2. Add the changelog fragment changelog.d/826-nis2-management-accountability.added.md.
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
| Art. 20(1) | Management bodies approve the risk-management measures, oversee their implementation and can be held liable | Acknowledgment, oversight events, revocation |
| Art. 20(2) | Members of management bodies follow training; entities are encouraged to offer similar training to employees | Management training tracking; employee training is out of scope |
| Art. 21(2)(g) | Measures include basic cyber hygiene and cybersecurity training | Employee training belongs here (follow-up issue) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| AggregateBase (Guid id, RaiseEvent, Apply) | src/Encina.DomainModeling/AggregateBase.cs | Base class of the new aggregate |
| IAggregateRepository<TAggregate> | src/Encina.Marten | Load, save, create of event-sourced aggregates |
| DPIAAggregate, DPIAEvents, DPIAProjection | src/Encina.Compliance.DPIA | Reference pattern for aggregate, events and projection |
| DPIAReviewReminderService and DPIAOptions (EnableExpirationMonitoring, ExpirationCheckInterval) | src/Encina.Compliance.DPIA | Reference pattern for the opt-in overdue hosted service |
| DPIAMartenExtensions (AddDPIAAggregates) | src/Encina.Compliance.DPIA | Reference pattern for aggregate registration |
| ManagementAccountabilityRecord and NIS2Options.ManagementAccountability | src/Encina.Compliance.NIS2 | Static snapshot kept when the workflow is not registered |
| CyberHygieneEvaluator | src/Encina.Compliance.NIS2/Evaluators | Only consumer of the record |
| NIS2Measure | src/Encina.Compliance.NIS2/Model/NIS2Measure.cs | Enum of the 10 Art. 21(2) measures (area names) |
| NIS2Diagnostics and NIS2LogMessages | src/Encina.Compliance.NIS2/Diagnostics | ActivitySource, Meter, logger messages |
| NIS2ComplianceHealthCheck | src/Encina.Compliance.NIS2/Health | Health check pattern |
| AddNIS2EventSourcing and range 9800-9899 | Plan of issue 822 | Shared pieces owned by 822 |

### Event ID Allocation

| Package | Range | Notes |
| :--- | :--- | :--- |
| Encina.Compliance.NIS2 | 9200-9299 | Existing; 9288-9299 unused |
| Encina.Compliance.NIS2 (event-sourced modules) | 9800-9899 | Range `ComplianceNIS2EventSourced` registered by the plan of issue 822 |
| Issue 826 management accountability | 9885-9899 | 15 ids, this plan |

### File Count Estimate

| Category | Files | Approx. lines |
| :--- | :--- | :--- |
| Production: model (status, delegation, training record, commands, schedule calculator) | 4 | 350 |
| Production: aggregate and events | 2 | 600 |
| Production: service abstraction and default implementation | 2 | 400 |
| Production: read model and projection | 2 | 280 |
| Production: overdue monitor, health check additions, options, DI and evaluator integration | 4 | 380 |
| Tests: unit | 4 | 750 |
| Tests: guard | 2 | 170 |
| Tests: contract | 1 | 100 |
| Tests: property | 1 | 120 |
| Tests: integration (Marten, PostgreSQL) | 1 | 230 |
| Docs and metadata: PublicAPI, coverage manifest, changelog fragment, feature page, README | 5 | 200 |
| Total | 28 | 3580 |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library (pre-1.0, no backward compatibility). You extend src/Encina.Compliance.NIS2 with an event-sourced management accountability lifecycle (issue #826, NIS2 Art. 20). The store is Marten on PostgreSQL. Read AGENTS.md first. The EventId range, project references and AddNIS2EventSourcing come from the plan of issue 822; the evaluator opt-in decision from the plan of issue 823.

IMPLEMENTATION OVERVIEW:
1. Model: status, oversight type, ComplianceDelegation, TrainingRecord, AccountabilityScheduler, commands.
2. Nine events (INotification) and ManagementAccountabilityAggregate (AggregateBase, one stream per appointment).
3. INIS2ManagementAccountabilityService + internal default over IAggregateRepository; NIS2Errors factories.
4. ManagementAccountabilityProjection with ReadModel (overdue dashboard, overview by person).
5. Registration lines in AddNIS2EventSourcing, options, CyberHygieneEvaluator reads the register when registered.
6. NIS2AccountabilityMonitorService (opt-in) and the health check addition.
7. Diagnostics and [LoggerMessage] ids 9885-9899.
8. Unit, guard, contract, property, integration tests; load and benchmark justifications.
9. Docs, PublicAPI, changelog fragment, coverage manifest. Deliver in the 3 pull requests of the split.

KEY PATTERNS:
- Mirror DPIAAggregate, DPIAEvents, DPIAProjection, DPIAReviewReminderService, DPIAMartenExtensions.
- ROP: Either<EncinaError, T>; EncinaError.Message never reaches logs; TimeProvider for time; PeriodicTimer loop catches per iteration.
- NIS2ResilienceHelper returns a fallback on failure: never wrap store calls in it.
- Design Choice 5 is a Dilemma recommended, pending the maintainer: keep the fallback path isolated.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs
- src/Encina.Compliance.NIS2/Evaluators/CyberHygieneEvaluator.cs
- src/Encina.Compliance.NIS2/Model/ManagementAccountabilityRecord.cs
- docs/plans/nis2-incident-lifecycle-implementation-plan-822.md
- docs/plans/nis2-supply-chain-assessment-implementation-plan-823.md
- docs/architecture/adr/019-compliance-event-sourcing-marten.md, 050-modules-may-require-modules.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Accountability state must be fresh when a gate or a health check reads it |
| 2 | OpenTelemetry | ✅ | Phase 6: activity per operation, `nis2.management_accountability.*` instruments on the package meter |
| 3 | Structured Logging | ✅ | Phase 6: `[LoggerMessage]` ids 9885-9899 of the range registered by the #822 plan |
| 4 | Health Checks | ✅ | Phase 5: overdue training or acknowledgment, or no Active person, gives Degraded |
| 5 | Validation | ✅ | Commands validated in the service (blank ids and areas, dates in the future, duplicate delegation) before any event |
| 6 | Resilience | ❌ | No external call; store calls are not wrapped in `NIS2ResilienceHelper`, which swallows failures |
| 7 | Distributed Locks | ⏭️ | Escalation is idempotent per due date and Marten optimistic concurrency protects duplicates (Design Choice 4); multi-instance coordination follows ADR-050; no new issue |
| 8 | Transactions | ✅ | The Marten session commits the event append atomically; no `TransactionPipelineBehavior` (ADR-050) |
| 9 | Idempotency | ✅ | Aggregate version and the per-due-date escalation guard |
| 10 | Multi-Tenancy | ✅ | `TenantId` in events, aggregate and read model |
| 11 | Module Isolation | ✅ | `ModuleId` on events and read model, as `DPIAAggregate`; the issue left the decision to the plan |
| 12 | Audit Trail | ✅ | The event stream is the audit trail; person identity follows the shared personal-data decision |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|---|---|---|
| #822 plan, PR 1: range 9800-9899, references, `AddNIS2EventSourcing` | Planned | PR 1 of this plan depends on it |
| #823 plan, Design Choice 3 (evaluators and the static configuration) | Pending the maintainer | Design Choice 5 follows its outcome |
| Personal-data decision (Dilemma 6 of the #822 plan) | Pending the maintainer | Person ids stay separate nullable properties until decided |
| Employee training programme tracking (Art. 21(2)(g)) | Issue file written, not opened | Reuses `TrainingRecord` later |
| #1603 NIS2 reads the system clock instead of `TimeProvider` | Open | New code takes `TimeProvider`; the old code is not touched here |

---

## Next Steps

1. Maintainer answers the Dilemma of Design Choice 5 (shared with #823, Design Choice 3) when work on #826 starts; it is marked "recommended, pending the maintainer".
2. The orchestrator opens `artifacts/issues/nis2-employee-training-programme.md`.
3. Brief PR 1 after the #822 PR 1 merges; PR 2 and 3 follow in order.
4. Run `check-plan.ps1` without `-Draft` after the adversarial review sets the `Verified` lines.
