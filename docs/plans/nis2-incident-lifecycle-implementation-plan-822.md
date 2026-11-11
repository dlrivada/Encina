# Implementation Plan: `Encina.Compliance.NIS2` — Incident Lifecycle with Phased Reporting (Art. 23)

> **Issue**: [#822](https://github.com/dlrivada/Encina/issues/822)
> **Type**: Feature
> **Complexity**: Very High (event-sourced aggregate, projection, monitor, 3 pull requests)
> **Estimated Scope**: ~2,200 lines of production code + ~1,600 lines of tests (about 31 files; split into 3 pull requests, see Phase 0)
> **Milestone**: v0.15.0 — EU Compliance: NIS2 & Digital Omnibus
> **Provider Category**: None for databases (single event-sourcing provider: Marten on PostgreSQL, ADR-027)
> **Family**: #822 to #826, five event-sourced modules of `Encina.Compliance.NIS2`. This plan owns the pieces the five share (see "Shared pieces of the family").

---

## Summary

`Encina.Compliance.NIS2` today is a stateless rule engine. `INIS2IncidentHandler.ReportIncidentAsync` validates timing, records metrics and forwards the incident to `IBreachNotificationService` when one is registered; nothing remembers which Art. 23(4) phase was sent, by whom, or whether a deadline was missed. This plan adds a persistent, event-sourced incident lifecycle, the NIS2 counterpart of the GDPR `BreachAggregate` (`src/Encina.Compliance.BreachNotification/Aggregates/BreachAggregate.cs`).

**What it implements** (NIS2 Directive (EU) 2022/2555, Art. 23):

- `NIS2IncidentAggregate` (an `AggregateBase`) with a state machine over `Detected -> EarlyWarningSent -> NotificationSent -> (IntermediateReportSent)* -> FinalReportSent -> Closed`. Escalation (deadline missed, severity or significance upgrade) is recorded as an event on top of the phase track.
- Deadlines computed once from the facts: 24 h and 72 h from awareness of the significant incident, one month from the incident notification (Art. 23(4)(a), (b), (d); the rules already coded in `NIS2Incident`).
- `INIS2IncidentLifecycleService` (record-keeping commands that append events), an inline Marten projection for the dashboard (`NIS2IncidentReadModel`), a deadline monitor that raises the escalation, a health check (overdue means Degraded) and OpenTelemetry metrics.

**Not in scope**: transmitting reports to a CSIRT (the service records what the entity submitted; see Design Choice 9 and the deferred issue file), the DORA incident infrastructure (#804), and changing `INIS2IncidentHandler` (it stays the stateless forwarder, as the issue's Alternatives section decides).

**Affected packages**: `Encina.Compliance.NIS2` (extended), `Encina.Marten`, `Encina.DomainModeling` and `Encina.Marten.GDPR` (project references added to the NIS2 package), `Encina` (EventId range registration only). The issue lists `NIS2IncidentId` in core; today's code does not need it (Design Choice 2).

**Provider category**: none of the 10 database providers, caching, transports, locks or validation providers applies; the aggregate store is Marten (AGENTS.md section 5 excludes event sourcing from the database matrix). The 12-function check is in the matrix at the end.

### Corrections to the issue text (checked against today's code)

| Issue text | Today's code | Plan decision |
|---|---|---|
| `EncinaAggregate<NIS2IncidentId>` | No such type. The base is `AggregateBase` (`Guid Id`) in `Encina.DomainModeling`; `BreachAggregate` extends it | Extend `AggregateBase`; no new strongly-typed id (Design Choice 2) |
| `NIS2IncidentStatus` includes `Escalated` | Escalation can happen in any phase and the deadlines keep running | `Escalated` is not a status; it is an event plus `EscalationCount` (Design Choice 4) |
| "Deadline calculator (24h, 72h, 1 month from detection)" vs table "1 month of notification" | `NIS2Incident.FinalReportDeadlineUtc` is `IncidentNotificationAtUtc + 1 month`, as Art. 23(4)(d) says | One month from the incident notification; the 24 h and 72 h clocks start at awareness of the significant incident |
| Metrics `encina.nis2.incidents.*` | The package meter `Encina.Compliance.NIS2` names instruments `nis2.*` | Instruments `nis2.incident.*` (Phase 6) |
| `NIS2IncidentSeverity` implied new | Exists (`Model/NIS2IncidentSeverity.cs`), as does `NIS2NotificationPhase` | Reuse both |

### Shared pieces of the family (#822 to #826)

This plan registers and owns the following; plans #823 to #826 reference it instead of repeating it. If another plan merges first, the pieces move there and this plan only references them.

| Shared piece | Decision | Owner |
|---|---|---|
| New NIS2 EventId range | `ComplianceNIS2EventSourced = (9800, 9899)` in `EventIdRanges.cs`, mapped to `Encina.Compliance.NIS2` in the `AssemblyRanges` of `EncinaEventIdAllocationTests`. The existing `ComplianceNIS2` range 9200-9299 has only 9288-9299 free (12 ids), which none of the five plans fits. Split inside the range: #822 9800-9829, #823 9830-9849, #824 9850-9869, #825 9870-9884, #826 9885-9899 | #822, PR 1 |
| Project references | `Encina.Marten` and `Encina.DomainModeling` (both already used by `Encina.Compliance.BreachNotification`), and `Encina.Marten.GDPR` if Dilemma 6 picks crypto-shredding | #822, PR 1 |
| Protection of personal data in events | Dilemma 6 below (recommended, pending the maintainer); the other four plans apply the outcome | #822 |
| Marten registration entry point | One extension `AddNIS2EventSourcing()` (class `NIS2MartenExtensions`) in the NIS2 package; each plan adds its own `AddAggregateRepository<T>()` / `AddProjection<,>()` line to it | #822 creates, others extend |
| ADR | ADR-064 "Event-sourced NIS2 modules" (shared range, references, personal-data outcome), written when Dilemma 6 is decided; 064 is the next free number today (last is 063) | #822 |

---

## Design Choices

<details>
<summary><strong>1. Package placement — extend <code>Encina.Compliance.NIS2</code></strong></summary>

- **Class**: Settled
- **Settled by**: issue #822 (Affected Packages: "existing package, extended"; Alternatives Considered rejects extending the handler); `Encina.Compliance.NIS2.csproj` already declares Art. 23 in its description.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extend `Encina.Compliance.NIS2`** | One package for Art. 23, shares `NIS2Options`, diagnostics, `NIS2Errors` | Adds Marten references to a package that is stateless today |
| B) New `Encina.Compliance.NIS2.Incidents` package | Marten stays optional for pure rule-engine users | New package for five modules would multiply (one per plan), more DI ceremony |

### Chosen Option: **A — Extend `Encina.Compliance.NIS2`**

### Rationale

- The package already references `Encina.Compliance.BreachNotification`, which references `Encina.Marten`, so Marten is a transitive dependency today; the explicit references change nothing for consumers.
- Opt-in is preserved at registration: nothing runs unless `AddNIS2EventSourcing()` (and the incident service registration) is called (AGENTS.md section 1, pay-for-what-you-use).

</details>

<details>
<summary><strong>2. Aggregate identity — <code>Guid</code> on <code>AggregateBase</code>, no <code>NIS2IncidentId</code></strong></summary>

- **Class**: Trivial
- **Settled by**: convention of every event-sourced compliance aggregate, for example `BreachAggregate : AggregateBase` (`src/Encina.Compliance.BreachNotification/Aggregates/BreachAggregate.cs`); `NIS2Incident.Id` is already a `Guid`; `IAggregateRepository<TAggregate>` constrains to `IAggregate`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AggregateBase` with `Guid`** | Same as all event-sourced modules, matches `NIS2Incident.Id`, Marten stream id is a Guid | Id mix-ups are possible at call sites |
| B) `AggregateBase<NIS2IncidentId>` with a `StronglyTypedId` | Type safety | A new core type, not used by the other modules, and a second id shape next to `NIS2Incident.Id` |

### Chosen Option: **A — `Guid`**

### Rationale

- Keeps the incident id identical in the stateless `NIS2Incident` and the aggregate, so the handler and the lifecycle can refer to the same incident.
- Core `Encina` needs no change (the issue's "Encina (core)" box is dropped).

</details>

<details>
<summary><strong>3. Status and phase vocabulary — reuse severity and phase enums, add <code>NIS2IncidentStatus</code></strong></summary>

- **Class**: Trivial
- **Settled by**: existing `NIS2IncidentSeverity` and `NIS2NotificationPhase` in `src/Encina.Compliance.NIS2/Model/`; `BreachStatus` for the status-enum convention.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse the two enums; new `NIS2IncidentStatus` (`Detected`, `EarlyWarningSent`, `NotificationSent`, `IntermediateReportSent`, `FinalReportSent`, `Closed`)** | One vocabulary for the stateless handler and the lifecycle | Status and phase overlap in meaning |
| B) Status derived only from the phase | No new enum | Cannot express `Detected` and `Closed` |

### Chosen Option: **A**

### Rationale

- `NIS2NotificationPhase` is the phase of a report; the status is where the incident stands. The projection and the health check need both.

</details>

<details>
<summary><strong>4. Escalation — an event and a counter, not a status</strong></summary>

- **Class**: Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `NIS2IncidentEscalated` event; `EscalationCount`, `LastEscalatedAtUtc` on the aggregate; status stays on the phase track** | Deadlines and the next permitted transition stay readable from `Status`; an incident can be escalated several times (deadline miss, then severity upgrade) | A dashboard filter "escalated" reads a flag, not a status |
| B) `Escalated` as a status (the issue's enum) | One field answers "is it escalated" | Leaves the phase track: the aggregate must remember the previous status to continue; a second escalation or a late report needs extra rules |

### Chosen Option: **A — event plus counter (recommended, pending the maintainer)**

### Rationale

- Better when the monitor escalates a missed 24 h warning and the entity still sends it late: the state machine keeps accepting the next legal transition.
- B wins when the authority's own process treats "escalated" as a distinct stage that operators filter by; the read model can still expose `IsEscalated`, so the cost of A is small.

</details>

<details>
<summary><strong>5. Deadline rules and significance — computed from facts in a pure calculator</strong></summary>

- **Class**: Settled
- **Settled by**: NIS2 Directive Art. 23(3) and 23(4)(a), (b), (d); the equivalent rules already in `NIS2Incident` (`EarlyWarningDeadlineUtc`, `IncidentNotificationDeadlineUtc`, `FinalReportDeadlineUtc`).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pure static `NIS2DeadlineCalculator` (no clock) used by the aggregate and the projection** | Deterministic replay; one place for the rules; property-testable | Callers pass timestamps |
| B) Compute inside each `Apply` | No extra type | Rules duplicated in aggregate, projection and monitor |
| C) Store deadlines as event data only | Replay needs no calculator | A rule change cannot be audited against the event |

### Chosen Option: **A — `NIS2DeadlineCalculator`**

### Rationale

- The 24 h and 72 h clocks start at awareness of the significant incident (Art. 23(4)(a)): `DetectedAtUtc` when the incident is significant at detection, or the time of the `NIS2IncidentEscalated` event with reason `BecameSignificant` when it is upgraded later. A non-significant incident has no reporting deadlines and may close directly from `Detected`.
- The final report is due one month (`AddMonths(1)`) after `NotificationSentAtUtc` (Art. 23(4)(d)). Art. 23(4)(e), the progress report when the incident is still ongoing, is covered by `IntermediateReportSent` reports, which are allowed any number of times between the notification and the final report; the final-report deadline is not moved.
- Events store timestamps given by the caller from `TimeProvider`; the aggregate never reads a clock (AGENTS.md section 3).

</details>

<details>
<summary><strong>6. Personal data in the events — crypto-shredding decision shared by the family</strong></summary>

- **Class**: Dilemma

The incident events carry the identifier of the person who acted (`ReportedByUserId`), the contact person given to the authority (name, email), and free text (description, indicators of compromise, which can include IP addresses, personal data under GDPR). The event store is append-only, so anything written in plaintext cannot be erased later (ADR-034, Context). Art. 17(3)(b) GDPR lets a controller keep data needed for a legal obligation (the NIS2 audit trail), but not beyond it.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A) No crypto-shredding: events hold only pseudonymous actor ids; free text is documented as personal-data-free; retention by the legal-obligation exemption | No dependency on `Encina.Marten.GDPR` or a key store; simplest | Contact names/emails and IP addresses in free text stay readable forever; an erasure request after the retention period cannot be honoured |
| **B) `[CryptoShredded]` on the actor and contact fields (`SubjectIdProperty` = the actor id); free text stays plain and documented** | Erasure of a person's identity without touching the audit facts; follows ADR-034 and the `IForgottenSubjectHandler` flow | Adds `Encina.Marten.GDPR` and a persistent key store (#1191); a key-store outage stops reads (fail closed); the in-memory key store loses data on restart |
| C) Per-incident key: `[CryptoShredded]` on all personal fields with the incident id as subject | The whole incident can be shredded at the end of its retention period | Not tied to a person, so a data-subject request cannot be served; every read needs the incident key |

### Chosen Option: **B — actor and contact fields shredded per actor (recommended, pending the maintainer)**

### Rationale

- B is better when the entity must honour an erasure request from a reporter or contact person while keeping the incident record, the case this regulation pair produces.
- A is better for a deployment with no Marten.GDPR key store and only pseudonymous ids; C is better when the retention policy, not a person, ends the record.
- The outcome applies to all five plans: #823 to #826 mark the same kind of fields (actor ids, contact data) the same way. Until decided, events are designed so every personal field is a separate, nullable property that can gain the attribute without changing the event shape.

</details>

<details>
<summary><strong>7. Deadline-miss detection — a hosted monitor with per-iteration error handling</strong></summary>

- **Class**: Settled
- **Settled by**: the issue (Health Checks: "Overdue incidents count as degraded health"; event `NIS2IncidentEscalated` triggered by "Deadline missed"); AGENTS.md section 3 (background loops: a `PeriodicTimer` loop catches per iteration, `OperationCanceledException` ends the loop); reference `src/Encina.Compliance.BreachNotification/BreachDeadlineMonitorService.cs`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `NIS2DeadlineMonitorService` (`BackgroundService`, `PeriodicTimer`) querying the projection** | Detects a miss even when nobody calls the service; feeds the metrics and health check | Needs multi-instance care |
| B) Evaluate only when a command or query touches the incident | No background service | A silent incident never escalates, the purpose of the feature |

### Chosen Option: **A — monitor**

### Rationale

- Idempotency instead of a lock: the monitor appends `NIS2IncidentEscalated(DeadlineMissed, phase)` only if the aggregate has not yet escalated for that phase (`EscalatedForPhases`), and Marten optimistic concurrency makes a duplicate from a second instance a handled `Left` (ADR-050 requires multi-instance coordination for event sourcing; a distributed lock is deferred to the cross-module work tracked by that ADR, see matrix row 7).
- A failed query or append is logged by error code and retried on the next iteration; it never ends the loop. Registered only by the opt-in registration (default off).

</details>

<details>
<summary><strong>8. Service contract — <code>ValueTask&lt;Either&lt;EncinaError, T&gt;&gt;</code> over <code>IAggregateRepository</code></strong></summary>

- **Class**: Trivial
- **Settled by**: `IBreachNotificationService` (`ValueTask<Either<EncinaError, Unit>>` methods over `IAggregateRepository<BreachAggregate>`); ADR-001 and AGENTS.md section 3 (Railway Oriented Programming); the issue's interface sketch.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Service with one method per transition; invalid transition returns `Left` (`NIS2Errors`), the aggregate throws `InvalidOperationException` only as a guard the service checks first** | Same as `BreachNotificationService`; no exceptions for business rules | The service pre-checks what the aggregate enforces |
| B) Commands through the Encina pipeline only | No service interface | The issue and the Test Matrix require the service contract |

### Chosen Option: **A**

### Rationale

- The aggregate methods enforce the state machine (reference: `BreachAggregate.Assess` throws `InvalidOperationException`); the service maps a wrong state to `NIS2Errors.IncidentInvalidTransition(...)` before calling, so callers get `Either`, never an exception for a business rule.

</details>

<details>
<summary><strong>9. CSIRT transmission — record-keeping only in this feature</strong></summary>

- **Class**: Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The service records the report the entity already submitted (reference number, channel, timestamps); transmission is a separate follow-up** | Fits entities that file through a national portal or the planned ENISA single entry point; no no-op sender shipped | The framework does not send the report; resilience for the CSIRT call is not needed here |
| B) `INIS2IncidentSubmitter` abstraction with a default implementation, called by the service before the event | One call sends and records | No national API exists to implement against; a default would be a no-op, the pattern AGENTS.md section 3 and SPEC-002 treat as a known gap outside Development (#2391) |

### Chosen Option: **A — record-keeping; transmission stays with #812 (recommended, pending the maintainer)**

### Rationale

- A is better today: Spain has not transposed NIS2 (SPEC-002 section on NIS2) so there is no national submission API; B is better once a CSIRT interface or the single entry point exists, which is the scope of #812.
- The resilience row of the issue is therefore not applicable here: the Marten calls are not wrapped in `NIS2ResilienceHelper`, because that helper returns its fallback on any failure (`NIS2ResilienceHelper.cs`, the `catch (Exception)` branches) and would report a failed append as success, which AGENTS.md section 3 forbids. Transmission belongs to the existing issue #812 (adapter pattern for incident reporting to external authorities, Digital Omnibus single entry point), so no new issue is needed.

</details>

<details>
<summary><strong>10. Shared range, references and registration entry point</strong></summary>

- **Class**: Settled
- **Settled by**: AGENTS.md section 7 (a range is registered in `EventIdRanges.cs` before use, EventIds packed sequentially inside the package range, the assembly added to `AssemblyRanges` of `EncinaEventIdAllocationTests`); `EventIdRanges.cs` today (9200-9299 `ComplianceNIS2`, 9750-9799 `SecurityBreakTheGlass`, nothing registered at 9800-9999).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A second NIS2 range 9800-9899 split by plan** | Room for the five plans (100 ids against 12 left) | Two ranges for one assembly |
| B) Use 9288-9299 | No new range | 12 ids; this plan alone needs about 25 |
| C) A range per plan | Independent | Five registrations and five map entries for one package |

### Chosen Option: **A — `ComplianceNIS2EventSourced` (9800, 9899)**

### Rationale

- `AssemblyRanges` accepts an array per assembly, so `Encina.Compliance.NIS2` maps to `[nameof(EventIdRanges.ComplianceNIS2), nameof(EventIdRanges.ComplianceNIS2EventSourced)]`.
- The block starts after the `SecurityBreakTheGlass` range with no overlap; the registry is the source of truth, so the worker re-checks the free space before registering.

</details>

---

## Implementation Phases

### Pull request split (read first)

<details>
<summary>Tasks</summary>

The feature is about 31 files, over the limit of 15 files and 1,500 lines per pull request (AGENTS.md section 11). Split:

| PR | Phases | Content | Files / lines (approx.) |
|---|---|---|---|
| 1 | 1 | Range registration, project references, model, commands, calculator, events, aggregate, unit/guard/property tests for the domain | 12 files / 1,400 |
| 2 | 2, 3, 4 | Service, errors, projection and read model, Marten extension, DI, contract and integration tests | 11 files / 1,450 |
| 3 | 5, 6, 7 (rest), 8 | Monitor, health check, diagnostics additions, cross-cutting wiring, remaining tests, docs, manifest | 10 files / 1,200 |

Each PR keeps zero warnings and its own per-flag targets from `.github/coverage-manifest/Encina.Compliance.NIS2.json`.

</details>

### Phase 1: Core Model, Events & Aggregate (PR 1)

<details>
<summary>Tasks</summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs`: add `public static readonly (int Min, int Max) ComplianceNIS2EventSourced = (9800, 9899);` with an XML summary naming the five issues; `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`: map `Encina.Compliance.NIS2` to both ranges; add the field to the `Encina` `PublicAPI.Unshipped.txt`.
2. `src/Encina.Compliance.NIS2/Encina.Compliance.NIS2.csproj`: add `ProjectReference` to `Encina.Marten`, `Encina.DomainModeling` (and `Encina.Marten.GDPR` if Dilemma 6 resolves to B).
3. `Model/NIS2IncidentStatus.cs`: `enum NIS2IncidentStatus { Detected, EarlyWarningSent, NotificationSent, IntermediateReportSent, FinalReportSent, Closed }`.
4. `Model/NIS2EscalationReason.cs`: `enum { DeadlineMissed, SeverityUpgraded, BecameSignificant }`.
5. `Model/NIS2DeadlineCalculator.cs`: `internal static class` with `EarlyWarningDeadline(DateTimeOffset awareAtUtc)`, `NotificationDeadline(...)`, `FinalReportDeadline(DateTimeOffset notificationAtUtc)`.
6. `Events/NIS2IncidentEvents.cs`: sealed records implementing `INotification` (as `BreachNotificationEvents`): `NIS2IncidentDetected`, `NIS2EarlyWarningSent`, `NIS2IncidentNotificationSent`, `NIS2IntermediateReportSent`, `NIS2FinalReportSent`, `NIS2IncidentEscalated`, `NIS2IncidentClosed`. Every event carries `IncidentId`, a `...AtUtc` timestamp, `TenantId`, `ModuleId` where the reference events do; personal fields are separate nullable properties (Design Choice 6).
7. `Aggregates/NIS2IncidentAggregate.cs`: `public sealed class NIS2IncidentAggregate : AggregateBase`; static `Detect(...)`; instance `SendEarlyWarning`, `SendNotification`, `SendIntermediateReport`, `SendFinalReport`, `Escalate`, `Close`; `protected override void Apply(object)` switch; properties `Status`, `Severity`, `IsSignificant`, `DetectedAtUtc`, `AwareAtUtc`, `EarlyWarningDeadlineUtc`, `NotificationDeadlineUtc`, `FinalReportDeadlineUtc`, `EscalationCount`, `TenantId`, `ModuleId`. Allowed transitions: `Detected -> EarlyWarningSent`; `EarlyWarningSent -> NotificationSent`; `NotificationSent` or `IntermediateReportSent -> IntermediateReportSent`; `NotificationSent` or `IntermediateReportSent -> FinalReportSent`; `FinalReportSent -> Closed`; `Detected -> Closed` only when not significant; `Escalate` from any non-closed status.
8. Tests: unit (every legal transition, every illegal one, replay from history, deadline values), guard (public methods), property (no sequence of commands skips a phase; deadlines always set when significant).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
You are implementing Phase 1 of the incident lifecycle for Encina.Compliance.NIS2 (Issue #822).

CONTEXT:
- Encina is .NET 10 / C# 14, Either<EncinaError, T> for business results, TimeProvider for time (the aggregate takes timestamps as parameters).
- Extend src/Encina.Compliance.NIS2; reuse Model/NIS2IncidentSeverity.cs and Model/NIS2NotificationPhase.cs.
- Pattern to follow: src/Encina.Compliance.BreachNotification/Aggregates/BreachAggregate.cs and Events/BreachNotificationEvents.cs.

TASK:
1. Register EventIdRanges.ComplianceNIS2EventSourced = (9800, 9899) FIRST and map it in EncinaEventIdAllocationTests.
2. Add the project references named in the plan.
3. Create the enums, NIS2DeadlineCalculator, the 7 events and NIS2IncidentAggregate with the transitions of Phase 1 task 7.
4. Add PublicAPI.Unshipped.txt lines; XML docs on every public member.
5. Write unit, guard and property tests that instantiate the real aggregate.

KEY RULES:
- Aggregate extends AggregateBase, Guid Id; no clock reads; invalid transition throws InvalidOperationException (guard), the service maps it to Left in Phase 2.
- 24 h / 72 h from awareness of the significant incident, one month from the notification (Art. 23(4)).
- No [Obsolete], no compatibility code; zero warnings.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Aggregates/BreachAggregate.cs
- src/Encina.Compliance.NIS2/Model/NIS2Incident.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 2: Service, Errors & Default Implementation (PR 2)

<details>
<summary>Tasks</summary>

1. `Abstractions/INIS2IncidentLifecycleService.cs`: `DetectIncidentAsync`, `SendEarlyWarningAsync`, `SendNotificationAsync`, `SendIntermediateReportAsync`, `SendFinalReportAsync`, `EscalateAsync`, `CloseAsync`, each `ValueTask<Either<EncinaError, ...>>` with `CancellationToken`; command records `NIS2IncidentDetectionCommand`, `NIS2EarlyWarningCommand`, `NIS2IncidentNotificationCommand`, `NIS2IntermediateReportCommand`, `NIS2FinalReportCommand`, `NIS2IncidentClosureCommand` in `Model/` (validated: required text not blank).
2. `Services/DefaultNIS2IncidentLifecycleService.cs` (internal): constructor takes `IAggregateRepository<NIS2IncidentAggregate>`, `TimeProvider`, `IOptions<NIS2Options>`, `ILogger`. Loads, pre-checks the transition, calls the aggregate, saves; maps repository `Left` unchanged; never swallows a `Left`.
3. `NIS2Errors.cs`: add `IncidentNotFound`, `IncidentInvalidTransition`, `IncidentCommandInvalid`, `IncidentEventStoreFailed` using the existing error-code scheme.
4. Tenant and module ids come from `ITenantContext`/`IModuleContext` when registered (Phase 5).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
You are implementing Phase 2 of the incident lifecycle for Encina.Compliance.NIS2 (Issue #822).

CONTEXT:
- Phase 1 delivered NIS2IncidentAggregate and events. This phase adds the service over IAggregateRepository<NIS2IncidentAggregate>.
- Pattern: src/Encina.Compliance.BreachNotification/Services/DefaultBreachNotificationService.cs and Abstractions/IBreachNotificationService.cs.

TASK:
1. Create INIS2IncidentLifecycleService and the command records.
2. Create the internal default implementation; time from TimeProvider; call the repository directly (NIS2ResilienceHelper returns a fallback on failure and would hide a failed append).
3. Add the NIS2Errors factories; every failure is Left with an error code, never an exception for a business rule.
4. Unit tests with NSubstitute for IAggregateRepository; guard tests; a contract test of the service.

KEY RULES:
- ROP: a Left from the repository fails the operation; log error codes, never EncinaError.Message; exceptions through ForLogging().
- Pre-check the status so an illegal transition is Left(IncidentInvalidTransition).

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Services/DefaultBreachNotificationService.cs
- src/Encina.Compliance.NIS2/NIS2Errors.cs
- src/Encina.Compliance.NIS2/NIS2Errors.cs
```

</details>

### Phase 3: Projection & Read Model (PR 2)

<details>
<summary>Tasks</summary>

1. `ReadModels/NIS2IncidentReadModel.cs`: id, status, severity, `IsSignificant`, the three deadlines, `EscalationCount`, last-report timestamps, `TenantId`, `ModuleId`.
2. `ReadModels/NIS2IncidentProjection.cs`: inline projection (`IProjectionCreator<NIS2IncidentDetected, ...>` plus one `IProjectionHandler` per remaining event), idempotent like `BreachProjection`.
3. Query surface used by the monitor and health check: the same read-model repository abstraction `BreachDeadlineMonitorService` uses, filtered by overdue and active incidents (confirm the abstraction in `BreachNotificationHealthCheck` before coding).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
You are implementing Phase 3 of the incident lifecycle for Encina.Compliance.NIS2 (Issue #822).

CONTEXT:
- Mirror src/Encina.Compliance.BreachNotification/ReadModels/BreachProjection.cs and BreachReadModel.cs.

TASK:
1. Create NIS2IncidentReadModel and NIS2IncidentProjection handling the 7 events.
2. Keep the projection idempotent so replay produces the same model.
3. Unit-test every handler with real instances.

KEY RULES:
- Projections take dependencies by constructor, never IServiceProvider (AGENTS.md section 3, event-sourced modules).
- Timestamps use the AtUtc suffix.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/ReadModels/BreachProjection.cs
```

</details>

### Phase 4: Configuration, DI & Marten Registration (PR 2)

<details>
<summary>Tasks</summary>

1. `NIS2MartenExtensions.cs`: `AddNIS2EventSourcing(this IServiceCollection)` calling `AddAggregateRepository<NIS2IncidentAggregate>()` and `AddProjection<NIS2IncidentProjection, NIS2IncidentReadModel>()` (reference: `BreachNotificationMartenExtensions.AddBreachNotificationAggregates`).
2. `ServiceCollectionExtensions.cs`: register `INIS2IncidentLifecycleService` with `TryAddScoped` inside `AddEncinaNIS2` only when the event-sourcing registration is used, or fail at registration with a clear error (AGENTS.md section 3, registration completeness: every dependency the service resolves is registered).
3. `NIS2Options.cs`: `IncidentDeadlineCheckInterval` (default 5 minutes) and `EnableIncidentDeadlineMonitoring` (default `false`); update `NIS2OptionsValidator`.
4. DI test: build the provider with `ValidateOnBuild` and `ValidateScopes` for every registration combination.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
You are implementing Phase 4 of the incident lifecycle for Encina.Compliance.NIS2 (Issue #822).

CONTEXT:
- Pattern: BreachNotificationMartenExtensions.cs and ServiceCollectionExtensions.cs of both packages.

TASK:
1. Add AddNIS2EventSourcing (shared entry point of the family) and register the lifecycle service.
2. Add the two options and validate them.
3. Write the DI test with ValidateOnBuild and ValidateScopes.

KEY RULES:
- TryAdd for every default; opt-in only; PublicAPI.Unshipped.txt lines for new public members.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/BreachNotificationMartenExtensions.cs
- src/Encina.Compliance.NIS2/ServiceCollectionExtensions.cs
- src/Encina.Compliance.NIS2/NIS2Options.cs
```

</details>

### Phase 5: Cross-Cutting Integration, Deadline Monitor & Health Check (PR 3)

<details>
<summary>Tasks</summary>

1. `NIS2DeadlineMonitorService.cs` (`BackgroundService`, `PeriodicTimer(IncidentDeadlineCheckInterval)`): per iteration queries overdue incidents from the projection, loads each aggregate and calls `Escalate(DeadlineMissed, phase)` when the phase has not been escalated; per-iteration `try/catch`, cancellation ends the loop, failures logged by error code.
2. `Health/NIS2IncidentHealthCheck.cs`: `DefaultName` constant, static `Tags`, scoped resolution through `IServiceProvider.CreateScope()`; overdue incidents -> `Degraded`, store failure -> `Unhealthy`.
3. Tenant and module scoping: `TenantId` and `ModuleId` set from `ITenantContext`/`IModuleContext` at detection and filterable on the read model.
4. Operation audit: the event store is the audit trail; no second store (matrix row 12).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
You are implementing Phase 5 of the incident lifecycle for Encina.Compliance.NIS2 (Issue #822).

CONTEXT:
- Pattern: BreachDeadlineMonitorService.cs and Health/BreachNotificationHealthCheck.cs in Encina.Compliance.BreachNotification; Health/NIS2ComplianceHealthCheck.cs in this package.

TASK:
1. Create the monitor and the health check described in the plan.
2. Register both only when EnableIncidentDeadlineMonitoring / AddHealthCheck are true.
3. Tests: monitor with FakeTimeProvider and a throwing store (the loop must survive and the failure must be logged, never swallowed in the test), health check status mapping.

KEY RULES:
- A PeriodicTimer loop catches per iteration; OperationCanceledException from the stopping token ends the loop (AGENTS.md section 3).
- Errors by code or ForLogging(); never EncinaError.Message.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/BreachDeadlineMonitorService.cs
- src/Encina.Compliance.NIS2/Health/NIS2ComplianceHealthCheck.cs
```

</details>

### Phase 6: Observability (PR 3)

<details>
<summary>Tasks</summary>

1. `Diagnostics/NIS2Diagnostics.cs`: reuse `ActivitySource`/`Meter` `Encina.Compliance.NIS2`; add `StartIncidentTransition(...)` activity and instruments `nis2.incident.transitions.total` (Counter, tags status, phase), `nis2.incident.deadline_misses.total` (Counter, tag phase), `nis2.incident.report_latency` (Histogram, hours from awareness to report, tag phase), `nis2.incident.active` and `nis2.incident.overdue` (ObservableGauge reading the last monitor sweep, so a scrape never queries the store).
2. `Diagnostics/NIS2LogMessages.cs`: new `[LoggerMessage]` methods with EventIds 9800-9829 packed from 9800 (detected, each phase sent, escalated, closed, invalid transition, store failure, monitor started/stopped/iteration failed, overdue found); the XML summary names the range (`see EventIdRanges.ComplianceNIS2EventSourced`).
3. Run the architecture tests (`EncinaEventIdAllocationTests`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
You are implementing Phase 6 of the incident lifecycle for Encina.Compliance.NIS2 (Issue #822).

CONTEXT:
- Phases 1 to 5 delivered the aggregate, service, projection, monitor and health check; this phase instruments them.

TASK:
1. Add the activity, instruments and logger messages described in the plan, EventIds 9800-9829 packed sequentially.
2. Record metrics in the service and monitor; the gauges read a snapshot set by the monitor.
3. Run the EventId architecture tests.

KEY RULES:
- Range registered first (Phase 1); [LoggerMessage] source generator; no sparse ids.
- Tags are low cardinality: status and phase, never the incident id.

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
| Unit | Aggregate transitions and replay, calculator, service with a substituted repository, projection handlers, monitor, health check | `tests/Encina.UnitTests/Compliance/NIS2/` |
| Guard | Public methods and constructors of aggregate, service, projection, extensions | `tests/Encina.GuardTests/Compliance/NIS2/` |
| Contract | `INIS2IncidentLifecycleService`: every method returns `Left` for an illegal transition and never throws | `tests/Encina.ContractTests/Compliance/NIS2/` |
| Property | FsCheck: random command sequences never skip a phase; deadlines set when significant; replay equals live state | `tests/Encina.PropertyTests/Compliance/NIS2/` |
| Integration | Full lifecycle with Marten on PostgreSQL through the shared Testcontainers fixture; projection updated inline; concurrent escalation yields one handled `Left` | `tests/Encina.IntegrationTests/Compliance/NIS2/` |
| Load / Benchmark | Not implemented: incident volume is low and nothing here is a hot path; `.md` justification files in the load and benchmark projects | `tests/Encina.LoadTests`, `tests/Encina.BenchmarkTests` |

Per-flag targets are added to `.github/coverage-manifest/Encina.Compliance.NIS2.json` per new file (unit and guard high for the aggregate and calculator, integration for the service and projection), each with a one-sentence justification; the package targets only go up (AGENTS.md section 9). CRAP of every new or changed method must be 10 or less; the aggregate `Apply` switch is a single-question switch and carries the `crap-exempt` marker.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
You are implementing Phase 7 (tests) of the incident lifecycle for Encina.Compliance.NIS2 (Issue #822).

CONTEXT:
- Tests live in the consolidated projects under tests/ (Encina.UnitTests, GuardTests, ContractTests, PropertyTests, IntegrationTests), folder Compliance/NIS2.

TASK:
1. Write unit, guard, contract, property and integration tests as listed in the plan, one set per pull request for the code that PR adds.
2. Integration tests use a shared [Collection] fixture for PostgreSQL and clear data in InitializeAsync; never a per-class fixture.
3. Add manifest targets per new file and run the per-flag coverage commands of AGENTS.md section 9.

KEY RULES:
- Tests execute real package code; assertions with Shouldly through Encina.Testing.Shouldly.
- A test never swallows an exception thrown by the code it drives.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Compliance/BreachNotification/BreachNotificationAggregateIntegrationTests.cs
- tests/Encina.UnitTests/Compliance/NIS2/DefaultNIS2IncidentHandlerTests.cs
- .github/coverage-manifest/Encina.Compliance.NIS2.json
```

</details>

### Phase 8: Documentation & Finalization (PR 3)

<details>
<summary>Tasks</summary>

- XML docs on every new public API (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`).
- `changelog.d/822-nis2-incident-lifecycle.added.md` (one fragment; never edit `CHANGELOG.md`).
- `PublicAPI.Unshipped.txt` for `Encina` and `Encina.Compliance.NIS2`.
- `docs/features/nis2-compliance.md` (new section: lifecycle, state machine diagram, deadlines, registration), `src/Encina.Compliance.NIS2/README.md`, `docs/INVENTORY.md`, `ROADMAP.md` if the milestone line changes, via `docs-writer`.
- ADR-064 once Dilemma 6 is decided.
- Verification: `dotnet build --configuration Release` with 0 warnings; `dotnet test` for the five NIS2 test folders; every coverage flag reaches its manifest target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
You are finishing the incident lifecycle for Encina.Compliance.NIS2 (Issue #822).

CONTEXT:
- Code and tests are done; this phase closes documentation, public API tracking and verification.

TASK:
1. Complete XML docs and PublicAPI.Unshipped.txt entries.
2. Add the changelog fragment changelog.d/822-nis2-incident-lifecycle.added.md.
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
| Art. 23(1) | Notify the CSIRT or competent authority of a significant incident without undue delay | Yes |
| Art. 23(3) | Defines a significant incident (severe disruption, financial loss, considerable damage to others) | Yes, `IsSignificant` |
| Art. 23(4)(a) | Early warning within 24 hours of awareness | Yes |
| Art. 23(4)(b) | Incident notification within 72 hours of awareness | Yes |
| Art. 23(4)(c) | Intermediate report upon request of the CSIRT or authority | Yes |
| Art. 23(4)(d) | Final report not later than one month after the incident notification | Yes |
| Art. 23(4)(e) | Progress report and final report one month after handling when the incident is ongoing | Partly: intermediate reports; deadline not moved (Design Choice 5) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| AggregateBase | src/Encina.DomainModeling/AggregateBase.cs | Base class of the new aggregate |
| IAggregateRepository<TAggregate> | src/Encina.Marten | Load, save, create of event-sourced aggregates |
| BreachAggregate and BreachNotificationEvents | src/Encina.Compliance.BreachNotification/Aggregates and Events | Reference pattern for aggregate and events |
| BreachProjection and BreachReadModel | src/Encina.Compliance.BreachNotification/ReadModels | Reference pattern for the inline projection |
| BreachNotificationMartenExtensions | src/Encina.Compliance.BreachNotification | Reference pattern for Marten registration |
| BreachDeadlineMonitorService | src/Encina.Compliance.BreachNotification | Reference pattern for the deadline monitor |
| NIS2Incident, NIS2IncidentSeverity, NIS2NotificationPhase | src/Encina.Compliance.NIS2/Model | Existing types reused; deadline rules already coded |
| INIS2IncidentHandler and DefaultNIS2IncidentHandler | src/Encina.Compliance.NIS2 | Stateless handler kept unchanged |
| NIS2Diagnostics and NIS2LogMessages | src/Encina.Compliance.NIS2/Diagnostics | ActivitySource, Meter, logger messages |
| NIS2ResilienceHelper | src/Encina.Compliance.NIS2 | Not used here: returns a fallback on any failure (see Design Choice 9) |
| NIS2ComplianceHealthCheck | src/Encina.Compliance.NIS2/Health | Health check pattern |
| CryptoShreddedAttribute | src/Encina.Marten.GDPR/Attributes | Field-level encryption of event properties (ADR-034), if Dilemma 6 picks B |
| EventIdRanges | src/Encina/Diagnostics/EventIdRanges.cs | EventId registry (ADR-021) |

### Event ID Allocation

| Package | Range | Notes |
| :--- | :--- | :--- |
| Encina.Compliance.NIS2 | 9200-9299 | Existing; 9200-9287 used, 9288-9299 unused (12 ids) |
| Encina.Compliance.NIS2 (event-sourced modules) | 9800-9899 | New range `ComplianceNIS2EventSourced`, registered by this plan |
| Issue 822 incident lifecycle | 9800-9829 | 30 ids |
| Issue 823 supply chain | 9830-9849 | 20 ids |
| Issue 824 risk analysis | 9850-9869 | 20 ids |
| Issue 825 coordinated vulnerability disclosure | 9870-9884 | 15 ids |
| Issue 826 management accountability | 9885-9899 | 15 ids |

### File Count Estimate

| Category | Files | Approx. lines |
| :--- | :--- | :--- |
| Production: model, enum, commands, deadline calculator | 4 | 350 |
| Production: aggregate and events | 2 | 650 |
| Production: lifecycle service abstraction and default implementation | 2 | 450 |
| Production: read model and projection | 2 | 300 |
| Production: Marten registration extensions, options and DI changes | 2 | 120 |
| Production: deadline monitor, health check, diagnostics and log messages additions | 3 | 350 |
| Tests: unit | 5 | 900 |
| Tests: guard | 2 | 200 |
| Tests: contract | 1 | 120 |
| Tests: property | 1 | 150 |
| Tests: integration (Marten, PostgreSQL) | 1 | 250 |
| Docs and metadata: PublicAPI, coverage manifest, changelog fragment, feature page, README, ADR | 6 | 250 |
| Total | 31 | 4090 |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library (pre-1.0, no backward compatibility). You extend src/Encina.Compliance.NIS2, today a stateless rule engine, with an event-sourced NIS2 Art. 23 incident lifecycle (issue #822). The aggregate store is Marten on PostgreSQL. Read AGENTS.md first.

IMPLEMENTATION OVERVIEW:
1. Register EventIdRanges.ComplianceNIS2EventSourced (9800-9899) and add the Marten and DomainModeling references.
2. Model: NIS2IncidentStatus, NIS2EscalationReason, NIS2DeadlineCalculator, command records.
3. Events (7, INotification) and NIS2IncidentAggregate (AggregateBase, state machine).
4. INIS2IncidentLifecycleService + internal default over IAggregateRepository; NIS2Errors factories.
5. NIS2IncidentProjection + NIS2IncidentReadModel (inline, idempotent).
6. AddNIS2EventSourcing, options, DI test with ValidateOnBuild/ValidateScopes.
7. NIS2DeadlineMonitorService, NIS2IncidentHealthCheck, tenant and module scoping.
8. Diagnostics (activity, counters, histogram, gauges) and [LoggerMessage] ids 9800-9829.
9. Unit, guard, contract, property and integration tests; load and benchmark justifications.
10. Docs, PublicAPI, changelog fragment, coverage manifest. Deliver in the 3 pull requests of Phase 0.

KEY PATTERNS:
- Mirror BreachAggregate, BreachNotificationEvents, BreachProjection, BreachNotificationMartenExtensions, BreachDeadlineMonitorService.
- ROP: Either<EncinaError, T>; EncinaError.Message never reaches logs; TimeProvider for time; PeriodicTimer loop catches per iteration.
- 24 h / 72 h from awareness of the significant incident, one month from the notification.
- Personal fields separate and nullable until Dilemma 6 is decided.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Aggregates/BreachAggregate.cs
- src/Encina.Compliance.NIS2/Model/NIS2Incident.cs
- src/Encina.Compliance.NIS2/DefaultNIS2IncidentHandler.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/architecture/adr/019-compliance-event-sourcing-marten.md, 034-crypto-shredding-through-the-stj-contract.md, 050-modules-may-require-modules.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Incident state must always be read from the event store or the inline projection; a cache would hide a missed deadline |
| 2 | OpenTelemetry | ✅ | Phase 6: activity per transition, `nis2.incident.*` instruments on the package meter |
| 3 | Structured Logging | ✅ | Phase 6: `[LoggerMessage]` in the new range 9800-9829 |
| 4 | Health Checks | ✅ | Phase 5: overdue incidents give Degraded, store failure Unhealthy |
| 5 | Validation | ✅ | Command records validated in the service (blank text, empty services list) before any event is raised |
| 6 | Resilience | ❌ | No external call: CSIRT transmission is out of scope (Design Choice 9; tracked by #812). The Marten calls are not wrapped in `NIS2ResilienceHelper` because it swallows failures |
| 7 | Distributed Locks | ⏭️ | Escalation is idempotent and Marten optimistic concurrency protects duplicates (Design Choice 7); multi-instance coordination for event-sourced modules follows ADR-050 and its tracking issues; no new issue needed |
| 8 | Transactions | ✅ | The Marten session commits the event append atomically; no `TransactionPipelineBehavior` (ADR-050 forbids that pairing with Marten) |
| 9 | Idempotency | ✅ | Aggregate version (optimistic concurrency) and the per-phase escalation guard; command retries after a conflict reload the aggregate |
| 10 | Multi-Tenancy | ✅ | `TenantId` on the events, aggregate and read model, filter on queries |
| 11 | Module Isolation | ✅ | `ModuleId` on events and read model (the issue deferred it; the reference events already carry it, so it costs one property); module-scoped queries are included |
| 12 | Audit Trail | ✅ | The event stream is the audit trail; no second store. Operation audit through `IOperationAuditStore` is awaited if the NIS2 audit sub-decision of SPEC-002 (#2413) lands first |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|---|---|---|
| `Encina.Marten`, `Encina.DomainModeling` | Exist | Project references added in PR 1 |
| `Encina.Marten.GDPR` and a persistent key store (#1191) | Exists / open | Only if Dilemma 6 resolves to B |
| ADR-050 coordination for event-sourced modules (Outbox in the Marten session, #2383) | Open | Delivery of the events as notifications; this plan does not depend on it |
| Plans #823 to #826 | In parallel | Reference the shared pieces above |
| #827 NIS2 reporting and read models (open sibling in the same package) | Open | Shares the range, the references and `AddNIS2EventSourcing`; outside this family, so it needs its own allocation in 9800-9899 or a later range |
| #804 DORA incident infrastructure | Open | Will reuse the aggregate pattern; nothing here depends on it |

---

## Next Steps

1. Maintainer answers Dilemmas 4, 6 and 9 when work on #822 starts (each is marked "recommended, pending the maintainer").
2. Brief PR 1 (range, references, domain) first; PR 2 and 3 follow in order.
3. Run `check-plan.ps1` without `-Draft` after the adversarial review sets the `Verified` lines.
