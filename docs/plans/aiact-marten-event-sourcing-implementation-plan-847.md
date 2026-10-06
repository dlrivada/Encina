# Implementation Plan: `Encina.Compliance.AIAct` — Marten Event Sourcing (2 Aggregates)

> **Issue**: [#847](https://github.com/dlrivada/Encina/issues/847) (child of [#415](https://github.com/dlrivada/Encina/issues/415), milestone v0.16.0 — AI Act)
> **Type**: Feature (architectural migration)
> **Complexity**: High (9 phases, Marten event sourcing, 2 aggregates, 2 inline projections, public API replacement, ~50 files)
> **Estimated Scope**: ~1,900-2,400 lines of production code + ~1,800-2,300 lines of tests
> **Supersedes**: `docs/plans/aiact-marten-es-migration-plan.md` (the issue's original plan, written before the 2026-10 rules; this plan replaces it and the old file is deleted in Phase 9)
> **Follow-ups that depend on this plan**: [#842](https://github.com/dlrivada/Encina/issues/842) (Art. 12 record-keeping, plan `docs/plans/aiact-record-keeping-implementation-plan-842.md`, PR #1962), [#839](https://github.com/dlrivada/Encina/issues/839) (Art. 14 workflow)

---

## Summary

Move the two stateful parts of `Encina.Compliance.AIAct` — the AI system registry and the human oversight decision records — from process memory to Marten event sourcing on PostgreSQL, following [ADR-019](../architecture/adr/019-compliance-event-sourcing-marten.md) and [ADR-027](../architecture/adr/027-marten-as-the-event-sourcing-provider.md), as the 9 other stateful compliance modules already do (#777-#784 and the later CrossBorderTransfer migration). Two event-sourced aggregates are introduced: `AISystemAggregate` (registration, reclassification and decommissioning of an AI system; AI Act Art. 6(3), Art. 49/71 registration, Art. 51 for GPAI) and `HumanOversightAggregate` (one Art. 14 oversight session: request, decision, escalation, override). Each has an inline projection to a read model and a service with the CQRS split of the other modules (`IAggregateRepository<T>` for writes, `IReadModelRepository<T>` for reads).

### What is in the code today (verified on `5b485b12`)

Nothing of #847 is implemented. The issue is open, `status:approved`, `p0-mandatory`.

| Item | Location | State |
|---|---|---|
| Package references | `src/Encina.Compliance.AIAct/Encina.Compliance.AIAct.csproj:12` | Only `Encina`; no `Encina.Marten`, `Encina.DomainModeling`, `Encina.Caching`, `Encina.Tenancy` (Consent has all four: `src/Encina.Compliance.Consent/Encina.Compliance.Consent.csproj:12-16`) |
| AI system registry | `InMemoryAISystemRegistry.cs:33` | `ConcurrentDictionary<string, AISystemRegistration>`; lost on restart; no tenant, module, actor or history |
| Registry port | `Abstractions/IAISystemRegistry.cs:134` | `bool IsRegistered(string systemId)` is **synchronous**; a persistent implementation would need a blocking database call, which `AGENTS.md` §3 forbids |
| Human decisions | `DefaultHumanOversightEnforcer.cs:31` | `ConcurrentDictionary<Guid, HumanDecisionRecord>`; lost on restart |
| Approval check | `DefaultHumanOversightEnforcer.cs:64` | `HasHumanApprovalAsync` returns `true` when **any** decision exists, including a rejection (`Decision` is a free string, `Model/HumanDecisionRecord.cs:50`). The new service replaces it with a typed outcome (Phase 3) |
| Reclassification notification | `InMemoryAISystemRegistry.cs:100-110`, `Notifications/AISystemReclassifiedNotification.cs:26` | Published by the in-memory registry; becomes the `AISystemReclassified` domain event |
| Registry consumers | `DefaultAIActClassifier.cs:44,57,69`, `DefaultAIActDocumentation.cs:50,68`, `DefaultAIActComplianceValidator.cs:74` (sync `IsRegistered`), `AIActAutoRegistrationHostedService.cs:68` (sync `IsRegistered`), `Health/AIActHealthCheck.cs:108,143` (loads every system to count them) | All rewired in Phase 4-5 |
| Pipeline | `AIActCompliancePipelineBehavior.cs:145` calls `IAIActComplianceValidator`; human oversight is only logged (`:265-268`), never requested or enforced | Unchanged contract; it gets persistence through the validator |
| Stale documentation | `Abstractions/IHumanOversightEnforcer.cs:29`, `Model/HumanDecisionRecord.cs:17` ("13 database providers") | Removed with the types they document |
| Structured logging | `Diagnostics/AIActLogMessages.cs:25-157`, EventIds 9500-9512 (`LoggerMessage.Define`) | Range `ComplianceAIAct = (9500, 9599)` at `src/Encina/Diagnostics/EventIdRanges.cs:375`; next free id is **9513** |
| Tests | `tests/Encina.UnitTests/Compliance/AIAct/` (19 files), guard/contract/property one file each, load and benchmark `AIAct.md` justifications, **no integration tests** (#1204 tracks that gap) | `InMemoryAISystemRegistryTests.cs` and `DefaultHumanOversightEnforcerTests.cs` are replaced |
| Coverage manifest | `.github/coverage-manifest/Encina.Compliance.AIAct.json` | Targets unit 70, guard 20, contract 15, property 15; **no integration target** |

No code outside the package uses `IAISystemRegistry`, `IHumanOversightEnforcer`, `HumanDecisionRecord`, `ReclassificationRecord` or `AISystemReclassifiedNotification` (searched `src/`, `samples/`, `tests/`), so the public API can be replaced completely (pre-1.0, `AGENTS.md` §1).

### Deviations from the issue text (each is put to the maintainer below or follows a binding rule)

- **`AISystemComplianceEvaluated` is dropped from the registry stream** (Design Choice 4). One event per pipeline check makes the `AISystemAggregate` stream unbounded; the #842 plan records per-use outcomes in its Art. 12 log instead (comment on #847, 2026-10-06).
- **EventIds start at 9513**, not 9540-9570: ADR-021 packs ids sequentially; 9513-9539 would be left unused (same comment).
- **No in-memory default.** The issue keeps the in-memory registry via `TryAdd`; `AGENTS.md` §3 says event-sourced compliance modules have no InMemory stores (Design Choice 5).
- **ADR number.** The issue asks for "ADR-022"; 022 is `022-provider-agnostic-attestation.md`. The next free number is taken from `docs/architecture/adr/index.md` at implementation time (037 or later today; 026, 032, 033 and 035 are reserved by plans).
- **"Test coverage ≥ 85%"** is replaced by the per-flag manifest targets (`AGENTS.md` §9), adding an `integration` target to the AIAct manifest.
- **Pipeline-triggered oversight requests and the Art. 14 gate** belong to #839 (Design Choice 1); this issue delivers the aggregate and the service they call.
- **Dates.** Annex III high-risk obligations apply from 2 Dec 2027 and Annex I from 2 Aug 2028 (Reg. (EU) 2026/1744, SPEC-002 §3.2, issue comment). The feature carries no date logic; XML docs and the feature page state the dates.

**Affected packages**: `Encina.Compliance.AIAct` (modified: new `Aggregates/`, `Events/`, `ReadModels/`, `Services/` folders; in-memory types removed; new references `Encina.Marten`, `Encina.DomainModeling`, `Encina.Caching`, `Encina.Tenancy`). No other package changes.

**Provider category**: Event sourcing (Marten on PostgreSQL), per ADR-019 and ADR-027. The 10-provider database rule does not apply (`AGENTS.md` §5 excludes event sourcing); caching uses the `ICacheProvider` abstraction, so all 8 cache providers work unchanged.

---

## Design Choices

<details>
<summary><strong>1. Scope boundary with #839 (Art. 14 human oversight workflow)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Both aggregates and their services here; the pipeline trigger, review queue, deadlines and Block-mode gate go to #839** | Matches #847's acceptance criteria (two aggregates); #839 builds on a persisted, typed oversight session instead of its stale 13-provider `IHumanDecisionStore`; each PR carries one decision | #839's body must be rewritten; until #839 lands, oversight sessions are created only through the service API, not by the pipeline |
| **B — Only `AISystemAggregate` here; all of human oversight moves to #839** | Smallest #847; Art. 14 designed in one place | Leaves `DefaultHumanOversightEnforcer.cs:31` volatile; contradicts the issue's title and acceptance criteria; #839 would also have to add the Marten wiring |
| **C — Everything here, including the pipeline opening a session for every request that requires oversight and blocking until an approval exists** | Art. 14 enforced end to end in one delivery | Doubles the scope; a gate needs a review queue, deadlines and an approval correlation contract (all #839 topics); sessions opened by the pipeline without a queue would pile up unresolved |

### Chosen Option: **A — Aggregates and services here, workflow and gate in #839** (recommended, pending the maintainer)

### Rationale

We recommend A. The persistence model of an oversight session (who asked, who decided, with which rationale, escalations and overrides) is exactly what #847 promises, and it is the record Art. 14 and a conformity assessment (Art. 43) need. When to open a session from the pipeline and whether to block until it is approved are workflow questions that need #839's queue and deadlines; doing them here (C) would design #839 inside #847. B leaves the volatile store in place for another release.

</details>

<details>
<summary><strong>2. Public API — replace the registry and enforcer ports, or keep them</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Replace `IAISystemRegistry` and `IHumanOversightEnforcer` with `IAISystemService` and `IHumanOversightService`; the classifier, validator, documentation, auto-registration and health check consume the services** | One API; async everywhere (drops the sync `IsRegistered`); actor, reason, tenant and decommissioning become part of the contract; typed decision outcome fixes the `HasHumanApprovalAsync` defect | Breaking change for any user of the old ports (none in the repository; pre-1.0) |
| **B — Keep both ports, implement them on Marten (`AISystemRegistryMarten`), and add the services alongside** | Existing call sites keep compiling | Two APIs for the same state; the sync `IsRegistered` still needs a blocking call or a cache; no actor or tenant in `RegisterSystemAsync`; violates "no compatibility layers" (`AGENTS.md` §3) |
| **C — Keep the ports and only swap the implementations, no new interfaces** | Smallest diff | Cannot express decommissioning, actor, rationale enum or tenant; keeps the defects listed in the Summary |

### Chosen Option: **A — Replace the ports with services** (recommended, pending the maintainer)

### Rationale

We recommend A. Pre-1.0 the best design wins and compatibility layers are forbidden. The old ports cannot carry what the regulation asks for (who reclassified, why, when it was decommissioned) and one of their members is synchronous. The stateless part of `IHumanOversightEnforcer` (reading `[RequireHumanOversight]`, `DefaultHumanOversightEnforcer.cs:42-49`) moves into the validator, which already reads the other attributes (`DefaultAIActComplianceValidator.cs:99`).

</details>

<details>
<summary><strong>3. Stream identity of an AI system (string <code>SystemId</code> vs Marten <code>Guid</code> streams)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Deterministic stream id: UUIDv5 of `(tenantId ?? "", systemId)` in an AIAct namespace; `SystemId` kept as a field** | Lookups by `SystemId` need no query (compute the id, `LoadAsync`/`GetByIdAsync`); duplicate registration across instances is detected by Marten (`MartenErrorCodes.StreamAlreadyExists`, `src/Encina.Marten/MartenAggregateRepository.cs:312-320`) with no lock; tenant-scoped by construction | A new helper (`AIActStreamIds`); a system re-registered after decommissioning needs an explicit `Reactivate` command instead of a new stream |
| **B — Random `Guid` per registration, lookup by `SystemId` through `IReadModelRepository.QueryAsync`** | Same as most modules (`DefaultApprovedTransferService.cs:507`) | Check-then-create race: two instances auto-registering at startup both see "not registered" and create two streams; every pipeline check runs a query; needs a unique index or a distributed lock |
| **C — String stream identity (`AggregateBase<string>`, Marten `StreamIdentity.AsString`)** | Stream key is the `SystemId` itself | `StreamIdentity` is store-wide: switching it breaks every other module's `Guid` streams; `IAggregateRepository<T>.LoadAsync` takes a `Guid` (`src/Encina.Marten/IAggregateRepository.cs:19-20`) |

### Chosen Option: **A — Deterministic UUIDv5 stream id** (recommended, pending the maintainer)

### Rationale

We recommend A. Attributes and callers identify systems by a string (`HighRiskAIAttribute.SystemId`, `AIActAutoRegistrationHostedService.cs:66`), Marten streams are `Guid`s, and the auto-registration hosted service runs on every instance at startup. A deterministic id turns the natural key into the stream key, makes registration idempotent through Marten's own collision detection, and lets the hot path (one lookup per decorated request) read by id. The #842 plan already uses deterministic UUIDs for its log streams, so the helper is shared. Oversight sessions keep random ids (they have no natural key). The decision is recorded in an ADR because it is the first natural-key stream in the compliance modules.

</details>

<details>
<summary><strong>4. Per-request compliance evaluations (<code>AISystemComplianceEvaluated</code>, overlap with #842)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Drop the event; per-request outcomes go to the #842 record-keeping log** | Registry stream stays small (a handful of events per system lifetime); no write on the hot path of every request; Art. 12 logging designed once, with retention and export, in #842 | Until #842 lands, evaluations are only in logs and metrics (as today) |
| **B — Keep the event in `AISystemAggregate` (issue text)** | Evaluation history next to the classification | One append per request makes the stream unbounded, slows every load and rebuild, and needs snapshots; puts a database write and an optimistic-concurrency conflict point on every decorated request; duplicates #842 |
| **C — Keep only counters on the read model (`ComplianceEvaluationCount`, `LastEvaluatedAtUtc`) without an event** | Cheap dashboard figure | A read model updated outside the event stream breaks projection rebuilds; still a write per request |

### Chosen Option: **A — Drop the event; #842 owns per-use records** (recommended, pending the maintainer)

### Rationale

We recommend A, as the #842 plan does (its Design Choice 3, comment on #847 of 2026-10-06). The registry records decisions about a system; Art. 12 records each use of it. Mixing both in one stream would make the registry the bottleneck of every AI request and would need snapshotting for no regulatory gain.

</details>

<details>
<summary><strong>5. Registration model and the in-memory default</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — No in-memory store; `AddEncinaAIAct()` registers the services and calls `AddAggregateRepository<T>()` / `AddProjection<,>()` itself; the application supplies the Marten store through `AddEncinaMarten(...)`** | Follows `AGENTS.md` §3 (no InMemory for event-sourced compliance) and the registration-completeness rule (one call registers everything it resolves, proven by a `ValidateOnBuild` test); impossible to forget the second call | Differs from the 9 other modules, which use a separate `Add{Module}Aggregates()` (`src/Encina.Compliance.Consent/ConsentMartenExtensions.cs:50`); AIAct now requires PostgreSQL |
| **B — No in-memory store; separate `AddAIActAggregates()` (issue text, Consent pattern)** | Consistent with the other modules | Two calls; forgetting the second leaves `DefaultAISystemService` with an unresolvable `IAggregateRepository<>` (the case the registration-completeness rule exists for) |
| **C — Keep `InMemoryAISystemRegistry` as the `TryAdd` default, replaced by `AddAIActAggregates()` (old plan)** | Runs without a database | Contradicts `AGENTS.md` §3; a volatile registry can be mistaken for compliance evidence in production; two code paths to test |

### Chosen Option: **A — One registration call, no in-memory store** (recommended, pending the maintainer)

### Rationale

We recommend A. `AGENTS.md` rules out C. Between A and B, the registration-completeness rule decides: an `AddEncina*` method that adds a service must register what that service resolves. The Marten store itself (connection string, schema) stays the application's choice. Whether the 9 existing modules should converge on A is a separate consistency question, not changed here. Unit tests mock `IAggregateRepository<T>` and `IReadModelRepository<T>` (NSubstitute); integration tests run on the shared `MartenCollection` fixture.

</details>

<details>
<summary><strong>6. Caching of registry reads on the compliance decision path</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — No cache; every lookup reads the read model by id** | Always consistent: a reclassification to a prohibited practice takes effect on the next request | One PostgreSQL round trip per decorated request |
| **B — `ICacheProvider`, 5-minute TTL, fire-and-forget invalidation (issue text, Consent pattern `DefaultConsentService.cs:467,632`)** | Lowest load | A system reclassified as prohibited (Art. 5) can still pass for up to 5 minutes, or longer if the unawaited removal fails; violates the spirit of fail-closed gates |
| **C — `ICacheProvider` with tenant-segmented keys, a short configurable TTL (default 60 s, `AIActOptions.RegistryCacheDuration`) and awaited invalidation on every mutation; a failed invalidation fails the mutation** | Removes most round trips; staleness bounded and explicit; mutations never report success with a stale cache | Slightly more code than B; L1-only caches on other instances still keep entries up to the TTL |

### Chosen Option: **C — Short TTL, tenant-segmented keys, awaited invalidation** (recommended, pending the maintainer)

### Rationale

We recommend C. The registry is read on every decorated request, so caching pays, but the read decides whether a request is blocked as a prohibited use. Awaiting the invalidation and bounding the TTL keeps that decision honest; tenant-segmented keys avoid the cross-tenant cache leaks fixed in Consent (#1315) and still open in LawfulBasis (#1949). Oversight reads are rare and not cached. Setting the TTL to zero gives A.

</details>

<details>
<summary><strong>7. Tenant and module scope in this issue (overlap with #845 and #846)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Stamp `TenantId`/`ModuleId` on every event, aggregate and read model; scope reads, writes and cache keys by the ambient tenant and fail closed when a multi-tenant application has no tenant (Consent pattern, `DefaultConsentService.cs:107-128`); per-tenant options stay in #845, module-scoped enforcement in #846** | Data is never stored without its tenant (fixing it later means an event upcaster); no cross-tenant leaks from day one; #845/#846 become pure behaviour changes | #845 and #846 need their bodies updated (the in-memory tenant-aware registry they describe disappears) |
| **B — Stamp the fields only; all scoping in #845/#846** | Smaller #847 | Ships a multi-tenant store that can read another tenant's systems (the #1315/#1949 bug class); fail-closed rule broken meanwhile |
| **C — Implement #845 and #846 completely here (per-tenant options, `ConfigureModule`)** | One delivery | Adds two features with their own design questions to an already large PR |

### Chosen Option: **A — Stamp and scope here, per-tenant behaviour in #845/#846** (recommended, pending the maintainer)

### Rationale

We recommend A. Storing tenant and module from the first event costs little and avoids migrating streams later; scoping reads is the security half of multi-tenancy and the compliance gates must fail closed (`AGENTS.md` §3, SPEC-002 DEC-006). Per-tenant enforcement modes and module-scoped registries change behaviour and deserve their own review.

</details>

<details>
<summary><strong>8. Granularity and decision model of <code>HumanOversightAggregate</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — One stream per oversight session (system + correlation id): requested → decided → escalated/overridden; typed `HumanOversightDecision` enum (`Approved`, `Rejected`) and `HumanOversightStatus`** | Matches Art. 14's process (request, review, decision, override) and the BreachNotification one-incident-one-stream pattern; typed outcome makes "has approval" unambiguous | Many small streams; "all sessions of system X" is a read-model query |
| **B — One stream per AI system collecting every decision** | Natural "history of system X" | Unbounded stream; concurrency conflicts between unrelated reviews of the same system |
| **C — One stream per decision, free-text `Decision` (today's `HumanDecisionRecord`)** | Simplest | Escalations and overrides cannot reference the decision they change; free text cannot be evaluated by a gate (#839) |

### Chosen Option: **A — One stream per session, typed decision** (recommended, pending the maintainer)

### Rationale

We recommend A. The session is the unit an auditor follows and the unit #839's gate will check. A typed outcome replaces the string `Decision` (`Model/HumanDecisionRecord.cs:50`) that made `HasHumanApprovalAsync` treat a rejection as an approval.

</details>

---

## Implementation Phases

### Phase 1: Domain Events, Aggregates & Enums

> **Goal**: Define the events and the two aggregates with their state machines and invariants.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `src/Encina.Compliance.AIAct/Encina.Compliance.AIAct.csproj`** (line 12): add `ProjectReference` to `..\Encina.DomainModeling\Encina.DomainModeling.csproj`, `..\Encina.Marten\Encina.Marten.csproj`, `..\Encina.Caching\Encina.Caching.csproj`, `..\Encina.Tenancy\Encina.Tenancy.csproj` (same set as `Encina.Compliance.Consent.csproj:12-16`). Remove the ten `InternalsVisibleTo` entries for ADO/Dapper/EF/MongoDB satellites (lines 21-50): AIAct has no database satellites.
2. **Create `Model/AISystemStatus.cs`** — `public enum AISystemStatus { Active, Decommissioned }`.
3. **Create `Model/HumanOversightStatus.cs`** — `public enum HumanOversightStatus { Requested, Decided, Escalated, Overridden }`.
4. **Create `Model/HumanOversightDecision.cs`** — `public enum HumanOversightDecision { Approved, Rejected }`.
5. **Create `Events/AISystemEvents.cs`** (namespace `Encina.Compliance.AIAct.Events`), sealed records implementing `INotification` (published by `EventPublishingPipelineBehavior`, `src/Encina.Marten/EventPublishingPipelineBehavior.cs`):
   - `AISystemRegistered(Guid StreamId, string SystemId, string Name, AISystemCategory Category, AIRiskLevel RiskLevel, IReadOnlyList<ProhibitedPractice> ProhibitedPractices, string? Provider, string? Version, string? Description, string? DeploymentContext, string RegisteredBy, DateTimeOffset OccurredAtUtc, string? TenantId, string? ModuleId)`
   - `AISystemReclassified(Guid StreamId, string SystemId, AIRiskLevel PreviousRiskLevel, AIRiskLevel NewRiskLevel, AISystemCategory PreviousCategory, AISystemCategory NewCategory, IReadOnlyList<ProhibitedPractice> ProhibitedPractices, string Reason, string ReclassifiedBy, DateTimeOffset OccurredAtUtc)` (Art. 6(3))
   - `AISystemDecommissioned(Guid StreamId, string SystemId, string Reason, string DecommissionedBy, DateTimeOffset OccurredAtUtc)`
   - `AISystemReactivated(Guid StreamId, string SystemId, string Reason, string ReactivatedBy, DateTimeOffset OccurredAtUtc)` (needed by Design Choice 3A)
6. **Create `Events/HumanOversightEvents.cs`** (names from the issue):
   - `HumanOversightRequested(Guid SessionId, string SystemId, string RequestTypeName, string? CorrelationId, string Reason, string RequestedBy, DateTimeOffset OccurredAtUtc, string? TenantId, string? ModuleId)`
   - `HumanDecisionRecorded(Guid SessionId, string ReviewerId, HumanOversightDecision Decision, string Rationale, DateTimeOffset OccurredAtUtc)`
   - `HumanOversightEscalated(Guid SessionId, string EscalatedBy, string EscalatedTo, string Reason, DateTimeOffset OccurredAtUtc)`
   - `HumanDecisionOverridden(Guid SessionId, string OverriddenBy, HumanOversightDecision PreviousDecision, HumanOversightDecision NewDecision, string Reason, DateTimeOffset OccurredAtUtc)`
7. **Create `Aggregates/AISystemAggregate.cs`** — `public sealed class AISystemAggregate : AggregateBase`
   - Properties (private set): `SystemId`, `Name`, `Category`, `RiskLevel`, `ProhibitedPractices`, `Provider`, `Version`, `Description`, `DeploymentContext`, `Status`, `RegisteredBy`, `RegisteredAtUtc`, `LastReclassifiedAtUtc`, `DecommissionedAtUtc`, `TenantId`, `ModuleId`.
   - `public static AISystemAggregate Register(Guid streamId, string systemId, string name, AISystemCategory category, AIRiskLevel riskLevel, IReadOnlyList<ProhibitedPractice> prohibitedPractices, string registeredBy, DateTimeOffset occurredAtUtc, string? provider = null, string? version = null, string? description = null, string? deploymentContext = null, string? tenantId = null, string? moduleId = null)`
   - `public void Reclassify(AIRiskLevel newRiskLevel, AISystemCategory newCategory, IReadOnlyList<ProhibitedPractice> prohibitedPractices, string reason, string reclassifiedBy, DateTimeOffset occurredAtUtc)` — throws `InvalidOperationException` when `Decommissioned` or when nothing changes.
   - `public void Decommission(string reason, string decommissionedBy, DateTimeOffset occurredAtUtc)` — throws when already decommissioned.
   - `public void Reactivate(string reason, string reactivatedBy, DateTimeOffset occurredAtUtc)` — throws unless decommissioned.
   - `protected override void Apply(object domainEvent)` — `switch` over the four events (`// crap-exempt: single-question switch` allowed).
8. **Create `Aggregates/HumanOversightAggregate.cs`** — `public sealed class HumanOversightAggregate : AggregateBase`
   - Properties: `SystemId`, `RequestTypeName`, `CorrelationId`, `Reason`, `Status`, `Decision`, `Rationale`, `ReviewerId`, `RequestedBy`, `RequestedAtUtc`, `DecidedAtUtc`, `EscalatedTo`, `TenantId`, `ModuleId`.
   - `public static HumanOversightAggregate Request(Guid sessionId, string systemId, string requestTypeName, string? correlationId, string reason, string requestedBy, DateTimeOffset occurredAtUtc, string? tenantId = null, string? moduleId = null)`
   - `public void RecordDecision(string reviewerId, HumanOversightDecision decision, string rationale, DateTimeOffset occurredAtUtc)` — allowed from `Requested` or `Escalated`; rationale required (Art. 14(4)).
   - `public void Escalate(string escalatedBy, string escalatedTo, string reason, DateTimeOffset occurredAtUtc)` — allowed from `Requested` or `Decided`.
   - `public void Override(string overriddenBy, HumanOversightDecision newDecision, string reason, DateTimeOffset occurredAtUtc)` — allowed from `Decided` or `Overridden`, and only when `newDecision` differs.
   - `public bool IsApproved => Decision == HumanOversightDecision.Approved && Status is HumanOversightStatus.Decided or HumanOversightStatus.Overridden`.
9. **Create `AIActStreamIds.cs`** — `internal static class AIActStreamIds` with `public static Guid ForSystem(string? tenantId, string systemId)` (RFC 9562 UUIDv5, SHA-1 over a fixed AIAct namespace `Guid` plus `tenantId ?? string.Empty` + `'\u001F'` + `systemId`). Only with Design Choice 3A.
10. **Delete** `Model/ReclassificationRecord.cs` (replaced by `AISystemReclassified`) and `Notifications/AISystemReclassifiedNotification.cs` (replaced by the event, which is an `INotification`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #847 (Encina.Compliance.AIAct to Marten event sourcing).

CONTEXT:
- Encina is .NET 10 / C# 14, pre-1.0: no backward compatibility, no [Obsolete], no compatibility layers.
- AIAct today keeps its registry in InMemoryAISystemRegistry.cs:33 and human decisions in
  DefaultHumanOversightEnforcer.cs:31 (ConcurrentDictionary). This phase adds the event-sourced domain.
- Compliance modules use Marten event sourcing (ADR-019, ADR-027). Consent is the reference module.
- Stream identity of an AI system is a deterministic UUIDv5 of (tenantId, systemId) (Design Choice 3).

TASK:
1. Add project references Encina.DomainModeling, Encina.Marten, Encina.Caching, Encina.Tenancy to
   src/Encina.Compliance.AIAct/Encina.Compliance.AIAct.csproj; remove the InternalsVisibleTo entries for the
   ADO/Dapper/EF/MongoDB satellites.
2. Create Model/AISystemStatus.cs, Model/HumanOversightStatus.cs, Model/HumanOversightDecision.cs.
3. Create Events/AISystemEvents.cs (AISystemRegistered, AISystemReclassified, AISystemDecommissioned,
   AISystemReactivated) and Events/HumanOversightEvents.cs (HumanOversightRequested, HumanDecisionRecorded,
   HumanOversightEscalated, HumanDecisionOverridden) with the fields listed in the plan.
4. Create Aggregates/AISystemAggregate.cs and Aggregates/HumanOversightAggregate.cs with the factory methods,
   commands and invariants listed in the plan.
5. Create internal static AIActStreamIds.ForSystem(tenantId, systemId) (UUIDv5, RFC 9562).
6. Delete Model/ReclassificationRecord.cs and Notifications/AISystemReclassifiedNotification.cs.

KEY RULES:
- Events are sealed records implementing INotification; UTC timestamps end in AtUtc; TenantId/ModuleId on the
  first event of each stream.
- Aggregates extend AggregateBase (Encina.DomainModeling), raise events with RaiseEvent(), rebuild in Apply().
- Invalid transitions throw InvalidOperationException (the service maps it to an EncinaError); guards use
  ArgumentException.ThrowIfNullOrWhiteSpace / ArgumentNullException.ThrowIfNull.
- No DateTime.UtcNow: timestamps are parameters supplied by the service from TimeProvider.
- Keep every changed method at CRAP <= 10; the Apply switch may carry "// crap-exempt: single-question switch — <reason>".
- XML docs on every public type cite the AI Act article (Art. 6(3), Art. 14, Art. 49/71, Art. 51) and state that
  Annex III obligations apply from 2 Dec 2027 and Annex I from 2 Aug 2028 (Reg. (EU) 2026/1744).
- Add every public symbol to PublicAPI.Unshipped.txt; remove the lines of deleted types.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Aggregates/ConsentAggregate.cs
- src/Encina.Compliance.Consent/Events/ConsentEvents.cs
- src/Encina.Compliance.BreachNotification/Aggregates/ (one-incident-one-stream lifecycle)
- src/Encina.DomainModeling/AggregateBase.cs
- src/Encina.Compliance.AIAct/Model/AISystemRegistration.cs
```

</details>

---

### Phase 2: Read Models & Inline Projections

> **Goal**: The query side: one read model and one inline projection per aggregate.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `ReadModels/AISystemReadModel.cs`** — `public sealed class AISystemReadModel : IReadModel`
   - `Guid Id` (the stream id), `string SystemId`, `string Name`, `AISystemCategory Category`, `AIRiskLevel RiskLevel`, `IReadOnlyList<ProhibitedPractice> ProhibitedPractices`, `bool IsProhibited` (computed: any prohibited practice), `AISystemStatus Status`, `string? Provider`, `string? Version`, `string? Description`, `string? DeploymentContext`, `string RegisteredBy`, `DateTimeOffset RegisteredAtUtc`, `DateTimeOffset? LastReclassifiedAtUtc`, `int ReclassificationCount`, `DateTimeOffset? DecommissionedAtUtc`, `string? TenantId`, `string? ModuleId`, `DateTimeOffset LastModifiedAtUtc`, `int Version`.
   - `public AISystemRegistration ToRegistration()` so the classifier and documentation keep using `AISystemRegistration` (`Model/AISystemRegistration.cs:18`).
2. **Create `ReadModels/AISystemProjection.cs`** — `public sealed class AISystemProjection : IProjection<AISystemReadModel>, IProjectionCreator<AISystemRegistered, AISystemReadModel>, IProjectionHandler<AISystemReclassified, AISystemReadModel>, IProjectionHandler<AISystemDecommissioned, AISystemReadModel>, IProjectionHandler<AISystemReactivated, AISystemReadModel>`; `ProjectionName => "AISystemProjection"`.
3. **Create `ReadModels/HumanOversightReadModel.cs`** — `Guid Id` (session id), `SystemId`, `RequestTypeName`, `CorrelationId`, `Reason`, `HumanOversightStatus Status`, `HumanOversightDecision? Decision`, `string? Rationale`, `string? ReviewerId`, `string? EscalatedTo`, `string RequestedBy`, `DateTimeOffset RequestedAtUtc`, `DateTimeOffset? DecidedAtUtc`, `DateTimeOffset? EscalatedAtUtc`, `DateTimeOffset? OverriddenAtUtc`, `bool IsApproved`, `TenantId`, `ModuleId`, `LastModifiedAtUtc`, `Version`.
4. **Create `ReadModels/HumanOversightProjection.cs`** — creator for `HumanOversightRequested`, handlers for the other three events; `ProjectionName => "HumanOversightProjection"`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #847 (Encina.Compliance.AIAct to Marten event sourcing).

CONTEXT:
- Phase 1 added AISystemAggregate, HumanOversightAggregate and their events under
  src/Encina.Compliance.AIAct/Aggregates and Events.
- Read models are the CQRS query side, kept up to date by inline Marten projections in the same transaction as
  the event append.

TASK:
Create ReadModels/AISystemReadModel.cs, ReadModels/AISystemProjection.cs, ReadModels/HumanOversightReadModel.cs
and ReadModels/HumanOversightProjection.cs with the members listed in the plan, plus
AISystemReadModel.ToRegistration().

KEY RULES:
- Read models implement IReadModel (Encina.Marten.Projections) with public setters; projections implement
  IProjection<T>, IProjectionCreator<TFirstEvent, T> and IProjectionHandler<TEvent, T>.
- Projections take no IServiceProvider and no constructor dependencies (#949); they are pure functions of
  the event and ProjectionContext.
- Every handler increments Version and sets LastModifiedAtUtc from the event's OccurredAtUtc (never the clock).
- Applying the same event twice gives the same read model (idempotent).
- XML docs and PublicAPI.Unshipped.txt entries for every public member.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ReadModels/ConsentReadModel.cs
- src/Encina.Compliance.Consent/ReadModels/ConsentProjection.cs
- src/Encina.Marten/Projections/IProjection.cs
- src/Encina.Compliance.AIAct/Model/AISystemRegistration.cs
```

</details>

---

### Phase 3: Service Abstractions & Default Services

> **Goal**: Replace `IAISystemRegistry` and `IHumanOversightEnforcer` with two services that own the write and read paths.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `Abstractions/IAISystemService.cs`** — `public interface IAISystemService`, every method `ValueTask<Either<EncinaError, T>>` with a trailing `CancellationToken cancellationToken = default`:
   - `RegisterSystemAsync(AISystemRegistrationRequest request, string registeredBy, CancellationToken)` → `Guid` (stream id); an existing stream returns `AIActErrors.SystemAlreadyRegistered`.
   - `ReclassifySystemAsync(string systemId, AIRiskLevel newRiskLevel, AISystemCategory newCategory, IReadOnlyList<ProhibitedPractice> prohibitedPractices, string reason, string reclassifiedBy, CancellationToken)` → `Unit`
   - `DecommissionSystemAsync(string systemId, string reason, string decommissionedBy, CancellationToken)` → `Unit`
   - `ReactivateSystemAsync(string systemId, string reason, string reactivatedBy, CancellationToken)` → `Unit`
   - `GetSystemAsync(string systemId, CancellationToken)` → `AISystemReadModel` (`AIActErrors.SystemNotRegistered` when absent)
   - `FindActiveSystemAsync(string systemId, CancellationToken)` → `Option<AISystemReadModel>` (hot path of the validator: `None` for absent or decommissioned)
   - `GetSystemsByRiskLevelAsync(AIRiskLevel level, CancellationToken)` → `IReadOnlyList<AISystemReadModel>`
   - `GetAllSystemsAsync(CancellationToken)` → `IReadOnlyList<AISystemReadModel>`
   - `CountSystemsAsync(CancellationToken)` → `long` (health check)
   - `GetSystemHistoryAsync(string systemId, CancellationToken)` → `IReadOnlyList<object>` (raw events, Art. 6(3) evidence; reads through Marten's event store)
2. **Create `Model/AISystemRegistrationRequest.cs`** — sealed record with the registration fields of `AISystemRegistration` minus `RegisteredAtUtc` (set by the service) plus `string? ModuleId`.
3. **Create `Abstractions/IHumanOversightService.cs`**:
   - `RequestOversightAsync(string systemId, string requestTypeName, string? correlationId, string reason, string requestedBy, CancellationToken)` → `Guid` (session id)
   - `RecordDecisionAsync(Guid sessionId, string reviewerId, HumanOversightDecision decision, string rationale, CancellationToken)` → `Unit`
   - `EscalateAsync(Guid sessionId, string escalatedBy, string escalatedTo, string reason, CancellationToken)` → `Unit`
   - `OverrideAsync(Guid sessionId, string overriddenBy, HumanOversightDecision newDecision, string reason, CancellationToken)` → `Unit`
   - `GetSessionAsync(Guid sessionId, CancellationToken)` → `HumanOversightReadModel`
   - `GetSessionsBySystemAsync(string systemId, CancellationToken)` → `IReadOnlyList<HumanOversightReadModel>`
   - `HasApprovalAsync(Guid sessionId, CancellationToken)` → `bool` (true only for `IsApproved`)
   - `RequiresHumanReviewAsync<TRequest>(TRequest request, CancellationToken)` is **not** carried over; the attribute check moves to the validator (Phase 4).
4. **Create `Services/DefaultAISystemService.cs`** — `internal sealed class DefaultAISystemService : IAISystemService`
   - Constructor: `IAggregateRepository<AISystemAggregate> repository`, `IReadModelRepository<AISystemReadModel> readModelRepository`, `IDocumentSession session` (history only), `ICacheProvider cache`, `TimeProvider timeProvider`, `IRequestContextAccessor requestContextAccessor`, `IOptions<AIActOptions> options`, `ILogger<DefaultAISystemService> logger`, `ITenantProvider? tenantProvider = null`.
   - Writes: resolve tenant (Phase 6) → `AIActStreamIds.ForSystem(tenant, systemId)` → `CreateAsync`/`LoadAsync`+command+`SaveAsync` → awaited cache invalidation (Design Choice 6C) → metric + log. `MartenErrorCodes.StreamAlreadyExists` maps to `SystemAlreadyRegistered`; `InvalidOperationException` maps to `AIActErrors.InvalidStateTransition`; any other exception to `AIActErrors.ServiceError(operation, ex)` logged through `ex.ForLogging()`.
   - Reads: cache key `aiact:tenant:{segment}:system:{streamId}`; fallback `GetByIdAsync(streamId)`; lists via `QueryAsync` filtered by `TenantId`.
5. **Create `Services/DefaultHumanOversightService.cs`** — same shape with `IAggregateRepository<HumanOversightAggregate>` and `IReadModelRepository<HumanOversightReadModel>`; `RequestOversightAsync` rejects an unknown or decommissioned system (`FindActiveSystemAsync` through `IAISystemService`); session ids from `Guid.CreateVersion7(timeProvider.GetUtcNow())`; no cache.
6. **Modify `AIActErrors.cs`** (class at line 22): add `SystemAlreadyRegisteredCode = "aiact.system_already_registered"`, `SystemDecommissionedCode = "aiact.system_decommissioned"`, `OversightSessionNotFoundCode = "aiact.oversight_session_not_found"`, `InvalidStateTransitionCode = "aiact.invalid_state_transition"`, `TenantContextRequiredCode = "aiact.tenant_context_required"`, `ServiceErrorCode = "aiact.service_error"` with factory methods; the existing `SystemNotRegisteredCode` (line 44) gets a factory if missing. Messages never contain an exception message.
7. **Delete** `Abstractions/IAISystemRegistry.cs`, `Abstractions/IHumanOversightEnforcer.cs`, `InMemoryAISystemRegistry.cs`, `DefaultHumanOversightEnforcer.cs`, `Model/HumanDecisionRecord.cs` (replaced by `HumanOversightReadModel`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #847 (Encina.Compliance.AIAct to Marten event sourcing).

CONTEXT:
- Phases 1-2 added the aggregates, events, read models and projections.
- IAISystemRegistry (with a synchronous IsRegistered) and IHumanOversightEnforcer are replaced by
  IAISystemService and IHumanOversightService (Design Choice 2). No code outside the AIAct package uses them.
- Writes go through IAggregateRepository<T> (Encina.Marten); reads through IReadModelRepository<T>.

TASK:
1. Create Abstractions/IAISystemService.cs, Abstractions/IHumanOversightService.cs and
   Model/AISystemRegistrationRequest.cs with the members listed in the plan.
2. Create Services/DefaultAISystemService.cs and Services/DefaultHumanOversightService.cs (internal sealed).
3. Add the new error codes and factories to AIActErrors.cs.
4. Delete IAISystemRegistry.cs, IHumanOversightEnforcer.cs, InMemoryAISystemRegistry.cs,
   DefaultHumanOversightEnforcer.cs and Model/HumanDecisionRecord.cs, and their PublicAPI lines.

KEY RULES:
- Every method returns ValueTask<Either<EncinaError, T>>; exceptions never escape except OperationCanceledException.
- AI system stream ids come from AIActStreamIds.ForSystem(tenantId, systemId); StreamAlreadyExists means
  "already registered" (idempotent for the auto-registration hosted service).
- Cache: tenant-segmented keys, TTL from AIActOptions.RegistryCacheDuration, invalidation awaited; a failed
  invalidation returns Left. Oversight reads are not cached.
- EncinaError.Message never reaches logs or tags; log the code or exceptions through ex.ForLogging().
- Actor ids (registeredBy, reviewerId...) are not logged (they can be personal data); log system id and operation.
- TimeProvider for every timestamp; no DateTime.UtcNow.
- CRAP <= 10 per method: extract tenant resolution, cache access and error mapping into small helpers.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- src/Encina.Compliance.Consent/Abstractions/IConsentService.cs
- src/Encina.Compliance.CrossBorderTransfer/Services/DefaultApprovedTransferService.cs
- src/Encina.Marten/IAggregateRepository.cs
- src/Encina.Marten/MartenAggregateRepository.cs (StreamAlreadyExists at lines 312-320)
- src/Encina.Compliance.AIAct/AIActErrors.cs
```

</details>

---

### Phase 4: Compliance Engine Rewiring

> **Goal**: Make the classifier, validator, documentation generator and auto-registration use the services.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `DefaultAIActClassifier.cs`** (constructor at line 29, lookups at 44, 57, 69): depend on `IAISystemService`; use `FindActiveSystemAsync(...)` and `ToRegistration()`; register as **scoped** (it now depends on a scoped service).
2. **Modify `DefaultAIActDocumentation.cs`** (constructor at line 35, lookups at 50, 68): same change; scoped.
3. **Modify `DefaultAIActComplianceValidator.cs`**:
   - Replace `IAISystemRegistry` and `IHumanOversightEnforcer` (lines 37-38) with `IAISystemService`.
   - Line 74: replace the sync `_registry.IsRegistered(...)` with `await FindActiveSystemAsync(...)`; a `Left` (store unreachable) is returned as `Left`, so the pipeline blocks with `aiact.validator_error` (fail closed, `AIActCompliancePipelineBehavior.cs:212-218`).
   - Line 94: replace `RequiresHumanReviewAsync` with a static per-type attribute cache of `[RequireHumanOversight]` (moved from `DefaultHumanOversightEnforcer.cs:32,42-49`).
4. **Modify `AIActAutoRegistrationHostedService.cs`**:
   - Inject `IServiceScopeFactory` instead of the registry (field at line 33); create one scope for the scan.
   - Replace the `IsRegistered` check (line 68 of the file) with `RegisterSystemAsync` and treat `SystemAlreadyRegistered` as "skipped" (no check-then-act race across instances).
   - Actor `"system:aiact-auto-registration"`; tenant: none (global registrations) unless `AIActOptions.AutoRegistrationTenantId` is set.
   - Replace the inline `LogDebug`/`LogInformation` calls with the source-generated messages of Phase 7.
5. **`AIActCompliancePipelineBehavior.cs`**: no contract change; it already depends only on `IAIActComplianceValidator` (line 59). Add the `aiact.tenant_id` activity tag in Phase 7.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #847 (Encina.Compliance.AIAct to Marten event sourcing).

CONTEXT:
- Phase 3 replaced IAISystemRegistry/IHumanOversightEnforcer with IAISystemService/IHumanOversightService.
- The classifier, documentation generator, compliance validator and auto-registration hosted service still use
  the old ports and must move to the services.
- Compliance gates fail closed (AGENTS.md section 3): a store failure blocks the request.

TASK:
1. DefaultAIActClassifier and DefaultAIActDocumentation: depend on IAISystemService, use FindActiveSystemAsync and
   AISystemReadModel.ToRegistration().
2. DefaultAIActComplianceValidator: replace the sync IsRegistered with FindActiveSystemAsync; propagate Left;
   read [RequireHumanOversight] through a static ConcurrentDictionary<Type, bool> cache.
3. AIActAutoRegistrationHostedService: IServiceScopeFactory, RegisterSystemAsync, SystemAlreadyRegistered = skipped.
4. Leave AIActCompliancePipelineBehavior's contract unchanged.

KEY RULES:
- No synchronous database access anywhere; every await uses ConfigureAwait(false) in library code.
- A hosted service never resolves scoped services from the root provider.
- Not registered or decommissioned system keeps today's semantics (MinimalRisk result, request passes);
  an unreachable store returns Left (blocked as aiact.validator_error).
- CRAP <= 10 for every changed method; measure locally with the crap-gate table before reporting.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/DefaultAIActComplianceValidator.cs
- src/Encina.Compliance.AIAct/AIActAutoRegistrationHostedService.cs
- src/Encina.Compliance.AIAct/DefaultAIActClassifier.cs
- src/Encina.Compliance.Consent/ConsentAutoRegistrationHostedService.cs
- src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs
```

</details>

---

### Phase 5: Configuration, DI & Health Check

> **Goal**: One registration call that registers everything it resolves, options for the new behaviour, and a health check that does not load every system.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `AIActOptions.cs`** (properties at lines 40-77): add `TimeSpan RegistryCacheDuration { get; set; } = TimeSpan.FromSeconds(60)` (zero disables caching), `bool? RequireTenantContext { get; set; }` (null: required when an `ITenantProvider` is registered; Consent semantics, `ConsentOptions.cs:148`), `string? AutoRegistrationTenantId { get; set; }`.
2. **Modify `AIActOptionsValidator.cs`**: `RegistryCacheDuration` between zero and 10 minutes.
3. **Modify `ServiceCollectionExtensions.cs`** (`AddEncinaAIAct`, lines 68-101):
   - Remove `TryAddSingleton<IAISystemRegistry, InMemoryAISystemRegistry>()` and `TryAddSingleton<IHumanOversightEnforcer, DefaultHumanOversightEnforcer>()` (lines 83, 85).
   - Add `TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>()`, `TryAddScoped<IAISystemService, DefaultAISystemService>()`, `TryAddScoped<IHumanOversightService, DefaultHumanOversightService>()`.
   - Change `IAIActClassifier` and `IAIActDocumentation` to `TryAddScoped`.
   - Design Choice 5A: call `services.AddAggregateRepository<AISystemAggregate>()`, `AddAggregateRepository<HumanOversightAggregate>()`, `AddProjection<AISystemProjection, AISystemReadModel>()`, `AddProjection<HumanOversightProjection, HumanOversightReadModel>()` (`src/Encina.Marten/ServiceCollectionExtensions.cs:118,194`). With 5B these four calls go to a new `AIActMartenExtensions.AddAIActAggregates()` instead.
   - XML docs: the application must call `AddEncinaMarten(...)` (and `AddEncinaCaching` or another `ICacheProvider` registration when `RegistryCacheDuration > 0`).
4. **Modify `Health/AIActHealthCheck.cs`**: resolve `IAISystemService` instead of `IAISystemRegistry` (line 108); replace `GetAllSystemsAsync` (line 143) with `CountSystemsAsync`; a `Left` from the store is `Unhealthy` with the error code only; drop the `IHumanOversightEnforcer` check (line 180) and check `IHumanOversightService` resolution.
5. **Delete** the `IHumanOversightEnforcer`/`IAISystemRegistry` mentions from `README.md` and XML docs.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #847 (Encina.Compliance.AIAct to Marten event sourcing).

CONTEXT:
- Phases 1-4 added aggregates, projections, services and rewired the engine.
- AddEncinaAIAct (src/Encina.Compliance.AIAct/ServiceCollectionExtensions.cs:68) still registers the in-memory types.
- Registration completeness (AGENTS.md section 3): an AddEncina* method registers every option and dependency
  its services resolve, proven by a DI test with ValidateOnBuild and ValidateScopes.

TASK:
1. Add RegistryCacheDuration, RequireTenantContext and AutoRegistrationTenantId to AIActOptions (+ validator).
2. Update AddEncinaAIAct: remove the in-memory registrations, register the two services (scoped), make the
   classifier and documentation scoped, register the two aggregate repositories and two projections.
3. Update AIActHealthCheck to use IAISystemService.CountSystemsAsync and IHumanOversightService.

KEY RULES:
- TryAdd* everywhere so applications can replace any service.
- The Marten store (AddEncinaMarten) is the application's registration; document it in XML docs and README.
- Health check keeps DefaultName = "encina-aiact", its Tags, and scoped resolution through CreateScope().
- Health results never contain EncinaError.Message; record the error code.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/ServiceCollectionExtensions.cs
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs
- src/Encina.Compliance.Consent/ConsentMartenExtensions.cs
- src/Encina.Marten/ServiceCollectionExtensions.cs (AddAggregateRepository line 118, AddProjection line 194)
- src/Encina.Compliance.AIAct/Health/AIActHealthCheck.cs
```

</details>

---

### Phase 6: Cross-Cutting Integration

> **Goal**: Tenant and module scope, fail-closed tenant enforcement, cache segmentation, transactions and idempotency.

<details>
<summary><strong>Tasks</strong></summary>

1. **Tenant scope in both services** (Design Choice 7A), copying `DefaultConsentService.cs:107-128`:
   - `CurrentTenantId` from `IRequestContextAccessor.RequestContext?.TenantId`.
   - `IsTenantContextRequired => options.RequireTenantContext ?? (tenantProvider is not null)`; missing tenant when required returns `AIActErrors.TenantContextRequired` (fail closed); an explicit opt-out in a multi-tenant application is logged once at construction (`AIActTenantEnforcementOptedOut`).
   - Stream ids include the tenant (`AIActStreamIds.ForSystem(tenant, systemId)`), so a lookup never crosses tenants; list queries filter `TenantId == current`; an oversight session loaded by id from another tenant is reported as not found, never mutated.
   - Global registrations (auto-registration without a tenant) are visible to all tenants only through an explicit fallback: lookup by tenant id first, then by the global id. Document the rule in XML docs.
2. **Module scope**: `ModuleId` from `IModuleExecutionContext.CurrentModule` (`src/Encina/Modules/Isolation/IModuleExecutionContext.cs:58`), optional dependency, stamped on `AISystemRegistered` and `HumanOversightRequested`; module-scoped enforcement stays in #846.
3. **Cache keys** contain the tenant segment (`aiact:tenant:{segment}:system:{streamId}`); never a key without tenant in a multi-tenant application (#1315, #1949).
4. **Transactions**: event append and inline projection commit in one `SaveChangesAsync` (Marten); no extra unit of work.
5. **Idempotency**: registration idempotent by stream id; projection handlers idempotent; commands that would not change state throw and map to `InvalidStateTransition` (no duplicate events).
6. **Resilience**: deferred to the issue drafted by the #842 plan, `artifacts/issues/plan-842-aiact-marten-resilience.md` (worktree `wplans03`, to be opened by the orchestrator), which already names #847's aggregate saves. No second issue file.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #847 (Encina.Compliance.AIAct to Marten event sourcing).

CONTEXT:
- DefaultAISystemService and DefaultHumanOversightService exist (Phase 3) but resolve tenant and module naively.
- Consent fixed a cross-tenant leak in #1315; LawfulBasis has the same open bug (#1949). AIAct must not repeat it.
- Compliance gates fail closed when the tenant is missing (AGENTS.md section 3, SPEC-002 DEC-006).

TASK:
1. Add tenant resolution and fail-closed enforcement to both services (Consent pattern).
2. Include the tenant in stream ids, list filters and cache keys; implement the global-registration fallback.
3. Stamp ModuleId from IModuleExecutionContext on the first event of each stream.
4. Verify append + projection are one transaction and that repeated commands do not create duplicate events.

KEY RULES:
- Missing tenant + required = Left(aiact.tenant_context_required), never a silent global read.
- An explicit opt-out (RequireTenantContext = false) in a multi-tenant app is logged, never silent.
- A record of another tenant is "not found", never returned or mutated.
- Tenant and module ids may be logged; actor ids may not.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs (lines 82-128, TryResolveTenantScope)
- tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs
- src/Encina/Abstractions/IRequestContextAccessor.cs
- src/Encina/Modules/Isolation/IModuleExecutionContext.cs
```

</details>

---

### Phase 7: Observability

> **Goal**: Traces, metrics and source-generated logs for the service operations.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `Diagnostics/AIActDiagnostics.cs`**: activities `AIAct.System.Register`, `AIAct.System.Reclassify`, `AIAct.System.Decommission`, `AIAct.System.Reactivate`, `AIAct.Oversight.Request`, `AIAct.Oversight.Decide`, `AIAct.Oversight.Escalate`, `AIAct.Oversight.Override` (style of `AIAct.ComplianceCheck`, line 75); counters `aiact.system.registered`, `aiact.system.reclassified`, `aiact.system.decommissioned`, `aiact.system.reactivated`, `aiact.oversight.requested`, `aiact.oversight.decided` (tag `aiact.decision`), `aiact.oversight.escalated`, `aiact.oversight.overridden`, `aiact.registry.cache` (tag `aiact.outcome` = hit/miss); tags `aiact.tenant_id`, `aiact.module_id` added to the existing compliance-check activity.
2. **Create `Diagnostics/AIActServiceLogMessages.cs`** — `internal static partial class` with `[LoggerMessage]`, EventIds packed from **9513** inside `ComplianceAIAct = (9500, 9599)` (no new range): 9513 `SystemRegistered`, 9514 `SystemAlreadyRegistered`, 9515 `SystemReclassified`, 9516 `SystemDecommissioned`, 9517 `SystemReactivated`, 9518 `SystemNotFound`, 9519 `RegistryCacheHit`, 9520 `InvalidStateTransition`, 9521 `ServiceError` (exception through `ForLogging()`), 9522 `TenantContextMissing`, 9523 `TenantEnforcementOptedOut`, 9524 `OversightRequested`, 9525 `OversightDecisionRecorded`, 9526 `OversightEscalated`, 9527 `OversightOverridden`, 9528 `AutoRegistrationSystemSkipped`, 9529 `AutoRegistrationSystemFailed`. XML doc: "Event IDs: 9513-9529 (see EventIdRanges.ComplianceAIAct)". #842 then starts at 9530.
3. `EncinaEventIdAllocationTests` (`tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`) already maps the AIAct assembly; run the architecture tests.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #847 (Encina.Compliance.AIAct to Marten event sourcing).

CONTEXT:
- AIAct logs with LoggerMessage.Define, EventIds 9500-9512 (Diagnostics/AIActLogMessages.cs:25-157), range
  ComplianceAIAct = (9500, 9599) in src/Encina/Diagnostics/EventIdRanges.cs:375.
- ActivitySource and Meter "Encina.Compliance.AIAct" exist in Diagnostics/AIActDiagnostics.cs.

TASK:
1. Add the activities, counters and tags listed in the plan to AIActDiagnostics.
2. Create Diagnostics/AIActServiceLogMessages.cs with [LoggerMessage] EventIds 9513-9529 and use them in the
   services and the auto-registration hosted service.
3. Run the architecture tests (EncinaEventIdAllocationTests, EventIdUniquenessRule).

KEY RULES:
- New code uses the [LoggerMessage] source generator; ids packed sequentially, no gaps, inside the registered range.
- Never log EncinaError.Message, exception messages, actor ids or rationale text; log codes, system ids,
  session ids, tenant and module ids.
- Counter tags are low-cardinality (no system id on counters that could explode; system id only on activities).

REFERENCE FILES:
- src/Encina.Compliance.AIAct/Diagnostics/AIActDiagnostics.cs
- src/Encina.Compliance.AIAct/Diagnostics/AIActLogMessages.cs
- src/Encina.Compliance.Consent/Diagnostics/ConsentLogMessages.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 8: Testing

> **Goal**: Every flag reaches its per-file target, including a new integration target on real Marten/PostgreSQL.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/Compliance/AIAct/`):
   - `Aggregates/AISystemAggregateTests.cs`, `Aggregates/HumanOversightAggregateTests.cs` — every transition and every invalid transition.
   - `ReadModels/AISystemProjectionTests.cs`, `ReadModels/HumanOversightProjectionTests.cs` — creation, each handler, idempotency.
   - `Services/DefaultAISystemServiceTests.cs`, `Services/DefaultHumanOversightServiceTests.cs` — NSubstitute mocks of `IAggregateRepository<T>`/`IReadModelRepository<T>`/`ICacheProvider`; `StreamAlreadyExists` mapping, awaited invalidation failure returns `Left`, tenant required/opt-out/mismatch, error codes never carry exception messages.
   - `AIActStreamIdsTests.cs` — determinism, tenant separation, RFC 9562 version and variant bits.
   - Update `DefaultAIActComplianceValidatorTests.cs`, `DefaultAIActClassifierTests.cs`, `DefaultAIActDocumentationTests.cs`, `AIActAutoRegistrationHostedServiceTests.cs`, `AIActHealthCheckTests.cs`, `ServiceCollectionExtensionsTests.cs` (DI test with `ValidateOnBuild` and `ValidateScopes`, Marten abstractions substituted).
   - Delete `InMemoryAISystemRegistryTests.cs`, `DefaultHumanOversightEnforcerTests.cs`, `HumanDecisionRecordTests.cs`.
2. **Guard** (`tests/Encina.GuardTests/Compliance/AIAct/AIActGuardTests.cs`): constructors and public methods of aggregates, services, projections, `AIActStreamIds`.
3. **Contract** (`tests/Encina.ContractTests/Compliance/AIAct/AIActContractTests.cs`): instantiate `DefaultAISystemService`/`DefaultHumanOversightService` with substitutes and assert the interface contract (Left on not found, Right on success, no exceptions).
4. **Property** (`tests/Encina.PropertyTests/Compliance/AIAct/AIActPropertyTests.cs`, FsCheck through `Encina.Testing.FsCheck`): replaying any valid command sequence gives the same aggregate state as the projection; a decommissioned system rejects reclassification; `IsApproved` iff last decision is `Approved`; `ForSystem` is injective across tenant/system pairs in generated samples.
5. **Integration** (`tests/Encina.IntegrationTests/Compliance/AIAct/`, `[Collection(MartenCollection.Name)]`, `MartenFixture.cs:110-118`, `[Trait("Category", "Integration")]`, `[Trait("Database", "PostgreSQL")]`):
   - `AIActAggregateIntegrationTests.cs` — register → reclassify → decommission → reactivate round trip; history returns the events in order; concurrent registration of the same system from two sessions creates one stream.
   - `AIActTenantScopingIntegrationTests.cs` — tenant A cannot see or mutate tenant B's system or session; missing tenant fails closed.
   - `AIActPipelineIntegrationTests.cs` — full `AddEncinaAIAct` + `AddEncinaMarten`: a system reclassified to a prohibited practice is blocked on the next request (cache invalidation awaited).
   - At least one test registers both projections with a real store (`AGENTS.md` §3, #949).
6. **Load / Benchmark**: update `tests/Encina.LoadTests/Compliance/AIAct/AIAct.md` and `tests/Encina.BenchmarkTests/Compliance/AIAct/AIAct.md` (justifications) to the new design; a benchmark of the validator hot path (cache hit vs miss) is recommended but optional.
7. **Coverage manifest** `.github/coverage-manifest/Encina.Compliance.AIAct.json`: per-file targets with one-sentence justifications for every new and touched file (#1762), including an `integration` target for the services and projections; run `coverage-report.cs --check-justifications`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of issue #847 (Encina.Compliance.AIAct to Marten event sourcing).

CONTEXT:
- Production code of Phases 1-7 is complete. AIAct has unit, guard, contract and property tests but no
  integration tests; this phase adds them on the shared Marten fixture.
- Event-sourced compliance modules have no InMemory stores: unit tests mock IAggregateRepository<T> and
  IReadModelRepository<T>; integration tests run on Marten/PostgreSQL via Testcontainers.

TASK:
Write the unit, guard, contract, property and integration tests listed in the plan, update the existing tests
that used the in-memory types, delete the obsolete test files, update the load/benchmark justifications and the
coverage manifest (per-file targets + justifications, new integration target).

KEY RULES:
- Tests execute real package code (no reflection-only tests); Shouldly through Encina.Testing.Shouldly; no
  FluentAssertions; FsCheck through Encina.Testing.FsCheck.
- Integration classes use [Collection(MartenCollection.Name)], never IClassFixture or new MartenFixture();
  never dispose the fixture from a test.
- FakeTimeProvider for time; no Thread.Sleep; no hard-coded dates where avoidable.
- Each flag reaches its own manifest target for every new or touched file; measure per flag with
  dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests
  then dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report.
- CRAP <= 10 on every changed method (crap-gate).

REFERENCE FILES:
- tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs
- tests/Encina.IntegrationTests/Compliance/BreachNotification/BreachNotificationAggregateIntegrationTests.cs
- tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs
- tests/Encina.UnitTests/Compliance/AIAct/
- .github/coverage-manifest/Encina.Compliance.AIAct.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 9: Documentation & Finalization

> **Goal**: Documentation, changelog, ADR, public API and the final build/test verification.

<details>
<summary><strong>Tasks</strong></summary>

1. XML doc comments (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`) on every new public API; AI Act articles and the Reg. (EU) 2026/1744 dates cited.
2. `changelog.d/847-aiact-marten-event-sourcing.changed.md` — AIAct persists its registry and oversight sessions with Marten; `IAISystemRegistry`/`IHumanOversightEnforcer` replaced by `IAISystemService`/`IHumanOversightService`; PostgreSQL (Marten) now required. Never edit `CHANGELOG.md` directly.
3. `src/Encina.Compliance.AIAct/README.md` — registration with `AddEncinaMarten`, service usage, tenant rules, migration note for the removed types.
4. `docs/features/aiact-compliance.md` — event-sourcing section: aggregates, events, read models, history query, caching rule, dates (a docs-writer applies the `encina-docs` skill).
5. ADR (next free number in `docs/architecture/adr/index.md`): deterministic natural-key stream identity for compliance aggregates (Design Choice 3), referencing ADR-019/027; add AIAct to the module list of ADR-019 if it keeps one.
6. `docs/INVENTORY.md` — new folders and files; `ROADMAP.md` — v0.16.0 AI Act: #847 done.
7. `PublicAPI.Unshipped.txt` — all new symbols added, removed symbols deleted (RS0016/RS0017 clean).
8. Delete `docs/plans/aiact-marten-es-migration-plan.md` (superseded by this plan) and update the link in #847's body.
9. Build: `dotnet build Encina.slnx --configuration Release` → 0 errors, 0 warnings.
10. Tests: `dotnet test Encina.slnx --configuration Release` → all pass; every coverage flag (unit, guard, contract, property, integration) reaches its target in `.github/coverage-manifest/Encina.Compliance.AIAct.json`; crap-gate table clean.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of issue #847 (Encina.Compliance.AIAct to Marten event sourcing).

CONTEXT:
- Code and tests of Phases 1-8 are complete.
- Documentation pages are owned by docs-writer (encina-docs skill); changelog fragments and PublicAPI lines by
  mechanical-fixer.

TASK:
1. Complete XML docs; write changelog.d/847-aiact-marten-event-sourcing.changed.md.
2. Update the package README, docs/features/aiact-compliance.md, docs/INVENTORY.md and ROADMAP.md.
3. Write the ADR for deterministic natural-key stream identity (next free number from the ADR index).
4. Reconcile PublicAPI.Unshipped.txt; delete docs/plans/aiact-marten-es-migration-plan.md.
5. Run the Release build and the full test suite; produce the per-flag coverage and crap-gate tables.

KEY RULES:
- English only; no AI attribution in commits or PRs; no hand-typed coverage figures in docs (covref markers).
- Never edit CHANGELOG.md's Unreleased section by hand.
- Zero warnings; RS0016/RS0017 resolved.

REFERENCE FILES:
- changelog.d/README.md
- docs/architecture/adr/019-compliance-event-sourcing-marten.md
- docs/architecture/adr/index.md
- docs/features/aiact-compliance.md
- src/Encina.Compliance.AIAct/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Provision | Requirement | Where in this plan |
|-----------|-------------|--------------------|
| AI Act Art. 5 | Prohibited practices are blocked | Reclassification takes effect on the next request (Design Choice 6) |
| AI Act Art. 6(3) | A provider that considers an Annex III system not high-risk documents that assessment | `AISystemReclassified` with reason and actor; history query |
| AI Act Art. 14(4) | Human overseers can decide not to use, override or reverse the output | `HumanOversightAggregate` (decide, escalate, override) |
| AI Act Art. 49, Art. 71 | Registration of high-risk systems in the EU database | `AISystemRegistered`, `AISystemDecommissioned` give the internal record behind it |
| AI Act Art. 51 | Classification of general-purpose AI models with systemic risk | Category and risk level on the aggregate |
| AI Act Art. 12 | Automatic logging of each use | Out of scope here: #842 (Design Choice 4) |
| Reg. (EU) 2026/1744 | Annex III high-risk obligations from 2 Dec 2027, Annex I from 2 Aug 2028; new Art. 5(1)(ba)/(bb) from 2 Dec 2026 | XML docs and feature page; no date logic |
| RFC 9562 | UUID version 5 (name-based, SHA-1) | `AIActStreamIds` (Design Choice 3) |
| SPEC-002 DEC-006 | Compliance gates fail closed | Tenant enforcement, store failure blocks |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `AggregateBase` | `src/Encina.DomainModeling/AggregateBase.cs:64` | Base of both aggregates |
| `IAggregateRepository<T>` / `MartenAggregateRepository<T>` | `src/Encina.Marten/IAggregateRepository.cs:10`, `MartenAggregateRepository.cs:270-320` | Create/load/save; `StreamAlreadyExists` for idempotent registration |
| `IReadModelRepository<T>`, `IProjection<T>` | `src/Encina.Marten/Projections/IReadModelRepository.cs:43`, `IProjection.cs:56` | Read models and inline projections |
| `AddAggregateRepository<T>`, `AddProjection<,>` | `src/Encina.Marten/ServiceCollectionExtensions.cs:118,194` | Registration |
| `EventPublishingPipelineBehavior` | `src/Encina.Marten/EventPublishingPipelineBehavior.cs` | Publishes the events as notifications inside a request |
| Consent module (reference) | `src/Encina.Compliance.Consent/` (`Services/DefaultConsentService.cs:55-128`) | Service shape, tenant fail-closed pattern, cache usage |
| `ICacheProvider` | `src/Encina.Caching/` | Registry read cache |
| `IRequestContextAccessor`, `ITenantProvider` | `src/Encina/Abstractions/IRequestContextAccessor.cs`, `src/Encina.Tenancy/` | Tenant resolution |
| `IModuleExecutionContext` | `src/Encina/Modules/Isolation/IModuleExecutionContext.cs:58` | Module id |
| `AIActDiagnostics`, `AIActLogMessages` | `src/Encina.Compliance.AIAct/Diagnostics/` | Extended instruments, logging style |
| `MartenFixture` / `MartenCollection` | `tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs:13,110-118` | Integration tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.AIAct` (#415) | 9500-9512 | In use, `AIActLogMessages.cs:25-157` |
| **`Encina.Compliance.AIAct` (#847, this plan)** | **9513-9529** | Inside `ComplianceAIAct = (9500, 9599)`; no new range; packed |
| `Encina.Compliance.AIAct` (#842) | from 9530 | The #842 plan packs right after #847's last id |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Events, aggregates, enums, stream ids (Phase 1) | 8 new, 1 modified (csproj), 2 deleted | |
| Read models and projections (Phase 2) | 4 new | |
| Services, abstractions, request model, errors (Phase 3) | 5 new, 1 modified, 5 deleted | |
| Engine rewiring (Phase 4) | 4 modified | Classifier, documentation, validator, hosted service |
| Options, DI, health check (Phase 5) | 4 modified | |
| Observability (Phase 7) | 1 new, 1 modified | |
| Tests (Phase 8) | ~13 new, ~9 modified, 3 deleted | Includes 3 integration test classes |
| Docs, changelog, ADR, manifest, PublicAPI (Phase 9) | 2 new, ~6 modified, 1 deleted | |
| **Total** | **~33 new, ~26 modified, ~11 deleted** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #847 — move Encina.Compliance.AIAct's AI system registry and human oversight records
from process memory to Marten event sourcing.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0: no backward compatibility, best solution always, no
  compatibility layers.
- Railway Oriented Programming: every operation returns Either<EncinaError, T>.
- Compliance modules use Marten event sourcing on PostgreSQL (ADR-019, ADR-027); event-sourced modules have no
  in-memory stores. Consent is the reference module.
- Today: InMemoryAISystemRegistry.cs:33 and DefaultHumanOversightEnforcer.cs:31 (ConcurrentDictionary);
  IAISystemRegistry.IsRegistered is synchronous; HasHumanApprovalAsync treats a rejection as an approval.

IMPLEMENTATION OVERVIEW:
Phase 1: Marten/DomainModeling/Caching/Tenancy references; AISystemAggregate (register, reclassify,
         decommission, reactivate) and HumanOversightAggregate (request, decide, escalate, override); events;
         enums; AIActStreamIds (UUIDv5 of tenant + systemId).
Phase 2: AISystemReadModel/HumanOversightReadModel and inline projections.
Phase 3: IAISystemService/IHumanOversightService + default services; old ports and in-memory types deleted.
Phase 4: Classifier, documentation, validator and auto-registration use the services (async, fail closed).
Phase 5: Options (cache duration, tenant requirement), AddEncinaAIAct registers services, repositories and
         projections; health check counts systems.
Phase 6: Tenant/module scope, fail-closed tenant enforcement, tenant-segmented cache keys, idempotency.
Phase 7: Activities, counters, [LoggerMessage] EventIds 9513-9529.
Phase 8: Unit, guard, contract, property and Marten integration tests; coverage manifest with integration target.
Phase 9: XML docs, changelog fragment, README, feature page, ADR, PublicAPI, build and test verification.

KEY PATTERNS:
- Aggregates extend AggregateBase, RaiseEvent() + Apply(); events are sealed records implementing INotification.
- Services: IAggregateRepository<T> for writes, IReadModelRepository<T> for reads, ICacheProvider with awaited
  invalidation; InvalidOperationException -> InvalidStateTransition; StreamAlreadyExists -> SystemAlreadyRegistered.
- TimeProvider for timestamps; TryAdd* registrations; scoped services; hosted service uses a scope.
- Never log EncinaError.Message, exception messages or actor ids; ex.ForLogging() for exceptions.
- Out of scope: per-use Art. 12 records (#842), oversight gate and review queue (#839), per-tenant options (#845),
  module-scoped enforcement (#846), resilience (issue drafted by the #842 plan).

REFERENCE FILES:
- src/Encina.Compliance.Consent/ (Aggregates, Events, ReadModels, Services, ServiceCollectionExtensions.cs)
- src/Encina.Compliance.CrossBorderTransfer/ (multiple aggregates in one module)
- src/Encina.Marten/ (IAggregateRepository.cs, MartenAggregateRepository.cs, Projections/, ServiceCollectionExtensions.cs)
- src/Encina.Compliance.AIAct/ (all files)
- tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs
- docs/architecture/adr/019-compliance-event-sourcing-marten.md
- docs/plans/aiact-record-keeping-implementation-plan-842.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ✅ Phases 3, 6 | Registry reads cached through `ICacheProvider` with a short TTL, tenant-segmented keys and awaited invalidation (Design Choice 6); oversight reads not cached |
| 2 | **OpenTelemetry** | ✅ Phase 7 | Eight activities and nine counters on the existing `Encina.Compliance.AIAct` source and meter; tenant and module tags on the compliance check |
| 3 | **Structured Logging** | ✅ Phase 7 | `[LoggerMessage]` EventIds 9513-9529 in `ComplianceAIAct`; hosted service inline logs replaced |
| 4 | **Health Checks** | ✅ Phase 5 | `AIActHealthCheck` counts systems through the read model (no full load) and reports store failures as Unhealthy with the error code |
| 5 | **Validation** | ✅ Phases 1, 5 | Aggregate invariants and guards; options validator for the cache duration; registration request validated in the service |
| 6 | **Resilience** | ⏭️ Issue file `artifacts/issues/plan-842-aiact-marten-resilience.md` (worktree `wplans03`, to be opened once) | Covers #847's aggregate saves and #842's log; Marten calls have no retry or circuit breaker today |
| 7 | **Distributed Locks** | ❌ N/A | Deterministic stream ids plus Marten's stream-collision and optimistic-concurrency checks make concurrent registration and updates safe without a lock |
| 8 | **Transactions** | ✅ Phase 6 | Event append and inline projection commit in one Marten `SaveChangesAsync` |
| 9 | **Idempotency** | ✅ Phases 3, 6 | Registration idempotent by stream id; projections idempotent; no-op commands rejected instead of appending duplicates |
| 10 | **Multi-Tenancy** | ✅ Phase 6 | `TenantId` on events, aggregates, read models, stream ids and cache keys; fail-closed tenant enforcement; per-tenant options stay in #845 |
| 11 | **Module Isolation** | ✅ Phase 6 | `ModuleId` stamped from `IModuleExecutionContext`; module-scoped enforcement stays in #846 |
| 12 | **Audit Trail** | ✅ Phases 1-3 | The event streams are the audit trail (who registered, reclassified, decided, overrode, when and why); history query exposes them |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | State | Why |
|---|---|---|
| Marten infrastructure (`Encina.Marten`, `MartenFixture`) | Available | Repositories, projections, integration fixture |
| Maintainer decisions on Design Choices 1-8 | Pending | Choices 1, 4, 5 and 7 change the issue's acceptance criteria |

No open issue blocks this work.

### Recommended (Not Blocking)

- [#842](https://github.com/dlrivada/Encina/issues/842) follows this issue (its plan's Design Choice 1); its EventIds start after 9529.
- [#839](https://github.com/dlrivada/Encina/issues/839): rewrite its body to build the review queue, deadlines, pipeline trigger and Block-mode gate on `IHumanOversightService` (its "13 database providers" `IHumanDecisionStore` is obsolete).
- [#845](https://github.com/dlrivada/Encina/issues/845), [#846](https://github.com/dlrivada/Encina/issues/846): drop the in-memory tenant/module-aware registries from their bodies; keep per-tenant options and module-scoped enforcement.
- [#1204](https://github.com/dlrivada/Encina/issues/1204): the integration tests of Phase 8 cover the AIAct part of it.
- [#1949](https://github.com/dlrivada/Encina/issues/1949): same tenant-leak class in LawfulBasis; Phase 6 avoids it in AIAct.

---

## Next Steps

1. The maintainer decides Design Choices 1-8; the orchestrator records the decisions in this plan.
2. Update #847's body: link this plan instead of `aiact-marten-es-migration-plan.md`, EventIds 9513-9529, no `AISystemComplianceEvaluated`, per-flag coverage instead of 85%, ADR number.
3. Update #839, #845 and #846 as listed under Recommended; open the resilience issue from the #842 plan's file once.
4. Implement one phase per commit in a worktree; final PR with `Fixes #847`; then start #842.
