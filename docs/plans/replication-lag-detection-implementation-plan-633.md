# Implementation Plan: Replication Lag Detection for Read/Write Routing

> **Issue**: [#633](https://github.com/dlrivada/Encina/issues/633) (`area-cdc`, `priority-medium`, `p1-recommended`)
> **Type**: Feature
> **Complexity**: Medium-High (one new abstraction, a selector change in `Encina.Messaging`, a probe per database family)
> **Estimated Scope**: ~1,100-1,500 lines of production code + ~1,800-2,400 lines of tests, delivered in five pull requests
> **Related plans of the same family (#634, #635, #636, #2393)**: independent of this plan; none of them shares a type with it. Sibling plans are referred to by issue number only.

---

## Summary

A query routed to a read replica can return stale data when that replica is behind the primary. Encina gets a replication lag monitor that measures the lag of each configured read replica in the background, feeds the existing replica health state, and lets the read/write connection selector skip a replica whose lag exceeds a configured threshold and fall back to the primary.

### What the code does today (2026-11-11, `main` at `942c0f124`)

1. **The eight `ReadWriteRoutingPipelineBehavior` classes do not choose a replica.** They only open a `DatabaseRoutingScope(intent)` with `DatabaseIntent.Read`, `Write` or `ForceWrite` (`src/Encina.ADO.SqlServer/ReadWriteSeparation/ReadWriteRoutingPipelineBehavior.cs:88-93`; identical 157-line files exist in `Encina.ADO.PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore` and `Encina.MongoDB`). The issue sketch ("the behavior asks the monitor before sending a query to a replica") describes the wrong place.
2. **The replica is chosen in `ReadWriteConnectionSelector`.** `GetReadConnectionString()` returns `_replicaSelector.SelectReplica()` when replicas are configured and falls back to the write connection string otherwise (`src/Encina.Messaging/ReadWriteSeparation/ReadWriteConnectionSelector.cs:134-139`). It is synchronous (`Either<EncinaError, string> GetReadConnectionString()`), so it cannot probe a replica on the query path. `IReplicaSelector.SelectReplica()` returns a bare `string` and throws `InvalidOperationException` when none is available (`IReplicaSelector.cs:30`). ADO.NET, Dapper and EF Core (`ReadWriteDbContextFactory`) all consume this selector, so one change covers nine of the ten providers.
3. **MongoDB is already staleness-aware.** `MongoReadWriteSeparationOptions.MaxStaleness` is applied to the driver read preference (`ReadWriteMongoCollectionFactory.cs:80,176-178`), so the driver excludes a stale secondary itself.
4. **The sharded path has half of the machinery.** `IReplicaHealthTracker.ReportReplicationLag(shardId, replicaConnectionString, lag)` and `GetAvailableReplicas(shardId, replicas, maxAcceptableLag)` exist (`src/Encina/Sharding/ReplicaSelection/IReplicaHealthTracker.cs:94`), `ShardReplicaHealthCheck` consumes the lag (`ShardReplicaHealthCheck.cs:102`) and `StalenessOptions` / `ShardedReadWriteOptions.MaxAcceptableReplicationLag` hold the threshold, but no code calls `ReportReplicationLag`: nothing produces a lag value. The sharded connection factories also call `GetAvailableReplicas` without the lag argument (`src/Encina.ADO.SqlServer/Sharding/ShardedReadWriteConnectionFactory.cs:233`).
5. **A tracker with no reading assumes the replica is fine.** Without recorded lag, `GetAvailableReplicas(..., maxAcceptableLag)` treats a replica as within the threshold (`ReplicaHealthTracker.cs:212,223`). The issue's acceptance criterion is the opposite for the new path: unknown lag routes to the primary.
6. **CDC cannot measure replica lag alone.** `CdcPosition` supports ordering (`src/Encina.Cdc/Abstractions/CdcPosition.cs:46`) but a CDC connector reads the log of the primary; the replica's applied position is not a CDC concept. `Encina.Cdc` also depends on `Encina.Messaging`, not the other way round, so a Messaging selector cannot reference a CDC type.

### Scope

- **Implements**: #633 for the nine relational providers through `ReadWriteConnectionSelector`, plus the lag producer that the sharded health check already expects. MongoDB keeps the driver's `MaxStaleness`; this plan adds its startup validation and metrics only.
- **Affected packages**: `Encina.Messaging` (contracts, options, selector, probe service), `Encina` (tracker reuse only), `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore` (probes and registration), `Encina.MongoDB` (validation), `Encina.OpenTelemetry` (subscription).
- **Provider category**: Database, all 10 providers (AGENTS.md section 5). SQLite and Oracle are out of the matrix.
- **Corrections to the issue text**: the routing behaviors are not changed; the replica list comes from `ReadWriteSeparationOptions.ReadConnectionStrings` (a second `Replicas` dictionary with connection strings would duplicate the configuration and spread secrets); metric names follow the final package (Design Choice 2).
- **Split into pull requests**: PR 1 = Phases 1-3 (Messaging core, about 14 files); PR 2 = Phase 4 (ADO.NET, about 9 files); PR 3 = Phase 5 (Dapper, about 9 files); PR 4 = Phase 6 (EF Core and MongoDB, about 7 files); PR 5 = Phases 7-9 remainder (OpenTelemetry, integration tests, documentation).

---

## Design Choices

<details>
<summary><strong>1. How lag is measured</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Heartbeat row**: a hosted service writes a timestamp row on the primary every interval; each replica is read back; lag is the age of the last heartbeat visible there | Same SQL idea on every relational database (pt-heartbeat approach); no special privilege on the replica | Needs a table, a write privilege and a write per interval on the primary; precision equals the interval; no MongoDB equivalent without a collection |
| **B) Native lag query on the replica** (PostgreSQL `pg_last_xact_replay_timestamp()` with a receive/replay LSN check, MySQL `SHOW REPLICA STATUS` `Seconds_Behind_Source`, SQL Server `sys.dm_hadr_database_replica_states.secondary_lag_seconds`) | Read-only, no schema change, precise, what proxies and poolers use; no write on the primary | One SQL per database (three, reused by ADO.NET, Dapper and EF Core); needs a privilege (`REPLICATION CLIENT`, `VIEW SERVER STATE`); SQL Server only answers on an availability group secondary |
| **C) CDC position comparison** (the issue's idea) | Reuses `CdcPosition.CompareTo` | The replica's applied position is not exposed through CDC; a per-provider replica read is needed anyway, plus a CDC connector on the primary; positions give no time lag without a time mapping |

### Chosen Option: **B — native lag query on the replica** (recommended, pending the maintainer)

### Rationale

B wins when operators accept a monitoring privilege and want an exact, read-only measurement; A wins when the application owner can create one small table and the replicas are of a kind that exposes no lag query; C wins only where a CDC connector already runs against the primary and positions map to time. The probe interface (Design Choice 3) lets A be added later as another implementation without changing the selector. The original preference for CDC-based detection (provider-agnostic, real-time, reuses infrastructure) is option C's argument and is not adopted by this recommendation.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the monitor abstraction lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Cdc`** (the issue) | Matches the issue and the CDC area label | The eight routing packages would reference `Encina.Cdc`, which references `Encina.Caching` and `Encina.Messaging`; the selector in `Encina.Messaging` could not use it (the dependency points the other way) |
| **B) `Encina` core**, next to `IReplicaHealthTracker` | The tracker and the monitor share a package | Core grows with a feature only the relational packages use; the non-sharded selector is in `Encina.Messaging`, not core |
| **C) `Encina.Messaging.ReadWriteSeparation`**, next to `ReadWriteConnectionSelector` and `IReplicaSelector` | The selector, options and monitor sit together; every relational provider already references `Encina.Messaging`; no CDC dependency | The area label `area-cdc` no longer describes the package; metric names change from `encina.cdc.*` |

### Chosen Option: **C — `Encina.Messaging.ReadWriteSeparation`** (recommended, pending the maintainer)

### Rationale

The consumer is the selector, which lives in `Encina.Messaging`. A would force a dependency cycle or a duplicate interface. B wins if the sharded and non-sharded selectors are to share one monitor type; the plan reaches the shared state through `IReplicaHealthTracker` (core) instead, so B is not needed.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Query-path decision — an in-memory reading, never a probe</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A background `ReplicationLagProbeService` writes readings into `IReplicaHealthTracker`; the selector only reads them** | No I/O on the query path; the synchronous selector contract stays; shared with the sharded health check | A reading is up to one interval old |
| **B) Probe the chosen replica before every query** | Freshest reading | A round trip per query; the selector is synchronous and AGENTS.md forbids synchronous database calls |
| **C) Probe lazily and cache for the interval** | Fewer probes than B | The first query after an expiry pays the round trip and blocks the request thread |

### Chosen Option: **A — background probes, in-memory reads**

### Rationale

`IReadWriteConnectionSelector.GetReadConnectionString()` is synchronous, and database calls must be asynchronous with a `CancellationToken`; a probe on the request path would break one rule or the other.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (database calls are asynchronous with a `CancellationToken`)

</details>

<details>
<summary><strong>4. Unknown or stale lag routes to the primary</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A replica with no reading, a failed probe or a reading older than three intervals is not eligible; with `FallbackToPrimary` the query goes to the primary, otherwise it returns a `Left`** | Matches the acceptance criterion; no stale read after a monitor failure; the failure is counted and logged by error code | For one interval after startup every query uses the primary |
| **B) Treat "no reading" as healthy** (what `GetAvailableReplicas(..., maxAcceptableLag)` does today) | No startup warm-up | A monitor that is down is indistinguishable from "no lag" |

### Chosen Option: **A — unknown is not eligible**

### Rationale

The issue states that unknown lag never routes to a replica; the sharded tracker keeps its lenient rule for its own callers and the new gate does not reuse it. The behavior applies only when lag detection is enabled.

**Class:** Settled
**Settled by:** #633 (acceptance criteria: unknown lag routes to the primary)

</details>

<details>
<summary><strong>5. Replica identity and secrets</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Replicas come from `ReadWriteSeparationOptions.ReadConnectionStrings`; the id is `replica-<index>`; connection strings never reach logs, tags or metrics** | One configuration; no second secret holder; ids are safe to log | The id depends on list order |
| **B) A separate `Replicas` dictionary of id to connection string in the lag options (the issue sketch)** | Stable names | Duplicates the connection strings; a second options class that must be `[JsonIgnore]` with a `ToString()` override |

### Chosen Option: **A — reuse `ReadConnectionStrings`**

### Rationale

`ReadWriteSeparationOptions` already carries the strings and overrides `ToString()`. The tracker is keyed by connection string internally (`MarkHealthy(shardId, replicaConnectionString)`), which stays inside the process; everything observable uses the id.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (options with connection strings are `[JsonIgnore]` with a `ToString()` override, #851)

</details>

<details>
<summary><strong>6. Fallback strategy among replicas</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `LagAwareReplicaSelector` decorates the configured strategy: it asks the inner selector up to N times (N = replica count) for an eligible replica, then returns "none"; the connection selector turns "none" into the primary or a `Left`** | The configured strategy (round robin, random, least connections) is kept; works with every existing `IReplicaSelector` | Up to N selector calls per query when most replicas are stale |
| **B) Filter first, then run the strategy on the filtered list** | Exactly one selection | `IReplicaSelector` has no list parameter; every strategy would need a new signature |

### Chosen Option: **A — decorator over `IReplicaSelector`**

### Rationale

Pre-1.0 allows a new signature, but the three strategies hold private lists and rotate state; a decorator keeps their behavior and adds the lag rule in one class. Selection bias towards the first eligible replica under heavy staleness is acceptable because the fallback is rare.

**Class:** Trivial
**Settled by:** AGENTS.md section 1 (pay for what you use: the decorator is registered only when lag detection is enabled)

</details>

---

## Implementation Phases

### Phase 1: Contracts, options and error codes (`Encina.Messaging`)

> **Goal**: define the monitor, the probe and the options without behavior.

<details>
<summary><strong>Tasks</strong></summary>

1. `ReadWriteSeparation/IReplicationLagMonitor.cs`: `Task<Either<EncinaError, ReplicationLagInfo>> GetLagAsync(string replicaId, CancellationToken)` and `Task<Either<EncinaError, bool>> IsReplicaHealthyAsync(string replicaId, CancellationToken)`; ROP returns replace the issue's bare tasks.
2. `ReplicationLagInfo` record: `ReplicaId`, `EstimatedLag` (`TimeSpan`), `MeasuredAtUtc` (from `TimeProvider`); the issue's `PrimaryPosition` / `ReplicaPosition` properties are dropped (they exist only under CDC measurement, option C).
3. `IReplicationLagProbe`: `Task<Either<EncinaError, TimeSpan>> MeasureAsync(string replicaConnectionString, CancellationToken)`, implemented per database in the provider packages.
4. `ReplicationLagOptions` (nested in `ReadWriteSeparationOptions.ReplicationLag`): `MaxAcceptableLag` (default 5 s), `FallbackToPrimary` (default true), `CheckInterval` (default 10 s), `ProbeTimeout` (default 3 s), `Enabled` (default false); `IValidateOptions` rejects non-positive values and `ProbeTimeout >= CheckInterval`.
5. `ReplicationLagErrorCodes` (`encina.readwrite.lag_unknown`, `lag_probe_failed`, `lag_exceeded`).
6. PublicAPI lines.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT: ReadWriteConnectionSelector (src/Encina.Messaging/ReadWriteSeparation) picks the read connection
string synchronously. Lag detection is opt-in and disabled by default (AGENTS.md section 1).
TASK: Add IReplicationLagMonitor, ReplicationLagInfo, IReplicationLagProbe, ReplicationLagOptions with its
validator, the error codes and the PublicAPI lines. Do not change the selector yet.
KEY RULES: Either<EncinaError, T> returns; AtUtc suffix; TimeProvider; XML docs; the options never print
connection strings; no [Obsolete].
REFERENCE FILES: src/Encina.Messaging/ReadWriteSeparation/ReadWriteSeparationOptions.cs, IReplicaSelector.cs,
src/Encina/Sharding/ReplicaSelection/IReplicaHealthTracker.cs.
```

</details>

### Phase 2: Lag gate and selector integration

> **Goal**: the selector skips stale or unknown replicas.

<details>
<summary><strong>Tasks</strong></summary>

1. `LagAwareReplicaSelector : IReplicaSelector`: wraps the strategy selector; consults the tracker state for the id and applies Design Choice 4 (reading age limit = 3 x `CheckInterval`, `TimeProvider`).
2. `ReadWriteConnectionSelector.GetReadConnectionString()`: when lag detection is on and no replica is eligible, return the write connection string (`FallbackToPrimary`) or `Left(lag_exceeded)`; catch the "no replica" signal of the decorator instead of letting `InvalidOperationException` escape.
3. Registration in `ReadWriteSeparationServiceCollectionExtensions` (or the existing registration path in each provider package, `RegisterReadWriteSeparation`): `TryAddSingleton<IReplicaHealthTracker>` for the non-sharded path with the constant scope id `"readwrite"`.
4. A `DatabaseIntent.ForceWrite` or `Write` request never reaches the lag gate.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT: Phase 1 added the contracts. IReplicaHealthTracker already stores ObservedReplicationLag and
LagObservedAtUtc per (shard, replica).
TASK: Implement LagAwareReplicaSelector and change ReadWriteConnectionSelector so a stale, unknown or
unprobed replica is never returned; fall back to the primary or a Left according to the options.
KEY RULES: unknown lag is not eligible; no I/O in the selector; thread-safe (called concurrently); the
replica id, never the connection string, in anything observable; disabled option means the code path is
byte-for-byte today's.
REFERENCE FILES: src/Encina.Messaging/ReadWriteSeparation/ReadWriteConnectionSelector.cs,
src/Encina.Messaging/ReadWriteSeparation/RoundRobinReplicaSelector.cs,
src/Encina/Sharding/ReplicaSelection/ReplicaHealthTracker.cs.
```

</details>

### Phase 3: Probe service and monitor

> **Goal**: measure lag in the background and publish it.

<details>
<summary><strong>Tasks</strong></summary>

1. `ReplicationLagMonitor : IReplicationLagMonitor` reads the tracker state; `IsReplicaHealthyAsync` applies the same rule as the gate.
2. `ReplicationLagProbeService : BackgroundService` with a `PeriodicTimer` created through `TimeProvider`: per iteration it probes each replica with `ProbeTimeout`, calls `ReportReplicationLag` on success or `MarkUnhealthy` on failure; the iteration body is inside `try/catch` (an `OperationCanceledException` from the stopping token ends the loop; every other exception is logged by error code through `ForLogging()` and the loop continues).
3. `ReplicationLagLog` (`[LoggerMessage]`, EventIds 7100-7109, range `ReplicationLag` registered in `EventIdRanges.cs` and mapped to `Encina.Messaging` in `EncinaEventIdAllocationTests`).
4. `AddEncinaReplicationLagDetection` registration: options, validator, tracker, monitor, decorator and hosted service; the DI test proves `ValidateOnBuild` and `ValidateScopes`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT: Lag readings must reach IReplicaHealthTracker without any request waiting for a probe.
TASK: Implement ReplicationLagMonitor, ReplicationLagProbeService, ReplicationLagLog and the registration.
KEY RULES: the loop catches per iteration and never lets an exception escape (AGENTS.md section 3, #2774);
OperationCanceledException from the stopping token ends the loop; log error codes and ForLogging() only,
never EncinaError.Message or connection strings; EventIds 7100-7109 registered first (ADR-021); TimeProvider
for the timer and for MeasuredAtUtc; every option type and dependency the hosted service resolves is registered.
REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxProcessorBase.cs, src/Encina/Diagnostics/EventIdRanges.cs,
tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs.
```

</details>

### Phase 4: ADO.NET probes (SQL Server, PostgreSQL, MySQL)

> **Goal**: one `IReplicationLagProbe` per database in the ADO.NET packages.

<details>
<summary><strong>Tasks</strong></summary>

1. `SqlServerReplicationLagProbe`: reads `secondary_lag_seconds` for the local database from `sys.dm_hadr_database_replica_states` (`is_local = 1`); no row (a standalone instance) is an unknown lag, never zero.
2. `PostgreSqlReplicationLagProbe`: `pg_is_in_recovery()`; lag is zero when `pg_last_wal_receive_lsn()` equals `pg_last_wal_replay_lsn()` (an idle primary is not lag), otherwise `now() - pg_last_xact_replay_timestamp()`.
3. `MySqlReplicationLagProbe`: `SHOW REPLICA STATUS` and `Seconds_Behind_Source`; `NULL` (replication stopped) is an unknown lag.
4. Calls use `OpenAsync(ct)`, `ExecuteReaderAsync(ct)` and the `ProbeTimeout` linked token; no synchronous overloads; the provider message never enters the `EncinaError` (code only).
5. Registration in each package's existing read/write registration (`TryAddEnumerable` of the probe).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT: The probe runs on the replica connection and returns the lag as a TimeSpan or a Left (unknown).
TASK: Implement the three ADO.NET probes and register them; keep the SQL in one const per class.
KEY RULES: async only; parameters not needed but never concatenate identifiers from options; unknown is a Left,
never zero; errors carry codes, not provider messages; document the required privilege in the XML remarks
(REPLICATION CLIENT, VIEW SERVER STATE).
REFERENCE FILES: src/Encina.ADO.SqlServer/ReadWriteSeparation/ReadWriteConnectionFactory.cs,
src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs (RegisterReadWriteSeparation), the PostgreSQL and MySQL equivalents.
```

</details>

### Phase 5: Dapper probes (SQL Server, PostgreSQL, MySQL)

> **Goal**: the same three probes in the Dapper packages.

<details>
<summary><strong>Tasks</strong></summary>

1. `SqlServerReplicationLagProbe`, `PostgreSqlReplicationLagProbe`, `MySqlReplicationLagProbe` in `src/Encina.Dapper.<Provider>/ReadWriteSeparation/` with `QueryFirstOrDefaultAsync` through `CommandDefinition` carrying the cancellation token.
2. Registration with `TryAddEnumerable`.
3. Same unknown-is-a-`Left` rule.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT: Phase 4 defined the SQL per database.
TASK: Port the three probes to the Dapper packages with the same behavior and register them.
KEY RULES: CommandDefinition with the CancellationToken; same SQL text as Phase 4 (a contract test compares
behavior, not text); no synchronous overloads.
REFERENCE FILES: src/Encina.Dapper.SqlServer/ReadWriteSeparation/ReadWriteConnectionFactory.cs, src/Encina.Dapper.PostgreSQL/ServiceCollectionExtensions.cs.
```

</details>

### Phase 6: EF Core probes and MongoDB validation

> **Goal**: cover the tenth provider and the EF Core package that serves three databases.

<details>
<summary><strong>Tasks</strong></summary>

1. EF Core: `EfCoreReplicationLagProbe` chooses the SQL by `DbContext.Database.ProviderName` (SQL Server, Npgsql, MySQL) and runs it on a short-lived connection built from the replica connection string with `DbConnection.OpenAsync(ct)`.
2. MongoDB: `MongoReadWriteSeparationOptions.MaxStaleness` is validated at startup against the 90-second minimum of the MongoDB server selection specification; no probe is added, the driver enforces the threshold. Metrics record the configured staleness.
3. Registration in `AddEncinaEntityFrameworkCore` and `AddEncinaMongoDB` paths.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT: EF Core reaches the replicas through ReadWriteDbContextFactory and the same selector; MongoDB reaches
them through the driver read preference with MaxStaleness.
TASK: Add the EF Core probe and the MongoDB option validation.
KEY RULES: the EF probe must not create a DbContext per probe with the application's interceptors; use a raw
DbConnection; MongoDB code adds no polling.
REFERENCE FILES: src/Encina.EntityFrameworkCore/ReadWriteSeparation/ReadWriteDbContextFactory.cs,
src/Encina.MongoDB/ReadWriteSeparation/MongoReadWriteSeparationOptions.cs.
```

</details>

### Phase 7: Observability and health

> **Goal**: lag, fallbacks and replica health are visible.

<details>
<summary><strong>Tasks</strong></summary>

1. `ReplicationLagMetrics` (meter `Encina.Messaging.ReadWriteSeparation`): histogram `encina.readwrite.replica.lag_seconds` (tag `replica.id`), counter `encina.readwrite.replica.fallbacks` (tag `reason`: `stale`, `unknown`), observable gauge `encina.readwrite.replica.healthy` (0 or 1 per replica).
2. `ReplicationLagActivitySource` (`Encina.Messaging.ReadWriteSeparation`): span `encina.readwrite.replica.probe` with `replica.id` and the error code on failure.
3. `ReplicationLagHealthCheck : EncinaHealthCheck` (`DefaultName`, static `Tags`): degraded when some replica is stale, unhealthy when all are and `FallbackToPrimary` is off.
4. `Encina.OpenTelemetry` `WithEncina()` subscribes the new meter and source.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT: Operators need to see why a query used the primary.
TASK: Add the metrics, the probe span, the health check and the OpenTelemetry subscription.
KEY RULES: tags carry the replica id and a reason, never a connection string or message text; scoped
resolution in the health check through IServiceProvider.CreateScope(); DefaultName const and static Tags.
REFERENCE FILES: src/Encina.Messaging/Health/DeadLetterHealthCheck.cs, src/Encina.Messaging/Diagnostics/DeadLetterMetrics.cs,
src/Encina.OpenTelemetry (WithEncina registration).
```

</details>

### Phase 8: Testing

> **Goal**: every applicable flag reaches its manifest target.

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: gate decisions with a fake `TimeProvider` (fresh, stale, unknown, boundary), the decorator, the probe service (a failing probe never ends the loop, `FakeTimeProvider.Advance` never throws), options validation.
2. Guard: all public methods.
3. Contract: `IReplicationLagProbe` behavior across the nine relational implementations (unknown is a `Left`) and the routing outcome in the eight routing packages (behaviors unchanged, selector shared).
4. Property: lag arithmetic and threshold boundaries for any `MaxAcceptableLag` and reading age.
5. Integration: PostgreSQL and MySQL with a primary and a streaming replica on Testcontainers in a dedicated shared fixture; a standalone SQL Server container proves the unknown path (no availability group in a single container). A real availability group probe is not reproducible with one container; this is recorded in the PR as the coverage limit of the SQL Server probe, not as a `.md` justification.
6. Benchmark: overhead of `GetReadConnectionString()` with the gate (hot path). Load: justification file (reads only an in-memory value).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
CONTEXT: Coverage is per flag (AGENTS.md section 9).
TASK: Write the tests above and compare each flag with the package manifest targets.
KEY RULES: tests execute real code; Shouldly via Encina.Testing.Shouldly; shared [Collection] fixtures for
databases; no Thread.Sleep; a test never swallows an exception thrown by FakeTimeProvider.Advance.
REFERENCE FILES: .github/coverage-manifest/Encina.Messaging.json, docs/en/guides/TESTING.md, docs/testing/integration-tests.md.
```

</details>

### Phase 9: Documentation and finalization

> **Goal**: users can enable and read lag detection.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on all new public APIs.
2. `changelog.d/633-replication-lag-detection.added.md`.
3. `src/Encina.Messaging/README.md` and the provider READMEs; a read/write separation page that states the privileges per database, the unknown-is-primary rule, the MongoDB `MaxStaleness` minimum and the availability-group requirement of the SQL Server probe.
4. ADR number assigned when the ADR pull request opens (measurement approach).
5. `PublicAPI.Unshipped.txt` lines; `docs/INVENTORY.md`.
6. Verification: build with zero warnings, tests, per-flag coverage of the new files against the manifest.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
CONTEXT: The feature is code-complete after Phase 8.
TASK: Documentation, changelog fragment, PublicAPI lines, verification.
KEY RULES: never type coverage figures by hand; English only; no emojis; follow the encina-docs skill; never
edit CHANGELOG.md.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, changelog.d/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| MongoDB server selection specification (`maxStalenessSeconds`) | Minimum of 90 seconds for `MaxStaleness`; validated at startup |
| PostgreSQL `pg_last_xact_replay_timestamp()`, `pg_last_wal_receive_lsn()`, `pg_last_wal_replay_lsn()` | Replay lag on a streaming replica; the LSN comparison avoids reporting an idle primary as lag |
| MySQL `SHOW REPLICA STATUS` (`Seconds_Behind_Source`) | Replica lag; the older `SHOW SLAVE STATUS` spelling is removed in recent versions |
| SQL Server `sys.dm_hadr_database_replica_states` | Availability group secondary lag |
| AGENTS.md section 3 | Background loops never let an exception escape; secrets in options; asynchronous database calls |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `ReadWriteConnectionSelector`, `IReplicaSelector`, three strategies | `src/Encina.Messaging/ReadWriteSeparation` | Integration point and decorator target |
| `IReplicaHealthTracker`, `ReplicaHealthTracker` | `src/Encina/Sharding/ReplicaSelection` | Stores the readings (`ReportReplicationLag`) |
| `ShardReplicaHealthCheck` | `src/Encina/Sharding/Health` | Already consumes the lag; starts working once readings exist |
| `MongoReadWriteSeparationOptions.MaxStaleness` | `src/Encina.MongoDB/ReadWriteSeparation` | MongoDB threshold |
| `OutboxProcessorBase` | `src/Encina.Messaging/Outbox` | Background loop pattern |
| `EncinaHealthCheck`, `DeadLetterMetrics` | `src/Encina.Messaging/Health`, `Diagnostics` | Health and metrics patterns |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (new range `ReplicationLag`) | 7100-7109 used; 7110-7119 spare | Registered in `EventIdRanges.cs` from the family block 7100-7149; if Design Choice 2 picks `Encina.Cdc` the range maps to that assembly instead |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Messaging core | 12 | contracts, options, validator, decorator, monitor, service, log, metrics, activity source, health check, errors, registration |
| Provider probes | 7 | three ADO.NET, three Dapper, one EF Core |
| MongoDB and OpenTelemetry | 2 | validation, subscription |
| Tests | ~26 | unit, guard, contract, property, integration fixture and classes, benchmark |
| Docs, changelog, manifests | ~10 | READMEs, page, fragment, manifest entries |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library with nullable reference types. You are
implementing issue #633: replication lag detection for read/write routing. The routing behaviors only set
a DatabaseIntent; the replica is chosen by ReadWriteConnectionSelector in Encina.Messaging. Read AGENTS.md first.

IMPLEMENTATION OVERVIEW: Phase 1 adds the contracts and options. Phase 2 adds LagAwareReplicaSelector and
changes the connection selector. Phase 3 adds the background probe service and monitor writing readings into
IReplicaHealthTracker. Phases 4-6 add a native-lag probe per database (ADO.NET, Dapper, EF Core) and MongoDB
validation. Phase 7 adds metrics, spans, a health check and the OpenTelemetry subscription. Phase 8 tests,
Phase 9 documents. Deliver as five pull requests (see the Summary).

KEY PATTERNS: lag detection is opt-in; unknown or stale lag is not eligible and routes to the primary; no I/O
on the query path; the loop catches per iteration; secrets never in logs, tags or metrics (replica id only);
TimeProvider everywhere; EventIds 7100-7109 registered first; Either<EncinaError, T>.

REFERENCE FILES: src/Encina.Messaging/ReadWriteSeparation/ReadWriteConnectionSelector.cs,
src/Encina/Sharding/ReplicaSelection/ReplicaHealthTracker.cs, src/Encina.MongoDB/ReadWriteSeparation/ReadWriteMongoCollectionFactory.cs,
src/Encina.Messaging/Outbox/OutboxProcessorBase.cs, .github/coverage-manifest/Encina.Messaging.json.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: readings are an in-memory value in the tracker; caching them again adds nothing |
| 2 | OpenTelemetry | ✅ | Phase 7: lag histogram, fallback counter, replica health gauge, probe span |
| 3 | Structured Logging | ✅ | `ReplicationLagLog`, EventIds 7100-7109, range registered first |
| 4 | Health Checks | ✅ | Phase 7: `ReplicationLagHealthCheck` |
| 5 | Validation | ✅ | Options validated at startup (`IValidateOptions`); MongoDB `MaxStaleness` minimum |
| 6 | Resilience | ✅ | Each probe has `ProbeTimeout`; a failed probe marks the replica unhealthy and the next interval retries; no circuit breaker needed for a read-only probe |
| 7 | Distributed Locks | ❌ | Not applicable: every instance needs its own reading and probes are read-only |
| 8 | Transactions | ❌ | Not applicable: reads only |
| 9 | Idempotency | ❌ | Not applicable: a measurement is idempotent |
| 10 | Multi-Tenancy | ⏭️ | Per-tenant replica sets follow the multi-tenancy work of #1257; the scope id `"readwrite"` leaves room for a per-tenant id |
| 11 | Module Isolation | ❌ | Not applicable: infrastructure-level |
| 12 | Audit Trail | ❌ | Not applicable: operational, not an audit event |

---

## Prerequisites & Dependencies

| Item | State | Effect |
|------|-------|--------|
| Sharded connection factories ignore the lag threshold | to be tracked by an issue (draft in the delivery report) | After this plan the sharded health check sees readings; the sharded factories still call `GetAvailableReplicas` without the threshold |
| #3412 (option properties nothing reads) | open spike | `StalenessOptions` and `ShardedReadWriteOptions.MaxAcceptableReplicationLag` are candidates; this plan feeds the producer side only |
| #2396 (processor liveness health checks) | open | The probe service is another background processor to include in liveness checks |

---

## Next Steps

1. Review and approve this plan; the maintainer answers the two Dilemmas (choices 1 and 2) when work on the issue starts.
2. Publish the plan link as a comment on #633.
3. Begin PR 1 (Phases 1-3) after the Dilemmas are answered.
4. Each phase is a self-contained commit; the final commit references `Fixes #633`.
