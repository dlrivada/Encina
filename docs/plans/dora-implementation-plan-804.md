# Implementation Plan: `Encina.Compliance.DORA` — Digital Operational Resilience Act (Regulation (EU) 2022/2554)

> **Issue**: [#804](https://github.com/dlrivada/Encina/issues/804)
> **Type**: Feature (new package)
> **Complexity**: Very High (four event-sourced aggregates, classification and deadline rules, shared incident reporting; 8 pull requests)
> **Estimated Scope**: 118 file touches and about 9,800 lines including tests and per-PR metadata; split into 8 pull requests, see "Pull request split"
> **Milestone**: v0.17.0 — Compliance Lifecycle. In the 1.0 scope since SPEC-000 DEC-009 (2026-10-23, REQ-030, which supersedes SPEC-002 DEC-015); the issue's `post-1.0` label and the issue comment "stays post-1.0" are stale
> **Provider Category**: None for databases (Marten on PostgreSQL, ADR-027; AGENTS.md section 5 excludes event sourcing from the database matrix)
> **Family**: `compliance-nis2-2` (regulatory compliance packages after NIS2). Shares the incident reporting port of the #812 plan and the event-sourcing conventions of #822.

---

## Summary

DORA is lex specialis for financial entities and their ICT third-party providers. Encina has no DORA code today (`git ls-tree origin/main src` shows no `Encina.Compliance.DORA`); the closest assets are `Encina.Compliance.NIS2` (stateless rules plus the event-sourced modules of #822 to #826), `Encina.Compliance.BreachNotification` (the incident record and, through #812, the reporting port `IIncidentReportingAdapter`) and the Marten event-sourcing pattern of `BreachAggregate`.

**What it implements** (Regulation (EU) 2022/2554, with the Level 2 acts the issue comment of 2026-09-23 verified: RTS 2024/1772 classification, RTS 2024/1774 ICT risk framework, RTS 2025/301 reporting times, ITS 2025/302 templates, ITS 2024/2956 register of information, RTS 2025/1190 TLPT):

- `ICTIncidentAggregate` (Arts. 17-23): classification (major or not, by the RTS 2024/1772 criteria), the three reports (initial within 4 hours of classification and 24 hours of awareness, intermediate within 72 hours of the initial one, final within one month of the latest intermediate, RTS 2025/301) and submission through the shared reporting port.
- `ICTRiskAggregate` (Arts. 5-16): ICT asset inventory entries with criticality, risk assessments and continuity or recovery test records.
- `ICTThirdPartyProviderAggregate` (Arts. 28-44): the register of information per provider, contractual-requirement checklist, concentration flag and exit strategy.
- `ResilienceTestAggregate` (Arts. 24-27): test plan, execution, findings and remediation, including TLPT (RTS 2025/1190).
- Services, inline Marten projections, a deadline monitor, a health check and OpenTelemetry metrics, registered by `AddEncinaDORA(...)`.

**Not in scope**: transmission to a competent authority (the shared port and the concrete adapter of #812 and #1942), threat-intelligence sharing between entities (Art. 45; follow-up inside this plan, Phase 8 task 4), and anything for the other regimes (#805, #806, #807 have their own plans).

**Affected packages**: `Encina.Compliance.DORA` (new), `Encina` (EventId range registration only), `Encina.Compliance.BreachNotification` (referenced for the port; the `IncidentRegulation.DORA` member exists in the #812 plan, so no change here).

**Provider category**: none of the database, caching, transport, lock or validation categories. The 12-function check is in the matrix at the end.

### Corrections to the issue text (checked against `origin/main` 4b0a600c7)

| Issue text | Today's code and verified facts | Plan decision |
|---|---|---|
| `services.AddEncinaCompliance(config => config.UseDORA(...))` | No `AddEncinaCompliance` and no `UseDORA` exist (`git grep` on `src`); every compliance package has its own `AddEncina<Name>(Action<Options>)` (`AddEncinaNIS2` in `Encina.Compliance.NIS2/ServiceCollectionExtensions.cs`, `AddEncinaBreachNotification`) | `AddEncinaDORA(Action<DORAOptions>? configure = null)` |
| "Shared incident reporting adapter (reuse from #812)" | #812 plans `IIncidentReportingAdapter`, `IncidentReport` and `IncidentRegulation { GDPR, NIS2, DORA, CRA }` inside `Encina.Compliance.BreachNotification` (`Reporting/`) | Reuse the port (Design Choice 3, Dilemma) |
| Cross-cutting rows "Caching N/A", "Distributed locks deferred", "Idempotency deferred", "Multi-tenancy deferred" | Siblings (#822) include tenant and module ids from the first PR; re-adding them later breaks stream contracts | Tenancy included now; locks and idempotency deferred with named rationale in the matrix |
| Deadlines "per RTS" without values | Comment of 2026-09-23: initial 4 h after classification and 24 h after awareness; intermediate 72 h after the initial; final one month after the latest intermediate | Encoded once in `DORAIncidentDeadlineCalculator` |
| Coverage "≥ 85%" | Per-flag targets (AGENTS.md section 9) | Numeric per-file targets in Phase 7 |
| Related `#809` EPIC | Closed | Cited as history only |

---

## Design Choices

<details>
<summary><strong>1. Package placement — new package <code>Encina.Compliance.DORA</code></strong></summary>

- **Class**: Settled
- **Settled by**: issue #804 (Affected Packages "new package"; Alternatives Considered 2 rejects an adapter over NIS2 because risk management and third-party oversight are DORA-specific).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package** | DORA applies to financial entities only; pay-for-what-you-use (AGENTS.md section 1) | A new csproj, README, manifest and test project references |
| B) Module inside `Encina.Compliance.NIS2` | Fewer packages | Forces NIS2 users onto the Marten and financial-sector surface; the issue rejects it |

### Chosen Option: **A**

### Rationale

- The package references `Encina`, `Encina.DomainModeling`, `Encina.Marten`, `Encina.Compliance.BreachNotification` and carries the `InternalsVisibleTo` entries of every test assembly, as the BreachNotification csproj does.

</details>

<details>
<summary><strong>2. Aggregate boundaries — four aggregates, one stream per subject</strong></summary>

- **Class**: Trivial
- **Settled by**: issue #804 Implementation Tasks (four aggregates); the one-stream-per-subject convention of `BreachAggregate` and the sibling plans (#822 to #826, `AggregateBase` with `Guid Id`).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ICTIncidentAggregate` per incident, `ICTRiskAggregate` per risk assessment, `ICTThirdPartyProviderAggregate` per provider, `ResilienceTestAggregate` per test campaign** | Small streams; independent concurrency | Cross-aggregate views need projections |
| B) One aggregate per financial entity | One place | Unbounded stream, contention across all four concerns |

### Chosen Option: **A**

### Rationale

- Rule violations return `Left` with a rule code, never `InvalidOperationException` (ADR-053; #2668 builds the shared catalogue). Natural keys (provider LEI per tenant) are checked against a read model and reported as `Conflict`; the key is tenant-qualified with a length-prefixed tenant segment, the reserved segment `default` being used only without a tenant context, so no two tenant and key pairs share a key.

</details>

<details>
<summary><strong>3. Incident reporting — reuse the shared port of #812, or a DORA-specific port</strong></summary>

- **Class**: Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Map a DORA incident to `IncidentReport` and submit through `IIncidentReportingAdapter` (`Regulation = DORA`)** | One port for GDPR, NIS2, DORA; matches the issue acceptance "shared incident reporting infrastructure" and the ENISA single entry point of the Digital Omnibus | `IncidentReport` carries GDPR-shaped fields (`BreachId`, `BreachSeverity`, `EstimatedAffectedSubjects`) that a DORA incident fills by mapping; a rename to a regime-neutral name would be a change inside the #812 plan |
| B) `IICTIncidentReporter` and `ICTIncidentReport` owned by the DORA package | Regime-correct fields (classification criteria, ITS 2025/302 template) | A second port; ENISA adapters would need a second implementation |

### Chosen Option: **A (recommended, pending the maintainer)**

### Rationale

- A wins while the Digital Omnibus single entry point is the likely route for DORA, B wins if financial supervisors keep their own channels and the ITS 2025/302 template fields do not fit `IncidentReport`. A needs `IncidentRegulation.DORA`, which the #812 plan already lists.

</details>

<details>
<summary><strong>4. Personal data in events — protect the payload</strong></summary>

- **Class**: Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Events hold identifiers, enums, dates and counters only; names, contacts and free-text descriptions stay in the application and are referenced by id** | Nothing personal in an immutable stream; no crypto-shredding needed | The application must store the descriptions elsewhere |
| B) Events hold the personal fields, protected with the crypto-shredding of `Encina.Marten.GDPR` as in Dilemma 6 of the #822 plan | Self-contained history | Adds the `Encina.Marten.GDPR` reference and key management to the package |

### Chosen Option: **A (recommended, pending the maintainer; follows the outcome of Dilemma 6 of the #822 plan)**

### Rationale

- A wins for the register of information (legal entities, not people); B wins if the entity wants incident narratives in the event store. Whatever #822 decides applies here.

</details>

<details>
<summary><strong>5. Classification rules — code the RTS criteria as data</strong></summary>

- **Class**: Trivial
- **Settled by**: Delegated Regulation (EU) 2024/1772 Arts. 1-9 (criteria and thresholds), cited in the issue comment of 2026-09-23.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `DORAClassificationCriteria` options with the RTS thresholds as defaults, evaluated by a pure `DORAIncidentClassifier`** | Auditable, property-testable, thresholds follow the RTS text | The defaults must be re-checked if the RTS is amended |
| B) Leave classification to the caller | Less code | The package would not enforce its own regulation |

### Chosen Option: **A**

### Rationale

- The classifier returns `Either<EncinaError, ICTIncidentClassification>`; an incomplete input (a missing criterion) returns `Left` (`Validation`) and never classifies as non-major (fail closed).

</details>

<details>
<summary><strong>6. Deadline monitor — opt-in hosted service</strong></summary>

- **Class**: Trivial
- **Settled by**: `BreachDeadlineMonitorService` (`src/Encina.Compliance.BreachNotification/BreachDeadlineMonitorService.cs`) and AGENTS.md section 3 (a `PeriodicTimer` loop catches per iteration, a `Left` fails visibly).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Opt-in `DORADeadlineMonitorService` (`BackgroundService`, `PeriodicTimer` on `TimeProvider`), off by default, an exception per iteration is caught and logged by code; a missed deadline records an escalation event** | Pay-for-what-you-use; mirrors the breach monitor | Multi-instance deployments use the optional lock (matrix row 7) |
| B) Evaluate deadlines only on query | No background work | A missed deadline is noticed late |

### Chosen Option: **A**

### Rationale

- Cancellation from the stopping token ends the loop and is not an error.

</details>

<details>
<summary><strong>7. Observability naming</strong></summary>

- **Class**: Trivial
- **Settled by**: ADR-054 decision 4 (no `_total` suffix; names lowercase dotted) and D4 (tenant tag only when multi-tenancy is on).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Meter and `ActivitySource` `Encina.Compliance.DORA`; instruments `dora.incident.*`, `dora.risk.*`, `dora.third_party.*`, `dora.resilience_test.*`** | Matches `NIS2Diagnostics` | Four families of instruments |
| B) One flat family | Fewer names | Loses the per-aggregate split |

### Chosen Option: **A**

### Rationale

- Logs carry error codes and exception types, never `EncinaError.Message` and never raw tenant or user ids.

</details>

---

## Implementation Phases

### Pull request split (read first)

The package is built bottom-up; each PR builds and passes alone. Test project edits are `ProjectReference` additions in `Encina.UnitTests` and `Encina.GuardTests` (PR 1), `Encina.PropertyTests` and `Encina.ContractTests` (PR 2), `Encina.IntegrationTests` (PR 6).

| PR | Phases | Files (production, metadata) | Test files | Files / lines (approx.) |
|---|---|---|---|---|
| 1 | 1 | `EventIdRanges.cs`, registry row, `Encina` `PublicAPI.Unshipped.txt`, `Encina.slnx`, csproj, `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, manifest, `DORAOptions`, `DORAErrors` (11) | `AssemblyRanges` entry in `EncinaEventIdAllocationTests`, `Encina.UnitTests.csproj`, `Encina.GuardTests.csproj` references, unit options test (4) | 15 files / 700 |
| 2 | 2 | classification model, `DORAIncidentClassifier`, `DORAIncidentDeadlineCalculator`, `ICTIncidentEvents`, `ICTIncidentAggregate`, `PublicAPI.Unshipped.txt`, manifest (8) | unit classifier, unit deadlines, unit aggregate, property deadlines, `Encina.PropertyTests.csproj` reference (5) | 13 files / 1,500 |
| 3 | 3 | `ICTRiskEvents`, `ICTRiskAggregate`, asset and risk models, `PublicAPI.Unshipped.txt`, manifest (5) | unit aggregate, guard, property score (3) | 8 files / 1,000 |
| 4 | 4 | `ICTThirdPartyProviderEvents`, `ICTThirdPartyProviderAggregate`, register and concentration models, `PublicAPI.Unshipped.txt`, manifest (5) | unit aggregate, guard, property concentration (3) | 8 files / 1,000 |
| 5 | 5 | `ResilienceTestEvents`, `ResilienceTestAggregate`, TLPT model, `PublicAPI.Unshipped.txt`, manifest (5) | unit aggregate, guard, property (3) | 8 files / 900 |
| 6 | 6 (incident and risk) | `IICTIncidentService`, `IICTRiskService`, two default services, two read models, two projections, `PublicAPI.Unshipped.txt`, manifest (10) | unit services, unit projections, integration Marten, `Encina.IntegrationTests.csproj` reference (4) | 14 files / 1,500 |
| 7 | 6 (third party and resilience test), 7 | `IICTThirdPartyProviderService`, `IResilienceTestService`, two default services, two read models, two projections, `DORAMartenExtensions`, `ServiceCollectionExtensions` (`AddEncinaDORA`), `PublicAPI.Unshipped.txt`, manifest (12) | DI test with `ValidateOnBuild` and `ValidateScopes`, unit services (2) | 14 files / 1,400 |
| 8 | 8, 9 | monitor, health check, `DORADiagnostics`, `DORALogMessages`, reporting mapper to `IncidentReport`, `PublicAPI.Unshipped.txt`, manifest, changelog fragment, README, feature page (11) | unit monitor, health check, mapper, guard; contract `IICT*` services (4) | 15 files / 1,300 |

The split is recounted with `git ls-tree` before each brief; a PR that would exceed 15 files hands its last aggregate's projection to the next PR.

### Phase 1: Package Skeleton, EventIds & Errors (PR 1)

<details>
<summary>Tasks</summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs`: `ComplianceDORA = (52800, 52899)` registered first (ADR-021), registry row, `Encina` PublicAPI line, `AssemblyRanges` entry for `Encina.Compliance.DORA` in `EncinaEventIdAllocationTests`.
2. `src/Encina.Compliance.DORA/Encina.Compliance.DORA.csproj` (project references of Design Choice 1, `InternalsVisibleTo` for the six test assemblies, PublicAPI `AdditionalFiles`), `Encina.slnx` entry, `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`.
3. `DORAOptions` (enforcement of classification thresholds, monitor interval, `EnableDeadlineMonitor` default false) and `DORAErrors` (codes with ADR-062 categories: `IncidentNotFound` NotFound, `IncidentInvalidTransition` FailedPrecondition, `ClassificationInputIncomplete` Validation, `ProviderAlreadyRegistered` Conflict, `DeadlineMissed` FailedPrecondition, `TenantMissing` Forbidden, `EventStoreFailed` UpstreamFailure).
4. Test project references and the unit test of the options.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
You are implementing Phase 1 of Encina.Compliance.DORA (Issue #804).

CONTEXT:
- New package; mirror src/Encina.Compliance.BreachNotification/Encina.Compliance.BreachNotification.csproj and DORA EventId range 52800-52899.

TASK:
1. Register the range first; add registry row, PublicAPI line and the AssemblyRanges entry.
2. Create the csproj, slnx entry, PublicAPI files, options and errors with ErrorCategory per ADR-062.

KEY RULES:
- Nullable enabled, XML docs, zero warnings; Either<EncinaError, T>; no [Obsolete].
- Count the test ProjectReference edits in the PR.

REFERENCE FILES: src/Encina.Compliance.NIS2/NIS2Errors.cs, src/Encina.Compliance.NIS2/NIS2Options.cs
```

</details>

### Phase 2: ICT Incident Aggregate, Classification & Deadlines (PR 2)

<details>
<summary>Tasks</summary>

1. `Model/ICTIncidentClassification.cs`, `ICTIncidentStatus.cs` (`Detected`, `Classified`, `InitialReportSent`, `IntermediateReportSent`, `FinalReportSent`, `Closed`), `DORAClassificationCriteria.cs`.
2. `Services/DORAIncidentClassifier.cs` (pure) and `DORAIncidentDeadlineCalculator.cs` (pure, takes the instants; RTS 2025/301).
3. `Events/ICTIncidentEvents.cs` (`INotification` records, every event carries `TenantId`, `ModuleId`) and `Aggregates/ICTIncidentAggregate.cs` (`AggregateBase`; static `Detect(...)` returning `Either`; `Classify`, `SendInitialReport`, `SendIntermediateReport`, `SendFinalReport`, `Escalate`, `Close`, each returning `Either<EncinaError, Unit>`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
You are implementing Phase 2 of Encina.Compliance.DORA (Issue #804).

CONTEXT:
- Phase 1 merged. Pattern: src/Encina.Compliance.BreachNotification/Aggregates/BreachAggregate.cs; error style per ADR-053.

TASK:
1. Create the classification model, the pure classifier and the deadline calculator.
2. Create the events and ICTIncidentAggregate with the transition table.

KEY RULES:
- Illegal transitions return Left with a rule code, raise no event, never throw.
- Missing classification input never classifies as non-major.
- Time from passed-in instants, never DateTime.UtcNow.

REFERENCE FILES: src/Encina.Compliance.NIS2/Model/NIS2Incident.cs, docs/architecture/adr/053-aggregate-rule-violations-return-either.md
```

</details>

### Phase 3: ICT Risk Aggregate (PR 3)

<details>
<summary>Tasks</summary>

1. `Model/ICTAsset.cs`, `ICTAssetCriticality.cs`, `ICTRiskLevel.cs`, `ContinuityTestKind.cs`.
2. `Events/ICTRiskEvents.cs` and `Aggregates/ICTRiskAggregate.cs`: `Open`, `RegisterAsset`, `AssessRisk`, `RecordContinuityTest`, `AcceptRisk`, `Close`; a pure risk score (likelihood times impact bands).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
You are implementing Phase 3 of Encina.Compliance.DORA (Issue #804).

CONTEXT:
- Phases 1 and 2 merged; same aggregate conventions.

TASK:
1. Create the asset and risk models and the ICTRiskAggregate with its events.

KEY RULES:
- Events hold ids and enums only (Design Choice 4); Either on every command; tenant and module ids on every event.

REFERENCE FILES: src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs
```

</details>

### Phase 4: ICT Third-Party Provider Aggregate (PR 4)

<details>
<summary>Tasks</summary>

1. `Model/ICTProviderCriticality.cs`, `ContractualRequirement.cs` (the Art. 30 checklist), `ConcentrationLevel.cs`, `ExitStrategyStatus.cs`.
2. `Events/ICTThirdPartyProviderEvents.cs` and `Aggregates/ICTThirdPartyProviderAggregate.cs`: `Register`, `UpdateRegisterEntry` (ITS 2024/2956 fields), `RecordContractReview`, `FlagConcentration`, `DefineExitStrategy`, `Terminate`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
You are implementing Phase 4 of Encina.Compliance.DORA (Issue #804).

CONTEXT:
- Same conventions as Phases 2 and 3. The natural key (tenant plus provider LEI) is enforced by the service through a read model, not by the aggregate.

TASK:
1. Create the third-party provider models, events and aggregate.

KEY RULES:
- Tenant-qualified, length-prefixed natural key; the reserved segment 'default' only without a tenant context.

REFERENCE FILES: src/Encina.Compliance.NIS2 (#823 supplier assessment plan for the register pattern)
```

</details>

### Phase 5: Resilience Test Aggregate (PR 5)

<details>
<summary>Tasks</summary>

1. `Model/ResilienceTestKind.cs` (vulnerability assessment, scenario test, TLPT), `ResilienceFinding.cs`, `FindingSeverity.cs`.
2. `Events/ResilienceTestEvents.cs` and `Aggregates/ResilienceTestAggregate.cs`: `Plan`, `Start`, `RecordFinding`, `PlanRemediation`, `VerifyRemediation`, `Complete`; the TLPT cycle check (every three years, RTS 2025/1190) is a computed due date.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
You are implementing Phase 5 of Encina.Compliance.DORA (Issue #804).

CONTEXT:
- Same conventions as Phases 2 to 4.

TASK:
1. Create the resilience test models, events and aggregate.

KEY RULES:
- Findings never include exploit details or free text; ids and enums only.

REFERENCE FILES: src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs
```

</details>

### Phase 6: Services, Read Models & Projections (PRs 6 and 7)

<details>
<summary>Tasks</summary>

1. Interfaces `IICTIncidentService`, `IICTRiskService`, `IICTThirdPartyProviderService`, `IResilienceTestService` (commands plus overdue and list queries) and default internal implementations taking `IAggregateRepository<T>`, `TimeProvider`, `ITenantContext?`, `IModuleContext?`, `ILogger`; optional parameters last (CS1737). A missing tenant with a registered `ITenantContext` returns `Left` `Forbidden` (fail closed).
2. Read models `ICTIncidentReadModel`, `ICTRiskReadModel`, `ICTProviderRegisterReadModel`, `ResilienceTestReadModel` and inline projections, idempotent, constructor-injected.
3. `DORAMartenExtensions` (`AddDORAEventSourcing()`): one `AddAggregateRepository<T>()` and `AddProjection<,>()` per aggregate.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
You are implementing Phase 6 of Encina.Compliance.DORA (Issue #804).

CONTEXT:
- Aggregates exist (Phases 2 to 5). Pattern: src/Encina.Compliance.BreachNotification/Services/DefaultBreachNotificationService.cs and ReadModels/BreachProjection.cs.

TASK:
1. Create the four services, read models and projections, and the Marten registration extension.

KEY RULES:
- Missing tenant denies; no scoped service in a singleton; Left from a store fails the operation.
- Integration tests use Marten on PostgreSQL through Testcontainers (no InMemory store).

REFERENCE FILES: src/Encina.Marten/Projections/IReadModelRepository.cs
```

</details>

### Phase 7: Configuration, DI & Reporting Mapper (PRs 7 and 8)

<details>
<summary>Tasks</summary>

1. `ServiceCollectionExtensions.AddEncinaDORA(...)`: options, validator, services, `AddDORAEventSourcing()`; `TryAdd*` so the application's registrations win; a DI test with `ValidateOnBuild` and `ValidateScopes`.
2. `Reporting/DORAIncidentReportMapper.cs`: maps `ICTIncidentReadModel` to `IncidentReport` (Design Choice 3) when `IIncidentReportingAdapter` is registered; a `Left` from the adapter fails the report (retry), never reports success.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
You are implementing Phase 7 of Encina.Compliance.DORA (Issue #804).

CONTEXT:
- Services and projections exist; the #812 plan defines IIncidentReportingAdapter and IncidentReport.

TASK:
1. Add AddEncinaDORA with TryAdd* and the DI test.
2. Add the incident report mapper.

KEY RULES:
- Register every option type and dependency a service resolves; a store registration wins in any order.

REFERENCE FILES: src/Encina.Compliance.NIS2/ServiceCollectionExtensions.cs
```

</details>

### Phase 8: Cross-Cutting Integration, Observability & Information Sharing (PR 8)

<details>
<summary>Tasks</summary>

1. `Diagnostics/DORADiagnostics.cs` (meter and `ActivitySource` `Encina.Compliance.DORA`) and `DORALogMessages.cs` (`[LoggerMessage]` ids 52800-52849 packed from 52800; 52850-52899 stay free).
2. `Health/DORAHealthCheck.cs`: overdue incident report or overdue resilience-test remediation gives `Degraded`.
3. `DORADeadlineMonitorService` (Design Choice 6), with the optional lock of matrix row 7.
4. Art. 45 information sharing: a single `ICTThreatInformationSharingArrangement` record on the provider register read model (arrangement exists, date, anonymisation confirmed); no exchange protocol.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
You are implementing Phase 8 of Encina.Compliance.DORA (Issue #804).

CONTEXT:
- Range 52800-52899 is registered (Phase 1); BreachDeadlineMonitorService is the monitor pattern.

TASK:
1. Add diagnostics, log messages, health check and the opt-in monitor.

KEY RULES:
- No _total suffix; the tenant tag only with multi-tenancy; the monitor loop catches per iteration and logs by code.

REFERENCE FILES: src/Encina.Compliance.BreachNotification/BreachDeadlineMonitorService.cs, src/Encina.Compliance.NIS2/Diagnostics/NIS2Diagnostics.cs
```

</details>

### Phase 9: Testing & Documentation (PRs 1 to 8)

<details>
<summary>Tasks</summary>

1. Unit, guard (every public constructor and method with reference-type parameters; nullable parameters that accept null get a valid-result test), contract (`IICT*Service` and the aggregates' rule-code behaviour), property (deadline monotonicity, classifier never downgrades with more criteria met, risk score bounds), integration (Marten on PostgreSQL, rebuild from history).
2. Per-file targets (manifest justification is one sentence each): classifier and deadline calculator unit 95 and property 85; aggregates unit 90 and guard 80; default services unit 90, guard 85, contract 70; projections unit 90, integration 80; options validator unit 95, guard 90; monitor unit 85; mapper unit 90. Load and benchmark tests: `.md` justification (low-throughput compliance operations).
3. Docs: XML docs, `changelog.d/804-dora-compliance.added.md`, package README, `docs/features/dora.md` (by `docs-writer`), `docs/INVENTORY.md`; the ADR number is assigned when the ADR pull request opens.

</details>

<details>
<summary>Prompt for AI Agents — Phase 9</summary>

```text
You are implementing the tests and documentation of Encina.Compliance.DORA (Issue #804).

CONTEXT:
- The production code of the PR exists; manifest .github/coverage-manifest/Encina.Compliance.DORA.json.

TASK:
1. Add the tests per PR and report per-flag coverage against the manifest.
2. Documentation goes to the docs-writer; fragments and PublicAPI lines to the mechanical-fixer.

KEY RULES:
- Tests execute real code; Shouldly via Encina.Testing.Shouldly; shared Collection fixtures; no Thread.Sleep.

REFERENCE FILES: tests/Encina.UnitTests/Compliance, tests/Encina.GuardTests
```

</details>

---

## Research

### Regulatory References

| Source | Requirement | Applies in this plan |
| :--- | :--- | :--- |
| DORA Arts. 5-16 | ICT risk management framework | `ICTRiskAggregate` |
| DORA Arts. 17-23, RTS 2024/1772, RTS 2025/301, ITS 2025/302 | Incident classification and reporting | `ICTIncidentAggregate`, classifier, deadlines, report mapper |
| DORA Arts. 24-27, RTS 2025/1190 | Resilience and TLPT testing | `ResilienceTestAggregate` |
| DORA Arts. 28-44, ITS 2024/2956 | Third-party risk and register of information | `ICTThirdPartyProviderAggregate` |
| DORA Art. 45 | Information sharing | Arrangement record only (Phase 8 task 4) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| `BreachAggregate`, `BreachProjection`, `BreachDeadlineMonitorService` | `src/Encina.Compliance.BreachNotification` | Patterns |
| `IIncidentReportingAdapter`, `IncidentReport`, `IncidentRegulation` | #812 plan (`Encina.Compliance.BreachNotification`, `Reporting/`) | Shared reporting port (Dilemma 3) |
| `NIS2Diagnostics`, `NIS2Errors` | `src/Encina.Compliance.NIS2` | Naming and error-code style |
| `IReadModelRepository<T>` | `src/Encina.Marten/Projections/IReadModelRepository.cs` | Read models |
| `ErrorCategory` | `src/Encina/Errors/ErrorCategory.cs` (ADR-062) | Error categories |

### Event ID Allocation

| Package | Range | Notes |
|---|---|---|
| `Encina.Compliance.DORA` | `52800-52899` (`ComplianceDORA`, new, Security and compliance zone) | Uses 52800-52849; 52850-52899 free. Slot is after the claims known on 2026-11-25 (up to 52749, plus 52750-52799 for #827); the orchestrator confirms it against the registry when the PR opens |

### Estimated File Count

| Category | Files | Notes |
|---|---|---|
| Production | 52 | Models, events, aggregates, services, projections, diagnostics |
| Metadata (csproj, slnx, PublicAPI, manifest, registry, README, fragment) | 22 | Counted per PR |
| Tests | 38 | Unit, guard, contract, property, integration, `.md` justifications, project references |
| Documentation page | 1 | |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a .NET 10 / C# 14 pre-1.0 library with ROP (Either<EncinaError, T>). Issue #804 adds Encina.Compliance.DORA: four Marten event-sourced aggregates (ICT incident, ICT risk, third-party provider register, resilience test), services, projections, deadline monitor, health check.

IMPLEMENTATION OVERVIEW: eight pull requests bottom-up (skeleton and EventIds, incident, risk, third party, resilience test, services and projections, DI and reporting mapper, observability and docs).

KEY PATTERNS: aggregates return Either with rule codes (ADR-053); tenant and module ids on every event; missing tenant denies; no personal data in events; injective tenant-qualified natural keys with the reserved 'default' segment; no _total counters (ADR-054); ErrorCategory per ADR-062; no InMemory store (Marten and PostgreSQL via Testcontainers); optional constructor parameters last; opt-in monitor.

REFERENCE FILES: src/Encina.Compliance.BreachNotification; src/Encina.Compliance.NIS2; docs/plans/dsr-implementation-plan-404.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Regulatory accuracy: an incident or register read must not be stale (as the issue and the #822 plan decide) |
| 2 | OpenTelemetry | ✅ | `dora.*` instruments and activities, tenant tag with multi-tenancy (ADR-054 D4) |
| 3 | Structured Logging | ✅ | Ids 52800-52849 in the new range |
| 4 | Health Checks | ✅ | Overdue reports and remediations give `Degraded`; reporting-endpoint connectivity belongs to the adapter of #812 and #1942 |
| 5 | Validation | ✅ | Classification input and option validation return `Left` |
| 6 | Resilience | ✅ | Retry and circuit breaking live in the reporting adapter (#812, #1942); the mapper only returns the `Left` |
| 7 | Distributed Locks | ✅ | The opt-in monitor takes an optional `IDistributedLockProvider` lock per iteration when one is registered, so a multi-instance deployment runs one evaluation; without a provider it runs unguarded |
| 8 | Transactions | ❌ | Each command appends to one stream; the repository commit is the owner (the orchestrator never calls `SaveChangesAsync`) |
| 9 | Idempotency | ✅ | Report submission is keyed by incident id, phase and sequence number through `IncidentReport.ReportId` (#812) |
| 10 | Multi-Tenancy | ✅ | `TenantId` on every event and read model; missing tenant denies |
| 11 | Module Isolation | ✅ | `ModuleId` on every event and read model |
| 12 | Audit Trail | ✅ | The event stream is the immutable audit trail; reads are not audited |

---

## Prerequisites & Dependencies

- #812 (open, plan on main) provides the reporting port; #822 provides the event-sourcing conventions and the personal-data outcome (Dilemma 6 of its plan); #2668 builds the shared rule-code catalogue (open; this plan adds its codes to `DORAErrors`).
- #809 (closed epic) and #1186 (open EPIC, SPEC-002) are context.

## Next Steps

1. Maintainer answers Dilemmas 3 and 4 when work on #804 starts; both are marked "recommended, pending the maintainer".
2. Brief PR 1 after #812 PR 1 is merged; PR 2 to PR 8 follow in order.
3. Run `check-plan.ps1` without `-Draft` after the adversarial review sets the `Verified` lines.
