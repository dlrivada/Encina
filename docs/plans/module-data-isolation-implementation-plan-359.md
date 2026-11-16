# Implementation Plan: Data Isolation per Module (registration on all providers, script generators, `[ModuleSchema]`, EF schema, Roslyn analyzer)

> **Issue**: [#359](https://github.com/dlrivada/Encina/issues/359)
> **Type**: Feature
> **Complexity**: High (7 phases, 10 database providers, 8 pull requests of 11, 12, 11, 11, 6, 15, 13 and 5 files)
> **Estimated Scope**: about 1,500 lines of production code and about 3,000 lines of tests, split over eight pull requests
> **Family**: modular monolith (#352 umbrella, #358, #360, #364); this plan owns the data half of module isolation

---

## Summary

The issue proposes a new `Encina.Modular.Data` package with a `ModuleDataIsolation` enum, `[ModuleSchema]`, `IModuleDbContext<TModule>`, a Roslyn analyzer and EF extension methods. Most of the runtime already exists under other names, and reading it shows real gaps that the issue does not mention. State of `origin/main` (`e694eb032`), verified by reading and grep:

| Issue item | Today | Owner of what remains |
|------------|-------|-----------------------|
| `ModuleDataIsolation` (None, SeparateSchema, SeparateDatabase) | `ModuleIsolationStrategy` (`DevelopmentValidationOnly`, `SchemaWithPermissions`, `ConnectionPerModule`), `ModuleIsolationOptions`, `ModuleSchemaOptions`, `IModuleSchemaRegistry` (core `src/Encina/Modules/Isolation/`), documented in `docs/features/module-isolation.md` | Nothing new (Design Choice 2) |
| Runtime enforcement (`FailOnCrossModuleQuery`) | `SchemaValidatingCommand` and `ModuleAwareConnectionFactory` in six ADO.NET and Dapper packages, `ModuleSchemaValidationInterceptor` (EF Core), `ModuleAwareMongoCollectionFactory`; a violation throws `ModuleIsolationViolationException` | Registration on all providers (gap 1) |
| Migration helpers for schema separation | `IModulePermissionScriptGenerator` with `SqlServerPermissionScriptGenerator` and `PostgreSqlPermissionScriptGenerator`; `GeneratePermissionScripts` and `PermissionScriptsOutputPath` on `ModuleIsolationOptions` | Gaps 2 and 3 |
| `[ModuleSchema]`, `IModuleDbContext<TModule>`, `ApplyModuleSchema`, `ApplyModuleBoundaryFilters` | Missing | Phase 5 |
| Roslyn analyzer (cross-module data access) | Missing; `Encina.Testing` has runtime architecture rules (`ModuleArchitectureRules.ModulesShouldNotAccessOtherModulesData`) | Phase 6 |

Four gaps found while reading the code, none in the issue:

1. **Four providers do not register module isolation, and at runtime the gate is silent.** The limitation is documented in the package READMEs (`src/Encina.ADO.PostgreSQL/README.md:170`, `src/Encina.ADO.MySQL/README.md:200`, `src/Encina.Dapper.PostgreSQL/README.md:165` and `src/Encina.Dapper.MySQL/README.md:164`: "does not support `UseModuleIsolation`"), but nothing fails when an application sets it, so the defect is the silent runtime behavior. Pull requests 3 and 4 make those four sentences false and edit them. `RegisterModuleIsolation` exists only in `Encina.ADO.SqlServer`, `Encina.Dapper.SqlServer`, `Encina.EntityFrameworkCore` and `Encina.MongoDB`. `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`, `Encina.Dapper.PostgreSQL` and `Encina.Dapper.MySQL` contain no `UseModuleIsolation` code (grep of `src/` finds the property only in the four registering packages and in `MessagingConfiguration`), although their `ModuleAwareConnectionFactory` and `SchemaValidatingCommand` classes exist. `config.UseModuleIsolation = true` there registers nothing, so no validation runs: a gate that fails open (`AGENTS.md` section 3, registration completeness and gates that fail closed). The integration tests of those packages build the wrappers by hand, so no test saw it.
2. **Permission script generators exist for two of four engines.** SQL Server and PostgreSQL have one; MySQL (the schema is a database) and MongoDB (database per module, `MongoDbModuleIsolationOptions`) have none. `AGENTS.md` section 5 requires provider-dependent features on all 10 providers.
3. **The generator default is wrong for two engines and the options are dead.** The registration helpers of ADO.SqlServer, Dapper.SqlServer and EF Core register `SqlServerPermissionScriptGenerator` by `TryAddSingleton`, so an EF Core PostgreSQL user receives the SQL Server generator. Nothing reads `GeneratePermissionScripts` or `PermissionScriptsOutputPath`; only the XML docs mention them.
4. **The module producer is incomplete** (the context is set only by an EF Core behavior): owned by #352, whose pull request 1 moves it to core. Data isolation needs it, so this plan depends on it for the end-to-end tests of ADO.NET, Dapper and MongoDB.

**Provider category**: Database (10): ADO.NET, Dapper and EF Core on SQL Server, PostgreSQL and MySQL, and MongoDB. The plan states per phase which providers change; `AGENTS.md` section 5.

**Terminology**: ADR-050 uses "module" for an Encina feature module; this plan says "application module" for `IModule`.

---

## Design Choices

<details>
<summary><strong>1. Placement: core, the existing provider packages and one new analyzer package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep isolation in `Encina` core and the provider packages where it is; add the analyzer as a new `Encina.Modules.Analyzers` package** | No move of shipped code; an analyzer must be a separate `netstandard2.0` package anyway | One new package |
| **B) A new `Encina.Modular.Data` package, as the issue says, with the analyzer inside** | One package for the feature | The isolation types would move out of core and four provider packages would re-point; an analyzer cannot live in a `net10.0` library |

### Chosen Option: **A — keep the placement, one new analyzer package**

### Rationale

The runtime ships in core and the providers (`ROADMAP.md` line 329); the only new project is the analyzer, shaped like the existing `Encina.Security.ABAC.Analyzers` (a `netstandard2.0` Roslyn component with `IncludeBuildOutput` off and the DLL packed under `analyzers/dotnet/cs`).

**Class:** Trivial
**Settled by:** existing placement (`src/Encina/Modules/Isolation/`, `src/Encina.*/Modules/`) and the precedent `src/Encina.Security.ABAC.Analyzers/Encina.Security.ABAC.Analyzers.csproj`.

</details>

<details>
<summary><strong>2. The isolation mode enum: keep `ModuleIsolationStrategy`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep `ModuleIsolationStrategy`; the issue's `None` is "module isolation off" (`UseModuleIsolation = false`), `SeparateSchema` is `SchemaWithPermissions`, `SeparateDatabase` is `ConnectionPerModule`** | One enum, documented and tested | The issue's names are not used |
| **B) Add `ModuleDataIsolation` beside it** | Matches the issue text | Two enums for one concept: a compatibility layer |

### Chosen Option: **A — keep the existing enum**

### Rationale

`AGENTS.md` section 3 forbids parallel aliases; the three strategies already ship with providers and tests (`ModuleIsolationContractTests`, the ten `ModuleIsolation*IntegrationTests` files). The issue's `EnforceDataIsolation`, `DataIsolation` and `FailOnCrossModuleQuery` switches are `UseModuleIsolation`, `Strategy` and the exception of `DevelopmentValidationOnly`.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (no compatibility aliases or layers) and `docs/features/module-isolation.md` (the shipped strategies).

</details>

<details>
<summary><strong>3. How a module declares its schema (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[ModuleSchema("orders")]` on the module class, read by an explicit `options.AddModuleSchemaFrom<TModule>()` that creates the module to read its `Name`** | Schema and module are declared once, in one place; no merge magic; no ordering dependency between `AddEncinaModules` and the database package | The call is still explicit per module |
| **B) The attribute is discovered automatically: `ModuleSchemaRegistry` merges the attributes of every registered module at construction** | No per-module call | The database packages register `ModuleIsolationOptions` and the registry independently of `AddEncinaModules`; the registry gains a dependency on `IModuleRegistry`, and the permission generators (which take `ModuleIsolationOptions`) would no longer see the merged set |
| **C) No attribute; keep `options.AddModuleSchema("Orders", "orders")` only** | Nothing new | The module name is a second string that can drift from `IModule.Name` |

### Chosen Option: **A — attribute plus explicit `AddModuleSchemaFrom<TModule>()`** (recommended, pending the maintainer)

### Rationale

A wins when the options instance must stay the single source of truth for the registry, the interceptors and the script generators, which is how the code is built. B wins for applications with many modules that want zero per-module registration and accept changing the generator inputs. C wins if the maintainer prefers no attribute. A module that carries both an attribute and an explicit `AddModuleSchema` for the same name with a different schema throws `InvalidOperationException` at registration. `AddModuleSchemaFrom<TModule>() where TModule : IModule, new()` instantiates the module at registration to read its `Name`, so a module that needs constructor arguments cannot use it and keeps `AddModuleSchema`.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. EF Core boundary: validate the model, not a query filter</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ModelBuilder.ValidateModuleBoundaries(...)` inspects every entity type's table schema when the model is built and throws `ModuleIsolationViolationException` for a schema the module may not use** | Fails at the first use of the context, before any SQL; works for every relational EF provider | Checks mapping, not hand-written SQL (that is `ModuleSchemaValidationInterceptor`'s job) |
| **B) `ApplyModuleBoundaryFilters()` as the issue says, adding a global query filter** | Matches the issue text | A global query filter is a row predicate (`HasQueryFilter`), so it cannot stop access to another schema |

### Chosen Option: **A — model validation**

### Rationale

Only A can satisfy the stated goal (an entity of another module's schema must not be reachable). `ApplyModuleSchema` sets `HasDefaultSchema` from the registry for entities without an explicit schema; `ValidateModuleBoundaries` then checks the explicit ones against `GetAllowedSchemas`.

**Class:** Settled
**Settled by:** Microsoft EF Core documentation on global query filters (https://learn.microsoft.com/ef/core/querying/filters): a query filter is a LINQ predicate applied to entity queries and has no notion of the schema it reads.

</details>

<details>
<summary><strong>5. Which engine's scripts are generated (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ModuleIsolationOptions.ScriptDialect` (`SqlServer`, `PostgreSql`, `MySql`, `MongoDb`, nullable); each ADO.NET, Dapper and MongoDB package sets its own default, EF Core requires it when scripts are requested; a core helper registers the matching generator** | Correct generator for every provider; EF Core stays provider-agnostic | One more option on `ModuleIsolationOptions` |
| **B) Keep a package-level default only (EF Core keeps the SQL Server default)** | No new option | An EF Core PostgreSQL or MySQL application gets the wrong generator |
| **C) Infer the engine from `DbContext.Database.ProviderName`** | No option | There is no `DbContext` instance at registration; scripts would be generated lazily and only for EF Core |

### Chosen Option: **A — an explicit dialect with package defaults** (recommended, pending the maintainer)

### Rationale

A wins when correctness for every provider matters; C wins only if script generation moves to a runtime tool that already has a context. When `GeneratePermissionScripts` is on and no dialect is known, registration throws `InvalidOperationException` (a startup error, never a silent wrong script).

**Class:** Dilemma

</details>

<details>
<summary><strong>6. What the permission-script options do (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A hosted `ModulePermissionScriptWriter` writes the scripts to `PermissionScriptsOutputPath` at startup when `GeneratePermissionScripts` is on** | Makes the documented options real; the "easy migration tooling" acceptance criterion is met without a new tool | Writes files at application start (opt-in, intended for development and deployment pipelines) |
| **B) Remove the two unused options; scripts are produced on demand by calling the generator** | No file writes; less code | The documented flow (`config.ModuleIsolation.GeneratePermissionScripts = true`) is deleted; users call the generator themselves |
| **C) A command-line tool** | Runs outside the application | A new tool and package; depends on the CLI plans |

### Chosen Option: **A — a hosted writer** (recommended, pending the maintainer)

### Rationale

A wins when a team wants the scripts regenerated by the application in development. B wins if the maintainer prefers a library with no file writes (the pre-1.0 rule is to remove code that serves no purpose, and today these two options serve none). C wins when scripts must come from a build step. The writer never overwrites a file whose content differs without logging it, and never writes outside `PermissionScriptsOutputPath`.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Script generators for MySQL and MongoDB</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add `MySqlPermissionScriptGenerator` and `MongoDbPermissionScriptGenerator` (a mongosh script), so all four engines have one** | Provider coherence; the script flow works for every provider | Two more generators and their integration tests |
| **B) Leave MySQL and MongoDB without scripts and document it** | Less code | The feature exists for 2 of 4 engines |

### Chosen Option: **A — all four engines**

### Rationale

`AGENTS.md` section 5 requires a provider-dependent feature to exist for every provider of its category. MySQL treats a schema as a database (`CREATE DATABASE`, `CREATE USER`, `GRANT ... ON db.*`); MongoDB isolates by database per module (`MongoDbModuleIsolationOptions.EnableDatabasePerModule`) with `createUser` and roles per database.

**Class:** Settled
**Settled by:** `AGENTS.md` section 5 (every provider-dependent feature MUST be implemented for all 10 providers; the script generator is chosen by engine).

</details>

<details>
<summary><strong>8. One core registration helper for all packages</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaModuleIsolationServices(IServiceCollection, ModuleIsolationOptions, ModuleScriptDialect)` in core, called by the eight packages** | One place registers the options, the registry, the context and the generator; the four missing packages cannot drift again | A new public helper |
| **B) Copy the registration block into the four packages that lack it** | No new public API | The copies already drifted (four packages have none; three register a SQL Server default) |

### Chosen Option: **A — one helper**

### Rationale

A shared registration helper is the established way to keep eight registrations in step, as `OperationAuditStoreRegistration` does for the audit stores.

**Class:** Trivial
**Settled by:** precedent `src/Encina.Security.Audit/OperationAuditStoreRegistration.cs`, and `AGENTS.md` section 3 (registration completeness proven by a DI test).

</details>

<details>
<summary><strong>9. Analyzer scope and identifiers (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Three diagnostics in `Encina.Modules.Analyzers`: `ENCMOD001` a module's code uses a `DbContext` that another module owns (`[ModuleDbContext]`), `ENCMOD002` a SQL string literal names a schema owned by another module, `ENCMOD003` one SQL literal joins two or more module schemas** | Matches the issue's three rules; static, no runtime cost | Literal detection is heuristic; code that builds SQL at runtime is covered by the runtime validation only |
| **B) Only `ENCMOD001`** | Precise | `ENCMOD002` and `ENCMOD003` of the issue are dropped |
| **C) Three diagnostics plus a code fix** | Helps adoption | A code-fix has no safe rewrite for a cross-module access |

### Chosen Option: **A — three diagnostics, no code fix** (recommended, pending the maintainer)

### Rationale

A wins when compile-time feedback matters more than perfect recall (the runtime strategies catch the rest). B wins if false positives on string literals are a concern for the maintainer. The decision also covers the new public name `[ModuleDbContext(typeof(TModule))]` (core, `Encina.Modules.Isolation`), which marks the `DbContext` a module owns so that `ENCMOD001` can tell owner from user; the alternative is to infer ownership from `ModuleDbContext<TModule>` alone, which would miss plain `DbContext` classes. The prefix `ENCMOD` avoids the issue's `ENC001` (too generic) and follows the per-feature prefix of the ABAC analyzers (`EEL`). A module owns a type when the type is declared in the assembly of the module class, or in the namespace of the module class when an assembly holds several modules; the schemas come from `[ModuleSchema]` metadata, which is readable across assemblies.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Script generators for MySQL and MongoDB (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina/Modules/Isolation/MySqlPermissionScriptGenerator.cs` (public sealed, `IModulePermissionScriptGenerator`, `ProviderName` "MySQL"): `CREATE DATABASE IF NOT EXISTS` for each module schema, `CREATE USER IF NOT EXISTS`, `GRANT` on `` `schema`.* `` to the module user and `SELECT` on shared schemas, `REVOKE` cleanup; identifiers quoted with backticks and validated (no quotes inside, length bound) so a module or schema name can never inject SQL.
2. `src/Encina/Modules/Isolation/MongoDbPermissionScriptGenerator.cs` (`ProviderName` "MongoDB"): a mongosh script per module that creates the module database user with a role on its database and read access to shared databases; names validated the same way.
3. Reuse `PermissionScript` ordering (000 revoke, 001 schemas, 002 users, 003 grants, module scripts).
4. Tests: unit `MySqlPermissionScriptGeneratorTests`, `MongoDbPermissionScriptGeneratorTests` (every script, quoting, hostile names rejected), guard (`PermissionScriptGeneratorGuardTests` extended), property `PermissionScriptGeneratorPropertyTests` (for any valid module set, every generator mentions every module schema and no script contains an unquoted hostile name), integration `ModuleIsolationScriptsMySqlIntegrationTests` (applies the generated scripts to a MySQL container and proves a module user cannot read another module's schema) and `ModuleIsolationScriptsMongoDbIntegrationTests` (runs the script with `mongosh` inside the Testcontainers MongoDB container); both use the existing `[Collection]` fixtures.
5. PublicAPI lines and `Encina.json` entries through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: IModulePermissionScriptGenerator has SQL Server and PostgreSQL implementations in src/Encina/Modules/Isolation. MySQL (schema = database) and MongoDB have none. Choice 7 is settled: all four engines.
TASK: Add MySqlPermissionScriptGenerator and MongoDbPermissionScriptGenerator with unit, guard, property and integration tests.
KEY RULES: Identifiers are validated and quoted, never concatenated raw (a module name comes from application code but a script is a security boundary); no [Obsolete]; integration tests use the shared [Collection] fixtures, never a per-class container and never dispose the fixture; keep every method at CRAP <= 10.
REFERENCE FILES: src/Encina/Modules/Isolation/PostgreSqlPermissionScriptGenerator.cs, src/Encina/Modules/Isolation/SqlServerPermissionScriptGenerator.cs, tests/Encina.UnitTests/Core/Modules/Isolation/PostgreSqlPermissionScriptGeneratorTests.cs, tests/Encina.GuardTests/Core/Modules/PermissionScriptGeneratorGuardTests.cs, tests/Encina.IntegrationTests/ADO/MySQL/ModuleIsolation/ModuleIsolationADOIntegrationTests.cs.
```

</details>

### Phase 2: Core registration helper, dialect and script writer (pull request 2)

<details>
<summary>Tasks</summary>

1. `src/Encina/Modules/Isolation/ModuleScriptDialect.cs`: enum `SqlServer`, `PostgreSql`, `MySql`, `MongoDb`; `ModuleIsolationOptions.ScriptDialect` (`ModuleScriptDialect?`).
2. `src/Encina/Modules/Isolation/ModuleIsolationServiceCollectionExtensions.cs`: `AddEncinaModuleIsolationServices(this IServiceCollection, ModuleIsolationOptions, ModuleScriptDialect? defaultDialect)`: registers the options singleton, `TryAddSingleton<IModuleSchemaRegistry, ModuleSchemaRegistry>()`, `TryAddScoped<IModuleExecutionContext, ModuleExecutionContext>()`, the generator for `options.ScriptDialect ?? defaultDialect` by `TryAddSingleton` and, when `GeneratePermissionScripts` is on, the writer; throws `InvalidOperationException` when scripts are requested with no dialect or no output path.
3. `src/Encina/Modules/Isolation/ModulePermissionScriptWriter.cs` (internal hosted service, D6): writes `GenerateAllScripts` results under `PermissionScriptsOutputPath`; the file name comes from `PermissionScript.Name` (generated constants plus a lower-cased module name), the resolved path must stay inside the output directory, an existing file with different content is overwritten only after a warning log, failures are logged by error code and rethrown (the host fails to start rather than reporting success).
4. `src/Encina/Modules/Diagnostics/ModuleLog.cs` (existing `ModuleIsolation` range 1900-1999, ids 1900-1906 used): two new `[LoggerMessage]` methods, `PermissionScriptOverwritten` (Warning, id 1907) and `PermissionScriptWriteFailed` (Error, id 1908, error code and file name only, never the exception message), packed from 1907; the `ModuleLog` XML remarks stay; the two new public methods get their PublicAPI lines in this pull request, and the writer unit tests assert that the warning and the error are logged (a fake logger).
5. Tests: unit (helper registration for each dialect, `ValidateOnBuild` and `ValidateScopes`; writer with a fake file system path under a temp directory and the two log calls; options), guard, PublicAPI, `Encina.json`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Pull request 1 merged the four generators. D5 (explicit dialect with package defaults) and D6 (hosted writer) are implemented as the maintainer answered, the recommended options until then. Eight packages will call the helper.
TASK: Add the dialect enum, the helper and the writer, with a DI test that builds the provider with ValidateOnBuild and ValidateScopes for every dialect.
KEY RULES: A requested script generation with no dialect or no path is a registration error, never a silent skip; the writer never writes outside its directory (Path.GetFullPath prefix check) and never swallows an IO error; use TimeProvider if time is needed; no scoped service injected into a singleton.
REFERENCE FILES: src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs (RegisterModuleIsolation), src/Encina/Modules/Isolation/ModuleIsolationOptions.cs, src/Encina.Security.Audit/OperationAuditStoreRegistration.cs.
```

</details>

### Phase 3: All eight packages adopt the helper (pull requests 3, 4 and 5)

<details>
<summary>Tasks</summary>

1. **Pull request 3, ADO.NET**: `Encina.ADO.PostgreSQL` and `Encina.ADO.MySQL` gain `RegisterModuleIsolation` (the helper call with their dialect) and the `ModuleAwareConnectionFactory` wrapping in the `AddEncinaADO` overload that takes a connection factory, mirroring `Encina.ADO.SqlServer` (`ServiceCollectionExtensions.cs` lines 156-172 and 282-300); `Encina.ADO.SqlServer` switches its block to the helper. The sentence "`AddEncinaADO` for PostgreSQL/MySQL does not support `UseModuleIsolation`" in `src/Encina.ADO.PostgreSQL/README.md` and `src/Encina.ADO.MySQL/README.md` is replaced by a description of the registered isolation (through `docs-writer`).
2. **Pull request 4, Dapper**: the same for `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL` and `Encina.Dapper.SqlServer`, and the same README sentence ("This registration does not support `UseModuleIsolation`") in `src/Encina.Dapper.PostgreSQL/README.md` and `src/Encina.Dapper.MySQL/README.md`.
3. **Pull request 5, EF Core and MongoDB**: `Encina.EntityFrameworkCore` replaces its block and its unconditional SQL Server generator by the helper (dialect required for scripts, D5); `Encina.MongoDB` calls the helper with `MongoDb`.
4. Tests per pull request: a DI test with `ValidateOnBuild` and `ValidateScopes` proving that with `UseModuleIsolation = true` every package registers the options, the registry, the context and, for ADO.NET and Dapper, a wrapped `IDbConnection`; integration tests that build the provider through `AddEncinaADO` or `AddEncinaDapper` (not the wrapper by hand) and prove a cross-schema statement throws `ModuleIsolationViolationException` for PostgreSQL and MySQL (extending `ModuleIsolationADOIntegrationTests` and `ModuleIsolationDapperIntegrationTests` of those two providers), and for MongoDB through `AddEncinaMongoDB`.
5. Coverage manifests of every edited package (through `mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: ADO.PostgreSQL, ADO.MySQL, Dapper.PostgreSQL and Dapper.MySQL register no module isolation although UseModuleIsolation exists; the other four register it by copy. Choice 8 gives one helper.
TASK: Call the core helper from every package, add the wrapped-connection registration to the four missing ones, and add the DI and end-to-end integration tests.
KEY RULES: A database store or registration must win over defaults in any order (TryAdd for defaults); no scoped service in a singleton; integration tests use the shared [Collection("ADO-PostgreSQL")] style fixtures, ClearAllDataAsync in InitializeAsync, never IClassFixture; test all providers of the pull request.
REFERENCE FILES: src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs, src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs, src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs, src/Encina.MongoDB/ServiceCollectionExtensions.cs, tests/Encina.IntegrationTests/Dapper/PostgreSQL/ModuleIsolation/ModuleIsolationDapperIntegrationTests.cs.
```

</details>

### Phase 4: `[ModuleSchema]`, `IModuleDbContext<TModule>` and the EF Core schema (pull request 6)

<details>
<summary>Tasks</summary>

1. `src/Encina/Modules/Isolation/ModuleSchemaAttribute.cs` (`[AttributeUsage(AttributeTargets.Class)]`, `SchemaName` validated) and `ModuleIsolationOptions.AddModuleSchemaFrom<TModule>() where TModule : IModule, new()` (D3); a conflict with an explicit entry throws `InvalidOperationException`.
2. `src/Encina/Modules/Isolation/IModuleDbContext.cs`: `public interface IModuleDbContext<TModule> where TModule : IModule { string Schema { get; } }`.
3. `src/Encina.EntityFrameworkCore/Modules/ModuleDbContext.cs`: `public abstract class ModuleDbContext<TModule> : DbContext, IModuleDbContext<TModule>`, constructor `(DbContextOptions options, IModuleSchemaRegistry registry)`, `Schema` resolved from the registry by the module name, `OnModelCreating` calls `ApplyModuleSchema` and `ValidateModuleBoundaries`.
4. `src/Encina.EntityFrameworkCore/Modules/ModelBuilderModuleExtensions.cs`: `ApplyModuleSchema(this ModelBuilder, IModuleSchemaRegistry, string moduleName)` (`HasDefaultSchema`; an unregistered module throws) and `ValidateModuleBoundaries(...)` (D4); MySQL maps a schema to a database, so the phase first verifies what the repository's MySQL EF provider does with `HasDefaultSchema` (`Directory.Packages.props` pins `Pomelo.EntityFrameworkCore.MySql` 9.0.0 and `ModuleIsolationEFMySqlTests` is skipped until a Pomelo release for EF Core 10 exists) and adds a MySQL case only for a provider that runs.
5. Tests: unit (one file for the core attribute and options method, one for the two EF extensions) and one guard file; integration: the three EF Core classes (`ModuleIsolationEFSqlServerTests`, `ModuleIsolationEFPostgreSqlTests`, `ModuleIsolationEFMySqlTests`, each `IAsyncLifetime` and sealed, so each is edited) gain a case with a `ModuleDbContext` that creates its tables in its own schema and rejects a foreign-schema entity.
6. PublicAPI of `Encina` and `Encina.EntityFrameworkCore`, manifests of both.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Modules declare schemas through ModuleIsolationOptions.AddModuleSchema today. D3 (attribute plus explicit AddModuleSchemaFrom) is implemented as the maintainer answered, the recommended option until then; D4 is settled: validate the model, never a query filter.
TASK: Add the attribute, AddModuleSchemaFrom, IModuleDbContext, ModuleDbContext, the two ModelBuilder extensions and their tests including the three EF Core integration classes.
KEY RULES: Names compare OrdinalIgnoreCase; a violation throws ModuleIsolationViolationException with the existing error code; an unregistered module is an error, never an empty schema; the DbContext constructor takes the registry by injection, never from a static; integration tests use the shared EFCore-* collection fixtures.
REFERENCE FILES: src/Encina/Modules/Isolation/ModuleIsolationOptions.cs, src/Encina/Modules/Isolation/ModuleSchemaRegistry.cs, src/Encina.EntityFrameworkCore/Modules/ModuleSchemaValidationInterceptor.cs, tests/Encina.TestInfrastructure/DbContexts/ModuleIsolationTestDbContext.cs, tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/PostgreSQL/ModuleIsolation/ModuleIsolationEFPostgreSqlTests.cs.
```

</details>

### Phase 5: The Roslyn analyzer package (pull request 7)

<details>
<summary>Tasks</summary>

1. `src/Encina.Modules.Analyzers/Encina.Modules.Analyzers.csproj` shaped like `Encina.Security.ABAC.Analyzers` (`netstandard2.0`, `IsRoslynComponent`, `EnforceExtendedAnalyzerRules`, packed under `analyzers/dotnet/cs`), a `README.md`, and an entry in `Encina.slnx`.
2. `DiagnosticDescriptors.cs` (`ENCMOD001` to `ENCMOD003`, category `Encina.Modules`, default severity Warning for 001 and 002, Error for 003), `ModuleDataIsolationAnalyzer.cs` (`[DiagnosticAnalyzer(LanguageNames.CSharp)]`, concurrent execution enabled, generated code skipped), and a small internal `ModuleOwnership.cs` that maps a symbol to its module (D9).
3. A `[ModuleDbContext(typeof(TModule))]` attribute is needed by `ENCMOD001`: `src/Encina/Modules/Isolation/ModuleDbContextAttribute.cs` is added in this pull request (core), with its PublicAPI line.
4. Tests: `tests/Encina.UnitTests/Modules/Analyzers/ModuleDataIsolationAnalyzerTests.cs` compile snippets with `CSharpCompilation` and run `CompilationWithAnalyzers` (no new Roslyn testing package: `Microsoft.CodeAnalysis.CSharp` 5.0.0 is already in `Directory.Packages.props`); the unit test project gains a `ProjectReference` to the analyzer project and a `PackageReference` to `Microsoft.CodeAnalysis.CSharp` in the same `Encina.UnitTests.csproj` edit (today only `Encina.IntegrationTests` references it; the version is central in `Directory.Packages.props`); cases: each rule fires, a same-module use does not, generated code is skipped, a shared schema is allowed, a schema-qualified literal of the own module is allowed.
5. Coverage manifest `.github/coverage-manifest/Encina.Modules.Analyzers.json` (new), through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: There is no compile-time check for cross-module data access; Encina.Testing has runtime architecture rules only. The only existing analyzer package is Encina.Security.ABAC.Analyzers, which has no tests in the repository.
TASK: Create the analyzer project, the three diagnostics, ModuleDbContextAttribute in core and the unit tests with a CompilationWithAnalyzers harness.
KEY RULES: netstandard2.0 and C# language version as the ABAC analyzers; no I/O and no Compilation retention across callbacks; EnableConcurrentExecution and ConfigureGeneratedCodeAnalysis(None); diagnostics message text never contains user data other than type and module names; zero warnings including RS rules.
REFERENCE FILES: src/Encina.Security.ABAC.Analyzers/Encina.Security.ABAC.Analyzers.csproj, src/Encina.Security.ABAC.Analyzers/RequireConditionAnalyzer.cs, src/Encina.Security.ABAC.Analyzers/DiagnosticDescriptors.cs, tests/Encina.UnitTests/Encina.UnitTests.csproj.
```

</details>

### Phase 6: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Module isolation (function 11): this plan is the data half; #352 is the request half, and both read the module the context producer sets.
2. Multi-tenancy (function 10): schema-per-module is independent of the tenant; no tenant-qualified identifier is created (no `default` tenant segment, no injectivity concern), and the script generators never take a tenant. Combined schema-per-module and tenant isolation stays as documented in `docs/features/module-isolation.md` ("Combined with Multi-Tenancy"); the tenant scope primitives are #1257's and #798's (`ResolveWrite`), unrelated to this feature.
3. Fail-closed: a package that cannot enforce isolation must not accept `UseModuleIsolation`; pull requests 3 to 5 make every package register it, and the DI tests prove it.
4. Telemetry (ADR-054 D4): isolation violations have no counter today (seven throw sites: six `SchemaValidatingCommand` classes and the EF interceptor); adding one is a follow-up issue file (`artifacts/issues/module-isolation-violation-telemetry.md`, this plan's worktree). When added, the tenant tag is emitted only with multi-tenancy on.
5. Error code and category (ADR-062): this feature adds no `EncinaError` code; violations are `ModuleIsolationViolationException` (code `Encina.ModuleIsolationViolation`), the existing channel; startup configuration errors are `InvalidOperationException`. A future `Either` form of the violation would be `ErrorCategory.Forbidden`.
6. EventIds: the script writer logs, so its two messages take ids 1907 and 1908 of the existing `ModuleIsolation` range 1900-1999 (1900-1906 used, packed; Phase 2 task 4); no new range is requested.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Isolation is enforced per provider and per request; the integrations above are decisions, not new code, except the DI proofs of Phase 3.
TASK: Verify the integrations listed exist and that the follow-up issue file for the violation counter is written.
KEY RULES: No tenant concept enters schema names; never log SQL text or exception messages.
REFERENCE FILES: AGENTS.md section 6, docs/features/module-isolation.md, src/Encina/Modules/Isolation/ModuleIsolationViolationException.cs.
```

</details>

### Phase 7: Testing, documentation and finalization (pull request 8 for documentation)

<details>
<summary>Tasks</summary>

Per-flag targets for the files this work adds or changes, entered in the package manifest in the pull request that adds their tests. Baselines of the edited files are measured at work start with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests` per flag and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`; each target is the measured value or higher, never below it.

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `Modules/Isolation/MySqlPermissionScriptGenerator.cs` | 95 | 70 | 0 | 80 | 80 |
| `Modules/Isolation/MongoDbPermissionScriptGenerator.cs` | 95 | 70 | 0 | 80 | 80 |
| `Modules/Isolation/ModuleIsolationServiceCollectionExtensions.cs` | 100 | 80 | 0 | 0 | 0 |
| `Modules/Isolation/ModulePermissionScriptWriter.cs` | 90 | 60 | 0 | 0 | 0 |
| `Modules/Isolation/ModuleSchemaAttribute.cs` | 100 | 100 | 0 | 0 | 0 |
| `Modules/Isolation/ModuleDbContextAttribute.cs` | 100 | 100 | 0 | 0 | 0 |
| EF `Modules/ModelBuilderModuleExtensions.cs` | 95 | 70 | 0 | 0 | 80 |
| EF `Modules/ModuleDbContext.cs` | 90 | 60 | 0 | 0 | 80 |
| `Encina.Modules.Analyzers/ModuleDataIsolationAnalyzer.cs` | 90 | 0 | 0 | 0 | 0 |

| File and flag | Justification |
|---------------|---------------|
| Generators unit 95, guard 70, property 80, integration 80 | Every script and quoting branch is unit-testable; guard tests reach the null checks and hostile-name rejections; the property test covers generated module sets; integration runs the scripts on real MySQL and MongoDB containers and misses only error paths of the engines. |
| `ModuleIsolationServiceCollectionExtensions.cs` unit 100, guard 80 | A small registration method whose dialect branches and error branches run in unit tests; guard tests reach the null and invalid-option checks. |
| `ModulePermissionScriptWriter.cs` unit 90, guard 60 | File writing runs against a temp directory; the remaining 10 percent is IO failure after a successful create. |
| Attributes unit 100, guard 100 | One validated constructor each. |
| EF extensions unit 95, guard 70, integration 80 | Model building runs in unit tests with the in-memory relational-free path and in integration tests on SQL Server, PostgreSQL and MySQL; guard tests reach the null checks. |
| Analyzer unit 90, guard 0 | The analyzer has no public parameterized API (the Roslyn entry points take framework types); the unit tests drive each diagnostic through a compilation. |
| Contract 0 for every file above | The new public contracts (`IModuleDbContext<TModule>`, the dialect enum) have no executable lines; `IModulePermissionScriptGenerator` is covered by `ModuleIsolationContractTests`, which is extended with the two new generators in pull request 1. |
| Integration 0 for the registration helper, writer, attributes and analyzer | In-process code with no database; the registration is proven end to end by the integration tests of pull requests 3 to 5; load and benchmark: a startup and compile-time feature, the existing justification files `tests/Encina.LoadTests/ModuleIsolation.md` and `tests/Encina.BenchmarkTests/ModuleIsolation.md` (they exist at the project roots) are extended in pull request 2 to cover script generation and registration; no second file is created. |

1. **Unit**, **Guard**, **Property**, **Integration** as in the phases; **Contract**: `ModuleIsolationContractTests` gains the MySQL and MongoDB generators (pull request 1).
2. **All 10 providers**: registration and end-to-end enforcement are tested for ADO.NET, Dapper and EF Core on SQL Server, PostgreSQL and MySQL and for MongoDB (pull requests 3 to 5); scripts are integration-tested on all four engines (SQL Server and PostgreSQL by the existing tests, MySQL and MongoDB by pull request 1).
3. **Documentation (pull request 8)**: the page `docs/features/module-isolation.md` (provider setup, scripts, attribute, EF schema, analyzer) through `docs-writer` (the analyzer README ships in pull request 7 and the four package README sentences in pull requests 3 and 4), `docs/INVENTORY.md`, `ROADMAP.md`, changelog fragment `changelog.d/359-module-data-isolation.added.md` (and `359-module-isolation-registration.fixed.md` for the four packages), through `mechanical-fixer`; no ADR (no new architectural decision beyond existing ADR-009 and ADR-024 provider scopes).
4. `dotnet build --configuration Release` with zero warnings; all tests green; every flag at its target; CRAP table for every changed method below 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Per-flag coverage targets live in .github/coverage-manifest/*.json; targets never go down; tests execute real code; integration for database features is never a .md justification.
TASK: Write the tests listed, the two justification files (load and benchmark only), the manifests with one-sentence justifications in the same pull request as their tests, the documentation and the changelog fragments through the specialists that own them.
KEY RULES: Shouldly through Encina.Testing.Shouldly; shared [Collection] fixtures; no Thread.Sleep; no hand-typed coverage figures in docs; never edit CHANGELOG.md.
REFERENCE FILES: .github/coverage-manifest/Encina.json, tests/Encina.ContractTests/Database/ModuleIsolation/ModuleIsolationContractTests.cs, docs/features/module-isolation.md, .claude/skills/encina-docs/SKILL.md, changelog.d/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` section 3 | Registration completeness; gates fail closed; no aliases | Gap 1 is a fail-open registration; choice 2 |
| `AGENTS.md` section 5 | Every database-dependent feature on all 10 providers | Generators for four engines; DI tests for eight registrations |
| ADR-009, ADR-024 | Oracle and SQLite are out of the matrix | Not planned |
| ADR-054 D4 | Tenant tag only with multi-tenancy | Violation counter follow-up |
| ADR-062 | `ErrorCategory` on every new `EncinaError` code | No new code |
| EF Core docs on global query filters | Why a filter cannot isolate a schema | Choice 4 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ModuleIsolationOptions`, `ModuleIsolationStrategy`, `ModuleSchemaOptions` | `src/Encina/Modules/Isolation/` | Extended; the shipped model |
| `IModuleSchemaRegistry`, `ModuleSchemaRegistry`, `SqlSchemaExtractor` | `src/Encina/Modules/Isolation/` | Allowed schemas; runtime validation |
| `IModulePermissionScriptGenerator`, the SQL Server and PostgreSQL generators | `src/Encina/Modules/Isolation/` | Pattern for the two new generators |
| `SchemaValidatingCommand`, `ModuleAwareConnectionFactory` | `src/Encina.ADO.*/Modules/`, `src/Encina.Dapper.*/Modules/` | Wrapped connections of the six SQL packages |
| `ModuleSchemaValidationInterceptor` | `src/Encina.EntityFrameworkCore/Modules/` | Runtime check for EF Core SQL |
| `ModuleAwareMongoCollectionFactory`, `MongoDbModuleIsolationOptions` | `src/Encina.MongoDB/Modules/` | MongoDB isolation by database |
| `MessagingConfiguration.UseModuleIsolation`, `ModuleIsolationOptions` | `src/Encina.Messaging/MessagingConfiguration.cs` | Where applications switch it on |
| `ModuleArchitectureRules` | `src/Encina.Testing/Modules/` | Runtime companion of the analyzer |
| `Encina.Security.ABAC.Analyzers` | `src/Encina.Security.ABAC.Analyzers/` | Project shape for the analyzer |
| `ModuleIsolation*` integration tests, `ModuleIsolationTestDbContext` | `tests/Encina.IntegrationTests/`, `tests/Encina.TestInfrastructure/` | Fixtures to extend |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (`ModuleIsolation`, existing) | 1900-1999, ids 1900-1906 used | This plan adds ids 1907 (script overwritten, Warning) and 1908 (script write failed, Error) in `ModuleLog.cs`, packed, no new range; the new range for the rest of the family (7600-7649, from the registry `artifacts/decisions/plan-batch/eventid-blocks.md`) is used by #360 (7600-7619), #352 (7620-7629), #358 (7630-7644) and #364 (7645-7649), so there is no collision |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 23 | core: 8 new (two generators, dialect enum, helper, writer, two attributes, `IModuleDbContext`) and `ModuleIsolationOptions` edited; EF Core: `ModuleDbContext` and `ModelBuilderModuleExtensions` new; 8 package registrations edited (ADO.PostgreSQL, ADO.MySQL, ADO.SqlServer, Dapper.PostgreSQL, Dapper.MySQL, Dapper.SqlServer, EF Core, MongoDB); analyzer project 4 new (csproj, descriptors, analyzer, ownership) plus README and the `Encina.slnx` edit |
| Public API and manifests | 1 to 4 per pull request | one PublicAPI file per touched package with new symbols; one manifest per touched package |
| Tests | about 30 | unit, guard, property, contract edit, integration (MySQL and MongoDB scripts, ADO and Dapper PostgreSQL and MySQL end to end, MongoDB, EF Core three) |
| Justification files | 2 | the existing load and benchmark files at the test project roots, edited (pull request 2) |
| Documentation | 11 | pull request 3: 2 package READMEs; pull request 4: 2 package READMEs; pull request 7: analyzer README (counted in production); pull request 8: page, inventory, roadmap, 2 changelog fragments |
| **Pull request 1** | **11** | 2 generators, PublicAPI, manifest, 7 tests (2 unit, guard edit, property, contract edit, 2 integration) |
| **Pull request 2** | **12** | dialect enum, helper, writer, `ModuleIsolationOptions`, `ModuleLog` (ids 1907-1908), PublicAPI, manifest, 3 tests (helper unit, writer unit including the two log assertions, guard), 2 justification files (edits) |
| **Pull request 3** | **11** | `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`, `Encina.ADO.SqlServer` registrations, 3 manifests, DI test, 2 integration test edits, 2 package READMEs (ADO.PostgreSQL, ADO.MySQL) |
| **Pull request 4** | **11** | the same for the three Dapper packages (3 registrations, 3 manifests, DI test, 2 integration edits) and 2 package READMEs (Dapper.PostgreSQL, Dapper.MySQL) |
| **Pull request 5** | **6** | `Encina.EntityFrameworkCore` and `Encina.MongoDB` registrations, 2 manifests, DI test, MongoDB integration edit |
| **Pull request 6** | **15** | 5 production (`ModuleSchemaAttribute`, `IModuleDbContext`, `ModuleIsolationOptions` edit, `ModuleDbContext`, `ModelBuilderModuleExtensions`), 2 PublicAPI, 2 manifests, 3 unit and guard tests (core unit, EF unit, one guard file), 3 integration edits (`ModuleIsolationEFSqlServerTests`, `ModuleIsolationEFPostgreSqlTests`, `ModuleIsolationEFMySqlTests`) |
| **Pull request 7** | **13** | analyzer csproj, README, descriptors, analyzer, ownership, `ModuleDbContextAttribute`, `Encina.slnx`, unit test project csproj edit, analyzer unit test, attribute guard test, `Encina.Modules.Analyzers.json`, `Encina.json` edit, `Encina` PublicAPI |
| **Pull request 8** | **5** | docs page, `docs/INVENTORY.md`, `ROADMAP.md`, 2 changelog fragments |

Callers and implementers, by grep on `origin/main`: `IModulePermissionScriptGenerator` is implemented by two classes (both in core) and registered in three packages; `RegisterModuleIsolation` is a private method in four packages; `UseModuleIsolation` is read in `MessagingConfiguration`, ADO.SqlServer, Dapper.SqlServer, EF Core and MongoDB. No public interface is changed (only added), so no pull request can leave the build broken; pull request 6 uses `AddModuleSchemaFrom` that it adds itself.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP. Module isolation ships in core (src/Encina/Modules/Isolation) and in the provider packages; ADO.PostgreSQL, ADO.MySQL, Dapper.PostgreSQL and Dapper.MySQL never register it, permission script generators exist only for SQL Server and PostgreSQL, and GeneratePermissionScripts is never read.

IMPLEMENTATION OVERVIEW: PR1 MySQL and MongoDB script generators with real-database integration tests; PR2 ModuleScriptDialect, the core helper AddEncinaModuleIsolationServices and the script writer; PR3-PR5 all eight packages call the helper with DI and end-to-end tests; PR6 [ModuleSchema], AddModuleSchemaFrom, IModuleDbContext, ModuleDbContext and the EF Core ModelBuilder extensions; PR7 the Encina.Modules.Analyzers package with ENCMOD001-003; PR8 documentation. Dilemmas D3, D5, D6 and D9 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: Keep ModuleIsolationStrategy; validate the EF model, never a query filter; identifiers in scripts are validated and quoted; registration errors are InvalidOperationException; a requested isolation that cannot be enforced is an error, never silent; DI tests with ValidateOnBuild and ValidateScopes; shared [Collection] integration fixtures on every database provider; CRAP <= 10; targets never go down.

REFERENCE FILES: src/Encina/Modules/Isolation/*.cs, src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs, src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs, src/Encina.MongoDB/ServiceCollectionExtensions.cs, src/Encina.Security.ABAC.Analyzers/*, tests/Encina.IntegrationTests/*/ModuleIsolation/*, docs/features/module-isolation.md, .github/coverage-manifest/Encina.json.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Module-scoped cache keys are #746 (open). |
| 2 | OpenTelemetry | ⏭️ | Violation counter at the seven throw sites: follow-up issue file `artifacts/issues/module-isolation-violation-telemetry.md` (this plan's worktree); tenant tag only with multi-tenancy (ADR-054 D4). |
| 3 | Structured Logging | ✅ | The script writer logs by error code and file name: `ModuleLog` ids 1907 (warning) and 1908 (error) in the existing 1900-1999 range; no new range. |
| 4 | Health Checks | ⏭️ | Per-module database health is #364 (open, same family). |
| 5 | Validation | ✅ | Registration validates dialect, output path and attribute conflicts; the EF model is validated; identifiers in generated scripts are validated. |
| 6 | Resilience | ❌ | No external call is added; scripts run offline against a database a person chooses. |
| 7 | Distributed Locks | ❌ | The writer runs once per process and writes local files; two instances writing the same directory is a deployment concern documented in the page. |
| 8 | Transactions | ❌ | No new transactional operation; isolation is per statement and per connection. |
| 9 | Idempotency | ❌ | Script generation is deterministic and re-runnable (`IF NOT EXISTS` in MySQL, `CREATE ... IF NOT EXISTS` forms elsewhere). |
| 10 | Multi-Tenancy | ✅ | Independent of the tenant: no tenant-qualified identifier is created, so the reserved `default` segment and injectivity rules do not apply; combination with tenant isolation stays documented (tenant scope: #1257, #798). |
| 11 | Module Isolation | ✅ | This plan is module data isolation; registration is made complete on all 10 providers. |
| 12 | Audit Trail | ⏭️ | Module attribution of audit rows is #1636 (open); audit of denied cross-module access is the follow-up issue file `artifacts/issues/module-cross-module-audit.md` shared with #352 (this worktree). |

---

## Prerequisites & Dependencies

- **Pull requests 1 and 2 have none**: they depend only on code on `origin/main` (`e694eb032`).
- **End-to-end tests of pull requests 3 to 5**: the wrapped connections only validate while a module is the ambient module; the tests set the module with `IModuleExecutionContext.CreateScope` directly, so they do not wait for #352's producer move, which makes the production behavior complete for ADO.NET, Dapper and MongoDB.
- **Related, not blocking**: #703 (cache of schema extraction) and #746 (module id in cache keys) touch the same isolation namespace; #360 supplies module names and the graph the analyzer's ownership rule can later reuse.

---

## Next Steps

1. Ask the maintainer D3, D5, D6 and D9 when work on #359 starts; record the answers in `## Maintainer Decisions`.
2. Pull requests in order: 1 generators (11 files), 2 helper, writer and log ids (12), 3 ADO.NET registration and READMEs (11), 4 Dapper registration and READMEs (11), 5 EF Core and MongoDB registration (6), 6 attribute and EF schema (15), 7 analyzer (13), 8 documentation (5).
3. Open the follow-up issue files `module-isolation-violation-telemetry` and `module-cross-module-audit` when the plan is merged.
