# Implementation Plan: `Encina.Compliance.EHDS` — European Health Data Space, Regulation (EU) 2025/327

> **Issue**: [#808](https://github.com/dlrivada/Encina/issues/808)
> **Type**: Feature
> **Milestone**: v0.17.0 — Compliance Lifecycle (the issue still carries the label `post-1.0`; see Corrections)
> **Complexity**: Very High (7 phases, new package, Marten only, ~35 production files; split into eight worker pull requests)
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

State of the code on `d1793713d` (current `origin/main`); the facts below were checked on it.

- No `Encina.Compliance.EHDS` project, no EHDS type and no health-data permit concept exist in `src/` (search confirms). Fifteen compliance packages exist under `src/Encina.Compliance.*`; the event-sourced ones (BreachNotification, DPIA, LawfulBasis and others) register aggregates with `AddBreachNotificationAggregates`, `AddDPIAAggregates` (`*MartenExtensions.cs`) and hold no InMemory store (AGENTS.md §3).
- The aggregate pattern is `AggregateBase` (for example `src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs`) with events that implement `INotification` (`Events/*.cs`), a projection and a read model (`ReadModels/`), and a service over `IAggregateRepository`.
- The pipeline behavior pattern with enforcement modes and an attribute is `src/Encina.Compliance.LawfulBasis/Pipeline/LawfulBasisValidationPipelineBehavior.cs` (`LawfulBasisEnforcementMode`: Block, Warn, Disabled) and `src/Encina.Compliance.DPIA/DPIARequiredPipelineBehavior`.
- The read audit is `IReadAuditStore` in `src/Encina.Security.Audit/` (`LogReadAsync`, `GetAccessHistoryAsync`, `GetUserAccessHistoryAsync`, `QueryAsync`, `PurgeEntriesAsync`), ADR-036 (three audit stores). #1245 decides which extension points the read audit exposes to serve as the EHDS logging component.
- Background loop and health-check patterns: `src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs`, `Health/DPIAHealthCheck.cs`.
- EventIds: the range map has no EHDS range; `6450-6549` is free today and assigned to this plan in the batch EventId registry (block 6400-6549: 6400-6429 plan #816, 6430-6449 plan #1942; plan #812 uses 9700-9749).

### Corrections to the issue body

- The issue is labelled `post-1.0` and `priority-low`. SPEC-000 DEC-009 (2026-10-23) brought #808 into 1.0 (SPEC-000 REQ-030); the label is stale. SPEC-002 DEC-015, which kept it post-1.0 apart from the extension points of REQ-052, is superseded for #808.
- "EHDS applies gradually (primary use 2027, secondary use 2029)" is inaccurate: general application is 26 March 2027; priority categories (a)-(c) from 26 March 2029, (d)-(f) from 26 March 2031; secondary use from 26 March 2029 with parts in 2027, 2031 and 2035 (SPEC-002 §3.3, [V]).
- `services.AddEncinaCompliance(config => config.UseEHDS(...))` does not exist. Every compliance package registers with `AddEncina<Module>(options => ...)`; this plan uses `AddEncinaEHDS(options => ...)` plus `AddEHDSAggregates()` for Marten.
- The sample's `[Flags] EHDSDataCategories` is replaced by extensible string identifiers (Design Choice 4, a Dilemma).
- "Coverage >= 85%" and "CHANGELOG.md update" are replaced by the per-flag targets of the package manifest (AGENTS.md §9) and a fragment under `changelog.d/` (AGENTS.md §11).
- "Cross-border exchange via MyHealth@EU", "European EHR exchange format", "Semantic interoperability" and "Fee structures" are not implemented here: the formats are a non-goal of SPEC-002 §2.3 (also stated in #1245), the implementing acts are due by 26 March 2027, and MyHealth@EU needs a national contact point that Encina cannot supply. They are listed in the deferral issue file.
- The issue's cross-cutting matrix defers Multi-Tenancy; SPEC-002 REQ-061 requires every capability it adds or changes to be tenant-aware, so this plan includes it. The issue's Health Checks and Resilience deferrals concern MyHealth@EU connectivity and stay deferred; the module's own store health check is included.

### Affected packages

- `Encina.Compliance.EHDS` (new): everything below. Project references: `Encina` and `Encina.Marten` in PR 1a; `Encina.Security.Audit` only in PR 5 (the access-log adapter); no reference to `Encina.Compliance.GDPR` or `Encina.Compliance.Anonymization`, because no planned type uses them (`AnonymisationRequirement` is the module's own enum; Anonymization and DataResidency are documentation cross-references only).
- `Encina` (core): `src/Encina/Diagnostics/EventIdRanges.cs`, one new range.
- `Encina.Security.Audit`: read-only use (access-log adapter, PR 5). No change expected; #1245 may change the read audit.
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
**Settled by:** SPEC-000 REQ-030 and DEC-009 (REQ-030 lists the EHDS package, #804-#808, as part of 1.0); AGENTS.md section 3 (registration completeness, `AddEncina*` methods)

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

**Class:** Dilemma (recommended, pending the maintainer)

The cited rules (AGENTS.md section 3: a gate that exists fails closed; Block/Warn/Disabled modes with static per-generic-type attribute caching; precedent `LawfulBasisValidationPipelineBehavior`, default `Block`) apply only to a gate that exists, so they do not by themselves require one.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[EhdsSecondaryUse(purpose, categories)]` on a request type; `EhdsSecondaryUsePermitBehavior<,>` finds an active permit for the tenant, purpose and categories; `EhdsEnforcementMode` Block (default), Warn, Disabled; missing context (no tenant, lookup failure) denies. `Warn` logs the outcome by code (EventId in the enforcement area), increments the counter with outcome `warned`, and lets the request continue** | Fail closed by default; consistent with the other gates; explicit opt-out; a user-facing default (`Block`) that is registered here | Applications must annotate requests |
| **B) No pipeline behavior; applications call `IEhdsPermitService` themselves** | No behavior and no default to register; nothing is enforced on a date the law has not reached | Easy to forget; no consistent gate |

### Chosen Option: **A — Attribute and behavior, default Block** (recommended, pending the maintainer)

### Rationale

- A wins for an application that wants the library to enforce the permit; B wins if enforcement is left to the application until the implementing acts exist. The module is opt-in as a whole (`AddEncinaEHDS`), so a default of `Block` imposes nothing on applications that do not use it. A micro-enterprise provider that is exempt from the secondary-use data-holder duties (Art. 50 per SPEC-002, unverified) sets the mode to `Disabled`; the Encina library does not decide exemptions.
- **Tenant semantics of the gate (SPEC-002 REQ-061, #1257).** The behavior resolves the tenant through `ITenantScopeResolver.ResolveRead(null)` of #1257 (named in Prerequisites): in a single-tenant application (no `MultiTenancyMarker`, no ambient tenant) it passes with the single-tenant scope the resolver defines (no tenant, or the default tenant, per the #1257 Dilemma; this plan is agnostic to it), so a single-tenant deployment is not denied wholesale; in a multi-tenant application a missing tenant is denied with `ehds.permit_context_missing`. Tests: single-tenant allows, multi-tenant without a tenant denies.

</details>

<details>
<summary><strong>6. Applicability dates — documented, not enforced</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

Both options satisfy SPEC-002 INV-003 (no default follows law that is not yet applicable) and AGENTS.md section 1 (every feature is opt-in): a documented switch on a date is a stated behavior, not a hidden default.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The dates (2027, 2029, 2031) appear in the documentation only; whether to enable the module and in which mode is the application's decision** | No clock logic; no hidden behaviour change on a date | The application must read the dates |
| **B) The behavior switches on at the legal application dates through `TimeProvider`** | Automatic | A behaviour that changes on a date without a deployment surprises operators; the dates may move |

### Chosen Option: **A — Documented only** (recommended, pending the maintainer)

### Rationale

- A wins when operators must control when enforcement starts; B wins for an application that wants the library to follow the calendar and accepts the surprise. An application that wants warnings before 2029 sets `EhdsEnforcementMode.Warn`.

</details>

<details>
<summary><strong>7. Evidential access log — an adapter over the read audit, shaped by #1245</strong></summary>

**Class:** Settled
**Settled by:** SPEC-002 REQ-052 and REQ-007; ADR-036 (three audit stores); the #1245 spike scope ("mapping of the EHDS logging requirements to the read-audit entry and query")

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IEhdsAccessLogProvider` is a thin adapter over `IReadAuditStore` (who, which subject, which data, when, origin; retention at least the period set by `EhdsOptions.AccessLogRetention`, whose default is Design Choice 9); the exact entry mapping follows the outcome of #1245** | Reuses an existing store; no second log | Depends on #1245 and on the evidential read audit (P-05) |
| **B) A separate EHDS access log store** | Independent | Duplicates the read audit; two logs to keep consistent |

### Chosen Option: **A — Adapter, in the last slice**

### Rationale

- This slice does not start before #1245 is decided; the other slices do not depend on it. The adapter is tenant-scoped and ADR-050-aware (the access log is read-only, so it needs none of its prerequisites).

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

<details>
<summary><strong>9. The default of `AccessLogRetention`</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

The access-information retention of EHDS Art. 9 ("at least three years") is marked [S] in SPEC-002 and unverified in this plan, so the default is a legal-adjacent value with a user-visible consequence (data kept or purged).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Require explicit configuration: `AccessLogRetention` has no default and the options validator fails the host start when the access-log adapter is registered without it** | No legal value ships as a default (SPEC-002 INV-003); the operator states the period | One more required setting |
| **B) Default to three years, documented as unverified** | Works out of the box | A default drawn from an unverified provision |

### Chosen Option: **A — Explicit configuration** (recommended, pending the maintainer)

### Rationale

- A wins until Art. 9 is verified against the text; B wins if the maintainer accepts a documented, unverified default for convenience. Under A the validator also rejects a non-positive period.

</details>

<details>
<summary><strong>10. Persisted spellings, `PermitReference` format and the applicant identifier</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

Three choices persisted in events and therefore API after 1.0: the spelling and casing of the string constants of `EhdsDataCategories` and `EhdsPurposes`, the format of `PermitReference`, and what `ApplicantId` may hold.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Constants are lowercase kebab-case ids (`patient-summary`, `public-health`), compared ordinally; `PermitReference` is an opaque non-blank string of at most 128 characters, ordinal comparison, padded values rejected; `ApplicantId` is an organisation or pseudonymous reference, never a natural person's direct identifier (an erasure request cannot delete an immutable event; if a natural person is possible, the applicant is stored as a pseudonymous reference and crypto-shredding applies)** | One stable representation; no erasure conflict with immutable events | Applications map their identifiers |
| **B) PascalCase constants, free-form references and a free `ApplicantId`** | Closer to the issue text | Aliasing by casing, unbounded values, an immutable event that may hold a direct identifier |

### Chosen Option: **A — Lowercase ids, bounded opaque reference, pseudonymous applicant** (recommended, pending the maintainer)

### Rationale

- A wins when persisted values must be stable and event data free of direct identifiers; B wins only if the maintainer prefers the issue's wording.

</details>

---

## Implementation Phases

### Phase 1: Package Scaffold, Registry and Options

> **Goal**: A buildable, packable package with the identifiers and the configuration surface. Worker PR 1.

<details>
<summary><strong>Tasks</strong></summary>

All paths are in `src/Encina.Compliance.EHDS/`, namespace `Encina.Compliance.EHDS`.

1. `Encina.Compliance.EHDS.csproj` (PR 1a): references `Encina`, `Encina.Marten`, `Microsoft.CodeAnalysis.PublicApiAnalyzers`, `Microsoft.Extensions.Diagnostics.HealthChecks`; no reference to GDPR or Anonymization (no planned type uses them) and none to `Encina.Security.Audit` until PR 5; `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`; package metadata and `README.md`; entry in `Encina.slnx`.
2. `.github/coverage-manifest/Encina.Compliance.EHDS.json` (by `mechanical-fixer`; every PR adds the entries of its own files).
3. `Model/EhdsDataCategories.cs` (public static, **provisional** until verified; lowercase ids per Design Choice 10): string constants for the priority categories named in the issue (patient summary, electronic prescription, laboratory results, medical images and reports, discharge report) and an `IsKnown(string)` helper. The list is confirmed against the Regulation text when this slice starts; unverified until then, and the PR 1a brief says so.
4. `Model/EhdsPurposes.cs` (public static): constants for the secondary-use purposes in the issue (research, innovation, public health, statistics), confirmed against the Regulation text when this slice starts.
5. `Model/PermitStatus.cs` (`Applied`, `Granted`, `Denied`, `Suspended`, `Revoked`, `Expired`) and `Model/AnonymisationRequirement.cs` (`Anonymised`, `Pseudonymised`, `Identified`).
6. `EhdsEnforcementMode.cs` (`Block`, `Warn`, `Disabled`).
7. `EhdsOptions.cs` (public sealed): `EnforcementMode` (default `Block`), `PublishNotifications` (true), `EnableExpirationMonitoring` (false), `ExpirationCheckInterval` (1 hour), `AccessLogRetention` (`TimeSpan?`, no default, required when the access-log adapter is registered, Design Choice 9 recommended A), `AddHealthCheck` (false), `AssembliesToScan`; XML docs list the dates of Regulation (EU) 2025/327 as documentation only. `PublishNotifications` (true) publishes the permit events through `IEncina.Publish`; because ADR-050 requires an Outbox, Inbox, dead letter queue and multi-instance coordination for event-sourced modules, publication with `PublishNotifications = true` needs the Outbox (`OutboxStoreMarten`, #2383) through the ADR-050 startup validation, and `PublishNotifications = false` needs none of them for this module.
8. `EhdsOptionsValidator.cs` (internal): defined enum, positive intervals and retention.
9. `ServiceCollectionExtensions.cs`: `AddEncinaEHDS(Action<EhdsOptions>? configure = null)` registering options and validator, `TimeProvider`, with `TryAdd`.
10. **Registered in PR 1b** (not PR 4): the `ComplianceEHDS` range in `EventIdRanges.cs`, its map entry in `EncinaEventIdAllocationTests`, and `Diagnostics/EhdsLogMessages.cs` with the ids the early PRs use (Phase 4 task 5), so no PR logs with an unregistered EventId.

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

1. `Aggregates/HealthDataPermitAggregate.cs` (public sealed, `AggregateBase`): state `PermitReference` (the HDAB permit number, unique per tenant), `ApplicantId`, `Purpose`, `DataCategories`, `AnonymisationRequirement`, `SecureProcessingEnvironmentId`, `ValidFromUtc`, `ValidUntilUtc`, `Status`, `TenantId`, `ModuleId`. Per ADR-053 the aggregate returns `Either`, never throws for a business rule: the factory `Apply(...)` (submit application) returns `Either<EncinaError, HealthDataPermitAggregate>`, and `Grant`, `Deny`, `Suspend`, `Reinstate`, `Revoke`, `Expire` return `Either<EncinaError, Unit>`; the rule code is decided **before** `RaiseEvent`, so an invalid transition raises no event and returns `Left(ehds.permit_invalid_transition)`. The service never catches `InvalidOperationException`. A unit test per invalid transition asserts the `Left` and that no exception is thrown and no event is raised. (`DPIAAggregate` on main still throws; that is the debt ADR-053 names, not a precedent.)
2. `Events/PermitEvents.cs`: `PermitApplied`, `PermitGranted`, `PermitDenied`, `PermitSuspended`, `PermitReinstated`, `PermitRevoked`, `PermitExpired`; `INotification` records; carry `TenantId` and `ModuleId`; no health data and no free text with personal data.
3. `ReadModels/PermitReadModel.cs`, `ReadModels/PermitProjection.cs` (dependencies by constructor and `IDocumentOperations`, never `IServiceProvider`).
4. `Abstractions/IEhdsPermitService.cs`, `Services/DefaultEhdsPermitService.cs`: apply, grant, deny, suspend, reinstate, revoke, get by id, get by reference, query active permits (`purpose`, `categories`, `tenantId`, `atUtc`), query expiring. Every method returns `ValueTask<Either<EncinaError, T>>`; time from `TimeProvider`. **Tenant rules (SPEC-002 REQ-061, #1257):** every call resolves the tenant through `ITenantScopeResolver` (`ResolveRead` for queries, `ResolveWrite` for commands); a single-tenant application is allowed with the resolver's single-tenant scope, a multi-tenant call without a tenant is denied with `ehds.permit_context_missing`. The **expiry sweep is the exception**: it has no ambient tenant, so it reads expiring permits across tenants through an operator-level projection query and calls `Expire` with **each permit's own `TenantId` as an explicit parameter** (#1257 resolver `ResolveWrite(explicitTenantId)`, which this plan needs to accept an explicit tenant when there is no ambient tenant: the case is a hard requirement on PR 1257-A1, confirmed when A1 lands, and the sweep test below asserts it), never mixing tenants in one unit of work. **Uniqueness of `PermitReference`** is a composite unique index `(TenantId, PermitReference)` on the read model (never a concatenated string, which would alias `a:b` + `c` with `a` + `b:c`), created with `NULLS NOT DISTINCT` so a no-tenant scope (#1257 Dilemma A) and the default tenant (B) both enforce it; the PostgreSQL version of the Testcontainers fixture must support it (15 or later, confirmed when PR 2b starts).
5. `EhdsErrors.cs`: codes created with `EncinaErrors.Create(code, ErrorCategory, ...)` (ADR-062), each with a `GetCategory` unit test: `ehds.permit_not_found` NotFound (R1: the request names the permit), `ehds.permit_invalid_transition` Conflict (R2), `ehds.permit_duplicate_reference` AlreadyExists (R2), `ehds.service_error` Internal (R4).
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
- Either returns from the aggregate and the service (ADR-053): no exception for an invalid transition, the rule is decided before RaiseEvent.
- Tenant: ITenantScopeResolver of #1257; the expiry sweep passes each permit's TenantId explicitly; unique index (TenantId, PermitReference).
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
3. `EhdsErrors.cs`: `ehds.permit_required` (`ErrorCategory.Forbidden`), `ehds.permit_context_missing` (`InvalidRequest`, ADR-062 R3: the request lacks a tenant); a `GetCategory` test each.
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

1. `EhdsPermitExpirationService.cs` (public sealed `BackgroundService`): `PeriodicTimer` with `ExpirationCheckInterval`, `TimeProvider`, catch per iteration (AGENTS.md §3, #2774); each cycle expires granted permits past `ValidUntilUtc` through the service, tenant by tenant (Phase 2 task 4); a failed expiry is retried next cycle and logged by error code; **a concurrency conflict, or a permit already expired by another instance, is success-by-another-instance** (no `Left`, no error log; ADR-050 assumes several instances for event-sourced modules); registered only when `EnableExpirationMonitoring` is `true`.
2. `Health/EhdsHealthCheck.cs` (`DefaultName = "encina-ehds"`, static `Tags`): permit service resolvable; counts of active and expiring permits; no content.
3. `src/Encina/Diagnostics/EventIdRanges.cs` (modify, register first, ADR-021; **done in PR 1b**, see Phase 1 task 10): `public static readonly (int Min, int Max) ComplianceEHDS = (6450, 6549);` with the summary "Encina.Compliance.EHDS — European Health Data Space".
4. `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (modify, PR 1b): map `"Encina.Compliance.EHDS"` to `[nameof(EventIdRanges.ComplianceEHDS)]`.
5. `Diagnostics/EhdsLogMessages.cs` (created in PR 1b): `[LoggerMessage]` ids numbered **sequentially from 6450 in creation order, with no gaps**, grouped by area inside the file (service, enforcement, monitor and health, access log) rather than by reserved sub-blocks; each PR appends the ids it uses; the rest of the range stays unallocated until used.
6. `Diagnostics/EhdsDiagnostics.cs`: `ActivitySource` and `Meter` named `Encina.Compliance.EHDS`; counters `ehds.permits.total` (tag `status`), `ehds.enforcement.checks.total` (tag `outcome`), `ehds.expiry.cycles.total`; activities per service operation and per enforcement check. The tenant is a tag or attribute **only when multi-tenancy is on**, through the #2676 helper once it exists (ADR-054 D4), and never in log messages.

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
- Packed EventIds, none outside 6450-6549; no gaps within an area block.
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
3. Registration with `TryAddScoped`, only when `Encina.Security.Audit` read auditing is registered; otherwise `ehds.access_log_unavailable` (`ErrorCategory.Internal`, ADR-062 R1: a wiring defect; `GetCategory` test). Queries use `ITenantScopeResolver.ResolveRead` and the access-log retention of Design Choice 9.
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
   Tenant tests (each in the PR of its code): a single-tenant application is allowed (permit service and behavior), a multi-tenant call without a tenant is denied, and an expiry sweep over the permits of two tenants expires each under its own tenant, with no ambient tenant (the resolver accepts the explicit tenant), and never mixes them.
7. **Per-flag targets** (AGENTS.md §9). The package is new, so every file is "new" with no manifest baseline; one number per flag and one sentence per file (measured values replace them and never go down; mutation scope entry is evaluated against ADR-056 when the package ships):

| File (PR) | Flags | Justification |
|-----------|-------|---------------|
| `Aggregates/HealthDataPermitAggregate.cs` (2a) | unit 95, guard 100, property 90 | every valid and invalid transition is a unit case; FsCheck proves the state machine. |
| `Events/PermitEvents.cs` (2a) | unit 90 | records built and replayed in the aggregate and projection tests. |
| `ReadModels/PermitProjection.cs`, `PermitReadModel.cs` (2b) | unit 90, integration 90 | replay and the real-store registration are integration cases. |
| `Services/DefaultEhdsPermitService.cs` (2b) | unit 95, guard 100, contract 100, integration 85 | mocked repository for unit; Marten for the lifecycle and two-tenant tests. |
| `EhdsMartenExtensions.cs` (2b) | unit 90, guard 100 | registration proven by DI tests with `ValidateOnBuild`. |
| `Attributes/EhdsSecondaryUseAttribute.cs` (3) | unit 100, guard 100 | constructor guards only. |
| `Pipeline/EhdsSecondaryUsePermitBehavior.cs` (3) | unit 95, guard 100, integration 80 | all modes and the fail-closed cases; one end-to-end test. |
| `EhdsErrors.cs` (2a, 3, 5) | unit 100 | factories asserted with their category. |
| `EhdsOptions.cs`, `EhdsOptionsValidator.cs`, `EhdsEnforcementMode.cs`, `Model/*.cs` constants and enums (1a, 1b) | unit 100 | pure data and validation. |
| `ServiceCollectionExtensions.cs` (1b and each later PR) | unit 90, guard 100 | registration, covered by the DI tests. |
| `EhdsPermitExpirationService.cs` (4) | unit 90, integration 70 | `FakeTimeProvider` cycles; a two-tenant sweep on Marten. |
| `Health/EhdsHealthCheck.cs` (4) | unit 90 | healthy and unhealthy paths. |
| `Diagnostics/EhdsDiagnostics.cs`, `EhdsLogMessages.cs` (1b, 4) | unit 90 | meter listener and fake logger. |
| `Abstractions/IEhdsAccessLogProvider.cs` (5) | unit 0 | an interface, no executable line. |
| `Services/ReadAuditEhdsAccessLogProvider.cs` (5) | unit 95, guard 100, contract 100 | mocked `IReadAuditStore`; the contract base runs the adapter. |

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
| EHDS | Art. 9 (access information at least three years) | Access-log retention (no default, Design Choice 9); marked [S] in SPEC-002, unverified here |
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
| `Encina.Compliance.EHDS` | **6450-6549** (new `ComplianceEHDS`) | Free per `EventIdRanges.cs` today and assigned to this plan (block 6400-6549: 6400-6429 plan #816, 6430-6449 plan #1942; plan #812 uses 9700-9749); registered, mapped and `EhdsLogMessages` created in PR 1b (ADR-021); ids packed sequentially from 6450 in creation order, grouped by area inside the file; the rest stays unused until needed |

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

Total about 91 files (the sum of the eight counts below, including the five test-project reference edits), far above the 15-file and 1,500-line limits of one worker PR (AGENTS.md §11). Proposed split into eight worker issues, each one pull request that builds alone with its own tests and carries the manifest entries and `PublicAPI.Unshipped.txt` lines of its own files (the orchestrator opens the sub-issues; this plan is the parent; counts include tests, manifest, PublicAPI, changelog fragment and project files, and are recounted by grep at the start of each PR):
- A new package needs a `ProjectReference` to `Encina.Compliance.EHDS` in each consolidated test project it is tested from (verified: `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.PropertyTests` and `Encina.IntegrationTests` each reference `Encina.Compliance.DPIA`); each edit is counted below in the first PR whose tests need that project.
- **PR 1a** (scaffold): csproj, two PublicAPI files, README, `Encina.slnx` entry (5); `EhdsDataCategories`, `EhdsPurposes`, `PermitStatus`, `AnonymisationRequirement`, `EhdsEnforcementMode` (5); constants unit test (1); manifest (1); `Encina.UnitTests.csproj` reference (1): 13 files.
- **PR 1b** (options, DI, range, logging): `EhdsOptions`, `EhdsOptionsValidator`, `ServiceCollectionExtensions`, `EventIdRanges.cs`, `EncinaEventIdAllocationTests.cs`, `EhdsLogMessages` (6); options and validator unit test, DI test, guard test (3); manifest, PublicAPI, changelog fragment (3); `Encina.GuardTests.csproj` reference (1): 13 files.
- **PR 2a** (aggregate and events): `HealthDataPermitAggregate`, `PermitEvents`, `EhdsErrors` (3); aggregate unit test (every transition), property test, errors unit test (3); manifest, PublicAPI (2); `Encina.PropertyTests.csproj`, `Encina.ContractTests.csproj` and `Encina.IntegrationTests.csproj` references (3): 11 files.
- **PR 2b** (hard prerequisite: PRs 1257-A1 and A2 of the #1257 plan merged, since the service calls `ResolveRead`/`ResolveWrite`; read model, projection, service, Marten extension): `PermitReadModel`, `PermitProjection`, `IEhdsPermitService`, `DefaultEhdsPermitService`, `EhdsMartenExtensions` (5); unit tests for projection and service, guard, contract, DI (5); Marten integration classes for the lifecycle, the projection with a real store and the two-tenant case (3); manifest, PublicAPI (2): 15 files.
- **PR 3** (enforcement): `EhdsSecondaryUseAttribute`, `EhdsSecondaryUsePermitBehavior`, `ServiceCollectionExtensions`, `EhdsErrors` (4); behavior unit, guard and DI tests (3); one end-to-end integration class (1); manifest, PublicAPI (2): 10 files.
- **PR 4** (expiry, health, diagnostics): `EhdsPermitExpirationService`, `EhdsHealthCheck`, `EhdsDiagnostics`, `ServiceCollectionExtensions` (4); unit tests for the three (3); a two-tenant sweep integration class (1); the load and benchmark justification `.md` files (2); manifest, PublicAPI (2): 12 files.
- **PR 5** (access-log adapter, after #1245): `IEhdsAccessLogProvider`, `EhdsAccessLogQuery`, `EhdsAccessLogEntry`, `ReadAuditEhdsAccessLogProvider`, `ServiceCollectionExtensions`, the csproj edit adding the `Encina.Security.Audit` reference (6); unit, guard and contract tests (3); manifest, PublicAPI (2): 11 files.
- **PR 6** (documentation): README, configuration page, package specification page, `docs/INVENTORY.md`, `ROADMAP.md`, changelog fragment: 6 files.

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
4. EhdsPermitExpirationService (PeriodicTimer, catch per iteration), EhdsHealthCheck, ComplianceEHDS = (6450, 6549)
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
| 3 | Structured Logging | ✅ | `EhdsLogMessages` in the new range 6450-6549 (Phase 4) |
| 4 | Health Checks | ✅ | `EhdsHealthCheck` for the module's own stores; MyHealth@EU connectivity stays deferred (issue file) |
| 5 | Validation | ✅ | Options validator; aggregate invariants on permit data (Phases 1-2) |
| 6 | Resilience | ⏭️ | Only relevant to cross-border exchange, which is deferred with MyHealth@EU in `artifacts/issues/plan-808-ehds-deferred-areas.md` |
| 7 | Distributed Locks | ❌ | Marten's optimistic concurrency serialises permit commands; the expiry service is idempotent per permit, so two instances cannot double-expire, and the non-winning instance treats the conflict or the already-expired permit as success-by-another-instance (no `Left`, no error log; ADR-050 multi-instance) |
| 8 | Transactions | ✅ | Each lifecycle command is one aggregate save |
| 9 | Idempotency | ✅ | A permit reference is unique per tenant: a duplicate application returns `ehds.permit_duplicate_reference` (Phase 2) |
| 10 | Multi-Tenancy | ✅ | `TenantId` on every event and projection and in the enforcement lookup, through `ITenantScopeResolver` of #1257 (single-tenant allowed, multi-tenant without a tenant denied, expiry sweep per tenant, composite unique index `(TenantId, PermitReference)`); a two-tenant integration test (SPEC-002 REQ-061, AC-043); the issue deferred it |
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
| #1257 tenant-scope primitive (`ITenantScopeResolver`, `TenantScope`) | Open (plan) | Hard prerequisite of PR 2b onward (PRs 1257-A1 and A2 merged): the permit service, the behavior and the expiry sweep resolve the tenant through it; PRs 1a to 2a do not need it. The plan is agnostic to the #1257 default-tenant Dilemma |
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

1. The maintainer decides Design Choices 1, 4, 5, 6, 9 and 10 when work on #808 starts; the orchestrator records them in a "Maintainer Decisions" section, updates each Chosen Option, then runs `adversarial-reviewer` on Choices 2, 3, 7 and 8.
2. Open the issue file above (`open-issue` skill) and link it here and from #808; decompose #808 into the eight worker issues named in the file-count section, since an issue of this size cannot be briefed whole (AGENTS.md §11), and correct the stale `post-1.0` label.
3. Link this plan from #808, then start PR 1 in a worktree (`worker-brief` skill) with the local CRAP table required in the brief.
4. Re-verify the EHDS dates, Art. 9 and Art. 50 against the Regulation text and the implementing acts within 30 days of any of their publication (SPEC-002 AC-025).
