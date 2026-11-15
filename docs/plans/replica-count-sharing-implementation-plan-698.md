# Implementation Plan: Shared replica connection counts for Read/Write Separation

> **Issue**: [#698](https://github.com/dlrivada/Encina/issues/698)
> **Type**: Feature
> **Complexity**: Medium (5 phases, 3 pull requests of 6, 14 and 4 files)
> **Estimated Scope**: about 450 lines of production code, about 900 lines of tests
> **Milestone**: v0.18.0 - Integration & Platform Capabilities (caching, database, read/write separation)

---

## Summary

The issue asks `ICacheProvider` to do three things for Read/Write Separation: cache the write connection string, cache the replica list and a short-lived selection result, and share per-replica connection counts across instances so the `LeastConnections` strategy balances globally.

Re-checked against `origin/main` (db1e7f688), most of that does not fit the code, and the plan keeps only the part that does (Design Choice 1, a Dilemma, recommended option B):

1. **Connection strings are secrets, and they already live in memory.** `ReadWriteSeparationOptions` holds the write and read connection strings and overrides `ToString()` to hide them (the AGENTS.md section 3 rule, #851). Sending them to a shared cache (Redis) would store credentials in plaintext in a second system, and `GetWriteConnectionString()` is a field read: there is nothing to save. The "write-connection", "read-replicas" and "selected" keys of the issue are rejected.
2. **The selector is synchronous and the cache is asynchronous.** `IReadWriteConnectionSelector.GetReadConnectionString()` and `IReplicaSelector.SelectReplica()` return values directly; `ICacheProvider` is `Task`-based. Awaiting inside them would block thread-pool threads (AGENTS.md section 3, S6966), and a cached "selected replica" held for 30 seconds would send every request of an instance to one replica, which defeats balancing.
3. **`ICacheProvider` has no atomic increment and no key enumeration** (`Get`, `Set`, `Remove`, `RemoveByPattern`, `Exists`, `GetOrSet`, sliding expiry, `Refresh`). Counts shared as `replica-count:{id}` entries would be last-writer-wins, and a reader cannot list the instances to sum them.
4. **The counts never go down today.** `LeastConnectionsReplicaSelector.SelectReplica()` increments a replica's count; only `ReleaseReplica` (called by the `ReplicaLease.Dispose` of `AcquireReplica`) decrements it, and nothing in `src/` calls `AcquireReplica` or `ReleaseReplica`: `ReadWriteConnectionSelector.GetReadConnectionString()` calls `SelectReplica()` (checked by grep over `src` on `origin/main`: the only references are inside the selector, its factory and its docs). The counts are therefore monotone and "least connections" is "least selections so far". Sharing them across instances shares a number that carries no load information until a release path exists. This is a bug of its own, recorded as a follow-up issue file (`artifacts/issues/least-connections-never-releases.md`) and a prerequisite of the value of this plan, not of its code.

What remains valuable, once the counts mean something: global least-connections. The plan shares **per-replica ordinal counts** between instances over `IPubSubProvider` (the backplane `Encina.Caching` already owns for cross-instance messages), read from a local snapshot so selection stays synchronous and never blocks. No connection string leaves the process.

This plan builds on #695 (`StoreCacheKey` for the channel name, `StoreCacheTelemetry` naming, the range `CachingStores` 350-389 where this plan takes ids 354-360). The backplane-missing behavior follows the sibling #2408 plan (its Design Choice 7).

**Affected packages:** `Encina.Messaging` (one small interface and an optional constructor parameter on `LeastConnectionsReplicaSelector`), `Encina.Caching` (exchange, hosted service, options, registration). The seven provider registrations (ADO x3, Dapper x3, EF Core) are not changed: the extension replaces the `IReplicaSelector` they register.

**Provider category**: Database (10) for the routing (the selector is shared by the seven provider registrations that call `ReplicaSelectorFactory.Create`: ADO x3, Dapper x3, EF Core; MongoDB has no replica-selector registration); Caching (8) for the backplane: Redis and the four Redis-protocol products have a real pub/sub, Memory is in-process only, Hybrid registers none, Memcached has none (a documented gap).

**Sibling plans (caching family)**: #695 owns the foundation; #2408 owns the generic invalidation subscriber and the same "no backplane" rule; #2472 (open bug: queries sent inside a command read from a replica) is unrelated to counts.

---

## Design Choices

<details>
<summary><strong>1. What is shared</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The issue as written: connection strings, the replica list, selection results and counts, all through `ICacheProvider`** | Matches the acceptance list | Credentials in a shared cache; synchronous selector over an asynchronous cache; a 30-second cached selection freezes balancing; counts without atomic operations |
| **B) Per-replica connection counts only, as ordinals, over pub/sub, read from a local snapshot** | No secret leaves the process; selection stays synchronous and lock-free; works with the cache providers that have a backplane | Not `ICacheProvider` as the issue says; the value needs the release path of the follow-up bug |
| **C) Defer the feature until the release path exists and `ICacheProvider` has an atomic counter** | Builds on correct primitives | Nothing ships; the atomic counter is a new capability for all cache providers |

### Chosen Option: **B - share ordinal counts over the backplane** (recommended, pending the maintainer)

### Rationale

- C wins if the maintainer prefers to design the counter once for every cache provider before any feature uses it; A is not recommended in any scenario because of the credential exposure.
- The issue's items "cache the write connection string", "cache the replica list" and "short-lived cached selection" are dropped with the reasons above; "fallback to local state on cache failure" and "opt-in" are kept.
- Class: Dilemma

</details>

<details>
<summary><strong>2. How instances exchange counts</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pub/sub gossip: each instance publishes its absolute per-replica counts every `PublishInterval` (default 5 seconds) on one channel and keeps the latest message of every other instance with an expiry of three intervals; the global count of a replica is the local count plus the sum of the live remote counts** | No atomic operations and no enumeration needed; absolute values, so duplicates and reordering are harmless; a dead instance expires by itself | Needs a backplane; the view is up to one interval old |
| **B) One `ICacheProvider` entry per instance plus a registry entry listing the instances** | Uses `ICacheProvider` as the issue says | The registry is a read-modify-write on an abstraction without atomics: instances overwrite each other; a crashed instance stays listed until someone cleans it |
| **C) A new atomic increment and key listing on `ICacheProvider`** | The textbook design | A new capability for all eight cache providers (AGENTS.md section 5) before the feature can start; a separate issue |

### Chosen Option: **A - gossip over `IPubSubProvider`** (recommended, pending the maintainer)

### Rationale

- C wins when several features need shared counters (rate limits, quotas); B is rejected because its registry races.
- The channel name is `StoreCacheKey.Build(KeyPrefix, null, "rw", "counts")` of #695, so it is fixed and cannot be influenced by input. The message is `{ v, i, c }`: version 1, the instance id (a GUID created at start) and the counts array; the receiver ignores its own id, a different version, a length different from its replica count (logged as a topology mismatch) and counts outside 0 to 1,000,000.
- Class: Dilemma

</details>

<details>
<summary><strong>3. How the remote counts reach the selector</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `public interface IRemoteReplicaLoad { int GetRemoteConnectionCount(int replicaIndex); }` in `Encina.Messaging`, and an optional trailing parameter on `LeastConnectionsReplicaSelector`'s constructor; `SelectReplica` compares `local + remote` per replica; `Encina.Caching` implements the interface from the exchange snapshot** | One selection algorithm; the release fix of the follow-up issue lands in the same class; `Encina.Messaging` stays free of any cache | A public interface and a changed public constructor (PublicAPI line); the 42 existing `new LeastConnectionsReplicaSelector(` sites (18 in the unit test file, 19 in the guard test file, 2 in benchmark files, 3 in the class and its factory) still compile because the parameter is optional |
| **B) A second selector class in `Encina.Caching` that duplicates the counting** | Messaging unchanged | Two implementations of the same algorithm drift; the follow-up fix would have to be made twice |

### Chosen Option: **A - an optional `IRemoteReplicaLoad`** (recommended, pending the maintainer)

### Rationale

- B wins when the maintainer wants no public change in `Encina.Messaging` for this issue.
- The replica index is the position in `ReadWriteSeparationOptions.ReadConnectionStrings`, in the order the selector already iterates; the exchange publishes in the same order, so a connection string never crosses the process boundary.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Replica identity on the wire</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The ordinal in the configured list, plus the list length as a consistency check** | Nothing derived from a secret leaves the process | Instances configured with the same replicas in a different order mismatch (the length check cannot see it) |
| **B) A hash of each connection string** | Order independent | An unsalted hash of a string that contains a password can be attacked offline by anyone who can read the channel; a hash of a secret is still derived from the secret |
| **C) The connection string** | Trivial | Credentials on the backplane |

### Chosen Option: **A - ordinals and a length check** (recommended, pending the maintainer)

### Rationale

- B wins in a deployment that reorders the replica list per instance (blue/green with different configuration sources) and accepts the offline-attack surface; C violates AGENTS.md section 3 (#851) in every scenario and is not recommended.
- The documentation states that all instances must configure the replicas in the same order.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Failure and fallback</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The publish loop is a `PeriodicTimer` loop that catches per iteration (an `OperationCanceledException` from the stopping token ends it); a failed publish is logged by exception type (`ForLogging()`), counted, and retried at the next tick; a failing or silent backplane leaves the remote counts empty, so selection falls back to local counts; a message handler never throws into the provider** | The behavior the issue asks for ("fallback to local state"); an exception never escapes a background loop | A broken backplane degrades balancing quietly (visible in logs and metrics) |
| **B) Let an exception stop the hosted service** | Loud | In a `BackgroundService` the default behavior then stops the host |

### Chosen Option: **A - catch per iteration, fall back to local**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (background loops and timers never let an exception escape; a `PeriodicTimer` loop catches per iteration, cancellation propagates; errors are logged by error code or through `ForLogging()`) and the acceptance of #698 ("resilient: fallback to local state on cache failure"). B is the failure mode that rule exists to prevent.

</details>

<details>
<summary><strong>6. Missing backplane or a strategy that is not LeastConnections</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaReplicaCountSharing` marks the feature as required: the hosted service fails at start (EventId 360, by type and reason) when no `IPubSubProvider` is registered or `ReadWriteSeparationOptions.ReplicaStrategy` is not `LeastConnections`** | A requested feature that can never work is an error, not a silent no-op (registration completeness); the same rule as the invalidation subscriber of #2408 | An application that registers it without a backplane stops at start |
| **B) Log a warning and do nothing** | Never blocks start | The operator believes balancing is global while it is local |

### Chosen Option: **A - fail at start** (recommended, pending the maintainer)

### Rationale

- B wins for development setups that register everything and run without a backplane; the maintainer may prefer a switch, which is an extra option and a reason to ask.
- The Memory pub/sub provider is in-process only: it passes the check and is correct for a single instance (and for the integration test of the receive path); Hybrid and Memcached have none and fail the check, which the documentation lists.
- Class: Dilemma

</details>

<details>
<summary><strong>7. EventIds</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Ids 354-360 inside the range `CachingStores` (350-389) registered by #695, packed after its 350-353** | No new registry entry; no collision with the family | Depends on #695 PR 1 for the range |
| **B) A new range for this feature** | Independent | A second registry entry in a block of 50 for seven ids |

### Chosen Option: **A - 354-360 in `CachingStores`**

### Rationale

- **Class:** Trivial
- **Settled by:** the family block 350-399 of the plan batch registry (ids packed in the order of the plans: #695 350-353, #698 354-360) and AGENTS.md section 7. Ids: 354 publish failed, 355 receive failed, 356 message rejected (reason code), 357 exchange started, 358 exchange stopped, 359 topology mismatch, 360 startup validation failed.

</details>

---

## Implementation Phases

### Phase 1: The remote-load seam in Encina.Messaging (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 6 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Messaging/ReadWriteSeparation/IRemoteReplicaLoad.cs` | new (public interface) |
| 2 | `src/Encina.Messaging/ReadWriteSeparation/LeastConnectionsReplicaSelector.cs` | modify (optional trailing `IRemoteReplicaLoad? remoteLoad = null`; `SelectReplica` compares local plus remote) |
| 3 | `src/Encina.Messaging/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`; remove the old constructor line, add the new one and the interface) |
| 4 | `tests/Encina.UnitTests/Messaging/ReadWriteSeparation/LeastConnectionsRemoteLoadTests.cs` | new |
| 5 | `tests/Encina.GuardTests/Messaging/ReadWriteSeparation/ReadWriteSeparationGuardTests.cs` | modify (the optional parameter accepts null: a valid-result test) |
| 6 | `.github/coverage-manifest/Encina.Messaging.json` | modify (`mechanical-fixer`) |

Tasks:

1. **Create** `IRemoteReplicaLoad.GetRemoteConnectionCount(int replicaIndex)` (documented: advisory, never negative, called on the selection path, must not block).
2. **Modify** `LeastConnectionsReplicaSelector`: keep the lock-based selection; the comparison value of replica `i` is `local[i] + Math.Max(0, remote?.GetRemoteConnectionCount(i) ?? 0)`; the local increment and `ReleaseReplica` are unchanged. Callers: 42 `new LeastConnectionsReplicaSelector(` sites (`src` 3, tests 39) are unchanged because the parameter is optional.
3. **Tests**: with remote counts the least-loaded replica is the one with the smallest sum; a negative remote value counts as zero; a null remote load behaves exactly as today (the existing unit tests of the selector stay green); the remote load is called once per replica per selection.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #698: global least-connections balancing. The selector is synchronous and the
counts never decrease today (a separate bug), so this PR only adds the seam through which remote counts will reach the selector.

TASK:
1. Create IRemoteReplicaLoad and add the optional trailing parameter to LeastConnectionsReplicaSelector.
2. Compare local plus remote counts per replica index; leave the local increment and release as they are.
3. Write the unit and guard tests; ask mechanical-fixer for the PublicAPI change and the manifest.

KEY RULES:
- No connection string is passed to or from the remote load; the replica index is the position in the configured list.
- The selection path stays synchronous and takes no new lock.
- The new parameter is optional and last; no existing call site changes.
- Zero warnings; CRAP of every method at most 10; XML docs on every public member.

REFERENCE FILES:
- src/Encina.Messaging/ReadWriteSeparation/LeastConnectionsReplicaSelector.cs, ReplicaSelectorFactory.cs
- tests/Encina.UnitTests/Messaging/ReadWriteSeparation/LeastConnectionsReplicaSelectorTests.cs
- tests/Encina.GuardTests/Messaging/ReadWriteSeparation/ReadWriteSeparationGuardTests.cs
```

</details>

### Phase 2: The exchange, the hosted service and the registration (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 14 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/ReadWriteSeparation/ReplicaCountSharingOptions.cs` | new (public) |
| 2 | `src/Encina.Caching/ReadWriteSeparation/ReplicaCountMessage.cs` | new (internal record and its validation) |
| 3 | `src/Encina.Caching/ReadWriteSeparation/ReplicaCountExchange.cs` | new (internal sealed; implements `IRemoteReplicaLoad`, holds the remote snapshot) |
| 4 | `src/Encina.Caching/ReadWriteSeparation/ReplicaCountExchangeService.cs` | new (internal sealed `BackgroundService`) |
| 5 | `src/Encina.Caching/ReadWriteSeparation/ReplicaCountSharingServiceCollectionExtensions.cs` | new (public `AddEncinaReplicaCountSharing`) |
| 6 | `src/Encina.Caching/ReadWriteSeparation/ReplicaCountSharingLog.cs` | new (ids 354-360) |
| 7 | `src/Encina.Caching/ReadWriteSeparation/ReplicaCountSharingTelemetry.cs` | new |
| 8 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 9 | `tests/Encina.UnitTests/Caching/ReadWriteSeparation/ReplicaCountExchangeTests.cs` | new |
| 10 | `tests/Encina.UnitTests/Caching/ReadWriteSeparation/ReplicaCountExchangeServiceTests.cs` | new (fake time) |
| 11 | `tests/Encina.UnitTests/Caching/ReadWriteSeparation/ReplicaCountSharingRegistrationTests.cs` | new (DI test with `ValidateOnBuild` and `ValidateScopes`) |
| 12 | `tests/Encina.GuardTests/Caching/ReplicaCountSharingGuardTests.cs` | new |
| 13 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 14 | `changelog.d/698-replica-count-sharing.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `ReplicaCountSharingOptions` (`PublishInterval` 5 s, `ExpiryIntervals` 3, `MaxCount` 1,000,000; positive-value setters).
2. **Create** `ReplicaCountMessage` (`V`, `I`, `C`) with `TryParse(string, int expectedReplicas, out ReplicaCountMessage?, out string reason)`: rejects empty or oversized text (more than 4096 characters), a wrong version, a length mismatch, counts outside the bound, malformed JSON (all with a reason code, never the text).
3. **Create** `ReplicaCountExchange`: holds the instance id, the local-count source (a `Func<IReadOnlyList<int>>` over the local selector), a `ConcurrentDictionary<Guid, (int[] counts, DateTime receivedAtUtc)>` with expiry by `TimeProvider`, and `GetRemoteConnectionCount(int index)` summing live entries lock-free.
4. **Create** `ReplicaCountExchangeService : BackgroundService`: at start it checks the backplane and the strategy (design choice 6, EventId 360), subscribes with `IPubSubProvider.SubscribeAsync(channel, handler, ct)` and publishes on a `PeriodicTimer` loop that catches per iteration; the handler parses and updates the exchange and never throws; stop disposes the subscription.
5. **Create** `AddEncinaReplicaCountSharing(this IServiceCollection services, Action<ReplicaCountSharingOptions>? configure = null)`: `ArgumentNullException.ThrowIfNull(services)`; registers the options, the exchange, `TimeProvider` and the hosted service with `TryAddEnumerable`; registers `IReplicaSelector` as a singleton factory (removing a previously registered `IReplicaSelector` descriptor, and winning over a later provider `TryAddSingleton`, so the order of the two calls does not matter) that builds `new LeastConnectionsReplicaSelector(options.ReadConnectionStrings, exchange)` from the registered `ReadWriteSeparationOptions` and wires the exchange's local-count source to it.
6. **Tests (unit)**: message parsing and every rejection reason; expiry of a silent instance with `FakeTimeProvider`; own messages ignored; the sum of two remote instances; the publish loop survives a throwing provider and keeps ticking; cancellation ends the loop; start fails without a backplane and with another strategy; DI resolves in both orders against a provider-style `TryAddSingleton<IReplicaSelector>` registration.
7. **Tests (guard)**: extension (`services` null), options setters. The service and exchange are internal (guard target 0 with that reason).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Issue #698. PR 1 added IRemoteReplicaLoad and the optional parameter of LeastConnectionsReplicaSelector. #695 added StoreCacheKey and the
range CachingStores (350-389). IPubSubProvider has PublishAsync(channel, string, ct) and SubscribeAsync(channel, Func<string, Task>, ct).

TASK:
1. Create ReplicaCountSharingOptions, ReplicaCountMessage, ReplicaCountExchange, ReplicaCountExchangeService,
   AddEncinaReplicaCountSharing, ReplicaCountSharingLog (354-360) and ReplicaCountSharingTelemetry as described in the plan.
2. Counts travel as ordinals with a length check; never a connection string.
3. Write the unit, guard and DI tests; ask mechanical-fixer for PublicAPI lines, the manifest and the changelog fragment.

KEY RULES:
- The publish loop catches per iteration; the receive handler never throws; cancellation ends the loop.
- Time comes from TimeProvider; no scoped service in a singleton; the exchange and the selector are singletons.
- The extension fails at start (not at registration) without a backplane or with a strategy other than LeastConnections.
- No count, replica, channel payload or connection string in a log or tag except bounded ordinals; exceptions through ForLogging().
- Zero warnings; CRAP of every method at most 10.

REFERENCE FILES:
- src/Encina.Messaging/ReadWriteSeparation/LeastConnectionsReplicaSelector.cs, ReadWriteSeparationOptions.cs
- src/Encina.ADO.PostgreSQL/ServiceCollectionExtensions.cs (the TryAddSingleton<IReplicaSelector> registration)
- src/Encina.Caching/Abstractions/IPubSubProvider.cs
- src/Encina.Cdc/Caching/CacheInvalidationSubscriberService.cs (hosted subscriber shape)
```

</details>

### Phase 3: Two-instance integration test and documentation (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 4 files:

| # | File | Change |
|---|------|--------|
| 1 | `tests/Encina.IntegrationTests/Infrastructure/Caching/ReplicaCountSharingRedisIntegrationTests.cs` | new (`[Collection(RedisCollection.Name)]`, two exchange instances over the real `RedisPubSubProvider`) |
| 2 | `tests/Encina.LoadTests/ReadWriteSeparation/ReplicaCountSharing.md` | new (justification: the selection path is an in-memory lock; the cost of the exchange is one small message per interval, measured by the integration test) |
| 3 | `docs/features/read-write-separation.md` | modify (`docs-writer`: the opt-in, the backplane requirement, the ordering rule, the documented gaps Memory, Hybrid and Memcached) |
| 4 | `docs/INVENTORY.md` | modify (`docs-writer`) |

The integration test starts two exchanges with the same replica list over one Redis container (the existing `RedisCollection` fixture), makes instance A select replicas, and asserts instance B sees A's counts within two intervals and that B's selection avoids the replica A loaded; a silent instance expires; a message with another replica count is rejected.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
PR 1 and PR 2 of issue #698 are merged. This PR proves count sharing over a real Redis backplane and documents it.

TASK:
1. Write the two-instance integration test with the shared Redis collection fixture and a fake time provider where the loop allows it.
2. Write the load-test justification file; ask docs-writer for the read-write-separation page and the INVENTORY entry.

KEY RULES:
- [Collection(RedisCollection.Name)] shared fixture only; never dispose the shared fixture; real provider, no mock of the backplane.
- The docs state the ordering rule and the backplane gaps; they type no coverage figures.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/Caching/RedisPubSubProviderIntegrationTests.cs
- docs/features/read-write-separation.md
```

</details>

### Phase 4: Observability, logging and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **Telemetry** (`ReplicaCountSharingTelemetry`, PR 2): meter `Encina.Caching.Stores` (the shared name of #695); counters `encina.rw.counts_published`, `encina.rw.counts_received` with tag `outcome` (`accepted`, `rejected`), and an observable gauge `encina.rw.replica_connections` with the tag `replica` (the ordinal, never the connection string). ADR-054 D4: the replicas are infrastructure, not tenant data, so there is no tenant tag. No payload or channel content in any tag (D6).
2. **Logs** (`ReplicaCountSharingLog`, range 354-360 inside `CachingStores`): as listed in Design Choice 7; exceptions through `ForLogging()`; a rejected message is logged by reason code only, never its text.
3. **Error codes**: none. The feature never returns an error to a caller: selection reads a local snapshot, and a start failure is an exception at host start (not an `EncinaError`), as the #2408 subscriber.
4. **Coverage targets** (per file, per flag; measured values replace the estimates before PR 2 and PR 3 open):

| File | unit | guard | integration | Justification |
|------|------|-------|-------------|---------------|
| `ReadWriteSeparation/LeastConnectionsReplicaSelector.cs` (Messaging) | 100 | 0 | 0 | Pure selection logic, every branch unit tested; the existing guard targets of the file are kept |
| `ReadWriteSeparation/ReplicaCountMessage.cs` | 100 | 0 | 40 | Parsing and every rejection unit tested; internal type without `InternalsVisibleTo` for `Encina.GuardTests`; the integration test parses real messages |
| `ReadWriteSeparation/ReplicaCountExchange.cs` | 95 | 0 | 60 | Expiry, sums and own-message handling unit tested with fake time; internal; reached by the two-instance test |
| `ReadWriteSeparation/ReplicaCountExchangeService.cs` | 90 | 0 | 60 | Loop, failure and cancellation paths unit tested; internal; the integration test runs start, publish and stop |
| `ReadWriteSeparation/ReplicaCountSharingOptions.cs` | 100 | 100 | 0 | Setter validation reachable from both flags |
| `ReadWriteSeparation/ReplicaCountSharingServiceCollectionExtensions.cs` | 95 | 90 | 0 | DI test and guard tests reach every line |
| `ReadWriteSeparation/ReplicaCountSharingTelemetry.cs`, `ReplicaCountSharingLog.cs` | 90 | 0 | 0 | Counters and log methods asserted through the service tests; internal |

5. **Property, contract, load, benchmark**: no property test (the unit cases cover the parsing and the sum); no contract test (one implementation of the new interface); the load test and the existing selection benchmarks are justified in the `.md` of PR 3 and by the unchanged hot path; the manifest `reason` says so.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
The replica count sharing of issue #698 exists. Telemetry and logs are added with the PR that introduces each caller (PR 2).

TASK:
Add ReplicaCountSharingTelemetry and ReplicaCountSharingLog (354-360), measure each flag with coverage, and ask mechanical-fixer to
record per-file targets with one-sentence justifications.

KEY RULES:
- EventIds only 354-360 inside the registered range CachingStores; packed, no gaps.
- No connection string, payload or channel content in a log or tag; the replica tag is an ordinal.
- Documentation never types coverage figures.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.Caching.json, Encina.Messaging.json
```

</details>

### Phase 5: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation on `IRemoteReplicaLoad`, `ReplicaCountSharingOptions` and `AddEncinaReplicaCountSharing`, with the ordering rule, the backplane requirement and an example.
2. `docs/features/read-write-separation.md` and `docs/INVENTORY.md` through `docs-writer` (PR 3); `changelog.d/698-replica-count-sharing.added.md` (PR 2); PublicAPI of `Encina.Messaging` and `Encina.Caching`.
3. The follow-up issue file `artifacts/issues/least-connections-never-releases.md` (reported, not opened by this plan).
4. Build with `dotnet build --configuration Release` (0 warnings), `dotnet test` of each flag, `dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
Issue #698 is implemented. Finish documentation and verification.

TASK:
Write the XML docs, ask docs-writer for the page and INVENTORY entry, ask mechanical-fixer for PublicAPI lines and the changelog fragment,
and run the build, tests and coverage checks.

KEY RULES:
- Say in the public docs that global balancing needs the release of replica leases to be wired (the follow-up issue) and a backplane.
- Documentation types no coverage figures.

REFERENCE FILES:
- docs/features/read-write-separation.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 3 | Secrets never serialized or printed (#851); background loops never let an exception escape; no blocking over async (S6966); registration completeness with a DI test; `TimeProvider` |
| AGENTS.md section 5 (database, caching) | The selector serves the database providers; the backplane differs per cache provider (Memcached has none) |
| ADR-012 | Sharded read/write separation (a separate selector family, `IShardReplicaSelector`, not changed here) |
| ADR-054 D4, D6 | No tenant tag for infrastructure state; no payloads in telemetry |
| #712 | Cache Integration epic |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| LeastConnectionsReplicaSelector, IReplicaSelector | src/Encina.Messaging/ReadWriteSeparation | the selector that gains the remote load |
| ReadWriteConnectionSelector, ReplicaSelectorFactory | src/Encina.Messaging/ReadWriteSeparation | consumers of the selector; unchanged |
| ReadWriteSeparationOptions | src/Encina.Messaging/ReadWriteSeparation/ReadWriteSeparationOptions.cs | the replica list and strategy; `ToString()` hides the strings |
| `TryAddSingleton<IReplicaSelector>` registrations | src/Encina.{ADO,Dapper}.{SqlServer,PostgreSQL,MySQL}, src/Encina.EntityFrameworkCore `ServiceCollectionExtensions.cs` | the seven provider registrations the extension replaces |
| IPubSubProvider, RedisPubSubProvider, MemoryPubSubProvider | src/Encina.Caching, .Redis, .Memory | the backplane |
| StoreCacheKey, StoreCacheTelemetry, StoreCacheLog | src/Encina.Caching/MessagingStores (#695) | channel name, metric names, the id range |
| CacheInvalidationSubscriberService | src/Encina.Cdc/Caching | shape of a hosted pub/sub subscriber (moved by #2408) |
| RedisCollection | tests/Encina.IntegrationTests | the shared Redis fixture |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (`CachingStores`, registered by #695) | 354-360 of 350-389 | 350-353 are #695's and reused by #696 and #699; 361-389 free |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 8 | 1 | New: interface, options, message, exchange, service, extension, log, telemetry; modified: `LeastConnectionsReplicaSelector.cs` |
| Tests | 6 | 1 | New: remote-load unit, exchange unit, service unit, DI, guard, integration; modified: `ReadWriteSeparationGuardTests.cs` |
| Documentation | 1 | 2 | New: load-test justification; modified: read-write-separation page, `INVENTORY.md` |
| Configuration | 1 | 4 | New: changelog fragment; modified: two PublicAPI files, `Encina.Messaging.json`, `Encina.Caching.json` |
| **Total** | **16** | **8** | 3 pull requests: 6, 14 and 4 files (24 file touches), each at most 15 files and about 700 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #698: global least-connections balancing for Read/Write
Separation. Connection strings are secrets and the selector is synchronous, so only per-replica ordinal counts are shared, over the pub/sub
backplane, and the selector reads a local snapshot.

IMPLEMENTATION OVERVIEW:
1. PR 1: IRemoteReplicaLoad (Messaging) and the optional parameter of LeastConnectionsReplicaSelector (local plus remote count).
2. PR 2: ReplicaCountMessage, ReplicaCountExchange, ReplicaCountExchangeService (PeriodicTimer loop, catch per iteration), options,
   AddEncinaReplicaCountSharing (replaces the registered IReplicaSelector in any order), logs 354-360, telemetry, tests.
3. PR 3: two-instance Redis integration test, load-test justification, documentation.

KEY PATTERNS:
- Ordinals and a length check on the wire; never a connection string; counts bounded 0..1,000,000; own and stale messages ignored.
- Selection never blocks and never awaits; a silent backplane means local counts.
- Fail at host start without IPubSubProvider or with a strategy other than LeastConnections.
- Loops and handlers never throw; cancellation ends the loop; exceptions through ForLogging(); no payloads in logs or tags.
- Apply Design Choices 1 to 4 and 6 as the Maintainer Decisions section says. The counts carry load information only after the release
  path exists (follow-up issue): state it in the docs.

REFERENCE FILES:
- src/Encina.Messaging/ReadWriteSeparation/LeastConnectionsReplicaSelector.cs, ReadWriteConnectionSelector.cs, ReadWriteSeparationOptions.cs
- src/Encina.ADO.PostgreSQL/ServiceCollectionExtensions.cs
- src/Encina.Caching/Abstractions/IPubSubProvider.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/RedisPubSubProviderIntegrationTests.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Uses the caching category's backplane (`IPubSubProvider`) instead of `ICacheProvider`, because the abstraction has no atomic counter (Design Choice 2); connection strings are not cached (Design Choice 1). |
| 2 | OpenTelemetry | ✅ Include | Published and received counters and an observable gauge by replica ordinal; no payloads; no tenant tag (infrastructure state, ADR-054 D4, D6). |
| 3 | Structured Logging | ✅ Include | EventIds 354-360 in `CachingStores`; rejected messages by reason code only; exceptions through `ForLogging()`. |
| 4 | Health Checks | ❌ N/A | A silent backplane degrades to local counts by design and is visible in metrics and logs; `ReadWriteSeparationHealthCheck` already checks the databases. |
| 5 | Validation | ✅ Include | Options setters and the strict validation of every received message (version, length, bounds, size). |
| 6 | Resilience | ✅ Include | Publish failures retry at the next tick; fallback to local counts; the loop and the handler never throw (Design Choice 5). |
| 7 | Distributed Locks | ❌ N/A | Counts are advisory absolute values, not shared mutable state to lock. |
| 8 | Transactions | ❌ N/A | No transactional state. |
| 9 | Idempotency | ✅ Include | Messages carry absolute counts, so duplicates and reordering are harmless; the latest message of an instance wins. |
| 10 | Multi-Tenancy | ❌ N/A | Replicas are application infrastructure; tenant-specific replicas belong to the sharding module (`IShardReplicaSelector`), which this plan does not change. |
| 11 | Module Isolation | ❌ N/A | Not module scoped. |
| 12 | Audit Trail | ❌ N/A | Balancing counts are not an audit event. |

**Deferred items:** one follow-up issue file is written with this plan: `artifacts/issues/least-connections-never-releases.md` (the counts of `LeastConnectionsReplicaSelector` are never decremented because nothing calls `AcquireReplica` or `ReleaseReplica`; a bug, prerequisite of the value of this plan). An atomic counter on `ICacheProvider` (Design Choice 2, option C) is an alternative, not a deferred item.

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| Release path for `LeastConnectionsReplicaSelector` counts | ⏭️ Open (follow-up issue file written, not yet opened) | PR 1 and PR 2 can merge before it; the counts only carry load information after it |
| #695 foundation (`StoreCacheKey`, `CachingStores` range, telemetry names) | ⏭️ Sibling plan | PR 2 needs #695 PR 1 (range and key builder) merged |
| #2408 (generic invalidation subscriber) | ⏭️ Sibling plan | Same backplane rule; no code dependency |
| `IPubSubProvider` providers | ✅ Available | Redis family real; Memory in-process; Hybrid and Memcached have none |
| Maintainer answers to Design Choices 1 to 4 and 6 | Pending | Asked when work starts |

Blocking prerequisite for PR 2: #695 PR 1.

---

## Next Steps

1. The orchestrator opens the follow-up issue from `artifacts/issues/least-connections-never-releases.md`, asks the maintainer the Dilemmas (Design Choices 1, 2, 3, 4 and 6) when work starts, records the answers under `## Maintainer Decisions`, then briefs PR 1 immediately and PR 2 after #695 PR 1.
2. If the maintainer answers C of Design Choice 1, close #698 after the atomic-counter issue is opened.
3. After PR 3, comment on #698 with the integration result.
