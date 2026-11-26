# Implementation Plan: Leader Election Provider (`ILeaderElectionProvider`, leadership handle and background renewal)

> **Issue**: [#216](https://github.com/dlrivada/Encina/issues/216)
> **Type**: Feature
> **Complexity**: High (a public coordination API over the lock providers, a background loop, 3 pull requests)
> **Estimated Scope**: ~900 lines of production code + ~1,700 lines of tests (about 2,600 lines over 3 pull requests; the per-PR estimates in "Estimated File Count" sum to it)
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027 (lock providers); [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md); [ADR-050](../architecture/adr/050-modules-may-require-modules.md) (registration errors); [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md)
> **Depends on**: [#2667](https://github.com/dlrivada/Encina/issues/2667) (implements ADR-052; open, not a pending maintainer decision); PR 2 of [#215](https://github.com/dlrivada/Encina/issues/215) (registers the EventId range of the family) for the log-bearing PR 2 of this plan; [#2405](https://github.com/dlrivada/Encina/issues/2405) (open bug: the SQL Server lock does not enforce its expiry) for the "leader stops renewing" scenario on SQL Server in PR 3 (Phase 3 task 1)
> **Related**: [#717](https://github.com/dlrivada/Encina/issues/717) (leader election for the message processors; plan on main, overlaps: Design Choice 1), [#221](https://github.com/dlrivada/Encina/issues/221) (`[RequiresLeadership]` behavior, consumer), [#223](https://github.com/dlrivada/Encina/issues/223) (auto-extend), [#2405](https://github.com/dlrivada/Encina/issues/2405) (Redis `ExtendAsync(resource)` extends a lock it does not own), [#218](https://github.com/dlrivada/Encina/issues/218) (fencing); family `lock-primitives` (#215, #217, #218, #219)

---

## Summary

Several Encina features need "exactly one instance does this": scheduled tasks, singleton workers, the outbox poller. Today the only tool is a raw lock, which leaves every caller to write its own renew loop, lost-lease handling and shutdown release. This plan adds an opt-in **leader election API** to `Encina.DistributedLock`, built on `IDistributedLockProvider` so that every lock provider of SPEC-000 DEC-009 works with no provider code:

1. **Handle API.** `ILeaderElectionProvider.TryAcquireLeadershipAsync` / `AcquireLeadershipAsync` return an `ILeadershipHandle` (ADR-052 shapes: `Right(Some(handle))` when elected, `Right(None)` when another instance leads, `Left` on a backend fault). The handle renews the lease through `ILockHandle.ExtendAsync` (ownership-checked), raises `LeadershipLost`, and releases on dispose.
2. **Safe leadership.** `IsStillLeader` and `IsLeaderAsync` are true only while `now < localLeaseExpiryUtc`, where the expiry is the time read **before** the acquire or extend call plus the lease (the lock backend starts its own clock after that instant). A stalled loop, a slow call or a lost lock can never leave an instance believing it still leads; any doubt is "not leader" (fail closed).
3. **Background renewal.** `LeaderElectionHostedService` campaigns for the configured election names on a `PeriodicTimer` over `TimeProvider`, renews while leading, re-campaigns after a loss, and releases on graceful shutdown. It catches every failure per iteration and per election.
4. **Operations.** Metrics (transitions, leadership duration, elections led), six log events, a health check on loop liveness.
5. **Opt-in.** Nothing is registered unless `AddEncinaLeadershipCampaign(...)` is called; it fails at registration when no lock provider is registered.

**Standards**: ADR-052; ADR-050 (registration errors); ADR-054 D3, D4, D6.3; ADR-062; `AGENTS.md` section 3 (background loops never let an exception escape; errors never swallowed); lease-based election as in Kubernetes `coordination.k8s.io` Leases and Hangfire's single active server (named comparable products, not dependencies); the [Leader Election pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/leader-election).

**Estimated scope**: about 11 new production files, 3 modified, about 14 test files, README and changelog fragment; 3 pull requests (see Implementation Phases).

**Affected packages**: `Encina.DistributedLock` (everything); `Encina` (EventId registration is #215's); test projects `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.IntegrationTests`. No provider package changes: the election runs on any `IDistributedLockProvider`.

**Provider category**: Distributed locks (AGENTS.md section 5) as the engine: the election works on all seven providers (InMemory, Redis, SQL Server now; PostgreSQL, MySQL, Azure Blob, DynamoDB when #207 to #210 land), proven by one contract scenario per provider. Database (10), caching and transports do not apply.

**Shared pieces and ownership (family `lock-primitives` and #717)**: the EventId range `DistributedLockPrimitives` (30550-30564) is registered by #215 (its PR 2); this plan uses 30553-30558. `DistributedLockDiagnostics` (source and meter `Encina.DistributedLock`) is created by whichever of #724 and #741 lands first; this plan adds instruments. #717 owns the adoption by the message processors (`ILeaderElection`, `UseLeaderElection`, the three processors); this plan owns the general API (Design Choice 1 states the dependency under each outcome). **Type-name ownership against the #717 plan on main** (every public or registered name both plans touch, grepped in `leader-election-implementation-plan-717.md`):

| Name | #717 plan (stays as is under A and B) | This plan |
|------|--------------------------------------|-----------|
| `LeaderElectionOptions`, `LeaderElectionOptionsValidator` (`Encina.Messaging`: `HeartbeatInterval`, `LeaseDuration`, the three processor election names) | creates them | not created: this plan's options are `LeadershipCampaignOptions` and `LeadershipCampaignOptionsValidator` (`Encina.DistributedLock`: `DefaultLeaseDuration`, `RenewalInterval`, `Elections`, `AutoRenew`) |
| `LeaderElectionHealthCheck` (`Encina.DistributedLock`, heartbeat liveness) | creates it | `LeadershipCampaignHealthCheck` (`DefaultName = "encina-leadership-campaign"`) |
| `AddEncinaLeaderElection(Action<LeaderElectionOptions>?)` (`Encina.DistributedLock`) | creates it | `AddEncinaLeadershipCampaign(Action<LeadershipCampaignOptions>?)`; no second extension method with the #717 name, so there is no call-site ambiguity and no double registration |
| `LeaderElectionService` (its loop), `LockLeaderElection`, `LeaderElectionLogMessages`, `ILeaderElection`, `LeaderElectionMarker`, `LeaderGate`, `UseLeaderElection` | creates them | `LeaderElectionHostedService`, `LeaderElectionProvider`, `LeaderElectionLog`, `LeaderElectionMetrics`: distinct names, no collision |
| `ILeaderElectionProvider`, `ILeadershipHandle` | names them in its Summary and Dilemma 1 as this plan's | creates them |

The two registrations coexist: under Dilemma 1 option B both loops would exist (two elections of the same resource names must then be avoided: this plan's resources are `leader:{name}`, #717's are `encina:leader:*`); under A, #717's `AddEncinaLeaderElection` becomes a thin registration of the adoption view that depends on `AddEncinaLeadershipCampaign`, and #717's file list loses its engine files (its Phase 2 loop). The renaming makes the choice between A and B independent of any type collision.

**What the code does today** (`origin/main` at `326e65ebf`):

- Nothing of this feature exists: no `ILeaderElectionProvider`, `ILeadershipHandle` or leader service under `src/` (`grep` for `LeaderElection` and `Leadership` under `src/` returns nothing). `IDistributedLockProvider` has the pre-ADR-052 shape; #2667 changes it.
- `ILockHandle` has `ExtendAsync(TimeSpan, CancellationToken)`. Extension is a reset to now plus the extension (`PEXPIRE` in `RedisDistributedLockProvider`'s `ExtendLockScript`; `now.Add(extension)` in the InMemory provider), so renewing with the lease duration every renewal interval keeps the lease at most one lease ahead.
- No lock provider exposes the identity of the current holder: `IDistributedLockProvider` offers `IsLockedAsync(resource)` only. That decides Design Choice 2.
- The existing health checks follow `IEncinaHealthCheck` with a `DefaultName` and `Tags` (`RedisDistributedLockHealthCheck`).
- `Encina.DistributedLock` references `Encina` and `Encina.Messaging`; background hosted services in the repository use `PeriodicTimer` (for example `TopologyRefreshHostedService`).
- `.github/coverage-manifest/Encina.DistributedLock.json` has package targets guard 20 and unit 60; the package has no `InternalsVisibleTo` for `Encina.GuardTests` (guard tests here target public types).

---

## Design Choices

<details>
<summary><strong>1. One election abstraction: this general API first, or #717's small `ILeaderElection` first</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) This plan builds the general API; #717 becomes the Messaging adoption on top of it** (its `ILeaderElection.IsLeader(name)` is a read-only view over this plan's state) | One public abstraction and one engine; usable by #221 and by scheduled tasks | #717 waits for PR 2 of this plan |
| **B) #717 ships its small `ILeaderElection` and heartbeat `LeaderElectionService` first; this plan is built after it and reuses that engine** | The processors get failover sooner | A second public surface; this plan's handle API re-bases onto #717's loop |

### Chosen Option: **A - this plan first** (recommended, pending the maintainer)

### Rationale

- This is the same dilemma as Design Choice 1 of the #717 plan, seen from the other side; the maintainer answers it once. A wins when other consumers than the processors are in the 1.0 scope (#221); B wins when only the processors need election before 1.0. The type names do not depend on the answer (this plan's options, validator, health check and extension method are renamed, see "Type-name ownership" in the header). What changes under A is the size of #717's file list (its engine, `LockLeaderElection` and `LeaderElectionService`, moves here and shrinks to an adoption view over `ILeaderElectionProvider`); Phases 1 to 3 here are the same, except under B this plan reuses #717's heartbeat loop and drops `LeaderElectionHostedService`.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. `GetCurrentLeaderAsync` and `LeaderInfo`: who can read the leader's identity</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Drop `GetCurrentLeaderAsync` and `LeaderInfo`; offer `IsLeaderAsync(name)` (this instance) and `HasLeaderAsync(name)` (some instance, through `IsLockedAsync`)** | Works on all seven providers with no provider change; nothing consumes the identity today | The issue's `LeaderInfo` is not delivered |
| **B) Extend the lock provider contract with a holder description (`GetLockInfoAsync(resource)`) and implement it in seven providers** | A real "who leads" answer for dashboards | A change to the contract of ADR-052 and to every provider (and to #2667's work in flight); `InstanceId` would become a stored value (a machine name) |

### Chosen Option: **A - local answer plus "some instance leads"** (recommended, pending the maintainer)

### Rationale

- A wins until a consumer needs the identity (an operations dashboard); B wins then, and is its own issue because it touches the lock contract in all providers. `LeadershipCampaignOptions.InstanceId` is dropped under A (it would be unused); the leadership identity is the lock's `LockId`.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How the holder learns it lost leadership: event only or event plus a token</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The issue's `event EventHandler<LeadershipLostEventArgs>? LeadershipLost` only** | Matches the issue | Work already running as leader has no way to stop itself except by subscribing and signalling manually |
| **B) The event plus `CancellationToken LeadershipLostToken` on the handle, cancelled when leadership ends** | Leader work takes the token and stops by itself; the event stays for observers | One more member to document and test |

### Chosen Option: **B - event plus token** (recommended, pending the maintainer)

### Rationale

- B wins when leader work is long-running (a job loop); A wins when a lost-lease notification is all that is needed. Event handlers are invoked inside the loop with their own `try/catch`: a handler exception is logged (event 30558) by exception type and never ends the loop.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. The renewal loop of the hosted service</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `PeriodicTimer` loop over `TimeProvider`; each iteration and each election has its own `try/catch`; only the stopping token's `OperationCanceledException` ends the loop; a failure is visible (log by exception type, counter, leadership marked lost)** | Matches the rule for background loops; deterministic tests with `FakeTimeProvider` | One more catch block |
| **B) Let an exception escape and rely on the host to restart** | Less code | In a `BackgroundService` the default behavior stops the host; the failure is swallowed otherwise |

### Chosen Option: **A - per-iteration catch**

### Rationale

- Only A complies with the rule that background loops never let an exception escape and that a failed operation fails visibly instead of reporting success.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (background loops and timers never let an exception escape; errors are never swallowed in background infrastructure; why: #2774)

</details>

<details>
<summary><strong>5. Timing: when does an instance stop believing it leads, and the defaults</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `localLeaseExpiryUtc = timeBeforeCall + lease`; leadership ends at that instant even if no renewal answered; defaults `DefaultLeaseDuration` 30 s and `RenewalInterval` 10 s as in the issue; a validator requires `RenewalInterval * 2 <= DefaultLeaseDuration`** | Safe under slow calls and stalls; one renewal can fail without losing leadership | A leader that cannot reach the backend steps down after the lease |
| **B) Leadership lasts until a renewal fails** | Simplest | A stalled instance keeps believing it leads while the backend gave the lock to another |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- The time is read before the backend call because the backend starts its own clock after that instant; reading after would let the local belief outlive the lock. Renewal goes through `ILockHandle.ExtendAsync` (ownership-checked), never `IDistributedLockProvider.ExtendAsync(resource)`, which extends a lock it does not own on Redis (#2405).
- Option B is not excluded by a repository rule (the Kubernetes Leases semantics and the #717 plan are comparables, not rules); it is excluded only by the safety argument (a stalled instance keeps believing it leads), which is why the maintainer confirms A together with the defaults of the issue body (30 s lease, 10 s interval). A fails closed, as the gate rule of `AGENTS.md` section 3 asks of any gate.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Registration and what it requires</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaLeadershipCampaign(Action<LeadershipCampaignOptions>?)` registers the options with validation on start, `LeaderElectionProvider` as a singleton `ILeaderElectionProvider`, the hosted service and the health check; it throws `InvalidOperationException` naming the missing call when no `IDistributedLockProvider` is registered; a second call changes nothing** | Registration completeness; a missing lock provider fails at startup, not at the first election | The call order matters (lock provider first) |
| **B) Register lazily and fail on first use** | Any order | The failure shows up in production traffic, not at startup |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- A satisfies the registration-completeness rule (every dependency is checked at registration, proven by a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes`) and follows ADR-050's registration errors for a required module; B (lazy, failing on first use) is not forbidden by `AGENTS.md` section 3 or ADR-050 (whose text covers modules that require modules), so the choice is the maintainer's: A fails at startup, B tolerates any registration order. The provider is a singleton and depends only on singletons (`IDistributedLockProvider`, `TimeProvider`, `IOptions<>`), so no scoped service is injected into a singleton.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Health check and telemetry content</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The health check reports loop liveness (last completed iteration within three renewal intervals, by `TimeProvider`) and lists leading and following counts as data; metrics aggregate over all elections (no election name tag); the election name is never a log argument** | A follower is healthy (an orchestrator must not kill followers); no data-bearing names (ADR-054 D6.3) | "Am I the leader" is data, not a status |
| **B) Unhealthy when this instance is not the leader** | Literal reading of "Am I the leader?" | Every follower would be restarted or removed from rotation |

### Chosen Option: **A**

### Rationale

- Only A is workable with an orchestrator; election names are application text, so the metrics count transitions and elections led without naming them. Instruments on `Meter("Encina.DistributedLock")` (no `_total` suffix): `encina.leader_election.transitions` (counter; tags `encina.election.transition` = `acquired` | `lost` | `released`, `encina.lock.system`), `encina.leader_election.leadership.duration` (histogram, `s`), `encina.leader_election.leading` (observable gauge: elections led by this instance). Tenant tag (ADR-054 D4) is not applicable: elections are global (follow-up in the matrix).

**Class:** Trivial
**Settled by:** [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D3 and D6.3; `src/Encina.DistributedLock.Redis/Health/RedisDistributedLockHealthCheck.cs` (the `IEncinaHealthCheck` convention: `DefaultName`, tags)

</details>

---

## Implementation Phases

### Phase 1: Abstractions, Options and the Handle-Based Provider (PR 1)

> **Goal**: Acquire, hold, renew and release leadership without any background loop.

<details>
<summary><strong>Tasks</strong></summary>

1. **`LeaderElection/ILeaderElectionProvider.cs`** (new, `src/Encina.DistributedLock/`) - `TryAcquireLeadershipAsync(string electionName, TimeSpan leaseDuration, CancellationToken)` returning `Task<Either<EncinaError, Option<ILeadershipHandle>>>`; `AcquireLeadershipAsync(electionName, leaseDuration, ct)` returning `Task<Either<EncinaError, ILeadershipHandle>>` (waits until elected or cancelled; polls every `RenewalInterval`); `IsLeaderAsync(electionName, ct)` and `HasLeaderAsync(electionName, ct)` returning `Task<Either<EncinaError, bool>>`; and `bool IsHoldingLeadership(string electionName)` (no I/O, never throws), the synchronous read that #717's `ILeaderElection.IsLeader(name)` view calls under Dilemma 1 option A. **Source of truth of "this instance leads"**: `LeaderElectionProvider` keeps a `ConcurrentDictionary<string, LeadershipHandle>` of the live handles it created (added on a successful acquire, removed on loss, release or dispose); `IsHoldingLeadership` and `IsLeaderAsync` read it and answer `handle.IsStillLeader` (local expiry check), `HasLeaderAsync` adds `IsLockedAsync(resource)`. The hosted service reads leadership only through this provider API; its own state (`Following`, `Leading`, last completed iteration) feeds the health check.
2. **`LeaderElection/ILeadershipHandle.cs`** (new) - `IAsyncDisposable` with `ElectionName`, `LeaderId` (the lock id), `AcquiredAtUtc`, `ExpiresAtUtc` (the local lease expiry), `IsStillLeader`, `LeadershipLostToken`, `RenewLeadershipAsync(ct)` returning `Task<Either<EncinaError, bool>>`, and `event EventHandler<LeadershipLostEventArgs>? LeadershipLost`; `LeadershipLostEventArgs` (new, sealed: `ElectionName`, `Reason` enum `Expired` | `RenewalFailed` | `Released`).
3. **`LeaderElection/LeadershipCampaignOptions.cs`** (new) - `DefaultLeaseDuration` (30 s), `RenewalInterval` (10 s), `Elections` (names the hosted service campaigns for; empty by default), `AutoRenew` (true); `LeadershipCampaignOptionsValidator` (internal `IValidateOptions`: positive durations, `RenewalInterval * 2 <= DefaultLeaseDuration`, non-empty distinct names).
4. **`LeaderElection/LeaderElectionErrorCodes.cs`** (new, public static) - `leader_election.invalid_name` (`Validation`, ADR-062 R3), `leader_election.invalid_lease` (`Validation`), `leader_election.backend_failure` (`Internal`, R4). A lock-provider `Left` is propagated unchanged (its code and category are the lock's).
5. **`LeaderElection/LeaderElectionProvider.cs`** (new, public sealed; constructor `(IDistributedLockProvider, IOptions<LeadershipCampaignOptions>, TimeProvider? timeProvider = null)`) - the lock resource is `leader:{electionName}` (the provider adds its `KeyPrefix`); the `leader:` prefix is **reserved**: it shares the namespace of the application's lock names, so an application lock named `leader:billing` is the election lock of `billing`; the XML docs and the README section state it, and `AcquireAsync` of the application must not use it; acquire with `wait` zero; the handle holds the `ILockHandle`; `RenewLeadershipAsync` calls `ILockHandle.ExtendAsync(leaseDuration)` and recomputes the local expiry from the time read before the call; a `false` or `Left` ends leadership (token cancelled, event raised once); `DisposeAsync` releases the lock and ends leadership.
6. **Tests** - unit with the InMemory lock provider and `FakeTimeProvider` (elected, contended, renew moves the expiry, no renew ends leadership at the local expiry, loss cancels the token and raises the event once, handler exception does not escape, the registry behind `IsHoldingLeadership` (added on acquire, removed on loss, release and dispose, false at the local expiry), dispose releases, second instance elected afterwards, cancellation); guard (`LeaderElectionProviderGuardTests`: null lock provider, options, null or whitespace election name returns `Left` not exception); options validator tests; PublicAPI (`mechanical-fixer`); manifest rows; README section and changelog fragment (specialists).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #216: the handle-based leader election over IDistributedLockProvider.

CONTEXT:
- #2667 has already changed IDistributedLockProvider to the ADR-052 shapes (Either, Option for contention); read the
  current signatures. ILockHandle.ExtendAsync resets the expiry to now plus the extension.
- Fail closed: the leader belief ends at timeBeforeCall + lease even when no answer arrived.
- Time comes from TimeProvider; no DateTime.UtcNow.

TASK:
Create ILeaderElectionProvider, ILeadershipHandle, LeadershipLostEventArgs, LeadershipCampaignOptions and its validator,
LeaderElectionErrorCodes and LeaderElectionProvider, with the unit, guard and options tests of the Phase 1 tasks.

KEY RULES:
- Contention (another instance leads) is Right(None), not an error; faults are Left; cancellation propagates.
- Renew through ILockHandle.ExtendAsync only (ownership-checked), never IDistributedLockProvider.ExtendAsync(resource).
- Event handlers run inside try/catch; an exception is recorded by exception type and never escapes.
- Never put the election name or EncinaError.Message in a log argument.

REFERENCE FILES:
- src/Encina.DistributedLock/IDistributedLockProvider.cs, ILockHandle.cs
- src/Encina.DistributedLock.InMemory/InMemoryDistributedLockProvider.cs
- docs/plans/leader-election-implementation-plan-717.md (lease safety rule)
```

</details>

### Phase 2: Hosted Service, Observability, Health Check and Registration (PR 2)

> **Goal**: Automatic campaign, renewal and failover, with telemetry; the log ids come from the family range.

<details>
<summary><strong>Tasks</strong></summary>

1. **`LeaderElection/LeaderElectionHostedService.cs`** (new, internal sealed `BackgroundService`; constructor `(ILeaderElectionProvider, IOptions<LeadershipCampaignOptions>, TimeProvider, ILogger<...>)`) - for each configured election keeps a state (`Following`, `Leading`); each `RenewalInterval` tick: leading means renew, following means try to acquire; per-election and per-iteration `try/catch` (Design Choice 4); on stopping, disposes every handle (release) with a bounded wait; exposes a read-only snapshot for the health check.
2. **`Diagnostics/LeaderElectionLog.cs`** (new, `internal static partial`) - ids 30553 leadership acquired (Information), 30554 leadership lost (Warning, reason), 30555 renewal failed (Warning, error code or exception type), 30556 leadership released at shutdown (Information), 30557 election iteration failed (Error, `ForLogging()`), 30558 leadership event handler threw (Warning, exception type). Packed from 30553 with no gaps; XML docs name the range `DistributedLockPrimitives`.
3. **`Diagnostics/LeaderElectionMetrics.cs`** (new, internal) - the three instruments of Design Choice 7 on the shared `DistributedLockDiagnostics` meter.
4. **`Health/LeadershipCampaignHealthCheck.cs`** (new, public sealed `IEncinaHealthCheck`; `DefaultName = "encina-leadership-campaign"`, tags `["distributed-lock", "ready"]`) - loop liveness by `TimeProvider`.
5. **`ServiceCollectionExtensions.cs`** (modify, core) - `AddEncinaLeadershipCampaign(this IServiceCollection, Action<LeadershipCampaignOptions>? configure = null)` per Design Choice 6; DI test with `ValidateOnBuild` and `ValidateScopes`, with and without a lock provider, called twice.
6. **Tests** - unit with `FakeTimeProvider` and the InMemory lock provider: failover (leader disposed or expired, follower elected at its next tick), a thrown iteration is caught and the loop continues, shutdown releases, stopping token ends the loop without an error, handler exception, health liveness; guard; PublicAPI; manifest; `EventIdRanges` is not edited here (#215 owns the range).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #216: the leader election hosted service and its operations.

CONTEXT:
- AGENTS.md section 3: a PeriodicTimer loop catches per iteration; only the stopping token ends it; a failure is logged by
  error code or exception type (ForLogging()), counted, and the operation fails visibly (leadership marked lost).
- The EventId range DistributedLockPrimitives (30550-30564) was registered by #215 PR 2; use ids 30553-30558 only.
- ADR-054: Meter "Encina.DistributedLock", no _total suffix, no data-bearing names.

TASK:
Create the hosted service, LeaderElectionLog, the metrics, the health check and AddEncinaLeadershipCampaign with the tests of
the Phase 2 tasks.

KEY RULES:
- The hosted service and the provider are singletons; inject no scoped service.
- Never log the election name or EncinaError.Message.
- Registration throws InvalidOperationException when no IDistributedLockProvider is registered.

REFERENCE FILES:
- src/Encina.Caching/Sharding/Services/TopologyRefreshHostedService.cs (PeriodicTimer pattern)
- src/Encina.DistributedLock.Redis/Health/RedisDistributedLockHealthCheck.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 3: Provider Contract Scenarios and Integration (PR 3)

> **Goal**: The election is proven on every lock provider that exists, and the later providers add one subclass each.

<details>
<summary><strong>Tasks</strong></summary>

1. **`tests/Encina.ContractTests/DistributedLock/LeaderElectionContractTests.cs`** (new) - an abstract base: two `LeaderElectionProvider` instances over one backend; exactly one is elected; the loser sees `Right(None)`; when the leader disposes the other is elected; when the leader stops renewing the other is elected after the lease; subclasses for InMemory, Redis and SQL Server. **Prerequisite for SQL Server**: the scenario "the leader stops renewing, the follower is elected after the lease" cannot pass on SQL Server until [#2405](https://github.com/dlrivada/Encina/issues/2405) lands (`SqlServerDistributedLockProvider.cs:276-278`: the `sp_getapplock` session lock is held until released, so `IsLockedAsync` stays true and the follower is never elected). The SQL Server subclass therefore overrides that one scenario to be skipped through a documented `Skip` reason naming #2405 (not removed: when #2405 merges first, the skip is deleted in the same PR); the dispose-based failover scenario runs on SQL Server either way.
2. **`tests/Encina.ContractTests/Encina.ContractTests.csproj`** (modify) and **`tests/Encina.ContractTests/DistributedLock/LockBackendCollections.cs`** (new) - `Encina.ContractTests` has no reference to any `Encina.DistributedLock*` project and no Redis or SQL Server collection today (verified in its csproj on main): add four `ProjectReference` lines (`Encina.DistributedLock`, `.InMemory`, `.Redis`, `.SqlServer`) and the two `[CollectionDefinition]` classes over `RedisFixture` and `SqlServerFixture` from `Encina.TestInfrastructure` (the assembly declares its own collection, as the AmazonSQS contract tests do). Both are counted in PR 3.
3. **Integration tests** - Redis and SQL Server through the shared collections `Redis` and `ADO-SqlServer` of `Encina.IntegrationTests` (the existing Redis lock integration test uses a per-class container, which these new files must not copy): leadership held across several renewals, failover on process-like disposal, `HasLeaderAsync` true while a leader exists.
4. **Later providers** - the PostgreSQL, MySQL, Azure Blob and DynamoDB packages (#207 to #210) add one contract subclass each in their own pull requests; this plan does not wait for them.
5. **Load and benchmark** - justification files (a coordination call at lease granularity; no hot path).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #216: contract and integration tests of the leader election across lock providers.

CONTEXT:
- LeaderElectionProvider works on any IDistributedLockProvider; the contract proves it on every provider that exists.

TASK:
Write LeaderElectionContractTests as an abstract base with InMemory, Redis and SQL Server subclasses and the integration
tests, using the shared collection fixtures.

KEY RULES:
- Integration tests use shared [Collection] fixtures, never a per-class container; use FakeTimeProvider only where the
  backend does not evaluate expiry itself (Redis and SQL Server expire on the server: use short real leases).
- Contract tests instantiate real implementations.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/DistributedLock/
- tests/Encina.ContractTests/
```

</details>

---

## Research

### Relevant Standards and Specifications

| Standard | Relevance |
|----------|-----------|
| SPEC-000 REQ-027, DEC-009, AC-027 | Election runs on the seven lock providers |
| ADR-052 | Outcomes of the lock primitive the election is built on |
| ADR-050 | Registration errors for a missing dependency |
| ADR-054 D3, D4, D6.3 | Meter name, tenant tag, no data-bearing names |
| ADR-062 | Categories of `leader_election.*` codes |
| Kubernetes Leases, Hangfire single active server | Lease semantics, comparable products |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `IDistributedLockProvider`, `ILockHandle` | `src/Encina.DistributedLock/` | The election engine |
| `IEncinaHealthCheck` | `src/Encina.DistributedLock.Redis/Health/` (convention) | The liveness check |
| `PeriodicTimer` hosted service | `src/Encina.Caching/Sharding/Services/TopologyRefreshHostedService.cs` | The loop pattern |
| `EncinaErrors.Create` | `src/Encina/` | `Left` values |
| #717 plan | `docs/plans/leader-election-implementation-plan-717.md` | Lease safety rule, processor adoption |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Provider | Engine | Proven by |
|----------|--------|-----------|
| InMemory | `InMemoryDistributedLockProvider` | PR 1 unit tests, PR 3 contract |
| Redis | `RedisDistributedLockProvider` | PR 3 contract and integration |
| SQL Server | `SqlServerDistributedLockProvider` | PR 3 contract and integration |
| PostgreSQL, MySQL, Azure Blob, DynamoDB | planned packages (#207 to #210) | one contract subclass in each package's PR |

### Event ID Allocation

| Package | Range | Ids |
|---------|-------|-----|
| `Encina.DistributedLock` (`DistributedLockPrimitives`, registered by #215) | `30550-30564` | #216 uses 30553-30558 (acquired, lost, renewal failed, released at shutdown, iteration failed, handler threw); no id of this plan is below 10000 |

### Estimated File Count

| PR | Content | New | Modified | Tests, manifests, PublicAPI, docs | Total | Lines (est.) |
|----|---------|-----|----------|------------------------------------|-------|--------------|
| 1 | Abstractions, options, provider | 7 (2 interfaces, event args, options, validator, error codes, provider) | 0 | 7 (unit, guard, options tests, PublicAPI, manifest, README section, fragment) | 14 | 1,000 |
| 2 | Hosted service, log, metrics, health, DI | 4 (service, log, metrics, health check) | 3 (`ServiceCollectionExtensions.cs`, PublicAPI, manifest) | 4 (unit for the service, unit for health and metrics, guard, DI test) | 11 | 1,000 |
| 3 | Contract and integration | 6 (contract base with its three subclasses in one file, `LockBackendCollections.cs`, Redis and SQL Server integration files, 2 justification `.md`) | 1 (`Encina.ContractTests.csproj`: four `ProjectReference` lines) | 0 | 7 | 650 |

Re-summed: 14 + 11 + 7 = 32 files; 1,000 + 1,000 + 650 = 2,650 lines, header "about 2,600" rounded. Every PR is at most 15 files and 1,500 lines (PR 1: 14 files, the closest to the limit; its tests include the registry cases of `IsHoldingLeadership`); PR 1 and PR 2 build and pass alone (PR 2 needs the range of #215 PR 2 merged).

### Per-file coverage targets (numeric, per flag)

| File | Unit | Guard | Contract | Integration | Justification |
|------|------|-------|----------|-------------|---------------|
| `LeaderElectionProvider.cs` | 90 | 25 | 70 | 40 | Pure logic over a lock provider with a fake clock; guards cover constructor and method arguments; the contract suite covers failover on real providers |
| `LeaderElectionHostedService.cs` | 85 | 0 | 0 | 40 | Loop driven by `FakeTimeProvider`; contract tests target the provider, not the loop. Guard 0: the type is internal and `Encina.DistributedLock` has no `InternalsVisibleTo` for `Encina.GuardTests` (guard tests cannot reach it; its constructor arguments are covered by the unit tests) |
| `LeadershipCampaignOptions.cs`, `LeadershipCampaignOptionsValidator.cs` | 95 | 0 | 0 | 0 | Pure validation. Options is a property bag with no parameters; the validator is internal, so no guard test can reach it (same IVT reason) |
| `LeadershipCampaignHealthCheck.cs` | 85 | 20 | 0 | 0 | Public, constructor guards reachable; state reading otherwise, tested with `FakeTimeProvider` |
| `LeaderElectionMetrics.cs` | 85 | 0 | 0 | 0 | Internal (no IVT for the guard project); instruments tested with a `MeterListener` in the unit flag |
| `ServiceCollectionExtensions.cs` (the added `AddEncinaLeadershipCampaign`) | 90 | 20 | 0 | 0 | Public extension: null services, missing lock provider, called twice |
| `LeaderElectionLog.cs`, `LeaderElectionErrorCodes.cs`, the two interfaces, `LeadershipLostEventArgs.cs` | 0 | 0 | 0 | 0 | Source-generated log methods, constants, interfaces and a data holder: no logic to measure; covered through the provider |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is pre-1.0 (.NET 10, C# 14, nullable enabled). Rules are in AGENTS.md. Issue #216 adds a leader election API to
Encina.DistributedLock, built on IDistributedLockProvider, working on all seven lock providers.

IMPLEMENTATION OVERVIEW:
1. Phase 1: ILeaderElectionProvider, ILeadershipHandle, options and validator, error codes, LeaderElectionProvider.
2. Phase 2: LeaderElectionHostedService (PeriodicTimer, per-iteration catch), log ids 30553-30558, metrics, health check,
   AddEncinaLeadershipCampaign (fails at registration without a lock provider).
3. Phase 3: contract and integration tests across providers.

KEY PATTERNS:
- ADR-052 outcomes; leadership belief ends at timeBeforeCall + lease (fail closed).
- Renew through ILockHandle.ExtendAsync only; never log election names or EncinaError.Message.
- Singletons only; TimeProvider for every time read; ValidateOnBuild and ValidateScopes DI test.
- Dilemmas 1, 2, 3, 5 and 6 are pending the maintainer: implement the recommended options only after he answers.

REFERENCE FILES:
- docs/plans/leader-election-provider-implementation-plan-216.md, docs/plans/leader-election-implementation-plan-717.md
- src/Encina.DistributedLock/, docs/architecture/adr/052-distributed-lock-failure-contract.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Leader state must be real-time; a cached answer would outlive the lease |
| 2 | OpenTelemetry | ✅ Include | Phase 2: transitions, leadership duration, elections led; no election name, no tenant tag (elections are global) |
| 3 | Structured Logging | ✅ Include | Phase 2: `LeaderElectionLog` 30553-30558, error code or exception type only |
| 4 | Health Checks | ✅ Include | Phase 2: loop liveness; a follower is healthy (Design Choice 7) |
| 5 | Validation | ✅ Include | Options validator and `Left` for an invalid election name or lease (Phase 1) |
| 6 | Resilience | ✅ Include | Lease renewal and failover are the feature; a failed renewal ends leadership visibly and the loop re-campaigns; no Polly policy around the lock calls |
| 7 | Distributed Locks | ✅ Include | Built on the lock primitive; every provider proven by the contract suite |
| 8 | Transactions | ❌ N/A | Coordination, not data mutation |
| 9 | Idempotency | ❌ N/A | Election is coordination; a leader's work must still be idempotent at least once (documented, with fencing in #218) |
| 10 | Multi-Tenancy | ⏭️ Defer | Per-tenant elections reuse the #741 key derivation; one follow-up for the family, see `artifacts/issues/lock-primitives-tenant-scoping.md` |
| 11 | Module Isolation | ❌ N/A | Modules are separated with `KeyPrefix` and election names; no new code |
| 12 | Audit Trail | ❌ N/A | Leadership changes are infrastructure events with no personal or regulated data; they are logged and metered (rows 2 and 3) |

---

## Prerequisites & Dependencies

| Item | State | Needed for |
|------|-------|------------|
| #2667 ADR-052 implementation | open | All PRs |
| #215 PR 2 (registers `DistributedLockPrimitives`) | planned | PR 2 and PR 3 of this plan |
| #724 / #741 `DistributedLockDiagnostics` | plans on main | PR 2 (reuse or create the holder) |
| #2405 SQL Server lock ignores its expiry (open bug) | open | The SQL Server subclass's "leader stops renewing, follower elected after the lease" scenario in PR 3 (skipped with the reason until it lands); not needed for any other scenario or PR |
| #717 plan decision (Design Choice 1) | Dilemma, recommended A | Which plan owns the renewal loop (type names do not depend on it) |
| #207 to #210 | plans on main | Contract subclasses of the later providers |

---

## Next Steps

1. After #2667 and PR 2 of #215 merge, open PR 1 and PR 2 of this plan in order, then PR 3.
2. The maintainer answers Dilemmas 1, 2, 3, 5 and 6 when work on the issue starts; PR 1 is written for the recommended answers and changes in one file per Dilemma if he chooses otherwise.
3. #221 (`[RequiresLeadership]`) and #717 (processors) build on `ILeaderElectionProvider` after PR 2.
