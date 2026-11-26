# Implementation Plan: `Encina.Compliance.DORA` — Digital Operational Resilience Act (Regulation (EU) 2022/2554)

> **Issue**: [#804](https://github.com/dlrivada/Encina/issues/804)
> **Type**: Feature (new package)
> **Complexity**: Very High (four event-sourced aggregates, classification and deadline rules, shared incident reporting; 11 pull requests)
> **Estimated Scope**: 142 file touches (the per-PR table sum; a shared file such as `PublicAPI.Unshipped.txt` counts in every PR that edits it) and about 12,250 lines including tests and per-PR metadata; split into 11 pull requests (each at most 15 files and 1,500 lines, with explicit file lists), see "Pull request split"
> **Milestone**: v0.17.0 — Compliance Lifecycle. In the 1.0 scope since SPEC-000 DEC-009 (2026-10-23, REQ-030, which supersedes SPEC-002 DEC-015); the issue's `post-1.0` label and the issue comment "stays post-1.0" are stale
> **Provider Category**: None for databases (Marten on PostgreSQL, ADR-027; AGENTS.md section 5 excludes event sourcing from the database matrix)
> **Family**: `compliance-nis2-2` (regulatory compliance packages after NIS2). Shares the incident reporting port of the #812 plan and the event-sourcing conventions of #822.

---

## Summary

DORA is lex specialis for financial entities and their ICT third-party providers. Encina has no DORA code today (`git ls-tree origin/main src` shows no `Encina.Compliance.DORA`); the closest assets are `Encina.Compliance.NIS2` (stateless rules plus the event-sourced modules of #822 to #826), `Encina.Compliance.BreachNotification` (the incident record and, through #812, the reporting port `IIncidentReportingAdapter`) and the Marten event-sourcing pattern of `BreachAggregate`.

**What it implements** (Regulation (EU) 2022/2554, with the Level 2 acts the issue comment of 2026-09-23 verified: RTS 2024/1772 classification, RTS 2024/1774 ICT risk framework, RTS 2025/301 reporting times, ITS 2025/302 templates, ITS 2024/2956 register of information, RTS 2025/1190 TLPT):

- `ICTIncidentAggregate` (Arts. 17-23): classification (major or not, by the RTS 2024/1772 criteria), the three reports (initial as early as possible, within 4 hours of classification as major and at the latest 24 hours after awareness, except that an incident classified later than 24 hours after awareness is due 4 hours after classification; intermediate within 72 hours of the initial one; final within one month of the latest intermediate; Delegated Regulation (EU) 2025/301 Art. 5, verified against the Official Journal text in the legal-facts record of 2026-11-26; the calculator also takes the weekend and bank-holiday rule of Art. 5 as an option: a deadline falling on a weekend or bank holiday of the entity's Member State may be met by noon of the next working day, except for initial notifications and intermediate reports of credit institutions, central counterparties, trading venues and NIS2 essential or important entities, and competent authorities may disapply it) and submission through the shared reporting port.
- `ICTRiskAggregate` (Arts. 5-16): ICT asset inventory entries with criticality, risk assessments and continuity or recovery test records.
- `ICTThirdPartyProviderAggregate` (Arts. 28-44): the register of information per provider, contractual-requirement checklist, concentration flag and exit strategy.
- `ResilienceTestAggregate` (Arts. 24-27): test plan, execution, findings and remediation, including TLPT (RTS 2025/1190).
- Services, inline Marten projections, a deadline monitor, a health check and OpenTelemetry metrics, registered by `AddEncinaDORA(...)`.

**Not in scope**: transmission to a competent authority (the shared port and the concrete adapter of #812 and #1942), threat-intelligence sharing between entities (Art. 45: only the arrangement record of Phase 4, with its event, no exchange protocol), and anything for the other regimes (#805, #806, #807 have their own plans).

**Affected packages**: `Encina.Compliance.DORA` (new), `Encina` (EventId range registration only), `Encina.Compliance.BreachNotification` (referenced for the port; the `IncidentRegulation.DORA` member exists in the #812 plan, so no change here).

**Provider category**: none of the database, caching, transport, lock or validation categories. The 12-function check is in the matrix at the end.

### Corrections to the issue text (checked against `origin/main` 19b761ffa)

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

- Rule violations return `Left` with a rule code, never `InvalidOperationException` (ADR-053; #2668 builds the shared catalogue). The natural key of the provider register is Design Choice 8.

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

<details>
<summary><strong>8. Provider register natural key and duplicate category</strong></summary>

- **Class**: Trivial
- **Settled by**: ADR-062 R2 (duplicates and "exists" are `AlreadyExists`) and the injective key rule of the #822 plan (length-prefixed tenant segment, the reserved segment `default` used only without a tenant context).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Key = tenant-qualified provider LEI, checked by the service against the provider register read model; a duplicate returns `ProviderAlreadyRegistered` with category `AlreadyExists`** | No two tenant and LEI pairs share a key; one register entry per provider per tenant | The check is read-model based, so two simultaneous registrations race: the stream id derived from the key makes the second append fail with a version mismatch (`Conflict`, R2) |
| B) Key without the tenant segment | Shorter key | Two tenants registering the same provider collide, which breaks tenant isolation |

### Chosen Option: **A**

### Rationale

- Option B contradicts the multi-tenancy row of the matrix; ADR-062 R2 fixes the categories of both outcomes (`AlreadyExists` for the duplicate, `Conflict` for the version mismatch).

</details>

<details>
<summary><strong>9. Where the Art. 45 information-sharing arrangement lives</strong></summary>

- **Class**: Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A small entity-level stream, `InformationSharingArrangementAggregate`, with one stream per tenant (stream id derived from the injective tenant key of Design Choice 8, reserved segment `default` without a tenant)** | Art. 45 is an arrangement of the financial entity with other financial entities, not a fact about one ICT provider; one record per entity, no duplication; no read model needed (the service loads the aggregate by its derived id) | A fifth aggregate: two more files in PR 4 and one more `AddAggregateRepository<T>()` in PR 8 |
| B) Inside `ICTThirdPartyProviderAggregate` | No extra aggregate | The stream is keyed by tenant plus provider LEI, so the entity-level arrangement would repeat per provider or sit on an arbitrary one |
| C) Inside `ICTRiskAggregate` | No extra aggregate | The stream is per risk assessment, the same repetition problem |

### Chosen Option: **A (recommended, pending the maintainer)**

### Rationale

- A wins on correctness of the model; B or C win only if the maintainer prefers four aggregates over one record per entity. The PR split below assumes A; choosing B or C removes two files from PR 4 and no other PR changes. Design Choice 2 does not depend on this answer.

</details>

---

## Implementation Phases

### Pull request split (read first)

The package is built bottom-up; each PR builds and passes alone. Test project edits are `ProjectReference` additions in `Encina.UnitTests` and `Encina.GuardTests` (PR 1), `Encina.PropertyTests` (PR 2), `Encina.IntegrationTests` (PR 6) and `Encina.ContractTests` (PR 10); each is counted in its PR below. Every file the plan names is assigned to exactly one PR (the validator and its tests to PR 10, the guard tests of each service to its service PR, the four log-message files to the PR whose code logs). No PR exceeds 15 files or 1,500 lines.

| PR | Phases | Files (production, metadata), explicit | Test files, explicit | Files / lines (approx.) |
|---|---|---|---|---|
| 1 | 1 | `src/Encina/Diagnostics/EventIdRanges.cs`, `docs/engineering/eventid-block-registry.md` row, `Encina` `PublicAPI.Unshipped.txt`, `Encina.slnx`, `Encina.Compliance.DORA.csproj`, `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, manifest `Encina.Compliance.DORA.json`, `DORAOptions` (with the TLPT, weekend-rule and `RequireTenantContext` options), `DORAErrors` (10) | `AssemblyRanges` entry in `EncinaEventIdAllocationTests`, `Encina.UnitTests.csproj` and `Encina.GuardTests.csproj` references, unit options test (4) | 14 files / 750 |
| 2 | 2 | `ICTIncidentClassification`, `ICTIncidentStatus`, `DORAClassificationCriteria`, `DORAIncidentClassifier`, `DORAIncidentDeadlineCalculator`, `ICTIncidentEvents`, `ICTIncidentAggregate`, `PublicAPI.Unshipped.txt`, manifest (9) | unit classifier, unit deadlines, unit aggregate, one guard file (classifier, calculator, aggregate), property deadlines, `Encina.PropertyTests.csproj` reference (6) | 15 files / 1,400 |
| 3 | 3 | `ICTAsset`, `ICTAssetCriticality`, `ICTRiskLevel`, `ContinuityTestKind`, `ICTRiskEvents`, `ICTRiskAggregate`, `PublicAPI.Unshipped.txt`, manifest (8) | unit aggregate, guard, property score (3) | 11 files / 1,000 |
| 4 | 4 | `ICTProviderCriticality`, `ContractualRequirement`, `ConcentrationLevel`, `ExitStrategyStatus`, `ICTThreatInformationSharingArrangement`, `ICTThirdPartyProviderEvents`, `ICTThirdPartyProviderAggregate`, `InformationSharingArrangementEvents`, `InformationSharingArrangementAggregate` (Dilemma 9, option A), `PublicAPI.Unshipped.txt`, manifest (11) | unit provider aggregate, unit arrangement aggregate, guard (one file, both aggregates), property concentration (4) | 15 files / 1,300 |
| 5 | 5 | `ResilienceTestKind`, `ResilienceFinding`, `FindingSeverity`, `ResilienceTestEvents`, `ResilienceTestAggregate`, `PublicAPI.Unshipped.txt`, manifest (7) | unit aggregate, guard, property (3) | 10 files / 900 |
| 6 | 6 (incident) | `IICTIncidentService`, `DefaultICTIncidentService`, `ICTIncidentReadModel`, `ICTIncidentProjection`, `DORAScopeResolver` (internal, tenant and module, Phase 6 task 1), `DORAMartenExtensions` (new, registers the incident aggregate and projection), `DORAIncidentLogMessages`, `PublicAPI.Unshipped.txt`, manifest (9) | unit service, unit projection, guard service, integration Marten, `Encina.IntegrationTests.csproj` reference (5) | 14 files / 1,300 |
| 7 | 6 (risk) | `IICTRiskService`, `DefaultICTRiskService`, `ICTRiskReadModel`, `ICTRiskProjection`, `DORAMartenExtensions` (edit), `DORARiskLogMessages`, `PublicAPI.Unshipped.txt`, manifest (8) | unit service, unit projection, guard service, integration Marten (4) | 12 files / 1,000 |
| 8 | 6 (third party) | `IICTThirdPartyProviderService`, `DefaultICTThirdPartyProviderService`, `ICTProviderRegisterReadModel`, `ICTProviderRegisterProjection`, `DORAMartenExtensions` (edit: also `AddAggregateRepository` for the arrangement aggregate; the service reads it by its derived stream id, no read model), `DORAProviderLogMessages`, `PublicAPI.Unshipped.txt`, manifest (8) | unit service, unit projection, guard service, integration Marten (4) | 12 files / 1,100 |
| 9 | 6 (resilience test) | `IResilienceTestService`, `DefaultResilienceTestService`, `ResilienceTestReadModel`, `ResilienceTestProjection`, `DORAMartenExtensions` (edit), `DORAResilienceLogMessages`, `PublicAPI.Unshipped.txt`, manifest (8) | unit service, unit projection, guard service, integration Marten (4) | 12 files / 1,000 |
| 10 | 7 | `ServiceCollectionExtensions` (`AddEncinaDORA`), `DORAOptionsValidator`, `DORAIncidentReportMapper`, `PublicAPI.Unshipped.txt`, manifest (5) | DI test with `ValidateOnBuild` and `ValidateScopes`, unit validator, unit mapper, one guard file (validator, registration, mapper, `DORAMartenExtensions`), contract `IICT*` services (one file), `Encina.ContractTests.csproj` reference, load and benchmark `.md` justifications (2) (8) | 13 files / 1,200 |
| 11 | 8, 9 | `DORADiagnostics`, `DORAHealthCheck`, `DORADeadlineMonitorService`, `DORAMonitorLogMessages`, `ServiceCollectionExtensions` (edit: monitor and health check registration), `PublicAPI.Unshipped.txt`, manifest, `changelog.d/804-dora-compliance.added.md`, package README, `docs/features/dora.md`, `docs/INVENTORY.md` (11) | unit monitor, unit health check, guard (diagnostics, health check, monitor) (3) | 14 files / 1,300 |

The split is recounted with `git ls-tree` before each brief; a PR that would exceed 15 files or 1,500 lines hands its last aggregate's projection to the next PR. Services of PRs 6 to 9 log through the `[LoggerMessage]` ids of their own partial `DORALogMessages` file; ids are packed from 52800 in merge order with no gaps and never pass 52849 (52850-52899 stay free).

### Phase 1: Package Skeleton, EventIds & Errors (PR 1)

<details>
<summary>Tasks</summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs`: `ComplianceDORA = (52800, 52899)` registered first (ADR-021), registry row, `Encina` PublicAPI line, `AssemblyRanges` entry for `Encina.Compliance.DORA` in `EncinaEventIdAllocationTests`.
2. `src/Encina.Compliance.DORA/Encina.Compliance.DORA.csproj` (project references of Design Choice 1, `InternalsVisibleTo` for the six test assemblies, PublicAPI `AdditionalFiles`), `Encina.slnx` entry, `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`.
3. `DORAOptions` (enforcement of classification thresholds, monitor interval, `EnableDeadlineMonitor` default false; `RequireTenantContext` default true, Phase 6 task 1; `TlptApplicable` default false and `TlptCycleMonths` default 36 for Art. 26(1), overridable per entity; the weekend and bank-holiday rule of RTS 2025/301 Art. 5: `ApplyWeekendRule` default false so deadlines stay strict until the entity opts in, `EntityKind` (credit institution, central counterparty, trading venue, NIS2 essential or important entity, other) to exclude initial and intermediate reports, and `BankHolidays` (a set of `DateOnly` for the entity's Member State, supplied by the application; no calendar abstraction); the authority's disapplication is `ApplyWeekendRule = false`) and `DORAErrors` (codes with ADR-062 categories: `IncidentNotFound` NotFound (R1, an incident addressed by the id the caller supplies), `IncidentInvalidTransition` Conflict (R2, an invalid transition; `FailedPrecondition` is not used before #3420), `ClassificationInputIncomplete` Validation (R3, a field rule), `ProviderAlreadyRegistered` AlreadyExists (R2, Design Choice 8), `TenantMissing` InvalidRequest (R3, a missing tenant; the gate still denies), `TenantMismatch` Forbidden (R6, an explicit tenant that differs from the ambient one), `EventStoreFailed` Internal (R4, a store failure). A missed deadline is not an error code: a late report is still accepted (reporting late beats not reporting), so the deadline breach is state, recorded as an escalation event by the monitor (Design Choice 6) and shown by the health check; there is no `DeadlineMissed` error. The version mismatch of two concurrent appends to one stream is `Conflict` (R2).
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
2. `Services/DORAIncidentClassifier.cs` (pure) and `DORAIncidentDeadlineCalculator.cs` (pure, takes the instants, the `ApplyWeekendRule`, `EntityKind` and `BankHolidays` values of PR 1; RTS 2025/301). `Escalate` carries a `MissedDeadlineReason` code (an enum, no free text) so the entity can tell the authority why a deadline was missed (duty cited as RTS 2025/301 Art. 5; re-read at the PR 2 brief); the late report is still accepted.
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

1. `Model/ICTProviderCriticality.cs`, `ContractualRequirement.cs` (the Art. 30 checklist), `ConcentrationLevel.cs`, `ExitStrategyStatus.cs`, `ICTThreatInformationSharingArrangement.cs`.
2. `Events/ICTThirdPartyProviderEvents.cs` and `Aggregates/ICTThirdPartyProviderAggregate.cs`: `Register`, `UpdateRegisterEntry` (ITS 2024/2956 fields), `RecordContractReview`, `FlagConcentration`, `DefineExitStrategy`, and the Art. 45 record ( the single `ICTThreatInformationSharingArrangement` record, arrangement exists, date, anonymisation confirmed; no exchange protocol; per Dilemma 9 option A this command lives on the new `InformationSharingArrangementAggregate` (`Record`, event `InformationSharingArrangementRecorded`, one stream per tenant), not on the provider aggregate), `Terminate`.

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
2. `Events/ResilienceTestEvents.cs` and `Aggregates/ResilienceTestAggregate.cs`: `Plan`, `Start`, `RecordFinding`, `PlanRemediation`, `VerifyRemediation`, `Complete`; the TLPT cycle check (at least every three years, DORA Art. 26(1), not RTS 2025/1190, which sets the test method; the competent authority can raise or lower the frequency per entity, and only entities it identifies under Art. 26(8) perform TLPT) is a computed due date: the period is an option defaulting to three years, overridable per entity, and a `TlptApplicable` flag on the entity's configuration (default false) decides whether a TLPT due date is computed at all.

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

### Phase 6: Services, Read Models & Projections (PRs 6 to 9, one per aggregate)

<details>
<summary>Tasks</summary>

1. Interfaces `IICTIncidentService`, `IICTRiskService`, `IICTThirdPartyProviderService`, `IResilienceTestService` (commands plus overdue and list queries) and default internal implementations taking `IAggregateRepository<T>`, `TimeProvider`, `IRequestContextAccessor`, `ILogger`, then the optional `MultiTenancyMarker? multiTenancy = null` and `IModuleExecutionContext? moduleContext = null` (optional parameters last, CS1737). `ITenantContext` and `IModuleContext` do not exist in `src` and are not used. The tenant comes from the ambient `IRequestContext.TenantId` (`src/Encina/Abstractions/IRequestContext.cs:125`) read through `IRequestContextAccessor.RequestContext` (`src/Encina/Abstractions/IRequestContextAccessor.cs:70`), the pattern of `DefaultConsentService.TryResolveWriteTenantScope` (`DefaultConsentService.cs:605-632`); the module id comes from `IModuleExecutionContext.CurrentModule` (`src/Encina/Modules/Isolation/IModuleExecutionContext.cs:80`). The Consent pattern fails closed only under a condition: `_options.RequireTenantContext ?? _isMultiTenantApplication` (`DefaultConsentService.cs:128`, true when `MultiTenancyMarker` is registered or the option is set), so an application with neither gets no denial. This package does not copy that default: `DORAOptions.RequireTenantContext` (`bool`, default `true`) makes a missing tenant deny unless the application sets it to `false` explicitly, and setting it to `false` while a `MultiTenancyMarker` is registered is logged by code (SPEC-002 DEC-006). The internal `DORAScopeResolver` (PR 6) holds that rule once: when `RequireTenantContext` is true and no tenant is present the command returns `Left` `TenantMissing` (`InvalidRequest`, ADR-062 R3; fail closed), and an explicit tenant that differs from the ambient one is `Left` `Forbidden` (R6; deliberately not Consent's `TenantRequired`, which ADR-062 R6 replaces for mismatches). No `ProjectReference` is added: `Encina.Compliance.DORA` already references `Encina`, and the typed resolver of #1257 (`ITenantScopeResolver`) replaces the helper when that plan merges, with no change to the services' public surface.
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

### Phase 7: Configuration, DI & Reporting Mapper (PR 10)

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

### Phase 8: Cross-Cutting Integration & Observability (PR 11)

<details>
<summary>Tasks</summary>

1. `Diagnostics/DORADiagnostics.cs` (meter and `ActivitySource` `Encina.Compliance.DORA`) and `DORAMonitorLogMessages.cs` (a partial of `DORALogMessages`; the service files of PRs 6 to 9 add theirs; `[LoggerMessage]` ids packed from 52800 in merge order, no gaps, ceiling 52849; 52850-52899 stay free).
2. `Health/DORAHealthCheck.cs`: overdue incident report or overdue resilience-test remediation gives `Degraded`.
3. `DORADeadlineMonitorService` (Design Choice 6), with the optional lock of matrix row 7.
4. Art. 45 information sharing needs nothing here: its record, command and event are in Phase 4 (PR 4) and its service access and repository registration in PR 8. PRs 6 to 9 prove their service only with a hand-built test provider because `AddEncinaDORA` lands in PR 10; the PR 6 brief says so.

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

### Phase 9: Testing & Documentation (PRs 1 to 11)

<details>
<summary>Tasks</summary>

1. Unit, guard (every public constructor and method with reference-type parameters; nullable parameters that accept null get a valid-result test), contract (the four `IICT*Service` interfaces, one file in PR 10; the aggregates' rule-code behaviour is covered by their unit tests), property (deadline monotonicity, classifier never downgrades with more criteria met, risk score bounds), integration (Marten on PostgreSQL, rebuild from history).
2. Per-file targets (numeric targets, not measurements: no baseline exists because the package is new, and the measured figures go in the PR, labelled "measured on <date> with <command>"; each manifest justification is one sentence): classifier and deadline calculator unit 95 and property 85; aggregates unit 90 and guard 80; default services unit 90, guard 85, contract 70; projections unit 90, integration 80; options validator unit 95, guard 90; monitor unit 85; mapper unit 90. Load and benchmark tests: `.md` justification (low-throughput compliance operations).
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
| DORA Art. 45 | Information sharing | Arrangement record only (Phase 4, event and command; Dilemma 9) |

The article numbers, periods and Level 2 act numbers above and in the phases (4 h, 24 h, 72 h, one month, the three-year TLPT cycle of Art. 26(1), the chapter ranges, RTS and ITS numbers) are verified against the Official Journal text (mirror) in the batch's legal-facts record of 2026-11-26. Not read there and therefore UNVERIFIED: the recurring-incident rule of RTS 2024/1772 (the plan does not model it; a later issue may), and the proportionality regime (simplified ICT risk framework of RTS 2024/1774 and Art. 16, microenterprises), which the plan does not model either. The brief of each PR re-reads the values against EUR-Lex before the constants are coded.

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
| Production | 54 | Models, events, aggregates, services, projections, diagnostics (distinct files; the PR table above counts a shared file in each PR that edits it) |
| Metadata (csproj, slnx, PublicAPI, manifest, registry, README, fragment) | 22 | Counted per PR |
| Tests | 38 | Unit, guard, contract, property, integration, `.md` justifications, project references |
| Documentation page | 1 | |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a .NET 10 / C# 14 pre-1.0 library with ROP (Either<EncinaError, T>). Issue #804 adds Encina.Compliance.DORA: four Marten event-sourced aggregates (ICT incident, ICT risk, third-party provider register, resilience test), services, projections, deadline monitor, health check.

IMPLEMENTATION OVERVIEW: eleven pull requests bottom-up (skeleton and EventIds, four aggregates, four service and projection PRs, DI and reporting mapper, observability and docs).

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
| 10 | Multi-Tenancy | ✅ | `TenantId` on every event and read model, read from `IRequestContext.TenantId` through `IRequestContextAccessor`; missing tenant denies while `DORAOptions.RequireTenantContext` is true, its default (`InvalidRequest`, ADR-062 R3) |
| 11 | Module Isolation | ✅ | `ModuleId` on every event and read model, from `IModuleExecutionContext.CurrentModule` |
| 12 | Audit Trail | ✅ | The event stream is the immutable audit trail; reads are not audited |

---

## Prerequisites & Dependencies

- #812 (open, plan on main) provides the reporting port; #822 provides the event-sourcing conventions and the personal-data outcome (Dilemma 6 of its plan); #2668 builds the shared rule-code catalogue (open; this plan adds its codes to `DORAErrors`).
- #809 (closed epic) and #1186 (open EPIC, SPEC-002) are context.

## Next Steps

1. Maintainer answers Dilemmas 3, 4 and 9 when work on #804 starts; all are marked "recommended, pending the maintainer".
2. Brief PR 1 after #812 PR 1 is merged; PR 2 to PR 11 follow in order.
3. Run `check-plan.ps1` without `-Draft` after the adversarial review sets the `Verified` lines.
