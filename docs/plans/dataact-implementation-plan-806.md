# Implementation Plan: `Encina.Compliance.DataAct` — EU Data Act (Regulation (EU) 2023/2854)

> **Issue**: [#806](https://github.com/dlrivada/Encina/issues/806)
> **Type**: Feature (new package)
> **Complexity**: High (three event-sourced aggregates plus an export writer family; 8 pull requests)
> **Estimated Scope**: 84 file touches and about 6,400 lines including tests and per-PR metadata; split into 8 pull requests, see "Pull request split"
> **Milestone**: v0.17.0 — Compliance Lifecycle. In the 1.0 scope since SPEC-000 DEC-009 (2026-10-23, REQ-030, which supersedes SPEC-002 DEC-015); the issue's `post-1.0` label and the issue comment "stays post-1.0" are stale
> **Provider Category**: None for databases (Marten on PostgreSQL, ADR-027; AGENTS.md section 5 excludes event sourcing from the database matrix)
> **Family**: `compliance-nis2-2`. Reuses the conventions of #822 and the export vocabulary of `Encina.Compliance.DataSubjectRights` (#404).

---

## Summary

The Data Act (applicable since 12 September 2025; switching charges abolished from 12 January 2027, Art. 29; Chapter IV for some earlier contracts from 12 September 2027, per the issue comment of 2026-09-23) gives users of connected products access to the data they generate, lets them direct it to third parties, lets public sector bodies request data in exceptional need, and lets customers switch cloud and edge providers. Encina has no Data Act code today. The only related code is GDPR portability: `IDataPortabilityExporter`, `IExportFormatWriter` and `ExportFormat` in `src/Encina.Compliance.DataSubjectRights`, which is typed to personal data locations.

**What it implements**:

- `DataSharingRequestAggregate` (Chapters II, III and V): a request by a user, a data recipient or a public sector body, its review, approval or refusal with a coded reason, the agreed trade-secret safeguards (Art. 4(6) and 5(9)), fulfilment and closure. Kind-specific rules (user access, third-party sharing, public sector) sit in a strategy (Design Choice 2).
- `DataSharingAgreementAggregate` (Chapter III): parties, scope, fair-reasonable-non-discriminatory terms record (Art. 8), compensation (Art. 9), term and termination.
- `CloudSwitchingAggregate` (Chapter VI): switching request, notice, transition, data retrieval and completion with the periods of Art. 25 as options.
- `IDataActExportWriter` with JSON and CSV writers for machine-readable exports (Art. 4(1) and 5(1) "structured, commonly used and machine-readable format").
- `ThirdCountryAccessAggregate` (Art. 32): a log of third-country government access requests and the assessment outcome (last PR).
- Services, inline projections, a deadline monitor, a health check and OpenTelemetry metrics, registered by `AddEncinaDataAct(...)`.

**Not in scope**: the personal-data case (GDPR portability stays in #404), technical interoperability specifications for data spaces, smart contracts (Art. 36), and automated transfer of the data itself (the package records and checks the decision; moving the bytes is the application's).

**Affected packages**: `Encina.Compliance.DataAct` (new) and `Encina` (EventId range registration).

**Provider category**: none. The 12-function check is in the matrix at the end.

### Corrections to the issue text (checked against `origin/main` 4b0a600c7)

| Issue text | Today's code | Plan decision |
|---|---|---|
| `AddEncinaCompliance(config => config.UseDataAct(...))` and flags `EnableDataSharingRequests` and others | No such entry point; each package has `AddEncinaDataAct(Action<DataActOptions>? configure = null)` | Per-feature options replace the four booleans only where a feature is opt-in (the monitor); the aggregates are always registered with the package (pay-for-what-you-use is the package itself) |
| "Portability export format abstractions" | `IExportFormatWriter` writes `PersonalDataLocation` lists | New `IDataActExportWriter` (Design Choice 4) |
| Alternatives: "extend DataSubjectRights" rejected | Agreed: the scope is non-personal data | A package of its own |
| Caching N/A; Idempotency, Multi-tenancy deferred | Tenancy changes the stream contract if added later | Tenancy and a caller-supplied idempotency reference are included from PR 1 |
| `Chapter VII` safeguards | Art. 32 concerns third-country government access | `ThirdCountryAccessAggregate` in PR 8 |
| Related `#809` EPIC | Closed | Context only |

---

## Design Choices

<details>
<summary><strong>1. Package placement — new package</strong></summary>

- **Class**: Settled
- **Settled by**: issue #806 (Affected Packages "new package"; Alternatives Considered 1 rejects extending DataSubjectRights).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.Compliance.DataAct`** | Non-personal data scope; opt-in | New csproj, README, manifest |
| B) Inside `Encina.Compliance.DataSubjectRights` | Reuses export writers | Mixes personal and non-personal regimes |

### Chosen Option: **A**

### Rationale

- References: `Encina`, `Encina.DomainModeling`, `Encina.Marten`; `InternalsVisibleTo` for the six test assemblies.

</details>

<details>
<summary><strong>2. Request model — one aggregate with a kind, or one aggregate per chapter</strong></summary>

- **Class**: Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `DataSharingRequestAggregate` with `DataSharingRequestKind` (`UserAccess`, `ThirdPartySharing`, `PublicSector`) and a rule strategy per kind** | One status track, one projection, one service; matches the issue's single "data sharing request aggregate" | The aggregate carries the strategy seam; public-sector rules (exceptional need, compensation) are the least similar |
| B) `DataSharingRequestAggregate` for Chapters II and III and a separate `PublicSectorDataRequestAggregate` for Chapter V | Each aggregate stays small and exact | Two projections and services for one idea; the issue lists one aggregate |

### Chosen Option: **A (recommended, pending the maintainer)**

### Rationale

- A wins for a data holder that mostly sees user and recipient requests; B wins if public-sector requests are frequent enough that their different deadlines and compensation rules dominate the model.

</details>

<details>
<summary><strong>3. Deadlines and periods — options with the regulation's values, working-day calendar</strong></summary>

- **Class**: Trivial
- **Settled by**: Data Act Art. 18 (public sector response periods) and Art. 25 (switching: maximum notice period of two months, transitional period of 30 calendar days, data retrieval period of at least 30 days); the verified application dates in the issue comment of 2026-09-23.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `DataActOptions` hold the periods with the regulation's values as defaults; working days come from `IWorkingDayCalendar` (default Monday to Friday, public holidays supplied by the application); pure `DataActDeadlineCalculator` takes instants** | Auditable, testable, amendable by configuration | The defaults must be re-checked against the Official Journal text at review (the adversarial review does it) |
| B) Constants in code | Simpler | An amendment needs a release |

### Chosen Option: **A**

### Rationale

- Time comes from `TimeProvider`; the calculator never reads the clock. The Art. 18 and Art. 25 values are verified against the regulation text at the adversarial review and the `Verified` line is set only then.

</details>

<details>
<summary><strong>4. Export writers — reuse the DSR types, or new ones</strong></summary>

- **Class**: Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `IDataActExportWriter` with its own `DataActExportFormat` (`Json`, `Csv`) and `DataActExport` result in the DataAct package** | No dependency on the personal-data package; typed to dataset records, not `PersonalDataLocation` | Two enums that look alike |
| B) Reference `Encina.Compliance.DataSubjectRights` and reuse `ExportFormat` and `ExportedData` | One vocabulary | A package reference from a non-personal regime to the GDPR rights package; `IExportFormatWriter` still cannot be reused (its input is personal data locations) |

### Chosen Option: **A (recommended, pending the maintainer)**

### Rationale

- B wins if the maintainer wants one export vocabulary across the compliance family and accepts the reference; A wins for independence of the two regimes. The writer never receives the data itself, only the descriptors and a stream the application provides.

</details>

<details>
<summary><strong>5. Personal data in events</strong></summary>

- **Class**: Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Events hold ids, enums, dates, dataset descriptors and trade-secret measure codes; party names and contacts stay in the application, referenced by id** | Nothing personal in an immutable stream | The application holds the party registry |
| B) Events hold party details, protected with the crypto-shredding of `Encina.Marten.GDPR` | Self-contained | Adds key management and the GDPR reference |

### Chosen Option: **A (recommended, pending the maintainer; follows the outcome of Dilemma 6 of the #822 plan)**

### Rationale

- A wins when parties are companies identified by registry ids; B wins when natural-person users appear by name. Whatever #822 decides applies here.

</details>

<details>
<summary><strong>6. Idempotency and natural keys</strong></summary>

- **Class**: Trivial
- **Settled by**: AGENTS.md section 6 (idempotency for operations that can receive duplicates) and the tenancy rule of section 3 (injective tenant-qualified ids; reserved `default` segment).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A caller-supplied `RequestReference` per tenant; the service looks it up through the read model and returns the existing stream id on a duplicate; the key is `<length>:<tenantId>` plus the reference, `default` reserved for no-tenant use, so no two pairs share a key** | Duplicate requests never create two streams | One read-model lookup per create |
| B) No duplicate check | Simpler | Duplicate obligations and deadlines |

### Chosen Option: **A**

### Rationale

- A real tenant id equal to `default` is rejected by the key builder.

</details>

<details>
<summary><strong>7. Observability naming</strong></summary>

- **Class**: Trivial
- **Settled by**: ADR-054 decision 4 (no `_total` suffix) and D4 (tenant tag only when multi-tenancy is on).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Meter and `ActivitySource` `Encina.Compliance.DataAct`; instruments `dataact.request.*`, `dataact.agreement.*`, `dataact.switching.*`** | Matches the other compliance packages | Three families |
| B) One flat family | Fewer names | Loses the split |

### Chosen Option: **A**

### Rationale

- Logs carry error codes and exception types, never `EncinaError.Message` and never raw tenant, user or party ids.

</details>

---

## Implementation Phases

### Pull request split (read first)

Each PR builds and passes alone. Test project edits are `ProjectReference` additions (UnitTests and GuardTests in PR 1, PropertyTests in PR 2, ContractTests in PR 5, IntegrationTests in PR 5).

| PR | Phases | Files (production, metadata) | Test files | Files / lines (approx.) |
|---|---|---|---|---|
| 1 | 1 | `EventIdRanges.cs`, registry row, `Encina` `PublicAPI.Unshipped.txt`, `Encina.slnx`, csproj, two PublicAPI files, manifest, `DataActErrors`, `DataActOptions` (10) | `AssemblyRanges` entry, UnitTests and GuardTests csproj references, unit options test (4) | 14 files / 650 |
| 2 | 2 | `DataSharingRequestKind`, `DataSharingRequestStatus`, `TradeSecretProtection`, events, aggregate, `DataActDeadlineCalculator`, `IWorkingDayCalendar`, `PublicAPI.Unshipped.txt`, manifest (10) | unit aggregate, unit calculator, property deadlines, PropertyTests csproj reference (4) | 14 files / 1,500 |
| 3 | 3 | `DataSharingAgreementEvents`, `DataSharingAgreementAggregate`, `AgreementTerms` models, `PublicAPI.Unshipped.txt`, manifest (5) | unit aggregate, guard, property (3) | 8 files / 900 |
| 4 | 4 | `CloudSwitchingEvents`, `CloudSwitchingAggregate`, models, `PublicAPI.Unshipped.txt`, manifest (5) | unit aggregate, guard, property periods (3) | 8 files / 900 |
| 5 | 5 (request) | `IDataSharingRequestService`, its default service, read model, projection, `DataActMartenExtensions`, `PublicAPI.Unshipped.txt`, manifest (7) | unit service, unit projection, guard, contract, integration Marten, ContractTests and IntegrationTests csproj references (6) | 13 files / 1,400 |
| 6 | 5 (rest), 6 | agreement and switching services, read models, projections (6), `IDataActExportWriter`, JSON and CSV writers, `PublicAPI.Unshipped.txt`, manifest (11) | unit services, unit writers (2) | 13 files / 1,400 |
| 7 | 7, 8 | `ServiceCollectionExtensions` (`AddEncinaDataAct`), options validator, `DataActDeadlineMonitorService`, health check, `DataActDiagnostics`, `DataActLogMessages`, `PublicAPI.Unshipped.txt`, manifest, changelog fragment, README, feature page (12) | DI test, unit monitor and health check (3) | 15 files / 1,100 |
| 8 | 9 | `ThirdCountryAccessEvents`, `ThirdCountryAccessAggregate`, service, projection, registration lines, `PublicAPI.Unshipped.txt`, manifest (7) | unit aggregate, unit service, guard (3) | 10 files / 900 |

### Phase 1: Package Skeleton, EventIds & Errors (PR 1)

<details>
<summary>Tasks</summary>

1. `EventIdRanges.cs`: `ComplianceDataAct = (52950, 52999)` registered first (ADR-021), registry row, `Encina` PublicAPI line, `AssemblyRanges` entry for `Encina.Compliance.DataAct`.
2. csproj (references of Design Choice 1, `InternalsVisibleTo` for six test assemblies), `Encina.slnx` entry, PublicAPI files.
3. `DataActErrors` with ADR-062 categories: `RequestNotFound` NotFound, `RequestInvalidTransition` FailedPrecondition, `RequestAlreadyExists` AlreadyExists, `AgreementInvalidTransition` FailedPrecondition, `SwitchingPeriodInvalid` Validation, `TenantMissing` Forbidden, `EventStoreFailed` UpstreamFailure.
4. `DataActOptions` (periods of Design Choice 3, monitor interval, `EnableDeadlineMonitor` default false).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
You are implementing Phase 1 of Encina.Compliance.DataAct (Issue #806).

CONTEXT:
- New package; mirror src/Encina.Compliance.NIS2/Encina.Compliance.NIS2.csproj. EventId range 52950-52999.

TASK:
1. Register the range first; create the csproj, slnx entry, PublicAPI files, errors and options.

KEY RULES:
- ErrorCategory per ADR-062; Either<EncinaError, T>; XML docs; zero warnings; no secret fields in options.

REFERENCE FILES: src/Encina.Compliance.NIS2/NIS2Errors.cs, src/Encina.Compliance.NIS2/NIS2Options.cs
```

</details>

### Phase 2: Data Sharing Request Aggregate & Deadlines (PR 2)

<details>
<summary>Tasks</summary>

1. `DataSharingRequestStatus` (`Received`, `UnderReview`, `Approved`, `Refused`, `Fulfilled`, `Closed`), `DataSharingRequestKind`, `TradeSecretProtection` (measure codes, agreed flag).
2. Events (each with `TenantId`, `ModuleId`, `RequestReference`) and `DataSharingRequestAggregate` (`AggregateBase`; static `Receive(...)` returning `Either`; `StartReview`, `Approve`, `Refuse(reasonCode)`, `AgreeTradeSecretMeasures`, `Fulfil`, `Close`).
3. `DataActDeadlineCalculator` and `IWorkingDayCalendar` with the default calendar.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
You are implementing Phase 2 of Encina.Compliance.DataAct (Issue #806).

CONTEXT:
- Phase 1 merged. Pattern: src/Encina.Compliance.BreachNotification/Aggregates/BreachAggregate.cs; errors per ADR-053 (Left with a rule code, never an exception).

TASK:
1. Create the request models, events, aggregate and the deadline calculator.

KEY RULES:
- Refusal always carries a coded reason; time from passed-in instants; no free text or party names in events.

REFERENCE FILES: src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs
```

</details>

### Phase 3: Data Sharing Agreement Aggregate (PR 3)

<details>
<summary>Tasks</summary>

1. `AgreementTerms` (scope descriptor ids, FRAND terms record by reference, compensation basis code), `DataSharingAgreementStatus`.
2. `DataSharingAgreementEvents` and `DataSharingAgreementAggregate`: `Propose`, `Accept`, `Amend`, `Suspend`, `Terminate`, `Expire`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
You are implementing Phase 3 of Encina.Compliance.DataAct (Issue #806).

CONTEXT:
- Same conventions as Phase 2.

TASK:
1. Create the agreement terms, events and aggregate.

KEY RULES:
- Terms are stored by reference id, not as free text; Either on every command.

REFERENCE FILES: src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs
```

</details>

### Phase 4: Cloud Switching Aggregate (PR 4)

<details>
<summary>Tasks</summary>

1. `CloudSwitchingStatus` (`Requested`, `NoticeGiven`, `InTransition`, `DataRetrieval`, `Completed`, `Cancelled`) and period models.
2. `CloudSwitchingEvents` and `CloudSwitchingAggregate`: `Request`, `GiveNotice`, `StartTransition`, `ExtendTransition(reason)` (only when the technical-unfeasibility reason code is given), `StartRetrieval`, `Complete`, `Cancel`; period checks use `DataActOptions`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
You are implementing Phase 4 of Encina.Compliance.DataAct (Issue #806).

CONTEXT:
- Same conventions; periods come from DataActOptions via the calculator of Phase 2.

TASK:
1. Create the switching models, events and aggregate.

KEY RULES:
- A period above the configured maximum returns Left (Validation); never clamp silently.

REFERENCE FILES: src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs
```

</details>

### Phase 5: Services, Read Models & Projections (PRs 5 and 6)

<details>
<summary>Tasks</summary>

1. `IDataSharingRequestService`, `IDataSharingAgreementService`, `ICloudSwitchingService` and internal default services taking `IAggregateRepository<T>`, `TimeProvider`, `ITenantContext?`, `IModuleContext?`, `ILogger` (optional parameters last, CS1737). A registered `ITenantContext` without a tenant returns `Left` `Forbidden` (fail closed). The request service applies Design Choice 6.
2. Read models and idempotent inline projections (constructor-injected, never `IServiceProvider`): request, agreement and switching overdue and list queries.
3. `DataActMartenExtensions` (`AddDataActEventSourcing()`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
You are implementing Phase 5 of Encina.Compliance.DataAct (Issue #806).

CONTEXT:
- Aggregates exist (Phases 2 to 4). Pattern: src/Encina.Compliance.BreachNotification/Services/DefaultBreachNotificationService.cs.

TASK:
1. Create the services, read models, projections and Marten registration.

KEY RULES:
- Missing tenant denies; a Left from a store fails the operation; integration tests use Marten on PostgreSQL via Testcontainers (no InMemory store).

REFERENCE FILES: src/Encina.Marten/Projections/IReadModelRepository.cs
```

</details>

### Phase 6: Export Writers (PR 6)

<details>
<summary>Tasks</summary>

1. `Export/IDataActExportWriter.cs` (`Format`, `WriteAsync(DataActExportDescriptor, Stream, CancellationToken)` returning `ValueTask<Either<EncinaError, DataActExport>>`), `DataActExportFormat`, `DataActExport`.
2. `JsonDataActExportWriter` and `CsvDataActExportWriter` (CSV escaping per RFC 4180, formula-injection guard for leading `=`, `+`, `-`, `@`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
You are implementing Phase 6 of Encina.Compliance.DataAct (Issue #806).

CONTEXT:
- Mirror the style of src/Encina.Compliance.DataSubjectRights/Export/JsonExportFormatWriter.cs; input is a descriptor and a caller-provided stream, not the data layer.

TASK:
1. Create the export port, models and the two writers.

KEY RULES:
- No exception for bad input; Left with Validation; stream cancellation honoured.

REFERENCE FILES: src/Encina.Compliance.DataSubjectRights/Abstractions/IExportFormatWriter.cs
```

</details>

### Phase 7: Configuration, DI, Cross-Cutting Integration & Observability (PR 7)

<details>
<summary>Tasks</summary>

1. `AddEncinaDataAct(...)`: options, validator, services, `AddDataActEventSourcing()`, `TryAdd*`; DI test with `ValidateOnBuild` and `ValidateScopes`.
2. `DataActDeadlineMonitorService` (opt-in `BackgroundService`, `PeriodicTimer` on `TimeProvider`, per-iteration catch; a missed deadline appends an escalation event; optional `IDistributedLockProvider` lock per iteration).
3. `DataActDiagnostics` (instruments of Design Choice 7), `DataActLogMessages` ids 52950-52979 packed from 52950 (52980-52999 free), health check (overdue request or switching period gives `Degraded`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
You are implementing Phase 7 of Encina.Compliance.DataAct (Issue #806).

CONTEXT:
- Services and projections exist; range 52950-52999 registered (Phase 1); BreachDeadlineMonitorService is the monitor pattern.

TASK:
1. Add registration, the DI test, the opt-in monitor, diagnostics, log messages and the health check.

KEY RULES:
- Register every option type and dependency; the monitor loop catches per iteration and logs by code; no _total suffix.

REFERENCE FILES: src/Encina.Compliance.NIS2/ServiceCollectionExtensions.cs, src/Encina.Compliance.BreachNotification/BreachDeadlineMonitorService.cs
```

</details>

### Phase 8: Testing (PRs 1 to 8)

<details>
<summary>Tasks</summary>

1. Unit, guard (public constructors and methods; nullable parameters that accept null get a valid-result test), contract (service contracts), property (deadline monotonicity, working-day calculation never lands on a weekend, switching periods never exceed the maximum), integration (Marten on PostgreSQL, rebuild from history).
2. Per-file targets: aggregates unit 90, guard 80; `DataActDeadlineCalculator` unit 95, property 85; services unit 90, guard 85, contract 70; projections unit 90, integration 80; writers unit 90, guard 85; options validator unit 95, guard 90; monitor unit 85. Load and benchmark: `.md` justification (low-throughput compliance operations).
3. CRAP of every changed method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
You are implementing the tests of Encina.Compliance.DataAct (Issue #806).

CONTEXT:
- Manifest .github/coverage-manifest/Encina.Compliance.DataAct.json; production code of the PR exists.

TASK:
1. Add the tests per PR and report per-flag coverage against the manifest.

KEY RULES:
- Tests execute real code; Shouldly via Encina.Testing.Shouldly; FakeTimeProvider for time; no Thread.Sleep.

REFERENCE FILES: tests/Encina.UnitTests/Compliance, tests/Encina.GuardTests
```

</details>

### Phase 9: Third-Country Access Log & Documentation (PR 8)

<details>
<summary>Tasks</summary>

1. `ThirdCountryAccessEvents` (`Received`, `Assessed(outcome code)`, `Challenged`, `Complied`, `Rejected`) and `ThirdCountryAccessAggregate`, a service and read model; entries record the requesting country code and legal-basis code, never the request text (Art. 32).
2. Docs: XML docs, `changelog.d/806-dataact-compliance.added.md`, package README, `docs/features/dataact.md` (by `docs-writer`), `docs/INVENTORY.md`; the ADR number is assigned when the ADR pull request opens.

</details>

<details>
<summary>Prompt for AI Agents — Phase 9</summary>

```text
You are implementing Phase 9 of Encina.Compliance.DataAct (Issue #806).

CONTEXT:
- Same aggregate and service conventions as Phases 2 and 5; registration lines are added to AddDataActEventSourcing.

TASK:
1. Create the third-country access aggregate, service and read model; documentation goes to the docs-writer.

KEY RULES:
- Store codes only (country, legal basis, outcome); missing tenant denies.

REFERENCE FILES: src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs
```

</details>

---

## Research

### Regulatory References

| Source | Requirement | Applies in this plan |
| :--- | :--- | :--- |
| Data Act Chapter II (Arts. 3-7) | User access to connected-product data, sharing at the user's request | `DataSharingRequestKind.UserAccess` and `ThirdPartySharing` |
| Chapter III (Arts. 8-12) | Terms for sharing between businesses, compensation, trade secrets | `DataSharingAgreementAggregate`, `TradeSecretProtection` |
| Chapter V (Arts. 14-22) | Business to government in exceptional need | `DataSharingRequestKind.PublicSector` |
| Chapter VI (Arts. 23-31) | Switching between data processing services, Art. 29 charges | `CloudSwitchingAggregate` |
| Art. 32 | Third-country governmental access to non-personal data | `ThirdCountryAccessAggregate` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| `BreachAggregate`, `BreachProjection`, `BreachDeadlineMonitorService` | `src/Encina.Compliance.BreachNotification` | Patterns |
| `IExportFormatWriter`, `JsonExportFormatWriter` | `src/Encina.Compliance.DataSubjectRights` | Style reference for the writers |
| `IReadModelRepository<T>` | `src/Encina.Marten/Projections/IReadModelRepository.cs` | Read models |
| `NIS2Diagnostics`, `NIS2Errors` | `src/Encina.Compliance.NIS2` | Style |
| `ErrorCategory` | `src/Encina/Errors/ErrorCategory.cs` (ADR-062) | Error categories |

### Event ID Allocation

| Package | Range | Notes |
|---|---|---|
| `Encina.Compliance.DataAct` | `52950-52999` (`ComplianceDataAct`, new, Security and compliance zone) | Phase 7 uses 52950-52979; Phase 9 adds three ids inside it (52980-52982) if needed; 52983-52999 free. After the claims known on 2026-11-25 (up to 52749, 52750-52799 for #827, 52800-52899 for #804, 52900-52949 for #805); the orchestrator confirms against the registry when the PR opens |

### Estimated File Count

| Category | Files | Notes |
|---|---|---|
| Production | 38 | Models, events, aggregates, services, projections, writers |
| Metadata (csproj, slnx, PublicAPI, manifest, registry, README, fragment) | 21 | Counted per PR |
| Tests | 24 | Unit, guard, contract, property, integration, `.md` justifications, project references |
| Documentation page | 1 | |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a .NET 10 / C# 14 pre-1.0 library with ROP. Issue #806 adds Encina.Compliance.DataAct: Marten event-sourced aggregates for data sharing requests, agreements, cloud switching and third-country access, plus JSON and CSV export writers.

IMPLEMENTATION OVERVIEW: eight pull requests (skeleton, request aggregate, agreement, switching, request service, other services and writers, DI and observability, third-country log).

KEY PATTERNS: aggregates return Either with rule codes (ADR-053); tenant and module ids on every event; missing tenant denies; no personal data or free text in events; injective tenant-qualified request references with the reserved 'default' segment; no _total counters (ADR-054); ErrorCategory per ADR-062; no InMemory store; optional constructor parameters last; opt-in monitor.

REFERENCE FILES: src/Encina.Compliance.BreachNotification; src/Encina.Compliance.NIS2; src/Encina.Compliance.DataSubjectRights/Export; docs/plans/dsr-implementation-plan-404.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Requests and deadlines must be read from the stream or the inline projection; a cache could hide an overdue obligation |
| 2 | OpenTelemetry | ✅ | `dataact.*` instruments and activities, tenant tag with multi-tenancy (ADR-054 D4) |
| 3 | Structured Logging | ✅ | Ids 52950-52979 |
| 4 | Health Checks | ✅ | Overdue requests and switching periods give `Degraded` |
| 5 | Validation | ✅ | Request, period and options validation return `Left` |
| 6 | Resilience | ❌ | No external call in the package (export writers receive a caller stream) |
| 7 | Distributed Locks | ✅ | The opt-in monitor takes an optional `IDistributedLockProvider` lock per iteration when one is registered |
| 8 | Transactions | ❌ | Each command appends to one stream; the repository commit is the owner |
| 9 | Idempotency | ✅ | Caller-supplied `RequestReference` per tenant (Design Choice 6) |
| 10 | Multi-Tenancy | ✅ | `TenantId` on every event and read model; missing tenant denies |
| 11 | Module Isolation | ✅ | `ModuleId` on every event and read model |
| 12 | Audit Trail | ✅ | The event stream is the immutable audit trail of every sharing decision |

---

## Prerequisites & Dependencies

- #822 (open, plan on main): event-sourcing conventions and the personal-data outcome (Dilemma 6 of its plan). #2668 (open) builds the shared rule-code catalogue; this plan adds its codes to `DataActErrors`.
- Related context: #404, #405 and #407 (closed), #1186 (open EPIC, SPEC-002); the structured export of SPEC-002 REQ-004 and S23 is the base the issue comment names.

## Next Steps

1. Maintainer answers Dilemmas 2, 4 and 5 when work on #806 starts; each is marked "recommended, pending the maintainer".
2. Brief PR 1 after #822 PR 1 has merged; PR 2 to PR 8 follow in order.
3. Run `check-plan.ps1` without `-Draft` after the adversarial review sets the `Verified` lines (including the check of the Art. 18 and Art. 25 values).
