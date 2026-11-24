# Implementation Plan: Single Active CDC Consumer per Source Across Instances (Leader Election)

> **Issue**: [#3990](https://github.com/dlrivada/Encina/issues/3990)
> **Type**: Feature (the issue carries `needs-decision`; its own Dilemma D1 is recorded below as recommended, pending the maintainer)
> **Complexity**: Medium (6 phases, no database provider code, 2 pull requests of 13 and 8 files)
> **Estimated Scope**: ~330 lines of production code + ~900 lines of tests and documentation
> **Family**: CDC and leader election. It consumes the leader election abstraction of #717 (`ILeaderElection`, an election engine over `IDistributedLockProvider`) and sits next to the CDC plans #2393 (persistent position store), #634, #635 and #636 (CDC consumers that note the duplicate problem) and #1251 (row claiming, not CDC). Siblings are referred to by issue number only.

---

## Summary

Two application instances that run the same CDC connector against the same source both stream the same change events, so every handler, audit sink and retention tracker runs twice per change. Today the only protection is the idempotency of the handlers. #3990 adds an opt-in lease, one per source, so that a single instance consumes the source at a time and a replica without the lease stays idle and keeps reporting health.

**What exists today** (`origin/main` at `278c164f1`, 2026-11-24):

- `CdcProcessor` (`src/Encina.Cdc/Processing/CdcProcessor.cs`, `BackgroundService`, internal sealed) loops `ProcessChangesAsync` then `Task.Delay(PollingInterval)`; each cycle opens a scope, resolves the scoped `ICdcConnector`, `ICdcDispatcher` and `ICdcPositionStore`, streams `connector.StreamChangesAsync`, dispatches each event and saves the position after each successful dispatch. Its constructor is `(IServiceProvider, ILogger<CdcProcessor>, CdcOptions, ICdcDeadLetterStore? = null)`. `ShardedCdcProcessor` (`Sharding/ShardedCdcProcessor.cs`) has the same loop over `IShardedCdcConnector.StreamAllShardsAsync` and `(IServiceProvider, ILogger, CdcOptions)`. `AddEncinaCdc` registers one or the other as a hosted service (`ServiceCollectionExtensions.cs:133,171`), never both.
- `ConnectorId` is a constant of each connector type, not an identity of the source: `encina-cdc-sqlserver`, `encina-cdc-postgresql`, `encina-cdc-mysql`, `encina-cdc-mongodb`, `encina-cdc-debezium`, `encina-cdc-debezium-kafka` (for example `SqlServerCdcConnector.cs:61`), and `ShardedCaptureOptions.ConnectorId` defaults to `sharded-cdc`. Two services that read two different SQL Server databases through the same package therefore report the same `ConnectorId`. A lease keyed by `ConnectorId` alone would make unrelated sources block each other (Design Choice 3).
- The position store is `InMemoryCdcPositionStore` unless a database provider overrides it (`ServiceCollectionExtensions.cs:83`, `TryAddSingleton`); the persistent store is #2393 (open, plan merged). After a failover the new leader resumes from the persisted position, so events after the last saved position are delivered again: at-least-once, with idempotent handlers as the ADR-050 CDC row requires.
- Configuration style: `CdcConfiguration` exposes fluent `With...` methods that set a flag on `CdcOptions` and keep an options object (`WithMessagingBridge` sets `UseMessagingBridge`, `WithShardedCapture` sets `UseShardedCapture`, `WithCacheInvalidation` sets `UseCacheInvalidation`); each options object is registered as a singleton instance (`services.AddSingleton(messagingOptions)`).
- Nothing of leader election exists under `src/` yet. #717 (open, plan merged) defines `ILeaderElection` (`bool IsLeader(string electionName)`, no I/O, plus snapshots) in `Encina.Messaging`, the engine `LockLeaderElection` and `LeaderElectionService` in `Encina.DistributedLock` (which references `Encina.Messaging`), the gauge and counters of the election, and a health check on the liveness of the heartbeat loop. `Encina.Cdc` references `Encina` and `Encina.Messaging` and not `Encina.DistributedLock`. The CDC row of ADR-050 already lists "row claiming, lock or leader (#1251)" among the CDC requirements.
- `EventIdRanges.Cdc` is 4800-4899 (`src/Encina/Diagnostics/EventIdRanges.cs:103`); `CdcLog.cs` uses 4800-4853 and the open plans claim 4854-4859 (#2393) and 4880-4885 with 4886-4891 held (#636), so this plan takes **4860-4863**.

**What this plan delivers**: `CdcConfiguration.WithLeaderElection(...)` (a flag and a `SourceName`), a gate that both processors consult once per cycle and once per event, a startup check, one counter, four log messages and the registration of the election name with the election engine of #717. A replica without the lease skips its cycles; a replica that loses the lease stops dispatching before the next event, and the new leader resumes from the persisted position.

**Standards and references**: lease-based single active consumer as in Hangfire (one server runs recurring work) and Kubernetes Leases (named comparable products, not dependencies); ADR-050 (modules may require modules), ADR-052 (lock failure contract); AGENTS.md sections 3, 6 and 7.

**Affected packages**: `Encina.Cdc` only. The lock providers are consumed through #717 and are not edited.

**Provider category**: Distributed locks (the engine of #717 runs on every lock provider of SPEC-000 DEC-009: InMemory, Redis, SqlServer, PostgreSQL, MySQL, Azure Blob, DynamoDB); there is no provider code in `Encina.Cdc`. The CDC connectors (SqlServer, PostgreSql, MySql, MongoDb, Debezium) are not touched.

**Shared pieces and who owns them**: #717 owns `ILeaderElection`, the election engine, the lease semantics, the election metrics and the loop-liveness health check. This plan owns `CdcLeaderElectionOptions`, `CdcLeaderGate`, the startup check and the four CDC log messages. #2393 owns the persistent position store that makes failover exact. #1251 owns row claiming in the outbox and scheduler processors and is not involved.

---

## Design Choices

<details>
<summary><strong>1. How a CDC source is kept from being consumed twice (Dilemma D1, the issue's own)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A lease per source, held by one instance, through the leader election** | One consumer per source, so ordering is the source's own; simple to explain; the lease engine already exists as a plan (#717) | A lock outage or a slow failover stops consumption for up to the lease; one instance carries the whole source |
| **B) Partition the source by table across instances** | More throughput; failures stay local | Ordering across tables is lost; a table-to-instance assignment needs its own coordination (a lease per table is still a lease); the connectors read the whole log, not a table |
| **C) Leave deduplication to the consumers** | No new infrastructure | Every consumer deduplicates; the audit and retention consumers of #635 and #636 each need it; N instances still read the source N times |

### Chosen Option: **A — A lease per source** (recommended, pending the maintainer)

### Rationale

- A wins for the common deployment: one source, a few replicas for availability, one consumer at a time. B wins when a single consumer cannot keep up with the source and the tables are independent (a deliberate high-volume case). C wins when an application runs one replica or accepts duplicate reads and no lock infrastructure is wanted; handlers must be idempotent in any case (ADR-050 CDC row).
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Which election engine the CDC processors use (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The `ILeaderElection` of #717: `Encina.Cdc` asks `IsLeader(electionName)` (no I/O), resolved from the root provider (a singleton), and registers its election name with the election options of #717** | One engine for every processor (heartbeat, lease anchored before the call, failover, metrics, loop-liveness health check); no new ProjectReference (Cdc already references Messaging); the consumer never touches the lock provider | Depends on #717 (open); #717 must let a module add an election name, which its fixed list of three does not yet allow |
| **B) A lease loop of its own inside `Encina.Cdc` over `IDistributedLockProvider`, as the issue text says** | Independent of #717 | A second heartbeat loop with its own lease arithmetic and failure handling; a new ProjectReference from `Encina.Cdc` to `Encina.DistributedLock`; two election mechanisms in one application |

### Chosen Option: **A — Build on `ILeaderElection`** (recommended, pending the maintainer)

### Rationale

- A wins on consistency and on the safety rules #717 already designs (the lease is anchored before the lock call, a failed renewal ends leadership visibly). B wins only if CDC must ship before #717 or the maintainer decides not to build #717. The requirement this plan places on #717 is small: `LeaderElectionOptions` must accept an election name added by another module (recorded in Prerequisites & Dependencies); under B the same name becomes the lock resource of the CDC loop.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. What identifies the source in the lease (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The operator names the source: `CdcLeaderElectionOptions.SourceName` is required when the election is on; the election name is `cdc:{connectorId}:{sourceName}` with the two parts escaped (`%` and `:` percent-encoded) so no two (connector, source) pairs share a name** | Explicit and stable; nothing about the connection (host, user) reaches a lock name, a metric tag or a log; fails at startup when missing | One more setting |
| **B) Derive the source from the connection: a hash of the normalized connection string host and database** | No setting | The connector, not `Encina.Cdc`, owns the connection and the connectors expose no source identity today (`ICdcConnector` has `ConnectorId` and `GetCurrentPositionAsync` only); a rename of a host changes the lease; a hash of connection details is a derived identifier of infrastructure in metrics |
| **C) `ConnectorId` alone** | No setting | Different sources of the same provider type share a lease (see Summary) |

### Chosen Option: **A — A required `SourceName`** (recommended, pending the maintainer)

### Rationale

- A wins because the lease name must be injective and must carry no connection detail; C is wrong whenever one lock backend serves two sources (shown by the constant `ConnectorId` values). B wins if a later `ICdcConnector` member exposes a source identity (an additive change to every connector, outside this issue).
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. When the election is missing or the lock backend is down (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fail closed: with the flag on, a processor consumes only while `IsLeader` is true; with no election registered it does not run and logs it once, and the startup check fails the host** | The flag keeps its meaning (at most one consumer); no event is lost because the position is persisted and the leader resumes | A lock outage stops consumption once the lease expires; a lag builds up |
| **B) Fail open: when leadership cannot be determined, consume anyway** | Never stops | Duplicate consumption exactly when the infrastructure is unhealthy; defeats the flag |

### Chosen Option: **A — Fail closed** (recommended, pending the maintainer)

### Rationale

- A wins for sources whose handlers cannot be made idempotent (audit, retention enforcement); B wins when a continuous stream matters more than duplicates and every handler is idempotent. The lag of A is visible: the loop-liveness health check of #717 and the follower counter of Phase 5 show it, and nothing is dropped because the position store keeps the resume point.
- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Opt-in only, or required by startup validation (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Opt-in (`WithLeaderElection`), disabled by default; ADR-050's enforcement of "row claiming, lock or leader" for CDC stays with the enforcement issue (#2382)** | Matches the acceptance criterion "opt-in, disabled by default"; a single-instance application is valid and pays nothing | A multi-instance CDC deployment can still run without coordination |
| **B) CDC enabled with no coordination mechanism is a registration error** | Enforces the ADR-050 row now | There is no mechanism for CDC but this flag (#1251 covers outbox and scheduler rows, not CDC), so the rule would force every CDC user to configure a lock provider, including single-instance ones |

### Chosen Option: **A — Opt-in** (recommended, pending the maintainer)

### Rationale

- A wins until the startup-requirements validator (#2382) lands and the maintainer decides how a multi-instance deployment is detected; B wins if the maintainer wants CDC to be unsafe-by-default impossible before 1.0 and accepts a mandatory lock provider.
- **Class:** Dilemma

</details>

<details>
<summary><strong>6. An in-memory position store with the election on (Settled)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Startup validation: with the election on, an `InMemoryCdcPositionStore` outside the Development environment fails the host start (the check names the module, the requirement, ADR-050 and the fix)** | A failover with an in-memory position would resume from nothing: it replays or skips; the check turns that into a start error | A Development host may still use the in-memory store (the election is then only a demonstration) |
| **B) Allow it and log a warning** | No friction | A silent data-handling defect on the first failover |

### Chosen Option: **A — Startup validation**

### Rationale

Only A complies with the rule that an in-memory default never satisfies a requirement outside Development.

- **Class:** Settled
- **Settled by:** `docs/architecture/adr/050-modules-may-require-modules.md` (the CDC row: "A persistent `ICdcPositionStore`"; and the sub-decision on in-memory defaults: "A `TryAdd` default may supply the in-memory implementation in Development, but never counts as satisfying the requirement elsewhere").

</details>

<details>
<summary><strong>7. Flag, options and gate follow the existing CDC configuration style (Trivial)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CdcOptions.UseLeaderElection` (bool) and `CdcConfiguration.WithLeaderElection(Action<CdcLeaderElectionOptions>)`, the options registered as a singleton instance, an internal static `CdcLeaderGate`, and the processors resolve `ILeaderElection` from the root provider (so their constructors do not change)** | The same shape as `WithMessagingBridge` and `WithShardedCapture`; no constructor change, so no `new CdcProcessor(` or `new ShardedCdcProcessor(` site (28 in five test files today) is touched | The gate reads the flag from `CdcOptions` |
| **B) A new optional constructor parameter on both processors** | Explicit dependency | Changes the constructor of two classes with about 28 `new` sites in tests; an optional parameter after `ICdcDeadLetterStore?` on one and none on the other |

### Chosen Option: **A — The existing configuration style, no constructor change**

### Rationale

The processors already take `IServiceProvider` and resolve scoped services from it; a singleton election resolved from the root provider adds no scoped-into-singleton injection.

- **Class:** Trivial
- **Settled by:** `src/Encina.Cdc/CdcConfiguration.cs` (`WithMessagingBridge`, `WithShardedCapture`, `WithCacheInvalidation`) and `src/Encina.Cdc/ServiceCollectionExtensions.cs:96` (options registered with `services.AddSingleton(...)`).

</details>

---

## Implementation Phases

### Phase 1: Core Abstractions — Options and the Gate (pull request A)

> **Goal**: The flag, the options and the decision function.

<details>
<summary>Tasks</summary>

1. **Prerequisite**: `ILeaderElection` (#717 Phase 1) is on `main`; the first task checks it and stops otherwise.
2. **Create** `src/Encina.Cdc/Leadership/CdcLeaderElectionOptions.cs` (public sealed class): `string SourceName` (required; validated: not null or whitespace, at most 100 characters, no control characters) and `string ElectionNameFor(string connectorId)` returning `cdc:{Escape(connectorId)}:{Escape(SourceName)}` where `Escape` percent-encodes `%` and `:` (injective). No secret, no connection string: the class needs neither `[JsonIgnore]` nor a `ToString` override, and `SourceName` is an operator-chosen label.
3. **Create** `src/Encina.Cdc/Leadership/CdcLeaderGate.cs` (internal static): `bool ShouldConsume(ILeaderElection? election, string electionName)` returns false when `election` is null (fail closed, Design Choice 4) and otherwise `election.IsLeader(electionName)`; it also owns the `Encina.Cdc.Leadership` meter with one counter `cdc.leadership.cycles` (unit `{cycle}`, tags `connector_id` and `role` with the two values `leader` and `follower`; no `_total` suffix), and the role-change logging (a change from follower to leader logs `CdcBecameActive`, the reverse `CdcBecameStandby`, so the log is not written every cycle).
4. **Modify** `CdcOptions.cs` (`public bool UseLeaderElection { get; set; }`, default false, XML docs), `CdcConfiguration.cs` (`WithLeaderElection(Action<CdcLeaderElectionOptions> configure)`: `ArgumentNullException.ThrowIfNull`, sets the flag, keeps the options; an internal `LeaderElectionOptions` accessor like `MessagingOptions`) and `CdcLog.cs` (EventIds **4860-4863**, packed: 4860 became active, 4861 became standby, 4862 lost the lease while processing a batch, 4863 flag on but no election registered; each carries the connector id and never an event body, per the `ForLogging()` rule).
5. Public symbols added (the `PublicAPI.Unshipped.txt` lines): `CdcOptions.UseLeaderElection` (get and set), `CdcLeaderElectionOptions` (type, default constructor, `SourceName` get and set, `ElectionNameFor`) and `CdcConfiguration.WithLeaderElection`. No new `EventIdRanges` field (the range `Cdc` exists), so no registry row and no field line. No error code is created: a failed election is a skipped cycle or a startup exception, not an `EncinaError`, so no ADR-062 `ErrorCategory` applies.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Encina is a .NET 10 / C# 14 library; issue #3990; plan docs/plans/cdc-single-active-consumer-implementation-plan-3990.md. Dilemmas D1 to D5 are recommended, pending the maintainer: read the Maintainer Decisions section first if it exists and follow the answers.
- ILeaderElection (bool IsLeader(string electionName), no I/O) comes from #717 and is registered in Encina.Messaging. ConnectorId is a constant per connector type, so the lease needs a SourceName.

TASK:
Create CdcLeaderElectionOptions and CdcLeaderGate, add UseLeaderElection to CdcOptions and WithLeaderElection to CdcConfiguration, and add four CdcLog messages with EventIds 4860-4863.

KEY RULES:
- EventIds only 4860-4863, packed; log the connector id only, never an event body or EncinaError.Message.
- Election name escaping is injective ('%' and ':' percent-encoded).
- Counter name without a _total suffix; tags connector_id and role only (no tenant tag: CDC has no tenant context here).
- No constructor change on CdcProcessor or ShardedCdcProcessor.

REFERENCE FILES:
- src/Encina.Cdc/CdcConfiguration.cs (WithMessagingBridge)
- src/Encina.Cdc/CdcLog.cs
- src/Encina.Cdc/DeadLetter/Diagnostics/CdcDeadLetterMetrics.cs (meter and counter style)
```

</details>

---

### Phase 2: Configuration & DI (pull request A)

> **Goal**: Register the options, the election name and the startup check; prove it with a DI test.

<details>
<summary>Tasks</summary>

1. **Modify** `ServiceCollectionExtensions.cs` (`AddEncinaCdc`): when `UseLeaderElection` is on, `services.AddSingleton(leaderOptions)`, register the election name with the election options of #717 through `services.Configure<LeaderElectionOptions>(...)` (the name needs the connector id, which is a scoped service: the connector id is resolved by a small `IConfigureOptions<LeaderElectionOptions>` that opens one scope and reads `ICdcConnector.ConnectorId` when the options are first built, never at registration), and `services.AddHostedService<CdcLeaderElectionStartupCheck>()`.
2. **Create** `src/Encina.Cdc/Leadership/CdcLeaderElectionStartupCheck.cs` (internal sealed `IHostedService`, throws in `StartAsync`; the `InvalidOperationException` names the module, the requirement, ADR-050 and the fix, never a value): fails when `SourceName` is invalid, when no `ILeaderElection` is registered (the `AddEncinaLeaderElection()` of #717 or the lock provider is missing; it resolves `ILeaderElection` with `GetService`, so the order of registrations does not matter), and when the resolved `ICdcPositionStore` is the in-memory default and `IHostEnvironment.IsDevelopment()` is false (Design Choice 6).
3. **DI test** (registration completeness, `AGENTS.md` section 3): build the provider with `ValidateOnBuild` and `ValidateScopes`, resolve every service the new code adds, in both registration orders of `AddEncinaCdc` and `AddEncinaLeaderElection`; a second case proves that a registration of a database position store wins over the in-memory default without overriding the application's own registration (the existing `TryAddSingleton` is unchanged).
4. The processors are registered exactly as today (`ServiceCollectionExtensions.cs:133,171`); nothing is injected into a singleton from a scope.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Issue #3990, Phase 2. CdcLeaderElectionOptions, CdcLeaderGate and the flag exist from Phase 1; #717 provides ILeaderElection and LeaderElectionOptions.

TASK:
Wire AddEncinaCdc for WithLeaderElection: options singleton, the election name registered with LeaderElectionOptions, and CdcLeaderElectionStartupCheck with the three failure cases; write the DI test with ValidateOnBuild and ValidateScopes.

KEY RULES:
- The connector id is scoped: resolve it inside a scope, never inject ICdcConnector into a singleton.
- The startup exception names the module, the requirement, ADR-050 and the fix; no value.
- The in-memory position store fails the check outside Development only.

REFERENCE FILES:
- src/Encina.Cdc/ServiceCollectionExtensions.cs
- docs/architecture/adr/050-modules-may-require-modules.md (CDC row)
- docs/plans/leader-election-implementation-plan-717.md (startup check and options)
```

</details>

---

### Phase 3: Pipeline Integration — The Processors (pull request B)

> **Goal**: Both processors skip as followers and stop before the next event when they lose the lease.

<details>
<summary>Tasks</summary>

1. **Modify** `Processing/CdcProcessor.cs`: when `_options.UseLeaderElection` is false nothing changes. When true, `ExecuteAsync` resolves `ILeaderElection` once from `_serviceProvider` (`GetService`, a singleton), builds the election name once from the options and the connector id, and each cycle starts with `CdcLeaderGate.ShouldConsume(...)`: a follower records the `follower` role, waits `PollingInterval` and continues; in `ProcessChangesAsync` the gate is checked before dispatching each event, and when it turns false the loop breaks before the dispatch (the event is neither dispatched nor saved, so the new leader resumes from the persisted position) and logs 4862. The retry and dead letter logic is unchanged: a `Left` from the dispatcher still fails that event (retry, then dead letter), never reports success.
2. **Modify** `Sharding/ShardedCdcProcessor.cs` the same way over `StreamAllShardsAsync`, with `ShardedCaptureOptions.ConnectorId` as the connector part of the name.
3. Bound of a lost lease: the gate is a local lease check (no I/O), so a replica stops dispatching at the first event after the lease expires: at most the lease duration of #717 (45 s by its default) plus the event in flight. A stream blocked while waiting for a change dispatches nothing. There is no fencing (#218), so the event being dispatched when the lease expires can complete; handlers are idempotent per ADR-050.
4. Constructor and call-site count: no constructor changes (Design Choice 7), so the 28 `new CdcProcessor(` and `new ShardedCdcProcessor(` sites under `tests` and `src` (counted with `git grep -c` on `origin/main`: 8, 3, 7, 1 and 9 in five test files) need no edit; the existing tests keep passing because the flag defaults to false and the provider is not touched.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Issue #3990, Phase 3. Pull request A is merged: CdcLeaderGate, the options and the startup check exist.

TASK:
Gate CdcProcessor and ShardedCdcProcessor as in the Phase 3 Tasks, with unit tests that script ILeaderElection (follower, becomes leader, loses the lease mid-batch).

KEY RULES:
- With the flag off the code path and the existing tests are unchanged.
- Check the gate before DispatchAsync of each event; break without dispatching or saving the position.
- Use the existing retry, dead letter and TimeProvider-free structure; do not add DateTime.UtcNow.
- Resolve ILeaderElection from the root provider, never from a scope.

REFERENCE FILES:
- src/Encina.Cdc/Processing/CdcProcessor.cs
- src/Encina.Cdc/Sharding/ShardedCdcProcessor.cs
- tests/Encina.UnitTests/Cdc/CdcProcessorDeadLetterTests.cs
- tests/Encina.UnitTests/Cdc/Sharding/ShardedCdcProcessorTests.cs
```

</details>

---

### Phase 4: Cross-Cutting Integration

> **Goal**: The twelve functions of AGENTS.md section 6, as in the matrix below.

<details>
<summary>Tasks</summary>

1. Health: no new health check. The election health of #717 (leader or follower as data, unhealthy only when the heartbeat loop stopped) covers the election; the existing `CdcHealthCheck` keeps checking connector and position store, and a follower passes both (follower is not a failure). #2396 (processor liveness) must treat a follower as live; recorded in Prerequisites & Dependencies.
2. Resilience: the lock backend's retry and timeout belong to the election engine of #717; the CDC processors add none.
3. Idempotency: the lease reduces duplicates; it does not remove them (failover replays from the persisted position, and the in-flight event has no fencing). The handlers' idempotency requirement of ADR-050 stays.
4. Multi-tenancy: the lease key has no tenant component (a CDC source holds all tenants' rows), so the reserved `default` tenant segment does not apply and no tenant tag is added (ADR-054 D4 adds a tag only where the tenant is known); a database per tenant is one source per `SourceName`.
5. Audit trail: the audit sink of #636 is a consumer; with the election on it runs once per change. No audit event is added here.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
- Issue #3990, Phase 4.

TASK:
Confirm the cross-cutting decisions of the matrix hold in the code of Phases 1 to 3 and that no health check, tenant tag, retry or audit event was added.

KEY RULES:
- Health: followers are healthy.
- No tenant tag; no EncinaError.Message in logs, tags or health data.

REFERENCE FILES:
- src/Encina.Cdc/Health/CdcHealthCheck.cs
- docs/plans/leader-election-implementation-plan-717.md (health check and metrics)
```

</details>

---

### Phase 5: Observability

> **Goal**: One counter and four log messages; the election metrics come from #717.

<details>
<summary>Tasks</summary>

1. `Encina.Cdc.Leadership` meter with the counter `cdc.leadership.cycles` (unit `{cycle}`, tags `connector_id`, `role`), inside `CdcLeaderGate`. No `ActivitySource`: a cycle is not an operation worth a span, and the election's own spans belong to #717.
2. The four `CdcLog` messages (4860-4863), registered in the existing range `Cdc` (4800-4899, `src/Encina/Diagnostics/EventIdRanges.cs:103`); no new range, no registry row, no `AssemblyRanges` change (`Encina.Cdc` is already mapped).
3. Errors reach the log as the error code or `ForLogging()` only, never `EncinaError.Message`; the connector id is a constant of the connector type and the source name is an operator label, so neither is a direct identifier.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
- Issue #3990, Phase 5.

TASK:
Check the counter and the four log messages: names, tags, EventIds 4860-4863 only.

KEY RULES:
- No _total suffix; connector_id and role tags only; no raw ids of tenants or users.

REFERENCE FILES:
- src/Encina.Cdc/CdcLog.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 6: Testing and Documentation & Finalization

> **Goal**: Every flag at its target; the documentation set.

<details>
<summary>Tasks</summary>

| Flag | Scope | Per-file target (justification) |
|------|-------|---------------------------------|
| Unit | options and escaping, gate (follower, leader, no election), startup check (three failures), `WithLeaderElection`, both processors scripted (follower skip, takes over, loses the lease mid-batch, flag off unchanged) | `CdcLeaderElectionOptions.cs` 95, `CdcLeaderGate.cs` 95, `CdcLeaderElectionStartupCheck.cs` 90 (every branch reachable with substituted services); `Processing/CdcProcessor.cs` and `Sharding/ShardedCdcProcessor.cs` keep their measured baseline and add the new branches (the pull request records the number; targets never go down) |
| Guard | `WithLeaderElection(null)`, `ElectionNameFor` null and whitespace, startup check constructor | `CdcLeaderElectionOptions.cs` 90, `CdcConfiguration.cs` keeps its baseline (the argument checks are the only branches the guard flag reaches) |
| Contract | none: no new interface or abstract class; the lock provider matrix (every provider of SPEC-000 DEC-009 through the election) is the contract test of #717 | justification in the PR |
| Property | for any connector id and source name the election name is injective (two different pairs never produce the same name) and `ElectionNameFor` is deterministic | `CdcLeaderElectionOptions.cs` 80 (one invariant over generated strings) |
| Integration | two `CdcProcessor` instances over one `TestCdcConnector` source and the election engine of #717 on a real lock provider: only one dispatches, the other idles; stopping the leader lets the other take over from the saved position; `[Collection("CDC-PostgreSQL")]` with the PostgreSQL lock provider, in `tests/Encina.IntegrationTests/Cdc/CdcLeaderElectionIntegrationTests.cs` | `CdcLeaderGate.cs` 80 and the two processors keep their integration baseline (the failover path is the new branch) |

Load and benchmark: a justification `.md` each (the gate is one lease comparison per event; no concurrency of its own).

Measure from the repository root per flag: `dotnet test tests\Encina.UnitTests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\UnitTests --filter "FullyQualifiedName~Cdc"` (and the same for `GuardTests`, `PropertyTests`, `IntegrationTests`), then `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report` and `dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications`. CRAP at most 10: `ExecuteAsync` already carries a retry `catch`; the gate and the role-change logging are separate small methods so the touched methods stay below the limit.

Documentation: XML docs on every new public API (with an `<example>` of `WithLeaderElection`); `src/Encina.Cdc/README.md` (a "Single active consumer" section: when to turn it on, the `SourceName` rule, the fail-closed behavior, the persistent position store requirement); one changelog fragment `changelog.d/3990-cdc-single-active-consumer.added.md` in pull request B (the flag does nothing until the processors use the gate); the `Encina.Cdc` manifest entries and PublicAPI lines through `mechanical-fixer`; no ADR (the choices are recorded as Dilemmas; the election engine's ADR-050 amendment belongs to #717), no ROADMAP change.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```
CONTEXT:
- Issue #3990, Phase 6. Shouldly through Encina.Testing.Shouldly; shared [Collection("CDC-PostgreSQL")] integration fixture.

TASK:
Write the tests of the Phase 6 table for your pull request, the README section, the changelog fragment and the manifest targets with one-sentence justifications.

KEY RULES:
- Tests execute real code; no Thread.Sleep; use FakeTimeProvider where time matters.
- A test never swallows an exception thrown by the code under test.
- Integration: shared fixture, ClearAllDataAsync in InitializeAsync; never IClassFixture.
- Targets never go down.

REFERENCE FILES:
- tests/Encina.UnitTests/Cdc/CdcProcessorDeadLetterTests.cs
- tests/Encina.IntegrationTests/Cdc/CdcProcessorIntegrationTests.cs
- tests/Encina.IntegrationTests/Cdc/Helpers/TestCdcConnector.cs
- .github/coverage-manifest/Encina.Cdc.json
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

Two pull requests, in order. Both need `ILeaderElection` and an extensible `LeaderElectionOptions` from #717 on `main` first.

<!-- local-draft: D:\Proyectos\Encina\.claude\worktrees\pb-e\artifacts\local-ai\out\p3990-filecount.md -->

| Category | PR A | PR B | Total |
| :--- | :--- | :--- | :--- |
| Production code, edited | 4 | 2 | 6 |
| Production code, new | 3 | 0 | 3 |
| PublicAPI and coverage manifest | 2 | 1 | 3 |
| Test files | 4 | 3 | 7 |
| README and changelog fragment | 0 | 2 | 2 |
| Total | 13 | 8 | 21 |

- **PR A** (13 files, about 600 lines): `CdcOptions.cs`, `CdcConfiguration.cs`, `ServiceCollectionExtensions.cs`, `CdcLog.cs`; new `Leadership/CdcLeaderElectionOptions.cs`, `Leadership/CdcLeaderGate.cs`, `Leadership/CdcLeaderElectionStartupCheck.cs`; `PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.Cdc.json`; tests: unit (options, gate, `WithLeaderElection`), unit (startup check and the `ValidateOnBuild`/`ValidateScopes` DI test), guard (`tests/Encina.GuardTests/Cdc/CdcConfigurationGuardTests.cs` edit), property (election name injectivity). No README and no changelog fragment: until pull request B the flag does nothing.
- **PR B** (8 files, about 450 lines): `Processing/CdcProcessor.cs`, `Sharding/ShardedCdcProcessor.cs`; the manifest; tests: unit `CdcProcessorLeadershipTests.cs` (new), unit `ShardedCdcProcessorLeadershipTests.cs` (new), integration `CdcLeaderElectionIntegrationTests.cs` (new); `src/Encina.Cdc/README.md`; the changelog fragment.
- No csproj edit: `Encina.Cdc` already references `Encina.Messaging`, grants `InternalsVisibleTo` to `Encina.UnitTests`, `Encina.GuardTests`, `Encina.IntegrationTests`, `Encina.ContractTests` and `Encina.PropertyTests`, and the test projects already reference it. The integration test needs the PostgreSQL lock provider package in `Encina.IntegrationTests`; its reference is verified before pull request B (a ProjectReference edit counts as a file; it keeps pull request B at 9 files).

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| ADR-050 (modules may require modules), CDC row | Requires a persistent position store and "row claiming, lock or leader"; the in-memory default never satisfies a requirement outside Development |
| ADR-052 (distributed lock failure contract) | The lock failure semantics the election of #717 follows |
| Lease-based single active worker (Hangfire; Kubernetes Leases) | Named comparable products for the model |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `CdcProcessor`, `ShardedCdcProcessor` | `src/Encina.Cdc/Processing/`, `src/Encina.Cdc/Sharding/` | The two loops that get the gate |
| `CdcConfiguration`, `CdcOptions`, `AddEncinaCdc` | `src/Encina.Cdc/` | Flag, fluent method and registration idiom |
| `CdcLog`, `EventIdRanges.Cdc` | `src/Encina.Cdc/CdcLog.cs`, `src/Encina/Diagnostics/EventIdRanges.cs:103` | The four messages in the existing range |
| `CdcDeadLetterMetrics` | `src/Encina.Cdc/DeadLetter/Diagnostics/` | Meter and counter style (no `_total`) |
| `ILeaderElection`, `LockLeaderElection`, `LeaderElectionService` | #717 (not yet on `main`) | The election this feature consumes |
| `ICdcPositionStore` and the persistent store | `src/Encina.Cdc/Abstractions/`, #2393 | The resume point after a failover |
| `TestCdcConnector`, `CdcProcessorIntegrationTests` | `tests/Encina.IntegrationTests/Cdc/` | Integration test style |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Cdc` (existing range 4800-4899) | 4860-4863 | `CdcLog`: four messages, packed. 4800-4853 are used; 4854-4859 are claimed by the #2393 plan, 4880-4885 by the #636 plan (4886-4891 held); 4864-4879 stay free. No new range, registry row or `EventIdRanges` field |

### Provider Matrix

| Category | Applies | Notes |
|----------|---------|-------|
| Distributed locks (7 for 1.0) | Through #717 | InMemory, Redis, SqlServer, PostgreSQL, MySQL, Azure Blob, DynamoDB: the election engine of #717 is contract-tested on each; `Encina.Cdc` has no lock-provider code |
| Database (10), caching, transports, validation, cloud | No | The feature reads no store of its own and touches no connector |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production code | 9 | 6 edited, 3 new |
| PublicAPI and manifest | 3 | two pull requests |
| Test files | 7 | unit 4, guard 1 (edit), property 1, integration 1 |
| README and changelog fragment | 2 | pull request B |
| Total | 21 | 13 + 8 |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Railway Oriented Programming; no [Obsolete]; XML docs on every public API; zero warnings; background loops never let an exception escape; errors are never swallowed.
- Issue #3990: only one instance consumes a CDC source at a time, through the ILeaderElection of #717. Plan: docs/plans/cdc-single-active-consumer-implementation-plan-3990.md. Dilemmas D1 to D5 are recommended, pending the maintainer; read the Maintainer Decisions section first if it exists.

IMPLEMENTATION OVERVIEW:
1. Phase 1: CdcLeaderElectionOptions (SourceName, injective ElectionNameFor), CdcLeaderGate (internal static; counter cdc.leadership.cycles), CdcOptions.UseLeaderElection, CdcConfiguration.WithLeaderElection, CdcLog EventIds 4860-4863.
2. Phase 2: AddEncinaCdc registration, the election name registered with LeaderElectionOptions, CdcLeaderElectionStartupCheck (invalid SourceName, no ILeaderElection, in-memory position store outside Development), DI test with ValidateOnBuild and ValidateScopes.
3. Phase 3 (second pull request): gate CdcProcessor and ShardedCdcProcessor per cycle and per event; break before dispatching when the lease is lost; no constructor change.
4. Phases 4 to 6: cross-cutting decisions, observability, tests per flag, README, changelog.

KEY PATTERNS:
- Fail closed: no election means no consumption; the flag is off by default.
- The lease key carries connector id and source name, escaped and injective; no tenant segment; no connection detail.
- Logs and tags carry the connector id only; never an event body, EncinaError.Message or a tenant id.
- Per-flag targets from the Encina.Cdc manifest; CRAP at most 10; shared [Collection] integration fixture.

REFERENCE FILES:
- src/Encina.Cdc/Processing/CdcProcessor.cs
- src/Encina.Cdc/Sharding/ShardedCdcProcessor.cs
- src/Encina.Cdc/CdcConfiguration.cs
- src/Encina.Cdc/ServiceCollectionExtensions.cs
- docs/plans/leader-election-implementation-plan-717.md
- docs/plans/dsr-implementation-plan-404.md (style)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: the gate reads the lease state in memory; there is nothing to cache |
| 2 | OpenTelemetry | ✅ | Phase 5: the counter `cdc.leadership.cycles` (tags `connector_id`, `role`); the election's own gauge, counters and spans are #717's; no tenant tag because CDC has no tenant context here (ADR-054 D4) |
| 3 | Structured Logging | ✅ | Phases 1 and 5: `CdcLog` EventIds 4860-4863 in the existing `Cdc` range; connector id only |
| 4 | Health Checks | ✅ | Phase 4: no new check; the election health of #717 reports leader or follower and the loop liveness; a follower is healthy by design |
| 5 | Validation | ✅ | Phase 2: `SourceName` is validated at startup (non-empty, at most 100 characters, no control characters) by the startup check |
| 6 | Resilience | ❌ | Not applicable here: the lock call's timeout and retry live in the election engine of #717; the processors keep their own retry and dead letter logic unchanged |
| 7 | Distributed Locks | ✅ | The core: the lease runs on `IDistributedLockProvider` through `ILeaderElection` (#717); no provider code in `Encina.Cdc` |
| 8 | Transactions | ❌ | Not applicable: no store write is added; the position save keeps its place after each successful dispatch |
| 9 | Idempotency | ✅ | Reduces duplicates; failover replays from the persisted position and the in-flight event has no fencing (#218), so the ADR-050 idempotent handler requirement stays |
| 10 | Multi-Tenancy | ❌ | Not applicable: a source holds all tenants' rows and the lease key has no tenant part, so the reserved `default` segment and tenant-qualified ids do not arise; a database per tenant is a distinct `SourceName` |
| 11 | Module Isolation | ❌ | Not applicable: no module-scoped state |
| 12 | Audit Trail | ❌ | Not applicable: no state change of its own; the audit sink of #636 benefits (one write per change) |

---

## Prerequisites & Dependencies

- **Blocking**: #717 (open; its plan is on `main`): `ILeaderElection` in `Encina.Messaging` and the engine in `Encina.DistributedLock`. The requirement this plan places on #717's implementation: `LeaderElectionOptions` must accept an election name added by another module (its Phase 1 task 2 lists a fixed set of three names). Under D2 option B this dependency disappears and `Encina.Cdc` references `Encina.DistributedLock`.
- **Recommended before pull request B is useful in production**: #2393 (persistent CDC position store): without it a failover resumes from an in-memory position (the startup check forbids that outside Development). #2667 (lock providers return `Either`, ADR-052) is a dependency of #717.
- Siblings, cited by number: #634, #635 and #636 (CDC consumers that gain exactly-once reads per change from the lease), #1251 (row claiming in the outbox and scheduler processors, not CDC), #2396 (processor liveness health checks for CDC: it must treat a follower as live), #2344 (the CDC dead letter queue, which still reads `DateTime.UtcNow` in `CdcProcessor`; this plan does not touch that line), #2382 (startup requirements validator, the home of the ADR-050 enforcement that D5 leaves out).
- Deferred inside this plan (not new issues): partitioning a source by table across instances (D1 option B) would be a separate design with its own plan if the maintainer chooses it; exposing a source identity from `ICdcConnector` (D3 option B) is an additive change to every connector.

---

## Next Steps

1. The maintainer answers Dilemmas D1 to D5 when work on #3990 starts (D1 is the issue's own `needs-decision`); the answers go into `## Maintainer Decisions` at the end of this plan.
2. Wait for #717 (`ILeaderElection` and the extensible election names) on `main`, then brief pull request A; pull request B follows A.
3. After pull request B, link the README section from #3990.
