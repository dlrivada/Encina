# Implementation Plan: `Encina.Compliance.NIS2` — Reporting, Read Models & Compliance Posture (CQRS)

> **Issue**: [#827](https://github.com/dlrivada/Encina/issues/827)
> **Type**: Feature
> **Complexity**: Medium (one cross-aggregate query service, one new read model, no new aggregate; 4 pull requests)
> **Estimated Scope**: 52 file touches and about 3,300 lines including tests and per-PR metadata; split into 4 pull requests, see "Pull request split"
> **Milestone**: v0.15.0 — EU Compliance: NIS2 & Digital Omnibus
> **Provider Category**: None for databases (read side of the Marten event-sourced NIS2 modules; AGENTS.md section 5 excludes event sourcing from the database matrix)
> **Family**: `compliance-nis2-2` reads the five event-sourced NIS2 modules of #822 to #826. #822 owns the shared pieces (Marten references, `AddNIS2EventSourcing`, ADR); this plan owns only the reporting layer.

---

## Summary

#822 to #826 each add an event-sourced aggregate with its own inline Marten read model (`NIS2IncidentReadModel`, `SupplierAssessmentReadModel`, `RiskAnalysisReadModel`, `VulnerabilityDisclosureReadModel`, `ManagementAccountabilityReadModel`) and a service with overdue queries. Nothing combines them. This plan adds the combination: a tenant-scoped query service `INIS2ReportingService`, the cross-aggregate `NIS2CompliancePosture`, a competent-authority report for a period and the one read model that no sibling provides, the per-incident event timeline.

**What it implements** (NIS2 Directive (EU) 2022/2555, Arts. 20, 21 and 23; reporting data for the competent authority or CSIRT):

- `INIS2ReportingService` (`GetCompliancePostureAsync`, `GetIncidentSummariesAsync`, `GetIncidentTimelineAsync`, `GetSupplierRiskDashboardAsync`, `GetRiskRegisterAsync`, `GenerateAuthorityReportAsync`) returning `Either<EncinaError, T>`, implemented over the sibling services' queries, never by replaying aggregates.
- `NIS2CompliancePosture` and `NIS2PostureLevel`, computed by a pure `NIS2CompliancePostureCalculator` (Design Choice 4).
- `NIS2IncidentTimelineReadModel` plus its inline projection, built from the event types of #822 (Design Choice 3).
- `NIS2CompetentAuthorityReport`, a typed record for a period (counts and per-incident phase timestamps, no free text).
- An optional posture cache (Design Choice 5), OpenTelemetry instruments, `[LoggerMessage]` messages and a health check addition (`Critical` posture gives `Degraded`).

**Not in scope**: transmitting a report to a CSIRT (#825 owns `INIS2CSIRTNotifier`; #812 the ENISA adapter), a user interface, report rendering (the typed record serialises with `System.Text.Json`), and any change to the five sibling aggregates.

**Affected packages**: `Encina.Compliance.NIS2` (extended; it already references `Encina.Caching`, `Encina.Security.Audit` and the Marten references added by #822) and `Encina` (EventId range registration only).

**Provider category**: none (Marten on PostgreSQL, ADR-027). The 12-function check is in the matrix at the end.

### Corrections to the issue text (checked against `origin/main` 4b0a600c7)

| Issue text | Today's code and sibling plans | Plan decision |
|---|---|---|
| Seven projections, five of them re-derive per-aggregate views (`NIS2IncidentSummary`, `SupplierRiskDashboard`, `RiskRegisterView`, `VulnerabilityDisclosureTracker`, `ManagementComplianceOverview`) | #822 to #826 each already plan an inline read model with those fields (`NIS2IncidentReadModel`, `SupplierAssessmentReadModel`, `RiskAnalysisReadModel`, `VulnerabilityDisclosureReadModel`, `ManagementAccountabilityReadModel`) | Do not duplicate; the reporting types are thin DTOs mapped from the sibling read models (Design Choice 2). New read model: the timeline only |
| Aggregate names `SupplierAssessmentAggregate`, `RiskAnalysisAggregate`, `VulnerabilityDisclosureAggregate`, `ManagementAccountabilityAggregate`, `NIS2IncidentAggregate` | Match the sibling plans | Used as written |
| "Projection lag metrics" and "health check for stale projections" | The family uses inline projections committed with the stream (#822 Design Choice on projections), so there is no async lag | Replace by a posture-level health check and a `nis2.posture.level` gauge (Design Choice 7) |
| `config.UseNIS2...`/`AddEncinaCompliance` style options | The real entry point is `AddEncinaNIS2(...)` (`ServiceCollectionExtensions.cs`) and the event-sourced one is `AddNIS2EventSourcing()` from #822 | New `AddNIS2Reporting()` extension next to them |
| Coverage "≥ 85%" | Coverage is per flag against the manifest (AGENTS.md section 9) | Numeric per-file targets in Phase 5 |
| Cache the posture | #822 plan marks caching ❌ so a cache cannot hide a missed deadline | Opt-in, off by default, Dilemma 5 |

### Shared pieces

| Shared piece | Owner | Used here as |
|---|---|---|
| `ComplianceNIS2EventSourced` range 9800-9899 | #822 (fully allocated: 9800-9899 across #822 to #826) | Not usable: no free id |
| Marten and DomainModeling references, `NIS2MartenExtensions` | #822 | `AddNIS2Reporting()` adds the timeline projection line there |
| Tenant and module filter on read models (`TenantId`, `ModuleId` properties) | #822 to #826 | Every query filters by the resolved tenant |
| New EventId range `ComplianceNIS2Reporting = (52750, 52799)` | this plan, PR 1 | 20 ids 52750-52769 used (Phase 6) |

---

## Design Choices

<details>
<summary><strong>1. Package placement — extend <code>Encina.Compliance.NIS2</code></strong></summary>

- **Class**: Settled
- **Settled by**: issue #827 (Affected Packages: "existing package, extended"); the sibling plans #822 to #826 place their modules in the same package.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extend `Encina.Compliance.NIS2`** | Reads the five services in-process; shares options, diagnostics and `NIS2Errors` | The package grows (sixth module) |
| B) New package `Encina.Compliance.NIS2.Reporting` | Isolated | The sibling services and read models would have to be public; a package for one service |

### Chosen Option: **A**

### Rationale

- The read models and services are consumed with the same lifetime and the same tenant context; a second package adds public surface only to cross a boundary the family does not need.

</details>

<details>
<summary><strong>2. Reuse the sibling read models; add only the timeline</strong></summary>

- **Class**: Trivial
- **Settled by**: issue #827 "Alternatives Considered" 2 (custom views rejected: "duplicates logic"); the read models planned in #822 to #826.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Map the sibling read models into reporting DTOs through the sibling services' queries** | No second projection per aggregate, one source of truth | The reporting layer depends on five services; each is resolved optionally (a module that is not registered contributes zero to the posture and is reported as `NotConfigured`) |
| B) Seven new projections as the issue lists | Matches the issue text | Two projections of the same stream that can disagree |

### Chosen Option: **A**

### Rationale

- A module that is not registered is reported as not configured, never as compliant: the posture treats a missing module as `Unknown` and the level cannot be `Good` or `Excellent` while a module is `Unknown` (Design Choice 4).

</details>

<details>
<summary><strong>3. Incident timeline — inline projection of event metadata, or read the stream on demand</strong></summary>

- **Class**: Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Inline projection `NIS2IncidentTimelineReadModel`: one document per incident holding `(EventType, OccurredAtUtc, Phase, ActorKind)` entries and no payload** | Cheap tenant-filtered query; no personal data stored (Dilemma 6 of the #822 plan is unaffected); rebuild from history is the same code path | One more document per incident; the projection must handle every event type of #822 and the new ones added later |
| B) Read the incident event stream through `IAggregateRepository` on demand | No extra storage | Needs the event payload (which may hold personal data, #822 Dilemma 6); cost grows with stream length; no pagination |

### Chosen Option: **A (recommended, pending the maintainer)**

### Rationale

- A wins when the timeline is shown on a dashboard and queried often; B wins when incidents are few, the timeline is a rare audit read and the extra document is not wanted. The reporting layer must not become the place where personal data in events is read, so A is recommended.

</details>

<details>
<summary><strong>4. Posture level — fixed rule table, or weighted numeric score</strong></summary>

- **Class**: Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Deterministic worst-of rule table: `Critical` if any incident is overdue or any supplier assessment is overdue with `Critical` risk or a module is `Unknown` for a registered package that failed; `Warning` if any overdue risk review, expiring embargo, lapsed training or acknowledgment; `Good` if none and every module configured; `Excellent` if additionally no open incident and no high-risk supplier** | Explainable to a regulator line by line; property-testable (monotonic: adding an overdue item never improves the level) | Coarse; one overdue item moves the whole level |
| B) Weighted numeric score with thresholds | Finer trend over time | Weights are arbitrary and hard to defend to an authority; thresholds need tuning |

### Chosen Option: **A (recommended, pending the maintainer)**

### Rationale

- B wins for an entity that wants a trend line and accepts configurable weights; A wins where the level must be defended to an authority. Both keep the counters of the issue's record; only the `OverallPosture` rule differs. A missing module never raises the level (fail closed).

</details>

<details>
<summary><strong>5. Posture cache — none, or an opt-in short-lived cache</strong></summary>

- **Class**: Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A) No cache | A missed deadline is visible at once (the rule of the #822 plan, matrix row 1) | Each dashboard refresh runs five queries |
| **B) Opt-in `NIS2ReportingOptions.PostureCacheDuration` (default `null` = off), cached through `ICacheProvider`, key `nis2:posture:` + the tenant segment** | The issue asks for it; relieves polling dashboards; off by default (pay-for-what-you-use) | Up to the duration of staleness; one more key space to invalidate |

### Chosen Option: **B (recommended, pending the maintainer)**

### Rationale

- A wins when deadlines are the dominant concern or load is low; B wins when many users poll the posture page. The key is injective: the tenant segment is length-prefixed (`<length>:<tenantId>`) and the reserved segment `default` is used only when no tenant context is registered; a real tenant id equal to `default` is rejected by the key builder. Authority reports are never cached.

</details>

<details>
<summary><strong>6. Query filter, validation and paging</strong></summary>

- **Class**: Trivial
- **Settled by**: the issue ("Query filters and pagination", validation row of its matrix); the `Either`-returning validation rule of AGENTS.md section 3 (Railway Oriented Programming).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `NIS2IncidentQueryFilter` record (`Status`, `MinSeverity`, `OverdueOnly`, `ModuleId`, `FromUtc`, `ToUtc`, `Skip`, `Take`); `Take` defaults to 100 and is limited to 500; an invalid filter returns `Left` with category `Validation`** | Bounded result size; no exceptions | One more public type |
| B) Unbounded lists | Simpler | Unbounded memory on a dashboard query |

### Chosen Option: **A**

### Rationale

- `FromUtc` later than `ToUtc`, `Skip` below 0 or `Take` outside 1-500 return `Left`.

</details>

<details>
<summary><strong>7. Observability of the read side — posture gauge and health check instead of projection lag</strong></summary>

- **Class**: Trivial
- **Settled by**: ADR-054 (instrument names without `_total`; tenant tag only when multi-tenancy is on, D4) and the inline-projection decision of the #822 plan.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Instruments `nis2.posture.queries` (counter, tags `outcome`), `nis2.posture.query.duration` (histogram, unit `s`), `nis2.posture.level` (observable gauge 0-3); the tenant tag when multi-tenancy is enabled; health check `Degraded` at `Critical`** | Matches what exists; no stale-projection state to measure | The gauge is per process, not global |
| B) Projection-lag metrics as the issue says | Matches the issue | Nothing to measure with inline projections |

### Chosen Option: **A**

### Rationale

- Logs carry error codes and exception types only, never `EncinaError.Message` and never raw tenant or user ids (AGENTS.md section 3).

</details>

<details>
<summary><strong>8. Authority report — typed record without free text</strong></summary>

- **Class**: Trivial
- **Settled by**: AGENTS.md section 3 (no `EncinaError.Message` or personal data in plaintext outputs); the data the #822 read model holds (phase timestamps, severity, deadlines).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `NIS2CompetentAuthorityReport` with period, generation time, counts and one `NIS2ReportedIncident` per incident (id, severity, significance, the three phase timestamps, whether each deadline was met)** | Ready for a CSIRT submission; no personal data | No narrative text (the entity adds it) |
| B) Include incident descriptions | Fuller report | Personal data and free text leave the event store |

### Chosen Option: **A**

### Rationale

- Period larger than 366 days returns `Left` (`Validation`); the report is not cached.

</details>

---

## Implementation Phases

### Pull request split (read first)

Prerequisite for every PR: #822 to #826 are merged (the services and read models exist). Each PR builds and passes alone.

| PR | Phases | Files (production, metadata) | Test files | Files / lines (approx.) |
|---|---|---|---|---|
| 1 | 1 | `EventIdRanges.cs`, `eventid-block-registry.md` row, `Encina` `PublicAPI.Unshipped.txt`, `Encina.json` manifest, `NIS2PostureLevel`, `NIS2CompliancePosture`, `NIS2IncidentQueryFilter`, `NIS2CompliancePostureCalculator`, NIS2 `PublicAPI.Unshipped.txt`, NIS2 manifest (10) | `EncinaEventIdAllocationTests` (`AssemblyRanges` entry), unit calculator, property calculator, guard filter (4) | 14 files / 800 |
| 2 | 2 | `INIS2ReportingService`, `DefaultNIS2ReportingService`, `NIS2CompetentAuthorityReport`, `NIS2ReportedIncident`, report DTOs (`NIS2SupplierRiskSummary`, `NIS2RiskRegisterSummary`, `NIS2IncidentSummary`), `NIS2Errors` edit, `PublicAPI.Unshipped.txt`, manifest (9) | unit service, guard service, contract (3) | 12 files / 1,000 |
| 3 | 3, 4 | `NIS2IncidentTimelineReadModel`, `NIS2IncidentTimelineProjection`, `NIS2MartenExtensions` edit, `NIS2ReportingOptions`, `NIS2ReportingOptionsValidator`, `ServiceCollectionExtensions` edit (`AddNIS2Reporting`), `PublicAPI.Unshipped.txt`, manifest (8) | unit projection, DI test (`ValidateOnBuild`, `ValidateScopes`), guard options, integration (Marten, PostgreSQL) (4) | 12 files / 950 |
| 4 | 5, 6, 8 | `NIS2Diagnostics` edit, `NIS2LogMessages` edit (ids 52750-52769), `NIS2ComplianceHealthCheck` edit, cache wiring in the service, `PublicAPI.Unshipped.txt`, manifest, changelog fragment, feature page, package README (9) | unit diagnostics and health check, benchmark, load-test `.md` justification (3) | 12 files / 550 |

The `AssemblyRanges` entry maps the new range to the `Encina.Compliance.NIS2` assembly (a second range for one assembly, as #822 does).

### Phase 1: Core Models, Posture Calculator & EventId Range (PR 1)

<details>
<summary>Tasks</summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs`: add `public static readonly (int Min, int Max) ComplianceNIS2Reporting = (52750, 52799);` registered first (ADR-021); add its row to `docs/engineering/eventid-block-registry.md` and the symbol to `src/Encina/PublicAPI.Unshipped.txt`; add the `Encina.Compliance.NIS2` entry to `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
2. `Model/NIS2PostureLevel.cs` (`Critical`, `Warning`, `Good`, `Excellent`) and `Model/NIS2CompliancePosture.cs` (the issue's record plus `Unknown` module list `NotConfiguredModules` and `GeneratedAtUtc`).
3. `Model/NIS2IncidentQueryFilter.cs` with a `Validate()` method returning `Either<EncinaError, Unit>` (category `Validation`).
4. `Services/NIS2CompliancePostureCalculator.cs` (internal, static, pure): `Calculate(counters, notConfigured, nowUtc)` implementing the rule table of Design Choice 4; no `DateTime.UtcNow`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
You are implementing Phase 1 of the NIS2 reporting layer for Encina (Issue #827).

CONTEXT:
- src/Encina.Compliance.NIS2 is extended; #822 to #826 are merged (read models and services exist).
- EventIds: register ComplianceNIS2Reporting (52750, 52799) in EventIdRanges.cs first.

TASK:
1. Register the range, the registry row, the PublicAPI line and the AssemblyRanges entry.
2. Create NIS2PostureLevel, NIS2CompliancePosture, NIS2IncidentQueryFilter (Validate returns Either).
3. Create the pure internal NIS2CompliancePostureCalculator with the worst-of rule table.

KEY RULES:
- ROP: Either<EncinaError, T>; invalid input returns Left (ErrorCategory.Validation, ADR-062), never throws.
- Time from the passed-in instant; never DateTime.UtcNow.
- A missing module never raises the level (fail closed).
- XML docs on every public type; PublicAPI.Unshipped.txt lines.

REFERENCE FILES: src/Encina.Compliance.NIS2/NIS2Errors.cs, src/Encina.Compliance.NIS2/Model/NIS2IncidentSeverity.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 2: Reporting Service & Authority Report (PR 2)

<details>
<summary>Tasks</summary>

1. `Abstractions/INIS2ReportingService.cs` with the six methods listed in the Summary; every method takes a `CancellationToken` and returns `ValueTask<Either<EncinaError, T>>`.
2. `Model/NIS2CompetentAuthorityReport.cs` and `Model/NIS2ReportedIncident.cs` (Design Choice 8); `NIS2IncidentSummary`, `NIS2SupplierRiskSummary`, `NIS2RiskRegisterSummary` DTOs mapped from the sibling read models.
3. `Services/DefaultNIS2ReportingService.cs` (internal; scoped): constructor takes `INIS2IncidentLifecycleService`, `INIS2SupplierAssessmentService?`, `INIS2RiskAnalysisService?`, `INIS2VulnerabilityDisclosureService?`, `INIS2ManagementAccountabilityService?` (the last four resolved optionally; a missing one is a `NotConfiguredModules` entry), `ITenantContext?`, `TimeProvider`, `ILogger`. When `ITenantContext` is registered and yields no tenant, every method returns `Left` (category `Forbidden`, code `ReportingTenantMissing`): fail closed. Optional parameters are placed last (CS1737).
4. `NIS2Errors` edit: `ReportingTenantMissing` (`Forbidden`), `ReportingFilterInvalid` (`Validation`), `ReportingPeriodInvalid` (`Validation`), `ReportingSourceFailed` (`UpstreamFailure`), `ReportingTimelineNotFound` (`NotFound`) per ADR-062.
5. Callers/implementers: `INIS2ReportingService` is new, so there is no existing implementer or fake to change (to be re-verified with `git grep` at implementation time).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
You are implementing Phase 2 of the NIS2 reporting layer for Encina (Issue #827).

CONTEXT:
- Phase 1 merged: NIS2CompliancePosture, NIS2CompliancePostureCalculator, NIS2IncidentQueryFilter.
- The sibling services (#822 to #826) expose overdue and list queries over their inline read models.

TASK:
1. Create INIS2ReportingService and the report and summary DTOs.
2. Create DefaultNIS2ReportingService (internal) composing the sibling services' queries with Either.Bind; a Left from a sibling fails the whole call (ReportingSourceFailed), never a partial success.
3. Add the five error codes to NIS2Errors with their ErrorCategory.

KEY RULES:
- Missing tenant with a registered ITenantContext returns Left Forbidden (fail closed); no tenant id or EncinaError.Message in logs.
- Optional constructor parameters last (CS1737); never inject a scoped service into a singleton.
- The authority report period is at most 366 days.

REFERENCE FILES: src/Encina.Compliance.BreachNotification/Services/DefaultBreachNotificationService.cs, src/Encina.Compliance.NIS2/NIS2Errors.cs
```

</details>

### Phase 3: Timeline Projection & Read Model (PR 3)

<details>
<summary>Tasks</summary>

1. `ReadModels/NIS2IncidentTimelineReadModel.cs`: incident id, tenant id, module id and the ordered entries `(EventType, OccurredAtUtc, Phase)`; no payload.
2. `ReadModels/NIS2IncidentTimelineProjection.cs`: inline projection for the seven incident events of #822, idempotent (replaying an event over its own result leaves the same document); constructor injection only, never `IServiceProvider` (AGENTS.md section 3).
3. `NIS2MartenExtensions.cs` edit: one `AddProjection<,>()` line (the file is created by #822).
4. `GetIncidentTimelineAsync` in the service reads it through `IReadModelRepository<NIS2IncidentTimelineReadModel>`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
You are implementing Phase 3 of the NIS2 reporting layer for Encina (Issue #827).

CONTEXT:
- Mirror src/Encina.Compliance.BreachNotification/ReadModels/BreachProjection.cs.
- Event types come from #822 (NIS2IncidentDetected, NIS2EarlyWarningSent, and so on).

TASK:
1. Create the timeline read model and the inline projection (metadata only, no payload).
2. Register it in NIS2MartenExtensions and read it from the service.

KEY RULES:
- Idempotent projection; dependencies by constructor, never IServiceProvider.
- Keep at least one test that registers the projection with a real Marten store (PostgreSQL via Testcontainers; event-sourced modules have no InMemory store).

REFERENCE FILES: src/Encina.Marten/Projections/IReadModelRepository.cs, src/Encina.Compliance.BreachNotification/BreachNotificationMartenExtensions.cs
```

</details>

### Phase 4: Configuration, DI & Cache (PR 3, cache wiring in PR 4)

<details>
<summary>Tasks</summary>

1. `NIS2ReportingOptions.cs`: `PostureCacheDuration` (`TimeSpan?`, default `null`), `DefaultPageSize` (100), `MaxPageSize` (500), `MaxReportPeriod` (366 days); validated by `NIS2ReportingOptionsValidator` (no connection string or secret, so no `[JsonIgnore]` obligation).
2. `ServiceCollectionExtensions.cs` edit: `AddNIS2Reporting()` registers the options, the validator, `INIS2ReportingService` (scoped) and the timeline projection through `AddNIS2EventSourcing()`; `TryAdd*` so an application registration wins.
3. DI test: builds the provider with `ValidateOnBuild` and `ValidateScopes`, with and without the five sibling services registered.
4. PR 4: `ICacheProvider` wiring for the posture only when `PostureCacheDuration` is set; the key builder rejects the tenant id `default`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
You are implementing Phase 4 of the NIS2 reporting layer for Encina (Issue #827).

CONTEXT:
- Phases 1 to 3 merged; the service and the timeline projection exist.

TASK:
1. Create NIS2ReportingOptions and its validator; add AddNIS2Reporting() with TryAdd*.
2. Write the DI test with ValidateOnBuild and ValidateScopes (all registration orders).
3. (PR 4) Wire the optional posture cache with an injective, length-prefixed tenant key.

KEY RULES:
- Registration completeness: every option type and dependency the service resolves is registered.
- ICacheProvider is optional; cache off by default.
- The reserved segment 'default' is used only when no tenant context is registered.

REFERENCE FILES: src/Encina.Compliance.NIS2/ServiceCollectionExtensions.cs, src/Encina.Compliance.NIS2/NIS2OptionsValidator.cs
```

</details>

### Phase 5: Cross-Cutting Integration, Health Check & Observability (PR 4)

<details>
<summary>Tasks</summary>

1. `Diagnostics/NIS2Diagnostics.cs` edit: the instruments of Design Choice 7 on the existing meter `Encina.Compliance.NIS2` and a `nis2.posture` activity on the existing `ActivitySource`; the tenant tag only when multi-tenancy is enabled (ADR-054 D4).
2. `Diagnostics/NIS2LogMessages.cs` edit: `[LoggerMessage]` ids 52750-52769, packed from 52750 (query started, completed, failed by error code, filter invalid, tenant missing, cache hit, cache miss, cache write failed, posture level changed, report generated, module not configured).
3. `Health/NIS2ComplianceHealthCheck.cs` edit: posture `Critical` gives `Degraded`, resolved optionally behind `AddHealthCheck`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
You are implementing Phase 5 of the NIS2 reporting layer for Encina (Issue #827).

CONTEXT:
- NIS2Diagnostics already owns the ActivitySource and Meter named Encina.Compliance.NIS2; the range 52750-52799 is registered (Phase 1).

TASK:
1. Add the nis2.posture instruments and activity; no _total suffix (ADR-054 decision 4).
2. Add the [LoggerMessage] messages in 52750-52769 (packed, no gaps).
3. Extend the health check: Critical posture means Degraded.

KEY RULES:
- Log error codes or exception types via ForLogging(); never EncinaError.Message, never raw tenant or user ids.
- Timer or loop code: none is added here.

REFERENCE FILES: src/Encina.Compliance.NIS2/Diagnostics/NIS2Diagnostics.cs, src/Encina.Compliance.NIS2/Health/NIS2ComplianceHealthCheck.cs
```

</details>

### Phase 6: Testing (PRs 1 to 4)

<details>
<summary>Tasks</summary>

1. Unit: calculator rule table (every level, every missing-module case), service composition, tenant missing, filter validation, projection, cache hit and miss (`FakeTimeProvider`).
2. Guard: `NIS2IncidentQueryFilter`, `DefaultNIS2ReportingService` constructor and methods (null and out-of-range), `NIS2ReportingOptionsValidator`; a nullable `ITenantContext` accepts null (valid-result test).
3. Contract: `INIS2ReportingService` (every method returns `Left` for an invalid filter and never throws).
4. Property (FsCheck): adding an overdue item never improves the level; the level of an empty tenant with all modules configured is `Excellent`; report period invariants.
5. Integration: projection and queries against Marten on PostgreSQL through the `[Collection]` fixture; rebuild from history gives the same timeline.
6. Benchmark: `Encina.BenchmarkTests/Compliance` posture query over 10,000 incidents. Load test: `.md` justification (read-only, low concurrency), per the AGENTS.md section 9 table.
7. Per-file targets (justified in the manifest): `NIS2CompliancePostureCalculator.cs` unit 95, property 80, guard 0 (internal, no parameters that are reference types beyond a counters record; reached by unit and property tests); `DefaultNIS2ReportingService.cs` unit 90, guard 85, contract 70; `NIS2IncidentQueryFilter.cs` unit 95, guard 90; `NIS2IncidentTimelineProjection.cs` unit 90, integration 80; `NIS2ReportingOptionsValidator.cs` unit 95, guard 90; `NIS2Diagnostics.cs` unit 85. CRAP of every changed method at most 10 (rule table split into small methods).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
You are implementing the tests of the NIS2 reporting layer for Encina (Issue #827).

CONTEXT:
- The production code of the PR being tested exists; the manifest is .github/coverage-manifest/Encina.Compliance.NIS2.json.

TASK: add the unit, guard, contract, property, integration and benchmark tests of the Phase 6 task list, per PR.

KEY RULES:
- Tests execute real package code (no reflection-only tests); Shouldly through Encina.Testing.Shouldly; NSubstitute for the sibling services.
- Integration tests use the shared Collection fixture for PostgreSQL; no per-class fixtures; no Thread.Sleep.
- Measure per-flag coverage against .github/coverage-manifest/Encina.Compliance.NIS2.json.

REFERENCE FILES: tests/Encina.UnitTests/Compliance/NIS2, tests/Encina.GuardTests, tests/Encina.ContractTests, tests/Encina.PropertyTests
```

</details>

### Phase 7: Documentation & Finalization (PR 4)

<details>
<summary>Tasks</summary>

1. XML docs on every public type and member of the PRs.
2. `changelog.d/827-nis2-reporting-posture.added.md`.
3. `src/Encina.Compliance.NIS2/README.md`: reporting section; `docs/features/nis2-reporting.md` (written by `docs-writer`).
4. No ADR of its own (the family ADR comes from #822); `docs/INVENTORY.md` and `ROADMAP.md` are not touched unless a file is added to them.
5. `dotnet build --configuration Release` with zero warnings; per-flag coverage reported against the manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
You are finishing the NIS2 reporting layer for Encina (Issue #827).

CONTEXT:
- All production code and tests of the four PRs exist.

TASK: XML docs, changelog fragment, README section and feature page; run the build and the tests; report per-flag coverage of the touched files against the manifest.

KEY RULES: documentation pages go to the docs-writer; fragments and PublicAPI lines to the mechanical-fixer; never type coverage figures by hand (covref markers).

REFERENCE FILES: changelog.d/README.md, src/Encina.Compliance.NIS2/README.md
```

</details>

---

## Research

### NIS2 Article References

| Article | Requirement | Applies in this plan |
| :--- | :--- | :--- |
| Art. 23(4) | Phased incident notification (early warning, notification, final report) | Authority report: per-incident phase timestamps and deadline-met flags |
| Art. 21(2) | Risk-management measures (risk analysis, supply chain, vulnerability handling) | Posture counters for risks, suppliers and disclosures |
| Art. 20 | Management oversight and training | Posture flags `ManagementTrainingCurrent`, `ManagementAcknowledgmentCurrent` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| Sibling services and read models | #822 to #826 (`Encina.Compliance.NIS2`) | Source of every counter |
| `IReadModelRepository<T>` | `src/Encina.Marten/Projections/IReadModelRepository.cs` | Timeline read |
| `BreachProjection` | `src/Encina.Compliance.BreachNotification/ReadModels/BreachProjection.cs` | Projection pattern |
| `ICacheProvider` | `Encina.Caching` (already referenced by the NIS2 csproj) | Optional posture cache |
| `NIS2Diagnostics`, `NIS2LogMessages`, `NIS2ComplianceHealthCheck` | `src/Encina.Compliance.NIS2` | Extended |
| `ErrorCategory` | `src/Encina/Errors/ErrorCategory.cs` (ADR-062) | Category of each new error code |

### Event ID Allocation

| Package | Range | Notes |
|---|---|---|
| `Encina.Compliance.NIS2` | `52750-52799` (`ComplianceNIS2Reporting`, new, Security and compliance zone) | This plan uses 52750-52769; 52770-52799 free. The 9800-9899 block is fully allocated to #822 to #826 and the older 9200-9299 range has 12 ids left (moves to 51750-51849 in #4007 SC-6). The issue's earlier 7470-7499 claim lies below 10000, outside any registered range: the registry lists it as a claim only and ADR-021 treats gaps below 10000 as not allocatable, so it is released. Slot to be confirmed against the registry by the orchestrator when the PR opens |

### Estimated File Count

| Category | Files | Notes |
|---|---|---|
| Production (models, service, projection, options, diagnostics) | 19 | Includes edits |
| Metadata (PublicAPI, manifests, registry row, `EventIdRanges.cs`, README, fragment) | 11 | Counted per PR in the split |
| Tests | 14 | Unit, guard, contract, property, integration, benchmark, `.md` |
| Documentation page | 1 | |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a .NET 10 / C# 14 pre-1.0 library; ROP with Either<EncinaError, T>; AGENTS.md rules bind. Issue #827 adds a reporting and compliance-posture layer to Encina.Compliance.NIS2 over the read models of #822 to #826.

IMPLEMENTATION OVERVIEW: Phase 1 range, models, calculator; Phase 2 service and authority report; Phase 3 timeline projection; Phase 4 options, DI, cache; Phase 5 observability and health; Phase 6 tests; Phase 7 docs. Four pull requests (see the split table), each at most 15 files and 1,500 lines.

KEY PATTERNS: fail closed on a missing tenant; injective tenant keys with the reserved 'default' segment; no EncinaError.Message or raw ids in logs; no _total counter suffix (ADR-054); ErrorCategory per ADR-062; inline projections idempotent; no InMemory store (Marten and PostgreSQL via Testcontainers); optional constructor parameters last.

REFERENCE FILES: src/Encina.Compliance.BreachNotification/ReadModels/BreachProjection.cs; src/Encina.Compliance.NIS2/ServiceCollectionExtensions.cs; src/Encina.Compliance.NIS2/Diagnostics/NIS2Diagnostics.cs; docs/plans/dsr-implementation-plan-404.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | Opt-in posture cache, off by default (Design Choice 5, Dilemma); never for authority reports |
| 2 | OpenTelemetry | ✅ | `nis2.posture.*` instruments and activity; tenant tag when multi-tenancy is on (ADR-054 D4) |
| 3 | Structured Logging | ✅ | Ids 52750-52769 in the new range, registered first |
| 4 | Health Checks | ✅ | `Critical` posture gives `Degraded`, added to the existing NIS2 health check |
| 5 | Validation | ✅ | Filter and period validation returns `Left` |
| 6 | Resilience | ❌ | Local Marten and in-process calls; no external system |
| 7 | Distributed Locks | ❌ | Read-only queries |
| 8 | Transactions | ❌ | Read-only; no commit owner needed |
| 9 | Idempotency | ❌ | Queries are idempotent; the projection is idempotent by construction |
| 10 | Multi-Tenancy | ✅ | Every query filtered by the resolved tenant; missing tenant denies (fail closed) |
| 11 | Module Isolation | ✅ | `ModuleId` is carried on the read models by #822 to #826; `NIS2IncidentQueryFilter` has an optional `ModuleId` that scopes the incident list and the posture counters |
| 12 | Audit Trail | ❌ | Read-only, no state change; report generation is logged by code only |

---

## Prerequisites & Dependencies

- #822, #823, #824, #825 and #826 merged (their services and read models). The PR 1 of this plan can start once #822 PR 1 has registered nothing it needs; PR 2 needs all five services.
- #414 (NIS2 base) is merged; the Marten references and `AddNIS2EventSourcing` come from #822 PR 1 and PR 4.
- Out of this plan: the ENISA single-entry-point adapter (#812) and the CSIRT port (#825).

## Next Steps

1. Maintainer answers Dilemmas 3, 4 and 5 when work on #827 starts; each is marked "recommended, pending the maintainer".
2. Brief PR 1 after the five sibling plans have landed their read models; PR 2 to PR 4 follow in order.
3. Run `check-plan.ps1` without `-Draft` after the adversarial review sets the `Verified` lines.
