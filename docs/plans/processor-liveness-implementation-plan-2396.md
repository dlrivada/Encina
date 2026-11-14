# Implementation Plan: Processor Liveness Health Checks

> **Issue**: [#2396](https://github.com/dlrivada/Encina/issues/2396)
> **Type**: Feature
> **Complexity**: High (6 phases, 70 file changes, split into 8 pull requests of 13, 14, 9, 5, 5, 11, 11 and 2 files)
> **Estimated Scope**: about 600 lines of production code, about 1,100 lines of tests
> **Milestone**: Encina 1.0 (messaging, health checks)

---

## Summary

The Outbox, Inbox, Saga and Scheduling health checks measure backlog only. A processor that crashed its loop, hangs on an await or fails every cycle leaves a small or empty backlog and looks healthy. This plan adds a shared heartbeat that each background processor records through a `TimeProvider`, and a liveness health check that turns "no cycle completed in time" into Unhealthy and "cycles keep failing" into Degraded.

The shape follows the maintainer's settled comment on the issue (2026-10-17): a shared `IProcessorHeartbeat` recorded by each processor plus `AddEncinaProcessorLiveness`, reusing #2254 for Recoverability.

Re-checked against `main`:

- No `IProcessorHeartbeat`, `AddEncinaProcessorLiveness` or `encina.processor.*` metric exists in `src/`.
- Processors in the issue's scope: `OutboxProcessorBase` (derived by the `OutboxProcessor` of ADO.NET SqlServer/PostgreSQL/MySQL, Dapper SqlServer/PostgreSQL/MySQL, EntityFrameworkCore and MongoDB; its loop is `sealed`, so the heartbeat is added once in the base), `ScheduledMessageProcessor`, `DelayedRetryProcessor`, `CdcProcessor` and `ShardedCdcProcessor`, and the compliance monitors `BreachDeadlineMonitorService`, `RetentionEnforcementService`, `DPIAReviewReminderService` and `TransferExpirationMonitor` (the last is the cross-border expiry monitor, which the issue's list does not name; Design Choice 7). `Encina.Compliance.ProcessorAgreements` has no background service, so there is nothing to instrument there.
- Four processors return from `ExecuteAsync` immediately when disabled (`EnableProcessor`, `EnableDelayedRetries`, `CdcOptions.Enabled`), so "never started" must not read as stale (Design Choice 4).
- The compliance packages (`Retention`, `BreachNotification`, `DPIA`, `CrossBorderTransfer`) do not reference `Encina.Messaging`; they reference `Encina` (directly or through `Encina.Caching` / `Encina.Marten`). `Encina.Cdc` and `Encina.Security.Audit` reference `Encina.Messaging`.

**Affected packages:**

- `Encina` (core) - `IProcessorHeartbeat`, `IProcessorHeartbeatRegistry`, snapshot type, the registry and its gauge. The issue's affected-package list does not tick core; it is needed because four compliance packages cannot see `Encina.Messaging` (Design Choice 1).
- `Encina.Messaging` - the liveness check and its log class; heartbeat in `OutboxProcessorBase`, `ScheduledMessageProcessor`, `DelayedRetryProcessor`.
- `Encina.AspNetCore` - `AddEncinaProcessorLiveness` next to `AddEncinaOutbox`.
- `Encina.Cdc` and the four compliance packages - heartbeat in their processors.
- `Encina.OpenTelemetry` - no change; it already adds the shared `Encina` meter.

**Provider category**: none. The heartbeat sits in the shared processor bases, so the 10 database providers inherit it. The eight `OutboxProcessor` subclasses are not modified; integration tests prove that a real processor over a real store reports liveness (Design Choice 12 decides one test per family or one per provider; Phase 6, PR 4).

**Sibling plans (health-checks family)**: #454 owns the `EncinaHealthTags.Live` constant and the probe endpoints; the processor checks carry a tag the host can add to the liveness set. #622 owns the per-check health gauge; this plan owns only `encina.processor.heartbeat_age_seconds`. #754 owns the shared `ProviderHealthCheckOptions`. #2254 (Recoverability health and OpenTelemetry) is reused for the liveness part of the delayed-retry processor; its backlog check and metrics stay with it.

---

## Design Choices

<details>
<summary><strong>1. Where the heartbeat abstraction lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In core `Encina` (`Encina.Diagnostics`)** | Every processor package can reference it: `Retention`, `BreachNotification`, `DPIA` and `CrossBorderTransfer` reference `Encina` but not `Encina.Messaging`; the registry and gauge need no messaging types | Adds public API to the core package |
| **B) In `Encina.Messaging`** (next to the existing health base) | Same package as the check and the Messaging processors; no core change | Four compliance packages would gain a `ProjectReference` to `Encina.Messaging` only for an interface, coupling compliance to messaging |

### Chosen Option: **A - in core `Encina`** (recommended, pending the maintainer)

### Rationale

- The check itself stays in `Encina.Messaging/Health` beside `OutboxHealthCheck` (it needs `IEncinaHealthCheck`); only the small recording contract lives in core.
- Option B wins when core must stay free of any new public type and the maintainer accepts the extra references.
- Class: Dilemma

</details>

<details>
<summary><strong>2. One check or one entry per processor</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `ProcessorLivenessHealthCheck` aggregating every registered heartbeat**, with a data entry per processor | The registry is filled at run time, so a static list of entries would have to be built when the health options are first resolved and could miss a late-starting processor; simple | One tag set for all processors: a host cannot make only the outbox `live` |
| **B) One health-check entry per processor** (`encina-processor-outbox`, ...) created from the registry when `HealthCheckServiceOptions` is first built | Per-processor names and tags; matches the issue's "one entry per registered processor" | Entries are fixed on first resolution; a processor that registers later is missed; needs a `Configure<HealthCheckServiceOptions>` that reads a run-time registry |

### Chosen Option: **A - one aggregating check** (recommended, pending the maintainer)

### Rationale

- Processors register when they are constructed (Design Choice 10), and hosted services are constructed before the first probe, but the guarantee is an ordering detail of the host, not a contract; A has no ordering dependency.
- Option B wins when a host must tag individual processors `live` (for example restart the pod when only the outbox dies); the plan then derives the entries from the registry's names at first resolution and documents the limitation.
- Class: Dilemma

</details>

<details>
<summary><strong>3. Mechanism: shared heartbeat and one check</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Shared `IProcessorHeartbeat` recorded by each processor plus one liveness check** | One mechanism, uniform thresholds | Touches every processor |
| **B) Ad hoc per-processor checks** | Local changes | Inconsistent, as today |
| **C) Rely on process liveness of the hosting platform** | Nothing to build | A hosted service can stop or hang while the process stays up |

### Chosen Option: **A - shared heartbeat and one check**

### Rationale

- This is the answer the maintainer recorded on the issue.
- Class: Settled
- Settled by: #2396 (the maintainer's comment of 2026-10-17, "Settled by elimination") and AGENTS.md section 6 (function 4).

</details>

<details>
<summary><strong>4. Status rules, defaults and disabled processors</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Unhealthy when a polling processor has completed no cycle within `StaleCycleMultiplier` x its interval (default 3) measured from the later of registration and the last completion, or has had a cycle in flight longer than that window; Degraded after `ConsecutiveFailureThreshold` failed cycles (default 3); a disabled processor reports Healthy with `state=disabled`; when a processor is both stale and failing the worst status (Unhealthy) wins, and the check reports the worst status over all processors** | Matches the issue text; a hung await is stale; the first cycle gets a grace period from registration | Two defaults (3 and 3) with no citation beyond the issue wording; two options to tune |
| **B) Unhealthy on the first failed cycle** | Simplest | One transient store error flips readiness-neutral health and alerts |

### Chosen Option: **A - stale multiple, consecutive failures, disabled is Healthy** (recommended, pending the maintainer)

### Rationale

- A cycle is one iteration of the loop: it completes when the loop body returns or its caught exception is recorded; the failure is recorded as the exception type name, never the message.
- The issue states "a configurable multiple" and "cycles complete with failures" but gives no numbers; the defaults 3 and 3, the grace period from registration, "disabled is Healthy" and "worst status wins" are user-visible defaults with user consequences, so they are recorded as a Dilemma (AGENTS.md section 3 bars exception messages in results; `src/Encina.Messaging/Health/OutboxHealthCheck.cs` classifies by thresholds, not by these values).
- Streaming and retry semantics are Design Choice 11; naming is Design Choice 9; never-started and empty registry are Design Choice 10.
- Class: Dilemma

</details>

<details>
<summary><strong>5. How a processor reaches the registry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An optional dependency: processors with an `IServiceProvider` resolve `IProcessorHeartbeatRegistry` with `GetService`; the others take `IProcessorHeartbeatRegistry? heartbeats = null`** | No registry means no heartbeat and no cost; the eight provider `OutboxProcessor` classes keep their constructors because the base already holds the provider | Optional constructor parameters on four processors |
| **B) A required dependency registered by default** | No null handling | Every processor pays for the heartbeat; breaks every constructor |

### Chosen Option: **A - optional dependency**

### Rationale

- `AddEncinaProcessorLiveness` registers the registry; without it nothing records.
- Option B is excluded by the opt-in rule of AGENTS.md section 1 (every feature is opt-in, pay-for-what-you-use); a trailing optional parameter follows the existing constructor pattern of these processors (an optional `TimeProvider`), so existing construction sites compile unchanged.
- Class: Trivial

</details>

<details>
<summary><strong>6. Tags and readiness</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Tags `encina`, `processor`; no `ready` tag by default**, like the outbox check | A restart-grade signal never silently takes an instance out of readiness; the host adds `live` (from #454) when it wants a restart | The host must opt in to either probe |
| **B) Tag `ready` by default** | Visible in the readiness set | A stuck processor removes the instance from the load balancer without the host asking; the same trap #2138 shows for scheduling |

### Chosen Option: **A - no `ready` by default**

### Rationale

- The issue states the check is readiness-neutral like the outbox check (`HealthCheckBuilderExtensions` keeps `DefaultTagsWithoutReady`); Option B is the trap #2138 shows for scheduling.
- Class: Settled
- Settled by: #2396 (the issue text names the outbox-style readiness-neutral tagging) and #2138.

</details>

<details>
<summary><strong>7. Which processors are in scope</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The issue's list plus the sibling cross-border expiry monitor; the other background services in a follow-up issue** | Bounded pull requests; every named processor covered | Dead-letter cleanup, message purge, audit retention, break-the-glass and sharding services stay without a heartbeat until the follow-up |
| **B) Every `BackgroundService` in `src/`** (about 27 files) | Complete | Oversized; crosses many packages in one family of pull requests |

### Chosen Option: **A - the issue's list, the follow-up issue for the rest** (recommended, pending the maintainer)

### Rationale

- The registry is generic, so the follow-up is mechanical: each service adds three lines.
- Option B (every `BackgroundService`, split across more pull requests) also satisfies #2396 and the size rule of AGENTS.md section 11, and Option A adds `TransferExpirationMonitor`, which the issue does not list; both are scope choices for the maintainer.
- Follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/processor-heartbeat-remaining-services.md).
- Class: Dilemma

</details>

<details>
<summary><strong>8. The heartbeat age metric</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An observable gauge `encina.processor.heartbeat_age_seconds` on the shared `Encina` meter, created by the registry, tag `processor`** | Same pattern as `OutboxProcessorMetrics`; `Encina.OpenTelemetry` already adds the `Encina` meter; no work when nobody scrapes | The gauge exists only when the registry is registered |
| **B) Compute the age only inside the health check** | No meter code | Not visible to dashboards between probes |

### Chosen Option: **A - an observable gauge from the registry**

### Rationale

- The age is computed from the snapshot and `TimeProvider` at scrape time, so no timer runs.
- The metric name comes from #2396; the pattern follows `src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs`.
- Class: Trivial

</details>

<details>
<summary><strong>9. Processor names and collisions</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Register` returns a distinct handle per call; a name that is already registered gets the suffix `#2`, `#3`, ... (never shared, never throws)** | One processor cannot mask another; the gauge tag cardinality is bounded by the number of processor instances | Suffixes in the tag when two instances share a name |
| **B) Register is idempotent per name (the second call returns the same handle)** | Simple | Two CDC connectors, `ShardedCdcProcessor` plus `CdcProcessor`, or two outbox processors would share one heartbeat and mask each other's failure |
| **C) A duplicate name fails registration** | No ambiguity | A processor failing to register means no heartbeat at all, the failure the feature exists to prevent |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- Names are built by the processor: `outbox:<concrete type name>`, `scheduled-messages`, `delayed-retry`, `cdc:<connector id>`, `cdc-sharded:<connector id>`, and the four compliance service names; they are tags and never user data.
- Class: Dilemma

</details>

<details>
<summary><strong>10. Never started, registration time and an empty registry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Processors register in their constructor (host start); a processor registered but whose `ExecuteAsync` never reaches its first cycle becomes Unhealthy after the grace period; an empty registry reports Healthy with `processors=0`** | Catches "crashed before the first cycle" (the issue's failure); a web-only node with no processor is not Unhealthy | A hosted service that is never registered with the host is invisible (documented) |
| **B) As A, but an empty registry reports Unhealthy** | Fail closed when the liveness check is wired but nothing is monitored | A host with no processors fails its probe forever |
| **C) Register inside `ExecuteAsync`** | Registration only for processors that ran | A service whose `ExecuteAsync` throws before registering is absent and the check is Healthy: the exact failure the issue targets |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- Option C is excluded by the issue's own failure statement; the choice between A and B (whether the empty state fails open or closed) is a user-visible default, so it is the maintainer's. Tests: a processor whose `ExecuteAsync` throws before its first cycle turns Unhealthy; an empty registry is Healthy under A.
- Class: Dilemma

</details>

<details>
<summary><strong>11. Streaming loops and retry delays</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Register(name, interval, kind)` with `ProcessorHeartbeatKind.Polling` or `Streaming`; a Streaming processor in flight is not stale (an in-flight cycle may legitimately last as long as the stream), it is stale only when no cycle is in flight and none started or completed within the window; `RecordCycleFailed(failureType, retryAfter)` extends the window by the backoff delay** | A healthy `CdcProcessor` over a long-running `StreamChangesAsync` (and `ShardedCdcProcessor`) is never Unhealthy; retry and dead-letter delays longer than `StaleCycleMultiplier` x `PollingInterval` (`CalculateRetryDelay`) do not trigger false alerts | A hang inside a streaming `await foreach` is not detected by time alone (documented) |
| **B) Every processor is treated as polling** | One rule | A streaming connector would turn Unhealthy while healthy |
| **C) Streaming processors report progress per dispatched change event** | Detects a hang | An idle stream produces no events and would turn stale; needs connector keepalive support that does not exist |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- Verified in `src/Encina.Cdc/Processing/CdcProcessor.cs`: `ProcessChangesAsync` is an `await foreach` over `connector.StreamChangesAsync` (line 138) and the retry delay is `CalculateRetryDelay(consecutiveErrors)`; `CdcProcessor` has no `TimeProvider`, and it holds an `IServiceProvider`, so the heartbeat is resolved from it without a constructor change.
- A test with a long-running stream proves a healthy streaming processor stays Healthy for several multiples of the interval.
- Class: Dilemma

</details>

<details>
<summary><strong>12. Provider coverage of the integration tests</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One integration test per family (ADO.NET, Dapper, EF Core, MongoDB)** | Four files; the heartbeat is in the shared `OutboxProcessorBase`, so the matrix proves inheritance, not provider SQL | The ADO/Dapper/EF families span three databases each and only one runs |
| **B) One integration test per provider (10 files)** | Meets "tests of a provider-dependent feature cover all 10 providers" literally; the issue says each provider verifies liveness over its store | Ten near-identical files for a base-class behavior |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- AGENTS.md section 9 requires 10 providers for provider-dependent features; the heartbeat is in a provider-independent base class, which is the argument for A, but the issue text says "database providers verify through their integration tests", which is the argument for B. PR 4 holds the tests (4 files under A, 10 under B; both at most 15).
- Class: Dilemma

</details>

---

## Implementation Phases

### Phase 1: Core abstractions and registry (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 13 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina/Diagnostics/IProcessorHeartbeat.cs` | new |
| 2 | `src/Encina/Diagnostics/IProcessorHeartbeatRegistry.cs` | new |
| 3 | `src/Encina/Diagnostics/ProcessorHeartbeatSnapshot.cs` | new |
| 4 | `src/Encina/Diagnostics/ProcessorHeartbeatKind.cs` | new |
| 5 | `src/Encina/Diagnostics/ProcessorHeartbeatRegistry.cs` | new (internal sealed) |
| 6 | `src/Encina/Diagnostics/ProcessorLivenessOptions.cs` | new |
| 7 | `src/Encina/Diagnostics/ProcessorHeartbeatServiceCollectionExtensions.cs` | new (`AddEncinaProcessorHeartbeat`) |
| 8 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 9 | `.github/coverage-manifest/Encina.json` | modify (`mechanical-fixer`) |
| 10 | `tests/Encina.UnitTests/Diagnostics/ProcessorHeartbeatRegistryTests.cs` | new (registry, options validation, DI test with `ValidateOnBuild` and `ValidateScopes`) |
| 11 | `tests/Encina.GuardTests/Diagnostics/ProcessorHeartbeatGuardTests.cs` | new |
| 12 | `tests/Encina.ContractTests/Diagnostics/ProcessorHeartbeatContractTests.cs` | new (a test double and the real registry honor the same behavior) |
| 13 | `changelog.d/2396-processor-heartbeat.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `IProcessorHeartbeat`: `RecordCycleStarted()`, `RecordCycleCompleted()`, `RecordCycleFailed(string failureType, TimeSpan? retryAfter = null)` (Design Choice 11), `MarkDisabled()`; none ever throws.
2. **Create** `IProcessorHeartbeatRegistry`: `IProcessorHeartbeat Register(string processorName, TimeSpan expectedInterval, ProcessorHeartbeatKind kind = ProcessorHeartbeatKind.Polling)` (a duplicate name receives the suffix `#2`, `#3`, ..., Design Choice 9) and `IReadOnlyList<ProcessorHeartbeatSnapshot> GetSnapshots()`.
3. **Create** `ProcessorHeartbeatSnapshot` (a record with `ProcessorName`, `ExpectedInterval`, `Kind`, `RegisteredAtUtc`, `LastCycleStartedAtUtc`, `LastCycleCompletedAtUtc`, `CycleInFlight`, `RetryAfter`, `ConsecutiveFailures`, `LastFailureType`, `IsDisabled`; UTC timestamps with the `AtUtc` suffix) and the `ProcessorHeartbeatKind` enum (`Polling`, `Streaming`).
4. **Create** the registry: `internal sealed`, takes `TimeProvider`, thread-safe (one lock per handle or `Interlocked` snapshots), creates the gauge on the shared `Encina` meter (`encina.processor.heartbeat_age_seconds`, unit `s`, tag `processor`).
5. **Create** `ProcessorLivenessOptions` (`StaleCycleMultiplier` 3.0, `ConsecutiveFailureThreshold` 3, validated greater than 1 and at least 1; Design Choice 4) and `AddEncinaProcessorHeartbeat(this IServiceCollection, Action<ProcessorLivenessOptions>? configure = null)` that `TryAdd`s the registry and registers the options with `ValidateOnStart`. The single source of the options is `IOptions<ProcessorLivenessOptions>`: the check never takes an options instance of its own.
6. **Tests and manifest in this PR**: register (duplicate names get distinct handles), snapshots, the gauge value on a `FakeTimeProvider`, thread safety, options validation, the DI test, guard tests for every public method and constructor (null `processorName`, empty or whitespace name, non-positive interval), the contract test. Manifest per-file targets with one-sentence justifications in `Encina.json`: `ProcessorHeartbeatRegistry.cs` (unit 90, guard 15, contract 60), `ProcessorLivenessOptions.cs` (unit 90, guard 0 because the class has no method parameters), `ProcessorHeartbeatServiceCollectionExtensions.cs` (unit 85, guard 15), the interfaces, the enum and the snapshot (unit and contract 0 where they hold no executable code, with that reason). Property: justified (threshold arithmetic is covered by the unit tests); load: justified (the registry takes one lock per cycle per processor); benchmark: justified (not a hot path); each manifest `reason` says so.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library. Issue #2396 adds a shared processor heartbeat. The abstraction lives in core Encina
because the compliance packages do not reference Encina.Messaging. The shared meter is named "Encina"
(src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs shows the observable gauge pattern).

TASK:
Create IProcessorHeartbeat, IProcessorHeartbeatRegistry, ProcessorHeartbeatSnapshot, the internal registry with its
observable gauge, ProcessorLivenessOptions with validation, and AddEncinaProcessorHeartbeat. Build with zero warnings.

KEY RULES:
- Time comes from TimeProvider (constructor parameter, default TimeProvider.System); never DateTime.UtcNow.
- Record the failure type name only, never an exception message.
- Thread-safe: processors record from background threads while probes read snapshots.
- Options validated at startup (ValidateOnStart); registration with TryAdd.
- XML docs with examples; sealed; nullable enabled; PublicAPI.Unshipped.txt updated.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs
- src/Encina/Diagnostics/EncinaDiagnostics.cs
- src/Encina.Messaging/Health/OutboxHealthCheck.cs (options and classification style)
- .github/coverage-manifest/Encina.json
```

</details>

### Phase 2: Observability and structured logging (pull request 2, with Phase 3)

<details>
<summary><strong>Tasks</strong></summary>

The files of Phases 2 and 3 form PR 2 (table in Phase 3); this phase lists the logging tasks.

1. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: register `public static readonly (int Min, int Max) MessagingProcessorLiveness = (7350, 7369);` from the family block 7350-7399.
2. **Modify** `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`: add `nameof(EventIdRanges.MessagingProcessorLiveness)` to the `Encina.Messaging` entry of `AssemblyRanges` (line 91).
3. **Create** `src/Encina.Messaging/Health/ProcessorLivenessLog.cs`: `[LoggerMessage]` methods `ProcessorStale` (7350, Warning: name, seconds since last cycle), `ProcessorFailing` (7351, Warning: name, consecutive failures, failure type), `ProcessorRecovered` (7352, Information: name), `ProcessorDisabled` (7353, Information: name, logged once). The log class is created in PR 2, the first PR that logs. `src/Encina/PublicAPI.Unshipped.txt` gets the new range field.
4. Logging happens on a status transition only (the check keeps the previous status per processor), so a probe every few seconds does not flood the log.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
The family block 7350-7399 is free; this plan takes 7350-7369. EventIdRanges is the source of truth for ranges and the
EncinaEventIdAllocationTests AssemblyRanges map must list the new range for Encina.Messaging.

TASK:
Register MessagingProcessorLiveness, update the architecture test map, and create ProcessorLivenessLog with the four
messages above (EventIds 7350-7353, packed).

KEY RULES:
- Register the range before using an EventId; source generator only; no sparse allocation.
- Log the exception type through ForLogging(); never a message.
- Add the range field to PublicAPI.Unshipped.txt of Encina.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina.Messaging/Diagnostics/SchedulingProcessorLog.cs
```

</details>

### Phase 3: Liveness check and registration (pull request 2); Messaging processors (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 14 files (PR 1 is merged first):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (`MessagingProcessorLiveness`) |
| 2 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 3 | `.github/coverage-manifest/Encina.json` | modify (`mechanical-fixer`) |
| 4 | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify |
| 5 | `src/Encina.Messaging/Health/ProcessorLivenessLog.cs` | new |
| 6 | `src/Encina.Messaging/Health/ProcessorLivenessHealthCheck.cs` | new |
| 7 | `src/Encina.Messaging/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 8 | `.github/coverage-manifest/Encina.Messaging.json` | modify (`mechanical-fixer`) |
| 9 | `src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs` | modify |
| 10 | `src/Encina.AspNetCore/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 11 | `.github/coverage-manifest/Encina.AspNetCore.json` | modify (`mechanical-fixer`) |
| 12 | `tests/Encina.UnitTests/Messaging/Health/ProcessorLivenessHealthCheckTests.cs` | new (check thresholds, builder extension tags, DI test with `ValidateOnBuild` and `ValidateScopes`) |
| 13 | `tests/Encina.GuardTests/Messaging/Health/ProcessorLivenessGuardTests.cs` | new |
| 14 | `changelog.d/2396-processor-liveness-check.added.md` | new (`mechanical-fixer`) |

Tasks (PR 2):

1. **Create** `ProcessorLivenessHealthCheck`: `EncinaHealthCheck` (`DefaultName = "encina-processor-liveness"`, tags `encina`, `processor`), constructor `(IProcessorHeartbeatRegistry registry, IOptions<ProcessorLivenessOptions> options, TimeProvider? timeProvider = null, ILogger<...>? logger = null)`; per processor, by the rules of Design Choices 4, 10 and 11: disabled gives Healthy; a Polling processor with no completed cycle within the window since the later of registration and last completion, or a cycle in flight longer than the window, gives Unhealthy; a Streaming processor is Unhealthy only when no cycle is in flight and none started or completed within the window; a failed cycle extends the window by its `retryAfter`; `ConsecutiveFailures >= threshold` gives Degraded; the worst status over all processors wins; an empty registry is Healthy with `processors=0` (Design Choice 10); `Data` holds one entry per processor (`state`, `seconds_since_last_cycle`, `consecutive_failures`, `last_failure_type`); no message text.
2. **Modify** `HealthCheckBuilderExtensions.cs`: `AddEncinaProcessorLiveness(this IHealthChecksBuilder, string? name = null, Action<ProcessorLivenessOptions>? configure = null, IEnumerable<string>? tags = null, HealthStatus? failureStatus = null)` that calls `AddEncinaProcessorHeartbeat(configure)` (options flow only through `IOptions<ProcessorLivenessOptions>`, validated by `ValidateOnStart`) and registers the check without the `ready` tag (via `CombineTagsWithoutReady`).
3. **Tests and manifests in this PR**: stale, hung cycle, streaming in flight, retry delay longer than the window, consecutive failures, disabled, grace period, recovery, empty registry; no message text; per-file targets with one-sentence justifications: `ProcessorLivenessHealthCheck.cs` (unit 90, guard 15), `ProcessorLivenessLog.cs` (unit 70, guard 0 because generated logger bodies run only on a status transition), `HealthCheckBuilderExtensions.cs` (keeps its targets, unit raised for the new method), `EventIdRanges.cs` (unit 90 through the allocation test, guard 0: static fields only).

PR 3 holds exactly these 9 files (the three Messaging processors; PR 2 is merged first):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Messaging/Outbox/OutboxProcessorBase.cs` | modify |
| 2 | `src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs` | modify |
| 3 | `src/Encina.Messaging/Recoverability/DelayedRetryProcessor.cs` | modify |
| 4 | `src/Encina.Messaging/PublicAPI.Unshipped.txt` | modify (the two optional constructor parameters; `mechanical-fixer`) |
| 5 | `.github/coverage-manifest/Encina.Messaging.json` | modify (`mechanical-fixer`) |
| 6 | `tests/Encina.UnitTests/Messaging/Processors/ProcessorHeartbeatTests.cs` | new (the three processors with a fake registry) |
| 7 | `tests/Encina.GuardTests/Messaging/Scheduling/ScheduledMessageProcessorGuardTests.cs` | modify (the nullable `heartbeats` parameter accepts null: a valid-result test) |
| 8 | `tests/Encina.GuardTests/Messaging/Recoverability/RecoverabilityGuardTests.cs` | modify (same for `DelayedRetryProcessor`) |
| 9 | `changelog.d/2396-processor-heartbeat-messaging.added.md` | new (`mechanical-fixer`) |

Tasks (PR 3):

1. **Modify** `OutboxProcessorBase.cs`: resolve the registry with `GetService` from the held `IServiceProvider` and register in the constructor (`outbox:<concrete type name>`, `_options.ProcessingInterval`, Polling); wrap the loop body (`RecordCycleStarted` before `ProcessPendingMessagesAsync`, `RecordCycleCompleted` after, `RecordCycleFailed(ex.GetType().Name)` in the existing catch, `MarkDisabled` in the disabled early return). The eight provider subclasses are not modified and their tests compile unchanged.
2. **Modify** `ScheduledMessageProcessor.cs` and `DelayedRetryProcessor.cs` likewise, with a trailing optional `IProcessorHeartbeatRegistry? heartbeats = null` parameter (existing construction sites in the unit and guard tests compile unchanged; only the two guard files above gain a case); the intervals are `_options.ProcessingInterval` and `ProcessingInterval`.
3. **Tests and manifest in this PR**: per-file targets in `Encina.Messaging.json` for the three processors (unit raised for the new lines, with one-sentence justifications); the integration targets for `OutboxProcessorBase.cs` are entered in PR 4 with the integration tests.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Phase 1 (core abstractions) and Phase 2 (EventId range and log class) are merged. OutboxProcessorBase has a sealed
ExecuteAsync loop and an IServiceProvider; ScheduledMessageProcessor and DelayedRetryProcessor catch every exception
inside their loops and delay by their processing interval.

TASK:
PR 2: create ProcessorLivenessHealthCheck and the AddEncinaProcessorLiveness builder extension (options only through
IOptions<ProcessorLivenessOptions>). PR 3 (after PR 2 merges): add the heartbeat calls to the three Messaging processors
(register in the constructor) and keep the eight OutboxProcessor subclasses untouched.

KEY RULES:
- A cycle completes in a finally path so a failure never leaves a cycle open; cancellation is not a failure.
- Disabled processors call MarkDisabled and return exactly as before.
- Results and logs carry the failure type name only; status transitions only are logged.
- The check never throws; timers and loops keep their existing exception handling.
- Tags: encina and processor, never ready by default.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxProcessorBase.cs
- src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs
- src/Encina.Messaging/Recoverability/DelayedRetryProcessor.cs
- src/Encina.Messaging/Health/OutboxHealthCheck.cs
- src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs (AddEncinaOutbox, CombineTagsWithoutReady)
```

</details>

### Phase 4: Integration tests (pull request 4) and CDC processors (pull request 5)

<details>
<summary><strong>Tasks</strong></summary>

PR 4 holds the provider integration tests of Design Choice 12 (Option A: 4 test files plus 1 manifest edit = 5 files; Option B: 10 files plus the manifest = 11, both within 15):

| # | File | Change |
|---|------|--------|
| 1-4 | `tests/Encina.IntegrationTests/Messaging/ProcessorLiveness/{ADO,Dapper,EFCore,MongoDB}OutboxProcessorLivenessIntegrationTests.cs` | new (a real `OutboxProcessor` over a real store through the shared collection fixture of the family; stop the store or block the loop and assert the check turns Unhealthy, then recovers) |
| 5 | `.github/coverage-manifest/Encina.Messaging.json` | modify (integration target of `OutboxProcessorBase.cs`, with its justification; `mechanical-fixer`) |

PR 5 holds exactly these 5 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Cdc/Processing/CdcProcessor.cs` | modify |
| 2 | `src/Encina.Cdc/Sharding/ShardedCdcProcessor.cs` | modify |
| 3 | `tests/Encina.UnitTests/Cdc/CdcProcessorHeartbeatTests.cs` | new (fake connector: success, failing, hanging, long-running stream; both processors) |
| 4 | `.github/coverage-manifest/Encina.Cdc.json` | modify (`mechanical-fixer`) |
| 5 | `changelog.d/2396-processor-heartbeat-cdc.added.md` | new (`mechanical-fixer`) |

Tasks (PR 5):

1. **Modify** `CdcProcessor.cs`: register in the constructor from its `IServiceProvider` (`GetService`; no constructor change, so `CdcHappyPathAndConstructorGuardTests` and the other callers compile unchanged) as `cdc:<connector id>` with `ProcessorHeartbeatKind.Streaming` and `_options.PollingInterval`; `RecordCycleStarted` before `ProcessChangesAsync`, `RecordCycleCompleted` after; the retry path records `RecordCycleFailed(ex.GetType().Name, retryAfter: delay)` with the `CalculateRetryDelay` value; the dead-letter branch counts as a failed cycle (Design Choice 11).
2. **Modify** `ShardedCdcProcessor.cs` likewise (`cdc-sharded:<connector id>`; one heartbeat per processor instance).
3. **Tests and manifest in this PR**: a long-running stream stays Healthy for several multiples of the interval; a retry delay longer than the window does not turn Unhealthy; a loop that exits turns Unhealthy; per-file targets with one-sentence justifications in `Encina.Cdc.json` for both processors.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Pull requests 1 to 3 are merged. CdcProcessor loops ProcessChangesAsync with retry and dead-lettering; it holds an
IServiceProvider and has no TimeProvider.

TASK:
Add the heartbeat calls to CdcProcessor and ShardedCdcProcessor and test them with a fake connector.

KEY RULES:
- The retry loop is one failed cycle per exception (with retryAfter set to the retry delay); a successful pass resets the failure count by recording completion; a streaming pass in flight is never stale.
- Record the exception type name only; keep the existing ForLogging() log calls unchanged.
- No behavior change when no registry is registered.

REFERENCE FILES:
- src/Encina.Cdc/Processing/CdcProcessor.cs
- src/Encina.Cdc/Sharding/ShardedCdcProcessor.cs
- src/Encina.Cdc/Health/CdcHealthCheck.cs
```

</details>

### Phase 5: Compliance monitors (pull requests 6 and 7)

<details>
<summary><strong>Tasks</strong></summary>

PR 6 (BreachNotification and Retention) holds exactly 11 files; PR 7 (DPIA and CrossBorderTransfer) holds the same shape, 11 files:

| # | File (PR 6) | Change |
|---|-------------|--------|
| 1 | `src/Encina.Compliance.BreachNotification/BreachDeadlineMonitorService.cs` | modify |
| 2 | `src/Encina.Compliance.Retention/RetentionEnforcementService.cs` | modify |
| 3-4 | `src/Encina.Compliance.{BreachNotification,Retention}/PublicAPI.Unshipped.txt` | modify (the changed constructors; `mechanical-fixer`) |
| 5-6 | `.github/coverage-manifest/Encina.Compliance.{BreachNotification,Retention}.json` | modify (`mechanical-fixer`) |
| 7 | `tests/Encina.UnitTests/Compliance/BreachNotification/BreachDeadlineMonitorServiceTests.cs` | modify (heartbeat cases on a `FakeTimeProvider`) |
| 8 | `tests/Encina.UnitTests/Compliance/Retention/RetentionEnforcementServiceTests.cs` | modify |
| 9 | `tests/Encina.GuardTests/Compliance/BreachNotification/BreachDeadlineMonitorServiceGuardTests.cs` | modify (the nullable `heartbeats` accepts null: a valid-result test) |
| 10 | `tests/Encina.GuardTests/Compliance/Retention/RetentionEnforcementServiceGuardTests.cs` | modify |
| 11 | `changelog.d/2396-processor-heartbeat-compliance-1.added.md` | new (`mechanical-fixer`) |

PR 7 mirrors it: `DPIAReviewReminderService.cs`, `TransferExpirationMonitor.cs` (`src/Encina.Compliance.CrossBorderTransfer/Notifications/`), the two `PublicAPI.Unshipped.txt`, the two manifests, `DPIAReviewReminderServiceTests.cs`, `TransferExpirationMonitorTests.cs`, `DPIAReviewReminderServiceGuardTests.cs`, `TransferExpirationMonitorGuardTests.cs`, and `changelog.d/2396-processor-heartbeat-compliance-2.added.md`.

Tasks:

1. **Modify** `BreachDeadlineMonitorService.cs` (`DeadlineCheckInterval`), `RetentionEnforcementService.cs` (`EnforcementInterval`), `DPIAReviewReminderService.cs` and `TransferExpirationMonitor.cs` (`ExpirationCheckInterval`): a trailing optional `IProcessorHeartbeatRegistry? heartbeats = null` constructor parameter (existing construction sites compile unchanged: the other test files that construct these services, such as the `Retention*LeakTests` and `DPIA*LeakTests`, need no edit); register in the constructor; heartbeat around each cycle method; the first cycle runs immediately in these services, so the grace period is short.
2. These services use `PeriodicTimer`, which ends the loop on an unhandled exception; the heartbeat records inside the existing per-cycle try/catch and the loop is not otherwise changed.
3. **Tests and manifests in each PR**: unit tests per service with a `FakeTimeProvider`; per-file targets with one-sentence justifications for each touched service file in its package manifest.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
The four compliance monitors reference Encina but not Encina.Messaging, so they use the core abstraction from Phase 1.

TASK:
Add the optional registry parameter and the heartbeat calls to the four services and test each.

KEY RULES:
- Do not change the cycle's error handling; record CycleFailed with the exception type name where the cycle already catches.
- A PeriodicTimer loop catches per iteration; do not let a recording call throw (the registry never throws).
- Pre-1.0: change constructors cleanly, update every construction site and test.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/BreachDeadlineMonitorService.cs
- src/Encina.Compliance.Retention/RetentionEnforcementService.cs
- src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs
- src/Encina.Compliance.CrossBorderTransfer/Notifications/TransferExpirationMonitor.cs
```

</details>

### Phase 6: Testing, documentation and finalization (pull request 8 holds the documentation)

<details>
<summary><strong>Tasks</strong></summary>

Each pull request above carries its own tests, manifest targets, PublicAPI lines and changelog fragment (the file tables of Phases 1 to 5); `mechanical-fixer` enters, in each PR, the per-flag targets with one-sentence justifications of that PR's files, and the measured values replace the estimates before the PR. This phase states what the tests contain.

1. Unit tests: registry (distinct handles for duplicate names, snapshots, gauge value on a `FakeTimeProvider`), check thresholds (stale, hung cycle, streaming in flight, retry delay, consecutive failures, disabled, grace period, recovery, empty registry), options validation, builder extension tags (no `ready`), the DI tests with `ValidateOnBuild` and `ValidateScopes` for `AddEncinaProcessorHeartbeat` (PR 1) and `AddEncinaProcessorLiveness` (PR 2).
2. Guard tests for every public method and constructor, in `tests/Encina.GuardTests`; contract tests for `IProcessorHeartbeat` and `IProcessorHeartbeatRegistry` (PR 1). No contract-test `.md` justification is written.
3. Integration (PR 4): per Design Choice 12; the heartbeat is in the shared base, so Option A proves inheritance, not provider SQL.
4. Property, load and benchmark: justified in the manifest `reason` of the files concerned (see PR 1).
5. PR 8 holds exactly 2 files: `docs/guides/health-checks.md` (the processor liveness section, including the limits: a streaming processor hang is not detected by time alone, an unregistered hosted service is invisible) and `src/Encina.AspNetCore/README.md`, both by `docs-writer`. No ADR: the decisions are recorded here and the contract is small.
6. Follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/processor-heartbeat-remaining-services.md): the remaining background services.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
The heartbeat, check, registration and processor changes exist. Tests, manifests and documentation remain; this phase is
split across the eight pull requests, each with the tests, manifest targets and PublicAPI lines of its own files.

TASK:
Write the tests above, measure per-flag coverage of every touched file with the coverage-report script, record targets
with one-sentence justifications, and complete the documentation through the specialists.

KEY RULES:
- Tests execute the real registry, check and processors; FakeTimeProvider for time; no Thread.Sleep.
- Integration tests use the shared [Collection] fixture of the provider family, never a per-class container.
- A test never swallows an exception thrown by the code it drives.
- CRAP of every added method 10 or less; keep the check's per-processor evaluation in small methods.

REFERENCE FILES:
- tests/Encina.UnitTests/Messaging/
- tests/Encina.UnitTests/Cdc/
- .github/coverage-manifest/Encina.Messaging.json
- docs/testing/integration-tests.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 3 | `TimeProvider` for time; no error or exception message in health results; background loops and timers never let an exception escape; opt-in features |
| AGENTS.md section 6, function 4 | A background service must not fail silently; a checkable dependency exposes a health check |
| AGENTS.md section 7 (ADR-021) | EventId range registered first, packed, mapped in the architecture test |
| ADR-050 | Module requirements: the heartbeat adds no module dependency and stays optional |
| Kubernetes probes | The `live` tag lets a host restart a pod whose processor died (#454) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `OutboxProcessorBase` | `src/Encina.Messaging/Outbox/OutboxProcessorBase.cs` | One change covers the eight provider `OutboxProcessor` subclasses |
| `ScheduledMessageProcessor`, `DelayedRetryProcessor` | `src/Encina.Messaging/Scheduling/`, `Recoverability/` | Heartbeat around each cycle |
| `CdcProcessor`, `ShardedCdcProcessor` | `src/Encina.Cdc/Processing/`, `Sharding/` | Heartbeat around each pass |
| Compliance monitors | `src/Encina.Compliance.{BreachNotification,Retention,DPIA,CrossBorderTransfer}/` | `PeriodicTimer` services |
| `EncinaHealthCheck`, `OutboxHealthCheck` | `src/Encina.Messaging/Health/` | Base type and status classification style |
| `HealthCheckBuilderExtensions` | `src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs` | `AddEncinaOutbox` and `CombineTagsWithoutReady` pattern |
| `OutboxProcessorMetrics` | `src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs` | Observable gauge on the shared `Encina` meter |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/`, `tests/Encina.UnitTests/Testing/Architecture/` | Range registration and assembly map |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (new range `MessagingProcessorLiveness`) | 7350-7369 | From the family block 7350-7399; 7350-7353 used by this plan, 7354-7369 free for the follow-up; mapped to `Encina.Messaging` in `AssemblyRanges` |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 9 | 11 | New: seven core files (two interfaces, snapshot, kind enum, registry, options, registration extension), the liveness check and its log class; modified: `EventIdRanges.cs`, `HealthCheckBuilderExtensions.cs`, three Messaging processors, two CDC processors, four compliance services |
| Tests | 11 | 11 | New: registry unit, guard, contract (PR 1), check unit and guard (PR 2), processor unit (PR 3), four integration (PR 4, Option A), CDC unit (PR 5); modified: the event-id allocation test, two Messaging guard files, four compliance unit files and four compliance guard files |
| Documentation | 0 | 2 | Guide, `Encina.AspNetCore` README |
| Configuration | 6 | 20 | Six changelog fragments (new); nine `PublicAPI.Unshipped.txt` edits and eleven manifest edits across the pull requests (`Encina.json` twice, `Encina.Messaging.json` three times, `Encina.AspNetCore.json` once, `Encina.Cdc.json` once, four compliance manifests once each) |
| **Total** | **26** | **44** | 70 file changes in 8 pull requests: PR 1 = 13, PR 2 = 14, PR 3 = 9, PR 4 = 5 (Option A; 11 under Option B), PR 5 = 5, PR 6 = 11, PR 7 = 11, PR 8 = 2 (13 + 14 + 9 + 5 + 5 + 11 + 11 + 2 = 70); each at most 15 files |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #2396 adds liveness health checks for
background processors: a processor that stopped, hangs or fails every cycle must be Unhealthy or Degraded, not an empty
backlog. Sibling plans: #454 (probe endpoints and the live tag), #622 (per-check health gauge), #754 (shared options).

IMPLEMENTATION OVERVIEW:
1. Core Encina: IProcessorHeartbeat, IProcessorHeartbeatRegistry, ProcessorHeartbeatSnapshot, an internal registry with an
   observable gauge encina.processor.heartbeat_age_seconds, ProcessorLivenessOptions, AddEncinaProcessorHeartbeat.
2. EventId range MessagingProcessorLiveness 7350-7369 registered in EventIdRanges and the architecture test map;
   ProcessorLivenessLog (7350-7353) logs status transitions only.
3. ProcessorLivenessHealthCheck in Encina.Messaging/Health and AddEncinaProcessorLiveness in Encina.AspNetCore
   (tags encina and processor, no ready tag).
4. Heartbeat calls in OutboxProcessorBase (once for the eight provider subclasses), ScheduledMessageProcessor,
   DelayedRetryProcessor, CdcProcessor, ShardedCdcProcessor and the four compliance monitors.
5. Unit, guard, contract and integration tests; manifests, guide, README and changelog through the specialists.
Eight pull requests: core, check and registration, Messaging processors, provider integration tests, CDC, two compliance
pairs, documentation.

KEY PATTERNS:
- Time through TimeProvider; failure type name only; the registry and the recording calls never throw.
- Optional dependency: no registry registered means no heartbeat and no behavior change.
- Processors register in their constructor; duplicate names get a suffix; an empty registry is Healthy (pending the maintainer).
- Disabled processors report Healthy with state disabled; a polling cycle in flight longer than the window is stale; a streaming cycle in flight is not.
- Options flow only through IOptions<ProcessorLivenessOptions>.
- A background loop or PeriodicTimer keeps its existing exception handling.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxProcessorBase.cs
- src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs
- src/Encina.Messaging/Health/OutboxHealthCheck.cs
- src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs
- src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.Messaging.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | A liveness signal must reflect real-time state |
| 2 | OpenTelemetry | ✅ Include | Phase 1: observable gauge `encina.processor.heartbeat_age_seconds` tagged by processor |
| 3 | Structured Logging | ✅ Include | Phase 2: EventIds 7350-7353 in the new range 7350-7369, status transitions only |
| 4 | Health Checks | ✅ Include | This is the feature |
| 5 | Validation | ✅ Include | `ProcessorLivenessOptions` validated at startup (multiplier above 1, threshold at least 1) |
| 6 | Resilience | ❌ N/A | The check reads in-process state and calls nothing external |
| 7 | Distributed Locks | ⏭️ Defer | Reporting which instance holds the leader lock waits for the row-claiming and leader-election work in #1251 |
| 8 | Transactions | ❌ N/A | No data is written |
| 9 | Idempotency | ❌ N/A | Recording a cycle is repeatable and holds no business state |
| 10 | Multi-Tenancy | ❌ N/A | Processors are process-scoped, not tenant-scoped |
| 11 | Module Isolation | ❌ N/A | Heartbeats are process-scoped; the module health checks stay as they are |
| 12 | Audit Trail | ❌ N/A | Infrastructure monitoring; compliance monitors record liveness, not compliance events |

**Deferred items:**

- Leader-lock reporting: #1251 (open).
- The remaining background services (dead-letter cleanup, message purge, audit retention, break-the-glass, sharding): follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/processor-heartbeat-remaining-services.md).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `EncinaHealthCheck`, `AddEncinaOutbox` pattern | ✅ Available | |
| Family EventId block 7350-7399 | ✅ Assigned | This plan takes 7350-7369 |
| `EncinaEventIdAllocationTests` map | ✅ Available | Updated in PR 2 |
| #454 (`EncinaHealthTags.Live`) | Parallel | Not required: the host may pass the tag string; the constant is adopted when #454 lands |
| #2254 (Recoverability health and OpenTelemetry) | Open | Reused: the delayed-retry liveness is here; comment on #2254 after merge |
| #2138 (SchedulingHealthCheck false readiness) | Open bug | Independent; the new check is readiness-neutral by design |
| Maintainer answers to the Dilemmas (Design Choices 1, 2, 4, 7, 9, 10, 11 and 12) | Pending | Asked when work starts |

No blocking prerequisites.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1, 2, 4, 7, 9, 10, 11 and 12) when work starts, records the answers under `## Maintainer Decisions`, then briefs the eight pull requests in order (core, check and registration, Messaging processors, integration tests, CDC, two compliance pairs, documentation).
2. After the last pull request, comment on #2396, on #2254 (liveness covered, backlog check and metrics remain) and on #454 (the processor check can carry the `live` tag); the follow-up issue for the remaining services is opened from the draft artifacts/decisions/plan-batch/followups/processor-heartbeat-remaining-services.md.
