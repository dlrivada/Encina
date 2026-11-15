# Implementation Plan: Persistent `ICdcPositionStore` and `IShardedCdcPositionStore` on the 10 Database Providers

> **Issue**: [#2393](https://github.com/dlrivada/Encina/issues/2393) (`area-cdc`, `area-database`, `p1-recommended`)
> **Type**: Feature
> **Complexity**: Medium-High (one contract addition in `Encina.Cdc`, five connector registrations, eight provider packages)
> **Estimated Scope**: ~1,300-1,700 lines of production code + ~2,200-2,800 lines of tests (about 132 files in total, including tests, manifests, PublicAPI and documentation), delivered in 15 pull requests, each at most 15 files and 1,500 lines (see "Split into pull requests")
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
- **Split into pull requests** (the whole is about 125 files and 3,500-4,500 lines once tests, coverage manifests, `PublicAPI.Unshipped.txt` lines and documentation are counted, so it far exceeds the 15-file and 1,500-line limit of AGENTS.md section 11). Every PR below counts its tests, manifest entries and PublicAPI edits; the tests of a unit of code travel in the PR of that code. The numbering is the merge order.

  | PR | Content (phase) | Files (incl. tests) | Lines (est.) | Depends on |
  |----|-----------------|---------------------|--------------|------------|
  | 1 | Core contract: `ICdcPositionCodec`, `CdcPositionStoreRegistration`, two error codes + factories, `CdcPositionStoreLog` (4854-4859), PublicAPI, registration unit test, `Encina.Cdc` manifest (Phase 1) | 8 | ~450 | none |
  | 2 | Codecs for SQL Server, PostgreSQL, MySQL: 3 codec classes, 3 registration edits, 3 manifest edits, 1 codec unit test file (Phase 3) | 10 | ~350 | 1 |
  | 3 | Codecs for MongoDB and Debezium (2 classes, 2 registration edits, 2 manifest edits), contract test base and `TestCdcPosition` fake (Phase 2), codec unit test file | 9 | ~650 | 1 |
  | 4 | Processors read the save result: `CdcProcessor`, `ShardedCdcProcessor`, 2 unit test files, manifest (Phase 7 task 1) | 5 | ~300 | 1; order agreed with #2343 |
  | 5 | Observability and health: metrics, activity source, OpenTelemetry subscription, `CdcOptions.PositionMaxAge` + validator, `CdcHealthCheck`, PublicAPI, 3 unit test files, 1 guard test file, manifest (Phase 7 tasks 2-5) | 12 | ~700 | 1 |
  | 6-8 | ADO.NET, one PR per database (SQL Server, PostgreSQL, MySQL): script, store, DI extension, PublicAPI, manifest, unit test, guard test, integration test class (Phase 4) | 8 each | ~500 each | 1, 3 |
  | 9-11 | Dapper, one PR per database, same shape as PRs 6-8 (Phase 5) | 8 each | ~450 each | 1, 3 |
  | 12 | EF Core: entity, configuration, store, DI extension, PublicAPI, manifest, unit test, guard test, 3 integration classes (one per database collection) (Phase 6) | 11 | ~700 | 1, 3 |
  | 13 | MongoDB: document, store, index creation, DI extension, PublicAPI, manifest, unit test, guard test, integration test class (Phase 6) | 9 | ~550 | 1, 3 |
  | 14 | Cross-package proofs: DI proof test (`ValidateOnBuild` and `ValidateScopes`, both registration orders, application-owned store), property tests, load justification file, one benchmark class (Phase 8 tasks 4, 6, 7) | 6 | ~450 | 6-13 |
  | 15 | Documentation: changelog fragment, `Encina.Cdc` README and the eight provider READMEs, `docs/features/cdc.md`, `docs/INVENTORY.md`, ADR (Phase 9) | 14 | ~700 | 14 |

  Total: 15 PRs, 132 files, about 7,700 line-changes (the sum of the per-PR upper estimates; it includes manifests, scripts, PublicAPI and documentation, which the code-and-test estimate of the header leaves out); no PR exceeds 15 files or 1,500 lines. PRs 2-5 are independent of each other after PR 1 (PR 2 and PR 3 both need PR 1); PRs 6-13 are independent of each other after PRs 1 and 3. The explicit count per PR is re-measured against the limit when each PR opens (the `pr-size` job enforces it).

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
| **A) An integer `Version` column compared in `UPDATE ... WHERE Version = @v`; before writing, `CompareTo` against the stored position rejects a regression with a `Left` (`CdcErrorCodes.PositionRegressed`)** | Two instances cannot move the position back for orderable positions; no provider-specific monotonic SQL; same logic in all 10 providers; the caller learns of the conflict | One extra read per save; the guard is only as strong as each connector's `CompareTo` (see the table below) |
| **B) As A, but a regression is an idempotent no-op that returns `Right` (the stored newer position wins silently)** | A slow instance is never an error; no new error code | A stale writer is not told it lost; hides a split-brain deployment |
| **C) Native row versioning (`rowversion` on SQL Server, `xmin` on PostgreSQL, `_version` on MongoDB) instead of an integer column** | The database maintains the token | Three mechanisms in 10 providers; MySQL has none; breaks provider coherence |
| **D) Version compare-and-swap only, no `CompareTo` guard** | No dependency on the connectors' ordering; no false rejection | Two instances that both hold the current version can still write in the wrong order after a retry; no protection against an older position under a fresh version |
| **E) Compare the bytes in SQL (`WHERE PositionBytes < @new`)** | Single statement | Byte order is not position order for a binlog file name or a resume token |
| **F) Last writer wins** | Simplest | The failure the issue names: a slow instance rewinds the stream |

### Chosen Option: **A — version compare-and-swap plus `CompareTo` guard, `Left` on regression** (recommended, pending the maintainer)

### Rationale

The issue requires that two instances cannot move the position backwards (maintainer comment of 2026-10-17); A, B, C and D each satisfy that sentence in part, so the rule does not select one of them and the choice is a Dilemma. A is recommended because it reports the conflict, keeps one mechanism across the 10 providers and matches the sagas' optimistic concurrency. Facts verified on `main` about the guard: `CdcPosition.CompareTo` is the contract (`CdcPosition.cs:46`); SQL Server compares `Version` (`SqlServerCdcPosition.cs:69`) and PostgreSQL compares `Lsn` (`PostgresCdcPosition.cs:69`), both total orders; MySQL compares GTID sets as ordinal strings, Debezium compares `OffsetJson` as ordinal strings and MongoDB compares the BSON of the resume token (`MySqlCdcPosition.cs:106`, `DebeziumCdcPosition.cs:64`, `MongoCdcPosition.cs:72`), which is an arbitrary order and not the order of the stream. The earlier text that "a position that cannot be ordered compares as equal and is written" was wrong: `CompareTo` returns a non-zero value, so under A the guard would reject genuine progress on those three connectors with `PositionRegressed`. Under A the property "the stored position never moves backwards" therefore holds only for SQL Server and PostgreSQL (and MySQL file/position mode); for MySQL GTID, Debezium and MongoDB only the version compare-and-swap protects the row. Resolving that needs a second maintainer choice (Design Choice 8).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Winning over the in-memory default in any registration order</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Remove only the in-memory descriptor (matched by implementation type) and then `TryAdd` the store** | An application's own `ICdcPositionStore` registration survives; works before or after `AddEncinaCdc` | The in-memory registration is recognised by its implementation type, which is `internal` to `Encina.Cdc` (so the removal code lives in `Encina.Cdc`) |
| **B) `services.Replace(...)`** | One line | Clobbers an application's own registration, which AGENTS.md section 3 forbids |
| **C) Make `AddEncinaCdc` skip the in-memory store when a provider flag is set** | No removal | Registration order dependence: the flag must be set first, which section 3 ("in any registration order") forbids |

### Chosen Option: **A — remove only the in-memory descriptor, then `TryAdd`**

### Rationale

AGENTS.md section 3 (registration completeness) requires that a database store wins over an in-memory default in any order without overriding the application's own registration, proven by a DI test with `ValidateOnBuild` and `ValidateScopes`. B and C each break one clause of that sentence, so only A remains. The public or private form of the helper is a separate choice (Design Choice 7).

**Class:** Settled
**Settled by:** AGENTS.md section 3, "Registration completeness" (#1269, #1295); B breaks "without overriding the application's own registration" and C breaks "in any registration order"

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

B wins for a deployment whose handlers are all same-database writers; it is a possible later option and does not change the store contract. Cross-cutting function 8 (Transactions) depends on this choice: with A it is "Not applicable" to the store (one atomic statement per save); if the maintainer picks B it flips to Integrate (`IUnitOfWork` per event). The issue's own matrix marks Transactions "where the provider allows", so the matrix row below carries both outcomes until the maintainer answers.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. A failed save is a failure of the batch, not a silent no-op</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CdcProcessor` and `ShardedCdcProcessor` read the `Either` from the save; a `Left` is logged by error code and the operation fails visibly (the consequence for the batch is Design Choice 9)** | Honors AGENTS.md section 3; a broken position database becomes visible | Touches `CdcProcessor.cs` (also the subject of #2343); the merge order must be agreed |
| **B) Keep ignoring the result** | No change | A database outage looks like success and the stream replays after a restart; AGENTS.md section 3 forbids it |

### Chosen Option: **A — read the result of the save**

### Rationale

With an in-memory store the discarded result was harmless; with a database it is the failure mode the persistent store introduces. On `main` the discard is at `CdcProcessor.cs:161-164` (inside the per-event loop, after `successCount++` at line 156) and `ShardedCdcProcessor.cs:135`. The rule forbids B, so A is the only compliant option; what A does on a `Left` is left to Design Choice 9.

**Class:** Settled
**Settled by:** AGENTS.md section 3 ("Errors are NEVER swallowed in background infrastructure", #1150-#1153, #1184); B is the swallowed `Left` the rule names

</details>

<details>
<summary><strong>7. Form of the removal helper — public in <code>Encina.Cdc</code> or private per provider</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A public static `CdcPositionStoreRegistration` in `Encina.Cdc` (`RemoveInMemoryDefault`, `IsInMemoryDefault`)** | One implementation for the 8 provider packages; `IsInMemoryDefault` is the hook the #2382 startup validator needs; follows `OperationAuditStoreRegistration` (`src/Encina.Security.Audit/OperationAuditStoreRegistration.cs`) | Two new public symbols in a core package |
| **B) A private `RemoveInMemoryDefault<TService, TInMemory>` per provider package, as `Encina.ADO.SqlServer` does (`ServiceCollectionExtensions.cs:108`)** | No public API | Cannot name the `internal` `InMemoryCdcPositionStore`; the recognition logic is copied 8 times; #2382 would need its own check |

### Chosen Option: **A — public helper** (recommended, pending the maintainer)

### Rationale

B cannot reference `InMemoryCdcPositionStore` (it is `internal`), so B would force the type public or a marker interface; A keeps the type internal and gives #2382 its hook.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Ordering guard for positions without a temporal order (MySQL GTID, Debezium, MongoDB)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ICdcPositionCodec` exposes `bool HasTotalOrder`; the `CompareTo` guard runs only for codecs that return true (SQL Server, PostgreSQL); the other connectors rely on the version compare-and-swap only** | No false `PositionRegressed` on valid progress; the limit is explicit and documented | One more member on the codec; the monotonic property holds for two of five connectors |
| **B) Run the guard for every connector** | Uniform | Rejects genuine progress where `CompareTo` is an ordinal string or BSON order (`MySqlCdcPosition.cs:106`, `DebeziumCdcPosition.cs:64`, `MongoCdcPosition.cs:72`) |
| **C) Fix each connector's `CompareTo` to a real order first** | Strongest guarantee | GTID sets, Debezium offsets and Mongo resume tokens have no total order to implement; a larger change in the connector packages |

### Chosen Option: **A — guard only for totally ordered codecs** (recommended, pending the maintainer)

### Rationale

B contradicts the fail-visible intent (a false regression stops the stream); C is not implementable for opaque tokens. A is the smallest change that keeps the guarantee honest. The property test "the stored position never moves backwards" is then written for the ordered codecs only.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. What a failed save does to the batch</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Stop the batch at the first failed save (the event stays unsaved and replays), count it as a failure and apply the existing backoff** | A broken position database stops consuming; no event is processed past an unsaved position | Needs a new branch next to `successCount++` (`CdcProcessor.cs:156`); overlaps #2343 |
| **B) Count the event as failed, log, and continue with the next event** | Throughput continues | Later saves can succeed and move the position past the event that could not be saved; the failure is easy to lose |

### Chosen Option: **A — stop the batch** (recommended, pending the maintainer)

### Rationale

The save sits inside the success branch of the loop (`CdcProcessor.cs:154-171`), so a failed save needs a new branch either way. The merge order with #2343, which edits the same method, is part of the maintainer's answer.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. <code>CdcOptions.PositionMaxAge</code> default and health behavior on an idle stream</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New option `PositionMaxAge`, default 5 minutes; `Degraded` when no save succeeded within it** | Detects a stuck store | Misfires on an idle stream with no changes |
| **B) As A, but the age is measured only while the connector reports pending changes (or from the last attempted save, not the last event)** | No false alarm when idle | Needs a signal from the connector or a counter of save attempts |
| **C) No time-based state; the health check reports only store accessibility and the save error counter** | No new option | Does not show a store that fails every save quietly |

### Chosen Option: **B — measure from the last save attempt, 5 minutes default** (recommended, pending the maintainer)

### Rationale

A is simple but turns every quiet period into `Degraded`. The default value and the new public option are the maintainer's to confirm.

**Class:** Dilemma

</details>

### Smaller decisions (Trivial)

Each is reversible without a public contract change or follows a precedent on `main`; none selects between options that a rule treats differently.

| Id | Decision | Class | Basis |
|----|----------|-------|-------|
| T1 | Store lifetime is scoped (`TryAddScoped<ICdcPositionStore>` and `<IShardedCdcPositionStore>`); a singleton would capture a scoped `DbContext` or connection and fail `ValidateScopes` | Trivial | The processors resolve the store inside `CreateScope()` (`CdcProcessor.cs:128-131`); the persistent stores of `Encina.ADO.SqlServer` are `TryAddScoped` (`ServiceCollectionExtensions.cs`, e.g. `IReadAuditStore`) |
| T2 | `ShardId` is the empty string for the unsharded store | Trivial | Design Choice 2; keeps one composite key `(ConnectorId, ShardId)` |
| T3 | A duplicate-key race on the first insert is retried once, then returns `Left(PositionStoreFailed)` | Trivial | Insert race happens only for a new `(ConnectorId, ShardId)`; the retry is invisible to callers |
| T4 | Codec classes are `internal sealed` (Phase 3 no longer leaves the choice open) | Trivial | Nothing outside the package names the type; no PublicAPI lines |
| T5 | Meter `Encina.Cdc.PositionStore`, activity source `Encina.Cdc.PositionStore`, instruments `encina.cdc.position.saves`, `.save_errors`, `.lag_seconds` | Trivial | Names follow `Encina.Cdc.Caching.Diagnostics` (`CacheInvalidationMetrics`) |
| T6 | Column sizes: `ConnectorId` and `ShardId` `nvarchar(200)` (or the provider equivalent), `PositionType` 50 | Trivial | Same order of size as the connector and shard ids in use; changeable by a later script |
| T7 | Table name `CdcPositions` (the issue text says `CdcPosition`) | Trivial | Plural PascalCase of `SagaStates`, `OutboxMessages` (Design Choice 2) |
| T8 | The multi-tenancy consequence: no `TenantId` column in this schema; a tenant-scoped position is carried by the connector id until #1257 | Trivial | Matrix row 10 |

---

## Implementation Phases

### Phase 1: Core contract, helper and error codes (`Encina.Cdc`)

> **Goal**: add `ICdcPositionCodec`, the registration helper and the error codes, without changing behavior.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Cdc/Abstractions/ICdcPositionCodec.cs`: `string PositionType { get; }`, `bool HasTotalOrder { get; }` (only if Design Choice 8 keeps option A) and `CdcPosition FromBytes(byte[] bytes)` (the five connector `FromBytes` already exist and throw `ArgumentException` on bad input; the codec wraps them in a `Left` at the store).
2. `src/Encina.Cdc/CdcPositionStoreRegistration.cs`: `public static class` with `RemoveInMemoryDefault(IServiceCollection)` (removes only the descriptors whose implementation is `InMemoryCdcPositionStore` or `InMemoryShardedCdcPositionStore`) and `IsInMemoryDefault(IServiceProvider)`.
3. `src/Encina.Cdc/Errors/CdcErrorCodes.cs` and `CdcErrors.cs`: add `PositionRegressed` (`encina.cdc.position_regressed`) and `PositionCodecMissing` (`encina.cdc.position_codec_missing`) beside `PositionStoreFailed`; factories in `CdcErrors`.
4. `src/Encina.Cdc/Processing/CdcPositionStoreLog.cs`: `[LoggerMessage]` events in the range allocated in Research (Event ID Allocation).
5. `PublicAPI.Unshipped.txt` of `Encina.Cdc` for the three new public symbols.
6. Document in the XML remarks which connectors give a total order: SQL Server and PostgreSQL do; MySQL GTID, Debezium and MongoDB compare ordinal strings or BSON (Design Choice 3).
7. Error-code categories: `PositionRegressed` and `PositionCodecMissing` declare their category through the merged error category contract of ADR 062 (`EncinaErrors.Create(code, ErrorCategory.X, ...)`, `GetCategory`; PR #3599).

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

> **Goal**: define the behavior every store must show, once, so the eight store implementations are verified by the same tests (delivered in PR 3, the codec PR that follows PR 1).

<details>
<summary><strong>Tasks</strong></summary>

1. `tests/Encina.ContractTests/Cdc/CdcPositionStoreContractTests.cs`: an abstract base with: get on empty returns `None`; save then get round-trips through the codec; save with an older position returns `PositionRegressed` (for codecs with a total order only, Design Choice 8); concurrent save with a stale version returns a `Left` and leaves the newer value; delete removes; sharded get-all returns all shards of one connector; `UpdatedAtUtc` equals the fake `TimeProvider` time.
2. `tests/Encina.UnitTests/Cdc/CdcPositionStoreRegistrationTests.cs`: `RemoveInMemoryDefault` keeps an application's own registration and removes only the in-memory type.
3. A fake codec `TestCdcPosition` in `Encina.TestInfrastructure` so the base needs no connector package.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT: Eight store implementations will implement ICdcPositionStore and IShardedCdcPositionStore. They must behave
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
3. `AddEncinaCdcPositionStoreADO` extension per package: `RemoveInMemoryDefault`, then `TryAddScoped<ICdcPositionStore>` and `<IShardedCdcPositionStore>` (scoped, not singleton: the processor resolves the store inside `CreateScope()` and the store takes a scoped connection; decision T1), `TryAddSingleton(TimeProvider.System)`.
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
3. `AddEncinaCdcPositionStoreDapper` extension with `RemoveInMemoryDefault` and `TryAddScoped` (decision T1).
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

1. EF Core: entity `CdcPositionEntity`, `CdcPositionEntityConfiguration` (`ToTable("CdcPositions")`, composite key, `IsConcurrencyToken` on `Version`), `CdcPositionStoreEF<TDbContext>` using `SaveChangesAsync(ct)` and mapping `DbUpdateConcurrencyException` to a `Left`; `AddEncinaCdcPositionStoreEntityFrameworkCore<TDbContext>` registers it `TryAddScoped` (a singleton would capture the scoped `DbContext`).
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

1. `CdcProcessor.cs` (save at lines 161-164, inside the per-event loop after `successCount++` at line 156) and `ShardedCdcProcessor.cs` (line 135): read the `Either` from the save; on `Left` log `CdcPositionStoreLog.SaveFailed` with the error code (never the message) and apply Design Choice 9 (recommended: stop the batch; this needs a new branch next to the success counting). Merge order with #2343, which edits the same method, is settled with the maintainer before PR 4 opens.
2. `src/Encina.Cdc/Diagnostics/CdcPositionStoreMetrics.cs` (meter `Encina.Cdc.PositionStore`): counters `encina.cdc.position.saves` and `encina.cdc.position.save_errors` (tag `store`), histogram `encina.cdc.position.lag_seconds` (now minus `ChangeMetadata.CapturedAtUtc`, from `TimeProvider`).
3. `CdcPositionStoreActivitySource` (`Encina.Cdc.PositionStore`) spans around get and save, tag `encina.cdc.position.store`.
4. `Encina.OpenTelemetry`: subscribe the new meter and source in `WithEncina()`.
5. `CdcHealthCheck` already calls `GetPositionAsync` (`src/Encina.Cdc/Health/CdcHealthCheck.cs`); add a `Degraded` result when no save succeeded within `CdcOptions.PositionMaxAge` (new option, default 5 minutes, validated), measured as in Design Choice 10 so an idle stream with no changes is not reported degraded.

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
2. Guard: null and empty `connectorId`, null position on every public method of the eight store implementations.
3. Contract: the Phase 2 base run against all 10 providers (provider matrix below).
4. Property: for any sequence of increasing positions the stored value equals the maximum, for the codecs with a total order only (SQL Server, PostgreSQL; Design Choice 8); round-trip `ToBytes`/codec for the five position types.
5. Integration: 10 providers on shared `[Collection]` fixtures; the restart test.
6. DI: build the provider with `ValidateOnBuild` and `ValidateScopes`, registering the store before and after `AddEncinaCdc` and with an application-owned store; the store must win in both orders and the application's must survive.
7. Load and benchmark: the processor saves after every successfully dispatched event (`CdcProcessor.cs:156-164`), so the store sees one read plus one `UPDATE` per event, not per batch. A justification file is therefore valid only for the load test (a single row per `(ConnectorId, ShardId)` has no cross-row contention; contention is between processors, which #1251 owns); the benchmark is NOT skipped: add a BenchmarkDotNet class for the save path of the ADO.NET SQL Server store (PR 14), because per-event round trips make this a hot path. Batching the save (one save per batch) is a possible later optimization and would be a Dilemma of its own.

#### Provider matrix for the contract and integration classes

| # | Family | Database | Contract / integration class | Collection fixture |
|---|--------|----------|------------------------------|--------------------|
| 1 | ADO.NET | SQL Server | `CdcPositionStoreAdoSqlServerTests` | `ADO-SqlServer` |
| 2 | ADO.NET | PostgreSQL | `CdcPositionStoreAdoPostgreSqlTests` | `ADO-PostgreSQL` |
| 3 | ADO.NET | MySQL | `CdcPositionStoreAdoMySqlTests` | `ADO-MySQL` |
| 4 | Dapper | SQL Server | `CdcPositionStoreDapperSqlServerTests` | `Dapper-SqlServer` |
| 5 | Dapper | PostgreSQL | `CdcPositionStoreDapperPostgreSqlTests` | `Dapper-PostgreSQL` |
| 6 | Dapper | MySQL | `CdcPositionStoreDapperMySqlTests` | `Dapper-MySQL` |
| 7 | EF Core | SQL Server | `CdcPositionStoreEfSqlServerTests` | `EFCore-SqlServer` |
| 8 | EF Core | PostgreSQL | `CdcPositionStoreEfPostgreSqlTests` | `EFCore-PostgreSQL` |
| 9 | EF Core | MySQL | `CdcPositionStoreEfMySqlTests` | `EFCore-MySQL` |
| 10 | MongoDB | MongoDB | `CdcPositionStoreMongoDbTests` | the MongoDB collection fixture |

The 10 rows map to 8 store implementations (ADO x3, Dapper x3, EF x1, MongoDB x1).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
CONTEXT: Coverage is per flag (AGENTS.md section 9); each flag reaches its own target from the package manifests.
TASK: Write the unit, guard, contract, property and integration tests plus the DI proof, run each flag's
project with --collect "XPlat Code Coverage", and compare with the manifest targets.
KEY RULES: tests execute real package code; Shouldly via Encina.Testing.Shouldly; shared [Collection]
fixtures for databases; TimeProvider fake; a justification .md only for the load test (the benchmark is implemented).
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
| `Encina.Cdc` (existing range 4800-4899, used 4800-4853) | 4854-4859 | `CdcPositionStoreLog`: save failed, get failed, codec missing, regression rejected, version conflict retried, position saved; six ids for six events, packed sequentially. The CDC family block in the batch registry is 4854-4891 (this plan 4854-4859, then 4860-4863, 4870-4875 and 4880-4885 for the sibling plans); 4892-4899 stay free. The registry's separate `cdc-1` block 7100-7149 is not used here: the ids stay inside the existing `Encina.Cdc` range, which needs no new `EventIdRanges.cs` entry |

### Per-file coverage targets (AGENTS.md section 9)

Targets are per flag (U unit, G guard, C contract, P property, I integration) from the package manifests under `.github/coverage-manifest/`; they are filled into the manifests by the PR that creates each file and are not lowered afterwards. A 0 carries its reason.

| File (new) | U | G | C | P | I | Justification |
|------------|---|---|---|---|---|---------------|
| `Encina.Cdc/Abstractions/ICdcPositionCodec.cs` | 0 | 0 | 0 | 0 | 0 | Interface only, no executable lines |
| `Encina.Cdc/CdcPositionStoreRegistration.cs` | 90 | 90 | 0 | 0 | 0 | Pure descriptor logic; every branch (own registration kept, in-memory removed, order) is unit-testable |
| `Encina.Cdc/Errors/CdcErrors.cs` (added factories) | 90 | 0 | 0 | 0 | 0 | Trivial factories, fully reached by the store tests |
| `Encina.Cdc/Processing/CdcPositionStoreLog.cs` | 70 | 0 | 0 | 0 | 0 | Source-generated `[LoggerMessage]` methods reached by the processor and store tests |
| Five `*CdcPositionCodec.cs` (connector packages) | 95 | 90 | 80 | 90 | 0 | One-line delegation to `FromBytes`; round-trip property per position type |
| `Encina.Cdc/Diagnostics/CdcPositionStoreMetrics.cs` and activity source | 90 | 0 | 0 | 0 | 0 | Counters and spans are asserted with a meter listener |
| `Encina.Cdc/Health/CdcHealthCheck.cs` (changed lines) | 90 | 0 | 0 | 0 | 0 | Three states (healthy, degraded, unhealthy) are all reachable with a fake `TimeProvider` |
| `CdcProcessor.cs`, `ShardedCdcProcessor.cs` (changed lines) | 90 | 0 | 0 | 0 | 0 | Save success and save failure branches are deterministic with a fake store |
| Each of the 8 store classes | 80 | 90 | 90 | 80 (ordered codecs) | 90 | Unit with a faked connection where the provider allows, integration against the real database covers the SQL; contract base is shared |
| Each of the 8 DI extensions | 90 | 0 | 0 | 0 | 80 | Registration paths are small; the DI proof builds the provider |
| EF entity, configuration, Mongo document | 0 | 0 | 0 | 0 | 80 | Mapping classes exercised only through a real context or collection |

CRAP is kept at or below 10 on every changed method: the store save method has two branches (update, then insert on no row) plus the guard, and the retry is a small helper to stay under the limit.

### Estimated File Count

The per-PR counts are in "Split into pull requests"; this table groups the same files by category.

| Category | Files | Notes |
|----------|-------|-------|
| Core (`Encina.Cdc`) | 8 | codec, helper, error codes (2 files), log, metrics, activity source, options/validator |
| Connector codecs | 10 | five codec classes plus five registration edits |
| Provider stores | 10 | one store class per package; EF adds entity and configuration; MongoDB adds a document |
| Scripts | 6 | ADO.NET and Dapper packages |
| DI extensions | 8 | one edit per package |
| PublicAPI and manifests | ~20 | one `PublicAPI.Unshipped.txt` and one manifest edit where the package gains a public symbol or a new file |
| Tests | ~50 | registration, codec, processor, metrics, health, contract base, fake, 8 unit, 8 guard, 10 integration, DI proof, property, benchmark and load justification |
| Docs, changelog | ~14 | nine READMEs, feature page, fragment, inventory, ADR |

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
flag; Phase 9 documents. Deliver as the 15 pull requests, in the order given in the Summary.

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
| 8 | Transactions | ⏭️ | Defer or Integrate, depending on Design Choice 5: with option A (recommended) the save is one atomic statement after dispatch and the row is Not applicable; with option B the store joins the handler's `IUnitOfWork` and the row becomes Integrate. The issue marks it "where the provider allows", so it is not closed until the maintainer answers |
| 9 | Idempotency | ⏭️ | At-least-once replay after a crash between handler and save; handler idempotency is #1220 |
| 10 | Multi-Tenancy | ⏭️ | The connector id may carry the tenant; tenant-scoped positions follow #1257 (position rows hold no personal data). Consequence for this schema: no `TenantId` column in `CdcPositions` (the issue says "TenantId column when tenant-scoped"); adding one later is a new script |
| 11 | Module Isolation | ❌ | Not applicable: infrastructure-level state |
| 12 | Audit Trail | ❌ | Not applicable: operational checkpoint, not an audit event |

---

## Prerequisites & Dependencies

| Item | State | Effect |
|------|-------|--------|
| #2343 (position advances past failed events) | open | Same method in `CdcProcessor` (the save sits in the success branch at lines 154-171); agree the merge order before PR 4, which stays minimal; Design Choice 9 depends on the overlap |
| #2382 (startup requirements validator) | open | Consumes `CdcPositionStoreRegistration.IsInMemoryDefault` |
| #2397 (MySQL restored position never applied, Debezium never commits) | open | Persistence helps only once the connectors apply what they read; not a blocker for the stores |
| #583 plan (persistent dead letter store) | plan | Same package set; the script numbers must be taken at implementation time |

---

## Next Steps

1. Review and approve this plan; the maintainer answers the Dilemmas (choices 1, 3, 5, 7, 8, 9 and 10) when work on the issue starts; none is decided here.
2. Publish the plan link as a comment on #2393.
3. Begin PR 1 (the core contract, Phase 1).
4. Each phase is a self-contained commit; the final commit references `Fixes #2393`.
