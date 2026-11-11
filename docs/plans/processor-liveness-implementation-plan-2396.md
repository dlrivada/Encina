# Implementation Plan: Processor Liveness Health Checks

> **Issue**: [#2396](https://github.com/dlrivada/Encina/issues/2396)
> **Type**: Feature
> **Complexity**: High (6 phases, about 45 files, split into 4 pull requests)
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

**Provider category**: none. The heartbeat sits in the shared processor bases, so the 10 database providers inherit it. The eight `OutboxProcessor` subclasses are not modified; one integration test per family proves a real processor over a real store reports liveness (Phase 6).

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

- Hosted services start before the first probe, but the guarantee is an ordering detail of the host, not a contract; A has no ordering dependency.
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
<summary><strong>4. Status rules, cycle meaning and disabled processors</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Unhealthy when no cycle completed within `StaleCycleMultiplier` x the processor interval (default 3); Degraded after `ConsecutiveFailureThreshold` failed cycles (default 3); a disabled processor reports Healthy with `state=disabled`** | Matches the issue text; a cycle that started but never ended counts as stale (a hung await); the first cycle gets a grace period from registration | Two options to tune |
| **B) Unhealthy on the first failed cycle** | Simplest | One transient store error flips readiness-neutral health and alerts |

### Chosen Option: **A - stale multiple, consecutive failures, disabled is Healthy**

### Rationale

- A cycle is one iteration of the loop: it completes when the loop body returns or its caught exception is recorded; the failure is recorded as the exception type name, never the message.
- Class: Trivial
- Settled by: #2396 (Unhealthy when no cycle completed within a configurable multiple of the interval; Degraded when cycles complete with failures), AGENTS.md section 3 (no exception message in health results) and the classification in `src/Encina.Messaging/Health/OutboxHealthCheck.cs`.

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
- Class: Trivial
- Settled by: AGENTS.md section 1 (every feature is opt-in, pay-for-what-you-use) and AGENTS.md section 3 (an optional `TimeProvider` parameter is the existing constructor pattern of these processors).

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

- The issue states the check is readiness-neutral like the outbox check (`HealthCheckBuilderExtensions` keeps `DefaultTagsWithoutReady`).
- Class: Trivial
- Settled by: #2396 and #2138.

</details>

<details>
<summary><strong>7. Which processors are in scope</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The issue's list plus the sibling cross-border expiry monitor; the other background services in a follow-up issue** | Bounded pull requests; every named processor covered | Dead-letter cleanup, message purge, audit retention, break-the-glass and sharding services stay without a heartbeat until the follow-up |
| **B) Every `BackgroundService` in `src/`** (about 27 files) | Complete | Oversized; crosses many packages in one family of pull requests |

### Chosen Option: **A - the issue's list, the follow-up issue for the rest**

### Rationale

- The registry is generic, so the follow-up is mechanical: each service adds three lines.
- Class: Settled
- Settled by: #2396 (the listed processors) and AGENTS.md section 11 (a worker's issue is one pull request of at most 15 files and 1500 lines).

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
- Class: Trivial
- Settled by: `src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs` and #2396 (the metric named in the issue).

</details>

---

## Implementation Phases

### Phase 1: Core abstractions and registry (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Diagnostics/IProcessorHeartbeat.cs`: `RecordCycleStarted()`, `RecordCycleCompleted()`, `RecordCycleFailed(string failureType)`, `MarkDisabled()`.
2. **Create** `src/Encina/Diagnostics/IProcessorHeartbeatRegistry.cs`: `IProcessorHeartbeat Register(string processorName, TimeSpan expectedInterval)` (idempotent per name: a second call returns the same handle) and `IReadOnlyList<ProcessorHeartbeatSnapshot> GetSnapshots()`.
3. **Create** `src/Encina/Diagnostics/ProcessorHeartbeatSnapshot.cs`: a record with `ProcessorName`, `ExpectedInterval`, `RegisteredAtUtc`, `LastCycleStartedAtUtc`, `LastCycleCompletedAtUtc`, `ConsecutiveFailures`, `LastFailureType`, `IsDisabled` (UTC timestamps with the `AtUtc` suffix).
4. **Create** `src/Encina/Diagnostics/ProcessorHeartbeatRegistry.cs`: `internal sealed`, takes `TimeProvider`, thread-safe (one lock per handle or `Interlocked` snapshots), creates the gauge on the shared `Encina` meter (`encina.processor.heartbeat_age_seconds`, unit `s`, tag `processor`).
5. **Create** `ProcessorLivenessOptions` (`StaleCycleMultiplier` 3.0, `ConsecutiveFailureThreshold` 3, validated greater than 1 and at least 1) and an `AddEncinaProcessorHeartbeat(this IServiceCollection)` that `TryAdd`s the registry and the options with `ValidateOnStart`.
6. `PublicAPI.Unshipped.txt` lines and `.github/coverage-manifest/Encina.json` targets (`mechanical-fixer`).

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

### Phase 2: Observability and structured logging (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: register `public static readonly (int Min, int Max) MessagingProcessorLiveness = (7350, 7369);` from the family block 7350-7399.
2. **Modify** `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`: add `nameof(EventIdRanges.MessagingProcessorLiveness)` to the `Encina.Messaging` entry of `AssemblyRanges`.
3. **Create** `src/Encina.Messaging/Health/ProcessorLivenessLog.cs`: `[LoggerMessage]` methods `ProcessorStale` (7350, Warning: name, seconds since last cycle), `ProcessorFailing` (7351, Warning: name, consecutive failures, failure type), `ProcessorRecovered` (7352, Information: name), `ProcessorDisabled` (7353, Information: name, logged once). `PublicAPI.Unshipped.txt` gets the new range field.
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

### Phase 3: Liveness check, Messaging processors and registration (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Messaging/Health/ProcessorLivenessHealthCheck.cs`: `EncinaHealthCheck` (`DefaultName = "encina-processor-liveness"`, tags `encina`, `processor`), constructor `(IProcessorHeartbeatRegistry registry, IOptions<ProcessorLivenessOptions> options, TimeProvider? timeProvider = null, ILogger<...>? logger = null)`; per processor: disabled gives Healthy, no completed cycle within `StaleCycleMultiplier x ExpectedInterval` since the later of registration and last completion (or a started cycle that never ended) gives Unhealthy, `ConsecutiveFailures >= threshold` gives Degraded; the worst status wins; `Data` holds one entry per processor (`state`, `seconds_since_last_cycle`, `consecutive_failures`, `last_failure_type`); no message text.
2. **Modify** `OutboxProcessorBase.cs`: resolve the registry from the held `IServiceProvider`, register with `_options.ProcessingInterval`, wrap the loop body (`CycleStarted` before `ProcessPendingMessagesAsync`, `CycleCompleted` after, `CycleFailed(ex.GetType().Name)` in the existing catch, `MarkDisabled` in the disabled early return). The eight provider subclasses are not modified.
3. **Modify** `ScheduledMessageProcessor.cs` and `DelayedRetryProcessor.cs` likewise (optional `IProcessorHeartbeatRegistry? heartbeats = null` parameter; the intervals are `_options.ProcessingInterval` and `ProcessingInterval`).
4. **Modify** `src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs`: `AddEncinaProcessorLiveness(this IHealthChecksBuilder, string? name = null, ProcessorLivenessOptions? options = null, IEnumerable<string>? tags = null, HealthStatus? failureStatus = null)` that registers the heartbeat registry (Phase 1) and the check without the `ready` tag (via `CombineTagsWithoutReady`).
5. `PublicAPI.Unshipped.txt` lines for `Encina.Messaging` and `Encina.AspNetCore`; manifests (`mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Phase 1 (core abstractions) and Phase 2 (EventId range and log class) are merged. OutboxProcessorBase has a sealed
ExecuteAsync loop and an IServiceProvider; ScheduledMessageProcessor and DelayedRetryProcessor catch every exception
inside their loops and delay by their processing interval.

TASK:
Create ProcessorLivenessHealthCheck, add the heartbeat calls to the three Messaging processors, add the
AddEncinaProcessorLiveness builder extension, and keep the eight OutboxProcessor subclasses untouched.

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

### Phase 4: CDC processors (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.Cdc/Processing/CdcProcessor.cs`: register with `_options.PollingInterval` using its `IServiceProvider`; record around `ProcessChangesAsync`; the retry path records `CycleFailed` with the exception type name; the dead-letter branch counts as a failed cycle.
2. **Modify** `src/Encina.Cdc/Sharding/ShardedCdcProcessor.cs` likewise (one heartbeat per processor instance named after the connector).
3. Tests with a fake connector (success, failing, hanging) and the manifest targets for `Encina.Cdc.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Pull requests 1 and 2 are merged. CdcProcessor loops ProcessChangesAsync with retry and dead-lettering; it holds an
IServiceProvider and has no TimeProvider.

TASK:
Add the heartbeat calls to CdcProcessor and ShardedCdcProcessor and test them with a fake connector.

KEY RULES:
- The retry loop is one failed cycle per exception; a successful pass resets the failure count by recording completion.
- Record the exception type name only; keep the existing ForLogging() log calls unchanged.
- No behavior change when no registry is registered.

REFERENCE FILES:
- src/Encina.Cdc/Processing/CdcProcessor.cs
- src/Encina.Cdc/Sharding/ShardedCdcProcessor.cs
- src/Encina.Cdc/Health/CdcHealthCheck.cs
```

</details>

### Phase 5: Compliance monitors (pull request 4)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `BreachDeadlineMonitorService.cs` (`DeadlineCheckInterval`), `RetentionEnforcementService.cs` (`EnforcementInterval`), `DPIAReviewReminderService.cs` and `TransferExpirationMonitor.cs` (`ExpirationCheckInterval`): optional `IProcessorHeartbeatRegistry? heartbeats = null` constructor parameter; heartbeat around each cycle method; the first cycle runs immediately in these services, so the grace period is short.
2. These services use `PeriodicTimer`, which ends the loop on an unhandled exception; the heartbeat records inside the existing per-cycle try/catch and the loop is not otherwise changed.
3. Unit tests per service with a `FakeTimeProvider` and the manifests of the four packages.

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

### Phase 6: Testing, documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. Unit tests (`tests/Encina.UnitTests/`): registry (register idempotent, snapshots, gauge value on a `FakeTimeProvider`), check thresholds (stale, hung cycle, consecutive failures, disabled, grace period, recovery), options validation, builder extension tags (no `ready`).
2. Guard tests for every public method and constructor; contract tests for `IProcessorHeartbeat` and `IProcessorHeartbeatRegistry` (a test double and the real registry honor the same behavior).
3. Integration (`tests/Encina.IntegrationTests`): a real `OutboxProcessor` over a real store through the shared collection fixture; stop the store or block the loop and assert the check turns Unhealthy, then recovers. One run per provider family (ADO.NET, Dapper, EF Core, MongoDB) uses the existing family fixture; the heartbeat is in the shared base, so the matrix proves inheritance, not provider SQL.
4. Property, load and benchmark: property is justified (simple threshold arithmetic), load is justified (the registry takes one lock per cycle per processor), benchmark is justified (not a hot path); each manifest `reason` says so.
5. `mechanical-fixer`: manifests (`Encina.json`, `Encina.Messaging.json`, `Encina.AspNetCore.json`, `Encina.Cdc.json`, four compliance manifests) with per-file targets and one-sentence justifications; PublicAPI lines; changelog fragment `changelog.d/2396-processor-liveness-health-checks.added.md`.
6. `docs-writer`: the processor liveness section of `docs/guides/health-checks.md` and the `Encina.AspNetCore` README. No ADR: the decisions are recorded here and the contract is small.
7. Open the follow-up issue from the issue file written with this plan (the remaining background services).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
The heartbeat, check, registration and processor changes exist. Tests, manifests and documentation remain; this phase is
split across the four pull requests, each with the tests of its own files.

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
| Production code | 7 | 12 | Core: 5 new (interfaces, snapshot, registry, options); Messaging: check and log class; modified: `EventIdRanges.cs`, three Messaging processors, `HealthCheckBuilderExtensions.cs`, two CDC processors, four compliance services |
| Tests | 10 | 6 | Registry, check, options, extension, per-processor tests, integration; modified: the event-id map and existing processor tests whose constructors change |
| Documentation | 0 | 2 | Guide, `Encina.AspNetCore` README |
| Configuration | 1 | 14 | Changelog fragment; `PublicAPI.Unshipped.txt` (5 packages) and manifests (9 files) |
| **Total** | **18** | **34** | 52 files in 4 pull requests: PR 1 core (about 10), PR 2 Messaging, range, check, registration (about 15), PR 3 CDC (about 7), PR 4 compliance (about 14); each at or under 15 files |

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
Four pull requests: core, Messaging, CDC, compliance.

KEY PATTERNS:
- Time through TimeProvider; failure type name only; the registry and the recording calls never throw.
- Optional dependency: no registry registered means no heartbeat and no behavior change.
- Disabled processors report Healthy with state disabled; a started cycle that never ends is stale.
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
- The remaining background services (dead-letter cleanup, message purge, audit retention, break-the-glass, sharding): the issue file `artifacts/issues/processor-heartbeat-remaining-services.md` written with this plan.

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `EncinaHealthCheck`, `AddEncinaOutbox` pattern | ✅ Available | |
| Family EventId block 7350-7399 | ✅ Assigned | This plan takes 7350-7369 |
| `EncinaEventIdAllocationTests` map | ✅ Available | Updated in Phase 2 |
| #454 (`EncinaHealthTags.Live`) | Parallel | Not required: the host may pass the tag string; the constant is adopted when #454 lands |
| #2254 (Recoverability health and OpenTelemetry) | Open | Reused: the delayed-retry liveness is here; comment on #2254 after merge |
| #2138 (SchedulingHealthCheck false readiness) | Open bug | Independent; the new check is readiness-neutral by design |
| Maintainer answers to Design Choices 1 and 2 | Pending | Asked when work starts |

No blocking prerequisites.

---

## Next Steps

1. The orchestrator asks the maintainer Design Choices 1 and 2 when work starts, records the answers under `## Maintainer Decisions`, then briefs four workers in order (core, Messaging, CDC, compliance).
2. After the last pull request, comment on #2396, on #2254 (liveness covered, backlog check and metrics remain) and on #454 (the processor check can carry the `live` tag); open the follow-up issue for the remaining services.
