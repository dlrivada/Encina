# Implementation Plan: `Encina.Compliance.DataSubjectRights` — Relational `IPersonalDataLocator` on the 10 Database Providers

> **Issue**: [#1248](https://github.com/dlrivada/Encina/issues/1248) (SPEC-002 tracking id **P-45**, priority **P0**)
> **Type**: Feature
> **Complexity**: High (8 phases, 10 database providers, ~75 files, 11 pull requests)
> **Estimated Scope**: ~1,800-2,400 lines of production code + ~2,500-3,200 lines of tests
> **Milestone**: v0.17.0 — Compliance Lifecycle
> **Parent EPIC**: [#1186](https://github.com/dlrivada/Encina/issues/1186) — EU regulatory readiness (SPEC-002)
> **Specification**: [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) REQ-054, AC-039, scenarios S14 and S23, DEC-011 (d); cross-cutting REQ-061 (AC-043) and REQ-062 (AC-044)
> **Consumers**: [#1188](https://github.com/dlrivada/Encina/issues/1188) (erasure with retention, P-02), [#1189](https://github.com/dlrivada/Encina/issues/1189) (blocked data, P-03; its plan already adapts this locator through `IBlockedDataLocator`), [#1195](https://github.com/dlrivada/Encina/issues/1195) (DSR access export, P-07)
> **Related**: [#1257](https://github.com/dlrivada/Encina/issues/1257) (multi-tenancy of the SPEC-002 capabilities), `Encina.Marten.GDPR` (the only locator shipped today)

---

## Summary

Data subject erasure, access export and blocking find a subject's data through `IPersonalDataLocator` (`src/Encina.Compliance.DataSubjectRights/Abstractions/IPersonalDataLocator.cs`). Encina ships exactly one implementation, `MartenEventPersonalDataLocator` (`src/Encina.Marten.GDPR/Locator/`), which covers event streams. Data held in EF Core, Dapper or ADO.NET tables, and MongoDB documents, is reached by no locator, so an application has to write its own on each provider. SPEC-002 DEC-011 (d) rejects that and puts a relational locator in 1.0; scenarios S14 (partial erasure) and S23 (one export from EF Core tables and Marten streams) depend on it.

Today:

- `PersonalDataAttribute` (`Attributes/`) marks properties with category, erasable, portable and legal-retention flags, and `PersonalDataMap` (internal) scans assemblies for them, but no code maps a **data subject to rows**: there is no declaration of which property holds the subject's key.
- `PersonalDataLocation` (`Model/`) already describes one located field: `EntityType`, `EntityId`, `FieldName`, `Category`, `IsErasable`, `IsPortable`, `HasLegalRetention`, `CurrentValue`.
- `CompositePersonalDataLocator` already combines every locator it is given (its constructor takes `IEnumerable<IPersonalDataLocator>`) and returns `DSRErrors.LocatorFailed` when one fails ("the inventory would be incomplete"). Nothing in `src/` registers it as the resolved `IPersonalDataLocator`: the Marten package registers its locator additively and its start-up validator stops the host when several locators are registered and the resolved one is not a composite. Registering the composite as an unkeyed `IPersonalDataLocator` today would recurse (its constructor resolves every `IPersonalDataLocator`, itself included). Design Choice 8 settles who owns that registration.
- The provider packages (`Encina.ADO.*`, `Encina.Dapper.*`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`) already reference `Encina.Compliance.DataSubjectRights`.

This plan delivers:

1. **Metadata**: a `[DataSubjectKey]` attribute (and a fluent override) that declares, per entity, the property holding the subject's key; a catalog that combines it with the `[PersonalData]` fields and is validated at start-up (an entity with personal data but no subject key stops the host).
2. **One provider-neutral engine** in the DSR package, `RelationalPersonalDataLocator`, which walks the catalog, applies tenant scope, calls a small provider seam `IRelationalRowSource` per entity, maps the rows to `PersonalDataLocation` and fails as a whole when any entity fails.
3. **Ten provider implementations of the seam** (ADO.NET x3, Dapper x3, EF Core covering SqlServer, PostgreSQL and MySQL through the model, MongoDB) with the registration `AddEncinaPersonalDataLocator{Provider}()` each.
4. The locator's output addresses each row (`EntityType` + primary key in `EntityId` + `FieldName`), which is what the erasure strategy of P-02, the exporter of P-07 and the blocking adapter of P-03 need.

**Standards**: GDPR Art. 15 and Art. 20 (access and portability), Art. 17 (erasure), Art. 30; LOPDGDD art. 32 (blocking); SPEC-002 REQ-004, REQ-005, REQ-054.

**Affected packages**: `Encina.Compliance.DataSubjectRights` (metadata, engine, seam, diagnostics); `Encina.ADO.{SqlServer,PostgreSQL,MySQL}`, `Encina.Dapper.{SqlServer,PostgreSQL,MySQL}`, `Encina.EntityFrameworkCore`, `Encina.MongoDB` (one row source and one registration each).

**Provider category**: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (one package, three provider variants tested), MongoDB (AGENTS.md section 5).

**Size**: about 75 files, so the work ships as eleven worker pull requests. Each one builds and passes alone with its own tests, manifest rows, PublicAPI lines and DI test, counted below including all of those files, and stays within 15 files and 1,500 lines (`pr-size`):

| PR | Content | Files (incl. tests, manifest, PublicAPI) |
|----|---------|------------------------------------------|
| 1 | Metadata and catalog: `DataSubjectKeyAttribute`, `RelationalPersonalDataEntity`, `RelationalRow`, `RelationalPersonalDataCatalog`, `DSRErrors` (modify), `PublicAPI.Unshipped.txt`, manifest, catalog unit tests, guard tests | 9 |
| 2 | Composite registration (Design Choice 8): DSR `ServiceCollectionExtensions` (modify), Marten.GDPR `ServiceCollectionExtensions` (modify), two `PublicAPI.Unshipped.txt`, two manifests, DI test, unit test, guard test | 9 |
| 3 | Telemetry: `DataSubjectRightsDiagnostics` (modify), `RelationalLocatorLogMessages`, `EventIdRanges.cs`, `EncinaEventIdAllocationTests`, `PublicAPI.Unshipped.txt` (Encina and DSR), manifest, telemetry unit test, guard test | 9 |
| 4 | Engine: `IRelationalRowSource`, `RelationalPersonalDataLocator`, options, start-up validator, `AddEncinaRelationalPersonalData` (modify DSR `ServiceCollectionExtensions` again), `PublicAPI.Unshipped.txt`, manifest, engine, validator and DI unit tests (with an in-memory row source in the test project), guard test | 12 |
| 5 | SQL Server: ADO.NET and Dapper row sources, two registrations (modify), two `PublicAPI.Unshipped.txt`, two manifests, shared integration test entities, two integration test classes, one DI test | 12 |
| 6 | PostgreSQL: same set as PR 5 without the shared entities | 11 |
| 7 | MySQL: same set as PR 6 | 11 |
| 8 | EF Core: `PersonalDataRowSourceEF`, `EfPersonalDataCatalogAdapter`, registration (modify), `PublicAPI.Unshipped.txt`, manifest, integration test classes for SQL Server, PostgreSQL and MySQL, DI test | 10 |
| 9 | MongoDB: row source, registration (modify), `PublicAPI.Unshipped.txt`, manifest, integration test, DI test | 6 |
| 10 | Cross-provider contract suite, property tests, load and benchmark justification `.md` files | 6 |
| 11 | Documentation: eight READMEs, the DSR feature page, `docs/INVENTORY.md`, changelog fragment | 11 |

PR 5 to 9 need PR 4 merged; PR 4 needs PRs 1 to 3; no PR references an unmerged package (all work is inside the existing packages).

---

## Design Choices

<details>
<summary><strong>1. Architecture — one neutral engine plus a per-provider row source</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Engine in `Encina.Compliance.DataSubjectRights` (catalog, tenant scope, mapping to `PersonalDataLocation`, failure policy, telemetry) and a seam `IRelationalRowSource` implemented once per provider family** | The compliance logic exists once and cannot drift between providers; each provider writes only its query; the provider packages already reference the DSR package | One more abstraction |
| **B) A complete locator class in each of the 10 provider packages** | No seam | Ten copies of tenant handling, failure policy and mapping; the drift the provider-coherence rule exists to prevent |
| **C) A new package `Encina.Compliance.DataSubjectRights.Relational`** | Keeps the DSR package smaller | A package that every provider package would also have to reference; no gain, because the DSR package already holds `Locators/` |

### Chosen Option: **A — neutral engine plus `IRelationalRowSource`**

### Rationale

- Provider coherence: abstractions and options live in the shared package, providers differ only in implementation (AGENTS.md section 3 and 5).
- Provider classes follow `{Feature}{Provider}` naming: `PersonalDataRowSourceADO`, `PersonalDataRowSourceDapper`, `PersonalDataRowSourceEF`, `PersonalDataRowSourceMongoDB`, each in a `PersonalData/` folder named after the feature (AGENTS.md section 4). ADO.NET and Dapper exist once per database (SqlServer, PostgreSQL, MySQL) because the SQL differs; EF Core is one class because EF builds the SQL from the model, and its integration test runs on all three databases.
- A located field's address is `EntityType` (the CLR type), `EntityId` (the row's primary key, converted to a string) and `FieldName` (the property name), so an erasure strategy can update exactly that column of that row.

**Class:** Settled
**Settled by:** AGENTS.md section 5 (every provider-dependent feature on all 10 providers) and section 3 (provider coherence); the issue's "Affected Packages" and comment of 2026-10-17 ("entity metadata ... EF reads the model, Dapper and ADO.NET use registered entity maps")

</details>

<details>
<summary><strong>2. Subject-Key Declaration — <code>[DataSubjectKey]</code> on the property, with a fluent override</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Attribute `[DataSubjectKey]` on the property that holds the subject's key (the entity's own id for a person table, a foreign key for a child table), plus `AddPersonalDataEntity<T>(b => b.SubjectKey(x => x.PatientId))` for types that cannot carry attributes** | Next to `[PersonalData]`, the discovery that already exists; explicit; fluent form covers third-party entity types | Two ways to declare |
| **B) Fluent registration only** | One way | Metadata far from the model; every entity needs registration code even when attributes exist |
| **C) Convention (`SubjectId`, `UserId`, `PatientId` by name)** | No declaration | Wrong for any entity whose column is named otherwise; `DefaultDataSubjectIdExtractor` already shows the failure of guessing |

### Chosen Option: **A — attribute with fluent override**

### Rationale

- `PersonalDataAttribute` (property level, `Encina.Compliance.DataSubjectRights`) is the existing convention; `RestrictProcessingAttribute.SubjectIdProperty` is the existing precedent for naming the subject property explicitly.
- An entity with at least one `[PersonalData]` field and no subject key is a start-up error (fail closed): an undeclared entity would be silently missing from every erasure and export.
- An entity may declare one subject key. Several subject keys per row (a row about two people) are rejected by the validator and left to application-written locators.

**Class:** Trivial
**Settled by:** Existing convention: `src/Encina.Compliance.DataSubjectRights/Attributes/PersonalDataAttribute.cs` and `RestrictProcessingAttribute.SubjectIdProperty`; AGENTS.md section 3 ("Compliance and security gates fail closed")

</details>

<details>
<summary><strong>3. Table and Column Source for Dapper and ADO.NET — reuse repository mappings or a dedicated personal-data map</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse the registered `IEntityMapping<TEntity,TId>` (table, id column, property-to-column map) that the Dapper and ADO.NET repositories already use** | No second mapping; table and column names cannot disagree with the repository | Only entities registered with a repository are located; a table read through query handlers only (S13 style) has no mapping |
| **B) A dedicated map registered for personal data, `AddPersonalDataEntity<T>(table, idColumn, subjectColumn)`, with columns by property name unless overridden** | Works for any table, with or without a repository; the compliance scope is explicit | A second mapping that can disagree with the repository's |
| **C) B, but taking table and columns from a registered `IEntityMapping` when one exists and from the dedicated registration otherwise** | Covers both cases | Two sources to explain; precedence rules to test |

### Chosen Option: **B — dedicated map (recommended, pending the maintainer)**

### Rationale

- Scenario in which A wins: an application whose personal data lives only in tables behind Encina repositories and that wants no duplicated mapping. The issue comment of 2026-10-17 says "entity metadata ... EF reads the model, Dapper and ADO.NET use registered entity maps", which reads as A (the registered repository maps) or as C; B is the reading in which "registered entity maps" means the maps this feature registers.
- Scenario in which B wins: the reference application reads appointments through Dapper query handlers (SPEC-002 S13); those tables have no `IEntityMapping`, so A would silently omit them from erasure and export.
- Scenario in which C wins: both kinds of table in one application and the maintainer prefers fewer registrations.
- Recommendation B because the compliance scope should be explicit and must not depend on whether a repository happens to exist; a validator in Phase 2 warns (not fails) when a repository mapping and a personal-data map name different tables for the same entity type. EF Core is unaffected: it reads the model.

**Class:** Dilemma
**Settled by:** none; awaits the maintainer

</details>

<details>
<summary><strong>4. Completeness — any failing entity fails the whole locate; no silent truncation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Return `Left` when any entity's query fails or the row cap is exceeded; never a partial inventory** | An erasure or export never claims completeness it does not have; consistent with `CompositePersonalDataLocator` | One broken table blocks the request until fixed |
| **B) Return the rows found and log the failing entity** | The request continues | A subject's data is left behind or omitted from an export while the response says success; the failure AGENTS.md section 3 forbids |
| **C) Return partial rows plus a list of failed entities in the result** | Informative | `IPersonalDataLocator` returns a list of locations; the contract has no place for it, and callers would have to remember to check |

### Chosen Option: **A — fail as a whole**

### Rationale

- `RelationalPersonalDataLocatorOptions.MaxRowsPerEntity` (default 10,000) bounds memory; exceeding it returns `Left(dsr.locator_row_limit_exceeded)` instead of truncating. A subject with more rows is served by raising the limit explicitly. The category is `Internal` (ADR-062 R4: the cap is an operator-configured resource limit of the store read, not a field of the request); all new codes and their categories are in Phase 1.
- Provider exceptions are caught in the row source and returned as `Left` with the exception type only (never its message, never the subject id); `OperationCanceledException` propagates.
- The engine reads entities sequentially on one connection or context; parallel reads are not worth the extra connections for a request that is neither frequent nor latency-critical.

**Class:** Settled
**Settled by:** AGENTS.md section 3 ("Errors are NEVER swallowed in background infrastructure", "Compliance and security gates fail closed"); existing behavior of `CompositePersonalDataLocator` (`DSRErrors.LocatorFailed`, "the inventory would be incomplete")

</details>

<details>
<summary><strong>5. Subject-Id Typing — parse the string subject id to the key's CLR type; unsupported types fail at start-up</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Parse the string subject id to the declared key property's type (`string`, `Guid`, `int`, `long`, and any type with a registered converter); a key type that cannot be parsed is a start-up error; a subject id that does not parse returns `Left(dsr.subject_id_invalid)`** | Any key type of #1149 works; failures are loud | Needs the converter contract of the subject-id work |
| **B) Compare as strings in SQL (`CAST(col AS text) = @s`)** | No parsing; satisfies REQ-016 literally (any key type is reachable) | Defeats indexes; different casing rules per database (GUID formatting), so a subject can be silently missed or another matched: wrong results in a compliance gate, which AGENTS.md section 3 ("fail closed") forbids |
| **C) Support `string` only** | Simple | Contradicts REQ-016 (the reference application's patient id is a `Guid`) |

### Chosen Option: **A — typed parsing**

### Rationale

- The inverse of the string conversion used by consent and DSR (`SubjectIdConversion` in `Encina.Compliance.DataSubjectRights`, and the shared `IDataSubjectIdConverter` that #1193 introduces in core `Encina`) is used, so the string that DSR carries and the key in the table agree.
- EF Core uses the model's own value converters for strongly-typed ids; Dapper and ADO.NET bind a typed parameter; MongoDB queries the field with the typed value.
- The locate never falls back to the authenticated caller as the subject.
- `EntityId` is a single string, so an entity with a composite primary key is a start-up error (an injective join of several key parts into one string is not defined here); it is added to the catalog validation list of Phase 2 and its unit tests. `dsr.subject_id_invalid` is `Validation` (ADR-062 R3).

**Class:** Settled
**Settled by:** SPEC-002 REQ-016 and AC-016 (subject ids of any type; unreadable subject fails closed, no silent fallback) and AGENTS.md section 3 (fail closed: B returns silently wrong results, C contradicts REQ-016); issue #1149 (closed)

</details>

<details>
<summary><strong>6. Tenant Scope — filter rows by tenant, fail closed when the tenant is missing</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Entities that are tenant-scoped (`ITenantEntity`, or a tenant column declared in the map, or an EF tenant query filter) are filtered by the ambient tenant; with tenancy required (`DataSubjectRightsOptions.RequireTenantContext`) and no tenant, return `Left(dsr.tenant_required)`; with tenancy required, an entity with no tenant declaration is a start-up error unless it is explicitly marked shared (`SharedAcrossTenants()` on the builder or `[SharedAcrossTenants]`)** | Tenant A never reads tenant B's rows (AC-043); no entity is silently unfiltered; reuses the existing option | One more marker for genuinely shared tables |
| **B) Rely on EF global filters and provider tenant stores only** | Nothing to add on EF | Dapper, ADO.NET and MongoDB have no equivalent filter; the locator would cross tenants |
| **C) Always require a tenant** | Strict | Breaks single-tenant applications; REQ-061 requires single-tenant applications to work with no tenant configuration |

### Chosen Option: **A — explicit tenant filter, fail closed**

### Rationale

- `ITenantEntity` (`Encina.Tenancy`) and `ITenantEntityMapping` (ADO.NET and Dapper tenancy) already say which column holds the tenant; the personal-data map accepts the same declaration.
- Tenancy off: the engine adds no tenant predicate at all and passes `tenantId = null` to the row source. There is no literal "default tenant" value to filter by (main has no default-tenant constant, `DefaultConsentService` uses a null tenant), so a real tenant that happens to be called `default` cannot clash.
- "Tenancy required" is `DataSubjectRightsOptions.RequireTenantContext` when set, else auto-detected from the presence of `ITenantProvider` (the Consent decision table: `src/Encina.Compliance.Consent/Services/DefaultConsentService.cs`, `_isMultiTenantApplication = tenantProvider is not null`). The ambient tenant is read through `IRequestContextAccessor`, never from request data.
- Fail closed on undeclared entities: with tenancy required, the catalog validation (Phase 2) turns "no tenant column, no `ITenantEntity`, no EF tenant filter, not marked shared" into a start-up error, so subject id `42` of tenant A and `42` of tenant B in an undeclared table can never both be returned (cross-tenant disclosure on export, over-erasure through P-02). Unit tests cover the error and the marked-shared exception.
- A two-tenant integration test on every provider shows tenant A's request never returns tenant B's rows.

**Class:** Settled
**Settled by:** SPEC-002 REQ-061 and AC-043; `DataSubjectRightsOptions.RequireTenantContext` (`src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs`); AGENTS.md section 3 (fail closed: B reads across tenants, C breaks single-tenant applications, and an unfiltered entity under A is a start-up error)

</details>

<details>
<summary><strong>7. What Is Read — project only the needed columns and fill <code>CurrentValue</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Select the primary key and the `[PersonalData]` columns only (EF: a projection, never full entity materialization); fill `CurrentValue` for each located field** | Minimal data in memory; export (P-07) gets values; erasure and blocking ignore them | Personal values pass through the engine |
| **B) Select ids only and let each consumer load values** | No personal values in the locator | Export would re-read every row; two code paths |
| **C) Materialize full entities** | Simple on EF | Loads unrelated columns, including secrets; tracking overhead |

### Chosen Option: **A — projected columns with `CurrentValue`**

### Rationale

- `PersonalDataLocation.CurrentValue` is the existing contract; the Marten locator fills it only where decryption allows.
- Values never reach logs, activity tags or metric tags (REQ-062); the engine handles them as opaque objects and releases the list to the caller.
- A field whose value is `null` is still returned as a location (the field exists for this subject); consumers decide what to do with empty values.

**Class:** Settled
**Settled by:** Existing contract `src/Encina.Compliance.DataSubjectRights/Model/PersonalDataLocation.cs`; SPEC-002 REQ-062 (no payloads or direct identifiers in telemetry)

</details>

<details>
<summary><strong>8. Who Registers the Composite — keyed leaf locators plus one composite, owned by this plan</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) This plan (PR 2) adds `AddPersonalDataLocator<TLocator>()` in the DSR package: every leaf locator is registered as a keyed `IPersonalDataLocator` (key = locator type) and one unkeyed registration resolves `CompositePersonalDataLocator` over `GetKeyedServices<IPersonalDataLocator>(KeyedService.AnyKey)`; the Marten registration switches to the same helper in the same PR** | One owner; no recursion (the composite never sees itself); "relational only", "relational + Marten" and "app locator + relational" all resolve to a composite that reaches every leaf; the Marten start-up validator then passes | PR 2 edits the Marten registration (one file) |
| **B) A separate prerequisite issue, owned by the DSR/Marten maintainers, changes `CompositePersonalDataLocator` to take a leaf marker interface and registers it; this plan waits for it** | Core change reviewed on its own | This plan cannot ship its DI test until that issue merges; a new owner and a new issue for a two-file change |
| **C) The relational registration replaces `IPersonalDataLocator` (last registration wins)** | No helper | With Marten also registered the host stops at start-up (its validator requires a composite); the Marten locator is silently dropped otherwise: an incomplete inventory |

### Chosen Option: **A — owned by this plan (recommended, pending the maintainer)**

### Rationale

- Scenario in which A wins: an application with Marten streams and relational tables (S23), which needs one resolved locator that reaches both.
- Scenario in which B wins: the maintainer wants the shared locator wiring reviewed as a core change before any provider feature relies on it.
- Recommendation A because C is unsafe (incomplete inventory) and B adds an owner for two files. The PR 2 DI test proves, with `ValidateOnBuild` and `ValidateScopes`, that "relational only", "relational + Marten" and "application locator + relational" resolve `IPersonalDataLocator` to a composite that returns the union of all leaves, and that the Marten start-up validator accepts each combination.

**Class:** Dilemma
**Settled by:** none; awaits the maintainer

</details>

<details>
<summary><strong>9. Two Stores in One Application — route each entity to its row source by store key</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IRelationalRowSource` has a `StoreKey` (default `"default"`); an entity names its store (`[DataSubjectKey(Store = "mongo")]` or the builder's `InStore("mongo")`); the engine takes `IEnumerable<IRelationalRowSource>` and routes per entity; two sources with the same key, or an entity naming an unknown store, are start-up errors** | One locator serves EF Core SQL Server plus MongoDB, or two `DbContext`s; nothing silently ignored | A store-key concept in the public seam |
| **B) One locator instance per provider, each with its own catalog slice, all combined by the composite of Design Choice 8** | No routing in the engine | Catalog slicing and one registration per store with its own options; more DI to test |
| **C) One source per container (`TryAdd`), the second registration ignored** | Simplest | An entity of the second store is queried against the first (wrong table) or silently dropped: an incomplete inventory |

### Chosen Option: **A — store key routing (recommended, pending the maintainer)**

### Rationale

- Scenario in which A wins: an application with relational tables and a MongoDB collection, or two databases, served by one locator and one catalog.
- Scenario in which B wins: stores owned by different modules that must keep separate options and failure handling.
- Recommendation A; C is rejected because it fails open (AGENTS.md section 3). Whatever is chosen, two registrations are never silent: a duplicate store key stops the host at start-up.

**Class:** Dilemma
**Settled by:** none; awaits the maintainer

</details>

<details>
<summary><strong>10. Filtered and Soft-Deleted Rows — the locator reads every stored row</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The locator reads all rows regardless of EF query filters other than the tenant filter (EF 10 named query filters: the tenant filter is replaced by the explicit tenant predicate and all other named filters such as soft delete are ignored with `IgnoreQueryFilters(names)`); ADO.NET, Dapper and MongoDB select regardless of any soft-delete flag; the documentation says so** | Export and erasure cover all stored data (GDPR Art. 15 and 17 apply to every stored copy); the inventory is complete | A soft-deleted row appears in an export, and the consumer cannot tell it was hidden |
| **B) The locator respects the application's filters (a soft-deleted row is not located)** | Matches what the application shows | Hidden rows keep personal data that erasure and export never reach; the inventory claims completeness it does not have |
| **C) A (all rows), and each `RelationalRow` carries which filter hid it so consumers can decide** | Informative | Extra contract surface for the consumers of #1188 and #1195 |

### Chosen Option: **A — all stored rows (recommended, pending the maintainer)**

### Rationale

- Scenario in which A wins: an application that soft-deletes patients and must still erase or export them on request (Art. 15 and 17 cover all stored data).
- Scenario in which B wins: an application whose filters model "not yet published" drafts that must not be exported.
- Scenario in which C wins: consumers that must treat archived rows differently from live ones.
- Recommendation A; B is rejected because it reports a complete inventory it does not have. Tests: a soft-deleted row is located on every provider (Phase 7).

**Class:** Dilemma
**Settled by:** none; awaits the maintainer

</details>

---

## Implementation Phases

### Phase 1: Core Models, Attributes and Errors

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.Compliance.DataSubjectRights/`

PR 1 (see the split table).

1. **`Attributes/DataSubjectKeyAttribute.cs`** (new) — `[AttributeUsage(AttributeTargets.Property)] public sealed class DataSubjectKeyAttribute : Attribute` with an optional `Store` (Design Choice 9); `[SharedAcrossTenants]` is a class-level marker in the same file set (Design Choice 6); XML docs with an example
2. **`Locators/Relational/RelationalPersonalDataEntity.cs`** (new, public sealed record) — `EntityType`, `StoreKey`, `StoreName` (table or collection), `IdProperty`, `SubjectKeyProperty`, `TenantProperty?`, `IsSharedAcrossTenants`, `IReadOnlyList<PersonalDataField> Fields`, `IReadOnlyDictionary<string,string> Columns` (property to column)
3. **`Locators/Relational/RelationalRow.cs`** (new) — `string EntityId`, `IReadOnlyDictionary<string, object?> Values` (property name to value)
4. **`Locators/Relational/RelationalPersonalDataCatalog.cs`** (new) — see Phase 2 task 2 (it lands in PR 1 with its unit tests so the metadata is verifiable alone)
5. **`DSRErrors.cs`** (modify; ADR-062 categories, `EncinaErrors.Create(code, category, ...)`, each with a `GetCategory` test) —
   - `dsr.locator_row_limit_exceeded`: `Internal` (R4; the cap is an operator-set limit of the store read)
   - `dsr.subject_id_invalid`: `Validation` (R3)
   - `dsr.tenant_required`: exists (`DSRErrors.TenantRequiredCode`), category `InvalidRequest` (R3, a missing tenant); the existing factory gets the category if main does not carry it yet
   - `dsr.locator_entity_failed`: `Internal` (R4; details: entity type name, provider, exception type; never the subject)
   - `dsr.catalog_invalid`: `Internal` (R1/R4, a wiring error found at start-up)
6. **`PublicAPI.Unshipped.txt`** (modify)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Issue #1248 (SPEC-002 P-45). Package src/Encina.Compliance.DataSubjectRights. PersonalDataAttribute, PersonalDataField,
  PersonalDataLocation, PersonalDataCategory and the internal PersonalDataMap exist; there is no subject-key declaration.
- Pre-1.0: no [Obsolete], no compatibility layers.

TASK:
Add DataSubjectKeyAttribute, RelationalPersonalDataEntity, RelationalRow and the new DSRErrors codes listed in the
Phase 1 tasks, with full XML docs and PublicAPI.Unshipped.txt entries.

KEY RULES:
- Errors carry a code and non-identifying details (entity type, provider, exception type); never the subject id,
  never exception messages.
- Records are immutable; nullable annotations exact.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Attributes/PersonalDataAttribute.cs
- src/Encina.Compliance.DataSubjectRights/Model/PersonalDataLocation.cs
- src/Encina.Compliance.DataSubjectRights/DSRErrors.cs
```

</details>

---

### Phase 2: Catalog, Seam and Engine

<details>
<summary><strong>Tasks</strong></summary>

Catalog (task 2) is PR 1; the seam, engine, options and validator (tasks 1, 3, 4, 5) are PR 4.

1. **`Locators/Relational/IRelationalRowSource.cs`** (new) — `string StoreKey { get; }` and `ValueTask<Either<EncinaError, IReadOnlyList<RelationalRow>>> ReadRowsAsync(RelationalPersonalDataEntity entity, object subjectKey, string? tenantId, int maxRows, CancellationToken ct)`; XML docs state the contract every provider honours (typed parameter, tenant filter only when `tenantId` is not null, projection, all stored rows regardless of non-tenant filters (Design Choice 10), `maxRows + 1` probe, `Left` on exceptions)
2. **`Locators/Relational/RelationalPersonalDataCatalog.cs`** (new, PR 1) — builds `RelationalPersonalDataEntity` from `[PersonalData]` + `[DataSubjectKey]` (reusing `PersonalDataMap` scanning) and from `AddPersonalDataEntity<T>` builders; validates and returns `dsr.catalog_invalid` for: subject key missing, more than one key per entity, unsupported key type, composite primary key, duplicate registration, a table or column identifier outside the allow-list (letters, digits, underscore, optionally one schema dot, so a `]` or a backtick can never break the provider quoting), no tenant declaration while tenancy is required and the entity is not marked shared (Design Choice 6), an entity naming an unknown store key (Design Choice 9); each rule has a unit test; EF additionally reads the model through its own catalog adapter
3. **`Locators/Relational/RelationalPersonalDataLocator.cs`** (new, `IPersonalDataLocator`, scoped) — constructor `(IRelationalPersonalDataCatalog, IEnumerable<IRelationalRowSource>, IRequestContextAccessor, IOptions<DataSubjectRightsOptions>, IOptions<RelationalPersonalDataLocatorOptions>, ILogger<…>)` (the ambient context is read through `IRequestContextAccessor`, as `DefaultConsentService` does; "tenancy required" follows Design Choice 6); `LocateAllDataAsync`: parse the subject id per entity key type -> tenant check -> per entity route to the row source of its `StoreKey` and `ReadRowsAsync` -> map each row and each `[PersonalData]` field to a `PersonalDataLocation` -> any `Left` fails the whole locate
4. **`Locators/Relational/RelationalPersonalDataLocatorOptions.cs`** (new) — `MaxRowsPerEntity` (default 10,000, must be positive)
5. **`Locators/Relational/RelationalPersonalDataStartupValidator.cs`** (new, hosted, singleton) — runs the catalog validation at start-up inside its own scope (`IServiceScopeFactory`: the EF catalog adapter needs the scoped `DbContext`) and fails the host on any catalog error and on two row sources with the same `StoreKey`. Lifetimes: the locator, the EF row source and the EF adapter are scoped; the ADO.NET and Dapper row sources are singletons (they hold a connection string and open a connection per call); the DI test builds the provider with `ValidateOnBuild` and `ValidateScopes` and proves no scoped service is injected into a singleton

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- IPersonalDataLocator.LocateAllDataAsync(string subjectId, CancellationToken) returns
  Either<EncinaError, IReadOnlyList<PersonalDataLocation>>. CompositePersonalDataLocator combines all registered
  locators and fails when one fails. DataSubjectRightsOptions.RequireTenantContext exists.
- The Marten locator (src/Encina.Marten.GDPR/Locator/MartenEventPersonalDataLocator.cs) is the style reference:
  never logs the subject id, returns Left with an error code on provider failure.

TASK:
Implement the seam IRelationalRowSource, the catalog with validation, RelationalPersonalDataLocator, its options and
the start-up validator as listed in the Phase 2 tasks.

KEY RULES:
- Any failing entity fails the whole locate; no partial inventory; row cap exceeded is Left, not truncation.
- Subject id parsed to the key's CLR type; invalid id is Left(dsr.subject_id_invalid); never fall back to the caller.
- Tenant: read it through IRequestContextAccessor; filter tenant-scoped entities by it; Left(dsr.tenant_required) when required and absent; with tenancy off add no tenant predicate; with tenancy required an undeclared, unshared entity is a start-up error.
- Routing: one row source per store key; a duplicate or unknown store key is a start-up error, never silent.
- Subject ids and values never appear in logs, tags or errors; exceptions logged with ForLogging().
- ROP: Either everywhere, no exceptions for business outcomes; OperationCanceledException propagates.
- Methods keep CRAP <= 10: split parsing, tenant check, row mapping and aggregation into small methods.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Locators/CompositePersonalDataLocator.cs
- src/Encina.Compliance.DataSubjectRights/PersonalDataMap.cs
- src/Encina.Compliance.DataSubjectRights/SubjectIdConversion.cs
- src/Encina.Marten.GDPR/Locator/MartenEventPersonalDataLocator.cs
```

</details>

---

### Phase 3: Configuration, DI and Auto-Registration

<details>
<summary><strong>Tasks</strong></summary>

1. **Composite registration (PR 2, Design Choice 8)** — `AddPersonalDataLocator<TLocator>()` in the DSR `ServiceCollectionExtensions` (modify) registers the leaf as a keyed `IPersonalDataLocator` and one unkeyed composite over `GetKeyedServices<IPersonalDataLocator>(KeyedService.AnyKey)`; `Encina.Marten.GDPR/ServiceCollectionExtensions.cs` (modify) calls it for the Marten locator. DI test (`ValidateOnBuild`, `ValidateScopes`): "Marten only", "relational only", "relational + Marten", "application locator + relational" each resolve a composite that returns the union of all leaves and pass the Marten start-up validator
2. **`ServiceCollectionExtensions` (DSR package, PR 4)** (modify) — `AddEncinaRelationalPersonalData(Action<RelationalPersonalDataBuilder>? configure = null)`: options, catalog (auto-discovery from `AssembliesToScan` when `AutoRegisterFromAttributes`), validator, and the locator through `AddPersonalDataLocator<RelationalPersonalDataLocator>()` (scoped leaf); `AddPersonalDataEntity<TEntity>(…)` on the builder. Row sources are registered as `IEnumerable<IRelationalRowSource>` entries (`AddSingleton`/`AddScoped` per provider, one per store key); a second source with an existing store key stops the host at start-up
3. **Provider registrations (PRs 5 to 9)** — one per package, called before the DSR registration: `AddEncinaPersonalDataLocatorADOSqlServer(connectionString)`, `…ADOPostgreSQL`, `…ADOMySQL`, `…DapperSqlServer`, `…DapperPostgreSQL`, `…DapperMySQL`, `AddEncinaPersonalDataLocatorEFCore<TDbContext>()`, `AddEncinaPersonalDataLocatorMongoDB()`, following `AddEncinaProcessingActivityADOPostgreSQL` and `AddEncinaProcessingActivityEFCore`
4. **DI tests** — PR 2 and PR 4 prove the DSR-side registration with an in-memory row source of the test project; each provider PR (5 to 9) adds the DI test of its own registration with `ValidateOnBuild` and `ValidateScopes`, so no PR lists a test whose registration lives in a later PR

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Neighbouring registrations: AddEncinaProcessingActivityADOPostgreSQL (src/Encina.ADO.PostgreSQL/
  ServiceCollectionExtensions.cs), AddEncinaProcessingActivityEFCore, and the Marten locator registration (switched to the keyed helper of Design Choice 8) in
  src/Encina.Marten.GDPR/ServiceCollectionExtensions.cs (services.AddScoped<IPersonalDataLocator, ...>).

TASK:
Add the composite registration helper, the DSR-side registration, the builder and the eight provider registrations listed in the Phase 3 tasks, each in its own pull request of the split table.

KEY RULES:
- Registration completeness: every option type and dependency the locator resolves is registered, proven by a DI
  test with ValidateOnBuild and ValidateScopes.
- Leaf locators are keyed; exactly one unkeyed IPersonalDataLocator (the composite) exists, so several locators coexist without recursion.
- One IRelationalRowSource per store key; a duplicate key is a start-up error.
- NEVER mix provider code into the DSR package.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs
- src/Encina.Marten.GDPR/ServiceCollectionExtensions.cs
- src/Encina.ADO.PostgreSQL/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 4: Provider Implementations — 10 Database Providers

<details>
<summary><strong>Tasks</strong></summary>

#### 4a. ADO.NET x3 (`Encina.ADO.{SqlServer,PostgreSQL,MySQL}/PersonalData/PersonalDataRowSourceADO.cs`)

1. Builds one parameterized `SELECT <idColumn>, <personal-data columns> FROM <table> WHERE <subjectColumn> = @subject [AND <tenantColumn> = @tenant]` with provider quoting and the row cap (`TOP (@n)` on SQL Server, `LIMIT @n` on PostgreSQL and MySQL; `n = maxRows + 1`)
2. Opens the connection with `OpenAsync(ct)` and reads with `ExecuteReaderAsync(ct)`; never the synchronous overloads
3. Identifier names come only from the validated catalog (never from request data) and are quoted with the provider's rules (brackets, double quotes, backticks)

#### 4b. Dapper x3 (`Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/PersonalData/PersonalDataRowSourceDapper.cs`)

1. Same SQL as 4a through `QueryAsync` with a `CommandDefinition` carrying the cancellation token; rows read as dynamic dictionaries and mapped to `RelationalRow`

#### 4c. EF Core (`Encina.EntityFrameworkCore/PersonalData/PersonalDataRowSourceEF.cs`, `EfPersonalDataCatalogAdapter.cs`)

1. Reads `DbContext.Model` for table, key, subject property and tenant filter; the EF catalog adapter produces `RelationalPersonalDataEntity` from the model so no registration is needed beyond the attributes
2. Per entity type, one cached generic delegate (created once at start-up, not per call) runs `context.Set<T>().AsNoTracking().Where(subjectKey == value).Select(projection).Take(max + 1)`. Query filters (Design Choice 10): the tenant filter is replaced by the explicit tenant predicate and every other named query filter (soft delete, archive) is ignored with the EF 10 named-filter overload `IgnoreQueryFilters(names)`, so hidden rows are located; the integration test loads a soft-deleted row. ADO.NET, Dapper and MongoDB select regardless of any flag column, which the documentation states
3. The row source never calls `SaveChangesAsync` and is read-only (`AsNoTracking`), so it flushes nothing of the caller's `DbContext`
4. Works for SqlServer, PostgreSQL and MySQL through the context's provider; integration tests run on all three

#### 4d. MongoDB (`Encina.MongoDB/PersonalData/PersonalDataRowSourceMongoDB.cs`)

1. `Find` with a typed filter on the subject field (and tenant), projection of the personal-data fields, `Limit(max + 1)`, `ToListAsync(ct)`; collection name from the map; the `_id` is the `EntityId`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- Provider packages already reference Encina.Compliance.DataSubjectRights. Existing per-provider feature folders to
  copy the layout from: ProcessingActivity/ (ADO.NET, Dapper, EF Core, MongoDB). SQL differences: SQL Server TOP (@n),
  PostgreSQL and MySQL LIMIT @n; quoting [x], "x", `x`.
- The Dapper scripts and SQL builders are intentionally kept per provider; do not share internals across packages.

TASK:
Implement IRelationalRowSource for ADO.NET (3), Dapper (3), EF Core and MongoDB as described in Phase 4.

KEY RULES:
- Parameters only for values; identifiers only from the validated catalog, quoted per provider.
- Database calls are asynchronous with a CancellationToken (OpenAsync, ExecuteReaderAsync); NEVER Open().
- Catch provider exceptions and return Left(dsr.locator_entity_failed) with the exception type only; let
  OperationCanceledException propagate.
- Read maxRows + 1 rows so the engine can detect the cap; project only the needed columns; no entity materialization.
- No tenant predicate at all when tenantId is null (tenancy off); soft-deleted and filtered rows are read (Design Choice 10).
- Tenant predicate explicit on Dapper, ADO.NET and MongoDB; on EF replace the global tenant filter with the explicit
  predicate rather than disabling filters broadly.

REFERENCE FILES:
- src/Encina.ADO.PostgreSQL/ProcessingActivity/ProcessingActivityRegistryADO.cs
- src/Encina.ADO.PostgreSQL/Tenancy/ITenantEntityMapping.cs
- src/Encina.EntityFrameworkCore/ProcessingActivity/
- src/Encina.MongoDB/ProcessingActivity/
```

</details>

---

### Phase 5: Cross-Cutting Integration

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-tenancy** — Design Choice 6; two-tenant tests on every provider
2. **Validation** — the start-up validator (Phase 2) is the validation integration
3. **Consumers** — the contract with P-02 (#1188), P-07 (#1195) and P-03 (#1189) is documented in the XML docs of `IPersonalDataLocator` and `RelationalPersonalDataLocator`: `EntityId` is the row's primary key as a string, `FieldName` the property name, `CurrentValue` the stored value; no consumer code changes in this plan
4. **Erasure strategy note** — the relational `IDataErasureStrategy` that updates a located row is part of P-02 (#1188); this plan only guarantees that every located field is addressable (type, key, property). If #1188's plan chooses another addressing scheme, the `PersonalDataLocation` mapping in Phase 2 is the one place to change
5. **Evaluated and not integrated** — caching, health checks, resilience, locks, transactions, idempotency, module isolation, audit trail (see the matrix)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- DataSubjectRightsOptions.RequireTenantContext, ITenantEntity (Encina.Tenancy) and ITenantEntityMapping (ADO.NET and
  Dapper Tenancy folders) already describe tenant scoping.

TASK:
Wire tenant scoping end to end and document the consumer contract of the located rows.

KEY RULES:
- Tenant is never taken from request data; it comes from IRequestContextAccessor.
- With tenancy off, no tenant predicate is added and no tenant configuration is needed (no literal default tenant).

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs
- src/Encina.Tenancy/Abstractions/ITenantEntity.cs
- docs/plans/blocked-data-state-implementation-plan-1189.md (RelationalBlockedDataLocator)
```

</details>

---

### Phase 6: Observability

<details>
<summary><strong>Tasks</strong></summary>

The names in the issue (`encina.dsr.locator_rows`, `encina.dsr.locate`, `encina.provider`) were proposals; the plan follows the package's existing naming (`DSR.*` activities, `dsr.*` counters and tags, `dsr.tenant_id`).

Phase 6 is PR 3 of the split table (engine logging arrives with PR 4, which depends on it).

1. **`Diagnostics/DataSubjectRightsDiagnostics.cs`** (modify) — activity `DSR.Locate` (tags `dsr.tenant_id` only when multi-tenancy is on and absent when it is off (ADR-054 D4), `dsr.provider`, `dsr.outcome`), counter `dsr.locator.rows_located.total` (tags `dsr.provider`, `dsr.entity_type`), counter `dsr.locator.failures.total` (tag `dsr.failure_reason`), histogram `dsr.locator.duration.ms`; no subject id, no values
2. **`EventIdRanges.cs`** (modify) — register a new range `ComplianceDSRLocator = (6550, 6564)` (the ComplianceDSR range 8300-8349 is fully used) and map `Encina.Compliance.DataSubjectRights` to both ranges in `EncinaEventIdAllocationTests`
3. **`Diagnostics/RelationalLocatorLogMessages.cs`** (new) — `[LoggerMessage]`: `LocateStarted` (6550, Debug), `LocateCompleted` (6551, Information, counts only), `LocateFailed` (6552, Error), `EntityReadFailed` (6553, Error, entity type and exception through `ForLogging()`), `RowLimitExceeded` (6554, Warning), `SubjectIdInvalid` (6555, Warning), `TenantRequired` (6556, Warning), `CatalogInvalid` (6557, Critical, start-up), `MappingConflict` (6558, Warning)
4. **Health check** — none: the locator has no checkable dependency beyond the databases the application already checks
5. Provider row sources do not log; the engine logs once per entity, so no provider package needs a new range

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- ActivitySource and Meter "Encina.Compliance.DataSubjectRights"; tags are prefixed "dsr."; EventIds 8300-8349
  (ComplianceDSR) are all used. The new range 6550-6564 comes from the plan batch's assigned block 6550-6599.

TASK:
Add the Locate activity, counters, histogram, the range registration and the [LoggerMessage] entries.

KEY RULES:
- Register the range in src/Encina/Diagnostics/EventIdRanges.cs first; pack EventIds sequentially from 6550; XML docs
  name the range ("Event IDs: 6550-6558 (see EventIdRanges.ComplianceDSRLocator)").
- Add the range to the Encina.Compliance.DataSubjectRights entry of EncinaEventIdAllocationTests and run it.
- No subject ids, row values, entity ids or Error.Message in tags, metrics or logs; exceptions via ForLogging().
- Tenant as "dsr.tenant_id", emitted only when multi-tenancy is on (ADR-054 D4), absent when off.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Diagnostics/DataSubjectRightsDiagnostics.cs
- src/Encina.Compliance.DataSubjectRights/Diagnostics/DSRLogMessages.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 7: Testing

<details>
<summary><strong>Tasks</strong></summary>

Manifest first: each pull request adds its files' rows to `.github/coverage-manifest/Encina.Compliance.DataSubjectRights.json` and to the manifest of the provider package it touches, in the same PR. Numeric per-file targets (percent of coverable lines per flag; every flag has its own coverable lines) with one-sentence justifications:

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `Attributes/DataSubjectKeyAttribute.cs` | 100 | 0 | 0 | 0 | 0 | Constructor and properties run in the catalog tests; guard 0 because the attribute has no validated parameters |
| `Locators/Relational/RelationalPersonalDataEntity.cs`, `RelationalRow.cs` | 100 | 0 | 0 | 0 | 0 | Records with no logic beyond construction, exercised by catalog and engine tests; no guarded parameters |
| `Locators/Relational/RelationalPersonalDataCatalog.cs` | 95 | 90 | 0 | 0 | 0 | Every validation rule has a unit test; guard tests call each public method with null and empty arguments |
| `Locators/Relational/IRelationalRowSource.cs` | 0 | 0 | 0 | 0 | 0 | Interface with no executable lines |
| `Locators/Relational/RelationalPersonalDataLocator.cs` | 95 | 90 | 80 | 80 | 60 | Engine paths (mapping, tenant, cap, failure, cancellation) are unit-tested with an in-memory source; contract and property run it over real stores; integration reaches the happy and failing paths |
| `Locators/Relational/RelationalPersonalDataLocatorOptions.cs` | 100 | 100 | 0 | 0 | 0 | One validated property |
| `Locators/Relational/RelationalPersonalDataStartupValidator.cs` | 95 | 90 | 0 | 0 | 70 | Start-up failure paths are unit-tested; integration builds a real host |
| `Diagnostics/RelationalLocatorLogMessages.cs` | 0 | 0 | 0 | 0 | 0 | Source-generated `[LoggerMessage]` delegates without executable lines in Cobertura (as `DSRLogMessages.cs` today) |
| `DSRErrors.cs` (modify) | 100 | 0 | 0 | 0 | 0 | Baseline unit 100; each new factory is asserted for code, category (`GetCategory`) and details |
| `Diagnostics/DataSubjectRightsDiagnostics.cs` (modify) | 100 | 0 | 73 | 61 | 65 | Baseline unit 100, contract 73, property 61, integration 65; kept (targets never go down) and the new members are covered by the listener-based unit test |
| `ServiceCollectionExtensions.cs` (DSR, modify) | 100 | 100 | 0 | 0 | 0 | Baseline unit 100, guard 100; the DI tests of PR 2 and PR 4 cover the new registrations |
| `Encina.Marten.GDPR/ServiceCollectionExtensions.cs` (modify) | 90 | 0 | 0 | 0 | 0 | No per-file target today (package default); the PR 2 DI test covers the changed registration |
| `PersonalDataRowSourceADO.cs` (3 packages) | 30 | 0 | 80 | 0 | 85 | Needs a real server: unit covers SQL building only; the contract suite and the integration tests reach the rest |
| `PersonalDataRowSourceDapper.cs` (3 packages) | 30 | 0 | 80 | 0 | 85 | Same reasoning as ADO.NET |
| `PersonalDataRowSourceEF.cs`, `EfPersonalDataCatalogAdapter.cs` | 30 | 0 | 80 | 0 | 85 | The model-driven query needs a `DbContext` on a real database (SQL Server, PostgreSQL, MySQL) |
| `PersonalDataRowSourceMongoDB.cs` | 30 | 0 | 80 | 0 | 85 | Needs a real MongoDB; unit covers filter building only |
| Provider `ServiceCollectionExtensions.cs` (8 packages, modify) | baseline | baseline | baseline | baseline | baseline | Baselines read from each manifest (ADO.NET SQL Server unit 55, guard 55, integration 40; the others in their manifests) are kept; the provider DI test covers the new registration |

The PR reports the measured per-flag coverage against this table; property tests cover the engine's "every row with the subject key is located and no other row is" invariant and the determinism of mapping, so the property flag lives on the engine row, not on the provider sources.

1. **UnitTests** — engine (mapping, tenant required, invalid subject id, row cap, one failing entity fails all, cancellation, routing by store key), catalog (missing subject key, duplicate key, unsupported key type, composite primary key, identifier allow-list, undeclared entity under required tenancy, builder override), start-up validator (including duplicate store key), DI tests; a `GetCategory` test for every new error code (ADR-062); telemetry test with an in-memory exporter (activity and counters carry `dsr.tenant_id` when tenancy is on and carry no tenant tag when it is off; nothing carries a subject id or a value)
2. **GuardTests** — all public constructors and methods
3. **ContractTests** — one row-source contract run against ADO.NET, Dapper, EF Core and MongoDB real instances: same rows, same `Left` codes, same cap behavior
4. **PropertyTests** — FsCheck: for any generated set of rows, every row with the subject key is located and no other row is
5. **IntegrationTests** — `[Collection]` shared fixtures per family (`ADO-SqlServer`, `ADO-PostgreSQL`, `ADO-MySQL`, `Dapper-*`, `EFCore-*`, MongoDB): subject with rows in two tables located; subject with none returns an empty list; two-tenant test; cap exceeded; a Guid and an int subject key; a soft-deleted row is located; an entity with a composite key or no tenant declaration fails start-up; S23-style test combining a relational table and the Marten locator through the composite of Design Choice 8; a two-store test (EF Core plus MongoDB) in PR 9; each provider PR (5 to 9) carries the integration tests of its own providers
6. **Load/Benchmark** — `.md` justifications (`tests/Encina.LoadTests/Compliance/DataSubjectRights/`, `tests/Encina.BenchmarkTests/…`): a DSR request is neither concurrent nor a hot path
7. **PR split** — the eleven pull requests of the split table in the Summary; the contract suite (task 3) is PR 10 and runs against the five provider families once PRs 5 to 9 are merged

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
- Test projects are consolidated under tests/ (UnitTests, GuardTests, ContractTests, PropertyTests, IntegrationTests).
  Integration tests use shared [Collection("<Family>-<Database>")] fixtures, never IClassFixture; ClearAllDataAsync in
  InitializeAsync. Assertions use Shouldly via Encina.Testing.Shouldly.
- Existing neighbours: tests/Encina.IntegrationTests/Compliance/DataSubjectRights/, per-provider ProcessingActivity tests.

TASK:
Implement the tests listed in Phase 7 and the manifest targets, then measure per-flag coverage.

KEY RULES:
- Tests execute real package code; contract and property tests instantiate real implementations; no reflection-only tests.
- Provider-dependent feature: tests cover all 10 database providers.
- No Thread.Sleep; FakeTimeProvider where time matters; builders for test data; deterministic.
- Measure per flag with dotnet test ... --collect "XPlat Code Coverage" and the coverage-report script.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Compliance.DataSubjectRights.json
- tests/Encina.IntegrationTests/Compliance/DataSubjectRights/
- tests/Encina.IntegrationTests/Dapper/Collections.cs
```

</details>

---

### Phase 8: Documentation & Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML doc comments on all new public APIs (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`)
2. `changelog.d/1248-relational-personal-data-locator.added.md`
3. README of `Encina.Compliance.DataSubjectRights` and of the seven provider packages (one short section each)
4. `docs/features/data-subject-rights.md` (or the existing DSR feature page): declaring `[DataSubjectKey]`, registering a provider, the tenant rule, the row cap, combining with the Marten locator
5. `docs/INVENTORY.md`; `PublicAPI.Unshipped.txt` in every touched package
6. ADR: not needed (the choices follow AGENTS.md and SPEC-002). ADR number assigned when the ADR pull request opens, if the maintainer asks for one
7. Build verification: `dotnet build --configuration Release` with 0 warnings; `dotnet test` green; every coverage flag reaches its manifest target; no hand-typed coverage figures (cite `cov:` markers)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
CONTEXT:
- Documentation rules: .claude/skills/encina-docs/SKILL.md. Changelog fragments: changelog.d/README.md.

TASK:
Document the relational locator: metadata declaration, provider registration, tenant behavior, completeness rule and
the combined export of S23.

KEY RULES:
- English only; no private client application named (write "the reference application").
- Never type coverage figures; cite cov: markers. Never edit CHANGELOG.md.

REFERENCE FILES:
- docs/features/ (data subject rights page)
- src/Encina.Compliance.DataSubjectRights/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Relevance |
|--------|-----------|-----------|
| GDPR | Art. 15, Art. 20 | Access and portability: the export needs every field of the subject |
| GDPR | Art. 17, Art. 17(3) | Erasure and its exceptions: located fields carry the legal-retention flag |
| GDPR | Art. 30 | Records of processing: the catalog documents where personal data lives |
| LOPDGDD | Art. 32 | Blocking: the blocked-data feature adapts this locator |
| SPEC-002 | REQ-004, REQ-005, REQ-054, AC-039, S14, S23, DEC-011 (d), REQ-016, REQ-061, REQ-062 | The requirement this plan implements |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IPersonalDataLocator`, `PersonalDataLocation` | `src/Encina.Compliance.DataSubjectRights/` | Contract and result type |
| `CompositePersonalDataLocator` | `src/Encina.Compliance.DataSubjectRights/Locators/` | Combines the relational and Marten locators; fails on any locator failure |
| `PersonalDataAttribute`, `PersonalDataMap` | `src/Encina.Compliance.DataSubjectRights/` | Field metadata and scanning |
| `MartenEventPersonalDataLocator` | `src/Encina.Marten.GDPR/Locator/` | Style reference and S23 partner |
| `DataSubjectRightsOptions` (`RequireTenantContext`, `AssembliesToScan`) | `src/Encina.Compliance.DataSubjectRights/` | Tenant rule and discovery |
| `ITenantEntity`, `ITenantEntityMapping` | `Encina.Tenancy`, ADO.NET and Dapper `Tenancy/` | Tenant column |
| `DataSubjectRightsDiagnostics`, `DSRErrors` | `src/Encina.Compliance.DataSubjectRights/` | Telemetry and error codes |
| `AddEncinaProcessingActivity{Provider}` registrations | provider `ServiceCollectionExtensions.cs` | Registration naming and shape |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.DataSubjectRights` (locator) | 6550-6564 (`ComplianceDSRLocator`, new, from the batch block 6550-6599) | Uses 6550-6558; `ComplianceDSR` 8300-8349 has no free slots; to be registered in `EventIdRanges.cs` by the implementing pull request |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core (DSR package) | ~12 | attribute, entity, row, seam, catalog, locator, options, validator, DI, diagnostics, log messages, errors |
| ADO.NET x3 + Dapper x3 | ~12 | one row source and one registration per package |
| EF Core + MongoDB | ~6 | row sources, catalog adapter, registrations |
| Tests | ~22 | across 5 test types and provider families |
| Documentation, manifests, PublicAPI, changelog | ~10 | |
| **Total** | **~75** | eleven pull requests (see the split table in the Summary; the per-category counts above are approximate) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #1248 (SPEC-002 P-45): the relational IPersonalDataLocator on the 10 database providers.

PROJECT CONTEXT:
- .NET 10 / C# 14, ROP (Either<EncinaError, T>), pre-1.0 (breaking changes preferred, no [Obsolete]).
- Providers: ADO.NET x3, Dapper x3, EF Core x3 (one package), MongoDB. Provider packages already reference
  Encina.Compliance.DataSubjectRights.
- Consumers: P-02 (#1188 erasure), P-03 (#1189 blocking), P-07 (#1195 export).

IMPLEMENTATION OVERVIEW:
Phase 1: [DataSubjectKey], RelationalPersonalDataEntity, RelationalRow, new DSRErrors
Phase 2: IRelationalRowSource seam, catalog with validation, RelationalPersonalDataLocator, options, start-up validator
Phase 3: DI: AddEncinaRelationalPersonalData + AddEncinaPersonalDataLocator{Provider} (keyed leaf locators plus one composite, Design Choice 8)
Phase 4: row sources: ADO.NET x3, Dapper x3, EF Core (model-driven), MongoDB
Phase 5: tenant scope, consumer contract
Phase 6: DSR.Locate activity, dsr.locator.* metrics, EventIds 6550-6558 (new range 6550-6564)
Phase 7: unit, guard, contract, property, integration on all providers, two tenants; manifests
Phase 8: docs, README, changelog, PublicAPI

KEY PATTERNS:
- Any failing entity fails the whole locate; row cap exceeded is Left; never a partial inventory.
- Typed subject key parsing; tenant filter explicit; fail closed.
- No subject ids, row values or Error.Message in logs, tags or metrics; exceptions via ForLogging().
- Async database calls with CancellationToken; parameters for values, catalog-validated quoted identifiers.
- CRAP <= 10 on every new method; tests execute real code; shared [Collection] fixtures.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/ (Abstractions/IPersonalDataLocator.cs, Locators/, Model/, PersonalDataMap.cs)
- src/Encina.Marten.GDPR/Locator/MartenEventPersonalDataLocator.cs
- src/Encina.ADO.PostgreSQL/ProcessingActivity/ and ServiceCollectionExtensions.cs
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (REQ-054, AC-039, S14, S23)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A located inventory must reflect the current rows; caching it would risk an erasure or export on stale data |
| 2 | OpenTelemetry | ✅ | `DSR.Locate` activity, `dsr.locator.*` counters and histogram, `dsr.tenant_id` tag only when tenancy is on (ADR-054 D4), no subject ids or values (Phase 6) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` 6550-6558 in the new range `ComplianceDSRLocator` (ADR-021) |
| 4 | Health Checks | ❌ | No new checkable dependency; the databases are checked by the application's existing health checks |
| 5 | Validation | ✅ | Start-up validator: entities with personal data but no subject key, unsupported key types and mapping conflicts stop the host |
| 6 | Resilience | ❌ | Local database reads inside a DSR request; a failure is returned to the caller (fail closed) and retried at request level, not hidden by a retry here |
| 7 | Distributed Locks | ❌ | Read-only operation; no shared state updated by concurrent hosts |
| 8 | Transactions | ❌ | Read-only; the erasure that follows (P-02) owns the transaction |
| 9 | Idempotency | ❌ | A pure read; repeated calls return the same inventory |
| 10 | Multi-Tenancy | ✅ | REQ-061/AC-043: explicit tenant filter, `dsr.tenant_required` when required, an undeclared entity is a start-up error under required tenancy, no predicate when tenancy is off; two-tenant test on every provider |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ❌ | The locator only reads; the DSR request aggregate already records the request, and the read audit of the disclosed data belongs to P-03/P-07 (#1189, #1195) |

---

## Prerequisites & Dependencies

| Item | Why | State |
|------|-----|-------|
| Subject-id conversion (`SubjectIdConversion`, and `IDataSubjectIdConverter` from #1193) | Parsing the string subject id to the key's type | `SubjectIdConversion` exists; the shared converter arrives with #1193 (use `SubjectIdConversion` until then) |
| Relational erasure strategy (`IDataErasureStrategy`) | Located fields must be erasable | Owned by P-02 (#1188); this plan only guarantees addressability |
| EventId range 6550-6564 | New logging | To be registered in `EventIdRanges.cs` by PR 3 |
| `Encina.Marten.GDPR` locator | S23 combines both | Exists |

---

## Next Steps

1. Link this plan from issue #1248; ask the maintainer the Dilemmas of Design Choices 3, 8, 9 and 10 when work on the issue starts
2. The eleven pull requests of the split table, in order (PRs 1 to 4 core, 5 to 9 providers, 10 contract and property, 11 documentation)
3. Confirm with the plan of #1188 that the `EntityId` = primary key string addressing suits its erasure strategy
4. The final commit references `Fixes #1248`
