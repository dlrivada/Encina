# Implementation Plan: Snowflake MachineId Lease So Replicas Never Share an Id Space

> **Issue**: [#2394](https://github.com/dlrivada/Encina/issues/2394)
> **Type**: Feature
> **Complexity**: High (5 pull requests, 3 packages; one new project reference)
> **Estimated Scope**: ~550-700 lines of production code + ~1,500-1,900 lines of tests
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027 (lock providers); [ADR-050](../architecture/adr/050-modules-may-require-modules.md) (the ID generation row: a unique `MachineId` per instance from a lease or an explicit value, settled 2026-10-17); [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md) (lock failure contract); [ADR-054](../architecture/adr/054-observability-conventions.md) (observability conventions); [ADR-062](../architecture/adr/062-error-category-contract.md) (error category)
> **Depends on**: #2667 (implements ADR-052; open, not a pending maintainer decision)
> **Related**: #638 (the generator), #2381 (module requirements ADR, closed), #2382 (startup requirements validator), #2405 (Redis extend ownership and SQL Server expiry), #207 and #208 (PostgreSQL and MySQL lock providers), #218 (fencing), #741, #717 (same lock family)

---

## Summary

`SnowflakeOptions.MachineId` is a plain `long` that defaults to 0 and is used directly by `SnowflakeIdGenerator.Generate()`. Two replicas that do not set it both emit machine id 0 and can produce the same id in the same millisecond and sequence: a silent duplicate-id risk. The maintainer settled the shape on 2026-10-17 (comment on #2394): support `Lease` (through `IDistributedLockProvider`) and `Explicit`, drop derived ids; with neither configured startup fails, and a single-instance deployment must say so. This plan delivers that:

1. **A source for the machine id.** `SnowflakeOptions.MachineIdSource` (`Unspecified` by default, `Explicit`, `Lease`). `Explicit` uses `MachineId` as today. `Lease` takes the id from an `IMachineIdProvider` registered by the lease package.
2. **Fail closed at startup.** A startup check fails the host when the source is `Unspecified`, or `Lease` with no `IMachineIdProvider` registered, naming the module, the requirement, ADR-050 and the fix (ADR-050 rule 3).
3. **The lease.** In `Encina.DistributedLock`: `MachineIdLeaseService` acquires `encina:snowflake:machine:{n}` for the first free `n` in `[0, MaxMachineId]` (starting at a random offset), holds it for the process lifetime, renews it with `ILockHandle.ExtendAsync` on a `PeriodicTimer` over the injected `TimeProvider`, and releases it on shutdown. If every id is taken, or the provider faults, the host does not start.
4. **Generation stops when the lease cannot be trusted.** `GetMachineId()` answers `Right(id)` only while `now` is before the lease's local expiry minus a safety margin (the expiry is computed from the time read **before** the acquire or extend call, so the lock backend's own clock starts after it); otherwise `Left` `encina.idgen.machine_id_unavailable`, and `Generate()` returns that `Left`. It never falls back to id 0 or to the old id after the lease may have expired.
5. **Visible.** The existing `IdGeneratorHealthCheck` reports the leased id and lease age and is unhealthy while the lease is lost; a gauge and a counter record the lease; `[LoggerMessage]` ids 7540-7546.
6. **Limit stated.** The lease protects `Generate()`. `Generate(string shardId)` encodes the caller's shard id in the same bits as the machine id (`SnowflakeIdGenerator.GenerateInternal`), so two replicas generating ids for the same shard can still collide whatever the lease says; that is a different defect and goes to its own issue (Prerequisites & Dependencies).

**Standards**: ADR-050 and the maintainer's 2026-10-17 decision; ADR-052; ADR-062 (`encina.idgen.machine_id_unavailable` and `encina.idgen.machine_id_exhausted` are `Unavailable`); the Snowflake id layout (41-bit timestamp, machine bits, sequence) as documented on `SnowflakeOptions`; lease-based coordination as in leader election (#717).

**Estimated scope**: 9 new production files, 10 modified, 3 `PublicAPI` and 3 manifest files, 17 test files, 7 documentation files; 5 pull requests.

**Affected packages**: `Encina.IdGeneration` (source, abstraction, startup check, generator, health check), `Encina` (error codes and factories, EventId range), `Encina.DistributedLock` (lease service, options, metrics, a new project reference to `Encina.IdGeneration`), `Encina.AspNetCore` (one line: the health check registration resolves `IMachineIdProvider`).

**Provider category**: Distributed locks (the lease works with any `IDistributedLockProvider`; InMemory exists for tests, Redis and SqlServer exist, PostgreSQL #207 and MySQL #208 are pending, Azure Blob #209 and DynamoDB #210 are not built: SPEC-000 DEC-009). It is not a database-store feature, so the 10-provider database matrix does not apply.

**What the code does today** (`origin/main` at `db1e7f688`):

- `SnowflakeOptions.MachineId` (`src/Encina.IdGeneration/Configuration/SnowflakeOptions.cs` line 43) has no default other than 0; `Validate()` checks only the range `[0, MaxMachineId]`, where `MaxMachineId = (1L << ShardBits) - 1` (1023 with the default 10 bits). `SnowflakeIdGenerator` takes `(SnowflakeOptions, TimeProvider?)`, validates the options in its constructor and uses `_options.MachineId` in `Generate()` (line 78); there are 38 `new SnowflakeIdGenerator(` sites in 11 files.
- `AddEncinaIdGeneration` (`Extensions/ServiceCollectionExtensions.cs`) builds the generator eagerly with `new SnowflakeIdGenerator(options.SnowflakeOptions)` and registers the instance for `IIdGenerator<SnowflakeId>` and `IShardedIdGenerator<SnowflakeId>`; it registers no hosted service and no options.
- `IdGeneratorHealthCheck` takes `(IdGeneratorHealthCheckOptions?, SnowflakeOptions?, TimeProvider?)` and reports `snowflake_machine_id` from the options; it is created in `src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs` (line 497).
- `IdGenerationErrors` and `IdGenerationErrorCodes` are in core (`src/Encina/Abstractions/IdGeneration/`); codes have the form `encina.idgen.*`. `IdGenerationLog` uses ids 8030, 8040 and 8050 inside `IdGeneration = (8030, 8099)`.
- `Encina.IdGeneration` references `Encina` and `Encina.Messaging`; `Encina.DistributedLock` references the same two. No project references the other, so `Encina.DistributedLock` -> `Encina.IdGeneration` creates no cycle (only the database packages, `Encina.AspNetCore` and `Encina.OpenTelemetry` reference `Encina.IdGeneration`).
- No production code in `src/` consumes `IDistributedLockProvider` (ADR-052 Context); `ILockHandle` has `DateTime ExpiresAtUtc` and `ExtendAsync`. The SQL Server handle is built with `now.Add(expiry)` but the lock lives with its connection (`SqlServerDistributedLockProvider.cs` line 74; #2405). #2405 also records that the Redis provider-level `ExtendAsync(resource)` does not check ownership, so renewal must use the handle.
- The lock-messaging family owns `7500-7539` (`MessagingLocking`) in the plan-batch registry; `7500-7505` are used by the messaging lock plans (#713 to #715; 7506-7539 stay unassigned). This plan takes `7540-7549` (`MachineIdLease`), outside that block, for a new range registered in `src/Encina/Diagnostics/EventIdRanges.cs` (it has no entry yet).
- Manifest `.github/coverage-manifest/Encina.IdGeneration.json`: package targets guard 15, unit 60; `Configuration/SnowflakeOptions.cs` guard 90, property 50, contract 50; `Generators/SnowflakeIdGenerator.cs` guard 65, property 55, contract 18; `Health/IdGeneratorHealthCheck.cs` guard 95; `Extensions/ServiceCollectionExtensions.cs` guard 55.

---

## Design Choices

<details>
<summary><strong>1. Where the lease implementation lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In `Encina.IdGeneration`, which then references `Encina.DistributedLock`; `UseSnowflake(s => s.MachineIdSource = Lease)` is the only call** | One registration call, as in the issue's example | Every consumer of `Encina.IdGeneration` (the ten database packages, `Encina.AspNetCore`, `Encina.OpenTelemetry`) carries the lock package and, through it, the lock abstractions, whether or not it leases |
| **B) The abstraction and the startup check in `Encina.IdGeneration`; the lease implementation in `Encina.DistributedLock` (which references `Encina.IdGeneration`), registered by `AddEncinaMachineIdLease()`** | Pay-for-what-you-use: `Encina.IdGeneration` stays free of locks; the same split as the leader election of #717 (flag and abstraction in one package, implementation in `Encina.DistributedLock`) | Two calls (`UseSnowflake(Lease)` and `AddEncinaMachineIdLease()`), tied together by the startup check |
| **C) A new package, `Encina.IdGeneration.Lease`** | Neither package depends on the other | A new package (manifest, README, solution entry) for about six files |

### Chosen Option: **B — abstraction in IdGeneration, lease in DistributedLock** (recommended, pending the maintainer)

### Rationale

- B wins when most consumers of `Encina.IdGeneration` do not lease (the database packages use it for key generation); A wins when the one-call experience of the issue's example matters more than the dependency; C wins if `Encina.DistributedLock` should not depend on `Encina.IdGeneration`.
- The startup check reports a missing `AddEncinaMachineIdLease()` with the exact fix, so the second call cannot be forgotten silently, and its order relative to `AddEncinaIdGeneration` does not matter (the check runs at host start).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. How an unset machine id is detected</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `MachineIdSource` enum whose default is `Unspecified`; `Explicit` must be chosen on purpose** | "Neither configured" is a value, not an inference; the declaration is visible in the configuration | Every existing `UseSnowflake(s => s.MachineId = 1)` call must add the source (pre-1.0: acceptable; the call sites in `src/` and docs are updated in PR 1) |
| **B) Make `MachineId` nullable, or detect an assignment in its setter** | Existing code keeps working | `long?` breaks the code that reads `MachineId` as `long` (generator, health check, 38 construction sites in tests); setter detection hides a state in a property |

### Chosen Option: **A — an explicit source, `Unspecified` by default** (recommended, pending the maintainer)

### Rationale

- A wins for clarity and because the maintainer's rule is "with neither configured, startup fails": an enum makes "configured" a fact. B wins only when not touching existing call sites matters more than a clear model.
- Direct construction (`new SnowflakeIdGenerator(options)`, as the 38 test sites do) is not affected: the check lives in the DI path, because the generator itself cannot know whether it is one of several replicas.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How a single-instance deployment declares itself</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MachineIdSource.Explicit` is the declaration: an application that runs one instance sets `Explicit` and a `MachineId`** | Nothing new to build; works today; honest: the operator has chosen the id | A multi-instance deployment that copies the setting is not detected |
| **B) Wait for `EncinaDeployment` of the requirements validator (#2382) and read it** | One place to declare single-instance for every module | #2382 is open and has no plan; this issue would wait for it |
| **C) A flag on `SnowflakeOptions`, for example `SingleInstance`** | Explicit and local | A second declaration that the validator of #2382 would later duplicate |

### Chosen Option: **A — `Explicit` is the declaration; the validator of #2382 may read `EncinaDeployment` later** (recommended, pending the maintainer)

### Rationale

- A wins now because it delivers the maintainer's rule without a dependency; B wins if the maintainer wants every "single-instance" declaration in one place from the start; C wins if the source must stay independent of the validator forever.
- When #2382 lands its validator can additionally reject `Explicit` in a deployment declared multi-instance without an explicit per-instance value; nothing in this plan blocks that.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. What happens when the lease is lost or cannot be renewed</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Generate()` returns `Left` `encina.idgen.machine_id_unavailable` until the lease is regained (the service keeps trying to acquire any free id); the health check is unhealthy; generation resumes on its own** | A transient backend outage does not kill the process; no duplicate ids are possible while it lasts | Callers that generate ids see failures during the outage |
| **B) Stop the host** | The strongest guarantee; the orchestrator restarts the process | One lock outage restarts every replica |
| **C) Keep generating with the old id** | No failures | Another replica may hold the id after expiry: duplicate ids. Invalid under fail-closed |

### Chosen Option: **A — fail visibly, recover by itself** (recommended, pending the maintainer)

### Rationale

- A wins for services that tolerate short generation failures and restarting is costly; B wins for services where an id failure is worse than a restart; C is excluded by `AGENTS.md` section 3 (gates fail closed) and is not a valid option.
- Regaining the lease may give a different id; ids already generated stay valid, because the uniqueness of `(timestamp, machine id, sequence)` only needs one holder at a time and the local expiry check stops generation before the old lease can lapse on the backend.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Lease timing</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `LeaseDuration` 30 s, `RenewInterval` 10 s, `SafetyMargin` 5 s** | Two renewals can fail before the lease is lost; a crashed replica's id is free within 30 s on Redis | A pause longer than 25 s (stop-the-world) stops generation |
| **B) `LeaseDuration` 2 minutes, `RenewInterval` 30 s, `SafetyMargin` 10 s** | Tolerates long pauses | A crashed replica's id stays taken for up to 2 minutes; with 1,024 ids and fast autoscaling, exhaustion comes sooner |

### Chosen Option: **A — 30 s, 10 s, 5 s** (recommended, pending the maintainer)

### Rationale

- A wins on platforms with fast restarts and a bounded id space; B wins on platforms with long pauses (overcommitted virtual machines). All three values are options, validated: `RenewInterval` positive and less than a third of `LeaseDuration`, `SafetyMargin` positive and less than `LeaseDuration` minus two renew intervals.
- On SQL Server the lock lives with the connection and ignores the expiry (#2405): the id is freed when the connection drops, which is at least as fast as A.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Startup and acquisition failure semantics, and the order of ids tried</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `StartAsync` acquires before the host serves: a provider `Left` or an exhausted id space fails the start (`InvalidOperationException` naming the error code and ADR-050); ids are tried from a random offset, wrapping around, with no wait per attempt** | The issue's rule ("if all are taken, fail startup"); random offset avoids every replica contending for id 0 | Up to 1,024 provider calls when nearly all ids are taken |
| **B) Start without an id and acquire in the background** | The host starts at once | Id generation fails until the first acquire; start-up races in code that generates ids early |
| **C) Try ids in order from 0** | Deterministic, low ids first | Every replica starts at 0 and contends |

### Chosen Option: **A — acquire in `StartAsync`, fail start on a fault or exhaustion, random starting offset**

### Rationale

- The fault and exhaustion errors use the codes `encina.idgen.machine_id_unavailable` and `encina.idgen.machine_id_exhausted`, created with `ErrorCategory.Unavailable` (ADR-062); the exception text names the code, never `EncinaError.Message`. Every id in `[0, MaxMachineId]` is equivalent (`SnowflakeOptions.MaxMachineId`), so the order tried carries no meaning.

**Class:** Settled
**Settled by:** the maintainer's decision on #2394 (2026-10-17: lease or explicit; startup fails when neither is configured; the issue text: "if all are taken, fail startup"), `AGENTS.md` section 3 (gates fail closed; errors are never swallowed), ADR-052 (a fault is `Left`, contention is `Right(None)`), ADR-050 rule 3

</details>

---

## Implementation Phases

### Phase 1: The Source, the Abstraction and Fail-Closed Generation — PR 1a

> **Goal**: Everything in `Encina.IdGeneration` and core that does not need a lock.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.IdGeneration`

1. **`Configuration/MachineIdSource.cs`** (new) — `enum MachineIdSource { Unspecified = 0, Explicit = 1, Lease = 2 }`.
2. **`Abstractions/IMachineIdProvider.cs`** (new) — `Either<EncinaError, long> GetMachineId()` and `MachineIdLeaseSnapshot GetSnapshot()`; no I/O, never throws.
3. **`Abstractions/MachineIdLeaseSnapshot.cs`** (new) — `sealed record MachineIdLeaseSnapshot(MachineIdLeaseState State, long? MachineId, DateTime? AcquiredAtUtc, DateTime? ValidUntilUtc)` and `enum MachineIdLeaseState { NotAcquired, Held, Lost }`, both in this one file.
4. **`Configuration/SnowflakeOptions.cs`** (modify) — `MachineIdSource MachineIdSource { get; set; }` (default `Unspecified`), XML docs: `Explicit` uses `MachineId`, `Lease` needs `AddEncinaMachineIdLease()`.
5. **`Generators/SnowflakeIdGenerator.cs`** (modify) — an optional trailing constructor parameter `IMachineIdProvider? machineIdProvider = null`; `Generate()` uses `machineIdProvider.GetMachineId()` when present and returns its `Left`, otherwise `_options.MachineId` (the 38 construction sites stay valid). `Generate(string shardId)` is unchanged.

#### `src/Encina`

6. **`Abstractions/IdGeneration/IdGenerationErrorCodes.cs`** and **`IdGenerationErrors.cs`** (modify) — `MachineIdUnavailable = "encina.idgen.machine_id_unavailable"` and `MachineIdExhausted = "encina.idgen.machine_id_exhausted"`; factories `MachineIdUnavailable(string reasonCode)` and `MachineIdExhausted(long maxMachineId)` created with `ErrorCategory.Unavailable`, with a test asserting `GetCategory`.
7. **`PublicAPI.Unshipped.txt`** of `Encina.IdGeneration` and `Encina`, through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #2394: the machine id source, the provider abstraction and fail-closed generation.

CONTEXT:
- SnowflakeIdGenerator has 38 construction sites; the new constructor parameter must be optional and trailing.
- Generate(string shardId) encodes the shard in the machine-id bits and is NOT changed by this plan.
- ADR-062: create the new errors with ErrorCategory.Unavailable; never put exception or lock messages in an error.

TASK:
Create MachineIdSource, IMachineIdProvider, MachineIdLeaseSnapshot and the two error codes and factories, add the
MachineIdSource option and the optional provider to the generator as listed. Unit tests: provider Right gives ids with
that machine id, provider Left is returned by Generate() and no id is produced, no provider keeps today's behavior
(existing generator tests run unchanged), error category is Unavailable. A property test: for two generators fed by
providers returning different machine ids and one fake clock, no id is generated twice. Guard tests for the new
public constructor parameter and factory arguments.

KEY RULES:
- Pure and thread-safe: the generator already locks; the provider call happens inside Generate and must not allocate per call beyond the Either.
- XML docs on every public member; PublicAPI lines through mechanical-fixer.

REFERENCE FILES: src/Encina.IdGeneration/Generators/SnowflakeIdGenerator.cs, src/Encina.IdGeneration/Configuration/SnowflakeOptions.cs,
src/Encina/Abstractions/IdGeneration/IdGenerationErrors.cs, docs/architecture/adr/062-error-category-contract.md.
```

</details>

### Phase 2: Startup Validation, Registration and Health — PR 1b

> **Goal**: An unconfigured Snowflake generator fails the host; the health check reports the lease.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Configuration/SnowflakeMachineIdStartupCheck.cs`** (new, internal sealed, `IHostedService`, modelled on `DelayedRetryStartupCheck`) — in `StartAsync` throws `InvalidOperationException` naming the module (`Encina.IdGeneration`), the requirement (a unique machine id per instance), ADR-050 and the fix when: the source is `Unspecified`; or it is `Lease` and no `IMachineIdProvider` is registered. `Explicit` passes.
2. **`Extensions/ServiceCollectionExtensions.cs`** (modify) — when Snowflake is enabled, register the startup check (`TryAddEnumerable`) and, for `Lease`, register the generator by factory (`sp => new SnowflakeIdGenerator(options, sp.GetService<TimeProvider>(), sp.GetRequiredService<IMachineIdProvider>())`) instead of by instance; `Explicit` keeps the eager instance. Update the XML example to `MachineIdSource = MachineIdSource.Explicit`; the tests and docs that set `MachineId` and run a host are updated in the same pull request (the 21 `AddEncinaIdGeneration(` sites are listed by grep in the brief).
3. **`Health/IdGeneratorHealthCheck.cs`** (modify) — an optional trailing `IMachineIdProvider? machineIdProvider = null`; with a provider, the data gains `snowflake_machine_id_source`, `snowflake_lease_state`, `snowflake_lease_age_seconds`, and the status is `Unhealthy` while the state is `Lost` or `NotAcquired`.
4. **`src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs`** (modify) — resolve `IMachineIdProvider` with `GetService` and pass it (line 497).
5. **`PublicAPI.Unshipped.txt`** (IdGeneration, for the health check constructor).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #2394: startup validation, registration and the health check.

CONTEXT:
- ADR-050 rule 3: a missing requirement is a startup error, never a silent default. The check runs at host start, so the
  order of AddEncinaIdGeneration and AddEncinaMachineIdLease must not matter.
- AddEncinaIdGeneration builds the generator eagerly today; for the Lease source it must use a factory so the provider is
  resolved from DI.

TASK:
Create the startup check, change the registration and the health check, update the example and call sites that set MachineId
to also set MachineIdSource = Explicit. Unit tests: Unspecified fails, Explicit passes, Lease without a provider fails with
the exact fix in the message (no EncinaError text), Lease with a provider passes; a DI test building the provider with
ValidateOnBuild and ValidateScopes for each source in both registration orders; health check Healthy with a held lease,
Unhealthy when lost, data keys present.

KEY RULES:
- The exception text names the module, the requirement and the fix; it carries no secret and no error message.
- The generator registration must not capture a scoped service (all are singletons).

REFERENCE FILES: src/Encina.IdGeneration/Extensions/ServiceCollectionExtensions.cs, src/Encina.IdGeneration/Health/IdGeneratorHealthCheck.cs,
src/Encina.Messaging/Recoverability/DelayedRetryStartupCheck.cs, docs/architecture/adr/050-modules-may-require-modules.md.
```

</details>

### Phase 3: Diagnostics and the EventId Range — PR 2

> **Goal**: The range is registered before any message exists.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina/Diagnostics/EventIdRanges.cs`** (modify) — `public static readonly (int Min, int Max) MachineIdLease = (7540, 7549);`, with `Encina.DistributedLock` mapped to it (alongside its other ranges) in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (modify); `PublicAPI.Unshipped.txt` of `Encina` through `mechanical-fixer`.
2. **`src/Encina.DistributedLock/MachineId/MachineIdLeaseLog.cs`** (new, internal static partial, `[LoggerMessage]`, packed from 7540): 7540 `MachineIdAcquired` (Information: machine id), 7541 `MachineIdAcquireFailed` (Error: error code), 7542 `MachineIdExhausted` (Error: max machine id), 7543 `MachineIdRenewalFailed` (Warning: error code or exception type via `ForLogging()`), 7544 `MachineIdLeaseLost` (Warning), 7545 `MachineIdReacquired` (Information: machine id), 7546 `MachineIdReleaseFailed` (Warning: exception type).
3. **`src/Encina.DistributedLock/MachineId/MachineIdLeaseMetrics.cs`** (new) — on the shared `Meter("Encina")`: `Counter<long> encina.id_generation.machine_id.lease_events_total` with the bounded tag `event` (acquired, renewed, lost, exhausted, released) and an `ObservableGauge<double> encina.id_generation.machine_id.lease_remaining_seconds` reading the snapshot. No tenant tag: the lease is per process, not per tenant (ADR-054 D4 applies where a tenant exists).
4. **`.github/coverage-manifest/Encina.DistributedLock.json`** and **`Encina.json`** entries, through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #2394: the EventId range, log messages and metrics for the lease.

CONTEXT:
- ADR-021: register the range in EventIdRanges.cs before any [LoggerMessage] uses it, map the assembly in
  EncinaEventIdAllocationTests, add the field to PublicAPI.Unshipped.txt. Ids are packed from 7540 with no gaps; the range
  7540-7549 is MachineIdLease (this plan); the lock-messaging block 7500-7539 is not used by this plan; 7500-7505 belong to the messaging lock plans.
- AGENTS.md section 3: EncinaError.Message and exception messages never reach logs; log the code or the exception type.

TASK:
Register the range, create MachineIdLeaseLog and MachineIdLeaseMetrics as listed, and unit-test the metrics (every
event value, the gauge reading a fake snapshot, tags bounded). Run the architecture tests.

KEY RULES:
- No free text and no machine-id value in a metric tag; the machine id is allowed in a log field.

REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs,
src/Encina.IdGeneration/Diagnostics/IdGenerationMetrics.cs, src/Encina.IdGeneration/Diagnostics/IdGenerationLog.cs.
```

</details>

### Phase 4: The Lease Service — PR 3

> **Goal**: A process takes an id, keeps it, and stops generating when it cannot prove it holds it.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Encina.DistributedLock.csproj`** (modify) — a project reference to `Encina.IdGeneration`.
2. **`MachineId/MachineIdLeaseOptions.cs`** (new) — `LeaseDuration` (30 s), `RenewInterval` (10 s), `SafetyMargin` (5 s), and **`MachineIdLeaseOptionsValidator.cs`** (new, `IValidateOptions`) with the rules of Design Choice 5.
3. **`MachineId/MachineIdLeaseService.cs`** (new, public sealed, `IHostedService`, `IMachineIdProvider`, `IAsyncDisposable`) — constructor `(IDistributedLockProvider lockProvider, SnowflakeOptions snowflakeOptions, IOptions<MachineIdLeaseOptions> options, ILogger<MachineIdLeaseService> logger, TimeProvider? timeProvider = null)`. `StartAsync`: reads `now` **before** each call, tries ids from a random offset with `TryAcquireAsync(resource: $"encina:snowflake:machine:{n}", expiry: LeaseDuration, wait: zero, retry: 100 ms, ct)`; the first `Right(Some(handle))` wins, `Right(None)` tries the next id, a `Left` or exhaustion fails the start; then starts the renewal loop. The loop runs on a `PeriodicTimer(RenewInterval, timeProvider)`, catches every non-cancellation exception per iteration (a timer loop never lets an exception escape), calls `handle.ExtendAsync(LeaseDuration)` (never the provider-level extend, #2405): `Right(true)` sets `validUntil = readBefore + LeaseDuration`; `Right(false)`, `Left` or an exception moves the state to `Lost` and the loop tries to acquire any id again each interval. `GetMachineId()` is `Right(id)` while `now < validUntil - SafetyMargin`, otherwise `Left` `MachineIdUnavailable`. `StopAsync` stops the loop and disposes the handle (a dispose failure is logged by type, never thrown).
4. **`ServiceCollectionExtensions.cs`** (modify) — `AddEncinaMachineIdLease(this IServiceCollection services, Action<MachineIdLeaseOptions>? configure = null)`: options with the validator, the service as a singleton registered as `IMachineIdProvider` and `IHostedService` (one instance), `TryAdd*`.
5. **`PublicAPI.Unshipped.txt`** (DistributedLock) through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #2394: the machine id lease service in Encina.DistributedLock.

CONTEXT:
- #2667 (ADR-052) changes IDistributedLockProvider to Either<EncinaError, Option<ILockHandle>>; this phase starts after it.
- A PeriodicTimer loop catches per iteration and never lets an exception escape; cancellation from the stopping token ends
  the loop (AGENTS.md section 3). The lease expiry is computed from a time read BEFORE the acquire or extend call.
- Renewal must go through ILockHandle.ExtendAsync (the Redis provider-level extend does not check ownership, #2405).

TASK:
Create the options, validator, service and registration as listed. Unit tests with a substituted lock provider and a
FakeTimeProvider: first free id wins; a held id is skipped (Right(None)); all taken fails start with exhausted; a provider
Left fails start; renewal extends validUntil; a failed renewal moves to Lost and GetMachineId returns Left before the lease
could lapse; reacquire after loss; stop disposes the handle once; a throwing provider inside the loop does not escape and is
logged by type; options validation. A property test: two services over one in-memory provider never hold the same id.
A DI test with ValidateOnBuild and ValidateScopes. Guard tests for the public constructor and the extension.

KEY RULES:
- Never fall back to id 0 or to a stale id.
- No Thread.Sleep; drive the timer with FakeTimeProvider.Advance and let no exception escape the callback.

REFERENCE FILES: src/Encina.DistributedLock/ILockHandle.cs, src/Encina.DistributedLock.InMemory, docs/architecture/adr/052-distributed-lock-failure-contract.md,
docs/plans/leader-election-implementation-plan-717.md, docs/en/guides/TESTING.md.
```

</details>

### Phase 5: Cross-Cutting Integration

> **Goal**: Record what is integrated and what is not.

<details>
<summary><strong>Tasks</strong></summary>

1. Observability: the metrics and logs of Phase 3, the health data of Phase 2, the existing `IdGenerationActivitySource` unchanged (no per-id span).
2. Validation: options validator (Phase 4) and the startup check (Phase 2). Resilience: renewal failure stops generation (Design Choice 4); no Polly policy around the lock call.
3. The in-memory provider on several replicas: rejected by the validator of #2382, not duplicated here; the integration tests use real Redis and SQL Server providers.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are verifying Phase 5 of issue #2394: cross-cutting integration.

CONTEXT:
- Matrix of this plan; the lease is process-level, so no tenant, module or audit data exists to record.

TASK:
Confirm by tests that every log field and tag is bounded and free of error messages, and that the health check data
matches the lease snapshot.

KEY RULES:
- Codes and exception types only in logs and tags.

REFERENCE FILES: src/Encina.IdGeneration/Health/IdGeneratorHealthCheck.cs, src/Encina.DistributedLock/MachineId/.
```

</details>

### Phase 6: Testing — PRs 1a, 1b, 2, 3 and 4

> **Goal**: Every flag reaches its target; real providers prove two holders never share an id.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit, guard, property**: PRs 1a to 3 (files named in the phases).
2. **Contract** (PR 4): `MachineIdLeaseContractTests` runs the same scenarios (acquire, skip a held id, exhaustion, renewal, loss) against `MachineIdLeaseService` over the InMemory, Redis and SqlServer providers.
3. **Integration** (PR 4): `MachineIdLeaseRedisIntegrationTests` and `MachineIdLeaseSqlServerIntegrationTests` (shared `RedisFixture` and `SqlServerFixture` collections): start several services against one backend, assert distinct ids, release on stop makes the id available, a killed holder's id is reacquirable after expiry (Redis) or connection close (SQL Server). PostgreSQL (#207), MySQL (#208), Azure Blob (#209) and DynamoDB (#210) are added to the contract test when they exist.
4. **Load and benchmark**: justified in `.md` files (one acquire at start, one renewal per interval; `Generate()` adds a read of the snapshot).
5. **Per-file targets**:

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|-----:|------:|---------:|---------:|------------:|---------------|
| `IdGeneration/Configuration/SnowflakeOptions.cs` | 90 | 90 | 50 | 50 | 0 | Existing guard, contract and property targets kept; the new property adds unit cases |
| `IdGeneration/Generators/SnowflakeIdGenerator.cs` | 90 | 65 | 18 | 55 | 0 | Existing targets kept; the provider branch is unit and property testable |
| `IdGeneration/Configuration/SnowflakeMachineIdStartupCheck.cs` | 95 | 0 | 0 | 0 | 0 | Internal; every branch is a unit case; guard 0 for that reason |
| `IdGeneration/Extensions/ServiceCollectionExtensions.cs` | 90 | 55 | 0 | 0 | 0 | Existing guard target kept; DI tests cover the three sources |
| `IdGeneration/Health/IdGeneratorHealthCheck.cs` | 90 | 95 | 0 | 0 | 0 | Existing guard target kept; the lease states are unit cases |
| `IdGeneration/Abstractions/MachineIdLeaseSnapshot.cs` | 90 | 0 | 0 | 0 | 0 | A record; reached through the service and health tests; no parameters worth a guard test |
| `DistributedLock/MachineId/MachineIdLeaseService.cs` | 90 | 70 | 80 | 60 | 70 | Time and failures are driven by a fake clock and a fake provider; the contract and integration flags run three real providers |
| `DistributedLock/MachineId/MachineIdLeaseOptions.cs` and validator | 95 | 60 | 0 | 0 | 0 | Pure validation rules |
| `DistributedLock/MachineId/MachineIdLeaseMetrics.cs` and `MachineIdLeaseLog.cs` | 80 | 0 | 0 | 0 | 0 | The log class is source-generated and excluded; metrics are driven through the service; internal |
| `Encina/Abstractions/IdGeneration/IdGenerationErrors.cs` | 95 | 40 | 0 | 0 | 0 | Two factories with a category assertion |

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #2394: tests across lock providers.

CONTEXT:
- Integration tests use shared [Collection] fixtures (RedisFixture, SqlServerFixture) and never create a per-class fixture.
- Tests execute real package code and assert with Shouldly through Encina.Testing.Shouldly.

TASK:
Write the contract class over three providers, the two integration classes, and the .md justifications for load and
benchmark tests. Check every file against the per-file targets and report measured per-flag coverage.

KEY RULES:
- No Thread.Sleep; bounded polling with a timeout in integration tests, FakeTimeProvider elsewhere.
- Never call a fixture's DisposeAsync from a test.

REFERENCE FILES: tests/Encina.TestInfrastructure/RedisFixture.cs, tests/Encina.IntegrationTests, tests/Encina.ContractTests/IdGeneration,
docs/testing/integration-tests.md.
```

</details>

### Phase 7: Documentation and Finalization — PR 4

> **Goal**: Operators know how to run replicas safely.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on every new public member, with an example of `UseSnowflake(s => s.MachineIdSource = MachineIdSource.Lease)` plus `AddEncinaMachineIdLease()`.
2. `changelog.d/2394-snowflake-machine-id-lease.added.md` (and a `changed` fragment for the startup failure when the source is unspecified), through `mechanical-fixer`.
3. READMEs of `Encina.IdGeneration` and `Encina.DistributedLock`, a scaling guide page "Running Snowflake IDs on several replicas" (sources, lease timing, the shard overload limit), through `docs-writer`.
4. ADR: none new; ADR-050 already has the ID generation row.
5. Build and test verification: zero warnings, per-flag targets met.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are finishing issue #2394: documentation and verification.

CONTEXT:
- Documentation goes through docs-writer; fragments and PublicAPI lines through mechanical-fixer.

TASK:
Complete the XML docs, request the fragments and pages, run the build and tests, report per-flag coverage.

KEY RULES:
- State the limits: Generate(shardId) is outside the lease; the lease is a lease without fencing; Explicit is a declaration of
  single-instance (or of an operator-managed id).
- Write "the reference application", never a private client name.

REFERENCE FILES: changelog.d/README.md, src/Encina.IdGeneration/README.md, docs/testing/coverage-measurement-methodology.md.
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Relevance |
|--------|-----------|
| Maintainer decision on #2394 (2026-10-17) | Lease and Explicit; no derived ids; startup fails with neither; single-instance declared |
| ADR-050 | The ID generation row; requirement enforced by startup validation |
| ADR-052 | Lock outcomes as `Either`; handle renewal; contention is `Right(None)` |
| ADR-062 | `Unavailable` category of the new codes |
| Snowflake id layout (Twitter) | Uniqueness needs a unique (machine id, sequence) per millisecond; two holders of one machine id break it |
| Lease-based locks (Kleppmann, "How to do distributed locking") | The reason for the local expiry check and the safety margin |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `SnowflakeIdGenerator`, `SnowflakeOptions` | `src/Encina.IdGeneration/` | Where the source and provider are applied |
| `IdGeneratorHealthCheck` | `src/Encina.IdGeneration/Health/IdGeneratorHealthCheck.cs` | Extended with lease data |
| `IdGenerationErrors`, `IdGenerationErrorCodes` | `src/Encina/Abstractions/IdGeneration/` | Home of the two new codes |
| `IDistributedLockProvider`, `ILockHandle` | `src/Encina.DistributedLock/` | The engine and the renewal |
| `DelayedRetryStartupCheck` | `src/Encina.Messaging/Recoverability/DelayedRetryStartupCheck.cs` | Model of the startup check |
| Leader election service (#717 plan) | plans | Same renewal and local-expiry pattern |
| `RedisFixture`, `SqlServerFixture` | `tests/Encina.TestInfrastructure/` | Real providers in integration tests |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Lock provider | Lease supported | Notes |
|---------------|:-:|-------|
| InMemory | Tests only | Single process; rejected on several replicas by #2382 |
| Redis | Yes | Renewal through the handle (ownership check, #2405) |
| SqlServer | Yes | The lock lives with the connection and ignores `expiry` (#2405): renewal is effectively a no-op and the id frees when the connection drops |
| PostgreSQL (#207), MySQL (#208) | Pending | Added to the contract test when they land |
| Azure Blob (#209), DynamoDB (#210) | Not built | Added to the contract test when they land |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.DistributedLock` (new `MachineIdLease`) | 7540-7549 | From the block assigned to the lock-messaging family; this plan registers the range in PR 2 and uses 7540-7546; 7547-7549 stay unassigned |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production (new) | 9 | `MachineIdSource`, `IMachineIdProvider`, `MachineIdLeaseSnapshot` (record and state enum in one file), startup check, options, validator, service, metrics, log |
| Production (modified) | 10 | `SnowflakeOptions`, `SnowflakeIdGenerator`, IdGeneration `ServiceCollectionExtensions`, `IdGeneratorHealthCheck`, `HealthCheckBuilderExtensions`, `IdGenerationErrorCodes`, `IdGenerationErrors`, `EventIdRanges`, DistributedLock csproj and `ServiceCollectionExtensions` |
| `PublicAPI.Unshipped.txt` | 3 | IdGeneration, Encina, DistributedLock |
| Coverage manifests | 3 | IdGeneration, DistributedLock, Encina |
| Tests | 17 | Unit (7), guard (3), property (2), contract (1), allocation map (1), integration (2), health and DI (1) |
| Documentation | 7 | Two READMEs, one guide page, two fragments, two `.md` justifications |

PR split (each at most 15 files and 1500 lines, counting tests, `PublicAPI`, manifests, README and fragment; each builds and passes alone):

| PR | Content | Files | Lines (est.) |
|----|---------|------:|-------------:|
| 1a | Phase 1: source, provider abstraction, snapshot, generator parameter, errors and codes, tests, `PublicAPI` x2, manifests x2 | 15 | 800 |
| 1b | Phase 2: startup check, registration, health check, AspNetCore line, tests, `PublicAPI`, manifest | 10 | 750 |
| 2 | Phase 3: EventId range, allocation map, log, metrics, tests, `PublicAPI`, manifests (no dependency on #2667) | 9 | 500 |
| 3 | Phase 4: csproj reference, options, validator, service, registration, tests (unit, property, DI, guard), `PublicAPI`, manifest; starts after #2667 and PR 2 | 13 | 1,300 |
| 4 | Phases 6 and 7: contract and integration tests, READMEs, guide, fragments, justifications | 10 | 900 |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases of issue #2394</strong></summary>

```
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 / C# 14 library with Railway Oriented Programming (Either<EncinaError, T>). Issue #2394 makes
the Snowflake machine id unique per instance: a MachineIdSource (Unspecified, Explicit, Lease), a startup check that fails
when it is not configured, and a lease on IDistributedLockProvider that Generate() consults. Read AGENTS.md sections 3, 6,
7 and 9 and ADR-050, ADR-052, ADR-062 and the maintainer's decision on #2394 first.

IMPLEMENTATION OVERVIEW:
1. Encina.IdGeneration: MachineIdSource, IMachineIdProvider, snapshot, optional provider in SnowflakeIdGenerator (Left when
   unavailable), startup check, factory registration for Lease, health data. Core: two error codes (Unavailable).
2. EventId range MachineIdLease 7540-7549 (7540-7546 used), log and metrics.
3. Encina.DistributedLock: MachineIdLeaseService (IHostedService + IMachineIdProvider): acquire encina:snowflake:machine:{n}
   from a random offset in StartAsync, renew with ILockHandle.ExtendAsync on a PeriodicTimer, local expiry from a time read
   before the call, Left while the lease cannot be trusted, release on stop; AddEncinaMachineIdLease.
4. Contract tests over three providers, integration tests with Redis and SQL Server, docs through specialists.

KEY PATTERNS:
- Fail closed, never id 0 or a stale id; timer loops catch per iteration and never throw; log codes and exception types only.
- Optional trailing constructor parameters (38 generator sites); TimeProvider for every clock read; no Thread.Sleep.
- Generate(shardId) is outside the lease and documented as such.

REFERENCE FILES: src/Encina.IdGeneration/Generators/SnowflakeIdGenerator.cs, src/Encina.IdGeneration/Extensions/ServiceCollectionExtensions.cs,
src/Encina.DistributedLock/ILockHandle.cs, docs/plans/leader-election-implementation-plan-717.md, src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: no read path to cache |
| 2 | OpenTelemetry | ✅ | Phase 3: lease event counter and remaining-seconds gauge; no tenant tag (a process-level lease, ADR-054 D4 applies where a tenant exists) |
| 3 | Structured Logging | ✅ | Phase 3: `[LoggerMessage]` 7540-7546 in `MachineIdLease`, registered first |
| 4 | Health Checks | ✅ | Phase 2: `IdGeneratorHealthCheck` reports the leased id, lease state and age; unhealthy when lost |
| 5 | Validation | ✅ | Phases 2 and 4: startup check, options validator |
| 6 | Resilience | ✅ | Design Choice 4: a failed renewal stops generation visibly; the margin absorbs one missed renewal; no Polly policy around the lock call |
| 7 | Distributed Locks | ✅ | The mechanism |
| 8 | Transactions | ❌ | Not applicable |
| 9 | Idempotency | ❌ | Not applicable |
| 10 | Multi-Tenancy | ❌ | Not applicable: the machine id is per process, not per tenant |
| 11 | Module Isolation | ❌ | Not applicable |
| 12 | Audit Trail | ❌ | Not applicable: no compliance-relevant operation |

---

## Prerequisites & Dependencies

- **#2667** (ADR-052 implementation) before PR 3 (the service is written against `Either<EncinaError, Option<ILockHandle>>`); PRs 1a, 1b and 2 do not use the lock interface. It is open and not a pending maintainer decision.
- **#2405** (Redis extend without an ownership check, SQL Server ignoring expiry) is not a prerequisite: renewal uses the handle, and the SQL Server behavior is safe for a lease (the lock lives with the connection). Its fix improves the Redis integration test only.
- **#2382** (requirements validator) is not a prerequisite (Design Choice 3, option A); #207 and #208 are not prerequisites (the contract test grows when they land).
- **Out-of-scope finding** (issue file `artifacts/issues/snowflake-shard-overload-replica-collision.md`): `Generate(string shardId)` writes the caller's shard id into the machine-id bits, so replicas that generate ids for the same shard can collide whatever the lease says.
- Not blocked by another issue's open maintainer decision; the dilemmas above are this issue's own.

## Next Steps

1. Open the plan PR; when work on #2394 starts, ask the maintainer to answer Dilemmas 1 to 5 and record them in `## Maintainer Decisions`.
2. Brief PR 1a first (no dependency), then PR 1b and PR 2; PR 3 after #2667.
