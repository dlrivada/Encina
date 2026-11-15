# Implementation Plan: `Encina.Compliance.NIS2` — Supply Chain Security Assessment Workflow (Art. 21(2)(d))

> **Issue**: [#823](https://github.com/dlrivada/Encina/issues/823)
> **Type**: Feature
> **Complexity**: High (event-sourced aggregate, projection, overdue monitor, 5 pull requests)
> **Estimated Scope**: 52 file touches and about 5,400 lines including tests and per-PR metadata; split into 5 pull requests, see "Pull request split"
> **Milestone**: v0.15.0 — EU Compliance: NIS2 & Digital Omnibus
> **Provider Category**: None for databases (single event-sourcing provider: Marten on PostgreSQL, ADR-027)
> **Family**: #822 to #826. Shared pieces are owned by the plan of #822 and are referenced here, not redefined.

---

## Summary

Supplier security in `Encina.Compliance.NIS2` is a static configuration check today: `NIS2Options.AddSupplier` fills a dictionary, `SupplyChainSecurityEvaluator` fails when it is empty or holds a `Critical` supplier, and `DefaultSupplyChainSecurityValidator` flags `High`/`Critical` suppliers and assessments older than 12 months. Nothing records who assessed a supplier, what was found, whether remediation happened or when the certification expires.

This plan adds an event-sourced **supplier assessment workflow** in the style of the GDPR DPIA module (`Encina.Compliance.DPIA`):

- `SupplierAssessmentAggregate` (an `AggregateBase`) with a state machine over assessment, remediation, approval, suspension and termination, reusing the existing `SupplierRiskLevel`.
- `INIS2SupplierAssessmentService` (commands that append events and one overdue query), an inline Marten projection (`SupplierAssessmentReadModel`), an opt-in overdue monitor, a health check addition and `nis2.supplier_assessment.*` metrics.
- A reassessment schedule from the risk level (1/3/6/12 months, the intervals `DefaultSupplyChainSecurityValidator` already uses) now configurable.

**Standards**: NIS2 Directive (EU) 2022/2555 Art. 21(2)(d) and 21(3). **Not in scope**: automatic suspension or termination (a business decision, Design Choice 2), contract or procurement workflows, and changes to the incident or risk modules.

**Affected packages**: `Encina.Compliance.NIS2` (extended). The `Encina` core, the `Encina.Marten` and `Encina.DomainModeling` references, the EventId range and the registration entry point come from the #822 plan.

**Provider category**: none of the 10 database providers; event sourcing (Marten) is excluded from the database matrix by AGENTS.md section 5.

### Corrections to the issue text (checked against today's code)

| Issue text | Today's code | Plan decision |
|---|---|---|
| `SupplierAssessmentStatus` has `Pending` and `InProgress` | No event leads to `Pending`: `SupplierAssessmentInitiated` starts the work | `Pending` is dropped; `Initiated` leads to `InProgress` (Design Choice 4) |
| Nine events | Motivation asks for "threshold-based alerts / automatic escalation" and Alternatives 8 for escalation events, but none is listed | A tenth event `SupplierAssessmentOverdueEscalated` |
| "Replacing the current static supplier configuration" | The module is opt-in (AGENTS.md section 1) | Dilemma 3: the static registry stays when the workflow is not registered |
| Metrics `encina.nis2.supplier_assessment.*` | The package meter `Encina.Compliance.NIS2` names instruments `nis2.*` | `nis2.supplier_assessment.*` |
| Cache follow-up issue (matrix row 1) | Reads are inline-projection lookups | Caching is not applicable (matrix); no follow-up issue |

### Shared pieces (owned by the #822 plan)

| Piece | Where it is decided | What this plan does |
|---|---|---|
| EventId range `ComplianceNIS2EventSourced` (9800-9899) | #822 plan, "Shared pieces of the family" | Allocates **9830-9849** (20 ids); never touches other blocks |
| Project references `Encina.Marten`, `Encina.DomainModeling` (and `Encina.Marten.GDPR` if the personal-data Dilemma resolves to B) | #822 plan, PR 1 | Depends on that pull request |
| `AddNIS2EventSourcing()` in `NIS2MartenExtensions` | #822 plan, Phase 4 | Adds one `AddAggregateRepository<SupplierAssessmentAggregate>()` and one `AddProjection<,>()` line |
| ADR-064 (event-sourced NIS2 modules) | #822 plan | Referenced, not written here |
| Personal data in events | Dilemma 6 of the #822 plan | The assessor identity is a separate nullable property; the outcome is applied here (Design Choice 7) |

---

## Design Choices

<details>
<summary><strong>1. Aggregate granularity — one stream per supplier, one cycle after another</strong></summary>

- **Class**: Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One stream per supplier and tenant (deterministic id); each assessment cycle is a run of events (`Initiated` ... `Approved`)** | Suspension, termination and certification updates belong to the supplier, not to a cycle; one place for the full history; no lookup of the "current" cycle | The stream grows with every cycle (a cycle is about 5 events, a supplier lives years); the id must be derived from tenant and supplier id |
| B) One stream per assessment cycle (random `Guid`); the projection answers "latest cycle of supplier X" | Each cycle closes cleanly; small streams | Suspension and certification events need a cycle to attach to; "is this supplier approved" needs the projection |

### Chosen Option: **A — one stream per supplier (recommended, pending the maintainer)**

### Rationale

- A is better because the issue's events `SupplierSuspended`, `SupplierTerminated` and `SupplierCertificationUpdated` describe the relationship, not a cycle.
- B wins if a supplier will hold hundreds of cycles, or when each cycle must be independently archived or erased; the read model can still expose cycle history in A.
- With A the id is `NIS2SupplierStreamId.For(tenantId, supplierId)`: a UUIDv5 (RFC 9562, SHA-1 name-based) with a fixed namespace Guid constant of the NIS2 package, over the key `"{tenant}|{supplier}"` where each part is trimmed and lower-cased with the invariant culture (so `Supplier-A` and `supplier-a` map to the same stream, matching the `OrdinalIgnoreCase` comparer of `NIS2Options.Suppliers`); a null tenant is the empty string only when the options allow a missing tenant, otherwise the call is denied (Phase 2 task 4). The algorithm and normalisation are part of the Dilemma 1 outcome, and `CreateAsync` on an existing stream fails with a handled `Left`, after which the service loads the stream and appends `Initiated`.

</details>

<details>
<summary><strong>2. Risk level determination — assessor declares the level, criteria are evidence</strong></summary>

- **Class**: Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The assessor declares `SupplierRiskLevel`; the criteria scores (access to systems, data sensitivity, service criticality, certification, incident history) are stored as evidence** | Matches qualitative methods such as NIST C-SCRM; no formula to defend | Two assessors can rate the same facts differently |
| B) The level is computed from weighted criteria by a pure calculator | Consistent, testable, comparable across suppliers | A formula must be chosen and defended per sector; edge cases need an override anyway |

### Chosen Option: **A — assessor declares, criteria as evidence (recommended, pending the maintainer)**

### Rationale

- A is better while there is no sector-specific weighting to cite; the aggregate rejects an assessment whose criteria are all absent (no evidence), and the alert threshold (option `AlertThreshold`, default `High`) flags a level at or above it on the read model and in a metric.
- B is better when the entity needs identical ratings across assessors or must justify the level to an authority with a formula; it can be added later behind the same command (an optional `IRiskLevelAdvisor`).
- Suspension and termination stay explicit commands in both options: Art. 21(3) asks entities to take supplier vulnerabilities into account, not to terminate automatically.

</details>

<details>
<summary><strong>3. Relation to the static registry — opt-in workflow, static registry unchanged (family decision)</strong></summary>

- **Class**: Dilemma

This choice is the same for #823, #824 and #826 (each has a boolean or snapshot configuration today). It is owned by this plan; the #824 and #826 plans reference it.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) When the workflow is registered, the evaluator and validator read the register; otherwise they keep using `NIS2Options` as today** | Opt-in (AGENTS.md section 1); applications without Marten keep working | Two sources of truth coexist; the evaluator needs both paths |
| B) Replace the static registry: remove `AddSupplier` and `SupplierInfo`, the register is the only source | One source, no legacy path (pre-1.0, AGENTS.md section 1) | Every user of the evaluator must run Marten; breaking for a package that is stateless today |
| C) The static registry seeds the aggregates at startup | Existing configuration is carried over | A startup writer that appends events from configuration: a migration helper, forbidden by AGENTS.md section 3, and it re-runs on every start |

### Chosen Option: **A — opt-in, both paths (recommended, pending the maintainer)**

### Rationale

- A is better while stateless users exist; the NIS2 package is the validator for applications that do not run an event store.
- B wins once the maintainer decides that NIS2 requires Marten for 1.0; the cost of choosing A first is one conditional in each evaluator, removable later.
- C is rejected in either case (AGENTS.md section 3).

</details>

<details>
<summary><strong>4. Status set and transitions</strong></summary>

- **Class**: Trivial
- **Settled by**: the issue's event table; the status handling of `DPIAAggregate` (`src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs`) for structure only. The issue's `SupplierAssessmentStatus` is deliberately NOT followed on `Pending`: no event leads to it (Corrections table), so a value no transition can reach is dropped.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Status `InProgress, Assessed, RemediationRequired, RemediationInProgress, RemediationVerified, Approved, Suspended, Terminated`; `Pending` dropped** | Every status is reachable from an event | One value fewer than the issue |
| B) Keep `Pending` with a synthetic first event | Matches the issue text | An event with no business meaning |

### Chosen Option: **A**

### Rationale

- Illegal transitions are `Left` values with a rule code, never exceptions (ADR-053): the aggregate commands return `Either<EncinaError, Unit>` and `Initiate` returns `Either<EncinaError, SupplierAssessmentAggregate>`; the rule is decided before `RaiseEvent`. `DPIAAggregate` still throws on main only because [#2668](https://github.com/dlrivada/Encina/issues/2668) (the ADR-053 implementation, with the shared rule-code catalogue) is open; this plan uses `NIS2Errors.SupplierAssessmentInvalidTransition` and moves it onto the catalogue when #2668 lands, as the #822 plan does.
- Transitions: `Initiated` from no stream, or from `Approved` (a new cycle), or from `Suspended` (the only way out of `Suspended`: a new `Initiated` cycle, which is the reinstatement path; there is no direct `Suspended` to `Approved`); `RiskAssessed` only from `InProgress`; `RemediationRequested` from `Assessed`; `RemediationSubmitted` from `RemediationRequired`; `RemediationVerified` from `RemediationInProgress`; `Approved` from `Assessed` (no findings) or `RemediationVerified`; `Suspended` and `CertificationUpdated` from any status except `Terminated`; `Terminated` from any status except itself; nothing leaves `Terminated`.

</details>

<details>
<summary><strong>5. Overdue and certification monitoring — hosted service with idempotent escalation</strong></summary>

- **Class**: Settled
- **Settled by**: issue #823 Alternatives Considered 8; `src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs` (always on, no opt-in flag since #3786; `PeriodicTimer`); AGENTS.md section 3 (a `PeriodicTimer` loop catches per iteration; `OperationCanceledException` from the stopping token ends the loop; errors are not swallowed); ADR-050 (multi-instance coordination for event-sourced modules).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `NIS2SupplierAssessmentMonitorService`: queries the projection for overdue assessments and expiring certifications, appends `SupplierAssessmentOverdueEscalated` once per due date** | Detects a miss when nobody calls the service; feeds the gauge and the health check | Needs duplicate protection with several instances |
| B) Evaluate only on read | No background service | A forgotten supplier never escalates |

### Chosen Option: **A**

### Rationale

- Duplicate protection without a lock: the aggregate remembers `EscalatedForDueAtUtc` and ignores a second escalation for the same due date; Marten optimistic concurrency turns a race into a handled `Left` that the next iteration skips.
- Expired or soon-to-expire certifications (`SupplierCertificationUpdated` carries `ExpiresAtUtc`) are reported by the same sweep on the read model, in the gauge and the health check; they do not produce an event (the due date of the next assessment is the escalation trigger).
- A failed query or append is logged by error code; the loop continues. Registered only when `NIS2Options.EnableSupplierAssessmentMonitoring` is true.

</details>

<details>
<summary><strong>6. Reassessment intervals — the existing 1/3/6/12 months, as options</strong></summary>

- **Class**: Trivial
- **Settled by**: `DefaultSupplyChainSecurityValidator` (next assessment at 1, 3, 6 or 12 months for `Critical`, `High`, `Medium`, `Low`); precedent `DPIAOptions.DefaultReviewPeriod`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Options `ReassessmentMonths` per level with those defaults** | One behaviour for validator and workflow | None |
| B) New defaults | Free choice | Two different schedules in one package |

### Chosen Option: **A**

### Rationale

- A pure `SupplierAssessmentScheduler` (`NextDue(level, assessedAtUtc, options)`, `AddMonths`) is used by the aggregate-facing service and by `DefaultSupplyChainSecurityValidator` so the schedule cannot diverge.

</details>

<details>
<summary><strong>7. Assessor identity in the events — outcome of the shared personal-data decision</strong></summary>

- **Class**: Trivial
- **Settled by**: the #822 plan, Dilemma 6 (personal data in events); `src/Encina.Marten.GDPR` ADR-034 for the mechanism if option B is chosen.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AssessedByUserId`, `ApprovedByUserId` are separate nullable properties of the events, annotated as the shared decision requires; supplier names and contacts of legal entities are not personal data** | Follows the family outcome; the event shape does not change when the attribute is added | Free-text findings may name people (documented) |
| B) Decide here | Independent | Contradicts the family decision |

### Chosen Option: **A**

### Rationale

- Findings and remediation text are documented as personal-data-free, as in the #822 plan; no per-supplier decision is taken here.

</details>

<details>
<summary><strong>8. Storage, aggregate base, id and event delivery</strong></summary>

- **Class**: Settled
- **Settled by**: ADR-019 (compliance modules are event sourced; amendment of 2026-10-16 on delivery through the Outbox), ADR-027 (Marten), ADR-050 (modules an event-sourced module requires), AGENTS.md section 3 (no InMemory stores for event-sourced compliance modules); `src/Encina.DomainModeling/AggregateBase.cs`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Marten, `AggregateBase` with `Guid Id`, events implementing `INotification`, no InMemory store** | Same as every compliance aggregate | Needs a PostgreSQL for tests |
| B) CRUD entity plus audit log | Simple | Mutable history, rejected by ADR-019 |

### Chosen Option: **A**

### Rationale

- Unit tests mock `IAggregateRepository<SupplierAssessmentAggregate>` (NSubstitute); integration tests use Marten on PostgreSQL through Testcontainers (AGENTS.md section 3).

</details>

---

## Implementation Phases

### Pull request split (read first)

<details>
<summary>Tasks</summary>

52 file touches and about 5,400 lines, over the limit of 15 files and 1,500 lines per pull request (AGENTS.md section 11; the blocking `pr-size` job counts tests, PublicAPI, manifest and docs). Split in 5, counts including every file:

| PR | Phases | Files | Test files | Files / lines (approx.) |
|---|---|---|---|---|
| 1 | 1 (tasks 1 to 4) | `SupplierAssessmentStatus`, `SupplierRiskCriteria`, `SupplierAssessmentScheduler`, `NIS2SupplierStreamId`, `PublicAPI.Unshipped.txt`, manifest (6) | unit (scheduler, stream id), property (stream id normalisation) (2) | 8 files / 700 |
| 2 | 1 (tasks 5 to 7) | events, aggregate, `NIS2Errors` (transition code), `PublicAPI.Unshipped.txt`, manifest (5) | unit aggregate, guard, property (3) | 8 files / 1,350 |
| 3 | 2, 3 | service interface, command records, default service, `NIS2Errors`, read model, projection, `PublicAPI.Unshipped.txt`, manifest (8) | unit service, unit projection, guard, contract (4) | 12 files / 1,350 |
| 4 | 4 | `NIS2MartenExtensions`, `ServiceCollectionExtensions`, `NIS2Options` and validator, `SupplyChainSecurityEvaluator`, `DefaultSupplyChainSecurityValidator`, `PublicAPI.Unshipped.txt`, manifest (8) | unit evaluator and validator, DI test, integration (3) | 11 files / 1,000 |
| 5 | 5, 6, 8 | monitor, `NIS2ComplianceHealthCheck`, `NIS2Diagnostics`, `NIS2LogMessages`, `PublicAPI.Unshipped.txt`, manifest, changelog fragment, feature page, README, `docs/INVENTORY.md` (10) | unit monitor, unit health check, guard (3) | 13 files / 1,000 |

If a PR measures over 15 files or 1,500 lines, the worker splits it again; it never asks for `size-exception-approved`.

Prerequisite: PR 1 of the #822 plan (range, references, `AddNIS2EventSourcing`) is merged; this plan's PR 1 waits for it.

</details>

### Phase 1: Core Model, Events & Aggregate (PR 1: tasks 1 to 4; PR 2: tasks 5 to 7)

<details>
<summary>Tasks</summary>

1. `Model/SupplierAssessmentStatus.cs`: the eight statuses of Design Choice 4.
2. `Model/SupplierRiskCriteria.cs`: sealed record with the scored criteria (`DataAccess`, `ServiceCriticality`, `CertificationCoverage`, `IncidentHistory`, each 0 to 4, plus free-text `Notes`).
3. `Model/SupplierAssessmentScheduler.cs` (internal, static): `NextDue(SupplierRiskLevel, DateTimeOffset, SupplierAssessmentOptions)`.
4. `Model/NIS2SupplierStreamId.cs` (internal, static): `For(string? tenantId, string supplierId)` returns the deterministic `Guid`.
5. `Events/SupplierAssessmentEvents.cs`: ten sealed records implementing `INotification`: `SupplierAssessmentInitiated`, `SupplierRiskAssessed`, `SupplierRemediationRequested`, `SupplierRemediationSubmitted`, `SupplierRemediationVerified`, `SupplierApproved`, `SupplierSuspended`, `SupplierTerminated`, `SupplierCertificationUpdated`, `SupplierAssessmentOverdueEscalated`. Each has the supplier id, a `...AtUtc` timestamp, `TenantId`, `ModuleId`; person ids are separate nullable properties.
6. `Aggregates/SupplierAssessmentAggregate.cs`: `public sealed class ... : AggregateBase`; static `Initiate(...)`; instance `RecordRiskAssessment`, `RequestRemediation`, `SubmitRemediation`, `VerifyRemediation`, `Approve`, `Suspend`, `Terminate`, `UpdateCertification`, `EscalateOverdue`, each returning `Either<EncinaError, Unit>` (`Initiate` returns `Either<EncinaError, SupplierAssessmentAggregate>`; ADR-053: an illegal transition is a `Left` with the `SupplierAssessmentInvalidTransition` rule code before `RaiseEvent`); `Apply` switch; properties of the issue plus `LastAssessedAtUtc`, `CertificationExpiresAtUtc`, `EscalatedForDueAtUtc`, `RequiresAttention`.
7. Tests: unit (each legal transition, each illegal one returns `Left` and raises no event, replay, scheduler 1/3/6/12, stream id normalisation), guard, property (no sequence leaves `Terminated`; `NextAssessmentDueAtUtc` set after approval).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
You are implementing Phase 1 of the supplier assessment workflow for Encina.Compliance.NIS2 (Issue #823).

CONTEXT:
- Encina is .NET 10 / C# 14, Either<EncinaError, T> for business results, TimeProvider for time (the aggregate takes timestamps as parameters).
- The EventId range 9800-9899, the Marten and DomainModeling references and AddNIS2EventSourcing come from the plan of issue 822; this phase uses ids 9830-9849 only.
- Pattern for structure, events and Apply: src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs and Events/DPIAEvents.cs; error style per docs/architecture/adr/053-aggregate-rule-violations-return-either.md (DPIAAggregate still throws until #2668; do not copy that).
- Two pull requests: PR 1 = status enum, criteria, scheduler, stream id (UUIDv5, trim + invariant lower-case); PR 2 = events and aggregate.

TASK:
1. Create the status enum, criteria record, scheduler, stream id helper, the 10 events and SupplierAssessmentAggregate (Design Choices 1, 2, 4, 6).
2. Reuse Model/SupplierRiskLevel.cs; add PublicAPI.Unshipped.txt lines and XML docs.
3. Write unit, guard and property tests that instantiate the real aggregate.

KEY RULES:
- AggregateBase with Guid Id; no clock reads; commands return Either<EncinaError, Unit> (Initiate: Either<EncinaError, SupplierAssessmentAggregate>); an illegal transition is a Left with the SupplierAssessmentInvalidTransition rule code decided before RaiseEvent; never throw InvalidOperationException for a rule (ADR-053).
- Person ids are separate nullable properties (shared personal-data decision of the #822 plan).
- No [Obsolete], no compatibility code; zero warnings.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs
- src/Encina.Compliance.NIS2/Model/SupplierRiskLevel.cs
- src/Encina.Compliance.NIS2/DefaultSupplyChainSecurityValidator.cs
```

</details>

### Phase 2: Service, Errors & Default Implementation (PR 3)

<details>
<summary>Tasks</summary>

1. `Abstractions/INIS2SupplierAssessmentService.cs`: `InitiateAssessmentAsync`, `RecordRiskAssessmentAsync`, `RequestRemediationAsync`, `SubmitRemediationAsync`, `VerifyRemediationAsync`, `ApproveSupplierAsync`, `SuspendSupplierAsync`, `TerminateSupplierAsync`, `UpdateCertificationAsync`, `GetOverdueAssessmentsAsync`; each `ValueTask<Either<EncinaError, ...>>` with `CancellationToken`. The issue's `Guid assessmentId` parameter is the supplier stream id returned by `InitiateAssessmentAsync`. Command records in `Model/`.
2. `Services/DefaultNIS2SupplierAssessmentService.cs` (internal): `IAggregateRepository<SupplierAssessmentAggregate>`, the read-model query, `TimeProvider`, `IOptions<NIS2Options>`, `ILogger`. Validates commands, calls the aggregate and composes its `Either` with `Bind` (no transition pre-check: the aggregate owns the state machine, ADR-053), saves; never swallows a `Left`.
3. `NIS2Errors.cs`: `SupplierAssessmentNotFound`, `SupplierAssessmentCommandInvalid`, `SupplierAssessmentStoreFailed` (`SupplierAssessmentInvalidTransition` came with the aggregate in PR 2).
4. Tenant and module ids from `ITenantContext`/`IModuleContext`; a missing tenant where the options require one is denied (issue Alternatives 10).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
You are implementing Phase 2 of the supplier assessment workflow for Encina.Compliance.NIS2 (Issue #823).

CONTEXT:
- Phase 1 delivered SupplierAssessmentAggregate and events. This phase adds the service over IAggregateRepository<SupplierAssessmentAggregate>.
- Pattern: src/Encina.Compliance.DPIA/Services/DefaultDPIAService.cs.

TASK:
1. Create INIS2SupplierAssessmentService, the command records and the internal default implementation.
2. Add the NIS2Errors factories; every failure is a Left with an error code.
3. Unit tests with NSubstitute for IAggregateRepository; guard tests; a contract test of the service.

KEY RULES:
- ROP: a Left from the repository fails the operation; log error codes, never EncinaError.Message; exceptions through ForLogging().
- Do not wrap repository calls in NIS2ResilienceHelper (it returns a fallback on failure).
- Time from TimeProvider.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Services/DefaultDPIAService.cs
- src/Encina.Compliance.NIS2/NIS2Errors.cs
```

</details>

### Phase 3: Projection & Read Model (PR 3)

<details>
<summary>Tasks</summary>

1. `ReadModels/SupplierAssessmentReadModel.cs`: supplier id and name, status, risk level, `NextAssessmentDueAtUtc`, `CertificationExpiresAtUtc`, `RequiresAttention`, `EscalatedForDueAtUtc`, tenant and module ids.
2. `ReadModels/SupplierAssessmentProjection.cs`: inline projection handling the ten events, idempotent, constructor-injected dependencies only (no `IServiceProvider`).
3. Queries for the service and monitor: overdue (`NextAssessmentDueAtUtc < now`, status not `Terminated`) and expiring certifications.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
You are implementing Phase 3 of the supplier assessment workflow for Encina.Compliance.NIS2 (Issue #823).

CONTEXT:
- Mirror src/Encina.Compliance.DPIA/ReadModels/DPIAProjection.cs.

TASK:
1. Create SupplierAssessmentReadModel and SupplierAssessmentProjection for the 10 events.
2. Add the overdue and expiring-certification queries.
3. Unit-test every handler with real instances and keep one integration test that registers the projection with a real Marten store.

KEY RULES:
- Idempotent projection; AtUtc timestamps; dependencies by constructor, never IServiceProvider.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/ReadModels/DPIAProjection.cs
```

</details>

### Phase 4: Configuration, DI, Evaluator & Validator Integration (PR 4)

<details>
<summary>Tasks</summary>

1. `NIS2MartenExtensions` (created by the #822 plan): add `services.AddAggregateRepository<SupplierAssessmentAggregate>();` and `services.AddProjection<SupplierAssessmentProjection, SupplierAssessmentReadModel>();`.
2. `ServiceCollectionExtensions.cs`: `TryAddScoped<INIS2SupplierAssessmentService, ...>` registered together with its dependencies (registration completeness, AGENTS.md section 3).
3. `NIS2Options`: `SupplierAssessmentOptions` (`ReassessmentMonths` per level, `AlertThreshold`, `EnableSupplierAssessmentMonitoring`, `SupplierAssessmentCheckInterval` default 1 hour); extend `NIS2OptionsValidator`.
4. Dilemma 3, option A: `SupplyChainSecurityEvaluator` and `DefaultSupplyChainSecurityValidator` read the register through the service when it is registered (a supplier counts when its status is `Approved` and not overdue; `Critical` risk or `Suspended`/`Terminated` fails), otherwise the code path stays as today. Lifetimes: `INIS2SupplierAssessmentService` is scoped (Marten session), but `DefaultSupplyChainSecurityValidator` and the evaluators are singletons today (`ServiceCollectionExtensions.cs`: `TryAddSingleton<ISupplyChainSecurityValidator, ...>`, evaluators registered as `ServiceDescriptor.Singleton`), and a singleton must not capture a scoped service (`ValidateScopes` rejects it). Resolution: their lifetimes do NOT change; the evaluator resolves the service from the `ServiceProvider` of its evaluation context inside a scope, and `DefaultSupplyChainSecurityValidator` takes `IServiceScopeFactory` (a singleton-safe dependency, a constructor change in this PR) and creates a scope per call to resolve the optional service. When the service is not registered, `GetService` returns null and the static path runs.
5. DI test with `ValidateOnBuild` and `ValidateScopes`, with and without the workflow.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
You are implementing Phase 4 of the supplier assessment workflow for Encina.Compliance.NIS2 (Issue #823).

CONTEXT:
- AddNIS2EventSourcing exists (plan of issue 822). Evaluators are in src/Encina.Compliance.NIS2/Evaluators.

TASK:
1. Register the aggregate, projection and service; add the options and validate them.
2. Make SupplyChainSecurityEvaluator and DefaultSupplyChainSecurityValidator use the register when the service is registered, otherwise unchanged. Both stay singletons: the evaluator resolves the scoped service through its context's service provider in a scope, the validator takes IServiceScopeFactory and creates a scope per call.
3. Write the DI test (ValidateOnBuild, ValidateScopes) for both configurations.

KEY RULES:
- TryAdd for every default; opt-in; PublicAPI.Unshipped.txt for new public members.
- Dilemma 3 is recommended, pending the maintainer: keep the fallback path small and isolated so option B can remove it.

REFERENCE FILES:
- src/Encina.Compliance.NIS2/ServiceCollectionExtensions.cs
- src/Encina.Compliance.NIS2/Evaluators/SupplyChainSecurityEvaluator.cs
- src/Encina.Compliance.DPIA/DPIAMartenExtensions.cs
```

</details>

### Phase 5: Cross-Cutting Integration, Monitor & Health Check (PR 5)

<details>
<summary>Tasks</summary>

1. `NIS2SupplierAssessmentMonitorService.cs` (`BackgroundService`, `PeriodicTimer`, scope per iteration through `IServiceScopeFactory`): per iteration, overdue assessments get `EscalateOverdue` once per due date; expiring certifications update the snapshot; per-iteration `try/catch`; failures logged by error code.
2. `Health/NIS2ComplianceHealthCheck.cs`: add the supplier check (overdue or expired certification gives `Degraded`) behind the existing `AddHealthCheck`.
3. Tenant and module scoping on queries; audit: the event stream is the record.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
You are implementing Phase 5 of the supplier assessment workflow for Encina.Compliance.NIS2 (Issue #823).

CONTEXT:
- Pattern: src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs and src/Encina.Compliance.NIS2/Health/NIS2ComplianceHealthCheck.cs.

TASK:
1. Create the monitor and register it only when EnableSupplierAssessmentMonitoring is true.
2. Extend the health check with the supplier status.
3. Tests with FakeTimeProvider and a throwing store: the loop survives and the failure is logged.

KEY RULES:
- A PeriodicTimer loop catches per iteration; OperationCanceledException from the stopping token ends the loop (AGENTS.md section 3).
- Escalation is idempotent per due date.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs
- src/Encina.Compliance.NIS2/Health/NIS2ComplianceHealthCheck.cs
```

</details>

### Phase 6: Observability (PR 5)

<details>
<summary>Tasks</summary>

1. `NIS2Diagnostics`: `StartSupplierAssessmentOperation(...)` activity (the supplier id is a span tag only, never a metric tag), instruments `nis2.supplier_assessment.operations.total` (Counter, tags operation and outcome), `nis2.supplier_assessment.threshold_exceeded.total` (Counter, tag risk level), `nis2.supplier_assessment.overdue` (ObservableGauge from the last monitor sweep).
2. `NIS2LogMessages`: `[LoggerMessage]` ids 9830-9849 packed from 9830 (one per transition, invalid transition, store failure, monitor started/stopped/iteration failed, overdue found, certification expiring); the XML summary names the range.
3. Run the EventId architecture tests.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
You are implementing Phase 6 of the supplier assessment workflow for Encina.Compliance.NIS2 (Issue #823).

CONTEXT:
- Phases 1 to 5 delivered the domain, service, projection and monitor; this phase instruments them.

TASK:
1. Add the activity, instruments and logger messages with EventIds 9830-9849 packed sequentially.
2. Record metrics in the service and monitor; the gauge reads a snapshot.
3. Run the EventId architecture tests.

KEY RULES:
- Range registered by the #822 plan; [LoggerMessage] source generator; no sparse ids.
- Metric tags are low cardinality: operation, outcome, risk level; never the supplier id.

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
| Unit | Aggregate transitions and replay, scheduler, stream id, service with a substituted repository, projection handlers, monitor, health check, evaluator with and without the register | `tests/Encina.UnitTests/Compliance/NIS2/` |
| Guard | Public methods and constructors | `tests/Encina.GuardTests/Compliance/NIS2/` |
| Contract | `INIS2SupplierAssessmentService` and the aggregate commands: illegal transitions return `Left` with the `SupplierAssessmentInvalidTransition` rule code, raise no event and never throw | `tests/Encina.ContractTests/Compliance/NIS2/` |
| Property | Terminated is final; due date set after approval; replay equals live state | `tests/Encina.PropertyTests/Compliance/NIS2/` |
| Integration | Full workflow on Marten and PostgreSQL through the shared Testcontainers fixture; projection registered with a real store; concurrent escalation yields one handled `Left` | `tests/Encina.IntegrationTests/Compliance/NIS2/` |
| Load / Benchmark | Not implemented: low volume, no hot path; `.md` justification files | `tests/Encina.LoadTests`, `tests/Encina.BenchmarkTests` |

Per-file per-flag targets with one-sentence justifications go into `.github/coverage-manifest/Encina.Compliance.NIS2.json`; targets only go up (AGENTS.md section 9). CRAP of new or changed methods is 10 or less; the aggregate `Apply` switch carries the `crap-exempt` marker.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
You are implementing Phase 7 (tests) of the supplier assessment workflow for Encina.Compliance.NIS2 (Issue #823).

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

- XML docs on every new public API (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`).
- `changelog.d/823-nis2-supplier-assessment.added.md`; never edit `CHANGELOG.md`.
- `PublicAPI.Unshipped.txt` for `Encina.Compliance.NIS2`.
- `docs/features/nis2-compliance.md` (supply chain section), `src/Encina.Compliance.NIS2/README.md`, `docs/INVENTORY.md` via `docs-writer`.
- No ADR: the design follows ADR-019 and the family ADR-064 of the #822 plan.
- Verification: `dotnet build --configuration Release` with 0 warnings; the five NIS2 test folders pass; every coverage flag reaches its manifest target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
You are finishing the supplier assessment workflow for Encina.Compliance.NIS2 (Issue #823).

CONTEXT:
- Code and tests are done; this phase closes documentation, public API tracking and verification.

TASK:
1. Complete XML docs and PublicAPI.Unshipped.txt entries.
2. Add the changelog fragment changelog.d/823-nis2-supplier-assessment.added.md.
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
| Art. 21(1) | Appropriate and proportionate technical, operational and organisational measures to manage risks to network and information systems | Frame of the workflow |
| Art. 21(2)(d) | Measures cover supply chain security, including relationships with direct suppliers and service providers | The assessment lifecycle |
| Art. 21(3) | Entities consider the vulnerabilities of each direct supplier and the quality of its cybersecurity practices | Criteria scores, certification tracking |
| Art. 22 | Union-level coordinated risk assessments of critical supply chains | Information only, no entity duty |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| AggregateBase (Guid id, RaiseEvent, Apply) | src/Encina.DomainModeling/AggregateBase.cs | Base class of the new aggregate |
| IAggregateRepository<TAggregate> | src/Encina.Marten | Load, save, create of event-sourced aggregates |
| DPIAAggregate, DPIAEvents, DPIAProjection | src/Encina.Compliance.DPIA | Reference pattern for periodic assessment aggregate, events and projection |
| DPIAReviewReminderService and DPIAOptions | src/Encina.Compliance.DPIA | Reference pattern for the opt-in overdue hosted service and its options |
| DPIAMartenExtensions (AddDPIAAggregates) | src/Encina.Compliance.DPIA | Reference pattern for aggregate registration |
| SupplierRiskLevel, SupplierInfo, SupplyChainAssessment, SupplierRisk | src/Encina.Compliance.NIS2/Model | Existing types; the risk enum is reused |
| NIS2Options.AddSupplier and SupplierConfiguration | src/Encina.Compliance.NIS2/NIS2Options.cs | Static supplier registry kept when the workflow is not registered |
| SupplyChainSecurityEvaluator and DefaultSupplyChainSecurityValidator | src/Encina.Compliance.NIS2 | Consumers of the static registry; 1/3/6/12 month intervals |
| NIS2Diagnostics and NIS2LogMessages | src/Encina.Compliance.NIS2/Diagnostics | ActivitySource, Meter, logger messages |
| NIS2ComplianceHealthCheck | src/Encina.Compliance.NIS2/Health | Health check pattern |
| AddNIS2EventSourcing and range 9800-9899 | Plan of issue 822 | Shared pieces owned by 822 |

### Event ID Allocation

| Package | Range | Notes |
| :--- | :--- | :--- |
| Encina.Compliance.NIS2 | 9200-9299 | Existing; 9288-9299 unused |
| Encina.Compliance.NIS2 (event-sourced modules) | 9800-9899 | Range `ComplianceNIS2EventSourced` registered by the plan of issue 822 |
| Issue 823 supply chain assessment | 9830-9849 | 20 ids, this plan |

### File Count Estimate

Per pull request, tests and metadata included (same figures as the split table; PublicAPI, manifest and `NIS2Errors` are touched in several PRs and count in each):

| PR | Files | Approx. lines |
| :--- | :--- | :--- |
| 1 Model, scheduler, stream id | 8 | 700 |
| 2 Events and aggregate | 8 | 1,350 |
| 3 Service, projection, read model | 12 | 1,350 |
| 4 Registration, options, evaluator and validator | 11 | 1,000 |
| 5 Monitor, health check, observability, docs | 13 | 1,000 |
| Total | 52 | 5,400 |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library (pre-1.0, no backward compatibility). You extend src/Encina.Compliance.NIS2 with an event-sourced supplier security assessment workflow (issue #823, NIS2 Art. 21(2)(d)). The store is Marten on PostgreSQL. Read AGENTS.md first. The EventId range, project references and AddNIS2EventSourcing come from the plan of issue 822.

IMPLEMENTATION OVERVIEW:
1. Model: SupplierAssessmentStatus, SupplierRiskCriteria, SupplierAssessmentScheduler, NIS2SupplierStreamId, commands.
2. Ten events (INotification) and SupplierAssessmentAggregate (AggregateBase, one stream per supplier).
3. INIS2SupplierAssessmentService + internal default over IAggregateRepository; NIS2Errors factories.
4. SupplierAssessmentProjection + ReadModel (inline, idempotent), overdue and certification queries.
5. Registration lines in AddNIS2EventSourcing, options, evaluator and validator read the register when registered.
6. NIS2SupplierAssessmentMonitorService (opt-in) and the health check addition.
7. Diagnostics and [LoggerMessage] ids 9830-9849.
8. Unit, guard, contract, property, integration tests; load and benchmark justifications.
9. Docs, PublicAPI, changelog fragment, coverage manifest. Deliver in the 5 pull requests of the split.

KEY PATTERNS:
- Mirror DPIAAggregate, DPIAEvents, DPIAProjection, DPIAReviewReminderService, DPIAMartenExtensions.
- ROP (ADR-053): aggregate commands return Either, an illegal transition is a Left with a rule code, never InvalidOperationException; EncinaError.Message never reaches logs; TimeProvider for time; PeriodicTimer loop catches per iteration.
- Singleton evaluator/validator never capture the scoped service: scope per call (Phase 4).
- NIS2ResilienceHelper returns a fallback on failure: never wrap store calls in it.
- Dilemmas 1, 2 and 3 are recommended, pending the maintainer: keep them isolated.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs
- src/Encina.Compliance.NIS2/Evaluators/SupplyChainSecurityEvaluator.cs
- src/Encina.Compliance.NIS2/DefaultSupplyChainSecurityValidator.cs
- docs/architecture/adr/019-compliance-event-sourcing-marten.md, 050-modules-may-require-modules.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Reads are inline-projection lookups, and a cached verdict could keep approving a suspended supplier; the issue's follow-up issue is not needed |
| 2 | OpenTelemetry | ✅ | Phase 6: activity per operation, `nis2.supplier_assessment.*` instruments on the package meter |
| 3 | Structured Logging | ✅ | Phase 6: `[LoggerMessage]` ids 9830-9849 of the range registered by the #822 plan |
| 4 | Health Checks | ✅ | Phase 5: overdue assessment or expired certification gives Degraded |
| 5 | Validation | ✅ | Commands validated in the service (blank ids, criteria out of range, past due dates) before any event |
| 6 | Resilience | ❌ | No external call; store calls are not wrapped in `NIS2ResilienceHelper`, which swallows failures |
| 7 | Distributed Locks | ⏭️ | Escalation is idempotent per due date and Marten optimistic concurrency protects duplicates (Design Choice 5); multi-instance coordination is deferred to [#1251](https://github.com/dlrivada/Encina/issues/1251), the issue ADR-050 names for it |
| 8 | Transactions | ✅ | The Marten session commits the event append atomically; no `TransactionPipelineBehavior` (ADR-050) |
| 9 | Idempotency | ✅ | Aggregate version and the per-due-date escalation guard |
| 10 | Multi-Tenancy | ✅ | `TenantId` in the events, aggregate, stream id and read model |
| 11 | Module Isolation | ✅ | `ModuleId` on events and read model, as `DPIAAggregate`; the issue left the decision to the plan |
| 12 | Audit Trail | ✅ | The event stream is the audit trail; assessor identity follows the shared personal-data decision |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|---|---|---|
| #822 plan, PR 1: range 9800-9899, references, `AddNIS2EventSourcing` | Planned | PR 1 of this plan depends on it |
| Personal-data decision (Dilemma 6 of the #822 plan) | Pending the maintainer | Assessor ids stay separate nullable properties until decided |
| #1603 NIS2 reads the system clock instead of `TimeProvider` | Open | New code takes `TimeProvider`; the old code is not touched here |
| #827 NIS2 reporting and read models | Open | Outside this family; no EventId block yet (9800-9899 is fully split among #822 to #826); the orchestrator assigns one in the batch registry |
| #2668 ADR-053 implementation and shared rule-code catalogue | Open | Not a blocker: this plan adds its code to `NIS2Errors` and moves it onto the catalogue when it lands |

---

## Next Steps

1. Maintainer answers Dilemmas 1, 2 and 3 when work on #823 starts (each is marked "recommended, pending the maintainer"; Dilemma 3 also decides #824 and #826).
2. Brief PR 1 after the #822 PR 1 merges; PR 2 to 5 follow in order. When the maintainer answers Dilemma 3, update the register statement of issue #823 (its Alternatives 1 says "not adopted" as Settled).
3. Run `check-plan.ps1` without `-Draft` after the adversarial review sets the `Verified` lines.
