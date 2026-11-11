# Implementation Plan: Persistent `ICdcPositionStore` and `IShardedCdcPositionStore` on the 10 Database Providers

> **Issue**: [#2393](https://github.com/dlrivada/Encina/issues/2393) (`area-cdc`, `area-database`, `p1-recommended`)
> **Type**: Feature
> **Complexity**: Medium-High (one contract addition in `Encina.Cdc`, five connector registrations, eight provider packages)
> **Estimated Scope**: ~1,300-1,700 lines of production code + ~2,200-2,800 lines of tests, delivered in five pull requests
> **Scope decision**: maintainer comment on #2393, 2026-10-17 ("settled by elimination"): persistent stores for the 10 database providers, a table or collection per provider, optimistic concurrency, an override of the in-memory default in any registration order, and a rejection of the in-memory store outside Development by #2382.
> **Related plans of the same family (#633, #634, #635, #636)**: this plan owns the CDC position contract (Phase 1) and the `RemoveInMemoryDefault` helper for CDC stores. The other four plans add handlers and do not touch positions. Sibling plans are referred to by issue number only.

---

## Summary

A restarted CDC processor resumes from its last committed position instead of the start or the end of the change stream. Encina gets a database-backed `ICdcPositionStore` and `IShardedCdcPositionStore` in the eight provider packages that cover the 10 providers of the database matrix: ADO.NET, Dapper and EF Core on SQL Server, PostgreSQL and MySQL, plus MongoDB.

### What the code does today (2026-11-11, `main` at `942c0f124`)

1. **Only an in-memory store is registered.** `AddEncinaCdc` calls `services.TryAddSingleton<ICdcPositionStore, InMemoryCdcPositionStore>()` (`src/Encina.Cdc/ServiceCollectionExtensions.cs:83`). The sharded twin `InMemoryShardedCdcPositionStore` is registered by `RegisterShardedCaptureServices` unless `ShardedCaptureOptions.PositionStoreType` names another type. `InMemoryCdcPositionStore` is `internal`, so no other assembly can name it to replace it.
2. **The connectors check the concrete position type.** `SqlServerCdcConnector` accepts the saved position only when `position is SqlServerCdcPosition` (`src/Encina.Cdc.SqlServer/SqlServerCdcConnector.cs:137`); the MySQL, PostgreSQL, MongoDB and Debezium connectors read the store the same way (`MySqlCdcConnector.cs:107`, `MongoCdcConnector.cs:74,103`). A store that returns a bytes-only position would be ignored. Each position class has a `FromBytes(byte[])` factory and `ToBytes()` (`SqlServerCdcPosition.cs:33`, `MySqlCdcPosition.cs:55`, `PostgresCdcPosition.cs:33`, `MongoCdcPosition.cs:35`, `DebeziumCdcPosition.cs:34`). The abstract `CdcPosition` declares only `ToBytes()`, `CompareTo` and `ToString()` (`src/Encina.Cdc/Abstractions/CdcPosition.cs:32-53`).
3. **The processor discards the result of the save.** `CdcProcessor.ProcessChangesAsync` awaits `positionStore.SavePositionAsync(...)` and never reads the returned `Either` (`src/Encina.Cdc/Processing/CdcProcessor.cs:161`). With an in-memory store that cannot fail; with a database it can, and AGENTS.md section 3 forbids swallowing a `Left` in background infrastructure.
4. **The position moves past a failed event** (#2343, open) and the processor does not retry a `Left` from the dispatcher; that is a separate defect. A persistent store makes it more visible, because the wrong position survives a restart.
5. **Existing persistent-store precedent.** Each provider package registers its stores in `ServiceCollectionExtensions.cs` and removes the in-memory default with a private `RemoveInMemoryDefault<TService, TInMemory>` (`src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:108,117,129`); `Encina.Security.Audit` exposes the same idea publicly as `OperationAuditStoreRegistration.RemoveInMemoryDefault` (`src/Encina.Security.Audit/OperationAuditStoreRegistration.cs`). CDC has no equivalent.
6. **Table and script conventions.** Tables are plural PascalCase (`SagaStates`, `OutboxMessages`, `DeadLetterMessages`); ADO.NET and Dapper packages ship numbered SQL scripts in `Scripts/` (the highest in `Encina.ADO.SqlServer` is `030_CreateBreakTheGlassElevationsTable.sql`); EF Core maps entities with `IEntityTypeConfiguration<T>` (`src/Encina.EntityFrameworkCore/Sagas/SagaStateConfiguration.cs`); MongoDB maps documents with `[BsonElement]` (`src/Encina.MongoDB/Sagas/SagaState.cs`).

### Scope

- **Implements**: #2393. Stores named per AGENTS.md section 4: `CdcPositionStoreADO`, `CdcPositionStoreDapper`, `CdcPositionStoreEF`, `CdcPositionStoreMongoDB`, each implementing both `ICdcPositionStore` and `IShardedCdcPositionStore` over one table or collection, `CdcPositions`.
- **Affected packages**: `Encina.Cdc` (contract, helper, processor), `Encina.Cdc.SqlServer`, `Encina.Cdc.PostgreSql`, `Encina.Cdc.MySql`, `Encina.Cdc.MongoDb`, `Encina.Cdc.Debezium` (codec registration), `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`.
- **Provider category**: Database, all 10 providers (AGENTS.md section 5). SQLite and Oracle are out of the matrix.
- **Out of scope**: the startup validator that rejects the in-memory store outside Development (#2382, which can read `CdcPositionStoreRegistration.IsInMemoryDefault`, Phase 1); processor retry and dead letter (#2343, #2344); leader election and row claiming (#1251); the Debezium offset commit defect (#2397).
- **Split into pull requests** (the whole exceeds the 15-file and 1,500-line limit of AGENTS.md section 11): PR 1 = Phases 1-3 (core and connector codecs, about 12 files); PR 2 = Phase 4 (ADO.NET, 3 packages, about 12 files); PR 3 = Phase 5 (Dapper, 3 packages, about 12 files); PR 4 = Phase 6 (EF Core and MongoDB, about 10 files); PR 5 = Phase 7-8 remainder (integration tests per family and documentation). PRs 2-4 depend on PR 1 and are independent of each other.

---

## Design Choices

<details>
<summary><strong>1. Rebuilding the provider-specific position — a codec registered by each connector</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ICdcPositionCodec` in `Encina.Cdc`**: each connector package registers one codec (`PositionType` string plus `FromBytes`); a store saves `PositionType` and bytes and asks the codec for the object | Eight stores written once, independent of the five connectors; adding a connector adds one class; the connectors keep their `position is XxxCdcPosition` checks | One new public abstraction; the codec for a connector must be registered or the store returns a `Left` |
| **B) A store per (data access provider x connector)** | No new abstraction | A cross product (8 provider packages x 5 connectors) and each provider package would reference every connector package |
| **C) Reflection on `PositionType` (assembly-qualified name) and a static `FromBytes`** | No registration | Reflection over a stored string is a deserialization-of-type risk, breaks trimming, and ties a column value to a CLR name |

### Chosen Option: **A — `ICdcPositionCodec`, one per connector** (recommended, pending the maintainer)

### Rationale

The connectors type-check the position they read (`SqlServerCdcConnector.cs:137`), and the database that stores the position is independent of the database the connector reads (a SQL Server connector can run beside a PostgreSQL application database, or the reverse). A codec registry keeps the eight stores free of connector references. C is rejected because a persisted type name read back into `Type.GetType` is the classic unsafe deserialization shape.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. One table for both stores — <code>CdcPositions</code> keyed by connector and shard</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One table `CdcPositions(ConnectorId, ShardId, PositionType, PositionBytes, Version, UpdatedAtUtc)`**, `ShardId` is the empty string for the unsharded store | One schema and one script per package; both store interfaces share the code; the key `(ConnectorId, ShardId)` matches `IShardedCdcPositionStore`'s composite key | `ShardId` empty string is a convention |
| **B) Two tables** | Each interface has its own shape | Two scripts, two entities and two stores per package for one concept |
| **C) A key-value table of opaque rows** | Generic | Loses the typed columns that health checks and operators query |

### Chosen Option: **A — one `CdcPositions` table**

### Rationale

The two interfaces differ only by the shard component (`IShardedCdcPositionStore.GetPositionAsync(shardId, connectorId)`, `IShardedCdcPositionStore.cs:33`). The plural name follows `SagaStates`, `OutboxMessages` and `DeadLetterMessages`; the issue text says `CdcPosition` and the plan keeps the established plural. `UpdatedAtUtc` comes from `TimeProvider`.

**Class:** Trivial
**Settled by:** table naming and `AtUtc` suffix of the existing stores, AGENTS.md section 4 and the persistent dead letter plan #583

</details>

<details>
<summary><strong>3. Concurrency and monotonic progress — compare-and-swap on a version, plus a regression check</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `Version` integer (or `rowversion` / `_version` field) compared in the `UPDATE ... WHERE Version = @v`; before writing, `CompareTo` against the stored position rejects a regression with a `Left` (`CdcErrorCodes.PositionRegressed`) | Two instances cannot move the position back (issue acceptance); no provider-specific monotonic SQL; same logic in all 10 providers | One extra read per save; `CompareTo` is only as strong as each connector's ordering (a Mongo resume token or a MySQL GTID set is not a total order) |
| **B) Compare the bytes in SQL (`WHERE PositionBytes < @new`)** | Single statement | Byte order is not position order for a binlog file name or a resume token; wrong for two of five connectors |
| **C) Last writer wins** | Simplest | The failure the issue names: a slow instance rewinds the stream |

### Chosen Option: **A — version compare-and-swap plus `CompareTo` guard**

### Rationale

The issue requires that two instances cannot move the position backwards; settled by elimination on #2393 (comment of 2026-10-17). `CdcPosition.CompareTo` is the contract for ordering (`CdcPosition.cs:46`). Phase 1 reads each connector's `CompareTo` and records in its XML docs which ones give a total order; a position that cannot be ordered compares as equal and is written (the guard only rejects a proven regression).

**Class:** Settled
**Settled by:** #2393 (maintainer comment 2026-10-17, "optimistic concurrency, two instances cannot move the position backwards"); AGENTS.md section 3 (asynchronous database calls with `CancellationToken`)

</details>

<details>
<summary><strong>4. Winning over the in-memory default in any registration order</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A public `CdcPositionStoreRegistration.RemoveInMemoryDefault(services)` in `Encina.Cdc`; each provider's `AddEncinaCdcPositionStore*` removes only the in-memory descriptor and then `TryAdd`s its store | An application's own `ICdcPositionStore` registration survives; works before or after `AddEncinaCdc`; follows `OperationAuditStoreRegistration` | The in-memory registration must carry a recognisable marker (implementation type check inside `Encina.Cdc`) |
| **B) `services.Replace(...)`** | One line | Clobbers an application's own registration, which AGENTS.md section 3 forbids |
| **C) Make `AddEncinaCdc` skip the in-memory store when a provider flag is set** | No removal | Registration order dependence: the flag must be set first |

### Chosen Option: **A — `CdcPositionStoreRegistration.RemoveInMemoryDefault`**

### Rationale

AGENTS.md section 3 (registration completeness) requires that a database store wins over an in-memory default in any order without overriding the application's own registration, proven by a DI test with `ValidateOnBuild` and `ValidateScopes`. The same helper serves the sharded store and exposes `IsInMemoryDefault` for the #2382 validator.

**Class:** Settled
**Settled by:** AGENTS.md section 3, "Registration completeness" (#1269, #1295); precedent `OperationAuditStoreRegistration.RemoveInMemoryDefault`

</details>

<details>
<summary><strong>5. Transaction scope of a save — own single-statement transaction after dispatch</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Save after the event is dispatched, in the store's own atomic statement (at-least-once delivery)** | Matches `CdcProcessor` (`CdcProcessor.cs:161`); no coupling to handlers; handlers stay idempotent (#1220) | A crash between the handler and the save replays that event |
| **B) Join the handler's ambient `IUnitOfWork` transaction when handler and store share a database** | Exactly-once effect for handlers that write to the same database | Handlers are resolved per table and call other resources (`IEncina.Publish`, caches); needs a unit of work per event and changes the dispatcher contract |

### Chosen Option: **A — save after dispatch, own statement** (recommended, pending the maintainer)

### Rationale

B wins for a deployment whose handlers are all same-database writers; it is a possible later option and does not change the store contract. Cross-cutting function 8 is therefore recorded as not applicable to the store itself (the issue's matrix says "where the provider allows").

**Class:** Dilemma

</details>

<details>
<summary><strong>6. A failed save is a failure of the batch, not a silent no-op</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CdcProcessor` reads the `Either` from `SavePositionAsync`; a `Left` is counted as a failure of that event, logged by error code, and surfaces to the retry and dead letter path** | Honors AGENTS.md section 3; a broken position database becomes visible | Touches `CdcProcessor.cs` (also the subject of #2343); the merge order must be agreed |
| **B) Keep ignoring the result** | No change | A database outage looks like success and the stream replays after a restart |

### Chosen Option: **A — read the result of the save**

### Rationale

With an in-memory store the discarded result was harmless; with a database it is the failure mode the persistent store introduces.

**Class:** Settled
**Settled by:** AGENTS.md section 3 ("Errors are NEVER swallowed in background infrastructure", #1150-#1153, #1184)

</details>

---

## Implementation Phases

### Phase 1: Core contract, helper and error codes (`Encina.Cdc`)

> **Goal**: add `ICdcPositionCodec`, the registration helper and the error codes, without changing behavior.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Cdc/Abstractions/ICdcPositionCodec.cs`: `string PositionType { get; }` and `CdcPosition FromBytes(byte[] bytes)` (the five connector `FromBytes` already exist and throw `ArgumentException` on bad input; the codec wraps them in a `Left` at the store).
2. `src/Encina.Cdc/CdcPositionStoreRegistration.cs`: `public static class` with `RemoveInMemoryDefault(IServiceCollection)` (removes only the descriptors whose implementation is `InMemoryCdcPositionStore` or `InMemoryShardedCdcPositionStore`) and `IsInMemoryDefault(IServiceProvider)`.
3. `src/Encina.Cdc/Errors/CdcErrorCodes.cs` and `CdcErrors.cs`: add `PositionRegressed` (`encina.cdc.position_regressed`) and `PositionCodecMissing` (`encina.cdc.position_codec_missing`) beside `PositionStoreFailed`; factories in `CdcErrors`.
4. `src/Encina.Cdc/Processing/CdcPositionStoreLog.cs`: `[LoggerMessage]` events in the range allocated in Research (Event ID Allocation).
5. `PublicAPI.Unshipped.txt` of `Encina.Cdc` for the three new public symbols.
6. Read each connector's `CompareTo` and document in the XML remarks which give a total order.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT: Encina.Cdc has an in-memory ICdcPositionStore registered with TryAddSingleton
(src/Encina.Cdc/ServiceCollectionExtensions.cs:83). Connectors type-check positions
(SqlServerCdcConnector.cs:137), so a persistent store must rebuild the exact CdcPosition subclass.
TASK: Add ICdcPositionCodec, CdcPositionStoreRegistration (RemoveInMemoryDefault, IsInMemoryDefault),
two error codes with factories, CdcPositionStoreLog (EventIds 4854-4859 only) and the PublicAPI lines.
KEY RULES: .NET 10 / C# 14; Either<EncinaError, T> returns; no [Obsolete]; XML docs on every public
member; EventIds only inside the range registered in EventIdRanges.cs (ADR-021); do not touch
CdcProcessor in this phase.
REFERENCE FILES: src/Encina.Cdc/Abstractions/ICdcPositionStore.cs, src/Encina.Cdc/Errors/CdcErrorCodes.cs,
src/Encina.Security.Audit/OperationAuditStoreRegistration.cs, src/Encina.Cdc/CdcLog.cs.
```

</details>

### Phase 2: Shared store behavior and contract test base

> **Goal**: define the behavior every store must show, once, so the eight stores are verified by the same tests.

<details>
<summary><strong>Tasks</strong></summary>

1. `tests/Encina.ContractTests/Cdc/CdcPositionStoreContractTests.cs`: an abstract base with: get on empty returns `None`; save then get round-trips through the codec; save with an older position returns `PositionRegressed`; concurrent save with a stale version returns a `Left` and leaves the newer value; delete removes; sharded get-all returns all shards of one connector; `UpdatedAtUtc` equals the fake `TimeProvider` time.
2. `tests/Encina.UnitTests/Cdc/CdcPositionStoreRegistrationTests.cs`: `RemoveInMemoryDefault` keeps an application's own registration and removes only the in-memory type.
3. A fake codec `TestCdcPosition` in `Encina.TestInfrastructure` so the base needs no connector package.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT: Eight stores will implement ICdcPositionStore and IShardedCdcPositionStore. They must behave
identically (AGENTS.md section 5, provider coherence).
TASK: Write the abstract contract test base and the registration helper unit tests. Contract tests
must instantiate real implementations (the in-memory stores now, the database stores through the
subclasses added in Phases 4-6).
KEY RULES: Shouldly through Encina.Testing.Shouldly; TimeProvider fake; no Thread.Sleep; descriptive
names; Arrange-Act-Assert; no FluentAssertions.
REFERENCE FILES: src/Encina.Cdc/Processing/InMemoryCdcPositionStore.cs,
src/Encina.Cdc/Processing/InMemoryShardedCdcPositionStore.cs, tests/Encina.ContractTests.
```

</details>

### Phase 3: Connector codec registrations

> **Goal**: each connector package registers its codec so any store can rebuild its position.

<details>
<summary><strong>Tasks</strong></summary>

1. `SqlServerCdcPositionCodec`, `PostgresCdcPositionCodec`, `MySqlCdcPositionCodec`, `MongoCdcPositionCodec`, `DebeziumCdcPositionCodec`: one small sealed class each (`PositionType` constant such as `"sqlserver"`; `FromBytes` delegates to the existing static factory).
2. Register each with `TryAddEnumerable(ServiceDescriptor.Singleton<ICdcPositionCodec, ...>())` in `AddEncinaCdcSqlServer`, `AddEncinaCdcPostgreSql`, `AddEncinaCdcMySql`, `AddEncinaCdcMongoDb` and the Debezium registration.
3. PublicAPI lines for the five new public classes (or `internal sealed` with registration only: the plan prefers `internal` because nothing outside the package needs the type).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT: Phase 1 added ICdcPositionCodec. Each connector has a static FromBytes on its position class.
TASK: Add one internal sealed codec per connector package and register it in the package's
AddEncinaCdc* method with TryAddEnumerable.
KEY RULES: internal types, so no PublicAPI lines; PositionType values are stable lowercase constants
(they are persisted); the codec never catches exceptions (the store converts them to a Left).
REFERENCE FILES: src/Encina.Cdc.SqlServer/SqlServerCdcPosition.cs and ServiceCollectionExtensions.cs and the
same files in Encina.Cdc.PostgreSql, Encina.Cdc.MySql, Encina.Cdc.MongoDb, Encina.Cdc.Debezium.
```

</details>

### Phase 4: ADO.NET stores (SQL Server, PostgreSQL, MySQL)

> **Goal**: `CdcPositionStoreADO` in the three ADO.NET packages.

<details>
<summary><strong>Tasks</strong></summary>

1. Script `0NN_CreateCdcPositionsTable.sql` in `Scripts/` of each package (next free number; 030 in `Encina.ADO.SqlServer` today). SQL Server: `ConnectorId nvarchar(200)`, `ShardId nvarchar(200) NOT NULL DEFAULT ''`, `PositionType nvarchar(50)`, `PositionBytes varbinary(max)`, `Version int`, `UpdatedAtUtc datetime2`, primary key `(ConnectorId, ShardId)`. PostgreSQL: `bytea`, `timestamptz`, quoted identifiers as the sibling scripts. MySQL: `varbinary(...)`/`longblob`, `datetime(6)`, backtick identifiers.
2. `CdcPositionStoreADO` in `src/Encina.ADO.<Provider>/Cdc/` implementing both interfaces: `OpenAsync(ct)`, `ExecuteReaderAsync(ct)`; upsert through `UPDATE ... WHERE Version = @v` then `INSERT` when no row exists (a duplicate-key race becomes a retry once, then a `Left`).
3. `AddEncinaCdcPositionStoreADO` extension per package: `RemoveInMemoryDefault`, then `TryAddSingleton<ICdcPositionStore>` and `<IShardedCdcPositionStore>`, `TryAddSingleton(TimeProvider.System)`.
4. Integration test class per provider using the existing `[Collection("ADO-SqlServer")]`, `"ADO-PostgreSQL"`, `"ADO-MySQL"` fixtures, deriving from the contract base; a restart test (save, rebuild the provider, get).
5. Coverage manifest entries (Research) via the manifest files of the three packages.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT: Eight packages need the same store. The ADO.NET family is written by hand per database (SQL
differences in AGENTS.md section 5). Tables are created by numbered scripts in Scripts/.
TASK: Implement CdcPositionStoreADO for SqlServer, PostgreSQL and MySQL with the script, the DI extension
and an integration test class per provider; pass the contract base from Phase 2.
KEY RULES: async only (OpenAsync/ExecuteNonQueryAsync with the CancellationToken, never Open()); parameters
only; TimeProvider for UpdatedAtUtc; errors as Left with CdcErrorCodes.PositionStoreFailed (code only, never
the provider message); shared [Collection] fixture, ClearAllDataAsync in InitializeAsync; no per-class fixture.
REFERENCE FILES: src/Encina.ADO.SqlServer/Sagas/SagaStoreADO.cs, src/Encina.ADO.SqlServer/Scripts/029_CreateDeadLetterMessagesTable.sql,
src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs, the same files in the PostgreSQL and MySQL packages.
```

</details>

### Phase 5: Dapper stores (SQL Server, PostgreSQL, MySQL)

> **Goal**: `CdcPositionStoreDapper` in the three Dapper packages with the same behavior.

<details>
<summary><strong>Tasks</strong></summary>

1. Scripts `0NN_CreateCdcPositionsTable.sql` in each Dapper package (same schema as Phase 4).
2. `CdcPositionStoreDapper` in `src/Encina.Dapper.<Provider>/Cdc/`: `connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: ct))`.
3. `AddEncinaCdcPositionStoreDapper` extension with `RemoveInMemoryDefault` and `TryAdd*`.
4. Integration tests on `[Collection("Dapper-SqlServer")]`, `"Dapper-PostgreSQL"`, `"Dapper-MySQL"`.
5. Manifest entries.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT: Same contract as Phase 4, Dapper flavour.
TASK: Implement CdcPositionStoreDapper for the three databases, the DI extension, the scripts and the
integration tests deriving from the contract base.
KEY RULES: CommandDefinition with the CancellationToken; no synchronous Dapper overloads; shared
[Collection] fixtures; error codes only; TimeProvider.
REFERENCE FILES: src/Encina.Dapper.SqlServer/Sagas/SagaStoreDapper.cs, src/Encina.Dapper.PostgreSQL/Sagas/SagaStoreDapper.cs,
src/Encina.Dapper.MySQL/ServiceCollectionExtensions.cs.
```

</details>

### Phase 6: EF Core and MongoDB stores

> **Goal**: `CdcPositionStoreEF` (one package for three databases) and `CdcPositionStoreMongoDB`.

<details>
<summary><strong>Tasks</strong></summary>

1. EF Core: entity `CdcPositionEntity`, `CdcPositionEntityConfiguration` (`ToTable("CdcPositions")`, composite key, `IsConcurrencyToken` on `Version`), `CdcPositionStoreEF<TDbContext>` using `SaveChangesAsync(ct)` and mapping `DbUpdateConcurrencyException` to a `Left`; `AddEncinaCdcPositionStoreEntityFrameworkCore<TDbContext>`.
2. MongoDB: document class with `[BsonElement]` fields, `CdcPositionStoreMongoDB` using `FindOneAndUpdateAsync` with a filter on `version`, a unique index on `(connectorId, shardId)`, `AddEncinaCdcPositionStoreMongoDB`.
3. Integration tests on `[Collection("EFCore-SqlServer")]`, `"EFCore-PostgreSQL"`, `"EFCore-MySQL"` and the MongoDB collection fixture.
4. Manifest entries.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT: EF Core covers three databases from one package; MongoDB is the tenth provider.
TASK: Implement the EF Core and MongoDB stores, their configuration, DI extensions and integration tests.
KEY RULES: no synchronous database calls; optimistic concurrency through the Version token; the index on
(connectorId, shardId) is created by the registration path used by the other MongoDB stores; error codes only.
REFERENCE FILES: src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs, src/Encina.EntityFrameworkCore/Sagas/SagaStateConfiguration.cs,
src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs, src/Encina.MongoDB/ServiceCollectionExtensions.cs.
```

</details>

### Phase 7: Processor integration, observability and health

> **Goal**: the processor reads the result of the save and the new behavior is visible.

<details>
<summary><strong>Tasks</strong></summary>

1. `CdcProcessor.cs` (line 161) and `ShardedCdcProcessor.cs`: read the `Either` from the save; on `Left` count the event as failed and log `CdcPositionStoreLog.SaveFailed` with the error code (never the message).
2. `src/Encina.Cdc/Diagnostics/CdcPositionStoreMetrics.cs` (meter `Encina.Cdc.PositionStore`): counters `encina.cdc.position.saves` and `encina.cdc.position.save_errors` (tag `store`), histogram `encina.cdc.position.lag_seconds` (now minus `ChangeMetadata.CapturedAtUtc`, from `TimeProvider`).
3. `CdcPositionStoreActivitySource` (`Encina.Cdc.PositionStore`) spans around get and save, tag `encina.cdc.position.store`.
4. `Encina.OpenTelemetry`: subscribe the new meter and source in `WithEncina()`.
5. `CdcHealthCheck` already calls `GetPositionAsync` (`src/Encina.Cdc/Health/CdcHealthCheck.cs`); add a `Degraded` result when no save succeeded within `CdcOptions.PositionMaxAge` (new option, default 5 minutes, validated).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT: Phases 1-6 added stores whose Save can fail. CdcProcessor discards that result today.
TASK: Make the processors honor the result, add metrics, spans, the OpenTelemetry subscription and the
position-age health state.
KEY RULES: a Left is never swallowed (AGENTS.md section 3); log by error code, never EncinaError.Message;
TimeProvider only; the meter is Encina.Cdc.PositionStore; EventIds 4854-4859 only; keep the change to
CdcProcessor minimal because #2343 edits the same method.
REFERENCE FILES: src/Encina.Cdc/Processing/CdcProcessor.cs, src/Encina.Cdc/Caching/Diagnostics/CacheInvalidationMetrics.cs,
src/Encina.Cdc/Health/CdcHealthCheck.cs.
```

</details>

### Phase 8: Testing

> **Goal**: every applicable flag reaches its manifest target.

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: codec mapping, regression guard, version conflict, registration helper, processor save-failure path.
2. Guard: null and empty `connectorId`, null position on every public method of the eight stores.
3. Contract: the Phase 2 base run against all 10 providers.
4. Property: for any sequence of increasing positions the stored value equals the maximum; round-trip `ToBytes`/codec for the five position types.
5. Integration: 10 providers on shared `[Collection]` fixtures; the restart test.
6. DI: build the provider with `ValidateOnBuild` and `ValidateScopes`, registering the store before and after `AddEncinaCdc` and with an application-owned store; the store must win in both orders and the application's must survive.
7. Load and benchmark: justification files (`.md`) for the single-row, low-rate upsert; no concurrency-sensitive hot path beyond one save per batch.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
CONTEXT: Coverage is per flag (AGENTS.md section 9); each flag reaches its own target from the package manifests.
TASK: Write the unit, guard, contract, property and integration tests plus the DI proof, run each flag's
project with --collect "XPlat Code Coverage", and compare with the manifest targets.
KEY RULES: tests execute real package code; Shouldly via Encina.Testing.Shouldly; shared [Collection]
fixtures for databases; TimeProvider fake; justification .md files only for load and benchmark.
REFERENCE FILES: .github/coverage-manifest/Encina.Cdc.json, tests/Encina.IntegrationTests, docs/testing/integration-tests.md.
```

</details>

### Phase 9: Documentation and finalization

> **Goal**: users can configure a durable CDC position store and operators can read it.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on all new public APIs (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`).
2. `changelog.d/2393-cdc-position-store.added.md`.
3. `src/Encina.Cdc/README.md` and the eight provider READMEs (a section "CDC position store"); `docs/features/cdc.md` (durable position, restart semantics, at-least-once, how to choose the store).
4. ADR number assigned when the ADR pull request opens: the codec contract and the registration rule.
5. `PublicAPI.Unshipped.txt` in the eight provider packages and `Encina.Cdc`; `docs/INVENTORY.md`.
6. Verification: `dotnet build --configuration Release` with zero warnings; `dotnet test`; per-flag coverage of the new files against the manifest targets; `dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
CONTEXT: The feature is code-complete after Phase 8.
TASK: Write the XML docs, the changelog fragment, the READMEs, the feature page and the PublicAPI lines;
run the build and test verification.
KEY RULES: never type coverage figures by hand (cite cov: references); English only; no emojis; pages follow
the encina-docs skill; never edit CHANGELOG.md.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, changelog.d/README.md, docs/features/cdc.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| AGENTS.md section 3 (registration completeness) | The store wins over the in-memory default in any order and does not override the application's registration |
| AGENTS.md section 3 (errors are never swallowed) | The processor reads the result of the save |
| AGENTS.md section 3 (database calls) | Asynchronous calls with a `CancellationToken` |
| AGENTS.md section 5 | All 10 database providers; shared `[Collection]` fixtures |
| #2381 (module requirements, closed) | CDC requires a persistent position store; #2382 enforces it at startup |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `ICdcPositionStore`, `IShardedCdcPositionStore` | `src/Encina.Cdc/Abstractions` | The contracts the stores implement |
| `CdcPosition` and five subclasses | `src/Encina.Cdc*/**Position.cs` | `ToBytes`, `FromBytes`, `CompareTo` |
| `InMemoryCdcPositionStore`, `InMemoryShardedCdcPositionStore` | `src/Encina.Cdc/Processing` | Defaults to remove; behavior reference |
| `OperationAuditStoreRegistration` | `src/Encina.Security.Audit` | Pattern for the registration helper |
| `CdcHealthCheck` | `src/Encina.Cdc/Health` | Already reads the store; gets the position-age state |
| Provider `Scripts/`, `*Configuration`, `[BsonElement]` mappings | the eight provider packages | Schema delivery conventions |
| `[Collection]` fixtures | `tests/Encina.IntegrationTests` | Shared containers per family |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Cdc` (existing range 4800-4899, used 4800-4853) | 4854-4859 | `CdcPositionStoreLog`: save failed, get failed, codec missing, regression rejected, version conflict retried, position saved. The sibling plans take 4860-4899; no new range is needed |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core (`Encina.Cdc`) | 6 | codec, helper, error codes, log, metrics, activity source |
| Connector codecs | 5 | one per connector package, plus registration edits |
| Provider stores | 8 | one store class per package (plus EF entity and configuration) |
| Scripts | 6 | ADO.NET and Dapper packages |
| DI extensions | 8 | one edit per package |
| Tests | ~30 | contract base, unit, guard, property, 10 integration classes, DI proof |
| Docs, changelog, manifests | ~14 | READMEs, feature page, fragment, manifest entries |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library with nullable reference types. You are
implementing issue #2393: persistent ICdcPositionStore and IShardedCdcPositionStore on the 10 database
providers, one table or collection CdcPositions per provider. Read AGENTS.md first.

IMPLEMENTATION OVERVIEW: Phase 1 adds ICdcPositionCodec, CdcPositionStoreRegistration and error codes to
Encina.Cdc. Phase 3 registers one codec per connector. Phases 4-6 add CdcPositionStoreADO (3), Dapper (3),
EF (1 package, 3 databases) and MongoDB with a numbered script or mapping each. Phase 7 makes the processors
read the result of the save and adds metrics, spans and a position-age health state. Phase 8 tests every
flag; Phase 9 documents. Deliver as five pull requests in the order given in the Summary.

KEY PATTERNS: Either<EncinaError, T> everywhere; version compare-and-swap plus CompareTo regression guard;
RemoveInMemoryDefault then TryAdd so the store wins in any registration order without clobbering the
application's own registration; TimeProvider for UpdatedAtUtc; asynchronous database calls with the
CancellationToken; error codes only in logs; EventIds 4854-4859; shared [Collection] fixtures; Shouldly.

REFERENCE FILES: src/Encina.Cdc/ServiceCollectionExtensions.cs, src/Encina.Cdc/Processing/CdcProcessor.cs,
src/Encina.Cdc/Processing/InMemoryCdcPositionStore.cs, src/Encina.Security.Audit/OperationAuditStoreRegistration.cs,
src/Encina.ADO.SqlServer/Sagas/SagaStoreADO.cs, src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs,
src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs, .github/coverage-manifest/Encina.Cdc.json.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: the position must be durable, a cache is the wrong guarantee (issue alternative 3) |
| 2 | OpenTelemetry | ✅ | Phase 7: save counters, error counter, lag histogram, spans |
| 3 | Structured Logging | ✅ | `CdcPositionStoreLog`, EventIds 4854-4859 inside the registered `Cdc` range |
| 4 | Health Checks | ✅ | Phase 7: `CdcHealthCheck` reports a degraded state when no save succeeded within `PositionMaxAge` |
| 5 | Validation | ✅ | `CdcOptions.PositionMaxAge` is validated at startup; the #2382 validator rejects the in-memory store outside Development |
| 6 | Resilience | ⏭️ | Retry and dead letter of failed events follow #2343 and #2344; the store retries one version conflict only |
| 7 | Distributed Locks | ⏭️ | Leader election and row claiming for CDC are #1251; the version check keeps the position safe meanwhile |
| 8 | Transactions | ❌ | Not applicable to the store: one atomic statement per save, after dispatch (Design Choice 5 records the alternative) |
| 9 | Idempotency | ⏭️ | At-least-once replay after a crash between handler and save; handler idempotency is #1220 |
| 10 | Multi-Tenancy | ⏭️ | The connector id may carry the tenant; tenant-scoped positions follow #1257 (position rows hold no personal data) |
| 11 | Module Isolation | ❌ | Not applicable: infrastructure-level state |
| 12 | Audit Trail | ❌ | Not applicable: operational checkpoint, not an audit event |

---

## Prerequisites & Dependencies

| Item | State | Effect |
|------|-------|--------|
| #2343 (position advances past failed events) | open | Same method in `CdcProcessor`; agree the merge order, Phase 7 stays minimal |
| #2382 (startup requirements validator) | open | Consumes `CdcPositionStoreRegistration.IsInMemoryDefault` |
| #2397 (MySQL restored position never applied, Debezium never commits) | open | Persistence helps only once the connectors apply what they read; not a blocker for the stores |
| #583 plan (persistent dead letter store) | plan | Same package set; the script numbers must be taken at implementation time |

---

## Next Steps

1. Review and approve this plan; the maintainer answers the two Dilemmas (choices 1 and 5) when work on the issue starts.
2. Publish the plan link as a comment on #2393.
3. Begin PR 1 (Phases 1-3).
4. Each phase is a self-contained commit; the final commit references `Fixes #2393`.
