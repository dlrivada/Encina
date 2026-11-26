# Implementation Plan: `Encina.Compliance.ENS` — Esquema Nacional de Seguridad (Real Decreto 311/2022)

> **Issue**: [#807](https://github.com/dlrivada/Encina/issues/807)
> **Type**: Feature (new package)
> **Complexity**: High (a 73-entry measure registry, two event-sourced aggregates, evaluators; 10 pull requests)
> **Estimated Scope**: 108 file touches and about 8,150 lines including tests and per-PR metadata; split into 10 pull requests, see "Pull request split"
> **Milestone**: v0.17.0 — Compliance Lifecycle. In the 1.0 scope since SPEC-000 DEC-009 (2026-10-23, REQ-030, which supersedes SPEC-002 DEC-015); the issue's `post-1.0` label and the issue comment "stays post-1.0" are stale
> **Provider Category**: None for databases (Marten on PostgreSQL, ADR-027; AGENTS.md section 5 excludes event sourcing from the database matrix)
> **Family**: `compliance-nis2-2`. ENS is Spain's regime for the public sector and its suppliers; it follows the conventions of the NIS2 family (#822 to #826) and reuses the evaluator idea of `Encina.Compliance.NIS2`.

---

## Summary

> **Legal values in this plan.** Checked on 2026-11-26 against the official BOE text of RD 311/2022 (legal-facts check of the plan batch): VERIFIED are the publication and entry-into-force dates, the five dimensions and three levels, the category rule of Annex I (plus its yearly reassessment or reassessment after a significant change), art. 2(3), the 73 measures of Annex II (4 `org`, 33 `op`, 36 `mp`) and the audit rule of art. 31 (ordinary audit at least every two years plus an extraordinary one after substantial changes, which restarts the two-year count). The split "Basic self-assessment, Medium and High formal audit" is art. 38(1) and Annex III sections 2.1 and 2.2, not art. 31. UNVERIFIED: the validity period (expiry) of a declaration or certificate of conformity, which the RD does not state (art. 38(2) only publishes them) and which sits in the Instruccion Tecnica de Seguridad de Conformidad con el ENS, not yet read. The applicability data shape of the measures is Design Choice 8.

ENS (RD 311/2022, BOE of 4 May 2022, in force 5 May 2022; Annex II has 73 measures; five security dimensions: confidentiality, integrity, traceability, authenticity, availability; applies to private suppliers of the public sector, art. 2(3), per the issue comment of 2026-09-23) requires a system to be categorised and its measures to be applied and audited. Encina has no ENS code today. It has the building blocks for evidence: `Encina.Security.Audit` (#395, closed) for the activity log of op.exp.8, `Encina.Security.ABAC` (#401, closed) for access control, and `Encina.Compliance.NIS2` for the evaluator pattern (`INIS2MeasureEvaluator`, `NIS2MeasureResult`).

**What it implements**:

- `ENSCategory` (`Basic`, `Medium`, `High`), `ENSDimension` and `ENSDimensionLevel` (`Low`, `Medium`, `High`) with a pure `ENSCategoryCalculator`: the category of a system is the highest level among its dimensions (Annex I).
- `ENSMeasureRegistry`: the 73 measures of Annex II as an embedded data file (id such as `op.exp.8`, framework `org`, `op` or `mp`, Spanish official name, English name, the dimensions it protects, applicability as Annex II states it: a measure is tied either to the system category or to one or more dimensions with a value per level (`aplica`, `n.a.`, plus reinforcements `+R1` to `+R5` chosen after risk analysis); the data shape is Design Choice 8).
- `ENSSystemAggregate` (event sourced): categorisation, declaration of applicability (which measures apply and why a measure is excluded) and per-measure implementation status; cloud provider records for op.nub.
- `ENSConformityAssessmentAggregate` (event sourced): self-assessment or audit (art. 31, art. 38(1) and Annex III), findings with CCN-STIC reference, report (art. 31(4)-(5): degree of compliance, conformities and non-conformities, methodology, scope), conformity declaration or certificate (art. 38(2)) and its expiry (the validity rule is UNVERIFIED, see the banner; it is a configurable value until verified).
- `IENSMeasureEvaluator` implementations that gather automatic evidence for the measures Encina can prove (op.exp.8 audit logging through the audit store, op.acc access control through the ABAC engine).
- Category completeness validation (`IENSComplianceValidator`): a system of category X must have every measure that applies at X implemented or justified.
- Services, inline projections, a review-due monitor, a health check and OpenTelemetry metrics, registered by `AddEncinaENS(...)`.

**Not in scope**: the CCN-STIC guide texts themselves, CCN's certification procedure, a security incident workflow (op.exp.7 is evidence-linked to the incident modules of #822 and #1942, not rebuilt), and transposition of NIS2 to the private sector.

**Affected packages**: `Encina.Compliance.ENS` (new) and `Encina` (EventId range registration).

**Provider category**: none. The 12-function check is in the matrix at the end.

### Corrections to the issue text (checked against `origin/main` 356c8cd62)

| Issue text | Today's code and verified facts | Plan decision |
|---|---|---|
| `config.UseENS(ens => ens.SystemCategory = ENSCategory.High ...)` | No `UseENS` and no `AddEncinaCompliance`; the entry point style is `AddEncina<Name>(Action<Options>)` | `AddEncinaENS(Action<ENSOptions>? configure = null)`; the category is computed from the dimensions, not configured (Design Choice 2) |
| "Security dimension assessment" and "Category determination" as two items | Annex I derives the category from the dimension levels | One calculator |
| "Full Annex II measure registry" | The comment of 2026-09-23 verifies 73 measures | Data file with exactly 73 entries, count asserted by a test |
| "CCN-STIC measure mapping" | CCN-STIC guides are documents, not data Encina can ship | `CcnStic` reference code per measure and per finding (a string), not the guide text |
| "Caching N/A", "Multi-tenancy deferred" | A per-system stream contract cannot add tenancy later without a migration | Tenant and module ids on every event from PR 4 (the first aggregate PR). The tenant comes from `ITenantScopeResolver` (#1257, PR 1257-A1; not yet in `src`) and the module from `IModuleExecutionContext.CurrentModule` (`src/Encina/Modules/Isolation/IModuleExecutionContext.cs`); `ITenantContext` and `IModuleContext` do not exist in `src` |
| "ENS logging per op.exp.8" as a new logging feature | `Encina.Security.Audit` already stores audit entries | An evaluator proves op.exp.8 by the presence and use of the audit store; no second log |

---

## Design Choices

<details>
<summary><strong>1. Package placement — new package</strong></summary>

- **Class**: Settled
- **Settled by**: issue #807 (Affected Packages "new package"; Alternatives Considered 1 and 2 reject a NIS2 mapping layer and an ISO 27001 profile because ENS has its own categorisation and measures).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.Compliance.ENS`** | Only Spanish public-sector suppliers take it | New csproj, README, manifest |
| B) Inside `Encina.Compliance.NIS2` | Reuses evaluators | Mixes an EU directive with a national regime |

### Chosen Option: **A**

### Rationale

- References: `Encina`, `Encina.DomainModeling`, `Encina.Marten`, `Encina.Security.Audit` and `Encina.Security.ABAC` (for the two evaluators); `InternalsVisibleTo` for the six test assemblies.

</details>

<details>
<summary><strong>2. Category — computed from the dimension levels</strong></summary>

- **Class**: Settled
- **Settled by**: RD 311/2022 Annex I (the category of a system is the highest level of its security dimensions).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ENSCategoryCalculator` derives the category; a declared category lower than the computed one is rejected (`Left`, `Validation`); a higher one is allowed and recorded** | Follows Annex I; fail closed | A system cannot simply be told "High" without levels |
| B) The configured `SystemCategory` of the issue | Simple | Ignores Annex I; allows under-categorisation |

### Chosen Option: **A**

### Rationale

- Only A satisfies Annex I. A missing dimension level returns `Left` and never defaults to `Low`.

</details>

<details>
<summary><strong>3. Measure registry — embedded data file, or C# tables</strong></summary>

- **Class**: Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Embedded JSON resource `ens-measures.json`, one measure per line (73 entries plus the array brackets, so the file stays about 80 lines whatever the number of fields per entry), loaded once into an immutable `ENSMeasureRegistry`; a test asserts 73 entries, unique ids and a complete applicability row** | Reviewable line by line against the BOE text; no logic mixed with data; the file is small enough to share a pull request with the loader | A loader and a resource entry in the csproj; long lines |
| B) A static C# table | No loader; compile-time checked | 73 rows of Spanish text in code; harder to review against the source |

### Chosen Option: **A (recommended, pending the maintainer)**

### Rationale

- B wins if the maintainer wants no resource loading in a library; A wins for reviewability against the legal source. The registry content is transcribed from Annex II of the BOE text in PR 3 and checked entry by entry; the 73 count is verified against the BOE text (4 `org`, 33 `op`, 36 `mp`). The data file is the thing under review: the registry test asserts 73 entries and the per-framework counts 4, 33 and 36, which checks the transcription is complete and consistent.

</details>

<details>
<summary><strong>4. Audit cadence and conformity kinds — configurable defaults, or constants</strong></summary>

- **Class**: Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ENSOptions` hold `AuditInterval` (default two years) and the rule "audit required for Medium and High"; the aggregate computes `NextAssessmentDueAtUtc` from `TimeProvider` instants** | Auditable, amendable by configuration | A configurable value can be set below what the regulation requires unless the validator rejects it |
| B) Constants in code | Simpler | An amendment needs a release |

### Chosen Option: **A (recommended, pending the maintainer)**

### Rationale

- Both options satisfy the legal rule (its values are the same either way), so this is not Settled by a citation. A wins when the rule may be amended or an entity wants a stricter cadence (the options validator rejects an interval longer than the regulation allows); B wins for the least moving parts.
- Legal values (checked 2026-11-26, see the banner): art. 31(1) sets the ordinary audit at least every two years and an extraordinary audit after substantial changes (which restarts the count; force majeure may extend the deadline by three months); the Basic self-assessment versus Medium and High formal audit split is art. 38(1) and Annex III sections 2.1 and 2.2, and Annex III sets no interval. The aggregate's rules cite the article that holds each rule. The validity period of a declaration or certificate is UNVERIFIED (ITS de Conformidad not read), so its default is not fixed by this plan.

</details>

<details>
<summary><strong>5. Personal data in events</strong></summary>

- **Class**: Dilemma (follows the outcome of the personal-data Dilemma of the #822 plan)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Events hold ids, enums, dates and codes; the four ENS roles (information owner, service owner, security officer, system owner) are person ids, never names** | Nothing personal in the stream | The application keeps the people registry |
| B) Names and contacts in events | Self-contained | Personal data in an immutable stream |

### Chosen Option: **A (recommended, pending the maintainer)**

### Rationale

- A wins on GDPR Art. 5(1)(c) data minimisation; B wins only for an entity that wants a self-contained stream and accepts crypto-shredding. The choice follows the outcome the maintainer gives to the personal-data Dilemma of #822, whichever it is; if that outcome is crypto-shredding, this package follows it in a later change. No other design choice of this plan rests on this one.

</details>

<details>
<summary><strong>6. Evidence evaluators — automatic where Encina can prove, manual otherwise</strong></summary>

- **Class**: Trivial
- **Settled by**: the evaluator pattern of `Encina.Compliance.NIS2` (`INIS2MeasureEvaluator` and `Evaluators/`), and AGENTS.md section 3 (a gate fails closed).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IENSMeasureEvaluator` returns `Satisfied`, `NotSatisfied` or `NotEvaluable`; two implementations (op.exp.8 through `IOperationAuditStore` presence and a recent entry, op.acc through the ABAC engine registration); every other measure stays manual and a `NotEvaluable` result never counts as satisfied** | Honest about what Encina can prove | Most measures need a human attestation |
| B) Claim automatic evidence for all measures | More automation | Unfounded compliance claims |

### Chosen Option: **A**

### Rationale

- Evaluators are resolved optionally; a missing store is `NotSatisfied`, never silent success.

</details>

<details>
<summary><strong>7. Registry language</strong></summary>

- **Class**: Trivial
- **Settled by**: AGENTS.md section 10 (code and documentation in English) and the public-docs rule that ENS content states Spain as its scope.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each measure holds `Id` (`op.exp.8`), `NameEs` (official BOE name, a legal term kept in Spanish) and `Name` (English)** | Traceable to the law; readable in English | Two strings per entry |
| B) English only | Simple | Loses the legal reference |

### Chosen Option: **A**

### Rationale

- Spanish appears only as the quoted official name of the measure.

</details>

<details>
<summary><strong>8. Measure applicability data shape — a basis per measure, or flat rows per dimension and level</strong></summary>

- **Class**: Dilemma

Annex II (section 2, selection table) ties a measure either to the system category (for example org.1) or to one or more dimensions with a value per level (for example op.exp.8 depends on dimension T, op.acc.1 on T and A); the values are `aplica`, `n.a.` and reinforcements `+R1` to `+R5` that are chosen after risk analysis. A row of "applicability per category" cannot represent op.exp.8 or op.acc.1 (legal-facts check, 2026-11-26).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each `ENSMeasure` has a `Basis` (`Category` or `Dimensions`); a category-based measure holds the applicability for Basic, Medium and High; a dimension-based measure holds, per protected dimension, the value for Low, Medium and High and the ids of the reinforcements it offers; the system's selected reinforcements are recorded on the system aggregate** | Mirrors the shape of the legal table; one line per measure in the data file | Two shapes in one type; the completeness validator evaluates a dimension-based measure against the level of each of its dimensions in the system |
| B) Flat rows `(measure, dimension or category, level, value, reinforcements)` | One uniform row type | About 150 or more rows instead of 73; the "73 measures" test needs a grouping step; harder to review against the table |

### Chosen Option: **A (recommended, pending the maintainer)**

### Rationale

- B wins if the maintainer prefers a uniform row type and accepts a longer file; A wins on reviewability against the BOE table and on the 73-entry file. Under both options the category is still computed from the dimension levels (Design Choice 2), the registry is immutable and a missing level returns `Left`; the choice is made before PR 3 fixes the data shape. The old "applies reinforced" flag is replaced by explicit reinforcement ids in both.

</details>

---

## Implementation Phases

### Pull request split (read first)

Each PR builds and passes alone, is at most 15 files and 1,500 lines, and counts tests, metadata, README, changelog fragment and docs. Test project edits are `ProjectReference` additions (UnitTests in PR 1, PropertyTests and GuardTests in PR 2, ContractTests and IntegrationTests in PR 6). The data file `ens-measures.json` is written one measure per line, so it is about 80 lines whatever the shape of Design Choice 8 (a pretty-printed file of 73 entries would be 900 to 1,100 lines and is not used).

| PR | Phases | Files (production, metadata) | Test files | Files / lines (approx.) |
|---|---|---|---|---|
| 1 | 1 | `EventIdRanges.cs`, `eventid-block-registry.md` row, `Encina` `PublicAPI.Unshipped.txt`, `Encina.json` manifest, `Encina.slnx`, ENS csproj, ENS `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`, ENS manifest, `ENSErrors`, `ENSOptions` (11) | `AssemblyRanges` entry, UnitTests csproj reference, unit options test, unit errors test (one `GetCategory` case per code) (4) | 15 files / 700 |
| 2 | 2 | `ENSCategory`, `ENSDimension`, `ENSDimensionLevel`, `ENSCategoryCalculator`, `PublicAPI.Unshipped.txt`, manifest (6) | unit calculator, property calculator, guard calculator, PropertyTests and GuardTests csproj references (5) | 11 files / 650 |
| 3 | 2 (registry) | `ENSMeasureApplicability`, `ENSMeasure`, `ENSMeasureRegistry`, `ens-measures.json` (one measure per line, about 80 lines), csproj edit (embedded resource), `PublicAPI.Unshipped.txt`, manifest (7) | unit registry (73 entries, 4/33/36 per framework, unique ids, complete applicability), guard registry (2) | 9 files / 850 |
| 4 | 3 | `ENSSystemEvents`, `ENSSystemAggregate`, status and justification models, `PublicAPI.Unshipped.txt`, manifest (5) | unit aggregate, guard, property (3) | 8 files / 1,000 |
| 5 | 4 | `ENSConformityAssessmentEvents`, `ENSConformityAssessmentAggregate`, finding models, `PublicAPI.Unshipped.txt`, manifest (6) | unit aggregate, guard, property (3) | 9 files / 1,000 |
| 6 | 5 (system side) | `IENSSystemService`, `DefaultENSSystemService`, `ENSSystemReadModel`, `ENSSystemProjection`, `ENSMartenExtensions` (created here), `PublicAPI.Unshipped.txt`, manifest (7) | unit service, unit projection, guard service, contract `IENSSystemService`, integration Marten, ContractTests and IntegrationTests csproj references (7) | 14 files / 1,000 |
| 7 | 5 (assessment side) | `IENSAssessmentService`, `DefaultENSAssessmentService`, `ENSAssessmentReadModel`, `ENSAssessmentProjection`, `ENSMartenExtensions` edit, `PublicAPI.Unshipped.txt`, manifest (7) | unit service, unit projection, guard service, contract `IENSAssessmentService`, integration Marten (5) | 12 files / 900 |
| 8 | 6 | `IENSMeasureEvaluator`, `OperationLogEvaluator`, `AccessControlEvaluator`, `IENSComplianceValidator`, `DefaultENSComplianceValidator`, `PublicAPI.Unshipped.txt`, manifest (7) | unit evaluators, unit validator, guard (3) | 10 files / 1,000 |
| 9 | 7 (registration, monitor, diagnostics) | `ServiceCollectionExtensions` (`AddEncinaENS`), `ENSOptionsValidator`, `ENSReviewMonitorService`, `ENSDiagnostics`, `ENSLogMessages`, `PublicAPI.Unshipped.txt`, manifest (7) | DI test, unit monitor, unit diagnostics, guard validator and monitor (4) | 11 files / 900 |
| 10 | 7 (health check), 8 | `ENSComplianceHealthCheck`, `ServiceCollectionExtensions` edit, manifest, changelog fragment, package README, `docs/features/ens.md`, `docs/INVENTORY.md` (7) | unit health check, DI test edit (2) | 9 files / 500 |

PR 6 carries the system service, read model, projection and the Marten registration; PR 7 the assessment side; PR 5 carries the assessment aggregate only. The sum is 15 + 11 + 9 + 8 + 9 + 14 + 12 + 10 + 11 + 9 = 108 file touches.

### Phase 1: Package Skeleton, EventIds & Errors (PR 1)

<details>
<summary>Tasks</summary>

1. `EventIdRanges.cs`: `ComplianceENS = (53000, 53049)` registered first (ADR-021), registry row, `Encina` PublicAPI line, `AssemblyRanges` entry for `Encina.Compliance.ENS`.
2. csproj (references of Design Choice 1), `Encina.slnx` entry, PublicAPI files.
3. `ENSErrors` with ADR-062 categories (rule in brackets): `SystemNotFound` `NotFound` (R1: a runtime entity addressed by the system id the caller supplies), `AssessmentNotFound` `NotFound` (R1, same reason), `CategoryBelowComputed` `Validation` (R3), `DimensionLevelMissing` `Validation` (R3), `MeasureUnknown` `Validation` (R3: the caller supplies a measure id that is not in the legal catalogue, a field rule; R1 excludes `NotFound` because a catalogue entry is static configuration data, not a runtime entity, and `Internal` would blame the server for a caller's value), `InvalidTransition` `Conflict` (R2: an invalid transition is `Conflict`; `FailedPrecondition` is not used before #3420), `EventStoreFailed` `Internal` (R4: a store failure, never `UpstreamFailure`). There is no own tenant-missing code: the `Left` of `ITenantScopeResolver` (`encina.tenancy.tenant_required`, `InvalidRequest`, R3) is returned unchanged.
4. `ENSOptions` (`AuditInterval`, `EnableReviewMonitor` default false, monitor interval).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
You are implementing Phase 1 of Encina.Compliance.ENS (Issue #807).

CONTEXT:
- New package; mirror src/Encina.Compliance.NIS2/Encina.Compliance.NIS2.csproj. EventId range 53000-53049.

TASK:
1. Register the range first; create the csproj, slnx entry, PublicAPI files, errors and options.

KEY RULES:
- ErrorCategory per ADR-062; Either<EncinaError, T>; XML docs; zero warnings.

REFERENCE FILES: src/Encina.Compliance.NIS2/NIS2Errors.cs, src/Encina.Compliance.NIS2/NIS2Options.cs
```

</details>

### Phase 2: Categorisation (PR 2) & Measure Registry (PR 3)

<details>
<summary>Tasks</summary>

1. PR 2: `Model/ENSCategory.cs`, `ENSDimension.cs`, `ENSDimensionLevel.cs`. PR 3: `Registry/ENSMeasureApplicability.cs` (`NotApplicable`, `Applies`, with reinforcement ids `R1` to `R5` as data, not an enum value; shape per Design Choice 8).
2. PR 2: `Services/ENSCategoryCalculator.cs` (pure): highest level among the five dimensions (Annex I); a missing dimension returns `Left`.
3. PR 3: `Registry/ENSMeasure.cs` (`Basis` and the applicability data of Design Choice 8), `ENSMeasureRegistry.cs` (loads the embedded resource once; `Get(id)`, `For(system levels)`), `Registry/ens-measures.json` with the 73 measures of Annex II (4 `org`, 33 `op`, 36 `mp`), one measure per line, csproj `EmbeddedResource` entry. The registry test asserts the 73 total and the 4/33/36 split.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
You are implementing Phase 2 of Encina.Compliance.ENS (Issue #807).

CONTEXT:
- Phase 1 merged. Annex I defines the category as the highest dimension level; Annex II lists the measures.

TASK:
1. (PR 2) Create the category types and the pure calculator.
2. (PR 3) Create the measure model, the registry loader and the one-line-per-measure data file transcribed entry by entry from Annex II of RD 311/2022, in the shape the maintainer chose for Design Choice 8.

KEY RULES:
- A test asserts 73 entries (4 org, 33 op, 36 mp), unique ids and a complete applicability value for every category or dimension level the measure's basis requires; the registry is immutable; no Spanish text outside NameEs.

REFERENCE FILES: src/Encina.Compliance.NIS2/Model/NIS2Measure.cs
```

</details>

### Phase 3: ENS System Aggregate (PR 4)

<details>
<summary>Tasks</summary>

1. `Model/ENSMeasureStatus.cs` (`NotStarted`, `InProgress`, `Implemented`, `Justified` with a reason code), `ENSCloudProviderRecord`.
2. `Events/ENSSystemEvents.cs` and `Aggregates/ENSSystemAggregate.cs` (`AggregateBase`; static `Register(...)` returning `Either`; `Categorise(levels)`, `DeclareApplicability`, `SetMeasureStatus`, `RecordCloudProvider`, `Retire`); every event carries `TenantId`, `ModuleId`; a cloud provider record below the system category returns `Left`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
You are implementing Phase 3 of Encina.Compliance.ENS (Issue #807).

CONTEXT:
- Phases 1 and 2 merged. Pattern: src/Encina.Compliance.BreachNotification/Aggregates/BreachAggregate.cs; errors per ADR-053.

TASK:
1. Create the status models, events and ENSSystemAggregate.

KEY RULES:
- Illegal transitions return Left with a rule code and raise no event; role holders are person ids only; time from passed-in instants.

REFERENCE FILES: src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs
```

</details>

### Phase 4: Conformity Assessment Aggregate (PR 5)

<details>
<summary>Tasks</summary>

1. `Model/ENSAssessmentKind.cs` (`SelfAssessment`, `Audit`), `ENSAssessmentStatus.cs`, `ENSFinding.cs` (measure id, severity, `CcnStic` reference code, status), `ENSConformityOutcome.cs`.
2. Events and `ENSConformityAssessmentAggregate`: `Plan`, `Start`, `RecordFinding`, `ResolveFinding`, `IssueReport`, `DeclareConformity` or `RecordCertificate`, `Expire`; an audit is required for Medium and High (Design Choice 4; art. 38(1), Annex III), and an illegal transition returns `Left` with `ENSErrors.InvalidTransition` (`Conflict`). `Expire` follows a validity period that is a configurable value until the ITS de Conformidad is read (UNVERIFIED).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
You are implementing Phase 4 of Encina.Compliance.ENS (Issue #807).

CONTEXT:
- Same conventions as Phase 3; the audit rule comes from ENSOptions.

TASK:
1. Create the assessment models, events and aggregate.

KEY RULES:
- Findings never hold free text; a conformity declaration with an open critical finding returns Left.

REFERENCE FILES: src/Encina.Compliance.DPIA/Aggregates/DPIAAggregate.cs
```

</details>

### Phase 5: Services, Read Models & Projections (PR 6: system side; PR 7: assessment side)

<details>
<summary>Tasks</summary>

1. `IENSSystemService` and `IENSAssessmentService` with internal default services taking `IAggregateRepository<T>`, `ITenantScopeResolver` (#1257; required), `TimeProvider`, `ILogger` and, last, `IModuleExecutionContext? moduleContext = null` (optional parameters last and written with `= null`, CS1737 and `ValidateOnBuild`). Commands call `ResolveWrite(null)` and queries `ResolveRead(null)`; a `Left` (`tenant_required`, `InvalidRequest`) is returned unchanged and nothing is appended or read (fail closed). The module id stamped on events is `moduleContext?.CurrentModule` (null when no module context is active).
2. PR 6: `ENSSystemReadModel` (category, measure status counts, `NextAssessmentDueAtUtc`) and its idempotent inline projection (constructor-injected). PR 7: `ENSAssessmentReadModel` and its projection.
3. `ENSMartenExtensions` (`AddENSEventSourcing()`), created in PR 6 with the system aggregate and projection and extended in PR 7.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
You are implementing Phase 5 of Encina.Compliance.ENS (Issue #807).

CONTEXT:
- Aggregates exist (Phases 3 and 4). Pattern: src/Encina.Compliance.BreachNotification/Services/DefaultBreachNotificationService.cs.

TASK:
1. Create the services, read models, projections and Marten registration (system side in PR 6, assessment side in PR 7).

KEY RULES:
- ITenantScopeResolver first, its Left returned unchanged (missing tenant denies); a Left from a store fails the operation; integration tests use Marten on PostgreSQL via Testcontainers (no InMemory store).

REFERENCE FILES: src/Encina.Marten/Projections/IReadModelRepository.cs
```

</details>

### Phase 6: Evidence Evaluators & Completeness Validation (PR 8)

<details>
<summary>Tasks</summary>

1. `IENSMeasureEvaluator` (`MeasureId`, `EvaluateAsync`) returning `Satisfied`, `NotSatisfied` or `NotEvaluable`; `OperationLogEvaluator` (op.exp.8) and `AccessControlEvaluator` (op.acc) resolving their dependencies optionally.
2. `IENSComplianceValidator` and `DefaultENSComplianceValidator`: for a system, every measure that applies at its category (category-based measures) or at the level of each of its dimensions (dimension-based measures, Design Choice 8) is `Implemented` or `Justified`; the result lists the gaps and a `NotEvaluable` evaluator never counts as satisfied.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
You are implementing Phase 6 of Encina.Compliance.ENS (Issue #807).

CONTEXT:
- Mirror src/Encina.Compliance.NIS2/Evaluators/ and INIS2MeasureEvaluator; the registry comes from Phase 2.

TASK:
1. Create the evaluator port, the two evaluators, and the completeness validator.

KEY RULES:
- Fail closed: a missing store or engine is NotSatisfied; never claim automatic evidence beyond what the code proves; singleton evaluators take no scoped dependency.

REFERENCE FILES: src/Encina.Compliance.NIS2/Evaluators/RiskAnalysisEvaluator.cs, src/Encina.Compliance.NIS2/Abstractions/INIS2MeasureEvaluator.cs
```

</details>

### Phase 7: Configuration, DI, Cross-Cutting Integration & Observability (PR 9: registration, monitor, diagnostics; PR 10: health check and documentation)

<details>
<summary>Tasks</summary>

1. `AddEncinaENS(...)`: options, validator, registry (singleton), services, evaluators, validator, `AddENSEventSourcing()`, `TryAdd*`; DI test with `ValidateOnBuild` and `ValidateScopes` (it registers `ITenantScopeResolver` through #1257's registration method).
2. `ENSReviewMonitorService` (opt-in `BackgroundService`, `PeriodicTimer` on `TimeProvider`, per-iteration catch; optional `IDistributedLockProvider` lock per iteration) escalating overdue assessments.
3. `ENSDiagnostics` (meter and `ActivitySource` `Encina.Compliance.ENS`; instruments `ens.system.*`, `ens.assessment.*`, `ens.measure.*`; no `_total`; tenant tag with multi-tenancy), `ENSLogMessages` ids 53000-53029 packed from 53000 (53030-53049 free) in PR 9; health check `ENSComplianceHealthCheck` (overdue assessment gives `Degraded`) in PR 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
You are implementing Phase 7 of Encina.Compliance.ENS (Issue #807).

CONTEXT:
- Components from Phases 2 to 6 exist; range 53000-53049 registered (Phase 1).

TASK:
1. Add registration, the DI test, the opt-in monitor, diagnostics, log messages and the health check.

KEY RULES:
- Register every option type and dependency; never inject a scoped service into a singleton; log error codes only, never EncinaError.Message or raw ids.

REFERENCE FILES: src/Encina.Compliance.NIS2/ServiceCollectionExtensions.cs, src/Encina.Compliance.BreachNotification/BreachDeadlineMonitorService.cs
```

</details>

### Phase 8: Testing & Documentation (PRs 1 to 10)

<details>
<summary>Tasks</summary>

1. Unit, guard (public constructors and methods; nullable parameters that accept null get a valid-result test), contract (`IENSSystemService`, `IENSMeasureEvaluator`), property (category equals the maximum level for any dimension levels; applicability monotone from Basic to High), integration (Marten on PostgreSQL, rebuild from history).
2. Per-file targets (numeric, required by AGENTS.md section 9; no measured figure is typed here, and any baseline is quoted only as "measured on <date> with <command>"; each target is justified in one sentence in the manifest): `ENSCategoryCalculator` unit 95 (a pure function, every branch a case), property 85, guard 85 (null and missing-level arguments); `ENSMeasureRegistry` unit 95, guard 85 (the loader is a pure function of the embedded file); aggregates unit 90, guard 80 (every transition and illegal transition is a unit case); services unit 90, guard 85, contract 70 (the `Left` paths of the public interfaces); projections unit 90, integration 80 (the Marten run covers registration); evaluators unit 90, guard 85; validator unit 90; options validator unit 95, guard 90; monitor unit 85, guard 80 (constructor arguments); health check unit 90. Load and benchmark: `.md` justification (low-throughput compliance operations).
3. Docs: XML docs, `changelog.d/807-ens-compliance.added.md`, package README, `docs/features/ens.md` (by `docs-writer`), `docs/INVENTORY.md`; the ADR number is assigned when the ADR pull request opens.
4. CRAP of every changed method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
You are implementing the tests and documentation of Encina.Compliance.ENS (Issue #807).

CONTEXT:
- Manifest .github/coverage-manifest/Encina.Compliance.ENS.json; production code of the PR exists.

TASK:
1. Add the tests per PR and report per-flag coverage against the manifest; documentation goes to the docs-writer.

KEY RULES:
- Tests execute real code; Shouldly via Encina.Testing.Shouldly; FakeTimeProvider; no Thread.Sleep; fragments and PublicAPI lines to the mechanical-fixer.

REFERENCE FILES: tests/Encina.UnitTests/Compliance, tests/Encina.GuardTests
```

</details>

---

## Research

### Regulatory References

| Source | Requirement | Applies in this plan |
| :--- | :--- | :--- |
| RD 311/2022 Annex I | Categories and dimensions | `ENSCategoryCalculator` |
| RD 311/2022 Annex II | The 73 security measures (`org`, `op`, `mp`) | `ENSMeasureRegistry` |
| RD 311/2022 art. 31, art. 38, Annex III | Art. 31: ordinary audit at least every two years, extraordinary after substantial changes; art. 38(1) and Annex III sections 2.1 and 2.2: Basic self-assessment, Medium and High formal audit; art. 38(2): declarations and certifications are published | `ENSConformityAssessmentAggregate` |
| Measure `op.nub` | Cloud services | `ENSCloudProviderRecord` |
| Measures `op.exp.8`, `op.mon`, `op.exp.7` | Activity log, monitoring, incident management | Evaluator for `op.exp.8`; `op.exp.7` linked, not rebuilt |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| `INIS2MeasureEvaluator`, `Evaluators/` | `src/Encina.Compliance.NIS2` | Evaluator pattern |
| `IOperationAuditStore` / audit store | `src/Encina.Security.Audit` (#395) | op.exp.8 evidence |
| ABAC engine | `src/Encina.Security.ABAC` (#401) | op.acc evidence |
| `BreachAggregate`, `BreachProjection`, `BreachDeadlineMonitorService` | `src/Encina.Compliance.BreachNotification` | Patterns |
| `IReadModelRepository<T>` | `src/Encina.Marten/Projections/IReadModelRepository.cs` | Read models |
| `ErrorCategory` | `src/Encina/Errors/ErrorCategory.cs` (ADR-062) | Error categories |

### Event ID Allocation

| Package | Range | Notes |
|---|---|---|
| `Encina.Compliance.ENS` | `53000-53049` (`ComplianceENS`, new, Security and compliance zone) | Phase 7 uses 53000-53029; 53030-53049 free. After the claims known on 2026-11-25 (up to 52749, 52750-52799 for #827, 52800-52899 for #804, 52900-52949 for #805, 52950-52999 for #806); verified free on 2026-11-26 against `eventid-block-registry.md`, `EventIdRanges.cs`, every plan on `main` and the open plan heads (#4381, #4383 and #4385 claim 52800-52999; none touches 53000-53049) |

### Estimated File Count

| Category | Files | Notes |
|---|---|---|
| Production | 40 | Models, registry and data file, aggregates, services, projections, evaluators, diagnostics, health check |
| Metadata (csproj, slnx, PublicAPI, manifests, registry row, README, fragment) | 28 | Counted per PR (PublicAPI and manifest recur) |
| Tests | 38 | Unit, guard, contract, property, integration, project references |
| Documentation (feature page, INVENTORY) | 2 | |
| Total | 108 | Matches the split: 15 + 11 + 9 + 8 + 9 + 14 + 12 + 10 + 11 + 9 |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a .NET 10 / C# 14 pre-1.0 library with ROP. Issue #807 adds Encina.Compliance.ENS (RD 311/2022): category calculator, 73-measure registry, two Marten event-sourced aggregates (system, conformity assessment), evidence evaluators and a completeness validator.

IMPLEMENTATION OVERVIEW: ten pull requests (skeleton, categorisation, registry, system aggregate, assessment aggregate, system service and projection, assessment service and projection, evaluators and validator, DI and observability, health check and documentation).

KEY PATTERNS: category computed from dimension levels (fail closed on a missing level); NotEvaluable never counts as satisfied; tenant and module ids on every event; missing tenant denies; no personal data in events; no _total counters (ADR-054); ErrorCategory per ADR-062; no InMemory store (Marten and PostgreSQL via Testcontainers); optional constructor parameters last; opt-in monitor.

REFERENCE FILES: src/Encina.Compliance.NIS2; src/Encina.Compliance.BreachNotification; docs/plans/dsr-implementation-plan-404.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Assessment and measure status must be read from the stream or the inline projection; the registry is an immutable in-memory object, not a cache |
| 2 | OpenTelemetry | ✅ | `ens.*` instruments and activities, tenant tag with multi-tenancy (ADR-054 D4) |
| 3 | Structured Logging | ✅ | Ids 53000-53029 |
| 4 | Health Checks | ✅ | Overdue assessments give `Degraded` |
| 5 | Validation | ✅ | Category completeness, dimension levels and options validation return `Left` |
| 6 | Resilience | ❌ | No external call in the package |
| 7 | Distributed Locks | ✅ | The opt-in monitor takes an optional `IDistributedLockProvider` lock per iteration when one is registered |
| 8 | Transactions | ❌ | Each command appends to one stream; the repository commit is the owner |
| 9 | Idempotency | ❌ | Commands carry the aggregate's expected version; a duplicate transition is an invalid transition (`Left`) |
| 10 | Multi-Tenancy | ✅ | `TenantId` on every event and read model, from `ITenantScopeResolver` (#1257); its `Left` is returned unchanged, so a missing tenant denies |
| 11 | Module Isolation | ✅ | `ModuleId` on every event and read model, from `IModuleExecutionContext.CurrentModule` (null when no module is active) |
| 12 | Audit Trail | ✅ | The event stream is the immutable audit trail of categorisation and certification; op.exp.8 evidence reads the audit store |

---

## Prerequisites & Dependencies

- #822 (open, plan on main): event-sourcing conventions and the personal-data outcome. #2668 (open) builds the shared rule-code catalogue; this plan adds its codes to `ENSErrors`.
- #1257 PR 1257-A1 (open, plan on main): `ITenantScopeResolver` and `TenantScope`, a hard prerequisite of PR 6 and PR 7; it does not exist in `src` today.
- #395 and #401 (closed) are referenced packages; #414 (closed) is context; #1186 (open EPIC, SPEC-002) is context.

## Next Steps

1. Maintainer answers Dilemmas 3, 4, 5 and 8 when work on #807 starts; each is marked "recommended, pending the maintainer". Dilemma 8 (data shape) is answered before PR 3.
2. Brief PR 1 after #822 PR 1 has merged; PR 2 to PR 10 follow in order.
3. Run `check-plan.ps1` without `-Draft` after the adversarial review sets the `Verified` lines; the remaining UNVERIFIED value is the validity period of a conformity declaration or certificate (ITS de Conformidad).
