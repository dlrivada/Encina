# Implementation Plan: `ConfigureShadowPrimaryKey<TEntity, TId>` for EF Core (shadow int primary key + Guid alternate key)

> **Issue**: [#1039](https://github.com/dlrivada/Encina/issues/1039)
> **Type**: Feature
> **Complexity**: Medium (4 phases, EF Core family only, 17 files in two pull requests, see Estimated File Count)
> **Estimated Scope**: ~90 lines of production code + ~600 lines of tests + ~450 lines of documentation (about 1,150 changed lines in total)

---

## Summary

An aggregate whose identity is a `GuidStronglyTypedId<TSelf>` is stored with the Guid as clustered primary key by default. For high-volume tables that causes page splits and fragmented clustered indexes. The pattern of the issue keeps a database-generated `int` as the clustered primary key (an EF Core shadow property the domain never sees) and maps the strongly-typed Guid `Id` as an alternate key, which is what cross-aggregate foreign keys target through `HasPrincipalKey(c => c.Id)`. Today it takes a shadow property, `HasKey`, `HasAlternateKey` and a value conversion, kept consistent by hand in every configuration. This plan adds `EntityConfigurationExtensions.ConfigureShadowPrimaryKey<TEntity, TId>(this EntityTypeBuilder<TEntity> builder, string shadowKeyName = "PhysicalId")` to `Encina.EntityFrameworkCore`, composing with the existing `ConfigureAggregateRoot<T, TId>()`.

- **Standards**: none. The pattern is the one of the issue's reference ("EF Core Is DDD", Zoran Horvat); EF Core alternate keys and shadow properties are the mechanism.
- **Affected packages**: `Encina.EntityFrameworkCore` (`Configuration/EntityConfigurationExtensions.cs`, its README, the coverage manifest). `Encina.DomainModeling` needs no change (`IEntity<TId>` and `GuidStronglyTypedId<TSelf>` are enough).
- **Provider category**: Database, EF Core family only. The three relational EF Core providers of the matrix (SqlServer, PostgreSQL, MySQL) apply; ADO.NET, Dapper and MongoDB are not applicable (different mapping model); SQLite is out of the matrix (AGENTS.md section 5, ADR-024). The Provider Implementation Matrix below states each.
- **Scope**: 1 new public extension method, plus unit, guard and integration tests, two test-type justification files, README, feature page, ADR, changelog fragment and manifest entry: 17 files, over one worker PR, so it ships as 2 pull requests of at most 12 files (see Estimated File Count).
- **Public API tracking**: `src/Encina.EntityFrameworkCore/PublicAPI.Unshipped.txt` holds only `#nullable enable` and the package references no public API analyzer, so the issue's task "update `PublicAPI.Unshipped.txt`" needs no line today (checked on `origin/main` `d523eda92`).

### Provider Implementation Matrix

| Provider | Helper applies | Integration test | Notes |
|----------|:-:|:-:|-------|
| EFCore-SqlServer | ✅ | ✅ `[Collection("EFCore-SqlServer")]` | `IDENTITY` primary key plus `UNIQUE` constraint on the Guid column |
| EFCore-PostgreSQL | ✅ | ✅ `[Collection("EFCore-PostgreSQL")]` | Identity column plus `UNIQUE` constraint |
| EFCore-MySQL | ✅ | ✅ written; skipped by `EFCoreMySqlFixture.IsAvailable` until Pomelo supports EF Core 10 (#2086) | `AUTO_INCREMENT` plus `UNIQUE` key |
| EFCore-SQLite | n/a | none | Out of the provider matrix (ADR-024, AGENTS.md section 5); the model-level unit tests run without any provider |
| ADO.NET, Dapper, MongoDB | ❌ N/A | none | Not EF Core mappings; the feature page says why |

---

## Design Choices

<details>
<summary><strong>1. Type of the shadow key</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `int`, as the issue specifies** | The 4-byte clustered key the motivation is about; matches brownfield tables that already have an `int` identity; one signature | An `int` identity overflows at about 2.1 billion rows, and the issue's own use case is high-volume tables |
| **B) A third type parameter `TKey` constrained to `int` or `long`-like numbers** | Covers tables that outgrow `int`; brownfield tables with a `bigint` key | A second generic argument at every call (`ConfigureShadowPrimaryKey<Invoice, InvoiceId, long>()`), and C# cannot constrain to exactly `int` and `long` |
| **C) `long` only** | Never overflows | Doubles the key width the pattern is meant to keep small; does not match brownfield `int` keys |

### Chosen Option: **A — `int` (recommended, pending the maintainer)**

### Rationale

- Option A is the issue's contract (acceptance: "PK on shadow `int`") and the simplest signature; a `long` variant can be added as a second method without changing this one. Option B wins for a deployment that expects more than two billion rows in one aggregate table, which is the scenario where the maintainer should answer before the first release.
- The default name `PhysicalId` and the `shadowKeyName` parameter let a brownfield table keep its existing column name.

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Composition with existing keys and with `ConfigureAggregateRoot`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The helper only sets keys, the shadow property and the Id mapping; it works whether or not `HasKey(e => e.Id)` was called before, and it never calls `ConfigureAggregateRoot`** | Verified on EF Core 10.0.12 (InMemory model, 2026-11-17): with `HasKey(e => e.Id)` first or not, the model ends with primary key `PhysicalId` (shadow) and keys `Id` and `PhysicalId`; the issue's usage already composes the two helpers in either order | The caller still writes two lines |
| **B) The helper also calls `ConfigureAggregateRoot<TEntity, TId>()` when the entity is an `AggregateRoot<TId>`** | One line | Entities that are not aggregate roots or that use another concurrency setup get behavior they did not ask for; the issue says the two helpers "compose without overlap" |

### Chosen Option: **A — keys only, order-independent**

### Rationale

- A unit test builds the model with `HasKey(e => e.Id)` before and after the helper and asserts the same result, so the order independence is executable, not assumed.
- `ConfigureAggregateRoot` keeps the concurrency token, audit columns and soft delete, unchanged.

- **Class:** Trivial
- **Settled by:** the issue ("composes with the existing `ConfigureAggregateRoot<T,TId>()` without overlap") and the verification run recorded in the Rationale

</details>

<details>
<summary><strong>3. Mapping of the Guid `Id`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A value conversion `id => id.Value` / `value => GuidStronglyTypedId<TId>.From(value)`, and `ValueGeneratedNever()` on `Id`** | The domain creates the Guid (`GuidStronglyTypedId<TSelf>.New()`, `StronglyTypedId.cs:223`), so the database must never generate it; the `From` factory is the one the issue names | `From` creates the id with `Activator.CreateInstance(typeof(TSelf), value)` (`StronglyTypedId.cs:233`), so the id type needs a public constructor taking a `Guid`; a type without one fails when EF materializes an entity |
| **B) A `Func<Guid, TId>` factory parameter** | Works for ids with a private constructor | A second argument on every call; the one-liner the issue asks for is lost |
| **C) Leave the conversion to the application** | No code | Defeats the point of the helper |

### Chosen Option: **A — conversion through `From`, `ValueGeneratedNever`**

### Rationale

- The XML docs state the public-constructor requirement; an integration test materializes an entity from each provider, so a missing constructor fails the test, not production.
- The relational schema keeps the Guid as `uniqueidentifier` (SQL Server), `uuid` (PostgreSQL) and `char(36)` (MySQL), the providers' defaults for `Guid`.

- **Class:** Trivial
- **Settled by:** the issue's Implementation Tasks ("construct strongly-typed IDs from Guid via the `From(Guid)` factory on `GuidStronglyTypedId<TSelf>`") and `src/Encina.DomainModeling/StronglyTypedId.cs:223-236`

</details>

<details>
<summary><strong>4. Argument validation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Exceptions at configuration time: `ArgumentNullException.ThrowIfNull(builder)`, `ArgumentException.ThrowIfNullOrWhiteSpace(shadowKeyName)`, and an `ArgumentException` when `shadowKeyName` equals `Id`** | The model builder runs once at startup, outside any request, and the existing public helpers of the class start with `ArgumentNullException.ThrowIfNull(builder)` (12 call sites, for example `EntityConfigurationExtensions.cs:357`, `:407`); a name equal to `Id` would collide with the Guid property and produce an unreadable EF error later | Exceptions, not `Either` |
| **B) Return `Either<EncinaError, ...>`** | ROP everywhere | An `EntityTypeBuilder` fluent helper cannot return an `Either` and keep chaining; AGENTS.md section 3 limits ROP to business logic |

### Chosen Option: **A — exceptions at configuration time**

### Rationale

- The guard tests assert each case: null builder, null, empty and whitespace name, and the name `Id`. The issue's "non-Guid `TId`" and "conflicting `HasKey`" cases need no test: the first is a compile error (`TId : GuidStronglyTypedId<TId>`), the second is choice 2 (order-independent).

- **Class:** Trivial
- **Settled by:** AGENTS.md section 3 (ROP "for business logic") and the existing methods of `EntityConfigurationExtensions` (`src/Encina.EntityFrameworkCore/Configuration/EntityConfigurationExtensions.cs:357`)

</details>

<details>
<summary><strong>5. Which providers are tested</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) SqlServer, PostgreSQL and MySQL integration tests (the MySQL one skipped by its fixture until #2086), no SQLite** | The three EF Core providers of the matrix; real databases, as required for database features | The MySQL test does not run today |
| **B) The four providers of the issue, including SQLite** | Matches the issue text | SQLite is out of the matrix and not tested or guaranteed (ADR-024) |
| **C) SqlServer and PostgreSQL only** | Both run in CI today | Breaks the "all providers of the category" rule and leaves MySQL without a test the day Pomelo ships |

### Chosen Option: **A — three providers**

### Rationale

- The MySQL file uses `Assert.SkipUnless(_fixture.IsAvailable, ...)` like every other EF Core MySQL test, so it starts running when #2086 is fixed, with no change here.
- Integration tests for a database feature are never replaced by a `.md` (AGENTS.md section 9).

- **Class:** Settled
- **Settled by:** AGENTS.md section 5 ("SQLite (ADR-024 ...) out of the matrix") and section 9 ("Integration: required, real databases; NEVER a `.md` justification"); the acceptance line "all 4 EF Core providers" of the issue predates ADR-024

</details>

<details>
<summary><strong>6. How the schema is verified</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Create the tables from the model with `IRelationalDatabaseCreator.CreateTablesAsync` (the pattern of the IdGeneration tests) and read the keys from `INFORMATION_SCHEMA` (`table_constraints`, `key_column_usage`, `referential_constraints`)** | Real DDL per provider, one query shape for three databases; the existing tests use the same creator | Reads catalog views, not a migration file |
| **B) Scaffold and apply EF migrations in the test** | Closest to the issue's wording "migration tests" | Needs the design-time package and a migrations folder per provider for one table |
| **C) Compare `GenerateCreateScript()` text** | No database | Not a real database; string comparison is brittle |

### Chosen Option: **A — create tables and read the catalog**

### Rationale

- Assertions: the primary key constraint covers the shadow column only; a unique constraint covers the Guid column; a second aggregate's foreign key references that unique constraint; the shadow value is database-generated and increasing; a duplicate Guid fails with `DbUpdateException`; an entity is read back by `Id` with the strongly-typed Guid intact.
- Tests use the shared `[Collection("EFCore-<Database>")]` fixtures, never a per-class fixture.

- **Class:** Trivial
- **Settled by:** `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/SqlServer/IdGeneration/IdGenerationConverterSqlServerTests.cs` (creator pattern) and AGENTS.md section 9 (shared collection fixtures)

</details>

<details>
<summary><strong>7. Encina's own repository and bulk operations with a shadow primary key</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Out of scope: the feature page documents that `FunctionalRepositoryEF<TEntity, TId>` looks entities up with `FindAsync([id])` (`GetByIdAsync` at `FunctionalRepositoryEF.cs:79`, `DeleteAsync` at `:440`), which targets the primary key and so does not accept the Guid of an entity configured with this helper; the audit interceptor (`AuditInterceptor.GetEntityId` at `Auditing/AuditInterceptor.cs:328` builds the audited entity id from `FindPrimaryKey()`, so the audit record would hold the internal `int` `PhysicalId` instead of the Guid `Id`) and `DbContextKeyExtensions.cs` also read `FindPrimaryKey()`; a follow-up issue makes the repository, the three EF bulk operations (`BulkOperationsEFSqlServer.cs:55` and its PostgreSQL and MySQL siblings), `AuditInterceptor.cs` and `DbContextKeyExtensions.cs` shadow-key aware (the grep of `FindPrimaryKey` over `src/Encina.EntityFrameworkCore` finds exactly these five files)** | The helper stays one small PR; the repository is optional (AGENTS.md section 3) and callers can use the `DbContext` (`FirstOrDefaultAsync(e => e.Id == id)`) | Until the follow-up, an application cannot combine this helper with `AddEncinaRepository` for the same entity |
| **B) Include the repository and bulk-operation changes in this plan** | The pattern works end to end on the day it ships | Touches five more files (repository, three bulk operations, audit interceptor, key extensions) with their tests on three providers, which together with the helper exceeds the 15-file limit of one PR, and is not what the issue asks |

### Chosen Option: **A — document now, follow-up issue (recommended, pending the maintainer)**

### Rationale

- Option B wins when the reference application needs the repository pattern on the same aggregates as soon as the helper ships; option A wins for the issue as written ("No changes required to existing entities (opt-in)") and keeps the PRs reviewable.
- The follow-up issue is written with this plan (`artifacts/issues/ef-repository-shadow-primary-key.md`, a `[DEBT]` item) and is listed in Next Steps.

- **Class:** Dilemma

</details>

<details>
<summary><strong>8. Load and benchmark tests</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Justification files for both: `tests/Encina.BenchmarkTests/EntityFrameworkCore/ShadowPrimaryKey.md` and `tests/Encina.LoadTests/ShadowPrimaryKey.md`** | The helper runs once at model build; the insert-throughput claim measures SQL Server index behavior, not Encina code; the docs then state the rationale without numbers | The motivating performance claim is not measured by Encina |
| **B) A BenchmarkDotNet insert-throughput benchmark (Guid primary key against shadow `int` plus Guid alternate key) on SQL Server** | Validates the argument of the pattern with data from this repository | A Docker-backed benchmark for a one-time configuration call; its result depends on Guid generation (random against sequential) and fill factor, so the numbers need careful design; documentation may not hand-type them (AGENTS.md section 9) |
| **C) Both a benchmark and a concurrent load test** | Most complete | The helper has no concurrent behavior to load |

### Chosen Option: **A — justification files (recommended, pending the maintainer)**

### Rationale

- Option B wins when the ADR is meant to state a measured gain; option A wins when the ADR states the mechanism (random Guid keys fragment a clustered index) and leaves measurement to the reader's own workload, which is how the issue's motivation is written. AGENTS.md section 9 allows a justification for benchmarks and load tests of thin or non-concurrent code, never for unit, guard or integration tests.
- Each file follows the justification format: title, `## Status: Not Implemented`, numbered reasons, adequate coverage from the other test types, recommended alternative, related files, date and issue.

- **Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: The helper (PR 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.EntityFrameworkCore/Configuration/EntityConfigurationExtensions.cs`: add
   `public static EntityTypeBuilder<TEntity> ConfigureShadowPrimaryKey<TEntity, TId>(this EntityTypeBuilder<TEntity> builder, string shadowKeyName = "PhysicalId") where TEntity : class, IEntity<TId> where TId : GuidStronglyTypedId<TId>`:
   - `ArgumentNullException.ThrowIfNull(builder)`; `ArgumentException.ThrowIfNullOrWhiteSpace(shadowKeyName)`; `ArgumentException` when `string.Equals(shadowKeyName, nameof(IEntity<TId>.Id), StringComparison.Ordinal)`.
   - `builder.Property<int>(shadowKeyName).ValueGeneratedOnAdd();` `builder.HasKey(shadowKeyName);` `builder.HasAlternateKey(e => e.Id);` `builder.Property(e => e.Id).HasConversion(id => id.Value, value => GuidStronglyTypedId<TId>.From(value)).ValueGeneratedNever();` `return builder;`
   - Add a `<item>` for the method to the class remarks list and a full XML doc: summary, `typeparam`, `param`, `returns`, `remarks` (the public-constructor requirement, `DbSet.Find` and `FindAsync` take the `int`, use `FirstOrDefaultAsync(e => e.Id == id)`, the cross-aggregate `HasPrincipalKey(c => c.Id)` example, brownfield tables pass their existing column name as `shadowKeyName`), `example` with the `Invoice` configuration of the issue.
2. No `PublicAPI.Unshipped.txt` line (see Summary).
3. Changelog fragment `changelog.d/1039-ef-core-shadow-primary-key.added.md` and the manifest entry for `Configuration/EntityConfigurationExtensions.cs` (via `mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.EntityFrameworkCore (src/Encina.EntityFrameworkCore/Configuration/EntityConfigurationExtensions.cs), .NET 10 / C# 14, EF Core 10. Issue #1039 (plan docs/plans/ef-shadow-primary-key-implementation-plan-1039.md). Existing helpers ConfigureAggregateRoot<T,TId>, ConfigureConcurrencyToken and the others live in the same static class and start with ArgumentNullException.ThrowIfNull(builder).
TASK: Add ConfigureShadowPrimaryKey<TEntity, TId> as described in Phase 1 of the plan, following the Chosen Option of every choice as the maintainer answered it (Dilemmas are recorded as recommended, pending the maintainer: re-read the Maintainer Decisions section if it exists).
KEY RULES: fluent (returns the builder); no [Obsolete]; XML docs with example and remarks; order-independent with HasKey(e => e.Id) and with ConfigureAggregateRoot; zero warnings; no PublicAPI line (the package tracks none); unit and guard tests (Phase 2) must reach every line.
REFERENCE FILES: src/Encina.EntityFrameworkCore/Configuration/EntityConfigurationExtensions.cs, src/Encina.DomainModeling/StronglyTypedId.cs, src/Encina.DomainModeling/IEntity.cs, src/Encina.DomainModeling/AggregateRoot.cs
```

</details>

### Phase 2: Unit and guard tests (PR 1)

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/EntityFrameworkCore/Configuration/ShadowPrimaryKeyConfigurationTests.cs` and its support file `ShadowPrimaryKeyTestDbContext.cs` with `InvoiceId : GuidStronglyTypedId<InvoiceId>`, `CustomerId`, `Invoice : AggregateRoot<InvoiceId>`, `Customer`; the project already references `Microsoft.EntityFrameworkCore.InMemory`): the primary key is the shadow `int` `PhysicalId` and is `ValueGenerated.OnAdd`; the model has the alternate key `Id`; `Id` is `ValueGenerated.Never` and its provider type is `Guid`; a custom `shadowKeyName` is honored; the result is identical with `HasKey(e => e.Id)` before and after (choice 2); it composes with `ConfigureAggregateRoot<Invoice, InvoiceId>()` in both orders (`RowVersion` is a concurrency token, `DomainEvents` is ignored); a second aggregate's `HasOne<Customer>().WithMany().HasPrincipalKey(c => c.Id).HasForeignKey(i => i.CustomerId)` builds and the foreign key's principal key is the alternate key; calling the helper twice leaves one shadow property.
2. Guard (`tests/Encina.GuardTests/EntityFrameworkCore/Configuration/EntityConfigurationExtensionsShadowKeyGuardTests.cs`): `ConfigureShadowPrimaryKey_NullBuilder_ThrowsArgumentNullException`, `..._NullShadowKeyName_ThrowsArgumentNullException`, `..._EmptyShadowKeyName_ThrowsArgumentException`, `..._WhitespaceShadowKeyName_ThrowsArgumentException`, `..._ShadowKeyNameEqualToId_ThrowsArgumentException`, `..._DefaultShadowKeyName_UsesPhysicalId` (valid result for the optional parameter). The builder for the name cases comes from the `OnModelCreating` of an InMemory context, as in `SoftDeleteTestDbContext`.
3. No property test (configuration only, no input domain) and no contract test (a static class, no interface; the surface is the one method, covered by unit tests).
4. Per-file targets for `Configuration/EntityConfigurationExtensions.cs` (the manifest entry has no per-file targets today). Baseline measured on 2026-11-18 on `origin/main` with `dotnet test tests\Encina.UnitTests --filter "FullyQualifiedName~EntityFrameworkCore" --collect "XPlat Code Coverage"` and the same for `tests\Encina.GuardTests` (252 coverable lines in the file): unit 15/252 = 6%, guard 0/252 = 0%, integration not measured (no test reaches the file). The new method adds about 13 coverable lines, all reached by the new tests, so the targets are the baseline plus the new lines, rounded down. The issue's criterion "coverage >= 85% for new code" is met by the new method itself (100% of its lines under unit; guard and integration each reach the lines their tests drive); a file-level 85% is unreachable because the other 239 lines are unrelated helpers (temporal tables, query filters) that this PR does not touch, and AGENTS.md section 9 replaces a single percentage by per-flag per-file targets. The worker re-measures with the per-flag runs and raises a number only if the measurement is higher.

| Flag | Target | One-sentence justification |
|------|:-:|----------------------------|
| Unit | 11 | Baseline 6% plus the new method's 13 lines, all driven by model-level unit tests; the rest of the 252-line file is not touched by this PR. |
| Guard | 5 | Baseline 0%; the guard tests execute the new method's argument checks and its valid default-name path (about 13 lines), and the other helpers have no parameters to guard. |
| Integration | 0 in PR 1, 5 in PR 2 | PR 1 has no integration test (the target stays at the absent value); PR 2 raises it to 5 because the three provider tests execute the new method (about 13 of 252 lines) against real databases. |
| Contract, Property | 0 | A static configuration class has no interface to keep and no input domain to generate. |

   CRAP of `ConfigureShadowPrimaryKey`: complexity 4 (three validations and the call chain), fully covered by the unit and guard tests, below 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 is done. Unit tests for Encina.EntityFrameworkCore live under tests/Encina.UnitTests/EntityFrameworkCore/ (see SoftDelete/SoftDeleteTestDbContext.cs for a test DbContext with the InMemory provider); guard tests under tests/Encina.GuardTests/EntityFrameworkCore/.
TASK: Write the unit and guard tests of Phase 2 with Shouldly through Encina.Testing.Shouldly; measure the baseline per-flag coverage of Configuration/EntityConfigurationExtensions.cs first and have mechanical-fixer write the manifest targets.
KEY RULES: tests execute the real helper against a real model (no mocks of EF types); AAA; one behavior per test; no Thread.Sleep; the guard tests include the default-parameter case as a valid result; inspect the model through IEntityType, IKey and IProperty.
REFERENCE FILES: tests/Encina.UnitTests/EntityFrameworkCore/SoftDelete/SoftDeleteTestDbContext.cs, tests/Encina.GuardTests/EntityFrameworkCore/Extensions/ServiceCollectionExtensionsGuardTests.cs, .github/coverage-manifest/Encina.EntityFrameworkCore.json
```

</details>

### Phase 3: Integration tests and justifications (PR 2)

<details>
<summary>Tasks</summary>

1. `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/ShadowKey/ShadowKeyDbContext.cs`: `ShadowKeyDbContext` with `Customer` and `Invoice` aggregates configured with `ConfigureShadowPrimaryKey` and `ConfigureAggregateRoot`, and the foreign key `Invoice.CustomerId` to the Customer alternate key.
2. `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/SqlServer/ShadowKey/ShadowKeySqlServerTests.cs` (`[Collection("EFCore-SqlServer")]`, `[Trait("Category", "Integration")]`, `[Trait("Provider", "EFCore.SqlServer")]`), `.../PostgreSQL/ShadowKey/ShadowKeyPostgreSqlTests.cs` and `.../MySQL/ShadowKey/ShadowKeyMySqlTests.cs` (the MySQL one skips through `Assert.SkipUnless(_fixture.IsAvailable, ...)` until #2086): create the tables with the `IRelationalDatabaseCreator` in `InitializeAsync` (as `IdGenerationConverterSqlServerTests` does), then assert the schema from `INFORMATION_SCHEMA` (primary key on the shadow column only, unique constraint on the Guid column, the foreign key referencing it) and the behavior (generated increasing `PhysicalId`, round trip by `Id`, duplicate Guid fails with `DbUpdateException`, an invoice with a missing customer fails on the foreign key).
3. `tests/Encina.BenchmarkTests/EntityFrameworkCore/ShadowPrimaryKey.md` and `tests/Encina.LoadTests/ShadowPrimaryKey.md` (justification format of AGENTS.md section 9; adequate coverage from the unit, guard and integration tests; alternative: measure with the application's own workload).
4. Manifest: raise the `integration` target of `Configuration/EntityConfigurationExtensions.cs` to the measured value (via `mechanical-fixer`), run `--check-justifications`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Phases 1 and 2 are merged. Integration tests for EF Core use the shared collections EFCore-SqlServer, EFCore-PostgreSQL and EFCore-MySQL (tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Collections.cs) and the fixtures of tests/Encina.TestInfrastructure/Fixtures/EntityFrameworkCore/.
TASK: Write the integration tests and the two justification files of Phase 3; keep the MySQL test skipped by the fixture, never deleted.
KEY RULES: shared [Collection] fixtures only, never IClassFixture or a per-class fixture; never call the fixture's DisposeAsync; Assert.SkipUnless(_fixture.IsAvailable, ...) first in every test; ClearAllDataAsync or RemoveRange in InitializeAsync; real databases, no mocks; Shouldly.
REFERENCE FILES: tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/SqlServer/IdGeneration/IdGenerationConverterSqlServerTests.cs, tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/IdGeneration/IdGenerationDbContext.cs, tests/Encina.TestInfrastructure/Fixtures/EntityFrameworkCore/EFCoreSqlServerFixture.cs, tests/Encina.BenchmarkTests/ModuleIsolation.md
```

</details>

### Phase 4: Documentation and finalization (PR 2)

<details>
<summary>Tasks</summary>

1. `src/Encina.EntityFrameworkCore/README.md` (via `docs-writer`): add `ConfigureShadowPrimaryKey()` to the configuration helper table next to `ConfigureAggregateRoot()` (`README.md:596`).
2. `docs/features/shadow-primary-key.md` and its entry in `docs/features/index.md` (via `docs-writer`): when to use the pattern (high-volume tables, brownfield tables), when not (small tables, sequential Guids already mitigate fragmentation), the usage and the cross-aggregate `HasPrincipalKey` example, the `Find` and repository limitation of choice 7 with the workaround, why ADO.NET, Dapper and MongoDB are not covered; no hand-typed figures.
3. `docs/architecture/adr/<number>-shadow-primary-key-guid-alternate-key.md` and its row in `docs/architecture/adr/index.md` (via `docs-writer`; the ADR number is assigned when the ADR pull request opens): the decision, the alternatives the issue rejected (source generator, convention, no helper) and choices 1, 7 and 8.
4. `ROADMAP.md`, `docs/INVENTORY.md` and `docs/releases/` need no change (no new package or module).
5. Build `dotnet build --configuration Release` (0 warnings), `dotnet test` of the unit, guard and integration projects filtered to the new classes, `--check-justifications`, the changelog check.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Phases 1 to 3 are done and green.
TASK: Document the helper: the Encina.EntityFrameworkCore README table row, the feature page docs/features/shadow-primary-key.md and the ADR with its index row; run the verification listed in Phase 4.
KEY RULES: documentation is written by docs-writer (never by the implementer); one Diataxis quadrant per page; real API names verified in src/; no hand-typed coverage or performance figures; ADR number assigned when the ADR pull request opens; the changelog fragment is written by mechanical-fixer.
REFERENCE FILES: src/Encina.EntityFrameworkCore/README.md, docs/features/immutable-domain-models.md, docs/architecture/adr/062-error-category-contract.md, docs/architecture/adr/index.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| EF Core alternate keys, shadow properties and value conversions | The three mechanisms the helper combines; behavior verified on 10.0.12 on 2026-11-17 |
| "EF Core Is DDD (Here Is Why)", Zoran Horvat (the issue's reference) | Origin of the pattern |
| ADR-024 and AGENTS.md section 5 | SQLite is outside the provider matrix |
| AGENTS.md section 9 | Per-flag coverage, integration tests on real databases, justification files |
| #2086 | EF Core MySQL integration tests skip until Pomelo supports EF Core 10 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `EntityConfigurationExtensions` | `src/Encina.EntityFrameworkCore/Configuration/EntityConfigurationExtensions.cs` | Hosts the helper; `ConfigureAggregateRoot` at `:353` composes with it |
| `IEntity<TId>` | `src/Encina.DomainModeling/IEntity.cs` | Constraint of `TEntity` |
| `GuidStronglyTypedId<TSelf>` (`New`, `From`) | `src/Encina.DomainModeling/StronglyTypedId.cs:207-258` | Constraint of `TId`, conversion factory |
| `AggregateRoot<TId>` | `src/Encina.DomainModeling/AggregateRoot.cs:55` | Aggregates of the tests |
| `FunctionalRepositoryEF<TEntity, TId>` | `src/Encina.EntityFrameworkCore/Repository/FunctionalRepositoryEF.cs:79` | `FindAsync([id])`; limitation of choice 7 |
| EF Core fixtures and collections | `tests/Encina.TestInfrastructure/Fixtures/EntityFrameworkCore/`, `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Collections.cs` | Integration tests |
| `Encina.EntityFrameworkCore.json` | `.github/coverage-manifest/Encina.EntityFrameworkCore.json` | Per-file targets |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.EntityFrameworkCore | none | Configuration-time code; it logs nothing and the block 750-799 stays unused |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production | 1 | `EntityConfigurationExtensions.cs` (PR 1) |
| Unit and guard tests | 3 | `ShadowPrimaryKeyConfigurationTests.cs`, `ShadowPrimaryKeyTestDbContext.cs`, `EntityConfigurationExtensionsShadowKeyGuardTests.cs` (PR 1) |
| Integration tests | 4 | `ShadowKeyDbContext.cs`, SqlServer, PostgreSQL and MySQL test classes (PR 2) |
| Justifications | 2 | benchmark and load `.md` files (PR 2) |
| Docs and metadata | 7 | `README.md`, `docs/features/shadow-primary-key.md`, its entry in `docs/features/index.md`, the ADR, `docs/architecture/adr/index.md`, the changelog fragment (PR 1), `.github/coverage-manifest/Encina.EntityFrameworkCore.json` (both PRs) |
| **Total** | **17 distinct** | PR 1: 6 files (about 330 lines); PR 2: 12 files (about 840 lines, the manifest counted again). No `ProjectReference`, DI registration or test-double changes |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14, EF Core 10). Issue #1039: ConfigureShadowPrimaryKey<TEntity, TId> in Encina.EntityFrameworkCore (shadow int primary key plus the strongly-typed Guid Id as alternate key). AGENTS.md rules apply (zero warnings, per-flag coverage, CRAP <= 10, shared collection fixtures, English only).

IMPLEMENTATION OVERVIEW: (1) the extension method in EntityConfigurationExtensions with argument validation, the shadow property, HasKey, HasAlternateKey and the Guid conversion; (2) model-level unit tests and guard tests; (3) integration tests on SQL Server, PostgreSQL and MySQL (skipped by its fixture) reading the schema from INFORMATION_SCHEMA, plus benchmark and load justification files; (4) README row, feature page, ADR, changelog fragment, manifest targets. Two pull requests: Phases 1-2, then Phases 3-4.

KEY PATTERNS: fluent EntityTypeBuilder helper; exceptions only for configuration errors; order-independent keys; no PublicAPI line (package tracks none); docs by docs-writer, fragment and manifest by mechanical-fixer; the repository limitation (FindAsync by primary key) is documented and tracked by a follow-up issue.

REFERENCE FILES: src/Encina.EntityFrameworkCore/Configuration/EntityConfigurationExtensions.cs, src/Encina.DomainModeling/StronglyTypedId.cs, src/Encina.DomainModeling/AggregateRoot.cs, tests/Encina.UnitTests/EntityFrameworkCore/SoftDelete/SoftDeleteTestDbContext.cs, tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/SqlServer/IdGeneration/IdGenerationConverterSqlServerTests.cs, .github/coverage-manifest/Encina.EntityFrameworkCore.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Configuration-time only; no reads |
| 2 | OpenTelemetry | ❌ | No runtime activity, so no tracing and no ADR-054 D4 tenant tags |
| 3 | Structured Logging | ❌ | The helper logs nothing; no EventId is allocated |
| 4 | Health Checks | ❌ | No dependency to check |
| 5 | Validation | ❌ | Arguments are checked by exceptions at model-build time (choice 4); no request input |
| 6 | Resilience | ❌ | No external call |
| 7 | Distributed Locks | ❌ | No shared state |
| 8 | Transactions | ❌ | The unit-of-work behavior is unchanged; no state change |
| 9 | Idempotency | ❌ | Calling the helper twice leaves the same model (unit tested) |
| 10 | Multi-Tenancy | ❌ | Tenant filters compose orthogonally; the Guid `Id` is globally unique, no tenant-qualified key is created, so the reserved `default` tenant segment does not apply |
| 11 | Module Isolation | ❌ | Module-independent |
| 12 | Audit Trail | ⏭️ | Audit columns are unaffected (`ConfigureAggregateRoot` keeps them), but `AuditInterceptor.GetEntityId` (`Auditing/AuditInterceptor.cs:328`) and `DbContextKeyExtensions.cs` read `FindPrimaryKey()`, so for an entity configured with this helper the audited entity id would be the internal `int` `PhysicalId`; deferred with choice 7 to the follow-up issue `artifacts/issues/ef-repository-shadow-primary-key.md` (`[DEBT]`, already lists `AuditInterceptor`; the worker adds `DbContextKeyExtensions.cs`) |

---

## Next Steps

1. Commit this plan, open its PR and link it from #1039.
2. Open the follow-up issue `artifacts/issues/ef-repository-shadow-primary-key.md` (the repository and bulk operations with a shadow primary key, choice 7).
3. When #1039 is picked up, re-check the choices against today's code and ask the maintainer about the Dilemmas (choices 1, 7 and 8).
4. Brief a worker for PR 1 (Phases 1-2, 6 files), then PR 2 (Phases 3-4, 12 files) after PR 1 is merged.
