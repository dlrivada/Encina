# Implementation Plan: Art. 9(2) Conditions and National Legal Bases next to the Art. 6 Basis — `Encina.Compliance.GDPR` and `Encina.Compliance.LawfulBasis`

> **Issue**: [#1196](https://github.com/dlrivada/Encina/issues/1196) (SPEC-002 P-08, REQ-010, AC-010, scenario S18; parent [#1186](https://github.com/dlrivada/Encina/issues/1186))
> **Type**: Feature
> **Complexity**: Medium-High (11 phases; a persisted shape change on the 10 database providers and on the Marten event stream)
> **Estimated Scope**: ~1,600-2,200 lines of production code + ~2,000-2,600 lines of tests; ~10 new and ~45 modified production files

---

## Summary

A health practice needs two legal grounds for every activity that touches health data: an Article 6(1) basis **and** an Article 9(2) condition. When the condition rests on Union or Member State law, it also needs a reference to that law, for example LOPDGDD art. 9.2 and its 17th additional provision (DA 17ª) in Spain. Today Encina can record only the first of the three. This plan adds the other two to processing activities, the Article 30 record (RoPA) export and lawful-basis registrations. It also makes an activity that declares special-category data without an Article 9(2) condition fail validation (AC-010).

### What exists today (verified on `main` at `5b485b12`)

Nothing of REQ-010 is implemented. A search of `src/` for `SpecialCategory`, `NationalLegalBasis` and `Art. 9` finds only DPIA and AI Act risk heuristics, no legal-ground model.

| Area | Current state | Evidence |
|------|---------------|----------|
| Art. 6 enum | Six values only, no Art. 9(2) concept | `src/Encina.Compliance.GDPR/Model/LawfulBasis.cs:17-57` |
| Processing activity | One `LawfulBasis`; special categories only as free text in `CategoriesOfPersonalData`; no tenant | `src/Encina.Compliance.GDPR/Model/ProcessingActivity.cs:39`, `:51` |
| Persistence | 14 columns, unique on `RequestTypeName`, on all 10 providers | `src/Encina.Compliance.GDPR/ProcessingActivityEntity.cs:21-92`; `ProcessingActivityMapper.cs:34-93`; `src/Encina.ADO.SqlServer/Scripts/011_CreateProcessingActivitiesTable.sql:19`; `src/Encina.EntityFrameworkCore/ProcessingActivity/ProcessingActivityEntityConfiguration.cs:71-72`; `src/Encina.MongoDB/ProcessingActivity/ProcessingActivityRegistryMongoDB.cs:48-52` |
| RoPA export | JSON and CSV carry Art. 6 only | `src/Encina.Compliance.GDPR/Export/JsonRoPAExporter.cs:99-115`; `CsvRoPAExporter.cs:31-47`, `:111-132` |
| RoPA export telemetry | `GDPR.RoPA.Export` activity, three instruments and EventIds 8109-8111 exist but **no exporter calls them** | `src/Encina.Compliance.GDPR/Diagnostics/GDPRDiagnostics.cs:41-50`, `:74-98`; `GDPRLogMessages.cs:119-149` |
| Lawful-basis aggregate | `LegalReference` is one free-text string; `ChangeBasis` refuses an unchanged Art. 6 basis, so a condition cannot be changed through it | `src/Encina.Compliance.LawfulBasis/Aggregates/LawfulBasisAggregate.cs:54`, `:155-159` |
| Lawful-basis events and read model | No Art. 9(2) field | `Events/LawfulBasisEvents.cs:34-44`, `:71-81`; `ReadModels/LawfulBasisReadModel.cs:27-108`; `ReadModels/LawfulBasisProjection.cs:48-110` |
| Lawful-basis queries | Filter neither by `TenantId` nor scope the cache key by tenant | `Services/DefaultLawfulBasisService.cs:479`, `:490-492`, `:521-523` |
| Lawful-basis auto-registration | Ignores the `Either` from `RegisterAsync` and counts failures as registered; registers a new stream on every start | `AutoRegistration/LawfulBasisAutoRegistrationHostedService.cs:63-73`, `:83-93` |
| Lawful-basis registration metrics | `registrations.created`, `.revoked`, `.basis_changed` and the LIA counters are declared but never incremented | `Diagnostics/LawfulBasisDiagnostics.cs:76-119` (only `ValidationsTotal`, `ConsentChecksTotal`, `LiaChecksTotal` are used) |

### Standards covered

GDPR Art. 6(1), 6(3), 9(1), 9(2)(a)-(j), 9(3), 9(4) and 30(1)(b)-(c); LOPDGDD (LO 3/2018) art. 9.2 and DA 17ª as the Spanish acceptance case (SPEC-002 §11, "Spanish law is the acceptance case, not hard-coded behaviour"); the Art. 9(2) points that the draft Digital Omnibus COM(2025) 837 proposes, built off by default (SPEC-002 REQ-024, DEC-012, #815).

### Affected packages

- `Encina.Compliance.GDPR`: model, attributes, validator, mapper, entity, in-memory registry, RoPA exporters, diagnostics.
- `Encina.Compliance.LawfulBasis`: aggregate, events, read model, projection, service, attribute resolution, auto-registration, pipeline, diagnostics.
- The 10 `IProcessingActivityRegistry` providers (`Encina.ADO.*`, `Encina.Dapper.*`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`), their scripts, and the test schemas in `tests/Encina.TestInfrastructure/Schemas/`.

### Provider category

**Database (10)** for `IProcessingActivityRegistry`, because the issue changes the persisted processing-activity shape that all 10 providers store (#681 implemented them). The issue's "Provider Implementation Matrix: not applicable" covers only `Encina.Compliance.LawfulBasis`, which is Marten-only under SPEC-002 DEC-008 (a) and ADR-019; that part is **Event sourcing (Marten)**.

---

## Design Choices

<details>
<summary><strong>1. Domain model of the Art. 9 ground — a <code>SpecialCategoryProcessing</code> value next to the Art. 6 basis</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Flat optional fields** (`SpecialCategoryCondition?`, `bool ProcessesSpecialCategoryData`) on each record | Simplest mapping; one column per field | The invalid state "special categories, no condition" is representable everywhere and must be rejected at every write path; two fields that must agree |
| **B) Composite value `SpecialCategoryProcessing`** (Art. 9(1) categories, Art. 9(2) condition), created through a factory that returns `Either<EncinaError, SpecialCategoryProcessing>`; `null` means no special-category data | The invalid state cannot be built; one concept reused by `ProcessingActivity`, the lawful-basis aggregate, events, read model and exports; matches ROP | One more type; persistence and attributes must flatten it |
| **C) Infer special categories from the free-text `CategoriesOfPersonalData`**, as `SpecialCategoryDataCriterion` does in DPIA | No new declaration for users | String matching ("health", "biometric") is not a legal declaration; misses "salud mental", false positives; AC-010 validation would depend on wording |

### Chosen Option: **B — composite `SpecialCategoryProcessing` value** (recommended, pending the maintainer)

### Rationale

- We recommend B because AC-010 asks that "a special-category activity without an Art. 9(2) condition fails validation"; with B the factory is that validation, and every consumer receives only valid values.
- The Art. 9(1) list becomes an enum `SpecialCategoryOfPersonalData` (eight values), not free text, so the RoPA states exactly which special categories it covers.
- `SpecialCategoryCondition` is the issue's enum for Art. 9(2)(a)-(j). Value `0` is `NotDeclared`, so attributes, which cannot hold nullable enums, can leave it unset. The factory rejects `NotDeclared` together with a non-empty category list.
- Option C stays in DPIA, where it is a risk heuristic, not a legal record.

</details>

<details>
<summary><strong>2. National legal basis — one structured <code>LegalBasisReference</code> list replacing the free-text <code>LegalReference</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add `NationalLegalBasisReference` next to the existing `LegalReference` string** | Smallest change to the lawful-basis aggregate | Two places to record "which law": the old string for Art. 6(3) and the new value for Art. 9(2); exports and validators must merge them |
| **B) Replace `LegalReference` with `IReadOnlyList<LegalBasisReference>`** (`Kind`, `Jurisdiction`, `Instrument`, `Provision`), used for both the Art. 6(3) law of Art. 6(1)(c)/(e) and the law, collective agreement or health-professional contract that Art. 9(2)(b), (g), (h), (i), (j) require | One machine-checkable model for every legal-source reference; jurisdiction is explicit, so a multi-country controller can list ES and PT laws; validation can check the reference kind the condition needs | Breaks `LawfulBasisAttribute.LegalReference`, the aggregate, events, read model and `ILawfulBasisService` signatures (pre-1.0: acceptable) |
| **C) Free-text note** | No model change | The issue rejects it: not machine-checkable |

### Chosen Option: **B — one structured `LegalBasisReference` list** (recommended, pending the maintainer)

### Rationale

- We recommend B because Art. 6(3) and Art. 9(2) ask the same question ("which law?"). Two fields for one question would diverge, and the pre-1.0 rule says to change the shape completely rather than keep a parallel one.
- `LegalBasisReference` is a sealed record with `Kind` (`UnionLaw`, `MemberStateLaw`, `CollectiveAgreement`, `HealthProfessionalContract`), `Jurisdiction` (ISO 3166-1 alpha-2, or `EU` for Union law), `Instrument` (for example "LO 3/2018 (LOPDGDD)") and `Provision` (for example "art. 9.2; DA 17ª").
- `ContractReference` on the lawful-basis aggregate stays, because it documents the Art. 6(1)(b) contract with the data subject, which is a different thing from the Art. 9(2)(h) contract with a health professional.
- Encina records the reference; it never decides which law applies (SPEC-002 §4.1 point 1: the application takes every legal decision).

</details>

<details>
<summary><strong>3. Art. 6 cardinality and scenario S18 — one Art. 6 basis per activity</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep one Art. 6 basis per processing activity**; S18's "appointments and invoicing" with Art. 6(1)(b)/(c) is two activities in the RoPA | No change to the existing basis model; matches regulator guidance that each purpose has one basis; each row of the export is one purpose with one ground | S18's wording reads as one activity; the reference scenario (P-25, #1227) must register two |
| **B) A list of Art. 6 bases per activity** | Mirrors S18's wording literally | Contradicts `LawfulBasis.cs:12-14` ("should not be swapped") semantics, the lawful-basis pipeline (one basis per request type, `LawfulBasisValidationPipelineBehavior.cs:145-157`) and the six-value tag; widens this issue to every consumer of `LawfulBasis` |

### Chosen Option: **A — one Art. 6 basis per activity** (recommended, pending the maintainer)

### Rationale

- We recommend A because appointments (Art. 6(1)(b), contract) and invoicing (Art. 6(1)(c), tax law) are two purposes with two retention periods; one row would hide which basis covers which purpose.
- The RoPA export already lists one row per request type (`JsonRoPAExporter.cs:78`), so S18 is met by two rows that both carry Art. 9(2)(h).
- The plan records this reading of S18 in the P-25 scenario notes (Next Steps), so the scenario author does not expect one row.

</details>

<details>
<summary><strong>4. Validation rule and placement — one pure validator at every write path</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A pure `LegalGroundValidator` in `Encina.Compliance.GDPR`, called on every write path**: the 11 `IProcessingActivityRegistry` implementations, `ILawfulBasisService` commands, and both attribute scans at startup | One rule, one test suite; fails at the moment a wrong record would be written; providers stay coherent through one contract test | Eleven call sites to keep in step (enforced by the contract test) |
| **B) A decorator around `IProcessingActivityRegistry`** | One call site for the 10 providers | Depends on registration order; conflicts with the rule that a database store wins over the in-memory default in any order; does not cover the lawful-basis aggregate |
| **C) Check only in the pipeline at request time** | No change to stores | Invalid records reach the RoPA and its export; AC-010 is about the record, not the request |

### Chosen Option: **A — one pure validator at every write path** (recommended, pending the maintainer)

### Rationale

We recommend A because the record itself is the Article 30 evidence, so it must be valid when written. The rule set:

| Rule | Error code | Source |
|------|------------|--------|
| Special categories declared and condition `NotDeclared` | `gdpr.special_category_condition_missing` | AC-010; enforced by the `SpecialCategoryProcessing` factory |
| Condition (b) without a `UnionLaw`, `MemberStateLaw` or `CollectiveAgreement` reference | `gdpr.legal_basis_reference_missing` | Art. 9(2)(b) |
| Condition (g), (i) or (j) without a `UnionLaw` or `MemberStateLaw` reference | same | Art. 9(2)(g), (i), (j) |
| Condition (h) without a `UnionLaw`, `MemberStateLaw` or `HealthProfessionalContract` reference | same | Art. 9(2)(h) and 9(3) |
| Art. 6(1)(c) or (e) without a `UnionLaw` or `MemberStateLaw` reference | same | Art. 6(3) |
| A draft Omnibus condition while the draft option is off | `gdpr.draft_condition_disabled` | REQ-024 (Design Choice 8) |
| A condition or references without any special category | `gdpr.special_category_condition_without_categories` | consistency |

- The validator only checks that the reference the article demands is present. It never judges whether the cited law is the right one.
- Aggregates keep throwing `ArgumentException` for invariants, as `LawfulBasisAggregate.Register` does today (`:108`); the service validates first and returns `Left`. Whether aggregates should return `Either` is #1798's question, not this issue's.

</details>

<details>
<summary><strong>5. Lawful-basis event model — extend the existing events and add <code>SpecialCategoryGroundChanged</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add the new fields to `LawfulBasisRegistered` and `LawfulBasisChanged`, and add one event `SpecialCategoryGroundChanged`** for a change of the Art. 9 ground or the references while the Art. 6 basis stays | Keeps the existing rule that an Art. 6 basis change is a distinct, deliberate act (`LawfulBasisAggregate.cs:155-159`); the stream shows exactly what changed; projection handles one more event | One more event type and command |
| **B) Generalize `ChangeBasis` into `ChangeGround`**, allowing an unchanged Art. 6 basis, one `LawfulBasisChanged` event for everything | One command | Loses the distinction auditors care about (an Art. 6 swap is a red flag, a new law reference is routine); `OldBasis == NewBasis` events become normal |
| **C) A separate `SpecialCategoryGroundAggregate`** | Independent lifecycle | Two streams for one registration; projections must join them; the pipeline needs two lookups |

### Chosen Option: **A — extend events and add `SpecialCategoryGroundChanged`** (recommended, pending the maintainer)

### Rationale

- We recommend A because it keeps one stream per registration and keeps the Art. 6 swap visible in the audit trail (GDPR Art. 5(2)).
- `SpecialCategoryGroundChanged(RegistrationId, SpecialCategoryProcessing? OldGround, SpecialCategoryProcessing? NewGround, IReadOnlyList<LegalBasisReference> References, DateTimeOffset ChangedAtUtc, string? TenantId, string? ModuleId)` carries old and new values, as `LawfulBasisChanged` does for the basis.
- Marten stores events as JSON; adding fields to existing records is a shape change, which pre-1.0 needs no upcaster for (no persisted user data exists).

</details>

<details>
<summary><strong>6. Declaration surface on attributes — properties plus a repeatable <code>[LegalBasisReference]</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New properties on `[ProcessingActivity]` and `[LawfulBasis]`** (`SpecialCategories`, `SpecialCategoryCondition`) **plus a repeatable `[LegalBasisReference(kind, jurisdiction, instrument, provision)]`** on the request type | Typed, compile-time checked; one attribute per law reads like the RoPA entry; both scans read the same references | One more attribute type |
| **B) String-encoded properties** (`LegalBases = new[] { "ES|LO 3/2018|art. 9.2" }`) | No new attribute | A parser and a format to document; typos found only at startup |
| **C) Programmatic registration only** | No attribute change | Breaks the declarative pattern both packages use (`ProcessingActivityAttribute.cs:34`, `LawfulBasisAttribute.cs:51`); attribute-declared activities could never be special-category |

### Chosen Option: **A — typed properties plus a repeatable `[LegalBasisReference]`** (recommended, pending the maintainer)

### Rationale

- We recommend A because it keeps the declarative style and gives a compile-time error for a wrong enum value. Example:

```csharp
[ProcessingActivity(Purpose = "Clinical appointments", LawfulBasis = LawfulBasis.Contract,
    DataCategories = ["Name", "Phone", "Clinical notes"], DataSubjects = ["Patients"], RetentionDays = 1825,
    SpecialCategories = [SpecialCategoryOfPersonalData.Health],
    SpecialCategoryCondition = SpecialCategoryCondition.HealthOrSocialCare)]
[LegalBasisReference(LegalBasisReferenceKind.MemberStateLaw, "ES", "LO 3/2018 (LOPDGDD)", "art. 9.2; DA 17ª")]
[LegalBasisReference(LegalBasisReferenceKind.MemberStateLaw, "ES", "Ley 41/2002", "arts. 16-17")]
public sealed record BookAppointmentCommand(...) : ICommand<AppointmentId>;
```

- When both `[ProcessingActivity]` and `[LawfulBasis]` declare a ground and they disagree, the lawful-basis pipeline already logs a conflict for the Art. 6 basis (`LawfulBasisValidationPipelineBehavior.cs:409-411`). The same check extends to the condition.

</details>

<details>
<summary><strong>7. Tenancy of the RoPA — integrate now across the 10 providers and the lawful-basis queries</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Integrate tenancy in this issue**: `TenantId` on `ProcessingActivity` and its entity on the 10 providers; shared definitions (registered from attributes, no tenant) plus per-tenant rows that override them; reads resolve the ambient tenant through `IRequestContextAccessor` and fail closed when required (the #1315 Consent pattern); lawful-basis queries and cache keys filter by tenant | Meets the issue's tenant criterion (AC-043) and SPEC-002 REQ-061 ("the RoPA (REQ-010) is per tenant"); one schema change on the 10 providers instead of two; fixes the lawful-basis cross-tenant read | Roughly doubles the provider work of this issue |
| **B) Defer tenancy to a new issue** | Smaller pull request | The issue's acceptance criterion fails; a second schema change on the same 10 tables later; #1257 (P-54) does not list the RoPA, so it would need its own issue |
| **C) Keep activities tenant-less (code-level definitions) and add tenant data only at export time** | No schema change for tenancy | The Art. 9 ground and national references differ per controller (a Portuguese tenant cites Portuguese law), so they cannot be code-level only |

### Chosen Option: **A — integrate tenancy now** (recommended, pending the maintainer)

### Rationale

- We recommend A because each practice is its own controller (SPEC-002 §4.1), so its RoPA, its conditions and its national references are its own, and DEC-009 says retrofitting tenancy after 1.0 would break persisted shapes.
- Shared definitions keep attribute registration working: the lookup returns the ambient tenant's row when one exists, else the shared row. A tenant never reads another tenant's row.
- SQL unique keys treat `NULL` differently per database (SQL Server: equal; PostgreSQL and MySQL: distinct), so the persisted `TenantId` column is `NOT NULL` and uses the literal `-` for shared definitions, as Consent's cache keys do (`src/Encina.Compliance.Consent/Services/DefaultConsentService.cs:32`). The unique key becomes `(TenantId, RequestTypeName)`.
- The `GDPROptions.RequireTenantContext` and `LawfulBasisOptions.RequireTenantContext` options mirror `ConsentOptions.RequireTenantContext`: on by default in a multi-tenant application, explicit opt-out logged once (`DefaultConsentService.cs:39-51`, `:109-111`).

</details>

<details>
<summary><strong>8. Room for the Omnibus draft conditions — reserved values, off by default</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add the Art. 9(2) points that COM(2025) 837 proposes as enum values now** (explicit numbers from 100 upward), rejected by the validator unless `LegalGroundOptions.EnableDraftOmnibusConditions` is `true` (default `false`) | Persisted shape is final before 1.0, as DEC-012 asks; AC-024's "written and read back unchanged while the option is off" test is possible; #815 builds on it | Two enum values for a law not yet adopted; names must follow the final text |
| **B) Leave them out; append when the Omnibus is adopted** | No draft law in the code | DEC-012 asks the shape to exist before 1.0; appending to a persisted enum later is cheap, but #815's metadata would then need its own shape change |
| **C) Open string codes for conditions** | Any future point fits | Loses enum checking; every consumer parses strings |

### Chosen Option: **A — reserved draft values, off by default** (recommended, pending the maintainer)

### Rationale

- We recommend A because SPEC-002 DEC-012 builds draft provisions before 1.0, off by default, when adding them later would touch persisted shapes; the issue asks for "room for the #815 AI-training values".
- The values are numbered from 100 so that current-law values (1-10) never shift; the XML documentation names the draft article, as REQ-024 requires. The implementer reads the point letters and wording from the EUR-Lex text of COM(2025) 837 before naming the values, because the proposal can still change in committee (SPEC-002 §3.5).
- `LegalGroundOptions` is one small options class registered by both `AddEncinaGDPR` and `AddEncinaLawfulBasis` (`TryAdd`), so the flag has one source.

</details>

<details>
<summary><strong>9. Persistence representation on the 10 providers — JSON text columns plus an integer condition</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Three new columns**: `SpecialCategoriesJson` (text, JSON array of enum names), `SpecialCategoryConditionValue` (int, `0` = none), `LegalBasisReferencesJson` (text), plus `TenantId` | Same pattern as the three existing JSON columns (`ProcessingActivityEntity.cs:48-61`); one mapper change serves all 10 providers; condition is indexable | Lists are not queryable in SQL |
| **B) Child tables** for categories and references | Fully relational and queryable | Two new tables and joins on 9 relational providers, a different shape in MongoDB; far more code for data nobody queries by column |
| **C) One JSON blob for the whole legal ground** | One column | The condition is hidden inside JSON; harder to index and read in the database |

### Chosen Option: **A — JSON text columns plus an integer condition** (recommended, pending the maintainer)

### Rationale

- We recommend A because the RoPA is read whole (export, pipeline lookup by request type), never searched by category, and the existing entity already stores lists as camel-case JSON through `ProcessingActivityMapper`.
- Enum names, not numbers, go into the JSON arrays, so an export or a database reader sees `"health"` rather than `7`.

</details>

---

## Implementation Phases

### Phase 1: Core Model, Validator and Errors (`Encina.Compliance.GDPR`)

> **Goal**: The legal-ground vocabulary and its single validation rule, shared by both packages.

<details>
<summary><strong>Tasks</strong></summary>

#### New files in `src/Encina.Compliance.GDPR/Model/` (namespace `Encina.Compliance.GDPR`)

1. `SpecialCategoryOfPersonalData.cs` — enum, explicit values: `RacialOrEthnicOrigin = 1`, `PoliticalOpinions = 2`, `ReligiousOrPhilosophicalBeliefs = 3`, `TradeUnionMembership = 4`, `GeneticData = 5`, `BiometricDataForIdentification = 6`, `Health = 7`, `SexLifeOrSexualOrientation = 8` (Art. 9(1)).
2. `SpecialCategoryCondition.cs` — enum: `NotDeclared = 0`, `ExplicitConsent = 1` (a), `EmploymentAndSocialSecurityLaw = 2` (b), `VitalInterestsSubjectIncapable = 3` (c), `NotForProfitBody = 4` (d), `ManifestlyMadePublic = 5` (e), `LegalClaimsOrCourts = 6` (f), `SubstantialPublicInterest = 7` (g), `HealthOrSocialCare = 8` (h), `PublicHealth = 9` (i), `ArchivingResearchOrStatistics = 10` (j); draft values from `100` per Design Choice 8, named from the EUR-Lex text of COM(2025) 837, each XML-documented as draft (REQ-024). Each value's `<remarks>` cites its point and whether it needs a law reference.
3. `LegalBasisReferenceKind.cs` — enum: `UnionLaw = 1`, `MemberStateLaw = 2`, `CollectiveAgreement = 3`, `HealthProfessionalContract = 4`.
4. `LegalBasisReference.cs` — `sealed record LegalBasisReference(LegalBasisReferenceKind Kind, string Jurisdiction, string Instrument, string Provision)`; static `Create(...)` returning `Either<EncinaError, LegalBasisReference>` (non-empty strings, jurisdiction `EU` or two upper-case letters, `EU` only with `UnionLaw`); `ToString()` → `"ES: LO 3/2018 (LOPDGDD), art. 9.2"`.
5. `SpecialCategoryProcessing.cs` — `sealed record SpecialCategoryProcessing` with `IReadOnlyList<SpecialCategoryOfPersonalData> Categories` and `SpecialCategoryCondition Condition`; private constructor; `static Either<EncinaError, SpecialCategoryProcessing> Create(IEnumerable<SpecialCategoryOfPersonalData> categories, SpecialCategoryCondition condition)` (distinct, sorted categories; non-empty; condition not `NotDeclared`); `ArticleReference` property → `"Art. 9(2)(h)"`.
6. `LegalGround.cs` — `readonly record struct LegalGround(LawfulBasis Basis, SpecialCategoryProcessing? SpecialCategory, IReadOnlyList<LegalBasisReference> References)`, the input of the validator, built from a `ProcessingActivity` or a lawful-basis command.

#### Validator and options

7. `src/Encina.Compliance.GDPR/Validation/LegalGroundValidator.cs` — `public static class LegalGroundValidator` with `Either<EncinaError, Unit> Validate(LegalGround ground, LegalGroundOptions options)` applying the rule table of Design Choice 4; one `private static` method per rule so each stays below CRAP 10; `ArticleOf(LawfulBasis)` helper → `"Art. 6(1)(b)"` reused by the exporters.
8. `src/Encina.Compliance.GDPR/LegalGroundOptions.cs` — `public sealed class LegalGroundOptions { public bool EnableDraftOmnibusConditions { get; set; } }` (default `false`).
9. `src/Encina.Compliance.GDPR/GDPRErrors.cs` — add `SpecialCategoryConditionMissingCode = "gdpr.special_category_condition_missing"`, `SpecialCategoryConditionWithoutCategoriesCode`, `LegalBasisReferenceMissingCode`, `LegalBasisReferenceInvalidCode`, `DraftConditionDisabledCode` and their factories; metadata carries the condition and the required reference kinds, never a subject identifier.
10. `PublicAPI.Unshipped.txt` of `Encina.Compliance.GDPR` — every new symbol.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #1196 (GDPR Art. 9(2) conditions and national legal bases).

CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0, Railway Oriented Programming (Either<EncinaError, T>, LanguageExt).
- Package: src/Encina.Compliance.GDPR. Today the only legal-ground type is the Art. 6 enum in Model/LawfulBasis.cs.
- Plan: docs/plans/special-category-conditions-implementation-plan-1196.md, Design Choices 1, 2, 4 and 8.

TASK:
Create SpecialCategoryOfPersonalData, SpecialCategoryCondition, LegalBasisReferenceKind, LegalBasisReference,
SpecialCategoryProcessing, LegalGround, LegalGroundOptions and the static LegalGroundValidator, plus the new
GDPRErrors codes, exactly as listed in the Phase 1 Tasks. Add every public symbol to PublicAPI.Unshipped.txt.

KEY RULES:
- Enums have explicit numeric values; SpecialCategoryCondition.NotDeclared = 0; draft Omnibus values start at 100
  and their XML docs name the draft article of COM(2025) 837 (read the EUR-Lex text first).
- Factories return Either; no exception for validation. Constructors of SpecialCategoryProcessing are private.
- The validator only checks that the reference kind the article demands is present; it never judges the law.
- One private method per rule; every method CRAP <= 10 (measure locally with the crap-gate table).
- Error metadata never contains subject identifiers; EncinaError.Message never goes to logs or tags.
- XML docs on every public member with the GDPR article; examples use LOPDGDD art. 9.2 / DA 17ª as an example only.

REFERENCE FILES:
- src/Encina.Compliance.GDPR/Model/LawfulBasis.cs (enum documentation style)
- src/Encina.Compliance.GDPR/GDPRErrors.cs (error code constants and factories)
- src/Encina.Compliance.GDPR/Model/ProcessingActivity.cs (record style)
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (REQ-010, REQ-024, AC-010, DEC-012)
```

</details>

---

### Phase 2: Processing Activity, Attributes and the In-Memory Registry

> **Goal**: `ProcessingActivity` carries the Art. 9 ground, the references and the tenant; attribute registration validates and fails fast.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Compliance.GDPR/Model/ProcessingActivity.cs` — add `SpecialCategoryProcessing? SpecialCategoryProcessing { get; init; }`, `IReadOnlyList<LegalBasisReference> LegalBasisReferences { get; init; } = []`, `string? TenantId { get; init; }` (null = shared definition, Design Choice 7); add `LegalGround ToLegalGround()`.
2. `src/Encina.Compliance.GDPR/Attributes/ProcessingActivityAttribute.cs` — add `SpecialCategoryOfPersonalData[] SpecialCategories { get; set; } = []` and `SpecialCategoryCondition SpecialCategoryCondition { get; set; }`.
3. New `src/Encina.Compliance.GDPR/Attributes/LegalBasisReferenceAttribute.cs` — `[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]`, constructor `(LegalBasisReferenceKind kind, string jurisdiction, string instrument, string provision)`, `ToReference()` returning `Either`.
4. `src/Encina.Compliance.GDPR/Attributes/LawfulBasisAttribute.cs` — add `SpecialCategories` and `SpecialCategoryCondition`; **remove** `LegalReference` (Design Choice 2: references come from `[LegalBasisReference]`).
5. New `src/Encina.Compliance.GDPR/Attributes/LegalGroundAttributeReader.cs` (internal static) — `Either<EncinaError, (SpecialCategoryProcessing?, IReadOnlyList<LegalBasisReference>)> Read(Type requestType)` used by both packages' scans; it builds the composite through the Phase 1 factories.
6. `src/Encina.Compliance.GDPR/InMemoryProcessingActivityRegistry.cs` — key becomes `(string TenantKey, Type RequestType)`; `RegisterActivityAsync`/`UpdateActivityAsync` call `LegalGroundValidator.Validate` first and return its `Left`; reads take the ambient tenant (Phase 8 helper) and return the tenant row, else the shared row; `AutoRegisterFromAssemblies` returns `Either<EncinaError, int>` and stops at the first invalid declaration, naming the request type.
7. `src/Encina.Compliance.GDPR/GDPRAutoRegistrationHostedService.cs` — on `Left`, log (new EventId, Phase 9) and throw `InvalidOperationException` with the error code so the host fails to start (startup is the boundary; ROP stays inside).
8. `PublicAPI.Unshipped.txt` — new and removed symbols (RS0017 for `LawfulBasisAttribute.LegalReference`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #1196.

CONTEXT:
- Phase 1 types exist in src/Encina.Compliance.GDPR/Model and Validation.
- ProcessingActivity (Model/ProcessingActivity.cs) is the Article 30 record; InMemoryProcessingActivityRegistry keys it by
  request type and AutoRegisterFromAssemblies builds it from [ProcessingActivity] (lines 117-161).
- Design Choices 1, 2, 6 and 7 of docs/plans/special-category-conditions-implementation-plan-1196.md apply.

TASK:
Add SpecialCategoryProcessing, LegalBasisReferences and TenantId to ProcessingActivity; extend [ProcessingActivity]
and [LawfulBasis]; add the repeatable [LegalBasisReference] attribute and the internal LegalGroundAttributeReader;
make the in-memory registry validate on write, be tenant-aware with shared definitions, and make auto-registration
fail the host start on an invalid declaration.

KEY RULES:
- Remove LawfulBasisAttribute.LegalReference completely (pre-1.0, no [Obsolete], no alias).
- The registry returns the validator's Left unchanged; never write an invalid activity.
- Tenant lookup: ambient tenant row first, then the shared row (TenantId null); never another tenant's row.
- TimeProvider for timestamps; no DateTime.UtcNow.
- Keep every changed method at CRAP <= 10.

REFERENCE FILES:
- src/Encina.Compliance.GDPR/InMemoryProcessingActivityRegistry.cs
- src/Encina.Compliance.GDPR/Attributes/ProcessingActivityAttribute.cs
- src/Encina.Compliance.GDPR/Attributes/LawfulBasisAttribute.cs
- src/Encina.Compliance.GDPR/GDPRAutoRegistrationHostedService.cs
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs (ambient tenant pattern, #1315)
```

</details>

---

### Phase 3: RoPA Export — JSON and CSV

> **Goal**: Both exports carry the Art. 6 article, the Art. 9 ground, the references and the tenant (AC-010, S18).

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Compliance.GDPR/Export/JsonRoPAExporter.cs` — `RoPAJsonActivity` gains `LawfulBasisArticle` (`"Art. 6(1)(b)"`), `SpecialCategoryProcessing` (`{ categories: ["health"], condition: "healthOrSocialCare", article: "Art. 9(2)(h)" }`, omitted when null by the existing `WhenWritingNull`), `LegalBasisReferences` (array of `{ kind, jurisdiction, instrument, provision }`) and `TenantId`; `RoPAJsonMetadata` gains `TenantId`.
2. `src/Encina.Compliance.GDPR/Export/CsvRoPAExporter.cs` — `Headers` gains `LawfulBasisArticle`, `SpecialCategories`, `SpecialCategoryCondition`, `SpecialCategoryArticle`, `LegalBasisReferences` (each reference rendered by `LegalBasisReference.ToString()`, joined with `;`), `TenantId`; metadata header gains `# Tenant: <id or shared>`.
3. `src/Encina.Compliance.GDPR/Export/RoPAExportMetadata.cs` — add `string? TenantId`.
4. Both exporters gain a constructor `(ILogger<T>? logger = null)` (null → `NullLogger`) for the Phase 9 log messages; the parameterless use in applications keeps working through the default.
5. Coordinate column order with #1198 (P-10 adds processor roles to the same export): whichever lands second appends its columns after the other's.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #1196.

CONTEXT:
- JsonRoPAExporter (Export/JsonRoPAExporter.cs:99-157) maps ProcessingActivity to private DTOs; CsvRoPAExporter
  (Export/CsvRoPAExporter.cs:31-47, 111-132) writes RFC 4180 rows with ';' list separators.
- Phase 2 added SpecialCategoryProcessing, LegalBasisReferences and TenantId to ProcessingActivity.

TASK:
Add the Art. 6 article, the Art. 9 ground (categories, condition, article), the legal-basis references and the
tenant to both exports and to RoPAExportMetadata, as listed in the Phase 3 Tasks.

KEY RULES:
- JSON uses the existing camel-case enum converter; null ground is omitted, never written as an empty object.
- CSV escapes every new field through EscapeCsv; references are joined with ';'.
- No personal data of data subjects appears in either export (the RoPA describes categories, not people).
- Keep FormatRow and MapActivity below CRAP 10 (extract helpers if needed).

REFERENCE FILES:
- src/Encina.Compliance.GDPR/Export/JsonRoPAExporter.cs
- src/Encina.Compliance.GDPR/Export/CsvRoPAExporter.cs
- tests/Encina.UnitTests/Compliance/GDPR/Export/JsonRoPAExporterTests.cs
- tests/Encina.UnitTests/Compliance/GDPR/Export/CsvRoPAExporterTests.cs
```

</details>

---

### Phase 4: Lawful-Basis Aggregate, Events, Read Model and Projection

> **Goal**: The event-sourced registration records the Art. 9 ground and references (Design Choices 2 and 5).

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Compliance.LawfulBasis/Events/LawfulBasisEvents.cs` — `LawfulBasisRegistered` and `LawfulBasisChanged`: replace `string? LegalReference` with `IReadOnlyList<LegalBasisReference> LegalBasisReferences`, add `SpecialCategoryProcessing? SpecialCategoryProcessing`; add `SpecialCategoryGroundChanged` (Design Choice 5), implementing `INotification`.
2. `src/Encina.Compliance.LawfulBasis/Aggregates/LawfulBasisAggregate.cs` — properties `SpecialCategoryProcessing`, `LegalBasisReferences` (replacing `LegalReference`); `Register(...)` and `ChangeBasis(...)` take them; new `ChangeSpecialCategoryGround(SpecialCategoryProcessing? ground, IReadOnlyList<LegalBasisReference> references, DateTimeOffset changedAtUtc)` throwing `InvalidOperationException` when revoked or when nothing changes; `Apply` handles the new event.
3. `src/Encina.Compliance.LawfulBasis/ReadModels/LawfulBasisReadModel.cs` — same property change.
4. `src/Encina.Compliance.LawfulBasis/ReadModels/LawfulBasisProjection.cs` — map the new fields in `Create` and `Apply(LawfulBasisChanged)`; add `IProjectionHandler<SpecialCategoryGroundChanged, LawfulBasisReadModel>`.
5. `src/Encina.Compliance.LawfulBasis/Abstractions/ILawfulBasisService.cs` and `Services/DefaultLawfulBasisService.cs` — `RegisterAsync` and `ChangeBasisAsync` take the ground and references instead of `legalReference`; new `ChangeSpecialCategoryGroundAsync(Guid registrationId, SpecialCategoryProcessing? ground, IReadOnlyList<LegalBasisReference> references, CancellationToken)`; each command calls `LegalGroundValidator.Validate` (with `IOptions<LegalGroundOptions>`) before touching the aggregate and returns its `Left`.
6. `src/Encina.Compliance.LawfulBasis/AutoRegistration/LawfulBasisAutoRegistrationHostedService.cs` — read the ground through `LegalGroundAttributeReader`; stop counting failed registrations and fail the start on `Left` (the minimal part of the bug in issue file `plan-1196-lawfulbasis-autoregistration-errors.md`, needed so an invalid declaration is not swallowed).
7. `src/Encina.Compliance.LawfulBasis/Pipeline/LawfulBasisValidationPipelineBehavior.cs` — `LawfulBasisAttributeInfo` gains the condition; the conflict check (`:409-411`) also compares the condition; when the condition is `ExplicitConsent` and `ValidateConsentForConsentBasis` is on, run the existing consent check (`:229-260`) as for Art. 6(1)(a). This check only works once #1920 lands (Prerequisites).
8. `PublicAPI.Unshipped.txt` of `Encina.Compliance.LawfulBasis`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #1196 in src/Encina.Compliance.LawfulBasis (Marten event-sourced, ADR-019).

CONTEXT:
- LawfulBasisAggregate (Aggregates/LawfulBasisAggregate.cs) raises LawfulBasisRegistered, LawfulBasisChanged and
  LawfulBasisRevoked (Events/LawfulBasisEvents.cs); ChangeBasis refuses an unchanged basis (lines 155-159).
- LawfulBasisProjection builds LawfulBasisReadModel; DefaultLawfulBasisService wraps commands in Either.
- Phase 1 and 2 types live in Encina.Compliance.GDPR (SpecialCategoryProcessing, LegalBasisReference,
  LegalGroundValidator, LegalGroundAttributeReader).

TASK:
Replace LegalReference with LegalBasisReferences and add SpecialCategoryProcessing on the events, aggregate, read
model, projection and service; add the SpecialCategoryGroundChanged event and the ChangeSpecialCategoryGround
command; validate in the service before the aggregate; propagate auto-registration failures; extend the pipeline's
attribute info and conflict check.

KEY RULES:
- No [Obsolete], no compatibility alias for LegalReference.
- The service validates first and returns Left; the aggregate keeps ArgumentException/InvalidOperationException
  guards (#1798 decides whether aggregates move to Either).
- Projections take dependencies through IDocumentOperations/constructor injection, never IServiceProvider.
- Event records carry TenantId and ModuleId like their siblings.
- Unit tests mock IAggregateRepository with NSubstitute; integration tests run on Marten/PostgreSQL (Phase 10).

REFERENCE FILES:
- src/Encina.Compliance.LawfulBasis/Aggregates/LawfulBasisAggregate.cs
- src/Encina.Compliance.LawfulBasis/Events/LawfulBasisEvents.cs
- src/Encina.Compliance.LawfulBasis/ReadModels/LawfulBasisProjection.cs
- src/Encina.Compliance.LawfulBasis/Services/DefaultLawfulBasisService.cs
- src/Encina.Compliance.LawfulBasis/AutoRegistration/LawfulBasisAutoRegistrationHostedService.cs
- src/Encina.Compliance.LawfulBasis/Pipeline/LawfulBasisValidationPipelineBehavior.cs
```

</details>

---

### Phase 5: Persistence Entity, Mapper and Provider Scripts

> **Goal**: One database-agnostic shape for the new fields (Design Choice 9), used by the 10 providers.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Compliance.GDPR/ProcessingActivityEntity.cs` — add `string TenantId` (`"-"` for shared), `string SpecialCategoriesJson`, `int SpecialCategoryConditionValue`, `string LegalBasisReferencesJson`.
2. `src/Encina.Compliance.GDPR/ProcessingActivityMapper.cs` — map both ways; `ToDomain` rebuilds the composite through `SpecialCategoryProcessing.Create` and `LegalBasisReference.Create` and returns `null` (as for an unknown request type, `:70-74`) when the stored row is invalid, and the registries count that as a skipped row in their trace; constant `SharedTenantKey = "-"`.
3. Schema scripts — `src/Encina.ADO.SqlServer/Scripts/011_CreateProcessingActivitiesTable.sql`, `src/Encina.ADO.PostgreSQL/Scripts/011_...sql`, `src/Encina.ADO.MySQL/Scripts/011_...sql`: add the four columns (`TenantId` `NOT NULL DEFAULT '-'`), replace `UQ_ProcessingActivities_RequestTypeName` with a unique key on `(TenantId, RequestTypeName)`, add an index on `TenantId`. SQL Server `NVARCHAR(128)`/`NVARCHAR(MAX)`/`INT`; PostgreSQL `VARCHAR(128)`/`TEXT`/`INTEGER`; MySQL `VARCHAR(128)`/`LONGTEXT`/`INT` with backtick identifiers.
4. Test schemas — `tests/Encina.TestInfrastructure/Schemas/SqlServerSchema.cs:481`, `PostgreSqlSchema.cs:478`, `MySqlSchema.cs:337`: the same columns and keys.
5. `src/Encina.EntityFrameworkCore/ProcessingActivity/ProcessingActivityEntityConfiguration.cs` — map the columns; replace the unique index (`:71-72`) with `HasIndex(x => new { x.TenantId, x.RequestTypeName }).IsUnique()`.
6. `src/Encina.MongoDB/ProcessingActivity/ProcessingActivityDocument.cs` — add the fields (`tenant_id`, `special_categories_json`, `special_category_condition_value`, `legal_basis_references_json`); the unique index (`ProcessingActivityRegistryMongoDB.cs:48-52`) becomes compound `(tenant_id, request_type_name)`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #1196: the persisted shape of ProcessingActivity on the 10 database providers.

CONTEXT:
- ProcessingActivityEntity and ProcessingActivityMapper (src/Encina.Compliance.GDPR) are shared by ADO.NET, Dapper,
  EF Core and MongoDB registries. Lists are stored as camel-case JSON strings today.
- ADO scripts: src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/Scripts/011_CreateProcessingActivitiesTable.sql.
  Test schemas: tests/Encina.TestInfrastructure/Schemas/{SqlServer,PostgreSql,MySql}Schema.cs.
- Design Choices 7 and 9 of the plan fix the columns and the (TenantId, RequestTypeName) unique key.

TASK:
Add TenantId, SpecialCategoriesJson, SpecialCategoryConditionValue and LegalBasisReferencesJson to the entity,
mapper, the three scripts, the three test schemas, the EF Core configuration and the MongoDB document, and change
the unique key to (TenantId, RequestTypeName).

KEY RULES:
- TenantId is NOT NULL with the literal "-" for shared definitions (NULL uniqueness differs per database).
- SQL Server: [brackets], NVARCHAR, INT; PostgreSQL: case-sensitive identifiers, TEXT, INTEGER; MySQL: backticks,
  LONGTEXT, INT.
- JSON arrays hold enum names in camel case, not numbers.
- The mapper rebuilds value objects through their factories; an invalid stored row maps to null.
- Do not edit the registries yet (Phase 6).

REFERENCE FILES:
- src/Encina.Compliance.GDPR/ProcessingActivityEntity.cs
- src/Encina.Compliance.GDPR/ProcessingActivityMapper.cs
- src/Encina.ADO.SqlServer/Scripts/011_CreateProcessingActivitiesTable.sql
- src/Encina.EntityFrameworkCore/ProcessingActivity/ProcessingActivityEntityConfiguration.cs
- src/Encina.MongoDB/ProcessingActivity/ProcessingActivityDocument.cs
```

</details>

---

### Phase 6: Multi-Provider Registries — All 10 Database Providers

> **Goal**: Every `IProcessingActivityRegistry` validates on write and reads by tenant with the same semantics.

<details>
<summary><strong>Tasks</strong></summary>

| Provider | File | Change |
|----------|------|--------|
| ADO SqlServer | `src/Encina.ADO.SqlServer/ProcessingActivity/ProcessingActivityRegistryADO.cs` | columns in INSERT/UPDATE/SELECT; `WHERE [TenantId] IN (@TenantId, '-')` ordered so the tenant row wins; validate before INSERT/UPDATE |
| ADO PostgreSQL | `src/Encina.ADO.PostgreSQL/ProcessingActivity/ProcessingActivityRegistryADO.cs` | same, PostgreSQL syntax |
| ADO MySQL | `src/Encina.ADO.MySQL/ProcessingActivity/ProcessingActivityRegistryADO.cs` | same, MySQL syntax |
| Dapper SqlServer | `src/Encina.Dapper.SqlServer/ProcessingActivity/ProcessingActivityRegistryDapper.cs` | same |
| Dapper PostgreSQL | `src/Encina.Dapper.PostgreSQL/ProcessingActivity/ProcessingActivityRegistryDapper.cs` | same |
| Dapper MySQL | `src/Encina.Dapper.MySQL/ProcessingActivity/ProcessingActivityRegistryDapper.cs` | same |
| EF Core (3) | `src/Encina.EntityFrameworkCore/ProcessingActivity/ProcessingActivityRegistryEF.cs` | LINQ filter on `TenantId`; validate before `Add`/`Update` |
| MongoDB | `src/Encina.MongoDB/ProcessingActivity/ProcessingActivityRegistryMongoDB.cs` | filter on `tenant_id`; validate before insert/replace |

1. Each registry gains constructor parameters `IRequestContextAccessor` and `IOptions<LegalGroundOptions>` (and keeps `TimeProvider` where present); the `ServiceCollectionExtensions` of each provider (`AddEncinaProcessingActivity*`) register through a factory that resolves them.
2. `GetAllActivitiesAsync` returns the ambient tenant's rows plus shared rows not overridden by a tenant row; `GetActivityByRequestTypeAsync` returns the tenant row, else the shared row; `UpdateActivityAsync` matches on `(TenantId, RequestTypeName)`.
3. Duplicate detection (`SqlException` 2627/2601 and the PostgreSQL, MySQL and MongoDB equivalents) keeps returning `GDPRErrors.ProcessingActivityDuplicate`.
4. Do **not** widen the existing `ex.Message` use in failure tags and store errors (`ProcessingActivityDiagnostics.cs:171`, `ProcessingActivityRegistryADO.cs:81-82`); new code passes the exception type only. The rest is #1591.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #1196: the 10 IProcessingActivityRegistry providers.

CONTEXT:
- Providers: ADO.NET x3, Dapper x3 (SqlServer, PostgreSQL, MySQL), EF Core (one registry serving 3 databases),
  MongoDB. Phase 5 changed the shared entity, mapper, scripts and schemas.
- Phase 2 made InMemoryProcessingActivityRegistry the reference behaviour: validate on write with
  LegalGroundValidator, tenant row first then shared row ("-"), never another tenant's row.

TASK:
Bring every provider registry to the in-memory reference behaviour: new columns in every statement, tenant
filtering through IRequestContextAccessor, LegalGroundValidator before every write, factory registration in each
provider's ServiceCollectionExtensions.

KEY RULES:
- Every database call is async with a CancellationToken (OpenAsync, ExecuteNonQueryAsync, ...).
- SQL per database: SQL Server @param/TOP/[ ]; PostgreSQL @param/LIMIT; MySQL @param/LIMIT/backticks.
- Ambient tenant missing while RequireTenantContext is on -> Left (fail closed, Phase 8 helper).
- No exception message in activity tags or error messages in new code (#1591 covers the old code).
- A database registry still wins over the in-memory default in any registration order.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/ProcessingActivity/ProcessingActivityRegistryADO.cs
- src/Encina.Dapper.SqlServer/ProcessingActivity/ProcessingActivityRegistryDapper.cs
- src/Encina.EntityFrameworkCore/ProcessingActivity/ProcessingActivityRegistryEF.cs
- src/Encina.MongoDB/ProcessingActivity/ProcessingActivityRegistryMongoDB.cs
- src/Encina.Compliance.GDPR/InMemoryProcessingActivityRegistry.cs (reference behaviour)
```

</details>

---

### Phase 7: Configuration and DI

> **Goal**: Options registered once, every dependency resolvable, proven by a `ValidateOnBuild` test.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Compliance.GDPR/ServiceCollectionExtensions.cs` — `TryAdd` `LegalGroundOptions` (`AddOptions<LegalGroundOptions>()`), `IRequestContextAccessor` if Encina core does not already register it; `GDPROptions.RequireTenantContext` (`bool?`, null = on when tenancy is registered, as Consent does).
2. `src/Encina.Compliance.LawfulBasis/ServiceCollectionExtensions.cs` — the same `LegalGroundOptions` registration (`TryAdd`, so whichever package registers first wins); `LawfulBasisOptions.RequireTenantContext`.
3. `src/Encina.Compliance.GDPR/GDPROptionsValidator.cs` and `src/Encina.Compliance.LawfulBasis/LawfulBasisOptionsValidator.cs` — no new failure cases; document the new options.
4. DI tests (Phase 10) build the provider with `ValidateOnBuild = true` and `ValidateScopes = true` for `AddEncinaGDPR`, `AddEncinaLawfulBasis` and each `AddEncinaProcessingActivity*` provider registration.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #1196.

CONTEXT:
- AddEncinaGDPR (src/Encina.Compliance.GDPR/ServiceCollectionExtensions.cs) and AddEncinaLawfulBasis
  (src/Encina.Compliance.LawfulBasis/ServiceCollectionExtensions.cs) register options, validators, registries and
  hosted services. Phases 2-6 added LegalGroundOptions and IRequestContextAccessor dependencies.

TASK:
Register LegalGroundOptions from both packages with TryAdd semantics, add RequireTenantContext to GDPROptions and
LawfulBasisOptions with the Consent semantics, and make sure every resolved dependency is registered.

KEY RULES:
- Registration completeness: a DI test builds the provider with ValidateOnBuild and ValidateScopes.
- A database registry wins over the in-memory default in any order, without overriding the application's own.
- Options with secrets: none here; no [JsonIgnore] needed.

REFERENCE FILES:
- src/Encina.Compliance.GDPR/ServiceCollectionExtensions.cs
- src/Encina.Compliance.LawfulBasis/ServiceCollectionExtensions.cs
- src/Encina.Compliance.Consent/ConsentOptions.cs (RequireTenantContext)
```

</details>

---

### Phase 8: Cross-Cutting Integration — Tenancy, Caching and Audit

> **Goal**: The functions marked ✅ in the matrix are wired: multi-tenancy, caching, validation and audit trail.

<details>
<summary><strong>Tasks</strong></summary>

1. New internal helper `src/Encina.Compliance.GDPR/Tenancy/ComplianceTenantResolver.cs` — `Either<EncinaError, string?> ResolveForQuery(IRequestContextAccessor, bool? requireTenantContext, bool isMultiTenantApplication, string operation)`; same decision table as `DefaultConsentService.TryResolveTenantScope` (`:121`, `:378`), returning `GDPRErrors.TenantContextRequired` (new code `gdpr.tenant_context_required`) when it fails closed. Used by the 11 registries and, through a project reference that already exists, by `DefaultLawfulBasisService`.
2. `src/Encina.Compliance.LawfulBasis/Services/DefaultLawfulBasisService.cs` — filter `GetRegistrationByRequestTypeAsync` (`:490-492`) and `GetAllRegistrationsAsync` (`:521-523`) by the ambient tenant; cache keys become `lb:reg:{tenantKey}:type:{requestTypeName}` (`:479`) so a cached entry is never served to another tenant; `RegisterAsync` takes the tenant from the request context when the caller passes none. This closes the bug in issue file `plan-1196-lawfulbasis-queries-ignore-tenant.md`.
3. Audit trail: the event-sourced registration is the audit trail; `SpecialCategoryGroundChanged` records old and new ground. Relational processing-activity edits have no history today; that is deferred (issue file `plan-1196-ropa-edit-history.md`).
4. Validation: `LegalGroundValidator` at every write path (Phases 2, 4, 6). It is a domain rule, not an `IValidationProvider` request validator, because it validates stored records, not request DTOs.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of issue #1196: cross-cutting integration.

CONTEXT:
- SPEC-002 REQ-061 makes the RoPA per tenant. DefaultConsentService (src/Encina.Compliance.Consent/Services/
  DefaultConsentService.cs) is the house pattern for an ambient tenant read through IRequestContextAccessor that
  fails closed when RequireTenantContext applies (#1315).
- DefaultLawfulBasisService today ignores TenantId in its queries and cache keys (lines 479, 490-492, 521-523).

TASK:
Add the internal ComplianceTenantResolver, use it in the registries and the lawful-basis service, scope the
lawful-basis cache keys by tenant, and add the gdpr.tenant_context_required error.

KEY RULES:
- Compliance gates fail closed; the only opt-out is explicit (RequireTenantContext = false) and logged once.
- Tenant A never reads, caches or exports tenant B's data; tenancy off uses the shared bucket "-".
- No direct identifier of a data subject in logs, tags or cache keys.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- src/Encina/Abstractions/IRequestContextAccessor.cs
- src/Encina.Compliance.LawfulBasis/Services/DefaultLawfulBasisService.cs
```

</details>

---

### Phase 9: Observability

> **Goal**: Every main operation emits an activity and a metric with `encina.tenant_id`, and logs through `[LoggerMessage]` in the packages' registered ranges (REQ-062, AC-044).

<details>
<summary><strong>Tasks</strong></summary>

1. **Names.** The issue's proposed names are replaced by the names these packages already ship, so dashboards see one convention per package: span `GDPR.RoPA.Export` (exists, `GDPRDiagnostics.cs:81`), metrics `gdpr.ropa_export.total`, `gdpr.ropa_export.failed`, `gdpr.ropa_export.duration` (exist, `:41-50`), new `gdpr.processing_activity.validation_failures` (Counter, tags `reason`, `encina.tenant_id`), new `lawful_basis.registrations.ground_changed` and `lawful_basis.registrations.rejected` (Counter, tags `reason`, `encina.tenant_id`).
2. **Wire the dead instruments.** Both RoPA exporters call `GDPRDiagnostics.StartRoPAExport`, `RecordExportCompleted`/`RecordExportFailed` and the three RoPA instruments, adding `encina.tenant_id`. `DefaultLawfulBasisService` increments `RegistrationsCreated`, `RegistrationsRevoked` and `BasisChanged` (`LawfulBasisDiagnostics.cs:76-99`), with `encina.tenant_id`, and starts activities `LawfulBasis.Register`, `LawfulBasis.ChangeBasis`, `LawfulBasis.ChangeSpecialCategoryGround`.
3. **Logging, `Encina.Compliance.GDPR` (range 8100-8199, `EventIdRanges.ComplianceGDPR`, last used 8116).** Make `GDPRLogMessages` `partial` and add `[LoggerMessage]` methods: 8117 `ProcessingActivityRejected` (request type, error code), 8118 `DraftConditionRejected`, 8119 `AutoRegistrationRejected`, 8120 `TenantContextMissing` (operation), 8121 `TenantEnforcementOptedOut` (once at startup). Wire the existing 8109-8111 RoPA export messages from the exporters.
4. **Logging, `Encina.Compliance.LawfulBasis` (range 8350-8399, last used 8386).** Add 8387 `RegistrationRejected` (request type, error code), 8388 `SpecialCategoryGroundChanged` (registration id, condition), 8389 `AutoRegistrationFailed`, 8390 `TenantContextMissing`, 8391 `TenantEnforcementOptedOut`. The registration paths of `DefaultLawfulBasisService` this issue touches move from `LogDebug`/`LogInformation` calls (`:104-123`) to these methods.
5. Telemetry carries the request type, condition, error code and tenant id only: never a subject id, a purpose text written by users, a reference's free text, or an `EncinaError.Message`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of issue #1196: observability.

CONTEXT:
- GDPR telemetry: src/Encina.Compliance.GDPR/Diagnostics/GDPRDiagnostics.cs (RoPA export activity and instruments
  exist but are never called), GDPRLogMessages.cs (LoggerMessage.Define, EventIds 8100-8116).
- LawfulBasis telemetry: src/Encina.Compliance.LawfulBasis/Diagnostics/LawfulBasisDiagnostics.cs (registration
  counters declared, never incremented), LawfulBasisLogMessages.cs ([LoggerMessage], EventIds up to 8386).
- Ranges are registered in src/Encina/Diagnostics/EventIdRanges.cs: ComplianceGDPR (8100-8199),
  ComplianceLawfulBasis (8350-8399); both assemblies are already mapped in
  tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs.

TASK:
Wire the existing RoPA export and registration instruments, add the new counters and activities with the
encina.tenant_id tag, and add the [LoggerMessage] methods with EventIds 8117-8121 and 8387-8391.

KEY RULES:
- [LoggerMessage] source generator for new messages; existing LoggerMessage.Define calls keep their literal
  new EventId(n, ...) (#1125).
- EventIds packed sequentially inside the package range; no gaps.
- No payload, user-written text, subject identifier or EncinaError.Message in tags, metrics or log templates;
  exceptions go through ForLogging().
- Activities start only when the source has listeners.

REFERENCE FILES:
- src/Encina.Compliance.GDPR/Diagnostics/GDPRDiagnostics.cs
- src/Encina.Compliance.GDPR/Diagnostics/GDPRLogMessages.cs
- src/Encina.Compliance.LawfulBasis/Diagnostics/LawfulBasisDiagnostics.cs
- src/Encina.Compliance.LawfulBasis/Diagnostics/LawfulBasisLogMessages.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 10: Testing

> **Goal**: Every flag reaches its target in `.github/coverage-manifest/Encina.Compliance.GDPR.json` and `Encina.Compliance.LawfulBasis.json`, and AC-010, AC-043 and AC-044 are proven by tests.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/Compliance/GDPR/`, `.../LawfulBasisModule/`): every factory and validator rule (one test per row of the Design Choice 4 table, both outcomes); `SpecialCategoryProcessing.Create` rejects `NotDeclared` with categories (AC-010); attribute reader; in-memory registry tenant resolution (tenant row, shared row, other tenant never); mapper round-trip and invalid stored row → `null`; aggregate `ChangeSpecialCategoryGround`; projection of the new event; service validates before the aggregate; auto-registration fails the start on `Left`; pipeline conflict on the condition; `ComplianceTenantResolver` decision table; telemetry tests with an in-memory `ActivityListener`/`MeterListener` asserting `encina.tenant_id` on the RoPA export and registration operations and the absence of payloads, references' free text and error messages (AC-044).
2. **Guard** (`tests/Encina.GuardTests/Compliance/GDPR/`, `.../LawfulBasis/`): every new public constructor, factory and method.
3. **Contract** (`tests/Encina.ContractTests/Compliance/GDPR/`): one `IProcessingActivityRegistry` contract (validation rejects, tenant isolation, shared fallback, duplicate per tenant) run against the in-memory registry; the same contract base reused by the provider integration tests; RoPA JSON and CSV snapshots through `Encina.Testing.Verify` with the S18 activities (two rows, Art. 6(1)(b) and (c), both Art. 9(2)(h), Spanish references); `ILawfulBasisServiceContractTests` extended for the new command.
4. **Property** (`tests/Encina.PropertyTests/Compliance/`): FsCheck round-trips — ground and references through mapper → entity → mapper, through events → projection, and through JSON export; validator invariant "categories non-empty and condition `NotDeclared` is always `Left`"; draft condition with the option off is always `Left` and, written through the mapper, reads back unchanged (AC-024 for #815's room).
5. **Integration**: the 10 existing `ProcessingActivityRegistry*Tests` (`tests/Encina.IntegrationTests/ADO/*/ProcessingActivity/`, `Dapper/*/ProcessingActivity/`, `Infrastructure/EntityFrameworkCore/*/ProcessingActivity/`, `Infrastructure/MongoDB/ProcessingActivity/`) gain the new columns, the validation rejection and a two-tenant test (tenant A never reads or exports tenant B's activity), on their shared `[Collection]` fixtures; `tests/Encina.IntegrationTests/Compliance/LawfulBasis/LawfulBasisAggregateIntegrationTests.cs` gains the new event, projection and two-tenant query on Marten/PostgreSQL through Testcontainers.
6. **Load**: justification file `tests/Encina.LoadTests/Compliance/GDPR/SpecialCategoryConditions.md` (registration and export are not concurrent hot paths).
7. **Benchmark**: justification file `tests/Encina.BenchmarkTests/Compliance/GDPR/SpecialCategoryConditions.md` (validator runs at registration, not per request).
8. **Coverage manifests**: per-file targets with one-sentence justifications for every new and touched `src/` file in both manifests; measure per flag and run `--check-justifications`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
You are implementing Phase 10 of issue #1196: tests for every flag.

CONTEXT:
- Test projects: Encina.UnitTests, Encina.GuardTests, Encina.ContractTests, Encina.PropertyTests,
  Encina.IntegrationTests (shared [Collection] fixtures, Testcontainers), Encina.LoadTests, Encina.BenchmarkTests.
- Existing tests to extend: tests/Encina.UnitTests/Compliance/GDPR/Export/*RoPAExporterTests.cs,
  tests/Encina.UnitTests/Compliance/LawfulBasisModule/**, the 10 ProcessingActivityRegistry*Tests and
  tests/Encina.IntegrationTests/Compliance/LawfulBasis/LawfulBasisAggregateIntegrationTests.cs.
- Coverage manifests: .github/coverage-manifest/Encina.Compliance.GDPR.json and Encina.Compliance.LawfulBasis.json.

TASK:
Write the unit, guard, contract, property and integration tests and the two justification files listed in the
Phase 10 Tasks; add per-file targets with justifications; measure each flag.

KEY RULES:
- Tests execute real package code; no reflection-only or type-only assertions.
- Shouldly through Encina.Testing.Shouldly; FsCheck, Verify, Bogus through their Encina.Testing wrappers.
- Integration tests use [Collection("<Family>-<Database>")], call ClearAllDataAsync in InitializeAsync, never
  create or dispose fixtures; [Trait("Category", "Integration")] and [Trait("Database", "<Db>")].
- Marten aggregates: unit tests mock IAggregateRepository with NSubstitute; integration on Marten/PostgreSQL.
- Every changed method CRAP <= 10; outputs under artifacts/.

REFERENCE FILES:
- tests/Encina.IntegrationTests/ADO/SqlServer/ProcessingActivity/ProcessingActivityRegistryADOSqlServerTests.cs
- tests/Encina.ContractTests/Compliance/LawfulBasis/ILawfulBasisServiceContractTests.cs
- tests/Encina.PropertyTests/Compliance/LawfulBasis/LawfulBasisAggregatePropertyTests.cs
- docs/testing/coverage-measurement-methodology.md
- docs/testing/integration-tests.md
```

</details>

---

### Phase 11: Documentation and Finalization

> **Goal**: Every public API documented, the change recorded, and the build and every coverage flag green.

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation on every new or changed public API (`<summary>`, `<remarks>` with the GDPR article, `<param>`, `<returns>`, `<example>` on `SpecialCategoryProcessing.Create`, `LegalBasisReference.Create` and `[LegalBasisReference]`); LOPDGDD and DA 17ª appear as examples an application configures, never as Encina defaults.
2. `changelog.d/1196-special-category-conditions.added.md`, and `changelog.d/1196-legal-basis-references.changed.md` for the removal of `LegalReference` and the tenant-scoped registry.
3. `src/Encina.Compliance.GDPR/README.md` and `src/Encina.Compliance.LawfulBasis/README.md` — the Art. 9 ground, references, validation and tenancy.
4. `docs/features/gdpr-compliance.md` and `docs/features/lawful-basis-validation.md` — usage, configuration (`LegalGroundOptions`, `RequireTenantContext`), the S18 example; follow the `encina-docs` skill (one Diátaxis quadrant per page).
5. ADR in `docs/architecture/adr/` (next free number in `index.md`): "Legal grounds: Art. 6 basis, Art. 9(2) condition and legal-basis references as one model", recording Design Choices 1, 2, 5 and 7.
6. `docs/INVENTORY.md` — new files. `ROADMAP.md` — only if the milestone line for P-08 changes. Release notes under `docs/releases/` only if the target version has a folder.
7. `PublicAPI.Unshipped.txt` of both packages and of every provider package whose public constructor changed.
8. Article-coverage rows for GDPR Art. 9(2) and LOPDGDD art. 9.2 / DA 17ª handed to #1214 and #1215 (P-17) as a comment, not written here.
9. Verification: `dotnet build Encina.slnx --configuration Release` → 0 errors, 0 warnings; `dotnet test` for each flag → all pass and each flag reaches its target in both manifests; the crap-gate table shows no changed method above 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 11</strong></summary>

```
You are implementing Phase 11 of issue #1196: documentation and finalization.

CONTEXT:
- Phases 1-10 are merged in the branch. Documentation rules: .claude/skills/encina-docs/SKILL.md.
- Changelog fragments go to changelog.d/ (see changelog.d/README.md); never edit CHANGELOG.md's Unreleased section.

TASK:
Write the XML documentation, the two changelog fragments, the two package README updates, the two feature pages,
the ADR and the inventory entry listed in the Phase 11 Tasks, then run the build, the per-flag tests and the
crap-gate check.

KEY RULES:
- English only; no hand-typed coverage figures (use covref markers, SPEC-001).
- Spanish law appears as an example an application configures; Encina never hard-codes a jurisdiction.
- Zero warnings; PublicAPI files complete (RS0016/RS0017 clean).

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
- docs/architecture/adr/index.md
- docs/features/gdpr-compliance.md
- docs/features/lawful-basis-validation.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Relevance to this plan |
|--------|-----------|------------------------|
| GDPR | Art. 6(1)(a)-(f) | The existing `LawfulBasis` enum; one basis per activity (Design Choice 3) |
| GDPR | Art. 6(3) | Art. 6(1)(c) and (e) rest on Union or Member State law: a law reference is required (Design Choice 4) |
| GDPR | Art. 9(1) | The eight special categories: `SpecialCategoryOfPersonalData` |
| GDPR | Art. 9(2)(a)-(j) | The ten conditions: `SpecialCategoryCondition`; (b), (g), (h), (i), (j) need a law, collective agreement or health-professional contract |
| GDPR | Art. 9(3), 9(4) | (h) under professional secrecy; Member States may add conditions for genetic, biometric and health data (why national references are a list with a jurisdiction) |
| GDPR | Art. 30(1)(b)-(c) | The RoPA lists purposes and categories; the export carries the new fields |
| GDPR | Art. 5(2) | Accountability: the event stream records every ground change |
| LOPDGDD (LO 3/2018) | art. 9.2, DA 17ª | Spanish acceptance case: processing under Art. 9(2)(g), (h), (i) founded on Spanish law needs a law with the rank of law; DA 17ª lists health laws |
| Ley 41/2002 | arts. 16-17 | Example reference for a clinical record (S18) |
| COM(2025) 837 (Digital Omnibus, proposal) | Draft new Art. 9(2) points | Reserved draft values, off by default (Design Choice 8, REQ-024, DEC-012, #815) |
| SPEC-002 | REQ-010, AC-010, S18, REQ-061, REQ-062, REQ-024, DEC-008, DEC-009, DEC-011, DEC-012 | The requirement, its acceptance, tenancy, telemetry and draft-law rules |
| ISO 3166-1 alpha-2 | Country codes | `LegalBasisReference.Jurisdiction` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Use in this feature |
|-----------|----------|---------------------|
| `ProcessingActivity`, entity, mapper | `src/Encina.Compliance.GDPR/Model/ProcessingActivity.cs`, `ProcessingActivityEntity.cs`, `ProcessingActivityMapper.cs` | Extended with the ground, references and tenant |
| `IProcessingActivityRegistry` on 10 providers + in-memory | `src/Encina.{ADO,Dapper}.*/ProcessingActivity/`, `src/Encina.EntityFrameworkCore/ProcessingActivity/`, `src/Encina.MongoDB/ProcessingActivity/`, `src/Encina.Compliance.GDPR/InMemoryProcessingActivityRegistry.cs` | Validate on write, tenant-aware reads |
| `IRoPAExporter` (JSON, CSV) | `src/Encina.Compliance.GDPR/Export/` | New fields; wired telemetry |
| `GDPRDiagnostics` RoPA export instruments | `src/Encina.Compliance.GDPR/Diagnostics/GDPRDiagnostics.cs:41-98` | Already declared; this plan calls them |
| `LawfulBasisAggregate`, events, projection, read model | `src/Encina.Compliance.LawfulBasis/` | Extended; one new event |
| `LawfulBasisValidationPipelineBehavior` | `src/Encina.Compliance.LawfulBasis/Pipeline/` | Condition in attribute info, conflict and explicit-consent check |
| Ambient-tenant fail-closed pattern | `src/Encina.Compliance.Consent/Services/DefaultConsentService.cs` (#1315) | `ComplianceTenantResolver` copies its decision table |
| `IRequestContextAccessor` | `src/Encina/Abstractions/IRequestContextAccessor.cs:54` | Ambient tenant |
| Shared integration fixtures | `tests/Encina.IntegrationTests/**/ProcessingActivity/`, `Compliance/LawfulBasis/` | Extended, not duplicated |
| `Encina.Testing.Verify` | testing packages | RoPA export snapshots |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.GDPR` | 8100-8199 (`EventIdRanges.ComplianceGDPR`, `EventIdRanges.cs:310`) | Used 8100-8116; this plan adds 8117-8121; no new range |
| `Encina.Compliance.LawfulBasis` | 8350-8399 (`EventIdRanges.ComplianceLawfulBasis`, `EventIdRanges.cs:319`) | Used up to 8386 (with pre-existing gaps, issue file `plan-1196-lawfulbasis-sparse-eventids.md`); this plan adds 8387-8391; 8 slots remain |

Both assemblies are already mapped in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs:68-69`, so no test map change is needed.

### Estimated File Count

| Category | New | Modified | Notes |
|----------|----:|---------:|-------|
| GDPR model, validator, options, attributes | 9 | 5 | Phases 1-2 |
| RoPA export | 0 | 3 | Phase 3 |
| LawfulBasis aggregate, events, read model, projection, service, pipeline, auto-registration | 0 | 9 | Phase 4 |
| Entity, mapper, scripts, test schemas, EF config, Mongo document | 0 | 10 | Phase 5 |
| Provider registries and their DI | 0 | 16 | Phase 6 (8 registry files, 8 `ServiceCollectionExtensions`) |
| DI, options, tenancy helper | 1 | 6 | Phases 7-8 |
| Diagnostics and logging | 0 | 4 | Phase 9 |
| Tests | ~12 | ~25 | Phase 10, including two `.md` justifications |
| Documentation | 3 | 6 | Phase 11 (ADR, two changelog fragments; READMEs, feature pages, inventory) |

### Findings in the Current Code

- The RoPA export telemetry and the lawful-basis registration counters are declared but never emitted (Summary table); Phase 9 wires them because REQ-062 requires the main operations to emit telemetry.
- The lawful-basis queries and cache keys ignore the tenant (`DefaultLawfulBasisService.cs:479`, `:490-492`, `:521-523`): the same defect #1315 fixed in Consent. Phase 8 fixes it under Design Choice 7; issue file `plan-1196-lawfulbasis-queries-ignore-tenant.md` tracks it either way.
- `LawfulBasisAutoRegistrationHostedService` swallows `Left` and registers a new stream for every type on every start (`:63-73`, `:83-93`). Phase 4 propagates `Left`; the duplicate registration is issue file `plan-1196-lawfulbasis-autoregistration-errors.md`.
- The issue states that the 10 database providers are not affected. That holds for `Encina.Compliance.LawfulBasis` only: `ProcessingActivity` is stored on all 10 providers (#681), so the matrix applies to it.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 / C# 14 library (nullable on, Railway Oriented Programming with Either<EncinaError, T>).
Issue #1196 (SPEC-002 P-08, REQ-010, AC-010, S18) adds the GDPR Art. 9(2) condition and the national legal-basis
reference next to the Art. 6 basis in Encina.Compliance.GDPR (ProcessingActivity, RoPA export) and
Encina.Compliance.LawfulBasis (Marten event-sourced registration). ProcessingActivity is stored on all 10 database
providers; LawfulBasis is Marten-only (DEC-008, ADR-019). Plan:
docs/plans/special-category-conditions-implementation-plan-1196.md. Rules: AGENTS.md.

IMPLEMENTATION OVERVIEW:
1. GDPR model: SpecialCategoryOfPersonalData, SpecialCategoryCondition (NotDeclared = 0; draft Omnibus values
   from 100, off by default), LegalBasisReferenceKind, LegalBasisReference, SpecialCategoryProcessing (factory
   returns Either), LegalGround, LegalGroundOptions, static LegalGroundValidator, new GDPRErrors codes.
2. ProcessingActivity gains SpecialCategoryProcessing, LegalBasisReferences, TenantId; [ProcessingActivity] and
   [LawfulBasis] gain SpecialCategories and SpecialCategoryCondition; repeatable [LegalBasisReference];
   LawfulBasisAttribute.LegalReference removed; in-memory registry validates and is tenant-aware; auto-registration
   fails the start on an invalid declaration.
3. JSON and CSV RoPA exports carry the Art. 6 article, the Art. 9 ground, the references and the tenant.
4. LawfulBasis events, aggregate, read model, projection and service replace LegalReference with references, add
   the ground and the SpecialCategoryGroundChanged event/command; auto-registration propagates Left; the pipeline
   compares conditions and checks consent for Art. 9(2)(a) (works once #1920 lands).
5. Entity, mapper, 3 ADO scripts, 3 test schemas, EF configuration, Mongo document: TenantId ("-" = shared),
   SpecialCategoriesJson, SpecialCategoryConditionValue, LegalBasisReferencesJson; unique (TenantId,
   RequestTypeName).
6. The 10 provider registries validate on write and read tenant row first, then shared row.
7. DI: LegalGroundOptions from both packages (TryAdd); RequireTenantContext on GDPROptions and LawfulBasisOptions.
8. Cross-cutting: ComplianceTenantResolver (Consent #1315 pattern), tenant-scoped lawful-basis queries and cache keys.
9. Observability: wire RoPA export and registration instruments with encina.tenant_id; EventIds 8117-8121 (GDPR)
   and 8387-8391 (LawfulBasis) through [LoggerMessage].
10. Tests on every flag; S18 snapshot; two-tenant tests on the 10 providers and Marten; load/benchmark .md.
11. XML docs, changelog fragments, READMEs, feature pages, ADR, inventory; build and per-flag coverage.

KEY PATTERNS:
- Factories and services return Either; aggregates keep their exception guards (#1798 decides otherwise).
- One validator, called at every write path; a contract test proves the 11 registries agree.
- Compliance gates fail closed on a missing tenant; the opt-out is explicit and logged once.
- No [Obsolete], no compatibility alias; TimeProvider for time; async database calls with CancellationToken.
- Telemetry: no payload, user-written text, subject identifier or EncinaError.Message; tenant id as encina.tenant_id.
- [LoggerMessage] with EventIds packed in the package's registered range.
- Integration tests on shared [Collection] fixtures; Marten on PostgreSQL through Testcontainers.
- Every changed method CRAP <= 10; every flag reaches its manifest target.

REFERENCE FILES:
- src/Encina.Compliance.GDPR/Model/ProcessingActivity.cs, Model/LawfulBasis.cs, GDPRErrors.cs
- src/Encina.Compliance.GDPR/ProcessingActivityEntity.cs, ProcessingActivityMapper.cs
- src/Encina.Compliance.GDPR/Export/JsonRoPAExporter.cs, Export/CsvRoPAExporter.cs
- src/Encina.Compliance.GDPR/Diagnostics/GDPRDiagnostics.cs, GDPRLogMessages.cs
- src/Encina.Compliance.LawfulBasis/Aggregates/LawfulBasisAggregate.cs, Events/LawfulBasisEvents.cs
- src/Encina.Compliance.LawfulBasis/Services/DefaultLawfulBasisService.cs
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- src/Encina.ADO.SqlServer/ProcessingActivity/ProcessingActivityRegistryADO.cs and its sibling providers
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/specifications/SPEC-002-eu-regulatory-readiness.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | No new cache; the existing lawful-basis read-model cache (`DefaultLawfulBasisService.cs:41`, `:479`) carries the new fields and its keys become tenant-scoped (Phase 8) so a cached registration is never served to another tenant |
| 2 | OpenTelemetry | ✅ | Wire the declared RoPA export span and instruments and the lawful-basis registration counters; new `gdpr.processing_activity.validation_failures`, `lawful_basis.registrations.ground_changed`, `.rejected`; `encina.tenant_id` on all (Phase 9, REQ-062) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` EventIds 8117-8121 (GDPR) and 8387-8391 (LawfulBasis) inside the registered ranges; existing 8109-8111 wired (Phase 9) |
| 4 | Health Checks | ❌ | No new dependency: the registries and the Marten store already have `ProcessingActivityHealthCheck` and `LawfulBasisHealthCheck`; invalid records cannot be written, so there is nothing new to check |
| 5 | Validation | ✅ | `LegalGroundValidator` at every write path (11 registries, lawful-basis service, attribute scans); a special-category activity without an Art. 9(2) condition fails (AC-010). A domain rule on stored records, not an `IValidationProvider` request validator |
| 6 | Resilience | ❌ | No call to an external system; database calls keep the providers' existing behaviour |
| 7 | Distributed Locks | ❌ | No shared state updated by concurrent hosts; uniqueness is enforced by the `(TenantId, RequestTypeName)` key and Marten stream ids |
| 8 | Transactions | ❌ | Each write is one row or one event append, already atomic |
| 9 | Idempotency | ⏭️ | Not a message entry point; the one duplicate-producing path is lawful-basis auto-registration on every start, deferred to issue file `plan-1196-lawfulbasis-autoregistration-errors.md` |
| 10 | Multi-Tenancy | ✅ | `TenantId` on `ProcessingActivity` on the 10 providers with shared definitions; tenant-filtered lawful-basis queries; fail closed on a missing tenant (Design Choice 7, Phases 5-8, REQ-061, AC-043) |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; the lawful-basis events keep their existing `ModuleId`; `ModuleId` in processing activities stays with #747 |
| 12 | Audit Trail | ✅ | The lawful-basis event stream records every ground change (`SpecialCategoryGroundChanged` with old and new values). Edit history of relational processing activities does not exist today and is deferred to issue file `plan-1196-ropa-edit-history.md` |

---

## Prerequisites & Dependencies

| Item | State | Relation |
|------|-------|----------|
| #1186 EU regulatory readiness (SPEC-002) | open (EPIC) | Parent |
| #1920 LawfulBasis consent check cannot work with Encina.Compliance.Consent | open | Advisable before Phase 4 item 7: the Art. 9(2)(a) consent check reuses the Art. 6(1)(a) path, which fails without the bridge. The rest of the plan does not depend on it |
| #1798 Either-returning decisions for event-sourced aggregates | open (SPIKE) | Not blocking: the service validates first; if #1798 chooses `Either`, `ChangeSpecialCategoryGround` follows that decision |
| #1198 (P-10) Processor roles in the RoPA export | open | Same exporters: coordinate column order (Phase 3 item 5) |
| #1257 (P-54) Multi-tenancy of the SPEC-002 capabilities | open | Umbrella for tenancy; does not list the RoPA, so this plan carries the RoPA tenancy (Design Choice 7) |
| #1227 (P-25) Reference scenario | open | S18 is verified there; reads S18 as two activities (Design Choice 3) |
| #815 AI model training as legitimate interest (Omnibus) | open | Builds on the reserved draft conditions (Design Choice 8) |
| #1214, #1215 (P-17) Article-coverage specifications for GDPR and LawfulBasis | open | Receive the Art. 9(2) and LOPDGDD rows (Phase 11 item 8) |
| #1591 Exception messages in activity tags and store errors | open | The registries touched here keep their old `ex.Message` uses for #1591; new code does not add any |
| Issue file `plan-1196-lawfulbasis-autoregistration-errors.md` | to open | Duplicate registration on every start and swallowed `Left`; Phase 4 fixes only the `Left` part |
| Issue file `plan-1196-lawfulbasis-queries-ignore-tenant.md` | to open | Closed by Phase 8 if Design Choice 7 is A |
| #1950 | open | Pre-existing EventId gaps in `LawfulBasisLogMessages.cs` |
| Issue file `plan-1196-ropa-edit-history.md` | to open | Deferred audit of relational RoPA edits |
| Issue file `plan-1196-explicit-consent-marker.md` | to open | Consent records cannot show that consent was explicit (Art. 9(2)(a)) |

---

## Next Steps

1. The maintainer decides Design Choices 1-9; the orchestrator records the answers in a `## Maintainer Decisions` section and updates each Chosen Option.
2. The orchestrator opens the five issue files under `artifacts/issues/plan-1196-*.md` and links them from #1196.
3. If Design Choice 7 is B, the orchestrator drafts a `[FEATURE]` issue for RoPA tenancy on the 10 providers and removes Phase 8 item 2's tenancy part from this plan.
4. Comment on #1227 (P-25) that S18 registers two activities (Design Choice 3), and on #1198 (P-10) about the export column order.
5. Link this plan from #1196 and start Phase 1 with an `issue-worker` (Sonnet; Opus only for Phase 4 if #1798 has decided for `Either` by then).
