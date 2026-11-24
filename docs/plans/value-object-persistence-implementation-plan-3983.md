# Implementation Plan: Persistence Mapping of Always-Valid Value Objects for Dapper, ADO.NET and MongoDB

> **Issue**: [#3983](https://github.com/dlrivada/Encina/issues/3983)
> **Type**: Feature
> **Complexity**: High (7 phases, 7 providers, a shared core and 12 pull requests of 6 to 11 files)
> **Estimated Scope**: ~1,400 lines of production code + ~4,200 lines of tests, documentation and manifests, in 12 pull requests
> **Family**: DDD value-object family. It consumes the factory contract `IValueObject<TSelf, TValue>`, the package `Encina.Validation.Domain` and the EF Core converter planned in #228, and shares the database matrix of every provider feature (`AGENTS.md` section 5). Siblings are referred to by issue number only.

---

## Summary

#228 plans an always-valid value object layer: a factory contract `IValueObject<TSelf, TValue>` (a static abstract `Create` returning `Either<EncinaError, TSelf>` and a `Value`), a catalog package and one EF Core converter that serves the three EF providers. The other seven providers have no mapping: a value object stored through Dapper, ADO.NET or MongoDB either fails (the driver cannot bind the object) or is stored in the wrong shape. #3983 persists a value object as its primitive and rebuilds it through `Create` on Dapper (3), ADO.NET (3) and MongoDB (1).

**What exists today** (`origin/main` at `278c164f1`, 2026-11-24):

- None of the contract exists yet on `main`: `src/Encina.DomainModeling/IValueObject.cs` is not there (it lands with #228 S1). `Encina.DomainModeling` has `ValueObject` and `SingleValueObject<TValue>` and is already referenced by all seven provider packages (checked in their csproj files), so a shared helper there adds no dependency edge.
- Dapper: the three packages carry `TypeHandlers/` (`UuidV7IdTypeHandler`, `UlidIdTypeHandler`, `SnowflakeIdTypeHandler`, `ShardPrefixedIdTypeHandler`; MySQL also `GuidTypeHandler`), one copy per package, each a `SqlMapper.TypeHandler<T>` with a static `EnsureRegistered()`; no library code calls `EnsureRegistered` for the id handlers. Each Dapper package references its own ADO package and `Dapper`, never another Dapper package.
- ADO.NET: entities are materialized and bound by hand. `FunctionalRepositoryADO.MapReaderToEntity` reads `reader.GetValue(ordinal)` and calls a private `ConvertValue(object, Type)` (it handles `Nullable<>`, enums and `Convert.ChangeType`), and `AddEntityParameters` binds `property.GetValue(entity)` as the parameter value. A value object property would reach `Convert.ChangeType` and the driver unchanged. The same pattern is repeated, by grep on `origin/main`: SqlServer 9 files (`BulkOperations/BulkOperationsADO.cs`, `Pagination/CursorPaginationHelper.cs`, `Repository/FunctionalRepositoryADO.cs`, `Repository/SpecificationSqlBuilder.cs`, `Sharding/FunctionalShardedRepositoryADO.cs`, `Sharding/ReferenceTables/ReferenceTableStoreADO.cs`, `Temporal/TemporalRepositoryADO.cs`, `Tenancy/TenantAwareFunctionalRepositoryADO.cs`, `UnitOfWork/UnitOfWorkRepositoryADO.cs`), PostgreSQL 9 (same names, `BulkOperationsPostgreSQL.cs`), MySQL 8 (no `Temporal`, `BulkOperationsMySQL.cs`).
- MongoDB: `src/Encina.MongoDB/Serializers/` holds `UuidV7IdSerializer`, `UlidIdSerializer`, `SnowflakeIdSerializer`, `ShardPrefixedIdSerializer` (each a `SerializerBase<T>` with a static `Instance`) and `IdGenerationSerializerRegistration.EnsureRegistered()` (lock plus flag, `BsonSerializer.RegisterSerializer`). The driver in `Directory.Packages.props` is 3.12.0 and `BsonSerializer.TryRegisterSerializer(Type, IBsonSerializer)` exists (checked on the 3.7.1 assembly in the local NuGet cache, the same API family).
- Tests: `Encina.UnitTests`, `Encina.GuardTests`, `Encina.PropertyTests`, `Encina.IntegrationTests` already reference the seven provider packages; `Encina.ContractTests` references the seven provider packages as well (checked in the csproj files), so no test csproj edit is needed. Integration collections: `Dapper-SqlServer`, `Dapper-PostgreSQL`, `Dapper-MySQL`, `ADO-SqlServer`, `ADO-PostgreSQL`, `ADO-MySQL`, `MongoDB`.

**Standards and references**: none (Dapper `SqlMapper.ITypeHandler`, MongoDB `IBsonSerializer<T>` and `System.Data` are the integration points). The personal-data rule applies: a value object such as an email address must never reach an exception message or a log (`AGENTS.md` section 3).

**Affected packages**: `Encina.DomainModeling` (shared core), `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`, `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`, `Encina.MongoDB`. `Encina.Validation.Domain` (named in the issue) does not change: it owns the value objects, not their persistence.

**Provider category**: Database (10). EF Core x3 is done by the #228 converter (S4 of that plan); this plan covers the other seven.

**Shared pieces and who owns them**: #228 owns `IValueObject<TSelf, TValue>`, `ValueObjectErrors` and the EF Core converter. This plan owns `ValueObjectPersistence` (shared core), the three Dapper handlers, the MongoDB serializer, the ADO call-site changes and the registration methods. This plan registers no EventId range and creates no error code.

---

## Design Choices

<details>
<summary><strong>1. Where the shared mapping lives (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One provider-neutral core `ValueObjectPersistence` in `Encina.DomainModeling` (rebuild through `Create`, the stored primitive, the supported-type check, the type-only exception) plus a thin per-provider adapter: a Dapper handler per Dapper package, a serializer in `Encina.MongoDB`, calls from the ADO call sites** | One copy of the rule that matters (rebuild, never unchecked, never the value in a message); no new dependency edge (all seven packages already reference DomainModeling); the per-package handlers are the precedent (four id handlers per Dapper package); the part that legitimately differs per provider (how the primitive binds, for example a MySQL `Guid` as `CHAR(36)`) stays local | Three copies of a ~40-line Dapper handler |
| **B) Everything per provider package, no shared core** | No change to DomainModeling | Seven copies of the rebuild rule; a fix lands seven times; drift is likely |
| **C) A new package `Encina.Persistence.ValueObjects` that references Dapper, the MongoDB driver and DomainModeling** | A single Dapper handler and a single serializer | A package that pulls the Dapper and MongoDB dependencies into an application that uses one of them; breaks pay-for-what-you-use |

### Chosen Option: **A — A shared core in DomainModeling and thin per-provider adapters** (recommended, pending the maintainer)

### Rationale

- A wins on consistency (`AGENTS.md` section 5, providers differ only in implementation) and keeps the data-library types out of DomainModeling: the shared core uses only `System` types and the contract. B wins only if the maintainer wants DomainModeling to stay free of any persistence-related code. C wins if the maintainer wants exactly one Dapper handler and accepts a new package.
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Stored data that `Create` rejects (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No unchecked path: every mapping rebuilds through `Create` and, on a `Left`, throws an `InvalidOperationException` that names the value object type and the error code and never the stored value** | The invariant of #228 holds at the infrastructure edge; one rule in one place (`ValueObjectPersistence`); corrupt data fails loudly | A row written by hand, or before a rule was tightened, cannot be loaded |
| **B) The answer the maintainer gives to #228 Design Choice 4 option B (a public bypass such as `CreateUnsafe`) is applied to the mappings too** | Loads legacy rows | Defeats "always valid"; only possible if #228 adds the bypass |

### Chosen Option: **A — No unchecked path** (recommended, pending the maintainer)

### Rationale

- The issue states this as settled, citing the #228 invariant, but #228's own Design Choice 4 is still a pending Dilemma, so this plan cannot treat it as settled: the answer must be the same for the EF converter and for these seven mappings. A wins unless #228 answers B; if it does, only `ValueObjectPersistence.FromStoredValue` changes (it is the one place that rebuilds).
- The exception carries the error code (`validation.domain.invalid_format` and siblings, from #228) because a code is allowed in messages and logs; it never carries the stored value or `EncinaError.Message`. These are exceptions, not `EncinaError` values: a column read has no `Either` channel in Dapper, ADO.NET or the driver, so no new error code and no ADR-062 `ErrorCategory` is created here.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Dapper wraps handler failures and prints the column value (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Accept Dapper's wrapper: the handler throws the type-only exception; Dapper wraps it in a `DataException` whose message names the column and the raw column text; the README and the tests say that the exception is logged through `ForLogging()` (type and stack, never the message) and that the inner exception is the one with the type-only text** | No code outside the handler; the logging rule of `AGENTS.md` section 3 already requires `ForLogging()` for every exception | The wrapper's `Message` still contains the stored value, so a caller that returns `ex.Message` in a response leaks it |
| **B) A sanitizing layer in Encina's own Dapper repository: catch the `DataException` raised while materializing and rethrow a type-only exception** | The repository never surfaces the value, whatever the caller does | Covers only the repository path, not an application's own `QueryAsync<T>`; a catch-and-rethrow in every repository method |

### Chosen Option: **A — Accept the wrapper and document it** (recommended, pending the maintainer)

### Rationale

- The behavior of `SqlMapper.ThrowDataException` is stated from the Dapper source (error text "Error parsing column N (name=value)"); the first integration test of the Dapper pull request pins it against the Dapper version in `Directory.Packages.props` (2.1.72), and this Dilemma changes if the test shows otherwise. A wins on simplicity and because the handler is also used by application queries that Encina does not own; B wins when the maintainer needs a hard guarantee for the repository path (health data, for example) and accepts a try/catch in each repository method, recorded as a follow-up inside this plan (Prerequisites & Dependencies, "Deferred").
- ADO.NET and MongoDB do not wrap the exception in a value-printing wrapper in the paths used here (the ADO call sites throw the helper's exception directly; the Mongo driver wraps a serializer failure in a `FormatException` or `BsonSerializationException` whose message is the serializer's own); the integration tests of those pull requests assert the message has no stored text.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. How many ADO.NET call sites change (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every entity materialization and binding site of each ADO package: the repository, the specification builder, the tenant-aware, sharded and unit-of-work repositories (the "repository family"), then bulk operations, reference tables, temporal and cursor pagination. Six pull requests (two per package)** | A value object works wherever the same entity works; no repository that silently fails | The largest option: 8 or 9 source files per package |
| **B) Only `FunctionalRepositoryADO` and `SpecificationSqlBuilder` now (the two files an entity round trip needs); the other sites in a later pull request** | Three small pull requests | The same entity fails in the tenant-aware or unit-of-work repository; contradicts the acceptance "round-trips through each of the 7 providers" for those paths |

### Chosen Option: **A — Every call site, two pull requests per package** (recommended, pending the maintainer)

### Rationale

- A wins on provider coherence (`AGENTS.md` section 5): the same entity must not work in one repository of a package and fail in another. B wins if the maintainer wants the first value objects in production quickly and accepts a documented limitation. The site list is the grep of `origin/main` at `278c164f1`; each pull request re-runs the grep and lists the exact files before the first edit.
- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Explicit registration by type (Trivial)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The application lists its value object types once: `services.AddEncinaValueObjectDapperHandlers(typeof(EmailAddress))` per Dapper package, `AddEncinaValueObjectSerializers(...)` for MongoDB; ADO.NET needs no registration because the call sites recognize `IValueObject<,>` by type** | Explicit; no assembly scan; the same idiom as the EF Core convention of #228 and the id converters | The application lists its types |
| **B) Scan assemblies for `IValueObject<,>` implementers** | Zero listing | Reflection over assemblies at startup; hides which types are mapped |

### Chosen Option: **A — Explicit registration, mirroring the EF Core convention**

### Rationale

Each method is idempotent (Dapper `SqlMapper.AddTypeHandler(type, handler)` replaces the handler for a type; the MongoDB side uses `TryRegisterSerializer`), validates every type (non-null, a class that implements `IValueObject<,>`, a supported primitive) and fails fast with an `ArgumentException` that names the type.

- **Class:** Trivial
- **Settled by:** `src/Encina.EntityFrameworkCore/Converters/IdGenerationModelConfigurationExtensions.cs:30-45` (explicit per-type conventions) and `src/Encina.MongoDB/Serializers/IdGenerationSerializerRegistration.cs` (explicit serializer registration), as chosen for the EF side in #228 Design Choice 5.

</details>

<details>
<summary><strong>6. The supported primitive set is an allow-list (Trivial)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TValue` must be one of `string`, `Guid`, `int`, `long`, `decimal`, `bool`, rejected otherwise at registration (and by the ADO helper on first use)** | A shape every driver binds natively; a wrong shape cannot be stored silently | A value object over another primitive needs the set extended |
| **B) Any `TValue`, converted with `Convert.ChangeType` or JSON** | No limit | The "wrong shape elsewhere" that the issue's Motivation names |

### Chosen Option: **A — An allow-list in `ValueObjectPersistence`**

### Rationale

The set covers the value objects planned in #228 (`EmailAddress` and `PhoneNumber` over `string`; a strongly typed id over `Guid`). The list lives in one place, so adding a primitive is one change plus its tests per provider.

- **Class:** Trivial
- **Settled by:** issue #3983 (Motivation: "fails or stores a wrong shape elsewhere") and the explicit-registration precedent of Design Choice 5.

</details>

<details>
<summary><strong>7. The stored value never reaches a message or a log (Settled)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every exception thrown by this plan's code names the value object type and, when known, the error code, and never the stored value; no logger is added** | Value objects hold personal data (email, phone) | The message is less detailed for the operator |
| **B) Include the stored value to ease debugging** | Easier debugging | Personal data in exceptions, logs and plaintext storage |

### Chosen Option: **A — Type and code only**

### Rationale

Option B violates the rule; only A complies. Design Choice 3 records the one place (the Dapper wrapper) that this plan cannot control.

- **Class:** Settled
- **Settled by:** `AGENTS.md` section 3 (`EncinaError.Message` never reaches logs, activity tags, health checks or plaintext storage; an exception passed to a logger goes through `ForLogging()`) and #228 register row C2 (messages and details never contain the rejected value).

</details>

<details>
<summary><strong>8. MongoDB reads and writes the primitive through the driver's own serializer (Trivial)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ValueObjectSerializer<TValueObject, TValue> : SerializerBase<TValueObject>` delegates to `BsonSerializer.LookupSerializer<TValue>()` for the primitive and rebuilds through `ValueObjectPersistence`** | The application's Guid representation and conventions apply to the primitive; no hand-written BSON per type | One lookup per call (cached in the instance) |
| **B) Write each primitive by hand with `context.Writer.WriteString(...)`, as the id serializers do** | No lookup | A switch over every `TValue`; ignores the configured Guid representation |

### Chosen Option: **A — Delegate to the primitive's serializer**

### Rationale

The id serializers write a fixed string form for a type they own; a generic serializer for arbitrary primitives must respect the driver's configuration.

- **Class:** Trivial
- **Settled by:** `src/Encina.MongoDB/Serializers/UuidV7IdSerializer.cs` (the `SerializerBase<T>` shape and the static `Instance`) and the MongoDB driver's serializer lookup API (`BsonSerializer.LookupSerializer`).

</details>

---

## Implementation Phases

### Phase 1: Core Abstractions — The Shared Mapping (pull request 1)

> **Goal**: The one place that rebuilds a value object from a stored primitive.

<details>
<summary>Tasks</summary>

1. **Prerequisite**: `IValueObject<TSelf, TValue>` (#228 S1) is on `main`. The first task checks it and stops otherwise.
2. **Create** `src/Encina.DomainModeling/ValueObjectPersistence.cs` (`public static class`, namespace `Encina.DomainModeling`): `bool IsValueObject(Type type)` (cached: a closed implementer of `IValueObject<,>`), `Type StoredType(Type valueObjectType)`, `object ToStoredValue(object valueObject)`, `object FromStoredValue(Type valueObjectType, object storedValue)` (the non-generic entry for the ADO call sites, one cached delegate per type built from `MethodInfo.MakeGenericMethod`), `TValueObject FromStoredValue<TValueObject, TValue>(TValue storedValue)` (the generic entry for the Dapper handler and the MongoDB serializer: calls `TValueObject.Create`), and `void EnsureSupported(Type valueObjectType)` (the allow-list of Design Choice 6, `ArgumentException` naming the type). On a `Left`, `FromStoredValue` throws `InvalidOperationException` with a message of the form `Stored data of <Type> is not a valid value (<code>)` built from `error.GetCode()` and never from `error.Message`.
3. **Modify** `PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.DomainModeling.json` (through `mechanical-fixer`).
4. Public symbols added: one static class and six public methods; no constructor, so there is no `new X(` site, no optional parameter and no generic constraint conflict (`TValueObject : IValueObject<TValueObject, TValue>` is the only constraint, with `TValue : notnull` as in #228).
5. Tests: unit (`tests/Encina.UnitTests/DomainModeling/ValueObjectPersistenceTests.cs`: rebuild, `Left` throws with the code and without the value, unsupported `TValue`, non-value-object type), guard (`tests/Encina.GuardTests/DomainModeling/ValueObjectPersistenceGuardTests.cs`: null `Type`, null value), property (`tests/Encina.PropertyTests/DomainModeling/ValueObjectPersistenceProperties.cs`: a value accepted by `Create` survives `ToStoredValue` then `FromStoredValue`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>).
- Issue #3983, plan docs/plans/value-object-persistence-implementation-plan-3983.md. Dilemmas D1 to D4 are recommended, pending the maintainer: read the Maintainer Decisions section if it exists and follow the answers.
- IValueObject<TSelf, TValue> (static abstract Create returning Either<EncinaError, TSelf>, and Value) comes from #228 S1 and must already be on main.

TASK:
Create ValueObjectPersistence in src/Encina.DomainModeling with the methods of the Phase 1 Tasks and its unit, guard and property tests.

KEY RULES:
- The exception names the type and the error code and never the stored value or EncinaError.Message.
- Cache reflection per type (ConcurrentDictionary); no reflection per call after the first.
- The supported primitives are string, Guid, int, long, decimal, bool.
- Tests execute real code; no reflection-only asserts.

REFERENCE FILES:
- src/Encina.DomainModeling/ValueObject.cs
- src/Encina/Results/EncinaErrors.cs (GetCode)
- docs/plans/domain-value-object-validation-implementation-plan-228.md (contract and error codes)
```

</details>

---

### Phase 2: Provider Implementations — Dapper (pull requests 2 to 4)

> **Goal**: One handler per Dapper package, registered by type.

<details>
<summary>Tasks</summary>

1. **Create** `src/Encina.Dapper.<P>/TypeHandlers/ValueObjectTypeHandler.cs` (`public sealed class ValueObjectTypeHandler<TValueObject, TValue> : SqlMapper.TypeHandler<TValueObject> where TValueObject : IValueObject<TValueObject, TValue> where TValue : notnull`): `Parse(object value)` converts the column to `TValue` (a `Guid` column read as `string` on MySQL is parsed, as `GuidTypeHandler` does) and calls `ValueObjectPersistence.FromStoredValue<TValueObject, TValue>`; `SetValue(IDbDataParameter, TValueObject)` sets `parameter.Value = value.Value` and the `DbType` of the primitive (MySQL: `Guid` as a `CHAR(36)` string, the only provider-specific line).
2. **Create** `src/Encina.Dapper.<P>/ValueObjectServiceCollectionExtensions.cs` (namespace `Encina.Dapper.<P>`, like `AddEncinaDapper`): `IServiceCollection AddEncinaValueObjectDapperHandlers(this IServiceCollection services, params Type[] valueObjectTypes)`: null checks, `ValueObjectPersistence.EnsureSupported` per type, `SqlMapper.AddTypeHandler(type, handler)` (idempotent), returns `services`. The method adds no DI service, so no registration-completeness proof beyond the unit test that the handlers are installed applies.
3. **Modify** `PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.Dapper.<P>.json` and the package README (the README through `docs-writer`; the manifest and PublicAPI through `mechanical-fixer`).
4. Public symbols per package: the handler class (constructor: the implicit public parameterless one, so no `new X(` site and no guard test), `Parse`, `SetValue` and the extension method; the guard tests cover the extension method's arguments (null `services`, null `valueObjectTypes`, a null element, a type that is not a value object, an unsupported `TValue`).
5. Tests per pull request: unit (`tests/Encina.UnitTests/Dapper/<P>/ValueObjectTypeHandlerTests.cs`: parse, set, invalid stored data throws the type-only exception), guard, property (a value accepted by `Create` survives `SetValue` then `Parse` over a real provider parameter object), integration (`tests/Encina.IntegrationTests/Infrastructure/Dapper/<P>/ValueObjects/ValueObjectDapperIntegrationTests.cs`, `[Collection("Dapper-<P>")]`, `ClearAllDataAsync` in `InitializeAsync`: insert and query by value object, round trip each supported primitive, a row the rules reject: the inner exception is the type-only one and the test pins the `DataException` wrapper of Design Choice 3). Pull request 2 also adds the shared test value objects `tests/Encina.IntegrationTests/Infrastructure/ValueObjects/ValueObjectTestTypes.cs` (a `string`, an `int` and a `Guid` value object implemented in the test project, so the tests do not depend on `Encina.Validation.Domain`), reused by the later pull requests.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Issue #3983, Phase 2. ValueObjectPersistence exists in Encina.DomainModeling (Phase 1) and IValueObject comes from #228.
- Dapper handlers are SqlMapper.TypeHandler<T>; the id handlers in src/Encina.Dapper.<P>/TypeHandlers/ are the precedent.

TASK:
Add ValueObjectTypeHandler and AddEncinaValueObjectDapperHandlers to ONE Dapper package (the pull request names which), with the tests of the Phase 2 Tasks.

KEY RULES:
- The handler throws only through ValueObjectPersistence; no message holds the stored value.
- Integration tests use the shared [Collection("Dapper-<P>")] fixture; never IClassFixture; never dispose the fixture.
- Registration is idempotent; each type is validated with ArgumentException naming the type.
- Every new public method has its guard tests; targets from the manifest.

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/TypeHandlers/UuidV7IdTypeHandler.cs
- src/Encina.Dapper.MySQL/TypeHandlers/GuidTypeHandler.cs
- src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs
- tests/Encina.IntegrationTests/Infrastructure/Dapper/SqlServer/BulkOperations/BulkOperationsDapperIntegrationTests.cs
```

</details>

---

### Phase 3: Provider Implementations — ADO.NET (pull requests 6 to 11)

> **Goal**: Every ADO call site that materializes or binds an entity understands a value object (Design Choice 4, option A).

<details>
<summary>Tasks</summary>

1. **Repository family, per package (pull requests 6, 8, 10)**: in `Repository/FunctionalRepositoryADO.cs`, `Tenancy/TenantAwareFunctionalRepositoryADO.cs`, `UnitOfWork/UnitOfWorkRepositoryADO.cs` and `Sharding/FunctionalShardedRepositoryADO.cs`, `ConvertValue(object value, Type targetType)` first asks `ValueObjectPersistence.IsValueObject(underlyingType)` and, when true, returns `ValueObjectPersistence.FromStoredValue(underlyingType, ConvertValue(value, ValueObjectPersistence.StoredType(underlyingType)))`; entity binding (`AddEntityParameters`) calls `ValueObjectPersistence.ToStoredValue` for a value object property before `AddParameter`; in `Repository/SpecificationSqlBuilder.cs` the parameter list passed to `AddParametersToCommand` converts a value object constant the same way. No public API changes (the call sites are private), so no PublicAPI line.
2. **Other sites, per package (pull requests 7, 9, 11)**: `BulkOperations/BulkOperationsADO.cs` (PostgreSQL `BulkOperationsPostgreSQL.cs`, MySQL `BulkOperationsMySQL.cs`; the PostgreSQL copy path binds the stored primitive with its `NpgsqlDbType`), `Sharding/ReferenceTables/ReferenceTableStoreADO.cs`, `Temporal/TemporalRepositoryADO.cs` (SqlServer and PostgreSQL only) and `Pagination/CursorPaginationHelper.cs`. Each pull request re-runs `git grep -l -E "ConvertValue\(|\.GetValue\(entity\)|AddParametersToCommand"` on `origin/main` and edits exactly the files it returns, so a file added after this plan is not missed.
3. The shared `ConvertValue` is private in each file (13 copies in the three packages today): this plan edits each copy and does not extract a shared one (a refactor with no behavior change, outside the issue's scope, recorded in Prerequisites & Dependencies, "Deferred").
4. Tests per pull request: unit (`tests/Encina.UnitTests/ADO/<P>/...` using an in-memory `DbDataReader` and `DbCommand` substitute to drive the changed `ConvertValue` and binding code through the repository), integration (`tests/Encina.IntegrationTests/Infrastructure/ADO/<P>/ValueObjects/ValueObjectADOIntegrationTests.cs`, `[Collection("ADO-<P>")]`: insert, query by value object through the specification builder, load, a row the rules reject fails with the type-only exception; the tenant-aware and unit-of-work repositories in the family pull request, bulk insert and reference tables in the second). The manifests record per-file targets for every touched file.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Issue #3983, Phase 3. ValueObjectPersistence exists in Encina.DomainModeling (Phase 1). Dilemma D4 is recommended, pending the maintainer: read the Maintainer Decisions section first.
- ADO repositories materialize with ConvertValue(object, Type) and bind with property.GetValue(entity); see src/Encina.ADO.SqlServer/Repository/FunctionalRepositoryADO.cs (~lines 862-960).

TASK:
In ONE ADO package (the pull request names which and whether it is the repository family or the other sites), make every listed call site convert a value object to its stored primitive on write and rebuild it through ValueObjectPersistence.FromStoredValue on read.

KEY RULES:
- Re-run the grep named in the Phase 3 Tasks and edit exactly the files it returns.
- Database calls stay asynchronous with a CancellationToken; do not introduce a synchronous overload.
- No new public API; no exception message holds a stored value.
- Integration tests use the shared [Collection("ADO-<P>")] fixture and ClearAllDataAsync in InitializeAsync.
- Targets in the manifest never go down.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Repository/FunctionalRepositoryADO.cs
- src/Encina.ADO.SqlServer/Repository/SpecificationSqlBuilder.cs
- src/Encina.ADO.SqlServer/BulkOperations/BulkOperationsADO.cs
```

</details>

---

### Phase 4: Provider Implementations — MongoDB (pull request 5)

> **Goal**: A generic serializer registered by type.

<details>
<summary>Tasks</summary>

1. **Create** `src/Encina.MongoDB/Serializers/ValueObjectSerializer.cs` (`public sealed class ValueObjectSerializer<TValueObject, TValue> : SerializerBase<TValueObject> where TValueObject : IValueObject<TValueObject, TValue> where TValue : notnull`; the constructor takes no argument and resolves `BsonSerializer.LookupSerializer<TValue>()` once): `Serialize` writes `value.Value` through the primitive's serializer; `Deserialize` reads the primitive and calls `ValueObjectPersistence.FromStoredValue<TValueObject, TValue>`; a failure carries the type-only exception (the driver may wrap it; the integration test pins the wrapper's message).
2. **Create** `src/Encina.MongoDB/Serializers/ValueObjectServiceCollectionExtensions.cs`: `IServiceCollection AddEncinaValueObjectSerializers(this IServiceCollection services, params Type[] valueObjectTypes)`: null checks, `EnsureSupported` per type, `BsonSerializer.TryRegisterSerializer(type, serializer)` (idempotent: a second registration of the same type is a no-op), returns `services`.
3. **Modify** `PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.MongoDB.json` and `src/Encina.MongoDB/README.md`.
4. Tests: unit (`tests/Encina.UnitTests/MongoDB/ValueObjectSerializerTests.cs`: `BsonDocument` round trip, invalid stored data), guard, property (round trip over generated values), integration (`tests/Encina.IntegrationTests/Infrastructure/MongoDB/ValueObjects/ValueObjectSerializerIntegrationTests.cs`, `[Collection("MongoDB")]`, in the style of `IdGeneration/IdGenerationSerializerTests.cs`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
- Issue #3983, Phase 4. ValueObjectPersistence exists in Encina.DomainModeling (Phase 1).
- Serializer precedent: src/Encina.MongoDB/Serializers/UuidV7IdSerializer.cs and IdGenerationSerializerRegistration.cs.

TASK:
Add ValueObjectSerializer<TValueObject, TValue> and AddEncinaValueObjectSerializers to Encina.MongoDB with the tests of the Phase 4 Tasks.

KEY RULES:
- Delegate the primitive to the driver's serializer; rebuild only through ValueObjectPersistence.
- Registration is idempotent (TryRegisterSerializer) and validates every type with ArgumentException naming the type.
- Integration tests use [Collection("MongoDB")]; no message holds a stored value.

REFERENCE FILES:
- src/Encina.MongoDB/Serializers/UuidV7IdSerializer.cs
- src/Encina.MongoDB/Serializers/IdGenerationSerializerRegistration.cs
- tests/Encina.IntegrationTests/Infrastructure/MongoDB/IdGeneration/IdGenerationSerializerTests.cs
```

</details>

---

### Phase 5: Cross-Cutting Integration and Observability

> **Goal**: Record what applies and what does not.

<details>
<summary>Tasks</summary>

1. No `ActivitySource`, `Meter`, health check or log message: the failure counter of a value object lives in the value objects (#228), and a column conversion has no tenant context, so the ADR-054 D4 tenant tag does not apply and none is added. No EventId range is registered and no `Log.cs` is written.
2. No error code: failures are exceptions at the infrastructure edge (Design Choice 2), so there is no ADR-062 `ErrorCategory` to assign.
3. Multi-tenancy: the tenant-aware ADO repositories gain value object support through the same helper; no tenant id or tenant-qualified key is built here, so the reserved `default` segment and injective ids do not apply.
4. Nothing registers a service, an options type or a hosted service, so the registration-completeness rule of `AGENTS.md` section 3 has no DI proof to add; the registration methods are tested directly.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
- Issue #3983, Phase 5. The feature is a persistence mapping.

TASK:
Confirm that no telemetry, logging, EventId range or DI service is added, and that no exception message or log line holds a stored value.

KEY RULES:
- Do not register an EventId range; do not add a tenant tag.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs (read only)
```

</details>

---

### Phase 6: Testing

> **Goal**: Every flag at its target on the seven providers.

<details>
<summary>Tasks</summary>

| Flag | Scope | Per-file target (justification) |
|------|-------|---------------------------------|
| Unit | the shared core, three Dapper handlers, the serializer, the ADO call sites | `ValueObjectPersistence.cs` 95 (every branch reachable with in-memory value objects: rebuild, `Left`, unsupported primitive, non-value-object); `ValueObjectTypeHandler.cs` (x3) 90 and `ValueObjectSerializer.cs` 90 (parse, set, invalid data; the driver-specific wrapping is integration); the touched ADO files keep their baseline measured with the command below and never lower it, and add the new branches (each pull request records the number) |
| Guard | the six public methods of the core, the two registration methods | `ValueObjectPersistence.cs` 90, `ValueObjectServiceCollectionExtensions.cs` (Dapper x3 and MongoDB) 90 (the argument checks are the only branches the guard flag reaches) |
| Contract | the same verdict on invalid stored data across the seven providers (pull request 12, `tests/Encina.ContractTests/Persistence/ValueObjectPersistenceContractTests.cs`: the shared core, the three Dapper handlers and the MongoDB serializer, plus the ADO helper entry point; all are public) | `ValueObjectPersistence.cs` 60 (the contract flag reaches the rebuild and the exception only) |
| Property | any value accepted by `Create` survives the round trip | `ValueObjectPersistence.cs` 80, `ValueObjectTypeHandler.cs` (x3) 70 and `ValueObjectSerializer.cs` 70 (one invariant per class over generated values) |
| Integration | real databases in the shared collections, seven providers (never a `.md`) | the touched ADO, Dapper and MongoDB files record their integration target in the manifest (an ADO repository file 60, a handler 80), measured by the pull request |

Load and benchmark: a justification `.md` each (`tests/Encina.LoadTests/...` and `tests/Encina.BenchmarkTests/...` per `AGENTS.md` section 9: per-column conversion, no concurrency of its own, no hot path of its own).

Measure from the repository root, per flag: `dotnet test tests\Encina.UnitTests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\UnitTests --filter "FullyQualifiedName~ValueObject"` and the same for `GuardTests`, `ContractTests`, `PropertyTests`, `IntegrationTests`; then `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report` and `dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications`. CRAP stays at most 10: `ValueObjectPersistence` methods are small and the ADO edits add one branch to `ConvertValue`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```
CONTEXT:
- Issue #3983, Phase 6. Shouldly through Encina.Testing.Shouldly; the shared integration fixtures per AGENTS.md section 9.

TASK:
Write the tests of the Phase 6 table for the pull request you are on and fill the manifest targets with one-sentence justifications.

KEY RULES:
- Tests execute real code; no reflection-only asserts; no Thread.Sleep.
- Integration tests: [Collection("<Family>-<Database>")], ClearAllDataAsync in InitializeAsync, _fixture.CreateConnection(); never new a fixture.
- Justification .md files for load and benchmark only; never for unit, guard, contract or database integration.

REFERENCE FILES:
- .github/coverage-manifest/Encina.DomainModeling.json
- tests/Encina.IntegrationTests/Infrastructure/MongoDB/IdGeneration/IdGenerationSerializerTests.cs
```

</details>

---

### Phase 7: Documentation & Finalization

> **Goal**: The mandatory documentation set, spread over the pull requests.

<details>
<summary>Tasks</summary>

1. XML docs on every new public API (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>` with the registration call).
2. One changelog fragment per pull request, `changelog.d/3983-value-object-persistence-<slug>.added.md` (for example `-core`, `-dapper-sqlserver`, `-mongodb`, `-ado-postgresql-repositories`); never `CHANGELOG.md`.
3. Package READMEs of the seven provider packages (a short "Value objects" section with the registration line and the supported primitives), written through `docs-writer`.
4. A guide page under `docs/` (pull request 12, through `docs-writer`): how to persist a value object on each of the ten providers (EF Core from #228, the seven from this plan), the exception behavior of Design Choices 2 and 3 and the logging rule; no hand-typed coverage figures.
5. `PublicAPI.Unshipped.txt` in pull requests 1 to 5. No ADR (no new architectural decision beyond the recorded Dilemmas), no `docs/INVENTORY.md` change (no new package or module), no ROADMAP change.
6. Verification per pull request: `dotnet build --configuration Release` with 0 warnings, the tests of the pull request and the per-flag targets.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```
CONTEXT:
- Issue #3983, Phase 7.

TASK:
Finish the XML docs, the changelog fragment, the README section and the guide page of the pull request you are on, and run the verification commands.

KEY RULES:
- Never edit CHANGELOG.md; fragments live in changelog.d/.
- Documentation pages go through the docs-writer agent; never type coverage figures by hand.

REFERENCE FILES:
- changelog.d/README.md
- src/Encina.Dapper.SqlServer/README.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

Twelve pull requests, in this order (each builds and passes alone; the table is the local-model draft of the counts, summed and reviewed against the file lists above; every pull request needs `IValueObject<,>` from #228 S1 on `main` first):

<!-- local-draft: D:\Proyectos\Encina\.claude\worktrees\pb-e\artifacts\local-ai\out\p3983-prtable.md -->

| PR | Scope | Source files | Test files | Manifest, PublicAPI, README, changelog | Total |
|---|---|---|---|---|---|
| PR1 | Shared mapping in `Encina.DomainModeling` | 1 | 3 | 3 | 7 |
| PR2 | Dapper SqlServer handler and shared integration test types | 2 | 5 | 4 | 11 |
| PR3 | Dapper PostgreSQL | 2 | 4 | 4 | 10 |
| PR4 | Dapper MySQL | 2 | 4 | 4 | 10 |
| PR5 | MongoDB serializer | 2 | 4 | 4 | 10 |
| PR6 | ADO SqlServer, repository family | 5 | 2 | 3 | 10 |
| PR7 | ADO SqlServer, bulk, reference tables, temporal, cursor pagination | 4 | 1 | 2 | 7 |
| PR8 | ADO PostgreSQL, repository family | 5 | 2 | 3 | 10 |
| PR9 | ADO PostgreSQL, bulk, reference tables, temporal, cursor pagination | 4 | 1 | 2 | 7 |
| PR10 | ADO MySQL, repository family | 5 | 2 | 3 | 10 |
| PR11 | ADO MySQL, bulk, reference tables, cursor pagination | 3 | 1 | 2 | 6 |
| PR12 | Cross-provider contract test and the guide page | 0 | 1 | 1 | 2 |
| All | | 35 | 30 | 35 | 100 |

Notes on the counts: no csproj and no ProjectReference edit is needed (the test projects and the seven provider packages already reference `Encina.DomainModeling`, the providers and each other as listed in the Summary); a Dapper pull request has no `new X(` site (public parameterless constructor); each ADO source count is a candidate list from the grep and the pull request fixes the exact list before the first edit. Pull requests 6 to 11 each stay below about 600 changed lines; pull request 1 about 650.

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| Dapper `SqlMapper.ITypeHandler` / `TypeHandler<T>` | The Dapper mapping contract, and the `DataException` wrapper of Design Choice 3 |
| MongoDB driver `IBsonSerializer<T>`, `SerializerBase<T>`, `BsonSerializer.TryRegisterSerializer` | The MongoDB mapping and its idempotent registration |
| ADO.NET `IDataRecord`, `IDbDataParameter` (`System.Data`) | The reader and parameter types of the ADO call sites |
| `AGENTS.md` section 3 (personal data and `ForLogging()`), section 5 (10 providers) | The two rules that shape the exception and the scope |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ValueObject`, `SingleValueObject<TValue>` | `src/Encina.DomainModeling/ValueObject.cs` | The base the value objects of #228 derive from |
| `IValueObject<TSelf, TValue>`, `ValueObjectErrors` | #228 S1 (not yet on `main`) | The contract this plan builds on and the error codes in the exception |
| `UuidV7IdTypeHandler`, `GuidTypeHandler` | `src/Encina.Dapper.SqlServer/TypeHandlers/`, `src/Encina.Dapper.MySQL/TypeHandlers/` | The handler shape, `EnsureRegistered` and the MySQL Guid binding |
| `UuidV7IdSerializer`, `IdGenerationSerializerRegistration` | `src/Encina.MongoDB/Serializers/` | The serializer shape and the registration idiom |
| `FunctionalRepositoryADO`, `SpecificationSqlBuilder` and siblings | `src/Encina.ADO.<P>/Repository/` and the paths in the Summary | The call sites that change |
| `EncinaErrors.GetCode` | `src/Encina/Results/EncinaErrors.cs:195` | The error code carried in the exception |
| `IdGenerationSerializerTests` | `tests/Encina.IntegrationTests/Infrastructure/MongoDB/IdGeneration/` | The MongoDB integration test style |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| all eight packages | none | A mapping has no logger; no `EventIdRanges.cs` field, no registry row, no `PublicAPI` field line |

### Provider Matrix

| Provider | Value object mapping | Notes |
|----------|:-:|-------|
| ADO-SqlServer | Phase 3 (PR6, PR7) | Call-site conversion, no registration |
| ADO-PostgreSQL | Phase 3 (PR8, PR9) | The COPY path binds the stored primitive |
| ADO-MySQL | Phase 3 (PR10, PR11) | `Guid` as `CHAR(36)` through the driver |
| Dapper-SqlServer | Phase 2 (PR2) | `TypeHandler<T>` |
| Dapper-PostgreSQL | Phase 2 (PR3) | `TypeHandler<T>` |
| Dapper-MySQL | Phase 2 (PR4) | `TypeHandler<T>`, `Guid` as a string |
| EFCore-SqlServer / PostgreSQL / MySQL | #228 S4 | One converter for the three |
| MongoDB | Phase 4 (PR5) | `IBsonSerializer<T>` |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production code | 35 | 1 core, 6 Dapper (handler and registration per package), 2 MongoDB, 26 ADO call-site edits (candidates) |
| Test files | 30 | unit, guard, property, integration, shared test types, one contract file |
| Manifest, PublicAPI, README, changelog | 35 | per pull request, as the table above |
| Total | 100 | across 12 pull requests of 2 to 11 files |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Railway Oriented Programming (Either<EncinaError, T>); no [Obsolete]; XML docs on every public API; zero warnings; database calls asynchronous with a CancellationToken.
- Issue #3983: persist IValueObject<TSelf, TValue> value objects (#228) as their primitive and rebuild them through Create on Dapper (3), ADO.NET (3) and MongoDB (1). Plan: docs/plans/value-object-persistence-implementation-plan-3983.md. Dilemmas D1 to D4 are recommended, pending the maintainer; read the Maintainer Decisions section first if it exists.

IMPLEMENTATION OVERVIEW:
1. Phase 1: ValueObjectPersistence in Encina.DomainModeling (the one place that rebuilds; type-only exception).
2. Phase 2: a Dapper TypeHandler and AddEncinaValueObjectDapperHandlers per Dapper package.
3. Phase 3: ConvertValue and binding call sites of each ADO package.
4. Phase 4: ValueObjectSerializer and AddEncinaValueObjectSerializers in Encina.MongoDB.
5. Phases 5 to 7: no telemetry or logging; tests per flag on the seven providers; docs and fragments per pull request.

KEY PATTERNS:
- Never an unchecked path; never the stored value in a message; the exception carries the error code only.
- Explicit registration by type, idempotent, validated with ArgumentException naming the type.
- Shared [Collection] integration fixtures; ClearAllDataAsync in InitializeAsync; never IClassFixture.
- Per-flag targets from the manifests; CRAP at most 10 on changed methods.

REFERENCE FILES:
- src/Encina.DomainModeling/ValueObject.cs
- src/Encina.Dapper.SqlServer/TypeHandlers/UuidV7IdTypeHandler.cs
- src/Encina.MongoDB/Serializers/UuidV7IdSerializer.cs
- src/Encina.ADO.SqlServer/Repository/FunctionalRepositoryADO.cs
- docs/plans/domain-value-object-validation-implementation-plan-228.md
- docs/plans/dsr-implementation-plan-404.md (style)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: a column conversion; the only cache is the per-type reflection cache inside `ValueObjectPersistence` |
| 2 | OpenTelemetry | ❌ | Not applicable: the creation-failure counter lives in the value objects (#228); a mapping has no tenant context, so no ADR-054 D4 tag is added |
| 3 | Structured Logging | ❌ | Not applicable: a mapping has no logger and the exception names the type and code only; no EventId range |
| 4 | Health Checks | ❌ | Not applicable: no dependency to check |
| 5 | Validation | ✅ | Phases 1 to 4: every read validates through `Create`; every registration validates its types |
| 6 | Resilience | ❌ | Not applicable: in-process conversion |
| 7 | Distributed Locks | ❌ | Not applicable: no shared state |
| 8 | Transactions | ❌ | Not applicable: mapping only; no `SaveChangesAsync` and no orchestrator is involved |
| 9 | Idempotency | ❌ | Not applicable: a pure conversion; registration is idempotent by design |
| 10 | Multi-Tenancy | ❌ | Not applicable: no tenant data or id is built; the tenant-aware ADO repositories get support through the shared call sites |
| 11 | Module Isolation | ❌ | Not applicable: not module specific |
| 12 | Audit Trail | ❌ | Not applicable: no state change of its own |

---

## Prerequisites & Dependencies

- **Blocking**: #228 S1 (`IValueObject<TSelf, TValue>`, `ValueObjectErrors`) must be on `main` before pull request 1. #228 is open and its plan is on `main`.
- Sibling plans on `main` for the same area: `domain-value-object-validation-implementation-plan-228.md` (the contract and the EF Core converter), `marten-aggregate-cache-implementation-plan-697.md` and `domain-events-outbox-implementation-plan-1236.md` (other DDD plans; no overlap).
- Deferred inside this plan (not new issues): (a) if Design Choice 3 is answered B, a type-only `try/catch` around materialization in the Dapper repositories; (b) extracting the 13 private copies of the ADO `ConvertValue` into one shared helper (a refactor with no behavior change); (c) a Dapper `ITypeHandler` selection by interface so that registration needs no type list (the explicit list of Design Choice 5 is the decision for this release).

---

## Next Steps

1. The maintainer answers Dilemmas D1 to D4 when work on #3983 starts; the answers go into `## Maintainer Decisions` at the end of this plan.
2. Wait for #228 S1, then brief pull request 1; pull requests 2 to 4 (Dapper) and 5 (MongoDB) can follow in any order; the ADO pull requests 6 to 11 follow, repository family before the second pull request of each package.
3. After pull request 12, link the guide page from #3983 and from the #228 documentation.
