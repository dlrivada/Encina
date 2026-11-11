# Implementation Plan: `Encina.Compliance.EHDS` — European Health Data Space, Regulation (EU) 2025/327

> **Issue**: [#808](https://github.com/dlrivada/Encina/issues/808)
> **Type**: Feature
> **Milestone**: v0.17.0 — Compliance Lifecycle (the issue still carries the label `post-1.0`; see Corrections)
> **Complexity**: Very High (7 phases, new package, Marten only, ~35 production files; split into five worker pull requests)
> **Estimated Scope**: ~3,500-4,800 lines of production code + ~5,000-6,500 lines of tests

---

## Summary

Create `Encina.Compliance.EHDS`, the Encina compliance module for the **European Health Data Space** (Regulation (EU) 2025/327). This plan scopes the module to what the Regulation text and SPEC-002 already fix, and defers what depends on **implementing acts that are not yet adopted** (due by 26 March 2027 per SPEC-002). The first release contains: a registry of data categories and secondary-use purposes; an event-sourced **health data access permit** lifecycle for the secondary-use side (application, grant, denial, suspension, revocation, expiry) with a secure-processing-environment reference; a `[EhdsSecondaryUse]` pipeline behavior that blocks a request that has no valid permit (fail closed); an expiry monitor; health check and observability; and an **evidential access-log adapter** that exposes the read audit as the EHDS logging component, built on the extension points decided in #1245. Interoperability formats (EEHRxF, HL7 FHIR, SNOMED CT, ICD), MyHealth@EU connectivity, data-quality labelling and fee structures are deferred to a follow-up issue with the reason recorded.

### Standards covered

- Regulation (EU) 2025/327 (EHDS): OJ 5 March 2025, in force 25 March 2025, general application 26 March 2027; priority categories (a)-(c) and the EHR systems that process them from 26 March 2029, (d)-(f) from 26 March 2031; secondary use from 26 March 2029 (parts 2027, 2031, 2035) (SPEC-002 §3.3 row EHDS, dates verified [V] there against EUR-Lex and the Commission FAQ). Art. 9 (access information kept at least three years) and the in-house EHR "put into service" reading are marked [S] in SPEC-002, so **this plan treats them as unverified**; Art. 50 (micro-enterprise exemption from secondary-use data-holder duties) is quoted from SPEC-002 and issue comment, also not re-verified here. Chapter and article numbers other than those three are deliberately not cited: the issue's "Chapters II-III" and "Chapter IV" are unverified and the module's XML docs are written against the Regulation text when slice 1 starts.
- GDPR Art. 9: health data are special-category data; the module builds on `Encina.Compliance.GDPR`, `Encina.Compliance.Anonymization` (#407, closed) and `Encina.Compliance.DataResidency` (#405, closed).
- SPEC-002 REQ-052 and #1245 (P-43): EHDS logging and export are 1.0 extension points; implementing acts due by 26 March 2027. SPEC-002 INV-003: no default follows law that is not yet applicable.
- SPEC-000 DEC-009 and REQ-030: #808 is in the 1.0 scope.

### What already exists

- No `Encina.Compliance.EHDS` project, no EHDS type and no health-data permit concept exist in `src/` (search confirms). Fifteen compliance packages exist under `src/Encina.Compliance.*`; the event-sourced ones (BreachNotification, DPIA, LawfulBasis and others) register aggregates with `AddBreachNotificationAggregates`, `AddDPIAAggregates` (`*MartenExtensions.cs`) and hold no InMemory store (AGENTS.md §3).
- The aggregate pattern is `AggregateBase` (for example `src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs`) with events that implement `INotification` (`Events/*.cs`), a projection and a read model (`ReadModels/`), and a service over `IAggregateRepository`.
- The pipeline behavior pattern with enforcement modes and an attribute is `src/Encina.Compliance.LawfulBasis/Pipeline/LawfulBasisValidationPipelineBehavior.cs` (`LawfulBasisEnforcementMode`: Block, Warn, Disabled) and `src/Encina.Compliance.DPIA/DPIARequiredPipelineBehavior`.
- The read audit is `IReadAuditStore` in `src/Encina.Security.Audit/` (`LogReadAsync`, `GetAccessHistoryAsync`, `GetUserAccessHistoryAsync`, `QueryAsync`, `PurgeEntriesAsync`), ADR-036 (three audit stores). #1245 decides which extension points the read audit exposes to serve as the EHDS logging component.
- Background loop and health-check patterns: `src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs`, `Health/DPIAHealthCheck.cs`.
- EventIds: the range map has no EHDS range; `9860-9959` is free today (9700-9749 is proposed to plan #812, 9750-9799 is `SecurityBreakTheGlass`, 9800-9829 to plan #816, 9830-9849 to plan #1942).

### Corrections to the issue body

- The issue is labelled `post-1.0` and `priority-low`. SPEC-000 DEC-009 (2026-10-23) brought #808 into 1.0 (SPEC-000 REQ-030); the label is stale. SPEC-002 DEC-015, which kept it post-1.0 apart from the extension points of REQ-052, is superseded for #808.
- "EHDS applies gradually (primary use 2027, secondary use 2029)" is inaccurate: general application is 26 March 2027; priority categories (a)-(c) from 26 March 2029, (d)-(f) from 26 March 2031; secondary use from 26 March 2029 with parts in 2027, 2031 and 2035 (SPEC-002 §3.3, [V]).
- `services.AddEncinaCompliance(config => config.UseEHDS(...))` does not exist. Every compliance package registers with `AddEncina<Module>(options => ...)`; this plan uses `AddEncinaEHDS(options => ...)` plus `AddEHDSAggregates()` for Marten.
- The sample's `[Flags] EHDSDataCategories` is replaced by extensible string identifiers (Design Choice 4, a Dilemma).
- "Coverage >= 85%" and "CHANGELOG.md update" are replaced by the per-flag targets of the package manifest (AGENTS.md §9) and a fragment under `changelog.d/` (AGENTS.md §11).
- "Cross-border exchange via MyHealth@EU", "European EHR exchange format", "Semantic interoperability" and "Fee structures" are not implemented here: the formats are a non-goal of SPEC-002 §2.3 (also stated in #1245), the implementing acts are due by 26 March 2027, and MyHealth@EU needs a national contact point that Encina cannot supply. They are listed in the deferral issue file.
- The issue's cross-cutting matrix defers Multi-Tenancy; SPEC-002 REQ-061 requires every capability it adds or changes to be tenant-aware, so this plan includes it. The issue's Health Checks and Resilience deferrals concern MyHealth@EU connectivity and stay deferred; the module's own store health check is included.

### Affected packages

- `Encina.Compliance.EHDS` (new): everything below.
- `Encina` (core): `src/Encina/Diagnostics/EventIdRanges.cs`, one new range.
- `Encina.Security.Audit` and `Encina.Compliance.Anonymization`: read-only use (access-log adapter; anonymisation requirement on a permit). No change expected; #1245 may change the read audit.
- Repository files: `Encina.slnx`, `.github/coverage-manifest/Encina.Compliance.EHDS.json`.
- Tests: `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.PropertyTests`, `Encina.IntegrationTests` (Marten on PostgreSQL); `.md` justifications under `Encina.LoadTests` and `Encina.BenchmarkTests`.

### Provider category

None of the 10 database providers. The module is event-sourced on Marten and supports PostgreSQL only in 1.0 (AGENTS.md §3, ADR-019, SPEC-002 DEC-008 (a)); there is no InMemory store. Unit tests mock `IAggregateRepository` (NSubstitute); integration tests run against Marten on PostgreSQL through Testcontainers.

---

## Design Choices

<details>
<summary><strong>1. Scope of the first release — what is built before the implementing acts exist</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Build what the Regulation text and SPEC-002 fix: category and purpose registry, permit lifecycle, secondary-use enforcement, expiry monitor, access-log adapter. Defer formats, quality labels, MyHealth@EU and fees** | Delivers a usable governance core; no draft semantics (SPEC-002 INV-003); shapes are additive later | The module is not the full list in the issue; applications needing formats write their own |
| **B) Implement every area of the issue now** | Matches the issue text | Formats and quality labels depend on implementing acts due by 26 March 2027; high risk of rework; MyHealth@EU needs a national node |
| **C) Only the REQ-052 extension points (the outcome of #1245 option A) and no module** | Smallest; no wrong semantics | Contradicts SPEC-000 DEC-009 and REQ-030 (the package is in 1.0) |

### Chosen Option: **A — Governance core now, dependent areas deferred** (recommended, pending the maintainer)

### Rationale

- I recommend A because the permit lifecycle is the part of the Regulation that applications need first (secondary use from 26 March 2029) and that does not depend on a data format. B wins only if the maintainer wants the module to promise the full issue list at 1.0 and accepts rework once the implementing acts are adopted. C wins if the maintainer reads DEC-009 as covering the extension points only.

</details>

<details>
<summary><strong>2. Package placement and registration style</strong></summary>

**Class:** Settled
**Settled by:** SPEC-000 REQ-030 and DEC-009 (the EHDS package is in 1.0); AGENTS.md section 3 (registration completeness, `AddEncina*` methods) and section 5 (one package per regulation, as `Encina.Compliance.*`)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.Compliance.EHDS` with `AddEncinaEHDS(options => ...)` and `AddEHDSAggregates()`** | Matches every compliance package; opt-in; own README, manifest and range | One more package |
| **B) A `Health/` folder inside `Encina.Compliance.GDPR`** | No new package | Mixes a regulation with a different cadence and different dependencies (Marten permits) into GDPR |

### Chosen Option: **A — New package**

### Rationale

- The issue itself names `Encina.Compliance.EHDS` (new package). Nothing registers unless the application calls `AddEncinaEHDS` (pay for what you use, AGENTS.md §1).

</details>

<details>
<summary><strong>3. Persistence — event-sourced on Marten, PostgreSQL only, no InMemory store</strong></summary>

**Class:** Settled
**Settled by:** AGENTS.md section 3 (event-sourced compliance modules have no InMemory stores; unit tests mock `IAggregateRepository`; integration tests use Marten on PostgreSQL); ADR-019; SPEC-002 DEC-008 (a)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `HealthDataPermitAggregate : AggregateBase`, projection and read model, `IEhdsPermitService` over `IAggregateRepository`** | Full history of every permit decision (audit); same pattern as DPIA and LawfulBasis | PostgreSQL only |
| **B) The 10 database providers with a store per provider** | Broader reach | Contradicts the module rule; the issue's own matrix says "Marten event sourcing (ADR-019 #776). No 13-provider matrix required" |

### Chosen Option: **A — Marten aggregate**

### Rationale

- Tests follow the repository's compliance-module pattern. Projections take dependencies through `IDocumentOperations` and constructor injection, never `IServiceProvider`, and one test registers each projection with a real store (AGENTS.md §3).

</details>

<details>
<summary><strong>4. Representation of data categories and purposes</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[Flags] enum` for the priority categories (as in the issue sample) and string identifiers for the secondary-use categories** | `PatientSummary \| LabResults` reads well; compile-time checks for the fixed priority list | Two representations; a new priority category needs a library release; flag values are persisted |
| **B) String identifiers for all categories and purposes, with constants in `EhdsDataCategories` and `EhdsPurposes` (like `HighRiskTriggers`)** | One representation; extensible by delegated or implementing acts without a release; stable persisted values | No compile-time exhaustiveness |
| **C) `[Flags] enum` for everything** | Single, compile-time | The secondary-use list is long and may be extended; a 64-bit flag budget; persisted flag numbers become API |

### Chosen Option: **B — String identifiers with constants** (recommended, pending the maintainer)

### Rationale

- I recommend B because the lists are set by the Regulation and its acts, which can change (SPEC-002 notes implementing acts and parts of secondary use through 2035), and because persisted values are API after 1.0. A wins for an application that wants compile-time combination of a closed list and accepts a release per new category. The precedent for B is `src/Encina.Compliance.DPIA/Model/HighRiskTriggers.cs`.

</details>

<details>
<summary><strong>5. Enforcement of the secondary-use permit — attribute, pipeline behavior, default mode</strong></summary>

**Class:** Settled
**Settled by:** AGENTS.md section 3 (compliance and security gates fail closed; Block/Warn/Disabled enforcement modes with static per-generic-type attribute caching); precedent `LawfulBasisValidationPipelineBehavior` (default `Block`)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[EhdsSecondaryUse(purpose, categories)]` on a request type; `EhdsSecondaryUsePermitBehavior<,>` finds an active permit for the tenant, purpose and categories; `EhdsEnforcementMode` Block (default), Warn, Disabled; missing context (no tenant, lookup failure) denies** | Fail closed; consistent with the other gates; explicit opt-out | Applications must annotate requests |
| **B) No pipeline behavior; applications call `IEhdsPermitService` themselves** | No behavior | Easy to forget; no consistent gate |

### Chosen Option: **A — Attribute and behavior, default Block**

### Rationale

- The module is opt-in as a whole (`AddEncinaEHDS`), so a default of `Block` does not impose anything on applications that do not use it. A micro-enterprise provider that is exempt from the secondary-use data-holder duties (Art. 50 per SPEC-002, unverified) sets the mode to `Disabled`; the Encina library does not decide exemptions.

</details>

<details>
<summary><strong>6. Applicability dates — documented, not enforced</strong></summary>

**Class:** Settled
**Settled by:** SPEC-002 INV-003 (no default follows law that is not yet applicable); AGENTS.md section 1 (every feature is opt-in)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The dates (2027, 2029, 2031) appear in the documentation only; whether to enable the module and in which mode is the application's decision** | No clock logic; no hidden behaviour change on a date | The application must read the dates |
| **B) The behavior switches on at the legal application dates through `TimeProvider`** | Automatic | A behaviour that changes on a date without a deployment surprises operators; the dates may move |

### Chosen Option: **A — Documented only**

### Rationale

- An application that wants warnings before 2029 sets `EhdsEnforcementMode.Warn`.

</details>

<details>
<summary><strong>7. Evidential access log — an adapter over the read audit, shaped by #1245</strong></summary>

**Class:** Settled
**Settled by:** SPEC-002 REQ-052 and REQ-007; ADR-036 (three audit stores); the #1245 spike scope ("mapping of the EHDS logging requirements to the read-audit entry and query")

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IEhdsAccessLogProvider` is a thin adapter over `IReadAuditStore` (who, which subject, which data, when, origin; retention at least the period set by `EhdsOptions.AccessLogRetention`, default three years pending verification of Art. 9); the exact entry mapping follows the outcome of #1245** | Reuses an existing store; no second log | Depends on #1245 and on the evidential read audit (P-05) |
| **B) A separate EHDS access log store** | Independent | Duplicates the read audit; two logs to keep consistent |

### Chosen Option: **A — Adapter, in the last slice**

### Rationale

- This slice does not start before #1245 is decided; the other slices do not depend on it. The three-year retention default is a recommendation to document, not a legal claim (SPEC-002 notes the period as [S]).

</details>

<details>
<summary><strong>8. Items deferred until the implementing acts exist</strong></summary>

**Class:** Settled
**Settled by:** SPEC-002 §3.3 (implementing acts due by 26 March 2027) and §2.3 (EEHRxF and FHIR serializers are a non-goal); SPEC-002 INV-003; AGENTS.md section 6 (Defer with an issue)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Defer formats, quality labelling, semantic interoperability, MyHealth@EU connectivity and fees, with the reason written in one issue file** | No draft semantics; the permit shapes do not need them | These issue areas are open until the acts are adopted |
| **B) Implement them against current drafts** | Earlier | Rework; wrong semantics shipped as defaults |

### Chosen Option: **A — Defer with an issue file**

### Rationale

- The issue file is `artifacts/issues/plan-808-ehds-deferred-areas.md` (see Prerequisites). It is revisited when the implementing acts are published (SPEC-002 AC-025 re-verification rule).

</details>

---

## Implementation Phases

### Phase 1: Package Scaffold, Registry and Options

> **Goal**: A buildable, packable package with the identifiers and the configuration surface. Worker PR 1.

<details>
<summary><strong>Tasks</strong></summary>

All paths are in `src/Encina.Compliance.EHDS/`, namespace `Encina.Compliance.EHDS`.

1. `Encina.Compliance.EHDS.csproj`: references `Encina`, `Encina.Marten`, `Encina.Compliance.GDPR`, `Encina.Compliance.Anonymization`, `Microsoft.CodeAnalysis.PublicApiAnalyzers`, `Microsoft.Extensions.Diagnostics.HealthChecks`; `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`; package metadata and `README.md`; entry in `Encina.slnx`.
2. `.github/coverage-manifest/Encina.Compliance.EHDS.json` (by `mechanical-fixer`).
3. `Model/EhdsDataCategories.cs` (public static): string constants for the priority categories named in the issue (patient summary, electronic prescription, laboratory results, medical images and reports, discharge report) and an `IsKnown(string)` helper. The list is confirmed against the Regulation text when this slice starts; unverified until then.
4. `Model/EhdsPurposes.cs` (public static): constants for the secondary-use purposes in the issue (research, innovation, public health, statistics), confirmed against the Regulation text when this slice starts.
5. `Model/PermitStatus.cs` (`Applied`, `Granted`, `Denied`, `Suspended`, `Revoked`, `Expired`) and `Model/AnonymisationRequirement.cs` (`Anonymised`, `Pseudonymised`, `Identified`).
6. `EhdsEnforcementMode.cs` (`Block`, `Warn`, `Disabled`).
7. `EhdsOptions.cs` (public sealed): `EnforcementMode` (default `Block`), `PublishNotifications` (true), `EnableExpirationMonitoring` (false), `ExpirationCheckInterval` (1 hour), `AccessLogRetention` (`TimeSpan`, default 3 years), `AddHealthCheck` (false), `AssembliesToScan`; XML docs list the dates of Regulation (EU) 2025/327 as documentation only.
8. `EhdsOptionsValidator.cs` (internal): defined enum, positive intervals and retention.
9. `ServiceCollectionExtensions.cs`: `AddEncinaEHDS(Action<EhdsOptions>? configure = null)` registering options and validator, `TimeProvider`, with `TryAdd`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Issue #808, plan docs/plans/ehds-implementation-plan-808.md. New package Encina.Compliance.EHDS for Regulation (EU)
2025/327. Event-sourced on Marten later; this phase has no persistence. Follow the layout of
src/Encina.Compliance.DPIA/ (options, validator, ServiceCollectionExtensions).

TASK:
Create the project, solution entry, README stub, identifier constants, enums, options, validator and AddEncinaEHDS
as in the Tasks block.

KEY RULES:
- Nullable enabled; XML docs on every public API; ArgumentNullException guards on public methods.
- Legal claims you cannot verify against the Regulation text are marked unverified; the constants are confirmed
  against the text before they are written.
- Public text names no private client application; use "the reference application" if needed.
- Add PublicAPI.Unshipped.txt lines; request the coverage manifest from mechanical-fixer.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DPIAOptions.cs
- src/Encina.Compliance.DPIA/ServiceCollectionExtensions.cs
- src/Encina.Compliance.DPIA/Model/HighRiskTriggers.cs
```

</details>

### Phase 2: Permit Aggregate, Events, Projection and Service

> **Goal**: The permit lifecycle on Marten. Worker PR 2.

<details>
<summary><strong>Tasks</strong></summary>

1. `Aggregates/HealthDataPermitAggregate.cs` (public sealed, `AggregateBase`): state `PermitReference` (the HDAB permit number, unique per tenant), `ApplicantId`, `Purpose`, `DataCategories`, `AnonymisationRequirement`, `SecureProcessingEnvironmentId`, `ValidFromUtc`, `ValidUntilUtc`, `Status`, `TenantId`, `ModuleId`. Methods: `Apply(...)` (submit application), `Grant`, `Deny`, `Suspend`, `Reinstate`, `Revoke`, `Expire`; invalid transitions throw `InvalidOperationException` caught by the service and returned as `Left`.
2. `Events/PermitEvents.cs`: `PermitApplied`, `PermitGranted`, `PermitDenied`, `PermitSuspended`, `PermitReinstated`, `PermitRevoked`, `PermitExpired`; `INotification` records; carry `TenantId` and `ModuleId`; no health data and no free text with personal data.
3. `ReadModels/PermitReadModel.cs`, `ReadModels/PermitProjection.cs` (dependencies by constructor and `IDocumentOperations`, never `IServiceProvider`).
4. `Abstractions/IEhdsPermitService.cs`, `Services/DefaultEhdsPermitService.cs`: apply, grant, deny, suspend, reinstate, revoke, get by id, get by reference, query active permits (`purpose`, `categories`, `tenantId`, `atUtc`), query expiring. Every method returns `ValueTask<Either<EncinaError, T>>`; time from `TimeProvider`.
5. `EhdsErrors.cs`: codes `ehds.permit_not_found`, `ehds.permit_invalid_transition`, `ehds.permit_duplicate_reference`, `ehds.service_error`.
6. `EhdsMartenExtensions.cs`: `AddEHDSAggregates()` registering the aggregate and the projection (pattern of `AddDPIAAggregates`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Plan #808, Phase 2. Event-sourced compliance modules have no InMemory stores (AGENTS.md section 3, ADR-019).
Reference implementation: src/Encina.Compliance.DPIA/ (DPIAAggregate, DPIAEvents, DPIAProjection, DefaultDPIAService,
DPIAMartenExtensions).

TASK:
Implement the permit aggregate, events, projection, read model, service, errors and the Marten registration.

KEY RULES:
- Either returns; no exceptions for business logic across the service boundary.
- No health data, personal identifiers or free-text justification in events, errors, logs or tags.
- Time from TimeProvider; no DateTime.UtcNow.
- Projections take IDocumentOperations by constructor; one test registers the projection with a real store.
- CRAP <= 10: keep the transition table in small methods.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs
- src/Encina.Compliance.DPIA/Services/DefaultDPIAService.cs
- src/Encina.Compliance.DPIA/DPIAMartenExtensions.cs
- src/Encina.Compliance.LawfulBasis/Aggregates/LIAAggregate.cs
```

</details>

### Phase 3: Secondary-Use Enforcement

> **Goal**: `[EhdsSecondaryUse]` and the pipeline behavior. Worker PR 3.

<details>
<summary><strong>Tasks</strong></summary>

1. `Attributes/EhdsSecondaryUseAttribute.cs` (public sealed, `AttributeTargets.Class`): `Purpose` (string), `Categories` (`params string[]`), optional `AnonymisationRequirement`.
2. `Pipeline/EhdsSecondaryUsePermitBehavior.cs` (public sealed, `IPipelineBehavior<TRequest, TResponse>`): static per-generic-type attribute cache; with `Disabled` skips; otherwise resolves the tenant from `IRequestContext`, finds an active permit through `IEhdsPermitService` for the purpose, categories and `now`; no tenant or a lookup failure denies (fail closed); `Warn` logs and continues; `Block` returns `EhdsErrors.PermitRequired`. Errors never include the permit content.
3. `EhdsErrors.cs`: `ehds.permit_required`, `ehds.permit_context_missing`.
4. `ServiceCollectionExtensions.cs`: `TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(EhdsSecondaryUsePermitBehavior<,>)))`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Plan #808, Phase 3. Gates fail closed (AGENTS.md section 3): when context is missing (no tenant) or a lookup fails,
the gate denies; the only opt-out is the explicit Disabled mode.

TASK:
Implement the attribute, the pipeline behavior with Block/Warn/Disabled and the error codes.

KEY RULES:
- Static per-generic-type attribute caching.
- EncinaError.Message never reaches logs; use the error code.
- Cover missing tenant, failed lookup, expired permit, permit for another purpose or category, Warn and Disabled.
- CRAP <= 10: extract the permit match into a small method.

REFERENCE FILES:
- src/Encina.Compliance.LawfulBasis/Pipeline/LawfulBasisValidationPipelineBehavior.cs
- src/Encina.Compliance.DPIA/DPIARequiredPipelineBehavior.cs
```

</details>

### Phase 4: Expiry Monitor, Health and Observability

> **Goal**: Background expiry, health check, registered EventIds. Worker PR 4.

<details>
<summary><strong>Tasks</strong></summary>

1. `EhdsPermitExpirationService.cs` (public sealed `BackgroundService`): `PeriodicTimer` with `ExpirationCheckInterval`, `TimeProvider`, catch per iteration (AGENTS.md §3, #2774); each cycle expires granted permits past `ValidUntilUtc` through the service; a failed expiry is retried next cycle and logged by error code; registered only when `EnableExpirationMonitoring` is `true`.
2. `Health/EhdsHealthCheck.cs` (`DefaultName = "encina-ehds"`, static `Tags`): permit service resolvable; counts of active and expiring permits; no content.
3. `src/Encina/Diagnostics/EventIdRanges.cs` (modify, register first, ADR-021): `public static readonly (int Min, int Max) ComplianceEHDS = (9860, 9959);` with the summary "Encina.Compliance.EHDS — European Health Data Space".
4. `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (modify): map `"Encina.Compliance.EHDS"` to `[nameof(EventIdRanges.ComplianceEHDS)]`.
5. `Diagnostics/EhdsLogMessages.cs`: `[LoggerMessage]` from 9860, packed by area (service 9860-9879, enforcement 9880-9889, monitor and health 9890-9899, access log 9900-9909; the rest of the range stays unallocated until used).
6. `Diagnostics/EhdsDiagnostics.cs`: `ActivitySource` and `Meter` named `Encina.Compliance.EHDS`; counters `ehds.permits.total` (tag `status`), `ehds.enforcement.checks.total` (tag `outcome`), `ehds.expiry.cycles.total`; activities per service operation and per enforcement check; no tenant-identifying content beyond the tenant tag.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Plan #808, Phase 4. ADR-021: register the range before use. Background loops and timers never let an exception
escape; the PeriodicTimer loop catches per iteration and a failed operation fails visibly (retry next cycle).
Precedent: src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs and Health/DPIAHealthCheck.cs.

TASK:
Implement the expiry service, health check, range registration, log messages and diagnostics.

KEY RULES:
- Packed EventIds, none outside 9860-9959; no gaps within an area block.
- Never log EncinaError.Message, health data or permit content; error codes and ex.ForLogging() only.
- All time from TimeProvider; tests with FakeTimeProvider; no Thread.Sleep.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs
- src/Encina.Compliance.DPIA/Health/DPIAHealthCheck.cs
```

</details>

### Phase 5: Evidential Access-Log Adapter (conditional on #1245)

> **Goal**: Expose the read audit as the EHDS logging component. Worker PR 5; starts only after #1245 is decided.

<details>
<summary><strong>Tasks</strong></summary>

1. `Abstractions/IEhdsAccessLogProvider.cs`: `QueryAsync(EhdsAccessLogQuery, CancellationToken)` returning `Either<EncinaError, PagedResult<EhdsAccessLogEntry>>`; `EhdsAccessLogEntry` (who, which subject reference, which data category, when, origin) mapped from the read audit entry as #1245 decides.
2. `Services/ReadAuditEhdsAccessLogProvider.cs`: adapter over `IReadAuditStore.QueryAsync`; never returns entries older than `AccessLogRetention` without saying so; purge is not exposed.
3. Registration with `TryAddScoped`, only when `Encina.Security.Audit` read auditing is registered; otherwise `ehds.access_log_unavailable`.
4. If #1245 decides the extension points belong to the read audit itself (its option A), this phase reduces to the mapping and the documentation page.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Plan #808, Phase 5. Do not start before #1245 (EHDS readiness spike) has a recorded outcome; read it first. The
read audit is IReadAuditStore in src/Encina.Security.Audit (ADR-036).

TASK:
Implement the provider contract and the adapter over IReadAuditStore as #1245 decides.

KEY RULES:
- Either returns; the entry carries identifiers, never health data content.
- Tenant-aware: queries are scoped to the caller's tenant (SPEC-002 REQ-061, AC-043).

REFERENCE FILES:
- src/Encina.Security.Audit/ (IReadAuditStore and its entry type)
- docs/architecture/adr/036-three-audit-stores.md
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (REQ-007, REQ-052)
```

</details>

### Phase 6: Testing

> **Goal**: Every flag at its target in every worker PR.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit**: options and validator; registry helpers; aggregate transitions (every valid and invalid transition); projection; service (mocked `IAggregateRepository`); behavior (all modes and the fail-closed cases); expiry service with `FakeTimeProvider` (including a cycle that fails and the next that succeeds, cancellation ends the loop); health check; access-log adapter (mocked `IReadAuditStore`); DI tests with `ValidateOnBuild` and `ValidateScopes` for each registration combination.
2. **Guard**: every public method with parameters (`ArgumentNullException`).
3. **Contract**: `IEhdsPermitService` and `IEhdsAccessLogProvider` contract bases against the default implementations.
4. **Property** (`Encina.Testing.FsCheck`): permit state machine (no sequence of valid commands reaches an invalid state; an active permit never outlives `ValidUntilUtc` after `Expire`); event replay yields the same read model.
5. **Integration** (Marten on PostgreSQL, Testcontainers, shared `[Collection]` fixture, `ClearAllDataAsync` in `InitializeAsync`): full permit lifecycle, projection registered with a real store, enforcement end to end with a granted and an expired permit, two tenants cannot see each other's permits.
6. **Load / Benchmark**: `.md` justifications (low volume, not a hot path; the enforcement behavior reads a projection).
7. **Per-flag targets** (AGENTS.md §9). The package is new, so there is no baseline; the worker proposes a manifest with per-file targets and one-sentence justifications, demanding and realistic: aggregate, behavior and service unit 95, guard 100, contract 100 where an interface exists; projection unit 90, integration 90; options, validator and constants unit 100; expiry service unit 90; health check unit 90. Mutation scope entry is evaluated against ADR-056 criteria when the package ships.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Plan #808, Phase 6. Tests execute real package code (AGENTS.md section 9). Event-sourced compliance modules have
no InMemory stores: unit tests mock IAggregateRepository with NSubstitute; integration tests use Marten on
PostgreSQL through Testcontainers and the shared [Collection] fixture. Assertions use Shouldly through
Encina.Testing.Shouldly; property tests use Encina.Testing.FsCheck.

TASK:
Write the tests of the Tasks block for the slice of your pull request, the .md justifications and the manifest
entries, and measure per-flag coverage with the AGENTS.md section 9 commands.

KEY RULES:
- A test never swallows an exception thrown by the code it drives; no Thread.Sleep; FakeTimeProvider for time.
- Test data builders; no hard-coded GUIDs when avoidable.

REFERENCE FILES:
- tests/Encina.UnitTests/ (existing DPIA and LawfulBasis tests as layout)
- .github/coverage-manifest/Encina.Compliance.DPIA.json
- docs/en/guides/TESTING.md
```

</details>

### Phase 7: Documentation and Finalization

> **Goal**: Docs, changelog, specification page, and verification.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on every public API, with examples; the options list the legal dates as documentation only.
2. `changelog.d/808-ehds-compliance-module.added.md` (one fragment per PR; by `mechanical-fixer`).
3. `src/Encina.Compliance.EHDS/README.md`, a configuration page, and a package specification page with the article table (act, article, status, Encina types, evidence, tracking issue) that lists the deferred areas as "Not covered" (SPEC-002 REQ-022; by `docs-writer`).
4. `docs/INVENTORY.md` rows and `ROADMAP.md` update (the stale `post-1.0` label).
5. An ADR only if the maintainer's answer to Design Choice 1 changes the architecture.
6. Build with zero warnings; `dotnet test` on the five projects; the per-flag measurement and `--check-justifications` commands of AGENTS.md §9.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
Plan #808, Phase 7. Documentation edits go to the docs-writer; changelog fragments, PublicAPI lines and coverage
manifests go to mechanical-fixer (path-ownership hooks enforce it).

TASK:
Finish the documentation, changelog fragment and PublicAPI lines; run the build, tests and coverage commands.

KEY RULES:
- Cite Regulation (EU) 2025/327 only for provisions you have verified against the text; mark the rest unverified.
- No hand-typed coverage figures (covref markers); public text names no private client application.

REFERENCE FILES:
- changelog.d/README.md
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (section 3.3, REQ-022, REQ-052)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Article / Section | Relevance |
|----------|-------------------|-----------|
| Regulation (EU) 2025/327 (EHDS) | In force 25 March 2025; general application 26 March 2027; priority categories (a)-(c) 26 March 2029, (d)-(f) 26 March 2031; secondary use 26 March 2029 (parts 2027, 2031, 2035) | Dates, per SPEC-002 §3.3 [V] |
| EHDS | Art. 9 (access information at least three years) | Access-log retention default; marked [S] in SPEC-002, unverified here |
| EHDS | Art. 50 (micro-enterprise exemption from secondary-use data-holder duties) | Why `Disabled` exists; unverified here |
| EHDS | Primary and secondary use chapters | Chapter and article numbers not cited until verified against the text |
| GDPR (Reg. (EU) 2016/679) | Art. 9 | Special categories; foundation |
| SPEC-002 | REQ-052, REQ-007, REQ-022, REQ-061, REQ-062, INV-003, AC-043, AC-044, AC-025 | Extension points, evidential access log, article table, tenant-aware and instrumented, no default follows non-applicable law, re-verification rule |
| SPEC-000 | DEC-009, REQ-030 | #808 is in 1.0 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| DPIA module layout | `src/Encina.Compliance.DPIA/` (aggregate, events, projection, service, Marten extensions, health, reminder service) | Pattern for every part of the permit module |
| `LawfulBasisValidationPipelineBehavior` | `src/Encina.Compliance.LawfulBasis/Pipeline/` | Enforcement-mode behavior pattern |
| `IReadAuditStore` | `src/Encina.Security.Audit/` | Source of the access log (Phase 5) |
| `Encina.Compliance.Anonymization` | `src/Encina.Compliance.Anonymization/` | `AnonymisationRequirement` on a permit refers to its techniques |
| `Encina.Compliance.DataResidency` | `src/Encina.Compliance.DataResidency/` | Secure processing environment location, a documented integration, not coded here |
| `Encina.Marten` | `src/Encina.Marten/` | `AggregateBase`, `IAggregateRepository` |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs`, `tests/Encina.UnitTests/Testing/Architecture/` | New range and mapping |
| Coverage manifest | `.github/coverage-manifest/` | New package manifest |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.EHDS` | **9860-9959** (new `ComplianceEHDS`) | Free per `EventIdRanges.cs` today; 9700-9749 is proposed to plan #812, 9750-9799 is `SecurityBreakTheGlass`, 9800-9829 to plan #816, 9830-9849 to plan #1942; register first (ADR-021). Allocated in blocks by area as Phase 4 lists; the rest stays unused until needed |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Project and package files | 5 | csproj, two PublicAPI files, README, solution entry |
| Models, enums, constants, options | 8 | categories, purposes, status, anonymisation requirement, enforcement mode, options, validator, errors |
| Aggregate, events, read model, projection | 5 | |
| Services and abstractions | 5 | permit service, access-log provider and adapter, expiry service, Marten extension |
| Pipeline | 2 | attribute and behavior |
| Health and diagnostics | 4 | health check, log messages, diagnostics, `EventIdRanges` entry |
| Tests | ~40 | unit 18, guard 6, contract 3, property 2, integration 5, justifications 2, event-id test update and DI tests inside unit |
| Documentation and records | 8 | README, configuration page, specification page, changelog fragments, manifest, INVENTORY, ROADMAP |

Total about 77 files, far above the 15-file and 1,500-line limits of one worker PR (AGENTS.md §11). Proposed split into five worker issues, each one pull request (the orchestrator opens the sub-issues; this plan is the parent): **PR 1** Phase 1 (scaffold, registry, options: about 14 files); **PR 2** Phase 2 (permit aggregate and service with unit, guard, contract and Marten integration tests: about 15 files, split the integration tests into a second PR if the line limit is exceeded); **PR 3** Phase 3 (enforcement: about 10 files); **PR 4** Phase 4 (expiry, health, observability: about 12 files); **PR 5** Phase 5 (access-log adapter, after #1245: about 8 files). Phases 6 and 7 are carried inside each PR for its own slice.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete implementation prompt (all phases)</strong></summary>

```text
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 / C# 14 library (nullable on, ROP with Either<EncinaError, T>, TimeProvider
injection, [LoggerMessage] logging with EventIds in registered ranges, TryAdd DI). AGENTS.md binds every change.
Issue #808 creates Encina.Compliance.EHDS for Regulation (EU) 2025/327. Event-sourced on Marten (ADR-019),
PostgreSQL only, no InMemory store (AGENTS.md section 3). The module is in 1.0 (SPEC-000 DEC-009, REQ-030); areas
that depend on unpublished implementing acts are deferred (SPEC-002 INV-003).
Plan: docs/plans/ehds-implementation-plan-808.md (follow the Design Choices as decided in its Maintainer
Decisions section, once it exists). Work one worker pull request at a time (five, see the plan).

IMPLEMENTATION OVERVIEW:
1. Package scaffold, string constants for categories and purposes, enums, options, validator, AddEncinaEHDS.
2. HealthDataPermitAggregate with events PermitApplied, Granted, Denied, Suspended, Reinstated, Revoked, Expired;
   projection (IDocumentOperations), read model, IEhdsPermitService, AddEHDSAggregates.
3. [EhdsSecondaryUse] and EhdsSecondaryUsePermitBehavior with Block (default), Warn, Disabled; fail closed.
4. EhdsPermitExpirationService (PeriodicTimer, catch per iteration), EhdsHealthCheck, ComplianceEHDS = (9860, 9959)
   registered first, EhdsLogMessages, EhdsDiagnostics.
5. IEhdsAccessLogProvider over IReadAuditStore, after #1245 is decided.
6. Tests on every flag; Marten integration with Testcontainers; .md justifications for load and benchmark.
7. Docs by docs-writer, changelog fragments and PublicAPI and manifest by mechanical-fixer, zero warnings.

KEY PATTERNS:
- Fail closed; Either returns; no health data, personal identifier or EncinaError.Message in logs, tags or events.
- Legal claims not verified against the Regulation text are marked unverified; the Regulation's dates are
  documentation only, not clock-driven behaviour.
- Tenant-aware (TenantId on every event and query); opt-in registration.
- Pre-1.0: no [Obsolete], no aliases. CRAP <= 10 on changed methods. Per-flag coverage targets.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/ (module layout)
- src/Encina.Compliance.LawfulBasis/Pipeline/LawfulBasisValidationPipelineBehavior.cs
- src/Encina.Security.Audit/ (IReadAuditStore)
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/specifications/SPEC-002-eu-regulatory-readiness.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Health data and permit decisions must not be cached (issue); the enforcement behavior reads the projection on each request |
| 2 | OpenTelemetry | ✅ | `ActivitySource` and `Meter` `Encina.Compliance.EHDS`, counters by status and outcome (Phase 4) |
| 3 | Structured Logging | ✅ | `EhdsLogMessages` in the new range 9860-9959 (Phase 4) |
| 4 | Health Checks | ✅ | `EhdsHealthCheck` for the module's own stores; MyHealth@EU connectivity stays deferred (issue file) |
| 5 | Validation | ✅ | Options validator; aggregate invariants on permit data (Phases 1-2) |
| 6 | Resilience | ⏭️ | Only relevant to cross-border exchange, which is deferred with MyHealth@EU in `artifacts/issues/plan-808-ehds-deferred-areas.md` |
| 7 | Distributed Locks | ❌ | Marten's optimistic concurrency serialises permit commands; the expiry service is idempotent per permit, so two instances cannot double-expire |
| 8 | Transactions | ✅ | Each lifecycle command is one aggregate save |
| 9 | Idempotency | ✅ | A permit reference is unique per tenant: a duplicate application returns `ehds.permit_duplicate_reference` (Phase 2) |
| 10 | Multi-Tenancy | ✅ | `TenantId` on every event and projection and in the enforcement lookup; a two-tenant integration test (SPEC-002 REQ-061, AC-043); the issue deferred it |
| 11 | Module Isolation | ❌ | `ModuleId` is carried on events like the other compliance modules; no module-scoped behaviour is needed |
| 12 | Audit Trail | ✅ | The permit events are the audit trail (ADR-019); the access-log adapter exposes the read audit (Phase 5) |

---

## Prerequisites & Dependencies

### Must coordinate

| Item | State (2026-11-11) | Relationship |
|------|--------------------|--------------|
| #1245 EHDS readiness spike (P-43) | Open | Decides the logging and export extension points; Phase 5 starts only after its outcome. Phases 1 to 4 do not depend on it |
| SPEC-002 P-05 (evidential read audit) and P-07 (DSR access export) | Planned | The read audit that Phase 5 adapts; the export hook for a future EEHRxF serializer |
| #407 Anonymization, #405 DataResidency | Closed | Referenced by `AnonymisationRequirement` and by the documented secure-environment location; no code change needed |
| #1186 SPEC-002 tracking | Open | REQ-022: a package specification with an article table |
| Implementing acts of Regulation (EU) 2025/327 | Due by 26 March 2027 per SPEC-002 (unverified here) | Gate for the deferred areas |

### Advisable, not blocking

| Item | State | Relationship |
|------|-------|--------------|
| #415 Encina.Compliance.AIAct | Closed | AI on health data must satisfy the AI Act and EHDS; documentation cross-reference only |
| #1233 `Encina.Http` | Open | Possible later use for HDAB or national-node clients |

### Missing prerequisites and findings, written as issue files

| File | Template | Finding |
|------|----------|---------|
| `artifacts/issues/plan-808-ehds-deferred-areas.md` | `[FEATURE]` | EEHRxF and HL7 FHIR interoperability, data-quality labelling, MyHealth@EU connectivity and secondary-use fees have no tracking issue and wait for the implementing acts |

---

## Next Steps

1. The maintainer decides Design Choices 1 and 4 when work on #808 starts; the orchestrator records them in a "Maintainer Decisions" section, updates each Chosen Option, then runs `adversarial-reviewer` on Choices 2, 3, 5, 6, 7 and 8.
2. Open the issue file above (`open-issue` skill) and link it here and from #808; decompose #808 into the five worker issues named in the file-count section, since an issue of this size cannot be briefed whole (AGENTS.md §11), and correct the stale `post-1.0` label.
3. Link this plan from #808, then start PR 1 in a worktree (`worker-brief` skill) with the local CRAP table required in the brief.
4. Re-verify the EHDS dates, Art. 9 and Art. 50 against the Regulation text and the implementing acts within 30 days of any of their publication (SPEC-002 AC-025).
