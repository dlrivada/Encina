# Implementation Plan: Leader Election for Background Message Processors

> **Issue**: [#717](https://github.com/dlrivada/Encina/issues/717)
> **Type**: Feature
> **Complexity**: Very high (6 pull requests, 2 packages plus the 3 processor adoptions)
> **Estimated Scope**: ~700-900 lines of production code + ~1,500-1,900 lines of tests
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027 (lock providers); [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md); [ADR-050](../architecture/adr/050-modules-may-require-modules.md)
> **Depends on**: [#2667](https://github.com/dlrivada/Encina/issues/2667) (implements ADR-052; open, not a pending maintainer decision)
> **Related**: [#216](https://github.com/dlrivada/Encina/issues/216) (`ILeaderElectionProvider`, overlaps), [#221](https://github.com/dlrivada/Encina/issues/221) (`[RequiresLeadership]` behavior), [#713](https://github.com/dlrivada/Encina/issues/713)-[#716](https://github.com/dlrivada/Encina/issues/716) (per-operation locks), [#1251](https://github.com/dlrivada/Encina/issues/1251) (row claiming), [#218](https://github.com/dlrivada/Encina/issues/218) (fencing), [#223](https://github.com/dlrivada/Encina/issues/223) (auto-extend), [#2405](https://github.com/dlrivada/Encina/issues/2405), [#763](https://github.com/dlrivada/Encina/issues/763) and [#741](https://github.com/dlrivada/Encina/issues/741) (same family; #763 plan (#763), #741 plan (#741))

---

## Summary

Every replica of an Encina application runs every polling background processor: eight packages ship an `OutboxProcessor` over `OutboxProcessorBase`, and `ScheduledMessageProcessor`, `DelayedRetryProcessor`, `DeadLetterCleanupProcessor` and `MessagePurgeService` poll too. With N replicas the database is polled N times and, without row claiming (#1251), the same message can be published N times. This plan adds an opt-in **leader election built on `IDistributedLockProvider`**, so that the processors that adopt it run only on the replica that holds the lease:

1. **Abstraction.** A small `ILeaderElection` in `Encina.Messaging` that processors consult: `bool IsLeader(string electionName)`, plus a read-only state snapshot for health checks. It performs no I/O.
2. **Default implementation.** In `Encina.DistributedLock` (the dependency direction forbids it in `Encina.Messaging`): a `LeaderElectionService : BackgroundService` runs a heartbeat loop on a `PeriodicTimer` over the injected `TimeProvider`. For each configured election it tries `TryAcquireAsync` (lease = lock expiry), and while leading it renews with `ILockHandle.ExtendAsync` every heartbeat (default 15 s; lease default 45 s, both pending Dilemma 7).
3. **Safe leadership.** `IsLeader` is true only while `now < leaseExpiresAtUtc` as the local clock sees it, where `leaseExpiresAtUtc` is the time read **before** the acquire or extend call plus the lease (the lock backend starts its own clock after that instant), so a stalled renewal loop, a slow call or a lost lock cannot leave a replica believing it still leads. A `Left`, a failed extension or an exception ends leadership visibly (logged by error code or exception type, counted), never silently.
4. **Failover.** When the leader stops (graceful shutdown disposes the handle) or dies (the lease expires), another replica acquires the lock at its next heartbeat.
5. **Adoption.** `OutboxProcessorBase` (covers the eight outbox packages without touching them), `ScheduledMessageProcessor` and `DelayedRetryProcessor` skip a cycle when `ILeaderElection` says they are not the leader, and **fail closed**: with the flag on and no election resolvable they do not run (design choice 3 sets the scope).
6. **Opt-in and enforcement.** `config.UseLeaderElection = true` in `MessagingConfiguration`; startup validation fails when the flag is on and no `ILeaderElection` or lock provider is registered (ADR-050 rule 3); the new requirement is an amendment row of ADR-050.

**Standards**: ADR-052; ADR-050; ADR-001 (errors as values); lease-based election as in Hangfire (single active server for recurring work) and Kubernetes `coordination.k8s.io` Leases (named comparable products, not dependencies).

**Estimated scope**: about 11 new production files, 8 modified, about 14 test files, 6 documentation files; 6 pull requests (see Implementation Phases).

**Affected packages**: `Encina.Messaging` (abstraction, options, flag, validation, three processors); `Encina.DistributedLock` (implementation, service, health check, diagnostics). The eight `*.OutboxProcessor` classes in the provider packages are not edited.

**Provider category**: Distributed locks as the engine (the election runs on any `IDistributedLockProvider`; every lock provider of SPEC-000 DEC-009 gets a contract test through it), and the Database (10) family only as a consumer of `OutboxProcessorBase` (one integration test per family proves the adoption). Messaging transports are not touched.

**What the code does today** (`origin/main` at `c49b987e9`):

- Nothing of this feature exists: no `ILeaderElection`, `UseLeaderElection` or leader service under `src/`. `MessagingConfiguration` has opt-in flags in the `UseOutbox` style (`src/Encina.Messaging/MessagingConfiguration.cs`, for example `UseOutbox` line 87, `UseScheduling` line 131, `UseTenancy` line 242).
- `OutboxProcessorBase : BackgroundService` (`Outbox/OutboxProcessorBase.cs`) has a sealed `ExecuteAsync` loop: `ProcessPendingMessagesAsync` then `Task.Delay(_options.ProcessingInterval, stoppingToken)`; it takes `(IServiceProvider, ILogger, OutboxOptions, TimeProvider?)` and each of the eight provider processors derives from it. `ScheduledMessageProcessor` has the same loop shape (`ProcessOnceAsync`, `Task.Delay(_options.ProcessingInterval, ...)`); `DelayedRetryProcessor` loops with `Task.Delay(ProcessingInterval, stoppingToken)`; `DeadLetterCleanupProcessor` and `MessagePurgeService` use `Task.Delay(..., _timeProvider, ...)`. They are registered in `MessagingServiceCollectionExtensions.cs` (lines 288, 404, 532) and `RecoverabilityServiceCollectionExtensions.cs` (line 131). There is no Inbox or Saga polling processor.
- `Encina.DistributedLock` references `Encina` and `Encina.Messaging` (`Encina.DistributedLock.csproj`), so the default implementation cannot live in `Encina.Messaging` without a project cycle.
- `IDistributedLockProvider` is pre-ADR-052 (`Task<IAsyncDisposable?>`); `ILockHandle` already has `ExtendAsync` (today `Task<bool>`), `ExpiresAtUtc` and `IsReleased`; ADR-052 makes the provider-level `ExtendAsync` an `Either<EncinaError, bool>`, and the service handles both shapes for the handle (see Phase 2 task 2). Bug #2405 (Redis provider-level `ExtendAsync(resource)` extends a lock it does not own) means renewal must go through `ILockHandle.ExtendAsync`, which checks ownership.
- ADR-050's closed list has the Outbox row ("row claiming or a lock (#1251)") and the CDC row ("row claiming, lock or leader (#1251)"); it has no leader election row, so the requirement of this plan is an amendment.
- `3850-3899` is `TenancyAspNetCore`; the family registry assigns the `DistributedLock` assembly the block 6250-6299 (6250-6269 for the #741 plan, **6270-6299 for this plan**). The #741 plan owns the range entry in `EventIdRanges.cs`, the `AssemblyRanges` mapping, the PublicAPI line and the shared `DistributedLockDiagnostics` (its PR 2); this plan only adds its ids and instruments afterwards, so **PR 2 of this plan starts after PR 2 of the #741 plan merges**.
- The manifests are `.github/coverage-manifest/Encina.Messaging.json` (targets contract 15, guard 20, property 15, unit 70) and `Encina.DistributedLock.json` (guard 20, unit 60).

---

## Design Choices

<details>
<summary><strong>1. One election abstraction or two: build the #216 API first, or the small <code>ILeaderElection</code> of this issue</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Build the general API of #216 first** (`ILeaderElectionProvider` with handle, `IsLeaderAsync`, `GetCurrentLeaderAsync`, `LeadershipLost`), and make this issue the Messaging adoption (flag, processors) on top of it | One public abstraction, no duplicate interfaces, usable outside messaging (#221, scheduled tasks) | This issue waits for #216; the first pull request is larger; #216 has no plan yet |
| **B) Keep this issue as specified**: the small `ILeaderElection` (`IsLeader(name)`) in `Encina.Messaging`, implemented in `Encina.DistributedLock`; fold #216 into it later or close it | Delivers processor failover sooner; smallest surface | Risk of two abstractions and a naming conflict if #216 also ships; `LeadershipLost` cancellation tokens are not offered |

### Chosen Option: **A — build the general API first, then the adoption** (recommended, pending the maintainer)

### Rationale

- A wins when other consumers (#221 `[RequiresLeadership]`, scheduled tasks) are in the 1.0 scope; B wins when only the processors need election before 1.0. The plan's phases are written so that Phases 2 to 4 (service, adoption, health) are the same under both options; only the interface in Phase 1 changes shape (the `IsLeader(name)` reading stays as the processors' view of any richer API).
- Under B, `ILeaderElection` is exactly the interface of Phase 1; under A, Phase 1 becomes "implement #216's interface" and its file list moves to that issue's plan.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Coordination model for the processors: election, per-batch locks, or both</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Leader election only** | Least database polling (followers do not poll); simplest to reason about | A failover gap of up to the lease; a paused ex-leader may finish one batch (no fencing, #218) |
| **B) Per-batch or per-message locks only** (#713-#716, row claiming #1251) | No failover gap; fine grained | Every replica still polls; N times the query load |
| **C) Both** (election decides who polls, a batch lock or row claim is the safety net) | Fewest duplicate publications | Two mechanisms to build and maintain |

### Chosen Option: **A — election only, #713-#716 and #1251 stay open** (recommended, pending the maintainer)

### Rationale

- A wins with many replicas where polling cost matters; B wins with few replicas or when a gap of seconds is unacceptable; C is not needed before 1.0 if duplicate delivery is acceptable under at-least-once semantics.
- The limit (an in-flight batch of a replaced leader can still finish) is stated in the documentation; fencing is #218.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Which processors adopt the election</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The dispatching processors: `OutboxProcessorBase`, `ScheduledMessageProcessor`, `DelayedRetryProcessor`** | These publish or dispatch, so duplicates matter; the base class covers eight packages with one edit | `DeadLetterCleanupProcessor` and `MessagePurgeService` keep running on every replica (idempotent deletes, only load) |
| **B) All five polling processors** | Uniform; least load | Cleanup and purge are idempotent, and a leader-only purge concentrates deletes on one node |
| **C) Only `OutboxProcessorBase` and `ScheduledMessageProcessor`** (the two the original issue names) | Smallest | Delayed retries would still duplicate redelivery |

### Chosen Option: **A — the three dispatching processors** (recommended, pending the maintainer)

### Rationale

- B wins if the operators want exactly one active node for everything; C wins for the smallest first delivery. Each processor reads the gate through one shared helper, so extending the list later is a few lines.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Where the implementation lives and how the processors reach it</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Interface and options in `Encina.Messaging`; implementation, service and health check in `Encina.DistributedLock`; processors resolve `ILeaderElection` optionally from the root provider at start** | Respects the dependency direction (`Encina.DistributedLock` references `Encina.Messaging`); the eight provider processors need no edit; no constructor change | The satellite registration `AddEncinaLeaderElection()` is a separate call |
| **B) Implementation in `Encina.Messaging` over its own lock abstraction** | One package | A project cycle or a duplicate lock abstraction |

### Chosen Option: **A — split by dependency direction**

### Rationale

- Provider coherence: abstractions and options live in `Encina.Messaging`; implementations differ only in the package that supplies them.

**Class:** Settled
**Settled by:** `src/Encina.DistributedLock/Encina.DistributedLock.csproj` (references `Encina.Messaging`), `AGENTS.md` section 5 (provider coherence: abstractions and options live in `Encina.Messaging`)

</details>

<details>
<summary><strong>5. Opt-in and enforcement: <code>UseLeaderElection</code> requires a lock provider and an election implementation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `config.UseLeaderElection = true` plus `AddEncinaLeaderElection()`; startup validation fails when the flag is on and no `ILeaderElection` or no `IDistributedLockProvider` is registered; the processors fail closed (do not run) when the flag is on and no election is resolvable; ADR-050 gets an amendment row** | Opt-in; a missing dependency is a startup error, never a silent single-leader-less or everyone-leads state | One more registration call |
| **B) The flag auto-registers a lock provider (in-memory)** | Nothing to forget | An in-memory lock on several replicas elects a leader per replica, which breaks the guarantee silently |
| **C) No flag: registering the service switches the processors** | Fewer settings | Not the opt-in style of `MessagingConfiguration`; hides the behavior change |

### Chosen Option: **A — flag, explicit registration, startup validation**

### Rationale

- The flag is the switch the processors read, through a marker object (`LeaderElectionMarker`) that `AddEncinaMessaging` registers exactly when the flag is on; the validator reports the module, the requirement, ADR-050 and how to add the missing module.
- Registration is split so options are registered once: `AddEncinaMessaging` (flag on) registers the marker and the startup check "an `ILeaderElection` is registered" (Messaging); `AddEncinaLeaderElection()` (satellite) registers the implementation, `LeaderElectionOptions` with its validator and the startup check "an `IDistributedLockProvider` is registered". Both checks run at host start, so the order of `AddEncinaMessaging`, `AddEncinaLeaderElection` and the provider registration does not matter; a DI test with `ValidateOnBuild` and `ValidateScopes` covers flag on and off and both orders.

**Class:** Settled
**Settled by:** ADR-050 (rule 3: a requirement is enforced by registration or startup validation, never a silent default; a requirement not in the table does not exist, so the table is amended), `AGENTS.md` section 3 (every messaging pattern optional; a module may require another only per ADR-050; gates fail closed)

</details>

<details>
<summary><strong>6. Error semantics and leadership safety</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Election operations return `Either`; a `Left` from acquire or a failed or faulted renewal ends leadership visibly; `IsLeader` is `now < localLeaseExpiryUtc` on the injected `TimeProvider`; the loop catches per iteration; graceful stop disposes the handle** | A stalled loop cannot leave a stale leader; no error is swallowed; failover is immediate on a clean stop | Followers may wait up to one heartbeat to take over |
| **B) `IsLeader` returns the last known state without an expiry check** | Simplest | A frozen renewal loop keeps a replica "leading" past its lease |

### Chosen Option: **A — visible loss, local expiry, per-iteration catch**

### Rationale

- The loop is a `PeriodicTimer` loop: an exception ends it silently, so it catches per iteration (cancellation from the stopping token ends the loop); the failure follows the background `Left` rule.

**Class:** Settled
**Settled by:** ADR-052 (`Left` for faults, cancellation propagates), `AGENTS.md` section 3 (errors never swallowed in background infrastructure; `PeriodicTimer` loops catch per iteration; time from `TimeProvider`)

</details>

<details>
<summary><strong>7. Election names, heartbeat and lease</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One election per processor kind** (`encina:leader:outbox`, `:scheduling`, `:delayed-retry`), heartbeat default 15 s (the issue), lease default 3 heartbeats (45 s); options validated at startup: heartbeat positive, lease greater than twice the heartbeat | Independent processors can lead on different replicas, spreading load; margin for one missed renewal | Three locks per deployment; the 45 s lease is a longer failover gap than a 2-heartbeat lease |
| **B) One election for all processors** | One lock; one leader runs everything | All load on one replica; one slow processor affects the rest |

### Chosen Option: **A — one election per processor kind** (recommended, pending the maintainer)

### Rationale

- The defaults are user-visible and no convention fixes them: only the 15 s heartbeat comes from the issue. The lease of three heartbeats (45 s) leaves room for one missed renewal before failover; a shorter lease fails over faster and risks flapping under a slow lock backend, a longer one widens the failover gap. The Kubernetes leader-election library takes lease duration, renew deadline and retry period as separate settings; this plan folds them into heartbeat and lease. The maintainer picks the defaults and the granularity (A or B) when work starts.
- The processors are independent, so spreading their leadership across replicas costs three small locks (A); B wins when one active node for everything is preferred.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Observability: tags are election name and outcome only</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Observable gauge `encina.leadership.state` (1/0), counters `encina.leadership.transitions` and `encina.leadership.renewals` (tag `outcome`), spans `encina.leadership.acquire` and `encina.leadership.renew` with `election.name`; a health check reporting leader or follower and the age of the last heartbeat** | Bounded cardinality (election names are a small fixed set); the health check detects a dead loop | A follower is healthy by design, so the check reports state as data, not as a failure |
| **B) Add the replica or host name as a tag** | Shows who leads | Unbounded cardinality; host names are infrastructure identifiers in metrics |

### Chosen Option: **A — bounded tags, health check on loop liveness**

### Rationale

- The health check is unhealthy only when the heartbeat loop has not run for more than twice the heartbeat interval.

**Class:** Trivial
**Settled by:** `AGENTS.md` section 6 (functions 2, 3 and 4), `AGENTS.md` section 7 (EventIds), SPEC-002 REQ-062 (no direct identifiers in tags)

</details>

---

## Implementation Phases

Six pull requests (0, 1, 2, 3a, 3b, 4), each at most 15 files and 1500 changed lines counting tests, `PublicAPI.*.txt`, manifests, test-project `ProjectReference` edits, DI tests and fragments (`AGENTS.md` section 11), and each carrying the tests of the flags its own `src/` files need (AGENTS.md section 9; the counts are in Estimated File Count). ADR-050 rule 1 says a requirement that is not in the table does not exist and adding one is an amendment, so the amendment row lands in PR 0, which merges before PR 1 (the first PR that enforces the requirement) and PR 2:

| PR | Content | Own tests |
|----|---------|-----------|
| 0 | Phase 6 task 3, first half: the ADR-050 amendment row "Leader election" (requires distributed locks; mechanism: startup validation), docs only | none (one file) |
| 1 | Phase 1: abstraction, options, validator, names, marker, gate, flag, Messaging startup check (the new internal hosted check file `LeaderElectionStartupCheck.cs`, modelled on `DelayedRetryStartupCheck`, or the validator of #2382 if it landed) | unit (validator, gate, DI both orders), guard; the contract and property flags of these files are 0 with a reason (an interface, an options validator and a pure helper; the election contract and property run in PR 2 against the real implementation) |
| 2 | Phase 2 and the logging and instruments of Phase 3 (tasks 2 and 3): election, service, registration, log messages, instruments. **Starts after PR 2 of the #741 plan merges, which in turn starts after [#2667](https://github.com/dlrivada/Encina/issues/2667)** (the #741 plan owns the `DistributedLock` range entry and `DistributedLockDiagnostics`) | unit, guard, contract over each lock provider, property (never two leaders), integration (competing services on Redis and SqlServer) |
| 3a | Phase 3 task 4 (health check in `Encina.DistributedLock`, registered in `LeaderElectionServiceCollectionExtensions.cs`) | unit (health check), guard, the existing DI test extended to resolve the health check |
| 3b | Phase 3 task 5 (skip outcome) and Phase 4 (gate in the three processors) | unit (three processors), integration (one class per outbox family: ADO, Dapper, EF Core and MongoDB, each on its own shared `[Collection("<Family>-<Database>")]` fixture) |
| 4 | Phase 5 remainder and Phase 6: justification files, documentation, the ADR of Dilemma 1 (the ADR-050 amendment is PR 0) | docs only |

### Phase 1: Abstraction, Options, Flag and Startup Validation

> **Goal**: The contract and the switch, in `Encina.Messaging`.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.Messaging/Leadership/` (new folder)

1. **`ILeaderElection.cs`** — `bool IsLeader(string electionName)`; `IReadOnlyCollection<LeadershipSnapshot> GetSnapshots()` (`ElectionName`, `IsLeader`, `LeaseExpiresAtUtc`, `LastHeartbeatAtUtc`); no I/O. Under Dilemma 1 option A this interface follows #216's API and this file list moves to that plan
2. **`LeaderElectionOptions.cs`** — `TimeSpan HeartbeatInterval` (15 s), `TimeSpan LeaseDuration` (45 s), election names for outbox, scheduling and delayed retry (constants in `LeaderElectionNames`)
3. **`LeaderElectionOptionsValidator.cs`** — `IValidateOptions`: heartbeat positive, lease greater than twice the heartbeat
4. **`LeaderElectionMarker.cs`** (internal sealed) — registered by `AddEncinaMessaging` exactly when `UseLeaderElection` is on; its presence is how the processors learn the flag (they receive `OutboxOptions`, an `IServiceProvider` and a logger, not `MessagingConfiguration`)
5. **`LeaderGate.cs`** (internal static) — `bool ShouldRun(LeaderElectionMarker? marker, ILeaderElection? election, string electionName)`: no marker is "feature off" and returns true; a marker with a null election returns **false** (fail closed, and the caller logs once that the flag is on but no election is registered); otherwise `election.IsLeader(electionName)`

#### `src/Encina.Messaging/`

6. **`MessagingConfiguration.cs`** (modify) — `bool UseLeaderElection { get; set; }` (opt-in, XML docs: requires `AddEncinaLeaderElection()` and a lock provider)
7. **`MessagingServiceCollectionExtensions.cs`** (modify) — when the flag is on: register the `LeaderElectionMarker` and a startup check that fails (`InvalidOperationException` naming module, requirement, ADR-050 and the fix) when no `ILeaderElection` is registered; it does **not** register `LeaderElectionOptions` (the single place is `AddEncinaLeaderElection`, Phase 2 task 3) and it runs at host start, so the order of `AddEncinaMessaging` and `AddEncinaLeaderElection` does not matter; implemented as the validator of #2382 when it lands, otherwise an internal hosted check like `DelayedRetryStartupCheck`
8. **`PublicAPI.Unshipped.txt`**

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #717: the leader election abstraction and switch in Encina.Messaging.

CONTEXT:
- Encina.DistributedLock references Encina.Messaging, so the abstraction and options live in Encina.Messaging and
  the implementation in Encina.DistributedLock (Phase 2).
- MessagingConfiguration has opt-in flags in the UseOutbox style. ADR-050 rule 3: a missing required module is a
  startup error, never a silent default.

TASK:
Create ILeaderElection, LeaderElectionOptions (+ validator), LeaderElectionNames, LeaderElectionMarker, LeaderGate and
the UseLeaderElection flag with its marker registration and startup check, as listed in the Phase 1 tasks. Track
public symbols in PublicAPI.Unshipped.txt (through mechanical-fixer). Write unit and guard tests, including a DI test
with ValidateOnBuild and ValidateScopes for flag on and off, and the gate cases: no marker runs, marker with an
election follows IsLeader, marker with no election does not run.

KEY RULES:
- The gate fails closed: flag on and no election means do not run.
- ILeaderElection.IsLeader performs no I/O and never throws.
- Time from TimeProvider only; no Task.Delay in this phase.
- ArgumentNullException guards; XML docs on every public member; zero warnings.

REFERENCE FILES:
- src/Encina.Messaging/MessagingConfiguration.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina.Messaging/Recoverability/DelayedRetryStartupCheck.cs
- docs/architecture/adr/050-modules-may-require-modules.md
```

</details>

### Phase 2: The Election Service and Default Implementation

> **Goal**: Acquire, renew, lose and fail over, on any lock provider.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.DistributedLock/Leadership/` (new folder; after #2667)

1. **`LockLeaderElection.cs`** (internal sealed) — `ILeaderElection`; holds, per election name, the `ILockHandle?`, `LeaseExpiresAtUtc`, `LastHeartbeatAtUtc` in a lock-free snapshot; `IsLeader` = handle present and `now < LeaseExpiresAtUtc` on the `TimeProvider`
2. **`LeaderElectionService.cs`** (internal sealed, `BackgroundService`) — constructor `(LockLeaderElection, IDistributedLockProvider, IOptions<LeaderElectionOptions>, TimeProvider, ILogger)`; `PeriodicTimer(HeartbeatInterval, timeProvider)`; one iteration per election name, each election in its own task (the iteration awaits them with `Task.WhenAll`), so a hung backend call of one election cannot delay the renewal of another: run sequentially, three elections and a hung backend would make the gap between two renewals of the first election three heartbeats, equal to the 45 s lease, and a healthy election would lose its lease; each call under a token linked to the stopping token and cancelled after one `HeartbeatInterval` (`new CancellationTokenSource(HeartbeatInterval, timeProvider)`), so a hung lock call cannot block the sequential iteration; a timeout ends or withholds leadership as a fault. **The lease anchor is read before the call**: `var started = timeProvider.GetUtcNow()` is taken before `TryAcquireAsync` or `ExtendAsync`, and on success `LeaseExpiresAtUtc = started + LeaseDuration` (never the time after the call returns, which would let a replica believe it leads up to the call latency past the real expiry). A follower tries `TryAcquireAsync(name, LeaseDuration, wait 0, retry 0, ct)` (`Right(Some)` becomes leader, `Right(None)` stays follower, `Left` logs the code and stays follower). A leader calls `handle.ExtendAsync(LeaseDuration, ct)`, which is `Task<bool>` today; ADR-052 makes the provider-level call an `Either` and #2667 may change the handle method too, so the service handles both shapes: `true` or `Right(true)` renews the lease; `false` or `Right(false)` means the lease is lost (not a fault, per ADR-052 a fault is never collapsed into `false`); `Left`, a timeout or an exception loses the lease and counts a fault. Losing the lease disposes the handle. The iteration catches everything except cancellation; `StopAsync` disposes every handle
3. **`LeaderElectionServiceCollectionExtensions.cs`** — `AddEncinaLeaderElection(this IServiceCollection, Action<LeaderElectionOptions>? configure = null)`: the single place that registers `LeaderElectionOptions` (`Configure` plus the validator through `TryAddEnumerable`), `TryAddSingleton<LockLeaderElection>`, `TryAddSingleton<ILeaderElection>`, `AddHostedService<LeaderElectionService>` and the startup check "an `IDistributedLockProvider` is registered" (ADR-050 message); it throws nothing at call time, so the order against the provider registration and `AddEncinaMessaging` does not matter; `TimeProvider`
4. **`Diagnostics/LeaderElectionLogMessages.cs`** — see Phase 3
5. **`PublicAPI.Unshipped.txt`**

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #717: the default leader election over IDistributedLockProvider.

CONTEXT:
- Issue #2667 has changed IDistributedLockProvider to the ADR-052 shapes: TryAcquireAsync returns
  Either<EncinaError, Option<ILockHandle>>; read the current signatures. ILockHandle.ExtendAsync returns Task<bool>
  today; if #2667 changed it to Either, handle Right(true), Right(false) (lease lost, not a fault) and Left (fault).
- The service is a PeriodicTimer loop: it must catch per iteration (cancellation from the stopping token ends the
  loop); a Left or a failed renewal ends leadership visibly; IsLeader must also expire locally so a stalled loop
  cannot keep a stale leader.

TASK:
Implement LockLeaderElection, LeaderElectionService and AddEncinaLeaderElection as listed. Unit tests with
FakeTimeProvider: acquire, renew, lose by false/Left/exception, local expiry without a renewal tick, the lease anchor
taken before the call (a slow fake provider that advances the clock inside the call does not extend the lease), a hung
call cut by the per-heartbeat timeout, a hung call of one election does not delay the renewal of the others (three elections,
one backend call hung: the other two keep their lease), failover between two services over the InMemory provider, graceful stop
releases, cancellation, provider fault keeps the follower, DI test (ValidateOnBuild and ValidateScopes) with flag on
and off in both registration orders.

KEY RULES:
- LeaseExpiresAtUtc = the time read before the acquire or extend call + the lease.
- Time only from the injected TimeProvider (PeriodicTimer(interval, timeProvider), GetUtcNow); never Task.Delay
  without it.
- Renew through ILockHandle.ExtendAsync, never the provider-level ExtendAsync(resource) (bug #2405).
- Log error codes or exception types (ForLogging()), never EncinaError.Message.

REFERENCE FILES:
- src/Encina.DistributedLock/IDistributedLockProvider.cs
- src/Encina.DistributedLock/ILockHandle.cs
- src/Encina.DistributedLock.InMemory/InMemoryDistributedLockProvider.cs
- docs/architecture/adr/052-distributed-lock-failure-contract.md
```

</details>

### Phase 3: Observability — Diagnostics, Metrics, Health and Logging

> **Goal**: Leadership is visible and a dead loop is detected.

<details>
<summary><strong>Tasks</strong></summary>

1. **Range** — already registered by the #741 plan (its PR 2 adds `DistributedLock = (6250, 6299)` to `src/Encina/Diagnostics/EventIdRanges.cs`, the `Encina.DistributedLock` mapping in `EncinaEventIdAllocationTests` and the PublicAPI line); this plan adds none of them and uses 6270-6277; 6278-6299 stay reserved for this family
2. **`Diagnostics/LeaderElectionLogMessages.cs`** — `[LoggerMessage]` 6270 became leader (Information), 6271 lost leadership with a code or exception type (Warning), 6272 acquire fault (Warning), 6273 renewal failed (Warning), 6274 heartbeat iteration failed (Error, `ForLogging()`), 6275 released on shutdown (Debug), 6276 election service started (Information), 6277 lease expired locally without a renewal (Warning)
3. **`Diagnostics/DistributedLockDiagnostics.cs`** (modify; created by the #741 plan as the single shared holder, so there is one `ActivitySource` and one `Meter` named `Encina.DistributedLock`; the lock instruments of #724 join it if it lands first) — add observable gauge `encina.leadership.state`, counters `encina.leadership.transitions` and `encina.leadership.renewals` (tag `outcome`), spans `encina.leadership.acquire` and `encina.leadership.renew` (attribute `election.name`)
4. **`Health/LeaderElectionHealthCheck.cs`** — `IEncinaHealthCheck`; healthy while the last heartbeat is younger than twice the interval, unhealthy otherwise; leader or follower is reported as data, not as status
5. Processor skip counter: outcome `skipped_not_leader` on the existing processor counters (`Diagnostics/OutboxProcessorMetrics.cs`, `SchedulingProcessorMetrics.cs`), no new Messaging EventId

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #717: logging, telemetry and health of the election.

CONTEXT:
- AGENTS.md section 7: ids only inside a range registered first in EventIdRanges.cs, packed, no gaps; the assembly is
  mapped in EncinaEventIdAllocationTests. The #741 plan registers the range and owns 6250-6269; this plan owns
  6270-6299 and registers nothing.
- Health checks: DefaultName const, static Tags array, scoped resolution through IServiceProvider.CreateScope()
  where scoped services are needed (the election is a singleton).

TASK:
Register or reuse the range, write the log messages, diagnostics and health check as listed, wire them into the
service, and run the EventId architecture tests.

KEY RULES:
- Tags: election name and outcome only; never a host name, replica id or lock key.
- Health result descriptions carry no exception message.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Messaging/Health/OutboxHealthCheck.cs
- src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 4: Adoption in the Processors

> **Goal**: Followers skip their cycle; the eight outbox packages are covered by one edit.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Outbox/OutboxProcessorBase.cs`** (modify) — in each loop iteration resolve the `LeaderElectionMarker` and `ILeaderElection` with `GetService` from the root provider (both are singletons; resolving per cycle and not once at start means an election that registers late is picked up and never leaves a window in which every replica runs); call `LeaderGate.ShouldRun(marker, election, name)` before `ProcessPendingMessagesAsync`; flag on with no election resolvable skips the cycle and logs once; a skipped cycle still waits the processing interval; the sealed method and the constructor signature stay unchanged, so the eight provider `OutboxProcessor` classes are not edited
2. **`Scheduling/ScheduledMessageProcessor.cs`** (modify) — the same gate before `ProcessOnceAsync`
3. **`Recoverability/DelayedRetryProcessor.cs`** (modify) — the same gate before the batch
4. **Existing tests** — the processor tests gain a leader and follower case each; the follower case asserts the store is never queried
5. `DeadLetterCleanupProcessor` and `MessagePurgeService` are unchanged (design choice 3)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #717: the processors consult the election.

CONTEXT:
- OutboxProcessorBase.ExecuteAsync is sealed and loops ProcessPendingMessagesAsync + Task.Delay; eight provider
  packages derive from it. ScheduledMessageProcessor and DelayedRetryProcessor have the same loop shape.
- With the feature off (no LeaderElectionMarker, that is UseLeaderElection false) behavior must be unchanged; with the
  flag on and no election resolvable the processor does NOT run (fail closed).

TASK:
Add the gate to the three processors as listed and extend their unit tests: leader runs, follower skips and does
not touch the store, feature off runs, flag on with no election registered skips and logs once, leadership lost
mid-run stops at the next cycle. Add one integration test per
database family (ADO, Dapper, EF Core, MongoDB) proving a follower outbox processor leaves its pending rows to the
leader.

KEY RULES:
- Do not change the sealed ExecuteAsync contract or any constructor signature.
- A skipped cycle is not an error and not a failure counter.
- Per-flag coverage of every touched file must not go below its manifest targets.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxProcessorBase.cs
- src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs
- src/Encina.Messaging/Recoverability/DelayedRetryProcessor.cs
- .github/coverage-manifest/Encina.Messaging.json
```

</details>

### Phase 5: Testing

> **Goal**: Every flag reaches its own target; failover runs against real infrastructure.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** — phases 1 to 4 (`tests/Encina.UnitTests/Messaging/`, `tests/Encina.UnitTests/DistributedLock/`)
2. **Guard** — constructors and public methods of the new types
3. **Contract** (`tests/Encina.ContractTests/`) — the election runs over each lock provider of the suite created by #2667 (InMemory, Redis, SqlServer now; the four planned providers when they land): one leader at a time, release on stop, failover after the lease
4. **Property** (`tests/Encina.PropertyTests/`) — FsCheck with a fake clock and random heartbeat delays: never two replicas with `IsLeader` true at the same instant while leases are valid; `IsLeader` false after local expiry
5. **Integration** (`tests/Encina.IntegrationTests/`) — Redis and SqlServer through the existing Testcontainers fixtures: three competing services, stop the leader, observe failover within lease plus heartbeat; kill the lock backend, observe all replicas step down
6. **Load** — extend the NBomber lock scenarios with an election scenario (renewal frequency) or a justification file if the maintainer prefers; **Benchmark** — justification file (acquisition latency is the lock provider's)
7. **Coverage manifest** — per-file targets with a one-sentence justification each, written by `mechanical-fixer` in the PR that adds the file (targets never go down; the touched processors keep their current targets and gain a case each). The lease and gate logic is small and fully testable with `FakeTimeProvider`:

   | File | Unit | Guard | Contract | Property | Integration | Justification |
   |------|-----:|------:|---------:|---------:|------------:|---------------|
   | `LeaderGate.cs` | 100 | 0 | 0 | 0 | 0 | Three-way pure function; internal static with no public parameters |
   | `LeaderElectionOptionsValidator.cs` | 100 | 0 | 0 | 0 | 0 | Two rules, internal |
   | `LeaderElectionOptions.cs`, `LeaderElectionMarker.cs` | 90 | 100 | 0 | 0 | 0 | Property bag and marker; guard on setters |
   | `LockLeaderElection.cs` | 95 | 100 | 90 | 90 | 80 | Lease expiry, snapshots and leadership state, all driven by `FakeTimeProvider`; property proves never two leaders; contract runs per lock provider |
   | `LeaderElectionService.cs` | 90 | 100 | 85 | 0 | 80 | Acquire, renew, lose, timeout and stop paths; the loop is exercised against real providers in the integration tests |
   | `LeaderElectionServiceCollectionExtensions.cs`, `MessagingServiceCollectionExtensions.cs` (new branch) | 90 | 100 | 0 | 0 | 0 | Registration branches for flag on and off, both orders |
   | `LeaderElectionHealthCheck.cs` | 95 | 100 | 0 | 0 | 0 | Healthy, stale, no heartbeat yet |

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #717: tests of the leader election.

CONTEXT:
- Tests execute real package code; assertions use Shouldly through Encina.Testing.Shouldly; time from FakeTimeProvider.
- Integration tests use shared [Collection] fixtures and never swallow an exception; containers are stopped with the
  Testcontainers API.
- Coverage is per flag against the manifests of Encina.Messaging and Encina.DistributedLock.

TASK:
Write the tests of the Phase 5 tasks, measure each flag with
`dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and report the per-file table.

KEY RULES:
- No Thread.Sleep; advance the fake clock instead.
- A test that drives FakeTimeProvider.Advance must fix, not swallow, a throwing callback.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/DistributedLock/
- tests/Encina.UnitTests/DistributedLock/
- .github/coverage-manifest/Encina.DistributedLock.json
- .github/coverage-manifest/Encina.Messaging.json
```

</details>

### Phase 6: Documentation and Finalization

> **Goal**: Operators can enable it and know its limits; the records are complete.

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation on every new public member (`<example>` on `AddEncinaLeaderElection` and the flag)
2. `docs-writer`: README of `Encina.Messaging` and `Encina.DistributedLock`, a leader election guide (configuration, heartbeat and lease, failover gap, no fencing, what is not covered), scaling guidance
3. **ADR-050 amendment** — a row "Leader election" (requires distributed locks; mechanism: startup validation) via `mechanical-fixer`, shipped in PR 0 before the enforcing code of PR 1 and PR 2; **ADR** for the abstraction decision of Dilemma 1 (number assigned when the ADR pull request opens); the amendment edits the existing ADR-050 and is named separately from the new ADR
4. `changelog.d/717-leader-election.added.md` (`mechanical-fixer`); `PublicAPI.Unshipped.txt` complete; `docs/INVENTORY.md` if packages or modules change; `ROADMAP.md` if the milestone text changes
5. `dotnet build --configuration Release` with 0 warnings; `dotnet test`; every flag at its target; CRAP <= 10 on changed methods (local crap-gate table)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are finishing issue #717: documentation and verification.

CONTEXT:
- Documentation goes through docs-writer and the encina-docs skill; the changelog fragment, PublicAPI lines, the
  ADR-050 row and the coverage manifest go through mechanical-fixer.

TASK:
Complete the Phase 6 tasks and report the per-flag figures of the touched files against their targets.

KEY RULES:
- State the limits: failover gap up to the lease, an in-flight batch of a replaced leader can finish, no fencing
  tokens (#218), per-operation locks (#713-#716) and row claiming (#1251) are separate.
- English only; no private client application named; no hand-typed coverage figures.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- docs/architecture/adr/050-modules-may-require-modules.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Provision | Relevance |
|--------|-----------|-----------|
| ADR-052 | Lock providers return `Either`; contention is `Right(None)` | The election's acquire mapping |
| ADR-050 | Closed list of module requirements; rule 3 enforcement | Startup validation; a new row is an amendment |
| ADR-001 | Errors are values | Election results as `Either` internally |
| SPEC-000 REQ-027, DEC-009, AC-027 | Seven lock providers with contract tests | The election is contract-tested over each |
| SPEC-002 REQ-062 | No direct identifiers in tags | Tag policy |
| Hangfire, Kubernetes Leases (`coordination.k8s.io`) | Single active node / lease with renew time | Named comparable products for lease-based election |
| `AGENTS.md` sections 3, 5, 6, 7 | Timers, errors, provider coherence, cross-cutting, EventIds | Constraints throughout |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `OutboxProcessorBase` | `src/Encina.Messaging/Outbox/OutboxProcessorBase.cs` | Gate added once; eight provider processors inherit it |
| `ScheduledMessageProcessor` | `src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs` | Gate before `ProcessOnceAsync` |
| `DelayedRetryProcessor` | `src/Encina.Messaging/Recoverability/DelayedRetryProcessor.cs` | Gate before the batch |
| `MessagingConfiguration` | `src/Encina.Messaging/MessagingConfiguration.cs` | Home of `UseLeaderElection` |
| `MessagingServiceCollectionExtensions` | `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs` | Option registration and startup check |
| `DelayedRetryStartupCheck` | `src/Encina.Messaging/Recoverability/` | Model for the interim startup check |
| `IDistributedLockProvider`, `ILockHandle` | `src/Encina.DistributedLock/` | The engine: acquire, `ExtendAsync` renewal, dispose |
| `InMemoryDistributedLockProvider` | `src/Encina.DistributedLock.InMemory/` | Fast tests of failover between two services |
| `OutboxHealthCheck` | `src/Encina.Messaging/Health/OutboxHealthCheck.cs` | Model for health check shape |
| `OutboxProcessorMetrics`, `SchedulingProcessorMetrics` | `src/Encina.Messaging/Diagnostics/` | Processor counters gain `skipped_not_leader` |
| Lock integration fixtures | `tests/Encina.IntegrationTests/Infrastructure/DistributedLock/` | Redis and SQL Server competing-instance tests |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Provider | Election support | Notes |
|----------|:-:|-------|
| InMemory | Included | Contract and unit tests; single process only (never leader-safe across replicas) |
| Redis | Included | Contract and Testcontainers failover test; the RedLock quorum (#226) strengthens it |
| SqlServer | Included | Contract and Testcontainers failover test |
| PostgreSQL (#207), MySQL (#208), Azure Blob (#209), DynamoDB (#210) | Deferred | Covered by the abstraction when each lands, with a contract-suite entry; tracked by those issues |

Database (10): the outbox processors in ADO.SqlServer, ADO.PostgreSQL, ADO.MySQL, Dapper.SqlServer, Dapper.PostgreSQL, Dapper.MySQL, EntityFrameworkCore (one class for the three EF providers) and MongoDB derive from `OutboxProcessorBase`; no provider code changes, one integration test per family proves the adoption.

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.DistributedLock` (`DistributedLock` entry, 6250-6299) | 6270-6277 | Election service messages; 6278-6299 is a reserved sub-block of this family (also reserved in the registry), not left unplanned. The range entry, the `AssemblyRanges` mapping and the PublicAPI line are owned by the #741 plan (6250-6269 there; 6253-6269 reserved) |
| `Encina.Messaging` | none | The processors' skip is a counter outcome, not a log line |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production (new) | 11 | `ILeaderElection`, options, validator, names, marker, gate, `LeaderElectionStartupCheck.cs` (Messaging); `LockLeaderElection`, `LeaderElectionService`, registration extension, health check and log messages (DistributedLock) |
| Production (modified) | 8 | `MessagingConfiguration`, `MessagingServiceCollectionExtensions`, three processors, two metrics classes, `DistributedLockDiagnostics.cs` (the range files are the #741 plan's) |
| PublicAPI and manifests | 4 | Two `PublicAPI.Unshipped.txt`, two manifest files |
| Unit and guard tests | 10 | Includes the DI tests and the three processor test files |
| Contract, property, integration tests | 8 | Contract, property, two lock integration classes (PR 2) and four outbox-family integration classes (PR 3b) |
| Justification files | 2 | Load (if not implemented) and benchmark |
| Documentation and changelog | 6 | Two READMEs, guide, ADR-050 amendment, ADR, fragment |
| **Total** | **~57** | Across 6 pull requests; the sum of the PR rows below (the `DistributedLock` `PublicAPI.Unshipped.txt`, manifest and registration extension are touched in PR 2 and again in PR 3a and counted in both) |

| PR | Files | Lines (estimate) |
|----|------:|-----------------:|
| 0: ADR-050 amendment | 1 (`docs/architecture/adr/050-modules-may-require-modules.md`) | ~20 |
| 1: Phase 1 | 15 (7 new (including `LeaderElectionStartupCheck.cs`) and 2 modified production, `PublicAPI.Unshipped.txt`, manifest, 4 test files: validator, gate, DI, guard) | ~800 |
| 2: Phase 2 and Phase 3 tasks 2-3 | 14 (`LockLeaderElection`, service, registration extension, log messages, `DistributedLockDiagnostics` modified, `PublicAPI.Unshipped.txt`, manifest, 7 test files: election and service unit (2), DI, guard, contract, property, integration) | ~1400 |
| 3a: Phase 3 task 4 | 7 (health check, `LeaderElectionServiceCollectionExtensions.cs` edit, `PublicAPI.Unshipped.txt` and manifest of `Encina.DistributedLock`, health unit test, guard test, DI test edit) | ~350 |
| 3b: Phase 3 task 5 and Phase 4 | 13 (three processors, two metrics classes, `Encina.Messaging` manifest, three processor unit test files, four integration files, one per outbox family: ADO, Dapper, EF Core, MongoDB) | ~1000 |
| 4: Phase 5 remainder and Phase 6 | 7 (2 justification files, 2 READMEs, guide, ADR, fragment) | ~580 |

Rows sum to 1 + 15 + 14 + 7 + 13 + 7 = 57. If PR 2 measures over 1500 lines (`git diff --shortstat origin/main...HEAD` on the PR branch), the integration test file moves to its own PR 2b.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #717: opt-in leader election for Encina's polling background processors.

PROJECT CONTEXT:
- .NET 10 / C# 14, nullable enabled, pre-1.0: best solution first, no compatibility layers, no [Obsolete].
- Lock contract: ADR-052, implemented by #2667 before this work (Either<EncinaError, Option<ILockHandle>>).
- Encina.DistributedLock references Encina.Messaging: the abstraction is in Messaging, the implementation in
  DistributedLock.

IMPLEMENTATION OVERVIEW:
1. Messaging: ILeaderElection (IsLeader(name), GetSnapshots), LeaderElectionOptions (heartbeat 15 s, lease 45 s,
   pending Dilemma 7) with a validator, LeaderElectionNames, LeaderElectionMarker (present exactly when the flag is
   on), LeaderGate (fail closed: marker and no election means do not run), MessagingConfiguration.UseLeaderElection,
   startup checks that fail without an ILeaderElection or lock provider (ADR-050 rule 3, order independent).
2. DistributedLock: LockLeaderElection + LeaderElectionService (PeriodicTimer on TimeProvider; acquire with
   TryAcquireAsync, renew with ILockHandle.ExtendAsync; lease anchored on the time read before the call; per-call
   timeout of one heartbeat; per-iteration catch; local lease expiry; dispose on stop) and AddEncinaLeaderElection
   (the single place that registers LeaderElectionOptions).
3. Observability: DistributedLock EventId range 6250-6299 is registered by the #741 plan (this plan 6270-6277), gauge,
   counters, spans added to the shared DistributedLockDiagnostics, health check on loop liveness. Six PRs (0, 1, 2, 3a, 3b, 4).
4. Adoption: OutboxProcessorBase (covers eight packages), ScheduledMessageProcessor, DelayedRetryProcessor skip a
   cycle when not leader; cleanup and purge processors unchanged.
5. Tests: unit, guard, contract over each lock provider, property (never two leaders while leases are valid),
   integration (competing services on Redis and SqlServer, kill the leader), outbox adoption per family; docs through
   docs-writer; changelog, PublicAPI, ADR-050 row and manifests through mechanical-fixer.

KEY PATTERNS:
- A Left, a failed renewal or an exception ends leadership visibly; nothing is swallowed.
- IsLeader is false after the local lease expiry even if the renewal loop is stalled.
- Followers skip cycles silently (counter outcome only); sealed ExecuteAsync and constructor signatures stay.

REFERENCE FILES:
- docs/plans/leader-election-implementation-plan-717.md
- docs/architecture/adr/052-distributed-lock-failure-contract.md
- docs/architecture/adr/050-modules-may-require-modules.md
- src/Encina.Messaging/Outbox/OutboxProcessorBase.cs
- .github/coverage-manifest/Encina.DistributedLock.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Election state is coordination state, not cacheable data |
| 2 | OpenTelemetry | ✅ | Phase 3: gauge, transition and renewal counters, acquire and renew spans, bounded tags |
| 3 | Structured Logging | ✅ | Phase 3: `[LoggerMessage]` 6270-6277 inside the `DistributedLock` range registered first |
| 4 | Health Checks | ✅ | Phase 3: `LeaderElectionHealthCheck` on heartbeat liveness |
| 5 | Validation | ✅ | Phase 1: options validator and the startup check for the flag |
| 6 | Resilience | ✅ | Phases 2 and 6: lease margin of three heartbeats absorbs one missed renewal; a failed renewal is loss of leadership; no Polly policy around the lock call |
| 7 | Distributed Locks | ✅ | The election is built on `IDistributedLockProvider` (Phase 2) |
| 8 | Transactions | ❌ | Control flow, not a data transaction |
| 9 | Idempotency | ❌ | Processors keep at-least-once semantics; the limits (failover gap, no fencing) are documented; per-operation protection is #713-#716 and #1251 |
| 10 | Multi-Tenancy | ⏭️ | Processors are global today; per-tenant election names would build on [#741](https://github.com/dlrivada/Encina/issues/741) (existing issue) |
| 11 | Module Isolation | ❌ | Elections are named per processor kind; `KeyPrefix` separates deployments; no module data held |
| 12 | Audit Trail | ❌ | Leadership changes are operational events recorded by logs and metrics, not data access or state changes of audited entities |

---

## Prerequisites & Dependencies

| Item | State | Effect on this plan |
|------|-------|---------------------|
| [#2667](https://github.com/dlrivada/Encina/issues/2667) — lock providers return `Either` (ADR-052) | Open, implementation issue | Phases 2 to 5 start after it merges; Phase 1 can start earlier (PR 1 builds on `main` alone) |
| [#216](https://github.com/dlrivada/Encina/issues/216), [#221](https://github.com/dlrivada/Encina/issues/221) — general election API and `[RequiresLeadership]` | Open, `status:proposal` | Dilemma 1; under option A, #216 is planned first |
| [#2382](https://github.com/dlrivada/Encina/issues/2382) — ADR-050 startup validator | Open | The Phase 1 startup check migrates into it when it exists |
| [#2405](https://github.com/dlrivada/Encina/issues/2405) — Redis provider-level `ExtendAsync` ownership | Open | Avoided by renewing through `ILockHandle.ExtendAsync` |
| [#218](https://github.com/dlrivada/Encina/issues/218), [#223](https://github.com/dlrivada/Encina/issues/223), [#713](https://github.com/dlrivada/Encina/issues/713)-[#716](https://github.com/dlrivada/Encina/issues/716), [#1251](https://github.com/dlrivada/Encina/issues/1251) | Open | Out of scope; documented as the complementary mechanisms and limits |
| [#741](https://github.com/dlrivada/Encina/issues/741) plan | Same family | Owns the `DistributedLock` EventId range entry (6250-6299), the `AssemblyRanges` mapping and `DistributedLockDiagnostics`; PR 2 of this plan starts after PR 2 of that plan merges |
| [#207](https://github.com/dlrivada/Encina/issues/207)-[#210](https://github.com/dlrivada/Encina/issues/210) | Open | Providers covered when they land |

---

## Next Steps

1. Link this plan from issue #717 and start the PR 0 and PR 1 briefs (PR 0 merges first); PR 2 onward after [#2667](https://github.com/dlrivada/Encina/issues/2667) merges.
2. When work on the issue starts, ask the maintainer to answer Dilemmas 1, 2, 3 and 7 (one abstraction or two; election, locks or both; which processors; heartbeat, lease and election granularity), record the answers in a `## Maintainer Decisions` section placed last, and have the adversarial-reviewer verify the Settled and Trivial choices.
3. One commit per phase; the last commit of PR 4 references `Fixes #717`.
