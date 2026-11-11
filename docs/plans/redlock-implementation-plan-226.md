# Implementation Plan: `Encina.DistributedLock.Redis` — RedLock quorum locking across several Redis instances

> **Issue**: [#226](https://github.com/dlrivada/Encina/issues/226)
> **Type**: Feature
> **Complexity**: Medium (one provider package, 3 pull requests)
> **Estimated Scope**: ~700-900 lines of production code + ~1,100-1,400 lines of tests
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027, DEC-003, DEC-009, AC-027
> **Decision**: [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md) (lock failure contract)
> **Depends on**: [#2667](https://github.com/dlrivada/Encina/issues/2667) (implements ADR-052; open, not a pending maintainer decision)
> **Related**: [#218](https://github.com/dlrivada/Encina/issues/218) (fencing tokens), [#223](https://github.com/dlrivada/Encina/issues/223) (auto-extend), [#222](https://github.com/dlrivada/Encina/issues/222) and [#724](https://github.com/dlrivada/Encina/issues/724) (lock observability), [#741](https://github.com/dlrivada/Encina/issues/741) (tenant-scoped locks, same family), [#755](https://github.com/dlrivada/Encina/issues/755) (health checks of the other lock providers), [#2405](https://github.com/dlrivada/Encina/issues/2405), [#2654](https://github.com/dlrivada/Encina/issues/2654), [#2653](https://github.com/dlrivada/Encina/issues/2653)

---

## Summary

Today `RedisDistributedLockProvider` (`src/Encina.DistributedLock.Redis/RedisDistributedLockProvider.cs`) locks against **one** `IConnectionMultiplexer` with `SET key value PX expiry NX`, releases and extends through Lua scripts that check ownership, and has a health check. Its XML remarks and `AGENTS.md` section 5 call the Redis provider "Redlock", but a single instance is a single point of failure: when that Redis is down, every lock fails; when it fails over to a replica that did not receive the key, two holders can own the same lock. This plan adds the **RedLock quorum algorithm** as an opt-in mode of the same package:

1. **Quorum acquire.** The same random lock value is written with `SET NX PX` to N independent instances. The lock is held only when a majority (N/2 + 1) accepted it **and** the remaining validity time (TTL minus elapsed time minus a clock-drift allowance) is still positive. Otherwise every instance is unlocked and the attempt counts as contention (`Right(None)`), retried until the `wait` deadline with a randomised delay.
2. **Quorum extend and release.** `ILockHandle.ExtendAsync` runs the ownership-checked Lua `PEXPIRE` on every instance and succeeds only on a majority; `DisposeAsync` runs the ownership-checked Lua `DEL` on every instance, best effort, logged by exception type only.
3. **One code path.** A single instance is the degenerate quorum (N = 1, majority = 1, no drift allowance), so the single-endpoint behavior stays the default and its semantics do not change.
4. **ADR-052 contract.** All outcomes use the ADR-052 shapes: quorum reached is `Right(Some(handle))`; held elsewhere, majority not obtained or validity exhausted is `Right(None)`; faults that make a quorum impossible are `Left` with a `LockErrorCodes` code; cancellation propagates.
5. **Operations.** Per-instance outcome logging (exception type only), a quorum counter and span, and a health check that reports each endpoint (healthy with all, degraded with a quorum, unhealthy without).

**Standards**: the Redis distributed locks pattern ("Redlock", https://redis.io/docs/latest/develop/clients/patterns/distributed-locks/); the critique that a lease needs fencing (Kleppmann, "How to do distributed locking", 2016), which is why fencing tokens stay in #218; ADR-052; SPEC-000 REQ-027 / AC-027.

**Estimated scope**: 4 new production files, 5 modified, 1 new test fixture, about 10 test files, 3 documentation files; split into 3 pull requests (see Implementation Phases).

**Affected packages**: `Encina.DistributedLock.Redis` (all production changes); `Encina.TestInfrastructure` (a three-container Redis fixture); `Encina.NBomber` (a multi-instance variant of the existing Redis lock scenario factory). `Encina.DistributedLock` (the base options and the interface) is not changed by this plan.

**Provider category**: Distributed locks (`AGENTS.md` section 5; SPEC-000 DEC-009 fixes seven providers: InMemory, Redis, SqlServer, PostgreSQL, MySQL, Azure Blob, DynamoDB). This plan changes the Redis provider only; the quorum is Redis specific. The other six are not applicable (the provider matrix is in Research). Valkey, Dragonfly, Garnet and KeyDB speak the Redis protocol and can be endpoints, but a mixed-engine quorum is not tested or promised.

**What the code does today** (`origin/main` at `7a0049c07`):

- `RedisDistributedLockProvider` has one `_connection` field and a constructor `(IConnectionMultiplexer, IOptions<RedisLockOptions>, ILogger<>, TimeProvider?)`. `TryAcquireAsync` returns `Task<IAsyncDisposable?>`, `AcquireAsync` returns `Task<IAsyncDisposable>`, `IsLockedAsync` and `ExtendAsync` return `Task<bool>` (the pre-ADR-052 shapes, #2667 changes them). Time already comes from the injected `TimeProvider`, and retry delays already use `Task.Delay(..., _timeProvider, ct)`.
- The Lua scripts `ReleaseLockScript` and `ExtendLockScript` live inside the private `LockHandle` class. EventIds 3750-3754 are in use inside the registered `DistributedLockRedis` range 3750-3799 (`src/Encina/Diagnostics/EventIdRanges.cs`), and `EncinaEventIdAllocationTests` already maps the assembly to that range.
- `RedisLockOptions : DistributedLockOptions` has only `Database`. `ServiceCollectionExtensions` has four overloads (connection string or `IConnectionMultiplexer`, with or without `Action<RedisLockOptions>`) and registers the health check when `ProviderHealthCheck.Enabled`.
- The manifest `.github/coverage-manifest/Encina.DistributedLock.Redis.json` gives `RedisDistributedLockProvider.cs` the targets unit 95, guard 14 and integration 70 with justifications.
- The issue's original sample (`Encina.Redis.DistributedLock`, `AddEncinaRedLock`, `@lock.IsAcquired`) does not exist in the code and does not follow the `Encina.DistributedLock.{Backend}` naming; this plan uses the real names.

---

## Design Choices

<details>
<summary><strong>1. Quorum implementation — own implementation over StackExchange.Redis, or wrap the RedLock.net library</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Own implementation** in `Encina.DistributedLock.Redis` over a list of `IConnectionMultiplexer` | No new dependency; full control of the ADR-052 mapping, `CancellationToken`, `TimeProvider` and the log rules of `AGENTS.md` section 3; the scripts and key layout stay shared with the single-instance path | Encina owns a subtle distributed algorithm and its tests (partial acquisition, clock drift, release of every instance after failure) |
| **B) Wrap RedLock.net** (https://github.com/samcook/RedLock.net) | Battle tested; less code to write | Third-party dependency in a 1.0 package; its API and exceptions must be adapted to `Either`; `TimeProvider` injection and cancellation behavior must be verified; licence and maintenance risk; it brings its own multiplexer handling |

### Chosen Option: **A — own implementation** (recommended, pending the maintainer)

### Rationale

- The contract Encina needs (ADR-052 outcomes, `TimeProvider`, no exception message in logs, `ILockHandle` returned directly) is the part a wrapper would have to bend, so the adapter would be about as large as the algorithm (about 150 lines of quorum logic).
- Option B wins if time to ship matters more than control and the library passes the lock contract tests with a fake clock; the maintainer can pick it when work on the issue starts.
- The algorithm is small, fully specified by the Redis documentation, and testable with a fake `IDatabase` and `FakeTimeProvider`, plus three Redis containers for the chaos cases.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Package placement — inside the existing package, opt-in through registration</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extend `Encina.DistributedLock.Redis`** | One Redis lock provider as `AGENTS.md` section 5 lists; no second `IDistributedLockProvider` to keep in sync with ADR-052; single-instance users pay nothing | The package grows by about 4 files |
| **B) New package `Encina.DistributedLock.Redis.RedLock`** | Isolates the algorithm | A second provider of the same backend, which is exactly the duplication #2653 is removing; two registrations to choose between |

### Chosen Option: **A — extend `Encina.DistributedLock.Redis`**

### Rationale

- The category table lists "Redis (Redlock)" as the Redis provider, and #2653 treats a second lock provider for one backend as debt.
- Quorum mode is chosen only by registering more than one endpoint; with one endpoint nothing new runs.

**Class:** Settled
**Settled by:** `AGENTS.md` section 5 (Redis (Redlock) is the Redis lock provider), `AGENTS.md` section 1 (pay-for-what-you-use), issue #2653

</details>

<details>
<summary><strong>3. One code path — a single instance is a quorum of one</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One path:** the provider always talks to `RedisLockConnections` (1 to N multiplexers), majority = N/2 + 1, drift allowance only when N > 1 | One set of scripts, tests and ADR-052 mapping; the single-instance case is exercised by the quorum tests | Existing unit tests that build the provider from one `IConnectionMultiplexer` must be adapted |
| **B) Two paths:** keep the current single-instance code untouched and add a separate coordinator for N >= 3 | Zero change for the single-instance path | Two implementations of acquire, extend and release to keep in step with ADR-052, #218 and #223; the more used path is the less tested one |

### Chosen Option: **A — one path**

### Rationale

- Pre-1.0 there is no existing user and no compatibility cost (`AGENTS.md` section 1), so the only cost of A is updating tests.
- With N = 1 no drift allowance is subtracted, so `ExpiresAtUtc` of a single-instance lock is unchanged.

**Class:** Settled
**Settled by:** `AGENTS.md` section 1 (pre-1.0, best solution first, no compromise for compatibility; no existing user)

</details>

<details>
<summary><strong>4. Configuration model — endpoints as registration overloads, tuning as options</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Overloads take the endpoints** (`IReadOnlyList<string>` connection strings, or `IReadOnlyList<IConnectionMultiplexer>`), mirroring the four existing overloads; `RedisLockOptions` gains only `ClockDriftFactor` and `InstanceTimeout` | Connection strings (which can hold passwords) never enter an options object that could be logged or serialized; same style as today | Endpoints cannot be bound from `IConfiguration` into the options type |
| **B) `RedisLockOptions.Endpoints` list** (the shape of the issue's sample) | Bindable from configuration | The property holds secrets, so it needs `[JsonIgnore]` and a `ToString()` override (`AGENTS.md` section 3); two places could define the connection (overload and options) |

### Chosen Option: **A — endpoints on the overloads**

### Rationale

- The rule of `AGENTS.md` section 3 (a connection string or password property is `[JsonIgnore]` and hidden from `ToString`) is avoided by design, not just satisfied.
- New members: `ClockDriftFactor` (default 0.01, the factor of the Redis documentation), `InstanceTimeout` (default 250 ms, small against the 30 s default expiry as the documentation asks), both validated at startup by an `IValidateOptions<RedisLockOptions>` (drift in [0, 0.5], timeout positive and below `DefaultExpiry`).
- The registration validates the endpoint count: one endpoint is the single-instance mode; two are rejected (no fault tolerance, quorum still needs both); three or more are accepted; an even count logs one warning.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (options with a connection string or password), existing `ServiceCollectionExtensions` overload pattern in `src/Encina.DistributedLock.Redis/ServiceCollectionExtensions.cs`

</details>

<details>
<summary><strong>5. Outcome mapping — ADR-052 shapes over the quorum</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) ADR-052 mapping:** quorum reached `Right(Some(handle))`; held elsewhere, majority missed or validity exhausted `Right(None)`; a fault that makes a quorum impossible (a majority of instances faulted) `Left(LockErrorCodes ...)`; cancellation propagates | Same contract for all providers; contention is not logged or counted as a failure | Needs a per-attempt classification of each instance (granted, refused, faulted) |
| **B) Any instance fault is `Left`** | Simple | Contradicts the purpose of a quorum: one dead minority instance would fail every acquisition |

### Chosen Option: **A — ADR-052 mapping**

### Rationale

- A minority of faulted instances is expected operation and is logged per instance; only when the faulted instances alone prevent a majority (so the outcome cannot be told from contention) is the result `Left`.
- Logs record the exception type and `ForLogging()`, never the message (`AGENTS.md` section 3).

**Class:** Settled
**Settled by:** ADR-052, `AGENTS.md` section 3 (errors are not swallowed; `EncinaError.Message` never reaches logs)

</details>

<details>
<summary><strong>6. Time source and retry delay</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TimeProvider.GetTimestamp()` and `GetElapsedTime()` for the elapsed time, `Task.Delay(..., _timeProvider, ct)` for waits, randomised delay from `RandomNumberGenerator.GetInt32`** | Deterministic with `FakeTimeProvider`; matches the retry loop already in the provider (#531); a cryptographic source avoids the CA5394 warning | Slightly more code than `Stopwatch` |
| **B) `Stopwatch` and `Random.Shared`** | Shortest | Production code that cannot be driven by a fake clock and trips CA5394 |

### Chosen Option: **A — `TimeProvider` and `RandomNumberGenerator`**

### Rationale

- `AGENTS.md` section 3 requires time from `TimeProvider`; the Redis documentation asks for a random delay on retry so competing clients desynchronize.

**Class:** Trivial
**Settled by:** `AGENTS.md` section 3 (time comes from `TimeProvider`), `AGENTS.md` section 8 (zero warnings, CA rules)

</details>

<details>
<summary><strong>7. Extension across the quorum, fencing and auto-extend</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ILockHandle.ExtendAsync` extends on every instance with the ownership-checked Lua script and succeeds on a majority; fencing tokens (#218) and the extension loop (#223) stay out** | Keeps the lock correct under the quorum; each deferred feature has its own issue and design | A user needing fencing now has none |
| **B) Add fencing tokens here** | Closes the known weakness of leases | RedLock does not produce monotonic tokens; it needs a counter design that touches `ILockHandle` and every provider, which is #218's scope |

### Chosen Option: **A — extend only; fencing and auto-extend deferred**

### Rationale

- The provider-level `ExtendAsync(resource)` that extends a lock it does not own is the bug #2405; this plan does not touch its ownership semantics beyond giving it the ADR-052 `Either` shape through #2667.

**Class:** Settled
**Settled by:** issues #218 and #223 (own scope), issue #2405, ADR-052

</details>

<details>
<summary><strong>8. Health check — per endpoint, with a degraded state</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One result: Healthy with every endpoint answering, Degraded with a quorum, Unhealthy without; the description names endpoint indexes and exception types only** | Operators see a lost minority before the lock fails; connection strings never printed | A check does N round trips |
| **B) Keep the single-endpoint probe on the first multiplexer** | No change | Hides a dead majority behind a healthy first node |

### Chosen Option: **A — per-endpoint check with Degraded**

### Rationale

- `HealthCheckResult.Degraded` already exists in `Encina.Messaging.Health`; the health-check function of ADR-018 applies to the checkable dependency (Redis).

**Class:** Trivial
**Settled by:** `AGENTS.md` section 6 (function 4, health checks), `AGENTS.md` section 3 (no message or secret in health-check results)

</details>

<details>
<summary><strong>9. Sequencing with #2667</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Implement after #2667 merges, against the ADR-052 signatures** | Written once; the contract tests of #2667 exist to reuse | Starts later |
| **B) Implement now against the current `IAsyncDisposable?` signatures and port later** | Starts earlier | Writes code that #2667 immediately rewrites; pre-1.0 there is no compatibility reason to |

### Chosen Option: **A — after #2667**

### Rationale

- The brief of this plan's PRs names #2667 as a prerequisite; if #2667 slips, Phase 2 (pure quorum math and the coordinator, which return internal result types) can start first because it does not touch the public signatures.

**Class:** Settled
**Settled by:** ADR-052 (decided contract), issue #2667 (implementation), `AGENTS.md` section 1 (no compatibility layers)

</details>

---

## Implementation Phases

The work is split into three pull requests (at most 15 files and 1500 changed lines each, `AGENTS.md` section 11): **PR 1** Phases 1 to 3 (options, coordinator, provider on the ADR-052 contract, with unit and guard tests); **PR 2** Phases 4 to 6 (registration, health check, cross-cutting, observability); **PR 3** Phases 7 and 8 (property, contract and integration tests, the NBomber variant, documentation).

### Phase 1: Options, Validation and the Connection Holder

> **Goal**: The configuration surface and the list of multiplexers, nothing else.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.DistributedLock.Redis/`

1. **`RedisLockOptions.cs`** (modify) — add `double ClockDriftFactor { get; set; } = 0.01` and `TimeSpan InstanceTimeout { get; set; } = TimeSpan.FromMilliseconds(250)`, with XML docs naming the Redis documentation formula
2. **`RedisLockOptionsValidator.cs`** (new, public sealed) — `IValidateOptions<RedisLockOptions>`; rejects drift outside [0, 0.5], non-positive `InstanceTimeout`, `InstanceTimeout >= DefaultExpiry`, `Database < 0`
3. **`RedisLockConnections.cs`** (new, public sealed) — `IReadOnlyList<IConnectionMultiplexer> Multiplexers`; constructor guards a non-empty list; `int Quorum => Multiplexers.Count / 2 + 1`; `IAsyncDisposable` disposing only the multiplexers the registration created (an `ownsMultiplexers` flag)
4. **`PublicAPI.Unshipped.txt`** — the new public members

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #226: the configuration surface of RedLock quorum locking.

CONTEXT:
- src/Encina.DistributedLock.Redis holds one provider over one IConnectionMultiplexer. RedLock needs a list of
  multiplexers (1 to N) and two tuning options; endpoints are passed to the registration overloads, not stored in
  options (design choice 4).
- Quorum = N/2 + 1. A single instance is a quorum of one (design choice 3).

TASK:
Add ClockDriftFactor and InstanceTimeout to RedisLockOptions, a public sealed RedisLockOptionsValidator
(IValidateOptions<RedisLockOptions>), and a public sealed RedisLockConnections holder with Quorum and an
ownership flag for disposal. Track the public symbols in PublicAPI.Unshipped.txt (through the mechanical-fixer
agent).

KEY RULES:
- ArgumentNullException guards on every public constructor and method parameter.
- XML documentation on every public member.
- No connection string or password in any options property; nothing is logged here.
- Zero warnings (CA and RS rules).

REFERENCE FILES:
- src/Encina.DistributedLock.Redis/RedisLockOptions.cs
- src/Encina.DistributedLock/DistributedLockOptions.cs
- docs/architecture/adr/052-distributed-lock-failure-contract.md
```

</details>

### Phase 2: The Quorum Coordinator

> **Goal**: The algorithm as an internal, unit-testable class that knows nothing about `Either`.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.DistributedLock.Redis/`

1. **`RedLockCoordinator.cs`** (new, internal sealed) — constructor `(RedisLockConnections, RedisLockOptions, TimeProvider, ILogger)`; methods:
   - `Task<QuorumAttempt> TryAcquireOnceAsync(string key, string value, TimeSpan expiry, CancellationToken ct)` — starts a timestamp, sends `StringSetAsync(key, value, expiry, When.NotExists)` to every instance concurrently, each wrapped in `WaitAsync(InstanceTimeout)` and `WaitAsync(ct)` (StackExchange.Redis calls take no token, which is the #2654 finding), counts granted / refused / faulted, computes `validity = expiry - elapsed - drift` where `drift = N > 1 ? expiry * ClockDriftFactor + 2 ms : 0`; when granted >= quorum and validity > 0 returns `Acquired(validity)`, otherwise unlocks every instance (not only the granted ones) and returns `Contended` or `Faulted` (granted + refused cannot reach quorum and the faulted count made the difference)
   - `Task<bool> ExtendAsync(string key, string value, TimeSpan extension, CancellationToken ct)` — the ownership-checked `PEXPIRE` script on every instance, true on a majority
   - `Task ReleaseAsync(string key, string value)` — the ownership-checked `DEL` script on every instance, never throws, logs each failure by exception type
   - `QuorumAttempt` — internal record struct (`Outcome`, `Validity`, `GrantedCount`, `FaultedCount`)
2. **`RedLockScripts.cs`** (new, internal static) — the two Lua scripts moved out of `LockHandle` unchanged
3. **`RedLockLogMessages.cs`** — see Phase 6

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #226: the RedLock coordinator.

CONTEXT:
- Algorithm (Redis distributed locks documentation): record a start timestamp; SET NX PX with the same random value
  on every instance, each call with a short timeout; validity = TTL - elapsed - drift; the lock is held only when
  a majority granted it and validity is positive; otherwise unlock every instance. Extend and release use the
  ownership-checked Lua scripts already in RedisDistributedLockProvider.LockHandle.
- A minority of faulted instances is normal operation; it must not fail an acquisition.

TASK:
Create internal RedLockCoordinator, QuorumAttempt and RedLockScripts as described in the Phase 2 tasks. Do not
change the public signatures of the provider in this phase.

KEY RULES:
- Time only from the injected TimeProvider (GetTimestamp / GetElapsedTime); never DateTime.UtcNow.
- StackExchange.Redis calls take no CancellationToken: use Task.WaitAsync(timeout) and Task.WaitAsync(ct) so
  cancellation propagates as OperationCanceledException.
- A background release never throws; log the exception type and ForLogging(), never the message.
- Methods stay small: CRAP <= 10 on every changed method (split classification from I/O).

REFERENCE FILES:
- src/Encina.DistributedLock.Redis/RedisDistributedLockProvider.cs
- docs/architecture/adr/052-distributed-lock-failure-contract.md
- tests/Encina.UnitTests/DistributedLock/Redis/RedisDistributedLockProviderTests.cs
```

</details>

### Phase 3: The Provider on the ADR-052 Contract

> **Goal**: `RedisDistributedLockProvider` runs on the coordinator and returns the ADR-052 shapes.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.DistributedLock.Redis/RedisDistributedLockProvider.cs` (modify; signatures as fixed by #2667)

1. Constructor takes `RedisLockConnections` instead of one `IConnectionMultiplexer` (`ArgumentNullException` guards kept)
2. `TryAcquireAsync` — loop: `TryAcquireOnceAsync`; `Acquired` returns `Right(Some(handle))` with `ExpiresAtUtc = acquiredAt + validity`; `Contended` waits `retry` plus a random jitter (cut to the time left before `wait`, at least 1 ms, as `NextDelay` does today) and repeats until the deadline, then `Right(None)`; `Faulted` returns `Left` with the `LockErrorCodes` code #2667 defines for an unavailable backend
3. `AcquireAsync` — the same loop without a deadline, as today, `Right(handle)`; `Faulted` returns `Left`
4. `IsLockedAsync` — true when a majority of instances hold the key; a majority of faults is `Left`
5. `LockHandle.ExtendAsync` — calls `ExtendAsync` of the coordinator; `ExpiresAtUtc` updates only on success
6. `LockHandle.DisposeAsync` — calls `ReleaseAsync` of the coordinator
7. Remove the stale remark "consider implementing the Redlock algorithm" and describe the quorum mode honestly; update `PublicAPI.Unshipped.txt`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #226: the Redis provider on the quorum coordinator and the ADR-052 contract.

CONTEXT:
- Issue #2667 has already changed IDistributedLockProvider and ILockHandle to the ADR-052 shapes (Either with
  Option for contention) and added LockErrorCodes; read their current signatures before editing.
- Contention and timeout are Right(None), a fault is Left, cancellation propagates, nothing swallows an error.

TASK:
Rewire RedisDistributedLockProvider as listed in the Phase 3 tasks, using RedLockCoordinator. Update the existing
unit and guard tests of the provider to the new constructor and signatures and add the quorum scenarios: majority
granted, minority granted (unlock all), one instance faulted, majority faulted (Left), validity exhausted,
cancellation, zero wait.

KEY RULES:
- Time from TimeProvider; retry delays on Task.Delay(..., _timeProvider, ct).
- Never put EncinaError.Message in logs or tags.
- Per-file coverage targets in .github/coverage-manifest/Encina.DistributedLock.Redis.json must not go down.

REFERENCE FILES:
- src/Encina.DistributedLock.Redis/RedisDistributedLockProvider.cs
- src/Encina.DistributedLock/IDistributedLockProvider.cs
- src/Encina.DistributedLock/ILockHandle.cs
- tests/Encina.UnitTests/DistributedLock/Redis/RedisDistributedLockProviderTests.cs
- tests/Encina.GuardTests/Infrastructure/DistributedLock/RedisDistributedLockProviderGuardTests.cs
```

</details>

### Phase 4: Configuration, DI and Health Check

> **Goal**: Registering several endpoints works, with the registration completeness proof.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.DistributedLock.Redis/ServiceCollectionExtensions.cs` (modify)

1. Two new overloads, each with and without `Action<RedisLockOptions>`: `AddEncinaDistributedLockRedis(this IServiceCollection, IReadOnlyList<string> connectionStrings)` and `AddEncinaDistributedLockRedis(this IServiceCollection, IReadOnlyList<IConnectionMultiplexer> connectionMultiplexers)`; guards for null, empty and blank entries; the endpoint-count rule of design choice 4 (a count of 2 throws `ArgumentException` at registration)
2. The core method registers `RedisLockConnections` (a singleton owning the multiplexers it created from connection strings, borrowing supplied ones), `IValidateOptions<RedisLockOptions>` (`RedisLockOptionsValidator`), `ValidateOnStart` on the options, the provider and the health check, all with `TryAdd*`, so a database or other registration keeps winning
3. The existing four overloads delegate with a single-element list

#### `src/Encina.DistributedLock.Redis/Health/RedisDistributedLockHealthCheck.cs` (modify)

4. Constructor takes `RedisLockConnections`; probes every instance with the existing `SET NX` + `DEL` test key, concurrently, with `InstanceTimeout`; Healthy / Degraded (quorum reachable) / Unhealthy; the description holds endpoint indexes and exception types only

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #226: DI registration and the health check for multi-endpoint locking.

CONTEXT:
- ServiceCollectionExtensions has four overloads and a private core method; all register with TryAdd*.
- AGENTS.md section 3 (registration completeness): every option type and dependency the provider resolves must be
  registered, proven by a DI test that builds the provider with ValidateOnBuild and ValidateScopes.

TASK:
Add the two list overloads (with and without configure), register RedisLockConnections and the options validator,
and update the health check as listed in the Phase 4 tasks. Add the DI test (one endpoint, three endpoints, a
caller-supplied IDistributedLockProvider registered before and after) and extend the health check tests (all up,
one down, two down).

KEY RULES:
- TryAdd* only; never override an application's own registration.
- Connection strings never appear in exception messages, logs or health check descriptions.
- Health check results carry no exception message, only the type name.

REFERENCE FILES:
- src/Encina.DistributedLock.Redis/ServiceCollectionExtensions.cs
- src/Encina.DistributedLock.Redis/Health/RedisDistributedLockHealthCheck.cs
- tests/Encina.UnitTests/DistributedLock/Redis/ServiceCollectionExtensionsTests.cs
- tests/Encina.UnitTests/DistributedLock/Redis/RedisDistributedLockHealthCheckTests.cs
```

</details>

### Phase 5: Cross-Cutting Integration

> **Goal**: Each of the 12 functions is integrated or deferred as the matrix says.

<details>
<summary><strong>Tasks</strong></summary>

1. **Resilience (6)** — per-instance `InstanceTimeout`, a faulted minority tolerated; no Polly policy around the quorum (a retry of an acquisition is the provider's own `wait`/`retry` loop)
2. **Validation (5)** — startup validation of the options and of the endpoint count (Phase 1 and 4)
3. **Multi-tenancy (10)** — the coordinator receives the final lock key, so tenant-scoped keys composed above it by #741 need no change here; recorded in the #741 plan (which owns tenant key composition)
4. **Distributed locks (7)**, **transactions (8)**, **idempotency (9)**, **caching (1)**, **module isolation (11)**, **audit trail (12)** — not applicable, with the reasons in the matrix

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #226: cross-cutting integration of the RedLock mode.

CONTEXT:
- The matrix of this plan marks resilience and validation as included, multi-tenancy as owned by #741, the rest as
  not applicable.

TASK:
Verify Phases 1 to 4 already cover resilience (per-instance timeout) and validation (options and endpoint count),
and add the missing tests if any. Do not add tenant or module logic to the coordinator.

KEY RULES:
- The coordinator and provider stay tenant-agnostic; key composition belongs to #741.
- No Polly dependency is added to the package.

REFERENCE FILES:
- docs/plans/redlock-implementation-plan-226.md (Cross-Cutting Integration Matrix)
- src/Encina.DistributedLock.Redis/RedisDistributedLockProvider.cs
```

</details>

### Phase 6: Observability — Diagnostics, Metrics and Logging

> **Goal**: Quorum outcomes are visible without counting contention as a failure.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.DistributedLock.Redis/Diagnostics/`

1. **`RedLockLogMessages.cs`** (new, internal static partial) — `[LoggerMessage]` methods with EventIds 3755-3762 (table in Research): quorum acquired (Debug), quorum not reached (Debug), instance faulted during acquire (Warning, exception type), instance faulted during release (Warning), validity exhausted (Debug), extend lost its majority (Warning), even endpoint count (Warning), faulted majority (Error)
2. **`RedLockDiagnostics.cs`** (new, internal static) — `ActivitySource` named `Encina.DistributedLock.Redis` (version `1.0`, the convention of `Encina.Sharding`); `Meter` with the counter `encina.distributed_lock.redis.quorum` (tag `outcome`: acquired, contended, faulted) and the histogram `encina.distributed_lock.redis.instance_failures` (tag `operation`); the span `encina.distributed_lock.redis.acquire` with `endpoints.count` and `quorum.size`
3. If #724 has merged a shared lock `Meter` and `ActivitySource` by then, register these instruments on them instead of creating a second set

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #226: logging and telemetry of the quorum.

CONTEXT:
- EventIds 3750-3754 are used by RedisDistributedLockProvider inside the registered DistributedLockRedis range
  3750-3799. This plan owns 3755-3769; the rest of the range stays spare for the Redis assembly (the tenant decorator of #741 and the election of #717 live in other assemblies and take their own range). The range is already registered in
  src/Encina/Diagnostics/EventIdRanges.cs and mapped in EncinaEventIdAllocationTests.
- Contention (Right(None)) is not a failure: log it at Debug and count it with outcome=contended.

TASK:
Create RedLockLogMessages and RedLockDiagnostics as listed, wire them into the coordinator and provider, and add
the PublicAPI lines only if a symbol is public (these classes are internal).

KEY RULES:
- EventIds packed sequentially from 3755, no gaps, XML docs naming the range.
- Never log or tag a connection string, an endpoint address, the lock key or an exception message: endpoints are
  identified by index, resources are not metric tags.
- Run the EventId architecture tests.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina/Sharding/Diagnostics/ShardingActivitySource.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 7: Testing

> **Goal**: Every flag reaches its own target; the failure cases run against real Redis.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/DistributedLock/Redis/`) — new `RedLockCoordinatorTests` (fake `IDatabase`, `FakeTimeProvider`: quorum math, drift, unlock-all after failure, one faulted instance, cancellation) and `RedisLockOptionsValidatorTests`; extend `RedisDistributedLockProviderTests`, `ServiceCollectionExtensionsTests`, `RedisDistributedLockHealthCheckTests`
2. **Guard** (`tests/Encina.GuardTests/Infrastructure/DistributedLock/`) — constructors and methods of `RedisLockConnections`, `RedisLockOptionsValidator`, the new overloads
3. **Contract** (`tests/Encina.ContractTests/`) — add the three-instance configuration to the lock provider contract suite that #2667 creates (the same scenarios as the other providers, SPEC-000 AC-027); there is no lock contract suite on `main` today
4. **Property** (`tests/Encina.PropertyTests/`) — FsCheck: for any N in 3..7 and any set of instances that refuse or fault, two concurrent holders never both have a granted majority; validity never exceeds the TTL; the held-lock count is monotone in the number of granting instances
5. **Integration** (`tests/Encina.IntegrationTests/Infrastructure/DistributedLock/`) — a three-container fixture `RedLockFixture` in `tests/Encina.TestInfrastructure/Fixtures/` (Testcontainers, `[Collection("Redis-Quorum")]`, `[Trait("Category", "Integration")]`, `[Trait("Database", "Redis")]`); scenarios: acquire and release, mutual exclusion between two providers, one instance stopped (still acquires), two instances stopped (`Right(None)` or `Left` as classified), restart and recover; the existing single-container test class keeps working
6. **Load** — extend `RedisLockProviderFactory` in `tests/Encina.NBomber/Scenarios/Locking/Providers/` with a three-instance variant so the existing contention scenarios cover it
7. **Benchmark** — justification file `tests/Encina.BenchmarkTests/DistributedLock/RedLock.md` (single-instance against quorum latency is documentation, not a hot path); a number is recorded in the documentation page only from a measured run
8. **Coverage manifest** — per-file targets with one-sentence justifications for `RedLockCoordinator.cs` (unit 95, integration 70), `RedLockScripts.cs`, `RedLockDiagnostics.cs`, `RedLockLogMessages.cs`, `RedisLockConnections.cs`, `RedisLockOptionsValidator.cs`, and raised or kept targets for every touched file (written by `mechanical-fixer`)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #226: tests of the RedLock mode.

CONTEXT:
- Tests must execute real package code (instantiate RedLockCoordinator and the provider); reflection-only tests
  cover nothing. Assertions use Shouldly through Encina.Testing.Shouldly. Time comes from FakeTimeProvider.
- Integration tests use a shared [Collection] fixture, never IClassFixture, and never swallow an exception.
- Per-flag coverage (unit, guard, contract, property, integration) is measured independently against
  .github/coverage-manifest/Encina.DistributedLock.Redis.json.

TASK:
Write the test files listed in the Phase 7 tasks, then measure each flag with
`dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and report the per-file table of
coverage-report.cs against the targets.

KEY RULES:
- No Thread.Sleep; no hard-coded GUIDs; builders for test data.
- Containers are stopped with the Testcontainers API, not by killing processes.
- Run the lock tests with the other lock providers' tests to confirm nothing regressed.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/DistributedLock/RedisDistributedLockIntegrationTests.cs
- tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs
- tests/Encina.NBomber/Scenarios/Locking/Providers/RedisLockProviderFactory.cs
- .github/coverage-manifest/Encina.DistributedLock.Redis.json
```

</details>

### Phase 8: Documentation and Finalization

> **Goal**: Users can configure and understand the quorum mode; the records are complete.

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation on every new public member (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>` for the new overloads)
2. `src/Encina.DistributedLock.Redis/README.md`-level documentation through `docs-writer`: configuration (three endpoints), the failure semantics, what RedLock does and does not guarantee (no fencing; see #218), clock drift, when a single instance is enough
3. `docs/features/` or the distributed lock guide page: a configuration example and the scaling guidance (odd endpoint count, independent failure domains)
4. `changelog.d/226-redlock-quorum.added.md` (written by `mechanical-fixer`)
5. `docs/architecture/adr/` — a short ADR only if the maintainer picks option B of design choice 1 (the library dependency); with option A the choice is recorded in the plan
6. `docs/INVENTORY.md` and `ROADMAP.md` — update if the lock provider list or the milestone text changes
7. `PublicAPI.Unshipped.txt` complete; `dotnet build --configuration Release` with 0 warnings; `dotnet test` passes; every flag reaches its target; CRAP <= 10 on changed methods (local crap-gate table)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are finishing issue #226: documentation and verification.

CONTEXT:
- Documentation pages go through the docs-writer agent and the encina-docs skill; coverage figures are never typed
  by hand (covref markers); the changelog fragment and PublicAPI lines go through mechanical-fixer.

TASK:
Complete the Phase 8 tasks, run the build and the five test flags, and run the local crap-gate and coverage
reports. Report the per-flag figures of the touched files against their targets.

KEY RULES:
- No claim of fencing or of safety under arbitrary clock jumps: state what the algorithm guarantees and what it does
  not.
- English only; no private client application named.
- Zero warnings; no [Obsolete]; no compatibility layer.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
- src/Encina.DistributedLock/README.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Provision | Relevance |
|--------|-----------|-----------|
| Redis documentation, "Distributed Locks with Redis" | The Redlock algorithm: majority of N independent masters, validity = TTL minus elapsed minus drift, unlock all instances, random retry delay | Defines the quorum acquire and the formula of this plan |
| Kleppmann, "How to do distributed locking" (2016) | A lease needs a fencing token; timing assumptions of Redlock | Why fencing stays in #218 and the documentation states the limits |
| ADR-052 | Lock providers return `Either`; contention is `Right(None)`; a fault is `Left` | Outcome mapping of every Redis method |
| SPEC-000 REQ-027, DEC-003, DEC-009, AC-027 | Seven lock providers for 1.0 with contract tests | The Redis provider joins the contract suite with a quorum configuration |
| `AGENTS.md` sections 3, 5, 6, 7 | Time, secrets, errors, registration, EventIds, provider rules | Constraints used throughout |
| ADR-018, ADR-021 | Cross-cutting check; EventId ranges | Matrix and Phase 6 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `RedisDistributedLockProvider` | `src/Encina.DistributedLock.Redis/RedisDistributedLockProvider.cs` | Modified to run on the coordinator; retry loop, `NextDelay` and the Lua scripts are reused |
| `RedisLockOptions` | `src/Encina.DistributedLock.Redis/RedisLockOptions.cs` | Gains `ClockDriftFactor` and `InstanceTimeout` |
| `DistributedLockOptions` | `src/Encina.DistributedLock/DistributedLockOptions.cs` | Base: `KeyPrefix`, `DefaultExpiry`, `DefaultWait`, `DefaultRetry`, `ProviderHealthCheck` |
| `ServiceCollectionExtensions` | `src/Encina.DistributedLock.Redis/ServiceCollectionExtensions.cs` | Two new overload groups; the four existing ones delegate |
| `RedisDistributedLockHealthCheck` | `src/Encina.DistributedLock.Redis/Health/RedisDistributedLockHealthCheck.cs` | Probes every instance, adds Degraded |
| `IEncinaHealthCheck`, `HealthCheckResult` | `src/Encina.Messaging/Health/` | Health check contract, including `Degraded` |
| `EventIdRanges.DistributedLockRedis` | `src/Encina/Diagnostics/EventIdRanges.cs` | Range 3750-3799, already registered and mapped |
| `ShardingActivitySource` | `src/Encina/Sharding/Diagnostics/ShardingActivitySource.cs` | Naming convention of an `ActivitySource` |
| `RedisFixture` | `tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs` | Model for the three-container fixture |
| `RedisLockProviderFactory` | `tests/Encina.NBomber/Scenarios/Locking/Providers/RedisLockProviderFactory.cs` | Multi-instance load variant |
| `EncinaEventIdAllocationTests` | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | Already maps `Encina.DistributedLock.Redis` |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Provider | RedLock quorum | Notes |
|----------|:-:|-------|
| InMemory | N/A | Single process; no quorum |
| Redis | Included | The subject of this plan |
| SqlServer | N/A | `sp_getapplock`; not Redis based |
| PostgreSQL (#207) | N/A | Advisory locks; not Redis based |
| MySQL (#208) | N/A | `GET_LOCK`; not Redis based |
| Azure Blob (#209) | N/A | Blob lease; not Redis based |
| DynamoDB (#210) | N/A | Conditional write; not Redis based |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.DistributedLock.Redis` (existing) | 3750-3754 | Used by `RedisDistributedLockProvider` (acquired, released, extended, failed, release failed) |
| `Encina.DistributedLock.Redis` (this plan) | 3755-3769 | 3755-3762 used by `RedLockLogMessages`; 3763-3769 spare for this plan |
| `Encina.DistributedLock.Redis` (spare) | 3770-3799 | Spare for this assembly only: the architecture test maps each assembly to its ranges, so the tenant decorator of #741 (assembly `Encina.DistributedLock`) cannot use ids here; it registers `DistributedLock` = 5500-5549 (3850-3899 is now `TenancyAspNetCore`) |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production (new) | 6 | `RedLockCoordinator.cs`, `RedLockScripts.cs`, `RedLockLogMessages.cs`, `RedLockDiagnostics.cs`, `RedisLockConnections.cs`, `RedisLockOptionsValidator.cs` |
| Production (modified) | 4 | `RedisDistributedLockProvider.cs`, `RedisLockOptions.cs`, `ServiceCollectionExtensions.cs`, `Health/RedisDistributedLockHealthCheck.cs` |
| PublicAPI and manifest | 2 | `PublicAPI.Unshipped.txt`, `Encina.DistributedLock.Redis.json` |
| Unit tests | 5 | 2 new, 3 extended |
| Guard tests | 2 | Extended |
| Contract tests | 1 | Redis quorum configuration in the suite of #2667 |
| Property tests | 1 | New |
| Integration tests | 2 | Fixture plus scenarios |
| Load and benchmark | 2 | NBomber factory variant; benchmark justification |
| Documentation and changelog | 3 | README, guide page, fragment |
| **Total** | **~28** | Over 3 pull requests of at most 15 files each |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #226: the RedLock quorum algorithm as an opt-in mode of the Redis lock provider.

PROJECT CONTEXT:
- .NET 10 / C# 14, nullable enabled, pre-1.0: best solution first, no compatibility layers, no [Obsolete].
- Package: src/Encina.DistributedLock.Redis (StackExchange.Redis). Lock contract: ADR-052, implemented by #2667
  before this work: TryAcquireAsync returns Either<EncinaError, Option<ILockHandle>>, AcquireAsync returns
  Either<EncinaError, ILockHandle>, IsLockedAsync and ExtendAsync return Either<EncinaError, bool>, cancellation
  propagates, LockErrorCodes holds the Left codes.
- Time only from TimeProvider. EncinaError.Message never reaches logs, tags or health checks.

IMPLEMENTATION OVERVIEW:
1. Options: ClockDriftFactor (0.01) and InstanceTimeout (250 ms) on RedisLockOptions; RedisLockOptionsValidator;
   RedisLockConnections (1..N multiplexers, Quorum = N/2 + 1, ownership flag).
2. RedLockCoordinator (internal): SET NX PX on every instance with a per-instance timeout, validity = TTL - elapsed
   - drift (drift only when N > 1), unlock all instances on failure, majority extend and release with the
   ownership-checked Lua scripts (moved to RedLockScripts).
3. Provider: retry loop with jitter; Acquired -> Right(Some(handle)); contended or timeout -> Right(None); a
   majority of faulted instances -> Left; ExtendAsync and DisposeAsync use the coordinator.
4. Registration: two list overloads (connection strings, multiplexers), single endpoint = single-instance mode,
   count of 2 rejected; TryAdd*; DI test with ValidateOnBuild and ValidateScopes.
5. Health check: per endpoint; Healthy / Degraded / Unhealthy; no endpoint strings in the description.
6. Observability: EventIds 3755-3762 in RedLockLogMessages, ActivitySource and Meter in RedLockDiagnostics.
7. Tests: unit, guard, contract (suite of #2667), property (FsCheck), integration (three Redis containers, stop
   one and two), NBomber variant; documentation through docs-writer; changelog, PublicAPI and manifest through
   mechanical-fixer.

KEY PATTERNS:
- StackExchange.Redis calls take no token: Task.WaitAsync(timeout) and Task.WaitAsync(ct).
- Satellite DI with TryAdd*; options validated at start.
- Methods small enough for CRAP <= 10; classification separate from I/O.

REFERENCE FILES:
- docs/plans/redlock-implementation-plan-226.md
- docs/architecture/adr/052-distributed-lock-failure-contract.md
- src/Encina.DistributedLock.Redis/RedisDistributedLockProvider.cs
- src/Encina.DistributedLock.Redis/ServiceCollectionExtensions.cs
- .github/coverage-manifest/Encina.DistributedLock.Redis.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Locks are ephemeral coordination state; nothing to cache |
| 2 | OpenTelemetry | ✅ | Phase 6: quorum counter, per-instance failure histogram, acquire span; names align with #724 (open) and move onto its shared instruments if it lands first |
| 3 | Structured Logging | ✅ | Phase 6: `[LoggerMessage]` EventIds 3755-3762 inside the registered `DistributedLockRedis` range, exception type only |
| 4 | Health Checks | ✅ | Phase 4: per-endpoint check with a Degraded state |
| 5 | Validation | ✅ | Phases 1 and 4: options validated at startup, endpoint count rule at registration |
| 6 | Resilience | ✅ | Phases 2 and 5: per-instance timeout, tolerated faulted minority; no Polly policy because the provider's own wait/retry loop is the retry |
| 7 | Distributed Locks | ❌ | This is the distributed lock feature itself |
| 8 | Transactions | ❌ | A lock is not a transactional resource; no unit of work is involved |
| 9 | Idempotency | ❌ | Acquisition receives no duplicate requests; a repeated attempt is a new attempt by design |
| 10 | Multi-Tenancy | ⏭️ | Tenant-scoped lock keys are composed above this provider by [#741](https://github.com/dlrivada/Encina/issues/741) (same family, existing issue); the coordinator receives the final key |
| 11 | Module Isolation | ❌ | Isolation is reached with `KeyPrefix`; the provider holds no module data |
| 12 | Audit Trail | ❌ | Acquisition and release are coordination events, not data access or state changes; logs and metrics cover operations |

---

## Prerequisites & Dependencies

| Item | State | Effect on this plan |
|------|-------|---------------------|
| [#2667](https://github.com/dlrivada/Encina/issues/2667) — lock providers return `Either` (ADR-052) | Open, implementation issue (no pending maintainer decision) | Phases 3 and 7 start after it merges; Phases 1 and 2 can start before it |
| [#2654](https://github.com/dlrivada/Encina/issues/2654) — Redis calls ignore the token, docs promise Redlock | Open | The coordinator's `WaitAsync(ct)` fixes the Redis half for the new path; the SQL Server half and the wording remain in that issue |
| [#2405](https://github.com/dlrivada/Encina/issues/2405) — provider `ExtendAsync(resource)` extends a lock it does not own | Open | Not fixed here; the quorum `ExtendAsync` is ownership-checked |
| [#741](https://github.com/dlrivada/Encina/issues/741) — tenant-scoped locks | Open (family plan) | Owns tenant key composition; its decorator logs from its own range (`DistributedLock`, 5500-5549) |
| [#222](https://github.com/dlrivada/Encina/issues/222), [#724](https://github.com/dlrivada/Encina/issues/724) — lock observability | Open | Phase 6 reuses a shared instrument set if it exists |
| [#218](https://github.com/dlrivada/Encina/issues/218), [#223](https://github.com/dlrivada/Encina/issues/223) — fencing tokens, auto-extend | Open | Out of scope, documented as limits |

---

## Next Steps

1. Link this plan from issue #226 and start the PR 1 brief after [#2667](https://github.com/dlrivada/Encina/issues/2667) merges (Phases 1 and 2 may start earlier).
2. When work on the issue starts, ask the maintainer to answer Dilemma 1 (own implementation or RedLock.net), record the answer in a `## Maintainer Decisions` section placed last, and have the adversarial-reviewer verify the Settled and Trivial choices.
3. One commit per phase; the last commit of PR 3 references `Fixes #226`.
